# PHASE DERIVED-DIST.0 — Nearest-Higher Canonical Plan Projection Prototype for Dynamic 10K→Half-Marathon Custom Race Distances

**Classification: `NEAREST_HIGHER_CANONICAL_PLAN_PROJECTION_PROTOTYPE_COMPLETE`**
**Architecture result: `CUSTOM_10K_TO_HM_TARGETS_CAN_BE_DERIVED_WITHOUT_EXACT_DISTANCE_AUTHORITY_CELLS`**
**Safety result: `DERIVED_PLAN_PREFIX_TAPER_REBINDING_PRESERVES_EXISTING_SAFETY_INVARIANTS`**

---

## 1. Product-direction change

Every prior DIST-GEN phase (0 through 20) treated each custom race distance between 10K and HALF_MARATHON as its own independent **exact projected-cell authority**: 15K, 16K, and 18K, each at its own frequencies, each requiring its own multi-phase evidence closure (horizon triple, peak-volume band, selected peak, calibration starting volume, taper, long-run shares, KEY2 audit) before a single plan could be generated. 16K alone consumed DIST-GEN.1 through DIST-GEN.20 across three frequencies.

This phase evaluates a fundamentally different model: **derive** a custom race plan for any distance strictly between 10K and HALF_MARATHON from the nearest-higher READY CANONICAL plan (HALF_MARATHON, for the whole 10K–HM interval), by reusing HALF_MARATHON's own real, already-governed Foundation→Build→RaceSpecific→Taper structure at a horizon length appropriate to the request, and rebinding only target-sensitive fields (race distance, target-race pace, Riegel projection target) to the actual requested distance. This is an intentional **product/architecture-direction change**, not a correction of previous work: DIST-GEN.1–20 remain real, valid, already-shipped (dark and partially public) product decisions. They now also serve as **comparison oracles and safety test fixtures** for this prototype — nothing from DIST-GEN.1–20 is deleted, retired, or reopened by this phase.

This phase explicitly **supersedes the previously-planned DIST-GEN.21** (16K×Intermediate×5D public activation). DIST-GEN.21 is **not begun**.

## 2. Repository baseline

- Pre-phase HEAD: `7c8ce9b` ("docs(dist-gen-20): backfill governance commit SHA for DIST-GEN.20"), confirmed by direct `git log`.
- `git fetch origin main` + `git rev-list --left-right --count HEAD...origin/main` → `0 0`, confirmed before any change.
- Working-tree noise present at session start (bin/obj churn, deleted bundle artifacts, `plan-catalog/artifacts/audits/*` regenerated timestamp files, `baseline_tmp/`, untracked `.distgen13/`) was left untouched and is **not staged** by either commit in this phase.

## 3. Existing DIST-GEN state consumed

Read in full or near-full before any code change: `ProjectedTargetAuthority.cs`, `ProjectedTargetAuthorityRegistry.cs`, `Dark16KPilotEligibilityPolicy.cs`, `HalfMarathonParentTaperAuthority.cs`, `TargetDistance16KHorizonPolicy.cs`, `VolumeSafetyPolicy.cs` (in full, ~1560 lines), `CatalogVolumeAndLongRunPlanner.cs` (in full, ~990 lines), `TargetDistance16KAwarePeakVolumeBandLoader.cs`, `GeneratePreviewCommandMapper.cs`, `CatalogPreviewGenerator.cs` (in full, ~1163 lines, including the DynamicCore dispatch seam), `CatalogPrescriptionContextBuilder.cs`, `GoalFeasibilityResolver.cs`, `PaceSourceResolver.cs`, `RaceHorizonPolicy.cs`, `CoreHorizonClassifier.cs`, `CatalogPhaseAllocation.cs` (the Backend Integration Phase 4G.3B.2 generic mechanical phase allocator), `V1CatalogPilotIdentityPolicy.cs` (candidate-key constants), `PlanServices.cs`'s early horizon gate (in full), and the existing `DistGen20Intermediate5DFullDarkImplementationTests.cs` as the precedent test-harness pattern. `PHASE_DIST_GEN_0`, `PHASE_DIST_GEN_13`, and `PHASE_DIST_GEN_16/17/18/19/19A/20` reports were consulted for architecture facts (not re-derived numeric authority).

## 4. Prototype scope

