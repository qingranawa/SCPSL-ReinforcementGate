using System;
using ReinforcementGate.Domain;

namespace ReinforcementGate.Api;

/// <summary>Exposes reinforcement interception events to other plugins.</summary>
public static class ReinforcementEvents
{
    /// <summary>Occurs after a reinforcement wave has been blocked.</summary>
    public static event EventHandler<WaveBlockedEventArgs>? WaveBlocked;

    internal static void PublishWaveBlocked(
        WaveBlockedEventArgs args,
        Action<Exception>? onSubscriberError = null)
    {
        if (args is null)
            throw new ArgumentNullException(nameof(args));

        EventHandler<WaveBlockedEventArgs>? handlers = WaveBlocked;
        if (handlers is null)
            return;

        foreach (EventHandler<WaveBlockedEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(null, args);
            }
            catch (Exception exception)
            {
                // External observers cannot undo an already committed wave decision.
                try
                {
                    onSubscriberError?.Invoke(exception);
                }
                catch
                {
                    // Error reporting cannot invalidate an already committed wave decision.
                }
            }
        }
    }

    internal static void ClearSubscribers() => WaveBlocked = null;
}
