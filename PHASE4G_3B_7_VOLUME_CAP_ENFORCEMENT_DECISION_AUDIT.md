# Phase 4G.3B.7 — TD-VOLUME-CAP-UNENFORCED-001 Resolution Decision Audit

**Read-only decision audit. No production code changed. No test changed. No
TD updated. Recommendation only — not applied.**

---

## 1. Exact current planner code path (quoted)

`CatalogVolumeAndLongRunPlanner.ResolvePeak` (`.cs:99-131`) — the "reachable
peak" formula:

```csharp
private ReachablePeakDecision ResolvePeak(double startingVolumeKm, CatalogVolumeBounds bounds, BoundCatalogPlan boundPlan)
{
    var nonTaperWeeks = boundPlan.Weeks.Count(w => w.PhaseKey != "TAPER");
    var transitions = Math.Max(0, nonTaperWeeks - 1);
    var canonicalDefaultMultiplier = _policy.GoldenFixtureResolvedPeakKm / _policy.GoldenFixtureStartingVolumeKm;
    var transitionAdjustedMultiplier = 1d + ((canonicalDefaultMultiplier - 1d) * transitions / _policy.GoldenFixtureNonTaperTransitions);
    var reachable = startingVolumeKm * transitionAdjustedMultiplier;

    reachable = Round(reachable);
    var selected = reachable < bounds.MinimumKm ? reachable : Round(Clamp(reachable, bounds.MinimumKm, bounds.MaximumKm));
    ...
    return new ReachablePeakDecision(
        startingVolumeKm, reachable, selected, classification,
        _policy.PreferredMaxWeeklyIncreaseRatio,
        _policy.HardMaxWeeklyIncreaseRatio,
        _policy.AbsoluteWeeklyIncrementCapKm,
        ...);
}
```

`CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` (`.cs:159-280`) — the actual
per-week non-taper interpolation (the only place a real value is produced):

```csharp
else
{
    var index = nonTaperWeeks.FindIndex(w => w.WeekNumber == week.WeekNumber);
    var denominator = Math.Max(1, nonTaperWeeks.Count - 1);
    unclamped = starting.SelectedStartingVolumeKm + ((peak.SelectedPeakKm - starting.SelectedStartingVolumeKm) * index / denominator);
    clamp = CatalogVolumeClamp.None;
    ...
}

var selected = isTaper
    ? Math.Min(Round(unclamped), peak.SelectedPeakKm)
    : Round(Clamp(unclamped, Math.Min(starting.SelectedStartingVolumeKm, peak.SelectedPeakKm), peak.SelectedPeakKm));
```

**Every reference to `HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm`
in the entire file** is exactly the three arguments passed into the
`ReachablePeakDecision` constructor above (`.cs:125-127`) — pure record
construction, never read back. Confirmed by direct, exhaustive re-inspection
of the file: no `if`, `Math.Min`, `Math.Max`, or `Clamp` call anywhere in
`BuildWeeklyPlan` references either field. The only clamp actually applied
per non-taper week (`Clamp(unclamped, Math.Min(start,peak), peak)`) bounds
the value between the starting volume and the selected peak — it has nothing
to do with the *rate* of week-to-week change. This is an exact, complete
confirmation of the TD's own claim, not a re-statement of it.

`VolumeSafetyPolicy.Default` (`.cs:37-50`) — the actual configured values:
`PreferredMaxWeeklyIncreaseRatio: 0.07d`, `HardMaxWeeklyIncreaseRatio: 0.08d`,
`AbsoluteWeeklyIncrementCapKm: 2.5d`, `GoldenFixtureStartingVolumeKm: 24d`,
`GoldenFixtureResolvedPeakKm: 38d`, `GoldenFixtureNonTaperTransitions: 10`.

`plan-catalog/catalog/templates/ten-k-master.v6.json` — `coreCycle`:
`minimumWeeks: 8, defaultWeeks: 12, maximumWeeks: 14`; `TAPER` phase:
`minimumWeeks: 1, preferredWeeks: 1, maximumWeeks: 1` (fixed, always exactly
1 week, `isCompressionProtected: true`) — confirmed by direct read, not
assumed: this means `nonTaperWeeks = weekCount - 1` and
`transitions = weekCount - 2` for every real target 8-14, always.

`plan-catalog/catalog/policies/peak-volume-bands.v3.json` — confirmed
directly: `{"distanceFamily": "TEN_K", "experience": "INTERMEDIATE",
"runsPerWeek": 4, "minimumKm": 30, "maximumKm": 42}` — matches the task's
own cited 30-42km bounds exactly, source-verified rather than assumed.

