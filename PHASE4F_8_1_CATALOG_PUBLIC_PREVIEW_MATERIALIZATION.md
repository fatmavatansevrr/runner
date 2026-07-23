# Phase 4F.8.1 - Catalog Public Preview Materialization

## Boundary

Input is the fully validated Phase 4F.7D `CatalogPrescribedPlan` plus the validated catalog candidate, original preview request, `AsOfDate`, plan start date, and 4F.7B volume/long-run plan. Output is the existing `GeneratedCatalogPlanPayload` stored on `CatalogPreviewSnapshot.GeneratedPreviewPlanPayload`.

The materializer is projection-only. It does not recompute phase allocation, stage allocation, workout binding, weekly volume, long-run distance, session distance, pace source, workout components, or TAPER_SHARPEN prescription.

## Producer

`CatalogPublicPreviewMaterializer` v1 is the first real producer of `GeneratedCatalogPlanPayload`. `CatalogPreviewGenerator` invokes it immediately after 4F.7D final prescribed-plan validation.

## Mapping

Plan-level mapping preserves candidate key/version, goal type, canonical distance family, days per week, start/end dates, planned week count, dependency versions, generation source, materializer version, and `AsOfDate`.

Week-level mapping preserves week number, plan-relative start/end dates, phase provenance, stage key, planned weekly volume, and exactly four run sessions.

Session-level mapping preserves date, order, structural role, progression stage, workout definition key/version, planned distance, pace prescription, duration semantics, effort guidance, ordered segments, and provenance.

## Public Workout Type Policy

Policy: `V1_CATALOG_PUBLIC_WORKOUT_TYPE_MAPPING_POLICY` v1.

- `EASY_STANDARD + EASY_SUPPORT` -> `Easy`
- `EASY_STANDARD + KEY_SESSION + TAPER_SHARPEN` -> `Easy`, with stage provenance and sharpening segments
- `EASY_STANDARD + KEY_SESSION` -> `Easy`
- `LONG_RUN_STANDARD + LONG_RUN` -> `LongRun`
- `FARTLEK + KEY_SESSION` -> `Interval`
- `THRESHOLD_TEMPO + KEY_SESSION` -> `Tempo`
- `GOAL_PACE_TEN_K + KEY_SESSION` -> `Interval`

The policy is exact-key/role/stage based and does not use display text, contains checks, list order, or family guessing.

## Pace

Exact paces map to `Target` seconds/km. Pace ranges map to `Range`. Effort-only and unresolved numeric states map to `EffortOnly` with null numeric pace fields. No ESTIMATED pace, preferred pace, or target goal pace is fabricated.

## Duration

Prescribed and segment-derived durations map only when a duration exists. Estimated goal-pace duration maps to `EstimatedDurationMinutes`. Unresolved effort-only duration remains null in the generated payload; no zero-duration placeholder is written to the payload.

## Segments

Policy: `V1_CATALOG_PUBLIC_SEGMENT_MAPPING_POLICY` v1.

Mapped segment types include `SESSION_TOTAL`, `WARM_UP`, `MAIN_SET`, `RECOVERY`, `COOL_DOWN`, `EASY_BASELINE`, `CONTROLLED_SHARPENING`, and `EASY_RECOVERY`. Ordering and distance accounting are preserved.

## TAPER_SHARPEN

TAPER_SHARPEN remains:

- `PhaseKey = TAPER`
- `ProgressionStageKey = TAPER_SHARPEN`
- `StructuralRole = KEY_SESSION`
- `WorkoutDefinitionKey = EASY_STANDARD`

Public type remains `Easy`, while provenance preserves stage identity and ordered segments preserve `EASY_BASELINE`, `CONTROLLED_SHARPENING`, and `EASY_RECOVERY`, making it distinguishable from ordinary easy support.

## Snapshot And Hash

`CatalogPreviewSnapshotBuilder` and `CatalogPreviewSnapshotVerifier` now include `GeneratedPreviewPlanPayload` in the canonical hash content for newly generated snapshots. Equivalent payloads serialize deterministically and verify; material prescription changes alter the hash.

## Confirm Boundary

Confirm remains disabled. A structurally valid non-null generated payload is validated by `CatalogPlanConfirmationService` and then rejected with the existing materialization-not-implemented guard before any `TrainingPlan`, `TrainingWeek`, or `TrainingDay` write.

## Unsupported Path

The 8-week explicit-zero weekly-volume path remains unsupported for public materialization. It fails closed with a typed preview-generation failure and no generated payload.

## Tests

Focused tests cover non-null payload generation, taper-sharpen representation, deterministic payload serialization and round-trip validation, unsupported explicit-zero behavior, and existing confirm guards.

## Live Routing Boundary

The candidate remains DRAFT. The PUBLISHED-only gate remains unchanged. No live catalog routing, persistence, migration, confirmation materialization, or publication is enabled.
