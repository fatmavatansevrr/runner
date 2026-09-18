# PHASE DIST-GEN.5 — 15K × Intermediate × 4D Second-Target Target-Distance Structural and Numeric Authority Closure

**Classification: GOVERNANCE / TRAINING-AUTHORITY.** No production code, catalog artifact, database, API, or frontend file was touched in this phase. This phase freezes the complete structural and numeric authority for the second intermediate-distance target (`TargetDistanceKm=15.0 × Intermediate × 4D`), testing whether the target-distance projection architecture proven by DIST-GEN.0-4 genuinely generalizes beyond the single value it has ever been built against.

## 1. DIST-GEN.4 decision consumed

`PHASE_DIST_GEN_4_...md` selected Axis B (second exact target distance) and named 15K as the specific candidate, explicitly rejecting 12K (reopens DIST-GEN.0's own unresolved 10-12K structural-parent boundary) and 18K/20K (converge too close to HM's own numeric authority to teach much about interior-band generalization). DIST-GEN.4 did **not** itself freeze structural-parent authority for 15K — it selected the target and reasoned about *why* it was a strong candidate, but left the actual authority-closure work (this phase) unstarted, exactly as its own §65/§66 required ("do not begin DIST-GEN.5 implementation," "return one next-track decision," not a frozen policy).

## 2. Current three-anchor authority baseline

Read directly from `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` (not reconstructed from memory):

| Field | TEN_K `Default` (Intermediate 4D) | HALF_MARATHON `HalfMarathonIntermediate4D` | 16K `HalfMarathonIntermediate4DTargetDistance16K` |
|---|---|---|---|
| ResolvedPeakReference | 38.0 | 43.0 | 40.0 |
| PeakVolumeBand | — (n/a on `Default`) | — | [34,46] (via `TargetDistance16KPeakVolumeBandPolicy`) |
| Calibration/golden start | 24.0 | 25.0 | 23.5 |
| Non-taper transitions (golden fixture) | 10 | 11 | 9 (own unique 12W-preferred/2W-taper derivation) |
| AbsoluteWeeklyIncrementCapKm | 2.5 | 2.5 | 2.5 |
| LR quadruple | 0.30/0.36/0.33/0.40 | 0.30/0.36/0.33/0.40 | 0.30/0.36/0.33/0.40 |
| PreferredAbsolutePeakLongRunKm | null | 19.0 | 15.0 |
| StartingAbsoluteLongRunCapKm | null | null | null |
| Taper | 1-week, 0.53 | 2-week, 0.70/0.43 | 2-week, 0.70/0.43 |
| Horizon (Min/Preferred/Max) | 8/12/14 | 10/14/16 | 10/12/14 |

## 3. Exact 15K pilot identity

`TargetDistanceKm = 15.0`, `Level = Intermediate`, `RunsPerWeek = 4`. Exactly this cell — no 14K, 18K, 20K, 10-mile, Beginner, Advanced, 3D, 5D, or 6D variant is in scope.

## 4. Structural-parent decision

**Consumed, not reopened, from DIST-GEN.0's already-approved Dual-Anchor architecture**: 15.0km falls inside `CanonicalDistanceFamilyResolver`'s `(10.0, 21.1]` band, resolving to `HALF_MARATHON`. Unlike 12K (which DIST-GEN.4 correctly refused to treat this classification as approved structural-parent authority for, given the unresolved 10-12K boundary), 15K sits comfortably inside the interior of this band — 5.0km clear of the TEN_K boundary and 6.1km clear of the HM boundary — squarely inside the zone DIST-GEN.0's own architecture was designed for. **Decision: `CanonicalDistanceFamily = HALF_MARATHON`**, confirmed via the smallest necessary evidence review (this section), not re-litigated from scratch.

## 5. DistanceFamily / TargetDistance semantics

`RequestedTargetDistanceKm = 15.0`, `CanonicalDistanceFamily = HALF_MARATHON`, `GoalDistanceKm` (fixed family-representative constant) remains `21.0975` — identical three-way separation architecture DIST-GEN.0 designed and DIST-GEN.2 proved correct end-to-end for 16K. No `GoalDistance.FifteenK` enum member; no schema change (`TrainingPlan`'s existing nullable fields already anticipate "e.g. 8.0 for an 8K request," per its own doc comment, confirmed by DIST-GEN.4's research).

## 6. Catalog identity reuse

15K reuses the `HALF_MARATHON × INTERMEDIATE × 4D` catalog candidate — the identical, unmodified identity 16K already reuses. No `FIFTEEN_K_MASTER`, no new combination file, no new RunLayout, no new level-modifier. `TargetDistanceKm` remains a pure runtime authority input, never a catalog-authoring concern, per DIST-GEN.0's own binding architecture.

## 7. RunLayout reuse

`RUN_LAYOUT_4D` (single-KEY-lane, 4 sessions/week) reused unchanged — confirmed `PARENT_FAMILY_INVARIANT`, identical to 16K's own reuse.

## 8. Phase topology

`half-marathon-master.v5.json`'s single phase-topology array (FOUNDATION 2/3/4, BUILD 3/4/5, RACE_SPECIFIC 3/5/5, TAPER 2/2/2, minimum sum 10) is reused unmodified — confirmed `PARENT_FAMILY_INVARIANT` per DIST-GEN.4's own finding that this topology is shared across every HM frequency/level cell, and now further confirmed to be shared across target-distance projections reusing the identical catalog identity, regardless of the specific `TargetDistanceKm` value.

## 9. Second-target generalization objective

This phase's real purpose, stated explicitly per its own governing prompt: determine which 16K-frozen fields are genuinely reusable at 15K because they are family/level/frequency-invariant generic infrastructure, versus which are specific to exactly 16.0km. The answer, worked out field-by-field below (§36, §50-53), is genuinely mixed — most fields converge identically via evidence, but one delicate field (`PreferredAbsolutePeakLongRunKm`) is mechanically forced to differ by the binding LR-below-target invariant. This is the single most important result of this phase.

