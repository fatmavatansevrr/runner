# Phase 4G.6A.4D — Dark Full-Week Preparation Runway Materialization

## 1. Executive result

`PRODUCTION_OWNED_DARK_PREPARATION_RUNWAY_FULL_WEEK_MATERIALIZER_IMPLEMENTED_AND_VALIDATED_WITH_CANONICAL_FOUR_ROLE_STRUCTURE`

The production Application assembly now owns a generic, dark-unwired materializer that expands already-resolved block allocations and already-bound progression anchors into ordered, undated four-slot runway weeks. It does not allocate, bind, prescribe, date, persist, or publish a schedule.

## 2. Inherited state

Phase 4G.6A.3/3B owns the dark allocator and canonical block chronology. Phase 4G.6A.4A/4C owns catalog progression capacity for all four blocks. Phase 4G.6A.4B owns exact-prefix anchor binding. Those algorithms, target matrices, capacities, and catalog artifacts remain unchanged.

## 3. Pilot scope

Executable policy is scoped to `TEN_K__4D__INTERMEDIATE v10`, four days per week, PreferredCore 12, runway lengths 3–8, and profiles `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY`. The materializer engine is generic over a stable block-key type; this does not activate or numerically generalize any other candidate.

## 4. Artifacts inspected

Inspection covered the runway allocator, TEN_K policy factory, progression reader, exact-prefix binder, reference validator and their tests; Phase 4F schedule skeleton/materializer/validators; `RUN_LAYOUT_4D v2`; `V1CatalogWorkoutRoleBindingPolicy`; all four runway progression documents; runway workout definitions; live v4 core workout definitions; `ten-k-workout-progression.v5.json`, `ten-k-master.v6.json`, `ten-k-4d-intermediate.v10.json`; Foundation Week 1; 4G.6A.3B/4A/4B/4C documents; dark/public/persistence containment tests and activation-readiness parity tests.

## 5. Progression-step meaning

A runway progression step selects exactly one defining/anchor workout. It is not a complete week. The materializer combines that anchor with the canonical layout and versioned support-workout policy. Existing progression JSON remains one-reference-per-step and unchanged.

## 6. Weekly-layout decision

`RUN_LAYOUT_4D v2` is reused structurally and unchanged:

1. `KEY_SESSION`
2. `EASY_SUPPORT`
3. `EASY_SUPPORT`
4. `LONG_RUN`

Every runway week has exactly four running-session slots. Slot order is structural and does not represent weekdays.

## 7. KEY_SESSION semantic audit

`KEY_SESSION` means the week's single defining or primary slot, not a quality-only physiological category. Evidence: the existing V1 role-binding policy classifies it as `StageControlled`, while workout family/intensity is selected separately; the run-layout and skeleton contracts impose role cardinality but no `QUALITY` family rule. Therefore `EASY_STANDARD v5` may honestly occupy `KEY_SESSION` for Consistency Step 1 and Transition, and as the default defining session when a long-run anchor already occupies `LONG_RUN`. Role-aware catalog validation still limits `KEY_SESSION` to runway-eligible `EASY` or `QUALITY`; it does not weaken validation globally.

## 8. Consistency role mappings

| Step | Anchor | Anchor role | Remaining slots |
|---:|---|---|---|
| 1 | `EASY_STANDARD v5` | `KEY_SESSION` | two `EASY_SUPPORT` → Easy v5; `LONG_RUN` → Long Run v5 |
| 2 | `LONG_RUN_STANDARD v5` | `LONG_RUN` | `KEY_SESSION` and two `EASY_SUPPORT` → Easy v5 |

The Step 1 long run is a controlled support slot, not the progression anchor. Step 2 uses its long-run anchor exactly once and never creates a second long-run slot.

## 9. General Endurance role mappings

Steps 1–5 each bind `LONG_RUN_STANDARD v5` to the single `LONG_RUN` slot. `KEY_SESSION` and both `EASY_SUPPORT` slots use `EASY_STANDARD v5`. The five weeks retain distinct progression step and block-week provenance; later numeric load variation remains deferred.

