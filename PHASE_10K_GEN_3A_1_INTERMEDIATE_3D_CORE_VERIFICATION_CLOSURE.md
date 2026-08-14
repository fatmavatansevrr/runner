# PHASE 10K-GEN.3A.1 — Intermediate 3D Core Verification Closure

Date: 2026-08-12  
Scope: verification and narrowly proven GEN.3A corrections only  
Rollout: **disabled**

## 1. Starting-state reproduction

- Application build: PASS, 0 warnings, 0 errors.
- Existing GEN.3A focused result inherited from GEN.3A: 251/251 PASS.
- Plan-catalog: 1,250/1,250 PASS.
- Backend monolithic suite reproduced the incomplete/hanging behavior.
- `--list-tests` initially enumerated 3,352 cases before the GEN.3A.1 additions.

## 2. Full-suite hang diagnosis

The diagnostic run reached 2,940 executed tests (2,939 pass, 1 fail) and reported
`LongHorizonPublicPreviewConfirmationTests.FiftyThreeWeeks_UsesExactSupportedWindowError`
as active when the outer timeout fired. That test passed alone in 6 seconds. The sole
recorded failure, `LongHorizonRestartContinuationTests.RestartAfterInitialGeContinuesToNextWindow`
(expected 71, actual 73), also passed alone in 5 seconds.

The testhost diagnostic showed many database/fixture tests active concurrently even
though `xunit.runner.json` declares `parallelizeTestCollections=false`. The file was not
copied to the test output directory, so the running testhost could not consume it.
Additionally, a short outer command timeout left a child testhost alive; a subsequent
diagnostic run was therefore contaminated by two concurrent suites. Those processes
were terminated diagnostically and were not adopted as a permanent workaround.

After copying the runner configuration, a clean monolithic run no longer produced a
single test exceeding the 120-second blame-hang threshold, but it did not finish inside
the 15-minute command boundary. Therefore no monolithic full-suite PASS is claimed.

## 3. Hang classification

`TEST_ISOLATION_OR_RESOURCE_LEAK` for the originally observed active-test/failure
symptoms: the intended collection-serialization configuration was absent from testhost
output, and both implicated tests passed individually. The remaining 15-minute result
is a runner/runtime-bound incomplete result, not evidence of a single-test infinite loop.

## 4. Fixes made

- TEST_INFRA_FIX: copy `xunit.runner.json` to test output so collection serialization is effective.
- GEN3A_BUG_FIX: `BoundCatalogPlanValidator` now compares bound role cardinality with
  the resolved dated skeleton instead of universally requiring 1 KEY / 2 EASY / 1 LONG.
  The real 3D candidate proved the former invariant wrong; 4D continues to require its
  existing four-role shape through its own source skeleton.

## 5. Eligibility and numeric projection matrix

Real `TEN_K__3D__INTERMEDIATE` v1 candidate and the real skeleton → binding → volume path:

| Start | Weeks | Pre-taper | Taper | Result |
|---|---:|---:|---:|---|
| explicit zero (12) | 8 | 17.5 | 9.5 | typed PRODUCT_INELIGIBLE |
| explicit zero (12) | 9 | 18.5 | 10.0 | typed PRODUCT_INELIGIBLE |
| explicit zero (12) | 10 | 19.5 | 10.5 | typed PRODUCT_INELIGIBLE |
| explicit zero (12) | 11 | 21.0 | 11.0 | typed PRODUCT_INELIGIBLE |
| explicit zero (12) | 12 | 22.5 | 12.0 | taper gate PASS |
| explicit zero (12) | 13 | 24.0 | 12.5 | taper gate PASS |
| explicit zero (12) | 14 | 25.5 | 13.5 | taper gate PASS |
| missing (16) | 8 | 23.5 | 12.5 | taper gate PASS |
| missing (16) | 10 | 27.0 | 14.5 | taper gate PASS |

All nine cases passed. Failure code is exactly
`THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`. The 22 km peak-band lower
bound was not forced.

## 6. Allocation matrix

