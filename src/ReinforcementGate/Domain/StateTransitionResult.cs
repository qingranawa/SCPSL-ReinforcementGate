namespace ReinforcementGate.Domain;

/// <summary>Describes one attempted reinforcement state transition.</summary>
public sealed class StateTransitionResult
{
    /// <summary>Initializes a state transition result.</summary>
    public StateTransitionResult(
        bool changed,
        ReinforcementStateSnapshot before,
        ReinforcementStateSnapshot after,
        ReinforcementStateAction action,
        ReinforcementTarget target,
        string source)
    {
        Changed = changed;
        Before = before;
        After = after;
        Action = action;
        Target = target;
        Source = source;
    }

    /// <summary>Gets whether the observable snapshot changed.</summary>
    public bool Changed { get; }

    /// <summary>Gets the snapshot before the transition.</summary>
    public ReinforcementStateSnapshot Before { get; }

    /// <summary>Gets the snapshot after the transition.</summary>
    public ReinforcementStateSnapshot After { get; }

    /// <summary>Gets the attempted action.</summary>
    public ReinforcementStateAction Action { get; }

    /// <summary>Gets the affected target.</summary>
    public ReinforcementTarget Target { get; }

    /// <summary>Gets the audit source.</summary>
    public string Source { get; }
}
