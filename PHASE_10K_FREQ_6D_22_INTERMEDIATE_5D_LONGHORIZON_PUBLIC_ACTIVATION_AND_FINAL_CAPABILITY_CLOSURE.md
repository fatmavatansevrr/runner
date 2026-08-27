# Phase 10K-FREQ.6D.22 — Intermediate×5D LongHorizon Real Public HTTP/PostgreSQL Verification, Public Activation & Final 5D Capability Closure

## 1. Preflight

- Verified FREQ.6D.21 governed complete: `TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_IMPLEMENTED_AND_VERIFIED` / `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED` (MASTER_ROADMAP.md line 82, PHASE_LEDGER.md row).
- Confirmed repository truth (not chat history): TrainingPlan persists `TargetFinishTimeSource`, no inference, organic Core `GOAL_PACE_TEN_K` works, UserDefined/historical-null fail closed, public LongHorizon gate was CLOSED (4D-only) prior to this phase.
- Determined next free ID `FREQ.6D.22` from `MASTER_ROADMAP.md`'s own "Next phase" pointer (line 90) — not assumed.
- Gate D pre-activation push already performed and recorded prior to this phase's work (commit `863bad6`); this phase's own closing push is the durability checkpoint for the just-completed activation (§ below).

## 2. Public Gate Reconnaissance

Traced every gate blocking Intermediate×5D LongHorizon 21–52 in `LongHorizonPublicPlanService.cs`. Found **7 hardcoded-4D sites** (not 6, as a mid-phase defect discovered a 7th during end-to-end verification — see §4):

| # | Site | Before | After |
|---|------|--------|-------|
| 1 | `ValidatePilot` | `DaysPerWeek != 4` | `DaysPerWeek is not (4 or 5)` |
| 2 | Candidate resolution | hardcoded `CandidateKey`/`CandidateVersion` (4D constants) | `V1CatalogPilotIdentityPolicy.ResolveCandidate(command.Level, command.DaysPerWeek)` |
| 3 | `LongHorizonRollingInitialActivationRequest.DaysPerWeek` | `= 4` | `= command.DaysPerWeek` |
| 4 | `LongHorizonPublicPreviewMapperInput.DaysPerWeek` | `= 4` | `= command.DaysPerWeek` |
| 5 | `PlanPreview.TemplateId` | `= CandidateKey` | `= resolvedCandidateKey` |
| 6 | `BuildTrainingPlan`'s `DaysPerWeek` | `= 4` | `= snapshot.Command.DaysPerWeek` |
| 7 | `LongHorizonRollingInitializationRequest.DaysPerWeek` (confirm-time) | omitted (record default `= 4`) | `= snapshot.Command.DaysPerWeek` |

No new identity was invented — candidate resolution reuses the same `(Intermediate, 4)/(Intermediate, 5)` dispatch table Preparation Runway already uses. `IsSupportedPreparationRunwayIdentity`'s own set was not widened.

## 3. Mid-Phase Defect Found and Fixed (§49 boundary)

The 7th site (`LongHorizonRollingInitializationRequest.DaysPerWeek`) was missed in the initial reconnaissance because the record's `DaysPerWeek` property carries a default value of `4` (documented in-file as deliberately matching "the previous hardcoded `plan.DaysPerWeek = 4`" for pre-5D callers) and the confirm-time object initializer never set it. This silently persisted every publicly-confirmed 5D plan's `LongHorizonRollingPlanState.DaysPerWeek` as `4`, so the very first real continuation call reconstructed a 4D-shaped structural roadmap (`StructuralWorkoutRoles.Count == 4`) while 5 real sessions had been completed, tripping `LongHorizonCheckpointEvidenceSnapshotValidator`'s `CompletedRunsCount cannot exceed PlannedRunsCount` guard and surfacing as `LONG_HORIZON_CONTINUATION_BLOCKED` / `EvidenceConflictUnresolved`.

Root cause was isolated via a sequence of temporary, targeted diagnostics (thrown exceptions / accumulator logs) at each layer of the checkpoint pipeline (`LongHorizonCheckpointEvidenceAggregator` → `LongHorizonRollingCheckpointRuntime` → `LongHorizonRollingWindowActivationService`), each inserted, run, read, and reverted once superseded — never guessed. This satisfies §49 exactly: the defect is implementation-only (a record-default a caller forgot to override), fully determined by existing authority (the same `command.DaysPerWeek` already threaded through six other sites), introduces no new schema/product/numeric decision, and no identity redesign. Fixed with one line: `DaysPerWeek = snapshot.Command.DaysPerWeek` added to the `LongHorizonRollingInitializationRequest` object initializer at the confirm boundary.

