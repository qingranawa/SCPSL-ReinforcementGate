# ReinforcementGate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a LabAPI plugin that lets Remote Admin staff independently allow, stop, inspect, and skip NTF/CI primary and mini reinforcement waves, with configurable BC/CASSIE notifications and cross-plugin state/control APIs.

**Architecture:** Keep the reinforcement state machine and public contracts independent from LabAPI, then place thin adapters around Remote Admin, wave events, Broadcast, and Announcer. All mutations flow through one controller so commands, cross-plugin calls, events, notifications, and one-shot skip consumption share identical behavior.

**Tech Stack:** C# 12, .NET Framework 4.8, Northwood LabAPI 1.1.7, xUnit 2.9.3, Microsoft.NET.Test.Sdk 18.8.1, xunit.runner.visualstudio 3.1.5.

## Global Constraints

- Use LabAPI 1.1.7 only; do not add EXILED or LabExtended.
- Target `net48`, C# 12, x64-compatible server execution, nullable reference types enabled.
- Build requires `SL_REFERENCES` to point at the SCP:SL managed-assembly directory containing `Assembly-CSharp.dll` and `CommandSystem.Core.dll`; this plan does not install or configure the server.
- Classify waves by `MtfWave`, `MiniMtfWave`, `ChaosWave`, and `MiniChaosWave`; never infer wave size from player count.
- Commands are `reinforcement` and alias `rf`; targets are exactly `all`, `ntf`, `ntf-mini`, `ci`, and `ci-mini`.
- `status` is available to every Remote Admin caller; `enable`, `disable`, `skip`, and `reset` require `PlayerPermissions.RespawnEvents`.
- `disable all` and `enable all` only change the global gate; they never overwrite four target-local states.
- Round start resets all runtime state; runtime state is never persisted.
- Existing players are never killed, removed, reassigned, or otherwise modified.
- Do not modify respawn timers, influence, tokens, wave population, or vehicle behavior.
- Notification failure must never change the allow/block decision.
- Public state snapshots and event arguments are immutable.
- Every production change follows red-green-refactor and ends with a focused commit.

---

## File Map

| Path | Responsibility |
| --- | --- |
| `ReinforcementGate.sln` | Solution entry point. |
| `Directory.Build.props` | Shared language, nullable, deterministic-build settings. |
| `src/ReinforcementGate/ReinforcementGate.csproj` | LabAPI plugin project and server assembly references. |
| `src/ReinforcementGate/Domain/*.cs` | Public enums, immutable snapshots, transition and block results. |
| `src/ReinforcementGate/State/ReinforcementStateService.cs` | The only mutable reinforcement state and transition logic. |
| `src/ReinforcementGate/Api/*.cs` | Cross-plugin read-only state API, control API, and wave-block event hub. |
| `src/ReinforcementGate/Configuration/*.cs` | YAML-backed notification configuration and semantic normalization. |
| `src/ReinforcementGate/Notifications/*.cs` | Template rendering, delivery orchestration, and LabAPI transport. |
| `src/ReinforcementGate/Control/*.cs` | Notification-aware controller shared by RA commands and cross-plugin control calls. |
| `src/ReinforcementGate/Commands/*.cs` | Pure command parsing/formatting plus the RA adapter. |
| `src/ReinforcementGate/Interception/*.cs` | Wave type classification and allow/block orchestration. |
| `src/ReinforcementGate/ReinforcementGatePlugin.cs` | Plugin composition root and lifecycle. |
| `src/ReinforcementGate/ReinforcementEventsHandler.cs` | LabAPI round/wave event adapter. |
| `tests/ReinforcementGate.Tests/*.cs` | State, API, command, template, notification, and interception tests. |
| `README.md` | GitHub-facing install, command, configuration, API, and compatibility documentation. |

---

### Task 1: Scaffold the repository and immutable domain contracts

**Files:**
- Create: `.gitignore`
- Create: `Directory.Build.props`
- Create: `ReinforcementGate.sln`
- Create: `src/ReinforcementGate/ReinforcementGate.csproj`
- Create: `src/ReinforcementGate/Properties/AssemblyInfo.cs`
- Create: `tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj`
- Create: `src/ReinforcementGate/Domain/ReinforcementTarget.cs`
- Create: `src/ReinforcementGate/Domain/ReinforcementStateAction.cs`
- Create: `src/ReinforcementGate/Domain/ReinforcementBlockReason.cs`
- Create: `src/ReinforcementGate/Domain/ReinforcementTargetState.cs`
- Create: `src/ReinforcementGate/Domain/ReinforcementStateSnapshot.cs`
- Test: `tests/ReinforcementGate.Tests/DomainContractTests.cs`

**Interfaces:**
- Produces: `ReinforcementTarget`, `ReinforcementStateAction`, `ReinforcementBlockReason`, `ReinforcementTargetState`, `ReinforcementStateSnapshot`.
- Produces: a buildable LabAPI 1.1.7 solution used by every later task.

- [ ] **Step 1: Initialize Git and create the solution skeleton**

Run:

```powershell
git init
git branch -M main
dotnet new sln --name ReinforcementGate
```

Create `Directory.Build.props` with:

```xml
<Project>
  <PropertyGroup>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <Deterministic>true</Deterministic>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

Create `src/ReinforcementGate/ReinforcementGate.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <OutputType>Library</OutputType>
    <PlatformTarget>x64</PlatformTarget>
    <AssemblyName>ReinforcementGate</AssemblyName>
    <RootNamespace>ReinforcementGate</RootNamespace>
    <Version>1.0.0</Version>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Northwood.LabAPI" Version="1.1.7" />
    <Reference Include="Assembly-CSharp" HintPath="$(SL_REFERENCES)\Assembly-CSharp.dll" Private="false" />
    <Reference Include="CommandSystem.Core" HintPath="$(SL_REFERENCES)\CommandSystem.Core.dll" Private="false" />
  </ItemGroup>
</Project>
```

Create `tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <IsPackable>false</IsPackable>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.8.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ReinforcementGate\ReinforcementGate.csproj" />
    <Reference Include="Assembly-CSharp" HintPath="$(SL_REFERENCES)\Assembly-CSharp.dll" Private="true" />
    <Reference Include="CommandSystem.Core" HintPath="$(SL_REFERENCES)\CommandSystem.Core.dll" Private="true" />
  </ItemGroup>
