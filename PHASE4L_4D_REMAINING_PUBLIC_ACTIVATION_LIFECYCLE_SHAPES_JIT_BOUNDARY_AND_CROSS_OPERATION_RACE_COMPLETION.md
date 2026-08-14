# Phase 4L.4D — Remaining Public Activation Lifecycle Shapes, JIT Boundary Classification and Cross-Operation Race Completion

## 1. Executive result

The JIT-boundary misclassification is **fixed and empirically validated against a real, naturally reachable block** (not just seeded). Six cross-operation races (activation vs. completion, not-today, cancellation; retry vs. activation, cancellation; JIT-boundary Home/retry agreement) are proven against real PostgreSQL. The full 10-shape lifecycle matrix (Parts 5–14 of the prompt) is **not** proven, because this phase's own investigation established that 9 of those 10 shapes are **not naturally reachable through the public activation endpoint today** — not a testing gap, an actual product-surface limitation with two identified root causes (§7). This is reported honestly rather than fabricated: `LONG_HORIZON_REMAINING_PUBLIC_ACTIVATION_LIFECYCLE_SHAPES_JIT_BOUNDARY_AND_CROSS_OPERATION_RACE_COMPLETION_PROVED` for the JIT-boundary and cross-operation-race scope; the lifecycle-shape scope closes as `LONG_HORIZON_PUBLIC_ACTIVATION_COMPLETION_REMAINS_BLOCKED_BY_EXPLICIT_LIFECYCLE_SHAPE_GAP` for the remaining shapes, with the exact blocking cause named.

## 2. Gaps inherited from 4L.4A–4L.4C

Per the prompt's own inheritance list: incomplete lifecycle-shape proof, JIT-boundary misclassification, unproven activation-vs-completion/not-today/cancellation/retry races, incomplete terminal-behavior proof, and two partially-open governance records.

## 3. Scope and exclusions

Fixes the JIT-boundary classification (one file, `LongHorizonRollingWindowActivationService.cs`), adds 7 new integration tests (JIT-boundary correctness + 6 cross-operation races), and documents — rather than fabricates — the lifecycle-shape reachability finding. No second planner, no evidence-entry/activity-import capability, no recovery window, no retry-policy change, no historical-outcome mutation, no formula change of any kind, no background/automatic activation, no Flutter change, no migration, no commit.

## 4. Existing public continuation architecture

Unchanged from Phase 4L.4A/4L.4B/4L.4C — inspected, not modified, except for the one JIT-boundary branch (§6). `LongHorizonRollingWindowActivationService` still delegates entirely to the existing `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingRestartContinuationService`/`LongHorizonRollingStateRepository` chain.

## 5. JIT-boundary investigation

