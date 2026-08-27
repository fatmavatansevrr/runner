# Phase 10K-FREQ.6D.19 — Intermediate×5D LongHorizon Persisted GE→Runway→Core Boundary & Core Dual-KEY Repair Closure

**Phase type:** REAL POSTGRESQL INTEGRATION VERIFICATION + DARK CLOSURE
**Parent phase:** FREQ.6D.18 (`DONE (PARTIAL)`, `INTERMEDIATE_5D_LONGHORIZON_PERSISTED_ADAPTATION_AND_REPAIR_VERIFIED_FOR_GE_SEGMENT_RUNWAY_CORE_BOUNDARY_SCENARIOS_REMAINING`)

## 1-2. Governance and scheduling

Verified FREQ.6D.18 as the latest completed phase (`PHASE_LEDGER.md` row 98, `DONE (PARTIAL)`), confirming its own recorded IMPLEMENTED/VERIFIED and EXPLICITLY-REMAINING lists match this phase prompt's description exactly, and that `MASTER_ROADMAP.md` recorded `NEXT_PHASE_NOT_YET_SCHEDULED`. Searched `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, and phase-report filenames for `FREQ.6D.19` — unreserved. Scheduled `FREQ.6D.19` in `MASTER_ROADMAP.md`, committed as `d34afc7`, then proceeded directly into execution.

## 3. §4 reconnaissance — PERSISTED_5D_LONGHORIZON_BOUNDARY_LIFECYCLE_TRACE

| Boundary | Class / method |
|---|---|
| GE rolling continuation | `LongHorizonRollingCheckpointRuntime.EvaluateAndActivateNextGeWindowAsync` |
| GE adaptation (severity table) | `WindowExecutionSummaryBuilder` → `NextWindowLoadDecisionPolicy` → `NextWindowNumericAnchorSelector` (production caller: `LongHorizonRollingWindowActivationService`) |
| GE→Runway / Runway→Core transition | `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync` → `LongHorizonRollingJitCompositionOrchestrator.ComposeAndActivateNextWindowAsync` |
| Runway numeric materialization | `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync` (first entry) / `LongHorizonRollingJitActivationRuntime` (continuation windows), both calling `PreparationRunwayNumericMaterializer.Materialize` |
| Core session materialization | `TenKPreparationRunwayCoreGenerator.GenerateAsync` → `DynamicCoreCalendarMaterializationOrchestrator` → `CatalogSessionPrescriptionPlanner` (ProfileBacked, `ExecutionPrescriptionIndex`) |
| Calendar alignment/persistence | `LongHorizonRealCalendarProjectionAdapter.MapSelectedWindow`/`AlignActivationResult`, `LongHorizonActivatedCalendarAlignmentValidator.Validate` |
| Repair/reschedule entry point | `ScheduleRepairPolicy.Evaluate` + `ScheduleRepairRuntimeOrchestrator.RunAsync` + `ScheduleRepairPersistenceService.PersistAsync` |
| Checkpoint continuation after repair | Same `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` reused on the next call |

No `TrainingDay` table involvement anywhere in this dark chain — `LongHorizonRollingSessionState` is the sole persisted unit, by original design (see its own doc comment: "Deliberately NOT a TrainingDay row").

## 4. §3 compliance — no fabricated Core fixture

Every Core session observed and asserted in this phase's tests was produced by the real, unmodified `LongHorizonRollingJitCompositionOrchestrator`/`TenKPreparationRunwayCoreGenerator` chain, reached by repeatedly calling `LongHorizonRollingCheckpointRuntime` + `ContinueJitCompositionAsync` against a real, organically-initialized 21-week Intermediate×5D plan (GE=1 week, Runway=weeks 2-9, Core=weeks 10-21) — no direct `INSERT`, no fabricated `LongHorizonRollingSessionState` object, no manually-assigned `LaneOrdinal`/profile lineage.

## 5. Real production defects found and fixed (§35 defect-discovery rule)

