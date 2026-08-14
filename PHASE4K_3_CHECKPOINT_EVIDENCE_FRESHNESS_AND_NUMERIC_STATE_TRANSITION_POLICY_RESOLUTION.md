# Phase 4K.3 — Checkpoint Evidence, Freshness and Numeric State Transition Policy Resolution

## 1. Executive result

A pure governance/policy-decision phase — zero production code, zero diagnostics/simulations. Phases 4K.1/4K.2 approved the rolling-activation shape and the validated-load/maintenance formulas but explicitly deferred *what a checkpoint actually evaluates*. This phase closes that gap: a conceptual evidence snapshot sourced exclusively from real completed `TrainingDay` history; freshness rules for weekly-volume, long-run, adherence, availability, and safety evidence (an explicit 4-week activation-window product default where no repository rule already exists, and explicit non-reuse of the unrelated 30-day race-pace ladder); a terminal-window rule; explicit partial/missed/NotToday treatment; a deterministic, non-invented adherence confidence gate; a fail-closed safety rule; an eight-case, priority-ordered, mutually exclusive state-transition table; and a nine-code reason taxonomy. No adherence percentage, evidence-age duration beyond the existing 4-week default, or medical-scoring system was invented. Runway/Core exact just-in-time timing remains explicitly deferred to Phase 4K.4. No commits made.

Final classification:

```
LONG_HORIZON_CHECKPOINT_EVIDENCE_FRESHNESS_AND_NUMERIC_STATE_TRANSITION_POLICY_APPROVED

LONG_HORIZON_ROLLING_RUNTIME_REMAINS_BLOCKED_PENDING_RUNWAY_CORE_JUST_IN_TIME_CONTEXT_POLICY_TYPED_CONTRACTS_AND_RUNTIME_IMPLEMENTATION
```

## 2. Inherited policies

Phase 4K.1: structural roadmap vs. 4-week rolling numeric activation window, six lifecycle states, checkpoint authority scoped to the next window only, missing-evidence fail-closed principle, structural immutability, Runway/Core just-in-time principle. Phase 4K.2: `ValidatedSustainableWeeklyVolumeKm`/`ValidatedSustainableLongRunKm` formulas, `GrowthEligible`/`MaintenanceOnly` conditions, the maintenance anchor and four-week shape, four blocked-reason families. This phase fills in the checkpoint's *evaluation mechanics* — evidence, freshness, terminal-window timing, and the deterministic transition logic — without altering any of the above.

## 3. Exact scope

In scope: checkpoint evidence snapshot; weekly-volume/long-run/adherence/availability/safety freshness; terminal-window rule; partial-completion treatment; missed/NotToday treatment; adherence confidence gate; safety handling; Growth/Maintenance/Blocked sufficiency; the deterministic state-transition table; the reason taxonomy; initial-window distinction; segment-boundary authority; profile policy.

Explicitly out of scope (deferred): production runtime code, API/persistence contracts; GE/Runway/Core algorithm changes; the Phase 4K.2 maintenance calculations themselves (reused, not reimplemented); Runway/Core exact just-in-time timing (Phase 4K.4); public preview; Flutter.

## 4. Repository evidence reviewed

`TrainingDayStatus.cs` (`Planned, Completed, Missed, Skipped, PendingConfirmation, Rescheduled, SoftMissed` — confirmed no distinct "Partial" or "Terminal" construct exists in code). `NotTodayDecision.cs`/`NotTodayDecisionStatus.cs` (`Pending, Confirmed, Expired, Cancelled` — distinct from `TrainingDay.Status`). `AdaptationAction.cs`/`AdaptationExplanationKeys.cs` (`Rescheduled` confirmed reserved-but-never-assigned anywhere in the live backend). `PaceSourceResolver.cs`'s `ComputePaceRecencyConfidence` — the repository's only existing evidence-age rule (30/60/90/180-day ladder), confirmed scoped exclusively to `RecentRaceDate` pace evidence and explicitly documented as trace-metadata only, never reused here for weekly-volume/long-run evidence. `TrainingPlan.InjuryNotes` (free-text) and `AdaptationExplanationKeys.SafetyValidatorBlocked` (a defined-but-unimplemented string constant — no `SafetyValidator` class exists). `FourDaySessionDistanceAllocationPolicy.Allocate` (throws `CatalogSessionPrescriptionInfeasibleException` on infeasibility — the existing feasibility contract reused unchanged). A full-repo search for an accepted `AdherenceRatePercent` threshold found none.

