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
