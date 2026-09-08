# PHASE HM.1.5 — HALF-MARATHON `VolumeSafetyPolicy` GROWTH/CALIBRATION FREEZE, LONG-RUN-SHARE SEMANTICS GAP DISCLOSED

**Phase type**: DECISION / AUTHORITY-CLOSURE, WITH a mandatory real-code pre-freeze verification step (no production code changes, no HM catalog artifacts, no public routing changes, no 10K changes; verification reads code but does not modify it)
**Execution status**: DONE
**Final classification**: `HM_VOLUME_PROGRESSION_GROWTH_AND_CALIBRATION_FROZEN_LONG_RUN_SHARE_BLOCKED_ON_SEMANTICS_GAP`
**Governance commit**: `b5c3918` (implementation + ledger row, backfilled self-referentially per this engagement's established two-commit pattern).
**Governing prompt**: direct user decision message "DECISION ON HM.1.4 — FREEZE 9 OF 10 FIELDS" — exact text is the spec, followed precisely, including its own instruction to withhold the freeze if pre-freeze verification fails.
**Parent**: `HM.1.4` (`PHASE_HM_1_4_VOLUME_SAFETY_POLICY_AUTHORITY_FIELD_CLASSIFICATION.md`), which closed 3 of 13 `VolumeSafetyPolicy` fields and left 10 as `GENUINE_GAP` (the three progression/increase-rate coefficients, the golden-fixture calibration pair, `TaperVolumeMultiplier`, and the four long-run-share fields).

---

## 0. Startup confirmation

- `git fetch origin` clean; `git rev-list --left-right --count origin/main...HEAD` = `0  0` before starting.
- Working tree changes limited to the pre-existing tracked `bin`/`obj`/`baseline_tmp`/`ten-k-pilot-domain-decision-audit.*` rebuild-artifact noise this engagement has repeatedly called out — left untouched, not staged.
- Required reading completed in full before writing anything: `PHASE_HM_1_4_VOLUME_SAFETY_POLICY_AUTHORITY_FIELD_CLASSIFICATION.md` (the immediate predecessor and exact 13-field enumeration); the real `VolumeSafetyPolicy.cs` and its consumer `CatalogVolumeAndLongRunPlanner.cs`, read directly (both in full); `HM_21K_Claude_Handoff_and_Build_Plan.md`'s already-frozen `[36,50]`/`43.0` peak-volume authority re-confirmed from `HM.1.4`'s own record, not re-derived from new research.
- `PHASE_LEDGER.md`'s HM Program table confirmed 8 existing rows (`HM.0`, `HM.0.1`, `HM.1`, `HM.1.1`, `HM.1.2`, `HM.1.3`, `HM.2`, `HM.1.4`); no `HM.1.5` row exists. **Chosen phase ID: `HM.1.5`** — the next free decimal sub-phase in this engagement's established `HM.0.1`/`HM.1.1`/`HM.1.2`/`HM.1.3`/`HM.1.4` numbering discipline, deliberately not colliding with `HM.1.4A`, the name the governing decision itself reserves for a future companion phase closing `TaperVolumeMultiplier`.

---

## 1. Required pre-freeze verification

### 1.1 — Is the "golden fixture" mechanism real, and does it work as assumed? **VERIFIED — real, matches the cited precedent.**

`VolumeSafetyPolicy.Default` (`VolumeSafetyPolicy.cs:76-89`) is the cited 10K precedent, confirmed by direct read:

```
GoldenFixtureStartingVolumeKm: 24d
ResolvedPeakReference: new(38d, ResolvedPeakReferenceProvenance.GoldenFixtureDerived)
GoldenFixtureNonTaperTransitions: 10
```

This is consumed in `CatalogVolumeAndLongRunPlanner.ResolvePeak` (`CatalogVolumeAndLongRunPlanner.cs:279-336`), specifically the non-3D-sequential branch used by every 4D-style policy (this cell's own family, confirmed by `IsThreeDaySequentialGrowthPolicy`, lines 276-277, which admits only `ThreeDayIntermediate`/`ThreeDayBeginner` — never a 4D policy):

```csharp
var transitions = Math.Max(0, nonTaperWeeks - 1);                                          // line 305
var canonicalDefaultMultiplier = _policy.ResolvedPeakReference.Value / _policy.GoldenFixtureStartingVolumeKm;   // line 308
var transitionAdjustedMultiplier = 1d + ((canonicalDefaultMultiplier - 1d) * transitions / _policy.GoldenFixtureNonTaperTransitions);  // line 309
var reachable = startingVolumeKm * transitionAdjustedMultiplier;                            // line 310
```

`nonTaperWeeks` (line 281) is counted directly from the real bound plan (`boundPlan.Weeks.Count(w => w.PhaseKey != "TAPER")`) — this is the actual candidate's own week structure, not a stored constant. `GoldenFixtureStartingVolumeKm`/`ResolvedPeakReference`/`GoldenFixtureNonTaperTransitions` are the fixed calibration triad from the *reference* trace, used only to derive the multiplier's shape (how much of the total growth ratio each transition contributes), then applied to the *current* candidate's own `startingVolumeKm` and its own `transitions` count. This is exactly the mechanism the decision describes — verified real, not assumed.

### 1.2 — Transition-count arithmetic. **VERIFIED — consistent with the code's own definition.**

The code's sole definition of "transition" is `nonTaperWeeks - 1` (line 305), applied uniformly regardless of policy or candidate. For HM's own already-frozen `3 Foundation + 4 Build + 5 RaceSpecific = 12` non-taper weeks (`HM.1`), this mechanically evaluates to `12 - 1 = 11` transitions — exactly the decision's assumption, and not a new interpretation of "transition," just the code's own formula applied to HM's already-frozen structure.

Checked against the existing 10K instances (`VolumeSafetyPolicy.cs:76-412`): every one of the 17 named instances (`Default`, `ThreeDayIntermediate`, `FiveDayIntermediate`, `SixDayIntermediate`, `Advanced3D`–`Advanced6D`, `Beginner2D`, `Intermediate2D`, `ThreeDayBeginner`, `BeginnerFourDay`) uses the identical fixed constant `GoldenFixtureNonTaperTransitions: 10`, regardless of that instance's own actual candidate week-count — confirming this field is a **calibration-reference constant** (tied to `TEN_K_MASTER v6`'s own historical golden-fixture trace, per the record's own class doc comment, lines 20-24) and not literally recomputed per-candidate. HM has no equivalent external historical trace to borrow from (per `HM.1.4 §2.6`'s own finding), so treating HM's own already-frozen `3/4/5/2` structure as its own calibration reference (rather than importing an unrelated external trace) is the correct application of `DIRECT_STRUCTURAL_DERIVATION`, not a mismatch with how 10K's own instances work — it fills the same slot in the same formula, self-consistently, for a cell that (unlike 10K) actually runs its own reference-caliber structure as its normal candidate structure. **Confirmed.**

### 1.3 — CRITICAL: does the long-run-share mechanism allow a late peak week to rise above `SelectedLongRunShare` (0.33) toward `HardLongRunShareCap` (0.40)? **VERIFIED — NO. The mechanism treats the selection share as an immutable every-week ratio. This is a real progression-semantics gap, not a hypothetical concern.**

Traced `CatalogVolumeAndLongRunPlanner.BuildLongRunPlan` (`CatalogVolumeAndLongRunPlanner.cs:503-619`) week-by-week, in full:

```csharp
var lower = Round(weeklyVolume * preferredMinShare);      // line 530
var upper = Round(weeklyVolume * preferredMaxShare);       // line 531
var hardCap = Round(weeklyVolume * hardCapShare);           // line 532
var target = Round(weeklyVolume * selectionShare);          // line 533  <- computed identically every week
var unclamped = target;                                     // line 534
...
if (week.WeekNumber == weekly.Weeks[0].WeekNumber && ...)    // line 539 — WEEK 1 ONLY
{
    unclamped = Math.Min(readiness.LongestRun.Kilometers.Value, target);   // line 544 — only ever LOWERS below target
}
if (lowConfidence) { unclamped = target; ... }               // line 549-555 — resets to target, never raises above it
var selected = Round(Clamp(unclamped, lower, Math.Min(upper, hardCap)));  // line 557
```

There is no week-number, phase-key, or "peak-long-run-week" branch anywhere in this method that raises the target share toward `hardCapShare` for a late RaceSpecific week. For every week except week 1 (and the low-confidence clamp, which also only ever resets to `target`, never above it), `unclamped == target == weeklyVolume(that week) × SelectedLongRunShare`. Since `PreferredLongRunShareMin ≤ SelectedLongRunShare ≤ PreferredLongRunShareMax` by construction for every existing policy in the file (including the proposed HM values, `0.30 ≤ 0.33 ≤ 0.36`), `target` is *always* already inside `[lower, upper]`, so the `Clamp` at line 557 never binds `upper` or `hardCap` in ordinary operation — `selected` reduces algebraically to `Round(weeklyVolume(week) × SelectedLongRunShare)` for every week, including the peak/RaceSpecific week. `HardLongRunShareCap` functions only as a passive safety assertion (`CatalogLongRunHardCapViolationException` at line 563-566, structurally unreachable given the clamp already caps at `Math.Min(upper, hardCap)`), never as a target the mechanism deliberately approaches at a peak week. This mirrors the same "informational, never load-bearing for the enforcement it names" pattern the class's own doc comment already discloses for `HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` in the 4D-style branch (`VolumeSafetyPolicy.cs:13-34`, `TD-VOLUME-CAP-UNENFORCED-001`) — the same structural shape recurring in a second field family.

**Concrete numeric confirmation of the contradiction the decision anticipated**: for the frozen `HALF_MARATHON × INTERMEDIATE × 4D` peak-volume band `[36,50]` (`HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2, re-confirmed from `HM.1.4`'s own record) and the already-frozen `ResolvedPeakReference = 43.0`:
- At the band midpoint peak (43.0 km/week): `43.0 × 0.33 = 14.19 → rounds to 14.0 km`.
- At the band's own upper bound (50.0 km/week, the `UpperBoundConstrained` case): `50.0 × 0.33 = 16.5 km`.
- Even at the passive `HardLongRunShareCap` ceiling (never actually reached by the mechanism, per above): `50.0 × 0.40 = 20.0 km`.

None of the values the mechanism can *actually produce* (14.0–16.5 km) reach `HM.1.1`'s already-frozen `PreferredAbsolutePeakLongRunKm = 19.0 km` ceiling — a user reaching the full frozen peak-volume band could never reach `HM.1.1`'s own frozen authority under the current mechanism. This is exactly the contradiction the governing decision flagged as the failure condition, confirmed by direct code trace, not assumed.

**Verdict: NO.** This is not a numeric-value problem solvable by picking different share numbers — it is a progression-semantics gap: the current `LongRunWeeklyShareDecision`/`BuildLongRunPlan` mechanism has no notion of "ordinary week vs. peak-long-run week," so no share value (however chosen) can simultaneously satisfy "ordinary weeks sit near 33%" and "the peak week reaches up to 40%, sufficient to make the already-frozen 19.0km ceiling reachable." Per the governing decision's own explicit instruction, this must NOT be silently frozen and passed unnoticed into `HM.2`.

---

## 2. Frozen authority — `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED 14W`, growth-rate and golden-fixture-calibration fields ONLY

Per the governing decision's own contingency: since §1.3 fails, this phase freezes everything the decision proposed **except** the four long-run-share fields. `TaperVolumeMultiplier` was never in scope for this decision (reserved for `HM.1.4A`) and remains untouched.

```
HALF_MARATHON x INTERMEDIATE x 4D x 14W PREFERRED CORE

ResolvedPeakReferenceKm = 43.0   [already frozen, HM.1.4 -- unchanged, not re-decided here]

PreferredWeeklyIncreaseRate = 0.07   -> FROZEN
HardWeeklyIncreaseRate      = 0.08   -> FROZEN
AbsoluteWeeklyIncreaseCapKm = 2.5    -> FROZEN

GoldenFixtureStartingVolumeKm    = 25.0   -> FROZEN
GoldenFixtureNonTaperTransitions = 11     -> FROZEN

RoundingIncrementKm / RoundingRule = [retained from HM.1.4, unchanged]

PreferredLongRunShareMin = 0.30   -> NOT FROZEN (blocked, see §4)
PreferredLongRunShareMax = 0.36   -> NOT FROZEN (blocked, see §4)
SelectedLongRunShare     = 0.33   -> NOT FROZEN (blocked, see §4)
HardLongRunShareCap      = 0.40   -> NOT FROZEN (blocked, see §4)

TaperVolumeMultiplier = [not addressed by this decision -- reserved for HM.1.4A, unchanged]
```

### Classification — recorded with citations

- **`GoldenFixtureNonTaperTransitions = 11`** → `DIRECT_STRUCTURAL_DERIVATION`. `12` non-taper weeks per `HM.1`'s already-frozen `3 Foundation / 4 Build / 5 RaceSpecific` split → `11` transitions, via the code's own `nonTaperWeeks - 1` definition (`CatalogVolumeAndLongRunPlanner.cs:305`), verified in §1.2 above.
- **`GoldenFixtureStartingVolumeKm = 25.0`** → `GOLDEN_CALIBRATION_PRODUCT_DEFAULT`. **NOT user starting-volume authority.** Must NOT replace a provided recent weekly volume, become a missing-readiness fallback, become an HM readiness threshold, or force any user upward toward 25km. This is calibration-only — it feeds `ResolvePeak`'s `canonicalDefaultMultiplier` denominator (line 308) exclusively; it never appears in `ResolveStartingVolume` (lines 189-267) for any real request's own starting point. Recorded here prominently, not as a footnote, matching `HM.1.4 §2.5`'s own caveat about `ResolvedPeakReference` not being automatically usable in isolation.
- **`PreferredWeeklyIncreaseRate = 0.07` / `HardWeeklyIncreaseRate = 0.08` / `AbsoluteWeeklyIncreaseCapKm = 2.5`** → `EVIDENCE_INFORMED_PRODUCT_DEFAULT`. Reused from the existing 10K×Intermediate×4D generic safety-guard values (`VolumeSafetyPolicy.Default`, lines 77-79). This is **not** "10K value copied because convenient": these are generic weekly-progression safety guards — not distance-specific physiological constants — applied to the SAME frequency (4D) `HM.1.4`'s target cell already specifies, with no evidence found anywhere (10K's own 17 instances, nor `HM_21K_Claude_Handoff_and_Build_Plan.md`) requiring a different value for this specific distance/frequency combination. Explicitly NOT claimed as scientifically optimal for HM specifically — the same caveat `HM.1.4 §3` itself already raised and this decision explicitly overrides only for these three fields, by direct user authority.

### Critical implementation caveat — recorded prominently, not buried

Per `HM.1.4 §2.5`'s own already-disclosed caveat, freezing `GoldenFixtureStartingVolumeKm`/`GoldenFixtureNonTaperTransitions` now makes the `ResolvePeak` formula's numerator (`ResolvedPeakReferenceKm = 43.0`) and denominator both populated and, for the first time, **executable** — `canonicalDefaultMultiplier = 43.0 / 25.0 = 1.72`, and for this exact 14-week/12-non-taper cell, `transitions (11) == GoldenFixtureNonTaperTransitions (11)`, so `transitionAdjustedMultiplier` reduces exactly to `1.72` — a clean, self-consistent calibration episode with no residual scaling artifact for the canonical 14-week case. This computation is **not itself blocked** by the long-run-share gap in §1.3/§4 below — weekly-volume progression and long-run-distance progression are two independently-computed decisions (`BuildWeeklyPlan` and `BuildLongRunPlan` respectively) that only share the resolved weekly-volume number as an input. The long-run-share gap blocks only the long-run-distance-progression decision, not the weekly-volume decision this section freezes.

---

## 3. What was NOT frozen, and why — the disclosed semantics gap

### 3.1 — `PreferredLongRunShareMin`/`Max`/`SelectedLongRunShare`/`HardLongRunShareCap`: withheld, per the governing decision's own explicit contingency

Per §1.3's verification, these four fields are **not** frozen. Freezing them as flat numbers (0.30/0.36/0.33/0.40) would not by itself be wrong as *numbers* — the classification the governing decision proposed (`EVIDENCE_INFORMED_PRODUCT_DEFAULT, WITH independent HM real-program support`, citing Hal Higdon Intermediate 1's real ~31-32%-ordinary/40%-peak pattern) remains sound as a **target policy**. The problem is that the current code has no mechanism capable of *implementing* that target policy — it can only ever produce a single flat ratio across all weeks, never a rising one. Freezing the numbers without first closing this mechanism gap would create exactly the silently-swallowed contradiction the governing decision warned against: an authority record claiming "peak week may rise toward 40%" sitting beside a real implementation that can never produce more than 33% at any week, for any input, ever (structurally, not as an edge case).

### 3.2 — New, precisely-scoped item raised for a future phase (not resolved here)

**`DOMAIN_DECISION_REQUIRED — HM_LONG_RUN_SHARE_PROGRESSION_MECHANISM`**: `CatalogVolumeAndLongRunPlanner.BuildLongRunPlan` (`CatalogVolumeAndLongRunPlanner.cs:503-619`) computes long-run distance as a single flat `weeklyVolume(week) × SelectedLongRunShare` for every non-anchor, non-low-confidence week, with `PreferredLongRunShareMax`/`HardLongRunShareCap` functioning only as passive, structurally-unreachable safety ceilings in this branch (mirroring the already-disclosed `TD-VOLUME-CAP-UNENFORCED-001` pattern for a different field pair). This makes any future `HALF_MARATHON × INTERMEDIATE × 4D` peak-long-run authority above `PeakVolumeBand.MaximumKm × SelectedLongRunShare` (currently `50 × 0.33 = 16.5 km`) structurally unreachable — concretely, `HM.1.1`'s own already-frozen `19.0 km` preferred peak-long-run ceiling cannot be reached by any real request under this cell's frozen peak-volume band, regardless of what long-run-share numbers are chosen, unless the share mechanism itself is changed to distinguish an ordinary week from a peak-long-run week (e.g., a week-specific or phase-relative share escalation, analogous to the taper-only override mechanism `V1BeginnerThreeDayTaperLongRunSharePolicy` already establishes as a real precedent for a different phase-specific share exception, `CatalogVolumeAndLongRunPlanner.cs:519-529`). **This phase does not design or select that mechanism** — it is out of scope for a decision-recording phase and would require production code changes this phase is barred from making. The four long-run-share numeric values themselves (0.30/0.36/0.33/0.40, with real Hal Higdon program support for the *concept* of a rising peak-week share) remain a reasonable **starting point** for whichever future phase (tentatively `HM.1.6`, not reserved here) designs the actual mechanism — they are not rejected as numbers, only withheld pending a real implementation path that can use them without contradiction.

### 3.3 — `TaperVolumeMultiplier`: correctly out of scope, unchanged

Not addressed by the governing decision (explicitly reserved for a separate companion phase, `HM.1.4A`). Left untouched by this phase.

---

## 4. Explicit scope constraint

This freezes ONLY the five fields listed in §2, for exactly `HALF_MARATHON × INTERMEDIATE × 4D × PREFERRED 14W`. Does not generalize to any other level/frequency/horizon cell. Does not freeze, alter, or invent any long-run-share value. Does not alter `TaperVolumeMultiplier`. Does not implement, design, or select a fix for the long-run-share progression-mechanism gap disclosed in §3.2.

---

## 5. What this phase explicitly did NOT do

- Did not freeze the four long-run-share fields, despite the governing decision proposing specific values for them — verification in §1.3 found the real code cannot implement the described peak-week-escalation behavior, and the decision's own contingency requires withholding rather than forcing.
- Did not design, select, or implement any fix for the disclosed long-run-share progression-mechanism gap — precisely scoped as a new item for a future phase, not resolved here.
- Did not change any production code — verification in §1 read `VolumeSafetyPolicy.cs`/`CatalogVolumeAndLongRunPlanner.cs` directly but modified neither file.
- Did not touch `TaperVolumeMultiplier`, correctly left for the separate `HM.1.4A` companion phase.
- Did not author any HM catalog artifact, widen any public routing surface, or touch 10K's authority, code, or catalog in any way.
- Did not resume `HM.2`'s implementation work.
- Did not force the full 9-field freeze the governing decision described as the default expectation — the honest result, per real verification, is a 5-field freeze plus one newly-disclosed, precisely-scoped semantics gap.

---

## 6. Final classification

```
HM_VOLUME_PROGRESSION_GROWTH_AND_CALIBRATION_FROZEN_LONG_RUN_SHARE_BLOCKED_ON_SEMANTICS_GAP
```

Of the 10 fields the governing decision proposed to freeze, **5 close cleanly** (`PreferredWeeklyIncreaseRate = 0.07`, `HardWeeklyIncreaseRate = 0.08`, `AbsoluteWeeklyIncreaseCapKm = 2.5`, `GoldenFixtureStartingVolumeKm = 25.0`, `GoldenFixtureNonTaperTransitions = 11`), verified against the real `ResolvePeak` mechanism in `CatalogVolumeAndLongRunPlanner.cs` and found to compute a clean, self-consistent `1.72` multiplier for this exact cell. **4 fields (`PreferredLongRunShareMin`/`Max`/`SelectedLongRunShare`/`HardLongRunShareCap`) are withheld**, not because the proposed numbers are wrong, but because direct trace of `BuildLongRunPlan` (`CatalogVolumeAndLongRunPlanner.cs:503-619`) proved the current mechanism can only ever produce a flat share across all weeks, making the already-frozen `HM.1.1` 19.0km peak-long-run ceiling structurally unreachable under the frozen `[36,50]` peak-volume band regardless of which share numbers are chosen — a progression-semantics gap, not a numeric-authority gap, disclosed as its own precisely-scoped `DOMAIN_DECISION_REQUIRED` item (`HM_LONG_RUN_SHARE_PROGRESSION_MECHANISM`) for a future phase rather than silently frozen past. `TaperVolumeMultiplier` remains correctly untouched, reserved for `HM.1.4A`. Combined with `HM.1.4`'s own 3 already-frozen fields, `8 of 13` total `VolumeSafetyPolicy` fields for this cell are now frozen; `5` remain open (`TaperVolumeMultiplier`, plus the 4 long-run-share fields now blocked on the mechanism gap rather than plain missing evidence). No production code, catalog artifact, or public routing change in this phase. No 10K authority, code, or catalog touched.
