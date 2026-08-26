using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.Progression;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

/// <summary>
/// Phase 4K.5A governance diagnostic. It invokes the real, unchanged Core
/// pipeline with checkpoint-derived values placed at its existing evidence
/// input seam. It is not a rolling-runtime implementation or production seam.
/// </summary>
public sealed class LongHorizonCoreWeekOneEvidenceAuthorityDiagnosticTests
{
    private static readonly DateOnly StartDate = new(2026, 8, 3);
    private static readonly DateOnly RaceDate = StartDate.AddDays(20 * 7);
    private static readonly IReadOnlyList<Weekday> PreferredWeekdays =
        [Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun];
    private static readonly IReadOnlyList<DayOfWeek> PreferredDays =
        [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday];

    public enum PaceCase { ProductAverage, RecentRace, ExplicitTarget }

    public static IEnumerable<object?[]> Phase4K8ADirectionMatrix()
    {
        var rows = new (double Weekly, double Below, double Equal, double Above)[]
        {
            (15, 4, 5, 7), (18, 5, 6, 8), (20, 5, 6, 9),
            (24, 6, 8, 10), (30, 8, 10, 12), (38, 10, 12.5, 16),
        };
        var frequencies = new int?[] { 2, 3, 4, null };
        var paces = Enum.GetValues<PaceCase>();
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            yield return [row.Weekly, row.Below, frequencies[index % frequencies.Length], paces[index % paces.Length], "BELOW"];
            yield return [row.Weekly, row.Equal, frequencies[(index + 1) % frequencies.Length], paces[(index + 1) % paces.Length], "EQUAL"];
            yield return [row.Weekly, row.Above, frequencies[(index + 2) % frequencies.Length], paces[(index + 2) % paces.Length], "ABOVE"];
        }
    }

    private sealed record DiagnosticResult(
        double CoreWeekOneWeeklyKm,
        double CoreWeekOneLongRunKm,
        string ReadinessValue,
        PrescriptionPaceSource PaceSource,
        PreparationRunwayCoreWeekOneNumericTarget Target);

    [Theory]
    [InlineData(15, 5, 2, PaceCase.ProductAverage, 15, 5, "CAUTION")]
    [InlineData(24, 9, 4, PaceCase.RecentRace, 24, 8, "READY")]
    [InlineData(30, 12, 3, PaceCase.ExplicitTarget, 30, 10, "READY")]
    [InlineData(20, 5, 4, PaceCase.ProductAverage, 20, 6, "CAUTION")]
    [InlineData(18, 7, 4, PaceCase.RecentRace, 18, 6, "READY")]
    [InlineData(38, 16, 4, PaceCase.ExplicitTarget, 38, 12.5, "READY")]
    public async Task CurrentValidatedEvidence_ThroughExistingCoreGenerator_ProducesSupportedWeekOneTarget(
        double validatedWeeklyKm,
        double validatedLongRunKm,
        int actualCompletedRunsPerWeek,
        PaceCase paceCase,
        double expectedWeeklyKm,
        double expectedLongRunKm,
        string expectedReadiness)
    {
        var result = await GenerateAsync(validatedWeeklyKm, validatedLongRunKm, actualCompletedRunsPerWeek, paceCase);

        Assert.Equal(expectedWeeklyKm, result.CoreWeekOneWeeklyKm);
        Assert.Equal(expectedLongRunKm, result.CoreWeekOneLongRunKm);
        Assert.Equal(expectedReadiness, result.ReadinessValue);
        Assert.Equal(validatedWeeklyKm, result.CoreWeekOneWeeklyKm);
        Assert.InRange(result.CoreWeekOneLongRunKm / result.CoreWeekOneWeeklyKm, 0.30, 0.40);
        Assert.Equal(result.Target.WeeklyVolumeKm, result.Target.OrderedSlots.Sum(s => s.DistanceKm));
    }

    [Theory]
    [InlineData(15, 5, 24, 9, -9)]
    [InlineData(30, 12, 24, 9, 6)]
    [InlineData(24, 9, 24, 9, 0)]
    public async Task CurrentEvidence_ReplacesRatherThanBlendsOnboardingEvidence(
        double currentWeekly,
        double currentLongRun,
        double onboardingWeekly,
        double onboardingLongRun,
        double expectedTargetDeltaFromLegacy)
    {
        var current = await GenerateAsync(currentWeekly, currentLongRun, 4, PaceCase.ProductAverage);
        var legacy = await GenerateAsync(onboardingWeekly, onboardingLongRun, 4, PaceCase.ProductAverage);

        Assert.Equal(expectedTargetDeltaFromLegacy, current.CoreWeekOneWeeklyKm - legacy.CoreWeekOneWeeklyKm);
        Assert.Equal(currentWeekly, current.CoreWeekOneWeeklyKm);
        if (currentWeekly != onboardingWeekly)
            Assert.NotEqual((currentWeekly + onboardingWeekly) / 2d, current.CoreWeekOneWeeklyKm);
    }

    [Fact]
    public async Task PaceAndTargetTimeSources_DoNotChangeVolumeAuthority()
    {
        var productAverage = await GenerateAsync(24, 9, 4, PaceCase.ProductAverage);
        var recentRace = await GenerateAsync(24, 9, 4, PaceCase.RecentRace);
        var explicitTarget = await GenerateAsync(24, 9, 4, PaceCase.ExplicitTarget);

        Assert.Equal(productAverage.CoreWeekOneWeeklyKm, recentRace.CoreWeekOneWeeklyKm);
        Assert.Equal(productAverage.CoreWeekOneWeeklyKm, explicitTarget.CoreWeekOneWeeklyKm);
        Assert.Equal(productAverage.CoreWeekOneLongRunKm, recentRace.CoreWeekOneLongRunKm);
        Assert.Equal(productAverage.CoreWeekOneLongRunKm, explicitTarget.CoreWeekOneLongRunKm);
        // The unchanged resolver owns the three pace classifications. This
        // governance diagnostic only proves none can alter volume authority.
        Assert.True(Enum.IsDefined(productAverage.PaceSource));
        Assert.True(Enum.IsDefined(recentRace.PaceSource));
        Assert.True(Enum.IsDefined(explicitTarget.PaceSource));
    }

    [Fact]
    public async Task ActualFrequencyAndFourDayAvailability_RemainDistinctAuthorities()
    {
        var twoCompletedRuns = await GenerateAsync(20, 5, 2, PaceCase.ProductAverage);
        var fourCompletedRuns = await GenerateAsync(20, 5, 4, PaceCase.ProductAverage);

        Assert.Equal(twoCompletedRuns.CoreWeekOneWeeklyKm, fourCompletedRuns.CoreWeekOneWeeklyKm);
        Assert.Equal(4, PreferredDays.Count);
        Assert.Equal(twoCompletedRuns.Target.WeeklyVolumeKm, twoCompletedRuns.Target.OrderedSlots.Sum(s => s.DistanceKm));
    }

    [Fact]
    public void AuthorityCatalog_ClosesRollingSourceWithoutChangingLegacyProductionMetadata()
    {
        Assert.Equal(LongHorizonEvidenceSource.CompletedTrainingHistory,
            LongHorizonEvidenceAuthorityCatalog.CoreWeekOneRollingAuthority.Source);
        Assert.Equal(LongHorizonEvidenceAuthorityStatus.Authoritative,
            LongHorizonEvidenceAuthorityCatalog.CoreWeekOneRollingAuthority.AuthorityStatus);
        Assert.Equal(LongHorizonEvidenceSource.OriginalOnboardingEvidence,
            LongHorizonEvidenceAuthorityCatalog.CoreWeekOneCurrentProductionSource.Source);
        Assert.Equal(LongHorizonEvidenceAuthorityStatus.LegacyCurrentProductionSource,
            LongHorizonEvidenceAuthorityCatalog.CoreWeekOneCurrentProductionSource.AuthorityStatus);
    }

    [Theory]
    [MemberData(nameof(Phase4K8ADirectionMatrix))]
    public async Task Phase4K8A_RealDirectionMatrix_SeparatesWeeklyAlignmentFromLongRunNormalization(
        double weekly, double rawLongRun, int? exactFrequency, PaceCase paceCase, string expectedLongRunRelation)
    {
        var generated = await GenerateAsync(weekly, rawLongRun, exactFrequency, paceCase);
        Assert.Equal(weekly, generated.CoreWeekOneWeeklyKm);
        var actualRelation = rawLongRun.CompareTo(generated.CoreWeekOneLongRunKm) switch
        {
            < 0 => "BELOW",
            0 => "EQUAL",
            > 0 => "ABOVE",
        };
        Assert.Equal(expectedLongRunRelation, actualRelation);

        foreach (PreparationRunwayAllocationProfile profile in Enum.GetValues<PreparationRunwayAllocationProfile>())
        {
            var runway = PreparationRunwayNumericMaterializer.Materialize(
                PreparationRunwayNumericMaterializerTests.Request(profile, 8,
                    PreparationRunwayNumericMaterializerTests.Evidence(
                        PreparationRunwayLoadEvidenceState.Provided, weekly,
                        PreparationRunwayLoadEvidenceState.Provided, rawLongRun),
                    generated.Target));
            if (weekly == 24 && rawLongRun == 6)
            {
                Assert.False(runway.IsSuccess);
                Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.LongRunShareViolation, runway.FailureCode);
                Assert.Null(runway.PrescribedWeeks);
            }
            else
            {
                Assert.True(runway.IsSuccess, runway.FailureReason);
                Assert.All(runway.PrescribedWeeks!, week => Assert.Equal(weekly, week.PlannedWeeklyVolumeKm));
                Assert.All(runway.PrescribedWeeks!.Skip(1), week => Assert.True(week.NumericTrace.WeeklyChangeKm >= 0));
            }
        }
    }

    private static async Task<DiagnosticResult> GenerateAsync(
        double weeklyKm,
        double longRunKm,
        int? runsPerWeek,
        PaceCase paceCase)
    {
        var targetSeconds = paceCase == PaceCase.ProductAverage ? 3480 : 3000;
        var targetSource = paceCase switch
        {
            PaceCase.ProductAverage => TargetFinishTimeSource.ProductAverage,
            PaceCase.ExplicitTarget => TargetFinishTimeSource.UserDefined,
            _ => (TargetFinishTimeSource?)null,
        };
        var recentRace = paceCase == PaceCase.RecentRace
            ? new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = StartDate.AddDays(-21) }
            : null;

        var preview = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            Level = RunningBackground.Intermediate,
            DaysPerWeek = 4,
            Unit = DistanceUnit.Km,
            StartDate = StartDate,
            RaceDate = RaceDate,
            TargetFinishTimeSeconds = targetSeconds,
            TargetFinishTimeSource = targetSource,
            PreferredDays = PreferredWeekdays,
            LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = weeklyKm,
            RecentLongestRunKm = longRunKm,
            RecentRunsPerWeek = runsPerWeek,
            RecentRace = recentRace,
        };
        var resolver = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK,
            GoalDistanceKm = 10,
            StartDate = StartDate,
            RaceDate = RaceDate,
            TargetFinishTimeSeconds = targetSeconds,
            TargetFinishTimeSource = targetSource,
            DaysPerWeek = 4,
            PreferredDays = PreferredWeekdays,
            LongRunDay = Weekday.Sun,
            Level = RunningBackground.Intermediate,
            RecentWeeklyVolumeKm = weeklyKm,
            RecentLongestRunKm = longRunKm,
            RecentRunsPerWeek = runsPerWeek,
            RecentRaceDistanceKm = recentRace is null ? null : 10,
            RecentRaceFinishTimeSeconds = recentRace?.FinishTimeSeconds,
            RecentRaceDate = recentRace?.RaceDate,
        };

        var options = Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() });
        var candidate = await new CatalogCandidateEligibilityGate(
                new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance))
            .LoadForInternalDryRunAsync(V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
        var conditions = new RuntimeConditionResolutionService(
                new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver())
            .ResolveAllResults(new RuntimeResolverContext { InputSnapshot = resolver, CoreCycle = candidate.CoreCycle, AsOfDate = StartDate });

        var corePipeline = new DynamicCoreCalendarMaterializationOrchestrator(
            new DynamicCoreSessionPrescriptionOrchestrator(
                new DynamicCoreVolumeAndLongRunOrchestrator(
                    new DynamicCoreWorkoutBindingOrchestrator(
                        new DynamicCoreWeekSkeletonOrchestrator(
                            new CatalogPhaseAllocationResolver(), new CatalogRunLayoutResolver(),
                            new CatalogStageToWeekMaterializer(), new GeneratedCatalogPlanSkeletonValidator()),
                        new CatalogWorkoutProgressionLoader(options), new ProgressionStageAllocator(),
                        new GeneratedCatalogStageScheduleValidator(), new CatalogWeekSkeletonCalendarMaterializer(),
                        new DatedGeneratedCatalogPlanSkeletonValidator(), new CatalogWorkoutBinder(), new BoundCatalogPlanValidator()),
                    new CatalogPrescriptionContextBuilder(), new CatalogVolumeAndLongRunPlanner()),
                new CatalogSessionPrescriptionPlanner(), new CatalogFinalPrescribedPlanFinalizer()));
        var generator = new TenKPreparationRunwayCoreGenerator(
            corePipeline, new CatalogWorkoutDefinitionLoader(options), new CatalogPeakVolumeBandLoader(options));
        var core = await generator.GenerateAsync(new TenKPreparationRunwayCoreGenerationRequest(
            candidate, StartDate.AddDays(8 * 7), RaceDate, StartDate, PreferredDays, DayOfWeek.Sunday,
            conditions, preview, resolver), CancellationToken.None);

        var volumePlan = core.PrescriptionResult.VolumeResult.VolumeAndLongRunPlan;
        var target = PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior(volumePlan, core.PrescriptionResult.FinalPrescribedPlan);
        var readiness = conditions.Single(r => r.ConditionType == CoreEntryReadinessResolver.ConditionTypeValue);
        return new DiagnosticResult(
            target.WeeklyVolumeKm,
            target.LongRunDistanceKm,
            readiness.OutputValue!,
            core.PrescriptionResult.VolumeResult.PrescriptionContext.PaceSource.Source,
            target);
    }

    private static string CatalogRoot() => Path.Combine(
        RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
}
