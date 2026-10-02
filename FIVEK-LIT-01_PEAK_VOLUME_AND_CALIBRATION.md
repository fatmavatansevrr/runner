# FIVEK-LIT-01 — Peak-Volume, Weekly-Volume, and Calibration Authority for Canonical FIVE_K

**Phase:** FIVE-K.0A. **Maps to FIVE-K.0 blocker:** FK0-B1 (§47). **Research order:** 3rd (after structure/objectives and intensity, per governing-prompt §6).

## 1. Research question

What weekly/peak-volume authority exists for FIVE_K by Level × RunsPerWeek, what starting/calibration-volume authority applies, and do FIVE_K-specific growth-rate rules differ from the existing generic growth-cap mechanism?

## 2. Runtime authority being sought

A `ResolvedPeakReference` (value + provenance) per candidate FIVE_K Level×Frequency cell; `PreferredMaxWeeklyIncreaseRatio`/`HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` values (or confirmation the generic mechanism/values apply unchanged); calibration/starting-volume authority (or confirmation generic current-volume adaptation suffices).

## 3. Existing internal evidence already reviewed

FIVE-K.0 §21-24/§32: weekly-volume and peak-volume authority were `SOURCE_SILENCE` for every candidate cell, and peak-volume specifically was additionally `BLOCKED` because no governed generic derivation rule exists that could produce a value without forbidden distance-scaling. The growth-rate-cap *mechanism* (the named record fields) is `CLOSED_SHARED_AUTHORITY`; FIVE_K-specific *values* were `SOURCE_SILENCE`. TEN_K's own values (0.07 preferred / 0.08 hard weekly-increase ratio, `ResolvedPeakReference` figures such as 38-51km depending on cell) are confirmed real but explicitly not reusable for FIVE_K without independent evidence (`VolumeSafetyPolicy.cs`, direct read, this and prior phases).

## 4. External sources

Hal Higdon 5K Training (halhigdon.com, direct): Novice — 3 runs/week, longest workout 3 miles (total weekly volume low, walk-run based, not a structured mileage total published). Intermediate — 5 runs/week, longest workout 7 miles. Advanced — 6 runs/week, 1 day off, longest workout 12 miles, long run builds to 90 minutes. Pfitzinger, *Faster Road Racing* (interpreted via secondary description, Goodreads/LetsRun, not page-verified): 12-week programs at beginner/intermediate/advanced; one cited example 12-week schedule spans roughly 32-55 miles/week; a reviewer's base of 15-20 mi/week was explicitly described as below what the book's own "back half" (higher-level) programs assume as a starting point. Vorlich/StrengthRunning-style secondary weekly-mileage guidance (runculture/strengthrunning/vorlich — lower-tier secondary sources, used only as directional corroboration, not as primary authority): general guidance that 5K-focused recreational training commonly spans roughly 15-40 miles/week (24-64 km/week) depending on level, with "advanced" 5K-specific training sometimes exceeding that range — given as a broad descriptive range in blog-style secondary sources, not a controlled study.

## 5. Atomic claims, supporting evidence, classification

**Claim A — Weekly volume for 5K preparation varies enormously by level, and a single reputable source (Higdon) spans the full range from low-volume walk/run (Novice) through a 12-mile long-run/6-day Advanced plan, without publishing an explicit total weekly-mileage figure at any level.** Higdon's plans are specified in session count/longest-run terms, not as a stated weekly-km total. Pfitzinger's cited example (32-55 mi/week ≈ 51-89 km/week) is explicitly a 12-week *program range across levels*, not a single peak figure, and was not independently page-verified (secondary-source interpreted). **Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED** for any exact `ResolvedPeakReference` km figure — the evidence constrains a broad envelope, not a precise number, and the two primary sources used (Higdon, Pfitzinger) do not even share the same units of specification (session/longest-run vs. total-mileage-range), making direct cross-source numeric synthesis unsafe.

**Claim B — Level differentiation (Beginner/Intermediate/Advanced volume being meaningfully different, not a flat line) is well-supported directionally.** Higdon's three levels differ in frequency (3→5→6 runs/week) and longest run (3→7→12 miles) — a clear, direct, same-source progression. Pfitzinger's beginner/intermediate/advanced split is structurally the same pattern. **Classification: DIRECT_CANONICAL_EVIDENCE** for the *qualitative* claim that FIVE_K volume authority should differentiate materially by level (not a flat default); **SOURCE_SILENCE** for the exact km deltas between levels.

**Claim C — RunsPerWeek interacts with volume/level the same directional way it does for TEN_K/HM (more days generally correlates with higher total volume and more quality-session capacity), with no FIVE_K-specific contradiction found.** Higdon's own level definitions conflate level and frequency together (Novice=3d, Intermediate=5d, Advanced=6d) rather than offering an independent Level×Frequency matrix (e.g., no published "Beginner/5D" or "Advanced/3D" cell was found in any source reviewed). **Classification: SOURCE_SILENCE** for any specific Level×Frequency *matrix cell* beyond the three level-frequency pairings the sources happen to publish together; this is an important finding — the sources do not independently vary level and frequency the way Appsel's own matrix structure requires, so no source directly supports, e.g., "Beginner at 4D" or "Advanced at 3D" as distinct from "Beginner, whichever frequency Higdon happened to pick."

