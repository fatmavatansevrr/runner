# PHASE DIST-GEN.6 — 15K × Intermediate × 4D Full Dark Second-Target Projection Implementation

## 1. DIST-GEN.5 authority consumed

Every numeric field implemented here is taken verbatim from `PHASE_DIST_GEN_5_15K_INTERMEDIATE_4D_SECOND_TARGET_TARGET_DISTANCE_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md` §53 (the complete 15K policy table). No value was re-derived, re-researched, or interpolated. The one genuinely target-specific field, `PreferredAbsolutePeakLongRunKm = 14.5` (§35), is implemented exactly as frozen — never 15.0 (16K's own value) and never re-computed from a ratio.

## 2. Repository baseline

Starting commit `869138f370794f5781b2cf44178283868ee2a06a` ("docs: backfill governance commit SHA for DIST-GEN.5"), branch `main`, `git status -b` reports `main...origin/main` with no ahead/behind annotation (0 ahead / 0 behind). Working tree carried only pre-existing tracked build-artifact noise under `backend/RunningApp.Api/bin/Debug/net9.0/...` at the start (per the conversation's own git-status snapshot) — not touched by this phase.

## 3. Build-artifact baseline/noise handling

The pre-existing `bin/`/`obj/`/`TestResults/`/copied catalog-artifact noise under `backend/RunningApp.Api/bin/...` was left exactly as found — never staged, never cleaned. All `git status` / `git diff` commands used throughout this phase excluded `bin/`, `obj/`, and `TestResults/` paths when auditing source changes (§4, §61).

## 4. Files changed (exact list)

**Modified (8):**
- `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs` (doc comment only — updated to describe the registry, not the logic)
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` (mismatch guard now uses `IsDarkEligible`)
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` (dispatch generalized to registry-resolved cell)
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KAwarePeakVolumeBandLoader.cs` (generalized to registry-resolved cell; name kept, per the lower-risk option)
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` (new `HalfMarathonIntermediate4DTargetDistance15K` instance added)
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` (skeleton horizon dispatch + `ResolveRequestedTargetDistanceKm` generalized)
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` (approved-cell registry added; `IsEligible` overloads left byte-identical)
- `backend/RunningApp.Application/Services/PlanServices.cs` (horizon dispatch generalized to registry-resolved cell, message text preserved per-pilot)

**Added (5):**
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/TargetDistance15KHorizonPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance15KPeakVolumeBandPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance15KTaperVolumePolicy.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark15KTargetDistanceProjectionTests.cs`
- `backend/RunningApp.IntegrationTests/DistGen6PublicBoundaryZeroDeltaTests.cs`

**Untouched (explicitly verified):** `backend/RunningApp.Application/Commands/Plan/GeneratePreviewCommandMapper.cs` (the public activation gate), `GenerateRacePlanPreviewRequestValidator.cs`, `RaceHorizonPolicy.cs`, every catalog JSON file, every EF Core migration file, every Flutter/`mobile/` file, `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`.

## 5. Existing 16K architecture audit

Confirmed via direct grep (matching DIST-GEN.4 §38's own finding) exactly 6 real production call sites consulted `Dark16KPilotEligibilityPolicy`: `PlanServices.cs`, `GeneratePreviewCommandMapper.cs`, `CatalogPreviewGenerator.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `CatalogPrescriptionContextBuilder.cs`. Of these, only `GeneratePreviewCommandMapper.cs` is the PUBLIC activation gate; the other 5 are dark-materialization-eligibility consumers. Three companion policy classes existed alongside the eligibility class: `TargetDistance16KHorizonPolicy`, `TargetDistance16KPeakVolumeBandPolicy`, `TargetDistance16KTaperVolumePolicy` — all single-target, hardcoded to 16.0.

## 6. 16K-specific hardcode classification

| Hit | Classification |
|---|---|
| `Dark16KPilotEligibilityPolicy.IsEligible(...)` in `GeneratePreviewCommandMapper.cs` | **intentional-public-gate** — left byte-identical, still only 16.0 |
| `Dark16KPilotEligibilityPolicy.EligibleTargetDistanceKm`/`EligibleParentDistanceFamily`/`EligibleLevel`/`EligibleRunsPerWeek` constants | **target-specific-authority** — kept as the 16K entry's own literal values, now also mirrored (not renamed) into `ApprovedDarkCells[0]` |
| `TargetDistance16KHorizonPolicy`/`TargetDistance16KPeakVolumeBandPolicy`/`TargetDistance16KTaperVolumePolicy` class names | **legacy-safe** — kept unrenamed per the lower-risk option; new `TargetDistance15K*` siblings added instead |
| `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K` | **target-specific-authority** — untouched, new `...TargetDistance15K` sibling added |
| `isDark16KEligible`/`isDark16KEligibleForSkeleton` local variable names in `PlanServices.cs`/`CatalogPreviewGenerator.cs` | **generic-concept-hidden-behind-16K-name** — variable now means "any dark-approved cell matched"; renamed reasoning documented inline, not renamed itself (lower blast radius) |
| Message text `"dark 16K target-distance..."` in `PlanServices.cs` exceptions | **test-only-sensitive / intentional** — preserved byte-identical for the 16K case (asserted by `Dark16KTargetDistanceProjectionTests`); mirrored, not merged, for the 15K case |
| `DARK_16K_CORE_HORIZON_*` reason code | **target-specific-authority** — untouched; `DARK_15K_CORE_HORIZON_*` added as an independent sibling |
| Test-file references (`Dark16KTargetDistanceProjectionTests.cs`) | **test-only** — zero edits made to this file; verified still 50/50 passing |
| No bug-classified hits were found. | — |

## 7. Second-target ownership model

A small, explicit, in-code registry: `Dark16KPilotEligibilityPolicy.ApprovedDarkCells` — an `IReadOnlyList<ApprovedDarkTargetDistanceCell>` with two entries (16.0 and 15.0, both HalfMarathon/Intermediate/4). Lookup is via `TryResolveDarkEligibleCell(...)` (two overloads mirroring the pre-existing `IsEligible` overload shapes), returning the matched cell or null — never nearest/interpolated. This registry is a DISTINCT concept from the unmodified, narrower `IsEligible(...)` overloads that remain the sole authority for PUBLIC activation (still true only for 16.0).

## 8. Projected-target policy selection

Every dark call site that previously branched on a single boolean now resolves the matched `ApprovedDarkTargetDistanceCell` once, then dispatches to the target-specific sibling class/instance via a `cell.TargetDistanceKm` pattern match (`{ TargetDistanceKm: 15.0 }` / `{ TargetDistanceKm: 16.0 }`), never a shared/reused single instance across both targets.

## 9. Dark eligibility

Verified by `Dark15KTargetDistanceProjectionTests.DarkEligibilityRegistry_15KAnd16K_AreTheOnlyEligibleCells` (17 inline cases): 15K Intermediate 4D and 16K Intermediate 4D are dark-eligible; 15K Beginner 4D, 15K Advanced 4D, 15K Intermediate 3D/5D/6D, 15K TenK-family, null override, and near-miss distances (12.0/18.0/20.0/14.9/15.1/15.9/16.1/16.09) are all NOT dark-eligible.

## 10. Structural-family resolution

`CanonicalDistanceFamilyResolver_15K_ResolvesToHalfMarathon_PreservesExactValue` confirms 15.0 → `HALF_MARATHON`, with `RequestedTargetDistanceKm` preserved exactly as 15.0 (never 21.0975, 16.0, or 5.0).

## 11. Exact-target preservation

Confirmed at both the resolver layer (§10) and the full dark pipeline layer (`GoldenTrace_12W_MaterializesWithFrozen15KNumerics...` asserts `context.ResolverInput.RequestedTargetDistanceKm == 15.0` before materializing) and at persistence (`RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` asserts `persistedPlan.RequestedTargetDistanceKm == 15.0`, `NotEqual(16.0, ...)`, and `NotEqual(..., GoalDistanceKm)`).

## 12. Catalog candidate reuse

The dark 15K pipeline resolves to `HALF_MARATHON__4D__INTERMEDIATE` — the identical, unmodified candidate 16K already reuses — confirmed by `CandidateAsync()` loading `V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey` unchanged, and by the public-boundary test asserting `template_id == "HALF_MARATHON__4D__INTERMEDIATE"` is never reachable for a `target_distance_km: 15.0` public request (it 400s before any candidate is loaded).

## 13. Horizon implementation

`TargetDistance15KHorizonPolicy` (new, independently declared, mirroring `TargetDistance16KHorizonPolicy`'s shape) freezes `MinimumCoreWeeks=10`, `PreferredCoreWeeks=12`, `MaximumCoreWeeks=14`, and its own reason code `DARK_15K_CORE_HORIZON_{weeks}_NOT_SUPPORTED`, distinct from `DARK_16K_CORE_HORIZON_*`.

## 14. Lower boundary (9W rejection)

`RealPipeline_BelowMinimumHorizon_RejectedByDark15KAuthority_NeverTargetBelowSumOfMinimums` (7/8/9W) confirms `PlanCoreHorizonTooShortException` with `DARK_15K_CORE_HORIZON` and `"dark 15K target-distance"` in the message, and explicitly asserts the message never contains `TARGET_BELOW_SUM_OF_MINIMUMS`, the HALF_MARATHON message shape, or `DARK_16K`.

## 15. Upper boundary (15W rejection)

`RealPipeline_AboveMaximumHorizon_RejectedByDark15KAuthority` (15W) confirms `PlanHorizonCompositionRequiredException` with `DARK_15K_CORE_HORIZON`, never `DARK_16K`, and never `PlanHorizonStartTooEarlyException`.

## 16. Supported-horizon matrix (all 5 horizons, real traces)

`AllFiveHorizons_MaterializeWithExpectedPhaseSplitsAndPeakVolume` (Theory, 5 cases) and `RealPipeline_EligibleHorizons_MaterializeSuccessfully` (10-14W) both pass. Real observed peak weekly volumes: 10W→31.0, 11W→32.5, 12W→34.0, 13W→34.0, 14W→34.0 — numerically identical to 16K's own already-proven traces (DIST-GEN.2B §7-11), confirmed exactly as predicted.

## 17. Phase allocation (exact splits, all 5 horizons)

| Weeks | FOUNDATION | BUILD | RACE_SPECIFIC | TAPER |
|---|---|---|---|---|
| 10 | 2 | 3 | 3 | 2 |
| 11 | 2 | 3 | 4 | 2 |
| 12 | 2 | 3 | 5 | 2 |
| 13 | 2 | 4 | 5 | 2 |
| 14 | 3 | 4 | 5 | 2 |

All 5 confirmed by real pipeline runs (`AllFiveHorizons_MaterializeWithExpectedPhaseSplitsAndPeakVolume` and `GoldenTrace_12W_...`), matching DIST-GEN.5 §15/§46's prediction exactly.

## 18. Stage occupancy

Every prescribed week carries exactly 4 sessions (`Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count))`), identical to the reused `RUN_LAYOUT_4D` structure — zero new stage/session-slot logic introduced.

## 19. Target-race-pace reuse

Confirmed `GENERIC_REUSABLE_INFRASTRUCTURE` — no code change was made to any pace-resolution logic. `RequestedTargetDistanceKm=15.0` flows through the same unmodified `GoalFeasibilityResolver`/pace-source logic 16K already exercises.

## 20. Riegel reuse

Unmodified — no exponent, formula, or distance-specific branch was added or touched.

## 21. Goal-time fitness separation

Unmodified — no code path here re-derives or substitutes goal-time fitness from calibration/peak-band data.

## 22. Product-average behavior

Unmodified — the existing Riegel-projection-from-HM's-own-canonical-constant mechanism is target-distance-generic already and required zero code change for 15.0.

## 23. Peak-volume policy (exact 34.0/46.0/40.0)

`TargetDistance15KPeakVolumeBandPolicy.MinimumKm=34.0`, `MaximumKm=46.0` (equal to 16K's own `TargetDistance16KPeakVolumeBandPolicy`, asserted directly in `PeakVolumeBandPolicy_15K_EncodesFrozenBand_IdenticalToButIndependentFrom16K`). `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K.ResolvedPeakReference.Value == 40.0`.

## 24. Selected peak (40.0, never forced)

`ResolvedPeakReferenceIsSelectedCeiling = true` on the 15K instance (reusing HM.5.3's generic mechanism unmodified) — real observed peaks (31.0-34.0) across all 5 horizons stay strictly below 40.0, confirmed never force-extrapolated past it (§16).

## 25. Weekly growth (0.07/0.08/2.5 reused)

`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5` — asserted byte-identical to every other Intermediate-4D anchor in `VolumeSafetyPolicy_15K_EncodesExactFrozenValues`.

## 26. Absolute weekly cap

`2.5` km, confirmed identical across TEN_K/HM/16K/15K Intermediate-4D anchors — no interpolation.

## 27. Calibration safety (23.5, never used as runtime fitness)

`GoldenFixtureStartingVolumeKm=23.5` on the 15K instance — reused exactly from the 25.0/43.0=0.5814 ratio applied to 40.0 (per DIST-GEN.5 §32), never read back as a missing-readiness fallback (the same fail-closed `ResolveStartingVolume` guard applies unchanged; no code was added to this guard).

## 28. LR shares (0.30/0.36/0.33/0.40)

Confirmed byte-identical to every other Intermediate-4D anchor in `VolumeSafetyPolicy_15K_EncodesExactFrozenValues`.

## 29. Absolute peak LR (14.5 — the one genuinely different field)

`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K.PreferredAbsolutePeakLongRunKm == 14.5`, explicitly asserted `NotEqual` to the 16K instance's own `15.0` in the same test. This is mechanically required: 16K's own 15.0km ceiling would equal the 15K race distance itself, violating the binding "peak LR strictly below target distance" invariant. 14.5 is both DIST-GEN.5-evidence-supported and satisfies the invariant with a real 0.5km margin.

## 30. Starting LR cap (null)

`StartingAbsoluteLongRunCapKm` is null on the 15K instance, mirroring `HalfMarathonIntermediate4D`'s own null — confirmed in `VolumeSafetyPolicy_15K_EncodesExactFrozenValues`.

## 31. Taper (2 weeks, 0.70/0.43, non-chained)

`TargetDistance15KTaperVolumePolicy` (new, independent class) freezes `TaperWeek1VolumeMultiplier=0.70`, `TaperWeek2VolumeMultiplier=0.43`, `ResolvedPeakReferenceKm=40.0`. `GoldenTrace_12W_...` confirms non-chained semantics: `Math.Abs(w2 - w1 * 0.43) > 0.5` (i.e., Week 2 is NOT Week 1 × 0.43 — both are independently computed from the fixed 40.0 anchor).

## 32. Race-specific dose (unchanged, generic)

No code change — the existing HM-derived race-specific workout dose mechanism is fully target-distance-agnostic and was not touched.

## 33. Session allocation

Confirmed via `Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count))` across all tested horizons — KEY+EASY1+EASY2+LONG summing correctly, unmodified allocator.

## 34. Calendar (generic, unchanged)

No code change — calendar materialization logic carries no target-distance-specific branch.

## 35. 15K golden trace (full 12W table)

From `GoldenTrace_12W_MaterializesWithFrozen15KNumerics_LrCeilingIsFourteenPointFive` (real pipeline run): phase split FOUNDATION=2/BUILD=3/RACE_SPECIFIC=5/TAPER=2; peak weekly volume = 34.0km (matches 16K's own 12W trace exactly); peak observed long run = 13.5km (matches 16K's own trace exactly); both LR ceiling checks pass (`< 15.0` and `<= 14.5`); non-chained taper confirmed. This is numerically identical to 16K's own already-proven 12W golden trace (DIST-GEN.2B §7-11), with the sole difference being the tighter (14.5 vs 15.0) LR ceiling validation, which does not bind at this readiness (consistent with DIST-GEN.5 §43's own prediction).

## 36. 15K-vs-16K comparison trace (side by side)

`SideBySide_15KAnd16K_SameHorizonSameReadiness_DistinctTargetDistanceAndLrAuthority` (real pipeline, both targets materialized at 12W with identical readiness inputs): both plans valid; both resolve the same catalog candidate identity; `RequestedTargetDistanceKm` correctly distinct (15.0 vs 16.0); `PreferredAbsolutePeakLongRunKm` authority correctly distinct (14.5 vs 15.0) even though the actual observed peak LR values coincide numerically (both 13.5km) at this typical readiness — exactly as DIST-GEN.5 §43 anticipated.

## 37. Independent policy identity proof

Same test additionally resolves `TryResolveDarkEligibleCell(15.0, ...)` and `TryResolveDarkEligibleCell(16.0, ...)` directly and asserts each returns a distinct `ApprovedDarkTargetDistanceCell` record with the correct own `TargetDistanceKm` — never cross-resolving. `DarkEligibilityRegistry_15KAnd16K_ResolveToDistinctIndependentCells` independently re-confirms this at the registry level.

## 38. Unsupported nearby targets (14.9/15.1/15.9/16.1)

All four confirmed NOT dark-eligible in `DarkEligibilityRegistry_15KAnd16K_AreTheOnlyEligibleCells` — no nearest-target snapping exists anywhere in the resolution logic (exact-match-only via `Math.Abs(km - cell.TargetDistanceKm) < 0.001`).

## 39. Wrong-cell negatives (15K Beginner/Advanced/3D/5D/6D)

All confirmed NOT dark-eligible in the same theory (§9/§38).

## 40. 10-mile exact status (unchanged)

16.09 confirmed NOT dark-eligible (same theory) and NOT publicly eligible — no code touched `TenMileExact_16Point09_NotTreatedAsEligible_TypedRejection` in `DistGen3PublicActivationTests.cs`, which still passes unchanged (§54).

## 41. Public 15K remains blocked (real HTTP proof)

`DistGen6PublicBoundaryZeroDeltaTests.PublicPreview_TargetDistance15_StillRejected_400_UnsupportedTargetDistance_NoPersistence` and the 5-horizon theory variant: real HTTP `POST /api/v1/plans/generate-preview/race` with `target_distance_km: 15.0` returns 400 `UNSUPPORTED_TARGET_DISTANCE` at every eligible dark horizon (10-14W), with zero preview/plan rows persisted. `GeneratePreviewCommandMapper.cs` was not modified at all — this is the same, unmodified public-gate logic DIST-GEN.3 froze.

## 42. Public 16K remains live (real HTTP proof, zero-delta)

`PublicPreview_TargetDistance16_StillSucceeds_200_IdenticalIdentity` (5 horizons) and `PublicPreview_TargetDistance16_9WeekHorizon_StillTypedRejection_Unchanged` confirm 200 OK with correct `requested_target_distance_km=16.0`/`template_id=HALF_MARATHON__4D__INTERMEDIATE` and the unchanged `DARK_16K_CORE_HORIZON` 9W rejection. The pre-existing `DistGen3PublicActivationTests.cs` (22 tests, unmodified) also re-ran clean (§54).

## 43. Original Custom safety (422 regression)

`GoalDistanceCustom_NoTargetDistanceKm_StillFailsClosed_422` confirms `goal_distance: custom` with no `target_distance_km` still returns 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`.

## 44. Habit Custom preservation

Zero diff confirmed: `git diff` shows no changes to `GenerateHabitPlanPreviewRequest.cs`, `GenerateHabitPlanPreviewRequestValidator.cs`, or any `custom_goal_page.dart`/Flutter file (§4's exact file list contains none of these; §53 confirms zero `mobile/` diff).

## 45. Persistence proof/readiness

Fully testable via the internal dark seam (no public activation needed to reach `ConfirmPlanAsync`): `RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` confirms `CanonicalDistanceFamily="HALF_MARATHON"`, `RequestedTargetDistanceKm=15.0`, distinct from `GoalDistanceKm`, persisted correctly through the confirm pipeline. Not deferred.

## 46. Persistence-fix preservation

`Dark16KTargetDistanceProjectionTests.RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` (unmodified test file) re-ran clean, confirming 16K's own persisted `RequestedTargetDistanceKm=16.0` is unaffected.

## 47. TEN_K zero-delta

`DistGen6PublicBoundaryZeroDeltaTests.ZeroDelta_TenKIntermediate4D_Unaffected` (real HTTP) and the pre-existing `DistGen3PublicActivationTests.ZeroDelta_TenKIntermediate4D_Unaffected` both pass: `template_id="TEN_K__4D__INTERMEDIATE"`, `requested_target_distance_km` null.

## 48. HM 4D zero-delta

`DistGen6PublicBoundaryZeroDeltaTests.ZeroDelta_HalfMarathonIntermediate4D_Unaffected` and the pre-existing `DistGen3PublicActivationTests.ZeroDelta_HalfMarathonIntermediate4D_Unaffected` both pass.

## 49. HM 6D zero-delta

Not re-added as a new test (no shared dispatch code specific to 6D was touched by this phase — `CatalogVolumeAndLongRunPlanner`'s 6D branch is untouched, gated on `DaysPerWeek == 6` and unrelated to the HALF_MARATHON Intermediate 4D branch this phase modified). The pre-existing `DistGen3PublicActivationTests.ZeroDelta_HalfMarathonIntermediate6D_Unaffected` re-ran clean as part of the full regression (§58).

## 50. 16K public zero-delta (comprehensive)

All 22 pre-existing `DistGen3PublicActivationTests` tests pass unchanged (§54), plus the new 15 `DistGen6PublicBoundaryZeroDeltaTests` (§41/§42) — 37 total real-HTTP tests confirming zero delta to the public 16K pilot.

## 51. Catalog/hash integrity

`git status --porcelain -- plan-catalog` returned empty before this phase's work. Running `PlanCatalog.Tests` (§55) auto-regenerated the known benign `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.json`/`.md` pair (a pre-existing, documented auto-regen artifact, unrelated to any 15K/16K catalog content); both files were reverted via `git checkout --` immediately after the test run, restoring `git status --porcelain -- plan-catalog` to clean (confirmed empty except for a harmless, untracked `TestResults/*.trx` file under `plan-catalog/tests/`, left in place per the "don't clean up build/test noise" instruction). No catalog JSON file was ever intentionally edited by this phase.

## 52. Database/schema zero-delta

No EF Core migration file was created or modified — `git status --porcelain -- backend/RunningApp.Persistence/Migrations` (implicitly covered by §4's exact file list, which contains no migration file) confirms zero schema change. `TrainingPlan`'s existing nullable `RequestedTargetDistanceKm`/`CanonicalDistanceFamily` columns (already present since DIST-GEN.2) are reused unchanged.

## 53. Frontend zero-delta

`git status --porcelain -- mobile` (and the full §4 file list) confirms zero Flutter diff — no file under `mobile/` was touched.

## 54. Targeted tests (full list + outcomes)

- `Dark16KTargetDistanceProjectionTests` (pre-existing, unmodified): 50/50 pass.
- `Dark15KTargetDistanceProjectionTests` (new): 53/53 pass.
- `DistGen3PublicActivationTests` (pre-existing, unmodified): 22/22 pass.
- `DistGen6PublicBoundaryZeroDeltaTests` (new): 15/15 pass.

Combined `TargetDistanceProjection` namespace filter: 103/103 pass (50+53).

## 55. PlanCatalog regression (exact numbers)

`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1510 total / 1510 passed / 0 failed / 0 skipped**, 7s. Exactly the required baseline — zero catalog file was touched.

## 56. RuntimeCatalog regression (exact numbers)

`dotnet test ... --filter "FullyQualifiedName~RuntimeCatalog"`: **3945 total / 3945 passed / 0 failed / 0 skipped**, 6m12s. Prior baseline (post-DIST-GEN.3) was 3892/3892 — the +53 delta is exactly the new `Dark15KTargetDistanceProjectionTests` (which live under the `RuntimeCatalog.TargetDistanceProjection` namespace). 100% pass rate.

## 57. PlanGeneration regression

N/A as a distinct namespace subset for this phase's purposes — `PlanGeneration` tests are included in, and pass within, the full monolithic run (§58); no `PlanGeneration`-specific file was touched by this phase.

## 58. Full monolithic regression (exact numbers, exact failing identities)

`dotnet test RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj` (fresh, from a clean build — see §60): **4958 total / 4954 passed / 4 failed / 0 skipped**, 45m15s. Exact failing identities (verified by direct extraction from the trx/console output, not assumed):
1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

These are byte-identical to the 4 known pre-existing failing identities named in the governing prompt — no new failing identity appeared.

## 59. Exact durable-baseline comparison

| | Baseline (post-DIST-GEN.3) | This phase (post-DIST-GEN.6) | Delta |
|---|---|---|---|
| Total | 4890 | 4958 | +68 |
| Passed | 4886 | 4954 | +68 |
| Failed | 4 | 4 | 0 (identical identities) |
| Skipped | 0 | 0 | 0 |

+68 = +53 (`Dark15KTargetDistanceProjectionTests`) + 15 (`DistGen6PublicBoundaryZeroDeltaTests`). Exact match to expectation; zero unexplained delta.

## 60. Build-freshness verification

`dotnet build backend/RunningApp.sln -v minimal` was run fresh immediately after every source edit batch (not from a stale prior build), most recently confirming **0 errors** and the same 16 pre-existing warnings (all in unrelated `PreparationRunway*`/`Gen9*`/`Hm*` test files, none touching this phase's files) both before and after this phase's edits. The full monolithic test run (§58) was executed via `dotnet test ... --no-build` immediately following a `dotnet build` of the IntegrationTests project that itself referenced the freshly-rebuilt `RunningApp.Application.dll` (confirmed via the build's own dependency-graph output showing `RunningApp.Application -> ... .dll` rebuilt before `RunningApp.IntegrationTests -> ... .dll`).

## 61. Final source-diff audit

`git status --porcelain -- backend | grep -v "bin/\|obj/\|TestResults/"` returns exactly the 13 files listed in §4 (8 modified + 5 added) — no unexpected file. Re-verified: `GeneratePreviewCommandMapper.cs`, `GenerateRacePlanPreviewRequestValidator.cs`, `RaceHorizonPolicy.cs`, and every file under `plan-catalog/` and `mobile/` show zero diff.

## 62. Second-target architecture result

**`SECOND_TARGET_DARK_REUSES_EXISTING_PROJECTION_ARCHITECTURE`.** No new architectural mechanism was invented: the existing eligibility-gate/horizon-policy/peak-band-policy/taper-policy/VolumeSafetyPolicy-instance pattern DIST-GEN.2 established was generalized (via a small explicit registry plus independently-declared sibling classes/instances) to carry a second target cleanly. Every dark call site's *shape* (resolve eligibility → dispatch to target-specific policy → materialize) is unchanged; only the *selection logic* at each of the 5 non-public-gate call sites was widened from a boolean to a registry lookup.

## 63. Current public/dark matrix

| Target | Public | Dark |
|---|---|---|
| 16.0 (HalfMarathon/Intermediate/4D) | LIVE (200 OK) | LIVE |
| 15.0 (HalfMarathon/Intermediate/4D) | BLOCKED (400 UNSUPPORTED_TARGET_DISTANCE) | LIVE (dark-only) |
| Every other target/cell (12/18/20/14.9/15.1/15.9/16.1/16.09, any Beginner/Advanced, any non-4D frequency at these targets) | BLOCKED | BLOCKED |
| TEN_K, HALF_MARATHON canonical (no override) | LIVE (unchanged) | N/A |

## 64. DIST-GEN.7 readiness statement

This phase deliberately does NOT begin any public-activation work for 15K. Should a future DIST-GEN.7 phase choose to publicly activate 15K, the widening required is narrow and already-scoped: extend `GeneratePreviewCommandMapper.cs`'s own canonicalization gate (and `GenerateRacePlanPreviewRequestValidator.cs`'s narrowed Custom guard, if it enumerates 16.0 explicitly) to also accept 15.0 — a change deliberately NOT made here.

## 65. Remaining blockers

None. Every DIST-GEN.5 field is implemented exactly (§23-31); no new training decision was invented; 15K and 16K resolve genuinely distinct authority (§29, §36, §37); all 5 horizons materialize (§16-17); both boundaries reject correctly with typed, target-specific reason codes (§14-15); public 15K remains blocked and public 16K remains zero-delta (§41-42, §50); full regression is clean against the documented baseline with zero new failing identity (§58-59); the architecture result is the "reuses existing architecture" outcome (§62).

---

## Final classification

**15K_INTERMEDIATE_4D_FULL_DARK_SECOND_TARGET_PROJECTION_IMPLEMENTATION_COMPLETE**
