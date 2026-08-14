using System;
using ReinforcementGate.Notifications;

namespace ReinforcementGate.Configuration;

/// <summary>Creates detached, semantically valid notification configuration trees.</summary>
public static class NotificationConfigNormalizer
{
    /// <summary>Normalizes a complete notification configuration tree.</summary>
    public static NotificationsConfig Normalize(NotificationsConfig? config) =>
        Normalize(config, null);

    /// <summary>
    /// Normalizes a complete notification configuration tree and reports paths that use defaults.
    /// Diagnostic callback failures are ignored so reporting cannot break configuration loading.
    /// </summary>
    public static NotificationsConfig Normalize(
        NotificationsConfig? config,
        Action<string>? onDefaultFallback)
    {
        NotificationsConfig source = config ?? new NotificationsConfig();

        return new NotificationsConfig
        {
            EnableApplied = NormalizeNode(
                "notifications.enable_applied",
                source.EnableApplied,
                NotificationNodeConfig.CreateEnableAppliedDefault(),
                onDefaultFallback),
            DisableApplied = NormalizeNode(
                "notifications.disable_applied",
                source.DisableApplied,
                NotificationNodeConfig.CreateDisableAppliedDefault(),
                onDefaultFallback),
            DisabledWaveBlocked = NormalizeNode(
                "notifications.disabled_wave_blocked",
                source.DisabledWaveBlocked,
                NotificationNodeConfig.CreateDisabledWaveBlockedDefault(),
                onDefaultFallback),
            SkipArmed = NormalizeNode(
                "notifications.skip_armed",
                source.SkipArmed,
                NotificationNodeConfig.CreateSkipArmedDefault(),
                onDefaultFallback),
            SkipTriggered = NormalizeNode(
                "notifications.skip_triggered",
                source.SkipTriggered,
                NotificationNodeConfig.CreateSkipTriggeredDefault(),
                onDefaultFallback),
        };
    }

    /// <summary>
    /// Returns a detached copy of a valid node, or a detached default when the node is invalid.
    /// </summary>
    public static NotificationNodeConfig NormalizeNode(
        string path,
        NotificationNodeConfig? node,
        NotificationNodeConfig defaultNode) =>
        NormalizeNode(path, node, defaultNode, null);

    /// <summary>
    /// Returns a detached normalized node and reports its path when the default is required.
    /// </summary>
    public static NotificationNodeConfig NormalizeNode(
        string path,
        NotificationNodeConfig? node,
        NotificationNodeConfig defaultNode,
        Action<string>? onDefaultFallback)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (defaultNode is null)
            throw new ArgumentNullException(nameof(defaultNode));

        if (!IsValid(node))
        {
            TryReportFallback(path, onDefaultFallback);
            return Clone(defaultNode);
        }

        return Clone(node!);
    }

    private static bool IsValid(NotificationNodeConfig? node) =>
        node is not null &&
        Enum.IsDefined(typeof(NotificationMode), node.Mode) &&
        node.Broadcast is not null &&
        node.Cassie is not null &&
        node.Broadcast.Message is not null &&
        node.Cassie.Message is not null &&
        node.Cassie.Subtitles is not null &&
        node.Broadcast.Duration != 0 &&
        !float.IsNaN(node.Cassie.Priority) &&
        !float.IsInfinity(node.Cassie.Priority) &&
        node.Cassie.GlitchScale >= 0f &&
        node.Cassie.GlitchScale <= 1f;

    private static void TryReportFallback(string path, Action<string>? onDefaultFallback)
    {
        try
        {
            onDefaultFallback?.Invoke(path);
        }
        catch
        {
            // Configuration fallback must remain available even when diagnostics fail.
        }
    }

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
