namespace PlanCatalog.Core.LongHorizon;

/// <summary>
/// Phase 4I.4 — the single deterministic catalog-only selector for the
/// LONG_HORIZON_GENERAL_ENDURANCE segment. Consumes an already-approved GE
/// week count (1-32, Phase 4I.1's formula) and a readiness profile, and
/// returns an ordered list of fully-resolved <see cref="GeWeekDescriptor"/>
/// values by combining a small, code-level structural rule (mesocycle/
/// remainder/ShortExtension placement -- deliberately NOT 32 hand-authored
/// catalog rows, per this phase's own architecture requirement) with the
/// catalog-driven <see cref="LongHorizonGeStageFamilyCatalogDocument"/> for
/// the actual workout-reference content of each stage family/role/profile
/// combination. Performs no date, volume, pace, or calendar computation.
/// </summary>
public static class LongHorizonGeCatalogSelector
{
    public static GeDurationClassification Classify(int geWeeks) =>
        geWeeks switch
        {
            >= 1 and <= 3 => GeDurationClassification.ShortExtension,
            >= 4 and <= 32 => GeDurationClassification.FullPhase,
            _ => throw new ArgumentOutOfRangeException(nameof(geWeeks), geWeeks, "GE week count must be 1..32."),
        };

    public static IReadOnlyList<GeWeekDescriptor> Select(
        int geWeeks,
        LongHorizonReadinessProfile profile,
        LongHorizonGeStageFamilyCatalogDocument catalog)
    {
        if (geWeeks is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(geWeeks), geWeeks, "GE week count must be 1..32.");

        return Classify(geWeeks) == GeDurationClassification.ShortExtension
            ? SelectShortExtension(geWeeks, profile, catalog)
            : SelectFullPhase(geWeeks, profile, catalog);
    }

    // ── ShortExtension (1-3 GE weeks) ───────────────────────────────────────

    private static IReadOnlyList<GeWeekDescriptor> SelectShortExtension(
        int geWeeks, LongHorizonReadinessProfile profile, LongHorizonGeStageFamilyCatalogDocument catalog)
    {
        // Per Phase 4I.1's own preferred semantics (reused verbatim, not
        // reinvented): 1 = EntryAlignment; 2 = EntryAlignment, PreRunwayAlignment;
        // 3 = EntryAlignment, ControlledDevelopment(BaseDevelopment), PreRunwayAlignment.
        var plan = geWeeks switch
        {
            1 => new[] { (ShortExtensionRole.EntryAlignment, GeStageFamily.Entry) },
            2 => new[]
            {
                (ShortExtensionRole.EntryAlignment, GeStageFamily.Entry),
                (ShortExtensionRole.PreRunwayAlignment, GeStageFamily.PreRunwayAlignment),
            },
            3 => new[]
            {
                (ShortExtensionRole.EntryAlignment, GeStageFamily.Entry),
                (ShortExtensionRole.ControlledDevelopment, GeStageFamily.BaseDevelopment),
                (ShortExtensionRole.PreRunwayAlignment, GeStageFamily.PreRunwayAlignment),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(geWeeks)),
        };

        var result = new List<GeWeekDescriptor>(plan.Length);
        for (var i = 0; i < plan.Length; i++)
        {
            var (role, stageFamily) = plan[i];
            var isTerminal = i == plan.Length - 1; // last ShortExtension week is always the alignment step
            result.Add(BuildDescriptor(
                weekIndex: i + 1,
                classification: GeDurationClassification.ShortExtension,
                mesocycleIndex: null,
                mesocyclePosition: MesocyclePosition.NotApplicable,
                shortExtensionRole: role,
                stageFamily: stageFamily,
                isRecoveryWeek: false,
                isTerminalAlignment: isTerminal,
                profile: profile,
                catalog: catalog));
        }
        return result;
    }

    // ── FullPhase (4-32 GE weeks) ───────────────────────────────────────────

