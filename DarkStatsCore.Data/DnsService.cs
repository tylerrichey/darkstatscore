using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace DarkStatsCore.Data
{
    class DnsService
    {
        private static Task _dnsTask;
        private static DarkStatsDbContext _context = new DarkStatsDbContext();
        private static ConcurrentDictionary<string, string> _hosts = new ConcurrentDictionary<string, string>();

        public static void Start() => _dnsTask = DnsTask();

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
                var currentHour = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, 0, 0);
                var lookups = _context.TrafficStats
                        .Where(t => t.Day == currentHour && t.Ip != "::" && (string.IsNullOrEmpty(t.Hostname) || t.Hostname == "(none)"))
                        .Select(t => TryGetHostName(t.Ip));
                await Task.WhenAll(lookups);
                await Task.Delay(TimeSpan.FromMinutes(15));
            }
        }
        
        private static async Task TryGetHostName(string ipString)
        {
            var log = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss") + " - Attempting DNS lookup on " + ipString + "... ";
            IPAddress ip;
            if (!IPAddress.TryParse(ipString, out ip))
            {
                return;
            }
            try
            {
                var host = await System.Net.Dns.GetHostEntryAsync(ip);
                var hostName = host.HostName;
                if (!string.IsNullOrEmpty(hostName))
                {
                    log = log + "Success.";
                    _hosts.AddOrUpdate(ipString, hostName, (key, value) => value = hostName);
                }
                else
                {
                    log = log + "Not found.";
                }
            }
            catch (Exception e)
            {
                log = log + e.Message;
            }
            finally
            {
                Console.WriteLine(log);
            }
        }
    }
}
