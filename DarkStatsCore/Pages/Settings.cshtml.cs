using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Data;

namespace DarkStatsCore.Pages
{
    public class SettingsPageModel : PageModel
    {
        [BindProperty]
        public SettingsModel SettingsModel { get; set; }
        private readonly SettingsLib _settings;

        public SettingsPageModel(SettingsLib settings)
        {
            _settings = settings;
        }

        public void OnGet()
        {
            SettingsModel = new SettingsModel
            {
                Url = _settings.Url,
                SaveTime = _settings.SaveTime.Seconds,
                DashboardRefreshTime = _settings.DashboardRefreshTime.TotalMilliseconds
            };
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var oldSaveTime = _settings.SaveTime;
            _settings.SetSaveTime(SettingsModel.SaveTime);
            _settings.SetDashboardRefreshTime(SettingsModel.DashboardRefreshTime);
            _settings.SetUrl(SettingsModel.Url);
            if (oldSaveTime != _settings.SaveTime)
            {
                Scraper.StartScrapeTask(_settings.SaveTime, _settings.Url);
            }
            return RedirectToPage("/Index");
        }
    }
}