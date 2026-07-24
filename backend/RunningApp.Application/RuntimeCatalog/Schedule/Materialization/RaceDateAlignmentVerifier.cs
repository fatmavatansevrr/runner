namespace RunningApp.Application.RuntimeCatalog.Schedule.Materialization;

internal enum RaceDateAlignmentOutcome
{
    Pass,
    Fail,
    NotApplicable,
}

internal sealed record RaceDateAlignmentCheck(string CheckName, bool Passed, string Detail);

internal sealed record RaceDateAlignmentVerificationResult(
    int TargetWeeks,
    DateOnly StartDate,
    DateOnly RaceDate,
    DateOnly FinalSessionDate,
    IReadOnlyList<RaceDateAlignmentCheck> Checks,
    RaceDateAlignmentOutcome Outcome,
    IReadOnlyList<string> Findings);

/// <summary>
/// Phase 4G.3B.3, verifier 9 of 9 (final). Independently re-checks, for an
/// already-generated dated schedule of any target week count, the same
/// race-date alignment invariant CatalogPreviewGenerator already enforces
/// live for the 12-week horizon.
///
/// LIVE-CHECK FINDING (read before writing any logic here, per this phase's
/// own instruction not to assume "horizon-agnostic" without confirming in
/// code): CatalogPreviewGenerator.cs's defensive backstop
/// (CatalogRaceDateAlignmentInvalidException, ~line 583-596) is:
/// <code>
/// var expectedWeekCount = RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks; // 12
/// var daysBeforeRace = raceDate.DayNumber - datedSkeleton.EndDate.DayNumber;
/// const int maxAllowedTrailingGapDays = 7;
/// if (datedSkeleton.Weeks.Count != expectedWeekCount || daysBeforeRace &lt; 0 || daysBeforeRace &gt; maxAllowedTrailingGapDays)
///     throw new CatalogRaceDateAlignmentInvalidException(...);
/// </code>
/// This is NOT genuinely horizon-agnostic in code, contrary to Phase
/// 4G.3B.1's narrative characterization: the condition ORs together a
/// hardcoded `Weeks.Count != 12` clause with the date-tolerance clause, so
/// the exception fires for ANY week count other than exactly 12, even if
/// the dates themselves are perfectly aligned. Only the date-tolerance
/// sub-condition (`0 &lt;= daysBeforeRace &lt;= 7`, i.e. the final session
/// falls on or up to 7 days before RaceDate, never after) is actually
/// week-count-independent logic; the surrounding `Weeks.Count != 12` guard
/// is not. This is a real discrepancy between the prior narrative and the
/// actual code -- reported in full in this phase's report, a risk-entry
/// addition is recommended (not created) there. This verifier deliberately
/// checks only the genuinely week-count-independent date-tolerance logic
/// (mirroring what the live check enforces about DATES specifically), not
/// the live check's additional exact-12-weeks clause, since re-asserting
/// "must be exactly 12 weeks" here would defeat the entire purpose of
/// checking 8-14 week schedules independently.
///
/// Pure function of its two inputs (the dated schedule and the RaceDate it
/// must align to -- RaceDate is not itself carried by
/// DatedGeneratedCatalogPlanSkeleton, so it must be supplied explicitly;
/// this deviates from the task's single-parameter illustrative signature
/// for exactly that reason, reported in the final report). No calls to
/// CatalogWeekSkeletonCalendarMaterializer, the phase allocator, the stage
/// scheduler, or the workout binder. Not called from any live request path.
/// </summary>
internal static class RaceDateAlignmentVerifier
{
    private const int MaxAllowedTrailingGapDays = 7;
    private const string TaperPhaseKey = "TAPER";

