# PHASE DIST-GEN.19 — 16K × Intermediate × 5D Projected-Distance Frequency-Orthogonality Structural and Numeric Authority Closure, with KEY2 Secondary-Quality-Lane Authority Audit

**GOVERNANCE / TRAINING-AUTHORITY / CROSS-AXIS-COMPOSITION PHASE. NO PRODUCTION IMPLEMENTATION, NO DARK OR PUBLIC ACTIVATION, NO NEW CATALOG COMBINATION.** Every fact below was re-read directly from actual phase reports and current source, not reconstructed from the task brief. Two material discrepancies from the brief are found and disclosed (§9, §25).

## 1. DIST-GEN.18 decision consumed

DIST-GEN.18 selected `NEXT_AXIS_16K_INTERMEDIATE_5D`, naming this exact phase, on the finding that a second I3D target would be corroborative (the target×frequency exact-cell-cluster problem shape is already fully characterized by DIST-GEN.15/15A/15B), whereas 5D opens the KEY2/secondary-quality-lane dimension no projected target has ever tested. DIST-GEN.18 also reported, citing a direct read of `VolumeSafetyPolicy.cs`, that canonical TEN_K and HM Intermediate×5D "agree" on LR-share (0.28/0.36/0.28/0.36) and on `StartingAbsoluteLongRunCapKm` being non-null (12.0). This phase independently re-verifies both claims from source rather than trusting them: the LR-share claim is **confirmed correct**; the `StartingAbsoluteLongRunCapKm` claim is **wrong** (§9, §25).

## 2. Repository baseline

`git fetch origin main && git rev-list --left-right --count HEAD...origin/main` → `0 0`. HEAD confirmed at `9ef898c` ("docs(dist-gen-18): backfill governance commit SHA for DIST-GEN.18"), with `3a91034` as DIST-GEN.18's primary commit — both match the brief exactly. This phase is governance-only; no production regression suite is run, per instruction. Direct source reads (`VolumeSafetyPolicy.cs`, `Dark16KPilotEligibilityPolicy.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `run-layout-5d.v1.json`, `half-marathon-5d-intermediate.v1.json`, `half-marathon-workout-progression.v2.json`, `TargetDistance16KHorizonPolicy.cs`) substitute for a regression run.

## 3. Current public/dark matrix

Frozen, not reopened. PUBLIC = DARK = {15K×I×4D, 16K×I×3D, 16K×I×4D, 18K×I×4D} — confirmed directly from `Dark16KPilotEligibilityPolicy.ApprovedPublicCells`/`ApprovedDarkCells`, exactly 4 entries each, byte-identical to the brief. No 16K×I×5D entry exists anywhere in either list.

## 4. Exact experiment cell

`TargetDistanceKm=16.0, ParentDistanceFamily=HalfMarathon, Level=Intermediate, RunsPerWeek=5`. No 15K/18K×I×5D, no 16K×I×6D, no Beginner/Advanced, no registration or activation performed.

## 5. Research question

**`PROJECTED_DISTANCE_AND_5D_FREQUENCY_PARTIALLY_ORTHOGONAL`.** The overwhelming majority of authority (structural parent, catalog candidate, RunLayout, phase topology, horizon, growth rates, absolute weekly cap, LR-share quadruple, taper, absolute-peak-LR, and — centrally — the entire KEY2/secondary-quality-lane semantic surface) composes cleanly from existing target, parent-family, frequency, and generic sources with zero new interaction field. Exactly one cluster — `PeakVolumeBandMin/Max`, `SelectedPeakReference`, `CalibrationStartingVolumeKm` — remains a genuine, unresolved target×frequency question this phase's evidence cannot responsibly close without either an explicit product decision or interpolation, both of which this phase is not authorized to perform unilaterally (§32 of the governing prompt). Not forced to `FULLY_ORTHOGONAL`.

## 6. Canonical 4D/5D reference matrix

Read directly from `VolumeSafetyPolicy.cs`:

| Field | TEN_K I4D (`Default`) | TEN_K I5D (`FiveDayIntermediate`) | HM I4D (`HalfMarathonIntermediate4D`) | HM I5D (`HalfMarathonIntermediate5D`) |
|---|---|---|---|---|
| PreferredWeeklyIncreaseRate/Hard | 0.07/0.08 | 0.07/0.08 | 0.07/0.08 | 0.07/0.08 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | 2.5 | 2.5 | 2.5 |
| GoldenFixtureStartingVolumeKm | 24.0 | 26.0 | 25.0 | 29.5 |
| ResolvedPeakReference | 38.0 | 44.5 | 43.0 | 51.0 |
| PeakVolumeBand (catalog) | — | — | [36,50] | [44,58] |
| ResolvedPeakReferenceIsSelectedCeiling | false | false | true | true |
| GoldenFixtureNonTaperTransitions | 10 | 10 | 11 | 11 |
| Taper | 1wk/0.53 | 1wk/0.53 | 2wk/0.70+0.43 non-chained | 2wk/0.70+0.43 non-chained (reused from 4D) |
| LR PreferredMin/Max/Selected/HardCap | 0.30/0.36/0.33/0.40 | **0.28/0.36/0.28/0.36** | 0.30/0.36/0.33/0.40 | **0.28/0.36/0.28/0.36** |
| PreferredAbsolutePeakLongRunKm | null | null | 19.0 | 19.0 |
| **StartingAbsoluteLongRunCapKm** | null | **null** | null | **12.0** |
| RunLayout (sessions) | 1 KEY, 1 EASY | 2 KEY, 2 EASY | 1 KEY, 2 EASY | 2 KEY, 2 EASY |

**TEN_K and HM genuinely agree at 5D on**: growth rates, absolute weekly cap (2.5), LR-share quadruple (0.28/0.36/0.28/0.36 — byte-identical), and RunLayout session-shape (2 KEY, 2 EASY, 1 LONG). **They genuinely disagree on**: `StartingAbsoluteLongRunCapKm` (TEN_K=null, HM=12.0 — see §9/§25, this is the DIST-GEN.18 discrepancy) and taper (TEN_K 1wk/0.53, HM 2wk/0.70/0.43 — a pre-existing, already-governed parent-family difference, not new).

## 7. 16K projected reference cells

`HalfMarathonIntermediate4DTargetDistance16K`: cap 2.5, calibration 23.5, peak 40.0 (band [34,46]), LR 0.30/0.36/0.33/0.40 (shared 4D authority), absolute-peak-LR 15.0, `StartingAbsoluteLongRunCapKm=null` (referenced from `IntermediateFourDayGrowthAuthority`). `HalfMarathonIntermediate3DTargetDistance16K`: cap 2.0, calibration 20.0 (exact-cell), peak 34.0 (band [28,40], exact-cell), LR 0.34/0.40/0.38/0.46 (parent-family), absolute-peak-LR 15.0 (reused), `StartingAbsoluteLongRunCapKm=null` (frequency authority at 3D). Horizon for both: `MinimumCoreWeeks=10, PreferredCoreWeeks=12, MaximumCoreWeeks=14`, target-owned, frequency-invariant (`TargetDistance16KHorizonPolicy`, no `RunsPerWeek` parameter anywhere in its declaration).

## 8. TEN_K 4D→5D delta

Growth rates unchanged. Absolute cap unchanged (2.5→2.5). LR-share: 0.30/0.36/0.33/0.40 → 0.28/0.36/0.28/0.36 (preferred-min −0.02, selected −0.05, hard-cap −0.04, preferred-max unchanged) — a real, already-shipped, *decrease* with more sessions. Peak volume 38.0→44.5 (+17%). Calibration 24.0→26.0. Absolute peak LR: null→null (unchanged). `StartingAbsoluteLongRunCapKm`: null→null (unchanged — **TEN_K never sets this field non-null at any frequency**, confirmed by direct read: neither `FiveDayIntermediate` nor `SixDayIntermediate` carries a `{ StartingAbsoluteLongRunCapKm = ... }` object-initializer override). Taper unchanged (1wk/0.53). RunLayout: 1 KEY→2 KEY (KEY2 introduced).

