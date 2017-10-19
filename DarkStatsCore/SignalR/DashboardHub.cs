using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DarkStatsCore.Models;
using Microsoft.AspNetCore.SignalR;
using DarkStatsCore.Data;

namespace DarkStatsCore.SignalR
{
    public class DashboardHub : Hub
    {
        private readonly Dashboard _dashboard;

        public DashboardHub(Dashboard dashboard)
        {
            _dashboard = dashboard;
        }

        public override Task OnConnectedAsync()
        {
            _dashboard.AddConnected();
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _dashboard.RemoveConnected();
            return base.OnDisconnectedAsync(exception);
        }

        public IEnumerable<long> GetCurrentDeltas()
        {
            return _dashboard.GetCurrentDeltas();
        }

        public DashboardModel GetDashboard()
		{
            return _dashboard.GetCurrentDashboard();
		}
    }
}
