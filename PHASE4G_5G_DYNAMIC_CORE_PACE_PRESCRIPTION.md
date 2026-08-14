# Phase 4G.5G — Dynamic Core Pace and Session Prescription

## 1. Executive result

A new, dark, unwired orchestrator — `DynamicCoreSessionPrescriptionOrchestrator` — generalizes the live 12-week pace/effort/duration/repetition/segment prescription step to any mathematically feasible standalone-core length (8-14 weeks for `TEN_K_MASTER v6`). The pre-existing research assumption that session prescription might not exist yet for the 12-week pilot was checked and found **false**: session/workout prescription is fully implemented and already live-wired into `CatalogPreviewGenerator`, one step past Phase 4G.5E/4G.5F's own scope. This orchestrator performs zero pace-derivation or prescription logic of its own — it chains Phase 4G.5F's volume/long-run orchestrator into the real, unmodified `CatalogSessionPrescriptionPlanner` and `CatalogFinalPrescribedPlanFinalizer`. All required prerequisite cross-checks against Phase 4G.5F's output passed cleanly (46/46 tests). All 35 (7 horizons × 5 pace-evidence categories) combinations passed, confirming the existing fail-closed philosophy generalizes correctly with zero new findings requiring a stop condition. One confirming governance clarification was recorded against `TD-NOTEVALUATED-FALLBACK-001`; no new TD was needed.

## 2. Prerequisite cross-checks from Phase 4G.5F

### 2.1 LongRunProgressionVerifier at the same 6-profile × 7-horizon scale

Phase 4G.5F's re-verification of `TD-VOLUME-CAP-UNENFORCED-001` covered only `VolumeProgressionVerifier` (weekly-volume increase caps). Before using `LongRunDecision` values as a pace-prescription grounding input, this pass extended the identical scale of verification to `LongRunProgressionVerifier` (Phase 4G.3B.3, verifier 8 of 9 — checks `PreferredMinimumShare`/`PreferredMaximumShare`/`HardCapShare` bounds and the absolute "long run never exceeds weekly volume" invariant), across the same 6 runner profiles × 7 horizons = 42 combinations, real unmodified `CatalogVolumeAndLongRunPlanner` output.

**Result: 42/42 pass, zero `EXCEEDS_WEEKLY_VOLUME`/`HARD_CAP_SHARE_VIOLATION` findings in any combination.** This confirms `LongRunProgressionVerifier`'s own pre-existing finding (its class doc comment: `PreferredMaximumShare` (0.36) is strictly less than `HardCapShare` (0.40), so the real planner's clamp always resolves into the preferred band, not merely the hard cap) generalizes across the full matrix, not just the single fixture previously exercised. No stop condition triggered — pace prescriptions were safe to build on top of these `LongRunDecision` values.

### 2.2 Taper meaningfulness at compressed horizons (8-11 weeks) specifically

Phase 4G.5F's report confirmed taper remains meaningful generally but did not isolate the compressed range explicitly. This pass added a targeted check: for each of 8/9/10/11 weeks, the taper week's `PlannedWeeklyVolumeKm` is strictly below the immediately preceding week's volume, and within rounding tolerance of `precedingWeek.PlannedWeeklyVolumeKm * TaperVolumeMultiplier` (0.53).

**Result: 4/4 pass.** Taper is confirmed meaningfully reduced at every compressed horizon, not only at 12-14 weeks. No stop condition triggered.

### 2.3 Location of Phase 4G.5F's governance clarification