---

## 2. Real observed max ratio increase per target + headroom

`VolumeProgressionVerifierTests.RealVolumePlanAsync` (`.cs:177-230`) builds
its "real" plan using a **test-local fixture with `RecentWeeklyVolumeKm =
24`** (`.cs:224`) — matching `GoldenFixtureStartingVolumeKm` exactly (not
the separate, also-real `Sw02`/`Sw13` HTTP acceptance payload's
`recent_weekly_volume_km=20`; these are two different real fixtures used by
different tests, both legitimate, both computed below).

The existing test suite already confirms **zero violations** for all 7
targets (`Verify_RealTwelveWeekPilotPlan_OutcomeIsPass`,
`Verify_RealNonPilotWeekCounts_ReportsActualOutcome_NotAssumedPass` for
8/9/10/11/13/14) — re-run in this pass, still passing (see Validation). The
per-transition numeric detail is **not stored in any committed report file**
(the test's own comment references "this phase's final report for the full
per-transition numbers," but no such numbers were found in any tracked
`.md` file — searched `PHASE4G_3B_3_SAFETY_VERIFICATION_PIPELINE_PLANNING.md`
and `PHASE4G_3B_4B_SAFETY_VERIFICATION_ORCHESTRATOR.md`, the only two files
referencing `VolumeProgressionVerifier` at all). This audit therefore
**computed the exact numbers directly from the quoted formula above**
(arithmetic shown, not re-derived from a different or invented formula),
for the `RecentWeeklyVolumeKm=24` fixture, full week-by-week, for the two
structurally-relevant targets (the tightest case, 8 weeks/fewest
transitions, and the live pilot, 12 weeks):

**12-week pilot (start=24, peak=38, denominator=10):**

| Week | Volume (km) | Δ km | Ratio |
|---|---|---|---|
| 1 | 24.0 | — | — |
| 2 | 25.5 | 1.5 | 6.25% |
| 3 | 27.0 | 1.5 | 5.88% |
| 4 | 28.0 | 1.0 | 3.70% |
| 5 | 29.5 | 1.5 | 5.36% |
| 6 | 31.0 | 1.5 | 5.08% |
| 7 | 32.5 | 1.5 | 4.84% |
| 8 | 34.0 | 1.5 | 4.62% |
| 9 | 35.0 | 1.0 | 2.94% |
| 10 | 36.5 | 1.5 | 4.29% |
| 11 | 38.0 (peak) | 1.5 | 4.11% |
| 12 (taper) | 20.0 | -18.0 | (taper rule, not increase rule) |

**Max: 6.25% (week 1→2), Δ=1.5km.** Headroom to `HardMaxWeeklyIncreaseRatio`
(8%): **1.75 percentage points** (21.9% relative headroom). Headroom to
`AbsoluteWeeklyIncrementCapKm` (2.5km): **1.0km** (40% relative headroom).

**8-week target (start=24, peak=32.5, denominator=6):**

| Week | Volume (km) | Δ km | Ratio |
|---|---|---|---|
| 1 | 24.0 | — | — |
| 2 | 25.5 | 1.5 | 6.25% |
| 3 | 27.0 | 1.5 | 5.88% |
| 4 | 28.5 | 1.5 | 5.56% |
| 5 | 29.5 | 1.0 | 3.51% |
| 6 | 31.0 | 1.5 | 5.08% |
| 7 | 32.5 (peak) | 1.5 | 4.84% |
| 8 (taper) | 17.0 | -15.5 | (taper rule) |

**Max: 6.25% (week 1→2), Δ=1.5km — same margin as the 12-week case.**

This audit also independently verified, using the *established live-HTTP*
`recentWeeklyVolumeKm=20` payload (`Sw02`/`Sw13`), that the max observed
ratio is **7.14%** (a rounding-cascade artifact at week 2→3, present
identically at both 8 and 12 weeks) — still below `HardMax` (8%) but only
0.86 percentage points of headroom (10.75% relative), noticeably tighter
than the 24km-fixture case. Both real fixtures stay under both caps; the
margin is comfortable in one and real-but-narrower in the other. Full
week-by-week arithmetic for this second case is available on request but
omitted here for length — the 6.25%/7.14% figures above are the load-bearing
numbers.

