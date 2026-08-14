# ReinforcementGate

ReinforcementGate 是一个 SCP: Secret Laboratory 服务端插件，用于在支援波真正刷新前控制九尾狐与混沌分裂者的主支援、迷你支援。管理员可以长期关闭某一类或全部支援，也可以只跳过下一波匹配支援。

插件只控制未来的支援刷新事件。运行时开关只保存在内存中，每回合开始恢复为全部允许，并清除所有待执行的 skip。

## Features

- 精确识别四类 LabAPI 支援波：`MtfWave`（`ntf`）、`MiniMtfWave`（`ntf-mini`）、`ChaosWave`（`ci`）和 `MiniChaosWave`（`ci-mini`）。
- 分类开关与全局开关彼此独立；解除全局停止不会覆盖原有分类状态。
- 支持分类或全局的一次性 skip，并在真正拦截后消费。
- 命令执行时与实际拦截时可分别配置 Broadcast（BC）和 Cassie（CASSIE）通知。
- 提供同步、强类型、不可变快照的状态查询 API、控制 API 与公共事件。
- 配置重载只替换通知配置，不改变运行时支援状态。

## Compatibility

- SCP: Secret Laboratory 专用服务器。
- **LabAPI 1.1.7**；插件不依赖 EXILED 或 LabExtended。
- 目标框架为 .NET Framework 4.8（`net48`），目标平台为 x64，源码使用 C# 12。
- 支援类别按 LabAPI 波类型判定，不按服务器人数或本波实际人数猜测大小支援。

服务器、LabAPI 或游戏程序集版本发生变化后，应重新构建并在测试服验证波事件签名与四类波封装类型。

## Installation

1. 确认服务器已安装兼容的 LabAPI 1.1.7。
2. 构建或下载 `ReinforcementGate.dll`。
3. 将 DLL 放入：

   ```text
   %AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll
   ```

   部分托管商使用服务器本地 `AppData` 等效目录；以托管商给出的 LabAPI 全局插件目录为准。
4. 启动或重启服务器。LabAPI 会创建默认配置，默认的按端口配置位置为：

   ```text
   %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\ReinforcementGate\reinforcement-gate.yml
   ```

   其中 `<port>` 是 LabAPI 按 SCP:SL 实际服务器端口创建的目录名。

## Remote Admin commands

根命令为 `reinforcement`，别名为 `rf`；以下所有形式都可将 `reinforcement` 换成 `rf`。

| 命令 | 作用 |
| --- | --- |
| `reinforcement status` | 查看全局、四分类、本地有效状态及待执行 skip。 |
| `reinforcement enable <target>` | 允许目标支援；`all` 只解除全局停止。 |
| `reinforcement disable <target>` | 长期停止目标支援；`all` 只启用全局停止。 |
| `reinforcement skip <target>` | 跳过下一波匹配支援；`all` 表示下一波任意已识别支援。 |
| `reinforcement reset` | 立即恢复全部默认状态并清除所有 skip。 |

可用的五个目标：

| 目标 | 含义 | LabAPI 波类型 |
| --- | --- | --- |
| `all` | 全部支援的全局控制 | 不对应单一波类型 |
| `ntf` | 九尾狐大支援 | `MtfWave` |
| `ntf-mini` | 九尾狐小支援 | `MiniMtfWave` |
| `ci` | 混沌大支援 | `ChaosWave` |
| `ci-mini` | 混沌小支援 | `MiniChaosWave` |

示例：`rf disable ntf-mini`、`rf skip ci`、`rf enable all`。

## State and skip precedence

每次已识别支援波到来时，按以下顺序决定是否放行：

1. 全局已停止：拦截，原因是 `global-disabled`。
2. 对应分类已停止：拦截，原因是 `target-disabled`。
3. 对应分类 skip 已就绪：拦截并只消费该分类 skip，原因是 `skip`。
4. 全局 skip 已就绪：拦截并只消费全局 skip，原因是 `skip`。
5. 以上均不成立：允许刷新。

长期停止的优先级高于 skip，所以被长期停止拦截时不会消费任何 skip。若同一分类 skip 与全局 skip 同时存在，对应分类 skip 先消费，全局 skip 留给后续下一波已识别支援。

