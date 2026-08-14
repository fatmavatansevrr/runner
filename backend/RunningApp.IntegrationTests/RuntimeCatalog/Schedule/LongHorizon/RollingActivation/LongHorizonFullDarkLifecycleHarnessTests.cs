using System.Collections.Concurrent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal static class LongHorizonFullLifecycleTestFixture
{
    internal static readonly DateOnly StartDate = new(2026, 8, 3);
    internal static readonly IReadOnlyList<DayOfWeek> PreferredDays =
        [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday];
    private static readonly ConcurrentDictionary<(int Weeks, ReadinessProfile Profile),
        Lazy<Task<LongHorizonFullDarkLifecycleValidationResult>>> Results = new();
    private static readonly Lazy<Task<PlanCatalogCandidateSummary>> Candidate = new(LoadCandidateAsync);

    public static IEnumerable<object[]> Horizons() => Enumerable.Range(21, 32).Select(week => new object[] { week });

    internal static Task<LongHorizonFullDarkLifecycleValidationResult> RunAsync(
        int weeks, ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded) =>
        Results.GetOrAdd((weeks, profile), key => new Lazy<Task<LongHorizonFullDarkLifecycleValidationResult>>(
            async () => await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(await ScenarioAsync(key.Weeks, key.Profile)))).Value;

    internal static async Task<LongHorizonLifecycleScenario> ScenarioAsync(
        int weeks, ReadinessProfile profile = ReadinessProfile.ConsistencyNeeded,
        Func<int, LongHorizonLifecycleWindowEvidence, LongHorizonLifecycleWindowEvidence>? transform = null,
        IReadOnlyList<LongHorizonLifecycleWindowEvidence>? retries = null,
        int? expectedBlockedOrdinal = null)
    {
        var evidence = Enumerable.Range(1, weeks).ToDictionary(
            ordinal => ordinal,
            ordinal =>
            {
                var row = new LongHorizonLifecycleWindowEvidence
                {
                    ActivationOrdinal = ordinal,
                    CheckpointDate = StartDate.AddDays(ordinal * 28 + 1),
                    SessionOutcomes = Enumerable.Range(0, 16).Select(_ => new LongHorizonLifecycleSessionOutcome
                    {
                        Status = TrainingDayStatus.Completed,
                        ActualDistanceMultiplier = 1d,
                    }).ToList(),
                    SafetyState = LongHorizonSafetyState.Clear,
                    Availability = PreferredDays,
                };
                return transform?.Invoke(ordinal, row) ?? row;
            });
        return new LongHorizonLifecycleScenario
        {
            ScenarioId = StableGuid($"Phase4K9|{weeks}|{profile}|{expectedBlockedOrdinal}|{retries?.Count ?? 0}"),
            TotalWeeks = weeks,
            ReadinessProfile = profile,
            InitialOnboardingEvidence = new LongHorizonGeEntryBaselineInput(20, 8, 4),
            PreferredDays = PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            StartDate = StartDate,
            RaceDate = StartDate.AddDays(weeks * 7),
            TargetFinishTimeSeconds = 3480,
            TargetFinishTimeSource = TargetFinishTimeSource.ProductAverage,
            EvidenceByActivationOrdinal = evidence,
            InitialPriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(20, 8),
            RetryEvidence = retries ?? [],
            ExpectedBlockedActivationOrdinal = expectedBlockedOrdinal,
            ExpectedFinalOutcome = retries?.Count > 0
                ? LongHorizonFullDarkLifecycleOutcome.RetryRecoveredAndCompleted
                : expectedBlockedOrdinal is null
                    ? LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully
                    : LongHorizonFullDarkLifecycleOutcome.BlockedAsExpected,
            CatalogRootPath = CatalogRoot(),
            Candidate = await Candidate.Value,
        };
    }

    private static string CatalogRoot() => Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog");

    private static async Task<PlanCatalogCandidateSummary> LoadCandidateAsync()
    {
        var loader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return await new CatalogCandidateEligibilityGate(loader).LoadForInternalDryRunAsync(
            V1CatalogPilotIdentityPolicy.CandidateKey, V1CatalogPilotIdentityPolicy.CandidateVersion);
    }

    internal static Guid StableGuid(string seed) => new(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(seed)).AsSpan(0, 16));
}

