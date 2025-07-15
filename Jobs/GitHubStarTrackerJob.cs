using JobRunner.Core;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace JobRunner.Jobs
{
    public class GitHubStarTrackerJob : IPreviewableJob
    {
        public string Name => "GitHubStarTrackerJob";

        private int _lastKnownStars = -1;

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: false, cancellationToken);

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: true, cancellationToken);

        private async Task Run(JobContext context, bool preview, CancellationToken cancellationToken)
        {
            var logger = context.Logger;
            var logBuilder = new StringBuilder();

            void LogInfo(string msg) => logBuilder.AppendLine(msg);
            void LogError(string msg) => logBuilder.AppendLine(msg);

            if (!context.Parameters.TryGetValue("Repo", out var repo) || string.IsNullOrWhiteSpace(repo))
            {
                logger.LogError("❌ Missing or invalid 'Repo' parameter (expected format: owner/repo)");
                return;
            }

            context.Parameters.TryGetValue("MinDelta", out var deltaRaw);
            _ = int.TryParse(deltaRaw, out var minDelta);
            minDelta = minDelta < 1 ? 1 : minDelta;

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JobRunner", "1.0"));

                var url = $"https://api.github.com/repos/{repo}";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(json);
                var currentStars = doc.RootElement.GetProperty("stargazers_count").GetInt32();

                if (preview)
                {
                    LogInfo($"Preview: {repo} has {currentStars} star(s)");
                }
                else if (_lastKnownStars == -1)
                {
                    _lastKnownStars = currentStars;
                    LogInfo($"Initialized: {repo} has {currentStars} star(s)");
                }
                else
                {
                    var diff = currentStars - _lastKnownStars;
                    if (diff >= minDelta)
                    {
                        LogInfo($"🚀 {repo} gained {diff} star(s)! ({_lastKnownStars} ➜ {currentStars})");
                        _lastKnownStars = currentStars;
                    }
                    else
                    {
                        LogInfo($"No significant change in stars for {repo} (Current: {currentStars})");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to track stars for {repo}: {ex.Message}");
            }

            logger.LogInformation("{BatchLog}", logBuilder.ToString().TrimEnd());

            await Task.CompletedTask;
        }
    }
}