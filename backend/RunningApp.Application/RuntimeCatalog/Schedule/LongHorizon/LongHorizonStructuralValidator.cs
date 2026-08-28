namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>Fail-closed, aggregate (not first-fail) validation result -- mirrors the established <c>TenKPreparationRunwayFinalInvariantValidator</c>/<c>GeneratedCatalogPlanSkeletonValidator</c> convention.</summary>
internal sealed record LongHorizonStructuralValidationResult(bool IsValid, IReadOnlyList<string> Findings)
{
    public static LongHorizonStructuralValidationResult Valid() => new(true, Array.Empty<string>());
    public static LongHorizonStructuralValidationResult Invalid(IReadOnlyList<string> findings) => new(false, findings);
}

/// <summary>
/// Phase 4I.5 — the single structural validator for a joined
/// <see cref="LongHorizonGeneratedStructuralSkeleton"/>. Never auto-corrects;
/// every violation is reported as a finding, never silently repaired.
/// </summary>
internal static class LongHorizonStructuralValidator
{
    private static readonly HashSet<string> ProhibitedGeWorkoutKeys = new(StringComparer.Ordinal)
    {
        "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K", "VO2MAX_INTERVAL", "RACE_SPECIFIC_SIMULATION",
    };

    private static readonly LongHorizonSegmentType[] ExpectedSegmentOrder =
    [
        LongHorizonSegmentType.LongHorizonGeneralEndurance,
        LongHorizonSegmentType.PreparationRunway,
        LongHorizonSegmentType.Core,
    ];

