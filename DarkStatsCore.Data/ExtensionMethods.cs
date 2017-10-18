using System;
using System.Linq;
using System.Collections.Generic;
using DarkStatsCore.Data.Models;

namespace DarkStatsCore.Data
{
    public static class ExtensionMethods
    {
        public static void AdjustDeltas(this List<TrafficStats> stats, List<HostDelta> hostDeltas)
        {
            stats.ForEach(s =>
            {
                var hostDelta = hostDeltas.FirstOrDefault(d => d.Ip == s.Ip) ?? new HostDelta { Ip = s.Ip };
                hostDelta.Hostname = s.Hostname;
                if (hostDelta.LastCheckTotalBytes > 0)
                {
                    hostDelta.LastCheckDeltaBytes = (s.In + s.Out) - hostDelta.LastCheckTotalBytes;
                }
                hostDelta.LastCheckTotalBytes = s.In + s.Out;
                if (hostDeltas.FirstOrDefault(d => d.Ip == s.Ip) == null)
                {
                    hostDeltas.Add(hostDelta);
                }
            });
        }
    }
}