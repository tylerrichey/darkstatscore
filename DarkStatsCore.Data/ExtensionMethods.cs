using System;
using System.Linq;
using System.Collections.Generic;
using DarkStatsCore.Data.Models;

namespace DarkStatsCore.Data
{
    public static class ExtensionMethods
    {
        public static int ToInt32(this double dbl) => Convert.ToInt32(dbl);
        public static void AdjustScrapeDeltas(this List<TrafficStats> stats, List<HostDelta> hostDeltas)
        {
            stats.ForEach(s =>
            {
                var hostDelta = hostDeltas.Find(d => d.Ip == s.Ip) ?? new HostDelta { Ip = s.Ip };
                hostDelta.Hostname = s.Hostname;
                if (hostDelta.LastCheckTotalBytes > 0)
                {
                    hostDelta.LastCheckDeltaBytes = (s.In + s.Out) - hostDelta.LastCheckTotalBytes;
                }
                hostDelta.LastCheckTotalBytes = s.In + s.Out;
                if (hostDeltas.Find(d => d.Ip == s.Ip) == null)
                {
                    hostDeltas.Add(hostDelta);
                }
            });
        }

        public static string ToShortHandString(this TimeSpan timeSpan)
        {
            var ts = timeSpan.TotalSeconds;
            var secs = Convert.ToInt32(ts % 60);
            var mins = Convert.ToInt32((ts / 60) % 60);
            var hrs = Convert.ToInt32((ts / 3600) % 24);
            var days = Convert.ToInt32(ts / 86400);
            var showZeroes = false;
            var result = string.Empty;

            if (days > 0)
            {
                result = days + (days == 1 ? " day" : " days");
                showZeroes = true;
            }
            if (showZeroes || hrs > 0)
            {
                result += (showZeroes ? ", " : "") + hrs + (hrs == 1 ? " hr" : " hrs");
                showZeroes = true;
            }
            if (showZeroes || mins > 0)
            {
                result += (showZeroes ? ", " : "") + mins + (mins == 1 ? " min" : " mins");
                showZeroes = true;
            }
            result += (showZeroes ? ", " : "") + secs + (secs == 1 ? " sec" : " secs");

            return result;
        }

        public static TrafficStats ToTrafficStats(this KeyValuePair<string, DscsModel> kvp)
        {
            var lastSeen = kvp.Value.LastSeen;
            return new TrafficStats
            {
                Ip = kvp.Key,
                Hostname = DnsService.GetHostName(kvp.Key),
                Mac = kvp.Value.Mac,
                In = kvp.Value.In,
                Out = kvp.Value.Out,
                LastSeen = DateTime.Now.Subtract(lastSeen).ToShortHandString(),
                Day = new DateTime(lastSeen.Year, lastSeen.Month, lastSeen.Day, lastSeen.Hour, 0, 0)
            };
        }

        public static IEnumerable<TrafficStats> ToTrafficStats(this Dictionary<string, DscsModel> dict) => dict.Select(ToTrafficStats);

        public static List<HostDelta> ToHostDeltas(this Dictionary<string, DscsModel> dict) =>
            dict.Select(d => new HostDelta
            {
                Hostname = DnsService.GetHostName(d.Key),
                Ip = d.Key,
                LastCheckDeltaBytes = (d.Value.Out + d.Value.In)
            })
            .ToList();
    }
}