# Phase 4G.4B — Neutral Typed Preparation Runway Contracts

## 1. Objective

Introduce a dark, immutable contract and structural-validation layer that can
represent successful, unresolved, unsupported, not-applicable, and invalid
Preparation Runway planning outcomes. No allocator or live consumer exists.

**These contracts make unresolved runway decisions representable. They do
not resolve them. Contract readiness does not imply allocator readiness.**

## 2. Scope

This phase adds internal contracts, four static structural validators, unit
tests, and this document. It does not add route or block selection, workout
selection, prescriptions, materialization, composition execution, date
assignment, persistence, public DTOs, DI, frontend behavior, catalog content,
TDs, or horizon activation.

## 3. Source limitations inherited from Phase 4G.4A

The repository's current imported canonical-decision artifact does not contain
the cited doc13 runway sections. Runway vocabulary and routing prose remain
import candidates. The renamed
`PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md` records the
source limitation and unresolved contradictions. No source rule is made
executable here.

## 4. Vocabulary decisions

`PreparationRunwayBlockType` is a dedicated enum containing only:

- `Consistency`
- `GeneralEndurance`
- `AerobicStrength`
- `PreSpecificTransition`

It contains no race-core member, partial member, or
`AerobicStrengthLight`. `PreparationRunwayPrescriptionProfile` contains
`Standard` and `Light`. Light is structurally representable only with
Aerobic Strength, but no planner produces it and its meaning remains
`DECISION_REQUIRED`.

`PreparationRunwayExperienceReference` carries both a vocabulary discriminator
and the original value. `PlanCatalogRunningExperience/New` and
`BackendRunningBackground/Beginner` remain different values; there is no
implicit mapper.

No generic `Phase` base type exists and no runway type derives from or reuses
a core-phase type.

## 5. Contract inventory

All types are internal and live in
`RunningApp.Application.RuntimeCatalog.Schedule.PreparationRunway`.

| Contract | Purpose |
|---|---|
| `PreparationRunwayBlockType` | runway-only block identity |
| `PreparationRunwayPrescriptionProfile` | Standard/Light content-profile identity, not content logic |
| `PreparationRunwayPlanningStatus` | explicit result status |
| `PreparationRunwayPlanningReason` | machine-readable reason |
| `PreparationRunwayNeedLevel` | distinguishes NotEvaluated from Low/Moderate/High |
| `PreparationRunwayExperienceReference` | vocabulary-qualified experience without mapping |
| `PreparationNeedProfile` | observed need states without inference or ordering |
| `PreparationRunwayContext` | inputs plus race-anchored derived facts |
| `PreparationRunwayBlockAllocation` | one full-week-only block entry |
| `PreparationRunwayAllocation` | complete full-week allocation facts |
| `PreparationRunwayLeadingPartialSpan` | separate materialization-boundary metadata |
| `RacePlanCompositionMetadata` | composition facts, not execution |
| `PreparationRunwayPlanningResult` | status, optional allocation, and findings |
| `PreparationRunwayValidationResult` | structural validation result |

Context field classification:

| Field | Classification |
|---|---|
| `Distance`, `TargetRunsPerWeek`, `StartDate`, `RaceDate` | INPUT |
| `Experience` | UNRESOLVED_CROSS_LAYER_VALUE |
| `NeedProfile`, recent volume/long-run/frequency | OPTIONAL_EVIDENCE; supplied, never inferred |
| `CoreStartDate`, `RunwayDays`, `FullRunwayWeeks`, `LeadingPartialDays` | DERIVED and cross-validated |
| `PreferredCoreWeeks` | INPUT from the selected catalog template |
| `CompositionType` | INPUT composition fact; does not select a route |

`PreparationNeedProfile` fields are ConsistencyNeed, FrequencyReadiness,
VolumeReadiness, LongRunReadiness, QualityReadiness, and CoreEntryReadiness.
Each is `NotEvaluated`, `Low`, `Moderate`, or `High`. No formula populates them.

Block allocation fields are BlockType, PrescriptionProfile, FullWeekCount,
SequenceIndex, optional MinimumWeeks/MaximumWeeks, optional SourceRuleId, and
optional DecisionTrace. Optional constraints are enforced only when supplied.

## 6. Status and reason model

Statuses:

- `Planned`
- `DecisionRequired`
- `Unsupported`
- `NotApplicable`
- `InvalidInput`

Typed reasons:

- `ExperienceMappingUnresolved`
- `AerobicStrengthLightSemanticsUnresolved`
- `ShortRunwayRouteSelectionUnresolved`
- `BlockCountRouteConflict`
- `LongRunwayCapacityExceeded`
- `LongRunwayContinuationPolicyMissing`
- `ReadinessRoutingUnresolved`
- `InsufficientEffectiveRunway`
- `InvalidDateRange`
- `InvalidDerivedDuration`
- `InvalidBlockAllocation`