## 5. Checkpoint evidence snapshot

A conceptual, policy-only field set: `checkpointDate`, `activatedWindowStartWeek`/`EndWeek`, `completedWindowWeeks`, `actualWeeklyVolumes`, `completedLongRuns`, `completedRunsCount`, `plannedRunsCount`, `AdherenceRatePercent`, `missedSessionCount`, current-availability signal, safety/pain signal, prior validated sustainable weekly load and long run, current segment, weeks until Runway. It distinguishes evidence observed in the current window from previously validated evidence, from original onboarding evidence, and from structurally planned future values — future planned values (null per Phase 4K.1) never enter the snapshot.

## 6. Weekly-volume freshness

Expected source: the most recent activated four-week block (fewer weeks for a partial GE window). Older completed weeks from a prior activated block may not fill missing positions in the current window — an incomplete current block is either used as-is (subject to the Growth confidence gate, §12) or the prior block's own validated load is used in full as a Maintenance anchor, never blended. Maximum age before evidence is stale: the activation window itself (4 weeks) — an explicit Appsel product default, since no existing repository rule addresses weekly-volume evidence age.

## 7. Long-run freshness

Reuses the same 4-week activation-window default as weekly-volume evidence — not the unrelated 30-day race-pace ladder (a different evidence type). When no completed long run exists within the fresh window, per Phase 4K.2's `VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE`, this phase confirms no safe bounded-long-run substitute is defined — the default is `NumericActivationBlocked`, not `MaintenanceOnly`.

## 8. Adherence freshness

Must represent the most recent activated block only and must not be inherited indefinitely from older blocks.

## 9. Terminal-window rule

A checkpoint may issue a next-window decision only when the activated window's calendar period has ended and every planned session has reached a terminal outcome (`Completed`, `Missed`, `Skipped`, or `SoftMissed` — `PendingConfirmation` and the never-assigned `Rescheduled` are explicitly non-terminal). Unresolved sessions are never silently counted as zero or Completed. A non-terminal window issues no Growth decision; it may still issue `MaintenanceOnly` using only a prior validated anchor (never blended with the incomplete window's partial data) when a prior anchor is available and safety is clear; otherwise it is `NumericActivationBlocked`/`CHECKPOINT_WINDOW_NOT_COMPLETE`. Partial segment-boundary windows use the identical rule scoped to their own, shorter session set.

## 10. Partial-completion treatment

`TrainingDayStatus` has no distinct "partial" value — a session completed below its planned distance is still `Status=Completed`, and its actual (lower) `ActualDistanceKm` already lowers the Phase 4K.2 weekly/long-run averages. No separate percentage-complete threshold is invented. Repeated partial completions are self-correcting through the load average and the per-week evidence check (§12); they never silently unlock Growth.

## 11. Missed and NotToday treatment

Missed contributes zero actual distance and lowers adherence (Phase 4K.2, unchanged). NotToday-resolved sessions (default `ResultingStatus=Missed`) are treated identically to unexplained Missed sessions for numeric purposes. `Rescheduled` is confirmed reserved-but-unimplemented — this phase documents, for any future implementer, that it must be treated as non-terminal until it resolves to a genuine terminal status, never auto-treated as Completed. An unexplained unresolved session after the calendar period has ended blocks the checkpoint per §9. Multiple misses never directly force Maintenance or Blocked on their own — they operate only through the validated-load-availability (Phase 4K.2) and adherence-confidence (§12) channels; no role-specific or count-based miss weighting is invented.

## 12. Adherence confidence gate

