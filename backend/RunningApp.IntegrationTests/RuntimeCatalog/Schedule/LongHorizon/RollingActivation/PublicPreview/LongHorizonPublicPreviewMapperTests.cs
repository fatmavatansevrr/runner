using System.Reflection;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon.RollingActivation.PublicPreview;

internal static class LongHorizonPublicPreviewFixture
{
    internal static LongHorizonPublicPreviewMapperInput InputFor(LongHorizonFullDarkLifecycleState state, Guid previewId) =>
        new()
        {
            PreviewId = previewId,
            GoalType = "Race",
            GoalDistance = "TenK",
            State = state,
            StartDate = LongHorizonFullLifecycleTestFixture.StartDate,
            EstimatedEndDate = LongHorizonFullLifecycleTestFixture.StartDate.AddDays(state.StructuralRoadmap.TotalWeeks * 7),
            DaysPerWeek = 4,
            PreferredDays = LongHorizonFullLifecycleTestFixture.PreferredDays,
            LongRunDay = DayOfWeek.Sunday,
            ProvenanceSummary = LongHorizonPublicProvenance.GeneratedFromInitialProfile,
        };

    internal static async Task<LongHorizonFullDarkLifecycleState> InitialStateAsync(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        return result.StateSnapshots[0];
    }

    internal static async Task<LongHorizonFullDarkLifecycleState> FinalStateAsync(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        return result.FinalState!;
    }

    internal static async Task<LongHorizonFullDarkLifecycleState> BlockedStateAsync()
    {
        var scenario = await LongHorizonFullLifecycleTestFixture.ScenarioAsync(
            28,
            transform: (ordinal, row) => ordinal == 1
                ? row with { SafetyState = LongHorizonSafetyState.UnresolvedSafetyCritical }
                : row,
            expectedBlockedOrdinal: 1);
        var result = await new LongHorizonFullDarkLifecycleHarness().RunLifecycleAsync(scenario);
        Assert.Equal(LongHorizonFullDarkLifecycleOutcome.BlockedAsExpected, result.Outcome);
        return result.FinalState!;
    }
}

public sealed class LongHorizonPublicPreviewContractShapeTests
{
    [Fact]
    public async Task TwentyOneWeekRoadmapContainsTwentyOneWeeks()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(21), Guid.NewGuid()));
        Assert.Equal(21, contract.StructuralRoadmap.Count);
        Assert.Equal(21, contract.TotalWeeks);
        LongHorizonPublicPreviewContractValidator.Validate(contract);
    }

    [Fact]
    public async Task FiftyTwoWeekRoadmapContainsFiftyTwoWeeks()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(52), Guid.NewGuid()));
        Assert.Equal(52, contract.StructuralRoadmap.Count);
        LongHorizonPublicPreviewContractValidator.Validate(contract);
    }

    [Fact]
    public async Task RoadmapWeeksAreContiguous()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(29), Guid.NewGuid()));
        var weeks = contract.StructuralRoadmap.Select(r => r.GlobalWeek).ToList();
        Assert.Equal(Enumerable.Range(1, 29), weeks);
    }

    [Fact]
    public async Task PhaseOrderIsGeneralEnduranceThenRunwayThenCore()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(29), Guid.NewGuid()));
        var order = contract.StructuralRoadmap.OrderBy(r => r.GlobalWeek).Select(r => r.Phase).Distinct().ToList();
        Assert.Equal([LongHorizonPublicPhase.GeneralEndurance, LongHorizonPublicPhase.PreparationRunway, LongHorizonPublicPhase.Core], order);
    }

    [Fact]
    public async Task PhaseDurationsMatchDarkRoadmap()
    {
        var state = await LongHorizonPublicPreviewFixture.InitialStateAsync(29);
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        Assert.Equal(state.StructuralRoadmap.GeneralEnduranceWeeks,
            contract.StructuralRoadmap.Count(r => r.Phase == LongHorizonPublicPhase.GeneralEndurance));
        Assert.Equal(8, contract.StructuralRoadmap.Count(r => r.Phase == LongHorizonPublicPhase.PreparationRunway));
        Assert.Equal(12, contract.StructuralRoadmap.Count(r => r.Phase == LongHorizonPublicPhase.Core));
    }

    [Fact]
    public async Task ContractVersionPreviewReadinessAndConfirmationReadinessArePresent()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(21), Guid.NewGuid()));
        Assert.Equal(1, contract.ContractVersion);
        Assert.Equal(LongHorizonPreviewReadiness.ReadyForPublicPreview, contract.PreviewReadiness);
        Assert.Equal(LongHorizonConfirmationReadiness.NotReadyForConfirmation, contract.ConfirmationReadiness);
    }
}

