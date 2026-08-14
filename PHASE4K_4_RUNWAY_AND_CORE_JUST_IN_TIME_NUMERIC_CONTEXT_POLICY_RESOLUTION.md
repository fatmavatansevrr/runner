# Phase 4K.4 — Runway and Core Just-in-Time Numeric Context Policy Resolution

## 1. Executive result

A pure governance/policy-decision phase — zero production code, zero algorithm changes. Unlike Phases 4K.1–4K.3, this phase's central decision could not be reasoned about in the abstract: it required real dependency tracing of the existing, unmodified `LongHorizonFullNumericOrchestrator`/`PreparationRunwayNumericMaterializer`/`TenKPreparationRunwayCoreGenerator` pipeline. That tracing proved Runway's numeric materializer structurally requires a resolved Core Week-1 target as a validated, non-null input, and computes its own weekly values as a direct linear interpolation toward it — a real, hard architectural coupling, not a convenience. This single finding determines every timing decision below: Runway and Core context are resolved atomically at Runway's entry checkpoint; only the Core weeks inside that checkpoint's activation window are exposed as executable, with later Core weeks refreshed fresh at their own future checkpoint; the Core Week-1 target is locked for already-activated Runway weeks while remaining versioned for not-yet-activated ones. Neither segment's underlying evidence source is changed — this phase governs timing only. No commits made.

Final classification:

```
LONG_HORIZON_RUNWAY_AND_CORE_JUST_IN_TIME_NUMERIC_CONTEXT_POLICY_APPROVED

LONG_HORIZON_ROLLING_RUNTIME_REMAINS_BLOCKED_PENDING_TYPED_CONTRACTS_CHECKPOINT_EVALUATOR_ROLLING_NUMERIC_RUNTIME_AND_PERSISTENCE_IMPLEMENTATION
```

## 2. Inherited policy state

Phase 4K.1: 21–52-week structural roadmap, 4-week rolling numeric window, six lifecycle states, mixed-window extension at segment boundaries, Runway/Core just-in-time principle (approved in principle, timing deferred here). Phase 4K.2: validated-load/maintenance formulas. Phase 4K.3: checkpoint evidence, freshness, terminal-window rule, deterministic transition table, GE-scoped reason taxonomy, and the explicit statement that "Runway/Core JIT authority takes over at the segment boundary" with exact timing deferred to this phase.

## 3. Exact scope

In scope: exact Runway/Core JIT activation timing; evidence snapshot and precedence; GE-exit-vs-actual-evidence treatment; Runway/Core authority relationship and immutability; mixed-window policy and atomicity; race-date/time-budget behavior; target-time/pace-source/availability behavior; missing-context behavior; JIT result model; JIT-specific reason taxonomy; profile policy.

Out of scope (deferred): production code, runtime/API/persistence contracts, any GE/Runway/Core algorithm or catalog change, Flutter, public preview, rolling typed contracts, the checkpoint evaluator implementation.

## 4. Repository dependencies reviewed

