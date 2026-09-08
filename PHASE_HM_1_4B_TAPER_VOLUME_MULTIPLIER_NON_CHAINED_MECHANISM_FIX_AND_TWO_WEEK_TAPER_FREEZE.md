# PHASE HM.1.4B — `TaperVolumeMultiplier` NON-CHAINED MECHANISM FIX AND TWO-WEEK TAPER FREEZE

**Phase type**: IMPLEMENTATION (real production code + real tests) AND DECISION (two taper-week numeric values frozen) — dark-only, no public routing/gate change, no HM catalog artifact authored.
**Execution status**: DONE
**Final classification**: `HM_TWO_WEEK_TAPER_MECHANISM_FIXED_AND_VALUES_FROZEN_ZERO_DELTA_CONFIRMED`
**Governing prompt**: user-authored "HM PHASE PROMPT 1.4B" — exact text is the spec, followed precisely.
**Governance commit**: `a51fbd9` (implementation + ledger row, backfilled self-referentially per this engagement's established two-commit pattern).
**Parent**: `HM.1.4A` (`PHASE_HM_1_4A_TAPER_VOLUME_MULTIPLIER_TWO_WEEK_TAPER_MECHANISM_AUDIT.md`, audit-only, confirmed `COMPOUNDED_MULTIPLIER`: a 2-week taper chained a single scalar twice, producing 43.0→23.0→12.0km, a 72.1% cumulative reduction, and found `VolumeProgressionVerifier` shares the identical chaining assumption and would report zero violation for this exact defect).

---

## 0. Startup confirmation

- `git fetch origin` clean; `git rev-list --left-right --count origin/main...HEAD` = `0 0` before starting.
- Working tree changes limited to the pre-existing tracked `bin`/`obj`/`baseline_tmp`/`ten-k-pilot-domain-decision-audit.*`/`.trx` rebuild-artifact noise this engagement has repeatedly called out — left untouched, not staged.
- `PHASE_LEDGER.md`'s HM Program table confirmed 10 existing rows (`HM.0` through `HM.1.4A`); no `HM.1.4B` row exists. **Chosen phase ID: `HM.1.4B`** — no collision.
- Required reading completed in full before writing anything: `PHASE_HM_1_4A_TAPER_VOLUME_MULTIPLIER_TWO_WEEK_TAPER_MECHANISM_AUDIT.md` (immediate predecessor, exact `BuildWeeklyPlan` trace with line numbers and the `VolumeProgressionVerifier` blind-spot finding); `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md` (confirms the frozen `3/4/5/2` split); `PHASE_HM_1_5_VOLUME_SAFETY_POLICY_PARTIAL_FREEZE_LONG_RUN_SHARE_SEMANTICS_GAP.md` (confirms `ResolvedPeakReferenceKm=43.0`, `GoldenFixtureStartingVolumeKm=25.0`, `GoldenFixtureNonTaperTransitions=11`, growth rates `0.07/0.08/2.5` already frozen for this cell; the 4 long-run-share fields remain blocked, unrelated to this phase's scope); the real `CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`, `VolumeProgressionVerifier.cs`, read directly and in full (not inferred from prior phases' prose).
- No sub-agents spawned, per this phase's own governance instruction — all investigation, implementation, and verification performed directly.

---

## 1. Step 1 — design verification against the real code

The proposed design (each taper week computed independently against the fixed pre-taper peak reference, never chained; `VolumeSafetyPolicy` gains a per-taper-week multiplier sequence) was verified against the real code before implementing, per the governing prompt's own instruction not to accept it uncritically.

**Finding: the proposed design is sound in spirit, but a simpler, more robust mechanism satisfies the same non-chaining guarantee without depending on `peak.SelectedPeakKm` bit-identically matching the pre-taper anchor for every existing policy shape** (in particular the two 3-day sequential-growth policies, `ThreeDayIntermediate`/`ThreeDayBeginner`, whose peak is computed by a structurally different iterative-growth loop in `ResolvePeak` than the per-week growth arithmetic in `BuildWeeklyPlan`'s own non-taper branch — proving byte-for-byte equality between the two independently-computed values for every existing policy was avoidable risk, not a requirement).

**Mechanism actually implemented — the pre-taper-anchor-capture pattern:**

- `BuildWeeklyPlan` now captures a single `double? preTaperAnchorKm`, assigned exactly once, at the moment the taper phase is entered (`preTaperAnchorKm ??= previous ?? starting.SelectedStartingVolumeKm`) — i.e. it is literally `previous`, the immediately preceding (non-taper) week's own materialized volume, captured before any taper week can overwrite it.
- Every taper week's `unclamped` target is then computed as `preTaperAnchorKm.Value * positionMultiplier`, where `positionMultiplier` is that taper week's own entry in an ordered multiplier list (`taperMultipliers[taperPosition]`, `taperPosition` incrementing once per taper week processed).
- **No taper week's target ever depends on another taper week's own (already-reduced) output** — every taper week reads only the single, fixed `preTaperAnchorKm` value and its own position's multiplier. This is the exact non-chaining guarantee the governing prompt required, achieved without introducing any new coupling to `ReachablePeakDecision.SelectedPeakKm`.
- For a 1-week taper (every existing 10K policy today), `preTaperAnchorKm` is captured and used exactly once, and — since `taperMultipliers` degrades to the single-element `[TaperVolumeMultiplier]` for every such policy — the arithmetic (`preTaperAnchorKm.Value * taperMultipliers[0]`) is the literal, byte-for-byte equivalent of the pre-existing `(previous ?? starting.SelectedStartingVolumeKm) * taper.Multiplier` formula. The legacy branch's `changeRule` string (`"canonical_taper_multiplier_0.53_from_previous_week"`) is preserved verbatim, unconditionally, for the `taperMultipliers.Count == 1` case — not merely the number, the full decision-trace text is unchanged.
- `VolumeSafetyPolicy` gained one new, optional, trailing record parameter: `IReadOnlyList<double>? TaperVolumeMultipliers = null`, plus a computed property `ResolvedTaperVolumeMultipliers => TaperVolumeMultipliers ?? [TaperVolumeMultiplier]`. Because it is optional and trailing, all 17 existing named instances (which construct the record with named arguments) compile and behave unchanged — none of them sets it, so all resolve to the single-element legacy list.

This is the "simpler mechanism… achieves the same non-chaining guarantee without a full schema redesign" the governing prompt explicitly permitted in place of the originally proposed shape, used because the real-code trace showed it removes an unnecessary and unproven equivalence assumption for the two 3-day sequential-growth policies.

**`VolumeProgressionVerifier` received the identical fix**, since it is a fully independent re-derivation (does not call the planner): it now computes its own `preTaperAnchorKm` (captured once, at the first taper *transition* it encounters — `from.PlannedWeeklyVolumeKm` at that point) and its own `taperWeekNumbersOrdered`/position lookup, reading `policy.ResolvedTaperVolumeMultipliers`. For a 1-week taper this reduces, again byte-for-byte, to the pre-existing `from.PlannedWeeklyVolumeKm * policy.TaperVolumeMultiplier` formula. `VolumeProgressionVerifier.Verify`'s public signature was **not** changed (an existing test, `Verify_IsPureFunction_TwoParametersOnly`, asserts exactly two parameters — confirmed compatible: the plan's already-existing `IsTaperWeek` flags and the policy's own resolved multiplier list are sufficient, no new parameter needed).

---

## 2. Step 2 — the two taper-week values, frozen

**Real-code validation shape actually needed** (per the audit's schema-compatibility citation): `ResolveTaperDecision` was generalized from validating a single scalar's reduction to validating an ordered multiplier sequence:

1. **Count check**: the policy's resolved multiplier count must exactly equal the bound plan's real taper-week count (0 taper weeks skips the check entirely — moot/unreachable case). Mismatch throws `CatalogVolumeInvalidTaperRuleException`.
2. **Final-week envelope check** (unchanged in spirit from the pre-existing single-scalar check): the **final** (deepest) taper week's reduction must fall in `[0.41, 0.60]` — the same evidence envelope every existing 10K single-week taper already satisfies, now explicitly scoped to "final week" rather than "the only week."
3. **New monotonic-deepening check**: every taper week before the final one must have a reduction that is both strictly positive and strictly smaller than the final week's reduction — i.e. taper reductions must deepen monotonically toward race week, never invert. Unreachable for every existing 10K policy (`multipliers.Count == 1`, loop never executes).

**Frozen values**, in a new dedicated class `HalfMarathonIntermediate4DTaperVolumePolicy` (`backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/HalfMarathonIntermediate4DTaperVolumePolicy.cs`), mirroring the engagement's own established convention for narrow, dedicated numeric-policy classes (`V1BeginnerThreeDayTaperLongRunSharePolicy`):

```
TaperWeek1VolumeMultiplier = 0.70   (≈30% reduction from the fixed 43.0km peak)
TaperWeek2VolumeMultiplier = 0.43   (≈57% reduction from the same fixed 43.0km peak — final/deepest, race week)
ResolvedPeakReferenceKm    = 43.0   [already frozen, HM.1.4/HM.1.5 — not re-decided here, cited for reference only]
```

Both applied **independently** against the fixed 43.0km pre-taper reference: `43.0 × 0.70 = 30.1 → round(0.5) = 30.0km`; `43.0 × 0.43 = 18.49 → round(0.5) = 18.5km`. Cumulative reduction from peak to final taper week: `(43.0−18.5)/43.0 ≈ 57.0%` — squarely inside the evidence envelope, nowhere near the confirmed 72.1% compounding defect.

**Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, with multi-source external convergence** (per the governing prompt; recorded as previously-gathered external evidence, not re-verified via new research this phase):
- Mujika & Padilla meta-analyses (2007, 2023): ~41–60% total volume reduction over a ~2-week taper.
- Marathon Handbook's HM-specific 2-week taper guide: Week 1 ≈ 25–35%, Week 2 (race week) ≈ up to 60%.
- best-running-tips.com: 40–60% standard, lighter-taper exception only above ~56km/week (the frozen 36–50km/week band sits below this threshold).
- runtothefinish.com: taper week 2 up to 60%.

**Dark-only, no full `VolumeSafetyPolicy` instance created for HM.** `HM.1.5 §3` left 4 of `VolumeSafetyPolicy`'s 13 fields (the long-run-share fields) genuinely unfrozen for this cell, blocked on a disclosed progression-mechanism gap unrelated to taper. Constructing a real, named `VolumeSafetyPolicy.HalfMarathonIntermediate4D` instance now would require inventing values for those four fields, which no phase has authorized — so this phase's frozen values live in their own dedicated, fully-documented class instead (exactly the same pattern the engagement already uses for `V1BeginnerThreeDayTaperLongRunSharePolicy`), proven correct via a **test-local** `VolumeSafetyPolicy` that reuses every already-frozen field for this cell and marks the four unfrozen long-run-share fields explicitly as test-only plumbing (never asserted as HM authority).

---

## 3. Exact final code shape

| File | Change |
|---|---|
| `VolumeSafetyPolicy.cs` | New optional trailing field `IReadOnlyList<double>? TaperVolumeMultipliers = null`; new computed property `ResolvedTaperVolumeMultipliers`. All 17 existing named instances unchanged (none sets the new field). |
| `HalfMarathonIntermediate4DTaperVolumePolicy.cs` (new file) | `TaperWeek1VolumeMultiplier = 0.70d`, `TaperWeek2VolumeMultiplier = 0.43d`, `ResolvedPeakReferenceKm = 43.0d` (citation only), `OrderedMultipliers` static list. |
| `CatalogVolumeAndLongRunPlanner.ResolveTaperDecision` | Signature changed to `(TaperVolumeDecision Decision, IReadOnlyList<double> Multipliers) ResolveTaperDecision(int taperWeekCount)` (private method — no external contract change). Generalized validation (count match, final-week envelope, monotonic-deepening). `multipliers.Count == 1` branch is byte-identical to the pre-HM.1.4B body. |
| `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` | New parameter `IReadOnlyList<double> taperMultipliers`. New `preTaperAnchorKm`/`taperPosition` capture-once pattern replacing the chained `previous * taper.Multiplier` formula. `taperMultipliers.Count == 1` case produces the exact legacy `changeRule` string and byte-identical arithmetic. |
| `VolumeProgressionVerifier.Verify` | Public signature **unchanged** (2 parameters, confirmed by existing reflection test). Internal taper-transition logic rewritten to use the same capture-once pre-taper-anchor pattern plus `policy.ResolvedTaperVolumeMultipliers`, replacing the chained `from.PlannedWeeklyVolumeKm * policy.TaperVolumeMultiplier` formula. `multipliers.Count == 1` case is byte-identical. |
| `TaperVolumeDecision` (`CatalogVolumeContracts.cs`) | **Not modified.** Deliberately avoided adding a collection-typed field here: record-generated equality on an `IReadOnlyList<double>` field would use reference equality (arrays/lists don't override `Equals`), which would have broken the existing `Assert.Equal(before.WeeklyVolumePlan.TaperVolumeDecision, after.WeeklyVolumePlan.TaperVolumeDecision)` equivalence test in `Phase4G3B0VolumeSafetyPolicyEquivalenceTests.cs` the moment two independently-computed decisions held two separately-allocated (if value-identical) list instances. The full multiplier list is instead threaded as a second return value / extra method parameter — plumbing internal to the same class, never exposed on an equality-checked contract. |

---

## 4. Zero-delta proof for every existing 10K 1-week taper

Real tests, not narrative (`backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm14bTaperVolumeMultiplierMechanismTests.cs`):

- `EveryExisting10KPolicy_ResolvedTaperVolumeMultipliers_IsSingleElementListOfItsOwnScalar_ZeroDelta` (12 cases — every named 10K `VolumeSafetyPolicy` instance: `Default`, `ThreeDayIntermediate`, `FiveDayIntermediate`, `SixDayIntermediate`, `Advanced3D`–`Advanced6D`, `Beginner2D`, `Intermediate2D`, `ThreeDayBeginner`, `BeginnerFourDay`) — confirms `ResolvedTaperVolumeMultipliers` is the single-element `[TaperVolumeMultiplier]`, value `0.53`, for every one.
- `LiveTwelveWeekTenKPilot_TaperWeek_IsExactlyTwentyKm_ByteIdenticalToPreHm14bBehavior` — runs the real, full `CatalogVolumeAndLongRunPlanner().Build(...)` pipeline against the same live 12-week TEN_K pilot fixture `Phase4F7BVolumeAndLongRunTests`/`Phase4G3B0VolumeSafetyPolicyEquivalenceTests` already exercise (`RecentWeeklyVolumeKm=24`, `VolumeSafetyPolicy.Default`) and asserts the taper week's `PlannedWeeklyVolumeKm` is exactly `20.0km` (matching `HM.1.4A`'s own cited "38km to 20km" trace) **and** that its `DecisionTrace.ChangeRule` is the exact legacy string `"canonical_taper_multiplier_0.53_from_previous_week"` — proving not just the number but the full decision-trace text is unchanged.
- Full regression (§6 below) re-confirms every pre-existing test in both suites — including `Phase4F7BVolumeAndLongRunTests.TaperMultiplier_IsCanonicalAndNotSupersededValue`, `Phase4G3B0VolumeSafetyPolicyEquivalenceTests` (all 3 tests, including the whole-record `TaperVolumeDecision` equality assertion), and every `VolumeProgressionVerifierTests`/`LongRunProgressionVerifierTests` taper-related case — passes unchanged.

## 5. `HALF_MARATHON × INTERMEDIATE × 4D × 14W` — real, non-compounded 2-week taper

`HalfMarathonIntermediate4D14W_TwoWeekTaper_ProducesTwoDistinctNonCompoundedTargets` builds a full, real (dark-only, test-local, never reachable through any identity gate) `CatalogVolumeAndLongRunPlanner` pipeline for a synthetic 14-week HM candidate with the frozen `3/4/5/2` phase split and a test-local `VolumeSafetyPolicy` populated with every already-frozen field for this cell (`ResolvedPeakReferenceKm=43.0`, `GoldenFixtureStartingVolumeKm=25.0`, `GoldenFixtureNonTaperTransitions=11`, growth rates `0.07/0.08/2.5`) plus `TaperVolumeMultipliers=[0.70,0.43]`. Confirmed real output:

```
Week 12 (RaceSpecific, last non-taper) = 43.0 km   (exactly the frozen pre-taper reference, by construction)
Week 13 (Taper 1/2)                    = 30.0 km   (43.0 × 0.70, independently computed)
Week 14 (Taper 2/2)                    = 18.5 km   (43.0 × 0.43, independently computed — NOT chained from Week 13's 30.0)
Cumulative reduction 43.0 → 18.5        = 57.0%     (within the 41-60% evidence envelope, NOT the confirmed 72.1% compounding defect)
```

A companion test (`TaperMultipliers_NonMonotonicallyDeepening_ThrowsTypedException`) confirms an inverted schedule (e.g. `[0.30, 0.43]`, where the first week would cut deeper than race week) fails closed with a typed exception, and `TaperMultiplierCount_MismatchedAgainstActualTaperWeekCount_ThrowsTypedException` confirms a multiplier-count/taper-week-count mismatch also fails closed — the new validation is real, not decorative.

## 6. `VolumeProgressionVerifier` fix confirmation — no false negative

- `VolumeProgressionVerifier_CorrectlyValidatesGenuineNonChainedTwoWeekTaper_NoFalseNegative` — feeds the verifier a synthetic plan with the exact correct, non-chained HM values (43.0 → 30.0 → 18.5) and confirms `Outcome == Pass`, zero findings.
- `VolumeProgressionVerifier_FlagsTheExactHm14aCompoundingDefect_NoLongerBlind` — feeds the verifier a synthetic plan reproducing the specific chained/compounded value a buggy re-implementation would produce for week 14 (`30.0 × 0.43 ≈ 13.0`, i.e. chaining off week 13's own output instead of the fixed 43.0 reference) and confirms the verifier now reports `Outcome == Fail` with a `TAPER_MULTIPLIER_VIOLATION` finding naming `week 13->14` — directly proving the exact blind spot `HM.1.4A §3.2` found (*"It would report zero violation for this exact 2-week compounding scenario"*) is closed.

---

## 7. Recurring-assumption-family search

Beyond the two consumers `HM.1.4A` already named (`BuildWeeklyPlan`, `VolumeProgressionVerifier`), grepped every reference to `TaperVolumeMultiplier`/`TaperVolumeDecision` in the repository:

- **`Phase4G5GPrerequisiteCrossChecksTests.cs:168-172`** — a **test-only** assertion (`expectedTaperKm = precedingWeek.PlannedWeeklyVolumeKm * VolumeSafetyPolicy.Default.TaperVolumeMultiplier`) that shares the identical chained-formula shape. Confirmed this needs **no change**: it only ever exercises `VolumeSafetyPolicy.Default` (a 1-week-taper policy), and for `N==1` the chained formula and the new non-chained formula are algebraically and bit-for-bit identical (both reduce to `precedingWeek.PlannedWeeklyVolumeKm * TaperVolumeMultiplier`, since `precedingWeek` IS the pre-taper anchor for a 1-week taper) — re-confirmed passing unchanged in the full regression (§8).
- **`LongRunProgressionVerifier.cs`** — traced in full. Has no taper-specific branch at all; a taper week's long run is computed by the exact same `weeklyVolume × SelectionShare`-then-clamp formula as every other week, deriving its reduction transitively from the already-materialized (and now correctly non-chained) `PlannedWeeklyVolumeKm`. **No chaining assumption present, no change needed** — confirmed by direct trace, not assumed.
- **`LongHorizonGeNumericExecutor.cs`** — grepped for `Taper`/`TaperVolumeMultiplier`: zero matches. Confirmed no consumer here.
- **`SafetyVerificationOrchestrator.cs`** — grepped for `Taper`/`TaperVolumeMultiplier`: zero matches (only forwards `VolumeProgressionVerifier.Verify`/`LongRunProgressionVerifier.Verify` calls, already covered above).

No fourth production or test consumer of the chaining assumption was found.

---

## 8. Required verification — full regression, both suites

**Reliable process-check method used** (per this engagement's standing convention): unfiltered `tasklist` grepped for `testhost.exe` before and between runs — confirmed no lingering test-host process at each checkpoint, and the two full-suite runs (`RunningApp.IntegrationTests`, `PlanCatalog.Tests`) were never launched concurrently (the second was started only after the first's background process had exited and `tasklist` re-confirmed zero `testhost.exe` processes).

**`RunningApp.IntegrationTests` — 4477 passed / 3 failed / 4480 total** (40m20s). The 3 failures are exactly this engagement's documented baseline, by name and reason, unchanged:
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13, 14) — pre-existing, unrelated to this phase.
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — pre-existing, unrelated to this phase.

The total grew from the documented `4460` baseline to `4480` — exactly the 20 new tests this phase added (`Hm14bTaperVolumeMultiplierMechanismTests`), all 20 passing. `4460 − 3 = 4457` (documented baseline pass count) and `4480 − 3 = 4477` (this run's pass count) confirms **zero new regressions**: every test that passed before this phase still passes, and no new failure was introduced anywhere in either suite.

**`PlanCatalog.Tests` — 1510 passed / 0 failed / 1510 total** (6s) — byte-identical to the documented baseline, unaffected by this phase (this phase touches no `plan-catalog` code).

---

## 9. What this phase explicitly did NOT do

- Did not touch any 2D/3D+ 10K numeric authority or structure beyond the minimum needed to add multi-week taper support (every existing named `VolumeSafetyPolicy` instance is byte-for-byte unchanged; only one new optional trailing field was added to the shared record).
- Did not construct a new, full `VolumeSafetyPolicy.HalfMarathon...` static instance — 4 of 13 fields remain genuinely unfrozen for this cell (`HM.1.5 §3`), and inventing values for them was out of scope and explicitly forbidden.
- Did not decide or alter HM's taper *duration* (still the `HM.1`-frozen 2-week Taper phase, `3/4/5/2` split, unchanged).
- Did not author any HM catalog artifact, widen any public routing/gate surface, or touch 10K's authority, code, or catalog beyond the shared mechanism fix.
- Did not spawn any sub-agent — all investigation, implementation, and verification performed directly.

---

## 10. Final classification

```
HM_TWO_WEEK_TAPER_MECHANISM_FIXED_AND_VALUES_FROZEN_ZERO_DELTA_CONFIRMED
```

The compounding defect `HM.1.4A` confirmed (43→22.8/23.0→12.1/12.0km, ≈72.1% cumulative reduction) is closed at the mechanism level: `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` and `VolumeProgressionVerifier` both now compute every taper week independently against a single, fixed, capture-once pre-taper anchor, never chaining from another taper week's own output. The two HM taper-week values (`TaperWeek1VolumeMultiplier=0.70`, `TaperWeek2VolumeMultiplier=0.43`) are frozen, `EVIDENCE_INFORMED_PRODUCT_DEFAULT` with multi-source external convergence, in a new dedicated `HalfMarathonIntermediate4DTaperVolumePolicy` class — not wrapped in a new full `VolumeSafetyPolicy` instance, since 4 of that record's 13 fields remain genuinely unfrozen for this cell. Zero delta for every existing 10K 1-week taper is proven by direct test, not narrative, at both the unit (`ResolvedTaperVolumeMultipliers`) and full-pipeline (`Build()`, exact km value, exact decision-trace string) levels. `VolumeProgressionVerifier`'s identical blind spot is fixed in the same pass and proven to no longer produce a false negative for the exact defect shape `HM.1.4A` found.
