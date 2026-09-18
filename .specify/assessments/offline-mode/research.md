# Idea Research: Windows 倒数计时器

- **Slug**: offline-mode
- **Created**: 2026-09-18
- **Evidence confidence (overall)**: medium（2026-09-18 补入用户本机实测的第一手证据，见文末「补证」；在线引用仍为零，竞品与平台文档仍待补）

> **研究环境说明**：本会话中 WebSearch 返回 403（账户未开通搜索），WebFetch 对 `github.com`、`stackoverflow.com`（均属 URL Trust Policy 白名单主机）的抓取被环境拦截（"Unable to verify if domain is safe to fetch"）。因此本轮**没有任何 `cited` 证据**：所有事实性陈述均按规则标记为 ASSUMPTION，置信度以作者背景知识为基础主观评定，不代表已核实。项目为全新空仓库，`.specify/` 中亦无既往规格或决策可供内部先例分析。

## Users & Demand

- 目前唯一的需求信号来自提议者本人的一句话意向陈述，没有第二个用户、工单、访谈或使用数据；"有多少人有此问题"完全未知 — [source: ASSUMPTION（依据 intake.md 原始输入）]（confidence: high——这是对"证据缺失"本身的确认）
- 桌面倒计时类工具的典型使用场景通常包括：番茄钟/专注、烹饪、会议或演讲、考试/测验、健身间歇；不同场景决定功能形态（多计时器、大字显示、置顶、提示音） — [source: ASSUMPTION]（confidence: medium）
- 大量同类免费应用在微软商店长期存在并积累评价，侧面说明该需求真实但已被充分满足、且用户付费意愿低 — [source: ASSUMPTION，本次无法核实任何具体下载量/评分数字]（confidence: low）

## Prior Art

- **Windows 内置"时钟"应用（Clock，旧称"闹钟和时钟"）已包含"计时器"功能**：支持自定义时长、多个并行计时器、命名、缩放显示；另含闹钟、秒表、"专注"会话（与 To Do 集成）。这是最强、最直接的免费内建替代品 — [source: ASSUMPTION（背景知识，未在线核实 Windows 11 当前版本的具体功能集）]（confidence: high）
- 第三方桌面倒计时工具生态成熟、多为免费：例如 **Hourglass**（开源 Windows 倒计时器，支持键盘输入时长、命令行参数）、**Orzeszek Timer**（轻量键盘驱动）、**SnapTimer**、各类多计时器（MultiTimer 类）应用 — [source: ASSUMPTION，名称凭记忆列出，仓库/官网地址未经核实故不记录 URL]（confidence: medium）
- GitHub 上存在大量基于 WPF/WinForms 的计时器示例与小型开源项目，但同质化严重、多数不活跃；未检索到具有显著网络效应的标杆产品 — [source: ASSUMPTION（GitHub 搜索在本环境不可用）]（confidence: low）
- 非桌面替代品：手机自带时钟、网页计时器（搜索引擎直接内置 timer）、智能音箱；跨设备场景下桌面端并非默认选择 — [source: ASSUMPTION]（confidence: medium）
- 内部先例：无。项目为空仓库，`.specify/` 下只有 Spec Kit 脚手架，无既有功能或历史决策 — [source: 仓库文件系统检查，2026-09-18]（confidence: high，cited：本地检查）

## Market & Context

- "什么都不做"的成本对用户几乎为零：Windows 开箱即有计时器，不足时商店有大量免费应用、浏览器有网页计时器。新产品必须有明确的差异化理由 — [source: ASSUMPTION]（confidence: high）
- slug 名为 `offline-mode`，暗示"离线/无网络"可能是拟议卖点；但本地计时器天然离线，**内置时钟应用同样离线**，离线本身在该品类中不构成差异化——除非指向更具体的场景（如内网/隔离环境、无账号、零遥测隐私诉求），目前未知 — [source: ASSUMPTION（基于 slug 与 intake 未知项的推断）]（confidence: medium）
- 平台生命周期：Windows 10 已于 2025-10-14 结束支持（背景知识），截至今日（2026-09）新应用以 Windows 11 为主要目标属常态；是否仍需兼容 Win10 用户是待确认的取舍 — [source: ASSUMPTION，未在线核实]（confidence: medium）

## Data & Constraints

- **计时准确性与睡眠**：普通 UI 定时器（如 DispatcherTimer 一类机制）存在漂移，且系统睡眠期间不计时；正确做法通常是记录目标时刻（绝对时间点）而非逐秒递减，并在唤醒/恢复时重算剩余时间。Windows 现代待机（Modern Standby, S0）与传统 S3 睡眠行为不同，后台计时与通知可靠性是此类应用的主要工程坑 — [source: ASSUMPTION，通用平台经验，未查当前文档]（confidence: medium）
- **通知**：Windows Toast 通知对未打包（unpackaged）桌面应用有额外要求（需 AppUserModelID / 开始菜单快捷方式注册）；专注助手/勿扰模式会抑制通知；锁屏与全屏游戏下的提醒可达性需要专门验证 — [source: ASSUMPTION]（confidence: medium）
- **分发形态**：可选 MSIX/微软商店上架（受商店政策约束）、winget、安装包或免安装单文件；纯本地单机应用通常无后端、无合规负担，隐私面小（若加遥测/联网则另当别论） — [source: ASSUMPTION]（confidence: medium）
- 目标架构（x64/ARM64）、最低 OS 版本、是否要求开机自启/托盘驻留/窗口置顶，均未定 — [source: ASSUMPTION：intake.md 未知项]（confidence: high——确认其为空白）

