using System;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using DarkStatsCore.Data.Models;

namespace DarkStatsCore.Data
{
    public static class Scraper
    {
        public static List<long> Deltas = new List<long>();
        public static DateTime LastGathered = DateTime.MinValue;
        public static TimeSpan TimeSpanSinceLastCheck = TimeSpan.FromSeconds(0);
        public static double ScrapeTimeAvg => _scrapeTime.Count() == 0 ? 0 : _scrapeTime.Average();
        public static EventHandler ScrapeSaved;
        private static List<long> _scrapeTime = new List<long>();
        private const int _scrapeTimeToKeep = 60;
        private static long _lastCheckTotalBytes = 0;
        private static int _deltasToKeep = 30;
        private static List<HostPadding> _hostPadding = new List<HostPadding>();        
        private static Task _scrapeTask;
        private static CancellationTokenSource _cancellationTokenSource;

        public static void Scrape(string url)
        {
            var context = new DarkStatsDbContext();
            context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var stats = ScrapeData(url);

            if (stats.Count() == 0)
            {
                throw new Exception("Aborting, scrape empty.");
            }

            var traffic = context.TrafficStats
                                  .Where(t => t.Day.Month == DateTime.Now.Month && t.Day.Year == DateTime.Now.Year)
                                  .ToList();
            _hostPadding.RemoveAll(h => h.Month != DateTime.Now.Month && h.Year != DateTime.Now.Year);
            if (stats.Sum(s => s.In + s.Out) + _hostPadding.Sum(h => h.In + h.Out) < traffic.Sum(t => t.In + t.Out))
            {
                Console.Write("Source stats are lower than db, calculating host padding... ");
                _hostPadding = new List<HostPadding>();
                foreach (var t in traffic.GroupBy(t => t.Ip))
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

            if (_lastCheckTotalBytes > 0)
            {
                if (Deltas.Count() == _deltasToKeep)
                {
                    Deltas.RemoveAt(0);
                }
                Deltas.Add(stats.Sum(t => t.In + t.Out) - _lastCheckTotalBytes);
            }
            _lastCheckTotalBytes = stats.Sum(t => t.In + t.Out);

            Console.Write("Saving... ");
            var lastRecords = context.TrafficStats
                                     .GroupBy(t => t.Ip)
                                     .Select(t => new
                                     {
                                         Ip = t.Key,
                                         Latest = t.Max(d => d.Day)
                                     })
                                     .ToList();

            foreach (var s in stats)
            {
                if (string.IsNullOrEmpty(s.Hostname) || s.Hostname == "(none)")
                {
                    DnsService.GetHostName(s);
                }
                var last = lastRecords.FirstOrDefault(t => t.Ip == s.Ip);
                if (last == null)
                {
                    context.Add(s);
                    continue;
                }

                if (last.Latest.Month != s.Day.Month || last.Latest.Year != s.Day.Year)
                {
                    context.Add(s);
                    continue;
                }

                var current = traffic.First(t => t.Ip == s.Ip && t.Day == last.Latest);
                if (current.Day == s.Day)
                {
                    var items = traffic.Where(t => t.Ip == s.Ip &&
                                                   t.Day != s.Day &&
                                                   t.Day.Month == s.Day.Month &&
                                                   t.Day.Year == s.Day.Year);
                    if (items != null)
                    {
                        s.In = s.In - items.Sum(t => t.In);
                        s.Out = s.Out - items.Sum(t => t.Out);
                    }
                    context.Update(s);
                }
                else
                {
                    var items = traffic.Where(t => t.Ip == s.Ip &&
                                                   t.Day.Month == s.Day.Month &&
                                                   t.Day.Year == s.Day.Year);
                    s.In = s.In - items.Sum(t => t.In);
                    s.Out = s.Out - items.Sum(t => t.Out);
                    context.Add(s);
                }
            }
            context.SaveChanges();

            var now = DateTime.Now;
            TimeSpanSinceLastCheck = now.Subtract(LastGathered);
            LastGathered = now;
            if (DashboardScrape.IsTaskActive)
            {
                ScrapeSaved?.Invoke(null, EventArgs.Empty);
            }
            context.Dispose();
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
                Console.Write("Initial scrape timed out, trying again. ");
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
                           Day = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0)
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

        public static void StartScrapeTask(TimeSpan saveTime, string url)
        {
            if (_scrapeTask != null)
            {
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            _scrapeTask = ScrapeTask(url, saveTime, _cancellationTokenSource.Token);
            DnsService.Start();
        }
        
        private static async Task ScrapeTask(string url, TimeSpan saveTime, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                GatherData(url);
                await Task.Delay(saveTime);
            }
        }

        private static void GatherData(string url)
        {
            Console.Write(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " - Gathering data... ");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                Scraper.Scrape(url);
                stopwatch.Stop();
                Console.WriteLine("Done. (" + stopwatch.ElapsedMilliseconds + "ms)");
                if (_scrapeTime.Count() == _scrapeTimeToKeep)
                {
                    _scrapeTime.RemoveAt(0);
                }
                _scrapeTime.Add(stopwatch.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine();
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message + Environment.NewLine + e.InnerException.StackTrace);
                }
                else
                {
                    Console.WriteLine(e.Message + Environment.NewLine + e.StackTrace);
                }
            }
        }
    }
}