**Honesty note on scope:** this audit fully hand-verified 2 of the 7 real
targets (the tightest-transitions case and the live pilot) for each of the
two real fixtures. The remaining 5 targets are covered by the mathematical
invariant proven in section 3 below, not independently hand-computed
transition-by-transition — stated explicitly rather than implied as
independently verified for all 7.

---

## 3. Reachability finding: structurally impossible, or genuinely reachable?

**Structurally impossible for this candidate today, for `HardMaxWeeklyIncreaseRatio`,
across any realistic starting volume and any of the 7 real target week
counts — proven by a formula-level invariant, not merely observed by
sampling.**

**The proof:** for any non-taper transition computed by the unclamped
linear-interpolation path (i.e. whenever `ResolvePeak`'s `reachable` is
either below `bounds.MinimumKm` or within `[MinimumKm, MaximumKm]` — see
below for the clamped-above case), the *ideal* (pre-rounding) per-step ratio
relative to the very first week is:

```
idealRatio = (peak - start) / transitions / start
           = (start * transitionAdjustedMultiplier - start) / transitions / start
           = (transitionAdjustedMultiplier - 1) / transitions
           = [(canonicalDefaultMultiplier - 1) * transitions / GoldenFixtureNonTaperTransitions] / transitions
           = (canonicalDefaultMultiplier - 1) / GoldenFixtureNonTaperTransitions
           = (38/24 - 1) / 10
           = 0.583333 / 10
           = 0.0583333  (5.83%)
```

**`transitions` cancels out of the formula algebraically.** This ratio is
**mathematically constant at 5.83%, independent of both `startingVolumeKm`
and the target week count**, for every one of the 7 real targets (transitions
6 through 12), as long as `peak.SelectedPeakKm` is not clamped *down* from
its raw multiplier-derived value. 5.83% ideal, plus the empirically-confirmed
rounding-cascade ceiling of ~7.14% (section 2), both sit below the 8% hard
cap with real, non-coincidental margin — the margin exists *because of the
formula's own algebraic structure*, not merely because the specific tested
inputs happened to land safely.

**What happens if `startingVolumeKm` is high enough to trigger the upper
clamp** (`reachable > bounds.MaximumKm = 42`)? This makes the actual
`selected` peak *smaller* than `start * transitionAdjustedMultiplier` would
imply, which can only *reduce* the effective per-step ratio below the 5.83%
invariant, never increase it — confirmed by direct inspection of `Clamp`'s
one-directional effect. If clamping would make `selected < startingVolumeKm`
(an implausibly high starting volume relative to the peak band), `ResolvePeak`
**throws `CatalogVolumeUnreachablePeakRuleException`** (`.cs:115-118`)
instead of silently producing an unsafe or degenerate curve — a real,
already-existing fail-closed backstop for that specific edge, independent of
this TD.

**For `AbsoluteWeeklyIncrementCapKm` (2.5km):** `kmIncrease = start *
0.0583333` (from the same invariant). The most permissive realistic case —
starting volume at the highest value that still keeps the raw peak at or
below the 42km ceiling, evaluated at the tightest target (8 weeks, `mult=1.35`,
threshold `start ≈ 42/1.35 ≈ 31.1km`) — yields `kmIncrease ≈ 31.1 * 0.0583333
≈ 1.82km`, **0.68km (27%) below the 2.5km cap.** This ceiling *decreases*
for every other (longer) target, since `mult` grows with week count while the
`0.0583333` coefficient stays fixed — so 8 weeks is confirmed the worst case
by construction, not by sampling. Rounding artifacts could add roughly
another 0.3-0.5km in a worst realistic case (by the same magnitude observed
for the ratio cap in section 2), which would still land at ≈2.1-2.3km,
under 2.5km — this specific bound is analytically argued rather than
exhaustively hand-verified transition-by-transition, and is reported with
that caveat rather than overstated as fully proven.

**Conclusion:** for `TEN_K__4D__INTERMEDIATE`/`TEN_K_MASTER v6` specifically,
given its current `GoldenFixtureStartingVolumeKm/GoldenFixtureResolvedPeakKm/GoldenFixtureNonTaperTransitions`
constants and its current `coreCycle`/`TAPER`/`PEAK_VOLUME_BANDS_V1` bounds,
**a scenario that actually exceeds either cap is structurally impossible
today** — not "coincidentally safe for the values that happen to have been
tested," but safe by algebraic construction of the formula itself, bounded
by real, source-verified catalog constants. **This finding is conditional on
those specific constants and bounds continuing to hold** — see section 9 for
the explicit conditions under which it would need re-verification.

