# Phase 4K.5A — Core Week-1 Rolling JIT Evidence Authority Resolution

## 1. Executive result

`TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001` is closed by approving Option A: the latest valid checkpoint `ValidatedSustainableLoad` replaces stale onboarding load fields at the existing Core generator input boundary. The unchanged existing Core generator remains the final Week-1 prescription authority. No rolling runtime, numeric algorithm, persistence, API, Flutter or public-preview behavior is implemented.

## 2. Blocking question

The question was which evidence may authoritatively seed `CoreWeekOneTargetWeeklyVolumeKm` and `CoreWeekOneTargetLongRunKm` when Runway and Core are resolved atomically at Runway entry. Existing production uses original onboarding fields; Phase 4K.5 correctly classified that as legacy behavior rather than rolling approval.

## 3. Inherited policy state

Phases 4K.1–4K.5 establish a full structural roadmap, four-week rolling numeric windows, completed-history-derived validated sustainable weekly/long-run load, fail-closed evidence freshness, atomic Runway/Core target resolution, immutable activated history, future-only context versioning and explicit evidence-authority contracts. Planned future values are not achieved capacity.

## 4. Repository dependencies inspected

Reviewed `DynamicCoreCalendarMaterializationOrchestrator`, `DynamicCoreVolumeAndLongRunOrchestrator`, `TenKPreparationRunwayCoreGenerator`, `PreparationRunwayCoreInputAdapter`, `CatalogPrescriptionContextBuilder`, `CatalogVolumeAndLongRunPlanner`, `PreparationRunwayCoreWeekOneTargetAdapter`, `FourDaySessionDistanceAllocationPolicy`, `PreparationRunwayNumericMaterializer`, calendar/pace continuity validators, all RollingActivation contracts, Phase 4I.6A/4I.6B/4I.6B.1 and Phase 4K.1–4K.5 governance records.

## 5. Core Week-1 target semantics

Core Week-1 is a planned starting prescription produced from entry evidence by the current Core pipeline. It is not the original baseline carried forward, a race-goal-derived volume, a catalog peak floor, or proof of achieved capacity. It may differ from an input long-run scalar because the existing planner reconciles that evidence into its approved 30–36% preferred band and 40% hard cap. Runway converges toward or consolidates at this prescription.

## 6. Candidate authorities

Evaluated: original onboarding evidence; validated load used directly as the target; validated load injected into the current Core generator; planned Runway/GE exit; a bounded onboarding/current hybrid; and an independently invented Core formula. Only injection through the existing generator meets repository support without introducing a new numeric rule.

## 7. Candidate comparison matrix

| Candidate | Repository support | Freshness/capacity alignment | Existing generator | Runway compatibility | Stale risk | New formula | Classification | Decision |
|---|---|---|---|---|---|---|---|---|
| Original onboarding | Current legacy call path only | Stale; predates completed GE | Compatible | Can recreate stale mismatch | High | No | Legacy/provenance | Reject |
| Validated load directly equals target | Validated metric exists, direct-target rule does not | Fresh/actual | Bypasses it | Would collapse source and output authority | Low | Yes, implicit | Unsupported direct formula | Reject |
| Validated load through existing generator | 4K.2–4K.5 plus real input seam | Fresh/actual input, planned output | Reused unchanged | Existing maintenance/build semantics | Low | No | Authoritative input; generator authority | Approve |
| Planned GE/Runway value | Planned value is explicitly non-authoritative | Not achieved capacity | Technically injectable | Recreates prior mismatch risk | High | No | ProvenanceOnly | Reject |
| Bounded hybrid | No canonical blend weight/bound | Partly stale | Would need a new adapter rule | Unproven | Medium | Yes | Unsupported | Reject |

## 8. Onboarding evidence assessment

`OriginalOnboardingEvidence` remains `LegacyCurrentProductionSource` when describing today's non-rolling generator and `ProvenanceOnly` for rolling resolution. It is not authoritative and is not a fallback. No canonical rule requires it after valid completed-history evidence exists; preserving its old method signature is not policy evidence.

## 9. Current validated load assessment

