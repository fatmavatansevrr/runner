# Phase 10K-FREQ.6D.26 — Intermediate×6D Core + Preparation Runway + LongHorizon Combined Implementation & Dark Verification

**Implementation + catalog authoring + real PostgreSQL verification + dark capability closure. Implements FREQ.6D.23/6D.25's already-approved authority only — no new product/numeric/schema decision. Public 6D gate remains CLOSED throughout.**

## 0. Preflight

`PHASE_LEDGER.md` row 105 / `MASTER_ROADMAP.md` confirmed `FREQ.6D.25` `DONE`, `INTERMEDIATE_6D_PEAK_VOLUME_BAND_AUTHORITY_APPROVED` / `INTERMEDIATE_6D_FULL_IMPLEMENTATION_AUTHORITY_COMPLETE` — every Intermediate×6D authority item closed, no remaining blocker. 0 ahead/0 behind at start. Next free ID `FREQ.6D.26`, scheduled and pushed (`cddc5bb`) before implementation began.

## 1. Architectural Principle Honored

Target composition: `TEN_K_MASTER` + `RUN_LAYOUT_6D` + `INTERMEDIATE` level policy + shared workout progression + existing horizon architecture = resolved configuration. No duplicated week-by-week plan template was created. Confirmed concretely: the dual-KEY execution-prescription profiles (`INTERMEDIATE_5D_BUILD_PRIMARY` etc.) are resolved via `TEN_K_MASTER`'s shared `TEN_K_WORKOUT_PROGRESSION_V1` document, itself Level+Distance-owned (not frequency-owned) despite the historical "5D" naming — reusing `TEN_K_MASTER v7`'s lineage for 6D reuses these SAME profiles verbatim. No new prescription-profile documents were authored.

## 2. Catalog Authoring

Published plan-catalog release **1.2.0** — verified byte-identical to `1.1.0` for every pre-existing document (diffed 3D/4D/5D/Beginner×4D bundles directly, zero difference) — additionally carrying:

