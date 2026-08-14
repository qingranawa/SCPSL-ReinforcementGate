using System;
using ReinforcementGate.Api;
using ReinforcementGate.Domain;
using ReinforcementGate.Notifications;
using ReinforcementGate.State;

namespace ReinforcementGate.Interception;

/// <summary>Evaluates classified waves and publishes blocked-wave side effects.</summary>
public sealed class WaveInterceptionService
{
    private readonly IReinforcementController _controller;
    private readonly INotificationService _notifications;
    private readonly IInterceptionLogger _logger;

    /// <summary>Initializes interception with silent fallback logging.</summary>
    public WaveInterceptionService(
        IReinforcementController controller,
        INotificationService notifications)
        : this(controller, notifications, NullInterceptionLogger.Instance)
    {
    }

    /// <summary>Initializes interception with explicit non-fatal logging.</summary>
    public WaveInterceptionService(
        IReinforcementController controller,
        INotificationService notifications,
        IInterceptionLogger logger)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Returns whether a classified wave must be blocked.</summary>
    public bool ShouldBlock(ReinforcementTarget target)
    {
        WaveDecision decision = _controller.EvaluateWave(target);
        if (!decision.IsBlocked)
            return false;

        ReinforcementBlockReason reason = decision.Reason ??
            throw new InvalidOperationException("A blocked wave decision must provide a reason.");
        ReinforcementStateSnapshot snapshot = decision.SkipConsumption?.After ?? _controller.GetSnapshot();
        WaveBlockedEventArgs eventArgs = new(target, reason, decision.Source, snapshot);

        try
        {
            ReinforcementEvents.PublishWaveBlocked(
                eventArgs,
                exception => TryLogError("A blocked-wave event subscriber failed.", exception));
        }
        catch (Exception exception)
        {
            TryLogError("Failed to publish the blocked-wave event.", exception);
        }

        try
        {
            _notifications.Notify(
                IsSkip(reason) ? NotificationKind.SkipTriggered : NotificationKind.DisabledWaveBlocked,
                new NotificationContext(
                    target,
                    ReinforcementTargetNames.ToDisplayName(target),
                    decision.Source,
                    "wave-blocked",
                    reason.ToString()));
        }
        catch (Exception exception)
        {
            TryLogError("Failed to deliver the blocked-wave notification.", exception);
        }

        return true;
    }

    private static bool IsSkip(ReinforcementBlockReason reason) =>
        reason == ReinforcementBlockReason.TargetSkip ||
        reason == ReinforcementBlockReason.GlobalSkip;

    private void TryLogError(string message, Exception exception)
    {
        try
        {
            _logger.Error(message, exception);
        }
        catch
        {
            // Logging cannot change an already committed wave decision.
        }
    }

    private sealed class NullInterceptionLogger : IInterceptionLogger
    {
        public static readonly NullInterceptionLogger Instance = new();

        public void Warn(string message)
        {
        }

        public void Error(string message, Exception exception)
        {
        }
    }
}
