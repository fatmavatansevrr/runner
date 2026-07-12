using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.2 — tests for
/// <see cref="CatalogStageToWeekMaterializer"/>. Every fixture is hand-built,
/// repository-evidence-backed test data (see
/// <see cref="CatalogStageToWeekMaterializerFixtures"/>'s own doc comment) —
/// never a claim that live catalog stage selection or materialization is
/// wired into any request path.
/// </summary>
public sealed class CatalogStageToWeekMaterializerTests
{
    private readonly CatalogStageToWeekMaterializer _materializer = new();

    // ── Week construction ────────────────────────────────────────────────────

    [Fact]
    public void Materialize_Week1_StartsExactlyOnStartDate()
    {
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext();
        var result = _materializer.Materialize(context);

        Assert.Equal(context.StartDate, result.Skeleton.Weeks[0].StartDate);
    }

    [Fact]
    public void Materialize_EveryWeek_SpansExactlySevenDays()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.All(result.Skeleton.Weeks, w => Assert.Equal(7, w.EndDate.DayNumber - w.StartDate.DayNumber + 1));
    }

    [Fact]
    public void Materialize_WeekNumbers_AreConsecutiveFromOne()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.Equal(Enumerable.Range(1, 12), result.Skeleton.Weeks.Select(w => w.WeekNumber));
    }

    [Fact]
    public void Materialize_WeekRanges_HaveNoGapsAndNoOverlaps()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var weeks = result.Skeleton.Weeks.OrderBy(w => w.WeekNumber).ToList();

        for (var i = 1; i < weeks.Count; i++)
        {
            // No gap and no overlap: each week's start is exactly one day after the previous week's end.
            Assert.Equal(weeks[i - 1].EndDate.AddDays(1), weeks[i].StartDate);
        }
    }

    [Fact]
    public void Materialize_PlanEndDate_MatchesLastWeekEndDate()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.Equal(result.Skeleton.Weeks[^1].EndDate, result.Skeleton.EndDate);
        // Explicit formula check, independent of the last-week lookup:
        Assert.Equal(result.Skeleton.StartDate.AddDays(12 * 7 - 1), result.Skeleton.EndDate);
    }

    [Fact]
    public void Materialize_NonMondayStartDate_RemainsUnchanged_NoCalendarNormalization()
    {
        var wednesday = new DateOnly(2026, 8, 5);
        Assert.Equal(DayOfWeek.Wednesday, wednesday.DayOfWeek);

        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext(startDate: wednesday));

        Assert.Equal(wednesday, result.Skeleton.StartDate);
        Assert.Equal(wednesday, result.Skeleton.Weeks[0].StartDate);
    }

    [Fact]
    public void Materialize_NoPartialFirstOrLastWeek_EveryWeekIsExactlySevenDays()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.Equal(7, result.Skeleton.Weeks[0].EndDate.DayNumber - result.Skeleton.Weeks[0].StartDate.DayNumber + 1);
        Assert.Equal(7, result.Skeleton.Weeks[^1].EndDate.DayNumber - result.Skeleton.Weeks[^1].StartDate.DayNumber + 1);
    }

    // ── Stage allocation ──────────────────────────────────────────────────────

    [Fact]
    public void Materialize_PilotAllocation_ProducesRepositoryConfirmedWeekSequence()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var stageKeysInOrder = result.Skeleton.Weeks.OrderBy(w => w.WeekNumber).Select(w => w.StageKey).ToList();

        var expected = Enumerable.Repeat("FOUNDATION", 3)
            .Concat(Enumerable.Repeat("BUILD", 4))
            .Concat(Enumerable.Repeat("RACE_SPECIFIC", 4))
            .Concat(Enumerable.Repeat("TAPER", 1))
            .ToList();

        Assert.Equal(expected, stageKeysInOrder);
    }

    [Fact]
    public void Materialize_EveryWeek_HasExactlyOneStage()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.All(result.Skeleton.Weeks, w => Assert.False(string.IsNullOrWhiteSpace(w.StageKey)));
    }

    [Fact]
    public void Materialize_StageWeekIndex_BeginsAtOneAndIncrementsWithinStage()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        var buildWeeks = result.Skeleton.Weeks.Where(w => w.StageKey == "BUILD").OrderBy(w => w.WeekNumber).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4 }, buildWeeks.Select(w => w.StageWeekIndex));
    }

    [Fact]
    public void Materialize_StageWeekCount_IsCorrectForEveryWeek()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        var expectedCounts = new Dictionary<string, int> { ["FOUNDATION"] = 3, ["BUILD"] = 4, ["RACE_SPECIFIC"] = 4, ["TAPER"] = 1 };
        Assert.All(result.Skeleton.Weeks, w => Assert.Equal(expectedCounts[w.StageKey], w.StageWeekCount));
    }

    [Fact]
    public void Materialize_StageOrder_IsPreserved()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var distinctStagesInFirstAppearanceOrder = result.Skeleton.Weeks
            .OrderBy(w => w.WeekNumber)
            .Select(w => w.StageKey)
            .Distinct()
            .ToList();

        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, distinctStagesInFirstAppearanceOrder);
    }

    [Fact]
    public void Materialize_MissingStageWeeks_ThrowsCatalogStageWeekCountMismatchException()
    {
        var shortAllocation = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("BUILD", 4),
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("TAPER", 0), // missing week, and also zero-length -- see next test for that distinct case
        };
        // Use a plausible non-zero-but-still-short variant: reduce BUILD to 3 to keep all allocations positive.
        var trulyShort = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("BUILD", 3),
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("TAPER", 1),
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(stageWeekAllocations: trulyShort);

        Assert.Throws<CatalogStageWeekCountMismatchException>(() => _materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_ExcessStageWeeks_ThrowsCatalogStageWeekCountMismatchException()
    {
        var excess = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("BUILD", 5), // one too many
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("TAPER", 1),
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(stageWeekAllocations: excess);

        Assert.Throws<CatalogStageWeekCountMismatchException>(() => _materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_ZeroLengthStageAllocation_ThrowsCatalogStageAllocationInvalidException()
    {
        var zeroLength = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("BUILD", 4),
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("TAPER", 0),
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(stageWeekAllocations: zeroLength);

        Assert.Throws<CatalogStageAllocationInvalidException>(() => _materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_UnknownStageKeyInAllocation_ThrowsCatalogStageAllocationInvalidException()
    {
        var unknownKey = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("BUILD", 4),
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("COOLDOWN_UNKNOWN", 1), // not in SelectedStageSequence
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(stageWeekAllocations: unknownKey);

        Assert.Throws<CatalogStageAllocationInvalidException>(() => _materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_OutOfOrderStageAllocation_ThrowsCatalogStageAllocationInvalidException()
    {
        var outOfOrder = new[]
        {
            new CatalogStageWeekAllocation("BUILD", 4),
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("TAPER", 1),
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(stageWeekAllocations: outOfOrder);

        Assert.Throws<CatalogStageAllocationInvalidException>(() => _materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_NoSilentWeekRedistribution_MismatchAlwaysThrows_NeverAdjustsCounts()
    {
        var short3 = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 2), // repository says 3 -- deliberately wrong
            new CatalogStageWeekAllocation("BUILD", 4),
            new CatalogStageWeekAllocation("RACE_SPECIFIC", 4),
            new CatalogStageWeekAllocation("TAPER", 1),
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(stageWeekAllocations: short3);

        // Must throw, not silently redistribute the missing week onto another stage.
        Assert.Throws<CatalogStageWeekCountMismatchException>(() => _materializer.Materialize(context));
    }

    [Fact]
    public void Materialize_ResolvedFallbackStage_AcceptedOnlyWhenAlreadyInAuthoritativeSelectedSequence()
    {
        // Simulates a caller that already resolved the RACE_SPECIFIC-internal
        // GOAL_PACE_REHEARSAL -> CURRENT_FITNESS_SPECIFIC_REHEARSAL fallback
        // (a workout-selection-level concept, out of Phase 4F.2's own week-
        // allocation scope) at the week-allocation granularity by simply
        // presenting a sequence that already includes the intended stage.
        // The materializer must accept this — it never second-guesses an
        // already-authoritative SelectedStageSequence.
        var sequenceWithResolvedStage = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(selectedStageSequence: sequenceWithResolvedStage);

        var result = _materializer.Materialize(context);

        Assert.Equal(12, result.Skeleton.Weeks.Count);
    }

    [Fact]
    public void Materialize_NeverSelectsAFallbackItself_UnresolvedAllocationForAnAlternateStageIsRejected()
    {
        // If the allocation names a stage that was never part of the authoritative
        // SelectedStageSequence (i.e. the materializer would have to "choose" it
        // itself), it must reject rather than accept.
        var sequenceWithoutFallback = new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" };
        var allocationNamingAnUnselectedFallback = new[]
        {
            new CatalogStageWeekAllocation("FOUNDATION", 3),
            new CatalogStageWeekAllocation("BUILD", 4),
            new CatalogStageWeekAllocation("CURRENT_FITNESS_SPECIFIC_REHEARSAL", 4), // never in SelectedStageSequence
            new CatalogStageWeekAllocation("TAPER", 1),
        };
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(
            selectedStageSequence: sequenceWithoutFallback,
            stageWeekAllocations: allocationNamingAnUnselectedFallback);

        Assert.Throws<CatalogStageAllocationInvalidException>(() => _materializer.Materialize(context));
    }

    // ── Session slots ─────────────────────────────────────────────────────────

    [Fact]
    public void Materialize_EveryPilotWeek_ContainsExactlyFourStructuralSlots()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.All(result.Skeleton.Weeks, w => Assert.Equal(4, w.SessionSlots.Count));
    }

    [Fact]
    public void Materialize_SlotOrder_IsStableAndConsecutive()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.All(result.Skeleton.Weeks, w =>
            Assert.Equal(new[] { 1, 2, 3, 4 }, w.SessionSlots.Select(s => s.SlotOrderInWeek)));
    }

    [Fact]
    public void Materialize_LayoutSlotKeys_MatchRepositoryRunLayoutDefinition()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var week = result.Skeleton.Weeks[0];

        Assert.Equal("KEY_SESSION_1", week.SessionSlots[0].LayoutSlotKey);
        Assert.Equal("EASY_SUPPORT_1", week.SessionSlots[1].LayoutSlotKey);
        Assert.Equal("EASY_SUPPORT_2", week.SessionSlots[2].LayoutSlotKey);
        Assert.Equal("LONG_RUN_1", week.SessionSlots[3].LayoutSlotKey);
    }

    [Fact]
    public void Materialize_StructuralRoleCounts_MatchAcceptedPilotLayout()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var week = result.Skeleton.Weeks[0];
        var roleCounts = week.SessionSlots.GroupBy(s => s.StructuralRole).ToDictionary(g => g.Key, g => g.Count());

        Assert.Equal(1, roleCounts["KEY_SESSION"]);
        Assert.Equal(2, roleCounts["EASY_SUPPORT"]);
        Assert.Equal(1, roleCounts["LONG_RUN"]);
    }

    [Fact]
    public void Materialize_NoRestSlot_IsGenerated()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var allRoles = result.Skeleton.Weeks.SelectMany(w => w.SessionSlots).Select(s => s.StructuralRole);

        Assert.DoesNotContain(allRoles, r => r.Equals("REST", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Materialize_NoOptionalOrRecoveryJogSlot_IsGenerated()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());
        var allRoles = result.Skeleton.Weeks.SelectMany(w => w.SessionSlots).Select(s => s.StructuralRole).ToList();

        Assert.DoesNotContain(allRoles, r => r.Contains("OPTIONAL", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(allRoles, r => r.Contains("RECOVERY", StringComparison.OrdinalIgnoreCase));
        // Only the exact four accepted pilot roles ever appear.
        Assert.All(allRoles, r => Assert.Contains(r, new[] { "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN" }));
    }

    [Fact]
    public void GeneratedCatalogSessionSlotSkeleton_HasNoWeekdayOrDateField()
    {
        var properties = typeof(GeneratedCatalogSessionSlotSkeleton).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(properties, n => n.Contains("Date", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("Weekday", StringComparison.OrdinalIgnoreCase) || n.Contains("DayOfWeek", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeneratedCatalogSessionSlotSkeleton_HasNoPrescriptionFields()
    {
        // Structural proof (Decisions 7/8): no distance, duration, pace, intensity, or segment field
        // exists anywhere on the slot skeleton type.
        var properties = typeof(GeneratedCatalogSessionSlotSkeleton).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(properties, n => n.Contains("Distance", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("Duration", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("Pace", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("Intensity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("Segment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeneratedCatalogWeekSkeleton_HasNoPlannedVolumeField()
    {
        // Decision 7: no fake weekly volume, no long-run distance -- the field must not exist at all.
        var properties = typeof(GeneratedCatalogWeekSkeleton).GetProperties().Select(p => p.Name).ToList();

        Assert.DoesNotContain(properties, n => n.Contains("Volume", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, n => n.Contains("Distance", StringComparison.OrdinalIgnoreCase));
    }

    // ── Determinism and isolation ────────────────────────────────────────────

    [Fact]
    public void Materialize_SameInput_ProducesStructurallyEquivalentOutput()
    {
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext();

        var result1 = _materializer.Materialize(context);
        var result2 = _materializer.Materialize(context);

        Assert.Equal(result1.Skeleton.Weeks.Count, result2.Skeleton.Weeks.Count);
        for (var i = 0; i < result1.Skeleton.Weeks.Count; i++)
        {
            Assert.Equal(result1.Skeleton.Weeks[i].WeekNumber, result2.Skeleton.Weeks[i].WeekNumber);
            Assert.Equal(result1.Skeleton.Weeks[i].StartDate, result2.Skeleton.Weeks[i].StartDate);
            Assert.Equal(result1.Skeleton.Weeks[i].EndDate, result2.Skeleton.Weeks[i].EndDate);
            Assert.Equal(result1.Skeleton.Weeks[i].StageKey, result2.Skeleton.Weeks[i].StageKey);
            Assert.Equal(result1.Skeleton.Weeks[i].SessionSlots.Count, result2.Skeleton.Weeks[i].SessionSlots.Count);
        }
        Assert.Equal(result1.Skeleton.EndDate, result2.Skeleton.EndDate);
    }

    [Fact]
    public void CatalogStageToWeekMaterializer_HasNoConstructorDependencies()
    {
        // Structural proof: no database, clock, HTTP/request, route-decider,
        // resolver, or catalog-loader dependency is even injectable.
        var ctors = typeof(CatalogStageToWeekMaterializer).GetConstructors();

        Assert.Single(ctors);
        Assert.Empty(ctors[0].GetParameters());
    }

    [Fact]
    public void Materialize_DoesNotMutate_TheSuppliedContextCollections()
    {
        var allocations = CatalogStageToWeekMaterializerFixtures.PilotStageAllocations.ToArray();
        var sequence = CatalogStageToWeekMaterializerFixtures.PilotStageSequence.ToArray();
        var context = CatalogStageToWeekMaterializerFixtures.PilotContext(
            selectedStageSequence: sequence, stageWeekAllocations: allocations);

        _materializer.Materialize(context);

        Assert.Equal(new[] { "FOUNDATION", "BUILD", "RACE_SPECIFIC", "TAPER" }, sequence);
        Assert.Equal(4, allocations.Length);
        Assert.Equal("FOUNDATION", allocations[0].StageKey);
        Assert.Equal(3, allocations[0].WeekCount);
    }

    // ── Provenance ────────────────────────────────────────────────────────────

    [Fact]
    public void Materialize_PlanProvenance_IsPresent()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.Equal("TEN_K__4D__INTERMEDIATE", result.Skeleton.Provenance.CandidateKey);
        Assert.Equal(10, result.Skeleton.Provenance.CandidateVersion);
        Assert.NotEmpty(result.Skeleton.Provenance.DependencyVersions);
    }

    [Fact]
    public void Materialize_WeekProvenance_IsPresent()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.All(result.Skeleton.Weeks, w =>
        {
            Assert.False(string.IsNullOrWhiteSpace(w.Provenance.StageKey));
            Assert.False(string.IsNullOrWhiteSpace(w.Provenance.SourcePhaseKey));
        });
    }

    [Fact]
    public void Materialize_SlotProvenance_IsPresent()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.All(result.Skeleton.Weeks.SelectMany(w => w.SessionSlots), s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Provenance.SourceStageKey));
            Assert.Equal("RUN_LAYOUT_4D", s.Provenance.SourceLayout.Key);
            Assert.Equal(2, s.Provenance.SourceLayout.Version);
        });
    }

    [Fact]
    public void Materialize_MaterializerVersion_IsRecorded()
    {
        var result = _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext());

        Assert.Equal("CATALOG_STAGE_TO_WEEK_MATERIALIZER_V1", result.Skeleton.Provenance.MaterializerVersion);
    }

    [Fact]
    public void GeneratedCatalogPlanSkeleton_ProvenanceTypes_AreAbsentFromPublicDtos()
    {
        var dtoAssembly = typeof(RunningApp.Application.DTOs.Plan.GeneratePreviewResponse).Assembly;
        var dtoTypes = dtoAssembly.GetTypes().Where(t => t.Namespace != null && t.Namespace.StartsWith("RunningApp.Application.DTOs"));

        foreach (var dtoType in dtoTypes)
        {
            foreach (var property in dtoType.GetProperties())
            {
                Assert.False(property.PropertyType.Namespace == "RunningApp.Application.RuntimeCatalog.Schedule.Materialization",
                    $"{dtoType.Name}.{property.Name} must not expose a Materialization-namespace type publicly.");
            }
        }
    }
}
