using System;
namespace DarkStatsCore.Models
{
    public class DashboardModel
    {
        public string CurrentHour { get; set; }
        public string CurrentMonth { get; set; }
        public string LastGathered { get; set; }
        public double ScrapeTimeAvg { get; set; }
        public double CurrentMonthTotalIn { get; set; }
        public double CurrentMonthTotalOut { get; set; }
        public double CurrentDayTotalIn { get; set; }
        public double CurrentDayTotalOut { get; set; }
        public string CurrentMonthString => DateTime.Now.ToString("MMMM, yyyy");
    }
}
