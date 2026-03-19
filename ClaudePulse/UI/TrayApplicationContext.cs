using ClaudePulse.Models;
using ClaudePulse.Server;
using ClaudePulse.Services;

namespace ClaudePulse.UI;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly HookHttpServer _server;
    private readonly SessionManager _sessionManager;
    private readonly System.Windows.Forms.Timer _stalenessTimer;

    public TrayApplicationContext()
    {
        _sessionManager = new SessionManager();

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

            // Check if hooks are configured on first launch - auto-configure
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
                _trayIcon.ShowBalloonTip(5000, "Task Complete",
                    $"{session.ProjectName}: Claude finished working", ToolTipIcon.Info);
                break;

            case "Notification":
                _trayIcon.ShowBalloonTip(5000,
                    evt.Title ?? "Claude Code",
                    evt.Message ?? "Notification received",
                    ToolTipIcon.Warning);
                break;

            case "SessionEnd" when _sessionManager.Sessions.Count == 0:
                _trayIcon.ShowBalloonTip(3000, "Session Ended",
                    $"{session.ProjectName}: All sessions closed", ToolTipIcon.Info);
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
        // NotifyIcon.Text has a 127 char limit
        var summary = _sessionManager.StatusSummary;
        var text = $"ClaudePulse {port}- {summary}";
        _trayIcon.Text = text.Length > 127 ? text[..127] : text;
    }

    private void RebuildMenu(ContextMenuStrip menu)
    {
        menu.Items.Clear();

        // Header
        var header = menu.Items.Add("ClaudePulse");
        header.Enabled = false;
        header.Font = new System.Drawing.Font(header.Font, System.Drawing.FontStyle.Bold);

        menu.Items.Add(new ToolStripSeparator());

        // Session list
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
                var item = menu.Items.Add($"{stateEmoji} {session.ProjectName} - {session.ElapsedDisplay}");
                item.Enabled = false;
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
        _stalenessTimer.Stop();
        _stalenessTimer.Dispose();
        _trayIcon.Visible = false;
        _server.Dispose();
        Application.Exit();
    }
}
