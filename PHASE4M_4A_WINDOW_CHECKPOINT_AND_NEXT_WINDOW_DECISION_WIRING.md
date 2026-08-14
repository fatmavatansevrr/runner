# Phase 4M.4A — Window Checkpoint and Next-Window Decision Wiring

## 1. Files inspected
`PlansController.cs`, `ILongHorizonActivePlanServices.cs`, `LongHorizonRollingWindowActivationService.cs` (full read), `LongHorizonActiveReadContracts.cs`, `LongHorizonActiveReadModelProvider.cs`, `LongHorizonAdaptationDecisionRecord.cs`, `AppDbContext.cs`, `LongHorizonRollingStateRepository.cs`, `LongHorizonRollingActivationPersistenceAdapter.cs`, `LongHorizonSessionRoleCodec.cs`, `WindowExecutionSummaryBuilder.cs` (full read), `NextWindowLoadDecisionPolicy.cs` (full read), `AdaptationDomainContracts.cs`, `LongHorizonPersistenceEnums.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`/`LongHorizonRollingRestartContinuationService.cs`/`LongHorizonRollingJitActivationRuntime.cs`, `LongHorizonExplicitNextWindowActivationTests.cs`.

## 2. Current checkpoint/activation architecture
`POST /api/v1/plans/active/long-horizon/activate-next-window` → `PlansController.ActivateLongHorizonNextWindow` → `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`. Explicit `BeginTransactionAsync` + `SELECT ... FOR UPDATE` on `TrainingPlans`, mirroring the mutation-service/persistence-service lock pattern already established. Idempotency via `LongHorizonActivationWindowRecords.IdempotencyKey` (unique index), keyed `"activation:{planStateId}:{windowId}:{sequence}"` / `"jit:{...}"`.

## 3. Readiness model
Re-verified `LongHorizonCheckpointReadiness { CurrentWindowInProgress, CurrentWindowComplete (unused/dead), NextWindowActivationReady, ReassessmentRequired, TerminalPlanComplete }`, computed solely by `LongHorizonActiveReadModelProvider.Readiness(...)`. The activation service does not duplicate this logic — it calls the exact same function every other consumer calls.

## 4. Source-window finality rule
Unchanged, reused as-is: `Readiness()` returns `NextWindowActivationReady` only when every session in the `[CurrentWindowStartWeek, CurrentWindowEndWeek]` range is non-`Planned`. The 4M.4A checkpoint computation (§9-10) is inserted **only inside the `NextWindowActivationReady` branch**, after the existing `switch`'s early-return branches for `TerminalPlanComplete`/`CurrentWindowInProgress`/`ReassessmentRequired` — no new completion threshold was invented.

## 5. WindowExecutionSummary input authority
A new file, `WindowCheckpointEvidenceMapper.cs`, maps the **exact same `windowSessions` list** the activation service already eager-loads (`aggregate.Weeks.Where(GlobalWeek in range).SelectMany(w => w.Sessions)`, no extra query) into `LogicalSessionEvidence`. No stale in-memory aggregate is used — this is the same transaction-locked, freshly-loaded aggregate the rest of the activation flow already relies on.

## 6. Rev3.1 denominator implementation
Not reimplemented — delegated entirely to the frozen `WindowExecutionSummaryBuilder.Build`. The mapper's only job is field translation (role, `OutcomeStatus`, `PlanningStatus`, `AdaptedFromSessionId`, mapped `NotTodayReasonCode`); every denominator/lineage/recovery rule (roots-only `ExpectedSessionCount`, Superseded-as-neutral, lineage-terminal-outcome-based recovery) is the builder's own unmodified logic.

## 7. Replacement/Superseded lineage handling
`session.AdaptedFromSessionId` → `LogicalSessionEvidence.AdaptedFromId` is a direct 1:1 field passthrough — the builder's own `FollowLineageToTerminalOutcome` walk resolves recovery. `session.PlanningStatus == Superseded` → `SessionPlanningStatus.Superseded`, everything else → `Active`.

