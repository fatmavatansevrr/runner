# PHASE DIST-GEN.16 — 16K × Intermediate × 3D Full Dark Projected-Distance Frequency-Orthogonality Implementation

## 1. DIST-GEN.15B authority consumed

DIST-GEN.15B's own complete implementation-ready authority table (§7) is the binding source for every number in this phase: `PeakVolumeBandMin=28.0`, `PeakVolumeBandMax=40.0`, `SelectedPeakReference=34.0`, `CalibrationStartingVolumeKm=20.0` (all four EXACT-CELL `NEW_TARGET_FREQUENCY_CELL_AUTHORITY`, closed in DIST-GEN.15B itself), plus every field DIST-GEN.15 §57 already closed and DIST-GEN.15B re-confirmed unchanged: `MinimumCoreWeeks=10` (parent), `PreferredCoreWeeks=12`/`MaximumCoreWeeks=14` (target), `AbsoluteWeeklyIncreaseCapKm=2.0` (frequency), LR-share quadruple `0.34/0.40/0.38/0.46` (parent), `PreferredAbsolutePeakLongRunKm=15.0` (target), `StartingAbsoluteLongRunCapKm=null` (frequency), taper `2wk/0.70/0.43/non-chained` (parent), `RunLayout=[KEY_SESSION,EASY_SUPPORT,LONG_RUN]` (frequency), and every generic mechanism (growth rates, target-race-pace, Riegel, goal-feasibility, readiness, calendar, adaptation). No number in this implementation was derived, adjusted, or re-decided — every value is a direct, traceable consumption of DIST-GEN.15/15B's own authority tables.

## 2. Repository baseline

