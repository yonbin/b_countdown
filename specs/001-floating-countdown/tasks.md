---

description: "Task list for 001-floating-countdown implementation"
---

# Tasks: 始终置顶的悬浮倒计时器

**Input**: Design documents from `/specs/001-floating-countdown/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/window-behavior.md ✅, quickstart.md ✅

**Tests**: 包含。plan.md R-9 已决定为 Core 类库编写 xUnit 单元测试（注入虚拟时钟），这是 SC-003（睡眠/唤醒误差 ≤1 秒）的自动化保障。UI/声音/真实睡眠不写自动化，走 quickstart.md 手工验收。

**Organization**: 任务按用户故事分组（US1=P1 MVP，US2=P2，US3=P3），每个故事可独立实现与验收。

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 可并行（不同文件、不依赖未完成任务）
- **[Story]**: US1 / US2 / US3；Setup、Foundational、Polish 阶段不带故事标签
- 所有描述含确切文件路径

## Path Conventions（来自 plan.md）

- Core 类库：`src/Countdown.Core/`（net8.0，无 UI 依赖）
- WPF 应用：`src/Countdown.App/`（net8.0-windows）
- 测试：`tests/Countdown.Core.Tests/`（xUnit，net8.0）

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: 解决方案与三个项目骨架

- [X] T001 创建解决方案与三个项目（仓库根目录）：`dotnet new sln -n Countdown`；`dotnet new classlib -f net8.0 -n Countdown.Core -o src/Countdown.Core`；`dotnet new wpf -f net8.0-windows -n Countdown.App -o src/Countdown.App`；`dotnet new xunit -n Countdown.Core.Tests -o tests/Countdown.Core.Tests`；执行 `dotnet sln add` 三个项目、`dotnet add src/Countdown.App reference src/Countdown.Core`、`dotnet add tests/Countdown.Core.Tests reference src/Countdown.Core`。产物：`Countdown.sln`、`src/Countdown.Core/Countdown.Core.csproj`、`src/Countdown.App/Countdown.App.csproj`、`tests/Countdown.Core.Tests/Countdown.Core.Tests.csproj`
- [X] T002 [P] 配置 `src/Countdown.App/Countdown.App.csproj`：确认 `<OutputType>WinExe</OutputType>`、`<TargetFramework>net8.0-windows</TargetFramework>`、`<UseWPF>true</UseWPF>`、`<Nullable>enable</Nullable>`；添加 `<AssemblyName>Countdown</AssemblyName>` 与单文件发布属性说明注释（实际发布命令见 quickstart.md，不强写 PublishSingleFile 到默认构建）
- [X] T003 [P] 仓库卫生：删除模板文件 `src/Countdown.Core/Class1.cs` 与 xUnit 模板 `tests/Countdown.Core.Tests/UnitTest1.cs`；在仓库根创建 `.gitignore`（标准 .NET 模板：bin/、obj/、*.user、.vs/）
- [X] T004 [P] 添加提示音资源：放入一个短促、中性的 wav 到 `src/Countdown.App/Resources/alert.wav`，并在 `src/Countdown.App/Countdown.App.csproj` 中将其注册为 `<EmbeddedResource>`（供 R-4 的 SoundPlayer 使用）

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 所有故事共用的时间抽象（虚拟时钟可测试性的根基，R-2）

**⛔ CRITICAL**: 本阶段完成前不得开始任何用户故事

- [X] T005 在 `src/Countdown.Core/IClock.cs` 定义时钟抽象（薄封装 .NET 8 `TimeProvider`：暴露 `UtcNow`（DateTimeOffset）；生产实现 `src/Countdown.Core/SystemClock.cs` 委托 `TimeProvider.System`）。测试用可手动推进的虚拟时钟实现同一抽象，放在 `tests/Countdown.Core.Tests/FakeClock.cs`

**Checkpoint**: `dotnet build` 通过；时钟抽象就绪，用户故事可开始

---

## Phase 3: User Story 1 - 设定计时并始终看到剩余时间 (Priority: P1) ⭐ MVP

**Goal**: 启动后可设定时长开始倒计时；无边框小悬浮窗始终置顶显示大号剩余时间，可拖动并记住位置；切应用/最大化不被遮挡（FR-001…FR-005、FR-016/017/018 的可见性部分）

**Independent Test**: 按 quickstart.md 的 S1：设 1 分钟开始 → 切换/最大化编辑器与浏览器 → 悬浮窗全程可见、逐秒递减；拖动后关闭重开回到上次位置。

### Tests for User Story 1（先写测试并确认失败，再实现，R-9）

- [X] T006 [P] [US1] 在 `tests/Countdown.Core.Tests/CountdownSessionTests.cs` 编写开始/剩余时间测试：用 FakeClock 固定开始时刻，开始 10 分钟后 Remaining 为 10:00；推进虚拟时钟 65 秒后 Remaining 为 08:55；到达结束时刻 Remaining=0（先失败：类型尚不存在）
- [X] T007 [P] [US1] 在 `tests/Countdown.Core.Tests/DurationParserTests.cs` 编写基础解析测试：纯数字按分钟（`"25"`→25 分钟、`"1"`→60 秒）；非法输入 `""`、`"0"`、`"-5"`、`"abc"` 必须被拒绝（FR-013，最小合法时长 1 秒）
- [X] T008 [P] [US1] 在 `tests/Countdown.Core.Tests/TimeFormatterTests.cs` 编写显示格式测试：59 分钟内 `MM:SS`（如 `09:05`）、≥1 小时 `HH:MM:SS`（如 `01:00:00`）、Idle 占位 `--:--`

### Implementation for User Story 1

- [X] T009 [US1] 在 `src/Countdown.Core/SessionState.cs` 定义状态枚举（约束逐字采用 data-model.md：`Idle / Running / Paused / Finished / Overtime`），在 `src/Countdown.Core/CountdownSession.cs` 实现 Idle/Running 两个状态：构造注入 `IClock`；`Start(TimeSpan duration)` 记录 `endTime = clock.UtcNow + duration`；`Remaining` 实时返回 `endTime − now`（不为负，负值行为留给 US2）；`State` 属性。使 T006 通过
- [X] T010 [P] [US1] 在 `src/Countdown.Core/DurationParser.cs` 实现解析：纯数字=分钟；拒绝空/0/负数/无法解析；成功返回正 `TimeSpan`、失败返回明确的非法结果（不抛异常给 UI）。使 T007 通过
- [X] T011 [P] [US1] 在 `src/Countdown.Core/TimeFormatter.cs` 实现纯函数格式化（规则同 T008；另含 US2 将复用的"已超时"格式占位方法签名，本任务只做剩余/占位格式）。使 T008 通过
- [X] T012 [P] [US1] 在 `src/Countdown.Core/Settings/AppSettings.cs` 定义设置模型（仅 `WindowLeft`/`WindowTop`（double?，设备无关像素）、`ScreenId`/`ScreenBounds`（可空，用于多屏校验）；逐字遵循 data-model.md 实体 2，**不得**加入计时/时长字段）；在 `src/Countdown.Core/Settings/ISettingsStore.cs` 定义 `Load()/Save(AppSettings)` 接口
- [X] T013 [US1] 在 `src/Countdown.App/Services/JsonSettingsStore.cs` 实现 ISettingsStore：路径 `%AppData%\Countdown\settings.json`，用 `System.Text.Json`；目录/文件缺失或 JSON 损坏时返回默认设置且不抛异常；写入用"临时文件 + 替换"避免半写（R-7）
- [X] T014 [US1] 在 `src/Countdown.App/Services/WindowPlacementService.cs` 实现位置保存/恢复：启动时把窗口放到保存坐标；校验该点仍落在某台现存显示器边界内，否则回退主显示器右上留边距的默认位置（FR-018）；关闭时保存左上角与所在屏幕标识
- [X] T015 [US1] 在 `src/Countdown.App/App.xaml.cs` 与 `src/Countdown.App/App.xaml` 实现单实例：启动时创建/持有 `Local\Countdown-SingleInstance` 命名 `Mutex`，获取失败则立即退出（R-6）；移除 App.xaml 中 StartupUri，改为手工创建主窗
- [X] T016 [US1] 在 `src/Countdown.App/ViewModels/TimerViewModel.cs` 实现基础 ViewModel：持有 `CountdownSession`；暴露显示文本（经 TimeFormatter）与状态；用 `DispatcherTimer` 间隔 250ms 触发重新计算 `Remaining` 并刷新属性（定时器只重绘不计时，R-2）；暴露 `StartWithMinutes(int)`/`StartWithDuration(TimeSpan)` 与 Idle 输入命令
- [X] T017 [US1] 在 `src/Countdown.App/Views/TimerWindow.xaml` 与 `TimerWindow.xaml.cs` 实现无边框置顶悬浮窗：`Topmost=true`、`WindowStyle=None`、固定约 200×84 设备无关像素、主数字约 36–40px、大号高对比；显示面只有倒计时文本（FR-020/FR-021），无标题栏、无关闭按钮、无常驻控件；窗口区域 `DragMove()` 拖动；挂载右键 `ContextMenu`（本任务只需含"退出"并能保存位置后退出；各状态菜单项在 T032 完成），遵循 contracts/window-behavior.md
- [X] T018 [US1] 在 `src/Countdown.App/Views/TimerWindow.xaml.cs` 接线位置服务：启动经 WindowPlacementService 恢复位置，关闭时保存；应用退出即终止、不保留计时状态（FR-015）。确认窗口在 Per-Monitor V2 下默认 DPI 感知（.NET 8 WPF 默认，无需额外 manifest）

**Checkpoint**: US1 独立可用——执行 quickstart.md **S1**；Core 测试 `dotnet test` 全绿。此即 MVP，可在此停下试用

---

## Phase 4: User Story 2 - 到点声音提醒，睡眠唤醒后依然正确 (Priority: P2)

**Goal**: 归零时本地循环提示音（勿扰下仍响），2 分钟自动停；超时显示并累加；睡眠唤醒瞬间重算，跨过结束时刻则立即响铃（FR-006…FR-010）

**Independent Test**: 按 quickstart.md 的 S2（归零响铃/确认停/2 分钟自停/勿扰复测）与 S3（真机睡眠：短于与跨过剩余两种，误差 ≤1 秒）。

### Tests for User Story 2（先失败）

- [X] T019 [P] [US2] 在 `tests/Countdown.Core.Tests/SleepResumeTests.cs` 用 FakeClock 编写：开始 5 分钟 → 时钟向前跳 1 分钟（模拟睡眠）→ Remaining 为 4:00 且状态仍 Running；向前跳过结束时刻 2 分钟 → 状态 Finished、随后刷新进入 Overtime 且超时时长约 2:00 并随时钟推进累加；T8（Resume 重算）后误差为 0
- [X] T020 [P] [US2] 在 `tests/Countdown.Core.Tests/CountdownSessionTests.cs` 追加：到结束时刻触发 Finished（记录 finishedAt）、alertState=Sounding；任意状态 Reset 后 alertState=Silent（Reset 的完整实现在 US3，此处仅断言会话层标志）

### Implementation for User Story 2

- [X] T021 [US2] 扩展 `src/Countdown.Core/CountdownSession.cs`：`Refresh()`（按注入时钟重算，供节拍与 Resume 共用）；now≥endTime 时迁移 Running→Finished（记 `finishedAt`、alertState=Sounding），继续流逝→Overtime 并暴露 `Overtime`；添加 `AlertState`（Silent/Sounding）与 `Acknowledge()`（停音、保留终态，T6）。使 T019/T020 通过
- [X] T022 [P] [US2] 在 `src/Countdown.App/Services/AudioAlertService.cs` 实现本地提醒：从嵌入资源加载 alert.wav，`PlayLooping()` 循环、`Stop()` 停止；无音频设备/播放失败时静默降级、绝不抛异常（边界用例）。**不**使用 Toast/通知通道（FR-010）
- [X] T023 [US2] 在 `src/Countdown.App/ViewModels/TimerViewModel.cs` 接线提醒：进入 Finished/Overtime 时调用 AudioAlertService 播放；启动一个基于 IClock 的 2 分钟自停计时（FR-008），到时调用 Stop；暴露 Acknowledge 命令（停音、保留视觉终态）；暴露 Overtime 显示文本（"已超时 MM:SS"，复用 TimeFormatter 新增超载，补 1 条格式化测试到 `tests/Countdown.Core.Tests/TimeFormatterTests.cs`）
- [X] T024 [US2] 在 `src/Countdown.App/Services/PowerWatcher.cs` 封装订阅 `Microsoft.Win32.SystemEvents.PowerModeChanged`：收到 `PowerModes.Resume` 时触发事件；在 `src/Countdown.App/App.xaml.cs`（或 TimerWindow）订阅 → 立即 `session.Refresh()` 并让 ViewModel 应用新状态（必要时立即响铃，FR-009）；应用退出时 Dispose 取消订阅
- [X] T025 [US2] 在 `src/Countdown.App/Views/TimerWindow.xaml` 增加 Finished/Overtime 视觉态：醒目的"时间到"与"已超时 MM:SS"样式（区别于运行态）；确认停音经右键菜单（完整菜单在 T032，本任务先接通确认命令）；状态呈现遵循 contracts/window-behavior.md
- [X] T026 [US2] 执行手工验收 quickstart.md **S3**（含 S0/S3 两种待机模式如有条件）并记录结果；S3 失败则回到 research.md R-2/R-3 排查，禁止带已知睡眠缺陷继续。（原含 **S2** 声音验收，2026-09-19 声音移除后 S2 已改为视觉终态验收，见 T043）— **2026-09-19 真机通过**：ThinkPad X1 Carbon（LENOVO 21D1）S0 现代待机（连接）下短于/跨过剩余两种睡眠均正确；该机型固件不支持 S3，无法复测，记录为环境限制

**Checkpoint**: US1+US2 均独立可用；真机睡眠/唤醒正确、到点声音可靠

---

## Phase 5: User Story 3 - 暂停/继续/重置与快速设定 (Priority: P3)

**Goal**: 暂停冻结、继续顺延、一键重置（停音）；5/10/25 分钟预设一键开始 + `m/s` 自定义输入；普通窗口化录屏时悬浮指示可见（FR-011…FR-015、FR-017）

**Independent Test**: 按 quickstart.md 的 S4（暂停 10 秒不扣时；预设一键；`90`/`1m30s` 合法、`abc`/`0` 被拒）与 S5（录屏回放可见）。

### Tests for User Story 3（先失败）

- [X] T027 [P] [US3] 在 `tests/Countdown.Core.Tests/DurationParserTests.cs` 追加后缀与组合：`"5m"`=5:00、`"90s"`=1:30、`"1m30s"`=1:30、`"0s"` 与 `"m"` 非法
- [X] T028 [P] [US3] 在 `tests/Countdown.Core.Tests/CountdownSessionTests.cs` 追加暂停测试：Running 暂停存 remainingOnPause 且清空 endTime；虚拟时钟推进后剩余不减少；继续时 endTime 重建为 now+冻结值，继续后扣时正确（T2/T3）

### Implementation for User Story 3

- [X] T029 [US3] 扩展 `src/Countdown.Core/DurationParser.cs`：支持 `m`/`s` 后缀及组合（顺序 m 后 s），逐字采用 data-model.md 校验规则；使 T027 通过
- [X] T030 [US3] 扩展 `src/Countdown.Core/CountdownSession.cs`：`Pause()`（Running→Paused，存冻结值）、`Resume()`（Paused→Running，重建 endTime）、`Reset()`（任意状态→Idle，清空计时字段且 alertState=Silent，T7）；不变量：仅 Idle 接受新时长。使 T028 通过
- [X] T031 [US3] 在 `src/Countdown.App/ViewModels/TimerViewModel.cs` 暴露命令：Pause、Resume、Reset、StartPreset(5/10/25)、自定义输入 Start；Reset 与 Acknowledge 均调用 AudioAlertService.Stop（边界用例：声音不叠加）
- [X] T032 [US3] 在 `src/Countdown.App/Views/TimerWindow.xaml`（可加 `Views/CustomDurationDialog.xaml`）实现完整右键菜单（FR-021，按 contracts/window-behavior.md 逐项）：Idle 含 `5/10/25 分钟`（单击即开始）与 `自定义…`（弹出小输入框，非法时确认禁用并提示 `90`/`5m`/`1m30s` 等可接受格式，FR-013）；Running 含 `暂停`/`重置`；Paused 含 `继续`/`重置`；Finished/Overtime 含 `确认`、预设与自定义重开（FR-019，先停音再开始）、`重置`；所有状态含 `退出`；菜单项按状态启用/禁用，菜单空关不改变任何状态；常用预设开始 ≤2 次点击（SC-002）
- [X] T033 [US3] 执行手工验收 quickstart.md **S4** 与 **S5**（普通显示器录制，回放确认悬浮指示在画面中；"会入镜"为已接受行为）并记录结果 — **2026-09-19 真机通过**（用户确认）

**Checkpoint**: 三个用户故事全部独立可用

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: 外观主观验收、环境边界验证、发布形态

- [X] T034 [P] 在 `src/Countdown.App/Views/TimerWindow.xaml`（可加 `src/Countdown.App/Resources/Styles.xaml`）做默认外观打磨：简洁耐看的深浅中性配色、大号字形、留白、Win11 无边框圆角默认观感（SC-007）；不做主题系统、不做透明度/点击穿透（非目标）
- [X] T035 [P] 执行 quickstart.md **S6**（多显示器与 100%/150% 混合缩放，数字不模糊；拔副屏后回退主屏）与 **S8**（二次启动不出现第二实例；关闭重开是全新待设定但保留位置）并记录结果 — **2026-09-19 真机通过**（用户确认无问题）
- [X] T036 执行 quickstart.md **S7**（飞行模式下完整流程；用防火墙/资源监视器确认零外联，FR-016）与第 5 节发布验证：`dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`（再出 `win-arm64`），干净 Win11 免安装运行、提示音可用
- [X] T037 [P] 在仓库根 `README.md` 写最小运行说明（前置 .NET 8 SDK、`dotnet build`/`dotnet test`/`dotnet run --project src/Countdown.App`、发布命令），引用 quickstart.md
- [ ] T038 全量回归：`dotnet build -c Release` 零警告目标、`dotnet test` 全绿；顺序执行 quickstart.md S1–S8 全部通过并在文末留验收记录（日期、机型、待机模式）

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**：无依赖，立即开始；T002/T003/T004 在 T001 之后可并行
- **Phase 2 Foundational**：依赖 T001；⛔ 阻塞全部用户故事
- **US1（Phase 3）**：依赖 T005；无其他故事依赖——MVP
- **US2（Phase 4）**：复用 US1 的 CountdownSession/VM/窗口；其会话层测试 T019/T020 可与 US1 后期并行（不同测试文件），但实现 T021 依赖 T009
- **US3（Phase 5）**：复用 US1/US2 的 VM 与窗口；T027/T028 测试文件独立，可在 US2 进行时并行编写
- **Phase 6 Polish**：依赖所有目标故事完成

### User Story Dependencies

- **US1 (P1)**：Foundational 后即可开始，不依赖任何故事
- **US2 (P2)**：需要 US1 的会话/窗口骨架，但独立交付"声音+睡眠正确性"价值
- **US3 (P3)**：需要 US1 的窗口与（为 Reset 停音）US2 的 AudioAlertService

### Within Each User Story

- 测试任务先写并确认失败（TDD），再写实现
- Core（模型/纯逻辑）→ App 服务 → ViewModel → 窗口 XAML → quickstart 手工验收
- 每个 Checkpoint 处必须独立验收通过才进入下一阶段

### Parallel Opportunities

- Phase 1：T002、T003、T004 并行（不同文件）
- US1：T006/T007/T008 三个测试文件并行；T010/T011/T012 并行（Core 中不同文件）
- US2：T019 与 T020 并行；T022 与 T021 并行（AudioAlertService 不依赖会话扩展）
- US3：T027 与 T028 并行
- Polish：T034 与 T035/T037 并行

---

## Parallel Example: User Story 1

```text
# 三个测试文件同时开工（互不依赖，都会先失败）：
Task T006: tests/Countdown.Core.Tests/CountdownSessionTests.cs
Task T007: tests/Countdown.Core.Tests/DurationParserTests.cs
Task T008: tests/Countdown.Core.Tests/TimeFormatterTests.cs

