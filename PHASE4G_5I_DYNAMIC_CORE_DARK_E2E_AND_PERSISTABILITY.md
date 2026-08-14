# Phase 4G.5I - Dynamic Core Dark End-to-End and Persistability

## Executive result

The complete 8-14-week dynamic standalone-core pipeline now has a test-only end-to-end harness. Every horizon builds a validated final schedule and a side-effect-free `TrainingPlan`/`TrainingWeek`/`TrainingDay` projection. No harness or dynamic orchestrator is reachable from a live request path, no public horizon was activated, and no persistence write occurs.

## Critical dark/live distinction

“Produce and discard before the public fail-closed response” is implemented only as a validation concept in the test harness: the harness independently builds blocked horizons, while separate real-host endpoint tests independently prove the actual endpoint remains fail-closed. `CatalogPreviewGenerator`, `PlanServices`, controllers, handlers, and DI were not changed to invoke or discard a dark result. Running the dark pipeline is not a prerequisite or side effect of any public request.

## Reuse and composition

`DynamicCoreDarkEndToEndHarness` is compiled only into `RunningApp.IntegrationTests`. It delegates to the existing Phase 4G.5H `DynamicCoreCalendarMaterializationOrchestrator`, whose existing composition directly nests:

1. `DynamicCoreWeekSkeletonOrchestrator` (4G.5D);
2. `DynamicCoreWorkoutBindingOrchestrator` (4G.5E);
3. `DynamicCoreVolumeAndLongRunOrchestrator` (4G.5F);
4. `DynamicCoreSessionPrescriptionOrchestrator` (4G.5G);
5. `DynamicCoreCalendarMaterializationOrchestrator` (4G.5H).

No allocator, skeleton, workout-selection, volume, long-run, pace, or calendar algorithm was copied into 5I.

## Eight-to-fourteen matrix

| Weeks | Full plan | Final validation | Sessions | Persistable weeks | Persistable days |
| ---: | --- | --- | ---: | ---: | ---: |
| 8 | Built | Pass | 32 | 8 | 32 |
| 9 | Built | Pass | 36 | 9 | 36 |
| 10 | Built | Pass | 40 | 10 | 40 |
| 11 | Built | Pass | 44 | 11 | 44 |
| 12 | Built | Pass | 48 | 12 | 48 |
| 13 | Built | Pass | 52 | 13 | 52 |
| 14 | Built | Pass | 56 | 14 | 56 |

Every session has a nonblank catalog workout identity, positive planned distance, and valid session prescription. Every race-date alignment check passes, including continuous week/calendar structure and the final-session boundary.

## Persistability mapping

The harness projects the validated result into detached domain entities:

- one `TrainingPlan` with catalog candidate/family provenance;
- one `TrainingWeek` per prescribed week with the exact catalog phase key and planned weekly volume;
- four `TrainingDay` rows per week with exact date, phase, progression-stage, workout-definition, structural-role, distance, duration, and serialized prescription fields.

The projection owns no `AppDbContext`, repository, transaction, or persistence service. Its only dependency is the existing dark calendar orchestrator. Objects are marked `DARK_TEST_ONLY_NOT_PERSISTED`; no `Add`, `SaveChanges`, preview row, plan row, week row, or day row is written.

## Invalid-result behavior

An infeasible 7-week request fails closed through the existing `DynamicCoreWeekSkeletonInfeasibleException`. The harness also rejects any composed result whose existing final prescription validation or race-date alignment is not `Pass`; it does not repair or normalize invalid output.

## Twelve-week parity

The consolidated Phase 4G.5D-5H regression suite remains green through the 5I composition. It includes the established 12-week fixed/dynamic field-level comparisons for allocation, skeleton, workout binding, volume/long-run, pace/session prescriptions, and dated calendar. The 5I harness additionally serializes the stable value projection of two independently constructed 12-week compositions and requires exact equality. The resulting projection remains exactly 12 weeks and 48 sessions.

