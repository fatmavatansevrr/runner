# Phase 4M.5D — TEN_K / INTERMEDIATE / 4D Adaptation V1 Final Re-acceptance

## 1. Canonical source

Sole product-behavior authority: `appsel-adaptation-v1-canonical-spec (2)-rev 5.md`, especially §§5, 6, 7, and 7a. Revision 4/4.1 numeric-anchor semantics are retained only through Rev5. `PHASE4M_5C_MULTI_WEEK_AGGREGATION_IMPLEMENTATION.md` and its corrected-fixture evidence are supporting implementation evidence, not competing product authority. Pre-Rev5 Phase 4M.5 flat-window results are historical only.

## 2. Scope

Final verification/closure of Adaptation V1 for `TEN_K / INTERMEDIATE / 4D`. No generalization, migration, product behavior, commit, or push was performed. Current code and persisted-source behavior were re-audited; Rev5-specific and original journey suites were rerun.

## 3. Rebuilt production authority map

| # | Authority (exact type) | Exact file | Responsibility | Competing production implementation? |
|---|---|---|---|---|
| 1 | `ReasonClassificationPolicy` | `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/ReasonClassificationPolicy.cs` | Canonical reason meaning: safety, illness, operational | No |
| 2 | `RuntimeNotTodayReasonMapper` | `.../Adaptation/RuntimeNotTodayReasonMapper.cs` | Fail-closed runtime string-to-reason mapping | No |
| 3 | `AdaptationSessionRoleResolver` | `.../Adaptation/AdaptationSessionRoleResolver.cs` | Persisted role-to-adaptation role normalization | No |
| 4 | `CandidateSelectionPolicy` | `.../Adaptation/CandidateSelectionPolicy.cs` | Deterministic earliest valid candidate | No |
| 5 | `ScheduleRepairPolicy` | `.../Adaptation/ScheduleRepairPolicy.cs` | Skip/reschedule/substitute decision matrix | No |
| 6 | `ScheduleRepairPersistenceService` | `.../Adaptation/ScheduleRepairPersistenceService.cs` | Transactional repair, lineage, supersession, audit, replay/concurrency | No |
| 7 | **`WeeklyWindowPartitioner` (Rev5 new)** | `.../Adaptation/WeeklyLoadDecisionAggregation.cs` | Structural-week evidence buckets using persisted week identity | No; it partitions only |
| 8 | **`WeeklyWindowPartitioner` (Rev5 new)** | same | Original-root week attribution by `AdaptedFromSessionId` ancestry | No; it does not interpret completion |
| 9 | `WindowExecutionSummaryBuilder` | `.../Adaptation/WindowExecutionSummaryBuilder.cs` | Sole logical expectation, lineage completion, denominator, role and safety summary | No |
| 10 | `NextWindowLoadDecisionPolicy` | `.../Adaptation/NextWindowLoadDecisionPolicy.cs` | One-week role-aware P/M/R plus weekly safety result | No |
| 11 | **`WeeklyLoadDecisionAggregator` (Rev5 new)** | `.../Adaptation/WeeklyLoadDecisionAggregation.cs` | B1 worst-week-wins (`R < M < P`) | No |
| 12 | `WeeklyLoadDecisionAggregator` | same | OR of weekly safety booleans, independently of B1 | No |
| 13 | `LongHorizonRollingWindowActivationService.PriorAnchor` | `backend/RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs` | Re-authorizes the latest validated load as prior-checkpoint evidence | No |
| 14 | `NextWindowNumericAnchorSelector` | `.../Adaptation/NextWindowNumericAnchorSelector.cs` | One final P/M/R-to-numeric-anchor selection | No |
| 15 | `LongHorizonRollingCheckpointRuntime` / `ExistingLongHorizonGeWindowMaterializer` | `.../RollingActivation/LongHorizonRollingCheckpointRuntime.cs`, `.../LongHorizonRollingInitialActivationRuntime.cs` | GE catalog-window progression/materialization | No adaptation duplicate |
| 16 | `TenKPreparationRunwayProgressionLoader`, `TenKPreparationRunwayCoreGenerator`, `TenKPreparationRunwayProgressionPolicyFactory` | `.../PreparationRunwayOrchestration/TenKPreparationRunwayComponentAdapters.cs`, `.../TenKPreparationRunwayProgressionPolicyFactory.cs` | TEN_K runway/core progression authority | No |
| 17 | `LongHorizonRollingJitCompositionOrchestrator` / `LongHorizonRollingRestartContinuationService` | `.../RollingActivation/LongHorizonRollingJitCompositionOrchestrator.cs`, `.../RollingActivation/Persistence/LongHorizonRollingRestartContinuationService.cs` | JIT composition and continuation handoff | No |
| 18 | `LongHorizonRollingStateRepository`, `LongHorizonRollingActivationPersistenceAdapter`, `LongHorizonRollingBlockPersistenceAdapter` | `.../RollingActivation/Persistence/*.cs` | Window/block persistence and `IsBlock` propagation | No |
| 19 | `LongHorizonActiveReadModelProvider` | `backend/RunningApp.Application/Services/LongHorizonActiveReadModelProvider.cs` | Home/calendar/day-detail active session mapping and readiness | No |

