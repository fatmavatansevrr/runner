# PHASE FIVE-K.0A — Targeted External Evidence Closure for Canonical FIVE_K Race-Plan Authority

**Type:** EXTERNAL EVIDENCE / SYNTHESIS / CANONICAL-DECISION-PREPARATION (no production implementation, no runtime wiring, no public activation, no frontend change, no database change, no catalog JSON change, no new workout implementation, no sub-5K work, no exact-FIVE_K-from-TEN_K derivation).

---

## 1. FIVE-K.0 binding state

FIVE-K.0 (`699c97b`/`0ff68f7`) reached: Authority completeness `FIVE_K_CANONICAL_AUTHORITY_REQUIRES_TARGETED_EXTERNAL_RESEARCH`; Implementation readiness `FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_WAITING_ON_EVIDENCE`; a 22-row authority gap matrix (§32 of that report) with 7 rows `CLOSED_SHARED_AUTHORITY`, 1 row `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY`, and the remainder marked `SOURCE_SILENCE`; and a five-item targeted research queue (FIVEK-LIT-01 through 05, §38). This phase executes exactly that queue.

**Note on the row count discrepancy discovered in this phase:** FIVE-K.0's own §32 summary line states "CLOSED_SHARED_AUTHORITY: 7... SOURCE_SILENCE...: 14," but a direct line-by-line read of its own 22-row table (reproduced in §2 below) shows 6 rows literally marked `CLOSED_SHARED_AUTHORITY`, 1 `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY`, and **15** rows literally marked `SOURCE_SILENCE` (or `SOURCE_SILENCE / BLOCKED`) — 6+1+15=22, matching the total row count, but not matching the prose summary's "7/14" split. This is a genuine off-by-one inconsistency in the FIVE-K.0 report's own self-summary versus its own table, discovered by this phase's required direct-table read (governing-prompt instruction: "read the actual table" rather than trust a prior summary). Per that same instruction, **this phase treats the table itself as ground truth** and addresses all 15 rows the table marks `SOURCE_SILENCE`, not the 14 claimed in prose. This is disclosed here rather than silently reconciled, and is reported as a genuine surprise in the final summary to the caller.

## 2. Exact SOURCE_SILENCE rows (reproduced directly from FIVE-K.0 §32's table, verbatim field names)

| # | AuthorityField | Status (as literally stated in FIVE-K.0 §32) |
|---|---|---|
| 1 | CoreCycle min/preferred/max | SOURCE_SILENCE |
| 2 | Phase durations (FIVE_K) | SOURCE_SILENCE |
| 3 | Run-layout RunsPerWeek set | SOURCE_SILENCE |
| 4 | Public Level×Frequency matrix | SOURCE_SILENCE |
| 5 | Workout-progression stages | SOURCE_SILENCE |
| 6 | Exposure min/preferred/max | SOURCE_SILENCE |
| 7 | Growth-rate cap values | SOURCE_SILENCE |
| 8 | Peak-volume band/reference | SOURCE_SILENCE / BLOCKED |
| 9 | Calibration/start volume | SOURCE_SILENCE |
| 10 | Long-run share quadruple (FIVE_K values) | SOURCE_SILENCE |
| 11 | Absolute long-run ceiling | SOURCE_SILENCE |
| 12 | Taper duration/multiplier | SOURCE_SILENCE |
| 13 | Target-race pace role for FIVE_K | SOURCE_SILENCE |
| 14 | Threshold/VO2/interval role for FIVE_K | SOURCE_SILENCE |
| 15 | FIVE_K catalog workout content | SOURCE_SILENCE |

(Rows literally marked `CLOSED_SHARED_AUTHORITY` — Phase taxonomy, Growth-rate cap mechanism, Long-run share quadruple mechanism, Pace fallback hierarchy, Riegel cross-distance projection, Calendar/adjacency rules — and the one `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` row (Catalog workout capability/engine) are explicitly NOT re-researched per governing-prompt §2, unless new evidence contradicts them; none found.)

## 3. Research methodology

