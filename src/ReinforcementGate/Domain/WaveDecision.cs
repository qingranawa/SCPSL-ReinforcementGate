namespace ReinforcementGate.Domain;

/// <summary>Describes whether a reinforcement wave may proceed.</summary>
public sealed class WaveDecision
{
    private WaveDecision(
        bool isBlocked,
        ReinforcementBlockReason? reason,
        ReinforcementTarget target,
        string source,
        StateTransitionResult? skipConsumption)
    {
        IsBlocked = isBlocked;
        Reason = reason;
        Target = target;
        Source = source;
        SkipConsumption = skipConsumption;
    }

    /// <summary>Gets whether the wave is blocked.</summary>
    public bool IsBlocked { get; }

    /// <summary>Gets the block reason, or <see langword="null"/> when allowed.</summary>
    public ReinforcementBlockReason? Reason { get; }

    /// <summary>Gets the evaluated concrete target.</summary>
    public ReinforcementTarget Target { get; }

    /// <summary>Gets the audit source associated with the decision.</summary>
    public string Source { get; }

    /// <summary>Gets the transition that consumed a one-shot skip, if any.</summary>
    public StateTransitionResult? SkipConsumption { get; }

    /// <summary>Creates an allowed decision.</summary>
    public static WaveDecision Allowed(ReinforcementTarget target) =>
        new(false, null, target, string.Empty, null);

    /// <summary>Creates a blocked decision.</summary>
    public static WaveDecision Blocked(
        ReinforcementTarget target,
        ReinforcementBlockReason reason,
        string source,
        StateTransitionResult? skipConsumption = null) =>
        new(true, reason, target, source, skipConsumption);
}
