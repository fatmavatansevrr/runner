using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;

internal static class Phase4K8DTestHarness
{
    public static async Task<(LongHorizonRollingJitCompositionRequest Request, LongHorizonRollingJitCompositionResult Result)> FirstAsync()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var result = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, result.Outcome);
        return (request, result);
    }

    public static async Task<LongHorizonRollingJitCompositionResult> ContinueAsync(int completedThrough)
    {
        var (request, first) = await FirstAsync();
        var continuation = request with
        {
            LifecycleStates = LongHorizonRollingJitCompositionOrchestratorTests.Lifecycle(28, completedThrough),
            PreviousActivatedWindow = LongHorizonRollingJitCompositionOrchestratorTests.PriorWindow(completedThrough),
            ExistingLockedCoreTarget = first.ActivationResult!.CoreTargetLock,
            ExistingRunwayPrescription = first.ActivationResult.RunwayPrescription,
            ExistingRunwayCalendarProjection = first.FullRunwayCalendarProjection,
            GeCheckpointDecision = null,
            GeActivatedWeeks = null,
            CheckpointDate = request.CheckpointDate.AddDays(28),
        };
        return await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(continuation);
    }
}

public sealed class Phase4K8DCalendarAuthorityTests
{
    [Fact]
    public async Task RealComposition_IsAuthoritativeAndEveryRunwayDateMatchesExactly()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        var expected = result.RealCompositionResult!.CalendarComposition!.DatedRunwayWeeks!
            .SelectMany(w => w.StructuralOrderedSlots.Select(s => s.SessionDate)).Take(16).Order().ToArray();
        var actual = result.ActivatedSessionCalendarProjection!.Select(s => s.SessionDate).Order().ToArray();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task WeekBoundary_RemainsSeparateFromFourExecutableSessionDates()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        Assert.All(result.ActivationResult!.NewlyActivatedWeeks, week =>
        {
            Assert.NotNull(week.CalendarDates);
            Assert.Equal(4, week.SessionPrescriptions!.Count(s => s.AssignedDate is not null));
            Assert.All(week.SessionPrescriptions!, s => Assert.InRange(s.AssignedDate!.Value, week.CalendarDates!.Value.Start, week.CalendarDates.Value.End));
        });
    }

    [Fact]
    public void Request_CannotAcceptRawFinalSessionDates()
    {
        Assert.DoesNotContain(typeof(LongHorizonRollingJitCompositionRequest).GetProperties(),
            p => p.PropertyType == typeof(IReadOnlyList<DateOnly>) || p.Name.Contains("FinalSessionDate", StringComparison.Ordinal));
    }

    [Fact]
    public void Adapter_IsMappingOnlyAndContainsNoComposerOrWeekStartDateCall()
    {
        var path = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "LongHorizon", "RollingActivation", "LongHorizonRealCalendarProjectionAdapter.cs");
        var source = File.ReadAllText(path);
        Assert.DoesNotContain("CalendarComposer", source);
        Assert.DoesNotContain("WeekStartDate(", source);
        Assert.DoesNotContain("AssignedDate(", source);
    }

    [Fact]
    public async Task ProjectionCarriesRealCalendarIdentityVersionAndProvenance()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        Assert.NotEqual(Guid.Empty, result.CalendarProjectionId);
        Assert.All(result.ActivatedSessionCalendarProjection!, p =>
        {
            Assert.NotEmpty(p.CalendarCompositionIdentity);
            Assert.NotEmpty(p.CalendarCompositionVersion);
            Assert.NotEmpty(p.PreferredDayProvenance);
            Assert.NotEmpty(p.OriginalComposedSessionIdentity);
        });
    }
}

