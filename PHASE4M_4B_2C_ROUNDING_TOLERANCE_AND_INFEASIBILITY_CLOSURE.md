# Phase 4M.4B.2C — Rounding Tolerance + Core Infeasibility Policy Closure

## 1. Canonical Revision 4.1 file

`appsel-adaptation-v1-canonical-spec — Revision 4.1.md` (new). Retains all Revision 4 content verbatim; adds a Rev4.1 revision note and two new §7 subsections: **ROUNDING PRODUCT DEFAULT** and **TARGET PRESCRIPTION INFEASIBILITY**, plus corresponding entries in §10 (UI), §11 (non-goals), §12 (BACKLOG), and §13 (implementation notes 10–11).

## 2. Rounding policy encoded

Rev4.1 §7 ROUNDING PRODUCT DEFAULT: *"Maintain, ProgressAsPlanned'i MATERYAL olarak aşmamalıdır. 'Materyal' = relative deviation > %1.5. Rounding-only sapma (≤ %1.5) kabul edilebilir PRODUCT DEFAULT'tur."* Explicitly labeled PRODUCT DEFAULT, not a scientific threshold. Explicitly lists what was *not* done: no downward clamp on Maintain, no upward inflation of ProgressAsPlanned, no change to session-distance rounding or catalog progression, no runtime epsilon constant (tolerance lives only in the test/governance acceptance layer).

## 3. Tolerance classification

**PRODUCT DEFAULT**, per the explicit instruction — calibrated from the real observed maximum (1.36%), not derived from any external/scientific standard.

## 4. Sweep test implementation

`MaintainNotExceedingProgressAsPlannedInvariantTests.Maintain_DoesNotMateriallyExceedProgressAsPlanned_BeyondRoundingTolerance` (renamed from the prior strict-invariant test, not deleted — same deterministic generation, same 200-iteration loop, same real `LongHorizonGeNumericExecutor` comparison quantity). Compares the exact same quantities as before (Maintain's held input vs. `numeric[0].TotalVolumeKm`, the real catalog's week-1 progressed output) and additionally records absolute/relative deviation per case. `RoundingToleranceRelativeDeviation = 0.015` is a `private const double` local to the test file — deliberately **not** promoted to a runtime/shared constant, since nothing at runtime compares Maintain against ProgressAsPlanned or needs this value.

## 5. Sweep case count

