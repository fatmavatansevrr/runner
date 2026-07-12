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
