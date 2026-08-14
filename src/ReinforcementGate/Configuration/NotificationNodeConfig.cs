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
        Create(
            NotificationMode.Broadcast,
            "<color=green>{target_name} 已恢复刷新</color>",
            "REINFORCEMENT ENABLED",
            "{target_name} 已恢复刷新");

    /// <summary>Creates the default notification for a disable transition.</summary>
    public static NotificationNodeConfig CreateDisableAppliedDefault() =>
        Create(
            NotificationMode.Both,
            "<color=red>{target_name} 已停止刷新</color>",
            "REINFORCEMENT SUSPENDED",
            "{target_name} 已停止刷新");

    /// <summary>Creates the default notification for a persistently blocked wave.</summary>
    public static NotificationNodeConfig CreateDisabledWaveBlockedDefault() =>
        Create(NotificationMode.None, string.Empty, string.Empty, string.Empty);

    /// <summary>Creates the default notification for arming a one-shot skip.</summary>
    public static NotificationNodeConfig CreateSkipArmedDefault() =>
        Create(
            NotificationMode.Broadcast,
            "下一次 {target_name} 支援将被跳过",
            string.Empty,
            string.Empty);

    /// <summary>Creates the default notification for a triggered one-shot skip.</summary>
    public static NotificationNodeConfig CreateSkipTriggeredDefault() =>
        Create(
            NotificationMode.Both,
            "{target_name} 支援已被跳过",
            "REINFORCEMENT WAVE CANCELLED",
            "{target_name} 支援已被跳过");

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