All five were discovered by driving the real lifecycle end-to-end for the first time for a 5D plan; none were invented, and each is a caller/validator failing to forward or generalize an already-existing, already-approved value:

1. **`LongHorizonRollingWindowActivationService`** never threaded `PlanCatalogOptions.PublishedBundleReleaseVersion` (the real, already-configured `"1.1.0"` in `appsettings.json`) into `ContinueJitCompositionAsync`. Without it, `PublishedTemplateBundleLoader.TryLoadAsync` always returns null, and Intermediate×5D Core's ProfileBacked `KEY_SESSION` can never resolve an `ExecutionPrescriptionIndex` (Legacy fallback is deliberately forbidden). 4D Core is Legacy throughout and was never affected. **Fixed**: added a public constructor overload taking `IOptions<PlanCatalogOptions>` (DI-resolved automatically; the internal 3-arg test seam keeps its old default), threading the real configured value through.
2. **`LongHorizonRollingJitActivationRuntime`** called `TenKPreparationRunwayNumericPolicyFactory.Build()` (always `VolumeSafetyPolicy.Default`, 30%/36% long-run share) instead of the candidate-aware `Build(candidate)` overload the dark orchestrator already uses — rejecting a genuine, approved 5D week at 28.85% share (inside the approved 28%/36% band) as a `LongRunShareViolation`. **Fixed**: added a `Candidate` field to `LongHorizonRollingJitActivationRequest` (nullable, DTO-only — no schema), threaded from the composition orchestrator, and dispatched on it.
3. **`LongHorizonRealCalendarProjectionAdapter.ValidateRunwayProjectionAuthority`** hardcoded `* 4` for the expected Runway session count. **Fixed**: derives the count from the real prescribed week's own `OrderedSlots.Count` (5 for 5D, byte-identical 4 for 4D).
4. **`LongHorizonActivatedCalendarAlignmentValidator.Validate`** hardcoded `!= 4` for the expected per-week session count. **Fixed**: uses the real numeric week's own `SessionPrescriptions.Count` as the expected value.
5. **`ContinueJitCompositionAsync`** had no way to supply `TargetFinishTimeSeconds`/`TargetFinishTimeSource`/`RecentRace` at all — any Core week containing a `GOAL_PACE_TEN_K` workout (`CatalogSessionPrescriptionPlanner`) requires resolved REALISTIC/CHALLENGING goal-feasibility evidence. **Partially addressed**: added the three parameters (default null, byte-identical for every existing 4D caller) so dark-verification callers can supply them, using this phase's tests to reuse the exact same `TargetFinishTimeSeconds=3480`/`TargetFinishTimeSource.ProductAverage` convention `LongHorizonFullLifecycleTestFixture` already established for 4D dark testing (not a new value). **Not fixed**: `LongHorizonRollingWindowActivationService`'s own real production call still passes none of these — `TrainingPlan.TargetFinishTimeSeconds` exists and could be read, but no `TargetFinishTimeSource` classification is persisted anywhere for a restarted/rolling plan, and choosing how to reclassify it (always ProductAverage? require RecentRace evidence? a new required field?) is a genuine product decision, not resolved here per §36's own STOP instruction. **This is the one disclosed, real, unresolved gap in this phase's success boundary.**

No new numeric constant, schema/migration, catalog content, or `(StructuralRole, LaneOrdinal, SlotOrdinal)`/JIT identity redesign in any of the five fixes.

## 6. New real-PostgreSQL tests (4 new, all passing)

