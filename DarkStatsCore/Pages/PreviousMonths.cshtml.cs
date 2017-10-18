using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Models;
using DarkStatsCore.Data;

namespace DarkStatsCore.Pages
{
    public class PreviousMonthsModel : PageModel
    {
        private readonly DarkStatsDbContext _context;
        public PreviousMonthsModel(DarkStatsDbContext darkStatsDbContext)
        {
            _context = darkStatsDbContext;
        }

        public IEnumerable<DateTime> PreviousMonths => _context.TrafficStats
                                                               .Select(t => new DateTime(t.Day.Year, t.Day.Month, 1))
                                                               .Distinct();

        public void OnGet()
        {
            _context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        }
    }
}
