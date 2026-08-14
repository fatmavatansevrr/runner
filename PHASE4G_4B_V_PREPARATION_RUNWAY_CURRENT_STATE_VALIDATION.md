# Phase 4G.4B.V — Preparation Runway Current-State Validation

## 1. Executive verdict

Phase 4G.4A is complete as a canonical-source reconciliation audit. Phase 4G.4B implemented a dark, internal, neutral contract vocabulary, structural validators, focused tests, documentation, and a source-scanning reachability guard. No route selector, allocator, workout/volume/pace prescription, composer, endpoint, DI registration, persistence path, or runtime activation exists.

No stop condition was found. The implementation is scope-compliant and ready for a named-decision-resolution phase, not yet for an allocator or prescription phase. Two minor gaps are intentionally visible: the 13–14 versus 15+ composition classifier is not executable yet, and unresolved route/coaching decisions remain unencoded.

## 2. HEAD and working-tree state

- HEAD: `3549a8a1eeef18ca96794fa1056043142d13bc78`
- HEAD subject: `docs(catalog): clarify GoalPaceReachabilityVerifier measures theoretical completeness, not runtime safety`
- No commit occurred after the uncommitted Phase 4G.4B work.
- `git diff --check`: exit 0; no whitespace or conflict-marker errors. Git emitted only LF-to-CRLF working-copy warnings on existing dirty files.
- `git diff --name-status` showed tracked changes from earlier Phase 4G.3B.7/3B.8 work, generated build outputs, governance artifacts, and local assets. Phase 4G.4A/4B files are untracked.

Dirty-file classification:

| Origin | Files |
| --- | --- |
| Phase 4G.4A | `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md` |
| Phase 4G.4B | `PHASE4G_4B_PREPARATION_RUNWAY_TYPED_CONTRACTS.md`; `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunway/*`; `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/PreparationRunway/*` |
| Prior Phase 4G.3B.7/3B.8 and governance work | `PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md`, `PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md`, `PHASE4G_3B_8_ALLOCATION_PRIORITY_EXTENSION_DECISION_AUDIT.md`, `VolumeSafetyPolicy.cs`, `VolumeProgressionVerifier.cs`, activation-readiness and ten-k audit pairs |
| Generated build artifacts | all dirty/untracked `backend/**/bin/**` and `backend/**/obj/**` paths |
| Local acceptance/output | `.claude/`, `LOCAL_CATALOG_ACCEPTANCE_TEST.md`, `backend/*_response.json`, `backend/calendar_july.json`, `baseline_tmp/`, `docker-compose.yml` |
| Design assets | modified/untracked `design-references/*.png` |

This pass did not alter any of those pre-existing files.

## 3. Files inspected

- `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md` (filename confirmed exactly)
- `PHASE4G_4B_PREPARATION_RUNWAY_TYPED_CONTRACTS.md`
- `PreparationRunwayContracts.cs`
- `PreparationRunwayValidators.cs`
- `PreparationRunwayContractsTests.cs`
- Application, API, Infrastructure, Persistence, and integration-test references found by repository-wide searches for runway/composition/partial/need/planning/allocation/profile vocabulary
- `ProgressionStageAllocator`, `CatalogWorkoutBinder`, `CatalogVolumeAndLongRunPlanner`, and `PaceSourceResolver` contracts and call shapes

## 4. Implementation inventory

| Type/file | Exists | Layer | Production/test/doc | Wired | Purpose/status |
| --- | ---: | --- | --- | ---: | --- |
| `PreparationRunwayBlockType` | yes | Application runway contracts | production | no | dedicated four-value taxonomy; IMPLEMENTED |
| `PreparationRunwayPrescriptionProfile` | yes | Application runway contracts | production | no | `Standard`/`Light` identity; IMPLEMENTED |
| `PreparationRunwayPlanningStatus` | yes | Application runway contracts | production | no | explicit outcomes; IMPLEMENTED |
| `PreparationRunwayPlanningReason` | yes | Application runway contracts | production | no | machine-readable fail-closed reasons; IMPLEMENTED |
| `PreparationRunwayContext` | yes | Application runway contracts | production | no | inputs and derived date facts; IMPLEMENTED |
| `PreparationNeedProfile` | yes | Application runway contracts | production | no | neutral six-axis carrier; IMPLEMENTED |
| `PreparationRunwayBlockAllocation` | yes | Application runway contracts | production | no | full-week block fact; IMPLEMENTED |
| `PreparationRunwayAllocation` | yes | Application runway contracts | production | no | allocation result shape, not allocator; IMPLEMENTED |
| `PreparationRunwayLeadingPartialSpan` | yes | Application runway contracts | production | no | separate calendar metadata; IMPLEMENTED |
| `RacePlanCompositionMetadata` | yes | Application runway contracts | production | no | day-accurate composition facts; IMPLEMENTED |
| `PreparationRunwayPlanningResult` | yes | Application runway contracts | production | no | status/allocation/findings envelope; IMPLEMENTED |
| Structural validators | yes | Application runway validators | production | no | invariant checks only; IMPLEMENTED |
| Focused/dark tests | yes | Integration tests | test | test-only | 41 scenarios and source reachability guard; IMPLEMENTED |
| Allocator/planner/composer | no | — | — | no | NOT_IMPLEMENTED |

