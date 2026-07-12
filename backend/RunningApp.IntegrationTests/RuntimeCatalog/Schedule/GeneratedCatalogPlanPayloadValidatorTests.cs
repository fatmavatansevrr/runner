using System;
using System.Collections.Generic;
using System.Linq;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Domain.Enums;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.Schedule;

/// <summary>
/// Backend Integration Phase 4F.1 — structural/persistability validation
/// tests for <see cref="GeneratedCatalogPlanPayloadValidator"/>, per Decisions
/// 3 (full-plan completeness), 6 (plan-relative weeks), 7 (single-authoritative
/// prescription), 8 (structured pace), 9 (optional segments), and 11
/// (provenance). Every fixture is hand-built test-only data — see
/// <see cref="GeneratedCatalogPlanPayloadFixtures"/>'s own doc comment: this
/// never implies live catalog materialization exists.
/// </summary>
public sealed class GeneratedCatalogPlanPayloadValidatorTests
{
    private readonly GeneratedCatalogPlanPayloadValidator _validator = new();

    // ── Baseline: the fixture itself must be valid ──────────────────────────

    [Fact]
    public void Validate_CompleteTwoWeekPilotFixture_IsValid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();

        var result = _validator.Validate(payload);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    // ── Schema version (test #5) ─────────────────────────────────────────────

