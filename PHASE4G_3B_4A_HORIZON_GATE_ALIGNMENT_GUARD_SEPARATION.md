# Phase 4G.3B.4a — Separate Horizon-Support Enforcement from Race-Date Alignment Validation

## 1. Purpose and scope

Narrow internal refactor separating two responsibilities that the live race-date
alignment guard inside `CatalogPreviewGenerator` previously combined into one
condition: (a) whether a standalone week-count horizon is publicly supported,
and (b) whether a generated schedule's final session is correctly aligned to
`RaceDate`. After this phase, (a) is owned exclusively by `RaceHorizonPolicy`/
the preview-routing layer (unchanged), and (b) is owned exclusively by the
guard itself.

Out of scope, and not done in this phase: enabling any new week-count horizon;
building the safety-verification orchestration pipeline; wiring any of the
nine standalone safety verifiers into production; creating a support registry;
any change to `RaceHorizonPolicy`, controllers, public DTOs, persistence, the
database schema, Flutter, catalog workout/phase definitions, or
`VolumeSafetyPolicy`.

## 2. Exact source location and containing method

`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`,
private method `BuildDarkInternalDatedSkeleton`, inside the
`try`/`catch (Exception ex) when (ex is CatalogPreferredDaysRequiredException ...)`
block, immediately after the dated-skeleton output-validation step.

## 3. Before condition

```csharp
var expectedWeekCount = RunningApp.Application.Common.RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks;
var daysBeforeRace = raceDateForAlignment.DayNumber - datedSkeleton.EndDate.DayNumber;
const int maxAllowedTrailingGapDays = 7;
if (datedSkeleton.Weeks.Count != expectedWeekCount || daysBeforeRace < 0 || daysBeforeRace > maxAllowedTrailingGapDays)
{
    throw new CatalogRaceDateAlignmentInvalidException(...);
}
```

## 4. After condition

```csharp
var daysBeforeRace = raceDateForAlignment.DayNumber - datedSkeleton.EndDate.DayNumber;
const int maxAllowedTrailingGapDays = 7;
if (daysBeforeRace < 0 || daysBeforeRace > maxAllowedTrailingGapDays)
{
    throw new CatalogRaceDateAlignmentInvalidException(...);
}
```

The `expectedWeekCount` local and the `Weeks.Count != expectedWeekCount` clause
were removed. No replacement week-count clause was added anywhere else in this
method or class. `RaceHorizonPolicy` itself, the seven-day tolerance, and how
`StartDate`/`RaceDate`/`EndDate`/final session date are calculated are all
unchanged.

## 5. Production call-path proof — unsupported horizons are rejected before catalog generation

`CatalogPreviewGenerator`/`ICatalogPreviewGenerator` has exactly one production
caller anywhere in `RunningApp.Application`, `RunningApp.Api`,
`RunningApp.Infrastructure`, or `RunningApp.Persistence`: `PlanServices`
(`Services/PlanServices.cs`), which holds it via a single `_catalogPreviewGenerator`
field. DI registration (`Program.cs`) registers exactly one implementation,
Scoped, with no alternates.

`PlanServices.GeneratePreviewAsync` (lines 96–153) runs an unconditional
"fail-closed horizon guard" for every `GoalType.Race` request carrying a
`RaceDate`, before route decision (line 176) and before any call into the
catalog generator (line 323):

- `RaceHorizonClassification.CompositionRequired` → throws
  `PlanHorizonCompositionRequiredException` (`PLAN_HORIZON_COMPOSITION_REQUIRED`).
- `RaceHorizonClassification.CoreLengthRecognizedButNotImplemented` → throws
  `PlanCoreHorizonUnsupportedException` (`PLAN_CORE_HORIZON_UNSUPPORTED`).
- `BelowMinimum` and `ExactStandaloneCoreSupported` (only) continue.

`BelowMinimum` is not itself rejected by this guard, but it can never reach
the catalog generator either: the route decider
(`LivePlanPreviewRoutingService.Decide` → `V1LiveCatalogPilotRoutingPolicy.Evaluate`,
called at `PlanServices.cs:176`, unconditionally before any catalog path) computes
`withinSupportedCycle` against the real candidate's `CoreCycle.MinimumWeeks`/`MaximumWeeks`
(8/14, mirroring `RaceHorizonPolicy`) and returns `CatalogRequestUnsupported`
(→ `CatalogLivePilotRequestUnsupportedException`) for any cycle length outside
that range — independently of, and before, the `PlanServices` guard's own
continue/throw decision matters for this case. `CoreLengthRecognizedButNotImplemented`
horizons are independently rejected a second time by the route decider's own
`CatalogCoreLengthNotImplemented` branch (→ `PlanCoreHorizonUnsupportedException`,
the identical exception/reason PlanServices already throws). `CompositionRequired`
horizons are also independently rejected a second time by the same
`withinSupportedCycle` check.