No accepted numeric adherence threshold exists anywhere in the repository, and none is invented here. Approved deterministic **Growth confidence condition**: every non-recovery week of the activated window contains at least one Completed session with usable actual evidence (the terminal-window rule, §9, must also hold). Approved **Maintenance confidence condition**: a validated sustainable weekly load is available — from the current window if §9 holds but the Growth condition doesn't, or from a prior fresh-enough window if the current window has no usable evidence — and no safety/feasibility conflict exists. Approved **Blocked confidence condition**: no validated load is available from either the current or any prior fresh window. Growth is never silently defaulted to; Maintenance is never an automatic consequence of failing Growth without its own validated-load check.

## 13. Safety handling

The repository contains no structured pain/injury/difficulty taxonomy (only free-text `InjuryNotes` and the unimplemented `SafetyValidatorBlocked` key) — no medical-scoring system is invented. An unresolved safety-critical signal (once a future mechanism can produce one) always produces `NumericActivationBlocked`/`SAFETY_REASSESSMENT_REQUIRED`, evaluated with the highest priority in the transition table, before any other condition — it can never produce `GrowthEligible`, and this phase does not approve it silently downgrading to `MaintenanceOnly`. Non-safety difficulty/fatigue signals are explicitly out of scope (no repository policy exists to draw that distinction).

## 14. Growth sufficiency

Terminal window (§9); validated weekly load and validated long-run evidence available from the current window (§6/§7); Growth confidence condition (§12); safety clear (§13); four-session feasibility via the existing, unmodified `FourDaySessionDistanceAllocationPolicy.Allocate`.

## 15. Maintenance sufficiency

A validated weekly load available from the current or a prior fresh window; safety clear; feasibility holding for that anchor.

## 16. Blocked activation

Any of: no valid current-or-prior load anchor; required long-run evidence unavailable with no approved substitute; feasibility failure; unresolved safety signal; window non-terminal with no usable prior anchor.

## 17. State-transition table

Eight deterministic, mutually exclusive cases, evaluated in this priority order (first match wins, guaranteeing exactly one output):

1. `safetyClear=false` → `NumericActivationBlocked`/`SAFETY_REASSESSMENT_REQUIRED`.
2. `windowTerminal=false` and no usable prior anchor → `NumericActivationBlocked`/`CHECKPOINT_WINDOW_NOT_COMPLETE`.
3. `windowTerminal=false` and a prior anchor is available and safe → `MaintenanceOnly`/`CHECKPOINT_WINDOW_NOT_COMPLETE` (prior anchor only).
4. No validated weekly load from current or any prior fresh window → `NumericActivationBlocked`/`VALIDATED_LOAD_UNAVAILABLE`.
5. Validated long-run evidence unavailable, no approved substitute → `NumericActivationBlocked`/`VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE`.
6. `numericFeasible=false` → `NumericActivationBlocked`/`NUMERIC_WINDOW_INFEASIBLE`.
7. All sufficiency conditions hold but Growth confidence (§12) fails → `MaintenanceOnly`/`ADHERENCE_CONFIDENCE_INSUFFICIENT_FOR_GROWTH` (current window) or `MaintenanceOnly`/`CHECKPOINT_EVIDENCE_STALE` (prior-window reuse, no current evidence at all).
8. All conditions including Growth confidence hold → `GrowthEligible`/no reason.

## 18. Reason taxonomy

Nine typed reasons: `CHECKPOINT_WINDOW_NOT_COMPLETE`, `CHECKPOINT_EVIDENCE_STALE`, `VALIDATED_LOAD_UNAVAILABLE`, `VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE`, `ADHERENCE_CONFIDENCE_INSUFFICIENT_FOR_GROWTH`, `MAINTENANCE_ANCHOR_UNAVAILABLE` (reserved for a Maintenance anchor failing its own rounding/peak-band/feasibility re-check), `NUMERIC_WINDOW_INFEASIBLE`, `SAFETY_REASSESSMENT_REQUIRED`, `EVIDENCE_CONFLICT_UNRESOLVED` (reserved for future evidence-source disagreement). `GrowthEligible` carries no failure reason; `MaintenanceOnly` always carries a non-failure decision-provenance reason; `Blocked` always carries exactly one authoritative reason — never a generic collapse of safety and missing-evidence causes; internal exception wording is never exposed as product copy.

