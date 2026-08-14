namespace PlanCatalog.Core.LongHorizon;

/// <summary>
/// Phase 4I.4 — fail-closed validation for a resolved GE week-descriptor
/// sequence. Never auto-corrects invalid catalog data; only reports it.
/// </summary>
public sealed record LongHorizonGeCatalogValidationResult(bool IsValid, IReadOnlyList<string> Findings)
{
    public static LongHorizonGeCatalogValidationResult Valid() => new(true, []);
    public static LongHorizonGeCatalogValidationResult Invalid(IEnumerable<string> findings) => new(false, findings.ToList());
}

public static class LongHorizonGeCatalogValidator
{
    private static readonly IReadOnlySet<string> ProhibitedFamilies = new HashSet<string> { "RACE" };
    private static readonly IReadOnlySet<string> ProhibitedKeys = new HashSet<string> { "THRESHOLD_TEMPO", "GOAL_PACE_TEN_K" };

    public static LongHorizonGeCatalogValidationResult Validate(int geWeeks, IReadOnlyList<GeWeekDescriptor> weeks)
    {
        var findings = new List<string>();

        if (geWeeks is < 1 or > 32)
            findings.Add($"geWeeks={geWeeks} is outside the approved 1..32 range.");

        if (weeks.Count != geWeeks)
            findings.Add($"Resolved {weeks.Count} weeks but expected exactly {geWeeks}.");

        for (var i = 0; i < weeks.Count; i++)
        {
            var week = weeks[i];
            var label = $"week[{i}] (WeekIndex={week.WeekIndex})";

            if (week.WeekIndex != i + 1)
                findings.Add($"{label}: WeekIndex out of order.");

            if (week.SegmentType != GeWeekDescriptor.LongHorizonGeneralEnduranceSegmentType)
                findings.Add($"{label}: unexpected SegmentType '{week.SegmentType}'.");

            if (week.Roles.Count != 4 ||
                !week.Roles.ContainsKey(GeWeekRole.KeySession) ||
                !week.Roles.ContainsKey(GeWeekRole.EasySupportA) ||
                !week.Roles.ContainsKey(GeWeekRole.EasySupportB) ||
                !week.Roles.ContainsKey(GeWeekRole.LongRun))
                findings.Add($"{label}: does not have exactly one KEY_SESSION, two EASY_SUPPORT, and one LONG_RUN role.");

            foreach (var (role, workout) in week.Roles)
            {
                if (ProhibitedKeys.Contains(workout.Key))
                    findings.Add($"{label} role={role}: prohibited workout '{workout.Key}' (threshold/goal-pace/race-specific content is never allowed in LONG_HORIZON_GENERAL_ENDURANCE).");
                if (ProhibitedFamilies.Contains(workout.Family))
                    findings.Add($"{label} role={role}: prohibited workout family '{workout.Family}'.");
            }

            if (week.Classification == GeDurationClassification.ShortExtension && week.IsRecoveryWeek)
                findings.Add($"{label}: ShortExtension weeks must never be recovery weeks.");

            if (week.Classification == GeDurationClassification.FullPhase)
            {
                var isMesocycleWeek = week.MesocycleIndex is not null;
                var isTerminalWeek = week.IsTerminalAlignment;
                if (isMesocycleWeek == isTerminalWeek)
                    findings.Add($"{label}: FullPhase week must be exactly one of mesocycle-positioned or terminal-alignment, never both/neither.");

                if (isMesocycleWeek && week.MesocyclePosition == MesocyclePosition.RecoveryConsolidation != week.IsRecoveryWeek)
                    findings.Add($"{label}: IsRecoveryWeek must be true exactly when MesocyclePosition is RecoveryConsolidation.");

                if (isTerminalWeek && week.IsRecoveryWeek)
                    findings.Add($"{label}: terminal-alignment weeks must never be recovery weeks.");
            }

            if (string.IsNullOrWhiteSpace(week.CatalogSourceId))
                findings.Add($"{label}: missing CatalogSourceId provenance.");
        }

        // Recovery-every-fourth-week invariant for FullPhase mesocycle weeks.
        var mesocycleWeeks = weeks.Where(w => w.MesocycleIndex is not null).ToList();
        for (var i = 0; i < mesocycleWeeks.Count; i++)
        {
            var expectedRecovery = (i + 1) % 4 == 0;
            if (mesocycleWeeks[i].IsRecoveryWeek != expectedRecovery)
                findings.Add($"Mesocycle week at overall position {i + 1} has IsRecoveryWeek={mesocycleWeeks[i].IsRecoveryWeek}, expected {expectedRecovery} (recovery must land exactly every fourth mesocycle week).");
        }

        // Terminal remainder must be a contiguous suffix, never interleaved.
        var firstTerminalIndex = weeks.ToList().FindIndex(w => w.IsTerminalAlignment);
        if (firstTerminalIndex >= 0)
        {
            for (var i = firstTerminalIndex; i < weeks.Count; i++)
            {
                if (!weeks[i].IsTerminalAlignment)
                    findings.Add($"week[{i}]: non-terminal week found after the terminal-alignment remainder began at index {firstTerminalIndex}.");
            }
        }

        return findings.Count == 0
            ? LongHorizonGeCatalogValidationResult.Valid()
            : LongHorizonGeCatalogValidationResult.Invalid(findings);
    }
}