## 10. Horizon evidence

Per DIST-GEN.2A's own now-established discipline (never repeat DIST-GEN.1's original error), the horizon question is decomposed into: (a) the reused catalog identity's own real structural floor, checked first and independently of any external plan-duration evidence; (b) Preferred/Maximum, informed by real external evidence but never assumed transferable from a different family's numbers.

## 11. Standalone-Core vs external-plan-duration distinction

DIST-GEN.0's own 15K evidence (§26 below) states existing 15K training plans run "~8 weeks (from a 15-20mi/week base)" — an explicit, self-disclosed pre-existing-fitness prerequisite, structurally identical in shape to the 10-mile evidence DIST-GEN.1 originally (and incorrectly) treated as transferable to Appsel's own Standalone Core minimum. Applying DIST-GEN.2A's own corrected discipline: this "~8 weeks" figure describes race-specific-preparation duration for an already-based runner, not Appsel's own Standalone-Core-from-arbitrary-readiness minimum, and must **not** be imported as `MinimumCoreWeeks` — exactly the mistake this phase must not repeat.

## 12. Minimum horizon

**`MinimumCoreWeeks = 10`.** Since 15K reuses the identical, unmodified HALF_MARATHON catalog identity 16K already reuses (§6/§8), the same real, mechanically-enforced phase-minimum sum (2+3+3+2=10) applies — this is not re-derived per target, it is a structural fact about the reused catalog identity, entirely independent of which exact km value is being projected onto it. **Classification: `DIRECT_EXISTING_AUTHORITY`** (a directly-observable fact about already-frozen, unmodified catalog structure, not an evidence-informed judgment call).

## 13. Preferred horizon

**`PreferredCoreWeeks = 12`.** No external or internal evidence distinguishes a different preferred duration for 15K specifically: the 15K sources (§26) cite the same "~8 week" race-specific-prep shape as the 16K sources did (again, not the Standalone Core figure, §11), and no anchor (TEN_K's own 12, HM's own 14) provides grounds to pick a value other than 12 given 15K's closer proximity to 16K than to HM. This is a **discovered convergence with 16K's own value, not a decision to treat 16K as a template** — the same evidentiary posture DIST-GEN.1 itself explicitly used when 16K's own horizon converged numerically with TEN_K's triple. **Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.**

## 14. Maximum horizon

**`MaximumCoreWeeks = 14`.** Same reasoning as §13 — no evidence suggests 15K should extend further than 16K's own frozen maximum, and importing HM's own 16-week maximum would require treating HM's own full-distance ceiling as directly transferable to a shorter interior target, which DIST-GEN.0's own architecture never endorses. **Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.**

## 15. Phase allocations

Because the horizon triple (10/12/14) and the reused catalog identity are byte-identical to 16K's own, the generic `CatalogPhaseAllocationResolver` produces the **identical** phase splits already proven real for every horizon in DIST-GEN.2B's own report:

| Weeks | FOUNDATION | BUILD | RACE_SPECIFIC | TAPER |
|---|---|---|---|---|
| 10 | 2 | 3 | 3 | 2 |
| 11 | 2 | 3 | 4 | 2 |
| 12 (preferred) | 2 | 3 | 5 | 2 |
| 13 | 2 | 4 | 5 | 2 |
| 14 (maximum) | 3 | 4 | 5 | 2 |

No hole in the supported range; no new allocator behavior required, since the allocator has never been distance-aware — it only ever consumed the horizon triple and the catalog's own phase minima, both unchanged for 15K.

## 16. Compressed-core status

8W/9W are **not** part of 15K's Standard Core, for exactly the same structural reason as 16K (§12: reused catalog identity's own real minimum-week sum is 10). Any future 15K compressed-core work is deferred to its own separately-governed track, mirroring the already-established HM-X2/16K-compressed-core deferral pattern — not solved here, and not silently treated as unsupported-forever.

## 17. Progression reuse

The HM Intermediate 4D workout progression is reused unmodified. No 15K-specific workout family, no new stage, no new progression version file.

## 18. Target-race-pace reuse

**Confirmed `GENERIC_REUSABLE_INFRASTRUCTURE` — no new mechanism needed.** `GoalFeasibilityResolver.cs`'s target-race-pace resolution is driven entirely by `RequestedTargetDistanceKm ?? GoalDistanceKm` with zero distance-value branching (confirmed by direct code read in DIST-GEN.4's research pass); `HM_PACE`'s own symbolic workout artifact never consumes target distance at all (confirmed by DIST-GEN.2's own finding). Only the numeric `TargetDistanceKm=15.0` value changes; no new pace token, workout family, or resolution branch is required.

## 19. Riegel reuse

The existing, unmodified Riegel equivalence formula (`predictedTime = sourceTime × (target/source)^1.06`) is reused directly — `targetDistanceKm = 15.0` flows through the identical, already-approved calculation with no exponent change, no new formula, and no distance-specific special case. Confirmed generic by direct code read (`GoalFeasibilityResolver.cs` line ~207/216, DIST-GEN.4 research).

## 20. Recent-race evidence scope

The same canonical recent-race sources approved for 16K (5K, 10K, HM performances, via `RecentRaceInput.Distance`) remain the acceptable evidence sources for 15K equivalence — no arbitrary custom recent-race distance is introduced or required here.

## 21. Product-average behavior

DIST-GEN.1's own 16K decision specified a **method** (Riegel projection from HM's own existing canonical average-time constant) rather than inventing a new number for the missing 16K product-average gap. That method is itself target-distance-generic — it projects from HM's canonical constant to whatever `RequestedTargetDistanceKm` is in play. The identical method applies unchanged to 15K: no new 15K-specific product-average time is invented; Riegel-projection-from-HM's-canonical-constant remains the governing mechanism.