Direct source inspection (not simulation) of: `LongHorizonFullNumericOrchestrator.cs` (9-stage execution order), `PreparationRunwayNumericMaterializationContracts.cs`/`PreparationRunwayNumericMaterializer.cs` (Core Week-1 target as a required, validated constructor field, used directly in the interpolation formula `unroundedWeekly = startingWeekly + ((targetWeekly - startingWeekly) * progress)`), `PreparationRunwayCalendarComposer.cs`/`PreparationRunwayPaceMaterializer.cs` (both also require and validate the Core Week-1 numeric/pace target), `TenKPreparationRunwayComponentAdapters.cs`/`DynamicCoreCalendarMaterializationOrchestrator.cs` (Core's Week-1 target computed from original onboarding evidence, not GE exit state), `PreparationRunwayCoreWeekOneTargetAdapter.cs` ("Reads the runway boundary from the existing deterministic Core prescription output. It does not independently calculate or assume a Core Week 1 value."), `RuntimeConditionResolutionService.cs` (called once, its inputs — race date, target finish time, `AsOfDate=startDate` — fixed and available from plan creation, independent of GE/Runway output), and PHASE4I_6A/§9 and PHASE4I_6B/line 59's own documented rationale ("recomputing it from Runway's own output would require running Runway before Core, which needs Core's target to exist first — a circular dependency this decision avoids"; "Entry consolidation at the Core target").

**Finding**: Runway's numeric pipeline cannot execute without a resolved Core Week-1 target — confirmed at three levels (orchestration order, API contract validation, and the interpolation formula itself).

## 5. Runway JIT timing candidates

Evaluated: (A) checkpoint immediately before the first Runway week; (B) beginning of the four-week activation window that first contains a Runway week; (C) four weeks before Runway begins; (D) plan creation.

## 6. Approved Runway timing

**(B), stated in exact week-boundary terms**: if the last General Endurance global week is N and the first Runway week is N+1, Runway JIT resolution occurs at the checkpoint that activates the window satisfying `EndWeek >= N+1` and `StartWeek <= N+1`. Remaining GE weeks in that same window retain GE checkpoint authority (Phase 4K.3); Runway weeks use Runway authority. No GE Growth/Maintenance formula ever prescribes Runway numeric values. No separate synthetic checkpoint is inserted merely because a segment boundary falls inside a four-week window — this reaffirms Phase 4K.1's mixed-window rule.

## 7. Core JIT timing candidates

Evaluated: (A) at Runway start, producing Runway and Core together; (B) checkpoint immediately before the first Core week; (C) beginning of the window that first contains a Core week; (D) plan creation.

## 8. Approved Core timing

**(A) — atomic, at Runway entry, for the Core Week-1 target specifically.** This is not a preference but a forced consequence of §4's finding: Runway's numeric materializer cannot run without it. However, Core's *full* internal schedule (all weeks, computed as an unavoidable byproduct of the existing, unmodified `TenKPreparationRunwayCoreGenerator` pipeline) is not entirely "activated" at that moment — only the Core weeks that fall within the *currently activating* rolling window (per the same GE→Runway mixed-window extension rule, applied identically to Runway→Core) are exposed as executable prescriptions. Core weeks beyond that window remain `NumericPending` (Phase 4K.1) and are recomputed fresh, via the same unchanged pipeline, at whichever future checkpoint eventually activates the window containing them.

## 9. Atomic versus separate resolution

Approved as a **hybrid**, not an exclusive either/or: atomic Runway+Core-target locking applies to the Core Week-1 target once it has been used to activate any Runway week (§6/§8) — that target must never be silently replaced for already-activated Runway weeks. Versioned future-only Core refresh applies to Core weeks not yet activated — they may be recomputed with fresher evidence at their own future checkpoint. Both principles apply simultaneously because they govern disjoint week sets. Any incompatibility between an already-locked target and a later refresh's implied target is resolved by the existing GE→Runway/Runway→Core validators and Phase 4I.6B's existing entry-consolidation policy, not a new reconciliation formula.

## 10. JIT evidence snapshot

This phase does not change either segment's evidence source: Runway's starting evidence remains GE exit state (Phase 4I.6A); Core's Week-1 target remains derived from original onboarding evidence (`RecentWeeklyVolumeKm`/`RecentLongestRunKm`/`RecentRunsPerWeek`), confirmed independent by design (§4's circular-dependency rationale). This phase governs only *when* that existing, unmodified mechanism runs within a rolling multi-checkpoint plan. Conceptual snapshot fields: current GE exit/validated evidence, current onboarding evidence (unchanged), current availability/preferred days/long-run-day, the immutable race date, current target-finish-time source and value, current recent-race evidence if fresh, the latest GE checkpoint decision, safety-clear state. Structurally planned future values never enter this snapshot (Phase 4K.3, reaffirmed).

## 11. Evidence precedence

