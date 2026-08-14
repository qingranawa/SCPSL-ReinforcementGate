using System;
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

        state.ResetForRound();

        Assert.Equal(1, roundEvents);
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
}
