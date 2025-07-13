using JobRunner.Core;
using Microsoft.Extensions.Logging;

namespace JobRunner.Jobs
{
    public class PingJob : IJobTask
    {
        public string Name => "PingJob";

        public async Task ExecuteAsync(JobContext context, CancellationToken cancellationToken)
        {
            var url = context.Parameters.TryGetValue("Url", out var value) ? value : "https://example.com";

            var logger = context.Logger;

            try
            {
                using var http = new HttpClient();
                var response = await http.GetAsync(url, cancellationToken);
                logger.LogInformation("{Url} -> {StatusCode}", url, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reach {Url}", url);
            }
        }
    }
}