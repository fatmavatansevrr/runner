# Phase 4G.5B.0A - Preparation Runway Dark-Reachability Test Reconciliation

## Original failing invariant

The former `DarkReachability_NoProductionConsumerOrDiRegistrationOutsideContractFolder` test rejected every production source file outside the Preparation Runway folder containing the text `PreparationRunway`. After Phase 4G.5A, that included `CoreHorizonClassifier.cs`, whose enum vocabulary legitimately names `PreparationRunwayPlusCore` without consuming or invoking any runway contract.

## Why textual matching became invalid

A composition classification is not a contract consumer, DI registration, planner invocation, public DTO, or persistence path. The old substring assertion conflated neutral vocabulary with executable reachability and produced a false positive.

## Reference inventory

| File | Reference type | Contract consumption? | Runtime call? | DI registration? | Allowed? |
| --- | --- | ---: | ---: | ---: | ---: |
| `Schedule/Horizon/CoreHorizonClassifier.cs` | `CoreHorizonMode.PreparationRunwayPlusCore` vocabulary and classification | No | No | No | Yes |
| `Schedule/PreparationRunway/PreparationRunwayContracts.cs` | Contract declarations in their owning folder | Definition only | No | No | Yes |
| `Schedule/PreparationRunway/PreparationRunwayValidators.cs` | Validator declarations in their owning folder | Definition only | No external call | No | Yes |
| `RunningApp.Api/Program.cs` | Service registrations inspected | No runway reference | No | No runway registration | Yes |
| `CatalogPreviewGenerator.cs` | Live preview implementation inspected | No runway reference | No runway invocation | No | Yes |
| Remaining Application/Api/Infrastructure/Persistence sources | Semantic symbol scan | No | No | No | Yes |

## Allowed classifier reference

The revised test invokes the classifier for an exact 15-week elapsed horizon and proves it returns `PreparationRunwayPlusCore`. It then subjects the classifier source to the same forbidden contract and runtime-wiring scans. The enum value is allowed; concrete runway contracts and behavior remain forbidden.

## Forbidden production references

The production scan rejects concrete consumption of `PreparationRunwayContext`, `PreparationRunwayAllocation`, `PreparationRunwayPlanningResult`, `PreparationRunwayBlockAllocation`, `PreparationRunwayLeadingPartialSpan`, `PreparationNeedProfile`, and `RacePlanCompositionMetadata`. A separate runtime scan rejects runway planners, allocators, materializers, composers, validators, and allocator interface references outside the owning folder.

## Revised test structure

The broad substring assertion was replaced by:

1. no production contract consumer outside the contract folder;
2. no DI registration or live behavior invocation;
3. explicit allowance and execution proof for classifier-only vocabulary;
4. adversarial controls using the same scanners.

No filename exemption is used for `CoreHorizonClassifier.cs`.

## Adversarial controls

The tests prove detection of these synthetic violations:

- `CatalogPreviewGenerator` invoking `PreparationRunwayPlanningResultValidator.Validate`;
- `Program.cs` registering `IPreparationRunwayAllocator`;
- a public DTO exposing `PreparationRunwayAllocation`.

## Focused test totals

```text
PreparationRunwayContractsTests: 52 passed, 0 failed, 0 skipped
CoreHorizonClassifierTests: 22 passed, 0 failed, 0 skipped
AllocationOrderCorrectnessVerifierTests: 12 passed, 0 failed, 0 skipped
Release build: succeeded, 0 warnings, 0 errors
```

## Horizon regression

The 21-test real-host horizon regression passed completely. Existing assertions preserve:

- 8 weeks: HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`;
- 12 weeks: HTTP 200;
- 14 weeks: HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`;
- 20 weeks: HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`.

The internal composition vocabulary does not change public routing.

## Full backend Run 1

```text
Failed: 0, Passed: 1422, Skipped: 1, Total: 1423
```

The skip is the intentional `RealHost_ServiceResolution_OpensNoDatabaseConnection` no-DB-I/O test.

## Full backend Run 2

```text
Failed: 0, Passed: 1422, Skipped: 1, Total: 1423
```

The totals match Run 1 exactly.

## Files changed

- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/PreparationRunway/PreparationRunwayContractsTests.cs`
- `PHASE4G_5B_0A_PREPARATION_RUNWAY_DARK_REACHABILITY_TEST_RECONCILIATION.md`

Existing Phase 4G.5B.0 verifier/governance working-tree changes were retained and not modified by this reconciliation.

## Runtime non-impact

No production file, Preparation Runway contract, validator, classifier, horizon policy, endpoint, DTO, persistence path, allocator, workout logic, volume logic, pace logic, or calendar materializer changed. Preparation Runway remains dark and unregistered.

## Commit/push status

No file was staged. No commit or push was performed.

## Final classifications

```text
DARK_REACHABILITY_TEST_STATUS=SEMANTICALLY_CORRECTED
CLASSIFIER_REFERENCE_STATUS=ALLOWED_NEUTRAL_VOCABULARY
CONTRACT_CONSUMPTION_STATUS=NONE
DI_REGISTRATION_STATUS=NONE
LIVE_INVOCATION_STATUS=NONE
PUBLIC_HORIZON_BEHAVIOR=UNCHANGED
PHASE_4G_5B_0_FINALIZATION_READINESS=READY
```
