using ReinforcementGate.Notifications;

namespace ReinforcementGate.Configuration;

/// <summary>Configures one reinforcement notification scenario.</summary>
public sealed class NotificationNodeConfig
{
    /// <summary>Gets or sets the enabled delivery channels.</summary>
    public NotificationMode Mode { get; set; }

    /// <summary>Gets or sets broadcast delivery options.</summary>
    public BroadcastConfig Broadcast { get; set; } = new();

    /// <summary>Gets or sets CASSIE delivery options.</summary>
    public CassieConfig Cassie { get; set; } = new();

    /// <summary>Creates the default notification for an enable transition.</summary>
    public static NotificationNodeConfig CreateEnableAppliedDefault() =>
        Create(NotificationMode.None, string.Empty, string.Empty, string.Empty);

    /// <summary>Creates the default notification for a disable transition.</summary>
    public static NotificationNodeConfig CreateDisableAppliedDefault() =>
        Create(NotificationMode.None, string.Empty, string.Empty, string.Empty);

    /// <summary>Creates the default notification for a persistently blocked wave.</summary>
    public static NotificationNodeConfig CreateDisabledWaveBlockedDefault() =>
        Create(NotificationMode.None, string.Empty, string.Empty, string.Empty);

    /// <summary>Creates the default notification for arming a one-shot skip.</summary>
    public static NotificationNodeConfig CreateSkipArmedDefault() =>
        Create(NotificationMode.None, string.Empty, string.Empty, string.Empty);

    /// <summary>Creates the default notification for a triggered one-shot skip.</summary>
    public static NotificationNodeConfig CreateSkipTriggeredDefault() =>
        Create(NotificationMode.None, string.Empty, string.Empty, string.Empty);

    private static NotificationNodeConfig Create(
        NotificationMode mode,
        string broadcastMessage,
        string cassieMessage,
        string subtitles) =>
        new()
        {
            Mode = mode,
            Broadcast = new BroadcastConfig
            {
                Message = broadcastMessage,
                Duration = 8,
                ClearPrevious = false,
            },
            Cassie = new CassieConfig
            {
                Message = cassieMessage,
                Subtitles = subtitles,
                PlayBackground = true,
                Priority = 0f,
                GlitchScale = 0f,
            },
        };
}
