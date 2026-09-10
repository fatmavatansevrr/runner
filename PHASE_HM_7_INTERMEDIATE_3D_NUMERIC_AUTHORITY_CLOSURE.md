# PHASE HM.7 — Half-Marathon Intermediate 3D Numeric Authority Closure

Governance / product-decision phase. No production code, no catalog file, no schema was modified by this phase. One explicit product-judgment call (the long-run-share magnitude, §8) was escalated to and answered by the user mid-phase, per this engagement's established pattern of using AskUserQuestion for genuinely contested product calls rather than unilaterally freezing them.

## 1. HM.6 gap set

`PHASE_HM_6_INTERMEDIATE_3D_5D_FREQUENCY_EXPANSION_AUTHORITY_AND_REUSE_AUDIT.md` listed 10 unresolved numeric fields for 3D: selected/reference peak weekly volume; preferred/hard weekly increase rate; absolute weekly increase cap; long-run preferred-share min/max; long-run selected share; long-run hard-share cap; taper week-1/week-2 multiplier. This phase also independently re-checked whether HM.6's list was exhaustive (§9 below) and found two additional items requiring explicit scope determination: calibration-only starting volume, and absolute-peak-long-run-ceiling scope.

## 2. Source inventory

Read directly (not from memory): `HM_21K_Claude_Handoff_and_Build_Plan.md` in full relevant sections (§4.1–4.3, §6 open items); `PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md`; `PHASE_HM_1_4_...md`; `PHASE_HM_1_5_...md`; `PHASE_HM_1_4B_...md`; `PHASE_HM_4_...md`; `PHASE_HM_6_...md`; the real `VolumeSafetyPolicy.cs` (all 12 named 10K instances plus `HalfMarathonIntermediate4D`); `CatalogVolumeAndLongRunPlanner.cs`'s `ResolvePeak`/`BuildLongRunPlan`. External research (two independent real-world 3-day-per-week half-marathon program sources) was used only where internal Appsel evidence was silent (§8), per this phase's own §19 rule.

## 3. Existing-authority vs source-silence findings

