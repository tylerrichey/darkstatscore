using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DarkStatsCore.Data;
using Serilog.Events;

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
                SaveTime = (int)_settings.SaveTime.TotalSeconds,
                DashboardRefreshTime = _settings.DashboardRefreshTime.TotalMilliseconds,
                DisplayMiniProfiler = Program.DisplayMiniProfiler
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
            Program.DisplayMiniProfiler = SettingsModel.DisplayMiniProfiler;
            Program.LoggingLevel.MinimumLevel = SettingsModel.DebugLogging ? LogEventLevel.Debug : LogEventLevel.Information;
            if (oldSaveTime != _settings.SaveTime || DataGatherTask.DataSource == null)
            {
                DataGatherTask.StartDataGatherTask(_settings.SaveTime, _settings.Url);
            }
            return RedirectToPage("/Settings");
        }
    }
}