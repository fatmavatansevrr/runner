# Phase 4F.7C Pace Source and Session Prescription

## Scope

Phase 4F.7C adds a dark/internal session-prescription layer after corrected 4F.7B weekly volume and long-run planning. It creates one internal prescription per bound session and then discards the result. It does not populate public preview payloads, snapshots, hashes, confirmation, persistence, migrations, routing, or publication state.

## Contracts

Internal contracts include `CatalogPrescribedPlan`, `CatalogPrescribedWeek`, `CatalogPrescribedSession`, `CatalogWorkoutPrescription`, `CatalogPrescriptionSegment`, `CatalogPacePrescription`, `CatalogDurationPrescription`, `CatalogDistancePrescription`, `SessionPrescriptionDecisionTrace`, and `SessionVolumeAllocationTrace`.

## Allocation Policy

Policy key: `V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY`
Policy version: `1`

The 4F.7B long-run distance is reserved first. Residual weekly volume is allocated to the KEY_SESSION and two EASY_SUPPORT sessions with:

- minimum EASY_SUPPORT distance: `1.5 km`;
- minimum KEY_SESSION distance: `3 km`;
- key target: 50% of residual, bounded by feasibility;
- remaining residual split across the two easy sessions;
- nearest-0.5 km rounding with final adjustment on the second easy support session.

The policy is deterministic and does not change workout identity.

## Pace Sources

`GOAL_PACE_TEN_K` may use exact target goal pace only when goal feasibility is `REALISTIC` or `CHALLENGING` and `TargetFinishTimeSeconds` exists. Pace is `TargetFinishTimeSeconds / GoalDistanceKm`.

EASY_STANDARD, LONG_RUN_STANDARD, FARTLEK, and THRESHOLD_TEMPO do not invent exact paces from preferred pace, recent race, target time, or ESTIMATED. They use effort-only prescriptions and preserve unresolved numeric pace provenance.

`TD-PACESOURCE-001` remains open because no ESTIMATED producer was implemented.

## Workout Prescriptions

- `EASY_STANDARD`: distance-first, exact session total, easy effort, no strides.
- `LONG_RUN_STANDARD`: exact 4F.7B long-run distance, controlled easy long-run effort, no fast-finish variant.
- `FARTLEK`: catalog component order `WARM_UP`, `MAIN_SET`, `RECOVERY`, `COOL_DOWN`; deterministic component distances within allocation.
- `THRESHOLD_TEMPO`: catalog component order `WARM_UP`, `MAIN_SET`, `COOL_DOWN`; threshold effort, unresolved exact threshold pace.
- `GOAL_PACE_TEN_K`: catalog component order `WARM_UP`, `MAIN_SET`, `COOL_DOWN`; exact goal pace only when feasible.

## TAPER_SHARPEN Boundary

The taper KEY_SESSION remains:

- `PhaseKey = TAPER`
- `ProgressionStageKey = TAPER_SHARPEN`
- `StructuralRole = KEY_SESSION`
- `WorkoutDefinitionKey = EASY_STANDARD`

4F.7C emits a baseline EASY_STANDARD prescription with status `BaselinePrescribedSharpeningPending`. The sharpening overlay belongs to Phase 4F.7D.

## Technical Debt

- `TD-PACESOURCE-001`: leaves unchanged.
- `TD-PACESOURCE-002`: leaves unchanged.
- `TD-CORE-READINESS-001`: leaves unchanged.
- `TD-REGISTRY-001`: leaves unchanged.

## Validation

Validators check one prescription per bound session, exact weekly distance reconciliation, exact long-run preservation, unchanged identity fields, segment distance accounting, goal-pace eligibility, and explicit TAPER_SHARPEN pending status.
