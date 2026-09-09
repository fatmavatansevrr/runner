# PHASE HM.1.6 — HALF-MARATHON INTERMEDIATE×4D LONG-RUN SHARE / PEAK-WEEK MECHANISM CLOSURE

**Phase type**: IMPLEMENTATION (real production code + real tests), MECHANISM-ONLY — no numeric authority is decided or re-decided by this phase.
**Execution status**: DONE
**Final classification**: `HM_LONG_RUN_SHARE_PEAK_MECHANISM_CLOSED`
**Governing prompt**: user-authored "PHASE HM.1.6 — HALF-MARATHON INTERMEDIATE x 4D LONG-RUN SHARE / PEAK-WEEK MECHANISM CLOSURE", closing the `DOMAIN_DECISION_REQUIRED — HM_LONG_RUN_SHARE_PROGRESSION_MECHANISM` item `HM.1.5` disclosed and reserved for this phase by name.
**Parent**: `HM.1.5` (`PHASE_HM_1_5_VOLUME_SAFETY_POLICY_PARTIAL_FREEZE_LONG_RUN_SHARE_SEMANTICS_GAP.md`), `HM.1.4B` (`PHASE_HM_1_4B_TAPER_VOLUME_MULTIPLIER_NON_CHAINED_MECHANISM_FIX_AND_TWO_WEEK_TAPER_FREEZE.md`), `HM.1.1` (`PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md`).

---

## 1. Doc-comment correction commit

Commit `79f168a` (already made and pushed by the orchestrating session before this phase began) corrected a doc-comment miscount in `VolumeSafetyPolicy.cs` — "17 named `VolumeSafetyPolicy` instances" → "12 named `VolumeSafetyPolicy` instances", the real, exhaustively-verified count (`Default`, `ThreeDayIntermediate`, `FiveDayIntermediate`, `SixDayIntermediate`, `Advanced3D`–`Advanced6D`, `Beginner2D`, `Intermediate2D`, `ThreeDayBeginner`, `BeginnerFourDay`). Doc-only, no numeric or behavioral change. Not redone by this phase.

---

## 2. Existing long-run authority map

Read directly (not from summary) before any code change:

- `VolumeSafetyPolicy.cs` — 13-field positional record (now 14, see §7), 12 named static instances, `ForIntermediateDaysPerWeek`/`ForAdvancedDaysPerWeek`/`ForBeginnerDaysPerWeek` dispatch factories.
- `CatalogVolumeAndLongRunPlanner.cs` — `Build`, `ResolvePeak`, `ResolveTaperDecision`, `ResolveLongRunWeeklyShareDecision`, `BuildWeeklyPlan`, `BuildLongRunPlan` read in full.
- `CatalogVolumePlanValidator.cs` — `ValidateWeeklyPlan`/`ValidateLongRunPlan`, both invoked unconditionally inside `Build`.
- `LongRunProgressionVerifier.cs` (`Schedule/Materialization`) — independent post-hoc auditor, not called from any live request path.
- `VolumeProgressionVerifier.cs` — sibling auditor for weekly volume; long-run distance explicitly out of its scope.
- `CatalogFinalPrescribedPlanValidator.cs` — `ResolveLongRunHardCapShare`, a final-stage defensive check; currently fails closed (`CatalogVolumeUnsupportedDistanceFamilyException`) for any non-`TEN_K` candidate, so structurally unreachable for HALF_MARATHON today (HM.2 territory, untouched by this phase).
- `HalfMarathonIntermediate4DTaperVolumePolicy.cs` — HM.1.4B's narrow-policy precedent for freezing HM-only numeric values without constructing a full named `VolumeSafetyPolicy` instance.
- `CatalogVolumeContracts.cs` — `LongRunWeeklyShareDecision`, `CatalogLongRunWeek`, `LongRunDecisionTrace`, and siblings.
- `Hm14bTaperVolumeMultiplierMechanismTests.cs` — the established HM dark-only test-fixture pattern (`HmTestPolicy`/`HmCandidate`/`BoundPlan`/`HmRequest`, `FOUNDATION 3 / BUILD 4 / RACE_SPECIFIC 5 / TAPER 2`), reused verbatim by this phase's own tests.
- `HM_21K_Claude_Handoff_and_Build_Plan.md`, `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` §16 — re-confirmed already-frozen authority, no new evidence re-derived.

