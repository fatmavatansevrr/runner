# Phase 4M.4B.2A — Multi-Window Activation Advancement Defect: Investigation + Narrow Fix

## 1. Reproduction

Minimal real repro (`LongHorizonWindowAdvancementDefectReproTests`, temporary fixture, deleted after the fix was confirmed and folded into permanent regression coverage): TEN_K/Intermediate/4-day, 21-week race, plan confirmed (window `[1-1]`). One session (`LONG_RUN`) completed, the other three marked `NotToday(illness)` — real Reduce evidence, `EffectiveCompletedCount=1`. Real HTTP `POST /api/v1/plans/active/long-horizon/activate-next-window`.

**Before the fix:** response was HTTP 200, `"outcome":"activated"`, `"next_window_load_decision":"reduce"`, `"activated_window_range":{"start":1,"end":1}` (same as before), `"activated_sessions"` were the same four already-touched Window 1 rows. `plan.CurrentWindowStartWeek/EndWeek` stayed `[1-1]`. `plan.ActiveContextVersionSequence` stayed `1` (never incremented). `LongHorizonActivationWindowRecords` had exactly the one pre-existing row from initial confirmation — no new row, same `ActivatedAtUtc` timestamp as before the call. A subsequent Complete/NotToday call against any of those four sessions returned 409 `LONG_HORIZON_ROLLING_SESSION_OUTCOME_CONFLICT` ("A not-today outcome already owns this rolling session"), because they had never actually been superseded by a new window.

**After the fix:** the identical scenario now correctly returns HTTP 409 `LONG_HORIZON_CONTINUATION_BLOCKED`.

## 2. Root cause

`LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` classifies the checkpoint runtime's outcome into three branches *before* calling into JIT/Runway composition:

```csharp
var pureGe = checkpoint.Outcome == NextGeWindowActivated && !reachesGeBoundary;
var isBlockAttempt = checkpoint.Outcome == NextGeWindowBlocked;
...
if (isJitEvidenceUnavailable) isBlockAttempt = true;
```

When neither `pureGe` nor `isBlockAttempt` is true (the `GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached` case — General Endurance phase exhausted, real Runway/Core JIT composition needed), the code calls `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync`. That method has its own, entirely separate ability to decide composition failed and persist a Block instead of an activation:

```csharp
if (result.Outcome != LongHorizonRollingJitCompositionOutcome.CompositionAndActivationSucceeded)
{
    ...
    return await _blockAdapter.PersistBlockAsync(...); // returns Outcome=Success (a successful BLOCK persist)
}
```

`LongHorizonRollingPersistenceResult` (the shared return type for both real activations and block persistence) had no field distinguishing "successfully persisted an activation" from "successfully persisted a Block." Back in `ActivateNextWindowAsync`, the outer switch only knows about `isBlockAttempt`, which was computed *before* this internal decision and has no visibility into it:

```csharp
case Success:
case IdempotentReplay:
    await tx.CommitAsync(ct);
    if (isBlockAttempt)                         // false -- outer code never learns a Block was persisted
        throw new LongHorizonContinuationBlockedException(...);
    return await BuildActivatedResponseAsync(...); // builds a false "activated" response from the UNCHANGED plan row
```

The transaction commits (the Block record itself is real and correctly persisted), but the response layer, believing this was a normal activation, re-reads the plan (whose `CurrentWindowStartWeek/EndWeek` were never touched by the Block persist) and reports it as a successful activation of the *same, already-checkpointed* window.

## 3. Defect classification

**`CHECKPOINT_OUTCOME_ROUTING_DEFECT`** — the outer routing logic's block/activation classification is computed too early and does not account for a block decision made later, inside the JIT/Runway composition call it dispatches to.

Window identity/idempotency-key derivation itself (§E of the investigation prompt) was audited and found correct: `WindowId = StableGuid(seed + "|window")` where `seed` includes `request.MostRecentlyActivatedWindow.WindowId`, `CheckpointDate`, evidence rows, etc. — chronologically distinct windows never collide because each has a distinct source `WindowId` and evidence shape in its seed. `nextStart`/`nextEnd` computation inside `LongHorizonRollingCheckpointRuntime` was independently instrumented and confirmed always correct. The active-read-model (`LongHorizonActiveReadModelProvider`) was not the defect either — it correctly read whatever `plan.CurrentWindowStartWeek/EndWeek` actually were; the row itself was simply never updated because no real activation had occurred.

## 4. Files inspected