public sealed class Phase4K8DSessionIdentityMappingTests
{
    [Fact]
    public async Task FourSessionsMapOneToOneByWeekAndStableStructuralOrdinal()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        foreach (var group in result.ActivatedSessionCalendarProjection!.GroupBy(p => p.GlobalWeekNumber))
        {
            Assert.Equal(4, group.Count());
            Assert.Equal([1, 2, 3, 4], group.Select(p => p.SessionOrdinal).Order().ToArray());
        }
    }

    [Fact]
    public async Task RoleWorkoutGlobalWeekAndSegmentArePreserved()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        foreach (var week in result.ActivationResult!.NewlyActivatedWeeks)
        foreach (var session in week.SessionPrescriptions!)
        {
            var projection = result.ActivatedSessionCalendarProjection!.Single(p =>
                p.GlobalWeekNumber == week.GlobalWeekNumber && p.SessionOrdinal == session.SessionOrdinal);
            Assert.Equal(session.SessionRole, projection.SessionRole);
            Assert.Equal(session.WorkoutKey, projection.WorkoutKey);
            Assert.Equal(session.WorkoutVersion, projection.WorkoutVersion);
            Assert.Equal(week.SegmentType, projection.Segment);
        }
    }

    [Fact]
    public async Task NumericSessionsExposeTheExactProjectedDates()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        foreach (var week in result.ActivationResult!.NewlyActivatedWeeks)
        foreach (var session in week.SessionPrescriptions!)
            Assert.Equal(result.ActivatedSessionCalendarProjection!.Single(p =>
                p.GlobalWeekNumber == week.GlobalWeekNumber && p.SessionOrdinal == session.SessionOrdinal).SessionDate,
                session.AssignedDate);
    }

    [Fact]
    public async Task MissingSessionIsRejectedTyped()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        var projection = Projection(result) with { SelectedSessions = result.ActivatedSessionCalendarProjection!.Skip(1).ToArray() };
        Assert.Throws<LongHorizonActivatedCalendarAlignmentException>(() =>
            LongHorizonActivatedCalendarAlignmentValidator.Validate(result.ActivationResult!, projection,
                [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday));
    }

    [Fact]
    public async Task DuplicateDateIsRejectedTyped()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        var sessions = result.ActivatedSessionCalendarProjection!.ToArray();
        sessions[1] = sessions[1] with { SessionDate = sessions[0].SessionDate, Weekday = sessions[0].Weekday };
        Assert.Throws<LongHorizonDuplicateDatedSessionException>(() =>
            LongHorizonActivatedCalendarAlignmentValidator.Validate(result.ActivationResult!, Projection(result) with { SelectedSessions = sessions },
                [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday], DayOfWeek.Sunday));
    }

    internal static LongHorizonActivatedCalendarProjectionResult Projection(LongHorizonRollingJitCompositionResult result) => new()
    {
        ProjectionId = result.CalendarProjectionId!.Value,
        SelectedSessions = result.ActivatedSessionCalendarProjection!,
        FullRunwayProjection = result.FullRunwayCalendarProjection,
        ValidationStages = [],
    };
}

public sealed class Phase4K8DRunwayCalendarProjectionTests
{
    [Fact]
    public async Task FirstWindowRetainsPrescriptionSliceTargetLockAndLocalGlobalIdentity()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        var activation = result.ActivationResult!;
        Assert.All(result.ActivatedSessionCalendarProjection!, p =>
        {
            Assert.Equal(activation.RunwayPrescription!.PrescriptionId, p.RunwayPrescriptionId);
            Assert.Equal(activation.RunwaySlice!.SliceId, p.RunwaySliceId);
            Assert.Equal(activation.CoreTargetLock!.ContextVersion.VersionId, p.CoreTargetLockId);
            var local = p.GlobalWeekNumber - activation.RunwayPrescription.StartGlobalWeek + 1;
            Assert.InRange(local, 1, activation.RunwayPrescription.FullRunwayDurationWeeks);
        });
    }

    [Fact]
    public async Task ContinuationReusesOriginalFullCompositionDatesAndChangesOnlySliceIdentity()
    {
        var (request, first) = await Phase4K8DTestHarness.FirstAsync();
        var continuationRequest = request with
        {
            LifecycleStates = LongHorizonRollingJitCompositionOrchestratorTests.Lifecycle(28, 12),
            PreviousActivatedWindow = LongHorizonRollingJitCompositionOrchestratorTests.PriorWindow(12),
            ExistingLockedCoreTarget = first.ActivationResult!.CoreTargetLock,
            ExistingRunwayPrescription = first.ActivationResult.RunwayPrescription,
            ExistingRunwayCalendarProjection = first.FullRunwayCalendarProjection,
            GeCheckpointDecision = null, GeActivatedWeeks = null,
            CheckpointDate = request.CheckpointDate.AddDays(28),
        };
        var continuation = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator()
            .ComposeAndActivateNextWindowAsync(continuationRequest);
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, continuation.Outcome);
        Assert.Null(continuation.RealCompositionResult);
        foreach (var session in continuation.ActivatedSessionCalendarProjection!)
        {
            var original = first.FullRunwayCalendarProjection!.Sessions.Single(s =>
                s.GlobalWeekNumber == session.GlobalWeekNumber && s.SessionOrdinal == session.SessionOrdinal);
            Assert.Equal(original.SessionDate, session.SessionDate);
            Assert.Equal(first.FullRunwayCalendarProjection.ProjectionId, continuation.FullRunwayCalendarProjection!.ProjectionId);
            Assert.Equal(continuation.ActivationResult!.RunwaySlice!.SliceId, session.RunwaySliceId);
        }
    }

    [Fact]
    public async Task TerminalPreSpecificTransitionKeepsOriginalDates()
    {
        var continuation = await Phase4K8DTestHarness.ContinueAsync(12);
        var prescription = continuation.ActivationResult!.RunwayPrescription!;
        var terminal = prescription.FullWeekReferences.Single(w => w.Stage == "PreSpecificTransition");
        var dates = continuation.ActivatedSessionCalendarProjection!.Where(p => p.GlobalWeekNumber == terminal.GlobalPlanWeek).Select(p => p.SessionDate).ToArray();
        Assert.Equal(4, dates.Length);
        Assert.Equal(continuation.FullRunwayCalendarProjection!.Sessions.Where(p => p.GlobalWeekNumber == terminal.GlobalPlanWeek)
            .Select(p => p.SessionDate).Order(), dates.Order());
    }

    [Fact]
    public async Task FutureRunwayWeeksRemainPendingAndCarryNoExecutableProjection()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        var future = result.ActivationResult!.RunwayPrescription!.FullWeekReferences
            .Select(w => w.GlobalPlanWeek).Except(result.ActivationResult.NewlyActivatedWeeks.Select(w => w.GlobalWeekNumber)).ToArray();
        Assert.All(future, week => Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, result.ActivationResult.LifecycleStates[week]));
        Assert.DoesNotContain(result.ActivatedSessionCalendarProjection!, p => future.Contains(p.GlobalWeekNumber));
    }
}

