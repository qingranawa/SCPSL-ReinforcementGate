using System;
using LabLogger = LabApi.Features.Console.Logger;

namespace ReinforcementGate.Notifications;

/// <summary>Writes notification diagnostics to the LabAPI server console.</summary>
public sealed class LabApiNotificationLogger : INotificationLogger
{
    /// <inheritdoc />
    public void Warning(string configurationPath, string message) =>
        LabLogger.Warn($"[ReinforcementGate] {configurationPath}: {message}");

    /// <inheritdoc />
    public void Error(string configurationPath, string message, Exception exception) =>
        LabLogger.Error($"[ReinforcementGate] {configurationPath}: {message} {exception}");
}