File: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/Persistence/Freq6D19FiveDayGeRunwayCoreBoundaryTests.cs` (+ fixture `Freq6D19FiveDayGeRunwayCoreBoundaryFixture.cs`, the 5D analogue of the proven 4D `LongHorizonRunwayCoreRestartFixture`).

- `OrganicFirstCoreWeek_TwoKeyTwoEasyOneLong_WithDistinctLaneAndSlotIdentity_AfterFreshReload`: drives GE→Runway→Core organically (3 real continuation calls, each a fresh DbContext), then a fresh reload proves the first real Core week has exactly 2 KEY_SESSION (LaneOrdinal 0 and 1, both present, both distinct), 2 EASY_SUPPORT (distinct SlotOrdinal, no LaneOrdinal), 1 LONG_RUN, and 5 distinct SlotOrdinals overall.
- `OrganicCoreKeySessions_ProfileBackedLineageSurvives_AfterFreshReload`: both KEY sessions carry non-null `ProgressionStageKey`, `CatalogPrescriptionProfileKey`/`CatalogPrescriptionProfileVersion` (both present, ProfileBacked), `WorkoutKey`/`WorkoutVersion`, after a fresh reload.
- `SecondaryKeyRepair_UsingRealRepairService_PreservesLane1_PrimaryStaysLane0_AfterFreshReload`: marks the organically-created lane1 KEY session NotToday (reason `schedule`), runs the real `ScheduleRepairRuntimeOrchestrator.RunAsync` against a fresh DbContext, and proves after a further fresh reload that the replacement session is still `LaneOrdinal = 1` with identical `ProgressionStageKey`/profile lineage, the primary KEY session is untouched at `LaneOrdinal = 0`, and both EASY sessions keep distinct SlotOrdinals.
- `RepairThenContinuation_NextWindowMaterializesDeterministically_NoDuplicateOrLostSession`: after the same lane1 repair, continues to the next real window (weeks 14-17) and proves it still materializes exactly 5 sessions with 2 distinct-lane KEY sessions and distinct SlotOrdinals, and that the week-10 repair replacement's execution lineage still resolves after this further continuation.

Every test follows DbContext A (create/mutate/save) → dispose → DbContext B (fresh) → reload → continue, exactly as required.

## 7. Date-order reversal (§21)

Read `ScheduleRepairCandidateProvider.GetEmptySlotCandidates`/`GetFutureEasySubstitutionCandidates`: both explicitly filter `if (date <= trigger.AssignedDate) continue;` / `if (session.AssignedDate <= trigger.AssignedDate) continue;` — every repair candidate this codebase supports is strictly **later** than the trigger's own date, by construction. Moving a secondary KEY to a date **earlier** than the primary is therefore structurally impossible under the current, real, approved repair contract — not attempted with a bypass. **Classified `NOT_REACHABLE_UNDER_VALID_REPAIR_CONSTRAINTS`**, per this phase's own explicit instruction for this exact situation (§21: "If current product repair rules make this exact movement invalid: do NOT bypass those rules... record NOT_REACHABLE"). This is itself a permanent, positive finding: lane identity can never be redefined by a legally-reachable date reorder, because no legal repair can reorder dates that way in the first place.

## 8. §36-39 STOP-condition audit

- No new product/numeric authority, constant, or schema was introduced by any of the five fixes (§5 above).
- The one item this phase could not close (`TargetFinishTimeSource` persistence/classification for a restarted 5D LongHorizon plan reaching a `GOAL_PACE_TEN_K` Core week) is exactly the kind of gap §36 says to STOP on rather than invent — disclosed, not bypassed. Production behavior for this specific scenario is unchanged from before this phase (still blocks); this phase's own dark-verification tests reuse an already-approved test convention to get past it for verification purposes only.
- No repair-architecture contradiction found — the real repair service correctly preserves lane/slot/profile identity once the pre-existing lineage-copy bug (fixed last phase, FREQ.6D.18) was in place.
- No persistence-architecture contradiction — the existing FREQ.6D.13 five-column schema was sufficient for every scenario this phase reached.

## 9. Regression results

- New FREQ.6D.19 tests: 4/4 pass (real PostgreSQL).
- Full LongHorizon integration-test subset: **1253/1253** pass (1249 baseline + 4 new).
- Full `RunningApp.IntegrationTests`: **3886/3888** pass — same 2 pre-existing failures documented since FREQ.6D.17/18 (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`), unrelated to this phase.
- `PlanCatalog.Tests`: **1510/1510** pass.
- Debug build: clean, 0 errors. Release build: clean, 0 errors. `git diff --check`: exit 0 (line-ending warnings only).

