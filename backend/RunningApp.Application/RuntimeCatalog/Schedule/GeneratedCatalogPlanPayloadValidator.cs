namespace RunningApp.Application.RuntimeCatalog.Schedule;

/// <summary>
/// Backend Integration Phase 4F.1 — every distinct structural/persistability
/// failure the validator can report, per Decision 3's rule list. A stable,
/// typed vocabulary (never a free-text message) so callers can react to
/// specific failure classes without string-matching.
/// </summary>
public enum GeneratedCatalogPlanPayloadValidationError
{
    UnsupportedSchemaVersion,
    StartDateNotBeforeEndDate,
    PlannedWeekCountNotPositive,
    ActualWeekCountMismatch,
    WeekNumbersNotConsecutiveFromOne,
    DuplicateWeekNumber,
    WeekDateRangeIncorrect,
    PlanEndDateInconsistentWithFinalWeek,
    WeekSessionCountIncorrect,
    DuplicateSessionDate,
    SessionDateOutsideOwningWeek,
    SessionDateOutsidePlanRange,
    SessionOrderInvalid,
    DistancePrescriptionInvalid,
    DurationPrescriptionInvalid,
    PacePrescriptionInvalid,
    SegmentOrderInvalid,
    SegmentPrescriptionInvalid,
    ProvenanceMissing,
}

/// <summary>
/// Result of validating one <see cref="GeneratedCatalogPlanPayload"/>. Never
/// partially valid — <see cref="IsValid"/> is true iff <see cref="Errors"/>
/// is empty. Per Phase 4F.1 Decision 3, "valid" here means exactly
/// "represents the complete, structurally sound schedule expected of the
/// selected candidate" — i.e. persistable, not merely well-formed JSON.
/// </summary>
public sealed class GeneratedCatalogPlanPayloadValidationResult
{
    public required bool IsValid { get; init; }
    public required IReadOnlyList<GeneratedCatalogPlanPayloadValidationError> Errors { get; init; }

    public static GeneratedCatalogPlanPayloadValidationResult Valid() =>
        new() { IsValid = true, Errors = Array.Empty<GeneratedCatalogPlanPayloadValidationError>() };

    public static GeneratedCatalogPlanPayloadValidationResult Invalid(IReadOnlyList<GeneratedCatalogPlanPayloadValidationError> errors) =>
        new() { IsValid = false, Errors = errors };
}

/// <summary>
/// Backend Integration Phase 4F.1 — validates a <see cref="GeneratedCatalogPlanPayload"/>
/// against the structural/persistability rules in Decision 3 (full-plan
/// completeness), Decision 6 (plan-relative week blocks), Decision 7
/// (single-authoritative-target prescriptions), Decision 8 (structured pace),
/// and Decision 9 (optional-but-internally-consistent segments).
///
/// Deliberately pure and dependency-free: no database, no wall clock, no
/// catalog loader, no resolver, no route decider. Validates ONLY the exact
/// immutable payload it is given — never fills a missing/invalid field from
/// the current catalog, the original request, the user profile, route data,
/// or a pilot constant. A payload that fails validation is simply invalid;
/// this type never repairs, migrates, or reinterprets one.
/// </summary>
public interface IGeneratedCatalogPlanPayloadValidator
{
    GeneratedCatalogPlanPayloadValidationResult Validate(GeneratedCatalogPlanPayload payload);
}

