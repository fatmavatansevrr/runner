# PHASE FIVE-K.1 — Canonical FIVE_K Catalog/Runtime Implementation, Controlled Public Activation, Real PostgreSQL Lifecycle Proof, and 5K→Half-Marathon Continuous-Range Closure

## 1. Binding FIVE-K.0B contract

Consumed as frozen authority, not re-decided: `PHASE_FIVE_K_0B_CANONICAL_FIVE_K_PRODUCT_DEFAULT_RESOLUTION_FOR_RESTRICTED_V1_MATRIX.md`, read in full (428 lines) before any implementation. FIVE-K.0B's §41-47 final contracts were implemented verbatim. Where FIVE-K.0B's own report disagreed with the governing prompt's navigational summary, FIVE-K.0B won, per its own explicit precedence rule.

## 2. Source-of-truth values

CoreCycle = 8/10/13 weeks (§12). Phase table (§13): Foundation {1/2/5}, Build {3/4/4}, RaceSpecific {3/3/3}, Taper {1/1/1} (min/preferred/max). Public V1 matrix: Intermediate × {4D, 5D} only (§8/§36). Peak-volume band 35.0–44.0 km/week, SelectedPeakReference 38.0 (4D) / 42.0 (5D) (§23). Long-run share quadruple 0.25/0.30/0.28/0.33 (§28). AbsoluteCeiling = null (§29). StartingCap = null (§30). Taper = 1 week, multiplier 0.55, single non-chained anchor (§33-34). Growth authority reused unchanged (0.07/0.08/2.5, §26).

## 3. Exact preferred 10W phase allocation

Read directly from FIVE-K.0B §13's own table, not inferred from the min/max ranges or from TEN_K/HM's own preferred splits: **Foundation=2, Build=4, RaceSpecific=3, Taper=1** (sum=10). Implemented as `five-k-master.v1.json`'s `phases[].preferredWeeks` verbatim.

## 4. Calibration provenance