public sealed class Phase4K9FullHorizonLifecycleMatrixTests
{
    [Theory]
    [MemberData(nameof(LongHorizonFullLifecycleTestFixture.Horizons), MemberType = typeof(LongHorizonFullLifecycleTestFixture))]
    public async Task EveryHorizon21Through52CompletesWithoutFullUpfrontNumericExecution(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);

        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, result.Outcome);
        Assert.Equal(totalWeeks, result.FinalState!.ActivatedWeeks.Count);
        Assert.DoesNotContain(result.FinalState.LifecycleStates.Values,
            state => state is LongHorizonNumericLifecycleState.NumericPending or LongHorizonNumericLifecycleState.NumericActivationBlocked);
        Assert.Equal(LongHorizonStructuralSegmentType.Core, result.FinalState.ActivatedWeeks[totalWeeks].SegmentType);
        Assert.True(result.StateSnapshots[0].ActivatedWeeks.Count <= 4);
        Assert.Equal(1, result.FinalState.FullRunwayMaterializationCount);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(53)]
    public async Task OutsideSupportedRangeIsRejected(int totalWeeks)
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(21);
        var invalid = scenario with { TotalWeeks = totalWeeks, RaceDate = scenario.StartDate.AddDays(totalWeeks * 7) };
        await Assert.ThrowsAsync<LongHorizonStructuralRoadmapInvalidException>(
            () => new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(invalid));
    }
}

public sealed class Phase4K9LifecycleRoutingAndBoundaryTests
{
    [Fact]
    public async Task AuditTraceProvesInitialCheckpointJitCalendarAndFinalRouting()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(29);
        var types = result.FinalState!.AuditEvents.Select(e => e.EventType).ToHashSet();

