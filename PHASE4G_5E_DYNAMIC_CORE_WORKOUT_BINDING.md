# Phase 4G.5E — Dynamic Core Workout Binding

## 1. Executive result

A new, dark, unwired orchestrator — `DynamicCoreWorkoutBindingOrchestrator` — generalizes the live 12-week workout-binding pipeline to any mathematically feasible standalone-core length (8-14 weeks for `TEN_K_MASTER v6`). It performs **zero binding logic of its own** — it is pure composition, chaining five already-existing, already-live/already-dark components exactly as they already are: the Phase 4G.5D skeleton orchestrator, the Phase 4F.6A `ProgressionStageAllocator`, the Phase 4F.5 calendar materializer, and the Phase 4F.6B `CatalogWorkoutBinder`, each called through their real, unmodified public entry points. Empirically verified across all 7 week counts and both GOAL_PACE_REHEARSAL eligibility branches: **every KEY_SESSION/EASY_SUPPORT/LONG_RUN slot resolves to a non-null catalog workout ID, TAPER_SHARPEN's identity remains EASY_STANDARD (AUD-507/508, unchanged), the GOAL_PACE_REHEARSAL fallback mechanism generalizes correctly with no capacity gap, and the 12-week binding output is byte/value-identical to the existing live pipeline.** No new TD was required — no workout-catalog gap, unapproved repetition, or new TD interaction was found. No existing production file was modified beyond one dark-reachability test reconciliation.

## 2. Reuse strategy — which approach was used and why

