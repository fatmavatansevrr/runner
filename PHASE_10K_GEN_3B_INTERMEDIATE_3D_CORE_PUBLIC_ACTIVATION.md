# PHASE 10K-GEN.3B — Intermediate 3D Core Public Activation

## 1. GEN.3A binding status

GEN.3A/3A.1/3A.2 is treated as frozen. The single dynamic 10K Core skeleton remains authoritative; `RUN_LAYOUT_3D` owns frequency structure and the combination remains a version/compatibility manifest. No schema or EF migration was added.

## 2. Pre-activation boundary table

| File/type | Previous 3D behavior | Responsibility | Change | Authority | Reason |
|---|---|---|---|---|---|
| `run-layout-3d.v1.json` | DRAFT | Three-role structure | VALIDATED | Domain/publication | Make exact frequency authority publishable |
| `ten-k-3d-intermediate.v1.json` | DRAFT | Compatibility manifest | VALIDATED | Compatibility/publication | Add the approved matrix cell to Process A release |
| Exact candidate dependency closure | DRAFT | Version-pinned inputs | VALIDATED | Publication | Canonical publisher rejects a root whose closure is excluded |
| `V1CatalogPilotIdentityPolicy` | 4D identity only | Candidate selection | Added exact 3D identity | Rollout | Avoid nearest-match and duplicated predicates |
| live preview routing | 4D-only invariant/load | Public admission | Resolves 3D or 4D identity | Rollout | Admit only the requested compatible candidate |
| catalog generator | Fixed 4D candidate | Generation | Frequency-selected candidate | Public boundary | Generate from the admitted identity |
| runway/long-horizon authorities | Explicit 4D | Other horizon families | Unchanged | Rollout | Preserve 3D containment |
| request/persistence/read APIs | Cardinality-generic | Validation/storage/read | No production change | Validation | Already represent exactly three days |

## 3. Catalog publication changes

`RUN_LAYOUT_3D v1`, `TEN_K__3D__INTERMEDIATE v1`, and only their exact referenced dependency closure were moved to `VALIDATED`. Process A published immutable Pilot release `0.7.3-pilot`; `verify-release` passed. Its manifest contains both `TEN_K__3D__INTERMEDIATE v1` and the existing `TEN_K__4D__INTERMEDIATE v4` bundles. Source files were not stamped `PUBLISHED` manually.

The newly validated workout closure changes key-based derived historical 4D bundle rebuild hashes. The cross-release exception ledger records that derived dependency cascade; source artifact identities were not mutated.

## 4. Support versus rollout authority

Compatibility remains catalog-owned. Frequency structure remains RunLayout-owned. Public selection is centralized in `V1CatalogPilotIdentityPolicy.ResolveCandidate(daysPerWeek)`. Runway and LongHorizon have independent 4D gates and were not widened.

## 5. Public request validation

The existing DTO/validator accepts three distinct preferred weekdays and requires `LongRunDay` membership. Count 2, count 4, duplicates, and a non-member long-run day all return HTTP 400. No 3D DTO was introduced.

## 6. Public preview horizon matrix

| Core weeks | Eligible readiness | Result | Sessions/week |
|---:|---|---|---:|
| 8 | pass | HTTP 200 | 3 |
| 9 | pass | HTTP 200 | 3 |
| 10 | pass | HTTP 200 | 3 |
| 11 | pass | HTTP 200 | 3 |
| 12 | pass | HTTP 200 | 3 |
| 13 | pass | HTTP 200 | 3 |
| 14 | pass | HTTP 200 | 3 |

Every generated week contains exactly KEY_SESSION, EASY_SUPPORT, and LONG_RUN; no fourth session is materialized.

## 7. Public eligibility matrix

Explicit-zero readiness at 8, 9, 10, and 11 weeks returns HTTP 422 with `THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`. It is now a stable `PlanProductIneligibleException`, not a generic generation 500. Eligible 12–14 requests continue past this gate.

