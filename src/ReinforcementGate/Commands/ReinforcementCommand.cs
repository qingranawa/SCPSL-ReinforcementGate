using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using ReinforcementGate.Api;
using ReinforcementGate.Domain;

namespace ReinforcementGate.Commands;

/// <summary>Inspects or controls reinforcement waves from Remote Admin.</summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ReinforcementCommand : ICommand
{
    /// <inheritdoc />
    public string Command => "reinforcement";

    /// <inheritdoc />
    public string[] Aliases => ["rf"];

    /// <inheritdoc />
    public string Description => "Inspect or control reinforcement waves.";

    /// <inheritdoc />
    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!ReinforcementCommandParser.TryParse(arguments, out ReinforcementCommandRequest? request, out response))
            return false;

        if (ReinforcementCommandParser.RequiresRespawnEvents(request!.Action) &&
            !sender.CheckPermission(PlayerPermissions.RespawnEvents))
        {
            response = "Missing Remote Admin permission: RespawnEvents.";
            return false;
        }

        string source = Player.Get(sender)?.Nickname ?? sender.SenderId;
        return ExecuteRequest(request, source, out response);
    }

    internal bool ExecuteRequest(
        ReinforcementCommandRequest request,
        string source,
        out string response)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        try
        {
            if (request.Action == ReinforcementCommandAction.Status)
            {
                response = ReinforcementStatusFormatter.Format(ReinforcementStatesApi.GetSnapshot());
                return true;
            }

            StateTransitionResult transition = request.Action switch
            {
                ReinforcementCommandAction.Enable =>
                    ReinforcementControlApi.SetEnabled(request.Target, true, source),
                ReinforcementCommandAction.Disable =>
                    ReinforcementControlApi.SetEnabled(request.Target, false, source),
                ReinforcementCommandAction.Skip =>
                    ReinforcementControlApi.ArmSkip(request.Target, source),
                ReinforcementCommandAction.Reset => ReinforcementControlApi.Reset(source),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(request),
                    request.Action,
                    "Unknown command action."),
            };

            response = ReinforcementStatusFormatter.FormatTransition(request.Action, transition);
            return true;
        }
        catch (InvalidOperationException exception)
        {
            response = exception.Message;
            return false;
        }
    }

}