Special check: the two Rev5 authorities do not duplicate the builder, policy, or selector. Partitioning only chooses evidence ownership; the unchanged builder interprets it; the unchanged policy evaluates each week; B1 collapses those results; the selector consumes only the single final result and window-level numeric evidence.

## 4. Illness-on-KEY historical correction

`LongHorizonNextWindowDecisionActivationTests.IllnessOnKey_OtherFifteenCompleted_NoRepair_Maintain_SafetyFalse` passed through real HTTP and fresh Postgres state. The affected original week is 3/4 with KEY absent: Maintain. The other weeks are 4/4: ProgressAsPlanned. B1 is Maintain. Raw source outcome/reason remains persisted, illness produces no repair, `SafetyReviewRequired=false`, and no false/replayed mutation occurs.

```text
Original pre-Rev5 Phase 4M.5 behavior:
ProgressAsPlanned

Post-Rev5 canonical behavior:
Maintain
```

## 5. All-complete result

PASS — every structural week is 4/4/P; B1=P, safety false, current validated evidence drives normal progression, the next range genuinely advances in fresh DB, and replay is idempotent.

## 6. Easy NotToday result

PASS — real operational Easy `NotToday` remains `Skip`; no replacement and no false completion. The affected week remains denominator 4, effective 3/4 with only Easy missing, therefore P; B1 reflects the actual weekly set.

## 7. Repaired-priority result

PASS — both reschedule and future-Easy substitution paths retain the source `NotToday`, preserve ancestry, close the original expectation only after replacement completion, do not increase the denominator, and preserve Superseded neutrality. Cross-week replacements remain owned by the original week; the destination does not gain expectation five.

## 8. Illness result

PASS — Section 4 is the canonical regression. Final P/M/R is Maintain, not the historical ProgressAsPlanned.

## 9. Soreness/safety result

PASS — raw `"soreness"` persists; safety mapping blocks repair. Weekly and final safety are true via OR, while B1 independently determines load. Safety neither forces Reduce nor changes the anchor.

## 10. Feasible Maintain result

PASS — real weekly evidence produces M; B1=M; `PriorValidatedCheckpointLoad` is selected verbatim once; chronology and actual materialization advance; fresh DB and distinct idempotency keys prove replay safety.

## 11. Feasible Reduce result

PASS — at least one weekly R makes B1=R. The frozen `min(current evidence, prior anchor)` path remains unchanged, no later progression step is applied, a real reduction materializes, chronology advances, and concurrent/replay coverage proves no double reduction.

## 12. Zero-completion result

PASS — selector tests prove no invented percentage/fallback when both approved anchors are absent; production continuation persists/returns typed `LONG_HORIZON_CONTINUATION_BLOCKED`, does not activate, and leaves chronology unchanged. A prior approved anchor, when present, remains the explicitly canonical Rev4 fallback.

## 13. Infeasible Maintain result

PASS — valid Maintain anchor plus infeasible target prescription produces typed Block without upward clamp, P fallback, or advancement.