</Project>
```

Create `.gitignore` with:

```gitignore
.vs/
.idea/
bin/
obj/
TestResults/
*.user
*.suo
```

Create `src/ReinforcementGate/Properties/AssemblyInfo.cs` with:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ReinforcementGate.Tests")]
```

After both project files exist, run:

```powershell
dotnet sln ReinforcementGate.sln add src/ReinforcementGate/ReinforcementGate.csproj
dotnet sln ReinforcementGate.sln add tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj
```

- [ ] **Step 2: Write the failing immutable-contract tests**

Create `tests/ReinforcementGate.Tests/DomainContractTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using ReinforcementGate.Domain;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class DomainContractTests
{
    [Fact]
    public void Snapshot_exposes_local_effective_and_skip_state()
    {
        ReinforcementTargetState ntf = new(
            ReinforcementTarget.Ntf,
            isLocallyEnabled: true,
            isEffectivelyEnabled: false,
            isSkipArmed: true,
            enabledLastChangedBy: "Admin A",
            skipLastChangedBy: "Admin B");

        ReinforcementStateSnapshot snapshot = new(
            isGlobalDisabled: true,
            isGlobalSkipArmed: false,
            globalDisabledLastChangedBy: "Admin C",
            globalSkipLastChangedBy: string.Empty,
            new Dictionary<ReinforcementTarget, ReinforcementTargetState>
            {
                [ReinforcementTarget.Ntf] = ntf,
            });

        Assert.True(snapshot.IsGlobalDisabled);
        Assert.False(snapshot.Targets[ReinforcementTarget.Ntf].IsEffectivelyEnabled);
        Assert.Equal("Admin B", snapshot.Targets[ReinforcementTarget.Ntf].SkipLastChangedBy);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<ReinforcementTarget, ReinforcementTargetState>)snapshot.Targets)
                .Add(ReinforcementTarget.Ci, ntf));
    }
}
```

- [ ] **Step 3: Run the contract test and verify red**

Run:

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter DomainContractTests
```

Expected: build fails because the `ReinforcementGate.Domain` types do not exist.

- [ ] **Step 4: Implement the domain contracts**

Use these exact enum members:

```csharp
public enum ReinforcementTarget { All, Ntf, NtfMini, Ci, CiMini }

public enum ReinforcementStateAction
{
    Enable,
    Disable,
    ArmSkip,
    ClearSkip,
    ConsumeSkip,
    Reset,
    RoundReset,
}

public enum ReinforcementBlockReason
{
    GlobalDisabled,
    TargetDisabled,
    TargetSkip,
    GlobalSkip,
}
```

Implement `ReinforcementTargetState` and `ReinforcementStateSnapshot` as sealed classes with constructor-assigned get-only properties. Copy the incoming target dictionary into a new `Dictionary` and expose it through `ReadOnlyDictionary<ReinforcementTarget, ReinforcementTargetState>` so callers cannot mutate state through a cast.

- [ ] **Step 5: Run the test and build**

Run:

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter DomainContractTests
dotnet build ReinforcementGate.sln -c Release
```

Expected: `DomainContractTests` passes; Release build succeeds with zero warnings and zero errors.

- [ ] **Step 6: Commit the scaffold and contracts**

```powershell
git add .gitignore Directory.Build.props ReinforcementGate.sln src tests docs/superpowers/specs/2026-08-14-reinforcement-gate-design.md docs/superpowers/plans/2026-08-14-reinforcement-gate.md
git commit -m "chore: scaffold ReinforcementGate contracts"
```

---

### Task 2: Implement the atomic reinforcement state machine

**Files:**
- Create: `src/ReinforcementGate/Domain/ReinforcementStateChangedEventArgs.cs`
- Create: `src/ReinforcementGate/Domain/StateTransitionResult.cs`
- Create: `src/ReinforcementGate/Domain/WaveDecision.cs`
- Create: `src/ReinforcementGate/State/IReinforcementStateProvider.cs`
- Create: `src/ReinforcementGate/State/IReinforcementController.cs`
- Create: `src/ReinforcementGate/State/ReinforcementStateService.cs`
- Test: `tests/ReinforcementGate.Tests/ReinforcementStateServiceTests.cs`

**Interfaces:**
- Consumes: domain types from Task 1.
- Produces: `GetSnapshot()`, `SetEnabled(...)`, `ArmSkip(...)`, `ClearSkip(...)`, `Reset(...)`, `ResetForRound()`, and `EvaluateWave(...)`.
- Produces: atomic `StateChanged` and `RoundStateReset` events used by Task 3.

- [ ] **Step 1: Write failing state-transition tests**

Create tests covering these exact cases:

```csharp
[Fact]
public void Global_gate_does_not_overwrite_target_local_state()
{
    ReinforcementStateService state = new();
    state.SetEnabled(ReinforcementTarget.Ntf, false, "A");
    state.SetEnabled(ReinforcementTarget.All, false, "B");
    state.SetEnabled(ReinforcementTarget.All, true, "C");

    ReinforcementTargetState ntf = state.GetSnapshot().Targets[ReinforcementTarget.Ntf];
    Assert.False(ntf.IsLocallyEnabled);
    Assert.False(ntf.IsEffectivelyEnabled);
}

[Fact]
public void Target_skip_is_consumed_before_global_skip()
{
    ReinforcementStateService state = new();
    state.ArmSkip(ReinforcementTarget.All, "global");
    state.ArmSkip(ReinforcementTarget.Ntf, "target");

    WaveDecision first = state.EvaluateWave(ReinforcementTarget.Ntf);
    WaveDecision second = state.EvaluateWave(ReinforcementTarget.Ci);

    Assert.Equal(ReinforcementBlockReason.TargetSkip, first.Reason);
    Assert.Equal("target", first.Source);
    Assert.Equal(ReinforcementBlockReason.GlobalSkip, second.Reason);
    Assert.Equal("global", second.Source);
}

[Fact]
public void Persistent_block_does_not_consume_skip()
{
    ReinforcementStateService state = new();
    state.SetEnabled(ReinforcementTarget.Ntf, false, "disabled");
    state.ArmSkip(ReinforcementTarget.Ntf, "skip");

    Assert.Equal(ReinforcementBlockReason.TargetDisabled,
        state.EvaluateWave(ReinforcementTarget.Ntf).Reason);
    Assert.True(state.GetSnapshot().Targets[ReinforcementTarget.Ntf].IsSkipArmed);
}

[Fact]
public void Round_reset_is_one_atomic_change_and_restores_defaults()
{
    ReinforcementStateService state = new();
    state.SetEnabled(ReinforcementTarget.CiMini, false, "A");
    int stateEvents = 0;
    int roundEvents = 0;
    state.StateChanged += (_, _) => stateEvents++;
    state.RoundStateReset += (_, _) => roundEvents++;

    state.ResetForRound();

    Assert.Equal(1, stateEvents);
    Assert.Equal(1, roundEvents);
    Assert.True(state.GetSnapshot().Targets[ReinforcementTarget.CiMini].IsEffectivelyEnabled);
}
```

