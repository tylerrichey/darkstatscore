using System;
namespace DarkStatsCore.Data.Models
{
    public class HostDelta
    {
        public string Hostname { get; set; }
        public string Ip { get; set; }
        public double LastCheckTotalBytes { get; set; }
        public double LastCheckDeltaBytes { get; set; }
    }
}
