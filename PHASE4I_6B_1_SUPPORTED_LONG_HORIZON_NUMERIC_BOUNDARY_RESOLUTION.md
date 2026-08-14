# Phase 4I.6B.1 — Supported Long-Horizon Numeric Boundary Resolution and Explicit Runtime Activation Scope Decision

## 1. Executive result

A deterministic, real-evidence-backed boundary was found: **`MaxNumericallySupportedTotalWeeks = 21`**. A full real-pipeline scan (192 combinations: 32 horizons × 2 readiness profiles × 3 approved input classes) proved that Core's Week-1 target equals the raw `RecentWeeklyVolumeKm` evidence directly — the same value GE itself starts from — so **any** GE development-progression growth beyond week 1 immediately produces an entry-above-target excess. Horizon 21 (GE=1 week, no progression at all) is the only horizon that passes universally, for every tested class and profile. Horizons 22 and 23 fail universally. Horizon 24 (GE=4, which lands exactly on GE's own recovery week) passes again — a real, confirmed, non-monotonic "isolated later success" — but per this phase's own explicit instruction, isolated later successes must not expand the activated prefix. Every horizon 25-52 fails universally. Zero production code was changed. No commits made.

Final classification:

```
LONG_HORIZON_NUMERIC_ACTIVATION_BOUNDARY_APPROVED_WITH_STRUCTURAL_21_TO_52_SUPPORT_PRESERVED_AND_RUNTIME_NUMERIC_ACTIVATION_LIMITED_TO_21_THROUGH_21

LONG_HORIZON_TOTAL_WEEKS_22_THROUGH_52_REMAIN_COMPOSITION_ELIGIBLE_BUT_NUMERIC_ENVELOPE_INACTIVE_PENDING_FUTURE_VOLUME_ENVELOPE_REDESIGN

21_TO_21_FULL_NUMERIC_EXECUTION_REMAINS_BLOCKED_PENDING_BOUNDARY_RUNTIME_IMPLEMENTATION_AND_FULL_MATRIX_VALIDATION
```

## 2. Selected Path B decision

Per the prompt's own selected product direction: the 21-52 week composition policy (Phase 4I.1) remains structurally valid and entirely unchanged; runtime numeric activation is narrowed to a strict lower subrange (`21..21`), while `22..52` remains structurally defined but numerically inactive. 53+ remains outside the composition window, unchanged.

## 3. Deferred Path A

`TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` was added `OPEN`/`DEFERRED`, capturing (not implementing) future evaluation of: GE growth-to-maintenance transitions, horizon-aware plateau/consolidation rules, alternate Runway duration, Core incoming-load-aware envelopes, long-run maintenance rules, and detraining/excessive-reduction risk boundaries. It does not block the validated 21-week activation.

## 4. Inherited state

Phase 4I.1-4I.6B's composition formula, GE structural/numeric policy, typed decision, catalog capacity, structural materializer, real GE/Runway/Core numeric integration, and the Phase 4I.6B governance findings (Runway's semantic role resolved; GE-side ceiling and Core-target lifting both rejected; no convergence formula approved) are all consumed as given, unchanged.

## 5. Exact scope

`TEN_K__4D__INTERMEDIATE v10`, Race, 10K, 4 days/week, both readiness profiles, horizons 21-52. Below-8/8-14/15-20/53+ unaffected. No generalization to other candidates/distances/levels/frequencies/habit plans.

## 6. Definition of universal support

A total horizon is universally numerically supported when the real, unchanged production pipeline (real GE numeric executor, real Runway numeric/calendar/pace stages, real Core generation, real `RuntimeConditionResolutionService`) produces a complete, fully numeric, workout-bound, dated plan for **every** required approved input class and both readiness profiles — not merely one typical fixture. One unsupported required input class excludes that horizon from the universal boundary. Dimensions tracked: total horizon, readiness profile, weekly-volume baseline class, recent-longest-run class, target-time source; Core target class and calendar pattern were confirmed not to independently vary the result (see §10/§15).

## 7. Production authorities used

