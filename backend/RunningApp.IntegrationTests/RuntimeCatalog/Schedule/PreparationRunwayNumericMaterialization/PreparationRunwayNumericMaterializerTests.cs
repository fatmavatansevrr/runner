using System.Text.Json;
using System.Text.RegularExpressions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Prescription.Execution;
using RunningApp.Application.RuntimeCatalog.Prescription.Session;
using RunningApp.Application.RuntimeCatalog.Prescription.Volume;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayEngine;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;

public sealed class PreparationRunwayNumericMaterializerTests
{
    public static IEnumerable<object[]> ProfileAndLengthMatrix()
    {
        foreach (PreparationRunwayAllocationProfile profile in Enum.GetValues<PreparationRunwayAllocationProfile>())
            foreach (var weeks in Enumerable.Range(3, 6))
                yield return [profile, weeks];
    }

    [Fact]
    public async Task CoreWeekOneTarget_IsReadFromCurrentAuthoritativeRepositoryBehavior()
    {
        var context = await SafetyVerificationContextFixtures.RealAsync(12, "REALISTIC");
        var plan = new CatalogVolumeAndLongRunPlan(context.VolumePlan, context.LongRunPlan);

        var target = PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior(plan, SingleKeySessionFirstWeekPlan());

        Assert.Equal("TEN_K__4D__INTERMEDIATE", target.CandidateKey);
        Assert.Equal(10, target.CandidateVersion);
        Assert.Equal(24d, target.WeeklyVolumeKm);
        Assert.Equal(8d, target.LongRunDistanceKm);
        Assert.Equal(new[] { 8d, 4d, 4d, 8d }, target.OrderedSlots.Select(s => s.DistanceKm));
        Assert.Contains("CatalogVolumeAndLongRunPlanner", target.SourceProvenance);
    }

    [Theory]
    [MemberData(nameof(ProfileAndLengthMatrix))]
    internal void BothProfiles_AllThreeToEightWeekMatrices_EndExactlyAtCore(
        PreparationRunwayAllocationProfile profile, int runwayWeeks)
    {
        var request = Request(profile, runwayWeeks,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 24,
                PreparationRunwayLoadEvidenceState.Provided, 9), Target(24, 8));

