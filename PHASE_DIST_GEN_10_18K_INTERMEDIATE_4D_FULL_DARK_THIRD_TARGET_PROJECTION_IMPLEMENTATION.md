# PHASE DIST-GEN.10 — 18K Intermediate 4D Full Dark Third-Target Projection Implementation

## 1. DIST-GEN.9 authority consumed

Every numeric value in this phase is taken verbatim from `PHASE_DIST_GEN_9_18K_INTERMEDIATE_4D_THIRD_TARGET_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md` §53-§57: TargetDistanceKm=18.0, ParentDistanceFamily=HALF_MARATHON, Level=Intermediate, RunsPerWeek=4, MinimumCoreWeeks=10, PreferredCoreWeeks=12, MaximumCoreWeeks=14, PeakVolumeBand=[34,46], SelectedPeakReference=40.0, PreferredWeeklyIncreaseRate=0.07, HardWeeklyIncreaseRate=0.08, AbsoluteWeeklyIncreaseCapKm=2.5, CalibrationStartingVolumeKm=23.5, LR shares 0.30/0.36/0.33/0.40, **PreferredAbsolutePeakLongRunKm=16.0** (the primary target-owned decision), StartingAbsoluteLongRunCapKm=null, Taper=2 weeks/0.70/0.43 non-chained fixed-anchor, GoldenFixtureNonTaperTransitions=9. No value was re-derived, re-researched, or interpolated; every field's origin is cited in the code comments added this phase (see `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K`'s own doc comment).

## 2. Repository baseline

Starting commit `ae1a2eb886335fdbf6c33f2321fba94b96e2a027` on `main`, 0 ahead / 0 behind `origin/main` at the start of this phase (`git status --short --branch` confirmed `## main...origin/main` with no ahead/behind annotation).

## 3. Files changed (exact list)

**Modified (6):**
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs` — added 18.0 to `ApprovedDarkCells` only.
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/../PlanServices.cs` (see `backend/RunningApp.Application/Services/PlanServices.cs`) — third cascade branch for horizon decide/classify/reason-code/min-max/pilot-label.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` — third cascade branch for the dynamic-core horizon-bounds skeleton selection.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — third cascade branch dispatching to the new 18K `VolumeSafetyPolicy` instance.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance16KAwarePeakVolumeBandLoader.cs` — third cascade branch dispatching to the new 18K peak-volume-band policy.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added `HalfMarathonIntermediate4DTargetDistance18K` named instance.

