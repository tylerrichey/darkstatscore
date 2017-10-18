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
    public class ViewDayModel : PageModel
    {
        private readonly DarkStatsDbContext _context;
        public IEnumerable<DayDataModel> DayData = new List<DayDataModel>();
        public bool DaySelected = false;
        public DateTime MinDay => _context.TrafficStats.Min(t => t.Day);
        public DateTime MaxDay => _context.TrafficStats.Max(t => t.Day);

        public ViewDayModel(DarkStatsDbContext darkStatsDbContext)
        {
            _context = darkStatsDbContext;
            _context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
        }

        public void OnGet(DateTime day)
        {
            if (day != DateTime.MinValue)
            {
                DaySelected = true;
                DayData = _context.GetDayData(day.Month, day.Year, day.Day);
            }
        }
    }
}
