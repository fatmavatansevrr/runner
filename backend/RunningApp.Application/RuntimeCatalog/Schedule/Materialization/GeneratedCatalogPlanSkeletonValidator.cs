namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

/// <summary>
/// Backend Integration Phase 4F.2 — every distinct structural failure
/// <see cref="GeneratedCatalogPlanSkeletonValidator"/> can report. A stable
/// typed vocabulary, never a free-text message, mirroring Phase 4F.1's own
/// <see cref="GeneratedCatalogPlanPayloadValidationError"/> convention.
/// </summary>
public enum GeneratedCatalogPlanSkeletonValidationError
{
    UnsupportedSchemaVersion,
    PlannedWeekCountNotPositive,
    ActualWeekCountMismatch,
    WeekNumbersNotConsecutiveFromOne,
    WeekDateRangeIncorrect,
    PlanEndDateInconsistentWithFinalWeek,
    StageAllocationIncomplete,
    StageWeekIndexingInvalid,
    SessionSlotCountIncorrect,
    SessionSlotOrderInvalid,
    RestSlotPresent,
    ProvenanceMissing,
}

/// <summary>
/// Result of validating one <see cref="GeneratedCatalogPlanSkeleton"/>.
/// Mirrors <see cref="GeneratedCatalogPlanPayloadValidationResult"/>'s own
/// shape exactly.
/// </summary>
public sealed class GeneratedCatalogPlanSkeletonValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<GeneratedCatalogPlanSkeletonValidationError> Errors { get; init; }

    public static GeneratedCatalogPlanSkeletonValidationResult Valid() =>
        new() { IsValid = true, Errors = Array.Empty<GeneratedCatalogPlanSkeletonValidationError>() };

    public static GeneratedCatalogPlanSkeletonValidationResult Invalid(IReadOnlyList<GeneratedCatalogPlanSkeletonValidationError> errors) =>
        new() { IsValid = false, Errors = errors };
}

/// <summary>
/// Backend Integration Phase 4F.2 — independently validates a
/// <see cref="GeneratedCatalogPlanSkeleton"/> regardless of how it was
/// produced (defense-in-depth alongside <see cref="CatalogStageToWeekMaterializer"/>'s
/// own inline construction-time checks — this validator re-checks the
/// OUTPUT, not the materializer's inputs). Pure and dependency-free: no
/// database, clock, resolver, or catalog-loader dependency of any kind.
/// </summary>
public interface IGeneratedCatalogPlanSkeletonValidator
{
    GeneratedCatalogPlanSkeletonValidationResult Validate(GeneratedCatalogPlanSkeleton skeleton);
}