## 5. Scope compliance

Verdict: `SCOPE_COMPLIANT`.

The implementation contains only neutral contracts, structural validators, tests, documentation, and dark-reachability protection. Searches found no New→Beginner mapper; experience route selection; automatic Light selection; block-count/order selection; runway allocator; readiness routing; skeleton/session generation; workout binding; kilometre, long-run, pace or repetition calculation; composition execution; or runtime activation.

## 6. Taxonomy and vocabulary

`PreparationRunwayBlockType` contains exactly `Consistency`, `GeneralEndurance`, `AerobicStrength`, and `PreSpecificTransition`. It contains none of the race-core stages, partial pseudo-stages, or `AerobicStrengthLight`, and does not inherit, alias, or reuse a core phase type. Verdict: `STRICTLY_SEPARATED`.

`AerobicStrength` plus profile `Light` is representable. The validator limits `Light` to that block type but nothing selects it automatically. Verdict: `NEUTRAL_REPRESENTATION`.

Plan-catalog `New` and backend `Beginner` remain vocabulary-qualified unequal values, explicitly tested. No switch, mapper, converter, factory, or cast introduces a mapping. Verdict: `UNRESOLVED_AND_PRESERVED`.

## 7. Date model

The validator uses inclusive race anchoring: `CoreStartDate = RaceDate - (PreferredCoreWeeks * 7 - 1)`, then derives runway days by `CoreStartDate.DayNumber - StartDate.DayNumber`, full weeks by integer division, and leading days by remainder. Race and core-start days are included; runway ends immediately before core start. Negative runway for runway composition is invalid.

| Scenario | Re-derived result |
| --- | --- |
| exact 12-week core | 84 inclusive core days; race minus 83; 0 runway |
| 12 weeks + 2 days | 2 runway/partial days |
| exact 20-week horizon with a 12-week core | 56 runway days; 8 full weeks |
| 20 weeks + 3 days | 59 runway days; 8 full weeks + 3 partial days |
| start equals core start | 0 runway days |
| start after core start | invalid |

Focused tests cover runway-day values 0, 1, 2, 6, 7, 58 and 59 plus bad anchors and negative runway. Verdict: `CORRECT`.

## 8. Partial-span model

`LeadingPartialDays` is constrained to 0..6. `RunwayDays = FullRunwayWeeks * 7 + LeadingPartialDays`, while block full-week totals must equal only `FullRunwayWeeks`. A partial span is absent at zero and required otherwise; it allows zero sessions, disallows quality progression, and inherits the first full block/profile. It cannot compensate for a missing full week or a block minimum. No partial block taxonomy exists. Verdict: `CORRECTLY_SEPARATED`.

## 9. Need-profile assessment

The six need levels carry no scoring, threshold, formula, derived calculation, route choice, block choice, or runtime behavior. Classification: `NEUTRAL_CONTRACT_ONLY` and, operationally, an `UNUSED_FUTURE_EXTENSION_POINT`.

Readiness can be represented independently of two partial days; no code can reinterpret `NotReady` as `Ready`. Verdict: `REPRESENTABLE_BUT_UNRESOLVED`. This preserves `TD-FOUNDATION-COMPRESSION-001`: partial calendar availability does not resolve foundation/readiness compression policy.

## 10. Allocation/prescription boundary

Runway contracts carry only block type/profile, full-week count, sequence, date boundaries, and planning outcome. They contain no weekly/session kilometres, volume endpoints, long-run distance, workout family, pace, duration, RPE, repetition count, or cutback amount. Verdict: `BOUNDARY_PRESERVED`.

| Type/component | Intended layer | Actual layer | Correct | Issue |
| --- | --- | --- | ---: | --- |
| context/date facts | composition input | runway contracts | yes | none |
| block/profile vocabulary | allocation contract | runway contracts | yes | decisions unresolved |
| structural validators | contract boundary | runway validators | yes | no selection behavior |
| generic stage allocator | core allocation | progression | yes | not reused by runway |
| workout binder | post-skeleton binding | binding | yes | core-shaped inputs |
| volume/long-run planner | prescription | volume prescription | yes | bound-core-plan inputs |
| pace source resolver | evidence resolution | resolver | yes | not a runway pace prescription |

Future reuse assessment:

