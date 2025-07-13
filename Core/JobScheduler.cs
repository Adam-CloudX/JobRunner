using JobRunner.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

            foreach (var schedule in _schedules)
            {
                if (!schedule.Enabled)
                {
                    _logger.LogInformation("⏭️  Skipping disabled job: {JobName}", schedule.JobName);
                    continue;
                }

                var task = _tasks.FirstOrDefault(t => t.Name == schedule.JobName);
                if (task == null)
                {
                    _logger.LogWarning("❌ No task found for job: {JobName}", schedule.JobName);
                    continue;
                }

                var context = new JobContext
                {
                    Logger = _logger,
                    Parameters = schedule.Parameters
                };

                bool isPreview = schedule.IsPreview || _runAllJobsInPreview;

                if (task is IPreviewableJob previewJob && isPreview)
                {
                    _logger.LogInformation("📝 Preview mode enabled for: {JobName}", schedule.JobName);
                    _ = Task.Run(() => previewJob.PreviewAsync(context, stoppingToken), stoppingToken);
                }
                else
                {
                    _logger.LogInformation("⏰ Scheduling job: {JobName} every {Interval}", schedule.JobName, schedule.Interval);
                    _ = RunOnIntervalAsync(task, schedule, stoppingToken);
                }
            }

            await Task.CompletedTask;
        }

        private async Task RunOnIntervalAsync(IJobTask task, JobSchedule schedule, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var context = new JobContext
                {
                    Logger = _logger,
                    Parameters = schedule.Parameters
                };

                try
                {
                    _logger.LogInformation("✅ Executing {JobName}", schedule.JobName);
                    await task.ExecuteAsync(context, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error executing {JobName}", task.Name);
                }

                await Task.Delay(schedule.Interval, token);
            }
        }
    }
}