Starting branch `main`, working tree carrying only pre-existing tracked `bin/`/`obj/` build-artifact noise (per the conversation's own git-status snapshot) plus the standard `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` auto-regeneration (reverted via `git checkout --` after each `PlanCatalog.Tests` run, per established DIST-GEN.13 precedent). Fresh `dotnet build backend/RunningApp.sln --no-incremental` confirmed 0 errors / 16 pre-existing warnings BEFORE this phase's own changes, matching the DIST-GEN.13-recorded baseline exactly.

## 3. Pre-implementation characterization

Confirmed by direct source read (not memory) before writing any code: `Dark16KPilotEligibilityPolicy.ApprovedDarkCells` held exactly 3 entries (15.0/16.0/18.0, all HalfMarathon×Intermediate×4D); `ProjectedTargetAuthorityRegistry` held exactly 3 registrations, one per approved dark cell, via a `Dictionary<ApprovedDarkTargetDistanceCell, ProjectedTargetAuthority>` keyed on the full `(TargetDistanceKm, ParentDistanceFamily, Level, RunsPerWeek)` quadruple — confirming DIST-GEN.15 §62's own claim that `RunsPerWeek` was already a first-class key field requiring zero type change. `CatalogVolumeAndLongRunPlanner.cs`'s own projected-target dispatch branch (the one call site DIST-GEN.13 did NOT fully generalize) was found hardcoded to `request.Candidate.DaysPerWeek == 4` — a genuine, previously-undetected `HIDDEN_4D_ASSUMPTION` that would have made the new 16K I3D cell structurally unreachable at that one site even after registration (see §67).

## 4. Files changed

**New (5):**
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/IntermediateThreeDayFrequencyAuthority.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KIntermediate3DPeakVolumeBandPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KIntermediate3DTaperVolumePolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/TargetDistance16KIntermediate3DHorizonPolicy.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/DistGen16Intermediate3DFullDarkImplementationTests.cs`

**Modified (5):**
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` (new `HalfMarathonIntermediate3DTargetDistance16K` instance)
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` (one new `ApprovedDarkCells` entry; `ApprovedPublicCells` untouched)
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/ProjectedTargetAuthorityRegistry.cs` (one new registration; doc-comment count corrected)
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` (widened the one hardcoded `DaysPerWeek == 4` dispatch gate to `(3 || 4)` — see §67)
- `backend/RunningApp.IntegrationTests/DistGen13SharedAuthorityNormalizationTests.cs` (bumped the dark-cell-count assertion from 3→4; added one duplicate-registration extension test for the new cell)

No catalog JSON, migration, or Flutter file touched. No `.csproj` changed.

## 5. Current projected matrix

| Cell | Dark | Public |
|---|---|---|
| 15.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 16.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 18.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| **16.0 × HalfMarathon × Intermediate × 3D** | **Yes (this phase)** | **No** |

## 6. Exact 16K I3D cell registration

`ProjectedTargetAuthorityRegistry`'s registration list now carries a 4th entry: `Cell = new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3)`, `PilotLabel = "dark 16K Intermediate 3D"`, composed from `TargetDistance16KIntermediate3DHorizonPolicy` (horizon), `VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K` (volume/taper/LR), and `TargetDistance16KIntermediate3DPeakVolumeBandPolicy` (band). `Dark16KPilotEligibilityPolicy.ApprovedDarkCells` carries the matching 4th entry. `ProjectedTargetAuthorityRegistry.All.Count == 4 == ApprovedDarkCells.Count`, confirmed by `Registry_HasExactlyOneAuthorityPerApprovedDarkCell_NoMoreNoFewer` (updated) and the new file's own `Registry_16KI3DAndI4D_ResolveAsGenuinelyDistinctAuthorities`.

## 7. Same-distance cross-frequency registry proof

`Registry_16KI3DAndI4D_ResolveAsGenuinelyDistinctAuthorities` proves `(16.0, HalfMarathon, Intermediate, 4)` and `(16.0, HalfMarathon, Intermediate, 3)` resolve as two distinct `ProjectedTargetAuthority` objects (`ReferenceEquals` false on `VolumeSafetyPolicy`) with different peak-volume band (`[34,46]` vs `[28,40]`), different selected peak (`40.0` vs `34.0`), different calibration (`23.5` vs `20.0`), different absolute cap (`2.5` vs `2.0`), different LR-share quadruple (`0.30/0.36/0.33/0.40` vs `0.34/0.40/0.38/0.46`) — but identical `TargetDistanceKm` (16.0), identical `PreferredCoreWeeks`/`MaximumCoreWeeks`/`MinimumCoreWeeks`, and identical `PreferredAbsolutePeakLongRunKm` (15.0 both). `RunsPerWeek` is confirmed the discriminating field (3 vs 4).

## 8. Public registry zero-delta

`ApprovedPublicCells` is byte-identical to before this phase: 3 entries, all `RunsPerWeek == 4` (confirmed by `PublicRegistry_Never16KI3D_DarkOnly`, which also asserts `Dark16KPilotEligibilityPolicy.IsPubliclyEligible(16.0, HalfMarathon, Intermediate, 3)` is `false` while the 4D grant remains `true`). Confirmed via `git status --porcelain` (see §4): the diff touches `ApprovedDarkCells` only.

## 9. Structural parent

Unchanged: `HALF_MARATHON`, per DIST-GEN.15 §11 — reconfirmed, not reopened.

## 10. Exact target identity

`TargetDistanceKm = 16.0`. Its own `PreferredAbsolutePeakLongRunKm` (15.0) and horizon triple (10/12/14) are reused unchanged from the 4D cell — a demonstrated frequency-invariance finding (DIST-GEN.15 §31-33), never a coincidence "fixed" into a shared value.

## 11. Catalog candidate

`plan-catalog/catalog/combinations/half-marathon-3d-intermediate.v1.json` — confirmed unmodified, reused exactly as DIST-GEN.15 §12 found it (`masterTemplate: HALF_MARATHON_MASTER v1`, `layout: RUN_LAYOUT_3D v1`, `levelModifier: INTERMEDIATE_MODIFIER v8`, `rulePack: APPSEL_RACE_PLAN_V1 v10`). Zero catalog file touched by this phase (confirmed by the clean `git status` in §4).

## 12. RunLayout

`plan-catalog/catalog/layouts/run-layout-3d.v1.json` — confirmed: `runsPerWeek: 3`, slots exactly `[KEY_SESSION, EASY_SUPPORT, LONG_RUN]`. Every golden-trace test in the new suite asserts exactly 3 sessions/week with exactly one of each role, zero KEY2/secondary-quality lane (`GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI3DNumerics`).

## 13. Horizon policy

`TargetDistance16KIntermediate3DHorizonPolicy`: `MinimumCoreWeeks=10` (references `RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks`, the same parent-family constant every sibling horizon policy uses), `PreferredCoreWeeks=12`/`MaximumCoreWeeks=14` (referenced directly from `TargetDistance16KHorizonPolicy`'s own constants — target-owned, frequency-invariant, never re-declared as an independent literal). Own distinct reason code: `DARK_16K_I3D_CORE_HORIZON_{weeks}_NOT_SUPPORTED`.

## 14. 9W lower-bound rejection

`RealPipeline_BelowMinimumHorizon_RejectedByOwnTyped16KI3DAuthority` (7/8/9W) proves the real `PlanServices.GeneratePreviewAsync` production path throws `PlanCoreHorizonTooShortException` with message containing `DARK_16K_I3D_CORE_HORIZON_{weeks}_NOT_SUPPORTED` and `"dark 16K Intermediate 3D"`, and explicitly asserts absence of `TARGET_BELOW_SUM_OF_MINIMUMS`, HM's own `"HALF_MARATHON standalone core minimum (10"` text, and 16K I4D's own `DARK_16K_CORE_HORIZON` (without the `_I3D_` infix) reason code. `NineWeekHorizon_RejectedTypedNot4DOrHmFallback` additionally confirms the low-level allocator's own `TARGET_BELOW_SUM_OF_MINIMUMS` exists as a distinct, deeper-layer fact that production never surfaces for this cell (the typed horizon gate intercepts first).

## 15. 10W materialization

`GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI3DNumerics(10)` and `RealPipeline_EligibleHorizons_MaterializeSuccessfully(10)` both pass: 10 weeks, phase split `2/3/3/2` (frequency-generic, identical to HM I3D's own and to 16K I4D's own at this same horizon), exactly 3 sessions/week, peak volume ≤34.0, LR ≤15.0/≤0.46 share, non-chained 2-week taper, weekly deltas ≤2.0km.

## 16. 11W materialization

Same theory, `weeks=11`: phase split `2/3/4/2`, passes with identical invariants.

## 17. 12W preferred golden

`weeks=12` (this cell's own `PreferredCoreWeeks`): phase split `2/3/5/2`. Additionally exercised by `CrossFrequencyTrace_16KI3DAndI4D_At12W_ShareIdentityButDifferAuthority` (§45) and by the real-HTTP-equivalent `RealPipeline_EligibleHorizons_MaterializeSuccessfully(12)`.

## 18. 13W materialization

`weeks=13`: phase split `2/4/5/2`. Passes with identical invariants; peak volume still bounded at ≤34.0 (fewer than 11 non-taper transitions at this horizon relative to the calibration point, so no forced extrapolation).

## 19. 14W materialization

`weeks=14` (this cell's own `MaximumCoreWeeks`): phase split `3/4/5/2`. `GoldenTrace_14W_HardCapShareClampsToAbsoluteCeiling_NeverExceedsIt` specifically exercises the hard-cap-share boundary (§31).

## 20. 15W upper-bound rejection

`RealPipeline_AboveMaximumHorizon_RejectedByOwnTyped16KI3DAuthority` proves 15W throws `PlanHorizonCompositionRequiredException` containing `DARK_16K_I3D_CORE_HORIZON_15_NOT_SUPPORTED`, and is explicitly not a `PlanHorizonStartTooEarlyException` (HM's own >16W contract, structurally unreachable here since this cell's own typed authority intercepts first).

## 21. Phase allocation

Identical to 16K I4D's own and to canonical HM I3D's own phase-allocation pattern at every shared horizon (10W→2/3/3/2 through 14W→3/4/5/2), since the underlying catalog phase topology (HALF_MARATHON_MASTER's Foundation/Build/RaceSpecific/Taper split) is frequency-invariant, per DIST-GEN.15 §14/§18 (reconfirmed here, not re-derived).

## 22. Stage occupancy

Not independently re-verified stage-key-by-stage-key in this phase (out of DIST-GEN.16's own required-proofs scope); the existing HM I3D vertical-slice suite (`Hm8Intermediate3DFullDarkVerticalSliceTests`, unmodified) already exercises the identical frequency-generic stage-key sequencing this cell's own materialization reuses unchanged via the same catalog progression.

## 23. Peak band

`TargetDistance16KIntermediate3DPeakVolumeBandPolicy`: `MinimumKm=28.0`, `MaximumKm=40.0` — a distinct, independently-declared C# constant (never a catalog JSON row), exactly mirroring the pattern every sibling `TargetDistanceNNKPeakVolumeBandPolicy` class already uses. Confirmed distinct from 16K I4D's own `[34,46]` (`Registry_16KI3DAndI4D_ResolveAsGenuinelyDistinctAuthorities`) and confirmed never referencing canonical HM I3D's own catalog-loaded band (`I3DAuthority_ValuesEqualCanonicalHmI3D_ButIsAGenuinelyIndependentInstance`'s structural source-text check).

## 24. Selected peak

`ResolvedPeakReference = 34.0` (exact midpoint of `[28,40]`), declared directly on the new `VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K` instance as its own exact-cell literal — never a reference to `VolumeSafetyPolicy.HalfMarathonIntermediate3D`.

## 25. Calibration

`GoldenFixtureStartingVolumeKm = 20.0` — declared directly as this cell's own literal (numerically equal to canonical HM I3D's own value, per DIST-GEN.15B §3-4's own explicit adoption, but never a reference to that canonical object).

## 26. Growth authority

`PreferredWeeklyIncreaseRate=0.07`/`HardWeeklyIncreaseRate=0.08` declared as plain literals directly on the new instance — `GENERIC`, universal, matching exactly how canonical `HalfMarathonIntermediate3D` and `ThreeDayIntermediate` both declare these same two fields (never routed through `IntermediateFourDayGrowthAuthority`, which is explicitly and permanently scoped to 4D only).

## 27. Absolute weekly cap

`AbsoluteWeeklyIncreaseCapKm = 2.0`, referenced from the new `IntermediateThreeDayFrequencyAuthority.AbsoluteWeeklyIncreaseCapKm` shared component — deliberately different from 16K I4D's own `2.5` (`IntermediateFourDayGrowthAuthority.AbsoluteWeeklyIncreaseCapKm`). Confirmed by `Registry_16KI3DAndI4D_ResolveAsGenuinelyDistinctAuthorities` and by the golden-trace weekly-delta assertions (≤2.0km, never 2.5km).

## 28. LR-share authority

`0.34/0.40/0.38/0.46` — declared as this cell's own literal quadruple (parent-family authority per DIST-GEN.15 §30, reused directly from HALF_MARATHON's own already-closed Intermediate×3D value). No shared 3D LR-share component was created for this, per the explicit instruction not to bundle parent-family LR-share into any frequency-scoped shared component, and because none already exists in the codebase for canonical HM I3D itself to reference (it too declares this quadruple as its own literal).

## 29. Absolute peak LR

`PreferredAbsolutePeakLongRunKm = 15.0`, reused directly, unchanged, from 16K I4D's own value — `TARGET_ONLY`, a demonstrated frequency-invariance finding (DIST-GEN.15 §31-32), confirmed identical in §7's cross-frequency proof.

## 30. Starting LR cap

`StartingAbsoluteLongRunCapKm = null`, referenced from `IntermediateThreeDayFrequencyAuthority.StartingAbsoluteLongRunCapKm` — mirrors 16K I4D's own null (`IntermediateFourDayGrowthAuthority.StartingAbsoluteLongRunCapKm`), consistent with this field being non-null only at 5D/6D.

## 31. Long-run feasibility

`GoldenTrace_14W_HardCapShareClampsToAbsoluteCeiling_NeverExceedsIt` directly exercises DIST-GEN.15B §6's own analytically-verified clamp case: at the 34.0km peak, the hard-cap share (0.46) would yield `34.0 × 0.46 = 15.64km`, which must clamp to the 15.0km absolute ceiling. The test asserts every materialized long-run value stays `≤ 15.0 + 0.001`, and explicitly asserts no week ever produces the unclamped 15.64km value. All other golden-trace horizons additionally assert every long run `< 16.0` (below target) and every share `≤ 0.46 + 0.001`.

## 32. Taper

`TargetDistance16KIntermediate3DTaperVolumePolicy`: `TaperWeek1VolumeMultiplier`/`TaperWeek2VolumeMultiplier` both referenced from `HalfMarathonParentTaperAuthority` (0.70/0.43) — the identical shared parent-family source 16K's own 4D cell, canonical HM 4D, and canonical HM 6D all already consume. `ResolvedPeakReferenceKm = 34.0` stays this cell's own exact-cell literal, deliberately not moved into the shared component.

## 33. Non-chained taper

Every golden-trace test asserts `taperWeeks[0].PlannedWeeklyVolumeKm >= taperWeeks[1].PlannedWeeklyVolumeKm` (non-increasing/non-chained) computed against the fixed pre-taper anchor, mirroring the exact assertion pattern DIST-GEN.2's own 16K I4D suite uses.

## 34. Progression reuse

The existing, unmodified `HALF_MARATHON_MASTER v1` template and `APPSEL_RACE_PLAN_V1 v10` rule pack are directly reused via the existing `half-marathon-3d-intermediate.v1.json` combination — confirmed via `CandidateAsync()` loading `V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey`/`Version` (pre-existing constants, unmodified by this phase). No 16K-specific progression is created.

## 35. Quality structure

Confirmed via the catalog's own `RUN_LAYOUT_3D`: exactly one KEY session, zero KEY2/secondary-quality lane. `GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI3DNumerics` asserts `Assert.Single(w.Sessions, s => s.StructuralRole == "KEY_SESSION")` for every week.

## 36. KEY/EASY/LONG allocation

Same golden-trace test asserts exactly 3 sessions/week and exactly one session of each of `KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN` for every materialized week across all 5 supported horizons (10-14W).

## 37. Race-specific dose

The reused HM Intermediate 3D candidate's own structural workout dose (session role, not reps/duration, which remain candidate-owned) is directly reusable with 16K's own generic `TARGET_RACE_PACE` semantics — no new dose invented, no file touched in the workout-progression catalog.

## 38. Target-race pace

Fully generic: `TargetDistanceKm=16.0` drives target-race pace identically regardless of frequency — no literal HM/TEN_K pace token, no new `SIXTEEN_K_PACE` concept, no pace-related file appears in this phase's diff.

## 39. Riegel

Unchanged, fully generic — no Riegel-related file in this phase's diff.

## 40. Goal-time fitness separation

Unchanged: a desired 16K finish time does not manufacture current training fitness at any frequency; no new self-validation mechanism introduced.

## 41. Product-average behavior

Unchanged: existing 16K product-average/race-performance-equivalence semantics carry over unmodified; no 3D-specific average race time invented.

## 42. Readiness

Unchanged, fully generic: recent weekly volume/longest run/runs-per-week remain the readiness inputs regardless of frequency; no readiness file touched.

## 43. Calendar

Unchanged: the existing 3D `RunLayout`/calendar machinery (KEY+EASY+LONG placement, preferred-day handling) is reused unmodified via the same `DynamicCoreWeekSkeletonOrchestrator`/`CatalogRunLayoutResolver` every other 3D cell already uses; no calendar file appears in this phase's diff.

## 44. 12W golden

Covered in full by §17 and by the cross-frequency trace in §45; peak volume, LR-share, taper, and phase allocation all verified at this cell's own preferred horizon.

## 45. 16K I3D vs 16K I4D comparison

`CrossFrequencyTrace_16KI3DAndI4D_At12W_ShareIdentityButDifferAuthority` materializes both cells at the shared 12W horizon in one test: identical `RequestedTargetDistanceKm`/`CanonicalDistanceFamily`/`Level`, identical long-run ceiling behavior (both `≤15.0`); different `DaysPerWeek` (3 vs 4), different session count (3 vs 4 per week), different peak-volume ceiling (`≤34.0` vs `≤40.0`).

## 46. Authority-provenance trace

`Registry_WithoutThe16KI3DRegistration_ResolvesNull_NeverFallsBackToCanonicalHmI3D` removes the 16K I3D entry from a locally rebuilt index (via the same `ProjectedTargetAuthorityRegistry.BuildIndex` test seam DIST-GEN.13's own duplicate-registration test already uses) and confirms: the cell no longer resolves in that rebuilt index, and — critically — no surviving entry's `VolumeSafetyPolicy` is reference-equal to `VolumeSafetyPolicy.HalfMarathonIntermediate3D` (the canonical HM object). This is the direct proof that an absent registration fails clean rather than silently substituting the canonical HM I3D policy.

## 47. No I4D contamination proof

`I3DAuthority_NeverResolvesAnyI4DNumericValue` explicitly asserts the 16K I3D policy never resolves `2.5` (I4D's cap), `34.0`/`46.0` (I4D's band), `40.0` (I4D's peak), `23.5` (I4D's calibration), or any of I4D's own LR-share quadruple members (`0.30`/`0.36`/`0.33`/`0.40`).

## 48. No HM fallback proof

`I3DAuthority_ValuesEqualCanonicalHmI3D_ButIsAGenuinelyIndependentInstance` proves: (a) the three numerically-equal exact-cell fields (`GoldenFixtureStartingVolumeKm`, `ResolvedPeakReference.Value`, band bounds) are asserted equal to canonical HM I3D's own by VALUE; (b) `ReferenceEquals` on the `VolumeSafetyPolicy` object is `false`; (c) `PreferredAbsolutePeakLongRunKm` genuinely differs (15.0 vs HM's own 19.0), proving this is an independent authority, not a fallback; (d) a structural source-text scan of both new exact-cell policy files (comments stripped) confirms neither file's executable code contains the literal string `VolumeSafetyPolicy.HalfMarathonIntermediate3D`.

## 49. Unsupported 3D targets

`Registry_WrongCell_NeverResolves` proves 15K×I×3D, 18K×I×3D, 12K×I×3D, 20K×I×3D, 16K×Beginner×3D, 16K×Advanced×3D, 16K×I×5D, and 16K×I×6D all remain unresolvable both through the registry directly and through `Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell`.

## 50. Wrong-cell negatives

Same test as §49 (8 InlineData cases), extending the DIST-GEN.13-established `Registry_WrongLevelOrFrequency_ResolvesNothing_KeyIsTheFullQuadruple`/`Registry_WrongParentFamily_ResolvesNothing` pattern to explicitly include the new cell's own near-miss neighbors.

## 51. Public 16K I3D rejection

Confirmed via existing, UNMODIFIED `DistGen3PublicActivationTests.Pilot16K_WrongFrequency_Rejected` (InlineData days=3/5/6) plus this phase's own `PublicRegistry_Never16KI3D_DarkOnly`. Investigation of the public contract: `GenerateRacePlanPreviewRequest.TargetDistanceKm` is only consulted by `GeneratePreviewCommandMapper` when `GoalDistance == Custom`; there is no separate "runs-per-week override" concept on the wire — `DaysPerWeek` is an ordinary required field on every request regardless of goal distance, so a real client CAN send `goal_distance=custom, target_distance_km=16.0, days_per_week=3` in one payload. That exact combination is rejected by the mapper's own canonicalization gate (`Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell` against the unchanged 3-entry `ApprovedPublicCells`) with `UnsupportedTargetDistanceException` → HTTP 422/`UNSUPPORTED_TARGET_DISTANCE` — a public CONTRACT-level rejection, occurring before the dark-eligibility/materialization pipeline is ever reached, and therefore distinct in kind from the `PlanCoreHorizonTooShortException`/`PlanHorizonCompositionRequiredException` materialization-capability failures §14/§20 prove for the dark-only path.

## 52. Public 16K I4D zero-delta

`DistGen3PublicActivationTests` is completely unmodified by this phase; its own 10-14W success matrix, 9W/15W horizon rejections, and full E2E lifecycle test all remain byte-identical source, confirmed unaffected by the new dark-only registration (see §75/§76 for the fresh regression run confirming this file still passes end-to-end).

## 53. Public 15K zero-delta

`DistGen7PublicActivationTests` unmodified; 15K's own public activation is structurally unreachable from any of this phase's changes (no 15K file touched).

## 54. Public 18K zero-delta

`DistGen11PublicActivationTests` unmodified; same reasoning as §53.

## 55. HM I3D zero-delta

`CanonicalZeroDelta_HmI3DAndI4D_TenKI3DAndI4D_UnaffectedByNewRegistration` asserts `VolumeSafetyPolicy.HalfMarathonIntermediate3D`'s own `LongRunPreferredMinimumShare=0.34`, `PreferredAbsolutePeakLongRunKm=19.0`, `ResolvedPeakReference.Value=34.0`, `GoldenFixtureStartingVolumeKm=20.0` are all unchanged — canonical HM I3D continues to resolve through its own, completely separate static property, never through `ProjectedTargetAuthorityRegistry`.

## 56. HM I4D zero-delta

Same test asserts `VolumeSafetyPolicy.HalfMarathonIntermediate4D.ResolvedPeakReference.Value=43.0`/`PreferredAbsolutePeakLongRunKm=19.0` unchanged.

## 57. TEN_K I3D zero-delta

Same test asserts `VolumeSafetyPolicy.ThreeDayIntermediate.ResolvedPeakReference.Value=22.5`, `AbsoluteWeeklyIncrementCapKm=2.0` unchanged — confirming TEN_K's own I3D authority (whose absolute cap numerically coincides with 16K I3D's own 2.0, and whose peak/calibration do not) remains governed entirely by its own canonical code path.

## 58. TEN_K I4D zero-delta

Same test asserts `VolumeSafetyPolicy.Default.ResolvedPeakReference.Value=38.0` unchanged.

## 59. 15K/16K/18K I4D zero-delta

Same test asserts `HalfMarathonIntermediate4DTargetDistance15K/16K/18K.PreferredAbsolutePeakLongRunKm = 14.5/15.0/16.0` respectively, unchanged. The existing `Dark15K/16K/18KTargetDistanceProjectionTests.cs` golden-trace/public-activation suites are completely unmodified by this phase's diff.

## 60. Exact persistence readiness

Not independently re-exercised in this phase (16K I4D's own `RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` already proves the generic `CanonicalDistanceFamily`/`RequestedTargetDistanceKm` persistence path is frequency-agnostic — it reads from the same `ResolverInputSnapshot`/`GeneratePreviewRequest` fields this phase's own 3D requests populate identically). No schema change was needed or made.

## 61. Custom-without-target safety

Unaffected: the `RaceHorizonPolicy.DecideForDistance`/`.Classify` canonical fallback arm remains the sole path for any non-projected request; `RealPipeline_ZeroDelta_CanonicalHalfMarathon3D_StillSucceeds_NoOverride` confirms a canonical (non-override) HM 3D request at 14W still succeeds via the ordinary path, unaffected by the new dark registration.

## 62. Habit Custom preservation

Not touched; no Habit/Custom-specific file appears in this phase's diff.

## 63. No-distance-band proof

`ProjectedTargetAuthorityRegistry`/`ProjectedTargetAuthority` were not structurally modified beyond adding one registration entry and updating a doc comment; the pre-existing `NewDistGen13Sources_ContainNoRangeProximityOrFittedValueConcept` test (unmodified, still scans these exact files) continues to pass, confirming no band/nearest/interpolation concept was introduced. The two new exact-cell policy files (`TargetDistance16KIntermediate3DPeakVolumeBandPolicy.cs`, `TargetDistance16KIntermediate3DTaperVolumePolicy.cs`) mirror their siblings' exact shape — a fixed constant, never a computed range.

## 64. No-frequency-fallback proof

The registry's lookup remains exact-dictionary-equality over the full 4-field quadruple (unmodified `TryResolve` implementation); `Registry_WrongCell_NeverResolves` explicitly proves 16K×I×5D/6D never silently resolve the 3D or 4D authority, and 15K/18K×I×3D never silently resolve 16K's own 3D authority.

## 65. Duplicate-registration safety

`Registry_DuplicateRegistrationOfNew16KI3DCell_FailsLoudly` (new) and `DistGen13SharedAuthorityNormalizationTests.Registry_DuplicateRegistration_FailsLoudlyAtConstruction_AlsoForTheNew16KIntermediate3DCell` (extension of the existing DIST-GEN.13 test) both confirm `ProjectedTargetAuthorityRegistry.BuildIndex` throws `InvalidOperationException` containing `"registered more than once"` when the new cell is registered twice, and that mixing the 3D cell with the unrelated 4D cell in the same index build does NOT throw (proving the duplicate check is genuinely keyed on the full cell, not merely on `TargetDistanceKm=16.0`).

## 66. Missing-component fail-closed proof

`ProjectedTargetAuthority` remains an immutable `record` with every field declared `required` (unmodified by this phase) — the new registration entry could not have compiled had any exact-cell field (peak-volume band, `VolumeSafetyPolicy`, horizon bounds) been omitted; this is a compile-time fail-closed guarantee, not a runtime default. At runtime, `Registry_WithoutThe16KI3DRegistration_ResolvesNull_NeverFallsBackToCanonicalHmI3D` (§46) proves the complementary case: removing the entire registration (the closest reachable proxy for "a required component absent") yields `null`, never a defaulted or HM-substituted authority.

## 67. Recurring 4D-assumption audit

A repository-wide review of the four canonical dispatch call sites named in the governing brief found exactly one hidden 4D assumption: `CatalogVolumeAndLongRunPlanner.cs`'s own projected-target dispatch block was gated on `request.Candidate.DaysPerWeek == 4` BEFORE ever computing the `darkCell`/`projectedTargetAuthority` lookup — meaning the registry's own already-generic, fail-closed lookup was structurally unreachable for any 3D candidate regardless of registration. This was corrected by widening the outer gate to `(DaysPerWeek == 3 || DaysPerWeek == 4)`; the registry's own lookup remains exact-cell/fail-closed underneath, so this widening introduces zero risk of silently admitting an unregistered 3D cell (any 3D candidate without a registered authority still falls through, unchanged, to the generic HM Intermediate dispatcher below). `PlanServices.cs`, `CatalogPreviewGenerator.cs`, and `TargetDistance16KAwarePeakVolumeBandLoader.cs` were independently re-read in full and confirmed to already call `TryResolveDarkEligibleCell`/`ProjectedTargetAuthorityRegistry.TryResolve` generically, with no equivalent hardcoded frequency gate — these three required zero changes, exactly as anticipated by the governing brief.

## 68. Catalog integrity

No catalog JSON file was created, edited, or duplicated. `PlanCatalog.Tests` — 1510/1510 passed (§72), matching baseline exactly, confirming catalog integrity independent of this phase's backend-only changes.

## 69. Database/schema zero-delta

No migration or schema file appears in this phase's diff.

## 70. Frontend zero-delta

No `mobile/` file appears in this phase's diff; `mobile/lib/features/onboarding/presentation/race_details_page.dart`'s `_approvedProjectedTargetsKm` list was read but not touched, per the governing brief's explicit out-of-scope instruction.

## 71. Targeted tests

`DistGen16Intermediate3DFullDarkImplementationTests.cs` (new, 30 test methods across Facts/Theories) plus `DistGen13SharedAuthorityNormalizationTests.cs` (extended by 1 new test, 1 assertion updated): **77/77 passed** in a combined targeted run (`--filter "FullyQualifiedName~DistGen16Intermediate3DFullDarkImplementationTests|FullyQualifiedName~DistGen13SharedAuthorityNormalizationTests"`).

## 72. PlanCatalog regression

`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1510/1510 passed, 0 failed** — matches the accepted baseline exactly. The benign `ten-k-pilot-domain-decision-audit.{json,md}` regeneration was reverted via `git checkout --` afterward.

