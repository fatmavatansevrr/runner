# Phase 4L.2B-R — Resumed Mixed-Window, Core Refresh, Failure-Injection and Concurrency Completion Matrix

## 1. Executive result

With both blocking root causes resolved (Phase 4L.2C's Runway continuation ValueTuple defect; Phase 4L.2D's GE→Runway test-harness RaceDate defect), this phase resumed the Phase 4L.2B matrix. Real, PostgreSQL-proven progress was made on capability groups 1 (Runway→Core mixed restart — fully closed for all three naturally-reachable shapes) and 2 (Core-only restart — representatively closed). Capability groups 3 (future-only Core refresh) and 4 (failure-injection rollback matrix) were not attempted — each requires substantial new capability/infrastructure (a Core-refresh production entry point; a test-only failpoint seam) beyond this phase's completed budget. Group 5 (concurrency/idempotency) was extended by exactly one focused test, not the full matrix. Per the phase's own explicit instruction, the TD is **not** narrowed to declare success — it remains OPEN.

```
LONG_HORIZON_POSTGRESQL_COMPLETION_MATRIX_REMAINS_BLOCKED_BY_EXPLICIT_MIXED_RESTART_CORE_REFRESH_ROLLBACK_CONCURRENCY_IDEMPOTENCY_OR_CONSTRAINT_GAP
```

## 2. Original Phase 4L.2B blocked result

Discovered a real Runway continuation window-advancement defect; none of the four originally-scoped capability groups closed.

## 3. Blockers resolved by Phase 4L.2C

Root-caused and fixed the ValueTuple JSON round-trip defect (`IncludeFields=true`), proving Runway continuation advances through distinct contiguous windows after real PostgreSQL restarts.

## 4. Blockers resolved by Phase 4L.2D

Proved the reported GE→Runway production defect did not exist; the actual issue was a test fixture hardcoding RaceDate to 21 weeks. Fixed with a one-line test-infrastructure change; horizons 25/26/27 now correctly persist and restart through GE remainder + Runway → Runway continuation.

## 5. Resumed scope and exclusions

Attempted: reachable Runway→Core shape discovery and restart matrix; aligned Runway-only→Core-only restart; a representative Core-only restart/terminal-completion sequence; one focused Runway→Core concurrency race. Not attempted (explicit honest exclusion, not a scope-narrowing to declare success): future-only Core refresh (no production capability exists to test); the test-only failure-injection seam and every rollback matrix depending on it (Parts 10-16); the remaining concurrency scenarios (Core-only, Core-refresh, block, retry, activation-vs-retry, checkpoint-vs-activation — Parts 18-23); the idempotency-key audit and near-collision matrix (Part 24); the PostgreSQL constraint review and JSONB tamper matrix beyond existing coverage (Parts 26-27, 30).

## 6. PostgreSQL authority

Every new test in this phase reuses the exact real-Postgres pattern established since Phase 4L.2A: `LongHorizonPersistenceTestFixture.NewContext()` (fresh `UseNpgsql` `AppDbContext`) per restart boundary, the real production repository/checkpoint runtime/composition orchestrator/persistence adapters. Zero EF InMemory, zero mocked repositories, zero fabricated snapshots.

## 7. Pre-flight regression gate

`LongHorizonRunwayContinuationWindowAdvancementRootCauseTests` (2), `LongHorizonGeRunwayPartialBoundaryHandoffTests` (3), `CheckpointRuntime_AcceptsPartialTerminalGeWindowInIsolation` (1) — all 6 re-run and pass before any new matrix test was added, confirming the inherited proofs (ValueTuple round-trip, three-call Runway progression, horizons 25/26/27) all hold.

## 8. Reachable Runway→Core boundary discovery

Derived analytically from the real structural formula (GE = TotalWeeks−20, Runway = 8 weeks, Core = 12 weeks) and the real greedy 4-week window selector, reusing the already-proven `[1-4]→[5-8]→[9-12]` sequence from Phase 4L.2D:

- **Horizon 25** (GE=5, Runway=6-13): after `[9-12]`, Runway remainder is week 13 only → next window `[13-16]` = **1 Runway + 3 Core**.
- **Horizon 26** (GE=6, Runway=7-14): after `[9-12]`, Runway remainder is 13-14 → next window `[13-16]` = **2 Runway + 2 Core**.
- **Horizon 27** (GE=7, Runway=8-15): after `[9-12]`, Runway remainder is 13-15 → next window `[13-16]` = **3 Runway + 1 Core**.
- **Horizon 21** (GE=1, Runway=2-9): `[2-5]→[6-9]` lands exactly on the Runway end — the naturally **aligned** case, no mixing.

All four predictions were confirmed empirically on the first real-Postgres test run, with zero fabricated lifecycle rows.

## 9. Runway→Core mixed restart matrix

