# ReinforcementGate 设计规格

## 文档状态

- 状态：已完成分段评审，等待用户对书面规格做最终确认
- 日期：2026-08-14
- 项目名：ReinforcementGate
- 建议仓库名：`SCPSL-ReinforcementGate`
- 目标产物：`ReinforcementGate.dll`
- 技术路线：LabAPI 1.1.7

## 1. 项目概述

ReinforcementGate 是一个 SCP: Secret Laboratory 服务端插件。管理员可以通过 Remote Admin 命令独立控制九尾狐和混沌分裂者的主支援波、迷你支援波，也可以全局停止支援或一次性跳过下一次匹配的支援。

插件只拦截未来的支援刷新。已经刷新并存活的玩家不会被移除、处决、改变角色或改变阵营。

支援控制状态只保存在内存中，每回合开始自动恢复默认状态。通知模板保存在插件配置文件中，可分别选择 Broadcast、CASSIE、两者同时或关闭。

## 2. 目标与非目标

### 2.1 目标

- 独立控制四类支援：`ntf`、`ntf-mini`、`ci`、`ci-mini`。
- 提供不覆盖分类状态的全局停止开关。
- 支持全局或分类的一次性跳过。
- 仅通过 Remote Admin 命令管理运行时状态。
- 每回合开始恢复全部允许并清除待执行跳过。
- 支持可配置的 BC/CASSIE 通知模板。
- 提供强类型公共 API，供其他插件查询、控制和监听。
- 单独开放只读 `ReinforcementStatesApi`，供跨插件查看当前状态。
- 在 GitHub `README.md` 中完整记录使用方式、配置和 API。

### 2.2 非目标

- 不修改支援计时器、影响力、支援令牌或波次人数。
- 不按实际刷新人数猜测大支援或小支援。
- 不保存或跨回合恢复运行时开关。
- 不处理已刷新的玩家。
- 不提供玩家控制台命令、客户端界面或网页管理面板。
- 不负责安装、升级或配置 LabAPI/游戏服务器环境。

## 3. 机制依据与类型判定

官方刷新机制将 Mini-wave 定义为主支援之后通过影响力条件获得的独立支援波。服务器人口除以五是迷你支援的人数上限，不是区分大小支援的判据。

插件必须根据 LabAPI 的支援波封装类型分类：

| LabAPI 类型 | 命令目标 | 中文名称 |
| --- | --- | --- |
| `MtfWave` | `ntf` | 九尾狐大支援 |
| `MiniMtfWave` | `ntf-mini` | 九尾狐小支援 |
| `ChaosWave` | `ci` | 混沌大支援 |
| `MiniChaosWave` | `ci-mini` | 混沌小支援 |

未知类型必须放行并记录一次警告，不能因为框架或游戏未来新增波类型而误拦截其他刷新系统。

参考资料：

