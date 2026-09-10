# PHASE HM.5.3 — Half-Marathon Dynamic-Core Volume and Long-Run Boundary Semantic Closure

**Status: DONE — `HM_DYNAMIC_CORE_VOLUME_AND_LONG_RUN_BOUNDARIES_CLOSED_ZERO_DELTA`.**

## 1. Frozen authorities (verified, not re-decided)

Read directly from `VolumeSafetyPolicy.HalfMarathonIntermediate4D` and prior HM reports before any code was touched:

- `GoldenFixtureStartingVolumeKm = 25.0`, `ResolvedPeakReference = 43.0` (`ProductDefaultWithEvidenceEnvelope`), `GoldenFixtureNonTaperTransitions = 11`.
- `PreferredMaxWeeklyIncreaseRatio = 0.07`, `HardMaxWeeklyIncreaseRatio = 0.08`, `AbsoluteWeeklyIncrementCapKm = 2.5`.
- `LongRunPreferredMinimumShare = 0.30`, `LongRunPreferredMaximumShare = 0.36`, `LongRunSelectionShare = 0.33`, `LongRunHardCapShare = 0.40`.
- `PreferredAbsolutePeakLongRunKm = 19.0` (ceiling, not target — HM.1.1).
- `RoundingIncrementKm = 0.5`.
- Taper: `TaperVolumeMultipliers` ordered `[0.70, 0.43]` (HM.1.4B), non-chained, each applied independently against one fixed pre-taper anchor.
- Peak weekly-volume band for this cell: 36–50 km/week (catalog peak-volume-band artifact, unchanged).

None of these values were read as changeable. None were changed.

## 2. Exact reproduction of all three blockers (before any code change)

Reproduced by running the existing (pre-fix) `Hm2FullDarkVerticalSliceTests` against the real, unmodified pipeline:

- **12W**: `TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker(12, ...)` threw `DynamicCoreSessionPrescriptionFailedException` → `CatalogFinalPrescribedPlanInvalidException` containing `FINAL_WEEK_10_LONG_RUN_SHARE_EXCEEDS_CAP`.
- **15W**: same shape, `FINAL_WEEK_13_LONG_RUN_SHARE_EXCEEDS_CAP`.
- **16W**: `SixteenWeekHorizon_HitsGenuinePeakVolumeCeilingOvershootBlocker` asserted (and the real planner produced) `WeeklyVolumePlan.PeakVolumeKm == 46.5`, i.e. `> 43.0`.

All three tests passed *before* this phase touched any production code (they were written by HM.5.1 specifically to pin the bug), confirming the reproduction matches the disclosed baseline exactly.

## 3. Current `ResolvePeak` formula (pre-fix)

```
nonTaperWeeks = count(weeks where PhaseKey != "TAPER")
transitions = max(0, nonTaperWeeks - 1)
canonicalDefaultMultiplier = ResolvedPeakReference.Value / GoldenFixtureStartingVolumeKm   // 43/25 = 1.72
transitionAdjustedMultiplier = 1 + (canonicalDefaultMultiplier - 1) * transitions / GoldenFixtureNonTaperTransitions
reachable = startingVolumeKm * transitionAdjustedMultiplier
```

For 14W: `nonTaperWeeks = 12`, `transitions = 11 = GoldenFixtureNonTaperTransitions` → `transitionAdjustedMultiplier = canonicalDefaultMultiplier` exactly → `reachable = 25 * 1.72 = 43.0`. Matches golden exactly.

For 16W: `nonTaperWeeks = 14`, `transitions = 13 > 11`. `transitionAdjustedMultiplier = 1 + 0.72 * 13/11 = 1.8509...`. `reachable = 25 * 1.8509 = 46.27...` → rounded to the 0.5 grid → **46.5**. Matches the disclosed value exactly.

## 4. 16W extrapolation root cause

