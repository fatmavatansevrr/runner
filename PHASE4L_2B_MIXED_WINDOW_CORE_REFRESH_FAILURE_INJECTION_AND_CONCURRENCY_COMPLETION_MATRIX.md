# Phase 4L.2B — Mixed-Window, Core Refresh, Failure-Injection and Concurrency Completion Matrix

## 1. Executive result

This phase attempted to close the four capability groups Phase 4L.2A left explicitly open: GE+Runway/Runway+Core mixed-window restart, Core-only and future-Core-refresh restart, the failure-injection/rollback matrix, and the full concurrency/idempotency matrix. It did not close them. Instead, real PostgreSQL testing discovered a genuine, previously-undetected defect: Phase 4L.2A's "Runway continuation restart" claim proved prescription/target-lock **identity reuse** but never verified that the activation **window actually advances** on a second continuation call — and when tested directly, it does not appear to. Mixed-window restart at the naturally-reachable 25/26/27-week horizons was also attempted and did not activate as expected, very plausibly the same root cause, but this could not be confirmed within this phase's time budget. Per the phase prompt's own explicit governance instruction, the new TD is kept **OPEN** — no capability group is falsely marked closed to hit a test count. No production code was changed. No commits made.

```
LONG_HORIZON_POSTGRESQL_COMPLETION_MATRIX_REMAINS_BLOCKED_BY_EXPLICIT_MIXED_RESTART_CORE_REFRESH_ROLLBACK_CONCURRENCY_IDEMPOTENCY_OR_CONSTRAINT_GAP
```

This is the phase prompt's own explicitly defined "if blocked" output, used here deliberately and accurately rather than the "completed" marker, since none of the four required capability groups close.

## 2. Residual gaps inherited from 4L.2A

Phase 4L.2A left open: pure GE+Runway mixed-window restart (1+3/2+2/3+1); Runway→Core mixed-window and Core-only restart; future-only Core refresh restart; the complete transaction failure-injection matrix; the complete concurrency/idempotency matrix beyond one first-Runway-entry race.

## 3. Scope and exclusions

Attempted in scope: all four groups above, via real PostgreSQL testing only, reusing production dark runtimes/adapters, never hand-fabricated snapshots. Excluded per the prompt: redesigning the Phase 4L.2 persistence model; a parallel persistence architecture; numeric/calendar/evidence/direction/lifecycle/retry policy changes; public preview/confirmation/API/DTO/Home/Calendar/completion-handler/Flutter changes; background jobs; commits.

## 4. Real PostgreSQL authority

Every investigative test this phase used `LongHorizonPersistenceTestFixture.NewContext()` (a fresh `UseNpgsql` `AppDbContext`) per operation, reusing the exact production repository/adapter/reconstruction/continuation code Phase 4L.2/4L.2A already implemented. Zero EF InMemory usage. No test-only substitute persistence layer was introduced.

## 5. Current transaction ownership