- [ ] **Step 2: Run state tests and verify red**

Run:

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ReinforcementStateServiceTests
```

Expected: build fails because the state service and transition contracts do not exist.

- [ ] **Step 3: Implement the state interfaces and result contracts**

Use these exact signatures:

```csharp
public interface IReinforcementStateProvider
{
    event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged;
    event EventHandler? RoundStateReset;
    ReinforcementStateSnapshot GetSnapshot();
    ReinforcementTargetState GetState(ReinforcementTarget target);
    bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state);
}

public interface IReinforcementController : IReinforcementStateProvider
{
    StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source);
    StateTransitionResult ArmSkip(ReinforcementTarget target, string source);
    StateTransitionResult ClearSkip(ReinforcementTarget target, string source);
    StateTransitionResult Reset(string source);
    StateTransitionResult ResetForRound();
    WaveDecision EvaluateWave(ReinforcementTarget target);
}
```

`StateTransitionResult` contains `Changed`, `Before`, `After`, `Action`, `Target`, and `Source`. `WaveDecision` contains `IsBlocked`, nullable `Reason`, `Target`, `Source`, and nullable skip-consumption transition.

- [ ] **Step 4: Implement the minimal state machine**

Store four mutable target records internally. Build a new immutable snapshot for every public return and event. Implement the core evaluation in this order:

```csharp
public WaveDecision EvaluateWave(ReinforcementTarget target)
{
    EnsureConcreteTarget(target);
    MutableTargetState local = _targets[target];

    if (_globalDisabled)
        return WaveDecision.Blocked(target, ReinforcementBlockReason.GlobalDisabled, _globalDisabledSource);

    if (!local.IsEnabled)
        return WaveDecision.Blocked(target, ReinforcementBlockReason.TargetDisabled, local.EnabledSource);

    if (local.IsSkipArmed)
    {
        string source = local.SkipSource;
        StateTransitionResult consumed = ConsumeTargetSkip(target, source);
        return WaveDecision.Blocked(target, ReinforcementBlockReason.TargetSkip, source, consumed);
    }

    if (_globalSkipArmed)
    {
        string source = _globalSkipSource;
        StateTransitionResult consumed = ConsumeGlobalSkip(source);
        return WaveDecision.Blocked(target, ReinforcementBlockReason.GlobalSkip, source, consumed);
    }

    return WaveDecision.Allowed(target);
}
```

Reject `All` in `GetState`, `TryGetState`, and `EvaluateWave`. Treat `All` specially only in `SetEnabled`, `ArmSkip`, and `ClearSkip`. Reject null, empty, or whitespace `source` with `ArgumentException`.

`ResetForRound()` uses source `round-start`. It always publishes `RoundStateReset`; it publishes `StateChanged` only when the before/after snapshots differ.

- [ ] **Step 5: Run all state tests**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ReinforcementStateServiceTests
```

Expected: all state tests pass; no transition emits more than one `StateChanged` event.

- [ ] **Step 6: Commit the state machine**

```powershell
git add src/ReinforcementGate/Domain src/ReinforcementGate/State tests/ReinforcementGate.Tests/ReinforcementStateServiceTests.cs
git commit -m "feat: add reinforcement state machine"
```

---

### Task 3: Expose lifecycle-safe cross-plugin APIs

**Files:**
- Create: `src/ReinforcementGate/Api/ReinforcementStatesApi.cs`
- Create: `src/ReinforcementGate/Api/ReinforcementControlApi.cs`
- Create: `src/ReinforcementGate/Api/ReinforcementEvents.cs`
- Create: `src/ReinforcementGate/Domain/WaveBlockedEventArgs.cs`
- Test: `tests/ReinforcementGate.Tests/PublicApiTests.cs`

**Interfaces:**
- Consumes: `IReinforcementStateProvider` and `IReinforcementController` from Task 2.
- Produces: the exact static APIs documented in the design and README.

- [ ] **Step 1: Write failing API lifecycle tests**

```csharp
[Fact]
public void States_api_reports_unavailable_before_registration_and_after_unregistration()
{
    Assert.False(ReinforcementStatesApi.IsAvailable);
    Assert.Throws<InvalidOperationException>(() => ReinforcementStatesApi.GetSnapshot());

    ReinforcementStateService service = new();
    ReinforcementStatesApi.Register(service);
    Assert.True(ReinforcementStatesApi.IsAvailable);

    ReinforcementStatesApi.Unregister(service);
    Assert.False(ReinforcementStatesApi.IsAvailable);
    Assert.False(ReinforcementStatesApi.TryGetState(ReinforcementTarget.Ntf, out _));
}

[Fact]
public void Control_api_delegates_to_the_registered_controller()
{
    ReinforcementStateService service = new();
    ReinforcementStatesApi.Register(service);
    ReinforcementControlApi.Register(service);

    ReinforcementControlApi.SetEnabled(ReinforcementTarget.Ci, false, "OtherPlugin");

    Assert.False(ReinforcementStatesApi.GetState(ReinforcementTarget.Ci).IsEffectivelyEnabled);
    ReinforcementControlApi.Unregister(service);
    ReinforcementStatesApi.Unregister(service);
}
```

Add test cleanup that unregisters providers after every test so static state cannot leak between tests.

