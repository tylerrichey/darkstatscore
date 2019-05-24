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
        private bool _dashboardActive, _firstDashUpdate;
        private readonly List<long> _deltas = new List<long>();
        private List<Dictionary<string, DscsModel>> _dashUpdates = new List<Dictionary<string, DscsModel>>();
        private Task _saveTask = Task.CompletedTask;
        private Timer _dataCheckTimer;
        private CancellationToken _cancellationToken;
        private TimeSpan _saveTime;

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
                _firstDashUpdate = true;
                UpdateFrequency(lastFrequency);
            });
        }

        public override Task GatherDataTask(TimeSpan saveTime, CancellationToken cancellationToken)
        {
            _saveTime = saveTime;
            _cancellationToken = cancellationToken;
            UpdateFrequency(saveTime.TotalSeconds.ToInt32());
            return Task.CompletedTask;
        }

        private async Task RetrieveData()
        {
            try
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    _dataCheckTimer = null;
                    return;
                }
                if (_client == null || !_client.Connected)
                {
                    _client = Connect();
                    _stream = _client.GetStream();
                }
                var ping = Encoding.ASCII.GetBytes(_updateFrequencySeconds.ToString() + '\n');
                await _stream.WriteAsync(ping, 0, ping.Length);

                var buffer = new byte[51200];
                int numberOfBytesRead = 0;
                numberOfBytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                var msg = Encoding.ASCII.GetString(buffer, 0, numberOfBytesRead);

                Dictionary<string, DscsModel> data;
                try
                {
                    data = JsonConvert.DeserializeObject<Dictionary<string, DscsModel>>(msg);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error parsing data: " + msg);
                    return;
                }

                if (_dashboardActive)
                {
                    _dashUpdates.Add(data);
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
                if (!_dashboardActive || DateTime.Now.Subtract(_lastGatheredSave).Seconds >= _saveTime.TotalSeconds)
                {
                    var dash = _dashUpdates.SelectMany(d => d.ToTrafficStats());
                    var ts = data.ToTrafficStats().ToList();
                    if (dash.Any())
                    {
                        foreach (var t in ts)
                        {
                            var dashInfo = dash.Where(d => d.Ip == t.Ip);
                            t.In += dashInfo.Sum(d => d.In);
                            t.Out += dashInfo.Sum(d => d.Out);
                        }
                        _dashUpdates.Clear();
                    }

                    _saveTask = UpdateDatabase(ts);
                    _lastGatheredSave = DateTime.Now;
                    if (_deltas.Count == 30)
                    {
                        _deltas.RemoveAt(0);
                    }
                    _deltas.Add(ts.Sum(t => t.In + t.Out));
                }
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Error on Dscs data gather.");
                _stream?.Close();
                _client?.Close();
            }
            finally
            {
                await _saveTask;
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
            var client = new TcpClient(uri.Host, uri.Port)
            {
                SendTimeout = _updateFrequencySeconds * 1000,
                ReceiveTimeout = _updateFrequencySeconds * 1000
            };
            return client;
        }

        private void UpdateFrequency(int seconds)
        {
            _updateFrequencySeconds = seconds;
            if (_client != null)
            {
                _client.SendTimeout = seconds * 1000;
                _client.ReceiveTimeout = seconds * 1000;
            }
            if (_dataCheckTimer == null)
            {
                _dataCheckTimer = new Timer(async (s) => await RetrieveData(), null, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(seconds));
            }
            else
            {
                _dataCheckTimer.Change(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));
            }
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