public sealed class LongHorizonPublicPreviewInitialPreviewTests
{
    [Fact]
    public async Task OnlyFirstActivatedWindowHasNumericDetails()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(29), Guid.NewGuid()));
        var available = contract.StructuralRoadmap.Where(r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Available).ToList();
        Assert.True(available.Count is > 0 and <= 4);
        Assert.All(available, r => Assert.True(r.NumericDetailsAvailable));
    }

    [Fact]
    public async Task FutureWeeksArePendingAcrossAllThreePhases()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(29), Guid.NewGuid()));
        var pending = contract.StructuralRoadmap.Where(r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Pending).ToList();
        Assert.Contains(pending, r => r.Phase == LongHorizonPublicPhase.GeneralEndurance);
        Assert.Contains(pending, r => r.Phase == LongHorizonPublicPhase.PreparationRunway);
        Assert.Contains(pending, r => r.Phase == LongHorizonPublicPhase.Core);
        Assert.All(pending, r => Assert.False(r.NumericDetailsAvailable));
        Assert.All(pending, r => Assert.False(r.IsExecutable));
    }

    [Fact]
    public async Task StructuralRoadmapRemainsCompleteRegardlessOfActivationProgress()
    {
        var initial = await LongHorizonPublicPreviewFixture.InitialStateAsync(29);
        var final = await LongHorizonPublicPreviewFixture.FinalStateAsync(29);
        var initialContract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(initial, Guid.NewGuid()));
        var finalContract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(final, Guid.NewGuid()));
        Assert.Equal(initialContract.StructuralRoadmap.Count, finalContract.StructuralRoadmap.Count);
        Assert.All(finalContract.StructuralRoadmap, r => Assert.True(
            r.LifecycleStatus is LongHorizonPublicLifecycleStatus.Completed or LongHorizonPublicLifecycleStatus.Available));
    }
}

public sealed class LongHorizonPublicPreviewExecutableWeekTests
{
    [Fact]
    public async Task ActivatedValuesMatchDarkActivatedNumericWeekExactly()
    {
        var state = await LongHorizonPublicPreviewFixture.InitialStateAsync(29);
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        foreach (var week in contract.CurrentExecutableWeeks)
        {
            var dark = state.ActivatedWeeks[week.GlobalWeek];
            Assert.Equal(dark.TotalWeeklyVolumeKm!.Value, week.WeeklyVolumeKm);
            Assert.Equal(dark.LongRunKm!.Value, week.LongRunVolumeKm);
            Assert.Equal(dark.CalendarDates!.Value.Start, week.WeekStartDate);
            Assert.Equal(dark.CalendarDates!.Value.End, week.WeekEndDate);
            Assert.Equal(dark.SessionPrescriptions!.Count, week.Sessions.Count);
        }
    }

    [Fact]
    public async Task SessionDatesMatchExactAssignedDate()
    {
        var state = await LongHorizonPublicPreviewFixture.InitialStateAsync(29);
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        foreach (var week in contract.CurrentExecutableWeeks)
        {
            var darkSessions = state.ActivatedWeeks[week.GlobalWeek].SessionPrescriptions!;
            foreach (var session in week.Sessions)
            {
                Assert.Contains(darkSessions, d => d.AssignedDate == session.SessionDate && d.SessionRole == session.SessionRole);
            }
        }
    }

