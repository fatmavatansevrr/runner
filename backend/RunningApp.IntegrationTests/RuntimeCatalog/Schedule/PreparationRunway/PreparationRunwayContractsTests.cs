using RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway;
using RunningApp.Application.RuntimeCatalog.Schedule.Horizon;
using RunningApp.Domain.Enums;
using System.Text.RegularExpressions;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.PreparationRunway;

public sealed class PreparationRunwayContractsTests
{
    private static readonly DateOnly RaceDate = new(2026, 10, 12);

    // Phase 4G.6A.1: CoreStart is exactly PreferredCoreWeeks*7 (84) EXCLUSIVE
    // days before RaceDate -- matching the corrected PreparationRunwayContextValidator
    // formula and the canonical CoreHorizonClassifier exclusive-day convention.
    // Previously RaceDate.AddDays(-83), an inclusive race-anchored value that
    // disagreed with the canonical authority by exactly one day.
    private static readonly DateOnly CoreStart = RaceDate.AddDays(-84);
    private static readonly PreparationNeedProfile UnknownNeeds = new(
        PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated,
        PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated,
        PreparationRunwayNeedLevel.NotEvaluated, PreparationRunwayNeedLevel.NotEvaluated);

    private static PreparationRunwayContext Context(int runwayDays, DateOnly? coreStart = null, DateOnly? start = null, int? fullWeeks = null, int? partialDays = null) => new(
        GoalDistance.TenK,
        new(PreparationRunwayExperienceVocabulary.PlanCatalogRunningExperience, "New"),
        UnknownNeeds, null, null, null, 4,
        start ?? CoreStart.AddDays(-runwayDays), coreStart ?? CoreStart, RaceDate,
        runwayDays, fullWeeks ?? runwayDays / 7, partialDays ?? runwayDays % 7, 12,
        runwayDays == 0 ? RacePlanCompositionType.StandaloneCore : RacePlanCompositionType.PreparationRunwayPlusCore);

    private static PreparationRunwayBlockAllocation Block(int weeks, int index, PreparationRunwayBlockType type = PreparationRunwayBlockType.GeneralEndurance, PreparationRunwayPrescriptionProfile profile = PreparationRunwayPrescriptionProfile.Standard) =>
        new(type, profile, weeks, index);

    private static PreparationRunwayLeadingPartialSpan Partial(int days, PreparationRunwayBlockType type = PreparationRunwayBlockType.GeneralEndurance) =>
        new(CoreStart.AddDays(-days), CoreStart.AddDays(-1), days, type, PreparationRunwayPrescriptionProfile.Standard, true, false);

