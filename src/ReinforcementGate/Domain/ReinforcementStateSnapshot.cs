using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReinforcementGate.Domain;

/// <summary>Represents an immutable snapshot of all reinforcement gate state.</summary>
public sealed class ReinforcementStateSnapshot
{
    /// <summary>Initializes a reinforcement state snapshot.</summary>
    /// <param name="isGlobalDisabled">Whether reinforcement is globally disabled.</param>
    /// <param name="isGlobalSkipArmed">Whether a global one-shot skip is armed.</param>
    /// <param name="globalDisabledLastChangedBy">The actor that last changed global disable state.</param>
    /// <param name="globalSkipLastChangedBy">The actor that last changed global skip state.</param>
    /// <param name="targets">The state for each reinforcement target.</param>
    public ReinforcementStateSnapshot(
        bool isGlobalDisabled,
        bool isGlobalSkipArmed,
        string globalDisabledLastChangedBy,
        string globalSkipLastChangedBy,
        IDictionary<ReinforcementTarget, ReinforcementTargetState> targets)
    {
        IsGlobalDisabled = isGlobalDisabled;
        IsGlobalSkipArmed = isGlobalSkipArmed;
        GlobalDisabledLastChangedBy = globalDisabledLastChangedBy;
        GlobalSkipLastChangedBy = globalSkipLastChangedBy;
        Targets = new ReadOnlyDictionary<ReinforcementTarget, ReinforcementTargetState>(
            new Dictionary<ReinforcementTarget, ReinforcementTargetState>(targets));
    }

    /// <summary>Gets whether reinforcement is globally disabled.</summary>
    public bool IsGlobalDisabled { get; }

    /// <summary>Gets whether a global one-shot skip is armed.</summary>
    public bool IsGlobalSkipArmed { get; }

    /// <summary>Gets the actor that last changed global disable state.</summary>
    public string GlobalDisabledLastChangedBy { get; }

    /// <summary>Gets the actor that last changed global skip state.</summary>
    public string GlobalSkipLastChangedBy { get; }

    /// <summary>Gets read-only state keyed by reinforcement target.</summary>
    public ReadOnlyDictionary<ReinforcementTarget, ReinforcementTargetState> Targets { get; }
}
