# PHASE 10K-GEN.3A.2 — Intermediate 3D Core Final Verification

Date: 2026-08-12  
Rollout state: **PUBLIC_ROLLOUT_DISABLED**

## 1. Starting state and remaining gaps

Chronology is preserved: GEN.3A was BLOCKED; GEN.3A.1 remained BLOCKED while
substantially improving verification. GEN.3A.2 addressed only the remaining allocation,
final-prescription, structural persistence, public containment, cardinality and backend-suite gaps.
No frozen GEN.2 value was changed.

## 2. Backend-suite completion diagnosis

The prior condition was not a hang. With `xunit.runner.json` copied to testhost output,
the suite runs serially and makes continuous progress. Recorded checkpoints included:

| Approximate elapsed | Executed | Passed |
|---:|---:|---:|
| initial | 239 | 239 |
| 2.5 min | 1,219 | 1,219 |
| 6 min | 1,317 | 1,317 |
| 8.5 min | 1,371 | 1,371 |
| 11 min | 1,426 | 1,426 |
| 13.5 min | 1,518 | 1,518 |
| 16 min | 1,658 | 1,658 |

Classification: `FULL_SUITE_LONG_RUNNING_BUT_HEALTHY`.

Three monolithic invocations completed. The first two exposed only test inventory
expectations: the source runtime catalog contains 73 JSON files after adding the two
DRAFT 3D artifacts, while the public release package intentionally remains 71 because
those DRAFT artifacts are excluded. The corrected distinction passed its 7-test class.

Final monolithic result:

- discovered/executed: 3,400
- passed: 3,400
- failed: 0
- skipped: 0
- duration: 17 min 45 s
- exit code: 0
- testhost: exited cleanly; no child remained
- blame-hang: all tests completed; no sequence/dump emitted
- status: `BACKEND_FULL_SUITE_PASS`

Partition accounting was not required.

## 3. Allocation negative matrix

The positive 12–32 km matrix remains exact. Committed negative/boundary coverage proves:

- weekly volume below 12 fails closed;
- a resolved LONG below 5 km fails closed;
- a post-round LONG above 42% fails closed;
- non-0.5 km resolved LONG fails closed;
- non-reconcilable totals fail closed;
- reconciliation never reduces KEY below 4 or EASY below 3;
- equal-error resolution uses stable KEY/EASY structural order;
- repeated failures and successful allocations are deterministic.

The 3D allocator now preserves the already-resolved readiness-compatible long-run value
instead of silently recalculating 40%; reconciliation adjusts KEY/EASY only.

## 4. Final prescribed-plan E2E and taper

The real `TEN_K__3D__INTERMEDIATE` v1 candidate reaches the complete internal chain:
layout, phase allocation, skeleton, stage allocation, workout binding, calendar, dated
validation, volume, long run, session allocation, pace/intensity, taper sharpen and final
validator.

Successful cases: 12-week recent volume, 12-week missing-volume fallback 16, 12-week
explicit zero 12, and 14-week representative input. The 8-week explicit-zero case throws
typed `ThreeDayCoreProductIneligibleException` before final output.

Every successful week has exactly 1 KEY / 1 EASY / 1 LONG, valid identity/date/prescription,
and exact weekly reconciliation. Eligible taper weeks retain all roles, remain at least
12 km, preserve 4/3/5 minima, retain `TAPER_SHARPEN` controlled-sharpening segments and
pass final validation.

## 5. Persistence structural compatibility

The existing production `GeneratedCatalogPlanPayload` mapping/validation seam accepts
the real final 3D plan without schema changes. Every mapped week has exactly three
TrainingDay-equivalent records, ordered 1–3, with `KEY_SESSION`, `EASY_SUPPORT`, and
`LONG_RUN`; workout/stage provenance survives. No `EASY_SUPPORT_2`, four-day assertion,
migration or public confirmation bypass is required.

## 6. Public rollout-negative proof

Internal catalog loading accepts the DRAFT 3D candidate, while release packaging remains
71 files and excludes the two DRAFT 3D additions from the public package. Source inventory
is 73. Existing public/confirmation tests are green, and no controller, route or rollout
gate was widened. Public 3D persistence remains unreachable.

## 7. Bound-cardinality and runner configuration

Committed real-candidate regressions prove:

- 3D 1/1/1 passes; missing or extra EASY fails;
- 4D 1/2/1 passes; one EASY or extra KEY fails.

Expected counts come from the dated source skeleton, never a global frequency constant.

`backend/RunningApp.IntegrationTests/bin/Debug/net9.0/xunit.runner.json` exists after
build and contains `parallelizeTestCollections=false`: `COPY_CONFIGURATION_CONFIRMED`.

## 8. Regression results

- Application build: PASS, 0 warnings, 0 errors, 2.17 s.
- GEN.3A/3A.1/3A.2 named focused suite: 47/47 PASS, 0 skipped; repeat 47/47 PASS.
- Broad shared 3D/4D validator/calendar/volume/progression/allocation/public/confirmation/golden suite: 138/138 PASS, 1 min 2 s.
- Plan-catalog full suite: 1,250/1,250 PASS, 0 skipped, 3 s.
- Backend monolithic: 3,400/3,400 PASS, 0 skipped, 17 min 45 s.
- Repeatability: focused repeated cleanly; multiple full runs completed and the final corrected run passed.

No 4D output delta or `4D_BEHAVIORAL_EQUIVALENCE_RISK` was observed.

## 9. GEN.3A.2 file-scope audit

| File | Classification |
|---|---|
| `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1ThreeDaySessionVolumeAllocationPolicy.cs` | GEN3A_BUG_FIX |
| `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/CatalogFinalPrescribedPlanValidator.cs` | GEN3A_BUG_FIX |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/DynamicCoreSessionPrescriptionOrchestratorTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/Gen3A2ThreeDayFinalPrescriptionTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/Gen3AThreeDayAllocationMatrixTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Binding/Gen3A2BoundCardinalityTests.cs` | TEST_ONLY |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogDeploymentPackagingTests.cs` | TEST_ONLY |
| `PHASE_10K_GEN_3A_2_INTERMEDIATE_3D_CORE_FINAL_VERIFICATION.md` | DOCUMENTATION |

Scoped `git diff --check` passed. Git emitted only an LF→CRLF informational warning,
not a whitespace error. Extensive unrelated pre-existing worktree changes were untouched.

## 10. Final result

Remaining blockers: **none for the authorized TEN_K / INTERMEDIATE / 3D / CORE_PATH scope**.

Rollout remains disabled; IMPLEMENTED_AND_GATED does not mean publicly activated.

**Final classification:**

`10K_GEN_3A_INTERMEDIATE_3D_CORE_IMPLEMENTED_AND_GATED`
