# PHASE DIST-GEN.15 — 16K × Intermediate × 3D Projected-Distance Frequency-Orthogonality Structural and Numeric Authority Closure

## 1. DIST-GEN.14 decision consumed

DIST-GEN.14 selected `NEXT_AXIS_INTERMEDIATE_3D` with 16K as the specific target, on the strength of a direct-source finding that canonical TEN_K's own Intermediate×3D long-run-share quadruple genuinely disagrees with canonical HALF_MARATHON's own — unlike at 4D/5D/6D, where the two families agree exactly. This phase's own objective, set by DIST-GEN.14, is to resolve that tension field-by-field through real evidence, not to assume either family's value nor to derive a new one mathematically.

## 2. Repository baseline

Starting commit `b8aad70` on `main`, 0 ahead / 0 behind `origin/main`. Working tree carried only pre-existing tracked build-artifact noise at the start. This phase is governance-only and performs zero production edits.

## 3. Current public/dark matrix

Frozen, not reopened:

| Target | Dark | Public |
|---|---|---|
| 15.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 16.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 18.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 16.0 × HalfMarathon × Intermediate × 3D | **No (this phase closes authority only)** | No |

## 4. Exact experiment cell

`TargetDistanceKm = 16.0, ParentDistanceFamily = HalfMarathon, Level = Intermediate, RunsPerWeek = 3`. No other cell (15K×3D, 18K×3D, any 5D/6D, any Beginner/Advanced) is touched by this phase.

## 5. Orthogonality research question

Whether projected-distance authority and run-frequency authority are fully orthogonal (A), partially orthogonal (B), not orthogonal (C), or blocked (D) — determined field by field, not assumed.

## 6. Canonical 2×2 reference matrix

Read directly from `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`:

| Field | TEN_K I4D (`Default`) | TEN_K I3D (`ThreeDayIntermediate`) | HM I4D (`HalfMarathonIntermediate4D`) | HM I3D (`HalfMarathonIntermediate3D`) |
|---|---|---|---|---|
| PreferredWeeklyIncreaseRate | 0.07 | 0.07 | 0.07 | 0.07 |
| HardWeeklyIncreaseRate | 0.08 | 0.08 | 0.08 | 0.08 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | **2.0** | 2.5 | **2.0** |
| CalibrationStartingVolumeKm | 24.0 | 12.0 | 25.0 | 20.0 |
| SelectedPeakReference | 38.0 | 22.5 | 43.0 | 34.0 |
| ResolvedPeakReferenceIsSelectedCeiling | false | false | true | true |
| GoldenFixtureNonTaperTransitions | 10 | 10 | 11 | 11 |
| Taper | 1wk/0.53 | 1wk/0.53 | 2wk/0.70+0.43, non-chained | 2wk/0.70+0.43, non-chained |
| LongRunPreferredMinimumShare | 0.30 | **0.38** | 0.30 | **0.34** |
| LongRunPreferredMaximumShare | 0.36 | **0.42** | 0.36 | **0.40** |
| LongRunSelectionShare | 0.33 | **0.40** | 0.33 | **0.38** |
| LongRunHardCapShare | 0.40 | **0.42** | 0.40 | **0.46** |
| PreferredAbsolutePeakLongRunKm | null | null | 19.0 | 19.0 |
| StartingAbsoluteLongRunCapKm | null | null | null | null |
| RoundingRule | standard | **3D-specific reconcile rule** | standard | standard |

Plus the existing projected reference, `HalfMarathonIntermediate4DTargetDistance16K`: growth/cap/LR-share all referenced from `IntermediateFourDayGrowthAuthority` (0.07/0.08/2.5/0.30-0.36-0.33-0.40); `CalibrationStartingVolumeKm=23.5`; `SelectedPeakReference=40.0` (band [34,46]); `PreferredAbsolutePeakLongRunKm=15.0`; taper referenced from `HalfMarathonParentTaperAuthority`.

## 7. 16K 4D reference

Confirmed unchanged and untouched: the above `HalfMarathonIntermediate4DTargetDistance16K` values, plus `TargetDistance16KHorizonPolicy` (`MinimumCoreWeeks=10, PreferredCoreWeeks=12, MaximumCoreWeeks=14`) and `TargetDistance16KPeakVolumeBandPolicy` (`MinimumKm=34.0, MaximumKm=46.0`).

## 8. TEN_K 4D→3D delta

Growth rates unchanged (0.07/0.08→0.07/0.08). Absolute cap: 2.5→2.0 (−0.5). LR-share quadruple: 0.30/0.36/0.33/0.40 → 0.38/0.42/0.40/0.42 — every member increases, and the preferred-maximum (0.42) and hard-cap (0.42) collapse to the identical value, leaving zero headroom between them, an internal shape distinct from 4D's own. Taper mechanism unchanged (single-week, 0.53). Peak volume 38.0→22.5 (a large reduction). Absolute peak LR: null→null (unchanged, both null). Starting LR cap: null→null. Rounding rule changes to a 3D-specific reconciliation variant not used anywhere in the HM family.

## 9. HM 4D→3D delta

