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

        public Dashboard(DarkStatsDbContext darkStatsDbContext, SettingsLib settings, IHubContext<DashboardHub> clients)
        {
            _context = darkStatsDbContext;
            _settings = settings;
            _clients = clients;
            DashboardScrape.DataGathered += DataGatheredEvent;
        }

        public void AddConnected()
        {
            _lock.Wait();
            try
            {
                _numConnected++;
                if (!DashboardScrape.IsTimerActive)
                {
                    DashboardScrape.StartScrapeTimer(_settings.Url, _settings.DashboardRefreshTime);
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
                    DashboardScrape.StopScrapeTimer();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public IObservable<DashboardModel> GetDashboard()
        {
            var saveTime = _settings.SaveTime;
            return Observable.Create(
                async (IObserver<DashboardModel> observer) =>
                {
                    while (_shouldStream)
                    {
                        observer.OnNext(GetDashboardModel());
                        await Task.Delay(saveTime);
                    }
                });
        }

        public IEnumerable<long> GetCurrentDeltas()
        {
            return Scraper.Deltas;
        }

        private HostDeltasModel GetLiveDeltas()
        {
            return new HostDeltasModel
            {
                ElapsedMs = DashboardScrape.ElapsedMs,
                Deltas = DashboardScrape.Deltas
                                        .Select(h => new HostDeltaModel
                                        {
                                            Hostname = (string.IsNullOrEmpty(h.Hostname) || h.Hostname == "(none)") ? h.Ip : h.Hostname,
                                            Speed = h.LastCheckDeltaBytes
                                        })
            };
        }

        private void DataGatheredEvent(object sender, EventArgs e)
        {
            _clients.Clients.All.InvokeAsync("GetLiveDeltas", GetLiveDeltas()).Wait();
        }

        private DashboardModel GetDashboardModel()
        {
            var dashboard = new DashboardModel
            {
                LastGathered = Scraper.LastGathered.ToString("MM/dd/yyyy hh:mm:ss tt"),
                ScrapeTimeAvg = Scraper.ScrapeTimeAvg
            };
			var traffic = _context.TrafficStats
                                  .Where(t => t.Day.Month == DateTime.Now.Month && t.Day.Year == DateTime.Now.Year)
                                  .Select(t => new
                                  {
                                      t.Day,
                                      t.In,
                                      t.Out
                                  })
                                  .ToList();
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
