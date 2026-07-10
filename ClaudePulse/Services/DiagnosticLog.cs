namespace ClaudePulse.Services;

using System.Diagnostics;

/// <summary>
/// Lightweight diagnostic logger. Writes to Debug output (debug builds only)
/// and appends to %LOCALAPPDATA%\ClaudePulse\diagnostic.log so diagnostics
/// survive in Release builds. File logging never throws.
/// </summary>
public static class DiagnosticLog
{
    private const long MaxLogSizeBytes = 1024 * 1024; // 1 MB, truncate on exceed

    private static readonly object FileLock = new();

    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClaudePulse", "diagnostic.log");

    public static void Info(string message)
        => Write($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");

    public static void Warn(string message)
        => Write($"⚠ {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");

    public static void Error(string message, Exception? ex = null)
        => Write($"❌ {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}{(ex != null ? $"\n{ex}" : "")}");

    private static void Write(string line)
    {
        Debug.WriteLine($"[ClaudePulse] {line}");

        try
        {
            lock (FileLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);

                var file = new FileInfo(LogPath);
                if (file.Exists && file.Length > MaxLogSizeBytes)
                    file.Delete();

                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Logging must never crash the app.
        }
    }
}