    [Fact]
    public void Validate_UnsupportedSchemaVersion_IsInvalid()
    {
        var payload = WithSchemaVersion(GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan(), 999);

        var result = _validator.Validate(payload);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.UnsupportedSchemaVersion, result.Errors);
    }

    // ── Full-plan validation (tests #6-#19) ──────────────────────────────────

    [Fact]
    public void Validate_PartialWeekList_ActualCountBelowPlannedCount_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var truncated = Clone(payload, weeks: payload.Weeks.Take(1).ToList(), plannedWeekCount: payload.PlannedWeekCount);

        var result = _validator.Validate(truncated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.ActualWeekCountMismatch, result.Errors);
    }

    [Fact]
    public void Validate_DuplicateWeekNumbers_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var duplicated = Clone(payload, weeks: new[] { payload.Weeks[0], payload.Weeks[0] }, plannedWeekCount: 2);

        var result = _validator.Validate(duplicated);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.DuplicateWeekNumber, result.Errors);
    }

    [Fact]
    public void Validate_NonConsecutiveWeekNumbers_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidPlan(new DateOnly(2026, 8, 3), weekCount: 3);
        var skippedNumbering = new[] { payload.Weeks[0], ReNumber(payload.Weeks[2], 3) }; // 1, 3 — missing 2

        var result = _validator.Validate(Clone(payload, weeks: skippedNumbering, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.WeekNumbersNotConsecutiveFromOne, result.Errors);
    }

    [Fact]
    public void Validate_WeekDateRange_NotAStartDateBasedSevenDayBlock_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var badWeek = ReDate(payload.Weeks[0], payload.Weeks[0].StartDate.AddDays(1), payload.Weeks[0].EndDate.AddDays(1));

        var result = _validator.Validate(Clone(payload, weeks: new[] { badWeek, payload.Weeks[1] }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.WeekDateRangeIncorrect, result.Errors);
    }

    [Fact]
    public void Validate_PartialCalendarWeek_SixDaySpan_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var week1 = payload.Weeks[0];
        var shortWeek = ReDate(week1, week1.StartDate, week1.EndDate.AddDays(-1)); // 6-day span, not 7

        var result = _validator.Validate(Clone(payload, weeks: new[] { shortWeek, payload.Weeks[1] }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.WeekDateRangeIncorrect, result.Errors);
    }

    [Fact]
    public void Validate_PlanEndDate_NotMatchingFinalWeekEndDate_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var wrongEndDate = Clone(payload, endDate: payload.EndDate.AddDays(7));

        var result = _validator.Validate(wrongEndDate);

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.PlanEndDateInconsistentWithFinalWeek, result.Errors);
    }

    [Fact]
    public void Validate_DuplicateSessionDates_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var week1 = payload.Weeks[0];
        var duplicatedSession = week1.Sessions[0];
        var mutatedSessions = week1.Sessions.Take(3).Append(ReOrder(duplicatedSession, 4)).ToList(); // date collides with session[0]

        var badWeek = new GeneratedCatalogWeekPayload
        {
            WeekNumber = week1.WeekNumber,
            StartDate = week1.StartDate,
            EndDate = week1.EndDate,
            StageKey = week1.StageKey,
            PlannedVolumeKm = week1.PlannedVolumeKm,
            Sessions = mutatedSessions,
            Provenance = week1.Provenance,
        };

        var result = _validator.Validate(Clone(payload, weeks: new[] { badWeek, payload.Weeks[1] }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.DuplicateSessionDate, result.Errors);
    }

    [Fact]
    public void Validate_SessionDateOutsideOwningWeek_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var week1 = payload.Weeks[0];
        var outOfWeekSession = ReDate(week1.Sessions[0], week1.EndDate.AddDays(3)); // lands in week 2's range instead

        var badWeek = ReplaceSession(week1, index: 0, outOfWeekSession);

        var result = _validator.Validate(Clone(payload, weeks: new[] { badWeek, payload.Weeks[1] }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SessionDateOutsideOwningWeek, result.Errors);
    }

    [Fact]
    public void Validate_SessionDateOutsidePlanRange_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var lastWeek = payload.Weeks[^1];
        var outOfPlanSession = ReDate(lastWeek.Sessions[^1], lastWeek.EndDate.AddDays(10));
        // Also push the owning week's own EndDate out so this is purely a plan-range violation,
        // not merely re-triggering the owning-week check.
        var stretchedWeek = new GeneratedCatalogWeekPayload
        {
            WeekNumber = lastWeek.WeekNumber,
            StartDate = lastWeek.StartDate,
            EndDate = lastWeek.EndDate.AddDays(10),
            StageKey = lastWeek.StageKey,
            PlannedVolumeKm = lastWeek.PlannedVolumeKm,
            Sessions = ReplaceSessionList(lastWeek.Sessions, lastWeek.Sessions.Count - 1, outOfPlanSession),
            Provenance = lastWeek.Provenance,
        };

        var result = _validator.Validate(Clone(payload, weeks: new[] { payload.Weeks[0], stretchedWeek }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SessionDateOutsidePlanRange, result.Errors);
    }

    [Fact]
    public void Validate_WeekSessionCount_NotEqualToDaysPerWeek_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var week1 = payload.Weeks[0];
        var threeSessionWeek = new GeneratedCatalogWeekPayload
        {
            WeekNumber = week1.WeekNumber,
            StartDate = week1.StartDate,
            EndDate = week1.EndDate,
            StageKey = week1.StageKey,
            PlannedVolumeKm = week1.PlannedVolumeKm,
            Sessions = week1.Sessions.Take(3).ToList(), // pilot expects 4 (payload.DaysPerWeek)
            Provenance = week1.Provenance,
        };

        var result = _validator.Validate(Clone(payload, weeks: new[] { threeSessionWeek, payload.Weeks[1] }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.WeekSessionCountIncorrect, result.Errors);
    }

    [Fact]
    public void GeneratedCatalogWorkoutType_HasNoRestEquivalentMember()
    {
        // Structural proof for Decision 4 ("rest-day workout entries are
        // rejected"): it is not merely runtime-rejected, it is impossible to
        // construct — the enum has no Rest-equivalent value at all.
        var names = Enum.GetNames(typeof(GeneratedCatalogWorkoutType));

        Assert.DoesNotContain(names, n => n.Equals("Rest", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeneratedCatalogTrainingDayPayload_HasNoRequiredOptionalClassificationField()
    {
        // Structural proof: no REQUIRED/OPTIONAL session field exists in the
        // Phase 4F.1 contract (explicitly deferred).
        var propertyNames = typeof(GeneratedCatalogTrainingDayPayload).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(propertyNames, n => n.Contains("Required", StringComparison.OrdinalIgnoreCase) ||
                                                    n.Contains("Optional", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GeneratedCatalogTrainingDayPayload_HasNoRecoveryJogSuggestionField()
    {
        // Structural proof: recovery-jog suggestions are outside the schedule
        // contract entirely (belong to a future recommendation/DailyTip/
        // Notification concern), never a field on the session contract.
        var propertyNames = typeof(GeneratedCatalogTrainingDayPayload).GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(propertyNames, n => n.Contains("Recovery", StringComparison.OrdinalIgnoreCase) &&
                                                    n.Contains("Suggestion", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("RecoveryJog", StringComparison.OrdinalIgnoreCase));
    }

    // ── Prescription validation (tests #20-#28) ──────────────────────────────

    [Fact]
    public void Validate_DistanceBasis_PositiveTargetDistance_NoTargetDuration_IsValid()
    {
        var session = SingleValidDistanceSession(targetDistanceKm: 5.0);
        Assert.True(_validator.Validate(SingleSessionPlan(session)).IsValid);
    }

    [Fact]
    public void Validate_DistanceBasis_NonPositiveTargetDistance_IsInvalid()
    {
        var session = SingleValidDistanceSession(targetDistanceKm: 0);
        var result = _validator.Validate(SingleSessionPlan(session));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.DistancePrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_DistanceBasis_WithSecondAuthoritativeTargetDuration_IsInvalid()
    {
        var session = SingleValidDistanceSession(targetDistanceKm: 5.0).with_TargetDurationMinutes(30);
        var result = _validator.Validate(SingleSessionPlan(session));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.DistancePrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_DistanceBasis_OptionalEstimatedDurationMinutes_IsAllowed()
    {
        var session = SingleValidDistanceSession(targetDistanceKm: 5.0).with_EstimatedDurationMinutes(30);
        Assert.True(_validator.Validate(SingleSessionPlan(session)).IsValid);
    }

    [Fact]
    public void Validate_DurationBasis_PositiveTargetDuration_NoTargetDistance_IsValid()
    {
        var session = SingleValidDurationSession(targetDurationMinutes: 40);
        Assert.True(_validator.Validate(SingleSessionPlan(session)).IsValid);
    }

    [Fact]
    public void Validate_DurationBasis_NonPositiveTargetDuration_IsInvalid()
    {
        var session = SingleValidDurationSession(targetDurationMinutes: -5);
        var result = _validator.Validate(SingleSessionPlan(session));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.DurationPrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_DurationBasis_WithSecondAuthoritativeTargetDistance_IsInvalid()
    {
        var session = SingleValidDurationSession(targetDurationMinutes: 40).with_TargetDistanceKm(7.0);
        var result = _validator.Validate(SingleSessionPlan(session));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.DurationPrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_DurationBasis_OptionalEstimatedDistanceKm_IsAllowed()
    {
        var session = SingleValidDurationSession(targetDurationMinutes: 40).with_EstimatedDistanceKm(7.0);
        Assert.True(_validator.Validate(SingleSessionPlan(session)).IsValid);
    }

    [Fact]
    public void Validate_PlanWithBothDistanceAndDurationSessionsOnDifferentDays_IsValid()
    {
        // The two-week pilot fixture already mixes both bases across days —
        // proven valid by Validate_CompleteTwoWeekPilotFixture_IsValid.
        // This test asserts it explicitly for basis diversity.
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var bases = payload.Weeks.SelectMany(w => w.Sessions).Select(s => s.PrescriptionBasis).Distinct().ToList();

        Assert.Contains(GeneratedCatalogPrescriptionBasis.Distance, bases);
        Assert.Contains(GeneratedCatalogPrescriptionBasis.Duration, bases);
        Assert.True(_validator.Validate(payload).IsValid);
    }

    [Fact]
    public void GeneratedCatalogPlanPayload_HasNoHabitConversionMember()
    {
        // Structural proof: no habit-plan conversion behavior/field exists in
        // the Phase 4F.1 contract (explicitly deferred to a future phase).
        var allTypeNames = new[]
        {
            typeof(GeneratedCatalogPlanPayload), typeof(GeneratedCatalogWeekPayload),
            typeof(GeneratedCatalogTrainingDayPayload), typeof(GeneratedCatalogPacePrescription),
        }.SelectMany(t => t.GetProperties()).Select(p => p.Name);

        Assert.DoesNotContain(allTypeNames, n => n.Contains("HabitConversion", StringComparison.OrdinalIgnoreCase));
    }

    // ── Pace validation (tests #29-#33) ──────────────────────────────────────

    [Fact]
    public void Validate_TargetPace_RequiresPositiveTargetSecondsPerKm()
    {
        var valid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Target, TargetSecondsPerKm = 300 };
        var invalid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Target, TargetSecondsPerKm = null };

        Assert.True(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, valid))).IsValid);
        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, invalid)));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_RangePace_RequiresValidPositiveMinAndMax()
    {
        var valid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Range, MinSecondsPerKm = 280, MaxSecondsPerKm = 300 };
        var invalidOrder = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Range, MinSecondsPerKm = 320, MaxSecondsPerKm = 300 };
        var missingMax = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.Range, MinSecondsPerKm = 280, MaxSecondsPerKm = null };

        Assert.True(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, valid))).IsValid);
        Assert.False(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, invalidOrder))).IsValid);
        Assert.False(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, missingMax))).IsValid);
    }

    [Fact]
    public void Validate_EffortOnlyPace_RejectsAnyNumericTarget()
    {
        var valid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy" };
        var invalid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, TargetSecondsPerKm = 300 };

        Assert.True(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, valid))).IsValid);
        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, invalid)));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_NonePace_RejectsAnyNumericTarget()
    {
        var valid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.None };
        var invalid = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.None, MinSecondsPerKm = 280, MaxSecondsPerKm = 300 };

        Assert.True(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, valid))).IsValid);
        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, invalid)));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.PacePrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_DisplayText_IsNonAuthoritative_DoesNotAffectValidity()
    {
        var contradictoryDisplayText = new GeneratedCatalogPacePrescription
        {
            PaceType = GeneratedCatalogPaceType.Target,
            TargetSecondsPerKm = 300,
            DisplayText = "this text says something completely different, e.g. 2:00/km",
        };

        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, contradictoryDisplayText)));

        Assert.True(result.IsValid);
    }

    // ── Segment validation (tests #34-#39) ───────────────────────────────────

    [Fact]
    public void Validate_SessionWithNoSegments_IsValid()
    {
        var session = SingleValidDistanceSession(5.0);
        Assert.Empty(session.Segments);
        Assert.True(_validator.Validate(SingleSessionPlan(session)).IsValid);
    }

    [Fact]
    public void Validate_SegmentOrder_MustBeConsecutiveFromOne()
    {
        var segments = new[]
        {
            Segment(order: 1, GeneratedCatalogSegmentType.WarmUp, GeneratedCatalogPrescriptionBasis.Distance, distanceKm: 1.0),
            Segment(order: 3, GeneratedCatalogSegmentType.CoolDown, GeneratedCatalogPrescriptionBasis.Distance, distanceKm: 1.0), // gap: missing 2
        };

        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: segments)));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SegmentOrderInvalid, result.Errors);
    }

    [Fact]
    public void Validate_DuplicateSegmentOrder_IsInvalid()
    {
        var segments = new[]
        {
            Segment(order: 1, GeneratedCatalogSegmentType.WarmUp, GeneratedCatalogPrescriptionBasis.Distance, distanceKm: 1.0),
            Segment(order: 1, GeneratedCatalogSegmentType.CoolDown, GeneratedCatalogPrescriptionBasis.Distance, distanceKm: 1.0),
        };

        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: segments)));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SegmentOrderInvalid, result.Errors);
    }

    [Fact]
    public void Validate_SegmentDistanceBasis_ValidatesDistanceOnly()
    {
        var valid = Segment(1, GeneratedCatalogSegmentType.Steady, GeneratedCatalogPrescriptionBasis.Distance, distanceKm: 3.0);
        var invalid = Segment(1, GeneratedCatalogSegmentType.Steady, GeneratedCatalogPrescriptionBasis.Distance, distanceKm: null);

        Assert.True(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: new[] { valid }))).IsValid);
        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: new[] { invalid })));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_SegmentDurationBasis_ValidatesDurationOnly()
    {
        var valid = Segment(1, GeneratedCatalogSegmentType.WorkInterval, GeneratedCatalogPrescriptionBasis.Duration, durationSeconds: 120);
        var invalid = Segment(1, GeneratedCatalogSegmentType.WorkInterval, GeneratedCatalogPrescriptionBasis.Duration, durationSeconds: 0);

        Assert.True(_validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: new[] { valid }))).IsValid);
        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: new[] { invalid })));
        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid, result.Errors);
    }

    [Fact]
    public void Validate_InvalidRepetitionCount_IsInvalid()
    {
        var segment = Segment(1, GeneratedCatalogSegmentType.WorkInterval, GeneratedCatalogPrescriptionBasis.Duration, durationSeconds: 120, repetitionCount: 0);

        var result = _validator.Validate(SingleSessionPlan(SingleValidDistanceSession(5.0, segments: new[] { segment })));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.SegmentPrescriptionInvalid, result.Errors);
    }

    // ── Provenance (tests #40-#42; #43 covered separately at the DTO-surface level) ──

    [Fact]
    public void Validate_MissingPlanProvenanceCandidateKey_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var badProvenance = new GeneratedCatalogPlanProvenance
        {
            CandidateKey = "",
            CandidateVersion = payload.Provenance.CandidateVersion,
            DependencyVersions = payload.Provenance.DependencyVersions,
            GenerationSource = payload.Provenance.GenerationSource,
            AsOfDate = payload.Provenance.AsOfDate,
        };

        var result = _validator.Validate(Clone(payload, provenance: badProvenance));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.ProvenanceMissing, result.Errors);
    }

    [Fact]
    public void Validate_MissingWeekProvenanceStageKey_IsInvalid()
    {
        var payload = GeneratedCatalogPlanPayloadFixtures.ValidTwoWeekPlan();
        var week1 = payload.Weeks[0];
        var badWeek = new GeneratedCatalogWeekPayload
        {
            WeekNumber = week1.WeekNumber,
            StartDate = week1.StartDate,
            EndDate = week1.EndDate,
            StageKey = week1.StageKey,
            PlannedVolumeKm = week1.PlannedVolumeKm,
            Sessions = week1.Sessions,
            Provenance = new GeneratedCatalogWeekProvenance { StageKey = "" },
        };

        var result = _validator.Validate(Clone(payload, weeks: new[] { badWeek, payload.Weeks[1] }, plannedWeekCount: 2));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.ProvenanceMissing, result.Errors);
    }

    [Fact]
    public void Validate_MissingDayProvenanceSourceStageKey_IsInvalid()
    {
        var session = SingleValidDistanceSession(5.0);
        var badSession = new GeneratedCatalogTrainingDayPayload
        {
            Date = session.Date,
            SessionOrderInWeek = session.SessionOrderInWeek,
            WorkoutType = session.WorkoutType,
            PrescriptionBasis = session.PrescriptionBasis,
            TargetDistanceKm = session.TargetDistanceKm,
            TargetDurationMinutes = session.TargetDurationMinutes,
            EstimatedDistanceKm = session.EstimatedDistanceKm,
            EstimatedDurationMinutes = session.EstimatedDurationMinutes,
            PlannedIntensity = session.PlannedIntensity,
            PacePrescription = session.PacePrescription,
            Segments = session.Segments,
            Provenance = new GeneratedCatalogDayProvenance { SourceStageKey = "" },
        };

        var result = _validator.Validate(SingleSessionPlan(badSession));

        Assert.False(result.IsValid);
        Assert.Contains(GeneratedCatalogPlanPayloadValidationError.ProvenanceMissing, result.Errors);
    }

    // ── Small test-local helpers (no production code involved) ──────────────

    private static GeneratedCatalogPlanPayload WithSchemaVersion(GeneratedCatalogPlanPayload p, int schemaVersion) => new()
    {
        SchemaVersion = schemaVersion,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        PlannedWeekCount = p.PlannedWeekCount,
        DaysPerWeek = p.DaysPerWeek,
        CanonicalDistanceFamily = p.CanonicalDistanceFamily,
        GoalType = p.GoalType,
        CandidateKey = p.CandidateKey,
        CandidateVersion = p.CandidateVersion,
        DependencyVersions = p.DependencyVersions,
        Weeks = p.Weeks,
        Provenance = p.Provenance,
    };

    private static GeneratedCatalogPlanPayload Clone(
        GeneratedCatalogPlanPayload p,
        IReadOnlyList<GeneratedCatalogWeekPayload>? weeks = null,
        int? plannedWeekCount = null,
        DateOnly? endDate = null,
        GeneratedCatalogPlanProvenance? provenance = null) => new()
    {
        SchemaVersion = p.SchemaVersion,
        StartDate = p.StartDate,
        EndDate = endDate ?? p.EndDate,
        PlannedWeekCount = plannedWeekCount ?? p.PlannedWeekCount,
        DaysPerWeek = p.DaysPerWeek,
        CanonicalDistanceFamily = p.CanonicalDistanceFamily,
        GoalType = p.GoalType,
        CandidateKey = p.CandidateKey,
        CandidateVersion = p.CandidateVersion,
        DependencyVersions = p.DependencyVersions,
        Weeks = weeks ?? p.Weeks,
        Provenance = provenance ?? p.Provenance,
    };

    private static GeneratedCatalogWeekPayload ReNumber(GeneratedCatalogWeekPayload w, int weekNumber) => new()
    {
        WeekNumber = weekNumber,
        StartDate = w.StartDate,
        EndDate = w.EndDate,
        StageKey = w.StageKey,
        PlannedVolumeKm = w.PlannedVolumeKm,
        Sessions = w.Sessions,
        Provenance = w.Provenance,
    };

    private static GeneratedCatalogWeekPayload ReDate(GeneratedCatalogWeekPayload w, DateOnly start, DateOnly end) => new()
    {
        WeekNumber = w.WeekNumber,
        StartDate = start,
        EndDate = end,
        StageKey = w.StageKey,
        PlannedVolumeKm = w.PlannedVolumeKm,
        Sessions = w.Sessions,
        Provenance = w.Provenance,
    };

    private static GeneratedCatalogTrainingDayPayload ReDate(GeneratedCatalogTrainingDayPayload s, DateOnly date) => new()
    {
        Date = date,
        SessionOrderInWeek = s.SessionOrderInWeek,
        WorkoutType = s.WorkoutType,
        PrescriptionBasis = s.PrescriptionBasis,
        TargetDistanceKm = s.TargetDistanceKm,
        TargetDurationMinutes = s.TargetDurationMinutes,
        EstimatedDistanceKm = s.EstimatedDistanceKm,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes,
        PlannedIntensity = s.PlannedIntensity,
        PacePrescription = s.PacePrescription,
        Segments = s.Segments,
        Provenance = s.Provenance,
    };

    private static GeneratedCatalogTrainingDayPayload ReOrder(GeneratedCatalogTrainingDayPayload s, int order) => new()
    {
        Date = s.Date,
        SessionOrderInWeek = order,
        WorkoutType = s.WorkoutType,
        PrescriptionBasis = s.PrescriptionBasis,
        TargetDistanceKm = s.TargetDistanceKm,
        TargetDurationMinutes = s.TargetDurationMinutes,
        EstimatedDistanceKm = s.EstimatedDistanceKm,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes,
        PlannedIntensity = s.PlannedIntensity,
        PacePrescription = s.PacePrescription,
        Segments = s.Segments,
        Provenance = s.Provenance,
    };

    private static GeneratedCatalogWeekPayload ReplaceSession(GeneratedCatalogWeekPayload w, int index, GeneratedCatalogTrainingDayPayload replacement) =>
        new()
        {
            WeekNumber = w.WeekNumber,
            StartDate = w.StartDate,
            EndDate = w.EndDate,
            StageKey = w.StageKey,
            PlannedVolumeKm = w.PlannedVolumeKm,
            Sessions = ReplaceSessionList(w.Sessions, index, replacement),
            Provenance = w.Provenance,
        };

    private static IReadOnlyList<GeneratedCatalogTrainingDayPayload> ReplaceSessionList(
        IReadOnlyList<GeneratedCatalogTrainingDayPayload> sessions, int index, GeneratedCatalogTrainingDayPayload replacement)
    {
        var list = sessions.ToList();
        list[index] = replacement;
        return list;
    }

    private static GeneratedCatalogPlanPayload SingleSessionPlan(GeneratedCatalogTrainingDayPayload session)
    {
        var start = new DateOnly(2026, 8, 3);
        var week = new GeneratedCatalogWeekPayload
        {
            WeekNumber = 1,
            StartDate = start,
            EndDate = start.AddDays(6),
            StageKey = "BUILD",
            PlannedVolumeKm = session.TargetDistanceKm ?? session.EstimatedDistanceKm ?? 0,
            Sessions = new[] { session },
            Provenance = new GeneratedCatalogWeekProvenance { StageKey = "BUILD" },
        };

        return new GeneratedCatalogPlanPayload
        {
            SchemaVersion = GeneratedCatalogPlanPayload.CurrentSchemaVersion,
            StartDate = start,
            EndDate = week.EndDate,
            PlannedWeekCount = 1,
            DaysPerWeek = 1,
            CanonicalDistanceFamily = "TEN_K",
            GoalType = GoalType.Race,
            CandidateKey = "TEN_K__4D__INTERMEDIATE",
            CandidateVersion = 10,
            DependencyVersions = new Dictionary<string, PlanCatalogReference>
            {
                ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
            },
            Weeks = new[] { week },
            Provenance = new GeneratedCatalogPlanProvenance
            {
                CandidateKey = "TEN_K__4D__INTERMEDIATE",
                CandidateVersion = 10,
                DependencyVersions = new Dictionary<string, PlanCatalogReference>
                {
                    ["masterTemplate"] = new PlanCatalogReference("TEN_K_MASTER", 6),
                },
                GenerationSource = "CATALOG",
                AsOfDate = start,
            },
        };
    }

    private static GeneratedCatalogTrainingDayPayload SingleValidDistanceSession(
        double targetDistanceKm,
        GeneratedCatalogPacePrescription? pace = null,
        IReadOnlyList<GeneratedCatalogWorkoutSegmentPayload>? segments = null) => new()
    {
        Date = new DateOnly(2026, 8, 3),
        SessionOrderInWeek = 1,
        WorkoutType = GeneratedCatalogWorkoutType.Easy,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Distance,
        TargetDistanceKm = targetDistanceKm,
        TargetDurationMinutes = null,
        EstimatedDistanceKm = null,
        EstimatedDurationMinutes = null,
        PlannedIntensity = "z2",
        PacePrescription = pace ?? new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "easy" },
        Segments = segments ?? Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance { SourceStageKey = "BUILD" },
    };

    private static GeneratedCatalogTrainingDayPayload SingleValidDurationSession(int targetDurationMinutes) => new()
    {
        Date = new DateOnly(2026, 8, 3),
        SessionOrderInWeek = 1,
        WorkoutType = GeneratedCatalogWorkoutType.Tempo,
        PrescriptionBasis = GeneratedCatalogPrescriptionBasis.Duration,
        TargetDistanceKm = null,
        TargetDurationMinutes = targetDurationMinutes,
        EstimatedDistanceKm = null,
        EstimatedDurationMinutes = null,
        PlannedIntensity = "z3",
        PacePrescription = new GeneratedCatalogPacePrescription { PaceType = GeneratedCatalogPaceType.EffortOnly, EffortLabel = "moderate" },
        Segments = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>(),
        Provenance = new GeneratedCatalogDayProvenance { SourceStageKey = "BUILD" },
    };

    private static GeneratedCatalogWorkoutSegmentPayload Segment(
        int order, GeneratedCatalogSegmentType type, GeneratedCatalogPrescriptionBasis basis,
        double? distanceKm = null, int? durationSeconds = null, int? repetitionCount = null) => new()
    {
        SegmentOrder = order,
        SegmentType = type,
        RepetitionCount = repetitionCount,
        PrescriptionBasis = basis,
        TargetDistanceKm = distanceKm,
        TargetDurationSeconds = durationSeconds,
        PacePrescription = null,
    };
}

