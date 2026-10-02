# FIVEK-LIT-04 — CoreCycle Horizon, Phase Model, and Workout-Progression Structure

**Phase:** FIVE-K.0A (targeted evidence closure). **Maps to FIVE-K.0 blocker:** FK0-B4 (§47 of `PHASE_FIVE_K_0_...md`). **Research order:** 1st (structure/objectives before intensity/volume, per governing-prompt §6).

## 1. Research question

What is the canonical FIVE_K preparation horizon (minimum/preferred/maximum structured weeks), what phase taxonomy applies, what are the phase durations and compression/extension priorities, and what is the canonical workout-progression (stage sequence, objective, family, exposure counts)?

## 2. Runtime authority being sought

`RaceHorizonPolicy`-shaped CoreCycle triple for `FiveK`; a phase-duration table (Foundation/Build/RaceSpecific/Taper min/preferred/max); compression order (what is dropped first below preferred) and extension priority (what absorbs weeks above preferred); a `workout-progressions/` -shaped stage table (StageConcept, TrainingObjective, WorkoutFamily, Min/Preferred/MaxExposure, RelativeOrder, CompressionPriority).

## 3. Existing internal evidence already reviewed

Per FIVE-K.0 §3/§13/§15-20/§32: TEN_K's CoreCycle is 8/12/14 weeks (`RaceHorizonPolicy`), HM's is 10/14/16 — two independently-set triples, not a shared default; the phase taxonomy (Foundation/Build/RaceSpecific/Taper) is itself `CLOSED_SHARED_AUTHORITY` (schema vocabulary, `plan-template.schema.json`), but FIVE_K-specific durations and workout-progression stages were `SOURCE_SILENCE`. The catalog engine's family-dispatch pattern (`CatalogVolumeAndLongRunPlanner.Build`) is `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` — ready for a FIVE_K branch, carrying no content. Direct read of `plan-catalog/catalog/workouts/` (this phase, new) shows the existing workout family vocabulary is coarse: `EASY`, `LONG_RUN`, `QUALITY` (with `QUALITY` covering `threshold-tempo`, `fartlek`, `goal-pace-ten-k`, `hm-pace`, `aerobic-strength-controlled-{intro,progressed}`). No `goal-pace-five-k`, no dedicated VO2max/repetition-interval workout definition exists anywhere in `plan-catalog/catalog/workouts/`.

## 4. External sources

- Jack Daniels, *Daniels' Running Formula* — 5K/10K training-plan chapter, four-phase structure (Phase I Base/Phase II Repetition+Threshold/Phase III Interval+race-pace/Phase IV Competition). Exact edition/page not directly consulted (no verified physical/PDF access this phase); content reconstructed from multiple independent secondary descriptions of the book's own published plan structure (Running with Rock's phase-by-phase series, LetsRun.com community discussion threads, Buttondown's plan-structure summary) that agree on the same four-phase description. Classified as **interpreted**, not a direct page citation.
- Pete Pfitzinger & Scott Douglas, *Faster Road Racing: 5K to Half Marathon* — 12-week program length for 5K/10K/HM plans at beginner/intermediate/advanced levels; example 12-week schedule cited at 32-55 miles/week (interpreted via Goodreads/LetsRun secondary description, not page-verified).
- Luke Humphrey (Hansons), 5K training philosophy — Hansons 5K segments "typically 10-12 weeks," described as "6-8 weeks of increasing distances at pace and maximizing fitness, followed by about four weeks of sharpening" (lukehumphreyrunning.com, direct source page).
- Brooks Running / Hansons-affiliated, "5K Advanced 9-Week Training Plan" (PDF, brooksrunning.com) — a shorter, 9-week structured variant.
- Hal Higdon, 5K Training (Novice/Intermediate/Advanced) — all three published plans are 8 weeks (halhigdon.com, direct source pages), differing in weekly frequency/volume/workout content, not in horizon length.

## 5. Atomic claims, supporting evidence, population applicability

