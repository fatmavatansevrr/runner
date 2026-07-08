# Active v4 Domain Blocker Inventory — ACTIVE-V4-BLOCKER-001

Read-only. Scope: the exact dependency closure of `TEN_K__4D__INTERMEDIATE v4`, the current sole
non-retired active root. Source of truth: `PilotDomainContentAudit.Entries` filtered to
`Classification == PLACEHOLDER_UNCONFIRMED`, cross-checked against the live
`ContentDecisionGuardResult` returned by a Production-channel publish attempt (see
`production-readiness-error-contract-audit.md`). **Count confirmed unchanged: 13 decisions / 9
artifacts.** No repository drift detected — proceeding as normal (not stopping for drift).

## Task 1 — exact 13-decision inventory

| Decision ID | Document type | Key | Version | Field/JSON path | Current value | Classification | Reason (summary) | Audit entry ID | Production error group | Active-root reachability path |
|---|---|---|---:|---|---|---|---|---|---|---|
| D1 | RUN_LAYOUT | RUN_LAYOUT_4D | 1 | `$.slots[*].sequenceOrder` | `KEY_SESSION=1, EASY_SUPPORT=2, EASY_SUPPORT=3, LONG_RUN=4` | PLACEHOLDER_UNCONFIRMED | Arbitrary authoring choice; brief mandates slot-role shape, not order; Process B assigns real weekday/scheduledDate independently and never consumes this field for that purpose | AUD-017 | `RUN_LAYOUT/RUN_LAYOUT_4D` v1 | `v4.layout → RUN_LAYOUT_4D v1` (direct combination field) |
| D2 | PROGRESSION_MODIFIER | INTERMEDIATE_PROGRESSION_MODIFIER_V1 | 1 | `$.maximumComplexityTier, $.maximumHardSessionsPerWeek, $.mainSetDoseMultiplier, $.allowGoalPaceRehearsal, $.allowSecondHardStimulus` | `2, 1, 1.0, true, false` | PLACEHOLDER_UNCONFIRMED | Dosage/complexity ceiling invented; `progression_rules_v2.yaml` defines weekly-volume % caps and cutback ratios — a different question; fixture's one hard-session/week observation is one data point, not a general rule | AUD-044 | `PROGRESSION_MODIFIER/INTERMEDIATE_PROGRESSION_MODIFIER_V1` v1 | `v4.levelModifier → INTERMEDIATE_MODIFIER v2.progressionModifier → INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1` |
| D3 | RUNTIME_CONDITION_VALUE_REGISTRY | RUNTIME_CONDITION_VALUES_V1 | 1 | `$.conditionValueSets[PACE_SOURCE_IN,TIME_ADEQUACY_IN,CORE_ENTRY_READINESS_IN]` | `PACE_SOURCE_IN=[RACE_RESULT,TIME_TRIAL,ESTIMATED,NOT_PROVIDED]`; `TIME_ADEQUACY_IN=[ADEQUATE,TIGHT,INSUFFICIENT]`; `CORE_ENTRY_READINESS_IN=[READY,NOT_READY,UNKNOWN]` | PLACEHOLDER_UNCONFIRMED | Brief names these `RuntimeConditionType`s but never gives their vocabulary; DecisionTrace has similarly-named Process-B-internal resolver fields, but ownership mapping to this Process A registry is not established | AUD-048 | `RUNTIME_CONDITION_VALUE_REGISTRY/RUNTIME_CONDITION_VALUES_V1` v1 | `v4.rulePack → APPSEL_RACE_PLAN_V1 v2.runtimeConditionValueRegistry → RUNTIME_CONDITION_VALUES_V1 v1` |
| D4 | PEAK_VOLUME_BAND_POLICY | PEAK_VOLUME_BANDS_V1 | 2 | `$.entries[TEN_K,NEW\|ADVANCED\|EXPERIENCED,3\|4\|5]` | 9 rows: NEW 20-30/24-34/28-38 km; ADVANCED 34-46/38-52/42-58 km; EXPERIENCED 40-55/46-62/50-68 km | PLACEHOLDER_UNCONFIRMED | No canonical v1.0 source located for non-INTERMEDIATE rows; Golden Fixture v3 is INTERMEDIATE-only and was not extrapolated | AUD-057 | `PEAK_VOLUME_BAND_POLICY/PEAK_VOLUME_BANDS_V1` v2 | `v4.rulePack → APPSEL_RACE_PLAN_V1 v2.peakVolumeBandPolicy → PEAK_VOLUME_BANDS_V1 v2` |
| D5 | WORKOUT_DEFINITION | EASY_STANDARD | 2 | `$.complexityTier` | `1` | PLACEHOLDER_UNCONFIRMED | Process A authoring-only concept; generated PlanDocument never surfaces it for any workout | AUD-206 | `WORKOUT_DEFINITION/EASY_STANDARD` v2 | `v4.levelModifier → INTERMEDIATE_MODIFIER v2.eligibleWorkouts[EASY_STANDARD v2]` (also referenced from `TEN_K_WORKOUT_PROGRESSION_V1 v2.workoutCandidates`, per WorkoutClosureResolver union) |
| D6 | WORKOUT_DEFINITION | EASY_STANDARD | 2 | `$.components` | `[{MAIN_SET, EASY}]` | PLACEHOLDER_UNCONFIRMED | Generic component breakdown is an authored, unconfirmed structural choice; fixture's generation-specific labels not promoted into the shared vocabulary | AUD-207 | `WORKOUT_DEFINITION/EASY_STANDARD` v2 | same as D5 |
| D7 | WORKOUT_DEFINITION | FARTLEK | 2 | `$.complexityTier` | `1` | PLACEHOLDER_UNCONFIRMED | Same as D5 | AUD-230 | `WORKOUT_DEFINITION/FARTLEK` v2 | `v4.levelModifier → INTERMEDIATE_MODIFIER v2.eligibleWorkouts[FARTLEK v2]` (also `TEN_K_WORKOUT_PROGRESSION_V1 v2.workoutCandidates`) |
| D8 | WORKOUT_DEFINITION | FARTLEK | 2 | `$.components` | `[{WARM_UP,EASY},{MAIN_SET,SURGE_AND_FLOAT},{COOL_DOWN,EASY}]` | PLACEHOLDER_UNCONFIRMED | Same as D6 | AUD-231 | `WORKOUT_DEFINITION/FARTLEK` v2 | same as D7 |
| D9 | WORKOUT_DEFINITION | LONG_RUN_STANDARD | 2 | `$.complexityTier` | `1` | PLACEHOLDER_UNCONFIRMED | Same as D5 | AUD-218 | `WORKOUT_DEFINITION/LONG_RUN_STANDARD` v2 | `v4.levelModifier → INTERMEDIATE_MODIFIER v2.eligibleWorkouts[LONG_RUN_STANDARD v2]` — confirmed **exclusively** reachable here, absent from `TEN_K_WORKOUT_PROGRESSION_V1 v2.workoutCandidates` (see `zero-six-pilot-release-audit.md`) |
| D10 | WORKOUT_DEFINITION | LONG_RUN_STANDARD | 2 | `$.components` | `[{WARM_UP,EASY},{MAIN_SET,STEADY}]` | PLACEHOLDER_UNCONFIRMED | Same as D6 | AUD-219 | `WORKOUT_DEFINITION/LONG_RUN_STANDARD` v2 | same as D9 |
| D11 | WORKOUT_DEFINITION | THRESHOLD_TEMPO | 2 | `$.complexityTier` | `2` | PLACEHOLDER_UNCONFIRMED | Same as D5 | AUD-242 | `WORKOUT_DEFINITION/THRESHOLD_TEMPO` v2 | `v4.levelModifier → INTERMEDIATE_MODIFIER v2.eligibleWorkouts[THRESHOLD_TEMPO v2]` (also `TEN_K_WORKOUT_PROGRESSION_V1 v2.workoutCandidates`) |
| D12 | WORKOUT_DEFINITION | THRESHOLD_TEMPO | 2 | `$.components` | `[{WARM_UP,EASY},{MAIN_SET,THRESHOLD},{COOL_DOWN,EASY}]` | PLACEHOLDER_UNCONFIRMED | Same as D6 | AUD-243 | `WORKOUT_DEFINITION/THRESHOLD_TEMPO` v2 | same as D11 |
| D13 | WORKOUT_DEFINITION | GOAL_PACE_TEN_K | 1 | `$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components` | `eligiblePhases=[RACE_SPECIFIC]`; `complexityTier=2`; `allowedPrescriptionModes=[PACE_BASED]`; `components=[{WARM_UP,EASY},{MAIN_SET,GOAL_PACE},{COOL_DOWN,EASY}]` | PLACEHOLDER_UNCONFIRMED | This workout key appears nowhere in Golden Fixture v3 (closest analogues are the differently-keyed `RACE_PACE_REPEATS`/`TEN_K_REPETITIONS`); zero fixture evidence for this specific key; legacy `PACE_BASED` prescription mode deliberately left unmigrated rather than guessed | AUD-249 | `WORKOUT_DEFINITION/GOAL_PACE_TEN_K` v1 | `v4.masterTemplate → TEN_K_MASTER v3.workoutProgression → TEN_K_WORKOUT_PROGRESSION_V1 v2.workoutCandidates[GOAL_PACE_TEN_K v1]` (RACE_SPECIFIC phase, per brief §9/§10 GOAL_PACE_REHEARSAL stage) |