        var result = PreparationRunwayNumericMaterializer.Materialize(request);

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(runwayWeeks, result.PrescribedWeeks!.Count);
        Assert.All(result.PrescribedWeeks, AssertWeekInvariants);
        Assert.All(result.PrescribedWeeks, week => Assert.Equal(24d, week.PlannedWeeklyVolumeKm));
        Assert.True(result.ContinuityAnalysis!.IsWithinTolerance);
        Assert.Equal(0d, result.ContinuityAnalysis.WeeklyVolumeChangeKm);
        Assert.Equal(0d, result.ContinuityAnalysis.LongRunChangeKm);
    }

    [Theory]
    [MemberData(nameof(ProfileAndLengthMatrix))]
    internal void BothProfiles_AllThreeToEightWeekMatrices_AlsoProveFeasibleBoundedDevelopment(
        PreparationRunwayAllocationProfile profile, int runwayWeeks)
    {
        var feasibleStart = runwayWeeks switch
        {
            3 => 22.5d,
            4 => 21d,
            5 => 20d,
            6 => 19d,
            7 => 18.5d,
            8 => 18d,
            _ => throw new ArgumentOutOfRangeException(nameof(runwayWeeks)),
        };
        var request = Request(profile, runwayWeeks,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, feasibleStart,
                PreparationRunwayLoadEvidenceState.Missing, null), Target(24, 8));

        var result = PreparationRunwayNumericMaterializer.Materialize(request);

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(feasibleStart, result.PrescribedWeeks![0].PlannedWeeklyVolumeKm);
        Assert.Equal(24d, result.PrescribedWeeks[^2].PlannedWeeklyVolumeKm);
        Assert.Equal(24d, result.PrescribedWeeks[^1].PlannedWeeklyVolumeKm);
        Assert.All(result.PrescribedWeeks, AssertWeekInvariants);
        Assert.All(result.PrescribedWeeks.Skip(1), week =>
        {
            Assert.InRange(week.NumericTrace.WeeklyChangeKm, 0d, 2.5d);
            Assert.True(week.NumericTrace.WeeklyChangeRatio <= 0.08d + 0.001d);
        });
        Assert.True(result.ContinuityAnalysis!.IsWithinTolerance);
    }

    [Fact]
    public void BoundedDevelopment_UsesHybridCurve_AndTransitionIsMaintenance()
    {
        var result = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 8,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 20,
                PreparationRunwayLoadEvidenceState.Provided, 6), Target(24, 8)));

        Assert.True(result.IsSuccess, result.FailureReason);
        var weeks = result.PrescribedWeeks!;
        Assert.Equal(20d, weeks[0].PlannedWeeklyVolumeKm);
        Assert.Equal(24d, weeks[^2].PlannedWeeklyVolumeKm);
        Assert.Equal(24d, weeks[^1].PlannedWeeklyVolumeKm);
        Assert.All(weeks.Skip(1), w => Assert.True(w.NumericTrace.WeeklyChangeKm <= 2.5d));
        Assert.All(weeks.Skip(1), w => Assert.True(w.NumericTrace.WeeklyChangeRatio <= 0.08d + 0.001d));
        Assert.Contains("maintenance bridge", weeks[^1].NumericTrace.TrajectoryRule);
        Assert.True(weeks.Select(w => w.PlannedLongRunDistanceKm).SequenceEqual(
            weeks.Select(w => w.PlannedLongRunDistanceKm).OrderBy(v => v)));
    }

    [Fact]
    public void InfeasibleShortRunway_FailsAtomicallyInsteadOfForcingCoreJump()
    {
        var result = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 20,
                PreparationRunwayLoadEvidenceState.Provided, 6), Target(24, 8)));

        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.WeeklyChangeLimitExceeded, result.FailureCode);
        Assert.Null(result.PrescribedWeeks);
        Assert.Null(result.ContinuityAnalysis);
    }

    [Fact]
    public void MissingEvidence_UsesRecordedDefault_AndIsNeverConvertedToZero()
    {
        var result = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Missing, null,
                PreparationRunwayLoadEvidenceState.Missing, null), Target(16, 5.5)));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(16d, result.PrescribedWeeks![0].PlannedWeeklyVolumeKm);
        Assert.Equal(5.5d, result.PrescribedWeeks[0].PlannedLongRunDistanceKm);
        Assert.DoesNotContain(result.Trace, line => line.Contains("starting_weekly=0", StringComparison.Ordinal));
    }

    [Fact]
    public void NoRecentRunningBase_UsesItsSeparateRecordedDefault()
    {
        var result = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            Evidence(PreparationRunwayLoadEvidenceState.NoRecentRunningBase, 0,
                PreparationRunwayLoadEvidenceState.NoRecentRunningBase, 0), Target(12, 4)));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(12d, result.PrescribedWeeks![0].PlannedWeeklyVolumeKm);
        Assert.Equal(4d, result.PrescribedWeeks[0].PlannedLongRunDistanceKm);
    }

    [Fact]
    public void ProvidedWeeklyOnly_AndProvidedLongestOnly_AreDeterministic()
    {
        var weeklyOnly = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.CoreEntryReady, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 20,
                PreparationRunwayLoadEvidenceState.Missing, null), Target(20, 6.5)));
        var longestOnly = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Missing, null,
                PreparationRunwayLoadEvidenceState.Provided, 5), Target(16, 5)));

        Assert.True(weeklyOnly.IsSuccess, weeklyOnly.FailureReason);
        Assert.Equal(20d, weeklyOnly.PrescribedWeeks![0].PlannedWeeklyVolumeKm);
        Assert.Equal(6.5d, weeklyOnly.PrescribedWeeks[0].PlannedLongRunDistanceKm);
        Assert.True(longestOnly.IsSuccess, longestOnly.FailureReason);
        Assert.Equal(16d, longestOnly.PrescribedWeeks![0].PlannedWeeklyVolumeKm);
        Assert.Equal(5d, longestOnly.PrescribedWeeks[0].PlannedLongRunDistanceKm);
    }

    [Fact]
    public void RecentLongestRun_IsBounded_NotCopiedBlindly()
    {
        var result = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.CoreEntryReady, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 20,
                PreparationRunwayLoadEvidenceState.Provided, 12), Target(20, 6.5)));

        Assert.True(result.IsSuccess, result.FailureReason);
        Assert.Equal(6.5d, result.PrescribedWeeks![0].PlannedLongRunDistanceKm);
        Assert.NotEqual(12d, result.PrescribedWeeks[0].PlannedLongRunDistanceKm);
    }

    [Fact]
    public void StartingAboveCore_FailsClosedBecauseNoReductionRuleExists()
    {
        var result = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.CoreEntryReady, 8,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 30,
                PreparationRunwayLoadEvidenceState.Provided, 10), Target(24, 8)));

        Assert.False(result.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.RunwayProgressionInfeasible, result.FailureCode);
        Assert.Null(result.PrescribedWeeks);
    }

    [Fact]
    public void RoundingResidual_IsDeterministic_AndSecondEasySupportOwnsIt()
    {
        var request = Request(PreparationRunwayAllocationProfile.CoreEntryReady, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 20.24,
                PreparationRunwayLoadEvidenceState.Missing, null), Target(20, 6.5));
        var first = PreparationRunwayNumericMaterializer.Materialize(request);
        var second = PreparationRunwayNumericMaterializer.Materialize(request);

        Assert.True(first.IsSuccess, first.FailureReason);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.All(first.PrescribedWeeks!, AssertWeekInvariants);
    }

    [Fact]
    public void BlockTrajectories_AreExplicitForEveryAllocatedBlockShape()
    {
        var consistency = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 8,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 24,
                PreparationRunwayLoadEvidenceState.Provided, 8), Target(24, 8)));
        var ready = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.CoreEntryReady, 8,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, 24,
                PreparationRunwayLoadEvidenceState.Provided, 8), Target(24, 8)));

        Assert.True(consistency.IsSuccess);
        Assert.True(ready.IsSuccess);
        Assert.Contains(consistency.PrescribedWeeks!, w => w.NumericTrace.TrajectoryRule.StartsWith("CONSISTENCY", StringComparison.Ordinal));
        Assert.Contains(consistency.PrescribedWeeks!, w => w.NumericTrace.TrajectoryRule.StartsWith("GENERAL_ENDURANCE", StringComparison.Ordinal));
        Assert.Contains(ready.PrescribedWeeks!, w => w.NumericTrace.TrajectoryRule.StartsWith("AEROBIC_STRENGTH", StringComparison.Ordinal));
        Assert.All(new[] { consistency.PrescribedWeeks![^1], ready.PrescribedWeeks![^1] },
            w => Assert.StartsWith("PRE_SPECIFIC_TRANSITION", w.NumericTrace.TrajectoryRule));
    }

    [Fact]
    public void InvalidEvidenceAndTargets_ReturnTypedFailuresWithoutPartialOutput()
    {
        var invalidWeekly = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Provided, -1,
                PreparationRunwayLoadEvidenceState.Missing, null), Target(16, 5.5)));
        var invalidTarget = Target(16, 5.5) with { OrderedSlots = [] };
        var missingCore = PreparationRunwayNumericMaterializer.Materialize(Request(
            PreparationRunwayAllocationProfile.ConsistencyNeeded, 3,
            Evidence(PreparationRunwayLoadEvidenceState.Missing, null,
                PreparationRunwayLoadEvidenceState.Missing, null), invalidTarget));

        Assert.False(invalidWeekly.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.InvalidRecentWeeklyVolume, invalidWeekly.FailureCode);
        Assert.Null(invalidWeekly.PrescribedWeeks);
        Assert.False(missingCore.IsSuccess);
        Assert.Equal(PreparationRunwayNumericMaterializationFailureCode.CoreWeekOneTargetUnavailable, missingCore.FailureCode);
    }

    [Fact]
    public void ProductionSource_HasNoHorizonRouteTable_Date_Pace_Persistence_OrPublicWiring()
    {
        var repo = RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
        var sourceDir = Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunwayNumericMaterialization");
        var sources = Directory.GetFiles(sourceDir, "*.cs").Select(File.ReadAllText).ToArray();
        var engine = File.ReadAllText(Path.Combine(sourceDir, "PreparationRunwayNumericMaterializer.cs"));

        Assert.DoesNotContain("switch (weeks.Length", engine, StringComparison.Ordinal);
        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("DateOnly", source, StringComparison.Ordinal);
            Assert.DoesNotMatch(new Regex(@"\bPace\b", RegexOptions.IgnoreCase), source);
            Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
        });
        Assert.False(typeof(PreparationRunwayNumericMaterializer).IsPublic);

        foreach (var relative in new[] { "backend/RunningApp.Api", "backend/RunningApp.Persistence", "backend/RunningApp.Application/RuntimeCatalog/PreviewRouting" })
        {
            var root = Path.Combine(repo, relative.Replace('/', Path.DirectorySeparatorChar));
            var hits = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Where(p => File.ReadAllText(p).Contains("PreparationRunwayNumericMaterializer", StringComparison.Ordinal));
            Assert.Empty(hits);
        }
    }

    private static void AssertWeekInvariants(PreparationRunwayPrescribedWeek<PreparationRunwayBlockType> week)
    {
        Assert.Equal(4, week.OrderedSlots.Count);
        Assert.All(week.OrderedSlots, slot => Assert.True(slot.PlannedDistanceKm > 0));
        Assert.Equal(week.PlannedWeeklyVolumeKm, week.OrderedSlots.Sum(s => s.PlannedDistanceKm), 3);
        Assert.Equal(week.PlannedLongRunDistanceKm,
            Assert.Single(week.OrderedSlots, s => s.StructuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun).PlannedDistanceKm);
        var share = week.PlannedLongRunDistanceKm / week.PlannedWeeklyVolumeKm;
        Assert.InRange(share, 0.30d - 0.001d, 0.36d + 0.001d);
    }

    internal static PreparationRunwayNumericMaterializationRequest<PreparationRunwayBlockType> Request(
        PreparationRunwayAllocationProfile profile,
        int runwayWeeks,
        PreparationRunwayStartingLoadEvidence evidence,
        PreparationRunwayCoreWeekOneNumericTarget target) => new(
            profile, StructuralWeeks(profile, runwayWeeks), evidence, target,
            TenKPreparationRunwayNumericPolicyFactory.Build(), PreparationRunwayQuantityUnit.Kilometers);

    internal static PreparationRunwayStartingLoadEvidence Evidence(
        PreparationRunwayLoadEvidenceState weeklyState, double? weekly,
        PreparationRunwayLoadEvidenceState longestState, double? longest) =>
        new(weeklyState, weekly, longestState, longest, 4,
            "recent weekly=four-week average; longest run=30-day maximum; typed evidence fixture");

    internal static PreparationRunwayCoreWeekOneNumericTarget Target(double weekly, double longRun)
    {
        var allocation = FourDaySessionDistanceAllocationPolicy.Allocate(weekly, longRun);
        return new PreparationRunwayCoreWeekOneNumericTarget(
            "TEN_K__4D__INTERMEDIATE", 10, weekly, longRun,
            [
                new(PreparationRunwaySlotRole.KeySession, 1, allocation.KeySessionDistanceKm),
                new(PreparationRunwaySlotRole.EasySupport, 1, allocation.FirstEasySupportDistanceKm),
                new(PreparationRunwaySlotRole.EasySupport, 2, allocation.SecondEasySupportDistanceKm),
                new(PreparationRunwaySlotRole.LongRun, 1, longRun),
            ],
            "current deterministic Core behavior test boundary",
            "CatalogVolumeAndLongRunPlanner + V1FourDaySessionVolumeAllocationPolicy");
    }

    internal static IReadOnlyList<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>> StructuralWeeks(
        PreparationRunwayAllocationProfile profile, int runwayWeeks)
    {
        var allocations = PreparationRunwayBlockAllocationEngine.Allocate(
            runwayWeeks, TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(profile));
        Assert.True(allocations.IsSuccess, allocations.FailureReason);
        var weeks = new List<PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>>();
        var number = 1;
        foreach (var allocation in allocations.Allocations!.OrderBy(a => a.CanonicalOrder).Where(a => a.AllocatedWeeks > 0))
        {
            for (var blockWeek = 1; blockWeek <= allocation.AllocatedWeeks; blockWeek++)
            {
                var slots = new[]
                {
                    Slot(allocation.BlockKey, blockWeek, PreparationRunwaySlotRole.KeySession, 1, 1),
                    Slot(allocation.BlockKey, blockWeek, PreparationRunwaySlotRole.EasySupport, 2, 1),
                    Slot(allocation.BlockKey, blockWeek, PreparationRunwaySlotRole.EasySupport, 3, 2),
                    Slot(allocation.BlockKey, blockWeek, PreparationRunwaySlotRole.LongRun, 4, 1),
                };
                weeks.Add(new PreparationRunwayMaterializedWeek<PreparationRunwayBlockType>(
                    number++, allocation.BlockKey, blockWeek, $"TEN_K_{allocation.BlockKey.ToString().ToUpperInvariant()}_PROGRESSION", 1,
                    blockWeek, allocation.CanonicalOrder, slots,
                    new PreparationRunwayMaterializedWeekProvenance(profile.ToString(), "TEN_K__4D__INTERMEDIATE", 10,
                        "TEN_K_PREPARATION_RUNWAY_ALLOCATION_POLICY", 1, "TEN_K_PREPARATION_RUNWAY_SUPPORT_WORKOUT_POLICY", 1,
                        new PlanCatalogReference("RUN_LAYOUT_4D", 2))));
            }
        }
        return weeks;
    }

    private static PreparationRunwayMaterializedWorkoutSlot<PreparationRunwayBlockType> Slot(
        PreparationRunwayBlockType block, int step, PreparationRunwaySlotRole role, int ordinal, int roleOrdinal) =>
        new(role, ordinal, roleOrdinal,
            role == PreparationRunwaySlotRole.LongRun ? "LONG_RUN_STANDARD" : "EASY_STANDARD", 5,
            block, $"TEN_K_{block.ToString().ToUpperInvariant()}_PROGRESSION", 1, step,
            PreparationRunwayWorkoutSlotSource.SupportPolicy,
            "TEN_K_PREPARATION_RUNWAY_SUPPORT_WORKOUT_POLICY", 1);

    /// <summary>
    /// Phase 10K-FREQ.6D.7: a minimal synthetic single-week plan containing
    /// exactly one KEY_SESSION (plus 2 EASY_SUPPORT + 1 LONG_RUN), used only
    /// to exercise <see cref="PreparationRunwayCoreWeekOneTargetAdapter"/>'s
    /// real-session KEY_SESSION-count derivation for the existing 4D case.
    /// </summary>
    private static CatalogPrescribedPlan SingleKeySessionFirstWeekPlan()
    {
        var sessions = new[]
        {
            SyntheticSession("KEY_SESSION", "EASY_STANDARD", 5, new DateOnly(2026, 1, 5)),
            SyntheticSession("EASY_SUPPORT", "EASY_STANDARD", 5, new DateOnly(2026, 1, 6)),
            SyntheticSession("EASY_SUPPORT", "EASY_STANDARD", 5, new DateOnly(2026, 1, 7)),
            SyntheticSession("LONG_RUN", "LONG_RUN_STANDARD", 5, new DateOnly(2026, 1, 8)),
        };
        var week = new CatalogPrescribedWeek
        {
            WeekNumber = 1, PhaseKey = "FOUNDATION", PlannedWeeklyVolumeKm = 24, AccountedWeeklyDistanceKm = 24,
            AllocationTrace = new SessionVolumeAllocationTrace(1, 24, 8, 16, 6, 5, 5, "TEN_K_PREPARATION_RUNWAY_NUMERIC_POLICY", 1),
            Sessions = sessions,
        };
        return new CatalogPrescribedPlan
        {
            CandidateKey = "TEN_K__4D__INTERMEDIATE", CandidateVersion = 10,
            Weeks = [week], Sessions = sessions,
            ValidationResult = new CatalogSessionPrescriptionValidationResult(true, []),
        };
    }

    private static CatalogPrescribedSession SyntheticSession(string role, string workout, int version, DateOnly date)
    {
        var pace = new CatalogPacePrescription(CatalogPacePrescriptionKind.EffortOnly, null, null, null,
            CatalogPaceSourceSelection.EffortOnly, "EASY", "EASY", "SYNTHETIC");
        var prescription = new CatalogWorkoutPrescription
        {
            PrescriptionMode = CatalogPrescriptionMode.Distance,
            DistanceAccountingMode = CatalogDistanceAccountingMode.ExactSessionTotal,
            DistancePrescription = new CatalogDistancePrescription(role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5, "ExactSessionTotal", "nearest_0.5km"),
            DurationPrescription = new CatalogDurationPrescription(CatalogDurationKind.Unresolved, null, "SYNTHETIC"),
            PacePrescription = pace,
            EffortGuidance = "EASY",
            OrderedSegments = [],
            Status = CatalogSessionPrescriptionStatus.Complete,
        };
        return new CatalogPrescribedSession
        {
            WeekNumber = 1, Date = date, PhaseKey = "FOUNDATION", ProgressionStageKey = "FOUNDATION_EASY_BASE",
            StructuralRole = role, WorkoutDefinitionKey = workout, WorkoutDefinitionVersion = version,
            PlannedDistanceKm = role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5,
            Prescription = prescription, BindingProvenance = "CORE_BINDING", PaceSourceProvenance = "CORE_SOURCE",
            VolumeAllocationProvenance = "CORE_VOLUME",
            DecisionTrace = new SessionPrescriptionDecisionTrace(1, date, workout, 24, 8, 16,
                role == "LONG_RUN" ? 8 : role == "KEY_SESSION" ? 6 : 5, "V1", "Distance", "V1", "NONE",
                "NOT_EVALUATED", CatalogPaceSourceSelection.EffortOnly, [], "nearest_0.5km", "ExactSessionTotal", "Unresolved", null, []),
            ValidationResult = new CatalogSessionPrescriptionValidationResult(true, []),
            PrescriptionSource = new CatalogSessionPrescriptionSource.Legacy(prescription),
        };
    }
}
