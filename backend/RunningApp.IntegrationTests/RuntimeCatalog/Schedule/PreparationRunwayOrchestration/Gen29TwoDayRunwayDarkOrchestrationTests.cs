using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;

/// <summary>
/// Phase 10K-GEN.29 -- real dark end-to-end verification (no HTTP, no public
/// gate, no PostgreSQL persistence) that the full
/// <see cref="TenKPreparationRunwayDarkOrchestrator"/> pipeline now works for
/// Beginner and Intermediate 2D Preparation Runway, implementing GEN.28 §9
/// (Candidate C)/§10/§11/§12/§14 and this phase's frozen AerobicStrength
/// content decision. Covers the admission-gate widening (Beginner is
/// admitted only for 2D; Beginner 3D/4D remain excluded, per GEN.28 §10),
/// the numeric-policy dispatch (GEN.28 §11), calendar composition for a
/// 2-slot week and long-run clamp application (GEN.28 §12), and the
/// AerobicStrength Pattern-A/Pattern-B split observed inside a real,
/// complete orchestration. Reuses the exact same
/// <see cref="TenKPreparationRunwayDarkOrchestrator"/> every other frequency's
/// own dark test file already exercises -- no 2D-specific orchestrator
/// exists. See <c>Gen29TwoDayRunwayBlockRoleReconciliationTests</c> for the
/// isolated, single-block materializer-level coverage of the reconciliation
/// mechanism itself.
///
/// GEN.11's frozen 2D readiness authority (re-confirmed unmodified by this
/// phase, <see cref="CatalogVolumeAndLongRunPlanner"/> line ~200): missing or
/// explicit-zero recent-running evidence is <c>PRODUCT_INELIGIBLE</c> for 2D
/// at both levels, so every request built here supplies a real, positive
/// observed weekly volume and longest run -- never null/zero, unlike the
/// higher-frequency dark test files' own missing-readiness cases.
/// </summary>
public sealed class Gen29TwoDayRunwayDarkOrchestrationTests
{
    private const string RealPublishedBundleReleaseVersion = "1.1.0";
    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static readonly IReadOnlyList<DayOfWeek> TueSun = [DayOfWeek.Tuesday, DayOfWeek.Sunday];

    public static IEnumerable<object[]> BeginnerHorizonProfileMatrix =>
        Enumerable.Range(15, 6).SelectMany(weeks => new[] { "READY", "NOT_READY" }
            .Select(readiness => new object[] { weeks, readiness }));

    public static IEnumerable<object[]> IntermediateHorizonProfileMatrix => BeginnerHorizonProfileMatrix;

    // ── Admission gate (GEN.28 §10) ──────────────────────────────────────────