## 4. Rollback Mechanism (§50)

No new feature-flag system was introduced (none existed). Rollback is a straight revert of the diff to `LongHorizonPublicPlanService.cs`: restore `ValidatePilot`'s `DaysPerWeek != 4`, remove the `V1CatalogPilotIdentityPolicy.ResolveCandidate` call (restore hardcoded `CandidateKey`/`CandidateVersion`), and restore the four `DaysPerWeek = 4` / `TemplateId = CandidateKey` hardcodes plus dropping the `DaysPerWeek = snapshot.Command.DaysPerWeek` line from the initialization request. Single file, single commit, `git revert`-able cleanly.

## 5. Tests Added

`Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests.cs` — 14 tests, all passing:

- Representative horizons (21, 22, 23, 24, 32, 52) — exact `TEN_K__5D__INTERMEDIATE` identity, 5 sessions/week (1 KEY, 3 EASY, 1 LONG).
- Full 21–52 matrix — 32/32 route to `rolling_long_horizon`.
- ProductAverage and UserDefined — HTTP confirm, fresh-PostgreSQL reload, exact source preserved.
- Missing readiness — typed non-`UNSUPPORTED` response (`PRODUCT_INELIGIBLE` class).
- **`PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithDualKeyAndSurvivesRepair`** — the principal proof: real public preview → confirm → Home/Calendar reads → 3 real `activate-next-window` HTTP continuations with real session completions → real GE(1wk)→Runway(8wk)→Core(week 10) → organic Core dual-KEY (lane0/lane1 distinct, `ProgressionStageKey`/`CatalogPrescriptionProfileKey`/`Version` populated) → real repair via `ScheduleRepairRuntimeOrchestrator` on the organically-materialized secondary KEY, verifying `LaneOrdinal=1` survives on the replacement and primary stays `LaneOrdinal=0` → **Adaptation regression (§32)**: asserts the real public plan's full-adherence GE week (5/5 completed) drives a genuine `ProgressAsPlanned` outcome, i.e. the newly-activated Runway week's persisted `WeeklyVolumeKm` is not reduced below the completed GE week's volume — proven from real persisted data originating from the public plan, not a dark fixture.
- Unsupported neighbors (Beginner×5D, Intermediate×6D, Intermediate×7D) — remain `LONG_HORIZON_PILOT_UNSUPPORTED`, no identity leakage of either 4D or 5D candidate keys.

## 6. Regression

- `RunningApp.IntegrationTests`: **3908/3910** — same 2 pre-existing failures durably documented since FREQ.6D.17 (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`), unrelated to this phase. 3894 (FREQ.6D.21 baseline) + 14 new = 3908, zero new failures — verified twice (identical signature both runs).
- `PlanCatalog.Tests`: 1510/1510.
- Debug and Release builds: 0 errors (pre-existing nullable-reference warnings only, unrelated to this phase).
- `git diff --check`: clean.

## 7. Production Diff Audit (§48)

Single production file touched: `LongHorizonPublicPlanService.cs`. Zero new numeric values, zero new product/schema authority. Every change threads an already-existing, already-validated value (`command.DaysPerWeek`, `V1CatalogPilotIdentityPolicy.ResolveCandidate`) through a previously-hardcoded seam. No migration.

## 8. Success Boundary (§58) — Status

All items A–Y close: public gate opened to the exact minimum identity set (no wildcard `DaysPerWeek >= 5`), representative + full 32/32 HTTP routing proven, ProductAverage/UserDefined correctness proven, missing/zero readiness typed, `DaysPerWeek=5` and `TargetFinishTimeSource` persist across fresh reload, Home/Calendar work, GE→Runway→Core public lifecycle valid, organic Core dual-KEY valid, repair regression valid, Adaptation regression valid, all zero-delta regressions hold (5D Core/Runway, 4D LongHorizon, 3D Intermediate, Beginner×4D — all covered by the unchanged 3908/3910 signature), unsupported neighbors remain closed, no silent fallback, no new numeric/product/schema authority introduced.

## 9. Final Classification

`INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`
`INTERMEDIATE_5D_FULL_HORIZON_CAPABILITY_COMPLETE`

Accumulated Intermediate×5D matrix: Core (8–14) PUBLIC, Runway (15–20) PUBLIC, LongHorizon (21–52) PUBLIC.

Per §60: this does **not** mean all 10K is complete. Beginner×5D, Advanced×5D, Intermediate×6D/7D remain closed/unresolved. Only Intermediate×5D's full horizon capability is complete.

Next capability: Intermediate 6D+7D combined evidence/product/numeric authority closure — scheduled separately (see roadmap), not implemented in this phase.
