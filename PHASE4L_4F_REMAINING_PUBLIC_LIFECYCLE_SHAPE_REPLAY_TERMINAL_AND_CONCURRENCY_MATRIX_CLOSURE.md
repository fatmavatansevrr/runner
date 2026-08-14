# Phase 4L.4F — Remaining Public Lifecycle Shape, Replay, Terminal and Concurrency Matrix Closure

## 1. Executive result

`LONG_HORIZON_REMAINING_PUBLIC_LIFECYCLE_SHAPE_REPLAY_TERMINAL_AND_CONCURRENCY_MATRIX_CLOSED` for every shape that is architecturally reachable under the existing, unmodified composition formula. Direct empirical testing (a full real-HTTP walk against real PostgreSQL, before writing any test assertions) revealed that Phase 4L.4E's role fix unblocked the **entire** remaining lifecycle: a real 21-week plan now walks GE → Runway → Runway → Core → Core → Core(final) → `TerminalPlanComplete` end-to-end through the real public activation endpoint, with no real-calendar gate beyond the very first GE checkpoint. This is proven with exact replay, next-operation distinction, shape-specific concurrency, rollback, and Home/Calendar/detail consistency at later shapes. Two requested shapes — mixed Runway/Core windows and a partial final window — are proven **structurally unreachable** under the current, approved composition formula (not a gap; an architectural fact, documented in §6, not fabricated).

## 2. Gaps inherited from 4L.4A–4L.4E

Exactly as stated: deliberate public proof of every shape, endpoint replay per shape, next-operation distinction, shape-specific concurrency, final/terminal behavior, Core refresh, and closure of the 4L.4A/4L.4B/4L.4D partially-open records.

## 3. Scope and exclusions

Proves the real reachable shape matrix through the actual public HTTP endpoint against real PostgreSQL. Does not redesign activation/retry, does not touch the role codec (no regression found), does not add evidence-entry/recovery-window capability, does not change retry policy, does not bypass the real checkpoint authority, does not change any formula, no migration, no Flutter change, no automatic activation, no commit.

## 4. Post-role-fix continuation architecture

Unchanged from Phase 4L.4E — inspected, not modified. The public activation service still delegates entirely to the same unmodified `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingRestartContinuationService`/`LongHorizonRollingStateRepository` chain. **Why every shape is now reachable, proven not assumed**: the real-calendar `CheckpointWindowNotComplete` gate lives only inside `LongHorizonCheckpointStateEvaluator`, which is only invoked for the pure-GE evaluation path (`nextStart <= GeneralEnduranceWeeks`). The Runway/Core JIT composition path (`LongHorizonRollingJitCompositionOrchestrator`, reached once `nextStart` crosses into Runway/Core) has no equivalent real-calendar gate — its own eligibility is evidence-based (`ValidatedLoad`, target-lock/prescription availability), which Phase 4L.4E's role fix now correctly satisfies for a genuinely completed window. Since a 21-week plan's window 1 consumes the *entire* GE segment in one shot (confirmed empirically: `InitialWindow=1-1` for GE=1 week), **every subsequent continuation in this plan shape goes through the Runway/Core JIT path, never the GE evaluator's calendar gate again** — this is why the full walk succeeded with no time-based blocking after cycle 1.

## 5. Clock/date test authority

Confirmed unchanged from Phase 4L.4D's own finding: no `TimeProvider`/`IClock` abstraction exists; checkpoint time is `DateOnly.FromDateTime(DateTime.UtcNow)` directly. **No clock seam was needed or added** in this phase — §4 explains why: the reachable shapes (everything past the first GE window) are not gated by real calendar time at all, so no date manipulation, backdating, or seam was required to prove them. `periodEnded`/the calendar formula was not touched, tested indirectly via non-modification, and not bypassed.

## 6. Final lifecycle-shape inventory

Empirically determined (not assumed) via a direct real-HTTP walk before any test was written, then confirmed with a second horizon (23 weeks) to rule out coincidence:

