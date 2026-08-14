# Backend Test Baseline Stabilization — Reset and DI Investigation

## Executive result

The required pre-check triggered a mandatory stop condition before any fix was attempted. The fresh full backend run did **not** reproduce the stated 14-failure baseline: all 12 reset-endpoint HTTP 500 failures disappeared and only the two `DependencyInjectionResolutionTests` assertions failed.

Result: `STOPPED — INITIAL_FAILURE_SET_MATERIALLY_DIFFERENT`.

No reset implementation, DI/configuration, production code, test, Preparation Runway file, or existing documentation was changed. The smallest missing evidence is a reproducible reset failure that exposes its actual server-side exception. Modifying reset behavior without that evidence would violate `RESET_HTTP_500_ROOT_CAUSE_NOT_OBSERVED`.

## Initial failure inventory

HEAD before the pass:

```text
3549a8a1eeef18ca96794fa1056043142d13bc78
3549a8a docs(catalog): clarify GoalPaceReachabilityVerifier measures theoretical completeness, not runtime safety
```

The Phase 4G.4B.V run immediately preceding this pass had reported 1,373 passed, 14 failed, 0 skipped, 1,387 total. Its preserved failure list was:

1. `DependencyInjectionResolutionTests.RealHost_CatalogLivePilotOptions_DefaultsToDisabled`
2. `DependencyInjectionResolutionTests.RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`
3. `GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests.Characterization_UserDefined_NoRecentRace_Returns422_RuntimeConditionUnsupported_NotSilentFallback_NoPersistence`
4. `GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests.Control_ProductAverage_NoRecentRace_Returns200_TwelveWeeks_FallbackUsedFalse_NoPersistenceGrowthBeyondPreview`
5. `ResetEndpointRelationalScenarioTests.ResetEndpoint_AllEightRequiredScenarios_SucceedWithoutFkViolationOrStaleState`
6. `Sw02ProductAverageEndToEndTests.Sw02Request_ReturnsHttp200_With12Weeks_4SessionsPerWeek_48TotalSessions_NoRestRows`
7. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_IsNotRejectedByTransportValidation`
8. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`
9. `Sw12LongHorizonFailClosedEndToEndTests.VerifiedRegressionCase_NoLegacyFallback_NoPreviewOrPlanPersistence`
10. `Sw12LongHorizonFailClosedEndToEndTests.InRangeBoundaryHorizon_Returns422_PlanCoreHorizonUnsupported_NotHttp200(weeks: 8)`
11. `Sw12LongHorizonFailClosedEndToEndTests.InRangeBoundaryHorizon_Returns422_PlanCoreHorizonUnsupported_NotHttp200(weeks: 14)`
12. `Sw12LongHorizonFailClosedEndToEndTests.UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(weeks: 15)`
13. `Sw12LongHorizonFailClosedEndToEndTests.UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(weeks: 16)`
14. `Sw12LongHorizonFailClosedEndToEndTests.UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(weeks: 24)`

The 12 non-DI failures had reached the shared `POST /api/v1/testing/reset` helper and observed HTTP 500. That prior client-side symptom did not contain the underlying server exception.

Fresh pre-change command:

```text
dotnet test backend/RunningApp.sln -c Release --no-build --logger "console;verbosity=normal"
```

Fresh result: 1,385 passed, 2 failed, 0 skipped, 1,387 total. Only these failures were emitted:

```text
RunningApp.IntegrationTests.DependencyInjectionResolutionTests
  .RealHost_CatalogLivePilotOptions_DefaultsToDisabled

Assert.False() Failure
Expected: False
Actual:   True
DependencyInjectionResolutionTests.cs:line 107

RunningApp.IntegrationTests.DependencyInjectionResolutionTests
  .RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection

Assert.False() Failure
Expected: False
Actual:   True
DependencyInjectionResolutionTests.cs:line 126
```

Therefore the 12 reset failures are not deterministic across these consecutive full-suite runs. They were not reproduced in the mandatory pre-change run.

## Working-tree attribution

The pre-check ran `git status --short`, `git diff --check`, and `git diff --name-status` before editing.

| Origin | Existing dirty content |
| --- | --- |
| Phase 4G.4A/4G.4B/4G.4B.V | the canonical reconciliation audit, typed-contract documentation, internal Preparation Runway contract/validator folder, focused tests, and current-state validation document |
| Earlier Phase 4G.3B.7/3B.8 work | volume policy/governance changes, volume verifier change, and two decision-audit documents |
| Governance/audit state | activation-readiness and ten-k audit JSON/Markdown pairs |
| Generated output | dirty/untracked `backend/**/bin/**` and `backend/**/obj/**` Debug/Release content |
| Local output/configuration | `.claude/`, local acceptance document, response/calendar JSON, `baseline_tmp/`, local `docker-compose.yml` |
| Design material | dirty/untracked `design-references/*.png` |

`git diff --check` returned exit code 0 with no whitespace/conflict-marker defect; it printed only existing LF-to-CRLF warnings. None of the pre-existing dirty files was modified by this pass.

## Track A root cause

