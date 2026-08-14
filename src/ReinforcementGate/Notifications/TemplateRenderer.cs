using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ReinforcementGate.Domain;

namespace ReinforcementGate.Notifications;

/// <summary>Safely expands the public notification template tokens.</summary>
public static class TemplateRenderer
{
    private static readonly Regex BraceTokenPattern =
        new(@"\{[^{}]+\}", RegexOptions.CultureInvariant);

    /// <summary>Renders a notification template without removing unknown brace tokens.</summary>
    public static TemplateRenderResult Render(string template, NotificationContext context)
    {
        if (template is null)
            throw new ArgumentNullException(nameof(template));
        if (context is null)
            throw new ArgumentNullException(nameof(context));

        Dictionary<string, string> replacements = new(StringComparer.Ordinal)
        {
            ["{target}"] = ReinforcementTargetNames.ToCommandName(context.Target),
            ["{target_name}"] = context.TargetName,
            ["{admin}"] = context.Admin,
            ["{action}"] = context.Action,
            ["{reason}"] = context.Reason,
        };
        List<string> unknownTokens = new();
        HashSet<string> seenUnknownTokens = new(StringComparer.Ordinal);

        string rendered = BraceTokenPattern.Replace(
            template,
            match =>
            {
                if (replacements.TryGetValue(match.Value, out string? replacement))
                    return replacement;

                if (seenUnknownTokens.Add(match.Value))
                    unknownTokens.Add(match.Value);
                return match.Value;
            });

        return new TemplateRenderResult(rendered, unknownTokens);
    }
}
