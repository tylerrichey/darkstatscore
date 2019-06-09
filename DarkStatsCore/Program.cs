using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using DarkStatsCore.Data;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Serilog.Core;

namespace DarkStatsCore
{
    public class Program
    {
        public static bool DisplayMiniProfiler { get; internal set; }
        public static LoggingLevelSwitch LoggingLevel = new LoggingLevelSwitch();
        private static string _listenUrl = "http://*:6677";
        
        public static int Main(string[] args)
        {
            var buildLogger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LoggingLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, standardErrorFromLevel: LogEventLevel.Warning);
            var seqhost = Environment.GetEnvironmentVariable("SEQ_HOST");
            var seqkey = Environment.GetEnvironmentVariable("SEQ_KEY");
            if (!string.IsNullOrEmpty(seqhost))
            {
                buildLogger.WriteTo.Seq(seqhost, apiKey: seqkey, compact: true);
            }
            Log.Logger = buildLogger.CreateLogger();
            Serilog.Debugging.SelfLog.Enable(Console.Error);

            try
            {
                if (args.Length == 1)
                {
                    _listenUrl = args[0];
                    Log.Information("Using listen address: {ListenUrl}", _listenUrl);
                }
                Log.Information("Starting DarkStatsCore ({Version})...", SettingsLib.VersionInformation);
                using (var context = new DarkStatsDbContext())
                {
                    context.Database.Migrate();
                    var settings = new SettingsLib(context);
                    if (!settings.InvalidSettings)
                    {
                        DataGatherTask.StartDataGatherTask(settings.SaveTime, settings.Url);
                    }
                }
                BuildWebHost(args).Run();
                return 0;
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseUrls(_listenUrl)
                   .UseStartup<Startup>()
                   .UseSerilog()
                   .Build();        
    }
}
