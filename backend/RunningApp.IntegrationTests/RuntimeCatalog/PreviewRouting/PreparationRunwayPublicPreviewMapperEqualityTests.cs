using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayCalendarComposition;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayNumericMaterialization;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayOrchestration;
using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunwayWeekMaterialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4G.6B.2 — proves the public
/// <see cref="PreviewWeekDto"/>/<see cref="PreviewDayDto"/> values produced
/// by <see cref="PreparationRunwayPublicPreviewMapper"/> are literally equal
/// (field-by-field, from the SAME orchestrator result object, never
/// recalculated independently) to the authoritative
/// <see cref="TenKPreparationRunwayDarkOrchestrationResult"/> the dark
/// orchestrator produced. No JSON-snapshot comparison and no
/// independently-reconstructed expected values.
/// </summary>
public sealed class PreparationRunwayPublicPreviewMapperEqualityTests
{
    private static string CatalogRoot => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");
    private static readonly Task<PlanCatalogCandidateSummary> CandidateTask = LoadCandidateAsync();
    private static readonly IReadOnlyList<DayOfWeek> MonWedFriSun =
        [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday];

    private static TenKPreparationRunwayDarkOrchestrator Orchestrator() =>
        TenKPreparationRunwayDarkOrchestratorFactory.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot });
        var loader = new PlanCatalogBundleLoader(options, NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    private static Weekday ToWeekday(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => Weekday.Mon, DayOfWeek.Tuesday => Weekday.Tue, DayOfWeek.Wednesday => Weekday.Wed,
        DayOfWeek.Thursday => Weekday.Thu, DayOfWeek.Friday => Weekday.Fri, DayOfWeek.Saturday => Weekday.Sat,
        DayOfWeek.Sunday => Weekday.Sun, _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };

    private static async Task<TenKPreparationRunwayDarkOrchestrationRequest> BuildRequestAsync(int totalWeeks, string readinessValue)
    {
        var candidate = await CandidateTask;
        var start = new DateOnly(2026, 8, 3);
        var race = start.AddDays(totalWeeks * 7);
        var ready = readinessValue == "READY";
        var preview = new GeneratePreviewRequest
        {
            GoalType = GoalType.Race, GoalDistance = GoalDistance.TenK, Level = RunningBackground.Intermediate,
            DaysPerWeek = 4, Unit = DistanceUnit.Km, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = 3000, TargetFinishTimeSource = null,
            PreferredDays = MonWedFriSun.Select(ToWeekday).ToArray(), LongRunDay = Weekday.Sun,
            RecentWeeklyVolumeKm = ready ? 24 : 12, RecentLongestRunKm = ready ? 9 : 5, RecentRunsPerWeek = 4,
            RecentRace = new RecentRaceInput { Distance = GoalDistance.TenK, FinishTimeSeconds = 3000, RaceDate = start.AddDays(-21) },
        };
        var resolver = new ResolverInputSnapshot
        {
            RequestedTargetDistanceKm = 10, CanonicalDistanceFamily = "TEN_K", GoalType = GoalType.Race,
            GoalDistance = GoalDistance.TenK, GoalDistanceKm = 10, StartDate = start, RaceDate = race,
            TargetFinishTimeSeconds = 3000, TargetFinishTimeSource = null,
            DaysPerWeek = 4, PreferredDays = MonWedFriSun.Select(ToWeekday).ToArray(), LongRunDay = Weekday.Sun, Level = RunningBackground.Intermediate,
            RecentWeeklyVolumeKm = ready ? 24 : 12, RecentLongestRunKm = ready ? 9 : 5, RecentRunsPerWeek = 4,
            RecentRaceDistanceKm = 10, RecentRaceFinishTimeSeconds = 3000, RecentRaceDate = start.AddDays(-21),
        };
        var readiness = RuntimeConditionResolutionResult.Evaluated(
            CoreEntryReadinessResolver.ConditionTypeValue, ready ? "READY" : "NOT_READY",
            ready ? "CORE_ENTRY_READY" : "CORE_ENTRY_NOT_READY");
        var conditions = new List<RuntimeConditionResolutionResult>
        {
            readiness,
            RuntimeConditionResolutionResult.Evaluated(PaceSourceResolver.ConditionTypeValue, "RECENT_RACE", "RECENT_RACE_RESULT_PROVIDED"),
            RuntimeConditionResolutionResult.Evaluated(GoalFeasibilityResolver.ConditionTypeValue, "REALISTIC", "WITHIN_REALISTIC_BAND"),
        };
        return new TenKPreparationRunwayDarkOrchestrationRequest(
            candidate, start, race, start, MonWedFriSun, DayOfWeek.Sunday, readiness, conditions,
            preview, resolver, PreparationRunwayQuantityUnit.Kilometers);
    }

    public static IEnumerable<object[]> Profiles => new[] { new object[] { "READY" }, new object[] { "NOT_READY" } };

    [Theory]
    [MemberData(nameof(Profiles))]
    public async Task MappedWeeks_AreFieldByFieldEqual_ToAuthoritativeOrchestratorResult(string readinessValue)
    {
        var request = await BuildRequestAsync(18, readinessValue);
        var result = await Orchestrator().OrchestrateAsync(request);
        Assert.True(result.IsSuccess, result.Failure?.Reason);

        var mapped = PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(result);
        var composedByGlobalWeek = result.CalendarComposition!.OrderedCombinedWeeks!.ToDictionary(w => w.GlobalWeekNumber);
        var pacedRunwayByGlobalWeek = result.PacedRunway!.PacedRunwayWeeks!.ToDictionary(w => w.OriginalWeek.GlobalWeekNumber);
        var coreWeeksByLocalNumber = result.CoreResult!.PrescriptionResult.FinalPrescribedPlan.Weeks.ToDictionary(w => w.WeekNumber);

        // Plan-level equality.
        Assert.Equal(18, mapped.Count);
        Assert.Equal(result.CalendarComposition.OrderedCombinedWeeks!.Count, mapped.Count);
        var totalOrchestratorSessions = result.CalendarComposition.OrderedCombinedWeeks!.Sum(w =>
            w.SegmentType == PreparationRunwaySegmentType.PreparationRunway
                ? pacedRunwayByGlobalWeek[w.GlobalWeekNumber].ChronologicalSlots.Count
                : coreWeeksByLocalNumber[w.SegmentLocalWeekNumber].Sessions.Count);
        Assert.Equal(totalOrchestratorSessions, mapped.Sum(w => w.Days.Count));

        foreach (var week in mapped)
        {
            var composed = composedByGlobalWeek[week.WeekNumber];
            Assert.Equal(composed.GlobalWeekNumber, week.WeekNumber);

            if (composed.SegmentType == PreparationRunwaySegmentType.PreparationRunway)
            {
                var pacedWeek = pacedRunwayByGlobalWeek[composed.GlobalWeekNumber];
                var blockType = pacedWeek.OriginalWeek.PrescribedWeek.StructuralWeek.BlockType;

                Assert.Equal(TrainingWeekType.PreparationRunway, week.WeekType);
                Assert.Equal(PreparationRunwayPublicPreviewMapper.RunwayBlockPublicName(blockType), week.RunwayBlock);
                Assert.Equal(pacedWeek.ChronologicalSlots.Count, week.Days.Count);

                for (var i = 0; i < pacedWeek.ChronologicalSlots.Count; i++)
                {
                    var slot = pacedWeek.ChronologicalSlots[i];
                    var day = week.Days[i];

                    Assert.Equal(slot.OriginalSlot.SessionDate.ToDateTime(TimeOnly.MinValue), day.Date);
                    Assert.Equal(slot.OriginalSlot.PrescribedSlot.PlannedDistanceKm, day.DistanceKm);
                    Assert.Equal(slot.PacePrescription.EffortLabel, day.Intensity);
                    Assert.Equal(
                        slot.OriginalSlot.PrescribedSlot.StructuralSlot.SlotRole == PreparationRunwaySlotRole.LongRun,
                        day.DayType == TrainingDayType.LongRun);
                    var expectedRole = slot.OriginalSlot.PrescribedSlot.StructuralSlot.SlotRole switch
                    {
                        PreparationRunwaySlotRole.LongRun => TrainingDayType.LongRun,
                        PreparationRunwaySlotRole.KeySession => TrainingDayType.Tempo,
                        _ => TrainingDayType.Easy,
                    };
                    Assert.Equal(expectedRole, day.DayType);
                }
                // Chronological order within the mapped week.
                Assert.Equal(week.Days.Select(d => d.Date), week.Days.Select(d => d.Date).OrderBy(d => d));
            }
            else
            {
                var coreWeek = coreWeeksByLocalNumber[composed.SegmentLocalWeekNumber];
                var orderedSessions = coreWeek.Sessions.OrderBy(s => s.Date).ToArray();

                Assert.Null(week.RunwayBlock);
                var expectedWeekType = coreWeek.PhaseKey switch
                {
                    "FOUNDATION" => TrainingWeekType.Base,
                    "BUILD" => TrainingWeekType.Build,
                    "RACE_SPECIFIC" => TrainingWeekType.Peak,
                    "TAPER" => TrainingWeekType.Taper,
                    _ => TrainingWeekType.Build,
                };
                Assert.Equal(expectedWeekType, week.WeekType);
                Assert.Equal(orderedSessions.Length, week.Days.Count);

                for (var i = 0; i < orderedSessions.Length; i++)
                {
                    var session = orderedSessions[i];
                    var day = week.Days[i];

                    Assert.Equal(session.Date.ToDateTime(TimeOnly.MinValue), day.Date);
                    Assert.Equal(session.PlannedDistanceKm, day.DistanceKm);
                    Assert.Equal(session.Prescription.PacePrescription.EffortLabel, day.Intensity);
                    var expectedRole = session.StructuralRole switch
                    {
                        "LONG_RUN" => TrainingDayType.LongRun,
                        "KEY_SESSION" => TrainingDayType.Tempo,
                        _ => TrainingDayType.Easy,
                    };
                    Assert.Equal(expectedRole, day.DayType);
                }
            }
        }

        // Final runway block is always PRE_SPECIFIC_TRANSITION; no Core week ever carries a runway_block.
        var runwayWeeks = mapped.Where(w => w.RunwayBlock is not null).ToList();
        Assert.Equal("PRE_SPECIFIC_TRANSITION", runwayWeeks.Last().RunwayBlock);
        Assert.All(mapped.Where(w => w.RunwayBlock is null), w => Assert.NotEqual(TrainingWeekType.PreparationRunway, w.WeekType));
    }

    [Fact]
    public async Task Mapper_IsValueDeterministic_RepeatedMappingOfSameResult_IsIdentical()
    {
        var request = await BuildRequestAsync(17, "READY");
        var result = await Orchestrator().OrchestrateAsync(request);
        Assert.True(result.IsSuccess, result.Failure?.Reason);

        var first = PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(result);
        var second = PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(result);

        Assert.Equal(Flatten(first), Flatten(second));
    }

    [Fact]
    public async Task Mapper_InputCollectionOrder_DoesNotAlterNormalizedOutput()
    {
        var request = await BuildRequestAsync(16, "READY");
        var forward = await Orchestrator().OrchestrateAsync(request);
        var reordered = await Orchestrator().OrchestrateAsync(request with
        {
            PreferredDays = request.PreferredDays.Reverse().ToArray(),
            ConditionResults = request.ConditionResults.Reverse().ToArray(),
        });
        Assert.True(forward.IsSuccess, forward.Failure?.Reason);
        Assert.True(reordered.IsSuccess, reordered.Failure?.Reason);

        Assert.Equal(
            Flatten(PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(forward)),
            Flatten(PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(reordered)));
    }

    private static string Flatten(List<PreviewWeekDto> weeks) => string.Join('|', weeks.Select(w =>
        $"{w.WeekNumber}:{w.WeekType}:{w.RunwayBlock}:" + string.Join(',', w.Days.Select(d =>
            $"{d.Date:yyyy-MM-dd}:{d.DayType}:{d.DistanceKm}:{d.Intensity}"))));
}
