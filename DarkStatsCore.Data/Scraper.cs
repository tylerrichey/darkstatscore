using System;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using DarkStatsCore.Data.Models;
using Serilog;

namespace DarkStatsCore.Data
{
    public static class Scraper
    {
        public static List<long> Deltas = new List<long>();
        private static long _lastCheckTotalBytes = 0;
        private static int _deltasToKeep = 30;
        private static List<HostPadding> _hostPadding = new List<HostPadding>();
        private static List<TrafficCache> _hourCache = new List<TrafficCache>();
        private static DateTime _currentHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
        private static List<TrafficStats> _traffic;
        public static void Scrape(string url)
        {
            _currentHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
            var stopwatch = Stopwatch.StartNew();
            var context = new DarkStatsDbContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var stats = ScrapeData(url);
            if (stats.Count() == 0)
            {
                throw new Exception("Aborting, scrape empty.");
            }
            if (_traffic == null)
            {
                _traffic = context.TrafficStats
                    .Where(t => t.Day.Month == DateTime.Now.Month && t.Day.Year == DateTime.Now.Year)
                    .ToList();
            }
            PopulateTrafficCache();
            CalculateHostPadding(stats);
            AdjustDeltas(stats);
            Log.Information("Gathering Data... ({DataTimer}ms) ", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
            foreach (var s in stats)
            {
                if (!_hourCache.Any(c => c.Ip == s.Ip))
                {
                    //only need to fill with empty here if first time host
                    _hourCache.Add(new TrafficCache
                    {
                        Ip = s.Ip,
                        HourAdded = DateTime.Now.Hour,
                        In = 0,
                        Out = 0
                    });
                }

                var last = _traffic.Where(t => t.Ip == s.Ip).OrderByDescending(t => t.Day).FirstOrDefault();
                if (last == null)
                {
                    context.Add(s);
                    _traffic.Add(s);
                    continue;
                }

                var cache = _hourCache.First(m => m.Ip == s.Ip);
                s.In = s.In - cache.In;
                s.Out = s.Out - cache.Out;

                if (last.Day == s.Day)
                {
                    context.Update(s);
                    _traffic.RemoveAll(t => t.Ip == s.Ip && t.Day == s.Day);
                    _traffic.Add(s);
                }
                else
                {
                    context.Add(s);
                    _traffic.Add(s);
                }
            }
            context.SaveChanges();
            Log.Information("Saving... ({SaveTimer}ms) ", stopwatch.ElapsedMilliseconds);
            context.Dispose();
        }

        private static void PopulateTrafficCache()
        {
            if (_hourCache.RemoveAll(m => m.HourAdded != DateTime.Now.Hour) > 0 || _hourCache.Count == 0)
            {
                _hourCache = _traffic.Where(t => t.Day != _currentHour && t.Day.Month == _currentHour.Month && t.Day.Year == _currentHour.Year)
                    .GroupBy(t => t.Ip)
                    .Select(t => new TrafficCache
                    {
                        Ip = t.Key,
                        HourAdded = _currentHour.Hour,
                        In = t.Sum(i => i.In),
                        Out = t.Sum(o => o.Out)
                    })
                    .ToList();
            }
        }

        private static void CalculateHostPadding(List<TrafficStats> stats)
        {
            _hostPadding.RemoveAll(h => h.Month != DateTime.Now.Month || h.Year != DateTime.Now.Year);
            if (stats.Sum(s => s.In + s.Out) + _hostPadding.Sum(h => h.In + h.Out) < _traffic.Sum(t => t.In + t.Out))
            {
                Log.Information("Source stats are lower than db, calculating host padding...");
                _hostPadding = new List<HostPadding>();
                foreach (var t in _traffic.GroupBy(t => t.Ip))
                {
                    var current = stats.FirstOrDefault(s => s.Ip == t.Key);
                    if (current != null)
                    {
                        _hostPadding.Add(new HostPadding
                        {
                            Ip = t.Key,
                            In = current.In < t.Sum(s => s.In) ? t.Sum(s => s.In) - current.In : 0,
                            Out = current.Out < t.Sum(s => s.Out) ? t.Sum(s => s.Out) - current.Out : 0,
                            Month = DateTime.Now.Month,
                            Year = DateTime.Now.Year
                        });
                    }
                }
            }
            if (_hostPadding.Any())
            {
                stats.ForEach(s =>
                {
                    var padding = _hostPadding.FirstOrDefault(h => h.Ip == s.Ip);
                    if (padding != null)
                    {
                        s.In = s.In + padding.In;
                        s.Out = s.Out + padding.Out;
                    }
                });
            }
        }

        private static void AdjustDeltas(List<TrafficStats> stats)
        {
            if (_lastCheckTotalBytes > 0)
            {
                if (Deltas.Count() == _deltasToKeep)
                {
                    Deltas.RemoveAt(0);
                }
                Deltas.Add(stats.Sum(s => s.In + s.Out) - _lastCheckTotalBytes);
            }
            _lastCheckTotalBytes = stats.Sum(s => s.In + s.Out);
        }

        public static List<TrafficStats> ScrapeData(string url)
        {
            string rawData = string.Empty;
            try
            {
                rawData = GetHtml(url);
            }
            catch
            {
                Log.Warning("Initial scrape timed out, trying again...");
                rawData = GetHtml(url);
            }

            var data = new HtmlDocument();
            data.LoadHtml(rawData);

            var trafficStats = data.DocumentNode
                       .Descendants("tr")
                       .Select(x => x.Elements("td"))
                       .Where(x => x.Count() == 7)
                       .Select(x => new TrafficStats
                       {
                           Ip = x.ElementAt(0).InnerText,
                           Hostname = DnsService.GetHostName(x.ElementAt(0).InnerText, x.ElementAt(1).InnerText),
                           Mac = x.ElementAt(2).InnerText,
                           In = Convert.ToInt64(x.ElementAt(3).InnerText.Replace(",", "")),
                           Out = Convert.ToInt64(x.ElementAt(4).InnerText.Replace(",", "")),
                           LastSeen = x.ElementAt(6).InnerText,
                           Day = _currentHour
                       })
                       .ToList();
            return trafficStats;
        }

        private static string GetHtml(string url)
        {
            //static HttpClient seems to be leaving sockets open for some reason
            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(200) })
            {
                return httpClient.GetStringAsync(url + @"hosts/?full=yes&sort=total").Result;
            }
        }
    }
}
