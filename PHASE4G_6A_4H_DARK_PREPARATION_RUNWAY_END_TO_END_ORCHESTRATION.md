# Phase 4G.6A.4H — Dark Preparation Runway End-to-End Orchestration

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_END_TO_END_DARK_ORCHESTRATION_IMPLEMENTED_AND_VALIDATED_FOR_15_TO_20_WEEK_HORIZONS_WITHOUT_PUBLIC_ACTIVATION`

One production-owned internal orchestrator now composes every completed Preparation Runway component and the existing authoritative 12-week Core pipeline into atomic dark 15–20-week results. No public route, confirmation path, persistence path, or 21+ staged policy was added.

## 2. Inherited completed components

The orchestrator reuses the canonical horizon classifier through `RaceHorizonPolicy`, typed Core-entry readiness output, TEN_K allocation factory, generic allocator, progression catalog reader/validator, exact-prefix binder, four-slot structural materializer, numeric materializer and Core Week 1 adapter, calendar authority/composer, pace context/Core target adapters and pace materializer, plus the existing dynamic Core skeleton/binding/volume/prescription/calendar pipeline.

## 3. Pilot scope

The executable scope is Race, exact 10.0 km/TEN_K, four days/week, Intermediate, `TEN_K__4D__INTERMEDIATE v10`, preferred Core 12 weeks, runway 3–8 full weeks, combined 15–20 full weeks, and `CONSISTENCY_NEEDED`/`CORE_ENTRY_READY`. Candidate-specific policy was not generalized.

## 4. Artifacts inspected

Inspection covered every 4D–4G production contract/test/document, `CoreHorizonClassifier`, `RaceHorizonPolicy`, `CoreEntryReadinessResolver`, all four runway progression documents and workout references, the complete dynamic Core 4G.5D–5H chain, prescription context and Core target adapters, public horizon routing/confirmation/persistence containment tests, and activation-readiness governance.

## 5. Orchestrator ownership

`TenKPreparationRunwayDarkOrchestrator` is internal to `RunningApp.Application`. Classification: `PRODUCTION_OWNED`, `DARK_INTERNAL_CALLABLE`, `PUBLICLY_UNREACHABLE`, `PERSISTENCE_UNREACHABLE`. It is not DI-registered.

## 6. Input contract

`TenKPreparationRunwayDarkOrchestrationRequest` accepts candidate, StartDate/RaceDate/AsOfDate, PreferredDays, LongRunDayPreference, the already-resolved Core-entry readiness object, the exact shared condition-result collection, internal preview request, resolver snapshot, and unit. It does not accept runway weeks, profile, CoreStartDate, or either Core Week 1 target.

## 7. Authoritative shared context

The readiness object must be the same object present in Core `ConditionResults`. Preview/resolver/orchestration date, distance-family, requested-distance, days/week, and candidate values are checked before execution. A narrow `PreparationRunwayCoreInputAdapter` replaces only StartDate with the authoritative derived CoreStartDate required by existing Core services; every evidence, target-source, preference, race, and readiness value is copied unchanged. Starting-load evidence and pace context are then adapted from the single Core-generated `CatalogPlanPrescriptionContext`, never resolved again.

## 8. Stage execution order

The enforced monotonic order is:

1. Horizon
2. Readiness profile
3. Allocation policy
4. Block allocation
5. All progression loading/validation
6. All exact-prefix binding
7. Structural materialization
8. Authoritative Core generation
9. Numeric materialization
10. Calendar composition
11. Pace materialization
12. Final invariant validation

Typed trace tests reject interleaving or reordering.

## 9. Horizon resolution

The existing canonical `RaceHorizonPolicy.Decide` call site produces the day-accurate `CoreHorizonDecision`. Only `PreparationRunwayPlusCore` decisions with 15–20 full weeks enter. `LeadingPartialDays` remains 0–6 alignment metadata. 8–14, 21–52, and 53+ return typed `HorizonNotDirectRunway` from this dark service without changing their existing public policy.

## 10. Profile resolution

No readiness threshold is duplicated. Evaluated `READY` maps to `CORE_ENTRY_READY`; evaluated `CAUTION` or `NOT_READY` maps to `CONSISTENCY_NEEDED`, including approved race Missing/no-base behavior. Other statuses/values fail `ReadinessProfileUnavailable`.

## 11. Allocation

`TenKPreparationRunwayAllocationPolicyFactory` supplies unchanged eligibility, minima, maxima, weights, priorities, and canonical order. `PreparationRunwayBlockAllocationEngine` consumes authoritative runway weeks. Success requires the allocation sum to equal the horizon-derived 3–8 count.

## 12. Progression loading

`TenKPreparationRunwayProgressionPolicyFactory` contains the four typed block-to-reference mappings in one versioned location. `PreparationRunwayBlockProgressionCatalogReader` loads every positive block; block identity and every workout reference are validated through the existing validator. All progressions finish loading before binding begins.

## 13. Binding

`PreparationRunwayBlockWorkoutBindingEngine` consumes each final allocation count and loaded definition. It retains exact-prefix behavior, capacity checks, ordered references, and original typed failures. No route table or repeat rule exists in the orchestrator.

## 14. Structural materialization

`PreparationRunwayWeekMaterializer` receives allocations, bindings, the canonical four-day layout, block-role policies, and support policy. Success contains exactly 3–8 contiguous weeks with KEY_SESSION, two EASY_SUPPORT slots, and LONG_RUN plus full block/workout provenance.

## 15. Core generation

The existing `DynamicCoreCalendarMaterializationOrchestrator` and its unmodified 4G.5D–5H dependency chain generate the authoritative 12-week Core from derived CoreStartDate and the same request evidence. The result supplies the undated and dated skeletons, volume/long-run plan, prescription context, and final paced Core plan. No second Core generator was authored.

## 16. Numeric materialization

`PreparationRunwayStartingLoadEvidenceAdapter` reads normalized readiness from that Core prescription context. `PreparationRunwayCoreWeekOneTargetAdapter` reads the same Core volume plan. The existing numeric policy/materializer produces weekly, long-run, and four-slot distances; the terminal Transition exactly matches Core Week 1.

## 17. Calendar composition

`PreparationRunwayCalendarAuthorityAdapter` derives runway/Core boundaries from the canonical horizon. The existing composer dates the continuous runway-plus-Core structural sequence once, preserves cross-boundary spacing, global/local numbering, PreferredDays and LongRunDayPreference, and proves no overlap/gap. Alignment remainder days contain no session.

## 18. Pace materialization

The pace context is adapted from the same Core `ResolvedPrescriptionPaceSource`; product/user/Missing provenance is retained. The existing pace materializer applies controlled non-race-specific runway effort and compares Transition with the authoritative Core Week 1 pace target. It recalculates neither distance nor date.

## 19. Final invariant validation

`TenKPreparationRunwayFinalInvariantValidator` checks allocation closure, positive-block progression/binding closure, four-role weeks, numeric sums and boundary equality, calendar counts/continuity, typed pace continuity, Transition→Foundation order, global/local numbering, preferred/long-run days, remainder containment, total week/session counts, forbidden runway pace absence, and nested provenance completeness.

## 20. Failure propagation

The top-level result retains typed stage, top-level code, originating component, original typed code/exception class, reason, and deterministic trace up to failure. Any failure returns no horizon/profile/allocation/binding/week/Core/numeric/calendar/pace/final artifacts. Progression, binding, Core, numeric, calendar, pace, and final-invariant injection tests prove atomic stopping.

## 21. Provenance and trace

Trace entries are typed records with sequence, stage, subject, source key/version, outcome, and structured details. Final slots retain nested input/profile/allocation/progression/anchor-or-support/numeric/calendar/pace provenance rather than flattening it into one string.

## 22. 15–20 proof matrix

The matrix proves 15=3+12, 16=4+12, 17=5+12, 18=6+12, 19=7+12, and 20=8+12 for both profiles. Additional cases cover Provided, Missing and NoRecentRunningBase readiness; recent-race, approved user-target, product-average and Missing pace sources; remainder 0–6; multiple weekday layouts; midweek starts; month/year crossings; determinism; and collection-order independence.

## 23. Invalid horizon behavior

14 weeks is rejected by this orchestrator and remains owned by the existing standalone Core path. 21, 52, and 53 weeks are rejected internally as not direct-runway scope. No General Endurance staged segment is materialized and no public error behavior is changed.

## 24. Existing 8–14 regression

The orchestrator has no public call site and never replaces the existing Core path. Existing dynamic/fixed Core, public preview, confirmation, persistence, schedule identity/date/distance/pace, and horizon tests remain the regression authority and are rerun in focused and full backend suites.

## 25. Public/persistence containment

Executable source scans prove the orchestration symbol is absent from API, PreviewRouting, Services, and Persistence. Production sources contain no EF/DbContext, public payload materializer, or plan-generation interface dependency. Eight older “zero consumer” tests were narrowly reconciled to permit only the new `PreparationRunwayOrchestration` folder while continuing to scan every live/public/persistence location.

## 26. Deferred activation

No controller, routing, preview, confirm, persistence, DI, or horizon-gate connection is made. Activation requires a separate explicit decision covering public request behavior, lifecycle status, response compatibility, confirmation/persistence semantics, operational rollout, and rollback.

## 27. Explicit non-implementation statement

No horizon/readiness/allocation/binder/structural/numeric/calendar/pace/Core algorithm or policy changed. No workout, progression document, schema, DTO, database model, migration, 15–20 public behavior, 21–52 staged General Endurance logic, 53+ policy, or non-TEN_K behavior was implemented. No new risk record was required because no unresolved composition or safety gap remained.

## 28. Exact next phase

Recommended activation phase: **Phase 4G.6B — Scoped Public Activation of 15–20-Week TEN_K Preparation Runway Composition**. It must remain a separate authorization boundary and must not be inferred from this dark readiness result.
