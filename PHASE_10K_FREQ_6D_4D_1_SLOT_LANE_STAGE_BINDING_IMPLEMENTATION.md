# Phase 10K-FREQ.6D.4D.1 (Split A) — Dual-KEY Slot/Lane Identity & Per-Lane Progression Stage Binding Implementation

**Implementation phase executing Split A only of the FREQ.6D.4D-approved D1 architecture. No product decision, no dosage change, no profile selection, no ExactPrescriptionProjectionDependency, no bundle/RunningApp-consumer/persistence/Adaptation change, no public activation.**

Phase ID note: `MASTER_ROADMAP.md`/`PHASE_LEDGER.md` recorded only "Split A," no concrete numeric child ID. Following this engagement's established subphase-numbering convention (e.g. `FREQ.6D.4B.1`-`.4`, `FREQ.6D.4C.1`-`.5`, each a sequential implementation step of its parent), this phase is ledgered as `FREQ.6D.4D.1` — the first of the A-E implementation splits `FREQ.6D.4D §41` defined.

## 1. Preflight

`PHASE_LEDGER.md` row 72: `FREQ.6D.4D`, `ARCHITECTURE_DESIGN`, `DONE`, `FREQ6D4D_ARCHITECTURE_APPROVED_MULTI_PHASE_IMPLEMENTATION_REQUIRED`, confirmed. Commits `f0f19a6`, `7ea4caf` confirmed reachable from HEAD via `git merge-base --is-ancestor`. Starting HEAD `7ea4caf3ebf5822e8bebeff9a549b94017971b1d`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 12`. `git status --short` → ` m baseline_tmp` only. `git diff --check` → clean. `FREQ.6D.4D.1` confirmed not already a ledger row.

The complete real `PHASE_10K_FREQ_6D_4D_DUAL_KEY_STAGE_PROFILE_PRODUCTION_INTEGRATION_ARCHITECTURE.md` was re-read in full, along with `FREQ.6D.1A`/`1B` (the prior-art evidence that report re-verified). Every defect the architecture described was independently re-confirmed against current code before any modification, not assumed:

- `CatalogWorkoutBinder.cs:105` (pre-change): `var stageWeeksByNumber = context.StageSchedule.Weeks.ToDictionary(w => w.WeekNumber);` — keyed by `WeekNumber` alone. **Confirmed still present.**
- `ScheduledProgressionWeek.StructuralRole` (pre-change): `public string StructuralRole => KeySessionStructuralRole;` — a hardcoded constant, never derived. **Confirmed still present.**
- `CatalogSessionPrescriptionPlanner.cs:32-37` (pre-change): `keyOrdinal` computed as a transient loop-local `int` via `boundWeek.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole)` — a different sort key from the binder's `SlotOrderInWeek`. **Confirmed still present**, and confirmed still never persisted/returned anywhere.
- Zero production declarations of `CatalogWorkoutProgressionLane`/`LaneOrdinal`/`KeySessionLaneOrdinal` existed anywhere in `backend/` before this phase (repository-wide grep, matching `FREQ.6D.4D`'s own preflight finding).

## 2. Parent architecture

Per `FREQ.6D.4D §5-§26` (Option D1, selected): catalog-authored `LaneOrdinal` + bind-time structural ordinal derived deterministically from `SlotOrderInWeek` + independent `ProgressionStageAllocator` invocation per lane, reusing the existing algorithm verbatim. This phase implements exactly that, and only that — `§23-§28` of the Split-A prompt's own scope boundary (no profile selection, no projection dependency, no bundle/RunningApp/persistence/Adaptation change) was honored throughout; every file touched is enumerated in §5 below and none falls outside the `PLAN-CATALOG MODEL`/`BINDING MODEL`/`STAGE ALLOCATION ORCHESTRATION` categories `FREQ.6D.4D §38` predicted for this layer.

## 3. Current defect reproduction

Direct, real reproduction (not assumed): a new test, `OriginalDefectReproduction_ExactlyOneScheduleRowForTwoKeySlots_NewBinderRejectsRatherThanSilentlyCollapsing`, constructs exactly the shape the pre-Split-A `WeekNumber`-only dictionary would have accepted unconditionally for **both** KEY_SESSION slots in a 2-KEY week — a stage schedule with exactly one row for the week. Under the new `(WeekNumber, LaneOrdinal)`-keyed model, the second slot's lookup key `(WeekNumber, 1)` genuinely does not exist, so the binder now throws `CatalogWorkoutBindingMissingProgressionStageException` (message explicitly names `"lane 1"`) instead of silently reusing lane 0's stage/workout binding for both structural slots. This is the exact semantic difference Split A exists to guarantee, proven directly rather than by inspection alone.

## 4. Files inspected

`CatalogPlanSkeletonOrchestrator`/`CatalogStageToWeekMaterializer`/`CatalogWeekSkeletonCalendarMaterializer` (dynamic Core skeleton/materialization, RunLayout expansion — confirmed `GeneratedCatalogSessionSlotSkeleton.LayoutSlotKey` already encodes a `{role}_{occurrenceIndex}` per-role occurrence counter at expansion time, though this phase derives the structural ordinal independently at bind time per the architecture's own §7 rule, not by parsing that string); `V1CatalogWorkoutRoleBindingPolicy` (role→binding-mode mapping, unchanged); `ProgressionStageAllocator.cs` (full 557-line read, both before and after edit); `CatalogWorkoutBinder.cs` (full read); `CatalogWorkoutProgressionDefinition.cs`, `CatalogWorkoutProgressionLoader.cs`, `ProgressionStageScheduleContracts.cs`, `GeneratedCatalogStageScheduleValidator.cs`, `BoundCatalogPlanContracts.cs`, `CatalogSessionPrescriptionPlanner.cs`; `Freq4TwoKeyCardinalityGeneralizationTests.cs` (the established FREQ.4 precedent for hand-constructed synthetic N-KEY test input, since no `RUN_LAYOUT_5D` catalog artifact exists yet); `CatalogWorkoutBinderTests.cs`/`ProgressionStageAllocatorTests.cs` (existing real+synthetic fixture conventions, reused).

## 5. Files changed

Production code (10 files, `backend/RunningApp.Application/`):
- `RuntimeCatalog/Schedule/Progression/CatalogWorkoutProgressionDefinition.cs` — `CatalogWorkoutProgressionLane`, optional `Lanes[]` + `EffectiveLanes` on `CatalogPhaseWorkoutProgression`.
- `RuntimeCatalog/Schedule/Progression/CatalogWorkoutProgressionLoader.cs` — optional `"lanes"` JSON parsing.
- `RuntimeCatalog/Schedule/Progression/ProgressionStageScheduleContracts.cs` — `LaneOrdinal` on `ScheduledProgressionWeek`/`StageAllocationDecisionTraceStep`.
- `RuntimeCatalog/Schedule/Progression/ProgressionStageAllocator.cs` — per-lane invocation orchestration; duplicate-lane-ordinal guard.
- `RuntimeCatalog/Schedule/Progression/ProgressionStageSchedulingExceptions.cs` — `ProgressionStageDuplicateLaneOrdinalException`.
- `RuntimeCatalog/Schedule/Progression/GeneratedCatalogStageScheduleValidator.cs` — lane-aware uniqueness/monotonicity checks.
- `RuntimeCatalog/Schedule/Binding/CatalogWorkoutBinder.cs` — structural-ordinal computation, `(WeekNumber, LaneOrdinal)`-keyed stage lookup, lane-count-vs-RunLayout validation.
- `RuntimeCatalog/Schedule/Binding/BoundCatalogPlanContracts.cs` — `LaneOrdinal` on `BoundCatalogSession`.
- `RuntimeCatalog/Schedule/Binding/CatalogWorkoutBindingExceptions.cs` — `CatalogWorkoutBindingDuplicateLaneStageAssignmentException`, `CatalogWorkoutBindingLaneCountMismatchException`.
- `RuntimeCatalog/Prescription/Session/CatalogSessionPrescriptionPlanner.cs` — reads `session.LaneOrdinal` instead of recomputing `keyOrdinal`.

Test (1 new file): `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Freq6D4DSplitADualKeyLaneStageBindingTests.cs` (21 tests).

Plus the mechanical rebuild of tracked `bin/`/`obj/` build artifacts across every backend project — this repository tracks build output (confirmed via `git check-ignore`, no match), the same convention every prior commit in this history follows (explicitly noted in `FREQ.6D.3D`'s own report).

No `WorkoutPrescriptionProfile` source, `WorkoutDefinition`, catalog-lifecycle, execution-contract, projector, `ExecutionPrescriptionIndex`/`PublishedTemplateBundleJsonReader`/`CatalogSessionPrescriptionSource`, database/persistence, Adaptation-policy, or public-routing file was touched — confirmed by direct review of the staged diff.

## 6. Canonical structural ordinal

Computed entirely locally inside `CatalogWorkoutBinder.BindAsync`, per dated week, as a zero-based rank over same-role slots ordered by the canonical `SlotOrderInWeek` (`structuralOrdinalByRole`, a `Dictionary<string, int>` reset per week, incremented as the binder's existing `datedWeek.SessionSlots.OrderBy(s => s.SlotOrderInWeek)` loop iterates). No new field was added to `GeneratedCatalogSessionSlotSkeleton`/`DatedCatalogSessionSlot` — the ordinal is a pure function of data the binder already consumes. This deliberately does **not** parse `GeneratedCatalogSessionSlotSkeleton.LayoutSlotKey`'s `{role}_{occurrenceIndex}` string (which already exists upstream, at RunLayout-expansion time) — the architecture's own §7 specifies the ordinal as a bind-time rank, and computing it locally from `SlotOrderInWeek` avoids adding a second, string-parsed source of the same fact.

## 7. LaneOrdinal implementation

Two related, deliberately distinct concepts (`FREQ.6D.4D §31`), both implemented:

- **Catalog-authored `LaneOrdinal`** (`CatalogWorkoutProgressionLane.LaneOrdinal`) — declared once, per lane, at progression-authoring time. Never derived from calendar/date/enumeration order.
- **Bind-time structural ordinal** (§6 above) — computed fresh for every dated week from `SlotOrderInWeek`.

The binding rule, implemented exactly as specified: for `StageControlled` sessions, `laneOrdinal = structuralOrdinal` — the structural ordinal *is* used as the lookup key into the lane-keyed stage schedule. `BoundCatalogSession.LaneOrdinal` (new, nullable `int`) carries the result thereafter — populated (always, including `0`) for `StageControlled` sessions, explicitly `null` for `FixedDefault` (`EASY_SUPPORT`/`LONG_RUN`) sessions, mirroring `ProgressionStageKey`'s existing null convention exactly.

## 8. Generic role/lane model

No `bool IsSecondaryKey`, no `if (runsPerWeek == 5)` branch, no hardcoded cap at two lanes anywhere in the diff (confirmed by direct review). `structuralOrdinalByRole` is keyed by role string generically — it would compute a correct ordinal for any repeated structural role, not only `KEY_SESSION`; only `StageControlled` roles (today, only `KEY_SESSION`, via `V1CatalogWorkoutRoleBindingPolicy`, unchanged) actually consume the ordinal as a `LaneOrdinal`. `CatalogWorkoutProgressionLane`/`EffectiveLanes` place no upper bound on lane count — a future 3-lane role would flow through the same code paths unmodified. 3D/4D's single `KEY_SESSION` slot degenerates the model to `LaneOrdinal 0` by construction, never by a special-cased branch.

## 9. stageWeeksByNumber disposition

Replaced with `stageWeeksByWeekAndLane`, a `Dictionary<(int WeekNumber, int LaneOrdinal), ScheduledProgressionWeek>`, built with `TryAdd` (throws `CatalogWorkoutBindingDuplicateLaneStageAssignmentException` on any genuine `(week, lane)` collision, never silently overwriting). For single-lane schedules (`LaneOrdinal` always `0`), this dictionary has exactly the same key cardinality as the old `WeekNumber`-only one — confirmed by the unchanged real-catalog `DefaultTwelveWeekPilot_EverySlotBoundExactlyOnce` test still passing with the same `48` total sessions.

## 10. Per-lane allocator orchestration

`ProgressionStageAllocator.Allocate`'s existing `foreach (var phaseProgression in progression.PhaseProgressions...)` loop gained an inner `foreach (var lane in phaseProgression.EffectiveLanes.OrderBy(l => l.LaneOrdinal))`, constructing a lane-scoped `CatalogPhaseWorkoutProgression { PhaseKey, Stages = lane.Stages }` view and calling the existing, **completely unmodified** `AllocatePhase(laneView, phaseWeeks, context, weeks, traceSteps, lane.LaneOrdinal)` — the only change to `AllocatePhase` itself is one new optional `laneOrdinal` parameter (default `0`) threaded into the two existing output-construction sites (`ScheduledProgressionWeek`/`StageAllocationDecisionTraceStep`), never into any decision logic. Duplicate `LaneOrdinal` declarations within one phase's `Lanes[]` are rejected before any allocation runs (`ProgressionStageDuplicateLaneOrdinalException`). Confirmed via direct diff review: zero changes to compression, extension, minimum/maximum-exposure math, stage ordering, or horizon semantics — matching the phase's own stop condition #2 (would have required `SPLIT_A_BLOCKED_ON_ALLOCATOR_ARCHITECTURE_CONTRADICTION`, not triggered).

## 11. Independence proof

Three dedicated tests, using deliberately different lane stage inputs (not relying on any coincidence in real data):

- `TwoLanes_DeliberatelyDifferentStageInputs_ProduceDistinguishableIndependentSchedules` — Lane 0 gets one 4-exposure stage, Lane 1 gets two 2-exposure stages; both lanes' schedules resolve correctly and independently against the same shared 4-week phase.
- `Lane1InputMutation_DoesNotChangeLane0Schedule` / `Lane0InputMutation_DoesNotChangeLane1Schedule` — mutating one lane's stage key leaves the other lane's resolved schedule byte-identical, proven by list-equality comparison of `(WeekNumber, ProgressionStageKey)` pairs before/after the mutation.
- `NoWeekNumberOnlyAliasing_BothLanesResolveDistinctStagesForSameWeek` — for a single shared week, both lanes independently resolve to their own, distinct `ProgressionStageKey` — proving `SAME_ALGORITHM DOES_NOT_IMPLY SAME_LANE_STAGE_BINDING` directly, not by assertion alone.

## 12. 3D result

`SingleKeyLegacyLayout_BindsToLaneOrdinalZero` (new, synthetic) and the existing real-catalog Intermediate×4D binder/allocator tests (no dedicated 3D synthetic fixture existed pre-Split-A either; 3D shares the identical single-`KEY_SESSION`-per-week structural shape 4D exercises) confirm `LaneOrdinal == 0` for the sole `KEY_SESSION` session every week, with workout/stage selection unchanged. `SINGLE_KEY_BEHAVIORAL_EQUIVALENCE_PRESERVED`.

## 13. 4D result

Real-catalog `CatalogWorkoutBinderTests.DefaultTwelveWeekPilot_EverySlotBoundExactlyOnce` (48 sessions, unchanged), `EasySupport_BindsToEasyStandard`, `LongRun_BindsToLongRunStandard`, and the full `ProgressionStageAllocatorTests`/`CatalogWorkoutBinderTests` suites all pass unchanged against the real v10 candidate. `LegacySingleKeyThreeAndFourDayShapes_LaneOrdinalAlwaysZero_WorkoutSelectionUnchanged` (new, synthetic, mirroring the real 4D role sequence `EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN`) directly confirms `LaneOrdinal == 0` and unchanged workout/stage selection. `SINGLE_KEY_BEHAVIORAL_EQUIVALENCE_PRESERVED`.

## 14. 5D result

`FiveDTwoKeySlots_BindToDistinctLaneOrdinalsZeroAndOne_BothStructuralRoleKeySession` proves, against a synthetic 2-`KEY_SESSION` week: both sessions carry `StructuralRole == "KEY_SESSION"` (no new role introduced); `LaneOrdinal` `0`/`1` respectively; distinct exact workout/stage bindings per lane. `LaneAssignment_IndependentOfCalendarWeekday_DrivenBySlotOrderInWeekOnly` proves lane identity is not weekday-derived even when both structural slots would otherwise be ambiguous by date alone.

## 15. 8/10/12/14 horizon results

`EveryHorizon_BothKeyLanesHaveExactlyOneDeterministicStageBindingPerWeek_NoMissingOrDuplicate` (theory, `[InlineData(8,10,12,14)]`), against a synthetic 2-lane phase sized to each horizon: every week has exactly one `LaneOrdinal 0` row and one `LaneOrdinal 1` row (`2 × totalWeeks` schedule rows total), and `GeneratedCatalogStageScheduleValidator.Validate` returns `IsValid == true` for all four horizons. No horizon-specific branch exists anywhere in the diff — `ProgressionStageAllocator.AllocatePhase`'s only per-horizon-varying input (`phaseWeeks.Count`) is unchanged from before Split A.

## 16. Taper result

`Taper_PreservesBothLaneStageBindings_PhaseRemainsCanonicalAcrossBothLanes` proves both `TAPER_SHARPEN_PRIMARY` (Lane 0) and `TAPER_SHARPEN_SECONDARY` (Lane 1) resolve independently across a 2-week synthetic Taper phase, with `PhaseKey == "TAPER"` identical across every row of both lanes — the canonical phase is never split or duplicated per lane, satisfying `§16` of the Split-A prompt directly.

## 17. Failure semantics

| Case | Typed exception | Test |
|---|---|---|
| Duplicate `LaneOrdinal` declared within one phase | `ProgressionStageDuplicateLaneOrdinalException` | `DuplicateLaneOrdinal_InSameProgressionPhase_ThrowsTypedException` |
| Missing lane stage binding (one of two KEY slots has no lane assignment) | `CatalogWorkoutBindingMissingProgressionStageException` | `MissingLaneStageAssignment_OneOfTwoKeySlotsHasNoLaneBinding_ThrowsTypedException`, `OriginalDefectReproduction_...` |
| Unsupported `LaneOrdinal` (more declared lanes than real structural KEY slots) | `CatalogWorkoutBindingLaneCountMismatchException` | `UnsupportedLaneOrdinal_MoreDeclaredLanesThanStructuralKeySlots_ThrowsTypedException` |
| Duplicate `(WeekNumber, LaneOrdinal)` stage-schedule row | `CatalogWorkoutBindingDuplicateLaneStageAssignmentException` | `DuplicateLaneStageAssignment_SameWeekAndLaneTwice_ThrowsTypedException` |

No `Lane1 missing → fall back to Lane0` path exists anywhere (confirmed by review — every lane lookup is an exact dictionary key, never a fallback chain).

## 18. Profile-binding zero-delta

`BoundCatalogSession_CarriesNoPrescriptionProfileField_ProfileSelectionRemainsSplitBScope` — a reflection-based architectural proof: `typeof(BoundCatalogSession).GetProperties()` contains no `PrescriptionProfileKey`/`PrescriptionProfileVersion`, and does contain `LaneOrdinal`. No `WorkoutPrescriptionProfile` type, catalog reference, or lookup was introduced anywhere in the diff.

## 19. Bundle zero-delta

`CatalogBundleAssembler`/`PublishedTemplateBundle.ExecutionPrescriptions` were not touched — confirmed by the staged-diff review (§5) and by the full `PlanCatalog.Tests` suite remaining exactly `1485/1485`, byte-identical to the pre-Split-A baseline.

## 20. RunningApp zero-delta

`ExecutionPrescriptionIndex`, `PublishedTemplateBundleJsonReader`, `CatalogSessionPrescriptionSource`, and every `TrainingDay`/API-DTO file remain untouched — confirmed by direct review of the staged diff (§5) and by grepping the full diff for those four type names, which appear only in doc-comment cross-references, never in modified code.

## 21. Persistence zero-delta

No database column or migration was added. Split A did not need to enrich any internal/catalog-generated value beyond the additive `LaneOrdinal` field on `BoundCatalogSession` (an in-memory, never-persisted internal contract — confirmed unchanged: still documented as "never exposed on any public DTO, never persisted, never hashed"). No architecture-boundary contradiction was encountered; `STOP: SPLIT_A_BLOCKED_ON_PERSISTENCE_BOUNDARY` was not triggered.

## 22. Adaptation zero-delta

`NextWindowLoadDecisionPolicy`, `WindowExecutionSummaryBuilder`, and `ScheduleRepairRuntimeOrchestrator` were not touched — confirmed by direct review of the staged diff. No comment claiming the 5-session severity table "awaits a future product decision" was preserved or repeated in this phase's diff (none existed in the files this phase touched to begin with — the existing, out-of-date comment lives in `NextWindowLoadDecisionPolicy.cs`, a file this phase did not modify).

## 23. Known severity implementation gap

Re-confirmed, not re-decided: `NextWindowLoadDecisionPolicy.DetermineLoadDecision` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/NextWindowLoadDecisionPolicy.cs:49-59`, unmodified by this phase) is still hardcoded to a 4-session week (`>= 4 => ProgressAsPlanned`, which would misclassify a 5-session week's "4 of 5 completed" identically to "fully complete"). Its own doc comment (lines 30-48) frames this as awaiting "a future product decision" — this framing is **incorrect**, per `FREQ.6D.4D §21`/`FREQ.6D.1B` Track A: the exact 24-row, 5-session severity table was already approved by `FREQ.6 §6`, verified row-for-row in `FREQ.6D.1B`. Classification: `APPROVED_POLICY_NOT_YET_IMPLEMENTED`, **not** `PRODUCT_DECISION_REQUIRED`. This gap belongs entirely to the later persistence/Adaptation-integration split (Split D per `FREQ.6D.4D §41`) — touching `NextWindowLoadDecisionPolicy.cs` was outside this Split-A diff's necessary scope (§28 of the Split-A prompt explicitly forbids modifying it here), so the stale comment is disclosed here rather than corrected in-place. No duplicate technical-debt entry is created — this is the same gap `FREQ.6D.4D §38`'s change manifest already scoped under "ADAPTATION INPUT."

## 24. Full regression

- New Split-A test file: **21/21 passed** (20 on first run + 1 defect-reproduction test added afterward, both confirmed independently).
- Full `RuntimeCatalog` namespace regression: **2,898 passed, 1 failed** (`PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe`, expected `78` actual `91`) — **confirmed pre-existing, unrelated**: reproduced identically against the unmodified baseline via `git stash`/re-test (same expected/actual values, same file), caused by catalog-content growth from prior FREQ.6D.4C.2-4C.5 phases (capability overlays, prescription profiles, four new `WorkoutDefinition` versions), never touched by this phase's diff. Restored via `git stash pop` (with an explicit-file `git checkout stash@{0} -- <paths>` recovery step after an initial `stash pop` conflict with tracked build-artifact noise — verified the recovered source content matched the original edits exactly via `git diff --cached`, before proceeding).
- Full `PlanCatalog.Tests` suite (untouched by this phase): **1,485 passed, 0 failed** — byte-identical to the `FREQ.6D.4C.5` baseline, confirming zero PlanCatalog delta.

## 25. Build

`dotnet build backend/RunningApp.sln`: 0 warnings, 0 errors. `dotnet build backend/RunningApp.sln -c Release`: 0 warnings, 0 errors. `git diff --check`: clean (only pre-existing CRLF-normalization warnings).

## 26. File attribution

| Category | Files |
|---|---|
| `SLOT_IDENTITY_MODEL` | `CatalogWorkoutBinder.cs` (structural-ordinal computation) |
| `LANE_BINDING_MODEL` | `CatalogWorkoutProgressionDefinition.cs`, `BoundCatalogPlanContracts.cs`, `ProgressionStageScheduleContracts.cs` |
| `STAGE_ALLOCATION_ORCHESTRATION` | `ProgressionStageAllocator.cs`, `CatalogWorkoutProgressionLoader.cs` |
| `BINDING_VALIDATION` | `GeneratedCatalogStageScheduleValidator.cs`, `CatalogWorkoutBindingExceptions.cs`, `ProgressionStageSchedulingExceptions.cs`, `CatalogSessionPrescriptionPlanner.cs` (ordinal-authority correction) |
| `TEST` | `Freq6D4DSplitADualKeyLaneStageBindingTests.cs` |
| `DOCUMENTATION` | this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | None — confirmed by direct review; every changed source file falls within the categories above, and all remaining changed files are the established tracked-build-artifact rebuild convention |

## 27. Split-B input contract

Per `FREQ.6D.4D §44`, explicit invariant recorded: Split B may **consume** `LaneOrdinal`, `ProgressionStageKey`, and `PhaseKey` directly off `BoundCatalogSession` — it must **not** derive `LaneOrdinal` from session order, weekday, `DoseCategory`, or a profile search, and must **not** re-run stage allocation. Every `StageControlled` `BoundCatalogSession` now deterministically carries `(WeekNumber, StructuralRole, SlotOrderInWeek-derived ordinal, LaneOrdinal, ProgressionStageKey)` — sufficient for Split B to resolve `Lane/Stage/Phase → exact WorkoutPrescriptionProfile ref` (via the new, not-yet-added `PrescriptionProfileCandidateKeys[]` field on `CatalogWorkoutProgressionStage`, per `FREQ.6D.4D §10`) without recomputing anything this phase already materialized.

## 28. Final classification

**`FREQ6D4D_SPLIT_A_IMPLEMENTED_ADAPTATION_ENGINEERING_GAP_REMAINS`**

Split A (slot/lane identity + stage binding) is fully implemented and tested: the `stageWeeksByNumber` defect is closed via a `(WeekNumber, LaneOrdinal)`-keyed model; `LaneOrdinal` is catalog-authored and generic (no cap at two, no `KEY1`/`KEY2` structural roles); the structural ordinal is deterministic from `SlotOrderInWeek` alone; `ProgressionStageAllocator`'s existing algorithm is reused verbatim, invoked once per lane with proven independence; all fail-closed failure paths are typed and tested; legacy Intermediate×3D/4D/Beginner×4D behavior is unchanged (`SINGLE_KEY_BEHAVIORAL_EQUIVALENCE_PRESERVED`); the known, pre-existing 5-session Adaptation severity-table gap (`APPROVED_POLICY_NOT_YET_IMPLEMENTED`) is re-disclosed, not touched, and remains correctly scoped to a later split. `FREQ.6D.4D` overall dual-KEY production integration is **not** complete — Splits B-E remain.