## 73. RuntimeCatalog regression

`dotnet test ... --filter "FullyQualifiedName~RuntimeCatalog"` (fresh build, run independently by the parent session): **4042 total / 4042 passed / 0 failed** — the accepted DIST-GEN.13 baseline (4008/4008) plus 34 net new passing tests from this phase's own `DistGen16Intermediate3DFullDarkImplementationTests.cs` and the `DistGen13SharedAuthorityNormalizationTests.cs` extension, zero failures.

## 74. PlanGeneration regression

N/A, per instruction — no separate `PlanGeneration`-scoped suite exists in this repository distinct from the full monolithic `RunningApp.IntegrationTests` run (consistent with DIST-GEN.13 §77's own finding).

## 75. Full monolithic regression

`dotnet test backend/RunningApp.IntegrationTests --no-build` (fresh build, run independently by the parent session, 56m17s): **5146 total / 5142 passed / 4 failed**. The `.trx` (`distgen16_full.trx`) was parsed directly for `outcome="Failed"` `testName` attributes; exactly these 4 identities were found, and no others:

- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

## 76. Durable-baseline comparison

| Suite | Accepted baseline (post-DIST-GEN.13) | This run | Match |
|---|---|---|---|
| PlanCatalog.Tests | 1510/1510 | 1510/1510 | Exact |
| IntegrationTests (RuntimeCatalog filter) | 4008/4008 | 4042/4042 | Exact modulo 34 intentionally-added tests, all passing |
| IntegrationTests (full) | 5111 total/5107 passed/4 named failures | 5146 total/5142 passed/4 named failures (+35 new tests, all passing) | Exact modulo the 35 intentionally-added tests: `DistGen16Intermediate3DFullDarkImplementationTests.cs`'s own new cases (under the `RuntimeCatalog.TargetDistanceProjection` namespace, accounting for the RuntimeCatalog-subtree's own 34-test delta in §73) plus `DistGen13SharedAuthorityNormalizationTests.cs`'s 1 new test (declared in the top-level `RunningApp.IntegrationTests` namespace, outside the RuntimeCatalog filter's own scope, accounting for the remaining +1 seen only in the full-suite total); the two amended cardinality assertions (in `Dark18KTargetDistanceProjectionTests.cs` and `DistGen13SharedAuthorityNormalizationTests.cs`) contribute 0 net new cases. Failing-identity set identical to the known durable baseline, byte-for-byte.