    [Fact]
    public async Task WeekBoundariesArePreserved()
    {
        var state = await LongHorizonPublicPreviewFixture.InitialStateAsync(29);
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        Assert.All(contract.CurrentExecutableWeeks, w => Assert.All(w.Sessions,
            s => Assert.InRange(s.SessionDate.ToDateTime(TimeOnly.MinValue),
                w.WeekStartDate.ToDateTime(TimeOnly.MinValue), w.WeekEndDate.ToDateTime(TimeOnly.MinValue))));
    }

    [Fact]
    public async Task NoInternalIdentifiersAppearOnExecutableWeeks()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(29), Guid.NewGuid()));
        foreach (var week in contract.CurrentExecutableWeeks)
        {
            var props = week.GetType().GetProperties();
            Assert.DoesNotContain(props, p => p.Name.Contains("PrescriptionId", StringComparison.Ordinal)
                || p.Name.Contains("ContextVersion", StringComparison.Ordinal) || p.Name.Contains("TargetLock", StringComparison.Ordinal));
        }
    }
}

public sealed class LongHorizonPublicPreviewLifecycleSnapshotTests
{
    [Theory]
    [InlineData(25)]
    [InlineData(29)]
    [InlineData(40)]
    public async Task RepeatedSnapshotsAlongLifecycleAllMapAndValidate(int totalWeeks)
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(totalWeeks);
        foreach (var snapshot in result.StateSnapshots)
        {
            var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(snapshot, Guid.NewGuid()));
            LongHorizonPublicPreviewContractValidator.Validate(contract);
        }
    }

    [Fact]
    public async Task CompletedHistoryMapsToCompletedStatus()
    {
        var final = await LongHorizonPublicPreviewFixture.FinalStateAsync(21);
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(final, Guid.NewGuid()));
        Assert.Contains(contract.StructuralRoadmap, r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Completed);
    }

    [Fact]
    public async Task FutureSuffixRemainsNonExecutableInEveryIntermediateSnapshot()
    {
        var result = await LongHorizonFullLifecycleTestFixture.RunAsync(29);
        var stillIncomplete = result.StateSnapshots.Where(s => s.ActivatedWeeks.Count < 29).ToList();
        Assert.NotEmpty(stillIncomplete);
        foreach (var snapshot in stillIncomplete)
        {
            var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(snapshot, Guid.NewGuid()));
            Assert.Contains(contract.StructuralRoadmap, r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Pending);
        }
    }
}

public sealed class LongHorizonPublicPreviewBlockedStateTests
{
    [Fact]
    public async Task BlockMapsToExactlyOnePublicCategory()
    {
        var state = await LongHorizonPublicPreviewFixture.BlockedStateAsync();
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        Assert.NotNull(contract.BlockedState);
        Assert.Equal(LongHorizonPublicBlockedReasonCategory.SafetyReviewRequired, contract.BlockedState!.ReasonCategory);
    }

    [Fact]
    public async Task SafetyBlockIsNotRetryEligible()
    {
        var state = await LongHorizonPublicPreviewFixture.BlockedStateAsync();
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        Assert.False(contract.BlockedState!.RetryEligible);
    }

    [Fact]
    public async Task BlockedWindowHasNoExecutableSessionPayload()
    {
        var state = await LongHorizonPublicPreviewFixture.BlockedStateAsync();
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        Assert.Empty(contract.CurrentExecutableWeeks);
        Assert.Equal(0, contract.CurrentExecutableWeekCount);
    }

    [Fact]
    public async Task NoInternalDiagnosticIsExposedOnBlockedState()
    {
        var state = await LongHorizonPublicPreviewFixture.BlockedStateAsync();
        var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, Guid.NewGuid()));
        var props = contract.BlockedState!.GetType().GetProperties().Select(p => p.Name);
        Assert.DoesNotContain(props, n => n.Contains("Diagnostic", StringComparison.Ordinal) || n.Contains("Trace", StringComparison.Ordinal));
    }

    [Fact]
    public void ReasonMappingCoversAllJitReasons()
    {
        foreach (var reason in Enum.GetValues<LongHorizonJitReasonCode>())
        {
            var mapped = LongHorizonPublicPreviewMapper.MapReasonToCategory(LongHorizonReasonCode.FromJit(reason));
            Assert.True(Enum.IsDefined(mapped));
        }
    }

    [Fact]
    public void ReasonMappingCoversAllCheckpointReasons()
    {
        foreach (var reason in Enum.GetValues<LongHorizonCheckpointReasonCode>())
        {
            var mapped = LongHorizonPublicPreviewMapper.MapReasonToCategory(LongHorizonReasonCode.FromCheckpoint(reason));
            Assert.True(Enum.IsDefined(mapped));
        }
    }
}

