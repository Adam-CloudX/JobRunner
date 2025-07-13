using JobRunner.Core;
using Microsoft.Extensions.Logging;

namespace JobRunner.Jobs
{
    public class DiskCleanupJob : IPreviewableJob
    {
        public string Name => "DiskCleanupJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            await Run(context, preview: false, cancellationToken);
        }

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
        {
            await Run(context, preview: true, cancellationToken);
        }

        private static async Task Run(JobContext context, bool preview, CancellationToken cancellationToken)
        {
            var logger = context.Logger;

            var hasPath = context.Parameters.TryGetValue("TargetDirectory", out var targetDir);
            var hasDays = context.Parameters.TryGetValue("DeleteOlderThanDays", out var daysStr);
            if (!hasPath || !hasDays || !int.TryParse(daysStr, out var days))
            {
                logger.LogError("❌ Missing or invalid parameters. Required: TargetDirectory, DeleteOlderThanDays");
                return;
            }

            if (!Directory.Exists(targetDir))
            {
                logger.LogError("📁 Directory not found: {TargetDirectory}", targetDir);
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
                            logger.LogInformation("📝 Preview: Would delete {File}", info.FullName);
                        }
                        else
                        {
                            totalBytes += info.Length;
                            info.Delete();
                            deletedCount++;
                            logger.LogInformation("🧹 Deleted: {File}", info.FullName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "⚠️ Failed to process {File}", file);
                }
            }

            var mode = preview ? "🔍 Preview mode" : "✅ Cleanup complete";
            var summary = preview
                ? $"{deletedCount} files would be deleted."
                : $"Deleted {deletedCount} files, freed {totalBytes / 1024:N0} KB";

            logger.LogInformation("{Mode}: {Summary}", mode, summary);

            await Task.CompletedTask;
        }
    }
}