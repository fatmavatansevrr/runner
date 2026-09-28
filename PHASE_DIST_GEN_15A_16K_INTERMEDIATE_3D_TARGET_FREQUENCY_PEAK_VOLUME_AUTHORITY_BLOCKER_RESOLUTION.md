# PHASE DIST-GEN.15A — 16K × Intermediate × 3D Target×Frequency Peak-Volume Authority Blocker Resolution

## 1. DIST-GEN.15 blocker consumed

DIST-GEN.15 closed `16K_INTERMEDIATE_3D_PROJECTED_DISTANCE_FREQUENCY_ORTHOGONALITY_AUTHORITY_BLOCKED`, reaching `PROJECTED_DISTANCE_AND_FREQUENCY_PARTIALLY_ORTHOGONAL` with exactly one unresolved cluster: `PeakVolumeBandMin`, `PeakVolumeBandMax`, `SelectedPeakReference`, `CalibrationStartingVolumeKm` for the cell `16.0 × HalfMarathon × Intermediate × 3`. Its own root-cause finding: two race-distance-matched evidence families (10-mile-specific 3-day content, and half-marathon-specific 3-day content) diverge by roughly a factor of two with no meaningful overlap, and the governing prompt's own explicit, repeated prohibition on any ratio/interpolation derivation forecloses the remaining closure paths. This phase's sole objective is to determine whether fresh, more targeted evidence can responsibly resolve that specific cluster — not to redo any other part of DIST-GEN.15.

## 2. Repository baseline

Starting commit `058804a` on `main`, 0 ahead / 0 behind `origin/main`. This phase is governance-only and performs zero production edits.

## 3. Frozen resolved authority

Re-confirmed directly against DIST-GEN.15's own report — the following remain closed and are not reopened:

`TargetDistanceKm=16.0`, `ParentDistanceFamily=HalfMarathon`, `Level=Intermediate`, `RunsPerWeek=3`, `RunLayout=[KEY,EASY,LONG]`, `MinimumCoreWeeks=10`, `PreferredCoreWeeks=12`, `MaximumCoreWeeks=14`, `PreferredWeeklyIncreaseRate=0.07`, `HardWeeklyIncreaseRate=0.08`, `AbsoluteWeeklyIncreaseCapKm=2.0`, LR-share quadruple `0.34/0.40/0.38/0.46`, `PreferredAbsolutePeakLongRunKm=15.0`, `StartingAbsoluteLongRunCapKm=null`, taper `2 weeks/0.70/0.43/non-chained fixed-anchor`, primary progression (existing HM×I×3D candidate), `QualitySessionCount=1`, target-race-pace/Riegel/readiness/calendar all generic/existing. This list matches DIST-GEN.15's own report exactly; no correction was needed.

## 4. Exact remaining blocker set

Exactly and only: `PeakVolumeBandMin`, `PeakVolumeBandMax`, `SelectedPeakReference`, `CalibrationStartingVolumeKm`. Not expanded to any other field.

## 5. Dependency graph

`PeakVolumeBandMin/Max` → `SelectedPeakReference` → `CalibrationStartingVolumeKm`. Confirmed via §6 below: this is a strict, one-directional dependency chain, not four independent research problems. External evidence is needed to close the band only; the two downstream fields are each resolved by applying an established *convention* (not a governance-frozen *formula*) to the band/peak once those exist.

## 6. Selected-peak methodology audit