        Assert.Contains(LongHorizonLifecycleAuditEventType.StructuralRoadmapCreated, types);
        Assert.Contains(LongHorizonLifecycleAuditEventType.InitialWindowActivated, types);
        Assert.Contains(LongHorizonLifecycleAuditEventType.CheckpointSnapshotCreated, types);
        Assert.Contains(LongHorizonLifecycleAuditEventType.RunwayPrescriptionCreated, types);
        Assert.Contains(LongHorizonLifecycleAuditEventType.CalendarProjectionAligned, types);
        Assert.Contains(LongHorizonLifecycleAuditEventType.CoreWindowActivated, types);
        Assert.Contains(LongHorizonLifecycleAuditEventType.LifecycleCompleted, types);
    }

    [Theory]
    [InlineData(25, 1, 3)]
    [InlineData(26, 2, 2)]
    [InlineData(27, 3, 1)]
    public async Task GeRunwayMixedBoundaryUsesGreedyFourWeekShape(int totalWeeks, int geCount, int runwayCount)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var mixed = result.StateSnapshots.Select(s => s.CurrentWindow)
            .First(window => window.SegmentsCovered.SequenceEqual([
                LongHorizonStructuralSegmentType.GeneralEndurance,
                LongHorizonStructuralSegmentType.PreparationRunway]));

        Assert.Equal(geCount, mixed.Weeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance));
        Assert.Equal(runwayCount, mixed.Weeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
    }

    [Fact]
    public async Task RunwayAndCoreCalendarsRemainCompleteDistinctAndInsideStructuralWeeks()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(52);
        var dated = result.FinalState!.ActivatedWeeks.Values
            .Where(w => w.SegmentType is LongHorizonStructuralSegmentType.PreparationRunway or LongHorizonStructuralSegmentType.Core)
            .ToList();
        Assert.All(dated, week =>
        {
            Assert.Equal(4, week.SessionPrescriptions!.Count);
            Assert.Equal(4, week.SessionPrescriptions.Select(s => s.AssignedDate).Distinct().Count());
            Assert.All(week.SessionPrescriptions, session =>
            {
                Assert.NotNull(session.AssignedDate);
                Assert.Contains(session.AssignedDate!.Value.DayOfWeek, LongHorizonFullLifecycleTestFixture.PreferredDays);
                Assert.InRange(session.AssignedDate.Value, week.CalendarDates!.Value.Start, week.CalendarDates.Value.End);
            });
            Assert.Equal(DayOfWeek.Sunday, week.SessionPrescriptions.Single(s =>
                string.Equals(s.SessionRole.Replace("_", string.Empty, StringComparison.Ordinal), "LONGRUN", StringComparison.OrdinalIgnoreCase))
                .AssignedDate!.Value.DayOfWeek);
        });
        Assert.Equal(dated.Sum(w => w.SessionPrescriptions!.Count),
            dated.SelectMany(w => w.SessionPrescriptions!).Select(s => s.AssignedDate).Distinct().Count());
    }

    [Theory]
    [InlineData(25, 1, 3)]
    [InlineData(26, 2, 2)]
    [InlineData(27, 3, 1)]
    public async Task EveryReachableRunwayCoreRemainderShapeOccursNaturally(int totalWeeks, int runwayCount, int coreCount)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var boundary = result.StateSnapshots.Select(s => s.CurrentWindow).First(window =>
            window.SegmentsCovered.SequenceEqual([
                LongHorizonStructuralSegmentType.PreparationRunway,
                LongHorizonStructuralSegmentType.Core]));
        Assert.Equal(runwayCount, boundary.Weeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway));
        Assert.Equal(coreCount, boundary.Weeks.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.Core));
    }

    [Fact]
    public async Task AlignedEightWeekRunwayProducesRunwayOnlyThenCoreOnlyAndNoFabricatedMixedBoundary()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(24);
        Assert.DoesNotContain(result.StateSnapshots.Select(s => s.CurrentWindow), window =>
            window.SegmentsCovered.SequenceEqual([
                LongHorizonStructuralSegmentType.PreparationRunway,
                LongHorizonStructuralSegmentType.Core]));
        Assert.Contains(result.StateSnapshots.Select(s => s.CurrentWindow), window =>
            window.SegmentsCovered.SequenceEqual([LongHorizonStructuralSegmentType.PreparationRunway]));
        Assert.Contains(result.StateSnapshots.Select(s => s.CurrentWindow), window =>
            window.SegmentsCovered.SequenceEqual([LongHorizonStructuralSegmentType.Core]));
    }

    [Theory]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(27, 3)]
    public async Task CoreCompletesThroughRepeatedWindowsAndExpectedFinalPartialSlice(int totalWeeks, int finalSize)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        var coreOnly = result.StateSnapshots.Select(s => s.CurrentWindow)
            .Where(window => window.SegmentsCovered.SequenceEqual([LongHorizonStructuralSegmentType.Core])).ToList();
        Assert.NotEmpty(coreOnly);
        Assert.Equal(finalSize, coreOnly[^1].ActualWindowSizeWeeks);
        Assert.Equal(12, result.FinalState!.ActivatedWeeks.Values.Count(w => w.SegmentType == LongHorizonStructuralSegmentType.Core));
    }
}

