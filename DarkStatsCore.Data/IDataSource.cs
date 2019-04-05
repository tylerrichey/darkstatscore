using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DarkStatsCore.Data
{
    public interface IDataSource
    {
        DateTime GetLastGathered();
        double GetScrapeTimeAvg();
        Task GatherDataTask(TimeSpan saveTime, CancellationToken cancellationToken);
        Task DashboardDataTask(TimeSpan refreshTime, CancellationToken cancellationToken);
        ValidationResult Test();
        List<long> GetDashboardDeltas();
    }
}