    public static LongHorizonStructuralValidationResult Validate(LongHorizonGeneratedStructuralSkeleton skeleton)
    {
        var findings = new List<string>();

        if (skeleton.TotalWeeks is < 21 or > 52)
            findings.Add($"TotalWeeks must be 21..52; was {skeleton.TotalWeeks}.");
        if (skeleton.PreparationRunwayWeeks != 8)
            findings.Add($"PreparationRunwayWeeks must be exactly 8; was {skeleton.PreparationRunwayWeeks}.");
        if (skeleton.CoreWeeks != 12)
            findings.Add($"CoreWeeks must be exactly 12; was {skeleton.CoreWeeks}.");
        if (skeleton.GeneralEnduranceWeeks != skeleton.TotalWeeks - 20)
            findings.Add($"GeneralEnduranceWeeks ({skeleton.GeneralEnduranceWeeks}) must equal TotalWeeks-20 ({skeleton.TotalWeeks - 20}).");
        if (skeleton.GeneralEnduranceWeeks + skeleton.PreparationRunwayWeeks + skeleton.CoreWeeks != skeleton.TotalWeeks)
            findings.Add("Segment counts do not sum to TotalWeeks.");
        if (skeleton.Weeks.Count != skeleton.TotalWeeks)
            findings.Add($"Weeks.Count ({skeleton.Weeks.Count}) does not equal TotalWeeks ({skeleton.TotalWeeks}).");

        var numbers = skeleton.Weeks.Select(w => w.GlobalWeekNumber).ToList();
        if (!numbers.SequenceEqual(Enumerable.Range(1, skeleton.Weeks.Count)))
            findings.Add("GlobalWeekNumber sequence is not contiguous, ascending, and 1-based without gaps or duplicates.");
        if (skeleton.Weeks.Count > 0 && skeleton.Weeks[^1].GlobalWeekNumber != skeleton.TotalWeeks)
            findings.Add("Final week's GlobalWeekNumber does not equal TotalWeeks.");

        var segments = skeleton.Weeks.Select(w => w.Segment).ToList();
        var observedDistinctOrder = segments.Distinct().ToList();
        if (!observedDistinctOrder.SequenceEqual(ExpectedSegmentOrder))
            findings.Add($"Segment order must be exactly GE, Runway, Core (with all three present); observed [{string.Join(", ", observedDistinctOrder)}].");
        else
        {
            var runs = new List<LongHorizonSegmentType>();
            foreach (var s in segments)
            {
                if (runs.Count == 0 || runs[^1] != s)
                    runs.Add(s);
            }
            if (!runs.SequenceEqual(ExpectedSegmentOrder))
                findings.Add("Segments are interleaved -- each segment must appear as exactly one contiguous run.");
        }

        var geCount = segments.Count(s => s == LongHorizonSegmentType.LongHorizonGeneralEndurance);
        var runwayCount = segments.Count(s => s == LongHorizonSegmentType.PreparationRunway);
        var coreCount = segments.Count(s => s == LongHorizonSegmentType.Core);
        if (geCount != skeleton.GeneralEnduranceWeeks)
            findings.Add($"GE week count in Weeks ({geCount}) does not match GeneralEnduranceWeeks ({skeleton.GeneralEnduranceWeeks}).");
        if (runwayCount != 8)
            findings.Add($"Runway week count in Weeks ({runwayCount}) is not 8.");
        if (coreCount != 12)
            findings.Add($"Core week count in Weeks ({coreCount}) is not 12.");

        // Phase 10K-FREQ.6D.14 -- generalized off the candidate's own resolved
        // shape instead of a hardcoded "exactly 4" literal. GE/Runway carry the
        // same role composition as each other (1 KEY + N EASY + 1 LONG); Core's
        // own approved shape is genuinely different (2 KEY + N EASY + 1 LONG),
        // so expected KEY/EASY counts are derived per segment, not uniformly.
        // Byte-identical for every 4D candidate.
        // Phase 10K-FREQ.6D.26 -- widened from a boolean isFiveDay to a
        // three-way dispatch recognizing 4D/5D/6D by candidate key.
        // Phase 10K-GEN.9 -- widened to recognize the approved Advanced
        // 3D/4D/5D/6D candidate identities (GEN.7/GEN.8 authority) alongside
        // Intermediate's. Advanced x3D (a genuinely new daysPerWeek value no
        // prior Level ever exercised in LongHorizon) resolves to 3.
        var expectedSlotCount = skeleton.CandidateKey switch
        {
            LongHorizonStructuralMaterializer.CandidateKeyAdvancedThreeDay => 3,
            LongHorizonStructuralMaterializer.CandidateKeyFiveDay or LongHorizonStructuralMaterializer.CandidateKeyAdvancedFiveDay => 5,
            LongHorizonStructuralMaterializer.CandidateKeySixDay or LongHorizonStructuralMaterializer.CandidateKeyAdvancedSixDay => 6,
            _ => 4,
        };
        var hasDualKeyCore = skeleton.CandidateKey is LongHorizonStructuralMaterializer.CandidateKeyFiveDay
            or LongHorizonStructuralMaterializer.CandidateKeySixDay
            or LongHorizonStructuralMaterializer.CandidateKeyAdvancedFiveDay
            or LongHorizonStructuralMaterializer.CandidateKeyAdvancedSixDay;

        foreach (var week in skeleton.Weeks)
        {
            if (week.OrderedWorkoutSlots.Count != expectedSlotCount)
            {
                findings.Add($"Global week {week.GlobalWeekNumber} has {week.OrderedWorkoutSlots.Count} slots, expected exactly {expectedSlotCount}.");
            }
            else
            {
                var keyCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "KEY_SESSION");
                var easyCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "EASY_SUPPORT");
                var longCount = week.OrderedWorkoutSlots.Count(s => s.StructuralRole == "LONG_RUN");
                var (expectedKey, expectedEasy) = hasDualKeyCore && week.Segment == LongHorizonSegmentType.Core
                    ? (2, expectedSlotCount - 3)
                    : (1, expectedSlotCount - 2);
                if (keyCount != expectedKey || easyCount != expectedEasy || longCount != 1)
                    findings.Add($"Global week {week.GlobalWeekNumber} does not contain exactly {expectedKey} KEY_SESSION, {expectedEasy} EASY_SUPPORT, and 1 LONG_RUN slot.");
                var indices = week.OrderedWorkoutSlots.Select(s => s.StructuralSlotIndex).ToList();
                if (!indices.SequenceEqual(Enumerable.Range(1, indices.Count)))
                    findings.Add($"Global week {week.GlobalWeekNumber} slot indices are not 1..{expectedSlotCount} in order.");
            }

