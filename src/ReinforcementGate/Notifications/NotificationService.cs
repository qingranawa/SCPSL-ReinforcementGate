using System;
using ReinforcementGate.Configuration;

namespace ReinforcementGate.Notifications;

/// <summary>Coordinates independent Broadcast and CASSIE notification channels.</summary>
public sealed class NotificationService : INotificationService
{
    private readonly INotificationTransport _transport;
    private readonly INotificationLogger _logger;
    private volatile NotificationsConfig _config;

    /// <summary>Creates a notification service from a configuration snapshot.</summary>
    public NotificationService(
        NotificationsConfig config,
        INotificationTransport transport,
        INotificationLogger logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = NotificationConfigNormalizer.Normalize(config, ReportConfigFallback);
    }

    /// <inheritdoc />
    public void Notify(NotificationKind kind, NotificationContext context)
    {
        if (context is null)
        {
            SafeError(
                "notifications",
                "Notification context was null.",
                new ArgumentNullException(nameof(context)));
            return;
        }

        NotificationsConfig config = _config;
        NotificationNodeConfig node;
        string nodePath;
        try
        {
            (node, nodePath) = SelectNode(config, kind);
        }
        catch (Exception exception)
        {
            SafeError("notifications", "Unable to select the notification node.", exception);
            return;
        }

        if (node.Mode is NotificationMode.Broadcast or NotificationMode.Both)
            TryBroadcast(node, nodePath, context);

        if (node.Mode is NotificationMode.Cassie or NotificationMode.Both)
            TryCassie(node, nodePath, context);
    }

    /// <inheritdoc />
    public void UpdateConfig(NotificationsConfig config)
    {
        NotificationsConfig normalized = NotificationConfigNormalizer.Normalize(
            config,
            ReportConfigFallback);

        _config = normalized;
    }

    private void TryBroadcast(
        NotificationNodeConfig node,
        string nodePath,
        NotificationContext context)
    {
        string messagePath = $"{nodePath}.broadcast.message";
        try
        {
            TemplateRenderResult rendered = TemplateRenderer.Render(node.Broadcast.Message, context);
            ReportUnknownTokens(messagePath, rendered);
            if (string.IsNullOrEmpty(rendered.Text))
                return;

            _transport.SendBroadcast(
                rendered.Text,
                node.Broadcast.Duration,
                node.Broadcast.ClearPrevious);
        }
        catch (Exception exception)
        {
            SafeError($"{nodePath}.broadcast", "Broadcast notification failed.", exception);
        }
    }

    private void TryCassie(
        NotificationNodeConfig node,
        string nodePath,
        NotificationContext context)
    {
        try
        {
            TemplateRenderResult message = TemplateRenderer.Render(node.Cassie.Message, context);
            TemplateRenderResult subtitles = TemplateRenderer.Render(node.Cassie.Subtitles, context);
            ReportUnknownTokens($"{nodePath}.cassie.message", message);
            ReportUnknownTokens($"{nodePath}.cassie.subtitles", subtitles);
            if (string.IsNullOrEmpty(message.Text))
                return;

            _transport.SendCassie(
                message.Text,
                subtitles.Text,
                node.Cassie.PlayBackground,
                node.Cassie.Priority,
                node.Cassie.GlitchScale);
        }
        catch (Exception exception)
        {
            SafeError($"{nodePath}.cassie", "CASSIE notification failed.", exception);
        }
    }

    private void ReportUnknownTokens(string path, TemplateRenderResult result)
    {
        foreach (string token in result.UnknownTokens)
            SafeWarning(path, $"Unknown template token '{token}' was preserved.");
    }

    private void ReportConfigFallback(string path) =>
        SafeWarning(path, "Invalid notification configuration was replaced with its default node.");

    private void SafeWarning(string path, string message)
    {
        try
        {
            _logger.Warning(path, message);
        }
        catch
        {
            // Diagnostics must never affect reinforcement control.
        }
    }

    private void SafeError(string path, string message, Exception exception)
    {
        try
        {
            _logger.Error(path, message, exception);
        }
        catch
        {
            // Diagnostics must never affect reinforcement control.
        }
    }

    private static (NotificationNodeConfig Node, string Path) SelectNode(
        NotificationsConfig config,
        NotificationKind kind) => kind switch
    {
        NotificationKind.EnableApplied =>
            (config.EnableApplied, "notifications.enable_applied"),
        NotificationKind.DisableApplied =>
            (config.DisableApplied, "notifications.disable_applied"),
        NotificationKind.DisabledWaveBlocked =>
            (config.DisabledWaveBlocked, "notifications.disabled_wave_blocked"),
        NotificationKind.SkipArmed =>
            (config.SkipArmed, "notifications.skip_armed"),
        NotificationKind.SkipTriggered =>
            (config.SkipTriggered, "notifications.skip_triggered"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown notification kind."),
    };
}
