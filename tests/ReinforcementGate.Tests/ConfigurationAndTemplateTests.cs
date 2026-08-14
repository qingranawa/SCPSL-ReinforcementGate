using System;
using System.Collections.Generic;
using ReinforcementGate.Configuration;
using ReinforcementGate.Domain;
using ReinforcementGate.Notifications;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class ConfigurationAndTemplateTests
{
    [Theory]
    [InlineData(NotificationMode.None)]
    [InlineData(NotificationMode.Broadcast)]
    [InlineData(NotificationMode.Cassie)]
    [InlineData(NotificationMode.Both)]
    public void All_notification_modes_are_preserved(NotificationMode mode)
    {
        NotificationNodeConfig node = new() { Mode = mode };

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.skip_triggered",
            node,
            NotificationNodeConfig.CreateSkipTriggeredDefault());

        Assert.Equal(mode, normalized.Mode);
    }

    [Fact]
    public void Default_configuration_has_no_builtin_notification_templates()
    {
        ReinforcementGateConfig config = new();

        AssertNode(
            config.Notifications.EnableApplied,
            NotificationMode.None,
            string.Empty,
            string.Empty,
            string.Empty);
        AssertNode(
            config.Notifications.DisableApplied,
            NotificationMode.None,
            string.Empty,
            string.Empty,
            string.Empty);
        AssertNode(
            config.Notifications.DisabledWaveBlocked,
            NotificationMode.None,
            string.Empty,
            string.Empty,
            string.Empty);
        AssertNode(
            config.Notifications.SkipArmed,
            NotificationMode.None,
            string.Empty,
            string.Empty,
            string.Empty);
        AssertNode(
            config.Notifications.SkipTriggered,
            NotificationMode.None,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    [Fact]
    public void Renderer_replaces_whitelisted_tokens_and_preserves_unknown_tokens()
    {
        NotificationContext context = new(
            ReinforcementTarget.NtfMini,
            "九尾狐迷你增援",
            "Admin",
            "skip",
            "skip");

        TemplateRenderResult rendered = TemplateRenderer.Render(
            "{target}|{target_name}|{admin}|{action}|{reason}|{unknown}", context);

        Assert.Equal("ntf-mini|九尾狐迷你增援|Admin|skip|skip|{unknown}", rendered.Text);
        Assert.Equal(new[] { "{unknown}" }, rendered.UnknownTokens);
    }

    [Fact]
    public void Unknown_tokens_are_reported_once_in_first_occurrence_order()
    {
        NotificationContext context = new(
            ReinforcementTarget.Ci,
            "混沌分裂者主增援",
            "Admin",
            "disable",
            "target-disabled");

        TemplateRenderResult rendered = TemplateRenderer.Render(
            "{second}|{first}|{second}|{target}", context);

        Assert.Equal("{second}|{first}|{second}|ci", rendered.Text);
        Assert.Equal(new[] { "{second}", "{first}" }, rendered.UnknownTokens);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<string>)rendered.UnknownTokens).Add("{third}"));
    }

    [Fact]
    public void Empty_template_is_valid()
    {
        NotificationContext context = new(
            ReinforcementTarget.All,
            "全部增援",
            "Admin",
            "enable",
            string.Empty);

        TemplateRenderResult rendered = TemplateRenderer.Render(string.Empty, context);

        Assert.Equal(string.Empty, rendered.Text);
        Assert.Empty(rendered.UnknownTokens);
    }

    [Fact]
    public void Invalid_cassie_glitch_scale_restores_the_default_node()
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateSkipTriggeredDefault();
        node.Cassie.GlitchScale = 2f;

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.skip_triggered",
            node,
            NotificationNodeConfig.CreateSkipTriggeredDefault());

        Assert.Equal(0f, normalized.Cassie.GlitchScale);
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(1.01f)]
    public void Out_of_range_glitch_scale_restores_the_entire_default_node(float invalidScale)
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateSkipTriggeredDefault();
        node.Mode = NotificationMode.Cassie;
        node.Broadcast.Message = "custom";
        node.Cassie.GlitchScale = invalidScale;

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.skip_triggered",
            node,
            NotificationNodeConfig.CreateSkipTriggeredDefault());

        Assert.Equal(NotificationMode.None, normalized.Mode);
        Assert.Equal(string.Empty, normalized.Broadcast.Message);
        Assert.Equal(0f, normalized.Cassie.GlitchScale);
    }

    [Fact]
    public void Non_finite_cassie_priority_restores_the_default_node()
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateDisableAppliedDefault();
        node.Cassie.Priority = float.NaN;

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.disable_applied",
            node,
            NotificationNodeConfig.CreateDisableAppliedDefault());

        Assert.Equal(0f, normalized.Cassie.Priority);
    }

    [Fact]
    public void Zero_broadcast_duration_restores_the_default_node()
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateEnableAppliedDefault();
        node.Broadcast.Duration = 0;

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.enable_applied",
            node,
            NotificationNodeConfig.CreateEnableAppliedDefault());

        Assert.Equal((ushort)8, normalized.Broadcast.Duration);
    }

    [Fact]
    public void Undefined_notification_mode_restores_the_entire_default_node()
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateEnableAppliedDefault();
        node.Mode = (NotificationMode)999;
        node.Broadcast.Message = "custom";

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.enable_applied",
            node,
            NotificationNodeConfig.CreateEnableAppliedDefault());

        Assert.Equal(NotificationMode.None, normalized.Mode);
        Assert.Equal(string.Empty, normalized.Broadcast.Message);
    }

    [Theory]
    [InlineData("broadcast.message")]
    [InlineData("cassie.message")]
    [InlineData("cassie.subtitles")]
    public void Null_template_field_restores_the_entire_default_node(string field)
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateSkipTriggeredDefault();
        node.Mode = NotificationMode.Cassie;
        node.Broadcast.Message = "custom";

        switch (field)
        {
            case "broadcast.message":
                node.Broadcast.Message = null!;
                break;
            case "cassie.message":
                node.Cassie.Message = null!;
                break;
            case "cassie.subtitles":
                node.Cassie.Subtitles = null!;
                break;
        }

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.skip_triggered",
            node,
            NotificationNodeConfig.CreateSkipTriggeredDefault());

        Assert.Equal(NotificationMode.None, normalized.Mode);
        Assert.Equal(string.Empty, normalized.Broadcast.Message);
        Assert.Equal(string.Empty, normalized.Cassie.Message);
        Assert.Equal(string.Empty, normalized.Cassie.Subtitles);
    }

    [Fact]
    public void Invalid_node_reports_its_complete_configuration_path()
    {
        NotificationsConfig source = new();
        source.SkipTriggered.Cassie.GlitchScale = 2f;
        List<string> invalidPaths = new();

        NotificationsConfig normalized = NotificationConfigNormalizer.Normalize(
            source,
            invalidPaths.Add);

        Assert.Equal(new[] { "notifications.skip_triggered" }, invalidPaths);
        Assert.Equal(0f, normalized.SkipTriggered.Cassie.GlitchScale);
    }

    [Fact]
    public void Diagnostic_callback_failure_does_not_prevent_default_fallback()
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateDisableAppliedDefault();
        node.Broadcast.Duration = 0;

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.disable_applied",
            node,
            NotificationNodeConfig.CreateDisableAppliedDefault(),
            _ => throw new InvalidOperationException("logger unavailable"));

        Assert.Equal(NotificationMode.None, normalized.Mode);
        Assert.Equal((ushort)8, normalized.Broadcast.Duration);
    }

    [Fact]
    public void Null_node_restores_a_clone_of_the_default_node()
    {
        NotificationNodeConfig defaults = NotificationNodeConfig.CreateSkipArmedDefault();

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.skip_armed",
            null,
            defaults);

        Assert.NotSame(defaults, normalized);
        Assert.NotSame(defaults.Broadcast, normalized.Broadcast);
        Assert.NotSame(defaults.Cassie, normalized.Cassie);
        Assert.Equal(defaults.Broadcast.Message, normalized.Broadcast.Message);
    }

    [Fact]
    public void Normalization_clones_valid_nodes_without_mutating_the_serialized_tree()
    {
        NotificationNodeConfig node = NotificationNodeConfig.CreateDisableAppliedDefault();
        node.Broadcast.Message = string.Empty;

        NotificationNodeConfig normalized = NotificationConfigNormalizer.NormalizeNode(
            "notifications.disable_applied",
            node,
            NotificationNodeConfig.CreateDisableAppliedDefault());

        Assert.NotSame(node, normalized);
        Assert.NotSame(node.Broadcast, normalized.Broadcast);
        Assert.NotSame(node.Cassie, normalized.Cassie);
        Assert.Equal(string.Empty, normalized.Broadcast.Message);
        normalized.Broadcast.Message = "changed";
        Assert.Equal(string.Empty, node.Broadcast.Message);
    }

    [Fact]
    public void Normalization_returns_a_detached_notification_tree()
    {
        NotificationsConfig source = new();

        NotificationsConfig normalized = NotificationConfigNormalizer.Normalize(source);

        Assert.NotSame(source, normalized);
        Assert.NotSame(source.EnableApplied, normalized.EnableApplied);
        Assert.NotSame(source.SkipTriggered.Broadcast, normalized.SkipTriggered.Broadcast);
        normalized.EnableApplied.Broadcast.Message = "changed";
        Assert.NotEqual("changed", source.EnableApplied.Broadcast.Message);
    }

    [Theory]
    [InlineData(ReinforcementTarget.All, "all", "全部增援")]
    [InlineData(ReinforcementTarget.Ntf, "ntf", "九尾狐主增援")]
    [InlineData(ReinforcementTarget.NtfMini, "ntf-mini", "九尾狐迷你增援")]
    [InlineData(ReinforcementTarget.Ci, "ci", "混沌分裂者主增援")]
    [InlineData(ReinforcementTarget.CiMini, "ci-mini", "混沌分裂者迷你增援")]
    public void Target_names_are_centralized(
        ReinforcementTarget target,
        string commandName,
        string displayName)
    {
        Assert.Equal(commandName, ReinforcementTargetNames.ToCommandName(target));
        Assert.Equal(displayName, ReinforcementTargetNames.ToDisplayName(target));
    }

    private static void AssertNode(
        NotificationNodeConfig node,
        NotificationMode mode,
        string broadcastMessage,
        string cassieMessage,
        string subtitles)
    {
        Assert.Equal(mode, node.Mode);
        Assert.Equal(broadcastMessage, node.Broadcast.Message);
        Assert.Equal((ushort)8, node.Broadcast.Duration);
        Assert.False(node.Broadcast.ClearPrevious);
        Assert.Equal(cassieMessage, node.Cassie.Message);
        Assert.Equal(subtitles, node.Cassie.Subtitles);
        Assert.True(node.Cassie.PlayBackground);
        Assert.Equal(0f, node.Cassie.Priority);
        Assert.Equal(0f, node.Cassie.GlitchScale);
    }
}