**New (3):**
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/TargetDistance18KHorizonPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/TargetDistance18KPeakVolumeBandPolicy.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark18KTargetDistanceProjectionTests.cs`

**Report (1, new):** this file.

**Deliberately NOT created:** `TargetDistance18KTaperVolumePolicy.cs` — see §33/§37.

**Deliberately NOT touched:** `GeneratePreviewCommandMapper.cs`, `Dark16KTargetDistanceProjectionTests.cs`, `DistGen7PublicActivationTests.cs`, any catalog JSON, any migration, any Flutter file.

**Amended after independent parent-session verification:** `Dark15KTargetDistanceProjectionTests.cs` received a narrow, disclosed 2-line edit (removing the `18.0` entries from `[InlineData(18.0, ..., false)]` in `DarkEligibilityRegistry_15KAnd16K_AreTheOnlyEligibleCells` and `[InlineData(18.0)]` in `ClassifiedButNotEligible_HalfMarathonFamily_NonAdjacentTargets`) — see §69 for why this is a correction of a now-superseded pre-18K literal, not new authority, and why it resolves the blocker below rather than avoiding it.

## 4. Multi-target architecture baseline

Before editing, all five documented call sites used the identical shape: `Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(...)` resolved once into a `darkCell` record, then a ternary/pattern-match cascade (`darkCell is { TargetDistanceKm: 15.0 } ? ... : isDark16KEligible ? ... : <canonical>`) selected the target-specific policy/horizon/band. This is exactly the `MANAGEABLE_BUT_NEEDS_GENERALIZATION` shape DIST-GEN.8 §5/§9 and DIST-GEN.9 §62 flagged as the trigger point for a keyed-lookup generalization once a third branch was needed.

## 5. Authority-ownership model consumed

Per DIST-GEN.8 §7 / DIST-GEN.9 §54-55, fields are split:
- **SHARED, level/frequency-invariant** (identical across TEN_K/HM/15K/16K/18K at Intermediate×4D): PreferredMaxWeeklyIncreaseRatio (0.07), HardMaxWeeklyIncreaseRatio (0.08), AbsoluteWeeklyIncrementCapKm (2.5), LR share quadruple (0.30/0.36/0.33/0.40), StartingAbsoluteLongRunCapKm (null).
- **SHARED, parent-family-inherited** (HALF_MARATHON's own, not distance-banded): TaperDurationWeeks=2, TaperWeek1/2Multiplier=0.70/0.43, non-chained fixed-anchor mechanism.
- **Mechanical consequence of a shared methodology, not an independent decision**: GoldenFixtureStartingVolumeKm=23.5 (HM's own 25.0/43.0=0.5814 ratio applied to this target's own 40.0 peak).
- **Genuinely target-owned**: PeakVolumeBand [34,46], SelectedPeakReference (40.0), horizon triple (10/12/14 — a real, independently-confirmed evidence convergence, not inheritance), and **PreferredAbsolutePeakLongRunKm = 16.0**, the one field DIST-GEN.9 identified as needing new, target-sensitive evidence.

This split is made explicit in the new `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K` doc comment (lines above the instance in `VolumeSafetyPolicy.cs`), rather than presenting every field as an independent 18K decision.

## 6. Exact-target registration

`Dark16KPilotEligibilityPolicy.ApprovedDarkCells` now has exactly three entries: `(16.0, HalfMarathon, Intermediate, 4)`, `(15.0, HalfMarathon, Intermediate, 4)`, `(18.0, HalfMarathon, Intermediate, 4)`. Lookup remains exact-match only (`Math.Abs(km - cell.TargetDistanceKm) < 0.001`), never nearest/interpolated.

## 7. Dark registry evolution

One line added to `ApprovedDarkCells`:
```csharp
new(18.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 4), // DIST-GEN.9/10 -- 18.0km (DARK ONLY -- never added to ApprovedPublicCells)
```
`ApprovedDarkCells.Count` is now 3 (verified by `Dark18KTargetDistanceProjectionTests.PublicEligibility_RemainsNarrowerThanDarkEligibility_18KNeverPublic`).

## 8. Public registry zero-delta

`ApprovedPublicCells` is untouched — still exactly `{16.0, 15.0}` — verified by direct assertion (`Assert.Equal(2, ...ApprovedPublicCells.Count)`, `Assert.DoesNotContain(..., c => c.TargetDistanceKm == 18.0)`) and by the real-HTTP `DistGen7PublicActivationTests.UnsupportedTargetDistances_StillRejected_NeverSubstituted(18.0)` test, which already existed and passes unchanged (400 `UNSUPPORTED_TARGET_DISTANCE`).

## 9. Structural-family resolution

`CanonicalDistanceFamilyResolver.Resolve(18.0)` classifies to `GoalDistance.HalfMarathon` — unchanged resolver, zero new code (verified by `CanonicalDistanceFamilyResolver_18K_ResolvesToHalfMarathon_PreservesExactValue`).

## 10. Exact-target preservation

`RequestedTargetDistanceKm` stays exactly 18.0 throughout the pipeline (`DynamicCoreCalendarMaterializationContext.ResolverInput.RequestedTargetDistanceKm`, and persisted `TrainingPlan.RequestedTargetDistanceKm`) — never collapses to 21.0975 (HM canonical), 16.0, 15.0, or 5.0. Verified directly in the golden-trace test and the persistence test.

## 11. Catalog candidate reuse

The materialization uses the unmodified `HALF_MARATHON__4D__INTERMEDIATE` candidate (`V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey/Version`) — the same candidate 15K/16K/canonical HM already reuse. No new catalog file, no new candidate row.

## 12. Horizon implementation

`TargetDistance18KHorizonPolicy` (new file) freezes MinimumCoreWeeks=10, PreferredCoreWeeks=12, MaximumCoreWeeks=14 and calls `RaceHorizonPolicy.Decide`'s generic 5-arg overload directly — never the TEN_K fallback arm, never HM's own 10/14/16 bounds. `GetUnsupportedReasonCode` produces `DARK_18K_CORE_HORIZON_{weeks}_NOT_SUPPORTED`.

## 13. Lower boundary (9W rejection)

7/8/9-week horizons classify `BelowMinimum` and throw `PlanCoreHorizonTooShortException` with message containing `DARK_18K_CORE_HORIZON_{n}_NOT_SUPPORTED` and `dark 18K target-distance`, never `TARGET_BELOW_SUM_OF_MINIMUMS`, never `DARK_15K`/`DARK_16K`. Verified by `RealPipeline_BelowMinimumHorizon_RejectedByDark18KAuthority_NeverTargetBelowSumOfMinimums` (7/8/9W, all pass).

## 14. Upper boundary (15W rejection)

15W classifies `CompositionRequired` and throws `PlanHorizonCompositionRequiredException` containing `DARK_18K_CORE_HORIZON`, never `DARK_15K`/`DARK_16K`, never `PlanHorizonStartTooEarlyException`. Verified by `RealPipeline_AboveMaximumHorizon_RejectedByDark18KAuthority(15)`.

## 15. Supported-horizon matrix (all 5 horizons, real traces)

All of 10/11/12/13/14 weeks materialize successfully through the full `PlanServices.GeneratePreviewAsync` pipeline (`RealPipeline_EligibleHorizons_MaterializeSuccessfully`, Theory over 10-14) and through the direct orchestrator (`AllFiveHorizons_MaterializeWithExpectedPhaseSplitsAndPeakVolume`), with peak volumes 31.0/32.5/34.0/34.0/34.0 exactly as predicted (identical to 15K's/16K's own real traces, per the shared 40.0 peak/23.5 calibration).

## 16. Phase allocation (exact splits)

| Weeks | FOUNDATION | BUILD | RACE_SPECIFIC | TAPER |
|---|---|---|---|---|
| 10 | 2 | 3 | 3 | 2 |
| 11 | 2 | 3 | 4 | 2 |
| 12 | 2 | 3 | 5 | 2 |
| 13 | 2 | 4 | 5 | 2 |
| 14 | 3 | 4 | 5 | 2 |

All confirmed by direct assertion against `prescribed.Weeks.GroupBy(w => w.PhaseKey)` for every horizon — identical to 15K's/16K's own splits, unmodified `CatalogPhaseAllocationResolver`.

## 17. Stage occupancy

Unchanged — the reused `HALF_MARATHON__4D__INTERMEDIATE` candidate's own stage/run-layout resolution is generic across all three dark targets and canonical HM; no target-distance-specific stage logic exists or was added.

## 18. Progression reuse

The existing `half-marathon-workout-progression.v2.json` (HALF_MARATHON Intermediate 4D primary progression) is reused unmodified — zero new progression file, zero progression code change.

## 19. Target-race-pace reuse

`RaceSpecificPaceSemantic` is `SHARED_GENERIC_MECHANISM` — driven entirely by whatever `RequestedTargetDistanceKm` (18.0) flows through the existing pace-derivation code; zero new code.

## 20. Riegel reuse

`RecentRaceEquivalenceMethod` (Riegel) is unmodified, generic, distance-parametric; consumes 18.0 like any other numeric target distance. Zero new code.

## 21. Goal-time fitness separation

`GoalFeasibilityThresholdReuse` unchanged; the feasibility resolver is target-distance-generic. Zero new code.

## 22. Product-average behavior

`AdaptationReuse`/`CalendarReuse` — both `SHARED_GENERIC_MECHANISM`, zero new code, per DIST-GEN.9 §54.

## 23. Peak-volume authority (34.0/46.0/40.0)

`TargetDistance18KPeakVolumeBandPolicy.MinimumKm/MaximumKm` = 34.0/46.0 (new file, byte-identical bounds to the 15K/16K siblings' own — a disclosed evidence convergence, not template-copying, per DIST-GEN.9 §55). `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K.ResolvedPeakReference.Value` = 40.0.

## 24. Selected peak (never forced)

`ResolvedPeakReferenceIsSelectedCeiling = true` — reused unmodified HM.5.3 mechanism; 13W/14W saturate at 40.0km rather than extrapolating past it, exactly like the 15K/16K siblings.

## 25. Calibration (23.5, mechanical consequence)

`GoldenFixtureStartingVolumeKm = 23.5d` — computed via the same HM.1's own 25.0/43.0=0.5814 ratio applied to this target's own 40.0 peak (40.0 × 0.5814 = 23.26, rounded to the 0.5km grid = 23.5). Never a missing-readiness fallback — same fail-closed `ResolveStartingVolume` guard applies unchanged.

## 26. Shared growth authority (0.07/0.08/2.5)

`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08` reused directly — identical literal values to every other Intermediate 4D anchor in `VolumeSafetyPolicy.cs`, proof of reuse (same numeric literal, same doc-comment classification `DIRECT_EXISTING_AUTHORITY`), not re-derivation.

## 27. Shared absolute-cap authority

`AbsoluteWeeklyIncrementCapKm = 2.5d` — identical to canonical HM/15K/16K's own 4D value.

## 28. Shared LR-share authority (0.30/0.36/0.33/0.40)

`LongRunPreferredMinimumShare/MaximumShare/SelectionShare/HardCapShare` = 0.30/0.36/0.33/0.40 — byte-identical across every Intermediate 4D anchor in the file.

## 29. Shared starting-LR authority (null)

`StartingAbsoluteLongRunCapKm` left unset (defaults to `null` per the record's own `init` property) — mirrors `HalfMarathonIntermediate4D`'s own null.

## 30. 18K absolute peak LR (16.0 — the central decision)

`PreferredAbsolutePeakLongRunKm: 16.0d` — deliberately different from 15K's 14.5 and 16K's 15.0. Verified directly against both siblings in `VolumeSafetyPolicy_18K_EncodesExactFrozenValues` (`Assert.NotEqual(fifteenK..., policy...)`, `Assert.NotEqual(sixteenK..., policy...)`).

## 31. 15K/16K/18K LR isolation (the most important test)

`Dark18KTargetDistanceProjectionTests.ThreeWayIsolation_15K16K18K_SameHorizonSameReadiness_DistinctPeakLongRunAuthority` materializes all three targets at 12W with identical readiness inputs in one test, asserts the registry resolves three distinct, non-equal `ApprovedDarkTargetDistanceCell` records (`cell15`/`cell16`/`cell18`, pairwise `Assert.NotEqual`), asserts three distinct `VolumeSafetyPolicy` object references (`Assert.False(ReferenceEquals(...))` pairwise), and asserts `PreferredAbsolutePeakLongRunKm` = 14.5/15.0/16.0 respectively — the direct policy-identity provenance proof requested, not merely "the output numbers differ" (which, in fact, coincide at 13.5 for all three at this readiness level, exactly as DIST-GEN.9 predicted).

## 32. LR-below-target invariant (16.0 < 18.0)

Asserted directly: `Assert.True(policy18.PreferredAbsolutePeakLongRunKm < 18.0d)` alongside the same check for 15K (<15.0) and 16K (<16.0) in the same three-way test.

## 33. Parent-family taper (2 weeks, 0.70/0.43, non-chained) — taper-class decision

**Decision: no `TargetDistance18KTaperVolumePolicy.cs` class was created.** `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance18K` reuses `TargetDistance16KTaperVolumePolicy.TaperWeek2VolumeMultiplier` and `TargetDistance16KTaperVolumePolicy.OrderedMultipliers` directly for its `TaperVolumeMultiplier`/`TaperVolumeMultipliers` fields, with an explicit inline comment: *"Deliberately reused directly from TargetDistance16KTaperVolumePolicy -- SHARED_PARENT_FAMILY_AUTHORITY, not a third near-duplicate taper class."* This was possible because `VolumeSafetyPolicy`'s taper fields are plain `double`/`IReadOnlyList<double>` values, not references to a specific policy-class type — no architectural requirement forces a dedicated class per target. DIST-GEN.9 explicitly classified taper as `SHARED_PARENT_FAMILY_AUTHORITY` (inherited from HALF_MARATHON's own 2-week taper, proven non-target-specific by TEN_K's own genuinely different 1-week/0.53 taper), so creating a third near-identical class would have been exactly the "represent every field as if it were an independent target decision" anti-pattern flagged by the governing prompt. This is a disclosed choice, not a silent duplication.

## 34. Race-specific dose (unchanged)

No change — the reused progression's race-specific-phase session content is candidate-owned, not target-distance-owned.

## 35. Session allocation

Unchanged — 4 sessions/week, generic `CatalogRunLayoutResolver`/`CatalogSessionPrescriptionPlanner`, verified by `Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count))` in the golden trace.

## 36. Long-run feasibility

Verified: every week's long run stays `< 18.0` (target distance) and `<= 16.0 + 0.001` (this target's own ceiling) — `Assert.All(volume.LongRunProgression.Weeks, w => {...})` in the golden trace and the 5-horizon theory test.

## 37. Readiness

`GoalFeasibilityResolver`/`CoreEntryReadinessResolver` unchanged, generic; consumes the request's own recent-volume/longest-run/recent-race fields identically for 18K as for any other target. Zero new code.

## 38. Calendar

`CatalogWeekSkeletonCalendarMaterializer`/`DynamicCoreCalendarMaterializationOrchestrator` unchanged; 18K materializes real dated weeks via the identical dynamic-core path 15K/16K already use (never the static "exact preferred core" path, which structurally assumes HM's own 14-week preferred length — see the existing `isDark16KEligibleForSkeleton || horizon.Mode is CompressedCore or ExtendedCore` gate in `CatalogPreviewGenerator.cs`, itself already inclusive of 18.0 since it checks "any dark cell", unmodified this phase).

## 39. 18K golden trace (full 12W table)

From `GoldenTrace_12W_MaterializesWithFrozen18KNumerics_LrCeilingIsSixteen` (real pipeline output, asserted exactly):

| Week | Phase | Volume (km) | Long Run (km) |
|---|---|---|---|
| 1 | FOUNDATION | 20.0 | 6.5 |
| 2 | FOUNDATION | 21.5 | 7.0 |
| 3 | BUILD | 23.0 | 7.5 |
| 4 | BUILD | 24.5 | 8.0 |
| 5 | BUILD | 26.0 | 8.5 |
| 6 | RACE_SPECIFIC | 28.0 | 9.0 |
| 7 | RACE_SPECIFIC | 29.5 | 10.5 |
| 8 | RACE_SPECIFIC | 31.0 | 11.5 |
| 9 | RACE_SPECIFIC | 32.5 | 12.5 |
| 10 | RACE_SPECIFIC | 34.0 | 13.5 |
| 11 | TAPER | 24.0 | 8.0 |
| 12 | TAPER | 14.5 | 5.0 |

Numerically identical to the already-proven 15K/16K real golden traces (per DIST-GEN.2B §7-11 / DIST-GEN.6), confirming the shared-peak/shared-calibration prediction exactly. Taper non-chained check: `w2 != w1 * 0.43` (verified `Math.Abs(w2 - w1*0.43) > 0.5`).

## 40. Three-target comparison trace (15K/16K/18K side by side)

At 12W, identical readiness, all three targets produce the identical observed weekly-volume/long-run trace above (peak volume 34.0, peak long run 13.5) — the CEILING AUTHORITY differs (14.5/15.0/16.0) even though the OBSERVED output coincides, exactly as predicted by DIST-GEN.9 (the ceiling never binds at this readiness). Verified in `ThreeWayIsolation_15K16K18K_SameHorizonSameReadiness_DistinctPeakLongRunAuthority`.

## 41. Exact-policy provenance (registry-level proof of non-cross-resolution)

`Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell` called with 15.0/16.0/18.0 returns three distinct, pairwise-`NotEqual` `ApprovedDarkTargetDistanceCell` records with `TargetDistanceKm` exactly 15.0/16.0/18.0 respectively — never a mismatched cell. Verified in `DarkEligibilityRegistry_18K_ResolvesToOwnDistinctIndependentCell` and the three-way isolation test.

## 42. Unsupported nearby targets (17.9/18.1/17.5/18.5)

All four verified `IsDarkEligible(...) == false` via `DarkEligibilityRegistry_15K16K18K_AreTheOnlyEligibleCells` and `ClassifiedButNotEligible_HalfMarathonFamily_NonAdjacentAndNearbyTargets` — no nearest-target snapping (exact-match-only registry lookup, no `Math.Round`/nearest-neighbor code anywhere).

## 43. Wrong-cell negatives (18K Beginner/Advanced/3D/5D/6D)

Verified `WrongCell_18K_NotEligibleAtAnyOtherLevelOrFrequency` (Beginner×4, Advanced×4, Intermediate×3, Intermediate×5, Intermediate×6 — all `false`).

## 44. 10-mile exact status (unchanged)

16.09 remains ineligible for both `IsDarkEligible` and `IsEligible` — verified `TenMileExact_16Point09_RemainsUnchangedStatus`. Untouched by this phase.

## 45. Public 18K remains off (real HTTP proof)

`DistGen7PublicActivationTests.UnsupportedTargetDistances_StillRejected_NeverSubstituted(18.0)` (pre-existing test, unmodified) passes: 400 `UNSUPPORTED_TARGET_DISTANCE`, no `HALF_MARATHON__4D__INTERMEDIATE` in body, no plan/preview persisted. Confirmed passing in this phase's full `DistGen7PublicActivationTests` run (23/23 passed).

## 46. Public 15K remains on (zero-delta real HTTP proof)

Same test-class run confirms `Target15KRequest` at supported horizons still returns 200 with `requested_target_distance_km: 15.0` — zero delta.

## 47. Public 16K remains on (zero-delta real HTTP proof)

Same run confirms `PeakLongRun_15K_Uses14Point5_16K_StillUses15_NoCrossContamination` passes unchanged — 16K's own public path is zero-delta.

## 48. Custom fail-closed safety (422 regression)

Covered by the pre-existing, unmodified tests in `DistGen7PublicActivationTests.cs` (lines ~324-359, `GoalDistance.Custom`-shaped requests with `target_distance_km` removed) — all pass unchanged in this phase's run.

## 49. Habit Custom preservation (zero diff)

`git diff` confirms zero changes to any file matching `GenerateHabitPlanPreviewRequest` or `custom_goal_page.dart` — grep for both across the full diff returns no matches, since this phase never touched any file in `Commands/`, `DTOs/Plan/GenerateHabitPlanPreviewRequest.cs`, or any Flutter path.

## 50. Persistence readiness/proof

Proven directly (mirroring 15K's own DIST-GEN.6 pattern): `Dark18KTargetDistanceProjectionTests.RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously` confirms a real preview→confirm round trip persists `CanonicalDistanceFamily="HALF_MARATHON"`, `RequestedTargetDistanceKm=18.0`, distinct from `GoalDistanceKm` (21.0975) and from 15.0/16.0. No deferral to DIST-GEN.11 needed for this proof — dark persistence was fully testable without public admission, exactly as for 15K.

## 51. TEN_K zero-delta

No TEN_K-owned file was touched; `RaceHorizonPolicy`'s TEN_K fallback arm (8/12/14) and `VolumeSafetyPolicy.Default`/named 10K instances are byte-identical (no diff). RuntimeCatalog subtree regression (100% pass, see §66) includes the full existing TEN_K test surface unchanged.

## 52. HM 4D zero-delta

`VolumeSafetyPolicy.HalfMarathonIntermediate4D` untouched — verified directly in `VolumeSafetyPolicy_18K_EncodesExactFrozenValues` (`Assert.Equal(43d, canonical.ResolvedPeakReference.Value)`, `Assert.Equal(19d, canonical.PreferredAbsolutePeakLongRunKm)`).

## 53. HM 6D zero-delta

`VolumeSafetyPolicy.HalfMarathonIntermediate6D` and its dispatch (`ForHalfMarathonIntermediateDaysPerWeek`) were not touched by this phase's edits (edits were confined to the HALF_MARATHON×Intermediate×4D dark-target branch inside `CatalogVolumeAndLongRunPlanner.Build`, gated on `ReferenceEquals(_policy, VolumeSafetyPolicy.Default)` — a TEN_K-only gate that HM 6D's own dispatch never enters). Confirmed no diff to any HM-6D-specific line.

## 54. 15K zero-delta (dark AND public)

Dark: `Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell(15.0,...)` still resolves the same 15.0 cell; `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K` untouched (no diff). Public: `DistGen7PublicActivationTests` 15K assertions pass unchanged. **Resolved (see §69)**: two pre-existing assertions inside `Dark15KTargetDistanceProjectionTests.cs` had hardcoded the value 18.0 as NOT dark-eligible (written in DIST-GEN.6, before 18K existed). Since this phase's own explicit purpose is to make 18.0 dark-eligible, those two literals were stale by construction the moment `ApprovedDarkCells` gained its third entry. The parent session corrected the two InlineData literals (removing the `18.0` case from each, since full affirmative/negative coverage of 18.0 already exists in the new `Dark18KTargetDistanceProjectionTests.cs`) rather than accepting a permanent failure or weakening the registry. All 53 tests in the file now pass; every 15K-specific assertion (eligibility, volume, taper, horizon) remains zero-delta.

## 55. 16K zero-delta (dark AND public)

`Dark16KTargetDistanceProjectionTests.cs` — completely unmodified, all 50 tests pass unchanged (verified in an isolated run: `Başarılı: 50, Toplam: 50`). This file's own `[InlineData(15.0, ..., false)]`/`[InlineData(18.0, ..., false)]` entries test the historical, narrower `IsEligible` (frozen single-triple) overload, not the multi-cell `IsDarkEligible` registry — so it was never at risk from either the 15K (DIST-GEN.6) or 18K (DIST-GEN.10) registry additions. Public 16K path zero-delta per §47.

## 56. Registry scalability — architecture assessment

Adding the third target did **not** require any new registry mechanism — `ApprovedDarkCells` scaled to a third entry with a one-line addition, and `TryResolveDarkEligibleCell`'s exact-match-over-a-list shape needed no change. However, the **downstream dispatch cascades** (horizon decide/classify/reason-code selection in `PlanServices.cs` and `CatalogPreviewGenerator.cs`; policy selection in `CatalogVolumeAndLongRunPlanner.cs` and `TargetDistance16KAwarePeakVolumeBandLoader.cs`) **did** grow by one branch each, exactly as DIST-GEN.8 §5/§9 and DIST-GEN.9 §62 predicted would happen at the third target. See §57 for the explicit decision and reasoning.

## 57. Dispatch-generalization decision (and internal naming audit)

**Decision made: option (a) — extend the existing ternary/pattern-match cascades with a third branch, matching the established pattern exactly. Option (b) — generalizing to a keyed lookup / per-target authority record — was NOT implemented this phase**, despite DIST-GEN.9 §62's own explicit recommendation to "seriously consider" it now and "not defer again."

**Reasoning, disclosed:** A keyed-lookup refactor touching all five call sites (two of which — `PlanServices.cs`'s horizon-gate block and `CatalogPreviewGenerator.cs`'s dynamic-core skeleton block — carry non-trivial exception-message and reason-code logic layered on top of the raw horizon decision) carries real risk of an unintended behavior/message-text change to the two already-shipped-and-tested 15K/16K pilots, which this phase is contractually required to keep byte-identical. Given this single phase already required (a) three new authority files, (b) five call-site edits, (c) a new ~600-line test file, and (d) a full three-suite regression re-verification (PlanCatalog 1510, RuntimeCatalog 4011, monolithic 5041) that alone took over an hour of wall-clock CI time, bundling a simultaneous dispatch-architecture rewrite was judged to add verification surface disproportionate to the risk it would retire — the exact-match cascade extension is a small, easily-diffable, line-for-line mirror of the existing 15K-then-16K pattern, which minimizes the chance of an undetected behavior change to the frozen pilots. This is the disclosed fallback path the original governing prompt explicitly permitted ("if you judge the refactor too risky to complete safely alongside the new 18K authority in one phase, option (a) is acceptable as a fallback, PROVIDED you document exactly why").

**Consequence for a hypothetical fourth target**: a fourth branch would again need to be added by hand to the same five call sites — the generalization DIST-GEN.9 recommended remains outstanding. This should be the explicit, named first task of any future DIST-GEN.11+ dark-target phase, now with the benefit of three concrete, byte-identical branches to generalize from rather than two.

**Internal naming audit** (per the governing prompt's own vocabulary):
- `Dark16KPilotEligibilityPolicy` — **HISTORICAL_NAME_WITH_MULTI_TARGET_RESPONSIBILITY**. The class now owns eligibility/registries for three dark targets (15K/16K/18K) and two public targets (15K/16K), not just the original 16K pilot. Not renamed this phase — a rename here would touch every one of the ~13 files that reference it by name (including both frozen zero-delta test files, `Dark15K...Tests.cs`/`Dark16K...Tests.cs`), which is exactly the kind of blast-radius this phase avoided for the same reason as §57's dispatch decision. Its own doc comment already explicitly documents this historical-name status (see the existing `ApprovedDarkTargetDistanceCell`/`ApprovedDarkCells` doc comments, themselves written during DIST-GEN.6 to explain the multi-target widening) — no new comment was needed, but this phase's own three-target reality reinforces the classification.
- `TargetDistance16KAwarePeakVolumeBandLoader` — **HISTORICAL_NAME_WITH_MULTI_TARGET_RESPONSIBILITY** (already so-classified in its own DIST-GEN.6 doc comment, which this phase's third branch is fully consistent with; not renamed, same blast-radius reasoning).
- `TargetDistance15KHorizonPolicy`/`TargetDistance16KHorizonPolicy`/`TargetDistance18KHorizonPolicy`, `TargetDistance15K/16K/18KPeakVolumeBandPolicy`, `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K/16K/18K` — **STILL_SEMANTICALLY_CORRECT**. Each name refers to exactly the one target it encodes; no widening occurred.
- No renames were performed this phase.

## 58. No-interpolation proof

Grep across the full diff for `lerp|Interpolat|midpoint|distance-ratio|DistanceRatio` (excluding the pre-existing, unrelated `ResolvePeak`/`transitionAdjustedMultiplier` interpolation-by-week-count mechanism, which is level/frequency-generic and untouched) returns no new occurrences introduced by this phase's edits. Every 18K numeric field is a frozen literal from DIST-GEN.9, never computed from 15K's/16K's own values at runtime.

## 59. No-band proof

`git diff` for this phase's changed/new files, grepped for `Band(?!VolumeBand)|Range|nearest|minTarget|maxTarget` (excluding the pre-existing, correctly-named `CatalogPeakVolumeBand`/`PeakVolumeBand` type, which predates this phase and represents a single target's own [min,max] km/week volume envelope, not a distance-banding concept) shows no new distance-band abstraction: `ApprovedDarkCells` is still a flat list of exact triples, `TryResolveDarkEligibleCell` is still exact-match, and no `minTargetKm`/`maxTargetKm`/`InteriorDistancePolicy`-shaped type was added.

## 60. Catalog/hash integrity

Zero catalog JSON changes. The one auto-regenerated benign diff (`plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`, produced by running `PlanCatalog.Tests`) was reverted via `git checkout --` before the final diff audit — confirmed `git status --short -- plan-catalog` shows no tracked-file changes.

## 61. Database/schema zero-delta

No migration file created or modified — `git status` shows no new file under any `Migrations/` directory. `TrainingPlan.CanonicalDistanceFamily`/`RequestedTargetDistanceKm` columns (added in a prior phase) are reused unchanged.

## 62. Frontend zero-delta

No Flutter file touched — the full diff contains zero files under any `frontend`/`lib`/`.dart` path.

## 63. Public-routing zero-delta

`GeneratePreviewCommandMapper.cs` has **zero diff** this phase — confirmed by `git status`/`git diff` showing no changes to that file. It consults only `ApprovedPublicCells` (unchanged at `{16.0, 15.0}`), never `ApprovedDarkCells`, so it structurally cannot be affected by the dark-registry widening.

## 64. Targeted tests (full list + outcomes)

- `Dark18KTargetDistanceProjectionTests.cs` (new, ~35 test methods incl. Theory expansions) — all pass.
- `Dark16KTargetDistanceProjectionTests.cs` (unmodified) — 50/50 pass.
- `Dark15KTargetDistanceProjectionTests.cs` (amended, 2 lines — see §69) — **53/53 pass**.
- `DistGen7PublicActivationTests.cs` (unmodified) — 23/23 pass.
- Combined `~TargetDistanceProjection` filter (post-amendment, fresh build): **166/166 pass** (see §69 for the precise accounting).

## 65. PlanCatalog regression (exact numbers)

`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1510 total / 1510 passed / 0 failed** — exactly matches the required baseline.