The committed 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 and 32 km matrix matches the
approved GEN.2B.2 KEY/EASY/LONG values exactly. Each row proves exact sum, 4/3/5 km
minima, 42% long-run cap, 0.5 km granularity, and deterministic repeat equality.
Below-floor and non-reconcilable inputs fail closed. Allocation-focused result: 14/14 PASS.

## 7. Progression verifier and 4D regression

The focused closure command included existing `VolumeProgressionVerifierTests` and
`CatalogVolumeAndLongRunPlannerTests`, covering the shared ratio/absolute enforcement
and existing 4D planner path. Combined focused result: 75/75 PASS. No 4D fixture was
changed and no `4D_BEHAVIORAL_EQUIVALENCE_RISK` was observed.

## 8. Calendar matrix

Committed 3D cases cover KEY before/after LONG, adjacency variants, deterministic
selection across weeks, distinct three-date cardinality, long-run preference, same-week
and cross-week separation, plus an unsafe Mon/Sat/Sun configuration that fails closed.
The authority continues to derive cardinality from `DaysPerWeek`; no universal literal
3D/4D authority was added.

## 9. Real-candidate internal proof

The eligibility tests exercise the real catalog candidate through combination/layout,
dynamic skeleton, progression, real workout binding, calendar and dated/bound validation,
then real weekly-volume and long-run planning for 8–14 representative cases. The validator
defect found by this proof was corrected. A separate committed all-the-way final prescribed
plan/persistence E2E matrix was not completed in this follow-up.

## 10. Public and persistence compatibility

`LongHorizonPublicPreviewConfirmationTests` was included in the 75/75 focused PASS.
Public 3D rollout policy was not modified. No schema or migration was added. The generic
role/cardinality correction is structural only and remains checked against the resolved
skeleton; it does not make 3D confirm publicly reachable.

## 11. Final verification results

- Application build: PASS; 0 warnings; 0 errors; 2.65 s.
- GEN.3A named focused suite: 36/36 PASS; repeat run PASS; 0 skipped.
- Broader volume/progression/public focused suite: 75/75 PASS; 58 s.
- Plan-catalog full suite: 1,250/1,250 PASS; 0 failed; 0 skipped; 4 s.
- Backend monolithic suite: INCOMPLETE; clean serialized attempt exceeded 15 minutes;
  no test crossed the 120-second blame-hang threshold; no PASS claimed.
- Repeatability: focused GEN.3A tests repeated cleanly; monolithic repeatability not established.

## 12. GEN.3A.1 file-scope audit

| File | Classification |
|---|---|
| `backend/RunningApp.Application/RuntimeCatalog/Schedule/Binding/BoundCatalogPlanValidator.cs` | GEN3A_BUG_FIX |
| `backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj` | TEST_INFRA_FIX |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/Gen3AThreeDayAllocationMatrixTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/DynamicCoreVolumeAndLongRunOrchestratorTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/Gen3AThreeDayEligibilityMatrixTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Gen3AThreeDayCoreTests.cs` | TEST_ONLY |
| `PHASE_10K_GEN_3A_1_INTERMEDIATE_3D_CORE_VERIFICATION_CLOSURE.md` | DOCUMENTATION |

The worktree contained extensive unrelated pre-existing modifications; none were normalized
or reverted. Scoped `git diff --check` passed; Git emitted only existing LF→CRLF notices,
not whitespace errors.

## 13. Remaining blockers and final classification

Remaining closure gaps are: one completed monolithic backend invocation, complete
partition accounting if that invocation cannot be obtained, the full requested allocation
negative-candidate matrix, and a committed final-prescribed-plan/persistence E2E matrix.

Historical GEN.3A BLOCKED status is therefore **not superseded**.

**Final classification:**
`10K_GEN_3A_INTERMEDIATE_3D_CORE_IMPLEMENTATION_BLOCKED`

This classification reflects verification incompleteness, not a reopened product decision.
All GEN.2A/2B values remain frozen, and rollout remains disabled.
