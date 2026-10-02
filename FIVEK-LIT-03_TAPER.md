# FIVEK-LIT-03 — Taper Authority for Canonical FIVE_K

**Phase:** FIVE-K.0A. **Maps to FIVE-K.0 blocker:** FK0-B3 (§47). **Research order:** 5th (last, after structure/intensity/volume/long-run, per governing-prompt §6).

## 1. Research question

What is FIVE_K's taper duration (minimum/preferred), volume-reduction semantics, quality-retention behavior, and activation-workout semantics — and does the evidence support a single multiplier, a range, or a context-dependent taper?

## 2. Runtime authority being sought

A `TaperVolumeMultiplier`/`TaperVolumeMultipliers` value or ordered list for FIVE_K, and the implied taper-week count (`ResolvedTaperVolumeMultipliers.Count`), analogous to TEN_K's single-week 0.53 or HM's two-week 0.70/0.43 — explicitly not copied from either.

## 3. Existing internal evidence already reviewed

FIVE-K.0 §12/§26/§32: taper was `SOURCE_SILENCE`/`BLOCKED` for FIVE_K specifically. `HalfMarathonParentTaperAuthority.cs`'s own doc comment (direct read, confirmed again unchanged) explicitly states TEN_K's taper is NOT assumed shared with HM's — i.e., this codebase's own convention already refuses default taper inheritance between distances, reinforcing that no TEN_K/HM taper value may be assumed for FIVE_K.

## 4. External sources

Peer-reviewed/systematic-review sources (via WebSearch secondary summarization, titles independently real and findable — not page-verified): a cited 2023 meta-analysis of taper studies describing 8-14 days of reduced training producing the best performance improvement across endurance sports generally (not 5K-specific); a PLOS ONE systematic review and meta-analysis, "Effects of tapering on performance in endurance athletes" (title verified as a real, locatable publication via search; full text not read in this phase, so internal findings are reported only at the level the search summary provided, which is itself a limitation disclosed here); a ResearchGate-hosted "The Effects of Tapering on Performance in Elite Endurance Runners: A Systematic Review" (title verified as real/locatable; population explicitly elite, a material population-applicability limit); a secondary coaching-advice source (runnersconnect.net, "How to Taper for a 5k: A Research-Backed Guide") stating "optimal taper duration for a 5K is 7-10 days" and "40 to 50% reduction recommended specifically for a 5K"; broader endurance-taper guidance (same and related secondary sources) describing a general "≤21-day taper... training volume progressively reduced by 41-60%... without changing training intensity or frequency" as an effective strategy across endurance events generally; one specific older study cited via secondary description only (a "1993 study" reporting an 85% volume reduction associated with a 3% 5km performance improvement) — this citation could not be independently verified to a specific author/journal/page in this phase and is recorded as **UNVERIFIED_SECONDARY_CLAIM**, not used as supporting evidence for any classified claim below.

## 5. Atomic claims, supporting evidence, classification

**Claim A — A short taper (roughly 7-14 days) is better supported for a 5K than a long one, with multiple independent secondary descriptions of peer-reviewed review-level literature converging on a shorter duration than typical marathon tapers (which commonly run 2-3 weeks).** The runnersconnect.net 5K-specific secondary source states 7-10 days; the broader endurance-taper meta-analysis-derived guidance states 8-14 days as "best" across endurance sports generally (not 5K-specific, but compatible in range); the ≤21-day framing is an upper bound across endurance events generally, not a 5K-specific recommendation. **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** for "FIVE_K's taper should be short relative to longer-distance tapers, on the order of roughly one to two weeks" — compatible direction and overlapping range across a 5K-specific secondary source and a general endurance-taper review-level secondary source; **not** `DIRECT_CANONICAL_EVIDENCE`, since the most specific 5K-only number (7-10 days, runnersconnect.net) is itself a secondary coaching-advice source, not a primary peer-reviewed study read directly in this phase.

**Claim B — Volume reduction in the 40-60% range is repeatedly cited, with the 5K-specific secondary source narrowing to 40-50% and the general endurance-taper literature-derived guidance citing 41-60%.** These two ranges overlap substantially (40-50% within 41-60%, essentially coincident). **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** for "taper volume reduction in approximately the 40-50% range (i.e., taper-week volume at roughly 50-60% of pre-taper volume, expressed as a multiplier)" — this is notably different in character from both TEN_K's single 0.53 multiplier (which, by coincidence of arithmetic, falls within this evidence-supported band — flagged explicitly as a coincidence per governing-prompt §28's "value equality ≠ authority equality" discipline, NOT used as justification) and HM's steeper two-stage 0.70/0.43 profile.

