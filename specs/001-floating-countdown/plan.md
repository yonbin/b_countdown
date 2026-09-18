# Implementation Plan: 始终置顶的悬浮倒计时器

**Branch**: `001-floating-countdown`（仅为 Spec Kit 编号名；本项目当前不是 git 仓库，未实际创建分支） | **Date**: 2026-09-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-floating-countdown/spec.md`

## Summary

构建一个 Windows 11 自用桌面小工具：单一悬浮窗始终置顶显示大号倒计时剩余时间，服务"编程、录屏时余光可见"的核心场景；到点播放本地重复提示音；计时以绝对结束时刻为基准，正确穿越睡眠/唤醒。技术方案为 **WPF on .NET 8 (LTS) + C# 12 + XAML**，纯本地、零网络、自包含单文件分发。计时核心逻辑放在无 UI 依赖的类库中以便单元测试（睡眠/唤醒数学必须可测，对应 SC-003 的 ±1 秒要求），WPF 项目仅承载窗口、声音与设置持久化。

## Technical Context

**Language/Version**: C# 12 / .NET 8 LTS（WPF，XAML）

**Primary Dependencies**: 仅 BCL / WPF，无第三方 UI 框架：
- `System.Windows.Threading.DispatcherTimer`（UI 节拍，250ms 重算显示，不用于计时本身）
- `System.Media.SoundPlayer`（嵌入式 wav，`PlayLooping` 循环提醒）
- `Microsoft.Win32.SystemEvents.PowerModeChanged`（监听 `Resume`，唤醒后立即重算/响铃）
- `System.Threading.Mutex`（Local 命名互斥体，单实例）
- `System.Text.Json`（设置文件）

**Storage**: 单个本地 JSON 设置文件 `%AppData%\FloatingCountdown\settings.json`，仅保存窗口位置（含所在显示器标识）；不保存进行中的计时（FR-015、Assumptions）。

**Testing**: xUnit 针对 Core 类库（net8.0，无 Windows 桌面运行时依赖）；用 .NET 8 内置 `TimeProvider` 抽象注入虚拟时钟，验证暂停、跨睡眠、超时累加等时间数学。UI/置顶/声音/真实睡眠走 quickstart.md 的手工验收。

**Target Platform**: Windows 11，x64 与 ARM64；`win-x64` / `win-arm64` 自包含单文件发布。

**Project Type**: desktop-app（单窗口 WPF 应用 + 一个可测 Core 类库 + 一个测试项目）

**Performance Goals**: 冷启动 < 2 秒（2026-09-18 实测 Release 212ms ✓）；常驻内存目标 < 80 MB（实测 Release 私有内存约 85 MB、Working Set 约 147 MB；66 秒静置、句柄约 1966 均平稳无增长，判定为 WPF + 透明分层窗运行时基线而非泄漏，**接受约 85 MB 私有内存的偏差**——converge T042 结论）；显示节拍 250ms（呈现粒度仍为整秒）；CPU 空闲近零；任意 10 分钟区间显示误差 ≤ 1 秒（SC-003，已由虚拟时钟单测覆盖）。

**Constraints**: 零网络请求（FR-016）；单实例、单倒计时；窗口始终置顶（普通窗口层级，不承诺独占全屏）；仅持久化窗口位置；离线完整可用。

**Scale/Scope**: 1 个窗口、约 15 个源文件、3 个 .NET 项目；唯一用户为提议者本人；appetite = small（数天）。

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` 存在但仍是**未填写的模板**（全部为占位符，无任何已批准原则、质量门禁或技术约束）。

- 门禁结论：**无已批准的宪法条款可违反，门禁空转通过（vacuously pass）**。
- 风险提示：项目没有显式原则（如测试要求、简洁性条款）可对照；本计划自行采用"计时核心必须单元测试、UI 薄、零第三方运行时依赖"作为本特性的自律约束。
- 建议：在进入实现前由用户填写并批准 constitution.md（非阻塞）。
- **Phase 1 后复检**：设计产物（research/data-model/contracts/quickstart）未引入与既有模板冲突的内容；结论不变。

