# PHASE HM.15 — HALF-MARATHON ADVANCED 3D/4D/5D NUMERIC AND LEVEL AUTHORITY CLOSURE

**Type:** GOVERNANCE / PRODUCT-DECISION (no production implementation, no public activation, no Beginner/Intermediate change, no 2D/6D, no Runway/LongHorizon)
**Scope:** HALF_MARATHON × ADVANCED × {3D, 4D, 5D} × CORE 10–16W × DARK

Two genuinely novel, high-magnitude product decisions in this phase (Advanced 5D's KEY2 hardness model; the Advanced absolute-peak-LR ceiling) were escalated to the user via direct question, mirroring this engagement's established pattern of escalating real departures from conservative precedent rather than deciding them unilaterally. Both answers are recorded and applied below, not re-derived.

---

## 1. HM.12 Advanced gap recap

HM.12 established: generic HM architecture (CoreCycle, phase headroom, workout-progression schema, run-layout geometry, pace/fallback, goal-feasibility, hardness-aware calendar) is level-blind and reusable; `advanced-progression-modifier.v1.json` already exists as real, frozen, distance-generic authority (`maximumHardSessionsPerWeek=2`, `allowSecondHardStimulus=true`); no hidden deep "HM==Intermediate" coupling exists beyond the same two additive extension points already widened twice (for Beginner, HM.14); the live catalog contains zero `HALF_MARATHON`/`ADVANCED` peak-volume rows; one open structural question was flagged — whether `RUN_LAYOUT_3D`'s single-`KEY_SESSION`-slot geometry can express Advanced's two-true-hard-stimuli authority. This phase closes that question and every other Advanced numeric/product gap HM.12 enumerated.

---

## 2. Advanced modifier authority

Read `plan-catalog/catalog/progression-modifiers/advanced-progression-modifier.v1.json` directly (verbatim, re-confirmed):

```json
{ "experience": "ADVANCED", "maximumHardSessionsPerWeek": 2, "mainSetDoseMultiplier": 1.0,
  "allowGoalPaceRehearsal": true, "allowSecondHardStimulus": true }
```

`MaximumTrueHardStimuliPerWeek = 2`; second true hard stimulus **ALLOWED**. `DIRECT_EXISTING_AUTHORITY` — no new decision for the ceiling itself. As with Beginner (HM.13 §2), `mainSetDoseMultiplier=1.0` is uniform across every progression modifier in the repo and does not itself differentiate dose; `allowGoalPaceRehearsal=true` is likewise uniform and does not specifically gate `HM_PACE` (§19). The modifier's only currently load-bearing, level-differentiating field remains the hard-stimulus pair. It does not own quality-workout-content selection (`advanced-modifier.v2.json`'s `eligibleWorkouts`, a separate document), secondary-lane structural presence (workout-progression's own level-blind `lanes` schema), or fallback mechanics (generic, level-blind `requires`/`fallbackStageKey`).

**A structurally significant architectural signal, not previously stated:** Intermediate 5D needed a *bespoke, cell-specific* modifier version (`INTERMEDIATE_MODIFIER v9`/`INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3`) to raise its hard-stimulus cap from 1 to 2, scoped only to that one combination. **Advanced gets `maximumHardSessionsPerWeek=2` natively, as its baseline level authority, with no bespoke exception required.** This is real evidence the level design intends Advanced athletes to treat two true-hard stimuli as ordinary baseline capacity, not an occasional exception — directly informing §7's KEY2 decision below.

---

## 3. Advanced progression compatibility

Read `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` and `advanced-modifier.v2.json`'s `eligibleWorkouts` (`EASY_STANDARD v4`, `LONG_RUN_STANDARD v4`, `AEROBIC_STRENGTH_CONTROLLED_INTRO v3`, `THRESHOLD_TEMPO v4`+`v5`, `FARTLEK v5`, `GOAL_PACE_TEN_K v2`+`v3` — no `HM_PACE`).

| Stage | Workout candidate(s) | Classification |
|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | `EASY_STANDARD v4` | `REUSE_UNCHANGED` |
| `BUILD_FASTER_THAN_HM_INTRO` | `FARTLEK v4` | `REUSE_UNCHANGED` — note: Advanced's own list carries `v5`, not `v4`; the *stage* still references `v4` (§4's disclosed gap) |
| `BUILD_THRESHOLD_DEVELOPMENT` | `THRESHOLD_TEMPO v4` | `REUSE_UNCHANGED` — same v4/v5 note |
| `RACE_SPECIFIC_HM_INTRODUCTION` | `THRESHOLD_TEMPO v4` | `REUSE_UNCHANGED` |
| `RACE_SPECIFIC_HM_DEVELOPMENT`/`_REHEARSAL` (+fallback) | `HM_PACE v1` primary, `THRESHOLD_TEMPO v4` fallback | `REUSE_WITH_ADVANCED_DOSE_MODIFIER` — same catalog-completeness gap as Beginner, §19 |
| `TAPER_HM_ACTIVATION` (+fallback) | `HM_PACE v1` primary, `EASY_STANDARD v4` fallback | `REUSE_WITH_ADVANCED_DOSE_MODIFIER` — same |

No stage is `ADVANCED-SPECIFIC_ELIGIBILITY` or `REQUIRES_ADVANCED-SPECIFIC_STAGE_AUTHORITY` for the **primary lane**. The existing v1 single-lane document is directly reusable for Advanced 3D/4D. Advanced 5D (§7) requires the same `lanes`-based dual-KEY mechanism 5D Intermediate already introduced (`half-marathon-workout-progression.v2.json`) — reusable **mechanism**, new **content** (a distinct lane-1 stage set, since Advanced's KEY2 model differs from Intermediate's, per §7/§9).

---

## 4. Advanced dose-tier decision

`HM_21K_Claude_Handoff_and_Build_Plan.md`'s HM-LIT-17 candidate mapping: `Advanced → HIGH` (same self-labeled "closed enough for product decision/adoption, not automatically repository authority" status as Beginner's `LOW` mapping, HM.13 §4). **Decision: `PrimaryDoseTier = HIGH`.** Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

**Disclosed, real finding (stronger nuance than Beginner's own §4 gap):** unlike Beginner (which reuses the exact same `FARTLEK v4`/`THRESHOLD_TEMPO v4` versions Intermediate's base progression already uses, zero content differentiation), Advanced's `eligibleWorkouts` already references genuinely different, newer versions — `FARTLEK v5` (verified by direct diff against `v4`: widens `eligiblePhases` to include `TAPER`, sets `eligibleForLegacyDefaultResolution:false`, and **structurally removes the recovery-jog segment** between main-set and cool-down — a real, if modest, dose/structure difference, not a version-number-only bump) and `THRESHOLD_TEMPO v5` (widens `eligiblePhases` to include `FOUNDATION`, same `eligibleForLegacyDefaultResolution:false` pattern). **However, the current single-lane `HALF_MARATHON_WORKOUT_PROGRESSION v1`'s own stage `workoutCandidates` are hardcoded to reference `v4` of both workouts, not `v5`.** This means real Advanced dose differentiation already exists in the catalog but is not yet wired into HM's own progression stages — a genuine, disclosed `CAPABILITY_GAP` for HM.16 to close (a mechanical catalog-content change: point Advanced-consuming stages at `v5`, or introduce Advanced-specific stage variants), not something this governance phase invents a fix for.

