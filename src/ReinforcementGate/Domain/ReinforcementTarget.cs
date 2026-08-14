namespace ReinforcementGate.Domain;

/// <summary>Identifies a controllable reinforcement wave category.</summary>
public enum ReinforcementTarget
{
    /// <summary>Targets every reinforcement wave category.</summary>
    All,

    /// <summary>Targets standard Nine-Tailed Fox waves.</summary>
    Ntf,

    /// <summary>Targets mini Nine-Tailed Fox waves.</summary>
    NtfMini,

    /// <summary>Targets standard Chaos Insurgency waves.</summary>
    Ci,

    /// <summary>Targets mini Chaos Insurgency waves.</summary>
    CiMini,
}