`ValidatedSustainableLoad` represents repeatable current load from actual completed history, not planned targets. It supplies the right entry-evidence semantics, removes stale-onboarding precedence and can be injected without changing Core formulas. It is an input authority; the final target remains the generator output.

## 10. Existing Core generator input-seam analysis

Exact mapping: `ValidatedSustainableLoad.WeeklyVolumeKm` → `GeneratePreviewRequest.RecentWeeklyVolumeKm` and matching `ResolverInputSnapshot.RecentWeeklyVolumeKm`; `LongRunKm` → both `RecentLongestRunKm` fields. Race date, target time/source, recent-race evidence, preferred days and long-run day retain their existing authorities. `CatalogPrescriptionContextBuilder` normalizes these values; `CatalogVolumeAndLongRunPlanner` and session allocation remain unchanged.

## 11. Weekly-volume authority

Precedence is: fresh current `ValidatedSustainableWeeklyVolumeKm`; then a prior still-valid validated checkpoint only where Phase 4K.3 permits it; otherwise `JIT_VALIDATED_LOAD_UNAVAILABLE`/`CORE_JIT_CONTEXT_UNAVAILABLE`. Planned GE exit, product averages, fabricated zero and onboarding evidence cannot supply rolling weekly volume.

## 12. Long-run authority

Fresh `ValidatedSustainableLongRunKm` is mapped with the same checkpoint provenance and evaluated jointly with authoritative weekly volume. The existing Core generator reconciles it through the existing preferred band and 0.40 hard cap, and four-session allocation remains required. A prior still-valid long-run value is permitted only under Phase 4K.3; otherwise `JIT_VALIDATED_LONG_RUN_UNAVAILABLE`. Onboarding cannot override lower current evidence.

## 13. Runs-per-week authority

Actual completed frequency and declared availability are distinct. Completed history is the only permitted rolling source for optional `RecentRunsPerWeek` readiness metadata. An already-validated exact integer may be carried; otherwise the nullable field remains absent because no fractional-to-integer rounding policy exists and none is invented. Current declared four-day availability independently controls the pilot schedule and four-session feasibility. The existing Core volume/long-run planner does not use `RecentRunsPerWeek` in its numeric formulas.

## 14. Target-time and pace-source relationship

`RuntimeConditionResolutionService`, explicit target-time semantics, product-average policy and recent-race freshness remain unchanged. They are re-resolved with the current `AsOfDate`, but they do not change weekly/long-run evidence authority. Diagnostic calls with product-average, recent-race and explicit-target inputs produced identical numeric targets for identical load evidence.

## 15. Runway purpose and interpolation compatibility

Runway and Core use the same source evidence but different responsibilities: Runway materializes the approved structural/workout transition; Core transforms that evidence into its starting prescription. For positive 0.5-km-aligned weekly evidence the current Core planner preserves the weekly scalar, so the numeric Runway is a valid maintenance/consolidation bridge rather than an invented uplift or reduction. Long-run start and target use the same existing band/cap rules. Structural progression, anchor workouts, calendar and pace continuity remain meaningful even when total volume is flat.

## 16. Selected authority

**OPTION A — CURRENT VALIDATED EVIDENCE THROUGH EXISTING CORE GENERATOR.** `LongHorizonEvidenceAuthorityCatalog.CoreWeekOneRollingAuthority` is now `CompletedTrainingHistory / Authoritative`. This is an evidence-source governance correction, not rolling execution code.

## 17. Evidence precedence

Safety conflict first; then fresh current validated weekly and long-run evidence; then prior still-valid validated evidence under Phase 4K.3; otherwise block. Current and prior snapshots are never blended. Onboarding and planned GE exit remain provenance only. Pace/target-time authority is independently resolved and cannot override load evidence.

## 18. Fallback/block behavior

No onboarding fallback is approved. Missing weekly evidence blocks with existing JIT load/context reasons. Missing required long-run evidence blocks per Phase 4K.3. Conflicting evidence uses `JIT_EVIDENCE_CONFLICT_UNRESOLVED`; infeasible session/transition output uses `JIT_SEGMENT_TRANSITION_INFEASIBLE`; unresolved safety uses the shared `SAFETY_REASSESSMENT_REQUIRED`. No new reason code is needed.

## 19. Target locking

