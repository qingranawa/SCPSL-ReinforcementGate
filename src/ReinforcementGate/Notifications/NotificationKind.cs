namespace ReinforcementGate.Notifications;

/// <summary>Identifies a configurable reinforcement notification.</summary>
public enum NotificationKind
{
    /// <summary>A target was enabled.</summary>
    EnableApplied,

    /// <summary>A target was disabled.</summary>
    DisableApplied,

    /// <summary>A wave was blocked by a persistent disable.</summary>
    DisabledWaveBlocked,

    /// <summary>A one-shot skip was armed.</summary>
    SkipArmed,

    /// <summary>An armed one-shot skip blocked a wave.</summary>
    SkipTriggered,
}