Only `RaceHorizonClassification.ExactStandaloneCoreSupported` (availableWeeks == 12)
passes both independent checks and reaches `GenerateCatalogPreviewAsync` →
`CatalogPreviewGenerator.GenerateAsync` → `BuildDarkInternalDatedSkeleton`.

Habit requests can never reach this code path at all:
`V1CatalogPilotIdentityPolicy.IsSupportedIdentity` requires `GoalType.Race`,
so a Habit request's route decision is always `Legacy`, and
`GenerateCatalogPreviewAsync` is only invoked when `routeDecision.Source == GenerationSource.Catalog`.
`CatalogPlanConfirmationService` (the `/plans/confirm` path) has zero dependency
on `ICatalogPreviewGenerator` by explicit doc-comment contract ("must never be
injected or called, directly or indirectly").

## 6. Entry-point table

| Entry point | Race request? | Horizon classified before generator? | Can non-12 reach alignment guard? | Evidence |
|---|---|---|---|---|
| `POST /api/v1/plans/generate-preview/race` (`PlansController.GenerateRacePlanPreview`) | Yes | Yes — `PlanServices.GeneratePreviewAsync` horizon guard (lines 107–153) **and** `LivePlanPreviewRoutingService.Decide` (independent, mirrored bounds) | No | `PlansController.cs:41-47` → `PlanServices.cs:84-85,96-153,176,312-323`; `LivePlanPreviewRouting.cs` `Evaluate` |
| `POST /api/v1/plans/generate-preview/habit` (`PlansController.GenerateHabitPlanPreview`) | No | N/A — can never match `V1CatalogPilotIdentityPolicy` (requires `GoalType.Race`) | No — routes `Legacy`, generator never invoked | `PlansController.cs:51-57`; `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` |
| `POST /api/v1/plans/confirm` (`PlansController.ConfirmPlan` → `CatalogPlanConfirmationService`) | N/A | N/A | No — zero dependency on `ICatalogPreviewGenerator` by explicit design | `CatalogPlanConfirmationService.cs:25-40` doc contract |
| Direct DI resolution of `ICatalogPreviewGenerator` | N/A | N/A | No caller other than `PlanServices` | `Program.cs:208-209` single Scoped registration; grep confirms `PlanServices` is the only class holding this field |
| Test-only direct instantiation (`CatalogPreviewGeneratorTests.cs`, `GeneratePreviewContractEndToEndCatalogTests.cs`, `Phase4F4DarkSkeletonWiringTests.cs`, `Phase4F5DarkCalendarWiringTests.cs`, `Phase4F5_1ProductionValidatorWiringTests.cs`, `Phase4G3B4AHorizonGateAlignmentGuardSeparationTests.cs`) | N/A | N/A (test seam) | N/A — not production | All under `backend/RunningApp.IntegrationTests`, never referenced from `Application`/`Api`/`Infrastructure`/`Persistence` production code |

No bypass path was found. The production guarantee (only exactly-12-week
requests reach the alignment guard) is proven, not inherited from any prior
report.

## 7. Ownership after the separation

- **Horizon support** (is this standalone week count implemented at all): owned
  exclusively by `RaceHorizonPolicy` / the preview-routing layer
  (`PlanServices.GeneratePreviewAsync`'s guard, `LivePlanPreviewRouting`).
  Unchanged by this phase.
- **Composition requirement** (horizon exceeds the standalone maximum): same
  owner, same mechanism, unchanged.
- **Race-date correctness** (is the generated schedule's final session within
  tolerance of `RaceDate`): owned exclusively by `CatalogPreviewGenerator`'s
  alignment guard, now with the week-count clause removed.

## 8. Evidence 12-week public behavior is unchanged

Full backend Release test suite (1278 passed / 2 known-baseline failed / 1280
total — see §19) includes the complete, unmodified `Sw13ExactTwelveWeekOnlyEndToEndTests`
and `CatalogPreviewGeneratorTests` suites, both of which exercise the real
12-week pilot end-to-end and continue to pass unchanged. A fresh live HTTP
request against the built Release binary (see §13) additionally confirmed:
12 weeks returned, final session date `2026-10-11` (identical to the
established checkpoint baseline recorded in the Phase 4G.3B.3 checkpoint
report), `fallback_used: false`, and the same public response shape.

## 9. Evidence 8–11 and 13–14 remain unsupported

`LongHorizonFailClosedTests.InRangeButNotExactTwelve_ThrowsPlanCoreHorizonUnsupported_BeforeLegacyOrCatalog_NoPreviewPersisted`
(theory over 8, 9, 10, 11, 13, 14) and `Sw13ExactTwelveWeekOnlyEndToEndTests.InRangeButNotExactTwelve_ReturnsPlanCoreHorizonUnsupported_NoPersistence`
(same set, real HTTP) both pass unchanged post-refactor. Live re-check (§13)
confirms the same for all six values against the built Release binary.

## 10. Evidence 15+ remains composition-required

`LongHorizonFailClosedTests.TwentyWeekHorizon_ThrowsPlanHorizonCompositionRequired_...`
and `Sw13ExactTwelveWeekOnlyEndToEndTests.FifteenPlusWeeks_StillReturnsPlanHorizonCompositionRequired_NotCoreHorizonUnsupported`
(15, 20) both pass unchanged. Live re-check (§13) confirms the same.

## 11. Internal non-12 alignment test result

`Phase4G3B4AHorizonGateAlignmentGuardSeparationTests.AlignmentGuard_EightWeekCorrectlyAlignedInternalSkeleton_NotRejectedForWeekCount`
constructs a real (non-synthetic) 8-week dated skeleton via the same
real-candidate/real-allocator/real-materializer construction already used by
`RaceDateAlignmentVerifierTests.RealDatedScheduleAsync`, substitutes it via the
generator's existing internal test-only calendar-materializer seam, and calls
`CatalogPreviewGenerator.GenerateAsync` directly (bypassing `PlanServices`
entirely — an internal ownership test only). Result: no
`CatalogRaceDateAlignmentInvalidException` was thrown. (A later, unrelated
`CatalogWorkoutBindingDefinitionInvalidException` occurs downstream, because
this synthetic scenario mismatches an 8-week calendar skeleton against the
real, non-horizon-aware fixed 12-week phase/stage schedule — proving the
alignment guard itself was already passed before that unrelated failure, not
that the whole pipeline is horizon-aware.) The companion test
`AlignmentGuard_PublicEightWeekRouting_RemainsRejectedByHorizonPolicy` confirms
`RaceHorizonPolicy.Classify(8)` is still `CoreLengthRecognizedButNotImplemented`
— the public 8-week route is not enabled by this internal proof.

## 12. Confirmation RaceDateAlignmentVerifier remains dark

Unchanged by this phase (no verifier file was modified). Re-confirmed:
`RaceDateAlignmentVerifierTests` (16/16) still passes unchanged;
`Phase4G3B4AHorizonGateAlignmentGuardSeparationTests.LiveAlignmentGuard_DoesNotInvokeStandaloneRaceDateAlignmentVerifier`
greps `CatalogPreviewGenerator.cs` and confirms it contains no
`RaceDateAlignmentVerifier.Verify(` call. No DI registration exists for it,
and no production call site was found anywhere in `RunningApp.Application`,
`RunningApp.Api`, `RunningApp.Infrastructure`, or `RunningApp.Persistence`.

## 13. Cross-references

- `PHASE4G_3A_EIGHT_WEEK_CORE_ALLOCATION_AUDIT.md` section 15's correction
  addendum (added in governance commit `1a7ed5d4ba9c2c9bf5dd777e6c0155e921c936fa`)
  remains preserved unchanged by this phase.
- Governance commit `1a7ed5d4ba9c2c9bf5dd777e6c0155e921c936fa`
  ("docs(governance): correct race-date horizon claim and add verification rule").
- `TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001`, now `CLOSED` — see
  `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`.

## 14–17. Explicit statements

- This phase does **not** enable any new public race-plan horizon. 8–11,
  13–14 remain 422 `PLAN_CORE_HORIZON_UNSUPPORTED`; 15+ remains 422
  `PLAN_HORIZON_COMPOSITION_REQUIRED`; 12 remains the only 200.
- This phase does **not** create a safety-verification orchestration
  pipeline. `PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md`'s
  "Common properties" section (orchestration not started) is unchanged by
  this phase's status-note addition.
- This phase does **not** wire any standalone safety verifier into
  production, and does not create a support registry.
- Full evidence for build/test execution, live acceptance results, and file
  scope is recorded in the Phase 4G.3B.4a final report delivered alongside
  this document (conversation record) rather than duplicated verbatim here.
