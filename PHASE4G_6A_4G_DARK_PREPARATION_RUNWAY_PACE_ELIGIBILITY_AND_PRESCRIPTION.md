# Phase 4G.6A.4G — Dark Preparation Runway Pace Eligibility and Prescription

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_PACE_ELIGIBILITY_AND_PRESCRIPTION_IMPLEMENTED_DARK_WITH_NON_RACE_SPECIFIC_CORE_ENTRY_CONTINUITY`

The dated 3–8-week Preparation Runway can now receive a production-owned, typed pace contract without changing allocation, workouts, distance, dates, public routing, or persistence. The implementation reuses the already-resolved Core pace-source type and the existing `CatalogPacePrescription` representation. All runway prescriptions are controlled `EffortOnly` contracts: no numeric easy/long-run derivation, goal pace, race-specific pace, threshold pace, duration synthesis, or pace ramp was introduced.

## 2. Inherited state

Phases 4G.6A.1–4F already supply authoritative horizon/date decisions, profile/block allocation, exact workout binding, four-session structural weeks, weekly/slot/long-run distance, exact Core Week 1 numeric continuity, calendar assignment, and a continuously dated runway-plus-Core result. This phase consumes that result without changing it.

## 3. Pilot scope

Scope is exactly `GoalType=Race`, `RequestedTargetDistanceKm=10.0`, `CanonicalDistanceFamily=TEN_K`, four days/week, Intermediate, `TEN_K__4D__INTERMEDIATE v10`, 12-week Core, 3–8 full runway weeks, total dark horizon 15–20 weeks, and the `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY` profiles. No numeric policy is generalized to another candidate.

## 4. Artifacts inspected

The review covered the 4D–4F allocator, binder, structural/numeric materializers, calendar composer and tests; `PaceSourceResolver`; `GoalFeasibilityResolver`; `ResolverInputSnapshot`; `CatalogPrescriptionContextBuilder`; `CatalogSessionPrescriptionPlanner`; `CatalogSessionPrescriptionContracts`; `DynamicCoreSessionPrescriptionOrchestrator`; Core pace tests and Phase 4F.7C/4G.5G documents; `EASY_STANDARD` v4/v5, `LONG_RUN_STANDARD` v4/v5, both AerobicStrength v1 definitions, FARTLEK, THRESHOLD_TEMPO, and GOAL_PACE_TEN_K; target-source/product-average decisions; activation-readiness risks; and Core Week 1 prescribed output.

## 5. Authoritative pace source

No second resolver exists. `PreparationRunwayPaceContextAdapter` accepts an existing `ResolvedPrescriptionPaceSource` plus the already-carried `TargetFinishTimeSource`. It never calls `PaceSourceResolver`, `GoalFeasibilityResolver`, or `RuntimeConditionResolutionService`.

| Input | Runway pace use | Core pace use | Missing behavior |
|---|---|---|---|
| Complete recent-race distance/time/date | Retain resolved `RecentRace` provenance; no runway numeric projection | Existing resolver selects source; non-goal Core workouts still use effort-only | Not treated as recent-race evidence |
| User target finish time/source | Retain provenance only when the existing chain returns a prescription-usable target source | Goal pace is restricted to eligible Core workout/feasibility | The currently reachable no-independent-evidence result is unsupported and fails typed |
| Product-average target/source | Retain explicit `ProductAverage` provenance; never claim athlete evidence | Existing goal-feasibility rule may accept it for Core | Never relabel as user or recent-race evidence |
| Existing Core pace context | Authoritative typed input and Core Week 1 comparison target | Owns current prescription behavior | Missing/invalid context fails typed |
| RunningBackground/profile | No raw pace derivation; profile only determines inherited workouts | Existing pipeline context only | No experience-only pace |
| Recent volume/longest run | Not used for pace | Volume pipeline only | No pace default derived from load |

## 6. Pace evidence states

- `PROVIDED_RECENT_RACE`: allowed as provenance; controlled effort remains the prescription because no approved easy/long-run race-result conversion exists.
- `TARGET_TIME_USER_PROVIDED`: allowed only if the already-resolved Core context is prescription-usable. The normal user-defined target with no independent evidence remains `NotEvaluated`/unresolved and fails `PACE_SOURCE_UNSUPPORTED`.
- `TARGET_TIME_PRODUCT_AVERAGE`: allowed as explicitly product-supplied provenance, never individual evidence; it cannot create runway goal pace.
- `MISSING_PACE_EVIDENCE`: allowed as explicit controlled effort via the existing `EffortOnly` representation.
- `PACE_SOURCE_UNSUPPORTED`: atomic typed failure; no fabricated fallback.

## 7. Core Week 1 pace target

`PreparationRunwayCoreWeekOnePaceAdapter` reads the first `FOUNDATION` week from the existing `CatalogPrescribedPlan`; it does not recalculate pace. Current pilot behavior is:

| Role | Workout | Representation | Contract | Target-time dependent |
|---|---|---|---|---|
| KEY_SESSION | EASY_STANDARD | EffortOnly | `EASY` | No |
| EASY_SUPPORT ×2 | EASY_STANDARD | EffortOnly | `EASY` | No |
| LONG_RUN | LONG_RUN_STANDARD | EffortOnly | `LONG_RUN_EASY_CONTROLLED` | No |

Foundation Week 1 is not race-specific. Each target retains the existing session `PaceSourceProvenance`.

## 8. Easy workout pace contract

`EASY_STANDARD v5` receives the existing canonical `CatalogPacePrescriptionKind.EffortOnly` representation with effort label `EASY` and source selection `EffortOnly`. It is controlled, deterministic, non-race-specific, identical across runway blocks, and exactly compatible with Core Foundation Week 1 easy slots. No new zone or numeric range was added.

## 9. Long-run pace contract

`LONG_RUN_STANDARD v5` receives `EffortOnly` / `LONG_RUN_EASY_CONTROLLED`, matching the current Core planner. There is no fast finish, target-goal dependency, seconds/km offset, or distinct invented zone. Long-run distance progression remains owned by 4E.

## 10. AerobicStrength pace contract

`AEROBIC_STRENGTH_CONTROLLED_INTRO v1` receives the catalog-authored effort label `CONTROLLED_AEROBIC_POWER_INTRO`; `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1` receives `CONTROLLED_AEROBIC_POWER_PROGRESSED`. Both are typed `EffortOnly`, below threshold/race-specific work, and have no numeric target. The progressed step is more demanding because its catalog workout structure adds progressed main-set semantics and recovery; this phase does not invent a faster pace, repetitions, or duration.

## 11. Consistency profile behavior

`CONSISTENCY_NEEDED` keeps one stable contract per workout identity across Consistency, General Endurance, and Transition. Missing evidence stays controlled effort. No two-week pace ramp, candidate-average substitution, narrowing, or profile branch exists in the materializer; progression remains volume/load driven.

## 12. CoreEntryReady profile behavior

General Endurance remains controlled. AerobicStrength uses only its two catalog-authored controlled-aerobic-power labels. Transition returns to EASY/LONG_RUN controlled contracts. A provided source changes trace provenance, not workout pace; any narrower range would require an existing producer, which the repository does not have.

## 13. Final-to-Core pace continuity

For both profiles and every runway length 3–8, the terminal Transition week is compared by structural role and role ordinal against authoritative Core Week 1. Compatibility uses current typed semantics: `EffortOnly` plus the same canonical effort label. The four checks are EASY key, two EASY supports, and controlled easy long run. No numeric tolerance was invented. All 12 profile/length combinations pass.

## 14. Pace representation

The implementation reuses `CatalogPacePrescription`, `CatalogPacePrescriptionKind`, and `CatalogPaceSourceSelection`. Runway output uses the existing `EffortOnly` union case with null exact/range fields. It does not create free-text instructions, a parallel pace union, a new zone, or public preview types.

## 15. Pace provenance

Every paced slot retains the original resolved source, evidence state, `TargetFinishTimeSource`, resolver decision/status/value/goal-feasibility/reason, exact workout rule/version, derivation statement, non-applicable rounding statement, and Core-continuity comparison. Product-average remains distinguishable from user-provided and recent-race evidence. Trace remains internal.

## 16. Typed contracts

New internal contracts are `PreparationRunwayPaceContext`, `PreparationRunwayPacePolicy`, `PreparationRunwayPaceMaterializationRequest`, `PreparationRunwayPacedSlot`, `PreparationRunwayPacedWeek`, `PreparationRunwayCoreWeekOnePaceTarget`, `PreparationRunwayPaceContinuityAnalysis`, provenance/trace records, and an atomic success/failure result. Existing dated/numeric objects are wrapped, not rewritten.

## 17. Materialization algorithm

The materializer validates candidate/policy/context/Core target; indexes exact workout ID/version rules; attaches one effort-only contract to every dated runway slot; rejects forbidden semantic tokens and unsupported rules; rechecks distance/date/workout identity preservation; compares the final Transition by role with Core Week 1; emits deterministic trace; and returns either the complete paced runway or no runway output.

## 18. Matrix proof

The focused matrix covers both profiles × runway lengths 3–8. Every week retains four paced slots; EASY and LONG_RUN contracts are consistent; one-step and two-step AerobicStrength cases resolve; no horizon-length pace table or branch exists; final Transition is Core-compatible; repeated and reversed-policy-input calls are value-identical; and dates, distances, workout references, and segment provenance remain inherited unchanged.

Representative evidence fixtures cover recent 10K, recent 5K (source reuse only, no projection), approved user target, product-average target, Missing, unsupported user target, fast target, conservative target, missing volume evidence, both AerobicStrength shapes, and numeric rounding-edge inputs (rounding is explicitly non-applicable to effort-only pace).

## 19. Unsupported pace behavior

An unresolved or otherwise unsupported authoritative source returns `PaceSourceUnsupported` with no partial weeks. A contradictory product/user/Missing state returns `PaceEvidenceContradiction`. Missing workout policy, non-effort rule, invalid Core target, forbidden goal/threshold/race-specific semantic, invalid range, or continuity mismatch likewise fails atomically. No HTTP mapping was added.

## 20. Typed failures

The result vocabulary contains: `InvalidPaceMaterializationRequest`, `PaceContextUnavailable`, `PaceSourceUnsupported`, `PaceEvidenceContradiction`, `CoreWeekOnePaceTargetUnavailable`, `WorkoutPacePolicyMissing`, `WorkoutPacePolicyIncompatible`, `RaceSpecificPaceNotAllowedInRunway`, `GoalPaceNotAllowedInRunway`, `ThresholdPaceNotAllowedInRunway`, `AerobicStrengthPaceUnresolved`, `PaceRangeInvalid`, `PaceContinuityViolation`, `DistanceMutationDetected`, `DateMutationDetected`, and `PaceMaterializationInvariantViolation`.

## 21. Distance/date preservation

Before success, the materializer compares every slot’s distance, date, and workout ID/version with the original 4F result. Weekly totals and long-run distances remain in the wrapped original records and are never recalculated. Duration is not added because effort-only contracts require no mechanical duration.

## 22. Production/dark/public/persistence classification

- Ownership: `PRODUCTION_OWNED`
- Wiring: `DARK_UNWIRED`
- Public reachability: `PUBLICLY_UNREACHABLE`
- Persistence reachability: `PERSISTENCE_UNREACHABLE`

There is no DI registration, preview call, public DTO mapping, DbContext reference, confirmation path, or persistence path.

## 23. Existing 8–14 regression result

This phase does not edit the live horizon gate or standalone Core routing. Existing public 8–11 and 13–14 behavior remains governed by the current activated horizon policy, and 12-week behavior remains the existing live Core behavior. The runway pace layer has no public call site and therefore cannot activate 15–20 weeks.

## 24. Deferred orchestrator wiring

The phase deliberately does not compose allocator → binder → structural → numeric → calendar → pace through an end-to-end production orchestrator. It establishes the last standalone typed layer and containment proof only. Later orchestration must pass already-resolved pace context and authoritative Core prescribed output; it must not invoke resolvers again.

## 25. Explicit non-implementation statement

No allocator/profile/block policy, workout reference, progression, schema, weekly/slot/long-run distance, date, calendar rule, Core prescription, public DTO, endpoint, horizon gate, confirmation behavior, persistence model, migration, 21–52 General Endurance staging, 53+ behavior, or intermediate-distance projection was implemented or modified. No risk registry entry was needed: no new unresolved safety gap was found, and existing pace-source TDs remain unchanged.

## 26. Exact next phase

Recommended next phase: **Phase 4G.6A.4H — Dark Preparation Runway End-to-End Orchestration and Invariant Validation**. It should compose the already-completed 3B/4B/4D/4E/4F/4G production layers without public activation or persistence and prove one atomic 15–20-week dark result.