全局开关不改写四个分类的本地开关。例如先执行 `disable ntf`，再执行 `disable all` 和 `enable all`，`ntf` 仍保持分类停止。每回合开始会原子恢复全部允许、清空全局与分类 skip；运行时状态不会写入配置或跨回合保存。

## Permission model

- `status` 对所有 Remote Admin 调用者开放，不检查 RA 权限节点，也不要求 `RespawnEvents`。
- `enable`、`disable`、`skip` 和 `reset` 会改变状态，要求调用者拥有 `PlayerPermissions.RespawnEvents`。
- 无权限或参数无效时不改变状态，也不发送通知。
- 跨插件只读 API 不使用 RA 权限；控制 API 使用调用方传入的 `source` 记录来源，也不执行 RA 权限检查。调用方插件自行决定自己的授权策略。

## Configuration

完整默认配置如下。配置只包含通知设置，不保存支援控制状态。

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
    broadcast:
      message: ""
      duration: 8
      clear_previous: false
    cassie:
      message: ""
      subtitles: ""
      play_background: true
      priority: 0
      glitch_scale: 0
  skip_armed:
    mode: Broadcast
    broadcast:
      message: "下一次 {target_name} 支援将被跳过"
      duration: 8
      clear_previous: false
    cassie:
      message: ""
      subtitles: ""
      play_background: true
      priority: 0
      glitch_scale: 0
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

通知节点用途：

| 节点 | 触发时机 |
| --- | --- |
| `enable_applied` | enable 确实改变状态后。 |
| `disable_applied` | disable 确实改变状态后。 |
| `disabled_wave_blocked` | 长期停止实际拦截一波时；默认 `None`，避免刷屏。 |
| `skip_armed` | skip 确实成功就绪后。 |
| `skip_triggered` | 一次性 skip 真正拦截并被消费时。 |

每个节点的字段：

- `mode`：`None`、`Broadcast`、`Cassie` 或 `Both`。
- `broadcast.message`：BC 文本模板；空字符串表示跳过 BC 通道。
- `broadcast.duration`：BC 显示秒数，必须大于 0。
- `broadcast.clear_previous`：发送前是否清除已有 BC。
- `cassie.message`：CASSIE 语音文本；空字符串表示跳过 CASSIE 通道。
- `cassie.subtitles`：CASSIE 字幕模板。
- `cassie.play_background`：是否播放 CASSIE 背景音。
- `cassie.priority`：CASSIE 优先级，必须是有限数值。
- `cassie.glitch_scale`：语音故障强度，范围为 0 到 1（含边界）。

无效节点会记录完整配置路径，并将该整个节点恢复为默认值。未知模板标记保留原文本并记录警告；模板解析或消息发送失败不会改变已经作出的支援放行/拦截决定。LabAPI 重载配置时，只原子替换通知树，不清空运行时状态。

## Template placeholders

Broadcast、Cassie 语音和字幕支持以下模板占位符：

| 占位符 | 内容 |
| --- | --- |
| `{target}` | `all`、`ntf`、`ntf-mini`、`ci` 或 `ci-mini`。 |
| `{target_name}` | `全部支援`、`九尾狐大支援`、`九尾狐小支援`、`混沌大支援` 或 `混沌小支援`。 |
| `{admin}` | RA 管理员名称，或控制 API 传入的 `source`。 |
| `{action}` | `enable`、`disable` 或 `skip`。 |
| `{reason}` | enable 通知中为空字符串；disable 或实际拦截通知中为 `global-disabled`、`target-disabled` 或 `skip`。 |

## Read-only States API

第三方 LabAPI 插件引用 `ReinforcementGate.dll` 后，可通过 `ReinforcementStatesApi` 查询状态。快照及其中的目标字典/目标状态都是只读对象，不会暴露内部控制器。

```csharp
using LabApi.Features.Console;
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

if (ReinforcementStatesApi.IsAvailable)
{
    ReinforcementStateSnapshot snapshot = ReinforcementStatesApi.GetSnapshot();
    ReinforcementTargetState ntf = snapshot.Targets[ReinforcementTarget.Ntf];
    Logger.Info($"NTF effective enabled: {ntf.IsEffectivelyEnabled}");
}
```

可用成员：