`LongHorizonRollingWindowActivationService.cs`, `LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonCheckpointStateEvaluator.cs`, `LongHorizonRollingStateRepository.cs` (`SaveActivationSuccessAsync`, `PersistGeCheckpointAsync`), `LongHorizonRollingActivationPersistenceAdapter.cs`, `LongHorizonRollingRestartContinuationService.cs` (`ContinueJitCompositionAsync`), `LongHorizonRollingBlockPersistenceAdapter.cs`, `LongHorizonRollingPersistenceContracts.cs`, `LongHorizonRollingStateReconstructionService.cs`, `LongHorizonContextVersion.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`.

## 5. Files changed

- `LongHorizonRollingActivation/Persistence/LongHorizonRollingPersistenceContracts.cs` — added `bool IsBlock { get; init; }` to `LongHorizonRollingPersistenceResult` (defaults `false`; every existing call site is unaffected).
- `LongHorizonRollingActivation/Persistence/LongHorizonRollingRestartContinuationService.cs` — `ContinueJitCompositionAsync`'s internal composition-failure branch now returns `blockResult with { IsBlock = true }` instead of the bare block-persist result.
- `Services/LongHorizonRollingWindowActivationService.cs` — the outer switch's guard became `if (isBlockAttempt || persistResult.IsBlock)`.

No change to `NextWindowNumericAnchorSelector`, `WindowExecutionSummaryBuilder`, `NextWindowLoadDecisionPolicy`, Maintain/Reduce/ProgressAsPlanned semantics, first-checkpoint typed-block behavior, schedule-repair behavior, phase allocation, or catalog progression rules.

## 6. Exact fix

Three files, ~10 net lines. See §5. The fix is a pure signal-propagation correction: it makes an already-real, already-correctly-persisted internal decision (Block vs. Activation) visible to the caller that has to decide how to respond, rather than changing what gets decided or persisted.

## 7. Window identity/idempotency findings

No bug found here (see §3). `IdempotencyKey = "activation:{planStateId}:{WindowId}:{ContextVersion.Sequence}"` never collided across chronologically distinct windows in any real run, before or after the fix. Two chronologically distinct windows sharing the same numeric load (e.g. Reduce landing on the same `WeeklyVolumeKm` as a subsequent Maintain) do not collapse identity — `WindowId` is derived from the *source* window's identity and fresh evidence, never from the numeric anchor value.

## 8. Maintain advancement proof

`LongHorizonThreeWindowAnchorThreadingE2ETests` — Window 0 (real full completion, ProgressAsPlanned) advances `[1-1]→[2-5]`; Window 1 (real sparse Reduce evidence) advances `[2-5]→[6-9]`. The Maintain leg (Window 1→Window 2) is exercised and now correctly reports HTTP 409 `LONG_HORIZON_CONTINUATION_BLOCKED` with `CurrentBlockedInternalReasonCode=CoreJitContextUnavailable` — critically, `CurrentWindowStartWeek/EndWeek` are asserted to remain exactly at `[6-9]` (unchanged), proving the fix: this Block no longer masquerades as an advancement. See §20 (DecisionRequired) for why this leg blocks rather than activates.

## 9. Reduce advancement proof