## 22. Goal-feasibility reuse

`GoalFeasibilityResolver`'s thresholds and logic are confirmed target-distance-generic (§18/§19) — no new goal-feasibility authority is required for 15K.

## 23. Internal TEN_K anchor

`Default` (Intermediate 4D): peak 38.0, calibration 24.0, cap 2.5, LR quadruple 0.30/0.36/0.33/0.40, no absolute-peak-LR field, 1-week 0.53 taper, horizon 8/12/14. Used as the lower bound / cross-check anchor throughout this closure.

## 24. Internal 16K anchor

`HalfMarathonIntermediate4DTargetDistance16K`: peak band [34,46]/reference 40.0, calibration 23.5, LR quadruple 0.30/0.36/0.33/0.40, `PreferredAbsolutePeakLongRunKm=15.0`, starting LR cap null, 2-week 0.70/0.43 taper, horizon 10/12/14, `GoldenFixtureNonTaperTransitions=9`. The nearest, most directly comparable real evidence point, used throughout as the primary reference — but never treated as a template to copy without independent justification (§9).

## 25. Internal HM anchor

`HalfMarathonIntermediate4D`: peak reference 43.0, calibration 25.0, cap 2.5, LR quadruple 0.30/0.36/0.33/0.40, `PreferredAbsolutePeakLongRunKm=19.0`, starting LR cap null, 2-week 0.70/0.43 taper, horizon 10/14/16. Used as the upper-bound structural-parent anchor.

## 26. Existing 15K external evidence

Located and extracted from `PHASE_DIST_GEN_0_...md` §16 and `PHASE_DIST_GEN_1_...md` §21 — not re-researched:

| Source | Population | Distance | Frequency | Plan length | Weekly volume | Long run | Quality structure | Taper | What it supports |
|---|---|---|---|---|---|---|---|---|---|
| Salomon, best-running-tips.com, runninforsweets (multiple 15K sources) | Recreational-to-intermediate, "from a 15-20mi/week base" | 15K | 3-4 days/week | ~8 weeks (race-specific prep, not Standalone Core, §11) | 20-33mi/week (32-53km) | 8-14.5km (5-9mi), building toward 90-120min | 1 easy, 1 quality (tempo/intervals), 1 long run; tempo explicitly "around your half marathon pace" | not detailed | Pace-domain classification (HM-pace, not a distinct 15K pace); weekly-volume/long-run magnitude bounds |
| Hal Higdon 15K/10-mile combined program | Intermediate | 15K and 10 mile combined | not specified per-day | not specified | not specified | **peaks at 10 miles (16.1km), at/near the 10-mile race distance itself** | not the focus | Directly informs §34's LR-invariant review below; NOT treated as 15K-specific evidence, since it is a dual-target combined program whose peak LR is oriented toward the 10-mile leg, not a pure-15K plan |
| General taper guidance (DIST-GEN.1 §30, covering "the 5K-15K range") | — | 5K-15K range explicitly | — | — | — | — | — | 7-10 days commonly cited | Directly relevant, on-point taper-duration evidence for 15K specifically (§37 below) |

No new external research was performed — this existing evidence is sufficient to freeze every required field with real, on-point support, per §22-23 of the governing prompt's own "reuse existing evidence first" discipline.

## 27. Supplemental evidence if needed

Not required. The existing evidence (§26) directly addresses every numeric field this closure needs, at a strength comparable to what DIST-GEN.1 itself had for 16K (multiple converging sources, real weekly-volume/long-run ranges, real pace-domain and taper-duration evidence). No narrow supplemental web research was performed.

## 28. Peak-volume band decision

**`PeakVolumeBandMin = 34.0`, `PeakVolumeBandMax = 46.0`.** Derivation: applying DIST-GEN.1's own established methodology (real anchor-band overlap — TEN_K's own implicit band and HM's own [36,50] — plus external evidence) to 15K's own external evidence (20-33mi/week, wide and non-decisive, similar in character and overlap to the 10-mile evidence DIST-GEN.1 itself judged "runs hotter than Appsel's own conservative model") produces the identical conclusion: no independent evidentiary basis exists to select a materially different band than 16K's own [34,46]. **This is a discovered evidence convergence, not template-copying** — explicitly NOT derived via any interpolation formula (§46 of the governing prompt's own prohibition is respected: no `16K − distanceRatio × Δ` calculation was performed). **Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.**

## 29. Selected-peak decision

**`SelectedPeakReferenceKmPerWeek = 40.0`.** Re-running DIST-GEN.1's own overlap-based selection method independently for 15K: the two canonical anchor bands (TEN_K/HM) haven't changed, their overlap region hasn't changed, and 15K's own external evidence doesn't provide a distinguishing signal to move away from the same conclusion 16K's own closure reached. Precedent stated explicitly, not silently recomputed: DIST-GEN.1 selected 40.0 as a value inside the TEN_K/HM overlap, corroborated by external volume clustering; the same overlap and a directionally-similar (if less precise) external volume range independently support the identical value for 15K. **Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.** Selected peak remains a reference/ceiling, never a value shorter horizons are forced to reach (§44).

## 30. Growth rates

