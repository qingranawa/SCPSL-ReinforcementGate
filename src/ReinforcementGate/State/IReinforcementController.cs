using ReinforcementGate.Domain;

namespace ReinforcementGate.State;

/// <summary>Controls reinforcement state and evaluates waves.</summary>
public interface IReinforcementController : IReinforcementStateProvider
{
    /// <summary>Enables or disables a target.</summary>
    StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source);

    /// <summary>Arms a one-shot skip.</summary>
    StateTransitionResult ArmSkip(ReinforcementTarget target, string source);

    /// <summary>Clears a one-shot skip.</summary>
    StateTransitionResult ClearSkip(ReinforcementTarget target, string source);

    /// <summary>Restores all state to defaults.</summary>
    StateTransitionResult Reset(string source);

    /// <summary>Restores defaults for a new round.</summary>
    StateTransitionResult ResetForRound();

    /// <summary>Evaluates one concrete reinforcement wave.</summary>
    WaveDecision EvaluateWave(ReinforcementTarget target);
}
