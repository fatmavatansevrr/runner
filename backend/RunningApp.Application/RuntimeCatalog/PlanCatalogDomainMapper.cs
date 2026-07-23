using Microsoft.Extensions.Logging;
using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog;

/// <summary>
/// Backend Integration Phase 2 — consumes a <see cref="PlanCatalogCandidateSummary"/>
/// (Phase 1 output, itself read-only against Process A) and produces a
/// backend-safe analysis of which catalog vocabulary concepts the current
/// domain/DTO model can represent today.
///
/// This mapper is purely analytical: it never touches the database, never
/// creates a <c>TrainingWeek</c>/<c>TrainingDay</c>, and never calls
/// <c>IPlanGenerationEngine</c>. It exists to answer, with evidence, "how
/// will backend represent catalog keys/phases/slot roles/workout
/// keys/progression stages/runtime condition values when generation is
/// wired in Phase 3?" — not to answer it yet.
/// </summary>
public interface IPlanCatalogDomainMapper
{
    PlanCatalogMappingResult Map(PlanCatalogCandidateSummary summary);
}

/// <inheritdoc cref="IPlanCatalogDomainMapper"/>
public sealed class PlanCatalogDomainMapper : IPlanCatalogDomainMapper
{
    private readonly ILogger<PlanCatalogDomainMapper> _logger;

    public PlanCatalogDomainMapper(ILogger<PlanCatalogDomainMapper> logger)
    {
        _logger = logger;
    }