    private static IReadOnlyList<GeWeekDescriptor> SelectFullPhase(
        int geWeeks, LongHorizonReadinessProfile profile, LongHorizonGeStageFamilyCatalogDocument catalog)
    {
        var fullMesocycles = geWeeks / 4;
        var remainder = geWeeks % 4;
        var result = new List<GeWeekDescriptor>(geWeeks);
        var weekIndex = 1;

        for (var mesocycle = 1; mesocycle <= fullMesocycles; mesocycle++)
        {
            var developmentStageFamily = StageFamilyForMesocycle(mesocycle, fullMesocycles);

            foreach (var position in new[] { MesocyclePosition.Development1, MesocyclePosition.Development2, MesocyclePosition.Development3 })
            {
                result.Add(BuildDescriptor(
                    weekIndex: weekIndex++,
                    classification: GeDurationClassification.FullPhase,
                    mesocycleIndex: mesocycle,
                    mesocyclePosition: position,
                    shortExtensionRole: ShortExtensionRole.NotApplicable,
                    stageFamily: developmentStageFamily,
                    isRecoveryWeek: false,
                    isTerminalAlignment: false,
                    profile: profile,
                    catalog: catalog));
            }

            result.Add(BuildDescriptor(
                weekIndex: weekIndex++,
                classification: GeDurationClassification.FullPhase,
                mesocycleIndex: mesocycle,
                mesocyclePosition: MesocyclePosition.RecoveryConsolidation,
                shortExtensionRole: ShortExtensionRole.NotApplicable,
                stageFamily: GeStageFamily.Consolidation,
                isRecoveryWeek: true,
                isTerminalAlignment: false,
                profile: profile,
                catalog: catalog));
        }

        // Terminal remainder (1-3 weeks): complete mesocycles always precede
        // it (Phase 4I.2), and it is Alignment -- never Recovery, never
        // duplicated Entry, never a race-specific peak.
        var remainderPlan = remainder switch
        {
            0 => Array.Empty<(ShortExtensionRole, GeStageFamily)>(),
            1 => new[] { (ShortExtensionRole.PreRunwayAlignment, GeStageFamily.PreRunwayAlignment) },
            2 => new[]
            {
                (ShortExtensionRole.ControlledDevelopment, GeStageFamily.BaseDevelopment),
                (ShortExtensionRole.PreRunwayAlignment, GeStageFamily.PreRunwayAlignment),
            },
            3 => new[]
            {
                (ShortExtensionRole.EntryAlignment, GeStageFamily.Entry),
                (ShortExtensionRole.ControlledDevelopment, GeStageFamily.BaseDevelopment),
                (ShortExtensionRole.PreRunwayAlignment, GeStageFamily.PreRunwayAlignment),
            },
            _ => throw new InvalidOperationException($"Impossible remainder value: {remainder}."),
        };

        foreach (var (role, stageFamily) in remainderPlan)
        {
            result.Add(BuildDescriptor(
                weekIndex: weekIndex++,
                classification: GeDurationClassification.FullPhase,
                mesocycleIndex: null,
                mesocyclePosition: MesocyclePosition.NotApplicable,
                shortExtensionRole: role,
                stageFamily: stageFamily,
                isRecoveryWeek: false,
                isTerminalAlignment: true,
                profile: profile,
                catalog: catalog));
        }

        return result;
    }

    /// <summary>
    /// Deterministic mesocycle-to-stage-family rule (Phase 4I.4 Part 9):
    /// mesocycle 1 is always BaseDevelopment (Entry's orientation role is
    /// absorbed here, per Phase 4I.2's own finding that Entry belongs only
    /// to ShortExtension's vocabulary); the final mesocycle is always
    /// AerobicDurability ("aligns toward Runway"); every other mesocycle
    /// alternates by index parity. This guarantees BaseDevelopment never
    /// repeats more than 1 consecutive mesocycle and AerobicDurability never
    /// repeats more than 2 consecutive mesocycles for any mesocycle count
    /// 1-8 (GE 4-32) -- proven by
    /// <c>LongHorizonGeCatalogSelectorTests.MesocycleSequencing_NeverExceedsRepetitionCaps</c>.
    /// Eight identical mesocycles never occur.
    /// </summary>
    private static GeStageFamily StageFamilyForMesocycle(int mesocycleIndex, int totalMesocycles)
    {
        if (mesocycleIndex == 1) return GeStageFamily.BaseDevelopment;
        if (mesocycleIndex == totalMesocycles) return GeStageFamily.AerobicDurability;
        return mesocycleIndex % 2 == 0 ? GeStageFamily.AerobicDurability : GeStageFamily.BaseDevelopment;
    }

