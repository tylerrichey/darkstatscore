using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DarkStatsCore.Data
{
    public abstract class DataSource : IDataSource
    {
        protected string _url;

        protected DataSource(string url)
        {
            _url = url;
        }

        public static DataSource GetForUrl(string url)
        {
            if (url.StartsWith("dscs://"))
            {
                return new DscsDataSource(url);
            }
            else
            {
                return new ScrapeDataSource(url);
            }
        }

        public abstract Task DashboardDataTask(TimeSpan refreshTime, CancellationToken cancellationToken);
        public abstract Task GatherDataTask(TimeSpan saveTime, CancellationToken cancellationToken);
        public abstract DateTime GetLastGathered();
        public abstract double GetScrapeTimeAvg();
        public abstract ValidationResult Test();

        public abstract List<long> GetDashboardDeltas();
    }
}
