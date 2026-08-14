using System;
using System.Threading.Tasks;
using ReinforcementGate.Domain;
using ReinforcementGate.State;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class ReinforcementStateServiceTests
{
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

        Assert.Equal(
            ReinforcementBlockReason.TargetDisabled,
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

    [Fact]
    public void Round_reset_always_publishes_round_event()
    {
        ReinforcementStateService state = new();
        int stateEvents = 0;
        int roundEvents = 0;
        state.StateChanged += (_, _) => stateEvents++;
        state.RoundStateReset += (_, _) => roundEvents++;

        StateTransitionResult first = state.ResetForRound();
        StateTransitionResult second = state.ResetForRound();

        Assert.True(first.Changed);
        Assert.False(second.Changed);
        Assert.Equal(1, stateEvents);
        Assert.Equal(2, roundEvents);
    }

    [Theory]
    [InlineData(ReinforcementTarget.Ntf)]
    [InlineData(ReinforcementTarget.All)]
    public void Repeated_enable_and_disable_with_new_sources_are_no_op_transitions(ReinforcementTarget target)
    {
        ReinforcementStateService state = new();
        StateTransitionResult firstDisable = state.SetEnabled(target, false, "disable-first");
        StateTransitionResult repeatedDisable = state.SetEnabled(target, false, "disable-second");
        StateTransitionResult firstEnable = state.SetEnabled(target, true, "enable-first");
        StateTransitionResult repeatedEnable = state.SetEnabled(target, true, "enable-second");

        Assert.True(firstDisable.Changed);
        Assert.False(repeatedDisable.Changed);
        Assert.Equal("disable-first", GetEnabledSource(repeatedDisable.After, target));
        Assert.True(firstEnable.Changed);
        Assert.False(repeatedEnable.Changed);
        Assert.Equal("enable-first", GetEnabledSource(repeatedEnable.After, target));
    }

    [Theory]
    [InlineData(ReinforcementTarget.Ci)]
    [InlineData(ReinforcementTarget.All)]
    public void Repeated_arm_and_clear_skip_with_new_sources_are_no_op_transitions(ReinforcementTarget target)
    {
        ReinforcementStateService state = new();
        StateTransitionResult firstArm = state.ArmSkip(target, "arm-first");
        StateTransitionResult repeatedArm = state.ArmSkip(target, "arm-second");
        StateTransitionResult firstClear = state.ClearSkip(target, "clear-first");
        StateTransitionResult repeatedClear = state.ClearSkip(target, "clear-second");

        Assert.True(firstArm.Changed);
        Assert.False(repeatedArm.Changed);
        Assert.Equal("arm-first", GetSkipSource(repeatedArm.After, target));
        Assert.True(firstClear.Changed);
        Assert.False(repeatedClear.Changed);
        Assert.Equal("clear-first", GetSkipSource(repeatedClear.After, target));
    }

    [Fact]
    public void Throwing_state_listener_cannot_interrupt_skip_block_decision()
    {
        ReinforcementStateService state = new();
        state.ArmSkip(ReinforcementTarget.NtfMini, "skip-source");
        int healthyListenerCalls = 0;
        state.StateChanged += (_, _) => throw new InvalidOperationException("subscriber failure");
        state.StateChanged += (_, _) => healthyListenerCalls++;

        WaveDecision decision = state.EvaluateWave(ReinforcementTarget.NtfMini);

        Assert.True(decision.IsBlocked);
        Assert.Equal(ReinforcementBlockReason.TargetSkip, decision.Reason);
        Assert.Equal("skip-source", decision.Source);
        Assert.NotNull(decision.SkipConsumption);
        Assert.False(state.GetState(ReinforcementTarget.NtfMini).IsSkipArmed);
        Assert.Equal(1, healthyListenerCalls);
    }

    [Fact]
    public void State_changed_listener_can_read_committed_state_without_state_lock()
    {
        ReinforcementStateService state = new();
        bool readCompleted = false;
        bool observedDisabled = false;
        state.StateChanged += (_, _) =>
        {
            Task<ReinforcementStateSnapshot> read = Task.Run(state.GetSnapshot);
            readCompleted = read.Wait(TimeSpan.FromMilliseconds(500));
            if (readCompleted)
                observedDisabled = read.Result.Targets[ReinforcementTarget.Ntf].IsLocallyEnabled is false;
        };

        state.SetEnabled(ReinforcementTarget.Ntf, false, "Admin");

        Assert.True(readCompleted);
        Assert.True(observedDisabled);
    }

    [Fact]
    public void Skip_consumption_listener_can_read_committed_state_without_state_lock()
    {
        ReinforcementStateService state = new();
        state.ArmSkip(ReinforcementTarget.Ntf, "SkipAdmin");
        bool readCompleted = false;
        bool observedConsumed = false;
        state.StateChanged += (_, _) =>
        {
            Task<ReinforcementStateSnapshot> read = Task.Run(state.GetSnapshot);
            readCompleted = read.Wait(TimeSpan.FromMilliseconds(500));
            if (readCompleted)
                observedConsumed = !read.Result.Targets[ReinforcementTarget.Ntf].IsSkipArmed;
        };

        WaveDecision decision = state.EvaluateWave(ReinforcementTarget.Ntf);

        Assert.Equal(ReinforcementBlockReason.TargetSkip, decision.Reason);
        Assert.True(readCompleted);
        Assert.True(observedConsumed);
    }

    [Fact]
    public void Round_state_reset_listener_can_read_committed_state()
    {
        ReinforcementStateService state = new();
        state.SetEnabled(ReinforcementTarget.Ci, false, "Admin");
        bool readCompleted = false;
        bool observedEnabled = false;
        state.RoundStateReset += (_, _) =>
        {
            Task<ReinforcementStateSnapshot> read = Task.Run(state.GetSnapshot);
            readCompleted = read.Wait(TimeSpan.FromMilliseconds(500));
            if (readCompleted)
                observedEnabled = read.Result.Targets[ReinforcementTarget.Ci].IsEffectivelyEnabled;
        };

        state.ResetForRound();

        Assert.True(readCompleted);
        Assert.True(observedEnabled);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Mutations_reject_invalid_sources(string? source)
    {
        ReinforcementStateService state = new();

        Assert.Throws<ArgumentException>(() => state.ArmSkip(ReinforcementTarget.Ntf, source!));
    }

    [Fact]
    public void Concrete_state_operations_reject_all()
    {
        ReinforcementStateService state = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => state.GetState(ReinforcementTarget.All));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.TryGetState(ReinforcementTarget.All, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => state.EvaluateWave(ReinforcementTarget.All));
    }

    private static string GetEnabledSource(ReinforcementStateSnapshot snapshot, ReinforcementTarget target) =>
        target == ReinforcementTarget.All
            ? snapshot.GlobalDisabledLastChangedBy
            : snapshot.Targets[target].EnabledLastChangedBy;

    private static string GetSkipSource(ReinforcementStateSnapshot snapshot, ReinforcementTarget target) =>
        target == ReinforcementTarget.All
            ? snapshot.GlobalSkipLastChangedBy
            : snapshot.Targets[target].SkipLastChangedBy;
}