- [ ] **Step 2: Run API tests and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter PublicApiTests
```

Expected: build fails because public API types do not exist.

- [ ] **Step 3: Implement `ReinforcementStatesApi`**

Expose:

```csharp
public static bool IsAvailable { get; }
public static event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged;
public static event EventHandler? RoundStateReset;
public static ReinforcementStateSnapshot GetSnapshot();
public static ReinforcementTargetState GetState(ReinforcementTarget target);
public static bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state);
```

Keep `Register(IReinforcementStateProvider)` and `Unregister(IReinforcementStateProvider)` internal. Registration subscribes to provider events; unregistration verifies reference identity, unsubscribes, clears the provider, and clears public event delegates to prevent reload leaks.

- [ ] **Step 4: Implement `ReinforcementControlApi` and `ReinforcementEvents`**

Expose the four controller methods exactly:

```csharp
public static StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source);
public static StateTransitionResult ArmSkip(ReinforcementTarget target, string source);
public static StateTransitionResult ClearSkip(ReinforcementTarget target, string source);
public static StateTransitionResult Reset(string source);
```

`ReinforcementEvents` exposes public `WaveBlocked` and an internal `PublishWaveBlocked(WaveBlockedEventArgs args)`. `WaveBlockedEventArgs` is immutable and includes target, reason, source, and the state snapshot after any skip consumption.

Also add internal `ReinforcementEvents.ClearSubscribers()` and invoke it during plugin teardown so external subscribers from a previous plugin instance cannot receive duplicate reload events.

- [ ] **Step 5: Run API tests**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter PublicApiTests
```

Expected: all lifecycle, immutability, event forwarding, invalid-target, and unavailable-provider tests pass.

- [ ] **Step 6: Commit public APIs**

```powershell
git add src/ReinforcementGate/Api src/ReinforcementGate/Domain/WaveBlockedEventArgs.cs tests/ReinforcementGate.Tests/PublicApiTests.cs
git commit -m "feat: expose reinforcement state APIs"
```

---

### Task 4: Implement notification configuration and safe template rendering

**Files:**
- Create: `src/ReinforcementGate/Configuration/ReinforcementGateConfig.cs`
- Create: `src/ReinforcementGate/Configuration/NotificationsConfig.cs`
- Create: `src/ReinforcementGate/Configuration/NotificationNodeConfig.cs`
- Create: `src/ReinforcementGate/Configuration/BroadcastConfig.cs`
- Create: `src/ReinforcementGate/Configuration/CassieConfig.cs`
- Create: `src/ReinforcementGate/Configuration/NotificationConfigNormalizer.cs`
- Create: `src/ReinforcementGate/Notifications/NotificationMode.cs`
- Create: `src/ReinforcementGate/Notifications/NotificationKind.cs`
- Create: `src/ReinforcementGate/Notifications/NotificationContext.cs`
- Create: `src/ReinforcementGate/Notifications/TemplateRenderResult.cs`
- Create: `src/ReinforcementGate/Notifications/TemplateRenderer.cs`
- Create: `src/ReinforcementGate/Domain/ReinforcementTargetNames.cs`
- Test: `tests/ReinforcementGate.Tests/ConfigurationAndTemplateTests.cs`

**Interfaces:**
- Produces: normalized configuration nodes for Task 5.
- Produces: `TemplateRenderer.Render(string, NotificationContext)` returning `TemplateRenderResult` with rendered text and unknown-token warnings.

- [ ] **Step 1: Write failing normalization and rendering tests**

Cover:

```csharp
[Theory]
[InlineData(NotificationMode.None)]
[InlineData(NotificationMode.Broadcast)]
[InlineData(NotificationMode.Cassie)]
[InlineData(NotificationMode.Both)]
public void All_notification_modes_are_preserved(NotificationMode mode)
{
    NotificationNodeConfig node = new() { Mode = mode };
    NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
        "notifications.skip_triggered", node, NotificationNodeConfig.CreateSkipTriggeredDefault());
    Assert.Equal(mode, normalized.Mode);
}

[Fact]
public void Renderer_replaces_whitelisted_tokens_and_preserves_unknown_tokens()
{
    NotificationContext context = new(
        ReinforcementTarget.NtfMini,
        "九尾狐小支援",
        "Admin",
        "skip",
        "skip");

    TemplateRenderResult rendered = TemplateRenderer.Render(
        "{target}|{target_name}|{admin}|{action}|{reason}|{unknown}", context);

    Assert.Equal("ntf-mini|九尾狐小支援|Admin|skip|skip|{unknown}", rendered.Text);
    Assert.Equal(new[] { "{unknown}" }, rendered.UnknownTokens);
}

[Fact]
public void Invalid_cassie_glitch_scale_restores_the_default_node()
{
    NotificationNodeConfig node = NotificationNodeConfig.CreateSkipTriggeredDefault();
    node.Cassie.GlitchScale = 2f;

    NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
        "notifications.skip_triggered", node, NotificationNodeConfig.CreateSkipTriggeredDefault());

    Assert.Equal(0f, normalized.Cassie.GlitchScale);
}
```

- [ ] **Step 2: Run configuration tests and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ConfigurationAndTemplateTests
```

Expected: build fails because notification configuration and renderer types do not exist.

- [ ] **Step 3: Implement exact configuration defaults**

`NotificationsConfig` contains five properties named `EnableApplied`, `DisableApplied`, `DisabledWaveBlocked`, `SkipArmed`, and `SkipTriggered`. Defaults must match the approved YAML in the design specification. `DisabledWaveBlocked.Mode` defaults to `None`; the other nodes retain their approved Broadcast/Both defaults.

Each `NotificationNodeConfig` contains:

```csharp
public NotificationMode Mode { get; set; }
public BroadcastConfig Broadcast { get; set; } = new();
public CassieConfig Cassie { get; set; } = new();
```

Broadcast fields are `Message`, `Duration` (`ushort`), and `ClearPrevious`. CASSIE fields are `Message`, `Subtitles`, `PlayBackground`, `Priority`, and `GlitchScale`.

- [ ] **Step 4: Implement semantic normalization and template rendering**

Normalize null nodes, zero Broadcast duration, non-finite CASSIE priority, and CASSIE glitch scale outside `0..1` by replacing the affected node with its default. Return a cloned normalized tree so validation cannot mutate the object currently used by LabAPI serialization.

`TemplateRenderer` replaces only `{target}`, `{target_name}`, `{admin}`, `{action}`, and `{reason}` using ordinal string replacement. Return `TemplateRenderResult` with `Text` and an immutable `UnknownTokens` collection. Preserve unknown brace tokens unchanged so Task 5 can log each unknown token once per template.

Implement `ReinforcementTargetNames.ToCommandName(target)` and `ToDisplayName(target)` as the single mappings used by commands, status, notifications, and README examples:

```text
All -> all -> 全部支援
Ntf -> ntf -> 九尾狐大支援
NtfMini -> ntf-mini -> 九尾狐小支援
Ci -> ci -> 混沌大支援
CiMini -> ci-mini -> 混沌小支援
```

- [ ] **Step 5: Run configuration tests**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ConfigurationAndTemplateTests
```