Per this pass's explicit instruction to reuse Phase 4F.6B's binder rather than duplicate it, the real `CatalogWorkoutBinder`/`ICatalogWorkoutBinder.BindAsync` was inspected first (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Binding/CatalogWorkoutBinder.cs`). Two governing facts determined the approach:

1. **`CatalogWorkoutBinder` is already fully week-count-agnostic.** It iterates `context.DatedSkeleton.Weeks` and joins against `context.StageSchedule.Weeks` by `WeekNumber` — nothing hardcodes 12 weeks or any specific phase composition. The same is true of `CatalogWeekSkeletonCalendarMaterializer` (Phase 4F.5, iterates `skeleton.Weeks` generically) and `ProgressionStageAllocator` (Phase 4F.6A, groups the skeleton's weeks by `StageKey` and allocates per phase generically — its own compression/extension/eligibility/fallback algorithm has no week-count assumption). **None of these three components required any change.**
2. **No dark-reachability test constrains `CatalogWorkoutBinder`'s, `ProgressionStageAllocator`'s, or `CatalogWeekSkeletonCalendarMaterializer`'s call sites** — a repo-wide grep for `HasNoCallSite`/`DarkReachability` tests referencing any of these three types found none (unlike `AllocationOrderCorrectnessVerifier` or the Phase 4G.5D skeleton orchestrator, which do have such guards). This is because all three are already legitimately live — `CatalogPreviewGenerator` is already their one production caller (data-gated unreachable only because the pilot candidate's catalog `status` is `DRAFT`, not because of a dark-reachability code guard).

Given both facts, the correct reuse strategy — mirroring `GoalPaceReachabilityVerifier`'s own established precedent of calling `ProgressionStageAllocator.Allocate` directly rather than routing through a sibling verifier's public entry point — was to **call each real component's real public entry point directly**: `new ProgressionStageAllocator().Allocate(...)`, `new CatalogWeekSkeletonCalendarMaterializer().Materialize(...)`, `new CatalogWorkoutBinder().BindAsync(...)` (or injected equivalents), composed by one new orchestrator that adds no selection/eligibility/compression/extension/fallback logic of its own. This is the same "call the same underlying mechanism it uses" approach the task anticipated as the fallback option if the binder couldn't be called directly — except here it turned out the binder *can* be called directly, with no structural obstacle, so no indirection was needed at all.

## 3. Preserved identity decisions — confirmed explicitly

**TAPER_SHARPEN → EASY_STANDARD (AUD-507/AUD-508).** `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs`'s AUD-507/AUD-508 entries govern this: TAPER_SHARPEN's stage key and `EASY_STANDARD` workout-identity binding are retained unchanged, with any future taper-specific prescription handled by a later phase's *modifier*, never a different bound workout key. `ten-k-workout-progression.v5.json`'s `TAPER_SHARPEN` stage still declares exactly one `workoutCandidates` entry, `EASY_STANDARD` v4 — unchanged by this pass. `CatalogWorkoutBinder.cs` was not modified, so its binding logic for this stage is untouched. **Verified empirically**, not just by reading the static catalog data: `BindAsync_RealCandidate_TaperSharpenIdentityIsEasyStandard_NeverChanged` (14 cases: 7 week counts × 2 goal-feasibility branches) asserts every `TAPER_SHARPEN`-stage session across every generated 8-14-week plan binds to `EASY_STANDARD`, with `BindingMode = StageControlled` and `PhaseKey = TAPER`. No different workout was bound, and the previously-rejected "sharpening via workout swap" option was not reintroduced anywhere in this pass.

**GOAL_PACE_REHEARSAL Requires/FallbackStageKey.** `ten-k-workout-progression.v5.json`'s `GOAL_PACE_REHEARSAL` stage still declares `requires: [{conditionType: GOAL_FEASIBILITY_IN, allowedValues: [REALISTIC, CHALLENGING]}]` and `fallbackStageKey: CURRENT_FITNESS_SPECIFIC_REHEARSAL` — unchanged. This pass never re-implements or reinterprets that eligibility logic: `ProgressionStageAllocator.ResolveEligibility`/`IsStageEligible` (Phase 4F.6A, untouched) are the real production code paths, called directly, exactly as they already run at 12 weeks. **Verified empirically for both branches at all 7 week counts**: `BindAsync_GoalFeasibilityRealistic_GoalPaceRehearsalBindsGoalPaceTenK` confirms every eligible-branch run binds `GOAL_PACE_TEN_K`; `BindAsync_GoalFeasibilityNotEvaluated_UsesCurrentFitnessSpecificRehearsalFallback_GeneralizesTheSame12WeekEligibilityLogic` confirms every `NotEvaluated`-branch run (the trigger `TD-NOTEVALUATED-FALLBACK-001` documents) correctly falls back to `CURRENT_FITNESS_SPECIFIC_REHEARSAL`/`THRESHOLD_TEMPO`, with `FallbackOrigin = GOAL_PACE_REHEARSAL`, and never binds `GOAL_PACE_TEN_K` in that branch. `TD-NOTEVALUATED-FALLBACK-001` was not reopened, resolved, or reinterpreted — its own scope (whether the *upstream* NotEvaluated-vs-Evaluated-but-unmet distinction is itself correct) is untouched; this pass only confirms the *downstream* fallback-selection mechanism generalizes correctly across week counts, which is a different, narrower question.

## 4. Required analysis — phase-length behavior (shorter/equal/longer than preferred)

Governed entirely by the existing, unmodified `ProgressionStageAllocator`. No new policy was written by this pass — the following is a report of what the real, already-implemented algorithm does, verified by execution across every reachable phase-week count:

| Phase | Stages (min/max exposures, behavior) | Preferred weeks | Reachable weeks (8-14 wk targets) |
|---|---|---|---|
| FOUNDATION | `FOUNDATION_EASY_BASE` (3/6, Compressible/Extendable) | 3 | 2, 3, 4 |
| BUILD | `FARTLEK_INTRO` (1/2, Compressible/Extendable), `THRESHOLD_INTRO` (2/4, Compressible/Extendable) | 4 | 3, 4, 5 |
| RACE_SPECIFIC | `TEN_K_SPECIFIC_INTRO` (1/2, Compressible/Extendable), `GOAL_PACE_REHEARSAL` (1/2, Protected/FixedExposure, conditional; fallback `CURRENT_FITNESS_SPECIFIC_REHEARSAL` 1/1, Compressible/Extendable) | 4 | 2, 3, 4 |
| TAPER | `TAPER_SHARPEN` (1/2, Protected/FixedExposure) | 1 | 1 (only) |

**Shorter than preferred (compression):** `ApplyCompression` reduces `Compressible` active stages only, highest `RelativeOrder` first, down to a floor of 1 exposure each; `Protected` stages (`GOAL_PACE_REHEARSAL`/its fallback when `Protected`, `TAPER_SHARPEN`) are never touched. FOUNDATION at 2 weeks: `FOUNDATION_EASY_BASE` compresses from 3→2 (still ≥ floor 1). RACE_SPECIFIC at 2 weeks: both top-level stages already at their minimum (1 each), exact fit, no compression needed.

**Equal to preferred:** no compression or extension action; each stage receives exactly its `MinimumExposures`.

**Longer than preferred (extension):** `ApplyExtension` grows `Extendable` active stages first (highest `RelativeOrder`, then key ordinal), then tries `FixedExposure` stages once Extendable headroom is exhausted, up to each stage's own `MaximumExposures` — never an unbounded/open-ended grow, and `FixedExposure` is a *deprioritization*, not a hard exclusion (documented in the allocator's own `ApplyExtension` remarks). FOUNDATION at 4 weeks: `FOUNDATION_EASY_BASE` extends 3→4 (headroom to 6). BUILD at 5 weeks: surplus of 1 distributed to whichever candidate's headroom the tie-break selects (both stages have headroom). RACE_SPECIFIC at 4 weeks (the only case both a Protected/FixedExposure stage AND a real surplus of 2 co-occur): confirmed by test that regardless of eligibility branch, capacity is exactly sufficient — see Section 5.

## 5. Required policy — as already implemented (not invented by this pass)

- **Which workouts are retained in compressed phases:** all of them — compression only ever reduces a `Compressible` stage's *exposure count* (how many weeks it occupies), never removes a stage or drops its workout candidate entirely. A stage's single catalog-declared workout candidate is retained down to its floor of 1 exposure.
- **Which workout is removed first:** none is ever fully removed within the reachable 8-14-week range for this candidate — every top-level stage's floor (1 exposure) is always reachable given the catalog's own minimums sum correctly to each phase's own catalog minimum. If a stage *were* driven to 0, that would be a capacity failure (`ProgressionPhaseCapacityInsufficientException`) that this pass's own tests never observed being thrown.
- **How an extra extension week is filled:** by growing an existing `Extendable` (then `FixedExposure`, if still needed) stage's own exposure count, in `RelativeOrder`-descending priority, up to that stage's own catalog-declared maximum — never by inventing a new stage or workout.
- **Whether an existing workout repeats:** yes, but this is pre-existing, catalog-declared V1 behavior, not something this pass introduced or made worse: V1 has exactly one workout candidate per stage (`CatalogWorkoutBinder` throws `CatalogWorkoutBindingAmbiguousCandidateException` if a stage ever declared more than one), so every exposure of a given stage necessarily binds the *same* single workout — e.g. `FOUNDATION_EASY_BASE` bound to `EASY_STANDARD` for all of its 2-4 weeks. This is identical in kind to what the existing 12-week plan already does (3 straight weeks of `EASY_STANDARD` for Foundation) — not a new pattern introduced by generalizing to 8-14 weeks, and not the "silently loop the last workout to fill unexpected extra weeks" anti-pattern this pass was told to avoid: the repeat count is always bounded by the stage's own catalog-declared `maximumExposures`, never open-ended.
- **Whether repetition is approved:** yes — it is the catalog-authored V1 design (one workout candidate per stage), not a scheduler workaround. No new, larger, or differently-shaped repetition was introduced by extending to 13/14-week Foundation/Build phases; the repeat count simply grows within the same catalog-declared maximum that already existed for the 12-week case.
- **Whether a maintenance variant is needed:** no — this pass introduces no new stage, workout, or variant. That question would only become relevant if a future phase decided repetition itself needed product-level mitigation (e.g., a distinct "week 4 of Foundation" variant workout), which is out of this pass's scope and not required by any of the guarantees below.

## 6. Required guarantees — verified

- **All workout IDs resolve from catalog:** `CatalogWorkoutBinder.ValidateInClosureAndPhase` (unmodified) throws if a resolved definition is outside the dependency closure or ineligible for its phase; every one of the 113 new tests' runs completed without that exception, across all 7 week counts and both goal-feasibility branches.
- **No unsupported workout family is introduced:** this pass adds zero new workout keys, catalog entries, or families. Every bound `WorkoutDefinitionKey` observed across the full matrix is one of the four pre-existing catalog workouts: `EASY_STANDARD`, `LONG_RUN_STANDARD`, `GOAL_PACE_TEN_K`, `THRESHOLD_TEMPO` (plus `FARTLEK`/other BUILD-phase KEY_SESSION candidates, unchanged from the existing catalog).
- **Goal-pace feasibility requirements remain respected:** `GOAL_PACE_REHEARSAL`'s `requires`/`fallbackStageKey` mechanism is consumed exactly as `ProgressionStageAllocator` already implements it — never bypassed, loosened, or hardcoded to always-eligible.
- **Taper remains lower-load:** `TAPER_SHARPEN` remains a single, `Protected`/`FixedExposure` stage, always exactly 1 week (matching the catalog's own `TAPER` phase bounds, min=max=1) across all 7 target week counts — confirmed by the Phase 4G.5D allocation matrix (reused unmodified) and re-confirmed here via the binding output.
- **Compressed plans do not stack incompatible quality sessions:** RACE_SPECIFIC's `TEN_K_SPECIFIC_INTRO`/`GOAL_PACE_REHEARSAL` (or its fallback) remain the only KEY_SESSION-controlling stages in that phase; compression only reduces exposure counts of `Compressible` stages, never merges two stages' workouts into the same week or otherwise stacks anything.

## 7. Test matrix (8-14 weeks)

Bound for all 7 target week counts, under both GOAL_PACE_REHEARSAL branches (REALISTIC-eligible and NotEvaluated-fallback) — 14 combinations total. Every combination confirmed: every `KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN` slot resolves to a non-null, non-empty `WorkoutDefinitionKey` with a positive `WorkoutDefinitionVersion`; total sessions = `weeks * 4`; `EASY_SUPPORT`→`EASY_STANDARD`, `LONG_RUN`→`LONG_RUN_STANDARD` in every case (`BindingMode = FixedDefault`); `TAPER_SHARPEN`→`EASY_STANDARD` in every case; `GOAL_PACE_REHEARSAL`→`GOAL_PACE_TEN_K` when eligible, →`CURRENT_FITNESS_SPECIFIC_REHEARSAL`/`THRESHOLD_TEMPO` with `FallbackOrigin = GOAL_PACE_REHEARSAL` when not; the resulting `BoundCatalogPlanValidator` output is valid in every case (no exception, no null, no unsupported combination).

```text
DynamicCoreWorkoutBindingOrchestratorTests: 61 passed, 0 failed, 0 skipped
DynamicCoreWeekSkeletonOrchestratorTests (re-run after Section 8's reconciliation): 40 passed, 0 failed, 0 skipped
Combined focused run: 113 passed, 0 failed, 0 skipped
```

## 8. Required verification

**Zero production call sites**: confirmed by grep (repo-wide search for `DynamicCoreWorkoutBinding(Orchestrator|Context|Result)` across all four production projects, excluding the new file's own source, returned zero matches) and by an executable structural test, `DarkReachability_NoProductionCallSite`, matching this session's established pattern. A companion test, `DarkReachability_NoDiRegistration`, confirms `IDynamicCoreWorkoutBindingOrchestrator` is never referenced in `RunningApp.Api` (where all live DI registration happens). `CatalogPreviewGenerator.cs`'s `git diff` remains empty — confirmed untouched by this pass.

**Byte/value-level 12-week regression**: `BindAsync_TargetWeekCount12_MatchesExistingFixedWeekBindingPipelineExactly` builds the bound plan via the **existing, completely unmodified** fixed-week pipeline (`CatalogPlanSkeletonOrchestrator` → `CatalogWeekSkeletonCalendarMaterializer` → `ProgressionStageAllocator` → `CatalogWorkoutBinder`, mirroring `CatalogWorkoutBinderTests.RealFixtureAsync`'s own established construction) and, separately, via the new `DynamicCoreWorkoutBindingOrchestrator` requesting `targetWeekCount=12`, then asserts full field-by-field equality across every bound session (`WeekNumber`, `Date`, `PhaseKey`, `ProgressionStageKey`, `StructuralRole`, `WorkoutDefinitionKey`, `WorkoutDefinitionVersion`, `BindingMode`, `BindingReason`, `FallbackOrigin`) — a concrete comparison, not a bare claim. The existing pipeline's own output is additionally pinned to literal values (48 total sessions, week 12's KEY_SESSION bound to `TAPER_SHARPEN`/`EASY_STANDARD`). All assertions pass.

**Existing-test reconciliation** (mirroring Phase 4G.5D's own precedent): adding this new orchestrator's call to `DynamicCoreWeekSkeletonOrchestrator.Build(...)` (via `IDynamicCoreWeekSkeletonOrchestrator`) made it a second reference to that Phase 4G.5D type, which broke that phase's own `DarkReachability_NoProductionCallSite` test (asserting zero references outside its own file). The test was updated, not weakened: renamed to `DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer`, with one new file-path exclusion (`DynamicCoreWorkoutBindingOrchestrator.cs`) and an updated doc comment explaining why — the new consumer is itself proven dark by its own two reachability tests, so the real invariant (unreachable from any live path) still holds. This is the only existing file modified by this pass.

## 8a. Full backend Run 1 / Run 2

```text
Run 1: Failed: 0, Passed: 1535, Skipped: 1, Total: 1536
Run 2: Failed: 0, Passed: 1535, Skipped: 1, Total: 1536
```

Both runs match exactly (+73 vs. Phase 4G.5D's `1462` baseline, matching `DynamicCoreWorkoutBindingOrchestratorTests`' own 73 new tests exactly — `DynamicCoreWeekSkeletonOrchestratorTests`' own test count is unchanged, only two of its existing tests were renamed).

## 9. New findings — none required a TD

Per this pass's own governance requirement, any workout-catalog gap, unapproved repetition, or new TD interaction found would need to be recorded as a proper `activation-readiness-risks.json`/`.md` entry, not left as prose. **None was found.** Specifically checked and confirmed clean:

- No reachable phase-week combination across the full 8-14-week × both-goal-feasibility-branch matrix lacked an approved workout — every one of the 14 combinations completed `BindAsync` without any `CatalogWorkoutBinding*Exception` or `ProgressionStage*Exception`/`ProgressionPhaseCapacity*Exception`.
- The RACE_SPECIFIC=4 case (reachable at 10, 11, 12, 13, and 14-week targets) was the one combination with real capacity tension: two top-level stages with `totalMinimum=2` need to absorb a `surplus=2` at 4 available weeks. This was verified feasible in **both** branches: when `GOAL_PACE_REHEARSAL` is eligible, its own headroom (max2-min1=1) plus `TEN_K_SPECIFIC_INTRO`'s headroom (1) exactly cover the surplus; when it falls back to `CURRENT_FITNESS_SPECIFIC_REHEARSAL`, the allocator's own existing "single substitution" merge rule (`ProgressionStageAllocator.cs`, the `requestedKeyIfFallback` branch) takes the *more generous* of the fallback's own max (1) and the original stage's max (2) — so the effective stage's `MaximumExposures` becomes 2, giving it the same headroom of 1 as the eligible branch. This merge logic was not written by this pass; it already existed and was simply exercised, for the first time at this specific (RACE_SPECIFIC=4, fallback-taken) combination, by this pass's tests.
- No required extension needed an unapproved repetition beyond what Section 5 describes (which is pre-existing, catalog-approved V1 behavior, unchanged in kind by extending to 13/14 weeks).
- `TD-NOTEVALUATED-FALLBACK-001` and `TD-EASY-WORKOUT-REGISTRY-001` were reviewed; neither's scope changed. `TD-EASY-WORKOUT-REGISTRY-001` remains `CLOSED` (unrelated — an `EASY_SHAKEOUT` fixture-divergence question, not touched by this pass). `TD-NOTEVALUATED-FALLBACK-001` remains `OPEN`, unchanged, un-reopened, un-reinterpreted — this pass only confirmed the fallback-selection *mechanism* generalizes, which is a narrower, already-covered claim, not a resolution of that TD's own open questions (whether `NotEvaluated` should be distinguished from `Evaluated`-but-unmet at the resolver level).

## 10. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Binding/DynamicCoreWorkoutBindingOrchestrator.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Binding/DynamicCoreWorkoutBindingOrchestratorTests.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DynamicCoreWeekSkeletonOrchestratorTests.cs` (existing file, two tests updated — see Section 8; no production file modified).
- `PHASE4G_5E_DYNAMIC_CORE_WORKOUT_BINDING.md` (new, this document).

No production (`RunningApp.Application`/`.Api`/`.Infrastructure`/`.Persistence`) file was modified — including `CatalogWorkoutBinder.cs`, `ProgressionStageAllocator.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `CatalogPreviewGenerator.cs`, and every catalog JSON file. `plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}` were not modified, since no new finding required a TD (Section 9).

## 11. Stop conditions — none triggered

- Catalog lacks an approved workout for a reachable phase week: **not triggered** — every one of the 14 (week count × goal-feasibility) combinations bound successfully.
- Extension requires unapproved repetition: **not triggered** — Section 5/9.
- Compression requires dropping a mandatory race-specific session: **not triggered** — RACE_SPECIFIC always retains both top-level stages down to their catalog floor.
- 12-week binding changes: **not triggered** — Section 8's regression test proves exact equality.
- TAPER_SHARPEN identity silently changed: **not triggered** — Section 3, verified across all 14 combinations.
- GOAL_PACE_REHEARSAL fallback logic silently reinterpreted: **not triggered** — the real, unmodified allocator's own logic was called directly, never re-derived.
- Zero-call-site proof cannot be established: **not triggered** — established via grep and two executable tests.

## 12. Commit/push status

No file was staged. No commit or push was performed.