# 随后三个 Core 文件并行实现：
Task T010: src/Countdown.Core/DurationParser.cs
Task T011: src/Countdown.Core/TimeFormatter.cs
Task T012: src/Countdown.Core/Settings/AppSettings.cs (+ISettingsStore.cs)
```

---

## Implementation Strategy

### MVP First（仅 User Story 1）

1. 完成 Phase 1 + Phase 2
2. 完成 Phase 3（US1），跑通 quickstart S1、Core 测试全绿
3. **停下来自用一两天**：验证 SC-001（遮挡 0 次）与 SC-007（是否愿意长期放在桌面）；置顶/外观不成立则先改 US1，不向后堆功能

### Incremental Delivery

1. Setup + Foundational → 地基
2. US1 → 始终可见的倒计时（MVP）
3. US2 → 到点不漏接、睡眠可信（真机 S2/S3 验收）
4. US3 → 操作顺手、预设快速（S4/S5 验收）
5. Phase 6 → 外观主观关、零网络/多屏/单文件发布、全量回归

### 说明（单开发者）

本项目单用户单开发者，"并行"指任务之间无文件冲突、可在同一阶段任意穿插，而非多人分工。

---

## Notes

- [P] = 不同文件、无未完成依赖
- 每个任务的行为约束以 spec.md 与 contracts/window-behavior.md 为准；数据字段/状态迁移逐字引用 data-model.md，不得临场增删
- 测试先确认失败再实现；每个逻辑组完成后提交（如启用 git）
- 真机睡眠（S3 任务 T026）是最高风险验收项，失败不得降级放行
- 技术决策依据见 research.md R-1…R-10；遇到任务内未决细节回到该文件，不引入第三方 UI/MVVM/音频库

---

## Implementation Notes（2026-09-18，/speckit-implement 执行结果）

**已自动化验证**：`dotnet build` 与 `dotnet test -c Release` 0 警告 0 错误；43/43 Core 单测通过（含虚拟时钟睡眠/超时场景）；Debug 与 Release 构建均成功；win-x64 自包含单文件发布成功（155 MB）。已机器验证：窗口正常启动/关闭、关闭即写 `%AppData%\Countdown\settings.json`、单实例（第二次启动 code=0 退出、仅 1 个实例）、运行期 TCP 连接数为 0（零联网）、alert.wav 嵌入程序集可被加载。

**仍需真人验收（对应未勾选任务，无法在无头会话中完成）**：

- T026 → quickstart **S3**（真机睡眠/唤醒，建议真的合盖测一次短于与跨过剩余的场景；代码路径已被单测模拟覆盖，但电源事件只能真机确认）。原 **S2** 声音/勿扰复测已随 T043 撤销，新 S2 为到点视觉终态目检
- T033 → **S4**（右键菜单实际操作手感）与 **S5**（录屏回放可见性/入镜确认）
- T035 → **S6**（本机虚拟屏宽 4480，具备多屏条件；请拖到不同缩放比例的屏幕验证清晰度与拔屏回退）与 **S8** 中"重启后是全新待设定状态"的目视确认
- T038 → 顺序执行 S1–S8 并在本文件或 quickstart.md 留验收记录；win-arm64 单文件发布未在本机执行（x64 无法烟雾测试 ARM64，命令见 README，参数替换即可）

**环境偏差**：计划写 .NET 8 SDK；本机实际只有 .NET 10 SDK（运行时含 8.0.31 Desktop）。已用 SDK 10 构建 net8.0 目标，产物在装有 .NET 8 桌面运行时的机器上可运行，自包含发布不依赖目标机运行时。

---

## Phase 7: Convergence（2026-09-18，/speckit-converge）

- [X] T039 [US1] 放宽时长上限以符合"不限制最大时长"的边界约定 per spec 边界用例·超长计时 (contradicts)：修改 `src/Countdown.Core/DurationParser.cs`，将 >99 小时硬拒绝改为溢出安全上限（建议 ≤ 9999 小时）；在 `tests/Countdown.Core.Tests/DurationParserTests.cs` 增加"100 小时可解析、超上限/溢出输入仍拒绝"用例；>99 小时的显示美观维持"不保证"
- [X] T040 [US1] 统一 Idle 占位呈现与契约描述 per contracts/window-behavior.md、FR-020 (partial)：二选一——(a) 将 `src/Countdown.App/ViewModels/TimerViewModel.cs` 的 IdleCards 改为置灰 `--/--` 并确认 46px 下破折号渲染正常（此前在透明分层窗截图中呈块状，需真机目检）；或 (b) 保留 00 卡片，走一次规格/契约修订把 `--:--` 占位描述改为置灰 00；完成后在本任务后记录采用的方案 ——已采用方案 (b)：保留置灰 00 卡片，规格 Clarifications 与窗口契约已修订
- [X] T041 [US1] 窗口位置恢复改为整体矩形校验 per FR-004/FR-018 (partial)：修改 `src/Countdown.App/Services/WindowPlacementService.cs`，用恢复位置与窗口尺寸构造矩形，要求其与某台现存显示器工作区有足够交集（如至少 60×40 设备无关像素在屏内），否则回退主显示器右上默认位；可在 `AppSettings` 既有字段内完成，无需新增持久化字段

---

## Phase 8: Convergence（2026-09-18，/speckit-converge 第二轮）

- [X] T042 核对常驻内存目标 per plan.md Technical Context「<80MB 内存」 (partial)：Release 实测 Private Memory ≈85 MB、WS ≈147 MB（启动 212ms 达标）；先确认是否为 WPF 基线（长时间静置后复测、检查是否有随节拍增长的对象），若属基线则在 plan.md 该目标处记录"接受约 85MB"的偏差与理由（converge 无权改 plan，由 implement 执行），若存在可回收占用则优化后复测；冷启动 <2s 已满足无需处理

### 机器辅助验证记录（2026-09-18，UIA 自动化，不替代真人验收）

- **发现并修复一个真实缺陷**：`RelayCommand.CanExecuteChanged` 原本未挂接 `CommandManager.RequerySuggested`，导致右键菜单的暂停/继续/确认/重置项在首次求值（Idle）后永不刷新——到点后"确认/重置"保持禁用。已修复（`src/Countdown.App/ViewModels/RelayCommand.cs`），修复后 UIA 实测：到点状态菜单"确认"与"重置"均 enabled，确认→重置→退出链路通过。
- UIA 端到端跑通：右键→自定义→输入 `10s`→倒计时开始→出现"时间到/已超时"（会话层 Sounding 状态成立，PlayLooping 被调用）；进程经"退出"干净结束。UIA 对透明分层窗的文本枚举不稳定（只影响测试脚本，不影响应用）。
- 仍属真人验收范围（故 T026/T033/T035/T038 保持未勾选）：提示音**是否真的听到**（含勿扰）、**真合盖睡眠/唤醒**、**录屏回放**可见性、混合缩放下的**肉眼清晰度**、S1–S8 完整记录与 arm64 发布。

### 修复记录：自定义时长回车无效（2026-09-18，用户反馈"自定义时间无法保存"）

- **根因（双重）**：① "开始"按钮未设为默认按钮，输入框里按回车不提交；② 用户使用小鹤双拼中文输入法，在输入框组词时第一个回车被 IME 吞掉用于确认组词。现象：输完按回车对话框无反应，像"保存不了"（鼠标点"开始"其实可用）。
- **修复**（`src/Countdown.App/Views/CustomDurationDialog.xaml(.cs)`）：输入框 `InputMethod.IsInputMethodEnabled="False"`（时长只需 ASCII）；"开始"按钮加 `IsDefault`；输入框显式处理 Enter 键直接提交（规避从右键菜单打开对话框时默认按钮路由的焦点怪癖）；Esc 仍为取消。
- **验证**：真实键盘驱动 UIA 测试——打开右键→自定义→输入 `90s`→单次回车，对话框关闭且 1:30 倒计时开始递减；Release 构建 0 警告，48/48 单测通过。

### 修改记录：去单位标签、取消正计时与结束文案（2026-09-18，用户反馈）

- 卡片去掉 min/sec/hr 文字标签，窗口高度收到约 100 DIP；移除 CardPart.Unit 与下方说明文字。
- **取消 Overtime**：归零后停在红色 00:00，不再向正计时累加；删除 `SessionState.Overtime`、`finishedAt`、`TimeFormatter`（不再被生产代码引用）及其测试；相关会话/睡眠测试重写。
- **结束态无任何文字**：移除"时间到/已超时"Caption；结束仅以红色卡片 + 声音表达。
- 顺带修复真实视觉 bug：卡片背景触发器在 DataTemplate 内裸绑 `{Binding State}`（DataContext 是 CardPart），导致暂停/结束配色从不生效；改为 RelativeSource 绑定窗口 VM 的 State（已截图验证结束红卡）。

---

## Phase 9: Convergence（2026-09-19，用户真机反馈后移除声音）

- [X] T043 整体移除到点声音能力（用户 T026 真机验收时反馈循环提示音偏吵）：
  - **App**：删除 `Services/AudioAlertService.cs`、`Resources/alert.wav`（含 Resources 目录）与 csproj 的 `<EmbeddedResource>`；`App.xaml.cs` 去掉创建/Dispose；`TimerViewModel` 删除 AudioAlertService 接线、2 分钟自停窗口（`_wasSounding`/`_alertStartedAt`/`SyncAlert`）与 `AcknowledgeCommand`，构造函数收紧为 `(CountdownSession)`；`TimerWindow.xaml` 删除右键菜单"确认"项。
  - **Core**：删除 `AlertState` 枚举与 `CountdownSession.Acknowledge()`，`Start/Reset/Refresh` 不再维护提醒标志；同步改写 `CountdownSessionTests`/`SleepResumeTests`（净减 1 个测试，37/37 通过）。
  - **文档**：spec.md 增「Session 2026-09-19」裁决并修订 Input/US2/边界用例/FR-007/FR-009/FR-014/FR-019/FR-021/SC-004/Assumptions（FR-008、FR-010 标记撤销）；contracts/window-behavior.md、data-model.md（去 alertState 字段、T5 标撤销）、quickstart.md（S2 重写为视觉终态、S3/S4/S7 与发布验证去声音）、plan.md、research.md（R-4 标撤销）、checklists/requirements.md（加修订说明）、README 同步；T026 验收范围缩减为 S3。
  - **验证**：`dotnet build -c Release` 0 警告 0 错误；`dotnet test -c Release` 37/37 通过。到点行为现为：卡片转红、停在 00:00，终态保持至重置/快速重开。
- 文档已同步：spec（FR-005/009/020/021、US2、边界用例、Clarifications）、data-model（状态与迁移表）、contracts/window-behavior、quickstart。Release 0 警告；Core 单测 38/38 通过。

---

## Phase 10: 重命名（2026-09-19，用户决定旧名 FloatingCountdown → 新名 Countdown）

- [X] T044 方案/产物全部重命名为 Countdown：
  - 解决方案 `FloatingCountdown.slnx` → `Countdown.slnx`；三个项目目录与 csproj 重命名：`src/Countdown.App`（原 `src/FloatingCountdown.App`）、`src/Countdown.Core`、`tests/Countdown.Core.Tests`（git mv 保留历史）。
  - 全部命名空间 `FloatingCountdown.*` → `Countdown.*`；XAML `x:Class`、窗口 Title、manifest assemblyIdentity 同步；程序集名 `Countdown`（产物 **Countdown.exe**）。
  - 运行期标识：单实例互斥体 `Local\Countdown-SingleInstance`（原 `Local\FloatingCountdown-SingleInstance`）；设置目录 `%AppData%\Countdown\settings.json`（原 `%AppData%\FloatingCountdown\`，不迁移，首启位置回到默认位一次）。
  - 文档：README、quickstart 构建/运行/发布命令、plan 文件树与存储路径、research R-6/R-7、tasks.md 历史条目路径均同步为新名；仅本段改名对照保留旧名。
  - 特性规格目录 `specs/001-floating-countdown/` 名称保留（特性编号，不随产物改名）。
  - **验证**：`dotnet build Countdown.slnx -c Release` 0 警告 0 错误，产物为 `Countdown.exe`；`dotnet test` 37/37 通过。
