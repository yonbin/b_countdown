# Phase 0 Research: 始终置顶的悬浮倒计时器

**Date**: 2026-09-18 | **Feature**: 001-floating-countdown | **Spec**: [spec.md](spec.md)

技术栈在评估裁决中已锁定（WPF / .NET 8 / Windows 11），本文件记录实现层面的具体决策、理由与备选。Context7 现行文档（/dotnet/wpf，2026-09-18 查询）确认 WPF 正在开发 .NET 10 支持、自 .NET 6 起支持 ARM64、SDK 风格项目用 `UseWPF=true` 与 `-windows` TFM。

## R-1：UI 框架与运行时版本

- **Decision**: WPF on .NET 8 LTS，C# 12，XAML；`net8.0-windows`（App）与 `net8.0`（Core/Tests）。
- **Rationale**: 置顶无边框悬浮窗是 WPF 二十年的成熟能力；矢量渲染在高 DPI/任意缩放下大号数字清晰（FR-018）；.NET 8 为当前 LTS，支持自包含单文件发布与 ARM64。评估阶段已据此排除其他栈。
- **Alternatives considered**: WinUI 3（外观更新但未打包部署与透明置顶窗摩擦更多）；WinForms（现代简洁外观与高 DPI 成本高，违背"外观"验收）；Tauri/Electron（为单悬浮窗背负 WebView/Chromium 与额外工具链）。

## R-2：计时正确性 —— 绝对结束时刻，而非逐秒递减

- **Decision**: `CountdownSession` 开始时记录 `endTime = now + duration`（`DateTimeOffset`），剩余时间永远由 `endTime - now` 实时重算；`DispatcherTimer` 仅以 250ms 节拍触发重绘，**不承担计时**。暂停时记录冻结的剩余值，继续时以"当前时刻 + 冻结剩余"重建 `endTime`。
- **Rationale**: UI 节拍在系统睡眠期间不触发、在负载下会漂移；逐秒累减必然在睡眠/唤醒后少计。以绝对时刻为唯一真值，唤醒后第一次重算即自动正确（FR-006，SC-003）。250ms 节拍保证整秒翻转不迟滞超过一拍，且 CPU 近零。
- **Time source**: 通过 `TimeProvider`（.NET 8 内置抽象）注入；生产用 `TimeProvider.System`，测试用可手动推进的虚拟时钟。
- **Alternatives considered**: `System.Timers.Timer` 累减（睡眠丢拍，否决）；高精度 `Stopwatch` 测量已运行时长（可行但与挂钟时刻脱节，处理"跨睡眠/改系统时间"更绕）；用调度器等待结束时刻（唤醒时机不可靠，仍需轮询兜底）。
- **系统时间被手改**: 按 FR 边界用例接受即时跳变（以新系统时刻重算），不做单调时钟纠偏。

## R-3：睡眠/唤醒处理

- **Decision**: 订阅 `Microsoft.Win32.SystemEvents.PowerModeChanged`；`PowerModes.Resume` 时立即让会话重算一次，并由 ViewModel 刷新显示；若已过 `endTime`，立即进入 Finished（2026-09-18 取消 Overtime；2026-09-19 取消触发声音，仅视觉终态，FR-009）。
- **Rationale**: 仅靠 250ms 节拍，唤醒瞬间最长会有一拍陈旧显示，且"立即响铃"需要显式事件；该事件是 Win32 电源消息的托管封装，WPF 桌面应用可直接使用。
- **Alternatives considered**: 轮询时间差（拒绝理由见上）；计划任务/系统通知（违背"本地声音、不依赖通知通道"，且复杂）。
- **真机验收项**: 现代待机（S0）与传统睡眠（S3）机型各测一次短于/长于剩余时间的睡眠（quickstart.md）。

## R-4：声音提醒（2026-09-19 整体撤销）

> **撤销记录**：用户真机验收时认为循环提示音偏吵，2026-09-19 决定移除全部声音能力（见 spec.md「Session 2026-09-19」）。以下内容仅作决策史保留；代码中的 `AudioAlertService`、嵌入 wav 与 Core 的 `AlertState`/`Acknowledge()` 均已删除。

- **Decision（已撤销）**: 内置一个短促提示音 wav 作为嵌入资源，用 `System.Media.SoundPlayer.PlayLooping()` 循环；用户手动停止或满 2 分钟时 `SoundPlayer.Stop()`（FR-007/FR-008）。重置即停止（边界用例：声音不叠加）。
- **Rationale**: `SoundPlayer` 走应用自身的本地音频播放，不经 Windows 通知通道，勿扰/专注模式下不受抑制（FR-010）；零依赖、零网络、随单文件发布。
- **Alternatives considered**: `MediaPlayer`（支持 mp3，但基于 Media Foundation、对短音循环与即时停止更重）；NAudio（第三方依赖，杀鸡用牛刀）；Toast 通知音频（受勿扰与未打包应用注册限制，明确排除）。
- **已知限制（写入 quickstart）**: 系统总静音/音量为 0/无音频设备时无声；视觉"时间到"状态照常呈现。

