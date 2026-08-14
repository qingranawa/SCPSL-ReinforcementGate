using ReinforcementGate.Domain;

namespace ReinforcementGate.Notifications;

/// <summary>Supplies values for a notification template.</summary>
public sealed class NotificationContext
{
    /// <summary>Initializes a notification context.</summary>
    public NotificationContext(
        ReinforcementTarget target,
        string targetName,
        string admin,
        string action,
        string reason)
    {
        Target = target;
        TargetName = targetName;
        Admin = admin;
        Action = action;
        Reason = reason;
    }

    /// <summary>Gets the strongly typed reinforcement target.</summary>
    public ReinforcementTarget Target { get; }

    /// <summary>Gets the display name for the target.</summary>
    public string TargetName { get; }

    /// <summary>Gets the administrator or API source.</summary>
    public string Admin { get; }

    /// <summary>Gets the action name.</summary>
    public string Action { get; }

    /// <summary>Gets the reason name.</summary>
    public string Reason { get; }
}