| Component | Classification | Evidence |
| --- | --- | --- |
| `CatalogVolumeAndLongRunPlanner` | `REUSE_WITH_ADAPTER` | consumes a bound catalog/core plan and core volume policy shapes |
| `CatalogWorkoutBinder` | `REUSE_WITH_ADAPTER` | consumes a dated core skeleton and progression-stage binding context |
| pace resolver | `UNRESOLVED` | current resolver selects an evidence source; it does not establish runway pace-prescription compatibility |
| weekly progression | `REUSE_WITH_ADAPTER` | current allocator/stages are race-core catalog semantics |
| long-run progression | `REUSE_WITH_ADAPTER` | current logic is embedded in the bound-plan volume path |

## 11. Planning outcomes

Statuses are exactly `Planned`, `DecisionRequired`, `Unsupported`, `NotApplicable`, and `InvalidInput`. Reasons cover all requested unresolved decisions, capacity/continuation failures, insufficient runway, and structural/date/allocation failures. Planned requires a valid allocation and no findings; non-planned states cannot carry allocation; decision, unsupported, and invalid states require a reason from their category; not-applicable cannot claim runway. Verdict: `EXPLICIT_AND_FAIL_CLOSED`.

## 12. Dark reachability

Production references are confined to the two files in the internal Application `Schedule/PreparationRunway` folder. Tests scan Application, API, Infrastructure, and Persistence outside that folder and reject a `PreparationRunway` reference. Additional searches found no endpoint, handler, preview/schedule generator, DI registration, persistence model, public DTO, reflection target, delegate, method-group, or static-member reachability. Verdict: `DARK_AND_UNWIRED`.

## 13. Generic allocator status

`ProgressionStageAllocator` exists for race-core progression, but neither contracts nor validators reference or invoke it. No runway route allocation exists. Verdict: `NOT_YET_USED`.

## 14. 13–14 vs 15+ boundary

The audit documentation preserves 8–14 as standalone-core mechanics and 15+ as the future runway-plus-core boundary; for 10K it records preferred 12 and maximum 14. The executable Phase 4G.4B contracts carry preferred core weeks and a caller-supplied composition type, but no maximum-core field or classifier. No incorrect `available > preferred => runway` implementation exists. Verdict: `NOT_IMPLEMENTED_YET`, a named pre-allocator gap rather than `PREFERRED_MAXIMUM_CONFUSION`.

## 15. Long-horizon failure representability

The day fields can represent 15, 20, 22, 30, and 40-week horizons without allocating them. `DecisionRequired` and `Unsupported`, together with `LongRunwayCapacityExceeded` and `LongRunwayContinuationPolicyMissing`, permit explicit failure without truncation, repeated mesocycles, or unbounded allocation. Verdict: `SAFE_EXPLICIT_FAILURE_READY`.

## 16. Test results

Commands run:

1. `dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj -c Release --filter "FullyQualifiedName~PreparationRunway" --logger "console;verbosity=minimal"` — 41 passed, 0 failed, 0 skipped, total 41.
2. `dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj -c Release --no-build --filter "FullyQualifiedName~Architecture|FullyQualifiedName~PreparationRunway" --logger "console;verbosity=minimal"` — 41 passed, 0 failed, 0 skipped, total 41. The runway suite itself contains the architecture/reachability, date, planning-result, taxonomy, and neutrality tests.
3. First full-suite attempt reached the 120-second command timeout before emitting a result.
4. `dotnet test backend/RunningApp.sln -c Release --no-build --logger "console;verbosity=minimal"` — 1,373 passed, 14 failed, 0 skipped, total 1,387 (2m23s).

Exact failures in the completed full run:

- `DependencyInjectionResolutionTests.RealHost_CatalogLivePilotOptions_DefaultsToDisabled`
- `DependencyInjectionResolutionTests.RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`
- `GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests.Characterization_UserDefined_NoRecentRace_Returns422_RuntimeConditionUnsupported_NotSilentFallback_NoPersistence`
- `GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests.Control_ProductAverage_NoRecentRace_Returns200_TwelveWeeks_FallbackUsedFalse_NoPersistenceGrowthBeyondPreview`
- `ResetEndpointRelationalScenarioTests.ResetEndpoint_AllEightRequiredScenarios_SucceedWithoutFkViolationOrStaleState`
- `Sw02ProductAverageEndToEndTests.Sw02Request_ReturnsHttp200_With12Weeks_4SessionsPerWeek_48TotalSessions_NoRestRows`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_IsNotRejectedByTransportValidation`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`
- `Sw12LongHorizonFailClosedEndToEndTests.VerifiedRegressionCase_NoLegacyFallback_NoPreviewOrPlanPersistence`
- `Sw12LongHorizonFailClosedEndToEndTests.InRangeBoundaryHorizon_Returns422_PlanCoreHorizonUnsupported_NotHttp200(weeks: 8)`
- `Sw12LongHorizonFailClosedEndToEndTests.InRangeBoundaryHorizon_Returns422_PlanCoreHorizonUnsupported_NotHttp200(weeks: 14)`
- `Sw12LongHorizonFailClosedEndToEndTests.UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(weeks: 15)`
- `Sw12LongHorizonFailClosedEndToEndTests.UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(weeks: 16)`
- `Sw12LongHorizonFailClosedEndToEndTests.UnsupportedLongHorizon_Returns422_PlanHorizonCompositionRequired(weeks: 24)`