- `bool IsAvailable`
- `ReinforcementStateSnapshot GetSnapshot()`
- `ReinforcementTargetState GetState(ReinforcementTarget target)`
- `bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state)`
- `StateChanged` 与 `RoundStateReset` 事件

`GetState`/`TryGetState` 只接受四个具体目标，不接受 `All`。插件尚未启用或正在卸载时，先检查 `IsAvailable`；`GetSnapshot`/`GetState` 在不可用时抛出清晰的 `InvalidOperationException`，`TryGetState` 返回 `false`。

这些只读查询不需要任何 RA 权限。

## Control API

控制 API 与 RA 命令使用同一通知感知控制器，因此实际状态变化会遵循相同通知规则。调用方必须提供非空白 `source`，用于审计与 `{admin}` 模板值。

```csharp
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

ReinforcementControlApi.ArmSkip(
    ReinforcementTarget.CiMini,
    "ExamplePlugin");
```

完整控制入口：

```csharp
StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source);
StateTransitionResult ArmSkip(ReinforcementTarget target, string source);
StateTransitionResult ClearSkip(ReinforcementTarget target, string source);
StateTransitionResult Reset(string source);
```

`SetEnabled`、`ArmSkip` 和 `ClearSkip` 接受 `All` 或具体目标。返回的 `StateTransitionResult` 含 `Changed`、前后快照、动作、目标和来源；重复操作的 `Changed` 为 `false`，不会重复发送命令时通知。

所有公共 API 都是同步 API，必须在游戏服务器主线程调用。API 不负责调度回主线程。只读状态查询不要求 RA 权限；控制调用不读取 RA 权限，而是使用传入的 `source`，因此调用插件负责在调用前完成自己的权限与线程检查。

## Public events

```csharp
ReinforcementStatesApi.StateChanged += (_, args) =>
    Logger.Info($"Changed by {args.Transition.Source}");

ReinforcementStatesApi.RoundStateReset += (_, _) =>
    Logger.Info("Reinforcement state reset for the new round");

ReinforcementEvents.WaveBlocked += (_, args) =>
    Logger.Info($"Blocked {args.Target}: {args.Reason}");
```

- `ReinforcementStatesApi.StateChanged`：可观察状态改变后触发，参数携带不可变的 `StateTransitionResult`。
- `ReinforcementStatesApi.RoundStateReset`：每次回合状态重置后触发，即使重置前已经是默认状态。
- `ReinforcementEvents.WaveBlocked`：实际拦截支援后触发，携带目标、`ReinforcementBlockReason`、来源以及消费 skip 后的不可变快照。

外部事件订阅者抛出的异常会被隔离，不会撤销已完成的状态变化或支援拦截。插件卸载时会注销内部转发并清理公共订阅者；依赖插件应在自己的生命周期中按正常方式管理订阅。

## Building and testing

需要 .NET SDK 与 SCP:SL 专用服务器的真实服务器程序集（托管程序集）。将 `SL_REFERENCES` 指向同时包含 `Assembly-CSharp.dll` 和 `CommandSystem.Core.dll` 的目录：

```powershell
$env:SL_REFERENCES = "D:\SCPServer\SCPSL_Data\Managed"
dotnet restore ReinforcementGate.sln
dotnet build ReinforcementGate.sln --configuration Release --no-restore
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --configuration Release --no-build
```

产物位于 `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`。发布前必须使用目标服务器版本的真实程序集完成 Release 构建，并在测试服验证插件加载、配置重载、四类支援识别、BC/CASSIE 和回合重置。仅用编译桩通过单元测试不代表服务器兼容。

## Known limits

- 插件不会移除、处决、改角色、改阵营或以其他方式处理已经刷新的玩家（players）。
- 插件不会修改支援计时器（timers）、影响力（influence）、支援令牌（tokens）、波次人数（wave population）或支援载具（vehicles）。
- 插件不主动生成一波支援，只能允许或阻止 LabAPI 提供的未来波事件。
- 运行时开关不持久化，回合开始后全部恢复默认。
- 未知或未来新增的支援波封装类型会被放行并记录限频警告，不会被猜测归类。
- 配置与公共 API 为同步路径；API 只能从服务器主线程安全调用。
- 本项目不安装、升级或配置游戏服务器与 LabAPI 环境。