## Project Structure

### Documentation (this feature)

```text
specs/001-floating-countdown/
├── plan.md              # 本文件
├── research.md          # Phase 0：技术决策与备选
├── data-model.md        # Phase 1：会话状态机与设置实体
├── quickstart.md        # Phase 1：构建/测试/手工验收指南
├── contracts/
│   └── window-behavior.md   # Phase 1：面向用户的窗口行为契约（按状态）
└── tasks.md             # Phase 2：/speckit-tasks 生成（本命令不创建）
```

### Source Code (repository root)

```text
src/
├── FloatingCountdown.Core/                # net8.0；无 UI、无 Windows 依赖，可快速单测
│   ├── CountdownSession.cs                # 状态机：以绝对结束时刻计算剩余
│   ├── SessionState.cs                    # Idle/Running/Paused/Finished/Overtime
│   ├── IClock.cs / SystemClock.cs         # 对 TimeProvider 的薄封装（测试注入虚拟时钟）
│   ├── DurationParser.cs                  # "25" / "5m" / "90s" 等输入解析与校验
│   └── Settings/
│       ├── AppSettings.cs                 # 窗口位置等可持久化设置的模型
│       └── SettingsStore.cs               # JSON 读写接口（实现放 App 项目）
└── FloatingCountdown.App/                 # net8.0-windows；WPF 启动项目
    ├── App.xaml / App.xaml.cs             # 单实例互斥体、启动、DI 组合（手工轻量组装）
    ├── Views/
    │   └── TimerWindow.xaml(.cs)          # 无边框置顶悬浮窗、拖动、控件
    ├── ViewModels/
    │   └── TimerViewModel.cs              # 会话状态 → 显示文本/命令；250ms 节拍
    ├── Services/
    │   ├── AudioAlertService.cs           # SoundPlayer 循环播放/停止
    │   ├── PowerWatcher.cs                # PowerModeChanged(Resume) → 立即重算
    │   ├── WindowPlacementService.cs      # 位置保存/恢复、多屏边界回退
    │   └── JsonSettingsStore.cs           # AppData 下 settings.json
    └── Resources/
        └── alert.wav                      # 内置提示音（嵌入资源）

tests/
└── FloatingCountdown.Core.Tests/          # xUnit，net8.0
    ├── CountdownSessionTests.cs           # 开始/暂停/继续/重置/归零/超时
    ├── SleepResumeTests.cs                # 虚拟时钟穿越睡眠：剩余重算、跨结束时刻
    └── DurationParserTests.cs             # 合法/非法/边界输入（FR-012/FR-013）
```

**Structure Decision**: 采用"App + Core + Tests"三项目而非单一 WPF 项目，唯一理由是**可测试性**：SC-003 要求睡眠/唤醒后误差 ≤1 秒，时间数学（绝对结束时刻、暂停冻结、超时累加）必须能用虚拟时钟在无 UI、无真人睡眠的条件下自动化验证；因此把状态机与解析逻辑放进不引用 WPF 的 net8.0 类库。UI 层保持薄：一个窗口、一个 ViewModel、四个被动服务。不引入 MVVM 框架、DI 容器或音频库。

## Complexity Tracking

> 无宪法违规需要说明（constitution 为空白模板）。以下仅记录本计划中两个"超出最简"的取舍备查，不构成违规。

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 额外的 Core 类库项目（相对单项目） | 计时/睡眠数学需要注入虚拟时钟做自动化测试（SC-003） | 把逻辑写在 WPF 项目里会强制测试承载 WindowsDesktop 运行时，且无法可靠模拟睡眠 |
| 显式 PowerWatcher 服务 | 唤醒瞬间必须立即重算并响铃，不能等下一个 250ms 节拍 | 仅靠定时器轮询会在刚唤醒时给出最长一拍的陈旧显示，且违背 FR-009"唤醒立即提醒" |
