using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ClaudePulse.UI;

public static class WindowActivator
{
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    private const int SW_RESTORE = 9;

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    /// <summary>
    /// Try to activate the terminal window running the Claude Code session.
    /// Strategy: search for windows whose title contains the project folder name,
    /// then fall back to windows with "claude" in the title.
    /// </summary>
    public static bool TryActivateSession(string? cwd, string? sessionId)
    {
        // Strategy 1: Match by project folder name in window title
        if (!string.IsNullOrEmpty(cwd))
        {
            var folderName = Path.GetFileName(cwd);
            if (!string.IsNullOrEmpty(folderName))
            {
                var hwnd = FindWindowByTitle(folderName);
                if (hwnd != IntPtr.Zero)
                    return ActivateWindow(hwnd);
            }

            // Also try the full path (some terminals show full path)
            var hwnd2 = FindWindowByTitle(cwd);
            if (hwnd2 != IntPtr.Zero)
                return ActivateWindow(hwnd2);
        }

        // Strategy 2: Find a window with "claude" in the title
        var claudeHwnd = FindWindowByTitle("claude");
        if (claudeHwnd != IntPtr.Zero)
            return ActivateWindow(claudeHwnd);

        // Strategy 3: Find Windows Terminal (might have Claude in a tab)
        var terminalHwnd = FindWindowByTitle("Windows Terminal");
        if (terminalHwnd != IntPtr.Zero)
            return ActivateWindow(terminalHwnd);

        // Strategy 4: Try to find the claude.exe process and its parent terminal
        return TryActivateByProcess();
    }

    private static IntPtr FindWindowByTitle(string searchText)
    {
        IntPtr found = IntPtr.Zero;
        var searchLower = searchText.ToLowerInvariant();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true; // continue

            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString().ToLowerInvariant();

            if (title.Contains(searchLower))
            {
                found = hWnd;
                return false; // stop
            }
            return true; // continue
        }, IntPtr.Zero);

        return found;
    }

    private static bool TryActivateByProcess()
    {
        try
        {
            // Find claude.exe processes
            var claudeProcesses = Process.GetProcessesByName("claude");
            if (claudeProcesses.Length == 0) return false;

            // Try to find the parent terminal window for any claude process
            foreach (var proc in claudeProcesses)
            {
                try
                {
                    // claude.exe itself doesn't have a window, but its parent (terminal) does
                    var mainWindow = proc.MainWindowHandle;
                    if (mainWindow != IntPtr.Zero)
                        return ActivateWindow(mainWindow);
                }
                catch { }
            }
        }
        catch { }

        return false;
    }

    private static bool ActivateWindow(IntPtr hWnd)
    {
        // Restore if minimized
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SW_RESTORE);

        return SetForegroundWindow(hWnd);
    }
}
