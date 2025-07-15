using JobRunner.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JobRunner.Utils;

namespace JobRunner.Core
{
    public class JobScheduler(
        IEnumerable<IJobTask> tasks,
        IEnumerable<JobSchedule> schedules,
        IConfiguration configuration,
        ILogger<JobScheduler> logger
    ) : BackgroundService
    {
        private readonly IEnumerable<IJobTask> _tasks = tasks;
        private readonly IEnumerable<JobSchedule> _schedules = schedules;
        private readonly ILogger<JobScheduler> _logger = logger;
        private readonly bool _runAllJobsInPreview = configuration.GetValue<bool>("RunAllJobsInPreview");

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚦 Job scheduler starting...");

            if (!_schedules.Any())
            {
                _logger.LogWarning("⚠️ No jobs configured.");
                return;
            }

            LogConfiguredJobs();

            foreach (var schedule in _schedules.Where(s => s.Enabled))
            {
                var task = _tasks.FirstOrDefault(t => t.Name == schedule.JobName);
                if (task == null)
                {
                    _logger.LogWarning("❓ No task found for job: {JobName}", schedule.JobName);
                    continue;
                }

                var isPreview = schedule.IsPreview || _runAllJobsInPreview;
                var context = new JobContext
                {
                    Logger = _logger,
                    Parameters = schedule.Parameters
                };

                if (task is IPreviewableJob previewable && isPreview)
                {
                    _ = Task.Run(() => previewable.PreviewAsync(context, stoppingToken), stoppingToken);
                }
                else
                {
                    _ = RunOnIntervalAsync(task, schedule, context, stoppingToken);
                }
            }

            await Task.CompletedTask;
        }

        private void LogConfiguredJobs()
        {
            _logger.LogInformation("📦 Jobs configured:");
            var count = _schedules.Count();

            for (int i = 0; i < count; i++)
            {
                var schedule = _schedules.ElementAt(i);
                var isLast = i == count - 1;
                var prefix = isLast ? "└─" : "├─";
                var status = schedule.Enabled ? "✅" : "❌";
                var preview = schedule.IsPreview || _runAllJobsInPreview ? " 🧪" : "";

                _logger.LogInformation(" {Prefix} {Status} {JobName}{Preview} ⏱️ Every {Interval}",
                    prefix, status, schedule.JobName, preview, schedule.Interval);
            }
        }

        private async Task RunOnIntervalAsync(IJobTask task, JobSchedule schedule, JobContext context, CancellationToken token)
        {
            var jobIcon = GetJobIcon(schedule.JobName);
            var outputBuffer = new StringWriter();
            var logCapture = context.Logger;

            // Replace the logger with one that writes to a buffer (if you want cleaner batching)
            var bufferedLogger = new BufferedLogger(logCapture, outputBuffer);

            var runContext = new JobContext
            {
                Logger = bufferedLogger,
                Parameters = context.Parameters
            };

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await task.ExecuteAsync(runContext, token);
                    var jobOutput = outputBuffer.ToString().Trim();

                    if (!string.IsNullOrEmpty(jobOutput))
                    {
                        _logger.LogInformation("{Icon} {JobName} run result:\n{Output}", jobIcon, schedule.JobName, jobOutput);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error executing {JobName}", schedule.JobName);
                }

                outputBuffer.GetStringBuilder().Clear(); // Clear between runs
                await Task.Delay(schedule.Interval, token);
            }
        }

        private static string GetJobIcon(string jobName) => jobName switch
        {
            "PingJob" => "📡",
            "DiskCleanupJob" => "🧹",
            "GitHubStarTrackerJob" => "⭐",
            "ApplicationResourceMonitorJob" => "📊",
            _ => "🧩"
        };
    }
}