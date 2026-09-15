# PHASE HM-X1.2 — HALF-MARATHON 6D INTERMEDIATE/ADVANCED STRUCTURAL AND NUMERIC AUTHORITY CLOSURE

**Type:** GOVERNANCE / PRODUCT-DECISION (no production implementation, no catalog authoring, no public activation)

---

## 1. HM-X1.1 eligibility consumed

`PHASE_HM_X1_1_...md` closed `HM_INTERMEDIATE_6D_PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE` and `HM_ADVANCED_6D_PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE`. Taken as binding: 6D is product-eligible for both levels, the architecture can represent it, `RUN_LAYOUT_6D` is valid, and session allocation/calendar/persistence are fundamentally capable. None of this is reopened. This phase freezes only the remaining numeric/structural authority HM-X1.1 explicitly deferred.

## 2. Frozen RUN_LAYOUT_6D

Reused exactly as HM-X1.1 found it: `RUN_LAYOUT_6D` = 2×`KEY_SESSION`, 3×`EASY_SUPPORT`, 1×`LONG_RUN`. Exactly one `EASY_SUPPORT` slot more than `RUN_LAYOUT_5D`. No third KEY, no second LONG, no new role. Not reopened.

## 3. Intermediate structural authority

| Phase | KEY1 semantic | KEY1 hardness | KEY2 semantic | KEY2 preferred family | KEY2 fallback | KEY2 hardness | LONG hardness |
|---|---|---|---|---|---|---|---|
| Foundation | Aerobic base (`FOUNDATION_AEROBIC_BASE`) | Easy | Easy-equivalent (`FOUNDATION_HM_SECONDARY_EASY`) | `EASY_STANDARD` v4 | none needed | Easy | Easy/moderate long |
| Build | Progresses `FARTLEK`→`THRESHOLD_TEMPO` (`BUILD_FASTER_THAN_HM_INTRO`→`BUILD_THRESHOLD_DEVELOPMENT`) | Moderate→true-hard | Light-quality (`BUILD_HM_SECONDARY_FARTLEK`) | `FARTLEK` v6 | `EASY_STANDARD` v4 (`BUILD_HM_SECONDARY_EASY_FALLBACK`) | Moderate (SURGE_AND_FLOAT, not THRESHOLD) | Steady/progressive |
| RaceSpecific | `THRESHOLD_TEMPO`→`HM_PACE` (`RACE_SPECIFIC_HM_INTRODUCTION`→`_DEVELOPMENT`→`_REHEARSAL`, fallback `THRESHOLD_TEMPO`) | True-hard/race-specific | Light-quality (`RACE_SPECIFIC_HM_SECONDARY_FARTLEK`) | `FARTLEK` v6 | `EASY_STANDARD` v4 | Moderate | HM-pace-containing or steady |
| Taper | `HM_PACE` (fallback `EASY_STANDARD`) | True-hard/race-specific (reduced volume) | Easy-equivalent (`TAPER_HM_SECONDARY_EASY`) | `EASY_STANDARD` v4 | none needed | Easy | Reduced |