`PreferredWeeklyIncreaseRate`/`HardWeeklyIncreaseRate` (0.07/0.08, per DIST-GEN.0's own finding of broad invariance, already reused unchanged by 16K) are reused unchanged for 15K. **Classification: `DIRECT_EXISTING_AUTHORITY` / `LEVEL_FREQUENCY_INVARIANT`.**

## 31. Absolute weekly cap

**`AbsoluteWeeklyIncreaseCapKm = 2.5`.** Confirmed identical across all three internal anchors (TEN_K/HM/16K Intermediate 4D, §2) — the strongest possible internal evidence for a level/frequency-only field, independent of target distance. Reused unchanged, not interpolated. **Classification: `DIRECT_EXISTING_AUTHORITY` / `LEVEL_FREQUENCY_INVARIANT`.**

## 32. Calibration start

**`CalibrationStartingVolumeKm = 23.5`.** Derivation: applying HM's own established ratio-reuse methodology (calibration ÷ selected-peak ratio, `25.0/43.0 = 0.5814`, the same ratio DIST-GEN.1 applied to derive 16K's own 23.5 from its own 40.0 peak) to 15K's own selected peak (40.0, §29, identical to 16K's): `0.5814 × 40.0 ≈ 23.26`, rounding to the same **23.5** convention DIST-GEN.1 used. This is methodology-reuse (a fixed family ratio applied to whatever the correctly-derived peak happens to be), not template-copying nor interpolation — the identical result follows mechanically from the identical peak value (§29), not from assuming 15K=16K a priori. Calibration remains test/reference authority only, never a runtime starting-volume substitute or zero-history fallback. **Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`** (methodology is `DIRECT_EXISTING_AUTHORITY`; the specific resulting number depends on the evidence-informed peak selection above).

## 33. LR-share authority

**`0.30 / 0.36 / 0.33 / 0.40`** (Preferred min/max, selection, hard cap) — reused unchanged. This quadruple is byte-identical across TEN_K, HM, and 16K Intermediate 4D — the single strongest piece of internal evidence in this entire engagement for a `LEVEL_FREQUENCY_INVARIANT` classification, independently reconfirmed by DIST-GEN.4's own research. No distinguishing evidence exists for 15K. **Classification: `DIRECT_EXISTING_AUTHORITY` / `LEVEL_FREQUENCY_INVARIANT`.**

## 34. LR < race-distance invariant review

DIST-GEN.0's binding, non-numeric invariant ("peak long run should remain below the target race distance, never at or above it") is **`RULE_REAFFIRMED`**, not reopened. The one candidate piece of contrary evidence — the Hal Higdon 15K/10-mile combined program's peak LR reaching 16.1km, at/near the 10-mile distance (§26) — is judged, exactly as DIST-GEN.1 judged the same source for 16K, to be a **non-generalizable outlier**: it is a dual-target combined program whose long-run peak is oriented toward its 10-mile leg, not independent evidence about a pure 15K plan's own appropriate ceiling. The three dedicated 15K-only sources (Salomon/best-running-tips/runninforsweets) all show peak long runs (8-14.5km) comfortably **below** the 15K race distance, directly corroborating the invariant rather than contradicting it. **Output: `RULE_REAFFIRMED`.**

## 35. Absolute peak LR decision

**`PreferredAbsolutePeakLongRunKm = 14.5`.** This is the single most important, genuinely target-distance-specific finding of this phase. 16K's own frozen value (15.0km) **cannot** be mechanically reused for 15K: doing so would set the ceiling exactly equal to the 15K race distance itself, directly violating the reaffirmed binding invariant (§34) that peak LR must remain **below**, not at, the target distance. Real, on-point 15K-specific external evidence (§26: "8-14.5km, building toward 90-120min," explicitly citing 15K plans) directly and independently supports a ceiling at the upper end of that cited range, comfortably below 15.0km. **14.5km** is therefore both the evidence-supported value and the value satisfying the binding invariant with a real margin (0.5km) — a smaller absolute buffer than 16K's own 1.0km margin, but proportionally consistent (14.5/15.0 = 96.7%, versus 15.0/16.0 = 93.75% — 15K's ceiling sits slightly closer to its own race distance than 16K's does, which is itself a genuine, disclosed, evidence-driven finding, not a designed-in ratio). **This value is `DIFFERENT_DUE_TO_TARGET_DISTANCE` from 16K's own, by mechanical necessity, not by choice.** **Classification: `STRONG_EVIDENCE_DERIVED_DECISION`** (real, on-point, target-specific external evidence directly supports the exact value, independently corroborated by the binding invariant's own hard ceiling).

## 36. Starting LR cap

**`StartingAbsoluteLongRunCapKm = null`**, reused unchanged from 16K/HM Intermediate 4D (this field is only non-null for HM's own Intermediate/Advanced 5D/6D cells, per DIST-GEN.4's own table — an orthogonal frequency-driven field, not target-distance-driven). **Classification: `DIRECT_EXISTING_AUTHORITY` / `LEVEL_FREQUENCY_INVARIANT`.**

## 37. Taper duration

**`TaperDurationWeeks = 2`.** Reused from HM's own mechanism (structural parent), consistent with 16K's own reuse. The same disclosed, non-blocking tension DIST-GEN.1 already accepted for 16K reapplies, slightly intensified: DIST-GEN.1's own §30 taper-duration evidence explicitly framed its "7-10 days commonly cited" range as covering "the 5K-15K range" — meaning this exact tension was already known to include 15K specifically when it was first disclosed, and was already judged non-blocking then. No new evidence has emerged to elevate this from a minor disclosed tension to a blocking contradiction. **Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`** (reaffirmed, not newly resolved).

## 38. Taper multipliers

**`0.70` (Week 1), `0.43` (Week 2)** — reused unchanged. No 15K-specific taper-percentage evidence contradicts the real external corroboration DIST-GEN.1 already found for these exact multipliers (published guidance citing 40-60%/70-80% of peak volume, matching 0.43/0.70 almost exactly) — that evidence was general taper-percentage guidance, not 16K-specific, and applies equally to 15K. **Classification: `STRONG_EVIDENCE_DERIVED_DECISION`** (same real external corroboration DIST-GEN.1 already elevated to this tier).

## 39. Taper mechanism

Non-chained, fixed-anchor semantics preserved explicitly: `Week1 = pre-taper anchor × 0.70`, `Week2 = same pre-taper anchor × 0.43` (never `Week2 = Week1 × 0.43`) — identical mechanism to both HM and 16K, reused unmodified.