**Claim D — A defensible, narrower V1 claim is supportable: Intermediate-level FIVE_K preparation at a moderate frequency (4-5 days/week) sits within a broad band loosely consistent with Pfitzinger's own cited intermediate-tier figures and with Higdon's Intermediate plan's own implied volume (5 runs/week, long run up to 7 miles ≈ 11.3km, i.e., total weekly volume plausibly in the mid-30s-to-mid-40s km range if the long run is roughly 25-30% of total per the long-run-share evidence in LIT-02).** This combines Claim A/B with LIT-02's long-run-share finding to back into an approximate band, rather than taking any single source's absolute number as authoritative. **Classification: EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED** (a band is evidence-informed; a single deterministic value within it is a product choice) — this is the only candidate cell (Intermediate × 4D, loosely 5D) for which this phase can assemble even an approximate, cross-checked band; all other level/frequency combinations remain `INSUFFICIENT_EVIDENCE` for any numeric peak-volume claim.

**Claim E — No FIVE_K-specific starting/calibration-volume authority was found distinct from generic current-volume adaptation.** No source publishes a "starting volume" concept distinct from "week 1 of the published plan," and none of the sources address runners entering mid-fitness with an existing, measured weekly volume the way Appsel's calibration input does. **Classification: SOURCE_SILENCE** — but per governing-prompt §34, this silence is explicitly read as *not requiring* a FIVE_K-owned calibration rule: the existing generic current-volume-adaptation mechanism (used identically by TEN_K/HM) has no FIVE_K-specific evidence contradicting its applicability, so it is the reasonable default consumer, recorded as `CLOSED_SHARED_AUTHORITY_CONFIRMED_BY_ABSENCE_OF_CONTRARY_EVIDENCE` rather than invented FIVE_K-owned authority.

**Claim F — No evidence supports a FIVE_K-specific deviation from the existing generic growth-rate-cap mechanism's *shape*, and no source publishes an explicit weekly percentage growth cap for 5K training distinct from the widely-cited general distance-running guidance (5-10% weekly increase, already not FIVE_K-specific) found during LIT-02's search.** No source states growth caps should be higher or lower for 5K training than other distances. **Classification: CLOSED_SHARED_AUTHORITY_CONFIRMED** for the mechanism continuing to apply with no FIVE_K-specific override; the *generic* (not FIVE_K-owned) 5-10%/week figure found is itself a widely-repeated general-training guideline across non-distance-specific secondary sources, not FIVE_K-specific evidence, and is NOT proposed here as a new FIVE_K value — it merely confirms no contradiction exists.

## 6. Contrary/limiting evidence

Higdon and Pfitzinger specify volume in incompatible units/structures (session-count-and-longest-run vs. total-weekly-mileage-range), which is a scope/definition difference, not a true numeric contradiction — no source was found stating a different NUMBER for the same defined quantity (e.g., two sources both stating "Intermediate peak weekly volume" with different conflicting km figures). No `CONFLICTING_EVIDENCE` row results from this file.

## 7. Population applicability

Higdon: broad recreational population across all three levels (explicitly includes a "Walkers" variant below Novice, underscoring how low-end its Novice tier is calibrated). Pfitzinger: assumes "reasonable base fitness," explicitly above a 15-20 mi/week starting point for its higher-level programs — population skews more toward already-training recreational/competitive racers than Higdon's Novice tier. This difference in assumed starting fitness is a real population-applicability limit, not merely incidental, and is why Claim D is scoped to "Intermediate," the level where Higdon and Pfitzinger's implied populations most plausibly overlap.

## 8. Source-silence notes

No independent Level×Frequency matrix exists in any source (Claim C). No FIVE_K starting-volume/calibration rule exists (Claim E, addressed by shared-mechanism fallback). No source publishes an exact peak-volume number for Beginner or Advanced at any specific frequency with enough precision to defend a `ResolvedPeakReference` value (only Intermediate has even an approximate cross-checked band, Claim D).

## 9. Canonical implication

Level differentiation itself (not a flat default) is directly evidenced and can be encoded as a structural rule. The *existing* generic growth-cap mechanism and generic current-volume-adaptation mechanism are confirmed applicable to FIVE_K with no override required. No FIVE_K-owned `ResolvedPeakReference` numeric value can be asserted as `DIRECT_CANONICAL_EVIDENCE` or `STRONG_MULTI_SOURCE_SYNTHESIS` for any cell; at best, Intermediate×4D/5D supports an `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` band.

## 10. Product-default implication

Every candidate FIVE_K peak-volume cell requires an explicit product-default decision within an evidence-bounded band (narrowest and most defensible: Intermediate 4D/5D); Beginner and Advanced cells, and any cell outside 4D/5D, lack even that band and should be classified `INSUFFICIENT_EVIDENCE` rather than forced into a product default in this phase. See phase report §25-26/PD-FIVEK items.

## 11. Final classification

`FIVE_K_VOLUME_AUTHORITY_LEVEL_DIFFERENTIATION_CLOSED_DIRECT_EVIDENCE`; `FIVE_K_VOLUME_AUTHORITY_NUMERIC_PEAK_VALUES_PRODUCT_DEFAULTS_REQUIRED_FOR_INTERMEDIATE_ONLY`; `FIVE_K_VOLUME_AUTHORITY_BEGINNER_ADVANCED_INSUFFICIENT_EVIDENCE`; `FIVE_K_GROWTH_CAP_MECHANISM_CLOSED_SHARED_AUTHORITY_CONFIRMED`; `FIVE_K_CALIBRATION_VOLUME_CLOSED_SHARED_AUTHORITY_CONFIRMED_BY_ABSENCE_OF_CONTRARY_EVIDENCE`.

## 12. Open gaps

Atomic follow-up candidate (not opened in this phase): **FIVEK-LIT-06 — Beginner and Advanced FIVE_K peak-volume authority by frequency**, should a future phase need those cells; this file does not attempt to force that evidence now.
