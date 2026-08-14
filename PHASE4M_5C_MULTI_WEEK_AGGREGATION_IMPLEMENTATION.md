# Phase 4M.5C — Multi-Week Window Aggregation: Implementation

**Canonical authority:** `appsel-adaptation-v1-canonical-spec (2)-rev 5.md`, §7a "Multi-Week Window Aggregation" (DECIDED). The Phase 4M.5B comparison document was consulted only as supporting audit evidence and never overrode Rev5.

## 1. Canonical Revision 5 source

Rev5 §7a selects **B-weekly-summary + B1 (worst-week-wins) + original-week lineage attribution**, explicitly rejecting B-weekly-checkpoint (would reopen window identity/idempotency/chronology/Block-semantics frozen across 4M.4–4M.4B.2C). The frozen formula:

```
WeeklyExecutionSummary(W) = WindowExecutionSummary (§6), scoped to structural week W
WeeklyLoadDecision(W)     = NextWindowLoadDecisionPolicy (§7, unchanged) applied to WeeklyExecutionSummary(W)
NextWindowLoadDecision    = min(WeeklyLoadDecision(W) for W in window), ordering Reduce < Maintain < ProgressAsPlanned
SafetyReviewRequired      = OR(WeeklySafetyReviewRequired(W) for W in window)
```

Numeric anchor architecture is explicitly unaffected — `NextWindowNumericAnchorSelector` still runs once per activation, on the final aggregated decision, using window-level `ValidatedSustainableLoad`/`PriorValidatedCheckpointLoad`. The Weekly Lineage Attribution Rule: a replacement session's evidence is always attributed to its **original root's** structural week, never the replacement's own physical week.

## 2. Files inspected (Section A re-audit)

- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/WindowCheckpointEvidenceMapper.cs`
- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/WindowExecutionSummaryBuilder.cs`
- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/NextWindowLoadDecisionPolicy.cs`
- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/NextWindowNumericAnchorSelector.cs`
- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs`
- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/AdaptationPersistenceContracts.cs`
- `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/ScheduleRepairCandidateProvider.cs`
- `RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs` (the single real call site)
- `RunningApp.Domain/Entities/LongHorizonRollingWeekState.cs`, `LongHorizonRollingSessionState.cs`
- `RunningApp.IntegrationTests/.../WindowCheckpointSummaryAndDecisionTests.cs` (existing fixture conventions)
- Grepped the whole `RunningApp.Application` tree for any existing `Reduce/Maintain/Progress` severity-ordering authority — none found, confirming a new one was required (Rev5 §7a Section G).

**Confirmed, not assumed:** the one real call site was exactly `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`, lines ~83–113 (pre-change): `windowSessions` flattened across the whole `[CurrentWindowStartWeek, CurrentWindowEndWeek]` range, fed once into `WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(windowSessions))` then `NextWindowLoadDecisionPolicy.Evaluate(windowSummary)` — the exact multi-week direct-invocation bug Rev5 exists to fix.

## 3. Old broken decision path

```
aggregate.Weeks[window range] → flat session list → WindowCheckpointEvidenceMapper.ToEvidence
    → WindowExecutionSummaryBuilder.Build (one summary for the WHOLE window, up to 16 sessions)
    → NextWindowLoadDecisionPolicy.Evaluate (reads raw EffectiveCompletedCount against the
      0/1/2/3/≥4 matrix calibrated for exactly 4 sessions)
```

## 4. New weekly decision path

```
aggregate.Weeks[window range] (kept as windowWeeks, not just flattened)
    → WeeklyWindowPartitioner.PartitionByStructuralWeekLineage(windowWeeks)
      (groups sessions by ORIGINAL ROOT's own WeekStateId, not physical week)
    → per group: WindowCheckpointEvidenceMapper.ToEvidence → WindowExecutionSummaryBuilder.Build
      → NextWindowLoadDecisionPolicy.Evaluate  (UNCHANGED, now correctly sees exactly 1K+2E+1L)
    → WeeklyLoadDecisionAggregator.AggregateWorstWeekWins(weeklyResults)  (B1, new)
    → nextWindowResult (feeds everything downstream exactly as before)
```

The pre-existing whole-window `windowSummary` build is **retained**, unchanged, immediately before the new weekly path — it is still required (and still window-level) because `NextWindowNumericAnchorSelector.Select` reads its `EffectiveCompletedCount`. It is simply never again passed into `NextWindowLoadDecisionPolicy.Evaluate` directly.

## 5. Structural-week authority used

`LongHorizonRollingWeekState.Id`/`GlobalWeek` (explicit, persisted). The partitioner groups sessions purely by `WeekStateId` identity — **no date/weekday arithmetic anywhere**, no `DayOfWeek == Monday` assumption. `LongHorizonRollingSessionState.WeekStateId` is the only identity read.

