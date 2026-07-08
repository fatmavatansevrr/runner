# Bundle Workout Closure Audit — Milestone E — CLOSURE-001

## Before

`CatalogBundleAssembler`'s effective-workout computation was an **intersection**: progression candidate
keys ∩ level-modifier eligible keys, resolved via the drift-prone `FindWorkout(key)` auto-selector.
`LONG_RUN_STANDARD` was eligible per `INTERMEDIATE_MODIFIER` but never a progression candidate anywhere in
`TEN_K_WORKOUT_PROGRESSION_V1` — so the intersection was always empty for it, and it never appeared in any
bundle (Part 1 Finding 9, confirmed still true for all of v1/v2/v3 today).

## After — union closure

For the candidate (exact) shape, `WorkoutClosureResolver.ComputeExactClosureRefs` computes:

```
exact WorkoutProgression.WorkoutCandidates  UNION  exact LevelModifier.EligibleWorkouts
```

— deduplicated, resolved via `FindWorkout(key, version)` (exact, no auto-selection). This is a genuine
**union, not an intersection** — eligibility alone is now sufficient for a workout to enter the bundle,
without requiring it to also be a progression candidate. This directly satisfies the required outcome
without touching `TEN_K_WORKOUT_PROGRESSION_V1`'s candidate lists (no `LONG_RUN_STANDARD` was added as a
candidate — verified explicitly by `BundleWorkoutClosureTests.LongRunStandard_AppearsWithoutBecomingAProgressionCandidate`
and by direct inspection of `ten-k-workout-progression.v2.json`, which contains zero references to
`LONG_RUN_STANDARD`).

**Live confirmation against the real candidate combination** (`TEN_K__4D__INTERMEDIATE v4`, built via CLI
`build-bundle`):

```
Workouts: EASY_STANDARD v2, FARTLEK v2, GOAL_PACE_TEN_K v1, LONG_RUN_STANDARD v2, THRESHOLD_TEMPO v2
```

Compare to v1/v2/v3's bundles (identical to each other, still missing `LONG_RUN_STANDARD`):

```
Workouts: EASY_STANDARD v2, FARTLEK v2, GOAL_PACE_TEN_K v1, THRESHOLD_TEMPO v2
```

`LONG_RUN_STANDARD v2` is the only difference — added purely through `INTERMEDIATE_MODIFIER v2`'s exact
`EligibleWorkouts` list, with zero change to the progression's candidate structure.

## "Any explicitly modeled exact default or required slot-workout reference" (item 3)

Not needed for this migration — the union of (1) progression candidates and (2) level-modifier eligible
workouts was already sufficient to satisfy the required outcome (`LONG_RUN_STANDARD` reachable via source
2). No third mechanism was introduced; `WorkoutClosureResolver` is structured so a future slot-default
source could be added as a third union member without disturbing the first two, if ever needed.

## E1 — Layout coverage validation

`CandidatePublishGraphValidator.ValidateLayoutCoverage` checks, for the candidate shape only, that every
distinct `RunLayoutDefinition` slot role has at least one family-compatible workout in the closure:

| Slot role | Compatible family | Real catalog result (v4) |
|---|---|---|
| `LongRun` | `LongRun` | satisfied by `LONG_RUN_STANDARD v2` |
| `EasySupport` | `Easy` | satisfied by `EASY_STANDARD v2` |
| `KeySession` | `Quality` or `Race` | satisfied by `FARTLEK v2`/`THRESHOLD_TEMPO v2` |

No scheduling, no dates, no pace/dosage calculation — purely a structural "does at least one compatible
workout exist" check, confirmed by code inspection of `ValidateLayoutCoverage` (it never touches any
date/week/pace field). Failure codes implemented: `LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT` (proven to fire via
`BundleWorkoutClosureTests.LayoutCoverage_FailsWhenASlotHasNoEligibleWorkout`),
`BUNDLE_MISSING_REFERENCED_WORKOUT` (raised if a closure reference can't be resolved),
`WORKOUT_REFERENCE_VERSION_NOT_FOUND` (raised by `CatalogBundleAssembler` itself if the same happens at
actual assembly time).

## E3 — historical validation era preserved

Historical releases (`1.0.0` through `0.5.0-pilot`) were built from schemaVersion 1 progressions/level
modifiers, whose bundles use the unchanged intersection algorithm — `CandidatePublishGraphValidator`'s
layout-coverage check is gated by `WorkoutClosureResolver.IsExactShape` and is a **no-op for legacy
(schemaVersion 1) graphs**, so no historical release or historical bundle is retroactively invalidated for
lacking `LONG_RUN_STANDARD`. All 6 releases still `verify-release` PASSED after this migration.

## Tests (8 — `tests/PlanCatalog.Tests/Publishing/BundleWorkoutClosureTests.cs`)

Tests 25-32 of the Part 2 required list, all passing (plus one extra negative test proving
`LAYOUT_SLOT_HAS_NO_ELIGIBLE_WORKOUT` actually fires, not just always passes).

## Final status: `SELF_CONTAINED_BUNDLE_CLOSURE` = **IMPLEMENTED**
