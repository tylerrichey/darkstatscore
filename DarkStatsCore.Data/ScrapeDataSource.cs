using DarkStatsCore.Data.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DarkStatsCore.Data
{
    public class ScrapeDataSource : DataSource
    {
        private readonly List<long> _scrapeTime = new List<long>();
        //private TimeSpan _timeSpanSinceLastCheck = TimeSpan.FromSeconds(0);
        private int _scrapeTimeToKeep = 5;
        private DateTime _lastGatheredSave = DateTime.MinValue;
        private DateTime _lastGatheredDash = DateTime.MinValue;
        private bool _updateEvent;
        private List<HostDelta> _dashboardDeltas = new List<HostDelta>();

        public ScrapeDataSource(string url) : base(url) { }

        public override async Task GatherDataTask(TimeSpan saveTime, CancellationToken cancellationToken)
        {
            _scrapeTimeToKeep = saveTime.TotalSeconds > 300 ? 1 : (int)Math.Round(300 / saveTime.TotalSeconds);
            while (!cancellationToken.IsCancellationRequested)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    Scraper.Scrape(_url);
                    stopwatch.Stop();
                    var now = DateTime.Now;
                    //_timeSpanSinceLastCheck = now.Subtract(_lastGatheredSave);
                    _lastGatheredSave = now;
                    if (_scrapeTime.Count == _scrapeTimeToKeep)
                    {
                        _scrapeTime.RemoveAt(0);
                    }
                    _scrapeTime.Add(stopwatch.ElapsedMilliseconds);
                    DataGatherTask.ScrapeSaved?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception e)
                {
                    Log.Fatal(e, "Error on scrape");
                }
                await Task.Delay(saveTime);
            }
        }

        public override async Task DashboardDataTask(TimeSpan refreshTime, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var traffic = Scraper.ScrapeData(_url);
                    traffic.AdjustScrapeDeltas(_dashboardDeltas);
                    var elapsedMs = DateTime.Now.Subtract(_lastGatheredDash).TotalMilliseconds;
                    _lastGatheredDash = DateTime.Now;
                    if (_updateEvent)
                    {
                        var deltas = _dashboardDeltas.Where(h => h.LastCheckDeltaBytes > 0)
                            .ToList();
                        DashboardGatherTask.DataGathered?.Invoke(null, new DashboardEventArgs(deltas, elapsedMs));
                    }
                    else
                    {
                        //skip first update to get deltas
                        _updateEvent = true;
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal(e, "Error on dashboard scrape");
                }
                await Task.Delay(refreshTime);
            }
            _updateEvent = false;
            _dashboardDeltas = new List<HostDelta>();
        }

        public override ValidationResult Test()
        {
            int count = 0;
            try
            {
                count = Scraper.ScrapeData(_url).Count;
            }
            catch
            {
                return new ValidationResult("Could not connect to the specified URL, please try again.");
            }
            if (count == 0)
            {
                return new ValidationResult("URL returned no results when scraping for data, please try again.");
            }
            return ValidationResult.Success;
        }

        public override DateTime GetLastGathered() => _lastGatheredSave;

        public override double GetScrapeTimeAvg() => _scrapeTime.Count == 0 ? 0 : _scrapeTime.Average();

        public override List<long> GetDashboardDeltas() => Scraper.Deltas;
    }
}