## 9. HM 4D→5D delta

Growth rates unchanged. Absolute cap unchanged (2.5→2.5). LR-share: 0.30/0.36/0.33/0.40 → 0.28/0.36/0.28/0.36 (identical delta shape to TEN_K's own, per HM.10 §9's own explicit disclosure that this was derived by applying TEN_K's own real 4D→5D directional delta to HM's own 4D baseline, landing coincidentally on TEN_K's own digits). Peak volume 43.0→51.0 (+19%, comparable magnitude to TEN_K's own). Calibration 25.0→29.5. Absolute peak LR: 19.0→19.0 (unchanged — frequency-invariant within HM, now confirmed at four frequencies: 3D/4D/5D/6D all 19.0). Taper unchanged (2wk/0.70/0.43, reused directly from 4D per HM.10 §12). **`StartingAbsoluteLongRunCapKm`: null→12.0 — a real, non-trivial change, introduced starting at 5D.** RunLayout: 1 KEY, 2 EASY → 2 KEY, 2 EASY (KEY2 introduced; `RUN_LAYOUT_5D` = `[KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN]`, confirmed directly from `run-layout-5d.v1.json`).

**Correction of the DIST-GEN.18 brief**: DIST-GEN.18 §19 stated TEN_K and HM "agree" that `StartingAbsoluteLongRunCapKm` is non-null (12.0) at 5D. Direct source reading shows this is **false**: `VolumeSafetyPolicy.FiveDayIntermediate` (TEN_K I5D) carries no override and therefore inherits the base record's default of `null`; only `HalfMarathonIntermediate5D` (and, identically, `HalfMarathonIntermediate6D`) sets it to `12.0`. This is a genuine cross-family **disagreement** at 5D, of the same shape as the LR-share disagreement DIST-GEN.15 found at 3D — except here the correct resolution is easier: the field is null at HM 3D/4D and non-null (12.0) at HM 5D/6D, a clean, internally-consistent, parent-family-scoped **and** frequency-scoped rule (never varies within TEN_K, which stays null at every frequency including 5D/6D). This is a `PARENT_FAMILY_X_FREQUENCY` interaction, not a frequency-only cross-family convergence — precisely the "value equality ≠ authority equality" discipline this engagement applies throughout, here manifesting as its mirror image: apparent cross-family *non*-agreement that is actually a clean, disclosed, single-family rule, not evidence of ambiguity.

## 10. Cross-anchor frequency analysis

| Field | Classification |
|---|---|
| Growth rates | `CONSISTENT_FREQUENCY_AUTHORITY` — unchanged at every anchor |
| Absolute weekly cap | `CONSISTENT_FREQUENCY_AUTHORITY` — both families agree exactly at 5D (2.5), as at 4D |
| LR-share quadruple | `CONSISTENT_FREQUENCY_AUTHORITY` at 5D (both families land on 0.28/0.36/0.28/0.36) — genuinely different from 3D's disagreement |
| Absolute peak LR | `CONSISTENT_FREQUENCY_AUTHORITY` within each family (HM 19.0 at every frequency; TEN_K null at every frequency) |
| `StartingAbsoluteLongRunCapKm` | `PARENT_FAMILY_X_FREQUENCY_INTERACTION` — null throughout TEN_K; null at HM 3D/4D, 12.0 at HM 5D/6D. **Not** a cross-family agreement (correcting §9) |
| Taper | `PARENT_FAMILY_ONLY`, frequency-blind within each family |
| Peak volume/selected peak/calibration | `TARGET_FREQUENCY_INTERACTION_SIGNAL` — real, comparable-magnitude increases at both anchors (+17%/+19%), but this is descriptive only; no 16K value is derived from either anchor's own delta (§27) |
| RunLayout/quality structure | `WORKOUT_ROLE_INTERACTION` — both families independently introduce a second KEY slot at 5D, a structural, not numeric, convergence |

## 11. Structural parent

Confirmed unchanged: 16K's structural parent is HALF_MARATHON. `HALF_MARATHON_MASTER v2` (the template `half-marathon-5d-intermediate.v1.json` references) is the same distance-family template every HM combination uses; frequency selects the layout/master version, never the structural parent. Not reopened.

## 12. Catalog candidate

Confirmed via direct catalog read: `plan-catalog/catalog/combinations/half-marathon-5d-intermediate.v1.json` exists, is `VALIDATED`, and specifies `masterTemplate: HALF_MARATHON_MASTER v2`, `layout: RUN_LAYOUT_5D v1`, `levelModifier: INTERMEDIATE_MODIFIER v9`, `rulePack: APPSEL_RACE_PLAN_V1 v10`. No new catalog combination is needed for 16K×I×5D — this existing candidate is the one a future implementation would reuse, exactly as 16K's own 3D/4D cells reuse the corresponding existing HM combinations.

## 13. RunLayout

`run-layout-5d.v1.json`: `[KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN]` — 2 KEY, 2 EASY, 1 LONG. Frozen exactly as read. This is the first RunLayout any projected target has ever needed with two KEY slots.

## 14. Horizon ownership