- `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2's peak-volume band table (3D/4D/5D columns) is real and frequency-scoped by the document's own structure — confirms peak-volume bands are frequency-specific data.
- §4.3's starting-long-run-cap table (Beginner/Intermediate/Advanced/Experienced rows, **no frequency column at all**) confirms this authority is frequency-independent by the document's own structure, not by inference.
- The document's own "long-run share/cap" mentions (lines 121, 784, 794) are bare category placeholders in an open-items list — **no percentage figures for any frequency exist anywhere in the internal handoff document**, confirming HM.6's own finding and extending it: even 4D's own 0.30/0.36/0.33/0.40 values did not come from this document.
- Tracing where 4D's long-run-share numbers actually came from: `PHASE_HM_1_5` cites a Hal Higdon Intermediate 1 program (a specific, single, external real-world citation introduced directly in the user's own decision message, not derived from internal synthesis) as directional support for a *rising peak-week share* — this is real-world program evidence, not internal Appsel authority, and (per Higdon's own published structure) that program's frequency is not universally 4-day. This is recorded honestly: 4D's own long-run-share freeze rests on the same class of evidence (external real-world convergence) this phase now uses for 3D, not a fundamentally stronger internal-authority source.

## 4. Peak-volume band status

`Intermediate 3D: 28–40 km/week` — re-verified directly at `HM_21K_Claude_Handoff_and_Build_Plan.md:165`, same table/section as 4D's `36–50` and 5D's `44–58`. Classification: **CURRENT_FROZEN_AUTHORITY** (the band itself, as an allowed envelope) — this is the identical source and section HM.1.4 used to freeze 4D's own band, so the same evidentiary standing applies to 3D's row in the same table. This is the *band*, not the *selected peak* — the selected peak remains a separate, unresolved decision (§5).

## 5. Selected peak decision

**`SelectedPeakReferenceKmPerWeek = 34.0`**

Rationale: 4D's own already-frozen `ResolvedPeakReferenceKm = 43.0` is *exactly* the arithmetic midpoint of its own band `[36,50]` (`(36+50)/2 = 43`). This is not stated as an explicit "midpoint-as-policy" rule anywhere in governance text, but it is the real, observable pattern in the one precedent this engagement has actually frozen. Applying the same observed pattern to 3D's own band `[28,40]` gives `(28+40)/2 = 34.0`. This is **methodology reuse from a real, on-point precedent**, not "midpoint chosen because it's convenient" — the precedent is that Appsel's one existing HM peak-selection decision landed exactly on the band midpoint, and no evidence anywhere argues for a different point within 3D's own band.

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (methodology-precedented, not physiologically derived).

Feasibility cross-check (§9 of the governing prompt): at 34.0 km/week, even the hard long-run-share cap (§8, 0.46) yields `34 × 0.46 = 15.64 → 15.5km` — comfortably below the 19.0km ceiling, with no forcing in either direction. At the band's own upper bound (40 km/week, reachable only by users with strong starting evidence), the hard cap yields `40 × 0.46 = 18.4 → 18.5km` — still below 19.0km. The ceiling is never required to bind, consistent with its own ceiling-not-target semantic.

## 6. Weekly progression decision

**`PreferredWeeklyIncreaseRate = 0.07`, `HardWeeklyIncreaseRate = 0.08`** — reused directly, frequency-independent. Verified by direct read of all 12 named 10K `VolumeSafetyPolicy` instances plus `HalfMarathonIntermediate4D`: **every single one**, across every level and every frequency (2D through 6D, Beginner through Advanced), uses exactly `0.07`/`0.08`. This is a real, universal, already-established Appsel convention — not a value special to 4D. Classification: `DIRECT_EXISTING_AUTHORITY` (reused as the generic safety-guard convention it already, verifiably, is).

**`AbsoluteWeeklyIncreaseCapKm = 2.0`** — **NOT reused from 4D's 2.5.** Direct verification of the same 12 named 10K instances shows this field genuinely varies: `2.0` for `Beginner2D`, `Intermediate2D`, `ThreeDayBeginner`, **and `ThreeDayIntermediate`**; `2.5` for `Default` (4D), `FiveDayIntermediate`, `SixDayIntermediate`, `Advanced3D–6D`, `BeginnerFourDay`. The single most directly on-point 10K precedent — `ThreeDayIntermediate`, the exact same Level×Frequency combination this phase is deciding for, just at the 10K distance — uses `2.0`, not 4D's `2.5`. This is genuine, on-point precedent reuse (same level, same frequency, different distance — a distance-generic mechanism-and-methodology reuse, not numeric inheritance across frequency). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 7. Calibration/start-volume audit

**`GoldenFixtureNonTaperTransitions = 11`** — reused directly, zero new decision. This value is not a physiological constant; it is `DIRECT_STRUCTURAL_DERIVATION` from HM's own already-frozen, frequency-generic phase structure (`3 Foundation + 4 Build + 5 RaceSpecific = 12` non-taper weeks → `11` transitions, per `CatalogVolumeAndLongRunPlanner.cs`'s own `nonTaperWeeks - 1` formula, confirmed unchanged and frequency-generic per HM.4/HM.6). Classification: `DIRECT_EXISTING_AUTHORITY`.

**`GoldenFixtureStartingVolumeKm = 19.8`** (rounds `34.0 × (25.0/43.0) = 19.77`, rounded to the nearest 0.5km increment used elsewhere in this policy family → `20.0`). Rationale: this is explicitly calibration-only (feeds `ResolvePeak`'s `canonicalDefaultMultiplier` denominator exclusively — never a user-facing starting-volume fallback, never a readiness threshold). The real, established Appsel methodology for deriving a *new* policy's calibration start (confirmed by direct precedent: 10K's `Advanced3D`/`Advanced4D`/`Advanced5D`/`Advanced6D` doc comments, e.g. `Advanced3D` explicitly "reuses `ThreeDayIntermediate`'s own already-proven `GoldenFixtureStartingVolumeKm`-to-`ResolvedPeakReference` ratio (12/22.5 = 0.5333), applied to Advanced's own approved peak reference") is to reuse an existing sibling policy's own calibration ratio, not invent a new one or copy the absolute value. Applying the identical methodology here: reuse `HalfMarathonIntermediate4D`'s own ratio (`25.0/43.0 = 0.5814`) against 3D's own selected peak (`34.0`) → `19.77`, rounded to **`20.0`**. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (methodology-precedented via a real, cited 10K convention).

**Explicit guardrail, recorded prominently per this engagement's own established practice**: `20.0` is calibration-only. It must never become a missing-readiness fallback, a user-facing default, or an HM readiness threshold. Explicit-zero and missing-readiness remain fail-closed exactly as already established for 4D.

## 8. Long-run share evidence

Internal Appsel evidence is silent for 3D specifically (§3). External research was used per §19's own explicit permission. Two independent real-world 3-day-per-week half-marathon program sources were found:

- **Marathon Handbook's 3-day HM plan** (peak week 11: 7mi easy + ~4-5mi tempo + 12mi long run ≈ 23-24mi total): long run ≈ **50%** of weekly volume at peak.
- **"Run Less, Run Faster" (the FIRST method, Pierce/Murr/Moss)**, the most-cited 3-day-a-week training framework: "weekly long run constitutes **60 to 70%** of total weekly mileage."

Both independently converge on the *direction* (3D's long run structurally represents a much larger fraction of weekly volume than 4D's, because only 3 sessions exist total) and roughly on *magnitude* (50–70% range), though the two sources' precise numbers differ (not mechanically averaged, per §19's own prohibition). A third, real, **directly on-point internal precedent** was also found: 10K's own `ThreeDayIntermediate` policy (`LongRunPreferredMinimumShare=0.38, Max=0.42, SelectionShare=0.40, HardCapShare=0.42`) already runs meaningfully higher long-run shares than `Default`'s 4D-equivalent (`0.30/0.36/0.33/0.40`) — the same directional pattern, from Appsel's own real, shipped 10K product, for the exact same Level×Frequency combination this phase concerns (differing only in distance).

## 9. Long-run share decision — escalated to and resolved by explicit user decision

Given the scale of departure from 4D's conservative philosophy that the real-world evidence implies (50–70% vs 4D's 33%/40%), this was presented to the user as a genuine product-judgment call rather than unilaterally frozen. **User decision: moderate uplift, still conservative — ordinary target in the ~38–40% range, hard cap in the ~45–48% range, explicitly not adopting the full real-world extreme.**

Frozen:

```
PreferredLongRunShareMin = 0.34
PreferredLongRunShareMax = 0.40
SelectedLongRunShare     = 0.38
HardLongRunShareCap      = 0.46
```

Classification: **`PRODUCT_DEFAULT`** (explicitly, per the user's own instruction) — informed by (a) two independent real-world 3-day program sources confirming the directional/rough-magnitude pattern, (b) 10K's own real `ThreeDayIntermediate` vs `Default` convention showing the identical directional pattern as decision methodology (not numeric inheritance across frequency, and not numeric inheritance across distance either — the actual 3D values chosen are HM's own, informed by but not copied from 10K's numbers), (c) an explicit user product decision on how much of the real-world magnitude to adopt. This is **not** claimed as `EVIDENCE_BACKED` or `STRONG_EVIDENCE_DERIVED_DECISION` — no causal/scientific evidence establishes these exact percentages as optimal for HM 3D; this is a real-world-informed, safety-conscious product default.

Preserved semantic (identical to 4D, per HM.1.6): `SelectedLongRunShare` is the **ordinary weekly target**, not a hard cap; `HardLongRunShareCap` is a **safety boundary**, not the everyday target; a late RaceSpecific week may rise above `SelectedLongRunShare` toward `HardLongRunShareCap` via the existing, unmodified HM.1.6 mechanism, never past it.

## 10. Absolute peak-LR scope decision

**`PreferredAbsolutePeakLongRunKm = 19.0`, scope broadened to `HALF_MARATHON × INTERMEDIATE` (frequency-independent).**

Rationale, re-derived from `PHASE_HM_1_1`'s own original evidence base (re-read directly, not assumed): the four real-world programs that produced 19.0km were **Hal Higdon Intermediate 1** (12mi≈19.3km), **Hal Higdon Intermediate 2** (12mi≈19.3km, *explicitly a 5-day-running program*), **ASICS first-HM/beginner plan** (18km), and **B.A.A. Level Two** (up to 11–12mi≈17.7–19.3km). This evidence base **already spans multiple weekly frequencies** (Higdon Intermediate 2 is explicitly 5-day) converging on the *same peak long-run distance* — meaning the peak-long-run-distance ceiling itself was never actually frequency-narrow evidence; `HM.1.1`'s own `4D × PREFERRED-14W ONLY` scope restriction (its §"Scope" note) was a deliberate, cautious methodological choice (never generalize without deliberate re-confirmation), not a reflection that the underlying evidence was 4D-specific. Re-confirming that evidence now, for the express purpose HM.1.1 itself anticipated ("this decision does not build a Distance×Level×Frequency peak-long-run table for any cell beyond Intermediate×4D×14W" — implying a *future* phase, this one, might), broadening to `HALF_MARATHON × INTERMEDIATE` (still scoped by distance and level, not further broadened to Beginner/Advanced or Marathon) is justified.

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (same evidence base as HM.1.1, now correctly interpreted at its true frequency-general scope).

Preserved invariant, unchanged: **19.0km remains a ceiling, never a mandatory target.** §5's feasibility check already confirms 3D's own hard-share-cap-derived long run (≤18.5km even at the band's upper bound) never requires or forces this ceiling to bind.

## 11. Starting-LR scope decision

**`StartingAbsoluteLongRunCap = 12.0 km`, reused directly, frequency-independent.**

`HM_21K_Claude_Handoff_and_Build_Plan.md` §4.3's starting-long-run-cap table has rows only for Beginner/New, Intermediate, Advanced/Experienced — **no frequency column exists in this table at all**, structurally confirming this authority was never frequency-scoped to begin with. Classification: `DIRECT_EXISTING_AUTHORITY`. User runtime evidence (recent weekly volume, recent longest run) remains the actual basis for a real request's starting point; this cap is a ceiling on that evidence, not a substitute for it. Explicit-zero/missing-readiness remains fail-closed, unchanged.

## 12. Taper authority scope decision

**`TaperWeek1VolumeMultiplier = 0.70`, `TaperWeek2VolumeMultiplier = 0.43`, reused directly, scope = `HALF_MARATHON × INTERMEDIATE` (frequency-independent).**

Re-read `PHASE_HM_1_4B`'s own four evidence sources directly: Mujika & Padilla meta-analyses (general taper-volume-reduction science, no frequency dimension), Marathon Handbook's HM taper guide (general weekly-volume-reduction percentages, no frequency dimension), best-running-tips.com (`40-60% standard, lighter-taper exception only above ~56km/week` — the deciding variable named is **weekly volume**, not running frequency), runtothefinish.com (general). None of the four sources that produced 0.70/0.43 are frequency-scoped — they describe volume-reduction as a function of total training load and race proximity, not session count. 3D's own peak volume (34.0, band max 40.0) sits well below the `~56km/week` lighter-taper-exception threshold, so no exception applies — the same taper-depth regime holds. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (reused with proven, re-verified scope, not blind copy).

Taper duration remains frozen at 2 weeks (unchanged, not reopened). Non-chained mechanism (fixed pre-taper anchor, both weeks derive independently) is generic and already proven — no 3D-specific change required.

## 13. Session-level feasibility

Simulated (by hand, against the real, unmodified generic mechanisms — no production code touched) a representative preferred-14W 3D week at peak volume 34.0km: ordinary long run ≈`34×0.38=12.92→13.0km`; remaining `34-13=21.0km` split across KEY+EASY, comfortably above every existing minimum session-distance floor this engagement has established for 3D-style layouts elsewhere (e.g. `V1ThreeDaySessionVolumeAllocationPolicy.MinimumKeyKm=4.0`/`MinimumEasyKm=3.0` cited in `Gen23BeginnerThreeDayCoreTests`) — no negative residual, no session-minimum violation. Taper week (anchor × 0.70 or × 0.43) similarly leaves a representable KEY+EASY+LONG split at the reduced volume, matching the same generic session-allocation shape 4D already proves. **No capability blocker found.** RUN_LAYOUT_3D's own KEY_SESSION lane binds `TAPER_HM_ACTIVATION` (or its fallback) exactly as 4D's own KEY lane does — HM.6 already confirmed this structural compatibility; this phase's numeric authority does not change that finding.

## 14. Exact 10–16W simulated numeric traces

Using the already-frozen, frequency-generic phase allocations (`10W→2/3/3/2` … `16W→4/5/5/2`, unchanged, not reopened) and this phase's newly-frozen 3D numeric authority, hand-traced (not executed — no production code was run for this pure-governance phase) against the same `ResolvePeak`/`BuildLongRunPlan` formulas already proven generic by HM.5.3:

- **10W–13W** (fewer non-taper transitions than the 11-transition calibration point): the interpolation formula's transition-capped-or-not distinction (HM.5.3's `ResolvedPeakReferenceIsSelectedCeiling`) applies identically — shorter horizons resolve a peak *below* 34.0km, proportional to their own transition count, never forced upward to chase it. No contradiction.
- **14W** (exactly 11 transitions, the calibration point): resolves to exactly `34.0 × (34.0/19.8) × ... ` — by construction, `canonicalDefaultMultiplier = 34.0/19.8 = 1.717`, and at exactly 11/11 transitions the formula reduces to the calibration ratio itself, `startingVolumeKm × 1.717`. This is the same clean, self-consistent, no-residual-scaling pattern HM.1.5 already proved for 4D's own 14W case.
- **15W–16W** (more transitions than calibration): with `ResolvedPeakReferenceIsSelectedCeiling = true` for this policy (reused directly from the generic HM.5.3 mechanism — the same opt-in flag closes this for 3D automatically, zero new code, zero new decision), the transition count caps at 11, so the multiplier saturates at `1.717` rather than extrapolating past it — `reachable` never exceeds `34.0km` for any horizon, matching the exact invariant HM.5.3 already proved generically. **No new mechanism, no new number, no horizon-specific table required** — this is exactly the "existing generic mechanism + new frequency policy data" shape this whole HM.6/HM.7 line of work is designed to produce.
- Long-run share guards hold identically at every horizon (§9's semantic, §5's feasibility bound already checked at the band extremes).
- Taper remains non-chained at every horizon (generic mechanism, unchanged).

## 15. Pace/fallback compatibility

No 3D numeric policy field depends on exact pace evidence. `HM_PACE`/fallback eligibility (`GOAL_FEASIBILITY_IN`/`PACE_SOURCE_IN` conditions) are stage-progression-level concerns (HM.6 already confirmed HM's workout progression is structurally frequency-compatible for 3D's single-KEY-lane shape) — entirely independent of this phase's volume/long-run/taper numeric authority. No design error found.

## 16. Calendar/adaptation reuse

`PreferredDays` cardinality=3, `LongRunDayPreference`, KEY↔LONG spacing, EASY adjacency, mid-week `StartDate` handling: all generic, frequency-dispatched by `RunLayout` alone, confirmed unchanged and requiring no numeric-authority decision (HM.6's own finding, re-confirmed not reopened). No 3D-specific adaptation percentage was created, per this phase's own explicit prohibition.

## 17. Complete authority table

| Authority | Value | Classification | Scope |
|---|---|---|---|
| PeakVolumeBand | 28–40 km/week | `CURRENT_FROZEN_AUTHORITY` | HM × Intermediate × 3D |
| SelectedPeakReference | 34.0 km/week | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| PreferredWeeklyIncreaseRate | 0.07 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| HardWeeklyIncreaseRate | 0.08 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| AbsoluteWeeklyIncreaseCapKm | 2.0 km | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| CalibrationStartingVolume (GoldenFixtureStartingVolumeKm) | 20.0 km (calibration-only) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| GoldenFixtureNonTaperTransitions | 11 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| TaperWeek1Multiplier | 0.70 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate (frequency-independent) |
| TaperWeek2Multiplier | 0.43 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate (frequency-independent) |
| LR PreferredMin | 0.34 | `PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| LR PreferredMax | 0.40 | `PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| LR Selected | 0.38 | `PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| LR HardCap | 0.46 | `PRODUCT_DEFAULT` | HM × Intermediate × 3D |
| AbsolutePeakLongRunKm | 19.0 km (ceiling, not target) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate (frequency-independent) |
| StartingAbsoluteLongRunCap | 12.0 km | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| ResolvedPeakReferenceIsSelectedCeiling | true | `DIRECT_EXISTING_AUTHORITY` (generic HM.5.3 mechanism, reused) | HM × Intermediate × 3D |
| Session allocation | Generic (KEY/EASY/LONG minimums) | `GENERIC_REUSE` | Frequency-generic |
| Hard-session count | 1 (KEY only; LONG remains easy) | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate × 3D |
| Pace/Fallback | Unchanged | `GENERIC_REUSE` | Distance-generic |
| Adaptation | Unchanged | `GENERIC_REUSE` | Frequency-generic |
| Calendar | Unchanged | `GENERIC_REUSE` | Frequency-generic |
| Public eligibility | Unchanged (dark) | `NOT_APPLICABLE` (out of scope) | N/A |

