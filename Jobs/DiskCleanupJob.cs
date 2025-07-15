using JobRunner.Core;
using Microsoft.Extensions.Logging;
using System.Text;

namespace JobRunner.Jobs
{
    public class DiskCleanupJob : IPreviewableJob
    {
        private static readonly HashSet<string> _skippedDirs = [];
        public string Name => "DiskCleanupJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: false, cancellationToken);

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: true, cancellationToken);

        private static async Task Run(JobContext context, bool preview, CancellationToken cancellationToken)
        {
            var logger = context.Logger;
            _skippedDirs.Clear();

            var logBuilder = new StringBuilder();
            void LogInfo(string msg) => logBuilder.AppendLine(msg);
            void LogError(string msg) => logBuilder.AppendLine(msg);

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
            var files = SafeEnumerateFiles(targetDir, "*", logger);

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
                            LogInfo($"Preview: Would delete {info.FullName}");
                        }
                        else
                        {
                            totalBytes += info.Length;
                            info.Delete();
                            deletedCount++;
                            LogInfo($"Deleted: {info.FullName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to process {file}: {ex.Message}");
                }
            }

            var mode = preview ? "🔍 Preview mode" : "✅ Cleanup complete";
            var summary = preview
                ? $"{deletedCount} files would be deleted."
                : $"Deleted {deletedCount} files, freed {totalBytes / 1024:N0} KB";

            logBuilder.AppendLine($"{mode}: {summary}");

            logger.LogInformation("{BatchLog}", logBuilder.ToString().TrimEnd());

            await Task.CompletedTask;
        }

        private static IEnumerable<string> SafeEnumerateFiles(string path, string searchPattern, ILogger? logger = null)
        {
            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                IEnumerable<string> files = Enumerable.Empty<string>();

                try
                {
                    files = Directory.GetFiles(dir, searchPattern);
                }
                catch (UnauthorizedAccessException)
                {
                    if (_skippedDirs.Add(dir))
                        logger?.LogWarning("⚠️ Skipped: {Directory} (Access Denied)", dir);
                }
                catch (Exception ex)
                {
                    if (_skippedDirs.Add(dir))
                        logger?.LogWarning("⚠️ Skipped: {Directory} ({Message})", dir, ex.Message);
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                IEnumerable<string> subDirs = Enumerable.Empty<string>();
                try
                {
                    subDirs = Directory.GetDirectories(dir);
                }
                catch (UnauthorizedAccessException)
                {
                    if (_skippedDirs.Add(dir))
                        logger?.LogWarning("⚠️ Skipped: {Directory} (Access Denied)", dir);
                }
                catch (Exception ex)
                {
                    if (_skippedDirs.Add(dir))
                        logger?.LogWarning("⚠️ Skipped: {Directory} ({Message})", dir, ex.Message);
                }

                foreach (var subDir in subDirs)
                {
                    stack.Push(subDir);
                }
            }
        }
    }
}