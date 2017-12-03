using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;

namespace DarkStatsCore.Data
{
    public static class ScrapeTask
    {
        private static Task _scrapeTask;
        private static CancellationTokenSource _cancellationTokenSource;
        private static List<long> _scrapeTime = new List<long>();
        private const int _scrapeTimeToKeep = 60;
        public static DateTime LastGathered = DateTime.MinValue;
        public static TimeSpan TimeSpanSinceLastCheck = TimeSpan.FromSeconds(0);
        public static double ScrapeTimeAvg => _scrapeTime.Count() == 0 ? 0 : _scrapeTime.Average();

        public static void StartScrapeTask(TimeSpan saveTime, string url)
        {
            if (_scrapeTask != null)
            {
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _scrapeTask = RunScrapeTask(url, saveTime, _cancellationTokenSource.Token);
            DnsService.Start();
        }

        private static async Task RunScrapeTask(string url, TimeSpan saveTime, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                GatherData(url);
                await Task.Delay(saveTime);
            }
        }

        private static void GatherData(string url)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Scraper.Scrape(url);
                stopwatch.Stop();
                var now = DateTime.Now;
                TimeSpanSinceLastCheck = now.Subtract(LastGathered);
                LastGathered = now;
                if (_scrapeTime.Count() == _scrapeTimeToKeep)
                {
                    _scrapeTime.RemoveAt(0);
                }
                _scrapeTime.Add(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Error on scrape");
            }
        }
    }
}
