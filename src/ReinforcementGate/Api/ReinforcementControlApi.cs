using System;
using ReinforcementGate.Domain;
using ReinforcementGate.State;

namespace ReinforcementGate.Api;

/// <summary>Provides lifecycle-safe control of reinforcement gate state.</summary>
public static class ReinforcementControlApi
{
    private static readonly object Sync = new();
    private static IReinforcementController? _controller;

    /// <summary>Enables or disables a target. Call this synchronous API on the game server main thread.</summary>
    public static StateTransitionResult SetEnabled(
        ReinforcementTarget target,
        bool enabled,
        string source) => GetController().SetEnabled(target, enabled, source);

    /// <summary>Arms a one-shot skip. Call this synchronous API on the game server main thread.</summary>
    public static StateTransitionResult ArmSkip(
        ReinforcementTarget target,
        string source) => GetController().ArmSkip(target, source);

    /// <summary>Clears a one-shot skip. Call this synchronous API on the game server main thread.</summary>
    public static StateTransitionResult ClearSkip(
        ReinforcementTarget target,
        string source) => GetController().ClearSkip(target, source);

    /// <summary>Restores all state to defaults. Call this synchronous API on the game server main thread.</summary>
    public static StateTransitionResult Reset(string source) => GetController().Reset(source);

    internal static void Register(IReinforcementController controller)
    {
        if (controller is null)
            throw new ArgumentNullException(nameof(controller));

        lock (Sync)
        {
            if (ReferenceEquals(_controller, controller))
                return;
            if (_controller is not null)
                throw new InvalidOperationException("A reinforcement controller is already registered.");

            _controller = controller;
        }
    }

    internal static void Unregister(IReinforcementController controller)
    {
        if (controller is null)
            throw new ArgumentNullException(nameof(controller));

        lock (Sync)
        {
            if (ReferenceEquals(_controller, controller))
                _controller = null;
        }
    }

    private static IReinforcementController GetController()
    {
        lock (Sync)
        {
            return _controller ?? throw new InvalidOperationException(
                "ReinforcementGate control is not available because the plugin is not ready.");
        }
    }
}
