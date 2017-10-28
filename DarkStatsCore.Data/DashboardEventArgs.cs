using System;
using System.Collections.Generic;
using DarkStatsCore.Data.Models;

namespace DarkStatsCore.Data
{
    public class DashboardEventArgs : EventArgs
    {
        private readonly List<HostDelta> _hostDeltas;
        private readonly double _elapsedMs;
        public List<HostDelta> HostDeltas => _hostDeltas;
        public double ElapsedMs => _elapsedMs;
        public DashboardEventArgs(List<HostDelta> hostDeltas, double elapsedMs)
        {
            _elapsedMs = elapsedMs;
            _hostDeltas = hostDeltas;
        }
    }
}