- [SCP:SL 官方刷新机制](https://en.scpslgame.com/index.php?stable=1&title=Spawning_Mechanics)
- [LabAPI 四类支援波映射](https://github.com/northwood-studios/LabAPI/blob/7b837be2ce8012f12b2d1dd80c52c4c11ba4e0c3/LabApi/Features/Wrappers/Facility/Respawning/RespawnWaves.cs)
- [LabAPI 可取消的 WaveRespawning 事件](https://github.com/northwood-studios/LabAPI/blob/7b837be2ce8012f12b2d1dd80c52c4c11ba4e0c3/LabApi/Events/Arguments/ServerEvents/WaveRespawningEventArgs.cs)
- [LabAPI 1.1.7 发布页](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.7)

## 4. Remote Admin 命令

主命令为 `reinforcement`，别名为 `rf`。

```text
reinforcement status
reinforcement enable <target>
reinforcement disable <target>
reinforcement skip <target>
reinforcement reset
```

有效目标：

```text
all
ntf
ntf-mini
ci
ci-mini
```

示例：

```text
rf status
rf disable ntf
rf disable ntf-mini
rf disable all
rf enable all
rf skip ci
rf skip ci-mini
rf skip all
rf reset
```

`status` 不检查 `PlayerPermissions.RespawnEvents`；由于命令只注册到 `RemoteAdminCommandHandler`，任何能够在 Remote Admin 中执行命令的人员都可以查看状态。

`enable`、`disable`、`skip`、`reset` 必须检查 `PlayerPermissions.RespawnEvents`。权限不足时返回明确错误，不改变状态，也不发送通知。

命令成功响应必须包含：执行动作、目标、变更前状态、变更后状态和当前有效状态。这样在全局停止仍开启时，管理员执行 `rf enable ntf` 能看到“分类状态已恢复，但仍受全局停止影响”。

## 5. 状态模型与行为

### 5.1 内存状态

状态服务保存：

- 一个全局持续停止标记。
- 四个分类允许标记。
- 一个全局一次性跳过标记。
- 四个分类一次性跳过标记。
- 每个持续状态和一次性跳过的来源字符串，用于通知和审计事件。

默认状态：

- 全局持续停止关闭。
- 四个分类全部允许。
- 所有一次性跳过均未设置。

### 5.2 全局与分类状态

- `rf disable all` 只开启全局停止，不覆盖四个分类标记。
- `rf enable all` 只解除全局停止，不覆盖四个分类标记。
- `rf reset` 解除全局停止、恢复四个分类并清空全部一次性跳过。
- `rf enable <分类>` 在全局停止期间仍会修改该分类的保存状态，但其有效状态仍是禁止。

### 5.3 回合边界

监听 `ServerEvents.RoundStarted`。每次回合开始执行无通知的内部重置，并发布 `RoundStateReset` 公共事件。

插件启用或重载时也初始化为默认状态，不能沿用已卸载实例的状态。

### 5.4 支援拦截顺序

监听 `ServerEvents.WaveRespawning`。处理顺序固定如下：

1. 将 `ev.Wave` 分类为四类目标之一。
2. 未知类型：放行并记录一次警告。
3. 全局持续停止开启：设置 `ev.IsAllowed = false`，不消耗任何一次性跳过。
4. 对应分类关闭：设置 `ev.IsAllowed = false`，不消耗任何一次性跳过。
5. 对应分类的一次性跳过已设置：清除该分类跳过并取消刷新。
6. 否则，全局一次性跳过已设置：清除全局跳过并取消刷新。
7. 都未命中：正常放行。

当 `rf skip all` 和 `rf skip ntf` 同时存在时，下一次 NTF 支援只消耗 `ntf`；全局跳过保留给之后的下一次可刷新支援。由此保证每条跳过指令实际对应一波独立的拦截。

持续停止期间发生的刷新尝试不会消耗待执行跳过。解除持续停止后，待执行跳过仍然生效。

## 6. 内部架构

### 6.1 `ReinforcementPlugin`

- 声明插件元数据和 `RequiredApiVersion`。
- 创建并连接服务。
- 注册 `WaveRespawning`、`RoundStarted` 事件。
- 卸载时注销全部事件并释放静态 API 入口。

### 6.2 `ReinforcementStateService`

- 单一管理运行时状态。
- 提供查询快照和状态变更操作。
- 执行重复操作检测。
- 保证命令和公共 API 使用同一套状态转换规则。

### 6.3 `WaveClassifier`

- 只负责 LabAPI 波类型到 `ReinforcementTarget` 的映射。
- 不读取人数、角色列表或文本名称。
- 对未知类型返回明确的未知结果。

### 6.4 `WaveInterceptionService`

- 按固定优先级计算本波是否允许。
- 只在最终决定为禁止时修改 `ev.IsAllowed`。
- 生成包含目标、原因、来源和已消费跳过项的不可变结果。
- 先完成支援决策，再调用通知服务；通知失败不能反向改变决策。

### 6.5 `NotificationService`

- 根据通知节点、模式和模板构造消息。
- BC 使用 `Server.SendBroadcast(...)`。
- CASSIE 使用 `Announcer.Message(...)`，不使用已弃用的 `Cassie` 类。
- 只替换白名单占位符，不执行表达式或任意格式化代码。
- 单个通知通道失败时隔离错误，另一个通道仍可继续。

### 6.6 `ReinforcementCommand`

- 注册到 `RemoteAdminCommandHandler`。
- 主命令 `reinforcement`，别名 `rf`。
- 对状态修改子命令执行 `RespawnEvents` 权限检查；`status` 仅要求调用者来自 Remote Admin。
- 负责参数解析和统一响应格式。
- 不直接修改字段，只调用状态服务或公共 API 门面。

### 6.7 `ReinforcementStatesApi`

- 作为跨插件只读状态入口公开。
- 返回不可变快照，不暴露内部字典、集合或状态服务实例。
- 不执行权限检查，因为它不提供状态修改能力。
- 插件未启用或正在卸载时返回明确的不可用结果，不能抛出难以诊断的空引用异常。

## 7. 通知配置

插件配置只保存通知和显示设置，不保存支援控制状态。

每个通知节点支持四种模式：

```text
None
Broadcast
Cassie
Both
```

建议默认配置结构：

```yaml
notifications:
  enable_applied:
    mode: Broadcast
    broadcast:
      message: "<color=green>{target_name} 已恢复刷新</color>"
      duration: 8
      clear_previous: false
    cassie:
      message: "REINFORCEMENT ENABLED"
      subtitles: "{target_name} 已恢复刷新"
      play_background: true
      priority: 0
      glitch_scale: 0

  disable_applied:
    mode: Both
    broadcast:
      message: "<color=red>{target_name} 已停止刷新</color>"
      duration: 8
      clear_previous: false
    cassie:
      message: "REINFORCEMENT SUSPENDED"
      subtitles: "{target_name} 已停止刷新"
      play_background: true
      priority: 0
      glitch_scale: 0

  disabled_wave_blocked:
    mode: None

  skip_armed:
    mode: Broadcast
    broadcast:
      message: "下一次 {target_name} 支援将被跳过"
      duration: 8
      clear_previous: false

  skip_triggered:
    mode: Both
    broadcast:
      message: "{target_name} 支援已被跳过"
      duration: 8
      clear_previous: false
    cassie:
      message: "REINFORCEMENT WAVE CANCELLED"
      subtitles: "{target_name} 支援已被跳过"
      play_background: true
      priority: 0
      glitch_scale: 0
```

`enable_applied`、`disable_applied` 和 `skip_armed` 在状态实际发生变化后发送。重复执行未造成状态变化时不发送。

`skip_triggered` 只在一次性跳过真正拦截一波时发送。`disabled_wave_blocked` 在持续停止实际拦截一波时发送，默认关闭以防刷屏。

公开占位符：

| 占位符 | 含义 |
| --- | --- |
| `{target}` | `all`、`ntf`、`ntf-mini`、`ci`、`ci-mini` |
| `{target_name}` | 目标的中文可读名称 |
| `{admin}` | RA 管理员名称或公共 API 提供的来源 |
| `{action}` | `enable`、`disable`、`skip` |
| `{reason}` | `global-disabled`、`target-disabled`、`skip` |

空 BC/CASSIE 文案只跳过对应通道。配置解析、模板替换或消息发送失败必须记录配置路径和错误，但不能中断插件控制逻辑。

LabAPI 重新加载配置时，通知服务必须原子替换为新模板；状态开关和待执行跳过不受配置重载影响。

## 8. 公共 API

公共 API 采用强类型目标和不可变快照，不暴露内部集合或可变状态。查询与控制使用两个独立入口，其他插件可以只引用只读状态 API。

### 8.1 类型

```text
ReinforcementTarget
  All
  Ntf
  NtfMini
  Ci
  CiMini

ReinforcementBlockReason
  GlobalDisabled
  TargetDisabled
  TargetSkip
  GlobalSkip

ReinforcementStateAction
  Enable
  Disable
  ArmSkip
  ClearSkip
  ConsumeSkip
  Reset
  RoundReset

ReinforcementTargetState
  Target
  IsLocallyEnabled
  IsEffectivelyEnabled
  IsSkipArmed
  EnabledLastChangedBy
  SkipLastChangedBy

ReinforcementStateSnapshot
  IsGlobalDisabled
  IsGlobalSkipArmed
  GlobalDisabledLastChangedBy
  GlobalSkipLastChangedBy
  Targets
```

`ReinforcementTargetState`、`ReinforcementStateSnapshot` 及其 `Targets` 集合必须不可变。`Targets` 包含 `Ntf`、`NtfMini`、`Ci`、`CiMini` 四项，不把 `All` 伪装成普通分类。

### 8.2 只读状态 API

对外公开类型名：

```text
ReinforcementStatesApi
```

公开成员：

```text
ReinforcementStatesApi.IsAvailable
ReinforcementStatesApi.GetSnapshot()
ReinforcementStatesApi.GetState(target)
ReinforcementStatesApi.TryGetState(target, out state)
ReinforcementStatesApi.StateChanged
ReinforcementStatesApi.RoundStateReset
```

- `IsAvailable` 表示 ReinforcementGate 插件是否已经启用并完成状态服务注册。
- `GetSnapshot()` 返回全局状态及四类状态的一致性快照。
- `GetState(target)` 只接受四个分类目标；传入 `All` 或未知值时抛出带参数名的 `ArgumentOutOfRangeException`。
- `TryGetState(...)` 对无效目标返回 `false`，不抛异常。
- 插件不可用时，`GetSnapshot()` 和 `GetState(...)` 抛出说明插件未就绪的 `InvalidOperationException`；`TryGetState(...)` 返回 `false`。
- 状态查询不触发通知、不发布新事件，也不要求 `RespawnEvents` 权限。

### 8.3 控制 API

```text
ReinforcementControlApi.SetEnabled(target, enabled, source)
ReinforcementControlApi.ArmSkip(target, source)
ReinforcementControlApi.ClearSkip(target, source)
ReinforcementControlApi.Reset(source)
```

`source` 是非空审计字符串。外部插件调用和 RA 命令调用必须经过相同的校验、状态转换、通知与事件发布流程。

`ArmSkip(All, source)` 和 `ClearSkip(All, source)` 只操作全局一次性跳过，不批量修改四个分类跳过。需要清除全部持续状态和跳过时调用 `Reset(source)`。控制操作后的状态通过返回结果或 `ReinforcementStatesApi` 查询。

API 方法是同步的，调用方必须在游戏服务器主线程使用。该约束必须写入 XML 文档和 `README.md`。

### 8.4 事件

```text
ReinforcementStatesApi.StateChanged
ReinforcementStatesApi.RoundStateReset
ReinforcementEvents.WaveBlocked
```

- `ReinforcementStatesApi.StateChanged`：状态真正发生变化后发布，事件参数包含变更前快照、变更后快照、动作、目标和来源。
- `ReinforcementStatesApi.RoundStateReset`：回合开始完成内部重置后发布。
- `ReinforcementEvents.WaveBlocked`：支援已经确定被拦截后发布，包含目标、原因和来源。

事件参数必须不可变。监听者不能通过事件参数撤销插件已经做出的支援决定。

`reset` 和回合开始重置都是原子状态转换：无论内部有多少字段发生变化，都只发布一次 `StateChanged`。回合开始随后再发布一次 `RoundStateReset`；RA/API 主动执行 `reset` 不发布 `RoundStateReset`。

## 9. 错误处理

- 无 `RespawnEvents` 权限执行状态修改命令：返回 RA 错误，不改变状态，不通知。
- RA 调用者执行 `status`：无论是否拥有 `RespawnEvents`，都返回当前只读状态。
- 参数数量错误：返回对应子命令用法。
- 未知子命令：返回完整命令列表。
- 未知目标：列出全部五个有效目标。
- 重复状态操作：返回“状态未变化”，不重复通知。
- 配置文件缺失：由 LabAPI 生成默认配置。
- 单个通知节点无效：记录完整配置路径并退回该节点的默认值。
- 未知占位符：保留原文本并记录警告，避免静默生成错误消息。
- 通知异常：隔离并记录，不改变支援结果。
- 未知支援类型：放行，并对每种运行时类型最多警告一次。
- 插件重载：旧实例必须先注销事件，避免重复命令、重复拦截和重复通知。

## 10. 测试与验收

### 10.1 单元测试

- 默认状态和回合重置。
- 四类目标独立启用、禁用。
- 全局停止不覆盖分类状态。
- `enable all` 解除全局停止后保留分类禁用。
- 分类跳过优先于全局跳过。
- 持续停止不消费一次性跳过。
- 每个一次性跳过只消费一次。
- 命令主名称和 `rf` 别名。
- `status` 对无 `RespawnEvents` 权限的 RA 调用者保持可用。
- `enable`、`disable`、`skip`、`reset` 的 `RespawnEvents` 权限允许和拒绝路径。
- 无效参数不改变状态。
- 重复操作不广播。
- 四种通知模式。
- 白名单占位符替换。
- 无效配置节点回退。
- 公共 API 查询、修改和事件参数不可变性。
- `ReinforcementStatesApi` 跨程序集读取全局、四分类、本地、有效和跳过状态。
- `ReinforcementStatesApi` 在插件未就绪、卸载后及无效目标下的约定行为。

### 10.2 集成测试

- `MtfWave` 被识别为 `ntf`。
- `MiniMtfWave` 被识别为 `ntf-mini`。
- `ChaosWave` 被识别为 `ci`。
- `MiniChaosWave` 被识别为 `ci-mini`。
- 四类支援分别在允许、持续禁止、一次跳过状态下行为正确。
- BC 与 CASSIE 能独立和同时工作。
- 通知发送失败不影响 `ev.IsAllowed` 的最终结果。
- 未知波类型保持放行。
- 插件重载后无重复事件处理。
- 已刷新玩家的角色、阵营和存活状态不受任何命令影响。

### 10.3 验收标准

- 四类支援能够独立控制。
- 全局停止和四类状态互不覆盖。
- 全局与分类一次性跳过均准确消费。
- 每回合开始恢复默认状态。
- 所有 RA 调用者都能执行只读的 `status`。
- 只有具备 `RespawnEvents` 权限的 RA 管理员可以修改支援状态。
- 配置模板能够为命令执行和实际拦截分别发送 BC/CASSIE。
- 第三方插件可以通过公共 API 查询、控制和监听。
- 第三方插件可以只通过 `ReinforcementStatesApi` 安全查看状态，不获得内部可变对象。
- 编译、单元测试和目标服务器集成验证均通过后，才能声明完成。

## 11. GitHub README 交付要求

执行 Agent 必须在仓库根目录编写 `README.md`，至少包含：

- 项目用途、支持版本和 LabAPI 依赖。
- DLL 安装位置与插件加载说明。
- 完整 RA 命令表、`rf` 别名和五个目标。
- 权限规则：`status` 对所有 RA 调用者开放，状态修改命令要求 `RespawnEvents`。
- 全局停止、分类状态和一次性跳过的优先级示例。
- 完整默认配置和每个配置字段说明。
- BC/CASSIE 四种模式及全部占位符。
- 公共 API 类型、方法和事件说明。
- 至少一个第三方插件调用 `ReinforcementStatesApi` 读取当前状态的 C# 示例。
- 至少一个第三方插件调用控制 API 的 C# 示例。
- API 主线程调用约束。
- 构建、测试、兼容性和已知限制。
- 明确说明插件不会处理已刷新的玩家，也不会修改计时器、令牌或影响力。

README 必须与代码中的命令、枚举、默认配置和 API 签名进行一致性检查，不能保留占位内容。

## 12. 交付边界

本规格只定义设计，不执行环境安装、服务器配置、插件编译或部署。用户确认本书面规格后，下一步使用 Superpowers `writing-plans` 方法编写逐文件、逐测试的实施计划。
