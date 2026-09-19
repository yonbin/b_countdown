# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Countdown 是一个 Windows 11 桌面悬浮倒计时（WPF, .NET 8, C# 12）。始终置顶的卡片式悬浮窗、纯本地、零网络。需求与验收标准以规格文档为准，改动行为前先读 `specs/001-floating-countdown/spec.md`（该目录还有 plan、research、contracts 等设计资料）。

## 常用命令

```bash
# 构建 / 运行 / 测试（解决方案文件是 Countdown.slnx）
dotnet build -c Release
dotnet run --project src/Countdown.App
dotnet test tests/Countdown.Core.Tests -c Release

# 跑单个测试（类名或方法名做子串过滤）
dotnet test tests/Countdown.Core.Tests --filter "FullyQualifiedName~SleepResumeTests"

# 重新生成应用图标（需要 Python + Pillow）
python src/Countdown.App/Assets/generate_icon.py

# 发布免安装单文件（Release 附件用的命令；产物 zip 前位于 artifacts/，已 gitignore）
dotnet publish src/Countdown.App -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none \
  -o artifacts/publish/win-x64/Countdown-<version>-win-x64
```

## 架构

### Core / App / Tests 分层

- `src/Countdown.Core/`：零 UI 依赖的计时核心。关键不变量是**剩余时间永远由绝对结束时刻减去注入的 `IClock.UtcNow` 得出**（`CountdownSession`），UI 计时器从不自己递减——因此系统睡眠/唤醒不会产生漂移；到点停在 `Finished`（00:00），绝不向上累计。测试用 `FakeClock` 确定性地模拟睡眠/唤醒，改时间逻辑必须配套测试。
- `src/Countdown.App/`：WPF 表现层 + Windows 系统服务。轻量 MVVM（手写 `INotifyPropertyChanged` + `RelayCommand`，无框架）。`DispatcherTimer`（250ms）只负责触发 `session.Refresh()` 后整体重绘。
- `App.xaml.cs` 是唯一的组装根：单实例 Mutex、`PowerWatcher`（唤醒事件立即刷新）、`WindowPlacementService`（位置持久化）、`TrayIconService`、`WindowIconService` 都在这里接线。

### 平台与互操作注意事项

- App 工程同时启用 `UseWPF` 和 `UseWindowsForms`（WinForms 仅用于托盘 `NotifyIcon`）。csproj 中显式 `<Using Remove="System.Windows.Forms" />`、`System.Drawing` 以消除与 WPF 的类型歧义（Application、KeyEventArgs 等）；新代码引用 WinForms 类型时用 `using Forms = System.Windows.Forms;` 别名或完全限定名。
- **DPI 感知在代码里声明**（`App` 静态构造函数调用 `SetProcessDpiAwarenessContext(PerMonitorV2)`），不在 app.manifest：启用 WinForms 后 manifest 的 DPI 段会被 SDK 剥离（WFAC010）。改启动路径时确保该调用早于任何窗口创建。
- 版本号唯一来源是仓库根 `Directory.Build.props`（`Version` / `FileVersion`）。`TimerViewModel.AppVersion` 通过 `Environment.ProcessPath` 读 exe 版本资源——**不要改回 `Assembly.Location`**，单文件发布时它返回空字符串。
- 设置存于 `%AppData%\Countdown\settings.json`；损坏静默回退默认值，写入是临时文件 + 替换的原子操作。

### 图标管线

`Assets/app.ico` 是多帧图标（16–256，每帧独立渲染，≤32px 有专门的简化加粗处理），由 `Assets/generate_icon.py`（Pillow）生成；`app.svg` 是矢量源，`app-256.png` 是预览。三者设计常量需保持同步。ico 在 csproj 中既是 `ApplicationIcon`（嵌入 exe）又是 `Resource`（供窗口/托盘经 pack URI `/Assets/app.ico` 加载）。`WindowIconService` 在窗口句柄创建后用 `WM_SETICON` 显式注册高清 HICON（避免高 DPI 下任务栏取到过小的帧）。

### 发布流程

版本号三处概念：`Directory.Build.props` 里的 `Version` → 构建产物 → git tag（`v<version>`）→ GitHub Release（`gh release create`，自包含 zip 用 `gh release upload` 附加）。发布前确认 tag 指向的提交与 zip 构建源码一致。
