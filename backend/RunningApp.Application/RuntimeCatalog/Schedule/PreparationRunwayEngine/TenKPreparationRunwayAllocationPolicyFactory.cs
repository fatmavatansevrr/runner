using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;

namespace RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;

/// <summary>
/// The two approved V1 Preparation Runway profiles (Phase 4G.6A.2, corrected
/// Phase 4G.6A.3B). Profile SELECTION (which profile a real runner falls
/// into) already belongs to readiness/orchestration policy
/// (<c>CoreEntryReadinessResolver</c>) and is not duplicated here.
/// </summary>
internal enum PreparationRunwayAllocationProfile
{
    ConsistencyNeeded,
    CoreEntryReady,
}

/// <summary>
/// Narrow, dark, production-owned policy factory binding the generic engine
/// to the real <see cref="PreparationRunwayBlockType"/> vocabulary (reused,
/// not duplicated -- see PreparationRunwayContracts.cs) and the exact
/// Phase 4G.6A.3B-corrected policy metadata for TEN_K__4D__INTERMEDIATE
/// v10. Every conditional block's true mandatory participation is now
/// expressed honestly via its own declared MinWeeks (1 when eligible, for
/// both CONSISTENCY and AEROBIC_STRENGTH) rather than relying on an engine
/// heuristic -- Phase 4G.6A.3's own MinWeeks=0 declarations were an
/// under-specification the engine silently compensated for; this factory
/// corrects the declaration instead.
///
/// This factory does not inspect raw recent-running evidence,
/// RunningBackground, RaceDate, StartDate, or CoreEntryReadiness inputs --
/// it consumes an already-resolved <see cref="PreparationRunwayAllocationProfile"/>
/// and does not duplicate CoreEntryReadinessResolver's own logic.
/// </summary>
internal static class TenKPreparationRunwayAllocationPolicyFactory
{
    public static IReadOnlyList<PreparationRunwayBlockAllocationPolicy<PreparationRunwayBlockType>> BuildPolicies(PreparationRunwayAllocationProfile profile)
    {
        var consistencyEligible = profile == PreparationRunwayAllocationProfile.ConsistencyNeeded;
        var aerobicStrengthEligible = profile == PreparationRunwayAllocationProfile.CoreEntryReady;
        var generalEnduranceWeight = profile == PreparationRunwayAllocationProfile.ConsistencyNeeded ? 0.60 : 0.65;

        return new[]
        {
            new PreparationRunwayBlockAllocationPolicy<PreparationRunwayBlockType>(
                PreparationRunwayBlockType.Consistency, consistencyEligible,
                MinWeeks: consistencyEligible ? 1 : 0, MaxWeeks: 2,
                PreferredWeight: 0.40, AllocationPriority: 3, CanonicalOrder: 1, IsExpandable: true),
            new PreparationRunwayBlockAllocationPolicy<PreparationRunwayBlockType>(
                PreparationRunwayBlockType.GeneralEndurance, IsEligible: true, MinWeeks: 1, MaxWeeks: 5,
                PreferredWeight: generalEnduranceWeight, AllocationPriority: 4, CanonicalOrder: 2, IsExpandable: true),
            new PreparationRunwayBlockAllocationPolicy<PreparationRunwayBlockType>(
                PreparationRunwayBlockType.AerobicStrength, aerobicStrengthEligible,
                MinWeeks: aerobicStrengthEligible ? 1 : 0, MaxWeeks: 2,
                PreferredWeight: 0.35, AllocationPriority: 2, CanonicalOrder: 3, IsExpandable: true),
            new PreparationRunwayBlockAllocationPolicy<PreparationRunwayBlockType>(
                PreparationRunwayBlockType.PreSpecificTransition, IsEligible: true, MinWeeks: 1, MaxWeeks: 1,
                PreferredWeight: 0, AllocationPriority: 1, CanonicalOrder: 4, IsExpandable: false),
        };
    }
}