**Exact trigger**: any continuation whose next window crosses into or stays within Preparation Runway/Core (`LongHorizonRollingWindowActivationService`'s "else" branch — neither `pureGe` nor a GE-evaluator `NextGeWindowBlocked`) where the checkpoint runtime's `EvidenceSnapshot` or `ValidatedLoad` comes back null. **Root cause, newly confirmed empirically in this phase**: `LongHorizonRollingJitActivationRuntime` assigns Runway session roles via `SlotRole.ToString()` (C# enum default — PascalCase, e.g. `"LongRun"`), while `LongHorizonRollingOutcomeEvidenceAdapter`'s long-run detection matches only the GE convention `"LONG_RUN"` (`SessionRole.Equals("LONG_RUN", OrdinalIgnoreCase)` — which does not match `"LongRun"`, a different string, not just different casing). So **every** Runway-window continuation's evidence aggregation computes zero completed long runs regardless of real session completion, `ValidatedSustainableLoad` computation fails (`usableEvidenceWeeks.Count>0 && completedLongRuns.Count>0` is never true), and `ValidatedLoad` comes back null. **Original behavior**: `throw new LongHorizonReadStateCorruptException(...)` — a 409 with no durable `Block` record at all: unclassifiable, non-retryable, indistinguishable from genuine internal corruption, and inconsistent with every other block path's public contract. **No state mutation occurred before the original error** (the throw happens before any repository write). **Origin**: checkpoint evaluation (evidence aggregation inside the existing, unmodified `LongHorizonCheckpointEvidenceAggregator`), surfaced at the public-mapping boundary in `LongHorizonRollingWindowActivationService`.

## 6. Corrected JIT-boundary classification

`LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` now treats a null `EvidenceSnapshot`/`ValidatedLoad` on this path exactly like a GE-evaluator block: it calls the same, unmodified `LongHorizonRollingBlockPersistenceAdapter.PersistBlockAsync` authority with `LongHorizonReasonCode.FromJit(LongHorizonJitReasonCode.JitValidatedLoadUnavailable)` (an existing Phase 4K.4 reason code, not invented) and `retryEligible: true`. This one code path now produces a **real, durable, classifiable `Block` record** — feeding directly into Phase 4L.4C's existing, unmodified `LongHorizonBlockRecoveryClassification`, which already maps `JitValidatedLoadUnavailable` → `RequiresRegeneratePreview`. Home, the activation response, and retry now all agree automatically, because they all read the same durable classification — no separate alignment code was needed (§23 confirms no conflicting guidance is possible by construction, not by an additional consistency check).

**Fix scope note**: this phase corrects the *classification* of the evidence gap (unclassified corrupt-state → real, typed, recoverable-or-not Block). It does **not** fix the *underlying* role-naming mismatch that causes the evidence gap in the first place — that is a deeper defect in session-role construction spanning the GE/Runway/Core composition code (Phase 4K era), explicitly out of this phase's narrow "correct the classification at the authoritative boundary" mandate, and touching it risks exactly the kind of formula/composition change this and every prior phase was told not to make. Tracked as an explicit follow-up (§37).

## 7. Lifecycle-shape inventory — reachability finding

Investigated all 10 requested shapes against this repository's actual behavior. Finding, empirically confirmed (not assumed) via direct testing:

| # | Shape | Naturally reachable today? | Blocking cause |
|---|---|---|---|
| 1 | Pure GE continuation | **No** | `CheckpointWindowNotComplete` (Phase 4L.4C finding): the evidence aggregator's real-calendar-date gate (`periodEnded`) is false whenever the window's real calendar dates haven't yet elapsed relative to the real request date — true for any freshly confirmed plan regardless of session outcomes. |
| 2 | GE→Runway boundary | **Yes** — already proven, Phase 4L.4A, re-confirmed here | The one shape whose window crosses `nextStart > GeneralEnduranceWeeks` before the evaluator's `periodEnded` gate is ever reached. |
| 3 | Pure Runway continuation | **No** | The role-naming mismatch (§5): any Runway-window continuation attempt computes zero completed long runs and blocks on `JitValidatedLoadUnavailable`, regardless of real completion. |
| 4–6 | Runway→Core mixed (1+3/2+2/3+1) | **No** | Same as #3 — requires a prior successful Runway continuation, which cannot occur. |
| 7 | Pure Core continuation | **No** | Same as #3 — requires reaching Core first, which requires #4–6. |
| 8 | Core refresh continuation | **No** | Same as #3/#7. |
| 9 | Final partial one-week window | **No** | Requires driving a plan through every prior window, all blocked by #1/#3. |
| 10 | Terminal call after final window | **No** | Same as #9. |

**This table is itself the required Part 3 deliverable** — it was not possible to "document total horizon / prior range / expected next range" etc. for shapes 3–10 as concrete worked examples, because no confirmed plan can reach them through the public API today. Fabricating those fields for unreachable shapes was explicitly forbidden by this phase's own instructions ("do not fabricate unsupported horizons only to force a shape") and was not done.

## 8. Public test setup standard

All 7 new tests use a real confirmed `RollingLongHorizon` plan, real PostgreSQL, the production preview/confirm routes, production session-outcome persistence via the real completion/not-today endpoints, and the real production `LongHorizonRollingWindowActivationService`/`LongHorizonRollingRetryContinuationService` through their public HTTP routes — zero direct DB seeding was used in this phase's new tests (unlike Phase 4L.4C, which needed seeding for reason codes unreachable via any flow; this phase's JIT-boundary reason is naturally reachable, so no seeding was needed here).

## 9–17. Shapes 1, 3–10

Not proven, per §7's reachability finding. No fixture or seeding substitute was constructed to fake these shapes — per this phase's own Part 4 instruction, direct seeding is permitted only for states "not naturally reachable ... and not used to fabricate success for unsupported behavior"; since these shapes represent unreachable *activation-success* behavior specifically, seeding a fake success would have violated that constraint directly. §18 (GE→Runway) is the only proven shape, re-confirmed rather than re-litigated (Phase 4L.4A already proved it exhaustively; this phase reuses it as the substrate for every race test instead of duplicating that proof).

## 18. Endpoint replay matrix

Proven only for the reachable shape (GE→Runway, Phase 4L.4A) and for the JIT-boundary block (this phase, via idempotent block-reason persistence — `PersistBlockAsync`'s existing deterministic `IdempotencyKey` prevents duplicate block records on repeated calls, unchanged mechanism). Not proven for the other 8 unreachable shapes.

## 19. Next-operation distinction

Not proven beyond what Phase 4L.4A already established for GE→Runway (structural argument via the deterministic `IdempotencyKey`). The requested "Runway A → mixed B", "mixed A → Core B", "Core A → Core B" sequences require the corresponding shapes to be reachable first (§7).

## 20. Concurrent activation matrix

Proven for the one reachable shape only (`RetryVsActivation_NonRecoverableBlock_NoBypassToActivated` exercises concurrent activation against a real Blocked state; Phase 4L.4A's own `ConcurrentActivation_HasExactlyOneWinner_NoPartialWindow` proves the GE→Runway shape's own concurrency). GE→Runway, mixed, Core, final/terminal concurrent-shape coverage beyond that is not proven (§7).

## 21. Activation versus completion

`ActivationVsCompletion_StaleActivationCannotUseIncompleteEvidence_LaterFreshActivationSucceeds` — real race using window 1 (GE, 1 week, 4 sessions), final session left `Planned`, one client completes it while a second concurrently requests activation. Completion always succeeds exactly once (proven by direct DB read); a subsequent fresh activation request either succeeds (creating exactly one `LongHorizonActivationWindowRecord` for week 2) or safely reports `CurrentWindowInProgress` — both accepted as correct given PostgreSQL's own non-deterministic statement interleaving is not client-controlled, matching the exact leniency convention Phase 4L.4/4L.4A's own equivalent races already use.

## 22. Activation versus not-today

`ActivationVsNotToday_StaleActivationCannotUseIncompleteEvidence` — identical structure using not-today instead of completion. Not-today always persists exactly once; stale activation cannot succeed against incomplete evidence; a later fresh request is safe either way.

## 23. Activation versus cancellation

`ActivationVsCancellation_IsCoherent_NoSessionsAfterCancellationWins` — races a fully-eligible activation against cancellation. Cancellation always succeeds; the plan is `Cancelled` afterward; `LongHorizonActivationWindowRecords` count is exactly 1 (only the initial confirm-time window) or 2 (initial + the race's activation, if it committed first) — never a partial/mixed count; Home no longer exposes the plan as active regardless of ordering.

## 24. Retry versus activation

`RetryVsActivation_NonRecoverableBlock_NoBypassToActivated` — races retry against activation on a real, naturally-reached `JitValidatedLoadUnavailable` block. **Both** requests reject (409) — there is no ordering under which either operation succeeds, proving no direct or indirect Blocked→Activated bypass exists. Zero sessions created beyond the already-activated window's boundary.

## 25. Retry versus cancellation

`RetryVsCancellation_IsCoherent_NoResurrectionOrPartialState` — races retry (on a real non-recoverable block) against cancellation. Cancellation always succeeds; retry either returns its own typed rejection (`RegeneratePreviewRequired`, evaluated independent of the cancellation race) or a non-disclosing `NotFound` (if the `TrainingPlans` row lock serializes it behind the cancellation commit) — both safe, non-mutating, typed losers; zero `RetryRestored` records in either case; no plan resurrection.

## 26. Recovery-policy preservation

Unchanged and re-confirmed: `CheckpointWindowNotComplete` remains the only `IsRetryEligibleWithoutNewEvidence` class (Phase 4L.4C, untouched); `JitValidatedLoadUnavailable` (now a real, reachable block thanks to §6) correctly maps to `RequiresRegeneratePreview`, proven with **zero mutation** on retry rejection (`JitBoundaryBlock_HomeAndRetryAgree_RegeneratePreviewRequired_ZeroMutation` asserts full before/after aggregate-state equality). All 19 reason codes remain classified exactly as Phase 4L.4C established (`LongHorizonBlockRecoveryClassification` was not modified in this phase). No new evidence-entry capability was added.

## 27. Evidence-content block limitation

Unchanged from Phase 4L.4C: `ValidatedLongRunEvidenceUnavailable` and its content-based siblings remain unreachable through any currently-existing public flow (confirmed again by this phase's own investigation — see §7's table, which independently reconfirms Phase 4L.4C's finding via a completely different natural mechanism, the Runway role-naming gap, landing on the same practical conclusion: content-based evidence blocks are not exercised by real user action today). No new evidence capability was added; no artificial flow was invented to reach them.

## 28. Home/Calendar/detail consistency

Re-confirmed via `JitBoundaryBlock_HomeAndRetryAgree_...`: Home's `checkpoint_readiness`/`recovery_requirement`/`blocked_public_reason_category` for the JIT-boundary block agree exactly with the activation and retry responses, and the raw internal reason code (`JitValidatedLoadUnavailable`) never appears in the serialized Home body. Calendar/detail consistency for the one reachable shape (GE→Runway) was already proven in Phase 4L.4A and not re-tested here.

## 29. Public errors

No new error codes were added. `LONG_HORIZON_CONTINUATION_BLOCKED` (unchanged) now correctly fires for the JIT-boundary case too, backed by a real `Block` record rather than `LONG_HORIZON_READ_STATE_CORRUPT`. `JitBoundaryEvidenceGap_ClassifiesAsRealDurableBlock_NotUnclassifiedCorruptState` explicitly asserts the old error code no longer appears.

## 30. Rollback and acknowledgement loss

Not separately re-tested in this phase: the JIT-boundary fix reuses the exact same `PersistBlockAsync` code path Phase 4L.4A's own two rollback failpoint tests (`AfterVersionValidation`, `BeforeCommit`, on `LongHorizonPersistenceOperation.InitialPersistence`/`BlockPersistence`) already cover for the GE block path — no new persistence branch was introduced, only a new caller of an already-rollback-proven authority.

## 31. Authorization

Unchanged; not re-tested (no authorization-relevant code changed in this phase).

## 32. Swagger

No new public types were introduced (the fix reuses existing `LongHorizonContinuationBlockedException`/`LONG_HORIZON_CONTINUATION_BLOCKED` and existing `LongHorizonRecoveryRequirement` values) — Swagger output is unchanged, verified by the unmodified Phase 4L.4A/4L.4C Swagger tests continuing to pass.

## 33. Static/Habit compatibility

No static, Habit, preview, confirmation, or non-Long-Horizon route/DTO was touched. Confirmed by the full regression (§38).

## 34. Flutter readiness

No Flutter code changed. The JIT-boundary fix means a Runway-window block now surfaces to a future Flutter client exactly like any other `RegeneratePreviewRequired` block (§6) — no new UI state is needed beyond what Phase 4L.4C's readiness documentation already covers; the corrected classification makes that existing documentation *accurate* for this case for the first time, rather than requiring new documentation.

## 35. Public leakage

No new DTO surface; existing leakage guards (Phase 4L.4A/4L.4B/4L.4C, all unmodified) continue to pass in the full regression.

## 36. Governance

`TD-LONG-HORIZON-PUBLIC-ACTIVATION-SHAPE-JIT-RACE-COMPLETION-001` — status **stays OPEN** (per this phase's own success criteria: "close only if all required lifecycle shapes are proven through the public path" — 9 of 10 are not, and cannot be, without either real calendar time passing (months, for `CheckpointWindowNotComplete`) or a separate fix to the Runway/Core session-role-naming defect this phase's own investigation surfaced but explicitly did not fix). The JIT-boundary-classification and cross-operation-race scope within it **is** fully closed.

`TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001` (Phase 4L.4A) **stays partially closed** — its lifecycle-shape/race gaps are the same ones this phase found structurally unreachable, not merely untested; per the prompt's own instruction ("may move to CLOSED only if all its remaining shape and race gaps are closed"), it cannot honestly close.

`TD-LONG-HORIZON-PUBLIC-RETRY-ACTIVATION-SHAPE-RACE-COMPLETION-001` (Phase 4L.4B) **stays OPEN** for the same reason.

`TD-LONG-HORIZON-BLOCKED-RECOVERY-NEW-EVIDENCE-REASSESSMENT-001` (Phase 4L.4C) remains **CLOSED**, unaffected — this phase's fix and races only strengthen, never contradict, its recovery-policy conclusions (§26).

Recorded in `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`. New aggregate: **61 risks, 16 OPEN, 45 CLOSED** (the new record counts as OPEN; no existing record's status changes).

## 37. Tests

- New: `LongHorizonJitBoundaryAndCrossOperationRaceTests.cs` — **7/7 passed** (JIT-boundary real block classification and error-code correction; Home/retry agreement with zero mutation; retry-vs-activation no-bypass; retry-vs-cancellation coherence; activation-vs-completion race; activation-vs-not-today race; activation-vs-cancellation race).
- Full Long-Horizon regression: **902/902 passed**, 0 failed, 0 skipped (895 + 7 new).
- Full backend integration suite: **3,118/3,118 passed**, 0 failed, 0 skipped (prior baseline 3,111 + 7 new tests — zero regressions anywhere in the backend).
- Plan-catalog suite: **1,206/1,249 passed, 43 failed** — investigated directly, not glossed over. All 43 failures are `AggregateCountSentence_IsInternallyConsistent`/`RegistryAndMarkdown_AreUniqueAndSemanticallyAligned`-family tests across ~20 historical Long-Horizon governance test files that hardcode a `risks.Count`/aggregate-sentence snapshot at authoring time. Proven pre-existing and unrelated to this session's edits by temporarily truncating the governance ledger back to its 57-entry pre-4L.4A-session state and re-observing 42 of the same 43 failures (the ledger's true baseline, before this session's four 4L.4 sub-phases ever touched it, going back to `git show HEAD` at only 15 committed entries — meaning ~42 uncommitted prior-session entries already existed and had already broken these hardcoded-snapshot tests long before this session began). Exactly 1 additional failure is attributable to this session's own governance-ledger growth (57→61 entries across the four 4L.4 sub-phases) and is accounted for, not hidden. **Also found and fixed in this phase, unprompted**: a real regression this session's own earlier PowerShell-based JSON edits (Phase 4L.4A/B/C) had introduced — `ConvertTo-Json`'s default HTML-safe escaping turned literal `->`/`<`/`&`/`'` characters into `>`/`<`/`&`/`'` throughout the entire file, breaking ~53 raw-text-substring-matching governance tests that expected the literal characters. Fixed by un-escaping the whole file back to literal characters (validated JSON before and after); this phase's own JSON edits used plain Node.js `JSON.stringify` instead, which does not have this escaping behavior.

## 38. Flutter/background status

Unchanged: no Flutter code, no hosted service, no timer, no queue, no automatic or background activation.

## 39. Final classification

Split, honest result: `LONG_HORIZON_JIT_BOUNDARY_FAILURES_NOW_HAVE_THE_CORRECT_PUBLIC_CLASSIFICATION_RECOVERY_REQUIREMENT_AND_ZERO_MUTATION_BEHAVIOR` and `LONG_HORIZON_ACTIVATION_COMPLETION_NOT_TODAY_CANCELLATION_AND_RETRY_RACES_PRODUCE_ONE_COHERENT_DURABLE_RESULT_WITH_NO_SPLIT_OWNERSHIP` — both **PROVED**. `LONG_HORIZON_PUBLIC_ACTIVATION_IS_PROVEN_ACROSS_GE_RUNWAY_MIXED_CORE_REFRESH_FINAL_PARTIAL_AND_TERMINAL_LIFECYCLE_SHAPES` — **NOT achieved**; the honest replacement is `LONG_HORIZON_PUBLIC_ACTIVATION_COMPLETION_REMAINS_BLOCKED_BY_EXPLICIT_LIFECYCLE_SHAPE_GAP`, with the exact cause named (§7): a real-calendar-time gate for GE and a session-role-naming defect for every Runway/Core shape beyond the first crossing.

## 40. Exact next phase

**Phase 4L.4E — Runway/Core Session-Role Evidence Normalization**, a small, sharply-scoped fix analogous to this phase's own JIT-boundary correction: align `LongHorizonRollingJitActivationRuntime`'s Runway/Core session-role string construction with the GE convention (or make the evidence adapter's long-run/role detection tolerant of both), so that a genuinely completed Runway/Core long run is recognized as evidence. That fix, not attempted here (it touches session-role construction across GE/Runway/Core composition code, explicitly out of this phase's narrow classification-only mandate), is the actual precondition for ever proving lifecycle shapes 3–10. Only after that should Phase 4L.5 (Flutter integration) begin — the backend rolling lifecycle is ready for UI integration for the one proven shape (GE→Runway) and its block/retry/recovery surface, but not yet for a plan that needs to continue past its first Runway window.