Optional human-readable detail is separate. TD identifiers are not reasons.
Only `Planned` may carry an executable allocation. DecisionRequired,
Unsupported, and InvalidInput require a reason of their respective category.
NotApplicable cannot claim runway composition.

## 7. Date semantics

Validators enforce the inclusive race-anchored relationship:

```csharp
preferredCoreDays = PreferredCoreWeeks * 7;
CoreStartDate = RaceDate.AddDays(-(preferredCoreDays - 1));
RunwayDays = CoreStartDate.DayNumber - StartDate.DayNumber;
FullRunwayWeeks = RunwayDays / 7;
LeadingPartialDays = RunwayDays % 7;
```

Total available days are inclusive StartDate through RaceDate. These checks
do not change `RaceHorizonPolicy` or activate 15+ week composition.

## 8. Allocation invariants

- Full runway weeks are nonnegative.
- Partial days are 0–6.
- `RunwayDays == FullRunwayWeeks * 7 + LeadingPartialDays`.
- Every block contains more than zero full weeks.
- Sequence indices are unique and contiguous from zero.
- Block full-week sum equals FullRunwayWeeks.
- Zero full weeks cannot contain blocks.
- Explicit min/max constraints, when supplied, are ordered and respected.
- Route order is not hard-coded.
- Partial days cannot compensate for a missing full week.

The last two sum invariants explicitly guarantee that a leading partial span
cannot reduce the full-week allocation of the first runway block.

## 9. Partial-span semantics

`PreparationRunwayLeadingPartialSpan` is separate from allocation blocks. It
contains inclusive dates, 1–6 days, inherited block/profile identity, and the
facts `AllowsZeroSessions=true` and `AllowsQualityProgression=false`.

It is absent at zero partial days and required at 1–6. It must inherit the
first full-week block without replacing it. No `PartialBlock` or
`PartialPhase` taxonomy exists. The contract selects no sessions and permits
zero sessions.

## 10. Validation rules

- `PreparationRunwayContextValidator`: dates, inclusive core anchor, derived
  duration arithmetic, target frequency, and nonnegative optional evidence.
- `PreparationRunwayAllocationValidator`: block totals, sequence, optional
  constraints, Light-profile pairing, and partial-span separation.
- `RacePlanCompositionMetadataValidator`: preferred-core days, total inclusive
  days, runway derivation, and partial-span presence.
- `PreparationRunwayPlanningResultValidator`: status-specific rules plus
  composition/allocation validation.

Validators return findings and never normalize input. They contain no
coaching or allocation decision logic.

## 11. Explicit unresolved decisions

- New/Beginner cross-layer mapping.
- Aerobic Strength Light semantics.
- Route selection for `<=3`, `4–6`, `7–10`, and `11+`.
- Advanced/Experienced two-route-versus-three-block contradiction.
- Long-runway maximum exhaustion and continuation/repetition/maintenance.
- Readiness interaction and minimum effective runway.
- Whether experience is a prior or the sole route input.

## 12. Dark reachability

The contracts may be referenced by their validators and tests, but no
production-reachable orchestration path may consume them. Source-based tests
scan Application, API, Infrastructure, and Persistence outside the dedicated
contract folder. There is no endpoint, handler, generator, persistence path,
DI registration, public response, or support-registry activation.

## 13. Test coverage

Tests cover exact and partial date boundaries, 20-week arithmetic,
off-by-one core starts, invalid dates/evidence, allocation sums and sequences,
zero/negative weeks, optional constraints, partial-span presence and content
restraint, all status families, taxonomy separation, explicit experience
vocabulary preservation, source neutrality, and dark reachability.

No test encodes a fixed experience route, New-to-Beginner mapping, automatic
Light selection, repeated mesocycles, continuation policy, readiness-based
selection, or workout/session selection.

## 14. Files changed

- renamed the Phase 4G.4A audit without substantive edits;
- added `PreparationRunwayContracts.cs`;
- added `PreparationRunwayValidators.cs`;
- added `PreparationRunwayContractsTests.cs`;
- added this document.

No catalog, canonical source, TD inventory, frontend, endpoint, DI,
persistence, public contract, allocator, materializer, or composer changed.

## 15. Entry criteria for Phase 4G.4C

A decision/governance pass may follow because unresolved outcomes now have
typed representation. Allocator implementation remains blocked until every
decision in §11 has an approved outcome. Phase 4G.4C must not infer those
answers from the existence of these contracts.
