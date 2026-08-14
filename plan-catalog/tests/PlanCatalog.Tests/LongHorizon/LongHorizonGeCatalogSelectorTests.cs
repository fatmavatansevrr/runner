using PlanCatalog.Core.LongHorizon;
using Xunit;

namespace PlanCatalog.Tests.LongHorizon;

/// <summary>
/// Phase 4I.4 — production-level tests for <see cref="LongHorizonGeCatalogSelector"/>
/// and <see cref="LongHorizonGeCatalogValidator"/> against the real catalog
/// document (not a hand-built fixture), proving deterministic, exhaustion-
/// free catalog-only selection for every GE duration 1-32 and both
/// readiness profiles.
/// </summary>
public sealed class LongHorizonGeCatalogSelectorTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PlanCatalog.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("PlanCatalog.sln not found.");
    }

    private static readonly LongHorizonGeStageFamilyCatalogDocument Catalog = LongHorizonGeStageFamilyCatalogLoader.Load(
        Path.Combine(RepoRoot(), "catalog", "long-horizon-progressions", "ten-k-long-horizon-ge-stage-families.v1.json"));

    private static readonly LongHorizonReadinessProfile[] BothProfiles =
        [LongHorizonReadinessProfile.ConsistencyNeeded, LongHorizonReadinessProfile.CoreEntryReady];

    // ── ShortExtension exact structures ─────────────────────────────────────

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Ge1_IsExactlyOneEntryAlignmentWeek(LongHorizonReadinessProfile profile)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(1, profile, Catalog);
        Assert.Single(weeks);
        Assert.Equal(ShortExtensionRole.EntryAlignment, weeks[0].ShortExtensionRole);
        Assert.Equal(GeStageFamily.Entry, weeks[0].StageFamily);
        Assert.False(weeks[0].IsRecoveryWeek);
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Ge2_IsEntryThenPreRunwayAlignment(LongHorizonReadinessProfile profile)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(2, profile, Catalog);
        Assert.Equal(2, weeks.Count);
        Assert.Equal(ShortExtensionRole.EntryAlignment, weeks[0].ShortExtensionRole);
        Assert.Equal(ShortExtensionRole.PreRunwayAlignment, weeks[1].ShortExtensionRole);
        Assert.All(weeks, w => Assert.False(w.IsRecoveryWeek));
    }

    [Theory]
    [MemberData(nameof(Profiles))]
    public void Ge3_IsEntryThenControlledDevelopmentThenPreRunwayAlignment(LongHorizonReadinessProfile profile)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(3, profile, Catalog);
        Assert.Equal(3, weeks.Count);
        Assert.Equal(ShortExtensionRole.EntryAlignment, weeks[0].ShortExtensionRole);
        Assert.Equal(ShortExtensionRole.ControlledDevelopment, weeks[1].ShortExtensionRole);
        Assert.Equal(ShortExtensionRole.PreRunwayAlignment, weeks[2].ShortExtensionRole);
        Assert.All(weeks, w => Assert.False(w.IsRecoveryWeek));
    }

    [Fact]
    public void ShortExtension_NeverContainsProhibitedWorkout()
    {
        foreach (var geWeeks in new[] { 1, 2, 3 })
        foreach (var profile in BothProfiles)
        {
            var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
            foreach (var week in weeks)
                foreach (var workout in week.Roles.Values)
                    Assert.DoesNotContain(workout.Key, new[] { "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K" });
        }
    }

    public static IEnumerable<object[]> Profiles() => BothProfiles.Select(p => new object[] { p });

    // ── Mesocycle structure ──────────────────────────────────────────────────

    [Theory]
    [InlineData(4, 1)]
    [InlineData(8, 2)]
    [InlineData(12, 3)]
    [InlineData(20, 5)]
    [InlineData(32, 8)]
    public void FullPhase_HasExpectedCompleteMesocycleCount(int geWeeks, int expectedMesocycles)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        var mesocycleIndices = weeks.Where(w => w.MesocycleIndex is not null).Select(w => w.MesocycleIndex!.Value).Distinct().ToList();
        Assert.Equal(expectedMesocycles, mesocycleIndices.Count);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(20)]
    [InlineData(32)]
    public void EveryFourthMesocycleWeek_IsRecovery(int geWeeks)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, LongHorizonReadinessProfile.CoreEntryReady, Catalog);
        var mesocycleWeeks = weeks.Where(w => w.MesocycleIndex is not null).ToList();
        for (var i = 0; i < mesocycleWeeks.Count; i++)
            Assert.Equal((i + 1) % 4 == 0, mesocycleWeeks[i].IsRecoveryWeek);
    }

    [Fact]
    public void DevelopmentPositions_AreOrderedCorrectlyWithinEachMesocycle()
    {
        var weeks = LongHorizonGeCatalogSelector.Select(8, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        var mesocycle1 = weeks.Where(w => w.MesocycleIndex == 1).ToList();
        Assert.Equal(
            new[] { MesocyclePosition.Development1, MesocyclePosition.Development2, MesocyclePosition.Development3, MesocyclePosition.RecoveryConsolidation },
            mesocycle1.Select(w => w.MesocyclePosition));
    }

    // ── Remainders ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(5, 1)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    public void RemainderIsTerminal_AfterCompleteMesocycles(int geWeeks, int expectedRemainderCount)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        var terminal = weeks.Where(w => w.IsTerminalAlignment).ToList();
        Assert.Equal(expectedRemainderCount, terminal.Count);
        Assert.All(terminal, w => Assert.False(w.IsRecoveryWeek));

        // Terminal weeks are always the contiguous suffix.
        var firstTerminalIndex = weeks.ToList().FindIndex(w => w.IsTerminalAlignment);
        Assert.All(weeks.Skip(firstTerminalIndex), w => Assert.True(w.IsTerminalAlignment));
        // No remainder week is itself flagged as a mesocycle position.
        Assert.All(terminal, w => Assert.Null(w.MesocycleIndex));
    }

    [Fact]
    public void NoRemainder_AppearsBeforeACompleteMesocycle()
    {
        var weeks = LongHorizonGeCatalogSelector.Select(11, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        var lastMesocycleWeekIndex = weeks.Where(w => w.MesocycleIndex is not null).Max(w => w.WeekIndex);
        var firstTerminalWeekIndex = weeks.First(w => w.IsTerminalAlignment).WeekIndex;
        Assert.True(firstTerminalWeekIndex > lastMesocycleWeekIndex);
    }

    [Fact]
    public void NoDuplicateTerminalAlignment_ForExactMultiplesOfFour()
    {
        var weeks = LongHorizonGeCatalogSelector.Select(8, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        Assert.DoesNotContain(weeks, w => w.IsTerminalAlignment);
    }

    // ── Mesocycle sequencing / repetition caps ──────────────────────────────

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(28)]
    [InlineData(32)]
    public void MesocycleSequencing_NeverExceedsRepetitionCaps(int geWeeks)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        var mesocycleStageFamilies = weeks
            .Where(w => w.MesocycleIndex is not null)
            .GroupBy(w => w.MesocycleIndex!.Value)
            .OrderBy(g => g.Key)
            .Select(g => g.First().StageFamily) // all development weeks of one mesocycle share a stage family
            .ToList();

        // BaseDevelopment: max 1 consecutive (catalog cap).
        AssertNoRunExceeds(mesocycleStageFamilies, GeStageFamily.BaseDevelopment, maxConsecutive: 1);
        // AerobicDurability: max 2 consecutive (catalog cap).
        AssertNoRunExceeds(mesocycleStageFamilies, GeStageFamily.AerobicDurability, maxConsecutive: 2);

        // Eight identical mesocycles never occur (only relevant at 32 weeks,
        // but checked generically here).
        if (mesocycleStageFamilies.Count > 1)
            Assert.True(mesocycleStageFamilies.Distinct().Count() > 1, "All mesocycles used an identical stage family.");
    }

    private static void AssertNoRunExceeds(IReadOnlyList<GeStageFamily> sequence, GeStageFamily family, int maxConsecutive)
    {
        var run = 0;
        foreach (var item in sequence)
        {
            run = item == family ? run + 1 : 0;
            Assert.True(run <= maxConsecutive, $"Stage family {family} repeated {run} times consecutively (cap {maxConsecutive}).");
        }
    }

    [Fact]
    public void ThirtyTwoWeeks_DoesNotUseEightIdenticalMesocycles()
    {
        var weeks = LongHorizonGeCatalogSelector.Select(32, LongHorizonReadinessProfile.CoreEntryReady, Catalog);
        var stageFamilies = weeks.Where(w => w.MesocycleIndex is not null)
            .GroupBy(w => w.MesocycleIndex!.Value).OrderBy(g => g.Key)
            .Select(g => g.First().StageFamily).ToList();
        Assert.Equal(8, stageFamilies.Count);
        Assert.True(stageFamilies.Distinct().Count() > 1);
        Assert.Equal(GeStageFamily.AerobicDurability, stageFamilies[^1]); // final mesocycle aligns toward Runway
        Assert.Equal(GeStageFamily.BaseDevelopment, stageFamilies[0]);
    }

    [Fact]
    public void IntentionalEasyAndLongRunReuse_IsPermittedAcrossConsecutiveWeeks()
    {
        // EASY_STANDARD/LONG_RUN_STANDARD repeating every single week is
        // intentional (numeric progression alone provides variation for
        // these families) -- not a repetition-rule violation.
        var weeks = LongHorizonGeCatalogSelector.Select(32, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        Assert.All(weeks, w => Assert.Equal("LONG_RUN_STANDARD", w.Roles[GeWeekRole.LongRun].Key));
        Assert.All(weeks, w => Assert.Equal("EASY_STANDARD", w.Roles[GeWeekRole.EasySupportA].Key));
    }

    [Fact]
    public void InvalidConsecutiveKeySessionReuse_ForCoreEntryReady_NeverExceedsStageFamilyCap()
    {
        // CORE_ENTRY_READY's KEY_SESSION alternates AEROBIC_STRENGTH_CONTROLLED_INTRO
        // (BaseDevelopment) / _PROGRESSED (AerobicDurability) by mesocycle stage
        // family -- proving the catalog does not silently repeat the same
        // controlled-aerobic-support workout beyond its own stage family's cap.
        var weeks = LongHorizonGeCatalogSelector.Select(32, LongHorizonReadinessProfile.CoreEntryReady, Catalog);
        var developmentKeySessions = weeks
            .Where(w => w.MesocyclePosition is MesocyclePosition.Development1 or MesocyclePosition.Development2 or MesocyclePosition.Development3)
            .Select(w => w.Roles[GeWeekRole.KeySession].Key)
            .ToList();
        // Never more than 6 consecutive identical KEY_SESSION workouts (2
        // mesocycles' worth of AerobicDurability, 3 weeks each = 6) --
        // bounded by the stage-family repetition cap, not unbounded.
        var run = 0;
        string? previous = null;
        foreach (var key in developmentKeySessions)
        {
            run = key == previous ? run + 1 : 1;
            previous = key;
            Assert.True(run <= 6, $"KEY_SESSION workout '{key}' repeated {run} times consecutively.");
        }
    }

    // ── Profile behavior ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(11)]
    [InlineData(20)]
    [InlineData(32)]
    public void BothProfiles_ProduceIdenticalStructuralPositions(int geWeeks)
    {
        var consistencyNeeded = LongHorizonGeCatalogSelector.Select(geWeeks, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        var coreEntryReady = LongHorizonGeCatalogSelector.Select(geWeeks, LongHorizonReadinessProfile.CoreEntryReady, Catalog);

        Assert.Equal(consistencyNeeded.Count, coreEntryReady.Count);
        for (var i = 0; i < consistencyNeeded.Count; i++)
        {
            Assert.Equal(consistencyNeeded[i].MesocycleIndex, coreEntryReady[i].MesocycleIndex);
            Assert.Equal(consistencyNeeded[i].MesocyclePosition, coreEntryReady[i].MesocyclePosition);
            Assert.Equal(consistencyNeeded[i].ShortExtensionRole, coreEntryReady[i].ShortExtensionRole);
            Assert.Equal(consistencyNeeded[i].IsRecoveryWeek, coreEntryReady[i].IsRecoveryWeek);
            Assert.Equal(consistencyNeeded[i].IsTerminalAlignment, coreEntryReady[i].IsTerminalAlignment);
        }
    }

    [Fact]
    public void ProfileContentDiffers_ConsistencyNeededNeverUsesControlledAerobicSupport()
    {
        var weeks = LongHorizonGeCatalogSelector.Select(32, LongHorizonReadinessProfile.ConsistencyNeeded, Catalog);
        Assert.All(weeks, w => Assert.DoesNotContain("AEROBIC_STRENGTH", w.Roles[GeWeekRole.KeySession].Key));
    }

    [Fact]
    public void ProfileContentDiffers_CoreEntryReadyUsesControlledAerobicSupportInDevelopmentWeeks()
    {
        var weeks = LongHorizonGeCatalogSelector.Select(32, LongHorizonReadinessProfile.CoreEntryReady, Catalog);
        var developmentKeySessions = weeks
            .Where(w => w.MesocyclePosition is MesocyclePosition.Development1 or MesocyclePosition.Development2 or MesocyclePosition.Development3)
            .Select(w => w.Roles[GeWeekRole.KeySession].Key);
        Assert.Contains(developmentKeySessions, k => k.StartsWith("AEROBIC_STRENGTH", StringComparison.Ordinal));
    }

    [Fact]
    public void LowIntensityDominanceRetained_NoProhibitedWorkoutForEitherProfile()
    {
        foreach (var geWeeks in new[] { 1, 4, 11, 20, 32 })
        foreach (var profile in BothProfiles)
        {
            var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
            foreach (var week in weeks)
                foreach (var workout in week.Roles.Values)
                    Assert.DoesNotContain(workout.Key, new[] { "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K", "FARTLEK" });
        }
    }

    // ── Full 1-32 duration matrix ─────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllDurationsBothProfiles))]
    public void EveryDuration_ResolvesWithExactWeekCountAndFourCompleteRoles(int geWeeks, LongHorizonReadinessProfile profile)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
        Assert.Equal(geWeeks, weeks.Count);
        foreach (var week in weeks)
        {
            Assert.Equal(4, week.Roles.Count);
            Assert.True(week.Roles.ContainsKey(GeWeekRole.KeySession));
            Assert.True(week.Roles.ContainsKey(GeWeekRole.EasySupportA));
            Assert.True(week.Roles.ContainsKey(GeWeekRole.EasySupportB));
            Assert.True(week.Roles.ContainsKey(GeWeekRole.LongRun));
        }
    }

    [Theory]
    [MemberData(nameof(AllDurationsBothProfiles))]
    public void EveryDuration_ClassifiesCorrectly(int geWeeks, LongHorizonReadinessProfile profile)
    {
        var expected = geWeeks <= 3 ? GeDurationClassification.ShortExtension : GeDurationClassification.FullPhase;
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
        Assert.All(weeks, w => Assert.Equal(expected, w.Classification));
        Assert.Equal(expected, LongHorizonGeCatalogSelector.Classify(geWeeks));
    }

    [Theory]
    [MemberData(nameof(AllDurationsBothProfiles))]
    public void EveryDuration_PassesValidation(int geWeeks, LongHorizonReadinessProfile profile)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
        var result = LongHorizonGeCatalogValidator.Validate(geWeeks, weeks);
        Assert.True(result.IsValid, string.Join("; ", result.Findings));
    }

    [Theory]
    [MemberData(nameof(AllDurationsBothProfiles))]
    public void EveryDuration_IsDeterministic(int geWeeks, LongHorizonReadinessProfile profile)
    {
        var first = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
        var second = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].StageFamily, second[i].StageFamily);
            Assert.Equal(first[i].Roles[GeWeekRole.KeySession].Key, second[i].Roles[GeWeekRole.KeySession].Key);
        }
    }

    [Theory]
    [MemberData(nameof(AllDurationsBothProfiles))]
    public void EveryDuration_NoUnresolvedCatalogReference(int geWeeks, LongHorizonReadinessProfile profile)
    {
        var validKeys = new[] { "EASY_STANDARD", "LONG_RUN_STANDARD", "AEROBIC_STRENGTH_CONTROLLED_INTRO", "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED" };
        var weeks = LongHorizonGeCatalogSelector.Select(geWeeks, profile, Catalog);
        foreach (var week in weeks)
            foreach (var workout in week.Roles.Values)
                Assert.Contains(workout.Key, validKeys);
    }

    public static IEnumerable<object[]> AllDurationsBothProfiles()
    {
        for (var geWeeks = 1; geWeeks <= 32; geWeeks++)
            foreach (var profile in BothProfiles)
                yield return new object[] { geWeeks, profile };
    }

    // ── 32-week exhaustion proof (mandatory, standalone) ────────────────────

    [Theory]
    [MemberData(nameof(Profiles))]
    public void ThirtyTwoWeeks_ExhaustionProof(LongHorizonReadinessProfile profile)
    {
        var weeks = LongHorizonGeCatalogSelector.Select(32, profile, Catalog);

        Assert.Equal(32, weeks.Count);
        Assert.Equal(8, weeks.Count(w => w.IsRecoveryWeek));
        Assert.Equal(0, weeks.Count(w => w.IsTerminalAlignment)); // 32 = 8 exact mesocycles, no remainder
        Assert.All(weeks, w => Assert.Equal(4, w.Roles.Count));

        var validation = LongHorizonGeCatalogValidator.Validate(32, weeks);
        Assert.True(validation.IsValid, string.Join("; ", validation.Findings));

        foreach (var week in weeks)
            foreach (var workout in week.Roles.Values)
                Assert.DoesNotContain(workout.Key, new[] { "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K" });
    }

    // ── GE -> Runway catalog compatibility ──────────────────────────────────

    [Theory]
    [MemberData(nameof(Profiles))]
    public void FinalGeWeek_IsStructurallyCompatibleWithRunwayWeekOne(LongHorizonReadinessProfile profile)
    {
        // Runway Week 1 (Consistency Step 1, per the existing, unchanged
        // Preparation Runway catalog) uses EASY_STANDARD for its KEY_SESSION-
        // equivalent opening step -- the final GE week (always
        // PreRunwayAlignment, KEY_SESSION=EASY_STANDARD) shares the same
        // workout family and intensity boundary, proving no structural
        // conflict or prohibited-intensity jump at the transition. Numeric
        // continuity (volume/long-run) is explicitly deferred to a later
        // phase -- not checked here.
        var weeks = LongHorizonGeCatalogSelector.Select(32, profile, Catalog);
        var finalWeek = weeks[^1];
        // 32 is an exact multiple of 4, so the final GE week is mesocycle
        // 8's own Consolidation/recovery week -- still KEY_SESSION=EASY_STANDARD
        // (Consolidation never uses controlled aerobic support, Phase 4I.2 §14),
        // which is exactly the family/intensity Runway Week 1 itself opens with.
        Assert.True(finalWeek.IsRecoveryWeek);
        Assert.Equal("EASY_STANDARD", finalWeek.Roles[GeWeekRole.KeySession].Key);
        Assert.Equal("EASY", finalWeek.Roles[GeWeekRole.KeySession].Family);
        // No prohibited/threshold/goal-pace content at the transition boundary.
        foreach (var workout in finalWeek.Roles.Values)
            Assert.DoesNotContain(workout.Key, new[] { "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K" });
    }
}