public sealed class Phase4K8DCoreCalendarProjectionTests
{
    [Fact]
    public async Task CoreOnlyUsesExactRealComposedDatesAndPreservesNumberingIdentityAndContext()
    {
        var result = await Phase4K8DTestHarness.ContinueAsync(16);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.CoreWindowActivated, result.ActivationResult!.Outcome);
        Assert.All(result.ActivatedSessionCalendarProjection!, p =>
        {
            Assert.Equal(LongHorizonStructuralSegmentType.Core, p.Segment);
            Assert.Equal(result.ActivationResult.ContextVersion, p.ContextVersion);
            Assert.Equal(p.SessionDate, result.ActivationResult.NewlyActivatedWeeks.Single(w => w.GlobalWeekNumber == p.GlobalWeekNumber)
                .SessionPrescriptions!.Single(s => s.SessionOrdinal == p.SessionOrdinal).AssignedDate);
        });
    }

    [Fact]
    public async Task CoreFutureWeeksRemainPendingAndHaveNoLifecycleDates()
    {
        var result = await Phase4K8DTestHarness.ContinueAsync(16);
        var selectedEnd = result.ActivationResult!.ActivationWindow!.EndGlobalWeek;
        for (var week = selectedEnd + 1; week <= 28; week++)
            Assert.Equal(LongHorizonNumericLifecycleState.NumericPending, result.ActivationResult.LifecycleStates[week]);
        Assert.DoesNotContain(result.ActivatedSessionCalendarProjection!, p => p.GlobalWeekNumber > selectedEnd);
    }

    [Fact]
    public async Task CoreRefreshDoesNotRewritePreviouslyReturnedDates()
    {
        var first = await Phase4K8DTestHarness.ContinueAsync(16);
        var second = await Phase4K8DTestHarness.ContinueAsync(16);
        Assert.Equal(first.ActivatedSessionCalendarProjection!.Select(p => (p.GlobalWeekNumber, p.SessionOrdinal, p.SessionDate)),
            second.ActivatedSessionCalendarProjection!.Select(p => (p.GlobalWeekNumber, p.SessionOrdinal, p.SessionDate)));
    }
}

