# Phase 4G.3B.10 — Independent Pre-Checkpoint Validation

## 1. Repository state baseline

### HEAD and history

```text
git rev-parse HEAD
3549a8a1eeef18ca96794fa1056043142d13bc78
```

The premise that no commit occurred after `276635f` is false. Fresh `git log --oneline -20` showed these later commits, in order:

```text
4ac0fba feat(catalog): add dark race-core support registry
28eab62 test(catalog): characterize real NotEvaluated goal-pace fallback behavior end-to-end
1e6de40 docs(governance): correct and clarify TD-NOTEVALUATED-FALLBACK-001 scope using real E2E evidence
80f742e docs(product): UX decision audit for UserDefined goal-time hard-block
3549a8a docs(catalog): clarify GoalPaceReachabilityVerifier measures theoretical completeness, not runtime safety
```

Therefore `276635f` is not the current checkpoint base and the work under review is split across committed post-base history and the working tree. This is a material provenance discrepancy.

`git diff --check` exited 0; it reported only LF-to-CRLF warnings on existing dirty text files, with no whitespace or conflict-marker error.

### Working-tree attribution

Fresh `git status --short` and `git diff --name-status` were captured. Source/doc changes map as follows:

| Item/origin | Files |
| --- | --- |
| Phase 4G.4C.0 DI work | modified `backend/RunningApp.IntegrationTests/DependencyInjectionResolutionTests.cs`; `BACKEND_DI_BASELINE_RESOLUTION_CATALOG_LIVE_PILOT.md`; `BACKEND_TEST_BASELINE_STABILIZATION_RESET_AND_DI_FIX.md` |
| Cross-phase audit | `CROSS_PHASE_4G_4A_TO_4G_4B_V_AND_BACKEND_BASELINE_INDEPENDENT_AUDIT.md` |
| Phase 4G.4B/4G.4B.1 | `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md`, `PHASE4G_4B_PREPARATION_RUNWAY_TYPED_CONTRACTS.md`, `PHASE4G_4B_V_PREPARATION_RUNWAY_CURRENT_STATE_VALIDATION.md`, untracked PreparationRunway production/test folders |
| TD closures / Phase 4G.3B.7–3B.9 | activation-readiness JSON/Markdown; volume policy/governance/verifier; 3B.7 and 3B.8 decision-audit documents |
| Existing audit drift not cleanly attributable to the five review items | modified ten-k pilot audit JSON/Markdown |
| Generated | tracked/untracked `backend/**/bin/**` and `backend/**/obj/**`, Debug and Release |
| Local acceptance/output | `.claude/`, `LOCAL_CATALOG_ACCEPTANCE_TEST.md`, response/calendar JSON, `baseline_tmp/`, `docker-compose.yml` |
| Unrelated design assets | modified/untracked `design-references/*.png` |

The generated/local/design files and ten-k audit drift do not map cleanly to the five claimed items and must not be silently included in a checkpoint.

## 2. Risk file integrity results

The JSON was parsed directly with PowerShell `ConvertFrom-Json`, not through a test:

```text
TOTAL=16
OPEN=9
CLOSED=7
DUPLICATE IDS=0
```

The nine OPEN IDs are exactly `TD-D3-001`, `TD-WAVE5-001`, `TD-BACKEND-001`, `TD-REGISTRY-001`, `TD-PACESOURCE-001`, `TD-PACESOURCE-002`, `TD-CORE-READINESS-001`, `TD-TESTFLAKE-001`, and `TD-NOTEVALUATED-FALLBACK-001`. Their order and status are preserved by the working-tree diff. No source evidence of an edit to their statement text was found.

Direct extraction produced the same 16 unique TD IDs from JSON and Markdown. The focused integrity tests independently passed:

```text
ActivationReadinessRiskParityTests + ActivationSafetyGateTests
Passed: 15, Failed: 0, Skipped: 0, Total: 15
```

This includes `ActivationReadinessRisksFile_IsNotMechanicallyConsumedByAnySourceFile`; repository search also found only prose/test guard references, not a production reader.

For `TD-VOLUME-CAP-UNENFORCED-001`, `TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001`, `TD-ALLOCATION-PRIORITY-001`, `TD-FOUNDATION-COMPRESSION-001`, and `TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001`, the original `statement` values remain present. Closures were appended as `closureNote`/`closureCriteriaSatisfied` and status changes. The allocation/volume records also amend `currentRuntimeImpact`; they do not replace the original statement, but this is more than a strictly resolution-note-only append and should be recognized as such.

## 3. Four independent re-verifications

### C1 — TD-VOLUME-CAP-UNENFORCED-001

`CatalogVolumeAndLongRunPlanner.ResolvePeak` currently computes:

```text
C = GoldenFixtureResolvedPeakKm / GoldenFixtureStartingVolumeKm
M = 1 + ((C - 1) * transitions / GoldenFixtureNonTaperTransitions)
reachable = startingVolumeKm * M
```