    private static GeWeekDescriptor BuildDescriptor(
        int weekIndex,
        GeDurationClassification classification,
        int? mesocycleIndex,
        MesocyclePosition mesocyclePosition,
        ShortExtensionRole shortExtensionRole,
        GeStageFamily stageFamily,
        bool isRecoveryWeek,
        bool isTerminalAlignment,
        LongHorizonReadinessProfile profile,
        LongHorizonGeStageFamilyCatalogDocument catalog)
    {
        var stageDefinition = catalog.StageFamilies.Single(sf => sf.StageFamilyKey == StageFamilyKey(stageFamily));

        var roles = new Dictionary<GeWeekRole, GeWorkoutReference>
        {
            [GeWeekRole.KeySession] = ResolveWorkout(stageDefinition, "KEY_SESSION", profile),
            [GeWeekRole.EasySupportA] = ResolveWorkout(stageDefinition, "EASY_SUPPORT", profile),
            [GeWeekRole.EasySupportB] = ResolveWorkout(stageDefinition, "EASY_SUPPORT", profile),
            [GeWeekRole.LongRun] = ResolveWorkout(stageDefinition, "LONG_RUN", profile),
        };

        return new GeWeekDescriptor(
            WeekIndex: weekIndex,
            SegmentType: GeWeekDescriptor.LongHorizonGeneralEnduranceSegmentType,
            Classification: classification,
            MesocycleIndex: mesocycleIndex,
            MesocyclePosition: mesocyclePosition,
            ShortExtensionRole: shortExtensionRole,
            StageFamily: stageFamily,
            IsRecoveryWeek: isRecoveryWeek,
            IsTerminalAlignment: isTerminalAlignment,
            ReadinessProfile: profile,
            Roles: roles,
            CatalogSourceId: catalog.Key,
            CatalogSourceVersion: catalog.Version);
    }

    private static GeWorkoutReference ResolveWorkout(GeStageFamilyDefinition stage, string role, LongHorizonReadinessProfile profile)
    {
        var profileToken = profile == LongHorizonReadinessProfile.ConsistencyNeeded ? "CONSISTENCY_NEEDED" : "CORE_ENTRY_READY";
        var assignment = stage.RoleAssignments.FirstOrDefault(a => a.Role == role && a.Profile == profileToken)
            ?? stage.RoleAssignments.FirstOrDefault(a => a.Role == role && a.Profile == "ANY")
            ?? throw new InvalidOperationException(
                $"No role assignment found for stage '{stage.StageFamilyKey}', role '{role}', profile '{profileToken}' (or ANY).");

        var candidate = assignment.WorkoutCandidates[0];
        var family = candidate.Key switch
        {
            "EASY_STANDARD" => "EASY",
            "LONG_RUN_STANDARD" => "LONG_RUN",
            "AEROBIC_STRENGTH_CONTROLLED_INTRO" or "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED" => "QUALITY",
            _ => throw new InvalidOperationException($"Unrecognized workout key '{candidate.Key}' -- family cannot be inferred without a schema/catalog lookup."),
        };
        return new GeWorkoutReference(candidate.Key, candidate.Version, family);
    }

    private static string StageFamilyKey(GeStageFamily stageFamily) => stageFamily switch
    {
        GeStageFamily.Entry => "ENTRY",
        GeStageFamily.BaseDevelopment => "BASE_DEVELOPMENT",
        GeStageFamily.AerobicDurability => "AEROBIC_DURABILITY",
        GeStageFamily.Consolidation => "CONSOLIDATION",
        GeStageFamily.PreRunwayAlignment => "PRE_RUNWAY_ALIGNMENT",
        _ => throw new ArgumentOutOfRangeException(nameof(stageFamily)),
    };
}
