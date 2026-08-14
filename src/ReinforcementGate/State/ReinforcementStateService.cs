using System;
using System.Collections.Generic;
using ReinforcementGate.Domain;

namespace ReinforcementGate.State;

/// <summary>Stores and atomically transitions reinforcement gate state in memory.</summary>
public sealed class ReinforcementStateService : IReinforcementController
{
    private const string InitialSource = "initial";
    private const string RoundStartSource = "round-start";

    private static readonly ReinforcementTarget[] ConcreteTargets =
    {
        ReinforcementTarget.Ntf,
        ReinforcementTarget.NtfMini,
        ReinforcementTarget.Ci,
        ReinforcementTarget.CiMini,
    };

    private readonly object _sync = new();
    private readonly Dictionary<ReinforcementTarget, MutableTargetState> _targets = new();
    private bool _globalDisabled;
    private bool _globalSkipArmed;
    private string _globalDisabledSource = InitialSource;
    private string _globalSkipSource = InitialSource;

    /// <summary>Initializes default in-memory state.</summary>
    public ReinforcementStateService()
    {
        foreach (ReinforcementTarget target in ConcreteTargets)
            _targets.Add(target, new MutableTargetState());
    }

    /// <inheritdoc />
    public event EventHandler<ReinforcementStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler? RoundStateReset;

    /// <inheritdoc />
    public ReinforcementStateSnapshot GetSnapshot()
    {
        lock (_sync)
            return BuildSnapshot();
    }

    /// <inheritdoc />
    public ReinforcementTargetState GetState(ReinforcementTarget target)
    {
        EnsureConcreteTarget(target);
        lock (_sync)
            return BuildTargetState(target, _targets[target]);
    }

    /// <inheritdoc />
    public bool TryGetState(ReinforcementTarget target, out ReinforcementTargetState? state)
    {
        EnsureConcreteTarget(target);
        lock (_sync)
        {
            state = BuildTargetState(target, _targets[target]);
            return true;
        }
    }

    /// <inheritdoc />
    public StateTransitionResult SetEnabled(ReinforcementTarget target, bool enabled, string source)
    {
        EnsureMutationTarget(target);
        EnsureSource(source);

        return Transition(
            enabled ? ReinforcementStateAction.Enable : ReinforcementStateAction.Disable,
            target,
            source,
            () =>
            {
                if (target == ReinforcementTarget.All)
                {
                    _globalDisabled = !enabled;
                    _globalDisabledSource = source;
                    return;
                }

                MutableTargetState local = _targets[target];
                local.IsEnabled = enabled;
                local.EnabledSource = source;
            });
    }

    /// <inheritdoc />
    public StateTransitionResult ArmSkip(ReinforcementTarget target, string source)
    {
        EnsureMutationTarget(target);
        EnsureSource(source);

        return Transition(ReinforcementStateAction.ArmSkip, target, source, () =>
        {
            if (target == ReinforcementTarget.All)
            {
                _globalSkipArmed = true;
                _globalSkipSource = source;
                return;
            }

            MutableTargetState local = _targets[target];
            local.IsSkipArmed = true;
            local.SkipSource = source;
        });
    }

    /// <inheritdoc />
    public StateTransitionResult ClearSkip(ReinforcementTarget target, string source)
    {
        EnsureMutationTarget(target);
        EnsureSource(source);

        return Transition(ReinforcementStateAction.ClearSkip, target, source, () =>
        {
            if (target == ReinforcementTarget.All)
            {
                _globalSkipArmed = false;
                _globalSkipSource = source;
                return;
            }

            MutableTargetState local = _targets[target];
            local.IsSkipArmed = false;
            local.SkipSource = source;
        });
    }

    /// <inheritdoc />
    public StateTransitionResult Reset(string source)
    {
        EnsureSource(source);
        return ResetCore(ReinforcementStateAction.Reset, source, publishRoundReset: false);
    }

    /// <inheritdoc />
    public StateTransitionResult ResetForRound() =>
        ResetCore(ReinforcementStateAction.RoundReset, RoundStartSource, publishRoundReset: true);

    /// <inheritdoc />
    public WaveDecision EvaluateWave(ReinforcementTarget target)
    {
        EnsureConcreteTarget(target);

        lock (_sync)
        {
            MutableTargetState local = _targets[target];

            if (_globalDisabled)
                return WaveDecision.Blocked(target, ReinforcementBlockReason.GlobalDisabled, _globalDisabledSource);

            if (!local.IsEnabled)
                return WaveDecision.Blocked(target, ReinforcementBlockReason.TargetDisabled, local.EnabledSource);

            if (local.IsSkipArmed)
            {
                string source = local.SkipSource;
                StateTransitionResult consumed = ConsumeTargetSkip(target, source);
                return WaveDecision.Blocked(target, ReinforcementBlockReason.TargetSkip, source, consumed);
            }

            if (_globalSkipArmed)
            {
                string source = _globalSkipSource;
                StateTransitionResult consumed = ConsumeGlobalSkip(source);
                return WaveDecision.Blocked(target, ReinforcementBlockReason.GlobalSkip, source, consumed);
            }

            return WaveDecision.Allowed(target);
        }
    }