public sealed class Phase4K8DMixedCalendarContinuityTests
{
    [Fact]
    public async Task RunwayCoreWindowIsChronologicalAndBothAuthoritiesRemainExact()
    {
        var mixed = await Phase4K8DTestHarness.ContinueAsync(14);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.RunwayCoreMixedWindowActivated, mixed.ActivationResult!.Outcome);
        var runwayLast = mixed.ActivatedSessionCalendarProjection!.Where(p => p.Segment == LongHorizonStructuralSegmentType.PreparationRunway).Max(p => p.SessionDate);
        var coreFirst = mixed.ActivatedSessionCalendarProjection!.Where(p => p.Segment == LongHorizonStructuralSegmentType.Core).Min(p => p.SessionDate);
        Assert.True(runwayLast < coreFirst);
        Assert.Equal(16, mixed.ActivatedSessionCalendarProjection!.Count);
    }

    [Fact]
    public async Task RunwayCorePreferredAndLongRunDaysRemainValid()
    {
        var mixed = await Phase4K8DTestHarness.ContinueAsync(14);
        Assert.All(mixed.ActivatedSessionCalendarProjection!, p =>
            Assert.Contains(p.Weekday, new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday }));
        Assert.All(mixed.ActivatedSessionCalendarProjection!.Where(p => p.SessionRole is "LongRun" or "LONG_RUN"),
            p => Assert.Equal(DayOfWeek.Sunday, p.Weekday));
    }

    [Fact]
    public async Task GeRunwayKeepsGeObjectsUnchangedAndRunwayDatesAreReal()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var geWeeks = 8;
        var ge = request with
        {
            LifecycleStates = LongHorizonRollingJitCompositionOrchestratorTests.Lifecycle(28, geWeeks - 2),
            PreviousActivatedWindow = LongHorizonRollingJitCompositionOrchestratorTests.PriorWindow(geWeeks - 2),
            GeCheckpointDecision = new LongHorizonCheckpointDecision
            {
                DecisionId = Guid.NewGuid(), EvidenceSnapshotId = Guid.NewGuid(), Outcome = LongHorizonCheckpointOutcome.GrowthEligible,
                ValidatedLoad = request.ValidatedLoad, ActivationWindowBoundary = (geWeeks - 1, geWeeks), SafetyPriorityApplied = true, PolicyProvenance = "test",
            },
            GeActivatedWeeks = BuildGe(geWeeks - 1, geWeeks),
        };
        var result = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(ge);
        Assert.Equal(LongHorizonRollingJitActivationOutcome.GeRunwayMixedWindowActivated, result.ActivationResult!.Outcome);
        Assert.Same(ge.GeActivatedWeeks![0], result.ActivationResult.NewlyActivatedWeeks[0]);
        Assert.All(result.ActivationResult.NewlyActivatedWeeks.Where(w => w.SegmentType == LongHorizonStructuralSegmentType.PreparationRunway)
            .SelectMany(w => w.SessionPrescriptions!), s => Assert.NotNull(s.AssignedDate));
    }

    private static IReadOnlyList<ActivatedNumericWeek> BuildGe(params int[] weeks) => weeks.Select(global => new ActivatedNumericWeek
    {
        GlobalWeekNumber = global, SegmentType = LongHorizonStructuralSegmentType.GeneralEndurance,
        LifecycleState = LongHorizonNumericLifecycleState.NumericActivated, TotalWeeklyVolumeKm = 24, LongRunKm = 8,
        SessionPrescriptions =
        [
            new() { SessionRole = "LONG_RUN", DistanceKm = 8 },
            new() { SessionRole = "EASY_1", DistanceKm = 8 },
            new() { SessionRole = "EASY_2", DistanceKm = 8 },
        ],
        CalendarDates = (LongHorizonRollingJitCompositionOrchestratorTests.PlanStart.AddDays((global - 1) * 7),
            LongHorizonRollingJitCompositionOrchestratorTests.PlanStart.AddDays((global - 1) * 7 + 6)),
    }).ToArray();
}

public sealed class Phase4K8DAlignmentAtomicityDeterminismTests
{
    [Fact]
    public async Task FinalValidatorStagesRunInRequiredOrder()
    {
        var (_, result) = await Phase4K8DTestHarness.FirstAsync();
        var expected = new[] { "RealCompositionResultValidation", "SelectedSessionIdentityMapping", "SessionDateProjection", "PerWeekCalendarAlignment", "MixedBoundaryContinuity", "ActivatedNumericWeekValidation", "FinalActivationResultValidation" };
        var indices = expected.Select(stage => result.ValidationStages.ToList().IndexOf(stage)).ToArray();
        Assert.All(indices, index => Assert.True(index >= 0));
        Assert.Equal(indices.Order(), indices);
    }