**Row count: 13. Distinct artifact identities: 9. No missing decision. No duplicated decision.** Every
row is mapped to its structured `ContentDecisionGuardError` (via matching `DocumentType`+`Key`+`Version`
error group) and is proven reachable from `TEN_K__4D__INTERMEDIATE v4` (path column above, derived from
the actual published `0.6.0-pilot` bundle's dependency graph — see `zero-six-pilot-release-audit.md`).

## The 9 owning artifacts

| # | Artifact identity | Decision IDs owned | Decision count |
|---|---|---|---:|
| A1 | `RUN_LAYOUT/RUN_LAYOUT_4D` v1 | D1 | 1 |
| A2 | `PROGRESSION_MODIFIER/INTERMEDIATE_PROGRESSION_MODIFIER_V1` v1 | D2 | 1 |
| A3 | `RUNTIME_CONDITION_VALUE_REGISTRY/RUNTIME_CONDITION_VALUES_V1` v1 | D3 | 1 |
| A4 | `PEAK_VOLUME_BAND_POLICY/PEAK_VOLUME_BANDS_V1` v2 | D4 | 1 |
| A5 | `WORKOUT_DEFINITION/EASY_STANDARD` v2 | D5, D6 | 2 |
| A6 | `WORKOUT_DEFINITION/FARTLEK` v2 | D7, D8 | 2 |
| A7 | `WORKOUT_DEFINITION/LONG_RUN_STANDARD` v2 | D9, D10 | 2 |
| A8 | `WORKOUT_DEFINITION/THRESHOLD_TEMPO` v2 | D11, D12 | 2 |
| A9 | `WORKOUT_DEFINITION/GOAL_PACE_TEN_K` v1 | D13 | 1 |

`1+1+1+1+2+2+2+2+1 = 13`. ✓

## Task 2 — decision-type classification

| Decision ID | Primary category | Why | Affects generated training behavior? | Belongs in Process A? | Could be removed instead of assigned? | Requires canonical running evidence? |
|---|---|---|---|---|---|---|
| D1 | **POSSIBLY_REDUNDANT_FIELD** | The brief explicitly disclaims that catalog-level `SequenceOrder` models real-world weekday scheduling (that is a Process B runtime concern); it is unclear whether anything downstream reads the numeric order at all, as opposed to only the slot *shape* (role counts). The primary open question is "is this field even consumed," not "which value is correct." | Not confirmed to affect output — Process B does its own scheduling independent of this field. | Yes, structurally (it's a `RunLayoutDefinition` slot field) | **Yes — this is the leading hypothesis to test first**, before assigning any canonical order | No — if kept, it is at most a display/authoring convention, not a domain-evidence question |
| D2 | **RUNNING_DOMAIN_RULE** | Directly governs training prescription: how many hard sessions per week, how much a main-set dose scales, whether goal-pace rehearsal / a second hard stimulus is permitted for an intermediate athlete. This is core dosage/periodization policy. | **Yes, directly** — `ProgressionModifier.maximumComplexityTier`/`maximumHardSessionsPerWeek` gate which workouts and how many hard sessions a generated plan may include | Yes (reusable per-experience-level policy) | No — essential gating logic, not decorative | **Yes, strongly** — needs coaching/exercise-science evidence on hard-session frequency and dose progression for intermediate 10K runners |
| D3 | **TAXONOMY_OR_VOCABULARY** | Defines the closed allowed-value vocabulary for three `RuntimeConditionType`s — an enum/taxonomy decision, not itself a numeric training rule | Indirectly — if wrong, condition-gated `RulePack` rules referencing these types could silently never match real Process B output | Yes (registry ownership is explicitly a Process A concern per `docs/README.md` §14) | No — structurally required if any rule references these condition types | Partially — the determining evidence is Process-B's actual resolver output vocabulary (an ownership/contract question) more than a literature question |
| D4 | **RUNNING_DOMAIN_RULE** | Peak weekly training-volume ranges (km) per experience level and weekly frequency are core training-load domain values | **Yes, directly** — bounds the target peak volume a generated plan may reach for these experience levels | Yes (reusable per-experience-level policy table) | No | **Yes, strongly** — needs coaching/exercise-science volume tables by experience level |
| D5, D7, D9, D11 | **TAXONOMY_OR_VOCABULARY** | `complexityTier` is a categorical tag (currently ordinal 1 or 2) assigning each workout to a tier bucket — a taxonomy/classification decision, distinct from the *threshold rule* that consumes it (D2) | Indirectly — consumed by `ProgressionModifier.maximumComplexityTier` (D2) to gate workout eligibility, even though the generated `PlanDocument` itself never surfaces the tier value | Yes | No — the field is actively consumed by D2's gating logic | Needs a defined tier rubric (what distinguishes tier 1 from tier 2) more than a literature citation per value |
| D6, D8, D10, D12 | **TAXONOMY_OR_VOCABULARY** | The generic `WARM_UP`/`MAIN_SET`/`COOL_DOWN` structural breakdown is a component-vocabulary granularity decision — the open question (per `ten-k-pilot-vocabulary-decisions.md`) is whether this generic vocabulary is the right level of detail versus the fixture's more specific generated-output labels (e.g. `FARTLEK_MAIN_SET`) | Uncertain — fixture shows the *generated output* uses more specific labels for some workouts, suggesting the catalog's generic breakdown may not be granular enough to drive that behavior, but ownership of that mapping is unresolved | Yes | Not clearly — some structural breakdown is real (fixture shows workouts do decompose into parts); the question is granularity, not existence | Needs an explicit vocabulary-ownership decision (ties to D3's "Process A vs Process B ownership" theme) |
| D13 | **SOURCE_EVIDENCE_GAP** | Unlike the other 4 workouts, `GOAL_PACE_TEN_K` has **zero** fixture evidence of any kind for any of its 4 bundled fields — the defining characteristic of this row is total evidence absence, not ambiguity about which category the fields belong to (individually they would span RUNNING_DOMAIN_RULE/eligiblePhases, TAXONOMY_OR_VOCABULARY/complexityTier+prescriptionMode+components) | Yes, potentially — this workout is reachable from the active closure and would affect generated plans if selected | Yes | Not obviously — the workout is structurally referenced by the active workout progression | Cannot currently be answered with any Tier 1/2 evidence targeting this specific key; needs either dedicated new source research or an explicit product decision |

**Category tally**: RUNNING_DOMAIN_RULE = 2 (D2, D4); TAXONOMY_OR_VOCABULARY = 9 (D3, D5–D12);
POSSIBLY_REDUNDANT_FIELD = 1 (D1); SOURCE_EVIDENCE_GAP = 1 (D13); PRODUCT_DEFAULT = 0 as a *primary*
category (several decisions are likely to *resolve* as `EXPLICIT_PRODUCT_DEFAULT` — see
`domain-blocker-resolution-plan.md` — but none of the 13 is classified that way as its root type);
TECHNICAL_METADATA = 0 (none of the 13 remaining blockers were found to be purely mechanical —
mechanical fields are already `TechnicalOnly` and excluded from this blocking set by construction);
DERIVED_OR_GENERATED_VALUE = 0 (no blocker here represents a value that should instead be computed by
Process B at runtime rather than authored as static catalog policy).

No running-domain decision (D2, D4) was classified as technical merely to unblock Production — both
remain `RUNNING_DOMAIN_RULE` and are flagged as requiring the strongest evidence standard.

## Final status: inventory complete, 13/13 rows produced, 9/9 artifacts confirmed, no drift.