Expected: all mode, default, fallback, empty-message, known-token, and unknown-token tests pass.

- [ ] **Step 6: Commit configuration and renderer**

```powershell
git add src/ReinforcementGate/Configuration src/ReinforcementGate/Notifications tests/ReinforcementGate.Tests/ConfigurationAndTemplateTests.cs
git commit -m "feat: add notification templates"
```

---

### Task 5: Deliver Broadcast and CASSIE notifications without affecting control

**Files:**
- Create: `src/ReinforcementGate/Notifications/INotificationTransport.cs`
- Create: `src/ReinforcementGate/Notifications/INotificationService.cs`
- Create: `src/ReinforcementGate/Notifications/INotificationLogger.cs`
- Create: `src/ReinforcementGate/Notifications/NotificationService.cs`
- Create: `src/ReinforcementGate/Notifications/LabApiNotificationTransport.cs`
- Create: `src/ReinforcementGate/Notifications/LabApiNotificationLogger.cs`
- Create: `src/ReinforcementGate/Control/NotifyingReinforcementController.cs`
- Test: `tests/ReinforcementGate.Tests/NotificationServiceTests.cs`

**Interfaces:**
- Consumes: normalized notification configuration and `NotificationContext` from Task 4.
- Produces: `INotificationService.Notify(NotificationKind kind, NotificationContext context)` for control and interception tasks.
- Produces: `NotifyingReinforcementController`, the single mutation path registered with `ReinforcementControlApi`.

- [ ] **Step 1: Write failing channel-isolation tests**

Use fake transport/logger classes and cover:

```csharp
[Fact]
public void Both_mode_attempts_cassie_when_broadcast_throws()
{
    FakeTransport transport = new() { ThrowOnBroadcast = true };
    FakeLogger logger = new();
    NotificationService service = TestNotifications.CreateService(
        NotificationMode.Both, transport, logger);

    service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

    Assert.Equal(1, transport.BroadcastAttempts);
    Assert.Equal(1, transport.CassieAttempts);
    Assert.Single(logger.Errors);
}

[Fact]
public void Empty_channel_message_is_skipped_without_throwing()
{
    FakeTransport transport = new();
    NotificationService service = TestNotifications.CreateService(
        NotificationMode.Both, transport, new FakeLogger(), broadcastMessage: string.Empty);

    service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

    Assert.Equal(0, transport.BroadcastAttempts);
    Assert.Equal(1, transport.CassieAttempts);
}
```

- [ ] **Step 2: Run notification tests and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter NotificationServiceTests
```

Expected: build fails because transport and notification service types do not exist.

- [ ] **Step 3: Implement transport-independent notification orchestration**

`NotificationService.Notify` selects the correct node, renders BC and CASSIE text independently, and wraps each transport call in a separate `try/catch`. `None` sends nothing; `Broadcast` sends BC only; `Cassie` sends CASSIE only; `Both` attempts both even if the first fails.

`UpdateConfig` normalizes first, then replaces the complete notification configuration reference in one assignment. Add a test that updates `SkipTriggered` from `None` to `Broadcast` and proves the next call uses the new template without reconstructing the service.

Use these transport signatures:

```csharp
public interface INotificationService
{
    void Notify(NotificationKind kind, NotificationContext context);
    void UpdateConfig(NotificationsConfig config);
}

public interface INotificationTransport
{
    void SendBroadcast(string message, ushort duration, bool clearPrevious);
    void SendCassie(string message, string subtitles, bool playBackground, float priority, float glitchScale);
}
```

- [ ] **Step 4: Implement the LabAPI transport**

```csharp
public sealed class LabApiNotificationTransport : INotificationTransport
{
    public void SendBroadcast(string message, ushort duration, bool clearPrevious) =>
        Server.SendBroadcast(message, duration, shouldClearPrevious: clearPrevious);

    public void SendCassie(
        string message,
        string subtitles,
        bool playBackground,
        float priority,
        float glitchScale) =>
        Announcer.Message(message, subtitles, playBackground, priority, glitchScale);
}
```

The logger adapter delegates warnings/errors to `LabApi.Features.Console.Logger` and includes the notification configuration path in every failure.

- [ ] **Step 5: Implement the notification-aware controller and test duplicate suppression**

`NotifyingReinforcementController` implements `IReinforcementController`, delegates state storage and wave evaluation to an inner controller, and forwards its events. For changed `SetEnabled` transitions, send `EnableApplied` or `DisableApplied`; for changed `ArmSkip`, send `SkipArmed`. `ClearSkip`, `Reset`, `ResetForRound`, `EvaluateWave`, and unchanged transitions do not send command-time notifications.

Add a test proving that both a direct controller call and a later `ReinforcementControlApi` call use this same decorator, while a duplicate disable produces only one `DisableApplied` notification.

- [ ] **Step 6: Run notification tests and Release build**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter NotificationServiceTests
dotnet build src/ReinforcementGate/ReinforcementGate.csproj -c Release
```

Expected: tests pass; plugin builds against LabAPI 1.1.7 without using the obsolete `Cassie` wrapper.

- [ ] **Step 7: Commit notification delivery**

```powershell
git add src/ReinforcementGate/Notifications src/ReinforcementGate/Control tests/ReinforcementGate.Tests/NotificationServiceTests.cs
git commit -m "feat: deliver reinforcement notifications"
```

---

### Task 6: Implement command parsing, status output, and RA permissions

**Files:**
- Create: `src/ReinforcementGate/Commands/ReinforcementCommandAction.cs`
- Create: `src/ReinforcementGate/Commands/ReinforcementCommandRequest.cs`
- Create: `src/ReinforcementGate/Commands/ReinforcementCommandParser.cs`
- Create: `src/ReinforcementGate/Commands/ReinforcementStatusFormatter.cs`
- Create: `src/ReinforcementGate/Commands/ReinforcementCommand.cs`
- Test: `tests/ReinforcementGate.Tests/ReinforcementCommandTests.cs`

**Interfaces:**
- Consumes: `ReinforcementStatesApi` and notification-aware `ReinforcementControlApi`.
- Produces: RA command `reinforcement` with alias `rf`.

- [ ] **Step 1: Write failing parser and authorization tests**

