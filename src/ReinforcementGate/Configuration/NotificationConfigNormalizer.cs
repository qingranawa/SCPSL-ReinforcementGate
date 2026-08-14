using System;

namespace ReinforcementGate.Configuration;

/// <summary>Creates detached, semantically valid notification configuration trees.</summary>
public static class NotificationConfigNormalizer
{
    /// <summary>Normalizes a complete notification configuration tree.</summary>
    public static NotificationsConfig Normalize(NotificationsConfig? config)
    {
        NotificationsConfig source = config ?? new NotificationsConfig();

        return new NotificationsConfig
        {
            EnableApplied = NormalizeNode(
                "notifications.enable_applied",
                source.EnableApplied,
                NotificationNodeConfig.CreateEnableAppliedDefault()),
            DisableApplied = NormalizeNode(
                "notifications.disable_applied",
                source.DisableApplied,
                NotificationNodeConfig.CreateDisableAppliedDefault()),
            DisabledWaveBlocked = NormalizeNode(
                "notifications.disabled_wave_blocked",
                source.DisabledWaveBlocked,
                NotificationNodeConfig.CreateDisabledWaveBlockedDefault()),
            SkipArmed = NormalizeNode(
                "notifications.skip_armed",
                source.SkipArmed,
                NotificationNodeConfig.CreateSkipArmedDefault()),
            SkipTriggered = NormalizeNode(
                "notifications.skip_triggered",
                source.SkipTriggered,
                NotificationNodeConfig.CreateSkipTriggeredDefault()),
        };
    }

    /// <summary>
    /// Returns a detached copy of a valid node, or a detached default when the node is invalid.
    /// </summary>
    public static NotificationNodeConfig NormalizeNode(
        string path,
        NotificationNodeConfig? node,
        NotificationNodeConfig defaultNode)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (defaultNode is null)
            throw new ArgumentNullException(nameof(defaultNode));

        if (!IsValid(node))
            return Clone(defaultNode);

        return Clone(node!);
    }

    private static bool IsValid(NotificationNodeConfig? node) =>
        node is not null &&
        node.Broadcast is not null &&
        node.Cassie is not null &&
        node.Broadcast.Duration != 0 &&
        !float.IsNaN(node.Cassie.Priority) &&
        !float.IsInfinity(node.Cassie.Priority) &&
        node.Cassie.GlitchScale >= 0f &&
        node.Cassie.GlitchScale <= 1f;

    private static NotificationNodeConfig Clone(NotificationNodeConfig source) =>
        new()
        {
            Mode = source.Mode,
            Broadcast = new BroadcastConfig
            {
                Message = source.Broadcast.Message,
                Duration = source.Broadcast.Duration,
                ClearPrevious = source.Broadcast.ClearPrevious,
            },
            Cassie = new CassieConfig
            {
                Message = source.Cassie.Message,
                Subtitles = source.Cassie.Subtitles,
                PlayBackground = source.Cassie.PlayBackground,
                Priority = source.Cassie.Priority,
                GlitchScale = source.Cassie.GlitchScale,
            },
        };
}
