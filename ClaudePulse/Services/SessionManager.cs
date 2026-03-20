using ClaudePulse.Models;

namespace ClaudePulse.Services;

public class SessionManager
{
    private readonly Dictionary<string, SessionInfo> _sessions = new();
    public event Action? OnStateChanged;

    public IReadOnlyDictionary<string, SessionInfo> Sessions => _sessions;

    public SessionState AggregateState
    {
        get
        {
            if (_sessions.Count == 0) return SessionState.Idle;

            var hasWorking = false;
            var hasWaiting = false;

            foreach (var s in _sessions.Values)
            {
                switch (s.State)
                {
                    case SessionState.Working: hasWorking = true; break;
                    case SessionState.WaitingForUser: hasWaiting = true; break;
                }
            }

            if (hasWorking) return SessionState.Working;
            if (hasWaiting) return SessionState.WaitingForUser;
            return SessionState.Idle;
        }
    }

    // Paths that indicate plugin-spawned subprocess sessions (not real user sessions)
    private static readonly string[] IgnoredPaths = { "double-shot-latte", ".claude/hooks" };

    private static bool IsPluginSession(string? cwd)
    {
        if (string.IsNullOrEmpty(cwd)) return false;
        var normalized = cwd.Replace('\\', '/');
        return IgnoredPaths.Any(p => normalized.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    public SessionInfo HandleEvent(HookEvent evt)
    {
        // Filter out plugin-spawned subprocess sessions
        if (IsPluginSession(evt.Cwd))
            return new SessionInfo { Id = evt.SessionId, Cwd = evt.Cwd };

        if (evt.HookEventName == "SessionEnd")
        {
            _sessions.Remove(evt.SessionId);
            OnStateChanged?.Invoke();
            return new SessionInfo { Id = evt.SessionId, Cwd = evt.Cwd };
        }

        if (!_sessions.TryGetValue(evt.SessionId, out var session))
        {
            session = new SessionInfo { Id = evt.SessionId, Cwd = evt.Cwd };
            _sessions[evt.SessionId] = session;
        }

        session.HandleEvent(evt);
        OnStateChanged?.Invoke();
        return session;
    }

    public void CleanupStale()
    {
        var now = DateTime.Now;
        var toRemove = new List<string>();

        foreach (var (id, session) in _sessions)
        {
            var elapsed = (now - session.LastEventTime).TotalSeconds;

            if (elapsed > 1800) // 30 min - remove
            {
                toRemove.Add(id);
            }
            else if (elapsed > 600) // 10 min - mark stale
            {
                session.State = SessionState.Stale;
            }
            else if (elapsed > 30 && session.State == SessionState.Working)
            {
                session.State = SessionState.Idle;
            }
        }

        foreach (var id in toRemove)
            _sessions.Remove(id);

        if (toRemove.Count > 0)
            OnStateChanged?.Invoke();
    }

    public string StatusSummary
    {
        get
        {
            if (_sessions.Count == 0) return "No active sessions";
            var working = _sessions.Values.Count(s => s.State == SessionState.Working);
            var waiting = _sessions.Values.Count(s => s.State == SessionState.WaitingForUser);
            var parts = new List<string> { $"{_sessions.Count} session(s)" };
            if (working > 0) parts.Add($"{working} working");
            if (waiting > 0) parts.Add($"{waiting} waiting");
            return string.Join(", ", parts);
        }
    }
}
