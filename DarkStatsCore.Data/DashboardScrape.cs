using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using DarkStatsCore.Data.Models;

namespace DarkStatsCore.Data
{
    public static class DashboardScrape
    {
        public static List<HostDelta> Deltas 
        { 
            get
            {
                return _dashboardDeltas.Where(h => h.LastCheckDeltaBytes > 0)
                                .OrderByDescending(h => h.LastCheckDeltaBytes)
                                .ToList();
            } 
        }
        public static bool IsTimerActive => _scrapeTimer != null;
        public static EventHandler DataGathered;
        public static double ElapsedMs = 0;
        private static List<HostDelta> _dashboardDeltas = new List<HostDelta>();
        private static Timer _scrapeTimer;
        private static TimeSpan _refreshTime;
        private static bool _updateEvent;
        private static DateTime _lastGathered = DateTime.Now;

        public static void StartScrapeTimer(string url, TimeSpan refreshTime)
        {
            if (_scrapeTimer == null)
            {
                _refreshTime = refreshTime;
                _updateEvent = false;
                _scrapeTimer = new Timer(Scrape, url, TimeSpan.FromSeconds(0), refreshTime);
            }
        }

        public static void StopScrapeTimer()
        {
            _scrapeTimer.Dispose();
            _scrapeTimer = null;
            _updateEvent = false;
            _dashboardDeltas = new List<HostDelta>();
        }

        private static void Scrape(object url)
        {
            try
            {
                var traffic = Scraper.ScrapeData(url as string);
                traffic.AdjustDeltas(_dashboardDeltas);
                ElapsedMs = DateTime.Now.Subtract(_lastGathered).TotalMilliseconds;
                _lastGathered = DateTime.Now;
                if (_updateEvent)
                {
                    DataGathered(null, EventArgs.Empty);
                }
                else
                {
                    _updateEvent = true;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error on Dashboard scrape: " + e.InnerException != null ? e.InnerException.Message : e.Message);
            }
        }
    }
}