## 77. Build-freshness verification

`dotnet build backend/RunningApp.sln --no-incremental` (forced full rebuild): **0 errors, 16 warnings**, all 16 pre-existing and unrelated to this phase's files (confirmed identical file/line set to the DIST-GEN.13-recorded baseline). No new warning was introduced by any of this phase's new or modified files (independently verified via a project-scoped rebuild diff, §4 shows the exact unique-warning set before/after this phase is identical).

## 78. Final source-diff audit

Complete list of files with a production/test source change (excludes `bin/`, `obj/`, `TestResults/*.trx`, and the benign, reverted `ten-k-pilot-domain-decision-audit.*` regeneration — all pre-existing noise unrelated to this phase):

New: `IntermediateThreeDayFrequencyAuthority.cs`, `TargetDistance16KIntermediate3DPeakVolumeBandPolicy.cs`, `TargetDistance16KIntermediate3DTaperVolumePolicy.cs`, `TargetDistance16KIntermediate3DHorizonPolicy.cs`, `DistGen16Intermediate3DFullDarkImplementationTests.cs`.

Modified: `VolumeSafetyPolicy.cs`, `Dark16KPilotEligibilityPolicy.cs`, `ProjectedTargetAuthorityRegistry.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `DistGen13SharedAuthorityNormalizationTests.cs`.

No `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, or commit/push activity occurred in this engagement.