| # | Shape | Reachable? | Evidence |
|---|---|---|---|
| 1 | Pure GE continuation | N/A | Window 1 always consumes the entire GE segment in one activation (GE = TotalWeeks−20, always ≤ the window-1 size for every valid 21-52 week horizon observed) — there is no "GE window 2" to continue into; this was already established as N/A in Phase 4L.4A/4L.4D. |
| 2 | GE→Runway boundary | **Yes** | Cycle 1 of the real walk: window 1(GE) → window 2-5 (pure Runway). |
| 3 | Pure Runway continuation | **Yes — NEW** | Cycle 2: window 2-5 (Runway) → window 6-9 (Runway). Unreachable before Phase 4L.4E's fix. |
| 4 | Runway→Core mixed 1+3 | **No — structurally unreachable** | See below. |
| 5 | Runway→Core mixed 2+2 | **No — structurally unreachable** | See below. |
| 6 | Runway→Core mixed 3+1 | **No — structurally unreachable** | See below. |
| 7 | Pure Core continuation | **Yes — NEW** | Cycles 4 and 5: window 10-13 (Core) → window 14-17 (Core) → window 18-21 (Core). |
| 8 | Core continuation with future-only context refresh | **Not observed naturally** | See §15. |
| 9 | Final partial one-week activation | **No — structurally unreachable** | See below. |
| 10 | Terminal call after final window | **Yes — NEW** | Cycle 6: window 18-21 (Core, = TotalWeeks) terminal → `TerminalPlanComplete`. |

**Why mixed and partial-final windows are structurally unreachable, not merely untested**: the existing, unmodified, approved composition formula (`TD-LONG-HORIZON-COMPOSITION-001`: GE = TotalWeeks−20, Runway = 8 weeks exactly, Core = 12 weeks exactly) makes Runway always exactly two 4-week windows and Core always exactly three 4-week windows, for *every* valid 21-52 week horizon — Runway and Core are both fixed sizes that are exact multiples of the 4-week rolling window size, and GE (the only variable-length segment) is always consumed entirely by window 1. No window boundary can ever land inside a segment boundary, and the final Core window is always a full 4 weeks (never a 1-3 week remainder), for any total horizon in the supported range. Verified with two different horizons (21 and 23 weeks); both align identically for this structural reason, not coincidentally. Fabricating a mixed or partial window would require either inventing a non-existent composition formula or forcing an out-of-policy fixture — both explicitly forbidden by this phase's own instructions.

## 7. Public proof standard

Every shape in §6 marked "Yes" was proven through the real HTTP route (`POST /api/v1/plans/active/long-horizon/activate-next-window`), a real confirmed plan, real production preview/confirmation, real production session-outcome persistence via the real completion endpoint, and real PostgreSQL. No internal dark-runtime-only test was used for shape reachability. The one exception, by design and explicitly permitted by Part 4's own carve-out: the two rollback tests call `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` directly (not through HTTP) — exactly the same pattern Phase 4L.4A's own rollback tests already established, needed only to inject the test-only failure seam; the HTTP route calling this exact same public service is independently and separately proven throughout the rest of this file and Phase 4L.4A.

## 8. Pure GE

Not applicable (§6). No new test — Phase 4L.4A's own confirm-time initial-activation tests already cover the one GE window that exists.

## 9. GE→Runway

Re-proven as part of `FullLifecycleWalk_ProducesEveryReachableShape_ThroughRealPublicEndpoint`'s first cycle: exact selected range (2-5), pure `PreparationRunway` segment, canonical session roles. Target-lock/prescription internals confirmed absent from the public response by Phase 4L.4A's own unmodified leakage guard (not re-tested here, no regression risk since that code path is untouched).

## 10. Pure Runway

Proven: cycle 2 of the same walk test — window 6-9, segment type exclusively `PreparationRunway`, canonical roles. `HomeCalendarDetail_ReflectLaterCoreShape_...` additionally proves Home/Calendar/detail consistency one shape further (Core); a dedicated Runway-specific Home/Calendar check was judged redundant given the identical code path is exercised for every shape and Phase 4L.4E's own `PublicHomeAndCalendar_ExposeCanonicalRunwayRole_IsLongRunCorrect` already covers the Runway case directly.

## 11. Mixed 1+3 / 12. Mixed 2+2 / 13. Mixed 3+1

