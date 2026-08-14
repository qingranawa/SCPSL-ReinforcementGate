using System;
using ReinforcementGate.Domain;

namespace ReinforcementGate.State;

/// <summary>Provides read-only access to reinforcement state.</summary>
public interface IReinforcementStateProvider
{
    /// <summary>Occurs after an observable state transition.</summary>
    event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged;

    /// <summary>Occurs whenever round state is reset.</summary>
    event EventHandler? RoundStateReset;

    /// <summary>Gets a consistent immutable snapshot.</summary>
    ReinforcementStateSnapshot GetSnapshot();

    /// <summary>Gets one concrete target state.</summary>
    ReinforcementTargetState GetState(ReinforcementTarget target);

    /// <summary>Tries to get one concrete target state.</summary>
    bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state);
}
