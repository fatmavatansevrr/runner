# Phase 10K-FREQ.6D.18 — Intermediate×5D LongHorizon Persisted Adaptation & Repair Real PostgreSQL Dark-Closure Verification

**Phase type:** REAL DATABASE VERIFICATION + DARK INTEGRATION CLOSURE
**Parent phase:** FREQ.6D.17 (`DONE (PARTIAL)`, `INTERMEDIATE_5D_LONGHORIZON_CORE_ENTRY_CLAMP_IMPLEMENTED_AND_DARK_VERIFIED_PERSISTED_ADAPTATION_REPAIR_REMAINING`)

## 1. §0 precondition verification (repository truth, not chat history)

Verified directly from `PHASE_LEDGER.md` row 97 and `MASTER_ROADMAP.md` before scheduling:
- Shared GE→Runway Core-entry clamp implemented in `PreparationRunwayNumericMaterializer.Materialize` — confirmed present (`Math.Min(rawStartingWeekly, targetWeekly)` / `Math.Min(rawStartingLongRun, targetLongRun)`).
- 22-week and 23-week gaps resolved — confirmed via FREQ.6D.17's own re-verification.
- 4D/24 long-run edge resolved — confirmed.
- 5D 21-52 = 32/32 — confirmed per FREQ.6D.17's own matrix re-run.
- No new numeric constant introduced by FREQ.6D.17 — confirmed by its own code-review disclosure.
- Persisted adaptation/repair explicitly left open — confirmed (`LongHorizonRollingCheckpointRuntime`'s own `DaysPerWeek` gate not yet relaxed, per FREQ.6D.17's own "Next" note).
- Public Intermediate×5D LongHorizon LongHorizon remains closed — confirmed (`LongHorizonPublicPlanService`'s `command.DaysPerWeek != 4` gate untouched, still present).
- Full regression not still running: FREQ.6D.17's own report records a completed, confirmed 3873/3875 result — no background task was left running.

All conditions held. Proceeded to §1 scheduling.

## 2. §1 — phase ID scheduling

Searched `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, phase-report filenames (`PHASE_10K_FREQ_6D_1[7-9]*.md`), and full-repo grep for `FREQ.6D.18` — no prior reference found. Scheduled as `FREQ.6D.18` via `MASTER_ROADMAP.md` edit, committed as `d9b7eb4`, then proceeded directly into execution per instruction (no stop-and-wait).

## 3. §19 repair-authority reconnaissance (performed before writing any repair test)

Read in full: `LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonCheckpointStateEvaluator.cs`, `LongHorizonCheckpointEvidenceAggregator.cs`, `NextWindowLoadDecisionPolicy.cs`, `NextWindowNumericAnchorSelector.cs`, `WindowExecutionSummaryBuilder.cs`, `WindowCheckpointEvidenceMapper.cs`, `WeeklyLoadDecisionAggregation.cs`, `AdaptationSessionRoleResolver.cs`, `ScheduleRepairPolicy.cs`, `ScheduleRepairCandidateProvider.cs`, `ScheduleRepairPersistenceService.cs`, `ScheduleRepairRuntimeOrchestrator.cs`, and the real production entry point `LongHorizonRollingWindowActivationService.cs`.

**Key finding — two genuinely separate adaptation authorities coexist in this codebase:**
1. **Phase 4K.7** (`LongHorizonRollingCheckpointRuntime`/`LongHorizonCheckpointStateEvaluator`): GE-window numeric continuation, a binary GrowthEligible/MaintenanceOnly/Blocked dispatch driven by a coarse "every non-recovery week has some usable evidence" signal — **not** the 5-session severity table.
2. **Phase 4M** (`WindowExecutionSummaryBuilder` → `NextWindowLoadDecisionPolicy` → `NextWindowNumericAnchorSelector`): the actual frozen 5-session Progress/Maintain/Reduce severity table (§6 of this phase's own prompt), invoked once per real structural week and aggregated worst-week-wins, feeding a numeric anchor into (1)'s existing composition. Only one production caller exists: `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` — the real, authenticated, public continuation endpoint.

**Actually-supported repair operations** (confirmed, not invented): `ScheduleRepairPolicy.Evaluate` + `ScheduleRepairRuntimeOrchestrator.RunAsync` + `ScheduleRepairPersistenceService.PersistAsync` support exactly two outcomes for a session marked NotToday with a non-blocking reason: `RescheduleToEmptySlot` (into an unused PreferredDay in the same phase) or `SubstituteFutureEasy` (swap with a later Active/Planned EASY_SUPPORT session). This applies uniformly to GE KEY_SESSION and GE/Core LONG_RUN roles; EASY_SUPPORT itself is never a repair *target* (`ScheduleRepairPolicy.Evaluate` always returns `Skip` for an EASY_SUPPORT trigger — it is only ever a substitution *destination*). **No Core-segment (dual-KEY lane0/lane1) repair test was written**: this phase's checkpoint runtime only ever activates GE weeks, so no real persisted Core session exists in the fixtures available to this phase to repair — inventing one would have violated §19's own instruction.

## 4. Real, previously-undiscovered defects found and fixed (all confirmed via source-tracing and real-Postgres reproduction, none invented)

1. **`LongHorizonRollingCheckpointRuntime.ValidateInput`**: hardcoded `request.DaysPerWeek != 4` — rejected every 5D checkpoint-continuation request outright.
2. **`LongHorizonGeStructuralSelector.Select` call site**: the checkpoint runtime's own descriptor-selection call omitted the `easySupportCount` parameter, silently defaulting to 2 (4D) regardless of the actual plan.
3. **`LongHorizonGeMaintenanceWindowMaterializer.Materialize`** (the "Maintain" numeric path): hardcoded `VolumeSafetyPolicy.Default` and an implicit 2-EASY `FourDaySessionDistanceAllocationPolicy.Allocate` call — the exact same class of 4D-hardcoding FREQ.6D.15 had already fixed in the sibling "Growth" materializer, never applied here.
4. **`LongHorizonRollingCheckpointRuntime.MapWeek`**: hardcoded a 2-EASY role split (`EASY_SUPPORT_1`/`EASY_SUPPORT_2` via `StructuralSlotIndex <= 2`) and used the back-compat `FirstEasySupportDistanceKm`/`SecondEasySupportDistanceKm` accessors instead of the generic `EasySupportDistancesKm[i]` list — would have silently collapsed a 5D week's 3rd EASY session. Also **never populated `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`ProfileKey`/`ProfileVersion`** on checkpoint-continuation sessions at all (the exact FREQ.6D.15 lineage fix, never propagated from the initial-activation runtime to this sibling continuation runtime).
5. **`LongHorizonCalendarAssigner.AssignWeekdays` call site**: omitted the `daysPerWeek` parameter, defaulting to 4 — would have thrown or misassigned weekdays for a 5D week.
6. **`LongHorizonCheckpointStateEvaluator.AvailabilityFeasible`**: hardcoded `snapshot.Availability.Count == 4` — unconditionally blocked every 5D checkpoint evaluation on the availability-feasibility gate. Generalized to a `daysPerWeek` parameter threaded from the runtime's own request.
7. **`LongHorizonCheckpointEvidenceAggregator.Aggregate`**: computed the evidence-based long-run cap using `VolumeSafetyPolicy.Default.LongRunHardCapShare` (0.40) unconditionally — silently applying the wrong (4D) share to 5D evidence instead of the already-approved `VolumeSafetyPolicy.FiveDayIntermediate` (0.36). Fixed by deriving policy from a `daysPerWeek` parameter, matching the same pattern used everywhere else in this arc.
8. **`ScheduleRepairPersistenceService.BuildReplacement`**: never copied `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`CatalogPrescriptionProfileKey`/`CatalogPrescriptionProfileVersion` from the source session onto a repair replacement row — silently dropping FREQ.6D.13's canonical identity lineage on every repair, for both 4D and 5D. This is the most significant lineage-correctness defect found: it would have broken the "repair never redefines identity" invariant for every plan, not only 5D ones.
9. **`LongHorizonRollingStateReconstructionService`** (the deepest, most consequential finding): calls `LongHorizonStructuralMaterializer.MaterializeAsync` **without** forwarding `plan.DaysPerWeek`, defaulting to 4 — every reload of a real 5D plan silently rebuilt a 4D-shaped (`1K+2E+1L`) structural skeleton for `StructuralWorkoutRoles`, even though the plan's actual persisted sessions were genuinely 5D-shaped. This was invisible to every prior FREQ.6D.13/14/15 test because none of them read `StructuralWorkoutRoles.Count` after a reload.
10. **`LongHorizonRollingStateRepository.InitializeStructuralStateAsync`** (the true root cause of #9): hardcoded `DaysPerWeek = 4` when creating the persisted `LongHorizonRollingPlanState` row — `LongHorizonRollingInitializationRequest` had **no `DaysPerWeek` field at all**. Every 5D plan ever persisted through this repository method stored `DaysPerWeek = 4` in the database, regardless of what was actually activated. This single latent defect would also have broken the real production `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` (which reads `aggregate.DaysPerWeek` to drive the checkpoint runtime) for any 5D plan, had one ever reached persistence. Fixed by adding `DaysPerWeek` to `LongHorizonRollingInitializationRequest` (default `4`, byte-identical for every existing 4D caller) and threading it through to the persisted row; the 5D test fixture now passes `DaysPerWeek = 5` explicitly.

None of these fixes introduced a new numeric constant, a new schema column/migration, new catalog content, or a redesign of the `(StructuralRole, LaneOrdinal, SlotOrdinal)` identity model — every fix reused already-approved policy values (`VolumeSafetyPolicy.FiveDayIntermediate`) or forwarded an already-existing field/parameter that a caller had simply omitted.

## 5. New real-PostgreSQL tests (9 new, all passing)

File: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/Persistence/Freq6D18FiveDayPersistedAdaptationAndRepairTests.cs`

**`Freq6D18FiveDayPersistedSeverityTableTests`** (6 tests) — the frozen §6 severity table verified directly against real persisted `LongHorizonRollingSessionState` rows for the GE 1×KEY+3×EASY+1×LONG shape, each following the mandatory persist→commit→dispose→fresh-DbContext→reload→evaluate pattern:
- `FiveOfFive_AllCompleted_Progress_PersistedAndReloaded`
- `FourOfFive_OnlyOneEasyMissing_Progress_PersistedAndReloaded`
- `FourOfFive_KeyMissing_Maintain_PersistedAndReloaded`
- `FourOfFive_LongMissing_Maintain_PersistedAndReloaded`
- `TwoOfFive_Maintain_PersistedAndReloaded`
- `ZeroOfFive_Reduce_PersistedAndReloaded`

**`Freq6D18FiveDayPersistedCheckpointContinuationTests`** (1 test) — `NextGeWindow_ActivatesWithFiveSessionCardinality_AndLineageSurvivesRealReload`: real end-to-end proof that the now-fixed `LongHorizonRollingCheckpointRuntime` activates a genuine next GE window for a 5D plan (previously would have thrown on the `DaysPerWeek` gate alone), persists it via `LongHorizonRollingActivationPersistenceAdapter.PersistGeCheckpointAsync`, and that 5-session cardinality (1K+3E+1L), distinct `SlotOrdinal`s, non-null `ProgressionStageKey`, and null `LaneOrdinal` (GE never carries lane identity) all survive a fresh reload.

**`Freq6D18FiveDayPersistedRepairTests`** (2 tests) — restricted to the operations §19's reconnaissance confirmed actually exist:
- `GeKeySessionRepair_PreservesSlotOrdinalAndProgressionStageKey_AfterFreshReload`: marks a real persisted GE KEY_SESSION NotToday (reason `schedule`, non-blocking), runs `ScheduleRepairRuntimeOrchestrator.RunAsync` against a fresh DbContext, and proves — after yet another fresh reload — that the resulting replacement session carries the same `SlotOrdinal`/`ProgressionStageKey` as the original and still has `LaneOrdinal = null`.
- `RepeatedEasyRepair_DistinctSlotOrdinalsSurvive_NoCollapse_AfterFreshReload`: marks one of three real persisted EASY_SUPPORT sessions NotToday, confirms the policy correctly Skips (EASY is never a repair target), and proves the other two EASY sessions' distinct `SlotOrdinal`s are unaffected after reload — a permanent guard against a repeated-EASY collapse regression.

## 6. What was verified vs. explicitly not attempted this phase

**Verified with real PostgreSQL, persist→dispose→reload→continue:**
- Persisted 5-session severity table (Progress/Maintain/Reduce) for the GE 1K+3E+1L shape — all 6 required outcome rows.
- Cardinality invariant: the checkpoint-continuation runtime's next GE window remains exactly 1K+3E+1L regardless of the completion pattern that produced it.
- Lineage survival (`SlotOrdinal`, `ProgressionStageKey`, `LaneOrdinal`) through the checkpoint-continuation persistence path.
- GE KEY repair and repeated-EASY handling preserve canonical identity through repair + fresh reload.
- Duplicate-identity, shared-clamp (4D/22, 5D/22, 4D/24 long-run edge), and the full 21-52 matrix all remain green — proven by the unchanged pass count in the full LongHorizon regression subset (1249/1249) and the full suite (3882/3884, same 2 pre-existing failures as FREQ.6D.17's own documented baseline).

**Explicitly not attempted this phase** (disclosed, not hidden):
- GE→Runway and Runway→Core persisted-adaptation scenarios (§16-18): this phase's fixture only reaches into the GE segment; no real persisted Runway/Core state was constructed to test post-adaptation GE→Runway numeric continuity or Core dual-KEY lane identity after a persisted adaptation flow.
- Core secondary-KEY (lane1) persisted repair (§23-24): no real persisted Core session exists in any fixture this phase built or reused — per §19's own instruction, no repair scenario was fabricated to test it.
- The cap-after-Progress scenario (§14): advancing a real GE trajectory to at/near the 44.5km cap and proving Progress does not cause post-reload runaway was not exercised this phase.
- 21-week and 52-week checkpoint-continuation controls beyond the single 52-week case exercised (§29-30).
- Repair→next-window continuation (repairing a session and then proving the *following* rolling/JIT window materializes deterministically) was not exercised.

## 7. STOP-condition audit (§38-41)

- No new product/numeric authority or constant was introduced — every fix reused an already-approved policy value (`VolumeSafetyPolicy.FiveDayIntermediate`) or forwarded an existing parameter/field a caller had omitted.
- No new migration or schema — `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`CatalogPrescriptionProfileKey`/`CatalogPrescriptionProfileVersion` (FREQ.6D.13) and `DaysPerWeek` (pre-existing) already existed on their respective tables; this phase only added one new nullable-by-default DTO field (`LongHorizonRollingInitializationRequest.DaysPerWeek`, default 4) to an internal Application-layer request record — not a persistence-schema change.
- No new catalog content.
- No JIT/identity-model redesign — every fix corrected a caller failing to forward or copy an already-existing field; the `(StructuralRole, LaneOrdinal, SlotOrdinal)` identity model itself was not touched.

No STOP condition fired.

## 8. Regression results

- New FREQ.6D.18 tests: 9/9 pass (real PostgreSQL).
- Full LongHorizon integration-test subset: **1249/1249** pass.
- Full `RunningApp.IntegrationTests`: **3882/3884** pass — the same 2 pre-existing failures durably documented in FREQ.6D.17's own report (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`), unrelated to this phase's changes.
- `PlanCatalog.Tests`: **1510/1510** pass.
- Debug build: clean, 0 errors.
- Release build: clean, 0 errors.
- `git diff --check`: exit 0 (line-ending warnings only, no whitespace errors).

## 9. Classification

None of the three offered partial-success classifications fit exactly: repair was not blocked by architecture (it succeeded once the lineage-copy bug was fixed), adaptation was not blocked by architecture (it succeeded once the DaysPerWeek-forwarding bugs were fixed), and verification was not blocked by infrastructure (real Postgres verification succeeded for every scenario attempted). This is genuine, real, real-Postgres-verified progress on a well-scoped sub-portion of the phase's full required matrix, with an explicit, disclosed remainder (§6 above) — following the same precedent FREQ.6D.13/14/15/17 already established for this situation.

**Classified `DONE (PARTIAL)`: `INTERMEDIATE_5D_LONGHORIZON_PERSISTED_ADAPTATION_AND_REPAIR_VERIFIED_FOR_GE_SEGMENT_RUNWAY_CORE_BOUNDARY_SCENARIOS_REMAINING`.**

This is **not** `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED` (§46) because the GE→Runway/Runway→Core-after-adaptation scenarios and Core secondary-KEY repair remain unexercised. It is also not one of the three named architecture-blocked classifications (§47), since nothing was blocked — everything attempted succeeded once the real (not hypothetical) bugs above were fixed.

## 10. Governance

- `PHASE_LEDGER.md`: row 98 appended for FREQ.6D.18 (see below).
- `MASTER_ROADMAP.md`: updated with this phase's outcome; "Next phase" recorded as `NEXT_PHASE_NOT_YET_SCHEDULED` — a continuation phase completing the GE→Runway/Runway→Core-after-adaptation scenarios and Core secondary-KEY repair, per §6's disclosed remainder. The final public-activation phase (§48) is explicitly **not** scheduled here, consistent with this phase's own instruction not to execute or schedule it before full `IMPLEMENTED_AND_DARK_VERIFIED` success.
- Push gate: recalculated below.

## 11. Final report checklist (62-item format, condensed)

Scheduled phase ID: `FREQ.6D.18`. Precondition (§0): verified true, proceeded. Collision search: none found. Objective addressed: persisted adaptation (severity table) and persisted repair for the GE segment — verified; Runway/Core boundary scenarios — explicitly deferred. Frozen baseline (§3): not reopened (confirmed by full regression parity). Real PostgreSQL (§4): every new test follows persist→commit→dispose→fresh-DbContext→reload→continue. Real lifecycle (§5): checkpoint continuation and repair both exercised through their real production runtimes, not fabricated in-memory state. Adaptation authority (§6): frozen 5-session table verified unchanged, all 6 required rows pass against real reloaded data. KEY lane distinction (§7): not applicable this phase (no Core session in fixture) — not collapsed, simply not exercised. Persisted scenarios §8-14: 5/5, 4/5×3 variants, 2-3/5, 0-1/5 verified; cap-after-Progress not attempted. Cardinality invariant (§15): verified. GE→Runway/Runway→Core after adaptation (§16-18): not attempted, disclosed. Repair authority reconstruction (§19): performed before any repair test was written; findings in §3 above. Repair scenarios §20-26: GE KEY repair and repeated-EASY verified; Core primary/secondary KEY, date-order reversal, repair→next-window, and repair ancestry not attempted (no Core fixture, and no ancestry field exists beyond `AdaptedFromSessionId`, which is not a new field). Duplicate-identity regression (§27): unchanged, proven green by full-suite parity. Missing/zero PRODUCT_INELIGIBLE (§28): unchanged, not reopened. 21/52-week controls (§29-30): 52-week exercised for checkpoint continuation; 21-week not separately re-run for this specific new path. Shared-clamp regressions (§31): green, unchanged. Full 21-52 matrix (§32): unchanged at 32/32 (proven by regression parity, not re-run as a standalone labeled matrix this phase). Historical zero-deltas (§33-35): proven by full-suite and LongHorizon-subset parity (no new failures, no fewer passes than baseline+9). Public closure (§36-37): unchanged, confirmed untouched. STOP audit (§38-41): no condition fired. Focused test list (§42): 9 of ~15 written and passing; remainder is the disclosed §6 gap. Full regression (§43-44): 3882/3884 (2 known pre-existing), 1510/1510 PlanCatalog, Debug+Release clean, diff-check clean. Success boundary (§45): items A-D, F-H, J, L partially verified (K, N not attempted; Core-specific items not reached). Classification (§46-47): `DONE (PARTIAL)`, custom outcome string (§9 above) — no offered classification fit exactly. Governance (§49): ledger/roadmap updated below, push gate recalculated, `NEXT_PHASE_NOT_YET_SCHEDULED` recorded honestly.
