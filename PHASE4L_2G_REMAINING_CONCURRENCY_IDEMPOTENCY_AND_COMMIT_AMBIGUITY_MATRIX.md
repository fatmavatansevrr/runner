# Phase 4L.2G — Remaining Concurrency, Idempotency and Commit-Ambiguity Matrix

## 1. Executive result

`LONG_HORIZON_REMAINING_CONCURRENCY_IDEMPOTENCY_AND_COMMIT_AMBIGUITY_MATRIX_COMPLETED`. Real PostgreSQL evidence closes the final dark persistence blocker without public activation.

## 2. Final blocker inherited from Phase 4L.2B

4L.2F-A closed rollback and provider-constraint coverage. The remaining parent scope was simultaneous ownership, replay versus genuinely new work, acknowledgement loss, stale writers and cross-plan isolation. This phase closes that scope.

## 3. Scope and exclusions

Scope is `TEN_K__4D__INTERMEDIATE v10` dark rolling persistence. No numeric, calendar, evidence, direction, target-lock, Runway, Core, refresh or retry formula changed. Phase 4L.3, confirmation, preview, API, UI and background work are excluded.

## 4. Concurrency authority inventory

| Operation | Authority | Identity/lookup | Safe loser |
|---|---|---|---|
| Mixed/Core/terminal activation | Combined `xmin` + activation unique key | plan + operation/window + context | replay, concurrency, or internal 23505 |
| Embedded checkpoint | Same activation transaction + checkpoint unique scope | plan + date + source range | activation loser outcome |
| Core refresh | `xmin` + plan/context-version + activation ownership | plan + as-of/evidence-derived context | stale/concurrency/unique conflict |
| Block | `xmin` + application lookup | plan + Block + range start + checkpoint date | replay/concurrency |
| Retry | lifecycle + `xmin` + application lookup | plan + Retry + range start + checkpoint date | replay/concurrency/lifecycle rejection |

No random GUID or wall-clock-now value is lookup authority.

## 5. Real PostgreSQL race standard

Each coordinated race uses two fresh `AppDbContext` instances, two explicitly-held Npgsql connections, distinct `ContextId` and `pg_backend_pid()` values, production repositories and a third fresh reload. No EF InMemory, mock, shared context or application lock is used.

## 6. Race harness

`LongHorizonConcurrentOperationHarness` installs a test-only `ILongHorizonPersistenceConstraintMutation` that pauses both contenders immediately before the existing authoritative `SaveChangesAsync`. The barrier selects no winner; PostgreSQL/EF ownership does.

## 7. Mixed activation races

Horizon 25 (1 Runway+3 Core), 26 (2+2) and 27 (3+1) each produce one durable activation record, one runway/context ownership result, one session set, one aggregate advancement and no half commit.

## 8. Core-only races

Same-key and different-key contenders for the same range have one success. The loser is safely replayed/rejected, sessions remain unique, and the following Core range activates separately.

## 9. Core-refresh races

Identical and different valid evidence candidates starting from the same V1/`xmin` create one V2 and one supersession link. V1 history remains byte-stable; no overlapping future ownership or stale automatic V3 survives.

## 10. Block races

Same-reason and different-reason attempts leave one Block row, one authoritative reason, Blocked lifecycle and no executable sessions. Reasons are not merged.

## 11. Retry races

Same and different decision identities leave one Blocked→Pending transition and one retry lineage row. Direct activation is not part of retry.

## 12. Activation versus retry

If stale activation reaches persistence while Blocked, lifecycle validation returns `IntegrityViolation`. If retry commits first, the pre-block activation returns `ConcurrencyConflict` from stale `xmin`. Neither schedule creates a Blocked→Activated transition or sessions.

## 13. Block versus activation

Commit order may select either a fully Activated state with sessions/no block or a fully Blocked state with block/no sessions. Split state is forbidden and not observed; no priority lock was introduced.

## 14. Checkpoint/refresh versus activation

The repository has no standalone checkpoint save: checkpoint input is part of `SaveActivationSuccessAsync`, so checkpoint/context/window commit atomically under the same `xmin`. Refresh likewise uses combined refresh-plus-activation authority. No fictitious standalone operation was added.

## 15. Checkpoint races

Same-scope checkpoint deduplication is the existing `(PlanStateId, AsOfDate, SourceWindowStartWeek)` ownership inside activation. A later checkpoint is persisted only with a genuinely later activation request after fresh reload.

## 16. Terminal races

Two preterminal contenders at horizon 25 create one `[25,25]` activation. No duplicate final sessions, no week beyond `TotalWeeks`, and no Pending/Blocked suffix remain.

## 17. Exact replay matrix

Activation replay returns `IdempotentReplay`; block/retry replay use their exact tuples; refresh replay is stale-safe and reconstructs the committed active context. Checkpoint and terminal replay inherit activation identity. No replay reports a second business transition.

## 18. Next-operation distinction

After replay, the next Core range advances with a new window identity. Retry replay does not swallow later activation. A natural 25-week plan proves V1→V2→V3 with a later context and unchanged V2-owned history.

