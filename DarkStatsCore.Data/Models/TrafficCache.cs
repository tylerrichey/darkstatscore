using System;
using System.Collections.Generic;
using System.Text;

namespace DarkStatsCore.Data.Models
{
    public class TrafficCache
    {
        public string Ip { get; set; }
        public long In { get; set; }
        public long Out { get; set; }
        public int HourAdded { get; set; }
    }
}
