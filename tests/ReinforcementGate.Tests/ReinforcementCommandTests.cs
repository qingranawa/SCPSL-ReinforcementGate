using System;
using System.Collections.Generic;
using ReinforcementGate.Api;
using ReinforcementGate.Commands;
using ReinforcementGate.Domain;
using ReinforcementGate.State;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ReinforcementGate.Tests;

public sealed class ReinforcementCommandTests : IDisposable
{
    private readonly ReinforcementStateService _service = new();

    public ReinforcementCommandTests()
    {
        ReinforcementControlApi.Unregister(_service);
        ReinforcementStatesApi.Unregister(_service);
    }

    [Theory]
    [InlineData("ntf", ReinforcementTarget.Ntf)]
    [InlineData("ntf-mini", ReinforcementTarget.NtfMini)]
    [InlineData("ci", ReinforcementTarget.Ci)]
    [InlineData("ci-mini", ReinforcementTarget.CiMini)]
    [InlineData("all", ReinforcementTarget.All)]
    [InlineData("NTF-MINI", ReinforcementTarget.NtfMini)]
    public void Parser_accepts_exact_target_names_case_insensitively(
        string text,
        ReinforcementTarget expected)
    {
        Assert.True(ReinforcementCommandParser.TryParseTarget(text, out ReinforcementTarget actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ntf_mini")]
    [InlineData("mtf")]
    [InlineData("chaos")]
    public void Parser_rejects_noncanonical_target_names(string text)
    {
        Assert.False(ReinforcementCommandParser.TryParseTarget(text, out _));
    }

    [Fact]
    public void Status_does_not_require_respawn_events_permission()
    {
        Assert.False(ReinforcementCommandParser.RequiresRespawnEvents(
            ReinforcementCommandAction.Status));
    }

    [Theory]
    [InlineData(ReinforcementCommandAction.Enable)]
    [InlineData(ReinforcementCommandAction.Disable)]
    [InlineData(ReinforcementCommandAction.Skip)]
    [InlineData(ReinforcementCommandAction.Reset)]
    public void Mutations_require_respawn_events_permission(ReinforcementCommandAction action)
    {
        Assert.True(ReinforcementCommandParser.RequiresRespawnEvents(action));
    }

    [Theory]
    [MemberData(nameof(ValidCommands))]
    public void Parser_accepts_the_documented_grammar(
        string[] arguments,
        ReinforcementCommandAction action,
        ReinforcementTarget target)
    {
        Assert.True(ReinforcementCommandParser.TryParse(
            new ArraySegment<string>(arguments),
            out ReinforcementCommandRequest? request,
            out string response));
        Assert.Equal(string.Empty, response);
        Assert.NotNull(request);
        Assert.Equal(action, request!.Action);
        Assert.Equal(target, request.Target);
    }

    [Theory]
    [MemberData(nameof(InvalidCommands))]
    public void Parser_rejects_invalid_grammar_with_the_documented_usage(
        string[] arguments,
        string expectedFragment)
    {
        Assert.False(ReinforcementCommandParser.TryParse(
            new ArraySegment<string>(arguments),
            out ReinforcementCommandRequest? request,
            out string response));
        Assert.Null(request);
        Assert.Contains(expectedFragment, response, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_formatter_includes_global_and_each_target_state_with_sources()
    {
        _service.SetEnabled(ReinforcementTarget.All, false, "GlobalAdmin");
        _service.ArmSkip(ReinforcementTarget.All, "GlobalSkipAdmin");
        _service.SetEnabled(ReinforcementTarget.Ntf, false, "NtfAdmin");
        _service.ArmSkip(ReinforcementTarget.Ntf, "NtfSkipAdmin");

        string status = ReinforcementStatusFormatter.Format(_service.GetSnapshot());

        Assert.Contains("global: disabled=true", status, StringComparison.Ordinal);
        Assert.Contains("skip=true", status, StringComparison.Ordinal);
        Assert.Contains("disabled-source=GlobalAdmin", status, StringComparison.Ordinal);
        Assert.Contains("skip-source=GlobalSkipAdmin", status, StringComparison.Ordinal);
        Assert.Contains("ntf (九尾狐大支援): local=disabled, effective=disabled, skip=true", status, StringComparison.Ordinal);
        Assert.Contains("enabled-source=NtfAdmin", status, StringComparison.Ordinal);
        Assert.Contains("skip-source=NtfSkipAdmin", status, StringComparison.Ordinal);

        Assert.Contains(
            "ntf-mini (九尾狐小支援): local=enabled, effective=disabled, skip=false, enabled-source=initial, skip-source=initial",
            status,
            StringComparison.Ordinal);
        Assert.Contains(
            "ci (混沌大支援): local=enabled, effective=disabled, skip=false, enabled-source=initial, skip-source=initial",
            status,
            StringComparison.Ordinal);
        Assert.Contains(
            "ci-mini (混沌小支援): local=enabled, effective=disabled, skip=false, enabled-source=initial, skip-source=initial",
            status,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Execute_request_delegates_mutations_and_reports_changed_and_unchanged_state()
    {
        ReinforcementStatesApi.Register(_service);
        ReinforcementControlApi.Register(_service);
        ReinforcementCommand command = new();
        ReinforcementCommandRequest request = new(
            ReinforcementCommandAction.Disable,
            ReinforcementTarget.Ntf);

        Assert.True(command.ExecuteRequest(request, "Admin", out string changedResponse));
        Assert.Contains("action=disable", changedResponse, StringComparison.Ordinal);
        Assert.Contains("target=ntf", changedResponse, StringComparison.Ordinal);
        Assert.Contains("before=enabled", changedResponse, StringComparison.Ordinal);
        Assert.Contains("after=disabled", changedResponse, StringComparison.Ordinal);
        Assert.Contains("effective=disabled", changedResponse, StringComparison.Ordinal);

        Assert.True(command.ExecuteRequest(request, "Admin", out string unchangedResponse));
        Assert.Contains("state unchanged", unchangedResponse, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Execute_request_status_and_reset_use_the_registered_apis()
    {
        ReinforcementStatesApi.Register(_service);
        ReinforcementControlApi.Register(_service);
        ReinforcementCommand command = new();
        _service.SetEnabled(ReinforcementTarget.CiMini, false, "Admin");

        Assert.True(command.ExecuteRequest(
            new ReinforcementCommandRequest(ReinforcementCommandAction.Status, ReinforcementTarget.All),
            "Viewer",
            out string status));
        Assert.Contains("ci-mini (混沌小支援): local=disabled", status, StringComparison.Ordinal);

        Assert.True(command.ExecuteRequest(
            new ReinforcementCommandRequest(ReinforcementCommandAction.Reset, ReinforcementTarget.All),
            "Admin",
            out string reset));
        Assert.Contains("action=reset", reset, StringComparison.Ordinal);
        Assert.True(_service.GetState(ReinforcementTarget.CiMini).IsLocallyEnabled);
    }

    public static IEnumerable<object[]> ValidCommands()
    {
        yield return new object[] { new[] { "status" }, ReinforcementCommandAction.Status, ReinforcementTarget.All };
        yield return new object[] { new[] { "RESET" }, ReinforcementCommandAction.Reset, ReinforcementTarget.All };
        yield return new object[] { new[] { "enable", "ntf" }, ReinforcementCommandAction.Enable, ReinforcementTarget.Ntf };
        yield return new object[] { new[] { "DISABLE", "CI-MINI" }, ReinforcementCommandAction.Disable, ReinforcementTarget.CiMini };
        yield return new object[] { new[] { "skip", "all" }, ReinforcementCommandAction.Skip, ReinforcementTarget.All };
    }

    public static IEnumerable<object[]> InvalidCommands()
    {
        yield return new object[] { Array.Empty<string>(), "reinforcement status" };
        yield return new object[] { new[] { "unknown" }, "reinforcement enable" };
        yield return new object[] { new[] { "status", "ntf" }, "reinforcement status" };
        yield return new object[] { new[] { "reset", "all" }, "reinforcement reset" };
        yield return new object[] { new[] { "enable" }, "reinforcement enable <all|ntf|ntf-mini|ci|ci-mini>" };
        yield return new object[] { new[] { "disable", "mtf" }, "all, ntf, ntf-mini, ci, ci-mini" };
        yield return new object[] { new[] { "skip", "ci", "extra" }, "reinforcement skip <all|ntf|ntf-mini|ci|ci-mini>" };
    }

    public void Dispose()
    {
        ReinforcementControlApi.Unregister(_service);
        ReinforcementStatesApi.Unregister(_service);
    }
}