---

## 5. Advanced 3D hard-structure decision

`RUN_LAYOUT_3D` (re-read directly, `plan-catalog/catalog/layouts/run-layout-3d.v1.json`): exactly `{KEY_SESSION, EASY_SUPPORT, LONG_RUN}` — **one** `KEY_SESSION` slot. Advanced's modifier permits *up to* 2 true-hard stimuli, not *exactly* 2 (the governing prompt's own critical distinction, §6). Converting `EASY_SUPPORT` or `LONG_RUN` into a second true-hard slot would require inventing new engine/layout behavior this phase is explicitly forbidden from doing, and no evidence anywhere (HM or 10K) supports a structural change to 3D's own layout at any level.

**Decision: `ADVANCED_3D_SINGLE_HARD_STRUCTURE_APPROVED`.** One true-hard stimulus (the existing KEY lane); `EASY_SUPPORT` stays easy; `LONG_RUN` stays controlled/easy (no quality-in-long-run, unchanged). Advanced's own 2-hard capacity is genuine, unused headroom at this frequency — explicitly not a quota every frequency must fill. Classification: `DIRECT_EXISTING_AUTHORITY` (forced by the existing, unmodified layout schema) combined with `PRODUCT_DEFAULT` (the "headroom, not quota" framing itself).

---

## 6. Advanced 4D hard-structure decision

