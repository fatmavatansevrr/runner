using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.2 — tests for
/// <see cref="GeneratedCatalogPlanSkeletonValidator"/>: independent,
/// defense-in-depth structural validation of a
/// <see cref="GeneratedCatalogPlanSkeleton"/>, regardless of whether it came
/// from <see cref="CatalogStageToWeekMaterializer"/> or a hand-built fixture.
/// </summary>
public sealed class GeneratedCatalogPlanSkeletonValidatorTests
{
    private readonly GeneratedCatalogPlanSkeletonValidator _validator = new();
    private readonly CatalogStageToWeekMaterializer _materializer = new();

    private GeneratedCatalogPlanSkeleton ValidPilotSkeleton() =>
        _materializer.Materialize(CatalogStageToWeekMaterializerFixtures.PilotContext()).Skeleton;

    [Fact]
    public void Validate_MaterializedPilotSkeleton_IsValid()
    {
        var result = _validator.Validate(ValidPilotSkeleton());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_UnsupportedSchemaVersion_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        var mutated = Clone(skeleton, schemaVersion: 999);

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.UnsupportedSchemaVersion, result.Errors);
    }

    [Fact]
    public void Validate_ActualWeekCountMismatch_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        var mutated = Clone(skeleton, weeks: skeleton.Weeks.Take(11).ToList());

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.ActualWeekCountMismatch, result.Errors);
    }

    [Fact]
    public void Validate_PlanEndDateInconsistentWithFinalWeek_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        var mutated = Clone(skeleton, endDate: skeleton.EndDate.AddDays(7));

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.PlanEndDateInconsistentWithFinalWeek, result.Errors);
    }

    [Fact]
    public void Validate_SessionSlotCountIncorrect_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        var week1 = skeleton.Weeks[0];
        var truncatedWeek = ReplaceSlots(week1, week1.SessionSlots.Take(3).ToList());
        var mutated = Clone(skeleton, weeks: new[] { truncatedWeek }.Concat(skeleton.Weeks.Skip(1)).ToList());

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.SessionSlotCountIncorrect, result.Errors);
    }

    [Fact]
    public void Validate_StageAllocationIncomplete_MissingIndex_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        // Corrupt BUILD's indices: skip index 2 (leaves 1,3,4 present for a StageWeekCount=4 group).
        var buildWeeks = skeleton.Weeks.Where(w => w.StageKey == "BUILD").OrderBy(w => w.WeekNumber).ToList();
        var corrupted = ReIndex(buildWeeks[1], stageWeekIndex: 3); // was 2, now duplicates week index 3

        var newWeeks = skeleton.Weeks.Select(w => w.WeekNumber == corrupted.WeekNumber ? corrupted : w).ToList();
        var mutated = Clone(skeleton, weeks: newWeeks);

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.StageAllocationIncomplete, result.Errors);
    }

    [Fact]
    public void Validate_RestSlotPresent_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        var week1 = skeleton.Weeks[0];
        var slotsWithRest = week1.SessionSlots.Take(3)
            .Append(ReplaceRole(week1.SessionSlots[3], "REST"))
            .ToList();
        var mutatedWeek = ReplaceSlots(week1, slotsWithRest);
        var mutated = Clone(skeleton, weeks: new[] { mutatedWeek }.Concat(skeleton.Weeks.Skip(1)).ToList());

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.RestSlotPresent, result.Errors);
    }

    [Fact]
    public void Validate_WeekNumbersNotConsecutive_IsInvalid()
    {
        var skeleton = ValidPilotSkeleton();
        var renumbered = skeleton.Weeks.Select((w, i) => i == 0 ? ReNumber(w, 99) : w).ToList();
        var mutated = Clone(skeleton, weeks: renumbered);

        var result = _validator.Validate(mutated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanSkeletonValidationError.WeekNumbersNotConsecutiveFromOne, result.Errors);
    }

    // ── helpers (test-only, no production code) ──────────────────────────────

    private static GeneratedCatalogPlanSkeleton Clone(
        GeneratedCatalogPlanSkeleton s,
        int? schemaVersion = null,
        DateOnly? endDate = null,
        IReadOnlyList<GeneratedCatalogWeekSkeleton>? weeks = null) => new()
    {
        SchemaVersion = schemaVersion ?? s.SchemaVersion,
        StartDate = s.StartDate,
        EndDate = endDate ?? s.EndDate,
        PlannedWeekCount = s.PlannedWeekCount,
        DaysPerWeek = s.DaysPerWeek,
        CanonicalDistanceFamily = s.CanonicalDistanceFamily,
        CandidateKey = s.CandidateKey,
        CandidateVersion = s.CandidateVersion,
        DependencyVersions = s.DependencyVersions,
        Weeks = weeks ?? s.Weeks,
        Provenance = s.Provenance,
    };

    private static GeneratedCatalogWeekSkeleton ReplaceSlots(GeneratedCatalogWeekSkeleton w, IReadOnlyList<GeneratedCatalogSessionSlotSkeleton> slots) => new()
    {
        WeekNumber = w.WeekNumber,
        StartDate = w.StartDate,
        EndDate = w.EndDate,
        StageKey = w.StageKey,
        StageWeekIndex = w.StageWeekIndex,
        StageWeekCount = w.StageWeekCount,
        SessionSlots = slots,
        Provenance = w.Provenance,
    };

    private static GeneratedCatalogWeekSkeleton ReIndex(GeneratedCatalogWeekSkeleton w, int stageWeekIndex) => new()
    {
        WeekNumber = w.WeekNumber,
        StartDate = w.StartDate,
        EndDate = w.EndDate,
        StageKey = w.StageKey,
        StageWeekIndex = stageWeekIndex,
        StageWeekCount = w.StageWeekCount,
        SessionSlots = w.SessionSlots,
        Provenance = w.Provenance,
    };

    private static GeneratedCatalogWeekSkeleton ReNumber(GeneratedCatalogWeekSkeleton w, int weekNumber) => new()
    {
        WeekNumber = weekNumber,
        StartDate = w.StartDate,
        EndDate = w.EndDate,
        StageKey = w.StageKey,
        StageWeekIndex = w.StageWeekIndex,
        StageWeekCount = w.StageWeekCount,
        SessionSlots = w.SessionSlots,
        Provenance = w.Provenance,
    };

    private static GeneratedCatalogSessionSlotSkeleton ReplaceRole(GeneratedCatalogSessionSlotSkeleton s, string role) => new()
    {
        SlotOrderInWeek = s.SlotOrderInWeek,
        LayoutSlotKey = s.LayoutSlotKey,
        StructuralRole = role,
        Provenance = s.Provenance,
    };
}
