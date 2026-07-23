using PlanCatalog.Contracts;

namespace PlanCatalog.Core.Audit;

/// <summary>
/// Hand-authored, field-level audit of every domain-content decision in the TEN_K / 4D / INTERMEDIATE
/// pilot catalog. This is the single source of truth consulted by both the audit-report generator and
/// the publish guard (<see cref="Validation.PublishReadinessValidator"/>) — it is not derived from
/// passing tests, which prove structural validity, not domain-content provenance.
///
/// Reconciled against Golden Fixture v3 (docs/canonical/golden-fixture-v3/) per the source-governance
/// hierarchy in docs/README.md. The fixture references TEN_K_MASTER v2 / APPSEL_RACE_PLAN_V1 v3, while
/// this catalog only has v1 of each — see ARTIFACT_VERSION_PARITY_UNRESOLVED notes below. Per explicit
/// instruction, no catalog artifact was upgraded/cloned/renamed to force version parity; field-level
/// fixture semantics that do not depend on the specific artifact version are still usable
/// (SOURCE_SEMANTICS_USABLE).
/// </summary>
public static class PilotDomainContentAudit
{
    private const string Combination = "TEN_K__4D__INTERMEDIATE";

    private const string FixtureSource = "docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json";
    private const string FixtureTraceSource = "docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json";
    private const string ProgressionRulesSource = "docs/canonical/golden-fixture-v3/progression_rules_v2.yaml";
    private const string BriefSource = "docs/specifications/plan-catalog-antigravity-brief-v2.md";

    private const string VersionParityCaveat =
        " [ARTIFACT_VERSION_PARITY_UNRESOLVED: fixture references TEN_K_MASTER v2 / APPSEL_RACE_PLAN_V1 v3; " +
        "this field-level fact is version-independent (SOURCE_SEMANTICS_USABLE) and is cited for the current v1 artifact without upgrading it.]";

    public static IReadOnlyList<DomainContentDecision> Entries { get; } = Build();

    /// <summary>True if any entry for the given (documentType, key, version) is a blocking placeholder.</summary>
    public static bool HasBlockingUnconfirmedContent(string documentType, string key, int version) =>
        Entries.Any(e => e.DocumentType == documentType && e.Key == key && e.Version == version && e.IsBlocking);

    public static IReadOnlyList<DomainContentDecision> BlockingEntriesFor(string documentType, string key, int version) =>
        Entries.Where(e => e.DocumentType == documentType && e.Key == key && e.Version == version && e.IsBlocking).ToList();

    private static DomainContentDecision Placeholder(
        string id, string group, string documentType, string key, int version, string jsonPath, string currentValue,
        string sourceFile, string reason, IReadOnlyList<string> validators) => new()
    {
        EntryId = id,
        Group = group,
        DocumentType = documentType,
        Key = key,
        Version = version,
        JsonPath = jsonPath,
        CurrentValue = currentValue,
        Classification = ContentDecisionStatus.PlaceholderUnconfirmed,
        SourceFile = sourceFile,
        SourceSectionOrReason = reason,
        IsBlocking = true,
        RequiredDecision = "Product/coaching decision required; replace with a traceable canonical source before production publish.",
        AffectedValidators = validators,
        AffectedBundlesOrReleases = [Combination],
        ProductionPublishAllowed = false
    };

    private static DomainContentDecision Confirmed(
        string id, string group, string documentType, string key, int version, string jsonPath, string currentValue,
        string sourceFile, string reason, IReadOnlyList<string> validators) => new()
    {
        EntryId = id,
        Group = group,
        DocumentType = documentType,
        Key = key,
        Version = version,
        JsonPath = jsonPath,
        CurrentValue = currentValue,
        Classification = ContentDecisionStatus.CanonicalConfirmed,
        SourceFile = sourceFile,
        SourceSectionOrReason = reason,
        IsBlocking = false,
        RequiredDecision = null,
        AffectedValidators = validators,
        AffectedBundlesOrReleases = [Combination],
        ProductionPublishAllowed = true
    };

    private static DomainContentDecision ExplicitDefault(
        string id, string group, string documentType, string key, int version, string jsonPath, string currentValue,
        string sourceFile, string reason, IReadOnlyList<string> validators) => new()
    {
        EntryId = id,
        Group = group,
        DocumentType = documentType,
        Key = key,
        Version = version,
        JsonPath = jsonPath,
        CurrentValue = currentValue,
        Classification = ContentDecisionStatus.ExplicitProductDefault,
        SourceFile = sourceFile,
        SourceSectionOrReason = reason,
        IsBlocking = false,
        RequiredDecision = null,
        AffectedValidators = validators,
        AffectedBundlesOrReleases = [Combination],
        ProductionPublishAllowed = true
    };

    private static DomainContentDecision Technical(
        string id, string group, string documentType, string key, int version, string jsonPath, string currentValue,
        string sourceFile, IReadOnlyList<string> validators, string? reason = null) => new()
    {
        EntryId = id,
        Group = group,
        DocumentType = documentType,
        Key = key,
        Version = version,
        JsonPath = jsonPath,
        CurrentValue = currentValue,
        Classification = ContentDecisionStatus.TechnicalOnly,
        SourceFile = sourceFile,
        SourceSectionOrReason = reason ?? "Structural/mechanical field — not a domain-content decision.",
        IsBlocking = false,
        RequiredDecision = null,
        AffectedValidators = validators,
        AffectedBundlesOrReleases = [Combination],
        ProductionPublishAllowed = true
    };