**Claim A — Structured 5K plan length clusters in an 8-13 week range across multiple independent, reputable sources, with Daniels offering a longer option when base-building is included.** Supporting: Higdon (8 weeks, all three levels, recreational/novice-to-veteran population), Pfitzinger (12 weeks, beginner/intermediate/advanced), Hansons (10-12 weeks, "segments"), Brooks/Hansons-affiliated advanced variant (9 weeks). Daniels' own four-phase plan is longer in total (18-24 weeks across secondary descriptions) but explicitly separates an unstructured/optional Phase I ("doesn't outline any specific workouts... opportunity to increase mileage") from the three structured phases that follow — so Daniels' own *structured* portion is plausibly shorter than its total length, though no source gives an exact week count for Phase I alone. Population: Higdon spans novice-to-veteran recreational; Pfitzinger/Hansons target recreational-to-sub-elite racers already running regularly. **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** for an 8-13 week structured-plan range; **not** direct evidence for any single deterministic week count.

**Claim B — A deterministic CoreCycle min/preferred/max triple is not independently stated by any single source the way TEN_K's 8/12/14 or HM's 10/14/16 are stated as such.** No source states "minimum X, preferred Y, maximum Z weeks" as an explicit named triple; each source publishes one fixed plan length (or, for Daniels, a longer phased structure). **Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED** — the range (8-13 structured weeks, Higdon's 8 as the low end, Pfitzinger/Hansons' 12 as a repeated mid-point, no strong multi-source anchor above 13 for a *structured, non-base-building* definition) supports selecting a deterministic triple as a product decision, not as directly-evidenced fact. See §53/PD-FIVEK items in the synthesis and phase report.

**Claim C — The Foundation/Build/RaceSpecific/Taper phase taxonomy maps onto the real four-source pattern (base/aerobic phase → speed/threshold development phase → race-specific/VO2-interval phase → competition/taper phase).** Daniels' own four phases (Base/Speed-Development/Race-Specific/Competition) and Hansons' two-part structure (base+speed-building, then sharpening) both independently resolve into the same four-stage shape the existing taxonomy already uses. **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** supporting re-use of the existing taxonomy (consistent with FIVE-K.0's own `CLOSED_SHARED_AUTHORITY` finding for the taxonomy as vocabulary) — no new phase-name invention required.

**Claim D — Relative phase-duration proportions, not absolute week counts, are supported.** Hansons: ~6-8 weeks of 10 within a ~12-week plan devoted to "increasing distances at pace and maximizing fitness" (Foundation+Build, roughly), ~4 weeks devoted to "sharpening" (RaceSpecific+Taper). Daniels' structure implies a smaller RaceSpecific/Competition fraction (Phase III+IV) relative to Base+Speed (Phase I+II), consistent in direction with Hansons. **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** for the *proportion* (roughly 60-70% Foundation+Build, 30-40% RaceSpecific+Taper) but **EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED** for exact min/preferred/max week counts per phase, since no source publishes those as an explicit per-phase triple.

**Claim E — Compression principle: the base/aerobic (Foundation) phase is the most elastic/compressible element, and taper is the least compressible.** Daniels' own description of Phase I as not having "any specific workouts" and being explicitly a flexible mileage/base-building window (vs. the fixed-structure Phases II-IV) is **interpreted**, not directly stated as a compression rule, as evidence that Foundation is the natural first phase to shorten when total horizon is constrained. No source directly states a compression/extension *priority order* as such. **Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED**, informed by this cross-source pattern (also consistent with, but not copied from, TEN_K/HM's own existing compression conventions — convergent, not derivative).

**Claim F — Workout-progression stage sequence: aerobic-base running → threshold/LT + repetition-pace development → VO2max/interval (3K-5K pace) development → race-specific rehearsal/race-pace simulation → taper (volume down, some intensity retained).** Supporting, convergently, across: Daniels (Phase II = Repetition + Threshold; Phase III = Interval ~3K-5K pace, shorter reps 800-1200m; Phase IV = Competition, mostly easy + weekend racing, one mid-week threshold session retained); Hansons ("start out with more LT repeats and 10K-pace work... progress down to 5K, 3K, and even some mile-pace work... 5K is largely aerobic... aerobic base is key"); Midgley/McNaughton VO2max-training literature (peer-reviewed: total time at ~90%+ VO2max is the strongest predictor of aerobic-ceiling improvement, with 3-5 minute repeats at vVO2max pace cited as an efficient means of accumulating it — PubMed 9927024 and related reviews); Billat (velocity-at-VO2max tracks closely with 3K-5K performance). **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** — compatible population (recreational-to-competitive 5K racers in the training-text sources; trained endurance athletes in the VO2max physiology literature), same directional conclusion (progress from general aerobic/threshold toward race-specific intensity, converging on 5K-adjacent paces late in the cycle).

