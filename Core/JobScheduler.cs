using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobRunner.Core
{
    public class JobScheduler(
        IEnumerable<IJobTask> tasks,
        IEnumerable<JobSchedule> schedules,
        ILogger<JobScheduler> logger
    ) : BackgroundService
    {
        private readonly IEnumerable<IJobTask> _tasks = tasks;
        private readonly IEnumerable<JobSchedule> _schedules = schedules;
        private readonly ILogger<JobScheduler> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var runningJobs = new List<Task>();

            foreach (var schedule in _schedules)
            {
                var task = _tasks.FirstOrDefault(t => t.Name == schedule.JobName);
                if (task == null)
                {
                    _logger.LogWarning("No task found for job: {JobName}", schedule.JobName);
                    continue;
                }

                _logger.LogInformation("Scheduling {JobName} every {Interval}", task.Name, schedule.Interval);
                runningJobs.Add(RunOnIntervalAsync(task, schedule, stoppingToken));
            }

            await Task.WhenAll(runningJobs);
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
                    _logger.LogInformation("Executing {JobName}", schedule.JobName);
                    await task.ExecuteAsync(context, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing {JobName}", task.Name);
                }

                await Task.Delay(schedule.Interval, token);
            }
        }
    }
}