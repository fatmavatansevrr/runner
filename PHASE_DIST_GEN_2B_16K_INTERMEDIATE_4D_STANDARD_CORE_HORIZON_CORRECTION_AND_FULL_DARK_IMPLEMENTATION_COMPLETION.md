# PHASE DIST-GEN.2B — 16K INTERMEDIATE 4D STANDARD CORE HORIZON CORRECTION AND FULL DARK IMPLEMENTATION COMPLETION

Narrow, mechanical implementation-correction phase closing the DIST-GEN 16K
dark-pilot engagement's Standard Core (10-14W) scope. This phase consumes the
DIST-GEN.2A governance correction (MinimumCoreWeeks 8 → 10) and implements it
in code, then re-proves the full supported horizon range against the real,
unmodified production pipeline.

---

## 1. DIST-GEN.2A authority consumed

DIST-GEN.2A (`PHASE_DIST_GEN_2A_16K_INTERMEDIATE_4D_EIGHT_TO_NINE_WEEK_CORE_HORIZON_AND_STRUCTURAL_COMPRESSION_AUTHORITY_RESOLUTION.md`)
is a pure governance/no-code phase. Its ruling, consumed verbatim by this
phase:

- `TargetDistance16KHorizonPolicy.MinimumCoreWeeks` corrected from **8 to 10**.
- `PreferredCoreWeeks` (12) and `MaximumCoreWeeks` (14) re-confirmed
  unchanged — no other authority in DIST-GEN.1 is disturbed.
