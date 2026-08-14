using System;
using System.IO;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class ReadmeContractTests
{
    [Fact]
    public void Readme_documents_required_public_surface()
    {
        string readme = File.ReadAllText(Path.Combine(RepositoryRoot.Find(), "README.md"));
        string[] required =
        {
            "reinforcement", "rf", "ntf-mini", "ci-mini", "RespawnEvents",
            "ReinforcementStatesApi", "ReinforcementControlApi", "StateChanged",
            "Broadcast", "Cassie", "{target_name}", "LabAPI 1.1.7",
            "enable_applied", "disable_applied", "disabled_wave_blocked",
            "skip_armed", "skip_triggered",
        };

        foreach (string value in required)
            Assert.Contains(value, readme, StringComparison.Ordinal);
    }
}