/// <summary>Small test-only extension helpers to build prescription-mismatch fixtures tersely.</summary>
file static class SessionMutationExtensions
{
    public static GeneratedCatalogTrainingDayPayload with_TargetDurationMinutes(this GeneratedCatalogTrainingDayPayload s, int? value) => new()
    {
        Date = s.Date, SessionOrderInWeek = s.SessionOrderInWeek, WorkoutType = s.WorkoutType, PrescriptionBasis = s.PrescriptionBasis,
        TargetDistanceKm = s.TargetDistanceKm, TargetDurationMinutes = value, EstimatedDistanceKm = s.EstimatedDistanceKm,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes, PlannedIntensity = s.PlannedIntensity, PacePrescription = s.PacePrescription,
        Segments = s.Segments, Provenance = s.Provenance,
    };

    public static GeneratedCatalogTrainingDayPayload with_TargetDistanceKm(this GeneratedCatalogTrainingDayPayload s, double? value) => new()
    {
        Date = s.Date, SessionOrderInWeek = s.SessionOrderInWeek, WorkoutType = s.WorkoutType, PrescriptionBasis = s.PrescriptionBasis,
        TargetDistanceKm = value, TargetDurationMinutes = s.TargetDurationMinutes, EstimatedDistanceKm = s.EstimatedDistanceKm,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes, PlannedIntensity = s.PlannedIntensity, PacePrescription = s.PacePrescription,
        Segments = s.Segments, Provenance = s.Provenance,
    };

    public static GeneratedCatalogTrainingDayPayload with_EstimatedDurationMinutes(this GeneratedCatalogTrainingDayPayload s, int? value) => new()
    {
        Date = s.Date, SessionOrderInWeek = s.SessionOrderInWeek, WorkoutType = s.WorkoutType, PrescriptionBasis = s.PrescriptionBasis,
        TargetDistanceKm = s.TargetDistanceKm, TargetDurationMinutes = s.TargetDurationMinutes, EstimatedDistanceKm = s.EstimatedDistanceKm,
        EstimatedDurationMinutes = value, PlannedIntensity = s.PlannedIntensity, PacePrescription = s.PacePrescription,
        Segments = s.Segments, Provenance = s.Provenance,
    };

    public static GeneratedCatalogTrainingDayPayload with_EstimatedDistanceKm(this GeneratedCatalogTrainingDayPayload s, double? value) => new()
    {
        Date = s.Date, SessionOrderInWeek = s.SessionOrderInWeek, WorkoutType = s.WorkoutType, PrescriptionBasis = s.PrescriptionBasis,
        TargetDistanceKm = s.TargetDistanceKm, TargetDurationMinutes = s.TargetDurationMinutes, EstimatedDistanceKm = value,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes, PlannedIntensity = s.PlannedIntensity, PacePrescription = s.PacePrescription,
        Segments = s.Segments, Provenance = s.Provenance,
    };
}