The generator output used by any activated Runway week is captured in `LongHorizonLockedCoreWeekOneTarget` and remains immutable for that activated range. Source metadata must identify current/prior validated checkpoint evidence and the context version. No later checkpoint can rewrite completed or activated Runway values.

## 20. Future refresh

A later fresh checkpoint may create a new future-only Core context version for non-overlapping, not-yet-activated weeks. Existing lock/version and compatibility validators decide whether it is accepted; incompatibility blocks rather than invoking a reconciliation formula. The prior locked target remains unchanged.

## 21. Real-pipeline checks

The real unchanged Core pipeline and target adapter were exercised with current validated loads 15/5, 18/7, 20/5, 24/9, 30/12 and 38/16 km; completed-frequency inputs 2–4; both `CAUTION`/`READY` readiness outcomes; and product-average, recent-race and explicit-target contexts. Week-1 weekly outputs were 15, 18, 20, 24, 30 and 38 km; long-run outputs were 5, 6, 6, 8, 10 and 12.5 km. Relative to a 24/9 onboarding baseline, current evidence produced −9 km, 0 km and +6 km target shifts without blending. Existing four-session allocation validated every target; no new formula was used.

## 22. Profile policy

`CONSISTENCY_NEEDED` and `CORE_ENTRY_READY` use identical evidence precedence, input mapping, generator authority, fallback/blocking and lock/version rules. Profile differences remain in workout/content allocation. Real `CAUTION` and `READY` cases both passed the unchanged Core pipeline.

## 23. Non-claims

Validated load is a product metric, not a laboratory test. Core Week-1 remains a prescription, not proof of capacity. Refreshed evidence does not guarantee race readiness or injury safety. Completed volume does not prove adaptation. No progression percentage, convergence multiplier, uplift, reduction, blend weight, medical score or wearable requirement is introduced. Runway interpolation remains planning logic.

## 24. Governance artifacts

`TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001` is `CLOSED`, non-blocking, with Option A and the complete precedence/locking decision recorded in `activation-readiness-risks.json/.md`. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` remains `OPEN`; it records that authority is resolved while checkpoint evaluator/aggregation, GE evidence migration and rolling runtime remain deferred. Aggregate: 36 total, 14 open, 22 closed.

## 25. Deferred implementation

Not implemented: checkpoint evidence aggregation from persisted `TrainingDay` history; exact optional frequency projection; checkpoint evaluator; GE-exit evidence-source migration; validated-evidence input adapter; rolling numeric activation/runtime; persistence; API/public preview; Flutter. Existing production continues its current non-rolling behavior.

## 26. Tests

Focused authority-policy/governance: 39 passed, 0 failed, 0 skipped. Real-pipeline diagnostic: 12 passed, 0 failed, 0 skipped. Existing rolling typed-contract/target-lock/versioning: 52 passed, 0 failed, 0 skipped. Full plan-catalog: 1,063 passed, 0 failed, 0 skipped. Full backend: 2,518 passed, 0 failed, 0 skipped. Release build: 0 warnings, 0 errors.

## 27. Final classification

`LONG_HORIZON_CORE_WEEK_ONE_ROLLING_JIT_EVIDENCE_AUTHORITY_APPROVED`

`CORE_WEEK_ONE_ROLLING_TARGET_USES_CURRENT_VALIDATED_CHECKPOINT_EVIDENCE_THROUGH_THE_EXISTING_UNCHANGED_CORE_GENERATOR_INPUT_BOUNDARY`

`ORIGINAL_ONBOARDING_EVIDENCE_REMAINS_LEGACY_PROVENANCE_ONLY_AND_IS_NOT_AUTHORITATIVE_FOR_ROLLING_CORE_TARGET_RESOLUTION`

`LONG_HORIZON_ROLLING_RUNTIME_REMAINS_DARK_PENDING_CHECKPOINT_EVALUATOR_GE_EVIDENCE_SOURCE_MIGRATION_AND_NUMERIC_RUNTIME_IMPLEMENTATION`

## 28. Exact next phase

**Phase 4K.6 — Rolling General Endurance Numeric Runtime and Initial Window Implementation.** It must implement only the separately authorized dark runtime work and must not infer public preview/persistence activation from this governance closure.