`GoldenFixtureNonTaperTransitions` is a **calibration point** — the number of non-taper transitions the 14W golden fixture itself has. The formula treats it as a straight-line slope definition (`(canonicalDefaultMultiplier−1)/GoldenFixtureNonTaperTransitions` per transition) with **no upper bound on `transitions`**. For any horizon whose non-taper transition count is at or below the calibration point, this is legitimate interpolation. For any horizon whose count exceeds it (only 15W: 12 transitions, and 16W: 13 transitions, in the current HM Core family), the same formula silently becomes **extrapolation past the calibration point**, and the multiplier — and therefore the resolved peak — grows without any ceiling. This is exactly assumption family "more transitions imply a proportionally higher peak," stated explicitly in the governing prompt.

The critical semantic question (governing prompt §4): does more Core duration mean (A) more time to reach the same selected peak, or (B) authority to raise the selected peak itself? The frozen HM authority (§2's `ResolvedPeakReference = 43.0` as the cell's *selected* peak) requires (A) for this cell specifically.

**Important correction made mid-phase**: an initial fix unconditionally capped every policy's interpolation at its own calibration count. Running the full regression against that version surfaced a genuine 10K zero-delta violation: `Gen4DBeginnerFourDayCoreTests.MissingReadiness_AllHorizonsGenerate_WithFourDayCardinality` failed at 13W/14W (expected pre-taper 22/23km, got 21km) — `VolumeSafetyPolicy.BeginnerFourDay` (`ResolvedPeakReference=21`, `GoldenFixtureNonTaperTransitions=10`) *already*, and correctly, extrapolates past its own reference for its longer horizons; this is real, already-shipped, already-tested 10K product behavior, not a bug. So question (A) vs (B) does not have one universal answer across every policy — it is a per-policy product decision, and 10K's own answer for `BeginnerFourDay` is (B) while HM's frozen answer for this cell is (A). See §5 for the resulting contract fix.

## 5. Selected-peak vs reachable-peak semantic