    private StateTransitionResult ResetCore(
        ReinforcementStateAction action,
        string source,
        bool publishRoundReset)
    {
        StateTransitionResult result = Transition(action, ReinforcementTarget.All, source, () =>
        {
            _globalDisabled = false;
            _globalSkipArmed = false;
            _globalDisabledSource = source;
            _globalSkipSource = source;

            foreach (MutableTargetState local in _targets.Values)
            {
                local.IsEnabled = true;
                local.IsSkipArmed = false;
                local.EnabledSource = source;
                local.SkipSource = source;
            }
        });

        if (publishRoundReset)
            RoundStateReset?.Invoke(this, EventArgs.Empty);

        return result;
    }

    private StateTransitionResult ConsumeTargetSkip(ReinforcementTarget target, string source)
    {
        MutableTargetState local = _targets[target];
        return TransitionLocked(ReinforcementStateAction.ConsumeSkip, target, source, () =>
        {
            local.IsSkipArmed = false;
            local.SkipSource = source;
        });
    }

    private StateTransitionResult ConsumeGlobalSkip(string source) =>
        TransitionLocked(ReinforcementStateAction.ConsumeSkip, ReinforcementTarget.All, source, () =>
        {
            _globalSkipArmed = false;
            _globalSkipSource = source;
        });

    private StateTransitionResult Transition(
        ReinforcementStateAction action,
        ReinforcementTarget target,
        string source,
        Action mutation)
    {
        lock (_sync)
            return TransitionLocked(action, target, source, mutation);
    }

    private StateTransitionResult TransitionLocked(
        ReinforcementStateAction action,
        ReinforcementTarget target,
        string source,
        Action mutation)
    {
        ReinforcementStateSnapshot before = BuildSnapshot();
        mutation();
        ReinforcementStateSnapshot after = BuildSnapshot();
        StateTransitionResult result = new(
            !SnapshotsEqual(before, after),
            before,
            after,
            action,
            target,
            source);

        if (result.Changed)
            StateChanged?.Invoke(this, new ReinforcementStateChangedEventArgs(result));

        return result;
    }

    private ReinforcementStateSnapshot BuildSnapshot()
    {
        Dictionary<ReinforcementTarget, ReinforcementTargetState> targets = new();
        foreach (ReinforcementTarget target in ConcreteTargets)
            targets.Add(target, BuildTargetState(target, _targets[target]));

        return new ReinforcementStateSnapshot(
            _globalDisabled,
            _globalSkipArmed,
            _globalDisabledSource,
            _globalSkipSource,
            targets);
    }

    private ReinforcementTargetState BuildTargetState(
        ReinforcementTarget target,
        MutableTargetState local) =>
        new(
            target,
            local.IsEnabled,
            !_globalDisabled && local.IsEnabled,
            local.IsSkipArmed,
            local.EnabledSource,
            local.SkipSource);

    private static bool SnapshotsEqual(
        ReinforcementStateSnapshot left,
        ReinforcementStateSnapshot right)
    {
        if (left.IsGlobalDisabled != right.IsGlobalDisabled ||
            left.IsGlobalSkipArmed != right.IsGlobalSkipArmed ||
            left.GlobalDisabledLastChangedBy != right.GlobalDisabledLastChangedBy ||
            left.GlobalSkipLastChangedBy != right.GlobalSkipLastChangedBy)
        {
            return false;
        }

        foreach (ReinforcementTarget target in ConcreteTargets)
        {
            ReinforcementTargetState leftTarget = left.Targets[target];
            ReinforcementTargetState rightTarget = right.Targets[target];
            if (leftTarget.IsLocallyEnabled != rightTarget.IsLocallyEnabled ||
                leftTarget.IsEffectivelyEnabled != rightTarget.IsEffectivelyEnabled ||
                leftTarget.IsSkipArmed != rightTarget.IsSkipArmed ||
                leftTarget.EnabledLastChangedBy != rightTarget.EnabledLastChangedBy ||
                leftTarget.SkipLastChangedBy != rightTarget.SkipLastChangedBy)
            {
                return false;
            }
        }

        return true;
    }

    private static void EnsureSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Source cannot be null, empty, or whitespace.", nameof(source));
    }

    private static void EnsureMutationTarget(ReinforcementTarget target)
    {
        if (target != ReinforcementTarget.All && Array.IndexOf(ConcreteTargets, target) < 0)
            throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown reinforcement target.");
    }

    private static void EnsureConcreteTarget(ReinforcementTarget target)
    {
        if (target == ReinforcementTarget.All || Array.IndexOf(ConcreteTargets, target) < 0)
            throw new ArgumentOutOfRangeException(nameof(target), target, "A concrete reinforcement target is required.");
    }

    private sealed class MutableTargetState
    {
        public bool IsEnabled { get; set; } = true;

        public bool IsSkipArmed { get; set; }

        public string EnabledSource { get; set; } = InitialSource;

        public string SkipSource { get; set; } = InitialSource;
    }
}
