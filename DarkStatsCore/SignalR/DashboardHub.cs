using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DarkStatsCore.Models;
using Microsoft.AspNetCore.SignalR;
using DarkStatsCore.Data;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Serilog;

namespace DarkStatsCore.SignalR
{
    public class DashboardHub : Hub
    {
        private DateTime CurrentHour => new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
        private readonly DarkStatsDbContext _context;
        private readonly SettingsLib _settings;
        private IHubContext<DashboardHub> _clients;

        public DashboardHub(DarkStatsDbContext darkStatsDbContext, SettingsLib settings, IHubContext<DashboardHub> clients)
        {
            _context = darkStatsDbContext;
            _settings = settings;
            _clients = clients;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public override Task OnConnectedAsync()
        {
            DashboardScrape.DataGathered += DataGatheredEvent;
            ScrapeTask.ScrapeSaved += ScrapeSavedEvent;
            DashboardScrape.StartDashboardScrapeTask(_settings.Url, _settings.DashboardRefreshTime);

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            DashboardScrape.StopDashboardScrapeTask();
            DashboardScrape.DataGathered -= DataGatheredEvent;
            ScrapeTask.ScrapeSaved -= ScrapeSavedEvent;
            return base.OnDisconnectedAsync(exception);
        }
        
        public async Task<DashboardModel> GetDashboard()
		{
            return await GetCurrentDashboard();
		}

        public IEnumerable<long> GetCurrentDeltas()
        {
            return Scraper.Deltas;
        }

        public async Task<DashboardModel> GetCurrentDashboard()
        {
            return await GetDashboardModel();
        }

        private void ScrapeSavedEvent(object sender, EventArgs e)
        {
            _clients.Clients.All.SendAsync("GetCurrentDashboard", GetDashboardModel().Result).Wait();
        }

        private void DataGatheredEvent(object sender, DashboardEventArgs e)
        {
            try
            {
                _clients.Clients.All.SendAsync("GetLiveDeltas", new HostDeltasModel
                {
                    ElapsedMs = e.ElapsedMs,
                    Deltas = e.HostDeltas.Select(h => new HostDeltaModel
                    {
                        Hostname = (string.IsNullOrEmpty(h.Hostname) || h.Hostname == "(none)") ? h.Ip : h.Hostname,
                        Speed = h.LastCheckDeltaBytes
                    })
                }).Wait();
            }
            catch (Exception exc)
            {
                Log.Fatal(exc, "Error pushing dashboard deltas");
            }
        }

        private async Task<DashboardModel> GetDashboardModel()
        {
            var dashboard = new DashboardModel
            {
                LastGathered = ScrapeTask.LastGathered.ToString("MM/dd/yyyy hh:mm:ss tt"),
                ScrapeTimeAvg = ScrapeTask.ScrapeTimeAvg
            };
            var traffic = await _context.TrafficStats
                                    .Where(t => t.Day.Month == DateTime.Now.Month && t.Day.Year == DateTime.Now.Year)
                                    .Select(t => new
                                    {
                                        t.Day,
                                        t.In,
                                        t.Out
                                    })
                                    .ToListAsync();
            dashboard.CurrentHour = traffic.Where(t => t.Day == CurrentHour)
                                        .Sum(t => t.In + t.Out)
                                        .BytesToBitsPsToString(DateTime.Now.Subtract(CurrentHour));
            dashboard.CurrentMonthTotalIn = traffic.Sum(t => t.In);
            dashboard.CurrentMonthTotalOut = traffic.Sum(t => t.Out);
            var monthinout = (long)(dashboard.CurrentMonthTotalIn + dashboard.CurrentDayTotalOut);
            dashboard.CurrentMonth = monthinout.BytesToBitsPsToString(DateTime.Now.Subtract(traffic.Min(t => t.Day)));
            var today = traffic.Where(t => t.Day.Day == DateTime.Now.Day);
            dashboard.CurrentDayTotalIn = today.Sum(t => t.In);
            dashboard.CurrentDayTotalOut = today.Sum(t => t.Out);
            return dashboard;
        }
    }
}
