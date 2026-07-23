# Phase 4G.2 — Exact-12-Week-Only Race Core Safety Constraint

## Status

**Temporary safety constraint**, superseding part of Phase 4G.1's matrix.
This is not the final product behavior.

## Problem

Phase 4G.1 fixed the long-horizon (>14 week) silent-truncation bug and
believed the nominal 8–14 week range was fully supported (per the real
candidate's `coreCycle.minimumWeeks`/`maximumWeeks`). A second live-verified
bug proved otherwise:

`POST /api/v1/plans/generate-preview/race` with `StartDate=2026-07-20`,
`RaceDate=2026-09-14` (an ~8-week horizon) returned **HTTP 200** with a
12-week plan. Final session `2026-10-11`, requested race date `2026-09-14` —
the plan **overshoots the race by about 4 weeks**, with `fallback_used:false`.

Combined with the earlier-verified 20-week undershoot (Phase 4G.1), this
proves: **the catalog phase allocator always emits its fixed ~12-week
allocation regardless of the accepted cycle length.** The `[8,14]`-week
routing acceptance range was never actually driving variable-length
generation — only the exact-12-week case happens to line up.

## Root cause (exact production path)

`CatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary candidate)`
(`backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs`)
takes **only the candidate** — it never receives the request's accepted
cycle length / available horizon. It always sums each phase's
`preferredWeeks` from the candidate's static `PhaseAllocations` config,
producing the same total (~12 weeks) for every request, independent of
`RaceDate`. This was verified live for every value in `[8,14]` except 12:
each produced an identical schedule ending `2026-10-11` regardless of the
requested race date.

## Fix

1. **`RaceHorizonPolicy.Classify`** (`RunningApp.Application.Common`) — new
   canonical classification:
   - `BelowMinimum` — horizon `< 8` weeks (pre-existing, unchanged concern).
   - `ExactStandaloneCoreSupported` — horizon `== 12` weeks (the only length
     proven live to align with `RaceDate`).
   - `CoreLengthRecognizedButNotImplemented` — horizon in `[8,14]` but
     `!= 12`.
   - `CompositionRequired` — horizon `> 14` weeks (Phase 4G.1, unchanged).
2. **`PlanCoreHorizonUnsupportedException`** — new, mapped to **HTTP 422**,
   error code `PLAN_CORE_HORIZON_UNSUPPORTED`. Thrown by
   `PlanServices.GeneratePreviewAsync`'s upstream guard for
   `CoreLengthRecognizedButNotImplemented`, before route decision, template
   selection, catalog generation, or any persistence.
3. **Routing policy updated** — `V1LiveCatalogPilotRoutingPolicy.Evaluate`
   now uses the same `RaceHorizonPolicy.Classify` decision: only an exact
   12-week horizon routes `CatalogLive`; every other in-range horizon routes
   the new `CatalogCoreLengthNotImplemented`, throwing the same
   `PlanCoreHorizonUnsupportedException` — one canonical decision shared by
   both layers (proven by a cross-layer consistency test for 7–15 and 20
   weeks).
4. **Defensive alignment invariant activated** — `CatalogPreviewGenerator`
   now verifies, after dated-skeleton materialization: exactly 12 weeks were
   generated, and the schedule ends no more than 7 days before `RaceDate`
   and never after it. Throws `CatalogRaceDateAlignmentInvalidException` →
   HTTP 422, `CATALOG_RACE_DATE_ALIGNMENT_INVALID`. This was previously
   evaluated in Phase 4G.1 and deliberately left inactive because it would
   have broken the (mistakenly believed) 8–14-week-supported matrix; it is
   now safe and active because only exact-12-week requests can ever reach
   this code path.

## Temporary safety policy

| Horizon | HTTP | Error code |
|---|---|---|
| < 8 weeks | (unchanged, pre-existing) | (unchanged) |
| 8, 9, 10, 11 weeks | 422 | `PLAN_CORE_HORIZON_UNSUPPORTED` |
| **12 weeks** | **200** | — (unchanged existing behavior) |
| 13, 14 weeks | 422 | `PLAN_CORE_HORIZON_UNSUPPORTED` |
| ≥ 15 weeks | 422 | `PLAN_HORIZON_COMPOSITION_REQUIRED` |

## Non-goals (explicitly out of scope)

- 8-week compression, 9–11-week composition, 13–14-week extension.
- Preparation weeks.
- Stretching, trimming, or repeating the 12-week candidate.
- Any change to workout generation, readiness semantics, or target-time
  source semantics.
- Changing the candidate's DRAFT lifecycle status.
- A DB migration (none was needed).

## Future work

A future phase must implement **true horizon-aware core allocation**:
`CatalogPhaseAllocationResolver` (or its replacement) needs to receive the
accepted cycle length and compress/expand each phase within its own
`minimumWeeks`/`preferredWeeks`/`maximumWeeks` bounds (already present in
the candidate's `ten-k-master.v6.json` — e.g. `FOUNDATION: [2,3,4]`,
`BUILD: [3,4,5]`, `RACE_SPECIFIC: [2,4,4]`, `TAPER: [1,1,1]`) so that every
horizon in `[8,14]` — not only exactly 12 — produces a schedule that
actually reaches its own `RaceDate`. Once that exists, `RaceHorizonPolicy.Classify`
should be updated to return `ExactStandaloneCoreSupported`-equivalent for
the full `[8,14]` range again, and `PlanCoreHorizonUnsupportedException`
should become unreachable for in-range horizons.