**Frozen decision**: Intermediate 6D reuses this exact table, unmodified, plus the one added `EASY_SUPPORT` session (no phase-specific content of its own — it is fixed-default-bound, generic across all phases, per HM-X1.1's binder finding). This is **not silently inherited** — it is formally frozen here as authority: **`INTERMEDIATE_6D_STRUCTURAL_AUTHORITY = INTERMEDIATE_5D_STRUCTURAL_AUTHORITY (KEY1/KEY2 semantics) + one additional generic EASY_SUPPORT session`**. Evidence: HM-X1.1's own structural finding that 6D's only delta is the added EASY slot, with no new lane and no new role; no evidence anywhere proposes any KEY1/KEY2 content change at 6D. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 4. Advanced structural authority

| Phase | KEY1 semantic | KEY1 hardness | KEY2 semantic | KEY2 preferred family | KEY2 fallback | KEY2 hardness | LONG hardness |
|---|---|---|---|---|---|---|---|
| Foundation | Aerobic base | Easy | Easy-equivalent (`FOUNDATION_HM_SECONDARY_EASY`, identical content to Intermediate's) | `EASY_STANDARD` v4 | none needed | Easy | Easy/moderate long |
| Build | `FARTLEK`→`THRESHOLD_TEMPO` | Moderate→true-hard | Genuine true-hard (`BUILD_HM_SECONDARY_THRESHOLD_ADVANCED`) | `THRESHOLD_TEMPO` v4 | `EASY_STANDARD` v4 (`BUILD_HM_SECONDARY_THRESHOLD_ADVANCED_EASY_FALLBACK`) | True-hard (THRESHOLD) | Steady/progressive |
| RaceSpecific | `THRESHOLD_TEMPO`→`HM_PACE` | True-hard/race-specific | Genuine true-hard (`RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED`) | `THRESHOLD_TEMPO` v4 | `EASY_STANDARD` v4 | True-hard | HM-pace-containing or steady |
| Taper | `HM_PACE` (fallback `EASY_STANDARD`) | True-hard/race-specific (reduced) | Easy-equivalent (`TAPER_HM_SECONDARY_EASY`, identical content to Intermediate's) | `EASY_STANDARD` v4 | none needed | Easy | Reduced |

**Frozen decision**: `ADVANCED_6D_STRUCTURAL_AUTHORITY = ADVANCED_5D_STRUCTURAL_AUTHORITY (KEY1/KEY2 semantics, 2 true-hard sessions in Build/RaceSpecific) + one additional generic EASY_SUPPORT session`. **No third hard stimulus is introduced** — 6D's layout still has exactly 2 KEY slots (§2), and no evidence anywhere proposes escalating the count. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 5. KEY2 formalization

| | Intermediate 6D | Advanced 6D |
|---|---|---|
| Preferred family (Build/RaceSpecific) | `FARTLEK` v6 | `THRESHOLD_TEMPO` v4 |
| Fallback | `EASY_STANDARD` v4 | `EASY_STANDARD` v4 |
| Hardness | Moderate (SURGE_AND_FLOAT) — never true-hard | True-hard (THRESHOLD) |
| Foundation/Taper | `EASY_STANDARD` v4 (identical between levels) | `EASY_STANDARD` v4 (identical between levels) |

Frozen exactly as the existing HM 5D authority (§3-§4). No new workout family is introduced — `FARTLEK` v6 and `THRESHOLD_TEMPO` v4 are both already-eligible, already-live catalog content. 10K's own 6D precedent is used only as structural/reuse evidence (confirming the 2-lane binder mechanism scales cleanly), not as workout-family authority — the actual family choice comes from HM's own direct 5D precedent, per this phase's own instruction.

## 6. HM_PACE secondary-lane rule

**Frozen**: `KEY2_HM_PACE_ELIGIBILITY = PROHIBITED`, for both Intermediate and Advanced 6D. Reasoning: HM.15 §7-8 froze this exact prohibition for Advanced 5D specifically "to avoid duplicating KEY1's own race-pace-specific rehearsal" — a rationale about avoiding *redundant* race-specific stimulus within one week, which is completely orthogonal to session count. 6D adds only an `EASY_SUPPORT` session; it changes nothing about KEY1's own race-pace rehearsal role or the risk of redundancy KEY2-HM_PACE would create. No evidence anywhere suggests 6D changes this. This decision is formally re-frozen here, not silently inherited. Classification: `DIRECT_EXISTING_AUTHORITY` (HM.15 §7-8's rationale is frequency-independent by its own terms and directly re-verified against 6D's actual structural delta).

## 7. Primary progression reuse

```
PRIMARY_PROGRESSION_REUSE_APPROVED
```

No `HM_6D_WORKOUT_PROGRESSION` document is created. `HALF_MARATHON_WORKOUT_PROGRESSION` v2 (Intermediate)/v3 (Advanced) are reused byte-for-byte; the lane-binder's "structural ordinal N binds to laneOrdinal N" rule (layout-driven, not frequency-driven, per HM-X1.1) means these existing files serve 6D with zero modification — mirroring 10K's own finding that no `_6D` progression variant was ever needed for either of its levels. No authority contradiction was found.

## 8. Internal numeric evidence inventory

Directly re-verified via source read of `VolumeSafetyPolicy.cs` (not trusted from memory):

| Policy | AbsWeeklyCapKm | CalibStartKm | PeakVolumeBand | ResolvedPeak | LR shares (min/max/sel/hardcap) | AbsPeakLR | StartLRCap | Taper |
|---|---|---|---|---|---|---|---|---|
| HM Intermediate 3D | 2.0 | 20.0 | [28,40] | 34.0 | 0.34/0.40/0.38/0.46 | 19.0 | n/a documented, unenforced | 0.70/0.43 |
| HM Intermediate 4D | 2.5 | 25.0 | [36,50] | 43.0 | 0.30/0.36/0.33/0.40 | 19.0 | n/a documented, unenforced | 0.70/0.43 |
| HM Intermediate 5D | 2.5 | 29.5 | [44,58] | 51.0 | 0.28/0.36/0.28/0.36 | 19.0 | 12.0 | 0.70/0.43 |
| HM Advanced 3D | 2.5 | 24.5 | [36,48] | 42.0 | 0.34/0.40/0.38/0.46 | 19.0 | reused from Int3D | 0.70/0.43 |
| HM Advanced 4D | 2.5 | 31.0 | [46,60] | 53.0 | 0.30/0.36/0.33/0.40 | 19.0 | N/A (not applicable) | 0.70/0.43 |
| HM Advanced 5D | 2.5 | 36.0 | [54,70] | 62.0 | 0.28/0.36/0.28/0.36 | 19.0 | N/A (not applicable) | 0.70/0.43 |
| 10K Intermediate 5D | 2.5 | 26.0 | [36,50] | 44.5 | 0.28/0.36/0.28/0.36 | n/a (10K) | n/a | 0.53 (single) |
| 10K Intermediate 6D | 2.5 | 26.0 | **[36,50] — unchanged** | **44.5 — unchanged** | 0.28/0.36/0.28/0.36 — unchanged | n/a | n/a | 0.53 |
| 10K Advanced 5D | 2.5 | 29.0 | [42,58] | 50.0 | 0.28/0.36/0.28/0.36 | n/a | n/a | 0.53 |
| 10K Advanced 6D | 2.5 | 30.0 | **[42,60] — max +2** | **51.0 — +1.0 (+2%)** | 0.28/0.36/0.28/0.36 — unchanged | n/a | n/a | 0.53 |

(*) HM Advanced 3D/4D exact figures cross-checked against HM.15's own report table in prior phases; cited here for directional pattern only, not re-derived.

**Decisive finding**: 10K's own real, governance-approved 5D→6D transition, at both levels, on the exact same product, is the strongest available directional evidence for the magnitude of change a 6D expansion should carry: **Intermediate — zero change** to band, peak, or LR shares; **Advanced — lower bound unchanged, upper bound +2km, ~+2% peak increase**, LR shares unchanged. HM's own 4D→5D transition (used only as secondary, lower-priority evidence per the mandated hierarchy) reflects a *different* kind of change — the introduction of KEY2 itself, a genuine new hard-stimulus opportunity — and is therefore not the right analog for 6D's much smaller structural delta (one easy session, no new hard stimulus).

## 9. External research

Per HM-X1.1's confirmed `SOURCE_SILENCE` on direct HM 6D volume authority, narrow external research was permitted and performed as a secondary corroboration check (the internal evidence in §8 was already sufficient and higher in the mandated hierarchy). Search results (Hal Higdon's Advanced Half Marathon program: "6 runs and 1 day off... very experienced runners... capable of running 30 to 60 minutes a day, five to seven days a week") confirm that established 6-day Advanced HM plans exist and target a meaningfully higher but not dramatically higher volume ceiling than lower-frequency Advanced plans — broadly consistent with (not contradicting) a modest upward band shift, though the time-based description doesn't convert to a precise km figure. No source gave a clean, convergent km/week band for 6-day HM plans specifically (public plans concentrate on 3-5 day frequencies), so this corroboration is directional only and does not override the internal, on-point 10K precedent used as primary evidence.

## 10. Intermediate peak-volume band

**Frozen: `PeakVolumeBandMin = 44 km`, `PeakVolumeBandMax = 58 km`** — unchanged from HM Intermediate 5D's own `[44,58]` band.

**Methodology and defensibility**: band-width-stability, directly mirroring 10K's own real, approved decision that the identical 5D→6D transition (adding one `EASY_SUPPORT` session, zero new hard stimulus) at Intermediate level warrants **zero** peak-volume-band change — confirmed by 10K's own `SixDayIntermediate` being byte-identical to `FiveDayIntermediate` on every volume field, including the catalog `PeakVolumeBand` row itself ([36,50] for both). This is not a default reused merely for convenience: it is the *same magnitude of structural change* (one non-hard easy session) that 10K's own governance process already evaluated and explicitly decided required no volume increase. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 11. Advanced peak-volume band

**Frozen: `PeakVolumeBandMin = 54 km`, `PeakVolumeBandMax = 72 km`** — HM Advanced 5D's own `[54,70]` band with the lower bound unchanged and the upper bound widened by exactly `+2 km`.

**Methodology and defensibility**: absolute-shift, directly reusing 10K's own real, approved absolute delta for the identical transition (Advanced, 5D→6D: `[42,58]→[42,60]`, lower bound unchanged, upper bound `+2km`). Percentage-shift was considered and rejected as a methodology here — it would introduce an extra transform step not anchored to any real approved number, whereas reusing the identical absolute delta 10K's own governance process already froze for this precise transition is the more parsimonious, evidence-faithful choice, and coincidentally close to a proportional analog (`70×(2/58)≈2.4km`) so it is not a large departure from that alternative either. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`. This explicitly rejects mechanical linear extrapolation of HM's own 3D/4D/5D band-width growth (which reflected the KEY2 introduction at 5D, not a comparable transition) per §38.

## 12. Selected peaks

| | SelectedPeakReferenceKmPerWeek | Basis |
|---|---|---|
| Intermediate 6D | **51.0 km** | Midpoint of `[44,58]`, unchanged from 5D — continues the established Appsel midpoint-selection convention (now confirmed at every HM cell) |
| Advanced 6D | **63.0 km** | Midpoint of the newly frozen `[54,72]` band |

Both remain a **ceiling/reference**, never a mandatory endpoint — per HM.5.3's invariant (§14), reused unmodified.

## 13. Relative weekly growth rates

**Frozen: `PreferredWeeklyIncreaseRate = 0.07`, `HardWeeklyIncreaseRate = 0.08`** for both Intermediate 6D and Advanced 6D. Classification: `DIRECT_EXISTING_AUTHORITY` — these two ratios are literally identical across **every single named policy in the entire `VolumeSafetyPolicy.cs` file**, HM and 10K, every level, every frequency including both 5D and 6D at 10K, with no exception found anywhere. Frequency does not alter relative growth rate anywhere in the product; 6D does not become the first exception. No evidence anywhere suggests 6D's extra session should change the pace of week-over-week volume growth (which governs the whole-week total, not per-session distribution).

## 14. Absolute weekly caps

**Frozen: `AbsoluteWeeklyIncreaseCapKm = 2.5 km`** for both Intermediate 6D and Advanced 6D. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — this exact value is used at HM 4D, HM 5D (both levels), and 10K's own 5D and 6D (both levels) without exception; 10K's own 5D→6D transition made zero change to this field for either level. Not `6D = 5D + fixed increment`; it is `6D = 5D`, matching the observed frequency-independence of this specific field once frequency ≥ 4.

## 15. Calibration starts

| | GoldenFixtureStartingVolumeKm | Derivation |
|---|---|---|
| Intermediate 6D | **29.5 km** | Reuses the established ratio-reuse methodology exactly: HM Intermediate 4D's own `25.0/43.0 = 0.5814` ratio applied to Intermediate 6D's own peak (51.0, unchanged from 5D): `51.0 × 0.5814 = 29.65` → rounded to the 0.5km catalog increment = `29.5` — identical to 5D's own value since the peak is unchanged |
| Advanced 6D | **36.5 km** | Reuses the established cross-level methodology: HM Advanced's own calibration always reuses Intermediate's *same-frequency* ratio applied to Advanced's own peak (5D did this: Intermediate 5D's `29.5/51.0=0.5784` ratio × Advanced 5D's `62.0` peak `=35.86→36.0`). Applying Intermediate 6D's own ratio (`0.5784`, identical since its peak is unchanged) to Advanced 6D's own newly-frozen peak (`63.0`): `63.0 × 0.5784 = 36.44` → rounded to the 0.5km increment = `36.5` |

Both remain strictly test/reference-only (§17's own scope) — never a runtime user fallback, minimum eligibility, or explicit-zero substitution, exactly matching every existing HM policy's own documented invariant (`CatalogVolumeAndLongRunPlanner.ResolveStartingVolume`'s fail-closed guard, reused unmodified). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (methodology-consistent extension of an already-established, repeatedly-applied derivation).

## 16. Intermediate LR shares

**Frozen: `PreferredMin=0.28`, `PreferredMax=0.36`, `SelectedShare=0.28`, `HardCapShare=0.36`** — unchanged from Intermediate 5D. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`, directly mirroring 10K's own real, approved zero-change decision for the identical transition (`SixDayIntermediate`'s LR shares are byte-identical to `FiveDayIntermediate`'s).

## 17. Advanced LR shares

**Frozen: `PreferredMin=0.28`, `PreferredMax=0.36`, `SelectedShare=0.28`, `HardCapShare=0.36`** — unchanged from Advanced 5D. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`, directly mirroring 10K's own real, approved zero-change decision for Advanced's identical transition (`Advanced6D`'s LR shares are byte-identical to `Advanced5D`'s, even though its peak-volume band did move).

## 18. Same-frequency LR-share reuse analysis

Confirmed: HM's existing convention (Advanced same-frequency LR-share = Intermediate same-frequency LR-share) already holds at 5D — both are `0.28/0.36/0.28/0.36`. This convention is extended to 6D: both levels freeze the identical quadruple (§16-§17). This is directly supported, not merely assumed — 10K's own real 6D values show the identical pattern (both Intermediate and Advanced 6D reuse their own 5D LR shares unchanged, and 10K's 5D shares already equal each other level-for-level at that frequency too, mirroring HM's convention). No divergence is created; no evidence anywhere calls for Advanced-specific lower shares at higher volume.

## 19. Absolute peak LR

**Frozen: `PreferredAbsolutePeakLongRunKm = 19.0 km`** for both Intermediate 6D and Advanced 6D — unchanged. Classification: `DIRECT_EXISTING_AUTHORITY` — this ceiling is HM.1.1's own frequency-independent evidence base, already re-verified true and unchanged at 3D, 4D, and 5D for both levels. It is a distance/level-scoped race-preparation ceiling, not a function of weekly running-day count; no evidence anywhere ties it to session frequency, and the default expectation (no change) is confirmed correct on direct audit — not raised merely because weekly volume increases per this phase's own explicit instruction.

## 20. Starting LR cap

**Frozen**: Intermediate 6D reuses `StartingAbsoluteLongRunCapKm = 12.0 km` (HM.10 §11's frequency-independent authority, unchanged at 3D/4D/5D). Advanced 6D reuses `N/A` (not applicable) — mirroring Advanced 5D's own precedent exactly (HM.15 §17: "unlike Intermediate 5D's..."). Classification: `DIRECT_EXISTING_AUTHORITY` for both. Both remain upper bounds only, never a default; missing readiness history never substitutes for this cap (unchanged mechanism).

## 21. Taper duration

**Frozen: 2 weeks**, unchanged, for both Intermediate 6D and Advanced 6D. Classification: `DIRECT_EXISTING_AUTHORITY` — universal across every HM cell; frequency alone is not evidence for a third taper week, and none is invented.

## 22. Taper multipliers

**Frozen: `TaperWeek1Multiplier = 0.70`, `TaperWeek2Multiplier = 0.43`**, non-chained from the fixed pre-taper anchor, for both Intermediate 6D and Advanced 6D — unchanged. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, matching the pattern every prior HM frequency phase used ("reused directly... frequency-independence re-verified").

**Evidence-tension disclosure (mandatory, per §23)**: this engagement's own do-not-reopen register already recorded, at HM.15/HM.16, that Advanced 5D's own selected peak (62.0 km/week) exceeds a cited evidence source's own named "lighter taper above ~56km/week" exception threshold, with no exact alternative percentage available — the existing 0.70/0.43 multipliers were retained as-is, a deliberate, frozen, do-not-reopen decision. **Advanced 6D's newly-frozen peak (63.0 km/week) sits even further past that same ~56km/week threshold than 5D's 62.0 did.** Per this phase's own explicit instruction ("if evidence suggests a lighter taper at higher volume but supplies no exact alternative number: record the tension, do not fabricate new multipliers, reuse may remain the safer product decision"), this tension is recorded and **not resolved** — the multipliers are reused as-is, consistent with the standing, frozen HM.15/16 decision this phase does not reopen.

## 23. Six-session taper feasibility

Simulated against the frozen minimums HM-X1.1 confirmed exist in the real allocator (`MinimumKeySessionDistanceKm=3km`, `MinimumEasySupportDistanceKm=1.5km`):

- **Intermediate 6D**, Taper Week 2 (the tighter constraint): total = `51.0 × 0.43 = 21.93 km`. Minimum non-LONG floor: `2×3 (KEY) + 3×1.5 (EASY) = 10.5 km`, leaving `~11.4 km` for LONG — comfortably feasible, no degeneracy.
- **Advanced 6D**, Taper Week 2: total = `63.0 × 0.43 = 27.09 km`. Same `10.5 km` floor, leaving `~16.6 km` for LONG — even more headroom given the higher peak.

Both taper weeks distribute safely across 6 sessions with no invented reduction to any session's dose. No capability or authority gap found.

## 24. Session allocator feasibility

Both cells route through the same generic `V1FourDaySessionVolumeAllocationPolicy`/`FourDaySessionDistanceAllocationPolicy` HM-X1.1 confirmed is already 6D-proven by 10K's own live implementation. Spot-checked at the lowest realistic total (calibration-start week): Intermediate 6D's `29.5 km` starting week, after allocating KEY1+KEY2 baseline and the ~28% LR share (`~8.3 km`), leaves comfortably more than `3×1.5=4.5km` for the three EASY_SUPPORT sessions combined (representative arithmetic: `29.5 - 8.3 - (KEY1+KEY2 baseline, typically 6-8km combined at low volume) ≈ 13-15km` split three ways `≈ 4.3-5.0km` each) — well above the `1.5km` floor per session, at the single lowest-volume week of the entire 10-16W horizon. No degenerate/near-zero session is produced anywhere in the simulated range for either level.

## 25. Minimum EASY-support feasibility

No new minimum is invented (per this phase's own instruction). The existing `MinimumEasySupportDistanceKm=1.5km` floor (already proven, already enforced by the generic allocator, already exercised safely by 10K's own live 6D at both levels) applies unmodified. §24 confirms candidate 6D weekly volumes across the entire calibration-to-peak range clear this floor with meaningful margin for all three EASY sessions simultaneously; no constraint is placed on calibration start, short-horizon reachable volume, or readiness requirements beyond what already exists.

## 26. Intermediate 10-16W simulation

Since Intermediate 6D's `CalibrationStartingVolumeKm` (29.5), `ResolvedPeakReference` (51.0, `IsSelectedCeiling=true`), `PreferredMaxWeeklyIncreaseRatio`/`HardMaxWeeklyIncreaseRatio` (0.07/0.08), `AbsoluteWeeklyIncrementCapKm` (2.5), `GoldenFixtureNonTaperTransitions` (11), and LR-share quadruple are **all literally identical** to Intermediate 5D's own already-shipped, already-tested values (§8-§18), the resulting weekly-volume growth curve, peak-vs-reference relationship, weekly absolute/relative deltas, LR vector, LR share vector, peak LR, and pre-taper anchor are **mathematically identical, week-for-week, to Intermediate 5D's own already-verified 10-16W feasibility** across every horizon (10W through 16W) — the only difference in the entire plan is that the same total weekly volume is now distributed across one additional `EASY_SUPPORT` session (§24-§25 confirm this remains non-degenerate at every point in the range). Taper W1/W2 totals: `35.7 km` / `21.93 km` (§23 confirms feasible distribution). No hidden per-horizon override is introduced; the existing HM.5.3 short/long-Core invariant (actual peak may remain below the selected reference for 10-13W; never extrapolated above it for 15-16W) applies unmodified, exactly as it already does for 5D.

## 27. Advanced 10-16W simulation

Advanced 6D's growth engine parameters (`0.07/0.08` rates, `2.5km` absolute cap, `10` non-taper transitions — note 10K's Advanced policies use `10`, HM's own use `11`; HM Advanced 5D itself already uses HM's own `11`-transition structural split per its doc comment, so Advanced 6D reuses HM's own `11`, not 10K's `10`) are unchanged from Advanced 5D; only the peak (`62.0→63.0`, `+1.6%`) and calibration start (`36.0→36.5`) move, by the small margin frozen in §11/§15. Required growth from calibration to peak: `63.0 - 36.5 = 26.5 km` versus 5D's already-proven `62.0 - 36.0 = 26.0 km` — a `0.5 km` difference in total required growth, well within the same absolute-cap-dominated climb profile 5D already exercises safely in production (at these volumes, `7%` of the current week routinely exceeds `2.5 km`, so the `2.5 km` absolute cap — not the percentage — governs most of the climb, identically to 5D). Since 5D's climb is shipped and tested feasible across the full 10-16W horizon, and 6D's required climb differs from it by less than 2%, 6D's own feasibility follows directly without a materially different risk profile at any horizon. **Dual-true-hard dose feasibility in Build/RaceSpecific**: unchanged from 5D — KEY1 (THRESHOLD_TEMPO/HM_PACE) and KEY2 (THRESHOLD_TEMPO) both retain their existing minimum-3km-plus-prescribed-dose allocation; no third hard stimulus is introduced (§4), so the dual-hard dose structure carries over unmodified. Taper W1/W2 totals: `44.1 km` / `27.09 km` (§23 confirms feasible distribution).

## 28. Calendar feasibility

Reusing HM-X1.1's confirmed generic authority (no new weekday-specific rule introduced): 6 running days / 1 non-running day, `MinimumKeySessionToLongRunSeparationDays=2` and the identical KEY-to-KEY minimum, already proven satisfiable by 10K's own live 6D schedule (`[Mon,Tue,Wed,Thu,Fri,Sun]`, Saturday rest). For **Intermediate**: primary-hard (KEY1) + moderate-FARTLEK (KEY2) + controlled/easy LONG can be placed with ≥2-day separation among KEY1/KEY2/LONG within a 6-day/1-rest week (e.g., KEY1 Tue, KEY2 Fri, LONG Sun — all pairwise ≥2 days apart). For **Advanced**: two true-hard sessions (KEY1, KEY2) + controlled/easy LONG require the same ≥2-day pairwise separation, equally satisfiable (e.g., KEY1 Tue, KEY2 Fri, LONG Sun). Both are structurally identical placement problems to what 5D already solves today (5D already has 2 KEY + 1 LONG needing the same separation, just with 2 EASY days instead of 3 — the added EASY day only adds *more* placement flexibility, never less). No calendar capability or authority gap found.

## 29. Intermediate hardness table

| Phase | KEY1 family/hardness | KEY2 family/hardness | LONG hardness | Actual true-hard count |
|---|---|---|---|---|
| Foundation | `EASY_STANDARD` / easy | `EASY_STANDARD` / easy | Easy/moderate | 0 |
| Build | `FARTLEK`→`THRESHOLD_TEMPO` / moderate→true-hard | `FARTLEK` / moderate | Steady/progressive | 1 (KEY1, later in Build) |
| RaceSpecific | `THRESHOLD_TEMPO`→`HM_PACE` / true-hard | `FARTLEK` / moderate | HM-pace-containing or steady | 1 (KEY1) |
| Taper | `HM_PACE` (reduced) / true-hard | `EASY_STANDARD` / easy | Reduced | 1 (KEY1, reduced dose) |

Frozen exactly as §3; no third hard stimulus, no escalation over 5D.

## 30. Advanced hardness table

| Phase | KEY1 family/hardness | KEY2 family/hardness | LONG hardness | Actual true-hard count |
|---|---|---|---|---|
| Foundation | `EASY_STANDARD` / easy | `EASY_STANDARD` / easy | Easy/moderate | 0 |
| Build | `FARTLEK`→`THRESHOLD_TEMPO` / moderate→true-hard | `THRESHOLD_TEMPO` / true-hard | Steady/progressive | 2 (KEY1 later-Build, KEY2) |
| RaceSpecific | `THRESHOLD_TEMPO`→`HM_PACE` / true-hard | `THRESHOLD_TEMPO` / true-hard | HM-pace-containing or steady | 2 (KEY1, KEY2) |
| Taper | `HM_PACE` (reduced) / true-hard | `EASY_STANDARD` / easy | Reduced | 1 (KEY1, reduced) |

**KEY2 HM_PACE**: prohibited (§6). **Build/RaceSpecific actual true-hard count: 2 — unchanged from 5D, no third hard stimulus.**

## 31. Adaptation reuse

```
GENERIC_ADAPTATION_REUSE
```

Confirmed, not merely inherited: the numeric simulation in §23-§28 exposed no contradiction requiring 6D-specific adaptation weights. `PlaceholderAdaptationEngine` remains fully generic (zero frequency branching), and real LongHorizon schedule-repair logic (already frequency-generic per HM-X1.1) requires no new code for either cell.

## 32. Pace / goal-feasibility compatibility

Current HM semantics preserved unmodified: no 6D-specific goal-feasibility band is created. `HM_PACE` continues to require the same approved evidence basis it already has (unchanged, reused for KEY1 exactly as at every other HM frequency); the existing no-exact-pace approved-fallback behavior applies unmodified. Desired goal continues to not create training pace, per existing invariant.

## 33. Readiness compatibility

No new 6D readiness threshold is invented, per this phase's own explicit instruction. The existing generic readiness mechanism (recent runs/week, recent weekly volume, recent longest run feeding the same fail-closed `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` logic already used at every other HM cell) already constrains realistic 6D starts: a user reporting insufficient recent volume/longest-run simply receives a lower calibrated starting point via the same unmodified mechanism, exactly as at 5D. No 6D-specific readiness authority is required or created.

## 34. Complete Intermediate 6D policy table

| Field | Value | Scope | Classification | Evidence/Governance Source |
|---|---|---|---|---|
| ProductEligible | Yes | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | HM-X1.1 |
| PeakVolumeBandMin | 44 km | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §10, 10K SixDayIntermediate precedent |
| PeakVolumeBandMax | 58 km | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §10 |
| SelectedPeakReference | 51.0 km | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §12, midpoint convention |
| PreferredWeeklyIncreaseRate | 0.07 | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §13, universal constant |
| HardWeeklyIncreaseRate | 0.08 | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §13 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | Intermediate 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §14 |
| CalibrationStartingVolume | 29.5 km | Intermediate 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §15, ratio-reuse methodology |
| LR PreferredShareMin | 0.28 | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §16 |
| LR PreferredShareMax | 0.36 | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §16 |
| LR SelectedShare | 0.28 | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §16 |
| LR HardShareCap | 0.36 | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §16 |
| PreferredAbsolutePeakLongRunKm | 19.0 km | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §19 |
| StartingAbsoluteLongRunCap | 12.0 km | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §20 |
| TaperDurationWeeks | 2 | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §21 |
| TaperWeek1Multiplier | 0.70 | Intermediate 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §22 |
| TaperWeek2Multiplier | 0.43 | Intermediate 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §22 |
| KEY1 semantics | Primary HM progression quality, unchanged from 5D | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §3 |
| KEY2 preferred family | `FARTLEK` v6 (Build/RaceSpecific) | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §5 |
| KEY2 fallback | `EASY_STANDARD` v4 | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §5 |
| KEY2 hardness | Moderate, never true-hard | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §5 |
| ActualTrueHardCount | 1 (KEY1 only) | Intermediate 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §29 |
| HM_PACE primary (KEY1) eligibility | Eligible (RaceSpecific/Taper) | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §3 |
| HM_PACE KEY2 eligibility | Prohibited | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §6 |
| Adaptation reuse | `GENERIC_ADAPTATION_REUSE` | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §31 |
| Goal-feasibility reuse | Existing HM semantics, unmodified | Intermediate 6D | `DIRECT_EXISTING_AUTHORITY` | §32 |

## 35. Complete Advanced 6D policy table

| Field | Value | Scope | Classification | Evidence/Governance Source |
|---|---|---|---|---|
| ProductEligible | Yes | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | HM-X1.1 |
| PeakVolumeBandMin | 54 km | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §11, 10K Advanced6D absolute-delta precedent |
| PeakVolumeBandMax | 72 km | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §11 |
| SelectedPeakReference | 63.0 km | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §12, midpoint of newly-frozen band |
| PreferredWeeklyIncreaseRate | 0.07 | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §13 |
| HardWeeklyIncreaseRate | 0.08 | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §13 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | Advanced 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §14 |
| CalibrationStartingVolume | 36.5 km | Advanced 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §15, cross-level ratio-reuse methodology |
| LR PreferredShareMin | 0.28 | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §17 |
| LR PreferredShareMax | 0.36 | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §17 |
| LR SelectedShare | 0.28 | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §17 |
| LR HardShareCap | 0.36 | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §17 |
| PreferredAbsolutePeakLongRunKm | 19.0 km | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §19 |
| StartingAbsoluteLongRunCap | N/A (not applicable) | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §20, mirrors Advanced 5D |
| TaperDurationWeeks | 2 | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §21 |
| TaperWeek1Multiplier | 0.70 | Advanced 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (evidence tension disclosed, unresolved by design) | §22 |
| TaperWeek2Multiplier | 0.43 | Advanced 6D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (evidence tension disclosed, unresolved by design) | §22 |
| KEY1 semantics | Primary HM progression quality, unchanged from 5D | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §4 |
| KEY2 preferred family | `THRESHOLD_TEMPO` v4 (Build/RaceSpecific) | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §5 |
| KEY2 fallback | `EASY_STANDARD` v4 | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §5 |
| KEY2 hardness | True-hard (THRESHOLD) | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §5 |
| ActualTrueHardCount | 2 (KEY1 + KEY2, Build/RaceSpecific) — **no third** | Advanced 6D | `STRONG_EVIDENCE_DERIVED_DECISION` | §30 |
| HM_PACE primary (KEY1) eligibility | Eligible (RaceSpecific/Taper) | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §4 |
| HM_PACE KEY2 eligibility | Prohibited | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §6 |
| Adaptation reuse | `GENERIC_ADAPTATION_REUSE` | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §31 |
| Goal-feasibility reuse | Existing HM semantics, unmodified | Advanced 6D | `DIRECT_EXISTING_AUTHORITY` | §32 |

Policies intentionally match Intermediate's on the following fields, with evidence-backed reuse (not copied merely to save work): growth rates (§13, universal constant across the entire product), absolute weekly cap (§14, both levels share this at every frequency ≥4), LR shares (§16-§18, HM's own established same-frequency convention plus 10K's own reinforcing precedent), taper duration/multipliers (§21-§22, frequency- and level-independent by repeated re-verification), `PreferredAbsolutePeakLongRunKm` (§19, distance/level-scoped, not frequency-scoped). They intentionally diverge on: peak-volume band/selected peak/calibration start (level-specific magnitude, §11/§12/§15), `StartingAbsoluteLongRunCapKm` (Advanced has none, mirroring 5D, §20), and KEY2 family/hardness (§5, the pre-existing FARTLEK-vs-THRESHOLD_TEMPO asymmetry).

## 36. Authority classifications

Summarized across §10-§35: `DIRECT_EXISTING_AUTHORITY` — 15 fields (growth rates, absolute peak LR, starting LR cap, taper duration, KEY1/HM_PACE-primary/HM_PACE-KEY2/adaptation/goal-feasibility semantics, product-eligibility). `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — 6 fields (absolute weekly cap, calibration starts ×2, taper multipliers ×2 with an explicit unresolved evidence-tension disclosure for Advanced). `STRONG_EVIDENCE_DERIVED_DECISION` — the remaining fields, most consequentially both peak-volume bands, both selected peaks, and both LR-share quadruples, all directly anchored to 10K's own real, approved 5D→6D transition at the same two levels — the single strongest available piece of on-point directional evidence given HM's own confirmed `SOURCE_SILENCE`. No field is classified `PRODUCT_DEFAULT` (no unanchored product judgment call was required) or `DECISION_REQUIRED` (no field was left unresolved). No source strength is overstated: every `STRONG_EVIDENCE_DERIVED_DECISION` is explicitly tied to a same-product, same-transition precedent, not merely a same-distance-family analogy.

## 37. Existing HM zero-delta statement

No file governing existing public HM V1 authority (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D) was read for modification or modified. Beginner 5D remains `PRODUCT_INELIGIBLE`, untouched. HM 6D authority is purely additive — every number frozen here is a new named policy's value, never an edit to an existing named policy.

## 38. 10K zero-delta statement

No 10K catalog or runtime file was modified. 10K's own 5D/6D `VolumeSafetyPolicy` records (`SixDayIntermediate`, `Advanced6D`, and their 5D siblings) were read only as evidence and are cited by value, never edited. The shared convention this phase discovered (zero-to-minimal change at the 5D→6D transition, no LR-share change) is reused by freezing HM's own equivalent authority, not by referencing or aliasing 10K's policy objects.

## 39. HM-X1.3 readiness

```
HM_INTERMEDIATE_6D_READY_FOR_DARK_IMPLEMENTATION
HM_ADVANCED_6D_READY_FOR_DARK_IMPLEMENTATION
```

Both cells have complete structural authority (§3-§7), complete numeric authority (§10-§22, §34-§35), confirmed 10-16W feasibility (§26-§27), confirmed session-allocation feasibility (§24-§25), confirmed calendar feasibility (§28), confirmed six-session taper feasibility (§23), and no unresolved dose number. HM-X1.3 may proceed to dark implementation: two new `VolumeSafetyPolicy` records (`HalfMarathonIntermediate6D`, `HalfMarathonAdvanced6D`) populated with §34-§35's exact values, two new taper-policy classes reusing the frozen 0.70/0.43 multipliers against each cell's own resolved peak, two new combination catalog files pointing at the existing `RUN_LAYOUT_6D`/`HALF_MARATHON_MASTER` v2 or v3/`HALF_MARATHON_WORKOUT_PROGRESSION` v2 or v3 (all unmodified), and the small set of additive dispatcher/allow-list widenings HM-X1.1's reuse matrix already identified (`case 6:` arms in `VolumeSafetyPolicy`'s two HM dispatchers and the matching `CatalogVolumeAndLongRunPlanner`/`CatalogFinalPrescribedPlanValidator` HM branches) — implementation work, explicitly out of scope for HM-X1.2 itself.

## 40. Remaining blockers

None for HM-X1.2's own scope. One item is explicitly carried forward as a recorded, unresolved tension (not a blocker): the taper-multiplier evidence tension for Advanced 6D (§22) remains open by design, consistent with the standing HM.15/16/19 do-not-reopen decision to retain 0.70/0.43 without an exact alternative number. This does not block `READY_FOR_DARK_IMPLEMENTATION` — it is the same accepted, disclosed risk already carried by Advanced 5D in production today, now marginally deeper, not a new capability or authority gap.

---

## FINAL CLASSIFICATION

```
HM_6D_INTERMEDIATE_ADVANCED_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSED
```

Both Intermediate and Advanced 6D structural policy are frozen (§3-§4); no third hard stimulus is introduced for either level (§4, §30); KEY2 semantics are frozen (§5); the HM_PACE secondary-lane prohibition is frozen for both levels (§6); both peak-volume bands are frozen with explicit, non-mechanical methodology (§10-§11); both selected peaks are frozen (§12); relative growth rates are frozen at the product-universal 0.07/0.08 (§13); absolute weekly caps are frozen (§14); calibration starts are frozen via the established ratio-reuse methodology (§15); both LR-share quadruples are frozen (§16-§18); absolute peak LR is frozen unchanged at 19.0km for both (§19); starting LR cap is frozen per level (§20); taper duration/multipliers are frozen, with the Advanced evidence tension explicitly disclosed rather than silently resolved (§21-§22); six-session taper is confirmed feasible for both cells (§23); all sessions remain non-degenerate across the full volume range simulated (§24-§25); both levels are feasible across every 10-16W horizon (§26-§27); calendar spacing is confirmed feasible (§28); no unsupported interpolation was silently promoted — every number traces to either a direct existing constant or a same-product, same-transition precedent, explicitly rejecting linear 3D/4D/5D extrapolation (§38); existing HM V1 authority remains unchanged (§37); 10K remains unchanged (§38); Experienced remains excluded (never referenced); Beginner 6D remains out of scope (never referenced); no production, catalog, schema, or frontend file was modified by this phase.
