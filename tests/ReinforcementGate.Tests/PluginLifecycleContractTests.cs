using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using CommandSystem;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using ReinforcementGate.Api;
using ReinforcementGate.Commands;
using ReinforcementGate.Configuration;
using ReinforcementGate.Domain;
using ReinforcementGate.Interception;
using ReinforcementGate.Notifications;
using ReinforcementGate.State;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class PluginLifecycleContractTests
{
    [Fact]
    public void Plugin_metadata_targets_the_approved_api()
    {
        ReinforcementGatePlugin plugin = new();

        Assert.Equal("ReinforcementGate", plugin.Name);
        Assert.Equal(new Version(1, 0, 0), plugin.Version);
        Assert.Equal(new Version(1, 1, 7), plugin.RequiredApiVersion);
        Assert.Equal("ReinforcementGate Contributors", plugin.Author);
        Assert.False(plugin.IsTransparent);
        Assert.Equal("reinforcement-gate.yml", plugin.ConfigFileName);
    }

    [Fact]
    public void Command_is_registered_only_for_remote_admin()
    {
        CustomAttributeData attribute = CustomAttributeData
            .GetCustomAttributes(typeof(ReinforcementCommand))
            .Single(x => x.AttributeType == typeof(CommandHandlerAttribute));

        Assert.Single(attribute.ConstructorArguments);
        Assert.Equal(typeof(RemoteAdminCommandHandler), attribute.ConstructorArguments[0].Value);
    }

    [Fact]
    public void Event_handler_overrides_round_start_and_wave_respawning()
    {
        Assert.True(typeof(CustomEventsHandler).IsAssignableFrom(typeof(ReinforcementEventsHandler)));

        MethodInfo roundStarted = typeof(ReinforcementEventsHandler)
            .GetMethod(nameof(ReinforcementEventsHandler.OnServerRoundStarted))!;
        MethodInfo waveRespawning = typeof(ReinforcementEventsHandler)
            .GetMethod(nameof(ReinforcementEventsHandler.OnServerWaveRespawning))!;

        Assert.Equal(typeof(ReinforcementEventsHandler), roundStarted.DeclaringType);
        Assert.Equal(typeof(ReinforcementEventsHandler), waveRespawning.DeclaringType);
        Assert.NotEqual(roundStarted, roundStarted.GetBaseDefinition());
        Assert.NotEqual(waveRespawning, waveRespawning.GetBaseDefinition());
    }

    [Fact]
    public void Enable_disable_cycle_registers_and_releases_public_apis()
    {
        ReinforcementGatePlugin plugin = new();

        try
        {
            plugin.Enable();
            Assert.True(ReinforcementStatesApi.IsAvailable);

            plugin.Disable();
            Assert.False(ReinforcementStatesApi.IsAvailable);

            plugin.Enable();
            Assert.True(ReinforcementStatesApi.IsAvailable);
        }
        finally
        {
            plugin.Disable();
        }

        Assert.False(ReinforcementStatesApi.IsAvailable);
    }

    [Fact]
    public void Config_normalization_reports_the_full_invalid_path_and_isolates_logger_failure()
    {
        List<string> warnings = new();
        ReinforcementGatePlugin plugin = new(warnings.Add);
        plugin.Config.Notifications.SkipTriggered.Cassie.GlitchScale = 2f;

        try
        {
            plugin.Enable();
            Assert.Equal(new[] { "notifications.skip_triggered" }, warnings);
            Assert.Equal(0f, plugin.Config.Notifications.SkipTriggered.Cassie.GlitchScale);
        }
        finally
        {
            plugin.Disable();
        }

        ReinforcementGatePlugin failingLoggerPlugin = new(_ =>
            throw new InvalidOperationException("logger unavailable"));
        failingLoggerPlugin.Config.Notifications.DisableApplied.Broadcast.Duration = 0;

        Exception? exception = null;
        try
        {
            exception = Record.Exception(failingLoggerPlugin.Enable);
        }
        finally
        {
            failingLoggerPlugin.Disable();
        }

        Assert.Null(exception);
        Assert.Equal(
            (ushort)8,
            failingLoggerPlugin.Config.Notifications.DisableApplied.Broadcast.Duration);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Null_wave_wrapper_preserves_existing_decision_and_warns_only_once(
        bool initialIsAllowed)
    {
        CountingController controller = new();
        RecordingInterceptionLogger logger = new();
        WaveInterceptionService interception = new(
            controller,
            new SilentNotifications(),
            logger);
        ReinforcementEventsHandler handler = new(controller, interception, logger);
#pragma warning disable SYSLIB0050
        WaveRespawningEventArgs args = (WaveRespawningEventArgs)
            FormatterServices.GetUninitializedObject(typeof(WaveRespawningEventArgs));
#pragma warning restore SYSLIB0050
        args.IsAllowed = initialIsAllowed;

        handler.OnServerWaveRespawning(args);
        handler.OnServerWaveRespawning(args);

        Assert.Equal(initialIsAllowed, args.IsAllowed);
        Assert.Equal(0, controller.EvaluateCalls);
        string warning = Assert.Single(logger.Warnings);
        Assert.Contains("wave", warning, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SilentNotifications : INotificationService
    {
        public void Notify(NotificationKind kind, NotificationContext context)
        {
        }

        public void UpdateConfig(NotificationsConfig config)
        {
        }
    }

    private sealed class RecordingInterceptionLogger : IInterceptionLogger
    {
        public List<string> Warnings { get; } = new();

        public void Warn(string message) => Warnings.Add(message);

        public void Error(string message, Exception exception)
        {
        }
    }

    private sealed class CountingController : IReinforcementController
    {
        private readonly ReinforcementStateService _inner = new();

        public int EvaluateCalls { get; private set; }

        public event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged
        {
            add => _inner.StateChanged += value;
            remove => _inner.StateChanged -= value;
        }

        public event EventHandler? RoundStateReset
        {
            add => _inner.RoundStateReset += value;
            remove => _inner.RoundStateReset -= value;
        }

        public ReinforcementStateSnapshot GetSnapshot() => _inner.GetSnapshot();

        public ReinforcementTargetState GetState(ReinforcementTarget target) =>
            _inner.GetState(target);

        public bool TryGetState(
            ReinforcementTarget target,
            out ReinforcementTargetState? state) =>
            _inner.TryGetState(target, out state);

        public StateTransitionResult SetEnabled(
            ReinforcementTarget target,
            bool enabled,
            string source) =>
            _inner.SetEnabled(target, enabled, source);

        public StateTransitionResult ArmSkip(ReinforcementTarget target, string source) =>
            _inner.ArmSkip(target, source);

        public StateTransitionResult ClearSkip(ReinforcementTarget target, string source) =>
            _inner.ClearSkip(target, source);

        public StateTransitionResult Reset(string source) => _inner.Reset(source);

        public StateTransitionResult ResetForRound() => _inner.ResetForRound();

        public WaveDecision EvaluateWave(ReinforcementTarget target)
        {
            EvaluateCalls++;
            return _inner.EvaluateWave(target);
        }
    }
}
