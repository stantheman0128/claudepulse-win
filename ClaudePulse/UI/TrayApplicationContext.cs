using ClaudePulse.Models;
using ClaudePulse.Server;
using ClaudePulse.Services;

namespace ClaudePulse.UI;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon _trayIcon = null!;
    private readonly HookHttpServer _server;
    private readonly SessionManager _sessionManager;
    private readonly System.Windows.Forms.Timer _stalenessTimer;

    // Debounce: only notify if idle for 3 seconds after Stop
    private readonly System.Windows.Forms.Timer _notifyDebounceTimer;
    private SessionInfo? _pendingNotifySession;

    // Track last notification's session for click-to-jump
    private SessionInfo? _lastNotificationSession;

    public TrayApplicationContext()
    {
        _sessionManager = new SessionManager();

        // Debounce timer - fires 3s after last Stop, cancelled by new working events
        _notifyDebounceTimer = new System.Windows.Forms.Timer { Interval = 3000 };
        _notifyDebounceTimer.Tick += (_, _) =>
        {
            _notifyDebounceTimer.Stop();
            var session = _pendingNotifySession;
            if (session != null)
            {
                _lastNotificationSession = session;
                _trayIcon.ShowBalloonTip(5000, "Claude Code Ready",
                    $"{session.ProjectName}: Waiting for your input\n(click to jump)",
                    ToolTipIcon.Info);
                _pendingNotifySession = null;
            }
        };

        // Build context menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Opening += (_, _) => RebuildMenu(contextMenu);
        RebuildMenu(contextMenu);

        // Create tray icon
        _trayIcon = new NotifyIcon
        {
            Icon = IconGenerator.Idle,
            Text = "ClaudePulse - No active sessions",
            Visible = true,
            ContextMenuStrip = contextMenu
        };

        // Click on balloon tip → jump to session
        _trayIcon.BalloonTipClicked += (_, _) =>
        {
            if (_lastNotificationSession != null)
            {
                WindowActivator.TryActivateSession(
                    _lastNotificationSession.Cwd,
                    _lastNotificationSession.Id);
            }
        };

        // Update icon when sessions change
        _sessionManager.OnStateChanged += UpdateTrayState;

        // Staleness timer
        _stalenessTimer = new System.Windows.Forms.Timer { Interval = 10_000 };
        _stalenessTimer.Tick += (_, _) => _sessionManager.CleanupStale();
        _stalenessTimer.Start();

        // Start HTTP server
        var syncContext = SynchronizationContext.Current!;
        _server = new HookHttpServer(syncContext);
        _server.OnHookEvent += HandleHookEvent;

        if (_server.Start())
        {
            _trayIcon.Text = $"ClaudePulse (:{_server.Port}) - No active sessions";

            if (!HookConfigurator.AreHooksConfigured(_server.Port))
            {
                ConfigureHooks();
            }
        }
        else
        {
            _trayIcon.Icon = IconGenerator.Error;
            _trayIcon.Text = "ClaudePulse - Server failed to start";
            _trayIcon.ShowBalloonTip(5000, "ClaudePulse Error",
                "Could not start server on ports 19280-19289", ToolTipIcon.Error);
        }
    }

    private void ConfigureHooks()
    {
        var (success, message) = HookConfigurator.EnsureHooksConfigured(_server.Port);
        _trayIcon.ShowBalloonTip(3000,
            success ? "Hooks Configured" : "Hook Configuration Failed",
            message,
            success ? ToolTipIcon.Info : ToolTipIcon.Error);
    }

    private void HandleHookEvent(HookEvent evt)
    {
        var session = _sessionManager.HandleEvent(evt);

        switch (evt.HookEventName)
        {
            case "Stop":
                // Don't notify immediately — start debounce timer.
                // If Claude starts working again within 3s, the timer gets cancelled.
                _pendingNotifySession = session;
                _notifyDebounceTimer.Stop();
                _notifyDebounceTimer.Start();
                break;

            case "UserPromptSubmit":
            case "PreToolUse":
            case "PostToolUse":
                // Claude is working again — cancel pending notification
                _notifyDebounceTimer.Stop();
                _pendingNotifySession = null;
                break;

            case "Notification" when evt.NotificationType == "permission_prompt":
                // Permission prompts are important — notify immediately
                _notifyDebounceTimer.Stop();
                _pendingNotifySession = null;
                _lastNotificationSession = session;
                _trayIcon.ShowBalloonTip(5000,
                    "Permission Needed",
                    $"{session.ProjectName}: Claude needs your approval\n(click to jump)",
                    ToolTipIcon.Warning);
                break;

            // Ignore other Notification types (plugin noise like "Double Shot Latte" etc.)

            case "SessionEnd" when _sessionManager.Sessions.Count == 0:
                _notifyDebounceTimer.Stop();
                _pendingNotifySession = null;
                break;
        }
    }

    private void UpdateTrayState()
    {
        var state = _sessionManager.AggregateState;
        _trayIcon.Icon = state switch
        {
            SessionState.Working => IconGenerator.Working,
            SessionState.WaitingForUser => IconGenerator.Waiting,
            SessionState.Stale => IconGenerator.Stale,
            _ => IconGenerator.Idle
        };

        var port = _server.Port > 0 ? $"(:{_server.Port}) " : "";
        var summary = _sessionManager.StatusSummary;
        var text = $"ClaudePulse {port}- {summary}";
        _trayIcon.Text = text.Length > 127 ? text[..127] : text;
    }

    private void RebuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        var header = menu.Items.Add("ClaudePulse");
        header.Enabled = false;
        header.Font = new System.Drawing.Font(header.Font, System.Drawing.FontStyle.Bold);

        menu.Items.Add(new ToolStripSeparator());

        if (_sessionManager.Sessions.Count > 0)
        {
            foreach (var session in _sessionManager.Sessions.Values)
            {
                var stateEmoji = session.State switch
                {
                    SessionState.Working => "🔵",
                    SessionState.WaitingForUser => "🟠",
                    SessionState.Stale => "⚪",
                    _ => "🟢"
                };
                var s = session;
                menu.Items.Add($"{stateEmoji} {session.ProjectName} - {session.ElapsedDisplay}",
                    null, (_, _) => WindowActivator.TryActivateSession(s.Cwd, s.Id));
            }
        }
        else
        {
            var noSessions = menu.Items.Add("No active sessions");
            noSessions.Enabled = false;
        }

        menu.Items.Add(new ToolStripSeparator());

        var hooksConfigured = _server != null && _server.Port > 0 && HookConfigurator.AreHooksConfigured(_server.Port);
        var configLabel = hooksConfigured ? "✓ Hooks Configured" : "Configure Hooks...";
        var configItem = menu.Items.Add(configLabel, null, (_, _) => ConfigureHooks());
        if (hooksConfigured) configItem.Enabled = false;

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());
    }

    private void ExitApp()
    {
        _notifyDebounceTimer.Stop();
        _notifyDebounceTimer.Dispose();
        _stalenessTimer.Stop();
        _stalenessTimer.Dispose();
        _trayIcon.Visible = false;
        _server.Dispose();
        Application.Exit();
    }
}
