# Phase 4F.7D - TAPER_SHARPEN and Final Prescription Validation

## Capability Assessment

Classification: `SUPPORTED_BY_ADDITIVE_RUNTIME_PRESCRIPTION_CONTRACT`.

`EASY_STANDARD` v4 supports `DISTANCE` prescription with `EXACT_SESSION_TOTAL` accounting and has no catalog-authored components. The Phase 4F.7C internal `CatalogPrescriptionSegment` contract can represent stage-specific runtime components while preserving `WorkoutDefinitionKey = EASY_STANDARD`, so no catalog schema extension, catalog workout revision, or new workout identity is required.

## Policy

Policy: `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY` v1.

Qualifying identity:

- `PhaseKey = TAPER`
- `ProgressionStageKey = TAPER_SHARPEN`
- `StructuralRole = KEY_SESSION`
- `WorkoutDefinitionKey = EASY_STANDARD`

The policy uses the 4F.7C assigned key-session distance as authoritative. It does not recompute weekly volume, long-run distance, or session allocation.

## Component Structure

The final runtime prescription replaces the temporary baseline `SESSION_TOTAL` segment with:

1. `EASY_BASELINE`
2. `CONTROLLED_SHARPENING`
3. `EASY_RECOVERY`

The controlled component is placed after easy running and before easy recovery. It is intentionally a runtime prescription component, not a catalog workout-definition component.

## Dose Rule

Sharpening dose is 20% of the assigned taper key-session distance, rounded to the existing 0.5 km rule and clamped to 0.5-1.5 km. Recovery is fixed at 0.5 km. The easy baseline receives the remainder and must remain at least 1.5 km. The minimum feasible taper key-session distance is 3.0 km.

## Effort And Pace

Pace remains effort-only:

- easy baseline: `EASY`
- sharpening: `CONTROLLED_FAST_RELAXED`
- recovery: `EASY_RECOVERY`

No exact pace is invented. Target 10K pace is not borrowed automatically. `ESTIMATED` remains unavailable because no producer exists.

## Distance Accounting

All three runtime components count toward the assigned taper key-session distance under `ExactSessionTotal`. The component sum must reconcile to the session total within the existing tolerance. No hidden volume is created.

## Low-Volume Behavior

The policy is feasible for active 8, 12, and 14 week missing-readiness paths, 12 and 14 week explicit-zero paths, and valid-positive readiness. The 8 week explicit-zero path remains blocked before 4F.7D by the existing 4F.7C four-day allocation minimums: taper residual volume is 5.5 km, while the current allocation policy requires 6.0 km for one key session and two easy supports. If a future allocation produces less than 3.0 km for the taper key session, the taper policy fails closed with `CatalogTaperSharpenPrescriptionInfeasibleException`.

## Cross-Cycle Behavior

The finalizer operates by detecting the actual `TAPER/TAPER_SHARPEN/KEY_SESSION/EASY_STANDARD` session, not by hardcoding a week number. It preserves phase/week/stage scheduling, calendar dates, workout identity, weekly totals, long-run values, and non-taper session prescriptions.

## Complete-Plan Validation

`CatalogFinalPrescribedPlanValidator` validates week/session counts, four sessions per week, date and identity preservation, weekly volume reconciliation, long-run reconciliation and 40% cap, positive session distances, segment distance accounting, unsupported exact pace usage, taper-sharpen component structure, controlled stimulus, no whole-run acceleration, no borrowed goal pace, duration semantics, and policy provenance.

## AUD-508 Outcome

`CLOSED`. The final TAPER_SHARPEN prescription remains `EASY_STANDARD`, is materially distinct from ordinary easy support through runtime components, preserves reduced taper workload, includes controlled intensity stimulus, avoids whole-run acceleration, and leaves no pending overlay state in the final internal prescribed plan.

## Technical Debt Status

- `TD-PACESOURCE-001`: `LEAVES_UNCHANGED`
- `TD-PACESOURCE-002`: `LEAVES_UNCHANGED`
- `TD-CORE-READINESS-001`: `LEAVES_UNCHANGED`
- `TD-REGISTRY-001`: `LEAVES_UNCHANGED`

## Dark Wiring

Phase 4F.7D runs after the Phase 4F.7C baseline prescribed-plan build and validation inside `CatalogPreviewGenerator`, then stops. The final plan is validated and discarded. It is not written to `GeneratedCatalogPlanPayload`, public DTOs, snapshot/hash, confirm, persistence, migrations, routing, or publication status.

## Tests

Focused Phase 4F.7D tests cover capability, identity, material distinction, distance accounting, effort-only pace behavior, no pending state, low-volume feasibility, typed infeasibility, complete-plan validation, and deterministic output.

## Next-Step Readiness

Ready for Phase 4F.8.1 public preview materialization with non-blocking pre-existing gaps around future pace-estimation/race-equivalence work and live public activation.
