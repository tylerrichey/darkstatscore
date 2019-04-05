using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DarkStatsCore.Data.Models;
using Serilog;
using System.Net.Http;

namespace DarkStatsCore.Data
{
    public static class DashboardGatherTask
    {
        public static EventHandler<DashboardEventArgs> DataGathered;
        private static Task _dashboardDataTask;
        private static CancellationTokenSource _cancellationToken;
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static List<(string clientId, EventHandler<DashboardEventArgs> dashEvent, EventHandler saveEvent)> _subs = 
            new List<(string clientId, EventHandler<DashboardEventArgs> dashEvent, EventHandler saveEvent)>();

        public static void StartDashboardGatherTask(TimeSpan refreshTime, string clientId, EventHandler<DashboardEventArgs> dashEvent, EventHandler saveEvent)
        {
            _lock.Wait();
            _subs.Add((clientId, dashEvent, saveEvent));
            DataGathered += dashEvent;
            DataGatherTask.ScrapeSaved += saveEvent;
            if (_dashboardDataTask == null)
            {
                Log.Information("Starting dashboard...");
                _cancellationToken = new CancellationTokenSource();
                _dashboardDataTask = DataGatherTask.DataSource.DashboardDataTask(refreshTime, _cancellationToken.Token);
            }
            _lock.Release();
        }

        public static void StopDashboardGatherTask(string clientId)
        {
            _lock.Wait();
            if (_subs.Count == 1)
            {
                _cancellationToken.Cancel();
                _dashboardDataTask = null;
                Log.Information("Dashboard stopped.");
            }
            var handlers = _subs.Find(s => s.clientId == clientId);
            DataGathered -= handlers.dashEvent;
            DataGatherTask.ScrapeSaved -= handlers.saveEvent;
            _subs.RemoveAll(s => s.clientId == clientId);
            _lock.Release();
        }
    }
}