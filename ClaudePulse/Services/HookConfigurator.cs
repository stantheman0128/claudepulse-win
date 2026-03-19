using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClaudePulse.Services;

public static class HookConfigurator
{
    private static readonly string[] HookEvents =
    {
        "SessionStart", "Stop", "Notification", "PreToolUse",
        "PostToolUse", "UserPromptSubmit", "SessionEnd"
    };

    public static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", "settings.json");

    public static (bool configured, string message) EnsureHooksConfigured(int port)
    {
        var hookUrl = $"http://localhost:{port}/";

        try
        {
            var path = SettingsPath;
            if (!File.Exists(path))
                return (false, $"settings.json not found at {path}");

            var json = File.ReadAllText(path);
            var root = JsonNode.Parse(json)?.AsObject();
            if (root == null)
                return (false, "Could not parse settings.json");

            var hooks = root["hooks"]?.AsObject();
            if (hooks == null)
            {
                hooks = new JsonObject();
                root["hooks"] = hooks;
            }

            var modified = false;

            foreach (var eventName in HookEvents)
            {
                if (IsHookPresent(hooks, eventName, hookUrl))
                    continue;

                // Create our hook entry
                var hookEntry = new JsonObject
                {
                    ["hooks"] = new JsonArray(new JsonObject
                    {
                        ["type"] = "http",
                        ["url"] = hookUrl
                    })
                };

                // Append to existing array or create new one
                var eventArray = hooks[eventName]?.AsArray();
                if (eventArray == null)
                {
                    eventArray = new JsonArray();
                    hooks[eventName] = eventArray;
                }

                eventArray.Add(hookEntry);
                modified = true;
            }

            if (!modified)
                return (true, "All hooks already configured");

            // Write back with formatting
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options));

            return (true, "Hooks configured successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    public static bool AreHooksConfigured(int port)
    {
        try
        {
            var hookUrl = $"http://localhost:{port}/";
            var json = File.ReadAllText(SettingsPath);
            var root = JsonNode.Parse(json)?.AsObject();
            var hooks = root?["hooks"]?.AsObject();
            if (hooks == null) return false;

            // Check at least Stop and SessionStart are configured
            return IsHookPresent(hooks, "Stop", hookUrl)
                && IsHookPresent(hooks, "SessionStart", hookUrl);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsHookPresent(JsonObject hooks, string eventName, string url)
    {
        var eventArray = hooks[eventName]?.AsArray();
        if (eventArray == null) return false;

        foreach (var ruleNode in eventArray)
        {
            var hooksArray = ruleNode?["hooks"]?.AsArray();
            if (hooksArray == null) continue;

            foreach (var hookNode in hooksArray)
            {
                var type = hookNode?["type"]?.GetValue<string>();
                var hookUrl = hookNode?["url"]?.GetValue<string>();
                if (type == "http" && hookUrl == url)
                    return true;
            }
        }

        return false;
    }

    public static (bool success, string message) RemoveHooks(int port)
    {
        var hookUrl = $"http://localhost:{port}/";

        try
        {
            var path = SettingsPath;
            var json = File.ReadAllText(path);
            var root = JsonNode.Parse(json)?.AsObject();
            var hooks = root?["hooks"]?.AsObject();
            if (hooks == null) return (true, "No hooks to remove");

            foreach (var eventName in HookEvents)
            {
                var eventArray = hooks[eventName]?.AsArray();
                if (eventArray == null) continue;

                for (int i = eventArray.Count - 1; i >= 0; i--)
                {
                    var hooksArray = eventArray[i]?["hooks"]?.AsArray();
                    if (hooksArray == null) continue;

                    for (int j = hooksArray.Count - 1; j >= 0; j--)
                    {
                        var type = hooksArray[j]?["type"]?.GetValue<string>();
                        var url = hooksArray[j]?["url"]?.GetValue<string>();
                        if (type == "http" && url == hookUrl)
                        {
                            hooksArray.RemoveAt(j);
                        }
                    }

                    // Remove empty rule objects
                    if (hooksArray.Count == 0)
                        eventArray.RemoveAt(i);
                }

                // Remove empty event arrays
                if (eventArray.Count == 0)
                    hooks.Remove(eventName);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root!.ToJsonString(options));

            return (true, "Hooks removed successfully");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }
}
