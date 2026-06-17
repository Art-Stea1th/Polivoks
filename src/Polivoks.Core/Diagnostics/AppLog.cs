using System.Diagnostics;

namespace Polivoks.Core.Diagnostics;

public static class AppLog
{
    private static readonly Lock Gate = new();
    private static string? _logDirectory;

    public static string? LogDirectory => _logDirectory;

    public static void Initialize(string appDataRoot)
    {
        _logDirectory = Path.Combine(appDataRoot, "logs");
        Directory.CreateDirectory(_logDirectory);
        Info($"Logging initialized at {_logDirectory}");
    }

    public static void Info(string message) => Write("INFO", message);

    public static void Warn(string message) => Write("WARN", message);

    public static void Error(string message, Exception? exception = null)
    {
        if (exception is null)
        {
            Write("ERROR", message);
            return;
        }

        Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {level} {message}";
        Debug.WriteLine(line);

        if (_logDirectory is null)
        {
            return;
        }

        var path = Path.Combine(_logDirectory, $"polivoks_{DateTime.Now:yyyyMMdd}.log");
        lock (Gate)
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
