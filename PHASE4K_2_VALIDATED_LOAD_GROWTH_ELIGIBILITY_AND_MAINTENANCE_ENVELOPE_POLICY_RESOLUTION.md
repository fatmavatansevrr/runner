# Phase 4K.2 — Validated Load, Growth Eligibility and Maintenance Envelope Policy Resolution

## 1. Executive result

A pure governance/policy-decision phase — zero production code, zero diagnostics/simulations. Phase 4K.1 approved a rolling 4-week numeric activation window but explicitly deferred *what a checkpoint actually computes*. This phase resolves that gap: a formal definition of validated sustainable weekly load and validated long-run capacity (derived exclusively from real, completed `TrainingDay` evidence — never planned/incomplete values), the conditions under which a plan is `GrowthEligible` vs. `MaintenanceOnly`, the exact maintenance anchor and four-week maintenance shape (reusing, never redefining, the existing Phase 4I.2A 0.85 recovery formula and existing long-run-share rule), and four distinct blocked-activation reason families. No new numeric constant, threshold, or algorithm was invented; every formula reuses existing repository authorities (`AdherenceRatePercent`, `VolumeSafetyPolicy.Default`, `FourDaySessionDistanceAllocationPolicy`, Phase 4I.2A's recovery formula). Detailed checkpoint evidence/freshness/state-transition thresholds and exact adherence percentages remain explicitly deferred to Phase 4K.3. No commits made.

Final classification:

```
LONG_HORIZON_VALIDATED_LOAD_GROWTH_ELIGIBILITY_AND_MAINTENANCE_ENVELOPE_POLICY_APPROVED

LONG_HORIZON_ROLLING_NUMERIC_RUNTIME_REMAINS_BLOCKED_PENDING_CHECKPOINT_EVIDENCE_STATE_TRANSITION_JUST_IN_TIME_CONTEXT_AND_TYPED_CONTRACT_IMPLEMENTATION
```

## 2. Inherited rolling-activation policy (Phase 4K.1, unchanged)

Structural roadmap (21–52 weeks) vs. bounded 4-week rolling numeric activation window, six lifecycle states, checkpoint authority scoped to the next window only, missing-evidence fail-closed principle, structural immutability, Runway/Core just-in-time principle. This phase fills in the checkpoint's *evaluation content* only — it does not alter any of the above.

## 3. Exact scope

In scope: validated sustainable weekly load formula, validated long-run formula, adherence's role, `GrowthEligible` conditions, `MaintenanceOnly` conditions, maintenance anchor, four-week maintenance shape, maintenance long-run envelope, consecutive-maintenance-window behavior, partial-window behavior, blocked-activation reason families, rounding, profile policy.

Explicitly out of scope (deferred): production runtime code; detailed checkpoint evidence aggregation, freshness, and state-transition thresholds (Phase 4K.3); exact adherence percentage threshold (Phase 4K.3); Runway/Core exact just-in-time timing (Phase 4K.4); any change to GE/Runway/Core numeric algorithms themselves.

## 4. Repository evidence reviewed

`TrainingDay.cs` (`Status`, `ActualDistanceKm`, `ActualDurationMin`, `ActualPaceMinKm`, `PlannedDistanceKm`, `PlannedDurationMin`, `CompletedAt`), `TrainingDayStatus.cs` (`Planned, Completed, Missed, Skipped, PendingConfirmation, Rescheduled, SoftMissed`), `WorkoutLog.cs`, `NotTodayDecision.cs` (default `ResultingStatus = Missed`), `QueryAndMutationServices.cs:668-680`'s existing `AdherenceRatePercent = completedRuns/totalRuns*100.0` (also exposed on `ProfileOverviewResponse.cs:23`), `VolumeSafetyPolicy.Default` constants, Phase 4I.2A's recovery formula, `FourDaySessionDistanceAllocationPolicy`. No existing repository term was found for "sustainable load" / "validated load" / "progression eligibility" — this genuinely new terminology was required; `AdherenceRatePercent` itself was reused verbatim rather than redefined.

## 5. Evidence-window decision

The most recent activated numeric window (≤4 weeks). Only `TrainingDay` rows with `Status = Completed` contribute actual distance; `Missed/Skipped/SoftMissed` (including `NotTodayDecision`'s default `ResultingStatus = Missed`) count as zero and are never excluded from the denominator, and are never inferred from `PlannedDistanceKm`. For `ShortExtension` partial GE windows (1–3 weeks, never containing a recovery week per Phase 4I.2), the full available window is used directly.

## 6. Actual-completion policy

Evidence is drawn exclusively from `TrainingDay.ActualDistanceKm` where `Status = Completed`. Planned-but-incomplete or future values are never substituted for actual evidence at any point in the formula.

## 7. Candidate weekly-load formulas considered

Candidates considered: (A) mean across the full window including the recovery week, (B) mean across only the non-recovery weeks, (C) last-completed-week value only, (D) peak completed week only. (A) understates true capability (recovery week is a deliberate reduction, not a failure). (C)/(D) are single-sample and noise-prone. (B) was selected.

## 8. Approved validated-load formula

`ValidatedSustainableWeeklyVolumeKm` = mean of actual summed weekly distance across the non-recovery weeks of the evidence window, rounded via the existing 0.5km increment. Raw evidence is always aggregated before rounding or progression is applied.

## 9. Recovery-week treatment

The recovery week is excluded from the average (its planned reduction is by design, not a completion failure), but its own actual completion still counts toward adherence. The pre-recovery planned peak is never automatically assumed achieved without its own completed evidence.

## 10. Validated long-run formula

`ValidatedSustainableLongRunKm` = mean of completed `LONG_RUN` `ActualDistanceKm` across the same non-recovery evidence weeks, additionally bounded by the existing `LongRunHardCapShare(0.40)`-of-weekly-volume ceiling, so a single anomalously long completed run can never independently define the next block's long run. Zero completed long runs with no usable prior evidence produces `VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE` (blocked).

## 11. Adherence role

Reuses the existing `AdherenceRatePercent` verbatim as a Growth-eligibility confidence gate — not folded numerically into the weekly-load average, not weighted differently by session role (Easy/Key/Long treated uniformly). No exact adherence threshold is invented here — explicitly deferred to Phase 4K.3.

## 12. Growth eligibility

`GrowthEligible` requires: validated weekly load exists; validated long-run evidence exists; adherence is sufficient (threshold deferred to Phase 4K.3); no unresolved safety conflict (taxonomy deferred); four viable sessions producible (reusing the existing `FourDaySessionDistanceAllocationPolicy` feasibility check, unchanged); the next progression, computed via the existing GE development-progression authority (0.07/0.08/2.5km), remains inside the existing peak-volume band. `GrowthEligible` does not mean an increase is mandatory — the existing authority may still produce zero delta. No second progression formula was created.

## 13. Maintenance eligibility

`MaintenanceOnly` is selected when evidence exists but doesn't support Growth, or when no new evidence exists but a prior validated sustainable load remains usable. Explicitly not a fallback for invalid inputs, unresolved safety signals, four-session infeasibility, total absence of validated load evidence, or an unsupported segment transition — those fail closed instead.

## 14. Maintenance anchor

`MaintenanceAnchorWeeklyVolumeKm = ValidatedSustainableWeeklyVolumeKm` directly (the actual completed average), subject to existing rounding/peak-band/session-feasibility rules. Explicitly not a percentage of original baseline, planned peak, Core Week-1 target, GE recovery volume, or the 0.53 Taper multiplier. The anchor never exceeds the last validated completed average.

## 15. Four-week maintenance shape

Weeks 1–3 flat at the maintenance anchor (only long-run-share/rounding-driven micro-variance, never a real change). Week 4 reuses the existing, unmodified 0.85 recovery formula (Phase 4I.2A) applied to the maintenance anchor as its own `PREVIOUS_NON_CUTBACK_VOLUME`-equivalent reference. No maintenance week may produce a new planned weekly-volume peak. The recovery formula itself is not redefined.

## 16. Maintenance long-run envelope

Reuses the existing long-run-share rule (0.33 selection share, clamped to [0.30, min(0.36, 0.40)]) applied to the flat maintenance anchor. Since the anchor doesn't grow, the derived long run algebraically cannot produce a new peak either — zero new share/constant invented. Week 4's long run reuses the existing 0.33-of-recovery-total rule unchanged.

## 17. Consecutive maintenance windows

Approved — multiple `MaintenanceOnly` windows may occur back-to-back. The anchor is refreshed each window from that window's own actual completed evidence (never inherited unchanged), preventing both automatic upward drift and repeated recovery-week-averaging decline. Evidence-age/staleness rules are deferred to Phase 4K.3. A Runway segment boundary always takes precedence when reached.

## 18. Partial-window behavior

The Growth/Maintenance decision applies only to the remaining GE portion of a window. When a window crosses from GE into Runway, Runway weeks are governed exclusively by the existing, unmodified Runway numeric authority (Phase 4I.6A) — GE maintenance rules are never applied to Runway weeks. No synthetic recovery week is ever inserted merely to pad a window to four weeks.

## 19. Blocked-activation conditions

Four distinct reason families, never collapsed into one generic reason: `VALIDATED_LOAD_UNAVAILABLE`, `VALIDATED_LONG_RUN_EVIDENCE_UNAVAILABLE`, `NUMERIC_WINDOW_INFEASIBLE` (four-session infeasibility per existing session-minimum rules), `SAFETY_REASSESSMENT_REQUIRED` (taxonomy deferred to Phase 4K.3). Maintenance is explicitly not a universal fallback for any of these. A plan's very first numeric window continues to use the existing Phase 4I.6 onboarding-evidence baseline mechanism unchanged — this checkpoint policy applies to subsequent windows only.

## 20. Rounding

Reuses the existing 0.5km increment and existing session-distribution policy unchanged. Raw actual evidence is always aggregated before rounding or progression is applied. No new rounding increment introduced.

## 21. Profile policy

Identical validated-load formula, Growth/Maintenance eligibility, and maintenance anchor apply to both `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY` profiles. Profile differences remain confined to workout content selection, not the numeric policy defined here.

## 22. Simulations

None run — this phase is a pure policy-shape decision, consistent with Phase 4K.1's precedent. No diagnostic or simulation code was written or executed.

## 23. Non-claims

Recorded verbatim in the governance artifact: validated sustainable load is a product decision metric, not a laboratory fitness measurement; completed volume does not prove injury safety; maintenance does not guarantee fitness preservation; no universal adherence percentage is claimed; no universal weekly-volume increase rule is introduced; maintenance is not Taper; maintenance is not GE recovery; pain/medical symptoms require separate safety handling (deferred); wearable/HRV data is not required.

## 24. Governance artifacts

`TD-LONG-HORIZON-VALIDATED-LOAD-AND-MAINTENANCE-ENVELOPE-001` — added **CLOSED** (`plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}`), with a full closure note covering all 15 policy points above. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — `requiredResolution` updated to record that the maintenance formula/validated-load/eligibility/envelope are now approved, remains **OPEN** (checkpoint evidence contracts and Runway/Core JIT timing still pending). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — unchanged, remains OPEN. Aggregate sentence updated to "32 risks are now recorded in total: 14 OPEN and 18 CLOSED."

## 25. Deferred decisions

Checkpoint evidence, freshness, and numeric state-transition policy (Phase 4K.3); exact adherence threshold (Phase 4K.3); safety-conflict taxonomy (Phase 4K.3); Runway/Core exact just-in-time context policy (Phase 4K.4); rolling runtime implementation; persistence; public preview; Flutter.

## 26. Tests

24 new governance cross-check tests in plan-catalog (`LongHorizonValidatedLoadMaintenanceEnvelopeGovernanceTests.cs`), each asserting exact substrings of the real JSON `closureNote` text (verified via direct grep against the artifact before finalizing assertions, avoiding the phrase-mismatch cycle seen in earlier phases). All passing: **24/24**. Full plan-catalog suite: **972 passed, 0 failed, 0 skipped** (948 baseline from Phase 4K.1 + 24 new). Backend suite not run — zero production files touched (pure governance/JSON/MD/test-only phase, matching the Phase 4K.1 precedent).

## 27. Final classification

```
LONG_HORIZON_VALIDATED_LOAD_GROWTH_ELIGIBILITY_AND_MAINTENANCE_ENVELOPE_POLICY_APPROVED

LONG_HORIZON_ROLLING_NUMERIC_RUNTIME_REMAINS_BLOCKED_PENDING_CHECKPOINT_EVIDENCE_STATE_TRANSITION_JUST_IN_TIME_CONTEXT_AND_TYPED_CONTRACT_IMPLEMENTATION
```

## 28. Exact next phase

**Phase 4K.3 — Checkpoint Evidence, Freshness and Numeric State Transition Policy Resolution** — the next governance phase in sequence, resolving the detailed checkpoint evidence aggregation, adherence threshold, evidence-freshness rules, and safety-conflict taxonomy this phase deliberately deferred.
