using LabApi.Features.Wrappers;

namespace ReinforcementGate.Notifications;

/// <summary>Delivers reinforcement notifications through LabAPI 1.1.7.</summary>
public sealed class LabApiNotificationTransport : INotificationTransport
{
    /// <inheritdoc />
    public void SendBroadcast(string message, ushort duration, bool clearPrevious) =>
        Server.SendBroadcast(message, duration, shouldClearPrevious: clearPrevious);

    /// <inheritdoc />
    public void SendCassie(
        string message,
        string subtitles,
        bool playBackground,
        float priority,
        float glitchScale) =>
        Announcer.Message(message, subtitles, playBackground, priority, glitchScale);
}