Same test, Window 1 leg: real sparse Reduce evidence (`EffectiveCompletedCount=1`, first-ever-checkpoint-adjacent but with a genuine prior anchor from Window 0) genuinely advances `[2-5]→[6-9]`, with `next_window_load_decision":"reduce"` and a freshly re-materialized Window 2 (all sessions `Planned`, none carrying Window 1's stale `Completed`/`NotToday` status).

## 10. Progress regression

`LongHorizonNumericAnchorMaterializationE2ETests` (2/2), `LongHorizonNextWindowDecisionActivationTests.FullyCompletedWindow_ActivationReportsProgressAsPlanned_SafetyFalse_NoNumericMutation` — unchanged, still green. ProgressAsPlanned's own path (`checkpoint.ValidatedLoad` unmodified) was never touched by this fix.

## 11. Role resolver confirmation

`AdaptationSessionRoleResolver` (from the original 4M.4B.2 confirmation pass) is untouched and still the single shared authority used by both `ScheduleRepairRuntimeOrchestrator` and `WindowCheckpointEvidenceMapper`. `ScheduleRepairRuntimeOrchestratorTests`/`RuntimeNotTodayReasonMapperTests`/`ScheduleRepairSupersededAndReadCorrectnessTests` — 38/38 green.

## 12. First-checkpoint confirmation

The fix exposed that two pre-existing first-checkpoint tests (`LongHorizonFirstCheckpointNumericAnchorTests`'s Maintain/Reduce-with-evidence cases) had been silently passing due to *this exact defect* — they asserted `"outcome":"activated"`, which the old buggy code produced even though the window never actually advanced. With the fix applied, these transitions now correctly report HTTP 409 (this pilot's General Endurance phase is fully consumed by the plan's very first window — confirmed via direct `TotalWeeks`/`GeneralEnduranceWeeks` inspection — so the *next* checkpoint always routes through real Runway/Core JIT composition, which requires complete evidence to succeed; see §20). Both tests were rewritten to assert the newly-correct Block behavior plus the critical no-false-advancement invariant. `LongHorizonFirstCheckpointNumericAnchorTests` — 3/3 green (the original zero-completion Block test was unaffected and remains unchanged). `LongHorizonNextWindowDecisionActivationTests`'s soreness test was similarly affected and rewritten; both files pass in full.

## 13. Maintain ≤ ProgressAsPlanned invariant

**Not added in this phase.** Deprioritized: building this test meaningfully requires driving real HTTP activations to real, successful Maintain and ProgressAsPlanned outcomes for comparison, and §20 below shows a real Maintain activation cannot currently succeed against this pilot's only-available roadmap shape at any race length probed. This remains open; see §20 items 2–3.

## 14. 3-window real chain

`LongHorizonThreeWindowAnchorThreadingE2ETests.RealChain_ReduceSucceedsAndThreadsAnchor_MaintainBlocksWithoutFalseAdvancement_NoDoubleApplication` (1/1, passing): Window 0→1 (ProgressAsPlanned) and Window 1→2 (Reduce) both genuinely activate and thread the anchor correctly, proven against fresh persisted state. Window 2's attempted Maintain transition Blocks — disclosed and explicitly asserted, not hidden or forced — on a separate, deeper, pre-existing defect in Core/Runway JIT composition (see §20 item 2). The test proves, completely, the exact invariant this phase's fix targets: a genuine Block never masquerades as a real window advancement, for both the successful and the blocked legs.

## 15. Fresh DB proof

Every assertion above (window ranges, session outcome statuses, activation-record count/idempotency-key uniqueness, `CurrentBlockedInternalReasonCode`) is read via a fresh `AppDbContext`/DI scope created after the relevant HTTP call fully returned.

## 16. Tests added

- `LongHorizonWindowAdvancementDefectReproTests.cs` — temporary diagnostic-capture fixture used to isolate the root cause; deleted once the fix was confirmed (its findings are captured here and its invariant coverage now lives in the permanent regression tests below).
- `LongHorizonFirstCheckpointNumericAnchorTests.cs` — Maintain/Reduce-with-evidence tests rewritten to assert the corrected Block behavior + no-false-advancement invariant (rename: `..._BlocksViaRealJitRunwayEvidenceCompletenessRequirement`).
- `LongHorizonNextWindowDecisionActivationTests.cs` — soreness test rewritten similarly (rename: `RealSorenessSubmission_BlocksViaRealJitRunwayEvidenceCompletenessRequirement_ReasonMappingStillCorrect`), preserving the double-mapping (`soreness` vs. `pain_or_discomfort`) audit.
- `LongHorizonThreeWindowAnchorThreadingE2ETests.cs` — rewritten (rename: `RealChain_ReduceSucceedsAndThreadsAnchor_MaintainBlocksWithoutFalseAdvancement_NoDoubleApplication`) to prove the fix across a real 3-checkpoint chain, disclosing rather than hiding the open Maintain-leg Core JIT issue.

## 17. Exact commands/results

```
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonFirstCheckpointNumericAnchorTests|FullyQualifiedName~LongHorizonNumericAnchorMaterializationE2ETests|FullyQualifiedName~ScheduleRepairRuntimeOrchestratorTests|FullyQualifiedName~RuntimeNotTodayReasonMapperTests|FullyQualifiedName~ScheduleRepairSupersededAndReadCorrectnessTests|FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests|FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests|FullyQualifiedName~NextWindowNumericAnchorSelectorTests"
  → 76/76 passed

dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests"
  → 1/1 passed

dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizon"
  → 1092/1092 passed
```

## 18. Full regression

`dotnet test RunningApp.sln` (repo-approved `xunit.runner.json`, `parallelizeTestCollections: false`) — see final chat report for the completed run's exact count (run in background at doc-authoring time; result reported in the final chat message).

## 19. Build/git diff

`dotnet build RunningApp.sln` → 0 warnings, 0 errors (verified after the fix and again after all test rewrites). `git diff --check` — see final chat report.

## 20. Remaining DecisionRequired items

1. **`Maintain ≤ ProgressAsPlanned` dedicated invariant test** — not built (§13). Blocked on item 2 below: no real HTTP scenario currently produces a successful Maintain activation for this pilot to compare against a successful ProgressAsPlanned activation.
2. **Core/Runway JIT composition rejects a Maintain-carried anchor with `CoreJitContextUnavailable`** (`TenKPreparationRunwayDarkOrchestrator`, `CoreGeneration`/`AllocationPolicy` stage), even though a structurally similar (in fact numerically smaller) Reduce-selected anchor succeeds through the identical orchestrator one transition earlier in the same real chain, and in `LongHorizonNumericAnchorMaterializationE2ETests`. This is a genuine, real, previously-undiscovered issue in a frozen subsystem (Core/Runway JIT composition's own numeric acceptance criteria), found only because this phase's routing fix stopped masking it as a false success. It is explicitly out of this phase's scope (window-advancement lifecycle correctness, not Core JIT composition internals — see the investigation prompt's Section A/G boundaries) and needs its own dedicated investigation.
3. **This pilot's General Endurance phase is fully consumed by its very first window at every race length probed (21, 22, 23, 24, 25, 26, 28, 40 weeks)** — confirmed via direct `TotalWeeks`/`GeneralEnduranceWeeks` inspection, not assumed. Combined with item 2 (JIT composition needing complete evidence at the very first GE→Runway handoff, but tolerating incomplete evidence on subsequent Runway/Core continuations), this means a real Maintain or Reduce decision can only ever be observed succeeding via HTTP for this pilot on the *second* checkpoint onward, never the first, and Maintain specifically cannot currently be observed succeeding at all against this pilot. Since TEN_K/Intermediate/4-day is the *only* pilot this system's Long-Horizon preview endpoint currently accepts (`LONG_HORIZON_PILOT_UNSUPPORTED` on every other combination probed, including `half_marathon`), this is a structural constraint of the currently-testable surface, not a choice made within this test suite. Longer races (≥25 weeks) additionally hit a separate, real `CheckpointWindowNotComplete` (calendar-period-not-ended) condition for multi-week initial windows, not further investigated here (out of scope — a distinct, third potential issue, not conflated with items 1–2 above).

## 21. Final classification

```
ADAPTATION_V1_NUMERIC_ANCHOR_MULTIWINDOW_THREADING_REOPEN_REQUIRED
```

The window-advancement routing defect this phase was chartered to investigate is found, root-caused, fixed with a minimal 3-file/~10-line change, and verified: a genuine Block can no longer masquerade as a successful activation, for the Reduce leg (which now demonstrably succeeds and threads the anchor correctly through real persistence) or the Maintain leg (which now demonstrably reports the correct 409 instead of a false 200). All previously-green regression suites remain green (1092/1092 LongHorizon tests), and the two pre-existing tests this defect had been silently masking are corrected and disclosed rather than left quietly broken.

Full closure of the original 4M.4B.2 confirmation pass remains blocked by a second, genuinely separate, deeper defect (§20 item 2) discovered only because this fix stopped hiding it — reopening for a dedicated Core/Runway JIT composition investigation, not this window-advancement lifecycle phase.

No code committed, no push, Phase 4M.5 not started.

## 22. Addendum — Phase 4M.4B.2B follow-up

§20 item 2 (the Core/Runway JIT "Maintain rejection") was investigated in Phase 4M.4B.2B: `PHASE4M_4B_2B_CORE_JIT_MAINTAIN_CONTEXT_DEFECT.md`. Finding: **no technical defect** — `PriorValidatedCheckpointLoad` is already correctly plumbed into Core/Runway JIT composition, and a real Maintain activation succeeds cleanly when given a sufficiently large carried anchor. The `CoreJitContextUnavailable` Block is the catalog's real, symmetric, pre-existing per-session minimum-volume floor correctly rejecting a numerically small carried anchor (confirmed to reject an equally-small real Reduce-selected anchor identically) — not something specific to Maintain. That investigation also found a genuine, separate, systematic (51% of sampled cases) small-magnitude violation of the `Maintain ≤ ProgressAsPlanned` invariant, caused by the catalog's own session-distance rounding — reported, not clamped. Both are disclosed as product-level DecisionRequired items, not runtime bugs.

Both were subsequently closed as canonical V1 policy in Phase 4M.4B.2C — **Revision 4.1** (`appsel-adaptation-v1-canonical-spec — Revision 4.1.md`): the rounding deviation (max 1.36%, always ≤ the frozen 1.5% PRODUCT DEFAULT tolerance) is accepted, and the target-week-infeasibility Block behavior was confirmed to already exactly match the newly-frozen canonical rule — no production code changed in that closure pass either. See `PHASE4M_4B_2C_ROUNDING_TOLERANCE_AND_INFEASIBILITY_CLOSURE.md`.
