using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Models;
using DarkStatsCore.Data;

namespace DarkStatsCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly SettingsLib _settings;
        public double SaveTime { get; internal set; }

        public IndexModel(SettingsLib settings)
        {
            _settings = settings;
            SaveTime = _settings.SaveTime.TotalMilliseconds;
        }

        public IActionResult OnGet()
        {
            if (_settings.InvalidSettings)
            {
                return RedirectToPage("/Settings");
            }
            return Page();
        }
    }
}
