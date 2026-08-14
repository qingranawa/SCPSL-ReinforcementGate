namespace ReinforcementGate.Domain;

/// <summary>Identifies a reinforcement state transition.</summary>
public enum ReinforcementStateAction
{
    /// <summary>Enables reinforcement.</summary>
    Enable,

    /// <summary>Disables reinforcement.</summary>
    Disable,

    /// <summary>Arms a one-shot skip.</summary>
    ArmSkip,

    /// <summary>Clears an armed one-shot skip.</summary>
    ClearSkip,

    /// <summary>Consumes an armed one-shot skip.</summary>
    ConsumeSkip,

    /// <summary>Resets state on demand.</summary>
    Reset,

    /// <summary>Resets state at the start of a round.</summary>
    RoundReset,
}