`MinimumCoreWeeks=10, PreferredCoreWeeks=12, MaximumCoreWeeks=14` — reused directly from 16K's own existing `TargetDistance16KHorizonPolicy`, which is not frequency-parametrized (takes no `RunsPerWeek` argument) and has already been independently confirmed frequency-invariant once (DIST-GEN.15, 3D). Cross-checked against HM's own horizon: HM Intermediate 3D/4D/5D/6D all reference the identical HM-parent horizon bounds (10/14/16, per HM's own already-frozen master), reconfirming frequency does not alter horizon within either family. **Classification: `TARGET_ONLY`, `DIRECT_EXISTING_AUTHORITY` (frequency-invariance pattern now independently demonstrated at two prior frequencies for this exact target, plus at all four frequencies for the parent).**

## 15. Phase topology

`HALF_MARATHON_MASTER v2` (used by 5D) and `v1` (used by 3D/4D) carry the identical `FOUNDATION/BUILD/RACE_SPECIFIC/TAPER` phase-week-minima structure (per HM.11's own ledger record: "isolate the new two-lane 5D shape while 3D/4D remain on unchanged v1 authority" — v2 exists to add the KEY2 lane content, not to change phase allocation). Phase minima are frequency-invariant within HM, confirmed at HM.4/HM.6/HM.9/HM.10's own closures and reconfirmed here by direct catalog read: no phase-level `PreferredWeeks`/`MinimumWeeks`/`MaximumWeeks` field differs between the v1 and v2 templates. **Classification: `PARENT_FAMILY_ONLY`, `DIRECT_EXISTING_AUTHORITY`.**

## 16. Peak-volume anchors

Canonical TEN_K I5D: band not separately declared in `VolumeSafetyPolicy.cs` (TEN_K bands live in catalog JSON, consistent with DIST-GEN.15A §8's own finding), `ResolvedPeakReference=44.5`, `GoldenFixtureStartingVolumeKm=26.0`. Canonical HM I5D: band [44,58] (`HM_21K_Claude_Handoff_and_Build_Plan.md`, HM.10 §3), `ResolvedPeakReference=51.0` (exact midpoint), `GoldenFixtureStartingVolumeKm=29.5`. 16K's own I4D: band [34,46], peak 40.0, calibration 23.5. Context only — no scaling applied to any of these for 16K's own I5D value (§27).

## 17. Existing 16K/10-mile 5D evidence

**This is the single most important evidentiary finding of this phase.** DIST-GEN.15A (re-read directly, not from memory, per the governing prompt's own explicit instruction) already fetched, in full, **Hal Higdon's Intermediate 15K/10-Mile program** — and classified it at the time as `UPPER_ANCHOR_CONTEXT` **only because its 5-day frequency did not match the 3-day cell DIST-GEN.15A was closing.** For a genuine 16K×I×5D cell, this exact same source is now a **direct three-way match**: race distance (15K/10-mile ≈ 16.09km, the same near-exact match already accepted at 3D), frequency (exactly 5 running days/week), and population (Higdon's own "Intermediate" label, the same tier name Appsel uses). DIST-GEN.15A's own extracted figures for this program: 10-week plan, peak weekly running volume **37-38 miles (59.5-61.2 km)** in week 9, peak long run **10 miles (16.1 km)** in week 9, **2 quality sessions/week** (Tuesday intervals + Wednesday tempo — a genuine two-quality-session structure, corroborating that `RUN_LAYOUT_5D`'s 2-KEY shape is a real, externally-observed pattern for this exact race-distance-and-frequency combination, not merely an Appsel/10K-family structural artifact).

## 18. Supplemental external research

Not performed as fresh web search in this phase: the governing prompt's own priority ordering (§29-31) requires exhausting existing evidence first, and DIST-GEN.15A's own already-fetched, full-text Higdon Intermediate 15K/10-Mile source is a materially stronger direct match for 5D than anything available for 3D was (§17) — re-deriving it via a second live fetch would add no new information. No new search was conducted.

## 19. Peak-volume band

**Not frozen.** See §27 for full reasoning: Higdon's own peak (59.5-61.2 km/week) sits meaningfully *above* HM's own canonical [44,58] band's entire range, and well above TEN_K I5D's own 44.5 peak — a real, disclosed tension between the single best-matched direct source and the closest structural/parent-family anchor, not resolvable by any permitted method (no interpolation, no ratio, no nearest-anchor substitution) without either an explicit product decision (which this phase is not authorized to issue on its own authority, per governing-prompt §32) or discarding one of the two conflicting evidence classes without principled justification.

## 20. Selected peak

**Not frozen — mechanically downstream of §19.** The band-midpoint convention (re-confirmed at HM.10 §4 as a real, now-thrice-confirmed-and-counting pattern, still classified `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, never a parameterless rule) cannot be applied without a frozen band.

## 21. Calibration

**Not frozen — mechanically downstream of §19-20.** The sibling-ratio-reuse convention (HM.10 §7 reused HM I4D's own 25.0/43.0=0.5814 ratio against HM I5D's own new peak) requires a frozen peak to apply; none exists for 16K×I×5D.

## 22. Growth rates

`PreferredWeeklyIncreaseRate=0.07`, `HardWeeklyIncreaseRate=0.08` — confirmed identical at every single named instance in `VolumeSafetyPolicy.cs`, including both 5D instances. **Classification: `GENERIC`, `DIRECT_EXISTING_AUTHORITY`.** No new decision.

## 23. Absolute weekly cap

`AbsoluteWeeklyIncreaseCapKm=2.5` — confirmed by exact agreement between `FiveDayIntermediate` (TEN_K) and `HalfMarathonIntermediate5D` (HM), both 2.5, matching 4D's own shared value (unlike 3D's outlier 2.0). **Classification: `FREQUENCY_ONLY` (Intermediate×5D-scoped, though numerically it also equals 4D's own value — a real, evidenced convergence, not assumed), `DIRECT_EXISTING_AUTHORITY`.**

## 24. LR-share ownership

`0.28/0.36/0.28/0.36` — confirmed byte-identical between `FiveDayIntermediate` (TEN_K) and `HalfMarathonIntermediate5D` (HM). Unlike 3D (where the two families disagreed and the field was resolved as HM-parent-scoped), 5D shows genuine, real, independently-computed cross-family agreement, consistent with DIST-GEN.18's own correct prediction and DIST-GEN.14's own original evidence digest. Per the "value equality ≠ authority equality" discipline, this agreement is treated as evidence *of* frequency-level ownership (not assumed from the numeric match alone) because the underlying derivations are independent: TEN_K's own value is a real, already-shipped 4D→5D delta; HM's own value (HM.10 §9) was derived by applying that same real TEN_K delta-*methodology* to HM's own 4D baseline — an intentional replication of methodology, not a copy of digits, that happens to land on the same numbers, explicitly disclosed as such in HM.10 §9 itself. **Classification: `FREQUENCY_ONLY` (Intermediate×5D-scoped), `EVIDENCE_INFORMED_PRODUCT_DEFAULT`** (matching HM.10's own honest tier for this field — `PRODUCT_DEFAULT` at HM's own closure, not `STRONG_EVIDENCE`, since the delta-application methodology is principled but not itself evidence-backed).

## 25. Starting-LR-cap ownership

**Corrects the DIST-GEN.18 brief (§9 above).** `StartingAbsoluteLongRunCapKm` is **not** cross-family agreed at 5D: TEN_K remains null (confirmed: neither `FiveDayIntermediate` nor `SixDayIntermediate` sets this field); HM is 12.0 at 5D and 6D specifically (confirmed: `HalfMarathonIntermediate5D`/`HalfMarathonIntermediate6D` are the only two named policies in the entire file that set this field non-null via object-initializer syntax). Within HM, this is a clean, disclosed, structural rule: null at HM's own Intermediate 3D/4D, 12.0 at HM's own Intermediate 5D/6D — HM.10 §11/§20 and HM-X1.2 both classify the 12.0 value `DIRECT_EXISTING_AUTHORITY`, "frequency-independent" in their own text, meaning independent of *which* frequency within {5,6}, not independent of frequency altogether (both reports pre-date and are silent on 3D/4D's own null, since neither report needed to address it). Since 16K's structural parent is HALF_MARATHON, and this is an internally consistent, disclosed, parent-scoped rule that activates specifically at RunsPerWeek≥5 within HM, 16K×I×5D is expected to inherit `StartingAbsoluteLongRunCapKm=12.0` by the same reasoning DIST-GEN.15/16 already applied to reuse HM's own null at 3D/4D. **Classification: `PARENT_FAMILY_X_FREQUENCY` (a genuinely new taxonomy entry — depends on both the HM parent identity and the ≥5D frequency threshold jointly), `DIRECT_EXISTING_AUTHORITY`** (a clean, consistently-applied, disclosed rule with zero exception found within HM at any of its four Intermediate frequencies).

## 26. Absolute peak LR ownership

16K's own `PreferredAbsolutePeakLongRunKm=15.0` (target-only) is reused unchanged at both its own already-frozen 3D and 4D cells. HM's own value is 19.0 at every one of its four frequencies (3D/4D/5D/6D, all confirmed directly from source) — the strongest frequency-invariance evidence this engagement has for any field, now confirmed at four points rather than two. **16K×I×5D reuses 15.0 directly, unchanged.** **Classification: `TARGET_ONLY`, `DIRECT_EXISTING_AUTHORITY`.** LR-below-target invariant (15.0 < 16.0) holds.

## 27. Taper

HM's own taper (2 weeks, 0.70/0.43, non-chained fixed-anchor) is identical at 3D/4D/5D/6D — HM.10 §12 explicitly reuses 4D's own multipliers directly for 5D, re-verifying (not re-deriving) that 5D's own peak (51.0) sits below the one disclosed volume threshold (~56km/week, best-running-tips.com) that would trigger a different taper regime. TEN_K's own genuinely different taper (1 week, 0.53) is likewise identical across its own 4D/5D/6D. **16K×I×5D reuses the identical HM-parent taper 16K's own 3D/4D cells already consume.** **Classification: `PARENT_FAMILY_ONLY`, `DIRECT_EXISTING_AUTHORITY`.**

## 28. KEY2 introduction

The central new dimension. `RUN_LAYOUT_5D`'s second `KEY_SESSION` slot is populated, for canonical HM, by `HALF_MARATHON_WORKOUT_PROGRESSION v2` (`half-marathon-workout-progression.v2.json`) — a distinct file from the v1 progression 3D/4D use, but **not** a per-frequency or per-target file: its `distanceFamily` field is `"HALF_MARATHON"` (no `RunsPerWeek`/target field anywhere in the schema), and it is directly confirmed, from two independent doc comments, to be reused byte-identically at both 5D and 6D (`HalfMarathonIntermediate6D`'s own doc comment: "KEY2... remains light-quality FARTLEK... EASY_EQUIVALENT... implemented via the existing, unmodified half-marathon-workout-progression.v2.json (no new progression file, HM-X1.2 §7)"). This means KEY2's entire semantic content is **parent-family (HALF_MARATHON) authority**, selected into a candidate purely by which `RUN_LAYOUT`/`masterTemplate` version that candidate references (a catalog/frequency-driven *selection*, not a catalog/frequency-driven *content* decision). Since 16K's structural parent is HALF_MARATHON and its own catalog candidate (§12) already references this exact same v2 progression file unmodified, KEY2 for 16K×I×5D requires **zero new workout catalog authority**.

## 29. KEY2 phase-by-phase audit

Read directly from `half-marathon-workout-progression.v2.json`'s lane-1 (secondary/KEY2) stages:

| Phase | Stage | Workout | Min/Max exposures | Requires | Fallback |
|---|---|---|---|---|---|
| FOUNDATION | `FOUNDATION_HM_SECONDARY_EASY` | `EASY_STANDARD v4` | 2/4 | none | — |
| BUILD | `BUILD_HM_SECONDARY_FARTLEK` | `FARTLEK v6` | 3/5 | `GOAL_FEASIBILITY_IN [REALISTIC, CHALLENGING]` | `BUILD_HM_SECONDARY_EASY_FALLBACK` (`EASY_STANDARD v4`, 3/5) |
| RACE_SPECIFIC | `RACE_SPECIFIC_HM_SECONDARY_FARTLEK` | `FARTLEK v6` | 3/5 | same | `RACE_SPECIFIC_HM_SECONDARY_EASY_FALLBACK` (`EASY_STANDARD v4`, 3/5) |
| TAPER | `TAPER_HM_SECONDARY_EASY` | `EASY_STANDARD v4` | 2/2, `PROTECTED`/`FIXED_EXPOSURE` | none | — |

No stage in this lane declares `preferredExposures` (only min/max) — unlike lane-0's own `BUILD_FASTER_THAN_HM_INTRO` stage, which explicitly declares `preferredExposures: 1`. This is a real, disclosed asymmetry: KEY2's own "preferred" exposure count is not separately authored anywhere in the catalog and is left to whatever the generic allocator resolves from min/max alone.

## 30. TEN_K vs HM KEY2 comparison

TEN_K's own 5D/6D secondary-KEY workout content was not independently re-fetched in this phase (out of scope: 16K's structural parent is HALF_MARATHON, and per DIST-GEN.15's own established discipline, TEN_K's own values are context/comparison only, never a direct source for an HM-parented target). From HM.9/HM.10's own doc comments (consumed, not re-derived): HM's own KEY2 semantics were designed with explicit reference to the *shape* of an already-real, already-shipped 10K dual-KEY session-allocation mechanism (`V1FourDaySessionVolumeAllocationPolicy`, HM.10 §14, "already exercised in real, live 10K production today" — TEN_K's own 6D Core famously runs 2K+3E+1L), but the workout *content* (FARTLEK-family light-quality vs EASY_EQUIVALENT) is HM's own frozen structural authority (HM.9 §1, not reopened here). This is descriptive, cross-family architectural corroboration (the mechanism generalizes), not a numeric or content comparison.

## 31. KEY1 vs KEY2 separation

Confirmed clean throughout the catalog: KEY1 (lane 0) carries the primary FARTLEK-intro/THRESHOLD_TEMPO/HM_PACE progression, gated on `PACE_SOURCE_IN [RECENT_RACE]` for its race-specific/taper race-pace stages — this is where target-race-pace (16K's own `TargetDistanceKm=16.0`) would apply. KEY2 (lane 1) never references `HM_PACE` or any race-pace workout at any phase — it is FARTLEK (light-quality, effort-based) or EASY_STANDARD throughout, at every phase including RACE_SPECIFIC. **16K×I×5D would use existing HM-parent KEY1 progression with 16K's own generic target-race-pace substitution (identical mechanism to 16K's own already-proven 3D/4D KEY1 reuse), while KEY2 remains entirely parent/frequency-owned, with zero target-distance interaction of any kind.**

## 32. KEY2 intensity

Confirmed via HM.9/HM.10's own frozen structural authority (not reopened): KEY2 is explicitly and repeatedly classified as never a "true hard" stimulus — `MaximumTrueHardStimuliPerWeek=2` is HM's own governed ceiling counting KEY1+LONG as the two true-hard slots, with KEY2 deliberately excluded from that count. FARTLEK in Build/RaceSpecific is "light-quality," EASY_EQUIVALENT in Foundation/Taper. This directly matches the governing prompt's own §17 warning not to treat 5D as mechanically "two hard days" — confirmed: it is one hard day (KEY1) plus one light-quality/easy-equivalent day (KEY2) plus the long run.

## 33. KEY2 pace authority

Confirmed directly: `CatalogSessionPrescriptionPlanner`'s pace-resolution switch maps `FARTLEK` to a `"SURGE_AND_FLOAT"` effort-only pace category (not a fixed-pace target-race calculation), the same generic effort-only branch `EASY_STANDARD` uses (HM.10 §19, directly confirmed: "the secondary FARTLEK lane does not depend on exact HM pace evidence at all... falls to the generic effort-only branch, same as EASY_STANDARD"). **KEY2 never uses target-race pace, threshold pace, or any distance-specific pace figure at any phase — it is effort/tempo-structure-based throughout.** **Classification: `GENERIC` (effort-based, not target-scoped), `DIRECT_EXISTING_AUTHORITY`.**

## 34. KEY2 exposure authority

Min/max only (§29); no `preferredExposures` authored for any lane-1 stage. `MinimumExposures=0` is never used for KEY2 (its floor is always ≥2); `MaximumExposures` is 4 (Foundation) or 5 (Build/RaceSpecific) or 2 (Taper, fixed). Since no preferred value is authored, the generic stage allocator (`ProgressionStageAllocator`) resolves the actual exposure count from the phase's own total available weeks and the stage's compression/extension behavior — the same generic mechanism every other min/max-only stage in the catalog already uses (confirmed present and load-bearing for numerous lane-0 stages too, e.g. `BUILD_THRESHOLD_DEVELOPMENT`, `FOUNDATION_AEROBIC_BASE`). **Classification: `CATALOG_FREQUENCY` (governed by the existing, unmodified HM progression file), `DIRECT_EXISTING_AUTHORITY`.** No new exposure number is invented for 16K.

## 35. PreferredExposures interaction

Confirmed: KEY2 never declares a `preferredExposures` field, so it never participates in whatever compression/extension tie-break logic specifically privileges an authored preferred value over the bare min/max range. This is existing, unmodified behavior — not something 16K×I×5D would change or need to change.

## 36. Compression behavior

Every lane-1 stage except Taper is `COMPRESSIBLE`/`EXTENDABLE`; Taper's lane-1 stage is `PROTECTED`/`FIXED_EXPOSURE` (exactly 2, never reduced). HM.11's own ledger record confirms this exact progression file was already dark-materialized and verified across all seven 10-16W horizons for HM's own Intermediate 5D cell ("dual-lane taper verification" explicitly named in the HM.11 ledger entry) with zero horizon producing an invalid or missing KEY2 exposure. **No compression bug converting KEY2's preferred exposure into a mandatory one can exist, because no preferred exposure is authored for KEY2 at all** — the theoretical failure mode named in governing-prompt §54/§68 does not apply to this specific catalog content, a real, disclosed structural fact rather than an assumption.

## 37. RaceSpecific KEY2 behavior

Confirmed: `RACE_SPECIFIC_HM_SECONDARY_FARTLEK` remains FARTLEK (light-quality, effort-based), not a target-specific pace workout — KEY2 does **not** become more target-specific in the race-specific phase; only KEY1 (via `RACE_SPECIFIC_HM_DEVELOPMENT`/`RACE_SPECIFIC_HM_REHEARSAL`, both `HM_PACE`-gated on `PACE_SOURCE_IN [RECENT_RACE]`) carries target-specific race pace. This answers governing-prompt §45 directly: 16K×I×5D's race-specific KEY2 would **not** require any new workout not already present in HM I5D — it reuses `RACE_SPECIFIC_HM_SECONDARY_FARTLEK`/its own easy fallback verbatim, with zero 16K-specific content.

## 38. Taper KEY2 behavior

`TAPER_HM_SECONDARY_EASY` is `EASY_STANDARD`, `PROTECTED`/`FIXED_EXPOSURE` at exactly 2 — KEY2 becomes fully easy-equivalent during taper, never a quality exposure, confirming the governing prompt's own §40/§19 expected pattern directly from source rather than assuming it.

## 39. Workout-catalog reuse

Confirmed: `FARTLEK v6` and `EASY_STANDARD v4` are both pre-existing, already-approved `WORKOUT_DEFINITION`s already exercised in HM's own live 5D/6D production (HM.10 §13, HM-X1.2's own doc comments). **Zero new workout catalog authority is required for 16K×I×5D's own KEY2 lane.** This satisfies governing-prompt §46 directly: full reuse, no new pace token, no new stage.

## 40. Quality-day safety

Confirmed: KEY1 (true-hard) + LONG (true-hard, per HM's own `MaximumTrueHardStimuliPerWeek=2` ceiling) + KEY2 (light-quality/easy-equivalent, explicitly excluded from that count) — this is a genuine two-true-hard-plus-one-light-quality structure, not "three hard days." Calendar hardness-awareness (whether the generic calendar day-placement algorithm actually distinguishes which `KEY_SESSION` instance is hard) was an open `IMPLEMENTATION_CAPABILITY_GAP` at HM.10's own closure (§17 of that report) — **resolved generically at HM.11** (confirmed via `PHASE_LEDGER.md`/`MASTER_ROADMAP.md` row 27: "`GENERIC_CALENDAR_HARDNESS_CAPABILITY_GAP`... fixed it with a generic workout-family-derived hardness bridge... no HM/5D/lane/day special case"). This generic fix is already live, target-blind, and would automatically cover 16K×I×5D with zero new calendar code, exactly as governing-prompt §41-43 requires.

## 41. Calendar stress classification

KEY2 classifies as `LIGHT_QUALITY`/effort-based for calendar hardness purposes (not `HARD`), per the same generic hardness bridge (§40). No new calendar rule is needed; the existing generic mechanism already correctly distinguishes it from KEY1/LONG.

## 42. Target-race pace

Unchanged, fully generic for KEY1/race-specific sessions: `TargetDistanceKm=16.0` drives target-race pace identically regardless of frequency, exactly as already twice-demonstrated at 16K's own 3D/4D cells. KEY2 never consumes target-race pace at any phase (§31/§33).

## 43. Riegel

Unchanged, fully generic. No new exponent, no frequency-dependent adjustment.

## 44. Goal-time fitness separation

Unchanged: a desired 16K finish time does not manufacture current training fitness at any frequency, including 5D.

## 45. Readiness

Unchanged, fully generic: recent weekly volume, recent longest run, recent runs/week remain the readiness inputs regardless of frequency. No new minimum-runs/week or minimum-mileage threshold is manufactured from the (unresolved) calibration value.

## 46. Complete session-role allocation

KEY1, KEY2, EASY×2, LONG — exactly 5 sessions, matching `RUN_LAYOUT_5D`'s own 5 slots. Session-allocation mechanism: `V1FourDaySessionVolumeAllocationPolicy` (the existing, already-generalized, `IReadOnlyList<double>`-based allocator every non-3-day frequency — including TEN_K's own real, live 6D Core — already uses; HM.10 §14 confirms this directly, "already exercised in real, live 10K production today"). No new allocation policy class is required.

## 47. Weekly-volume feasibility

**Cannot be numerically completed** — every computation (KEY1/KEY2/EASY residual, long-run absolute value, growth trajectory) takes weekly volume as an input, and no peak-volume band is frozen (§19). The allocation *mechanism* itself is unchanged and already proven at HM's own 5D cell (HM.10 §14 hand-traced a full numeric feasibility check at HM's own 51.0 peak, finding residual volume for KEY1+KEY2+EASY1+EASY2 of 36.5km ≈9.1km/session, comfortably feasible) — but that proof used HM's own peak, not a 16K-specific one, and cannot be mechanically ratio-scaled to 16K without violating the no-interpolation rule.

## 48-52. 10W/11W/12W/13W/14W simulations

**Not produced.** Per governing-prompt §53's own conditional ("mandatory if authority closes"), and mirroring DIST-GEN.15/15A's own explicit precedent for the analogous 3D blocker, a full week-by-week numeric simulation requires a frozen peak-volume band as its starting input. The phase/stage skeleton itself (which weeks belong to which phase, at which horizon) is fully known and identical to 16K's own already-proven 3D/4D skeleton (§14-15), independent of volume — but no numerically complete simulation can be honestly produced.

## 53. KEY2 exposure simulation

Structurally verified without needing actual volume figures: KEY2's own min/max exposure bounds (§29) are phase-week-count-driven, not volume-driven, and HM.11's own real dark-materialization proof already exercised this exact progression file across all seven of HM's own 10-16W horizons with zero invalid/missing/duplicate KEY2 exposure found (§36). Since 16K's own supported horizon set (10/12/14 preferred/max, with 11/13 reachable via the same generic phase-allocation resolver already proven at 3D/4D) is a subset of HM's own 10-16W range and uses the identical, unmodified phase-allocation/progression-stage machinery, the same feasibility conclusion transfers by architecture, not by re-simulation: **no compression bug specific to 16K×I×5D is expected, and none can be verified further without also freezing weekly volume** (since `ProgressionStageAllocator`'s own compression/extension logic operates on phase-week counts, which are volume-independent, this specific claim does not require the still-blocked peak-volume band to verify).

## 54. 16K I4D vs I5D table

| Field | 16K I4D | Candidate 16K I5D | Same/Different | Owner | Evidence |
|---|---|---|---|---|---|
| Structural parent | HALF_MARATHON | HALF_MARATHON | Same | Parent | §11 |
| Catalog candidate | HALF_MARATHON×I×4D | HALF_MARATHON×I×5D | Different (correct — different layout/master version) | Catalog | §12 |
| Horizon (10/12/14) | Same | Same | Same | Target | §14 |
| Phase topology | Same | Same | Same | Parent | §15 |
| Growth rates | 0.07/0.08 | 0.07/0.08 | Same | Generic | §22 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | 2.5 | Same | Frequency | §23 |
| LR-share quadruple | 0.30/0.36/0.33/0.40 | 0.28/0.36/0.28/0.36 | **Different** | Frequency | §24 |
| StartingAbsoluteLongRunCapKm | null | **12.0 (expected)** | **Different** | Parent×Frequency | §25 |
| PreferredAbsolutePeakLongRunKm | 15.0 | 15.0 | Same | Target | §26 |
| Taper | 2wk/0.70+0.43 | 2wk/0.70+0.43 | Same | Parent | §27 |
| PeakVolumeBand/SelectedPeak/Calibration | [34,46]/40.0/23.5 | **BLOCKED** | Unresolved | Target×Frequency | §19-21 |
| RunLayout | 1 KEY, 2 EASY | 2 KEY, 2 EASY | **Different (KEY2 introduced)** | Catalog | §13 |
| KEY2 semantics | N/A | Reused HM-parent verbatim | New (structurally) | Parent | §28-39 |

## 55. TEN_K/HM 4D→5D table

| Row | TEN_K 4D | TEN_K 5D | TEN_K effect | HM 4D | HM 5D | HM effect | Cross-anchor conclusion |
|---|---|---|---|---|---|---|---|
| Peak volume | 38.0 | 44.5 | +17% | 43.0 | 51.0 | +19% | Comparable magnitude, no shared reuse signal (§16/§19) |
| Calibration | 24.0 | 26.0 | +8% | 25.0 | 29.5 | +18% | Different magnitude, no shared reuse signal |
| Growth rates | 0.07/0.08 | 0.07/0.08 | None | 0.07/0.08 | 0.07/0.08 | None | Frequency-invariant, universal |
| Absolute cap | 2.5 | 2.5 | None | 2.5 | 2.5 | None | Agreed — frequency-shared |
| LR shares | 0.30/0.36/0.33/0.40 | 0.28/0.36/0.28/0.36 | Decrease | 0.30/0.36/0.33/0.40 | 0.28/0.36/0.28/0.36 | Decrease | **Agreed — frequency-shared, unlike 3D** |
| StartingAbsoluteLongRunCapKm | null | null | None | null | **12.0** | **Introduced** | **Disagreed — parent×frequency, not frequency-shared (corrects DIST-GEN.18)** |
| Absolute peak LR | null | null | None | 19.0 | 19.0 | None | Frequency-invariant within each family |
| Taper | 1wk/0.53 | 1wk/0.53 | None | 2wk/0.70+0.43 | 2wk/0.70+0.43 | None | Parent-family-owned, frequency-blind |
| RunLayout/KEY count | 1 KEY | 2 KEY | KEY2 introduced | 1 KEY | 2 KEY | KEY2 introduced | Both families independently add KEY2 at 5D — structural convergence |
| KEY2 intensity | N/A | Light-quality (not re-audited, out of scope) | — | N/A | Light-quality/FARTLEK, never true-hard | — | HM's own frozen structural authority; TEN_K not re-derived here |

## 56. Field-by-field authority classifications

| Field | Classification |
|---|---|
| Structural parent | `PARENT_FAMILY_ONLY` |
| Catalog candidate, RunLayout | `CATALOG_FREQUENCY` |
| MinimumCoreWeeks | `PARENT_FAMILY_ONLY` |
| PreferredCoreWeeks/MaximumCoreWeeks | `TARGET_ONLY` |
| Growth rates | `GENERIC` |
| AbsoluteWeeklyIncreaseCapKm | `FREQUENCY_ONLY` |
| LR-share quadruple | `FREQUENCY_ONLY` |
| StartingAbsoluteLongRunCapKm | `PARENT_FAMILY_X_FREQUENCY` |
| PreferredAbsolutePeakLongRunKm | `TARGET_ONLY` |
| Taper | `PARENT_FAMILY_ONLY` |
| PeakVolumeBandMin/Max, SelectedPeakReference, CalibrationStartingVolumeKm | `TARGET_X_FREQUENCY` (`INSUFFICIENT_EVIDENCE` for closure) |
| KEY2 workout content/intensity/pace/phase-behavior | `PARENT_FAMILY_ONLY` |
| KEY2 exposure min/max | `CATALOG_FREQUENCY` |
| Session-allocation mechanism, calendar hardness bridge | `GENERIC` |
| Target-race-pace, Riegel, readiness, adaptation | `GENERIC` |

## 57. KEY2 authority table

| Phase | Stage | KEY2 present? | Session role | Workout family | Intensity class | Pace owner | Exposure min | Exposure preferred | Exposure max | Authority owner | Evidence tier | Projected reuse allowed? |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| FOUNDATION | FOUNDATION_HM_SECONDARY_EASY | Yes | Secondary/support | EASY_STANDARD v4 | Easy-equivalent | Generic effort | 2 | Not authored | 4 | Parent family | DIRECT_EXISTING_AUTHORITY | Yes |
| BUILD | BUILD_HM_SECONDARY_FARTLEK (fallback: EASY) | Yes | Secondary/quality | FARTLEK v6 | Light-quality, never true-hard | Generic effort (SURGE_AND_FLOAT) | 3 | Not authored | 5 | Parent family | DIRECT_EXISTING_AUTHORITY | Yes |
| RACE_SPECIFIC | RACE_SPECIFIC_HM_SECONDARY_FARTLEK (fallback: EASY) | Yes | Secondary/quality | FARTLEK v6 | Light-quality, never true-hard, never target-specific | Generic effort | 3 | Not authored | 5 | Parent family | DIRECT_EXISTING_AUTHORITY | Yes |
| TAPER | TAPER_HM_SECONDARY_EASY | Yes | Secondary/support | EASY_STANDARD v4 | Easy-equivalent | Generic effort | 2 | Not authored (PROTECTED/FIXED at 2) | 2 | Parent family | DIRECT_EXISTING_AUTHORITY | Yes |

## 58. Peak-cluster ownership

`PeakVolumeBandMin/Max`, `SelectedPeakReference`, `CalibrationStartingVolumeKm`: consistent with the established DIST-GEN.15/15B classification at 3D, this cluster is **not** parent-family authority — it does not inherit HM's own [44,58]/51.0/29.5 automatically merely because 16K's parent is HALF_MARATHON, per the same "value equality ≠ authority equality" discipline. It remains genuine `TARGET_X_FREQUENCY` exact-cell authority, and — unlike 3D, where the closest available anchor (HM I3D) and the best direct evidence (Higdon Novice) at least sat in the same rough neighborhood after accounting for population mismatch — at 5D the single best-matched, population-and-frequency-and-distance-aligned direct source (Higdon Intermediate, §17) actively **conflicts** with the closest structural anchor (HM I5D's own [44,58] band), landing above its entire range. This is reported as a genuine, disclosed evidence tension, not resolved by this phase's own authority (§19).

## 59. Absolute-LR ownership

`TARGET_ONLY`. 15.0 is reused unchanged from 16K's own already-frozen value, supported by the strongest frequency-invariance evidence in this engagement to date: HM's own ceiling is identical (19.0) at all four of its own frequencies (3D/4D/5D/6D), and 16K's own value has already been independently reused unchanged once before (3D, DIST-GEN.15 §32). Not reopened, not re-derived.

## 60. Evidence-confidence table

| Field | Tier |
|---|---|
| Structural parent, MinimumCoreWeeks, horizon triple, growth rates, absolute cap, absolute peak LR, taper, catalog candidate/RunLayout, KEY2 content/intensity/pace/exposure bounds, session-allocation mechanism, calendar hardness bridge | `DIRECT_EXISTING_AUTHORITY` |
| LR-share quadruple, StartingAbsoluteLongRunCapKm | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` / `DIRECT_EXISTING_AUTHORITY` respectively (§24-25) |
| PeakVolumeBandMin/Max, SelectedPeakReference, CalibrationStartingVolumeKm | `DECISION_REQUIRED` |

## 61. Complete implementation-ready table

| Field | Value | Owner | Classification | Evidence tier | Source |
|---|---|---|---|---|---|
| TargetDistanceKm | 16.0 | Target | `TARGET_ONLY` | `DIRECT_EXISTING_AUTHORITY` | Existing |
| ParentDistanceFamily | HalfMarathon | Parent | `PARENT_FAMILY_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §11 |
| Level/RunsPerWeek | Intermediate/5 | — | — | — | Fixed by scope |
| RunLayout | KEY,EASY,KEY,EASY,LONG | Catalog | `CATALOG_FREQUENCY` | `DIRECT_EXISTING_AUTHORITY` | §13 |
| Catalog candidate | HALF_MARATHON×I×5D (existing) | Catalog | `CATALOG_FREQUENCY` | `DIRECT_EXISTING_AUTHORITY` | §12 |
| MinimumCoreWeeks | 10 | Parent | `PARENT_FAMILY_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §14 |
| PreferredCoreWeeks | 12 | Target | `TARGET_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §14 |
| MaximumCoreWeeks | 14 | Target | `TARGET_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §14 |
| PeakVolumeBandMin | **BLOCKED** | Target×Frequency | `TARGET_X_FREQUENCY` | `DECISION_REQUIRED` | §19 |
| PeakVolumeBandMax | **BLOCKED** | Target×Frequency | `TARGET_X_FREQUENCY` | `DECISION_REQUIRED` | §19 |
| SelectedPeakReference | **BLOCKED** | Target×Frequency | `TARGET_X_FREQUENCY` | `DECISION_REQUIRED` | §20 |
| CalibrationStartingVolumeKm | **BLOCKED** | Target×Frequency | `TARGET_X_FREQUENCY` | `DECISION_REQUIRED` | §21 |
| PreferredWeeklyIncreaseRate/Hard | 0.07/0.08 | Generic | `GENERIC` | `DIRECT_EXISTING_AUTHORITY` | §22 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | Frequency | `FREQUENCY_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §23 |
| LR PreferredMin/Max/Selected/HardCap | 0.28/0.36/0.28/0.36 | Frequency | `FREQUENCY_ONLY` | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | §24 |
| PreferredAbsolutePeakLongRunKm | 15.0 | Target | `TARGET_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §26 |
| StartingAbsoluteLongRunCapKm | 12.0 (expected) | Parent×Frequency | `PARENT_FAMILY_X_FREQUENCY` | `DIRECT_EXISTING_AUTHORITY` | §25 |
| TaperDuration/Multipliers/Mechanism | 2wk/0.70+0.43/non-chained | Parent | `PARENT_FAMILY_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §27 |
| KEY1 semantics | Reused HM-parent progression + 16K target-race pace | Parent+Target | `PARENT_FAMILY_ONLY`+`TARGET_ONLY` | `DIRECT_EXISTING_AUTHORITY` | §31 |
| KEY2 semantics/phase behavior/pace owner/exposure min/max | See §57 table | Parent | `PARENT_FAMILY_ONLY`/`CATALOG_FREQUENCY` | `DIRECT_EXISTING_AUTHORITY` | §28-39 |
| QualitySessionCount | 2 (KEY1+KEY2) | Catalog | `CATALOG_FREQUENCY` | `DIRECT_EXISTING_AUTHORITY` | §13 |
| RaceSpecificPaceSemantic (KEY1) | Generic, TargetDistanceKm=16.0 | Generic | `GENERIC` | `DIRECT_EXISTING_AUTHORITY` | §42 |
| Riegel, Readiness, Adaptation | Generic, unmodified | Generic | `GENERIC` | `DIRECT_EXISTING_AUTHORITY` | §43-45 |
| Calendar | Generic 5D machinery + generic hardness bridge (already fixed, HM.11) | Generic | `GENERIC` | `DIRECT_EXISTING_AUTHORITY` | §40-41 |

**Four rows remain `DECISION_REQUIRED`. Per this engagement's own no-forced-closure discipline, this table is not implementation-ready as a whole.**

## 62. Reuse table

| Field | 16K I4D owner | Canonical TEN_K I5D owner | Canonical HM I5D owner | Candidate 16K I5D owner | Reuse status | New decision needed? |
|---|---|---|---|---|---|---|
| AbsoluteWeeklyIncreaseCapKm | Frequency (2.5) | Own (2.5) | Own (2.5) | Frequency (expected 2.5) | Direct reuse | No |
| LR-share quadruple | Frequency (0.30/0.36/0.33/0.40) | Own (0.28/0.36/0.28/0.36) | Own (0.28/0.36/0.28/0.36) | Frequency (expected 0.28/0.36/0.28/0.36) | Direct reuse | No |
| StartingAbsoluteLongRunCapKm | Frequency (null) | Own (null) | Own (12.0) | Parent×Frequency (expected 12.0) | Direct reuse (from HM only) | No |
| Taper | Parent (2wk/0.70/0.43) | Own (1wk/0.53) | Own (reused from 4D) | Parent (expected 2wk/0.70/0.43) | Direct reuse | No |
| PreferredAbsolutePeakLongRunKm | Target (15.0) | N/A (null) | Own (19.0) | Target (expected 15.0) | Direct reuse | No |
| Peak-volume band/selected peak/calibration | Exact cell ([34,46]/40.0/23.5) | Own (44.5/26.0) | Own ([44,58]/51.0/29.5) | **New exact-cell authority** | **No reuse source usable without conflict** | **Yes — blocked** |
| KEY2 content/pace/exposure | N/A (no KEY2 at 4D) | Not re-audited | Own (frozen, HM.9/HM.10) | Parent (expected: direct reuse) | Direct reuse | No |

## 63. Feasibility table

**Not produced.** Per governing-prompt §76's own conditional ("if numeric closure succeeds"), and mirroring DIST-GEN.15/15A's precedent, no horizon-by-horizon feasibility table is honestly producible without a frozen peak-volume band.

## 64. Numeric feasibility result

**Cannot be returned as `16K_INTERMEDIATE_5D_NUMERICALLY_FEASIBLE`.** Every resolved field is individually well-formed (LR-share quadruple correctly ordered with headroom; taper mechanism valid and re-verified against the 5D-scoped 56km/week threshold context; KEY2 exposure bounds internally consistent and already proven feasible in HM's own live 5D dark closure), but actual weekly-volume allocation, actual KEY1/KEY2/EASY residuals, and actual long-run progression cannot be verified without the still-blocked peak-volume band (§19). The blocker is precisely and only the peak-volume/selected-peak/calibration cluster.

## 65. KEY2 authority result

**`KEY2_FULLY_REUSED_FROM_CANONICAL_FREQUENCY_PARENT_AUTHORITY`.** Every KEY2 field (workout content, phase behavior, intensity classification, pace ownership, exposure bounds, calendar hardness classification) is directly, verifiably reusable from the existing, unmodified `half-marathon-workout-progression.v2.json` and HM.9/HM.10/HM.11's own already-frozen structural authority and already-fixed generic calendar-hardness capability. This is the single cleanest, most fully-resolved dimension in this entire phase — the "genuinely unexamined" axis DIST-GEN.18 identified as the primary reason to select 5D turns out to require **zero new authority**, because 16K's own structural parent (HALF_MARATHON) already owns KEY2 completely and 16K's own catalog candidate already references the exact file that implements it.

## 66. Orthogonality result

**`PROJECTED_DISTANCE_AND_5D_FREQUENCY_PARTIALLY_ORTHOGONAL`** (§5). The large majority of authority — including, notably, the entire KEY2/secondary-quality-lane surface this phase was specifically chartered to investigate — composes cleanly with zero new interaction field. The sole load-bearing exception remains the same *category* of field that blocked 3D: the peak-volume/selected-peak/calibration cluster, though the specific character of the 5D blocker differs materially from 3D's (at 3D, the best evidence was population-mismatched from the target population; at 5D, the best-matched evidence — Higdon Intermediate 15K/10-Mile, a genuine three-way distance/frequency/population match — actively conflicts numerically with the closest structural anchor).

## 67. Registry-architecture result

**`16K_INTERMEDIATE_5D_FITS_EXISTING_PROJECTED_TARGET_REGISTRY_ARCHITECTURE`, with one disclosed, precedented implementation cost for a future DIST-GEN.20.** Confirmed directly from source: `ApprovedDarkTargetDistanceCell`/`ProjectedTargetAuthority.Cell` already carry `RunsPerWeek` as a first-class field, so `(16.0, HalfMarathon, Intermediate, 5)` is structurally representable today with zero type change. However, `CatalogVolumeAndLongRunPlanner.cs`'s own dark-target-distance dispatch guard (the same site DIST-GEN.16 widened from `DaysPerWeek == 4` to `(DaysPerWeek == 3 || DaysPerWeek == 4)`) is currently scoped to exactly `{3, 4}` and does **not** include 5 — a future DIST-GEN.20 would need the identical, already-precedented one-line widening (`(DaysPerWeek == 3 || DaysPerWeek == 4 || DaysPerWeek == 5)`) to make a new registration reachable at all, exactly mirroring DIST-GEN.16's own experience. This is a known, precedented, low-risk implementation task, not a new architecture question, and does not affect this phase's own governance classification.

## 68. 6D implications

Nothing in this phase's own findings pre-authorizes 16K×I×6D. The KEY2 authority closure (§65) would very likely transfer directly, by the identical reasoning (HM's own 6D progression is the same unmodified v2 file), but 6D's own peak-volume cluster and its own `StartingAbsoluteLongRunCapKm=12.0` (already parent×frequency-owned at both 5D and 6D per §25) would still need their own independent confirmation, and 6D introduces materially higher session-distribution complexity than 5D per DIST-GEN.4/8/12/14's own consistent, unreversed finding. Not authorized here.

## 69. Existing projected zero-delta

Confirmed via `git status`: zero changes to any 15K/16K(3D/4D)/18K policy file, value, catalog combination, or test. This phase's own diff contains no production source at all.

## 70. Canonical zero-delta

TEN_K's and HALF_MARATHON's own 3D/4D/5D/6D authority (including the very values read and compared here) remain completely untouched — read as evidence and cross-anchor context only, never modified.

## 71. DIST-GEN.20 readiness

**Not ready.** DIST-GEN.20 (16K × Intermediate × 5D full dark implementation) cannot proceed without making its own training-science decision on the peak-volume-band/selected-peak/calibration cluster, which this phase's explicit scope (governing-prompt §32) prohibits doing via any permitted method given the evidence in hand, and prohibits this phase from resolving via an unauthorized product decision. Everything else this report resolves — structural parent, catalog candidate, RunLayout, phase topology, full horizon triple, growth/cap/LR-share/starting-cap/absolute-peak-LR/taper ownership, and, centrally, the **complete KEY2 authority closure** (§57/§65) — is implementation-ready and requires no further governance work. DIST-GEN.20 would also need the one precedented dispatch-gate widening noted in §67.

## 72. Remaining blockers

**One, precisely scoped:** `PeakVolumeBandMin`, `PeakVolumeBandMax`, `SelectedPeakReference`, and `CalibrationStartingVolumeKm` for the exact cell `16.0 × HalfMarathon × Intermediate × 5D` remain `DECISION_REQUIRED`. Root cause, fully disclosed: the single best-matched direct source found (Hal Higdon's Intermediate 15K/10-Mile program — already fetched in full by DIST-GEN.15A, a genuine three-way distance/frequency/population match unlike anything available at 3D) reports a peak weekly running volume of 59.5-61.2 km/week and a peak long run of 16.1 km — both of which sit above HM's own canonical Intermediate×5D band ([44,58] km/week peak, 19.0 km ceiling) and, in the long-run case, above 16K's own already-frozen 16.0 km target distance itself (16.1 km > 16.0 km, which would violate the standing "long run strictly below target distance" invariant if adopted literally). This is not a population mismatch of the kind that blocked 3D — it is a genuine numeric conflict between the best available direct evidence and the closest structural/parent-family anchor, for a single, non-corroborated source. No permitted derivation method (interpolation, ratio, nearest-anchor substitution) can responsibly close it, and this phase is not authorized to issue an unrequested exact-cell product decision on its own authority (governing-prompt §32, distinguishing this phase from DIST-GEN.15B, where such a decision was made directly by product authority after DIST-GEN.15A presented, without selecting among, a finite option envelope). Per that same precedent, this phase presents an equivalent finite envelope for a future explicit product decision, without selecting among the options:

- **Option 1**: adopt canonical HM Intermediate×5D's own already-frozen band/peak/calibration directly ([44,58] / 51.0 / 29.5) as 16K×I×5D's own independent exact-cell authority — the same methodology DIST-GEN.15B used at 3D — explicitly disclosing this as a product decision, not parent-family authority, and explicitly not extending to 15K/18K×I×5D.
- **Option 2**: adopt a band informed by Higdon's Intermediate 15K/10-Mile figures directly (peak ≈59.5-61.2 km/week), explicitly disclosing that this would require re-examining whether 16K's own already-frozen `PreferredAbsolutePeakLongRunKm=15.0` ceiling remains appropriate at 5D given real evidence suggesting a materially higher peak long run for this population/frequency/distance combination — a decision this phase is not authorized to make, since it would reopen an already-frozen target-only field (§26), not merely the peak-volume cluster.
- **Option 3**: commission genuinely new, non-literature-based evidence or an internal product/coaching decision, which this phase has no visibility into.

This phase deliberately selects none of these options, consistent with the same discipline DIST-GEN.15A applied, and reports `PRODUCT_DECISION_REQUIRED` alongside the smallest-evidence-need statement above.

---

## Final classification

**`16K_INTERMEDIATE_5D_PROJECTED_DISTANCE_FREQUENCY_ORTHOGONALITY_AUTHORITY_BLOCKED`**

Every criterion in the governing prompt's own §79 checklist is met except one: structural parent (§11), catalog candidate (§12), RunLayout (§13), horizon (§14), phase topology (§15), growth ownership (§22), absolute weekly cap (§23), LR-share authority (§24), starting-LR cap (§25, with a genuine correction to the DIST-GEN.18 brief), absolute peak LR (§26), taper (§27), KEY1 semantics (§31, §42), **KEY2 semantics/phase behavior/intensity authority/pace ownership/exposure bounds — fully closed (§28-39, §57, §65)**, compression behavior (§36), RaceSpecific KEY2 behavior (§37), Taper KEY2 behavior (§38), existing workout catalog sufficiency (§39, §46), no interpolation used (confirmed throughout), no new workout authority invented (confirmed), no existing projected/canonical authority changed (§69-70), and no production source changed (§69) are all genuinely resolved. The sole reason this phase is not classified `_CLOSED` is the single, precisely-scoped, honestly-disclosed blocker in §72: the peak-volume-band/selected-peak/calibration cluster cannot be responsibly frozen given a genuine numeric conflict between the best available direct evidence and the closest structural anchor, and this phase is not authorized to issue the exact-cell product decision that would be required to close it (unlike DIST-GEN.15B, which recorded a decision made directly by product authority). Per this engagement's own established norm of honest `_BLOCKED` classification over forced closure, this phase reports blocked rather than inventing a number or issuing an unauthorized product decision to force `DIST-GEN.20`'s own readiness. DIST-GEN.20 is not begun.