## R-5：窗口形态 —— 无边框置顶悬浮窗

- **Decision**: 单窗口：`Topmost=true`、`WindowStyle=None`、`ShowInTaskbar=true`、`ResizeMode=CanMinimize`（或固定小尺寸 + 边缘拖拽改尺寸，最终在 tasks 阶段定一个，默认固定尺寸）、`WindowStartupLocation=Manual`；Win11 圆角通过无边框窗口的桌面管理器默认行为获得；拖动用窗口内 `DragMove()`；窗口不嵌任何按钮，全部操作（含退出）走右键菜单（2026-09-18 clarify 决定，见 FR-021）。
- **Rationale**: `Topmost` 即 WS_EX_TOPMOST，保证在所有普通窗口之上（FR-002）；无边框 + 自定义小块布局是"简洁耐看"与"小占地"的前提（FR-005、SC-007）。
- **位置记忆**: 关闭时保存窗口左上角与所在屏幕标识；启动时恢复，若目标屏幕已不存在或保存位置不在任何屏幕边界内，回退主显示器默认位置（FR-004、FR-018）。
- **DPI**: .NET Core 3.0+ 的 WPF 默认 Per-Monitor V2 感知，多屏混合缩放无需额外 manifest；仍需真机混合缩放验收。
- **Alternatives considered**: `AllowsTransparency=True` 透明层窗（首版不需要透明点击穿透；会带来额外的 WPF 性能/空域代价，留给 Option B）；AppBar 贴边（复杂且非需求）；置顶工具的"点击穿透"（明确列入非目标）。
- **已知边界**: 独占全屏（Exclusive Fullscreen）游戏之上不保证可见；录屏普通窗口化录制时可见且会入镜——均已在 spec Assumptions 声明。

## R-6：单实例

- **Decision**: 启动时创建 `Local\Countdown-SingleInstance` 命名 `Mutex`（2026-09-19 由 FloatingCountdown 改名），取不到则直接退出。
- **Rationale**: FR/Assumptions 要求单实例单倒计时；Local 前缀限定到本机会话，无需管理员权限。
- **Alternatives considered**: 命名管道/信号量（对本需求过重）。

## R-7：持久化

- **Decision**: `%AppData%\Countdown\settings.json`（2026-09-19 由 FloatingCountdown 目录改名；首启在新路径建文件，旧目录的位置记忆不迁移），`System.Text.Json` 直接序列化 `AppSettings`（首版仅窗口位置）；目录/文件缺失时回退默认值且不报错；写入采用临时文件替换避免半写。
- **Rationale**: 人类可读、零依赖、随用户配置走；不把设置写在 exe 旁边（单文件发布位置可能不可写）。
- **Alternatives considered**: 注册表（过度、不便携）；WPF Settings 设计器（对 SDK 风格项目不自然）；可移植 exe 旁文件（Program Files 下不可写）。

## R-8：时长输入解析

- **Decision**: 预设按钮 5/10/25 分钟一键开始；自定义输入接受"分钟"整数，以及带单位后缀的 `m`/`s`（如 `90`、`5m`、`90s`、`1m30s`）；解析失败或为 0 时禁止开始并给轻提示（FR-012/FR-013）。最大不设硬下限以外的限制；>99 小时显示美观不保证（spec 边界用例）。
- **Rationale**: 覆盖"最快两次点击"（SC-002）与任意时长；解析为纯函数放 Core，全部用单测枚举。
- **Alternatives considered**: 仅时分秒三个数字框（点击多、启动慢）；自然语言"一小时后"（过度设计）。

## R-9：测试策略

- **Decision**: Core 类库 xUnit 单测：`CountdownSessionTests`（状态迁移、暂停冻结、归零、2 分钟自动停由 ViewModel 层逻辑可注入时钟测试）、`SleepResumeTests`（虚拟时钟跳变模拟睡眠：剩余重算、跨过结束时刻 → Overtime）、`DurationParserTests`（合法/非法/边界）。UI 与声音不写自动化，走 quickstart 手工脚本。
- **Rationale**: 风险最高的是时间数学而非渲染；虚拟时钟可在毫秒内确定性地覆盖"睡眠数小时"场景。不引入 UI 自动化框架（small 胃口、单用户）。
- **Alternatives considered**: FlaUI/White 做窗口自动化（收益/成本不匹配，置顶与声音仍需真人确认）。

## R-10：构建与分发

- **Decision**: `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true`（ARM64 同理），产出免安装单 exe；首版不做安装包、不做自动更新（Assumptions）。
- **Rationale**: 自用、离线；自包含免去目标机运行时安装。wav 作为嵌入资源打进程序集，单文件内随附。
- **Alternatives considered**: MSIX/商店（非目标）；framework-dependent（要求目标机装 .NET 8，对自用换机不友好）。

## Resolved Unknowns 清单

- Technical Context 中无遗留 NEEDS CLARIFICATION：语言/版本、依赖、存储、测试、平台、性能、约束、规模均已确定。
- 留待真机（非设计阻塞）：S0/S3 两种睡眠行为、混合 DPI 多屏、录屏入镜观感、提示音音色主观接受度。
