using System;
using System.Linq;
using System.Reactive.Linq;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DarkStatsCore.Data;
using DarkStatsCore.Models;

namespace DarkStatsCore.SignalR
{
    public class Dashboard
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private bool _shouldStream => _numConnected > 0;
        private int _numConnected = 0;
        private DateTime CurrentHour => new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
        private readonly DarkStatsDbContext _context;
        private readonly SettingsLib _settings;
        private IHubContext<DashboardHub> _clients;
        private CancellationTokenSource _cancellationTokenSource;

        public Dashboard(DarkStatsDbContext darkStatsDbContext, SettingsLib settings, IHubContext<DashboardHub> clients)
        {
            _context = darkStatsDbContext;
            _settings = settings;
            _clients = clients;
            DashboardScrape.DataGathered += DataGatheredEvent;
            Scraper.ScrapeSaved += ScrapeSavedEvent;
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public void AddConnected()
        {
            _lock.Wait();
            try
            {
                _numConnected++;
                if (!DashboardScrape.IsTaskActive)
                {
                    _cancellationTokenSource = new CancellationTokenSource();
                    DashboardScrape.StartDashboardScrapeTask(_settings.Url, _settings.DashboardRefreshTime, _cancellationTokenSource.Token);
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void RemoveConnected()
        {
            _lock.Wait();
            try
            {
                _numConnected--;
                if (_numConnected == 0)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            finally
            {
                _lock.Release();
            }
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
            _clients.Clients.All.InvokeAsync("GetCurrentDashboard", GetDashboardModel().Result).Wait();
        }

        private void DataGatheredEvent(object sender, DashboardEventArgs e)
        {
            _clients.Clients.All.InvokeAsync("GetLiveDeltas", new HostDeltasModel
            {
                ElapsedMs = e.ElapsedMs,
                Deltas = e.HostDeltas.Select(h => new HostDeltaModel
                {
                    Hostname = (string.IsNullOrEmpty(h.Hostname) || h.Hostname == "(none)") ? h.Ip : h.Hostname,
                    Speed = h.LastCheckDeltaBytes
                })
            }).Wait();
        }

        private async Task<DashboardModel> GetDashboardModel()
        {
            var dashboard = new DashboardModel
            {
                LastGathered = Scraper.LastGathered.ToString("MM/dd/yyyy hh:mm:ss tt"),
                ScrapeTimeAvg = Scraper.ScrapeTimeAvg
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
            var monthinout = (long) (dashboard.CurrentMonthTotalIn + dashboard.CurrentDayTotalOut);
            dashboard.CurrentMonth = monthinout.BytesToBitsPsToString(DateTime.Now.Subtract(traffic.Min(t => t.Day)));
            var today = traffic.Where(t => t.Day.Day == DateTime.Now.Day);
            dashboard.CurrentDayTotalIn = today.Sum(t => t.In);
            dashboard.CurrentDayTotalOut = today.Sum(t => t.Out);
            return dashboard;
        }
    }
}
