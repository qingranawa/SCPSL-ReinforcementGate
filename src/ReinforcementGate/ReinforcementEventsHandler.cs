using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.CustomHandlers;
using ReinforcementGate.Domain;
using ReinforcementGate.Interception;
using ReinforcementGate.State;

namespace ReinforcementGate;

/// <summary>Adapts LabAPI round and wave events to reinforcement control services.</summary>
public sealed class ReinforcementEventsHandler : CustomEventsHandler
{
    private readonly IReinforcementController _controller;
    private readonly WaveInterceptionService _interception;
    private readonly IInterceptionLogger _logger;
    private readonly HashSet<Type> _warnedUnknownWaveTypes = new();
    private bool _warnedNullWave;

    /// <summary>Creates the LabAPI event adapter.</summary>
    public ReinforcementEventsHandler(
        IReinforcementController controller,
        WaveInterceptionService interception,
        IInterceptionLogger logger)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        _interception = interception ?? throw new ArgumentNullException(nameof(interception));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public override void OnServerRoundStarted()
    {
        _controller.ResetForRound();
    }

    /// <inheritdoc />
    public override void OnServerWaveRespawning(WaveRespawningEventArgs ev)
    {
        if (ev is null)
            throw new ArgumentNullException(nameof(ev));

        if (ev.Wave is null)
        {
            if (!_warnedNullWave)
            {
                _warnedNullWave = true;
                TryWarn("Unknown reinforcement wave could not be wrapped and was allowed.");
            }

            return;
        }

        if (!WaveClassifier.TryClassify(ev.Wave, out ReinforcementTarget target))
        {
            Type waveType = ev.Wave.GetType();
            if (_warnedUnknownWaveTypes.Add(waveType))
                TryWarn($"Unknown reinforcement wave type allowed: {waveType.FullName}");
            return;
        }

        if (_interception.ShouldBlock(target))
            ev.IsAllowed = false;
    }

    private void TryWarn(string message)
    {
        try
        {
            _logger.Warn(message);
        }
        catch
        {
            // Diagnostics cannot turn an unknown wave into a blocked wave.
        }
    }
}
