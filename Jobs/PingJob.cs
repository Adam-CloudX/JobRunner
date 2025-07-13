using JobRunner.Core;
using Microsoft.Extensions.Logging;

namespace JobRunner.Jobs
{
    public class PingJob : IPreviewableJob
    {
        public string Name => "PingJob";

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
            var url = context.Parameters.TryGetValue("Url", out var value) ? value : "https://example.com";

            if (preview)
            {
                logger.LogInformation("📝 Preview: Would ping {Url}", url);
                return;
            }

            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(url, cancellationToken);
                logger.LogInformation("📡 {Url} -> {StatusCode}", url, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Failed to reach {Url}", url);
            }
        }
    }
}