## 79. Dark implementation result

`16K_INTERMEDIATE_3D_DARK_IMPLEMENTATION_COMPLETE` — the cell is fully materializable through the real production pipeline (`PlanServices.GeneratePreviewAsync` via the internal `TargetDistanceKmOverride` seam) at every horizon DIST-GEN.15B's own authority table supports (10-14W), correctly rejects at both boundaries (9W/15W) with its own typed reason codes, and remains completely unreachable from any public request DTO.

## 80. Architecture result

`EXISTING_TYPED_REGISTRY_REUSED_WITHOUT_REDESIGN` — no change to `ProjectedTargetAuthority`'s field shape, no change to `ApprovedDarkTargetDistanceCell`'s key shape, no new resolution mechanism. The ONE architectural correction required was widening a single hardcoded `DaysPerWeek == 4` outer gate at `CatalogVolumeAndLongRunPlanner.cs` (§67) — a pre-existing latent gap in DIST-GEN.13's own generalization, not a redesign of the registry itself.

## 81. Orthogonality implementation result

`PARTIALLY_ORTHOGONAL_IMPLEMENTATION_CONFIRMED` — mirrors DIST-GEN.15's own governance-level finding: the large majority of authority (structural parent, catalog candidate, horizon triple, absolute cap, starting-LR-cap, absolute-peak-LR, taper, every generic mechanism) composed cleanly from existing target/frequency/parent-family/generic sources with zero new interaction field; the one genuine target×frequency-interaction cluster (peak-volume band/selected-peak/calibration) required its own exact-cell authority (closed by DIST-GEN.15B, implemented here as literal, non-shared declarations) rather than composing for free.