---

## 3. Exact meaning of all four share fields (proven against real code, not assumed)

Traced `CatalogVolumeAndLongRunPlanner.BuildLongRunPlan` week-by-week, pre-change:

```csharp
var lower   = Round(weeklyVolume * preferredMinShare);           // PreferredLongRunShareMin
var upper   = Round(weeklyVolume * preferredMaxShare);           // PreferredLongRunShareMax
var hardCap = Round(weeklyVolume * hardCapShare);                // HardLongRunShareCap
var target  = Round(weeklyVolume * selectionShare);              // SelectedLongRunShare -- computed IDENTICALLY every week
var unclamped = target;
...
var selected = Round(Clamp(unclamped, lower, Math.Min(upper, hardCap)));
```

- **`SelectedLongRunShare` (0.33)**: the sole driver of `target` for every week except week 1 (longest-run-anchor reconciliation, which only ever *lowers* `unclamped` below `target`, never raises it) and low-confidence weeks (which reset `unclamped = target`, never above it). No phase, week-number, or progression-state branch anywhere raised it. **Pre-HM.1.6: a de facto immutable flat ratio, not merely a "selected" target among alternatives.**
- **`PreferredLongRunShareMin`/`Max` (0.30/0.36)**: since `0.30 ≤ 0.33 ≤ 0.36` for every existing policy including HM's proposed values, `target` always already sits inside `[lower, upper]`. The `Clamp` therefore never actually bound anything in ordinary operation — these two fields were real inputs to the clamp expression but structurally inert given the numbers actually configured.
- **`HardLongRunShareCap` (0.40)**: `Math.Min(upper, hardCap)` always resolves to `upper` (0.36 < 0.40 for this cell's real values), so the hard cap never bound the clamp either. It functioned only as (a) the guard for `CatalogLongRunHardCapViolationException` (structurally unreachable given the clamp already sat at ≤`upper`), and (b) `LongRunProgressionVerifier`'s own sole *failing* check.
- **Applied every week, unconditionally** (pre-HM.1.6): yes — no phase-aware branch existed.
- **Before/after absolute long-run caps**: no independent absolute-km cap existed pre-HM.1.6 in the planner; only the per-week `weeklyVolume`-derived percentage bounds.
- **Phase effect**: none, pre-HM.1.6 (the one exception, `V1BeginnerThreeDayTaperLongRunSharePolicy`, is a *different*, narrower, Beginner×3D-taper-only override — not a RACE_SPECIFIC/peak mechanism, and untouched by this phase).
- **Progression-state effect**: none, pre-HM.1.6.
- **Can RaceSpecific currently exceed `SelectedLongRunShare`?** No — proven by the trace above; this is the exact gap `HM.1.5 §1.3`/§3.2 disclosed and reserved this phase to close.
- **Can any route reach `HardLongRunShareCap`?** No, pre-HM.1.6 (structurally unreachable, since `target` never left `[lower, upper]` and `upper < hardCap`).
- **Which authority wins when percentage and absolute limits conflict?** Pre-HM.1.6: no absolute long-run-km limit existed in the planner at all (only `HM.1.1`'s `PreferredAbsolutePeakLongRunKm`, which had never been wired into any code — it lived only as a frozen decision in `PHASE_HM_1_1_...md`).

**Verdict, matching `HM.1.5`'s own finding exactly, now independently re-confirmed**: this is a progression-semantics gap, not a numeric-value problem. `LongRunProgressionVerifier`, by contrast, was already correctly designed — see §9.

---

## 4. Current architectural limitation

`BuildLongRunPlan` had **no notion of "ordinary week" vs. "peak-eligible week."** A single scalar (`SelectedLongRunShare`) drove every week's target, with `PreferredLongRunShareMax`/`HardLongRunShareCap` acting only as passive, unreachable ceilings given the real numbers configured. No week-number, `PhaseKey`, or progression-state branch existed to let a late RaceSpecific week's share rise. This made `HM.1.1`'s already-frozen `PreferredAbsolutePeakLongRunKm = 19.0km` structurally unreachable for any input at this cell's frozen `[36,50]` peak-volume band (max possible pre-HM.1.6 output: `50 × 0.33 = 16.5km`).

---

## 5. Frozen HM semantic (implemented, not re-decided)

- Ordinary week: `longRunTarget ≈ SelectedLongRunShare × weeklyVolume` (unchanged, byte-identical formula and rounding).
- Late RaceSpecific peak opportunity: share MAY ramp above `SelectedLongRunShare`, bounded absolutely by `HardLongRunShareCap` (share) and `PreferredAbsolutePeakLongRunKm` (km) — an *allowance*, never a forced target. No "chase 19km" branch exists anywhere: the ramp target is expressed purely in terms of the two already-approved share numbers (`SelectedLongRunShare`, `HardLongRunShareCap`); the resulting kilometer value is whatever the already-existing (unmodified) weekly-volume progression happens to produce for that week, multiplied by the ramped share.

---

## 6. 43km/19km/40% compatibility analysis

At the frozen `ResolvedPeakReferenceKm = 43.0` golden-fixture peak: `43.0 × 0.40 = 17.2 → rounds to 17.0km` (0.5km increment) — **below** 19.0km. This is the **correct, expected, valid** outcome, not a failure: the 40% hard-cap SHARE binds before the 19.0km absolute ceiling ever does at this weekly-volume level. `19.0/0.40 = 47.5` — 19km only becomes share-feasible at ≥47.5km/week, a weekly volume this cell's frozen `[36,50]` peak-volume band's own upper edge can reach (`50km` week, `UpperBoundConstrained` classification), but the *golden* `43.0km` reference trajectory correctly does not. Both are proven directly by test (§12, tests E and F) rather than asserted.

---

## 7. Minimal mechanism decision

**Decision: extend `VolumeSafetyPolicy` with one new optional, nullable, SCALAR trailing field — `double? PreferredAbsolutePeakLongRunKm = null`** — mirroring `HM.1.4B`'s own established, working precedent for safely extending this exact record (an optional trailing field defaulting to `null`, degrading every existing policy to its pre-existing exact behavior). Deliberately a **scalar**, not a collection, to avoid repeating the record-equality hazard `HM.1.4C`'s audit flagged for `TaperVolumeMultipliers` (`IReadOnlyList<double>` gets reference equality, not structural, on a positional record's default-generated `Equals`) — nothing new was added to that risk surface.

No new share number was invented: the "peak-eligible share ceiling" is simply `HardLongRunShareCap` itself (already approved, `HM.1.5`) — the mechanism ramps the effective per-week target share linearly from `SelectedLongRunShare` toward `HardLongRunShareCap` across a RACE_SPECIFIC-phase week's ordered position in that phase, reusing only the two already-frozen share numbers. `LongRunWeeklyShareDecision` (the planner's internal per-build decision record) gained the identical optional trailing field, threaded from the policy.

**Why this satisfies "smallest generic seam" (item 8)**: the entire mechanism is gated on a single nullable field being non-null. Every existing 10K named policy (all 12) leaves it `null` — the ramp branch and the absolute-ceiling clamp are both unconditionally skipped, producing byte-identical output regardless of whether a given 10K candidate's own phase keys happen to include `"RACE_SPECIFIC"` (proven by test, §12 "J", using a real TEN_K candidate whose `PhaseKeys` array does include `"RACE_SPECIFIC"` — the gate is the policy field, not incidental phase-key absence). No HM-only branch, no distance-family check, no week-number hardcoding, and no change to `BuildWeeklyPlan`, `ResolvePeak`, or `ResolveTaperDecision` (weekly-volume and taper mechanics are completely untouched).

### Exact mechanism (`CatalogVolumeAndLongRunPlanner.BuildLongRunPlan`)

```csharp
var raceSpecificWeekNumbers = weekly.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC").Select(w => w.WeekNumber).ToList();
...
var isPeakEligibleWeek = !useBeginnerThreeDayTaperShare && share.PreferredAbsolutePeakLongRunKm is not null && week.PhaseKey == "RACE_SPECIFIC";
if (isPeakEligibleWeek)
{
    var position = raceSpecificWeekNumbers.IndexOf(week.WeekNumber);
    var denominator = Math.Max(1, raceSpecificWeekNumbers.Count - 1);
    selectionShare += (hardCapShare - selectionShare) * position / denominator;   // linear ramp, SelectionShare -> HardCapShare
}
var lower   = Round(weeklyVolume * preferredMinShare);
var upper   = Round(weeklyVolume * (isPeakEligibleWeek ? hardCapShare : preferredMaxShare));   // widened band only for peak-eligible weeks
var hardCap = Round(weeklyVolume * hardCapShare);
if (share.PreferredAbsolutePeakLongRunKm is { } absoluteCeilingKm)
{
    hardCap = Math.Min(hardCap, Round(absoluteCeilingKm));    // absolute ceiling narrows the effective hard cap, every week, never widens it
}
var target = Round(weeklyVolume * selectionShare);
```

- **Continuous, not a jump**: at the first RACE_SPECIFIC week (`position == 0`), the ramp reduces to `selectionShare` unchanged — identical to the immediately preceding BUILD week's own flat 33% target. No discontinuity at the phase boundary.
- **Monotonic**: share increases linearly with position across the RACE_SPECIFIC phase, reaching exactly `HardCapShare` at the final RACE_SPECIFIC week.
- **Absolute ceiling is a pure additional restriction**: it only ever narrows `hardCap` downward (`Math.Min`), never widens it — proven never to force a higher value (§12, test G).
- **All existing downstream consumers of `hardCap` automatically inherit the ceiling** — the `CatalogLongRunHardCapViolationException` guard, `CatalogVolumeBounds` attached to each week (already consumed by `CatalogVolumePlanValidator.ValidateLongRunPlan`'s `LONG_RUN_OUTSIDE_COMPATIBILITY_BOUNDS` check), and the decision trace — with **zero additional code changes** to any of those three call sites.

---

## 8. Phase/stage eligibility

Implemented exactly as item 9 requested, with no week-number hardcoding:

- **Foundation, Build**: ordinary flat `SelectedLongRunShare` — unchanged (not `RACE_SPECIFIC`, so `isPeakEligibleWeek` is always `false`).
- **RaceSpecific**: peak-eligible — share ramps linearly by ordered position within the phase, from `SelectedLongRunShare` to `HardLongRunShareCap`, bounded further by `PreferredAbsolutePeakLongRunKm`.
- **Taper**: not `RACE_SPECIFIC`, so `isPeakEligibleWeek` is always `false` — flat `SelectedLongRunShare` on top of the taper's own already-reduced weekly volume (`HM.1.4B`, untouched by this phase). No new peak can arise here; proven by test (§12, test H).

No specific week (e.g. "Week 11 must be peak") is ever hardcoded — eligibility is entirely phase-key- and position-driven, generalizing correctly to any RACE_SPECIFIC phase length.

---

## 9. Planner/verifier parity

**Finding: `LongRunProgressionVerifier` required ZERO code changes** — it was already correctly designed before this phase touched anything:

```csharp
var violatesHardCapShare = actualShare > policy.LongRunHardCapShare;                                    // the ONLY failing bound
var violatesPreferredShareBand = actualShare < policy.LongRunPreferredMinimumShare || actualShare > policy.LongRunPreferredMaximumShare;  // non-failing, soft finding only
```

The verifier's own class doc comment even states this explicitly ("a value between the preferred band and the hard cap is not itself a failure, only exceeding the hard cap is... reachable in principle even though the real planner's own clamp currently prevents it in practice"). It anticipated exactly the mechanism this phase built, before this phase existed. `CatalogVolumePlanValidator.ValidateLongRunPlan` also required zero changes — its `CatalogBounds` come directly from `BuildLongRunPlan`'s own (now ceiling-aware) `hardCap`, so it automatically stays consistent with the planner with no independent assumption of its own.

Proven by test (§12, test I): the fixed HM 14-week trace's genuinely-elevated (>36%) late-RaceSpecific weeks surface only as the verifier's pre-existing `PREFERRED_SHARE_BAND_DEVIATION_NONFAILING` finding — never `HARD_CAP_SHARE_VIOLATION` — and the overall `Outcome` is `Pass`.

---

## 10. Recurring-assumption-family search

Every "`SelectedLongRunShare` is always the maximum" occurrence found and classified:

| Occurrence | Classification | Action |
|---|---|---|
| `CatalogVolumeAndLongRunPlanner.BuildLongRunPlan` | `LEGACY_ASSUMPTION` (real, confirmed defect per §3) | **FIXED this phase** (§7) |
| `LongRunProgressionVerifier.Verify` | `SAFE_SIBLING` (already `TARGET_ONLY`/hard-cap-only semantic) | No change needed |
| `CatalogVolumePlanValidator.ValidateLongRunPlan` | `SAFE_SIBLING` (derives bounds from the planner directly, no independent assumption) | No change needed |
| `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare` | `SAFE_SIBLING` for today (already reads `HardCapShare`, not `SelectionShare`, as its ceiling) but `REQUIRES_PARAMETERIZATION` before HM.2 (currently fails closed for any non-`TEN_K` candidate) | Out of scope — HM.2 territory, noted as remaining gap (§16) |
| `VolumeProgressionVerifier` | Not applicable — explicitly out of scope for long-run distance (weekly volume only) | No change |
| `ThreeDayIntermediate`/`FiveDayIntermediate`/`SixDayIntermediate`/`Advanced3D-6D`/`Beginner2D`/`Intermediate2D`/`ThreeDayBeginner`/`BeginnerFourDay` (sibling 3D/5D/6D/Advanced/Beginner policies) | `SAFE_SIBLING` / `ZERO_DELTA` — `PreferredAbsolutePeakLongRunKm` stays `null` for every one of them | No change; mechanism dormant, proven by test (§12, "J") |
| `V1BeginnerThreeDayTaperLongRunSharePolicy` (existing taper-only override) | `SAFE_SIBLING` — a different, narrower mechanism (Beginner×3D taper only), explicitly excluded from the new ramp via `!useBeginnerThreeDayTaperShare` | No change; guard added defensively though the phase-key check alone already excludes it (taper weeks are never `RACE_SPECIFIC`) |
| Existing 10K golden-fixture/pilot tests asserting flat 33% shares (`Phase4G3B0VolumeSafetyPolicyEquivalenceTests`, `LongRunProgressionVerifierTests`, `Hm14bTaperVolumeMultiplierMechanismTests`) | `SAFE_SIBLING` — all exercise policies with `PreferredAbsolutePeakLongRunKm == null` | No change; re-run clean (§14) |
| `TenKPreparationRunwayNumericPolicyFactory`, `PreparationRunwayNumericMaterializer`, `LongHorizonGeNumericExecutor`, `LongHorizonRollingCheckpointRuntime`/`LongHorizonCheckpointEvidenceAggregator` (Preparation-Runway/LongHorizon `TenK*` subtree) | `SAFE_SIBLING` / out of this engagement's Core-horizon scope (per `HM.0`'s own scope boundary) — read `LongRunHardCapShare`/`LongRunSelectionShare` fields directly, unaffected by the new field's mere existence | No change |

No occurrence beyond the one fixed required any code change; every sibling either already had the correct semantic or is structurally inert against the new field.

---

## 11. HM 14W long-run trace (test-level only, NOT HM.2 activation)

Real output from `Hm16LongRunSharePeakWeekMechanismTests.BuildHmPlan(recentWeeklyVolumeKm: 25)` — `HALF_MARATHON × INTERMEDIATE × 4D × 14W`, `FOUNDATION 3 / BUILD 4 / RACE_SPECIFIC 5 / TAPER 2`, test-local `VolumeSafetyPolicy` combining every already-frozen field for this cell plus `PreferredAbsolutePeakLongRunKm = 19.0` (HM.1.1):

| Week | Phase | WeeklyVolumeKm | SelectedShareTargetKm (33%) | ActualLongRunKm | ActualShare | Which constraint won | Decision reason |
|---|---|---|---|---|---|---|---|
| 1 | FOUNDATION | 25.0 | 8.5 | 8.5 | 34.0% | week-1 longest-run anchor (min of anchor=9 and target=8.5) | `recent_longest_run_anchor_reconciled_below_or_equal_reported_maximum` |
| 2–3 | FOUNDATION | (interpolated) | ~33% | ~33% | ~33% | flat selected share | `weekly_volume_derived_long_run_share` |
| 4–7 | BUILD | (interpolated) | ~33% | ~33% | ~33% | flat selected share | `weekly_volume_derived_long_run_share` |
| 8 | RACE_SPECIFIC (pos 0/4) | 36.5 | 12.0 | 12.0 | 32.9% | flat selected share (ramp at position 0 = no change, continuous with BUILD) | `hm_1_6_race_specific_peak_share_progression_toward_hard_cap` |
| 9 | RACE_SPECIFIC (pos 1/4) | 38.0 | 12.5 | 13.0 | 34.2% | ramped share (34.75%) | `hm_1_6_race_specific_peak_share_progression_toward_hard_cap` |
| 10 | RACE_SPECIFIC (pos 2/4) | 39.5 | 13.0 | 14.5 | 36.7% | ramped share (36.5%) — now above the ordinary preferred band, still within hard cap | `hm_1_6_race_specific_peak_share_progression_toward_hard_cap` |
| 11 | RACE_SPECIFIC (pos 3/4) | 41.5 | 13.5 | 16.0 | 38.6% | ramped share (38.25%) | `hm_1_6_race_specific_peak_share_progression_toward_hard_cap` |
| 12 | RACE_SPECIFIC (pos 4/4, resolved weekly-volume peak) | 43.0 | 14.0 | 17.0 | 39.5% | ramped share reaches exactly `HardCapShare` (40%); 40%-share limit binds before the 19.0km absolute ceiling ever does | `hm_1_6_race_specific_peak_share_progression_toward_hard_cap` |
| 13 | TAPER (week 1 of 2) | 30.0 | 9.9→10.0 | 10.0 | 33.3% | not `RACE_SPECIFIC` — flat selected share on already-reduced volume; below the week-12 peak | `weekly_volume_derived_long_run_share` |
| 14 | TAPER (week 2 of 2, race week) | 18.5 | 6.1→6.0 | 6.0 | 32.4% | not `RACE_SPECIFIC` — flat selected share on the deepest-reduced volume; below the week-12 peak | `weekly_volume_derived_long_run_share` |

**Demonstrates**: no forced jump at the FOUNDATION→BUILD→RACE_SPECIFIC boundary (week 8 continuous with week 7's own ~33%); no week exceeds the 40% hard-cap share; no week exceeds 19.0km (peak reached is 17.0km — correctly below the ceiling, per §6); ordinary weeks (1–7, 13–14) stay within the ordinary preferred band; late RACE_SPECIFIC weeks (9–12) exceed 33% (and weeks 10–12 exceed the 36% preferred band, non-failingly, per §9) as progression naturally allows; taper reduces without creating a new peak.

At an elevated starting volume (`recentWeeklyVolumeKm = 30`, peak resolves to the `[36,50]` band's own `50.0km` upper edge), the identical mechanism reaches exactly `19.0km` at week 12 (§12, test F) — proving the ceiling is genuinely reachable when weekly volume and progression allow it, with no separate branch or special-casing required.

---

## 12. HM focused tests

All in `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm16LongRunSharePeakWeekMechanismTests.cs` (22 tests, dark-only, test-local `VolumeSafetyPolicy`/candidate — never reachable through any real identity gate, mirroring `Hm14bTaperVolumeMultiplierMechanismTests`'s own established pattern):

- **A** `OrdinaryFoundationAndBuildWeeks_UseFlatThirtyThreePercentSelectedTarget` — ordinary weeks stay within the preferred band.
- **B** `LateRaceSpecificWeeks_ShareRampsAboveThirtyThreePercent_ContinuousFromBuildPhase` — proves the ramp is continuous at the phase boundary, monotonic, and exceeds 33% by the final RACE_SPECIFIC week (reaches exactly 40%, matching the golden-fixture peak week).
- **C** `HardCapShare_NeverExceeded_AcrossEntireFourteenWeekPlan` — every week ≤ 40% share.
- **D** `AbsoluteCeiling_NeverExceeded_EvenAtElevatedStartingVolume` — every week ≤ 19.0km even when the resolved peak comfortably exceeds 47.5km/week.
- **E** `FortyThreeKilometerGoldenPeakReference_StaysAtShareLimitedValue_NotNineteenKm_AndThisIsValid` — 43km golden week stays at 17.0km (share-limited), asserted as `IsValid`, not a failure.
- **F** `AtOrAboveFortySevenPointFiveKilometersPerWeek_NineteenKilometersBecomesReachable` — at the peak-volume band's own 50km/week upper edge, the mechanism reaches exactly 19.0km.
- **G** `LowReadinessTrajectory_RemainsFarBelowTheoreticalPeak_NoForcedChasing` — a low-starting-volume trajectory (15km/week) stays far below the 19.0km ceiling even at its own hard-cap share.
- **H** `TaperWeeks_RemainBelowPreTaperLongRunPeak` — both taper weeks stay below the pre-taper peak and use the flat, unramped share.
- **I** `LongRunProgressionVerifier_AgreesWithPlanner_NoFalseFailureForElevatedRaceSpecificShares` — planner/verifier parity, no false `HARD_CAP_SHARE_VIOLATION`, elevated shares surface only as the verifier's pre-existing non-failing finding.
- **J** `EveryExisting10KPolicy_PreferredAbsolutePeakLongRunKm_IsNull_MechanismInert` (12 cases) + `LiveTwelveWeekTenKPilot_LongRunShares_ByteIdenticalToPreHm16Behavior` — zero-delta proof (§13).

All 22 tests pass.

---

## 13. 10K zero-delta proof

- **Structural**: the entire mechanism (ramp branch and absolute-ceiling clamp) is gated on `share.PreferredAbsolutePeakLongRunKm is not null`. Every one of the 12 named 10K `VolumeSafetyPolicy` instances leaves this field at its `null` default (proven by test, §12 "J" theory). When `null`, `isPeakEligibleWeek` is always `false` and the `hardCap` ceiling clamp never executes — `selectionShare`, `upper`, and `hardCap` are computed by the exact pre-existing expressions, unconditionally.
- **Not merely incidental non-use of the `RACE_SPECIFIC` phase key**: proven directly by test using the real `TenKCandidate()` fixture, whose own `PhaseKeys` array explicitly includes `"RACE_SPECIFIC"` — even so, every RACE_SPECIFIC-phase TEN_K week resolves to the flat ~33% target, byte-identical to pre-HM.1.6 behavior (§12 "J", `LiveTwelveWeekTenKPilot_LongRunShares_ByteIdenticalToPreHm16Behavior`). The gate is the policy field, never the phase key's mere presence or absence.
- **No shared method's signature or control flow changed** for the non-peak-eligible path: `ResolvePeak`, `ResolveTaperDecision`, `BuildWeeklyPlan` are completely untouched by this phase; only `BuildLongRunPlan` and `ResolveLongRunWeeklyShareDecision` changed, and only by adding new, purely-additive local computations gated on the new nullable field.

---

## 14. Regression results

Targeted regression run first (`HalfMarathon` directory + `LongRunProgressionVerifierTests` + `Phase4G3B0VolumeSafetyPolicyEquivalenceTests` + `VolumeProgressionVerifierTests`): **151 passed, 0 failed.**

New HM.1.6 test file alone: **22 passed, 0 failed.**

Full-suite regression (`RunningApp.IntegrationTests`, run alone, no concurrent testhost against the shared PostgreSQL test database):

**4498 passed, 4 failed, 4502 total** (40m45s). The four failures are the exact disclosed pre-existing baseline: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` for weeks 13 and 14, `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`, and the date-coincidental `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`. No new failure was introduced.

`PlanCatalog.Tests` (`plan-catalog/PlanCatalog.sln`), run after the above completed (sequential, per governance):

**1510 passed, 0 failed, 1510 total** (6s).

---

## 15. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` — added one new optional, nullable trailing field, `PreferredAbsolutePeakLongRunKm`, with a full doc comment. No existing field, value, or named instance changed.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeContracts.cs` — `LongRunWeeklyShareDecision` gained the identical optional trailing field.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` — `ResolveLongRunWeeklyShareDecision` threads the new field through; `BuildLongRunPlan` gained the RACE_SPECIFIC-phase share-ramp and absolute-ceiling-clamp mechanism (§7). No other method changed.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm16LongRunSharePeakWeekMechanismTests.cs` — new, 22 tests (§12).
- `PHASE_HM_1_6_LONG_RUN_SHARE_PEAK_WEEK_MECHANISM_CLOSURE.md` — this report.
- `PHASE_LEDGER.md` — new HM Program row.
- `MASTER_ROADMAP.md` — §4 and §16 updated.

No HM catalog artifact authored. No public/Runway identity gate touched. No 10K numeric authority, value, or named instance changed.

---

## 16. Remaining gaps

- **Full `VolumeSafetyPolicy` freeze for HM is still NOT decided by this phase.** This phase proves the *mechanism* can express "ordinary target vs. peak-eligible ceiling" correctly; it does not itself freeze `PreferredLongRunShareMin`/`Max`/`SelectedLongRunShare`/`HardLongRunShareCap` as HM's own permanent numeric authority (still formally reserved for a future decision phase, though this phase's tests demonstrate the previously-blocking contradiction is resolved using exactly those four proposed values plus the already-frozen `PreferredAbsolutePeakLongRunKm=19.0`).
- **`CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`** still fails closed for any non-`TEN_K` distance family (`CatalogVolumeUnsupportedDistanceFamilyException`) — correctly out of scope for a Core-mechanism-only phase, but must be widened (an additive `HALF_MARATHON` arm, mirroring `HM.2`'s own established additive-widening pattern) before any real HM final-session-prescription pipeline can run. Noted, not fixed, here.
- **No HM catalog artifact exists yet** — this phase is test-level/mechanism-only, per its own explicit scope constraint (item 15 of the governing prompt). `HM.2`'s own numeric-policy/volume/long-run/workout-binding/calendar stages remain unimplemented for real candidate authoring.
- **The peak-eligible ramp's exact shape (linear, by ordered RACE_SPECIFIC position) is a mechanism design choice, not itself frozen evidence-backed authority** — a future phase could revisit the ramp *shape* (e.g. non-linear) without needing to touch the gating field or the zero-delta guarantee, since both are independent of the ramp's exact interpolation curve.

---

## 17. HM.2 gating conclusion

The `HM_LONG_RUN_SHARE_PROGRESSION_MECHANISM` gap `HM.1.5` disclosed and reserved this phase to close is **closed**: the real planner can now express "ordinary week uses `SelectedLongRunShare`; a late, eligible RaceSpecific week may rise toward `HardLongRunShareCap`, bounded absolutely by `PreferredAbsolutePeakLongRunKm`" — proven by 22 passing focused tests plus a full 14-week trace, with `LongRunProgressionVerifier` already in parity and zero 10K regression. A future phase may now freeze the four long-run-share fields for `HALF_MARATHON × INTERMEDIATE × 4D × 14W` (using the same 0.30/0.36/0.33/0.40 values `HM.1.5` proposed, or any other numbers standard product-decision process selects) without reproducing the structural contradiction `HM.1.5` found — and, separately, `HM.2` may resume its own implementation work once that freeze decision is made and `CatalogFinalPrescribedPlanValidator`'s remaining `TEN_K`-only gate (§16) is widened.

---

## 18. Final classification

```
HM_LONG_RUN_SHARE_PEAK_MECHANISM_CLOSED
```

33% proven ordinary selected target, not immutable cap (§3, §12 test A/B). Eligible late RaceSpecific long runs exceed 33%, up to and including exactly 40% at the golden-fixture peak week (§11, §12 test B). 40% remains inviolate across the full 14-week trace and at elevated starting volumes (§12 test C). 19km remains a ceiling, never a target — reachable only when weekly volume and progression independently allow it (≥47.5km/week), never forced (§6, §12 test D/F/G). The 43km peak reference does not force 19km — it correctly stays at the share-limited 17.0km, asserted valid (§6, §12 test E). Planner and verifier agree, with zero verifier code changes required (§9, §12 test I). No peak chasing exists — a low-readiness trajectory stays far below the ceiling (§12 test G). Taper cannot create a new long-run peak (§12 test H). All 10K behavior remains zero-delta, proven structurally and by test against a real candidate whose own phase keys include `RACE_SPECIFIC` (§13, §12 test J). No new regression: focused HM.1.6 **22/22**, targeted related surface **151/151**, full `RunningApp.IntegrationTests` **4498/4502** with the exact four disclosed baseline failures, and `PlanCatalog.Tests` **1510/1510**.