## 19. Initial activation distinction

The plan's very first numeric window uses the existing, unmodified Phase 4I.6 onboarding-evidence baseline mechanism (established by Phase 4K.2), not this checkpoint's freshness/terminal-window/adherence-confidence machinery, because no completed-history evidence yet exists. It remains subject to the same safety and feasibility validation defined here. The production contract for this distinction (e.g. an `InitialActivation` state) is deferred to a future implementation phase.

## 20. Segment-boundary authority

The checkpoint state decision governs only the remaining GE portion of a window — ordinary GE Growth/Maintenance logic never prescribes Runway or Core weeks. Runway/Core's own just-in-time authority (Phase 4K.1 principle) takes over the moment a Runway segment boundary is reached; if that JIT context is unavailable, activation becomes `NumericActivationBlocked`, but the exact reason code and evidence precedence for that specific case are explicitly deferred to Phase 4K.4.

## 21. Profile policy

Identical freshness, terminal-window, partial/missed/NotToday treatment, adherence-confidence rule, safety handling, and state-transition table for both `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY`. No profile-specific decision threshold is approved.

## 22. Simulations

None run — a pure policy-shape decision, consistent with Phase 4K.1/4K.2's precedent. No diagnostic or simulation code was written or executed.

## 23. Non-claims

Recorded verbatim: checkpoint state is a product-planning decision, not a medical diagnosis; adherence does not directly measure fitness; completed volume does not prove injury safety; stale evidence cannot prove detraining; `MaintenanceOnly` does not mean the athlete regressed; `GrowthEligible` does not guarantee improved performance; no universal adherence percentage is claimed (none was found, none was invented); HRV and wearable data are not required.

## 24. Governance artifacts

`TD-LONG-HORIZON-CHECKPOINT-EVIDENCE-STATE-TRANSITION-001` — added **CLOSED** (`plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}`), full closure note covering all 18 policy points above. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — `requiredResolution` updated to record checkpoint evidence/freshness/state-transition as approved, remains **OPEN** (Runway/Core JIT timing and runtime implementation still pending). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — unchanged, remains OPEN. Aggregate sentence updated to "33 risks are now recorded in total: 14 OPEN and 19 CLOSED."

## 25. Deferred decisions

Runway/Core exact just-in-time context policy (Phase 4K.4); production evidence-snapshot contract, typed reason-code enum, and runtime checkpoint evaluator (future implementation phase); rolling numeric contracts/runtime/persistence; public preview; Flutter.

## 26. Tests

34 new governance cross-check tests in plan-catalog (`LongHorizonCheckpointEvidenceStateTransitionGovernanceTests.cs`), each asserting exact substrings copied directly from the authored JSON `closureNote` text — verified on the first run with zero phrase-mismatch fixes needed. All passing: **34/34**. Full plan-catalog suite: **1006 passed, 0 failed, 0 skipped** (972 baseline from Phase 4K.2 + 34 new). Backend suite not run — zero production files touched (pure governance/JSON/MD/test-only phase).

## 27. Final classification

```
LONG_HORIZON_CHECKPOINT_EVIDENCE_FRESHNESS_AND_NUMERIC_STATE_TRANSITION_POLICY_APPROVED

LONG_HORIZON_ROLLING_RUNTIME_REMAINS_BLOCKED_PENDING_RUNWAY_CORE_JUST_IN_TIME_CONTEXT_POLICY_TYPED_CONTRACTS_AND_RUNTIME_IMPLEMENTATION
```

## 28. Exact next phase

**Phase 4K.4 — Runway and Core Just-in-Time Numeric Context Policy Resolution** — the next governance phase in sequence, resolving the exact Runway/Core just-in-time checkpoint timing and evidence precedence this phase and Phase 4K.1 deliberately deferred.