public sealed class Phase4K9ProfileReplayAndImmutabilityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task BothProfilesCompleteWithSameLifecycleAuthorities(int profileValue)
    {
        var profile = profileValue == 0 ? ReadinessProfile.ConsistencyNeeded : ReadinessProfile.CoreEntryReady;
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(28, profile);
        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, result.Outcome);
        Assert.Equal(28, result.FinalState!.ActivatedWeeks.Count);
        Assert.Equal(1, result.FinalState.FullRunwayMaterializationCount);
    }

    [Fact]
    public async Task CompleteReplayIsDeterministic()
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(24);
        var first = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);
        var second = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);

        Assert.Equal(first.Outcome, second.Outcome);
        Assert.Equal(first.FinalState!.ContextVersion, second.FinalState!.ContextVersion);
        Assert.Equal(first.FinalState.RunwayPrescription!.PrescriptionId, second.FinalState.RunwayPrescription!.PrescriptionId);
        Assert.Equal(first.FinalState.RunwayTargetLock, second.FinalState.RunwayTargetLock);
        Assert.Equal(
            first.FinalState.ActivatedWeeks.SelectMany(p => p.Value.SessionPrescriptions!).Select(s => s.AssignedDate),
            second.FinalState.ActivatedWeeks.SelectMany(p => p.Value.SessionPrescriptions!).Select(s => s.AssignedDate));
    }

    [Fact]
    public async Task HistoricalRunwayDatesAndTargetLockRemainImmutableAcrossCoreRefreshes()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(52);
        var final = result.FinalState!;
        Assert.True(final.CoreContextIds.Count >= 2);
        Assert.Equal(1, final.FullRunwayMaterializationCount);
        var firstLock = result.StateSnapshots.First(s => s.RunwayTargetLock is not null).RunwayTargetLock;
        Assert.Equal(firstLock, final.RunwayTargetLock);
        var firstRunway = result.StateSnapshots.First(s => s.RunwayCalendarProjection is not null).RunwayCalendarProjection;
        Assert.Equal(firstRunway, final.RunwayCalendarProjection);
    }

    [Fact]
    public async Task ChangedEvidenceChangesDecisionAndFutureOnlyWhileInitialHistoryRemainsIdentical()
    {
        var baseline = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(28);
        var changed = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(
            28,
            transform: (ordinal, row) => ordinal == 1
                ? row with
                {
                    SessionOutcomes = row.SessionOutcomes.Select(outcome =>
                        outcome with { ActualDistanceMultiplier = 0.9d }).ToList(),
                }
                : row);
        changed = changed with { ScenarioId = baseline.ScenarioId };
        var first = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(baseline);
        var second = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(changed);

        Assert.Equal(
            first.StateSnapshots[0].ActivatedWeeks.OrderBy(pair => pair.Key)
                .Select(pair => (pair.Key, pair.Value.TotalWeeklyVolumeKm, pair.Value.LongRunKm, pair.Value.CalendarDates)),
            second.StateSnapshots[0].ActivatedWeeks.OrderBy(pair => pair.Key)
                .Select(pair => (pair.Key, pair.Value.TotalWeeklyVolumeKm, pair.Value.LongRunKm, pair.Value.CalendarDates)));
        Assert.NotEqual(first.FinalState!.CheckpointDecisions[0].DecisionId, second.FinalState!.CheckpointDecisions[0].DecisionId);
        Assert.Equal(
            first.StateSnapshots[0].ActivatedWeeks.SelectMany(pair => pair.Value.SessionPrescriptions!).Select(s => s.AssignedDate),
            second.StateSnapshots[0].ActivatedWeeks.SelectMany(pair => pair.Value.SessionPrescriptions!).Select(s => s.AssignedDate));
        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, second.Outcome);
    }
}

