using System;
using System.Text;
using ReinforcementGate.Domain;

namespace ReinforcementGate.Commands;

/// <summary>Formats stable, canonical Remote Admin state responses.</summary>
public static class ReinforcementStatusFormatter
{
    private static readonly ReinforcementTarget[] ConcreteTargets =
    {
        ReinforcementTarget.Ntf,
        ReinforcementTarget.NtfMini,
        ReinforcementTarget.Ci,
        ReinforcementTarget.CiMini,
    };

    /// <summary>Formats a complete state snapshot.</summary>
    public static string Format(ReinforcementStateSnapshot snapshot)
    {
        if (snapshot is null)
            throw new ArgumentNullException(nameof(snapshot));

        StringBuilder response = new("Reinforcement status\n");
        response.Append("global: disabled=")
            .Append(FormatBoolean(snapshot.IsGlobalDisabled))
            .Append(", skip=")
            .Append(FormatBoolean(snapshot.IsGlobalSkipArmed))
            .Append(", disabled-source=")
            .Append(snapshot.GlobalDisabledLastChangedBy)
            .Append(", skip-source=")
            .Append(snapshot.GlobalSkipLastChangedBy);

        foreach (ReinforcementTarget target in ConcreteTargets)
        {
            ReinforcementTargetState state = snapshot.Targets[target];
            response.Append('\n')
                .Append(FormatTargetName(target))
                .Append(": local=")
                .Append(FormatEnabled(state.IsLocallyEnabled))
                .Append(", effective=")
                .Append(FormatEnabled(state.IsEffectivelyEnabled))
                .Append(", skip=")
                .Append(FormatBoolean(state.IsSkipArmed))
                .Append(", enabled-source=")
                .Append(state.EnabledLastChangedBy)
                .Append(", skip-source=")
                .Append(state.SkipLastChangedBy);
        }

        return response.ToString();
    }

    /// <summary>Formats a state transition response.</summary>
    public static string FormatTransition(
        ReinforcementCommandAction action,
        StateTransitionResult transition)
    {
        if (transition is null)
            throw new ArgumentNullException(nameof(transition));

        string prefix = transition.Changed ? "state changed" : "state unchanged";
        string actionName = ToCommandName(action);
        string targetName = FormatTargetName(transition.Target);
        return prefix +
            "; action=" + actionName +
            ", target=" + targetName +
            ", before=" + FormatRelevantState(action, transition.Target, transition.Before) +
            ", after=" + FormatRelevantState(action, transition.Target, transition.After) +
            ", effective=" + FormatEffectiveState(transition.Target, transition.After);
    }

    private static string FormatRelevantState(
        ReinforcementCommandAction action,
        ReinforcementTarget target,
        ReinforcementStateSnapshot snapshot)
    {
        if (action == ReinforcementCommandAction.Reset)
        {
            return snapshot.IsGlobalDisabled || HasDisabledTarget(snapshot)
                ? "disabled"
                : "enabled";
        }

        if (action == ReinforcementCommandAction.Skip)
        {
            bool armed = target == ReinforcementTarget.All
                ? snapshot.IsGlobalSkipArmed
                : snapshot.Targets[target].IsSkipArmed;
            return armed ? "armed" : "clear";
        }

        bool enabled = target == ReinforcementTarget.All
            ? !snapshot.IsGlobalDisabled
            : snapshot.Targets[target].IsLocallyEnabled;
        return FormatEnabled(enabled);
    }

    private static string FormatEffectiveState(
        ReinforcementTarget target,
        ReinforcementStateSnapshot snapshot) =>
        target == ReinforcementTarget.All
            ? FormatEnabled(!snapshot.IsGlobalDisabled)
            : FormatEnabled(snapshot.Targets[target].IsEffectivelyEnabled);

    private static bool HasDisabledTarget(ReinforcementStateSnapshot snapshot)
    {
        foreach (ReinforcementTarget target in ConcreteTargets)
        {
            if (!snapshot.Targets[target].IsLocallyEnabled)
                return true;
        }

        return false;
    }

    private static string ToCommandName(ReinforcementCommandAction action) => action switch
    {
        ReinforcementCommandAction.Status => "status",
        ReinforcementCommandAction.Enable => "enable",
        ReinforcementCommandAction.Disable => "disable",
        ReinforcementCommandAction.Skip => "skip",
        ReinforcementCommandAction.Reset => "reset",
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown command action."),
    };

    private static string FormatEnabled(bool enabled) => enabled ? "enabled" : "disabled";

    private static string FormatBoolean(bool value) => value ? "true" : "false";

    private static string FormatTargetName(ReinforcementTarget target) =>
        ReinforcementTargetNames.ToCommandName(target) +
        " (" + ReinforcementTargetNames.ToDisplayName(target) + ")";
}
