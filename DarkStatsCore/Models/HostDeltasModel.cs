using System;
using System.Collections.Generic;

namespace DarkStatsCore.Models
{
    public class HostDeltasModel
    {
        public IEnumerable<HostDeltaModel> Deltas { get; set; }
        public double ElapsedMs { get; set; }
    }
}