Cover exact aliases and permissions through a pure parser/policy seam:

```csharp
[Theory]
[InlineData("ntf", ReinforcementTarget.Ntf)]
[InlineData("ntf-mini", ReinforcementTarget.NtfMini)]
[InlineData("ci", ReinforcementTarget.Ci)]
[InlineData("ci-mini", ReinforcementTarget.CiMini)]
[InlineData("all", ReinforcementTarget.All)]
public void Parser_accepts_exact_target_names(string text, ReinforcementTarget expected)
{
    Assert.True(ReinforcementCommandParser.TryParseTarget(text, out ReinforcementTarget actual));
    Assert.Equal(expected, actual);
}

[Fact]
public void Status_does_not_require_respawn_events_permission()
{
    Assert.False(ReinforcementCommandParser.RequiresRespawnEvents(
        ReinforcementCommandAction.Status));
}

[Theory]
[InlineData(ReinforcementCommandAction.Enable)]
[InlineData(ReinforcementCommandAction.Disable)]
[InlineData(ReinforcementCommandAction.Skip)]
[InlineData(ReinforcementCommandAction.Reset)]
public void Mutations_require_respawn_events_permission(ReinforcementCommandAction action)
{
    Assert.True(ReinforcementCommandParser.RequiresRespawnEvents(action));
}
```

Add formatter assertions that status includes global gate, global skip, and for each target: local state, effective state, skip state, and source fields.

- [ ] **Step 2: Run command tests and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ReinforcementCommandTests
```

Expected: build fails because command parser and formatter do not exist.

- [ ] **Step 3: Implement pure parsing and formatting**

Parser grammar:

```text
status
reset
enable <all|ntf|ntf-mini|ci|ci-mini>
disable <all|ntf|ntf-mini|ci|ci-mini>
skip <all|ntf|ntf-mini|ci|ci-mini>
```

Parsing is case-insensitive but status output always prints canonical lowercase target names. Reject extra or missing arguments with the exact usage string from the design.

- [ ] **Step 4: Implement the Remote Admin adapter**

Register only to RA:

```csharp
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ReinforcementCommand : ICommand
{
    public string Command => "reinforcement";
    public string[] Aliases => ["rf"];
    public string Description => "Inspect or control reinforcement waves.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!ReinforcementCommandParser.TryParse(arguments, out ReinforcementCommandRequest? request, out response))
            return false;

        if (ReinforcementCommandParser.RequiresRespawnEvents(request.Action) &&
            !sender.CheckPermission(PlayerPermissions.RespawnEvents))
        {
            response = "Missing Remote Admin permission: RespawnEvents.";
            return false;
        }

        string source = Player.Get(sender)?.Nickname ?? sender.SenderId;
        return ExecuteRequest(request, source, out response);
    }
}
```

`ExecuteRequest` returns current state for `status` and delegates every mutation to `ReinforcementControlApi`. The notification-aware controller from Task 5 sends `EnableApplied`, `DisableApplied`, or `SkipArmed` only for changed transitions, so the command must not send notifications itself. Report “state unchanged” for duplicates. `reset` emits no BC/CASSIE node.

- [ ] **Step 5: Run command tests and build**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ReinforcementCommandTests
dotnet build src/ReinforcementGate/ReinforcementGate.csproj -c Release
```

Expected: parser/formatter tests pass; the RA adapter compiles with `RespawnEvents`; no client or game-console command handler is registered.

- [ ] **Step 6: Commit RA commands**

```powershell
git add src/ReinforcementGate/Commands tests/ReinforcementGate.Tests/ReinforcementCommandTests.cs
git commit -m "feat: add reinforcement RA command"
```

---

### Task 7: Classify and block LabAPI waves at the cancellable event

**Files:**
- Create: `src/ReinforcementGate/Interception/WaveClassifier.cs`
- Create: `src/ReinforcementGate/Interception/WaveInterceptionService.cs`
- Create: `src/ReinforcementGate/Interception/IInterceptionLogger.cs`
- Test: `tests/ReinforcementGate.Tests/WaveInterceptionServiceTests.cs`

**Interfaces:**
- Consumes: `IReinforcementController`, `INotificationService`, and `ReinforcementEvents`.
- Produces: a boolean block result used to set `WaveRespawningEventArgs.IsAllowed`.

- [ ] **Step 1: Write failing interception behavior tests**

Use the real state service with fake notification/logger dependencies:

```csharp
[Fact]
public void Skip_trigger_blocks_once_and_sends_skip_triggered()
{
    ReinforcementStateService state = new();
    state.ArmSkip(ReinforcementTarget.CiMini, "Admin");
    RecordingNotifications notifications = new();
    WaveInterceptionService service = new(state, notifications);

    Assert.True(service.ShouldBlock(ReinforcementTarget.CiMini));
    Assert.False(service.ShouldBlock(ReinforcementTarget.CiMini));
    Assert.Single(notifications.Items, x => x.Kind == NotificationKind.SkipTriggered);
}

[Fact]
public void Persistent_block_uses_disabled_wave_notification_and_preserves_skip()
{
    ReinforcementStateService state = new();
    state.SetEnabled(ReinforcementTarget.Ntf, false, "Admin A");
    state.ArmSkip(ReinforcementTarget.Ntf, "Admin B");
    RecordingNotifications notifications = new();
    WaveInterceptionService service = new(state, notifications);

    Assert.True(service.ShouldBlock(ReinforcementTarget.Ntf));
    Assert.True(state.GetState(ReinforcementTarget.Ntf).IsSkipArmed);
    Assert.Single(notifications.Items, x => x.Kind == NotificationKind.DisabledWaveBlocked);
}
```

Also assert notification exceptions are swallowed after the state decision and cannot flip a block to allow.

- [ ] **Step 2: Run interception tests and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter WaveInterceptionServiceTests
```

Expected: build fails because interception service does not exist.

- [ ] **Step 3: Implement the exact LabAPI wave mapping**

```csharp
public static bool TryClassify(RespawnWave wave, out ReinforcementTarget target)
{
    switch (wave)
    {
        case MtfWave:
            target = ReinforcementTarget.Ntf;
            return true;
        case MiniMtfWave:
            target = ReinforcementTarget.NtfMini;
            return true;
        case ChaosWave:
            target = ReinforcementTarget.Ci;
            return true;
        case MiniChaosWave:
            target = ReinforcementTarget.CiMini;
            return true;
        default:
            target = default;
            return false;
    }
}
```

- [ ] **Step 4: Implement interception orchestration**

`ShouldBlock(target)` calls `EvaluateWave` exactly once. Allowed decisions return `false`. Blocked decisions publish immutable `WaveBlockedEventArgs`; `TargetSkip` and `GlobalSkip` use `SkipTriggered`; persistent reasons use `DisabledWaveBlocked`. Wrap event publication and notification separately so either failure is logged and the method still returns `true`.

- [ ] **Step 5: Run interception tests and build**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter WaveInterceptionServiceTests
dotnet build ReinforcementGate.sln -c Release
```

