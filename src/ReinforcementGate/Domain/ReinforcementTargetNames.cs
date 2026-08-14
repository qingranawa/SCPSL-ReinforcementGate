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
        ReinforcementTarget.All => "全部支援",
        ReinforcementTarget.Ntf => "九尾狐大支援",
        ReinforcementTarget.NtfMini => "九尾狐小支援",
        ReinforcementTarget.Ci => "混沌大支援",
        ReinforcementTarget.CiMini => "混沌小支援",
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown reinforcement target."),
    };
}
