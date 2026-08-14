using LabApi.Features.Wrappers;
using ReinforcementGate.Domain;

namespace ReinforcementGate.Interception;

/// <summary>Maps supported LabAPI wave wrappers to reinforcement targets.</summary>
public static class WaveClassifier
{
    /// <summary>Attempts to classify one LabAPI reinforcement wave.</summary>
    public static bool TryClassify(RespawnWave? wave, out ReinforcementTarget target)
    {
        switch (wave)
        {
            case MtfWave:
                target = ReinforcementTarget.Ntf;
                return true;
            case MiniMtfWave:
                target = ReinforcementTarget.NtfMini;
                return true;
            case ChaosWave:
                target = ReinforcementTarget.Ci;
                return true;
            case MiniChaosWave:
                target = ReinforcementTarget.CiMini;
                return true;
            default:
                target = default;
                return false;
        }
    }
}
