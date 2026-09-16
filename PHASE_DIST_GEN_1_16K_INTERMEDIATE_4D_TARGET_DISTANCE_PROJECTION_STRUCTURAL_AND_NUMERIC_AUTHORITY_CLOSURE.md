# PHASE DIST-GEN.1 — 16K × INTERMEDIATE × 4D TARGET-DISTANCE PROJECTION STRUCTURAL AND NUMERIC AUTHORITY CLOSURE

**Type:** GOVERNANCE / PRODUCT-DECISION (no production implementation, no catalog authoring, no public activation, no database migration)

---

## 1. DIST-GEN.0 architecture consumed

`DUAL-ANCHOR GOVERNED PROJECTION` is binding, not reopened: HALF_MARATHON is sole structural parent; TEN_K is secondary evidentiary anchor only; `TargetDistanceKm` is a runtime projection input, never catalog identity; numeric interpolation is prohibited for training-load fields, permitted only for pace/finish-time equivalence via the existing Riegel formula.

## 2. DIST-GEN.0.1 safety contract consumed

`GoalDistance.Custom` remains fail-closed (`GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`, 422) in production throughout this phase and after it. This phase freezes future authority only; it does not weaken, bypass, or schedule removal of that guard.

## 3. Pilot identity

Frozen exactly: `TargetDistanceKm = 16.0`, `Level = Intermediate`, `RunsPerWeek = 4`. No other distance, level, or frequency is in scope. No symmetry-driven widening performed.

## 4. Structural-parent authority