## 14. Infeasible Reduce result

PASS — symmetric Reduce infeasibility produces typed Block; no clamp, fake activation, or chronology change.

## 15. 8-Easy regression result

PASS — pure real-DB aggregation fixture has four weeks of 2 Easy completed and KEY/LONG absent: `M/M/M/M -> M`. The old flat-window `ProgressAsPlanned` result cannot occur. The real HTTP production-path companion uses one Long completion only to satisfy the independent JIT evidence prerequisite and returns Maintain; literal zero-Long decision semantics remain proven at the Rev5 aggregation boundary.

## 16. Weekly aggregation matrix

| Weekly decisions | B1 |
|---|---|
| P/P/P/P | P |
| P/M/P/P | M |
| M/M/P/P | M |
| R/P/P/P | R |
| P/P/P/R | R |
| P/M/R/P | R |

PASS — front-loaded and back-loaded Reduce both yield Reduce. B1 is deliberately recency-blind; no majority or recency weighting exists.

## 17. Cross-week lineage result

PASS — reschedule and substitution characterizations place a Week-1 priority root, its `NotToday`, and a physically Week-2 completed descendant in Week 1's evidence bucket. Week 1 stays denominator 4 and resolves role completion through lineage. Week 2 gains no fifth expectation; substituted Easy remains neutral/informational. `WeeklyWindowPartitioner` owns week assignment; `WindowExecutionSummaryBuilder` alone owns logical completion/denominator semantics.

## 18. Multi-window lifecycle

PASS — real P, M, R, and catalog-infeasible Block transitions are covered across multiple windows. Fresh-state assertions cover source/target ranges, activation-record uniqueness, anchor threading, no stale active source, genuine advancement when feasible, and no advancement when blocked.

## 19. Schedule-repair matrix

PASS:

```text
EASY -> Skip
KEY/LONG + (Safety or Illness) -> Skip
otherwise earliest valid empty slot -> RescheduleToEmptySlot
else earliest valid future Easy -> SubstituteFutureEasy
else -> Skip
```

Deterministic earliest, no KEY/LONG substitution, no phase/window crossing, taper freeze, no cascade/makeup, and spacing constraints remain covered. Rev5 did not make candidate selection week-scoped: legal cross-week repair remains possible and is then attributed to the original week for evidence.

## 20. Provenance/denominator result

PASS — original expectations define `ExpectedSessionCount`; descendants add none; Superseded removes none and is neither Completed nor NotToday; successful descendants close their root expectation. The root's persisted week owns that expectation. Production contains no alternative ad-hoc completed-count decision path around the builder.

## 21. Safety orthogonality

PASS — weekly safety is mapped by the unchanged summary/policy and ORed across weeks; weekly decisions are separately B1-aggregated. P+true, M+true, and R+true are characterized in coverage. The selector signature has no safety parameter. Safety does not force Reduce, alter anchors, block by itself, add recovery, or add acknowledgement state.

## 22. Numeric-anchor re-acceptance

PASS — one final B1 `LoadDecision` enters one `NextWindowNumericAnchorSelector.Select` call. P uses current validated load; M uses exact prior validated checkpoint load (or the already-frozen first-checkpoint fallback); R uses the unchanged evidence/prior minimum rule and the zero-completion rule. The selector has no structural-week input or knowledge.

## 23. Rounding sweep

PASS — deterministic real-catalog sweep: 183 valid cases, 94 strict post-materialization ordering deviations, maximum absolute 0.247 km, maximum relative 1.36%, and 0 cases above 1.5%. These observations are not policy constants; only the frozen 1.5% Product Default is enforced. Rev5 changes decision granularity, not numeric materialization.

## 24. Block/no-false-success

PASS — the 4M.4B.2A path persists a JIT Block, propagates `IsBlock`, throws typed blocked behavior, never returns HTTP 200 activated, and never reuses the old range as a fake next window. B1 occurs before composition and does not interfere with routing.

## 25. Read-model result

PASS — Home, Calendar, and training-day detail tests retain source NotToday history and replacement visibility while excluding Superseded sessions from actionability. Fresh reads expose the genuinely active next-window identity. Weekly partitioning is internal to activation evidence and changes no public scheduling projection.

