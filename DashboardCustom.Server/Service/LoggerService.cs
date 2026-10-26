namespace DashboardCustom.Server.Service;

public static class LoggerService
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DashboardCustom", "logs");
    private static readonly string LogPath = Path.Combine(LogDir, "app.log");
    private static readonly object Lock = new();

    public static void Log(string message, Exception? ex = null)
    {
        lock (Lock)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                if (ex != null)
                    logEntry += $" | {ex.Message} | {ex.StackTrace}";
                File.AppendAllText(LogPath, logEntry + Environment.NewLine);
            }
            catch
            {
                // ignore logging failures
            }
        }
    }
}
public sealed class FileLogger(string category) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        LoggerService.Log($"[{category}] {formatter(state, exception)}", exception);
    }
}

public sealed class FileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName);
    public void Dispose() { }
}
