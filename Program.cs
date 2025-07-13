using JobRunner.Core;
using JobRunner.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        var jobList = configuration.GetSection("Jobs").Get<List<JobSchedule>>() ?? [];

        services.AddSingleton<IEnumerable<JobSchedule>>(jobList);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("Startup");

        if (jobList.Count == 0)
        {
            logger.LogWarning("No jobs found in configuration!");
        }
        else
        {
            logger.LogInformation("Loaded {Count} job(s): {Jobs}",
                jobList.Count,
                string.Join(", ", jobList.Select(j => $"{j.JobName} ({j.Interval})")));
        }

        // Register job implementations
        services.AddSingleton<IJobTask, PingJob>();
        services.AddSingleton<IJobTask, DiskCleanupJob>();

        // Register background scheduler
        services.AddHostedService<JobScheduler>();
    })
    .Build();

await host.RunAsync();