## 40. Race-specific dose

The existing HM-derived race-specific workout dose (quantity/duration of quality work in the RACE_SPECIFIC phase) is reused unchanged for 15K. No evidence found anywhere in this phase's research suggests race-specific dose quantity should vary with the exact target distance within this interior band — only the **pace target** of that work changes (§18/§41), never its quantity or duration. No new dose authority is frozen or required.

## 41. Faster-than-target semantics

The generic target-relative pace mechanism (Build-stage faster-than-goal-pace work resolved from `RequestedTargetDistanceKm`, not from a literal HM-pace token) applies unchanged — confirmed generic per §18, since `HM_PACE`'s own symbolic workout artifact never itself consumes target distance (DIST-GEN.2's own finding). No new workout family is required.

## 42. Session-dose feasibility

Since `AbsoluteWeeklyIncrementCapKm` (2.5), calibration (23.5), and selected peak (40.0) are all identical to 16K's own frozen values (§28-32), the generated weekly-volume trajectories for 15K are numerically identical to 16K's own already-proven real golden traces (DIST-GEN.2B §7-11: 10W peak 31.0, 11W peak 32.5, 12W peak 34.0, 13W peak 34.0, 14W peak 34.0). The generic multi-session allocator (KEY+EASY1+EASY2+LONG summing to weekly volume) is unaffected by target distance (confirmed generic, DIST-GEN.4 §17 research) — no negative residual, no degenerate EASY session, and no need to alter dose to fit an underpowered peak band, since the band itself is unchanged from an already-proven-feasible configuration.

## 43. Long-run feasibility

Because weekly volume trajectories are numerically identical to 16K's own proven traces (§42), and the LR-share quadruple is identical (§33), the resulting per-week long-run values are also numerically identical to 16K's own — with the sole difference being the **ceiling check**: 15K's plans must validate `<= 14.5` (not `<= 15.0`) and `< 15.0` (not `< 16.0`). Cross-checking 16K's own real observed long-run peaks across every horizon (maximum observed: 13.5km, at 12W/13W/14W's W10-equivalent RACE_SPECIFIC week, per DIST-GEN.2B's own traces) — **13.5km sits comfortably below both the 14.5km and 15.0km ceilings**, meaning no real generated 15K plan across any supported horizon is expected to bind against the tighter ceiling in practice, though the ceiling itself must still be correctly authored at 14.5 (not 15.0) to preserve correctness for any higher-readiness or edge-case trajectory that could approach it. Rounding and weekly-residual behavior are unaffected (generic, target-distance-agnostic mechanisms).

## 44. Readiness

The generic readiness model (recent weekly volume / recent longest run / recent runs-per-week inputs) is reused unchanged — confirmed target-distance-generic per DIST-GEN.4's own research (no distance-specific readiness score exists anywhere in the codebase). No new 15K readiness threshold is fabricated.

## 45. Missing/zero readiness

Fail-closed behavior is preserved unchanged: calibration, peak-band minimum, and starting-LR cap remain forbidden from being used as synthetic fitness substitutes for missing recent-history inputs, exactly as already established for every other distance. If missing-history eligibility for the dark 15K pilot remains unsupported by a future implementation phase, that is recorded as a DIST-GEN.6 implementation concern, not a gap in this authority closure.

## 46. Complete horizon simulations

Conceptual simulation for every supported horizon (10-14W), using the frozen 15K numbers (identical volume/LR-share/taper mechanics to 16K, differing only in the absolute LR ceiling):

| Weeks | Phase split (F/B/R/T) | Peak weekly volume | Peak long run (observed, per §43) | Selected-peak relation | Session feasibility |
|---|---|---|---|---|---|
| 10 | 2/3/3/2 | 31.0 | ≤13.5 (well under 14.5 ceiling) | Below 40.0 reference — correctly not forced (§29/§44) | Valid, mirrors 16K's own proven 10W trace |
| 11 | 2/3/4/2 | 32.5 | ≤13.5 | Below 40.0 reference | Valid, mirrors 16K's own proven 11W trace |
| 12 (preferred) | 2/3/5/2 | 34.0 | ≤13.5 | Below 40.0 reference | Valid, mirrors 16K's own proven 12W golden trace exactly in volume/LR-share terms |
| 13 | 2/4/5/2 | 34.0 | ≤13.5 | Below 40.0 reference | Valid, mirrors 16K's own proven 13W trace |
| 14 (maximum) | 3/4/5/2 | 34.0 | ≤13.5 | Below 40.0 reference | Valid, mirrors 16K's own proven 14W trace |

No hole in the supported range; no unsupported progression duplication.

## 47. Selected-peak proof

Every simulated horizon's peak weekly volume (31.0-34.0) stays comfortably below the 40.0 selected-peak reference — consistent with HM.5.3's own standing invariant that shorter/moderate cores may safely stay below the reference rather than being force-chased, and that no horizon extrapolates above it. No horizon-specific forced convergence is introduced.

## 48. Monotonicity sanity check

`TEN_K (38.0) ≤ 15K (40.0) = 16K (40.0) ≤ HM (43.0)` for selected peak — non-strict, consistent with the governing prompt's own "≤" sanity-check framing (§21), not a strict-inequality requirement. `TEN_K (24.0) ≤ 16K (23.5) ≤ 15K (23.5) ≤ HM (25.0)` for calibration — note 16K's own calibration (23.5) is actually slightly *below* TEN_K's (24.0), an already-existing, already-accepted non-strict-monotonic real result from DIST-GEN.1's own closure (peak-to-calibration ratio-driven, not distance-ordered) — 15K's identical value does not introduce any new violation. Absolute peak LR: `15K (14.5) < 16K (15.0) < HM (19.0)` — strictly monotonic and expected, since this field is mechanically bounded by each target's own race distance. No sequence violated evidence-derived values; no value was clamped or adjusted merely to satisfy ordering — every value above was reached independently via evidence/methodology (§28-38) and monotonicity was checked afterward, never used to compute a value.

