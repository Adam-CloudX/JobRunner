using JobRunner.Core;
using Microsoft.Extensions.Logging;

namespace JobRunner.Jobs
{
    public class DiskCleanupJob : IJobTask
    {
        public string Name => "DiskCleanupJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            var logger = context.Logger;

            var hasPath = context.Parameters.TryGetValue("TargetDirectory", out var targetDir);
            var hasDays = context.Parameters.TryGetValue("DeleteOlderThanDays", out var daysStr);
            var preview = context.Parameters.TryGetValue("Preview", out var previewVal) && previewVal.Equals("true", StringComparison.OrdinalIgnoreCase);

            if (!hasPath || !hasDays || !int.TryParse(daysStr, out var days))
            {
                logger.LogError("Missing or invalid parameters. Required: TargetDirectory, DeleteOlderThanDays");
                return;
            }

            if (!Directory.Exists(targetDir))
            {
                logger.LogError("Directory not found: {TargetDirectory}", targetDir);
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-days);
            var files = Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories);

            int deletedCount = 0;
            long totalBytes = 0;

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTimeUtc < cutoff)
                    {
                        if (preview)
                        {
                            logger.LogInformation("Preview: Would delete {File}", info.FullName);
                        }
                        else
                        {
                            totalBytes += info.Length;
                            info.Delete();
                            deletedCount++;
                            logger.LogInformation("Deleted: {File}", info.FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process {File}", file);
                }
            }

            if (!preview)
            {
                logger.LogInformation("Cleanup complete. Deleted {Count} files, freed {Size} KB",
                    deletedCount, totalBytes / 1024);
            }
            else
            {
                logger.LogInformation("Preview mode complete. {Count} files would be deleted.", deletedCount);
            }

            await Task.CompletedTask;
        }
    }
}