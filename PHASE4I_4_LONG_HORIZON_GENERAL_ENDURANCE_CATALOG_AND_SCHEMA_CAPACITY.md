# Phase 4I.4 — Long-Horizon General Endurance Catalog and Schema Capacity

## 1. Executive result

Real catalog/schema capacity now exists for the full 1–32-week
`LONG_HORIZON_GENERAL_ENDURANCE` segment, for both readiness profiles,
with proven deterministic, non-exhausting, profile-specific selection.
**Zero new workout keys were created** — a direct audit of the 22
existing workout definitions found `EASY_STANDARD`, `LONG_RUN_STANDARD`,
`AEROBIC_STRENGTH_CONTROLLED_INTRO`, and
`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` already express every approved
semantic; only four new `eligiblePhases`-extension versions were added. A
new dark stage-family catalog document plus a deterministic C# selector
(mesocycle/remainder/ShortExtension placement as a code-level rule, never
32 hand-authored week records) prove the full 1–32 matrix resolves
correctly for both profiles, including a standalone mandatory 32-week
exhaustion proof. Zero live or public behavior changed — no backend file
was touched, and the new catalog folder is confirmed inert to the real
loader, mirroring the exact containment mechanism already proven for the
Preparation Runway's own dark progression documents.

```
LONG_HORIZON_GENERAL_ENDURANCE_CATALOG_AND_SCHEMA_CAPACITY_COMPLETED_FOR_1_TO_32_WEEKS_WITH_PROFILE_SPECIFIC_DETERMINISTIC_SELECTION_AND_NO_PUBLIC_ACTIVATION
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_STRUCTURAL_MATERIALIZATION_NUMERIC_EXECUTION_DARK_PIPELINE_WIRING_AND_PUBLIC_PREVIEW
```

## 2. Inherited policies

Phase 4I.1 (composition formula), Phase 4I.2 (structural safety policy,
mesocycle/weekly-role/ShortExtension/remainder rules), Phase 4I.2A
(recovery numeric policy metadata), Phase 4I.3 (dark typed runtime
authority `LongHorizonCompositionResolver`, `LongHorizonCompositionDecision`,
`LongHorizonCompositionValidator`, backend-side, unwired). All consumed as
authoritative input; none re-derived or re-decided here.

## 3. Exact scope

`TEN_K__4D__INTERMEDIATE v10` only; Race/10K/4-days-per-week;
`CONSISTENCY_NEEDED`/`CORE_ENTRY_READY`; GE 1–32 weeks. Explicitly not
created: 5K/Half Marathon/Marathon catalog capacity, other levels, other
day-frequencies, habit-plan capacity, 53+ capacity. No numeric volume/
pace/date value was assigned anywhere in this phase.

## 4. Artifacts inspected

Full catalog tree (`catalog/templates`, `catalog/workouts`,
`catalog/workout-progressions`, `catalog/preparation-runway-progressions`,
`catalog/registries`); `schemas/workout-definition.schema.json`;
`src/PlanCatalog.Contracts/Enums/PhaseKey.cs`;
`src/PlanCatalog.Infrastructure/Repositories/FileSystemCatalogSourceRepository.cs`
(confirmed the exact 10 hardcoded subfolders it scans:
`templates, layouts, level-modifiers, workout-progressions,
progression-modifiers, workouts, registries, policies, rule-packs,
combinations` — `preparation-runway-progressions` and the new
`long-horizon-progressions` are both outside this list); all 22
pre-existing workout-definition files (every version of `EASY_STANDARD`,
`LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`,
`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED`); the Preparation
Runway's own dark precedent (`schemas/preparation-runway-block-progression.schema.json`,
`catalog/preparation-runway-progressions/*.json`,
`tests/.../RemainingRunwayBlockCatalogTests.cs`,
`AerobicStrengthPreparationRunwayCatalogTests.cs`); `ten-k-master.v6.json`
(re-confirmed unchanged: Foundation 2/3/4, Build 3/4/5, RaceSpecific
2/4/4, Taper 1/1/1, coreCycle 8/12/14); the four current GE governance
decisions (`TD-LONG-HORIZON-COMPOSITION-001`/`-GE-SAFETY-001`/
`-GE-RECOVERY-MAGNITUDE-001`/`-TYPED-RUNTIME-DECISION-001`).

