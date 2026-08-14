# Phase 4G.3B.10A — Pre-Checkpoint Fix Validation

## 1. Files inspected

- `PHASE_4G_3B_10_PRE_CHECKPOINT_INDEPENDENT_VALIDATION.md`
- `plan-catalog/artifacts/audits/activation-readiness-risks.json`
- `plan-catalog/artifacts/audits/activation-readiness-risks.md`
- `PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md`
- `PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md`
- `VolumeSafetyPolicy.cs`
- `VolumeProgressionVerifier.cs`
- `CatalogVolumeAndLongRunPlanner.cs`
- `DependencyInjectionResolutionTests.cs`
- `BACKEND_DI_BASELINE_RESOLUTION_CATALOG_LIVE_PILOT.md`
- Preparation Runway contracts, validators, tests, and production-reference search results
- Existing SW-12/SW-13 horizon end-to-end tests

Pre-check HEAD was `3549a8a1eeef18ca96794fa1056043142d13bc78`. `git status --short`, `git diff --name-status`, and `git diff --check` were captured before editing. Existing generated, local-output, Docker, design, response, and unrelated dirty files were left untouched.

## 2. Exact stale claims found

1. The Foundation closure said its repository-wide production scan found 16 files and described that as current. The present tree has 17 because the later dark Preparation Runway contract adds `PreparationNeedProfile.CoreEntryReadiness`.
2. Volume-cap wording could be read repository-wide as saying no reject logic reads the fields. The live planner does not enforce them, but the dark `VolumeProgressionVerifier` reads them and can report violations.
3. `RealHost_AllSixTargetServices_ResolveFromOneScope` counted five catalog services plus an options object as “six target services.”
4. The no-DB skip mentioned missing instrumentation but did not lead with the exact invariant that no reliable interceptor/counter currently measures it.

Historical audit statements that were accurate at their original time were not rewritten. Current-state clarifications were appended.

## 3. Foundation wording fix

Both activation-readiness representations now state:

- the original Phase 4G.3B.9 scan found 16 production files;
- the current repository has 17 after the later dark runway contract;
- the additional item is the neutral `PreparationNeedProfile.CoreEntryReadiness` field;
- it is not consumed by `CatalogPhaseAllocationResolver` or a live race-core allocation path;
- the non-reachability proof and `CLOSED_AS_NON_REACHABLE_IN_CURRENT_SUPPORTED_SCOPE` classification remain unchanged.

No phase bounds, allocation, readiness behavior, threshold, or closure status changed.

## 4. Volume wording fix

Both activation-readiness representations now distinguish:

```text
CatalogVolumeAndLongRunPlanner does not clamp, reject, or alter the live
reachable peak using the caps.

VolumeProgressionVerifier reads the caps and can report violations, but is
dark and unwired from the planner/public generation path.
```

No formula, numeric cap, verifier wiring, preview behavior, or TD status changed.

## 5. DI test rename

Renamed only:

```text
RealHost_AllSixTargetServices_ResolveFromOneScope
→ RealHost_CatalogTargetServices_ResolveFromOneScope
```

At the end of Phase 4G.3B.10A, the five service assertions and separate options-resolution assertion were unchanged. Phase 4G.3B.10B subsequently removed that redundant options resolution because the two dedicated options tests already own the concern; the five service assertions remain unchanged. No feature-flag or no-database assertion was added.

## 6. No-DB skip verification

`RealHost_ServiceResolution_OpensNoDatabaseConnection` remains decorated with `[Fact(Skip=...)]`. Its reason now begins:

```text
No reliable connection interceptor/counter currently measures this no-DB-I/O invariant.
```

It continues to cite `CROSS_PHASE_4G_4A_TO_4G_4B_V_AND_BACKEND_BASELINE_INDEPENDENT_AUDIT.md Part F Option A/C` and explicitly avoids misattributing the gap to the unrelated runway TD.

Focused output proved it remains a genuine discovered skip:

```text
[SKIP] RealHost_ServiceResolution_OpensNoDatabaseConnection
Passed: 8, Failed: 0, Skipped: 1, Total: 9
```

No test was deleted or silently converted to pass.