## 6. Original-week lineage implementation

`WeeklyWindowPartitioner.PartitionByStructuralWeekLineage` (new file `WeeklyLoadDecisionAggregation.cs`):
1. Builds a flat `byId` map of every session in the window.
2. For each session, walks `AdaptedFromSessionId` backward (mirroring the exact backward-walk shape `WindowExecutionSummaryBuilder` already uses forward) to find its ultimate root, and reads that root's own `WeekStateId`.
3. Assigns the session to that week's evidence bucket — a root (including a Superseded one, which is never itself a replacement) always resolves to its own week; every descendant resolves to its root's week, regardless of the descendant's own physical `WeekStateId`.

`WindowExecutionSummaryBuilder` and `WindowCheckpointEvidenceMapper` are **completely unmodified** — they simply run once per resulting bucket instead of once against the whole window, with no awareness the partitioning exists.

## 7. Cross-week replacement behavior

Proven directly (`WeeklyLoadDecisionAggregationTests.L9`, `L10`): a KEY session marked `NotToday` in Week 1, repaired (`RescheduleToEmptySlot` or `SubstituteFutureEasy`) into a physically-Week-2 slot and `Completed`, is attributed entirely to **Week 1**'s evidence bucket — Week 1's `ExpectedSessionCount` stays 4 (never 5), `KeySessionCompleted` becomes true via the cross-week replacement. Week 2's own bucket never gains a 5th expectation from the physically-present replacement row, and (for `SubstituteFutureEasy`) Week 2's own superseded EASY session remains part of Week 2's own neutral denominator, unchanged from Rev3.1 §6 semantics.

## 8. WindowExecutionSummaryBuilder reuse

Zero lines of `WindowExecutionSummaryBuilder.cs` or `WindowCheckpointEvidenceMapper.cs` were changed. The "minimal partitioning/input seam" allowed by Section E was sufficient — no input-contract change to the builder was needed, because it already operates on an arbitrary `IReadOnlyList<LogicalSessionEvidence>` with no week concept baked in; the seam only had to decide *which* sessions go into each call.

## 9. B1 aggregation implementation

`WeeklyLoadDecisionAggregator.AggregateWorstWeekWins` (same new file): takes the per-week `NextWindowAdaptationResult` list, selects the least-severe-ordered (Reduce=0 < Maintain=1 < ProgressAsPlanned=2) `LoadDecision` via `MinBy`, and OR-aggregates `SafetyReviewRequired` across the same list. Pure function — no DB, no numeric anchors, no workout generation, no phase logic.

## 10. Decision ordering authority

`Severity(NextWindowLoadDecision)` inside `WeeklyLoadDecisionAggregator` is the **only** place in the codebase this ordering is encoded as a comparable value (confirmed via repo-wide grep before writing it — no prior authority existed). It is deliberately not derived from or shared with `NextWindowNumericAnchorSelector`'s numeric "lower of two loads" comparison, which is an unrelated notion of "lower."

## 11. Safety OR aggregation