public sealed class Phase4K9BlockedRetryContractTests
{
    [Fact]
    public async Task SafetyBlockRequiresExplicitRetryThenCompletes()
    {
        var retry = new LongHorizonLifecycleWindowEvidence
        {
            ActivationOrdinal = 1,
            CheckpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30),
            SessionOutcomes = Enumerable.Range(0, 16).Select(_ => new LongHorizonLifecycleSessionOutcome
            {
                Status = TrainingDayStatus.Completed,
            }).ToList(),
            SafetyState = LongHorizonSafetyState.Clear,
            Availability = LongHorizonFullLifecycleTestFixture.PreferredDays,
        };
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(
            28,
            transform: (ordinal, row) => ordinal == 1
                ? row with { SafetyState = LongHorizonSafetyState.UnresolvedSafetyCritical }
                : row,
            retries: [retry],
            expectedBlockedOrdinal: 1);

        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);

        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.RetryRecoveredAndCompleted, result.Outcome);
        Assert.Contains(result.FinalState!.AuditEvents, e => e.EventType == LongHorizonLifecycleAuditEventType.WindowBlocked
            && e.Reason == LongHorizonReasonCode.SafetyReassessmentRequired);
        Assert.Contains(result.FinalState.AuditEvents, e => e.EventType == LongHorizonLifecycleAuditEventType.BlockRestoredToPending);
        Assert.DoesNotContain(result.StateSnapshots.Zip(result.StateSnapshots.Skip(1)), pair =>
            pair.First.LifecycleStates.Any(state => state.Value == LongHorizonNumericLifecycleState.NumericActivationBlocked)
            && pair.Second.LifecycleStates.Any(state => state.Value == LongHorizonNumericLifecycleState.NumericActivated));
    }

    [Fact]
    public async Task UnresolvedSessionsLaterBecomeTerminalAndRecoverThroughPending()
    {
        var terminal = new LongHorizonLifecycleWindowEvidence
        {
            ActivationOrdinal = 1,
            CheckpointDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(30),
            SessionOutcomes = Enumerable.Range(0, 16).Select(_ => new LongHorizonLifecycleSessionOutcome
            {
                Status = TrainingDayStatus.Completed,
            }).ToList(),
            SafetyState = LongHorizonSafetyState.Clear,
            Availability = LongHorizonFullLifecycleTestFixture.PreferredDays,
        };
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(
            28,
            transform: (ordinal, row) => ordinal == 1
                ? row with
                {
                    SessionOutcomes = Enumerable.Range(0, 16).Select(_ => new LongHorizonLifecycleSessionOutcome
                    {
                        Status = TrainingDayStatus.Planned,
                    }).ToList(),
                }
                : row,
            retries: [terminal],
            expectedBlockedOrdinal: 1);
        scenario = scenario with { InitialPriorValidatedAnchor = null };

        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);

        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.RetryRecoveredAndCompleted, result.Outcome);
        Assert.Contains(result.FinalState!.AuditEvents, e => e.EventType == LongHorizonLifecycleAuditEventType.BlockRetryRequested);
    }

    [Fact]
    public void ExplicitChangedLaterRetryRestoresBlockedOnlyToPending()
    {
        var service = new LongHorizonBlockedActivationRetryService();
        var result = service.RestorePendingEligibility(new LongHorizonBlockedActivationRetryRequest
        {
            LifecycleStates = new Dictionary<int, LongHorizonNumericLifecycleState>
            {
                [1] = LongHorizonNumericLifecycleState.Completed,
                [2] = LongHorizonNumericLifecycleState.NumericActivationBlocked,
            },
            BlockedGlobalWeeks = [2],
            PreviousCheckpointDate = new DateOnly(2026, 8, 10),
            RetryCheckpointDate = new DateOnly(2026, 8, 11),
            PreviousDecisionId = LongHorizonFullLifecycleTestFixture.StableGuid("blocked"),
            PreviousEvidenceIdentity = "missing-weekly",
            RetryEvidenceIdentity = "weekly-resolved",
        });

        Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, result.LifecycleStates[2]);
        Assert.NotEqual(LongHorizonNumericLifecycleState.NumericActivated, result.LifecycleStates[2]);
        Assert.NotEqual(LongHorizonFullLifecycleTestFixture.StableGuid("blocked"), result.RetryDecisionId);
    }

    [Fact]
    public void SameDateOrUnchangedEvidenceCannotRetry()
    {
        var service = new LongHorizonBlockedActivationRetryService();
        var baseline = new LongHorizonBlockedActivationRetryRequest
        {
            LifecycleStates = new Dictionary<int, LongHorizonNumericLifecycleState>
            {
                [2] = LongHorizonNumericLifecycleState.NumericActivationBlocked,
            },
            BlockedGlobalWeeks = [2],
            PreviousCheckpointDate = new DateOnly(2026, 8, 10),
            RetryCheckpointDate = new DateOnly(2026, 8, 10),
            PreviousDecisionId = LongHorizonFullLifecycleTestFixture.StableGuid("blocked"),
            PreviousEvidenceIdentity = "same",
            RetryEvidenceIdentity = "changed",
        };
        Assert.Throws<LongHorizonIllegalLifecycleTransitionException>(() => service.RestorePendingEligibility(baseline));
        Assert.Throws<LongHorizonIllegalLifecycleTransitionException>(() => service.RestorePendingEligibility(
            baseline with { RetryCheckpointDate = new DateOnly(2026, 8, 11), RetryEvidenceIdentity = "same" }));
    }

    [Fact]
    public void DirectBlockedToActivatedRemainsIllegal()
    {
        Assert.Throws<LongHorizonIllegalLifecycleTransitionException>(() =>
            LongHorizonNumericLifecycleTransitionValidator.ValidateTransition(
                LongHorizonNumericLifecycleState.NumericActivationBlocked,
                LongHorizonNumericLifecycleState.NumericActivated));
    }

    public static IEnumerable<object[]> RequiredReasonCodes()
    {
        var checkpoint = new[]
        {
            LongHorizonCheckpointReasonCode.CheckpointWindowNotComplete,
            LongHorizonCheckpointReasonCode.CheckpointEvidenceStale,
            LongHorizonCheckpointReasonCode.ValidatedLoadUnavailable,
            LongHorizonCheckpointReasonCode.ValidatedLongRunEvidenceUnavailable,
            LongHorizonCheckpointReasonCode.MaintenanceAnchorUnavailable,
            LongHorizonCheckpointReasonCode.NumericWindowInfeasible,
            LongHorizonCheckpointReasonCode.SafetyReassessmentRequired,
            LongHorizonCheckpointReasonCode.EvidenceConflictUnresolved,
        }.Select(reason => LongHorizonReasonCode.FromCheckpoint(reason).Code);
        var jit = Enum.GetValues<LongHorizonJitReasonCode>().Select(reason => LongHorizonReasonCode.FromJit(reason).Code);
        return checkpoint.Concat(jit).Select(code => new object[] { code });
    }

    [Theory]
    [MemberData(nameof(RequiredReasonCodes))]
    public void EveryRequiredBlockReasonHasOneTypedAuthoritativeRepresentation(string reasonCode)
    {
        Assert.False(string.IsNullOrWhiteSpace(reasonCode));
        Assert.Single(RequiredReasonCodes(), row => string.Equals((string)row[0], reasonCode, StringComparison.Ordinal));
    }
}

