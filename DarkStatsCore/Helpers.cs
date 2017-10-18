using System;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using DarkStatsCore.Data;
using DarkStatsCore.Data.Models;
using DarkStatsCore.Models;

namespace DarkStatsCore
{
    public static class Helpers
    {
        public static string BytesToString(this long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        public static string BytesToBitsPsToString(this long bytes, TimeSpan ts)
        {
            double bits = bytes.BytesToBitsPs(ts);
            string[] Suffix = { "bps", "Kbps", "Mbps", "Gbps", "Tbps" };
            int i;
            double dblSBits = bits;
            for (i = 0; i < Suffix.Length && bits >= 1024; i++, bits /= 1024)
            {
                dblSBits = bits / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSBits, Suffix[i]);
        }

        public static double BytesToBitsPs(this long bytes, TimeSpan ts)
        {
            return bytes * 8 / ts.TotalSeconds;
        }

        public static string GetSpeedForMonth(this DarkStatsDbContext context, int month, int year)
        {
			var traffic = context.TrafficStats
                                 .Where(t => t.Day.Month == month && t.Day.Year == year);
			return traffic.Sum(t => t.In + t.Out)
				.BytesToBitsPsToString(traffic.Max(t => t.Day).Subtract(traffic.Min(t => t.Day)));
        }

        public static IEnumerable<DayDataModel> GetDayData(this DarkStatsDbContext context, int month, int year, int day)
        {
            var dayRequested = new DateTime(year, month, day);
            return context.TrafficStats
                        .Where(t => dayRequested == new DateTime(t.Day.Year, t.Day.Month, t.Day.Day))
                        .GroupBy(t => t.Day)
                        .Select(t => new DayDataModel
                        {
                            Hour = t.Key.ToString("h tt"),
                            TotalBytes = t.Sum(c => c.In + c.Out).BytesToString(),
                            GraphBytesIn = t.Sum(c => c.In),
                            GraphBytesOut = t.Sum(c => c.Out),
                            TopConsumers = t.OrderByDescending(c => c.Out + c.In)
                                            .Select(c => new TrafficStatsModel
                                            {
                                                Hostname = c.Hostname,
                                                Total = (c.Out + c.In).BytesToString()
                                            }).Take(3)
                        });
        }
    }
}
