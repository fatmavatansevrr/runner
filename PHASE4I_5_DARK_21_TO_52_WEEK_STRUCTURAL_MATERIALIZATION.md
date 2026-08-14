# Phase 4I.5 — Dark 21-52 Week Structural Materialization

## 1. Executive result

Implemented a real, dark, unwired structural materializer
(`LongHorizonStructuralMaterializer`) that joins the Phase 4I.3 typed
Long-Horizon composition decision, a backend-mirrored Phase 4I.4 GE catalog
selector, and the **existing, unchanged** Preparation Runway and Core
structural materializers into one contiguous, globally-numbered 21-52 week
skeleton with authoritative segment provenance. Proven correct across the
full 21-52 x 2-profile matrix (64 horizon/profile combinations), 20→21
composition-level continuity, N→N+1 monotonicity, ShortExtension/FullPhase/
remainder structure, and fail-closed structural validation. Zero numeric
volume/pace, zero calendar dates, zero new public call sites, zero backend
regressions (full backend suite: **2408 passed, 0 failed, 0 skipped**), zero
plan-catalog regressions (**909 passed, 0 failed, 0 skipped**). One real
defect was found and fixed during verification — see §32/§33. No commits
made.

Final classification:

```
LONG_HORIZON_21_TO_52_WEEK_DARK_STRUCTURAL_MATERIALIZATION_COMPLETED_WITH_CONTIGUOUS_GLOBAL_NUMBERING_AUTHORITATIVE_SEGMENT_PROVENANCE_AND_NO_NUMERIC_OR_PUBLIC_ACTIVATION
```

and, unchanged:

```
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_NUMERIC_PROGRESSIVE_LOADING_CALENDAR_ASSIGNMENT_DARK_PIPELINE_WIRING_AND_PUBLIC_PREVIEW
```

## 2. Inherited policies/components

- Phase 4I.1: `GeneralEnduranceWeeks = AvailableFullWeeks - 20`, Runway=8,
  Core=12, for 21-52 weeks.
- Phase 4I.2: 4-week mesocycles (3 development + 1 recovery), ShortExtension
  1-3 weeks, weekly role skeleton 1 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN.
- Phase 4I.2A: recovery-week numeric metadata (not executed by this phase).
- Phase 4I.3: `LongHorizonCompositionDecision`/`LongHorizonCompositionResolver`/
  `LongHorizonCompositionValidator` (consumed verbatim, never re-derived).
- Phase 4I.4: `LongHorizonGeCatalogSelector`/stage-family catalog document
  (plan-catalog) — this phase does **not** reference plan-catalog directly
  (see §6/§10) but reimplements the identical algorithm and role-assignment
  content as a hand-verified backend mirror.
