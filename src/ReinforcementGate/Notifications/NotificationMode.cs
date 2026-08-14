namespace ReinforcementGate.Notifications;

/// <summary>Selects the delivery channels used by a notification.</summary>
public enum NotificationMode
{
    /// <summary>Disables the notification.</summary>
    None,

    /// <summary>Sends only a broadcast.</summary>
    Broadcast,

    /// <summary>Sends only a CASSIE announcement.</summary>
    Cassie,

    /// <summary>Sends both a broadcast and a CASSIE announcement.</summary>
    Both,
}
