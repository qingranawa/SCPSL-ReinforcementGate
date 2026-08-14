namespace ReinforcementGate.Configuration;

/// <summary>Contains the configurable reinforcement notification scenarios.</summary>
public sealed class NotificationsConfig
{
    /// <summary>Gets or sets the enable-transition notification.</summary>
    public NotificationNodeConfig EnableApplied { get; set; } =
        NotificationNodeConfig.CreateEnableAppliedDefault();

    /// <summary>Gets or sets the disable-transition notification.</summary>
    public NotificationNodeConfig DisableApplied { get; set; } =
        NotificationNodeConfig.CreateDisableAppliedDefault();

    /// <summary>Gets or sets the persistently blocked-wave notification.</summary>
    public NotificationNodeConfig DisabledWaveBlocked { get; set; } =
        NotificationNodeConfig.CreateDisabledWaveBlockedDefault();

    /// <summary>Gets or sets the skip-armed notification.</summary>
    public NotificationNodeConfig SkipArmed { get; set; } =
        NotificationNodeConfig.CreateSkipArmedDefault();

    /// <summary>Gets or sets the skip-triggered notification.</summary>
    public NotificationNodeConfig SkipTriggered { get; set; } =
        NotificationNodeConfig.CreateSkipTriggeredDefault();
}