**Claim C — Intensity/frequency should be relatively preserved during taper even as volume drops, consistent with general taper literature ("progressively reduced [volume]... without changing training intensity or frequency") and with the training-text sources from LIT-04/LIT-05 describing a "Competition" phase that retains at least one mid-week quality/threshold touch while otherwise reducing to easy running (Daniels' Phase IV, interpreted via secondary description as in LIT-04).** **Classification: STRONG_MULTI_SOURCE_SYNTHESIS** for the qualitative principle (retain intensity/frequency, cut volume); **SOURCE_SILENCE** for the exact FIVE_K activation-workout content/structure.

**Claim D — No source supports an exact deterministic single multiplier (e.g., "0.XX") as scientifically unique for FIVE_K; the evidence supports a range (roughly 0.50-0.60 for a one-week taper, or a two-stage profile if a two-week taper is adopted, by analogy to the general 40-60% volume-reduction guidance applied across however many taper weeks are chosen).** No source publishes a week-by-week multiplier schedule specific to 5K the way `HalfMarathonIntermediate4DTaperVolumePolicy.OrderedMultipliers` does for HM. **Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED** — do not treat any single number here as evidence-unique; a deterministic value must be an explicit product decision within the evidence-supported band.

**Claim E — Taper duration itself (1 week vs. 2 weeks) is a product decision bounded by evidence, not dictated by it.** The evidence (Claim A) supports "short, roughly 1-2 weeks" without discriminating cleanly between exactly 1 and exactly 2 weeks for a 5K specifically — both are within the supported range, and the choice interacts with the CoreCycle/horizon product decision from LIT-04. **Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED.**

## 6. Contrary/limiting evidence

The UNVERIFIED_SECONDARY_CLAIM (85% reduction / 3% performance gain, "1993 study") was explicitly excluded from all classified claims above rather than silently incorporated, because its author/journal/exact population could not be verified in this phase — including it would risk exactly the fabricated-citation and over-interpolation failure mode this engagement's evidence discipline forbids. If a future phase can verify this citation properly (author, journal, year, population), it may be re-evaluated; until then it is excluded, not relied upon.

## 7. Population applicability

The elite-population systematic review (ResearchGate-hosted) is explicitly noted as elite-athlete evidence — its *duration* conclusions were used only insofar as they overlap with the general/5K-specific secondary sources' ranges (triangulation), not applied directly as if elite and recreational tapering needs are identical; no elite-specific numeric value was carried into any classified claim's recommended range.

## 8. Source-silence notes

Exact deterministic multiplier/week-count: silent (Claims D/E). Exact FIVE_K activation-workout content: silent (Claim C, qualitative principle only).

## 9. Canonical implication

A taper shorter than HM's two-week profile, with volume reduction in the general 40-60% band and preserved intensity/frequency, is a defensible evidence-informed envelope for a future FIVE_K taper policy. No exact multiplier or week-count can be asserted as directly evidenced.

## 10. Product-default implication

PD-shaped decisions required: (1) 1-week vs. 2-week FIVE_K taper; (2) exact multiplier(s) within the ~0.40-0.60 reduction band. See phase report PD-FIVEK items.

## 11. Final classification

`FIVE_K_TAPER_DURATION_BAND_CLOSED_MULTI_SOURCE_SYNTHESIS`; `FIVE_K_TAPER_VOLUME_REDUCTION_BAND_CLOSED_MULTI_SOURCE_SYNTHESIS`; `FIVE_K_TAPER_EXACT_MULTIPLIER_AND_WEEK_COUNT_PRODUCT_DEFAULT_REQUIRED`; `FIVE_K_TAPER_ACTIVATION_WORKOUT_CONTENT_SOURCE_SILENCE_REMAINS`.

## 12. Open gaps

Exact FIVE_K-specific activation-workout content (what the final pre-race quality touch should actually be) remains silent; no atomic follow-up is proposed since this is more naturally resolved alongside the LIT-04/LIT-05 workout-progression product decisions than as an isolated taper-literature search.
