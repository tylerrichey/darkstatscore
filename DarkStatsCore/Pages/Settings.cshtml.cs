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
        public string VersionInformation { get; internal set; }
        private readonly SettingsLib _settings;

        public SettingsPageModel(SettingsLib settings)
        {
            _settings = settings;
            if (!string.IsNullOrEmpty(_settings.Branch))
            {
                VersionInformation = "Docker Version - Branch: " + _settings.Branch + " Commit: " + _settings.Commit + " Image: " + _settings.ImageName;
            }
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

            _settings.SetSaveTime(SettingsModel.SaveTime);
            _settings.SetDashboardRefreshTime(SettingsModel.DashboardRefreshTime);
            _settings.SetUrl(SettingsModel.Url);
            Scraper.StartScrapeTask(_settings.SaveTime, _settings.Url);
            return RedirectToPage("/Index");
        }
    }
}