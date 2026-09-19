# Countdown

Windows 11 自用桌面小工具：一个始终置顶的悬浮倒计时条，编程或录屏时余光即可看到剩余时间。到点卡片转红、停在 00:00 提醒（无声音），睡眠/唤醒后计时依然正确。纯本地运行、零网络、无账号。

完整规格与验收：[`specs/001-floating-countdown/`](specs/001-floating-countdown/)（先读 `spec.md` 与 `quickstart.md`）。

## 环境

- Windows 11（x64 / ARM64）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 或更高（本仓库曾用 .NET 10 SDK 构建 net8.0 目标）

## 构建与测试

```powershell
dotnet build -c Release
dotnet test tests/Countdown.Core.Tests -c Release
```

## 运行

```powershell
dotnet run --project src/Countdown.App
```

启动后在悬浮条上**右键**操作：5/10/25 分钟预设、`自定义…`（接受 `25`、`5m`、`90s`、`1m30s`）、暂停/继续、重置、退出。悬浮条可拖动，位置自动记忆；到点卡片转红并停在 00:00（纯视觉提醒、不发声），可重置或直接以新时长重开。

## 发布免安装单文件

```powershell
# x64
dotnet publish src/Countdown.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
# ARM64
dotnet publish src/Countdown.App -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true
```

产物在 `src/Countdown.App/bin/Release/net8.0-windows/<rid>/publish/`，复制到未装运行时的 Windows 11 机器即可运行。

## 项目结构

- `src/Countdown.Core/` — 无 UI 依赖的计时核心（状态机、绝对结束时刻、时长解析、格式化、设置模型）
- `src/Countdown.App/` — WPF 界面与系统服务（置顶悬浮窗、右键菜单、电源事件、位置持久化）
- `tests/Countdown.Core.Tests/` — xUnit 单元测试（含虚拟时钟模拟睡眠/唤醒）
