namespace ReinforcementGate.Commands;

/// <summary>Identifies a Remote Admin reinforcement command action.</summary>
public enum ReinforcementCommandAction
{
    /// <summary>Displays current state.</summary>
    Status,

    /// <summary>Enables a target.</summary>
    Enable,

    /// <summary>Disables a target.</summary>
    Disable,

    /// <summary>Arms a one-shot skip.</summary>
    Skip,

    /// <summary>Restores default state.</summary>
    Reset,
}