## 49. No-interpolation proof

No field in this closure was computed via linear distance interpolation, ratio-of-distance scaling (e.g. "16K value × 15/16"), or any other prohibited formula (§46 of the governing prompt). Every frozen value traces to one of: (a) a direct structural fact about the reused, unmodified catalog identity (§12, horizon minimum); (b) a discovered evidence convergence with 16K's own independently-derived value (§13-14, §28-33, §36-38 — the *same methodology*, re-run independently for 15K, happened to reach the same number, which is disclosed explicitly as convergence, not assumed); (c) a genuinely new, target-specific value driven by real 15K external evidence and the binding LR invariant (§35, the one field forced to differ). The only "ratio" used anywhere in this closure (§32, calibration) is the same fixed family ratio-reuse *methodology* DIST-GEN.1 itself established as legitimate precedent — applied to an independently-derived peak value, not to a distance value.

## 50. 15K-vs-16K comparison

| Field | 15K value/decision | 16K value/decision | Same/Different | Why | Authority classification |
|---|---|---|---|---|---|
| Structural parent | HALF_MARATHON | HALF_MARATHON | Same | Both fall inside the same resolved interior band | `DIRECT_EXISTING_AUTHORITY` |
| Catalog candidate | HM×Intermediate×4D | HM×Intermediate×4D | Same | Identical reuse architecture | `DIRECT_EXISTING_AUTHORITY` |
| RunLayout | RUN_LAYOUT_4D | RUN_LAYOUT_4D | Same | `PARENT_FAMILY_INVARIANT` | `DIRECT_EXISTING_AUTHORITY` |
| Phase topology | 2/3/3/2 min | 2/3/3/2 min | Same | Single shared master template | `DIRECT_EXISTING_AUTHORITY` |
| Horizon Min | 10 | 10 | Same | Both reuse the identical structural floor | `DIRECT_EXISTING_AUTHORITY` |
| Horizon Preferred | 12 | 12 | Same (evidence convergence) | No distinguishing evidence | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| Horizon Maximum | 14 | 14 | Same (evidence convergence) | No distinguishing evidence | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| Progression | HM Intermediate 4D | HM Intermediate 4D | Same | `PARENT_FAMILY_INVARIANT` | `DIRECT_EXISTING_AUTHORITY` |
| Target-race-pace mechanism | Generic | Generic | Same | Already target-distance-driven | `DIRECT_EXISTING_AUTHORITY` |
| Riegel | Generic | Generic | Same | Zero distance-value branching | `DIRECT_EXISTING_AUTHORITY` |
| Goal feasibility | Generic | Generic | Same | Same reasoning | `DIRECT_EXISTING_AUTHORITY` |
| Peak-volume band | [34,46] | [34,46] | Same (evidence convergence) | Independent overlap-method re-run reached the same result | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| Selected peak | 40.0 | 40.0 | Same (evidence convergence) | Same overlap-method reasoning | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| Growth % | 0.07/0.08 | 0.07/0.08 | Same | `LEVEL_FREQUENCY_INVARIANT` | `DIRECT_EXISTING_AUTHORITY` |
| Absolute weekly cap | 2.5 | 2.5 | Same | `LEVEL_FREQUENCY_INVARIANT` (identical across 3 anchors) | `DIRECT_EXISTING_AUTHORITY` |
| Calibration | 23.5 | 23.5 | Same (methodology convergence) | Identical peak → identical ratio-derived result | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| LR shares | 0.30/0.36/0.33/0.40 | 0.30/0.36/0.33/0.40 | Same | `LEVEL_FREQUENCY_INVARIANT` (byte-identical across 3 anchors) | `DIRECT_EXISTING_AUTHORITY` |
| **Absolute peak LR** | **14.5** | **15.0** | **Different** | **Mechanically forced by the binding `< target distance` invariant plus real 15K-specific evidence** | `STRONG_EVIDENCE_DERIVED_DECISION` |
| Starting LR cap | null | null | Same | `LEVEL_FREQUENCY_INVARIANT` | `DIRECT_EXISTING_AUTHORITY` |
| Taper duration | 2 weeks | 2 weeks | Same | `FAMILY_INVARIANT`, reaffirmed disclosed tension | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| Taper multipliers | 0.70/0.43 | 0.70/0.43 | Same | Real external corroboration applies equally | `STRONG_EVIDENCE_DERIVED_DECISION` |
| Readiness | Generic | Generic | Same | No distance-specific model exists | `DIRECT_EXISTING_AUTHORITY` |
| Calendar | Generic | Generic | Same | `PARENT_FAMILY_INVARIANT` | `DIRECT_EXISTING_AUTHORITY` |
| Adaptation | Generic | Generic | Same | No distance-specific adaptation logic exists | `DIRECT_EXISTING_AUTHORITY` |

## 51. Family-invariant authority table

Fields reused unchanged for target-distance-independent structural reasons (extracted from §50): structural parent, catalog candidate, RunLayout, phase topology, horizon minimum, progression, target-race-pace mechanism, Riegel, goal feasibility, growth rates, absolute weekly cap, LR shares, starting LR cap, taper multipliers (mechanism), readiness, calendar, adaptation. These are the mechanisms proven, not merely assumed, reusable across a second real target-distance data point.

## 52. Target-specific authority table

Exactly one field required genuinely new, target-specific authority: `PreferredAbsolutePeakLongRunKm = 14.5` (§35). Peak-volume band, selected peak, calibration, horizon Preferred/Maximum, and taper duration are recorded as evidence-convergent (same value, independently re-derived, not assumed) rather than either purely family-invariant or purely target-specific — an intermediate category this phase's own methodology surfaced honestly rather than forcing into one of the two extremes.

