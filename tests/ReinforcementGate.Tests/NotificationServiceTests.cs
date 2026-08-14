using System;
using System.Collections.Generic;
using ReinforcementGate.Api;
using ReinforcementGate.Configuration;
using ReinforcementGate.Control;
using ReinforcementGate.Domain;
using ReinforcementGate.Notifications;
using ReinforcementGate.State;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class NotificationServiceTests : IDisposable
{
    private IReinforcementController? _registeredController;

    [Fact]
    public void Both_mode_attempts_cassie_when_broadcast_throws()
    {
        FakeTransport transport = new() { ThrowOnBroadcast = true };
        FakeLogger logger = new();
        NotificationService service = TestNotifications.CreateService(
            NotificationMode.Both,
            transport,
            logger);

        service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

        Assert.Equal(1, transport.BroadcastAttempts);
        Assert.Equal(1, transport.CassieAttempts);
        Assert.Single(logger.Errors);
        Assert.Contains("notifications.skip_triggered.broadcast", logger.Errors[0]);
    }

    [Fact]
    public void Empty_channel_message_is_skipped_without_throwing()
    {
        FakeTransport transport = new();
        NotificationService service = TestNotifications.CreateService(
            NotificationMode.Both,
            transport,
            new FakeLogger(),
            broadcastMessage: string.Empty);

        service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

        Assert.Equal(0, transport.BroadcastAttempts);
        Assert.Equal(1, transport.CassieAttempts);
    }

    [Theory]
    [InlineData(NotificationMode.None, 0, 0)]
    [InlineData(NotificationMode.Broadcast, 1, 0)]
    [InlineData(NotificationMode.Cassie, 0, 1)]
    [InlineData(NotificationMode.Both, 1, 1)]
    public void Mode_selects_only_configured_channels(
        NotificationMode mode,
        int expectedBroadcasts,
        int expectedCassie)
    {
        FakeTransport transport = new();
        NotificationService service = TestNotifications.CreateService(mode, transport, new FakeLogger());

        service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

        Assert.Equal(expectedBroadcasts, transport.BroadcastAttempts);
        Assert.Equal(expectedCassie, transport.CassieAttempts);
    }

    [Fact]
    public void Cassie_failure_does_not_escape_notification_service()
    {
        FakeTransport transport = new() { ThrowOnCassie = true };
        FakeLogger logger = new();
        NotificationService service = TestNotifications.CreateService(
            NotificationMode.Cassie,
            transport,
            logger);

        Exception? exception = Record.Exception(() =>
            service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context));

        Assert.Null(exception);
        Assert.Single(logger.Errors);
        Assert.Contains("notifications.skip_triggered.cassie", logger.Errors[0]);
    }

    [Fact]
    public void Update_config_atomically_replaces_templates_for_next_call()
    {
        FakeTransport transport = new();
        NotificationsConfig initial = new();
        initial.SkipTriggered.Mode = NotificationMode.None;
        NotificationService service = new(initial, transport, new FakeLogger());
        NotificationsConfig updated = new();
        updated.SkipTriggered.Mode = NotificationMode.Broadcast;
        updated.SkipTriggered.Broadcast.Message = "updated:{target}";

        service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);
        service.UpdateConfig(updated);
        updated.SkipTriggered.Broadcast.Message = "mutated-after-update";
        service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

        Assert.Equal(1, transport.BroadcastAttempts);
        Assert.Equal("updated:ntf-mini", transport.LastBroadcastMessage);
    }

    [Fact]
    public void Unknown_tokens_are_preserved_and_warned_once_per_template()
    {
        FakeTransport transport = new();
        FakeLogger logger = new();
        NotificationService service = TestNotifications.CreateService(
            NotificationMode.Broadcast,
            transport,
            logger,
            broadcastMessage: "{unknown}|{unknown}|{target}");

        service.Notify(NotificationKind.SkipTriggered, TestNotifications.Context);

        Assert.Equal("{unknown}|{unknown}|ntf-mini", transport.LastBroadcastMessage);
        Assert.Single(logger.Warnings);
        Assert.Contains("notifications.skip_triggered.broadcast.message", logger.Warnings[0]);
    }

    [Fact]
    public void Changed_direct_and_api_calls_share_decorator_and_suppress_duplicate_notification()
    {
        ReinforcementStateService state = new();
        RecordingNotificationService notifications = new();
        NotifyingReinforcementController controller = new(state, notifications);
        _registeredController = controller;
        ReinforcementControlApi.Register(controller);

        StateTransitionResult first = controller.SetEnabled(ReinforcementTarget.Ntf, false, "Direct");
        StateTransitionResult duplicate = ReinforcementControlApi.SetEnabled(
            ReinforcementTarget.Ntf,
            false,
            "ApiDuplicate");
        StateTransitionResult second = ReinforcementControlApi.SetEnabled(
            ReinforcementTarget.Ci,
            false,
            "OtherPlugin");

        Assert.True(first.Changed);
        Assert.False(duplicate.Changed);
        Assert.True(second.Changed);
        Assert.Equal(
            new[] { NotificationKind.DisableApplied, NotificationKind.DisableApplied },
            notifications.Kinds);
        Assert.Equal(new[] { "Direct", "OtherPlugin" }, notifications.Admins);
    }

    [Fact]
    public void Controller_only_notifies_changed_set_enabled_and_arm_skip_transitions()
    {
        ReinforcementStateService state = new();
        RecordingNotificationService notifications = new();
        NotifyingReinforcementController controller = new(state, notifications);

        controller.SetEnabled(ReinforcementTarget.All, false, "Disable");
        controller.SetEnabled(ReinforcementTarget.All, true, "Enable");
        controller.ArmSkip(ReinforcementTarget.CiMini, "Skip");
        controller.ArmSkip(ReinforcementTarget.CiMini, "Duplicate");
        controller.ClearSkip(ReinforcementTarget.CiMini, "Clear");
        controller.Reset("Reset");
        controller.ResetForRound();
        controller.EvaluateWave(ReinforcementTarget.Ntf);

        Assert.Equal(
            new[]
            {
                NotificationKind.DisableApplied,
                NotificationKind.EnableApplied,
                NotificationKind.SkipArmed,
            },
            notifications.Kinds);
        Assert.Equal(new[] { "global-disabled", string.Empty, "skip" }, notifications.Reasons);
    }

    [Fact]
    public void Throwing_notification_cannot_invalidate_committed_controller_transition()
    {
        ReinforcementStateService state = new();
        NotifyingReinforcementController controller = new(state, new ThrowingNotificationService());

        StateTransitionResult result = controller.SetEnabled(ReinforcementTarget.NtfMini, false, "Admin");

        Assert.True(result.Changed);
        Assert.False(controller.GetState(ReinforcementTarget.NtfMini).IsEffectivelyEnabled);
    }

    [Fact]
    public void Decorator_forwards_inner_state_events()
    {
        ReinforcementStateService state = new();
        NotifyingReinforcementController controller = new(state, new RecordingNotificationService());
        int stateEvents = 0;
        int roundEvents = 0;
        controller.StateChanged += (_, _) => stateEvents++;
        controller.RoundStateReset += (_, _) => roundEvents++;

        controller.SetEnabled(ReinforcementTarget.Ci, false, "Admin");
        controller.ResetForRound();

        Assert.Equal(2, stateEvents);
        Assert.Equal(1, roundEvents);
    }

    public void Dispose()
    {
        if (_registeredController is not null)
            ReinforcementControlApi.Unregister(_registeredController);
    }

    private sealed class FakeTransport : INotificationTransport
    {
        public bool ThrowOnBroadcast { get; set; }

        public bool ThrowOnCassie { get; set; }

        public int BroadcastAttempts { get; private set; }

        public int CassieAttempts { get; private set; }

        public string? LastBroadcastMessage { get; private set; }

        public void SendBroadcast(string message, ushort duration, bool clearPrevious)
        {
            BroadcastAttempts++;
            LastBroadcastMessage = message;
            if (ThrowOnBroadcast)
                throw new InvalidOperationException("broadcast failed");
        }

        public void SendCassie(
            string message,
            string subtitles,
            bool playBackground,
            float priority,
            float glitchScale)
        {
            CassieAttempts++;
            if (ThrowOnCassie)
                throw new InvalidOperationException("cassie failed");
        }
    }

    private sealed class FakeLogger : INotificationLogger
    {
        public List<string> Warnings { get; } = new();

        public List<string> Errors { get; } = new();

        public void Warning(string configurationPath, string message) =>
            Warnings.Add($"{configurationPath}: {message}");

        public void Error(string configurationPath, string message, Exception exception) =>
            Errors.Add($"{configurationPath}: {message}: {exception.Message}");
    }

    private sealed class RecordingNotificationService : INotificationService
    {
        public List<NotificationKind> Kinds { get; } = new();

        public List<string> Admins { get; } = new();

        public List<string> Reasons { get; } = new();

        public void Notify(NotificationKind kind, NotificationContext context)
        {
            Kinds.Add(kind);
            Admins.Add(context.Admin);
            Reasons.Add(context.Reason);
        }

        public void UpdateConfig(NotificationsConfig config)
        {
        }
    }

    private sealed class ThrowingNotificationService : INotificationService
    {
        public void Notify(NotificationKind kind, NotificationContext context) =>
            throw new InvalidOperationException("notification failed");

        public void UpdateConfig(NotificationsConfig config)
        {
        }
    }

    private static class TestNotifications
    {
        public static NotificationContext Context { get; } = new(
            ReinforcementTarget.NtfMini,
            "九尾狐迷你增援",
            "Admin",
            "skip",
            "skip");

        public static NotificationService CreateService(
            NotificationMode mode,
            INotificationTransport transport,
            INotificationLogger logger,
            string broadcastMessage = "broadcast:{target}")
        {
            NotificationsConfig config = new();
            config.SkipTriggered.Mode = mode;
            config.SkipTriggered.Broadcast.Message = broadcastMessage;
            config.SkipTriggered.Cassie.Message = "cassie {target}";
            config.SkipTriggered.Cassie.Subtitles = "subtitle {target_name}";
            return new NotificationService(config, transport, logger);
        }
    }
}
