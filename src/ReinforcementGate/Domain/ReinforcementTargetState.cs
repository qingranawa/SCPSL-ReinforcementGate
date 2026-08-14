namespace ReinforcementGate.Domain;

/// <summary>Represents immutable state for one reinforcement target.</summary>
public sealed class ReinforcementTargetState
{
    /// <summary>Initializes a reinforcement target state.</summary>
    /// <param name="target">The represented reinforcement target.</param>
    /// <param name="isLocallyEnabled">Whether the target is locally enabled.</param>
    /// <param name="isEffectivelyEnabled">Whether the target is enabled after global state is applied.</param>
    /// <param name="isSkipArmed">Whether a one-shot skip is armed for the target.</param>
    /// <param name="enabledLastChangedBy">The actor that last changed the enabled state.</param>
    /// <param name="skipLastChangedBy">The actor that last changed the skip state.</param>
    public ReinforcementTargetState(
        ReinforcementTarget target,
        bool isLocallyEnabled,
        bool isEffectivelyEnabled,
        bool isSkipArmed,
        string enabledLastChangedBy,
        string skipLastChangedBy)
    {
        Target = target;
        IsLocallyEnabled = isLocallyEnabled;
        IsEffectivelyEnabled = isEffectivelyEnabled;
        IsSkipArmed = isSkipArmed;
        EnabledLastChangedBy = enabledLastChangedBy;
        SkipLastChangedBy = skipLastChangedBy;
    }

    /// <summary>Gets the represented reinforcement target.</summary>
    public ReinforcementTarget Target { get; }

    /// <summary>Gets whether the target is locally enabled.</summary>
    public bool IsLocallyEnabled { get; }

    /// <summary>Gets whether the target is effectively enabled.</summary>
    public bool IsEffectivelyEnabled { get; }

    /// <summary>Gets whether a one-shot skip is armed for the target.</summary>
    public bool IsSkipArmed { get; }

    /// <summary>Gets the actor that last changed the enabled state.</summary>
    public string EnabledLastChangedBy { get; }

    /// <summary>Gets the actor that last changed the skip state.</summary>
    public string SkipLastChangedBy { get; }
}
