using ReinforcementGate.Configuration;

namespace ReinforcementGate.Notifications;

/// <summary>Renders and safely delivers configured reinforcement notifications.</summary>
public interface INotificationService
{
    /// <summary>Attempts to deliver one notification without affecting control flow.</summary>
    void Notify(NotificationKind kind, NotificationContext context);

    /// <summary>Atomically replaces the active normalized notification configuration.</summary>
    void UpdateConfig(NotificationsConfig config);
}
