using System.Text.Json.Serialization;

namespace ClaudePulse.Models;

public class HookEvent
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = "";

    [JsonPropertyName("hook_event_name")]
    public string HookEventName { get; set; } = "";

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("transcript_path")]
    public string? TranscriptPath { get; set; }

    [JsonPropertyName("tool_name")]
    public string? ToolName { get; set; }

    [JsonPropertyName("tool_input")]
    public object? ToolInput { get; set; }

    [JsonPropertyName("notification_type")]
    public string? NotificationType { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("last_assistant_message")]
    public string? LastAssistantMessage { get; set; }

    [JsonPropertyName("permission_mode")]
    public string? PermissionMode { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
