namespace RunningApp.Application.RuntimeCatalog.Schedule.LongHorizon;

/// <summary>
/// Phase 4I.5 — backend-internal structural mirror of plan-catalog's
/// deterministic <c>LongHorizonGeCatalogSelector</c>
/// (<c>PlanCatalog.Core.LongHorizon</c>, Phase 4I.4). Reimplements the exact
/// same mesocycle/remainder/ShortExtension placement rule (deliberately NOT
/// 32 hand-authored rows) and mirrors the role-assignment content of
/// <c>plan-catalog/catalog/long-horizon-progressions/ten-k-long-horizon-ge-stage-families.v1.json</c>
/// (a dark, deliberately unregistered catalog document -- not one of
/// <c>FileSystemCatalogSourceRepository</c>'s 10 scanned subfolders) as a
/// hand-verified, hardcoded backend constant table, matching the established
/// "independent mirror, never a cross-project reference" precedent
/// (see <c>LongHorizonGeStructuralContracts.cs</c>'s own doc comment,
/// <c>TD-BACKEND-001</c>). Performs no date, volume, pace, or calendar
/// computation.
/// </summary>
internal static class LongHorizonGeStructuralSelector
{
    public const string CatalogSourceId = "TEN_K_LONG_HORIZON_GE_STAGE_FAMILIES";
    public const int CatalogSourceVersion = 1;

    private sealed record RoleAssignment(string Role, string Profile, string WorkoutKey, int WorkoutVersion, string Family);

