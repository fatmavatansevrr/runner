# Phase 4G.5A — Dynamic Standalone-Core Horizon Classification

## Outcome

Implemented a pure, internal, dark, day-accurate `CoreHorizonClassifier` for the current 10K catalog bounds. It describes future composition requirements without changing the live `RaceHorizonPolicy`, routing, preview generation, endpoints, DTOs, persistence, or public support.

## Canonical bounds and existing policy

`ten-k-master.v6.json` declares `minimumWeeks=8`, `defaultWeeks=12`, and `maximumWeeks=14`. The existing live policy continues to own public behavior and remains exact-12-only:

- 8–11 and 13–14 remain `PLAN_CORE_HORIZON_UNSUPPORTED`;
- 12 remains publicly live;
- 15+ remains `PLAN_HORIZON_COMPOSITION_REQUIRED`;
- below 8 remains the pre-existing below-minimum path.

## Typed contracts

The new internal contracts are:

- `CoreHorizonMode`: `Unsupported`, `ReadinessOnly`, `CompressedCore`, `PreferredCore`, `ExtendedCore`, `PreparationRunwayPlusCore`, `InvalidInput`;
- `CoreHorizonDecisionReason`;
- `CoreHorizonContext`;
- `CoreHorizonDecision`, carrying available days, full weeks, partial days, all three catalog bounds, mode, reason, and rules.

`ReadinessOnly` is representable but is not selected: the current repository has no approved rule that maps a below-minimum race horizon to readiness-only planning, so the classifier fails closed as `Unsupported` instead of inventing policy.

## Day arithmetic

The classifier uses elapsed calendar days from `StartDate` to `RaceDate`, matching the established race-horizon boundary, but does not ceiling partial weeks. It retains:

```text
AvailableFullWeeks = AvailableDays / 7
LeadingPartialDays = AvailableDays % 7
```

Classification compares `AvailableDays` to `minimum/preferred/maximum * 7`. Consequently 11 weeks + 6 days remains compressed, 12 weeks + 1 day is extended, and 14 weeks + 1 day requires runway composition. Invalid date ranges and inconsistent catalog bounds fail closed as `InvalidInput`.

## Dark boundary

The classifier has no production call site, DI registration, endpoint, handler, live routing reference, planner, allocator, materializer, or composer. A source-scanning test enforces that it remains unreferenced elsewhere in `RunningApp.Application`. The generic phase allocator and Preparation Runway types were inspected but not invoked or modified.

## Coverage

Tests cover exact horizons 7–15 and 20 weeks, partial-day boundaries, invalid dates, invalid bounds, preservation of unresolved readiness-only routing, typed decision fields, and dark reachability.

Validation results:

```text
CoreHorizonClassifierTests: 22 passed, 0 failed, 0 skipped
SW-12/SW-13 real-host horizon regression: 21 passed, 0 failed, 0 skipped
Release build: succeeded, 0 warnings, 0 errors
git diff --check: no whitespace/conflict-marker errors (existing LF/CRLF warnings only)
```

The real-host regression preserves 8 and 14 weeks as HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`, 12 weeks as HTTP 200 with the established 12-week/48-session shape, and 20 weeks as HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`.

No commit or push was performed.
