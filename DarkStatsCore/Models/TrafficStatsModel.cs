using System;
namespace DarkStatsCore.Models
{
    public class TrafficStatsModel
    {
        public string Ip { get; set; }
		public string Hostname { get; set; }
		public string Mac { get; set; }
		public string In { get; set; }
		public string Out { get; set; }
        public string Total { get; set; }
		public string LastSeen { get; set; }
		public string Day { get; set; }
        public int Hour { get; set; }
        public string Tooltip { get; set; }
    }
}
