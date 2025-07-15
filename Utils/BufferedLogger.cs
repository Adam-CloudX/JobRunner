using Microsoft.Extensions.Logging;

namespace JobRunner.Utils
{
    public class BufferedLogger(ILogger inner, StringWriter buffer) : ILogger
    {
        private readonly ILogger _inner = inner;
        private readonly StringWriter _buffer = buffer;

        IDisposable ILogger.BeginScope<TState>(TState state) => _inner.BeginScope(state) ?? NullScope.Instance;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);
            _buffer.WriteLine(GetPrefix(logLevel) + " " + message);
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            private NullScope() { }
            public void Dispose() { }
        }
        public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

        private static string GetPrefix(LogLevel level) => level switch
        {
            LogLevel.Information => "ℹ️",
            LogLevel.Warning => "⚠️",
            LogLevel.Error => "❌",
            LogLevel.Debug => "🔍",
            LogLevel.Trace => "🔹",
            LogLevel.Critical => "🔥",
            _ => "🔸"
        };
    }
}