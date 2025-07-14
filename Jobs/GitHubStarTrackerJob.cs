using JobRunner.Core;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JobRunner.Jobs
{
    public class GitHubStarTrackerJob : IPreviewableJob
    {
        public string Name => "GitHubStarTrackerJob";

        private int _lastKnownStars = -1;

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            await Run(context, preview: false, cancellationToken);
        }

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
        {
            await Run(context, preview: true, cancellationToken);
        }

        private async Task Run(JobContext context, bool preview, CancellationToken cancellationToken)
        {
            var logger = context.Logger;

            if (!context.Parameters.TryGetValue("Repo", out var repo) || string.IsNullOrWhiteSpace(repo))
            {
                logger.LogError("❌ Missing or invalid 'Repo' parameter (expected format: owner/repo)");
                return;
            }

            context.Parameters.TryGetValue("MinDelta", out var deltaRaw);
            int.TryParse(deltaRaw, out var minDelta);
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
                    logger.LogInformation("⭐ Preview: {Repo} has {Stars} star(s)", repo, currentStars);
                    return;
                }

                if (_lastKnownStars == -1)
                {
                    _lastKnownStars = currentStars;
                    logger.LogInformation("📦 Initialized: {Repo} has {Stars} star(s)", repo, currentStars);
                    return;
                }

                var diff = currentStars - _lastKnownStars;
                if (diff >= minDelta)
                {
                    logger.LogInformation("🚀 {Repo} gained {Delta} star(s)! ({Prev} ➜ {Now})", repo, diff, _lastKnownStars, currentStars);
                    _lastKnownStars = currentStars;
                }
                else
                {
                    logger.LogInformation("📉 No significant change in stars for {Repo} (Current: {Now})", repo, currentStars);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "⚠️ Failed to track stars for {Repo}", repo);
            }
        }
    }
}