## 18. Evidence/product-default classifications — summary

No value in this table is classified `EVIDENCE_BACKED` or claimed as scientifically optimal. Two fields are `DIRECT_EXISTING_AUTHORITY` reused with zero new decision (weekly progression rates, starting-LR cap, calibration transition count, the generic ceiling-flag mechanism). Five fields are `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, each citing a specific, real, on-point precedent (10K's own `ThreeDayIntermediate` for the absolute weekly cap; HM 4D's own observed midpoint-selection pattern for the peak reference; the established Advanced-policy ratio-reuse convention for calibration start; HM.1.4B's own frequency-blind evidence sources for taper; HM.1.1's own multi-frequency-convergent evidence for the absolute LR ceiling). Four fields (the long-run-share quadruple) are honestly classified `PRODUCT_DEFAULT`, reflecting a genuine, disclosed, user-confirmed judgment call rather than a stronger evidentiary tier.

## 19. Remaining gaps

None blocking this cell. `HALF_MARATHON__3D__INTERMEDIATE` catalog combination artifacts (master/run-layout/progression/numeric-policy wiring) were not created — this phase is explicitly governance-only, per its own scope. 5D's second-KEY-lane authority gap (HM.6) remains untouched and out of scope, per this phase's own explicit prohibition (§24 of the governing prompt).

## 20. Recommended implementation phase

A future `HM.8` (or equivalently-numbered) implementation phase would: (1) add a new named `HalfMarathonIntermediate3D` `VolumeSafetyPolicy` instance (and a corresponding `HalfMarathonIntermediate3DTaperVolumePolicy`-style narrow class, mirroring the existing 4D convention) encoding exactly this report's frozen table; (2) determine catalog lifecycle/versioning for `HALF_MARATHON__3D__INTERMEDIATE` (likely a new combination record referencing the *existing*, unmodified `HALF_MARATHON_MASTER` and `HALF_MARATHON_WORKOUT_PROGRESSION`, plus the existing `RUN_LAYOUT_3D`, per HM.6's own recommended single-master architecture); (3) run the full 10–16W dark-materialization proof (mirroring HM.5.1/HM.5.3's own established test shape) against this new policy; (4) prove 10K zero-delta exactly as every prior HM.5.x phase has. HM remains dark and non-public throughout.

## Final classification

`HM_INTERMEDIATE_3D_NUMERIC_AUTHORITY_CLOSED`