This is precisely the "smallest typed seam" case the governing prompt anticipated: the existing contract (`ResolvedPeakReference`) cannot distinguish "this is a hard selected-peak ceiling" from "this is a calibration point a longer horizon may extrapolate past" — and, per §4, different policies genuinely need different answers. Rather than branch on `CanonicalDistanceFamily`/`Level`/`DaysPerWeek` inside the shared `ResolvePeak` algorithm (which the governing prompt's hard boundaries forbid — no per-distance special case, no Half-Marathon-only planner), a new, additive, opt-in boolean was added to the policy contract itself: `VolumeSafetyPolicy.ResolvedPeakReferenceIsSelectedCeiling` (default `false`). `ResolvePeak` reads only this generic flag — it contains no HM/10K/distance-family conditional anywhere. Every existing 10K policy leaves the field at its default (`false`), preserving today's extrapolation-permitted behavior exactly; only `HalfMarathonIntermediate4D` sets it `true`, expressing this cell's frozen "43.0km is a ceiling for the whole Core family" authority declaratively, as policy data, not as algorithm logic.

## 6. Minimal peak mechanism fix

In `CatalogVolumeAndLongRunPlanner.ResolvePeak`, cap the interpolation numerator at the calibration count, but only for a policy that opts in:

```csharp
var cappedTransitions = _policy.ResolvedPeakReferenceIsSelectedCeiling
    ? Math.Min(transitions, _policy.GoldenFixtureNonTaperTransitions)
    : transitions;
var transitionAdjustedMultiplier = 1d + ((canonicalDefaultMultiplier - 1d) * cappedTransitions / _policy.GoldenFixtureNonTaperTransitions);
```

with `HalfMarathonIntermediate4D` (only) setting `ResolvedPeakReferenceIsSelectedCeiling: true`.

Effect for `HalfMarathonIntermediate4D`: for `transitions <= GoldenFixtureNonTaperTransitions` (HM 10W–14W), `cappedTransitions == transitions` — byte-identical formula, byte-identical output, including the 14W golden. For `transitions > GoldenFixtureNonTaperTransitions` (HM 15W/16W only), the multiplier **saturates** at `canonicalDefaultMultiplier` — the exact ratio the golden fixture itself resolves to — so `reachable` asymptotically equals (and, for this fixture's starting volume, resolves to exactly) `ResolvedPeakReference.Value`, never exceeds it.

Effect for every 10K policy (flag left at its `false` default): `cappedTransitions == transitions` unconditionally — the exact pre-phase formula, unchanged in every branch, for every horizon, confirmed both by construction (the ternary short-circuits to the untouched expression) and empirically (`Gen4DBeginnerFourDayCoreTests` re-passes unchanged after this correction, §17). No `Math.Min(..., 43)` HM-specific special case exists in the algorithm; no distance-family/Level/DaysPerWeek branch was added to `ResolvePeak`; no VolumeSafetyPolicy numeric field changed. Same single generic formula, same single generic policy contract, for every distance and every horizon — the only new surface is one additive, opt-in boolean read generically.

## 7. 12W long-run root cause

Traced `BuildLongRunPlan`'s per-week clamp: `selected = Round(Clamp(unclamped, lower, Math.Min(upper, hardCap)))`, where (pre-fix) `hardCap = Round(weeklyVolume * hardCapShare)` used the same nearest-rounding (`Math.Round(..., MidpointRounding.AwayFromZero)`) as every ordinary target value in this class. For 12W's week 10 (mid-`RACE_SPECIFIC`, HM.1.6 share-ramp active), the real `weeklyVolume` and `hardCapShare = 0.40` combination produced a raw ceiling whose nearest-rounding (to the 0.5 km grid) rounded **upward**, past the true 40%-of-weekly-volume value — the planner then clamped `selected` up to that inflated ceiling. `CatalogFinalPrescribedPlanValidator`'s own hard-cap check (`prescribedLongRun.PlannedDistanceKm > week.PlannedWeeklyVolumeKm * hardCap + 0.001`) uses the **unrounded** `hardCapShare` fraction directly with only a 0.001 km tolerance, so it correctly rejected the inflated, rounded-up value the planner had produced — `FINAL_WEEK_10_LONG_RUN_SHARE_EXCEEDS_CAP`. This is defect family **(B)** from the governing prompt's own list: "raw distance is valid but rounding pushes it above 40%."

## 8. 15W long-run root cause

Identical mechanism, different week (week 13, also mid-`RACE_SPECIFIC` under the HM.1.6 ramp) and identical root cause — the same nearest-rounded `hardCap` ceiling in `BuildLongRunPlan`. Not a second, independent defect: same formula, same class, same fix.

## 9. LR rounding/safety analysis

`CatalogVolumeAndLongRunPlanner`'s single `Round` helper (`Math.Round(value / increment, AwayFromZero) * increment`) is correct and desired for **target** values (starting volume, weekly-volume interpolation, long-run selection target) — those are meant to land on the nearest grid point. It is **not** safe when the same helper is reused to compute a **hard ceiling**: rounding a ceiling to the nearest grid point can move it *up*, which silently widens the very limit it is supposed to enforce. This is exactly the governing prompt's own worked example (43.0 × 0.40 = 17.2 → nearest-0.5 rounds to 17.5, and 17.5/43 > 40%). The fix is the resolution order the prompt prescribes: **a value used as a safety ceiling must round down, never to nearest** — the final materialized value must satisfy the hard cap after rounding, and flooring the ceiling guarantees that any planner-selected value clamped to it does too.

## 10. Minimal LR mechanism fix

Added a second, explicitly ceiling-only rounding helper:

```csharp
private double RoundDown(double value) => Math.Floor(value / _policy.RoundingIncrementKm) * _policy.RoundingIncrementKm;
```

and used it only for the two values that function as hard safety ceilings in `BuildLongRunPlan`:

```csharp
var hardCap = RoundDown(weeklyVolume * hardCapShare);
...
if (share.PreferredAbsolutePeakLongRunKm is { } absoluteCeilingKm)
{
    hardCap = Math.Min(hardCap, RoundDown(absoluteCeilingKm));
}
```

`lower`, `upper`, and every ordinary target (`target = Round(...)`) are unchanged — they are preferred/selection values, not hard ceilings, and the prompt's own §9 explicitly forbids turning the hard cap into the ordinary target. `selected` is still computed exactly as before (`Round(Clamp(unclamped, lower, Math.Min(upper, hardCap)))`); only the ceiling it is clamped against is now floor-rounded. Because the floored ceiling is, by construction, never greater than the true fractional/absolute limit, every value the planner selects at or below it is also accepted by `CatalogFinalPrescribedPlanValidator`'s own unrounded check — restoring planner/validator parity (see §11) without touching any of the 30/33/36/40% share authorities, the 19 km absolute ceiling, or the HM.1.6 ramp mechanism itself.

## 11. Planner/validator parity

- **Weekly-volume peak**: only one formula (`ResolvePeak`) computes the resolved/selected peak; `CatalogVolumePlanValidator.ValidateWeeklyPlan` only checks the *output* against `plan.PeakVolumeKm`/`plan.CatalogBounds`, it never re-derives the peak independently. No duplication found.
- **Long-run hard cap**: two independent computations of the *same* 0.40 authority existed — `BuildLongRunPlan`'s internal `hardCap` (used to clamp) and `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare(candidate)` (used to verify the final session-bound distance). Both correctly resolve to the same `VolumeSafetyPolicy.HalfMarathonIntermediate4D.LongRunHardCapShare = 0.40` value/source (no drift in the *authority* itself), but before this phase they applied **different rounding treatments** to it — the planner rounded its derived ceiling to nearest, the validator compared against the raw fraction. That rounding-treatment mismatch, not a numeric-authority mismatch, was the actual parity gap. §10's fix closes it: the planner's ceiling is now guaranteed `<=` the validator's raw threshold, so every planner-emitted value passes the validator by construction. `VolumeProgressionVerifier`/`LongRunProgressionVerifier` (Materialization namespace, not on any live path) independently re-check the same raw, unrounded authority fractions against the plan's actual numbers — they needed no code change, and now report zero hard-cap violations for 12W/15W where they previously would have (their own doc comments already anticipated exactly this class of check).

## 12. Recurring-assumption-family findings

**"More transitions imply a proportionally higher peak" (§4 family):** searched every `VolumeSafetyPolicy` instance's `GoldenFixtureNonTaperTransitions` (2D/3D/4D/5D/6D × Beginner/Intermediate/Advanced, 10K and HM). Every 10K policy uses `GoldenFixtureNonTaperTransitions = 10` and, critically, several 10K policies (confirmed concretely for `BeginnerFourDay` via `Gen4DBeginnerFourDayCoreTests`) *do* exercise horizons whose non-taper transitions exceed that calibration count, and *do* rely on the resulting extrapolation as real, accepted product behavior — this was the mid-phase correction in §4/§5. Classification: **LEGACY_NARROW_CASE** for every 10K policy (the extrapolation-past-calibration behavior is real, intentional, and explicitly left untouched via the opt-in flag defaulting to `false`); **SAME_DEFECT_FAMILY** for `HalfMarathonIntermediate4D`'s 15W/16W specifically, where the identical mechanical pattern collides with a *different*, frozen product decision (43.0km is a hard ceiling for this cell) — now closed via the opt-in flag; no `UNREACHABLE`/`UNKNOWN` cases found — the formula and its single call site are fully enumerable, and the flag makes the two per-policy answers explicit rather than accidental.

**"Desired long-run progression may be resolved before hard weekly-share intersection" / "rounding may occur after safety evaluation without revalidation" (§8/§12 family):** searched every place a `*Share` or `*Cap` field feeds a `Round(...)` call in `CatalogVolumeAndLongRunPlanner`. Found exactly the one instance fixed in §10 (`hardCap`, plus its absolute-ceiling narrowing). `lower`/`upper` are preferred-band values, not safety ceilings — rounding them to nearest is correct product behavior (a preferred-band edge is not a hard failure boundary) and out of scope per the prompt's own §9 (33% remains an ordinary target, not a hard cap; the hard cap is the *only* value requiring floor-safety). `V1BeginnerThreeDayTaperLongRunSharePolicy`'s own hard-cap share is passed through the exact same `hardCapShare` variable and therefore the exact same `RoundDown` fix — no separate occurrence to fix. `ClassifyLongRunCompatibility` compares raw, unrounded ratios directly against `LongRunHardCapShare`/`LongRunPreferredMaximumShare` — no rounding step exists there to be unsafe. Classification: **SAME_DEFECT_FAMILY** (the one fixed instance); **SAFE** everywhere else this pattern was searched.

## 13. Exact 10–16W numeric traces (post-fix, real dark pipeline, `HALF_MARATHON × INTERMEDIATE × 4D`, `RecentWeeklyVolumeKm=25`)

| Horizon | Phases (F/B/RS/T) | PeakWeeklyVolumeKm | PeakLongRunShare ≤ 0.40 | PeakLongRunKm ≤ 19 | PreTaperAnchorKm | Taper W1 | Taper W2 | Final plan valid |
|---|---|---|---|---|---|---|---|---|
| 10W | 2/3/3/2 | ≤43.0 | Yes | Yes | matches pre-taper week | anchor×0.70 | anchor×0.43 | Yes |
| 11W | 2/3/4/2 | ≤43.0 | Yes | Yes | matches pre-taper week | anchor×0.70 | anchor×0.43 | Yes |
| 12W | 2/3/5/2 | ≤43.0 | Yes (week 10 now compliant) | Yes | matches pre-taper week | anchor×0.70 | anchor×0.43 | Yes |
| 13W | 2/4/5/2 | ≤43.0 | Yes | Yes | matches pre-taper week | anchor×0.70 | anchor×0.43 | Yes |
| 14W (golden) | 3/4/5/2 | **43.0 exactly** | Yes | Yes | matches pre-taper week | 30.0 | 18.5 | Yes |
| 15W | 4/4/5/2 | ≤43.0 | Yes (week 13 now compliant) | Yes | matches pre-taper week | anchor×0.70 | anchor×0.43 | Yes |
| 16W | 4/5/5/2 | **43.0 exactly** (was 46.5) | Yes | Yes | matches pre-taper week | anchor×0.70 | anchor×0.43 | Yes |

All seven rows verified via the real, unmodified `DynamicCoreCalendarMaterializationOrchestrator` pipeline (`Hm2FullDarkVerticalSliceTests`, `NonPreferredCoreLengths_NowFeasible_TraverseRealPipelineAndMaterializeValidDarkPreviews` theory extended to include 12/15/16, plus two dedicated proof tests for the previously-offending weeks). No horizon's peak exceeds 43.0 km; no horizon is required to reach it; no long run exceeds 40% of its own week's volume (post-rounding) or 19 km absolute.

## 14. 14W golden zero-delta

`PreferredFourteenWeekCore_TraversesRealPipelineAndMaterializesValidDarkPreview` — unmodified, no assertion touched — passes unchanged: `PeakVolumeKm == 43.0` exactly (transitions=11=calibration count, `cappedTransitions` is a no-op), taper `30.0 → 18.5` exactly (taper ceiling fix is a no-op here since 14W's long-run values were never near the hard cap), phase allocation `[3,4,5,2]` unchanged, stage sequence unchanged, workout binding unchanged, pace derivation unchanged. Confirmed by direct test run, not by inspection alone.

## 15. Taper proof

`Hm14bTaperVolumeMultiplierMechanismTests` (HM.1.4B's own suite) — unmodified, all passing after this phase's changes. Neither fix touches `ResolveTaperDecision` or the taper branch of `BuildWeeklyPlan`. For every 10–16W horizon, `preTaperAnchorKm` is still captured exactly once, at first taper-week entry, and both taper weeks still derive independently from that one fixed anchor (`anchor × 0.70`, `anchor × 0.43`) — never chained, never re-derived from a shorter horizon's lower anchor in any special-cased way (a shorter horizon that peaks below 43 km simply produces a lower anchor and thus lower taper values, exactly as intended, with zero code change to that mechanism).

## 16. Stage-allocation preservation

`CatalogPhaseAllocationResolver` and `ProgressionStageAllocator` were not touched. `Hm5StageAllocatorMechanismProofTests` and every HM.5.2/HM.5.2A structural-occupancy assertion (RACE_SPECIFIC_HM_REHEARSAL exactly twice, TAPER_HM_ACTIVATION exactly twice, Build split table) pass unchanged for all seven horizons, confirmed by the same test run as §14/§15 (162/162 in the `Hm2FullDarkVerticalSliceTests`+`HalfMarathon` namespace filter, zero failures).

## 17. 10K zero-delta proof

The two code changes are both structured to be no-ops for every 10K policy:

- §6's transition cap is now gated on `VolumeSafetyPolicy.ResolvedPeakReferenceIsSelectedCeiling`, which defaults to `false` and is set `true` only on `HalfMarathonIntermediate4D`. This is not merely a "no 10K call site reaches the trigger condition" argument — it was **caught empirically**: an earlier, unconditional version of this cap (capping every policy's transitions, regardless of a flag) broke `Gen4DBeginnerFourDayCoreTests.MissingReadiness_AllHorizonsGenerate_WithFourDayCardinality` at 13W/14W (expected pre-taper 22km/23km, actual 21km) — proof that `BeginnerFourDay` genuinely does extrapolate past its own `ResolvedPeakReference` today as real, accepted product behavior. After gating the cap on the new opt-in flag, this exact test re-passes unchanged, confirmed by direct test run (§18).
- §10's `RoundDown` ceiling only changes behavior when nearest-rounding would have rounded a hard-cap ceiling *upward* past the true fraction; for the 10K policies whose `PreferredMaximumShare < HardCapShare` (per `LongRunProgressionVerifier`'s own doc-comment finding), the planner's `Math.Min(upper, hardCap)` already resolves to `upper` in practice, so `hardCap`'s own rounding treatment is provably inert for every existing 10K weekly-volume/share combination that has ever been exercised — confirmed by the same full regression run showing zero 10K test-outcome changes.

Full `PlanCatalog.Tests` run: **1510 passed / 0 failed / 1510 total** (unchanged from the HM.5.2A baseline). Full `RunningApp.IntegrationTests` regression (§19) shows zero new failures anywhere in the 10K namespace and the exact same durable baseline failure set by name — the empirical zero-delta proof this phase requires, now doubly confirmed by the `Gen4DBeginnerFourDayCoreTests` catch-and-fix above.

## 18. Catalog/runtime regression

- Targeted `Hm2FullDarkVerticalSliceTests` + `HalfMarathon` + `Gen4DBeginnerFourDayCoreTests` filter: **180 passed / 0 failed / 180 total** (the `Gen4DBeginnerFourDayCoreTests` inclusion specifically re-confirms the 10K zero-delta catch in §4/§17).
- Full `RuntimeCatalog` subtree: **3660 passed / 0 failed / 3660 total**, 5m49s.
- `PlanCatalog.Tests`: **1510 passed / 0 failed / 1510 total**.
- Full `RunningApp.IntegrationTests` monolithic run: see §19.

## 19. Full integration regression

Run sequentially, after `PlanCatalog.Tests` and the full `RuntimeCatalog` subtree run had both completed (never concurrently, per this engagement's established testing discipline): `dotnet test backend/RunningApp.IntegrationTests -c Debug --no-build`.

**Observed: 4567 passed / 3 failed / 4570 total, 40m12s.**

The 3 failures, checked by exact test name and error against the documented durable baseline:

| Failing test | Error | Matches documented baseline? |
|---|---|---|
| `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` | `Expected: OK / Actual: InternalServerError` | Yes — exact name+week match |
| `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)` | `Expected: OK / Actual: InternalServerError` | Yes — exact name+week match |
| `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` | `Expected: OK / Actual: InternalServerError` | Yes — exact name match |

The fourth documented durable baseline member (`LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, the recurring wall-clock-date artifact) did **not** trigger on this run — consistent with its own documented ~4-of-7-calendar-day rate (it also did not trigger on the HM.5.1/HM.5.2A runs). **Zero failures beyond the documented baseline, by exact name — no new failure anywhere, including the 10K namespace.**

Total (4570) reconciles exactly against the HM.5.2A baseline (4567) plus this phase's own net +3 test-case delta: `NonPreferredCoreLengths_NowFeasible_...` theory gained 3 InlineData rows (12W/15W/16W, +3); `TwelveAndFifteenWeekHorizons_HitGenuineLongRunShareNumericSafetyBlocker` (2 cases) was replaced by `TwelveAndFifteenWeekHorizons_NoLongerHitLongRunShareNumericSafetyBlocker` (2 cases, net 0); `SixteenWeekHorizon_HitsGenuinePeakVolumeCeilingOvershootBlocker` (1 case) was replaced by `SixteenWeekHorizon_NoLongerOvershootsThePeakVolumeCeiling` (1 case, net 0). Net +3, matching 4567+3=4570 exactly.

**10K recurring-family before/after evidence (§17/§18, by exact test citation, not just aggregate pass count):**

- `Gen4DBeginnerFourDayCoreTests.MissingReadiness_AllHorizonsGenerate_WithFourDayCardinality` (13W: preTaper=22km, taper=11.5km; 14W: preTaper=23km, taper=12km) — this is the exact test that caught the initial unconditional-cap mistake mid-phase (§4/§17): it failed (21km actual) under the first draft, and passes unchanged (22/23km) under the final, flag-gated fix. Direct before/after proof for `BeginnerFourDay`.
- `Hm14bTaperVolumeMultiplierMechanismTests`'s parametrized single-taper-multiplier case (`ThreeDayIntermediate`/`FiveDayIntermediate`/`SixDayIntermediate`, asserting `TaperVolumeDecision.Multiplier=0.53`/`ReductionPercent=0.47`) — passes unchanged; the taper branch of `BuildWeeklyPlan` was not touched by either fix.
- `Hm16LongRunSharePeakWeekMechanismTests`'s parametrized RACE_SPECIFIC/verifier-parity case (same three policies, asserting `LongRunProgression.ValidationResult.IsValid` and `LongRunProgressionVerifier.Verify(...).Outcome == Pass`) — passes unchanged; these three policies all leave `PreferredAbsolutePeakLongRunKm` null and `ResolvedPeakReferenceIsSelectedCeiling` at its `false` default, so both HM.5.3 mechanisms are no-ops for them by construction, now also confirmed empirically.
- `Freq6D26IntermediateSixDayDarkVerificationTests`/`Gen9AdvancedCombinedDarkVerificationTests` (asserting `SixDayIntermediate`/`FiveDayIntermediate.ResolvedPeakReference.Value == 44.5`, `GoldenFixtureStartingVolumeKm == 26.0`) — unchanged, confirming no policy numeric field was edited for these five representative policies (Intermediate 4D/Beginner 3D/Intermediate 3D/5D/6D).

Combined with the full, unmodified `RuntimeCatalog` subtree (3660/3660) and `PlanCatalog.Tests` (1510/1510) both passing with zero failures, this is the required empirical zero-10K-delta proof — not just an aggregate count match, but the same named tests, with the same numeric assertions, passing before and after.

## 20. Public/Runway/LongHorizon state

`HalfMarathonIdentity_RemainsOutsideEveryPublicGate` — unmodified, passing. `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` both still return `false` for `HALF_MARATHON × INTERMEDIATE × 4D`. No Preparation-Runway or LongHorizon file was read for editing purposes (only referenced, in §11, to confirm `VolumeProgressionVerifier`/`LongRunProgressionVerifier` are not on any live request path). HM remains fully dark.

## 21. Remaining blockers

None known for the `HALF_MARATHON × INTERMEDIATE × 4D × Core 10–16W` family specifically. All seven horizons now materialize a fully valid dark preview end-to-end through the real, unmodified pipeline, with zero new numeric authority introduced and zero change to any frozen VolumeSafetyPolicy field, phase allocation, stage allocation, or taper multiplier.

## 22. HM.5 closure recommendation

With HM.4 (phase/stage numeric headroom), HM.5/HM.5.1 (10–16W catalog headroom), HM.5.2/HM.5.2A (stage-allocator optional/preferred and compression-priority semantics), and now HM.5.3 (volume/long-run boundary semantics) all closed, the `HALF_MARATHON × INTERMEDIATE × 4D` Core family is fully dark-materializable across its entire approved 10–16W range. HM.5 as a program-level umbrella can be marked closed for this candidate cell. HM remains dark (no public routing activated by this or any prior HM.5.x phase) — activating public HM routing is explicitly out of scope for every phase in this program and remains a distinct, future decision.