## 8. Safety evidence aggregation
Derived **entirely from session-level data already on `LongHorizonRollingSessionState`** — `OutcomeStatus == NotToday` + `NotTodayReason` (mapped via the existing 4M.3 `RuntimeNotTodayReasonMapper`) is exactly what `WindowExecutionSummaryBuilder.Build`'s own `HasSafetyFlag` computation (`ReasonClassificationPolicy.TriggersSafetyFlag`) needs. **No query against `LongHorizonAdaptationDecisionRecords` was needed or added** — the per-decision `SafetyReviewRequired` column on that table is a separate, already-redundant signal for this purpose (the session row's own reason is the authoritative source, confirmed sufficient and consistent with 4M.3's own mapping).

## 9. WindowExecutionSummaryBuilder wiring
Called live, exactly once per `NextWindowActivationReady` activation attempt, in `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`: `WindowExecutionSummaryBuilder.Build(WindowCheckpointEvidenceMapper.ToEvidence(windowSessions))`. Zero changes to the builder itself.

## 10. NextWindowLoadDecisionPolicy wiring
`NextWindowLoadDecisionPolicy.Evaluate(summary)` called immediately after, producing `NextWindowAdaptationResult`. Zero changes to the policy itself; the 4D decision matrix (0-1→Reduce, 2→Maintain, 3-only-EASY-missing→ProgressAsPlanned, 3-KEY-or-LONG-missing→Maintain, 4+→ProgressAsPlanned) was re-verified by direct code read (§7 of the audit) to match the phase document exactly, unchanged.

## 11. 4D load-decision matrix
Unchanged (re-verified, not re-implemented) — see §10. Confirmed via 8 targeted tests (`LoadDecisionMatrix_ByEffectiveCompletedCount` theory ×4 + 3 dedicated 3/4-missing-role tests + `SupersededByAdaptationCountAlone_DoesNotAlterDecision`).

## 12. ProgressAsPlanned propagation
Computed value flows unchanged into the response's `NextWindowLoadDecision = ProgressAsPlanned`. **Does not currently influence composition** — see §14/§15/§26 (DecisionRequired).

## 13. Maintain propagation
Same as ProgressAsPlanned: the symbolic value reaches the response DTO. **Gap confirmed by audit (§9 of the audit report)**: no existing typed progression-factor/version/sequence concept exists anywhere in the numeric generation pipeline (`ComposeAndActivateNextWindowAsync`, `LongHorizonCheckpointEvidenceAggregator`) that could represent "do not advance." Reusing something that doesn't exist was not possible; inventing one was explicitly forbidden. Materialization behavior for Maintain is therefore **unchanged from pre-4M.4A** (numeric generation always proceeds forward from raw evidence, exactly as before).

## 14. Reduce behavior
Symbolic only. `NextWindowLoadDecision.Reduce` reaches the response DTO (`LongHorizonNextWindowLoadDecision.Reduce`) and nothing else. No numeric translation of any kind was implemented or attempted.

## 15. Explicit numeric-Reduce non-implementation confirmation
Confirmed by construction: no file created or modified in this phase references distance/duration/intensity percentages, volume bands, multipliers, or `RecoveryWeek` (the one `RecoveryWeek` concept that exists in the codebase, `LongHorizonGeStructuralSelector`, is a pre-existing, unrelated, position-based GE mesocycle concept — confirmed untouched and not referenced by any new file). Verified by test 34 (`FullyCompletedWindow_ActivationReportsProgressAsPlanned_SafetyFalse_NoNumericMutation` — activated sessions carry ordinary non-special-cased content).

## 16. SafetyReviewRequired propagation
`NextWindowAdaptationResult.SafetyReviewRequired` (= `summary.HasSafetyFlag`, computed independently of `LoadDecision` inside the frozen policy) flows unchanged to `LongHorizonActivateNextWindowResponse.NextWindowSafetyReviewRequired`. Verified independent of `LoadDecision` by three dedicated tests (`OneSorenessNotToday...ProgressAsPlanned`, `SafetyTrue_MaintainScenario...`, `SafetyTrue_ReduceScenario...` — same `SafetyReviewRequired=true` co-occurring with all three different `LoadDecision` values).