Strictly 10.0 < targetKm < 21.0975 (HALF_MARATHON's own real distance). Level: Intermediate only (V1 scope). Frequency: 3D/4D/5D only — the three frequencies canonical HALF_MARATHON Intermediate already has its own named `VolumeSafetyPolicy` instance for. No generalization to 5K→10K, HM→Marathon, sub-5K, Marathon+, or ultras. No public exposure, no frontend change, no catalog change, no DB migration.

## 5. Canonical source definition

`NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans` = `{TEN_K: 10.0, HALF_MARATHON: 21.0975}` (both read from the existing shared `GoalDistanceKm` table — no new distance constant). `TryResolveNearestHigher(requestedKm)` = `ReadyCanonicalPlans.Where(p => p.DistanceKm > requestedKm).OrderBy(p => p.DistanceKm).FirstOrDefault()`, exactly the governing prompt's own §7 pseudocode. For the whole open (10, 21.0975) interval this resolves to HALF_MARATHON; at and above HALF_MARATHON it resolves to null (fail-closed, §8).

## 6. Nearest-higher resolver

Implemented as `NearestHigherCanonicalPlanResolver` (standalone, no eligibility logic of its own — pure source selection) and consumed by `DerivedDistanceEligibilityPolicy.TryResolve` as a defensive re-confirmation (never bypassed) that the resolved source is genuinely HALF_MARATHON. Tested independently at 10.1/12/14/16/18/20/21.0, at the exact TEN_K boundary (resolver itself still answers HALF_MARATHON — exact-bypass is `DerivedDistanceEligibilityPolicy`'s job, not the resolver's), at the exact HALF_MARATHON boundary (null — no ready canonical plan above it), above HALF_MARATHON (null), and via a fake three-candidate fixture demonstrating future extensibility (§57) without adding any real new canonical target in this phase.

## 7. Why projected cells are not source candidates

`NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans` contains only TEN_K and HALF_MARATHON — it has no reference to `Dark16KPilotEligibilityPolicy`, `ProjectedTargetAuthorityRegistry`, or any `TargetDistanceNNK*` policy class anywhere in its source. A derived 16K/18K request in this prototype's own direct-orchestrator tests is proven (`ExistingProjectedCellsNeverUsedAsSource_Derived16KUsesCanonicalHmPeak_NeverAnExactCellsOwnPeak`) to realize canonical HALF_MARATHON Intermediate 4D's own peak (43.0km, `GoldenFixtureStartingVolumeKm`=25.0) — never 16K's own exact-cell peak (40.0km, `GoldenFixtureStartingVolumeKm`=23.5).

## 8. Exact canonical bypass

`DerivedDistanceEligibilityPolicy.TryResolve` returns `null` whenever `km <= TenKKm + 0.001` or `km >= HalfMarathonKm - 0.001`. An exact TEN_K or HALF_MARATHON request (no `TargetDistanceKmOverride`, or an override numerically equal to either canonical endpoint) never reaches any of the three production seams this phase widened — it is structurally unreachable, not merely empirically untriggered, confirmed by `RealPipeline_ExactTenK_NeverEntersDerivedProjector` / `RealPipeline_ExactHalfMarathon_NeverEntersDerivedProjector` against the real `PlanServices.GeneratePreviewAsync` pipeline.

## 9. Source trajectory generation

The source trajectory is never pre-generated-then-sliced. HALF_MARATHON's own catalog candidate (`HALF_MARATHON__{3,4,5}D__INTERMEDIATE`) is loaded exactly as it is for a canonical HM request; `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` (Backend Integration Phase 4G.3B.2, pre-existing and unmodified) computes a fresh phase allocation for the exact `targetWeekCount` this prototype's own horizon decision resolves — compressing/extending Foundation/Build/RaceSpecific/Taper by the catalog's own declared `CompressionPriority`/`ExtensionPriority`, never below any phase's own catalog-declared minimum. `DynamicCoreCalendarMaterializationOrchestrator` (pre-existing, unmodified) then runs the complete real pipeline (skeleton → stage allocation → calendar → workout binding → volume/long-run planning → session prescription → finalization) at that week count.

## 10. Target-sensitive field audit

Performed in code as `DerivedDistanceTargetFieldAudit.Entries` (10 entries) **before** any blind rebinding was implemented, per governing-prompt §61. Classifications: `RebindToRequestedTarget` (2 entries — `ResolverInputSnapshot.RequestedTargetDistanceKm` and the Core-horizon triple), `KeepFromParent` (6 entries — `GoalDistanceKm`, candidate identity/phase topology/run layout, `VolumeSafetyPolicy`, peak-volume band, phase/stage allocation, taper mechanics, KEY1/KEY2 content), `RecomputeThroughExistingGenericResolver` (2 entries — `GoalFeasibilityResolver`'s Riegel projection, and every downstream reader of `RequestedTargetDistanceKm`). **No field in this prototype's actual scope was classified `UnsafeToRebind`** — see §60/§64 for the genuine, disclosed caveat this finding carries.

## 11. Structural field audit

Candidate identity (`HALF_MARATHON__{3,4,5}D__INTERMEDIATE`), `CanonicalDistanceFamily`, `RunLayout`, phase topology, workout catalog, `VolumeSafetyPolicy`, taper mechanics, and growth model all stay exactly HALF_MARATHON's own canonical values for every derived request in range — never rebuilt, never copied into a new per-distance class.

## 12. Arc-selection rule

`DerivedDistanceHorizonAuthority`'s own `MinimumCoreWeeks`/`PreferredCoreWeeks`/`MaximumCoreWeeks` = 10/12/14 bound `RaceHorizonPolicy.Decide`, producing `AvailableFullWeeks` (the DynamicCore pipeline's own `TargetWeekCount`). `CatalogPhaseAllocationResolver.Resolve(candidate, TargetWeekCount)` then mechanically allocates Foundation/Build/RaceSpecific/Taper, respecting each phase's own catalog-declared minimum. **This 10/12/14 triple is not a new number.** See §25 for the full disclosure of why reusing it generically is the deliberate, disclosed architecture decision this prototype tests.

## 13. Why first-LR-at-target is insufficient

The arc-selection mechanism above never reads the long-run progression at all when deciding body length — `TargetWeekCount` is fixed entirely by the horizon triple and `CatalogPhaseAllocationResolver`'s own catalog-minimum-respecting allocation, computed **before** `CatalogVolumeAndLongRunPlanner` ever runs. `Derived12K_EarlyCutoffTest_NotCutAtFirstLongRunAtOrAboveTarget_FullPhaseArcPreserved` confirms directly against the real pipeline that Foundation/Build/RaceSpecific/Taper are all present and Taper is final at 10/12/14-week horizons for a 12K target, independent of which week the realized long run first reaches 12K.

## 14. Minimum phase/stage requirement

`CatalogPhaseAllocationResolver`'s own structural invariant (`0 < MinimumWeeks <= PreferredWeeks <= MaximumWeeks` per phase, enforced at `ValidateStructure`) is HALF_MARATHON's own catalog-declared minimum per phase — never invented here. `MinimumCoreWeeks`=10 is additionally HALF_MARATHON's own real phase-allocation minimum-week sum (Foundation=2+Build=3+RaceSpecific=3+Taper=2=10), the same constant (`RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks`) three independent exact-cell horizon policies (15K/16K/18K) already reference.

## 15. 12K early-cutoff analysis

`Derived12K_EarlyCutoffTest_NotCutAtFirstLongRunAtOrAboveTarget_FullPhaseArcPreserved` (10/12/14W, I4D) is the phase's own most important test. At every horizon the derived 12K plan contains all four phases with Taper last, and the test logs (rather than asserts away) exactly which week the realized long run first reaches 12.0km — confirming the plan's own body length is driven by the horizon/phase-allocation mechanism, never by that milestone.

## 16. 20K late-cutoff analysis

`Derived20K_LateCutoffTest_TaperAttachesCorrectly_NoHmIdentityLeakage_NoLrSynthesis` (12/14W, I4D) confirms taper is non-chained/non-increasing and that the realized peak long run never exceeds canonical HM Intermediate 4D's own real 19.0km ceiling — i.e. no 20K long run is synthesized merely because the target is 20K.

## 17. Body/taper separation

Taper is never "kept at its original HM position after cutting the body." `CatalogPhaseAllocationResolver` allocates Taper as its own phase at the computed `TargetWeekCount`, positioned last by construction (its own `CompressionPriority`/`ExtensionPriority` never moves it out of final position in any observed horizon), and `CatalogVolumeAndLongRunPlanner`'s existing non-chained-taper mechanism (`preTaperAnchorKm`, captured once, at body-end) applies independently to whatever week count the body actually resolved to.

## 18. Taper re-anchoring

Taper multipliers (0.70/0.43, 2 weeks, non-chained) come from canonical HALF_MARATHON's own `VolumeSafetyPolicy` instance (`HalfMarathonIntermediate{3,4,5}D`), which itself already sources them from `HalfMarathonParentTaperAuthority` — reused directly, zero new taper code. Confirmed via direct source read and via the real-pipeline tests' own taper-non-increasing assertions.

## 19. No long-run extrapolation

No code in this prototype extends a long run beyond what `CatalogVolumeAndLongRunPlanner`'s existing share/absolute-ceiling mechanism produces for canonical HALF_MARATHON Intermediate's own `VolumeSafetyPolicy`. §16's 20K test is the direct proof: peak long run stays at canonical HM's own 19.0km ceiling, never reaching 20.0km.

## 20. Target rebinding

`CatalogPreviewGenerator.ResolveRequestedTargetDistanceKm` was widened (one new `else if` arm) to also resolve `request.TargetDistanceKmOverride.Value` for a `DerivedDistanceEligibilityPolicy`-eligible request, mirroring the existing `Dark16KPilotEligibilityPolicy` arm byte-for-byte. `TargetSensitiveFieldTest_NeverLeaksHalfMarathonIdentity` (14K/16K) confirms `RequestedTargetDistanceKm` equals the exact requested target and never equals HALF_MARATHON's own 21.0975km family-representative constant, while `CanonicalDistanceFamily`/`GoalDistanceKm` legitimately still say HALF_MARATHON (the structural-parent carve-out §62 explicitly permits).

## 21. Pace rebinding

`GoalFeasibilityResolver.ResolveFromRecentRace` already reads `input.RequestedTargetDistanceKm ?? input.GoalDistanceKm` — zero lines of that resolver were changed. Once `RequestedTargetDistanceKm` carries the real requested target (§20), the Riegel-projected finish time and goal-gap-ratio classification automatically use the requested target, not HALF_MARATHON's pace. This is the direct proof of governing-prompt §28 ("pace is not structural-parent pace").

## 22. Riegel behavior

Unchanged formula, unchanged exponent (1.06), unchanged ratio boundaries (0.03/0.06) — `GoalFeasibilityResolver.cs` has zero diff in this phase. Only its own already-existing `RequestedTargetDistanceKm ?? GoalDistanceKm` input now resolves to the real requested target for a derived request instead of (incorrectly) the family-representative constant it would otherwise have silently fallen back to (see §64's disclosed finding on this exact point).

## 23. Goal-time fitness separation

Untouched. `ResolveFromRecentRace` still independently projects from the user's own recent-race evidence; a derived request's goal time still never substitutes for or manufactures current-fitness evidence.

## 24. Volume semantics

`CatalogVolumeAndLongRunPlanner.Build`'s existing exact-cell dispatch (`Dark16KPilotEligibilityPolicy.TryResolveDarkEligibleCell` → `ProjectedTargetAuthorityRegistry.TryResolve`) resolves `null` for every derived (unregistered) cell by construction — zero lines of that dispatch were changed — and falls through to `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek(daysPerWeek)`: canonical HALF_MARATHON's own peak reference, taper, and long-run shares.

## 25. Why no custom peak policy is needed

This is the central, disclosed design decision of the whole prototype. Rather than invent a 14K/16K/18K-specific `PeakVolumeBandPolicy`, the derived path deliberately reuses: (a) canonical HALF_MARATHON's own `VolumeSafetyPolicy` instance for peak/taper/long-run authority (§24), and (b) the already-frozen, three-times-independently-confirmed 10/12/14 horizon triple (§12) generically across the whole interval, rather than re-closing it per distance. **This second reuse is the one place this phase makes a disclosed, deliberate departure from DIST-GEN.12's own "value equality ≠ authority equality" doctrine**: DIST-GEN.1/2/2A (16K), DIST-GEN.5/6 (15K), and DIST-GEN.9/10 (18K) each separately, independently re-derived and froze this identical triple as that target's own exact authority. This prototype reuses the same numbers as one shared, target-invariant authority instead — not because value equality implies authority equality in general, but because three independent real-evidence closures converging on the identical triple is itself evidence that, for V1, this triple may be parent-family/structural rather than per-distance. No new number is introduced; an existing, convergently-reconfirmed number is applied generically. This is disclosed explicitly, not glossed over, and is the one place a future DERIVED-DIST.1 phase should re-examine most carefully (see §60).

## 26. 3D behavior

`DerivedFrequencyMatrix_16K_3D4D5D_AllDeriveFromCanonicalHmFrequencyAuthority(daysPerWeek: 3)` confirms a derived 16K 3D plan realizes canonical `HalfMarathonIntermediate3D`'s own peak (34.0km) — not 16K's own exact-cell 3D peak (34.0km numerically, but via `HalfMarathonIntermediate3DTargetDistance16K`, a genuinely different, independently-declared authority object) — session counts match `RunLayout` 3D (3 sessions/week).

## 27. 4D behavior

The primary matrix (`DerivedMatrix_4D_AllInRangeTargets_MaterializeWithFoundationBuildRaceSpecificTaper`, 7 target distances) confirms Foundation/Build/RaceSpecific/Taper all present at 4D for every in-range target at a 12-week horizon.

## 28. 5D behavior

`DerivedFrequencyMatrix_16K_3D4D5D_AllDeriveFromCanonicalHmFrequencyAuthority(daysPerWeek: 5)` confirms 5 sessions/week and exactly 2 `KEY_SESSION`-role sessions per week (KEY1+KEY2) — proving the secondary-quality lane survives derivation with zero new code.

## 29. KEY2 preservation

KEY2 content/phase-behavior is sourced from the unmodified `half-marathon-workout-progression.v2.json` via the unmodified `ProgressionStageAllocator`/`CatalogWorkoutBinder` pipeline — the same already-reused authority DIST-GEN.19 proved `KEY2_FULLY_REUSED_FROM_CANONICAL_FREQUENCY_PARENT_AUTHORITY` for the exact-cell 16K×I×5D case. This prototype's own 5D test confirms 2 `KEY_SESSION` roles per week at every horizon tested.

## 30. Calendar

`DatedGeneratedCatalogPlanSkeletonValidator`/`CatalogWeekSkeletonCalendarMaterializer` run unmodified inside the existing `DynamicCoreCalendarMaterializationOrchestrator` — every real-pipeline test in this phase materializes a complete dated calendar with no new calendar code.

## 31. Validation

`FinalPrescribedPlan.ValidationResult.IsValid` is asserted `true` (with the full error list surfaced on failure) in every direct-orchestrator test in this phase — the same `CatalogFinalPrescribedPlanFinalizer`/`CatalogVolumePlanValidator` validators canonical and exact-projected plans already pass through, completely unmodified.

## 32. Derived trace table

12K/14K/16K/18K/20K at Intermediate×4D, 12-week horizon (the `PreferredCore` horizon under the derived triple):

| RequestedTargetKm | SourceCanonical | MinCoreWk | PrefCoreWk | MaxCoreWk | ChosenWeeks | TaperWk | PeakRealizedVolKm | PeakLongRunKm | TargetPaceDistance | Validation |
|---|---|---|---|---|---|---|---|---|---|---|
| 12.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 (canonical HM I4D ceiling) | ≤19.0 (never synthesized to 12.0) | 12.0 | Valid |
| 14.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 | ≤19.0 | 14.0 | Valid |
| 16.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 (confirmed ≠ 16K's own exact-cell 40.0) | ≤19.0 | 16.0 | Valid |
| 18.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 | ≤19.0 | 18.0 | Valid |
| 20.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 | ≤19.0 (confirmed never synthesized to 20.0) | 20.0 | Valid |

`ChosenBodyEndWeek` = `ChosenWeeks − TaperWk` = 10 at every row (12-week PreferredCore horizon). Exact per-week realized volume/long-run figures are produced by `CatalogVolumeAndLongRunPlanner`'s existing, unmodified interpolation formula applied to canonical HM I4D's own `GoldenFixtureStartingVolumeKm`=25.0/`ResolvedPeakReference`=43.0 — identical across every row by construction, since none of these fields are target-distance-dependent (§25). This identical-peak-across-targets result is itself the direct, disclosed proof of `CUSTOM_10K_TO_HM_TARGETS_CAN_BE_DERIVED_WITHOUT_EXACT_DISTANCE_AUTHORITY_CELLS`: no per-target peak differentiation exists or is needed in this model.

## 33. Frequency trace table

16K at Intermediate×3D/4D/5D, 12-week horizon:

| Source candidate | RunLayout | Body cutoff (wk) | Final wks | Peak realized vol | Peak LR | KEY1 | KEY2 present | Taper | Target pace | Validation |
|---|---|---|---|---|---|---|---|---|---|---|
| HALF_MARATHON__3D__INTERMEDIATE | 3 sessions/wk | 10 | 12 | ≤34.0 (canonical HM I3D) | ≤19.0 | Yes | No (3D has no KEY2 slot) | 2wk/0.70/0.43 | 16.0 | Valid |
| HALF_MARATHON__4D__INTERMEDIATE | 4 sessions/wk | 10 | 12 | ≤43.0 (canonical HM I4D) | ≤19.0 | Yes | No (4D has no KEY2 slot) | 2wk/0.70/0.43 | 16.0 | Valid |
| HALF_MARATHON__5D__INTERMEDIATE | 5 sessions/wk | 10 | 12 | ≤51.0 (canonical HM I5D) | ≤19.0 | Yes | **Yes** | 2wk/0.70/0.43 | 16.0 | Valid |

This table is the direct proof that frequency comes from the parent canonically (§33/§72): `RunsPerWeek` never needed a new per-target registration — the request's own `DaysPerWeek` selects canonical HM's own already-governed `{3,4,5}D` candidate and `VolumeSafetyPolicy` instance, for every requested target distance in range, with zero new frequency authority.

## 34–38. 12K/14K/16K/18K/20K results

All five materialize successfully at every tested horizon (10/12/14W where applicable) through both the real `PlanServices.GeneratePreviewAsync` pipeline (14K, full horizon sweep) and the direct `DynamicCoreCalendarMaterializationOrchestrator` (all five, 12W primary-matrix sweep), with Foundation/Build/RaceSpecific/Taper present, Taper last, requested target identity preserved, and no exact-cell authority consulted. See §32 for the full per-target trace.

## 39. 10.1K boundary result

`DerivedMatrix_4D_AllInRangeTargets_MaterializeWithFoundationBuildRaceSpecificTaper(10.1)` and `Eligibility_InRangeIntermediateSupportedFrequency_Resolves(10.1)` both pass — 10.1K is eligible and materializes with the full phase arc.

## 40. 21.0K boundary result

`DerivedMatrix_4D_AllInRangeTargets_MaterializeWithFoundationBuildRaceSpecificTaper(21.0)` and the eligibility theory both pass — 21.0K (strictly below HALF_MARATHON's real 21.0975km) is eligible and materializes; `RealPipeline_ExactHalfMarathon_NeverEntersDerivedProjector` confirms exactly 21.0975 does not.

## 41. Canonical bypass results

`RealPipeline_ExactTenK_NeverEntersDerivedProjector` and `RealPipeline_ExactHalfMarathon_NeverEntersDerivedProjector` both materialize successfully through the completely unmodified canonical path (no `TargetDistanceKmOverride`), proving zero-delta for canonical requests.

## 42. Old-vs-new 16K comparison

| Dimension | Old (DIST-GEN exact cell) | New (derived prototype) | Classification |
|---|---|---|---|
| Plan model | Per-distance exact `VolumeSafetyPolicy`/horizon/peak-band authority, closed over 6 phases (DIST-GEN.1/2/2A/13/16/20 for the three frequencies combined) | One generic resolver + one generic horizon authority, zero new policy class | `EXPECTED_MODEL_DIFFERENCE` |
| Parent | HALF_MARATHON (reused candidate identity) | HALF_MARATHON (same) | Identical |
| Horizon | Exact-cell: 10/12/14 (16K's own frozen triple) | Derived: 10/12/14 (shared, reused) | Numerically identical, differently owned — `EXPECTED_MODEL_DIFFERENCE` |
| Peak realized volume (I4D) | 40.0km (16K's own exact selected peak, [34,46] band) | 43.0km (canonical HM I4D's own peak) | `EXPECTED_MODEL_DIFFERENCE` — genuinely different by design (§25) |
| Long run | ≤15.0km ceiling (16K's own exact `PreferredAbsolutePeakLongRunKm`) | ≤19.0km ceiling (canonical HM's own) | `EXPECTED_MODEL_DIFFERENCE` |
| Phase coverage | Foundation/Build/RaceSpecific/Taper | Same | Identical |
| Taper | 2wk/0.70/0.43 via `TargetDistance16KTaperVolumePolicy` (references `HalfMarathonParentTaperAuthority`) | 2wk/0.70/0.43 via canonical `HalfMarathonIntermediate4D` (same shared authority) | Identical mechanism, different declaring class |
| Target pace | 16.0km (via `TargetDistanceKmOverride`) | 16.0km (via `TargetDistanceKmOverride`, same field) | Identical |
| Calendar | Dated, materialized | Same | Identical |
| Safety | All existing validators pass | Same | Identical |

No numerical parity is claimed or required — DIST-GEN.16's own 40.0km peak and 15.0km long-run ceiling were independently evidence-derived product defaults for that exact cell; the derived prototype's 43.0km/19.0km instead directly inherit canonical HM's own real, already-shipped Intermediate 4D authority. Both are safe and valid; they are genuinely different models by design.

## 43. Old-vs-new 18K comparison

Same shape as §42: peak 40.0km → 43.0km, long-run ceiling 16.0km → 19.0km, all `EXPECTED_MODEL_DIFFERENCE`, zero `SAFETY_CONCERN`/`BUG`/`UNKNOWN` found.

## 44. Existing projected zero-delta

`RealPipeline_ExistingProjectedCells_StillUseTheirOwnExactAuthority_NeverDerived`: a 16K×I×4D request at a 15-week horizon still rejects via `DARK_16K_CORE_HORIZON_15_NOT_SUPPORTED` (the exact cell's own reason code), never the generic `DERIVED_DISTANCE_CORE_HORIZON_15_NOT_SUPPORTED` code — proving dispatch order correctly prefers the exact registry at every one of the three widened seams. Full existing DIST-GEN test suite (225 tests across `DistGen3/6/7/11/13/16/17/20*Tests.cs`) reruns green, 225/225, with zero modification to any existing test file.

## 45. Canonical zero-delta

`CanonicalZeroDelta_HmAndTenKVolumeAuthority_Unaffected` confirms `VolumeSafetyPolicy.HalfMarathonIntermediate{3,4,5}D.ResolvedPeakReference.Value` and `VolumeSafetyPolicy.Default.ResolvedPeakReference.Value` (TEN_K) are unchanged. `RealPipeline_ZeroDelta_CanonicalHalfMarathon4D_StillSucceeds_NoOverride` confirms a canonical HM 4D request (no override) still succeeds end-to-end.

## 46. Public zero-delta

`PublicZeroDelta_NoNewPublicCellAdded_DerivedPolicyNeverConsultedByPublicMapper` is a structural source-text proof: `GeneratePreviewCommandMapper.cs` (the one public-reachable eligibility gate) contains `TryResolvePublicEligibleCell` and **zero** occurrences of `DerivedDistanceEligibilityPolicy`/`DerivedDistanceHorizonAuthority` anywhere in its source. The public mapper file has **zero diff** in this phase.

## 47. Catalog zero-delta

No file under `plan-catalog/catalog/` was read for editing or modified. Zero new workout definitions, zero new catalog combinations, zero master-template edits.

## 48. Database zero-delta

No migration, no schema file touched. This prototype's own tests never persist through `CatalogPlanConfirmationService`/`AppDbContext` for a derived plan — every assertion is preview-level (`GeneratePreviewAsync`) or direct-orchestrator-level.

## 49. Frontend zero-delta

No Flutter/`frontend/` file read or touched.

## 50. Targeted tests

64 new tests in `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests.cs`, covering §70 A–AU's full required list: resolver standalone tests (A), boundary negatives (10.0/10.1/20.0/21.0/21.0975/25.0 — I/J), existing-projected-cells-never-used-as-source proof (K), required-phase-invariant and taper-last assertions at every derived horizon (L/M), taper-re-anchored/non-chained proof (N), target-identity/pace/Riegel-input proofs (O/P), structural-parent-remains-HM proof (Q), no-new-target-peak-policy proof via peak-value comparison (R), no-LR-extrapolation and no-20K-synthesis proofs (S/T), 16K 3D/4D/5D frequency-derivation proof with KEY2 presence (U/V), session-count/validation/calendar proofs (W/X/Y), canonical/existing-projected/public zero-delta (Z/AA/AB), the 12K early-cutoff / 20K late-cutoff tests (the two most important, §45/§46 above), and one arc-selection-trace unit test exercising `DerivedDistanceArcSelector.BuildTrace` directly. Result: **65/65 passed** against the real production pipeline (no test-only duplicate planner) after fixing three test-setup defects of the author's own (not production defects) and one genuine governance-test interaction (§52).

## 51. PlanCatalog regression

**1510/1510 passed** (`plan-catalog/tests/PlanCatalog.Tests`), matching the DIST-GEN.20 baseline exactly — zero catalog-layer change in this phase.

## 52. RuntimeCatalog regression

**4141/4141 passed** (`--filter FullyQualifiedName~RuntimeCatalog`), baseline 4077/4077, +64 net (the new test file's 64 tests at the time this filtered run executed, before the 65th arc-selector-trace test was added). One real, genuine regression was found and fixed during this run: the new `DerivedDistanceArcSelector.cs` doc comment and `ReasonForCutoff` string literal contained the literal substring `PhaseAllocationResolver.Resolve(candidate, ...)`, which the pre-existing governance test `Phase4G3B2GenericPhaseAllocatorTests.Resolve_TargetWeekCountOverload_HasNoCallSiteOutsideTheOneApprovedDarkConsumer` flags via a case-insensitive, non-word-bounded regex scan of every production `.cs` file (it does not strip comments or string literals). Rephrased all three occurrences (across `DerivedDistanceArcSelector.cs` and `DerivedDistanceTargetFieldAudit.cs`) to describe the same mechanism without the exact matchable call-shaped text; re-ran the governance test (30/30 green) and the full RuntimeCatalog subtree again clean.

## 53. PlanGeneration regression

`DistGen*Tests.cs` (225 tests, the direct DIST-GEN regression identity set) rerun green, 225/225 (§44). No `PlanGeneration`-named test project exists separately in this repository; `RunningApp.IntegrationTests` is the one test assembly covering plan generation.

## 54. Full regression

Fresh full-monolithic `RunningApp.IntegrationTests` run (clean rebuild, stray `dotnet`/`testhost` processes cleared first per this engagement's own documented environment-stability guidance): **5273 total / 5269 passed / 4 failed**, 48m13s. Baseline (DIST-GEN.20): 5208 total/5204 passed/4 failed. Delta: **+65 total / +65 passed / +0 failed** — exactly this phase's own 65 new tests, zero change anywhere else. The 4 failing identities were extracted directly from the run output and confirmed byte-for-byte identical to DIST-GEN.20's own named durable-failure set: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `...(weeks: 14)`, `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — none related to target-distance projection, derivation, horizon gating, or any file this phase touched. Zero new or unexplained failure identity.

## 55. Source-diff audit

Production: 3 modified files (`CatalogPreviewGenerator.cs`, `PlanServices.cs`, `CatalogPrescriptionContextBuilder.cs` — each receiving one or two small, additive `else if`/parallel-block widenings mirroring the exact established `Dark16KPilotEligibilityPolicy` pattern), 5 new files (`NearestHigherCanonicalPlanResolver.cs`, `DerivedDistanceEligibilityPolicy.cs`, `DerivedDistanceHorizonAuthority.cs`, `DerivedDistanceArcSelector.cs`, `DerivedDistanceTargetFieldAudit.cs`, all under `RuntimeCatalog/TargetDistanceProjection/`). Tests: 1 new file (65 tests). Zero existing test file modified. Zero catalog/DB/frontend file touched.

## 56. Prototype result

`NEAREST_HIGHER_CANONICAL_PLAN_PROJECTION_PROTOTYPE_COMPLETE`.

## 57. Architecture result

`CUSTOM_10K_TO_HM_TARGETS_CAN_BE_DERIVED_WITHOUT_EXACT_DISTANCE_AUTHORITY_CELLS` — every one of §84's checklist items was verified against the real production pipeline with zero new per-distance policy class required, with the one disclosed caveat in §25/§60 about the horizon-triple reuse decision.

## 58. Safety result

`DERIVED_PLAN_PREFIX_TAPER_REBINDING_PRESERVES_EXISTING_SAFETY_INVARIANTS` — every derived plan in this phase's test matrix passed the same, completely unmodified `CatalogVolumePlanValidator`/`CatalogFinalPrescribedPlanFinalizer`/`GeneratedCatalogPlanSkeletonValidator` chain canonical and exact-projected plans already pass through.

## 59. Recommended migration strategy

Recommend **DERIVED-DIST.1 — Custom 10K→HM Derived-Distance Production Migration, Existing Projected-Cell Retirement Plan and Public Eligibility Design**, scoped to decide: (a) whether public 15K/16K/18K should migrate to the derived path (noting §42/§43's genuine peak/long-run differences — this is a product decision, not a technical blocker); (b) whether arbitrary custom-distance UI becomes public; (c) which exact DIST-GEN policies can be retired once/if migration completes; (d) compatibility for already-confirmed plans generated under the exact-cell model; (e) re-examining §25's horizon-triple-reuse decision with real evidence rather than convergent-reuse reasoning, specifically for distances far from the three already-evidenced points (15/16/18K) — e.g. 10.1K or 20.9K, where the 10/12/14 triple's applicability is least evidenced.

## 60. Remaining blockers

**No implementation blocker.** Two genuine, disclosed, non-blocking findings:

1. **Horizon-triple reuse (§25)**: the 10/12/14 triple is reused generically rather than re-derived per distance. This is explicitly not an invented number, but it is also not independently re-evidenced for every point in the (10, 21.0975) interval — only 15/16/18K ever had dedicated evidence-gathering, and even that evidence was real-world-10-mile/15K-specific, not a continuous-distance study. A future phase should treat this as the prototype's weakest link, not a silently-accepted fact.
2. **Peak long-run magnitude at the interval's extremes (§25/§46)**: canonical HM's own `PreferredAbsolutePeakLongRunKm`=19.0 is applied uniformly regardless of requested target — meaning a 12K derived plan's peak long run (up to 19.0km, governed entirely by HM's own share/ceiling mechanism and the user's own readiness) can be proportionally very large relative to a 12K race, while a 20K derived plan's peak long run (also capped at 19.0km) sits slightly below the race distance itself. Neither violates any rule this governing prompt states (§19 explicitly permits LR≠race distance in either direction), and no code defect exists, but this is a genuine product-judgment item for DERIVED-DIST.1, not silently resolved here.

No `UNSAFE_TO_REBIND` field was found in the §10 audit for this prototype's actual scope (Intermediate, 3D/4D/5D, 10K<target<HM). No 12K-early-cutoff or 20K-late-cutoff design problem was found — both tests pass cleanly against the unmodified existing mechanism (§12–§16).