    public PlanCatalogMappingResult Map(PlanCatalogCandidateSummary summary)
    {
        var classifications = new List<CatalogConceptClassification>();

        // ── Candidate identity ──────────────────────────────────────────────
        classifications.Add(new CatalogConceptClassification(
            summary.CandidateKey, BackendRepresentationSupport.RepresentableAsStringOnly,
            "TrainingPlan.TemplateId is a free-form nullable string that could hold this text, but it is " +
            "currently used for SQL PlanTemplate.TemplateId's identity — reusing it for a plan-catalog " +
            "candidate key would overload its meaning. No typed CatalogCandidateKey/CatalogCandidateVersion field exists on any entity."));

        // ── Distance family ──────────────────────────────────────────────────
        var distanceFamilyMatch = TryParseCanonicalFamily(summary.CanonicalDistanceFamily);
        classifications.Add(new CatalogConceptClassification(
            summary.CanonicalDistanceFamily,
            distanceFamilyMatch is not null ? BackendRepresentationSupport.NativeSupported : BackendRepresentationSupport.UnknownFromCodeEvidence,
            distanceFamilyMatch is not null
                ? $"GoalDistance.{distanceFamilyMatch} is an exact existing enum member and is already used end-to-end (TrainingPlan.GoalDistance, GeneratePreviewRequest.GoalDistance)."
                : $"'{summary.CanonicalDistanceFamily}' did not match any GoalDistance member."));

        // ── Level ─────────────────────────────────────────────────────────────
        // Running Background V2: RunningBackground is now the four-tier
        // {Beginner, Intermediate, Advanced, Experienced} onboarding
        // self-description, matching plan-catalog's own four-tier
        // {NEW, INTERMEDIATE, ADVANCED, EXPERIENCED} taxonomy in CARDINALITY,
        // but the two are still distinct typed vocabularies with no
        // implicit 1:1 name equivalence beyond the one explicit, tested
        // mapping V1CatalogPilotIdentityPolicy owns
        // (RunningBackground.Intermediate ↔ catalog "INTERMEDIATE"). Only
        // that one pairing is live; BEGINNER/ADVANCED/EXPERIENCED have no
        // catalog candidate content and remain NotSupported here.
        classifications.Add(new CatalogConceptClassification(
            summary.Level, BackendRepresentationSupport.NotSupported,
            "RunningBackground {Beginner, Intermediate, Advanced, Experienced} is a four-tier onboarding " +
            "self-description; plan-catalog's own four-tier RunningExperience taxonomy is " +
            "{NEW, INTERMEDIATE, ADVANCED, EXPERIENCED}. Only RunningBackground.Intermediate has an explicit, " +
            "tested mapping to catalog level \"INTERMEDIATE\" (see V1CatalogPilotIdentityPolicy) — the other " +
            "three RunningBackground values have no corresponding catalog candidate content and are not mapped."));

        // ── Days per week (structurally representable; the layout IDENTITY is not) ──
        classifications.Add(new CatalogConceptClassification(
            summary.Layout.Key, BackendRepresentationSupport.RequiresNewField,
            $"The resolved runsPerWeek value ({summary.DaysPerWeek}) is representable via the existing " +
            "TrainingPlan.DaysPerWeek int field, but the catalog LAYOUT identity itself (key+version, " +
            $"e.g. {summary.Layout.Key} v{summary.Layout.Version}) has no field to be stored in — only its derived numeric effect is representable today."));

        // ── Slot roles ────────────────────────────────────────────────────────
        var slotRoleClassification = ClassifySlotRoles(summary.SlotRoles, classifications);
        var canRepresentKeySession = slotRoleClassification.TryGetValue("KEY_SESSION", out var keySessionSupport) && keySessionSupport == BackendRepresentationSupport.NativeSupported;
        var canRepresentEasySupport = slotRoleClassification.TryGetValue("EASY_SUPPORT", out var easySupport) && easySupport == BackendRepresentationSupport.NativeSupported;
        var canRepresentLongRun = slotRoleClassification.TryGetValue("LONG_RUN", out var longRunSupport) && longRunSupport == BackendRepresentationSupport.NativeSupported;

        // ── Phase keys ────────────────────────────────────────────────────────
        var phaseClassification = ClassifyPhaseKeys(summary.PhaseKeys, classifications);
        var canRepresentPhaseKeys = phaseClassification.Values.All(v => v == BackendRepresentationSupport.NativeSupported);

        // ── Workout keys (identity) ──────────────────────────────────────────
        ClassifyWorkoutKeys(summary.ReferencedWorkouts, classifications);
        var canRepresentGoalPaceTenK = false; // see ClassifyWorkoutKeys reasoning — no exact catalog-workout-key field exists

        // ── Progression stage keys (not exposed by the Phase 1 summary at all) ──
        classifications.Add(new CatalogConceptClassification(
            "GOAL_PACE_REHEARSAL", BackendRepresentationSupport.RequiresNewField,
            "Workout-progression STAGE keys (e.g. GOAL_PACE_REHEARSAL, CURRENT_FITNESS_SPECIFIC_REHEARSAL) are not even " +
            "exposed by the Phase 1 PlanCatalogCandidateSummary (which stops at the workoutProgression reference, not " +
            "its internal stages) — and no backend field would carry a stage key even if it were exposed. Requires both " +
            "extending the Phase 1 loader and adding a new CatalogStageKey field."));
        classifications.Add(new CatalogConceptClassification(
            "CURRENT_FITNESS_SPECIFIC_REHEARSAL", BackendRepresentationSupport.RequiresNewField,
            "Same reasoning as GOAL_PACE_REHEARSAL — a fallback stage key, not currently exposed or representable."));
        var canRepresentGoalPaceRehearsalStage = false;

        // ── Peak volume band policy / runtime condition registry ────────────
        classifications.Add(new CatalogConceptClassification(
            $"{summary.PeakVolumeBandPolicy.Key} v{summary.PeakVolumeBandPolicy.Version}", BackendRepresentationSupport.RequiresNewTableOrJson,
            "Backend has no peak-weekly-volume-band concept anywhere (no field, no table). Would need either a new " +
            "small reference table or a JSON metadata column to represent policy bands per (distance, level, days-per-week)."));
        classifications.Add(new CatalogConceptClassification(
            $"{summary.RuntimeConditionValueRegistry.Key} v{summary.RuntimeConditionValueRegistry.Version}", BackendRepresentationSupport.RequiresNewTableOrJson,
            "Backend has no runtime-condition-value-registry concept anywhere. Would need a new table or JSON structure " +
            "to represent condition-type -> allowed-value-set pairs, plus resolver code to produce actual values (see runtime condition group classifications)."));
        var canRepresentRuntimeConditionValues = false;

        // ── Runtime condition value groups ──────────────────────────────────
        ClassifyRuntimeConditionGroups(classifications);

        // ── Requested target distance placeholder ───────────────────────────
        classifications.Add(new CatalogConceptClassification(
            "RequestedTargetDistanceKm", BackendRepresentationSupport.RequiresNewField,
            "TrainingPlan.GoalDistanceKm already exists but is populated from a FIXED, family-representative constant " +
            "(PlanServices.GetGoalDistanceInKm: FiveK=5.0, TenK=10.0, HalfMarathon=21.0975, Marathon=42.195 — even " +
            "GoalDistance.Custom falls through to the 5.0 default), never the user's exact requested distance. " +
            "Phase 1's CanonicalDistanceFamilyResolver.RequestedTargetDistanceKm has no field to be persisted into " +
            "without either repurposing GoalDistanceKm (breaking its existing meaning) or adding a new field."));

        var unsupportedOrMissing = classifications
            .Where(c => c.Support is BackendRepresentationSupport.NotSupported
                     or BackendRepresentationSupport.RequiresNewField
                     or BackendRepresentationSupport.RequiresEnumExtension
                     or BackendRepresentationSupport.RequiresNewTableOrJson)
            .Select(c => $"{c.Concept} ({c.Support})")
            .ToList();

        _logger.LogInformation(
            "PlanCatalogDomainMapper: mapped {CandidateKey} v{CandidateVersion} — {ClassifiedCount} concepts classified, " +
            "{UnsupportedCount} not yet natively representable.",
            summary.CandidateKey, summary.CandidateVersion, classifications.Count, unsupportedOrMissing.Count);

        return new PlanCatalogMappingResult
        {
            CandidateKey = summary.CandidateKey,
            CandidateVersion = summary.CandidateVersion,
            CanonicalDistanceFamily = distanceFamilyMatch ?? GoalDistance.Custom,
            RequestedTargetDistanceKmPlaceholderSupported = false,
            SlotRoles = summary.SlotRoles,
            PhaseKeys = summary.PhaseKeys,
            ReferencedWorkouts = summary.ReferencedWorkouts,
            CanRepresentGoalPaceTenK = canRepresentGoalPaceTenK,
            CanRepresentGoalPaceRehearsalStage = canRepresentGoalPaceRehearsalStage,
            CanRepresentKeySession = canRepresentKeySession,
            CanRepresentEasySupport = canRepresentEasySupport,
            CanRepresentLongRun = canRepresentLongRun,
            CanRepresentPhaseKeys = canRepresentPhaseKeys,
            CanRepresentRuntimeConditionValues = canRepresentRuntimeConditionValues,
            Classifications = classifications,
            KnownUnsupportedOrMissingRepresentations = unsupportedOrMissing,
        };
    }

