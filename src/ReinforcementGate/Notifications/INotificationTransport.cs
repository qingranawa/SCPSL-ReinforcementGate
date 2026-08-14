namespace ReinforcementGate.Notifications;

/// <summary>Delivers rendered reinforcement notifications to game channels.</summary>
public interface INotificationTransport
{
    /// <summary>Sends a server broadcast.</summary>
    void SendBroadcast(string message, ushort duration, bool clearPrevious);

    /// <summary>Sends a CASSIE announcement.</summary>
    void SendCassie(
        string message,
        string subtitles,
        bool playBackground,
        float priority,
        float glitchScale);
}