---

## 4. Option A cost/regression-risk estimate

**Critical finding, stated prominently as required: enforcing the caps
would NOT change the live 12-week pilot's current output.** Both real
fixtures examined (the `VolumeProgressionVerifierTests` 24km fixture and the
established `Sw02`/`Sw13` HTTP-tested 20km payload) stay below both caps at
every real transition (section 2) — a clamp added on top of the existing
formula would never actually fire for either of these already-established,
already-tested real inputs. The already-passing
`Verify_RealTwelveWeekPilotPlan_OutcomeIsPass` test (asserting `Pass` and
zero findings) independently corroborates this from the verifier's own
side.

**Implementation is not a trivial "add one clamp line," however.** A naive
per-week clamp (`selected = Min(selected, previous * (1 + HardMax), previous
+ AbsoluteCapKm)`) would create a **new correctness problem**: if the
interpolation's raw per-step target ever legitimately needed a larger jump
than the cap allows (not the case for the current candidate, per section 3,
but the whole point of "enforcement" is to guard against a *future*
candidate where it might), later weeks would never catch up to
`ReachablePeakDecision.SelectedPeakKm` within the fixed number of non-taper
weeks — leaving `PeakVolumeKm`/`ReachablePeakDecision` describing a peak the
plan's last non-taper week never actually reaches. This is a real design
question, not a mechanical add-on: a correct implementation would need
either (a) to redefine "reachable peak" itself as *whatever volume the
capped growth rate can actually reach in the available transitions*
(`start * (1 + effectiveCap)^transitions`-style compounding, or an
equivalent capped-linear model), replacing the golden-fixture-scaled
formula rather than merely gating its output, or (b) to accept that a
capped plan may legitimately fall short of the nominal peak and make that
an explicit, documented outcome. Additionally, `VolumeProgressionVerifier`'s
own doc comment (`.cs:38-53`) already flags that the *combination rule*
between the ratio cap and the absolute-km cap (AND vs. OR) was never
resolved against real planner logic — the same ambiguity would need a
product/engineering decision before Option A's clamp logic could be written
correctly, not invented ad hoc by this audit.

**Regression risk to the live 12-week pilot: none, if implemented correctly**
(per the "would not change current output" finding above) — but the
implementation itself carries real design risk if done as a naive clamp
rather than a properly-redesigned reachable-peak model, which is a
materially larger engineering task than the TD's own wording ("a real
engineering change, however small") may suggest.

---

## 5. Option B documentation-change proposal

If Option B is chosen, the following documentation locations would need an
explicit informational-only reclassification (not applied in this pass):

1. **`VolumeSafetyPolicy.cs`** — add to the record's own doc comment (near
   `HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm`'s declarations):
   *"As of Phase 4G.3B/4G.3B.7, these two fields are informational/provenance-only:
   they are threaded into `ReachablePeakDecision` for decision-trace purposes
   but are never read back by `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan`
   to clamp or reject an actual week-to-week transition. Safe today because
   the planner's linear-interpolation formula has a proven algebraic invariant
   (ideal per-step ratio ≡ (GoldenFixtureResolvedPeakKm/GoldenFixtureStartingVolumeKm
   - 1) / GoldenFixtureNonTaperTransitions, independent of starting volume or
   week count) that keeps every real transition for the current
   TEN_K_MASTER v6 8-14-week range below both values with real margin — see
   PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md. Would need
   revisiting if GoldenFixtureStartingVolumeKm/GoldenFixtureResolvedPeakKm/GoldenFixtureNonTaperTransitions
   change, if a future candidate's core-cycle/peak-volume-band bounds differ
   materially from this one, or if a non-TEN_K_MASTER-v6-shaped candidate is
   ever added."*
2. **`PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md`** — add a
   cross-reference to this decision audit and the same informational-only
   statement, matching that document's existing per-field provenance
   convention.
