using JobRunner.Core;
using JobRunner.Jobs;
using JobRunner.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("🚀 Starting JobRunner...");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog()
        .ConfigureAppConfiguration(config =>
        {
            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        })
        .ConfigureServices((context, services) =>
        {
            var configuration = context.Configuration;
            var jobList = configuration.GetSection("Jobs").Get<List<JobSchedule>>() ?? [];

            services.AddSingleton<IEnumerable<JobSchedule>>(jobList);
            services.AddSingleton<IConfiguration>(configuration);

            var logger = Log.ForContext("SourceContext", "Startup");

            if (jobList.Count == 0)
            {
                logger.Warning("⚠️ No jobs found in configuration!");
            }
            else
            {
                logger.Information("📦 Loaded {Count} job(s): {Jobs}",
                    jobList.Count,
                    string.Join(", ", jobList.Select(j => $"{j.JobName} ⏱️ {j.Interval}")));
            }

            // Register job implementations
            services.AddSingleton<IJobTask, PingJob>();
            services.AddSingleton<IJobTask, DiskCleanupJob>();

            // Register background scheduler
            services.AddHostedService<JobScheduler>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Host terminated unexpectedly");
}
finally
{
    Log.Information("👋 Shutting down JobRunner.");
    Log.CloseAndFlush();
}