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
        private static CancellationTokenSource _cancellationToken;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static List<(string clientId, EventHandler<DashboardEventArgs> dashEvent, EventHandler saveEvent)> _subs = 
            new List<(string clientId, EventHandler<DashboardEventArgs> dashEvent, EventHandler saveEvent)>();

        public static void StartDashboardScrapeTask(string url, TimeSpan refreshTime, string clientId, EventHandler<DashboardEventArgs> dashEvent, EventHandler saveEvent)
        {
            _lock.Wait();
            _subs.Add((clientId, dashEvent, saveEvent));
            DataGathered += dashEvent;
            ScrapeTask.ScrapeSaved += saveEvent;
            if (!IsTaskActive)
            {
                Log.Information("Starting dashboard scrape...");
                _updateEvent = false;
                _cancellationToken = new CancellationTokenSource();
                _scrapeTask = ExecuteScrapeTask(url, refreshTime, _cancellationToken.Token);
            }
            _lock.Release();
        }

        public static void StopDashboardScrapeTask(string clientId)
        {
            _lock.Wait();
            if (_subs.Count == 1)
            {
                _cancellationToken.Cancel();
            }
            var handlers = _subs.Find(s => s.clientId == clientId);
            DataGathered -= handlers.dashEvent;
            ScrapeTask.ScrapeSaved -= handlers.saveEvent;
            _subs.RemoveAll(s => s.clientId == clientId);
            _lock.Release();
        }

        private async static Task ExecuteScrapeTask(string url, TimeSpan refreshTime, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Scrape(url);
                await Task.Delay(refreshTime);
            }
            _updateEvent = false;
            _dashboardDeltas = new List<HostDelta>();
            _scrapeTask = null;
            Log.Information("Dashboard scrape stopped");
        }

        private static void Scrape(string url)
        {
            try
            {
                var traffic = Scraper.ScrapeData(url);
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
                    //skip first update to get deltas
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