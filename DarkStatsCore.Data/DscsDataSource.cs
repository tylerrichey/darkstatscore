using DarkStatsCore.Data.Models;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DarkStatsCore.Data
{
    public class DscsDataSource : DataSource
    {
        private DateTime _lastGatheredSave = DateTime.MinValue;
        private DateTime _lastGatheredDash = DateTime.MinValue;
        private TcpClient _client;
        private NetworkStream _stream;
        private int _updateFrequencySeconds;
        private bool _dashboardActive, _firstDashUpdate, _updateFrequency;
        private readonly List<long> _deltas = new List<long>();

        public DscsDataSource(string url) : base(url) { }

        public override async Task DashboardDataTask(TimeSpan refreshTime, CancellationToken cancellationToken)
        {
            var lastFrequency = _updateFrequencySeconds;
            UpdateFrequency(refreshTime.TotalSeconds.ToInt32());
            _dashboardActive = true;
            _firstDashUpdate = true;
            await Task.Run(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(Convert.ToInt32(refreshTime.TotalMilliseconds));
                }
                _dashboardActive = false;
                UpdateFrequency(lastFrequency);
            });
        }

        public override async Task GatherDataTask(TimeSpan saveTime, CancellationToken cancellationToken)
        {
            var dashUpdates = new List<Dictionary<string, DscsModel>>();
            while (true)
            {
                var saveTask = Task.CompletedTask;
                try
                {
                    _client = Connect();
                    _stream = _client.GetStream();
                    UpdateFrequency(saveTime.TotalSeconds.ToInt32());

                    while (_stream.CanRead && !cancellationToken.IsCancellationRequested)
                    {
                        if (_updateFrequency)
                        {
                            var update = Encoding.ASCII.GetBytes(_updateFrequencySeconds.ToString() + '\n');
                            await _stream.WriteAsync(update, 0, update.Length);
                            _updateFrequency = false;
                        }
                        if (_stream.DataAvailable)
                        {
                            var buffer = new byte[1024];
                            var msg = string.Empty;
                            int numberOfBytesRead = 0;
                            while (_stream.DataAvailable)
                            {
                                numberOfBytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                                msg += Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead);
                            }

                            Dictionary<string, DscsModel> data;
                            try
                            {
                                data = JsonConvert.DeserializeObject<Dictionary<string, DscsModel>>(msg);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "Error parsing data: " + msg);
                                continue;
                            }

                            if (_dashboardActive)
                            {
                                dashUpdates.Add(data);
                                if (_firstDashUpdate)
                                {
                                    var update = data.ToHostDeltas();
                                    foreach (var d in update)
                                    {
                                        d.LastCheckDeltaBytes /= DateTime.Now.Subtract(_lastGatheredSave).Seconds;
                                    }
                                    DashboardGatherTask.DataGathered?.Invoke(null, new DashboardEventArgs(
                                        update, _updateFrequencySeconds * 1000));
                                    _firstDashUpdate = false;
                                }
                                else
                                {
                                    var elapsedMs = DateTime.Now.Subtract(_lastGatheredDash).TotalMilliseconds;
                                    var deltas = data.ToHostDeltas();
                                    DashboardGatherTask.DataGathered?.Invoke(null, new DashboardEventArgs(
                                        deltas, elapsedMs));
                                }
                                _lastGatheredDash = DateTime.Now;
                            }
                            if (!_dashboardActive || DateTime.Now.Subtract(_lastGatheredSave).Seconds >= saveTime.TotalSeconds)
                            {
                                var dash = dashUpdates.SelectMany(d => d.ToTrafficStats());
                                var ts = data.ToTrafficStats().ToList();
                                if (dash.Any())
                                {
                                    foreach (var t in ts)
                                    {
                                        var dashInfo = dash.Where(d => d.Ip == t.Ip);
                                        t.In += dashInfo.Sum(d => d.In);
                                        t.Out += dashInfo.Sum(d => d.Out);
                                    }
                                    dashUpdates.Clear();
                                }

                                await saveTask;
                                saveTask = UpdateDatabase(ts);
                                _lastGatheredSave = DateTime.Now;
                                if (_deltas.Count == 30)
                                {
                                    _deltas.RemoveAt(0);
                                }
                                _deltas.Add(ts.Sum(t => t.In + t.Out));
                            }
                        }

                        await Task.Delay(50);
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal(e, "Error on Dscs data gather.");
                }
                finally
                {
                    _stream.Close();
                    _client.Close();
                    await saveTask;
                    Log.Information("Waiting 5 seconds and then attempting to re-connect...");
                    await Task.Delay(5000);
                }
            }
        }

        private async Task UpdateDatabase(IEnumerable<TrafficStats> ts)
        {
            using (var db = new DarkStatsDbContext())
            {
                foreach (var h in ts)
                {
                    var exists = db.TrafficStats.FirstOrDefault(t => t.Ip == h.Ip && t.Day == h.Day);
                    if (exists != null)
                    {
                        exists.In += h.In;
                        exists.Out += h.Out;
                        exists.LastSeen = h.LastSeen;
                        exists.Hostname = h.Hostname;
                        exists.Mac = h.Mac;
                        db.Update(exists);
                    }
                    else
                    {
                        db.Add(h);
                    }
                }
                await db.SaveChangesAsync();
                DataGatherTask.ScrapeSaved?.Invoke(null, EventArgs.Empty);
                Log.Information("Dscs: Saved.");
            }
        }

        private TcpClient Connect()
        {
            var uri = new Uri(_url);
            return new TcpClient(uri.Host, uri.Port);
        }

        private void UpdateFrequency(int seconds)
        {
            _updateFrequencySeconds = seconds;
            _updateFrequency = true;
        }

        public override ValidationResult Test()
        {
            try
            {
                var client = Connect();
                client.Close();
            }
            catch (Exception e)
            {
                return new ValidationResult("Error connecting: " + e.Message);
            }
            return ValidationResult.Success;
        }

        public override DateTime GetLastGathered() => _lastGatheredSave;

        public override double GetScrapeTimeAvg() => 0;

        public override List<long> GetDashboardDeltas() => _deltas;
    }
}