## 7. Governance parity result

The JSON parsed successfully with `ConvertFrom-Json`. Focused governance tests:

```text
ActivationReadinessRiskParityTests + ActivationSafetyGateTests
Passed: 15, Failed: 0, Skipped: 0, Total: 15
```

JSON/Markdown parity and the documentation-only/non-mechanical-consumption guard remain intact.

## 8. Focused test totals

```text
DependencyInjectionResolutionTests
Passed: 8, Failed: 0, Skipped: 1, Total: 9

FoundationCompression / ReadinessEligibility / VolumeProgression / PreparationRunway
Passed: 77, Failed: 0, Skipped: 0, Total: 77
```

An initial sandboxed DI invocation failed during NuGet restore with `NU1301` TLS credential errors before test execution. It was rerun through the approved dotnet environment, restored successfully, built, and produced the clean totals above; this was infrastructure setup, not a test failure.

## 9. 8-week live result

The existing real-host HTTP regression suite passed and confirmed:

```text
HTTP 422
PLAN_CORE_HORIZON_UNSUPPORTED
```

No preview response or plan persistence is produced, and the Preparation Runway path remains unreachable.

## 10. 12-week live result

The same fresh regression batch confirmed:

```text
HTTP 200
12 weeks
4 sessions/week
48 total sessions
```

Established assertions for no rest-day rows, core phase structure, response shape, preview-only persistence behavior, and absence of public Preparation Runway metadata remain passing.

## 11. 14-week live result

Fresh real-host coverage confirmed:

```text
HTTP 422
PLAN_CORE_HORIZON_UNSUPPORTED
```

The established typed error response remains unchanged. The closed allocation-priority decision was not applied to live routing.

## 12. 20-week guard result

Fresh real-host coverage confirmed:

```text
HTTP 422
PLAN_HORIZON_COMPOSITION_REQUIRED
```

Combined SW-12/SW-13 horizon batch:

```text
Passed: 21, Failed: 0, Skipped: 0, Total: 21
```

Preparation Runway composition remains dark.

## 13. Full backend Run 1

```text
Passed: 1,394
Failed: 0
Skipped: 1
Total: 1,395
Duration: 2m43s
```

## 14. Full backend Run 2

```text
Passed: 1,394
Failed: 0
Skipped: 1
Total: 1,395
Duration: 2m46s
```

The two runs are identical. The only skip in both is the explicit no-DB instrumentation gap test.

Release build result:

```text
Build succeeded.
Warnings: 0
Errors: 0
```

## 15. Files changed

Files intentionally modified by Phase 4G.3B.10A:

- `backend/RunningApp.IntegrationTests/DependencyInjectionResolutionTests.cs`
- `plan-catalog/artifacts/audits/activation-readiness-risks.json`
- `plan-catalog/artifacts/audits/activation-readiness-risks.md`
- `PHASE_4G_3B_10_PRE_CHECKPOINT_INDEPENDENT_VALIDATION.md` (direct follow-up status/name clarification)

File created:

- `PHASE_4G_3B_10A_PRE_CHECKPOINT_FIX_VALIDATION.md`

## 16. Files explicitly not changed

- `CatalogVolumeAndLongRunPlanner.cs` and all volume algorithms/values
- `VolumeSafetyPolicy.cs` and `VolumeProgressionVerifier.cs` beyond their pre-existing dirty state
- allocation, readiness resolver, phase constraints, and catalog template data
- Preparation Runway contracts, validators, tests, allocator/composer/wiring
- horizon policy, endpoints, DTOs, persistence, frontend
- generated/local/Docker/design/response files
- unrelated TDs and ten-k audit artifacts

## 17. Confirmation no runtime behavior change

No runtime production file was changed by this pass. No horizon was enabled; 8 and 14 remain core-unsupported, 12 remains live, and 20 remains composition-required. No allocator/verifier/runway type was newly wired. No Foundation readiness threshold was invented. Volume, endpoint, DTO, and persistence behavior remain unchanged.

## 18. Confirmation no commit or push

No file was staged. No commit, amend, rebase, reset, history rewrite, branch operation, or push occurred.
