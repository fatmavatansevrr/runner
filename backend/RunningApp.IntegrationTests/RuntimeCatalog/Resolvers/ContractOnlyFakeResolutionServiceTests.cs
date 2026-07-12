using System.Collections.Generic;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Resolvers;

/// <summary>
/// Backend Integration Phase 4C — a contract-only fake implementation of
/// IRuntimeConditionResolutionService, used ONLY by
/// ContractOnlyFakeResolutionServiceTests below. This class exists solely to
/// prove the interface is implementable and produces a well-formed
/// ResolverDecisionTrace; it returns fixed, clearly-labeled placeholder
/// values and performs no real feasibility/pace/adequacy/readiness logic.
///
/// This class lives in the TEST PROJECT ONLY. It is never referenced by
/// RunningApp.Application, RunningApp.Api, or Program.cs, and is never
/// registered in the DI container -- it cannot be invoked from live
/// generate-preview/confirm flow.
/// </summary>
internal sealed class ContractOnlyFakeResolutionService : IRuntimeConditionResolutionService
{
    public ResolverDecisionTrace ResolveAll(RuntimeResolverContext context)
    {
        return new ResolverDecisionTrace
        {
            Steps = new[]
            {
                new ResolverDecisionTraceStep
                {
                    StepIndex = 0,
                    ResolverKey = "FAKE_CORE_ENTRY_READINESS_RESOLVER",
                    ConditionType = "CORE_ENTRY_READINESS_IN",
                    Status = RuntimeConditionResolutionStatus.Evaluated,
                    OutputValue = "READY",
                    ReasonCode = "FAKE_PLACEHOLDER_ALWAYS_READY",
                    FallbackApplied = true,
                    Warnings = new[] { "FAKE: contract-only placeholder, not a real decision." },
                },
                new ResolverDecisionTraceStep
                {
                    StepIndex = 1,
                    ResolverKey = "FAKE_TIME_ADEQUACY_RESOLVER",
                    ConditionType = "TIME_ADEQUACY_IN",
                    Status = RuntimeConditionResolutionStatus.Evaluated,
                    OutputValue = "ADEQUATE",
                    ReasonCode = "FAKE_PLACEHOLDER_ALWAYS_ADEQUATE",
                    FallbackApplied = true,
                    Warnings = new[] { "FAKE: contract-only placeholder, not a real decision." },
                },
                new ResolverDecisionTraceStep
                {
                    StepIndex = 2,
                    ResolverKey = "FAKE_PACE_SOURCE_RESOLVER",
                    ConditionType = "PACE_SOURCE_IN",
                    Status = RuntimeConditionResolutionStatus.Evaluated,
                    OutputValue = "NONE",
                    ReasonCode = "FAKE_PLACEHOLDER_ALWAYS_NONE",
                    FallbackApplied = true,
                    Warnings = new[] { "FAKE: contract-only placeholder, not a real decision." },
                },
                new ResolverDecisionTraceStep
                {
                    StepIndex = 3,
                    ResolverKey = "FAKE_GOAL_FEASIBILITY_RESOLVER",
                    ConditionType = "GOAL_FEASIBILITY_IN",
                    Status = RuntimeConditionResolutionStatus.Evaluated,
                    OutputValue = "NOT_REQUESTED",
                    ReasonCode = "FAKE_PLACEHOLDER_ALWAYS_NOT_REQUESTED",
                    FallbackApplied = true,
                    Metadata = new Dictionary<string, string> { ["aggressivenessBand"] = "N/A" },
                    Warnings = new[] { "FAKE: contract-only placeholder, not a real decision." },
                },
            },
        };
    }
}

public sealed class ContractOnlyFakeResolutionServiceTests
{
    [Fact]
    public void ContractOnlyFakeResolutionService_ProducesFourStepTrace_AllFallbackApplied()
    {
        IRuntimeConditionResolutionService service = new ContractOnlyFakeResolutionService();

        var trace = service.ResolveAll(new RuntimeResolverContext { InputSnapshot = new ResolverInputSnapshot() });

        Assert.Equal(4, trace.Steps.Count);
        Assert.All(trace.Steps, step =>
        {
            Assert.True(step.FallbackApplied);
            Assert.Contains("FAKE:", step.Warnings[0]);
        });
    }
}
