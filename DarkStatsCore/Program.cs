using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using DarkStatsCore.Data;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace DarkStatsCore
{
    public class Program
    {
        private static string _listenUrl = "http://*:6677";
        
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code, standardErrorFromLevel: LogEventLevel.Warning)
                .CreateLogger();
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
                        ScrapeTask.StartScrapeTask(settings.SaveTime, settings.Url);
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
