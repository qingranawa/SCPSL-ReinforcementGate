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
    public void Default_configuration_matches_the_approved_notification_templates()
    {
        ReinforcementGateConfig config = new();

        AssertNode(
            config.Notifications.EnableApplied,
            NotificationMode.Broadcast,
            "<color=green>{target_name} 已恢复刷新</color>",
            "REINFORCEMENT ENABLED",
            "{target_name} 已恢复刷新");
        AssertNode(
            config.Notifications.DisableApplied,
            NotificationMode.Both,
            "<color=red>{target_name} 已停止刷新</color>",
            "REINFORCEMENT SUSPENDED",
            "{target_name} 已停止刷新");
        Assert.Equal(NotificationMode.None, config.Notifications.DisabledWaveBlocked.Mode);
        AssertNode(
            config.Notifications.SkipArmed,
            NotificationMode.Broadcast,
            "下一次 {target_name} 支援将被跳过",
            string.Empty,
            string.Empty);
        AssertNode(
            config.Notifications.SkipTriggered,
            NotificationMode.Both,
            "{target_name} 支援已被跳过",
            "REINFORCEMENT WAVE CANCELLED",
            "{target_name} 支援已被跳过");
    }

    [Fact]
    public void Renderer_replaces_whitelisted_tokens_and_preserves_unknown_tokens()
    {
        NotificationContext context = new(
            ReinforcementTarget.NtfMini,
            "九尾狐小支援",
            "Admin",
            "skip",
            "skip");

        TemplateRenderResult rendered = TemplateRenderer.Render(
            "{target}|{target_name}|{admin}|{action}|{reason}|{unknown}", context);

        Assert.Equal("ntf-mini|九尾狐小支援|Admin|skip|skip|{unknown}", rendered.Text);
        Assert.Equal(new[] { "{unknown}" }, rendered.UnknownTokens);
    }

    [Fact]
    public void Unknown_tokens_are_reported_once_in_first_occurrence_order()
    {
        NotificationContext context = new(
            ReinforcementTarget.Ci,
            "混沌大支援",
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
            "全部支援",
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

        Assert.Equal(NotificationMode.Both, normalized.Mode);
        Assert.Equal("{target_name} 支援已被跳过", normalized.Broadcast.Message);
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
    [InlineData(ReinforcementTarget.All, "all", "全部支援")]
    [InlineData(ReinforcementTarget.Ntf, "ntf", "九尾狐大支援")]
    [InlineData(ReinforcementTarget.NtfMini, "ntf-mini", "九尾狐小支援")]
    [InlineData(ReinforcementTarget.Ci, "ci", "混沌大支援")]
    [InlineData(ReinforcementTarget.CiMini, "ci-mini", "混沌小支援")]
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