    /// <summary>
    /// Verbatim structural mirror of the 5 stage families' <c>roleAssignments</c>
    /// declared in <c>ten-k-long-horizon-ge-stage-families.v1.json</c> (Phase 4I.4).
    /// </summary>
    private static readonly IReadOnlyDictionary<LongHorizonGeStageFamily, IReadOnlyList<RoleAssignment>> StageFamilyRoleAssignments =
        new Dictionary<LongHorizonGeStageFamily, IReadOnlyList<RoleAssignment>>
        {
            [LongHorizonGeStageFamily.Entry] = new[]
            {
                new RoleAssignment("KEY_SESSION", "CONSISTENCY_NEEDED", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("KEY_SESSION", "CORE_ENTRY_READY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("EASY_SUPPORT", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("LONG_RUN", "ANY", "LONG_RUN_STANDARD", 6, "LONG_RUN"),
            },
            [LongHorizonGeStageFamily.BaseDevelopment] = new[]
            {
                new RoleAssignment("KEY_SESSION", "CONSISTENCY_NEEDED", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("KEY_SESSION", "CORE_ENTRY_READY", "AEROBIC_STRENGTH_CONTROLLED_INTRO", 2, "QUALITY"),
                new RoleAssignment("EASY_SUPPORT", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("LONG_RUN", "ANY", "LONG_RUN_STANDARD", 6, "LONG_RUN"),
            },
            [LongHorizonGeStageFamily.AerobicDurability] = new[]
            {
                new RoleAssignment("KEY_SESSION", "CONSISTENCY_NEEDED", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("KEY_SESSION", "CORE_ENTRY_READY", "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED", 2, "QUALITY"),
                new RoleAssignment("EASY_SUPPORT", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("LONG_RUN", "ANY", "LONG_RUN_STANDARD", 6, "LONG_RUN"),
            },
            [LongHorizonGeStageFamily.Consolidation] = new[]
            {
                new RoleAssignment("KEY_SESSION", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("EASY_SUPPORT", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("LONG_RUN", "ANY", "LONG_RUN_STANDARD", 6, "LONG_RUN"),
            },
            [LongHorizonGeStageFamily.PreRunwayAlignment] = new[]
            {
                new RoleAssignment("KEY_SESSION", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("EASY_SUPPORT", "ANY", "EASY_STANDARD", 6, "EASY"),
                new RoleAssignment("LONG_RUN", "ANY", "LONG_RUN_STANDARD", 6, "LONG_RUN"),
            },
        };

    public static GeneralEnduranceDurationClassification Classify(int geWeeks) => geWeeks switch
    {
        >= 1 and <= 3 => GeneralEnduranceDurationClassification.ShortExtension,
        >= 4 and <= 32 => GeneralEnduranceDurationClassification.FullPhase,
        _ => throw new ArgumentOutOfRangeException(nameof(geWeeks), geWeeks, "GE week count must be 1..32."),
    };

    public static IReadOnlyList<LongHorizonGeWeekDescriptor> Select(int geWeeks, ReadinessProfile profile)
    {
        if (geWeeks is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(geWeeks), geWeeks, "GE week count must be 1..32.");

        return Classify(geWeeks) == GeneralEnduranceDurationClassification.ShortExtension
            ? SelectShortExtension(geWeeks, profile)
            : SelectFullPhase(geWeeks, profile);
    }

    private static IReadOnlyList<LongHorizonGeWeekDescriptor> SelectShortExtension(int geWeeks, ReadinessProfile profile)
    {
        var plan = geWeeks switch
        {
            1 => new[] { (LongHorizonGeShortExtensionRole.EntryAlignment, LongHorizonGeStageFamily.Entry) },
            2 => new[]
            {
                (LongHorizonGeShortExtensionRole.EntryAlignment, LongHorizonGeStageFamily.Entry),
                (LongHorizonGeShortExtensionRole.PreRunwayAlignment, LongHorizonGeStageFamily.PreRunwayAlignment),
            },
            3 => new[]
            {
                (LongHorizonGeShortExtensionRole.EntryAlignment, LongHorizonGeStageFamily.Entry),
                (LongHorizonGeShortExtensionRole.ControlledDevelopment, LongHorizonGeStageFamily.BaseDevelopment),
                (LongHorizonGeShortExtensionRole.PreRunwayAlignment, LongHorizonGeStageFamily.PreRunwayAlignment),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(geWeeks)),
        };

        var result = new List<LongHorizonGeWeekDescriptor>(plan.Length);
        for (var i = 0; i < plan.Length; i++)
        {
            var (role, stageFamily) = plan[i];
            var isTerminal = i == plan.Length - 1;
            result.Add(BuildDescriptor(
                weekIndex: i + 1,
                classification: GeneralEnduranceDurationClassification.ShortExtension,
                mesocycleIndex: null,
                mesocyclePosition: LongHorizonGeMesocyclePosition.NotApplicable,
                shortExtensionRole: role,
                stageFamily: stageFamily,
                isRecoveryWeek: false,
                isTerminalAlignment: isTerminal,
                profile: profile));
        }
        return result;
    }

    private static IReadOnlyList<LongHorizonGeWeekDescriptor> SelectFullPhase(int geWeeks, ReadinessProfile profile)
    {
        var fullMesocycles = geWeeks / 4;
        var remainder = geWeeks % 4;
        var result = new List<LongHorizonGeWeekDescriptor>(geWeeks);
        var weekIndex = 1;

        for (var mesocycle = 1; mesocycle <= fullMesocycles; mesocycle++)
        {
            var developmentStageFamily = StageFamilyForMesocycle(mesocycle, fullMesocycles);

            foreach (var position in new[]
            {
                LongHorizonGeMesocyclePosition.Development1,
                LongHorizonGeMesocyclePosition.Development2,
                LongHorizonGeMesocyclePosition.Development3,
            })
            {
                result.Add(BuildDescriptor(
                    weekIndex: weekIndex++,
                    classification: GeneralEnduranceDurationClassification.FullPhase,
                    mesocycleIndex: mesocycle,
                    mesocyclePosition: position,
                    shortExtensionRole: LongHorizonGeShortExtensionRole.NotApplicable,
                    stageFamily: developmentStageFamily,
                    isRecoveryWeek: false,
                    isTerminalAlignment: false,
                    profile: profile));
            }

            result.Add(BuildDescriptor(
                weekIndex: weekIndex++,
                classification: GeneralEnduranceDurationClassification.FullPhase,
                mesocycleIndex: mesocycle,
                mesocyclePosition: LongHorizonGeMesocyclePosition.RecoveryConsolidation,
                shortExtensionRole: LongHorizonGeShortExtensionRole.NotApplicable,
                stageFamily: LongHorizonGeStageFamily.Consolidation,
                isRecoveryWeek: true,
                isTerminalAlignment: false,
                profile: profile));
        }

        var remainderPlan = remainder switch
        {
            0 => Array.Empty<(LongHorizonGeShortExtensionRole, LongHorizonGeStageFamily)>(),
            1 => new[] { (LongHorizonGeShortExtensionRole.PreRunwayAlignment, LongHorizonGeStageFamily.PreRunwayAlignment) },
            2 => new[]
            {
                (LongHorizonGeShortExtensionRole.ControlledDevelopment, LongHorizonGeStageFamily.BaseDevelopment),
                (LongHorizonGeShortExtensionRole.PreRunwayAlignment, LongHorizonGeStageFamily.PreRunwayAlignment),
            },
            3 => new[]
            {
                (LongHorizonGeShortExtensionRole.EntryAlignment, LongHorizonGeStageFamily.Entry),
                (LongHorizonGeShortExtensionRole.ControlledDevelopment, LongHorizonGeStageFamily.BaseDevelopment),
                (LongHorizonGeShortExtensionRole.PreRunwayAlignment, LongHorizonGeStageFamily.PreRunwayAlignment),
            },
            _ => throw new InvalidOperationException($"Impossible remainder value: {remainder}."),
        };

        foreach (var (role, stageFamily) in remainderPlan)
        {
            result.Add(BuildDescriptor(
                weekIndex: weekIndex++,
                classification: GeneralEnduranceDurationClassification.FullPhase,
                mesocycleIndex: null,
                mesocyclePosition: LongHorizonGeMesocyclePosition.NotApplicable,
                shortExtensionRole: role,
                stageFamily: stageFamily,
                isRecoveryWeek: false,
                isTerminalAlignment: true,
                profile: profile));
        }

        return result;
    }

    private static LongHorizonGeStageFamily StageFamilyForMesocycle(int mesocycleIndex, int totalMesocycles)
    {
        if (mesocycleIndex == 1) return LongHorizonGeStageFamily.BaseDevelopment;
        if (mesocycleIndex == totalMesocycles) return LongHorizonGeStageFamily.AerobicDurability;
        return mesocycleIndex % 2 == 0 ? LongHorizonGeStageFamily.AerobicDurability : LongHorizonGeStageFamily.BaseDevelopment;
    }

    private static LongHorizonGeWeekDescriptor BuildDescriptor(
        int weekIndex,
        GeneralEnduranceDurationClassification classification,
        int? mesocycleIndex,
        LongHorizonGeMesocyclePosition mesocyclePosition,
        LongHorizonGeShortExtensionRole shortExtensionRole,
        LongHorizonGeStageFamily stageFamily,
        bool isRecoveryWeek,
        bool isTerminalAlignment,
        ReadinessProfile profile)
    {
        var roles = new Dictionary<LongHorizonGeWeekRole, LongHorizonGeWorkoutReference>
        {
            [LongHorizonGeWeekRole.KeySession] = ResolveWorkout(stageFamily, "KEY_SESSION", profile),
            [LongHorizonGeWeekRole.EasySupportA] = ResolveWorkout(stageFamily, "EASY_SUPPORT", profile),
            [LongHorizonGeWeekRole.EasySupportB] = ResolveWorkout(stageFamily, "EASY_SUPPORT", profile),
            [LongHorizonGeWeekRole.LongRun] = ResolveWorkout(stageFamily, "LONG_RUN", profile),
        };

        return new LongHorizonGeWeekDescriptor(
            WeekIndex: weekIndex,
            Classification: classification,
            MesocycleIndex: mesocycleIndex,
            MesocyclePosition: mesocyclePosition,
            ShortExtensionRole: shortExtensionRole,
            StageFamily: stageFamily,
            IsRecoveryWeek: isRecoveryWeek,
            IsTerminalAlignment: isTerminalAlignment,
            ReadinessProfile: profile,
            Roles: roles,
            CatalogSourceId: CatalogSourceId,
            CatalogSourceVersion: CatalogSourceVersion);
    }

    private static LongHorizonGeWorkoutReference ResolveWorkout(LongHorizonGeStageFamily stageFamily, string role, ReadinessProfile profile)
    {
        var profileToken = profile == ReadinessProfile.ConsistencyNeeded ? "CONSISTENCY_NEEDED" : "CORE_ENTRY_READY";
        var assignments = StageFamilyRoleAssignments[stageFamily];
        var assignment = assignments.FirstOrDefault(a => a.Role == role && a.Profile == profileToken)
            ?? assignments.FirstOrDefault(a => a.Role == role && a.Profile == "ANY")
            ?? throw new InvalidOperationException(
                $"No role assignment found for stage '{stageFamily}', role '{role}', profile '{profileToken}' (or ANY).");
        return new LongHorizonGeWorkoutReference(assignment.WorkoutKey, assignment.WorkoutVersion, assignment.Family);
    }
}
