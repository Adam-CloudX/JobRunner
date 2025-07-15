using JobRunner.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace JobRunner.Jobs
{
    public class ApplicationResourceMonitorJob : IPreviewableJob
    {
        public string Name => "ApplicationResourceMonitorJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: false, cancellationToken);

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: true, cancellationToken);

        private static async Task Run(JobContext context, bool preview, CancellationToken token)
        {
            var logger = context.Logger;
            var parameters = context.Parameters;
            var logBuilder = new StringBuilder();

            void LogInfo(string msg) => logBuilder.AppendLine(msg);
            void LogWarning(string msg) => logBuilder.AppendLine(msg);

            var process = Process.GetCurrentProcess();

            var usedMemoryMb = process.WorkingSet64 / (1024 * 1024);
            var cpuUsage = await GetCpuUsageForProcessAsync(process, token);

            LogInfo($"Application Memory Usage: {usedMemoryMb} MB");
            LogInfo($"Application CPU Usage: {cpuUsage:F2}%");

            if (!preview)
            {
                if (parameters.TryGetValue("MaxMemoryMB", out var memStr) &&
                    int.TryParse(memStr, out var memThreshold) &&
                    usedMemoryMb > memThreshold)
                {
                    LogWarning($"Memory usage exceeded: {usedMemoryMb} MB > {memThreshold} MB");
                }

                if (parameters.TryGetValue("MaxCpuPercent", out var cpuStr) &&
                    float.TryParse(cpuStr, out var cpuThreshold) &&
                    cpuUsage > cpuThreshold)
                {
                    LogWarning($"CPU usage exceeded: {cpuUsage:F2}% > {cpuThreshold}%");
                }
            }

            logger.LogInformation("{BatchLog}", logBuilder.ToString().TrimEnd());

            await Task.CompletedTask;
        }

        private static async Task<double> GetCpuUsageForProcessAsync(Process process, CancellationToken token)
        {
            var startTime = DateTime.UtcNow;
            var startCpu = process.TotalProcessorTime;

            // Wait 500ms to sample CPU
            await Task.Delay(500, token);

            var endTime = DateTime.UtcNow;
            var endCpu = process.TotalProcessorTime;

            var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
            var elapsedMs = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * elapsedMs) * 100;

            return cpuUsageTotal;
        }
    }
}