/// <inheritdoc cref="IGeneratedCatalogPlanSkeletonValidator"/>
public sealed class GeneratedCatalogPlanSkeletonValidator : IGeneratedCatalogPlanSkeletonValidator
{
    public GeneratedCatalogPlanSkeletonValidationResult Validate(GeneratedCatalogPlanSkeleton skeleton)
    {
        var errors = new List<GeneratedCatalogPlanSkeletonValidationError>();

        if (skeleton.SchemaVersion != GeneratedCatalogPlanSkeleton.CurrentSchemaVersion)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.UnsupportedSchemaVersion);
        }

        if (skeleton.PlannedWeekCount <= 0)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.PlannedWeekCountNotPositive);
        }

        if (skeleton.Weeks.Count != skeleton.PlannedWeekCount)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.ActualWeekCountMismatch);
        }

        var weekNumbers = skeleton.Weeks.Select(w => w.WeekNumber).OrderBy(n => n).ToList();
        var consecutive = weekNumbers.Count > 0 && weekNumbers.SequenceEqual(Enumerable.Range(1, weekNumbers.Count));
        if (!consecutive)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.WeekNumbersNotConsecutiveFromOne);
        }

        var weekDatesValid = true;
        var stageIndexingValid = true;

        // Group consecutive weeks by StageKey occurrence to validate StageWeekIndex/StageWeekCount
        // without assuming allocation details beyond what the skeleton itself declares.
        var stageOccurrenceCounters = new Dictionary<string, int>();

        foreach (var week in skeleton.Weeks.OrderBy(w => w.WeekNumber))
        {
            var expectedStart = skeleton.StartDate.AddDays((week.WeekNumber - 1) * 7);
            var expectedEnd = expectedStart.AddDays(6);
            if (week.StartDate != expectedStart || week.EndDate != expectedEnd)
            {
                weekDatesValid = false;
            }

            if (week.StageWeekIndex <= 0 || week.StageWeekCount <= 0 || week.StageWeekIndex > week.StageWeekCount)
            {
                stageIndexingValid = false;
            }

            if (string.IsNullOrWhiteSpace(week.StageKey) || string.IsNullOrWhiteSpace(week.Provenance.StageKey))
            {
                errors.Add(GeneratedCatalogPlanSkeletonValidationError.ProvenanceMissing);
            }

            if (week.SessionSlots.Count != skeleton.DaysPerWeek)
            {
                errors.Add(GeneratedCatalogPlanSkeletonValidationError.SessionSlotCountIncorrect);
            }

            var slotOrders = week.SessionSlots.Select(s => s.SlotOrderInWeek).OrderBy(o => o).ToList();
            var slotOrdersValid = slotOrders.Count > 0 && slotOrders.SequenceEqual(Enumerable.Range(1, slotOrders.Count));
            if (!slotOrdersValid)
            {
                errors.Add(GeneratedCatalogPlanSkeletonValidationError.SessionSlotOrderInvalid);
            }

            foreach (var slot in week.SessionSlots)
            {
                if (string.Equals(slot.StructuralRole, "REST", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(GeneratedCatalogPlanSkeletonValidationError.RestSlotPresent);
                }

                if (string.IsNullOrWhiteSpace(slot.Provenance.SourceStageKey))
                {
                    errors.Add(GeneratedCatalogPlanSkeletonValidationError.ProvenanceMissing);
                }
            }
        }

        if (!weekDatesValid)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.WeekDateRangeIncorrect);
        }

        if (!stageIndexingValid)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.StageWeekIndexingInvalid);
        }

        // Complete-stage-allocation check: for each contiguous run of weeks sharing a StageKey,
        // StageWeekIndex must increment 1..StageWeekCount exactly once each (no gaps, no duplicates).
        var stageGroups = skeleton.Weeks.OrderBy(w => w.WeekNumber).GroupBy(w => new { w.StageKey, w.StageWeekCount });
        foreach (var group in stageGroups)
        {
            var indices = group.Select(w => w.StageWeekIndex).OrderBy(i => i).ToList();
            if (!indices.SequenceEqual(Enumerable.Range(1, group.Key.StageWeekCount)))
            {
                errors.Add(GeneratedCatalogPlanSkeletonValidationError.StageAllocationIncomplete);
                break;
            }
        }

        var finalWeek = skeleton.Weeks.OrderByDescending(w => w.WeekNumber).FirstOrDefault();
        if (finalWeek == null || skeleton.EndDate != finalWeek.EndDate)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.PlanEndDateInconsistentWithFinalWeek);
        }

        if (string.IsNullOrWhiteSpace(skeleton.CandidateKey) ||
            skeleton.CandidateVersion <= 0 ||
            string.IsNullOrWhiteSpace(skeleton.Provenance.CandidateKey) ||
            skeleton.Provenance.CandidateVersion <= 0 ||
            string.IsNullOrWhiteSpace(skeleton.Provenance.MaterializerVersion) ||
            skeleton.DependencyVersions.Count == 0)
        {
            errors.Add(GeneratedCatalogPlanSkeletonValidationError.ProvenanceMissing);
        }

        return errors.Count == 0
            ? GeneratedCatalogPlanSkeletonValidationResult.Valid()
            : GeneratedCatalogPlanSkeletonValidationResult.Invalid(errors.Distinct().ToList());
    }
}
