# PHASE HM.1.4A — `TaperVolumeMultiplier` TWO-WEEK TAPER MECHANISM AUDIT

**Phase type**: AUDIT ONLY — no production code changes, no number selected/frozen, no schema change, no HM catalog artifacts, no public routing changes, no 10K changes of any kind.
**Execution status**: DONE
**Final classification**: `HM_TWO_WEEK_TAPER_REQUIRES_PARAMETERIZATION`
**Governing prompt**: user-authored "HM PHASE PROMPT 1.4A" — exact text is the spec, followed precisely.
**Governance commit**: `b67dee8` (implementation + ledger row, backfilled self-referentially per this engagement's established two-commit pattern).
**Parent**: `HM.1.5` (`PHASE_HM_1_5_VOLUME_SAFETY_POLICY_PARTIAL_FREEZE_LONG_RUN_SHARE_SEMANTICS_GAP.md`), which froze 8 of 13 `VolumeSafetyPolicy` fields for `HALF_MARATHON × INTERMEDIATE × 4D × 14W` and explicitly reserved `TaperVolumeMultiplier` for this phase, `HM.1.4A`.

---

## 0. Startup confirmation

- `git fetch origin` clean; `git rev-list --left-right --count origin/main...HEAD` = `0 0` before starting.
- Working tree changes limited to the pre-existing tracked `bin`/`obj`/`baseline_tmp`/`ten-k-pilot-domain-decision-audit.*` rebuild-artifact noise this engagement has repeatedly called out — left untouched, not staged.
- `PHASE_LEDGER.md`'s HM Program table confirmed 9 existing rows (`HM.0`, `HM.0.1`, `HM.1`, `HM.1.1`, `HM.1.2`, `HM.1.3`, `HM.2`, `HM.1.4`, `HM.1.5`); no `HM.1.4A` row exists. **Chosen phase ID: `HM.1.4A`** — the exact name `HM.1.5`'s own record (§0, §3.3) reserved for this companion phase; no collision.
- Required reading completed in full before writing anything: `PHASE_HM_1_5_VOLUME_SAFETY_POLICY_PARTIAL_FREEZE_LONG_RUN_SHARE_SEMANTICS_GAP.md` (immediate predecessor); `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md` (confirms the frozen `3 Foundation / 4 Build / 5 RaceSpecific / 2 Taper` split for `HALF_MARATHON × INTERMEDIATE × 4D × 14W`); the real `VolumeSafetyPolicy.cs` and `CatalogVolumeAndLongRunPlanner.cs`, both read directly in full — not inferred from memory or from prior phases' prose summaries.
- No sub-agents spawned for this phase, per its own governance note; all investigation and writing performed directly.

---

## 1. Real-code inventory — every consumer of `TaperVolumeMultiplier`

Grepped the entire repository for `TaperVolumeMultiplier`. Excluding doc/markdown citations and test assertions, exactly **three production-code consumers** exist:

1. **`VolumeSafetyPolicy.cs`** — the field declaration (record parameter, line 58) and its value in every named instance (`0.53d` in all 17 instances, lines 83/98/128/155/216/232/248/264/308/336/391/406).
2. **`CatalogVolumeAndLongRunPlanner.ResolveTaperDecision`** (`CatalogVolumeAndLongRunPlanner.cs:338-353`) — validates the scalar once per `Build()` call and packages it into a `TaperVolumeDecision`.
3. **`CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan`** (`CatalogVolumeAndLongRunPlanner.cs:364-501`), specifically the `else if (isTaper)` branch (lines 395-401) — the only place the multiplier is actually applied to produce a week's planned volume.

A fourth, independent consumer exists in a different subsystem:

4. **`VolumeProgressionVerifier.Verify`** (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/VolumeProgressionVerifier.cs:89-113`) — a post-hoc auditor that re-derives an "expected" taper value for every `from → to` week transition and flags a `TAPER_MULTIPLIER_VIOLATION` if the actual deviates. Traced below because it is directly relevant to whether a 2-week compounding defect would ever be caught.

No other file (phase-materialization, `ProgressionStageAllocator`, `CatalogPhaseAllocationResolver`, etc.) reads or branches on `TaperVolumeMultiplier`.

---

## 2. Exact trace: `ResolveTaperDecision` (`CatalogVolumeAndLongRunPlanner.cs:338-353`)

```csharp
private TaperVolumeDecision ResolveTaperDecision()
{
    var reduction = 1d - _policy.TaperVolumeMultiplier;                         // line 340
    if (reduction < 0.41d || reduction > 0.60d)                                 // line 341
    {
        throw new CatalogVolumeInvalidTaperRuleException(...);                  // line 343
    }
    return new TaperVolumeDecision(_policy.TaperVolumeMultiplier, ...);         // lines 346-352
}
```

This is called **exactly once** per `Build()` invocation (line 138: `var taper = ResolveTaperDecision();`), producing a single `TaperVolumeDecision` object that is then threaded, unchanged, into `BuildWeeklyPlan` and reused for **every** taper week in the plan (line 140: `var weekly = BuildWeeklyPlan(request, bounds, starting, peak, taper);`).

**Critical finding**: this validation checks only the single-step reduction implied by the raw scalar (`1 − 0.53 = 0.47`, inside `[0.41, 0.60]` — passes). It has **no way to see or validate the cumulative, multi-week effect** of applying that scalar repeatedly across a multi-week taper phase — it runs once, before any week is materialized, against the policy object alone, with no visibility into `boundPlan.Weeks` or how many of them are tagged `TAPER`. This guard would not fire for a 2-week taper phase even if the eventual cumulative reduction fell far outside `[0.41, 0.60]`.

---

## 3. Exact trace: `BuildWeeklyPlan`'s taper branch (`CatalogVolumeAndLongRunPlanner.cs:376-484`)

```csharp
double? previous = null;
foreach (var week in orderedWeeks)                                              // line 377
{
    var isTaper = week.PhaseKey == "TAPER";                                     // line 379
    ...
    else if (isTaper)                                                           // line 395
    {
        unclamped = (previous ?? starting.SelectedStartingVolumeKm) * taper.Multiplier;   // line 397
        clamp = CatalogVolumeClamp.TaperReduction;
        changeRule = "canonical_taper_multiplier_0.53_from_previous_week";      // line 399
        ...
    }
    ...
    var selected = isTaper
        ? Math.Min(Round(unclamped), peak.SelectedPeakKm)                        // line 423
        : ...;
    ...
    previous = selected;                                                        // line 484 — updates on EVERY iteration, including taper weeks
}
```

**This is the load-bearing finding of this audit.** Three facts, all directly observable in this code, combine to determine the real 2-week-taper outcome:

1. `isTaper` is a pure boolean derived from `week.PhaseKey == "TAPER"` (line 379). There is **no concept anywhere in this method of which taper week a given week is**, how many taper weeks exist, or which one is "final." Every taper week is handled by the identical branch.
2. The taper branch's input is `previous` (line 397) — the **immediately preceding week's own already-selected volume** — not a fixed reference such as `peak.SelectedPeakKm` or the last non-taper week's volume captured once.
3. `previous` is reassigned to `selected` at the end of **every** loop iteration (line 484), including taper iterations. So the second taper week's `previous` is the **first taper week's own reduced output**, not the pre-taper peak.

The mechanical consequence for a 2-week Taper phase: `TaperVolumeMultiplier` is applied **twice in sequence**, each application consuming the prior application's already-reduced result. This is compounding by construction, not by accident of any particular input values — it would happen for any candidate whose Taper phase has 2 or more weeks, and it has evidently never been exercised because no existing `VolumeSafetyPolicy` instance/candidate in the repository today has a multi-week Taper phase reach this branch (10K's own precedent, cited in `HM.1.5 §1.1`, is a single taper week: "Golden Fixture v3 week 12 reduces from 38km to 20km"). HM's frozen `3/4/5/2` split is apparently the first real candidate shape with a 2-week Taper phase to reach this code.

### 3.1 Concrete numeric trace, using the already-frozen inputs

Using `HALF_MARATHON × INTERMEDIATE × 4D × 14W`'s frozen `3/4/5/2` split (12 non-taper weeks, 2 taper weeks) and the already-frozen `ResolvedPeakReferenceKm = 43.0` as the last RaceSpecific week's planned volume (this is exactly what the non-taper interpolation branch, lines 410-414, produces for the final non-taper week: `index == denominator`, so `unclamped == peak.SelectedPeakKm` exactly — confirmed by direct read, not assumed) — and `RoundingIncrementKm = 0.5` (frozen, `HM.1.4`, unchanged), `Round` defined at lines 623-626 as `Math.Round(value / increment, AwayFromZero) * increment`:

**Taper Week 1** (`previous = 43.0`, the last RaceSpecific week's volume):
```
unclamped = 43.0 * 0.53 = 22.79
Round(22.79, 0.5) = Math.Round(45.58, AwayFromZero) * 0.5 = 46 * 0.5 = 23.0
selected = Min(23.0, 43.0) = 23.0 km
previous ← 23.0
```

**Taper Week 2** (`previous = 23.0`, Taper Week 1's own selected output — NOT 43.0 again):
```
unclamped = 23.0 * 0.53 = 12.19
Round(12.19, 0.5) = Math.Round(24.38, AwayFromZero) * 0.5 = 24 * 0.5 = 12.0
selected = Min(12.0, 43.0) = 12.0 km
```

**Result: Taper Week 1 = 23.0 km, Taper Week 2 = 12.0 km.** Total reduction from the 43.0 km peak to the final taper week: `(43.0 − 12.0) / 43.0 = 72.1%` — closely matching the governing prompt's own illustrative unrounded arithmetic (`43 → 22.8 → 12.1`, ≈72%), confirmed here against the real, rounded, production code path rather than assumed.

### 3.2 Does `VolumeProgressionVerifier` catch this?

No. Traced `VolumeProgressionVerifier.Verify` (lines 89-113): for the `Week13(taper) → Week14(taper)` transition, `isTaper = to.IsTaperWeek = true` (line 95), so it computes `expectedTaperVolumeKm = from.PlannedWeeklyVolumeKm * policy.TaperVolumeMultiplier` (line 104) using `from.PlannedWeeklyVolumeKm` — Week 13's own already-reduced 23.0 km, the **same chained value** `BuildWeeklyPlan` itself used. The verifier's own definition of "correct" is "apply the multiplier once per step, relative to the immediately preceding week," which is identical to the planner's actual (compounding) behavior. It would report **zero violation** for this exact 2-week compounding scenario — the verifier is not an independent check against this defect; it encodes the same assumption.

---

## 4. Classification of current real behavior

**`COMPOUNDED_MULTIPLIER`** — confirmed by direct trace, not inferred.

Ruled out the other three options explicitly:
- **`SINGLE_FINAL_TAPER_REFERENCE_COMPATIBLE`** — false. There is no branch anywhere in `BuildWeeklyPlan` that identifies "the final taper week" or computes any taper week against a fixed pre-taper reference. Both taper weeks use the identical `else if (isTaper)` branch driven by `previous`.
- **`SAME_PEAK_MULTIPLIER_EACH_TAPER_WEEK`** — false. This would require every taper week to compute `peak.SelectedPeakKm (or 43.0) × 0.53` independently, landing both weeks at ≈22.8/23.0 km. The real code does not do this: Taper Week 2's input is Taper Week 1's own reduced 23.0 km, not the peak/43.0 reference again.
- **`OTHER`** — not needed; the code's actual behavior matches `COMPOUNDED_MULTIPLIER`'s definition exactly (43 → 22.8(→23.0 rounded) → 12.1(→12.0 rounded) km, ≈72% total reduction, outside the 41-60% evidence envelope `ResolveTaperDecision` itself validates against for the single-step case but cannot see for the cumulative case).

This is a **real defect** as applied to any candidate with a 2+-week Taper phase, confirmed by trace of code that already exists and already runs today — it has simply never been exercised by an existing candidate shape (every current 10K instance's Taper phase is 1 week).

---

## 5. Schema-compatibility check — can `VolumeSafetyPolicy` represent a genuine 2-step taper without a schema change?

Read `VolumeSafetyPolicy.cs:51-64` directly. The record's taper-relevant surface is exactly one field:

```csharp
public sealed record VolumeSafetyPolicy(
    ...
    double TaperVolumeMultiplier,
    ...)
```

A single `double`. There is no array, list, or second field of any kind for a week-specific or position-specific taper value anywhere in the 13-field record.

**Finding, in two parts:**

1. **As a literal data contract, the schema cannot store two independently-authored numeric targets** (e.g., "Taper Week 1 retains 65%, Taper Week 2 retains 41%") without adding a field — there is nowhere in the record to put a second number. This part of the question requires a schema extension if the product wants two distinct, separately-frozen authority values.
2. **A narrower fix is conceivable using the existing single scalar as-is**: if the *product intent* is "one authored reduction target for the final taper week, with any earlier taper week(s) computed by a deterministic algorithm (e.g., linear interpolation between the last non-taper week's volume and the final taper target) rather than a second authored number," then `VolumeSafetyPolicy` would not need a new field — but this is **not what the current code does** (confirmed `COMPOUNDED_MULTIPLIER` in §4), and building it requires:
   - New logic in `BuildWeeklyPlan`'s taper branch to detect *which* taper week is being processed (today: none — only the boolean `isTaper`) and how many taper weeks exist in total;
   - A new, currently-nonexistent computation rule for every non-final taper week (interpolation vs. a distinct per-week fraction vs. something else) — this is itself an unresolved design/product question with **zero evidence anywhere in this engagement's record** for how an intermediate (non-final) taper week should be computed, distinct from the final week;
   - A corresponding update to `VolumeProgressionVerifier`'s own taper-transition check (§3.2), which currently encodes the same chained/compounding assumption and would need to validate against the new, non-chained rule instead.

Neither path is "unfreeze one scalar, pick 0.53, done." Path 1 needs a schema extension; Path 2 needs new planner logic plus a new, currently-absent design decision for intermediate taper weeks — "new logic," precisely the alternative condition the governing prompt's own `REQUIRES_PARAMETERIZATION` definition names alongside a schema extension.

---

## 6. Final classification

```
HM_TWO_WEEK_TAPER_REQUIRES_PARAMETERIZATION
```

Representing `HALF_MARATHON × INTERMEDIATE × 4D × 14W`'s frozen 2-week Taper phase correctly — i.e., without producing the confirmed `COMPOUNDED_MULTIPLIER` ≈72% cumulative reduction, outside the 41-60% evidence envelope — requires a real change beyond selecting a scalar value. Specifically, at least one of:

- **(a) A schema extension** to `VolumeSafetyPolicy` (e.g., a second taper-related field, or restructuring `TaperVolumeMultiplier` into a per-taper-week-position value), plus the corresponding `BuildWeeklyPlan`/`VolumeProgressionVerifier` logic to consume it positionally instead of chaining from `previous`; **or**
- **(b) New planner logic only** (no new `VolumeSafetyPolicy` field): teach `BuildWeeklyPlan`'s taper branch to recognize taper-week position/count, apply `TaperVolumeMultiplier` once to a fixed pre-taper reference for the final taper week, and compute any earlier taper week via a newly-designed, currently-nonexistent deterministic rule (e.g., interpolation) — which is itself an open product/design decision this audit does not resolve, having found zero existing evidence for it anywhere in this engagement.

Either path is a real production-code change (and, for path (a), a real schema change) — not achievable by this audit phase, and not something a subsequent decision message freezing `TaperVolumeMultiplier = 0.53` alone would fix. **Freezing `TaperVolumeMultiplier = 0.53` as a number remains sound in isolation** (0.47 single-step reduction sits mid-envelope, `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, per the governing prompt's own §"If COMPATIBLE" text) — but that freeze alone does not make HM's real 2-week Taper phase produce a correct result today, because the defect is in how the scalar is *applied across two consecutive taper weeks*, not in the scalar's value.

---

## 7. What this phase explicitly did NOT do

- Did not change any production code — `CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`, and `VolumeProgressionVerifier.cs` were read directly, in full for the relevant methods, but not modified.
- Did not select, freeze, or invent any numeric coefficient — `TaperVolumeMultiplier` remains unfrozen, exactly as `HM.1.5` left it.
- Did not change or extend the `VolumeSafetyPolicy` schema.
- Did not design or select a fix for the 2-week-taper compounding defect disclosed in §3-§6 — precisely scoped as a new item for a future implementation phase, not resolved here.
- Did not author any HM catalog artifact, widen any public routing surface, or touch 10K's authority, code, or catalog in any way.
- Did not resume `HM.2`'s implementation work.
- Did not assume 10K's own single-week taper semantics apply to HM's 2-week case — traced the real code's actual multi-week behavior directly, per the governing prompt's explicit instruction not to assume.

---

## 8. Recommended next step

A separate, short decision message (not this audit phase) may freeze `TaperVolumeMultiplier = 0.53`, classified `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (external evidence envelope 41-60%, Mujika & Padilla 2007/2023; selected retained fraction 53%; equivalent single-step reduction 47%, mid-envelope) — but per §6 above, that freeze alone does **not** close HM's 2-week taper correctly; it would need to be paired with, or explicitly followed by, a small, separately-scoped implementation item that resolves §6's path (a) or (b), informed by this audit's findings, before a real `HALF_MARATHON × INTERMEDIATE × 4D × 14W` candidate can materialize a correct Taper phase.