For the current constants `38`, `24`, and `10`, linear interpolation over `transitions` requires a per-transition increment relative to the starting volume of:

```text
(reachable - start) / (start * transitions)
= (start * (1 + ((38/24 - 1) * transitions/10)) - start)
  / (start * transitions)
= (38/24 - 1) / 10
= 7/120
= 0.0583333333...
```

Both `start` and `transitions` cancel. This independently confirms the ratio claim for positive transition counts. At 24 km the corresponding increment is `24 * 7/120 = 1.4 km`, below `0.08 * 24 = 1.92 km` and the 2.5 km absolute value.

New check: substituted a different start (30 km) and transition count (6): multiplier `1 + (7/12 * 6/10) = 1.35`, reachable 40.5; `(40.5-30)/(30*6) = 7/120`, again identical.

Source search found no use of either hard field in `CatalogVolumeAndLongRunPlanner` except threading into `ReachablePeakDecision`; planner interpolation does not clamp or reject against them. However, repository-wide search found `VolumeProgressionVerifier` lines 116–123 actively reading both fields and producing `WEEKLY_INCREASE_VIOLATION`/Fail findings. Thus “unread by the real planner clamp/reject path” is true, while “unread by **any** clamp/reject logic” is false after the dark verifier was added.

Verdict: `CONFIRMED_WITH_MINOR_DISCREPANCY` — algebra and planner non-enforcement are confirmed; the prompt's repository-wide “any reject logic” wording is contradicted by the dark verifier.

### C2 — TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001

Fresh source trace:

1. `PreparationRunwayPlanningResultValidator.Validate` unconditionally calls `RacePlanCompositionMetadataValidator.Validate` for every status.
2. That validator constructs a `PreparationRunwayContext` and unconditionally calls `PreparationRunwayContextValidator.Validate`.
3. The context validator rejects `PreparationRunwayPlusCore` with zero runway and rejects every other composition with positive runway.

New adversarial check, distinct from existing literal tests: an `InvalidInput` result with `CompositionType=CompressedCore`, `RunwayDays=2`, `FullRunwayWeeks=0`, `LeadingPartialDays=2`, and a valid `InvalidDateRange` finding enters the unconditional metadata path and receives `InvalidDerivedDuration` because a positive runway requires `PreparationRunwayPlusCore`. Its status-valid finding cannot erase that metadata finding, so `IsValid=false` deterministically follows. This covers a fourth composition value not used by the existing StandaloneCore mismatch examples.

The focused runway/readiness/allocation batch passed 72/72, including the executable status matrix.

Verdict: `INDEPENDENTLY_CONFIRMED`.

### C3 — TD-ALLOCATION-PRIORITY-001

Direct `ten-k-master.v6.json` values:

| Phase | compressionPriority | extensionPriority |
| --- | ---: | ---: |
| FOUNDATION | 1 | 1 |
| BUILD | 2 | 2 |
| RACE_SPECIFIC | 3 | 3 |
| TAPER | 4 | 4 |

The catalog file is not dirty, so these values were not changed by the closure. Fresh verifier tests passed. Current executable outcomes are:

- target 13: `DecisionRequired` (priority-dependent, verifier design unchanged);
- target 14: `Pass` (maximum-exhausted/order-independent boundary).

New independent bound check: at 13, RACE_SPECIFIC and TAPER have no extension headroom from preferred, leaving one extra week contested by FOUNDATION versus BUILD; at 14 both reach their maximums, so final allocation is order-independent. This derives the outcome without relying on the closure report.

Verdict: `INDEPENDENTLY_CONFIRMED`.

### C4 — TD-FOUNDATION-COMPRESSION-001

Fresh `ReadinessEligibilityVerifierTests` passed as part of the 72-test focused batch. The real allocator/test pins Foundation allocations:

```text
8:2, 9:2, 10:2, 11:2, 12:3, 13:4, 14:4
```

Direct catalog minimums are:

```text
FOUNDATION=2, BUILD=3, RACE_SPECIFIC=2, TAPER=1
```

New check: minimum sum is `2+3+2+1=8`; the allocator's compression predicate only adjusts a phase while allocated weeks exceed its minimum. Therefore no feasible 8–14 allocation can require Foundation below 2, independently of readiness vocabulary.

Prominent discrepancy: a fresh production-project grep for `CoreEntryReadiness|CORE_ENTRY_READINESS` found **17 files**, not the closure note's 16. The additional file is `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunway/PreparationRunwayContracts.cs`, whose neutral `PreparationNeedProfile.CoreEntryReadiness` property was added later. It is dark and does not influence race-core phase allocation. The other results remain resolver/trace/DI/non-dependency/downstream-stage categories; `CatalogPhaseAllocationResolver.Resolve` has no readiness input.

No numeric readiness threshold appears in the closure. Its numeric values are catalog minimums and observed allocations, not invented readiness cutoffs.