public sealed class Phase4K9GrowthMaintenanceAndRecoveryTests
{
    [Fact]
    public async Task CanonicalCompletedEvidenceProducesRepeatedGrowthDecisions()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(52);
        Assert.True(result.FinalState!.AuditEvents.Count(e => e.EventType == LongHorizonLifecycleAuditEventType.GrowthDecisionMade) >= 2);
        Assert.All(result.FinalState.CheckpointDecisions.Where(d => d.Outcome == LongHorizonCheckpointOutcome.GrowthEligible),
            decision => Assert.Null(decision.AuthoritativeReason));
        Assert.Contains(result.FinalState.ActivatedWeeks.Values,
            week => week.NumericPolicyProvenance?.Contains("GrowthEligible", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task ExplicitIncompleteWeekWithFreshPriorProducesMaintenanceWithoutUpwardDrift()
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(
            36,
            transform: (ordinal, row) => ordinal is 1 or 2
                ? row with
                {
                    SessionOutcomes = row.SessionOutcomes.Select((outcome, index) => index < 4
                        ? outcome with { Status = TrainingDayStatus.Missed, ExplicitActualDistanceKm = null }
                        : outcome).ToList(),
                }
                : row);
        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);

        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, result.Outcome);
        Assert.True(result.FinalState!.AuditEvents.Count(e => e.EventType == LongHorizonLifecycleAuditEventType.MaintenanceDecisionMade) >= 1);
        Assert.Contains(result.FinalState.ActivatedWeeks.Values,
            week => week.NumericPolicyProvenance?.Contains("MaintenanceOnly", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task ExistingRecoveryWeeksRemainLowerThanTheirImmediatePredecessor()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(52);
        var ge = result.FinalState!.ActivatedWeeks.Values
            .Where(w => w.SegmentType == LongHorizonStructuralSegmentType.GeneralEndurance)
            .OrderBy(w => w.GlobalWeekNumber).ToList();
        var recoveries = result.FinalState.StructuralSkeleton.Weeks
            .Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance && w.IsRecoveryWeek == true)
            .Select(w => w.GlobalWeekNumber).Where(w => w > 1).ToList();
        Assert.NotEmpty(recoveries);
        Assert.All(recoveries, week => Assert.True(
            ge.Single(w => w.GlobalWeekNumber == week).TotalWeeklyVolumeKm
            < ge.Single(w => w.GlobalWeekNumber == week - 1).TotalWeeklyVolumeKm));
    }
}