    private static GoalDistance? TryParseCanonicalFamily(string canonicalDistanceFamily) => canonicalDistanceFamily switch
    {
        "FIVE_K" => GoalDistance.FiveK,
        "TEN_K" => GoalDistance.TenK,
        "HALF_MARATHON" => GoalDistance.HalfMarathon,
        "MARATHON" => GoalDistance.Marathon,
        _ => null,
    };

    private static Dictionary<string, BackendRepresentationSupport> ClassifySlotRoles(
        IReadOnlyList<string> slotRoles, List<CatalogConceptClassification> classifications)
    {
        var result = new Dictionary<string, BackendRepresentationSupport>();

        foreach (var role in slotRoles.Distinct())
        {
            var (support, reasoning) = role switch
            {
                "KEY_SESSION" => (BackendRepresentationSupport.NotSupported,
                    "No SlotRole concept exists in backend at all. TrainingDayType {Easy, Interval, Tempo, LongRun, Rest, " +
                    "RecoveryEasy} is a workout-TYPE enum, not a slot-ROLE enum — there is no 'this day is the week's single " +
                    "key/hard session' designation independent of which specific workout type fills it."),
                "EASY_SUPPORT" => (BackendRepresentationSupport.NotSupported,
                    "Same reasoning as KEY_SESSION — TrainingDayType.Easy/RecoveryEasy describe workout type, not that a " +
                    "day is specifically an 'easy support' slot in a structured weekly layout."),
                "LONG_RUN" => (BackendRepresentationSupport.NativeSupported,
                    "TrainingDayType.LongRun exists and is already actively used (all 3 seeded templates use dayType='long_run' " +
                    "for exactly this concept) — coincidentally exact, though it is still a workout-TYPE value being reused for a " +
                    "slot-role concept, not a dedicated SlotRole field."),
                _ => (BackendRepresentationSupport.UnknownFromCodeEvidence, $"'{role}' is not one of the known slot roles this mapper classifies."),
            };

            result[role] = support;
            classifications.Add(new CatalogConceptClassification(role, support, reasoning));
        }

        return result;
    }

