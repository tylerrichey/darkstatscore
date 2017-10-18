using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DarkStatsCore.Data;
using DarkStatsCore.Models;

namespace DarkStatsCore.Controllers
{
    public class ApiController : Controller
    {
        // private readonly DarkStatsDbContext _context;
        // public ApiController(DarkStatsDbContext darkStatsDbContext)
        // {
        //     _context = darkStatsDbContext;
        // }

        // public IEnumerable<double> GetDeltas()
        // {
        //     var d = Scraper.Deltas.Select(s => s.BytesToBitsPs(Scraper.TimeSpanSinceLastCheck) / 1024.0 / 1024.0);
        //     return d.Select(s => Math.Round(s, 2));
        // }

        // public DashboardModel GetDashboard()
        // {
        //     return _context.GetDashboard();
        // }
    }
}
