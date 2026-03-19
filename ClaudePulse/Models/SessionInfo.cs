namespace ClaudePulse.Models;

public class SessionInfo
{
    public string Id { get; set; } = "";
    public string? Cwd { get; set; }
    public string? Model { get; set; }
    public SessionState State { get; set; } = SessionState.Idle;
    public DateTime StartTime { get; set; } = DateTime.Now;
    public DateTime LastEventTime { get; set; } = DateTime.Now;
    public string? LastToolName { get; set; }

    public string ProjectName =>
        string.IsNullOrEmpty(Cwd) ? Id[..Math.Min(8, Id.Length)] : Path.GetFileName(Cwd)!;

    public string ElapsedDisplay
    {
        get
        {
            var elapsed = DateTime.Now - StartTime;
            return elapsed.TotalHours >= 1
                ? $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m"
                : elapsed.TotalMinutes >= 1
                    ? $"{(int)elapsed.TotalMinutes}m {elapsed.Seconds}s"
                    : $"{elapsed.Seconds}s";
        }
    }

    public void HandleEvent(HookEvent evt)
    {
        LastEventTime = DateTime.Now;

        if (!string.IsNullOrEmpty(evt.Cwd))
            Cwd = evt.Cwd;
        if (!string.IsNullOrEmpty(evt.Model))
            Model = evt.Model;

        switch (evt.HookEventName)
        {
            case "SessionStart":
                State = SessionState.Idle;
                break;
            case "UserPromptSubmit":
            case "PreToolUse":
            case "PostToolUse":
                State = SessionState.Working;
                if (!string.IsNullOrEmpty(evt.ToolName))
                    LastToolName = evt.ToolName;
                break;
            case "Notification" when evt.NotificationType == "permission_prompt":
                State = SessionState.WaitingForUser;
                break;
            case "Stop":
                State = SessionState.Idle;
                break;
        }
    }
}
