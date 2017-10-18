using System;
using System.Collections.Generic;
using DarkStatsCore.Models;

public class DayDataModel
{
    public string Hour { get; set; }
    public string TotalBytes { get; set; }
    public double GraphBytesIn { get; set; }
    public double GraphBytesOut { get; set; }
    public IEnumerable<TrafficStatsModel> TopConsumers { get; set; }
}