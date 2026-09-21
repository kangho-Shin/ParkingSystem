using System.Collections.Concurrent;

namespace Parking.TerminalAgent;

public sealed class DailyFileLoggerProvider : ILoggerProvider
{
    private readonly string _baseDirectory;
    private readonly object _writeLock = new();
    private readonly ConcurrentDictionary<string, DailyFileLogger> _loggers = new();

    public DailyFileLoggerProvider(string baseDirectory)
    {
        _baseDirectory = Path.IsPathRooted(baseDirectory)
            ? baseDirectory
            : Path.Combine(AppContext.BaseDirectory, baseDirectory);
    }

    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new DailyFileLogger(name, WriteLine));

    public void Dispose() => _loggers.Clear();

    private void WriteLine(string line)
    {
        DateTime now = DateTime.Now;
        string directory = Path.Combine(_baseDirectory, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
        string filePath = Path.Combine(directory, "TerminalAgent.log");

        lock (_writeLock)
        {
            Directory.CreateDirectory(directory);
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
    }

    private sealed class DailyFileLogger(
        string categoryName,
        Action<string> writeLine) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            string message = formatter(state, exception);
            string exceptionText = exception is null ? string.Empty : $" | {exception}";
            writeLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logLevel}] {categoryName}: {message}{exceptionText}");
        }
    }
}