Expected: all decision, one-shot, precedence, failure-isolation, and event tests pass.

- [ ] **Step 6: Commit wave interception**

```powershell
git add src/ReinforcementGate/Interception tests/ReinforcementGate.Tests/WaveInterceptionServiceTests.cs
git commit -m "feat: intercept reinforcement waves"
```

---

### Task 8: Compose the LabAPI plugin lifecycle and round reset

**Files:**
- Create: `src/ReinforcementGate/ReinforcementGatePlugin.cs`
- Create: `src/ReinforcementGate/ReinforcementEventsHandler.cs`
- Test: `tests/ReinforcementGate.Tests/PluginLifecycleContractTests.cs`

**Interfaces:**
- Consumes: every service and adapter from Tasks 2-7.
- Produces: one loadable `ReinforcementGate.dll` with safe enable/disable/reload behavior.

- [ ] **Step 1: Write lifecycle contract tests**

Because a live SCP:SL server is not available to unit tests, reflect over the plugin assembly and assert:

```csharp
[Fact]
public void Plugin_metadata_targets_the_approved_api()
{
    ReinforcementGatePlugin plugin = new();
    Assert.Equal("ReinforcementGate", plugin.Name);
    Assert.Equal(new Version(1, 1, 7), plugin.RequiredApiVersion);
    Assert.False(plugin.IsTransparent);
    Assert.Equal("reinforcement-gate.yml", plugin.ConfigFileName);
}

[Fact]
public void Command_is_registered_only_for_remote_admin()
{
    CustomAttributeData attribute = CustomAttributeData
        .GetCustomAttributes(typeof(ReinforcementCommand))
        .Single(x => x.AttributeType == typeof(CommandHandlerAttribute));

    Assert.Single(attribute.ConstructorArguments);
    Assert.Equal(typeof(RemoteAdminCommandHandler), attribute.ConstructorArguments[0].Value);
}
```

- [ ] **Step 2: Run lifecycle tests and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter PluginLifecycleContractTests
```

Expected: build fails because the plugin composition root and event handler do not exist.

- [ ] **Step 3: Implement the LabAPI event handler**

```csharp
public sealed class ReinforcementEventsHandler : CustomEventsHandler
{
    private readonly IReinforcementController _controller;
    private readonly WaveInterceptionService _interception;
    private readonly IInterceptionLogger _logger;
    private readonly HashSet<Type> _warnedUnknownWaveTypes = new();

    public ReinforcementEventsHandler(
        IReinforcementController controller,
        WaveInterceptionService interception,
        IInterceptionLogger logger)
    {
        _controller = controller;
        _interception = interception;
        _logger = logger;
    }

    public override void OnServerRoundStarted()
    {
        _controller.ResetForRound();
    }

    public override void OnServerWaveRespawning(WaveRespawningEventArgs ev)
    {
        if (!WaveClassifier.TryClassify(ev.Wave, out ReinforcementTarget target))
        {
            if (_warnedUnknownWaveTypes.Add(ev.Wave.GetType()))
                _logger.Warn($"Unknown reinforcement wave type allowed: {ev.Wave.GetType().FullName}");
            return;
        }

        if (_interception.ShouldBlock(target))
            ev.IsAllowed = false;
    }
}
```

- [ ] **Step 4: Implement plugin composition and strict teardown order**

`ReinforcementGatePlugin` derives from `Plugin<ReinforcementGateConfig>`, declares version `1.0.0`, required API `1.1.7`, author `ReinforcementGate Contributors`, description from the design, and `IsTransparent => false`.

Override `LoadConfigs()` to call `base.LoadConfigs()`, normalize the newly loaded configuration, and call `_notificationService?.UpdateConfig(Config.Notifications)`. This makes LabAPI config reloads update templates while the plugin remains enabled.

Enable in this order:

1. Normalize `Config`.
2. Construct logger, transport, notification service, state service, `NotifyingReinforcementController`, interception service, and event handler.
3. Register `ReinforcementStatesApi` with the notification-aware controller.
4. Register `ReinforcementControlApi` with the same notification-aware controller.
5. Register the custom event handler.

Disable in this order:

1. Unregister the custom event handler.
2. Unregister `ReinforcementControlApi`.
3. Unregister `ReinforcementStatesApi`.
4. Clear `ReinforcementEvents` subscribers.
5. Null private service fields.

Do not send reset notifications during enable, disable, reload, or round start.

- [ ] **Step 5: Run lifecycle tests and full test suite**

```powershell
dotnet test ReinforcementGate.sln -c Release
dotnet build ReinforcementGate.sln -c Release
```

Expected: all tests pass; Release output contains `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`; no obsolete-API warning exists.

- [ ] **Step 6: Commit the plugin lifecycle**

```powershell
git add src/ReinforcementGate/ReinforcementGatePlugin.cs src/ReinforcementGate/ReinforcementEventsHandler.cs tests/ReinforcementGate.Tests/PluginLifecycleContractTests.cs
git commit -m "feat: compose LabAPI plugin lifecycle"
```

---

### Task 9: Write the GitHub README and public API examples

**Files:**
- Create: `README.md`
- Create: `tests/ReinforcementGate.Tests/RepositoryRoot.cs`
- Test: `tests/ReinforcementGate.Tests/ReadmeContractTests.cs`

**Interfaces:**
- Consumes: final command names, configuration fields, API signatures, and compatibility constraints.
- Produces: the GitHub-facing documentation explicitly requested by the user.

- [ ] **Step 1: Write a failing README contract test**

```csharp
[Fact]
public void Readme_documents_required_public_surface()
{
    string readme = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "README.md"));
    string[] required =
    {
        "reinforcement", "rf", "ntf-mini", "ci-mini", "RespawnEvents",
        "ReinforcementStatesApi", "ReinforcementControlApi", "StateChanged",
        "Broadcast", "Cassie", "{target_name}", "LabAPI 1.1.7",
        "enable_applied", "disable_applied", "disabled_wave_blocked",
        "skip_armed", "skip_triggered",
    };

    foreach (string value in required)
        Assert.Contains(value, readme, StringComparison.Ordinal);
}
```

Add `RepositoryRoot.Find()` as a test-only helper that walks parents from `AppContext.BaseDirectory` until it finds `ReinforcementGate.sln`.

- [ ] **Step 2: Run README test and verify red**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ReadmeContractTests
```