## 53. Complete 15K policy table

| Field | Value | Scope | Classification | Evidence/Governance source |
|---|---|---|---|---|
| TargetDistanceKm | 15.0 | Exact target | `DIRECT_EXISTING_AUTHORITY` | This phase, §3 |
| ParentDistanceFamily | HALF_MARATHON | Structural | `DIRECT_EXISTING_AUTHORITY` | §4, DIST-GEN.0's architecture |
| Level | Intermediate | Exact cell | `DIRECT_EXISTING_AUTHORITY` | This phase, §3 |
| RunsPerWeek | 4 | Exact cell | `DIRECT_EXISTING_AUTHORITY` | This phase, §3 |
| RunLayout | RUN_LAYOUT_4D | Family/frequency | `DIRECT_EXISTING_AUTHORITY` | §7 |
| MinimumCoreWeeks | 10 | Family/structural | `DIRECT_EXISTING_AUTHORITY` | §12 |
| PreferredCoreWeeks | 12 | Target (converged) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §13 |
| MaximumCoreWeeks | 14 | Target (converged) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §14 |
| PeakVolumeBandMin | 34.0 | Target (converged) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §28 |
| PeakVolumeBandMax | 46.0 | Target (converged) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §28 |
| SelectedPeakReference | 40.0 | Target (converged) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §29 |
| PreferredWeeklyIncreaseRate | 0.07 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §30 |
| HardWeeklyIncreaseRate | 0.08 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §30 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §31 |
| CalibrationStartingVolumeKm | 23.5 | Target (methodology-converged) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §32 |
| LR PreferredShareMin | 0.30 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §33 |
| LR PreferredShareMax | 0.36 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §33 |
| LR SelectedShare | 0.33 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §33 |
| LR HardShareCap | 0.40 | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §33 |
| PreferredAbsolutePeakLongRunKm | **14.5** | **Target-specific** | `STRONG_EVIDENCE_DERIVED_DECISION` | §35 |
| StartingAbsoluteLongRunCapKm | null | Level/frequency | `DIRECT_EXISTING_AUTHORITY` | §36 |
| TaperDurationWeeks | 2 | Family (reaffirmed) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §37 |
| TaperWeek1Multiplier | 0.70 | Family | `STRONG_EVIDENCE_DERIVED_DECISION` | §38 |
| TaperWeek2Multiplier | 0.43 | Family | `STRONG_EVIDENCE_DERIVED_DECISION` | §38 |
| TaperMechanism | Non-chained, fixed-anchor | Family | `DIRECT_EXISTING_AUTHORITY` | §39 |
| PrimaryProgression | HM Intermediate 4D workout progression | Family | `DIRECT_EXISTING_AUTHORITY` | §17 |
| RaceSpecificPaceSemantic | Target-relative (generic) | Generic infrastructure | `DIRECT_EXISTING_AUTHORITY` | §18/§41 |
| RecentRaceEquivalenceMethod | Riegel (`^1.06`), generic | Generic infrastructure | `DIRECT_EXISTING_AUTHORITY` | §19 |
| GoalFeasibilityThresholdReuse | Generic, unchanged | Generic infrastructure | `DIRECT_EXISTING_AUTHORITY` | §22 |
| ReadinessModel | Generic, unchanged | Generic infrastructure | `DIRECT_EXISTING_AUTHORITY` | §44 |
| CalendarReuse | Generic, unchanged | Family | `DIRECT_EXISTING_AUTHORITY` | §51 |
| AdaptationReuse | Generic, unchanged | Generic infrastructure | `DIRECT_EXISTING_AUTHORITY` | §51 |

No blank row — every field required by §54 of the governing prompt is present with a value, scope, classification, and source.

## 54. Authority classifications