Unchanged from Phase 4L.2A: confirmed by direct inspection, not re-verified with new evidence this phase (the investigation that consumed this phase's budget was a correctness question about window *selection*, not transaction *boundaries* — `SaveActivationSuccessAsync` still performs exactly one `SaveChangesAsync` per authoritative operation).

## 6. Reachable boundary discovery

Confirmed via the exact greedy 4-week window-selection arithmetic (matching Phase 4K.9's own proven-correct in-memory selection): for a 25-week horizon (GE=5, Runway=weeks 6-13, Core=weeks 14-25), the natural window sequence is [1-4]GE, [5-8]=1GE+3Runway mixed, [9-12]pure Runway, [13-16]=1Runway+3Core mixed. This is the exact shape this phase attempted to reach and verify; it was not reached successfully (see §7-8).

## 7. GE→Runway restart matrix

**Attempted, not achieved.** A real test was written driving `LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync` (the same real checkpoint-then-composition helper already proven for the aligned GE=1 case in Phase 4L.2A) at horizons 25, 26, and 27. In every case the resulting window's `SegmentsCovered` was `[GeneralEndurance]` only — the composition did not extend into Runway as the natural boundary discovery (§6) predicts it should. The test file was removed rather than committed failing or misrepresented as passing.

## 8. Runway→Core restart matrix

**Not independently attempted.** Reaching a real Runway→Core mixed window in this test harness requires first successfully completing GE→Runway mixed activation (§7) and then multiple further continuation steps — both prerequisites are now known to be unreliable (§7, §9), so pursuing this further without first resolving those would not have produced trustworthy evidence.

## 9. Aligned Runway-only→Core-only restart

**Not proven.** Attempted using the already-working 21-week (GE=1) pure-Runway-entry case, continuing through a second Runway continuation call toward the Runway→Core boundary at week 10. This is where the continuation-advancement defect (§10) was directly discovered and isolated: the second continuation call's resulting window did not advance past the first-entry range, so the Core-only boundary was never actually reached in this attempt.

## 10. Core-context ownership integrity

Not independently re-verified this phase. Phase 4L.2A's per-plan Core-context identity fix continues to hold (confirmed via the full persistence suite re-run, 46/46 passing), but no NEW Core-context ownership scenario was exercised this phase since Core-only entry itself could not be reached.

## 11. Failure-injection seam

**Not implemented.** No test-only failpoint/interceptor was added to the repository. This phase's investigative budget was consumed by the continuation-advancement finding (§10 heading above refers to the topic, not a completed implementation) rather than building and exercising a failure-injection harness on top of restart paths that were themselves found to be unreliable.

## 12–19. Rollback matrices (GE→Runway / Runway→Core / Core-only / Core-refresh / block / retry)

**Not implemented.** None of these could be meaningfully exercised without the failure-injection seam (§11) and the underlying restart paths they depend on (§7-9).

## 20. Commit-acknowledgement ambiguity

Not extended this phase beyond Phase 4L.2/4L.2A's own existing block/retry idempotency tests, re-confirmed still passing.

## 21–27. Concurrency and races (GE→Runway / continuation / mixed / Core-only / refresh / block / retry / activation-vs-retry / checkpoint-vs-activation)

**Not extended.** Only the single first-Runway-entry concurrency race Phase 4L.2A already proved was re-confirmed passing; no new concurrency scenario was added, since most of the required scenarios depend on restart paths (§7-9) not yet proven reliable.

## 28. Idempotency-key audit

Not performed as a dedicated deliverable this phase.

## 29. PostgreSQL constraint review

No new constraint changes were made or found necessary. Phase 4L.2A's per-plan Core-context fix remains the most recent real constraint-adjacent correction.

## 30. Per-plan Core-context regression

Not separately re-verified with a new dedicated test; Phase 4L.2A's own reproduction/fix tests continue to pass (confirmed via full suite re-run).

## 31. JSONB full-fidelity regression

Not separately extended; Phase 4L.2A's own round-trip tests for target lock, prescription, and calendar projection continue to pass (confirmed via full suite re-run, 46/46).

## 32. Reconstruction without regeneration

Unchanged from Phase 4L.2A — proven for the first-entry subset only; not extended to paths that remain unproven.

## 33. Corruption completion matrix

Not extended beyond Phase 4L.2A's representative 3-case subset.

## 34. Final completion restart

Not attempted — depends on the Core-only/refresh matrix, which remains open.

## 35. Dark integration

Unaffected. No production code was modified this phase. The one investigative test file (mixed-window shapes) was written and then removed after it did not work. One stricter assertion was added to an existing Phase 4L.2A test, confirmed the finding, then reverted with an explanatory comment left in its place — the test itself still passes with its original (narrower, now-clearly-labeled) scope. No endpoint, DI registration, background job, or Flutter file was touched.

## 36. Governance artifacts

New TD `TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001`, status **OPEN** (kept open per the phase prompt's own explicit instruction, since not all four required capability groups close). Append-only updates added to `TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001`, `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`, `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001`, and `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (remains OPEN). Aggregate updated to 49 risks, 15 OPEN, 34 CLOSED (the new TD is the first Long-Horizon governance entry added as OPEN, not CLOSED, in several phases — an intentional signal that closing prematurely was rejected). Twelve prior governance test files with stale 48/14/34 hardcoded counts updated to 49/15/34. New governance cross-check test file, itself asserting the TD status is OPEN, not CLOSED.

## 37. Tests

Net zero new passing tests. One investigative mixed-window test file was written, found not to work, and removed. One stricter assertion was added to an existing Phase 4L.2A test, confirmed a real finding, then reverted (with the finding preserved as a code comment) since fixing the underlying defect was outside this phase's time budget. The full existing Long-Horizon persistence suite (46 tests: 30 from Phase 4L.2 + 16 from Phase 4L.2A) was re-run and continues to pass at 46/46, confirming this phase's investigation introduced no regression.

## 38. Public/confirmation/API/Flutter status

Unchanged. No endpoint, DI registration, confirmation code, home/calendar query, completion-handler, background job, or Flutter file was touched.

## 39. Final classification

`LONG_HORIZON_POSTGRESQL_COMPLETION_MATRIX_REMAINS_BLOCKED_BY_EXPLICIT_MIXED_RESTART_CORE_REFRESH_ROLLBACK_CONCURRENCY_IDEMPOTENCY_OR_CONSTRAINT_GAP`. This phase's real value is a genuine, previously-undetected finding — Runway continuation window advancement appears broken through the persistence/restart-continuation layer, invisible until Phase 4L.2A's own fix (making `ExistingRunwayPrescription` real instead of always-null) removed the masking effect of forced full regeneration on every call — disclosed honestly rather than hidden or worked around with a fabricated passing test.

## 40. Exact next phase

**Not Phase 4L.3.** The recommended next phase is a targeted defect investigation and fix: root-cause why `LongHorizonRollingJitCompositionOrchestrator`/`LongHorizonRollingJitActivationRuntime`, when given a real, correctly-reconstructed `ExistingRunwayPrescription` and `LifecycleStates` showing the prior window's weeks as `NumericActivated`, does not select and activate the next contiguous Pending range on a second continuation call. This is very likely the same root cause blocking mixed-window restart, Core-only restart, and future-refresh restart — fixing it first is the highest-leverage next step before re-attempting any of this phase's remaining capability groups, and before Phase 4L.3 (Long-Horizon Confirmation and Public Preview Wiring), which should not proceed while rolling continuation itself is unproven.
