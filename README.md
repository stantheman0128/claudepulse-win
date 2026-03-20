# ClaudePulse for Windows

A Windows system tray monitor for [Claude Code](https://claude.ai/claude-code) sessions — inspired by [ClaudePulse](https://github.com/tzangms/claudepulse) (macOS).

> The original ClaudePulse is a beautiful macOS menu bar app. This project brings the same concept to Windows, with additional features not found in the original.

## Features

### Core
- **System Tray Icon** — Color-coded status at a glance (🟢 idle, 🔵 working, 🟠 waiting, ⚪ stale)
- **Windows Toast Notifications** — Get notified when Claude finishes working
- **Multi-Session Tracking** — Monitor multiple Claude Code sessions simultaneously
- **Auto-Configure Hooks** — Automatically sets up Claude Code HTTP hooks on first launch

### Beyond the Original
Features that the macOS version doesn't have:

| Feature | Description |
|---------|-------------|
| **Click-to-Jump** | Click a notification to instantly jump to the Claude Code terminal window, even from another app |
| **Smart Debounce** | Only notifies when Claude is truly idle (3s debounce), not on every intermediate stop |
| **Permission Alerts** | Immediate notification when Claude needs your approval — no more missed permission prompts |
| **Plugin Noise Filter** | Filters out noisy plugin notifications, only surfaces what matters |
| **Non-Destructive Hook Merge** | Safely adds hooks to your `settings.json` without overwriting your existing hooks |
| **Tool & Model Tracking** | Tracks which tools and models each session is using |

## Screenshots

<!-- TODO: Add screenshots
- System tray icon (green/blue states)
- Toast notification popup
- Right-click context menu with session list
-->

<img width="612" height="389" alt="image" src="https://github.com/user-attachments/assets/da161885-8fac-4140-bcf3-fe38eeb3b74b" />

<img width="335" height="236" alt="image" src="https://github.com/user-attachments/assets/b5238be3-68d1-473d-93a8-4fabf5c12081" />



## Installation

### Requirements
- Windows 10/11
- [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0) (check with `dotnet --version`)

### Option A: Build from Source
```bash
git clone https://github.com/stantheman0128/claudepulse-win.git
cd claudepulse-win/ClaudePulse
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ../publish-lite
```
The exe will be at `publish-lite/ClaudePulse.exe` (~176KB).

### Option B: Download Release
Check [Releases](https://github.com/stantheman0128/claudepulse-win/releases) for pre-built executables.

## Usage

1. **Run `ClaudePulse.exe`** — it appears as a colored circle in your system tray
2. **First launch** — automatically configures Claude Code hooks in `~/.claude/settings.json`
3. **Open Claude Code** — ClaudePulse will start tracking your sessions
4. **Click notifications** — jump directly to the Claude Code terminal window

### Auto-Start on Boot
Copy `ClaudePulse.exe` to your Windows Startup folder:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

### Development Workflow
```bash
# During development
cd ClaudePulse && dotnet run

# Deploy new version (build → kill old → copy to Startup → restart)
bash deploy.sh
```

## How It Works

```
Claude Code ──(HTTP hooks)──► ClaudePulse (localhost:19280)
                                    │
                                    ├── Update tray icon color
                                    ├── Track session state
                                    ├── Show Toast notification (debounced)
                                    └── Click → Jump to terminal window
```

ClaudePulse runs a lightweight HTTP server on `localhost:19280` that receives webhook events from Claude Code. Events include `SessionStart`, `Stop`, `PreToolUse`, `PostToolUse`, `Notification`, and more.

When ClaudePulse is not running, the HTTP hooks simply timeout — **Claude Code continues to work normally**.

## Architecture

```
ClaudePulse/
├── Program.cs                  # Entry point, single-instance check
├── Models/
│   ├── HookEvent.cs            # JSON model for Claude Code hook events
│   ├── SessionInfo.cs          # Per-session state machine
│   └── SessionState.cs         # Idle / Working / WaitingForUser / Stale
├── Server/
│   └── HookHttpServer.cs       # HttpListener on port 19280-19289
├── Services/
│   ├── SessionManager.cs       # Multi-session tracking + staleness cleanup
│   └── HookConfigurator.cs     # Non-destructive settings.json merge
└── UI/
    ├── TrayApplicationContext.cs  # NotifyIcon + debounced notifications
    ├── IconGenerator.cs           # Programmatic colored circle icons
    └── WindowActivator.cs         # Win32 API force-activate terminal window
```

**Zero external dependencies** — built entirely with .NET 6 built-in APIs.

## Roadmap

- [ ] **Floating Window UI** — WPF-based Dynamic Island-style overlay with animations and blur effects
- [ ] **Auto-Update** — Check GitHub releases on startup, download and install new versions
- [ ] **Settings UI** — Configure notification preferences, debounce timing, hook events
- [ ] **Session History** — Log and browse past sessions
- [ ] **Global Hotkey** — Keyboard shortcut to show/hide session panel
- [ ] **GitHub Actions CI** — Automated builds and releases on push

## Acknowledgments

- [ClaudePulse](https://github.com/tzangms/claudepulse) by [@tzangms](https://github.com/tzangms) — the original macOS inspiration
- Built with [Claude Code](https://claude.ai/claude-code) using Slice-Based Iterative Development

## License

MIT