## 17. Distinction from 4M.3 SafetyFlag
Explicit, by naming and by doc comment on the new response field: `LongHorizonSessionMutationResponse.ScheduleRepairSafetyFlag` (4M.3, single-event, immediate HTTP result) vs. `LongHorizonActivateNextWindowResponse.NextWindowSafetyReviewRequired` (4M.4A, window-level, aggregated at checkpoint). No code path reads one to produce the other; they are computed from different inputs at different times by different authorities (`ReasonClassificationPolicy.TriggersSafetyFlag` per-decision vs. `WindowExecutionSummaryBuilder`'s aggregate `HasSafetyFlag` over the whole window).

## 18. ReassessmentRequired behavior
Entirely unchanged — still computed solely by `LongHorizonActiveReadModelProvider.Readiness()`'s own pre-existing rule (`CurrentBlockedPublicReasonCategory is not null` or any week `NumericActivationBlocked`), with **no** reference to `SafetyReviewRequired`/`HasSafetyFlag` anywhere in that computation. The two remain fully distinct enums/signals, never merged.

## 19. TerminalPlanComplete behavior
Unchanged: the `TerminalResponse(...)` branch returns before the new checkpoint-computation code is ever reached — `NextWindowLoadDecision`/`NextWindowSafetyReviewRequired` are `null` on that response (verified by `TerminalPlanComplete_DoesNotProduceNextWindowDecision`).

## 20. Activation/idempotency behavior
No new durable decision record was added (see §21). The computed `NextWindowAdaptationResult` is derived deterministically from `windowSessions` — the same immutable, already-transaction-locked snapshot the rest of the activation flow uses — so a genuine replay of the same checkpoint (same window, same persisted rows) recomputes byte-for-byte identically every time; proven by `LockedRev31Scenario_ProducesExactCanonicalSummary`'s fresh-`DbContext` re-read producing an equal `WindowExecutionSummary` and identical `LoadDecision`.

## 21. Persistence/audit decision
**No new table added.** Audited per §N of the prompt: (1) activation is already recorded in `LongHorizonActivationWindowRecords` (existing idempotency record); (2) replay of `activate-next-window` already re-reads fresh persisted state rather than returning a cached body (confirmed in the audit, `BuildActivatedResponseAsync` always re-queries); (3) the checkpointed window's session data is immutable once `NextWindowActivationReady` is reached (no further `Complete`/`NotToday` mutation is possible — every session is already terminal, and 4M.3's own guards prevent re-mutating terminal/Superseded rows), so deterministic re-evaluation against that immutable state is sufficient and consistent with the existing architecture's own approach — a durable decision record is not required for correctness. Documented per the prompt's explicit "document that" instruction rather than adding a new table.

## 22. Files created
- `WindowCheckpointEvidenceMapper.cs`
- Tests: `WindowCheckpointSummaryAndDecisionTests.cs`, `LongHorizonNextWindowDecisionActivationTests.cs`

## 23. Files modified
- `LongHorizonRollingWindowActivationService.cs` — checkpoint computation inserted after readiness switch; `BuildActivatedResponseAsync` signature extended with `NextWindowAdaptationResult`; response population added.
- `LongHorizonActiveReadContracts.cs` — new public enum `LongHorizonNextWindowLoadDecision`; two new nullable fields on `LongHorizonActivateNextWindowResponse`.

## 24. Tests added
19 new tests: 17 in `WindowCheckpointSummaryAndDecisionTests.cs` (summary correctness items 1-9, safety items 10-14, decision-matrix items 15-22 — several combined via `[Theory]`), 2 in `LongHorizonNextWindowDecisionActivationTests.cs` (real end-to-end activation wiring + terminal non-production). Combined with re-run/unmodified pre-existing activation tests (`LongHorizonExplicitNextWindowActivationTests`, `LongHorizonFullLifecycleMatrixTests` — 15 tests, all green after the role-resolution fix in §26).

## 25. Exact targeted test commands/results
```
dotnet test --filter "FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests"        → 17/17 passed
dotnet test --filter "FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests"    → 2/2 passed
dotnet test --filter "FullyQualifiedName~LongHorizonExplicitNextWindowActivationTests|FullyQualifiedName~LongHorizonFullLifecycleMatrixTests" → 15/15 passed
dotnet test --filter "FullyQualifiedName~LongHorizonActivatedCalendarProjectionTests|...JitActivationRuntimeTests|...JitCompositionOrchestratorTests|...FullDarkLifecycleHarnessTests" → 59/59 passed
```

