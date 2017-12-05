using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DarkStatsCore.Data.Models;
using Serilog;
using System.Net.Http;

namespace DarkStatsCore.Data
{
    public static class DashboardScrape
    {
        public static bool IsTaskActive => _scrapeTask == null ? false : true;
        public static EventHandler<DashboardEventArgs> DataGathered;
        private static List<HostDelta> _dashboardDeltas = new List<HostDelta>();
        private static bool _updateEvent;
        private static DateTime _lastGathered = DateTime.Now;
        private static Task _scrapeTask;

        public static void StartDashboardScrapeTask(string url, TimeSpan refreshTime, CancellationToken cancellationToken)
        {
            if (!IsTaskActive)
            {
                Log.Information("Starting dashboard scrape...");
                _updateEvent = false;
                _scrapeTask = ExecuteScrapeTask(url, refreshTime, cancellationToken);
            }
        }

        private async static Task ExecuteScrapeTask(string url, TimeSpan refreshTime, CancellationToken cancellationToken)
        {
            using (var httpClient = new HttpClient())
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Scrape(url, httpClient);
                    await Task.Delay(refreshTime);
                }
            }
            _updateEvent = false;
            _dashboardDeltas = new List<HostDelta>();
            _scrapeTask = null;
            Log.Information("Dashboard scrape stopped");
        }       

        private static void Scrape(string url, HttpClient httpClient)
        {
            try
            {
                var traffic = Scraper.ScrapeData(url, httpClient);
                traffic.AdjustDeltas(_dashboardDeltas);
                var elapsedMs = DateTime.Now.Subtract(_lastGathered).TotalMilliseconds;
                _lastGathered = DateTime.Now;
                if (_updateEvent)
                {
                    var deltas = _dashboardDeltas.Where(h => h.LastCheckDeltaBytes > 0)
                        .OrderByDescending(h => h.LastCheckDeltaBytes)
                        .ToList();
                    DataGathered?.Invoke(null, new DashboardEventArgs(deltas, elapsedMs));
                }
                else
                {
                    _updateEvent = true;
                }
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Error on dashboard scrape");
            }
        }
    }
}