using System;
using LabApi.Events.CustomHandlers;
using LabApi.Loader.Features.Plugins;
using ReinforcementGate.Api;
using ReinforcementGate.Configuration;
using ReinforcementGate.Control;
using ReinforcementGate.Interception;
using ReinforcementGate.Notifications;
using ReinforcementGate.State;
using LabLogger = LabApi.Features.Console.Logger;

namespace ReinforcementGate;

/// <summary>Composes and owns the ReinforcementGate LabAPI plugin lifecycle.</summary>
public sealed class ReinforcementGatePlugin : Plugin<ReinforcementGateConfig>
{
    private readonly Action<string> _invalidConfigWarning;
    private INotificationLogger? _notificationLogger;
    private IInterceptionLogger? _interceptionLogger;
    private INotificationTransport? _notificationTransport;
    private NotificationService? _notificationService;
    private ReinforcementStateService? _stateService;
    private IReinforcementController? _controller;
    private WaveInterceptionService? _interceptionService;
    private ReinforcementEventsHandler? _eventsHandler;

    /// <summary>Initializes the plugin with LabAPI configuration diagnostics.</summary>
    public ReinforcementGatePlugin()
        : this(SafeWarnInvalidConfigPath)
    {
    }

    internal ReinforcementGatePlugin(Action<string> invalidConfigWarning)
    {
        _invalidConfigWarning = invalidConfigWarning ??
            throw new ArgumentNullException(nameof(invalidConfigWarning));
    }

    /// <inheritdoc />
    public override string Name => "ReinforcementGate";

    /// <inheritdoc />
    public override string Description =>
        "Controls future NTF and Chaos reinforcement waves by type, with global gates, one-shot skips, configurable notifications, and public APIs.";

    /// <inheritdoc />
    public override string Author => "ReinforcementGate Contributors";

    /// <inheritdoc />
    public override Version Version => new(1, 0, 0);

    /// <inheritdoc />
    public override Version RequiredApiVersion => new(1, 1, 7);

    /// <inheritdoc />
    public override bool IsTransparent => false;

    /// <inheritdoc />
    public override string ConfigFileName { get; set; } = "reinforcement-gate.yml";

    /// <inheritdoc />
    public override void LoadConfigs()
    {
        base.LoadConfigs();
        NormalizeConfig();
        _notificationService?.UpdateConfig(Config.Notifications);
    }

    /// <inheritdoc />
    public override void Enable()
    {
        if (_eventsHandler is not null)
            return;

        NormalizeConfig();

        _notificationLogger = new LabApiNotificationLogger();
        _interceptionLogger = new LabApiInterceptionLogger();
        _notificationTransport = new LabApiNotificationTransport();
        _notificationService = new NotificationService(
            Config.Notifications,
            _notificationTransport,
            _notificationLogger);
        _stateService = new ReinforcementStateService();
        _controller = new NotifyingReinforcementController(
            _stateService,
            _notificationService);
        _interceptionService = new WaveInterceptionService(
            _controller,
            _notificationService,
            _interceptionLogger);
        _eventsHandler = new ReinforcementEventsHandler(
            _controller,
            _interceptionService,
            _interceptionLogger);

        try
        {
            ReinforcementStatesApi.Register(_controller);
            ReinforcementControlApi.Register(_controller);
            CustomHandlersManager.RegisterEventsHandler(_eventsHandler);
        }
        catch
        {
            Disable();
            throw;
        }
    }

    /// <inheritdoc />
    public override void Disable()
    {
        if (_eventsHandler is not null)
            CustomHandlersManager.UnregisterEventsHandler(_eventsHandler);

        if (_controller is not null)
        {
            ReinforcementControlApi.Unregister(_controller);
            ReinforcementStatesApi.Unregister(_controller);
        }

        ReinforcementEvents.ClearSubscribers();

        _eventsHandler = null;
        _interceptionService = null;
        _controller = null;
        _stateService = null;
        _notificationService = null;
        _notificationTransport = null;
        _interceptionLogger = null;
        _notificationLogger = null;
    }

    private void NormalizeConfig()
    {
        Config ??= new ReinforcementGateConfig();
        Config.Notifications = NotificationConfigNormalizer.Normalize(
            Config.Notifications,
            _invalidConfigWarning);
    }

    private static void SafeWarnInvalidConfigPath(string configurationPath)
    {
        try
        {
            LabLogger.Warn(
                $"[ReinforcementGate] Invalid configuration node '{configurationPath}'; restored its default value.");
        }
        catch
        {
            // Configuration fallback must survive an unavailable logger.
        }
    }

    private sealed class LabApiInterceptionLogger : IInterceptionLogger
    {
        public void Warn(string message) =>
            LabLogger.Warn($"[ReinforcementGate] {message}");

        public void Error(string message, Exception exception) =>
            LabLogger.Error($"[ReinforcementGate] {message} {exception}");
    }
}
