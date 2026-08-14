using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using LabApi.Features.Wrappers;
using ReinforcementGate.Api;
using ReinforcementGate.Configuration;
using ReinforcementGate.Domain;
using ReinforcementGate.Interception;
using ReinforcementGate.Notifications;
using ReinforcementGate.State;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class WaveInterceptionServiceTests : IDisposable
{
    public WaveInterceptionServiceTests() => ReinforcementEvents.ClearSubscribers();

    [Theory]
    [InlineData(typeof(MtfWave), ReinforcementTarget.Ntf)]
    [InlineData(typeof(MiniMtfWave), ReinforcementTarget.NtfMini)]
    [InlineData(typeof(ChaosWave), ReinforcementTarget.Ci)]
    [InlineData(typeof(MiniChaosWave), ReinforcementTarget.CiMini)]
    public void Classifier_maps_only_the_four_supported_wrapper_types(
        Type waveType,
        ReinforcementTarget expected)
    {
        RespawnWave wave = CreateUninitializedWave(waveType);

        bool classified = WaveClassifier.TryClassify(wave, out ReinforcementTarget actual);

        Assert.True(classified);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Classifier_returns_unknown_when_no_wrapper_is_available()
    {
        bool classified = WaveClassifier.TryClassify(null, out ReinforcementTarget target);

        Assert.False(classified);
        Assert.Equal(default, target);
    }

    [Fact]
    public void Skip_trigger_blocks_once_and_sends_skip_triggered()
    {
        ReinforcementStateService state = new();
        state.ArmSkip(ReinforcementTarget.CiMini, "Admin");
        RecordingNotifications notifications = new();
        WaveInterceptionService service = new(state, notifications);

        Assert.True(service.ShouldBlock(ReinforcementTarget.CiMini));
        Assert.False(service.ShouldBlock(ReinforcementTarget.CiMini));
        NotificationRecord item = Assert.Single(
            notifications.Items,
            x => x.Kind == NotificationKind.SkipTriggered);
        Assert.Equal("Admin", item.Context.Admin);
        Assert.Equal(ReinforcementBlockReason.TargetSkip.ToString(), item.Context.Reason);
    }

    [Fact]
    public void Persistent_block_uses_disabled_wave_notification_and_preserves_skip()
    {
        ReinforcementStateService state = new();
        state.SetEnabled(ReinforcementTarget.Ntf, false, "Admin A");
        state.ArmSkip(ReinforcementTarget.Ntf, "Admin B");
        RecordingNotifications notifications = new();
        WaveInterceptionService service = new(state, notifications);

        Assert.True(service.ShouldBlock(ReinforcementTarget.Ntf));

        Assert.True(state.GetState(ReinforcementTarget.Ntf).IsSkipArmed);
        NotificationRecord item = Assert.Single(
            notifications.Items,
            x => x.Kind == NotificationKind.DisabledWaveBlocked);
        Assert.Equal("Admin A", item.Context.Admin);
        Assert.Equal(ReinforcementBlockReason.TargetDisabled.ToString(), item.Context.Reason);
    }

    [Fact]
    public void Allowed_wave_evaluates_once_without_event_or_notification()
    {
        CountingController controller = new(WaveDecision.Allowed(ReinforcementTarget.Ntf));
        RecordingNotifications notifications = new();
        WaveInterceptionService service = new(controller, notifications);
        int eventCalls = 0;
        ReinforcementEvents.WaveBlocked += (_, _) => eventCalls++;

        bool blocked = service.ShouldBlock(ReinforcementTarget.Ntf);

        Assert.False(blocked);
        Assert.Equal(1, controller.EvaluateCalls);
        Assert.Equal(0, eventCalls);
        Assert.Empty(notifications.Items);
    }

    [Fact]
    public void Blocked_wave_evaluates_once_and_publishes_post_decision_snapshot()
    {
        ReinforcementStateService state = new();
        state.ArmSkip(ReinforcementTarget.Ci, "OtherPlugin");
        CountingController controller = new(state);
        WaveInterceptionService service = new(controller, new RecordingNotifications());
        WaveBlockedEventArgs? observed = null;
        ReinforcementEvents.WaveBlocked += (_, args) => observed = args;

        bool blocked = service.ShouldBlock(ReinforcementTarget.Ci);

        Assert.True(blocked);
        Assert.Equal(1, controller.EvaluateCalls);
        Assert.NotNull(observed);
        Assert.Equal(ReinforcementTarget.Ci, observed!.Target);
        Assert.Equal(ReinforcementBlockReason.TargetSkip, observed.Reason);
        Assert.Equal("OtherPlugin", observed.Source);
        Assert.False(observed.StateSnapshot.Targets[ReinforcementTarget.Ci].IsSkipArmed);
    }

    [Theory]
    [InlineData(ReinforcementBlockReason.TargetSkip)]
    [InlineData(ReinforcementBlockReason.GlobalSkip)]
    public void Skip_reasons_use_skip_triggered_notification(ReinforcementBlockReason reason)
    {
        CountingController controller = new(WaveDecision.Blocked(
            ReinforcementTarget.NtfMini,
            reason,
            "Admin"));
        RecordingNotifications notifications = new();
        WaveInterceptionService service = new(controller, notifications);

        Assert.True(service.ShouldBlock(ReinforcementTarget.NtfMini));

        Assert.Equal(NotificationKind.SkipTriggered, Assert.Single(notifications.Items).Kind);
    }

    [Theory]
    [InlineData(ReinforcementBlockReason.TargetDisabled)]
    [InlineData(ReinforcementBlockReason.GlobalDisabled)]
    public void Persistent_reasons_use_disabled_wave_notification(ReinforcementBlockReason reason)
    {
        CountingController controller = new(WaveDecision.Blocked(
            ReinforcementTarget.CiMini,
            reason,
            "Admin"));
        RecordingNotifications notifications = new();
        WaveInterceptionService service = new(controller, notifications);

        Assert.True(service.ShouldBlock(ReinforcementTarget.CiMini));

        Assert.Equal(NotificationKind.DisabledWaveBlocked, Assert.Single(notifications.Items).Kind);
    }

    [Fact]
    public void Notification_exception_is_logged_and_cannot_flip_block_to_allow()
    {
        ReinforcementStateService state = new();
        state.SetEnabled(ReinforcementTarget.Ntf, false, "Admin");
        RecordingLogger logger = new();
        WaveInterceptionService service = new(state, new ThrowingNotifications(), logger);

        bool blocked = service.ShouldBlock(ReinforcementTarget.Ntf);

        Assert.True(blocked);
        LogRecord error = Assert.Single(logger.Errors);
        Assert.Contains("notification", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    [Fact]
    public void Event_subscriber_exception_is_logged_and_cannot_flip_block_to_allow()
    {
        ReinforcementStateService state = new();
        state.SetEnabled(ReinforcementTarget.Ci, false, "Admin");
        RecordingLogger logger = new();
        WaveInterceptionService service = new(state, new RecordingNotifications(), logger);
        ReinforcementEvents.WaveBlocked += (_, _) =>
            throw new InvalidOperationException("subscriber failed");

        bool blocked = service.ShouldBlock(ReinforcementTarget.Ci);

        Assert.True(blocked);
        LogRecord error = Assert.Single(logger.Errors);
        Assert.Contains("event", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    public void Dispose() => ReinforcementEvents.ClearSubscribers();

    private static RespawnWave CreateUninitializedWave(Type waveType)
    {
#pragma warning disable SYSLIB0050
        return (RespawnWave)FormatterServices.GetUninitializedObject(waveType);
#pragma warning restore SYSLIB0050
    }

    private sealed class RecordingNotifications : INotificationService
    {
        public List<NotificationRecord> Items { get; } = new();

        public void Notify(NotificationKind kind, NotificationContext context) =>
            Items.Add(new NotificationRecord(kind, context));

        public void UpdateConfig(NotificationsConfig config)
        {
        }
    }

    private sealed class ThrowingNotifications : INotificationService
    {
        public void Notify(NotificationKind kind, NotificationContext context) =>
            throw new InvalidOperationException("notification transport failed");

        public void UpdateConfig(NotificationsConfig config)
        {
        }
    }

    private sealed class RecordingLogger : IInterceptionLogger
    {
        public List<string> Warnings { get; } = new();

        public List<LogRecord> Errors { get; } = new();

        public void Warn(string message) => Warnings.Add(message);

        public void Error(string message, Exception exception) =>
            Errors.Add(new LogRecord(message, exception));
    }

    private sealed class CountingController : IReinforcementController
    {
        private readonly IReinforcementController? _inner;
        private readonly WaveDecision? _decision;
        private readonly ReinforcementStateSnapshot _snapshot = new ReinforcementStateService().GetSnapshot();

        public CountingController(WaveDecision decision) => _decision = decision;

        public CountingController(IReinforcementController inner) => _inner = inner;

        public int EvaluateCalls { get; private set; }

        public event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged
        {
            add { }
            remove { }
        }

        public event EventHandler? RoundStateReset
        {
            add { }
            remove { }
        }

        public ReinforcementStateSnapshot GetSnapshot() => _inner?.GetSnapshot() ?? _snapshot;

        public ReinforcementTargetState GetState(ReinforcementTarget target) =>
            _inner?.GetState(target) ?? _snapshot.Targets[target];

        public bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state)
        {
            state = GetState(target);
            return true;
        }

        public StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source) =>
            throw new NotSupportedException();

        public StateTransitionResult ArmSkip(ReinforcementTarget target, string source) =>
            throw new NotSupportedException();

        public StateTransitionResult ClearSkip(ReinforcementTarget target, string source) =>
            throw new NotSupportedException();

        public StateTransitionResult Reset(string source) => throw new NotSupportedException();

        public StateTransitionResult ResetForRound() => throw new NotSupportedException();

        public WaveDecision EvaluateWave(ReinforcementTarget target)
        {
            EvaluateCalls++;
            return _inner?.EvaluateWave(target) ?? _decision!;
        }
    }

    private sealed class NotificationRecord
    {
        public NotificationRecord(NotificationKind kind, NotificationContext context)
        {
            Kind = kind;
            Context = context;
        }

        public NotificationKind Kind { get; }

        public NotificationContext Context { get; }
    }

    private sealed class LogRecord
    {
        public LogRecord(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        public string Message { get; }

        public Exception Exception { get; }
    }
}