Reconstructed directly from DIST-GEN.1 §23, DIST-GEN.5 §29, and DIST-GEN.9 §30-31: at every prior closure, `SelectedPeakReference` was placed at the exact midpoint of the frozen band — a real, 100%-consistently-applied pattern across every single named instance in `VolumeSafetyPolicy.cs` that has a formally declared band (15K/16K/18K all place their own selected peak at exactly the midpoint of their own [34,46] band; canonical HM's own instances at every frequency/level place their own selected peak at the exact midpoint of their own band). However, this is explicitly documented as an "established convention," classified `EVIDENCE_INFORMED_PRODUCT_DEFAULT` at every single application — never `DIRECT_EXISTING_AUTHORITY`, never stated anywhere as a binding rule that removes judgment. DIST-GEN.9 §31 itself declines to treat the three-target agreement as independent confirming evidence for exactly this reason (`INSUFFICIENT_PATTERN_EVIDENCE`, "mechanically tied to §29's own peak-volume-band derivation, not an independent confirmation"). **Conclusion: a real, consistently-applied convention exists (band midpoint), but it requires a frozen band to apply — it is not itself a source of evidence for the band, and it does not eliminate the need for a genuine decision once a band exists (choosing to apply the midpoint convention is itself the decision, consistently made the same way every time, but a decision nonetheless).**

## 7. Calibration methodology audit

Reconstructed directly from the same three closures plus a full ratio computation across every named `VolumeSafetyPolicy` instance. The calibration/selected-peak ratio is **not constant** across the codebase — it ranges from 0.5333 (`ThreeDayIntermediate`) to 0.7059 (`ThreeDayBeginner`), clustering around 0.578-0.588 for HM-parented instances but never landing on a single canonical number except by rounding coincidence. The three interior projected targets (15K/16K/18K) all land on an identical 23.5/40.0=0.5875 ratio only because they share an identical unrounded peak input (40.0) and identical rounding outcome — this is a coincidence of shared inputs, not evidence of one fixed ratio. The actual, real methodology at every prior closure was: pick the empirically-observed calibration/peak ratio from the most structurally-appropriate already-approved sibling policy (HM's own 4D ratio for the three interior targets; TEN_K's own 3D ratio was itself used to derive `Advanced3D`'s own calibration; HM's own 4D ratio was separately used to extend to `HalfMarathonBeginner3D/4D`), apply it to the new peak, then round to the nearest 0.5km grid. This requires a real, disclosed, per-closure choice of *which* sibling's ratio is most appropriate — not a parameterless formula. **Conclusion: no governance-frozen deterministic rule exists for either field. Both are real, disclosed, historically-consistent conventions requiring their own judgment call once a band exists — meaning even a successfully-frozen band would not by itself automatically resolve selected-peak or calibration; it would only make the subsequent (still-judgment-based, but well-precedented and low-risk) application of those conventions possible.**

## 8. Canonical TEN_K I3D context

`ThreeDayIntermediate`: `AbsoluteWeeklyIncreaseCapKm=2.0`, `GoldenFixtureStartingVolumeKm=12.0`, `SelectedPeakReference=22.5` (`ProductDefaultWithEvidenceEnvelope`), LR-share `0.38/0.42/0.40/0.42`, no formally declared `PeakVolumeBandMin/Max` policy class exists for TEN_K at any frequency (TEN_K's band, where it exists, lives in the catalog JSON, not in `VolumeSafetyPolicy.cs`). Context only, per §18/§22 of the governing prompt — never a direct source for 16K's own value.

## 9. Canonical HM I3D context

`HalfMarathonIntermediate3D`: `AbsoluteWeeklyIncreaseCapKm=2.0`, `GoldenFixtureStartingVolumeKm=20.0`, `SelectedPeakReference=34.0` (exact midpoint of its own [28,40] band, `ProductDefaultWithEvidenceEnvelope`, per HM.7 §9's own documented derivation informed by two real 3-day HM program sources), LR-share `0.34/0.40/0.38/0.46`. Context/upper-anchor only.

## 10. 16K I4D context

Already-frozen: `PeakVolumeBandMin/Max=[34.0,46.0]`, `SelectedPeakReference=40.0` (exact midpoint), `CalibrationStartingVolumeKm=23.5`. Context only — no 3/4 scaling applied anywhere in this phase.

## 11. DIST-GEN.15 evidence mismatch

Preserved exactly as DIST-GEN.15 found it: 10-mile-specific 3-day content skews to ~13-24km/week with long runs peaking ~13-14.5km, population below Appsel's own Intermediate tier; dedicated half-marathon 3-day content (Marathon Handbook) runs ~40-43km/week with long runs peaking ~19km, population/structure closer to Intermediate but targeting a different race distance (21.1km, not 16.0km). This phase does not erase, average, or otherwise resolve this mismatch by fiat — it is treated as the central, real finding to be tested against fresh, more targeted evidence.

## 12. Research strategy

Per the governing prompt's own explicit priority ordering (§12-19): prioritize sources matching all three of race-distance-neighborhood (15K/10-mile/16K), exactly 3 running days/week, and an existing-running-base "Intermediate" population — in that order of importance — over sources matching only one or two dimensions. Prefer official/established coaching sources (Hal Higdon, McMillan) over generic SEO/wellness content. Perform genuinely fresh, live web research and direct source fetches rather than relying on DIST-GEN.15's own prior search snippets.

## 13. Search queries

`"10 mile" training plan intermediate 3 days per week weekly mileage schedule`; `15K training plan 3 runs per week intermediate weekly mileage long run`; `16K training plan 3 runs per week`; `"3 days a week" 10 mile OR 15K training plan intermediate runner already running weekly mileage`; plus direct fetches of the two most relevant results found (Hal Higdon's own Intermediate and Novice 15K/10-Mile programs) and an attempted fetch of a McMillan 15K/10-Mile Level 2/3 plan.

## 14. Sources inspected

- **Hal Higdon — Intermediate 15K/10-Mile** (halhigdon.com, fetched in full): 10-week program, 5 running days + 2 strength + 1 cross-training + 1 rest, peak weekly mileage 37-38 miles (59.5-61.2km), peak long run 10 miles (16.1km) in week 9. A well-known, reputable, coach-authored program already cited in DIST-GEN.1's own original evidence base.
- **Hal Higdon — Novice 15K/10-Mile** (halhigdon.com, fetched in full): 10-week program, exactly 3 running days/week (Tuesday/Thursday/Saturday) + cross-training + strength, peak weekly mileage 24 miles (38.6km) in week 9, peak long run 8 miles (12.9km) in week 9, with explicit stepback weeks at 4 and 7.
- **Marathon Handbook — 3 Day A Week Half Marathon Plan** (already fetched in DIST-GEN.15, re-confirmed relevant): 12-week plan, exactly 3 running days/week (1 easy + 1 quality + 1 long), peak weekly mileage 25-27 miles (40-43km), peak long run 12 miles (19km) in week 10.
- **McMillan Running — 15K/10 Mile Level 2/3 (Intermediate)** (trainingpeaks.com and site.finalsurge.com): search results confirmed these exist and are named "Intermediate," with one general summary claiming 20-33 miles/week — but direct fetch attempts returned either a 403 (final surge) or promotional-only content with no actual schedule (TrainingPeaks) and were excluded per §57's explicit instruction not to infer hidden plan contents behind a wall.

## 15. Sources excluded

McMillan's own detailed plan pages (both platforms) — content behind a paywall/purchase gate, only promotional summaries retrievable; excluded per §57 rather than guessed at. Generic 10K-focused and marathon-focused 3-day content surfaced in search results but not pursued further, since it fails the distance-match standard (§18) outright.

## 16. Population-match analysis

Hal Higdon's own naming is directly informative here: his "Novice" 15K/10-Mile tier (the ONLY one of his own tiers offered at exactly 3 days/week) is explicitly positioned below his own "Intermediate" tier (offered only at 5 days/week). This is the single most important new finding of this phase: **the same reputable author, for the exact same race-distance neighborhood, only offers a 3-day-per-week structure at a population tier he himself labels below "Intermediate."** This is not a coincidence of which two sources DIST-GEN.15 happened to find — it is a structural pattern in how real, established training-plan publishers design their own offerings for this exact race-distance range.

## 17. Frequency-match analysis

Hal Higdon's Novice program is a genuine, explicit 3-running-day/week schedule (not a flexible or reducible 4-5 day plan) — the strongest possible frequency match found. Marathon Handbook's 3-day HM plan is likewise an explicit, dedicated 3-day structure. Higdon's Intermediate program is explicitly 5 days and is not a valid 3D source under §17's own standard (a plan from which a day could theoretically be dropped is not 3D authority).

## 18. Distance-match analysis

Higdon's Novice AND Intermediate programs are both exact race-distance matches (15K/10-mile, essentially identical to 16K). Marathon Handbook's plan targets the full half-marathon (21.1km), a distance mismatch relative to 16K.

## 19. Running-volume extraction

All three fully-fetched sources report running-only mileage (Higdon's own cross-training/strength days are explicitly separate from the running-mileage figures reported; Marathon Handbook's own cross-training allowance is likewise reported as separate from its stated 25-27 mile running total). No mixed-modality figures were used.

## 20. Direct evidence table

| Source | Starting weekly running volume | Peak weekly running volume | Peak LR | Plan weeks | Entry base | Quality sessions/week | Notes |
|---|---|---|---|---|---|---|---|
| Higdon Novice 15K/10-Mile | ~12-13 mi (19.3-20.9km) | 24 mi (38.6km) | 8 mi (12.9km) | 10 | Below Intermediate, per author's own tier naming | 1 (Tuesday quality) | Genuine 3-day, distance-matched, population-mismatched (low) |
| Higdon Intermediate 15K/10-Mile | ~10 mi/wk implied wk1 | 37-38 mi (59.5-61.2km) | 10 mi (16.1km) | 10 | Matches Appsel Intermediate reasonably well | 2 (Tue intervals + Wed tempo) | Distance+population matched, frequency mismatched (5 days) |
| Marathon Handbook 3-Day HM | Not explicitly stated | 25-27 mi (40-43km) | 12 mi (19km) | 12 | Adaptable, "injury-prone/short on time" framing | 1 (Wed/Thu tempo or interval) | Frequency+population reasonably matched, distance mismatched (HM not 16K) |

## 21. Evidence-source quality table

| Source | Distance match | Frequency match | Population match | Volume extractability | Peak-volume relevance | LR relevance | Quality-structure relevance | Authority weight | Exclusion/limitations |
|---|---|---|---|---|---|---|---|---|---|
| Higdon Novice | Exact | Exact | Below Intermediate | Clean | High but population-adjacent | High but population-adjacent | High (1 quality/wk matches Appsel 3D exactly) | `DIRECT_POPULATION_MATCH` on distance+frequency; `PARTIAL_POPULATION_MATCH` overall | Population tier below Appsel Intermediate |
| Higdon Intermediate | Exact | None (5D) | Good match | Clean | High but frequency-mismatched | High but frequency-mismatched | Moderate (2 quality/wk, not 1) | `UPPER_ANCHOR_CONTEXT` | Wrong frequency entirely |
| Marathon Handbook 3D HM | None (HM not 16K) | Exact | Good match | Clean | High but distance-mismatched | High but distance-mismatched | High (1 quality/wk matches exactly) | `PARTIAL_POPULATION_MATCH` | Wrong race distance |
| McMillan 15K/10Mi Intermediate | Exact | Unknown (blocked) | Claimed Intermediate | Blocked | Unknown | Unknown | Unknown | `EXCLUDED` | Paywall/promotional-only content |

## 22. Evidence envelope

Descriptive only, per §21 of the governing prompt (no arithmetic average computed): peak weekly running volume across all genuinely relevant sources spans **24-43 miles (38.6-69.2km)** depending on which dimension is prioritized; peak long run spans **8-12 miles (12.9-19.3km)**; plan duration spans **10-12 weeks**. This envelope is wide specifically because the sources disagree along the population/frequency axis, not because of measurement noise within a single well-matched population.

## 23. Conflict analysis

DIST-GEN.15's original ~2× divergence is now independently reconfirmed and further explained, not resolved, by this phase's own additional research. The clearest new evidence for *why* the conflict exists: Hal Higdon — a single, internally-consistent, reputable author — offers his own 15K/10-Mile "Novice" tier ONLY at 3 days/week (peak 24mi) and his own "Intermediate" tier ONLY at 5 days/week (peak 37-38mi), a jump of over 50%. This is not two different authors disagreeing about the same population; it is one author's own considered judgment that this exact race distance, at his own "Intermediate" fitness/experience bar, genuinely requires more than 3 days/week to reach — and that a 3-day structure at this distance is, in his own system, appropriately paired with a lower population tier. Source classifications: Higdon Novice = `PARTIAL_POPULATION_MATCH` (distance+frequency exact, population one tier low); Higdon Intermediate = `UPPER_ANCHOR_CONTEXT` (distance+population exact, frequency wrong); Marathon Handbook 3D HM = `PARTIAL_POPULATION_MATCH` (frequency+population reasonable, distance wrong); McMillan = `EXCLUDED`.

## 24. Source-silence analysis

No source was found — across DIST-GEN.15's own prior research and this phase's own fresh, targeted, live research, including direct fetches of the two most obviously relevant Hal Higdon programs — that simultaneously matches race-distance-neighborhood, exactly 3 running days/week, AND an Appsel-equivalent "Intermediate" (existing running base, above first-timer, below high-mileage) population. This is recorded as genuine source silence for that exact three-way intersection, not as "no evidence exists at all" (real evidence exists for every pairwise combination of the three dimensions, just not for all three simultaneously).

## 25. Peak-band decision standard

Applying §24 of the governing prompt precisely: a product default is permitted only if (a) external evidence provides a defensible safe envelope — it does (§22); (b) canonical 3D anchors provide sanity context — they do (§8-10); (c) existing 16K target authority provides target context — it does (§10); (d) the choice does not use interpolation — this is the binding constraint; (e) the exact reason a specific envelope point is chosen is explicit; (f) the value can be defended independently of DIST-GEN.16's own need to proceed. Requirement (d) is where this phase's own analysis stops short: every way of picking one specific number from within the 24-43mi envelope that references more than one of the found sources simultaneously (e.g., "somewhere between Higdon Novice and Marathon Handbook's own 3-day plan") is, in substance, interpolation between two mismatched-population anchors — exactly the reasoning §25 of the governing prompt explicitly names as insufficient ("16K should be somewhere between X and Y... is insufficient... does not freeze a number"). Using only the single most relevant source (Higdon Novice, the best frequency+distance match) in isolation is not interpolation, but doing so would silently adopt a population tier this phase has explicitly, repeatedly found to be a poor match for Appsel's own Intermediate definition — which would not be a defensible, explicit product decision so much as an implicit downgrade of the population target, undisclosed as such.

## 26. PeakVolumeBandMin

**Not frozen.** See §25/§55 for the full reasoning.

## 27. PeakVolumeBandMax

**Not frozen.** Same reasoning.

## 28. Peak-band confidence tier

**`DECISION_REQUIRED`.**

## 29. Selected-peak methodology

Reconfirmed (§6): band-midpoint convention, consistently applied, never codified as a mandatory rule. Ready to apply the instant a band exists, requiring no further external evidence of its own.

## 30. SelectedPeakReference

**Not frozen — no band exists to take a midpoint of.**

## 31. Calibration methodology

Reconfirmed (§7): sibling-ratio-reuse convention (most likely candidate for 16K×I×3D would be HM I3D's own real 20.0/34.0=0.5882 ratio, since 3D-frequency match dominates the choice-of-sibling pattern seen at every other 3D-precedent instance in the codebase — e.g., `Advanced3D` borrows `ThreeDayIntermediate`'s own ratio, `HalfMarathonBeginner3D` borrows `HalfMarathonIntermediate3D`'s own ratio), applied to the (not-yet-existing) selected peak, then rounded to the nearest 0.5km. Ready to apply once a peak exists.

## 32. CalibrationStartingVolumeKm

**Not frozen — mechanically downstream of §30.**

## 33. Target×frequency ownership

If a band is ever frozen for this exact cell, `PeakVolumeBandMin/Max`, `SelectedPeakReference`, and `CalibrationStartingVolumeKm` would each be `16K × Intermediate × 3D` exact-cell authority — not generalized to all 16K frequencies, all Intermediate×3D targets, or all HM-parented 3D projected targets, per §45-47 of the governing prompt. This phase does not pre-authorize any such generalization, and none is implied by anything found here.

## 34. LR-share preservation

Unchanged: `0.34/0.40/0.38/0.46`. Not reopened. The Marathon Handbook 3-day HM plan's own long-run-share-of-total-volume (~44-48%, per DIST-GEN.15's own calculation) continues to corroborate this value as sanity context, but this was not re-litigated.

## 35. Absolute peak LR preservation

Unchanged: `15.0`. Not reopened. Nothing found in this phase's own research contradicts the frequency-invariance reasoning DIST-GEN.15 established (Higdon's own peak-long-run figures at both frequency tiers — 8mi Novice, 10mi Intermediate — are themselves both well below 16K's own 15.0km absolute ceiling, consistent with, not contradicting, that ceiling never being forced/chased).

## 36. Growth/cap preservation

`PreferredWeeklyIncreaseRate=0.07`, `HardWeeklyIncreaseRate=0.08`, `AbsoluteWeeklyIncreaseCapKm=2.0` — unchanged. Not reopened. Not adjusted to make any candidate band fit, since no band was frozen.

## 37. Horizon preservation

`MinimumCoreWeeks=10`, `PreferredCoreWeeks=12`, `MaximumCoreWeeks=14` — unchanged. Not reopened.

## 38. Taper preservation

HM-parent taper (2 weeks, 0.70/0.43, non-chained fixed-anchor) — unchanged. Not reopened.

## 39. KEY/EASY/LONG feasibility

Cannot be numerically completed without a frozen peak-volume band — the allocation *mechanism* itself is unchanged and already proven (§44 of DIST-GEN.15), but no actual weekly figures can be produced.

## 40. Long-run feasibility

Same limitation — the LR-share quadruple's own internal ordering remains valid (`0.34 ≤ 0.38 ≤ 0.40 ≤ 0.46`), but feasibility against actual weekly volumes cannot be completed.

## 41. Complete horizon simulations

**Not produced**, for the same reason as DIST-GEN.15's own §47 — a frozen peak-volume band is the necessary starting input for any numerically complete simulation, and it remains unfrozen.

## 42. Growth safety

Not applicable to complete without a target weekly-volume trajectory; the growth-rate ceilings themselves (0.07/0.08/2.0 per week) are unchanged and were not relaxed or tightened to accommodate any candidate band, since none was adopted.

## 43. Taper feasibility

The taper mechanism itself remains valid and unchanged; its numeric application point (the pre-taper anchor, i.e., the actual peak volume reached) cannot be verified without a frozen band.

## 44. Readiness safety

Unaffected; no synthetic readiness value was introduced or considered at any point in this phase.

## 45. No-interpolation proof

Confirmed: no linear interpolation, distance ratio, 3D/4D ratio, TEN_K↔HM normalized position, nearest-target substitution, midpoint-between-canonical-peaks, regression/curve-fitting, or session-count scaling was used anywhere in this phase's own reasoning. The one candidate reasoning path that would have approached this line (blending the Higdon Novice and Marathon Handbook figures into a single number) was explicitly identified and explicitly rejected in §25 for exactly this reason.

## 46. No-nearest-anchor proof

Confirmed: neither TEN_K I3D's nor HM I3D's own peak-volume value was used as a substitute for 16K's own value at any point; both were consulted only as sanity/context anchors, per §8-10.

## 47. No-band proof (distance-band search)

Confirmed: no interior-distance-band, distance-bucket, range-resolver, or nearest-target-resolver concept was introduced anywhere in this phase's own analysis or report.

## 48. 15K/18K non-authorization

Confirmed unchanged: no evidence or authority produced by this phase authorizes 15K×I×3D or 18K×I×3D. Both remain entirely unauthorized.

## 49. Canonical zero-delta

Confirmed via `git status`: zero changes to canonical TEN_K or HALF_MARATHON authority at any frequency. Their values were read as evidence only.

## 50. Current product zero-delta

Confirmed via `git status`: `PUBLIC` projected cells remain exactly {15K, 16K, 18K}×Intermediate×4D. `DARK` cells remain the same three. 16K×Intermediate×3D remains unimplemented — authority closure (even if it had succeeded) does not itself create a cell.

## 51. Updated implementation-ready authority table

Unchanged from DIST-GEN.15's own table in every row except the four still-blocked rows, which remain exactly as DIST-GEN.15 left them (`DECISION_REQUIRED`, `NEW_TARGET_FREQUENCY_CELL_AUTHORITY`). No previously-resolved value was altered by this phase.

## 52. Before/after blocker table

| Field | DIST-GEN.15 status | DIST-GEN.15A evidence | Methodology | DIST-GEN.15A final value/status | Authority owner | Confidence tier |
|---|---|---|---|---|---|---|
| PeakVolumeBandMin | `DECISION_REQUIRED` | Three additional real sources found (Higdon Novice/Intermediate, re-confirmed Marathon Handbook); envelope widened to 24-43mi/wk, confirming rather than resolving the original mismatch | N/A — no permitted method closes this without interpolation | **`DECISION_REQUIRED` (unchanged)** | Target×Frequency exact cell (unresolved) | — |
| PeakVolumeBandMax | `DECISION_REQUIRED` | Same | N/A | **`DECISION_REQUIRED` (unchanged)** | Same | — |
| SelectedPeakReference | `DECISION_REQUIRED` | Methodology confirmed as band-midpoint convention (not a formula) | Would apply band-midpoint once a band exists | **`DECISION_REQUIRED` (unchanged, blocked upstream)** | Same | — |
| CalibrationStartingVolumeKm | `DECISION_REQUIRED` | Methodology confirmed as sibling-ratio-reuse convention (not a formula); best-candidate sibling identified as HM I3D's own 0.5882 ratio | Would apply sibling-ratio-reuse once a peak exists | **`DECISION_REQUIRED` (unchanged, blocked upstream)** | Same | — |

## 53. Feasibility table

Not applicable — no numeric authority closed, so no horizon-by-horizon feasibility table can be produced, per §64 of the governing prompt's own conditional requirement ("Only required if numeric authority closes").

## 54. Numeric feasibility result

Cannot be returned as `16K_INTERMEDIATE_3D_NUMERICALLY_FEASIBLE`. The blocker is identical in scope to DIST-GEN.15's own finding: the peak-volume-band/selected-peak/calibration cluster remains unresolved.

## 55. Orthogonality status preservation

`PROJECTED_DISTANCE_AND_FREQUENCY_PARTIALLY_ORTHOGONAL` — unchanged and not reopened. This phase's own additional research, if anything, strengthens rather than weakens that conclusion: the large majority of authority (confirmed again in §3) remains cleanly composable, and the single remaining gap is now more precisely characterized (a real, structural population/frequency trade-off visible even within one single reputable author's own two training-plan tiers for this exact race distance) rather than resolved.

## 56. Registry-architecture status

Unchanged: `16K_INTERMEDIATE_3D_FITS_EXISTING_PROJECTED_TARGET_REGISTRY_ARCHITECTURE`. No code change; `RunsPerWeek` is already a first-class key field, confirmed previously and not re-litigated here.

## 57. DIST-GEN.16 readiness

**Not ready.** DIST-GEN.16 cannot implement this cell without making the exact same training-science decision this phase's own genuinely fresh research could not responsibly resolve.

## 58. Remaining blockers

**One, precisely scoped, now more thoroughly evidenced than DIST-GEN.15 left it:** `PeakVolumeBandMin`, `PeakVolumeBandMax`, `SelectedPeakReference`, and `CalibrationStartingVolumeKm` for the exact cell `16.0 × HalfMarathon × Intermediate × 3` remain `DECISION_REQUIRED`.

**Smallest exact next evidence need (§59):** a single, genuinely dedicated, published training plan explicitly combining all three of (a) a 15K/10-mile/16K race target, (b) an exact 3-running-days/week structure, and (c) an intermediate/existing-running-base population (not a first-timer/couch-to-X tier). Despite an exhaustive, good-faith search across both DIST-GEN.15's own queries and this phase's own additional targeted queries, and direct, full fetches of the two most obviously relevant candidate sources (Hal Higdon's own Novice and Intermediate 15K/10-Mile programs), no such source was found. The evidence gap appears structural, not merely under-searched: the one reputable author found to offer BOTH a 3-day and a higher-population-tier variant for this exact race distance (Hal Higdon) explicitly does not offer them at the same tier — his own 3-day option is paired with a lower population tier than his own "Intermediate" label.

**Exact finite candidate envelope for a possible future explicit product decision (§60, `PRODUCT_DECISION_REQUIRED`, not unilaterally selected by this phase):** should a future governance phase determine that an `EVIDENCE_INFORMED_PRODUCT_DEFAULT` is an acceptable path forward despite the population mismatch, the finite decision space this phase can offer, without itself choosing among the options, is:
- **Option 1**: adopt Higdon's own Novice-tier figures directly (peak ~24mi/38.6km, band perhaps [20,28] or similar), explicitly disclosing that this accepts a population tier one notch below Appsel's own Intermediate definition for this one cell only, on the reasoning that a real, reputable, exact-distance-and-frequency-matched source outweighs a same-population-but-wrong-distance or same-distance-but-wrong-frequency alternative.
- **Option 2**: adopt HM I3D's own already-frozen band/peak/calibration values directly for 16K×I×3D (band [28,40], peak 34.0, calibration 20.0), explicitly disclosing this as a deliberate, disclosed reuse of parent-family authority for this specific field cluster too (extending the same reasoning already applied to LR-share in DIST-GEN.15, on the theory that if LR-share is parent-family-scoped at 3D, peak-volume-family fields might be as well) — a real, principled, non-interpolative option this phase did not adopt on its own authority because DIST-GEN.15's own methodology audit (its own §20-22, consumed here) had already classified peak-volume/selected-peak/calibration as exact-target-owned, not parent-family-owned, at 4D, and extending that reclassification to 3D would itself be a genuine governance decision this phase was not authorized to make unilaterally.
- **Option 3**: commission genuinely new, non-literature-based evidence (e.g., a direct product/coaching-team decision informed by Appsel's own internal training philosophy rather than external published plans), which this phase has no visibility into and cannot evaluate.

This phase deliberately selects none of these three options on its own authority, consistent with §60's own explicit instruction, and reports `PRODUCT_DECISION_REQUIRED` alongside the smallest-evidence-need statement above, rather than forcing a number to unlock DIST-GEN.16.

---

## Final classification

**16K_INTERMEDIATE_3D_TARGET_FREQUENCY_PEAK_VOLUME_AUTHORITY_REMAINS_BLOCKED**

Fresh, genuinely targeted external research was performed (§12-24), including direct full fetches of the two most relevant candidate sources rather than relying on search snippets; source populations, frequencies, and distances were explicitly classified per source (§16-21); the exact dependency chain and established (non-formulaic) conventions for selected-peak and calibration were reconstructed and disclosed (§6-7, §29-32); every already-closed DIST-GEN.15 value was preserved unchanged and re-confirmed, not reopened (§34-38); no interpolation, nearest-anchor substitution, or distance-band concept was introduced anywhere (§45-47); canonical TEN_K/HM and the existing 15K/16K/18K public/dark matrices remain fully untouched (§49-50); and no production source changed. Peak-volume-band authority — and, downstream of it, selected peak and calibration — could not be responsibly frozen without either violating the governing prompt's own explicit, repeated prohibition on ratio/interpolation derivation, or silently adopting a population tier one notch below Appsel's own Intermediate definition without disclosing that as the deliberate, explicit product decision it would be. Per this engagement's own established norm and the governing prompt's own explicit instruction that "the preferred phase outcome is accurate authority, not `_CLOSED`," this phase reports the smallest exact remaining evidence/decision need (§58) rather than forcing closure. DIST-GEN.16 is not begun.
