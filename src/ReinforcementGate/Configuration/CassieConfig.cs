namespace ReinforcementGate.Configuration;

/// <summary>Configures a CASSIE announcement notification.</summary>
public sealed class CassieConfig
{
    /// <summary>Gets or sets the spoken CASSIE template.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the subtitle template.</summary>
    public string Subtitles { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the announcement background is played.</summary>
    public bool PlayBackground { get; set; } = true;

    /// <summary>Gets or sets the announcement priority.</summary>
    public float Priority { get; set; }

    /// <summary>Gets or sets the voice glitch scale in the inclusive range 0..1.</summary>
    public float GlitchScale { get; set; }
}
