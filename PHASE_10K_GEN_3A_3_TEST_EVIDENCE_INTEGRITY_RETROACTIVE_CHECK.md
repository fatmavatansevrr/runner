# PHASE 10K-GEN.3A.3 — Test Evidence Integrity Retroactive Check

## 1. Audit objective

This is an audit/revalidation only. It asks whether the absent testhost copy of `xunit.runner.json` weakened historical Adaptation V1 concurrency/idempotency evidence. No product, catalog, adaptation, schema, or rollout behavior was changed.

## 2. Git-history evidence

- `git log --all -- backend/RunningApp.IntegrationTests/xunit.runner.json` returns no commit. The file is currently untracked. Its filesystem creation/last-write timestamp is 2026-08-11 12:31:22 +03:00.
- The integration-test project was introduced by commit `2dd5d6f4b31c6daa864bcd77dcc02b3e83383a88` on 2026-07-01 16:40:56 +03:00. No runner-file item metadata existed in that committed project.
- The only later committed project change is `a0ca152e17e6f832a1c5b48c3b0f050643b93ac0` on 2026-07-12 16:55:39 +03:00; it added EF InMemory/Application references, not runner copying.
- The current uncommitted project diff adds `<None Update="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />`. No `CopyToPublishDirectory` metadata exists and no shared `Directory.Build.props/targets` supplies an alternative copy.
- `PHASE4M_4A...` says the file/config was established during the 4M.3 confirmation pass. `PHASE_10K_GEN_3A_1...` later proves it had not reached testhost and records the copy fix. Because both the runner file, fix, and 4M reports are uncommitted, exact commit provenance cannot be reconstructed from Git.
- There is no evidence of an earlier correctly copied period.

Historical configuration classification: `HISTORY_INCONCLUSIVE`.

The strongest non-Git chronology is: runner configuration authored during 4M.3 (2026-08-11), asserted in later 4M reports, but not actually copied until GEN.3A.1 on 2026-08-12. This strongly suggests 4M.3–4M.5D exposure, but cannot satisfy an exact-commit classification.

## 3. Adaptation V1 exposure window

### ADAPTATION_V1_XUNIT_EXPOSURE_TABLE

| Phase | Date/commit | Historical backend count | Misconfiguration overlap? | Concurrency-sensitive evidence? | Revalidation required? |
|---|---|---:|---|---|---|
| 4M.1 | 2026-08-10; uncommitted report | 3229/3229 | File apparently not yet introduced; Git-inconclusive | Pure policies only; concurrency deferred | No |
| 4M.2 | 2026-08-10; uncommitted report | 3253/3253 final | Pre-file period; global default parallelism still applied | Yes: duplicate trigger, row lock, unique constraints, real race | Yes, conservatively |
| 4M.3 | 2026-08-11; uncommitted report | exact full count not recorded in document | Strong filesystem/report overlap | Yes: runtime replay/orchestration and shared mock-user DB | Yes |
| 4M.4/4A | 2026-08-11; uncommitted report | exact full count deferred to chat; 74/74 activation subset | Strong overlap | Yes: activation/checkpoint/replay | Yes |
| 4M.4B–4M.4B.2C | 2026-08-11/12; uncommitted reports | full counts mostly deferred; LongHorizon 1092/1092 then 1096/1096 | Strong overlap | Yes: concurrent Reduce, replay, Block/activation | Yes |
| 4M.5 | 2026-08-12; uncommitted report | 3337/3337 | Strong overlap | Yes | Yes |
| 4M.5C | 2026-08-12; uncommitted report | 3351/3351 | Strong overlap | Yes: distinct idempotency keys/no double anchor | Yes |
| 4M.5D | 2026-08-12; uncommitted report | 3351/3351 | Strong overlap | Yes: accepted final concurrency/replay inventory | Yes |

Because history is incomplete, this audit conservatively treated every material 4M.2+ claim as exposed.

## 4. Concurrency/idempotency critical-test inventory

| Material proof | Classification | Persisted-state rationale |
|---|---|---|
| `ScheduleRepairPersistenceTests.Concurrency_TwoSimultaneousCallsForSameTrigger_ExactlyOneCommittedDecision` | `CONCURRENCY_EVIDENCE_CRITICAL` | Same plan/trigger; two physical contexts race; winner/loser and exact DB cardinality asserted |
| Schedule-repair replay/duplicate/migration uniqueness tests | `POTENTIAL_FALSE_POSITIVE` historically; now revalidated | Shared Postgres could have been reset by unrelated collections, although fresh GUID identities substantially reduce accidental satisfaction |
| `LongHorizonNumericAnchorMaterializationE2ETests.ConcurrentActivation_WithRealReduceDecision_HasExactlyOneWinner_NoDoubleReduction` | `CONCURRENCY_EVIDENCE_CRITICAL` | Same persisted rolling aggregate, two HTTP clients, exact activation-record cardinality |
| Runtime orchestration duplicate calls and NotToday replay | `POTENTIAL_FALSE_POSITIVE` historically; now revalidated | Shared mock user/global reset makes cross-collection interference theoretically capable of changing observed active state |
| Three-window anchor, retry, block-recovery, JIT cross-operation races | `CONCURRENCY_EVIDENCE_CRITICAL` as a set | Claims depend on durable aggregate version/idempotency/chronology across multiple operations |
| Pure decision/weekly aggregation tests | `NO_SHARED_STATE_RISK` | No shared database or host state |
| Sequential stale-target/replay tests using fresh GUID plans | `POTENTIAL_FALSE_NEGATIVE_ONLY` in most cases | Unrelated reset could delete fixtures and fail the test; it is unlikely to manufacture the exact fresh-GUID rows needed to pass |

## 5. False-positive risk analysis

