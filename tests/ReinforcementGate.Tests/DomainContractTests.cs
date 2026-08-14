using System;
using System.Collections.Generic;
using ReinforcementGate.Domain;
using Xunit;

namespace ReinforcementGate.Tests;

public sealed class DomainContractTests
{
    [Fact]
    public void Snapshot_exposes_local_effective_and_skip_state()
    {
        ReinforcementTargetState ntf = new(
            ReinforcementTarget.Ntf,
            isLocallyEnabled: true,
            isEffectivelyEnabled: false,
            isSkipArmed: true,
            enabledLastChangedBy: "Admin A",
            skipLastChangedBy: "Admin B");

        ReinforcementStateSnapshot snapshot = new(
            isGlobalDisabled: true,
            isGlobalSkipArmed: false,
            globalDisabledLastChangedBy: "Admin C",
            globalSkipLastChangedBy: string.Empty,
            new Dictionary<ReinforcementTarget, ReinforcementTargetState>
            {
                [ReinforcementTarget.Ntf] = ntf,
            });

        Assert.True(snapshot.IsGlobalDisabled);
        Assert.False(snapshot.Targets[ReinforcementTarget.Ntf].IsEffectivelyEnabled);
        Assert.Equal("Admin B", snapshot.Targets[ReinforcementTarget.Ntf].SkipLastChangedBy);
        Assert.Throws<NotSupportedException>(() =>
            ((IDictionary<ReinforcementTarget, ReinforcementTargetState>)snapshot.Targets)
                .Add(ReinforcementTarget.Ci, ntf));
    }
}