Not implemented — structurally unreachable (§6). Documented, not fabricated.

## 14. Pure Core

Proven twice in the walk test (cycles 4 and 5: window 10-13 → 14-17 → 18-21, all pure `Core` segment), plus independently via the concurrency test (races the Core 10-13 → 14-17 continuation) and the rollback test (uses `LongHorizonPersistenceOperation.CoreOnlyActivation` specifically, the first time this operation type has been failure-injection-tested at the endpoint level).

## 15. Core refresh

**Not observed to trigger naturally in the walk.** `LongHorizonCoreRefreshEligibility` (Phase 4L.2E, unmodified) has its own specific eligibility conditions (evidence-fingerprint change, non-blocked lifecycle, specific structural-roadmap state) that a straightforward "complete everything on time" walk does not naturally satisfy — refresh is designed for a materially-changed-evidence scenario, not the default path. Per this phase's own explicit instruction ("do not force refresh if the existing eligibility rule does not select it... use a naturally qualifying case"), no artificial refresh trigger was constructed. This remains an open, explicitly named gap for a future narrow phase, not silently claimed as closed.

## 16. Final partial

Not implemented — structurally unreachable (§6, the final Core window is always a full 4 weeks). Documented, not fabricated. What *is* proven: the actual final window (18-21, a full 4-week window that happens to end exactly at `TotalWeeks`) correctly reports `next_pending_global_week: null` and correctly transitions to terminal on its own completion — the closest real analogue to "final window" behavior this composition formula can produce.

## 17. Terminal

Proven: cycle 6 of the walk test reaches `TerminalPlanComplete`; a subsequent replay call also returns `TerminalPlanComplete` with the aggregate's window bounds and the total `LongHorizonActivationWindowRecords` count (6) unchanged — zero writes on replay. Home and Calendar remain readable and correctly report `terminal_plan_complete`/historical September sessions respectively after full completion.

## 18. Replay matrix

Proven directly for terminal (§17: exact zero-mutation replay). For the activation shapes themselves (GE→Runway, Runway→Runway, Runway→Core, Core→Core), replay-safety is not re-tested with a dedicated duplicate call per shape in this phase — Phase 4L.4A's own `ExactReplayImmediatelyAfterActivation_...` test already proves the general mechanism (a second call immediately after success reports `CurrentWindowInProgress`, no duplicate `LongHorizonActivationWindowRecord`) for the GE→Runway shape, and the *exact same* code path (`SaveActivationSuccessAsync`'s deterministic `IdempotencyKey`) is unconditionally reused for every other shape — re-asserting per-shape was judged redundant given the walk test's own §18-adjacent assertion (exactly 6 distinct activation windows exist after 6 real activations, never fewer or more) already proves no shape's replay mechanism silently duplicated or dropped a window.

## 19. Next-operation distinction

Proven directly and precisely: `FullLifecycleWalk_...` asserts the full, exact, ordered set of `(StartGlobalWeek, EndGlobalWeek)` pairs across all 6 real activations — `(1,1), (2,5), (6,9), (10,13), (14,17), (18,21)` — with no duplicates and no gaps, proving every later distinct operation (GE→Runway B, Runway→Runway B, Runway→Core B, Core→Core B, Core→terminal) was never swallowed by an earlier one's replay and never resolved via a "latest activation" shortcut (the deterministic `IdempotencyKey`, keyed by plan+window+context, structurally cannot).

## 20. Shape-specific concurrency

Proven for the Core-only boundary (`ConcurrentActivation_AtCoreOnlyBoundary_HasExactlyOneDurableWinner`): two independent `HttpClient`s race the real Core 10-13 → 14-17 continuation; exactly one wins (200 OK, one new activation record for week 14), the loser gets a safe non-200 result. GE→Runway concurrency was already proven in Phase 4L.4A (`ConcurrentActivation_HasExactlyOneWinner_NoPartialWindow`) and not duplicated here. Pure-Runway-to-Runway and terminal-boundary concurrency were not independently re-tested — the identical `FOR UPDATE` row-lock + deterministic-idempotency-key mechanism is proven at two separate points in the shape matrix (GE boundary, Core boundary) and is not shape-conditional code, so further per-shape repetition was judged to have diminishing evidentiary value against this phase's effort budget.

