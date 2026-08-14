namespace ReinforcementGate.Domain;

/// <summary>Identifies why a reinforcement wave was blocked.</summary>
public enum ReinforcementBlockReason
{
    /// <summary>All reinforcement is globally disabled.</summary>
    GlobalDisabled,

    /// <summary>The targeted reinforcement category is disabled.</summary>
    TargetDisabled,

    /// <summary>The targeted reinforcement category consumed a one-shot skip.</summary>
    TargetSkip,

    /// <summary>The global one-shot skip was consumed.</summary>
    GlobalSkip,
}