Growth rates unchanged. Absolute cap: 2.5→2.0 (−0.5, **identical delta to TEN_K's own**). LR-share quadruple: 0.30/0.36/0.33/0.40 → 0.34/0.40/0.38/0.46 — every member increases, but by a different magnitude than TEN_K's own delta, and the shape is preserved (0.34<0.38<0.40<0.46, clean ascending order with real headroom between selection share and hard cap, unlike TEN_K's collapsed shape). Taper mechanism unchanged (2-week, 0.70/0.43, non-chained). Peak volume 43.0→34.0. Absolute peak LR: 19.0→19.0 (**unchanged — frequency-invariant within the HM family**). Starting LR cap: null→null.

## 10. Cross-anchor frequency analysis

| Field | Cross-anchor conclusion |
|---|---|
| Growth rates | `CONSISTENT_FREQUENCY_AUTHORITY` — unchanged at every anchor, every frequency |
| Absolute weekly cap | `CONSISTENT_FREQUENCY_AUTHORITY` — both families agree exactly at 3D (2.0), just as they agree exactly at 4D/5D/6D (2.5) |
| LR-share quadruple | `FAMILY_DEPENDENT_FREQUENCY_AUTHORITY` — see §28-30 below for the full resolution; this is the central tension the phase exists to resolve |
| Absolute peak LR | `CONSISTENT_FREQUENCY_AUTHORITY` at the family level — HM's own value is identical at 3D/4D (19.0/19.0); TEN_K's own is identical (null/null) at both. Both canonical families independently demonstrate frequency-invariance for this specific field |
| Starting LR cap | `CONSISTENT_FREQUENCY_AUTHORITY` — null at 3D for both families, matching the established fact that this field is frequency-driven (non-null only at 5D/6D), not distance- or target-driven |
| Taper | `PARENT_FAMILY_ONLY`, unaffected by frequency within either family (HM: identical 3D/4D; TEN_K: identical 3D/4D) — reconfirms DIST-GEN.8/12/13's own conclusion that taper is inherited from parent family, never frequency-scoped |
| Peak volume / selected peak / calibration | `INSUFFICIENT_EVIDENCE` at the cross-anchor level for direct reuse purposes — real magnitude changes at both anchors (TEN_K −41%, HM −21%), of different scale, offering no clean shared-owner signal; genuinely target-owned per prior governance and requiring independent 16K-specific evidence, not derivable from either anchor's own delta |
| RoundingRule | `NOT_APPLICABLE` to this phase's own decision surface — a TEN_K-family-internal mechanical detail, not consumed by the HM-parented 16K cell |

This diagnostic table is descriptive only, per §9 of the governing prompt — no 16K value is derived from it.

## 11. Structural parent

Confirmed unchanged and independent of frequency: 16K's structural parent is HALF_MARATHON. This is directly reconfirmed by the catalog itself — the `HALF_MARATHON_MASTER v1` template (see §12) declares `supportedRunsPerWeek: [3, 4]`, meaning the SAME structural parent already, natively supports both frequencies for canonical HM. Nothing in this phase contradicts or reopens the original DIST-GEN.0 structural-parent decision.

## 12. Catalog candidate

Confirmed via direct catalog inspection: `plan-catalog/catalog/combinations/half-marathon-3d-intermediate.v1.json` exists, is current, and specifies `masterTemplate: HALF_MARATHON_MASTER v1`, `layout: RUN_LAYOUT_3D v1`, `levelModifier: INTERMEDIATE_MODIFIER v8`, `rulePack: APPSEL_RACE_PLAN_V1 v10` — identical to the 4D combination (`half-marathon-4d-intermediate.v1.json`) in every field except `layout`. No new catalog combination, no `SIXTEEN_K` family, no custom progression is created or needed.

## 13. RunLayout

`plan-catalog/catalog/layouts/run-layout-3d.v1.json`: exactly `[KEY_SESSION, EASY_SUPPORT, LONG_RUN]` — 1 KEY, 1 EASY, 1 LONG, zero secondary-quality/KEY2 lane. Frozen exactly as read from source, not from memory.

## 14. Phase topology

Confirmed frequency-invariant: both the 3D and 4D combinations reference the identical `HALF_MARATHON_MASTER v1` template, whose own `supportedRunsPerWeek: [3, 4]` field confirms frequency is handled as an orthogonal layout swap at the catalog level, never a template fork. Phase minima (`FOUNDATION` min2/pref3/max4, `BUILD` min3/pref4/max5, `RACE_SPECIFIC` min3/pref5/max5, `TAPER` min2/pref2/max2) are byte-identical to 4D. Consumed directly, not re-derived — consistent with DIST-GEN.4's own finding that horizon/phase topology is broadly frequency-invariant within the family.

## 15. Minimum horizon

`MinimumCoreWeeks = 10`, confirmed frequency-agnostic: `RaceHorizonPolicy`'s HM-parent constants (`HalfMarathonMinimumSupportedStandaloneWeeks=10`) carry no frequency parameter anywhere in their declaration or consumption, and `TargetDistance16KHorizonPolicy.MinimumCoreWeeks` already references this same constant directly. Direct reuse — parent-family structural authority, unaffected by frequency by construction.

## 16. Preferred horizon

`PreferredCoreWeeks = 12`, reused directly from 16K's own existing `TargetDistance16KHorizonPolicy`. This class was never frequency-parametrized to begin with (it takes no `RunsPerWeek` argument), and the catalog's own topology confirms frequency does not alter phase structure — classifying this as `TARGET_ONLY` authority, safely reusable, not `TARGET_X_FREQUENCY_INTERACTION`.

## 17. Maximum horizon

`MaximumCoreWeeks = 14`, same reasoning and same direct reuse as §16.

## 18. Complete phase allocations

Identical to 16K's own existing 4D phase-allocation pattern at every horizon (10W→2/3/3/2 through 14W→3/4/5/2), since the underlying catalog phase topology (§14) is unchanged by frequency. No new allocation table is required.

## 19. Compressed-core status

Unaffected. Anything below the 10-week structural minimum remains explicitly out of scope for this or any future phase, per standing engagement policy.

## 20. Existing 16K/10-mile evidence inventory

DIST-GEN.1's own complete external-evidence table (Marathon Handbook, Snacking in Sneakers, RUN/Outside, Cherry Blossom official intermediate program, Hal Higdon 15K/10-mile combined program) contains exactly one explicit frequency figure across all sources — "4 days/week," named in one source — with every other source frequency-silent. **None of DIST-GEN.1's own original evidence is 3-day-specific.** This confirms DIST-GEN.14's own §39 characterization ("moderate, unresearched" 3D evidence availability) and establishes that this phase genuinely needs its own dedicated evidence-gathering, not merely a re-read of DIST-GEN.1's own table.

## 21. New 3D-specific external evidence

Targeted web research was performed for two distinct, race-distance-matched query families, since the population these plans target turned out to matter enormously:

**Query: "10 mile race training plan 3 days per week intermediate runner weekly mileage long run."** Findings across Marathon Handbook, Snacking in Sneakers, RunToTheFinish, RUN/Outside, and the Cherry Blossom intermediate program: a 3-day 10-mile structure builds from roughly 8-15 running miles/week (13-24km) across the plan, with the long run building gradually from 3-4 miles toward 8-9 miles (13-14.5km) by race day, over roughly 8-10 weeks, explicitly aimed at runners who can already complete a 10K but are new to 3-day-a-week structured racing at this distance. **Supports**: that a genuine 3-day, 10-mile-specific training structure exists and follows the same KEY/EASY/LONG shape Appsel's own RunLayout already uses. **Does not support**: any specific Appsel peak-volume-band or LR-share-percentage number, since this population skews toward the lower end of a "first 10-miler" experience tier, materially below what Appsel's own "Intermediate" classification (an existing, meaningful running base) generally assumes.

**Query: "half marathon training plan 3 days a week intermediate weekly mileage peak long run,"** followed by direct fetch of Marathon Handbook's dedicated "3 Day A Week Half Marathon Plan": a 12-week plan with weekly structure of 1 easy run + 1 quality/tempo-interval workout + 1 long run (an exact structural match to Appsel's own 3D RunLayout), peaking at approximately 25-27 miles/week (40-43km) with a peak long run of 12 miles (19km) in week 10 — the long run alone constitutes roughly 44-48% of total weekly volume at peak, a proportion that lands almost exactly at HM's own already-closed 3D hard-cap share (0.46), a genuine, independently-sourced corroboration of that already-frozen HM value (see §29 below). **Supports**: that a real, well-structured, 3-day half-marathon-style plan exists with volume and long-run-share proportions broadly consistent with Appsel's own already-closed HM Intermediate 3D authority. **Does not support**: a specific 16K number, since this plan targets the full half-marathon distance (21.1km), not 16K, and its own weekly volume (40-43km) sits far closer to Appsel's own HM 4D authority (43.0) than to HM's own, already-reduced-for-3-days-per-week 3D authority (34.0) — a real, disclosed tension this report does not attempt to resolve on HM's behalf, since HM's own 3D authority is canonical and untouched (§73).

**The RUN/Outside "Flexible Half Marathon Training Plan (3/4/5/6 days)" source could not be fetched** — it redirected to an authentication wall and is excluded from this evidence base rather than guessed at.

## 22. Peak-volume authority

**Not frozen in this phase.** See §67/§70 for the full reasoning: the two race-distance-matched evidence families found (§21) diverge by roughly a factor of two (13-24km for 10-mile-specific 3-day content vs. 40-43km for half-marathon-specific 3-day content) with no meaningful overlap, reflecting a genuine population mismatch (the 10-mile content skews toward a lower experience tier than Appsel's own Intermediate classification assumes) rather than converging evidence around a central estimate. The governing prompt explicitly and repeatedly (§23-24, §76) prohibits deriving this value from any ratio, interpolation, or nearest-anchor relationship to 16K's own 4D value or to either canonical family's own 3D value. With both the permitted derivation methods and the available direct evidence foreclosed, this field cannot be responsibly frozen in this phase.

## 23. Selected peak

**Not frozen — mechanically downstream of §22.** `SelectedPeakReference` has, at every prior closure, been derived from within an already-frozen peak-volume band; with no band frozen, no selected-peak value can be responsibly stated.

## 24. Calibration

**Not frozen — mechanically downstream of §22-23.** `CalibrationStartingVolumeKm` has, at every prior closure (including 16K's own 4D closure), been derived via the same HM-family calibration/peak ratio methodology applied to that target's own already-selected peak; with no peak selected, this value cannot be responsibly stated without inventing a peak first.

## 25. Growth-rate ownership

`PreferredWeeklyIncreaseRate=0.07`, `HardWeeklyIncreaseRate=0.08` — confirmed identical across every single instance inspected in `VolumeSafetyPolicy.cs`, at every level and frequency, with zero exception found anywhere. **Classification: `LEVEL_FREQUENCY_INVARIANT` / `GENERIC`, `REUSED_FROM_GENERIC_MECHANISM`, `DIRECT_EXISTING_AUTHORITY`.** No new authority owner is created in this governance-only phase (per §31/§64 of the governing prompt); the conceptual owner for a future implementation phase would be the existing, unmodified generic growth-rate concept — these two literal values, unchanged.

## 26. Absolute weekly-cap ownership

`AbsoluteWeeklyIncreaseCapKm=2.0` at Intermediate×3D — confirmed by direct, exact agreement between canonical TEN_K's own `ThreeDayIntermediate` and canonical HM's own `HalfMarathonIntermediate3D` (both 2.0, both distinct from 4D's shared 2.5). This is a clean, real, cross-family convergence at 3D specifically — the same kind of evidence DIST-GEN.8 used to establish 4D's own 2.5 as level/frequency-invariant. **Classification: `FREQUENCY_ONLY` (Intermediate×3D-scoped), `REUSED_FROM_FREQUENCY_AUTHORITY`, `DIRECT_EXISTING_AUTHORITY`.** Stronger evidence than the LR-share situation specifically because both canonical anchors agree on the exact numeric value itself, not merely on the existence of some frequency-driven change — there is no conflict to resolve here.

## 27. Starting-LR-cap ownership

`StartingAbsoluteLongRunCapKm = null` at Intermediate×3D for both canonical families — confirmed. This is consistent with the already-established fact (DIST-GEN.8/12/13) that this field is frequency-driven and non-null only at 5D/6D; 3D correctly remains null, exactly as 4D does. **Classification: `FREQUENCY_ONLY`, `REUSED_FROM_FREQUENCY_AUTHORITY`, `DIRECT_EXISTING_AUTHORITY`.** Not converted into a numeric default.

## 28. LR-share canonical conflict

Confirmed byte-for-byte via direct source read: TEN_K `ThreeDayIntermediate` = `0.38/0.42/0.40/0.42`; HM `HalfMarathonIntermediate3D` = `0.34/0.40/0.38/0.46`. The conflict DIST-GEN.14 identified is real and precisely as described. One additional structural observation: TEN_K's own quadruple has its preferred-maximum share exactly equal to its hard-cap share (0.42 = 0.42), leaving zero headroom — an internally unusual shape neither TEN_K's own 4D/5D/6D instances nor any HM instance shares. HM's own 3D quadruple preserves a clean ascending shape with real headroom (0.34<0.38<0.40<0.46), consistent with every other instance in the file.

## 29. LR-share evidence analysis

Two further, decisive data points were found by examining every other Intermediate-adjacent instance at 3D in `VolumeSafetyPolicy.cs`, beyond the two DIST-GEN.14 already compared:

- **`HalfMarathonAdvanced3D`** carries LR shares `0.34/0.40/0.38/0.46` — **byte-identical to `HalfMarathonIntermediate3D`.**
- **`HalfMarathonBeginner3D`** carries LR shares `0.34/0.40/0.38/0.46` — **byte-identical to `HalfMarathonIntermediate3D`**, with its own doc comment explicitly stating this reuse is a deliberate, precedented pattern ("reused verbatim... 10K's own twice-independently-confirmed real precedent that Beginner's LR-share quadruple equals the same-frequency Intermediate quadruple exactly when the field is frequency-owned").
- **`Advanced3D`** (TEN_K family) carries LR shares `0.38/0.42/0.40/0.42` — **byte-identical to TEN_K's own `ThreeDayIntermediate`**, with its own doc comment stating this is deliberate ("Advanced does not get a different long-run policy at 3D").

This produces a clean, consistent, cross-level pattern within each family independently: LR-share at 3D is **level-blind within a family** (Beginner=Intermediate=Advanced, both for HM's own 0.34/0.40/0.38/0.46 and for TEN_K's own 0.38/0.42/0.40/0.42), but the two families genuinely disagree with each other. This is a materially stronger and more precise signal than DIST-GEN.14's own framing ("genuinely ambiguous") suggested — it points specifically and consistently toward `PARENT_FAMILY_SCOPED` ownership at 3D (the same ownership category taper already occupies), not toward an unresolved or genuinely ambiguous interaction, and not toward a level-driven pattern.

A second relevant fact: `HalfMarathonIntermediate3D`'s own LR-share quadruple carries a full, explicit governance provenance (HM.7 §9, "informed by but not copied from two independent real-world 3-day HM program sources and 10K's own directional precedent" — see also the corroborating external Marathon Handbook 3-day-HM-plan finding in §21, whose own long-run-share-of-total (~44-48%) lands almost exactly at HM's own hard-cap share of 0.46). `TEN_K`'s own `ThreeDayIntermediate` quadruple, by contrast, carries **no doc comment and no recorded derivation anywhere in the codebase** — it is an undocumented legacy constant. Disagreement between a well-evidenced, externally-corroborated value and a completely unattributed legacy value is weak, not strong, evidence against parent-family-scoping — it is at least as plausible that TEN_K's own number was never itself evidence-derived, in which case its disagreement with HM's own value says little about whether the field is genuinely level/frequency-invariant in principle.

## 30. LR-share final authority

**16K × Intermediate × 3D adopts HALF_MARATHON's own already-closed Intermediate×3D long-run-share quadruple directly: `PreferredLongRunShareMin=0.34, PreferredLongRunShareMax=0.40, SelectedLongRunShare=0.38, HardLongRunShareCap=0.46`.**

This is a reuse decision, not a new derivation: 16K's structural parent is HALF_MARATHON (§11), and taper/phase-topology/horizon-minimum are all already-established HM-parent-scoped authorities for 16K (§14-15); extending that identical parent-family-scoping principle to LR-share-at-3D specifically — now directly supported by the clean level-blind pattern within each family (§29) — is the most internally consistent choice available, and directly reuses an existing, already-governed, externally-corroborated value rather than inventing one. Per §13/§65-66 of the governing prompt (evaluate options A-D; do not interpolate or average; do not over- or under-generalize from two anchors): Option B (HM-family 3D authority) is selected over Option A (TEN_K-family) because HM is 16K's own actual structural parent and HM's own value carries real, documented, externally-corroborated evidence while TEN_K's does not; over Option C (a new exact-cell 16K×I×3D authority) because no 16K-specific LR-share evidence exists or is needed once parent-family ownership is established; interpolation/averaging (explicitly listed among Option C's rejected variants) was never considered. **Confidence tier: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`** — solidly evidence-informed via the clean cross-level pattern and the external Marathon Handbook corroboration, but not `STRONG_EVIDENCE_DERIVED_DECISION` since the reused HM value's own original tier was itself `PRODUCT_DEFAULT`-adjacent, not top-tier. **Interaction classification: `REUSED_FROM_PARENT_AUTHORITY`.**

## 31. Absolute peak LR evidence

Both canonical families independently demonstrate frequency-invariance for this exact field: HM's own `PreferredAbsolutePeakLongRunKm` is identically 19.0 at both 3D and 4D; TEN_K's own is identically null at both 3D and 4D. This is a clean, consistent, cross-family signal — stronger than the LR-share case, since here both families agree with themselves across frequency and neither family shows any change at all. 16K's own absolute peak LR (15.0) is already exact-target-owned authority, independently frozen and distinct from every other target (14.5/15.0/16.0/19.0).

## 32. Absolute peak LR final authority

**16K × Intermediate × 3D reuses 16K's own already-frozen `PreferredAbsolutePeakLongRunKm = 15.0` directly, unchanged from 4D.** This is not a ratio or transformation of 16K's own 4D value — it is a direct application of a real, demonstrated frequency-invariance property (§31) to this specific target's own already-governed exact-target value, in the same category as reusing `MinimumCoreWeeks`/taper/growth-rate/absolute-cap: a field independently shown to be frequency-invariant, carried over unchanged, never recomputed. **Confidence tier: `DIRECT_EXISTING_AUTHORITY`.** **Interaction classification: `REUSED_FROM_TARGET_AUTHORITY`.**

## 33. LR-below-target invariant

15.0 < 16.0 — holds, unchanged, not reopened.

## 34. Taper ownership

Confirmed unchanged: HM's own taper (2-week, 0.70/0.43, non-chained fixed-anchor) is identical at 3D and 4D; TEN_K's own genuinely different taper (single-week, 0.53) is likewise identical at 3D and 4D within its own family — the same clean cross-frequency-invariance-within-family, cross-family-difference pattern already established for taper by DIST-GEN.8/12/13, now directly reconfirmed to hold at 3D as well. **16K × Intermediate × 3D consumes the identical HM-parent taper authority 16K's own 4D cell already consumes.** No cell-specific taper is created. **Classification: `PARENT_FAMILY_ONLY`, `REUSED_FROM_PARENT_AUTHORITY`, `DIRECT_EXISTING_AUTHORITY`.**

## 35. Taper mechanism

Non-chained, fixed-anchor: Week 1 = pre-taper anchor × 0.70, Week 2 = the SAME pre-taper anchor × 0.43 (never chained from Week 1's own output). Unchanged from 4D. The pre-taper anchor itself is the cell's own `ResolvedPeakReference` — which, per §22-24, is not yet frozen for this cell, so the taper *mechanism* is fully resolved while the taper's own numeric *application point* remains blocked on the same peak-volume gap.

## 36. Progression reuse

The existing, unmodified `HALF_MARATHON_MASTER v1` template and `APPSEL_RACE_PLAN_V1 v10` rule pack are directly reused via the existing `half-marathon-3d-intermediate.v1.json` combination — no 16K-specific progression is created or required.

## 37. Quality-session structure

Confirmed via direct catalog read (§13): exactly one KEY session per week at 3D, zero secondary-quality/KEY2 lane. This cleanly avoids the 5D/6D quality-lane complexity DIST-GEN.14 flagged as a reason to sequence those frequencies behind 3D.

## 38. Race-specific dose

The reused HM Intermediate 3D candidate's own structural workout dose (session role, not reps/duration, which remain candidate-owned and are not invented here) is directly reusable with 16K's own `TARGET_RACE_PACE` semantics, exactly as 16K's own 4D cell already reuses the 4D candidate's dose with the same generic pace mechanism. No new dose is invented.

## 39. Target-race-pace

Unchanged, fully generic: `TargetDistanceKm=16.0` drives target-race pace identically regardless of frequency. No literal HM or TEN_K pace token, no new `SIXTEEN_K_PACE` concept. **`SHARED_GENERIC_MECHANISM`.**

## 40. Riegel

Unchanged, fully generic. No new exponent, no frequency-dependent adjustment. **`SHARED_GENERIC_MECHANISM`.**

## 41. Goal-time fitness separation

Unchanged: a desired 16K finish time does not manufacture current training fitness at any frequency. No self-validation introduced.

## 42. Product-average behavior

Unchanged: existing 16K product-average/race-performance-equivalence semantics carry over unmodified; frequency does not alter race-performance equivalence, and no 3D-specific average race time is invented.

## 43. Readiness

Unchanged, fully generic: recent weekly volume, recent longest run, recent runs/week remain the readiness inputs regardless of frequency. Frequency may affect eligibility/feasibility downstream but does not and must not convert calibration into synthetic readiness. No new readiness score.

## 44. KEY/EASY/LONG allocation

Structurally verifiable (session-role shape, allocation *mechanism*) but not numerically verifiable to completion, since actual weekly-volume figures depend on the still-unfrozen peak-volume band (§22). The mechanism itself — one KEY, one EASY, one LONG, with LONG's share governed by the now-frozen LR-share quadruple (§30) and KEY/EASY splitting the residual — is unchanged from the existing, proven allocator used by every other Intermediate cell; no new allocation logic is required once a peak volume exists.

## 45. Long-run feasibility

Cannot be fully validated numerically without a frozen peak-volume/weekly-volume range (§22), but the *share* values themselves are structurally valid on their own terms: `0.34 ≤ 0.38 ≤ 0.40 ≤ 0.46` is a clean, correctly-ordered quadruple (min ≤ selected ≤ max ≤ hard cap) with real headroom at every step — unlike TEN_K's own collapsed shape — so no share-ordering defect exists independent of the peak-volume question.

## 46. Calendar feasibility

Fully confirmed conceptually: the existing 3D `RunLayout`/calendar machinery (KEY + EASY + LONG session placement, preferred-day handling, existing hard/hard-adjacency policy) is reused unmodified — no custom 16K calendar logic is needed or created.

## 47. Complete horizon simulations

**Not produced.** A full week-by-week simulation (weekly volume, KEY/EASY/LONG splits, LR share applied, absolute LR checked, taper position) requires a frozen peak-volume band as its starting input; with that input blocked (§22), no numerically complete horizon simulation can be honestly produced. The phase/stage skeleton itself (which weeks belong to which phase, at which horizon) is fully known and identical to 16K's own 4D skeleton (§14/§18), independent of volume.

## 48. Selected-peak semantics

Preserved conceptually (reference/ceiling, never a mandatory endpoint, shorter horizons may peak below it, longer horizons never force extrapolation past it) — the semantic *rule* is unchanged and ready to apply the moment a numeric selected peak exists; only the number itself is unresolved.

## 49. 16K 4D vs 16K 3D table

| Field | 16K I4D | Candidate 16K I3D | Same/Different | Why | Owner | Evidence |
|---|---|---|---|---|---|---|
| Structural parent | HALF_MARATHON | HALF_MARATHON | Same | Frequency does not alter structural parent | Parent | §11 |
| Catalog candidate | HALF_MARATHON×I×4D | HALF_MARATHON×I×3D | Different (correctly — different layout) | Distinct catalog combination for the frequency | Catalog | §12 |
| MinimumCoreWeeks | 10 | 10 | Same | Parent structural fact, frequency-blind by construction | Parent | §15 |
| PreferredCoreWeeks | 12 | 12 | Same | Target-owned, never frequency-parametrized | Target | §16 |
| MaximumCoreWeeks | 14 | 14 | Same | Same as above | Target | §17 |
| PreferredWeeklyIncreaseRate/HardRate | 0.07/0.08 | 0.07/0.08 | Same | Universal, zero exception at any anchor/frequency | Generic | §25 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | **2.0** | **Different** | Real, agreed cross-family 3D value | Frequency | §26 |
| PeakVolumeBandMin/Max | [34,46] | **BLOCKED** | Unresolved | No permitted derivation path, evidence mismatched | Target×Frequency (unresolved) | §22 |
| SelectedPeakReference | 40.0 | **BLOCKED** | Unresolved | Downstream of peak-volume | Target×Frequency (unresolved) | §23 |
| CalibrationStartingVolumeKm | 23.5 | **BLOCKED** | Unresolved | Downstream of selected peak | Target×Frequency (unresolved) | §24 |
| LR-share quadruple | 0.30/0.36/0.33/0.40 | **0.34/0.40/0.38/0.46** | **Different** | Parent-family-scoped at 3D, not level/frequency-invariant here | Parent | §30 |
| PreferredAbsolutePeakLongRunKm | 15.0 | 15.0 | Same | Demonstrated frequency-invariant within family | Target | §32 |
| StartingAbsoluteLongRunCapKm | null | null | Same | Frequency fact (non-null only at 5D/6D) | Frequency | §27 |
| Taper | 2wk/0.70+0.43 | 2wk/0.70+0.43 | Same | Parent-family authority, frequency-blind | Parent | §34 |
| RunLayout | KEY/EASY/EASY/LONG | KEY/EASY/LONG | Different (correctly — fewer sessions) | Catalog-defined per frequency | Catalog | §13 |
| Target-race-pace, Riegel, readiness, calendar, adaptation | Generic | Generic | Same | Fully generic mechanisms | Generic | §39-43 |

## 50. TEN_K/HM frequency-delta table

| Row | TEN_K 4D | TEN_K 3D | TEN_K effect | HM 4D | HM 3D | HM effect | Cross-anchor conclusion |
|---|---|---|---|---|---|---|---|
| Horizon (min/pref/max) | 8/12/14 | 8/12/14 | None | 10/14/16 | 10/14/16 | None | Frequency-invariant within each family |
| Peak volume | 38.0 | 22.5 | −41% | 43.0 | 34.0 | −21% | Real but different-magnitude change at each anchor — no shared reuse signal |
| Selected peak | Same as peak (no separate ceiling flag) | Same | — | 43.0 (ceiling) | 34.0 (ceiling) | −21% | Same as peak volume |
| Calibration | 24.0 | 12.0 | −50% | 25.0 | 20.0 | −20% | Different-magnitude change, no shared reuse signal |
| Growth rates | 0.07/0.08 | 0.07/0.08 | None | 0.07/0.08 | 0.07/0.08 | None | Frequency-invariant, universal |
| Absolute cap | 2.5 | 2.0 | −0.5 | 2.5 | 2.0 | −0.5 | **Identical delta — safely shared at 3D** |
| LR shares | 0.30/0.36/0.33/0.40 | 0.38/0.42/0.40/0.42 | Rises, collapsed shape | 0.30/0.36/0.33/0.40 | 0.34/0.40/0.38/0.46 | Rises, ascending shape | Both rise, but disagree in magnitude/shape — resolved as parent-family-scoped (§30) |
| Absolute peak LR | null | null | None | 19.0 | 19.0 | None | Frequency-invariant within each family |
| Starting LR cap | null | null | None | null | null | None | Frequency-invariant (non-null only at 5D/6D) |
| Taper | 1wk/0.53 | 1wk/0.53 | None | 2wk/0.70+0.43 | 2wk/0.70+0.43 | None | Parent-family-owned, frequency-blind |
| RunLayout | KEY/EASY×2/LONG (4D) | KEY/EASY/LONG (3D) | Fewer sessions | Same shape | Same shape | Same | Frequency-driven session-count change, identical shape logic in both families |
| Quality structure | 1 KEY, no KEY2 at either | Same | None | Same | Same | None | No secondary-quality lane at either 3D or 4D for either family |

## 51. Field-by-field orthogonality classifications

| Field | Classification |
|---|---|
| Structural parent | `PARENT_FAMILY_ONLY` |
| Catalog candidate | `PARENT_FAMILY_ONLY` (existing HM×I×3D candidate reused) |
| MinimumCoreWeeks | `PARENT_FAMILY_ONLY` |
| PreferredCoreWeeks / MaximumCoreWeeks | `TARGET_ONLY` |
| PreferredWeeklyIncreaseRate / HardWeeklyIncreaseRate | `GENERIC` |
| AbsoluteWeeklyIncreaseCapKm | `FREQUENCY_ONLY` |
| PeakVolumeBandMin/Max | `TARGET_X_FREQUENCY_INTERACTION` (unresolved — `INSUFFICIENT_EVIDENCE`) |
| SelectedPeakReference | `TARGET_X_FREQUENCY_INTERACTION` (unresolved, downstream) |
| CalibrationStartingVolumeKm | `TARGET_X_FREQUENCY_INTERACTION` (unresolved, downstream) |
| LR-share quadruple | `PARENT_FAMILY_ONLY` (resolved this phase) |
| PreferredAbsolutePeakLongRunKm | `TARGET_ONLY` |
| StartingAbsoluteLongRunCapKm | `FREQUENCY_ONLY` |
| Taper (duration/multipliers/mechanism) | `PARENT_FAMILY_ONLY` |
| RunLayout | `FREQUENCY_ONLY` (catalog-defined per frequency, HM-parent scoped) |
| Target-race-pace, Riegel, goal feasibility, readiness, calendar, adaptation | `GENERIC` |

## 52. Target-only authorities

`PreferredCoreWeeks`, `MaximumCoreWeeks`, `PreferredAbsolutePeakLongRunKm` — all reused directly from 16K's own already-frozen 4D values, unaffected by frequency.

## 53. Frequency-only authorities

`AbsoluteWeeklyIncreaseCapKm` (2.0), `StartingAbsoluteLongRunCapKm` (null), `RunLayout` (catalog-defined 3D layout) — all resolved this phase via direct cross-family agreement or direct catalog read.

## 54. Parent-family authorities

Structural parent (HALF_MARATHON), catalog candidate, `MinimumCoreWeeks` (10), the LR-share quadruple (0.34/0.40/0.38/0.46 — the central resolution of this phase), and taper (2-week/0.70/0.43/non-chained) — all reused directly from existing HM-parent authority.

## 55. Generic mechanisms

Growth rates, target-race-pace, Riegel, goal-feasibility, readiness, calendar, adaptation — all confirmed fully generic and unaffected by this cell.

## 56. Target×frequency interaction authorities

`PeakVolumeBandMin/Max`, `SelectedPeakReference`, `CalibrationStartingVolumeKm` — the one genuine cluster of fields this phase could not responsibly resolve. This is the honest, central finding of this phase: distance and frequency are NOT fully orthogonal for this field family — real, independent evidence at the exact target×frequency cell is required, and the two race-distance-matched external sources found diverge too widely (population mismatch, not measurement noise) to responsibly close without either inventing a number or violating the governing prompt's own explicit prohibition on ratio/interpolation derivation.

## 57. Complete implementation-ready authority table

| Field | Value | Owner | Classification | Source |
|---|---|---|---|---|
| TargetDistanceKm | 16.0 | Target | `TARGET_ONLY` | Existing |
| ParentDistanceFamily | HalfMarathon | Parent | `PARENT_FAMILY_ONLY` | §11 |
| Level | Intermediate | — | — | Fixed by experiment scope |
| RunsPerWeek | 3 | — | — | Fixed by experiment scope |
| RunLayout | KEY, EASY, LONG | Frequency | `FREQUENCY_ONLY` | §13 |
| MinimumCoreWeeks | 10 | Parent | `PARENT_FAMILY_ONLY` | §15 |
| PreferredCoreWeeks | 12 | Target | `TARGET_ONLY` | §16 |
| MaximumCoreWeeks | 14 | Target | `TARGET_ONLY` | §17 |
| PeakVolumeBandMin | **BLOCKED** | Target×Frequency | Unresolved | §22 |
| PeakVolumeBandMax | **BLOCKED** | Target×Frequency | Unresolved | §22 |
| SelectedPeakReference | **BLOCKED** | Target×Frequency | Unresolved | §23 |
| PreferredWeeklyIncreaseRate | 0.07 | Generic | `GENERIC` | §25 |
| HardWeeklyIncreaseRate | 0.08 | Generic | `GENERIC` | §25 |
| AbsoluteWeeklyIncreaseCapKm | 2.0 | Frequency | `FREQUENCY_ONLY` | §26 |
| CalibrationStartingVolumeKm | **BLOCKED** | Target×Frequency | Unresolved | §24 |
| LR PreferredShareMin | 0.34 | Parent | `PARENT_FAMILY_ONLY` | §30 |
| LR PreferredShareMax | 0.40 | Parent | `PARENT_FAMILY_ONLY` | §30 |
| LR SelectedShare | 0.38 | Parent | `PARENT_FAMILY_ONLY` | §30 |
| LR HardShareCap | 0.46 | Parent | `PARENT_FAMILY_ONLY` | §30 |
| PreferredAbsolutePeakLongRunKm | 15.0 | Target | `TARGET_ONLY` | §32 |
| StartingAbsoluteLongRunCapKm | null | Frequency | `FREQUENCY_ONLY` | §27 |
| TaperDurationWeeks | 2 | Parent | `PARENT_FAMILY_ONLY` | §34 |
| TaperWeek1Multiplier | 0.70 | Parent | `PARENT_FAMILY_ONLY` | §34 |
| TaperWeek2Multiplier | 0.43 | Parent | `PARENT_FAMILY_ONLY` | §34 |
| TaperMechanism | Non-chained, fixed-anchor | Parent | `PARENT_FAMILY_ONLY` | §35 |
| PrimaryProgression | HALF_MARATHON_MASTER v1 (reused) | Parent | `PARENT_FAMILY_ONLY` | §36 |
| QualitySessionCount | 1 (KEY only, no KEY2) | Frequency | `FREQUENCY_ONLY` | §37 |
| RaceSpecificPaceSemantic | Generic, TargetDistanceKm=16.0 | Generic | `GENERIC` | §39 |
| RecentRaceEquivalenceMethod | Riegel, unmodified | Generic | `GENERIC` | §40 |
| GoalFeasibilityBehavior | Generic, unmodified | Generic | `GENERIC` | §41-42 |
| ReadinessModel | Generic, unmodified | Generic | `GENERIC` | §43 |
| CalendarReuse | Generic 3D calendar machinery | Frequency | `FREQUENCY_ONLY` | §46 |
| AdaptationReuse | Generic, unmodified | Generic | `GENERIC` | Unaffected |

## 58. Evidence-confidence classifications

| Field | Tier |
|---|---|
| MinimumCoreWeeks, PreferredCoreWeeks, MaximumCoreWeeks | `DIRECT_EXISTING_AUTHORITY` |
| PreferredWeeklyIncreaseRate, HardWeeklyIncreaseRate | `DIRECT_EXISTING_AUTHORITY` |
| AbsoluteWeeklyIncreaseCapKm | `DIRECT_EXISTING_AUTHORITY` (real cross-family agreement) |
| StartingAbsoluteLongRunCapKm | `DIRECT_EXISTING_AUTHORITY` |
| PreferredAbsolutePeakLongRunKm | `DIRECT_EXISTING_AUTHORITY` |
| Taper (all fields) | `DIRECT_EXISTING_AUTHORITY` |
| LR-share quadruple | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| RunLayout, catalog candidate | `DIRECT_EXISTING_AUTHORITY` |
| PeakVolumeBandMin/Max, SelectedPeakReference, CalibrationStartingVolumeKm | `DECISION_REQUIRED` |

No field was upgraded to a stronger tier than its evidence supports.

## 59. Authority-owner classifications

| Field | Interaction classification |
|---|---|
| MinimumCoreWeeks, catalog candidate, structural parent, LR-share quadruple, taper | `REUSED_FROM_PARENT_AUTHORITY` |
| PreferredCoreWeeks, MaximumCoreWeeks, PreferredAbsolutePeakLongRunKm | `REUSED_FROM_TARGET_AUTHORITY` |
| AbsoluteWeeklyIncreaseCapKm, StartingAbsoluteLongRunCapKm, RunLayout | `REUSED_FROM_FREQUENCY_AUTHORITY` |
| Growth rates, pace, Riegel, feasibility, readiness, calendar, adaptation | `REUSED_FROM_GENERIC_MECHANISM` |
| PeakVolumeBandMin/Max, SelectedPeakReference, CalibrationStartingVolumeKm | `NEW_TARGET_FREQUENCY_CELL_AUTHORITY` — required but not yet supplied |

## 60. Numeric feasibility result

**Cannot be returned as `16K_INTERMEDIATE_3D_NUMERICALLY_FEASIBLE`.** Every resolved field is individually well-formed (LR-share quadruple correctly ordered with real headroom; taper mechanism valid; horizon structurally feasible at every supported week count per the unchanged catalog topology), but full numeric feasibility — actual KEY/EASY/LONG weekly allocations, actual long-run progression, actual growth-rate application across actual weekly volumes — cannot be verified without a frozen peak-volume band, since every one of those computations takes weekly volume as an input. The blocker is precisely and only the peak-volume/selected-peak/calibration cluster (§56).

## 61. Orthogonality result

**`PROJECTED_DISTANCE_AND_FREQUENCY_PARTIALLY_ORTHOGONAL`.** The large majority of authority (structural parent, catalog candidate, RunLayout, phase topology, horizon triple, growth rates, absolute weekly cap, starting-LR cap, absolute peak long run, taper, every generic mechanism) composes cleanly from existing target, frequency, parent-family, and generic sources with no new interaction field required — a genuine, substantial confirmation that the post-DIST-GEN.13 authority-composition architecture works as intended across a new frequency. One real, load-bearing exception exists: the long-run-share quadruple, which this phase resolves as parent-family-scoped rather than a true interaction (§30), and the peak-volume/selected-peak/calibration cluster, which remains a genuine, unresolved target×frequency question this phase's own evidence cannot responsibly close (§56). This is not equated with failure, per the governing prompt's own explicit instruction — a partially-orthogonal result with one precisely-scoped, honestly-disclosed evidence gap is the correct scientific outcome here, not an incomplete one.

## 62. Registry-architecture result

**`16K_INTERMEDIATE_3D_FITS_EXISTING_PROJECTED_TARGET_REGISTRY_ARCHITECTURE`.** Confirmed directly from source: `ApprovedDarkTargetDistanceCell(TargetDistanceKm, ParentDistanceFamily, Level, RunsPerWeek)` already carries `RunsPerWeek` as a first-class field, and `ProjectedTargetAuthority.Cell` reuses this exact record type. A future `(16.0, HalfMarathon, Intermediate, 3)` registration is structurally representable today with zero type change, zero dispatch change, and zero architecture redesign — directly confirming DIST-GEN.13/14's own prior claim, now verified against a real candidate cell rather than a hypothetical one.

## 63. 15K/18K 3D implications

This closure directly implies, but does not authorize: `MinimumCoreWeeks`, catalog candidate, RunLayout, absolute weekly cap, starting-LR cap, taper, and the LR-share quadruple (0.34/0.40/0.38/0.46, parent-family-scoped) would all very likely transfer directly to a future 15K×I×3D or 18K×I×3D closure with the same reasoning, since none of these fields' resolutions in this phase depended on anything specific to the number 16.0. What would NOT transfer, and would need independent closure for each: `PreferredAbsolutePeakLongRunKm` (each target's own exact-target value — 14.5 for 15K, 16.0 for 18K — reused unchanged by the same frequency-invariance logic established here, but each is its own independently-frozen number), and — most importantly — peak-volume band/selected-peak/calibration, which this very phase could not close even for 16K and would need genuinely fresh, target-specific evidence for each of 15K and 18K as well. No 15K or 18K cell is authorized by this phase.

## 64. 5D/6D implications

Nothing in this phase's own findings should be read as predicting 5D/6D's own LR-share ownership pattern, since 5D/6D introduce KEY2/secondary-quality-lane semantics entirely absent at 3D, and — per the existing cross-family evidence already on record (DIST-GEN.14's own digest) — TEN_K and HM already agree exactly on LR-share at 5D/6D (0.28/0.36/0.28/0.36 both), unlike the 3D disagreement this phase resolved. If anything, this confirms 5D/6D's own frequency-invariance pattern is already well-supported and does not need the same kind of resolution 3D required. No 5D/6D cell is authorized or pre-closed by this phase.

## 65. 4D projected zero-delta

Confirmed via `git status`: zero changes to any 15K, 16K, or 18K I4D policy file, value, or test. This phase's own diff contains no production source at all.

## 66. TEN_K zero-delta

TEN_K's own 3D and 4D authority (including its undocumented `ThreeDayIntermediate` values) remain completely untouched — read as evidence, never modified, never "corrected" to agree with HM.

## 67. HM zero-delta

HM's own 3D and 4D authority remain completely untouched — read as canonical anchors and directly reused where governance-appropriate (§30, §32, §34), never altered.

## 68. Existing product zero-delta

No production, catalog, database, API, or frontend file was changed by this phase. `git status --porcelain` before finalizing shows a diff consisting exactly of this report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md`.

## 69. DIST-GEN.16 readiness

**Not ready for full dark implementation.** DIST-GEN.16 (16K×Intermediate×3D full dark implementation) cannot proceed without making its own training-science decision on peak-volume-band/selected-peak/calibration, which this phase's own explicit scope prohibits doing via any permitted method given the evidence in hand. Everything else this report resolves — structural parent, catalog candidate, RunLayout, phase topology, full horizon triple, growth/cap/starting-cap/absolute-peak-LR/taper ownership, and, centrally, the LR-share parent-family resolution — is implementation-ready and requires no further governance work.

## 70. Remaining blockers

**One, precisely scoped:** `PeakVolumeBandMin`, `PeakVolumeBandMax`, `SelectedPeakReference`, and `CalibrationStartingVolumeKm` for the exact cell `16.0 × HalfMarathon × Intermediate × 3D` remain `DECISION_REQUIRED`. Root cause, fully disclosed: two genuinely race-distance-matched, 3-days-per-week evidence sources were found and directly evaluated (§21) — dedicated 10-mile 3-day content (13-24km/week, LR peaking 13-14.5km, population skewing toward a lower experience tier than Appsel's own Intermediate classification) and a dedicated half-marathon 3-day plan (40-43km/week, LR peaking 19km, population and structure a closer match to Appsel's own Intermediate tier but targeting the full HM distance, not 16K) — and they diverge by roughly a factor of two with no meaningful overlap, reflecting genuine population mismatch rather than noise around a shared estimate. The governing prompt's own explicit, repeated prohibition (§23-24, §76) against deriving this value via any ratio, interpolation, or nearest-anchor relationship to 16K's own 4D value or to either canonical family's own 3D value forecloses the remaining methods that could otherwise close this gap. This is reported honestly as a blocker rather than resolved by inventing a number, picking whichever evidence source is more convenient, or silently reusing a formula the governing prompt explicitly bans. A future phase could resolve this by: (a) commissioning genuinely dedicated "16K/10-mile, 3 days per week, intermediate (existing running base)" evidence specifically, since neither source found matches that exact population; or (b) explicitly authorizing, as its own governance decision, a `PRODUCT_DEFAULT`-tier closure within a disclosed envelope bounded by the two found sources — a legitimate path this phase did not take on its own authority, since the governing prompt's own emphatic repetition of its derivation prohibitions was read as intentionally raising the bar for this specific field, not merely restating standing policy.

---

## Final classification

**16K_INTERMEDIATE_3D_PROJECTED_DISTANCE_FREQUENCY_ORTHOGONALITY_AUTHORITY_BLOCKED**

Every other criterion in the governing prompt's own §84 checklist is met: structural parent is resolved (§11), catalog candidate is resolved (§12), RunLayout is resolved (§13), phase topology is resolved (§14), minimum/preferred/maximum horizons are frozen (§15-17), growth-rate/absolute-cap/starting-LR-cap ownership is resolved (§25-27), the canonical 3D LR-share conflict is explicitly and evidentially resolved in favor of parent-family authority (§28-30), absolute peak LR is frozen with the LR-below-target invariant respected (§31-33, §39), taper ownership is resolved (§34-35), progression reuse is resolved (§36), quality-session count is resolved (§37), target-race-pace/Riegel/goal-feasibility/readiness all remain confirmed generic (§39-43), the existing registry architecture is confirmed to represent this exact cell with zero redesign (§62), 16K×I×3D remains unsupported both publicly and dark (§3), existing 15K/16K/18K I4D behavior and both canonical anchors are fully untouched (§65-67), and no production source changed (§68). The sole reason this phase is not classified `_CLOSED` is the single, precisely-scoped, honestly-disclosed blocker in §70: the peak-volume-band/selected-peak/calibration cluster for this exact cell cannot be responsibly frozen given genuinely mismatched available evidence and the governing prompt's own explicit, repeated prohibition on every derivation method that could otherwise close it. Per this engagement's own established norm of honest `_BLOCKED` classification over forced closure, and per the governing prompt's own explicit instruction that "a blocked authority phase is valid," this phase reports blocked rather than inventing a number to force `DIST-GEN.16`'s own readiness. DIST-GEN.16 is not begun.
