using System;
using System.Collections.Generic;

namespace ReinforcementGate.Notifications;

/// <summary>Contains rendered notification text and unresolved template tokens.</summary>
public sealed class TemplateRenderResult
{
    /// <summary>Initializes a template rendering result.</summary>
    public TemplateRenderResult(string text, IEnumerable<string> unknownTokens)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        if (unknownTokens is null)
            throw new ArgumentNullException(nameof(unknownTokens));

        UnknownTokens = Array.AsReadOnly(new List<string>(unknownTokens).ToArray());
    }

    /// <summary>Gets the rendered text.</summary>
    public string Text { get; }

    /// <summary>Gets distinct unknown tokens in first-occurrence order.</summary>
    public IReadOnlyList<string> UnknownTokens { get; }
}