**Claim G — Exact exposure counts (min/preferred/max repetitions of each stage's quality session) are NOT supported as stated rules.** Sources describe weekly *session counts* (Daniels: "three Quality days... rest of the days are E running"; Higdon Advanced: 6 runs/week, 1 day off; Hansons Advanced 9-week: tempo/hill/interval sessions "throughout the training cycle" with no stated min/max count) but these are sample-schedule occurrences within one published plan, not stated minimum/preferred/maximum exposure rules analogous to TEN_K's/HM's own workout-progression records. **Classification: SOURCE_SILENCE_REMAINS** for exact per-stage exposure min/preferred/max; **STRONG_MULTI_SOURCE_SYNTHESIS** only for the coarser claim "one hard/quality session type typically recurs weekly through most of the structured plan, with roughly 2-3 quality sessions per week being the common pattern across Daniels/Higdon-Advanced/Hansons-Advanced" (three independent sources agree on a 2-3 quality-session-per-week cadence at intermediate/advanced levels specifically).

## 6. Contrary/limiting evidence

No direct contradiction found between sources on the general phase-sequence direction (base → threshold/speed → VO2/race-specific → taper). The sources *disagree* on total length (8 weeks Higdon vs. 18-24 weeks Daniels-with-base-building) but this is a scope/definition difference (Higdon's 8-week plans assume an existing aerobic base and compress or omit a separate Foundation phase; Daniels' longer plan explicitly includes an extended, non-workout-prescribing base-building phase for runners without one) — not a true contradiction. Classified as scope difference, not `CONFLICTING_EVIDENCE`, per governing-prompt §54-57.

## 7. Population applicability

Higdon's three levels and Hansons' Beginner/Intermediate/Advanced/Elite line up reasonably with Appsel's Beginner/Intermediate/Advanced model. Daniels' plan targets "already-trained" runners with an existing aerobic base for Phases II-IV; its Phase I is explicitly for runners without one. Pfitzinger assumes a "reasonable base fitness level" (one cited reviewer's 15-20 mi/week base was found "notably below" what the back-half programs required). No elite-only source was used for horizon/phase claims in this file.

## 8. Source-silence notes

Exact CoreCycle triple: silent (Claim B). Exact per-phase week counts: silent beyond proportion (Claim D). Exact exposure min/preferred/max counts: silent (Claim G).

## 9. Canonical implication

The phase taxonomy itself needs no new invention (re-use Foundation/Build/RaceSpecific/Taper). The workout-progression *stage sequence and objectives* (aerobic base → threshold/repetition → VO2/interval → race-specific → taper) are evidence-supportable as `STRONG_MULTI_SOURCE_SYNTHESIS` and can be encoded as a canonical ordered stage list without numeric exposure counts attached.

## 10. Product-default implication

CoreCycle triple, exact phase-duration weeks, exact exposure min/preferred/max, and exact compression/extension priority all require an explicit product-default decision layered on top of the ranges/proportions/principles this research supports — they cannot be asserted as scientifically unique values. See phase report §42-43/PD-FIVEK-01/02/03.

## 11. Final classification

`FIVE_K_HORIZON_AUTHORITY_PRODUCT_DEFAULT_REQUIRED` (range/proportions closed by synthesis; deterministic triple requires product decision). `FIVE_K_WORKOUT_PROGRESSION_STAGE_SEQUENCE_CLOSED_MULTI_SOURCE_SYNTHESIS`; `FIVE_K_WORKOUT_PROGRESSION_EXPOSURE_COUNTS_SOURCE_SILENCE_REMAINS`.

## 12. Open gaps

Exact CoreCycle numeric triple (product decision, not evidence gap — evidence supports only a range). Exact exposure min/preferred/max per stage (genuine evidence gap — no further *targeted* search is proposed per governing-prompt §83; this is not an atomic claim with an identifiable missing source, it is the texts' own publication format not stating such rules).
