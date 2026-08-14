namespace ReinforcementGate.Configuration;

/// <summary>Configures a server broadcast notification.</summary>
public sealed class BroadcastConfig
{
    /// <summary>Gets or sets the broadcast template.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the display duration in seconds.</summary>
    public ushort Duration { get; set; } = 8;

    /// <summary>Gets or sets whether existing broadcasts are cleared first.</summary>
    public bool ClearPrevious { get; set; }
}