**183 valid cases** (out of 200 randomized iterations; 17 hit the same real per-session minimum-volume floor `MaintainNotExceedingProgressAsPlannedInvariantTests` already handles by skipping — Rev4.1's own TARGET PRESCRIPTION INFEASIBILITY behavior, not a new finding).

## 6. Strict-order violation count

**94 / 183 (51%)** — `Maintain > ProgressAsPlanned` under the strict `>` comparison. Unchanged from the original 4M.4B.2B finding (same deterministic seed/generation).

## 7. Max absolute deviation

**0.247 km.**

## 8. Max relative deviation

**1.36%** (`0.01361...`) — below the frozen 1.5% tolerance, confirmed by direct test run (temporarily forced to fail to capture the report string, then reverted): *"183 valid cases (+17 infeasible/skipped...), weekly range [5.04-59.64]km, 15 plateaus, 74 growth cases, 94 strict-order violations, maxAbsoluteDeviation=0.247km, maxRelativeDeviation=1.36%, 0 cases beyond tolerance."*

## 9. Cases beyond tolerance

**0.** All 94 strict-order violations are rounding-only and within the 1.5% frozen tolerance. The test's own `Assert.True(beyondTolerance.Count == 0, ...)` passes without any widening of the threshold.

## 10. Catalog infeasibility policy encoded

Rev4.1 §7 TARGET PRESCRIPTION INFEASIBILITY: a valid Maintain/Reduce anchor may be numerically insufficient for the target Core/Runway week's catalog minimum prescription; this is not automatically an adaptation-policy defect. Approved V1 outcome: the existing typed `LONG_HORIZON_CONTINUATION_BLOCKED` response. Explicitly forbidden: increasing the anchor, clamping upward to catalog minimum, weakening catalog minimums, synthesizing lighter workout structure, creating `RecoveryWeek`, rewriting workout content, skipping ahead to another phase/week, or silently falling back to ProgressAsPlanned. §7 also encodes the architectural invariant (Catalog = progression authority, Adaptation = numeric anchor authority) and the corrected multi-window acceptance rule: not every `Reduce → Maintain → ProgressAsPlanned` sequence must succeed; a genuine numeric-infeasibility Block is an approved outcome as long as chronology never falsely advances.

## 11. Current runtime behavior audit

Performed *before* any test changes, per Section F. Traced the exact path: `FourDaySessionDistanceAllocationPolicy` throws `CatalogSessionPrescriptionInfeasibleException` on infeasible allocation → propagates as an unhandled exception up through `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync`'s `CoreGeneration` stage catch block → mapped to `LongHorizonJitReasonCode.CoreJitContextUnavailable` → `LongHorizonRollingJitCompositionOrchestrator` returns a `Blocked` result → `ContinueJitCompositionAsync` persists via `PersistBlockAsync`, marked `IsBlock = true` (the Phase 4M.4B.2A signal) → `LongHorizonRollingWindowActivationService`'s outer switch sees `persistResult.IsBlock == true` → throws `LongHorizonContinuationBlockedException` → HTTP 409 `LONG_HORIZON_CONTINUATION_BLOCKED`. **This exactly matches Rev4.1's frozen rule already, with no gap.**

## 12. Production code changes, if any

**None.** Per the audit in §11, current behavior already implements the newly-frozen canonical rule exactly. No production files were modified in this phase.

## 13. Feasible Maintain proof

`LongHorizonThreeWindowAnchorThreadingE2ETests.RealMaintainActivation_UsesPriorValidatedCheckpointLoadVerbatim_GenuinelyAdvancesWindow` (pre-existing from 4M.4B.2B, still passing, unmodified) — real HTTP Maintain activation succeeds, window genuinely advances, fresh-DB confirms materialized numeric target matches the held (Maintain) anchor.

## 14. Infeasible Maintain proof

`LongHorizonThreeWindowAnchorThreadingE2ETests.RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement` (pre-existing from 4M.4B.2B, still passing, unmodified) — Reduce succeeds and threads correctly, the subsequent Maintain attempt with a too-small carried anchor Blocks with `CoreJitContextUnavailable`, window range and activation-record count unchanged (no false advancement).

## 15. Feasible Reduce proof

`LongHorizonThreeWindowAnchorThreadingE2ETests.RealReduceActivation_ThreadsAnchorCorrectly_GenuinelyAdvancesWindow` (pre-existing from 4M.4B.2B, still passing, unmodified) — real HTTP Reduce activation succeeds, window genuinely advances.

## 16. Infeasible Reduce proof

**New in this phase**: `LongHorizonThreeWindowAnchorThreadingE2ETests.RealReduceLandingOnRunwayCoreBoundary_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement` — a Reduce decision (not Maintain) whose anchor is too small for the target Core/Runway week also Blocks via the identical `CoreJitContextUnavailable` mechanism, proving the Block is feasibility-based, not decision-enum-based. Window range and activation-record count confirmed unchanged.

## 17. No-upward-clamp confirmation

Confirmed by the §11 code audit (no clamp/increase logic exists anywhere on the Block path) and empirically by §14/§16: the anchor value that entered composition is never modified upward before the Block is raised — the exception is thrown from inside session-distance allocation itself, before any persistence occurs.

## 18. No-false-success regression

`LongHorizonRollingPersistenceResult.IsBlock` signal propagation (Phase 4M.4B.2A's fix) re-verified green: both §14 and §16 assert `CurrentWindowStartWeek/EndWeek` remain unchanged and the activation-record count/idempotency-key set remains consistent across the Block.

## 19. Files created

- `appsel-adaptation-v1-canonical-spec — Revision 4.1.md`
- `PHASE4M_4B_2C_ROUNDING_TOLERANCE_AND_INFEASIBILITY_CLOSURE.md` (this file)

## 20. Files modified

- `PHASE4M_4B_2_MAINTAIN_REDUCE_NUMERIC_ANCHOR_IMPLEMENTATION.md` (§36 addendum)
- `PHASE4M_4B_2A_MULTIWINDOW_ACTIVATION_ADVANCEMENT_DEFECT.md` (§22 addendum extended)
- `PHASE4M_4B_2B_CORE_JIT_MAINTAIN_CONTEXT_DEFECT.md` (§22 addendum)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Adaptation/MaintainNotExceedingProgressAsPlannedInvariantTests.cs` (rewritten: strict invariant → tolerance invariant, per §21 below)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Adaptation/LongHorizonThreeWindowAnchorThreadingE2ETests.cs` (added the infeasible-Reduce test, §16)

No production (`RunningApp.Application`/`RunningApp.Api`) files modified — confirmed unnecessary by the §11 audit.

## 21. Tests added/updated

- **Updated** (not deleted): `MaintainNotExceedingProgressAsPlannedInvariantTests.Maintain_DoesNotMateriallyExceedProgressAsPlanned_BeyondRoundingTolerance` (renamed from `MaintainAnchor_NeverExceedsProgressAsPlannedAnchor_AcrossRandomizedRealCatalogProgression`) — now asserts `relativeDeviation <= 0.015` per case instead of strict `<=`, reports strict-violation count/max absolute/max relative deviation/beyond-tolerance count.
- **Added**: `LongHorizonThreeWindowAnchorThreadingE2ETests.RealReduceLandingOnRunwayCoreBoundary_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement`.

## 22. Exact commands/results

```
dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~MaintainNotExceedingProgressAsPlannedInvariantTests"
  → 1/1 passed

dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests"
  → 4/4 passed
```

## 23. 4M.1 result

`dotnet test --filter "FullyQualifiedName~PlanAdaptationV1DecisionTests"` → **68/68 passed**.

## 24. 4M.2 result

`dotnet test --filter "FullyQualifiedName~ScheduleRepairPersistenceTests"` → **25/25 passed**.

## 25. 4M.3 result

`dotnet test --filter "FullyQualifiedName~RuntimeNotTodayReasonMapperTests|FullyQualifiedName~ScheduleRepairRuntimeOrchestratorTests|FullyQualifiedName~ScheduleRepairSupersededAndReadCorrectnessTests"` → **38/38 passed**.

## 26. 4M.4A result

Covered within the combined targeted run below (`WindowCheckpointSummaryAndDecisionTests`, `LongHorizonNextWindowDecisionActivationTests`) — all passing.

## 27. 4M.4B.2 targeted result

```
dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests|FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests|FullyQualifiedName~LongHorizonFirstCheckpointNumericAnchorTests|FullyQualifiedName~LongHorizonNumericAnchorMaterializationE2ETests|FullyQualifiedName~NextWindowNumericAnchorSelectorTests|FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests|FullyQualifiedName~MaintainNotExceedingProgressAsPlannedInvariantTests"
  → 43/43 passed
```

## 28. LongHorizon result

`dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizon"` → **1096/1096 passed, 0 failed** — zero failures, restored from the prior phase's single intentional failure.

## 29. Full backend regression result

`dotnet test backend/RunningApp.sln` (repo-approved `xunit.runner.json`, `parallelizeTestCollections: false`) — see final chat report for the completed run's exact count (run in background at doc-authoring time).

## 30. Build/git diff result

`dotnet build backend/RunningApp.sln` → 0 warnings, 0 errors. `git diff --check` — clean (only pre-existing CRLF/LF informational warnings unrelated to this phase's changes).

## 31. Remaining DecisionRequired items

**None new.** Both items from Phase 4M.4B.2B §20 are closed via Rev4.1. The rounding tolerance (1.5%) itself remains a PRODUCT DEFAULT subject to future recalibration with real user/production data (Rev4.1 §12 BACKLOG) — this is an explicitly acknowledged, intentional characteristic of a PRODUCT DEFAULT classification, not an open defect.

## 32. Final classification

```
ADAPTATION_V1_NUMERIC_POLICY_AND_CATALOG_INFEASIBILITY_CLOSED
```

Revision 4.1 exists and encodes both closures as PRODUCT DEFAULT / DECIDED respectively. The real-catalog tolerance sweep found 0 cases beyond the 1.5% tolerance (max observed 1.36%). No runtime clamp was introduced anywhere — confirmed by code audit before any change was made, and the audit found current behavior already matched the frozen rule exactly, so no production code was changed. Catalog minimum authority is untouched. Feasible Maintain succeeds, feasible Reduce succeeds, infeasible Maintain Blocks, infeasible Reduce Blocks — all four proven with real HTTP/DB tests. The Block never masquerades as an activation (4M.4B.2A's `IsBlock` signal re-verified). LongHorizon regression: 1096/1096. Full backend regression: pending confirmation in final chat report. Build green, git diff clean. No Phase 4M.5 work started.

No code committed, no push, Phase 4M.5 not started.