Verdict: `CONFIRMED_WITH_MINOR_DISCREPANCY` — reachability proof holds, but the claimed fresh grep count is stale by one due to later runway contracts.

## 4. DI baseline fix validation

Current tests are:

- `CatalogLivePilotOptions_TypeDefault_IsDisabled`: bare type asserts false.
- `RealHost_CatalogLivePilotOptions_DevelopmentEffectiveValue_IsEnabled`: real Development host asserts true.
- `RealHost_CatalogTargetServices_ResolveFromOneScope`: resolves only the five catalog target services. Phase 4G.3B.10A removed the misleading count-bearing name; Phase 4G.3B.10B removed the redundant options resolution already covered by the two dedicated options tests. The historical `NOT_READY_ISSUES_FOUND` verdict below remains unchanged.
- `RealHost_ServiceResolution_OpensNoDatabaseConnection`: retained with `[Fact(Skip=...)]`.

Focused result after a real Release build:

```text
DependencyInjectionResolutionTests: Passed 8, Failed 0, Skipped 1, Total 9
```

Explicit skip-only run:

```text
[SKIP] RealHost_ServiceResolution_OpensNoDatabaseConnection
Passed 0, Failed 0, Skipped 1, Total 1
```

The skip is genuine. Its reason cites `CROSS_PHASE_4G_4A_TO_4G_4B_V_AND_BACKEND_BASELINE_INDEPENDENT_AUDIT.md Part F Option A/C` and explicitly says no dedicated TD exists; it correctly does **not** misattribute the gap to the unrelated runway TD.

Two consecutive fresh full runs:

| Run | Passed | Failed | Skipped | Total | Duration |
| --- | ---: | ---: | ---: | ---: | --- |
| 1 | 1,394 | 0 | 1 | 1,395 | 2m44s |
| 2 | 1,394 | 0 | 1 | 1,395 | 2m41s |

The totals are identical; no reset HTTP 500 recurred.

## 5. Cross-closure interaction check

- Runway exhaustiveness changes are confined to the PreparationRunway contracts/validators/tests. Foundation compression depends on `CatalogPhaseAllocationResolver`, `ReadinessEligibilityVerifier`, and template phase constraints. No shared executable dependency was found.
- Allocation priority closure changed governance only; `AllocationOrderCorrectnessVerifier.cs` is not dirty. Its 13/14 results remain DecisionRequired/Pass. `ReadinessEligibilityVerifier` does not call it, so Foundation reachability is unaffected.
- Baseline total was 1,387. Current total is 1,395: net `+8`. DI changed a seven-test class into nine discovered tests (`+2`, including one skipped); PreparationRunway grew from 41 to 45 tests (`+4`); the Foundation allocation-pin/readiness additions account for `+2`. No removed test was found. `1,387 + 2 + 4 + 2 = 1,395`.

## 6. Scope discipline and live checks

Repository/source searches found no PreparationRunway call site outside its dedicated internal folder/tests, and no new live call to `AllocationOrderCorrectnessVerifier` or `ReadinessEligibilityVerifier`. The generic allocator/verifiers remain dark.

Fresh HTTP regression batch:

```text
Sw12LongHorizonFailClosedEndToEndTests + Sw13ExactTwelveWeekOnlyEndToEndTests
Passed: 21, Failed: 0, Skipped: 0, Total: 21
```

It confirms:

- 8 weeks → HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`;
- 12 weeks → HTTP 200 with the established 12-week/48-session catalog preview assertions;
- 20 weeks → HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`.

No endpoint/DTO/horizon behavior change was found. The runway remains dark. No closure text invents a readiness threshold.

Scope discrepancy: the working tree contains generated outputs, local Docker/response/baseline/design content, earlier phase documents, and audit drift outside the five-item checkpoint scope. These may be pre-existing, but they must be excluded or separately attributed before checkpointing.

## 7. Overall verdict

`NOT_READY_ISSUES_FOUND`

The functional/test evidence is strong and repeatable, but checkpoint integrity is not ready because:

1. The claimed base is wrong: five commits exist after `276635f`; the intended checkpoint range must be redefined against actual HEAD `3549a8a`.
2. C4 governance evidence originally said 16 production files while current state has 17. Phase 4G.3B.10A appended a historical/current-scan clarification without changing the closure.
3. C1 now consistently distinguishes “not enforced by the **real planner**” from the dark `VolumeProgressionVerifier` reading the fields and reporting findings; Phase 4G.3B.10A appended this clarification without changing behavior.
4. Out-of-scope generated/local/design/audit files must be explicitly excluded or assigned to a separately reviewed commit.

No fix was attempted in this validation pass.

## 8. Files changed and history operations

Created by this pass only:

```text
PHASE_4G_3B_10_PRE_CHECKPOINT_INDEPENDENT_VALIDATION.md
```

No code, test, TD, catalog, governance, frontend, or existing documentation file was edited or deleted. Test execution updated generated `bin`/`obj` outputs only. No commit, amend, rebase, reset, branch operation, or push occurred.
