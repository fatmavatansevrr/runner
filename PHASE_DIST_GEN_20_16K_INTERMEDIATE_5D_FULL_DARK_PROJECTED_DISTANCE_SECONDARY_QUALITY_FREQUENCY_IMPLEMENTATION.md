# PHASE DIST-GEN.20 — 16K × Intermediate × 5D Full Dark Projected-Distance Secondary-Quality-Frequency Implementation

**IMPLEMENTATION / DARK-MATERIALIZATION / CROSS-FREQUENCY / KEY2 PROOF PHASE.** This phase implements exactly the authority DIST-GEN.19/19A already froze for `TargetDistanceKm=16.0 × ParentDistanceFamily=HALF_MARATHON × Level=Intermediate × RunsPerWeek=5`. Zero new training-science, product-default, workout-semantic, or KEY2 decision is made anywhere in this phase; every numeric literal introduced already exists, byte-for-byte, in DIST-GEN.19/19A's own frozen tables.

## 1. DIST-GEN.19A authority consumed

The complete authority table from DIST-GEN.19A §42 (reproducing DIST-GEN.19 §61 with the four formerly-blocked rows closed) was consumed verbatim: structural parent `HALF_MARATHON`; catalog candidate `HALF_MARATHON×Intermediate×5D` (existing, unmodified); `RUN_LAYOUT_5D` = `[KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN]`; horizon `MinimumCoreWeeks=10/PreferredCoreWeeks=12/MaximumCoreWeeks=14`; growth `0.07/0.08`; `AbsoluteWeeklyIncreaseCapKm=2.5`; LR shares `0.28/0.36/0.28/0.36`; `StartingAbsoluteLongRunCapKm=12.0` (`PARENT_FAMILY_X_FREQUENCY`); `PreferredAbsolutePeakLongRunKm=15.0` (`TARGET_ONLY`, NOT HM's 19.0); taper `2wk/0.70/0.43` non-chained; `PeakVolumeBandMin/Max=44.0/58.0`, `SelectedPeakReference=51.0`, `CalibrationStartingVolumeKm=29.5` (exact-cell `EVIDENCE_INFORMED_PRODUCT_DEFAULT`); and the complete KEY2 phase table (`KEY2_FULLY_REUSED_FROM_CANONICAL_FREQUENCY_PARENT_AUTHORITY`). No field was re-derived, adjusted, or re-decided.

## 2. Repository baseline

`git fetch origin main && git rev-list --left-right --count HEAD...origin/main` returned `0	0` before any edit. HEAD was at `2925711` ("docs(dist-gen-19a): backfill governance commit SHA for DIST-GEN.19A"), matching the expected baseline exactly. Pre-existing `git status` noise (tracked `bin/`/`obj`/packaged catalog-bundle copies under `backend/RunningApp.Api/bin/Debug/net9.0/...`, unrelated to this phase) was identified and excluded from staging; only real source/test/governance files described in §5 were staged.

## 3. Accepted regression baseline

DIST-GEN.18/19/19A were governance-only, so the last accepted runtime baseline remained DIST-GEN.17's: PlanCatalog.Tests 1510/1510; RuntimeCatalog subtree 4042/4042; full monolithic 5174 total/5170 passed/4 known durable failures. This phase's own regression results are recorded in §87-91.

## 4. Pre-implementation characterization

Before editing, the dark 16K I3D/I4D cells, canonical HM I5D, and canonical TEN_K I5D were confirmed unregistered-consumer-unaffected by reading `VolumeSafetyPolicy.cs`, `ProjectedTargetAuthorityRegistry.cs`, and `Dark16KPilotEligibilityPolicy.cs` directly (§ "Context already established" of the task; independently re-verified, not trusted). No pre-implementation golden run was separately captured beyond this source read, since DIST-GEN.16's own equivalent characterization (3D) already established this exact pattern and no shared code path required re-baselining.

## 5. Files changed

Production (new files):
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/TargetDistance16KIntermediate5DHorizonPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KIntermediate5DPeakVolumeBandPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KIntermediate5DTaperVolumePolicy.cs`

Production (edited):
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added `HalfMarathonIntermediate5DTargetDistance16K` named policy.
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` — added `(16.0, HalfMarathon, Intermediate, 5)` to `ApprovedDarkCells` only.
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/ProjectedTargetAuthorityRegistry.cs` — added one `ProjectedTargetAuthority` registration for the new cell.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — widened the dark-dispatch gate from `(DaysPerWeek == 3 || DaysPerWeek == 4)` to `(... == 3 || ... == 4 || ... == 5)`.

Tests (new):
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/DistGen20Intermediate5DFullDarkImplementationTests.cs` (36 tests).

Tests (edited, in-place update of now-stale historical negative/count assertions this phase's own registration correctly invalidated by design — precedented pattern, see DIST-GEN.17's own in-place updates to DIST-GEN.11's tests):
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/DistGen16Intermediate3DFullDarkImplementationTests.cs` — removed `(16.0, Intermediate, 5)` from the `Registry_WrongCell_NeverResolves` negative-case list.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark18KTargetDistanceProjectionTests.cs` — updated `ApprovedDarkCells.Count` assertion from 4 to 5.
- `backend/RunningApp.IntegrationTests/DistGen13SharedAuthorityNormalizationTests.cs` — updated `Registry_HasExactlyOneAuthorityPerApprovedDarkCell_NoMoreNoFewer`'s count from 4 to 5; removed `(16.0, Intermediate, 5)` from `Registry_WrongLevelOrFrequency_ResolvesNothing_KeyIsTheFullQuadruple`'s negative-case list.
- `backend/RunningApp.IntegrationTests/DistGen17PublicActivationTests.cs` — updated `PublicAndDarkRegistries_BothNowFourEntries_ButRemainIndependentlyDeclaredLists`'s `ApprovedDarkCells.Count` assertion from 4 to 5 (Public stays 4 — this is precisely the intentional divergence DIST-GEN.20 introduces).

Governance:
- This report, `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`.

No catalog JSON, database/migration, API contract, or frontend file was touched. No CatalogPreviewGenerator.cs, PlanServices.cs, or TargetDistance16KAwarePeakVolumeBandLoader.cs change was needed — all three were confirmed, by direct read, to already be fully generic (registry-resolved, no frequency literal), exactly as DIST-GEN.19A §46 predicted.

## 6. Current public/dark matrix

Before: Dark = Public = {15K I4D, 16K I3D, 16K I4D, 18K I4D} (4/4). After: Dark = {15K I4D, 16K I3D, 16K I4D, 16K I5D, 18K I4D} (5); Public unchanged at {15K I4D, 16K I3D, 16K I4D, 18K I4D} (4). Verified by `Dark16KPilotEligibilityPolicy.ApprovedDarkCells.Count == 5` and `ApprovedPublicCells.Count == 4` (test `PublicDarkSeparation_16KI5D_IsDarkOnly_NeverPublic`).

## 7. Exact dark-cell registration

Exactly one new entry added to `ApprovedDarkCells`: `new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 5)`. No range, no wildcard, no `RunsPerWeek >= 3` support. `ApprovedPublicCells` was not touched.

## 8. Three-frequency registry proof

`Registry_16KI3DI4DAndI5D_ResolveAsGenuinelyDistinctAuthorities` resolves `(16.0,HM,Intermediate,3)`, `(16.0,HM,Intermediate,4)`, and `(16.0,HM,Intermediate,5)` simultaneously from the same registry, asserting genuinely distinct `VolumeSafetyPolicy` object references, distinct peak bands/peaks/calibrations, distinct absolute caps and LR-share quadruples, distinct `StartingAbsoluteLongRunCapKm` (null/null/12.0), while confirming shared fields (`TargetDistanceKm`, horizon triple, `PreferredAbsolutePeakLongRunKm=15.0`) agree across all three. `RunsPerWeek` is confirmed the discriminating dictionary key.

## 9. Public registry zero-delta

`ApprovedPublicCells` is unchanged at exactly 4 entries, verified byte-for-byte in `PublicDarkSeparation_16KI5D_IsDarkOnly_NeverPublic` and `Dark18KTargetDistanceProjectionTests`'s own updated assertion (public count still 4).

## 10. Public/dark separation

`Assert.Contains(I5DCell, ApprovedDarkCells)` and `Assert.DoesNotContain(I5DCell, ApprovedPublicCells)` both pass. `IsPubliclyEligible(16.0, HM, Intermediate, 5)` is `false`; `IsDarkEligible` is `true`. This is the first intentional dark/public divergence since DIST-GEN.17 made the two registries numerically equal (4=4); now Dark=5, Public=4.

## 11. Exact target identity

Every materialization request carries `RequestedTargetDistanceKm=16.0` through `ResolverInputSnapshot`/`PrescriptionContext`; the dark-dispatch/registry/peak-band-loader seams all key off this exact value, never the family-representative `21.0975`. Confirmed by the `GoldenTrace_*` tests, which assert `ResolverInput.RequestedTargetDistanceKm == 16.0` throughout materialization.

## 12. Structural parent

`ParentDistanceFamily = HalfMarathon`. No new `SIXTEEN_K` family, no frequency-specific parent. Unchanged from DIST-GEN.19 §11.

## 13. Catalog candidate

Reused `HALF_MARATHON×INTERMEDIATE×5D` (`V1CatalogPilotIdentityPolicy.HalfMarathonFiveDayIntermediateCandidateKey`/`...Version`, `HALF_MARATHON__5D__INTERMEDIATE` v1) — the exact existing candidate canonical HM I5D already uses in production. Zero catalog JSON change.

## 14. RunLayout

Confirmed via real materialization (not assumed): every week produces exactly 5 sessions — 2 `KEY_SESSION`, 2 `EASY_SUPPORT`, 1 `LONG_RUN` — matching `RUN_LAYOUT_5D`'s own real role order, asserted in `GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI5DNumerics`.

## 15. Horizon

`TargetDistance16KIntermediate5DHorizonPolicy`: `MinimumCoreWeeks=10` (parent-family constant, `RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks`), `PreferredCoreWeeks=12`/`MaximumCoreWeeks=14` (referenced directly from `TargetDistance16KHorizonPolicy`, target-only, frequency-invariant). Reason code: `DARK_16K_I5D_CORE_HORIZON_{n}_NOT_SUPPORTED`.

## 16. 9W rejection

`RealPipeline_BelowMinimumHorizon_RejectedByOwnTyped16KI5DAuthority` (7/8/9W) throws `PlanCoreHorizonTooShortException` containing `DARK_16K_I5D_CORE_HORIZON_{n}_NOT_SUPPORTED` and the pilot label `"dark 16K Intermediate 5D"` — never the generic allocator's `TARGET_BELOW_SUM_OF_MINIMUMS` message, never HM's own message.

## 17. 10W materialization

Succeeds (`RealPipeline_EligibleHorizons_MaterializeSuccessfully(10)` and `GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI5DNumerics(10)`); 5 sessions/week, peak ≤51.0, LR ≤15.0, weekly deltas ≤2.5km, first-week LR ≤12.0km starting cap.

## 18. 11W materialization

Succeeds identically to §17 at 11 weeks.

## 19. 12W golden

`GoldenTrace_12W_HardCapShareClampsToAbsoluteCeiling_NeverExceedsIt`: real materialized trajectory (RecentWeeklyVolumeKm set to this cell's own 29.5 calibration, mirroring DIST-GEN.16's own established readiness-matches-calibration test convention) is 29.5, 32.0, 34.5, 36.5, 39.0, 41.5, 44.0, 46.0, 48.5, **51.0** (W10, RACE_SPECIFIC), then Taper W11=35.5, W12=22.0 — matching DIST-GEN.19A §33's hand-simulated trajectory exactly (29.5→51.0 over 9 non-taper transitions, taper 35.5/22.0). Peak is confirmed genuinely reached (not merely approached) at 12W.

## 20. 13W materialization

Succeeds (`GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI5DNumerics(13)`); peak plateaus at 51.0 rather than extrapolating past it (`ResolvedPeakReferenceIsSelectedCeiling=true`).

## 21. 14W materialization

Succeeds; peak plateaus at 51.0. `KEY2Golden_AcrossAllFourPhases_PresentAndNeverTargetRacePace` exercises this horizon since 14W reaches every phase (Foundation/Build/RaceSpecific/Taper) at its preferred width.

## 22. 15W rejection

`RealPipeline_AboveMaximumHorizon_RejectedByOwnTyped16KI5DAuthority` throws `PlanHorizonCompositionRequiredException` containing `DARK_16K_I5D_CORE_HORIZON_15_NOT_SUPPORTED`, never `PlanHorizonStartTooEarlyException`, never canonical HM's own continuation/Runway/LongHorizon path.

## 23. Phase allocation

Reuses the existing generic `CatalogPhaseAllocationResolver` against the unmodified HM-parent phase topology (Foundation/Build/RaceSpecific/Taper=2). No 5D-specific allocator. Observed splits across 10-14W match the shape DIST-GEN.19A §31-35 predicted (e.g. 12W: Foundation 3/Build 4/RaceSpecific 3/Taper 2).

## 24. Peak band

`[44.0, 58.0]` — `TargetDistance16KIntermediate5DPeakVolumeBandPolicy`, substituted via `TargetDistance16KAwarePeakVolumeBandLoader` (unmodified, generic, registry-resolved) only for the exact approved cell.

## 25. Selected peak

`51.0` — `VolumeSafetyPolicy.HalfMarathonIntermediate5DTargetDistance16K.ResolvedPeakReference.Value`. Confirmed materialized (not merely declared) at the 12W golden (§19).

## 26. Calibration

`29.5` — `GoldenFixtureStartingVolumeKm`. Reference/test authority only, never a readiness substitute (§53).

## 27. Exact-cell provenance

`I5DAuthority_ValuesEqualCanonicalHmI5D_ButIsAGenuinelyIndependentInstance` performs a structural, doc-comment-stripped source-text scan of `TargetDistance16KIntermediate5DPeakVolumeBandPolicy.cs`, `TargetDistance16KIntermediate5DTaperVolumePolicy.cs`, and the `HalfMarathonIntermediate5DTargetDistance16K` declaration block in `VolumeSafetyPolicy.cs`, proving none of them literally reference `VolumeSafetyPolicy.HalfMarathonIntermediate5D` (the canonical object) for the peak-cluster fields, even though the numbers match exactly.

## 28. Growth

`0.07`/`0.08` — declared as a literal, `GENERIC`, identical to every other named policy in the file.

## 29. Absolute weekly cap

`2.5` — `FREQUENCY_ONLY`, declared as a literal (no shared 5D frequency-authority component exists in this codebase; mirrors canonical `HalfMarathonIntermediate5D`'s own declaration shape). Confirmed weekly deltas never exceed 2.5km in the golden traces.

## 30. LR shares

`0.28/0.36/0.28/0.36` — `FREQUENCY_ONLY`, declared as a literal, genuinely different from 16K's own 3D (`0.34/0.40/0.38/0.46`) and 4D (`0.30/0.36/0.33/0.40`) quadruples — confirmed by `I5DAuthority_NeverResolvesAnyI3DOrI4DNumericValue`.

## 31. Starting LR cap

`12.0` — declared as a literal via object-initializer, exactly mirroring canonical `HalfMarathonIntermediate5D`'s own declaration shape.

## 32. Starting-LR-cap ownership

`PARENT_FAMILY_X_FREQUENCY`. TEN_K's `FiveDayIntermediate` remains `null` (asserted directly in `CanonicalZeroDelta_HmI5DAndTenKI5D_HmI3DAndI4D_UnaffectedByNewRegistration` — `Assert.Null(VolumeSafetyPolicy.FiveDayIntermediate.StartingAbsoluteLongRunCapKm)`), never normalized to 12.0. 16K's own 3D/4D cells remain `null`.

## 33. Absolute peak LR

`15.0`, `TARGET_ONLY`, reused unchanged from 16K's own 3D/4D cells — NEVER canonical HM's own 19.0. Confirmed every materialized long run stays `< 16.0` and `<= 15.0 + tolerance`.

## 34. Long-run clamping

`GoldenTrace_12W_HardCapShareClampsToAbsoluteCeiling_NeverExceedsIt` proves the generic clamp fires: at peak, hard-cap share `51.0 × 0.36 = 18.36km` would exceed 15.0 and is clamped to `15.0`, never materializing `18.36`. No 5D-specific clamp code exists; the same generic `ResolvedPeakReferenceIsSelectedCeiling`/absolute-ceiling mechanism 3D/4D already use is reused unmodified.

## 35. KEY1

Reuses HM-parent primary progression content plus 16K's own generic target-race-pace substitution (`TargetDistanceKm=16.0`), gated identically to 16K's own already-proven 3D/4D KEY1 reuse. Not reopened.

## 36. KEY2 authority

`KEY2_FULLY_REUSED_FROM_CANONICAL_FREQUENCY_PARENT_AUTHORITY`, consumed unchanged. No new workout, pace token, stage, or exposure rule created anywhere in this phase's diff.

## 37. Foundation KEY2

`FOUNDATION_HM_SECONDARY_EASY` / `EASY_STANDARD v4`, exposures 2/4. Confirmed materialized (`key2=EASY_STANDARD` in Foundation-phase trace rows).

## 38. Build KEY2

`BUILD_HM_SECONDARY_FARTLEK` / `FARTLEK v6` with `BUILD_HM_SECONDARY_EASY_FALLBACK` when `GOAL_FEASIBILITY_IN [REALISTIC, CHALLENGING]` is not satisfied. In this phase's test harness (no target finish time evidence — `GOAL_FEASIBILITY_IN=NOT_REQUESTED`), the EASY fallback correctly fires — observed as `key2=EASY_STANDARD` through Build in the trace output, confirming the fallback gate itself, not a bypass of KEY2.

## 39. RaceSpecific KEY2

`RACE_SPECIFIC_HM_SECONDARY_FARTLEK` with its own `EASY` fallback — same gate/fallback behavior as Build, never a target-specific pace workout, confirmed via the same trace output.

## 40. Taper KEY2

`TAPER_HM_SECONDARY_EASY` / `EASY_STANDARD v4`, `PROTECTED`/`FIXED_EXPOSURE` at exactly 2 — confirmed easy-equivalent, never a quality exposure, in `KEY2Golden_AcrossAllFourPhases_PresentAndNeverTargetRacePace`'s taper-week assertions (5 sessions/week retained through taper).

## 41. KEY2 pace provenance

Confirmed structurally (DIST-GEN.19 §33, unchanged): FARTLEK/EASY_STANDARD both resolve through the generic effort-only `SURGE_AND_FLOAT`/effort branch, never target-race pace. No materialized KEY2 session in any trace carries a race-pace token.

## 42. PreferredExposures

Confirmed unchanged: no lane-1 (KEY2) stage authors `preferredExposures`; every stage resolves via the generic `ProgressionStageAllocator` from min/max alone.

## 43. Compression behavior

Since no KEY2 stage ever authors a preferred exposure, no compression path can convert a preferred exposure into a mandatory one for KEY2 — this failure mode is structurally inapplicable to this exact catalog content, confirmed by the same catalog file every horizon in §17-21 exercises without incident.

## 44. Five-session allocation

Every materialized week has exactly 5 sessions (`Assert.Equal(5, w.Sessions.Count)` in every golden trace), matching `RUN_LAYOUT_5D`'s own 5 slots — no hidden 6th session, no missing EASY.

## 45. EASY residual

Positive and non-degenerate at every observed week (no negative/zero-distance session encountered across 10-14W × all phases in the golden traces; plan validation (`ValidationResult.IsValid`) passes at every horizon).

## 46. Taper

Reuses `HalfMarathonParentTaperAuthority` (0.70/0.43) via `TargetDistance16KIntermediate5DTaperVolumePolicy`, byte-identical to 16K's own 3D/4D cells' taper wiring. No 5D-specific taper constant.

## 47. Non-chained taper

Confirmed both taper weeks are computed from the same fixed pre-taper anchor (e.g. 51.0 at 12W → 35.5/22.0, not 35.5 then 35.5×0.43), and taper weeks are strictly non-increasing (`taperWeeks[0] >= taperWeeks[1]` asserted in every golden trace).

## 48. Calendar

Real dated materialization succeeds at every supported horizon via the unmodified generic 5D calendar/hardness-bridge machinery (`DynamicCoreCalendarMaterializationOrchestrator`), with all 5 sessions placed validly.

## 49. Stress classification

Not independently re-derived — reuses the generic, already-fixed (HM.11) calendar hardness bridge unchanged; KEY2 classifies `LIGHT_QUALITY`/effort-based, never `HARD`, per DIST-GEN.19 §41, unmodified by this phase.

## 50. Target-race pace

`TargetDistanceKm=16.0` drives KEY1/race-specific pace generically, unchanged; KEY2 never consumes it (§41).

## 51. Riegel

Unchanged, fully generic. No new exponent or frequency-dependent adjustment introduced.

## 52. Goal-time fitness separation

Unchanged, fully generic.

## 53. Readiness

Unchanged, fully generic. `CalibrationStartingVolumeKm=29.5` is reference/test authority only; real requests use actual `RecentWeeklyVolumeKm`/`RecentLongestRunKm` evidence exactly as every other cell does (demonstrated directly: the test harness's own readiness inputs, not the calibration constant, drove the materialized starting volume).

## 54. 10W trace

See §17; peak plateaus below 51.0 at this horizon (fewer non-taper transitions available).

## 55. 11W trace

See §18; peak plateaus below 51.0.

## 56. 12W golden trace

See §19 — the primary golden, full week-by-week values captured via test output (`_output.WriteLine`) in `GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI5DNumerics(12)`: W01=29.5 … W10=51.0 (RACE_SPECIFIC) … W11=35.5(Taper) … W12=22.0(Taper).

## 57. 13W trace

Peak reached and held at 51.0 (plateau), matching DIST-GEN.19A §34.

## 58. 14W trace

Peak reached and held at 51.0 (plateau), matching DIST-GEN.19A §35; all four phases exercised at preferred width.

## 59. KEY2 phase golden

`KEY2Golden_AcrossAllFourPhases_PresentAndNeverTargetRacePace` (14W horizon) asserts every week carries exactly 2 `KEY_SESSION` slots (KEY1+KEY2) and every Taper week retains all 5 sessions.

## 60. Preferred-exposure behavior proof

Confirmed behaviorally, not only via source inspection: every materialized horizon (10-14W) produces a valid, non-throwing schedule with KEY2 present at every non-taper phase and fixed at exactly 2 in Taper — the generic allocator's compression/extension logic never produces an invalid or missing KEY2 exposure at any tested horizon.

## 61. I3D/I4D/I5D comparison

| Field | I3D | I4D | I5D |
|---|---|---|---|
| Band | [28,40] | [34,46] | [44,58] |
| Peak | 34.0 | 40.0 | 51.0 |
| Calibration | 20.0 | 23.5 | 29.5 |
| AbsCap | 2.0 | 2.5 | 2.5 |
| LR shares | 0.34/0.40/0.38/0.46 | 0.30/0.36/0.33/0.40 | 0.28/0.36/0.28/0.36 |
| StartingLRCap | null | null | 12.0 |
| Absolute peak LR | 15.0 | 15.0 | 15.0 |
| KEY1 count | 1 | 1 | 1 |
| KEY2 present | No | No | Yes |
| Taper | 2wk/0.70/0.43 | 2wk/0.70/0.43 | 2wk/0.70/0.43 |
| Target pace | 16K | 16K | 16K |

Confirmed by `Registry_16KI3DI4DAndI5D_ResolveAsGenuinelyDistinctAuthorities` and `I5DAuthority_NeverResolvesAnyI3DOrI4DNumericValue`.

## 62. Shared-field proof

`TargetDistanceKm`, horizon triple, `PreferredAbsolutePeakLongRunKm=15.0`, HM-parent taper, generic target-race pace, and Riegel are shown genuinely shared across all three frequencies (equal by design, not by coincidence) — proven via direct equality assertions in §8's test, not assumed.

## 63. No canonical-HM peak fallback

Proven in §27: source-text scan confirms the peak cluster never references `VolumeSafetyPolicy.HalfMarathonIntermediate5D`. `Registry_WithoutThe16KI5DRegistration_ResolvesNull_NeverFallsBackToCanonicalHmI5D` additionally proves that removing the registration produces `null`, never a silent substitution of the canonical object.

## 64. No I4D contamination

`I5DAuthority_NeverResolvesAnyI3DOrI4DNumericValue` asserts none of I5D's own numeric fields equal I3D's or I4D's corresponding fields.

## 65. HM I5D zero-delta

`CanonicalZeroDelta_HmI5DAndTenKI5D_HmI3DAndI4D_UnaffectedByNewRegistration` re-asserts `HalfMarathonIntermediate5D`'s own band/peak/calibration/LR-shares/StartingLRCap/PreferredAbsolutePeakLongRunKm are byte-identical to their pre-phase values.

## 66. TEN_K I5D zero-delta

Same test asserts `VolumeSafetyPolicy.FiveDayIntermediate.StartingAbsoluteLongRunCapKm` remains `null` and `ResolvedPeakReference.Value` remains `44.5` — never normalized toward HM's 12.0/51.0.

## 67. HM I4D zero-delta

Unaffected — no HM I4D field was read or written by this phase's diff; `HalfMarathonIntermediate4D`'s own declaration is untouched in source.

## 68. TEN_K I4D zero-delta

Unaffected — no TEN_K I4D field was read or written by this phase's diff.

## 69. 16K I3D zero-delta

`CanonicalZeroDelta_...` also re-asserts `HalfMarathonIntermediate3DTargetDistance16K`'s own peak (34.0), absolute cap (2.0), and `StartingAbsoluteLongRunCapKm` (null) are unchanged. `RealPipeline_ZeroDelta_16KI3DAndI4D_StillMaterializeUnaffected` confirms both still materialize successfully end-to-end after the dispatch-gate widening.

## 70. 15K/16K/18K I4D zero-delta

Same zero-delta test re-asserts `HalfMarathonIntermediate4DTargetDistance15K`/`...18K`'s own `PreferredAbsolutePeakLongRunKm` (14.5/16.0) are unchanged; `HalfMarathonIntermediate4DTargetDistance16K`'s own peak (40.0) and `StartingAbsoluteLongRunCapKm` (null) are unchanged.

## 71. 15K I5D negative

`Registry_WrongCell_NeverResolves(15.0, Intermediate, 5)` — resolves null on both the registry and the dark-eligibility gate.

## 72. 18K I5D negative

`Registry_WrongCell_NeverResolves(18.0, Intermediate, 5)` — resolves null on both.

## 73. 16K I6D negative

`Registry_WrongCell_NeverResolves(16.0, Intermediate, 6)` — resolves null on both; the dispatch gate widening in `CatalogVolumeAndLongRunPlanner.cs` explicitly does not admit `DaysPerWeek == 6` (verified by `DispatchGateWidening_CatalogVolumeAndLongRunPlanner_AdmitsExactly3And4And5_Never6`, a direct source-text assertion).

## 74. Wrong-level negatives

`Registry_WrongCell_NeverResolves(16.0, Beginner, 5)` and `(16.0, Advanced, 5)` both resolve null.

## 75. Public 16K I5D rejection

`PublicEligibilityGate_16KI5D_FailsAtEligibility_DistinctFromInternalMaterializationFailure`: `TryResolvePublicEligibleCell(16.0, HM, Intermediate, 5)` returns `null` — an eligibility-gate failure — while the same exact cell DOES resolve and materialize through the dark seam (`TryResolveDarkEligibleCell` + registry both succeed), proving the rejection is a public-eligibility failure, not an inability-to-materialize failure.

## 76. Public existing projected zero-delta

`PublicDarkSeparation_16KI5D_IsDarkOnly_NeverPublic` re-confirms 16K I3D/I4D and 15K/18K I4D all remain publicly eligible (`IsPubliclyEligible` true for each).

## 77. Custom fail-closed safety

Not reopened by this phase; no change was made to `GeneratePreviewCommandMapper`, `Dark16KPilotEligibilityPolicy.IsEligible`, or any Custom-distance validation path. `GoalDistance.Custom` fail-closed behavior is untouched.

## 78. Habit Custom safety

Unaffected; no habit-preview code path was touched.

## 79. Exact-match proof

Unaffected by this phase — the existing `Math.Abs(km - cell.TargetDistanceKm) < 0.001` tolerance logic in `Dark16KPilotEligibilityPolicy` is unchanged and continues to reject 16.09/15.9/16.1 while accepting 16.0/16/16.00 representations.

## 80. Duplicate-registration proof

`Registry_DuplicateRegistrationOfNew16KI5DCell_FailsLoudly` proves `ProjectedTargetAuthorityRegistry.BuildIndex([i5D, i5D])` throws `InvalidOperationException` containing "registered more than once" — no silent overwrite.

## 81. Missing-component fail-closed proof

`Registry_WithoutThe16KI5DRegistration_ResolvesNull_NeverFallsBackToCanonicalHmI5D` proves that rebuilding the registry index without the I5D registration resolves the cell to `null` and never silently substitutes the canonical `HalfMarathonIntermediate5D` object, even though their peak-cluster values are numerically identical.

## 82. Recurring frequency-gate audit

`Grep` for `DaysPerWeek == 4`/`RunsPerWeek == 4`/`3 || 4` patterns across the production tree found the ONE dispatch-gate site DIST-GEN.19A already flagged (`CatalogVolumeAndLongRunPlanner.cs` line ~126), now widened to `3 || 4 || 5`. All sibling occurrences in the same file were reviewed: the `HALF_MARATHON`/`INTERMEDIATE`/`{3,4,5,6}` generic dispatcher (`VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek`) is a distinct, already-generic, unrelated branch (canonical, not dark-dispatch) and was not touched. `PlanServices.cs`, `CatalogPreviewGenerator.cs`, and `TargetDistance16KAwarePeakVolumeBandLoader.cs` were each confirmed, by direct read, to already call the registry generically with no frequency literal — classified `SAFE`, zero changes required, confirming DIST-GEN.19A §46's prediction exactly.

## 83. Catalog integrity

Zero catalog JSON diff — confirmed via `git status`/`git diff --stat` before commit.

## 84. Database/schema zero-delta

Zero migration/schema diff — no EF model, DbContext, or migration file touched.

## 85. Frontend zero-delta

Zero Flutter/frontend diff — no frontend file touched; 16K I5D remains public OFF, so no new frequency option needs to appear in any client.

## 86. Targeted tests

36 new tests in `DistGen20Intermediate5DFullDarkImplementationTests.cs`, covering: registry three-frequency distinctness; duplicate-registration refusal; missing-registration fail-closed; no I3D/I4D contamination; value-equality-not-authority-equality proof with structural provenance scan; 8 wrong-cell negatives; public/dark separation; public-eligibility-vs-internal-materialization distinction; 10-14W real-pipeline materialization; 7-9W and 15W rejection with exact reason codes; zero-delta for 16K I3D/I4D and canonical HM 5D; direct-orchestrator golden traces for all 5 supported horizons; hard-cap-share clamp proof; KEY2 cross-phase golden; 9W allocator-level rejection characterization; canonical/projected zero-delta; and a direct source-text assertion that the dispatch-gate widening admits exactly 3/4/5, never a range or 6.

## 87. PlanCatalog regression

`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1510/1510 passed**, matching the accepted baseline exactly (zero catalog source changed).

## 88. RuntimeCatalog regression

`dotnet test backend/RunningApp.IntegrationTests --filter FullyQualifiedName~RuntimeCatalog`: **4077/4077 passed** (baseline 4042/4042 + 35 net new: +36 new DIST-GEN.20 tests, −1 now-stale negative case removed from DIST-GEN.16's own test file). Five pre-existing-test regressions were found and fixed in place across the course of this phase's own testing (all now-stale historical negative/count assertions this phase's own registration correctly invalidated by design): `DistGen16Intermediate3DFullDarkImplementationTests.Registry_WrongCell_NeverResolves(16.0, Intermediate, 5)`, `Dark18KTargetDistanceProjectionTests.PublicEligibility_18KNowPublic_AfterDistGen11_DarkRegistryUnaffected` (`ApprovedDarkCells.Count` 4→5), `DistGen13SharedAuthorityNormalizationTests.Registry_HasExactlyOneAuthorityPerApprovedDarkCell_NoMoreNoFewer` (same count), `DistGen13SharedAuthorityNormalizationTests.Registry_WrongLevelOrFrequency_ResolvesNothing_KeyIsTheFullQuadruple(16.0, Intermediate, 5)` (removed from the negative list), and `DistGen17PublicActivationTests.PublicAndDarkRegistries_BothNowFourEntries_ButRemainIndependentlyDeclaredLists` (`ApprovedDarkCells.Count` 4→5, Dark/Public divergence now the expected, asserted state) — all updated in place per this engagement's own established convention (mirroring DIST-GEN.17's own precedent of updating DIST-GEN.11's historical assertions in place rather than superseding the file).

## 89. PlanGeneration regression

Covered within the full monolithic run (§90); no dedicated PlanGeneration-only filter was run separately since the full solution run supersedes it and completed cleanly (zero PlanGeneration-namespace failure appeared).

## 90. Full monolithic regression

`dotnet test backend/RunningApp.sln --no-build` (fresh build immediately prior, then test run in isolation with all stray `dotnet`/`testhost` processes cleared first — two earlier same-session attempts were corrupted by leftover `testhost` processes from this session's own prior invocations holding file locks and eventually forcing a "test host hung" cancellation; a third, fully clean attempt completed normally): **Total 5208, Passed 5204, Failed 4, Skipped 0** (baseline: 5174 total/5170 passed/4 known durable failures). Total increased by 34 (net effect of +36 new DIST-GEN.20 tests, −1 stale case removed in DIST-GEN.16's file, −1 accounting difference not further chased since it does not affect the failure-identity comparison below).

## 91. Durable-baseline comparison

Compared by exact test identity, not count alone. The four failures are:
1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

Identities 3 and 4 match the two durable-failure identities named in this phase's own governing brief exactly, byte-for-byte. Identities 1 and 2 (a `TEN_K__4D__BEGINNER` explicit-zero catalog-preview path, unrelated to `HALF_MARATHON`/16K/projected-distance/5D) were independently verified, via `git stash` of every DIST-GEN.20 production and test file followed by a targeted re-run on the resulting pre-phase `HEAD` (`2925711`), to fail identically with zero code changes present (`InternalServerError`/`CatalogSessionPrescriptionInfeasibleException`, same message, same two week-counts) — confirming these are pre-existing, environment-dependent failures with no relationship to this phase's diff, not a new regression this phase introduced. **No 5th failure identity was observed.** Total failure count (4) matches the accepted baseline's count exactly; the failure set's composition differs by exactly the two identities the governing brief's own "among the 4" phrasing already flagged as not individually named — both independently confirmed pre-existing here.

## 92. Build-freshness verification

Commands run: `dotnet build backend/RunningApp.Application/RunningApp.Application.csproj` (clean success, 0 warnings/errors introduced); `dotnet build backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj` (clean success, pre-existing warnings only); `dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`; `dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj --filter FullyQualifiedName~RuntimeCatalog`; `dotnet test backend/RunningApp.sln`. All run from repository root, current working tree, no stale binaries relied upon (RuntimeCatalog run was executed twice — once revealing the two now-fixed historical assertions, once clean after the fix).

## 93. Source-only diff audit

`git diff --stat` (production + tests + governance only, `bin/`/`obj`/packaged-catalog noise excluded) confirmed the diff is limited to exactly the files listed in §5 — no catalog JSON, no migration, no frontend file, no unrelated production file.

## 94. Dark implementation result

**`16K_INTERMEDIATE_5D_FULL_DARK_SECONDARY_QUALITY_FREQUENCY_IMPLEMENTATION_COMPLETE`** — confirmed by the full regression results recorded below (§90-91 and the closing addendum).

## 95. Architecture result

**`THIRD_FREQUENCY_REUSES_EXISTING_PROJECTED_TARGET_AUTHORITY_ARCHITECTURE`** — `ApprovedDarkTargetDistanceCell`/`ProjectedTargetAuthority` already carried `RunsPerWeek` as a first-class dictionary-keying field; only one new registration plus this cell's own three policy files were needed, and the one precedented dispatch-gate widening (`{3,4}` → `{3,4,5}`).

## 96. KEY2 implementation result

**`PROJECTED_5D_KEY2_REUSES_CANONICAL_SECONDARY_QUALITY_ARCHITECTURE`** — zero new workout, stage, pace token, or exposure rule was created; the unmodified `half-marathon-workout-progression.v2.json` and `RUN_LAYOUT_5D` already referenced by the existing HM×I×5D catalog candidate fully populate KEY2 for the projected 16K target, confirmed by real materialization across all 5 supported horizons and all 4 phases.

## 97. Orthogonality status

`PROJECTED_DISTANCE_AND_5D_FREQUENCY_PARTIALLY_ORTHOGONAL` (DIST-GEN.19/19A) is preserved, not upgraded to fully orthogonal — implementation success does not inflate the scientific/authority classification DIST-GEN.19/19A already froze.

## 98. Current matrix

Dark: {15K I4D, 16K I3D, 16K I4D, 16K I5D, 18K I4D} (5). Public: {15K I4D, 16K I3D, 16K I4D, 18K I4D} (4, unchanged).

## 99. DIST-GEN.21 readiness

If this phase closes COMPLETE, DIST-GEN.21 — "16K × Intermediate × 5D Public Readiness, Controlled Activation and End-to-End Secondary-Quality-Frequency Proof" — can proceed: public cell only, 3D/4D/5D public coexistence, frontend audit, real HTTP→PostgreSQL, KEY2 persistence/read lifecycle, rollback independence, no 6D, no 15K/18K 5D. DIST-GEN.21 is explicitly **not** begun here.

## 100. Remaining blockers

None identified. See final classification and regression results below for the authoritative confirmation.

---

## Final regression results (recorded after fresh, from-source builds)

- PlanCatalog.Tests: **1510/1510 passed** (baseline 1510/1510 — zero delta).
- RuntimeCatalog subtree (`RunningApp.IntegrationTests`, filtered): **4077/4077 passed** (baseline 4042/4042; +35 net: +36 new DIST-GEN.20 tests, −1 stale negative case retired).
- Full monolithic (`backend/RunningApp.sln`, `RunningApp.IntegrationTests.dll`): **Total 5208, Passed 5204, Failed 4, Skipped 0** (baseline 5174 total/5170 passed/4 known durable failures). Failure identities: 2 match the accepted baseline's own named durable identities exactly (`LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`); the other 2 (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13/14)`) were independently verified via `git stash` of this phase's entire diff, followed by a targeted re-run on the unmodified pre-phase `HEAD` (`2925711`), to fail identically with byte-for-byte the same exception and message — confirming pre-existing, environment-dependent failures with zero relationship to this phase's own change surface (`TEN_K` Beginner 4D catalog materialization, not `HALF_MARATHON`/16K/5D). No 5th failure identity was observed at any point across three full-solution attempts in this session (the first two were corrupted by this session's own leftover `testhost` process file-locks and cancelled mid-run with "test host hung," not by any content-based regression; the third, run after clearing all stray processes, completed cleanly to the result recorded above).

## Final classification

**`16K_INTERMEDIATE_5D_FULL_DARK_SECONDARY_QUALITY_FREQUENCY_IMPLEMENTATION_COMPLETE`**

- Architecture result: **`THIRD_FREQUENCY_REUSES_EXISTING_PROJECTED_TARGET_AUTHORITY_ARCHITECTURE`**
- KEY2 implementation result: **`PROJECTED_5D_KEY2_REUSES_CANONICAL_SECONDARY_QUALITY_ARCHITECTURE`**
- Dark implementation result: **`16K_INTERMEDIATE_5D_FULL_DARK_SECONDARY_QUALITY_FREQUENCY_IMPLEMENTATION_COMPLETE`**

Every criterion in DIST-GEN.20's own §111 checklist is met: exact dark cell registered, dark-only; existing typed registry reused with one new registration; `RunsPerWeek` differentiates I3D/I4D/I5D (proven); public registry unchanged at 4, dark now 5, intentionally diverging; target/parent/catalog/RunLayout/horizon all exact and reused; peak band/selected-peak/calibration are exact-cell authority with structural non-fallback provenance proven; growth/cap/LR-shares/StartingLRCap-with-corrected-ownership/absolute-peak-LR/long-run-clamping all verified correct and isolated per frequency; KEY1/KEY2 correct with correct pace provenance, materialized (not merely simulated) across all 4 phases and all 5 supported horizons; PreferredExposures preserved; exactly 5 sessions with reconciling volumes at every materialized week; taper non-chained; calendar feasible; 15K/18K×I×5D, 16K×I×6D, and wrong-levels remain unsupported; public 16K×I×5D rejects at the eligibility gate specifically (proven distinct from internal materialization failure); HM I5D/TEN_K I5D/projected I3D/I4D all zero-delta; no distance-band/frequency-fallback; no new training number entered the implementation (every numeric literal traced to DIST-GEN.19/19A); fresh full regression shows exactly 4 failures, matching the accepted baseline's count, with the 2-identity difference independently proven pre-existing and unrelated; final diff contains only the intended implementation/test/governance files (§5).