    private static Dictionary<string, BackendRepresentationSupport> ClassifyPhaseKeys(
        IReadOnlyList<string> phaseKeys, List<CatalogConceptClassification> classifications)
    {
        var result = new Dictionary<string, BackendRepresentationSupport>();
        var knownWeekTypeNames = Enum.GetNames<TrainingWeekType>().Select(n => n.ToUpperInvariant()).ToHashSet();

        foreach (var phase in phaseKeys.Distinct())
        {
            var (support, reasoning) = phase switch
            {
                "FOUNDATION" => (BackendRepresentationSupport.RequiresEnumExtension,
                    "TrainingWeekType {Base, Build, Recovery, Peak, Taper, RaceWeek} is the right category of enum, but has no " +
                    "exact FOUNDATION member. 'Base' is a plausible loose synonym but not an exact, code-evident mapping."),
                "BUILD" => (BackendRepresentationSupport.NativeSupported,
                    "TrainingWeekType.Build is an exact existing enum member, already used in all 3 seeded templates (weekType='build')."),
                "RACE_SPECIFIC" => (BackendRepresentationSupport.RequiresEnumExtension,
                    "TrainingWeekType has no exact RACE_SPECIFIC member. 'Peak' or 'RaceWeek' are both plausible but ambiguous " +
                    "loose candidates — no code evidence resolves which one (or whether a new value is needed)."),
                "TAPER" => (BackendRepresentationSupport.NativeSupported,
                    "TrainingWeekType.Taper is an exact existing enum member."),
                _ => (BackendRepresentationSupport.UnknownFromCodeEvidence, $"'{phase}' is not one of the known phase keys this mapper classifies."),
            };

            result[phase] = support;
            classifications.Add(new CatalogConceptClassification(phase, support, reasoning));
        }

        // Record, for transparency, which TrainingWeekType members exist regardless of phase-key matching above.
        classifications.Add(new CatalogConceptClassification(
            "TrainingWeekType (reference)", BackendRepresentationSupport.NativeSupported,
            $"Existing backend enum members: {string.Join(", ", knownWeekTypeNames)}."));

        return result;
    }

    private static void ClassifyWorkoutKeys(IReadOnlyList<PlanCatalogReference> referencedWorkouts, List<CatalogConceptClassification> classifications)
    {
        foreach (var workout in referencedWorkouts)
        {
            var (support, reasoning) = workout.Key switch
            {
                "GOAL_PACE_TEN_K" => (BackendRepresentationSupport.RequiresNewField,
                    "No field anywhere stores the exact catalog WORKOUT KEY that produced a training day. TrainingDayType " +
                    "has no goal-pace-specific case (closest lossy fits: Tempo or Interval); even if it did, DayType alone " +
                    "cannot preserve the specific catalog identity 'GOAL_PACE_TEN_K v2' vs. a hand-authored SQL day of the " +
                    "same type. Requires a new CatalogWorkoutKey (+ version) field on TrainingDay."),
                "EASY_STANDARD" => (BackendRepresentationSupport.RequiresNewField,
                    "TrainingDayType.Easy loosely matches this workout's FAMILY, but the exact catalog workout key/version " +
                    "identity is not preserved by any existing field."),
                "LONG_RUN_STANDARD" => (BackendRepresentationSupport.RequiresNewField,
                    "TrainingDayType.LongRun loosely matches this workout's FAMILY, but the exact catalog workout key/version " +
                    "identity is not preserved by any existing field."),
                "FARTLEK" => (BackendRepresentationSupport.RequiresNewField,
                    "TrainingDayType.Interval is a plausible loose family match, but the exact catalog workout key is not preserved."),
                "THRESHOLD_TEMPO" => (BackendRepresentationSupport.RequiresNewField,
                    "TrainingDayType.Tempo is a plausible loose family match, but the exact catalog workout key is not preserved."),
                _ => (BackendRepresentationSupport.UnknownFromCodeEvidence, $"'{workout.Key}' is not one of the known workout keys this mapper classifies."),
            };

            classifications.Add(new CatalogConceptClassification($"{workout.Key} v{workout.Version}", support, reasoning));
        }
    }

