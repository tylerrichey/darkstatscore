using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DarkStatsCore.Models;
using Microsoft.AspNetCore.SignalR;
using DarkStatsCore.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Serilog;
using Microsoft.AspNetCore.Http;

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
            DashboardGatherTask.StartDashboardGatherTask(_settings.DashboardRefreshTime, Context.ConnectionId, DataGatheredEvent, ScrapeSavedEvent);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            DashboardGatherTask.StopDashboardGatherTask(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
        
        public async Task<DashboardModel> GetDashboard() => await GetCurrentDashboard();

        public IEnumerable<long> GetCurrentDeltas() => DataGatherTask.DataSource.GetDashboardDeltas();

        public async Task<DashboardModel> GetCurrentDashboard() => await GetDashboardModel();

        //have to tell client to call function instead of passing data
        //as the dbcontext will be disposed of in the callback thread
        private void ScrapeSavedEvent(object sender, EventArgs e) => _clients.Clients.All.SendAsync("GetCurrentDashboard").Wait();

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
                    .OrderByDescending(h => h.Speed)
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
                LastGathered = DataGatherTask.LastGathered.ToString("MM/dd/yyyy hh:mm:ss tt"),
                ScrapeTimeAvg = DataGatherTask.ScrapeTimeAvg
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
            dashboard.CurrentMonth = traffic.Count > 0 ? monthinout.BytesToBitsPsToString(DateTime.Now.Subtract(traffic.Min(t => t.Day))) : "0";
            var today = traffic.Where(t => t.Day.Day == DateTime.Now.Day);
            dashboard.CurrentDayTotalIn = today.Sum(t => t.In);
            dashboard.CurrentDayTotalOut = today.Sum(t => t.Out);
            return dashboard;
        }
    }
}
