# PHASE HM.13 — HALF-MARATHON BEGINNER 3D/4D/5D NUMERIC AND LEVEL AUTHORITY CLOSURE

**Type:** GOVERNANCE / PRODUCT-DECISION (no production implementation, no public activation, no Advanced work, no 2D/6D, no Runway/LongHorizon)
**Scope:** HALF_MARATHON × BEGINNER × {3D, 4D, 5D} × CORE 10–16W × DARK

---

## 1. HM.12 Beginner gap recap

HM.12 (`PHASE_HM_12_...md`) established, by direct code/catalog read: generic HM architecture (CoreCycle, phase headroom, workout-progression schema, run-layout geometry, pace/fallback, goal-feasibility, calendar) is level-blind and reusable as-is; `beginner-progression-modifier.v1.json` already exists as real, frozen, distance-generic authority (`maximumHardSessionsPerWeek=1`, `allowSecondHardStimulus=false`); no hidden deep "HM==Intermediate" coupling exists beyond two additive extension points; and the live catalog contains **zero** `HALF_MARATHON`/Beginner rows anywhere — the historical Beginner band table is legacy handoff material only. This phase closes exactly the numeric/product gap set HM.12 enumerated in its Beginner ledger (§27), using the same evidence-basis discipline HM.7/HM.9/HM.10 established.

---

## 2. Beginner modifier authority

Read `plan-catalog/catalog/progression-modifiers/beginner-progression-modifier.v1.json` directly (verbatim, re-confirmed this phase):

```json
{ "experience": "NEW", "maximumHardSessionsPerWeek": 1, "mainSetDoseMultiplier": 1.0,
  "allowGoalPaceRehearsal": true, "allowSecondHardStimulus": false }
```

Confirmed: `MaximumTrueHardStimuliPerWeek = 1`; second true hard stimulus **NOT ALLOWED**. This is `DIRECT_EXISTING_AUTHORITY` — no new decision needed for the hard-stimulus ceiling itself.