## Evidence Against the Idea

1. **最强反对理由：免费内建替代品已存在且开箱可用**——Windows 时钟应用的计时器覆盖了"设个倒计时"的基本需求，第三方工具也极多；在未陈述差异化场景前，构建理由薄弱。— [source: ASSUMPTION]（confidence: high）
2. **没有任何超出提议者本人的需求证据**；这可能是合理的自用/学习项目，但若目标是"给别人用的产品"，需求尚未被证明。— [source: ASSUMPTION]（confidence: high）
3. **品类红海、免费主导**，几乎不存在货币化空间；投入产出仅在自用、学习或特定小众场景下成立。— [source: ASSUMPTION]（confidence: medium）
4. **"离线"作为卖点在该品类中不稀缺**（本地计时器与内置应用本就离线）；slug 暗示的方向可能是个伪差异点。— [source: ASSUMPTION]（confidence: medium）
5. **"简单小工具"暗藏平台复杂度**：睡眠/唤醒、现代待机、通知抑制与权限、定时器漂移都需要实测打磨，低估成本是常见失败模式。— [source: ASSUMPTION]（confidence: medium）

## Gaps & Open Questions

- [NEEDS CLARIFICATION: 这是自用/学习项目，还是面向他人分发的产品？决定"需求证据"与差异化的评判标准]
- [NEEDS CLARIFICATION: 核心使用场景（专注番茄/烹饪/演讲/考试/其他）？不同场景的最小可用功能集差别很大]
- [NEEDS CLARIFICATION: 功能范围：单个倒计时 vs 多个并行计时器；预设、暂停/继续、循环、历史记录、自定义标签是否需要]
- [NEEDS CLARIFICATION: 提醒方式：声音、Toast 系统通知、弹窗、窗口置顶闪烁；锁屏/勿扰模式下必须多可靠]
- [NEEDS CLARIFICATION: `offline-mode` 到底指什么——纯本地无后端？零联网零遥测的隐私主张？目标使用环境是否为隔离/内网机器？]
- [NEEDS CLARIFICATION: 相对 Windows 内置时钟计时器，用户期望补上的具体痛点是什么？（这是成败关键）]
- [NEEDS CLARIFICATION: 目标平台：仅 Windows 11 x64，还是含 Windows 10 / ARM64]
- [NEEDS CLARIFICATION: 技术栈与分发方式有无偏好或约束；开发者对 WPF/WinUI/Web 技术栈的熟悉程度]
- [NEEDS CLARIFICATION: 对睡眠/关机/长时间后台的行为预期（暂停？唤醒后补提醒？错过提醒如何呈现）]
- 证据补强建议（联网恢复后执行）：核实 Windows 11 时钟应用当前计时器功能边界；微软商店同类应用的评分/评论样本；Hourglass 等开源项目的维护状态与技术栈；Microsoft Learn 关于 toast 通知与现代待机的现行文档。

## 补证：本机实测（2026-09-18，第一手，cited）

用户在本人 Windows 11 机器上完成 concept.md Option C 的核心实测（针对内置"时钟"应用），结论为**不满足需求**，具体差距三条：

1. **不能置顶**：计时器无法始终悬浮在其他应用之上——直接否掉本项目的核心需求（"剩余时间始终可见"）。— [source: 用户本机实测，2026-09-18]（confidence: high，cited）
2. **不是独立应用、功能太多**：时钟应用是闹钟/计时器/秒表/专注会话的集合体，用户想要的是单一、独立的小工具——形态不合，而非缺功能。— [source: 用户本机实测，2026-09-18]（confidence: high，cited）
3. **外观不满意（"样子太丑"）**：视觉上不愿长期放在桌面上——对"始终可见"类工具，外观是可用性的一部分（会因为嫌丑而不用）。— [source: 用户本机实测，2026-09-18]（confidence: high，cited；主观偏好，但对自用工具即为决定性标准）

影响：
- research 中原"最强反对理由（免费内建替代品已存在）"**被亲手削弱**：内建品在核心维度（置顶）不成立，且形态/外观差距确认。Option A 的立项理由从 ASSUMPTION 转为有第一手证据支撑。
- 仍未覆盖的实测项：**第三方免费工具**（Hourglass 等，部分本身支持置顶）未测试；**睡眠/唤醒后计时**与**到点提醒可靠性**未实测。对自用 small 胃口项目，这些可作为 specify/实现期的验收点，不再阻塞立项。
- 第 2、3 条差距为 Option A 增加了两条明确的首版验收维度：**单一独立形态**（无功能膨胀）与**简洁耐看的外观**（最小视觉打磨应作为首版范围的一部分，而非全部推给 Option B）。

## Sources

- 本地实测：用户本人 Windows 11 内置"时钟"应用计时器，2026-09-18（cited，第一手；上述「补证」一节）
- 无成功获取的外部来源。尝试记录如下（均未取得内容，不作为证据）：
  - `https://github.com/search?q=windows+countdown+timer...`（host: github.com，policy: allowlisted——环境拦截，未抓取）
  - `https://stackoverflow.com/questions/tagged/timer...`（host: stackoverflow.com，policy: allowlisted——环境拦截，未抓取）
  - WebSearch：403，搜索能力在当前账户不可用
- 本地来源：仓库文件系统检查（空项目；仅 Spec Kit 脚手架，无内部先例），2026-09-18
