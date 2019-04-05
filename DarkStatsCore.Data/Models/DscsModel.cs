using System;

namespace DarkStatsCore.Data.Models
{
    public class DscsModel
    {
        public int In { get; set; }
        public int Out { get; set; }
        public string Mac { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