public sealed class LongHorizonPublicPreviewConfirmationReadinessTests
{
    [Fact]
    public async Task InitialLongHorizonPreviewIsNotFalselyConfirmable()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(21), Guid.NewGuid()));
        Assert.Equal(LongHorizonConfirmationReadiness.NotReadyForConfirmation, contract.ConfirmationReadiness);
    }

    [Fact]
    public async Task PreviewReadinessAndConfirmationReadinessAreIndependent()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(21), Guid.NewGuid()));
        Assert.Equal(LongHorizonPreviewReadiness.ReadyForPublicPreview, contract.PreviewReadiness);
        Assert.Equal(LongHorizonConfirmationReadiness.NotReadyForConfirmation, contract.ConfirmationReadiness);
    }

    [Fact]
    public void ExistingLegacyPreviewResponseTypeIsUnchangedByThisPhase()
    {
        var type = typeof(RunningApp.Application.DTOs.Plan.GeneratePreviewResponse);
        var props = type.GetProperties().Select(p => p.Name).ToHashSet();
        Assert.Equal(new HashSet<string> { "PreviewId", "TemplateId", "GoalType", "GoalDistance", "Level", "DaysPerWeek", "Unit", "Weeks", "FallbackUsed", "FallbackReason", "Lifecycle" }, props);
    }

    [Fact]
    public void GeneratePreviewResponseHasNoLongHorizonField()
    {
        var type = typeof(RunningApp.Application.DTOs.Plan.GeneratePreviewResponse);
        Assert.DoesNotContain(type.GetProperties(), p => p.Name.Contains("LongHorizon", StringComparison.Ordinal));
    }
}

public sealed class LongHorizonPublicPreviewLeakageGuardTests
{
    private static readonly string[] ForbiddenTypeNames =
    [
        "ImmutablePreparationRunwayPrescription",
        "PreparationRunwayPrescriptionId",
        "PreparationRunwayTargetLockScope",
        "LongHorizonLockedCoreWeekOneTarget",
        "BoundedPreparationRunwayPrescriptionSlice",
        "ValidatedSustainableLoad",
        "LongHorizonCheckpointDecision",
        "RuntimeConditionResolutionResult",
        "LongHorizonLifecycleAuditEvent",
        "LongHorizonContextVersion",
    ];

    [Fact]
    public void PublicContractGraphExposesNoForbiddenInternalTypes()
    {
        var visited = new HashSet<Type>();
        AssertNoForbiddenTypes(typeof(LongHorizonPlanPreviewContract), visited);
    }

    private static void AssertNoForbiddenTypes(Type type, HashSet<Type> visited)
    {
        if (!visited.Add(type) || type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true)
        {
            return;
        }

        Assert.DoesNotContain(ForbiddenTypeNames, forbidden => type.Name.Contains(forbidden, StringComparison.Ordinal));

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propType = prop.PropertyType;
            if (propType.IsGenericType)
            {
                foreach (var arg in propType.GetGenericArguments())
                {
                    if (arg.Namespace?.StartsWith("RunningApp", StringComparison.Ordinal) == true)
                    {
                        AssertNoForbiddenTypes(arg, visited);
                    }
                }
            }
            else if (propType.Namespace?.StartsWith("RunningApp", StringComparison.Ordinal) == true)
            {
                AssertNoForbiddenTypes(propType, visited);
            }
        }
    }

    [Fact]
    public async Task NoFutureWeeklyVolumeLeaksForPendingWeeks()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(29), Guid.NewGuid()));
        // Pending rows carry no numeric field at all -- structurally guaranteed by
        // LongHorizonStructuralRoadmapWeekContract having no volume/session properties.
        var pendingRowType = typeof(LongHorizonStructuralRoadmapWeekContract);
        Assert.DoesNotContain(pendingRowType.GetProperties(), p => p.Name.Contains("Volume", StringComparison.Ordinal));
        Assert.DoesNotContain(pendingRowType.GetProperties(), p => p.Name.Contains("Session", StringComparison.Ordinal));
        Assert.Contains(contract.StructuralRoadmap, r => r.LifecycleStatus == LongHorizonPublicLifecycleStatus.Pending);
    }
}

