using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Serilog;

namespace DarkStatsCore.Data
{
    class DnsService
    {
        private static Task _dnsTask;
        private static DarkStatsDbContext _context = new DarkStatsDbContext();
        private static ConcurrentDictionary<string, string> _hosts = new ConcurrentDictionary<string, string>();

        public static void Start()
        {
            if (_dnsTask == null)
            {
                _dnsTask = DnsTask();
            }
        }

        public static void GetHostName(TrafficStats trafficStats)
        {
            string hostName;
            _hosts.TryGetValue(trafficStats.Ip, out hostName);
            trafficStats.Hostname = string.IsNullOrEmpty(hostName) ? trafficStats.Hostname : hostName;
        }

        public static string GetHostName(string ip, string hostName)
        {
            var _hostName = hostName;
            _hosts.TryGetValue(ip, out hostName);
            return hostName ?? _hostName;
        }

        private static async Task DnsTask()
        {
            while (true)
            {
                Log.Information("Running DNS service...");
                var currentHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                var lookups = await _context.TrafficStats
                        .Where(t => t.Day == currentHour && t.Ip != "::")
                        .Select(t => TryGetHostName(t.Ip))
                        .ToListAsync();
                await Task.WhenAll(lookups);
                Log.Information("DNS service complete; Successful lookups: {Successful} - Failures: {Failures}", lookups.Where(l => l.Result).Count(), lookups.Where(l => !l.Result).Count());
                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }
        
        private static async Task<bool> TryGetHostName(string ipString)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(ipString, out ip))
            {
                return false;
            }
            try
            {
                var host = await Dns.GetHostEntryAsync(ip);
                var hostName = host.HostName;
                if (!string.IsNullOrEmpty(hostName))
                {
                    _hosts.AddOrUpdate(ipString, hostName, (key, value) => value = hostName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                //ignore not finding a host
                return false;
            }
        }
    }
}
