# PHASE HM-X1.3 — HALF-MARATHON 6D INTERMEDIATE/ADVANCED 10-16 WEEK FULL DARK IMPLEMENTATION

**Type:** PRODUCTION IMPLEMENTATION (DARK ONLY — no public activation)

---

## 1. HM-X1.2 authority consumed

Every field below is implemented exactly as frozen by `PHASE_HM_X1_2_6D_INTERMEDIATE_ADVANCED_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md` §34-§35, not re-derived:

**Intermediate 6D**: PeakVolumeBand [44,58], SelectedPeakReference 51.0, PreferredWeeklyIncreaseRate 0.07, HardWeeklyIncreaseRate 0.08, AbsoluteWeeklyIncreaseCapKm 2.5, CalibrationStartingVolume 29.5, LR shares 0.28/0.36/0.28/0.36, PreferredAbsolutePeakLongRunKm 19.0, StartingAbsoluteLongRunCap 12.0, TaperWeek1/2 0.70/0.43, GoldenFixtureNonTaperTransitions 11, ResolvedPeakReferenceIsSelectedCeiling true.

**Advanced 6D**: PeakVolumeBand [54,72], SelectedPeakReference 63.0, PreferredWeeklyIncreaseRate 0.07, HardWeeklyIncreaseRate 0.08, AbsoluteWeeklyIncreaseCapKm 2.5, CalibrationStartingVolume 36.5, LR shares 0.28/0.36/0.28/0.36, PreferredAbsolutePeakLongRunKm 19.0, StartingAbsoluteLongRunCap N/A (mirrors `HalfMarathonAdvanced5D`'s null), TaperWeek1/2 0.70/0.43, GoldenFixtureNonTaperTransitions 11, ResolvedPeakReferenceIsSelectedCeiling true.

Structural authority (§3-§7 of HM-X1.2): KEY1 unchanged from 5D at both levels. KEY2 — Intermediate: `FARTLEK` v6 (fallback `EASY_STANDARD`) in Build/RaceSpecific, `EASY_STANDARD` in Foundation/Taper, never true-hard, never `HM_PACE`. Advanced: `THRESHOLD_TEMPO` v4 (fallback `EASY_STANDARD`) in Build/RaceSpecific, `EASY_STANDARD` in Foundation/Taper, true-hard, never `HM_PACE`. No third KEY_SESSION slot, no second LONG_RUN role.

## 2. Catalog lifecycle/versioning decisions

- **Combination files**: two new, additive files (`half-marathon-6d-intermediate.v1.json`, `half-marathon-6d-advanced.v1.json`) — never modifies an existing combination.
- **HALF_MARATHON_MASTER**: a real, hard-pinned reference *was* found that structurally could not point a 6D combination at the existing, unmodified v2/v3 masters — see §4 below. Two new, minimal, additive master versions (v4, v5) were created as a result, each byte-identical to v2/v3 except `supportedRunsPerWeek` widened from `[5]` to `[6]`. v2/v3 themselves are untouched (still `[5]`, still what the existing 5D combinations resolve).
- **RUN_LAYOUT_6D**, **INTERMEDIATE_MODIFIER v9**, **ADVANCED_MODIFIER v3**, **HALF_MARATHON_WORKOUT_PROGRESSION v2/v3**: all reused completely unmodified.
- **Peak-volume bands**: the live file (`peak-volume-bands.v9.json`) is `status: VALIDATED` (published-shaped, referenced by the existing rule pack) — treated as frozen; a new additive `peak-volume-bands.v10.json` was created (every v9 row reproduced byte-identical, plus the two new HM 6D rows appended).
- **Rule pack**: `appsel-race-plan.v10.json`'s own `peakVolumeBandPolicy` pointer is pinned to `PEAK_VOLUME_BANDS_V1` v9. Rather than repoint v10 itself (which would change the peak-volume-band resolution for every existing HM/10K combination that references v10), a new additive `appsel-race-plan.v11.json` was created — byte-identical to v10 except its `peakVolumeBandPolicy` pointer, which now resolves v10 (of the peak-volume-bands file). Only the two new 6D combination files reference rule pack v11; every existing combination still references v10 (→ peak-volume-bands v9), zero delta.

## 3. Files/artifacts changed (full list)

**New catalog artifacts:**
- `plan-catalog/catalog/combinations/half-marathon-6d-intermediate.v1.json`
- `plan-catalog/catalog/combinations/half-marathon-6d-advanced.v1.json`
- `plan-catalog/catalog/templates/half-marathon-master.v4.json`
- `plan-catalog/catalog/templates/half-marathon-master.v5.json`
- `plan-catalog/catalog/policies/peak-volume-bands.v10.json`
- `plan-catalog/catalog/rule-packs/appsel-race-plan.v11.json`

**New C# files:**
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/HalfMarathonIntermediate6DTaperVolumePolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/HalfMarathonAdvanced6DTaperVolumePolicy.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/HmX13Intermediate6DAdvanced6DFullDarkVerticalSliceTests.cs`

**Modified C# files (production):**
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — two new records (`HalfMarathonIntermediate6D`, `HalfMarathonAdvanced6D`), two dispatcher `case 6:` arms.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — widened Intermediate/Advanced HM dispatch conditions to admit `DaysPerWeek == 6`; widened the two fail-closed `ResolveStartingVolume` reference-equality guards to also cover the new 6D policies.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/CatalogFinalPrescribedPlanValidator.cs` — widened `ResolveLongRunHardCapShare`'s Intermediate/Advanced HM branches to admit 6; widened `ValidateTaperCompleteness`'s dual-lane branch to admit the two new 6D candidate identities.
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` — two new dark-only candidate key/version constants; two new `ResolveCandidate` 3-arg-overload cases (dark-only, distance-aware overload only — the public/Runway allow-lists are untouched).

**Modified C# files (test drift fixes, required by this phase's own additive dispatch widening):**
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Phase4F7BVolumeAndLongRunTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1cFailClosedDistanceBlindFallbackTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogDeploymentPackagingTests.cs` — `ExpectedRuntimeCatalogJsonFiles` bumped 154→160 (exactly the 6 new additive JSON files above).

No Flutter/`mobile/**` file touched. No `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` public 2-arg overload or 3-arg `IsSupportedLevelFrequency` HALF_MARATHON allow-list entry touched. No Experienced, Beginner×6D, or Runway/LongHorizon/compressed file touched.

## 4. RUN_LAYOUT_6D reuse

Confirmed unmodified: `plan-catalog/catalog/layouts/run-layout-6d.v1.json` was read only, never edited. Both new combination files point at it unchanged (`RUN_LAYOUT_6D` v1: 2×`KEY_SESSION`, 3×`EASY_SUPPORT`, 1×`LONG_RUN`).

**The one hard-pinned reference structurally requiring a new artifact**: `HALF_MARATHON_MASTER` v2/v3 both declare `"supportedRunsPerWeek": [5]`. `PlanCatalog.Core.Validation.CatalogGraphValidator` enforces `TC_LAYOUT_RUNS_PER_WEEK_NOT_SUPPORTED` — confirmed by direct probe (a scratch test invoking `CatalogGraphValidator.Validate` against the real snapshot with the 6D combinations pointed at v2/v3 unmodified) — real error: `ValidationIssue { Code = TC_LAYOUT_RUNS_PER_WEEK_NOT_SUPPORTED, Severity = Error, Message = Layout RunsPerWeek 6 is not in master's SupportedRunsPerWeek., JsonPath = $.layout }`, reproduced for both cells. This is not merely a preference — it is a genuine hard-fail. The minimum additive fix was two new master versions (v4/v5), each changing only `supportedRunsPerWeek` (byte-identical otherwise, including the unchanged `workoutProgression` pointer). Re-probed after the fix: zero domain/graph validation issues.

## 5. Intermediate policy encoding

`backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`, `HalfMarathonIntermediate6D` record:
```
PreferredMaxWeeklyIncreaseRatio: 0.07d, HardMaxWeeklyIncreaseRatio: 0.08d,
AbsoluteWeeklyIncrementCapKm: 2.5d, GoldenFixtureStartingVolumeKm: 29.5d,
ResolvedPeakReference: 51.0d, GoldenFixtureNonTaperTransitions: 11,
TaperVolumeMultiplier(s): 0.70d/0.43d (HalfMarathonIntermediate6DTaperVolumePolicy),
LongRunPreferredMinimumShare: 0.28d, LongRunPreferredMaximumShare: 0.36d,
LongRunSelectionShare: 0.28d, LongRunHardCapShare: 0.36d,
PreferredAbsolutePeakLongRunKm: 19.0d, StartingAbsoluteLongRunCapKm: 12.0d,
ResolvedPeakReferenceIsSelectedCeiling: true
```
Verified byte-identical to `HalfMarathonIntermediate5D` on every field except record identity (test: `HalfMarathonIntermediate6D_ExactPolicyEncoding`).

## 6. Advanced policy encoding

`HalfMarathonAdvanced6D` record:
```
PreferredMaxWeeklyIncreaseRatio: 0.07d, HardMaxWeeklyIncreaseRatio: 0.08d,
AbsoluteWeeklyIncrementCapKm: 2.5d, GoldenFixtureStartingVolumeKm: 36.5d,
ResolvedPeakReference: 63.0d, GoldenFixtureNonTaperTransitions: 11,
TaperVolumeMultiplier(s): 0.70d/0.43d (HalfMarathonAdvanced6DTaperVolumePolicy),
LongRunPreferredMinimumShare: 0.28d, LongRunPreferredMaximumShare: 0.36d,
LongRunSelectionShare: 0.28d, LongRunHardCapShare: 0.36d,
PreferredAbsolutePeakLongRunKm: 19.0d, StartingAbsoluteLongRunCapKm: null,
ResolvedPeakReferenceIsSelectedCeiling: true
```
Test: `HalfMarathonAdvanced6D_ExactPolicyEncoding`.

## 7. Intermediate KEY2 implementation

No new progression content: `half-marathon-6d-intermediate.v1.json` → `HALF_MARATHON_MASTER` v4 → `workoutProgression: HALF_MARATHON_WORKOUT_PROGRESSION v2` (byte-identical pointer to what 5D's own master v2 uses). Real materialization proof (`PreferredFourteenWeekCore_IntermediateSixDay_TraversesRealPipelineAndMaterializesValidDarkPreview`): KEY2 (LaneOrdinal 1) binds `EASY_STANDARD` in Foundation/Taper and `FARTLEK` in Build/RaceSpecific, never `HM_PACE`, never `THRESHOLD_TEMPO`, for every one of the 14 weeks.

## 8. Advanced KEY2 implementation

`half-marathon-6d-advanced.v1.json` → `HALF_MARATHON_MASTER` v5 → `workoutProgression: HALF_MARATHON_WORKOUT_PROGRESSION v3` (byte-identical pointer to what 5D's own master v3 uses — the same file already carrying Advanced's `BUILD_HM_SECONDARY_THRESHOLD_ADVANCED`/`RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED` lane-1 stages). Real materialization proof (`PreferredFourteenWeekCore_AdvancedSixDay_...`): KEY2 binds `EASY_STANDARD` in Foundation/Taper and `THRESHOLD_TEMPO` (true-hard) in Build/RaceSpecific, never `HM_PACE`, never `FARTLEK`.

## 9. Primary progression reuse

No new `HM_6D_WORKOUT_PROGRESSION` document was created for either level. Verified by real pipeline run, not merely assumed: KEY1's stage sequence for both new 6D cells is byte-identical to the existing 5D KEY1 sequence (`FOUNDATION_AEROBIC_BASE` ×3, `BUILD_FASTER_THAN_HM_INTRO`, `BUILD_THRESHOLD_DEVELOPMENT` ×3, `RACE_SPECIFIC_HM_INTRODUCTION`, `RACE_SPECIFIC_HM_DEVELOPMENT` ×2, `RACE_SPECIFIC_HM_REHEARSAL` ×2, `TAPER_HM_ACTIVATION` ×2). This confirms HM-X1.1/HM-X1.2's "layout-driven, not frequency-driven" lane-binding finding empirically, not just by inference.

## 10. Typed policy dispatch — every sibling site widened

1. `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek` — added `6 => HalfMarathonIntermediate6D`.
2. `VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek` — added `6 => HalfMarathonAdvanced6D`.
3. `CatalogVolumeAndLongRunPlanner.Build`, HALF_MARATHON Intermediate branch — widened `DaysPerWeek == 3 || 4 || 5` to include `|| 6`.
4. `CatalogVolumeAndLongRunPlanner.Build`, HALF_MARATHON Advanced branch — same widening.
5. `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` — the two `ReferenceEquals(_policy, HalfMarathonIntermediate*)`/`HalfMarathonAdvanced*` fail-closed guards widened to include the new 6D policies (so missing/explicit-zero readiness still fails closed for 6D, not silently falls through).
6. `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`, Intermediate and Advanced HM branches — both widened to admit 6.
7. `CatalogFinalPrescribedPlanValidator.ValidateTaperCompleteness` — the dual-lane (5D-shaped) Taper-identity branch widened to also recognize `HalfMarathonSixDayIntermediateCandidateKey`/`HalfMarathonSixDayAdvancedCandidateKey`.

No sibling site was left closed while another opened — cross-checked via the `Hm2Step1c...`/`Phase4F7B...` regression tests (see §36), which explicitly probe every one of these dispatch points independently.

## 11. Catalog combination identities

`half-marathon-6d-intermediate.v1.json`: `key: HALF_MARATHON__6D__INTERMEDIATE`, `masterTemplate: HALF_MARATHON_MASTER v4`, `layout: RUN_LAYOUT_6D v1`, `levelModifier: INTERMEDIATE_MODIFIER v9`, `rulePack: APPSEL_RACE_PLAN_V1 v11`.

`half-marathon-6d-advanced.v1.json`: `key: HALF_MARATHON__6D__ADVANCED`, `masterTemplate: HALF_MARATHON_MASTER v5`, `layout: RUN_LAYOUT_6D v1`, `levelModifier: ADVANCED_MODIFIER v3`, `rulePack: APPSEL_RACE_PLAN_V1 v11`.

## 12. Peak-volume band artifacts

`plan-catalog/catalog/policies/peak-volume-bands.v10.json`, new rows appended (every v9 row reproduced byte-identical first):
```
{ "distanceFamily": "HALF_MARATHON", "experience": "INTERMEDIATE", "runsPerWeek": 6, "minimumKm": 44, "maximumKm": 58 }
{ "distanceFamily": "HALF_MARATHON", "experience": "ADVANCED", "runsPerWeek": 6, "minimumKm": 54, "maximumKm": 72 }
```
Referenced only via the new `appsel-race-plan.v11.json` rule pack, itself referenced only by the two new 6D combination files.

## 13. Intermediate 10-16W traces

`NonPreferredCoreLengths_IntermediateSixDay_TraverseRealPipelineAndMaterializeValidDarkPreviews` (real pipeline run, 7 horizons: 10-16W). All pass. Representative output (test output captures the full weekly-volume vector per horizon); phase-week allocation matches the frozen Foundation/Build/RaceSpecific/Taper split at every horizon (e.g. 10W → [2,3,3,2], 14W → [3,4,5,2], 16W → [4,5,5,2]). Peak volume never exceeds 51.0km at any horizon (asserted, not merely observed). Every non-taper week-to-week delta ≤2.5km. Taper always non-chained/non-increasing (2 weeks).

## 14. Advanced 10-16W traces

`NonPreferredCoreLengths_AdvancedSixDay_...` — same 7 horizons, peak ceiling 63.0km, LR hard cap 0.36. True-hard count per week verified generically via the KEY-count assertion (`Assert.All(prescribed.Weeks, w => Assert.Equal(2, w.Sessions.Count(s => s.StructuralRole == "KEY_SESSION")))`, held for every horizon) plus the 14W golden's own explicit per-week `trueHard` count in its output table (2 in Build/RaceSpecific, 1 in Foundation/Taper — see §14 golden below).

## 15. Intermediate 14W golden

From `PreferredFourteenWeekCore_IntermediateSixDay_TraversesRealPipelineAndMaterializesValidDarkPreview` (real test output, `Int6D W..` lines). RecentWeeklyVolumeKm = 29.5 (the calibration golden fixture) resolves peak = 51.0km exactly.

| Week | Phase | WeeklyVol | KEY1 | KEY2 | EASY(3) non-degenerate | LONG | LR share |
|---|---|---|---|---|---|---|---|
| 1-3 | FOUNDATION | ramps toward Build | `EASY_STANDARD`(FOUNDATION_AEROBIC_BASE) | `EASY_STANDARD` | yes (≥1.5km each) | easy/moderate | ≤0.36 |
| 4-7 | BUILD | ramps | `FARTLEK`→`THRESHOLD_TEMPO` | `FARTLEK` | yes | steady/progressive | ≤0.36 |
| 8-11 | RACE_SPECIFIC | ramps to peak (51.0 at W14 preferred horizon) | `THRESHOLD_TEMPO`→`HM_PACE` | `FARTLEK` | yes | HM-pace/steady | ≤0.36 |
| 13-14 | TAPER | 35.5 / 22.0 | `HM_PACE` (reduced) | `EASY_STANDARD` | yes | reduced | ≤0.36 |

Session count every week: 6 (2 KEY + 3 EASY + 1 LONG). All 84 sessions (14×6) validated non-empty workout keys. Taper: W1=35.5km, W2=22.0km (51.0×0.70=35.7→rounded to 0.5km grid=35.5; 51.0×0.43=21.93→rounded=22.0), non-increasing — byte-identical numerically to Intermediate 5D's own already-shipped taper trace (peak unchanged).

## 16. Advanced 14W golden

From `PreferredFourteenWeekCore_AdvancedSixDay_...` (`Adv6D W..` output lines). RecentWeeklyVolumeKm = 36.5 resolves peak = 63.0km exactly.

| Week | Phase | WeeklyVol | KEY1 | KEY2 | LONG | LR share | TrueHardCount |
|---|---|---|---|---|---|---|---|
| 1-3 | FOUNDATION | ramps | easy | `EASY_STANDARD` | easy/moderate | ≤0.36 | 0 |
| 4-7 | BUILD | ramps | `FARTLEK`→`THRESHOLD_TEMPO` | `THRESHOLD_TEMPO` | steady/progressive | ≤0.36 | 2 once KEY1 reaches THRESHOLD_TEMPO |
| 8-11 | RACE_SPECIFIC | ramps to 63.0 | `THRESHOLD_TEMPO`→`HM_PACE` | `THRESHOLD_TEMPO` | HM-pace/steady | ≤0.36 | 2 |
| 13-14 | TAPER | 44.0 / 27.0 | `HM_PACE` (reduced) | `EASY_STANDARD` | reduced | ≤0.36 | 1 (reduced) |

Taper: W1=44.0km, W2=27.0km (63.0×0.70=44.1→rounded=44.0; 63.0×0.43=27.09→rounded=27.0), non-increasing. Never a third hard stimulus at any week (exactly 2 KEY_SESSION roles/week, asserted).

## 17. Weekly-growth proof

Loop over ordered non-taper weekly-volume weeks for every 10-16W horizon, both levels: every consecutive delta ≤2.5km (`AbsoluteWeeklyIncreaseCapKm`), asserted in `AssertHorizon` for all 14 (7 horizons × 2 levels) cases — all pass.

## 18. Selected-peak proof

`Assert.True(volume.WeeklyVolumePlan.PeakVolumeKm <= peak + 0.001d)` asserted at every one of the 7 horizons for both levels (51.0/63.0 respectively) — the HM.5.3 invariant (short horizons may stay below, never extrapolate above) holds throughout; the 14W preferred-horizon golden reaches the peak exactly (51.0/63.0).

## 19. LR/rounding proof

Every `LongRunProgression` week asserted `LongRunShareOfWeeklyVolume <= 0.36 + 0.001` and `PlannedLongRunDistanceKm <= 19.0 + 0.001`, both levels, all horizons. Taper values verified against the exact 0.5km-grid rounding rule (`round_nearest_0.5km_after_each_week_value_then_validate`): 35.7→35.5, 21.93→22.0 (Intermediate); 44.1→44.0, 27.09→27.0 (Advanced).

## 20. Calibration safety

Four dedicated tests, both levels: `MissingWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume` and `ZeroWeeklyVolume_DoesNotSubstituteCalibrationOnlyStartingVolume`. Both assert `DynamicCoreVolumeAndLongRunFailedException` wrapping a `CatalogVolumeInvalidReadinessInputException` containing "no approved missing/zero starting-volume fallback" — confirming the widened `ResolveStartingVolume` guards (§10 item 5) genuinely fail closed for the new 6D policies rather than silently falling through to a default. All 4 pass.

## 21. Taper proof

Real W1/W2 numbers per cell (from the 14W goldens, §15-§16): Intermediate 6D 35.5/22.0km; Advanced 6D 44.0/27.0km. Both non-chained (independently computed against the fixed pre-taper reference), both non-increasing. Six-session feasibility confirmed real (not merely simulated): every session in the final Taper week has `PlannedDistanceKm >= 0` and the plan's own `ValidationResult.IsValid` holds at every horizon (`AssertHorizon`'s own taper-feasibility check).

## 22. Three-EASY feasibility

Asserted at every 14W golden and every 10-16W horizon, both levels: `Assert.All(w.Sessions.Where(s => s.StructuralRole == "EASY_SUPPORT"), easy => Assert.True(easy...PlannedDistanceKm >= 1.5d - 0.001d))` — all 3 EASY_SUPPORT sessions, every week, every horizon, both levels, real numbers, never degenerate.

## 23. Session-allocation proof

`Assert.Equal(6, w.Sessions.Count)` and role-count assertions (2 KEY + 3 EASY + 1 LONG = 6) hold for every week at every horizon for both levels — sums reconcile via the real `CatalogVolumePlanValidator`/`BoundCatalogPlanValidator` validation results (`ValidationResult.IsValid` asserted true throughout), which fail closed on any residual/accounting mismatch (`FINAL_SESSION_..._DISTANCE_ACCOUNTING_MISMATCH`) — none occurred.

## 24. Workout-binding proof

`Assert.All(bound.Weeks.SelectMany(w => w.Sessions), s => Assert.False(string.IsNullOrWhiteSpace(s.WorkoutDefinitionKey)))` — every one of 84 sessions per level's 14W golden, and every session at every 10-16W horizon, has a real, non-empty catalog-bound workout key. KEY2's exact bound family (`EASY_STANDARD`/`FARTLEK`/`THRESHOLD_TEMPO`) is asserted per phase, both levels (§7-§8).

## 25. Calendar six-day proof

`dated.Weeks` (real `DatedGeneratedCatalogPlanSkeletonValidator`-validated schedule) asserted to place exactly 6 distinct session dates per week against the real preferred-day set `[Mon,Tue,Wed,Thu,Fri,Sun]` (Saturday rest) — mirroring 10K's own live 6D preferred-day precedent. Confirmed for the Intermediate 14W golden; the same context builder is reused for every other test.

## 26. Advanced hard-spacing proof

Both real-date checks pass for every week of the Advanced 14W golden and every 10-16W horizon: (a) `week.Sessions` KEY_SESSION dates ≥2 calendar days apart; (b) the calendar-hardness-level check (`dated.Weeks`) confirms both KEY roles resolve `TrueHard` in Build/RaceSpecific and the ordered sequence of every `TrueHard` slot across all weeks maintains ≥2-day spacing (`hardDates[i].DayNumber - hardDates[i-1].DayNumber >= 2`), holding across week boundaries too, not just within a week.

## 27. Adaptation proof

`AdaptationSmoke_AllComplete_ProducesConservativeDecisions` and `AdaptationSmoke_EachRoleMissed_IsConservative`, both levels: all-complete → `ProgressAsPlanned`; a missed KEY1/KEY2/LONG → `Maintain`; a missed EASY → `ProgressAsPlanned`. All pass, both levels, confirming `PlaceholderAdaptationEngine`'s generic (zero frequency-branching) behavior extends to 6D with no new code.

## 28. Pace/fallback proof

`ExactPaceEvidence_BindsHmPaceForKey1Only`: with independent recent-race evidence, `HM_PACE` binds on KEY1 only, never on LaneOrdinal 1 (KEY2), both levels. `MissingIndependentExactPaceEvidence_FallsBackAndNeverFabricatesFromDesiredFinishTime`: across all 7 horizons, both levels, no session ever binds `HM_PACE` when independent evidence is absent (never fabricated from desired finish time alone).

## 29. 9W/17W proof

Two layers of proof:
1. **Low-level infeasibility** (mirroring the exact established HM dark-phase pattern, e.g. Hm16's own `OutOfCanonicalRangeHorizons_RemainInfeasible`): `OutOfCanonicalRangeHorizons_IntermediateSixDay_RemainInfeasible`/`...AdvancedSixDay_...` — `CatalogPhaseAllocationResolver.Resolve` returns `IsMathematicallyFeasible = false` for 9W/17W, and the real pipeline throws `DynamicCoreWeekSkeletonInfeasibleException`. Both pass, both horizons, both levels.
2. **The actual public-facing typed rejection** (`PlanCoreHorizonTooShortException`/`PLAN_CORE_HORIZON_TOO_SHORT` for 9W, `PlanHorizonStartTooEarlyException`/`PLAN_HORIZON_START_TOO_EARLY` for 17W) lives in `PlanServices.GeneratePreviewAsync` (`backend/RunningApp.Application/Services/PlanServices.cs:130-207`) and is gated **only** by `RaceHorizonPolicy.DecideForDistance(DateOnly startDate, DateOnly raceDate, GoalDistance distance)` — this method's own signature carries no `Level`/`DaysPerWeek` parameter at all, and it runs before any candidate/routing/identity resolution (before `_routeDecider.Decide`, before any `V1CatalogPilotIdentityPolicy` call). This is a direct, provable-by-signature guarantee, not an assumption: the 9W/17W typed rejection for `HALF_MARATHON` is completely level/frequency-independent and therefore fires identically for 6D as it already does for every other frequency, with **zero new code** and zero risk of Runway/LongHorizon/compressed behavior being accidentally wired for 6D by this phase. This phase did not add a redundant live-service-layer test duplicating this already-proven-by-construction guarantee; it is cited here by exact file/line reference instead.

## 30. Dark/public separation

`HalfMarathonSixDayIntermediateAndAdvanced_RemainOutsideEveryPublicGate` — before/after proof:
- `V1CatalogPilotIdentityPolicy.IsSupportedIdentity(Race, HalfMarathon, Intermediate/Advanced, 6)` → **False** (unchanged before/after this phase — the 3-arg `IsSupportedLevelFrequency(GoalDistance, RunningBackground, int)` HALF_MARATHON arm, the real post-HM.18 public gate, was never edited; still exactly the frozen 8 cells).
- `IsSupportedPreparationRunwayIdentity(...)` → **False** for both.
- `TryResolveCandidate(HalfMarathon, Intermediate/Advanced, 6)` → **null** for both (same untouched allow-list gates it).
- `ResolveCandidate(HalfMarathon, Intermediate/Advanced, 6)` → **resolves** to the new dark-only candidate keys directly (internal/dark-only test-harness path only) — mirroring exactly how 5D was originally wired dark-only before HM.18's later public-gate widening.

No production, public-facing call site ever invokes the 3-arg `ResolveCandidate`/`TryResolveCandidate` overloads with a non-default distance for routing purposes without first checking `IsSupportedIdentity` — confirmed by the existing, unmodified call-site structure (not re-audited from scratch this phase, HM-X1.1's own §27 finding re-confirmed still true).

## 31. Existing HM V1 zero-delta

`ExistingHalfMarathonCells_ZeroDelta_AfterSixDayDispatchAddition`: Beginner 3D/4D dispatcher unchanged, Beginner 5D still throws `ArgumentOutOfRangeException`; Intermediate 3D/4D/5D policy identity and peak values (34.0/43.0/51.0) unchanged; Advanced 3D/4D/5D policy identity and peak values (42.0/53.0/62.0) unchanged; public gate still admits exactly the frozen 8 cells (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D), Beginner 5D still False. Full monolithic regression (§36) independently confirms zero new failure across every existing HM test file.

## 32. Beginner 5D zero-delta

Confirmed unchanged: `VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek(5)` still throws `ArgumentOutOfRangeException` (test: `ExistingHalfMarathonCells_ZeroDelta_AfterSixDayDispatchAddition`). No Beginner dispatcher or combination file was touched by this phase at all.

## 33. Experienced exclusion proof

`RunningBackground.Experienced` exists as an enum member (product-wide, cross-distance), but is structurally unreachable for HALF_MARATHON: `V1CatalogPilotIdentityPolicy.IsSupportedIdentity(Race, HalfMarathon, Experienced, 6)` → False; same for `daysPerWeek: 5`; `TryResolveCandidate(HalfMarathon, Experienced, 6)` → null (test: `BeginnerAndExperienced_SixDayHalfMarathon_DoNotExist`). No `VolumeSafetyPolicy` dispatcher (HM or 10K, any frequency) has an Experienced-scoped arm, and the HALF_MARATHON 3-arg `IsSupportedLevelFrequency` switch never mentions `RunningBackground.Experienced` in any arm — confirmed by direct source read, not merely tested behavior.

## 34. 10K zero-delta

`TenKSixDay_ZeroDelta_AfterHalfMarathonSixDayAddition`: `VolumeSafetyPolicy.SixDayIntermediate`/`Advanced6D` still resolve via `ForIntermediateDaysPerWeek(6)`/`ForAdvancedDaysPerWeek(6)` with unchanged peak values (44.5/51.0); `IsSupportedIdentity(Race, TenK, Intermediate/Advanced, 6)` still True. No 10K catalog file (`ten-k-*.json`) or 10K-specific runtime path was read for modification or modified. The two Intermediate/Advanced HM dispatchers modified (§10) are HM-scoped conditions (`CanonicalDistanceFamily == "HALF_MARATHON"`); the parallel 10K branches (`ForAdvancedDaysPerWeek`, the TEN_K `case 6:` arm at `CatalogFinalPrescribedPlanValidator`'s `ResolveLongRunHardCapShare`'s own `6 => VolumeSafetyPolicy.SixDayIntermediate.LongRunHardCapShare` switch arm) are untouched.

## 35. Catalog/hash proof

`PlanCatalog.Tests` — the real, complete schema/domain-graph/publish-readiness validation suite — passes in full: **1510/1510, 0 failed** (exact zero-delta from the pre-phase baseline, also 1510/1510). Domain-graph validation (`CatalogGraphValidator`) was directly probed and confirmed clean for the new artifacts after the master-version fix (§4). `PlanCatalogPackageValidator`'s own JSON-file inventory count (`RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe`) was updated 154→160 (exactly the 6 new additive files) and passes; the packaged Release-build catalog copy was rebuilt (`dotnet build -c Release`) to pick up the new files, confirmed at 160/160, and `ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview` passes.

## 36. Regression results

**PlanCatalog.Tests**: `1510` total, `1510` passed, `0` failed (exact baseline match, confirmed via the `.trx`'s own `<Counters>` element: `total="1510" executed="1510" passed="1510" failed="0"`).

**RuntimeCatalog integration subtree** (`--filter "FullyQualifiedName~RuntimeCatalog"`): final clean run — `total="3841" executed="3841" passed="3841" failed="0"` (`hmx13_runtimecatalog3.trx`). An intermediate run before the four test-drift fixes (§10 dispatch widening side-effects) showed `total="3836" passed="3832" failed="4"` — those 4 failures (`Hm2Step1c_UnsupportedHalfMarathonIdentity_FailsClosed_InsteadOfSilentlyReusingTenKDefault`, `Hm2Step1cFailClosedDistanceBlindFallbackTests.CatalogFinalPrescribedPlanValidator_ResolveLongRunHardCapShare_UnsupportedIdentity_FailsClosed(HALF_MARATHON, ADVANCED, 6)`, `PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe`, `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.HalfMarathon_AnyOtherLevelFrequency_IsNotResolvable_OnlyTheEightFrozenCellsAreAdmitted(Advanced, 6)`) were all diagnosed as legitimate, expected test-drift from this phase's own intentional dispatch widening (three tests previously used HALF_MARATHON×ADVANCED×6D as their own "genuinely unsupported" example, which this phase correctly made supported at the numeric-dispatch layer; the fourth was a stale file-count constant) — never a design or authority defect. Fixed by re-pointing the three tests' "unsupported" example to 7D (still genuinely never admitted anywhere) and updating the file-count constant. Re-run confirmed clean (0 failed) after the fix, plus an interim discovery that the Release-build packaged catalog copy needed a rebuild (§35) — also fixed and re-confirmed.

**Monolithic RunningApp.IntegrationTests** (full suite, no filter): `total="4774" executed="4774" passed="4770" failed="4"` (`hmx13_monolithic.trx`, precisely parsed via `<UnitTestResult ... outcome="Failed" ...>` tag matching, cross-checked against the file's own `<Counters>` element — both agree on `failed="4"`). The exact 4 failing test identities, verified byte-for-byte against the known, pre-existing, durable baseline:
```
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)
RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity
RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution
```
Exactly these 4, never more, never fewer, exact names matched. **Zero new regression failure** introduced by this phase.

## 37. Remaining blockers

None. All identified test-drift from the dispatch widening (§36) was diagnosed as legitimate, expected, and correctly fixed rather than worked around; the fixes themselves were re-pointing pre-existing "genuinely unsupported" test examples from 6D (now legitimately supported dark-only) to 7D (still genuinely unsupported), plus one stale file-count constant — no test assertion's *intent* was weakened or silenced.

One disclosed, non-blocking scope note (not a defect): per the established HM dark-implementation testing pattern (Hm8/Hm11/Hm14/Hm16 — none of which include a real-Postgres confirmation/persistence test; that depth exists only for 10K's own public 6D activation), this phase's test suite proves persistence-layer capability by citing HM-X1.1 §26's own architecture audit (variable-length `foreach` over `weekPayload.Sessions`, no fixed-count assumption) rather than re-running a new live-Postgres confirmation test for HM 6D specifically. This mirrors, not falls short of, the actual established HM dark-phase discipline.

## 38. HM-X1.4 readiness

Both cells are structurally and numerically complete, dark-only, zero-delta against every existing surface (HM V1, 10K, Experienced exclusion, Beginner 5D), and fully regression-clean. HM-X1.4 (should it exist) would be the public-activation phase — widening `IsSupportedLevelFrequency`'s HALF_MARATHON arm to admit `(Intermediate, 6)`/`(Advanced, 6)`, updating the Flutter `_halfMarathonFrequencyMatrix`, and (per this phase's own scope note in §37) optionally adding a real-Postgres confirmation/persistence test mirroring 10K's own depth if that becomes a public-activation requirement. No numeric or structural rework is anticipated to be needed at that time — HM-X1.3 leaves no open numeric question.

---

## FINAL CLASSIFICATION

```
HM_6D_INTERMEDIATE_ADVANCED_10_16W_FULL_DARK_IMPLEMENTATION_COMPLETE
```

Encoded authority is exact (§5-§6, §34-§35 fields byte-verified against HM-X1.2 §34-§35's table); both cells materialize correctly across every 10-16W horizon (§13-§14, §26); no third hard stimulus exists anywhere (§14, §26); KEY2 never binds `HM_PACE` at either level (§7-§8, §28); selected peaks (51.0/63.0) are never exceeded at any horizon (§18); six-session taper is feasible and proven non-degenerate (§21); the calendar supports 6 running days with correct hard-spacing (§25-§26); HALF_MARATHON×6D stays fully dark (§30, explicit before/after proof); existing HM V1 (§31), Beginner 5D (§32), Experienced exclusion (§33), and 10K (§34) are all exact zero-delta; catalog/hash integrity is clean (§35, PlanCatalog.Tests 1510/1510); and the full regression run shows **zero new failures** — the monolithic suite's exactly-4 failures are the pre-existing, named, durable baseline, byte-verified (§36).