## 26. 4M.1 regression result
`dotnet test --filter "FullyQualifiedName~PlanAdaptationV1DecisionTests"` → **68/68 passed**.

## 27. 4M.2 regression result
`dotnet test --filter "FullyQualifiedName~ScheduleRepairPersistenceTests"` → **25/25 passed**.

## 28. 4M.3 regression result
`dotnet test --filter "FullyQualifiedName~RuntimeNotTodayReasonMapperTests|...OrchestratorTests|...SupersededAndReadCorrectnessTests"` → **38/38 passed**.

## 29. Activation/readiness regression result
See §25 — **74/74 passed** across all pre-existing activation/RollingActivation test files, after fixing a real regression this phase's first implementation attempt introduced: `WindowCheckpointEvidenceMapper`'s initial role resolution used `LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy` only, which deliberately fails closed on General Endurance's suffixed `EASY_SUPPORT_1`/`EASY_SUPPORT_2` tokens — this broke 10 real pre-existing GE-boundary activation tests with `LONG_HORIZON_ADAPTATION_INTEGRITY_VIOLATION`. Fixed by falling back to the existing `IsEasySupport`/`IsKeySession`/`IsLongRun` prefix-matching helpers (the same pattern every other GE-aware call site already uses) before all 74 activation tests were re-run and confirmed green.

## 30. Full backend regression result
See final report — command `dotnet test RunningApp.IntegrationTests` with `RunningApp.IntegrationTests/xunit.runner.json` (`parallelizeTestCollections: false`, the repo-accepted configuration established during Phase 4M.3's confirmation pass to eliminate a pre-existing cross-collection shared-mock-user race).

## 31. Build/static/git diff result
`dotnet build` → 0 warnings, 0 errors. `git diff --check` → only pre-existing CRLF/LF warnings, no real errors.

## 32. Scope/non-goal confirmation
No numeric Reduce percentage/band/multiplier, no workout-content mutation, no catalog rewrite, no new plan generator, no background/automatic activation, no Flutter/UI changes, no 21K/Marathon changes, no new adaptation reasons/schedule-repair rules, no adherence scoring model, no missed_score/HRV/sleep/medical logic, no race-date changes. Verified by construction — none of these concepts appear in any file created or modified in this phase.

## 33. Remaining Phase 4M.4B boundaries
The exact numeric translation of `Maintain`/`Reduce` into `ComposeAndActivateNextWindowAsync`'s evidence/load inputs (`ValidatedSustainableLoad`, `EvidenceSnapshot`, or a new typed progression-factor concept) is entirely Phase 4M.4B's scope — no seam for it exists today (confirmed by audit), and none was invented here.

## 34. DecisionRequired items
**One, exactly matching the prompt's anticipated scenario (§I/§V):** the real next-window numeric-generation pipeline (`LongHorizonRollingJitCompositionOrchestrator`/`LongHorizonCheckpointEvidenceAggregator`) has no existing typed seam to represent "Maintain" (freeze progression) or "Reduce" (constrain downward) without inventing new progression/numeric behavior. Per instruction, this sub-part was stopped at the typed boundary: `NextWindowLoadDecision`/`SafetyReviewRequired` are computed live from real persisted state and fully propagated to the typed application/response boundary, but do not yet influence what next-window content is actually materialized. This is the exact condition for the alternate classification below.

## 35. Final classification

```
ADAPTATION_V1_WINDOW_DECISION_WIRING_IMPLEMENTED_NUMERIC_ACTIVATION_REMAINS_BLOCKED
```

All checkpoint/decision-wiring correctness criteria hold (real persisted-state input authority, Rev3.1 denominator/lineage/Superseded semantics preserved, safety independence preserved, 4D matrix unchanged, terminal/in-progress/reassessment behavior preserved, targeted + regression suites green, build/git diff clean). The phase is blocked from full completion **only** because the activation/materialization seam requires a numeric Maintain/Reduce translation that does not yet exist and was correctly not invented here — exactly the alternate classification the phase document defines for this situation.

No code committed, no push, Phase 4M.4B not started, Phase 4M.5 not started.