Mathematically identical to the pre-existing single-window `HasSafetyFlag = sessions.Any(...)` computation (OR is associative — `Any` over 16 sessions directly equals `Any` of four `Any`-over-4 results). Verified independent of `LoadDecision` in `WeeklyLoadDecisionAggregationTests.L7` (all four weeks `ProgressAsPlanned`, one week's safety flag true, aggregated result: `ProgressAsPlanned` + `SafetyReviewRequired=true`).

## 12. Mixed-phase handling

No new logic was needed. Because `WeeklyExecutionSummary` is now scoped per structural week, and every real structural week already carries its own `Stage`/`SegmentType` (Section F/4M.5B), a window spanning multiple stages (confirmed real via captured HTTP data: GeneralEndurance→AerobicStrength→AerobicStrength→PreSpecificTransition) is handled correctly by construction — proven in `WeeklyLoadDecisionAggregationTests.L11` (4 differently-staged weeks, no cross-week evidence contamination, one decision per week, B1 aggregates correctly).

## 13. Files created

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/WeeklyLoadDecisionAggregation.cs` (`WeeklyWindowPartitioner`, `WeeklyLoadDecisionAggregator`)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Adaptation/WeeklyLoadDecisionAggregationTests.cs` (L1–L11 + Section M structural guard, 13 tests)
- `PHASE4M_5C_MULTI_WEEK_AGGREGATION_IMPLEMENTATION.md` (this file)

## 14. Files modified

- `backend/RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs` — replaced the direct `NextWindowLoadDecisionPolicy.Evaluate(windowSummary)` call with the weekly-partition-then-aggregate path; retained the whole-window `windowSummary` build (still feeds the numeric anchor selector's `EffectiveCompletedCount`, unchanged).
- `backend/RunningApp.IntegrationTests/.../LongHorizonNextWindowDecisionActivationTests.cs` — `IllnessOnKey_OtherFifteenCompleted_NoRepair_ProgressAsPlanned_SafetyFalse` renamed to `..._Maintain_SafetyFalse` and its assertion corrected: under the OLD bug this case (1 of 16 sessions missing, the missing one being KEY) incorrectly reported `ProgressAsPlanned`; Rev5's correct weekly evaluation finds the KEY-missing week is genuinely `Maintain` (3/4, Key not Easy missing), and B1 worst-of-four correctly surfaces `Maintain` for the window. This was Phase 4M.5's own originally-disclosed open question, now resolved by the fix, not a new regression.
- `backend/RunningApp.IntegrationTests/.../LongHorizonThreeWindowAnchorThreadingE2ETests.cs` — `RealMaintainActivation_UsesPriorValidatedCheckpointLoadVerbatim_GenuinelyAdvancesWindow`'s fixture updated to complete one LONG_RUN + one EASY_SUPPORT **per real structural week** (previously concentrated in only the window's first week, which under Rev5's correct per-week evaluation would now aggregate to `Reduce`, a different, still-real scenario the test was never designed to prove); added one new real end-to-end test (§16 below).

## 15. Tests added/changed

- 12 new pure-aggregation tests + 1 structural guard test in `WeeklyLoadDecisionAggregationTests.cs` (L1–L11, Section M).
- 1 new real end-to-end HTTP test in `LongHorizonThreeWindowAnchorThreadingE2ETests.cs` (§16).
- 2 pre-existing tests corrected (§14) to match Rev5's corrected behavior, not weakened.

## 16. 8-Easy regression result (Section K)

**Pure aggregator** (`WeeklyLoadDecisionAggregationTests.L8_EightEasyZeroKeyZeroLong_...`): real Postgres fixture, 4 structural weeks, each with exactly 2 EASY completed and KEY/LONG both NotToday (window-wide: 8 Easy / 0 Key / 0 Long completed). Result: **all four weekly decisions = `Maintain`; aggregated = `Maintain`** — deterministic, not merely "not ProgressAsPlanned". PASS.

**Real HTTP end-to-end** (`RealActivation_EightEasyOneLongZeroKeyAcrossRealFourWeekWindow_...`): same shape through the live `activate-next-window` endpoint across a real 4-week/16-session window (Long completed once, in week 1 only, to satisfy a real, pre-existing, unrelated JIT evidence-completeness requirement that legitimately Blocks on zero window-wide Long-Run evidence — see the test's doc comment for why the literal 0-Long edge is not a reachable production HTTP state and is instead covered by the pure-aggregator proof). Result: `outcome=activated`, `next_window_load_decision=maintain`. PASS.

## 17. Feasible Maintain/Reduce regression

`RealMaintainActivation_UsesPriorValidatedCheckpointLoadVerbatim_GenuinelyAdvancesWindow` (fixed fixture) and `RealReduceActivation_ThreadsAnchorCorrectly_GenuinelyAdvancesWindow` (unmodified — its evidence shape already produced worst-case-everywhere Reduce under both old and new semantics) — both PASS, proving genuine window advancement with correctly-threaded anchors under the new weekly-aggregation path.

## 18. Catalog-infeasible Block regression

`RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement` and `RealReduceLandingOnRunwayCoreBoundary_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement` — both PASS unmodified. `LONG_HORIZON_CONTINUATION_BLOCKED` still fires correctly; no upward clamp introduced.

## 19. IsBlock/no-false-success regression

Covered by the same two tests above plus the full `LongHorizonBlockRecoveryClassificationTests`/`LongHorizonJitBoundaryAndCrossOperationRaceTests` suites (re-run, all green) — the 4M.4B.2A `IsBlock` fix is entirely upstream of this phase's change and untouched.

## 20. Idempotency regression

`activationRecords.Count == activationRecords.Select(a => a.IdempotencyKey).Distinct().Count()` assertions in the anchor-threading E2E tests, plus `LongHorizonRetryContinuationTests` (full suite re-run) — all green, no duplicate activation, no double numeric-anchor application.

## 21. Exact commands and results

```
dotnet build backend/RunningApp.sln
  → 0 Uyarı (warnings), 0 Hata (errors)

dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~WeeklyLoadDecisionAggregationTests"
  → 12/12 passed (then 13/13 after the Section M guard test was added)

dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~WeeklyLoadDecisionAggregationTests|FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests"
  → 18/18 passed

dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~NextWindowLoadDecisionPolicy|FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests|FullyQualifiedName~PlanAdaptationV1DecisionTests|FullyQualifiedName~MaintainNotExceedingProgressAsPlannedInvariantTests|FullyQualifiedName~NextWindowNumericAnchorSelectorTests|FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests|FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests|FullyQualifiedName~LongHorizonFirstCheckpointNumericAnchorTests|FullyQualifiedName~LongHorizonNumericAnchorMaterializationE2ETests|FullyQualifiedName~ScheduleRepairPersistenceTests|FullyQualifiedName~ScheduleRepairRuntimeOrchestratorTests|FullyQualifiedName~ScheduleRepairSupersededAndReadCorrectnessTests|FullyQualifiedName~RuntimeNotTodayReasonMapperTests|FullyQualifiedName~LongHorizonExplicitNextWindowActivationTests|FullyQualifiedName~LongHorizonRetryContinuationTests|FullyQualifiedName~LongHorizonBlockRecoveryClassificationTests|FullyQualifiedName~LongHorizonJitBoundaryAndCrossOperationRaceTests|FullyQualifiedName~WeeklyLoadDecisionAggregationTests"
  → 245/245 passed
```

Two genuine, expected fixture-level regressions were found on the first pass (documented in §14), root-caused as correct consequences of the Rev5 fix (not defects introduced by this phase), and corrected.

## 22-23. Full LongHorizon and full backend regression

```
dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizon"
  → 1112/1112 passed (baseline 1098 + 14 new tests: 13 WeeklyLoadDecisionAggregationTests + 1 new E2E test)

dotnet test backend/RunningApp.sln
  → 3351/3351 passed (baseline 3337 + 14 new tests), 15m14s
```

## 24. Build/git diff result

```
dotnet build backend/RunningApp.sln → 0 Uyarı, 0 Hata

git diff --check -- backend/RunningApp.Application backend/RunningApp.IntegrationTests backend/RunningApp.Domain
  → clean (only pre-existing, unrelated LF/CRLF warnings on files this phase never touched)

git status --short backend/RunningApp.Application backend/RunningApp.IntegrationTests backend/RunningApp.Domain
  → identical shape to the pre-4M.5C session state (the entire LongHorizon subtree was already untracked
    from earlier, uncommitted phases in this session; this phase's new/modified files are absorbed into
    that same untracked subtree, with no new top-level or unrelated diff entries)
```

## 25. Production migrations/schema changes

**None.** No new DB tables, columns, or migrations. `WeeklyWindowPartitioner`/`WeeklyLoadDecisionAggregator` are pure, transient, in-memory functions operating on already-persisted data.

## 26. New PRODUCT DEFAULT / constants

**None.** The severity ordering (`Reduce < Maintain < ProgressAsPlanned`) is Rev4.1's own already-frozen invariant, merely encoded once as a comparable value (Section G requirement: exactly one authority, not duplicated). No new threshold, percentage, or numeric constant was introduced anywhere.

## 27. Remaining DecisionRequired items

**None newly introduced by this phase.** The two fixture corrections (§14) were not product ambiguities — they were pre-existing tests that had encoded the old, buggy behavior as their expected value; Rev5 §7a already specifies the correct behavior unambiguously, and the corrected fixtures now assert it.

## 28. Scope/non-goal confirmation

Explicitly **not** implemented, per the phase's own prohibitions — confirmed by code review of every file touched:
- B-weekly-checkpoint (no new activation/checkpoint boundary; the rolling activation window's identity, idempotency, chronology, and Block-semantics are byte-for-byte unchanged).
- No percentage/adherence-ratio threshold anywhere.
- No new role-percentage threshold.
- No majority voting, no recency weighting, no most-recent-week-wins (B1 worst-week-wins only).
- No destination-week lineage attribution (original-week attribution only, per §7).
- No numeric anchor formula change (`NextWindowNumericAnchorSelector.cs` has zero diff).
- No new durable `LoadDecision`/`SafetyReviewRequired` snapshot persistence.
- No new DB schema/migration.
- No generalization beyond TEN_K/INTERMEDIATE/4D.
- No commit, no push.
- The final post-Rev5 re-acceptance phase was not started.

## 29. Final classification

```
ADAPTATION_V1_MULTI_WEEK_AGGREGATION_IMPLEMENTED_AND_VERIFIED
```

Current overall Adaptation V1 classification remains, per the phase's own explicit instruction, **not yet** re-promoted:

```
TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_CONDITIONALLY_VERIFIED
```

(A final post-Rev5 re-acceptance pass, not run in this phase, is required before that classification can change.)