## 66. RuntimeCatalog regression (exact numbers)

Final authoritative run (fresh build, post-amendment): `dotnet test ... --filter "FullyQualifiedName~RuntimeCatalog"`: **4008 total / 4008 passed / 0 failed** (baseline after DIST-GEN.7 was 3945/3945; net +63 new passing tests — the new `Dark18KTargetDistanceProjectionTests.cs` file, net of the 2 InlineData cases removed from `Dark15KTargetDistanceProjectionTests.cs` in the §69 amendment). 100% pass rate, zero failures of any kind.

## 67. PlanGeneration regression

N/A — no dedicated `PlanGeneration`-named test project/subtree distinct from the monolithic `RunningApp.IntegrationTests` suite exists in this repo; covered by §68.

## 68. Full monolithic regression (exact numbers, exact failing identities)

Three full runs occurred across this phase, per this repo's own documented stale-build caution:

**Run 1 & Run 2 (superseded)**: executed before the §69 amendment; both surfaced the 2 now-resolved Dark15K failures documented in the agent's original delivery, in addition to the 4 known pre-existing identities.

**Run 3 (fresh clean build, authoritative, post-amendment)**: `dotnet build backend/RunningApp.sln` completed with 0 errors immediately before the run, confirming the build reflects the final, amended `Dark15KTargetDistanceProjectionTests.cs` (§69). Full-suite result: **5038 total / 5034 passed / 4 failed**, duration 45m05s. The 4 failing identities, parsed directly from the `.trx` (`distgen10_full.trx`):

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — known pre-existing (DIST-GEN.7 baseline).
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)` — known pre-existing (DIST-GEN.7 baseline).
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` — known pre-existing (DIST-GEN.7 baseline).
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — known pre-existing (DIST-GEN.7 baseline).

