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
/// <summary>
/// Phase 10K-GEN.32 (GEN.31 §3.4 items 1-3) -- centralizes the
/// daysPerWeek-to-GE-cardinality derivation every LongHorizon call site
/// otherwise duplicates as <c>daysPerWeek - 2</c> (the "exactly 1 KEY + 1
/// LONG, remainder EASY" identity every pre-GEN.32 constant-KEY-every-week
/// frequency obeys). That identity does not hold for 2D (GEN.11 §1's
/// alternating Model B week has no week with both a KEY_SESSION and an
/// EASY_SUPPORT slot simultaneously) -- <c>daysPerWeek - 2 == 0</c> for 2D,
/// which both violates <see cref="LongHorizonGeStructuralSelector.Select"/>'s
/// own "at least one EASY_SUPPORT" precondition and is not the correct
/// Pattern-B cardinality (1) regardless. This resolver is the single place
/// that distinguishes the two cases; every pre-GEN.32 caller's own
/// <c>daysPerWeek - 2</c> literal is byte-for-byte reproduced for every
/// daysPerWeek != 2.
/// </summary>
internal static class LongHorizonGeCardinality
{
    public static (int EasySupportCount, bool AlternatingKeyEasy) Resolve(int daysPerWeek) =>
        daysPerWeek == 2
            ? (EasySupportCount: 1, AlternatingKeyEasy: true)
            : (EasySupportCount: daysPerWeek - 2, AlternatingKeyEasy: false);
}

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

    /// <param name="easySupportCount">
    /// Phase 10K-FREQ.6D.14 -- number of EASY_SUPPORT sessions on a week that
    /// carries EASY_SUPPORT at all (2 for every existing 4D caller via the
    /// default parameter, exactly reproducing pre-FREQ.6D.14 output; 3 for
    /// the FREQ.6D.12-approved Intermediate x5D shape; 1 for 2D's own
    /// Pattern-B week, GEN.11 §1). Reuses the same EASY_STANDARD catalog
    /// content already approved for EasySupportA/B -- no new content, no new
    /// WorkoutDefinition.
    /// </param>
    /// <param name="alternatingKeyEasy">
    /// Phase 10K-GEN.32 (GEN.31 §1/§3.4 item 1) -- when true, emits the
    /// Option-A 2D alternating sequence approved by GEN.31 §1: odd
    /// <see cref="LongHorizonGeWeekDescriptor.WeekIndex"/> (GE is always the
    /// plan's first segment, so WeekIndex here already equals
    /// GlobalWeekNumber -- GEN.30 §3.4/§4.2) is Pattern A
    /// (KEY_SESSION + LONG_RUN, easySupportCount for that week forced to 0);
    /// even WeekIndex is Pattern B (EASY_SUPPORT + LONG_RUN,
    /// <paramref name="easySupportCount"/> EASY_SUPPORT sessions, no
    /// KEY_SESSION), mirroring the identical odd/even convention
    /// <c>TenKPreparationRunwayWeekMaterializationPolicyFactory</c>'s own
    /// <c>TwoDayModelBPattern</c> already established for Runway/Core.
    /// Defaults to false, reproducing every pre-GEN.32 (4D/5D/6D
    /// constant-every-week-KEY) caller byte-for-byte -- no existing call
    /// site passes this parameter, so this is a zero-delta addition by
    /// construction.
    /// </param>
    public static IReadOnlyList<LongHorizonGeWeekDescriptor> Select(
        int geWeeks, ReadinessProfile profile, int easySupportCount = 2, bool alternatingKeyEasy = false)
    {
        if (geWeeks is < 1 or > 32)
            throw new ArgumentOutOfRangeException(nameof(geWeeks), geWeeks, "GE week count must be 1..32.");
        if (easySupportCount < 1)
            throw new ArgumentOutOfRangeException(nameof(easySupportCount), easySupportCount, "GE requires at least one EASY_SUPPORT session.");

        return Classify(geWeeks) == GeneralEnduranceDurationClassification.ShortExtension
            ? SelectShortExtension(geWeeks, profile, easySupportCount, alternatingKeyEasy)
            : SelectFullPhase(geWeeks, profile, easySupportCount, alternatingKeyEasy);
    }

    private static IReadOnlyList<LongHorizonGeWeekDescriptor> SelectShortExtension(int geWeeks, ReadinessProfile profile, int easySupportCount, bool alternatingKeyEasy)
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
                profile: profile,
                easySupportCount: easySupportCount,
                alternatingKeyEasy: alternatingKeyEasy));
        }
        return result;
    }

    private static IReadOnlyList<LongHorizonGeWeekDescriptor> SelectFullPhase(int geWeeks, ReadinessProfile profile, int easySupportCount, bool alternatingKeyEasy)
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
                    profile: profile,
                    easySupportCount: easySupportCount,
                    alternatingKeyEasy: alternatingKeyEasy));
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
                profile: profile,
                easySupportCount: easySupportCount,
                alternatingKeyEasy: alternatingKeyEasy));
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
                profile: profile,
                easySupportCount: easySupportCount,
                alternatingKeyEasy: alternatingKeyEasy));
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
        ReadinessProfile profile,
        int easySupportCount,
        bool alternatingKeyEasy = false)
    {
        // Phase 10K-GEN.32 (GEN.31 §1/§3.4 item 1) -- Option A: every GE week
        // is Pattern A (KEY_SESSION + LONG_RUN) or Pattern B (EASY_SUPPORT +
        // LONG_RUN), alternating by the week's own global ordinal. GE is
        // always the plan's first segment (GEN.30 §3.4), so `weekIndex` here
        // already *is* GlobalWeekNumber -- no externally-supplied offset is
        // needed at this layer (contrast Runway, which needs one; see the
        // GE->Runway continuity item). Odd weekIndex = Pattern A (hasKeySession,
        // zero EASY_SUPPORT this week); even weekIndex = Pattern B (zero
        // KEY_SESSION, `easySupportCount` EASY_SUPPORT sessions) -- the same
        // odd/even convention already established for Runway/Core
        // (TenKPreparationRunwayWeekMaterializationPolicyFactory's
        // TwoDayModelBPattern). Recovery weeks are not exempted: GEN.31 §1
        // says "every GE week" alternates, with no stage/recovery carve-out.
        // Defaults to false (hasKeySession=true, easySupportCount unchanged),
        // reproducing every pre-GEN.32 caller byte-for-byte.
        var hasKeySession = !alternatingKeyEasy || weekIndex % 2 == 1;
        var effectiveEasySupportCount = alternatingKeyEasy && hasKeySession ? 0 : easySupportCount;

        var easySupportWorkouts = Enumerable.Range(0, effectiveEasySupportCount)
            .Select(_ => ResolveWorkout(stageFamily, "EASY_SUPPORT", profile))
            .ToList();

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
            KeySessionWorkout: ResolveWorkout(stageFamily, "KEY_SESSION", profile),
            EasySupportWorkouts: easySupportWorkouts,
            LongRunWorkout: ResolveWorkout(stageFamily, "LONG_RUN", profile),
            CatalogSourceId: CatalogSourceId,
            CatalogSourceVersion: CatalogSourceVersion,
            HasKeySession: hasKeySession);
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
