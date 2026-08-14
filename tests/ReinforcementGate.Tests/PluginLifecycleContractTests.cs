using System;
using System.Linq;
using System.Reflection;
using CommandSystem;
using LabApi.Events.CustomHandlers;
using ReinforcementGate.Api;
using ReinforcementGate.Commands;
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
}
