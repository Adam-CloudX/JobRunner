using JobRunner.Core;
using Microsoft.Extensions.Logging;
using System.Text;

namespace JobRunner.Jobs
{
    public class PingJob : IPreviewableJob
    {
        public string Name => "PingJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: false, cancellationToken);

        public async Task PreviewAsync(JobContext context, CancellationToken cancellationToken)
            => await Run(context, preview: true, cancellationToken);

        private static async Task Run(JobContext context, bool preview, CancellationToken cancellationToken)
        {
            var logger = context.Logger;
            var logBuilder = new StringBuilder();

            void LogInfo(string msg) => logBuilder.AppendLine(msg);
            void LogError(string msg) => logBuilder.AppendLine(msg);

            var url = context.Parameters.TryGetValue("Url", out var value) ? value : "https://example.com";

            if (preview)
            {
                LogInfo($"Preview: Would ping {url}");
            }
            else
            {
                try
                {
                    using var http = new HttpClient();
                    var response = await http.GetAsync(url, cancellationToken);
                    LogInfo($"Pinged {url} -> {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    LogError($"Failed to reach {url}: {ex.Message}");
                }
            }

            logger.LogInformation("{BatchLog}", logBuilder.ToString().TrimEnd());
        }
    }
}