using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;

namespace DarkStatsCore.Data
{
    public static class DataGatherTask
    {
        private static Task _dataGatherTask;
        private static CancellationTokenSource _cancellationTokenSource;

        public static DataSource DataSource;
        public static DateTime LastGathered => DataSource == null ? DateTime.MinValue : DataSource.GetLastGathered();
        public static double ScrapeTimeAvg => DataSource == null ? 0 : DataSource.GetScrapeTimeAvg();
        public static EventHandler ScrapeSaved;

        public static void StartDataGatherTask(TimeSpan saveTime, string url)
        {
            if (_dataGatherTask != null)
            {
                _cancellationTokenSource.Cancel();
            }
            _cancellationTokenSource = new CancellationTokenSource();
            DataSource = DataSource.GetForUrl(url);
            _dataGatherTask = Task.Run(() => DataSource.GatherDataTask(saveTime, _cancellationTokenSource.Token));
            DnsService.Start();
        }
    }
}