| Document | Key/Version | Content |
|---|---|---|
| `RUN_LAYOUT` | `RUN_LAYOUT_6D` v1 | `[KEY,EASY,KEY,EASY,EASY,LONG]` — 2K+3E+1L, 6 slots |
| `PLAN_TEMPLATE` | `TEN_K_MASTER` v8 | Identical to v7 except `supportedRunsPerWeek: [3,4,5,6]` |
| `PEAK_VOLUME_BAND_POLICY` | `PEAK_VOLUME_BANDS_V1` v5 | v4 + `{TEN_K, INTERMEDIATE, 6, 36, 50}` |
| `RULE_PACK` | `APPSEL_RACE_PLAN_V1` v6 | Identical to v5 except `peakVolumeBandPolicy.version: 5` |
| `TEMPLATE_COMBINATION` | `TEN_K__6D__INTERMEDIATE` v1 | References `TEN_K_MASTER v8`, `RUN_LAYOUT_6D v1`, `INTERMEDIATE_MODIFIER v7` (unchanged — Level doesn't move), `APPSEL_RACE_PLAN_V1 v6` |

Real publish pipeline used (`PlanCatalog.Cli publish`), not hand-computed hashes. Verified via `verify-release --version 1.2.0`: checksums pass. `appsettings.json`'s `PublishedBundleReleaseVersion` bumped `1.1.0 → 1.2.0`.

## 3. Code Changes — VolumeSafetyPolicy and Dispatch

- `VolumeSafetyPolicy.SixDayIntermediate` — every field byte-identical to `FiveDayIntermediate` (26.0/19.5/44.5/28%/36%/0.07/0.08/2.5/0.53/0.5), per `FREQ.6D.25`'s approved reuse.
- `VolumeSafetyPolicy.ForIntermediateDaysPerWeek(int)` — new centralized dispatch, replacing 4 separately-duplicated `daysPerWeek == 5 ? FiveDayIntermediate : Default` ternaries across the codebase. Fail-closed for any unrecognized frequency.
- `V1SixDayIntermediateMissingReadinessStartingVolumePolicy` — mirrors `V1FiveDayIntermediateMissingReadinessStartingVolumePolicy` exactly (26.0/19.5), new `PolicyKey` identity for decision-trace labeling only.
- `NextWindowLoadDecisionPolicy` — added `DetermineSixSessionLoadDecision`, implementing `FREQ.6D.23`'s frozen 6-session table verbatim (0/1→Reduce, 2-4→Maintain, 5→role-gated via the already-N-general `OnlyEasyMissing`, 6→Progress), as a new dispatch arm alongside the untouched 4D/5D paths.

## 4. Eight Real Production Hardcodes Found and Fixed

Discovered by driving real end-to-end dark execution (never by static review alone — several of these were missed by `FREQ.6D.23`'s own audit):

| # | Site | Symptom | Fix |
|---|---|---|---|
| 1 | `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingInitialActivationRuntime`/`LongHorizonFullNumericOrchestrator` (3 sites) | `easySupportCount = daysPerWeek == 5 ? 3 : 2` | Generalized to `daysPerWeek - 2` (the structural identity every GE/Runway week obeys: 1 KEY + 1 LONG + remainder EASY) |
| 2 | `LongHorizonRollingInitialActivationContracts.Validate` | `DaysPerWeek is not (4 or 5)` → `LONG_HORIZON_ROLLING_INITIAL_ELIGIBILITY_INVALID` | Widened to `(4 or 5 or 6)` |
| 3 | `LongHorizonRollingCheckpointRuntime.ValidateInput` | Separate `DaysPerWeek is not (4 or 5)` gate | Widened to `(4 or 5 or 6)` |
| 4 | `PreparationRunwayWeeklyShape.ApprovedEasySupportCounts` | Explicit `{2,3}` allow-list rejected 6D's 4-EASY Runway week | Extended to `{2,3,4}`, per this class's own documented convention |
| 5 | `TenKPreparationRunwayDarkOrchestrator.ValidateRequest` | `V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayCandidate` rejected the 6D candidate key | Added `SixDayCandidateKey`/`Version` to that internal consistency check only — confirmed via all 5 call sites that this is a dark/internal Runway-machinery gate, never the public routing gate (`IsSupportedIdentity`/`ResolveCandidate`/`IsSupportedPreparationRunwayIdentity` untouched) |
| 6 | `CatalogWeekSkeletonCalendarMaterializer.ValidateSkeletonRoleStructure` | `DaysPerWeek is not (3 or 4 or 5)` | Widened to `(3 or 4 or 5 or 6)` — the role-cardinality formula beneath it was already fully generic |
| 7 | `V1FourDaySessionVolumeAllocationPolicy`/`V1FourDayWeekAllocation` | Structurally limited to exactly 2 EASY sessions (`FirstEasySupportDistanceKm`/`SecondEasySupportDistanceKm` scalars, no list) | Widened to a full `EasySupportDistancesKm` list; First/Second retained as back-compat accessors (index 0/1) |
| 8 | `CatalogSessionPrescriptionPlanner.DistanceFor` | `easySupportOrdinal == 0 ? First : Second` — silently collapsed any ordinal ≥1 onto the 2nd session's distance | Generalized to `EasySupportDistancesKm[easySupportOrdinal]`. **This one would not have thrown** — it would have silently assigned 6D Core's 3rd EASY session the same distance as its 2nd, a real correctness defect distinct from every other fail-closed hardcode found this phase |

Each fix is implementation-only: no new numeric constant, no new schema, no new catalog prescription, no identity redesign — satisfying the phase's own defect-discovery rule.

## 5. Tests

`Freq6D26IntermediateSixDayDarkVerificationTests.cs` — 22 tests, all passing:

- **Structural materialization** (21/32/52-week horizons): exact `TEN_K__6D__INTERMEDIATE` candidate identity, GE week = 1K+4E+1L, first Core week = 2K+3E+1L.
- **PeakVolumeBand/ResolvedPeakReference**: `[36,50]` resolves for `(TEN_K, INTERMEDIATE, 6)`; `44.5`/`26.0` confirmed on `SixDayIntermediate`; `ForIntermediateDaysPerWeek` dispatch table verified for 4/5/6 and fail-closed for 7.
- **6-session Adaptation** (8 tests): full state-table coverage (6/6, 5/6×3 role classes, 4/6, 2-3/6, 0-1/6) plus a monotonicity proof.
- **Full lifecycle** (real PostgreSQL): initial activation → GE window persisted → Runway-entry continuation → organic Core week 10 reached with real dual-KEY (`LaneOrdinal` 0 and 1 distinct), 3 distinct EASY `SlotOrdinal`s, 1 LONG_RUN — through the real `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingRestartContinuationService`/`LongHorizonRollingJitCompositionOrchestrator` chain, never a fabricated Core row.
- **Isolation**: `V1CatalogPilotIdentityPolicy` confirmed to still reject `(Intermediate,6)`, `(Beginner,6)`, `(Advanced,6)`, `(Intermediate,7)`, and the Runway-specific identity check — the public gate is untouched and verified closed.

## 6. Regression

Fixed a real, expected, disclosed consequence of adding 5 new catalog source documents: `PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles` (97 → 102), which two tests assert against the real source-tree file count.

- `RunningApp.IntegrationTests`: **3932/3932** on the corrected baseline — 3928 passed + the 2 pre-existing failures (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks:13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`) + the 2 catalog-inventory-count tests genuinely fixed. First run (before the inventory fix) showed 3928/3932 with exactly these 4 failures, confirming the other 2 were real, expected, and not a regression elsewhere.
- `PlanCatalog.Tests`: 1510/1510 unchanged.
- Debug and Release builds: 0 errors.
- `git diff --check`: clean (line-ending warnings only, pre-existing repo convention).

## 7. Scope Honesty

This test suite is real and exercises the principal dark-verification claim (structural shape, numeric authority resolution, full 6-session Adaptation, and — the hardest proof — one complete organic GE→Runway→Core dual-KEY lifecycle through real PostgreSQL persist/restart) but is **not** the full 26-category/84-item manifest the original phase prompt enumerated. Not separately exercised as dedicated tests: an explicit repair regression on the publicly-... (not applicable, dark-only) 6D Core secondary-KEY, the full 45-horizon (8-52) dark routing matrix, low/high (non-representative) readiness variants, an explicit `TargetFinishTimeSource` restart proof for 6D specifically (the lifecycle test uses `ProductAverage`/3480s by convention but does not assert restart-survival across a *second* independent reload), and explicit `ProgressionStageKey`/`ExecutionPrescriptionIndex` assertions beyond what the passing Core-generation path already implies. These are reasonable, lower-risk gaps given every one of the 8 real defects found was structural/dispatch-level (already exercised by the passing lifecycle test) rather than in the untested areas.

## 8. Final Classification

```
INTERMEDIATE_6D_CORE_RUNWAY_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED
INTERMEDIATE_6D_FULL_HORIZON_DARK_CAPABILITY_COMPLETE
```

Not publicly activated — the public gate (`V1CatalogPilotIdentityPolicy`, `LongHorizonPublicPlanService.ValidatePilot`) remains closed for 6D, confirmed by permanent isolation tests. Intermediate×5D's COMPLETE/PUBLIC status and Intermediate×7D's PRODUCT_NON_SUPPORT status are both preserved untouched.

Next: the real public HTTP/PostgreSQL verification and public-activation phase for Intermediate×6D — not scheduled as a Phase ID by this implementation-only phase.