## 19. Idempotency-key audit

Activation keys include plan, operation/window identity and context sequence. Block/retry lookup includes plan, event type, range start and checkpoint date. Refresh output is scoped by plan, expected aggregate version, date and evidence-derived context. Changed plan/range/context/date/evidence is distinct.

## 20. Activation-lookup precision

Activation lookup is exact `IdempotencyKey`, never “latest activation for plan.” Block/retry lookups use exact operation tuples. Plan-scoped context identity prevents cross-plan matches. Window B is not resolved to window A.

## 21. Commit-success/acknowledgement-loss

`AfterCommitBeforeAcknowledgement` proves committed mixed/Core activation, block and retry survive context disposal; fresh reload discovers ownership; exact replay does not advance twice; the next distinct operation still works. Embedded checkpoint/refresh/terminal use the same activation commit boundary, not separate transactions.

## 22. Stale-context behavior

Stale writers produce concurrency, replay, safe unique conflict or lifecycle rejection. The failed context is discarded. A fresh context reconstructs the winner before explicit retry/new work; tracked state is never silently refreshed and reused.

## 23. Cross-plan isolation

Two plans with identical ranges and local context sequences commit in parallel. Their plan-scoped context IDs, activation rows and sessions remain separate; one plan cannot roll back or corrupt the other.

## 24. Multi-refresh behavior

The pilot horizon naturally supports sequential V1→V2→V3. V3 has a later sequence, owns only later Pending work, and does not rewrite the captured V2 historical fingerprint. Concurrent V2 candidates are one-winner safe.

## 25. Unique-constraint/xmin interaction

Race order may surface `ConcurrencyConflict`, `IdempotentReplay`, or internal PostgreSQL 23505. All are allowed loser outcomes; none is success, no partial write survives, and the winner remains reconstructable. Provider details remain internal.

## 26. Retryability classification

`IdempotentReplay` means committed ownership is discoverable; `ConcurrencyConflict` means reload then re-evaluate; `IntegrityViolation` covers lifecycle/permanent invalid state; 23505 is an internal committed/conflicting ownership result; reconstruction lineage failures remain non-retryable corruption. No public classifier/API mapping was necessary.

## 27. Final complete-lifecycle acceptance

The evidence composes the existing restart-between-windows lifecycle with exact replay, post-commit loss, a stale concurrent loser, future Core refresh and terminal completion. Existing 4L.2B-R/2E/2F/2F-A lifecycle and rollback suites remain the supporting full-path proof; no test-only runtime was substituted.

## 28. Corruption versus concurrency

Stale `xmin`, exact ownership and changed lifecycle are normal conflict/replay outcomes. Missing context, broken lineage or activation/session mismatch remains corruption and is not auto-repaired. Concurrent losers are not mislabeled corruption.

## 29. No-formula-change proof

Only integration-test coordination, one test-fixture outcome-preservation adjustment, governance and this record changed. Structural horizon, GE/Runway/Core generation, refresh eligibility, target lock, direction, calendar, checkpoint, block/retry, greedy selection and downward-interpolation status are unchanged.

## 30. Dark integration

The harness and tests are internal. There is no endpoint, public DI registration, API DTO, preview/confirmation route, Home/Calendar/completion integration, Flutter reference or scheduled service.

## 31. Governance

`TD-LONG-HORIZON-REMAINING-CONCURRENCY-IDEMPOTENCY-COMMIT-AMBIGUITY-001` is `CLOSED`. Eight required existing TDs received append-only 4L.2G updates. The Phase 4L.2B parent moves to `CLOSED`; public/confirmation readiness is not closed by this result.

## 32. Tests

Focused matrix: 19/19 passed against configured PostgreSQL. Governance parity/integrity, full Long-Horizon, full backend and full plan-catalog commands and final counts are recorded after final validation below or in the delivery report.

## 33. Public/confirmation/API/Flutter status

Public preview: unwired. Confirmation: unwired. API: unchanged. Home/Calendar: unchanged. Completion handlers: unchanged. Flutter: unchanged. Background jobs: none added.

## 34. Final classification

`LONG_HORIZON_EVERY_CONCURRENT_MIXED_CORE_REFRESH_BLOCK_RETRY_CHECKPOINT_AND_TERMINAL_OPERATION_HAS_ONE_DURABLE_WINNER_WITH_NO_SPLIT_OR_DUPLICATE_OWNERSHIP`.

`LONG_HORIZON_EXACT_REPLAY_NEVER_CREATES_A_SECOND_TRANSITION_WHILE_THE_NEXT_DISTINCT_OPERATION_RETAINS_ITS_OWN_DETERMINISTIC_AUTHORITY`.

`LONG_HORIZON_COMMIT_SUCCESS_WITH_ACKNOWLEDGEMENT_LOSS_RECOVERS_FROM_FRESH_POSTGRESQL_STATE_WITHOUT_DOUBLE_VERSION_ADVANCEMENT`.

## 35. Exact next phase

Recommended next phase: **Phase 4L.3 — Long-Horizon Confirmation and Public Preview Wiring**. It was not begun here and still requires explicit authorization.
