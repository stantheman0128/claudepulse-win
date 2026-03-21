# ClaudePulse for Windows

## Project Overview
Windows system tray application that monitors Claude Code sessions via hooks.
Built with C# / .NET 10.0 / Windows Forms.

## Architecture
- `ClaudePulse/Server/` - HTTP server receiving hook events from Claude Code
- `ClaudePulse/Services/` - Session management and hook configuration
- `ClaudePulse/Models/` - Data models (SessionInfo, HookEvent)
- `ClaudePulse/UI/` - Tray icon, window activation, icon generation
- `deploy.sh` - Local build + deploy to Windows Startup folder

## Build & Deploy
```bash
# Local development deploy (build → kill → copy to Startup → restart)
bash deploy.sh
```

## Versioning & Release Rules

This project uses **Semantic Versioning** (semver). Claude should **autonomously judge** when to suggest a new release after pushing changes.

### Version Format: `vMAJOR.MINOR.PATCH`

| Bump   | When                                                         | Examples                                    |
|--------|--------------------------------------------------------------|---------------------------------------------|
| PATCH  | Bug fixes, minor UI tweaks, documentation-only changes       | Fix notification bug, update README         |
| MINOR  | New user-facing features, significant UI improvements        | Add settings page, new notification type    |
| MAJOR  | Breaking changes, major UI overhaul, architecture rewrite    | v1.0.0 = stable release with full UI        |

### Release Decision Rules

After pushing changes, evaluate ALL commits since the last tag and suggest a release when:

1. **Any bug fix that affects user experience** → suggest PATCH bump
2. **Any new feature or notable behavior change** → suggest MINOR bump
3. **3+ commits accumulated since last tag** → suggest at minimum a PATCH release
4. **Documentation-only changes** → do NOT suggest a release on their own

When suggesting, show:
- Current version (last tag)
- Proposed new version
- Changelog summary (grouped by: Features, Fixes, Improvements, Docs)

Wait for user confirmation before tagging.

### Current Version
- Last tag: check with `git describe --tags --abbrev=0`

## Code Conventions
- C# with .NET 6.0, file-scoped namespaces
- Windows Forms for UI (system tray app, no main window)
- Win32 API via P/Invoke for window management
- Keep changes minimal and focused