`ScheduleRepairPersistenceTests` creates every plan/week/session with fresh GUIDs and deletes narrowly by its own plan IDs. Its critical race is largely isolation-independent with respect to accidental row creation, but a concurrent broad reset could cause false failures. Classification after inspection: `ISOLATION_INDEPENDENT` for exact winner/cardinality semantics, with historical false-negative interference risk.

HTTP adaptation/activation tests use the shared `mock-user-001`, the common Postgres schema, `/api/v1/testing/reset`, and globally selected active-plan state. Parallel unrelated collections could reset or replace that logical state. Their historical proof strength was isolation-dependent. Corrected runs make their status `ISOLATION_DEPENDENT_BUT_REVALIDATED`; none remains unresolved.

No static trigger/plan GUID is reused by the direct persistence race. HTTP flows derive current plan/session IDs from the newly confirmed plan, but share the global user and reset boundary.

## 6. Clean-configuration revalidation

- Integration project build: PASS, 0 warnings, 0 errors.
- Testhost output `bin/Debug/net9.0/xunit.runner.json`: present.
- Effective content: `parallelizeTestCollections=false`.
- Exact schedule-repair race alone: 1/1 PASS, 6s.
- Exact Reduce activation race alone: 1/1 PASS, 11s.
- `ScheduleRepairPersistenceTests` class: 25/25 PASS, 7s.
- `LongHorizonNumericAnchorMaterializationE2ETests` class: 2/2 PASS, 15s.
- Authoritative 4M.5D Adaptation V1 set, run 1: 279/279 PASS, 0 failed/skipped, 3m27s.
- Same set, run 2: 279/279 PASS, 0 failed/skipped, 3m26s.
- Current corrected-config monolithic run from GEN.3B: 3423/3423 PASS, 0 failed/skipped, 18m47s.

No expectation was changed and no failed invariant was repaired.

## 7. Intentional in-test concurrency proof

The runner option serializes test collections only. It does not alter tasks created inside a test.

- Schedule-repair race creates two `Task.Run` operations, each opens an independent `AppDbContext`/connection, and joins them with `Task.WhenAll`. It asserts exactly one commit, one replay/conflict, one decision record, one replacement, and one superseded EASY.
- Reduce activation race creates a second HTTP client, issues two activation requests in one `Task.WhenAll`, then asserts one HTTP success, one loser, and one activation-window record using a fresh DbContext.

For both: `INTENTIONAL_IN_TEST_CONCURRENCY_PRESERVED`.

The repeated 279-test set includes the broader activation, retry, block-recovery, JIT cross-operation, schedule-repair, and replay inventory; collection serialization did not sequentialize their internal calls.

## 8. Historical 4M numeric test-count inventory

### HISTORICAL_4M_TEST_COUNT_TABLE

| Phase | Reported discovered/executed | Passed | Failed | Skipped | Count record still valid? | Concurrency proof status |
|---|---:|---:|---:|---:|---|---|
| 4M.1 | 3229 | 3229 | 0 | 0 | Yes | Isolation-independent pure evidence |
| 4M.2 initial | 3253 | 3252 | 1 | not stated | Yes | Superseded by documented fix/final run |
| 4M.2 final | 3253 | 3253 | 0 | 0 | Yes | Revalidated |
| 4M.3 | not recorded in document | not recorded | not recorded | not recorded | No numeric claim to validate | Revalidated by current critical set |
| 4M.4A | full count deferred; targeted 74 | 74 targeted | 0 | 0 | Targeted record remains valid | Revalidated |
| 4M.4B.2A | LongHorizon 1092 | 1092 | 0 | not stated | Yes | Revalidated |
| 4M.4B.2B | LongHorizon 1095 | 1094 | 1 intentional | not stated | Yes | Later closure supersedes intentional failure |
| 4M.4B.2C | LongHorizon 1096 | 1096 | 0 | not stated | Yes | Revalidated |
| 4M.5 | 3337 | 3337 | 0 | not stated | Yes | Revalidated |
| 4M.5C | 3351 | 3351 | 0 | not stated | Yes | Revalidated |
| 4M.5D | 3351 | 3351 | 0 | 0 | Yes | Revalidated |

The example counts 3254/3254 and 3311/3311 do not appear in the authoritative 4M repository reports inspected, so they are not attributed or invented.

Historical count classification: `HISTORICAL_NUMERIC_TEST_COUNTS_REMAIN_VALID`. This preserves literal recorded outcomes while separately correcting the interpretation of collection-isolation strength.

## 9. Changed evidence and production-code impact

No accepted concurrency/idempotency invariant changed. No production defect was found. No production code, product policy, adaptation algorithm, schema, catalog, or rollout file was changed in this audit.

## 10. Remaining governance debt

- Commit provenance is irrecoverable for the untracked runner file, uncommitted csproj fix, and uncommitted 4M reports. Future evidence-bearing infrastructure and reports should be committed atomically.
- Add a permanent test/build assertion that the runner configuration is present in testhost output and has the intended value; this audit did not add infrastructure because current physical-output verification and reruns were sufficient.
- Phase reports should embed TRX paths/count fields rather than defer full-suite counts to chat.

## 11. Final classifications

- Historical configuration: `HISTORY_INCONCLUSIVE`
- Adaptation V1 evidence integrity: `ADAPTATION_V1_CONCURRENCY_EVIDENCE_REVALIDATED`
- Historical counts: `HISTORICAL_NUMERIC_TEST_COUNTS_REMAIN_VALID`

No invariant is reopened.

## 12. Files inspected and changed

Inspected: Git history for the integration csproj/runner path; current csproj, runner output/config, collection/factory/reset infrastructure; all listed 4M.1–4M.5D reports; Adaptation V1 tests and TRX results.

Changed: this documentation file only.