Does it also own dose tier / quality eligibility / progression scaling / secondary-lane behavior / fallback behavior? Inspecting each field: `mainSetDoseMultiplier=1.0` is a **numeric scalar**, uniformly `1.0` across every progression modifier in the repo (Beginner, both Intermediate versions, Advanced) — it does not currently differentiate dose by level at all (see §5's `CAPABILITY_GAP` finding). `allowGoalPaceRehearsal=true` is uniformly `true` everywhere too — it does not gate HM_PACE specifically (see §17). So the modifier's **only currently load-bearing, level-differentiating field** is the hard-stimulus pair (`maximumHardSessionsPerWeek`/`allowSecondHardStimulus`). It does **not** own quality-workout-content selection (that is `beginner-modifier.v1.json`'s `eligibleWorkouts` list, a separate document — see §17), secondary-lane structural presence (that is the workout-progression's own `lanes` schema, level-blind per HM.12 §6), or fallback mechanics (generic, level-blind `requires`/`fallbackStageKey`, HM.12 §19).

---

## 3. Beginner progression compatibility

Read `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` (single-lane, used today by 3D/4D) and `beginner-modifier.v1.json`'s `eligibleWorkouts` list (`EASY_STANDARD v4`, `LONG_RUN_STANDARD v4`, `FARTLEK v4`, `THRESHOLD_TEMPO v4`, `GOAL_PACE_TEN_K v2` — no `HM_PACE`).

| Stage | Workout candidate(s) | Classification |
|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | `EASY_STANDARD v4` | `REUSE_UNCHANGED` — eligible |
| `BUILD_FASTER_THAN_HM_INTRO` | `FARTLEK v4` | `REUSE_UNCHANGED` — eligible (Beginner's own list carries `v4`, the exact version this stage wants) |
| `BUILD_THRESHOLD_DEVELOPMENT` | `THRESHOLD_TEMPO v4` | `REUSE_UNCHANGED` — eligible |
| `RACE_SPECIFIC_HM_INTRODUCTION` | `THRESHOLD_TEMPO v4` | `REUSE_UNCHANGED` — eligible |
| `RACE_SPECIFIC_HM_DEVELOPMENT`/`_REHEARSAL` (+fallback) | `HM_PACE v1` primary, `THRESHOLD_TEMPO v4` fallback | `REUSE_WITH_BEGINNER_DOSE_MODIFIER` — see §17: mechanism already level-blind, catalog-completeness declaration recommended |
| `TAPER_HM_ACTIVATION` (+fallback) | `HM_PACE v1` primary, `EASY_STANDARD v4` fallback | `REUSE_WITH_BEGINNER_DOSE_MODIFIER` — same |

**No stage is `BEGINNER_INELIGIBLE` or `REQUIRES_BEGINNER-SPECIFIC_STAGE_AUTHORITY`.** The existing v1 (single-lane) `HALF_MARATHON_WORKOUT_PROGRESSION` is directly reusable, unmodified, for Beginner 3D/4D. No new progression document is needed.

---

## 4. Beginner dose-tier decision

Read `HM_21K_Claude_Handoff_and_Build_Plan.md` §"HM-LIT-17 — Workout Dose & Session Load" (lines 320-369) directly. This section is real internal research synthesis, explicitly self-labeled: *"considered closed enough for product decision/adoption. They are not automatically repository authority until explicitly encoded."* Its own candidate level→tier mapping: `Beginner/New → LOW`, `Intermediate → STANDARD`, `Advanced → HIGH`.

**Decision: `PrimaryDoseTier = LOW` for Beginner.** Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (real, on-point internal synthesis; not yet encoded as a runtime field anywhere).

**Honest disclosure — a real `CAPABILITY_GAP`, verified by direct code/catalog read, not assumed:** `mainSetDoseMultiplier` is `1.0` on **every** progression modifier in the repo (`beginner-progression-modifier.v1.json`, both `intermediate-progression-modifier.v3/v4.json`, `advanced-progression-modifier.v1.json`) — there is currently **no** live numeric mechanism differentiating dose intensity/duration by level at all. Beginner's own `eligibleWorkouts` list references the exact same workout-definition versions (`FARTLEK v4`, `THRESHOLD_TEMPO v4`) that HM's own primary Intermediate progression already binds today — i.e., **the catalog currently expresses zero content-level dose difference between Beginner and Intermediate for these two workouts.** The real, currently-active mechanism that keeps Beginner's training load lower is structural (fewer true-hard stimuli permitted per week, per §2), not a reduced-intensity variant of the same workout. This is disclosed as a genuine, non-blocking gap: it does not prevent dark implementation (the hard-stimulus-cap mechanism is sufficient and already proven), but the "LOW dose tier" label should not be read as "a lighter version of `FARTLEK`/`THRESHOLD_TEMPO` exists" — it doesn't yet.

---

## 5. 5D KEY2 decision (conditional — see §6 for eligibility)

Per this phase's own instruction ("No FARTLEK-as-light-quality reuse from Intermediate unless explicit Beginner authority permits it") and §2's confirmed `allowSecondHardStimulus=false`: **no approved Beginner authority exists that would permit any true-hard or light-quality second KEY.** Therefore, if HM Beginner 5D were ever implemented, the only authority-consistent answer is:

```
BeginnerSecondaryKeyByPhase:
  Foundation:    KEY2 = EASY_EQUIVALENT
  Build:         KEY2 = EASY_EQUIVALENT
  RaceSpecific:  KEY2 = EASY_EQUIVALENT
  Taper:         KEY2 = EASY_EQUIVALENT

MaximumTrueHardStimuliPerWeek = 1
```

This is **not** freed from the governing prompt's own suggested shape by assumption — it is the only shape consistent with the already-frozen `allowSecondHardStimulus=false` constraint (§2), a direct, forced consequence, not a fresh evidence-derived choice. Classification: `DIRECT_EXISTING_AUTHORITY` (the constraint) combined with `PRODUCT_DEFAULT` (the "all-phases EASY_EQUIVALENT, no exception" conclusion, since no alternative is permitted). **This entire section is rendered moot for V1 by §6's product-ineligibility finding** — recorded here only for completeness/future reference, per this phase's own instruction not to leave it implicit.

---

## 6. 5D product-eligibility decision

Two independent, real, on-point pieces of internal evidence converge:

1. **`HM_21K_Claude_Handoff_and_Build_Plan.md` §"WAVE HM-E"** (lines 925-951) contains the project's own **explicit, named rollout-target matrix**:
   ```
                 3D      4D      5D
   Beginner      TARGET  TARGET  —
   Intermediate  TARGET  TARGET  TARGET
   Advanced      TARGET  TARGET  TARGET
   ```
   Beginner 5D is marked `—` (not targeted) **by name**, explicitly and deliberately, while Intermediate and Advanced both include 5D. §8's separate "Recommended HM rollout order" list (lines 1007-1019) reinforces this: item 4 is literally `"Beginner 3D + 4D"` — 5D is absent — while item 5 (`"Advanced 3D + 4D + 5D"`) and item 3 (`"Intermediate 3D + 5D"`) both include it.
2. **10K's own real, shipped rollout** (confirmed directly in HM.12 §4 and re-verified this phase via `VolumeSafetyPolicy.ForBeginnerDaysPerWeek`, §11 below): Beginner never received a 5D (or 6D) cell at all — not deferred, never proposed.

Both sources are independent (one is HM-specific planning material predating any of this engagement's own HM phases; the other is 10K's own real production history) and point the same direction. Combined with the structural consequence in §5 (Beginner 5D's second KEY role would carry zero quality content in every phase — an entire weekly session reduced to undifferentiated easy volume, with no compensating benefit the architecture can currently express) and this engagement's own product-simplicity precedent, this is confident, on-point, real evidence — not merely 10K-pattern-matching.

**Decision: `BEGINNER_5D_PRODUCT_INELIGIBLE`.**

This is a confident ineligibility finding (not `BEGINNER_5D_PRODUCT_DECISION_REQUIRED`) precisely because real, named, on-point internal evidence exists — this is not an absence-of-evidence default.

---

## 7. Peak-band evidence and decisions

Read `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2 (lines 160-180) directly. The candidate table:

```
                3D       4D       5D
Beginner/New    22–32    28–38    32–44
```

is explicitly self-labeled *"product envelopes, not compulsory targets"* — not frozen authority (confirmed by HM.12: zero matching rows in the live `peak-volume-bands.v9.json`).

**Evidence-quality argument for adoption (not blind promotion):** this exact same table's `Intermediate` row (`28–40 / 36–50 / 44–58`) was **independently re-derived and frozen** by later phases (HM.1.5/HM.4 for 4D, HM.7 for 3D, HM.10 for 5D) through entirely separate evidence chains, and the values that emerged **exactly match** this table's own original Intermediate candidates, now confirmed live in `peak-volume-bands.v9.json`. This is real, verifiable, in-repo corroboration that whoever authored this table's methodology produced numbers that survived independent re-derivation — raising, but not eliminating, confidence in the table's Beginner row, which has not yet been independently re-derived the same way.

**Decision: freeze the Beginner row for 3D and 4D as stated** (5D is moot per §6):

| Frequency | Min | Max |
|---|---|---|
| 3D | 22 | 32 |
| 4D | 28 | 38 |

Classification: **`EVIDENCE_INFORMED_PRODUCT_DEFAULT`** (not `STRONG_EVIDENCE_DERIVED_DECISION` — the corroboration is indirect/structural, not a fresh independent citation search for Beginner specifically; not `PRODUCT_DEFAULT` either, since the corroboration argument above is real and citable, not arbitrary). No new external web research was run this phase: per this phase's own §9 instruction ("use existing Appsel synthesis/evidence first... if insufficient, external research is permitted narrowly"), the internal corroboration argument above was judged sufficient — external search was not run given the (a) internal evidence's demonstrated track record and (b) this being the conservative end of HM's own level ladder, where a plausible-but-imperfect band poses materially lower safety risk than an under-specified Advanced/high-volume band would.

This is not escalated to the user via a decision question: adopting the lowest, most-conservative band in HM's own level ladder is the expected, non-departure direction (mirroring this engagement's own established distinction between HM.7's escalated 3D LR-share *increase* and HM.10's non-escalated 5D LR-share *decrease* — a new-but-conservative-direction number does not require the same sign-off a genuine departure does).

---

## 8. Selected-peak convention/decisions

Per HM.12 §10's own finding, band-midpoint selection is a *practiced* pattern (5 repetitions across the repo) but **not codified as a rule**, and is directly contradicted by at least one real counter-example (10K's `FiveDayIntermediate`=44.5 against a `[36,50]` band whose midpoint is 43.0). This phase additionally checked all three of 10K's own real **Beginner**-tier selected peaks against their own bands:

| 10K Beginner policy | Band | Midpoint | Actual `ResolvedPeakReference` |
|---|---|---|---|
| `Beginner2D` | `[16,22]` | 19.0 | 19.0 (exact match) |
| `ThreeDayBeginner` | `[16,20]` | 18.0 | **17.0** (one increment below midpoint) |
| `BeginnerFourDay` | `[18,24]` | 21.0 | 21.0 (exact match) |

Two of three match the midpoint exactly; one (3D) deviates by one 0.5km-grid increment toward the conservative side, with no doc-comment rationale recorded for the deviation. This is too weak and inconsistent a pattern (2-for-3, no stated rationale for the one deviation) to establish "Beginner should lean below midpoint" as a rule. **Decision: apply the same midpoint-selection methodology HM's own Intermediate cells already used, for internal self-consistency, while explicitly retaining HM.12's own honest caveat** (practiced, not codified; a future phase remains free to deviate with real evidence).

| Frequency | Band | `SelectedPeakReferenceKmPerWeek` |
|---|---|---|
| 3D | `[22,32]` | **27.0** (exact midpoint) |
| 4D | `[28,38]` | **33.0** (exact midpoint) |

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (practiced-convention reuse, explicitly not a codified rule — consistent with HM.12's own framing, not overstated as `STRONG_EVIDENCE_DERIVED_DECISION`).

`SelectedPeakReference` is preserved throughout as a **selected ceiling/reference**, not a mandatory endpoint — identical semantic to every existing HM/10K policy (`ResolvedPeakReferenceIsSelectedCeiling=true` will be set for both, mirroring Intermediate, per §22).

---

## 9. Weekly-growth decisions

`PreferredMaxWeeklyIncreaseRatio=0.07`/`HardMaxWeeklyIncreaseRatio=0.08` are confirmed identical across **every** named policy in `VolumeSafetyPolicy.cs` — all 10K policies (Beginner2D/3D/4D included) and all three HM Intermediate policies. This is the single most level-and-distance-invariant numeric constant in the entire catalog (re-confirmed directly this phase, matching HM.12 §11's own finding).

**Decision: reuse `0.07`/`0.08` unchanged for HM Beginner 3D/4D.** Classification: `DIRECT_EXISTING_AUTHORITY`.

---

## 10. Absolute-growth-cap decisions

Real, on-point, **same-level** 10K precedent (re-read directly this phase, `VolumeSafetyPolicy.cs:642-670`):

| 10K Beginner policy | `AbsoluteWeeklyIncrementCapKm` |
|---|---|
| `ThreeDayBeginner` | 2.0 |
| `BeginnerFourDay` | 2.5 |

These exactly match HM's own already-frozen Intermediate values at the same frequencies (3D=2.0, 4D=2.5) — a second, independent confirmation (beyond HM.7's own 3D-outlier reasoning) that this field is frequency-owned, not simply level-scaled, and that the 2.0/2.5 split holds at Beginner too, not only at Intermediate.

**Decision:** Beginner 3D = **2.0 km**, Beginner 4D = **2.5 km**. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (direct, on-point, same-level 10K precedent, re-verified — not a blind copy of HM's own Intermediate value, though it happens to coincide).

---

## 11. Calibration decisions

`GoldenFixtureStartingVolumeKm` is genuinely load-bearing for HM's own volume planner (confirmed via `CatalogVolumeAndLongRunPlanner`'s calibration-interpolation branch, which HM's 3D/4D/5D Intermediate policies all use substantively — unlike 10K's `ThreeDayBeginner`, whose own doc comment discloses this field is "unused by this planner's 3D sequential-growth branch," a **different, 10K-specific mechanism** not shared by HM). So this field cannot be treated as decorative for HM Beginner; a real value is required.

**Methodology:** reuse HM's own already-established ratio-reuse convention (HM.7 borrowed `HalfMarathonIntermediate4D`'s own `25.0/43.0=0.5814` starting/peak ratio for 3D; HM.10 reused the same ratio for 5D) — extended here, for the first time, **across levels** rather than only across frequencies within the same level. This is disclosed explicitly as a methodology extension, not a previously-tested pattern:

| Frequency | Selected peak | × 0.5814 ratio | Rounded (0.5km grid) |
|---|---|---|---|
| 3D | 27.0 | 15.70 | **15.5** |
| 4D | 33.0 | 19.19 | **19.0** |

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (established methodology, honestly disclosed as a cross-level extension). This is calibration-only — never a missing-readiness fallback, never an explicit-zero substitute, never a "Beginner minimum readiness" default (the same fail-closed guard HM's `CatalogVolumeAndLongRunPlanner` already applies to `HalfMarathonIntermediate3D/4D/5D` must be extended, mechanically, to the new Beginner policies at implementation time — flagged in §26, not a new capability).

---

## 12. LR-share evidence and decisions

This is the single strongest piece of directly-on-point internal methodology evidence found in this entire phase. Re-reading `VolumeSafetyPolicy.cs`'s real 10K Beginner policies directly:

| 10K policy | LR shares (Min/Max/Selected/HardCap) | Compared to same-frequency 10K Intermediate |
|---|---|---|
| `ThreeDayBeginner` | `0.38/0.42/0.40/0.42` | **Identical** to `ThreeDayIntermediate`'s own values — doc comment states explicitly: *"Long-run shares... reused verbatim, unchanged from `ThreeDayIntermediate`"* |
| `BeginnerFourDay` | `0.30/0.36/0.33/0.40` | **Identical** to `Default`'s (10K's own 4D Intermediate policy) own values |

10K's own governance has **twice**, at two different frequencies, explicitly decided that Beginner's LR-share quadruple equals the same-frequency Intermediate quadruple exactly — not derived by a level-adjustment formula, not scaled down, not independently re-evidenced, but reused verbatim. This directly answers the very methodology question HM.12 §13 flagged as unsettled (2D's own precedent showed LR-share can be frequency-owned rather than level-owned; these two 3D/4D data points now show that when it *is* frequency-owned, level produces **zero** shift within that frequency).

**Decision: reuse HM's own already-frozen Intermediate LR-share quadruples verbatim for Beginner, at the same frequency:**

| Frequency | Min | Max | Selected | HardCap | Source |
|---|---|---|---|---|---|
| 3D | 0.34 | 0.40 | 0.38 | 0.46 | = `HalfMarathonIntermediate3D`'s own frozen values |
| 4D | 0.30 | 0.36 | 0.33 | 0.40 | = `HalfMarathonIntermediate4D`'s own frozen values |

Classification: **`STRONG_EVIDENCE_DERIVED_DECISION`** — this is the only field in this entire phase earning that tier, because the derivation methodology itself (not just the destination value) is independently, twice-confirmed real internal precedent, not a single analogy. `Selected` remains ordinary routine behavior; `HardCap` remains the safety boundary, never the routine target — same semantic distinction HM's own Intermediate policies already preserve.

This decision was **not** escalated to the user: it reuses HM's own already-user-confirmed Intermediate numbers verbatim (HM.7's 3D share was itself already escalated and decided once; reusing it here is not a new departure).

---

## 13. Absolute peak-LR decision

`PreferredAbsolutePeakLongRunKm=19.0` was frozen in `HM.1.1` **explicitly and only** for `HALF_MARATHON × INTERMEDIATE × 4D` (re-read directly this phase — `PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md` §2.4: *"Explicitly NOT yet decided... Beginner×3D/4D... B.A.A.'s own level-differentiated data (Level One ~9–10 mi, Level Three 13–14 mi) demonstrates real level-based variance exists — do not assume 19.0 km transfers to other levels/frequencies."*). HM.7/HM.10 only ever broadened this to *other Intermediate frequencies*, never across levels.

HM.1.1's own accepted evidence base already contains a real, on-point, Beginner-tier data point it explicitly declined to use at the time: **B.A.A. Level One (the beginner-equivalent tier in that same source) ≈ 9–10 miles ≈ 14.5–16.1 km.** This phase reuses that already-accepted citation directly (no new external research run — the evidence already exists inside a closed phase's own record) and applies the identical selection convention HM.1.1 itself used for 19.0 km ("upper end of the observed convergence range, conservative relative to the full range"):

**Decision: `PreferredAbsolutePeakLongRunKm = 16.0 km` for Beginner** (upper end of the 14.5–16.1 km B.A.A. Level One range, rounded to the 0.5-grid — mirroring exactly how 19.0, not 19.3, was chosen for Intermediate). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (same evidentiary tier as the original 19.0 decision — reusing already-accepted evidence, not new causal proof). Scope: `HALF_MARATHON × BEGINNER`, frequency-independent within Beginner (mirroring how 19.0 became frequency-independent within Intermediate). Preserved as a **ceiling, never a target** — identical invariant language to HM.1.1's own.

---

## 14. Starting-LR-cap decision

The historical `Beginner/New = 8.0 km` candidate (`HM_21K_Claude_Handoff_and_Build_Plan.md` §4.3, line 187) is explicitly self-labeled a *"starting long-run candidate cap,"* never frozen. Re-confirming HM's own real current usage of this mechanism (`VolumeSafetyPolicy.cs`, `StartingAbsoluteLongRunCapKm`): **only `HalfMarathonIntermediate5D` sets this field; `HalfMarathonIntermediate3D`/`4D` leave it `null`.** The mechanism exists generically (opt-in, gates only the first Core week) but has never been needed at 3D/4D for any level to date.

**Decision: `StartingAbsoluteLongRunCap = N/A` (not set) for Beginner 3D and 4D**, mirroring Intermediate 3D/4D's own established precedent exactly — no structural reason distinguishes Beginner 3D/4D from Intermediate 3D/4D on this specific field. The 8.0 km legacy candidate is **not silently promoted**; it remains disclosed, unadopted `LEGACY_CANDIDATE` material. Classification: `NOT_APPLICABLE`. The user's real recent-longest-run evidence remains fully authoritative and untouched by this decision — this cap, when unset, simply never engages; it was never a default starting distance for any policy that has it, let alone one that doesn't.

---

## 15. Taper-duration scope

Re-confirmed directly (`plan-catalog/catalog/templates/half-marathon-master.v1.json`/`v2.json`, both byte-identical on this point): the 2-week Taper phase (`minimumWeeks:2, preferredWeeks:2, maximumWeeks:2, isCompressionProtected:true`) is carried entirely by `HALF_MARATHON_MASTER`, which has no `level` field anywhere in its schema (HM.12 §5/§17's own architectural proof, re-verified this phase). **Decision: reuse 2 weeks unchanged for Beginner.** Classification: `HM_DISTANCE_WIDE_AUTHORITY`, architecturally proven, not merely assumed. No shortening is applied merely because Beginner's peak volume is lower — no real evidence in this phase supports that link, and the master template's own construction makes level-based taper-duration variance impossible without a new template field, which nothing here motivates adding.

---

## 16. Taper-multiplier decisions

Re-read all four evidence sources cited by `HalfMarathonIntermediate3D/4D/5DTaperVolumePolicy`'s own doc comments (Mujika & Padilla meta-analyses; Marathon Handbook's HM taper guide; best-running-tips.com; runtothefinish.com). Per HM.7's own re-verification (re-confirmed directly this phase): none of these four sources are frequency-scoped, and best-running-tips.com's own named lighter-taper exception applies only **above ~56 km/week** — a volume threshold, not a level distinction. Beginner's own new selected peaks (27.0/33.0 km) sit **further** below that threshold than any HM Intermediate cell does (34.0–51.0 km) — so if the standard 40–60% reduction regime already applied cleanly to Intermediate's own range, it applies at least as cleanly here.

The one genuine, not-yet-tested question (raised honestly in HM.12 §18): Mujika & Padilla's literature is drawn substantially from trained/competitive athlete populations, and its direct applicability to a true beginner's less-adapted physiology has never been explicitly re-examined in any HM phase. This phase examined it and found **no internal or already-cited external evidence suggesting a different reduction magnitude for less-trained athletes** — taper literature converges on volume/proximity as the operative variable, not training history, and no source in this engagement's own accepted evidence base contradicts that. Absent evidence for a different number, inventing one would violate this phase's own no-invention discipline.

**Decision: reuse `TaperWeek1Multiplier=0.70`, `TaperWeek2Multiplier=0.43` unchanged for Beginner 3D/4D.** Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (reused with re-verified, and in this instance *strengthened*, scope — Beginner's lower volumes sit more comfortably inside the standard-regime range than Intermediate's own do). Mechanism (non-chained, fixed pre-taper anchor) is unchanged, generic, already proven.

---

## 17. HM_PACE eligibility

**A real, previously-undisclosed finding, verified by direct code read this phase:** `beginner-modifier.v1.json`'s `eligibleWorkouts` list is **not** consulted anywhere in the actual runtime stage/workout-binding logic. Grepped `DynamicCoreWorkoutBindingOrchestrator.cs` in full for any reference to `eligibleWorkouts`/`EligibleWorkouts`: none exists. The only consumer found repo-wide is `PlanCatalogBundleLoader.cs:85` (`ReadEligibleWorkouts`), which reads the list purely as a **declared-dependency inventory** (used for catalog packaging/bundle-content bookkeeping — the same mechanism HM.11's own agent traced a Release-build packaging-smoke-test file-count mismatch to). The actual, sole runtime gate on whether `HM_PACE` can bind for any candidate is the workout-progression stage's own `requires` conditions (`PACE_SOURCE_IN: [RECENT_RACE]`, `GOAL_FEASIBILITY_IN`) — confirmed level-blind by HM.12 §19's own resolver read.

**This means HM_PACE eligibility for Beginner is not a binder-capability gap as HM.12 §7 characterized it — it is narrower: a catalog-completeness/dependency-declaration gap.** The binder would already correctly resolve `HM_PACE` for a Beginner candidate today if real `PACE_SOURCE_IN: RECENT_RACE` evidence existed, exactly as it does for Intermediate — but `beginner-modifier.v1.json` not listing `HM_PACE` in `eligibleWorkouts` risks an incomplete catalog bundle at packaging time (unverified this phase whether that manifests as a hard load-time failure or merely an incomplete artifact — flagged, not invented).

**Decision: Beginner HM progression *should* be permitted to use `HM_PACE`**, under the exact same evidence gate Intermediate already uses (`PACE_SOURCE_IN: RECENT_RACE` and `GOAL_FEASIBILITY_IN`), at `RACE_SPECIFIC_HM_DEVELOPMENT`/`_REHEARSAL` and `TAPER_HM_ACTIVATION`. A Beginner athlete with a genuine recent race result is not disqualified from evidence-based pace prescription merely by level — conflating "Beginner level" with "no pace evidence" would violate this phase's own §20 instruction. When no such evidence exists, the already-proven fallback (`THRESHOLD_TEMPO v4`/`EASY_STANDARD v4`, both already Beginner-eligible) applies exactly as it does today — **Beginner generation already works without exact pace evidence, unchanged.**

This requires, at implementation time, a new `BEGINNER_MODIFIER v2` catalog version adding `{HM_PACE, v1}` to `eligibleWorkouts` (mechanical, additive, mirrors `INTERMEDIATE_MODIFIER`'s own shape exactly) — not a new mechanism. Classification: mechanism = `DIRECT_EXISTING_AUTHORITY` (already-approved workout, already-generic gate); the decision to declare it for Beginner = `PRODUCT_DEFAULT` (no prior authority explicitly required this, but the level-blind design intent and the "evidence, not level, gates pace" principle strongly support it, and no counter-evidence exists).

---

## 18. Goal-feasibility compatibility

Re-confirmed directly (HM.12 §20, re-verified this phase by re-reading `GoalFeasibilityResolver.cs:1-60`): the resolver's `RealisticMaxRatio`/`ChallengingMaxRatio`/`StretchMaxRatio` constants carry no level parameter anywhere. **No reopening required.** A Beginner athlete's goal feasibility is evaluated by the same generic mechanism as any other level; a Beginner with strong independent race evidence and a genuinely realistic goal gap is not treated differently by this engine than an Intermediate athlete would be. Classification: `GENERIC_MECHANISM_REUSE`.

---

## 19. 3D feasibility

Hand-simulated (against the real, unmodified generic mechanisms; no code touched), Beginner 3D at the frozen values (peak=27.0, calibration start=15.5, LR shares 0.34/0.40/0.38/0.46, absolute cap 2.0 km/week, growth 7%/8%):

**Preferred-14W peak week:** LR = `27.0 × 0.38 = 10.26 → 10.0 km` (rounded). Remaining `27.0 - 10.0 = 17.0 km` splits across KEY+EASY, comfortably above `V1ThreeDaySessionVolumeAllocationPolicy`'s own `MinimumKeyKm=4.0`/`MinimumEasyKm=3.0` floors (re-confirmed directly this phase, `V1ThreeDaySessionVolumeAllocationPolicy.cs:10-12`) — no negative residual, no minimum-session violation.

**Growth:** starting 15.5 → peak 27.0 = 11.5 km total increase; at ≤2.0 km/week absolute cap and ≤11 non-taper transitions (the calibration horizon), an average of ~1.05 km/week is required — well inside both the percentage-rate and the absolute-cap guards. No contradiction.

**Taper:** anchor = 27.0 (fixed pre-taper reference, non-chained). Week 1 = `27.0 × 0.70 = 18.9 → 19.0`; Week 2 = `27.0 × 0.43 = 11.61 → 11.5`. LR at Week 2 (normal 0.38 share): `11.5 × 0.38 = 4.37 → 4.5 km` — above the standard `MinimumLongKm=5.0`... **note this sits just under 5.0**, which triggers the *exact same* narrow floor concern 10K's `ThreeDayBeginner` once hit. **Verified directly this phase (not assumed):** `CatalogSessionPrescriptionPlanner.cs:35` already computes `useBeginnerThreeDayTaperMinima = isThreeDay && weekly.IsTaperWeek && request.Candidate.Level == "NEW"` — this check is keyed on `Level == "NEW"` **generically**, with no `CanonicalDistanceFamily` qualifier. This means the exact narrow taper-floor override 10K's own `ThreeDayBeginner` originally required (`BeginnerTaperMinimumKeyKm=3d`/`EasyKm=2.5d`/`LongKm=3d`, replacing the normal 4.0/3.0/5.0 floors) **already activates automatically and correctly for a future HALF_MARATHON×BEGINNER×3D candidate's own Taper weeks, with zero new code** — a genuine, previously-undisclosed pleasant finding. Against the *lowered* Beginner-taper floor (`3.0 km`), Week 2's `4.5 km` LR clears comfortably.

**Result: `BEGINNER_3D_NUMERICALLY_FEASIBLE`.** No blocker found. No second hard session introduced; no quality LR introduced.

---

## 20. 4D feasibility

Hand-simulated Beginner 4D (peak=33.0, calibration start=19.0, LR shares 0.30/0.36/0.33/0.40, absolute cap 2.5 km/week):

**Preferred-14W peak week:** LR = `33.0 × 0.33 = 10.89 → 11.0 km`. Remaining `33.0 - 11.0 = 22.0 km` splits across KEY + 2×EASY (RUN_LAYOUT_4D's real role cardinality), comfortably above `V1FourDaySessionVolumeAllocationPolicy`'s own `MinimumKeySessionDistanceKm=3.0`/`MinimumEasySupportDistanceKm=1.5` floors (re-confirmed directly this phase) — no negative residual.

**Growth:** starting 19.0 → peak 33.0 = 14.0 km total increase; at ≤2.5 km/week absolute cap over ≤11 transitions, average ~1.27 km/week — comfortably inside both guards.

**Taper:** Week 1 = `33.0 × 0.70 = 23.1 → 23.0`; Week 2 = `33.0 × 0.43 = 14.19 → 14.0`. LR at Week 2: `14.0 × 0.33 = 4.62 → 4.5 km` — well above 4D's own, materially lower, `1.5 km` easy-support floor and its `3.0 km` key-session floor; no narrow override is needed at 4D (4D's floors are already low enough that Beginner's own reduced volumes never approach them).

**Result: `BEGINNER_4D_NUMERICALLY_FEASIBLE`.** No blocker found.

---

## 21. 5D feasibility

Not pursued in depth: `BEGINNER_5D_PRODUCT_INELIGIBLE` (§6) makes this moot for V1. For completeness: at a plausible midpoint peak of 38.0 km/week (band `[32,44]`, unfrozen, legacy-candidate-only) split across 5 sessions with KEY2 forced to `EASY_EQUIVALENT` (§5), the fifth day would add only undifferentiated easy volume rather than a materially useful training stimulus at Beginner's own already-modest weekly volume — the exact "does the fifth day provide useful easy-support volume, or does the selected weekly volume become too low to distribute safely across five runs" tension this phase's own §25 anticipated. This observation reinforces, but was not the primary basis for, §6's ineligibility decision.

---

## 22. Exact 10–16 simulations

Using the frozen closed-baseline phase-allocation table (10W→2/3/3/2 ... 16W→4/5/5/2) and the already-proven-generic HM.5.3 mechanism (`ResolvedPeakReferenceIsSelectedCeiling`, set `true` for both new Beginner policies, mirroring every existing HM policy):

- **Shorter horizons (10W–13W)** have fewer non-taper transitions than the `GoldenFixtureNonTaperTransitions=11` calibration point. E.g. at 10W (2+3+3=8 non-taper weeks, 7 transitions), starting from 15.5 (3D) at ~7% average growth reaches only `15.5 × 1.07^7 ≈ 24.9 km` — below the 27.0 selected peak, and the generic mechanism does **not** force acceleration to chase it (HM.1.1's own "must not accelerate merely to reach a ceiling" invariant, re-verified generic and unmodified). No contradiction.
- **Longer horizons (15W–16W)** have more non-taper transitions than 11. At 16W (4+5+5=14 non-taper weeks, 13 transitions), the `ResolvedPeakReferenceIsSelectedCeiling=true` flag caps the interpolation's transition count at 11 (the same HM.5.3 mechanism that already prevents 4D/3D/5D Intermediate's own 15W/16W extrapolation) — volume saturates at the selected peak (27.0/33.0) rather than extrapolating past it. No contradiction.
- Weekly growth guards (7%/8%, 2.0/2.5 km absolute) hold at every horizon by construction (unchanged generic enforcement).
- LR hard-cap (0.46/0.40) holds after rounding — the post-rounding LR-safety mechanism is generic and unmodified (HM.5.3/HM.12 §5).
- Absolute LR ceiling (16.0 km, §13) is never approached at either frequency's own selected peak × selected share (3D: `27.0×0.38=10.26`; 4D: `33.0×0.33=10.89` — both far below 16.0), so the ceiling never actually binds within the frozen bands. This is disclosed honestly: the ceiling is real, correctly scoped authority, but is not expected to be the *active* constraint for Beginner's own volumes — a materially different situation from Intermediate, where 19.0 km sits much closer to (and does occasionally bind against) the selected-share LR value.
- Taper remains non-chained at every horizon (generic, unmodified).

**No per-horizon numeric table was authored** (per this phase's own instruction) — the generic mechanism, already proven across seven horizons for three separate Intermediate frequencies, is reused unmodified; this phase only supplies the new numeric inputs it consumes.

---

## 23. Product-eligibility matrix

| Cell | Status | Classification |
|---|---|---|
| **BEGINNER 3D** | **ELIGIBLE** | Numerically feasible (§19); no product concern raised |
| **BEGINNER 4D** | **ELIGIBLE** | Numerically feasible (§20); no product concern raised |
| **BEGINNER 5D** | **INELIGIBLE** | `PRODUCT_INELIGIBLE` (§6) — real, on-point internal evidence (HM's own named rollout matrix + 10K's own real precedent), not `AUTHORITY_NOT_YET_RESOLVED` and not `ENGINE_CAPABILITY_GAP` (the engine could technically materialize it) |

---

## 24. Complete Beginner policy tables

### BEGINNER 3D

| Row | Value | Scope | Classification | Evidence/Source |
|---|---|---|---|---|
| ProductEligible | Yes | 3D | — | §6, §19 |
| PeakVolumeBandMin | 22.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2 |
| PeakVolumeBandMax | 32.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| SelectedPeakReference | 27.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | band midpoint, practiced HM/10K convention |
| PreferredWeeklyIncreaseRate | 0.07 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal across `VolumeSafetyPolicy.cs` |
| HardWeeklyIncreaseRate | 0.08 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | same |
| AbsoluteWeeklyIncreaseCapKm | 2.0 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | 10K `ThreeDayBeginner` on-point precedent |
| CalibrationStartingVolume | 15.5 km | 3D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM's own 4D-ratio-reuse methodology (cross-level extension) |
| LR PreferredShareMin | 0.34 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | = `HalfMarathonIntermediate3D` (10K's own twice-confirmed level-invariance-within-frequency precedent) |
| LR PreferredShareMax | 0.40 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR SelectedShare | 0.38 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR HardShareCap | 0.46 | 3D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| PreferredAbsolutePeakLongRunKm | 16.0 km | Beginner-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | B.A.A. Level One (already-accepted HM.1.1 evidence) |
| StartingAbsoluteLongRunCap | N/A (unset) | 3D | `NOT_APPLICABLE` | mirrors Intermediate 3D's own null; 8km legacy candidate not promoted |
| TaperWeek1Multiplier | 0.70 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | reused, re-verified (strengthened) scope |
| TaperWeek2Multiplier | 0.43 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| PrimaryDoseTier | LOW | Beginner-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM-LIT-17 (disclosed `CAPABILITY_GAP`: no distinct workout content yet) |
| HM_PACE eligibility | Eligible under `PACE_SOURCE_IN:RECENT_RACE` | Beginner-wide | mechanism `DIRECT_EXISTING_AUTHORITY`; declaration `PRODUCT_DEFAULT` | §17 |
| 5D KEY2 behavior | N/A | — | — | 3D has no KEY2 |
| MaximumTrueHardStimuliPerWeek | 1 | Beginner-wide | `DIRECT_EXISTING_AUTHORITY` | `beginner-progression-modifier.v1.json` |

### BEGINNER 4D

| Row | Value | Scope | Classification | Evidence/Source |
|---|---|---|---|---|
| ProductEligible | Yes | 4D | — | §6, §20 |
| PeakVolumeBandMin | 28.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2 |
| PeakVolumeBandMax | 38.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| SelectedPeakReference | 33.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | band midpoint |
| PreferredWeeklyIncreaseRate | 0.07 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| HardWeeklyIncreaseRate | 0.08 | HM-wide | `DIRECT_EXISTING_AUTHORITY` | universal |
| AbsoluteWeeklyIncreaseCapKm | 2.5 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | 10K `BeginnerFourDay` on-point precedent |
| CalibrationStartingVolume | 19.0 km | 4D | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | ratio-reuse methodology |
| LR PreferredShareMin | 0.30 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | = `HalfMarathonIntermediate4D` |
| LR PreferredShareMax | 0.36 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR SelectedShare | 0.33 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| LR HardShareCap | 0.40 | 4D | `STRONG_EVIDENCE_DERIVED_DECISION` | same |
| PreferredAbsolutePeakLongRunKm | 16.0 km | Beginner-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same as 3D |
| StartingAbsoluteLongRunCap | N/A (unset) | 4D | `NOT_APPLICABLE` | mirrors Intermediate 4D's own null |
| TaperWeek1Multiplier | 0.70 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| TaperWeek2Multiplier | 0.43 | HM-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| PrimaryDoseTier | LOW | Beginner-wide | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | same |
| HM_PACE eligibility | Eligible under `PACE_SOURCE_IN:RECENT_RACE` | Beginner-wide | same as 3D | §17 |
| 5D KEY2 behavior | N/A | — | — | 4D has no KEY2 |
| MaximumTrueHardStimuliPerWeek | 1 | Beginner-wide | `DIRECT_EXISTING_AUTHORITY` | same |

### BEGINNER 5D — `PRODUCT_INELIGIBLE`, no further numeric authority pursued (§6, §21).

---

## 25. Authority classifications

Summary count across both frozen frequencies: `DIRECT_EXISTING_AUTHORITY` — 6 fields (growth rates ×2, hard-stimulus cap, both starting-LR-cap N/A determinations); `STRONG_EVIDENCE_DERIVED_DECISION` — 8 fields (both LR-share quadruples, the strongest tier reached this phase, earned by twice-independently-confirmed 10K methodology precedent); `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — the remainder (peak bands, selected peaks, absolute caps, calibration starts, absolute peak LR, taper multipliers, dose tier). **No field is classified `PRODUCT_DEFAULT`** (the weakest tier) — every frozen number in this phase traces to a real, cited, on-point piece of internal or already-accepted evidence, never an arbitrary choice. No value is classified `EVIDENCE_BACKED` — consistent with this engagement's unbroken discipline of never overstating practice-convergence or precedent-reuse as causal proof.

---

## 26. Remaining capability gaps

1. **Dose-tier content gap (§4):** `LOW` dose tier is currently expressed only structurally (fewer true-hard stimuli), not through any reduced-intensity workout-definition variant — `FARTLEK v4`/`THRESHOLD_TEMPO v4` are shared verbatim with Intermediate's own base progression. Non-blocking for dark implementation; disclosed for future consideration.
2. **`eligibleWorkouts` semantic ambiguity (§17):** whether an undeclared workout reference causes a hard catalog-load failure or merely an incomplete bundle was not independently re-verified this phase (would require reading `PlanCatalogBundleLoader.cs`'s consumer of `ReferencedWorkouts` in full, out of this phase's scope) — flagged as a real open question for whoever implements the `BEGINNER_MODIFIER v2` addition.
3. **Mechanical, not evidentiary, implementation-time tasks** (do not block this phase's own closure, per its own §34 criteria): (a) extend `CatalogVolumeAndLongRunPlanner`'s existing missing/zero-readiness fail-closed guard (currently lists `HalfMarathonIntermediate3D/4D/5D` by `ReferenceEquals`) to also cover the new `HalfMarathonBeginner3D/4D` instances; (b) extend `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek`-style dispatch with a new sibling `ForHalfMarathonBeginnerDaysPerWeek(int)`, mirroring 10K's own `ForBeginnerDaysPerWeek` shape (HM.12 §29's own recommended pattern); (c) new dark-only `V1CatalogPilotIdentityPolicy` candidate keys/tuple arms; (d) new `BEGINNER_MODIFIER v2` catalog version (§17); (e) two new combination files (`half-marathon-3d-beginner.v1.json`, `half-marathon-4d-beginner.v1.json`) referencing the *existing, unmodified* `HALF_MARATHON_MASTER v1`, `RUN_LAYOUT_3D`/`RUN_LAYOUT_4D`, and `HALF_MARATHON_WORKOUT_PROGRESSION v1` — no new master/progression/layout version required for 3D/4D (unlike 5D's own precedent, since Beginner adds no second lane).

None of these require new engine behavior beyond what is explicitly named here.

---

## 27. HM.14 implementation readiness

```
HM_BEGINNER_3D_READY_FOR_DARK_IMPLEMENTATION
HM_BEGINNER_4D_READY_FOR_DARK_IMPLEMENTATION
```

Beginner 5D is explicitly and deliberately excluded from V1 scope (`PRODUCT_INELIGIBLE`, §6) — this does not block overall Beginner closure, per this phase's own §30 rule that a deliberately-frozen-out ineligible cell does not prevent closure.

---

## 28. Recommended implementation shape

Mirrors HM.8's own proven pattern exactly, per this phase's own §29 target architecture — **zero new generator classes**:

- Two new named `VolumeSafetyPolicy` instances (`HalfMarathonBeginner3D`, `HalfMarathonBeginner4D`), encoding exactly this report's frozen tables, plus a corresponding `HalfMarathonBeginner{3D,4D}TaperVolumePolicy` pair mirroring the existing internal-class convention.
- One new `BEGINNER_MODIFIER v2` catalog version (adds `HM_PACE v1` to `eligibleWorkouts`; everything else unchanged from v1).
- Zero new `HALF_MARATHON_MASTER`/`HALF_MARATHON_WORKOUT_PROGRESSION`/`RUN_LAYOUT` versions — 3D/4D reuse the existing, unmodified v1 of each verbatim (no second lane, no new stage).
- Two new dark-only combination files referencing `BEGINNER_MODIFIER v2` and the existing unmodified master/layout/progression documents.
- A `ForHalfMarathonBeginnerDaysPerWeek(int)` dispatcher (new sibling to the existing `ForHalfMarathonIntermediateDaysPerWeek`, mirroring 10K's own `ForBeginnerDaysPerWeek`/`ForIntermediateDaysPerWeek` sibling-method shape).
- Two new dark-only candidate-key constants + tuple arms in `V1CatalogPilotIdentityPolicy`, additive, mirroring the existing HM Intermediate pattern exactly.
- Extension of the existing missing/zero-readiness fail-closed guard to the two new policies (mechanical).

No `Beginner3DGenerator`/`Beginner4DGenerator`-style per-cell engine. Public/Runway/LongHorizon remain untouched and closed.

---

## FINAL CLASSIFICATION

```
HM_BEGINNER_3D_4D_5D_AUTHORITY_CLOSED
```

All required criteria are met: the intended Beginner frequency matrix is frozen (3D/4D eligible, 5D deliberately ineligible with real evidence, not silently dropped); peak bands, selected peaks, weekly progression, absolute increase caps, LR-share policies, absolute peak LR, starting-LR-cap (explicitly N/A), and taper scope/numbers are all frozen for every eligible cell; Beginner dose behavior is frozen (LOW, with an honestly disclosed content-gap); `HM_PACE` eligibility is frozen (eligible under existing evidence gates, with a disclosed catalog-declaration task); 5D KEY2 semantics are frozen (conditionally, for completeness, though moot); 10–16W numeric feasibility is proven by hand-simulation for both eligible frequencies; every implementation-time task is explicitly named (§26/§28), not left as an unidentified new-engine-behavior surprise; no unsupported legacy value was silently promoted (the 8km starting-cap candidate was explicitly examined and explicitly not adopted); no Advanced decision was made anywhere in this phase.
