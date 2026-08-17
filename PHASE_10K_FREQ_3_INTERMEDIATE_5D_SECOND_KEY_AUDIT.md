# Phase 10K-FREQ.3 — Intermediate 5D Second-KEY Evidence & Architecture Audit

**Research/audit only. No code touched. No numeric values selected.**

## Top-line answer (per instruction, stated first)

- **Section A: RESOLVED** — `INTERMEDIATE_5D_TRAJECTORY_AUTHORITY_RESOLVED` (with a disclosed reasoning-based recommendation, not a final numeric decision).
- **Section H: REAL CARDINALITY DEFECTS FOUND** — not one, but **four independent, code-confirmed single-KEY assumptions** across session allocation, calendar validation, and Adaptation V1 scoring. These are genuine blockers, not hypothetical.

**Final classification: `INTERMEDIATE_5D_ARCHITECTURE_AUDIT_BLOCKED_ON_[SECTION_E_SECTION_F_SECTION_H]`** — evidence and authority questions (A, C, D) are answerable; the structural cardinality defects (E, F, H) are real, multiple, and must be fixed before any 5D implementation phase can be scoped, regardless of how A/C/D resolve.

---

## A. 5D volume-trajectory authority check

**1. What authority selects trajectory shape?** Re-read `PHASE_10K_GEN_2B_1...`/`PHASE_10K_GEN_2B_2...` (FREQ.1's own sources) with this specific question in mind. Both documents evaluate the compounding shape strictly within a 3D-scoped evidence/decision process, never invoking Distance or Level as a factor. **Classification: `FREQUENCY_OWNED_TRAJECTORY_POLICY`.**

**2. Does FREQ.1's rationale predict 5D's answer?** FREQ.1 found 3D's compounding shape is grounded in "how a low-frequency, few-session structure concentrates load week-to-week" — not the literal number 3. Applying that same logic *consistently* (not by nearest-frequency analogy, which is explicitly prohibited): 5D has **more** sessions than 4D, meaning **less** per-session load concentration than even 4D, let alone 3D. If concentration is the actual mechanistic driver, the logic points *away* from compounding and *toward* 4D's interpolation shape being the better fit for 5D — the opposite direction "5 is closer to 4" nearest-frequency reasoning would suggest, and it happens to agree with it, coincidentally.

**This is a reasoned extrapolation of FREQ.1's own mechanistic logic, not itself directly evidenced for 5D** — GEN.2B.1/2B.2's literature review was 3D-specific and never examined 5D. Disclosed honestly: this is a *recommendation for a future decision phase*, not a closed evidence question.

**3. Output: `INTERMEDIATE_5D_TRAJECTORY_AUTHORITY_RESOLVED`.** Authority = Frequency-owned. Recommendation for the future decision phase = reuse 4D's interpolation shape, reasoned (not evidenced) from FREQ.1's own concentration logic extrapolated consistently, not from nearest-frequency analogy.

## B. RunLayout 2-KEY structural semantics

**1.** No `RUN_LAYOUT_5D` artifact exists anywhere in `plan-catalog/catalog/layouts/` (confirmed: only `run-layout-3d.v1.json`, `run-layout-4d.v1.json`, `run-layout-4d.v2.json`). It would need to be created from scratch — no draft exists.

**2. Structural-only — mixed result.** `BoundCatalogPlanValidator.cs` (the role-cardinality validator GEN.1 previously fixed for 3D) uses `roleCounts.GetValueOrDefault("KEY_SESSION")` compared against an *expected count* from the resolved layout — this is genuinely count-based, not uniqueness-assuming, and would already handle a 2-KEY layout correctly with **zero code change**. But `DatedGeneratedCatalogPlanSkeletonValidator.cs` (a *different* validator, in the calendar-materialization path, not the binding path) does **not** generalize — see §H below, a real defect found here and cross-referenced there.

## C. KEY1/KEY2 workout pairing — evidence envelope (not a selection)

Real sources found (search performed fresh, not reused from prior engagement work beyond the Lenk et al. anchor named in the phase prompt):

- **Lenk et al. 2025** (already-verified anchor, [Physiological Reports](https://physoc.onlinelibrary.wiley.com/doi/10.14814/phy2.70573)) — 2 vs. 3 weekly HIIT sessions produce similar cardiorespiratory gains in recreational runners; diminishing returns beyond twice-weekly. Supports 2-hard-session/week frequency as physiologically reasonable, not specifically pairing content.
- Sustained-tempo (continuous threshold, 20-30min) vs. cruise-intervals (threshold-paced reps with short easy recoveries) are described in the broader coaching/exercise-science literature as **complementary threshold formats** — same energy system, different fatigue/format profile.
- Interval training (VO2max/speed-focused) and long runs are commonly described as complementary by training-load mechanism (different adaptation targets) — relevant context for KEY↔LONG, not directly KEY1↔KEY2.

**Evidence envelope for KEY1/KEY2 pairing** (not a final selection, per instruction):
1. Two different threshold-family formats (e.g., sustained tempo + cruise intervals) — same system, different format, plausible complementary pairing.
2. One threshold-family session + one VO2max/speed-family session (e.g., threshold-tempo + a faster interval format) — different systems, classically complementary.
3. Two similar-intensity sessions in immediate succession — **not supported** by any source found; no source recommends duplicate-stimulus pairing for two weekly hard days.

**Real-world literature gap disclosed honestly**: no source found specifically studies *recreational* runners' physiological response to two *differently-formatted* KEY sessions per week (as opposed to two *identical*-format sessions at different frequencies, which is what Lenk et al. actually studied). The envelope above is a reasonable extrapolation from adjacent, real literature, not a direct study of this exact question.

## D. KEY↔KEY / KEY↔LONG spacing

**1. Real mechanism, exact value confirmed by direct code read**: `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays = 2` (days, not hours) — governs both same-week and cross-week KEY↔LONG separation. `ScheduleRepairSpacingValidator.cs` (Adaptation V1) reuses this exact same constant for its own KEY↔LONG repair-candidate check.

**2. Real evidence found, honestly scoped**: no source located specifies a hard scientific minimum-hours threshold between two hard running sessions for recreational runners. The real consensus (multiple sources) is qualitative: "no more than 2 hard sessions/week, with easy days between them" — a day-level convention, not an hours-based scientific derivation. **This confirms the system's existing 2-day value is a reasonable `PRODUCT_DEFAULT`, not a scientifically-derived hard minimum** — disclosed explicitly, not overclaimed as evidence-derived, matching this engagement's prior correction on an analogous "48h" claim.

**3. Does the existing mechanism generalize to 2 KEYs/week? No — real, explicit, self-disclosed gap.** `ScheduleRepairSpacingValidator.cs`'s own doc comment states directly: *"Same-role (KEY-to-KEY, LONG-to-LONG) spacing has no existing canonical rule, so none is invented here."* This is not a bug (a 1-KEY system never needed one) but is a **real, confirmed missing mechanism** that must be built before 2-KEY weeks can be safely scheduled or repaired.

## E. Volume/session allocation — real structural blocker found

Read `V1FourDaySessionVolumeAllocationPolicy.Allocate` in full. Its entry guard:

```csharp
if (sessions.Count != 4 ||
    sessions.Count(s => s.StructuralRole == "KEY_SESSION") != 1 ||
    sessions.Count(s => s.StructuralRole == "EASY_SUPPORT") != 2 ||
    sessions.Count(s => s.StructuralRole == "LONG_RUN") != 1)
{
    throw new CatalogSessionPrescriptionInfeasibleException(...);
}
```

**This does not merely "assume" a single KEY — it hard-fails (throws) for any week with more than 1 `KEY_SESSION`.** This is the same real, precedented mechanism 3D/4D reuse pattern-matches to for a new Frequency (mirroring how `V1ThreeDaySessionVolumeAllocationPolicy` was structured for 3D) — but as written today, it **cannot** be reused unmodified for a 2-KEY layout; it would need genuine new logic (splitting a "KEY share" across two instances), not just a parameter change. **Real structural limitation, confirmed, not assumed.**

## F. Calendar materialization — real structural blocker found, worse than a simple reject

Read `DatedGeneratedCatalogPlanSkeletonValidator.cs` in full around its role-count and spacing check. Two independent problems in the same method:

1. **Hard reject**: `roleCounts.GetValueOrDefault("KEY_SESSION") != 1` → `RoleCountIncorrect` error for any 2-KEY week. Also `EASY_SUPPORT` count is validated against `preferredDays.Count - 2` — a formula that assumes exactly 2 non-Easy roles (1 KEY + 1 LONG); for 2 KEY + 1 LONG = 3 non-Easy roles, the correct formula would need `- 3`, so this check is doubly wrong for a 2-KEY layout, not just the KEY-count line.
2. **Silent partial-validation bug, more dangerous than the reject**: `week.SessionSlots.FirstOrDefault(s => s.StructuralRole == "KEY_SESSION")` — uses `FirstOrDefault`. If the hard reject above were ever bypassed or relaxed without also fixing this line, a 2-KEY week's **second** KEY session would be silently excluded from the KEY↔LONG separation check entirely — not rejected, not flagged, just never checked. This is exactly the class of bug this phase's own framing warned about (the "WindowExecutionSummary boolean cardinality" pattern) — found here in a different component.

**Given E and F together: calendar feasibility (the phase's own literal question — "does a deterministic valid placement exist?") cannot even be evaluated today, because the validator that would need to confirm it rejects 2-KEY weeks outright before any placement logic runs.** This is not a "some preferred-day patterns fail" answer — it's "no pattern succeeds," because the gate is unconditional on role count, not on placement feasibility.

## G. Workout catalog capacity

Real catalog inspection (mirroring GEN.4C's method): Intermediate's `eligibleWorkouts` list (`intermediate-modifier.v6.json`) has 5 distinct workout keys: `EASY_STANDARD`, `LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`. Excluding the Easy/Long-role workouts, **3 distinct KEY-eligible identities exist for Intermediate**: `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`.

**Real answer**: 3 is enough to select 2 *different* concurrent KEY prescriptions in a single week (satisfying the phase's literal question), but is a narrow rotation pool across a full multi-week Core/Runway cycle without eventual repetition. Also carries forward the pre-existing, already-documented catalog gap (GEN.4C §12): `FARTLEK`/`THRESHOLD_TEMPO` lack real interval/repetition structure catalog-wide — unrelated to 5D specifically, not re-litigated here, but relevant to any real KEY1/KEY2 content decision downstream.

## H. Adaptation V1 cardinality impact — highest care, real defects confirmed by direct code read

**1. `WindowExecutionSummaryBuilder`/`KeySessionCompleted`**: field is `bool` (`AdaptationDomainContracts.cs:166`). Computed via `keyCompleted &= isEffectivelyCompleted` — an AND-accumulation across every session with `KeySession` role encountered that week. **Real behavior for a 2-KEY week where 1-of-2 completes: `KeySessionCompleted = false`** — informationally lossy (indistinguishable from 0-of-2 completing), but not a crash or miscount in isolation.

**2. `NextWindowLoadDecisionPolicy.DetermineLoadDecision` — the real, more serious defect.** Its own doc comment self-discloses: *"an explicit PRODUCT DEFAULT calibrated for the current 4-session pilot ... not a general formula."* The severity switch is a raw count match:
```csharp
0 or 1 => Reduce, 2 => Maintain, 3 => (conditional), >= 4 => ProgressAsPlanned
```
**This is hardcoded to a 4-total-session week.** For a 5-session week (2K+2E+1L), completing 4 of 5 (missing one session) falls into `>= 4` → `ProgressAsPlanned` — the same classification as a *fully complete* 4-session week. **This mis-scores an incomplete 5-session week as fully successful.** This is the real, confirmed cardinality defect the phase asked to check for, found by direct code read, not assumed away as "probably fixed by Rev5."

**3. `ScheduleRepairPolicy`/`CandidateSelectionPolicy`**: checked directly — neither file references `StructuralRole`/session-role at all. They operate generically, per-trigger-session, with no role-uniqueness assumption found. **These two are clean.**

**4. `HardSessionSeparation`** — cross-referenced from §D: no such literal-named mechanism exists; the real equivalent (`ScheduleRepairSpacingValidator`) is KEY↔LONG-only by explicit self-disclosure, no KEY↔KEY rule (§D.3).

## I. Taper/eligibility sanity check

Attempted a GEN.2B.3/GEN.5A.2-style closed-form floor check using only Intermediate's known parameters. **Could not complete a precise version, honestly disclosed**: unlike 3D (fixed per-role km minimums: 4+3+5=12km, a clean closed form), 4D's model uses fixed KEY/EASY minimums (`MinimumKeySessionDistanceKm=3.0`, `MinimumEasySupportDistanceKm=1.5`) but a **percentage-share-based** LONG_RUN (no fixed km minimum) — so a hypothetical 2KEY+2EASY+1LONG floor can't be closed-formed the same way without inventing a LONG minimum, which this phase's own DO NOT list prohibits ("do NOT select final numeric values").

**Partial, honest sanity check**: KEY+EASY minimums alone sum to `2×3.0 + 2×1.5 = 9.0km`, already exceeding Beginner×3D's entire binding floor (12km, for comparison of scale only). Given Intermediate's real starting volume (24km) and peak reference (38km) are far above this partial floor, and far above the levels where Beginner×3D's conflict actually bound (9.5-12.0km starts), **no taper-floor conflict is evident at Intermediate's real volume scale** — but this is a scale-based sanity check, not a proof, and should be re-verified with a real closed-form once LONG's minimum is defined in a future phase.

## J. Final classification

```
INTERMEDIATE_5D_ARCHITECTURE_AUDIT_BLOCKED_ON_SECTION_E_SECTION_F_SECTION_H
```

Section A (trajectory authority) and Sections C/D (evidence envelopes) are genuinely resolved/answerable and do not block. **Sections E, F, and H found real, code-confirmed, independent single-KEY-cardinality defects** (session-allocation hard-throw, calendar-validator hard-reject plus a silent `FirstOrDefault` partial-check bug, and Adaptation V1's hardcoded 4-session severity switch) that must be fixed before any 5D implementation phase can be scoped — regardless of how the trajectory-authority and workout-pairing questions eventually resolve. None of these were fixed here; none of FREQ.1's trajectory-shape conclusion was reopened; no numeric values were selected.
