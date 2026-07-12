using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.3 — test-only fixtures. Two distinct
/// sources are used deliberately:
/// <list type="bullet">
/// <item><see cref="PilotCandidate"/>: a hand-built <see cref="PlanCatalogCandidateSummary"/>
/// for isolated resolver/factory unit tests — never touches a catalog file.</item>
/// <item><see cref="LoadRealPilotCandidateAsync"/>: loads the REAL, unmodified,
/// still-DRAFT <c>TEN_K__4D__INTERMEDIATE v10</c> candidate from the actual
/// plan-catalog source tree via the same read-only
/// <see cref="ICatalogCandidateEligibilityGate.LoadForInternalDryRunAsync"/>
/// entry point used throughout Phase 4E.1+ — for real, end-to-end
/// orchestration proof. Neither ever implies live public preview reachability.</item>
/// </list>
/// </summary>
internal static class CatalogPlanSkeletonOrchestratorFixtures
{
    public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
    public const int CandidateVersion = 10;

    public static readonly PlanCatalogReference PilotMasterTemplate = new("TEN_K_MASTER", 6);
    public static readonly PlanCatalogReference PilotLayout = new("RUN_LAYOUT_4D", 2);

    public static PlanCatalogCandidateSummary PilotCandidate(
        IReadOnlyList<PlanCatalogPhaseAllocation>? phaseAllocations = null,
        IReadOnlyList<string>? slotRoles = null,
        int? daysPerWeek = null,
        PlanCatalogReference? masterTemplate = null,
        PlanCatalogReference? layout = null,
        int? coreCycleDefaultWeeks = null)
    {
        var allocations = phaseAllocations ?? new[]
        {
            new PlanCatalogPhaseAllocation("FOUNDATION", 3),
            new PlanCatalogPhaseAllocation("BUILD", 4),
            new PlanCatalogPhaseAllocation("RACE_SPECIFIC", 4),
            new PlanCatalogPhaseAllocation("TAPER", 1),
        };
        var roles = slotRoles ?? new[] { "KEY_SESSION", "EASY_SUPPORT", "EASY_SUPPORT", "LONG_RUN" };
        var mt = masterTemplate ?? PilotMasterTemplate;
        var lay = layout ?? PilotLayout;

        return new PlanCatalogCandidateSummary
        {
            CandidateKey = CandidateKey,
            CandidateVersion = CandidateVersion,
            CandidateStatus = "DRAFT",
            DependencyStatuses = new Dictionary<string, string>
            {
                ["masterTemplate"] = "DRAFT", ["layout"] = "DRAFT", ["levelModifier"] = "DRAFT", ["rulePack"] = "DRAFT",
            },
            CanonicalDistanceFamily = "TEN_K",
            Level = "INTERMEDIATE",
            DaysPerWeek = daysPerWeek ?? 4,
            CoreCycle = new PlanCatalogCoreCycle(8, coreCycleDefaultWeeks ?? 12, 14),
            MasterTemplate = mt,
            Layout = lay,
            LevelModifier = new PlanCatalogReference("INTERMEDIATE_MODIFIER", 6),
            WorkoutProgression = new PlanCatalogReference("TEN_K_WORKOUT_PROGRESSION_V1", 5),
            ProgressionModifier = new PlanCatalogReference("INTERMEDIATE_PROGRESSION_MODIFIER", 1),
            RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 4),
            PeakVolumeBandPolicy = new PlanCatalogReference("PEAK_VOLUME_BAND_POLICY", 1),
            RuntimeConditionValueRegistry = new PlanCatalogReference("RUNTIME_CONDITION_VALUES_V1", 2),
            ReferencedWorkouts = Array.Empty<PlanCatalogReference>(),
            PhaseKeys = Array.Empty<string>(),
            PhaseAllocations = allocations,
            SlotRoles = roles,
        };
    }

    public static CatalogPlanSkeletonOrchestrationContext OrchestrationContext(
        PlanCatalogCandidateSummary? candidate = null,
        DateOnly? startDate = null,
        string? expectedCandidateKey = null,
        int? expectedCandidateVersion = null,
        PlanCatalogReference? expectedMasterTemplate = null,
        PlanCatalogReference? expectedRunLayout = null)
    {
        var c = candidate ?? PilotCandidate();
        var start = startDate ?? new DateOnly(2026, 8, 5);

        return new CatalogPlanSkeletonOrchestrationContext
        {
            Candidate = c,
            ExpectedCandidateKey = expectedCandidateKey ?? c.CandidateKey,
            ExpectedCandidateVersion = expectedCandidateVersion ?? c.CandidateVersion,
            ExpectedMasterTemplate = expectedMasterTemplate ?? c.MasterTemplate,
            ExpectedRunLayout = expectedRunLayout ?? c.Layout,
            StartDate = start,
            AsOfDate = start,
        };
    }

    public static CatalogPlanSkeletonOrchestrator RealOrchestrator() => new(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    /// <summary>
    /// Loads the REAL, unmodified <c>TEN_K__4D__INTERMEDIATE v10</c> candidate
    /// (still DRAFT) from the actual plan-catalog source tree, bypassing only
    /// the PUBLISHED-only status check (the documented, test-only dry-run
    /// entry point — never used by any live public request path).
    /// </summary>
    public static async System.Threading.Tasks.Task<PlanCatalogCandidateSummary> LoadRealPilotCandidateAsync()
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        var gate = new CatalogCandidateEligibilityGate(bundleLoader);

        return await gate.LoadForInternalDryRunAsync(CandidateKey, CandidateVersion);
    }
}
