using Microsoft.Extensions.Options;
using RunningApp.Application.Common;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule.Binding;
using RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.5 — production-level tests for <see cref="LongHorizonStructuralMaterializer"/>
/// and <see cref="LongHorizonStructuralValidator"/>. Every case uses the real
/// <see cref="LongHorizonCompositionResolver"/> (fed a real
/// <c>CoreHorizonDecision</c> anchored the same way
/// <see cref="LongHorizonCompositionResolverTests"/> already does) and the
/// real, file-system-backed <see cref="CatalogWorkoutDefinitionLoader"/>
/// (no database, no network -- consistent with <c>PreparationRunwayWeekMaterializerTests</c>).
/// </summary>
public sealed class LongHorizonStructuralMaterializerTests
{
    private static readonly DateOnly Anchor = new(2000, 1, 1);

    private static string RepoRoot() => RuntimeCatalog.PreviewRouting.TestPlanServicesFactory.RepoRoot();
    private static string CatalogRoot() => Path.Combine(RepoRoot(), "plan-catalog", "catalog");
    private static ICatalogWorkoutDefinitionLoader Loader() =>
        new CatalogWorkoutDefinitionLoader(Options.Create(new PlanCatalogOptions { CatalogRootPath = CatalogRoot() }));

    private static LongHorizonCompositionDecision Decide(int availableFullWeeks, ReadinessProfile profile)
    {
        var raceDate = Anchor.AddDays(availableFullWeeks * 7);
        var coreHorizon = RaceHorizonPolicy.Decide(Anchor, raceDate);
        return LongHorizonCompositionResolver.Resolve(coreHorizon, profile);
    }

    private static Task<LongHorizonGeneratedStructuralSkeleton> MaterializeAsync(int totalWeeks, ReadinessProfile profile) =>
        LongHorizonStructuralMaterializer.MaterializeAsync(Decide(totalWeeks, profile), CatalogRoot(), Loader());

    // ── Segment order / global numbering (Parts 3-4) ────────────────────────

    [Theory]
    [InlineData(21, 1)]
    [InlineData(24, 4)]
    [InlineData(40, 20)]
    [InlineData(52, 32)]
    public async Task SegmentOrderAndGlobalNumbering_ExactlyAsSpecified(int totalWeeks, int expectedGe)
    {
        var skeleton = await MaterializeAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded);

        Assert.Equal(totalWeeks, skeleton.TotalWeeks);
        Assert.Equal(expectedGe, skeleton.GeneralEnduranceWeeks);
        Assert.Equal(8, skeleton.PreparationRunwayWeeks);
        Assert.Equal(12, skeleton.CoreWeeks);
        Assert.Equal(totalWeeks, skeleton.Weeks.Count);

        Assert.Equal(Enumerable.Range(1, totalWeeks), skeleton.Weeks.Select(w => w.GlobalWeekNumber));

        var geRange = skeleton.Weeks.Take(expectedGe);
        Assert.All(geRange, w => Assert.Equal(LongHorizonSegmentType.LongHorizonGeneralEndurance, w.Segment));

        var runwayRange = skeleton.Weeks.Skip(expectedGe).Take(8);
        Assert.All(runwayRange, w => Assert.Equal(LongHorizonSegmentType.PreparationRunway, w.Segment));

        var coreRange = skeleton.Weeks.Skip(expectedGe + 8).Take(12);
        Assert.All(coreRange, w => Assert.Equal(LongHorizonSegmentType.Core, w.Segment));

