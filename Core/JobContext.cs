using Microsoft.Extensions.Logging;

namespace JobRunner.Core
{
    public class JobContext
    {
        public required ILogger Logger { get; init; }
        public IDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
    }
}