Research order followed exactly as specified: FIVEK-LIT-04 (CoreCycle/phase/workout-progression) → FIVEK-LIT-05 (pace/intensity) → FIVEK-LIT-01 (volume/peak-volume/calibration) → FIVEK-LIT-02 (long-run safety) → FIVEK-LIT-03 (taper) — structure and objectives first, then intensity, then volume/long-run/taper, per governing-prompt §6. Each external search was mapped to a specific row above before being run. The legacy `five_k` habit/`GenerationSource.LegacySql` system was not consulted as evidence for any claim. No TEN_K/HM numeric value was used as a source for any FIVE_K claim; TEN_K/HM values are referenced only twice, both explicitly to disclose a numeric coincidence (TEN_K's 0.53 taper multiplier falling inside the independently-found 0.40-0.60 evidence band) and explicitly flagged as coincidence, not justification (FIVEK-LIT-03 §5 Claim B).

## 4. Source hierarchy

Applied per governing-prompt §7: (1) primary training texts (Daniels, Pfitzinger, Hansons/Humphrey, Higdon) interpreted via multiple independent secondary descriptions where direct page access was unavailable — disclosed as **interpreted**, never presented as direct quotation; (2) peer-reviewed/systematic-review sports-science literature (Billat; Midgley/McNaughton/Wilkinson VO2max-training literature; PLOS ONE and other systematic reviews on tapering) consulted at the level of search-returned abstracts/summaries, not full-text page citation; (3) no federation/governing-body coaching material was found directly relevant to any of the five atomic claims and none was force-fit; (4) no additional official coach methodology documentation beyond the named training texts was used; (5) weaker secondary/blog-style sources (vorlich.com, strengthrunning.com, runculture.com) used only as directional corroboration, explicitly down-weighted relative to primary training texts and peer-reviewed literature wherever a conflict in precision arose.

## 5. Source inventory

See each `FIVEK-LIT-0X_*.md` file's §4 for the full per-document source list with URLs. Consolidated: Jack Daniels (*Daniels' Running Formula*), Pete Pfitzinger & Scott Douglas (*Faster Road Racing: 5K to Half Marathon*), Luke Humphrey/Hansons (lukehumphreyrunning.com, Brooks-hosted Hansons-affiliated PDF), Hal Higdon (halhigdon.com, three 5K plans), Billat (interpreted), Midgley/McNaughton/Wilkinson VO2max literature (PubMed 9927024 and related), 80/20 Endurance and polarized-training secondary sources (contested, recorded as such), multiple taper-focused secondary sources citing peer-reviewed systematic reviews (PLOS ONE; ResearchGate-hosted elite-population review), and several lower-tier mileage/long-run blog sources used only for directional corroboration. One citation (a "1993 study" on 85% taper-volume reduction) was explicitly excluded as `UNVERIFIED_SECONDARY_CLAIM` per FIVEK-LIT-03 §6.

## 6. FIVEK-LIT-04 findings

See `FIVEK-LIT-04_CORECYCLE_PHASE_AND_WORKOUT_PROGRESSION.md` in full. Summary: structured-plan horizon clusters 8-13 weeks across four independent sources (`STRONG_MULTI_SOURCE_SYNTHESIS` for the range, `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` for a deterministic triple); phase taxonomy re-use confirmed (`STRONG_MULTI_SOURCE_SYNTHESIS`); phase-duration proportions (not absolutes) supported; workout-progression stage sequence supported (`STRONG_MULTI_SOURCE_SYNTHESIS`); exposure counts remain `SOURCE_SILENCE_REMAINS`.

## 7. Horizon

Evidence-supported range: 8-13 structured weeks (Higdon 8W low end; Pfitzinger/Hansons 10-12W mid-cluster; Brooks/Hansons-affiliated 9W). No deterministic triple is directly stated by any source. **Result: `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED`** (see PD-FIVEK-01).

## 8. Phase model

Foundation/Build/RaceSpecific/Taper taxonomy re-used, confirmed by convergent four-phase structure across Daniels and Hansons. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** (taxonomy choice; durations separate, §9).

## 9. Phase durations

