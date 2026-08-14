using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReinforcementGate.Configuration;
using ReinforcementGate.Notifications;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReinforcementGate.Tests;

public sealed class ReadmeContractTests
{
    [Fact]
    public void Readme_documents_installation_commands_permissions_and_precedence()
    {
        string readme = ReadReadme();

        Assert.Contains(
            @"%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\ReinforcementGate.dll",
            readme,
            StringComparison.Ordinal);
        Assert.Contains(
            @"%AppData%\SCP Secret Laboratory\LabAPI\configs\<port>\ReinforcementGate\reinforcement-gate.yml",
            readme,
            StringComparison.Ordinal);

        string[] commands =
        {
            "`reinforcement status`",
            "`reinforcement enable <target>`",
            "`reinforcement disable <target>`",
            "`reinforcement skip <target>`",
            "`reinforcement reset`",
        };
        Assert.All(commands, command => Assert.Contains(command, readme, StringComparison.Ordinal));
        Assert.Contains("别名为 `rf`", readme, StringComparison.Ordinal);

        string[] targetRows =
        {
            "| `all` | 全部支援波次的全局控制",
            "| `ntf` | 九尾狐大支援（MTF Primary Wave / `MtfWave`） | `MtfWave` |",
            "| `ntf-mini` | 九尾狐小支援（MTF Mini-Wave / `MiniMtfWave`） | `MiniMtfWave` |",
            "| `ci` | 混沌大支援（CI Primary Wave / `ChaosWave`） | `ChaosWave` |",
            "| `ci-mini` | 混沌小支援（CI Mini-Wave / `MiniChaosWave`） | `MiniChaosWave` |",
        };
        Assert.All(targetRows, row => Assert.Contains(row, readme, StringComparison.Ordinal));

        Assert.Contains(
            "`status` 对所有 Remote Admin 调用者开放，不检查 RA 权限节点，也不要求 `RespawnEvents`。",
            readme,
            StringComparison.Ordinal);
        Assert.Contains(
            "`enable`、`disable`、`skip` 和 `reset` 会改变状态，要求调用者拥有 `PlayerPermissions.RespawnEvents`。",
            readme,
            StringComparison.Ordinal);

        string[] precedence =
        {
            "1. 全局已禁用：拦截，原因是 `global-disabled`。",
            "2. 指定支援已禁用：拦截，原因是 `target-disabled`。",
            "3. 对应分类 `skip` 已就绪：拦截并只消费该分类 `skip`，原因是 `skip`。",
            "4. 全局 `skip` 已就绪：拦截并只消费全局 `skip`，原因是 `skip`。",
            "5. 以上均不成立：放行支援刷新。",
        };
        Assert.All(precedence, line => Assert.Contains(line, readme, StringComparison.Ordinal));
    }

    [Fact]
    public void Readme_default_yaml_deserializes_to_the_real_complete_configuration_contract()
    {
        string yaml = ExtractDefaultConfigurationYaml(ReadReadme());
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        IDictionary<object, object> root =
            deserializer.Deserialize<Dictionary<object, object>>(yaml);
        AssertExactKeys(root, "notifications");
        IDictionary<object, object> notifications = GetMap(root, "notifications");
        string[] notificationNames =
        {
            "enable_applied",
            "disable_applied",
            "disabled_wave_blocked",
            "skip_armed",
            "skip_triggered",
        };
        AssertExactKeys(notifications, notificationNames);

        foreach (string notificationName in notificationNames)
        {
            IDictionary<object, object> node = GetMap(notifications, notificationName);
            AssertExactKeys(node, "mode", "broadcast", "cassie");
            AssertExactKeys(
                GetMap(node, "broadcast"),
                "message",
                "duration",
                "clear_previous");
            AssertExactKeys(
                GetMap(node, "cassie"),
                "message",
                "subtitles",
                "play_background",
                "priority",
                "glitch_scale");
        }

        ReinforcementGateConfig documented = deserializer.Deserialize<ReinforcementGateConfig>(yaml);
        NotificationsConfig actual = documented.Notifications;

        AssertNode(actual.EnableApplied, NotificationMode.None, "", "", "");
        AssertNode(actual.DisableApplied, NotificationMode.None, "", "", "");
        AssertNode(actual.DisabledWaveBlocked, NotificationMode.None, "", "", "");
        AssertNode(actual.SkipArmed, NotificationMode.None, "", "", "");
        AssertNode(actual.SkipTriggered, NotificationMode.None, "", "", "");

        string[] fields =
        {
            "`broadcast.message`", "`broadcast.duration`", "`broadcast.clear_previous`",
            "`cassie.message`", "`cassie.subtitles`", "`cassie.play_background`",
            "`cassie.priority`", "`cassie.glitch_scale`",
        };
        Assert.All(fields, field => Assert.Contains(field, ReadReadme(), StringComparison.Ordinal));
        Assert.All(new[] { "`None`", "`Broadcast`", "`Cassie`", "`Both`" },
            mode => Assert.Contains(mode, ReadReadme(), StringComparison.Ordinal));
    }

