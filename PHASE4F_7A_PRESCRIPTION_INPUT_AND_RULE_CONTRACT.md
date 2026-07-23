# Phase 4F.7A Prescription Input and Rule Contract

## Scope

Phase 4F.7A adds a dark/internal prescription context for the active Appsel Plan Catalog pilot route (`TEN_K__4D__INTERMEDIATE` v10). The context is built after runtime-condition resolution, skeleton materialization, progression-stage allocation, calendar assignment, and workout binding.

No public preview DTO, snapshot hash, confirmation payload, persistence schema, routing rule, publish status, or migration changes are included in this phase.

## Active Catalog Boundary

The implemented boundary was checked against the current v10 dependency graph:

- `TEN_K_MASTER` v6
- `INTERMEDIATE_MODIFIER` v6
- `PROGRESSION_MODIFIER` v2
- `APPSEL_RACE_PLAN_V1` v4
- `PEAK_VOLUME_BANDS_V1` v3
- runtime condition value registry v2
- `TEN_K_WORKOUT_PROGRESSION_V1` v5
- workout definitions: `EASY_STANDARD` v4, `LONG_RUN_STANDARD` v4, `FARTLEK` v4, `THRESHOLD_TEMPO` v4, `GOAL_PACE_TEN_K` v2

## Input Normalization

`CatalogPrescriptionInputNormalizer` normalizes the existing preview request evidence into explicit states:

- recent weekly volume: available, not provided, invalid
- recent longest run: available, not provided, invalid
- recent runs per week: available, not provided, invalid
- recent race: available, not provided, incomplete, invalid

Zero values remain distinct from missing values. Negative distance values are invalid. A longest-run value greater than the recent weekly-volume value is marked inconsistent/low-confidence rather than invalid, because weekly volume is a four-week weekly average while longest run is a single maximum run from the last 30 days. That input is preserved but not used directly as the long-run anchor; the context falls back to a weekly-volume-derived long-run anchor for later conservative clamp logic. Recent-race input must include distance, finish time, and date together; partial evidence is incomplete, and future race-result dates are invalid.

## Goal Distance Contract

The old resolver input-path hardcode `GoalDistanceKm = 10.0` is removed. The preview generator now derives the value through `CatalogGoalDistanceResolver`, which checks the request goal distance against the selected catalog candidate family before returning the typed TEN_K distance.

The current pilot only accepts `GoalDistance.TenK` with catalog family `TEN_K`; mismatches fail as internal catalog prescription contract errors.

## Source Selection

The context records source-selection decisions only. It does not calculate final workouts.

- weekly volume anchor: recent four-week average when valid positive evidence exists; otherwise an intermediate conservative default or unresolved invalid state
- long-run anchor: recent 30-day longest run when valid positive evidence exists; otherwise weekly-volume derived or intermediate conservative default
- pace source: direct mapping from runtime `PACE_SOURCE_IN`, gated by `GOAL_FEASIBILITY_IN`
- unsupported goal feasibility cannot select target-goal pace as the prescription pace source
- not-evaluated resolver outputs remain not evaluated and are not reinterpreted

## Rule Ownership Matrix

The catalog fixes identity-level and mode/bounds-level facts:

- workout identity and exact version
- family
- eligible phases
- allowed prescription modes
- allowed distance-accounting modes
- component structure where the workout definition provides components

Runtime remains responsible for user-derived dosage choices in later phases:

- final session distance
- duration
- pace ranges
- repetition counts
- recovery intervals
- component segment values
- long-run dosage
- taper dosage

Derived display fields remain a later presentation concern.

## Session Context

Each bound session receives one `CatalogSessionPrescriptionContext`. The context preserves:

- week/date/phase
- structural role
- progression stage where applicable
- workout definition key/version
- binding provenance
- weekly-volume anchor
- long-run anchor for long-run workouts
- pace-source decision
- fallback provenance

`TAPER_SHARPEN` is preserved as a stage context even though it currently binds to `EASY_STANDARD`. The current catalog shape is therefore classified as `CONTRACT_EXTENSION_REQUIRED`: the identity is preserved, but later 4F.7 prescription work needs an additive runtime/catalog rule extension to distinguish taper sharpening from ordinary easy running without changing public payloads.

## Non-Generation Guarantee

Phase 4F.7A does not generate numeric prescriptions for:

- weekly volume
- session distance
- duration
- pace
- repetitions
- recovery
- workout segments
- long-run distance
- taper dosage

The new contracts store source states, provenance, allowed modes, ownership, and validation results only.
