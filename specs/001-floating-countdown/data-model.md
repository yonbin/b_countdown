# Data Model: 始终置顶的悬浮倒计时器

**Date**: 2026-09-18 | **Feature**: 001-floating-countdown | **Spec**: [spec.md](spec.md)

本特性无数据库、无网络、无多用户。运行期"数据"只有一个内存中的倒计时会话；持久化数据只有一个窗口位置设置。字段名是行为级描述，不等于最终代码符号。

## 实体 1：倒计时会话（Countdown Session）

一次倒计时活动，同一时刻全局唯一。

| 字段 | 含义 | 规则 |
|------|------|------|
| `state` | 当前状态 | 枚举：`Idle` / `Running` / `Paused` / `Finished`（2026-09-18 取消 Overtime 状态） |
| `duration` | 设定时长 | 正的时间长度；下限 1 秒；由 DurationParser 校验，非法不得进入 Running |
| `endTime` | 预定结束时刻（绝对挂钟时间） | Running 时唯一真值；`Idle`/`Paused` 时为空；继续时按"当前时刻 + remainingOnPause"重建 |
| `remainingOnPause` | 暂停瞬间冻结的剩余时长 | 仅 `Paused` 有意义；暂停期间不随现实时间变化 |
| `alertState` | 提醒音状态 | `Silent` / `Sounding`；归零置 Sounding，手动确认或重置或持续 2 分钟后回 Silent |

派生（读取时实时计算，不存储）：

- `remaining = endTime − now`：> 0 显示剩余；归零后进入 Finished 并恒为 0，**不向正计时方向累加**。

### 状态迁移

```text
                 设定合法时长 + 开始
        Idle ───────────────────────────────► Running
         ▲                                       │  │
         │重置(任何状态)                          │  │ now ≥ endTime
         │                                       │  │（含 Resume 时发现已超时）
         │        暂停             ┌─────────────┘  ▼
         └─────── Paused ◄─────────┤            Finished（红色 00:00，声音 Sounding）
                   ▲   └──────────► Running        │  │
                   └── 继续（重建 endTime）         │  │ 手动确认（停止声音，红色 00:00 保留）
                                                  ▼  ▼
                                              重置 → Idle（停止声音）
```

迁移规则：

| # | From | Event | To | 副作用 |
|---|------|-------|----|--------|
| T1 | Idle | 输入合法时长并开始 | Running | 记录 `duration`、`endTime=now+duration` |
| T2 | Running | 暂停 | Paused | 存 `remainingOnPause=endTime−now`，清空 `endTime` |
| T3 | Paused | 继续 | Running | `endTime=now+remainingOnPause` |
| T4 | Running | 节拍/Resume 重算发现 now ≥ endTime | Finished | 卡片转红、remaining 恒为 0、alertState=Sounding；无文字提示 |
| T5 | Finished | 用户确认 | Finished（红色 00:00 保留） | alertState=Silent；显示保持红色 00:00 直到重置/重开 |
| T6 | 任意 | 重置 | Idle | alertState=Silent，清空计时字段，不发声 |
| T7 | Running/Paused | 睡眠后 Resume | Running/Paused（状态不变） | 立即按挂钟重算：剩余正确（可能直接触发 T4） |
| T8 | Finished | 快速重开（右键菜单选预设或合法自定义时长，FR-019/FR-021） | Running | alertState=Silent（立即停音）→ 等价 T6 → 以新时长执行 T1 |

不变量：

- `Idle`、`Finished` 接受新时长设定（Finished 经 T8 快速重开：停音→重置→开始）；`Running`/`Paused` 不暴露时长设定，必须先重置（2026-09-18 clarify）。
- `Paused` 中 `remaining` 永远等于 `remainingOnPause`，与现实流逝无关（FR-011）。
- 任意状态下重置都必须使 alertState 变 Silent（边界用例：声音不叠加）。

## 实体 2：用户设置（App Settings）

跨启动保留的少量本地设置；单文件 JSON。

| 字段 | 含义 | 规则 |
|------|------|------|
| `windowLeft` / `windowTop` | 上次关闭时窗口左上角（设备无关像素） | 可空；首次启动为空 → 主显示器默认位置 |
| `screenId` / `screenBounds` | 上次所在显示器标识与边界 | 恢复时校验该位置仍落在某台现存显示器内；否则回退主显示器（FR-018） |

显式**不持久化**：进行中的计时、上次时长、暂停状态（FR-015：进程结束即全新开始）、任何账号/在线标识（不存在）。

## 校验规则汇总（来自 FR）

- 时长输入：空、0、负数、无法解析 → 拒绝，保持 Idle，UI 轻提示（FR-013）。
- 支持形式：纯数字按分钟；带后缀 `m`/`s` 及组合（如 `1m30s`）；预设 5/10/25 分钟。
- 显示格式：`<60min` 两张卡显示分、秒；≥1 小时三张卡显示时、分、秒；卡片无单位文字。
- 精度：剩余计算与现实挂钟误差 ≤1 秒（SC-003）。

## 错误与降级

- 设置文件缺失/损坏/目录不可写：回退默认设置并继续运行，不崩溃、不弹窗报错。
- 系统时间/时区在运行中被手改：下一拍起以新系统时刻重算，接受跳变（spec 边界用例）。
- 音频不可用：T4/T5 仍正常发生，仅声音缺失，视觉状态照常。