Not established. The mandatory fresh run exercised `/api/v1/testing/reset` successfully and did not produce the HTTP 500. Consequently there is no observed exception type, message, inner exception, failing repository stack frame, database operation, entity, or table to report from this pass.

Classification: `RESET_HTTP_500_ROOT_CAUSE_NOT_OBSERVED`.

The evidence supports only that the earlier reset failure pattern is transient/non-deterministic. It does not support a foreign-key-order, transaction, parallelism, context-lifetime, or configuration diagnosis.

## Track A fix

No fix was made. Adding deletion statements, retries, exception suppression, or test changes without observing the real exception would be speculative and prohibited.

## Reset data dependency/order analysis

Not performed beyond the pre-check because the failure set materially differed and the stop condition applies before implementation investigation. No deletion order was changed or proposed as fact.

## Reset safety and environment restriction

The fresh run demonstrates that `/api/v1/testing/reset` can succeed in the current test environment. This pass did not establish new evidence about empty/partial/full graph idempotency or production blocking and did not change either behavior.

## Track A focused test results

Not run after the stop condition. The mandatory full run already showed that all 12 previously affected cases passed in that run. A representative/group investigation intended to expose the exception cannot do so while the symptom is absent.

## Track B root cause for each test

No Track B root-cause investigation or fix was started after the mandatory stop. The observed facts are limited to both tests reading `true` where they assert `false`.

Required cause classifications remain **undetermined**; assigning `STALE_TEST_EXPECTATION`, `ACCIDENTAL_CONFIGURATION_OVERRIDE`, `ENVIRONMENT_LEAK`, `OPTIONS_BINDING_DEFECT`, `TEST_HOST_CONFIGURATION_DEFECT`, or `MULTIPLE_CONCERNS_IN_ONE_TEST` without completing the required evidence review would be premature.

## Configuration-vs-test decision

Not made. Although test output shows `activationEnabled=True` in runtime routing logs, this pass stopped before tracing the approved activation decision and configuration precedence. Accordingly, it does not claim whether the live-pilot enabled state is intentional.

## Track B fix

No DI/configuration or test file changed. The service-resolution invariant was not weakened or combined with a feature-flag workaround.

## Full regression result

Pre-change full backend run:

```text
Failed: 2
Passed: 1,385
Skipped: 0
Total: 1,387
```

Exact remaining failures are the two DI tests listed above. Because the suite is not green and the stop condition fired, no Release build/final regression sequence was claimed as completed. Plan-catalog tests were not run because no plan-catalog code or mechanically consumed artifact changed.

## Files changed

Created by this pass only:

```text
BACKEND_TEST_BASELINE_STABILIZATION_RESET_AND_DI_FIX.md
```

No existing file was modified or deleted.

## Files explicitly not changed

- Reset endpoint/controller/service and persistence implementation
- `DependencyInjectionResolutionTests.cs`
- options classes, `Program`, appsettings, and test-host configuration
- public endpoints and DTOs
- horizon policy and preview behavior
- all pre-existing dirty files

## Preparation Runway non-impact confirmation

The following remained untouched:

- `PreparationRunwayContracts.cs`
- `PreparationRunwayValidators.cs`
- `PreparationRunwayContractsTests.cs`
- `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md`
- `PHASE4G_4B_PREPARATION_RUNWAY_TYPED_CONTRACTS.md`
- `PHASE4G_4B_V_PREPARATION_RUNWAY_CURRENT_STATE_VALIDATION.md`

Preparation Runway remains dark and unwired. No allocator, route, materializer, prescription, composer, or activation work occurred. Public horizon behavior, including `PLAN_HORIZON_COMPOSITION_REQUIRED`, was not changed.

## Remaining risks

- The transient reset-endpoint HTTP 500 root cause remains unknown because it did not reproduce.
- The two DI assertions remain red and their configuration-versus-test classification remains unresolved because the mandatory mismatch stop preceded Track B investigation.
- A future diagnostic run should capture server logs/exception details at the first reset failure before changing cleanup behavior.

No test was skipped, suppressed, retried, or weakened. No broad exception handling was added.

## Commit/push status

No commit, amend, rebase, reset, history rewrite, branch switch, or push was performed.

## Stop conditions

Triggered:

```text
INITIAL_FAILURE_SET_MATERIALLY_DIFFERENT
RESET_HTTP_500_ROOT_CAUSE_NOT_OBSERVED
```

Not established:

```text
RESET_FAILURES_HAVE_MULTIPLE_UNRELATED_ROOT_CAUSES
RESET_FIX_REQUIRES_PRODUCTION_DATA_DELETION_CHANGE
LIVE_PILOT_CONFIGURATION_INTENT_CANNOT_BE_DETERMINED
DI_TEST_FAILURE_CAUSE_CANNOT_BE_SEPARATED_FROM_FEATURE_ACTIVATION
FIX_REQUIRES_WEAKENING_TEST_ISOLATION
```

Anything not completed: Track A exception capture/root-cause/fix and focused tests; Track B intent analysis/classification/fix; Release build; final green full-suite run; regression checks dependent on those fixes. The pass stops here as required rather than describing a non-green result as complete.
