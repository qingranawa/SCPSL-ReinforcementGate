using System;

namespace ReinforcementGate.Domain;

/// <summary>Provides the canonical command and display names for reinforcement targets.</summary>
public static class ReinforcementTargetNames
{
    /// <summary>Returns the canonical Remote Admin command name for a target.</summary>
    public static string ToCommandName(ReinforcementTarget target) => target switch
    {
        ReinforcementTarget.All => "all",
        ReinforcementTarget.Ntf => "ntf",
        ReinforcementTarget.NtfMini => "ntf-mini",
        ReinforcementTarget.Ci => "ci",
        ReinforcementTarget.CiMini => "ci-mini",
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown reinforcement target."),
    };

    /// <summary>Returns the canonical Chinese display name for a target.</summary>
    public static string ToDisplayName(ReinforcementTarget target) => target switch
    {
        ReinforcementTarget.All => "全部增援",
        ReinforcementTarget.Ntf => "九尾狐主增援",
        ReinforcementTarget.NtfMini => "九尾狐迷你增援",
        ReinforcementTarget.Ci => "混沌分裂者主增援",
        ReinforcementTarget.CiMini => "混沌分裂者迷你增援",
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown reinforcement target."),
    };
}
