using System;
using ReinforcementGate.Domain;

namespace ReinforcementGate.Commands;

/// <summary>Parses and authorizes the pure command grammar.</summary>
public static class ReinforcementCommandParser
{
    /// <summary>Lists all supported command forms.</summary>
    public const string FullUsage =
        "Usage:\n" +
        "reinforcement status\n" +
        "reinforcement enable <all|ntf|ntf-mini|ci|ci-mini>\n" +
        "reinforcement disable <all|ntf|ntf-mini|ci|ci-mini>\n" +
        "reinforcement skip <all|ntf|ntf-mini|ci|ci-mini>\n" +
        "reinforcement reset";

    private const string ValidTargets = "Valid targets: all, ntf, ntf-mini, ci, ci-mini.";

    /// <summary>Parses the arguments supplied after the root command.</summary>
    public static bool TryParse(
        ArraySegment<string> arguments,
        out ReinforcementCommandRequest? request,
        out string response)
    {
        request = null;
        response = string.Empty;
        if (arguments.Count == 0)
        {
            response = FullUsage;
            return false;
        }

        string actionText = arguments.Array![arguments.Offset];
        if (!TryParseAction(actionText, out ReinforcementCommandAction action))
        {
            response = FullUsage;
            return false;
        }

        if (action == ReinforcementCommandAction.Status || action == ReinforcementCommandAction.Reset)
        {
            if (arguments.Count != 1)
            {
                response = GetUsage(action);
                return false;
            }

            request = new ReinforcementCommandRequest(action, ReinforcementTarget.All);
            return true;
        }

        if (arguments.Count != 2)
        {
            response = GetUsage(action);
            return false;
        }

        string targetText = arguments.Array[arguments.Offset + 1];
        if (!TryParseTarget(targetText, out ReinforcementTarget target))
        {
            response = ValidTargets;
            return false;
        }

        request = new ReinforcementCommandRequest(action, target);
        return true;
    }

    /// <summary>Parses one canonical target name case-insensitively.</summary>
    public static bool TryParseTarget(string? text, out ReinforcementTarget target)
    {
        switch (text?.ToLowerInvariant())
        {
            case "all":
                target = ReinforcementTarget.All;
                return true;
            case "ntf":
                target = ReinforcementTarget.Ntf;
                return true;
            case "ntf-mini":
                target = ReinforcementTarget.NtfMini;
                return true;
            case "ci":
                target = ReinforcementTarget.Ci;
                return true;
            case "ci-mini":
                target = ReinforcementTarget.CiMini;
                return true;
            default:
                target = default;
                return false;
        }
    }

    /// <summary>Returns whether an action changes state and requires RespawnEvents.</summary>
    public static bool RequiresRespawnEvents(ReinforcementCommandAction action) => action switch
    {
        ReinforcementCommandAction.Status => false,
        ReinforcementCommandAction.Enable => true,
        ReinforcementCommandAction.Disable => true,
        ReinforcementCommandAction.Skip => true,
        ReinforcementCommandAction.Reset => true,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown command action."),
    };

    private static bool TryParseAction(string text, out ReinforcementCommandAction action)
    {
        switch (text.ToLowerInvariant())
        {
            case "status":
                action = ReinforcementCommandAction.Status;
                return true;
            case "enable":
                action = ReinforcementCommandAction.Enable;
                return true;
            case "disable":
                action = ReinforcementCommandAction.Disable;
                return true;
            case "skip":
                action = ReinforcementCommandAction.Skip;
                return true;
            case "reset":
                action = ReinforcementCommandAction.Reset;
                return true;
            default:
                action = default;
                return false;
        }
    }

    private static string GetUsage(ReinforcementCommandAction action) => action switch
    {
        ReinforcementCommandAction.Status => "Usage: reinforcement status",
        ReinforcementCommandAction.Enable =>
            "Usage: reinforcement enable <all|ntf|ntf-mini|ci|ci-mini>",
        ReinforcementCommandAction.Disable =>
            "Usage: reinforcement disable <all|ntf|ntf-mini|ci|ci-mini>",
        ReinforcementCommandAction.Skip =>
            "Usage: reinforcement skip <all|ntf|ntf-mini|ci|ci-mini>",
        ReinforcementCommandAction.Reset => "Usage: reinforcement reset",
        _ => FullUsage,
    };
}