    [Theory]
    [MemberData(nameof(BeginnerHorizonProfileMatrix))]
    public async Task Beginner_TwoDay_AllHorizons_BothProfiles_OrchestratesSuccessfully(int totalWeeks, string readinessValue)
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks, readinessValue, weekly: 12d, longest: 5d);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");
        Assert.Equal(totalWeeks - 12, result.StructuralRunway!.Weeks!.Count);
        Assert.Equal(totalWeeks, result.CalendarComposition!.OrderedCombinedWeeks!.Count);
        Assert.True(result.FinalInvariants!.IsValid, string.Join(",", result.FinalInvariants.Findings));
        AssertEveryWeekIsValidTwoDayModelBShape(result.StructuralRunway.Weeks);
    }

    [Theory]
    [MemberData(nameof(IntermediateHorizonProfileMatrix))]
    public async Task Intermediate_TwoDay_AllHorizons_BothProfiles_OrchestratesSuccessfully(int totalWeeks, string readinessValue)
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayIntermediateCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Intermediate, totalWeeks, readinessValue, weekly: 14d, longest: 6d);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");
        Assert.Equal(totalWeeks - 12, result.StructuralRunway!.Weeks!.Count);
        Assert.Equal(totalWeeks, result.CalendarComposition!.OrderedCombinedWeeks!.Count);
        Assert.True(result.FinalInvariants!.IsValid, string.Join(",", result.FinalInvariants.Findings));
        AssertEveryWeekIsValidTwoDayModelBShape(result.StructuralRunway.Weeks);
    }

    [Fact]
    public async Task Beginner_ThreeDay_Runway_RemainsExcluded_NotWidenedByTheTwoDayFix()
    {
        // GEN.28 §10's own explicit constraint: widening admission for
        // Beginner must be narrow (2D only) -- Beginner x3D Runway was never
        // designed/approved and must remain excluded. IsSupportedPreparationRunwayCandidate
        // never recognizes ThreeDayBeginnerCandidateKey, so the orchestrator
        // fails closed at the very first identity check regardless of any
        // other request field.
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.ThreeDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.ThreeDayBeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks: 15, readinessValue: "READY", weekly: 12d, longest: 5d, daysPerWeek: 3, preferredDays: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(TenKPreparationRunwayOrchestrationFailureCode.CandidateNotSupported, result.Failure!.Code);
    }

    [Fact]
    public async Task Beginner_FourDay_Runway_RemainsExcluded_NotWidenedByTheTwoDayFix()
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.BeginnerCandidateKey, V1CatalogPilotIdentityPolicy.BeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks: 15, readinessValue: "READY", weekly: 20d, longest: 8d, daysPerWeek: 4, preferredDays: [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday]);
        var result = await Orchestrator().OrchestrateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(TenKPreparationRunwayOrchestrationFailureCode.CandidateNotSupported, result.Failure!.Code);
    }

    // ── Numeric-policy dispatch (GEN.28 §11) ─────────────────────────────────

    [Fact]
    public void NumericPolicyFactory_DispatchesBeginnerTwoDay_ToTheFrozenGen11Authority()
    {
        var candidate = MinimalCandidate("TEN_K__2D__BEGINNER", "NEW", 2);
        var policy = TenKPreparationRunwayNumericPolicyFactory.Build(candidate);
        Assert.Equal(0.55d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.60d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.55d, policy.LongRunSelectionShare);
        Assert.Equal(0.60d, policy.LongRunHardCapShare);
    }

    [Fact]
    public void NumericPolicyFactory_DispatchesIntermediateTwoDay_ToTheFrozenGen11Authority()
    {
        var candidate = MinimalCandidate("TEN_K__2D__INTERMEDIATE", "INTERMEDIATE", 2);
        var policy = TenKPreparationRunwayNumericPolicyFactory.Build(candidate);
        Assert.Equal(0.55d, policy.LongRunPreferredMinimumShare);
        Assert.Equal(0.60d, policy.LongRunPreferredMaximumShare);
        Assert.Equal(0.55d, policy.LongRunSelectionShare);
        Assert.Equal(0.60d, policy.LongRunHardCapShare);
    }

    [Fact]
    public void NumericPolicyFactory_UnrecognizedCombination_StillFallsBackToDefault_ZeroDelta()
    {
        // Zero-delta guard: an unmapped (family, level, daysPerWeek) tuple
        // must still resolve to the pre-existing Default policy, exactly as
        // before this phase's new 2D branches were added.
        var candidate = MinimalCandidate("SOME_OTHER_CANDIDATE", "INTERMEDIATE", 99);
        var policy = TenKPreparationRunwayNumericPolicyFactory.Build(candidate);
        Assert.Equal(VolumeSafetyPolicy.Default.LongRunPreferredMinimumShare, policy.LongRunPreferredMinimumShare);
    }

    // ── Calendar composition / long-run clamp for a real 2-slot week (GEN.28 §12) ──

    [Theory]
    [InlineData(15)]
    [InlineData(20)]
    public async Task Beginner_TwoDay_CalendarComposition_TwoSlotWeeks_AndLongRunClampApplied(int totalWeeks)
    {
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, totalWeeks, "READY", weekly: 12d, longest: 5d);
        var result = await Orchestrator().OrchestrateAsync(request);
        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");

        // Every real Runway week in the composed calendar has exactly 2
        // dated sessions (2D's own slot count), and every prescribed
        // Runway week's long-run share sits within the frozen 55%/60% band.
        var runwayWeeks = result.CalendarComposition!.OrderedCombinedWeeks!
            .Where(w => w.RunwayWeek is not null)
            .Select(w => w.RunwayWeek!)
            .ToArray();
        Assert.NotEmpty(runwayWeeks);
        Assert.All(runwayWeeks, w => Assert.Equal(2, w.ChronologicalSlots.Count));

        Assert.All(result.NumericRunway!.PrescribedWeeks!, w =>
        {
            var share = w.PlannedLongRunDistanceKm / w.PlannedWeeklyVolumeKm;
            Assert.True(share >= 0.55d - 0.02d && share <= 0.60d + 0.02d,
                $"Week {w.StructuralWeek.RunwayWeekNumber}: long-run share {share:P2} outside the frozen 55/60% band.");
        });
    }

    // ── AerobicStrength Pattern-A/Pattern-B split observed end-to-end ────────

    [Fact]
    public async Task Beginner_TwoDay_Ready_Profile_AerobicStrengthBlock_ShowsPatternSplit_WhenBothPatternsOccur()
    {
        // READY profile => CoreEntryReady allocation profile => AerobicStrength
        // block eligible. Uses the maximal 20wk horizon (8 runway weeks) to
        // maximize the chance AerobicStrength's own allocation spans both a
        // Pattern-A and a Pattern-B week in one real run.
        var candidate = await LoadCandidateAsync(V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey, V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateVersion);
        var request = await BuildRequestAsync(candidate, RunningBackground.Beginner, 20, "READY", weekly: 12d, longest: 5d);
        var result = await Orchestrator().OrchestrateAsync(request);
        Assert.True(result.IsSuccess, $"{result.Failure?.Stage}/{result.Failure?.Code}: {result.Failure?.Reason}");

        var aerobicWeeks = result.StructuralRunway!.Weeks!.Where(w => w.BlockType == PreparationRunwayBlockType.AerobicStrength).ToArray();
        Assert.NotEmpty(aerobicWeeks); // profile=READY => the block is eligible with MinWeeks=1

        foreach (var week in aerobicWeeks)
        {
            var anchor = week.OrderedWorkoutSlots.Single(s => s.SourceKind == PreparationRunwayWorkoutSlotSource.Anchor);
            var isPatternA = week.OrderedWorkoutSlots.Any(s => s.SlotRole == PreparationRunwaySlotRole.KeySession);
            if (isPatternA)
            {
                Assert.Equal(PreparationRunwaySlotRole.KeySession, anchor.SlotRole);
                Assert.StartsWith("AEROBIC_STRENGTH_CONTROLLED", anchor.WorkoutId, StringComparison.Ordinal);
            }
            else
            {
                Assert.Equal(PreparationRunwaySlotRole.EasySupport, anchor.SlotRole);
                Assert.Equal("EASY_STANDARD", anchor.WorkoutId);
            }
        }
    }

    // ── Zero-delta for pre-existing frequencies (Advanced-shape sanity, cheap re-check) ──

    [Fact]
    public void BuildBlockRolePolicies_ZeroDelta_AcrossAllPreExistingDaysPerWeek()
    {
        // GEN.29 removed BuildBlockRolePolicies' daysPerWeek branch entirely
        // (it always returns the same, frequency-independent policy set) --
        // this directly confirms 3D/4D/5D/6D still resolve to the exact same
        // anchor-role dictionaries as each other and as 2D, i.e. nothing
        // about the *policy* changed for any frequency; the real behavior
        // change is isolated entirely inside PreparationRunwayWeekMaterializer's
        // new role-conditioned redirection, which is a no-op whenever
        // weekRoles already contains the fixed anchor role (true for every
        // non-2D layout by construction).
        var reference = TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(4);
        foreach (var daysPerWeek in new[] { 2, 3, 5, 6 })
        {
            var policies = TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(daysPerWeek);
            Assert.Equal(reference.Count, policies.Count);
            foreach (var block in reference)
            {
                var other = policies.Single(p => p.BlockKey == block.BlockKey);
                Assert.Equal(block.AnchorRoleByProgressionStep, other.AnchorRoleByProgressionStep);
                Assert.Equal(block.CanonicalOrder, other.CanonicalOrder);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AssertEveryWeekIsValidTwoDayModelBShape(IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>> weeks) =>
        Assert.All(weeks, week =>
        {
            var roles = week.OrderedWorkoutSlots.Select(s => s.SlotRole).ToArray();
            Assert.True(PreparationRunwayWeeklyShape.IsValidTwoDayModelB(roles),
                $"Runway week {week.RunwayWeekNumber} is not a valid 2D Model B shape: [{string.Join(",", roles)}]");
        });

    private static PlanCatalogCandidateSummary MinimalCandidate(string key, string level, int daysPerWeek) => new()
    {
        CandidateKey = key,
        CandidateVersion = 1,
        CandidateStatus = "VALIDATED",
        CanonicalDistanceFamily = "TEN_K",
        Level = level,
        DaysPerWeek = daysPerWeek,
        CoreCycle = new PlanCatalogCoreCycle(8, 12, 20),
        MasterTemplate = new PlanCatalogReference("TEN_K_MASTER", 11),
        Layout = new PlanCatalogReference("RUN_LAYOUT_2D", 1),
        LevelModifier = new PlanCatalogReference(level == "NEW" ? "BEGINNER_MODIFIER" : "INTERMEDIATE_MODIFIER", 1),
        WorkoutProgression = new PlanCatalogReference("UNUSED", 1),
        ProgressionModifier = new PlanCatalogReference("UNUSED", 1),
        RulePack = new PlanCatalogReference("APPSEL_RACE_PLAN_V1", 8),
        PeakVolumeBandPolicy = new PlanCatalogReference("UNUSED", 1),
        RuntimeConditionValueRegistry = new PlanCatalogReference("UNUSED", 1),
        DependencyStatuses = new Dictionary<string, string>(),
        ReferencedWorkouts = [],
        PhaseKeys = [],
        PhaseAllocations = [],
        SlotRoles = [],
    };

    private static TenKPreparationRunwayDarkOrchestrator Orchestrator() =>
        TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions
        {
            CatalogRootPath = CatalogRoot,
            PublishedBundleReleaseVersion = RealPublishedBundleReleaseVersion,
        });

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync(string key, int version)
    {
        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(key, version);
    }

    private static async Task<TenKPreparationRunwayDarkOrchestrationRequest> BuildRequestAsync(
        PlanCatalogCandidateSummary candidate, RunningBackground level, int totalWeeks, string readinessValue,
        double weekly, double longest, int daysPerWeek = 2, IReadOnlyList<DayOfWeek>? preferredDays = null)
    {
        var days = preferredDays ?? TueSun;
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(totalWeeks * 7);
        var longRunDay = DayOfWeek.Sunday;

        int? targetSeconds = 3600;
        var weekdays = days.Select(ToWeekday).ToArray();
        var preview = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = level,
            DaysPerWeek = daysPerWeek, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = null,
            PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay),
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = daysPerWeek,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3600, RaceDate = start.AddDays(-21) },
        };
        var resolver = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = targetSeconds, TargetFinishTimeSource = null,
            DaysPerWeek = daysPerWeek, PreferredDays = weekdays, LongRunDay = ToWeekday(longRunDay), Level = level,
            RecentWeeklyVolumeKm = weekly, RecentLongestRunKm = longest, RecentRunsPerWeek = daysPerWeek,
            RecentRaceDistanceKm = 10,
            RecentRaceFinishTimeSeconds = 3600,
            RecentRaceDate = start.AddDays(-21),
        };
        var readiness = RuntimeConditionResolutionResult.Evaluated(
            CoreEntryReadinessResolver.ConditionTypeValue,
            readinessValue,
            readinessValue == "READY" ? "CORE_ENTRY_READY" : "CORE_ENTRY_NOT_READY");
        var conditions = new List<RuntimeConditionResolutionResult>
        {
            readiness,
            RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, "REALISTIC", "WITHIN_REALISTIC_BAND"),
        };
        return new TenKPreparationRunwayDarkOrchestrationRequest(
            candidate, start, race, start, days, longRunDay, readiness, conditions,
            preview, resolver, PreparationRunwayQuantityUnit.Kilometers);
    }

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon,
        DayOfWeek.Tuesday => Weekday.Tue,
        DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu,
        DayOfWeek.Friday => Weekday.Fri,
        DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun,
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };
}