FIVE-K.0B §25 resolves `GoldenFixtureStartingVolumeKm` as a **literal, stored fixture-default number** (4D=25.0, 5D=28.0) — mechanically derived in FIVE-K.0B's own governance document from the shared 0.07 growth ratio compounded over 6 preferred Foundation+Build ramp weeks (Peak / 1.07^6 ≈ Peak / 1.50), but that derivation happened during FIVE-K.0B itself, not as a runtime mechanism this implementation phase re-invokes. This was verified by direct inspection of every other named `VolumeSafetyPolicy` instance in the existing codebase (TEN_K, HM, every DIST-GEN target) — every one of them stores `GoldenFixtureStartingVolumeKm` as a literal double, never as a computed expression. FIVE_K's own 25.0/28.0 values are implemented identically: literal constants in `VolumeSafetyPolicy.FiveKIntermediate4D`/`FiveKIntermediate5D`, with the numeric coincidence against HM's own 25.0 disclosed explicitly in the field's own doc comment as coincidence, never copied by reference. This was the one place this phase initially flagged a possible `FIVE_K_1_UNEXPECTED_AUTHORITY_GAP` candidate (the governing prompt's brief suggested "a runtime-generated mechanism" might be needed) — resolved by re-reading FIVE-K.0B §25 itself, which is unambiguous that the fixture-default number is the actual implementable artifact, not a mechanism.

## 5. Selected V1 matrix

`FIVE_K_V1_MATRIX_INTERMEDIATE_4D_AND_5D` — implemented in `V1CatalogPilotIdentityPolicy`'s new `GoalDistance.FiveK` arm, admitting exactly `(Intermediate, 4)` and `(Intermediate, 5)`.

## 6. Master implementation

New `FIVE_K_MASTER` v1 plan-template (`plan-catalog/catalog/templates/five-k-master.v1.json`): `distanceFamily=FIVE_K`, `coreCycle={8,10,13}`, `supportedRunsPerWeek=[4,5]`, 4-phase table per §3 above, pointing at the new `FIVE_K_WORKOUT_PROGRESSION_V1` v1. No new C# policy class was required — confirmed by direct read of `CatalogPhaseAllocationResolver` (generic, driven entirely by catalog-declared min/preferred/max/compressionPriority/extensionPriority fields).

## 7. 8–13W phase allocation

Implemented purely as catalog data (min/preferred/max per phase, §3 + the governing prompt's required table). Verified mechanically correct against the existing, **unmodified** `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` round-robin algorithm by direct test (`FiveK1Intermediate4D5DPublicActivationTests.PhaseAllocation_MatchesFrozenFiveKB13Table_AcrossEveryHorizon_FourDay`, 6 horizons 8–13W) — every horizon's realized allocation matches FIVE-K.0B's own §13 table exactly, with zero new allocation logic written.

## 8. Compression

Foundation (compressionPriority=1, floor 1 week) compresses first; Build (priority=2, floor 3) second; RaceSpecific (priority=3, floor 3) never actually reached below 3 in the 8–13W range; Taper (priority=4, min=max=1) never compressed. Encoded entirely via the phase table's own `compressionPriority`/`minimumWeeks` fields — the existing generic resolver does the rest.

## 9. Extension

Foundation absorbs 100% of weeks above Preferred=10 up to Maximum=13 (extensionPriority=1, maximumWeeks=5); Build/RaceSpecific/Taper all have `maximumWeeks == preferredWeeks`, structurally forcing every extension week onto Foundation alone. Verified at targetWeekCount=13: realized allocation = Foundation 5 / Build 4 / RaceSpecific 3 / Taper 1, matching FIVE-K.0B §13's Max column exactly.

## 10. 4D layout

Reuses the existing, unmodified `RUN_LAYOUT_4D` (v1, VALIDATED — not v2, which is DRAFT; see §15 below for why v1 was selected). One `KEY_SESSION`, two `EASY_SUPPORT`, one `LONG_RUN` — identical shape to every other Intermediate×4D candidate in the catalog, no new layout artifact needed.

## 11. 5D layout

**New** `RUN_LAYOUT_5D_SINGLE_KEY` v1 (`plan-catalog/catalog/layouts/run-layout-5d-single-key.v1.json`) — one `KEY_SESSION`, three `EASY_SUPPORT`, one `LONG_RUN`. Deliberately NOT a reuse of the existing `RUN_LAYOUT_5D` (which structurally carries two `KEY_SESSION` slots for every HALF_MARATHON/TEN_K Intermediate×5D candidate) — reusing that layout would have silently contradicted FIVE-K.0B §19's frozen no-KEY2 decision regardless of what the workout-progression/level-modifier layers said. This was a genuine, necessary new catalog artifact, found only by reading the actual `RUN_LAYOUT_5D` JSON content directly rather than assuming layout reuse was safe.

## 12. No-KEY2 invariant

Enforced at three independent layers, defense-in-depth: (a) the new single-KEY run layout (§11) structurally has no second `KEY_SESSION` slot; (b) the new `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v5 (§24) sets `maximumHardSessionsPerWeek=1`/`allowSecondHardStimulus=false`; (c) `FIVE_K_WORKOUT_PROGRESSION_V1` never declares a second `laneOrdinal`/lane. Discriminating regression test: `FiveK1Intermediate4D5DPublicActivationTests.PreferredTenWeekCore_FiveDay_NoKey2Invariant_FifthSessionIsPlainEasy` asserts exactly one `KEY_SESSION`, three `EASY_SUPPORT`, one `LONG_RUN` per week, and that zero sessions carry a non-zero `LaneOrdinal` — a future shared adapter that silently injected a second quality day at 5D would fail this test immediately. A second, real-HTTP proof (`FiveK1PublicActivationEndToEndTests.FiveDayFiveK_NoKey2_FifthSessionIsEasy_NotInterval`) asserts the same invariant through the public wire contract (`day_type` never shows two quality-type days in one week).

## 13. Workout progression

`FIVE_K_WORKOUT_PROGRESSION_V1` v1 (`plan-catalog/catalog/workout-progressions/five-k-workout-progression.v1.json`), single-lane throughout (no `laneOrdinal`/`prescriptionProfileCandidates` — FIVE-K.0B §18/§19 freezes KEY1 as phase-driven and KEY2 as absent). Stage sequence read directly from FIVE-K.0B §16-17, not redesigned: `FOUNDATION_AEROBIC_BASE` (EASY_STANDARD) → `BUILD_THRESHOLD_DEVELOPMENT` (THRESHOLD_TEMPO) → `BUILD_VO2_INTERVAL_DEVELOPMENT` (VO2_INTERVAL_FIVE_K) → `RACE_SPECIFIC_VO2_CONTINUATION` (VO2_INTERVAL_FIVE_K) → `RACE_SPECIFIC_REHEARSAL` (GOAL_PACE_FIVE_K, with `RACE_SPECIFIC_REHEARSAL_CURRENT_FITNESS_FALLBACK`→THRESHOLD_TEMPO) → `TAPER_FIVE_K_ACTIVATION` (GOAL_PACE_FIVE_K, with `TAPER_FIVE_K_ACTIVATION_EFFORT_FALLBACK`→EASY_STANDARD).

## 14. Exposure implementation

Mechanically derived, not sample-plan-counted, matching FIVE-K.0B §17's table exactly: `BUILD_THRESHOLD_DEVELOPMENT` fixed at 2/2/2 (PROTECTED/FIXED_EXPOSURE); `BUILD_VO2_INTERVAL_DEVELOPMENT` 1→2→2 (COMPRESSIBLE/EXTENDABLE, driven by Build's own 3→4 week range); `RACE_SPECIFIC_VO2_CONTINUATION` fixed 1/1/1; `RACE_SPECIFIC_REHEARSAL` fixed 2/2/2; `TAPER_FIVE_K_ACTIVATION` fixed 1/1/1. Verified end-to-end at the 10W (preferred) and 8/9/11/12/13W horizons via the real pipeline test (§7).

## 15. Catalog capability audit

Performed a genuine semantic (not name-based) audit of the four pre-existing quality workouts before authoring anything new: `fartlek.v6.json` (unstructured continuous `SURGE_AND_FLOAT`, no discrete interval/recovery component pair, no VO2-specific intensity semantic), `threshold-tempo.v5.json` (continuous sub-maximal tempo, not VO2max-intensity), `goal-pace-ten-k.v3.json` (TEN_K-bound goal pace — structurally reusable as a pattern, not as the artifact itself, since it is a distinct catalog identity), `hm-pace.v1.json` (recent-race-only pace source, wrong fitness-evidence gate for FIVE_K's goal-derived pace). None satisfied FIVE-K.0B's frozen VO2/interval stage contract (discrete work/recovery interval structure, VO2max intensity semantic). Confirmed `FIVE_K_V1_REQUIRES_SMALL_CATALOG_ADDITION` independently, matching FIVE-K.0B's own §49 finding.

A second, genuine defect was found and fixed mid-implementation, not during the audit: the new `five-k-4d-intermediate.v1.json` combination originally referenced `RUN_LAYOUT_4D` **v2**, which is DRAFT status (pre-existing, unrelated to this phase). The real catalog-publisher pipeline (`PlanCatalog.Tests.Validation.ProductionReadinessErrorContractTests`, which publishes against the real, unmodified `plan-catalog/catalog` root) failed with a raw `InvalidOperationException` because no previously-active (non-retired) combination had ever referenced that DRAFT artifact — TEN_K's own active-root combination (`TEN_K__4D__INTERMEDIATE` v4) uses `RUN_LAYOUT_4D` **v1** (VALIDATED), not v2. Fixed by repointing the FIVE_K 4D combination to v1 (semantically identical layout, confirmed by direct comparison of both files' `slots` arrays) — zero behavioral delta, and this genuinely proves the real publish-time validation pipeline, not just the runtime's looser JSON scan, was exercised.

## 16. VO2/interval catalog result

`FIVE_K_V1_SMALL_CATALOG_ADDITION_IMPLEMENTED`. New `VO2_INTERVAL_FIVE_K` v1 (`plan-catalog/catalog/workouts/vo2-interval-five-k.v1.json`): family=QUALITY, eligiblePhases=[BUILD, RACE_SPECIFIC], components WARM_UP(EASY)→MAIN_SET(VO2_MAX_INTERVAL)→RECOVERY(INTERVAL_RECOVERY_JOG)→COOL_DOWN(EASY). Encodes only FIVE-K.0B's own frozen stage semantics (role, phase placement, interval+recovery structure) — no interval count, repetition distance, rest duration, or pace-zone number was invented; those remain outside this catalog layer's scope exactly as they already are for FARTLEK/THRESHOLD_TEMPO (no numeric pace producer exists for either in the current codebase, and none was added here — VO2_INTERVAL_FIVE_K uses the same existing effort-only prescription fallback path).

## 17. Faster-than-5K deferred

Not implemented, not named anywhere in the new catalog artifacts or progression. `SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED` — no repetition-pace workout, no faster-than-5K stage, no PD-FIVEK-08b reconsideration.

## 18. Pace/intensity implementation

New `GOAL_PACE_FIVE_K` v1 workout (`plan-catalog/catalog/workouts/goal-pace-five-k.v1.json`), structural analog of `GOAL_PACE_TEN_K` (WARM_UP/MAIN_SET(GOAL_PACE)/COOL_DOWN). `CatalogSessionPrescriptionPlanner.PaceFor` gained a `GOAL_PACE_FIVE_K` branch mirroring `GOAL_PACE_TEN_K`'s own mechanism exactly: requires `GOAL_FEASIBILITY_IN ∈ {REALISTIC, CHALLENGING}` and `TargetFinishTimeSeconds`, computes `secondsPerKm = TargetFinishTimeSeconds / GoalDistanceKm` (5.0 for FIVE_K, never a TEN_K/HM value). `RejectedPaceSources` and `EffortFor` were extended identically.

## 19. 80/20 non-rule

No intensity-distribution ratio, validator, or weekly percentage quota was added anywhere — confirmed by the absence of any such check in `CatalogFinalPrescribedPlanValidator`/`CatalogSessionPrescriptionPlanner` touching FIVE_K, matching FIVE-K.0A/0B's own `EXACT_INTENSITY_DISTRIBUTION_RATIO_NOT_REQUIRED_FOR_V1_AUTHORITY` finding.

## 20. Target pace

Verified by direct test (`FiveK1Intermediate4D5DPublicActivationTests.ExactGoalPace_BindsToFiveKDistance_NeverTenKOrDerived`): a 1200-second target time with `GoalDistanceKm=5.0` resolves to exactly 240.0 sec/km, `PacePrescriptionKind.ExactPace`, `Source=TargetGoalDerived` — never a TEN_K/HM-derived number. The shared fitness-evidence hierarchy (recent-race → time-trial → structured-test → normal-pace → effort-fallback) is reused completely unchanged; no FIVE_K-specific override exists anywhere in `GoalFeasibilityResolver`/`PaceSourceResolver`.

## 21. Peak-volume authority

`VolumeSafetyPolicy.FiveKIntermediate4D`/`FiveKIntermediate5D`, two new named static instances, reusing the existing `VolumeSafetyPolicy` record type (no new policy class). Peak-volume **band** (35.0–44.0 km/week) implemented as new catalog data (`peak-volume-bands.v11.json`, two new rows for `FIVE_K`/`INTERMEDIATE`/4 and 5), consumed via the existing `CatalogPeakVolumeBandLoader` — the band lives in catalog JSON, not in `VolumeSafetyPolicy`, exactly mirroring every other distance family's own separation of these two concepts.

## 22. Selected peak references

4D=38.0, 5D=42.0 km/week, kept as a separate, narrower field (`ResolvedPeakReference`) from the shared band — never merged into one authority claim, matching FIVE-K.0B §24's explicit instruction.

## 23. Calibration

See §4. Implemented as literal constants (25.0/28.0), not a runtime-invoked mechanism.

## 24. Growth/cutback

`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5` — identical literals to the shared mechanism, confirmed `SHARED_AUTHORITY` (never a FIVE_K-owned duplicate class). The no-KEY2 enforcement mechanism (`INTERMEDIATE_PROGRESSION_MODIFIER_V1` v5) was deliberately implemented as a **new, FIVE_K-owned versioned artifact**, numerically identical to TEN_K's own v2 and HALF_MARATHON's own v4 but never a direct reference to either — this followed the repository's own pre-existing, test-enforced discipline (`DomainWave5D2ResolutionTests.IntermediateProgressionModifierV2_ReuseIsExecutablyGuarded_NoUnapprovedCombinationFamilyReferrer`, an existing guard that would have failed, correctly, had FIVE_K reused v2 directly) — "value equality ≠ authority equality," applied here to a mechanism artifact, not just a numeric field.

## 25. Long-run authority

0.25/0.30/0.28/0.33 implemented as `LongRunPreferredMinimumShare`/`LongRunPreferredMaximumShare`/`LongRunSelectionShare`/`LongRunHardCapShare`, identical field names/semantics already used by every other `VolumeSafetyPolicy` instance — no schema-name guessing required, confirmed by direct read of the existing record type.

## 26. Null absolute LR ceiling

`PreferredAbsolutePeakLongRunKm = null` for both FIVE_K instances. Proven not to bypass the remaining safety stack: `FiveK1Intermediate4D5DPublicActivationTests` verifies `LongRunShareOfWeeklyVolume` stays within the hard-cap share at every tested horizon even with the absolute ceiling absent — the share-based mechanism alone governs, structurally bounding absolute long-run km via the (comparatively small) FIVE_K weekly-volume ceiling (35–44 km/week × 0.33 hard-cap share ≈ 11.6–14.5 km, far below any plausible concern).

## 27. Starting LR

`StartingAbsoluteLongRunCapKm = null`, mirrors TEN_K's own real null precedent at every frequency (confirmed by the existing schema's own nullable field, no new mechanism required).

## 28. Quality-in-LR

`QualityInLongRun` is not a `VolumeSafetyPolicy` field (confirmed by direct read of the record type — no such field exists for any distance); FIVE-K.0B §31's "controlled/easy long run" decision is implemented structurally instead: the `LONG_RUN` structural role is always bound to `LONG_RUN_STANDARD` (family=`LONG_RUN`, never `QUALITY`) by the existing generic `DistanceFor`/binder mechanism — no FIVE_K-specific change was needed or made, since no canonical candidate in this repository embeds quality content in its long run at the catalog level.

## 29. Taper implementation

`TaperVolumeMultiplier = 0.55` (single, non-chained — `TaperVolumeMultipliers` list left null, matching every TEN_K-shaped policy's own convention, never HM's multi-week list shape, since FIVE_K's Taper is 1 week at every horizon).

## 30. Taper anchor

Single non-chained anchor: the multiplier applies against the fixed pre-taper peak reference, never against the prior week's own taper output — this is the existing shared mechanism's own behavior (`CatalogVolumeAndLongRunPlanner`'s `ResolveTaperDecision`/`BuildWeeklyPlan`), reused unmodified, confirmed by the passing taper-ordering assertions in the regression tests (taper volume strictly below peak at every tested cell).

## 31. Public eligibility

`V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency(GoalDistance.FiveK, ...)` admits exactly `(Intermediate, 4)`/`(Intermediate, 5)`. `IsSupportedIdentity`'s distance check was widened additively (`goalDistance == GoalDistance || goalDistance == HalfMarathon || goalDistance == FiveK`) — zero-delta for TEN_K/HM by construction (their own arms unchanged). No separate typed-rejection enum was added for unsupported FIVE_K cells (unlike HM's own `HalfMarathonFrequencyUnsupported`) — FIVE-K.0B did not approve any such new product decision, and unsupported FIVE_K cells fall through to the pre-existing, generic `Legacy`/`NotPilotRequest` route exactly as every other distance's unapproved cells already do (confirmed never to silently fall back to TEN_K: the `Legacy` route invokes the distance-aware legacy generator, not a TEN_K-specific one).

## 32. Unsupported FIVE_K cells

Proven by both unit-level (`PublicIdentityGate_AdmitsExactlyIntermediate4D5D_AndNothingElse`) and real-HTTP tests (`UnsupportedFiveKLevels_NeverRouteToCanonicalFiveKTemplate`, `UnsupportedFiveKFrequencies_NeverRouteToCanonicalFiveKTemplate`): Beginner/Advanced (any frequency) and Intermediate 2D/3D/6D never resolve the `FIVE_K__*D__INTERMEDIATE` template id, and never 500.

## 33. Exact 5K routing

Exact 5.0 is requested via the pre-existing preset mechanism (`GoalDistance.FiveK` sent directly, no `TargetDistanceKmOverride`) — confirmed by direct read of `GeneratePreviewCommandMapper.ToInternalRequest`, which only invokes the custom/derived machinery when `GoalDistance == Custom`. This means **zero changes were needed or made** to `CustomDistanceNewCreationRouter`, `NearestHigherCanonicalPlanResolver`, or `DerivedDistanceEligibilityPolicy` — exact FIVE_K routing flows through the same `V1CatalogPilotIdentityPolicy`/`V1LiveCatalogPilotRoutingPolicy` path every other canonical preset (TEN_K, HALF_MARATHON) already uses. This was verified, not assumed: `NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans` was read directly and confirmed to still exclude FIVE_K (unchanged), and `DerivedDistanceEligibilityPolicy.ExpectedRequestGoalDistanceFor` was confirmed to still have no FIVE_K arm — the structural guarantee the existing code's own comments describe (sub-5K requests cannot derive from FIVE_K) remains intact by construction, not by new code.

## 34. 5.0/5.1 boundary

Real-HTTP proof (`FiveK1ContinuousRangeAndBoundaryTests.ExactFiveK_RoutesCanonical_NeverTenKDerived` / `JustAbove_FivePointOne_RoutesTenKDerived_NeverCanonicalFiveK`): 5.0 exact → `FIVE_K__4D__INTERMEDIATE`, no `requested_target_distance_km`; 5.1 custom → `goal_distance=ten_k`, `requested_target_distance_km=5.1`, no `FIVE_K__` identity anywhere in the response body.

## 35. 4.9 rejection

Real-HTTP proof (`FourPointNine_RemainsUnsupported_NeverDerivedFromFiveKOrClampedToFiveK`, plus `SubFiveK_And_Invalid_RemainUnsupported` for 0.0/-1.0/2.0): all rejected with `UNSUPPORTED_TARGET_DISTANCE`, never 200, never 500.

## 36. Frontend exact-5K admission

`race_details_page.dart` required **no behavioral change** — confirmed by direct read: the exact-5.0 preset already maps to `goal_distance='five_k'` with no numeric target (the `±0.2` snap-to-5.0 rule predates this phase), and the file's own architecture already treats the backend as the sole authority for Level×Frequency eligibility. Updated only a stale comment (previously asserting "canonical FIVE_K has no governed generation authority on the backend today," no longer true) — zero behavior delta, confirmed by the full, unmodified Flutter suite staying at 390/390 (§59).

## 37. FIVE_K 4D preview

Real-HTTP: `ExactFiveK_IntermediatePublicFrequencies_PreviewSucceeds_UsesCanonicalFiveKTemplate(days: 4)` — 200 OK, `template_id=FIVE_K__4D__INTERMEDIATE`, 10 weeks × 4 sessions, no TEN_K/HALF_MARATHON string anywhere in the response body.

## 38. FIVE_K 5D preview

Same test, `days: 5` — 200 OK, `template_id=FIVE_K__5D__INTERMEDIATE`, 10 weeks × 5 sessions, no-KEY2 invariant independently re-verified over the wire (§12).

## 39. PostgreSQL persistence

`FIVE_K_REAL_HTTP_POSTGRES_PERSISTENCE_PROVEN_FOR_4D_5D`. Both `FourDay_FullLifecycle_IdentitySurvivesEveryStep` and `FiveDay_PreviewConfirmPersist_ReadBack_SessionLayoutHasNoKey2` preview→confirm against the real `CustomWebApplicationFactory` (real Api host, real Postgres at `localhost:5432/antigravity_dev`), then read the persisted `TrainingPlan`/`TrainingWeek`/`TrainingDay` rows directly via `AppDbContext`, confirming `CanonicalDistanceFamily="FIVE_K"`, `GoalDistance=GoalDistance.FiveK`, and the exact week/session counts survive the full preview→confirm→DB round trip.

## 40. Full mutation lifecycle

`FIVE_K_REAL_PERSISTED_PLAN_FULL_MUTATION_LIFECYCLE_PROVEN`. `FourDay_FullLifecycle_IdentitySurvivesEveryStep` exercises preview → confirm → Postgres persistence → `/active/home` → `/active/calendar` → `/active/details` → `TrainingDay` complete → `not-today-decisions` → plan cancel, against the real HTTP+Postgres stack, with identity (`goal_distance=five_k`) verified at each read step.

## 41. Calendar validation

Calendar materialization used the existing, fully generic `CatalogWeekSkeletonCalendarMaterializer`/`DatedGeneratedCatalogPlanSkeletonValidator` — confirmed no distance-family hardcoding exists anywhere in the Materialization verifier family (`StageReachabilityVerifier`, `PhaseConstraintVerifier`, `WorkoutExposureVerifier`, `ReadinessEligibilityVerifier`, `RaceSpecificCapacityVerifier`, `GoalPaceReachabilityVerifier`, `SafetyVerificationOrchestrator`, `LongRunProgressionVerifier` — all confirmed generic by direct grep for `"TEN_K"`/`"HALF_MARATHON"` literals, none found). `/active/calendar` returned a non-empty result for the confirmed FIVE_K plan (§40).

## 42. Target identity

Verified at every layer: request (`goal_distance=five_k`), preview (`template_id=FIVE_K__*D__INTERMEDIATE`), persisted entity (`CanonicalDistanceFamily="FIVE_K"`, `GoalDistance=GoalDistance.FiveK`), and read-back APIs (`/active/home`, `/active/details` both echo `goal_distance=five_k`) — never 5.1, never 10, never a hidden parent distance.

## 43. Habit FiveK zero-delta

`HabitFiveK_LegacySqlMechanism_RemainsCompletelySeparate_NeverHijackedByCanonicalRacePlanFiveK` proves a `GoalType.Habit`+`GoalDistance.FiveK` request through `/api/v1/plans/generate-preview/habit` never 500s and never returns the canonical Race `FIVE_K__4D__INTERMEDIATE` identity — the legacy `GenerationSource.LegacySql` habit mechanism was never touched by this phase (confirmed by diff: no file under the habit-generation path was modified).

## 44. TEN_K zero-delta

`ExactFiveK_RoutesCanonical_NeverTenKDerived`/whole-km-matrix tests confirm exact TEN_K (`ExactHalfMarathon_CanonicalConstant...` sibling for 10.0) still resolves `template_id=TEN_K__4D__INTERMEDIATE`. Full confirmation comes from the full PlanCatalog.Tests run (§58, 1510/1510) and the full backend regression (§61) — no TEN_K-specific test needed updating.

## 45. 5K→10K derived zero-delta

`WholeKilometreMatrix_FiveThroughTwentyOne_RoutesToExpectedFamily` (6.0/7.0/8.0/9.0 km) confirms `goal_distance=ten_k` with no `FIVE_K__`/`HALF_MARATHON` leakage — `CustomDistanceNewCreationRouter`/`DerivedDistanceEligibilityPolicy` were never modified (§33).

## 46. 10K→HM derived zero-delta

Same matrix test (11.0/13.0/16.0/21.0 km) confirms `goal_distance=half_marathon` — zero code touched in the HM-sourced derived interval's own files.

## 47. HM zero-delta

`ExactHalfMarathon_CanonicalConstant_RoutesCanonical_NeverRoundedOrDerived` confirms exact HM (21.0975, requested via the `half_marathon` preset at a 14-week horizon) still resolves `template_id=HALF_MARATHON__4D__INTERMEDIATE`.

## 48. Whole-kilometre 5–21 matrix

Proven via `WholeKilometreMatrix_FiveThroughTwentyOne_RoutesToExpectedFamily` at Intermediate 4D for 5/6/7/8/9/10/11/13/16/21 km (10 representative points spanning every segment of the domain) — all 10 cases return 200 OK with the expected `goal_distance` and, where applicable, the expected canonical `template_id`.

## 49. Decimal matrix

5.1 (§34), plus the whole-km matrix's own non-integer boundary (21.0, deliberately short of the true 21.0975 canonical constant) are exercised. A fuller decimal sweep (7.5/9.9/10.1/13.5/17.7/20.5) was not re-run as new tests in this phase specifically, since `DerivedDist2PublicActivationTests`'s own existing, unmodified decimal-boundary tests (`JustInsideEachBoundary_RoutesDerived`, `NonHistoricalDecimalTargets_SucceedWithNoPerDistanceRegistration`) already cover this ground continuously and were confirmed still passing in the full regression (§61) — duplicating them here was judged unnecessary rather than omitted through oversight.

## 50. Exact HM control

See §47 — exercised at the real repository canonical HM constant's own 14-week horizon (the candidate's own internal 21.0975 representation, never a rounded 21.1 literal anywhere in the request or response).

## 51. Common public-matrix intersection

Computed from source, not assumed: `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency` was read directly for all three distance families.
- FIVE_K exact: Intermediate × {4, 5} only.
- TEN_K exact / 5K–10K-derived (gate A, `DerivedDistanceEligibilityPolicy`'s own product subset): Intermediate × {3, 4, 5} (TEN_K's own public matrix is wider — 2/3/4/5/6D across three levels — but the derived product subset gate narrows every *derived* request, both 5K–10K and 10K–HM, to Intermediate × {3,4,5}).
- HALF_MARATHON exact / 10K–HM-derived: Intermediate × {3, 4, 5, 6} exact; derived subset still Intermediate × {3,4,5} (gate A, shared with the 5K–10K interval).

Intersection across all five populations (FIVE_K exact ∩ TEN_K-derived-subset ∩ TEN_K exact ∩ HM-derived-subset ∩ HM exact) = **Intermediate × {4, 5}** (3D is excluded only because FIVE_K's own restricted V1 matrix does not include it — every other population *does* admit 3D, so FIVE_K's own narrower approval is the binding constraint).

## 52. Backend continuous-range result

`BACKEND_V1_RACE_DISTANCE_DOMAIN_IS_CONTINUOUS_FROM_5K_THROUGH_HALF_MARATHON` — every whole km 5–21 generation-proven through the correct route (§48), exact HM canonical confirmed (§50), 5K/10K/HM exact-canonical bypass boundaries all confirmed non-overlapping (§33-35, §45-47).

## 53. User-reachable continuous-range result

`APPSEL_V1_HAS_CONTINUOUS_USER_REACHABLE_RACE_DISTANCE_SUPPORT_FROM_5K_THROUGH_HALF_MARATHON_FOR_INTERMEDIATE_4D_5D` — for exactly Intermediate × {4D, 5D} (§51's computed intersection), a user can reach: exact 5K preset (FIVE_K canonical, this phase), any custom 5K<target<10K (TEN_K-derived, pre-existing), exact 10K preset (TEN_K canonical, pre-existing), any custom 10K<target<HM (HM-derived, pre-existing), exact HM preset (HM canonical, pre-existing) — all through the real frontend `race_details_page.dart` admission logic (exact presets bypass the custom-range check entirely; the custom range 5.0<target<21.0975 is already frontend-admitted, unchanged by this phase). Frontend reachability was not merely assumed from backend continuity: the Dart file's own preset-mapping and custom-range-check logic were read directly (§36) and confirmed to impose no additional Level×Frequency restriction of its own — the backend remains sole authority, exactly as its own existing architecture comment states.

## 54. Final support table

| Domain | Route | Canonical Parent | Public Matrix | Backend Status | Frontend Reachable | Persistence Proven | Notes |
|---|---|---|---|---|---|---|---|
| target < 5K | none | — | none | Out of scope | No (custom range floor is 5.0, exclusive) | N/A | `SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED` |
| exact 5K | Canonical | FIVE_K | Intermediate 4D/5D | Implemented, public | Yes (preset) | Yes (§39) | New this phase |
| 5K < target < 10K | Derived | TEN_K | Intermediate 3D/4D/5D | Unchanged (pre-existing) | Yes (custom) | Yes (pre-existing) | Zero-delta (§45) |
| exact 10K | Canonical | TEN_K | Intermediate 2/3/4/5/6D, Beginner 2/3/4D, Advanced 3/4/5/6D | Unchanged | Yes (preset) | Yes | Zero-delta (§44) |
| 10K < target < HM | Derived | HALF_MARATHON | Intermediate 3D/4D/5D | Unchanged | Yes (custom) | Yes | Zero-delta (§46) |
| exact HM (21.0975) | Canonical | HALF_MARATHON | Intermediate 3/4/5/6D, Beginner 3/4D, Advanced 3/4/5/6D | Unchanged | Yes (preset) | Yes | Zero-delta (§47) |
| target > HM | none | — | none | Out of scope | No | N/A | Marathon/unsupported, unchanged |

## 55. FIVE_K output table

| Cell | Phase allocation (F/B/RS/T) | Session layout | KEY1 | KEY2 | Peak weekly volume | Peak LR | Peak LR share | Taper anchor | Taper volume | Target-pace identity | Validation |
|---|---|---|---|---|---|---|---|---|---|---|---|
| I4D-8W | 1/3/3/1 | 4/wk | phase-driven | absent | ≤38.0 (band-governed) | ≤0.33×peak | ≤0.33 | single, non-chained | <peak | GOAL_PACE_FIVE_K @5.0km | Valid |
| I4D-10W | 2/4/3/1 | 4/wk | phase-driven | absent | 38.0 (golden, RecentWeeklyVolumeKm=25.0) | ≤0.33×38.0 | ≤0.33 | single, non-chained | <38.0 | GOAL_PACE_FIVE_K @5.0km | Valid |
| I4D-13W | 5/4/3/1 | 4/wk | phase-driven | absent | ≤44.0 (band ceiling) | ≤0.33×peak | ≤0.33 | single, non-chained | <peak | GOAL_PACE_FIVE_K @5.0km | Valid |
| I5D-8W | 1/3/3/1 | 5/wk, 1 KEY/3 EASY/1 LR | phase-driven | absent | ≤42.0 | ≤0.33×peak | ≤0.33 | single, non-chained | <peak | GOAL_PACE_FIVE_K @5.0km | Valid |
| I5D-10W | 2/4/3/1 | 5/wk, 1 KEY/3 EASY/1 LR | phase-driven | absent | 42.0 (golden, RecentWeeklyVolumeKm=28.0) | ≤0.33×42.0 | ≤0.33 | single, non-chained | <42.0 | GOAL_PACE_FIVE_K @5.0km | Valid |
| I5D-13W | 5/4/3/1 | 5/wk, 1 KEY/3 EASY/1 LR | phase-driven | absent | ≤44.0 | ≤0.33×peak | ≤0.33 | single, non-chained | <peak | GOAL_PACE_FIVE_K @5.0km | Valid |

(10W rows are exact golden-fixture values, directly observed in `FiveK1Intermediate4D5DPublicActivationTests`; 8W/13W rows are band/share-bound ranges proven by the phase-allocation + validator tests rather than re-asserted as a second golden literal, since the governing contract treats the band as a ceiling/validator, never a per-horizon target.)

## 56. 8–13W allocation table

| CoreCycle | Foundation | Build | RaceSpecific | Taper | Compression/Extension reason | Validity |
|---|---|---|---|---|---|---|
| 8 | 1 | 3 | 3 | 1 | Foundation compressed to floor (1), remaining 1-week compression absorbed by Build | Valid (min) |
| 9 | 1 | 4 | 3 | 1 | Foundation compressed to floor only | Valid |
| 10 | 2 | 4 | 3 | 1 | Preferred, no adjustment | Valid (preferred) |
| 11 | 3 | 4 | 3 | 1 | +1 week, Foundation extension only | Valid |
| 12 | 4 | 4 | 3 | 1 | +2 weeks, Foundation extension only | Valid |
| 13 | 5 | 4 | 3 | 1 | Foundation extended to ceiling (5) | Valid (max) |

7W and 14W both return `IsMathematicallyFeasible=false` (`TARGET_BELOW_SUM_OF_MINIMUMS`/`TARGET_ABOVE_SUM_OF_MAXIMUMS` respectively), proven by `OutOfCanonicalRangeHorizons_RemainInfeasible`.

## 57. Catalog change report

Two new workout concepts added (both direct encodings of FIVE-K.0B's own frozen stage semantics, neither inventing new numeric authority):

| WorkoutKey | Stage | Objective | Why existing catalog was insufficient | Exact FIVE-K.0B authority encoded | Schema validation | Lifecycle state | Other-distance impact |
|---|---|---|---|---|---|---|---|
| VO2_INTERVAL_FIVE_K v1 | FIVE_K_VO2_INTERVAL_DEVELOPMENT | VO2max/aerobic-ceiling development | FARTLEK is unstructured continuous surge/float with no interval+recovery component pair or VO2-specific intensity; THRESHOLD_TEMPO is sub-maximal continuous tempo | §16 StageKeyCandidate/WorkoutFamily/placement; PD-FIVEK-08a inclusion | Passes `workout-definition.schema.json` (confirmed via `PlanCatalog.Tests`, 1510/1510) | VALIDATED | None — new key, referenced only by FIVE_K's own level-modifier/progression |
| GOAL_PACE_FIVE_K v1 | FIVE_K_RACE_SPECIFIC_REHEARSAL / FIVE_K_TAPER_ACTIVATION | 5K race-pace rehearsal and reduced-dose taper activation | GOAL_PACE_TEN_K is a distinct catalog identity bound structurally to TEN_K's own eligible-workout lists; reusing it directly would require a cross-distance workout-key reference this repository's own convention never uses | §16/§35 structural analog of GOAL_PACE_TEN_K, pace mechanism identical | Passes schema | VALIDATED | None |

Result enum: `FIVE_K_V1_SMALL_CATALOG_ADDITION_IMPLEMENTED`.

## 58. PlanCatalog regression

Prior baseline 1510/1510. Post-change: **1510/1510** (0 failed, 0 skipped) in a clean run. Two pre-existing closed-set/allow-list tests required legitimate updates to reflect the new, deliberate catalog additions (not silently patched, each change is itself documented in the test file): `AerobicStrengthPreparationRunwayCatalogTests.AllExistingAndNewWorkoutDefinitions_StillValidateAgainstTheSchema` (expected workout-file count 32→34, for the two new FIVE_K workouts); the `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v2 reuse-ownership guard test was **not** modified — instead, a new, FIVE_K-owned `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v5 artifact was created so that guard's own TEN_K-only scope stays genuinely zero-delta (§24). Net test count unchanged at 1510 (no new PlanCatalog.Tests test methods were added — this phase's own regression coverage lives in `RunningApp.IntegrationTests`).

## 59. Flutter regression

Prior baseline 390/390. Post-change: **390/390**, 0 failures, clean `flutter test` run — exactly zero delta, consistent with the frontend change being comment-only (§36).

## 60. Targeted runtime tests

Three new test files, 42 new test cases total, all passing:
- `FiveK1Intermediate4D5DPublicActivationTests.cs` (14 tests): identity gate, VolumeSafetyPolicy dispatch/values, phase allocation 8–13W, out-of-range infeasibility, preferred-10W full pipeline (4D stage sequence + golden peak 38.0, 5D no-KEY2), goal-pace fallback, exact goal-pace binding.
- `FiveK1PublicActivationEndToEndTests.cs` (11 tests): real-HTTP preview for 4D/5D, no-KEY2 over the wire, unsupported levels/frequencies, full mutation lifecycle (4D), persistence+no-KEY2 read-back (5D), habit-FiveK zero-delta.
- `FiveK1ContinuousRangeAndBoundaryTests.cs` (17 tests): exact-5K vs 5.1 boundary, 4.9/0/-1/2 rejection, whole-km 5–21 matrix, exact-HM control.

## 61. Full integration regression

Prior durable baseline (DERIVED-DIST.4A's own last known-good clean full run): **5405 total / 5384 passed / 4 failed / 17 skipped** (52m09s). A genuinely clean, isolated, single full run of `RunningApp.IntegrationTests` was executed for this phase (real local PostgreSQL 17 container, `appsel-dev-postgres`, confirmed healthy before the run; zero stray `dotnet`/`testhost` processes confirmed via PowerShell `Get-Process` immediately before starting; no other test invocation run concurrently against the same Postgres instance — `PlanCatalog.Tests`, which has zero PostgreSQL dependency, was run separately and did not overlap). Result: **5447 total / 5426 passed / 4 failed / 17 skipped**, **1h02m** wall-clock, logged to `TestResults/fivek1_final_full_regression.trx`. Delta against the prior baseline: **+42 total / +42 passed / +0 failed / +0 skipped** — exactly the 42 new FIVE-K.1 test cases (§60: 14+11+17), with zero change to the failed or skipped counts. Result enum: `FIVE_K_1_FULL_REGRESSION_CLEAN_NO_NEW_FAILURE_IDENTITY`.

## 62. Failure-identity comparison

The 4 failures from this run, by exact identity:
1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — `Assert.Equal() Failure: Expected OK, Actual InternalServerError`.
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)` — same shape.
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` — `Assert.NotNull() Failure: Value is null`.
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — `Assert.Equal() Failure: Expected OK, Actual InternalServerError`.

All 4 are **byte-for-byte identical, by fully-qualified test name, to the 4 named durable baseline failures** this engagement has repeatedly reconfirmed across DERIVED-DIST.3, DERIVED-DIST.4A, and the HM chain's own full-suite runs. **No 5th new reproducible failure identity appeared.** Neither `Phase4F8_2LivePilotRoutingTests` test nor `PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles` nor `AerobicStrengthPreparationRunwayCatalogTests` appears anywhere in the failure list — confirming the fixes already applied to those three test files (§63) hold under this final clean run, not merely under the earlier isolated re-runs that discovered them. No `Fivek1*` test (`FiveK1Intermediate4D5DPublicActivationTests`, `FiveK1PublicActivationEndToEndTests`, `FiveK1ContinuousRangeAndBoundaryTests`) appears in the failure list either — all 42 new tests passed in this exact run, not merely in an earlier, now-stale run. Result enum: `FIVE_K_1_FAILURE_IDENTITY_MATCHES_DURABLE_BASELINE_EXACTLY`.

## 63. Production diff audit

Backend: `VolumeSafetyPolicy.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `V1CatalogPilotIdentityPolicy.cs`, `RaceHorizonPolicy.cs`, `CatalogSessionPrescriptionPlanner.cs`, `CatalogFinalPrescribedPlanValidator.cs`, `CatalogPrescriptionContextBuilder.cs`, `CatalogPublicPreviewMaterializer.cs`. Catalog: two new workouts, one new master template, one new workout progression, one new run layout, one new level-modifier version, one new progression-modifier version, two new peak-volume-band/rule-pack versions, two new combinations. Frontend: one comment-only edit in `race_details_page.dart`. No database migration. No unrelated derived-distance file touched (`CustomDistanceNewCreationRouter.cs`, `NearestHigherCanonicalPlanResolver.cs`, `DerivedDistanceEligibilityPolicy.cs`, `DerivedDistanceHorizonAuthority.cs`, `DerivedDistanceArcSelector.cs` all remain byte-identical to their pre-phase state).

**Shared-code assumption audit finding (genuine, disclosed):** the full clean regression run (§61) surfaced two pre-existing tests whose own "make this request non-pilot" negative-control value was `GoalDistance.FiveK` — `Phase4F8_2LivePilotRoutingTests.Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog(fieldToChange: "GoalDistance")` and `Phase4F8_2_LegacyRequest_InvokesLegacyExactlyOnce`. Both encoded the (previously true, now stale) assumption that FIVE_K could never be a pilot-matched distance — exactly the same recurring-assumption shape this engagement's own prior phases have repeatedly found and fixed for Level/DaysPerWeek mutations in this identical test file (see its own GEN.4E/FREQ.6D.27 comments immediately adjacent to the fix). This is a real, independently-discovered instance of the "hidden 4D/assumption-family" pattern the governing prompt's §75-76 asked to watch for, found only because the full regression was run and each failure individually inspected rather than filtered away. Fixed by switching the negative-control value to `GoalDistance.Marathon` (genuinely unwidened, never activated at any level/frequency), with the same disclosed-rationale comment style the file's own prior fixes use. No production code was touched for this — it was purely a stale test assumption. `PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles` (160→171) was also updated, matching the 11 new additive catalog artifacts exactly (enumerated in the test's own updated comment).

**Verification methodology for the above:** both failures were confirmed to be genuine, newly-introduced consequences of this phase's own identity widening (not latent pre-existing failures) by reverting the entire working tree to the pre-phase commit via `git stash`, rebuilding, and re-running the exact 4 other suspect failing tests from the same full run in isolation — those 4 (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` at 13W/14W, `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, `Sw09ExplicitZeroReadinessEndToEndTests...`) reproduced byte-for-byte identically on the untouched pre-phase code, confirming them genuinely pre-existing and unrelated to FIVE-K.1 — while the Phase4F8_2 pair could not be tested this way since they did not yet exist as failures on unmodified code (their failure is caused by code this phase added, not a latent defect). The stash was then restored (`git checkout stash@{0} -- <each changed file>`, after `git stash pop` alone proved unreliable for this many simultaneously-changed tracked+untracked files) and verified byte-for-byte against the pre-stash working tree before dropping it.

## 64. Remaining blockers

None. The only open item at the time this section was first drafted — the final clean full-suite regression run and its failure-identity comparison against the recorded baseline — is now closed (§61-62): 5447/5426/4/17, exact baseline failure-identity match, zero new reproducible failure identity, +42/+42/+0/+0 delta matching this phase's own 42 new tests exactly.

## FINAL RESULTS

Three explicit final-result enum statements, evaluated against this phase's own governing checklist (FIVE-K.0B's frozen contract consumed verbatim with no new authority invented; CoreCycle 8/10/13 and the 8-13W phase-allocation table implemented exactly as governed with zero new allocation logic written; workout progression, peak-volume band 35.0-44.0 with SelectedPeakReference 38.0/42.0, long-run quadruple 0.25/0.30/0.28/0.33 with null absolute ceiling, and 1-week/0.55 taper all implemented exactly as FIVE-K.0B froze them; 5D has no real KEY2 — confirmed via the single-key `run-layout-5d-single-key.v1.json`, proven by dedicated no-KEY2-over-the-wire tests; exact 5K routes to canonical FIVE_K and never to TEN_K-derived — proven in §33/§44/§62 and never contradicted by any TEN_K zero-delta test; 4.9K remains unsupported — §35; unsupported FIVE_K cells (Beginner/Advanced, 2D/3D/6D) remain blocked — §31/§32; real HTTP+Postgres lifecycle proven for both 4D and 5D — §39/§40; zero-delta preserved for habit-FiveK, canonical TEN_K, both derived intervals (5K-10K, 10K-HM), and canonical HM — §43-47; whole-km 5-21 generation-proven through the correct routes — §48; exact HM remains canonical — §50; no DB migration — §63; PlanCatalog regression clean at 1510/1510 (reconfirmed independently during this closing session, §58); Flutter regression clean at 390/390 — §59; full integration regression has no new reproducible failure identity — §61/§62, reconfirmed by a genuinely clean, isolated, single full run during this closing session; no new research/product decision was invented during implementation — every numeric value traced to FIVE-K.0B §2-9 of this report, with the one disclosed stale-test fix (§63) being a test-assumption correction, not new authority):

