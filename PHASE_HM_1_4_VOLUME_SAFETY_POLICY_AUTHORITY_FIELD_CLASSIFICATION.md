# PHASE HM.1.4 — HALF-MARATHON `VolumeSafetyPolicy` NUMERIC-AUTHORITY SURFACE: FIELD-BY-FIELD CLASSIFICATION

**Phase type**: DECISION / AUTHORITY-CLOSURE ONLY (no production code, no HM catalog artifacts, no public routing changes, no 10K changes, no new external research)
**Execution status**: DONE
**Final classification**: `HM_VOLUME_PROGRESSION_AUTHORITY_PARTIAL`
**Governance commit**: `dadea15` (implementation + ledger row, backfilled self-referentially per this engagement's established two-commit pattern).
**Governing prompt**: user-authored "HM PHASE PROMPT 1.4" — verbatim text defines scope; followed precisely.
**Parent**: `HM.2` (`PHASE_HM_2_CORE_VERTICAL_SLICE_PREREQUISITES_AND_IDENTITY_WIDENING.md`), which found `HM.0 §I` item 2 — a `VolumeSafetyPolicy`-shaped record for `HALF_MARATHON × INTERMEDIATE × 4D` — was never closed by any phase and correctly blocked Step 2 on it rather than improvising; `HM.1.1`/`HM.1.3` (precedent for how a field is classified `VALIDATED_CANDIDATE_EXISTS` vs. escalated as a genuine gap in this engagement's own established discipline).

---

## 0. Startup confirmation

- `git fetch origin` clean; `git rev-list --left-right --count origin/main...HEAD` = `0  0` before starting.
- Worktree changes limited to the pre-existing tracked `bin`/`obj` rebuild-artifact noise this engagement has repeatedly called out — left untouched, not staged.
- Required reading completed in full before writing anything: `PHASE_HM_2_CORE_VERTICAL_SLICE_PREREQUISITES_AND_IDENTITY_WIDENING.md`; `HM_21K_Claude_Handoff_and_Build_Plan.md` (re-read in full, specifically §4.2 peak-volume matrix, §4.3 starting-long-run policy, §4.5 pace hierarchy, `HM-LIT-20` taper/race-week material, §6 `OPEN-HM-*` items); the real `VolumeSafetyPolicy.cs` (`backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`) and its consumer, `CatalogVolumeAndLongRunPlanner.cs`, read directly — not inferred from `HM.2`'s narrative summary; `PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md` and `PHASE_HM_1_3_WORKOUT_PROGRESSION_EXPOSURE_COUNT_AUTHORITY_DECISION_CLOSURE.md` for classification-discipline precedent.
- `PHASE_LEDGER.md`'s HM Program table confirmed rows 1–7 (`HM.0`, `HM.0.1`, `HM.1`, `HM.1.1`, `HM.1.2`, `HM.1.3`, `HM.2`) exist; no `HM.1.4` row exists. **Chosen phase ID: `HM.1.4`** — a decimal sub-phase closing out `HM.2`'s own disclosed numeric-authority blocker, mirroring `HM.0.1`/`HM.1.1`/`HM.1.3`'s established discipline, and deliberately not colliding with `HM.2` (already the reserved/used name for the implementation phase this closure feeds back into).

---

## 1. Step 1 — the real authority surface, read directly from code

`VolumeSafetyPolicy` (`VolumeSafetyPolicy.cs:51-64`) is a C# record with **13 required positional parameters** (no defaults — every field must be supplied to construct any new instance):

| # | Field | Type | Load-bearing? (per `CatalogVolumeAndLongRunPlanner.cs`) |
|---|---|---|---|
| 1 | `PreferredMaxWeeklyIncreaseRatio` | `double` | Yes — `ResolvePeak`'s 3D-sequential branch (line 287); real growth-rate cap |
| 2 | `HardMaxWeeklyIncreaseRatio` | `double` | 3D-sequential branch only (line 289); for the non-3D (4D-style) branch used here, the class's own doc comment (lines 13-34) discloses this field is **informational/provenance-only** for that branch — never read back to clamp a transition, per the closed `TD-VOLUME-CAP-UNENFORCED-001` finding |
| 3 | `AbsoluteWeeklyIncrementCapKm` | `double` | Same as #2 — load-bearing for 3D-sequential, informational-only for the 4D-style branch used by this cell |
| 4 | `GoldenFixtureStartingVolumeKm` | `double` | Yes — `ResolvePeak` line 308, directly forms `canonicalDefaultMultiplier` |
| 5a | `ResolvedPeakReference.Value` | `double` | Yes — same line 308 |
| 5b | `ResolvedPeakReference.Provenance` | enum | No — explicit code comment line 306-307: "Provenance is audit metadata only... never branches on" it |
| 6 | `GoldenFixtureNonTaperTransitions` | `int` | Yes — `ResolvePeak` line 309, denominator of the transition-adjusted multiplier |
| 7 | `TaperVolumeMultiplier` | `double` | Yes — `ResolveTaperDecision` (line 340), and validated against a hardcoded `[0.41, 0.60]` acceptance band (lines 341-343) |
| 8 | `LongRunPreferredMinimumShare` | `double` | Yes — `ResolveLongRunWeeklyShareDecision` (line 356) |
| 9 | `LongRunPreferredMaximumShare` | `double` | Yes — line 357, also used at line 645 for compatibility classification |
| 10 | `LongRunSelectionShare` | `double` | Yes — line 358, the actual deterministic long-run-km selection driver |
| 11 | `LongRunHardCapShare` | `double` | Yes — line 359, also line 646 |
| 12 | `RoundingIncrementKm` | `double` | Yes — lines 291, 430, 623 (`Round` helper) |
| 13 | `RoundingRule` | `string` | Yes (decision-trace) — lines 480, 603 |

**Explicit scope note (per Step 1's own instruction not to over-scope)**: `PeakVolumeBand` (the `[36,50]`-shaped min/max range itself) is **not** a field of this record. Per `SixDayIntermediate`'s own doc comment (`VolumeSafetyPolicy.cs:141-146`), the band is "implemented as a catalog row, not a field here." This record only needs a single resolved **point** reference (`ResolvedPeakReference`), derived from a band by a separate authoring step. Authoring the actual catalog `PeakVolumeBand` row is out of scope for this record and for this phase — it is future catalog-authoring work per `HM.2 §6`'s own disclosed remainder.

This is the complete, verified authority surface a `HalfMarathon × Intermediate × 4D` instance would need to supply. No field beyond these 13 (14 counting the two `ResolvedPeakReference` sub-fields separately) is required by the actual contract.

---

## 2. Step 2 — classification of every field, source boundary = `HM_21K_Claude_Handoff_and_Build_Plan.md` only

Verified by direct re-read (not memory): the handoff document contains **no** percentage figures anywhere for weekly-volume growth rate, absolute-km weekly increment, or long-run weekly-volume share (confirmed by full-document grep for `%`/`share` — the only percentage figures in the whole document are the unrelated goal-pace-gap bands in `HM-LIT-26`, §4.5's `0–3%`/`3–6%`/etc.). This shapes several classifications below.

### 2.1 — `PreferredMaxWeeklyIncreaseRatio` → **C, `GENUINE_GAP`**

Mechanism (a preferred percentage-of-current-volume weekly growth cap) is `GENERIC_MECHANISM_REUSE` — handoff §3 explicitly lists "progression coefficients/caps" among items that are "structurally generic while the selected values remain HM-specific." No HM-specific coefficient exists anywhere in the handoff document. 10K's own `0.07d` is identical across **every one of the 17 named 10K instances** in `VolumeSafetyPolicy.cs` — `HM.0 §E` already flagged this as "a 10K-wide constant, not distance-generic." Copying it would violate the handoff document's own top-level prohibition (§0: "copy 10K numeric values into HM merely because the architecture is reusable"). **No value frozen.**

### 2.2 — `HardMaxWeeklyIncreaseRatio` → **C, `GENUINE_GAP`**

Same reasoning as 2.1. 10K's `0.08d` is likewise identical across all 17 instances. No HM-specific figure exists in the handoff document.

### 2.3 — `AbsoluteWeeklyIncrementCapKm` → **C, `GENUINE_GAP`**

Mechanism (a secondary absolute-km ceiling alongside the percentage cap — "single-session spike guard"-adjacent dual-cap design) is reusable in concept; 10K's own values vary by cell (`2.0` for 3D/Beginner2D/Intermediate2D/ThreeDayBeginner, `2.5` for everything else) — proving even 10K itself does not treat this as a universal constant, so there is even less basis to assume a single HM-wide value transfers. No HM-specific figure exists in the handoff document.

### 2.4 — `GoldenFixtureStartingVolumeKm` → **C, `GENUINE_GAP`**

This must not be confused with §4.3's **starting-long-run** cap (`12 km` for Intermediate) — that is a distinct quantity (the `LONG_RUN` role's own starting distance), explicitly warned against confusing with anything peak/volume-related by the handoff document itself ("These are *starting* caps, not peak-long-run caps," §4.3). No **starting weekly total volume** figure exists anywhere in the handoff document for Intermediate×4D. `GENUINE_GAP`.

### 2.5 — `ResolvedPeakReference` → **A + B (mixed), FREEZE**

- **Value → A, `VALIDATED_CANDIDATE_EXISTS`.** Handoff §4.2's peak weekly-volume matrix gives Intermediate×4D = **36–50 km/week** explicitly, by name, for this exact cell — this is precisely the kind of "specific enough value/range for Intermediate×4D specifically" Step 2.A requires. **Freeze `ResolvedPeakReference.Value = 43.0`** (the band midpoint), using the same band→point-reference conversion methodology `VolumeSafetyPolicy.cs` itself already applies uniformly for every non-golden-fixture-derived 10K cell (`Beginner2D`: band `[16,22]`→`19.0`; `Advanced4D`: band `[38,52]`→`45.0`; `Advanced5D`: band `[42,58]`→`50.0` — every single one is the exact arithmetic midpoint, confirmed by inspection). Using this already-established, distance-agnostic **mechanism** (not new external research, not a fresh product decision) to convert an already-validated HM band into a point value is `GENERIC_MECHANISM_REUSE` (B) layered on top of the validated band (A) — it is not inventing new numeric authority, only mechanically formalizing an already-approved range the same way every existing policy in this file already does.
- **Provenance → freeze `ProductDefaultWithEvidenceEnvelope`.** No real observed HM training-program trace exists to justify `GoldenFixtureDerived` (that classification is reserved, per its only real usage — `Default`'s own 10K golden fixture — for a value taken from an actually-observed reference program run, not a band midpoint).

**Important caveat, disclosed prominently, not buried**: freezing this field's *value* does not make it *usable*. `ResolvePeak`'s real formula (line 308) divides `ResolvedPeakReference.Value` by `GoldenFixtureStartingVolumeKm` (§2.4, `GENUINE_GAP`) to form the growth multiplier. A frozen numerator with an unfrozen denominator cannot produce a real, executable peak-resolution decision. This field closes cleanly **as a field**; the record as a whole remains blocked by §2.4/§2.6.

### 2.6 — `GoldenFixtureNonTaperTransitions` → **C, `GENUINE_GAP`**

This is not a generically-derivable "count the non-taper weeks in HM's own frozen 14-week allocation" arithmetic exercise. It is the third leg of a single, self-consistent **calibration episode** together with `GoldenFixtureStartingVolumeKm` and `ResolvedPeakReference` — 10K's own value (`10`, identical across every 10K instance in the file) is tied to `TEN_K_MASTER v6`'s own specific historical reference-program trace (the record's own class doc comment ties `HardMaxWeeklyIncreaseRatio`'s safety proof explicitly to "`TEN_K_MASTER v6`'s current constants," the same triad this field belongs to). No equivalent HM reference-program trace exists in the handoff document, so there is no principled way to assign this field a number without either (a) inventing an HM-specific trace that was never gathered, or (b) mechanically copying 10K's own historical constant — both barred. Coupled to §2.4's gap; cannot be resolved independently of it.

### 2.7 — `TaperVolumeMultiplier` → **C, `GENUINE_GAP`** — **and a source-boundary discrepancy that must be disclosed**

Verified directly, not assumed: the handoff document (`HM-LIT-20`, lines 440-447, and §4.1) gives **only a taper-duration preference** — "preferred HM taper = 2 weeks, minimum HM taper = 1 week" — and contains **zero** volume-reduction-percentage evidence anywhere. A full-document grep for `41`/`60%`/`reduction`/`taper` (case-insensitive) found no percentage figure of any kind for taper volume reduction. **This corrects this phase's own governing prompt**, whose Step 4 framed "~41–60% volume reduction" as if it were handoff-document evidence: it is not. That figure exists only as `CatalogVolumeAndLongRunPlanner.ResolveTaperDecision`'s hardcoded **10K-specific structural acceptance band** (`VolumeSafetyPolicy.cs`/`CatalogVolumeAndLongRunPlanner.cs:341-343`, `reduction < 0.41d || reduction > 0.60d` throws), documented in-code as sourced from "Golden Fixture v3 week 12 reduces from 38km to 20km (0.526 remaining)" — a 10K-specific observed data point, not HM research. This is the same class of finding `HM.1.2` itself disclosed prominently (the `HM-LIT-06..11` vs. `15..27` numbering mismatch) rather than silently complying with an incorrect premise.

**Consequence**: Step 4's own duration question is answered — the duration (2 weeks preferred) is **already frozen**, via `HM.1`'s own `3/4/5/2` phase allocation (Taper = 2 weeks), matching the handoff document's preference exactly; no new decision is needed for duration. But the **magnitude** (the actual `TaperVolumeMultiplier` value / per-week reduction split Step 4 asked about) has **zero** supporting evidence in the permitted source material at all — not a range to read a value off of, not a per-week split to verify. `GENUINE_GAP`, not a B-classification "single reasonable split read off a range," because no range exists to read from.

The general **mechanism** (apply a fractional multiplier to reduce weekly volume during taper weeks) is trivially `GENERIC_MECHANISM_REUSE` — every training methodology tapers volume — but the exact coefficient is absent.

### 2.8–2.11 — `LongRunPreferredMinimumShare` / `LongRunPreferredMaximumShare` / `LongRunSelectionShare` / `LongRunHardCapShare` → all **C, `GENUINE_GAP`**

These are **weekly-volume-share percentages** (what fraction of total weekly km the long run consumes) — categorically distinct from both the already-frozen `19.0 km` absolute peak-long-run **ceiling** (`HM.1.1`, a fixed distance, not a share) and the `12 km` starting-long-run **absolute cap** (handoff §4.3). Confirmed by full-document grep: no percentage-of-weekly-volume figure for long-run share exists anywhere in the handoff document. `HM.2`'s own report explicitly names "long-run share bounds (distinct from the already-frozen 19.0km absolute ceiling)" as still-unfrozen — this phase's independent verification confirms that finding was correct. 10K's own values vary meaningfully by frequency (`0.30–0.36` at 4D, `0.38–0.42` at 3D, `0.28–0.36` at 5D/6D, `0.55–0.60` at 2D) — proving this is frequency-sensitive product data, not a transferable constant, and there is no HM-specific analog for any of the four figures.

### 2.12 — `RoundingIncrementKm` → **B, `GENERIC_MECHANISM_REUSE`, FREEZE**

This is a **catalog display/precision convention** — the increment plan values round to for presentation and internal consistency — not a training-science-calibrated safety constant. It is identical (`0.5`) across **every single one** of the 17 named 10K instances regardless of level, frequency, or distance-implied training load, strongly indicating it is a distance-agnostic UX/catalog-precision decision, not something Half-Marathon-specific training evidence could ever inform differently. **Freeze `RoundingIncrementKm = 0.5`.**

### 2.13 — `RoundingRule` → **B, `GENERIC_MECHANISM_REUSE`, FREEZE**

Same reasoning as 2.12. The non-3D-sequential-growth family (which the frozen `4D` cell belongs to — see `IsThreeDaySequentialGrowthPolicy`, `CatalogVolumeAndLongRunPlanner.cs:276-277`, which does not admit any 4D policy) uses the identical string `"round_nearest_0.5km_after_each_week_value_then_validate"` across `Default`, `FiveDayIntermediate`, `SixDayIntermediate`, `Advanced3D`–`Advanced6D`, `Beginner2D`, `Intermediate2D`, and `BeginnerFourDay` — i.e., every 4D-style (non-3D-sequential) policy in the file, without exception. **Freeze `RoundingRule = "round_nearest_0.5km_after_each_week_value_then_validate"`** (the exact string this cell's own algorithmic family already uses everywhere else).

---

## 3. Step 3 — progression/increase-rate coefficients: confirmed `GENUINE_GAP`, as the handoff document itself warned was likely

Per the governing prompt's own explicit caution (echoing the handoff document's principle in §3 that "mechanism reusable != exact coefficients reusable"), fields 2.1–2.3 (`PreferredMaxWeeklyIncreaseRatio`, `HardMaxWeeklyIncreaseRatio`, `AbsoluteWeeklyIncrementCapKm`) were checked specifically, not assumed transferable from 10K's `0.07`/`0.08`/`2.0–2.5km`. **Confirmed genuine gaps, not a formality**: even within 10K's own history, these three values have never been shown to be frequency- or Level-invariant by causal evidence — they are simply reused verbatim across 10K cells by product convention (e.g. `ThreeDayBeginner`'s own doc comment explicitly states "no Level effect established or introduced here," an admission of convention, not proof). Nothing in the handoff document supplies any HM-specific version of these three coefficients, at any frequency or level. This is exactly the outcome the prompt's Step 3 predicted as "likely, not unlikely."

---

## 4. Step 4 — taper multiplier: verified, not assumed; the honest answer is `GENUINE_GAP`, and the prompt's own premise needed correcting

Per §2.7 above: the handoff document supplies a **duration** preference (2 weeks, already frozen via `HM.1`) but supplies **no** magnitude evidence whatsoever — not a range, not a per-week split, not a single figure. The `~41–60%` figure named in this phase's own governing prompt does not exist in `HM_21K_Claude_Handoff_and_Build_Plan.md`; it is 10K's own code-embedded structural validation band, sourced from `TEN_K_MASTER v6`'s own golden fixture, not HM research. Since no range exists in the permitted source at all, Step 4's own two options ("a single reasonable split read off the range" vs. "a genuine choice the source doesn't make") do not apply as written — there is no range to read a split off of. Classified `GENUINE_GAP`, honestly, not forced into either of the two pre-supposed shapes.

---

## 5. Frozen `HALF_MARATHON × INTERMEDIATE × 4D` `VolumeSafetyPolicy`-shaped authority — every field classified A or B

```
Field                          | Value                                       | Classification | Source
--------------------------------|----------------------------------------------|-----------------|----------------------------------
ResolvedPeakReference.Value     | 43.0 (km/week)                               | A (+ B midpoint | HM_21K_Claude_Handoff_and_Build_
                                 |                                               | mechanism)      | Plan.md §4.2, Intermediate×4D
                                 |                                               |                 | band [36,50]; midpoint per
                                 |                                               |                 | VolumeSafetyPolicy.cs's own
                                 |                                               |                 | established band->point convention
ResolvedPeakReference.Provenance| ProductDefaultWithEvidenceEnvelope           | B (mechanism)   | No observed HM reference trace
                                 |                                               |                 | exists; matches every non-golden-
                                 |                                               |                 | fixture 10K precedent
RoundingIncrementKm             | 0.5 (km)                                     | B (mechanism)   | Distance-agnostic catalog/UX
                                 |                                               |                 | precision convention; identical
                                 |                                               |                 | across all 17 10K instances
RoundingRule                    | "round_nearest_0.5km_after_each_week_value_  | B (mechanism)   | Identical string used by every
                                 |  then_validate"                              |                 | 4D-style (non-3D-sequential) 10K
                                 |                                               |                 | policy without exception
```

**All other 10 required fields/sub-fields remain `GENUINE_GAP` (classification C)** — see §6.

---

## 6. Genuine gaps — precisely disclosed, not invented around

| Field | Classification | Adjacent-evidence option (explicitly NOT selected) | Honest next step |
|---|---|---|---|
| `PreferredMaxWeeklyIncreaseRatio` | C | 10K's own `0.07` — but identical across all 17 10K cells (not level/frequency-differentiated even within 10K), and copying it is explicitly barred by the handoff document's own §0 prohibition | Short real-world-program cross-check (mirroring `HM.1.1`'s method): compare weekly-mileage progression rates across 2–3 published Intermediate 4D Half-Marathon plans |
| `HardMaxWeeklyIncreaseRatio` | C | 10K's own `0.08`, same caveat as above | Same cross-check, same source set |
| `AbsoluteWeeklyIncrementCapKm` | C | 10K's own `2.5` (4D-family value) — same caveat | Same cross-check |
| `GoldenFixtureStartingVolumeKm` | C | 10K's own start/peak ratios cluster ~0.53–0.63, but applying any of them imports 10K's own historical calibration into a new distance, explicitly barred | Needs one real reference-program data point: a stated/typical Intermediate×4D HM starting weekly volume |
| `GoldenFixtureNonTaperTransitions` | C | None constructible without either an HM reference trace or copying 10K's `10` | Falls out mechanically once a real HM reference program (same one used for `GoldenFixtureStartingVolumeKm`) supplies both its own starting volume and its own weeks-to-peak count |
| `TaperVolumeMultiplier` | C | 10K's own `0.53` sits inside 10K's own generic `[0.41,0.60]` structural acceptance band, but that band and value are both 10K-sourced, not HM evidence | Short cross-check of taper-week volume reduction in 2–3 published Intermediate 4D HM plans |
| `LongRunPreferredMinimumShare` | C | None — no HM or transferable-10K single value fits without picking an arbitrary frequency analog | Needs real HM Intermediate×4D program data on long-run share of weekly volume |
| `LongRunPreferredMaximumShare` | C | Same | Same |
| `LongRunSelectionShare` | C | Same | Same |
| `LongRunHardCapShare` | C | Same | Same |

**Honest sizing of the remaining work, stated plainly per this phase's own instruction**: 10 of 13 required fields remain open, and they are not independent trivia — they are the substantive core of the safety policy (how fast volume may grow, where it starts, how much it recedes for taper, and what share of it the long run may claim). Four of these ten (the long-run share cluster) are homogeneous and could plausibly be closed together by one short real-world-program cross-check, similar in shape and effort to `HM.1.1`'s peak-long-run resolution. The growth-rate/starting-volume/taper cluster (six fields) likely needs the same kind of short, targeted cross-check across 2–3 real published Intermediate×4D Half-Marathon programs — not a full literature re-opening, but genuinely more than a single unsupported decision message, since no HM-specific figure of any kind exists yet for any of them in the permitted source material. This phase does **not** attempt that evidence-gathering itself (out of scope, and would require external research this phase is explicitly barred from performing) — it precisely scopes what such a follow-up pass would need to close.

---

## 7. What this phase explicitly did NOT do

- Did not invent any numeric value for any `GENUINE_GAP` field — no weekly-increase ratio, absolute-km cap, starting volume, transition count, taper multiplier, or long-run share was assigned a number.
- Did not perform new external research or web search — all classifications rest solely on `HM_21K_Claude_Handoff_and_Build_Plan.md`'s existing text, re-read directly, plus the real `VolumeSafetyPolicy.cs`/`CatalogVolumeAndLongRunPlanner.cs` source.
- Did not silently comply with the governing prompt's own mistaken premise about a "~41–60% volume reduction" figure existing in the handoff document — verified directly and disclosed the discrepancy (§2.7/§4), consistent with `HM.1.2`'s own precedent of disclosing source-boundary discrepancies rather than working around them.
- Did not author the `PeakVolumeBand` catalog row, any `WORKOUT_PROGRESSION`/`PLAN_TEMPLATE`/`TEMPLATE_COMBINATION` catalog artifact, or any production code.
- Did not resume `HM.2`'s implementation work.
- Did not touch 10K's authority, code, or catalog in any way.
- Did not force `HM_VOLUME_PROGRESSION_AUTHORITY_CLOSED` — 10 of 13 fields remain genuinely open; the honest classification is `PARTIAL`.

---

## 8. Final classification

```
HM_VOLUME_PROGRESSION_AUTHORITY_PARTIAL
```

Of the 13 required `VolumeSafetyPolicy` fields for `HALF_MARATHON × INTERMEDIATE × 4D` (verified directly against the real record and its real consumer, not assumed from any prior phase's narrative), **3 close cleanly** (`ResolvedPeakReference` — value `43.0` from the handoff's own validated `[36,50]` band via the repository's own established midpoint mechanism, provenance `ProductDefaultWithEvidenceEnvelope`; `RoundingIncrementKm = 0.5`; `RoundingRule = "round_nearest_0.5km_after_each_week_value_then_validate"` — both rounding fields via `GENERIC_MECHANISM_REUSE`, being distance-agnostic catalog-precision conventions, not training-science authority). **10 fields remain `GENUINE_GAP`**: the three progression/increase-rate coefficients (confirmed absent, as the handoff document itself warned was likely, per Step 3); the starting-volume/golden-fixture calibration pair (`GoldenFixtureStartingVolumeKm`, `GoldenFixtureNonTaperTransitions`); the taper-volume multiplier (with an explicit correction of this phase's own governing prompt's mistaken premise that a `~41–60%` figure exists in the handoff document — it does not; only a taper-duration preference exists, and that is already frozen via `HM.1`); and all four long-run weekly-volume-share fields (categorically distinct from the already-frozen `19.0 km` absolute ceiling and the `12 km` starting-long-run cap, and unsupported by any percentage evidence anywhere in the handoff document). This is not a small, forceable set of `DOMAIN_DECISION_REQUIRED` line items — it is the substantive core of the safety policy, honestly sized in §6 as needing a short, targeted real-world-program cross-check (mirroring `HM.1.1`/`HM.1.3`'s own resolution method), not a full new research phase, but also not a single decision message. No production code, catalog artifact, or public routing change in this phase. No 10K authority, code, or catalog touched.