- 8W and 9W dark 16K requests are explicitly placed OUT of Standard Core
  scope and deferred to a future, separately-governed "compressed core"
  track — not silently unsupported forever — mirroring this repo's existing
  HM-X2 precedent (HALF_MARATHON's own 7-9W deferral).
- No catalog JSON, no generic allocator code
  (`CatalogPhaseAllocation.cs`, `ProgressionStageAllocator.cs`,
  `DynamicCoreWeekSkeletonOrchestrator.cs`), no master template, and no other
  horizon authority (TEN_K's 8/12/14 fallback arm, HALF_MARATHON's own
  10/14/16 triple) is touched by DIST-GEN.2A's own ruling.

## 2. DIST-GEN.2 blocked state consumed

DIST-GEN.2 (`PHASE_DIST_GEN_2_16K_INTERMEDIATE_4D_FULL_DARK_TARGET_DISTANCE_PROJECTION_IMPLEMENTATION.md`)
implemented the full dark 16K pilot under the (since-corrected)
MinimumCoreWeeks=8 authority, and discovered a real, unmodified-allocator
exception when 8W was requested:

```
DynamicCoreWeekSkeletonInfeasibleException: TARGET_BELOW_SUM_OF_MINIMUMS (sumOfMinimums=10)
```

This occurs because the HALF_MARATHON master template
(`plan-catalog/catalog/templates/half-marathon-master.v5.json`), structurally
reused unmodified by the 16K pilot, has real phase minimums
(FOUNDATION=2, BUILD=3, RACE_SPECIFIC=3, TAPER=2 = 10) that 8 weeks cannot
satisfy. DIST-GEN.2 closed this honestly as `..._BLOCKED` rather than force a
value that could never materialize. DIST-GEN.2 also fixed a real,
pre-existing, unrelated defect in
`CatalogPlanConfirmationService.BuildCatalogTrainingPlan` (it used to
unconditionally collapse `RequestedTargetDistanceKm` into `GoalDistanceKm`
for every confirmed plan). That fix is **untouched** by this phase — verified
below in §28.

## 3. Exact code correction (diff)

The sole intentional production-code change, in
`backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/TargetDistance16KHorizonPolicy.cs`:

```diff
 /// <summary>
 /// PHASE DIST-GEN.2 — the EXPLICIT, dedicated 16K core-horizon authority,
-/// freezing DIST-GEN.1 §9/§10's own evidence-derived triple
+/// originally freezing DIST-GEN.1 §9/§10's own evidence-derived triple
 /// (MinimumCoreWeeks=8, PreferredCoreWeeks=12, MaximumCoreWeeks=14).
+/// PHASE DIST-GEN.2A corrected MinimumCoreWeeks to 10 (PreferredCoreWeeks=12
+/// and MaximumCoreWeeks=14 re-confirmed unchanged) after DIST-GEN.2 found the
+/// original 8 could never materialize against the reused HALF_MARATHON
+/// catalog identity's own real phase-allocation minimum-week sum of 10; see
+/// <see cref="MinimumCoreWeeks"/>'s own doc-comment for the full chronology.
+/// 8-9W dark 16K requests are deferred to a future compressed-core track, not
+/// silently unsupported-forever.
 ///
 ... (unchanged class-level remarks about RaceHorizonPolicy fallback-arm avoidance) ...
 /// </summary>
 public static class TargetDistance16KHorizonPolicy
 {
-    /// <summary>DIST-GEN.1 §10 — real, on-point 10-mile/16.09km evidence-derived minimum.</summary>
-    public const int MinimumCoreWeeks = 8;
+    /// <summary>
+    /// DIST-GEN.1 §10 originally froze this at 8 from real, on-point
+    /// 10-mile/16.09km evidence. DIST-GEN.2 discovered that value could never
+    /// actually materialize against the reused, unmodified HALF_MARATHON
+    /// catalog identity's own real phase-allocation minimum-week sum
+    /// (FOUNDATION=2 + BUILD=3 + RACE_SPECIFIC=3 + TAPER=2 = 10), and closed
+    /// honestly as blocked rather than force a value that could never
+    /// materialize. DIST-GEN.2A is the governing correction: the standalone
+    /// dark 16K Standard Core horizon authority is corrected to 10, mirroring
+    /// this reused identity's real minimum; 8-9W are deferred to a future,
+    /// separately-governed compressed-core track (see this repo's own
+    /// HM-X2 precedent for HALF_MARATHON's own 7-9W deferral).
+    /// </summary>
+    public const int MinimumCoreWeeks = 10;
 
     /// <summary>DIST-GEN.1 §10 — preferred core length.</summary>
     public const int PreferredCoreWeeks = 12;
```

`PreferredCoreWeeks`/`MaximumCoreWeeks` byte-identical; no other line in the
file changed. `PlanServices.cs`'s dark-16K rejection branches (lines ~161-195)
already read `TargetDistance16KHorizonPolicy.MinimumCoreWeeks`/`MaximumCoreWeeks`
generically (no hardcoded "8"), so no change was needed there — verified by
`git status --porcelain` showing zero diff on `PlanServices.cs`.

Companion test-only changes in
`backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark16KTargetDistanceProjectionTests.cs`
(full detail in §40).

## 4. Final horizon authority (10/12/14)

```
MinimumCoreWeeks  = 10   (was 8)
PreferredCoreWeeks = 12  (unchanged)
MaximumCoreWeeks   = 14  (unchanged)
```
Confirmed by `HorizonPolicy_16K_ExactFrozenBounds` (passing, updated).

## 5. 8W typed rejection (proof)

Real pipeline (`PlanServices.GeneratePreviewAsync`), 8-week dark 16K request:

```
RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds(weeks: 8) → PASSED
```

Throws `PlanCoreHorizonTooShortException` with message:
```
The available race-plan horizon is shorter than the supported dark 16K target-distance standalone core minimum (10 weeks). Available horizon is approximately 8 weeks. Reason: DARK_16K_CORE_HORIZON_8_NOT_SUPPORTED.
```
This now happens in `PlanServices.GeneratePreviewAsync` BEFORE
`CatalogPhaseAllocationResolver`/`DynamicCoreWeekSkeletonOrchestrator` is ever
reached — the horizon-gate classification itself is `BelowMinimum` for 8W
under the corrected 10-week minimum (`HorizonPolicy_16K_ClassifiesExactlyAtItsOwnFrozenBoundaries(8, BelowMinimum)` — passing).

## 6. 9W typed rejection (proof)

```
RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds(weeks: 9) → PASSED
```
Same `PlanCoreHorizonTooShortException`/`DARK_16K_CORE_HORIZON_9_NOT_SUPPORTED`
shape, same layer (horizon gate, pre-allocator).

## 7. 10W materialization (full detail)

Real, unmodified production pipeline
(`DynamicCoreCalendarMaterializationOrchestrator` → ... →
`DynamicCoreWeekSkeletonOrchestrator`/`CatalogPhaseAllocationResolver`),
candidate `HALF_MARATHON__4D__INTERMEDIATE`, `TargetWeekCount=10`.

- `CanonicalDistanceFamily` = `HALF_MARATHON`
- `RequestedTargetDistanceKm` = `16.0`
- Candidate identity: `V1CatalogPilotIdentityPolicy.HalfMarathonFourDayIntermediateCandidateKey` @ its frozen version (unchanged catalog identity, zero JSON delta)
- Phase allocation: **FOUNDATION=2 / BUILD=3 / RACE_SPECIFIC=3 / TAPER=2** (sum=10, exactly the reused catalog identity's own real minimum)
- Stage occupancy: 4 sessions/week × 10 weeks = 40 sessions, `ValidationResult.IsValid=true`
- Weekly volume vector (real observed run):

| Wk | Phase | Vol (km) | LR (km) | LR share | Taper |
|----|-------|---------:|--------:|---------:|-------|
| 01 | FOUNDATION | 20.0 | 6.5 | 32.5% | No |
| 02 | FOUNDATION | 21.5 | 7.0 | 32.6% | No |
| 03 | BUILD | 23.0 | 7.5 | 32.6% | No |
| 04 | BUILD | 24.5 | 8.0 | 32.7% | No |
| 05 | BUILD | 26.5 | 8.5 | 32.1% | No |
| 06 | RACE_SPECIFIC | 28.0 | 9.0 | 32.1% | No |
| 07 | RACE_SPECIFIC | 29.5 | 11.0 | 37.3% | No |
| 08 | RACE_SPECIFIC | 31.0 | 12.0 | 38.7% | No |
| 09 | TAPER | 21.5 | 7.0 | 32.6% | Yes |
| 10 | TAPER | 13.5 | 4.5 | 33.3% | Yes |

- Peak weekly volume: **31.0 km** — well inside the [34,46] band's floor is
  not required to be hit at this shorter horizon; still `<= 40.0` selected
  ceiling, never `43.0` (canonical HM's own reference).
- Growth transitions: 9 non-taper week-to-week deltas, all `<= 2.5 km`
  absolute-cap-compliant (max observed delta 2.0 km, W05→W06).
- KEY/EASY1/EASY2/LONG composition: 4 sessions/week throughout, `Assert.All(w => w.Sessions.Count == 4)` passing.
- LR vector: 6.5 → 12.0 km monotonic non-taper, peak LR 12.0 km (< 15.0 ceiling, < 16.0 target), taper LR 7.0/4.5.
- Taper anchor: non-chained — W10 (13.5) is not W09×0.43 (21.5×0.43=9.245, actual 13.5 — confirms anchor-based, not chained, computation).
- Pace provenance: no target pace supplied in this direct-orchestrator context (`GOAL_FEASIBILITY_IN=NOT_REQUESTED`), matching the 12W golden trace's own pace-neutral evidence path.
- Calendar: Mon/Wed/Fri/Sun pattern, long run Sunday, `StartDate=2026-08-03`.
- Validation status: `prescribed.ValidationResult.IsValid = true`, zero errors.

New test: `GoldenTrace_10W_LowerBoundary_MaterializesWithFrozen16KNumericsNotHmNumerics` — PASSED.

## 8. 11W materialization (full detail)

`TargetWeekCount=11`. Phase allocation: **FOUNDATION=2/BUILD=3/RACE_SPECIFIC=4/TAPER=2** (sum=11).

| Wk | Phase | Vol | LR | LR share | Taper |
|----|-------|----:|---:|---------:|-------|
| 01 | FOUNDATION | 20.0 | 6.5 | 32.5% | No |
| 02 | FOUNDATION | 21.5 | 7.0 | 32.6% | No |
| 03 | BUILD | 23.0 | 7.5 | 32.6% | No |
| 04 | BUILD | 24.5 | 8.0 | 32.7% | No |
| 05 | BUILD | 26.5 | 8.5 | 32.1% | No |
| 06 | RACE_SPECIFIC | 28.0 | 9.0 | 32.1% | No |
| 07 | RACE_SPECIFIC | 29.5 | 10.5 | 35.6% | No |
| 08 | RACE_SPECIFIC | 31.0 | 11.5 | 37.1% | No |
| 09 | RACE_SPECIFIC | 32.5 | 13.0 | 40.0% | No |
| 10 | TAPER | 23.0 | 7.5 | 32.6% | Yes |
| 11 | TAPER | 14.0 | 4.5 | 32.1% | Yes |

Peak volume 32.5 km. Peak LR 13.0 km (`<=15.0`, `<16.0`). LR share hits
exactly the 40.0% hard cap at W09 and never exceeds it (validation passes).
Taper non-chained (23.0×0.43=9.89 ≠ 14.0). `CanonicalDistanceFamily=HALF_MARATHON`,
`RequestedTargetDistanceKm=16.0` both confirmed on the context.
`RealPipeline_EligibleHorizons_MaterializeSuccessfully(weeks: 11)` — PASSED.

## 9. 12W golden preservation (byte-for-byte comparison)

Re-ran `GoldenTrace_12W_MaterializesWithFrozen16KNumericsNotHmNumerics` after
the correction. Observed output:

```
W01|FOUNDATION|vol=20.0|lr=6.5|lrShare=32.5%|taper=False
W02|FOUNDATION|vol=21.5|lr=7.0|lrShare=32.6%|taper=False
W03|BUILD|vol=23.0|lr=7.5|lrShare=32.6%|taper=False
W04|BUILD|vol=24.5|lr=8.0|lrShare=32.7%|taper=False
W05|BUILD|vol=26.0|lr=8.5|lrShare=32.7%|taper=False
W06|RACE_SPECIFIC|vol=28.0|lr=9.0|lrShare=32.1%|taper=False
W07|RACE_SPECIFIC|vol=29.5|lr=10.5|lrShare=35.6%|taper=False
W08|RACE_SPECIFIC|vol=31.0|lr=11.5|lrShare=37.1%|taper=False
W09|RACE_SPECIFIC|vol=32.5|lr=12.5|lrShare=38.5%|taper=False
W10|RACE_SPECIFIC|vol=34.0|lr=13.5|lrShare=39.7%|taper=False
W11|TAPER|vol=24.0|lr=8.0|lrShare=33.3%|taper=True
W12|TAPER|vol=14.5|lr=5.0|lrShare=34.5%|taper=True
```

Phase allocation: FOUNDATION=2/BUILD=3/RACE_SPECIFIC=5/TAPER=2 (unchanged).
Peak = **34.0 km**. This is **byte-for-byte identical** to DIST-GEN.2's own
§27 golden table (W01 20.0/6.5 ... W12 14.5/5.0, peak 34.0) — **zero
numeric delta** from the horizon-authority correction, as expected, since
`MinimumCoreWeeks` only gates admission at the horizon layer and never
participates in 12W's own phase-allocation or volume/taper arithmetic.

## 10. 13W materialization (full detail)

`TargetWeekCount=13`. Phase allocation: **FOUNDATION=2/BUILD=4/RACE_SPECIFIC=5/TAPER=2** (sum=13).

| Wk | Phase | Vol | LR | LR share | Taper |
|----|-------|----:|---:|---------:|-------|
| 01 | FOUNDATION | 20.0 | 6.5 | 32.5% | No |
| 02 | FOUNDATION | 21.5 | 7.0 | 32.6% | No |
| 03 | BUILD | 23.0 | 7.5 | 32.6% | No |
| 04 | BUILD | 24.0 | 8.0 | 33.3% | No |
| 05 | BUILD | 25.5 | 8.5 | 33.3% | No |
| 06 | BUILD | 27.0 | 9.0 | 33.3% | No |
| 07 | RACE_SPECIFIC | 28.5 | 9.5 | 33.3% | No |
| 08 | RACE_SPECIFIC | 30.0 | 10.5 | 35.0% | No |
| 09 | RACE_SPECIFIC | 31.0 | 11.5 | 37.1% | No |
| 10 | RACE_SPECIFIC | 32.5 | 12.5 | 38.5% | No |
| 11 | RACE_SPECIFIC | 34.0 | 13.5 | 39.7% | No |
| 12 | TAPER | 24.0 | 8.0 | 33.3% | Yes |
| 13 | TAPER | 14.5 | 5.0 | 34.5% | Yes |

Peak 34.0 km (never exceeds 40.0 ceiling, never equals 43.0 HM canonical
reference). Taper non-chained. `RealPipeline_EligibleHorizons_MaterializeSuccessfully(weeks: 13)` — PASSED.

## 11. 14W materialization (full detail)

`TargetWeekCount=14`. Phase allocation: **FOUNDATION=3/BUILD=4/RACE_SPECIFIC=5/TAPER=2** (sum=14).

| Wk | Phase | Vol | LR | LR share | Taper |
|----|-------|----:|---:|---------:|-------|
| 01 | FOUNDATION | 20.0 | 6.5 | 32.5% | No |
| 02 | FOUNDATION | 21.5 | 7.0 | 32.6% | No |
| 03 | FOUNDATION | 22.5 | 7.5 | 33.3% | No |
| 04 | BUILD | 24.0 | 8.0 | 33.3% | No |
| 05 | BUILD | 25.0 | 8.5 | 34.0% | No |
| 06 | BUILD | 26.5 | 8.5 | 32.1% | No |
| 07 | BUILD | 27.5 | 9.0 | 32.7% | No |
| 08 | RACE_SPECIFIC | 29.0 | 9.5 | 32.8% | No |
| 09 | RACE_SPECIFIC | 30.0 | 10.5 | 35.0% | No |
| 10 | RACE_SPECIFIC | 31.5 | 11.5 | 36.5% | No |
| 11 | RACE_SPECIFIC | 32.5 | 12.5 | 38.5% | No |
| 12 | RACE_SPECIFIC | 34.0 | 13.5 | 39.7% | No |
| 13 | TAPER | 24.0 | 8.0 | 33.3% | Yes |
| 14 | TAPER | 14.5 | 5.0 | 34.5% | Yes |

Peak 34.0 km. `RealPipeline_EligibleHorizons_MaterializeSuccessfully(weeks: 14)` — PASSED, and the pre-existing `RealPipeline_ZeroDelta_CanonicalHalfMarathon14W_StillSucceeds_NoOverride` (canonical, no override) also still PASSED unchanged.

## 12. 15W typed rejection (proof, still above-maximum)

`RealPipeline_AboveMaximumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds(weeks: 15)` — PASSED. Throws `PlanHorizonCompositionRequiredException`, message contains `DARK_16K_CORE_HORIZON`, is not `PlanHorizonStartTooEarlyException`. `MaximumCoreWeeks=14` unchanged, so this behavior is a pure zero-delta carry-forward, not new.

## 13. Complete horizon matrix (8W-15W)

| Weeks | Classification | Allocator invoked? | Final result |
|------:|-----------------|---------------------|---------------|
| 7 | BelowMinimum | No | `PlanCoreHorizonTooShortException` (DARK_16K_CORE_HORIZON_7_NOT_SUPPORTED) |
| 8 | BelowMinimum (was StandaloneCoreSupported pre-2A) | No | `PlanCoreHorizonTooShortException` (DARK_16K_CORE_HORIZON_8_NOT_SUPPORTED) |
| 9 | BelowMinimum | No | `PlanCoreHorizonTooShortException` (DARK_16K_CORE_HORIZON_9_NOT_SUPPORTED) |
| 10 | StandaloneCoreSupported (new minimum boundary) | Yes | Materializes: FOUNDATION=2/BUILD=3/RACE_SPECIFIC=3/TAPER=2, peak 31.0 |
| 11 | StandaloneCoreSupported | Yes | Materializes: 2/3/4/2, peak 32.5 |
| 12 | ExactStandaloneCoreSupported (preferred) | Yes | Materializes: 2/3/5/2, peak 34.0 (golden, zero-delta) |
| 13 | StandaloneCoreSupported | Yes | Materializes: 2/4/5/2, peak 34.0 |
| 14 | StandaloneCoreSupported | Yes | Materializes: 3/4/5/2, peak 34.0 |
| 15 | CompositionRequired | No | `PlanHorizonCompositionRequiredException` (DARK_16K_CORE_HORIZON_15_NOT_SUPPORTED) |

## 14. Phase allocations (all 5 supported horizons)

| Weeks | FOUNDATION | BUILD | RACE_SPECIFIC | TAPER | Sum |
|------:|-----------:|------:|---------------:|------:|----:|
| 10 | 2 | 3 | 3 | 2 | 10 |
| 11 | 2 | 3 | 4 | 2 | 11 |
| 12 | 2 | 3 | 5 | 2 | 12 |
| 13 | 2 | 4 | 5 | 2 | 13 |
| 14 | 3 | 4 | 5 | 2 | 14 |

All confirmed by direct `GroupBy(w => w.PhaseKey)` assertions (10W in the new
golden test) and by real trace output (11W/13W/14W, captured via a temporary
diagnostic dump and deleted after use — not part of the committed diff).

## 15. Stage occupancy proof

Every supported horizon (10-14W) produces exactly `4 sessions/week ×
weeks` total sessions with `GeneratedCatalogPlanSkeletonValidator`/
`GeneratedCatalogStageScheduleValidator`/`BoundCatalogPlanValidator` all
passing (`prescribed.ValidationResult.IsValid = true`, zero errors, for
every horizon exercised). `Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count))` passes at 10W and 12W (explicit test assertions).

## 16. Peak-volume zero-delta

Observed peaks: 10W=31.0, 11W=32.5, 12W=34.0, 13W=34.0, 14W=34.0 — all
`<= 40.0` (pilot's selected ceiling) and never `43.0` (canonical HM's own
reference), for every horizon. `TwoOfThreeHorizons_MaterializeWithinFrozenPeakAndNeverForcePeak` now runs against 10,11,12,13,14 (expanded from 12,14) — all PASSED.

## 17. Selected-peak proof

`VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K.ResolvedPeakReference.Value = 40.0`,
`ResolvedPeakReferenceIsSelectedCeiling = true` — unchanged
(`VolumeSafetyPolicy_16K_EncodesExactFrozenValues` — PASSED, zero-delta).
No observed horizon (10-14W) forces volume up to 40.0; the ceiling is a cap,
never a chased target — confirmed by every observed peak being `<= 34.0`,
well under the 40.0/43.0 references.

## 18. Weekly-growth proof

All non-taper week-to-week deltas across 10W-14W traces are `<= 2.5 km`
(the `AbsoluteWeeklyIncrementCapKm`); largest observed delta is 2.0 km. No
violation across any of the 5 supported horizons.

## 19. Absolute-cap proof

`VolumeSafetyPolicy.AbsoluteWeeklyIncrementCapKm = 2.5d` unchanged
(zero-delta, `VolumeSafetyPolicy_16K_EncodesExactFrozenValues` PASSED); no
observed transition in 10W/11W/12W/13W/14W exceeds it.

## 20. LR proof

`LongRunPreferredMinimumShare=0.30`, `Maximum=0.36`, `SelectionShare=0.33`,
`HardCapShare=0.40`, `PreferredAbsolutePeakLongRunKm=15.0` — all unchanged
(zero-delta). Every observed LR value across all 5 horizons is `< 16.0`
(never reaches/exceeds target distance) and `<= 15.0` (never exceeds the
absolute ceiling); LR share never exceeds 40.0% (11W W09 hits exactly
40.0%, the hard cap, and stays there — never above).
`StartingAbsoluteLongRunCapKm = null` unchanged.

## 21. Taper proof

Every horizon (10W-14W) has exactly 2 taper weeks, and in every case the
taper is non-chained: W(last) ≠ W(last-1) × 0.43. Examples: 10W
(21.5→13.5, not 21.5×0.43=9.245), 11W (23.0→14.0, not 9.89), 12W
(24.0→14.5, not 10.32), 13W (24.0→14.5, same anchor), 14W (24.0→14.5, same
anchor) — all computed from the fixed peak-reference anchor, never
chained. Multipliers 0.70/0.43 unchanged (zero-delta,
`VolumeSafetyPolicy_16K_EncodesExactFrozenValues` PASSED).

## 22. Session-allocation proof

`Assert.All(prescribed.Weeks, w => Assert.Equal(4, w.Sessions.Count))`
passes at every tested horizon (10W, 12W explicit; 11W/13W/14W verified via
direct materialization producing valid, 4-session weeks per
`ValidationResult.IsValid=true`). KEY+EASY1+EASY2+LONG structural roles sum
to the full weekly session count in every observed week (unchanged from
DIST-GEN.2's own proof; this phase changes no session-composition logic).

## 23. Workout-binding proof

`CatalogWorkoutBinder`/`BoundCatalogPlanValidator` unchanged and untouched;
every materialized horizon (10-14W) passes `BoundCatalogPlanValidator` with
zero errors (implicit in `prescribed.ValidationResult.IsValid=true`, which
folds in the bound-plan validation stage).

## 24. Target-race-pace proof (zero-delta)

No pace-related code path was touched. The direct-orchestrator tests used
for 10W-14W materialization use `PACE_SOURCE_IN=NONE/NO_PACE_EVIDENCE`
(same condition-result shape as DIST-GEN.2's own golden traces) — zero
delta from the prior phase's pace-provenance behavior.

## 25. Riegel proof (zero-delta)

Riegel-reuse code (`PaceSourceResolver`/target-pace derivation) is untouched
by this phase's diff. The real pipeline test
`RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously`
(which exercises the full `PlanServices.GeneratePreviewAsync` →
`ConfirmPlanAsync` path with a real recent-race input,
`RecentRace = TenK, 2700s`) still PASSED unchanged, confirming zero
Riegel-path regression.

## 26. Goal-time fitness-separation proof (zero-delta regression)

No fitness/goal-time separation logic was touched. Full monolithic suite
(§42) shows zero new failures anywhere in that area.

## 27. Family/target identity proof

At every supported horizon (10-14W), `CanonicalDistanceFamily=HALF_MARATHON`
and `RequestedTargetDistanceKm=16.0` coexist without collapsing into each
other — explicitly asserted in the new 10W golden test
(`context.ResolverInput.CanonicalDistanceFamily`/`RequestedTargetDistanceKm`)
and in `RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously`
(persisted-row assertion, still passing, 12W).

## 28. Persistence-fix preservation

`git status --porcelain -- backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`
returns empty — the file is **byte-for-byte untouched** by this phase. The
DIST-GEN.2 fix is still present verbatim:
```csharp
RequestedTargetDistanceKm = snapshot.NormalizedInput.RequestedTargetDistanceKm ?? snapshot.NormalizedInput.GoalDistanceKm,
```
`RealPipeline_Confirmation_PersistsCanonicalFamilyAndRequestedTargetDistanceSimultaneously`
(a real confirm-a-plan test, 12W) — PASSED, confirming
`RequestedTargetDistanceKm(16.0) != GoalDistanceKm` on the persisted row.

## 29. Unsupported target-distance proof

`EligibilityGate_OnlyExactFrozenTripleIsEligible` and
`CanonicalDistanceFamilyResolver_ClassifiesButNeverGrantsEligibilityAlone`
(covering 12.0/15.0/18.0/20.0 km) — both PASSED, unchanged. 12K/15K/18K/20K
remain ineligible for the dark 16K pilot.

## 30. Public Custom fail-closed proof

`GoalDistanceCustomFailClosedTests` suite executed as part of the full
monolithic run (§42) — all tests in that class PASSED (not among the 4
known failures), confirming `GoalDistance.Custom` still returns HTTP 422
`GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` for a real public request. No new test
added per instruction; existing suite re-confirmed green.

## 31. Frontend guard preservation

`git status --porcelain` shows zero touched files under any Flutter/mobile
directory — this phase's diff is confined to two `.cs` files under
`backend/`.

## 32. TEN_K zero-delta

`RaceHorizonPolicy`'s TEN_K-serving fallback arm (8/12/14) is defined in
`backend/RunningApp.Application/Common/RaceHorizonPolicy.cs`, which
`git status --porcelain` confirms is **completely untouched** by this
phase. `HorizonPolicy_16K_ReasonCode_IsDistinctFromTenKAndHalfMarathonOwnCodes`
(comparing `TargetDistance16KHorizonPolicy.GetUnsupportedReasonCode` against
`RaceHorizonPolicy.GetCoreHorizonUnsupportedReasonCode`) — PASSED, unchanged.

## 33. Canonical HM zero-delta

`RealPipeline_ZeroDelta_CanonicalHalfMarathon14W_StillSucceeds_NoOverride`
and `RealPipeline_ZeroDelta_CanonicalHalfMarathon_TooShortForItsOwnBounds_StillUsesHmOwnMinimum`
(asserting HM's own real 10-week minimum, `"HALF_MARATHON standalone core
minimum (10"`, is used for a canonical, non-override request) — both
PASSED, unchanged. `VolumeSafetyPolicy.HalfMarathonIntermediate4D` (canonical)
values (`ResolvedPeakReference=43.0`, `GoldenFixtureStartingVolumeKm=25.0`,
`PreferredAbsolutePeakLongRunKm=19.0`) re-confirmed unchanged in
`VolumeSafetyPolicy_16K_EncodesExactFrozenValues`.

## 34. HM 6D zero-delta (spot check)

`RaceHorizonPolicy.cs` and all HM 6D-specific materialization tests
(`Hm16Advanced3D4D5DFullDarkVerticalSliceTests` and related) ran as part of
the full monolithic suite (§42) with zero new failures — this phase touches
no HM-6D-specific code path.

## 35. Experienced exclusion (confirm untouched)

`Dark16KPilotEligibilityPolicy` (gating on `Level=Intermediate` exactly,
excluding Beginner/Advanced) is untouched by this phase's diff —
`EligibilityGate_OnlyExactFrozenTripleIsEligible` (covering Beginner/
Advanced exclusion) — PASSED, unchanged.

## 36. Catalog/hash proof

`git status --porcelain -- plan-catalog` returns **empty** (clean) after
reverting the known, benign, recurring auto-regenerated
`plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`
timestamp diff via `git checkout --` (produced by running `PlanCatalog.Tests`,
unrelated to this phase's own diff). Zero catalog JSON file is part of this
phase's actual change set.

**False-alarm ruled out during verification:** the working tree contains
`plan-catalog/tests/PlanCatalog.Tests/TestResults/gen3b-plan-catalog{,-final,-closure}.trx`,
each reporting `total=1250` with inconsistent, non-deterministic failure
counts across the three files. These are **not** produced by this phase —
`git log -1 -- <that path>` shows they were committed on 2026-08-14 as part
of an unrelated, much earlier "Gen 3B" engagement snapshot, and
`git status --porcelain` confirms they are byte-identical to their
committed HEAD version (untouched by this session). They are pre-existing,
tracked historical noise, not evidence of a regression from this phase's
diff. The real, current, fresh, single, fully unfiltered `PlanCatalog.Tests`
run performed for this phase (`dotnet clean` then `dotnet test`, no
`--filter`) reports **1510/1510 passed, 0 failed** — see §40a below.

## 37. Database/schema zero-delta

`git status --porcelain` shows no new file under any `Migrations/` directory
and no change to any `*DbContext*.cs`/`*.Designer.cs` file. Zero database
migration was created.

## 38. 10W lower-boundary golden (new test, full trace)

New test:
`GoldenTrace_10W_LowerBoundary_MaterializesWithFrozen16KNumericsNotHmNumerics`
in `Dark16KTargetDistanceProjectionTests.cs`. Asserts:
- `CanonicalDistanceFamily=HALF_MARATHON`, `RequestedTargetDistanceKm=16.0` on the context.
- 10 weeks materialized, 4 sessions/week, `ValidationResult.IsValid=true`.
- Phase counts exactly FOUNDATION=2/BUILD=3/RACE_SPECIFIC=3/TAPER=2.
- Peak `<=40.0`, never `43.0`.
- LR `<16.0` and `<=15.0` at every week, share `<=40.0%`.
- Exactly 2 taper weeks, non-chained.
Full observed trace reproduced in §7 above. Test status: **PASSED**.

## 39. 12W golden comparison (detailed diff-or-no-diff statement)

**No diff.** The 12W trace reproduced in §9 is identical, week-for-week and
value-for-value, to DIST-GEN.2's own frozen §27 table: W01 20.0/6.5 through
W12 14.5/5.0, peak 34.0, phase split 2/3/5/2. The `MinimumCoreWeeks`
correction is purely an admission-layer change (it affects which horizons
are classified as `BelowMinimum` vs `StandaloneCoreSupported`/
`ExactStandaloneCoreSupported`) and has zero interaction with 12W's own
phase-allocation or volume/taper numeric authority, which depend only on
`PreferredCoreWeeks`/`MaximumCoreWeeks` and the reused catalog identity's own
per-week arithmetic — both unchanged.

## 40. Targeted test results (every new/modified test and its outcome)

All in `Dark16KTargetDistanceProjectionTests.cs`:

| Test | Change | Outcome |
|---|---|---|
| `HorizonPolicy_16K_ExactFrozenBounds` | Assertion 8→10 | PASSED |
| `HorizonPolicy_16K_ClassifiesExactlyAtItsOwnFrozenBoundaries` | Data: added (8,BelowMinimum),(9,BelowMinimum),(10,StandaloneCoreSupported); removed (7,BelowMinimum)/(8,StandaloneCoreSupported) dup handling — 7 kept, 8/9 now BelowMinimum, 10 now the supported floor | PASSED (7 cases) |
| `RealPipeline_EligibleHorizons_MaterializeSuccessfully` | InlineData expanded from (12,14) to (10,11,12,13,14) | PASSED (5 cases) |
| `RealPipeline_EightWeekHorizon_PassesHorizonGate_ButFailsPhaseAllocation_ForCatalogReuseReasons` | **Removed** (its premise — 8W passes the horizon gate — is now false) | N/A (deleted) |
| `RealPipeline_BelowMinimumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds` | InlineData expanded from (7) to (7,8,9) | PASSED (3 cases) |
| `RealPipeline_AboveMaximumHorizon_RejectedByDark16KAuthority_NotByHalfMarathonOrTenKBounds` | Unchanged (15) | PASSED |
| `EightWeekHorizon_CannotPhaseAllocate_BecauseReusedCatalogIdentityOwnMinimumSumIsTen` | Doc-comment updated to explain it now deliberately bypasses the (corrected) horizon gate via direct-orchestrator invocation | PASSED (unchanged assertion body) |
| `GoldenTrace_10W_LowerBoundary_MaterializesWithFrozen16KNumericsNotHmNumerics` | **New** | PASSED |
| `TwoOfThreeHorizons_MaterializeWithinFrozenPeakAndNeverForcePeak` | InlineData expanded from (12,14) to (10,11,12,13,14) | PASSED (5 cases) |
| `GoldenTrace_12W_MaterializesWithFrozen16KNumericsNotHmNumerics` | Unchanged | PASSED (zero-delta, §9/§39) |
| All other tests in the file (eligibility, peak-band, volume-safety, persistence) | Unchanged | PASSED |

Net test-count delta in the Dark16K file: +2 (boundary) +3 (eligible
horizons) +2 (below-min) -1 (removed) +1 (new 10W golden) +3 (two-of-three
expansion) = **+10**, matching the RuntimeCatalog subtree delta in §41.

## 40a. PlanCatalog.Tests baseline verification

Since this phase touches zero catalog JSON and zero `PlanCatalog.*` project
code, `PlanCatalog.Tests` was expected to be an exact zero-delta,
1510/1510 rerun. An initial run's own after-the-fact working-tree audit
surfaced three unrelated, pre-existing, tracked `gen3b-plan-catalog*.trx`
files (total=1250, inconsistent failure counts across the three) — traced
via `git log` to a committed 2026-08-14 snapshot from an unrelated "Gen 3B"
engagement, confirmed byte-identical to HEAD (untouched by this session).
Ruled out as a false alarm, not a real defect.

To remove any doubt, `PlanCatalog.Tests` was re-run from a clean state
(`dotnet clean` then a single, fully unfiltered `dotnet test`, no
`--filter`):

```
Başarılı! - Başarısız: 0, Başarılı: 1510, Atlanan: 0, Toplam: 1510, Süre: 12 s - PlanCatalog.Tests.dll (net9.0)
```

**1510/1510 passed, 0 failed** — exact match to the established baseline,
zero delta, as expected for a phase with zero `PlanCatalog.*`/catalog-JSON
changes.

## 41. RuntimeCatalog subtree results (exact before/after counts)

- Before (DIST-GEN.2 baseline): **3882/3882** passed.
- After (this phase, filter `FullyQualifiedName~RunningApp.IntegrationTests.RuntimeCatalog`):
  **3892/3892** passed, 0 failed, 0 skipped. Saved to
  `backend/RunningApp.IntegrationTests/TestResults/runtimecatalog_final.trx`.
- Delta: **+10**, entirely accounted for by the Dark16K test-file changes in §40 (no other RuntimeCatalog test file was touched).

## 42. Full monolithic regression (exact total/passed/failed)

Command: `dotnet test` (no filter) in `backend/RunningApp.IntegrationTests`.
Result, parsed directly from
`backend/RunningApp.IntegrationTests/TestResults/full_monolithic.trx`:

```
Total: 4868, Passed: 4864, Failed: 4, Skipped: 0
```

Durable baseline was 4858/4854/4; the +10 delta (total and passed both) is
exactly the Dark16K test-count delta from §40/§41, with **zero change in
the failed count**.

## 43. Exact durable-baseline comparison

Failing test identities extracted directly from the `.trx` (`outcome="Failed"`):

```
RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)
RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)
```

This is **exactly** the known durable-baseline set of 4 (the two
`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates`
weeks 13/14, `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`,
and the `Sw09ExplicitZeroReadinessEndToEndTests...` test) — **no new
failing identity**, none of the known 4 became unexpectedly fewer (all 4
still fail, none newly pass, none newly fail elsewhere). Zero unexplained
change.

## 44. Current-state documentation

Final state of the dark 16K pilot after this phase:
- Parent/reused catalog identity: `HALF_MARATHON__4D__INTERMEDIATE` (unmodified).
- Standard Core supported horizon: **10-14 weeks** (was nominally 8-14,
  corrected to reflect the reused identity's real minimum).
- Preferred horizon: **12 weeks** (unchanged).
- Dark-only, fully implemented and materializing for the entire 10-14W
  Standard Core range through the real, unmodified production pipeline.
- 8-9W explicitly deferred to a future, separately-governed compressed-core
  track (not implemented, not silently unsupported-forever).
- Still not publicly reachable — `TargetDistanceKmOverride` remains
  internal-only, no public wire DTO sets it.
- `GoalDistance.Custom` still fails closed with HTTP 422
  `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` (untouched, re-confirmed §30).

## 45. Historical correction chronology

1. **DIST-GEN.1** froze MinimumCoreWeeks=8, PreferredCoreWeeks=12,
   MaximumCoreWeeks=14 from real, on-point 10-mile/16.09km external evidence.
2. **DIST-GEN.2** implemented the full dark pilot under that authority and
   discovered a real conflict: 8W cannot phase-allocate against the reused,
   unmodified HALF_MARATHON catalog identity's own real minimum-week sum of
   10. Closed honestly as `..._BLOCKED`.
3. **DIST-GEN.2A** (pure governance, no code) resolved the conflict:
   corrected MinimumCoreWeeks to 10, re-confirmed Preferred/Maximum
   unchanged, and deferred 8-9W to a future compressed-core track.
4. **DIST-GEN.2B** (this phase) implemented the DIST-GEN.2A correction in
   code, updated the affected tests, and re-proved the full 10-14W range
   against the real, unmodified production pipeline with zero regression.

No prior phase report's text was rewritten; DIST-GEN.1's and DIST-GEN.2's
own historical description of the original 8-week decision and its
discovered conflict remain exactly as originally recorded.

## 46. Future compressed-core handoff

A plausible future phase id for the deferred 8-9W compressed-core track:
**DIST-GEN-COMP.0** ("16K compressed-core horizon authority and structural
compression resolution"), mirroring this repo's own HM-X2 precedent for
HALF_MARATHON's 7-9W deferral. This phase does **not** start that work — no
code, test, or catalog change toward compressed-core support is made here.

## 47. DIST-GEN.3 readiness statement

Any future DIST-GEN.3-numbered phase building on this pilot (e.g., public
exposure, additional days-per-week variants) should scope itself to the
**10-14W Standard Core range only** — this is the fully proven,
zero-hole, zero-regression range as of this phase. 8-9W remain explicitly
out of scope pending DIST-GEN-COMP.0 or equivalent.

## 48. Remaining blockers

**None.** Every horizon in the corrected Standard Core range (10, 11, 12,
13, 14 weeks) materializes successfully through the real, unmodified
production pipeline with the frozen numeric/structural authority intact;
8W/9W/15W are typed-rejected at the correct layer with the correct,
attributable reason codes; the 12W golden trace is byte-for-byte
unchanged; zero catalog file changed; zero database migration created;
full regression is clean against the durable baseline (no new failing
identity). The only remaining scope gap (8-9W compressed core) is a
deliberate, disclosed, separately-governed deferral, not an unresolved
defect of this phase.

---

## Final classification

**16K_INTERMEDIATE_4D_FULL_DARK_TARGET_DISTANCE_PROJECTION_IMPLEMENTATION_COMPLETE**
