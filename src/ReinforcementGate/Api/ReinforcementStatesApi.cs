using System;
using ReinforcementGate.Domain;
using ReinforcementGate.State;

namespace ReinforcementGate.Api;

/// <summary>Provides lifecycle-safe, read-only access to reinforcement gate state.</summary>
public static class ReinforcementStatesApi
{
    private static readonly object Sync = new();
    private static IReinforcementStateProvider? _provider;

    /// <summary>Occurs after an observable state transition.</summary>
    public static event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged;

    /// <summary>Occurs after state is reset at round start.</summary>
    public static event EventHandler? RoundStateReset;

    /// <summary>Gets whether the plugin has registered its state provider.</summary>
    public static bool IsAvailable
    {
        get
        {
            lock (Sync)
                return _provider is not null;
        }
    }

    /// <summary>
    /// Gets a consistent immutable snapshot. Call this synchronous API on the game server main thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">The plugin state provider is not available.</exception>
    public static ReinforcementStateSnapshot GetSnapshot() => GetProvider().GetSnapshot();

    /// <summary>
    /// Gets one concrete target state. Call this synchronous API on the game server main thread.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="target"/> is not a concrete target.</exception>
    /// <exception cref="InvalidOperationException">The plugin state provider is not available.</exception>
    public static ReinforcementTargetState GetState(ReinforcementTarget target)
    {
        EnsureConcreteTarget(target);
        return GetProvider().GetState(target);
    }

    /// <summary>
    /// Tries to get one concrete target state. Call this synchronous API on the game server main thread.
    /// </summary>
    public static bool TryGetState(
        ReinforcementTarget target,
        out ReinforcementTargetState? state)
    {
        state = null;
        if (!IsConcreteTarget(target))
            return false;

        IReinforcementStateProvider? provider;
        lock (Sync)
            provider = _provider;

        return provider is not null && provider.TryGetState(target, out state);
    }

    internal static void Register(IReinforcementStateProvider provider)
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));

        lock (Sync)
        {
            if (ReferenceEquals(_provider, provider))
                return;
            if (_provider is not null)
                throw new InvalidOperationException("A reinforcement state provider is already registered.");

            _provider = provider;
            provider.StateChanged += OnProviderStateChanged;
            provider.RoundStateReset += OnProviderRoundStateReset;
        }
    }

    internal static void Unregister(IReinforcementStateProvider provider)
    {
        if (provider is null)
            throw new ArgumentNullException(nameof(provider));

        lock (Sync)
        {
            if (!ReferenceEquals(_provider, provider))
                return;

            provider.StateChanged -= OnProviderStateChanged;
            provider.RoundStateReset -= OnProviderRoundStateReset;
            _provider = null;
            StateChanged = null;
            RoundStateReset = null;
        }
    }

    private static IReinforcementStateProvider GetProvider()
    {
        lock (Sync)
        {
            return _provider ?? throw new InvalidOperationException(
                "ReinforcementGate state is not available because the plugin is not ready.");
        }
    }

    private static void OnProviderStateChanged(
        object? sender,
        ReinforcementStateChangedEventArgs args)
    {
        EventHandler<ReinforcementStateChangedEventArgs>? handlers = StateChanged;
        if (handlers is null)
            return;

        foreach (EventHandler<ReinforcementStateChangedEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                // Do not expose the internal state provider/controller through the public sender.
                handler(null, args);
            }
            catch
            {
                // External observers cannot invalidate an already completed transition.
            }
        }
    }

    private static void OnProviderRoundStateReset(object? sender, EventArgs args)
    {
        EventHandler? handlers = RoundStateReset;
        if (handlers is null)
            return;

        foreach (EventHandler handler in handlers.GetInvocationList())
        {
            try
            {
                // Do not expose the internal state provider/controller through the public sender.
                handler(null, args);
            }
            catch
            {
                // External observers cannot invalidate an already completed round reset.
            }
        }
    }

    private static void EnsureConcreteTarget(ReinforcementTarget target)
    {
        if (!IsConcreteTarget(target))
            throw new ArgumentOutOfRangeException(nameof(target), target, "A concrete reinforcement target is required.");
    }

    private static bool IsConcreteTarget(ReinforcementTarget target) =>
        target == ReinforcementTarget.Ntf ||
        target == ReinforcementTarget.NtfMini ||
        target == ReinforcementTarget.Ci ||
        target == ReinforcementTarget.CiMini;
}
