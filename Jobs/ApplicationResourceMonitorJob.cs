using JobRunner.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace JobRunner.Jobs
{
    public class ApplicationResourceMonitorJob : IPreviewableJob
    {
        public string Name => "ApplicationResourceMonitorJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            await Run(context, preview: false, cancellationToken);
        }

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
        {
            await Run(context, preview: true, cancellationToken);
        }

        private static async Task Run(JobContext context, bool preview, CancellationToken token)
        {
            var logger = context.Logger;
            var parameters = context.Parameters;

            var process = Process.GetCurrentProcess();

            var usedMemoryMb = process.WorkingSet64 / (1024 * 1024);
            var cpuUsage = await GetCpuUsageForProcessAsync(process, token);

            logger.LogInformation("📦 Application Memory Usage: {Used} MB", usedMemoryMb);
            logger.LogInformation("🧮 Application CPU Usage: {Usage:F2}%", cpuUsage);

            if (!preview)
            {
                if (parameters.TryGetValue("MaxMemoryMB", out var memStr) &&
                    int.TryParse(memStr, out var memThreshold) &&
                    usedMemoryMb > memThreshold)
                {
                    logger.LogWarning("⚠️ Memory usage exceeded: {Used} MB > {Threshold} MB", usedMemoryMb, memThreshold);
                }

                if (parameters.TryGetValue("MaxCpuPercent", out var cpuStr) &&
                    float.TryParse(cpuStr, out var cpuThreshold) &&
                    cpuUsage > cpuThreshold)
                {
                    logger.LogWarning("⚠️ CPU usage exceeded: {Used:F2}% > {Threshold}%", cpuUsage, cpuThreshold);
                }
            }

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
