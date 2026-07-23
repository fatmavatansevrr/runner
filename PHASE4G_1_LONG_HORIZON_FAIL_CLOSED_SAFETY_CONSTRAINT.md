# Phase 4G.1 — Long-Horizon Race Request Fail-Closed Safety Constraint

## Status

**Temporary safety constraint.** This is not the final product behavior.

## Problem

A verified production-like bug: `POST /api/v1/plans/generate-preview/race` with
`StartDate=2026-05-25`, `RaceDate=2026-10-12` (an ~20-week horizon) returned
**HTTP 200** with a 12-week plan silently anchored to `StartDate`. The plan's
final session (2026-08-16) landed roughly 8 weeks before the requested race
date, with `fallback_used: false` — nothing in the response signaled that the
plan didn't actually reach the race.

## Root cause

`V1LiveCatalogPilotRoutingPolicy.Evaluate` (in
`LivePlanPreviewRouting.cs`) computed the request's "cycle length" as:

```csharp
cycleLength = Math.Ceiling((raceDate.DayNumber - asOfDate.DayNumber) / 7d)
```

using `asOfDate` (`DateTime.UtcNow`, i.e. "today") instead of the request's
own `StartDate`. For a `StartDate` that isn't "today" — as in the verified
case, where `StartDate` (2026-05-25) was well in the past relative to the
system clock at request time (2026-07-21) — this produced a *wrong* cycle
length (≈12 weeks, computed from "today" to `RaceDate`) instead of the real
horizon (≈20 weeks, computed from the actual `StartDate` to `RaceDate`). The
wrong, smaller value passed the `[8,14]`-week acceptance range, so the
request was routed to catalog generation, which then built its default
~12-week plan anchored at the real (past-relative-to-today) `StartDate` —
producing exactly the silently-truncated schedule from the bug report.

The legacy SQL template path (`PlaceholderPlanGenerationEngine`) has an
independent, deeper version of the same class of gap: it selects a seeded
template purely by `(GoalType, GoalDistance, Level, DaysPerWeek)` and never
consults `RaceDate`/`StartDate`/horizon at all.

## Fix

1. **`RaceHorizonPolicy`** (`RunningApp.Application.Common`) — the single,
   centralized calculation of a race request's available horizon
   (`StartDate` to `RaceDate`, whole weeks, rounded up) and the currently
   approved standalone bounds (8–14 weeks, mirroring the real candidate's
   `ten-k-master.v6.json` `coreCycle` bounds). `V1LiveCatalogPilotRoutingPolicy.Evaluate`
   now uses this instead of its own `asOfDate`-based calculation.
2. **Universal fail-closed guard** in `PlanServices.GeneratePreviewAsync` —
   runs immediately after request validation, for **every** race request
   (not just the one hardcoded catalog-pilot identity), before route
   decision, before template selection, before any catalog generation, and
   before any persistence. If the horizon exceeds
   `RaceHorizonPolicy.MaximumSupportedStandaloneWeeks` (14), it throws
   `PlanHorizonCompositionRequiredException` — mapped by `GlobalExceptionHandler`
   to **HTTP 422**, error code `PLAN_HORIZON_COMPOSITION_REQUIRED`. This
   closes the gap for both the catalog path and the legacy path.

## Non-goals (explicitly out of scope for this fix)

- Preparation weeks / 8-week-preparation + 12-week-race-core composition.
- Stretching or extending the 12-week core to fit a longer horizon.
- Any change to workout generation, readiness semantics, or target-time
  source semantics.
- A DB migration (none was needed).

## A separate, deeper gap discovered during this work (not fixed here)

While implementing a defensive "generated schedule must end near RaceDate"
invariant (requested as backstop protection even for supported horizons),
live testing revealed that **the catalog candidate's phase allocation is not
horizon-aware**: `CatalogPhaseAllocationResolver.Resolve(candidate)` takes
only the candidate, never the request's accepted cycle length, and always
emits the master template's fixed default allocation (~12 weeks) —
regardless of whether the accepted horizon was 8, 9, 10, 11, 13, or 14 weeks.
In other words, **only an exactly-12-week request currently produces a
schedule that actually lands on its own RaceDate**; the rest of the
nominally-"supported" `[8,14]` range is accepted by routing but does not
currently produce a RaceDate-aligned schedule either.

This is a real, separate defect. It was **not** fixed as part of this task:
doing so is variable-length/horizon-aware phase-allocation composition work,
squarely inside this task's stated non-goals ("Do not stretch or extend the
12-week core") and would have broken the explicit requirement to leave
existing 8–14-week behavior unchanged. A defensive alignment check was
evaluated and deliberately scoped down to only reject a schedule that ends
**after** `RaceDate` would ever occur (currently never, for the fixed
~12-week allocation) rather than one that ends materially before it (which
*does* currently occur for non-12-week supported horizons, and rejecting it
would be an undocumented, out-of-scope behavior change).
`CatalogRaceDateAlignmentInvalidException` is defined and mapped to HTTP 422
(`CATALOG_RACE_DATE_ALIGNMENT_INVALID`) as prepared infrastructure for
whichever future phase makes phase allocation horizon-aware.

## Silent truncation is prohibited

Per this fix, a race request whose horizon cannot be represented by the
current standalone cycle-composition rules must never return HTTP 200 with a
schedule that doesn't reach `RaceDate`. It must fail closed with a typed
error instead.

## Future work

A later phase will implement genuine preparation-block + race-core
composition (e.g., N weeks of general preparation followed by the approved
8–14-week race-specific core, aligned to end at `RaceDate`), replacing this
temporary reject-outright behavior for long horizons, and will also need to
make phase allocation horizon-aware for the currently-approved 8–14 week
range so every accepted horizon — not only exactly 12 weeks — produces a
schedule that actually reaches its own race date.