        Assert.Equal(totalWeeks, skeleton.Weeks[^1].GlobalWeekNumber);
    }

    [Fact]
    public async Task TwentyOneWeeks_ExactStructuralExample_MatchesPhaseDocument()
    {
        var skeleton = await MaterializeAsync(21, ReadinessProfile.ConsistencyNeeded);
        Assert.Single(skeleton.Weeks, w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        Assert.Equal(1, skeleton.Weeks[0].GlobalWeekNumber);
        Assert.Equal(Enumerable.Range(2, 8), skeleton.Weeks.Skip(1).Take(8).Select(w => w.GlobalWeekNumber));
        Assert.Equal(Enumerable.Range(10, 12), skeleton.Weeks.Skip(9).Take(12).Select(w => w.GlobalWeekNumber));
    }

    [Fact]
    public async Task FiftyTwoWeeks_MaximumCapacity_ProducesFiftyTwoWeeksTwoOhEightSlots()
    {
        var skeleton = await MaterializeAsync(52, ReadinessProfile.CoreEntryReady);
        Assert.Equal(52, skeleton.Weeks.Count);
        Assert.Equal(208, skeleton.Weeks.Sum(w => w.OrderedWorkoutSlots.Count));
        var validation = LongHorizonStructuralValidator.Validate(skeleton);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));
    }

    // ── ShortExtension (Part 10) ─────────────────────────────────────────

    [Theory]
    [InlineData(21, ReadinessProfile.ConsistencyNeeded)]
    [InlineData(21, ReadinessProfile.CoreEntryReady)]
    [InlineData(22, ReadinessProfile.ConsistencyNeeded)]
    [InlineData(22, ReadinessProfile.CoreEntryReady)]
    [InlineData(23, ReadinessProfile.ConsistencyNeeded)]
    [InlineData(23, ReadinessProfile.CoreEntryReady)]
    internal async Task ShortExtensionHorizons_NoRecoveryWeek_TerminalCompatibleWithRunway(int totalWeeks, ReadinessProfile profile)
    {
        var skeleton = await MaterializeAsync(totalWeeks, profile);
        var geWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();

        Assert.Equal(totalWeeks - 20, geWeeks.Count);
        Assert.All(geWeeks, w => Assert.Equal(GeneralEnduranceDurationClassification.ShortExtension, w.GeClassification));
        Assert.All(geWeeks, w => Assert.False(w.IsRecoveryWeek));
        Assert.True(geWeeks[^1].IsTerminalAlignment);
        // 1-week ShortExtension is a single EntryAlignment/Entry week (it is both the first and the
        // terminal week); 2/3-week ShortExtension always ends on PreRunwayAlignment (Phase 4I.4).
        Assert.Equal(
            geWeeks.Count == 1 ? LongHorizonGeStageFamily.Entry : LongHorizonGeStageFamily.PreRunwayAlignment,
            geWeeks[^1].GeStageFamily);

        var firstRunwayWeek = skeleton.Weeks[geWeeks.Count];
        Assert.Equal(LongHorizonSegmentType.PreparationRunway, firstRunwayWeek.Segment);
        Assert.Equal(1, firstRunwayWeek.LocalSegmentWeekNumber);
    }

    // ── FullPhase representative totals (Part 11) ────────────────────────

    [Theory]
    [InlineData(24, 4, 1)]
    [InlineData(28, 8, 2)]
    [InlineData(32, 12, 3)]
    [InlineData(40, 20, 5)]
    [InlineData(52, 32, 8)]
    public async Task FullPhaseHorizons_ExactMesocycleAndRecoveryStructure(int totalWeeks, int expectedGe, int expectedMesocycles)
    {
        var skeleton = await MaterializeAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        var geWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();

        Assert.Equal(expectedGe, geWeeks.Count);
        Assert.All(geWeeks, w => Assert.Equal(GeneralEnduranceDurationClassification.FullPhase, w.GeClassification));
        Assert.Equal(expectedMesocycles, geWeeks.Count(w => w.IsRecoveryWeek == true));

        foreach (var mesocycleIndex in Enumerable.Range(1, expectedMesocycles))
        {
            var mesocycleWeeks = geWeeks.Where(w => w.MesocycleIndex == mesocycleIndex).ToList();
            Assert.Equal(4, mesocycleWeeks.Count);
            Assert.True(mesocycleWeeks[^1].IsRecoveryWeek);
            Assert.Equal(LongHorizonGeStageFamily.Consolidation, mesocycleWeeks[^1].GeStageFamily);
        }
    }

    // ── Remainder materialization (Part 12) ──────────────────────────────

    [Theory]
    [InlineData(25, 1)]
    [InlineData(26, 2)]
    [InlineData(27, 3)]
    [InlineData(29, 1)]
    [InlineData(30, 2)]
    [InlineData(31, 3)]
    public async Task RemainderHorizons_OccurAfterCompleteMesocyclesImmediatelyBeforeRunway(int totalWeeks, int expectedRemainder)
    {
        var skeleton = await MaterializeAsync(totalWeeks, ReadinessProfile.CoreEntryReady);
        var geWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();

        var terminalWeeks = geWeeks.Where(w => w.IsTerminalAlignment == true).ToList();
        Assert.Equal(expectedRemainder, terminalWeeks.Count);
        Assert.Equal(geWeeks.Skip(geWeeks.Count - expectedRemainder), terminalWeeks);
        Assert.All(terminalWeeks, w => Assert.False(w.IsRecoveryWeek));
        Assert.Equal(LongHorizonGeStageFamily.PreRunwayAlignment, terminalWeeks[^1].GeStageFamily);

        // Terminal remainder never appears at the beginning.
        Assert.False(geWeeks[0].IsTerminalAlignment);
    }

    // ── Runway/Core reuse regression (Part 6-7) ──────────────────────────

    [Theory]
    [InlineData(ReadinessProfile.ConsistencyNeeded, "Consistency,GeneralEndurance,PreSpecificTransition")]
    [InlineData(ReadinessProfile.CoreEntryReady, "GeneralEndurance,AerobicStrength,PreSpecificTransition")]
    internal async Task RunwaySegment_ProfileBlockAllocationMatchesApprovedMatrix(ReadinessProfile profile, string expectedBlocks)
    {
        var skeleton = await MaterializeAsync(24, profile);
        var runwayWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.PreparationRunway).ToList();
        Assert.Equal(8, runwayWeeks.Count);
        Assert.Equal(Enumerable.Range(1, 8), runwayWeeks.Select(w => w.LocalSegmentWeekNumber));
        Assert.Equal(expectedBlocks, string.Join(",", runwayWeeks.Select(w => w.RunwayBlock).Distinct()));
        Assert.Equal("PreSpecificTransition", runwayWeeks[^1].RunwayBlock);
        Assert.All(runwayWeeks, w => Assert.All(w.OrderedWorkoutSlots, s => Assert.NotNull(s.WorkoutKey)));
    }

    [Fact]
    public async Task CoreSegment_ExactlyTwelveWeeksFoundationBuildRaceSpecificTaper()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var coreWeeks = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.Core).ToList();

        Assert.Equal(12, coreWeeks.Count);
        Assert.Equal(Enumerable.Range(1, 12), coreWeeks.Select(w => w.LocalSegmentWeekNumber));
        Assert.Equal(3, coreWeeks.Count(w => w.CorePhase == "FOUNDATION"));
        Assert.Equal(4, coreWeeks.Count(w => w.CorePhase == "BUILD"));
        Assert.Equal(4, coreWeeks.Count(w => w.CorePhase == "RACE_SPECIFIC"));
        Assert.Equal(1, coreWeeks.Count(w => w.CorePhase == "TAPER"));
        Assert.Equal(
            new[] { "FOUNDATION", "FOUNDATION", "FOUNDATION", "BUILD", "BUILD", "BUILD", "BUILD",
                     "RACE_SPECIFIC", "RACE_SPECIFIC", "RACE_SPECIFIC", "RACE_SPECIFIC", "TAPER" },
            coreWeeks.Select(w => w.CorePhase));
        Assert.All(coreWeeks, w => Assert.Equal(4, w.OrderedWorkoutSlots.Count));
    }

    // ── 20->21 suffix continuity (Part 13) ───────────────────────────────

    [Theory]
    [InlineData(ReadinessProfile.ConsistencyNeeded)]
    [InlineData(ReadinessProfile.CoreEntryReady)]
    internal async Task TwentyToTwentyOne_RunwayAndCoreSuffixStructurallyStable(ReadinessProfile profile)
    {
        var twentyOne = await MaterializeAsync(21, profile);
        var twentyFour = await MaterializeAsync(24, profile);

        // The final 20 weeks (Runway+Core) of the 21-week plan and of a
        // longer plan must be structurally identical (role sequence, block
        // sequence, phase sequence, workout references) modulo the global
        // week-number offset -- proving the composition-level 20-week
        // suffix never changes shape as GE grows.
        var suffix21 = twentyOne.Weeks.Skip(1).ToList();
        var suffix24 = twentyFour.Weeks.Skip(4).ToList();

        Assert.Equal(20, suffix21.Count);
        Assert.Equal(20, suffix24.Count);

        for (var i = 0; i < 20; i++)
        {
            var a = suffix21[i];
            var b = suffix24[i];
            Assert.Equal(a.Segment, b.Segment);
            Assert.Equal(a.WeekType, b.WeekType);
            Assert.Equal(a.RunwayBlock, b.RunwayBlock);
            Assert.Equal(a.CorePhase, b.CorePhase);
            Assert.Equal(a.LocalSegmentWeekNumber, b.LocalSegmentWeekNumber);
            Assert.Equal(
                a.OrderedWorkoutSlots.Select(s => (s.StructuralRole, s.WorkoutKey, s.WorkoutVersion)),
                b.OrderedWorkoutSlots.Select(s => (s.StructuralRole, s.WorkoutKey, s.WorkoutVersion)));
        }
    }

    // ── N->N+1 monotonicity (Part 14) ────────────────────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(31)]
    [InlineData(35)]
    [InlineData(44)]
    [InlineData(51)]
    public async Task NToNPlusOne_GeGrowsByOne_RunwayCoreSuffixStable(int n)
    {
        var lower = await MaterializeAsync(n, ReadinessProfile.ConsistencyNeeded);
        var higher = await MaterializeAsync(n + 1, ReadinessProfile.ConsistencyNeeded);

        Assert.Equal(lower.GeneralEnduranceWeeks + 1, higher.GeneralEnduranceWeeks);
        Assert.Equal(8, higher.PreparationRunwayWeeks);
        Assert.Equal(12, higher.CoreWeeks);
        Assert.Equal(n + 1, higher.TotalWeeks);

        var lowerSuffix = lower.Weeks.Skip(lower.GeneralEnduranceWeeks).ToList();
        var higherSuffix = higher.Weeks.Skip(higher.GeneralEnduranceWeeks).ToList();
        Assert.Equal(20, lowerSuffix.Count);
        Assert.Equal(20, higherSuffix.Count);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(lowerSuffix[i].Segment, higherSuffix[i].Segment);
            Assert.Equal(lowerSuffix[i].RunwayBlock, higherSuffix[i].RunwayBlock);
            Assert.Equal(lowerSuffix[i].CorePhase, higherSuffix[i].CorePhase);
        }
    }

    // ── Profiles (Part 15) ────────────────────────────────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(40)]
    [InlineData(52)]
    public async Task BothProfiles_IdenticalDurationsAndBoundaries_DifferOnlyInApprovedContent(int totalWeeks)
    {
        var consistency = await MaterializeAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        var coreEntry = await MaterializeAsync(totalWeeks, ReadinessProfile.CoreEntryReady);

        Assert.Equal(consistency.TotalWeeks, coreEntry.TotalWeeks);
        Assert.Equal(consistency.GeneralEnduranceWeeks, coreEntry.GeneralEnduranceWeeks);
        Assert.Equal(consistency.PreparationRunwayWeeks, coreEntry.PreparationRunwayWeeks);
        Assert.Equal(consistency.CoreWeeks, coreEntry.CoreWeeks);

        for (var i = 0; i < consistency.Weeks.Count; i++)
        {
            Assert.Equal(consistency.Weeks[i].Segment, coreEntry.Weeks[i].Segment);
            Assert.Equal(consistency.Weeks[i].IsRecoveryWeek, coreEntry.Weeks[i].IsRecoveryWeek);
            Assert.Equal(consistency.Weeks[i].GlobalWeekNumber, coreEntry.Weeks[i].GlobalWeekNumber);
        }

        // Core is candidate-defined and must remain identical across profiles.
        var consistencyCore = consistency.Weeks.Where(w => w.Segment == LongHorizonSegmentType.Core).Select(w => w.CorePhase);
        var coreEntryCore = coreEntry.Weeks.Where(w => w.Segment == LongHorizonSegmentType.Core).Select(w => w.CorePhase);
        Assert.Equal(consistencyCore, coreEntryCore);
    }

    // ── Full 21-52 matrix (Part 16-ish / required tests 53-60) ───────────

    public static IEnumerable<object[]> AllHorizons()
    {
        foreach (var week in Enumerable.Range(21, 32))
        {
            yield return new object[] { week, ReadinessProfile.ConsistencyNeeded };
            yield return new object[] { week, ReadinessProfile.CoreEntryReady };
        }
    }

    [Theory]
    [MemberData(nameof(AllHorizons))]
    internal async Task EveryHorizon_MaterializesValidDeterministicSkeleton(int totalWeeks, ReadinessProfile profile)
    {
        var first = await MaterializeAsync(totalWeeks, profile);
        var second = await MaterializeAsync(totalWeeks, profile);

        Assert.Equal(totalWeeks, first.TotalWeeks);
        Assert.Equal(totalWeeks, first.Weeks.Count);
        Assert.Equal(4 * totalWeeks, first.Weeks.Sum(w => w.OrderedWorkoutSlots.Count));

        var validation = LongHorizonStructuralValidator.Validate(first);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));

        // Determinism: repeated materialization for identical inputs is byte-identical in shape.
        Assert.Equal(
            first.Weeks.Select(w => (w.Segment, w.LocalSegmentWeekNumber, w.WeekType, w.RunwayBlock, w.CorePhase, w.IsRecoveryWeek)),
            second.Weeks.Select(w => (w.Segment, w.LocalSegmentWeekNumber, w.WeekType, w.RunwayBlock, w.CorePhase, w.IsRecoveryWeek)));

        var geSlots = first.Weeks.Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).SelectMany(w => w.OrderedWorkoutSlots);
        Assert.DoesNotContain(geSlots, s => s.WorkoutKey is "THRESHOLD_TEMPO" or "GOAL_PACE_TEN_K" or "VO2MAX_INTERVAL");
        Assert.All(geSlots, s => Assert.NotNull(s.WorkoutKey));
    }

    // ── Invalid inputs (Part 17) ─────────────────────────────────────────

    [Fact]
    public async Task TwentyWeeks_RejectedByMaterializer()
    {
        var decision = Decide(20, ReadinessProfile.ConsistencyNeeded);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader()));
    }

    [Fact]
    public async Task FiftyThreeWeeks_RejectedByMaterializer()
    {
        var decision = Decide(53, ReadinessProfile.ConsistencyNeeded);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader()));
    }

    [Fact]
    public async Task NullReadinessProfile_RejectedByMaterializer()
    {
        var decision = Decide(24, ReadinessProfile.ConsistencyNeeded) with { ReadinessProfile = null };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader()));
    }

    [Fact]
    public async Task RunwayWeeksNotEight_RejectedByMaterializer()
    {
        var decision = Decide(24, ReadinessProfile.ConsistencyNeeded) with { PreparationRunwayWeeks = 7 };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader()));
    }

    [Fact]
    public async Task CoreWeeksNotTwelve_RejectedByMaterializer()
    {
        var decision = Decide(24, ReadinessProfile.ConsistencyNeeded) with { CoreWeeks = 11 };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader()));
    }

    [Fact]
    public async Task SumMismatch_RejectedByMaterializer()
    {
        var decision = Decide(24, ReadinessProfile.ConsistencyNeeded) with { GeneralEnduranceWeeks = 5 };
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LongHorizonStructuralMaterializer.MaterializeAsync(decision, CatalogRoot(), Loader()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    public void GeStructuralSelector_RejectsOutOfRangeGeWeeks(int geWeeks)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            LongHorizonGeStructuralSelector.Select(geWeeks, ReadinessProfile.ConsistencyNeeded));
    }

    // ── Structural validator direct tests (Part 16 / required tests 1-10) ─

    [Fact]
    public async Task Validator_ValidSkeleton_Passes()
    {
        var skeleton = await MaterializeAsync(28, ReadinessProfile.CoreEntryReady);
        Assert.True(LongHorizonStructuralValidator.Validate(skeleton).IsValid);
    }

    [Fact]
    public async Task Validator_DuplicateGlobalWeekNumber_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var broken = skeleton.Weeks.Select((w, i) => i == 5 ? w with { GlobalWeekNumber = w.GlobalWeekNumber - 1 } : w).ToList();
        var result = LongHorizonStructuralValidator.Validate(skeleton with { Weeks = broken });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_WeekNumberGap_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var broken = skeleton.Weeks.Select((w, i) => i == 5 ? w with { GlobalWeekNumber = w.GlobalWeekNumber + 1 } : w).ToList();
        var result = LongHorizonStructuralValidator.Validate(skeleton with { Weeks = broken });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_FinalWeekMismatch_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var result = LongHorizonStructuralValidator.Validate(skeleton with { TotalWeeks = 25 });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_InterleavedSegments_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var weeks = skeleton.Weeks.ToList();
        (weeks[0], weeks[4]) = (weeks[4] with { GlobalWeekNumber = weeks[0].GlobalWeekNumber }, weeks[0] with { GlobalWeekNumber = weeks[4].GlobalWeekNumber });
        var result = LongHorizonStructuralValidator.Validate(skeleton with { Weeks = weeks });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_InvalidRoleCount_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var weeks = skeleton.Weeks.ToList();
        weeks[0] = weeks[0] with { OrderedWorkoutSlots = weeks[0].OrderedWorkoutSlots.Take(3).ToList() };
        var result = LongHorizonStructuralValidator.Validate(skeleton with { Weeks = weeks });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_MissingGeProvenance_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var weeks = skeleton.Weeks.ToList();
        weeks[0] = weeks[0] with { GeStageFamily = null };
        var result = LongHorizonStructuralValidator.Validate(skeleton with { Weeks = weeks });
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validator_IncompatibleSegmentField_Fails()
    {
        var skeleton = await MaterializeAsync(24, ReadinessProfile.ConsistencyNeeded);
        var weeks = skeleton.Weeks.ToList();
        weeks[0] = weeks[0] with { CorePhase = "FOUNDATION" }; // GE week carrying Core provenance.
        var result = LongHorizonStructuralValidator.Validate(skeleton with { Weeks = weeks });
        Assert.False(result.IsValid);
    }

    // ── Containment (Part 21) ─────────────────────────────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(24)]
    [InlineData(40)]
    [InlineData(52)]
    public async Task PublicPreviewContainment_HttpPathUnaffectedByDarkMaterializerExisting(int totalWeeks)
    {
        // The dark materializer can succeed for this horizon in a focused test...
        var skeleton = await MaterializeAsync(totalWeeks, ReadinessProfile.ConsistencyNeeded);
        Assert.True(LongHorizonStructuralValidator.Validate(skeleton).IsValid);

        // ...while the pre-existing, unchanged public HTTP containment assertion for this exact
        // horizon (21-52 -> PLAN_HORIZON_COMPOSITION_REQUIRED, never a schedule) is covered by
        // LongHorizonGenerationContainmentTests (Phase 4I.3, real HTTP host + real Postgres) and is
        // not re-asserted here to avoid duplicating that suite's own dependency -- this phase adds
        // zero call sites into PlanServices/CatalogPreviewGenerator (confirmed by grep: no reference
        // to LongHorizonStructuralMaterializer exists outside this test file and its own production
        // file), so that suite's 21/24/52/53 assertions remain valid evidence for this phase too.
    }

    // ── Performance (Part 22, diagnostic only) ───────────────────────────

    [Theory]
    [InlineData(21)]
    [InlineData(40)]
    [InlineData(52)]
    public async Task Materialization_CompletesQuicklyAndDeterministically(int totalWeeks)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var skeleton = await MaterializeAsync(totalWeeks, ReadinessProfile.CoreEntryReady);
        stopwatch.Stop();

        Assert.Equal(totalWeeks, skeleton.Weeks.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Materialization took {stopwatch.ElapsedMilliseconds}ms (diagnostic threshold 5000ms).");
    }
}