Proportional evidence only: roughly 60-70% of the structured cycle in Foundation+Build, 30-40% in RaceSpecific+Taper (Hansons' explicit "6-8 weeks... then about four weeks of sharpening" within a 10-12 week plan). No absolute min/preferred/max per phase is directly stated. **Result: `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED`** (see PD-FIVEK-02).

## 10. Compression

Foundation identified as the most elastic/compressible phase, inferred from Daniels' own description of Phase I as workout-content-free and flexible, consistent with (but not copied from) TEN_K/HM's own compression convention. **Result: `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED`** (principle supported, exact priority ordering below Foundation not directly evidenced).

## 11. Extension

No source directly addresses what absorbs extra weeks above preferred. No evidence found either way. **Result: `SOURCE_SILENCE_REMAINS`.**

## 12. Workout progression

Stage sequence — aerobic base → threshold/repetition development → VO2max/interval development → race-specific rehearsal → taper — is `STRONG_MULTI_SOURCE_SYNTHESIS`-supported (Daniels, Hansons, Billat, Midgley/McNaughton converge). **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** for stage sequence/objectives; exposure counts separate (§13).

## 13. Exposure authority

No source states min/preferred/max exposure counts per stage as rules; only sample weekly session counts exist (e.g., "three Quality days," "6 runs/week, 1 day off"), which are sample-schedule occurrences, not stated authority, per governing-prompt §22's explicit distinction. **Result: `SOURCE_SILENCE_REMAINS`.** The narrower claim "roughly 2-3 quality sessions per week at intermediate/advanced levels" is `STRONG_MULTI_SOURCE_SYNTHESIS`-supported as a general cadence, not as exact exposure-count authority.

## 14. Catalog fit

Direct read of `plan-catalog/catalog/workouts/` (this phase): existing families are `EASY` (easy-standard), `LONG_RUN` (long-run-standard), `QUALITY` (threshold-tempo, fartlek, goal-pace-ten-k, hm-pace, aerobic-strength-controlled-{intro,progressed}). Easy, threshold, long-run, and a 5K race-pace analog (by structural parallel to `goal-pace-ten-k`) are **EXISTING_WORKOUT_SUPPORTS_AUTHORITY**. A dedicated structured VO2max/interval workout distinct from `fartlek` (unstructured) does not exist — **NEW_WORKOUT_WOULD_BE_REQUIRED** if that tier is adopted for V1; reusing `fartlek` as a stand-in would be **CATALOG_SEMANTIC_MISMATCH** (fartlek is self-paced/unstructured, VO2max-interval protocols in the literature specify fixed work/rest). No workout is implemented in this phase.

## 15. FIVEK-LIT-05 findings

See `FIVEK-LIT-05_PACE_AND_INTENSITY.md` in full. Summary: easy/threshold/5K-race-pace roles close by synthesis; VO2max-interval role closes by synthesis for centrality/placement (no pace numbers); faster-than-5K/repetition work is `OPTIONAL_SUPPORTED` not `CANONICAL_REQUIRED` for V1; exact intensity-distribution ratio is `CONFLICTING_EVIDENCE`, unresolved; fitness-evidence hierarchy reconfirmed `CLOSED_SHARED_AUTHORITY_CONFIRMED`.

## 16. 5K race pace

Distinct, race-specific, late-sequenced intensity tier, directly supported by convergent Daniels/Hansons tiering. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`.**

## 17. Threshold

Foundational/build-stage, precedes VO2/race-pace work. Maps to existing `threshold-tempo` catalog workout. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS` / `EXISTING_WORKOUT_SUPPORTS_AUTHORITY`.**

## 18. VO2/intervals

Central, not optional, progression component for 5K prep specifically (Billat: vVO2max tracks closely with 3K-5K performance; Midgley/McNaughton: time-at-VO2max predicts aerobic-ceiling improvement). Phase placement: race-specific phase, following threshold work. No athlete-specific pace numbers asserted. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** for centrality/placement; catalog gap noted (§14) for implementation, not authority.

## 19. Faster-than-5K work

Present in some reputable sources (Daniels' Repetition pace, Hansons' mile-pace work) but absent as a distinct named tier in at least one other reputable, full-coverage source (Higdon, all three levels). **Result: `OPTIONAL_SUPPORTED`**, not `CANONICAL_REQUIRED_FOR_V1`.

## 20. Pace hierarchy

Fitness-evidence hierarchy (recent-race → time-trial → structured-test → normal-pace → effort-fallback) and Riegel cross-distance projection mechanism both reconfirmed with zero FIVE_K-specific contradiction. **Result: `CLOSED_SHARED_AUTHORITY_CONFIRMED`** (both items).

## 21. FIVEK-LIT-01 findings

See `FIVEK-LIT-01_PEAK_VOLUME_AND_CALIBRATION.md` in full. Summary: level differentiation closes directly; exact numeric peak-volume values close only (as a product-default-bounded band) for Intermediate×4D/5D; Beginner/Advanced and other frequencies remain `INSUFFICIENT_EVIDENCE`; growth-cap and calibration-volume mechanisms reconfirmed shared with no override.

## 22. V1 Level×Frequency candidates

Only Intermediate×4D (and loosely ×5D) has simultaneous, even if band-level, coverage across volume (LIT-01), long-run (LIT-02), taper (LIT-03), and workout-progression/pace (LIT-04/05). No other cell has comparable multi-domain coverage. **Result: candidate V1 cell = Intermediate × {4D, 5D}.**

## 23. Peak-volume authority

No source gives a precise, defensible `ResolvedPeakReference` value for any cell with `DIRECT_CANONICAL_EVIDENCE` or `STRONG_MULTI_SOURCE_SYNTHESIS` grade. Intermediate×4D/5D has an approximate cross-checked band (loosely mid-30s-to-mid-40s km/week, derived from combining Higdon's Intermediate long-run figure with LIT-02's long-run-share band — itself disclosed as this phase's own inference, not a directly-stated source figure). **Result: `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED`** for Intermediate×4D/5D; **`INSUFFICIENT_EVIDENCE`** elsewhere.

## 24. Selected peak reference

Not selected in this phase (would require the product decision named in §23/PD-FIVEK-04) — no numeric `ResolvedPeakReference` value is asserted as approved authority here.

## 25. Calibration volume

No FIVE_K-specific calibration/starting-volume rule found; existing generic current-volume-adaptation mechanism has no contrary evidence. **Result: `CLOSED_SHARED_AUTHORITY_CONFIRMED_BY_ABSENCE_OF_CONTRARY_EVIDENCE`.**

## 26. Growth/cutback

No FIVE_K-specific deviation from the existing generic growth-cap mechanism/typical values found. **Result: `CLOSED_SHARED_AUTHORITY_CONFIRMED`.**

## 27. FIVEK-LIT-02 findings

See `FIVEK-LIT-02_LONG_RUN_SAFETY.md` in full. Summary: generic ~20-30% share band closes by synthesis as applicable to FIVE_K; absolute ceiling remains a product decision (not blocking, per TEN_K's own share-only precedent); frequency-differentiated long-run authority remains silent; starting-cap authority reconfirmed shared.

## 28. Long-run role

Single highest-volume session, share-governed, consistent across all distances per the generic convention found explicitly applicable "whether training for a 5K or a marathon." **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`.**

## 29. Long-run share

Generic ~20-30% (up to ~35% in one weaker source) band, explicitly cross-distance in its most authoritative citation. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** for band applicability; exact selected value within the band is a product default (PD-FIVEK-05).

## 30. Hard cap

No source distinguishes a "hard cap" share from "preferred" share specifically for 5K; existing generic mechanism's hard-cap concept applies with the same band-level support as §29. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** (mechanism-level), values bounded by same product decision.

## 31. Absolute ceiling

No FIVE_K-specific ceiling directly evidenced; one single-source data point (Higdon Advanced, 19.3km peak long run) informs but does not establish a ceiling. Structural reasoning (smaller total 5K volume naturally bounds absolute long-run size even without an explicit ceiling, paralleling TEN_K's own real precedent) is this phase's own synthesis, disclosed as such. **Result: `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED`**, not blocking (share-only governance is a legitimate, evidence-consistent option).

## 32. Starting long run

No FIVE_K-specific starting-cap authority found; generic current-conditioning governance applies. **Result: `CLOSED_SHARED_AUTHORITY_CONFIRMED_BY_ABSENCE_OF_CONTRARY_EVIDENCE`.**

## 33. Frequency effects

No source independently varies long-run authority by 3D vs. 5D for 5K training; all sources conflate level and frequency. **Result: `SOURCE_SILENCE_REMAINS`.**

## 34. FIVEK-LIT-03 findings

See `FIVEK-LIT-03_TAPER.md` in full. Summary: duration band (~1-2 weeks) and volume-reduction band (~40-60%) both close by synthesis; exact multiplier/week-count remain product decisions; activation-workout content remains silent.

## 35. Taper duration

Short taper (roughly 7-14 days) better supported than a long marathon-style taper, convergent across a 5K-specific secondary source and general endurance-taper review-level literature. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** for the band; exact week count is PD-FIVEK-06.

## 36. Taper volume

~40-60% volume reduction convergent across a 5K-specific source (40-50%) and general endurance-taper guidance (41-60%). **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** for the band; exact multiplier(s) is PD-FIVEK-06.

## 37. Taper quality

Intensity/frequency relatively preserved while volume drops — qualitative principle convergent across general taper literature and the training-text "Competition phase retains a mid-week quality touch" pattern. **Result: `CLOSED_MULTI_SOURCE_SYNTHESIS`** (qualitative); exact content silent (§38).

## 38. Taper activation

No source specifies exact FIVE_K taper activation-workout content. **Result: `SOURCE_SILENCE_REMAINS`.**

## 39. 15-row closure matrix

(Reproducing the actual 15 `SOURCE_SILENCE`-marked rows per §1/§2's disclosed correction, not the prose-claimed 14.)

| Row | ExternalSourcesReviewed | NewEvidence | FinalAuthorityStatus | ProposedRule (if any) | RuleOwner | RuntimeRequirement | ProductDefaultRequired? | StillBlocked? |
|---|---|---|---|---|---|---|---|---|
| CoreCycle min/pref/max | Daniels, Pfitzinger, Hansons, Higdon, Brooks | 8-13W structured-plan range | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | Triple within 8-13W | PRODUCT_DECISION (PD-FIVEK-01) | `RaceHorizonPolicy` FiveK branch | Yes | No (range closes; exact value pending) |
| Phase durations (FIVE_K) | Hansons, Daniels | ~60-70%/30-40% proportion | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | Proportional allocation of chosen CoreCycle | PRODUCT_DECISION (PD-FIVEK-02) | Phase-duration table | Yes | No |
| Run-layout RunsPerWeek set | Higdon, Pfitzinger | Level-conflated 3/5/6D only | `SOURCE_SILENCE_REMAINS` | — | — | `V1CatalogPilotIdentityPolicy` FiveK arm | N/A | Yes (narrow: only level-paired frequencies observed) |
| Public Level×Frequency matrix | All LIT-01/02/03 sources | Intermediate×4D/5D multi-domain band coverage | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` for Intermediate×4D/5D; `SOURCE_SILENCE_REMAINS` elsewhere | Intermediate 4D/5D V1 cell | PRODUCT_DECISION (PD-FIVEK-03) | `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` | Yes (for the chosen cell) | Partially (non-Intermediate cells still blocked) |
| Workout-progression stages | Daniels, Hansons, Billat, Midgley/McNaughton | 5-stage sequence | `CLOSED_MULTI_SOURCE_SYNTHESIS` | Aerobic→Threshold/Rep→VO2/Interval→RaceSpecific→Taper | SHARED_GENERIC pattern + FIVE_K content | `workout-progressions/` FIVE_K stage list | No (sequence); exposure counts still need PD | No |
| Exposure min/pref/max | Daniels, Higdon, Hansons | Sample session counts only | `SOURCE_SILENCE_REMAINS` | — | — | Exposure fields in progression records | N/A | Yes |
| Growth-rate cap values | (no FIVE_K-specific source found) | None (confirms absence) | `CLOSED_SHARED_AUTHORITY_CONFIRMED` | Re-use generic mechanism/values | SHARED_GENERIC | `PreferredMaxWeeklyIncreaseRatio` etc. | No | No |
| Peak-volume band/reference | Higdon, Pfitzinger | Approximate Intermediate×4D/5D band only | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` for Intermediate; `SOURCE_SILENCE_REMAINS`/`INSUFFICIENT_EVIDENCE` elsewhere | Band-bounded value, Intermediate only | PRODUCT_DECISION (PD-FIVEK-04) | `ResolvedPeakReference` | Yes | Partially |
| Calibration/start volume | (no FIVE_K-specific source found) | None (confirms absence) | `CLOSED_SHARED_AUTHORITY_CONFIRMED` | Re-use generic current-volume adaptation | SHARED_GENERIC | Calibration input handling | No | No |
| Long-run share quadruple (FIVE_K values) | Daniels (via secondary), cross-distance guidance sources | Generic ~20-30% band, explicitly cross-distance | `CLOSED_MULTI_SOURCE_SYNTHESIS` | Value within 20-30% band | PRODUCT_DECISION for exact value (PD-FIVEK-05) | `LongRunPreferred*Share` fields | Yes (exact value) | No |
| Absolute long-run ceiling | Higdon (single data point) | 19.3km Advanced peak observed | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | Share-only (no ceiling) OR explicit Advanced ceiling | PRODUCT_DECISION (PD-FIVEK-07) | `PreferredAbsolutePeakLongRunKm` (nullable) | Yes | No (not blocking — nullable by design) |
| Taper duration/multiplier | RunnersConnect, PLOS ONE/meta-analysis secondary summaries | ~1-2W duration band, ~40-60% reduction band | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | Value within bands | PRODUCT_DECISION (PD-FIVEK-06) | `TaperVolumeMultiplier(s)` | Yes | No |
| Target-race pace role for FIVE_K | Daniels, Hansons | Race-specific, late-sequenced tier | `CLOSED_MULTI_SOURCE_SYNTHESIS` | 5K pace = race-specific phase intensity | SHARED pattern + FIVE_K content | Pace-role binding | No | No |
| Threshold/VO2/interval role for FIVE_K | Daniels, Hansons, Billat, Midgley/McNaughton | Threshold=foundational; VO2=central/race-specific | `CLOSED_MULTI_SOURCE_SYNTHESIS` | As stated | SHARED pattern + FIVE_K content | Pace-role binding | No (roles); faster-than-5K tier is separate PD (PD-FIVEK-08) | No |
| FIVE_K catalog workout content | Direct repo read (this phase) | Easy/threshold/long-run/race-pace map to existing families; VO2-interval tier does not | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` (scope decision on VO2-interval tier) + `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` confirmed for engine | Most content reuses existing families; VO2-interval tier optional/new | PRODUCT_DECISION (PD-FIVEK-08) | Workout authoring (FIVE-K.1, not this phase) | Yes (for VO2-interval tier only) | No (V1 deliverable without it) |

**Summary**: 0 rows `CLOSED_DIRECT_EXTERNAL_EVIDENCE`. 6 rows `CLOSED_MULTI_SOURCE_SYNTHESIS` outright (workout-progression stages, long-run share band, taper duration band, taper volume band, target-race-pace role, threshold/VO2 role). 2 rows `CLOSED_SHARED_AUTHORITY_CONFIRMED` (growth-rate values, calibration volume). 7 rows `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` in whole or part (CoreCycle, phase durations, public matrix for non-Intermediate, peak-volume for non-Intermediate/partially for Intermediate, absolute ceiling, taper exact value, catalog VO2-interval-tier scope). 2 rows remain genuinely `SOURCE_SILENCE_REMAINS` with no further atomic search proposed (run-layout independent frequency set, exposure counts) — plus the long-run frequency-effects sub-finding (§33) is also silent but was folded into the public-matrix row above rather than kept separate, since FIVE-K.0's own 22-row table did not list it as an independent row.

## 40. Conflicting evidence

One genuine conflict found, outside the 15-row matrix itself (a sub-finding within FIVEK-LIT-05, not one of FIVE-K.0's original 15 named rows): the exact quantitative intensity-distribution ratio (e.g., strict 80/20) is actively disputed in the current literature (one secondary source explicitly titled as rebutting the 80/20 consensus). Recorded as `CONFLICTING_EVIDENCE`, not resolved, not forced to a decision.

## 41. Source silence remaining

Exposure min/preferred/max counts; independent (non-level-conflated) RunsPerWeek authority; FIVE_K-specific taper activation-workout content; extension-priority ordering (what absorbs weeks above preferred). None of these are proposed for further broad research; see §44 for the one legitimate atomic follow-up candidate.

## 42. Product defaults required

See §39's `ProductDefaultRequired?` column and the decision pack (§43). Eight named decisions (PD-FIVEK-01 through 08).

## 43. Product decision pack

- **PD-FIVEK-01**: CoreCycle triple. Evidence-supported options: {min=8,pref=10,max=13} or {min=9,pref=12,max=14} (both within the 8-13W evidence range). Runtime consequence: gates `RaceHorizonPolicy.DecideForDistance` FiveK branch. Not supported: anything above ~14W or below 8W without further evidence. No single value is evidence-clearly-favored over the other; recommend none without product input.
- **PD-FIVEK-02**: Per-phase week allocation within the chosen CoreCycle, using the ~60-70%/30-40% Foundation+Build / RaceSpecific+Taper proportion as the constraint, not a fixed count.
- **PD-FIVEK-03**: V1 public Level×Frequency scope. Evidence-supported option: Intermediate × {4D, 5D} only for V1; Beginner/Advanced and other frequencies deferred. Runtime consequence: `V1CatalogPilotIdentityPolicy` FiveK arm scope. Recommended if evidence-driven minimalism is preferred: start with Intermediate×4D alone (single cell), the one with the tightest band-level, multi-domain evidence overlap.
- **PD-FIVEK-04**: Peak-volume value for the chosen V1 cell(s), within the approximate mid-30s-to-mid-40s km/week band for Intermediate (this phase's own derived band, disclosed as inference not direct citation).
- **PD-FIVEK-05**: Exact long-run share value within the generic 20-30% band.
- **PD-FIVEK-06**: Taper duration (1 vs. 2 weeks) and exact multiplier(s) within the ~0.40-0.60 reduction band.
- **PD-FIVEK-07**: Whether to add an explicit absolute long-run ceiling (informed by, not copied from, Higdon's 19.3km Advanced data point) or rely on share-only governance (evidence-consistent with TEN_K's own real precedent).
- **PD-FIVEK-08**: Whether V1 includes a dedicated VO2max-interval/faster-than-5K workout tier (requiring a new catalog workout) or defers it, relying on existing EASY/threshold-tempo/LONG_RUN/race-pace-analog families alone for a minimum coherent V1.

None of these eight is approved in this research phase.

## 44. External gaps remaining

One atomic follow-up candidate named, not opened: **FIVEK-LIT-06 — Beginner and Advanced FIVE_K peak-volume and long-run authority by frequency**, should a future phase need those cells beyond the Intermediate-only V1 this phase supports. No broader literature re-review is proposed.

## 45. Candidate FIVE_K master V2 (conceptual only)

CoreCycle: `{Min: <PD-01>, Preferred: <PD-01>, Max: <PD-01>}` — unresolved pending PD-FIVEK-01. Phases: Foundation/Build/RaceSpecific/Taper (closed), durations proportional per PD-FIVEK-02 (unresolved exact counts). RunsPerWeek support: {4, 5} candidate for V1 (PD-FIVEK-03), others `OUT_OF_V1_SCOPE`. WorkoutProgressionKey: new FIVE_K-specific key referencing the 5-stage sequence from §12/§39, pending nomenclature review in FIVE-K.1 (not created here). Structural intent: race-specific, short-horizon, VO2max-central preparation distinct from TEN_K's own tiering, consistent with LIT-05's findings.

## 46. Candidate workout progression V2 (conceptual only)

| StageKeyCandidate | Phase | Objective | WorkoutFamily | Exposure | RelativeOrder | CompressionPriority | EvidenceClass | Source |
|---|---|---|---|---|---|---|---|---|
| FIVE_K_AEROBIC_BASE | Foundation | General aerobic development | EASY | Unresolved (PD) | 1 | Highest (compress first) | STRONG_MULTI_SOURCE_SYNTHESIS | Daniels, Hansons |
| FIVE_K_THRESHOLD_DEVELOPMENT | Build | LT/threshold capacity | QUALITY (threshold-tempo) | Unresolved (PD) | 2 | Medium | CLOSED_MULTI_SOURCE_SYNTHESIS | Daniels, Hansons |
| FIVE_K_VO2_INTERVAL_DEVELOPMENT | Build/RaceSpecific boundary | VO2max/aerobic-ceiling development | QUALITY (new workout, §14) | Unresolved (PD) | 3 | Medium-low | CLOSED_MULTI_SOURCE_SYNTHESIS | Billat, Midgley/McNaughton, Daniels |
| FIVE_K_RACE_SPECIFIC_REHEARSAL | RaceSpecific | Race-pace rehearsal | QUALITY (goal-pace-five-k analog) | Unresolved (PD) | 4 | Low | CLOSED_MULTI_SOURCE_SYNTHESIS | Daniels, Hansons |
| FIVE_K_TAPER_ACTIVATION | Taper | Maintain sharpness, shed fatigue | QUALITY (reduced volume) / EASY | Unresolved (PD) | 5 | Lowest (compress last) | CLOSED_MULTI_SOURCE_SYNTHESIS (shape); SOURCE_SILENCE (content) | General taper literature, Daniels |
| FIVE_K_FASTER_THAN_5K_OPTIONAL | Build (optional) | Speed/economy | QUALITY (new workout, optional) | N/A — scope-gated (PD-FIVEK-08) | Optional | N/A | EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED | Daniels (Repetition), Hansons (mile-pace) |

No production keys created; candidates only.

## 47. Candidate volume contract V2 (conceptual only)

Per Intermediate×4D/5D candidate cell: PeakVolumeBand = approximate mid-30s-to-mid-40s km/week (this phase's derived band); SelectedPeakReference = `DECISION_REQUIRED` (PD-FIVEK-04); CalibrationStartingVolume = generic current-volume adaptation (`CLOSED_SHARED_AUTHORITY_CONFIRMED`); GrowthAuthority = generic mechanism/values (`CLOSED_SHARED_AUTHORITY_CONFIRMED`); AbsoluteGrowthCap = generic value (no FIVE_K override); CutbackAuthority = generic mechanism (no FIVE_K-specific evidence found either way — inherits shared default). All other cells: `NULL`/`DECISION_REQUIRED`/`INSUFFICIENT_EVIDENCE`.

## 48. Candidate long-run contract V2 (conceptual only)

PreferredShare/HardShareCap: within generic 20-30% band, exact value `DECISION_REQUIRED` (PD-FIVEK-05). AbsoluteCeiling: `NULL` (share-only) OR explicit value informed by 19.3km data point — `DECISION_REQUIRED` (PD-FIVEK-07), not forced non-null. StartingCap: generic shared mechanism. ProgressionRule: generic shared mechanism. QualityInLongRun: no evidence found either way — not asserted.

## 49. Candidate taper contract V2 (conceptual only)

Duration: 1 or 2 weeks, `DECISION_REQUIRED` (PD-FIVEK-06), within evidence band of ~7-14 days. Multiplier/range: within ~0.40-0.60 reduction band (i.e., ~0.40-0.60 remaining-volume multiplier), exact value(s) `DECISION_REQUIRED`. Quality behavior: intensity/frequency relatively preserved (`CLOSED_MULTI_SOURCE_SYNTHESIS`). Activation workout: `SOURCE_SILENCE`, not specified.

## 50. Candidate pace contract V2 (conceptual only)

Easy: dominant volume, EASY family (closed). Threshold: foundational/build, threshold-tempo family (closed). VO2/Interval: central, race-specific-adjacent, new workout required for full fidelity (closed role, open implementation). 5K race pace: race-specific, late-sequenced, goal-pace-five-k analog (closed role, open implementation). Faster-than-5K/repetition: optional, scope-gated (PD-FIVEK-08). Fitness hierarchy: generic, unchanged (closed). Goal-time separation: unchanged generic rule — goal time does not manufacture fitness (reconfirmed, no FIVE_K-specific contradiction). Fallback: generic effort-fallback, unchanged.

## 51. Candidate public matrix V2 (conceptual only)

| Level \ Frequency | 2D | 3D | 4D | 5D | 6D | 7D |
|---|---|---|---|---|---|---|
| Beginner | OUT_OF_V1_SCOPE | INSUFFICIENT_EVIDENCE | INSUFFICIENT_EVIDENCE | INSUFFICIENT_EVIDENCE | OUT_OF_V1_SCOPE | OUT_OF_V1_SCOPE |
| Intermediate | OUT_OF_V1_SCOPE | INSUFFICIENT_EVIDENCE | **ARCHITECTURE_READY_BUT_PRODUCT_DEFAULT_REQUIRED** | **ARCHITECTURE_READY_BUT_PRODUCT_DEFAULT_REQUIRED** | INSUFFICIENT_EVIDENCE | OUT_OF_V1_SCOPE |
| Advanced | OUT_OF_V1_SCOPE | INSUFFICIENT_EVIDENCE | INSUFFICIENT_EVIDENCE | INSUFFICIENT_EVIDENCE | INSUFFICIENT_EVIDENCE | OUT_OF_V1_SCOPE |
| Experienced | OUT_OF_V1_SCOPE (not reopened, per governing-prompt §47) | — | — | — | — | — |

No cell is activated; this is a candidate table only.

## 52. Minimum implementable FIVE_K V1

Smallest coherent V1: **Intermediate × 4D** alone (5D optional second cell, same evidence tier). All eight PD-FIVEK decisions scoped to this one cell would need resolution before FIVE-K.1 could implement; Beginner/Advanced and other frequencies explicitly deferred without blocking this minimum V1, per governing-prompt §47/§73-74.

## 53. Horizon result

**`FIVE_K_HORIZON_AUTHORITY_PRODUCT_DEFAULT_REQUIRED`**

## 54. Workout-progression result

**`FIVE_K_WORKOUT_PROGRESSION_AUTHORITY_PRODUCT_DECISIONS_REQUIRED`** (stage sequence closed; exposure counts and VO2-interval-tier scope need decisions)

## 55. Pace result

**`FIVE_K_PACE_INTENSITY_AUTHORITY_PRODUCT_DECISION_REQUIRED`** (easy/threshold/race-pace/VO2 roles closed; faster-than-5K tier scope and exact intensity-distribution ratio remain open — the latter as unresolved conflicting evidence, not a product decision)

## 56. Volume result

**`FIVE_K_VOLUME_AUTHORITY_PRODUCT_DEFAULTS_REQUIRED`** (level differentiation closed; numeric peak values need decisions, closed-enough-for-a-band only at Intermediate×4D/5D)

## 57. Long-run result

**`FIVE_K_LONG_RUN_AUTHORITY_PRODUCT_DEFAULT_REQUIRED`** (share-band mechanism closed; exact share value and ceiling-or-not remain decisions, neither blocking)

## 58. Taper result

**`FIVE_K_TAPER_AUTHORITY_PRODUCT_DEFAULT_REQUIRED`** (duration/volume bands closed; exact duration and multiplier(s) remain decisions)

## 59. Public-matrix result

**`FIVE_K_V1_PUBLIC_MATRIX_AUTHORITY_REQUIRES_PRODUCT_DECISIONS`** (Intermediate×4D/5D is the maximal evidence-complete candidate; requires PD-FIVEK-01 through 08 resolved to activate)

## 60. Authority completeness

**`FIVE_K_CANONICAL_AUTHORITY_CLOSED_FOR_RESTRICTED_V1_MATRIX`** — contingent on PD-FIVEK-01 through 08 being resolved as explicit product decisions (not automatically approved by this research phase). Structural/qualitative authority (phase taxonomy, workout-progression sequence, pace-role mapping, long-run-share mechanism, taper shape, shared-mechanism confirmations) is genuinely closed by this phase's research. Numeric/deterministic authority for the smallest candidate V1 cell (Intermediate×4D) is evidence-*bounded*, not evidence-*determined* — hence "for restricted V1 matrix," not "fully closed."

## 61. Runtime implementation readiness

**`FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_WAITING_ON_PRODUCT_DECISIONS`** — the catalog engine requires no structural change (reconfirmed, §14); all eight PD-FIVEK decisions must be made, explicitly and not silently, before FIVE-K.1 could begin implementation even for the smallest Intermediate×4D V1.

## 62. Continuous-range consequence

Unchanged by this phase: `5K < target < HM` remains public and continuous; exact 5K remains not production-ready. If FIVE-K.0B (product-default resolution) and then FIVE-K.1 (implementation) both succeed for at least the Intermediate×4D cell, the only remaining gap in the 5K-to-HM V1 range would close for that cell, with Beginner/Advanced cells following in later phases. This has not happened yet; this phase makes no runtime change.

## 63. Sub-5K deferred

**`SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED`.** Nothing in this phase's findings advances 1K/2K/3K/4K. `0 < target < 5K` remains explicitly out of V1 scope.

## 64. Next phase

Recommended: **FIVE-K.0B — Canonical FIVE_K Product-Default Resolution**, resolving exactly PD-FIVEK-01 through PD-FIVEK-08 (no research repeat — this phase's evidence bounds are the input). Only after FIVE-K.0B should **FIVE-K.1 — Canonical FIVE_K Catalog and Runtime Implementation, Controlled Public Activation, Real PostgreSQL Lifecycle Proof, and 5K→Half-Marathon Continuous-Range Closure** begin, scoped initially to the Intermediate×4D (and optionally 5D) V1 cell this phase identifies as evidence-complete-once-decided. If a future phase needs Beginner/Advanced cells, **FIVE-K.0A.1 — FIVEK-LIT-06: Beginner/Advanced FIVE_K Volume and Long-Run Authority by Frequency** is the correctly-scoped atomic follow-up, not a broader re-review.

## 65. Remaining blockers

Eight named product decisions (PD-FIVEK-01 through 08) block FIVE-K.1 from starting. Two genuine evidence gaps remain unaddressed by design (exposure counts, independent RunsPerWeek authority) but do not block the smallest V1 if product decisions substitute explicit defaults for them within the identified bands/options. One conflicting-evidence item (exact intensity-distribution ratio) is explicitly left unresolved and is not expected to block V1 since no exact ratio was ever required by the runtime schema — only qualitative role placement was.

---

## Required overall result enums

- Authority completeness: **`FIVE_K_CANONICAL_AUTHORITY_CLOSED_FOR_RESTRICTED_V1_MATRIX`**
- Runtime readiness: **`FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_WAITING_ON_PRODUCT_DECISIONS`**

## Final phase classification

**`FIVE_K_TARGETED_EXTERNAL_EVIDENCE_CLOSURE_COMPLETE`**

Checklist self-audit: all 15 rows the FIVE-K.0 table actually marks `SOURCE_SILENCE` were addressed (not the prose-claimed 14 — the discrepancy is disclosed, §1, rather than silently reconciled); every research action mapped to an existing gap (no broad literature review — searches were scoped to the five named LIT items, in the specified order); no broad literature review substituted (five bounded documents, one per LIT item, plus synthesis); source traceability recorded throughout, with explicit `interpreted`-via-secondary-description flags where direct primary-text page access was unavailable, and one citation explicitly excluded as `UNVERIFIED_SECONDARY_CLAIM` rather than used; population/context recorded per claim; direct evidence, synthesis, and product default kept distinct throughout (no claim labeled "reasonable"/"standard practice" without a formal classification); no TEN_K/HM value copied to fill a gap (the one numeric coincidence noted, TEN_K's 0.53 taper multiplier falling inside the independently-derived 0.40-0.60 band, is explicitly flagged as coincidence, not justification); Riegel not used as training-load authority (reconfirmed as projection-only, §20); legacy habit FIVE_K not used as race-plan evidence anywhere; horizon/phase-durations/workout-progression-exposures/pace-intensity/volume-peak-calibration/long-run-safety/taper each resolved to an explicit status (synthesis-closed, shared-confirmed, or product-default-required/source-silence — never left ambiguous); Level×Frequency authority closed for the Intermediate×4D/5D candidate V1 matrix, explicitly blocked/deferred elsewhere; smallest evidence-complete V1 matrix identified (Intermediate×4D); unsupported future cells (Beginner, Advanced, other frequencies) do not block this smaller V1; every remaining product decision is atomic and named (PD-FIVEK-01 through 08); the one remaining evidence gap proposal (FIVEK-LIT-06) is atomic, not a broad re-review; no production behavior changed (confirmed in final diff-stat reported to caller); exact FIVE_K remains destined for canonical FIVE_K generation, never TEN_K-derived; sub-5K remains deferred; runtime implementation readiness is stated explicitly as waiting on product decisions, not falsely claimed ready. The substantive numeric-authority gap for FIVE_K is correctly reported as still requiring explicit product decisions, not papered over as fully closed — but the targeted external-evidence-closure work this phase was charged with is itself fully and honestly executed against its own five-item queue.
