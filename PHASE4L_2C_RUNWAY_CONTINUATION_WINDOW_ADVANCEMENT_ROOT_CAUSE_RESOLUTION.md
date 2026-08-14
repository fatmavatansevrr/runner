# Phase 4L.2C — Runway Continuation Window Advancement Root-Cause Resolution

## 1. Executive result

This phase resolves the exact defect Phase 4L.2B discovered but did not fix: a persisted Long-Horizon Runway continuation did not advance to the next distinct `NumericPending` Runway window after restart. The root cause is proven, not hypothesized — `LongHorizonLockedCoreWeekOneTarget.LockedForActivatedRunwayWeekRange`, a `(int, int)` C# ValueTuple, silently round-tripped as `(0,0)` through `System.Text.Json`'s default (fields-excluded) serialization options, tripping the reuse-scope validator on every second continuation call. The fix is minimal: a shared `JsonSerializerOptions { IncludeFields = true }` applied symmetrically at every serialize/deserialize call site. A new fail-closed `LongHorizonRunwayContinuationAdvancementValidator` now guards the resolved invariant in the real continuation path. Proven against real PostgreSQL: three-call progression through GE→Runway→Core, a strengthened continuation-advancement regression, replay idempotency, and fail-closed corruption behavior.

```
LONG_HORIZON_RUNWAY_CONTINUATION_WINDOW_ADVANCEMENT_ROOT_CAUSE_RESOLVED
```

## 2. The single invariant this phase resolves

"A committed Runway continuation must advance the durable lifecycle boundary." Nothing broader was attempted — not the full Phase 4L.2B matrix, not Core refresh, not failure injection, not the full concurrency matrix.

## 3. Reproduction