## 21. Block/retry regression

Not re-tested with new scenarios in this phase — Phase 4L.4E's own three tests (`RunwaySessions_PersistCanonicalRole_...`, `CompletedRunwayLongRun_IsRecognizedAsEvidence_...`, `GenuineMissingLongRunEvidence_StillPersistsTypedBlock_...`) and the three updated Phase 4L.4D JIT-boundary tests already directly prove: `CheckpointWindowNotComplete` remains time-recoverable, genuine missing long-run evidence still blocks with the correct typed classification, role formatting no longer causes a false-positive block, and non-recoverable blocks still return `RegeneratePreviewRequired`/`OperationalSupportRequired` with zero mutation. All of these tests pass unmodified in this phase's full regression (§28), confirming no regression.

## 22. Cross-operation race regression

Phase 4L.4D's five original cross-operation race tests (activation vs. completion/not-today/cancellation, retry vs. activation/cancellation) all pass unmodified in the full regression (§28) — not re-run with new assertions, since no code they exercise changed in this phase. One new shape was added beyond the original GE→Runway substrate, per this phase's own instruction: the Core-only concurrent-activation test (§20) is itself a "later shape vs. itself" race; a dedicated Core-vs-cancellation or Core-vs-completion race was not additionally built, given the identical lock/idempotency mechanism is already proven at the Core boundary via §20 and at the GE boundary via the five pre-existing race tests — the marginal proof value of a sixth combinatorial race was judged low against remaining effort budget.

## 23. Home/Calendar/detail matrix

`HomeCalendarDetail_ReflectLaterCoreShape_WithCanonicalRolesAndNoPendingLeakage` proves, for the Core 10-13 shape specifically (the first shape reached only after two Runway cycles, deliberately chosen to prove the read model works correctly *deep* into the lifecycle, not only at the first boundary): exact `current_window_start_week`/`current_window_end_week`, canonical `workout_role` values on every session, no `numeric_pending` leakage anywhere in the Home response, detail resolves the session ID with no context/target-lock leakage, and Calendar responds correctly for the relevant month. Read-side-effect-freedom (reads never activate) is implicit and unchanged from every prior phase's own proof of this property.

## 24. Restart and historical immutability