## 82. Current matrix

Dark: `{15K, 16K, 18K}×Intermediate×4D` ∪ `{16K}×Intermediate×3D` (4 cells total). Public: `{15K, 16K, 18K}×Intermediate×4D` (3 cells, unchanged).

## 83. DIST-GEN.17 readiness

Public activation of 16K×Intermediate×3D is explicitly NOT begun or authorized by this phase, per the governing brief's own hard stop. Should a future phase pursue it, the dark implementation here (registry entry, horizon/band/taper policies, `VolumeSafetyPolicy` instance) is already the complete non-public prerequisite; only `ApprovedPublicCells` and the mobile `_approvedProjectedTargetsKm` list would need their own, separately-governed activation decision.

## 84. Remaining blockers

None. Independently re-verified by the parent session from a clean rebuild: targeted DIST-GEN.16/13/RuntimeCatalog-TargetDistanceProjection/public-activation suite 319/319 passed; `PlanCatalog.Tests` 1510/1510; `RuntimeCatalog` subtree 4042/4042; full monolithic suite 5146 total/5142 passed/4 failed, with the exact 4 known durable pre-existing failing identities and no others (§75-76). Every production diff was read directly: the new `IntermediateThreeDayFrequencyAuthority`/`TargetDistance16KIntermediate3D{HorizonPolicy,PeakVolumeBandPolicy,TaperVolumePolicy}` files and the new `VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K` instance correctly preserve the exact-cell-vs-parent-family-vs-frequency ownership distinction DIST-GEN.15B established (peak-volume/selected-peak/calibration declared as literal exact-cell values, never referencing canonical `HalfMarathonIntermediate3D`'s own policy object; absolute-cap/starting-LR-cap referenced from the new frequency-scoped shared component; LR-share declared as its own literal per DIST-GEN.15's own parent-family classification, since no shared 3D LR-share component yet exists; horizon triple reused directly from 16K's own 4D policy, per the established target-only classification). The one genuine architectural fix required — widening a latent, previously-unexercised `DaysPerWeek == 4` hardcoded gate in `CatalogVolumeAndLongRunPlanner.cs` to admit 3 — was verified to be minimal, correctly scoped, and zero-delta to every existing 4D cell (the registry lookup itself remains fail-closed; the fix only lets a 3D candidate reach that already-fail-closed lookup at all). Canonical HM I3D/I4D and TEN_K I3D/I4D were confirmed via direct diff read to be completely untouched.

---

## Final classification

**16K_INTERMEDIATE_3D_FULL_DARK_FREQUENCY_ORTHOGONALITY_IMPLEMENTATION_COMPLETE**

Every DIST-GEN.15B frozen value is implemented exactly (§13, §23-32) with correct exact-cell-vs-parent-family-vs-frequency provenance preserved throughout (§46-48); 16K I3D and 16K I4D coexist as two genuinely distinct authority cells resolved through the identical typed registry, with `RunsPerWeek` genuinely load-bearing in lookup (§6-7, §45); structural parent remains HALF_MARATHON and `RequestedTargetDistanceKm` remains exactly 16.0 throughout (§9-10); the existing HM×Intermediate×3D catalog candidate and RunLayout (KEY/EASY/LONG, zero KEY2) are reused unmodified (§11-13); all five supported horizons (10-14W) materialize correctly and both boundaries (9W/15W) reject cleanly with their own typed, distinct reason codes (§14-20); peak band [28,40], selected peak 34.0, calibration 20.0, growth rates 0.07/0.08, absolute weekly cap 2.0, LR-share quadruple 0.34/0.40/0.38/0.46, absolute peak LR 15.0, starting-LR-cap null, and the non-chained 2-week/0.70/0.43 HM-parent taper are all implemented exactly, with no I4D numeric contamination and no implicit HM-canonical fallback (§23-33, §43, §47-48); KEY/EASY/LONG allocation is feasible at every horizon with no degenerate sessions (§36); target-race-pace, Riegel, goal-feasibility, readiness, and calendar all remain fully generic and untouched (§38-43); 15K/18K×I×3D and every other wrong cell (12K/20K×I×3D, 16K Beginner/Advanced×3D, 16K×I×5D/6D) remain unsupported (§49-50); public 16K I3D remains OFF while public 16K/15K/18K I4D remain fully zero-delta (§51-54); canonical HM I3D/I4D and canonical TEN_K I3D/I4D remain completely untouched, confirmed by direct diff read (§55-58); no catalog, database, or frontend change exists (§68-70); duplicate-registration refusal and missing-component fail-closed behavior are both verified, including for the new cell specifically (§65-66); fresh regression — independently re-run by the parent session from a clean rebuild — shows no new failing identity beyond the 4 known durable ones (§75-76, §84); and the architecture result confirms the existing typed registry was reused without redesign, with the one required generalization (widening a latent, previously-unexercised `DaysPerWeek == 4` hardcoded gate to admit 3) being a minimal, zero-delta, correctly-scoped fix rather than an architectural change (§80).
