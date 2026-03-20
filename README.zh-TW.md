# ClaudePulse for Windows

繁體中文 | [English](README.md)

Windows 系統匣監控工具，即時追蹤 [Claude Code](https://claude.ai/claude-code) 的工作狀態 — 靈感來自 [ClaudePulse](https://github.com/tzangms/claudepulse)（macOS 版）。

> 原版 ClaudePulse 是精美的 macOS menu bar 應用程式。本專案將相同概念帶到 Windows，並加入了原版沒有的功能。

## 功能

### 系統匣圖示
單一圖示代表**所有 session 的整體狀態**，顯示最緊急的狀態：

| 顏色 | 狀態 | 意思 | 優先順序 |
|------|------|------|----------|
| 🔵 藍色 | Working | Claude 正在工作（編輯、執行、讀取） | 最高 |
| 🟠 橙色 | Waiting | Claude 需要你的許可 | ↑ |
| 🟢 綠色 | Idle | Claude 做完了，等你輸入 | ↓ |
| ⚪ 灰色 | Stale | 超過 10 分鐘沒動靜 | 最低 |

> 如果你有 3 個 session，其中一個在 Working，圖示就是藍色。全部 Idle 才會變綠色。

### Toast 通知
通知**只在 Claude 真正停下來等你的時候才彈出**（3 秒 debounce 過濾掉中間步驟的假停頓）。點擊通知可以直接跳到對應的終端機視窗。

### 核心功能
- **多 Session 追蹤** — 同時監控多個 Claude Code session，每個都有彩色狀態指示
- **自動設定 Hooks** — 首次啟動時自動在 `~/.claude/settings.json` 設定 HTTP hooks

### 超越原版的功能
以下是 macOS 原版沒有的功能：

| 功能 | 說明 |
|------|------|
| **點擊跳轉** | 點擊通知直接跳到 Claude Code 的終端機視窗，即使你在其他 app（PPT、Chrome 等） |
| **智慧 Debounce** | 只在 Claude 真正閒下來時才通知（3 秒 debounce），不會每次中間步驟都彈 |
| **權限提醒** | Claude 需要你的許可時立即通知 — 不再錯過權限提示 |
| **Plugin 雜訊過濾** | 過濾 plugin 產生的雜訊通知（如 Double Shot Latte），只顯示重要的 |
| **非破壞性 Hook 合併** | 安全地將 hooks 加入 `settings.json`，不會覆蓋你現有的 hooks |
| **工具與模型追蹤** | 追蹤每個 session 使用的工具和模型 |

## 截圖

<img width="612" height="389" alt="image" src="https://github.com/user-attachments/assets/da161885-8fac-4140-bcf3-fe38eeb3b74b" />

<img width="355" height="307" alt="image" src="https://github.com/user-attachments/assets/b017b3b4-f238-44be-9640-6494cb82f421" />

## 安裝

### 系統需求
- Windows 10/11
- [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0)（用 `dotnet --version` 確認）

### 方式 A：從原始碼建置
```bash
git clone https://github.com/stantheman0128/claudepulse-win.git
cd claudepulse-win/ClaudePulse
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ../publish-lite
```
產出的 exe 在 `publish-lite/ClaudePulse.exe`（約 176KB）。

### 方式 B：下載 Release
到 [Releases](https://github.com/stantheman0128/claudepulse-win/releases) 下載預建置的執行檔：

| 檔案 | 大小 | 說明 |
|------|------|------|
| **ClaudePulse-lite.exe** | ~179KB | 需要安裝 .NET 6.0 Runtime |
| **ClaudePulse-standalone.exe** | ~146MB | 不需要安裝任何東西，雙擊即可執行 |

## 使用方式

1. **執行 `ClaudePulse.exe`** — 系統匣會出現一個彩色圓點
2. **首次啟動** — 自動設定 Claude Code hooks 到 `~/.claude/settings.json`
3. **開啟 Claude Code** — ClaudePulse 會自動開始追蹤你的 session
4. **點擊通知** — 直接跳到 Claude Code 的終端機視窗

### 開機自動啟動
將 `ClaudePulse.exe` 複製到 Windows 啟動資料夾：
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

### 開發工作流
```bash
# 開發中
cd ClaudePulse && dotnet run

# 部署新版（建置 → 關閉舊版 → 複製到啟動資料夾 → 重啟）
bash deploy.sh
```

## 運作原理

```
Claude Code ──(HTTP hooks)──► ClaudePulse (localhost:19280)
                                    │
                                    ├── 更新系統匣圖示顏色
                                    ├── 追蹤 session 狀態
                                    ├── 顯示 Toast 通知（有 debounce）
                                    └── 點擊 → 跳到終端機視窗
```

ClaudePulse 在 `localhost:19280` 跑一個輕量 HTTP server，接收 Claude Code 的 webhook 事件。事件包括 `SessionStart`、`Stop`、`PreToolUse`、`PostToolUse`、`Notification` 等。

當 ClaudePulse 沒在跑時，HTTP hooks 只會 timeout — **Claude Code 不受任何影響**。

## 架構

```
ClaudePulse/
├── Program.cs                  # 進入點，單實例檢查
├── Models/
│   ├── HookEvent.cs            # Claude Code hook 事件的 JSON 模型
│   ├── SessionInfo.cs          # Session 狀態機
│   └── SessionState.cs         # Idle / Working / WaitingForUser / Stale
├── Server/
│   └── HookHttpServer.cs       # HttpListener on port 19280-19289
├── Services/
│   ├── SessionManager.cs       # 多 Session 追蹤 + 過期清理
│   └── HookConfigurator.cs     # 非破壞性 settings.json 合併
└── UI/
    ├── TrayApplicationContext.cs  # NotifyIcon + debounce 通知
    ├── IconGenerator.cs           # 程式化生成彩色圓形圖示
    └── WindowActivator.cs         # Win32 API 強制啟動終端機視窗
```

**零外部依賴** — 完全使用 .NET 6 內建 API。

## 開發計畫

- [ ] **浮動視窗 UI** — WPF 打造 Dynamic Island 風格的浮動面板，支援動畫和毛玻璃效果
- [ ] **自動更新** — 啟動時檢查 GitHub releases，自動下載安裝新版
- [ ] **設定 UI** — 設定通知偏好、debounce 時間、hook 事件
- [ ] **Session 歷史** — 記錄和瀏覽過往的 session
- [ ] **全域快捷鍵** — 鍵盤快捷鍵顯示/隱藏 session 面板
- [ ] **GitHub Actions CI** — push 時自動建置和發布

## 致謝

- [ClaudePulse](https://github.com/tzangms/claudepulse) by [@tzangms](https://github.com/tzangms) — macOS 原版靈感來源
- 使用 [Claude Code](https://claude.ai/claude-code) 以 Slice-Based Iterative Development 方法開發

## 授權

MIT