`LongHorizonRunwayCoreMixedWindow_ReachesNaturalShapeAndPersistsAcrossRestart` (Theory, 3 cases) proves, against real Postgres with a restart immediately before and after the mixed boundary call: exact global ranges `[13,16]`; correct Runway/Core week counts per shape; no Runway regeneration (same `PrescriptionId` before/after); historical Runway weeks' volume and calendar dates unchanged; exactly one `LongHorizonActivationWindowRecord` owning the full `[13,16]` range; future Core weeks beyond the window remain Pending with no sessions; exact next-boundary pointer (`plan.CurrentWindowStartWeek/EndWeek`).

## 10. Aligned Runway-only→Core-only restart

`AlignedRunwayOnlyToCoreOnly_HasNoMixedOwnershipAndCoreBeginsCorrectly` proves: the Core-only window `[10,13]` contains no Runway weeks (`SegmentsCovered` is `Core` only); target lock/prescription remain historical and unchanged; no calendar overlap (first Core session date strictly after the last Runway session's end date).

## 11. Core-only restart matrix

**Representative, not exhaustive.** `CoreOnlyContinuation_RestartsThroughToFinalOneWeekTerminalCompletion` (horizon 25) proves, with a restart between every call: post-mixed-boundary Core-only continuation `[17,20]`; mid-Core restart and continuation `[21,24]`; restart immediately before a natural final 1-week terminal Core window `[25,25]`; full terminal completion (all 25 weeks `NumericActivated`, zero Pending/Blocked); no duplicate activation records; idempotent repeated reconstruction. The full 7-point matrix Part 6 requests (separate coverage of final-2-week and final-3-week terminal windows as independent horizons, immediately-after-first-Core-activation as its own restart point) was not exhaustively covered.

## 12. Future-only Core refresh

**Not attempted.** No dedicated Core-refresh (V1→V2 context supersession) production entry point exists in the codebase. Building one plus its full restart/negative-case/concurrency matrix (Parts 7, 19) was outside this phase's completed budget. Honestly left OPEN rather than fabricated or narrowed away.

## 13. Core-context ownership integrity

Not separately extended this phase. The existing per-plan Core-context identity fix (Phase 4L.2A) continues to hold, re-confirmed via the mixed-window tests' single-new-context-per-race assertion (§16).

## 14. Existing transaction ownership

Not separately re-audited this phase (Part 9's full review was not performed). No transaction-boundary defect was found or fixed; existing Phase 4L.2/4L.2A/4L.2C transaction behavior (one `SaveChangesAsync` per authoritative operation) is unchanged.

## 15. Failure-injection seam

**Not implemented.** No test-only failpoint/interceptor (Part 10) was added to the repository.

## 16. Runway→Core rollback matrix

**Not implemented** — depends on the failure-injection seam (§15), which was not built this phase.

## 17. Core-only rollback matrix

**Not implemented**, for the same reason.

## 18. Core-refresh rollback matrix

**Not implemented** — depends on both the failure-injection seam and the Core-refresh capability itself (§12), neither of which exist yet.

## 19. Block rollback matrix

**Not implemented** this phase.

## 20. Retry rollback matrix

**Not implemented** this phase.

## 21. Commit-acknowledgement ambiguity

**Not extended** beyond Phase 4L.2/4L.2A's own pre-existing block/retry idempotency tests (re-confirmed still passing via the full regression run).

## 22. Concurrent mixed activation

`ConcurrentMixedRunwayToCoreActivationHasExactlyOneWinner` — one new focused test, real Postgres, reusing Phase 4L.2A's deterministic winner/loser race pattern (two independently-loaded snapshots at the same xmin/version; winner commits via the real production path; loser reuses the stale version against the block adapter and receives `ConcurrencyConflict`). Proves: exactly one activation record for the mixed range; exactly one new Core-context row created by the race (not a duplicate); loser reloads winner state on reconstruction.

## 23. Concurrent Core-only activation

**Not implemented** this phase.

## 24. Concurrent Core refresh

**Not implemented** — depends on the Core-refresh capability (§12), which does not exist yet.

## 25. Concurrent block

**Not implemented** this phase.

## 26. Concurrent retry

**Not implemented** this phase.

## 27. Activation/retry race

**Not implemented** this phase.

## 28. Checkpoint/refresh activation race

**Not implemented** this phase.

## 29. Idempotency-key audit

**Not performed** as a dedicated deliverable this phase. The existing deterministic `IdempotencyKey` mechanism (proven in Phase 4L.2C) continues to govern the activation-persistence path used by every test this phase added, and was exercised incidentally (no duplicate rows observed in any test) but not separately audited for near-collision properties.

## 30. Activation-lookup precision

Not separately reviewed this phase. Existing exact-range lookup semantics (`IdempotencyKey`-based, proven in Phase 4L.2C) are unchanged.

## 31. PostgreSQL constraint review

Not extended this phase; no schema changes were made or found necessary.

## 32. JSONB and ValueTuple regression

Re-confirmed passing as part of the pre-flight gate (§7) and every new test's own assertions (prescription/target-lock identity unchanged across restarts, non-zero `LockedForActivatedRunwayWeekRange` implicitly proven by every successful Runway reuse in the mixed-window tests).

## 33. RaceDate fixture regression

Re-confirmed: every new test this phase uses `LongHorizonRunwayCoreRestartFixture.AdvanceOneWindowAsync`, which (since Phase 4L.2D) derives `RaceDate` from `state.StructuralRoadmap.TotalWeeks`. No hardcoded 21-week RaceDate remains in this fixture. A dedicated search for equivalent hardcoded assumptions in other fixtures was not performed this phase (Part 28's "search all relevant test fixtures" was not exhaustively executed).

## 34. Reconstruction without regeneration

Confirmed via the mixed-window tests: `PrescriptionId` is identical before and after the mixed-boundary call, proving no Runway regeneration occurred. Dedicated invocation-count instrumentation for the condition resolver/Core generator/Runway materializer/calendar composers (Part 29's exact literal requirement) was not added this phase; the existing Phase 4L.2A no-regeneration proof (grepped source, no reference to regeneration authorities in the reconstruction service) remains the current evidence and is unaffected by this phase's changes (no reconstruction-service code was touched).

## 35. Corruption completion matrix

**Not extended** this phase beyond Phase 4L.2A's existing representative subset.

## 36. Final complete lifecycle restart

Achieved incidentally as part of §11: the horizon-25 Core-only test drives the full plan from initialization through terminal completion (week 25, the plan's final week) with restarts between every call, and confirms zero Pending/Blocked weeks remain and reconstruction is idempotent. This is real evidence for Part 31's invariant, though narrower than the phase's suggested "invoke restart continuation once" post-completion check.

## 37. Dark integration

Unaffected. No endpoint, DI registration, background job, confirmation, public-preview, Home/Calendar, completion-handler, API, or Flutter file was touched. No production code was modified this phase at all (all changes are new test files).

## 38. Governance updates

`TD-LONG-HORIZON-MIXED-CORE-REFRESH-POSTGRESQL-COMPLETION-MATRIX-001` updated append-only with `phase4L2BResumedUpdate`, remains **OPEN** (per the phase's own explicit instruction: closeable only if all five capability groups fully complete, which they do not). Seven other TDs updated append-only: `TD-LONG-HORIZON-RUNWAY-CONTINUATION-WINDOW-ADVANCEMENT-001`, `TD-LONG-HORIZON-GE-RUNWAY-PARTIAL-BOUNDARY-HANDOFF-001`, `TD-LONG-HORIZON-RUNWAY-CORE-POSTGRESQL-RESTART-RECOVERY-MATRIX-001`, `TD-LONG-HORIZON-ROLLING-PERSISTENCE-RESTART-SAFETY-001`, `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001`, `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001`, `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (unaffected — no formula changed). No second replacement TD was created. Confirmation/public-wiring TDs untouched.

## 39. Tests

6 new focused tests, all against real PostgreSQL, 0 failing: 3 mixed-shape restart cases (Theory), 1 aligned Runway-only→Core-only, 1 Core-only-through-terminal-completion, 1 focused mixed-boundary concurrency race. Full LongHorizon suite: 763/763 (up from 757, zero regression). Full backend suite and plan-catalog suite re-confirmed passing (see final report for exact counts).

## 40. Public/confirmation/API/Flutter status

Unchanged. All remain entirely unwired.

## 41. Final classification

```
LONG_HORIZON_POSTGRESQL_COMPLETION_MATRIX_REMAINS_BLOCKED_BY_EXPLICIT_MIXED_RESTART_CORE_REFRESH_ROLLBACK_CONCURRENCY_IDEMPOTENCY_OR_CONSTRAINT_GAP
```

Specifically: capability groups 1 (Runway→Core mixed restart) and, representatively, 2 (Core-only restart) are closed with real evidence. Groups 3 (future-only Core refresh) and 4 (failure-injection rollback matrix) are entirely unattempted. Group 5 (concurrency/idempotency) is extended by one test, far short of the full matrix. The TD is **not** narrowed to declare success, per the phase's own explicit instruction.

## 42. Exact next phase

**Not Phase 4L.3.** Recommend a further-focused resume phase (or phases) targeting specifically: (1) a minimal future-only Core-refresh production capability plus its restart proof; (2) the test-only failure-injection seam (Part 10) plus at minimum the Runway→Core and Core-only rollback matrices; (3) the remaining concurrency scenarios (Core-only, block, retry, activation-vs-retry). These three remain the highest-value next steps before Phase 4L.3 (Long-Horizon Confirmation and Public Preview Wiring) should be considered, since the underlying persistence/restart matrix this session's governance convention requires is still genuinely incomplete.