Confirmed directly (this session's own prior turn): Phase 4G.5F recorded its confirming finding as an append-only addendum to `TD-VOLUME-CAP-UNENFORCED-001`'s existing `currentStateClarification` field (JSON) and a new prose section in the risk `.md` file, matching the exact established convention already set by `TD-FOUNDATION-COMPRESSION-001`'s Phase 4G.3B.10A clarification and `TD-ALLOCATION-PRIORITY-001`'s Phase 4G.5B.0 clarification. **This matches the session's established append-only-addendum convention exactly — no consistency finding to report.**

## 3. Reuse strategy — which approach was used and why

Per this pass's explicit instruction, the existing pace-resolution and session-prescription logic was located and inspected before any new code was written. Three collaborating layers already exist, fully implemented and live-wired:

1. **`PaceSourceResolver`** (`backend/RunningApp.Application/RuntimeCatalog/Resolvers/PaceSourceResolver.cs`) — the evidence-hierarchy resolver: recent-race evidence (all 3 of distance/finish-time/date required) → target finish time → (`ESTIMATED`, never emitted in V1) → `NONE`. Confirmed by direct code inspection: `Resolve()` never reads `Level`/`RunningBackground` anywhere — experience level cannot be the sole pace source by construction (enforced by omission, not by an explicit guard, but structurally impossible regardless).
2. **`CatalogPrescriptionContextBuilder.MapPaceSource`/`GuardGoalFeasibilityPaceBoundary`** (already reused unmodified by Phase 4G.5E/4G.5F) — maps the resolver's raw `PACE_SOURCE_IN`/`GOAL_FEASIBILITY_IN` outputs onto a prescription-usable `ResolvedPrescriptionPaceSource`, with a hard guard against using target-goal pace under unsupported feasibility.
3. **`CatalogSessionPrescriptionPlanner`/`CatalogFinalPrescribedPlanFinalizer`** (`backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/`) — the actual per-session prescriber: distance allocation, pace/effort, duration, segments, and (via the finalizer) `V1TaperSharpenPrescriptionPolicy` completion.

None of these three layers required any change. No dark-reachability test constrains any of their call sites — like `CatalogWorkoutBinder` (Phase 4G.5E) and `CatalogVolumeAndLongRunPlanner` (Phase 4G.5F), they are already `CatalogPreviewGenerator`'s live call sites, data-gated unreachable only by the pilot candidate's `DRAFT` catalog status. The same reuse strategy therefore applies: call each real component's real public entry point directly, with no adaptation to their internal logic.

**One minimal, transparent adaptation was required** (not a change to any existing production logic): `DynamicCoreVolumeAndLongRunResult` (Phase 4G.5F, itself a dark type this session already owns) did not previously expose the `CatalogPlanPrescriptionContext` it already builds internally. `CatalogSessionPrescriptionPlanner` requires that exact context as an input. Re-deriving it a second time in this new orchestrator would have duplicated `CatalogPrescriptionContextBuilder.Build`'s logic rather than reusing it — so `DynamicCoreVolumeAndLongRunResult` gained one new `required` field (`PrescriptionContext`), populated from the value the orchestrator already computes at line ~161 of its own file. No planner, resolver, or finalizer logic was touched; this is exposing an already-computed value from a Phase 4G.5F file this session authored, not modifying an established production component.

## 4. Preserved identity/design decisions — confirmed, not reinterpreted

**TAPER_SHARPEN "sharpening via prescription, not workout swap" (AUD-507/AUD-508).** Confirmed this design is fully implemented, not merely documented: `V1TaperSharpenPrescriptionPolicy.Complete` (unmodified) builds a three-segment prescription (easy baseline / `CONTROLLED_SHARPENING` / recovery) on top of the unchanged `EASY_STANDARD` workout identity, applied by `CatalogFinalPrescribedPlanFinalizer.Complete` for every `TAPER_SHARPEN`-stage session. **Verified empirically across all 35 combinations** (`PrescribeAsync_RealCandidate_TaperSharpenAppliesPrescriptionAdjustment_IdentityUnchanged`): every taper-sharpen session retains `WorkoutDefinitionKey = EASY_STANDARD`, carries a `CONTROLLED_SHARPENING` segment, has no target-goal-derived pace anywhere in its segments, and reaches `CatalogSessionPrescriptionStatus.FinalPrescriptionComplete`. This is the existing decision applied exactly as already established — this pass invented nothing new here.

**TD-NOTEVALUATED-FALLBACK-001 — not silently resolved or reinterpreted.** This orchestrator never calls `PaceSourceResolver`/`GoalFeasibilityResolver`/`RuntimeConditionResolutionService` itself; `ConditionResults` is an external input, exactly mirroring how Phase 4G.5E/4G.5F already treat it. The fail-closed philosophy (Phase 4G.3B.6.1: reject/reroute before generating, never silently substitute) is therefore preserved by construction, not by new logic. See Section 5 for the empirical confirmation and Section 8 for the governance record.

## 5. Required behavior — pace resolution per workout type, verified

For each of the 7 workout-progression stages (`FOUNDATION_EASY_BASE`, `FARTLEK_INTRO`, `THRESHOLD_INTRO`, `TEN_K_SPECIFIC_INTRO`, `GOAL_PACE_REHEARSAL`, `CURRENT_FITNESS_SPECIFIC_REHEARSAL`, `TAPER_SHARPEN`) plus `EASY_SUPPORT`/`LONG_RUN` fixed-default roles, the real `CatalogSessionPrescriptionPlanner` (unmodified) resolves: pace source, pace range or effort, duration, segments, and decision trace, per session. Confirmed by direct code inspection and by the 35-combination test matrix:

- **`GOAL_PACE_TEN_K` only** gets `ExactPace` (numeric `SecondsPerKilometer`, derived as `TargetFinishTimeSeconds / GoalDistanceKm`), and only when `GoalFeasibilityValue` is `REALISTIC`/`CHALLENGING` — otherwise the planner throws `CatalogGoalPacePrescriptionUnsupportedException` (never reached in practice, per Section 6).
- **Every other workout** (`EASY_STANDARD`, `LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, etc.) gets `EffortOnly` — no numeric pace derivation exists for these in V1, by explicit existing design ("No approved easy/long-run/fartlek/threshold pace derivation is present"), not a gap this pass needed to fill.
- Segments, duration, and repetition are derived from the workout definition's declared components and the week's already-planned `PlannedWeeklyVolumeKm`/`PlannedLongRunDistanceKm` (Phase 4G.5F output), via `V1FourDaySessionVolumeAllocationPolicy` and `CatalogSessionPrescriptionPlanner`'s own segment-building logic — unmodified.
- Every session carries a full `SessionPrescriptionDecisionTrace` (raw pace source, goal feasibility, selected/rejected pace sources, allocation policy) — the existing decision-trace mechanism, reused unmodified.

## 6. Fail-closed behavior across 5 pace-evidence categories × 7 horizons

Per this pass's required test matrix, 35 combinations were run: `RecentRace`, `TargetTimeNoIndependentEvidence` (the `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE` trigger), `ProductAverage` (the `PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED` trigger), `NotEvaluated` (both `PACE_SOURCE_IN` and `GOAL_FEASIBILITY_IN` unresolved), and `Unsupported` (`GOAL_FEASIBILITY_IN = UNSUPPORTED`) — each at all 7 target week counts.

**Confirmed in every combination:**
- `RecentRace`/`ProductAverage`: any `GOAL_PACE_TEN_K` session present (0-2, depending on whether the horizon/eligibility routes there) always carries `ExactPace` with a non-null numeric value.
- `TargetTimeNoIndependentEvidence`/`NotEvaluated`/`Unsupported`: **zero `GOAL_PACE_TEN_K` sessions ever appear.** The upstream `ProgressionStageAllocator` eligibility/fallback mechanism (unmodified, Phase 4F.6A) already routes these to `CURRENT_FITNESS_SPECIFIC_REHEARSAL`/`THRESHOLD_TEMPO` before this orchestrator's prescription step is reached — `CatalogSessionPrescriptionPlanner`'s own `CatalogGoalPacePrescriptionUnsupportedException` fail-closed guard is structurally protected by this upstream fallback and was never observed to throw in any of the 21 fail-closed-category combinations. This is stated as a confirmed finding, not assumed.
- No session's pace is ever derived from experience/level alone in any combination — every non-`GOAL_PACE_TEN_K` session is `EffortOnly`/`Unresolved`, never a numeric pace synthesized from `RunningBackground`.

**Result: fail-closed behavior is confirmed unchanged and correctly generalizes to 8-14 weeks.** See Section 8 for the corresponding `TD-NOTEVALUATED-FALLBACK-001` governance record.

## 7. Compression and extension behavior

**8-11 weeks (compression):** `PrescribeAsync_CompressedHorizon_SessionCountAndStructurePreserved_NoDensityIncrease` confirms every week retains exactly 4 sessions (1 `KEY_SESSION`, 2 `EASY_SUPPORT`, 1 `LONG_RUN`) — the prescription layer introduces no new session and removes none; quality-session count/spacing is entirely inherited, unchanged, from the already-verified Phase 4G.5D/4G.5E skeleton/binding output. "Reduce workout count before increasing density," "avoid adjacent incompatible quality sessions," and "preserve long-run/key-session spacing" are therefore satisfied by construction at the binding layer (already verified in Phase 4G.5D/4G.5E) — the prescription layer does not, and structurally cannot, alter session count or placement, only per-session pace/duration/segment content.

**13-14 weeks (extension):** `PrescribeAsync_ExtendedHorizon_GoalPaceExposureStaysWithinApprovedPolicyBounds` confirms `GOAL_PACE_TEN_K` session count never exceeds 2 — `GOAL_PACE_REHEARSAL`'s own catalog-declared `maximumExposures` (unchanged), enforced entirely by the real, unmodified `ProgressionStageAllocator`. No unapproved goal-pace repetition is introduced by this orchestrator; it consumes whatever exposure count the allocator already produced.

## 8. New findings — governance clarification recorded, no new TD

Per this pass's own governance requirement, a genuine new problem (a workout with no resolvable pace source under a reachable evidence combination, an unsafe compression/extension density, or a new problematic interaction with an open TD) would require a proper new risk entry. **None was found.** Because this pass's finding is confirming (the existing fail-closed mechanism generalizes correctly) rather than problematic, and per this session's established append-only-addendum convention (confirmed applicable in Section 2.3), the finding was recorded as an append-only addition to `TD-NOTEVALUATED-FALLBACK-001`'s existing `statement` field (matching that TD's own established convention of appending dated paragraphs, e.g. "SCOPE CORRECTION", "SEMANTICS CLARIFICATION") in `activation-readiness-risks.json`, and a matching sentence appended to its row in `activation-readiness-risks.md`. The TD's own open decisions (items 1-3, all product/UX questions) are explicitly unchanged and unresolved — this pass does not attempt to resolve them. No aggregate count changed. `ActivationReadinessRiskParityTests`/`ActivationSafetyGateTests` re-run after both edits: **15/15 passed**.

## 9. Test matrix

```text
DynamicCoreSessionPrescriptionOrchestratorTests: 79 passed, 0 failed, 0 skipped
DynamicCoreVolumeAndLongRunOrchestratorTests (re-run after Section 10's reconciliation): 51 passed, 0 failed, 0 skipped
Phase4G5GPrerequisiteCrossChecksTests: 46 passed, 0 failed, 0 skipped
Combined focused run: 176 passed, 0 failed, 0 skipped
```

## 10. Required verification

**Zero production call sites**: confirmed by grep (repo-wide search for `DynamicCoreSessionPrescription(Orchestrator|Context|Result)` across all four production projects, excluding the new file's own source, returned zero matches) and by an executable structural test, `DarkReachability_NoProductionCallSite`. A companion test, `DarkReachability_NoDiRegistration`, confirms `IDynamicCoreSessionPrescriptionOrchestrator` is never referenced in `RunningApp.Api`. `CatalogPreviewGenerator.cs`'s `git diff` remains empty — confirmed untouched.

**Byte/value-level 12-week regression**: `PrescribeAsync_TargetWeekCount12_MatchesExistingFixedWeekPrescriptionPipelineExactly` builds the prescribed plan via the **existing, completely unmodified** fixed-week pipeline (extended through the real `CatalogSessionPrescriptionPlanner`/`CatalogFinalPrescribedPlanFinalizer` — the same two components `CatalogPreviewGenerator` itself calls) and, separately, via the new `DynamicCoreSessionPrescriptionOrchestrator` requesting `targetWeekCount=12`, then asserts full field-by-field equality of every session's workout key, distance, pace kind/value, duration kind, and ordered segments — a concrete comparison, not a bare claim. The existing pipeline's own output is additionally pinned to literal values (48 total sessions, week 12's `TAPER_SHARPEN` session bound to `EASY_STANDARD` with a `CONTROLLED_SHARPENING` segment). All assertions pass.

**Existing-test reconciliation** (the pattern now encountered a fourth time: 4G.5D↔4G.3B.2, 4G.5E↔4G.5D, 4G.5F↔4G.5E, now 4G.5G↔4G.5F): adding this new orchestrator's call to `DynamicCoreVolumeAndLongRunOrchestrator.PlanAsync(...)` (via `IDynamicCoreVolumeAndLongRunOrchestrator`) made it a second production reference to that Phase 4G.5F type, which broke that phase's own `DarkReachability_NoProductionCallSite` test. The test was updated, not weakened: renamed to `DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer`, with one new file-path exclusion (`DynamicCoreSessionPrescriptionOrchestrator.cs`) and an updated doc comment explaining why — the new consumer is itself proven dark by its own two reachability tests. (`Phase4G5GPrerequisiteCrossChecksTests.cs` also references the Phase 4G.5F type, but that scan's project list is limited to the four production projects and does not include `RunningApp.IntegrationTests`, so no exclusion was needed for it — confirmed and documented in the updated test's own comment.)

A second, one-level-deeper cascade was caught by the first full-suite run (not by the targeted filtered run, which does not exercise every project's dark-reachability scan): the new file's own XML doc comment happened to spell out the literal identifier `DynamicCoreWorkoutBindingOrchestrator` in prose (explaining the full pipeline chain), which matched Phase 4G.5E's own `DynamicCoreWorkoutBindingOrchestratorTests.DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer` regex scan — a real text match, correctly caught by that test, not a false positive. Resolved by rewording the doc comment to describe the chain without repeating that exact identifier (a documentation change, not a code or exclusion-list change) — this preserves the existing test's own literal-symbol-scan semantics rather than adding a third exclusion for a non-code-reference. Re-ran all 28 dark-reachability tests repo-wide after the fix: all pass.

These are the only existing production/test files modified by this pass beyond the governance file updates in Section 8 and the minimal field addition in Section 3.

## 11. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/DynamicCoreSessionPrescriptionOrchestrator.cs` (new).
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/DynamicCoreVolumeAndLongRunOrchestrator.cs` (existing Phase 4G.5F file, this session's own — one field added, `PrescriptionContext`; see Section 3).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/DynamicCoreSessionPrescriptionOrchestratorTests.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/Phase4G5GPrerequisiteCrossChecksTests.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/DynamicCoreVolumeAndLongRunOrchestratorTests.cs` (existing file, one test updated — see Section 10; no production file modified).
- `plan-catalog/artifacts/audits/activation-readiness-risks.json` — one paragraph appended (`TD-NOTEVALUATED-FALLBACK-001.statement`), append-only.
- `plan-catalog/artifacts/audits/activation-readiness-risks.md` — one sentence appended, append-only.
- `PHASE4G_5G_DYNAMIC_CORE_PACE_PRESCRIPTION.md` (new, this document).

No true production component (`PaceSourceResolver.cs`, `GoalFeasibilityResolver.cs`, `CatalogPrescriptionContextBuilder.cs`, `CatalogSessionPrescriptionPlanner.cs`, `V1TaperSharpenPrescriptionPolicy.cs`, `CatalogFinalPrescribedPlanValidator.cs`, `CatalogPreviewGenerator.cs`, or any catalog JSON file) was modified.

## 12. Full backend suite

```text
Run 1: Failed: 0, Passed: 1711, Skipped: 1, Total: 1712
Run 2: Failed: 0, Passed: 1711, Skipped: 1, Total: 1712
```

Both runs match exactly (+125 vs. Phase 4G.5F's `1586` baseline, matching `DynamicCoreSessionPrescriptionOrchestratorTests`' own 79 new tests plus `Phase4G5GPrerequisiteCrossChecksTests`' own 46 new tests exactly — `DynamicCoreVolumeAndLongRunOrchestratorTests`' own test count (51) is unchanged, only its dark-reachability test was renamed).

## 13. Stop conditions — none triggered

- Pace derived from experience alone: **not triggered** — `PaceSourceResolver` never reads `Level`/`RunningBackground`; confirmed structurally impossible.
- `NotEvaluated` silently falls back: **not triggered** — Section 6 confirms `GOAL_PACE_TEN_K` never appears under `NotEvaluated`/`Unsupported`/`TargetTimeNoIndependentEvidence`.
- Quality density becomes unsafe: **not triggered** — Section 7 confirms session count/structure unchanged at every horizon.
- Goal-pace feasibility bypassed: **not triggered** — `ExactPace` only ever appears with `REALISTIC`/`CHALLENGING` feasibility, confirmed across all 35 combinations.
- 12-week pace output changes unexpectedly: **not triggered** — Section 10's regression test proves exact equality.
- Long-run share caps fail to hold across the matrix: **not triggered** — Section 2.1 confirms 42/42 pass.
- Taper not meaningful at any compressed horizon: **not triggered** — Section 2.2 confirms 4/4 pass.
- Zero-call-site proof cannot be established: **not triggered** — established via grep and two executable tests.

## 14. Commit/push status

No file was staged. No commit or push was performed.