## 26. Concurrency/idempotency

PASS — schedule-repair duplicate trigger/orchestration, concurrent trigger, stale target, and stale trigger coverage remains green. Activation duplicate/concurrent calls, Reduce no-double-application, Maintain replay, and Block retry/no-false-activation remain green. Weekly partition/B1 creates no persisted mutable state and therefore no new race surface.

## 27. Direct multi-week policy invocation audit

Repo-wide production search found exactly one live call:

`backend/RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs:132`

Its argument is a `weekSessions` group from `WeeklyWindowPartitioner.PartitionByStructuralWeekLineage`; there is no production full-window call. Test-only invocations characterize the one-week policy. `WeeklyWindowPartitionerBoundaryTests` also guards against reintroducing `NextWindowLoadDecisionPolicy.Evaluate(windowSummary)`.

## 28. Coverage matrix

| Load-bearing rule | Pure unit | Integration/DB | Real HTTP | Fresh DB | Grade |
|---|---:|---:|---:|---:|---|
| Reason/role mapping | Yes | Yes | Yes | Yes | FULL |
| Repair matrix/candidate order | Yes | Yes | Yes | Yes | FULL |
| Repair persistence/idempotency | Yes | Yes | Yes | Yes | FULL |
| Provenance/denominator | Yes | Yes | Via journey | Yes | FULL |
| Structural-week partitioning | Yes | Real Postgres fixture | Via activation | Yes | FULL |
| Original-week lineage ownership | Yes | Real Postgres fixture | Companion journey | Yes | FULL |
| Weekly summary semantics | Yes | Yes | Via activation | Yes | FULL |
| B1 weekly aggregation | Yes | Yes | Yes | Yes | FULL |
| Recency-blindness | Yes | Yes | Via same path | N/A (pure order) | FULL |
| Safety OR | Yes | Yes | Yes | Yes | FULL |
| Direct multi-week policy invocation forbidden | Structural source guard | N/A | Production call audited | N/A | ADEQUATE |
| One final LoadDecision per activation | Structural/unit | Yes | Yes | Yes | FULL |
| Numeric-anchor handoff | Yes | Yes | Yes | Yes | FULL |
| P/M/R materialization | Yes | Yes | Yes | Yes | FULL |
| Zero/no-anchor Block | Yes | Yes | Yes | Yes | FULL |
| Maintain/Reduce infeasibility | Yes | Yes | Yes | Yes | FULL |
| IsBlock/no false success | Yes | Yes | Yes | Yes | FULL |
| Multi-window chronology | Yes | Yes | Yes | Yes | FULL |
| Read models | Yes | Yes | HTTP/service reads | Yes | FULL |
| Activation concurrency/replay | Yes | Real Postgres | Yes | Yes | FULL |
| Rounding default | Deterministic sweep | Real catalog | N/A | N/A | FULL |

No load-bearing row is MISSING. Rows involving weekly behavior and the anchor handoff are graded from Rev5 tests, not inherited Phase 4M.5 results.

## 29. Durable-snapshot backlog

Confirmed: the final rolling `LoadDecision` and `SafetyReviewRequired` are recomputed from durable session/source truth and returned, not durably snapshotted as a historical checkpoint pair. `LongHorizonAdaptationDecisionRecord` is the schedule-repair audit record, not that missing activation snapshot. Preserve `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` as BACKLOG. No table or migration was added.

## 30. Negative/non-goal confirmation

Confirmed absent from this phase and Rev5 decision path: percentage Reduce, `ReduceBand`, `RecoveryWeek`, percentage adherence thresholds, weekly activation/checkpoint, weekly numeric anchors, majority voting, recency weighting, destination-week lineage, post-generation workout rewriting, phase rollback, automatic race-date mutation, missed-session inference, background activation, safety acknowledgement workflow, and unsupported generalization. No 21K, Beginner, Advanced/Expert, 2D/3D/5D, Habit Adaptation, or Flutter work was performed.

## 31. Files inspected