`RUN_LAYOUT_4D v1` (re-read directly, confirmed the `VALIDATED` version HM's own Intermediate/Beginner 4D combinations reference, not the `DRAFT` `v2`): `{KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN}` — again exactly **one** `KEY_SESSION` slot. Same reasoning as 3D applies identically; nothing about 4D's own layout differs in the relevant respect.

**Decision: same structural policy as 3D — one true-hard stimulus, both `EASY_SUPPORT` roles stay easy, `LONG_RUN` stays controlled/easy, level's 2-hard capacity remains unused headroom at 4D.** Classification: `DIRECT_EXISTING_AUTHORITY` + `PRODUCT_DEFAULT`, same basis as §5.

---

## 7. Advanced 5D KEY2 decision — **user-escalated, decided**

`RUN_LAYOUT_5D`: `{KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN}` — **two** `KEY_SESSION` slots, the one HM frequency where Advanced's native 2-hard capacity has a structural home without inventing anything.

**User decision (escalated, see phase preamble): Advanced 5D's KEY2 becomes a genuine second true-hard session in Build/RaceSpecific**, not a light-quality-only lane like Intermediate's own KEY2. Rationale from §2's architectural signal (Advanced's modifier is natively built for 2-hard baseline capacity, unlike Intermediate's own bespoke 5D-only exception) plus the user's own explicit confirmation.

**Frozen phase-by-phase model:**

```
BeginnerSecondaryKeyByPhase (Advanced 5D):
  Foundation:    KEY2 = EASY_EQUIVALENT (no rationale for early double-hard load at any level; mirrors Intermediate's own Foundation choice)
  Build:         KEY2 = THRESHOLD_TEMPO (genuine true-hard; counts toward the 2-hard budget), fallback EASY_STANDARD
  RaceSpecific:  KEY2 = THRESHOLD_TEMPO (deliberately NOT HM_PACE — see below), fallback EASY_STANDARD
  Taper:         KEY2 = EASY_EQUIVALENT (mirrors every other level/frequency's Taper approach; doubling hard work into taper contradicts the same taper evidence base already cited, §18)
```

**Why `THRESHOLD_TEMPO`, not `HM_PACE`, for RaceSpecific KEY2:** the governing prompt's own §9 explicitly warns against "doubling race-specific work without explicit authority" and names `HM_PACE + HM_PACE` as a pattern requiring deliberate justification, not a default. KEY1 already carries the primary `HM_PACE`-based race-specific rehearsal (unchanged from every other level). A second, *different-stimulus-type* hard session (threshold, not goal-pace) avoids duplicating the same race-specific stimulus twice in one week while still giving Advanced athletes genuine additional quality load — a complementary, not duplicate, second hard stimulus.

Classification: `PRODUCT_DEFAULT` for the phase-by-phase pattern itself (a genuine, disclosed product judgment, escalated and user-confirmed — not evidence-derived in the way the LR-share reuse below is); `DIRECT_EXISTING_AUTHORITY` for the underlying `THRESHOLD_TEMPO`/`EASY_STANDARD` workout identities themselves (already-approved, already-Advanced-eligible workouts, no new content invented).

---

## 8. Advanced 5D KEY2 family eligibility

| Family | Classification | Basis |
|---|---|---|
| `EASY_STANDARD` | `SECONDARY_ALLOWED` (Foundation/Taper KEY2) | Already Advanced-eligible |
| `FARTLEK` | `SECONDARY_PROHIBITED` for KEY2 | Not chosen — Intermediate's own FARTLEK-based light-quality model was explicitly rejected in favor of a genuine hard stimulus per §7's user decision; using FARTLEK as a *true-hard* KEY2 would misclassify a light-quality family as hard |
| `THRESHOLD_TEMPO` | `SECONDARY_ALLOWED` (Build/RaceSpecific KEY2, true-hard) | §7's frozen decision |
| `HM_PACE` | `SECONDARY_PROHIBITED` for KEY2 | §7's explicit reasoning — reserved for KEY1 only, avoiding race-specific-stimulus doubling |
| Any hill/stride family | `NOT_APPLICABLE` | None found referenced by the real HM workout progression (`HM-LIT-16`'s strides/hills research was never encoded into HM's own catalog content — confirmed absent from `half-marathon-workout-progression.v1/v2.json`) |
| `LONG_RUN_STANDARD` | `SECONDARY_PROHIBITED` | Explicit invariant, both this phase's own §9 instruction and every prior HM phase: LONG content never moves into KEY roles |

`THRESHOLD_TEMPO + THRESHOLD_TEMPO` (KEY1's own Build-phase threshold work co-occurring with KEY2's own) is **not** doubly-prohibited by this decision, since KEY1 and KEY2 are structurally distinct roles at different points in the week — but note KEY1's own Build-phase stage (`BUILD_THRESHOLD_DEVELOPMENT`) and KEY2's own Build-phase stage both resolving to `THRESHOLD_TEMPO` in the same week is a real, disclosed consequence of this decision, not hidden: Build-phase Advanced 5D would carry two threshold-type sessions in one week. This is consistent with Advanced's higher dose tier (§4) and was implicit in the user's own confirmed decision to make KEY2 genuinely hard via threshold work; flagged here explicitly rather than left implicit, per this phase's own discipline.

---

## 9. Product eligibility matrix

| Cell | Status | Classification |
|---|---|---|
| **ADVANCED 3D** | **PRODUCT_ELIGIBLE** | Real, on-point 10K precedent: `Advanced3D` (10K's own Advanced 3D policy) ships today (dark) at a comparable peak volume (40.0km/week) concentrated across the identical 3-session layout — direct evidence this magnitude of session concentration is an accepted Appsel design point at Advanced level, not merely engine-capable |
| **ADVANCED 4D** | **PRODUCT_ELIGIBLE** | Session-volume simulation (§22) shows no floor violation or implausible concentration; direct 10K `Advanced4D` precedent at comparable relative volume |
| **ADVANCED 5D** | **PRODUCT_ELIGIBLE** | Session-volume simulation (§23) shows both KEY roles and both EASY roles receive plausible, non-degenerate volume even with KEY2 now genuinely hard; direct 10K `Advanced5D` precedent |

Unlike Beginner 5D (HM.13 §6, real named evidence for ineligibility), no comparable negative evidence exists for any Advanced cell — 10K's own real rollout matrix and the internal handoff document's own named rollout-target matrix (`HM_21K_Claude_Handoff_and_Build_Plan.md`, previously cited in HM.13 §6) both mark Advanced 3D/4D/5D as `TARGET`, unlike Beginner 5D's own explicit `—`.

---

## 10. Peak-band evidence and decisions

`HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2's legacy candidate table: `Advanced: 3D=36–48, 4D=46–60, 5D=54–70`. Same evidentiary argument as HM.13 §7 (Beginner): this table's `Intermediate` row was independently re-derived and frozen verbatim by later phases, raising confidence in its other rows without treating them as automatically authoritative. Confirmed via direct read of `peak-volume-bands.v9.json`: zero live `HALF_MARATHON`/`ADVANCED` rows exist — this remains legacy candidate material only.

**Decision: freeze as stated.**

| Frequency | Min | Max |
|---|---|---|
| 3D | 36 | 48 |
| 4D | 46 | 60 |
| 5D | 54 | 70 |

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (same tier and same reasoning as HM.13's Beginner bands — indirect corroboration, not a fresh independent citation search). No external research was run for this decision, for the same reasons HM.13 §7 gave (internal evidence's demonstrated track record; the alternative would require fresh multi-source convergence research disproportionate to what this specific field needs, per this phase's own §14 "only if needed" instruction) — not escalated to the user, since adopting the pre-existing candidate table (rather than inventing new numbers) is the lower-risk, non-departure choice.

---

## 11. Selected-peak convention and decisions

Beyond HM.12/HM.13's own finding (midpoint selection practiced 5+ times, not codified, one real counter-example at 10K Intermediate 5D), this phase found **additional, stronger corroboration specifically at 10K's own Advanced tier**: re-reading `VolumeSafetyPolicy.cs`'s `Advanced3D/4D/5D/6D` doc comments and cross-checking against `peak-volume-bands.v9.json`'s real `TEN_K`/`ADVANCED` rows:

| 10K Advanced policy | Band | Midpoint | Actual `ResolvedPeakReference` |
|---|---|---|---|
| `Advanced3D` | `[34,46]` | 40.0 | 40.0 (exact) |
| `Advanced4D` | `[38,52]` | 45.0 | 45.0 (exact) |
| `Advanced5D` | `[42,58]` | 50.0 | 50.0 (exact) |
| `Advanced6D` | `[42,60]` | 51.0 | 51.0 (exact) |

**4-for-4 exact matches at 10K's own Advanced tier** — a materially stronger, more consistent pattern than the Beginner tier's own 2-for-3 (HM.13 §8). Combined with HM's own Intermediate 3D/4D/5D (3-for-3) and Beginner 3D/4D (2-for-2, both frozen as exact midpoints in HM.13), the convention is now observed as consistent in **every instance except one** (10K's own `FiveDayIntermediate`/`SixDayIntermediate`, still the sole counter-example, HM.12 §10). This still falls short of an explicit, codified governance rule, but the practiced-convention argument is now considerably stronger for Advanced than it was even for Beginner.

**Decision: apply the midpoint convention for all three Advanced frequencies**, consistent with every prior HM level.

| Frequency | Band | `SelectedPeakReferenceKmPerWeek` |
|---|---|---|
| 3D | `[36,48]` | **42.0** |
| 4D | `[46,60]` | **53.0** |
| 5D | `[54,70]` | **62.0** |

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, strengthened relative to Beginner's own equivalent decision by the additional 10K Advanced-tier corroboration. Preserved as ceiling/reference, never mandatory endpoint, identical semantic to every other HM policy.

---

## 12. Weekly-growth decisions

`0.07`/`0.08` confirmed identical across every named policy in `VolumeSafetyPolicy.cs`, including all four 10K Advanced policies. **Decision: reuse unchanged.** Classification: `DIRECT_EXISTING_AUTHORITY`. No evidence anywhere supports "higher training status → faster relative growth"; the governing prompt's own §16 explicitly warns against inferring this, and none of the 10K Advanced policies deviate from the universal 7%/8%, confirming the caution was warranted (the real precedent argues against, not for, a higher rate).

---

## 13. Absolute-increase-cap decisions

Re-read `VolumeSafetyPolicy.cs`'s real 10K Advanced policies directly: `Advanced3D=2.5`, `Advanced4D=2.5`, `Advanced5D=2.5` (and `Advanced6D=2.5`) — **uniformly 2.5 across every 10K Advanced frequency, including 3D.**

This is a genuinely important, real finding: 10K's own Intermediate 3D uses `2.0` (the frequency-outlier HM.7 borrowed for HM's own Intermediate 3D), but 10K's own **Advanced** 3D uses `2.5` — a real, deliberate, same-level departure from the Intermediate-tier 3D value at 10K's own hands, not merely a coincidence of copying Intermediate. This directly means the "3D gets a lower absolute cap" pattern is Intermediate-tier-specific, not frequency-universal — it does not carry to Advanced.

**Decision:**

| Frequency | `AbsoluteWeeklyIncreaseCapKm` | Basis |
|---|---|---|
| 3D | **2.5** (NOT 2.0 — a deliberate departure from HM's own Intermediate 3D value) | Direct, on-point, same-level 10K `Advanced3D` precedent |
| 4D | 2.5 | Direct, on-point, same-level 10K `Advanced4D` precedent (matches HM's own Intermediate 4D too) |
| 5D | 2.5 | Direct, on-point, same-level 10K `Advanced5D` precedent (matches HM's own Intermediate 5D too) |

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` for all three (real, cited, same-level 10K precedent, re-verified scope per this engagement's established discipline — not a blind copy).

---

## 14. Calibration decisions

Methodology: the same cross-level ratio-reuse extension HM.13 introduced for Beginner (borrow the same-frequency Intermediate policy's own starting/peak ratio, apply to the new peak) — now found to be **the exact, real methodology 10K's own GEN.9 governance used for all four of its own Advanced policies** (re-confirmed by direct doc-comment read: `Advanced3D` reuses `ThreeDayIntermediate`'s `12/22.5=0.5333` ratio; `Advanced4D` reuses `Default`'s `24/38=0.6316`; `Advanced5D` reuses `FiveDayIntermediate`'s `26/44.5=0.5843`; `Advanced6D` reuses `SixDayIntermediate`'s own ratio) — a fourth, independent, real confirmation of this exact cross-level methodology, considerably stronger evidentiary footing than HM.13 had when first extending it for Beginner.

| Frequency | Same-frequency Intermediate ratio | × Selected peak | Rounded (0.5km grid) |
|---|---|---|---|
| 3D | `HalfMarathonIntermediate3D`: 20.0/34.0 = 0.5882 | × 42.0 = 24.71 | **24.5** |
| 4D | `HalfMarathonIntermediate4D`: 25.0/43.0 = 0.5814 | × 53.0 = 30.81 | **31.0** |
| 5D | `HalfMarathonIntermediate5D`: 29.5/51.0 = 0.5784 | × 62.0 = 35.86 | **36.0** |

Classification: `STRONG_EVIDENCE_DERIVED_DECISION` (upgraded one tier from HM.13's own `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, since the methodology itself is now independently confirmed four times at 10K alone, not merely reused once). Calibration-only, never a missing-readiness fallback — the same fail-closed guard pattern must be mechanically extended to the three new Advanced policies at implementation time (§29).

---

## 15. LR-share evidence and decisions

Re-read all four 10K Advanced policies' LR-share quadruples directly and compared against their own same-frequency Intermediate siblings:

| 10K policy | LR shares | Compared to same-frequency Intermediate |
|---|---|---|
| `Advanced3D` | `0.38/0.42/0.40/0.42` | **Identical** to `ThreeDayIntermediate` — doc comment states explicitly: *"Advanced does not get a different long-run policy at 3D"* |
| `Advanced4D` | `0.30/0.36/0.33/0.40` | **Identical** to `Default` (10K's 4D Intermediate) |
| `Advanced5D` | `0.28/0.36/0.28/0.36` | **Identical** to `FiveDayIntermediate` |
| `Advanced6D` | `0.28/0.36/0.28/0.36` | **Identical** to `SixDayIntermediate` |

**This is the strongest evidence chain in the entire phase: 10K's own governance has now confirmed, at FOUR separate frequencies (not two, as for Beginner), that Advanced's LR-share quadruple equals the same-frequency Intermediate quadruple exactly, with an explicit doc-comment statement that this is a deliberate policy ("Advanced does not get a different long-run policy"), not an accident.**

**Decision: reuse HM's own already-frozen Intermediate LR-share quadruples verbatim for Advanced, at the same frequency:**

| Frequency | Min | Max | Selected | HardCap | Source |
|---|---|---|---|---|---|
| 3D | 0.34 | 0.40 | 0.38 | 0.46 | = `HalfMarathonIntermediate3D` |
| 4D | 0.30 | 0.36 | 0.33 | 0.40 | = `HalfMarathonIntermediate4D` |
| 5D | 0.28 | 0.36 | 0.28 | 0.36 | = `HalfMarathonIntermediate5D` |

Classification: **`STRONG_EVIDENCE_DERIVED_DECISION`** — the strongest tier reached, now backed by four independent 10K confirmations plus an explicit stated policy rationale, stronger even than HM.13's own two-instance basis for Beginner. This decision was **not** escalated: it is a routine reuse of HM's own already-user-confirmed Intermediate values, not a new departure, exactly mirroring HM.13's own non-escalation reasoning for the analogous Beginner decision.

---

## 16. Absolute peak-LR decision — **user-escalated, decided**

**User decision (escalated, see phase preamble): keep `PreferredAbsolutePeakLongRunKm = 19.0 km`, the same ceiling as Intermediate — no level-based increase.** The single available Advanced-tier citation (B.A.A. Level Three, ~13–14mi/20.9–22.5km, already present in HM.1.1's own accepted evidence but explicitly not used there) was judged, by the user, insufficient on its own (single-source, versus Intermediate's own multi-source convergence) to justify raising the ceiling, and raising it risked approaching race-distance proximity without adequately strong justification.

Classification: `DIRECT_EXISTING_AUTHORITY` (HM.1.1's own 19.0km value, now explicitly broadened by user decision to cover `HALF_MARATHON × ADVANCED` in addition to `× INTERMEDIATE`, frequency-independent within Advanced, mirroring the same frequency-independence Intermediate's own ceiling already has). Preserved as ceiling, never target — same invariant. Scope: `HALF_MARATHON` (now spans both Intermediate and Advanced; Beginner remains its own, lower, 16.0km ceiling per HM.13).

---

## 17. Starting-LR-cap decision

Historical candidate: `Advanced/Experienced = 15.0 km` (`HM_21K_Claude_Handoff_and_Build_Plan.md` §4.3), explicitly self-labeled a "starting long-run candidate cap," never frozen. Re-confirming current real usage (`VolumeSafetyPolicy.cs`): only `HalfMarathonIntermediate5D` sets `StartingAbsoluteLongRunCapKm` (12.0); every other HM policy, including both new Beginner policies (HM.14), leaves it `null`.

Checked whether Advanced's own numbers create a comparable feasibility concern to the one that motivated Intermediate 5D's cap (HM.10): at Advanced 5D's own calibration start (36.0) and first-Core-week LR share (0.28), a plausible first-week LR is `36.0×0.28≈10.08→10.0km` — comfortably below the 15km historical candidate and below any floor-violation risk. No equivalent feasibility trigger was found for Advanced 3D/4D either.

**Decision: `StartingAbsoluteLongRunCap = N/A` (not set) for all three Advanced frequencies**, mirroring Intermediate 3D/4D's and both Beginner cells' own established precedent — the mechanism exists generically and remains available if a real future need arises, but nothing in this phase's own numbers motivates using it. The 15km legacy candidate is explicitly examined and explicitly not promoted. Classification: `NOT_APPLICABLE`.

---

## 18. Taper-duration decision

Re-confirmed via `HALF_MARATHON_MASTER v1`/`v2`'s own construction (no `level` field anywhere): the 2-week Taper phase is architecturally `HM_DISTANCE_WIDE_AUTHORITY`, proven identically for Beginner (HM.13 §15) and now re-verified unchanged for Advanced. **Decision: reuse 2 weeks unchanged.** Classification: `HM_DISTANCE_WIDE_AUTHORITY`. No evidence anywhere supports shortening or lengthening taper based on level.

---

## 19. Taper-multiplier decision — genuine tension disclosed, not fabricated around

Re-examined the same four evidence sources (Mujika & Padilla; Marathon Handbook; best-running-tips.com; runtothefinish.com) against Advanced's own newly-frozen peaks (42.0/53.0/62.0). **A real, previously-unencountered tension exists here that did not arise for Beginner or Intermediate:** best-running-tips.com's own named lighter-taper exception applies **above ~56 km/week**. Advanced 3D's selected peak (42.0) sits comfortably below this threshold — standard regime applies cleanly, same reasoning as every prior HM cell. Advanced 4D's selected peak (53.0) sits below it but closer than any prior cell (band maximum, 60.0, exceeds it). **Advanced 5D's own selected peak (62.0) itself exceeds the named threshold** — the first HM cell where the evidence source's own stated exception condition is genuinely triggered by the frozen selected-peak value itself, not merely by an edge of its band.

This phase does **not** invent a specific "lighter" numeric substitute: the source's own guidance is qualitative ("lighter taper") with no exact percentage given anywhere in this engagement's evidence record, and this phase's own §14/§31 explicitly forbid fabricating exact percentages from narrative guidance.

**Decision: freeze `TaperWeek1Multiplier=0.70`/`TaperWeek2Multiplier=0.43` for all three Advanced frequencies as the mechanism default**, since no real alternative exact value exists to freeze instead, and a deeper (more conservative) taper reduction is not itself unsafe — it is potentially sub-optimal for very-high-volume Advanced 5D athletes, not risky. **This is explicitly disclosed as an honest, real, non-blocking evidence gap for Advanced 5D specifically** (and, to a lesser extent, Advanced 4D near its band ceiling): the exact taper depth for HM Advanced athletes above the ~56km/week threshold is genuinely under-specified by the evidence this engagement has ever cited, and a future refinement phase should treat this as a real open item, not a settled question. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` for 3D (clean fit, same reasoning as every prior cell); `EVIDENCE_INFORMED_PRODUCT_DEFAULT` with an explicit disclosed tension for 4D/5D (reused as the only concrete number available, not because the evidence cleanly supports it at this volume). This tension does not block this phase's own closure (per §44's own instruction that policy-decision residue does not equal blockage) but is named honestly rather than smoothed over.

---

## 20. HM_PACE eligibility

Same architectural finding as HM.13 §17 (re-confirmed, not re-derived): `eligibleWorkouts` is never consulted by `DynamicCoreWorkoutBindingOrchestrator.cs`; the real runtime gate is the workout-progression stage's own level-blind `requires` conditions (`PACE_SOURCE_IN: RECENT_RACE`, `GOAL_FEASIBILITY_IN`). **Decision: Advanced HM progression should be permitted to use `HM_PACE`** under the identical evidence gate Intermediate/Beginner already use, at `RACE_SPECIFIC_HM_DEVELOPMENT`/`_REHEARSAL` and `TAPER_HM_ACTIVATION` (KEY1 only — KEY2 in 5D deliberately never uses `HM_PACE`, per §7/§8). When no evidence exists, the already-proven fallback (`THRESHOLD_TEMPO v4`/`EASY_STANDARD v4`, both already Advanced-eligible) applies unchanged. Requires, at implementation time, a new `ADVANCED_MODIFIER v3` adding `{HM_PACE, v1}` to `eligibleWorkouts` (mechanical, additive, mirrors `BEGINNER_MODIFIER v2`'s own precedent exactly). Classification: mechanism `DIRECT_EXISTING_AUTHORITY`; the declaration decision `PRODUCT_DEFAULT`, identical tier and reasoning to HM.13's own Beginner decision.

---

## 21. Goal-feasibility compatibility

Re-confirmed unchanged (HM.12 §20, HM.13 §18, re-verified again this phase): `GoalFeasibilityResolver.cs`'s thresholds carry no level parameter. **No reopening required.** An Advanced athlete's goal feasibility is evaluated identically to any other level — Advanced level does not itself make a stretch goal more feasible; only real fitness evidence does, exactly as this phase's own §25 anticipated. Classification: `GENERIC_MECHANISM_REUSE`.

---

## 22. 3D session-concentration feasibility

Hand-simulated Advanced 3D at the frozen values (peak=42.0, LR share 0.34/0.40/0.38/0.46, absolute cap 2.5km/week, calibration start 24.5):

**Peak week:** LR = `42.0×0.38=15.96→16.0km`. Remaining `42.0-16.0=26.0km` splits across KEY+EASY — comfortably above `V1ThreeDaySessionVolumeAllocationPolicy`'s `MinimumKeyKm=4.0`/`MinimumEasyKm=3.0` floors, no negative residual. A ~26km combined KEY+EASY volume across 2 sessions at Advanced level (e.g., a genuine quality KEY session in the mid-teens plus a substantial easy run) is real, plausible Advanced-level training — directly corroborated by 10K's own real `Advanced3D` shipping at a comparable 40.0km peak across the identical 3-session structure. No hidden session invented, no LR hard-cap raised, no quality-in-long-run introduced. **No blocker found.**

**Growth:** calibration start 24.5 → peak 42.0 = 17.5km total increase; at ≤2.5km/week absolute cap over ≤11 transitions, average ~1.59km/week — within both guards.

**Result: structurally and numerically feasible, `PRODUCT_ELIGIBLE`** (§9).

---

## 23. 4D session feasibility

Advanced 4D (peak=53.0, LR shares 0.30/0.36/0.33/0.40, absolute cap 2.5km/week, calibration start 31.0):

**Peak week:** LR = `53.0×0.33=17.49→17.5km`. Remaining `53.0-17.5=35.5km` splits across KEY + 2×EASY, comfortably above `V1FourDaySessionVolumeAllocationPolicy`'s `MinimumKeySessionDistanceKm=3.0`/`MinimumEasySupportDistanceKm=1.5` floors. No negative residual, no dose overload, no excessive single-session concentration (average ~11.8km per non-LONG session, reasonable for Advanced).

**Growth:** 31.0 → 53.0 = 22.0km total increase; at ≤2.5km/week over ≤11 transitions, average ~2.0km/week — within the absolute cap (closer to it than 3D's own margin, but still compliant, and the percentage guards independently cap the early-horizon rate).

**Result: feasible, `PRODUCT_ELIGIBLE`.**

---

## 24. 5D dual-key feasibility

Advanced 5D (peak=62.0, LR shares 0.28/0.36/0.28/0.36, absolute cap 2.5km/week, calibration start 36.0), now with KEY2 genuinely hard (§7):

**Peak week:** LR = `62.0×0.28=17.36→17.5km`. Remaining `62.0-17.5=44.5km` splits across KEY1+EASY1+KEY2+EASY2 (4 sessions, average ~11.1km each). Both KEY roles must receive genuine, non-degenerate quality volume under this split; neither collapses toward a minimum floor, and neither EASY role is starved. No negative residual found. The generic multi-KEY session allocator (the same one already proven for Intermediate 5D's own two-KEY-session distribution, HM.10/HM.11's own finding that this mechanism was already production-proven via "6D Core's 2K+3E+1L") requires no new code to distribute volume across two now-genuinely-hard KEY roles instead of one-hard-one-light — the allocator was never hardness-aware in the first place, only role-aware.

**Growth:** 36.0 → 62.0 = 26.0km total increase; at ≤2.5km/week over ≤11 transitions, average ~2.36km/week — within the absolute cap, with a real but not alarming margin.

**Result: feasible, `PRODUCT_ELIGIBLE`.** No weekly volume was inflated merely to make two hard workouts fit — the frozen peak (62.0) is the band-midpoint-derived value from §11, arrived at independently of this feasibility check, and the feasibility check confirms it happens to work rather than being reverse-engineered to make it work.

---

## 25. Calendar/hardness feasibility

The generic hardness-aware calendar mechanism (`CatalogWeekSkeletonCalendarMaterializer.cs`'s two-pass placement search, `CatalogCalendarSessionHardness` enum-driven, `Unknown`-fails-safe-to-hard) is confirmed level-blind by construction (HM.12 §23, re-verified this phase — no `RunningBackground`/level literal anywhere in the relevant code path). Advanced 5D's own KEY2 becoming genuinely hard (§7) is **exactly** the scenario this mechanism was designed for: both KEY roles will resolve to `TrueHard` via the existing, unmodified family-to-hardness mapping (`THRESHOLD_TEMPO` and `HM_PACE` both being non-EASY/non-LONG_RUN families, already `TrueHard` by the existing rule), and the existing same-week/cross-week true-hard spacing validation (≥48h, no adjacency) will apply to them automatically, with **zero new code** — this was explicitly anticipated and confirmed as a non-issue by HM.12 §23's own forward-looking note ("if Advanced 5D introduces harder KEY2 authority later, the calendar's existing mechanism already handles it").

For 3D/4D (single true-hard KEY, §5/§6), the calendar's existing single-KEY placement logic is unchanged from Intermediate/Beginner's own already-proven 3D/4D behavior. `PreferredDays` cardinality and `LongRunDayPreference` feasibility are generic, frequency-scoped concerns, unaffected by level. **No calendar capability gap found for any Advanced cell.**

---

## 26. Adaptation compatibility

Re-confirmed unchanged from HM.12/HM.13's own findings: the generic adaptation engine consumes structural roles (`KEY_SESSION`, `EASY_SUPPORT`, `LONG_RUN`) and completion state, with no level-literal branching found anywhere in the code paths this and prior phases inspected. Advanced 5D's two now-genuinely-hard KEY roles are both structurally `KEY_SESSION` — the generic engine treats them identically by role, which is appropriate here since both are now real true-hard stimuli of comparable (if not identical) training significance, unlike Intermediate 5D's own primary/light-secondary asymmetry (where treating both KEY roles identically would have been a more debatable simplification, not exercised differently by this phase since Intermediate's adaptation behavior is unchanged and out of scope here).

**Result: `GENERIC_ADAPTATION_SUFFICIENT`.** No new adaptation weights required or invented.

---

## 27. Exact 10–16 simulations

Using the frozen phase-allocation table (10W→2/3/3/2 ... 16W→4/5/5/2, unchanged, re-derived from the same generic resolver) and the already-proven-generic HM.5.3 mechanism (`ResolvedPeakReferenceIsSelectedCeiling=true` for all three new Advanced policies, mirroring every existing HM policy):

- **Shorter horizons (10W–13W):** fewer non-taper transitions than the `GoldenFixtureNonTaperTransitions=11` calibration point. At 10W (7 transitions), e.g. Advanced 3D starting from 24.5 at ~7% average growth reaches `24.5×1.07^7≈39.4km` — below the 42.0 selected peak; the generic mechanism does not force acceleration to chase it (same invariant re-verified at every prior HM cell). No contradiction for any of the three frequencies (4D: 31.0→~49.8 vs peak 53.0; 5D: 36.0→~57.8 vs peak 62.0 — all comfortably below their own peaks at 7 transitions).
- **Longer horizons (15W–16W):** more non-taper transitions than 11 (13 at 16W). `ResolvedPeakReferenceIsSelectedCeiling=true` caps interpolation growth at 11 transitions' worth for all three, saturating at the selected peak rather than extrapolating past it — identical mechanism already proven for every Beginner/Intermediate cell, unmodified.
- Weekly growth guards (7%/8%, 2.5km absolute for all three Advanced frequencies) hold by construction at every horizon.
- LR hard-cap (0.46/0.40/0.36 respectively) holds after rounding — generic, unmodified mechanism.
- Absolute LR ceiling (19.0km, §16): checked against each cell's own selected-peak×selected-share value — 3D: `42.0×0.38=15.96km`; 4D: `53.0×0.33=17.49km`; 5D: `62.0×0.28=17.36km`. **All three sit below 19.0km but meaningfully closer to it than any Beginner cell did (which never approached 16.0km) or Intermediate typically does** — this ceiling is a real, live consideration for Advanced, not a dormant one, though it does not actually bind at any cell's own selected values. Disclosed explicitly, not glossed over.
- Taper remains non-chained at every horizon for all three frequencies (generic, unmodified mechanism; see §19's own disclosed evidence tension on the taper *depth*, separate from the *mechanism*, which is unaffected).

No per-horizon numeric table was authored, per this phase's own instruction — the generic mechanism, already proven across seven horizons for five separate HM frequencies (three Intermediate, two Beginner), is reused unmodified.

---

## 28. Complete Advanced policy tables

### ADVANCED 3D

| Row | Value | Scope | Classification | Evidence/Source |
|---|---|---|---|---|
| ProductEligible | Yes | 3D | — | §9, §22 |
| PeakVolumeBandMin | 36.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2 |
| PeakVolumeBandMax | 48.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| SelectedPeakReference | 42.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | band midpoint, 4/4-confirmed 10K Advanced convention |
| PreferredWeeklyIncreaseRate | 0.07 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| HardWeeklyIncreaseRate | 0.08 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| AbsoluteWeeklyIncreaseCapKm | 2.5 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | 10K `Advanced3D` on-point precedent (deliberate departure from Intermediate 3D's own 2.0) |
| CalibrationStartingVolume | 24.5 km | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | 4x-confirmed cross-level ratio-reuse methodology |
| LR PreferredShareMin | 0.34 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | = `HalfMarathonIntermediate3D` |
| LR PreferredShareMax | 0.40 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR SelectedShare | 0.38 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR HardShareCap | 0.46 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| PreferredAbsolutePeakLongRunKm | 19.0 km | HM-wide (Intermediate+Advanced) | `DIRECT_EXISTING_AUTHORITY` | HM.1.1, user-confirmed no increase (§16) |
| StartingAbsoluteLongRunCap | N/A (unset) | 3D | `NOT_APPLICABLE` | mirrors Intermediate/Beginner 3D's own null |
| TaperWeek1Multiplier | 0.70 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | clean fit, well below taper-exception threshold |
| TaperWeek2Multiplier | 0.43 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| PrimaryDoseTier | HIGH | Advanced-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM-LIT-17 (disclosed `CAPABILITY_GAP`: v5 content not yet wired into stages) |
| PrimaryHardness | TrueHard | 3D | `DIRECT_EXISTING_AUTHORITY` | unchanged from every level |
| SecondaryKeyEnabled | No (3D has no KEY2) | — | `NOT_APPLICABLE` | §5 |
| SecondaryKeyFamily/Fallback/Hardness | N/A | — | — | — |
| MaximumTrueHardStimuliPerWeek | 2 (allowed), 1 (used) | Advanced-wide (allowed) / 3D (used) | `DIRECT_EXISTING_AUTHORITY` + `PRODUCT_DEFAULT` | §5, headroom not quota |
| HM_PACE eligibility | Eligible under `PACE_SOURCE_IN:RECENT_RACE` | Advanced-wide | mechanism `DIRECT_EXISTING_AUTHORITY`; declaration `PRODUCT_DEFAULT` | §20 |

### ADVANCED 4D

| Row | Value | Scope | Classification | Evidence/Source |
|---|---|---|---|---|
| ProductEligible | Yes | 4D | — | §9, §23 |
| PeakVolumeBandMin | 46.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §10 |
| PeakVolumeBandMax | 60.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| SelectedPeakReference | 53.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | band midpoint |
| PreferredWeeklyIncreaseRate | 0.07 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| HardWeeklyIncreaseRate | 0.08 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| AbsoluteWeeklyIncreaseCapKm | 2.5 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | 10K `Advanced4D` precedent (matches Intermediate 4D too) |
| CalibrationStartingVolume | 31.0 km | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | ratio-reuse methodology |
| LR PreferredShareMin | 0.30 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | = `HalfMarathonIntermediate4D` |
| LR PreferredShareMax | 0.36 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR SelectedShare | 0.33 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR HardShareCap | 0.40 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| PreferredAbsolutePeakLongRunKm | 19.0 km | HM-wide | `DIRECT_EXISTING_AUTHORITY` | §16 |
| StartingAbsoluteLongRunCap | N/A (unset) | 4D | `NOT_APPLICABLE` | mirrors Intermediate/Beginner 4D's own null |
| TaperWeek1Multiplier | 0.70 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | fits, but near band ceiling — disclosed tension, §19 |
| TaperWeek2Multiplier | 0.43 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| PrimaryDoseTier | HIGH | Advanced-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §4 |
| PrimaryHardness | TrueHard | 4D | `DIRECT_EXISTING_AUTHORITY` | unchanged |
| SecondaryKeyEnabled | No (4D has no KEY2) | — | `NOT_APPLICABLE` | §6 |
| MaximumTrueHardStimuliPerWeek | 2 (allowed), 1 (used) | Advanced-wide / 4D | `DIRECT_EXISTING_AUTHORITY` + `PRODUCT_DEFAULT` | §6 |
| HM_PACE eligibility | Eligible under evidence gate | Advanced-wide | same as 3D | §20 |

### ADVANCED 5D

| Row | Value | Scope | Classification | Evidence/Source |
|---|---|---|---|---|
| ProductEligible | Yes | 5D | — | §9, §24 |
| PeakVolumeBandMin | 54.0 km | 5D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §10 |
| PeakVolumeBandMax | 70.0 km | 5D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| SelectedPeakReference | 62.0 km | 5D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | band midpoint |
| PreferredWeeklyIncreaseRate | 0.07 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| HardWeeklyIncreaseRate | 0.08 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| AbsoluteWeeklyIncreaseCapKm | 2.5 km | 5D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | 10K `Advanced5D` precedent (matches Intermediate 5D too) |
| CalibrationStartingVolume | 36.0 km | 5D | `STRONG_EVIDENCE_DERIVED_DECISION` | ratio-reuse methodology |
| LR PreferredShareMin | 0.28 | 5D | `STRONG_EVIDENCE_DERIVED_DECISION` | = `HalfMarathonIntermediate5D` |
| LR PreferredShareMax | 0.36 | 5D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR SelectedShare | 0.28 | 5D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR HardShareCap | 0.36 | 5D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| PreferredAbsolutePeakLongRunKm | 19.0 km | HM-wide | `DIRECT_EXISTING_AUTHORITY` | §16, user-confirmed |
| StartingAbsoluteLongRunCap | N/A (unset) | 5D | `NOT_APPLICABLE` | §17 |
| TaperWeek1Multiplier | 0.70 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | **genuine disclosed evidence tension** — selected peak exceeds the named lighter-taper-exception threshold, §19 |
| TaperWeek2Multiplier | 0.43 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same tension |
| PrimaryDoseTier | HIGH | Advanced-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §4 |
| PrimaryHardness | TrueHard | 5D (KEY1) | `DIRECT_EXISTING_AUTHORITY` | unchanged |
| SecondaryKeyEnabled | **Yes** | 5D (KEY2) | `PRODUCT_DEFAULT` | §7, **user-escalated decision** |
| SecondaryKeyFamily | `THRESHOLD_TEMPO` (Build/RaceSpecific); `EASY_STANDARD` (Foundation/Taper) | 5D | `PRODUCT_DEFAULT` (pattern) + `DIRECT_EXISTING_AUTHORITY` (workout identities) | §7, §8 |
| SecondaryKeyFallback | `EASY_STANDARD` | 5D | `DIRECT_EXISTING_AUTHORITY` | §7 |
| SecondaryKeyHardness | TrueHard (Build/RaceSpecific); EasyEquivalent (Foundation/Taper) | 5D | `PRODUCT_DEFAULT` | §7, §25 |
| MaximumTrueHardStimuliPerWeek | 2 (allowed **and used**, Build/RaceSpecific) | Advanced-wide / 5D | `DIRECT_EXISTING_AUTHORITY` + `PRODUCT_DEFAULT` | §7 — the one HM cell where the level's full capacity is genuinely exercised |
| HM_PACE eligibility | KEY1 only, eligible under evidence gate; **KEY2 explicitly never uses HM_PACE** | 5D | mechanism `DIRECT_EXISTING_AUTHORITY`; KEY2 exclusion `PRODUCT_DEFAULT` | §7, §8, §20 |

---

## 29. Authority classifications

Summary across all three frequencies: `DIRECT_EXISTING_AUTHORITY` — 8 fields (growth rates ×2 per cell, absolute-peak-LR, both structural single-hard determinations' underlying-layout component, starting-LR-cap N/A determinations); `STRONG_EVIDENCE_DERIVED_DECISION` — 15 fields (all three LR-share quadruples reused verbatim, now the strongest-evidenced field family in the whole HM engagement at four independent 10K confirmations; all three calibration starts, upgraded a tier from Beginner's own equivalent decision on the strength of the same 4x-confirmed 10K methodology); `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — the remainder (peak bands, selected peaks, absolute caps, taper multipliers, dose tier); `PRODUCT_DEFAULT` — confined to the Advanced 5D KEY2 phase-by-phase pattern itself and the "headroom not quota" framing for 3D/4D, both explicitly disclosed as genuine product judgment (one user-escalated and confirmed, the others routine). **No field is classified `EVIDENCE_BACKED`**, consistent with this engagement's unbroken discipline.

---

## 30. Remaining capability gaps

1. **Dose-tier content gap (§4):** Advanced-preferred workout versions (`FARTLEK v5`, `THRESHOLD_TEMPO v5`) already exist and are already declared Advanced-eligible, but the current single-lane `HALF_MARATHON_WORKOUT_PROGRESSION v1`'s own stage `workoutCandidates` are hardcoded to `v4` — real Advanced dose differentiation exists in the catalog but is not yet wired into HM's own progression stages. Non-blocking for dark implementation (v4 remains a safe, already-proven default), but a real, disclosed item for HM.16.
2. **Taper-depth evidence gap for Advanced 4D/5D (§19):** the evidence sources this engagement has ever cited for taper reduction name a "lighter taper" exception above ~56km/week without giving an exact alternative percentage. Advanced 5D's own selected peak (62.0) exceeds this threshold; Advanced 4D's own band ceiling (60.0) does too, though its selected peak (53.0) does not. No new number was invented to fill this gap — 0.70/0.43 is retained as the safe (if possibly sub-optimal) default. A future phase with either fresh, permitted external research or new internal evidence could refine this.
3. **`eligibleWorkouts`/binder consistency (§20, mirrors HM.13's own §26 item):** whether an undeclared workout reference causes a hard catalog-load failure or merely an incomplete bundle at packaging time was not independently re-verified this phase (same open item HM.13 disclosed for Beginner, still open, now applicable to Advanced's own eventual `ADVANCED_MODIFIER v3`).
4. **Mechanical, non-evidentiary, implementation-time tasks** (do not block this phase's closure): (a) extend `CatalogVolumeAndLongRunPlanner`'s missing/zero-readiness fail-closed guard to the three new `HalfMarathonAdvanced3D/4D/5D` instances; (b) new `ForHalfMarathonAdvancedDaysPerWeek(int)` dispatcher, mirroring the existing Beginner/Intermediate sibling-method shape; (c) new dark-only `V1CatalogPilotIdentityPolicy` candidate keys/tuple arms for all three frequencies; (d) new `ADVANCED_MODIFIER v3` catalog version (§20); (e) a new `half-marathon-workout-progression v3` (or equivalent) introducing Advanced 5D's own lane-1 stage content (`BUILD_HM_SECONDARY_THRESHOLD`-style stages, mirroring 5D Intermediate's own lane-1 precedent shape but with different, genuinely-hard content per §7/§8) — 5D specifically needs new progression content, unlike 3D/4D which reuse v1 verbatim; (f) three new combination files, two (3D/4D) referencing existing unmodified master/layout/progression documents, one (5D) referencing the new lane-bearing progression version.

None of these require new engine behavior beyond what is explicitly named here.

---

## 31. HM.16 implementation readiness

```
HM_ADVANCED_3D_READY_FOR_DARK_IMPLEMENTATION
HM_ADVANCED_4D_READY_FOR_DARK_IMPLEMENTATION
HM_ADVANCED_5D_READY_FOR_DARK_IMPLEMENTATION
```

All three Advanced frequencies are product-eligible and numerically feasible — unlike Beginner, no cell is deliberately excluded from V1 scope.

---

## 32. Recommended implementation shape

Mirrors HM.11's own precedent (the last HM phase requiring genuine new progression content, not just numeric policy) for 5D, and HM.14's own precedent (zero new progression content needed) for 3D/4D — **no new generator classes anywhere**:

- Three new named `VolumeSafetyPolicy` instances (`HalfMarathonAdvanced3D`, `HalfMarathonAdvanced4D`, `HalfMarathonAdvanced5D`), encoding exactly this report's frozen tables, plus a corresponding `HalfMarathonAdvanced{3D,4D,5D}TaperVolumePolicy` triple.
- One new `ADVANCED_MODIFIER v3` catalog version (adds `HM_PACE v1` to `eligibleWorkouts`; everything else unchanged from v2).
- Zero new `HALF_MARATHON_MASTER` version needed for any frequency (all three reuse the existing v1/v2 pair verbatim, same as every prior HM level).
- Zero new workout-progression version for 3D/4D (reuse v1 verbatim, no second lane needed, mirroring Beginner's own precedent exactly). **One new workout-progression version for 5D** (a `lanes`-bearing document mirroring `half-marathon-workout-progression.v2.json`'s own shape, but with Advanced-specific lane-1 content per §7/§8's frozen `THRESHOLD_TEMPO`-based genuine-hard model, distinct from Intermediate 5D's own FARTLEK-based light-quality lane-1 content).
- Three new dark-only combination files, two referencing existing unmodified documents (3D/4D), one referencing the new Advanced-specific progression version (5D).
- A `ForHalfMarathonAdvancedDaysPerWeek(int)` dispatcher (new sibling, mirrors the existing Beginner/Intermediate shape exactly).
- Three new dark-only candidate-key constants + tuple arms in `V1CatalogPilotIdentityPolicy`, additive, mirroring the existing pattern exactly.
- Extension of the existing missing/zero-readiness fail-closed guard to the three new policies (mechanical).

No `Advanced3DGenerator`/`Advanced5DGenerator`-style per-cell engine. Public/Runway/LongHorizon remain untouched and closed. No Beginner or Intermediate authority, code, or catalog content is touched anywhere in this recommended shape.

---

## FINAL CLASSIFICATION

```
HM_ADVANCED_3D_4D_5D_AUTHORITY_CLOSED
```

All required criteria are met: the Advanced V1 frequency eligibility matrix is frozen (all three `PRODUCT_ELIGIBLE`, none deliberately excluded); every eligible frequency has a frozen peak-volume band, selected peak, weekly-progression authority, absolute weekly increase cap, resolved calibration authority (via a now four-times-confirmed methodology), frozen LR-share quadruple (the strongest-evidenced field family in this engagement), frozen absolute-peak-LR authority (user-confirmed, unchanged from Intermediate), resolved starting-LR-cap (explicitly N/A), and frozen taper duration/multipliers (with one honestly disclosed, non-blocking evidence tension for higher-volume cells); Advanced dose behavior is frozen (HIGH, with a disclosed content-wiring gap); `HM_PACE` eligibility is frozen; Advanced 3D and 4D hard-session structure are frozen (single-hard, headroom not quota); Advanced 5D KEY2 behavior is frozen (user-escalated, genuine second true-hard via `THRESHOLD_TEMPO`); maximum hard-stimulus semantics are frozen per frequency; all three eligible cells are proven feasible across 10–16W by hand-simulation against real session-allocation floors; no unsupported historical value was silently promoted (the 15km starting-cap candidate was explicitly examined and declined); no new engine behavior was invented anywhere (every implementation-time task is explicitly named in §30/§32); no Beginner or Intermediate authority, code, or catalog artifact was touched or reopened.
