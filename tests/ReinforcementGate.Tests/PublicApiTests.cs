using System;
using ReinforcementGate.Api;
using ReinforcementGate.Domain;
using ReinforcementGate.State;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class PublicApiTests : IDisposable
{
    private readonly ReinforcementStateService _service = new();

    public PublicApiTests()
    {
        ReinforcementControlApi.Unregister(_service);
        ReinforcementStatesApi.Unregister(_service);
        ReinforcementEvents.ClearSubscribers();
    }

    [Fact]
    public void States_api_reports_unavailable_before_registration_and_after_unregistration()
    {
        Assert.False(ReinforcementStatesApi.IsAvailable);
        Assert.Throws<InvalidOperationException>(() => ReinforcementStatesApi.GetSnapshot());

        ReinforcementStatesApi.Register(_service);
        Assert.True(ReinforcementStatesApi.IsAvailable);

        ReinforcementStatesApi.Unregister(_service);
        Assert.False(ReinforcementStatesApi.IsAvailable);
        Assert.False(ReinforcementStatesApi.TryGetState(ReinforcementTarget.Ntf, out _));
    }

    [Fact]
    public void Control_api_delegates_to_the_registered_controller()
    {
        ReinforcementStatesApi.Register(_service);
        ReinforcementControlApi.Register(_service);

        StateTransitionResult result = ReinforcementControlApi.SetEnabled(
            ReinforcementTarget.Ci,
            false,
            "OtherPlugin");

        Assert.True(result.Changed);
        Assert.False(ReinforcementStatesApi.GetState(ReinforcementTarget.Ci).IsEffectivelyEnabled);
    }

    [Fact]
    public void Control_api_throws_when_no_controller_is_registered()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ReinforcementControlApi.ArmSkip(ReinforcementTarget.NtfMini, "OtherPlugin"));

        Assert.Contains("not available", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void States_api_forwards_provider_events()
    {
        ReinforcementStatesApi.Register(_service);
        ReinforcementStateChangedEventArgs? stateArgs = null;
        object? stateSender = null;
        int roundResets = 0;
        object? roundSender = null;
        ReinforcementStatesApi.StateChanged += (sender, args) =>
        {
            stateSender = sender;
            stateArgs = args;
        };
        ReinforcementStatesApi.RoundStateReset += (sender, _) =>
        {
            roundSender = sender;
            roundResets++;
        };

        _service.SetEnabled(ReinforcementTarget.Ntf, false, "Admin");
        _service.ResetForRound();

        Assert.NotNull(stateArgs);
        Assert.Equal("round-start", stateArgs!.Transition.Source);
        Assert.Equal(1, roundResets);
        Assert.Null(stateSender);
        Assert.Null(roundSender);
        Assert.NotSame(_service, stateSender);
        Assert.NotSame(_service, roundSender);
    }

    [Fact]
    public void States_api_unregistration_requires_same_provider_and_clears_public_subscribers()
    {
        ReinforcementStateService other = new();
        ReinforcementStatesApi.Register(_service);
        int calls = 0;
        ReinforcementStatesApi.StateChanged += (_, _) => calls++;

        ReinforcementStatesApi.Unregister(other);
        Assert.True(ReinforcementStatesApi.IsAvailable);

        ReinforcementStatesApi.Unregister(_service);
        ReinforcementStatesApi.Register(_service);
        _service.SetEnabled(ReinforcementTarget.Ntf, false, "Admin");

        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData(ReinforcementTarget.All)]
    [InlineData((ReinforcementTarget)999)]
    public void States_api_rejects_invalid_concrete_targets(ReinforcementTarget target)
    {
        ReinforcementStatesApi.Register(_service);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => ReinforcementStatesApi.GetState(target));

        Assert.Equal("target", exception.ParamName);
        Assert.False(ReinforcementStatesApi.TryGetState(target, out ReinforcementTargetState? state));
        Assert.Null(state);
    }

    [Fact]
    public void Wave_blocked_event_args_are_immutable_and_publish_the_supplied_snapshot()
    {
        ReinforcementStateSnapshot snapshot = _service.GetSnapshot();
        WaveBlockedEventArgs args = new(
            ReinforcementTarget.CiMini,
            ReinforcementBlockReason.TargetSkip,
            "OtherPlugin",
            snapshot);
        WaveBlockedEventArgs? observed = null;
        ReinforcementEvents.WaveBlocked += (_, eventArgs) => observed = eventArgs;

        ReinforcementEvents.PublishWaveBlocked(args);

        Assert.Same(args, observed);
        Assert.Equal(ReinforcementTarget.CiMini, args.Target);
        Assert.Equal(ReinforcementBlockReason.TargetSkip, args.Reason);
        Assert.Equal("OtherPlugin", args.Source);
        Assert.Same(snapshot, args.StateSnapshot);
        Assert.All(
            typeof(WaveBlockedEventArgs).GetProperties(),
            property => Assert.False(property.CanWrite));
    }

    [Fact]
    public void Wave_blocked_subscribers_can_be_cleared_for_plugin_reload()
    {
        int calls = 0;
        ReinforcementEvents.WaveBlocked += (_, _) => calls++;
        ReinforcementEvents.ClearSubscribers();

        ReinforcementEvents.PublishWaveBlocked(new WaveBlockedEventArgs(
            ReinforcementTarget.Ntf,
            ReinforcementBlockReason.GlobalDisabled,
            "Admin",
            _service.GetSnapshot()));

        Assert.Equal(0, calls);
    }

    public void Dispose()
    {
        ReinforcementControlApi.Unregister(_service);
        ReinforcementStatesApi.Unregister(_service);
        ReinforcementEvents.ClearSubscribers();
    }
}