The first two are the named local activation-configuration baseline. The other 12 failed at the shared reset endpoint with HTTP 500 in this run, matching the repository's documented `TD-TESTFLAKE-001` observation. That historical classification does not prove a root cause for this run; it only establishes that the pattern predates Phase 4G.4B. The focused runway suite is clean, and no failing stack reaches a runway member. Plan-catalog tests were not run because this pass and Phase 4G.4B changed no plan-catalog code or mechanically consumed artifact.

## 17. Open decisions

| Decision | Resolved | Encoded where | Approved source | Blocks next phase |
| --- | ---: | --- | --- | ---: |
| New/Beginner mapping | no | explicit vocabulary distinction only | none | yes |
| AerobicStrength Light semantics | no | representable profile only | none | yes |
| <=3 route | no | reason vocabulary only | none | yes |
| 4–6 route | no | reason vocabulary only | none | yes |
| 7–10 route | no | reason vocabulary only | none | yes |
| 11+ route | no | capacity/continuation reasons only | none | yes |
| Advanced third block | no | no selector | none | yes |
| maximum capacity exhaustion | no | explicit failure reasons only | none | yes |
| minimum effective runway | no | `InsufficientEffectiveRunway` only | none | yes |
| readiness routing | no | need profile and unresolved reason | none | yes |
| experience sole input vs default prior | no | experience reference only | none | yes |

## 18. Next-phase readiness

| Capability | Expected now | Exists | Wired | Verdict |
| --- | ---: | ---: | ---: | --- |
| date calculation | yes | yes | no | structurally ready |
| runway context | yes | yes | no | ready |
| block taxonomy | yes | yes | no | ready |
| planning outcomes | yes | yes | no | fail-closed ready |
| structural validation | yes | yes | no | ready |
| route selection | no | no | no | blocked by decisions |
| block allocation | shape only | shape only | no | allocator absent |
| partial materialization | metadata only | metadata only | no | behavior absent |
| week skeleton | no | no | no | not ready |
| workout binding | no | no | no | not ready |
| volume planning | no | no | no | not ready |
| long-run planning | no | no | no | not ready |
| pace prescription | no | no | no | not ready |
| runway/core composition | no | metadata only | no | not ready |
| runtime activation | no | no | no | dark |

Final classifications:

```text
PHASE_4G_4A_STATUS=COMPLETE
PHASE_4G_4B_STATUS=COMPLETE_WITH_MINOR_GAPS
TYPED_CONTRACT_NEUTRALITY=SCOPE_COMPLIANT
DATE_MODEL_CORRECTNESS=CORRECT
PARTIAL_SPAN_CORRECTNESS=CORRECT
TAXONOMY_SEPARATION=STRICTLY_SEPARATED
ALLOCATION_PRESCRIPTION_BOUNDARY=BOUNDARY_PRESERVED
PREPARATION_NEED_PROFILE_STATUS=NEUTRAL_CONTRACT_ONLY
PLANNING_OUTCOME_STATUS=EXPLICIT_AND_FAIL_CLOSED
DARK_REACHABILITY_STATUS=DARK_AND_UNWIRED
GENERIC_ALLOCATOR_REUSE_STATUS=NOT_YET_USED
LONG_HORIZON_FAILURE_READINESS=SAFE_EXPLICIT_FAILURE_READY
ALLOCATOR_READINESS=BLOCKED_BY_NAMED_DECISIONS
PRESCRIPTION_READINESS=NOT_READY
COMPOSER_READINESS=NOT_READY
RUNTIME_ACTIVATION_STATUS=DARK_AND_UNWIRED
NEXT_PHASE_RECOMMENDATION=READY_FOR_DECISION_RESOLUTION
```

## 19. Exact file created

Only `PHASE4G_4B_V_PREPARATION_RUNWAY_CURRENT_STATE_VALIDATION.md` was created by this validation pass.

## 20. Confirmation no code/catalog/TD/frontend change

No production code, test, plan-catalog artifact, technical-debt record, frontend file, or existing documentation file was modified by this pass. Test execution updated pre-existing generated `bin`/`obj` outputs only; those are not deliverables.

## 21. Confirmation no commit or push

No commit, amend, rebase, reset, history rewrite, branch operation, or push was performed.
