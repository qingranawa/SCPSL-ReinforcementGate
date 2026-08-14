using ReinforcementGate.Domain;

namespace ReinforcementGate.Commands;

/// <summary>Represents a parsed reinforcement command request.</summary>
public sealed class ReinforcementCommandRequest
{
    /// <summary>Creates a parsed command request.</summary>
    public ReinforcementCommandRequest(
        ReinforcementCommandAction action,
        ReinforcementTarget target)
    {
        Action = action;
        Target = target;
    }

    /// <summary>Gets the requested action.</summary>
    public ReinforcementCommandAction Action { get; }

    /// <summary>Gets the requested target, or <see cref="ReinforcementTarget.All"/> for status and reset.</summary>
    public ReinforcementTarget Target { get; }
}