**No other failures.** Total count reconciliation: baseline (DIST-GEN.7) was 4975; this phase adds the new `Dark18KTargetDistanceProjectionTests.cs` file net of the 2 InlineData cases removed from `Dark15KTargetDistanceProjectionTests.cs`, landing at 5038 — fully reconciled, zero unexplained delta.

## 69. Exact durable-baseline comparison

Documented DIST-GEN.7 baseline: 4975 total / 4971 passed / 4 failed, with these exact 4 known pre-existing failing identities:
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

All four re-confirmed present, byte-identical stack traces, in the authoritative full-suite run (§68, Run 3) — no other identity present.

**Resolution of the two transient failures surfaced during this phase's own verification:**
- `Dark15KTargetDistanceProjectionTests.DarkEligibilityRegistry_15KAnd16K_AreTheOnlyEligibleCells(targetDistanceKm: 18, ...)`
- `Dark15KTargetDistanceProjectionTests.ClassifiedButNotEligible_HalfMarathonFamily_NonAdjacentTargets(km: 18)`

**Root cause, fully explained**: both assertions were written in DIST-GEN.6 (before 18K existed) as forward-looking "18.0 is still not eligible" checks against the multi-cell `IsDarkEligible`/`TryResolveDarkEligibleCell` registry (not the narrower, frozen `IsEligible` single-triple overload — see §55 for why `Dark16KTargetDistanceProjectionTests.cs`'s own superficially-similar `[InlineData(15.0,...,false)]`/`[InlineData(18.0,...,false)]` entries were never at risk: they test `IsEligible`, which DIST-GEN.10 never touches). DIST-GEN.10's own explicit, required purpose (§7 of this report) is to add 18.0 to `ApprovedDarkCells`, which makes `IsDarkEligible(18.0, HalfMarathon, Intermediate, 4)` true — directly, unavoidably, and correctly superseding these two specific pre-DIST-GEN.10 assertions.

This was not a functional regression to 15K's own production behavior (eligibility, volume, taper, horizon all remained zero-delta) — it was a stale literal in test data, analogous to a test that hardcodes "there are only 2 users in the system" being invalidated by intentionally adding a third user. The `18.0` InlineData case was removed from both theories (2 lines total) rather than the file being weakened, mislabeled, or left permanently red: the equivalent, and more complete, affirmative (`18.0 → true`) and negative (`17.9`/`18.1`/`17.5`/`18.5` → `false`) coverage for 18.0 already exists in the new `Dark18KTargetDistanceProjectionTests.cs` (§64), so no assertion coverage was lost — it now lives in the file that owns that target's authority. Post-amendment: `Dark15KTargetDistanceProjectionTests.cs` is 53/53 passing, and the full monolithic suite (§68, Run 3) shows exactly the 4 known pre-existing failures and no others.

## 70. Build-freshness verification

`dotnet build backend/RunningApp.sln -v minimal` run immediately before the authoritative full-suite run: **"Oluşturma başarılı oldu. 0 Uyarı 0 Hata"** (0 warnings, 0 errors), elapsed ~2.7s (incremental — no other source changed since the prior full build). This confirms the subsequent test run reflects the current, reverted source tree exactly, per this repo's own documented stale-build-false-alarm history (DIST-GEN.3).

## 71. Final source-diff audit (complete file list)

Modified (6, production): see §3. New (3 code + 1 report): see §3. Amended (1, test-only, 2 lines, disclosed): `Dark15KTargetDistanceProjectionTests.cs` (§69). Confirmed **zero diff** to: `Dark16KTargetDistanceProjectionTests.cs`, `DistGen7PublicActivationTests.cs`, `GeneratePreviewCommandMapper.cs`, every catalog JSON, every migration, every Flutter file — via `git status`/`git diff --stat`.

## 72. Third-target architecture result

**`THIRD_TARGET_DARK_REUSES_EXISTING_MULTI_TARGET_PROJECTION_ARCHITECTURE`.** The registry (`ApprovedDarkCells`) scaled with a one-line addition and zero mechanism change; every shared/generic mechanism (Riegel, readiness, calendar, adaptation, progression) required zero new code; only the target-owned fields (peak band, horizon triple, absolute peak LR) needed new, independently-authored sibling files, exactly mirroring the 15K-then-16K pattern. The one caveat, fully disclosed in §56/§57: the downstream dispatch cascades were extended with a third branch rather than generalized to a keyed lookup, which DIST-GEN.9 had recommended reconsidering at this exact point — a real, acknowledged, but non-blocking architectural debt for a future phase.

## 73. Current public/dark matrix

| Target | Dark | Public |
|---|---|---|
| 15K (HALF_MARATHON, Intermediate, 4D) | Yes | Yes (since DIST-GEN.7) |
| 16K (HALF_MARATHON, Intermediate, 4D) | Yes | Yes (since DIST-GEN.3) |
| 18K (HALF_MARATHON, Intermediate, 4D) | **Yes (new, this phase)** | **No — dark only** |
| Everything else (12K/17K/19K/20K/10-mile-exact/other levels/frequencies) | No | No |

## 74. DIST-GEN.11 readiness statement

18K's dark authority is now fully implemented and proven (all 5 horizons, both boundaries, three-way isolation, persistence). A future DIST-GEN.11 public-activation phase would need only: (a) add `(18.0, HalfMarathon, Intermediate, 4)` to `ApprovedPublicCells`, (b) real end-to-end HTTP tests mirroring `DistGen7PublicActivationTests.cs`'s own 15K-activation shape, and (c) resolve the §57-disclosed dispatch-generalization debt before or during that phase, since a public-facing change is a higher-stakes place to discover an unintended cascade-ordering bug than this dark-only phase. Not begun here, per explicit scope.

## 75. Remaining blockers

**None.** The one genuine, precisely-scoped item surfaced during this phase's own verification — two pre-existing `Dark15KTargetDistanceProjectionTests.cs` assertions hardcoding `18.0` as ineligible, written in DIST-GEN.6 before 18K existed — was resolved via a disclosed, two-line, test-data-only amendment (§69), not by weakening the 18K dark-registry entry or by silently hiding a failure. Post-amendment, all three suites are 100% clean against their respective baselines (§65/§66/§68).

One non-blocking structural item remains for a future phase: the dispatch-generalization debt (§57/§72) — extend-vs-generalize was resolved in favor of the lower-risk "extend" path this phase; a fourth target would repeat the same five-call-site manual extension unless a future phase performs the keyed-lookup generalization DIST-GEN.9 recommended.

---

## Final classification

**18K_INTERMEDIATE_4D_FULL_DARK_THIRD_TARGET_PROJECTION_IMPLEMENTATION_COMPLETE**

Every DIST-GEN.9 field is implemented exactly with correct shared/target-owned attribution; all three dark targets resolve distinct, non-cross-contaminating authority (§31/§41); all 5 horizons materialize with correct phase splits and the predicted golden trace (§15/§16/§39); both horizon boundaries reject with correctly-typed, correctly-labeled errors (§13/§14); public 18K stays off and public 15K/16K stay zero-delta by real HTTP proof (§45-47); PlanCatalog (1510/1510), RuntimeCatalog (4008/4008), and the full monolithic suite (5038/5034, exactly the 4 known pre-existing failures and no others) all regress cleanly; no band/interpolation code was introduced (§58/§59); the architecture result is `THIRD_TARGET_DARK_REUSES_EXISTING_MULTI_TARGET_PROJECTION_ARCHITECTURE` (§72). The two transient test failures the implementing agent originally, honestly surfaced (§69) were a stale pre-18K literal in test data, not a production or authority defect — they were corrected via a disclosed, minimal, two-line edit with no coverage loss (the equivalent assertions already exist, more completely, in the new `Dark18KTargetDistanceProjectionTests.cs`) rather than forcing closure over a real gap. The one disclosed, non-blocking architectural item (dispatch-generalization, §57/§72) is carried forward explicitly as a named task for a future phase, consistent with this engagement's "no forced closure, no invented numbers, no silently deferred debt" discipline.
