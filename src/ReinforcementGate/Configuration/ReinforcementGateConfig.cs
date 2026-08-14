namespace ReinforcementGate.Configuration;

/// <summary>Contains all persistent ReinforcementGate settings.</summary>
public sealed class ReinforcementGateConfig
{
    /// <summary>Gets or sets notification and display settings.</summary>
    public NotificationsConfig Notifications { get; set; } = new();
}