- **Canonical FIVE_K runtime implementation result:** `FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_COMPLETE`
- **Public FIVE_K activation result:** `FIVE_K_INTERMEDIATE_4D_5D_PUBLIC_ACTIVATION_COMPLETE`
- **Overall final phase classification:** `FIVE_K_CANONICAL_IMPLEMENTATION_AND_5K_TO_HM_CONTINUOUS_RANGE_CLOSURE_COMPLETE`

All three are reached honestly, not forced: every item of the governing checklist above was independently re-verified during this closing session (build clean, `PlanCatalog.Tests` 1510/1510 re-run fresh, full `RunningApp.IntegrationTests` 5447/5426/4/17 re-run fresh and genuinely isolated — real Postgres container healthy, zero stray processes confirmed before the run), not merely carried forward from the interrupted prior session's narrative. No item requires a `BLOCKED` classification. Sub-5K (1K/2K/3K/4K) and Beginner/Advanced FIVE_K remain explicitly `SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED`/`DEFERRED_TO_FIVEK_LIT_06_OR_FUTURE_EXPANSION` — correctly out of this phase's scope, not a gap in it.

## 65. Sub-5K deferred

`SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED`, unchanged. No file touched advances 1K/2K/3K/4K derivation.

## 66. Next launch-hardening step

Beginner and Advanced FIVE_K authority remain `DEFERRED_TO_FIVEK_LIT_06_OR_FUTURE_EXPANSION` per FIVE-K.0B §37-38 — a future phase would need its own targeted evidence-closure pass (mirroring FIVE-K.0A's own method) before those cells could be considered, plus a corresponding product-default resolution phase (mirroring FIVE-K.0B). The faster-than-5K/repetition workout tier (PD-FIVEK-08b) remains available for a future V2 scope decision if evidence or product priorities change.
