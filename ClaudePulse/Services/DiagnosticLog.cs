namespace ClaudePulse.Services;

using System.Diagnostics;

/// <summary>
/// Lightweight diagnostic logger that writes to Debug output.
/// Only visible when running with a debugger attached.
/// </summary>
public static class DiagnosticLog
{
    public static void Info(string message)
        => Debug.WriteLine($"[ClaudePulse] {DateTime.Now:HH:mm:ss} {message}");

    public static void Warn(string message)
        => Debug.WriteLine($"[ClaudePulse] ⚠ {DateTime.Now:HH:mm:ss} {message}");

    public static void Error(string message, Exception? ex = null)
        => Debug.WriteLine($"[ClaudePulse] ❌ {DateTime.Now:HH:mm:ss} {message}{(ex != null ? $"\n{ex}" : "")}");
}
