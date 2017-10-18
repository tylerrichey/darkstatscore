using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Data;
using DarkStatsCore.Models;

namespace DarkStatsCore.Pages
{
    public class ViewMonthModel : PageModel
    {
        public TotalsModel TrafficTotals { get; set; }
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
            TrafficStatsModel = _context.TrafficStats
                                        .Where(t => allTime || (t.Day.Month == month && t.Day.Month == month))
                                        .GroupBy(t => t.Ip)
                                        .OrderByDescending(t => t.Sum(s => s.In + s.Out))
                                        .Select(t => new TrafficStatsModel
                                        {
                                            Ip = t.First().Ip,
                                            Hostname = (string.IsNullOrEmpty(t.First().Hostname) || t.First().Hostname == "(none)") ? t.Key : t.First().Hostname,
                                            Mac = t.First().Mac,
                                            In = t.Sum(s => s.In).BytesToString(),
                                            Out = t.Sum(s => s.Out).BytesToString(),
                                            Total = (t.Sum(s => s.In) + t.Sum(s => s.Out)).BytesToString(),
                                            Tooltip = GetTooltip(t.First())
                                        });

            if (allTime)
            {
                ViewingMonthSpeed = _context.TrafficStats
                                            .Sum(t => t.In + t.Out)
                                            .BytesToBitsPsToString(_context.TrafficStats.Max(t => t.Day).Subtract(_context.TrafficStats.Min(t => t.Day)));
                TrafficTotals = new TotalsModel
                {
                    EffectiveDates = "All",
                    In = _context.TrafficStats
                                 .Sum(t => t.In)
                                 .BytesToString(),
                    Out = _context.TrafficStats
                                  .Sum(t => t.Out)
                                  .BytesToString(),
                    Total = _context.TrafficStats
                                    .Sum(t => t.In + t.Out)
                                    .BytesToString()
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
            else
            {
                ViewingMonthSpeed = _context.GetSpeedForMonth(month, year);
                var traffic = _context.TrafficStats
                                      .Where(t => t.Day.Month == month && t.Day.Year == year);
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
                                   Date = t.First().Day.ToString("M/d"),
                                   In = Math.Round(t.Sum(g => g.In) / 1024.0 / 1024.0 / 1024.0),
                                   Out = Math.Round(t.Sum(g => g.Out) / 1024.0 / 1024.0 / 1024.0)
                               });
            }
        }

        private string GetTooltip(TrafficStats t)
        {
            var tooltip = string.Empty;
            if (!string.IsNullOrEmpty(t.Hostname) && t.Hostname != "(none)")
            {
                tooltip = "IP: " + t.Ip + "<br>";
            }
            tooltip = tooltip + "MAC: " + t.Mac + "<br>Last Seen: " + t.LastSeen;
            return tooltip;
        }
    }
}