/// <inheritdoc cref="IGeneratedCatalogPlanPayloadValidator"/>
public sealed class GeneratedCatalogPlanPayloadValidator : IGeneratedCatalogPlanPayloadValidator
{
    public GeneratedCatalogPlanPayloadValidationResult Validate(GeneratedCatalogPlanPayload payload)
    {
        var errors = new List<GeneratedCatalogPlanPayloadValidationError>();

        // ── Schema version ───────────────────────────────────────────────────
        // Checked first and independently: an unsupported schema version means
        // every other field's meaning is unverifiable, but we still run the
        // remaining checks so a caller sees the full error set in one pass
        // (mirrors this codebase's ValidateSnapshotSchema convention of
        // collecting every missing field rather than failing on the first).
        if (payload.SchemaVersion != GeneratedCatalogPlanPayload.CurrentSchemaVersion)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.UnsupportedSchemaVersion);
        }

        // ── Plan-level date/week-count sanity ────────────────────────────────
        if (payload.StartDate >= payload.EndDate)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.StartDateNotBeforeEndDate);
        }

        if (payload.PlannedWeekCount <= 0)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.PlannedWeekCountNotPositive);
        }

        if (payload.Weeks.Count != payload.PlannedWeekCount)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.ActualWeekCountMismatch);
        }

        // ── Week numbering ───────────────────────────────────────────────────
        var weekNumbers = payload.Weeks.Select(w => w.WeekNumber).ToList();
        if (weekNumbers.Distinct().Count() != weekNumbers.Count)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.DuplicateWeekNumber);
        }

        var sortedWeekNumbers = weekNumbers.OrderBy(n => n).ToList();
        var isConsecutiveFromOne = sortedWeekNumbers.Count > 0 &&
            sortedWeekNumbers.SequenceEqual(Enumerable.Range(1, sortedWeekNumbers.Count));
        if (!isConsecutiveFromOne)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.WeekNumbersNotConsecutiveFromOne);
        }

        // ── Per-week structural rules ────────────────────────────────────────
        var weekDateRangeValid = true;
        var allSessionDates = new List<DateOnly>();

        foreach (var week in payload.Weeks.OrderBy(w => w.WeekNumber))
        {
            // Decision 6: Week N start = StartDate + (N-1)*7; end = start + 6.
            var expectedStart = payload.StartDate.AddDays((week.WeekNumber - 1) * 7);
            var expectedEnd = expectedStart.AddDays(6);
            if (week.StartDate != expectedStart || week.EndDate != expectedEnd)
            {
                weekDateRangeValid = false;
            }

            if (week.Sessions.Count != payload.DaysPerWeek)
            {
                errors.Add(GeneratedCatalogPlanPayloadValidationError.WeekSessionCountIncorrect);
            }

            if (string.IsNullOrWhiteSpace(week.StageKey) ||
                string.IsNullOrWhiteSpace(week.Provenance.StageKey))
            {
                errors.Add(GeneratedCatalogPlanPayloadValidationError.ProvenanceMissing);
            }

            var sessionOrders = week.Sessions.Select(s => s.SessionOrderInWeek).OrderBy(o => o).ToList();
            var sessionOrdersConsecutive = sessionOrders.Count > 0 &&
                sessionOrders.SequenceEqual(Enumerable.Range(1, sessionOrders.Count));
            if (week.Sessions.Count > 0 && !sessionOrdersConsecutive)
            {
                errors.Add(GeneratedCatalogPlanPayloadValidationError.SessionOrderInvalid);
            }

            foreach (var session in week.Sessions)
            {
                allSessionDates.Add(session.Date);

                if (session.Date < week.StartDate || session.Date > week.EndDate)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.SessionDateOutsideOwningWeek);
                }

                if (session.Date < payload.StartDate || session.Date > payload.EndDate)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.SessionDateOutsidePlanRange);
                }

                if (string.IsNullOrWhiteSpace(session.Provenance.SourceStageKey))
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.ProvenanceMissing);
                }

                ValidatePrescription(session.PrescriptionBasis, session.TargetDistanceKm, session.TargetDurationMinutes,
                    session.EstimatedDistanceKm, session.EstimatedDurationMinutes, errors);

                ValidatePace(session.PacePrescription, errors);

                ValidateSegments(session.Segments, errors);
            }
        }

        if (!weekDateRangeValid)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.WeekDateRangeIncorrect);
        }

        if (allSessionDates.Distinct().Count() != allSessionDates.Count)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.DuplicateSessionDate);
        }

        // ── Plan EndDate must equal the final plan-relative week's EndDate ──
        var finalWeek = payload.Weeks.OrderByDescending(w => w.WeekNumber).FirstOrDefault();
        if (finalWeek == null || payload.EndDate != finalWeek.EndDate)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.PlanEndDateInconsistentWithFinalWeek);
        }

        // ── Plan-level provenance ────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(payload.CandidateKey) ||
            string.IsNullOrWhiteSpace(payload.Provenance.CandidateKey) ||
            payload.CandidateVersion <= 0 ||
            payload.Provenance.CandidateVersion <= 0 ||
            string.IsNullOrWhiteSpace(payload.Provenance.GenerationSource) ||
            payload.DependencyVersions.Count == 0)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.ProvenanceMissing);
        }

        return errors.Count == 0
            ? GeneratedCatalogPlanPayloadValidationResult.Valid()
            : GeneratedCatalogPlanPayloadValidationResult.Invalid(errors.Distinct().ToList());
    }

    /// <summary>
    /// Decision 7: exactly one authoritative target. The non-authoritative
    /// metric is an optional estimate that must not itself look like a second
    /// authoritative target (i.e. it is never validated for positivity — an
    /// estimate of zero/absent is fine — but its mere presence is allowed,
    /// never required).
    /// </summary>
    private static void ValidatePrescription(
        GeneratedCatalogPrescriptionBasis basis,
        double? targetDistanceKm,
        int? targetDurationMinutes,
        double? estimatedDistanceKm,
        int? estimatedDurationMinutes,
        List<GeneratedCatalogPlanPayloadValidationError> errors)
    {
        switch (basis)
        {
            case GeneratedCatalogPrescriptionBasis.Distance:
                if (targetDistanceKm is not > 0 || targetDurationMinutes is not null)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.DistancePrescriptionInvalid);
                }
                if (estimatedDurationMinutes is <= 0)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.DistancePrescriptionInvalid);
                }
                break;

            case GeneratedCatalogPrescriptionBasis.Duration:
                if (targetDurationMinutes is not > 0 || targetDistanceKm is not null)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.DurationPrescriptionInvalid);
                }
                if (estimatedDistanceKm is <= 0)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.DurationPrescriptionInvalid);
                }
                break;

            default:
                errors.Add(GeneratedCatalogPlanPayloadValidationError.DistancePrescriptionInvalid);
                break;
        }
    }

    /// <summary>
    /// Decision 8. Min/max convention: <c>MinSecondsPerKm</c> is the FASTER
    /// (numerically smaller) end of the band, <c>MaxSecondsPerKm</c> the
    /// SLOWER (numerically larger) end — documented once here as the single
    /// source of truth for this ordering (seconds-per-km is an inverse-speed
    /// unit, so "min" meaning "fastest" is the one convention that keeps
    /// "Min &lt;= Max" true and non-confusing).
    /// </summary>
    private static void ValidatePace(GeneratedCatalogPacePrescription pace, List<GeneratedCatalogPlanPayloadValidationError> errors)
    {
        switch (pace.PaceType)
        {
            case GeneratedCatalogPaceType.None:
                if (pace.TargetSecondsPerKm is not null || pace.MinSecondsPerKm is not null || pace.MaxSecondsPerKm is not null)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid);
                }
                break;

            case GeneratedCatalogPaceType.Target:
                if (pace.TargetSecondsPerKm is not > 0 || pace.MinSecondsPerKm is not null || pace.MaxSecondsPerKm is not null)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid);
                }
                break;

            case GeneratedCatalogPaceType.Range:
                if (pace.MinSecondsPerKm is not > 0 || pace.MaxSecondsPerKm is not > 0 ||
                    pace.MinSecondsPerKm > pace.MaxSecondsPerKm)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid);
                }
                break;

            case GeneratedCatalogPaceType.EffortOnly:
                if (pace.TargetSecondsPerKm is not null || pace.MinSecondsPerKm is not null || pace.MaxSecondsPerKm is not null)
                {
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid);
                }
                break;

            default:
                errors.Add(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid);
                break;
        }
    }

    /// <summary>Decision 9. Segments are optional at session level; when present, each is independently validated.</summary>
    private static void ValidateSegments(IReadOnlyList<GeneratedCatalogWorkoutSegmentPayload> segments, List<GeneratedCatalogPlanPayloadValidationError> errors)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var orders = segments.Select(s => s.SegmentOrder).OrderBy(o => o).ToList();
        if (orders.Distinct().Count() != orders.Count)
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.SegmentOrderInvalid);
        }
        if (!orders.SequenceEqual(Enumerable.Range(1, orders.Count)))
        {
            errors.Add(GeneratedCatalogPlanPayloadValidationError.SegmentOrderInvalid);
        }

        foreach (var segment in segments)
        {
            if (segment.RepetitionCount is <= 0)
            {
                errors.Add(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid);
            }

            switch (segment.PrescriptionBasis)
            {
                case GeneratedCatalogPrescriptionBasis.Distance:
                    if (segment.TargetDistanceKm is not > 0 || segment.TargetDurationSeconds is not null)
                    {
                        errors.Add(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid);
                    }
                    break;
                case GeneratedCatalogPrescriptionBasis.Duration:
                    if (segment.TargetDurationSeconds is not > 0 || segment.TargetDistanceKm is not null)
                    {
                        errors.Add(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid);
                    }
                    break;
                default:
                    errors.Add(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid);
                    break;
            }

            if (segment.PacePrescription is not null)
            {
                ValidatePace(segment.PacePrescription, errors);
            }
        }
    }
}