    [Fact]
    public void Readme_documents_compatibility_build_commands_and_live_validation_boundary()
    {
        string readme = ReadReadme();

        Assert.Contains("LabAPI 1.1.7", readme, StringComparison.Ordinal);
        Assert.Contains(".NET Framework 4.8（`net48`）", readme, StringComparison.Ordinal);
        Assert.Contains("插件不依赖 EXILED 或 LabExtended。", readme, StringComparison.Ordinal);
        Assert.Contains("SL_REFERENCES", readme, StringComparison.Ordinal);
        Assert.Contains(
            "dotnet build ReinforcementGate.sln --configuration Release --no-restore",
            readme,
            StringComparison.Ordinal);
        Assert.Contains(
            "dotnet test tests/ReinforcementGate.Tests/ReinforcementGate.Tests.csproj --configuration Release --no-build",
            readme,
            StringComparison.Ordinal);
        Assert.Contains("真实服务器程序集", readme, StringComparison.Ordinal);
        Assert.Contains("在测试服验证", readme, StringComparison.Ordinal);
        Assert.Contains("仅用编译桩通过单元测试不代表服务器兼容", readme, StringComparison.Ordinal);
    }

    [Fact]
    public void Readme_documents_templates_apis_threading_and_non_goals_as_real_contracts()
    {
        string readme = ReadReadme();

        Assert.All(
            new[] { "{target}", "{target_name}", "{admin}", "{action}", "{reason}" },
            token => Assert.Contains($"| `{token}` |", readme, StringComparison.Ordinal));
        Assert.Contains("`enable` 通知中为空字符串", readme, StringComparison.Ordinal);

        Assert.Contains("ReinforcementStatesApi.IsAvailable", readme, StringComparison.Ordinal);
        Assert.Contains("ReinforcementStateSnapshot snapshot = ReinforcementStatesApi.GetSnapshot();", readme, StringComparison.Ordinal);
        Assert.Contains("ReinforcementTargetState ntf = snapshot.Targets[ReinforcementTarget.Ntf];", readme, StringComparison.Ordinal);
        Assert.Contains("ReinforcementControlApi.ArmSkip(", readme, StringComparison.Ordinal);
        Assert.Contains("ReinforcementTarget.CiMini,", readme, StringComparison.Ordinal);
        Assert.Contains("StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source);", readme, StringComparison.Ordinal);
        Assert.Contains("StateTransitionResult ClearSkip(ReinforcementTarget target, string source);", readme, StringComparison.Ordinal);
        Assert.Contains("ReinforcementStatesApi.StateChanged", readme, StringComparison.Ordinal);
        Assert.Contains("ReinforcementEvents.WaveBlocked", readme, StringComparison.Ordinal);
        Assert.Contains("所有公共 API 都是同步 API，必须在游戏服务器主线程调用。", readme, StringComparison.Ordinal);

        string[] nonGoals =
        {
            "已经生成的玩家", "不会主动创建支援", "不会回溯已经发生的支援事件",
        };
        Assert.All(nonGoals, term => Assert.Contains(term, readme, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadReadme() =>
        File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "README.md"));

    private static string ExtractDefaultConfigurationYaml(string readme)
    {
        const string marker = "完整默认配置如下";
        int markerIndex = readme.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, "README must introduce the complete default configuration.");
        int openingFence = readme.IndexOf("```yaml", markerIndex, StringComparison.Ordinal);
        Assert.True(openingFence >= 0, "README must contain a YAML default configuration fence.");
        int yamlStart = openingFence + "```yaml".Length;
        int closingFence = readme.IndexOf("```", yamlStart, StringComparison.Ordinal);
        Assert.True(closingFence >= 0, "README default YAML fence must be closed.");
        return readme.Substring(yamlStart, closingFence - yamlStart);
    }

    private static IDictionary<object, object> GetMap(
        IDictionary<object, object> parent,
        string key)
    {
        Assert.True(parent.TryGetValue(key, out object? value), $"YAML key '{key}' is required.");
        return Assert.IsAssignableFrom<IDictionary<object, object>>(value);
    }

    private static void AssertExactKeys(
        IDictionary<object, object> map,
        params string[] expected)
    {
        string[] actual = map.Keys.Select(key => Assert.IsType<string>(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        string[] sortedExpected = expected.OrderBy(key => key, StringComparer.Ordinal).ToArray();
        Assert.Equal(sortedExpected, actual);
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