            switch (week.Segment)
            {
                case LongHorizonSegmentType.LongHorizonGeneralEndurance:
                    if (week.RunwayBlock is not null)
                        findings.Add($"GE week {week.GlobalWeekNumber} carries a non-null RunwayBlock.");
                    if (week.CorePhase is not null)
                        findings.Add($"GE week {week.GlobalWeekNumber} carries a non-null CorePhase.");
                    if (week.GeStageFamily is null || week.GeClassification is null)
                        findings.Add($"GE week {week.GlobalWeekNumber} is missing GE stage/classification provenance.");
                    if (week.OrderedWorkoutSlots.Any(s => s.WorkoutKey is null || s.WorkoutVersion is null))
                        findings.Add($"GE week {week.GlobalWeekNumber} has a slot with a null workout reference.");
                    if (week.OrderedWorkoutSlots.Any(s => s.WorkoutKey is not null && ProhibitedGeWorkoutKeys.Contains(s.WorkoutKey)))
                        findings.Add($"GE week {week.GlobalWeekNumber} references a prohibited (threshold/VO2max/goal-pace/race-specific) workout key.");
                    break;
                case LongHorizonSegmentType.PreparationRunway:
                    if (week.RunwayBlock is null)
                        findings.Add($"Runway week {week.GlobalWeekNumber} is missing RunwayBlock provenance.");
                    if (week.CorePhase is not null)
                        findings.Add($"Runway week {week.GlobalWeekNumber} carries a non-null CorePhase.");
                    if (week.GeStageFamily is not null || week.GeClassification is not null)
                        findings.Add($"Runway week {week.GlobalWeekNumber} carries GE stage/classification metadata.");
                    if (week.OrderedWorkoutSlots.Any(s => s.WorkoutKey is null || s.WorkoutVersion is null))
                        findings.Add($"Runway week {week.GlobalWeekNumber} has a slot with a null workout reference.");
                    break;
                case LongHorizonSegmentType.Core:
                    if (week.RunwayBlock is not null)
                        findings.Add($"Core week {week.GlobalWeekNumber} carries a non-null RunwayBlock.");
                    if (week.CorePhase is null)
                        findings.Add($"Core week {week.GlobalWeekNumber} is missing CorePhase provenance.");
                    if (week.GeStageFamily is not null || week.GeClassification is not null)
                        findings.Add($"Core week {week.GlobalWeekNumber} carries GE stage/classification metadata.");
                    break;
            }
        }

        var geWeeksList = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.LongHorizonGeneralEndurance).ToList();
        foreach (var week in geWeeksList)
        {
            if (week.GeClassification == GeneralEnduranceDurationClassification.FullPhase && week.MesocycleIndex is not null)
            {
                var expectedRecovery = week.LocalSegmentWeekNumber % 4 == 0;
                if (week.IsRecoveryWeek != expectedRecovery)
                    findings.Add($"GE week {week.GlobalWeekNumber} recovery flag ({week.IsRecoveryWeek}) does not match its mesocycle position (local week {week.LocalSegmentWeekNumber}).");
            }
            if (week.GeClassification == GeneralEnduranceDurationClassification.ShortExtension && week.IsRecoveryWeek == true)
                findings.Add($"GE week {week.GlobalWeekNumber} is ShortExtension but flagged as a recovery week -- ShortExtension never contains recovery.");
        }

        var localNumbersByGe = geWeeksList.Select(w => w.LocalSegmentWeekNumber).ToList();
        if (!localNumbersByGe.SequenceEqual(Enumerable.Range(1, localNumbersByGe.Count)))
            findings.Add("GE local segment week numbers are not contiguous 1..N.");

        var runwayWeeksList = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.PreparationRunway).Select(w => w.LocalSegmentWeekNumber).ToList();
        if (!runwayWeeksList.SequenceEqual(Enumerable.Range(1, runwayWeeksList.Count)))
            findings.Add("Runway local segment week numbers are not contiguous 1..8.");

        var coreWeeksList = skeleton.Weeks.Where(w => w.Segment == LongHorizonSegmentType.Core).Select(w => w.LocalSegmentWeekNumber).ToList();
        if (!coreWeeksList.SequenceEqual(Enumerable.Range(1, coreWeeksList.Count)))
            findings.Add("Core local segment week numbers are not contiguous 1..12.");

        return findings.Count == 0 ? LongHorizonStructuralValidationResult.Valid() : LongHorizonStructuralValidationResult.Invalid(findings);
    }
}