`LongHorizonGeStructuralSelector`/`LongHorizonGeNumericExecutor` (real, pure, Phase 4I.6); `CatalogCandidateEligibilityGate`, `RuntimeConditionResolutionService` (with all 4 real resolvers), `DynamicCoreCalendarMaterializationOrchestrator`/`TenKPreparationRunwayCoreGenerator` (real Core generation, Phase 4I.6A); `LongHorizonFullNumericOrchestrator` (real end-to-end confirmation, Phase 4I.6A) — nothing was replaced by a simplified simulator; the only intentional efficiency (§ finding in §7 of the phase's own review) is that Core's target, once proven horizon-invariant, was measured once per (profile, class) rather than once per horizon.

## 8. Approved input classes

**Low**: 15km/5km/3 runs-per-week. **Typical**: 20km/8km/3 (the existing representative pilot input). **High**: 30km/12km/5 (the highest value already proven reachable by Core's own peak-volume-band logic in Phase 4I.6A's own testing — 45km was proven unreachable and excluded as pre-existing-invalid). All three classes use `TargetFinishTimeSource.ProductAverage` (the existing default pilot pace source) with no recent race — recent-race and explicit-target-time paths were not separately scanned across all 32 horizons (a disclosed scope reduction, §39) since pace/target-time source does not affect the GE-exit-vs-Core-target comparison itself (Core's Week-1 *volume* target does not depend on pace source — confirmed by inspection of `StartingVolumeDecision`, which reads only `RecentWeeklyVolumeKm`/`RecentLongestRunKm`/readiness evidence).

## 9. Excluded invalid input classes

45km/week (and similar very-high values) were excluded as pre-existing invalid — Core's own `CatalogVolumeAndLongRunPlanner.ResolvePeak` rejects them with `CatalogVolumeUnreachablePeakRuleException` ("resolved reachable peak is below starting volume"), a pre-existing validation failure unrelated to the Long-Horizon boundary question.

## 10. Core target range

**Real, measured result:** the Core Week-1 target equals `RecentWeeklyVolumeKm` exactly, for all three tested classes (Low→15km, Typical→20km, High→30km), and is confirmed horizon-invariant (measured identically regardless of which total-week anchor was used to derive `CoreStartDate`/`RaceDate`). `MinimumApprovedCoreWeek1Target = 15km` (Low class), `MaximumApprovedCoreWeek1Target = 30km` (High class) across the classes tested. This directly explains the core finding: since GE's own Week 1 also starts at the identical raw evidence value, Core's target provides **zero headroom** for any GE growth.

## 11. Horizon scan method

`LongHorizonNumericActivationBoundaryScanTests.HorizonScan_AllClassesBothProfiles_ProducesUniversalBoundaryEvidence`: for each of 2 profiles × 3 classes (6 real Core-target measurements), iterate every horizon 21-52 (32 horizons), compute GE exit via the real `LongHorizonGeNumericExecutor`, and record pass/fail (`GE exit <= Core target`) — 192 total comparisons, all using real production computation. Full results persisted to `PHASE4I_6B_1_HORIZON_SCAN_MATRIX.csv` (193 rows including header).

## 12. Full 21-52 matrix

See `PHASE4I_6B_1_HORIZON_SCAN_MATRIX.csv` for the complete machine-readable table (columns: `totalWeeks,geWeeks,profile,inputClass,recentWeeklyVolumeKm,recentLongestRunKm,coreWeek1TargetKm,geExitVolumeKm,entryMinusTargetKm,result`). Summary: PASS at 21 and 24 for every class/profile; FAIL at every other horizon (22-23, 25-52) for every class/profile.

## 13. Both-profile results

Identical outcome for `ConsistencyNeeded` and `CoreEntryReady` at every horizon and class — profile has zero effect on the GE-exit-vs-Core-target comparison (expected: GE numeric execution does not branch on profile, per Phase 4I.6's own design; profile affects only workout *content*).

## 14. Low-baseline results

First failure at 22 weeks (GE exit 16km > target 15km). Isolated pass again at 24 weeks (14.5km <= 15km). Fails for every horizon thereafter.

## 15. Typical-baseline results

First failure at 22 weeks (21.5km > 20km). Isolated pass at 24 weeks (19.5km <= 20km). Fails thereafter — matches Phase 4I.6A/4I.6B's own original 28/40/52-week evidence exactly (24.0/41.5/61.0km GE exit against a 20km target).

## 16. High-baseline results

First failure at 22 weeks (32km > 30km). Isolated pass at 24 weeks (29km <= 30km). Fails thereafter.

## 17. Longest-run boundary results

Not separately scanned across all 32 horizons — the GE-exit-vs-Core-target comparison is a total-volume comparison and does not depend on the long-run sub-allocation (disclosed scope reduction, §39); the long-run values are recorded in the diagnostic (Phase 4I.6B's own `LongHorizonRunwayEntryAboveCoreTargetPolicyDiagnosticTests.cs`) but not re-scanned here.

## 18. Missing-input fallback results

Not scanned — GE's own numeric executor requires `RecentWeeklyVolumeKm` and fails closed if absent (Phase 4I.6's own approved behavior, unchanged); this is a pre-existing validation boundary, not part of the Long-Horizon activation-boundary question.

## 19. Product-average results

All three input classes used `TargetFinishTimeSource.ProductAverage` (§8) — this is the class actually scanned across all 32 horizons.

## 20. Recent-race results

Not separately scanned (§8's rationale — pace source does not affect the volume comparison).

## 21. Explicit target-time results

Not separately scanned (same rationale).

## 22. First failure per class

Every one of the 6 (profile × class) combinations first fails at **22 weeks** — no variation across classes or profiles was observed in the first-failure horizon.

## 23. Monotonicity analysis

**Support is confirmed non-monotonic.** 21 passes, 22-23 fail, 24 passes, 25-52 all fail. This directly answers Part 5's own anticipated question: yes, a GE recovery week (landing exactly on GE's final week when `geWeeks % 4 == 0`) can and does restore compatibility in isolation, but does not restore it for surrounding horizons.

## 24. Recovery-position effects

Confirmed the direct cause of the 24-week anomaly: `geWeeks=4` is exactly one complete mesocycle, so GE's own final (4th) week is a recovery week (0.85 × prior peak) — this is what brings the exit value back at-or-below the Core target for that one specific horizon.

## 25. Remainder-position effects

`geWeeks=2,3` (23,22 weeks respectively — wait, `geWeeks = totalWeeks - 20`, so 22→geWeeks=2, 23→geWeeks=3) are ShortExtension weeks with pure upward progression and no recovery cushion, explaining their failure. `geWeeks=5,6,7` (25,26,27 weeks) are one full mesocycle plus a non-recovery remainder, continuing from the mesocycle's peak rather than its recovery — also failing, consistent with the pattern.

## 26. Rounding effects

The near-boundary values (e.g. Low class 24-week: 14.5 vs 15.0, a 0.5km margin) show the 0.5km rounding granularity occasionally produces a very narrow pass, but the fundamental pattern (pass only at 21 and 24) is driven by the recovery-week structure, not rounding artifacts — confirmed by the pattern being identical in direction across all three independently-scaled baseline classes.

## 27. Universal boundary derivation

`MaxUniversallySupportedTotalWeeks = min across all 6 required classes of (FirstNumericallyUnsupportedTotalWeeks - 1) = min(22-1, 22-1, 22-1, 22-1, 22-1, 22-1) = 21`. Per the explicit "prefer the highest contiguous universally supported prefix starting at 21; do not activate isolated later horizons" instruction, 24 is excluded despite passing.

## 28. Technical maximum

**21.**

## 29. Recommended product maximum

**21** — no rationale exists to recommend against activating a horizon proven safe by the full matrix; technical and recommended maxima match exactly, per the phase's own preference.

## 30. Activated numeric range

`21..21` (i.e., 21 weeks only).

## 31. Inactive structural range

`22..52` — composition-eligible (Phase 4I.1's formula still applies: GE=total-20, Runway=8, Core=12), catalog-capable (Phase 4I.4), structurally materializable (Phase 4I.5), but numerically inactive under the current pipeline.

## 32. 53+ distinction

Unchanged — 53+ remains `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW`, a composition-window rejection, never conflated with the new `22..52` numeric-envelope-inactive state.

## 33. New reason taxonomy

`LONG_HORIZON_NUMERIC_ENVELOPE_NOT_SUPPORTED` (or repository-compatible equivalent) is defined by this phase as the conceptual reason for `22..52`, distinct from `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW`, insufficient-time reasons, generic catalog infeasibility, and "generation not yet wired." Production implementation of this reason code remains for a future phase.

## 34. Runtime feasibility guard

Both layers are approved in principle: (1) a static product boundary (`AvailableFullWeeks <= 21`) limiting activated scope, AND (2) the existing real numeric-transition validation (the same check already proven in Phase 4I.6A/4I.6B) must still run for every individual request inside the activated range, since even within `21..21` a sufficiently unusual approved input combination could theoretically still fail — the static boundary narrows scope, it does not replace per-request validation. Neither layer is implemented by this phase.

## 35. Structural-policy preservation

Confirmed unchanged: no file under Phase 4I.1's composition resolver, Phase 4I.4's GE catalog, or Phase 4I.5's structural materializer was touched. Composition arithmetic (`GE=total-20`, `Runway=8`, `Core=12`), GE classification (`ShortExtension`/`FullPhase`), catalog capacity, and structural materialization all remain valid for every horizon 21-52, exactly as before this phase.

## 36. Future volume-envelope redesign

`TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (§3) — OPEN/DEFERRED, not blocking.

## 37. Governance artifacts

`TD-LONG-HORIZON-NUMERIC-ACTIVATION-BOUNDARY-001` — **added CLOSED** (a deterministic boundary was found). `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — **added OPEN/DEFERRED**. `TD-LONG-HORIZON-RUNWAY-ENTRY-ABOVE-CORE-TARGET-001` — updated: non-blocking for the activated 21-week range, fully blocking for 22-52. `TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001`/`TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001` — unchanged, remain OPEN pending Phase 4I.6C implementation. `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — unchanged, remains OPEN.

## 38. Tests

5 new tests in `LongHorizonNumericActivationBoundaryScanTests.cs` (Core-target invariance confirmation; the full 192-combination horizon scan; 3 real-orchestrator boundary-confirmation cases at 21/22/24 weeks). Plus 6 new governance cross-check tests in plan-catalog. All passing.

## 39. Non-claims

This phase does not claim: that 21 weeks is a product-meaningful standalone Long-Horizon offering (GE=1 week involves no meaningful development — it is the sole horizon the pipeline can support without inventing anything, not a claim of product value); that 22-52 weeks are permanently unsupportable (only inactive under the current numeric model); that the longest-run/missing-input/recent-race/explicit-target-time dimensions were exhaustively scanned across all 32 horizons (only the 3 baseline classes × 2 profiles × `ProductAverage` pace source were, since the volume comparison itself does not depend on the unscanned dimensions — a reasoned, disclosed scope reduction, not an oversight); that this is a complete resolution of the underlying product problem (that remains Path A's job).

## 40. Residual blockers

Runtime implementation of the 21-week activation boundary, the `CompositionEligibleButNumericEnvelopeInactive` state, and the `LONG_HORIZON_NUMERIC_ENVELOPE_NOT_SUPPORTED` reason code all remain unimplemented (Phase 4I.6C). `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (Path A) remains the actual foundational fix this finding motivates.

## 41. Final classification

```
LONG_HORIZON_NUMERIC_ACTIVATION_BOUNDARY_APPROVED_WITH_STRUCTURAL_21_TO_52_SUPPORT_PRESERVED_AND_RUNTIME_NUMERIC_ACTIVATION_LIMITED_TO_21_THROUGH_21

LONG_HORIZON_TOTAL_WEEKS_22_THROUGH_52_REMAIN_COMPOSITION_ELIGIBLE_BUT_NUMERIC_ENVELOPE_INACTIVE_PENDING_FUTURE_VOLUME_ENVELOPE_REDESIGN

21_TO_21_FULL_NUMERIC_EXECUTION_REMAINS_BLOCKED_PENDING_BOUNDARY_RUNTIME_IMPLEMENTATION_AND_FULL_MATRIX_VALIDATION
```

## 42. Exact next phase

Given the activated range (21 weeks only) is extremely narrow and of limited standalone product value, the recommended next phase is **not** a rushed Phase 4I.6C implementing only the 21-week boundary, but rather **Phase 4I.6B.2 — Long-Horizon Volume Envelope Redesign Evidence Sourcing (Path A, Governance)**: begin the actual foundational work `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` describes, since the real evidence gathered by this phase shows the entry-above-target state is the norm (not an edge case) for the entire product-intended 21-52 week range, and a narrow 21-week-only runtime activation would deliver minimal product value relative to the effort. If product priorities instead favor shipping the narrow win immediately, Phase 4I.6C (implementing exactly the 21-week boundary, the inactive-range reason code, and the runtime feasibility guard) remains available as a smaller, well-evidenced, low-risk alternative.