## 8. Runway and LongHorizon containment

3D at 15, 20, 21, and 52 weeks does not return success or 500. Classification remains: `CORE_3D_ACTIVE`, `RUNWAY_3D_NOT_ACTIVATED`, `LONG_HORIZON_3D_NOT_ACTIVATED`.

## 9–12. Confirm, persistence, database cardinality, and read surfaces

The real reset → preview → confirm path persisted a 12-week plan atomically with `DaysPerWeek = 3`, candidate `TEN_K__3D__INTERMEDIATE`, 12 weeks, and exactly 36 TrainingDays: 12 KEY, 12 EASY, and 12 LONG. Dates, distances, workout identity, stage/prescription provenance, and candidate provenance survived.

The active Home endpoint returned the confirmed plan. Calendar returned the persisted three-day schedule with no ghost day. Training-day detail was read for one KEY, EASY, and LONG day and matched the persisted identifiers and prescriptions.

## 13–15. Completion, NotToday, and full API E2E

A persisted 3D training day completed through the real completion endpoint and remained `Completed` in storage. The monolithic suite covers existing NotToday/adaptation regressions without adding 3D adaptation scope. Full reset → preview → confirm → DB → home/calendar/detail → complete passed.

## 16. 4D public regression

The full backend suite passed. Existing 4D candidate routing and its special 8-week explicit-zero classification remain intact; only 3D bypasses that legacy early classification to reach its typed taper authority.

## 17. Unsupported-combination containment

Beginner/3D, Advanced/3D, and Intermediate/5D do not generate a public preview. No nearest-match fallback was introduced.

## 18. Release-package validation

The deployable API Release build packages the complete 73-file runtime JSON inventory. The count is defined once in the deployment packaging tests and used for source and package assertions. Package smoke and real HTTP smoke passed. Process A release `0.7.3-pilot` independently passed checksum verification.

## 19. Error-contract validation

Expected validation/product/unsupported outcomes do not surface `500 INTERNAL_ERROR`. The new typed product-ineligibility mapping is HTTP 422 and exposes the frozen reason code.

## 20. Test results

| Check | Discovered | Passed | Failed | Skipped | Duration |
|---|---:|---:|---:|---:|---:|
| Application build | — | PASS | 0 | — | 1.15s |
| GEN.3A + GEN.3B focused | 70 | 70 | 0 | 0 | 57s |
| GEN.3B + deployment/package focused | 30 | 30 | 0 | 0 | 59s |
| Plan-catalog full | 1,250 | 1,250 | 0 | 0 | 4s |
| Backend monolithic | 3,423 | 3,423 | 0 | 0 | 18m47s |

The monolithic result subsumes shared 3D/4D, public API, completion/NotToday, unsupported-combination, runway, LongHorizon, and catalog-runtime regressions.

## 21. Files changed by GEN.3B

- `CATALOG_PUBLICATION`: 3D layout/combination and exact dependency-closure lifecycle metadata; cross-release derived-bundle ledger.
- `ROLLOUT_ACTIVATION`: `V1CatalogPilotIdentityPolicy`, live route decision/load.
- `PUBLIC_BOUNDARY_GENERALIZATION`: catalog preview candidate selection.
- `GEN3B_BUG_FIX`: typed public product-ineligibility exception and API mapping.
- `TEST_ONLY`: GEN.3B E2E, package inventory, and publication-resolution expectations.
- `DOCUMENTATION`: this report.

## 22. Git diff check

`git diff --check`: PASS.

## 23–25. Final state, deferred scope, classification

Public rollout state: TEN_K / INTERMEDIATE / 3D / Core 8–14 is enabled alongside existing 4D.

Deferred and still disabled: 3D Preparation Runway, 3D LongHorizon, 3D adaptation generalization, Beginner/Advanced/Expert 3D, 2D, and 5D+.

Final classification:

`10K_GEN_3B_INTERMEDIATE_3D_CORE_PUBLICLY_ACTIVE`