## 5. Existing catalog-capacity audit

Every one of the 22 workout files was inspected for key, version, family,
`eligiblePhases`, and MAIN_SET `intensityDescriptor`. Findings: only
`EASY_STANDARD` and `LONG_RUN_STANDARD` (both v5) already carry
`PREPARATION_RUNWAY` eligibility and generic `EASY`/`STEADY` intensity —
directly extensible. `FARTLEK` (`SURGE_AND_FLOAT`) is hard-scoped to
`BUILD` only in every version. `THRESHOLD_TEMPO` (`THRESHOLD`) and
`GOAL_PACE_TEN_K` (`GOAL_PACE`) are exactly the two prohibited-intensity
workouts Phase 4I.2 already forbids for GE `KEY_SESSION` content —
confirmed never extended. `AEROBIC_STRENGTH_CONTROLLED_INTRO`/
`_PROGRESSED` (v1, `CONTROLLED_AEROBIC_POWER_INTRO`/`_PROGRESSED`) are
scoped to `PREPARATION_RUNWAY` only but semantically match Phase 4I.2's
approved "controlled aerobic support" `KEY_SESSION` content directly.

## 6. Catalog architecture decision

**Hybrid D**, the phase's own preferred option: explicit ShortExtension
templates (1/2/3 GE weeks — code-level rule, 3 possible shapes total, not
32 records), reusable 4-week mesocycle stage-family templates
(catalog-driven role/workout content, keyed by stage family, not by
absolute week number), explicit terminal-remainder templates (1/2/3
leftover weeks — reuses the same 3 shapes as ShortExtension, since both
are structurally "1–3 weeks of Entry/ControlledDevelopment/Alignment"),
and one deterministic selector combining them. This cannot exhaust at 32
weeks because the selector's per-mesocycle stage-family choice is a
formula (`mesocycleIndex`/`totalMesocycles` → one of 2 stage families),
not a lookup into a finite pre-authored list — the same formula produces
correct output for a hypothetical 33rd mesocycle without any new catalog
data (even though 33+ is out of scope and rejected upstream by Phase
4I.1's own 52-week ceiling). No 32 manually duplicated week records exist
anywhere; the catalog document itself has exactly 5 stage-family entries,
regardless of how many GE weeks are ultimately selected.

## 7. Schema changes

Additive only: `schemas/workout-definition.schema.json`'s
`eligiblePhases` enum gained one member,
`"LONG_HORIZON_GENERAL_ENDURANCE"` — every existing workout document
(with the old, unchanged enum values) remains schema-valid, confirmed by
`AllExistingAndNewWorkoutDefinitions_StillValidateAgainstTheSchema`
re-running unchanged (only its hardcoded file-count assertion was updated
from 22 to 26, a legitimate consequence of 4 new files, not a semantic
change). One new schema file was added,
`schemas/long-horizon-ge-stage-family.schema.json` (new document type
`LONG_HORIZON_GE_STAGE_FAMILY_CATALOG`), mirroring
`preparation-runway-block-progression.schema.json`'s exact structure/
convention. No migration; no existing schema file's meaning changed for
any pre-existing document.

## 8. Segment/provenance model

`src/PlanCatalog.Core/LongHorizon/LongHorizonGeCatalogContracts.cs`:
`LongHorizonReadinessProfile`, `GeDurationClassification`, `GeStageFamily`
(`Entry`/`BaseDevelopment`/`AerobicDurability`/`Consolidation`/
`PreRunwayAlignment`), `MesocyclePosition` (`Development1`/`Development2`/
`Development3`/`RecoveryConsolidation`/`NotApplicable`), `ShortExtensionRole`
(`EntryAlignment`/`ControlledDevelopment`/`PreRunwayAlignment`/
`NotApplicable`), `GeWeekRole` (`KeySession`/`EasySupportA`/`EasySupportB`/
`LongRun`), `GeWorkoutReference`, `GeWeekDescriptor` (the full provenance
record: week index, segment type constant, classification, mesocycle
index/position, ShortExtension role, stage family, recovery/terminal
flags, profile, role→workout map, catalog source ID/version).
`LongHorizonGeneralEnduranceSegmentType = "LONG_HORIZON_GENERAL_ENDURANCE"`
is a distinct string constant, never merged with
`PreparationRunwayBlockType.GeneralEndurance` or
`preparation-runway-block-progression.schema.json`'s `blockType:
"GENERAL_ENDURANCE"` — proven by
`LongHorizonSegmentType_IsTextuallyDistinctFromRunwayGeneralEnduranceBlock`
(the Runway schema file is grepped and confirmed to never contain the new
string).

## 9. Stage-family model

Five stage families, exactly as required, each with `eligibleContexts`
(`SHORT_EXTENSION`/`MESOCYCLE`/`TERMINAL_REMAINDER`), `recoveryCompatible`,
`maxConsecutiveMesocycles`, and per-role/per-profile `workoutCandidates`:
`ENTRY` (ShortExtension only, cap 0 — never appears in a mesocycle, per
Phase 4I.2's own finding that Entry belongs only to ShortExtension's
vocabulary), `BASE_DEVELOPMENT` (cap 1 consecutive mesocycle),
`AEROBIC_DURABILITY` (cap 2 consecutive mesocycles, "appears in longer
horizons"), `CONSOLIDATION` (recovery-compatible, mesocycle Week 4 only),
`PRE_RUNWAY_ALIGNMENT` (ShortExtension/terminal-remainder only). No
race-specific stage exists. `BASE_DEVELOPMENT`/`AEROBIC_DURABILITY` are
textually and semantically distinct from Core's `FOUNDATION`/`BUILD` —
different enum, different catalog document, different schema.

## 10. ShortExtension catalog

1 GE week → `EntryAlignment`/`Entry` (single combined week). 2 GE weeks →
`EntryAlignment`/`Entry` then `PreRunwayAlignment`/`PreRunwayAlignment`.
3 GE weeks → `EntryAlignment`/`Entry`, `ControlledDevelopment`/
`BaseDevelopment`, `PreRunwayAlignment`/`PreRunwayAlignment`. No week is
ever a recovery week (structurally impossible — `ShortExtensionRole`
weeks never map to `IsRecoveryWeek=true` anywhere in the selector).
`CONSISTENCY_NEEDED`'s `KEY_SESSION` is `EASY_STANDARD` throughout;
`CORE_ENTRY_READY`'s `ControlledDevelopment` week (GE3 only) may use
`AEROBIC_STRENGTH_CONTROLLED_INTRO` (via the `BaseDevelopment` stage
family's role assignment) — Entry/Alignment weeks use `EASY_STANDARD` for
both profiles (no controlled support at pure orientation/alignment
boundaries). Duration is never altered by profile — proven by
`BothReadinessProfiles_ProduceIdenticalStructuralPositions` at GE=1.

## 11. Four-week mesocycle catalog

Week 1/2/3 = `Development1`/`Development2`/`Development3`, all sharing one
mesocycle-level stage family (`BaseDevelopment` or `AerobicDurability`,
per §15); Week 4 = `RecoveryConsolidation`, always `Consolidation` stage
family, always `IsRecoveryWeek=true`. Every week carries the full 4-role
map. No numeric volume or recovery formula is executed — the catalog
exposes only the structural/semantic shape (stage family, position,
workout key/version/family) Phase 4I.6 (or whichever future phase
executes numerics) will need.

## 12. Development-week content

`KEY_SESSION`: `CONSISTENCY_NEEDED` → `EASY_STANDARD` always (no
unsupported hard aerobic support, per Part 4). `CORE_ENTRY_READY` →
`AEROBIC_STRENGTH_CONTROLLED_INTRO` in `BaseDevelopment` mesocycles,
`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` in `AerobicDurability`
mesocycles. `EASY_SUPPORT`×2 and `LONG_RUN`: `EASY_STANDARD`/
`LONG_RUN_STANDARD` for both profiles, every stage family. `THRESHOLD_TEMPO`,
`GOAL_PACE_TEN_K`, and `FARTLEK` are never referenced anywhere in the new
catalog document or selector output — proven by
`NoStageFamilyDocument_ReferencesAThresholdOrGoalPaceWorkout` (document-
level grep) and `LowIntensityDominanceRetained_NoProhibitedWorkoutForEitherProfile`
(selector-output-level, both profiles, 5 representative durations).

## 13. Recovery-week content

`Consolidation` stage family's `KEY_SESSION` role assignment uses profile
`"ANY"` → `EASY_STANDARD` for both profiles (never
`AEROBIC_STRENGTH_CONTROLLED_*`, even for `CORE_ENTRY_READY` — downgraded/
recovery-compatible, per Part 7). `EASY_SUPPORT`×2/`LONG_RUN` also
`EASY_STANDARD`/`LONG_RUN_STANDARD`. `KEY_SESSION` remains structurally
`KEY_SESSION` (the role key is unchanged; only its resolved workout family
differs) — the "preferred behavior" option from Part 7. All four slots
remain running sessions (the descriptor always has exactly 4 role
entries, validator-enforced). No new workout definition was needed for
recovery content — `EASY_STANDARD` alone expresses it fully.

## 14. Terminal remainder catalog

Remainder 1 → `PreRunwayAlignment` only. Remainder 2 →
`ControlledDevelopment`/`BaseDevelopment` then `PreRunwayAlignment`.
Remainder 3 → `EntryAlignment`/`Entry`, `ControlledDevelopment`/
`BaseDevelopment`, `PreRunwayAlignment`/`PreRunwayAlignment` — identical
shape to ShortExtension GE3 (intentional reuse of the same 3-role
pattern, not a defect). Always placed after complete mesocycles
(`NoRemainderAppearsBeforeACompleteMesocycle`), always a contiguous
terminal suffix, never contains a recovery-flagged week, never duplicates
Entry within the same duration's remainder, never produces an
unsupported peak (all remainder content is `EASY_STANDARD`/
`AEROBIC_STRENGTH_CONTROLLED_INTRO` at most — never
`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED`, the "harder" of the two
controlled-support variants), and never modifies Runway Week 1 (the
Preparation Runway catalog is untouched, §24).

## 15. Mesocycle sequencing

Deterministic rule (`LongHorizonGeCatalogSelector.StageFamilyForMesocycle`):
mesocycle 1 = `BaseDevelopment` (Entry's orientation role is absorbed
here per Phase 4I.2's own finding); the final mesocycle of a given
duration = `AerobicDurability` ("aligns toward Runway"); every other
mesocycle alternates by index parity. Result for all eight mesocycle
counts (GE 4/8/12/16/20/24/28/32): `BaseDevelopment` never repeats more
than 1 consecutive mesocycle; `AerobicDurability` never repeats more than
2 consecutive mesocycles; eight identical mesocycles never occur (32-week
sequence is `Base,Aero,Base,Aero,Base,Aero,Base,Aero` — strictly
alternating, both stage families appear, proven directly). This is a
single formula, testable and tested for every GE duration 4..32, not a
hardcoded table.

## 16. CONSISTENCY_NEEDED selection

Every duration produces the same structural positions as
`CORE_ENTRY_READY` (§17 comparison). Content: `KEY_SESSION` is always
`EASY_STANDARD` — no controlled aerobic support anywhere, in
ShortExtension, FullPhase development weeks, or terminal remainder. Low-
intensity dominance is trivially retained (100% `EASY`/`LONG_RUN` family
content).

## 17. CORE_ENTRY_READY selection

Same structural positions as `CONSISTENCY_NEEDED` for every duration
1–32 (proven by loop, `BothReadinessProfiles_ProduceIdenticalStructuralPositions`,
not just fixture points). `KEY_SESSION` uses
`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` in `FullPhase`
development weeks (never in `Consolidation`/`Entry`/`Alignment` weeks),
proven present at least once across a 32-week selection
(`ProfileContentDiffers_CoreEntryReadyUsesControlledAerobicSupportInDevelopmentWeeks`).
No prohibited intensity ever appears for this profile either.

## 18. Workout-definition audit

Full table recorded in §5. Classification per the phase's own required
taxonomy: `EASY_STANDARD`/`LONG_RUN_STANDARD` = directly reusable with a
new eligiblePhases version; `AEROBIC_STRENGTH_CONTROLLED_INTRO`/
`_PROGRESSED` = reusable with a new eligiblePhases version, specific to
the `CORE_ENTRY_READY` profile's development-week `KEY_SESSION`;
`THRESHOLD_TEMPO`/`GOAL_PACE_TEN_K` = prohibited (never touched);
`FARTLEK` = prohibited for this phase's scope (its `SURGE_AND_FLOAT`
intensity is not one of Phase 4I.2's approved `KEY_SESSION` content
categories, and no direct audit evidence justified extending it — left
unextended, not a gap since `EASY_STANDARD`/`AEROBIC_STRENGTH_CONTROLLED_*`
already cover every required role). No new-definition-required case was
found.

## 19. Reused definitions

`EASY_STANDARD`, `LONG_RUN_STANDARD`, `AEROBIC_STRENGTH_CONTROLLED_INTRO`,
`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` — all four via a new version
each (v6/v6/v2/v2), each version an `eligiblePhases`-array-only diff from
its immediate predecessor, no other field changed.

## 20. New definitions

**None.** Zero new workout keys were created — the direct audit (§5, §18)
proved unnecessary before any was considered, per this phase's own
decision standard ("Do not create new workout definitions before this
audit").

## 21. Repetition policy

Encoded as catalog metadata (`maxConsecutiveMesocycles` per stage family)
plus selector-level structural guarantees. `EASY_STANDARD`/
`LONG_RUN_STANDARD` repeating every single week (up to 32 consecutive
weeks) is explicitly intentional — numeric progression alone is
sufficient semantic variation for these two families, matching the
existing, real, already-approved precedent
(`ten-k-general-endurance-progression.v1.json`'s own 5 identical
`LONG_RUN_STANDARD` steps, confirmed unchanged and untouched). CORE_ENTRY_READY's
`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` `KEY_SESSION` never
exceeds 6 consecutive weeks of the identical workout (bounded by the
`maxConsecutiveMesocycles=1`/`2` stage-family caps × 3 development weeks
per mesocycle) — proven directly, not assumed.

## 22. 1–32 duration matrix

All 32 durations × both profiles (64 combinations) resolve with the
exact expected week count, correct `ShortExtension`/`FullPhase`
classification, exactly 4 roles per week, zero prohibited workout, zero
unresolved catalog reference, deterministic output, and pass
`LongHorizonGeCatalogValidator.Validate` — proven by five
`[Theory]`/`MemberData`-driven tests spanning all 64 combinations each
(`EveryDuration_ResolvesWithExactWeekCountAndFourCompleteRoles`,
`EveryDuration_ClassifiesCorrectly`, `EveryDuration_PassesValidation`,
`EveryDuration_IsDeterministic`, `EveryDuration_NoUnresolvedCatalogReference`).

## 23. 32-week exhaustion proof

Standalone, mandatory, separate from the general matrix
(`ThirtyTwoWeeksExhaustionProof`, both profiles): exactly 32 weeks,
exactly 8 recovery weeks, 0 terminal-alignment weeks (32 is an exact
multiple of 4), full role-skeleton completeness on every week,
`LongHorizonGeCatalogValidator` passes, zero prohibited workout. Combined
with `ThirtyTwoWeeks_DoesNotUseEightIdenticalMesocycles` (§15), this
proves the catalog architecture does not exhaust or degenerate at the
maximum approved duration.

## 24. GE→Runway compatibility

The final GE week's `KEY_SESSION` resolves to `EASY_STANDARD` (family
`EASY`) for both profiles — structurally compatible with the Preparation
Runway's own opening reference (`ten-k-consistency-progression.v1.json`
Step 1, also `EASY_STANDARD`, confirmed via the pre-existing
`RemainingRunwayBlockCatalogTests` file, unchanged). No prohibited-
intensity jump, no duplicate transition/alignment semantics, no
structural conflict. Numeric continuity (volume/pace) is explicitly
**not** computed or claimed here — deferred to a later phase, per the
phase's own required distinction. The approved Runway catalog documents
(`ten-k-consistency-progression.v1.json`,
`ten-k-general-endurance-progression.v1.json`,
`ten-k-pre-specific-transition-progression.v1.json`,
`ten-k-aerobic-strength-progression.v1.json`) are confirmed unmodified —
direct file-content check, not just "not mentioned in a diff."

## 25. Validators

`LongHorizonGeCatalogValidator.Validate(geWeeks, weeks)`: exact week
count, week-index ordering, segment-type consistency, exactly 4 roles per
week, no prohibited workout key/family, `ShortExtension` weeks never
recovery, `FullPhase` weeks exactly one of mesocycle-positioned or
terminal (never both/neither), `IsRecoveryWeek` matches
`RecoveryConsolidation` position exactly, terminal weeks never recovery,
provenance (`CatalogSourceId`) present, recovery lands exactly every
fourth mesocycle week, terminal remainder is a contiguous suffix. Fails
closed — returns `IsValid=false` with findings, never auto-corrects
(`Validator_RejectsAnImpossibleHandConstructedDecision`-equivalent
coverage via the adversarial hand-`with`-modified fixture pattern
established in Phase 4I.3, reused here for the catalog validator too).

## 26. Registry/reference integrity

No static manifest exists for `catalog/` source documents (confirmed:
registry integrity for the source tree is purely test-driven, per direct
inspection of `catalog/registries/` — those documents are the unrelated
`RUNTIME_CONDITION_VALUE_REGISTRY` vocabulary, not a document-count
index). All new document IDs/versions are unique (no `key`+`version`
collision with any existing 22 workout files or the new stage-family
document); all catalog-candidate references inside the new stage-family
document resolve to one of the four approved new workout versions
(`StageFamilyCatalogDocument_ReferencesOnlyTheNewLongHorizonEligibleWorkoutVersions`);
no orphan stage or workout; no circular reference (the document has no
self-referential structure); the new `long-horizon-progressions` folder
remains outside the real loader's candidate-visibility surface entirely.

## 27. Containment

`long-horizon-progressions` confirmed NOT among
`FileSystemCatalogSourceRepository.LoadSnapshot`'s 10 hardcoded scanned
subfolders (direct source inspection, §4). The real published
`TEN_K__4D__INTERMEDIATE v10` bundle, constructed via the actual
`CatalogBundleAssembler`/`CatalogStamper` pipeline (not a mock), is
confirmed to include none of the four new workout versions
(`RealTenKCandidateBundle_DoesNotIncludeAnyNewWorkoutVersionOrLongHorizonDocument`).
The live `ten-k-workout-progression.v5.json` is confirmed to reference
none of them either. `LongHorizonCompositionResolver` (Phase 4I.3) and the
new `LongHorizonGeCatalogSelector` (this phase) remain completely unwired
from each other and from any live request path — no code in either phase
calls the other, and no backend file was touched by this phase at all.
Since `TD-BACKEND-001` already establishes zero backend/plan-catalog
integration exists today, public preview containment for 21/24/40/52
weeks is unaffected structurally by construction — re-confirmed by the
fact that no backend file appears in this phase's file list (§ Final
Report items 1–4).

## 28. Governance status

`TD-LONG-HORIZON-COMPOSITION-001`, `TD-LONG-HORIZON-GE-SAFETY-001`,
`TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001`,
`TD-LONG-HORIZON-TYPED-RUNTIME-DECISION-001`: unchanged, remain `CLOSED`.
New: `TD-LONG-HORIZON-GE-CATALOG-CAPACITY-001`, added `CLOSED`.
`TD-GENERAL-ENDURANCE-STAGED-PLAN-001`: remains `OPEN` (structural
materialization, numeric execution, calendar assignment, dark pipeline
wiring, public preview, confirmation, persistence all still
unimplemented). Aggregate updated to 24 risks total, 10 `OPEN`, 14
`CLOSED`.

## 29. Focused tests

`LongHorizonGeCatalogCapacityTests` (schema/containment, new) +
`LongHorizonGeCatalogSelectorTests` (selector/matrix, new) +
`LongHorizonGeCatalogCapacityGovernanceTests` (governance cross-check,
new) + the pre-existing Phase 4I.1/4I.2/4I.2A/4I.3-related decision tests
also matched by the `~LongHorizon` filter substring — **506 passed, 0
failed** on the first combined focused run (after one real
test-authoring fix, §32).

## 30. Full plan-catalog suite

```
dotnet test tests/PlanCatalog.Tests
Başarısız: 0, Başarılı: 904, Atlanan: 0, Toplam: 904
```

**904 passed, 0 failed, 0 skipped** (up from the 516 pre-existing baseline
— the increase reflects both the new tests and `[Theory]`/`MemberData`
expansion of the 1–32×2-profile matrix tests into individual xUnit test
cases).

## 31. Backend suite

Not run. No backend file was created or modified by this phase (all
changes are plan-catalog-only) — confirmed by the file lists in §2/§3 of
the final report below.

## 32. Production defects found

One real, pre-existing test defect surfaced (not a design defect): a
hardcoded expected workout-file count (22) in
`AerobicStrengthPreparationRunwayCatalogTests.AllExistingAndNewWorkoutDefinitions_StillValidateAgainstTheSchema`
needed updating to 26 after this phase's four legitimate new files were
added — corrected with an updated comment explaining the new total's
derivation, not silently changed.

## 33. Production fixes made

The one test-count update in §32. No production catalog/schema logic
defect was found or fixed — the existing catalog/schema architecture
supported this phase's needs without any correction.

## 34. Explicit non-implementation statement

No structural plan materialization occurred. No numeric weekly volume,
long-run distance, or recovery magnitude was computed or executed (Phase
4I.2A's formula remains policy metadata only, referenced by ID, never
run). No calendar date was assigned. No public preview route was added or
activated for 21–52 weeks. No confirmability or persistence path exists.
`LongHorizonCompositionResolver` was not wired into any live pipeline.
53+ remains fully unsupported. No candidate other than
`TEN_K__4D__INTERMEDIATE v10` was touched. No Flutter file was touched.

## 35. Residual blockers

Structural materialization (turning a `GeWeekDescriptor` sequence into a
real `TrainingWeek`/`TrainingDay`-shaped structure), numeric execution
(applying Phase 4I.2/4I.2A's volume/long-run/recovery formulas to produce
actual km/min values), calendar assignment, dark pipeline wiring (the
still-undecided question of when/how `LongHorizonCompositionResolver` and
`LongHorizonGeCatalogSelector` get invoked together from a real request),
public preview, confirmation, persistence, segment-provenance persistence
shape, and the unproven Habit-planner-reuse claim — all still tracked
under `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`, still `OPEN`.

## 36. Final classification

```
LONG_HORIZON_GENERAL_ENDURANCE_CATALOG_AND_SCHEMA_CAPACITY_COMPLETED_FOR_1_TO_32_WEEKS_WITH_PROFILE_SPECIFIC_DETERMINISTIC_SELECTION_AND_NO_PUBLIC_ACTIVATION
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_STRUCTURAL_MATERIALIZATION_NUMERIC_EXECUTION_DARK_PIPELINE_WIRING_AND_PUBLIC_PREVIEW
```

## 37. Exact next phase

**Phase 4I.5 — Long-Horizon General Endurance Structural Materialization
(Dark)**: consume this phase's `GeWeekDescriptor` sequence plus Phase
4I.2/4I.2A's numeric policy to produce a dark, unwired structural
week/day skeleton (still no calendar dates, still no public activation) —
mirroring how the Preparation Runway line sequenced its own
`PreparationRunwayNumericMaterializer`/`TenKPreparationRunwayDarkOrchestrator`
work after its own catalog-capacity phase closed. Only after that should
a future phase decide dark pipeline wiring, calendar assignment, and
eventually public preview activation.