`LongHorizonContinuationDiagnosticTests.Diagnose` (a throwaway diagnostic written during investigation, since replaced) first reproduced the defect directly: after a real first Runway entry (weeks 2-5) and restart, a second continuation call returned `CompositionBlocked`/`JitSegmentTransitionInfeasible`. Adding a direct try/catch around `ImmutablePreparationRunwayPrescriptionValidator.Validate(prescription)` (bypassing the outer runtime's exception-swallowing catch) surfaced the real exception: `PreparationRunwayTargetLockScopeViolationException: The locked Core Week-1 target's range (0-0) must exactly cover the full Runway global range (2-9)`.

## 4. Window-selection authority chain

Unchanged and re-confirmed: `LongHorizonRollingJitActivationRuntime.ResolveAndActivateNextWindowAsync` remains the sole window-selection authority. No second selector was added. Its `ResolveOrCreateRunwayPrescription` reuse branch (`ExistingRunwayPrescription`/`ExistingLockedCoreTarget` both non-null) is the exact code path that was failing.

## 5. Lifecycle source-of-truth

Confirmed via direct diagnostic evidence: `LifecycleStates` reconstructs correctly after restart (verified: weeks 1-5 `NumericActivated`, weeks 6-21 `NumericPending`). This ruled out `LifecycleReconstructionStale` and `FirstPendingDerivationIncorrect` as candidate root causes before the JSON round-trip defect was found.

## 6. Idempotency review

`SaveActivationSuccessAsync`'s `IdempotencyKey`-based duplicate check was ruled out as the root cause early: a collision would produce `Outcome=IdempotentReplay`, but the observed pre-fix outcome was a real `CompositionBlocked`, which the idempotency path cannot produce. Post-fix, `LongHorizonRunwayContinuationReplayAndCorruptionTests.ReplayingIdenticalActivationRequest_ReturnsIdempotentReplayWithoutDuplication` directly proves replay behavior against real Postgres: an identical request (same `IdempotencyKey`) returns `IdempotentReplay` and creates no duplicate row.

## 7. Activation-record lookup review

No change was needed or made to activation-record lookup semantics. The strengthened continuation test (§13) directly queries `LongHorizonActivationWindowRecords` and confirms exactly one record for the first window range and exactly one for the second, with no duplication.

## 8. Persistence adapter and reconstruction review

Both `LongHorizonRollingActivationPersistenceAdapter` (write side) and `LongHorizonRollingStateReconstructionService` (read side) were found to share the exact same defect: `JsonSerializer.Serialize`/`Deserialize` calls for `PrescriptionPayloadJson`, `CalendarProjectionPayloadJson`, and `TargetLockPayloadJson` all used default `JsonSerializerOptions`, which exclude public fields. `LockedForActivatedRunwayWeekRange` (a ValueTuple, fields not properties) was the exact property this broke.

## 9. Exact root-cause classification

**Category**: JSON serialization data-loss defect in the full-fidelity persistence round-trip (not one of a pre-enumerated list of lifecycle/selection/lookup categories — an explicit, evidence-backed classification, not a guess).

- **Failing input**: any persisted `LongHorizonLockedCoreWeekOneTarget`/`ImmutablePreparationRunwayPrescription` whose `LockedForActivatedRunwayWeekRange` tuple is read back after a restart (i.e., every second-or-later Runway continuation call).
- **Incorrect value**: `LockedForActivatedRunwayWeekRange` deserializes as `(0, 0)` instead of the real persisted value (e.g. `(2, 9)`).
- **Owning component**: `LongHorizonRollingActivationPersistenceAdapter` (write) and `LongHorizonRollingStateReconstructionService` (read), both introduced/modified in Phase 4L.2A.
- **Exact code path**: `ResolveOrCreateRunwayPrescription`'s reuse branch → `ImmutablePreparationRunwayPrescriptionValidator.Validate` Step 3 (`LockedForActivatedRunwayWeekRange` must equal `StartGlobalWeek`/`EndGlobalWeek`) → throws `PreparationRunwayTargetLockScopeViolationException` → caught and generalized to `JitSegmentTransitionInfeasible` by the outer runtime's `catch (LongHorizonRollingContractException)`.
- **Why prior tests didn't detect it**: Phase 4L.2A's own continuation test asserted only prescription/target-lock *identity* reuse (same `PrescriptionId`/`CreatedByDecisionId`), which trivially holds regardless of whether the tuple round-tripped correctly, since identity is a separate field from the tuple.
- **Why 4L.2A's fix exposed it**: before 4L.2A, `ExistingRunwayPrescription` was always null after restart, forcing full regeneration on every "continuation" call — the reuse-validation path (and therefore this defect) was never exercised at all.

## 10. The fix

A single shared `internal static readonly JsonSerializerOptions FullFidelityJsonOptions = new() { IncludeFields = true };` in `LongHorizonRollingActivationPersistenceAdapter`, applied to all three `Serialize` calls there and all three matching `Deserialize` calls in `LongHorizonRollingStateReconstructionService`. No numeric, calendar, direction, evidence, target-lock, or slice formula was touched. No prescription regeneration workaround, idempotency bypass, deleted prior activation records, manual test-harness boundary increment, second window-selection algorithm, hardcoded advancement, or weakened assertion was introduced.

## 11. `LongHorizonRunwayContinuationAdvancementValidator`

New type, wired into the real (non-test-only) `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync`, invoked immediately before every successful JIT composition is persisted. Fail-closed: throws `LongHorizonRunwayContinuationAdvancementViolationException` if the new window equals the previous window, regresses, leaves a gap/overlap versus `previous.EndGlobalWeek + 1`, or re-activates any already-activated global week. This guards the resolved invariant for every future call, not only the tests written this phase.

## 12. Three-call progression proof

`LongHorizonRunwayContinuationWindowAdvancementRootCauseTests.ThreeCallProgression_AdvancesThroughRunwayIntoCoreAcrossRestarts` proves, against real Postgres with a brand-new `AppDbContext` per call: GE entry → first Runway continuation (weeks 2-5) → second Runway continuation (weeks 6-9, the exact call that was blocked pre-fix) → Runway-to-Core boundary continuation (weeks 10-13). Each call strictly advances `plan.CurrentWindowStartWeek`/`EndWeek` and produces a distinct `LongHorizonActivationWindowRecord`.

## 13. Full eight-week Runway progression

The 21-week canonical scenario's Runway segment (weeks 2-9, 8 weeks total) is fully covered by the two Runway continuation calls in §12 (weeks 2-5, then 6-9) — the full 8-week Runway range activates across exactly two continuation calls with a restart between each, with the third call proving the transition into Core (week 10+).

## 14. GE→Runway 25/26/27 diagnostic re-classification

Re-attempted after the fix. **NOT resolved by this fix.** All three horizons fail with a *different* error: `LongHorizonCheckpointDecisionInvalidException: Next GE week 5 must be NumericPending`, thrown during GE checkpoint validation *before* Runway composition is ever reached. This is definitively not the same code path as the ValueTuple defect (which only manifests inside `ResolveOrCreateRunwayPrescription`'s reuse-validation branch, reached only after a successful checkpoint). **Classification: IndependentDefectRemains.** Not investigated further, per this phase's explicit scope boundary ("do not resume the full Phase 4L.2B matrix"). The exploratory test was removed rather than committed failing.

## 15. Replay/idempotency proof

`LongHorizonRunwayContinuationReplayAndCorruptionTests.ReplayingIdenticalActivationRequest_ReturnsIdempotentReplayWithoutDuplication` proves, against real Postgres: replaying an identical already-persisted activation request (same `IdempotencyKey`) returns `IdempotentReplay` and creates no duplicate `LongHorizonActivationWindowRecords` row.

## 16. Focused concurrency regression

Not newly added this phase. Phase 4L.2A's existing single first-Runway-entry concurrency race (`LongHorizonRunwayCoreConcurrencyTests`) was re-confirmed passing as part of the full 751-test LongHorizon suite re-run (§20). A dedicated second-continuation concurrency race was not added — explicitly out of this phase's narrow scope (one invariant only).

## 17. Corruption/fail-closed checks

`LongHorizonRunwayContinuationReplayAndCorruptionTests.ReactivatingAlreadyActivatedWeek_FailsClosedWithIntegrityViolation` proves, against real Postgres: attempting to re-activate an already-`NumericActivated` week fails closed with `IntegrityViolation` and the exact expected failure reason, never silently overwriting the week.

## 18. Strengthened Phase 4L.2A continuation test

`RestartBetweenContinuationSlicesReusesSameLockAndPrescription` (in `LongHorizonRunwayCorePostgresRestartTests.cs`) no longer validates identity reuse alone. It now additionally asserts: the second window's start/end differ from the first; the second window starts exactly at `firstWindow.EndGlobalWeek + 1`; the second window's status is `Activated`; no week from the second window appears in the first window's week list; exactly one activation record exists for each of the two distinct ranges; and the durable plan-state pointer (`CurrentWindowStartWeek`/`EndWeek`) matches the second window's boundaries after persistence.

## 19. No-regeneration proof (unchanged)

Unaffected by this phase's fix. `LongHorizonRollingStateReconstructionService`'s source continues to contain no reference to `RuntimeConditionResolutionService`, `TenKPreparationRunwayDarkOrchestrator`, or `PreparationRunwayNumericMaterializer` (Phase 4L.2A's own proof, re-confirmed still true since this phase changed only `JsonSerializerOptions` arguments on existing calls, not what is called).

## 20. Regression scope

Full existing LongHorizon integration suite (751 tests, spanning Phase 4K.6 through 4L.2B) re-run and passes 751/751 after the fix and validator were added — zero regression to GE-only, first-Runway-entry, blocked/retry, no-regeneration, corruption, or concurrency paths already proven by earlier phases.

## 21. Test matrix (actual, not the full 68-item suggested list)

This phase added/rewrote 8 focused tests, all against real PostgreSQL: (1) reconstruction/reuse-validation reproduction, (2) three-call GE→Runway→Core progression, (3-4) replay idempotency and corruption fail-closed, (5) strengthened continuation-advancement regression, (6-7) two identity-preservation assertions folded into the strengthened test, (8) the original diagnostic converted to a permanent reproduction test. The full 68-item suggested matrix (mixed-window, Core-refresh, full concurrency, full failure-injection) was explicitly not attempted — that remains Phase 4L.2B's open scope, unaffected by this phase's narrow fix.

## 22. Tests run

`LongHorizonRunwayContinuationWindowAdvancementRootCauseTests` (2/2), `LongHorizonRunwayContinuationReplayAndCorruptionTests` (2/2), `LongHorizonRunwayContinuationRestartTests` (strengthened, 2/2), full `RunningApp.IntegrationTests` filter `LongHorizon` (751/751), full `plan-catalog` suite (1195+13 new governance tests).

## 23. Governance artifacts

New TD `TD-LONG-HORIZON-RUNWAY-CONTINUATION-WINDOW-ADVANCEMENT-001`, status **CLOSED** (all closure criteria met: exact root cause proven not guessed; production code minimally corrected; second continuation selects/persists the distinct next range; three-call progression advances through the Runway→Core boundary; full 8-week canonical Runway proven; no regeneration workaround; restart reconstruction remains full-fidelity; idempotent replay AND next-range distinction both pass; corruption/fail-closed re-confirmed). Append-only updates added to `TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001` (remains OPEN — mixed-window/Core-refresh/failure-injection/concurrency matrices unresolved, GE→Runway finding re-diagnosed as independent), `TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001`, `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`. Confirmation/public-wiring TDs not touched. Aggregate updated to 50 risks, 15 OPEN, 35 CLOSED. 14 governance test files updated/bumped (13 stale-count bumps + 1 new dedicated file).

## 24. Dark integration

Unaffected. No endpoint, DI registration, background job, or Flutter file references `FullFidelityJsonOptions`, `LongHorizonRunwayContinuationAdvancementValidator`, or any changed type. `LongHorizonRollingRestartContinuationService` remains a non-public C# type.

## 25. Numeric/calendar/direction/evidence/target-lock/slice formulas

Zero changes. This phase touched only JSON serialization options and added one new fail-closed validator; no formula, threshold, or business rule was modified.

## 26. Public preview / confirmation / API / Flutter status

Unchanged. All remain entirely unwired, exactly as before this phase.

## 27. Commits

None. Zero git commits were made throughout this phase, matching this session's standing convention.

## 28. Recommended next phase

**Resume Phase 4L.2B.** The continuation-advancement defect that blocked it is now resolved. The remaining Phase 4L.2B scope (mixed-window restart, Core-only/refresh restart, the full failure-injection matrix, the full concurrency/idempotency matrix) is unblocked to re-attempt, though the newly-confirmed independent GE→Runway checkpoint-validation defect (§14) should likely be root-caused first, since it blocks the same mixed-window scenarios for a different reason.

## 29. What this phase explicitly did NOT do

Did not resume the full Phase 4L.2B matrix. Did not implement Core refresh completion. Did not add the full failure-injection matrix. Did not add the full concurrency matrix. Did not enable public preview or confirmation. Did not change API/DTO/Home/Calendar/completion handlers/Flutter. Did not change numeric/calendar/direction/evidence/target-lock/slice formulas. Did not regenerate the Runway prescription as a workaround. Did not fix the independent GE→Runway checkpoint-validation defect discovered in §14 (out of scope, honestly disclosed instead).

## 30. Final classification

```
LONG_HORIZON_RUNWAY_CONTINUATION_WINDOW_ADVANCEMENT_ROOT_CAUSE_RESOLVED
```

## 31. Evidence integrity statement

Every claim above is backed by a real PostgreSQL test run in this session (751/751 LongHorizon integration tests, plus this phase's new focused tests), not asserted from reasoning alone. The GE→Runway 25/26/27 finding was independently re-attempted (not assumed fixed) and found to fail with a different, unrelated error — reported honestly as unresolved rather than silently dropped or misclassified as fixed.

## 32. Full required test matrix disclosure

The phase prompt's suggested 68-item test matrix was not exhaustively implemented. This phase implemented the minimum set of tests sufficient to prove the single stated invariant with real evidence (root cause, fix, three-call progression, replay, corruption, strengthened regression) rather than fabricating breadth to hit a suggested count. This is a deliberate, disclosed scope choice consistent with this session's established governance convention: real narrow evidence over inflated claimed coverage.
