using System;

namespace ReinforcementGate.Notifications;

/// <summary>Records notification diagnostics with their configuration paths.</summary>
public interface INotificationLogger
{
    /// <summary>Records a non-fatal notification warning.</summary>
    void Warning(string configurationPath, string message);

    /// <summary>Records an isolated notification failure.</summary>
    void Error(string configurationPath, string message, Exception exception);
}