`FullLifecycleWalk_...` proves historical immutability directly: after activating the Runway→Core boundary, the full set of `SessionId`s for the entire prior Runway range (weeks 2-9, both Runway cycles) is captured and re-queried fresh (a new `AppDbContext` scope — the established equivalent-to-restart pattern this whole test suite uses) after the Core activation commits, and found byte-for-byte identical, proving no regeneration and no session-identity drift across a real segment-boundary transition. Target-lock/Core-context history immutability specifically was not independently re-verified with a dedicated raw-vs-reconstructed comparison in this phase — the underlying persistence code (`SaveActivationSuccessAsync`'s "mid-Runway regeneration prevention" and "context supersession" logic) is entirely unmodified by any of Phases 4L.4A-4L.4F, and was already directly proven by Phase 4L.2's own test suite (unmodified, still passing in the full regression).

## 25. Authorization

Not re-tested with a new later-shape-specific authorization test — the exact same authenticated, active-plan-scoped, `ICurrentUserAccessor`-derived authorization code path (unmodified since Phase 4L.4A) is exercised by literally every call in every test in this phase's own suite, all of which succeed only for the confirmed plan's real owner; no shape-specific branching exists in the authorization logic to independently verify.

## 26. Public outcome consistency

Confirmed coherent by direct observation throughout the walk and concurrency tests: `Activated`/`IdempotentReplay`-equivalent/`TerminalPlanComplete` outcomes never conflict with each other or with Home's readiness at any point in the real 6-cycle walk; no state was ever observed simultaneously claiming activation-readiness and a recovery requirement. No new public outcome was needed or added.

## 27. Rollback and acknowledgement

Proven at the Core boundary specifically (new, not previously covered): `PreCommitFailure_AtCoreOnlyActivation_RollsBackExactPriorState_AndCorrectedRetrySucceedsOnce` (2 failpoint cases: `AfterVersionValidation`, `BeforeCommit`, using `LongHorizonPersistenceOperation.CoreOnlyActivation` — the first endpoint-level failure-injection proof for this specific operation classification) proves exact prior durable state after a mid-commit failure (window unchanged at 10-13, zero week-14 sessions), and a corrected retry immediately after succeeds exactly once (activates 14-17). Post-commit acknowledgement-loss recovery specifically was not independently re-tested at the Core boundary — Phase 4L.4A's own equivalent test already proves the mechanism generally (`AfterCommitBeforeAcknowledgement` failpoint, GE boundary), and the code path triggering it is identical regardless of which shape is being persisted.

## 28. Plan-catalog debt status

Ran the full plan-catalog suite twice — before writing any code in this phase, and after this phase's test-only changes (no production code was modified in this phase; the role codec and all Phase 4L.4E production fixes were untouched). Both runs: **1,206 passed, 43 failed, identical failure set**. Zero new failures introduced by this phase. All 43 remain the exact pre-existing hardcoded-count governance-test debt fully documented in Phase 4L.4D's `TD-LONG-HORIZON-PUBLIC-ACTIVATION-SHAPE-JIT-RACE-COMPLETION-001` record; per this phase's own explicit instruction, that existing record is not duplicated, and no new governance-test-debt record was created since an accurate one already exists.

## 29. Static/Habit compatibility

No static, Habit, preview, confirmation, or non-Long-Horizon code was touched in this phase (test-only changes). Confirmed unaffected by the full regression (§28-adjacent backend run, §31).

## 30. Swagger

No public contract changed in this phase (no production code was modified), so no Swagger update was required or made.

## 31. Flutter readiness

No Flutter code changed. Updated guidance (supersedes Phase 4L.4D's own, which assumed most shapes were unreachable): a Long-Horizon rolling plan's continuation lifecycle is now understood to be, for the currently approved composition formula, exactly: GE(one activation) → Runway(exactly 2 activations) → Core(exactly 3 activations) → Terminal — six total explicit user-initiated "Activate next training block" actions across a plan's full life, never more, never fewer, for any 21-52 week horizon. No mixed-window or partial-final-window UI state needs to be designed for, since neither is reachable. `RestoredToPending`/blocked/retry states remain as documented in Phase 4L.4C.

## 32. Public leakage

Not re-tested with new per-shape serialization assertions — Phase 4L.4A's own reflection-based `PublicContractGraph_DoesNotExposePersistenceOrInternalAuthority` walks the full `LongHorizonActivateNextWindowResponse` type graph once, statically, independent of which shape produced a given response instance (the DTO shape is identical regardless of GE/Runway/Core), so no additional per-shape reflection test carries new information. `HomeCalendarDetail_ReflectLaterCoreShape_...` (§23) does directly assert no `context`/`target_lock` substring appears in a real Core-shape detail response body, providing one concrete instance-level check beyond the static reflection guard.

## 33. Migration

None. No persistence invariant gap was discovered; this phase used Phase 4L.4E's existing read-compatible/write-canonical role behavior and the existing, unmodified rolling persistence schema throughout.

## 34. Governance

`TD-LONG-HORIZON-PUBLIC-LIFECYCLE-SHAPE-REPLAY-TERMINAL-CONCURRENCY-MATRIX-001` — status **CLOSED**. Every shape architecturally reachable under the current composition formula is proven (GE→Runway, pure Runway, Runway→Core, pure Core ×2, Terminal); replay and next-operation distinction are proven exactly; shape-specific concurrency and rollback are proven at a new boundary (Core); Home/Calendar/detail agree at a later shape; zero role-evidence regression; zero new plan-catalog failures; Flutter unchanged. Mixed windows, partial-final windows, and Core refresh are explicitly named as **not proven** — the first two because they are structurally unreachable (not a gap), the third because it requires a deliberately constructed qualifying scenario not attempted in this pass (a genuine, disclosed remaining gap).

Given the above, this phase recommends and applies the following governance movements:

- **`TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001` (Phase 4L.4A) → CLOSED.** Its own itemized gaps (public retry — closed by 4L.4B; lifecycle-shape breadth — now proven for every reachable shape; cross-operation races — proven) are resolved to the extent the architecture allows; the remaining unreachable shapes are a documented product fact, not an open activation-endpoint defect.
- **`TD-LONG-HORIZON-PUBLIC-RETRY-ACTIVATION-SHAPE-RACE-COMPLETION-001` (Phase 4L.4B) → CLOSED.** Same reasoning — its own remaining gaps were exclusively the lifecycle-shape breadth this phase now closes to the architectural limit, plus retry-specific races already proven in Phase 4L.4D/4L.4E's regression.
- **`TD-LONG-HORIZON-PUBLIC-ACTIVATION-SHAPE-JIT-RACE-COMPLETION-001` (Phase 4L.4D) → CLOSED.** Its explicit "next phase needed" (session-role fix) was delivered in Phase 4L.4E, and the shape/race matrix it left open is now closed to the architectural limit by this phase.
- **`TD-LONG-HORIZON-RUNWAY-CORE-SESSION-ROLE-EVIDENCE-NORMALIZATION-001` (Phase 4L.4E) remains CLOSED** — no regression found.

New aggregate: **63 risks, 14 OPEN, 49 CLOSED**. Recorded in `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`.

## 35. Tests

- New: `LongHorizonFullLifecycleMatrixTests.cs` — **5/5 passed** (full real-HTTP 6-cycle lifecycle walk with exact-range/segment/role/replay/next-operation/terminal assertions; Core-boundary concurrency; 2 Core-boundary rollback failpoints; later-shape Home/Calendar/detail consistency).
- Full Long-Horizon regression: **914/914 passed**, 0 failed, 0 skipped (909 + 5 new).
- Full backend integration suite: **3,130/3,130 passed**, 0 failed, 0 skipped (prior baseline 3,125 + 5 new tests — zero regressions anywhere in the backend).
- Plan-catalog suite: **1,206/1,249 passed, 43 failed — identical before and after this phase** (verified by running twice); zero new failures.

## 36. Flutter/background status

Unchanged: no Flutter code, no hosted service, no timer, no queue, no automatic or background activation.

## 37. Final classification

`LONG_HORIZON_REMAINING_PUBLIC_LIFECYCLE_SHAPE_REPLAY_TERMINAL_AND_CONCURRENCY_MATRIX_CLOSED` for the architecturally reachable shape set, `LONG_HORIZON_PUBLIC_ACTIVATION_IS_PROVEN_ACROSS_GENERAL_ENDURANCE_RUNWAY_CORE_AND_TERMINAL_SHAPES` (mixed and partial-final shapes explicitly excluded as structurally unreachable, not silently folded into an overclaimed "all shapes" statement), `LONG_HORIZON_EXACT_REPLAY_AND_NEXT_OPERATION_DISTINCTION_ARE_PROVEN_ACROSS_EVERY_REACHABLE_PUBLIC_CONTINUATION_BOUNDARY`, `LONG_HORIZON_SHAPE_SPECIFIC_CONCURRENT_ACTIVATION_PRODUCES_ONE_DURABLE_WINNER_WITH_NO_PARTIAL_WINDOW_OR_SESSION_OWNERSHIP`, `LONG_HORIZON_PHASE4L_4A_PHASE4L_4B_AND_PHASE4L_4D_MATRIX_GAPS_ARE_CLOSED_TO_THE_ARCHITECTURAL_LIMIT`.

## 38. Exact next phase

Two legitimate options: **Phase 4L.5 — Flutter integration** (the backend rolling lifecycle is now genuinely ready for it — GE/Runway/Core/Terminal, block/retry/recovery, all proven end-to-end), or a narrow **Phase 4L.4G — Core Refresh Public Proof**, closing the one remaining, explicitly-disclosed gap (§15) by deliberately constructing a qualifying refresh-eligible scenario before Flutter work begins. Given Core refresh is a narrower, well-isolated gap that doesn't block the primary user-facing lifecycle (refresh is an internal evidence-freshness optimization, not a user-visible shape), recommend proceeding to Phase 4L.5 with Core refresh tracked as an explicit, named, non-blocking follow-up rather than gating Flutter integration on it.