public sealed class LongHorizonPublicPreviewDeterminismTests
{
    [Fact]
    public async Task IdenticalDarkSnapshotProducesIdenticalPublicContract()
    {
        var state = await LongHorizonPublicPreviewFixture.InitialStateAsync(29);
        var id = Guid.NewGuid();
        var a = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, id));
        var b = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(state, id));
        Assert.Equivalent(a, b, strict: true);
    }

    [Fact]
    public async Task HistoricalValuesRemainUnchangedBetweenInitialAndFinalMapping()
    {
        var initial = await LongHorizonPublicPreviewFixture.InitialStateAsync(21);
        var final = await LongHorizonPublicPreviewFixture.FinalStateAsync(21);
        var initialContract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(initial, Guid.NewGuid()));
        var finalContract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(final, Guid.NewGuid()));
        foreach (var initialWeek in initialContract.CurrentExecutableWeeks)
        {
            var finalRow = finalContract.StructuralRoadmap.Single(r => r.GlobalWeek == initialWeek.GlobalWeek);
            Assert.Equal(initialWeek.WeekStartDate, finalRow.StructuralStartDate);
        }
    }
}

public sealed class LongHorizonPublicPreviewProfileAndHorizonTests
{
    [Fact]
    public async Task BothProfilesMapAndValidate()
    {
        foreach (var profile in new[] { ReadinessProfile.ConsistencyNeeded, ReadinessProfile.CoreEntryReady })
        {
            var result = await LongHorizonFullLifecycleTestFixture.RunAsync(21, profile);
            var contract = LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewFixture.InputFor(result.StateSnapshots[0], Guid.NewGuid()));
            LongHorizonPublicPreviewContractValidator.Validate(contract);
            Assert.Equal(profile.ToString(), contract.ReadinessProfile);
        }
    }

    [Fact]
    public async Task LightweightRoadmapPayloadRemainsValidAtFiftyTwoWeeks()
    {
        var contract = LongHorizonPublicPreviewMapper.Map(
            LongHorizonPublicPreviewFixture.InputFor(await LongHorizonPublicPreviewFixture.InitialStateAsync(52), Guid.NewGuid()));
        LongHorizonPublicPreviewContractValidator.Validate(contract);
        Assert.Equal(52, contract.StructuralRoadmap.Count);
        Assert.True(contract.CurrentExecutableWeeks.Count <= 4);
    }
}

public sealed class LongHorizonPublicPreviewWiringStatusTests
{
    [Fact]
    public void NoEndpointReferencesThePublicPreviewMapper()
    {
        var controllerPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Controllers", "PlansController.cs");
        var text = File.ReadAllText(controllerPath);
        Assert.DoesNotContain(nameof(LongHorizonPublicPreviewMapper), text);
    }

    [Fact]
    public void NoPublicDiRegistrationExistsForThePublicPreviewMapper()
    {
        var programPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "backend", "RunningApp.Api", "Program.cs");
        var text = File.ReadAllText(programPath);
        Assert.DoesNotContain(nameof(LongHorizonPublicPreviewMapper), text);
    }

    [Fact]
    public void MapperAndValidatorRemainInternal_WhileEndpointContractIsPublic()
    {
        Assert.False(typeof(LongHorizonPublicPreviewMapper).IsPublic);
        Assert.True(typeof(LongHorizonPlanPreviewContract).IsPublic);
        Assert.False(typeof(LongHorizonPublicPreviewContractValidator).IsPublic);
    }
}