## 10. AerobicStrength role mappings

Step 1 `AEROBIC_STRENGTH_CONTROLLED_INTRO v1` and Step 2 `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED v1` each occupy `KEY_SESSION` exactly once. Both easy-support slots use Easy v5 and the long-run slot uses Long Run v5. No second quality slot is introduced.

## 11. Transition role mapping

`EASY_STANDARD v5` occupies `KEY_SESSION` as the terminal settle/bridge anchor. Two easy-support slots use Easy v5 and one controlled long-run slot uses Long Run v5. Its block identity, terminal position, progression identity, and later settle trajectory distinguish it from General Endurance and Core Week 1.

## 12. Anchor/support distinction

Every slot carries `Anchor` or `SupportPolicy`. The selected bound anchor is consumed exactly once. Support workouts fill only unoccupied roles and come from `TEN_K_PREPARATION_RUNWAY_SUPPORT_WORKOUT_POLICY v1`: key-session default Easy v5, easy-support default Easy v5, long-run default Long Run v5. Workout identity repetition is structural support reuse, not an additional progression selection.

## 13. Typed contracts

Production-owned internal contracts include:

- `PreparationRunwayWeekMaterializationRequest<TKey>`
- `PreparationRunwayMaterializationBlockBinding<TKey>`
- `PreparationRunwayCanonicalWeeklyLayout`
- `PreparationRunwayBlockWeekRolePolicy<TKey>`
- `PreparationRunwaySupportWorkoutPolicy`
- `PreparationRunwayMaterializedWeek<TKey>`
- `PreparationRunwayMaterializedWorkoutSlot<TKey>`
- `PreparationRunwayMaterializedWeekProvenance`
- `PreparationRunwayWeekMaterializationResult<TKey>`
- `PreparationRunwayWeekMaterializationFailureCode`

No existing dated plan/week/day model was reused because its required `DateOnly` fields would violate this phase's undated contract.

## 14. Materialization algorithm

1. Validate identities, canonical layout, unique allocation keys/orders, non-negative allocations, role policies, bindings, counts, contiguous prefix provenance, and support policy.
2. Canonically sort positive allocations by `CanonicalOrder`; input array order is not authoritative.
3. Iterate each block's already-bound anchor prefix in progression-step order.
4. Resolve anchor role from the explicit block/step policy, never from horizon length or workout family inference.
5. Fill remaining canonical roles from the versioned support policy.
6. Validate every anchor/support reference through the existing runway reference validator and explicit role-family compatibility.
7. Validate four-slot cardinality, one anchor, contiguous global/block ordinals, canonical order, and total count.
8. Return all weeks on success or a typed failure with no partial skeleton.

The engine never recalculates allocation and never invokes the binder.

## 15. Slot ordering

Slots are always `KEY_SESSION`, `EASY_SUPPORT` ordinal 1, `EASY_SUPPORT` ordinal 2, `LONG_RUN`, with global slot ordinals 1–4. These values come through the canonical weekly-layout contract. They are not calendar-day assignments.

## 16. Provenance

Week provenance retains profile, candidate key/version, allocation-policy key/version, support-policy key/version, source layout key/version, block type, block ordinal, canonical block order, progression ID/version and progression step. Slot provenance additionally retains workout key/version, source block/step, anchor-versus-support classification and the exact role/support policy key/version that supplied the slot.

## 17. Matrix materialization proof

| Profile | Runway weeks | Resulting block counts | Materialized weeks |
|---|---:|---|---:|
| Consistency Needed | 3 | C1 GE1 T1 | 3 |
| Consistency Needed | 4 | C1 GE2 T1 | 4 |
| Consistency Needed | 5 | C2 GE2 T1 | 5 |
| Consistency Needed | 6 | C2 GE3 T1 | 6 |
| Consistency Needed | 7 | C2 GE4 T1 | 7 |
| Consistency Needed | 8 | C2 GE5 T1 | 8 |
| Core Entry Ready | 3 | GE1 AS1 T1 | 3 |
| Core Entry Ready | 4 | GE2 AS1 T1 | 4 |
| Core Entry Ready | 5 | GE2 AS2 T1 | 5 |
| Core Entry Ready | 6 | GE3 AS2 T1 | 6 |
| Core Entry Ready | 7 | GE4 AS2 T1 | 7 |
| Core Entry Ready | 8 | GE5 AS2 T1 | 8 |