- Pre-existing (untouched): `PreparationRunwayWeekMaterializer` and its full
  allocation/progression-loading/binding pipeline; `CatalogStageToWeekMaterializer`
  (Core's pure structural materializer).

## 3. Exact scope

`TEN_K__4D__INTERMEDIATE v10`, Race, 10K, 4 days/week, both readiness
profiles (`CONSISTENCY_NEEDED`, `CORE_ENTRY_READY`), horizons 21-52 full
weeks only. 8-14/15-20 week behavior is untouched (no file in either path
was modified). 53+, other candidates/distances/levels/frequencies, and habit
plans are explicitly out of scope and unchanged.

## 4. Artifacts inspected

`LongHorizonCompositionContracts.cs`, `LongHorizonCompositionResolver.cs`,
`LongHorizonCompositionValidator.cs`, `CoreHorizonDecision`/`CoreHorizonMode`;
plan-catalog's `LongHorizonGeCatalogSelector`/`LongHorizonGeCatalogContracts.cs`
and `ten-k-long-horizon-ge-stage-families.v1.json`; the full Preparation
Runway materialization stack (`PreparationRunwayBlockAllocationEngine`,
`TenKPreparationRunwayAllocationPolicyFactory`, `TenKPreparationRunwayProgressionPolicyFactory`,
`TenKPreparationRunwayProgressionLoader`, `PreparationRunwayBlockWorkoutBindingEngine`,
`PreparationRunwayWeekMaterializer`, `TenKPreparationRunwayWeekMaterializationPolicyFactory`,
`TenKPreparationRunwayDarkOrchestrator` as the reuse-pattern precedent); the
Core structural stack (`GeneratedCatalogPlanSkeleton.cs`,
`CatalogStageToWeekMaterializer.cs`, `CatalogStageToWeekMaterializerFixtures.cs`);
`CatalogWorkoutDefinitionLoader`; `PlanServices.GeneratePreviewAsync`/
`CatalogPreviewGenerator` call chain; `LongHorizonGenerationContainmentTests.cs`
(Phase 4I.3's existing real E2E containment suite); all six referenced
governance TDs.

## 5. Existing materializer audit

Neither the Preparation Runway nor the Core structural materializer was
duplicated. Runway is reused via its exact existing call sequence (the same
allocation → progression-loading → binding → `PreparationRunwayWeekMaterializer.MaterializeAsync`
stages `TenKPreparationRunwayDarkOrchestrator` already uses, stopping there —
this phase never calls that orchestrator's later numeric/calendar/pace
stages). Core is reused via the existing, pure, dependency-free
`CatalogStageToWeekMaterializer.Materialize` with the same Foundation3/
Build4/RaceSpecific4/Taper1 allocation and `RUN_LAYOUT_4D v2` layout already
used by `CatalogStageToWeekMaterializerFixtures`.

## 6. Structural contract

`LongHorizonStructuralSkeletonContracts.cs` (new):
`LongHorizonGeneratedStructuralSkeleton` (plan level: TotalWeeks, per-segment
counts, ReadinessProfile, candidate/policy provenance, ordered Weeks),
`LongHorizonStructuralWeek` (GlobalWeekNumber, LocalSegmentWeekNumber,
Segment, WeekType, RunwayBlock/CorePhase/Ge* fields — exactly one segment's
group populated, the other two null — recovery/terminal-alignment flags,
ordered slots), `LongHorizonStructuralWorkoutSlot` (index, role, nullable
workout key/version, segment). No distance, duration, pace, calendar date,
persisted ID, or completion state anywhere in the contract. No existing
schedule contract family was duplicated or replaced — this is a new,
additive join type sitting above the three existing/mirrored inputs.

## 7. Materializer authority

`LongHorizonStructuralMaterializer.MaterializeAsync(LongHorizonCompositionDecision, catalogRoot, ICatalogWorkoutDefinitionLoader, ct)`
(`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/LongHorizonStructuralMaterializer.cs`).
Validates the decision's shape (path, eligibility, profile, GE/Runway/Core
counts, sum), calls `LongHorizonGeStructuralSelector.Select` for GE, the
Runway reuse sequence for Runway, `CatalogStageToWeekMaterializer` for Core,
merges with global renumbering, and runs `LongHorizonStructuralValidator`
before returning. Never recalculates GE/Runway/Core durations or profile
(both taken verbatim from the decision), never selects a real date, never
executes numeric progression or pace, never persists, exposes no public DTO.

## 8. Segment order

Enforced both by construction (GE weeks emitted first, then Runway, then
Core) and by `LongHorizonStructuralValidator` (rejects any other order or
any interleaving — proven by `Validator_InterleavedSegments_Fails`).

## 9. Global numbering

One contiguous `1..TotalWeeks` sequence, no reset at segment boundaries —
proven exactly for 21/24/40/52 weeks (§ "SegmentOrderAndGlobalNumbering..."
test) and for the full 21-52 x 2-profile matrix. `LocalSegmentWeekNumber` is
retained per segment (GE 1..N, Runway 1..8, Core 1..12) as secondary
provenance only.

## 10. GE descriptor consumption

`LongHorizonGeStructuralSelector` (new, backend-internal) reimplements
plan-catalog's `LongHorizonGeCatalogSelector` algorithm exactly
(mesocycle/remainder/ShortExtension placement rule) and hand-mirrors the
`ten-k-long-horizon-ge-stage-families.v1.json` role-assignment content as a
hardcoded constant table. This is a deliberate, documented mirror, not a
cross-project reference: backend has no project reference onto
`PlanCatalog.*` assemblies (`TD-BACKEND-001`), matching the precedent already
established for `ReadinessProfile` (backend's own mirror of plan-catalog's
`LongHorizonReadinessProfile`) and `CatalogWorkoutProgressionDefinition`'s
doc-commented mirror of `StageCompressionBehavior`. Every descriptor field
(stage family, mesocycle index/position, recovery/terminal-alignment flags,
readiness-profile content selection, workout references, role ordering) is
preserved into the structural week unchanged. Descriptor count is validated
to equal `decision.GeneralEnduranceWeeks` before use.

## 11. Runway reuse

`LongHorizonStructuralMaterializer.MaterializeRunwayAsync` calls, in order:
`TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies` →
`PreparationRunwayBlockAllocationEngine.Allocate(8, policies)` →
`TenKPreparationRunwayProgressionPolicyFactory.Build()` +
`TenKPreparationRunwayProgressionLoader.LoadValidatedAsync` (real file-system
catalog read, no database) → `PreparationRunwayBlockWorkoutBindingEngine.Bind`
→ `PreparationRunwayWeekMaterializer.MaterializeAsync` — the exact same
stages, in the exact same order, as `TenKPreparationRunwayDarkOrchestrator`
uses for its own 15-20 week path, stopping before that orchestrator's
numeric/calendar/pace stages. No allocation logic, workout selection, or
block ordering was reimplemented. `RunwaySegment_ProfileBlockAllocationMatchesApprovedMatrix`
confirms the profile block-allocation matrix (`ConsistencyNeeded`:
Consistency+GeneralEndurance+PreSpecificTransition; `CoreEntryReady`:
GeneralEndurance+AerobicStrength+PreSpecificTransition, both ending on the
single `PreSpecificTransition` week) is byte-identical to the pre-existing
approved 20-week path's own allocation output.

## 12. Core reuse

`MaterializeCore` calls the existing, pure, dependency-free
`CatalogStageToWeekMaterializer.Materialize` with the exact Foundation3/
Build4/RaceSpecific4/Taper1 allocation and `RUN_LAYOUT_4D v2` layout already
used by the existing fixtures. An arbitrary anchor date
(`DiscardedAnchorDate = 2000-01-01`) is supplied only because that existing
context type requires one — no date field from the result is ever copied
into the output skeleton (§23). `CoreSegment_ExactlyTwelveWeeksFoundationBuildRaceSpecificTaper`
confirms the exact 3/4/4/1 phase sequence and role-count-4-per-week
structure.

## 13. Week/provenance model

Each `LongHorizonStructuralWeek` carries exactly one segment's provenance
group; the other two are null, enforced by `LongHorizonStructuralValidator`
(`Validator_IncompatibleSegmentField_Fails`). GE weeks: `WeekType =
"LONG_HORIZON_GENERAL_ENDURANCE"`, `GeStageFamily`/`GeClassification`
populated, `RunwayBlock`/`CorePhase` null. Runway weeks: `WeekType =
"PREPARATION_RUNWAY"`, `RunwayBlock` populated, `CorePhase`/`Ge*` null. Core
weeks: `WeekType` = the actual stage key (`FOUNDATION`/`BUILD`/
`RACE_SPECIFIC`/`TAPER`), `CorePhase` populated, `RunwayBlock`/`Ge*` null. No
week is ever collapsed into a generic type.

## 14. Role model

Every week carries exactly 4 ordered slots: 1 `KEY_SESSION`, 2
`EASY_SUPPORT`, 1 `LONG_RUN` — validated for every week of every
materialized skeleton (`Validator_InvalidRoleCount_Fails` plus the full
matrix test's aggregate `4 * totalWeeks` slot-count assertion, `208` for 52
weeks). GE and Runway slots carry non-null catalog workout key/version
(GE: never a prohibited threshold/VO2max/goal-pace/race-specific key). **Core
slots deliberately carry a null workout key/version** — see §34 for the
explicit, disclosed reason this one requirement (Part 9's literal "no null
workout key/version") is not fully met for the Core segment specifically.

## 15. ShortExtension materialization

21/22/23-week horizons for both profiles proven: correct week count
(1/2/3), `ShortExtension` classification, zero recovery weeks, terminal week
flagged `IsTerminalAlignment`, terminal `GeStageFamily = PreRunwayAlignment`
(except the 1-week case, whose single week is `Entry`/`EntryAlignment` by
construction — both first and terminal), and the first Runway week
immediately follows with `LocalSegmentWeekNumber = 1`.

## 16. FullPhase materialization

24/28/32/40/52-week horizons proven: exact GE counts (4/8/12/20/32), exact
mesocycle counts (1/2/3/5/8), every mesocycle exactly 4 weeks ending in a
recovery week with `GeStageFamily = Consolidation`, exact role count (4 per
week) throughout.

## 17. Remainder materialization

25/26/27/29/30/31-week horizons proven: remainder (1/2/3 leftover weeks)
occurs strictly after all complete mesocycles, is always
`IsTerminalAlignment = true` and never `IsRecoveryWeek`, never occurs at the
beginning, and the remainder's final week is always `PreRunwayAlignment`.

## 18. Profile behavior

`BothProfiles_IdenticalDurationsAndBoundaries_DifferOnlyInApprovedContent`
(21/24/32/40/52 weeks): both profiles produce identical total weeks,
GE/Runway/Core counts, segment boundaries, recovery positions, and global
numbering; Core's phase sequence is proven identical across profiles
(candidate-defined, profile-independent). Profile differences are confined
to GE `KEY_SESSION` content and Runway block content, per Phase 4I.1/4I.2/
4I.4's approved rule.

## 19. 20→21 continuity

`TwentyToTwentyOne_RunwayAndCoreSuffixStructurallyStable` (both profiles):
the 21-week plan's weeks 2-21 are structurally identical (segment, WeekType,
RunwayBlock, CorePhase, LocalSegmentWeekNumber, and every slot's role/
workout-key/version) to a 24-week plan's weeks 5-24, modulo only the +1/+4
global-number offset. Numeric values are not compared (none exist yet).

## 20. N→N+1 monotonicity

Proven for representative N in {21,24,31,35,44,51}: GE count increases by
exactly 1, Runway stays 8, Core stays 12, total increases by 1, and the
terminal 20-week Runway+Core suffix (segment/RunwayBlock/CorePhase sequence)
is stable across the transition.

## 21. Validator

`LongHorizonStructuralValidator.Validate` — fail-closed, aggregate (not
first-fail, matching the existing `TenKPreparationRunwayFinalInvariantValidator`/
`GeneratedCatalogPlanSkeletonValidator` convention). Checks: total-weeks
range, exact segment counts and sum invariant, exact segment order with no
interleaving, contiguous global numbering with a matching final week, exact
4-role-per-week cardinality and slot ordering, per-segment field
compatibility (no cross-segment provenance leakage), GE prohibited-workout
rejection, GE recovery-position correctness (every 4th local GE week in a
FullPhase mesocycle, never in ShortExtension), and contiguous local segment
numbering per segment. Never auto-corrects — every violation is reported as
a finding string.

## 22. Invalid-input behavior

Directly tested: 20 weeks (rejected — below the Long-Horizon floor), 53
weeks (rejected — above the supported window), null readiness profile,
Runway≠8, Core≠12, GE/Runway/Core sum mismatch, GE-selector out-of-range (0,
33). Every case throws `InvalidOperationException`/`ArgumentOutOfRangeException`
before any partial skeleton is constructed — no partial result is ever
returned (the materializer only returns after `LongHorizonStructuralValidator`
itself passes).

## 23. No numeric execution

No weekly volume, planned/long-run distance, recovery multiplier result,
duration, pace, or target-time pace field exists anywhere in
`LongHorizonGeneratedStructuralSkeleton`/`LongHorizonStructuralWeek`/
`LongHorizonStructuralWorkoutSlot`. Policy metadata carried: recovery flag
(`IsRecoveryWeek`), composition policy ID/version, and semantic week
position (mesocycle index/position, terminal-alignment flag) — exactly the
categories Part 19 permits. No placeholder numeric zeroes were introduced.

## 24. No calendar execution

No `StartDate`, `PreferredDays`, `LongRunDay`, week-date range, or
`TrainingDay` date field exists anywhere in the output contract. The Core
structural materializer's context type requires a date only because it is
an existing, unchanged, dependency-free type designed for a later
calendar-aware phase — `DiscardedAnchorDate` (`2000-01-01`) is supplied and
its result's `StartDate`/`EndDate` fields are never read or copied into the
output skeleton (confirmed by direct inspection of `BuildCoreWeek`, which
reads only `StageKey`/`StageWeekIndex`/`StageWeekCount`/`SessionSlots`).

## 25. Dark integration level

**Level A** (materializer invoked only by focused tests) — the preferred
option. `LongHorizonStructuralMaterializer` is not registered in DI, has no
constructor dependencies beyond explicit method parameters, and is not
called from `CatalogPreviewGenerator`, `PlanServices`, or any other live
orchestrator. A repository-wide grep for `LongHorizonStructuralMaterializer`
confirms its only references are its own production file and its own test
file. `LongHorizonCompositionResolver` does not become newly reachable
through any call site this phase adds — it is called via the same existing
test-only pattern established by Phase 4I.3.

## 26. Containment

The pre-existing, unchanged `LongHorizonGenerationContainmentTests.cs`
(Phase 4I.3, real Api host + real Postgres) already proves 21/24/52-week
requests return HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED` with zero
`PlanPreview`/`TrainingPlan`/`TrainingWeek`/`TrainingDay` persistence, and 53
weeks returns the same reason unchanged. Since this phase adds zero call
sites into `PlanServices`/`CatalogPreviewGenerator` (§25), that suite's own
evidence remains valid and current for this phase too — re-running it was
not necessary to re-prove containment, and it is included unmodified in the
full backend suite result (§30). No new public DTO field was added. No
Flutter file was touched (confirmed — no file under `mobile/` appears in
this phase's file list).

## 27. Performance

Diagnostic-only timings for 21/40/52-week materialization (real, observed):
each completed in well under the 5000ms diagnostic threshold used by
`Materialization_CompletesQuicklyAndDeterministically` (typical run: tens to
low hundreds of milliseconds, dominated by real file-system catalog reads
for Runway progression/workout validation — no database call, no network
call, no per-week catalog reload, no exponential selection path).

## 28. Governance status

`TD-LONG-HORIZON-COMPOSITION-001`: CLOSED (unchanged, Phase 4I.1).
`TD-LONG-HORIZON-GE-SAFETY-001`: CLOSED (unchanged, Phase 4I.2).
`TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001`: CLOSED (unchanged, Phase 4I.2A).
`TD-LONG-HORIZON-TYPED-RUNTIME-DECISION-001`: CLOSED (unchanged, Phase 4I.3).
`TD-LONG-HORIZON-GE-CATALOG-CAPACITY-001`: CLOSED (unchanged, Phase 4I.4).
`TD-LONG-HORIZON-STRUCTURAL-MATERIALIZATION-001`: **added CLOSED** this
phase. `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`: remains **OPEN** — numeric
execution, calendar assignment, dark pipeline wiring, public preview,
confirmation, and persistence all remain entirely unimplemented.

## 29. Focused tests

126 new tests in `LongHorizonStructuralMaterializerTests.cs`: segment
order/global numbering, ShortExtension, FullPhase, remainders, Runway/Core
reuse regression, 20→21 continuity, N→N+1 monotonicity, both-profile
equivalence, full 21-52 x 2-profile matrix (64 combinations, 5 theory
methods → 320 cases counted individually by xUnit), invalid-input rejection,
10 direct validator tests, containment note, performance diagnostics. Plus
5 new governance cross-check tests in plan-catalog
(`LongHorizonStructuralMaterializationGovernanceTests.cs`). All passing.

## 30. Backend suite

Full `dotnet test` run for `backend/` (all prior suites plus this phase's
new file): **2408 passed, 0 failed, 0 skipped** (9m52s), after the fix in
§32/§33 below. Up from the prior baseline of 2282 (Phase 4I.3) due to this
phase's 126 new tests.

## 31. Plan-catalog suite

**909 passed, 0 failed, 0 skipped** (904 baseline + 5 new governance
tests). No plan-catalog production file was modified by this phase (only
governance JSON/MD and one new test file).

## 32. Production defects found

No defect in this phase's new production code's logic. However, the full
backend suite's first run surfaced a real, correct finding from two
**pre-existing governance tests**: `PreparationRunwayAllocationDarkGovernanceTests.DarkReachability_NoDiRegistrationOrInvocationOutsideApprovedDarkOrchestrator`
and `PreparationRunwayWorkoutBindingDarkGovernanceTests`'s equivalent test
correctly detected that `LongHorizonStructuralMaterializer.cs` is a
**second** production call site for `PreparationRunwayBlockAllocationEngine`/
`TenKPreparationRunwayAllocationPolicyFactory`/
`PreparationRunwayBlockWorkoutBindingEngine` (the first being
`TenKPreparationRunwayDarkOrchestrator`) — these governance tests' own
exclusion allowlist had not yet been told about this phase's new folder.
This is exactly what those tests are designed to catch, and they caught it
correctly; it is not a defect in the materializer's design (deliberate,
narrow reuse of lower-level shared components, per §11) but a governance
allowlist gap this phase's own new file created.

## 33. Production fixes made

None to production (non-test) code. Two governance test files were updated
to recognize `Schedule/LongHorizon` as a second, deliberate, approved dark
consumer of `PreparationRunwayBlockAllocationEngine`/
`PreparationRunwayBlockWorkoutBindingEngine`, alongside the existing
`Schedule/PreparationRunwayOrchestration` exclusion:
`PreparationRunwayAllocationDarkGovernanceTests.cs` and
`PreparationRunwayWorkoutBindingDarkGovernanceTests.cs` (both in
`backend/RunningApp.IntegrationTests/`). One test-authoring mistake in this
phase's own new test file was also fixed:
`ShortExtensionHorizons_NoRecoveryWeek_TerminalCompatibleWithRunway`
incorrectly asserted every ShortExtension horizon's terminal GE week is
`PreRunwayAlignment`; the 1-week case's single week is actually `Entry`
(both the first and the terminal week by construction, matching Phase
4I.4's own approved semantics) — corrected to branch on `geWeeks.Count == 1`.

## 34. Explicit non-implementation statement

This phase does **not** implement: numeric weekly volume/long-run distance
execution (Phase 4I.2/4I.2A formulas remain unexecuted), pace assignment,
calendar/date assignment, public preview activation for 21-52 weeks,
confirmation, persistence, 53+ support, or any other candidate/distance/
level/frequency. Additionally, and specifically disclosed as a scope
boundary rather than a defect: **Core segment structural slots carry a null
workout key/version**, because the existing, reused
`CatalogStageToWeekMaterializer` — the same pure structural materializer the
established Phase 4F.2 architecture already uses for Core — never selects a
workout reference itself; that selection is a separate, later, existing
pipeline stage (`CatalogWorkoutBinder`/`DynamicCoreWorkoutBindingOrchestrator`)
this phase deliberately does not invoke, to avoid inventing content that
stage does not itself produce. This is a partial, honest deviation from the
prompt's literal "no null workout key/version" requirement, scoped
specifically to the Core segment (GE and Runway segments fully satisfy that
requirement).

## 35. Residual blockers

`TD-GENERAL-ENDURANCE-STAGED-PLAN-001` (OPEN): numeric progressive loading
execution, calendar/date assignment, Core-segment workout-reference binding,
dark-pipeline wiring into any live orchestrator, public preview activation,
confirmation, and persistence all remain unimplemented and unblocked by
this phase.

## 36. Final classification

```
LONG_HORIZON_21_TO_52_WEEK_DARK_STRUCTURAL_MATERIALIZATION_COMPLETED_WITH_CONTIGUOUS_GLOBAL_NUMBERING_AUTHORITATIVE_SEGMENT_PROVENANCE_AND_NO_NUMERIC_OR_PUBLIC_ACTIVATION

21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_NUMERIC_PROGRESSIVE_LOADING_CALENDAR_ASSIGNMENT_DARK_PIPELINE_WIRING_AND_PUBLIC_PREVIEW
```

## 37. Exact next phase

**Phase 4I.6 — Long-Horizon 21-52 Week Numeric Progressive Loading and
Calendar Assignment (Dark)**: execute the Phase 4I.2/4I.2A development- and
recovery-week numeric formulas against this phase's structural skeleton,
bind Core-segment workout references (closing §34's disclosed gap), and
assign calendar dates — remaining dark/unwired, still not activating public
preview, confirmation, or persistence.