## Shared verifier reachability isolation

The existing shared helper was exercised against all nine verifiers. For these eight, the additive parameter remains omitted and the original single-caller restriction is unchanged:

- `PhaseConstraintVerifier`;
- `RaceSpecificCapacityVerifier`;
- `StageReachabilityVerifier`;
- `WorkoutExposureVerifier`;
- `GoalPaceReachabilityVerifier`;
- `ReadinessEligibilityVerifier`;
- `VolumeProgressionVerifier`;
- `LongRunProgressionVerifier`.

Only `RaceDateAlignmentVerifier` receives the existing opt-in allowance for `DynamicCoreCalendarMaterializationOrchestrator.cs`. The 5I harness calls no verifier directly, so no further helper relaxation was needed.

## Cumulative zero-call-site proof

A consolidated structural test scans all production C# sources in Application, Api, Infrastructure, and Persistence. It permits only the established dark-to-dark chain (5E calling 5D, 5F calling 5E, 5G calling 5F, and 5H calling 5G). It rejects every other production reference to the five orchestrators and the test harness. It separately scans stripped executable source in `CatalogPreviewGenerator.cs` and `PlanServices.cs` and proves none of the six symbols occurs.

Result: all five orchestrators and the 5I harness have zero live request-path callers; the harness itself has zero production references and no DI registration.

## Public behavior

The 21-test real-host horizon regression passed:

- 8-11 weeks: HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`;
- 12 weeks: HTTP 200;
- 13-14 weeks: HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`;
- 15+ weeks, including 20: HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`.

Blocked requests fail through the existing horizon policy/gates, never through an invoked-and-discarded dark pipeline.

## Validation results

```text
Phase 4G.5I harness: 12 passed, 0 failed, 0 skipped
Consolidated 4G.5D-5I + reachability/parity: 296 passed, 0 failed, 0 skipped
Public horizon HTTP regression: 21 passed, 0 failed, 0 skipped
Release build: succeeded, 0 warnings, 0 errors
Full backend Run 1: 1750 passed, 1 skipped, 0 failed (1751 total)
Full backend Run 2: 1750 passed, 1 skipped, 0 failed (1751 total)
```

The one skip in each full run is the intentional `RealHost_ServiceResolution_OpensNoDatabaseConnection` no-DB-I/O test. Full-run totals match exactly.

## New findings and TD status

No horizon failed to build, no persistability-field gap was found, and no new interaction requiring a technical-debt record was discovered. Therefore activation-readiness risk JSON/Markdown files were not changed by Phase 4G.5I.

## Files created

- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DynamicCoreDarkEndToEndHarnessTests.cs`
- `PHASE4G_5I_DYNAMIC_CORE_DARK_E2E_AND_PERSISTABILITY.md`

## Files modified

None for Phase 4G.5I.

## Explicitly unchanged

No production source, `CatalogPreviewGenerator`, `PlanServices`, public DTO, controller, handler, DI registration, horizon policy, confirmation path, persistence service, database mapping, catalog artifact, phase bound, workout logic, volume/long-run logic, pace policy, calendar logic, Preparation Runway behavior, or frontend file changed.

## Commit/push status

No staging, commit, or push was performed.

## Final status

```text
PHASE_4G_5I_STATUS=COMPLETE
DARK_E2E_HORIZONS=8_THROUGH_14_VALID
PERSISTABILITY_STATUS=DETACHED_MAPPING_VALIDATED_NO_MUTATION
TWELVE_WEEK_PARITY=EXISTING_BYTE_VALUE_REGRESSIONS_PRESERVED
SHARED_VERIFIER_ISOLATION=UNCHANGED_FOR_OTHER_EIGHT
CUMULATIVE_DARK_REACHABILITY=ZERO_LIVE_CALLERS
PUBLIC_HORIZON_BEHAVIOR=UNCHANGED
LIVE_PIPELINE_WIRING=NONE
NEW_TECHNICAL_DEBT=NONE
```