    private static RacePlanCompositionMetadata Metadata(int runwayDays, PreparationRunwayLeadingPartialSpan? span = null) => new(
        runwayDays == 0 ? RacePlanCompositionType.StandaloneCore : RacePlanCompositionType.PreparationRunwayPlusCore,
        // TotalAvailableDays is an inclusive StartDate-through-RaceDate count
        // (RacePlanCompositionMetadataValidator's own, unchanged, +1 rule) --
        // with StartDate = CoreStart.AddDays(-runwayDays) and the corrected
        // CoreStart (RaceDate - 84 exclusive days), RaceDate - StartDate =
        // 84 + runwayDays, so the inclusive total is runwayDays + 85.
        runwayDays + 85, 12, 84, CoreStart.AddDays(-runwayDays), CoreStart, RaceDate,
        runwayDays, runwayDays / 7, runwayDays % 7, span);

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(58)] // 20 total weeks + 2 days beyond the preferred core
    [InlineData(59)] // 20 total weeks + 3 days beyond the preferred core
    public void Context_DateArithmetic_ValidForBoundaryCases(int runwayDays) =>
        Assert.True(PreparationRunwayContextValidator.Validate(Context(runwayDays)).IsValid);

    [Fact]
    public void Context_ExactTwentyWeekHorizon_HasEightFullRunwayWeeks()
    {
        var context = Context(56);
        Assert.Equal(8, context.FullRunwayWeeks); Assert.Equal(0, context.LeadingPartialDays);
        Assert.True(PreparationRunwayContextValidator.Validate(context).IsValid);
    }

    [Fact]
    public void Context_RejectsStartAfterCoreStart() =>
        Assert.False(PreparationRunwayContextValidator.Validate(Context(-1, start: CoreStart.AddDays(1))).IsValid);

    [Fact]
    public void Context_RejectsOffByOneCoreStart() =>
        Assert.Contains(PreparationRunwayContextValidator.Validate(Context(0, CoreStart.AddDays(1))).Findings, f => f.Reason == PreparationRunwayPlanningReason.InvalidDateRange);

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Context_RejectsPartialDaysOutsideRange(int partial) =>
        Assert.False(PreparationRunwayContextValidator.Validate(Context(0, partialDays: partial)).IsValid);

    [Fact]
    public void Context_RejectsNegativeOptionalEvidence()
    {
        var invalid = Context(0) with { RecentWeeklyVolumeKm = -1 };
        Assert.False(PreparationRunwayContextValidator.Validate(invalid).IsValid);
    }

    [Fact]
    public void ExperienceReference_PreservesVocabularyWithoutMapping()
    {
        var catalog = new PreparationRunwayExperienceReference(PreparationRunwayExperienceVocabulary.PlanCatalogRunningExperience, "New");
        var backend = new PreparationRunwayExperienceReference(PreparationRunwayExperienceVocabulary.BackendRunningBackground, "Beginner");
        Assert.NotEqual(catalog, backend);
    }

    [Fact]
    public void Allocation_ZeroWeeksAndNoBlocks_IsValid() =>
        Assert.True(PreparationRunwayAllocationValidator.Validate(new(0, 0, 0, [])).IsValid);

    [Fact]
    public void Allocation_OneFullWeek_IsValid() =>
        Assert.True(PreparationRunwayAllocationValidator.Validate(new(7, 1, 0, [Block(1, 0)])).IsValid);

    [Fact]
    public void Allocation_MultipleContiguousBlocks_IsValid() =>
        Assert.True(PreparationRunwayAllocationValidator.Validate(new(21, 3, 0, [Block(1, 0), Block(2, 1)])).IsValid);

    [Theory]
    [MemberData(nameof(InvalidAllocations))]
    public void Allocation_RejectsStructuralViolations(object allocation) =>
        Assert.False(PreparationRunwayAllocationValidator.Validate((PreparationRunwayAllocation)allocation).IsValid);

    public static IEnumerable<object[]> InvalidAllocations()
    {
        yield return [new PreparationRunwayAllocation(14, 2, 0, [Block(1, 0)])]; // sum mismatch
        yield return [new PreparationRunwayAllocation(14, 2, 0, [Block(1, 0), Block(1, 0)])]; // duplicate
        yield return [new PreparationRunwayAllocation(14, 2, 0, [Block(1, 0), Block(1, 2)])]; // gap
        yield return [new PreparationRunwayAllocation(0, 0, 0, [Block(0, 0)])];
        yield return [new PreparationRunwayAllocation(-7, -1, 0, [Block(-1, 0)])];
        yield return [new PreparationRunwayAllocation(10, 1, 3, [Block(1, 0)])]; // arithmetic mismatch
        yield return [new PreparationRunwayAllocation(10, 2, 3, [Block(1, 0)])]; // partial cannot compensate
    }

    [Fact]
    public void Allocation_PartialSpanRequiredForOneThroughSixDays()
    {
        for (var days = 1; days <= 6; days++)
        {
            var allocation = new PreparationRunwayAllocation(7 + days, 1, days, [Block(1, 0)]);
            Assert.False(PreparationRunwayAllocationValidator.Validate(allocation).IsValid);
            Assert.True(PreparationRunwayAllocationValidator.Validate(allocation, Partial(days)).IsValid);
        }
    }

    [Fact]
    public void Allocation_PartialSpanAbsentAtZeroDays() =>
        Assert.False(PreparationRunwayAllocationValidator.Validate(new(7, 1, 0, [Block(1, 0)]), Partial(1)).IsValid);

    [Fact]
    public void Allocation_PartialDaysCannotReduceFirstBlocksFullWeeks()
    {
        var allocation = new PreparationRunwayAllocation(17, 2, 3, [Block(1, 0)]);
        Assert.Contains(PreparationRunwayAllocationValidator.Validate(allocation, Partial(3)).Findings, f => f.Detail!.Contains("cannot compensate"));
    }

    [Fact]
    public void Allocation_PartialSpanAllowsZeroSessionsAndRejectsQualityProgression()
    {
        var allocation = new PreparationRunwayAllocation(10, 1, 3, [Block(1, 0)]);
        var unsafeSpan = Partial(3) with { AllowsZeroSessions = false, AllowsQualityProgression = true };
        Assert.False(PreparationRunwayAllocationValidator.Validate(allocation, unsafeSpan).IsValid);
    }

    [Fact]
    public void Allocation_LightProfileExistsOnlyForAerobicStrength()
    {
        Assert.True(PreparationRunwayAllocationValidator.Validate(new(7, 1, 0, [Block(1, 0, PreparationRunwayBlockType.AerobicStrength, PreparationRunwayPrescriptionProfile.Light)])).IsValid);
        Assert.False(PreparationRunwayAllocationValidator.Validate(new(7, 1, 0, [Block(1, 0, PreparationRunwayBlockType.Consistency, PreparationRunwayPrescriptionProfile.Light)])).IsValid);
    }

    [Fact]
    public void Allocation_EnforcesOnlyConstraintsExplicitlySupplied()
    {
        var block = Block(2, 0) with { MinimumWeeks = 3, MaximumWeeks = 5 };
        Assert.False(PreparationRunwayAllocationValidator.Validate(new(14, 2, 0, [block])).IsValid);
    }

    [Fact]
    public void Result_ValidPlanned()
    {
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.Planned, Metadata(7), new(7, 1, 0, [Block(1, 0)]), []);
        Assert.True(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    [Fact]
    public void Result_PlannedWithoutAllocationRejected() =>
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(new(PreparationRunwayPlanningStatus.Planned, Metadata(7), null, [])).IsValid);

    [Theory]
    [InlineData((int)PreparationRunwayPlanningStatus.DecisionRequired, (int)PreparationRunwayPlanningReason.ExperienceMappingUnresolved)]
    [InlineData((int)PreparationRunwayPlanningStatus.Unsupported, (int)PreparationRunwayPlanningReason.LongRunwayCapacityExceeded)]
    [InlineData((int)PreparationRunwayPlanningStatus.InvalidInput, (int)PreparationRunwayPlanningReason.InvalidDateRange)]
    public void Result_NonSuccessStatusesRequireMatchingTypedReason(int statusValue, int reasonValue)
    {
        var status = (PreparationRunwayPlanningStatus)statusValue;
        var reason = (PreparationRunwayPlanningReason)reasonValue;
        var result = new PreparationRunwayPlanningResult(status, Metadata(7), null, [new(reason)]);
        Assert.True(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    [Fact]
    public void Result_DecisionRequiredWithoutReasonRejected() =>
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(new(PreparationRunwayPlanningStatus.DecisionRequired, Metadata(7), null, [])).IsValid);

    [Fact]
    public void Result_NotApplicableStandaloneCore_IsValid() =>
        Assert.True(PreparationRunwayPlanningResultValidator.Validate(new(PreparationRunwayPlanningStatus.NotApplicable, Metadata(0), null, [])).IsValid);

    [Fact]
    public void Result_UnsupportedCannotCarryExecutableAllocation()
    {
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.Unsupported, Metadata(7), new(7, 1, 0, [Block(1, 0)]), [new(PreparationRunwayPlanningReason.LongRunwayCapacityExceeded)]);
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    // ── TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001 fix (Phase 4G.4B.1) ─────────
    // The cross-phase audit's exact adversarial example
    // (CROSS_PHASE_4G_4A_TO_4G_4B_V_AND_BACKEND_BASELINE_INDEPENDENT_AUDIT.md
    // section 6): "CompositionType=StandaloneCore with RunwayDays=7" carried
    // by a DecisionRequired result. Metadata(7) always derives
    // CompositionType=PreparationRunwayPlusCore from its own runwayDays
    // parameter (see the Metadata() helper above), so the contradiction is
    // constructed explicitly here via `with`, exactly reproducing the literal
    // case the audit found -- not a different, weaker stand-in for it.

    [Fact]
    public void Result_DecisionRequired_RejectsLiteralAuditAdversarialCase_StandaloneCoreWithNonzeroRunwayDays()
    {
        var contradictoryMetadata = Metadata(7) with { CompositionType = RacePlanCompositionType.StandaloneCore };
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.DecisionRequired, contradictoryMetadata, null, [new(PreparationRunwayPlanningReason.ExperienceMappingUnresolved)]);
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    [Fact]
    public void Result_Unsupported_RejectsCompositionTypeRunwayDaysMismatch_StandaloneCoreWithNonzeroRunwayDays()
    {
        var contradictoryMetadata = Metadata(7) with { CompositionType = RacePlanCompositionType.StandaloneCore };
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.Unsupported, contradictoryMetadata, null, [new(PreparationRunwayPlanningReason.LongRunwayCapacityExceeded)]);
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    [Fact]
    public void Result_InvalidInput_RejectsCompositionTypeRunwayDaysMismatch_StandaloneCoreWithNonzeroRunwayDays()
    {
        var contradictoryMetadata = Metadata(7) with { CompositionType = RacePlanCompositionType.StandaloneCore };
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.InvalidInput, contradictoryMetadata, null, [new(PreparationRunwayPlanningReason.InvalidDateRange)]);
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    [Fact]
    public void Result_DecisionRequired_RejectsReverseMismatch_PreparationRunwayPlusCoreWithZeroRunwayDays()
    {
        // The reverse direction the audit also named: "or the reverse" --
        // CompositionType=PreparationRunwayPlusCore paired with RunwayDays==0.
        var contradictoryMetadata = Metadata(0) with { CompositionType = RacePlanCompositionType.PreparationRunwayPlusCore };
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.DecisionRequired, contradictoryMetadata, null, [new(PreparationRunwayPlanningReason.ExperienceMappingUnresolved)]);
        Assert.False(PreparationRunwayPlanningResultValidator.Validate(result).IsValid);
    }

    [Fact]
    public void Result_PreviouslyPassingNotApplicableAndPlannedCases_StillPassUnchanged()
    {
        // Re-confirms, in this same fix's test, that the pre-existing
        // NotApplicable-specific check was not weakened and the valid
        // Planned case is unaffected by the new general consistency rule.
        Assert.True(PreparationRunwayPlanningResultValidator.Validate(new(PreparationRunwayPlanningStatus.NotApplicable, Metadata(0), null, [])).IsValid);
        Assert.True(PreparationRunwayPlanningResultValidator.Validate(new(PreparationRunwayPlanningStatus.Planned, Metadata(7), new(7, 1, 0, [Block(1, 0)]), [])).IsValid);
    }

    [Fact]
    public void PreparationRunwayPlanningResultValidator_Validate_CalledTwiceWithIdenticalInput_ProducesIdenticalOutput()
    {
        var contradictoryMetadata = Metadata(7) with { CompositionType = RacePlanCompositionType.StandaloneCore };
        var result = new PreparationRunwayPlanningResult(PreparationRunwayPlanningStatus.DecisionRequired, contradictoryMetadata, null, [new(PreparationRunwayPlanningReason.ExperienceMappingUnresolved)]);

        var first = PreparationRunwayPlanningResultValidator.Validate(result);
        var second = PreparationRunwayPlanningResultValidator.Validate(result);

        Assert.Equal(first.IsValid, second.IsValid);
        Assert.Equal(first.Findings.Count, second.Findings.Count);
        Assert.Equal(first.Findings.Select(f => f.Reason), second.Findings.Select(f => f.Reason));
        Assert.Equal(first.Findings.Select(f => f.Detail), second.Findings.Select(f => f.Detail));
    }

    [Fact]
    public void Taxonomy_RunwayBlockValuesAreExactAndSeparate()
    {
        Assert.Equal(new[] { "Consistency", "GeneralEndurance", "AerobicStrength", "PreSpecificTransition" }, Enum.GetNames<PreparationRunwayBlockType>());
        Assert.DoesNotContain(Enum.GetNames<PreparationRunwayBlockType>(), n => new[] { "Foundation", "Build", "RaceSpecific", "Taper", "PartialBlock", "AerobicStrengthLight" }.Contains(n));
        Assert.Equal(new[] { "Standard", "Light" }, Enum.GetNames<PreparationRunwayPrescriptionProfile>());
        Assert.Equal(typeof(Enum), typeof(PreparationRunwayBlockType).BaseType);
    }

    [Fact]
    public void Neutrality_NoAllocatorRouteMapperOrSessionSelectionExistsInRunwayProductionFolder()
    {
        var repo = RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
        var root = Path.Combine(repo, "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "PreparationRunway");
        var files = Directory.GetFiles(root, "*.cs");
        Assert.Equal(2, files.Length);
        var source = string.Join('\n', files.Select(File.ReadAllText));
        foreach (var forbidden in new[] { "PreparationRunwayPlanner", "PreparationRunwayAllocator", "Materializer", "Composer", "NewToBeginner", "ProgressionStageAllocator", "CatalogWorkoutBinder", "PreferredDays" })
            Assert.DoesNotContain(forbidden, source);
    }

    [Fact]
    public void DarkReachability_NoProductionContractConsumerOutsideContractFolder()
    {
        var findings = ProductionSources()
            .SelectMany(file => FindForbiddenContractConsumption(file.Path, file.Source))
            .ToArray();

        Assert.Empty(findings);
    }

    [Fact]
    public void DarkReachability_NoDiRegistrationOrLiveInvocation()
    {
        var findings = ProductionSources()
            .SelectMany(file => FindForbiddenRuntimeWiring(file.Path, file.Source))
            .ToArray();

        Assert.Empty(findings);
    }

    [Fact]
    public void DarkReachability_ClassifierVocabularyIsAllowedWithoutContractConsumption()
    {
        var start = new DateOnly(2026, 1, 1);
        var decision = CoreHorizonClassifier.Classify(new(start, start.AddDays(15 * 7), 8, 12, 14));
        var classifierPath = Path.Combine(RepoRoot(), "backend", "RunningApp.Application", "RuntimeCatalog", "Schedule", "Horizon", "CoreHorizonClassifier.cs");
        var source = File.ReadAllText(classifierPath);

        Assert.Equal(CoreHorizonMode.PreparationRunwayPlusCore, decision.Mode);
        Assert.Contains("PreparationRunwayPlusCore", source);
        Assert.Empty(FindForbiddenContractConsumption(classifierPath, source));
        Assert.Empty(FindForbiddenRuntimeWiring(classifierPath, source));
    }

    [Theory]
    [InlineData("CatalogPreviewGenerator.cs", "PreparationRunwayPlanningResultValidator.Validate(result)")]
    [InlineData("Program.cs", "services.AddScoped<IPreparationRunwayAllocator, PreparationRunwayAllocator>()")]
    [InlineData("PublicPlanDto.cs", "public PreparationRunwayAllocation Runway { get; init; }")]
    public void DarkReachability_AdversarialWiringWouldBeDetected(string path, string source)
    {
        var findings = FindForbiddenContractConsumption(path, source)
            .Concat(FindForbiddenRuntimeWiring(path, source))
            .ToArray();

        Assert.NotEmpty(findings);
    }

    private static IReadOnlyList<(string Path, string Source)> ProductionSources()
    {
        var repo = RepoRoot();
        var allowedRoot = Path.Combine("Schedule", "PreparationRunway");
        return new[] { "RunningApp.Application", "RunningApp.Api", "RunningApp.Infrastructure", "RunningApp.Persistence" }
            .SelectMany(project => Directory.GetFiles(Path.Combine(repo, "backend", project), "*.cs", SearchOption.AllDirectories))
            .Where(path => !path.Contains("\\bin\\") && !path.Contains("\\obj\\") && !path.Contains(allowedRoot))
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .ToArray();
    }

    private static IEnumerable<string> FindForbiddenContractConsumption(string path, string source)
    {
        var concreteContractTypes = new[]
        {
            "PreparationRunwayContext", "PreparationRunwayAllocation", "PreparationRunwayPlanningResult",
            "PreparationRunwayBlockAllocation", "PreparationRunwayLeadingPartialSpan", "PreparationNeedProfile",
            "RacePlanCompositionMetadata",
        };
        return concreteContractTypes
            .Where(symbol => Regex.IsMatch(source, $@"\b{Regex.Escape(symbol)}\b"))
            .Select(symbol => $"{path}: consumes {symbol}");
    }

    private static IEnumerable<string> FindForbiddenRuntimeWiring(string path, string source)
    {
        var behaviorSymbols = new[]
        {
            "PreparationRunwayPlanningResultValidator", "PreparationRunwayAllocationValidator",
            "PreparationRunwayContextValidator", "PreparationRunwayPlanner", "PreparationRunwayAllocator",
            "PreparationRunwayMaterializer", "PreparationRunwayComposer", "IPreparationRunwayAllocator",
        };
        return behaviorSymbols
            .Where(symbol => Regex.IsMatch(source, $@"\b{Regex.Escape(symbol)}\b"))
            .Select(symbol => $"{path}: wires or invokes {symbol}");
    }

    private static string RepoRoot() =>
        RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
}