    private static List<DomainContentDecision> Build()
    {
        var entries = new List<DomainContentDecision>();
        const string masterFileV1 = "catalog/templates/ten-k-master.v1.json";
        const string masterFile = "catalog/templates/ten-k-master.v2.json";
        const string progressionFile = "catalog/workout-progressions/ten-k-workout-progression.v1.json";
        const string layoutFile = "catalog/layouts/run-layout-4d.v1.json";
        const string levelModifierFile = "catalog/level-modifiers/intermediate-modifier.v1.json";
        const string progressionModifierFile = "catalog/progression-modifiers/intermediate-progression-modifier.v1.json";
        const string registryFile = "catalog/registries/runtime-condition-values.v1.json";
        const string peakFile = "catalog/policies/peak-volume-bands.v1.json";
        const string rulePackFile = "catalog/rule-packs/appsel-race-plan.v1.json";
        const string combinationFile = "catalog/combinations/ten-k-4d-intermediate.v1.json";

        // ===================== phase-metadata (TEN_K_MASTER) =====================
        entries.Add(Confirmed("AUD-001", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.coreCycle", "min=8, default=12, max=14",
            BriefSource, "brief §20: '10K pilot data — minimum core: 8 weeks, default core: 12 weeks, maximum core: 14 weeks'. Corroborated by Golden Fixture v3 $.horizon (availableWeeks=12, coreWeeks=12)." + VersionParityCaveat,
            ["PlanTemplateValidator"]));
        entries.Add(Confirmed("AUD-002", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.supportedRunsPerWeek", "[3,4,5]",
            BriefSource, "brief §20: 'supported runs per week: 3, 4, 5'. Corroborated by Golden Fixture v3 profileSnapshot.runsPerWeek=4 (one of the three).",
            ["PlanTemplateValidator", "TemplateCombinationValidator"]));
        entries.Add(Confirmed("AUD-003", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].preferredWeeks", "[3,4,4,1]",
            BriefSource, "brief §20: 'Foundation preferred: 3, Build preferred: 4, Race Specific preferred: 4, Taper preferred: 1'. " +
            "Independently corroborated by Golden Fixture v3 $.phaseAllocation: FOUNDATION weeks=[1,2,3] (3), BUILD weeks=[4..7] (4), RACE_SPECIFIC weeks=[8..11] (4), TAPER weeks=[12] (1) — exact match." + VersionParityCaveat,
            ["PlanTemplateValidator"]));
        entries.Add(Placeholder("AUD-004", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].minimumWeeks", "[2,3,2,1]",
            masterFile, "Only the sum (8) was mandated by the brief; the per-phase split was authored without a canonical source. " +
            "Golden Fixture v3 resolves a single concrete 12-week plan and does not expose per-phase minimum/maximum bounds (only the one realized allocation), so it cannot confirm or deny this field.",
            ["PlanTemplateValidator"]));
        entries.Add(Placeholder("AUD-005", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].maximumWeeks", "[4,5,4,1]",
            masterFile, "Only the sum (14) was mandated by the brief; the per-phase split was authored without a canonical source. Golden Fixture v3 does not expose phase bounds (see AUD-004).",
            ["PlanTemplateValidator"]));
        entries.Add(Placeholder("AUD-006", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].intents", "AEROBIC_BASE, VOLUME_BUILD, RACE_SPECIFIC_SHARPENING, TAPER",
            masterFile, "PhaseIntent vocabulary and per-phase assignment invented; not present in the brief. Golden Fixture v3 and progression_rules_v2.yaml contain no 'intent' vocabulary of any kind — searched both files for AEROBIC_BASE/VOLUME_BUILD/RACE_SPECIFIC_SHARPENING/intent; zero matches. Remains unconfirmed.",
            ["PlanTemplateValidator"]));
        entries.Add(Confirmed("AUD-007", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].eligibleWorkoutFamilies",
            "FOUNDATION:[EASY,LONG_RUN]; BUILD/RACE_SPECIFIC:[EASY,LONG_RUN,QUALITY]; TAPER:[EASY,LONG_RUN,QUALITY,RACE]",
            masterFile,
            "RESOLVED (formerly recorded as an unresolved conflict): TEN_K_MASTER TAPER eligibleWorkoutFamilies omitted QUALITY and RACE " +
            "even though approved Golden Fixture v3 Week 12 contains both a QUALITY activation workout and a RACE workout in the TAPER phase. " +
            "The master definition was corrected to allow EASY, LONG_RUN, QUALITY, and RACE. " +
            $"Source trace: {FixtureSource} $.weeks[11].days[0].workout (workoutKey=RACE_PACE_REPEATS, family=QUALITY, week phaseKey=TAPER) and " +
            "$.weeks[11].days[3].workout (workoutKey=RACE_DAY, family=RACE, week phaseKey=TAPER); corrected artifact catalog/templates/ten-k-master.v2.json $.phases[3].eligibleWorkoutFamilies. " +
            "FOUNDATION/BUILD/RACE_SPECIFIC family lists remain corroborated by Golden Fixture v3 per-phase workout family usage (FOUNDATION uses EASY+LONG_RUN; BUILD and RACE_SPECIFIC use EASY+LONG_RUN+QUALITY — matches exactly). " +
            "Rationale: TAPER phase eligibility must permit workout families used by the approved Golden Fixture v3. " +
            $"Correction was published as TEN_K_MASTER v2 ({masterFile}) because v1 ({masterFileV1}, contentHash c6cb0c0b…) is already PUBLISHED and immutable across three prior releases (1.0.0, 0.1.0-pilot, 0.2.0-pilot); v1 was left untouched. " +
            "Combination v1 preserved; new v2 created referencing TEN_K_MASTER v2 (catalog/combinations/ten-k-4d-intermediate.v1.json unchanged, referencing TEN_K_MASTER v1; catalog/combinations/ten-k-4d-intermediate.v2.json new, referencing TEN_K_MASTER v2) — see artifacts/audits/combination-immutability-investigation.md for the immutability defect this superseded and its correction." + VersionParityCaveat,
            ["PlanTemplateValidator", "WorkoutProgressionValidator"]));
        entries.Add(Placeholder("AUD-008", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].compressionPriority / extensionPriority", "1,2,3,4 (each)",
            masterFile, "Ordering priorities invented; no canonical source for relative compression/extension priority. Golden Fixture v3 resolves one plan without needing to compress/extend phases (12 available weeks == 12 core weeks, runwayWeeks=0), so it exercises no compression/extension logic at all and cannot confirm these priorities.",
            ["PlanTemplateValidator"]));
        entries.Add(Placeholder("AUD-009", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.phases[*].isCompressionProtected", "false,false,false,true",
            masterFile, "Plausible (Taper protected) but not explicitly mandated by the brief. Not exercised by Golden Fixture v3 (see AUD-008).",
            ["PlanTemplateValidator"]));

        // ===================== workout-progression (TEN_K_WORKOUT_PROGRESSION_V1) =====================
        entries.Add(Confirmed("AUD-010", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 1,
            "$.phaseProgressions[RACE_SPECIFIC].stages[TEN_K_SPECIFIC_INTRO,GOAL_PACE_REHEARSAL]", "relativeOrder 1,2",
            BriefSource, "brief §9 worked example: 'RACE_SPECIFIC: 1. TEN_K_SPECIFIC_INTRO, 2. GOAL_PACE_REHEARSAL'. Stage identity is a Process A authoring-time concept not surfaced in the generated PlanDocument/DecisionTrace, so Golden Fixture v3 neither corroborates nor contradicts it.",
            ["WorkoutProgressionValidator"]));
        entries.Add(Confirmed("AUD-011", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 1,
            "$.phaseProgressions[RACE_SPECIFIC].stages[GOAL_PACE_REHEARSAL].requires", "GOAL_FEASIBILITY_IN: [REALISTIC, CHALLENGING]",
            BriefSource, "brief §10 worked example, verbatim. Corroborated by Golden Fixture v3 $.goalFeasibility.classification=REALISTIC (one of the two allowed values actually produced by a real generation run).",
            ["WorkoutProgressionValidator"]));
        entries.Add(Confirmed("AUD-012", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 1,
            "$.phaseProgressions[RACE_SPECIFIC].stages[GOAL_PACE_REHEARSAL].fallbackStageKey", "CURRENT_FITNESS_SPECIFIC_REHEARSAL",
            BriefSource, "brief §10 worked example, verbatim fallback stage name. Process A authoring-time concept; not surfaced in generated output.",
            ["WorkoutProgressionValidator"]));
        entries.Add(Placeholder("AUD-013", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 1,
            "$.phaseProgressions[FOUNDATION,BUILD,TAPER].stages[*].stageKey", "FOUNDATION_EASY_BASE, FARTLEK_INTRO, THRESHOLD_INTRO, TAPER_SHARPEN",
            progressionFile, "Stage keys outside the brief's own RACE_SPECIFIC example were invented for pilot completeness. Golden Fixture v3 does not expose abstract stage identity at all (see AUD-010); remains unconfirmed.",
            ["WorkoutProgressionValidator"]));
        entries.Add(Placeholder("AUD-014", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 1,
            "$.phaseProgressions[*].stages[*].minimumExposures / maximumExposures", "various (1-6 range)",
            progressionFile, "Exposure counts invented; no canonical dosage source. progression_rules_v2.yaml (schemaVersion 2, precedence level 2) was inspected in full — it defines weekly-volume percentage caps, absolute weekly increment caps, and cutback/spike guardrails, but no per-stage exposure-count constants. Remains unconfirmed.",
            ["WorkoutProgressionValidator"]));
        entries.Add(Placeholder("AUD-015", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 1,
            "$.phaseProgressions[*].stages[*].compressionBehavior / extensionBehavior", "COMPRESSIBLE/PROTECTED, EXTENDABLE/FIXED_EXPOSURE",
            progressionFile, "Both the enum vocabulary and its per-stage assignment were invented; not specified in the brief, and not exercised by Golden Fixture v3 (runwayWeeks=0, no compression/extension needed for this plan). Remains unconfirmed per explicit instruction not to invent a replacement.",
            ["WorkoutProgressionValidator"]));

        // ===================== layout-metadata (RUN_LAYOUT_4D) =====================
        entries.Add(Confirmed("AUD-016", "layout-metadata", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 1, "$.slots (role shape)", "1 KEY_SESSION, 2 EASY_SUPPORT, 1 LONG_RUN",
            BriefSource, "brief §20: '4-day layout: one KEY_SESSION, two EASY_SUPPORT slots, one LONG_RUN'. Corroborated by Golden Fixture v3: every non-taper week's 4 days carry slotRoles {KEY_SESSION x1, EASY_SUPPORT x2, LONG_RUN x1} exactly." + VersionParityCaveat,
            ["RunLayoutValidator"]));
        entries.Add(Placeholder("AUD-017", "layout-metadata", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 1, "$.slots[*].sequenceOrder", "KEY_SESSION=1, EASY=2,3, LONG_RUN=4",
            layoutFile, "Which sequence number holds which role is an arbitrary authoring choice; the brief only mandates the shape, not the order. Golden Fixture v3 assigns concrete scheduledDate/weekday values per day — a Process B runtime-scheduling concern this catalog's SequenceOrder deliberately does not model (brief explicitly forbids assigning weekdays at the catalog level) — so it cannot confirm an authoring-time ordering convention. Remains unconfirmed.",
            ["RunLayoutValidator"]));

        // ===================== workout-definitions =====================
        AddWorkoutDefinitionEntries(entries);
        AddWave2DomainBlockerResolutionEntries(entries);
        AddWave3ComplexityRemovalEntries(entries);
        AddWave5D2ResolutionEntries(entries);
        AddD3RuntimeConditionRegistryResolutionEntries(entries);
        AddD4PeakVolumeBandResolutionEntries(entries);
        AddD13GoalPaceTenKResolutionEntries(entries);
        AddStepCV1PilotBindingGovernanceEntries(entries);

        // ===================== progression-modifier (INTERMEDIATE_PROGRESSION_MODIFIER_V1) =====================
        entries.Add(Placeholder("AUD-044", "progression-modifier", DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 1,
            "$.maximumComplexityTier, $.maximumHardSessionsPerWeek, $.mainSetDoseMultiplier, $.allowGoalPaceRehearsal, $.allowSecondHardStimulus",
            "2, 1, 1.0, true, false",
            progressionModifierFile,
            "All dosage/complexity numbers invented; no canonical source for intermediate-level caps or multipliers. " +
            $"progression_rules_v2.yaml ({ProgressionRulesSource}, precedence level 2) was checked in full: it defines profilePercentageCaps.INTERMEDIATE = {{preferred:[0.04,0.07], hardCap:0.08}} (a weekly TOTAL-VOLUME percentage-increase constraint) and cutbackPolicy.reductionRatioByProfile.INTERMEDIATE = [0.15,0.20] — neither is a MaximumHardSessionsPerWeek or MainSetDoseMultiplier value; they answer a different question (how fast weekly volume may grow / how much a cutback reduces it), not how many hard sessions per week or how a main-set dose scales. " +
            "Golden Fixture v3 realizes exactly one hard training stimulus per week for this specific INTERMEDIATE/4-day plan, but per explicit instruction this single observation does not prove MaximumHardSessionsPerWeek=1 as a general INTERMEDIATE rule — it is one data point, not a policy. Remains unconfirmed.",
            ["ProgressionModifierValidator", "TemplateCombinationValidator"]));

        // ===================== level-modifier (INTERMEDIATE_MODIFIER) =====================
        entries.Add(Placeholder("AUD-045", "workout-definitions", DocumentTypes.LevelModifier, "INTERMEDIATE_MODIFIER", 1, "$.eligibleWorkoutKeys",
            "EASY_STANDARD, LONG_RUN_STANDARD, FARTLEK, THRESHOLD_TEMPO, GOAL_PACE_TEN_K",
            levelModifierFile, "Which workouts an intermediate athlete may access is a product decision invented for the pilot. " +
            "4 of the 5 referenced keys (EASY_STANDARD, LONG_RUN_STANDARD, FARTLEK, THRESHOLD_TEMPO) are independently corroborated to exist as real, generation-used workout keys by Golden Fixture v3 — but the fixture shows a *result* (which workouts one generated plan happened to use), not a *policy* (the complete set an intermediate athlete may access). The set-membership decision itself remains unconfirmed.",
            ["LevelModifierValidator", "TemplateCombinationValidator"]));

        // ===================== runtime-condition-registry (RUNTIME_CONDITION_VALUES_V1) =====================
        entries.Add(Confirmed("AUD-046", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 1,
            "$.conditionValueSets[GOAL_FEASIBILITY_IN]", "REALISTIC, CHALLENGING, UNSUPPORTED, NOT_REQUESTED",
            BriefSource, "brief §7.6 example registry JSON, verbatim. Corroborated by Golden Fixture v3 $.goalFeasibility.classification=REALISTIC.",
            ["RuntimeConditionValueRegistryValidator"]));
        entries.Add(Confirmed("AUD-047", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 1,
            "$.conditionValueSets[PLAN_MODE_IN]", "STANDARD, FOCUSED_CORE, COMPRESSED, READINESS_ONLY, COMPLETION_FOCUSED",
            BriefSource, "brief §7.6 example registry JSON, verbatim. Corroborated by Golden Fixture v3 $.planMode=STANDARD.",
            ["RuntimeConditionValueRegistryValidator"]));
        entries.Add(Placeholder("AUD-048", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 1,
            "$.conditionValueSets[PACE_SOURCE_IN,TIME_ADEQUACY_IN,CORE_ENTRY_READINESS_IN]", "authored allowed-value lists",
            registryFile,
            "The brief names these RuntimeConditionType values (§7.5) but never gives their allowed-value vocabulary; invented for schema completeness. " +
            "Golden Fixture v3's DecisionTrace contains plausibly-related internal resolver fields — capacitySnapshot.paceSource=RECENT_RACE, TIME_ADEQUACY_RESOLVER.result.timeAdequacy=ADEQUATE, CORE_ENTRY_READINESS_RESOLVER.result.readiness=STANDARD — none of which match this registry's currently-invented values (RACE_RESULT/TIME_TRIAL/ESTIMATED/NOT_PROVIDED; READY/NOT_READY/UNKNOWN), except ADEQUATE which happens to already be one of ours. " +
            "Per explicit instruction ('Golden Fixture v3 may verify actual Process B output vocabulary, but it must not silently redefine Process A registry ownership' / 'Do not promote Process B output-only values into Process A shared contracts without explicit ownership evidence'), these DecisionTrace field names are Process-B-internal resolver output labels with no stated mapping to this Process A registry's RuntimeConditionType conditions — the naming similarity is suggestive, not dispositive. NOT reclassified; registry values left unchanged and unconfirmed pending an explicit ownership decision.",
            ["RuntimeConditionValueRegistryValidator"]));

        // ===================== peak-volume-policy (PEAK_VOLUME_BANDS_V1) =====================
        // PEAK-POLICY-IMMUT-001 remediation (see artifacts/audits/peak-volume-policy-immutability-remediation.md):
        // v1 is RESTORED to its exact original, earliest-published (1.0.0) content — INTERMEDIATE rows
        // 26-40/30-45/34-50km, never fixture-confirmed. The fixture-confirmed rows (22-32/30-42/36-50km)
        // now live on the genuinely new v2 artifact, never under v1.
        entries.Add(Placeholder("AUD-049", "peak-volume-policy", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 1,
            "$.entries[TEN_K,INTERMEDIATE,3|4|5]", "26-40km / 30-45km / 34-50km",
            "1.0.0 (restored original)", "v1's true, restored historical content. An earlier pass mistakenly edited this v1 file in place with fixture-corroborated values instead of creating v2 (PEAK-POLICY-IMMUT-001); it has now been restored to its exact original content, which was never independently corroborated. The confirmed rows live on v2, not here. Remains unconfirmed for v1.",
            ["PeakVolumeBandPolicyValidator", "TemplateCombinationValidator"]));
        entries.Add(Confirmed("AUD-056", "peak-volume-policy", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 2,
            "$.entries[TEN_K,INTERMEDIATE,3|4|5]", "22-32km / 30-42km / 36-50km",
            "review-provided", "Provided as canonical v1.0 source material in the domain-content audit review request (TEN_K/Intermediate 3/4/5-day bands). " +
            "The 4-day row (30-42km) is independently corroborated by Golden Fixture v3 $.peakVolume.typicalBandKm=[30,42] — exact match. Do not confuse this typical band with the fixture's resolvedPeakKm=38 (a Process-B-computed instance value for this one athlete; never stored as reusable policy). This confirmed value lives on the genuinely new v2 artifact created by PEAK-POLICY-IMMUT-001 remediation.",
            ["PeakVolumeBandPolicyValidator", "TemplateCombinationValidator"]));
        entries.Add(Placeholder("AUD-050", "peak-volume-policy", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 1,
            "$.entries[TEN_K,NEW|ADVANCED|EXPERIENCED,3|4|5]", "9 authored rows",
            peakFile, "No canonical v1.0 source located in the repository or provided for NEW/ADVANCED/EXPERIENCED TEN_K rows; invented to complete the matrix shape. Golden Fixture v3 is an INTERMEDIATE-only fixture and cannot evidence other experience levels; per explicit instruction, not interpolated or extrapolated. Unaffected by PEAK-POLICY-IMMUT-001 (these rows are byte-identical between v1 and v2 — not invented or changed by this remediation).",
            ["PeakVolumeBandPolicyValidator"]));
        entries.Add(Placeholder("AUD-057", "peak-volume-policy", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 2,
            "$.entries[TEN_K,NEW|ADVANCED|EXPERIENCED,3|4|5]", "9 authored rows",
            "catalog/policies/peak-volume-bands.v2.json", "Same unconfirmed rows as v1 AUD-050 — v2 only corrected the INTERMEDIATE rows; NEW/ADVANCED/EXPERIENCED rows were carried over unchanged and remain unconfirmed. Not invented or interpolated by PEAK-POLICY-IMMUT-001.",
            ["PeakVolumeBandPolicyValidator"]));

        // ===================== technical-metadata =====================
        entries.Add(Technical("AUD-051", "technical-metadata", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 1, "$.runtimeConditionValueRegistry, $.peakVolumeBandPolicy, $.policies, $.rules",
            "references only (policies/rules empty)", rulePackFile, ["RulePackValidator"]));
        entries.Add(Technical("AUD-052", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 1, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "root references only (masterTemplate v1 — published, immutable, unchanged)", combinationFile, ["TemplateCombinationValidator"]));
        entries.Add(Technical("AUD-055", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 2, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "root references only (masterTemplate v2 — corrected TAPER family)", "catalog/combinations/ten-k-4d-intermediate.v2.json", ["TemplateCombinationValidator"],
            reason: "Combination v1 preserved; new v2 created referencing TEN_K_MASTER v2. v2 was created after discovering that v1's source file had been mutated in place " +
            "(masterTemplate.version changed 1→2 while the filename/declared version stayed 1) rather than properly versioned. v1 was restored to its exact historical " +
            "content (contentHash c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71, matching all releases published before the defect) and v2 was created " +
            "as a distinct, independently-hashed artifact. See artifacts/audits/combination-immutability-investigation.md."));
        entries.Add(Technical("AUD-058", "technical-metadata", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 2, "$.runtimeConditionValueRegistry, $.peakVolumeBandPolicy, $.policies, $.rules",
            "references only (peakVolumeBandPolicy now v2; runtimeConditionValueRegistry unchanged at v1)", "catalog/rule-packs/appsel-race-plan.v2.json", ["RulePackValidator"],
            reason: "RulePack v1 preserved unchanged (still correctly references the restored PEAK_VOLUME_BANDS_V1 v1); new v2 created solely to point peakVolumeBandPolicy at the " +
            "corrected PEAK_VOLUME_BANDS_V1 v2 (PEAK-POLICY-IMMUT-001). No other field changed. See artifacts/audits/dependency-version-cascade-audit.md."));
        entries.Add(Technical("AUD-059", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 3, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "root references only (rulePack v2 — corrected peak-volume policy; masterTemplate/layout/levelModifier unchanged from v2)", "catalog/combinations/ten-k-4d-intermediate.v3.json", ["TemplateCombinationValidator"],
            reason: "Combination v1 and v2 preserved unchanged (both already PUBLISHED); new v3 created solely to point rulePack at the corrected APPSEL_RACE_PLAN_V1 v2 " +
            "(PEAK-POLICY-IMMUT-001 cascade). v3 is the new active pilot combination. See artifacts/audits/dependency-version-cascade-audit.md."));

        // ===================== technical-metadata: artifact-version parity (record only) =====================
        entries.Add(Technical("AUD-053", "technical-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 2, "$.metadata.version",
            "v2 (incidentally resolves TEN_K_MASTER-side parity)",
            masterFile,
            ["PlanTemplateValidator"],
            reason:
            "ARTIFACT_VERSION_PARITY: Golden Fixture v3 references TEN_K_MASTER v2 ($.template.version in " + FixtureSource + "). " +
            "This task's TAPER-family correction required publishing a new immutable version regardless (v1 was already PUBLISHED), and v2 was chosen — which incidentally now matches the fixture's own version reference. " +
            "This was not done to force parity; it is a side effect of the immutability rule (a new version number was mandatory once content changed). No other TEN_K_MASTER v2 content was back-filled from the fixture beyond the TAPER family correction itself."));
        entries.Add(Technical("AUD-054", "technical-metadata", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 1, "$.metadata.version",
            "v1 (fixture references v3; NOT upgraded)",
            rulePackFile,
            ["RulePackValidator", "TemplateCombinationValidator"],
            reason:
            "ARTIFACT_VERSION_PARITY_UNRESOLVED: Golden Fixture v3 references APPSEL_RACE_PLAN_V1 v3 ($.rulePack.version in " + FixtureSource + "), " +
            "while the current catalog only has v1. Semantic impact: unknown — the fixture does not reveal what changed between rule-pack v1/v2/v3 (policies/rules arrays are currently empty in this pilot, so no observable behavioral difference is evidenced). " +
            "Does not block the TAPER correction in this task (the TAPER fix only required a new TEN_K_MASTER version, not a rule-pack change). " +
            "Required future decision: a dedicated review must determine what APPSEL_RACE_PLAN_V1 v2/v3 actually added/changed and whether this pilot's rule pack needs a version bump; explicitly out of scope here per instruction not to upgrade APPSEL_RACE_PLAN_V1 in this task."));

        return entries;
    }

    private static void AddWave2DomainBlockerResolutionEntries(List<DomainContentDecision> entries)
    {
        const string wave2Schema = "artifacts/audits/domain-wave2-schema-migration.md";
        const string wave2Vocabulary = "artifacts/audits/domain-wave2-component-vocabulary.md";

        entries.Add(Technical("AUD-300", "layout-metadata", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 2, "$.slots (array order)",
            "sequenceOrder absent; order derived from slots array position",
            "catalog/layouts/run-layout-4d.v2.json",
            ["RunLayoutValidator"],
            reason: "WAVE2 D1: schemaVersion 2 removes the independently-authored sequenceOrder field. Slot order is derived mechanically from the JSON array position and carries no running-domain claim; historical v1 sequenceOrder remains readable."));

        entries.Add(Placeholder("AUD-301", "workout-definitions", DocumentTypes.WorkoutDefinition, "EASY_STANDARD", 3, "$.complexityTier", "1",
            "catalog/workouts/easy-standard.v3.json",
            "WAVE2 intentionally preserved the unresolved complexityTier decision from EASY_STANDARD v2. This task resolved only D6/components; D5 remains PLACEHOLDER_UNCONFIRMED.",
            ["WorkoutDefinitionValidator"]));
        entries.Add(Technical("AUD-302", "workout-definitions", DocumentTypes.WorkoutDefinition, "EASY_STANDARD", 3, "$.components",
            "absent",
            wave2Schema,
            ["WorkoutDefinitionValidator"],
            reason: "WAVE2 D6: EASY_STANDARD is a continuous workout; components are optional and omitted rather than synthesized as WARM_UP/MAIN_SET/COOL_DOWN. This is a schema/ownership decision, not a dosage claim."));

        entries.Add(Placeholder("AUD-303", "workout-definitions", DocumentTypes.WorkoutDefinition, "FARTLEK", 3, "$.complexityTier", "1",
            "catalog/workouts/fartlek.v3.json",
            "WAVE2 intentionally preserved the unresolved complexityTier decision from FARTLEK v2. This task resolved only D8/components; D7 remains PLACEHOLDER_UNCONFIRMED.",
            ["WorkoutDefinitionValidator"]));
        entries.Add(ExplicitDefault("AUD-304", "workout-definitions", DocumentTypes.WorkoutDefinition, "FARTLEK", 3, "$.components",
            "WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN",
            wave2Vocabulary,
            "WAVE2 D8 / WAVE3 evidence review: approved structural component sequence for FARTLEK. The Golden Fixture shows generated fartlek work with warm-up, variable-effort main work, recovery segments, and cool-down, but it does not canonically define this reusable catalog component sequence. RECOVERY represents recovery segments between variable efforts; no concrete duration, distance, pace, or repetition values are assigned.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(Placeholder("AUD-305", "workout-definitions", DocumentTypes.WorkoutDefinition, "LONG_RUN_STANDARD", 3, "$.complexityTier", "1",
            "catalog/workouts/long-run-standard.v3.json",
            "WAVE2 intentionally preserved the unresolved complexityTier decision from LONG_RUN_STANDARD v2. This task resolved only D10/components; D9 remains PLACEHOLDER_UNCONFIRMED.",
            ["WorkoutDefinitionValidator"]));
        entries.Add(Technical("AUD-306", "workout-definitions", DocumentTypes.WorkoutDefinition, "LONG_RUN_STANDARD", 3, "$.components",
            "absent",
            wave2Schema,
            ["WorkoutDefinitionValidator"],
            reason: "WAVE2 D10: LONG_RUN_STANDARD is a standard continuous long run; components are optional and omitted. No marathon-specific, fast-finish, progression, or embedded-quality structure was introduced."));

        entries.Add(Placeholder("AUD-307", "workout-definitions", DocumentTypes.WorkoutDefinition, "THRESHOLD_TEMPO", 3, "$.complexityTier", "2",
            "catalog/workouts/threshold-tempo.v3.json",
            "WAVE2 intentionally preserved the unresolved complexityTier decision from THRESHOLD_TEMPO v2. This task resolved only D12/components; D11 remains PLACEHOLDER_UNCONFIRMED.",
            ["WorkoutDefinitionValidator"]));
        entries.Add(ExplicitDefault("AUD-308", "workout-definitions", DocumentTypes.WorkoutDefinition, "THRESHOLD_TEMPO", 3, "$.components",
            "WARM_UP, MAIN_SET, COOL_DOWN",
            wave2Vocabulary,
            "WAVE2 D12 / WAVE3 evidence review: approved continuous-tempo component sequence. The Golden Fixture shows tempo main-set output with warm-up and cool-down, but it does not canonically define the reusable catalog decomposition. RECOVERY and cruise-repeat or interval structure are excluded from this artifact and remain separate workout-definition concerns.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(Technical("AUD-309", "technical-metadata", DocumentTypes.LevelModifier, "INTERMEDIATE_MODIFIER", 3, "$.eligibleWorkouts",
            "exact workout references repointed to EASY_STANDARD v3, LONG_RUN_STANDARD v3, FARTLEK v3, THRESHOLD_TEMPO v3; GOAL_PACE_TEN_K remains v1",
            "catalog/level-modifiers/intermediate-modifier.v3.json",
            ["LevelModifierValidator", "TemplateCombinationValidator"],
            reason: "WAVE2 immutable parent cascade: only exact references changed to preserve the candidate dependency graph after leaf workout version changes."));
        entries.Add(Technical("AUD-310", "technical-metadata", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 3, "$.phaseProgressions[*].stages[*].workoutCandidates",
            "exact workout references repointed to EASY_STANDARD/FARTLEK/THRESHOLD_TEMPO v3 where those candidates are used",
            "catalog/workout-progressions/ten-k-workout-progression.v3.json",
            ["WorkoutProgressionValidator", "TemplateCombinationValidator"],
            reason: "WAVE2 immutable parent cascade required by exact references; stage dosage and selection rules were copied unchanged from v2."));
        entries.Add(Technical("AUD-311", "technical-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 4, "$.workoutProgression",
            "TEN_K_WORKOUT_PROGRESSION_V1 v3",
            "catalog/templates/ten-k-master.v4.json",
            ["PlanTemplateValidator", "TemplateCombinationValidator"],
            reason: "WAVE2 immutable parent cascade required because TEN_K_MASTER owns the exact WorkoutProgression reference. Template content is otherwise unchanged from v3."));
        entries.Add(Technical("AUD-312", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 5, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "candidate root references TEN_K_MASTER v4, RUN_LAYOUT_4D v2, INTERMEDIATE_MODIFIER v3, APPSEL_RACE_PLAN_V1 v2",
            "catalog/combinations/ten-k-4d-intermediate.v5.json",
            ["TemplateCombinationValidator"],
            reason: "WAVE2 candidate root. Predecessor is TEN_K__4D__INTERMEDIATE v4; no publish, retirement, or activation is applied in this task."));
    }

    private static void AddWave3ComplexityRemovalEntries(List<DomainContentDecision> entries)
    {
        const string wave3Evidence = "artifacts/audits/domain-wave3-d8-d12-evidence-review.md";

        entries.Add(Technical("AUD-313", "workout-definitions", DocumentTypes.WorkoutDefinition, "EASY_STANDARD", 4, "$.complexityTier",
            "absent",
            "catalog/workouts/easy-standard.v4.json",
            ["WorkoutDefinitionValidator"],
            reason: "WAVE3 D5: removed redundant legacy complexityTier field from reusable WorkoutDefinition schemaVersion 3. No replacement taxonomy, derived field, or running-domain tier value was selected."));
        entries.Add(Technical("AUD-314", "workout-definitions", DocumentTypes.WorkoutDefinition, "FARTLEK", 4, "$.complexityTier",
            "absent",
            "catalog/workouts/fartlek.v4.json",
            ["WorkoutDefinitionValidator"],
            reason: "WAVE3 D7: removed redundant legacy complexityTier field from reusable WorkoutDefinition schemaVersion 3. No replacement taxonomy, derived field, or running-domain tier value was selected."));
        entries.Add(ExplicitDefault("AUD-315", "workout-definitions", DocumentTypes.WorkoutDefinition, "FARTLEK", 4, "$.components",
            "WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN",
            wave3Evidence,
            "WAVE3 D8 evidence review retained the approved sequence as EXPLICIT_PRODUCT_DEFAULT, not CANONICAL_CONFIRMED. Fixture evidence supports the shape but does not directly canonize the reusable catalog component vocabulary or ordering.",
            ["WorkoutDefinitionValidator"]));
        entries.Add(Technical("AUD-316", "workout-definitions", DocumentTypes.WorkoutDefinition, "LONG_RUN_STANDARD", 4, "$.complexityTier",
            "absent",
            "catalog/workouts/long-run-standard.v4.json",
            ["WorkoutDefinitionValidator"],
            reason: "WAVE3 D9: removed redundant legacy complexityTier field from reusable WorkoutDefinition schemaVersion 3. No replacement taxonomy, derived field, or running-domain tier value was selected."));
        entries.Add(Technical("AUD-317", "workout-definitions", DocumentTypes.WorkoutDefinition, "THRESHOLD_TEMPO", 4, "$.complexityTier",
            "absent",
            "catalog/workouts/threshold-tempo.v4.json",
            ["WorkoutDefinitionValidator"],
            reason: "WAVE3 D11: removed redundant legacy complexityTier field from reusable WorkoutDefinition schemaVersion 3. No replacement taxonomy, derived field, or running-domain tier value was selected."));
        entries.Add(ExplicitDefault("AUD-318", "workout-definitions", DocumentTypes.WorkoutDefinition, "THRESHOLD_TEMPO", 4, "$.components",
            "WARM_UP, MAIN_SET, COOL_DOWN",
            wave3Evidence,
            "WAVE3 D12 evidence review retained the approved sequence as EXPLICIT_PRODUCT_DEFAULT, not CANONICAL_CONFIRMED. Fixture evidence supports the shape but does not directly canonize the reusable catalog component vocabulary or ordering.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(Technical("AUD-319", "technical-metadata", DocumentTypes.LevelModifier, "INTERMEDIATE_MODIFIER", 4, "$.eligibleWorkouts",
            "exact workout references repointed to EASY_STANDARD v4, LONG_RUN_STANDARD v4, FARTLEK v4, THRESHOLD_TEMPO v4; GOAL_PACE_TEN_K remains v1",
            "catalog/level-modifiers/intermediate-modifier.v4.json",
            ["LevelModifierValidator", "TemplateCombinationValidator"],
            reason: "WAVE3 immutable parent cascade: only exact references changed to preserve the candidate dependency graph after leaf workout version changes."));
        entries.Add(Technical("AUD-320", "technical-metadata", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 4, "$.phaseProgressions[*].stages[*].workoutCandidates",
            "exact workout references repointed to EASY_STANDARD/FARTLEK/THRESHOLD_TEMPO v4 where those candidates are used",
            "catalog/workout-progressions/ten-k-workout-progression.v4.json",
            ["WorkoutProgressionValidator", "TemplateCombinationValidator"],
            reason: "WAVE3 immutable parent cascade required by exact references; stage dosage and selection rules were copied unchanged from v3."));
        entries.Add(Technical("AUD-321", "technical-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 5, "$.workoutProgression",
            "TEN_K_WORKOUT_PROGRESSION_V1 v4",
            "catalog/templates/ten-k-master.v5.json",
            ["PlanTemplateValidator", "TemplateCombinationValidator"],
            reason: "WAVE3 immutable parent cascade required because TEN_K_MASTER owns the exact WorkoutProgression reference. Template content is otherwise unchanged from v4."));
        entries.Add(Technical("AUD-322", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 6, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "candidate root references TEN_K_MASTER v5, RUN_LAYOUT_4D v2, INTERMEDIATE_MODIFIER v4, APPSEL_RACE_PLAN_V1 v2",
            "catalog/combinations/ten-k-4d-intermediate.v6.json",
            ["TemplateCombinationValidator"],
            reason: "WAVE3 candidate root. Predecessor candidate TEN_K__4D__INTERMEDIATE v5 is preserved unchanged; no publish, retirement, or activation is applied in this task."));
    }

    private static void AddWave5D2ResolutionEntries(List<DomainContentDecision> entries)
    {
        const string wave5Ownership = "artifacts/audits/domain-wave5-d2-ownership.md";
        const string wave5Implementation = "artifacts/audits/domain-wave5-d2-implementation.md";
        const string progressionModifierV2File = "catalog/progression-modifiers/intermediate-progression-modifier.v2.json";

        // AUD-044 (v1) is left untouched — historical fact, still PLACEHOLDER_UNCONFIRMED for that
        // immutable, already-PUBLISHED version. D2 is resolved only on the new v2 artifact below, one
        // entry per field (this task's approved decision set assigns a different classification to each
        // of the 5 fields, so they can no longer share a single bundled audit row).

        entries.Add(Technical("AUD-330", "progression-modifier", DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2, "$.maximumComplexityTier",
            "absent",
            wave5Implementation,
            ["ProgressionModifierValidator"],
            reason: "WAVE5 D2: removed redundant legacy maximumComplexityTier field from ProgressionModifier schemaVersion 2 (complexityTier was already removed from WorkoutDefinition in Wave 3; the cap no longer has a meaningful target field). No replacement complexity field was introduced."));

        entries.Add(Confirmed("AUD-331", "progression-modifier", DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2, "$.maximumHardSessionsPerWeek", "1",
            progressionModifierV2File,
            "WAVE5 D2, evidence-backed for this exact scope: Golden Fixture v3 realizes exactly one hard training stimulus per week for the TEN_K/INTERMEDIATE/4-day combination this artifact's sole current referrer (INTERMEDIATE_MODIFIER -> TEN_K__4D__INTERMEDIATE) represents. Approved as a CEILING, not a target or minimum — valid weeks may contain zero hard sessions (deload/taper/readiness); no consumer may infer that exactly one hard session must always be scheduled. Scope: TEN_K / INTERMEDIATE / 4 runs per week only; not generalized to other day-counts, distances, or experience levels sharing the INTERMEDIATE label without independent evidence.",
            ["ProgressionModifierValidator", "TemplateCombinationValidator"]));

        entries.Add(ExplicitDefault("AUD-332", "progression-modifier", DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2, "$.mainSetDoseMultiplier", "1.00",
            wave5Implementation,
            "WAVE5 D2: INTERMEDIATE is the product baseline/reference dose; 1.00 is an identity multiplier with no scaling effect, not a scientifically validated universal ratio. Consumption trace (see " + wave5Ownership + "): MainSetDoseMultiplier is validated (>0) and transported through PublishedTemplateBundle by reference only — no consumer in this repository (Process A) multiplies any dose/duration/distance/repetition value by it; Process B (out of scope, runner/backend) is the only theoretical consumer and was not inspected or modified. It is unused for computation in this repository today. Non-identity values remain unsupported pending a future normalized-dose contract; no multiplierTarget/doseMetric field was introduced.",
            ["ProgressionModifierValidator"]));

        entries.Add(ExplicitDefault("AUD-333", "progression-modifier", DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2, "$.allowGoalPaceRehearsal", "true",
            wave5Implementation,
            "WAVE5 D2: PRINCIPLE_FLAG / UNCONSUMED. This is a capability flag only — it does not independently make any workout eligible. No runtime guard code (race-specific phase, goal feasibility, preparation time, pace confidence, hard-session budget, or workout progression/stage eligibility) reads this field in this repository today; those guards, where they exist, are separate concerns (e.g. TEN_K_WORKOUT_PROGRESSION_V1's GOAL_PACE_REHEARSAL stage `requires: GOAL_FEASIBILITY_IN`). No new runtime behavior was introduced by setting this value. " +
            "WAVE5-CLARIFICATION: the field has zero readers in this repository (confirmed by repository-wide search) — it is currently write-only authoring metadata. The prior 'RUNTIME_GUARDED' tag is corrected to 'UNCONSUMED' because no runtime guard code actually reads this field; GOAL_FEASIBILITY_IN is an independent gate that applies regardless of this boolean's value, and this boolean being true does not bypass it or make any workout eligible by itself. See artifacts/audits/domain-wave5-d2-clarification.md.",
            ["ProgressionModifierValidator"]));

        entries.Add(Confirmed("AUD-334", "progression-modifier", DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", 2, "$.allowSecondHardStimulus", "false",
            progressionModifierV2File,
            "WAVE5 D2, evidence-backed for this exact scope: for the TEN_K/INTERMEDIATE/4-day pilot, do not allow a second hard stimulus in the same week (if goal-pace rehearsal is already HARD, long-run quality must be suppressed/downgraded) — consistent with ProgressionModifierValidator's PM_HARD_SESSION_CAP_EXCEEDS_SINGLE_STIMULUS rule and the fixture's single-hard-stimulus week. This is NOT a universal statement for every INTERMEDIATE plan family: ownership was inspected (see " + wave5Ownership + ") and INTERMEDIATE_PROGRESSION_MODIFIER_V1 currently has exactly one referrer (INTERMEDIATE_MODIFIER, referenced only by the TEN_K__4D__INTERMEDIATE combination family) — reuse boundary matches this artifact's actual scope exactly today. 5-6 day or other-distance plan families remain explicitly out of scope and must not be assumed to inherit this value without new evidence if/when they are introduced.",
            ["ProgressionModifierValidator", "TemplateCombinationValidator"]));

        entries.Add(Technical("AUD-335", "technical-metadata", DocumentTypes.LevelModifier, "INTERMEDIATE_MODIFIER", 5, "$.progressionModifier",
            "exact reference repointed to INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2",
            "catalog/level-modifiers/intermediate-modifier.v5.json",
            ["LevelModifierValidator", "TemplateCombinationValidator"],
            reason: "WAVE5 immutable parent cascade: only the exact ProgressionModifier reference changed to preserve the candidate dependency graph after the D2 leaf-artifact version change. eligibleWorkouts copied unchanged from v4."));
        entries.Add(Technical("AUD-336", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 7, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "candidate root references TEN_K_MASTER v5, RUN_LAYOUT_4D v2, INTERMEDIATE_MODIFIER v5, APPSEL_RACE_PLAN_V1 v2",
            "catalog/combinations/ten-k-4d-intermediate.v7.json",
            ["TemplateCombinationValidator"],
            reason: "WAVE5 candidate root (D2 resolution only). Predecessor candidate TEN_K__4D__INTERMEDIATE v6 is preserved unchanged; no publish, retirement, or activation is applied in this task."));
    }

    private static void AddD3RuntimeConditionRegistryResolutionEntries(List<DomainContentDecision> entries)
    {
        const string registryV2File = "catalog/registries/runtime-condition-values.v2.json";

        // AUD-048 (v1) is left untouched — historical fact, still PLACEHOLDER_UNCONFIRMED for that
        // immutable, already-PUBLISHED version. D3 is resolved only on the new v2 artifact below.

        entries.Add(Confirmed("AUD-400", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 2,
            "$.conditionValueSets[PACE_SOURCE_IN]", "NONE, RECENT_RACE, ESTIMATED, TARGET_TIME",
            registryV2File,
            "D3 RESOLUTION: explicit Process A/Process B ownership decision recorded — this Process A registry is the sole canonical owner of PACE_SOURCE_IN's allowed-value vocabulary; Process B may map user/onboarding inputs into these values but may not invent additional runtime condition codes. Approved canonical values: NONE (no usable pace input available), RECENT_RACE (pace derived from a recent race result), ESTIMATED (pace estimated from user-provided running background/recent volume/longest run/plan assumptions), TARGET_TIME (pace derived from an explicit target finish time). This supersedes AUD-048's invented RACE_RESULT/TIME_TRIAL/ESTIMATED/NOT_PROVIDED set on v1, which remains unchanged and unconfirmed on that immutable version.",
            ["RuntimeConditionValueRegistryValidator"]));

        entries.Add(Confirmed("AUD-401", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 2,
            "$.conditionValueSets[TIME_ADEQUACY_IN]", "ADEQUATE, COMPRESSED, INSUFFICIENT",
            registryV2File,
            "D3 RESOLUTION: approved canonical values for TIME_ADEQUACY_IN. ADEQUATE (available plan duration is sufficient for the normal/default plan structure), COMPRESSED (available duration is shorter than ideal but still usable with a compressed core plan), INSUFFICIENT (available duration is too short for a safe or meaningful training-plan build; should not silently generate a normal plan). This supersedes AUD-048's invented ADEQUATE/TIGHT/INSUFFICIENT set on v1, which remains unchanged and unconfirmed on that immutable version. Note: COMPRESSED is also a PLAN_MODE_IN value on a different conditionType; RuntimeConditionValueRegistryValidator only checks for duplicate condition TYPES and per-set uniqueness, not cross-type value collisions, so this is not a validation conflict.",
            ["RuntimeConditionValueRegistryValidator"]));

        entries.Add(Confirmed("AUD-402", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 2,
            "$.conditionValueSets[CORE_ENTRY_READINESS_IN]", "READY, CAUTION, NOT_READY",
            registryV2File,
            "D3 RESOLUTION: approved canonical values for CORE_ENTRY_READINESS_IN. READY (user can enter the normal core plan), CAUTION (user can enter only with conservative constraints, warnings, or reduced assumptions), NOT_READY (user should not enter the normal core plan; use readiness-only guidance or reject normal plan generation depending on product flow). This supersedes AUD-048's invented READY/NOT_READY/UNKNOWN set on v1, which remains unchanged and unconfirmed on that immutable version.",
            ["RuntimeConditionValueRegistryValidator"]));

        entries.Add(Technical("AUD-403", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 2,
            "$.conditionValueSets[GOAL_FEASIBILITY_IN,PLAN_MODE_IN]", "unchanged from v1 (REALISTIC/CHALLENGING/UNSUPPORTED/NOT_REQUESTED; STANDARD/FOCUSED_CORE/COMPRESSED/READINESS_ONLY/COMPLETION_FOCUSED)",
            registryV2File,
            ["RuntimeConditionValueRegistryValidator"],
            reason: "D3 scope was limited to PACE_SOURCE_IN, TIME_ADEQUACY_IN, and CORE_ENTRY_READINESS_IN. GOAL_FEASIBILITY_IN and PLAN_MODE_IN were already CANONICAL_CONFIRMED (AUD-046/AUD-047) and are carried forward byte-identical; not re-litigated by this task."));

        entries.Add(Technical("AUD-404", "technical-metadata", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 3, "$.runtimeConditionValueRegistry, $.peakVolumeBandPolicy, $.policies, $.rules",
            "references only (runtimeConditionValueRegistry now v2; peakVolumeBandPolicy unchanged at v2)",
            "catalog/rule-packs/appsel-race-plan.v3.json",
            ["RulePackValidator"],
            reason: "D3 immutable parent cascade: RulePack v1/v2 preserved unchanged; new v3 created solely to point runtimeConditionValueRegistry at the corrected RUNTIME_CONDITION_VALUES_V1 v2. peakVolumeBandPolicy unchanged (D4 not resolved in this task)."));

        entries.Add(Technical("AUD-405", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 8, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "candidate root references TEN_K_MASTER v5, RUN_LAYOUT_4D v2, INTERMEDIATE_MODIFIER v5, APPSEL_RACE_PLAN_V1 v3",
            "catalog/combinations/ten-k-4d-intermediate.v8.json",
            ["TemplateCombinationValidator"],
            reason: "D3 candidate root (RUNTIME_CONDITION_VALUES_V1 resolution only). Predecessor candidate TEN_K__4D__INTERMEDIATE v7 (Wave 5 / D2) is preserved unchanged; no publish, retirement, or activation is applied in this task."));

        entries.Add(Technical("AUD-406", "runtime-condition-registry", DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", 2,
            "$.conditionValueSets[PACE_SOURCE_IN,TIME_ADEQUACY_IN,CORE_ENTRY_READINESS_IN]", "declared and structurally validated; zero references in any catalog artifact",
            "artifacts/audits/domain-d3-followup.md",
            ["RuntimeConditionValueRegistryValidator"],
            reason: "TD-D3-001 (activation-readiness note, does not reopen D3): repository-wide search of catalog/ (workout-progressions, rule-packs, templates, workouts) found zero references to PACE_SOURCE_IN, TIME_ADEQUACY_IN, or CORE_ENTRY_READINESS_IN outside the registry artifact itself — no stage.Requires, rule, or other artifact currently consumes these three condition types, so the D3 vocabulary normalization has no traced candidate/runtime behavior impact today (DECLARED_BUT_CURRENTLY_UNUSED). Process B/backend mapping to the v2 canonical values is UNKNOWN_FROM_REPO_EVIDENCE (out of scope to inspect runner/backend). Golden Fixture v3's DecisionTrace resolver output is suggestive but not dispositive (per AUD-048's existing caution): paceSource=RECENT_RACE now textually matches the new v2 PACE_SOURCE_IN value (it did not match v1's RACE_RESULT); readiness=STANDARD matches neither v1 nor v2 CORE_ENTRY_READINESS_IN vocabulary at all. Before any future publish/activation of TEN_K__4D__INTERMEDIATE v8 or its descendants, Process B/runtime must explicitly confirm it maps to the v2 canonical values and no longer emits or expects the old v1 strings (RACE_RESULT/TIME_TRIAL/NOT_PROVIDED/TIGHT/UNKNOWN). See artifacts/audits/domain-d3-followup.md for the full consumer trace."));
    }

    private static void AddD4PeakVolumeBandResolutionEntries(List<DomainContentDecision> entries)
    {
        const string peakV3File = "catalog/policies/peak-volume-bands.v3.json";

        // AUD-049/AUD-050 (v1) and AUD-056/AUD-057 (v2) are left untouched — historical facts, still
        // their original classifications for those immutable, already-PUBLISHED versions. D4 is resolved
        // only on the new v3 artifact below, which is scoped to TEN_K/INTERMEDIATE only.

        entries.Add(Confirmed("AUD-410", "peak-volume-policy", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 3,
            "$.entries[TEN_K,INTERMEDIATE,3|4|5]", "22-32km / 30-42km / 36-50km",
            "review-provided", "D4 RESOLUTION: carries forward the same review-provided, fixture-corroborated INTERMEDIATE values already CANONICAL_CONFIRMED on v2 (AUD-056) — unchanged in value. The 4-day row (30-42km) remains independently corroborated by Golden Fixture v3 $.peakVolume.typicalBandKm=[30,42].",
            ["PeakVolumeBandPolicyValidator", "TemplateCombinationValidator"]));

        entries.Add(Technical("AUD-411", "peak-volume-policy", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 3,
            "$.entries[TEN_K,NEW|ADVANCED|EXPERIENCED,3|4|5]", "absent (removed, not carried forward from v2)",
            peakV3File,
            ["PeakVolumeBandPolicyValidator"],
            reason: "D4 RESOLUTION: the 9 unapproved, invented NEW/ADVANCED/EXPERIENCED rows (previously AUD-050 on v1, AUD-057 on v2 — 'No canonical v1.0 source located... invented to complete the matrix shape') are removed entirely from v3, not replaced or reclassified. v3's scope is narrowed to exactly the TEN_K/INTERMEDIATE pilot rows this candidate family needs; no replacement values were invented for NEW/ADVANCED/EXPERIENCED. PeakVolumeBandPolicyValidator has no requirement that every experience level be represented (only duplicate-tuple and min<=max checks), so this narrowing is structurally valid."));

        entries.Add(Technical("AUD-412", "technical-metadata", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4, "$.runtimeConditionValueRegistry, $.peakVolumeBandPolicy, $.policies, $.rules",
            "references only (peakVolumeBandPolicy now v3; runtimeConditionValueRegistry unchanged at v2)",
            "catalog/rule-packs/appsel-race-plan.v4.json",
            ["RulePackValidator"],
            reason: "D4 immutable parent cascade: RulePack v1/v2/v3 preserved unchanged; new v4 created solely to point peakVolumeBandPolicy at the corrected PEAK_VOLUME_BANDS_V1 v3. runtimeConditionValueRegistry unchanged (D3 vocabulary not touched by this task)."));

        entries.Add(Technical("AUD-413", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 9, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "candidate root references TEN_K_MASTER v5, RUN_LAYOUT_4D v2, INTERMEDIATE_MODIFIER v5, APPSEL_RACE_PLAN_V1 v4",
            "catalog/combinations/ten-k-4d-intermediate.v9.json",
            ["TemplateCombinationValidator"],
            reason: "D4 candidate root (PEAK_VOLUME_BANDS_V1 resolution only). Predecessor candidate TEN_K__4D__INTERMEDIATE v8 (D3) is preserved unchanged; no publish, retirement, or activation is applied in this task."));
    }

    private static void AddD13GoalPaceTenKResolutionEntries(List<DomainContentDecision> entries)
    {
        const string goalPaceV2File = "catalog/workouts/goal-pace-ten-k.v2.json";

        // AUD-249 (v1, dynamically numbered — see AddWorkoutDefinitionEntries) is left untouched --
        // historical fact, still PLACEHOLDER_UNCONFIRMED for that version. D13 is resolved only on the
        // new v2 artifact below. Content is byte-identical to v1: the approved D13 decision confirms the
        // EXISTING structural representation was already correct (single continuous WARM_UP/MAIN_SET
        // (GOAL_PACE)/COOL_DOWN block, RACE_SPECIFIC-only, PACE_BASED prescription) -- nothing needed to
        // change except the governance classification, which per this repository's immutability discipline
        // requires a new version rather than mutating the historical v1 record.

        entries.Add(Confirmed("AUD-420", "workout-definitions", DocumentTypes.WorkoutDefinition, "GOAL_PACE_TEN_K", 2, "$.eligiblePhases", "RACE_SPECIFIC",
            goalPaceV2File,
            "D13 RESOLUTION: approved product/training decision confirms goal-pace work is scoped to the RACE_SPECIFIC phase only (requirement: 'scoped to the race-specific phase only unless repository evidence shows an existing safer convention' -- no such convention was found, and this value already matched it). Independently structurally consistent with TEN_K_WORKOUT_PROGRESSION_V1's GOAL_PACE_REHEARSAL stage, which exists only under the RACE_SPECIFIC phaseProgression (never FOUNDATION/BUILD/TAPER) -- confirming goal-pace work is never scheduled in taper week and never in every week (stage minimumExposures=1, maximumExposures=2 within that phase only).",
            ["WorkoutDefinitionValidator", "WorkoutProgressionValidator"]));

        entries.Add(ExplicitDefault("AUD-421", "workout-definitions", DocumentTypes.WorkoutDefinition, "GOAL_PACE_TEN_K", 2, "$.complexityTier", "2",
            goalPaceV2File,
            "D13 RESOLUTION: approved as EXPLICIT_PRODUCT_DEFAULT, not CANONICAL_CONFIRMED -- no canonical source assigns this specific tier value (same caveat as the other workouts' complexityTier fields before Wave 3 removed the field entirely for them); GOAL_PACE_TEN_K was not part of that removal and is not being migrated to a components-only schemaVersion by this task, since D13's approved decision does not address the complexityTier taxonomy. Value carried forward unchanged from v1.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(ExplicitDefault("AUD-422", "vocabulary", DocumentTypes.WorkoutDefinition, "GOAL_PACE_TEN_K", 2, "$.allowedPrescriptionModes", "PACE_BASED",
            goalPaceV2File,
            "D13 RESOLUTION: approved as EXPLICIT_PRODUCT_DEFAULT. PACE_BASED is an appropriate, deliberate choice for a goal-pace rehearsal workout (prescribing effort relative to a target race pace); not migrated to the DISTANCE/MIXED vocabulary used by the other 4 workouts (WORKOUT-IMMUT-001) because that migration was fixture-driven for those specific keys and GOAL_PACE_TEN_K has no fixture evidence to migrate against. Left unmigrated per the same reasoning already recorded for v1 (AUD-249), now formally approved rather than merely unconfirmed.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(ExplicitDefault("AUD-423", "workout-definitions", DocumentTypes.WorkoutDefinition, "GOAL_PACE_TEN_K", 2, "$.components", "WARM_UP, MAIN_SET(GOAL_PACE), COOL_DOWN",
            goalPaceV2File,
            "D13 RESOLUTION: approved as EXPLICIT_PRODUCT_DEFAULT. This single continuous-block structure matches the approved concrete guidance ('short continuous goal-pace blocks inside a key session... after adequate warm-up', 'avoid large standalone goal-pace volume') -- deliberately conservative: one MAIN_SET at GOAL_PACE intensity, not an interval/repeat structure, so no additional numeric prescription (reps/duration/distance) was invented beyond the existing descriptive intensity label.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(Technical("AUD-424", "technical-metadata", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5, "$.phaseProgressions[RACE_SPECIFIC].stages[GOAL_PACE_REHEARSAL].workoutCandidates",
            "exact reference repointed to GOAL_PACE_TEN_K v2",
            "catalog/workout-progressions/ten-k-workout-progression.v5.json",
            ["WorkoutProgressionValidator", "TemplateCombinationValidator"],
            reason: "D13 immutable parent cascade: only the exact GOAL_PACE_TEN_K reference changed (v1 -> v2); stage structure, GOAL_FEASIBILITY_IN requirement, minimumExposures/maximumExposures, compressionBehavior=PROTECTED, and fallbackStageKey=CURRENT_FITNESS_SPECIFIC_REHEARSAL are byte-identical to v4 -- confirming the single-key-session, non-every-week, non-taper representation is unchanged by this task, only formally approved."));

        entries.Add(Technical("AUD-425", "technical-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 6, "$.workoutProgression",
            "TEN_K_WORKOUT_PROGRESSION_V1 v5",
            "catalog/templates/ten-k-master.v6.json",
            ["PlanTemplateValidator", "TemplateCombinationValidator"],
            reason: "D13 immutable parent cascade required because TEN_K_MASTER owns the exact WorkoutProgression reference. Template content (phases, eligibleWorkoutFamilies, coreCycle) otherwise unchanged from v5."));

        entries.Add(Technical("AUD-426", "technical-metadata", DocumentTypes.LevelModifier, "INTERMEDIATE_MODIFIER", 6, "$.eligibleWorkouts",
            "exact reference repointed to GOAL_PACE_TEN_K v2; other eligible workouts unchanged from v5",
            "catalog/level-modifiers/intermediate-modifier.v6.json",
            ["LevelModifierValidator", "TemplateCombinationValidator"],
            reason: "D13 immutable parent cascade: only the exact GOAL_PACE_TEN_K reference changed to preserve the candidate dependency graph after the leaf-artifact version change. progressionModifier reference (Wave 5 / D2) unchanged."));

        entries.Add(Technical("AUD-427", "technical-metadata", DocumentTypes.TemplateCombination, Combination, 10, "$.masterTemplate, $.layout, $.levelModifier, $.rulePack",
            "candidate root references TEN_K_MASTER v6, RUN_LAYOUT_4D v2, INTERMEDIATE_MODIFIER v6, APPSEL_RACE_PLAN_V1 v4",
            "catalog/combinations/ten-k-4d-intermediate.v10.json",
            ["TemplateCombinationValidator"],
            reason: "D13 candidate root (GOAL_PACE_TEN_K resolution only). Predecessor candidate TEN_K__4D__INTERMEDIATE v9 (D4) is preserved unchanged; no publish, retirement, or activation is applied in this task. This closes the last remaining domain-content decision in the catalog audit (D2, D3, D4, D13 all resolved as of this candidate)."));
    }

    /// <summary>
    /// Phase 4F.6 Pre-Implementation — Step C (V1 Pilot Workout Progression, Fixed Role Binding, and
    /// Governance Decisions): formalizes decision-record entries that were previously either scoped only
    /// to an immutable historical version (AUD-014/AUD-015 on WORKOUT_PROGRESSION v1) or never recorded
    /// anywhere (V1 EASY_SUPPORT/LONG_RUN/KEY_SESSION role-binding decisions, stage-semantics contract
    /// meaning, TAPER_SHARPEN identity retention, and the TAPER-phase evidence tension surfaced by Step B).
    /// New static ID block (500+), deliberately disjoint from every hardcoded AUD-0xx id (highest is
    /// AUD-427) and from the dynamic AUD-2xx block (highest is below 260) so it can never collide.
    /// Full decision rationale lives in PHASE4F_6_STEP_C_V1_PILOT_WORKOUT_AND_BINDING_DECISIONS.md — these
    /// entries are the machine-consulted governance record, not a duplicate of that document's prose.
    /// No numeric catalog value, workout candidate list, or structural field was changed by this pass.
    /// </summary>
    private static void AddStepCV1PilotBindingGovernanceEntries(List<DomainContentDecision> entries)
    {
        const string stepCDoc = "PHASE4F_6_STEP_C_V1_PILOT_WORKOUT_AND_BINDING_DECISIONS.md";
        const string progressionV5File = "catalog/workout-progressions/ten-k-workout-progression.v5.json";
        const string layoutV2File = "catalog/layouts/run-layout-4d.v2.json";

        entries.Add(ExplicitDefault("AUD-500", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "$.phaseProgressions[*].stages[*].minimumExposures / maximumExposures", "unchanged from v1/v4 (various, 1-6 range across all 7 stages)",
            progressionV5File,
            "STEP C D-C03 RESOLUTION: formally reclassifies all 14 exposure-count fields (7 stages x min/max) from PLACEHOLDER_UNCONFIRMED (AUD-014, recorded only against the immutable v1 artifact) to EXPLICIT_PRODUCT_DEFAULT on the current v5 artifact. Rationale per Phase 4F.6 Step B evidence mapping (phase4f6-step-b-training-science-evidence-mapping.json, decisions D03/D04/D10/D11/D17/D18/D24/D25/D30/D31/D38/D39/D44/D45): scientific evidence (Kenneally/Casado/Santos-Concejero 2018; Casado et al. 2022) supports repeated and progressively specific exposure to a training stimulus in principle, but does not determine the exact Appsel exposure numbers for any stage. No further scientific search is required before pilot implementation; these are accepted V1 product defaults. Numeric values themselves are unchanged from v1 — only the governance classification changed, and only on this current version.",
            ["WorkoutProgressionValidator"]));

        entries.Add(ExplicitDefault("AUD-501", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "$.phaseProgressions[*].stages[*].compressionBehavior / extensionBehavior", "unchanged from v1/v4 (COMPRESSIBLE/PROTECTED, EXTENDABLE/FIXED_EXPOSURE per stage)",
            progressionV5File,
            "STEP C D-C04 RESOLUTION: formally reclassifies the compression/extension behavior fields from PLACEHOLDER_UNCONFIRMED (AUD-015, recorded only against the immutable v1 artifact) to EXPLICIT_PRODUCT_DEFAULT on the current v5 artifact. Per Step B (decisions D05/D06/D12/D13/D19/D20/D26/D27/D32/D33/D40/D41/D46/D47): PROTECTED/FIXED_EXPOSURE on GOAL_PACE_REHEARSAL and TAPER_SHARPEN is directionally EVIDENCE_INFORMED (specificity-protection principle); all other COMPRESSIBLE/EXTENDABLE assignments are NOT_AN_EVIDENCE_QUESTION (scheduling policy). Does not add ExtensionPriority, StageDistributionBehavior, or any new tie-break field — those remain a later stage-scheduler contract task (4F.6A). Values themselves are unchanged from v1; only the governance classification changed, and only on this current version. Note: CURRENT_FITNESS_SPECIFIC_REHEARSAL's asymmetry against GOAL_PACE_REHEARSAL's PROTECTED status (Step B decision D42) is preserved here as an open Step-C-recorded observation, not resolved by this reclassification — see PHASE4F_6_STEP_C_V1_PILOT_WORKOUT_AND_BINDING_DECISIONS.md.",
            ["WorkoutProgressionValidator"]));

        entries.Add(Confirmed("AUD-502", "role-binding-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "$ (contract-level meaning of WorkoutProgressionStageDefinition, not a single field)", "governs KEY_SESSION progression intent only; does not populate the full weekly layout",
            stepCDoc,
            "STEP C D-C06 RESOLUTION (CANONICAL_CONFIRMED as an accepted Appsel domain interpretation, NOT a sports-science claim — evidenceBasis is NOT_AN_EVIDENCE_QUESTION): for the TEN_K/INTERMEDIATE/4D V1 pilot, WorkoutProgressionStageDefinition governs KEY_SESSION progression intent only. It does not directly assign workouts to EASY_SUPPORT or LONG_RUN, and it is not a full-week slot population mechanism, a public workout type, a personalized prescription, or a fixed calendar week. Supporting references: WorkoutProgressionStageDefinition.cs has no Role/SlotRole/StructuralRole field (confirmed by direct source read); Phase 4F.6 Step A.1 (phase4f6-step-a1-role-ownership-and-gap-clarification.json, A1-Q01/A1-Q04) found no role-to-workout binding mechanism anywhere; Step A.2 (phase4f6-step-a2-easy-support-coverage-and-blocker-classification.json) found the progression lacks full weekly role coverage (BUILD/RACE_SPECIFIC phases have zero EASY-family stages) and zero LONG_RUN-family stages exist anywhere in the progression.",
            ["WorkoutProgressionValidator"]));

        entries.Add(ExplicitDefault("AUD-503", "role-binding-governance", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 2,
            "EASY_SUPPORT (structural role) -> V1 fixed workout-identity binding (not a literal field on this artifact; a cross-artifact V1 pilot decision)", "EASY_STANDARD",
            layoutV2File,
            "STEP C D-C07 RESOLUTION [TEMPORARY_V1_SIMPLIFICATION]: for the TEN_K/INTERMEDIATE/4D V1 pilot, EASY_SUPPORT -> EASY_STANDARD. This is NOT a permanent architectural restriction — it limits the V1 pilot to one accepted canonical workout identity for EASY_SUPPORT while preserving future versioned expansion (e.g. EASY_SHAKEOUT/EASY_WITH_STRIDES, see activation risk TD-EASY-WORKOUT-REGISTRY-001). EASY_SUPPORT must not be claimed to always map to EASY_STANDARD in every future catalog version. Evidence basis is EVIDENCE_INFORMED (Step B: low-intensity dominance and easy running's appropriateness across all phases are evidence-backed generally; the exact identity binding itself is not a scientific question). No role-binding runtime service, schema, or artifact is implemented by this decision record.",
            ["RunLayoutValidator"]));

        entries.Add(ExplicitDefault("AUD-504", "role-binding-governance", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 2,
            "LONG_RUN (structural role) -> V1 fixed workout-identity binding (not a literal field on this artifact; a cross-artifact V1 pilot decision)", "LONG_RUN_STANDARD",
            layoutV2File,
            "STEP C D-C08 RESOLUTION [TEMPORARY_V1_SIMPLIFICATION]: for the TEN_K/INTERMEDIATE/4D V1 pilot, LONG_RUN -> LONG_RUN_STANDARD. This is NOT a permanent architectural restriction — future catalog versions may introduce long-run variants or selection policies without changing historical published-plan behavior. LONG_RUN_PROGRESSION (the fixture-evidenced, non-substitutable, more complex workout key documented in domain-wave1-schema-necessity-audit.md and ten-k-pilot-vocabulary-decisions.md) is explicitly NOT added by this decision. Evidence basis is EVIDENCE_INFORMED (Step B/Step A.1: long-run training generally is evidence-relevant, but the exact identity binding is not itself a scientific question). No role-binding runtime service, schema, or artifact is implemented by this decision record.",
            ["RunLayoutValidator"]));

        entries.Add(Confirmed("AUD-505", "role-binding-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "KEY_SESSION (structural role) -> resolution mechanism (not a literal field; a cross-artifact V1 pilot decision)", "stage-controlled workout candidate resolution (distinct from EASY_SUPPORT/LONG_RUN's fixed V1 defaults)",
            stepCDoc,
            "STEP C D-C09 RESOLUTION (CANONICAL_CONFIRMED; evidenceBasis NOT_AN_EVIDENCE_QUESTION — architectural, not a sports-science claim): for the TEN_K/INTERMEDIATE/4D V1 pilot, KEY_SESSION resolves via stage-controlled workout candidate resolution (the 5 progression-controlled stages targeting QUALITY/EASY-family workouts), remaining distinct from EASY_SUPPORT and LONG_RUN's fixed V1 defaults (AUD-503/AUD-504). The exact stage-scheduler and candidate-resolution algorithm remain a later implementation task (Phase 4F.6A) and are not implemented by this decision record.",
            ["WorkoutProgressionValidator"]));

        entries.Add(Technical("AUD-506", "phase-metadata", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 6, "$.phases[TAPER].preferredWeeks (traceability addendum to AUD-003's existing CanonicalConfirmed value; classification NOT changed)",
            "1 (unchanged)",
            stepCDoc,
            ["PlanTemplateValidator"],
            reason: "STEP C D-C05 evidence-tension traceability note (does not reopen or reclassify AUD-003, which remains CanonicalConfirmed against the brief and Golden Fixture v3). Phase 4F.6 Step B (phase4f6-step-b-training-science-evidence-mapping.json, decision D51) found: tapering itself is EVIDENCE_BACKED (Bosquet et al. 2007 meta-analysis); Bosquet found ~2 weeks the most efficient taper window in the analyzed competitive-athlete evidence; this does not directly prove a 10K/intermediate/12-week Appsel plan must use two taper weeks (population is broad competitive-athlete/multi-sport, not 10K-specific — DISTANCE_EXTRAPOLATION and ELITE_TO_INTERMEDIATE_EXTRAPOLATION both required). The current 1-week value remains an accepted V1 pilot product default (see AUD-003); this tension is recorded here for traceability and must not be treated as a contradiction that automatically changes the catalog. No phase week count was changed by this entry."));

        entries.Add(Confirmed("AUD-507", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "$.phaseProgressions[TAPER].stages[TAPER_SHARPEN]", "stageKey=TAPER_SHARPEN, workoutCandidates=[EASY_STANDARD v4] (unchanged)",
            stepCDoc,
            "STEP C D-C11 RESOLUTION: retains TAPER_SHARPEN's stageKey and EASY_STANDARD workout-identity binding unchanged; no new taper workout key is introduced. Per Step B's central finding (decision D43): TAPER_SHARPEN's name implies an intensity-maintaining purpose, but its bound candidate is a plain EASY-family workout, which by itself does not fulfill Bosquet et al. (2007)'s finding that effective tapers maintain intensity while reducing volume. This decision accepts that gap for V1 and assigns its resolution to Phase 4F.7: the sharpening effect must be produced through a taper-specific prescription modifier (reducing total workload while preserving an appropriate intensity stimulus, using only components/prescription modes already allowed by EASY_STANDARD and the future prescription contract — not a generic 'faster easy pace', and not defined here). Stage context must be available to Phase 4F.7 prescription generation so TAPER_SHARPEN and ordinary EASY_STANDARD sessions do not receive identical prescriptions by accident. Implementation owner: Phase 4F.7. Evidence basis: EVIDENCE_INFORMED.",
            ["WorkoutProgressionValidator"]));

        // ===== Phase 4F.6 Pre-Implementation Step C.1 (append-only clarification of AUD-507/D-C11) =====
        // AUD-507 is left completely unchanged above -- this is a new, additive entry only. It does not
        // alter TAPER_SHARPEN's stageKey or workout-identity binding (both remain exactly as AUD-507
        // states) and does not reopen D-C11. It exists solely because AUD-507, on strict re-reading against
        // the CONCRETE_PRESCRIPTION_DIRECTIVE checklist, states the required prescription EFFECT (reduced
        // workload + preserved intensity + allowed-components-only) explicitly, but expresses the
        // "must be materially distinguishable from ordinary EASY_STANDARD" and "must not be merely a naive
        // pace increase" requirements only by entailment/caution rather than as freestanding, unambiguous
        // sentences -- see PHASE4F_6_STEP_C1_TAPER_SHARPEN_AND_RUNTIME_BOUNDARY_CLOSURE.md section 2 for
        // the full CONCRETE_PRESCRIPTION_DIRECTIVE-checklist assessment that produced this entry.
        entries.Add(Confirmed("AUD-508", "workout-progression", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "$.phaseProgressions[TAPER].stages[TAPER_SHARPEN] (append-only clarification of AUD-507 / D-C11 — does not change stageKey or workout-identity binding)", "stageKey=TAPER_SHARPEN, workoutCandidates=[EASY_STANDARD v4] (unchanged, same as AUD-507)",
            "PHASE4F_6_STEP_C1_TAPER_SHARPEN_AND_RUNTIME_BOUNDARY_CLOSURE.md",
            "STEP C.1 CLARIFICATION OF AUD-507 / D-C11 (title: 'TAPER_SHARPEN prescription directive concretized'; append-only — AUD-507 is not edited, deleted, or superseded, only completed): Phase 4F.7's prescription for TAPER_SHARPEN sessions MUST be materially distinguishable from an ordinary (non-taper) EASY_STANDARD session's prescription — not merely 'not identical by accident' (AUD-507's own phrasing), but affirmatively required to differ in a way a reviewer could observe (e.g. in reduced total distance/duration/volume and/or a distinguishable intensity-zone signature), while still using only components and prescription modes already allowed by EASY_STANDARD (schemaVersion 3, allowedPrescriptionModes=[DISTANCE], allowedDistanceAccountingModes=[EXACT_SESSION_TOTAL] as of v4) and the future prescription contract. Explicitly prohibited implementations: (a) uniformly increasing pace across the entire session ('indiscriminately faster'); (b) a trivial/negligible volume trim with no distinguishable intensity treatment, which would technically satisfy 'reduced workload' while failing the material-distinguishability requirement; (c) introducing a new workout key automatically to sidestep the modifier requirement (AUD-507/D-C11 already forecloses this); (d) deferring the training intent for later review without an enforceable effect (this would be DELEGATION_ONLY, not a closed decision, and is exactly what this entry closes). Stage context (TAPER_SHARPEN's own stageKey, preserved from whatever future stage-scheduler output Phase 4F.6A produces) MUST be available to Phase 4F.7 prescription generation as an affirmative input, not merely as a non-loss guarantee — Phase 4F.7 cannot apply this modifier at all if the assigned progression stageKey is not threaded through Phase 4F.6A/4F.6B's output. Evidence basis: EVIDENCE_INFORMED (Bosquet et al. 2007, per Step B decision D43/D51 — reduce volume, preserve intensity; does not determine exact segments/pace/repetition/recovery/distance/duration/modifier schema, all of which remain Phase 4F.7 design details). Decision status: CANONICAL_CONFIRMED. Implementation owner: Phase 4F.7 (prescription); stage-context propagation owner: Phase 4F.6A (see Step C.1 responsibility matrix). No catalog value, stageKey, or workout candidate changed by this entry.",
            ["WorkoutProgressionValidator"]));

        entries.Add(Confirmed("AUD-509", "volume-governance", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 3,
            "$.entries[TEN_K/INTERMEDIATE/4].minKmPerWeek (runtime semantic boundary)", "30 is a typical peak-band lower bound, not a Week 1 floor",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: the prior runtime interpreted the 30km lower band as a starting-week floor. Corrected canonical behavior treats PEAK_VOLUME_BANDS_V1 as a typical peak-volume band only; valid readiness weekly-volume anchors below 30km are preserved and progressed toward a reachable peak instead of being clamped to 30km at Week 1. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: CANONICAL_CONFIRMED; source files: peak-volume-bands.v3.json plus golden fixture weeklyVolumeAnchorKm=24 and resolvedPeakKm=38. No catalog artifact value changed.",
            ["PeakVolumeBandPolicyValidator", "TemplateCombinationValidator"]));

        entries.Add(Confirmed("AUD-510", "volume-governance", DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", 3,
            "$.entries[TEN_K/INTERMEDIATE/4].maxKmPerWeek (runtime reachable-peak semantic boundary)", "42 is an upper constraint, not an unconditional selected peak",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: the prior runtime selected 42km unconditionally. Corrected canonical behavior computes a reachable peak from the valid starting volume and cycle length, then constrains that result to the typical peak band when applicable. The golden fixture demonstrates a 24km anchor resolving to a 38km peak, not 42km. Evidence basis: EVIDENCE_INFORMED; decision status: CANONICAL_CONFIRMED. No catalog artifact value changed.",
            ["PeakVolumeBandPolicyValidator", "TemplateCombinationValidator"]));

        entries.Add(Confirmed("AUD-511", "volume-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "runtime weekly-volume anchor selection (not a literal catalog field)", "valid positive RecentWeeklyVolumeKm remains the starting-volume anchor",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: valid positive readiness input is preserved as the Week 1 volume anchor and is not raised to the peak-band lower bound. This follows Phase 4F.7A's normalized-readiness semantics and the golden fixture capacitySnapshot.weeklyVolumeAnchorKm=24. Evidence basis: EVIDENCE_INFORMED; decision status: CANONICAL_CONFIRMED. Invalid readiness fails closed; missing/explicit-zero Intermediate fallback remains unresolved pending a canonical source.",
            ["PublishReadinessValidator"]));

        entries.Add(Confirmed("AUD-512", "volume-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "runtime invalid-readiness handling (not a literal catalog field)", "typed fail-closed exception",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: invalid readiness inputs must not silently fall back to catalog peak-band values. Corrected runtime behavior throws explicit typed failures for invalid volume/readiness state and for missing canonical rule sources. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: CANONICAL_CONFIRMED; aligns with accepted Phase 4E/4F fail-closed governance. No catalog artifact value changed.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-513", "volume-governance", DocumentTypes.PlanTemplate, "TEN_K_MASTER", 6,
            "runtime taper volume multiplier (not a literal catalog field)", "0.53 multiplier / 47% reduction from previous week",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: the prior 0.65 multiplier is outside the accepted 41%-60% reduction envelope. Corrected V1 runtime default is 0.53, matching the golden fixture's 38km to 20km taper transition after rounding. Evidence basis: EVIDENCE_INFORMED; decision status: EXPLICIT_PRODUCT_DEFAULT. No catalog artifact value changed.",
            ["PlanTemplateValidator"]));

        entries.Add(ExplicitDefault("AUD-514", "volume-governance", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 2,
            "runtime four-day long-run weekly-share rule (not a literal catalog field)", "preferred 30%-36%; selected 33%; hard cap 40%",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: the prior 20%-35% range was not the accepted four-day long-run target share. Corrected V1 runtime default uses preferred share 30%-36%, selected share 33%, and hard cap 40%; compatibility classes from readiness are confidence classifications and not target prescriptions. Evidence basis: PRODUCT_PRACTICE_INFORMED; decision status: EXPLICIT_PRODUCT_DEFAULT. No catalog artifact value changed.",
            ["RunLayoutValidator"]));

        entries.Add(Confirmed("AUD-515", "volume-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "runtime long-run compatibility semantics (not a literal catalog field)", "compatibility class only; not a target-share source",
            "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md",
            "PHASE 4F.7B.1 correction: Phase 4F.7A compatibility state such as BALANCED/ACCEPTABLE/HIGH_SHARE/INCONSISTENT is a readiness/confidence classification used to prevent unsafe direct use of inconsistent inputs, not the source of a prescribed long-run weekly share. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: CANONICAL_CONFIRMED. No catalog artifact value changed.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-516", "volume-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_MISSING_READINESS_STARTING_VOLUME_POLICY v1: missing RecentWeeklyVolumeKm", "16km Week 1 starting-volume default",
            "PHASE4F_7B2_MISSING_ZERO_READINESS_DECISION.md",
            "PHASE 4F.7B.2 product decision: repository Doc13 volume sections remain absent, so this is not claimed as canonical-confirmed. For TEN_K/INTERMEDIATE/4D V1, missing recent weekly-volume evidence uses a conservative Intermediate starting-volume default of 16km, preserving INTERMEDIATE identity and avoiding the 30km peak-band minimum. Evidence basis: PRODUCT_PRACTICE_INFORMED; decision status: EXPLICIT_PRODUCT_DEFAULT; affected phase: 4F.7B.2; numeric value changed from fail-closed/no numeric output to 16km; correction closes the missing-input blocker without editing catalog artifact values.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-517", "volume-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_MISSING_READINESS_STARTING_VOLUME_POLICY v1: explicit zero RecentWeeklyVolumeKm", "12km Week 1 no-recent-running default",
            "PHASE4F_7B2_MISSING_ZERO_READINESS_DECISION.md",
            "PHASE 4F.7B.2 product decision: explicit zero remains distinct from missing evidence and uses a lower no-recent-running V1 default of 12km, preserving INTERMEDIATE identity while reflecting reduced readiness. The 30km peak-band minimum is not used. Evidence basis: PRODUCT_PRACTICE_INFORMED; decision status: EXPLICIT_PRODUCT_DEFAULT; affected phase: 4F.7B.2; numeric value changed from fail-closed/no numeric output to 12km; no catalog artifact value changed.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-518", "volume-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_MISSING_READINESS_STARTING_VOLUME_POLICY v1: candidate and cycle behavior", "generation continues for 8/12/14 weeks with below-typical reachable peak allowed",
            "PHASE4F_7B2_MISSING_ZERO_READINESS_DECISION.md",
            "PHASE 4F.7B.2 product decision: the active TEN_K/INTERMEDIATE/4D candidate remains selected for missing and explicit-zero weekly-volume states; the user is not reclassified as BEGINNER. For supported 8/12/14 week cycles, the selected starts (16km missing, 12km zero) preserve four-session feasibility, non-zero residual volume after the long run, the 30%-36% preferred long-run share, the 40% hard cap, and reachable-peak semantics. Evidence basis: PRODUCT_PRACTICE_INFORMED; decision status: EXPLICIT_PRODUCT_DEFAULT; affected phase: 4F.7B.2; no catalog artifact value changed.",
            ["PublishReadinessValidator", "TemplateCombinationValidator"]));

        entries.Add(ExplicitDefault("AUD-519", "session-prescription-governance", DocumentTypes.RunLayout, "RUN_LAYOUT_4D", 2,
            "V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY v1", "residual volume -> KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT; easy minimum 1.5km, key minimum 3km",
            "PHASE4F_7C_PACE_SOURCE_AND_SESSION_PRESCRIPTION.md",
            "PHASE 4F.7C product/technical decision: after the 4F.7B long-run distance is reserved, residual volume is deterministically allocated to the bound KEY_SESSION and two EASY_SUPPORT sessions without changing workout identity. This is not a scientific claim; it is deterministic allocation arithmetic needed for dark session prescription. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: EXPLICIT_PRODUCT_DEFAULT; no catalog content changed.",
            ["RunLayoutValidator"]));

        entries.Add(ExplicitDefault("AUD-520", "session-prescription-governance", DocumentTypes.WorkoutDefinition, "FARTLEK", 4,
            "V1_COMPONENT_RANGE_SELECTION_POLICY", "deterministic component distance split within allocated session volume",
            "PHASE4F_7C_PACE_SOURCE_AND_SESSION_PRESCRIPTION.md",
            "PHASE 4F.7C decision: workout definitions provide component order and intensity descriptors but no dose-selection formula. 4F.7C therefore records one deterministic V1 component-selection policy that preserves catalog component order, keeps warm-up/cool-down where present, and fits the allocated session envelope. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: EXPLICIT_PRODUCT_DEFAULT; no catalog content changed.",
            ["WorkoutDefinitionValidator"]));

        entries.Add(ExplicitDefault("AUD-521", "session-prescription-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "effort-only fallback for unsupported numeric pace derivations", "EASY/LONG_RUN/FARTLEK/THRESHOLD use effort-only unless a supported exact pace source exists",
            "PHASE4F_7C_PACE_SOURCE_AND_SESSION_PRESCRIPTION.md",
            "PHASE 4F.7C decision: Target goal pace is only active for GOAL_PACE_TEN_K when goal feasibility permits it. Easy, long-run, fartlek, and threshold sessions do not derive invented numerical paces from preferred pace, recent race, target goal, or ESTIMATED. They use effort-only prescriptions with unresolved numeric pace provenance. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: EXPLICIT_PRODUCT_DEFAULT; no catalog content changed.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-522", "session-prescription-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "TAPER_SHARPEN baseline-pending status", "BASELINE_PRESCRIBED_SHARPENING_PENDING",
            "PHASE4F_7C_PACE_SOURCE_AND_SESSION_PRESCRIPTION.md",
            "PHASE 4F.7C boundary decision: the TAPER_SHARPEN KEY_SESSION keeps EASY_STANDARD identity and receives only a baseline EASY prescription in 4F.7C, with explicit pending status for the Phase 4F.7D sharpening overlay. No strides, intensity overlay, or workout-key substitution is introduced. Evidence basis: NOT_AN_EVIDENCE_QUESTION; decision status: EXPLICIT_PRODUCT_DEFAULT; no catalog content changed.",
            ["WorkoutProgressionValidator"]));

        entries.Add(ExplicitDefault("AUD-523", "session-prescription-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1: concrete final prescription", "TAPER_SHARPEN completed as EASY_STANDARD with additive runtime components",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D implementation decision: the final TAPER_SHARPEN prescription preserves PhaseKey=TAPER, ProgressionStageKey=TAPER_SHARPEN, StructuralRole=KEY_SESSION, and WorkoutDefinitionKey=EASY_STANDARD while completing the pending 4F.7C baseline state. Runtime effect: baseline SESSION_TOTAL is replaced only in the internal prescribed plan by componentized additive runtime prescription content. Catalog content changed=false. Closes AUD-508's implementation requirement when paired with AUD-524..AUD-530.",
            ["WorkoutProgressionValidator", "PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-524", "session-prescription-governance", DocumentTypes.WorkoutDefinition, "EASY_STANDARD", 4,
            "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1: component type and placement", "EASY_BASELINE -> CONTROLLED_SHARPENING -> EASY_RECOVERY",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D product-default decision: EASY_STANDARD v4 has no native catalog components, but the 4F.7C internal CatalogPrescriptionSegment contract can legally represent stage-specific runtime components without a new workout identity. Runtime effect: controlled sharpening is placed after an easy baseline and before easy recovery. Catalog content changed=false. Closes the AUD-508 material-distinction requirement.",
            ["WorkoutDefinitionValidator", "PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-525", "session-prescription-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1: dose-selection rule", "20% rounded to 0.5km, clamped 0.5-1.5km; recovery 0.5km; easy receives remainder",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D product-default decision: the sharpening dose is deterministic, materially smaller than the session, and bounded so it cannot dominate a reduced taper key session. Runtime effect: no weekly volume, long-run distance, or key-session distance changes. Catalog content changed=false. Supports AUD-508 closure.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-526", "session-prescription-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1: effort and pace behavior", "effort-only EASY / CONTROLLED_FAST_RELAXED / EASY_RECOVERY; no target pace borrowed",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D product-default decision: no ESTIMATED pace producer or general numeric easy/sharpening model exists. Runtime effect: numeric pace remains unresolved by design, TargetGoalDerived is not used, and the whole run is not accelerated. Catalog content changed=false. Supports AUD-508 closure.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-527", "session-prescription-governance", DocumentTypes.WorkoutDefinition, "EASY_STANDARD", 4,
            "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1: distance-accounting behavior", "ExactSessionTotal across all runtime components",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D technical/product decision: all TAPER_SHARPEN runtime components count toward the already-assigned 4F.7C taper key-session distance, and their rounded sum must reconcile to that assigned distance. Runtime effect: no hidden or unaccounted volume. Catalog content changed=false. Supports AUD-508 closure.",
            ["WorkoutDefinitionValidator", "PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-528", "session-prescription-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1: low-volume feasibility", "minimum 3km taper key-session; typed infeasibility below minimum",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D product-default decision: supported 8/12/14 week plans for valid-positive and missing weekly-volume paths remain feasible, and 12/14 week explicit-zero paths remain feasible. The 8-week explicit-zero path remains blocked before 4F.7D by existing 4F.7C allocation minimums (5.5km taper residual versus 6.0km required key/easy minimum). An assigned taper key-session below 3km fails closed through a typed exception rather than increasing volume or silently omitting sharpening. Catalog content changed=false. Supports AUD-508 closure for reachable final prescribed plans.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-529", "session-prescription-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "CatalogFinalPrescribedPlanValidator", "complete internal prescribed-plan validation required before dark pipeline stop",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D technical decision: final internal prescribed plans must validate structure, session counts, dates/identity, weekly totals, long-run values, component accounting, pace-source use, taper behavior, and provenance before the dark pipeline stops. Runtime effect: invalid complete prescriptions fail closed. Catalog content changed=false. Supports AUD-508 closure.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-530", "session-prescription-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "TAPER_SHARPEN pending-state removal", "BASELINE_PRESCRIBED_SHARPENING_PENDING must not remain in final prescribed plan",
            "PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md",
            "PHASE 4F.7D implementation decision: BaselinePrescribedSharpeningPending remains a 4F.7C intermediate status only; 4F.7D replaces it with FinalPrescriptionComplete in the final internal prescribed plan and fails closed if any pending state remains. Runtime effect: no future overlay is required before public materialization begins. Catalog content changed=false. Closes AUD-508's pending-overlay requirement.",
            ["WorkoutProgressionValidator", "PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-531", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "CATALOG_PUBLIC_PREVIEW_MATERIALIZER v1", "final prescribed plan -> GeneratedCatalogPlanPayload",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: the fully validated 4F.7D prescribed plan is projected into the existing GeneratedCatalogPlanPayload preview contract after final prescribed-plan validation. Runtime effect: supported internal catalog dry-run previews now carry a non-null generated payload; live routing remains closed and catalog content changed=false. Public compatibility impact: additive population of an existing nullable field; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-532", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_CATALOG_PUBLIC_WORKOUT_TYPE_MAPPING_POLICY v1", "explicit workout key/role/stage -> GeneratedCatalogWorkoutType",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: public workout type mapping is deterministic and keyed by exact workout identity, structural role, and stage context; it does not depend on display strings, list order, or family guessing. Runtime effect: EASY_STANDARD, LONG_RUN_STANDARD, FARTLEK, THRESHOLD_TEMPO, GOAL_PACE_TEN_K, and TAPER_SHARPEN are representable in the existing enum. Public compatibility impact: no enum change; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-533", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "public effort-only pace representation", "GeneratedCatalogPaceType.EffortOnly with null numeric pace",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: effort-only prescriptions materialize as structured EffortOnly pace with effort label and no numeric target/range. Runtime effect: no ESTIMATED pace, preferred pace, or target goal pace is fabricated. Public compatibility impact: uses existing pace contract; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-534", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "public duration semantics", "prescribed/derived/estimated/unresolved preserved without zero placeholders in payload",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: estimated durations are mapped only when available; effort-only unresolved durations remain null in GeneratedCatalogPlanPayload. Runtime effect: estimated goal-pace duration is not collapsed into prescribed duration and unresolved duration is not represented by zero in the payload. Public compatibility impact: uses existing nullable schedule fields; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-535", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_CATALOG_PUBLIC_SEGMENT_MAPPING_POLICY v1", "ordered internal prescription segments -> public segment payloads",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: internal segment order and distance accounting are projected to public segment payloads with explicit mappings for SESSION_TOTAL, WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN, EASY_BASELINE, CONTROLLED_SHARPENING, and EASY_RECOVERY. Runtime effect: TAPER_SHARPEN and quality sessions are not flattened. Public compatibility impact: uses existing segment contract; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-536", "public-preview-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "TAPER_SHARPEN public representation", "EASY_STANDARD public type plus stage provenance and ordered sharpening components",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: TAPER_SHARPEN keeps public compatibility with the existing Easy workout type while preserving stage identity in provenance and EASY_BASELINE/CONTROLLED_SHARPENING/EASY_RECOVERY segment detail. Runtime effect: clients can distinguish taper sharpen from ordinary easy support without a new workout definition. Public compatibility impact: no enum change; schema changed=false; dark-only=true.",
            ["WorkoutProgressionValidator", "PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-537", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "snapshot hash includes generated payload", "ContentHash changes when material prescription payload changes",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 technical decision: CatalogPreviewSnapshotBuilder and verifier include GeneratedPreviewPlanPayload in canonical hash content. Runtime effect: equivalent payloads verify deterministically and material prescription changes alter the hash. Public compatibility impact: applies only to newly generated previews; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-538", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "confirm remains disabled with non-null payload", "valid generated payload still throws materialization-not-implemented on confirm",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 boundary decision: non-null catalog preview payload is preview-only; CatalogPlanConfirmationService still validates and rejects structurally valid generated payloads before any TrainingPlan/TrainingWeek/TrainingDay persistence. Public compatibility impact: no confirm activation; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-539", "public-preview-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "8-week explicit-zero unsupported propagation", "typed failure and no generated payload",
            "PHASE4F_8_1_CATALOG_PUBLIC_PREVIEW_MATERIALIZATION.md",
            "PHASE 4F.8.1 boundary decision: the known 8-week explicit-zero weekly-volume path remains unsupported for public materialization and fails closed with a typed preview-generation failure. Runtime effect: no partial or misleading GeneratedCatalogPlanPayload is returned and weekly volume is not silently raised. Public compatibility impact: existing error response path; schema changed=false; dark-only=true.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-540", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "V1_LIVE_CATALOG_PILOT_ROUTING_POLICY v1: exact pilot identity", "Race/TenK/RunningRegularly/4D plus supported cycle length",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 technical decision: only the typed TEN_K/RACE/RunningRegularly/4D request shape with a valid supported 8-14 week race cycle can enter the live catalog routing boundary. Runtime effect: non-pilot requests use the established legacy route and do not probe catalog. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-541", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "PUBLISHED plus activation dual gate", "candidate lifecycle cannot be overridden by activation",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 governance decision: CATALOG_LIVE requires candidate lifecycle status PUBLISHED and CatalogLivePilot.Enabled=true. DRAFT plus enabled remains non-live. Runtime effect: lifecycle is authoritative and activation is only a second gate after publication. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-542", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "default-disabled live pilot activation", "CatalogLivePilot:Enabled defaults false",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 rollout decision: live pilot routing uses one explicit configuration option and no production default enables it. Runtime effect: the current repository remains non-live. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-543", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "DRAFT candidate behavior", "catalog-supported but not published routes to approved legacy boundary",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 boundary decision: the real TEN_K__4D__INTERMEDIATE v10 DRAFT candidate never serves catalog output to real users. Runtime effect: pilot-shaped live requests hit the existing exact legacy template path and preserve typed template-not-available failure when no exact template exists. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-544", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "out-of-scope request behavior", "non-pilot shapes remain legacy only",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 fallback decision: 5K, half marathon, marathon, habit, non-RunningRegularly, non-4D, unsupported cycles, and custom unsupported catalog requests do not silently execute catalog. Runtime effect: out-of-scope supported legacy behavior is preserved and unsupported catalog/safety failures fail typed. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-545", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "legacy fallback permission matrix", "availability fallback allowed; safety/data fallback prohibited",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 routing decision: fallback is permitted for non-pilot requests, not-published pilot availability, and activation-disabled availability; fallback is prohibited for invalid requests, unsupported cycles, readiness infeasibility, artifact inconsistency, snapshot/hash failure, payload validation failure, and confirm materialization. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-546", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "route provenance in preview snapshot", "LEGACY_SQL response JSON versus CATALOG snapshot GenerationSource",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 technical decision: catalog previews continue to persist CatalogPreviewSnapshot.GenerationSource=CATALOG and legacy previews continue using the legacy response JSON; confirmation dispatch uses stored provenance rather than recomputing routing. Snapshot/hash schema changed=false. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-547", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "catalog confirmation remains disabled", "catalog preview cannot persist TrainingPlan/Week/Day",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 confirmation boundary decision: catalog preview confirmation remains fail-closed through CatalogPreviewMaterializationNotImplementedException after snapshot validation; no TrainingPlan, TrainingWeek, or TrainingDay writes are introduced. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-548", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "8-week explicit-zero no-fallback route", "known generation-infeasible request rejected before generator/legacy fallback",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 safety decision: the 8-week explicit-zero weekly-volume pilot path is classified as CATALOG_GENERATION_INFEASIBLE with fallback prohibited. Runtime effect: no legacy detour, no snapshot, no payload, and no silent weekly-volume increase. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-549", "live-routing-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "live route observability", "structured policy/version/route/lifecycle/activation/reason fields",
            "PHASE4F_8_2_SCOPED_LIVE_PILOT_ROUTING.md",
            "PHASE 4F.8.2 observability decision: live routing logs sanitized structured route decisions without free-form onboarding text, payloads, secrets, or normal stack traces. Runtime effect: route provenance is inspectable while privacy boundaries are preserved. Candidate status changed=false; activation changed=false.",
            ["PublishReadinessValidator"]));

        entries.Add(ExplicitDefault("AUD-550", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "catalog confirm source of truth", "stored hash-verified preview snapshot only",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: catalog confirmation persists the exact stored CatalogPreviewSnapshot and GeneratedCatalogPlanPayload after hash verification. Runtime behavior: no route selection, resolver orchestration, stage allocation, workout binding, volume allocation, date assignment, or prescription generation runs during confirm. Schema impact=true; transaction impact=all persisted rows share one confirm transaction; legacy compatibility=unchanged; migration added=true.",
            ["CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-551", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "TrainingPlan provenance mapping", "candidate, dependency, preview, hash, materializer, confirmed timestamp",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: TrainingPlan stores catalog candidate key/version/status, artifact dependency versions, SourcePreviewId, CatalogPreviewContentHash, CatalogMaterializerVersion, GenerationSource=CATALOG, and CatalogConfirmedAtUtc. Runtime behavior: historical plan explanation does not require current catalog files. Schema impact=true; transaction impact=included in confirm transaction; legacy compatibility=nullable fields; migration added=true.",
            ["CatalogPlanConfirmationService", "AppDbContext"]));

        entries.Add(ExplicitDefault("AUD-552", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "phase and progression-stage persistence", "CatalogPhaseKey separate from CatalogProgressionStageKey",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: TrainingDay.CatalogPhaseKey stores phase provenance and TrainingDay.CatalogProgressionStageKey stores fine-grained progression stage provenance when distinct. Existing CatalogStageKey is retained as legacy/deprecated compatibility data and is not repurposed. Runtime behavior: TAPER and TAPER_SHARPEN remain distinct. Schema impact=true; transaction impact=included in day writes; legacy compatibility=old field retained; migration added=true.",
            ["TrainingDay", "CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-553", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "structured prescription persistence", "CATALOG_SESSION_PRESCRIPTION_SNAPSHOT v1 JSON",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: catalog sessions persist a versioned JSON prescription snapshot with deterministic snake_case fields, ordered segments, pace object, duration semantics, and day provenance. Runtime behavior: effort-only, target pace, pace range, unresolved numeric pace, estimated duration, and component order are not flattened into lossy display text. Schema impact=true; transaction impact=included in day writes; legacy compatibility=nullable field; migration added=true.",
            ["CatalogPlanConfirmationService", "CatalogPersistedPlanValidator"]));

        entries.Add(ExplicitDefault("AUD-554", "catalog-confirmation-governance", DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", 5,
            "TAPER_SHARPEN persistence", "phase TAPER, stage TAPER_SHARPEN, role KEY_SESSION, workout EASY_STANDARD",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: confirmed taper sharpening sessions preserve CatalogPhaseKey=TAPER, CatalogProgressionStageKey=TAPER_SHARPEN, CatalogStructuralRole=KEY_SESSION, CatalogWorkoutDefinitionKey=EASY_STANDARD, and ordered EASY_BASELINE/CONTROLLED_SHARPENING/EASY_RECOVERY prescription components. Runtime behavior: public workout type may remain Easy but the persisted row is distinguishable from ordinary easy support. Schema impact=true; transaction impact=included in day writes; legacy compatibility=additive; migration added=true.",
            ["CatalogPlanConfirmationService", "CatalogPersistedPlanValidator"]));

        entries.Add(ExplicitDefault("AUD-555", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "idempotent catalog confirmation", "same preview returns existing plan",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: repeated confirmation of an already confirmed preview returns the existing TrainingPlan and creates no duplicate weeks or days. Runtime behavior: PlanPreview.ConfirmedPlanId is the application idempotency anchor and TrainingPlans.SourcePreviewId has a unique filtered index. Schema impact=true; transaction impact=idempotency link written with plan; legacy compatibility=unchanged; migration added=true.",
            ["CatalogPlanConfirmationService", "AppDbContext"]));

        entries.Add(ExplicitDefault("AUD-556", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "concurrent catalog confirmation behavior", "at most one TrainingPlan per SourcePreviewId",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: simultaneous confirmation attempts must not create duplicate plans for the same preview. Runtime behavior is protected by the SourcePreviewId uniqueness invariant and typed persistence failure handling. Schema impact=true; transaction impact=database uniqueness participates in transaction; legacy compatibility=nullable filtered index; migration added=true.",
            ["AppDbContext", "CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-557", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "atomic confirmation transaction", "plan, weeks, days, event, preview link all-or-none",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: relational catalog confirmation persists TrainingPlan, all TrainingWeeks, all TrainingDays, PlanEvent, and PlanPreview.ConfirmedPlanId in one transaction. Runtime behavior: failed persistence rolls back partial rows and leaves preview unconsumed. Schema impact=false beyond Phase 4F.9 columns; transaction impact=explicit transaction; legacy compatibility=unchanged; migration added=true.",
            ["CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-558", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "preview consumption lifecycle", "ConfirmedPlanId set only after successful confirmation",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: successful catalog confirmation marks the preview confirmed/consumed by setting ConfirmedPlanId while preserving the snapshot for audit; failed confirmation writes nothing and leaves the preview unconsumed. Runtime behavior: expiration blocks unconfirmed previews but does not require regenerating an already-confirmed plan. Schema impact=false; transaction impact=preview link written in confirm transaction; legacy compatibility=unchanged; migration added=false.",
            ["CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-559", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "legacy confirmation isolation", "catalog confirm never calls legacy generation",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: legacy confirmation behavior remains isolated and catalog confirmation does not invoke the legacy SQL generation path. Runtime behavior: stored preview provenance controls dispatch and catalog failures are not converted to legacy materialization. Schema impact=false; transaction impact=none for legacy path; legacy compatibility=unchanged; migration added=false.",
            ["PlanServices", "CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-560", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "publication-independent confirm for existing valid previews", "DRAFT candidate allowed for stored valid catalog test previews",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: confirmation of an already-created, valid catalog preview is independent from candidate publication and production activation. Runtime behavior: repository tests may confirm DRAFT candidate snapshots; this phase does not publish v10, add publication ledger entries, or enable production activation. Schema impact=false; transaction impact=none beyond confirm; legacy compatibility=unchanged; migration added=false.",
            ["CatalogPlanConfirmationService"]));

        entries.Add(ExplicitDefault("AUD-561", "catalog-confirmation-governance", DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", 4,
            "unsupported 8-week explicit-zero defensive guard", "invalid handcrafted snapshot fails before persistence",
            "PHASE4F_9_CATALOG_CONFIRMATION_AND_PERSISTENCE.md",
            "PHASE 4F.9 decision: the known unsupported 8-week cycle plus explicit-zero recent-volume path remains unsolved and must not be materialized through a handcrafted payload. Runtime behavior: confirmation rejects that combination with a typed persistence-contract failure before mutation. Schema impact=false; transaction impact=no writes on failure; legacy compatibility=unchanged; migration added=false.",
            ["CatalogPlanConfirmationService"]));
    }

    private static void AddWorkoutDefinitionEntries(List<DomainContentDecision> entries)
    {
        // WORKOUT-IMMUT-001 remediation (see artifacts/audits/published-workout-immutability-remediation.md):
        // each of these 4 keys now has two immutable versions.
        //   v1 = RESTORED to its exact original, pre-reconciliation historical content — legacy
        //        (PACE_BASED/EFFORT_BASED) PrescriptionMode, no AllowedDistanceAccountingModes field at
        //        all. This was its true published state before an earlier pass mistakenly edited it in
        //        place instead of creating v2.
        //   v2 = the corrected, Golden-Fixture-v3-confirmed content (DISTANCE/MIXED PrescriptionMode +
        //        AllowedDistanceAccountingModes). This is the version the active pilot dependency graph
        //        now resolves to.
        // family/eligiblePhases never differed between the restored v1 and v2 content, so those two
        // fields are confirmed identically for both versions. Versioning fixes provenance; it does not
        // upgrade domain confidence — v1's confirmed fields stay confirmed, v1's restored-legacy
        // prescription mode stays exactly as unconfirmed as it always was pre-reconciliation.
        var workouts = new (string Key, string V1File, string V2File, string PrescriptionModeV2, string AccountingModeV2, string LegacyPrescriptionModeV1, string EligiblePhases)[]
        {
            ("EASY_STANDARD", "catalog/workouts/easy-standard.v1.json", "catalog/workouts/easy-standard.v2.json", "DISTANCE", "EXACT_SESSION_TOTAL", "EFFORT_BASED", "FOUNDATION, BUILD, RACE_SPECIFIC, TAPER"),
            ("LONG_RUN_STANDARD", "catalog/workouts/long-run-standard.v1.json", "catalog/workouts/long-run-standard.v2.json", "DISTANCE", "EXACT_SESSION_TOTAL", "EFFORT_BASED", "FOUNDATION, BUILD, RACE_SPECIFIC, TAPER"),
            ("FARTLEK", "catalog/workouts/fartlek.v1.json", "catalog/workouts/fartlek.v2.json", "MIXED", "ESTIMATED_SESSION_TOTAL", "EFFORT_BASED", "BUILD"),
            ("THRESHOLD_TEMPO", "catalog/workouts/threshold-tempo.v1.json", "catalog/workouts/threshold-tempo.v2.json", "MIXED", "ESTIMATED_SESSION_TOTAL", "PACE_BASED, EFFORT_BASED", "BUILD, RACE_SPECIFIC"),
        };

        // Dynamic block starting at 200 — deliberately disjoint from every hardcoded AUD-0xx id in this
        // file (highest hardcoded id is AUD-055) so this expanded (v1+v2) entry set can never collide,
        // regardless of call order.
        var idCounter = 200;
        foreach (var w in workouts)
        {
            foreach (var version in new[] { 1, 2 })
            {
                var file = version == 1 ? w.V1File : w.V2File;

                entries.Add(Confirmed($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, w.Key, version, "$.family (taxonomy)", "EASY | LONG_RUN | QUALITY | RACE",
                    BriefSource, "brief §12.6: family is canonical and closed to exactly these four values. Corroborated by Golden Fixture v3 (this exact family value is used for this exact workoutKey). Unaffected by the WORKOUT-IMMUT-001 v1/v2 split (this field never changed between the restored v1 and corrected v2 content).",
                    ["WorkoutDefinitionValidator"]));

                entries.Add(Confirmed($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, w.Key, version, "$.eligiblePhases", w.EligiblePhases,
                    FixtureSource, $"Golden Fixture v3: workoutKey '{w.Key}' is used in exactly these phases across all 12 weeks — exact match. Unaffected by the WORKOUT-IMMUT-001 v1/v2 split (this field never changed)." + VersionParityCaveat,
                    ["WorkoutDefinitionValidator", "WorkoutProgressionValidator"]));

                entries.Add(Placeholder($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, w.Key, version, "$.complexityTier", "authored (1 or 2)",
                    file, "ComplexityTier is a Process A authoring-only concept; the generated PlanDocument never surfaces it for any workout, so Golden Fixture v3 can neither confirm nor deny any specific tier value.",
                    ["WorkoutDefinitionValidator"]));

                entries.Add(Placeholder($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, w.Key, version, "$.components", "authored structural content",
                    file, "Generic WARM_UP/COOL_DOWN tokens are structurally corroborated as existing for quality workouts in Golden Fixture v3, but the fixture's generated-output-specific main-set labels (e.g. FARTLEK_MAIN_SET, TEMPO_MAIN_SET) are NOT promoted into this catalog's generic MAIN_SET choice or into the shared WorkoutComponentType enum — ownership unresolved; see artifacts/audits/ten-k-pilot-vocabulary-decisions.md. The catalog's own generic component breakdown remains an authored, unconfirmed structural choice.",
                    ["WorkoutDefinitionValidator"]));
            }

            // v1: restored to its exact original, pre-reconciliation historical content — legacy
            // prescription mode, never fixture-confirmed, no AllowedDistanceAccountingModes field at all.
            entries.Add(Placeholder($"AUD-{idCounter++:000}", "vocabulary", DocumentTypes.WorkoutDefinition, w.Key, 1, "$.allowedPrescriptionModes", w.LegacyPrescriptionModeV1,
                w.V1File, "v1's true, restored historical content — the legacy PrescriptionMode value(s) that predate this catalog's vocabulary migration, and that were never corroborated by Golden Fixture v3. An earlier pass mistakenly overwrote this v1 file in place with the migrated/confirmed values instead of creating a new version (WORKOUT-IMMUT-001); it has now been restored to its exact original content. The corrected, fixture-confirmed value lives on v2, not here. Remains unconfirmed for v1.",
                ["WorkoutDefinitionValidator"]));
            entries.Add(Technical($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, w.Key, 1, "$.allowedDistanceAccountingModes",
                "absent (field never present on v1)", w.V1File, ["WorkoutDefinitionValidator"],
                reason: "v1's restored original content predates the AllowedDistanceAccountingModes field entirely (it was introduced only on the corrected v2 artifact) — its absence here is the correct, faithfully-restored historical schema shape, not an omission."));

            // v2: the corrected, Golden-Fixture-v3-confirmed content.
            entries.Add(Confirmed($"AUD-{idCounter++:000}", "vocabulary", DocumentTypes.WorkoutDefinition, w.Key, 2, "$.allowedPrescriptionModes", w.PrescriptionModeV2,
                FixtureSource, $"Golden Fixture v3: every occurrence of workoutKey '{w.Key}' carries prescriptionMode={w.PrescriptionModeV2}. This confirmed value lives on the genuinely new v2 artifact created by WORKOUT-IMMUT-001 remediation." + VersionParityCaveat,
                ["WorkoutDefinitionValidator"]));
            entries.Add(Confirmed($"AUD-{idCounter++:000}", "vocabulary", DocumentTypes.WorkoutDefinition, w.Key, 2, "$.allowedDistanceAccountingModes", w.AccountingModeV2,
                FixtureSource, $"Golden Fixture v3: every occurrence of workoutKey '{w.Key}' carries distanceAccountingMode={w.AccountingModeV2}. New field; exists only on v2." + VersionParityCaveat,
                ["WorkoutDefinitionValidator"]));
        }

        // GOAL_PACE_TEN_K — unaffected by WORKOUT-IMMUT-001 (not one of the 5 mutated identities); no
        // Golden Fixture v3 evidence for this specific key at all.
        entries.Add(Confirmed($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, "GOAL_PACE_TEN_K", 1, "$.family (taxonomy)", "EASY | LONG_RUN | QUALITY | RACE",
            BriefSource, "brief §12.6: family is canonical and closed to exactly these four values (applies regardless of per-workout fixture evidence).",
            ["WorkoutDefinitionValidator"]));
        entries.Add(Placeholder($"AUD-{idCounter++:000}", "workout-definitions", DocumentTypes.WorkoutDefinition, "GOAL_PACE_TEN_K", 1,
            "$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components", "authored structural content (legacy PACE_BASED prescription mode retained)",
            "catalog/workouts/goal-pace-ten-k.v1.json",
            "This workout key does not appear anywhere in Golden Fixture v3 (the fixture's closest analogues are the differently-keyed RACE_PACE_REPEATS/TEN_K_REPETITIONS). No fixture evidence exists for this specific key; per explicit instruction this catalog entry was not renamed/merged into a fixture key, and its legacy PrescriptionMode.PaceBased value was left unmigrated rather than invent a DISTANCE/MIXED guess. AllowedDistanceAccountingModes intentionally left unset (absent, not guessed).",
            ["WorkoutDefinitionValidator"]));
    }
}