**Weekly-volume (Runway's own entry evidence)**: (a) current validated sustainable weekly load from the last completed GE checkpoint (Phase 4K.2/4K.3) — an explicit, *disclosed future change* from today's code, which currently uses GE's own planned final-week value via `LongHorizonGeExitState.From`; implementing this evidence-source swap for the rolling runtime is deferred, not performed here; (b) prior still-valid validated load if the most recent checkpoint's evidence is stale; (c) otherwise blocked, `JIT_VALIDATED_LOAD_UNAVAILABLE`. Long-run precedence mirrors this identically. Planned GE exit never outranks actual validated evidence; no averaging of incompatible sources. Core's Week-1 target evidence precedence is not redefined (§10) — it remains onboarding-evidence-only, unchanged.

## 12. GE exit versus actual evidence

Runway entry is anchored to current validated actual evidence (`ValidatedSustainableWeeklyVolumeKm`/`ValidatedSustainableLongRunKm`, Phase 4K.2), not the last planned GE peak. Planned GE exit may be retained as provenance/consistency-check context but must never override lower actual capacity nor force Runway upward. A material mismatch requires no new convergence percentage and no new blocking rule: Runway's existing interpolation formula already accepts any starting-evidence magnitude, and the existing Phase 4I.6B "build toward" / "entry consolidation" policies already govern both directions. Only an existing, independent feasibility failure blocks activation — never the magnitude mismatch itself. `LongHorizonGeExitState.From`'s existing planned-progression-only computation remains valid only for the non-rolling/full-upfront path (Phase 4I.6B.1's diagnostic scans, legacy/preview contexts) — not redefined here.

## 13. Runway exit versus Core entry

Core's Week-1 target is not derived from the same evidence used at Runway entry (confirmed independent, intentional, §4). Runway is designed to converge/consolidate toward that target (existing interpolation formula, Phase 4I.6B). Core's target context is frozen for already-activated Runway weeks (§9); it may be recalculated for not-yet-activated Core weeks; such recalculation never invalidates already-activated Runway prescriptions.

## 14. Mixed-segment window behavior

Both the existing GE→Runway mixed window (Phase 4K.1, reaffirmed) and the newly-addressed Runway→Core mixed window follow identical principles: GE weeks use the Phase 4K.3 checkpoint decision; Runway weeks use the approved JIT Runway context; Core weeks (when present) use the atomically co-resolved Core Week-1-adjacent target (§8); each week retains correct segment provenance and unchanged global week numbering; no synthetic recovery week is inserted; Runway/Core weeks are never treated as GE maintenance weeks.

## 15. Atomicity

**Option A approved**: the entire four-week mixed window succeeds or none of it activates. This is not merely preferred for schedule continuity — it is the only option the existing, unmodified architecture can mechanically support, since Core's Week-1 target is a hard, synchronous prerequisite of Runway's own numeric materialization (§4). Partial activation would require deep pipeline changes explicitly out of scope for this phase.

## 16. Race-date behavior

The race date remains structurally authoritative and immutable (Phase 4K.1, reaffirmed). Remaining weeks at JIT activation are calculated by simple subtraction against the immutable race date and current calendar date, reusing the existing `RaceHorizonPolicy`/`PreparationRunwayCalendarAuthorityAdapter` unchanged. Segment durations remain structurally fixed. A delayed checkpoint does not shorten Runway/Core or shift the race date; genuinely insufficient remaining calendar time is resolved via the existing, unmodified `TimeAdequacyResolver`/goal-feasibility taxonomy — no new compressed-Long-Horizon formula is invented.

## 17. Target-time and pace-source behavior

Reuses `RuntimeConditionResolutionService`, the existing target-time-source policy, and the existing recent-race freshness ladder verbatim and unmodified. Weekly-volume freshness (4-week window, Phase 4K.3) is never reinterpreted as race-pace freshness (30/60/90/180-day ladder) or vice versa. Because the resolver service's own inputs are fixed and available from plan creation (§4), the pace/target context is re-resolved (the same unchanged service, called again with a current `AsOfDate`) at the atomic Runway+Core-target checkpoint (§8/§9) rather than reused frozen from initial plan creation; it is re-resolved again only if a later, versioned Core refresh occurs for not-yet-activated Core weeks.

## 18. Availability and calendar behavior

Current `PreferredDays`/days-per-week/long-run-day are reconfirmed at JIT resolution using the identical Phase 4K.3 freshness policy. Four-session feasibility (existing, unmodified `FourDaySessionDistanceAllocationPolicy`) remains required for both Runway and Core numeric generation, exactly as today. Completed history dates never move; only unstarted future weeks may receive revised dates. Exact persistence/rescheduling mechanics remain out of scope.

## 19. Missing/stale/conflicting context

Deterministic `NumericActivationBlocked` with exactly one typed reason for every listed failure mode (§20). Explicitly forbidden fallbacks: planned GE peak, stale original baseline, fabricated zero, generic product-average weekly volume, or GE maintenance rules applied to Runway/Core.

## 20. JIT result model

Conceptual `JitContextApproved`/`JitContextBlocked` outcomes identifying: activation boundary, segments covered, evidence snapshot provenance, weekly-volume/long-run/pace-source/target-time/goal-feasibility authority, a context version/decision identity, whether Runway and Core were resolved atomically, and validators required before activation. `JitContextBlocked` always carries exactly one authoritative reason. Production types are deferred to Phase 4K.5.

## 21. Reason taxonomy

Ten JIT-specific typed reasons approved, additive to and non-duplicative of Phase 4K.3's nine: `RUNWAY_JIT_CONTEXT_UNAVAILABLE`, `CORE_JIT_CONTEXT_UNAVAILABLE`, `JIT_VALIDATED_LOAD_UNAVAILABLE`, `JIT_VALIDATED_LONG_RUN_UNAVAILABLE`, `JIT_PACE_SOURCE_UNRESOLVED`, `JIT_GOAL_FEASIBILITY_UNRESOLVED`, `JIT_AVAILABILITY_INFEASIBLE`, `JIT_EVIDENCE_CONFLICT_UNRESOLVED`, `JIT_ACTIVATION_BOUNDARY_MISSED`, `JIT_SEGMENT_TRANSITION_INFEASIBLE`. `SAFETY_REASSESSMENT_REQUIRED` (Phase 4K.3) is reused directly, retaining the highest evaluation priority. Internal exception wording is never exposed as product copy. The existing, unrelated 53+ horizon rejection is unchanged.

## 22. Immutability/versioning principle

Completed weeks never change. Already-activated numeric weeks, including the Core Week-1 target locked into their interpolation math, never silently change. Future pending weeks may receive a new JIT context. A JIT context used to activate weeks retains provenance. Future-only recalculation produces a new decision identity/version rather than mutating the prior one in place. The structural roadmap remains entirely unchanged. No database table is designed.

## 23. Profile policy

Identical JIT timing, evidence precedence, blocked conditions, and context immutability for both `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY` — the reviewed Runway/Core generator code contains no profile-specific branching in its JIT evidence path.

## 24. Non-claims

Recorded verbatim: JIT resolution does not measure laboratory fitness; actual completed load does not guarantee race readiness; a current Core target is a planning input, not a physiological certainty; product-average target time is not an individualized performance prediction; JIT recalculation does not guarantee injury prevention; planned GE or Runway progression is not proof of adaptation; no medical or wearable requirement is introduced.

## 25. Governance artifacts

`TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001` — added **CLOSED** (`plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}`), full closure note covering all 18 policy points above. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — `requiredResolution` updated, remains **OPEN**. `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — unchanged, remains OPEN. Aggregate sentence updated to "34 risks are now recorded in total: 14 OPEN and 20 CLOSED."

## 26. Deferred decisions

Production evidence-snapshot contract, typed JIT result model, checkpoint evaluator, and the disclosed future `GeExitState` evidence-source change (planned progression → Phase 4K.2 validated load): all deferred to Phase 4K.5 or later. Rolling numeric contracts, rolling runtime, persistence, public preview, Flutter: all open, unimplemented.

## 27. Tests

29 new governance cross-check tests in plan-catalog (`LongHorizonRunwayCoreJitContextGovernanceTests.cs`). One phrase mismatch found and fixed on first run (`ActualValidatedEvidence_OutranksPlannedGeExit` — JSON used uppercase "CURRENT VALIDATED ACTUAL evidence", not the lowercase paraphrase originally written). All passing after the fix: **29/29**. Full plan-catalog suite: **1035 passed, 0 failed, 0 skipped** (1006 baseline from Phase 4K.3 + 29 new). Backend suite not run — zero production files touched; this phase performed real *dependency tracing* (reading existing source) as required repository review, not a code or diagnostic change.

## 28. Final classification

```
LONG_HORIZON_RUNWAY_AND_CORE_JUST_IN_TIME_NUMERIC_CONTEXT_POLICY_APPROVED

LONG_HORIZON_ROLLING_RUNTIME_REMAINS_BLOCKED_PENDING_TYPED_CONTRACTS_CHECKPOINT_EVALUATOR_ROLLING_NUMERIC_RUNTIME_AND_PERSISTENCE_IMPLEMENTATION
```

## 29. Exact next phase

**Phase 4K.5 — Structural Roadmap, Rolling Numeric Activation and JIT Context Typed Contracts** — the next phase in sequence, defining the production typed contracts (evidence snapshot, checkpoint decision, JIT result model, reason-code enums) for everything Phases 4K.1–4K.4 have approved at the policy level.
