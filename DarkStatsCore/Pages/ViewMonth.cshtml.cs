using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Data;
using DarkStatsCore.Models;
using StackExchange.Profiling;

namespace DarkStatsCore.Pages
{
    public class ViewMonthModel : PageModel
    {
        public TotalsModel TrafficTotals { get; internal set; }
        public IEnumerable<TrafficStatsModel> TrafficStatsModel;
        public IEnumerable<TotalGraphModel> Graph;
        public string ViewingMonthSpeed;
        private readonly DarkStatsDbContext _context;

        public ViewMonthModel(DarkStatsDbContext darkStatsDbContext)
        {
            _context = darkStatsDbContext;
            _context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        }

        public void OnGet(int month = 0, int year = 0, bool allTime = false)
        {
            using (MiniProfiler.Current.Step("Get Hosts"))
            {
                TrafficStatsModel = _context.TrafficStats
                                            .Where(t => allTime || (t.Day.Month == month && t.Day.Month == month))
                                            .GroupBy(t => (string.IsNullOrEmpty(t.Hostname) || t.Hostname == "(none)") ? t.Ip : t.Hostname)
                                            .OrderByDescending(t => t.Sum(s => s.In + s.Out))
                                            .Select(t => new TrafficStatsModel
                                            {
                                                Hostname = t.Key,
                                                In = t.Sum(s => s.In).BytesToString(),
                                                Out = t.Sum(s => s.Out).BytesToString(),
                                                Total = (t.Sum(s => s.In) + t.Sum(s => s.Out)).BytesToString(),
                                                Tooltip = GetTooltip(t)
                                            });
            }

            if (allTime)
            {
                using (MiniProfiler.Current.Step("Get All Time"))
                {
                    var totalIn = _context.TrafficStats.Sum(t => t.In);
                    var totalOut = _context.TrafficStats.Sum(t => t.Out);
                    ViewingMonthSpeed = (totalIn + totalOut).BytesToBitsPsToString(_context.TrafficStats.Max(t => t.Day).Subtract(_context.TrafficStats.Min(t => t.Day)));
                    TrafficTotals = new TotalsModel
                    {
                        EffectiveDates = "All",
                        In = totalIn.BytesToString(),
                        Out = totalOut.BytesToString(),
                        Total = (totalIn + totalOut).BytesToString()
                    };

                    Graph = _context.TrafficStats
                                    .OrderBy(t => t.Day)
                                    .GroupBy(t => t.Day.Month)
                                    .Select(t => new TotalGraphModel
                                    {
                                        Date = t.First().Day.ToString("M/yy"),
                                        In = Math.Round(t.Sum(g => g.In) / 1024.0 / 1024.0 / 1024.0),
                                        Out = Math.Round(t.Sum(g => g.Out) / 1024.0 / 1024.0 / 1024.0)
                                    });
                }
            }
            else
            {
                using (MiniProfiler.Current.Step("Get Month"))
                {
                    var traffic = _context.TrafficStats
                                          .Where(t => t.Day.Month == month && t.Day.Year == year)
                                          .ToList();
                    ViewingMonthSpeed = traffic.Sum(t => t.In + t.Out)
                        .BytesToBitsPsToString(traffic.Max(t => t.Day).Subtract(traffic.Min(t => t.Day)));

                    TrafficTotals = new TotalsModel
                    {
                        EffectiveDates = new DateTime(year, month, 1).ToString("MMMM, yyyy"),
                        In = traffic.Sum(t => t.In)
                                    .BytesToString(),
                        Out = traffic.Sum(t => t.Out)
                                     .BytesToString(),
                        Total = traffic.Sum(t => t.In + t.Out)
                                       .BytesToString()
                    };

                    Graph = traffic.OrderBy(t => t.Day)
                                   .GroupBy(t => t.Day.Day)
                                   .Select(t => new TotalGraphModel
                                   {
                                       Date = t.First().Day.ToString("dd"),
                                       In = Math.Round(t.Sum(g => g.In) / 1024.0 / 1024.0 / 1024.0),
                                       Out = Math.Round(t.Sum(g => g.Out) / 1024.0 / 1024.0 / 1024.0)
                                   });
                }
            }
        }

        private string GetTooltip(IEnumerable<TrafficStats> trafficStats)
        {
            var tooltip = string.Empty;
            if (trafficStats.Any(t => !string.IsNullOrEmpty(t.Hostname) && t.Hostname != "(none)"))
            {
                tooltip = "IP: " + string.Join(", ", trafficStats.Select(t => t.Ip).Distinct()) + "<br>";
            }
            tooltip = tooltip + "MAC: " + string.Join(", ", trafficStats.Select(t => t.Mac).Distinct()) + "<br>Last Seen: " + trafficStats.Last().LastSeen;
            return tooltip;
        }
    }
}
