# Phase 4L.4C — Blocked Recovery, New-Evidence Reassessment and Meaningful Retry Policy

## 1. Executive result

**Outcome B**: `LONG_HORIZON_BLOCKED_RECOVERY_REQUIRES_REGENERATE_PREVIEW_PLAN_TERMINATION_OR_OPERATIONAL_SUPPORT`. Direct investigation (Part 3) found no evidence-update, activity-import, profile-refresh or recovery-window authority exists anywhere in this repository. Public retry no longer implies recoverability for a block whose evidence is durable and immutable: it now classifies every reachable block reason and only restores Blocked→Pending when the block is genuinely time-recoverable (`CheckpointWindowNotComplete`, an existing canonical real-calendar-time rule); every other reason returns a typed, no-mutation `RegeneratePreviewRequired` or `OperationalSupportRequired` result instead of a misleading `RestoredToPending`. The approved recovery path for those blocks — cancel the blocked plan and confirm a new preview — is proven to work using **100% pre-existing, unmodified capability**.

## 2. Immutable-evidence retry loop

The gap this phase resolves: Phase 4L.4B's retry always eventually succeeded (a server-derived, date-based fingerprint made *any* later-day retry pass the repository's "changed evidence" guard), regardless of whether the underlying evidence had actually changed. For a block whose evidence comes from an already-fully-terminal window — which the activation endpoint's own eligibility gate guarantees is the *only* kind of block reachable, since it requires full window terminality before ever calling the checkpoint runtime — no public operation can subsequently change that evidence. So the old retry created meaningless state churn: it always "succeeded," and the very next checkpoint evaluation always re-blocked identically. This phase closes that gap for real, not by picking the easy answer, but by directly investigating what block Phase 4L.4B's own test scenario actually produced.

## 3. Scope and exclusions

