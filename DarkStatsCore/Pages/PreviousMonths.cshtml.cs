using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Models;
using DarkStatsData;

namespace DarkStatsCore.Pages
{
	public class IndexModel : PageModel
	{
		private readonly DarkStatsDbContext _context;
		public IndexModel(DarkStatsDbContext darkStatsDbContext)
		{
			_context = darkStatsDbContext;
		}

		public IEnumerable<TrafficStatsModel> TrafficStatsModel => _context.TrafficStats
							   .GroupBy(t => t.Ip)
							   .OrderByDescending(t => t.Sum(s => s.In + s.Out))
							   .Select(t => new TrafficStatsModel
							   {
								   Ip = t.Key,
								   Hostname = t.First().Hostname,
								   Mac = t.First().Mac,
								   In = t.Sum(s => s.In).BytesToString(),
								   Out = t.Sum(s => s.Out).BytesToString(),
								   Total = (t.Sum(s => s.In) + t.Sum(s => s.Out)).BytesToString()
							   });

		public TotalsModel TrafficTotals { get; set; }

		public string LastGathered => Program.LastGathered.ToString("MM/dd/yyyy hh:mm tt");

		public void OnGet(int month = 0, int year = 0)
		{
			_context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

			if (month == 0 && year == 0)
			{
				TrafficTotals = new TotalsModel
				{
					EffectiveDates = "All",
					In = _context.TrafficStats.Sum(t => t.In).BytesToString(),
					Out = _context.TrafficStats.Sum(t => t.Out).BytesToString(),
					Total = _context.TrafficStats.Sum(t => t.In + t.Out).BytesToString()
				};
			}
			else
			{
				var traffic = _context.TrafficStats.Where(t => t.Day.Month == month && t.Day.Year == year);
				TrafficTotals = new TotalsModel
				{
					EffectiveDates = new DateTime(year, month, 1).ToString("MMMM, yyyy"),
					In = traffic.Sum(t => t.In).BytesToString(),
					Out = traffic.Sum(t => t.Out).BytesToString(),
					Total = traffic.Sum(t => t.In + t.Out).BytesToString()
				};
			}
		}
	}
}