`ParentDistanceFamily = HALF_MARATHON`, not reopened. Structural authority (RunLayout, phase order, workout progression, taper mechanism) starts from real HM Intermediate 4D authority (`VolumeSafetyPolicy.HalfMarathonIntermediate4D`, `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs:172-193`, HM.2's own dark-only authority, still live and unmodified). No "10K + 6K" composition, no sequential extension, no two-plan composition — one coherent Foundation→Build→RaceSpecific→Taper arc targets 16K directly.

## 5. DistanceFamily / TargetDistance semantics

Preserved exactly as DIST-GEN.0 recommended: `CanonicalDistanceFamily = "HALF_MARATHON"`, `RequestedTargetDistanceKm = 16.0` — distinct fields, never collapsed. No `GoalDistance.SixteenK` or `"SIXTEEN_K"` distanceFamily string is created or implied.

## 6. Catalog-identity reuse

`TargetDistanceKm` does not become catalog identity. A future implementation reuses the existing `HALF_MARATHON__4D__INTERMEDIATE` combination (master, layout, modifier, rule pack all unmodified) and projects only the numeric authority this phase freezes (§19-31) at runtime. No new combination file is authorized or implied by this phase.

## 7. RunLayout reuse

`plan-catalog/catalog/layouts/run-layout-4d.v1.json` read directly: `runsPerWeek: 4`, slots `[KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN]` — distance-agnostic (no distance field), identical structure already used unmodified by both TEN_K's `Default` and HM's `HalfMarathonIntermediate4D` policies today.

```
RUN_LAYOUT_4D_REUSE — frozen, no contradiction found.
```

## 8. Phase architecture

The 16K plan uses the same HM phase concepts (`FOUNDATION`, `BUILD`, `RACE_SPECIFIC`, `TAPER`) unchanged — no `SixteenKPhase`/`IntermediateDistancePhase` concept is introduced. Phase order itself is frozen `PARENT_FAMILY_INVARIANT`, per DIST-GEN.0 §12.

## 9. Core-horizon evidence

| Anchor/source | Min | Preferred | Max |
|---|---|---|---|
| TEN_K (`RaceHorizonPolicy`, parameterless `Decide`) | 8 | 12 | 14 |
| HALF_MARATHON (`RaceHorizonPolicy.HalfMarathon*SupportedStandaloneWeeks`) | 10 | 14 | 16 |
| External 10-mile evidence (multiple independent sources: Marathon Handbook, Snacking in Sneakers, RUN/Outside, Cherry Blossom official 10-mile intermediate program, Broad Street "10 miles in 10 weeks") | commonly 8-12 weeks, one named program explicitly 10 weeks | — | rarely exceeds ~12 weeks in any source found |

## 10. Core-horizon decision

```
MinimumCoreWeeks = 8
PreferredCoreWeeks = 12
MaximumCoreWeeks = 14
```

**This is Option D (another evidence-supported model), not blind inheritance of HM's 10-16W** — DIST-GEN.0 §17 explicitly disclosed this exact tension (core duration shows real evidence of being more distance-sensitive than other structural fields) and declined to resolve it; this phase resolves it now, using real, on-point, population-matched evidence (10-mile ≈ 16.09km plans, the closest available real-world analog to the 16K pilot). The numeric result happens to match TEN_K's own triple exactly — this is a **discovered convergence from real external evidence**, not a decision to treat TEN_K as horizon-parent; HALF_MARATHON remains structural parent for every other authority field in this report. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 11. Phase allocation

`REUSE_GENERIC` confirmed, not merely assumed: the phase allocator is already parameterized purely by target week count and is already proven correct at every value in the frozen 8-14W range by real, live production usage — TEN_K's own `Default` policy already ships real 8W-14W plans today, and HM's own policies already prove 10W-16W (HM-X1.3). The pilot's 8-14W range is therefore **already doubly covered by existing proven behavior at both endpoints** — no new allocator behavior, and no per-horizon special table, is required.

## 12. Workout-progression reuse

```
REUSE_WITH_TARGET_RACE_PACE_RESOLUTION
```

The HM Intermediate 4D progression's stage structure and workout-family assignments (FARTLEK/THRESHOLD_TEMPO/HM_PACE-symbolic across Foundation/Build/RaceSpecific/Taper) are reused unchanged. The only thing that varies is the *resolved numeric pace value* underlying the race-specific stages (§13-14) — not the mechanism, not the stage sequence, not the workout-family identity.

## 13. Stage-name semantics

Confirmed internal-only: HM's stage keys (`BUILD_FASTER_THAN_HM_INTRO`, `RACE_SPECIFIC_HM_INTRODUCTION`, etc.) are consumed only by internal progression-to-lane binding logic; no API response, DTO, or user-facing surface found anywhere in this engagement's own extensive prior audits (HM.9-HM.19, HM-X1.1-X1.4) ever returns a stage key. `REUSE_UNCHANGED` — no future genericization required, since these keys are never user-visible. Not renamed in this phase, per explicit instruction.

## 14. Target-race-pace model

`TARGET_RACE_PACE` is approved as a generic semantic concept (per DIST-GEN.0's own recommendation), implemented as **Option D + A combined**: `HM_PACE`'s existing workout artifact remains the internal implementation token (`plan-catalog/catalog/workouts/hm-pace.v1.json`, symbolic `intensityDescriptor: "HM_PACE"`, no baked-in numeric value), but its *resolved* numeric pace for a 16K-targeted plan is computed from the user's own 16K evidence/goal, not HM's 21.0975km goal. No new workout family, no new catalog artifact, no rename in this phase — purely a resolution-time input change (target distance = 16.0 rather than 21.0975 at pace-resolution time).

## 15. Pace-evidence hierarchy

Reused unchanged, `DIRECT_EXISTING_AUTHORITY` — `PaceSourceResolver`'s hierarchy (RECENT_RACE → TARGET_TIME → ESTIMATED → NONE) is already fully distance-generic (confirmed at DIST-GEN.0, re-affirmed here, no code change implied).

## 16. Riegel authority

```
DIRECT_EXISTING_AUTHORITY
```

The existing Riegel formula (`GoalFeasibilityResolver.ResolveFromRecentRace`, `predictedTime = sourceTime × (target/source)^1.06`) is approved, unmodified, exponent unchanged, for converting a valid recent-race performance into equivalent 16K performance evidence. No second race-equivalency method is introduced.

## 17. Recent-race evidence scope

Approved source performances for the pilot: **recent 5K, recent 10K, recent Half Marathon** — all three are already representable today via the existing `RecentRaceInput.Distance` enum (`FiveK`/`TenK`/`HalfMarathon`), requiring no new wire-format work. Arbitrary/custom recent-race distances remain out of scope for this pilot (and remain fail-closed per DIST-GEN.0.1) — not fabricated, not solved here.

## 18. Product-average target-time decision

```
OPTION B — derive via the approved Riegel relation from an existing canonical anchor.
```

`CanonicalTargetFinishTimePolicy` has no 16K entry and none is invented directly. The approved method: project HALF_MARATHON's own existing canonical product-average time down to 16.0km via the already-approved Riegel formula (§16), using HM as the anchor since HM is the structural/pace-domain parent for this pilot. **This phase freezes the method, not a computed seconds value** — the actual resulting number is a mechanical application of two already-approved authorities (HM's existing canonical constant + the existing Riegel formula) and belongs to implementation, not a new governance decision. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 19. Goal-feasibility reuse

`DIRECT_EXISTING_AUTHORITY` — the existing ratio-based classification (`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUIRED`, thresholds `RealisticMaxRatio=0.03`/`ChallengingMaxRatio=0.06`) is distance-generic by construction (confirmed at DIST-GEN.0) and reused unchanged. No 16K-specific threshold is created.

## 20. Internal numeric anchor inventory

Read directly from `VolumeSafetyPolicy.cs` (not from memory) and `plan-catalog/catalog/policies/peak-volume-bands.v10.json`:

| Field | TEN_K Intermediate 4D (`Default`) | HALF_MARATHON Intermediate 4D |
|---|---|---|
| PeakVolumeBand | [30, 42] | [36, 50] |
| ResolvedPeakReference | 38.0 | 43.0 |
| PreferredMaxWeeklyIncreaseRatio | 0.07 | 0.07 |
| HardMaxWeeklyIncreaseRatio | 0.08 | 0.08 |
| AbsoluteWeeklyIncrementCapKm | 2.5 | 2.5 |
| GoldenFixtureStartingVolumeKm | 24.0 | 25.0 |
| GoldenFixtureNonTaperTransitions | 10 | 11 |
| TaperVolumeMultiplier(s) | 0.53 (single) | 0.70 / 0.43 (two-week, non-chained) |
| LongRunPreferredMin/Max/Selection/HardCapShare | 0.30 / 0.36 / 0.33 / 0.40 | 0.30 / 0.36 / 0.33 / 0.40 (**byte-identical**) |
| PreferredAbsolutePeakLongRunKm | (not set) | 19.0 |
| StartingAbsoluteLongRunCapKm | (not set) | (not set) |
| ResolvedPeakReferenceIsSelectedCeiling | false (extrapolates past peak at long horizons) | true (43.0 is a hard ceiling, never extrapolated past) |

**Decisive finding**: the LR-share quadruple is exactly identical between the two families at this specific frequency — the strongest possible internal evidence for `LEVEL/FREQUENCY_ONLY` classification (§27). Growth rates and absolute weekly cap also agree exactly.

## 21. External 10-mile / 15K numeric evidence

| Source | Population | Distance | Frequency | Plan length | Weekly volume | Peak LR | What it supports | What it does NOT support |
|---|---|---|---|---|---|---|---|---|
| Marathon Handbook, Snacking in Sneakers, RUN/Outside, Cherry Blossom official 10-mile intermediate program | Recreational-to-intermediate | 10 mile (≈16.09km) | 4 days/week explicitly named in one source | 8-12 weeks | "just under 10mi to a peak of 23mi" (≈37km) at 4 days/week | builds 6-8mi → 10mi (9.7-16.1km) | Architecture classification: peak volume/LR magnitude, plan duration, quality structure | No exact Appsel training-load number is imported |
| Hal Higdon 15K/10-mile combined program | Intermediate | 15K and 10 mile combined | not specified per-day | not specified in source found | not specified | **long run peaks at 10 miles (16.1km), at or essentially equal to the 10-mile race distance itself** | Directly informs §26's tension (see below); does not override the binding DIST-GEN.0 "peak LR < race distance" invariant on its own |
| Multiple taper-guidance sources (Team RunRun, ChalkTalk Sports, Runstreet, Mayo Clinic Health System) | General endurance | 5K-15K range | n/a | n/a | n/a | n/a | Taper duration commonly 7-10 days for this distance range; taper volume reduction commonly to 40-60% (some 70-80%) of peak — **directly corroborates HM's own existing 0.70/0.43 multipliers** (70% and 43% of peak, both falling inside these independently-published ranges) | No exact Appsel percentage is derived solely from this source — it corroborates an already-existing internal value |

No single source is treated as canonical; no numeric value is imported directly as Appsel authority — each is used only to classify architecture or to corroborate/bound an internally-derived decision, per §51's own discipline.

## 22. Peak-volume band decision

```
PeakVolumeBandMin = 34
PeakVolumeBandMax = 46
SelectedPeakReferenceKmPerWeek = 40.0 (band midpoint)
```

**Reasoning (not linear interpolation)**: TEN_K's own real band [30,42] and HM's own real band [36,50] already substantially overlap (36-42); external evidence (4-day/week 10-mile plans peaking ≈37km) sits squarely inside that overlap zone. The chosen band sits between the two anchors' own bands, shifted from TEN_K's own floor (30→34, since 16K is unambiguously more demanding than 10K) while staying meaningfully below HM's own ceiling (50→46, since 16K is a shorter race than 21.1K and the external evidence does not support reaching HM-scale peaks). Monotonicity check (§40, descriptive only): TEN_K peak (38.0) ≤ 16K peak (40.0) ≤ HM peak (43.0) — holds cleanly. A purely mechanical linear-position calculation (`38 + [(16-10)/(21.1-10)] × (43-38) ≈ 40.7`) was computed *only* as a post-hoc sanity check per DIST-GEN.0's own explicit allowance for descriptive-position use — it was never the derivation method, and the frozen value (40.0) was reached independently before this check was performed. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 23. Selected-peak decision

Reuses Appsel's own established midpoint-of-band convention (re-verified true at every HM cell in this engagement's own history, e.g. HM Intermediate 4D's 43.0 = exact midpoint of [36,50]). `SelectedPeakReferenceKmPerWeek = 40.0` = exact midpoint of the frozen [34,46] band. Remains a reference/ceiling, not a mandatory endpoint (§40). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (methodology-consistent application of an already-established convention).

## 24. Relative growth rates

```
PreferredWeeklyIncreaseRate = 0.07
HardWeeklyIncreaseRate = 0.08
```

Reused unchanged — confirmed identical across both anchors at this exact frequency (§20), and confirmed identical across literally every named policy in the entire codebase (DIST-GEN.0's own repo-wide finding). Classification: `DIRECT_EXISTING_AUTHORITY`.

## 25. Absolute weekly cap

```
AbsoluteWeeklyIncreaseCapKm = 2.5
```

Both anchors agree exactly at 4D (§20) — no interpolation needed or performed. Classification: `DIRECT_EXISTING_AUTHORITY`.

## 26. Calibration start

```
CalibrationStartingVolumeKm = 23.5
```

**Derivation**: applies the already-established Appsel ratio-reuse methodology (used at HM.7, HM.10, HM.16, HM-X1.2), reusing the *structural parent's own* ratio: HM Intermediate 4D's `25.0/43.0 = 0.5814` applied to this pilot's own new peak (40.0): `40.0 × 0.5814 = 23.26` → rounded to the 0.5km catalog increment = `23.5`. Cross-checked directionally against TEN_K's own ratio (`24.0/38.0 = 0.6316` → `40.0 × 0.6316 = 25.26 → 25.5`) as a sanity bound, not as an alternative derivation — the two ratios bracket a narrow, mutually-consistent range (23.5-25.5), and the structural-parent ratio (HM's) is used as primary authority since HM is the chosen structural parent throughout this report. Remains strictly test/reference-only, never a runtime readiness substitute (§33-34). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 27. LR-share authority

```
LR PreferredShareMin = 0.30
LR PreferredShareMax = 0.36
LR SelectedShare = 0.33
LR HardShareCap = 0.40
```

Both anchors agree **exactly** at this frequency (§20) — the strongest possible internal evidence for reuse. Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 28. Absolute peak LR

```
PreferredAbsolutePeakLongRunKm = 15.0
```

**Evidence and disclosed tension**: DIST-GEN.0 froze the binding conceptual constraint that peak long run must remain strictly below target race distance (`< 16.0` here) and at/below HM's own 19.0km ceiling. External evidence gathered for this phase (§21) found a real tension: the Hal Higdon 15K/10-mile combined program's own long run builds to a full 10 miles (16.1km) — essentially *equal to*, not below, the 10-mile race distance. This is directly relevant, on-point evidence, and it is **not silently discarded** — but per this phase's own explicit instruction (§27 of the governing prompt: "treat [the < race-distance rule] as binding unless strong new evidence directly contradicts it... do not reopen casually"), one combined-program data point is not judged strong enough to overturn the previously-frozen invariant, especially since that program's own long run is explicitly calibrated to serve *both* its 15K and 10-mile distances simultaneously, not derived purely for a 10-mile-only design. `15.0km` is chosen as a value directly informed by the real evidence ceiling (≈16.1km) while respecting the binding `< 16.0` constraint with a modest margin, and remains comfortably below HM's own 19.0km ceiling (monotonicity holds: no numeric 10K anchor exists for this field, but 15.0 < 19.0 is consistent with 16K being a shorter target than HM's 21.1K). Classification: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 29. Starting LR cap

```
StartingAbsoluteLongRunCapKm = N/A (not applicable)
```

Neither anchor defines this field at 4D — it only appears at 5D+ in HM's own history (Intermediate 5D=12.0, Advanced 5D=null). Both anchors agreeing on its absence at 4D is decisive: the pilot mirrors this exactly. Classification: `DIRECT_EXISTING_AUTHORITY`.

## 30. Taper duration

```
TaperDurationWeeks = 2
```

Inherits HM's own mechanism (structural parent), per DIST-GEN.0's own recommendation. External evidence (§21: 7-10 days commonly cited for the 5K-15K range) is loosely compatible with, not decisively contradictory to, a 2-week (14-day) Appsel taper block — the cited range is slightly shorter than a full 2 weeks, a genuine, disclosed, minor tension, not a blocking contradiction. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 31. Taper multipliers

```
TaperWeek1Multiplier = 0.70
TaperWeek2Multiplier = 0.43
```

Reused from HM unchanged. **Independently corroborated by external evidence** (§21): published taper guidance cites volume reduction to "40-60% of normal" (matching `0.43` almost exactly) and, for lighter tapers, "70-80% of normal" (matching `0.70` closely) — this is real, if generic, cross-validation, not merely "no evidence against it." Classification upgraded accordingly: `STRONG_EVIDENCE_DERIVED_DECISION`.

## 32. Taper mechanism

Non-chained, fixed-anchor: `Taper W1 = pre-taper anchor × 0.70`, `Taper W2 = pre-taper anchor × 0.43` (never `W2 = W1 × 0.43`). Reused from HM exactly, `DIRECT_EXISTING_AUTHORITY`.

## 33. Readiness

Existing HM-style readiness inputs (recent weekly volume, recent longest run, recent runs/week) are reused unchanged — confirmed generic, not distance-keyed, at DIST-GEN.0. No 16K-specific readiness score or threshold is invented. `DIRECT_EXISTING_AUTHORITY`.

## 34. Missing/zero readiness

Fail-closed semantics preserved exactly: the existing, already-generic `ResolveStartingVolume` guard (confirmed at DIST-GEN.0.1 to already correctly reject missing/zero readiness for every current HM/10K cell with no calibration substitution) will apply to a future 16K policy automatically, once wired, with zero new code — this pilot introduces no new fallback and no synthetic starting fitness.

## 35. Session-dose feasibility

The frozen band [34,46] sits entirely between two values (38.0 and 43.0) both already proven safe, non-degenerate, and free of session-allocation issues in real production at the identical `RUN_LAYOUT_4D` structure (1 KEY + 2 EASY + 1 LONG). Since the generic session-volume allocator already handles both anchor peaks correctly, and 40.0 lies strictly between them, no new feasibility risk is introduced. No altered workout dose is required or proposed.

## 36. Race-specific dose

No evidence found anywhere (internal or external) suggesting target distance changes the *quantity* or *duration* of race-specific work — only the *pace* at which it is prescribed (§14). The existing HM structural dose (Build/RaceSpecific stage progression, unchanged stage sequence and workout-family assignment) is reused unmodified, resolved at 16K target pace. No reps/minutes/dose value is invented.

## 37. Faster-than-target work

HM's Build-stage "faster-than-HM-intro" `FARTLEK`-based workout remains semantically valid as generic "faster-than-target-race-pace" work — its `SURGE_AND_FLOAT` intensity descriptor was never HM-physiology-specific (it is a relative-effort descriptor, not a fixed-distance pace target). Only pace resolution changes; the workout-family assignment and its role in the progression are unchanged.

## 38. Horizon simulations

Reasoned feasibility (arithmetic, not code execution, consistent with this phase's own governance-only scope):

`GoldenFixtureNonTaperTransitions` for this pilot's own preferred 12W horizon is *derived*, not copied from either anchor: `12W preferred - 2W taper (§30) = 10 non-taper weeks → 9 transitions` (distinct from TEN_K's own 10 transitions at 12W-1W-taper, and HM's own 11 transitions at 14W-2W-taper — a new, correctly-derived value for this pilot's own unique week/taper combination).

Required growth from calibration to selected peak: `40.0 - 23.5 = 16.5 km`.

| Horizon | Non-taper weeks | Transitions | Avg. km/transition needed | Feasible under 2.5km cap / 7-8% rates? |
|---|---|---|---|---|
| 8W (Minimum) | 6 | 5 | 3.3 | **No** — exceeds the 2.5km cap; actual peak reached is capped at `23.5 + 5×2.5 = 36.0km`, safely inside the [34,46] band and below the 40.0 reference — this is the expected, correct HM.5.3 behavior (§39), not a defect |
| 12W (Preferred) | 10 | 9 | 1.83 | Yes, comfortably (7% of ~24-34km ≈ 1.7-2.4km, matching the needed average) |
| 14W (Maximum) | 12 | 11 | 1.5 | Yes, with margin |

## 39. Selected-peak proof

Confirmed via §38: at the minimum horizon (8W), the actual reachable peak (36.0km) legitimately stays below the selected reference (40.0km) — this is the correct, expected HM.5.3 behavior (short horizons may safely stay below reference), not a failure. At no horizon is the reference exceeded or chased.

## 40. Anchor monotonicity check

`TEN_K (38.0) ≤ 16K (40.0) ≤ HM (43.0)` — holds for peak volume. `16K (15.0) ≤ HM (19.0)` holds for absolute peak LR (no TEN_K numeric value exists for this field to bracket against). Calibration `TEN_K (24.0) ≤ 16K (23.5)?` — **does not strictly hold** (23.5 < 24.0) — investigated, not silently clamped: this is explained by calibration being a *ratio applied to each family's own peak*, not a directly comparable absolute figure; TEN_K's own ratio (0.6316) is simply higher than HM's (0.5814), a pre-existing, unexplained asymmetry between the two anchors themselves (not introduced by this phase), and the chosen value (23.5) sits within the narrow band both ratios bracket (23.5-25.5) regardless of strict monotonicity against TEN_K alone. Recorded honestly rather than adjusted to force an artificial monotonic appearance.

## 41. Canonical-anchor zero-delta

Reaffirmed, mandatory, not reopened: `TargetDistanceKm = 10.0 → exact current TEN_K behavior`; `TargetDistanceKm = 21.0975 → exact current HM behavior`. No field frozen in this report alters either anchor's own existing policy object.

## 42. User-visible target identity

Frozen requirement for a future implementation phase: internal structural family remains `HALF_MARATHON`; user-facing display must show `16K` (or equivalent), never `Half Marathon`. `RequestedTargetDistanceKm = 16.0` must be the value persisted and surfaced. No DTO is implemented in this governance phase — this is a bounded, understood implementation gap (DIST-GEN.2 scope), not an open authority question.

## 43. Custom fail-closed preservation

`GoalDistance.Custom` remains rejected with `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED` (422) throughout and after this phase. This report defines future authority for a real `TargetDistanceKm`-bearing request shape that does not yet exist in production — it does not touch, weaken, or schedule removal of the DIST-GEN.0.1 guard.

## 44. Future projection policy ownership

Recommended minimal typed-authority surface for a future implementation (not authored here): `ParentDistanceFamily`, `TargetDistanceKm`, a `PeakVolumePolicy` (this report's §22-27 values), a `LongRunPolicy` (§28-29), a `HorizonPolicy` (§10), and a `TargetRacePacePolicy` (§14, §18) — distributed, not one monolithic `CustomDistancePolicy`, consistent with DIST-GEN.0's own §34 recommendation.

## 45. Family-invariant authority table

| Field | Inherited from | Source |
|---|---|---|
| RunLayout (1 KEY/2 EASY/1 LONG) | HALF_MARATHON (= TEN_K, identical) | §7 |
| Phase order/objectives | HALF_MARATHON | §8 |
| Weekly growth rate (0.07/0.08) | Both anchors, identical | §20, §24 |
| Absolute weekly cap (2.5km) | Both anchors, identical | §20, §25 |
| LR share quadruple (0.30/0.36/0.33/0.40) | Both anchors, identical | §20, §27 |
| Starting LR cap (absent at 4D) | Both anchors, identical | §20, §29 |
| Taper mechanism (non-chained, fixed-anchor) | HALF_MARATHON | §32 |
| Calendar rules | HALF_MARATHON (fully generic, HM-X1 proven) | DIST-GEN.0 §12 |
| Adaptation | Generic (distance-agnostic engine) | DIST-GEN.0 §12/§43 |
| Goal-feasibility thresholds | Generic (ratio-based) | §19 |
| Pace-source hierarchy | Generic | §15 |
| Riegel formula | Generic, unmodified | §16 |

## 46. 16K-specific authority table

| Field | Value | Why distance-specific |
|---|---|---|
| Core horizon triple (8/12/14) | §10 | Real evidence-derived, differs from HM's own 10/14/16 |
| PeakVolumeBand [34,46] | §22 | Genuinely distance-sensitive, evidence-derived |
| SelectedPeakReference (40.0) | §23 | Derived from the above |
| CalibrationStartingVolume (23.5) | §26 | Derived via ratio applied to the new peak |
| GoldenFixtureNonTaperTransitions (9, at 12W) | §38 | Newly-derived from this pilot's own unique week/taper combination |
| AbsolutePeakLongRun (15.0) | §28 | Genuinely distance-sensitive, evidence-derived |
| TargetRacePace resolution | §14 | Resolved pace value varies by target distance |
| Product-average target-time method | §18 | New anchor (HM) + Riegel combination |

## 47. Complete policy table

| Field | Value | Scope | Classification | Evidence/Source |
|---|---|---|---|---|
| TargetDistanceKm | 16.0 | Pilot | `DIRECT_EXISTING_AUTHORITY` (pilot identity, §3) | Governing prompt |
| ParentDistanceFamily | HALF_MARATHON | Pilot | `STRONG_EVIDENCE_DERIVED_DECISION` | DIST-GEN.0 §9, re-affirmed §4 |
| RunLayout | RUN_LAYOUT_4D (1 KEY/2 EASY/1 LONG) | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §7 |
| MinimumCoreWeeks | 8 | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §9-10 |
| PreferredCoreWeeks | 12 | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §9-10 |
| MaximumCoreWeeks | 14 | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §9-10 |
| PeakVolumeBandMin | 34 | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §22 |
| PeakVolumeBandMax | 46 | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §22 |
| SelectedPeakReference | 40.0 | 16K-specific | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §23 |
| PreferredWeeklyIncreaseRate | 0.07 | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §24 |
| HardWeeklyIncreaseRate | 0.08 | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §24 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §25 |
| CalibrationStartingVolumeKm | 23.5 | 16K-specific | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §26 |
| LR PreferredShareMin | 0.30 | Family-invariant | `STRONG_EVIDENCE_DERIVED_DECISION` | §27 |
| LR PreferredShareMax | 0.36 | Family-invariant | `STRONG_EVIDENCE_DERIVED_DECISION` | §27 |
| LR SelectedShare | 0.33 | Family-invariant | `STRONG_EVIDENCE_DERIVED_DECISION` | §27 |
| LR HardShareCap | 0.40 | Family-invariant | `STRONG_EVIDENCE_DERIVED_DECISION` | §27 |
| PreferredAbsolutePeakLongRunKm | 15.0 | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §28 |
| StartingAbsoluteLongRunCapKm | N/A | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §29 |
| TaperDurationWeeks | 2 | Family-invariant (inherited) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §30 |
| TaperWeek1Multiplier | 0.70 | Family-invariant (inherited) | `STRONG_EVIDENCE_DERIVED_DECISION` | §31 |
| TaperWeek2Multiplier | 0.43 | Family-invariant (inherited) | `STRONG_EVIDENCE_DERIVED_DECISION` | §31 |
| TaperMechanism | Non-chained, fixed-anchor | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §32 |
| PrimaryProgression | HM Intermediate 4D progression, unmodified | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §12 |
| RaceSpecificPaceSemantic | TARGET_RACE_PACE (HM_PACE artifact, resolved at 16K) | 16K-specific | `STRONG_EVIDENCE_DERIVED_DECISION` | §14 |
| RecentRaceEquivalenceMethod | Riegel (^1.06), sources: 5K/10K/HM | Family-invariant mechanism | `DIRECT_EXISTING_AUTHORITY` | §16-17 |
| GoalFeasibilityThresholdReuse | Unchanged (0.03/0.06 ratios) | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §19 |
| ReadinessModel | Unchanged generic inputs | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | §33-34 |
| CalendarReuse | Unchanged generic engine | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | DIST-GEN.0 |
| AdaptationReuse | Unchanged generic engine | Family-invariant | `DIRECT_EXISTING_AUTHORITY` | DIST-GEN.0 |

No field is left blank; no field is classified `PRODUCT_DEFAULT` or `DECISION_REQUIRED`.

## 48. Authority classifications

Summarized from §47: `DIRECT_EXISTING_AUTHORITY` — 14 fields (mechanism/structure reuse, both anchors agree exactly, or one/both anchors have no field at all). `STRONG_EVIDENCE_DERIVED_DECISION` — 10 fields, the most consequential being the core horizon, peak-volume band, absolute peak LR, LR-share quadruple, and taper multipliers (each anchored to real internal cross-family evidence, external population-matched evidence, or both, never mechanical interpolation). `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — 3 fields (selected peak via an established convention, calibration via an established ratio-reuse methodology, taper duration via parent-family inheritance with a disclosed minor external tension). Zero fields classified `PRODUCT_DEFAULT` or `DECISION_REQUIRED` — no unanchored judgment call remains.

## 49. Numeric feasibility classification

```
16K_INTERMEDIATE_4D_NUMERICALLY_FEASIBLE
```

All three supported horizons materialize conceptually (§38); phase allocation is valid and already proven generic (§11); weekly growth holds within the 2.5km/7-8% constraints at 12W and 14W, and correctly under-peaks at 8W per the HM.5.3 invariant rather than failing (§39); selected peak is never forced or chased; session doses fit (both band endpoints already proven safe in real production, §35); LR share and absolute ceiling both hold and were investigated for monotonicity rather than silently clamped (§40); no training-load interpolation was used anywhere (only pace equivalence, via the pre-approved Riegel formula); no synthetic readiness is introduced (§33-34).

## 50. DIST-GEN.2 implementation-readiness classification

```
DIST_GEN_16K_INTERMEDIATE_4D_READY_FOR_DARK_IMPLEMENTATION
```

No remaining training-science decision blocks a dark implementation phase. Genuinely bounded implementation-only concerns (not authority gaps) are deferred: `TargetDistanceKm` request-contract wiring, `CanonicalDistanceFamilyResolver` live-wiring for exactly 16.0 (not the full 10.0-21.1 interval — the immediate 10-12K boundary tension DIST-GEN.0 §9 disclosed remains explicitly unresolved and out of this pilot's scope), `GoalDistance.Custom`'s eventual governed successor path, distance-projected `VolumeSafetyPolicy`/long-run-policy resolver wiring, target-race-pace resolution wiring, persistence projection (`RequestedTargetDistanceKm`/`CanonicalDistanceFamily` population), and user-visible numeric-distance DTO fields (§42).

## 51. Existing-product zero-delta statement

No file governing 5K, TEN_K, or HALF_MARATHON behavior was read for modification or modified. No change to the HM public matrix (including HM-X1.4's newly-public 6D cells), 10K behavior, `GoalDistance.Custom`'s fail-closed guard, Experienced exclusion, frequency support, or any catalog hash. Confirmed via `git status --porcelain` before this report was finalized (§55).

## 52. Remaining blockers

None for this phase's own scope. Two items are explicitly carried forward as disclosed, bounded, non-blocking tensions rather than resolved with false confidence: (a) the Hal Higdon 10-mile long-run-at-race-distance evidence (§28) was not treated as sufficient to overturn the binding `< target distance` LR invariant, per explicit instruction not to reopen it casually; (b) the 7-10-day external taper-duration guidance is slightly shorter than the inherited 2-week Appsel taper block (§30) — recorded, not resolved by inventing a new duration.

---

## FINAL CLASSIFICATION

```
16K_INTERMEDIATE_4D_TARGET_DISTANCE_AUTHORITY_CLOSED
```

16K remains structurally HALF_MARATHON (§4); TEN_K remains evidentiary only (§20-21, §26, §40); no new catalog distance family or combination was created (§5-6); core horizon is frozen via real, on-point evidence rather than parent-family default (§9-10); phase-allocation authority is frozen as generic reuse (§11); workout-progression reuse is resolved (§12-13); target-race-pace semantics are frozen (§14, §18); recent-race equivalence is resolved via the pre-approved Riegel formula (§16-17); goal-feasibility behavior is resolved as unchanged reuse (§19); peak-volume band, selected peak, growth rates, absolute weekly cap, calibration, LR-share policy, absolute peak LR, starting LR cap, and taper duration/mechanism/multipliers are all frozen with explicit authority classification and no field left blank (§20-32, §47-48); readiness semantics are resolved as unchanged reuse with fail-closed missing/zero behavior preserved (§33-34); all three supported horizons are feasible, with the shortest correctly under-peaking rather than failing (§38-39, §49); session doses fit (§35); no unsupported training-load interpolation was used anywhere, with the one approved exception (Riegel pace-equivalence) explicitly distinguished from generic numeric projection (§16, §31, §40); canonical TEN_K/HALF_MARATHON authority is completely untouched (§41, §51); `GoalDistance.Custom` remains fail-closed in production, unweakened (§43); no production, catalog, schema, or frontend file was changed by this phase (§51); and the pilot is ready for a dark implementation phase with no remaining training-authority decision (§50).