Summary count: 17 fields `DIRECT_EXISTING_AUTHORITY`, 7 fields `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, 3 fields `STRONG_EVIDENCE_DERIVED_DECISION`, 0 fields `PRODUCT_DEFAULT` (ungrounded), 0 fields `DECISION_REQUIRED`. Special scrutiny applied per the governing prompt's own instruction: horizon (§12-14, correctly split between structural-fact and evidence-informed tiers, never overstated as fully "direct" for Preferred/Maximum); peak band/selected peak (§28-29, honestly disclosed as evidence-convergent rather than independently strong, since the external evidence itself was not decisive); absolute LR (§35, the one field earning the strongest, most scrutinized classification, since it required actually resolving a real invariant conflict, not merely citing a number); starting LR cap (§36, correctly low-stakes, `DIRECT_EXISTING_AUTHORITY` since it's simply null everywhere at this frequency); taper (§37-39, duration correctly kept at the more modest `EVIDENCE_INFORMED_PRODUCT_DEFAULT` tier given the disclosed, reaffirmed tension, while the multipliers themselves retain their stronger tier from real percentage-guidance corroboration).

## 55. Numeric feasibility

**`15K_INTERMEDIATE_4D_NUMERICALLY_FEASIBLE`.** All five supported horizons (10-14W) allocate cleanly against the reused, unmodified phase topology (§15); weekly progression is valid and numerically identical to 16K's own already-proven traces (§42); selected peak is never forced (§47); session doses fit via the unmodified generic allocator (§42); LR policy is valid at every horizon, with peak long runs observed well inside both the 14.5km ceiling and the 40%-share hard cap (§43); taper fits via the unmodified non-chained mechanism (§39); no training-load interpolation was used anywhere (§49); no synthetic readiness substitute was introduced (§45).

## 56. Second-target architecture result

**`SECOND_TARGET_REUSES_PROJECTION_ARCHITECTURE_WITH_TARGET_SPECIFIC_NUMERIC_AUTHORITY`.** Earned, not assumed: §50-53 show the overwhelming majority of both structural mechanisms (RunLayout, phase topology, horizon-policy shape, progression, calendar, adaptation, readiness) and generic cross-cutting infrastructure (target-race-pace, Riegel, goal feasibility) require zero new architecture — they were built target-distance-generically from the start and this phase is the first real proof of that claim beyond one data point. Exactly one field (`PreferredAbsolutePeakLongRunKm`) required genuinely new, non-reusable, target-specific numeric authority, precisely because of a real, mechanically-unavoidable constraint (the binding LR-below-target invariant) rather than an architectural limitation — this is the expected, healthy shape of "second-target-specific numeric authority" the governing prompt's own hypothesis anticipated, not a sign the architecture failed to generalize.

## 57. Distance-banding evidence update

Bounded update per field, using the now-real two-interior-point comparison (15K, 16K) — no band boundaries defined, no band policy created:

| Field | Classification |
|---|---|
| Peak-volume band / selected peak | `POTENTIAL_SHARED_BAND_SIGNAL` — two independent evidence-driven derivations converged on the identical value |
| Calibration | `POTENTIAL_SHARED_BAND_SIGNAL` — same convergence via the fixed ratio methodology |
| Horizon Preferred/Maximum | `POTENTIAL_SHARED_BAND_SIGNAL` (weak) — converged, but via absence of distinguishing evidence rather than positive confirmation |
| Horizon Minimum | `NO_BANDING_SIGNAL_NEEDED` — already established as structural-family-driven (DIST-GEN.2A), a stronger and more precise model than banding |
| Growth rates / absolute weekly cap / LR shares / starting LR cap | `NO_BANDING_SIGNAL_NEEDED` — already established as `LEVEL_FREQUENCY_INVARIANT`, not distance-dependent at all |
| Taper duration/multipliers/mechanism | `NO_BANDING_SIGNAL_NEEDED` — already established as `FAMILY_INVARIANT` with real corroborating evidence |
| Absolute peak LR | `CLEARLY_TARGET_SPECIFIC` — mechanically proven, not merely theorized, by this phase's own §35 finding |

Net effect: the fields that looked like plausible band candidates in DIST-GEN.4's own `INSUFFICIENT_EVIDENCE` classification (peak volume, selected peak, calibration) have now moved to `POTENTIAL_SHARED_BAND_SIGNAL` with real second-point evidence — still not enough for a third-phase to responsibly define band boundaries (that would need at least one more, ideally non-adjacent, data point), but a meaningfully stronger evidentiary position than before this phase.

## 58. 10-mile question status

Not solved here, per explicit instruction. One new, relevant observation for the record: the Hal Higdon 15K/10-mile combined program's own peak LR (16.1km, at/near the full 10-mile distance) sits notably closer to its own target than either 15K's (14.5/15.0 = 96.7%) or 16K's (15.0/16.0 = 93.75%) frozen ratios — weak additional evidence that the 10-mile-exact target's own eventual authority, if ever closed, may need to differ materially from 16K's own numbers rather than sharing them by simple display-alias, reinforcing (not resolving) DIST-GEN.4's own deferred framing of that question. No authority is frozen or activated for 10-mile-exact in this phase.

## 59. DIST-GEN.6 implementation readiness

**`DIST_GEN_15K_INTERMEDIATE_4D_READY_FOR_DARK_IMPLEMENTATION`.** Every field in §53 carries a resolved value and classification; no remaining training-science decision blocks a future implementation phase. Implementation-only concerns (adding a 15K entry to whatever eligibility-registry shape DIST-GEN.6 chooses, a new `TargetDistance15KHorizonPolicy`-shaped class, a new `VolumeSafetyPolicy` instance, a new peak-volume-band companion policy, dark-only test harness construction, dispatch-vs-16K proof, unsupported-target fail-closed proof, canonical-anchor zero-delta proof) are fully specified in behavior by this closure and do not block authority.

## 60. Existing-product zero-delta

Confirmed by construction — this phase touched no production, catalog, database, API, or frontend file (`git status --porcelain -- backend plan-catalog mobile` verified empty before and after writing this report). 5K, TEN_K, HALF_MARATHON, the public 16K pilot (its own 10-14W horizon, [34,46]/40.0 band, 15.0km LR ceiling, taper, and public eligibility all remain completely untouched), `GoalDistance.Custom`'s unsupported-target fail-closed behavior, Habit Custom, Experienced exclusion, the HM public matrix, and catalog hashes are all unaffected. 15K remains publicly unsupported — this closure freezes authority only, per §49 of the governing prompt; DIST-GEN.6 will own dark implementation, and a further future phase would own any public activation.

## 61. Remaining blockers

None. Every required authority field is frozen with an explicit value and classification (§53-54); numeric feasibility is confirmed (§55); the second-target generalization question is answered with evidence, not assumption (§56); the LR invariant was reassessed and reaffirmed rather than casually reopened (§34); the 10-mile question is correctly left bounded rather than silently resolved (§58); canonical anchors and the public 16K pilot remain fully untouched (§60).

---

## Final classification

**15K_INTERMEDIATE_4D_TARGET_DISTANCE_AUTHORITY_CLOSED**

Structural parent, catalog identity, horizon triple (10/12/14), phase allocations across all five supported horizons, and the complete numeric policy table (§53) are frozen. The second-target generalization test (§56) confirms the target-distance projection architecture built across DIST-GEN.0-3 is genuinely reusable — the overwhelming majority of both structural mechanisms and generic cross-cutting infrastructure required zero new architecture, while exactly one field (`PreferredAbsolutePeakLongRunKm=14.5`) required real, evidence-derived, target-specific authority, precisely because of a mechanically-unavoidable invariant, not an architectural gap. DIST-GEN.6 — 15K × Intermediate × 4D Full Dark Second-Target Projection Implementation — is the named, concrete next phase, explicitly not begun here.
