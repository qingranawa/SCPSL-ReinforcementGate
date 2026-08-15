# ReinforcementGate

[![LabAPI](https://img.shields.io/badge/LabAPI-1.1.7-5865F2)](https://github.com/northwood-studios/LabAPI) [![SCP:SL](https://img.shields.io/badge/SCP%3ASL-14.2.7-2f3136)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![.NET%20Framework](https://img.shields.io/badge/.NET%20Framework-4.8-512BD4)](https://dotnet.microsoft.com/download/dotnet-framework) [![Release](https://img.shields.io/github/v/release/qingranawa/SCPSL-ReinforcementGate?display_name=tag&sort=semver)](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases/latest) [![CI](https://github.com/qingranawa/SCPSL-ReinforcementGate/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/qingranawa/SCPSL-ReinforcementGate/actions/workflows/ci.yml) [![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

Fine-grained SCP:SL reinforcement-wave control for LabAPI.

ReinforcementGate independently controls all four NTF and Chaos Insurgency waves, one-wave `skip` actions, Remote Admin permissions and public plugin APIs.

The main README is in Chinese. Other languages: [中文](README.md) · [Polski](README.pl.md) · [Deutsch](README.de.md)

## Core capabilities

- Independently control NTF and Chaos main and mini reinforcement waves.
- Arm target or global one-wave `skip` actions without rewriting persistent stops.
- Manage waves through Remote Admin, `RespawnEvents` permissions and configurable Broadcast/CASSIE notifications.
- Integrate other LabAPI plugins through synchronous typed snapshots, control methods and public events.

## Quick Start

1. Download `ReinforcementGate.dll` from the [latest Release](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases/latest).
2. Copy it to `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll`.
3. Restart the server.
4. Run `rf status` in Remote Admin; the state snapshot confirms that the plugin is loaded.

## Server Admin

### Features

- Recognizes the four LabAPI reinforcement wrappers: `MtfWave` (`ntf`), `MiniMtfWave` (`ntf-mini`), `ChaosWave` (`ci`) and `MiniChaosWave` (`ci-mini`).
- Global and per-target switches are independent; clearing the global stop does not change local target states.
- Supports per-target and global one-shot `skip` actions.
- Allows separate Broadcast and CASSIE templates for command execution and actual interception.
- Exposes synchronous, strongly typed, read-only snapshots, control APIs and public events.
- Reloading the configuration replaces only the notification tree and preserves runtime state for the current round.

### Compatibility

- SCP: Secret Laboratory Dedicated Server.
- **LabAPI 1.1.7**; no EXILED or LabExtended dependency.
- .NET Framework 4.8 (`net48`), x64, C# 12.
- Classification uses LabAPI wave types, not player counts or guessed game state.

When the server, LabAPI or game assemblies change, rebuild and verify the event signatures and four wrapper types on a test server.

### Installation

1. Install a compatible LabAPI 1.1.7 server.
2. Download `ReinforcementGate.dll` from [Releases](https://github.com/qingranawa/SCPSL-ReinforcementGate/releases).
3. Copy it to `%AppData%\\SCP Secret Laboratory\\LabAPI\\plugins\\global\\ReinforcementGate.dll` (hosting providers may use an equivalent local AppData directory).
4. Start or restart the server. LabAPI creates the per-port configuration at `%AppData%\\SCP Secret Laboratory\\LabAPI\\configs\\<port>\\ReinforcementGate\\reinforcement-gate.yml`.

### Remote Admin commands

The root command is `reinforcement`, with `rf` as an alias; either name can be used.

| Command | Description |
| --- | --- |
| `reinforcement status` | Show global state, all four targets, local state and pending `skip` actions. |
| `reinforcement enable <target>` | Allow a target; `all` only clears the global stop. |
| `reinforcement disable <target>` | Persistently stop a target; `all` enables the global stop. |
| `reinforcement skip <target>` | Stop the next matching wave; `all` means the next recognized wave. |
| `reinforcement reset` | Restore defaults immediately and clear every `skip`. |

Targets: `all` (all recognized waves), `ntf` (NTF main), `ntf-mini` (NTF mini), `ci` (Chaos Insurgency main), `ci-mini` (Chaos Insurgency mini).

Examples: `rf disable ntf-mini`, `rf skip ci`, `rf enable all`.

### State and `skip` precedence

For every recognized wave, the decision order is:

1. Global stop: block with `global-disabled`.
2. Target stop: block with `target-disabled`.
3. Target `skip`: block and consume only that target skip, with reason `skip`.
4. Global `skip`: block and consume only the global skip, with reason `skip`.
5. Otherwise, allow the wave to spawn.

Persistent stops have priority over `skip`, so no skip is consumed while a persistent stop blocks the wave. If target and global skips coexist, the target skip is consumed first. Global enable/disable does not rewrite the four local target switches. Round start enables all targets and clears all skips; runtime state is not saved across rounds.

### Permissions

- `status` is open to every Remote Admin caller; it checks neither an RA permission node nor `RespawnEvents`.
- `enable`, `disable`, `skip` and `reset` change state and require `PlayerPermissions.RespawnEvents`.
- Invalid or unauthorized requests leave state unchanged and send no notification.
- Cross-plugin read-only APIs do not use RA permissions. The control API records the caller-supplied `source` and leaves authorization to the calling plugin.

### Configuration and notifications

All notification nodes default to `mode: None`; no Broadcast or CASSIE is sent by default. Server owners can write their own templates and set a node to `Broadcast`, `Cassie` or `Both`.

The nodes are `enable_applied`, `disable_applied`, `disabled_wave_blocked`, `skip_armed` and `skip_triggered`. They correspond to a changed enable action, a changed disable action, an actual persistent-stop interception, a successfully armed skip, and a consumed one-shot skip.

Supported fields are `mode`, `broadcast.message`, `broadcast.duration`, `broadcast.clear_previous`, `cassie.message`, `cassie.subtitles`, `cassie.play_background`, `cassie.priority` and `cassie.glitch_scale`. Invalid nodes fall back to defaults and log their full path; unknown placeholders are preserved and logged. Template or send failures do not change the already-made allow/block decision. Configuration reload atomically replaces only the notification tree.

### Template placeholders

Broadcast, CASSIE speech and subtitles support `{target}`, `{target_name}`, `{admin}`, `{action}` and `{reason}`. `{reason}` is empty for `enable`, and is `global-disabled`, `target-disabled` or `skip` for disable/interception notifications.

## Developer API

Other LabAPI plugins can use the following synchronous, strongly typed interfaces to read state, control waves and subscribe to public events.

### Read-only state API

Reference `ReinforcementGate.dll` from another LabAPI plugin and use `ReinforcementStatesApi`. Snapshots and target dictionaries are read-only and do not expose the internal controller.

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

Available members include `IsAvailable`, `GetSnapshot()`, `GetState(target)`, `TryGetState(target, out state)`, `StateChanged` and `RoundStateReset`. `GetState` and `TryGetState` accept only concrete targets, not `All`. These queries do not require RA permissions; unavailable services report a clear exception or `false`.

### Control API

The control API uses the same notification-aware controller as the RA commands. A non-blank `source` is required for audit data and the `{admin}` placeholder.

```csharp
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

ReinforcementControlApi.ArmSkip(
    ReinforcementTarget.CiMini,
    "ExamplePlugin");
```

The complete entry points are `SetEnabled(target, enabled, source)`, `ArmSkip(target, source)`, `ClearSkip(target, source)` and `Reset(source)`. They accept `All` or a concrete target and return a `StateTransitionResult`. Repeating an operation returns `Changed == false` and does not duplicate command notifications. All public APIs are synchronous and must be called on the server main thread; the caller owns authorization and thread checks.

### Public events

- `ReinforcementStatesApi.StateChanged` fires after a state change with an immutable `StateTransitionResult`.
- `ReinforcementStatesApi.RoundStateReset` fires at every round reset.
- `ReinforcementEvents.WaveBlocked` fires after an actual interception with the target, `ReinforcementBlockReason`, source and immutable post-consumption snapshot.

Subscriber exceptions are isolated. Unloading unregisters internal forwarding and clears public subscriptions.

## Build and test

Set `SL_REFERENCES` to a real SCP:SL `SCPSL_Data/Managed` directory containing `Assembly-CSharp.dll` and `CommandSystem.Core.dll`:

```powershell
$env:SL_REFERENCES = "D:\\SCPServer\\SCPSL_Data\\Managed"
dotnet restore ReinforcementGate.sln
dotnet build ReinforcementGate.sln --configuration Release --no-restore
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --configuration Release --no-build
```

The output is `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`. A stub-only build does not prove server compatibility; validate loading, reload, all four waves, Broadcast/CASSIE and round reset on a test server.

The public CI does not download or redistribute SCP:SL game assemblies. It runs restore, whitespace, repository-binary and diff checks; the `SL_REFERENCES` build, unit tests and server-compatibility validation must run locally or in a controlled test environment.

## Known limitations

- The plugin only decides whether future LabAPI reinforcement events are allowed.
- It does not create or replay waves and does not modify already spawned players, roles or factions.
- Round start restores defaults.
- Unknown or future wrapper types pass through with a rate-limited warning.
- Configuration and public APIs are synchronous and safe only on the server main thread.
- This project does not install, upgrade or configure the game server or LabAPI.

## Community files

- [Contributing](CONTRIBUTING.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)
- [Support](SUPPORT.md)
- [Issue templates](.github/ISSUE_TEMPLATE/)
- [Security policy](SECURITY.md)
- [Pull Request template](.github/PULL_REQUEST_TEMPLATE.md)

## License

Original code in this repository is licensed under the [MIT License](LICENSE). LabAPI, SCP:SL assemblies and other third-party components are not redistributed here and remain under their own terms.