## 10. Success boundary (§44) — item by item

A. Real persisted GE Adaptation state crosses into Runway — **verified** (GE=1 week for this horizon; the single GE week's checkpoint-continuation transitions directly into the real persisted Runway entry).
B. Real persisted Runway state crosses into canonical Core — **verified**.
C. First Core week 2K+2E+1L — **verified**.
D. KEY lane0 survives persistence — **verified**.
E. KEY lane1 survives persistence — **verified**.
F. Exact ProfileBacked lineage survives reload — **verified**.
G. Secondary KEY repair uses the real supported service — **verified**.
H. Repaired secondary KEY remains lane1 — **verified**.
I. Primary remains lane0 — **verified**.
J. Legal date reordering does not redefine lanes — **N/A, structurally unreachable under valid repair constraints (§7 above)**, not a failure.
K. Repair→next-window succeeds — **verified**.
L. Repeated EASY identity survives boundaries — **verified**.
M. 4D LongHorizon zero-delta — **verified** (full-suite and LongHorizon-subset regression parity; the existing 4D `LongHorizonCoreOnlyRestartMatrixTests`/`LongHorizonRunwayCorePostgresRestartTests` remain green with all five fixes applied, each confirmed byte-identical for 4D by construction).
N. 5D dark matrix remains green — **verified** (regression parity; no new hole introduced).
O. Core/Runway public regression remains green — **verified** (public 4D/5D Core/Runway tests untouched, regression parity).
P. Public LongHorizon remains closed — **verified** (`LongHorizonPublicPlanService`'s `DaysPerWeek != 4` gate untouched).

## 11. Classification

Every organically-reachable scenario this phase's own objective describes was verified successfully; the one requested scenario that is genuinely unreachable under valid repair semantics (date-order reversal) was correctly classified as such rather than faked, per §46's own explicit instruction ("A scenario that is invalid by product contract is NOT an implementation blocker"). However, one real, disclosed production gap remains: `LongHorizonRollingWindowActivationService` cannot yet supply `TargetFinishTimeSource` for a real (non-test) 5D LongHorizon plan reaching a `GOAL_PACE_TEN_K` Core week, and closing it requires a product decision this phase correctly did not make.

This does not fit `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED` (§45) — that classification requires the *full* accumulated capability, and real production Core generation for a 5D plan with a non-ProductAverage goal-time posture would still fail today. It is classified:

**`DONE (PARTIAL)`: `INTERMEDIATE_5D_LONGHORIZON_GE_RUNWAY_CORE_BOUNDARY_AND_DUAL_KEY_REPAIR_DARK_VERIFIED_TARGET_FINISH_TIME_PRODUCT_DECISION_REMAINING`.**

## 12. Governance

- `PHASE_LEDGER.md`: row 99 appended.
- `MASTER_ROADMAP.md`: updated; next phase recorded as `NEXT_PHASE_NOT_YET_SCHEDULED` — a product-decision phase resolving how a restarted/rolling LongHorizon plan should classify `TargetFinishTimeSource` when reaching a `GOAL_PACE_TEN_K` Core week (the sole remaining item before `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED` can be honestly claimed), followed by the final public-activation phase (§47) — neither scheduled here.
- Push gate: recalculated below.

## 13. Final report (65-item format, condensed)

Scheduled phase ID: `FREQ.6D.19` (scheduling commit `d34afc7`). Preflight: HEAD `c279f41`, working tree clean except pre-existing unrelated `baseline_tmp`/audit-file modifications, branch `main`, 26 commits ahead / 0 behind `origin/main`, `git diff --check` clean. Files changed: 7 production files + 1 new fixture + 1 new test file (§5-6 above). Lifecycle classes/methods: §3 table above. Real DB fixture/horizon: 21-week Intermediate×5D (GE=1, Runway=2-9, Core=10-21), `Freq6D19FiveDayGeRunwayCoreBoundaryFixture`. GE persisted structure: 1K+3E+1L, verified in FREQ.6D.18. Persisted GE adaptation decision: single-GE-week horizon transitions directly (no multi-window adaptation scenario within GE itself for this horizon). Fresh GE reload proof: verified via FREQ.6D.18's own tests, reused. Last GE week / first Runway week: both 1K+3E+1L (GE week 1, Runway week 2), confirmed via the passing organic continuation. GE→Runway numeric continuity / clamp-uses-current-state: unaffected by this phase's fixes (FREQ.6D.16/17's own clamp untouched); not reopened. Runway persisted structure/reload proof: 1K+3E+1L, DaysPerWeek=5, verified via successful continuation calls 1-2. Organic Runway→Core transition: verified (call 3, weeks 10-13). First Core cardinality: 2K+2E+1L verified. KEY lane0/lane1 persisted results: both verified present and distinct. Repeated EASY result: 2 distinct SlotOrdinals verified. ProgressionStageKey/ProfileKey/ProfileVersion reload results: all verified non-null and consistent. PublishedBundleReleaseVersion result: real production defect found and fixed (§5.1). ExecutionPrescriptionIndex result: resolves correctly once (§5.1) fixed. Core DB reload proof: verified via fresh-DbContext queries in all 4 new tests. Secondary KEY repair operation used: `ScheduleRepairRuntimeOrchestrator.RunAsync` (real service, RescheduleToEmptySlot or SubstituteFutureEasy per the real policy). Secondary/primary KEY LaneOrdinal after repair: 1 and 0 respectively, verified. SlotOrdinal after repair: non-null, verified. Repair ancestry result: `AdaptedFromSessionId` correctly links replacement to trigger (no new ancestry field). Date-order reversal result: `NOT_REACHABLE_UNDER_VALID_REPAIR_CONSTRAINTS` (§7). Repair→continuation result: verified deterministic, no duplicate/lost session. Post-repair execution-profile result: verified resolves. Repeated EASY boundary result: verified unaffected. Post-boundary adaptation result: not separately re-exercised (GE=1 week horizon has no post-boundary adaptation scenario to reach cheaply; not required per §26's own "if the real lifecycle naturally produces" qualifier). 4D LongHorizon / 4D-22 / 5D-22 / 4D-24-edge regressions: all green, unchanged (regression parity). 5D 21-52 dark result / 5D Core / 5D Runway regressions: green, unchanged. Missing/zero readiness results: unchanged, not reopened. Unsupported-neighbor result: unchanged, confirmed untouched. Public LongHorizon state: confirmed closed. Focused tests: 4 written (of the 10 named in §40; items 7 and 10 addressed via code-reading evidence rather than a new test, per §7 above and existing regression parity respectively). LongHorizon subset total: 1253/1253. IntegrationTests total: 3886/3888 (2 known pre-existing). PlanCatalog total: 1510/1510. Debug/Release builds: clean. `git diff --check`: clean. Defects discovered/fixed: 5, listed in §5. Baseline failure attribution: both residual failures carry the identical signature durably documented in FREQ.6D.17/18's own reports. Implementation SHAs / governance SHA / resulting HEAD / push-gate result: recorded below. Remaining blocker: `TargetFinishTimeSource` product decision for restarted 5D LongHorizon plans (§5.5, §11). Accumulated LongHorizon dark status: GE/Runway/Core boundary and dual-KEY repair now dark-verified; full `IMPLEMENTED_AND_DARK_VERIFIED` still pending the one disclosed item. Next phase ID: `NEXT_PHASE_NOT_YET_SCHEDULED`. Final classification: §11 above.