All rows are produced by the real allocator, real catalog reader, real binder and production materializer. No production route table is hard-coded. Every resulting week has four slots and one selected anchor.

## 18. Typed failures

Implemented failure codes are:

- `InvalidMaterializationRequest`
- `DuplicateBlockAllocation`
- `MissingBlockBinding`
- `BlockBindingMismatch`
- `BindingCountMismatch`
- `InvalidBlockOrder`
- `UnsupportedBlockRolePolicy`
- `AnchorWorkoutReferenceInvalid`
- `AnchorRoleIncompatible`
- `SupportWorkoutReferenceInvalid`
- `WeekRoleCardinalityViolation`
- `NonContiguousWeekNumber`
- `NonContiguousBlockWeekOrdinal`
- `MaterializationInvariantViolation`

Failures expose no partial `Weeks` or `TotalWeekCount`.

## 19. Production/dark/public/persistence classification

- Ownership: `PRODUCTION_OWNED`
- Wiring: `DARK_UNWIRED`
- Public reachability: `PUBLICLY_UNREACHABLE`
- Persistence reachability: `PERSISTENCE_UNREACHABLE`

The materializer is internal, has no DI registration, and is absent from API, PreviewRouting and Persistence sources. Tests directly consume the production implementation; no test-owned duplicate engine exists.

## 20. Catalog and live-bundle containment

No workout, progression, schema, candidate, master, combination, release or bundle artifact changed in 4D. Runway artifacts remain `CATALOG_AVAILABLE`, `DARK_LOADABLE`, `NOT_PUBLISHED_IN_LIVE_CANDIDATE`, and `NOT_RUNTIME_BOUND`.

## 21. Deferred numeric prescription work

Weekly volume, workout distance/duration, long-run distance, pace, intensity dose, recovery duration, load trajectory and runway-to-Core numeric continuity remain unimplemented. Materialized contracts have no such fields.

## 22. Deferred calendar work

No `DateOnly`, weekday, StartDate, RaceDate, PreferredDays, LongRunDayPreference or calendar assignment exists in the materializer contracts or engine. Weeks and slots are purely ordinal.

## 23. Deferred orchestrator wiring

No preview, confirmation, persistence or composition orchestrator invokes the materializer. Future wiring must explicitly combine eligibility/profile resolution, allocation, catalog loading, binding and materialization; that composition is not implied by this standalone dark pass.

## 24. Explicit non-implementation statement

This phase made no allocator or allocation-policy change, no binder algorithm or progression-reader change, no workout/progression/schema change, no catalog publication, no numeric prescription, no date/calendar handling, no public DTO/HTTP mapping, no confirmation behavior, no persistence/migration, no 15–20 activation, no General Endurance 21–52 staging, and no intermediate-distance projection. The pre-existing 4B dark-governance assertion was narrowed only to recognize the explicitly approved dark materializer call to the separate reference validator; binder-engine and progression-reader wiring remain forbidden.

## 25. Exact next phase

Recommended next phase: **Phase 4G.6A.4E — Dark Preparation Runway Volume and Long-Run Continuity Policy**, defining and validating starting-load anchors, per-week progression/maintenance/deload rules and the runway-final-to-Core-Week-1 transition without dates, persistence, public wiring or horizon activation.

Final classification:

`PRODUCTION_OWNED_DARK_PREPARATION_RUNWAY_FULL_WEEK_MATERIALIZER_IMPLEMENTED_WITH_CANONICAL_FOUR_ROLE_STRUCTURE_AND_NO_NUMERIC_OR_RUNTIME_ACTIVATION`