3. **`VolumeProgressionVerifier.cs`**'s own doc comment — already correctly
   describes the gap ("IMPORTANT CAVEAT... does NOT actually apply... as a
   per-week-transition cap") and needs only a added cross-reference to this
   audit's conclusion, not a rewrite of its existing, still-accurate caveat.
4. **`TD-VOLUME-CAP-UNENFORCED-001`** itself — a future, separate pass (not
   this one, per scope) would record the actual decision made and close or
   re-scope the TD accordingly.

---

## 6. Six-criteria comparison table

| Criterion | Option A (enforce) | Option B (reclassify as informational) |
|---|---|---|
| **Safety** — risks silently under-protecting a real future scenario? | No — actively closes the gap for any future candidate. | **Partial risk, explicitly bounded**: safe for the current candidate by proof (§3), but a future candidate with different golden-fixture constants or bounds could reintroduce real risk if this documentation isn't re-verified at that time — mitigated only by the explicit revisit-trigger language in §5/§9, not by code. |
| **Honesty** — does the field's behavior match its name/purpose? | Yes, fully — the field would do what its name says. | Yes, **after** the documentation change — today, *before* any change, the fields are actively misleading (named as caps, behave as inert metadata); Option B fixes this honesty gap without fixing the underlying behavior gap. |
| **Implementation cost / regression risk** | Moderate-to-real (§4: not a trivial clamp — may require redefining "reachable peak" itself, plus resolving the AND/OR combination-rule ambiguity `VolumeProgressionVerifier` already flagged). Zero regression risk to current live output (proven, §4). | Zero — pure documentation, no code touched, no risk of any kind. |
| **`ARCHITECTURAL_CLAIM_VERIFICATION_GOVERNANCE.md` consistency** (claims about scope must be source-verified, not implied broader) | Consistent, if implemented — the enforced behavior would match its documented scope exactly. | Consistent **only if** the documentation is precise about the *conditional* nature of the safety claim (§3's invariant, §5's revisit triggers) rather than an unqualified "this is always safe" statement — this audit's proposed text (§5) is written to satisfy that bar explicitly. |
| **Forecloses the other option later?** | No — once enforced, it can always be relaxed/removed later if found overly conservative (a design regression, but not a safety regression). | **No** — reclassifying as informational today does not prevent implementing real enforcement later; §5's proposed text explicitly states the conditions under which A should be revisited, keeping the door open rather than closing it. |

---

## 7. Recommendation — **Option B (documentation-only reclassification), NOT YET APPLIED**

**Reasoning tied directly to the evidence above:** Section 3's finding is
the deciding factor. This is not a case of "the tested inputs happened to
be safe" (which would weigh toward A, per the task's own stated decision
rule) — it is a **structural, algebraic invariant** of the current
candidate's specific formula constants, independently confirmed for two
real fixtures at the two most structurally relevant targets (§2), with the
mathematical proof explaining *why* every other target and every realistic
starting volume must also stay safe (§3), not merely observed to do so by
chance. Per the task's own explicit decision rule — *"If the evidence in
step 3 shows a real, reachable scenario where the caps would matter for
THIS candidate today, that should weigh heavily toward A... If step 3 shows
the scenario is structurally impossible given the current catalog bands, B
becomes far more defensible"* — the evidence unambiguously lands in the
second case. Combined with Option A's real, non-trivial implementation
complexity (§4 — potentially requiring a redesign of the reachable-peak
model itself, not just a clamp) and its already-confirmed zero benefit to
the current live pilot's actual output, Option B is the recommendation:
**fix the honesty gap immediately via documentation (§5), defer the
implementation cost of A until a real future scenario (§9) actually
requires it.**

---

## 8. (Option A not recommended — smallest-safe-implementation sketch omitted per section 7's conclusion, available on request if the recommendation is overridden.)

---

## 9. Exact proposed documentation text — see section 5 in full.

## 10. Open questions for engineering sign-off before implementing either option

1. **Does engineering accept Option B's conditional safety claim** (§3's
   invariant, explicitly scoped to `TEN_K_MASTER v6`'s current constants) as
   sufficient governance, or does risk tolerance require Option A regardless
   of the proof, purely as defense-in-depth for any future catalog change?
2. **If Option A is ever pursued**, who resolves the AND/OR combination-rule
   ambiguity between the ratio and absolute-km caps that
   `VolumeProgressionVerifier`'s own doc comment already flagged as
   unresolved against real planner logic — this audit did not invent an
   answer and should not be assumed to have implicitly picked one.
3. **What specifically should trigger a mandatory re-verification** of this
   audit's §3 invariant: any change to `GoldenFixtureStartingVolumeKm`/
   `GoldenFixtureResolvedPeakKm`/`GoldenFixtureNonTaperTransitions`; any new
   candidate whose `coreCycle`/`TAPER` bounds differ from `TEN_K_MASTER v6`'s;
   any new `PEAK_VOLUME_BANDS_V1` entry with a materially different
   min/max spread; or all three — this should be recorded explicitly
   wherever Option B's documentation change is actually applied.