Adds a block-taxonomy classification (pure function, no new persistence), extends the retry service with an eligibility gate, extends the Home response with two new fields, adds two typed errors, and adds a corrected/expanded test suite (24 pure classification tests + 13 real-Postgres integration tests, replacing Phase 4L.4B's now-superseded retry tests). Adds no evidence-submission endpoint, no reassessment endpoint, no activity-import subsystem, no recovery-window catalog content, and no migration — each is explicitly rejected in Part 3/9/10/11 below, not silently skipped. No Flutter change. No commit.

## 4. Existing block taxonomy

All 19 reason codes come from the existing, unmodified Phase 4K.3/4K.4 taxonomy (`LongHorizonCheckpointReasonCode`, 9 values; `LongHorizonJitReasonCode`, 10 values) — none invented here.

| Reason | Source | Reachable as a real Block today? | Evidence could ever change? | Class |
|---|---|---|---|---|
| `CheckpointWindowNotComplete` | Checkpoint evaluator, real-calendar-date gate | **Yes** — confirmed empirically (§8) | Yes, via elapsed real calendar time only | RecoverableWithElapsedCalendarTime |
| `CheckpointEvidenceStale` | Checkpoint evaluator | No — only reachable on the `MaintenanceOnly` (non-blocking) path | N/A | RequiresRegeneratePreview (defensive) |
| `ValidatedLoadUnavailable` | Checkpoint evaluator | Only if window calendar period has ended with incomplete evidence — not reachable through the public activation eligibility gate today | No (immutable terminal window) | RequiresRegeneratePreview |
| `ValidatedLongRunEvidenceUnavailable` | Checkpoint evaluator | Same as above — see §8 finding | No | RequiresRegeneratePreview |
| `AdherenceConfidenceInsufficientForGrowth` | Checkpoint evaluator | No — only reachable on the `MaintenanceOnly` (non-blocking) path | N/A | RequiresRegeneratePreview (defensive) |
| `MaintenanceAnchorUnavailable` | Checkpoint evaluator (`ValidatePrior`) | No — internal trigger only; the persisted block reason is always `EvidenceConflictUnresolved` for this path | N/A | RequiresRegeneratePreview (defensive) |
| `NumericWindowInfeasible` | Checkpoint evaluator (availability) | Only if plan's static preferred-days configuration is infeasible | No (no availability-update endpoint) | RequiresRegeneratePreview |
| `SafetyReassessmentRequired` | Checkpoint evaluator | Only if `LongHorizonSafetyState.UnresolvedSafetyCritical` — no public input source sets this today | No (no safety-input endpoint) | OperationalSupportRequired |
| `EvidenceConflictUnresolved` | Checkpoint evaluator catch-path | Only on contradictory `TrainingDay` evidence (a data-integrity condition) | No | OperationalSupportRequired |
| `RunwayJitContextUnavailable` / `CoreJitContextUnavailable` / `JitValidatedLoadUnavailable` / `JitValidatedLongRunUnavailable` / `JitPaceSourceUnresolved` / `JitGoalFeasibilityUnresolved` / `JitAvailabilityInfeasible` / `JitSegmentTransitionInfeasible` | Runway/Core JIT composition | Only via the boundary-handoff path (Phase 4L.4A's own null-`ValidatedLoad` guard currently maps missing-evidence there to `LongHorizonReadStateCorruptException` instead — a related, separate gap, not closed in this phase; see §21) | No (immutable terminal window / static plan config) | RequiresRegeneratePreview |
| `JitEvidenceConflictUnresolved` / `JitActivationBoundaryMissed` | Runway/Core JIT composition | Contradiction/lifecycle-integrity conditions | No | OperationalSupportRequired |

## 5. Recoverability classification

Three classes were implemented (`LongHorizonBlockRecoveryClass`, internal): `RecoverableWithElapsedCalendarTime`, `RequiresRegeneratePreview`, `OperationalSupportRequired`. `TerminallyUnsupported` and `CorruptState` were not implemented as separate classes — a genuinely terminal plan is already handled distinctly by the existing `TerminalPlanComplete` readiness (unrelated to blocking), and integrity/corruption conditions are folded into `OperationalSupportRequired` (both mean "do not self-service, needs review," and splitting them further had no behavioral consequence worth the added surface). `LongHorizonBlockRecoveryClassificationTests` proves all 19 reason codes plus an unrecognized code classify deterministically, and that corruption/contradiction reasons and permanent-infeasibility reasons are never classified as temporarily retryable (fail-closed default for unknown codes).

## 6. Available new-evidence sources

Investigated per Part 3's checklist — **none exist as a public, persisted, user-owned capability beyond the fixed rolling schedule**:

| Source | Exists today? | Public? | Persisted? | Could alter checkpoint evidence? |
|---|---|---|---|---|
| Rolling session completion/not-today | Yes | Yes | Yes | Yes — but immutable once terminal (one-shot) |
| Static `TrainingDay` outcomes | Yes (different strategy) | Yes | Yes | Not applicable to `RollingLongHorizon` plans |
| Recent weekly volume / longest run (profile fields) | Only at plan-creation time (preview request) | No update endpoint | Snapshotted into the plan at confirm | No — no refresh flow |
| Recent race result | No | — | — | — |
| Later independent runs outside the schedule | No | — | — | — |
| Checkpoint date progression | Yes | Implicit (server-derived per request) | N/A | Only for `CheckpointWindowNotComplete` |
| Wearable/imported activity | No | — | — | — |
| User-edited outcome correction flow | No | — | — | — |

## 7. Historical outcome immutability

Unchanged and reconfirmed: `Completed`/`NotToday` outcomes are one-shot (`LongHorizonRollingSessionMutationService` throws a typed conflict on any attempt to set a different outcome on an already-terminal session — Phase 4L.4, unmodified). No correction flow exists; none was added in this phase (per Part 4's own instruction: "do not add one merely to unblock retry").

## 8. Long-run-evidence block decision — and the empirical correction that drove it

**Investigation finding, not assumption.** Phase 4L.4B's own test asserted a block occurred and *inferred* the reason was `ValidatedLongRunEvidenceUnavailable` from reading the evaluator's source code, without ever querying the persisted `InternalReasonCode`. This phase queried it directly: for the exact same scenario (every `LONG_RUN` session `NotToday`, remaining sessions `Completed`, second pure-GE window), the real persisted reason is **`CheckpointWindowNotComplete`** — and, confirmed by a second direct check, this fires *even when every session including the long run is `Completed`*. The evaluator's `!WindowCalendarPeriodEnded` check runs before any evidence-completeness check and is false whenever the window's real calendar dates (necessarily in the future for a freshly confirmed plan) haven't yet elapsed relative to the real request date — this holds regardless of session outcomes. Phase 4L.4A's/4L.4B's own successful activation tests only ever avoided this because their plans' second windows crossed the GE→Runway boundary (a different code path that skips this evaluator entirely), not because the evidence was different.

Given this, `ValidatedLongRunEvidenceUnavailable` is **not independently reachable through pure-GE continuation** with a realistically future-dated plan in this environment; it requires either a plan whose window dates have already elapsed, or is reached via the Runway/Core JIT boundary (currently miscategorized as `LongHorizonReadStateCorruptException` rather than a clean typed block there — §21). This phase proves the classification is correct for it regardless, by directly seeding the exact durable fields `SaveBlockAsync` would set for that reason (the same construction technique Phase 4L.2's own tests already use), rather than fighting to reach it through a public flow that cannot naturally produce it today.

**Decision for `ValidatedLongRunEvidenceUnavailable`**: **Candidate E-adjacent — `RequiresRegeneratePreview`**, not a bespoke "Candidate A/B/C" new-evidence flow. None of A (no activity-import authority), B (no profile-refresh endpoint), or C (no recovery-window catalog/formula support — see §14) exist. D is supported by 100% existing capability (§15) and does not require inventing anything.

## 9. Generic retry policy

**Decision B** (from the prompt's own Part 6 options): retry became eligible only when an approved recovery authority is present. Concretely: `LongHorizonRollingRetryContinuationService.RetryAsync` now classifies the last `Block` record's `InternalReasonCode` via `LongHorizonBlockRecoveryClassification` *before* calling `PersistRetryAsync`. Only `RecoverableWithElapsedCalendarTime` proceeds to the existing repository call (which still separately enforces the strictly-later-checkpoint-date guard); every other class returns a typed, zero-mutation error. `RetryWithImmutableEvidence...`/`RequiresRegeneratePreview_...` tests prove no `LongHorizonBlockRetryRecord`, no lifecycle transition, and no aggregate field changes when retry is rejected this way.

## 10. Public recovery-state contract

`LongHorizonRecoveryRequirement` (public enum): `None`, `CalendarWindowPending`, `RegeneratePreviewRequired`, `OperationalSupportRequired`. Scoped down from the prompt's longer suggested list (`NewTrainingEvidenceRequired`, `LongRunEvidenceRequired`, `ProfileEvidenceRefreshRequired`, `RecoveryWindowRequired` were not added) because none of those authorities exist (§6) — adding enum values for capabilities that don't exist would misrepresent the product surface to a future Flutter client. `LongHorizonActivePlanSummaryResponse` (Home) gained `RecoveryRequirement` (nullable, only set when `ReassessmentRequired`) and `BlockedPublicReasonCategory` (reuses the existing, already-public `PublicReasonCategory` string set at block time — `"MoreTrainingDataNeeded"` for GE/JIT evidence blocks — no new internal string was invented).

## 11. Home and active-details behavior

`Home_ExposesRecoveryRequirementAndBlockedReasonCategory_WhenBlocked` proves: `checkpoint_readiness = "reassessment_required"`, `recovery_requirement` set correctly per the block's real classification, `blocked_public_reason_category` visible, and the raw internal reason code (`CheckpointWindowNotComplete`, `ValidatedLongRunEvidenceUnavailable`, etc.) never appears anywhere in the response body. Active-details (`/active/details`) was not separately extended in this phase — it already omits numeric detail for non-activated weeks and doesn't surface per-window readiness beyond what Home already covers; extending it was judged out of scope given the Home extension already satisfies the "consistent recovery state" requirement.

## 12. Evidence-update authority

**Not implemented.** Per Part 9's own instruction ("implement only if the repository/product already supports or explicitly approves it") and the §6 inventory finding no such capability exists, no `POST /api/v1/plans/active/long-horizon/reassessment-evidence` route was added.

## 13. Independent activity evidence

**Not implemented, and explicitly rejected** per Part 10's own instruction. No workout/activity-import model exists outside the generated rolling schedule anywhere in this repository (confirmed by the same investigation that produced §6). Candidate A is unavailable.

## 14. Recovery-window feasibility

**Rejected.** No recovery/readiness/bridge workout catalog entry, structural roadmap segment, or numeric-progression formula for a non-race-progression window exists in the plan catalog or the Long-Horizon structural/GE/Runway/Core code inspected for this phase. Synthesizing ad hoc easy/long-run sessions to manufacture one was explicitly out of bounds per the prompt's own constraint ("do not synthesize ad hoc easy/long-run sessions"). Candidate C is unavailable.

## 15. Regenerate-preview policy

The confirmed rolling plan cannot safely continue past an evidence-immutable block; the approved path is: **(1) cancel the blocked plan** via the existing `POST /api/v1/plans/{planId}/cancel` (already enforces one-active-plan uniqueness and already proven, Phase 4L.4, to retain full history read-access to the cancelled plan's rolling state), **(2) generate and confirm a new preview** via the existing `POST /api/v1/plans/generate-preview/race/long-horizon` + `POST /api/v1/plans/confirm/long-horizon` routes (unmodified). `RegeneratePreview_ViaExistingCancelAndNewConfirm_IsTheApprovedRecoveryPath` proves this full sequence end-to-end against real PostgreSQL: the new plan becomes active and `CurrentWindowInProgress`, the old plan is `Cancelled`, and its rolling state (still `NumericActivationBlocked`, immutable) remains queryable. No atomic "replace" capability was built — none was needed, since cancel-then-confirm already satisfies every requirement in Part 12 (one-active-plan uniqueness via the existing conflict check; historical accessibility via existing cancellation semantics; no silent replacement, since the user must explicitly take both steps). Race identity is *not* reused — the new preview is a fresh confirmation, matching how every other new-plan flow in this codebase already works; nothing here is Long-Horizon-specific or novel.

## 16. Retry eligibility authority

Implemented as a direct classification check rather than a separate named `LongHorizonRetryEligibility` result record — the prompt's suggested field set (`IsRetryAllowed`, `RecoveryClass`, `RecoveryRequirement`, `CurrentBlockIdentity`, `EvidenceVersion/Fingerprint`, `HasMateriallyNewEvidence`, `PublicOutcome`, `ReasonCategory`, `IsPermanent`, `IsCorrupt`) collapses, for this repository's actual reachable state space, into two already-sufficient primitives: `LongHorizonBlockRecoveryClass` (the class) and the boolean `IsRetryEligibleWithoutNewEvidence(class)`. A dedicated record type was judged to add indirection without adding information, given no "HasMateriallyNewEvidence" dimension is ever independently computed (there is no evidence source that produces one — §6).

## 17. Material-evidence change detection

Unchanged from Phase 4L.4B for the one class that legitimately uses it (`RecoverableWithElapsedCalendarTime`): the server-derived `{blockFingerprint}|retry:{date}` composition, gated additionally now by the classification check so it is only ever attempted for genuinely time-recoverable blocks. For every other class, no fingerprint comparison is attempted at all — the classification itself is the gate, which is stricter and more correct than any fingerprint-diffing scheme could be for evidence that provably cannot change.

## 18. Unchanged-evidence retry

`RequiresRegeneratePreview_RetryRejectsWithNoMutation_ForImmutableEvidenceBlock` and `OperationalSupportRequired_RetryRejectsWithNoMutation_ForSafetyBlock` both prove **Option A** (the prompt's own preferred option): a typed rejection (`LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED` / `LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED`) with a full before/after durable-state snapshot equality check — zero mutation, zero aggregate version advancement, zero new `LongHorizonBlockRetryRecord`.

## 19. New-evidence retry

Not applicable — no new-evidence path exists to test (§6, §12, §13).

## 20. Reassessment operation

**Not implemented as a separate route.** Given retry and reassessment collapse to the same meaningful operation once retry is properly gated by recovery classification (retry now *is* the reassessment: it checks current durable state and either restores eligibility or explains why it can't), a separate `POST .../reassess` would duplicate `POST .../retry`'s new logic with no behavioral difference. Documented as a deliberate non-addition rather than an oversight.

## 21. Block-category transitions

No arbitrary block-category replacement is possible: `SaveBlockAsync`/`SaveRetryRestorationAsync` (both existing, unmodified) are the only writers of `CurrentBlockedInternalReasonCode`/lifecycle state, and this phase adds no new writer. One residual, explicitly flagged gap: Phase 4L.4A's activation service currently maps a null `ValidatedLoad` on the Runway/Core JIT boundary-handoff path to `LongHorizonReadStateCorruptException` (a 409, not a durable Block record at all) rather than a clean typed Block — meaning a genuine `ValidatedLongRunEvidenceUnavailable`-equivalent condition reached via that specific path today produces no block record to classify, retry against, or recover from at all. This is a known, separate defect surfaced by this phase's investigation, not fixed here (fixing it would mean changing Phase 4L.4A's activation-service branching, out of this phase's narrow scope) — tracked explicitly in §33/governance as a next-phase item.

## 22. Idempotency

Proven for the two real transition-adjacent scenarios: repeated rejection of an ineligible retry (`RepeatedRetryAgainstImmutableEvidence...`, zero duplicate records across two calls) and the one real eligible-restoration path (`RecoverableWithElapsedCalendarTime...`, one `RetryRestored` record, `NumericPending` transition). Evidence-submission and reassessment-specific idempotency (Part 19's items 16–23, 28–29) are not applicable — no such operations exist.

## 23. Concurrency

`ConcurrentRetryAgainstImmutableEvidence_BothRejected_NoPartialMutation` proves two simultaneous retry requests against an ineligible block **both** reject (not "one wins, one loses" — neither succeeds, since neither should), with zero durable mutation from either. Evidence-vs-retry, reassessment-vs-activation, and cancellation-vs-evidence races (Part 20's other items) are not applicable — no evidence-submission or reassessment operation exists; cancellation-vs-retry is unchanged from Phase 4L.4B's own proof (the same `TrainingPlans` row lock).

## 24. Rollback and acknowledgement loss

Not re-tested with a new failure-injection scenario in this phase: for the (now much narrower) `RecoverableWithElapsedCalendarTime` path that still reaches `SaveRetryRestorationAsync`, Phase 4L.4B's own rollback proof (2 real-Postgres pre-commit failpoints) already covers the exact same repository code path, unmodified by this phase. For every other class, retry now returns before any write is attempted — there is nothing to roll back, which is itself the correctness property this phase establishes (§18's before/after snapshot equality *is* the rollback-safety proof for the ineligible paths, achieved architecturally rather than by failure injection).

## 25. Authorization

Unchanged from Phase 4L.4B (same route, same `ICurrentUserAccessor`-derived ownership, same non-disclosing cross-user 404). Not re-tested in this phase; no authorization-relevant code changed.

## 26. Public errors

| Exception | HTTP | Code |
|---|---|---|
| `LongHorizonRegeneratePreviewRequiredException` | 409 | `LONG_HORIZON_REGENERATE_PREVIEW_REQUIRED` |
| `LongHorizonOperationalSupportRequiredException` | 409 | `LONG_HORIZON_OPERATIONAL_SUPPORT_REQUIRED` |

Reuses existing `LONG_HORIZON_NO_BLOCKED_BOUNDARY` (409), `LONG_HORIZON_RETRY_NOT_ELIGIBLE` (422 — now also covers the strictly-later-checkpoint-date rejection for the one still-eligible class), `LONG_HORIZON_CONTINUATION_VERSION_UNSUPPORTED` (422), `LONG_HORIZON_ACTIVE_PLAN_NOT_FOUND` (404) unmodified. No internal checkpoint code, evidence fingerprint, resolver trace, SQLSTATE, or context identity is ever exposed — confirmed by the existing `GlobalExceptionHandler` convention (unmodified) and the new tests' explicit `DoesNotContain` assertions against raw internal reason-code strings in response bodies.

## 27. Swagger

`LongHorizonRecoveryRequirement` now appears in `swagger.json` (auto-generated from the new public enum type reachable via `LongHorizonActivePlanSummaryResponse`/Home). No dedicated new example was added for the recovery-rejection error bodies (they use the existing generic `ApiErrorResponse` envelope, already documented). No reassessment/evidence route exists to document (§12, §20).

## 28. Observability

One new log line: "Long-Horizon retry rejected: durable evidence cannot change" with `PlanId`, `ReasonCode`, `RecoveryClass` — no raw evidence, no medical/safety detail, no internal payload. The rest of Phase 4L.4B's logging (requested/restored/ineligible) is unchanged.

## 29. Migration

None. `LongHorizonRecoveryRequirement`/`RecoveryRequirement`/`BlockedPublicReasonCategory` are all computed at read time from existing, unmodified columns (`CurrentBlockedInternalReasonCode`/`CurrentBlockedPublicReasonCategory` on `LongHorizonRollingPlanStates`, already added by Phase 4L.2's own migration). No static `TrainingDay` persistence was touched.

## 30. Static/Habit compatibility

No static, Habit, preview, confirmation, or non-Long-Horizon route/DTO was touched beyond the two new exception mappings in `GlobalExceptionHandler` (additive, no existing mapping changed) and the two new nullable fields on `LongHorizonActivePlanSummaryResponse` (additive, no existing field changed or removed). Full regression in §34 confirms zero impact.

## 31. Flutter readiness

No Flutter code changed. Updated mapping (supersedes Phase 4L.4B's own, which assumed capabilities this phase found don't exist):

- `CalendarWindowPending` → explain the plan is waiting for the current window's real calendar days to elapse; a retry/recheck action is meaningful (though likely not until weeks/months later for a freshly confirmed plan — set expectations accordingly, don't imply "try again in a minute").
- `RegeneratePreviewRequired` → explain the current plan cannot continue; offer the existing generate-new-plan flow (cancel current plan, then the existing preview/confirm flow) — no new UI capability needed, this is the existing plan-creation flow reused.
- `OperationalSupportRequired` → generic support/error state; no retry or evidence action offered.
- No `NewTrainingEvidenceRequired`/`ProfileEvidenceRefreshRequired`/`RecoveryWindowRequired` states exist to map — they were not added (§10).

## 32. Public leakage

Extended reflection coverage is unnecessary beyond Phase 4L.4B's existing `LongHorizonRetryContinuationResponse` guard (unchanged) — the new fields live on the already-guarded `LongHorizonHomeResponse`/`LongHorizonActivePlanSummaryResponse` graph (Phase 4L.4's own `PublicContractGraphs_DoNotExposeRollingPersistenceOrInternalAuthority` test already walks this graph and continues to pass with the two new fields added, confirmed in the full regression run, §34). `Home_ExposesRecoveryRequirementAndBlockedReasonCategory_WhenBlocked` additionally asserts the raw internal reason-code string never appears in the serialized Home response.

## 33. Governance

`TD-LONG-HORIZON-BLOCKED-RECOVERY-NEW-EVIDENCE-REASSESSMENT-001` — status **CLOSED** under Outcome B: the product has explicitly declared (via this phase's implementation) that public retry no longer implies recoverability, and every currently reachable block category has a deterministic, honest public recovery requirement. This closes the meaningful-recovery *policy* question. It does **not** close two residual, explicitly separate items, both newly surfaced by this phase's investigation and tracked as follow-up: (a) the JIT-boundary null-`ValidatedLoad` → `CorruptState` miscategorization noted in §21, and (b) `ValidatedLongRunEvidenceUnavailable`/other evidence-content-based blocks remain independently unreachable through the public activation eligibility gate given today's realistic future-dated plan horizons (only verified by direct seeding, not by a natural public flow) — both recorded as open follow-up in the governance ledger, not silently dropped.

Recorded in `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md` (the repository's real append-only ledger, matching the convention Phase 4L.4A/4L.4B both confirmed). New aggregate after this phase: **60 risks, 16 OPEN, 44 CLOSED**. `TD-LONG-HORIZON-PUBLIC-RETRY-ACTIVATION-SHAPE-RACE-COMPLETION-001` (Phase 4L.4B) stays **OPEN** — its own lifecycle-shape/race-breadth gaps are unrelated to and unresolved by this phase. `TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001` (Phase 4L.4A) stays **partially closed**.

## 34. Tests

- New: `LongHorizonBlockRecoveryClassificationTests.cs` — **24/24 passed** (pure, DB-free: all 19 reason codes + 1 unknown-code default classify deterministically; corruption/contradiction and permanent-infeasibility reasons are never retry-eligible).
- New/rewritten: `LongHorizonRetryContinuationTests.cs` — **13/13 passed** (no-blocked-boundary, unsupported version, no-active-plan 404, activation-while-blocked, Home recovery-state exposure, the one real `RecoverableWithElapsedCalendarTime` restoration path including the too-soon rejection, `RequiresRegeneratePreview` rejection with full state-snapshot equality, `OperationalSupportRequired` rejection, repeated-rejection no-churn, concurrent-rejection no-partial-mutation, the real cancel+regenerate-preview recovery path end-to-end, leakage guard, Swagger route/enum presence).
- Phase 4L.4A activation regression (`LongHorizonExplicitNextWindowActivationTests`): **10/10 passed**, unaffected.
- Full Long-Horizon regression: **895/895 passed**, 0 failed, 0 skipped (replaces Phase 4L.4B's 11 now-superseded retry tests with 37 new tests: 868 − 11 + 37 + 1 = 895).
- Full backend integration suite: **3,111/3,111 passed**, 0 failed, 0 skipped (prior baseline 3,084; net +27 tests — zero regressions anywhere in the backend).
- Plan-catalog suite: not re-run this phase (untouched; prior baseline 1,249/1,249 confirmed unaffected by two consecutive prior phases).

## 35. Flutter/background status

Unchanged: no Flutter code, no hosted service, no timer, no queue, no automatic or background activation.

## 36. Final classification

`LONG_HORIZON_BLOCKED_RECOVERY_REQUIRES_REGENERATE_PREVIEW_PLAN_TERMINATION_OR_OPERATIONAL_SUPPORT` and `LONG_HORIZON_PUBLIC_RETRY_NO_LONGER_IMPLIES_RECOVERABILITY_WHEN_THE_AUTHORITATIVE_HISTORICAL_EVIDENCE_CANNOT_CHANGE` and `LONG_HORIZON_EACH_BLOCK_CATEGORY_NOW_HAS_AN_EXPLICIT_PUBLIC_RECOVERY_REQUIREMENT_WITH_NO_MEANINGLESS_STATE_CHURN`.

## 37. Exact next phase

Two candidates, either reasonable: **Phase 4L.4D — Remaining Public Activation Lifecycle Shapes and Cross-Operation Race Completion** (the prompt's own recommendation, closing Phase 4L.4B's still-open shape/race breadth gaps), or a narrower **Phase 4L.4C-1 — JIT Boundary Block Classification Fix**, closing the specific residual gap this phase surfaced in §21/§33(a) (the Runway/Core JIT boundary's null-`ValidatedLoad` currently produces `LongHorizonReadStateCorruptException` instead of a classifiable, retryable-or-not Block record) before building further on top of an activation path that has a known miscategorized error case. Recommend the latter first, since it is small, concrete, and directly informed by this phase's own investigation.
