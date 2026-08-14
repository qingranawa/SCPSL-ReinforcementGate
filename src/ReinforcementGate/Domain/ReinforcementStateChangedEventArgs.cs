using System;

namespace ReinforcementGate.Domain;

/// <summary>Provides an immutable reinforcement state transition.</summary>
public sealed class ReinforcementStateChangedEventArgs : EventArgs
{
    /// <summary>Initializes event data for a completed state transition.</summary>
    public ReinforcementStateChangedEventArgs(StateTransitionResult transition)
    {
        Transition = transition ?? throw new ArgumentNullException(nameof(transition));
    }

    /// <summary>Gets the transition that caused the event.</summary>
    public StateTransitionResult Transition { get; }
}