    public static RaceDateAlignmentVerificationResult Verify(DatedGeneratedCatalogPlanSkeleton datedSchedule, DateOnly raceDate)
    {
        var allSessions = datedSchedule.Weeks.SelectMany(w => w.SessionSlots.Select(s => (Week: w, Slot: s))).ToList();

        if (datedSchedule.Weeks.Count == 0 || allSessions.Count == 0)
        {
            return new(datedSchedule.PlannedWeekCount, datedSchedule.StartDate, raceDate, datedSchedule.StartDate, [],
                RaceDateAlignmentOutcome.NotApplicable,
                ["NOT_APPLICABLE_EMPTY_SCHEDULE: no weeks or sessions are present to verify alignment against."]);
        }

        var findings = new List<string>();
        var checks = new List<RaceDateAlignmentCheck>();
        var finalSessionDate = allSessions.Max(s => s.Slot.SessionDate);

        // (a) NoSessionAfterRaceDate
        var afterRace = allSessions.Where(s => s.Slot.SessionDate > raceDate).ToList();
        foreach (var violation in afterRace)
        {
            findings.Add($"SESSION_AFTER_RACE_DATE: week {violation.Week.WeekNumber} session on {violation.Slot.SessionDate:yyyy-MM-dd} ({violation.Slot.StructuralRole}) falls after RaceDate {raceDate:yyyy-MM-dd}.");
        }
        checks.Add(new RaceDateAlignmentCheck("NoSessionAfterRaceDate", afterRace.Count == 0,
            afterRace.Count == 0 ? "Every session date is on or before RaceDate." : $"{afterRace.Count} session(s) fall after RaceDate."));

        // (b) FinalSessionWithinTolerance -- mirrors the live check's own
        // date-tolerance sub-condition exactly (0 <= daysBeforeRace <= 7).
        var daysBeforeRace = raceDate.DayNumber - finalSessionDate.DayNumber;
        var withinTolerance = daysBeforeRace >= 0 && daysBeforeRace <= MaxAllowedTrailingGapDays;
        if (!withinTolerance)
        {
            findings.Add($"FINAL_SESSION_OUTSIDE_TOLERANCE: FinalSessionDate={finalSessionDate:yyyy-MM-dd} is {daysBeforeRace} day(s) before RaceDate={raceDate:yyyy-MM-dd}; must be between 0 and {MaxAllowedTrailingGapDays} days before, inclusive.");
        }
        checks.Add(new RaceDateAlignmentCheck("FinalSessionWithinTolerance", withinTolerance,
            $"FinalSessionDate is {daysBeforeRace} day(s) before RaceDate (allowed: 0-{MaxAllowedTrailingGapDays})."));

        // (c) NoUnexplainedGap -- week-number contiguity, no duplicate
        // session dates, every session's weekday is one of the plan's own
        // declared PreferredDays (LongRunDay is already validated upstream
        // to be a member of PreferredDays, so this single check covers both).
        var orderedWeekNumbers = datedSchedule.Weeks.Select(w => w.WeekNumber).OrderBy(n => n).ToList();
        var expectedWeekNumbers = Enumerable.Range(1, datedSchedule.PlannedWeekCount).ToList();
        var weekSequenceOk = orderedWeekNumbers.SequenceEqual(expectedWeekNumbers);
        if (!weekSequenceOk)
        {
            findings.Add($"WEEK_NUMBER_SEQUENCE_GAP: expected week numbers 1..{datedSchedule.PlannedWeekCount} contiguous, found [{string.Join(",", orderedWeekNumbers)}].");
        }

        var duplicateDates = allSessions.GroupBy(s => s.Slot.SessionDate).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        foreach (var duplicate in duplicateDates)
        {
            findings.Add($"DUPLICATE_SESSION_DATE: {duplicate:yyyy-MM-dd} is used by more than one session slot.");
        }

        var preferredDays = datedSchedule.Provenance.PreferredDays;
        var offPreferredDay = allSessions.Where(s => !preferredDays.Contains(s.Slot.SessionDayOfWeek)).ToList();
        foreach (var violation in offPreferredDay)
        {
            findings.Add($"SESSION_NOT_ON_PREFERRED_DAY: week {violation.Week.WeekNumber} session on {violation.Slot.SessionDate:yyyy-MM-dd} falls on {violation.Slot.SessionDayOfWeek}, not in PreferredDays [{string.Join(",", preferredDays)}].");
        }

        var noUnexplainedGap = weekSequenceOk && duplicateDates.Count == 0 && offPreferredDay.Count == 0;
        checks.Add(new RaceDateAlignmentCheck("NoUnexplainedGap", noUnexplainedGap,
            noUnexplainedGap ? "Week sequence is contiguous, no duplicate dates, every session falls on a preferred day." :
                $"{(weekSequenceOk ? "" : "week-sequence gap; ")}{duplicateDates.Count} duplicate date(s); {offPreferredDay.Count} off-preferred-day session(s)."));

        // (d) TaperWithinFinalWindow
        var taperSessions = allSessions.Where(s => s.Week.PhaseKey == TaperPhaseKey).ToList();
        var taperViolations = taperSessions.Where(s => s.Slot.SessionDate > finalSessionDate).ToList();
        foreach (var violation in taperViolations)
        {
            findings.Add($"TAPER_SESSION_BEYOND_FINAL_WINDOW: week {violation.Week.WeekNumber} taper session on {violation.Slot.SessionDate:yyyy-MM-dd} falls after FinalSessionDate {finalSessionDate:yyyy-MM-dd}.");
        }
        checks.Add(new RaceDateAlignmentCheck("TaperWithinFinalWindow", taperViolations.Count == 0,
            taperViolations.Count == 0 ? "Every taper-week session falls on or before the final pre-race session date." : $"{taperViolations.Count} taper session(s) fall beyond the final pre-race session date."));

        var outcome = checks.All(c => c.Passed) ? RaceDateAlignmentOutcome.Pass : RaceDateAlignmentOutcome.Fail;

        return new(datedSchedule.PlannedWeekCount, datedSchedule.StartDate, raceDate, finalSessionDate, checks, outcome, findings);
    }
}