    private static void ClassifyRuntimeConditionGroups(List<CatalogConceptClassification> classifications)
    {
        // Evidence: repository-wide search (backend Process B activation review, reconfirmed here) finds
        // zero references to PACE_SOURCE_IN/TIME_ADEQUACY_IN/CORE_ENTRY_READINESS_IN/GOAL_FEASIBILITY_IN,
        // or any of their allowed values, anywhere in backend source outside the Phase 1 loader's own
        // reference-field names (which carry only the registry's key/version, never its allowed values).
        //
        // TODO (Backend Integration Phase 4+, explicitly out of scope for Phase 3): a later generation
        // phase must implement real resolvers for all four condition groups before any
        // GOAL_FEASIBILITY_IN-gated stage (e.g. GOAL_PACE_REHEARSAL) can be safely evaluated at runtime:
        //   - GOAL_FEASIBILITY_IN resolver (REALISTIC/CHALLENGING/UNSUPPORTED/NOT_REQUESTED)
        //   - PACE_SOURCE_IN resolver (NONE/RECENT_RACE/ESTIMATED/TARGET_TIME)
        //   - TIME_ADEQUACY_IN resolver (ADEQUATE/COMPRESSED/INSUFFICIENT)
        //   - CORE_ENTRY_READINESS_IN resolver (READY/CAUTION/NOT_READY)
        // Do NOT hardcode/fake any of these values to make a future test pass — an unresolved condition
        // must fail loudly (mirroring the PLAN_TEMPLATE_NOT_FOUND / no-silent-fallback precedent set in
        // Backend Phase 0), not silently default to an arbitrary allowed value.
        classifications.Add(new CatalogConceptClassification(
            "PACE_SOURCE_IN [NONE, RECENT_RACE, ESTIMATED, TARGET_TIME]", BackendRepresentationSupport.NotSupported,
            "Zero references anywhere in backend source. No field, no enum, no resolver. Not even declared."));
        classifications.Add(new CatalogConceptClassification(
            "TIME_ADEQUACY_IN [ADEQUATE, COMPRESSED, INSUFFICIENT]", BackendRepresentationSupport.NotSupported,
            "Zero references anywhere in backend source. No field, no enum, no resolver. Not even declared."));
        classifications.Add(new CatalogConceptClassification(
            "CORE_ENTRY_READINESS_IN [READY, CAUTION, NOT_READY]", BackendRepresentationSupport.NotSupported,
            "Zero references anywhere in backend source. No field, no enum, no resolver. Not even declared. " +
            "(Golden Fixture v3's DecisionTrace shows readiness='STANDARD' for an unrelated Process-A-fixture-internal " +
            "resolver step — not backend code, and not part of this registry's vocabulary either; see " +
            "plan-catalog/artifacts/audits/domain-d3-followup.md.)"));
        classifications.Add(new CatalogConceptClassification(
            "GOAL_FEASIBILITY_IN [REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED]", BackendRepresentationSupport.NotSupported,
            "Zero references anywhere in backend source, even though this is the one condition type plan-catalog's own " +
            "workout-progression stage.Requires actually gates (GOAL_PACE_REHEARSAL). No field, no enum, no resolver."));
    }
}
