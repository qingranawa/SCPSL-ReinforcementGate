using System;
using ReinforcementGate.Domain;
using ReinforcementGate.Notifications;
using ReinforcementGate.State;

namespace ReinforcementGate.Control;

/// <summary>Adds command-time notifications to a reinforcement controller.</summary>
public sealed class NotifyingReinforcementController : IReinforcementController
{
    private readonly IReinforcementController _inner;
    private readonly INotificationService _notifications;

    /// <summary>Creates a notification-aware controller decorator.</summary>
    public NotifyingReinforcementController(
        IReinforcementController inner,
        INotificationService notifications)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <inheritdoc />
    public event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged
    {
        add => _inner.StateChanged += value;
        remove => _inner.StateChanged -= value;
    }

    /// <inheritdoc />
    public event EventHandler? RoundStateReset
    {
        add => _inner.RoundStateReset += value;
        remove => _inner.RoundStateReset -= value;
    }

    /// <inheritdoc />
    public ReinforcementStateSnapshot GetSnapshot() => _inner.GetSnapshot();

    /// <inheritdoc />
    public ReinforcementTargetState GetState(ReinforcementTarget target) => _inner.GetState(target);

    /// <inheritdoc />
    public bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state) =>
        _inner.TryGetState(target, out state);

    /// <inheritdoc />
    public StateTransitionResult SetEnabled(
        ReinforcementTarget target,
        bool enabled,
        string source)
    {
        StateTransitionResult result = _inner.SetEnabled(target, enabled, source);
        if (!result.Changed)
            return result;

        NotificationKind kind = enabled
            ? NotificationKind.EnableApplied
            : NotificationKind.DisableApplied;
        string action = enabled ? "enable" : "disable";
        string reason = enabled
            ? string.Empty
            : target == ReinforcementTarget.All
                ? "global-disabled"
                : "target-disabled";
        TryNotify(kind, CreateContext(result, action, reason));
        return result;
    }

    /// <inheritdoc />
    public StateTransitionResult ArmSkip(ReinforcementTarget target, string source)
    {
        StateTransitionResult result = _inner.ArmSkip(target, source);
        if (result.Changed)
            TryNotify(NotificationKind.SkipArmed, CreateContext(result, "skip", "skip"));
        return result;
    }

    /// <inheritdoc />
    public StateTransitionResult ClearSkip(ReinforcementTarget target, string source) =>
        _inner.ClearSkip(target, source);

    /// <inheritdoc />
    public StateTransitionResult Reset(string source) => _inner.Reset(source);

    /// <inheritdoc />
    public StateTransitionResult ResetForRound() => _inner.ResetForRound();

    /// <inheritdoc />
    public WaveDecision EvaluateWave(ReinforcementTarget target) => _inner.EvaluateWave(target);

    private static NotificationContext CreateContext(
        StateTransitionResult result,
        string action,
        string reason) =>
        new(
            result.Target,
            ReinforcementTargetNames.ToDisplayName(result.Target),
            result.Source,
            action,
            reason);

    private void TryNotify(NotificationKind kind, NotificationContext context)
    {
        try
        {
            _notifications.Notify(kind, context);
        }
        catch
        {
            // A notification implementation cannot invalidate a committed transition.
        }
    }
}
