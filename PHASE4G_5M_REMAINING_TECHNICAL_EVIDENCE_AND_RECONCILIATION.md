# Phase 4G.5M — Remaining Technical Evidence and Reconciliation

## Scope and activation boundary

This phase completes technical evidence after Phase 4G.5L. It does not publish
`TEN_K__4D__INTERMEDIATE v10`, change its `DRAFT` lifecycle, enable
`CatalogLivePilot` in normal configuration, activate Preparation Runway, or
make a production-deployment decision.

Phase 4G.5L established the only horizon authority:

```text
RaceDate - StartDate (exclusive elapsed-day semantics)
-> CoreHorizonClassifier
-> CoreHorizonDecision { AvailableFullWeeks, LeadingPartialDays, Mode }
-> RaceHorizonPolicy public mapping
-> routing and allocation
```

Partial days never round upward. `7w6d` remains Unsupported and returns HTTP
422 `CATALOG_LIVE_PILOT_REQUEST_UNSUPPORTED`. `14w1d` and longer remain
`PreparationRunwayPlusCore` and return HTTP 422
`PLAN_HORIZON_COMPOSITION_REQUIRED` before catalog composition. Exact `12w0d`
remains the canonical `PreferredCore` path.

## Preserved core structure

| Weeks | Foundation | Build | Race-specific | Taper |
|---:|---:|---:|---:|---:|
| 8 | 2 | 3 | 2 | 1 |
| 9 | 2 | 3 | 3 | 1 |
| 10 | 2 | 3 | 4 | 1 |
| 11 | 2 | 4 | 4 | 1 |
| 12 | 3 | 4 | 4 | 1 |
| 13 | 4 | 4 | 4 | 1 |
| 14 | 4 | 5 | 4 | 1 |

Every week contains exactly one `KEY_SESSION`, two distinct `EASY_SUPPORT`
sessions, and one `LONG_RUN`.

## Read-model reconciliation

Real PostgreSQL tests cover representative 8, 12, 13, and 14-week plans from
public preview through confirmation and compare:

- the frozen `CatalogPreviewSnapshot` and typed generated payload;
- `TrainingPlan`, `TrainingWeek`, and every `TrainingDay`;
- active-plan details;
- every calendar month intersecting the persisted session date range;
- representative training-day details (key, easy, long run, final taper, and
  a month-boundary session when present);
- home active-plan identity, current StartDate-relative week, real week
  sessions, and the established synthetic-rest fallback when today is not a
  scheduled session.

Calendar reconciliation uses stable `TrainingDay.Id` set equality. Every
persisted session appears once, no extra persisted identity appears, dates are
returned by the correct month, and all exposed schedule fields agree.
Calendar and training-day-detail DTOs do not expose plan ID, catalog
provenance, structural role, or prescription segments; those fields are
therefore compared at the snapshot/persistence boundary, not invented on a
public DTO.

The tests exposed and corrected one additive confirmation defect: catalog
confirmation had not copied frozen `RaceName`, `PreferredDays`, `LongRunDay`,
weekly availability, or preferred pace into `TrainingPlan`. These inputs are
now part of `ResolverInputSnapshot` and are persisted from the immutable
snapshot at confirmation. No schedule algorithm changed.

## Pace-source and prescription evidence

Real HTTP tests cover 8, 12, and 14 weeks.

| Input | Resolver/public result |
|---|---|
| Product-average target | HTTP 200; `PACE_SOURCE_IN=TARGET_TIME`; feasibility `CHALLENGING` |
| Valid recent 5K race + user target | HTTP 200; `PACE_SOURCE_IN=RECENT_RACE`; feasibility `REALISTIC` |
| User target without independent evidence | HTTP 422 `RUNTIME_CONDITION_UNSUPPORTED`; reason `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`; zero persistence |
| Aggressive/contradictory recent-race projection | Evaluated `UNSUPPORTED`; established safe non-goal-pace stage selection; HTTP 200 |
| Race evidence older than 180 days | Still `RECENT_RACE`; `NOT_USABLE_AS_PACE_ANCHOR` is trace metadata only under current policy |
| Zero target, non-positive race time, future race evidence | HTTP 400 `VALIDATION_ERROR`; zero persistence |
| Unknown pace-source enum token | Model-binding HTTP 400; zero persistence |

There is no distinct V1 public `ESTIMATED`/fitness-evidence pace source.
`PaceSourceResolver` deliberately never emits it (`TD-PACESOURCE-001` remains
OPEN). A user-defined target plus valid recent-race evidence succeeds through
the higher-priority `RECENT_RACE` source; no separate “TARGET_TIME with
independent evidence” output exists under the current priority contract.

Prescription field classification:

- Required and populated internally/persisted: workout identity, prescription
  basis, distance or estimate, typed pace object, effort guidance, ordered
  segment collection, and catalog provenance.
- Optional and populated where applicable: numeric target/range pace,
  estimated duration, intensity, repetitions, and recovery semantics.
- Optional and absent by contract: numeric pace for effort-only sessions and
  segments/repetition/recovery for simple steady sessions.
- Internal-only: raw pace-source selection, resolver decision trace, structured
  segments, catalog workout/progression identity, and provenance.
- Not represented on current public DTOs: pace-source identity, structured
  segments, and catalog provenance. Public/persistence parity therefore uses
  shared date, distance, duration, type, and intensity fields; full structured
  parity is proven between frozen payload and `CatalogPrescriptionJson`.

## Public calendar adversarial matrix

Monday, Wednesday, and Sunday starts were exercised at 8, 12, and 14 weeks,
including a Sunday start that immediately crosses a month boundary. Tests
prove StartDate-relative seven-day windows, preferred-day authority, long-run
preference authority, unique dates, key/long-run separation, final taper, and
race alignment.

Duplicate preferred days, wrong preferred-day count, a long-run day outside
preferred days, and a race date before StartDate fail with the existing typed
400/422 contracts and zero schedule persistence. An invalid weekday token is
rejected by ASP.NET model binding with HTTP 400; that framework response does
not carry the application `errorCode` envelope. The internal exhaustive
calendar test already proves every valid four-of-seven preferred-day set has a
safe assignment, so there is no canonical “valid shape but impossible
distribution” public case to manufacture.

## Governance semantic parity

`activation-readiness-risks.json` is the structured machine-readable source of
truth. Markdown is the human-readable semantic projection. Tests compare the
required TD IDs, status, classification markers, scope markers, represented
evidence references, and stable closure-phase/reason markers without comparing
full paragraphs or formatting.

The test found one stale projection: the Markdown table row for
`TD-COREHORIZON-ALLOCATOR-UNWIRED-001` carried `CLOSED` but still described the
pre-closure unwired state. Its row now records the JSON-canonical Phase
4G.5J/4G.5K upstream-gate closure. No TD status changed.

Candidate/version/range caveats in this inventory are documentation-only.
Runtime candidate identity and horizon boundaries have their own code/tests,
but production code intentionally does not consume this governance inventory
or generically enforce each record's future-candidate prose.

## Test-host portability and isolation

The integration host intentionally removes machine-dependent Windows Event
Log providers and keeps console logging. Expected database errors therefore
remain observable without elevated Event Log permissions.

Any integration-test class that resets, shares, inserts into, or counts rows
in the common PostgreSQL database must use
`ApiIntegrationTestCollection`. This prevents reset/count races across the
shared mock user.

Phase 4G.5M adds a test-only EF `DbConnectionInterceptor`. After real-host
startup, the test resets its counter, resolves the complete catalog service
graph from one real DI scope, and observes zero connection opens. The formerly
skipped `RealHost_ServiceResolution_OpensNoDatabaseConnection` test is enabled.

## Historical 12-week evidence limitation

Internal fixed-versus-dynamic 12-week allocation, workout, volume, pace, and
calendar parity is proven. Current public-preview versus confirmed-persistence
shared-field parity is proven. No independent pre-activation persisted
12-week golden source exists.

This is `HISTORICAL_EVIDENCE_LIMITATION`: historical persistence parity remains
`SOURCE_MISSING`, not FAILED, and is not an active code blocker. No current
fixture was manufactured and mislabeled as historical evidence.

## Validation result

- Release backend build: PASS, 0 warnings, 0 errors.
- Read-model reconciliation: PASS, 4/4.
- Pace-source matrix: PASS, 15/15.
- Public-calendar adversarial matrix: PASS, 14/14.
- Horizon/allocation/orchestration/confirmation/runway focused regression:
  PASS, 496/496.
- Governance semantic parity: PASS, 2/2.
- Instrumented real-host no-database-I/O check: PASS, 1/1 and zero observed
  connection opens.
- Plan-catalog Release build: PASS, 0 warnings, 0 errors.
- Plan-catalog full suite: PASS, 348/348.
- Backend full suite run 1: PASS, 1,821/1,821, 0 skipped.
- Backend full suite run 2: PASS, 1,821/1,821, 0 skipped.
- Governance JSON syntax: PASS.
- `git diff --check`: PASS; line-ending conversion warnings only.

Phase conclusion: `COMPLETE_WITH_DOCUMENTED_LIMITATIONS`. The only limitation
is the explicitly documented absence of an independent historical persisted
12-week golden source. The current runtime, persistence, and read-model paths
are reconciled and test-clean. No lifecycle, configuration, catalog
activation, Preparation Runway, or production-deployment decision was made.
