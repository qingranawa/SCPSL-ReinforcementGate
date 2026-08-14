using System;

namespace ReinforcementGate.Domain;

/// <summary>Provides immutable details about a blocked reinforcement wave.</summary>
public sealed class WaveBlockedEventArgs : EventArgs
{
    /// <summary>Initializes blocked-wave event data.</summary>
    public WaveBlockedEventArgs(
        ReinforcementTarget target,
        ReinforcementBlockReason reason,
        string source,
        ReinforcementStateSnapshot stateSnapshot)
    {
        if (target == ReinforcementTarget.All || !Enum.IsDefined(typeof(ReinforcementTarget), target))
            throw new ArgumentOutOfRangeException(nameof(target), target, "A concrete reinforcement target is required.");
        if (!Enum.IsDefined(typeof(ReinforcementBlockReason), reason))
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown reinforcement block reason.");
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null, empty, or whitespace.", nameof(source));

        Target = target;
        Reason = reason;
        Source = source;
        StateSnapshot = stateSnapshot ?? throw new ArgumentNullException(nameof(stateSnapshot));
    }

    /// <summary>Gets the concrete reinforcement target.</summary>
    public ReinforcementTarget Target { get; }

    /// <summary>Gets the reason the wave was blocked.</summary>
    public ReinforcementBlockReason Reason { get; }

    /// <summary>Gets the source responsible for the blocking state.</summary>
    public string Source { get; }

    /// <summary>Gets state captured after any one-shot skip was consumed.</summary>
    public ReinforcementStateSnapshot StateSnapshot { get; }
}