Expected: test fails because `README.md` does not exist.

- [ ] **Step 3: Write the complete README**

Use these sections in order:

```text
ReinforcementGate
Features
Compatibility
Installation
Remote Admin commands
State and skip precedence
Permission model
Configuration
Template placeholders
Read-only States API
Control API
Public events
Building and testing
Known limits
```

Document installation under `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll`, while noting that hoster-policy layouts use the server-local `AppData` equivalent.

Document the per-port configuration path as `%AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\ReinforcementGate\reinforcement-gate.yml`; `<port>` is the actual SCP:SL server port directory created by LabAPI.

Include this read-only API example:

```csharp
if (ReinforcementStatesApi.IsAvailable)
{
    ReinforcementStateSnapshot snapshot = ReinforcementStatesApi.GetSnapshot();
    ReinforcementTargetState ntf = snapshot.Targets[ReinforcementTarget.Ntf];
    Logger.Info($"NTF effective enabled: {ntf.IsEffectivelyEnabled}");
}
```

Include this control API example:

```csharp
ReinforcementControlApi.ArmSkip(
    ReinforcementTarget.CiMini,
    "ExamplePlugin");
```

Explicitly state that public APIs are synchronous and must be called on the server main thread, read-only state queries require no RA permission, and control API calls use the supplied `source` rather than RA permissions.

- [ ] **Step 4: Run README contract and consistency checks**

```powershell
dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --filter ReadmeContractTests
rg -n "ntf-main|chaos-main|FacilityManagement|ReinforcementApi\." README.md src tests
```

Expected: README test passes; `rg` returns no matches.

- [ ] **Step 5: Commit GitHub documentation**

```powershell
git add README.md tests/ReinforcementGate.Tests/ReadmeContractTests.cs
git commit -m "docs: document commands config and APIs"
```

---

### Task 10: Perform final automated and live-server verification

**Files:**
- Modify only if verification exposes a defect: the smallest responsible production/test file.
- Verify: `src/ReinforcementGate/bin/Release/net48/ReinforcementGate.dll`
- Verify: `README.md`

**Interfaces:**
- Consumes: complete plugin from Tasks 1-9.
- Produces: verified Release DLL and an evidence-backed handoff.

- [ ] **Step 1: Run clean automated verification**

```powershell
dotnet clean ReinforcementGate.sln -c Release
dotnet test ReinforcementGate.sln -c Release --logger "console;verbosity=normal"
dotnet build ReinforcementGate.sln -c Release --no-restore
```

Expected: zero failed tests, zero warnings, zero build errors, and a Release DLL at the documented path.

- [ ] **Step 2: Inspect the compiled public surface**

Run a reflection-based test that asserts these public types and members exist:

```text
ReinforcementStatesApi.IsAvailable
ReinforcementStatesApi.GetSnapshot
ReinforcementStatesApi.GetState
ReinforcementStatesApi.TryGetState
ReinforcementStatesApi.StateChanged
ReinforcementStatesApi.RoundStateReset
ReinforcementControlApi.SetEnabled
ReinforcementControlApi.ArmSkip
ReinforcementControlApi.ClearSkip
ReinforcementControlApi.Reset
ReinforcementEvents.WaveBlocked
```

Expected: reflection contract test passes without missing or renamed members.

- [ ] **Step 3: Verify on a LabAPI 1.1.7 test server**

Copy only `ReinforcementGate.dll` to the configured LabAPI plugin path, start a test round, and execute this matrix:

```text
rf status
rf disable ntf
rf skip ntf
rf enable ntf
rf skip all
rf disable ntf-mini
rf disable ci
rf disable ci-mini
rf disable all
rf enable all
rf reset
```

Expected:

- RA without `RespawnEvents` can run `rf status` but all mutations fail.
- RA with `RespawnEvents` can run every command.
- `ntf`, `ntf-mini`, `ci`, and `ci-mini` match the four real LabAPI wrapper types.
- Target skip is consumed before global skip.
- Persistent blocks do not consume one-shot skips.
- Round start restores all four targets, clears the global gate, and clears all skips.
- BC and CASSIE independently obey `None`, `Broadcast`, `Cassie`, and `Both`.
- Existing players retain role, team, inventory, and life state.
- Unknown wave types, if introduced by another plugin, are allowed and warned once.

- [ ] **Step 4: Verify reload safety**

Reload ReinforcementGate twice, then trigger one state change and one blocked wave.

Expected: one state event, one block event, and one configured notification; no duplicate command registration or handler invocation.

- [ ] **Step 5: Record final repository state and commit verification-only fixes**

```powershell
git status --short
git log --oneline --decorate -10
```

Expected: working tree is clean. If a verification defect required a fix, rerun Steps 1-4 and commit the focused change with `fix: correct verified reinforcement behavior` before reporting completion.

---

## Spec Coverage Matrix

| Specification requirement | Implementation task |
| --- | --- |
| Four exact NTF/CI primary/mini types | Tasks 2 and 7 |
| Global gate preserving local states | Task 2 |
| Global and target one-shot skip precedence | Tasks 2 and 7 |
| Round-start reset | Tasks 2 and 8 |
| `reinforcement` / `rf` commands and exact targets | Task 6 |
| `status` open to RA; mutations require `RespawnEvents` | Task 6 |
| Configurable command-time and block-time BC/CASSIE | Tasks 4 and 5 |
| Notification failure isolation | Tasks 5 and 7 |
| Read-only cross-plugin States API | Task 3 |
| Cross-plugin control API and immutable events | Task 3 |
| No existing-player, timer, influence, token, or wave-size mutation | Tasks 7, 8, and 10 |
| Unknown-wave fail-open behavior | Tasks 7 and 8 |
| Safe plugin reload and API teardown | Tasks 3, 8, and 10 |
| GitHub README with commands, config, and API examples | Task 9 |
| Automated and live-server verification | Task 10 |