    [Fact]
    public async Task IdentityMismatchBlocksAtomicallyAndRetainsDiagnostic()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var orchestrator = new LongHorizonRollingJitCompositionOrchestrator(new TamperingActivationRuntime());
        var result = await orchestrator.ComposeAndActivateNextWindowAsync(request);
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionBlocked, result.Outcome);
        Assert.Null(result.ActivationResult);
        Assert.Null(result.ActivatedSessionCalendarProjection);
        Assert.Equal(LongHorizonJitReasonCode.JitEvidenceConflictUnresolved, result.AuthoritativeReason!.Value.JitReason);
        Assert.Contains("LongHorizonCalendarIdentityMismatchException", result.InternalDiagnostic);
    }

    [Fact]
    public async Task IdenticalCompositionProducesIdenticalProjectionIdentityAndDates()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var a = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request);
        var b = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request);
        Assert.Equal(a.CalendarProjectionId, b.CalendarProjectionId);
        Assert.Equal(a.ActivatedSessionCalendarProjection!.Select(p => p.SessionDate), b.ActivatedSessionCalendarProjection!.Select(p => p.SessionDate));
        Assert.Equal(a.BoundedCoreSelection!.CoreContextId, b.BoundedCoreSelection!.CoreContextId);
    }

    [Fact]
    public async Task ChangedSelectedWindowChangesProjectionIdentity()
    {
        var (_, first) = await Phase4K8DTestHarness.FirstAsync();
        var continuation = await Phase4K8DTestHarness.ContinueAsync(12);
        Assert.NotEqual(first.CalendarProjectionId, continuation.CalendarProjectionId);
    }

    [Fact]
    public async Task ChangedPreferredDaysChangeProjectionIdentity()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var baseline = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request);
        var alternativeDays = new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var changed = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request with
        {
            PreferredDays = alternativeDays,
            CurrentAvailability = alternativeDays,
            EvidenceSnapshot = request.EvidenceSnapshot with { Availability = alternativeDays },
        });
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, changed.Outcome);
        Assert.NotEqual(baseline.CalendarProjectionId, changed.CalendarProjectionId);
    }

    [Fact]
    public async Task ChangedLongRunDayChangesProjectionIdentity()
    {
        var request = await LongHorizonRollingJitCompositionOrchestratorTests.FirstRunwayEntryRequestAsync();
        var baseline = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request);
        var changed = await LongHorizonRollingJitCompositionOrchestratorTests.Orchestrator().ComposeAndActivateNextWindowAsync(request with
        {
            LongRunDay = DayOfWeek.Friday,
        });
        Assert.Equal(LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded, changed.Outcome);
        Assert.NotEqual(baseline.CalendarProjectionId, changed.CalendarProjectionId);
    }

    [Fact]
    public void ProjectionContractsAreInternalImmutableRecordsAndNoPublicDiRegistrationExists()
    {
        Assert.False(typeof(LongHorizonActivatedSessionCalendarProjection).IsPublic);
        Assert.False(typeof(LongHorizonRealCalendarProjectionAdapter).IsPublic);
        Assert.False(typeof(LongHorizonActivatedCalendarAlignmentValidator).IsPublic);
        Assert.True(typeof(LongHorizonActivatedSessionCalendarProjection).GetMethod("<Clone>$") is not null);
    }

    [Fact]
    public void NewProjectionSourcesContainNoRandomGuidClockOrCalendarFormula()
    {
        var root = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "LongHorizon", "RollingActivation");
        foreach (var file in new[] { "LongHorizonActivatedCalendarProjectionContracts.cs", "LongHorizonRealCalendarProjectionAdapter.cs", "LongHorizonActivatedCalendarAlignmentValidator.cs" })
        {
            var source = File.ReadAllText(Path.Combine(root, file));
            Assert.DoesNotContain("Guid.NewGuid", source);
            Assert.DoesNotContain("DateTime.Now", source);
            Assert.DoesNotContain("DateTime.UtcNow", source);
            Assert.DoesNotContain("WeekStartDate(", source);
        }
    }

    private sealed class TamperingActivationRuntime : ILongHorizonRollingJitActivationRuntime
    {
        public async Task<LongHorizonRollingJitActivationResult> ResolveAndActivateNextWindowAsync(
            LongHorizonRollingJitActivationRequest request, CancellationToken cancellationToken = default)
        {
            var result = await new LongHorizonRollingJitActivationRuntime().ResolveAndActivateNextWindowAsync(request, cancellationToken);
            var weeks = result.NewlyActivatedWeeks.ToArray();
            var sessions = weeks[0].SessionPrescriptions!.ToArray();
            sessions[0] = sessions[0] with { WorkoutKey = "TAMPERED_WORKOUT" };
            weeks[0] = weeks[0] with { SessionPrescriptions = sessions };
            return result with { NewlyActivatedWeeks = weeks, ActivationWindow = result.ActivationWindow! with { Weeks = weeks } };
        }
    }
}