Canonical Rev5 spec; 4M.5C report; all production files named in Section 3; adaptation tests under `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Adaptation`; lifecycle, persistence, read-model, retry, and concurrency tests; relevant domain entities/migrations; solution/project/test configuration; repo-wide policy call sites.

## 32. Files created

- `PHASE4M_5D_TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_FINAL_REACCEPTANCE.md`

## 33. Files modified

None. Existing user/prior-phase changes were preserved.

## 34. Tests added/changed

None. Existing Rev5 coverage was present, current, and green; the confirmed illness regression was not redundantly duplicated.

## 35. Exact targeted commands/results

```text
dotnet test RunningApp.IntegrationTests --no-restore --filter "FullyQualifiedName~WeeklyLoadDecisionAggregationTests|FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests|FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests|FullyQualifiedName~PlanAdaptationV1DecisionTests|FullyQualifiedName~NextWindowLoadDecisionPolicy|FullyQualifiedName~NextWindowNumericAnchorSelectorTests|FullyQualifiedName~MaintainNotExceedingProgressAsPlannedInvariantTests|FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests|FullyQualifiedName~LongHorizonFirstCheckpointNumericAnchorTests|FullyQualifiedName~LongHorizonNumericAnchorMaterializationE2ETests|FullyQualifiedName~ScheduleRepairPolicyTests|FullyQualifiedName~ScheduleRepairPersistenceTests|FullyQualifiedName~ScheduleRepairRuntimeOrchestratorTests|FullyQualifiedName~ScheduleRepairSupersededAndReadCorrectnessTests|FullyQualifiedName~RuntimeNotTodayReasonMapperTests|FullyQualifiedName~LongHorizonActiveReadAndMutationTests|FullyQualifiedName~LongHorizonExplicitNextWindowActivationTests|FullyQualifiedName~LongHorizonRetryContinuationTests|FullyQualifiedName~LongHorizonBlockRecoveryClassificationTests|FullyQualifiedName~LongHorizonJitBoundaryAndCrossOperationRaceTests|FullyQualifiedName~LongHorizonRemainingConcurrencyTests" --logger "console;verbosity=normal"
=> exit 0; 0 failed; 204.9 s; no retries (aggregate count obscured by verbose DB output, so no count is invented)
```

The targeted set includes Rev5 weekly aggregation and corrected fixture, original journeys, repair, provenance, safety, numeric anchor/rounding, infeasibility/Block, lifecycle, read, and concurrency/idempotency evidence.

## 36. LongHorizon result

```text
dotnet test RunningApp.IntegrationTests --no-restore --filter "FullyQualifiedName~LongHorizon" --logger "console;verbosity=minimal"
=> 1112 passed / 1112 total; 0 failed; 0 skipped; 5m45s; no retries
```

## 37. Full backend regression result

```text
dotnet test RunningApp.sln --no-restore --logger "console;verbosity=minimal"
=> 3351 passed / 3351 total; 0 failed; 0 skipped; 15m49s; no retries
```

## 38. Build/git result

```text
dotnet build RunningApp.sln --no-restore
=> succeeded; 0 warnings; 0 errors; 5.34s

git diff --check
=> exit 0; no whitespace errors (pre-existing LF/CRLF conversion warnings only)

git status --short
=> heavily dirty pre-existing worktree; this phase added only the untracked report named in Section 32; no cleanup/reset performed

git diff --stat
=> 213 tracked files changed, 9,344 insertions, 1,914 deletions, all pre-existing/prior-phase state apart from this new untracked report (untracked files are not included in diff --stat)

git diff
=> inspected; existing tracked changes preserved; the new report is untracked and therefore not emitted by plain git diff
```

## 39. Remaining DecisionRequired

None. Rev5 is unambiguous and no new product decision was encountered.

## 40. Remaining backlog

- `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` — historical snapshot of final activation `LoadDecision` and safety result.

## 41. Post-Adaptation next step

Adaptation V1 is closed at the requested pilot boundary. Any next phase must be separately authorized; this phase does not start distance/level/day-count generalization, Habit Adaptation, Flutter, or other product expansion.

## 42. Final classification

```text
TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_IMPLEMENTED_AND_VERIFIED
```