public sealed class Phase4K9LoadAndPaceMatrixTests
{
    [Theory]
    [InlineData(12, 5)]
    [InlineData(20, 8)]
    [InlineData(30, 11)]
    public async Task LowTypicalAndHighSupportedStartingLoadsComplete(double weekly, double longRun)
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(24);
        scenario = scenario with
        {
            ScenarioId = LongHorizonFullLifecycleTestFixture.StableGuid($"load|{weekly}|{longRun}"),
            InitialOnboardingEvidence = new LongHorizonGeEntryBaselineInput(weekly, longRun, 4),
            InitialPriorValidatedAnchor = LongHorizonCheckpointTestFixture.Prior(weekly, longRun),
        };
        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);
        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, result.Outcome);
    }

    [Fact]
    public async Task UserDefinedTargetTimeCompletesThroughRealConditionService()
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(24);
        scenario = scenario with
        {
            ScenarioId = LongHorizonFullLifecycleTestFixture.StableGuid("user-defined-target"),
            TargetFinishTimeSeconds = 3600,
            TargetFinishTimeSource = TargetFinishTimeSource.UserDefined,
            RecentRace = new RecentRaceInput
            {
                Distance = GoalDistance.TenK,
                FinishTimeSeconds = 3550,
                RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(-21),
            },
        };
        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);
        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, result.Outcome);
    }

    [Fact]
    public async Task RecentRacePaceSourceCompletesAndRemainsSeparateFromVolumeEvidence()
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(24);
        scenario = scenario with
        {
            ScenarioId = LongHorizonFullLifecycleTestFixture.StableGuid("recent-race"),
            RecentRace = new RecentRaceInput
            {
                Distance = GoalDistance.TenK,
                FinishTimeSeconds = 3500,
                RaceDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(-21),
            },
        };
        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);
        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.CompletedSuccessfully, result.Outcome);
        Assert.NotNull(result.FinalState!.LatestValidatedLoad);
        Assert.NotEqual(LongHorizonEvidenceSource.RuntimeConditionResolution,
            result.FinalState.LatestValidatedLoad!.WeeklyLoadSource.Source);
    }

    [Fact]
    public async Task MissingExplicitPaceInputsUseExistingProductAverageFallbackAtRealCompositionSeam()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var result = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator()
            .ComposeAndActivateNextWindowAsync(request with
            {
                TargetFinishTimeSeconds = null,
                TargetFinishTimeSource = null,
                RecentRace = null,
            });
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, result.Outcome);
        Assert.Contains(result.ResolvedConditionResults!, condition =>
            condition.ConditionType == "PACE_SOURCE_IN" && condition.Status == RuntimeConditionResolutionStatus.Evaluated);
        Assert.Null(result.AuthoritativeReason);
    }
}

public sealed class Phase4K9DarkIntegrationBoundaryTests
{
    [Fact]
    public void HarnessIsInternalAndNotRegisteredInApiOrPublicContracts()
    {
        Assert.False(typeof(LongHorizonFullDarkLifecycleHarness).IsPublic);
        var root = TestPlanServicesFactory.RepoRoot();
        foreach (var relative in new[] { "backend/RunningApp.Api", "backend/RunningApp.Persistence", "mobile" })
        {
            var files = Directory.Exists(Path.Combine(root, relative))
                ? Directory.GetFiles(Path.Combine(root, relative), "*.*", SearchOption.AllDirectories)
                    .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".dart", StringComparison.OrdinalIgnoreCase))
                : [];
            Assert.DoesNotContain(files, file => File.ReadAllText(file).Contains("LongHorizonFullDarkLifecycleHarness", StringComparison.Ordinal));
        }
    }
}
