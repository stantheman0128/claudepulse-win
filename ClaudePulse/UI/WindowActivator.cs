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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private const int SW_RESTORE = 9;
    private const int SW_SHOW = 5;
    private const byte VK_MENU = 0x12; // Alt key
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

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
                    return ForceActivateWindow(hwnd);
            }

            var hwnd2 = FindWindowByTitle(cwd);
            if (hwnd2 != IntPtr.Zero)
                return ForceActivateWindow(hwnd2);
        }

        // Strategy 2: Find a window with "claude" in the title
        var claudeHwnd = FindWindowByTitle("claude");
        if (claudeHwnd != IntPtr.Zero)
            return ForceActivateWindow(claudeHwnd);

        // Strategy 3: Find Windows Terminal
        var terminalHwnd = FindWindowByTitle("Windows Terminal");
        if (terminalHwnd != IntPtr.Zero)
            return ForceActivateWindow(terminalHwnd);

        // Strategy 4: Try to find claude.exe process window
        return TryActivateByProcess();
    }

    private static IntPtr FindWindowByTitle(string searchText)
    {
        IntPtr found = IntPtr.Zero;
        var searchLower = searchText.ToLowerInvariant();

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd))
                return true;

            var sb = new StringBuilder(512);
            GetWindowText(hWnd, sb, sb.Capacity);
            var title = sb.ToString().ToLowerInvariant();

            if (title.Contains(searchLower))
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

        return found;
    }

    private static bool TryActivateByProcess()
    {
        try
        {
            var claudeProcesses = Process.GetProcessesByName("claude");
            if (claudeProcesses.Length == 0) return false;

            foreach (var proc in claudeProcesses)
            {
                try
                {
                    var mainWindow = proc.MainWindowHandle;
                    if (mainWindow != IntPtr.Zero)
                        return ForceActivateWindow(mainWindow);
                }
                catch { }
            }
        }
        catch { }

        return false;
    }

    /// <summary>
    /// Force-activate a window even from a background process.
    /// Uses AttachThreadInput + Alt key trick to bypass Windows focus stealing prevention.
    /// </summary>
    private static bool ForceActivateWindow(IntPtr hWnd)
    {
        // Restore if minimized
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SW_RESTORE);

        // Get thread IDs
        var foregroundHwnd = GetForegroundWindow();
        var currentThreadId = GetCurrentThreadId();
        var foregroundThreadId = GetWindowThreadProcessId(foregroundHwnd, out _);
        var targetThreadId = GetWindowThreadProcessId(hWnd, out _);

        bool attached = false;

        try
        {
            // Attach to foreground thread to gain focus-setting permission
            if (currentThreadId != foregroundThreadId)
            {
                attached = AttachThreadInput(currentThreadId, foregroundThreadId, true);
            }

            // Send Alt key press/release to unlock SetForegroundWindow
            keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Now we can set foreground
            BringWindowToTop(hWnd);
            ShowWindow(hWnd, SW_SHOW);
            var result = SetForegroundWindow(hWnd);

            // Flash topmost then remove topmost (ensures it pops up)
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);

            return result;
        }
        finally
        {
            if (attached)
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
        }
    }
}
