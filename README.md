# ReinforcementGate

[![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7-5865F2)](https://github.com/northwood-studios/LabAPI) [![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.2.7-2f3136)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![.NET%20Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework) [![Release](https://img.shields.io/github/v/release/qingranawa/SCPSL-ReinforcementGate?display_name=tag&sort=semver)](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases/latest) [![CI](https://github.com/qingranawa/SCPSL-ReinforcementGate/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/qingranawa/SCPSL-ReinforcementGate/actions/workflows/ci.yml) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

基于 LabAPI 的 SCP:SL 支援波精细控制：四类支援独立控制、`skip`、Remote Admin 和公共 API。

> 主 README 使用中文。其他语言： [English](README.en.md) · [Polski](README.pl.md) · [Deutsch](README.de.md)

## 核心能力

- 独立控制九尾狐和混沌分裂者的大支援、小支援四类波次。
- 支持分类或全局的单次 `skip`，不改写持续禁用状态。
- 通过 Remote Admin、`RespawnEvents` 权限和可配置的 BC/CASSIE 通知管理支援波。
- 为其他 LabAPI 插件提供同步、强类型的只读快照、控制 API 和公共事件。

## Quick Start

1. 从 [最新 Release](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases/latest) 下载 `ReinforcementGate.dll`。
2. 将 DLL 放入 `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll`。
3. 重启服务器。
4. 在 Remote Admin 执行 `rf status`；能看到状态快照就说明插件已加载。

## 服务器管理员

### 功能

- 识别四类 LabAPI 支援波：`MtfWave`（`ntf`）、`MiniMtfWave`（`ntf-mini`）、`ChaosWave`（`ci`）和 `MiniChaosWave`（`ci-mini`）。
- 分类开关与全局开关彼此独立，解除全局禁用不会影响已有分类状态。
- 支持分类或全局的单次跳过下一波支援 `skip`。
- 命令执行时和实际拦截时可以分别配置 BC 公屏（Broadcast）与 CASSIE 广播。
- 提供同步、强类型、只读快照状态 API、控制 API 和公共事件。
- 配置重载只替换通知配置，不改变当前回合的运行时状态。

### 兼容性

- SCP: Secret Laboratory Dedicated Server。
- **LabAPI 1.1.7**；插件不依赖 EXILED 或 LabExtended。
- .NET Framework 4.8（`net48`），x64，源码使用 C# 12。
- 支援分类按 LabAPI 波类型判定，不按玩家数量或其他游戏状态猜测波次类型。

服务器、LabAPI 或游戏程序集版本发生变化后，应重新构建并在测试服验证事件签名和四类支援封装类型。

### 安装

1. 确认服务器安装了兼容的 LabAPI 1.1.7。
2. 从 [Releases](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases) 下载 `ReinforcementGate.dll`。
3. 将 DLL 放入：

   ```text
   %AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll
   ```

   部分托管商使用服务器本地 `AppData` 等效目录，请以托管商给出的 LabAPI 全局插件目录为准。
4. 启动或重启服务器。LabAPI 会创建默认配置，按端口的配置位置为：

   ```text
   %AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\ReinforcementGate\reinforcement-gate.yml
   ```

   `<port>` 是服务器实际端口对应的配置目录名。

### Remote Admin 命令

根命令为 `reinforcement`，别名为 `rf`；以下所有形式都可将 `reinforcement` 换成 `rf`。

| 命令 | 作用 |
| --- | --- |
| `reinforcement status` | 查看全局、四类支援、分类状态和待执行的 `skip`。 |
| `reinforcement enable <target>` | 放行指定支援；`all` 只解除全局禁用。 |
| `reinforcement disable <target>` | 持续禁用指定支援；`all` 只启用全局禁用。 |
| `reinforcement skip <target>` | 跳过下一次匹配的支援；`all` 表示下一次任意已识别支援。 |
| `reinforcement reset` | 立即恢复默认状态并清除所有 `skip`。 |

可用目标：

| 目标 | 含义 | LabAPI 波类型 |
| --- | --- | --- |
| `all` | 全部支援波次的全局控制 | 所有已识别支援 |
| `ntf` | 九尾狐大支援（MTF Primary Wave / `MtfWave`） | `MtfWave` |
| `ntf-mini` | 九尾狐小支援（MTF Mini-Wave / `MiniMtfWave`） | `MiniMtfWave` |
| `ci` | 混沌大支援（CI Primary Wave / `ChaosWave`） | `ChaosWave` |
| `ci-mini` | 混沌小支援（CI Mini-Wave / `MiniChaosWave`） | `MiniChaosWave` |

示例：`rf disable ntf-mini`、`rf skip ci`、`rf enable all`。

### 状态与 `skip` 优先级

每次已识别支援波到来时，按以下顺序决定是否放行：

1. 全局已禁用：拦截，原因是 `global-disabled`。
2. 指定支援已禁用：拦截，原因是 `target-disabled`。
3. 对应分类 `skip` 已就绪：拦截并只消费该分类 `skip`，原因是 `skip`。
4. 全局 `skip` 已就绪：拦截并只消费全局 `skip`，原因是 `skip`。
5. 以上均不成立：放行支援刷新。

持续禁用的优先级高于 `skip`，所以被持续禁用拦截时不会消费任何 `skip`。若同一分类 `skip` 与全局 `skip` 同时存在，分类 `skip` 先消费，全局 `skip` 留给后续下一次已识别支援。

全局开关不改写四个分类的分类开关。例如先执行 `disable ntf`，再执行 `disable all` 和 `enable all`，`ntf` 仍保持分类禁用。每回合开始会恢复全部放行并清空所有 `skip`；运行时状态不会写入配置，也不会跨回合保存。

### 权限

- `status` 对所有 Remote Admin 调用者开放，不检查 RA 权限节点，也不要求 `RespawnEvents`。
- `enable`、`disable`、`skip` 和 `reset` 会改变状态，要求调用者拥有 `PlayerPermissions.RespawnEvents`。
- 无权限或参数无效时不改变状态，也不发送通知。
- 跨插件只读 API 不使用 RA 权限；控制 API 使用调用方传入的 `source` 记录来源，也不执行 RA 权限检查。调用方插件自行决定自己的授权策略。

### 配置

完整默认配置如下。默认不发送 BC 或 CASSIE；服主可以填写自己的模板，并把对应节点的 `mode` 改为 `Broadcast`、`Cassie` 或 `Both`。

```yaml
notifications:
  enable_applied:
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
  disable_applied:
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
  skip_triggered:
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
```

通知节点用途：

| 节点 | 触发时机 |
| --- | --- |
| `enable_applied` | `enable` 确实改变状态后。 |
| `disable_applied` | `disable` 确实改变状态后。 |
| `disabled_wave_blocked` | 持续禁用实际拦截支援时；默认 `None`，避免刷屏。 |
| `skip_armed` | `skip` 确实成功就绪后。 |
| `skip_triggered` | 一次性 `skip` 真正拦截并被消费时。 |

字段说明：

- `mode`：`None`、`Broadcast`、`Cassie` 或 `Both`。
- `broadcast.message`：BC 文本模板；空字符串表示不发送 BC。
- `broadcast.duration`：BC 显示秒数，必须大于 0。
- `broadcast.clear_previous`：发送前是否清除已有 BC。
- `cassie.message`：CASSIE 语音文本；空字符串表示不发送 CASSIE。
- `cassie.subtitles`：CASSIE 字幕模板。
- `cassie.play_background`：是否播放 CASSIE 背景音。
- `cassie.priority`：CASSIE 优先级，必须是有限数值。
- `cassie.glitch_scale`：语音故障强度，范围为 0 到 1（含边界）。

无效节点会记录完整配置路径，并将整个节点恢复为默认值。未知占位符会保留原文本并记录警告；模板解析或消息发送失败不会改变已经作出的放行/拦截决定。LabAPI 重载配置时，只原子替换通知树，不清空运行时状态。

### 模板占位符

BC、CASSIE 语音和字幕支持以下占位符：

| 占位符 | 内容 |
| --- | --- |
| `{target}` | `all`、`ntf`、`ntf-mini`、`ci` 或 `ci-mini`。 |
| `{target_name}` | `全部支援`、`九尾狐大支援`、`九尾狐小支援`、`混沌大支援` 或 `混沌小支援`。 |
| `{admin}` | RA 管理员名称，或控制 API 传入的 `source`。 |
| `{action}` | `enable`、`disable` 或 `skip`。 |
| `{reason}` | `enable` 通知中为空字符串；`disable` 或实际拦截通知中为 `global-disabled`、`target-disabled` 或 `skip`。 |

## Developer API

第三方 LabAPI 插件可以通过下面的同步、强类型接口读取状态、控制支援波并订阅公共事件。

### 只读状态 API

第三方 LabAPI 插件引用 `ReinforcementGate.dll` 后，可通过 `ReinforcementStatesApi` 查询状态。快照及其中的分类字典/单项状态都是只读对象，不会暴露内部控制器。

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

可用成员：`IsAvailable`、`GetSnapshot()`、`GetState(target)`、`TryGetState(target, out state)`、`StateChanged` 和 `RoundStateReset`。

`GetState`/`TryGetState` 只接受四个具体目标，不接受 `All`。插件尚未启用或正在卸载时，先检查 `IsAvailable`；不可用时 `GetSnapshot`/`GetState` 抛出清晰的 `InvalidOperationException`，`TryGetState` 返回 `false`。这些只读查询不需要任何 RA 权限。

### 控制 API

控制 API 与 RA 命令使用同一通知感知控制器。调用方必须提供非空白 `source`，用于审计和 `{admin}` 模板值。

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

`SetEnabled`、`ArmSkip` 和 `ClearSkip` 接受 `All` 或具体目标。返回的 `StateTransitionResult` 含 `Changed`、前后快照、动作、目标和来源；重复操作的 `Changed` 为 `false`，不会重复发送命令通知。

所有公共 API 都是同步 API，必须在游戏服务器主线程调用。API 不负责调度回主线程。只读状态查询不要求 RA 权限；控制调用不读取 RA 权限，而是使用传入的 `source`，因此调用插件负责在调用前完成自己的权限与线程检查。

### 公共事件

```csharp
ReinforcementStatesApi.StateChanged += (_, args) =>
    Logger.Info($"Changed by {args.Transition.Source}");

ReinforcementStatesApi.RoundStateReset += (_, _) =>
    Logger.Info("Reinforcement state reset for the new round");

ReinforcementEvents.WaveBlocked += (_, args) =>
    Logger.Info($"Blocked {args.Target}: {args.Reason}");
```

- `ReinforcementStatesApi.StateChanged`：状态改变后触发，携带不可变的 `StateTransitionResult`。
- `ReinforcementStatesApi.RoundStateReset`：每回合状态重置后触发，即使重置前已经是默认状态。
- `ReinforcementEvents.WaveBlocked`：实际拦截支援后触发，携带目标、`ReinforcementBlockReason`、来源和消费 `skip` 后的不可变快照。

外部事件订阅者抛出的异常会被隔离，不会撤销已经完成的状态变化或支援拦截。插件卸载时会注销内部转发并清理公共订阅者。

## 构建与测试

需要 .NET SDK 与 SCP:SL Dedicated Server 的真实服务器程序集。将 `SL_REFERENCES` 指向同时包含 `Assembly-CSharp.dll` 和 `CommandSystem.Core.dll` 的目录：

```powershell
$env:SL_REFERENCES = "D:\SCPServer\SCPSL_Data\Managed"
dotnet restore ReinforcementGate.sln
dotnet build ReinforcementGate.sln --configuration Release --no-restore
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --configuration Release --no-build
```

产物位于 `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`。发布前必须使用目标服务器版本的真实程序集完成 Release 构建，并在测试服验证插件加载、配置重载、四类支援识别、BC/CASSIE 和回合重置。仅用编译桩通过单元测试不代表服务器兼容。

公开 CI 不下载或再分发 SCP:SL 游戏程序集；CI 只执行 restore、格式、仓库二进制和差异检查。需要 `SL_REFERENCES` 的 Release 构建、单元测试和实服兼容性验证必须在本地或受控测试环境完成。

## 已知限制

- 插件只决定 LabAPI 已提供的未来支援事件是否放行。
- 插件不会主动创建支援，也不会回溯已经发生的支援事件。
- 插件不会处理已经生成的玩家、角色或阵营。
- 插件每回合开始会恢复默认。
- 未知或未来新增的支援封装类型会被放行并记录限频警告，不会被猜测归类。
- 配置与公共 API 是同步路径；API 只能从服务器主线程安全调用。
- 本项目不安装、升级或配置游戏服务器与 LabAPI 环境。

## 社区文件

- [贡献指南](CONTRIBUTING.md)
- [行为准则](CODE_OF_CONDUCT.md)
- [获取帮助](SUPPORT.md)
- [安全策略](SECURITY.md)
- [Issue 模板](.github/ISSUE_TEMPLATE/)
- [Pull Request 模板](.github/PULL_REQUEST_TEMPLATE.md)

## 许可证

本仓库原创代码采用 [MIT License](LICENSE)。本仓库不包含或再分发 LabAPI、SCP: Secret Laboratory 游戏程序集或其他第三方二进制文件；这些依赖仍受其各自许可证和服务条款约束。
