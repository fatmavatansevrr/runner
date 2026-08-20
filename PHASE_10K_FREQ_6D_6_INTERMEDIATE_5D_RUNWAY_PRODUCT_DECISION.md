# PHASE 10K-FREQ.6D.6 — Intermediate×5D Preparation Runway Weekly Structure & Starting-Volume Product Decision

**Type:** EVIDENCE + PRODUCT_DECISION (no production code touched)
**Parent phase:** FREQ.6D.5
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Everything below is re-derived from the current repository state plus targeted external evidence.

---

## 1. Preflight

- `git rev-parse HEAD` at start: `fca553d79ec4061d4bc79bc34f6560e4a7eb7aa8`.
- `git branch --show-current`: `main`.
- `git status --short` (non-build-output): `m baseline_tmp`, `M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` — pre-existing, unrelated, preserved untouched.
- `git rev-list --left-right --count origin/main...HEAD`: `0  20`.
- `git diff --check`: clean.
- Commits `b465bbb`/`fca553d` confirmed reachable from `HEAD`.
- `FREQ.6D.5` report re-read in full; final classification `INTERMEDIATE_5D_RUNWAY_LONGHORIZON_SEPARATE_WAVES_REQUIRED` confirmed, along with its 7 repository-backed findings (Core 8-14 publicly active; Runway hardcoded to 4D at multiple layers; Runway catalog content candidate-agnostic; Runway numeric allocator already KEY-count-generic; no product authority for 5D Runway weekly structure; LongHorizon out of scope here).

**Phase ID.** No explicit next ID is pre-assigned in `MASTER_ROADMAP.md` beyond `FREQ.6D.5` (its own `[Next, not yet scheduled]` entry deliberately names no ID, per the "no speculative phase tree" rule). Following this engagement's established sequential convention (`FREQ.6D.4D.1`...`.5G`, then `FREQ.6D.5`), the next sequential ID is `FREQ.6D.6`, used here per this phase's own instruction to follow repository truth when no ID is explicitly fabricated ahead of time.

---

## 2. FREQ.6D.5 input (re-confirmed, not re-derived)

Re-verified directly from the audit report and, where load-bearing for this decision, re-traced independently this phase (not merely re-quoted): Preparation Runway's routing gate, candidate load, and orchestrator validation are hardcoded to `TEN_K__4D__INTERMEDIATE`; its catalog content (`plan-catalog/catalog/preparation-runway-progressions/*.json`) carries no level/frequency field at all; its numeric allocator (`V1FourDaySessionVolumeAllocationPolicy`) is explicitly documented as reusable for "a hypothetical 5D 2 KEY + 2 EASY + 1 LONG layout"; and no product authority anywhere decides Intermediate×5D's actual Runway weekly structure. LongHorizon remains untouched and out of scope.

---

## 3. Existing 4D Runway behavior (`INTERMEDIATE_4D_RUNWAY_BEHAVIOR_TABLE`)

Traced directly from `TenKPreparationRunwayWeekMaterializationPolicyFactory.cs` (`BuildLayout()`/`BuildBlockRolePolicies()`/`BuildSupportPolicy()`), `TenKPreparationRunwayAllocationPolicyFactory` (block weight/max-weeks), and the real catalog files.

| Block | Max in-block weeks | KEY_SESSION slots/week | EASY_SUPPORT slots/week | LONG_RUN slots/week | Workout(s) used |
|---|---|---|---|---|---|
| Consistency, week 1 | 1 of 2 | 1 | 2 | 1 | KEY→`EASY_STANDARD` v5, EASY→`EASY_STANDARD` v5, LONG→`LONG_RUN_STANDARD` v5 |
| Consistency, week 2 | 1 of 2 | **0** — KEY slot overridden to a second LONG_RUN | 2 | **2** | Both LONG slots→`LONG_RUN_STANDARD` v5 |
| GeneralEndurance | up to 5 | **0** at every week — KEY slot always overridden to LONG_RUN | 2 | **2** | `LONG_RUN_STANDARD` v5 |
| AerobicStrength | up to 2 | 1 at every week | 2 | 1 | KEY→`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` **v1** (steps 1/2), EASY→`EASY_STANDARD` v5, LONG→`LONG_RUN_STANDARD` v5 |
| PreSpecificTransition | fixed 1 | 1 | 2 | 1 | KEY→`EASY_STANDARD` v5 (not a quality workout), EASY/LONG same as above |

**Session count**: constant 4/week throughout (`RUN_LAYOUT_4D`, `runsPerWeek: 4`), never ramps.
**KEY count**: never more than 1/week, and genuinely 0/week for a majority of typical Runway compositions (Consistency-week-2 and every GeneralEndurance week — the single dominant, "near-always-present" block per its own product-intent doc, §4 below).
**Weekly volume progression**: linear interpolation, `startingWeekly → targetWeekly` (Core Week 1's own resolved volume) across `RunwayWeeks - 1` steps, final week forced exactly equal to target ("exact maintenance bridge").
**Long-run progression**: same interpolation mechanism, `startingLongRun → targetLongRun`, clamped to a `[30%, 36%]` share of weekly volume at every week, never permitted to decrease.
**Transition into Core**: PreSpecificTransition's single week is structurally identical in shape to 4D Core Week 1 (1 KEY + 2 EASY + 1 LONG, since 4D never has more than 1 KEY anywhere), and is forced numerically exact (all 4 slot distances within `0.001km`) via `AnalyzeContinuity`.
**Calendar behavior**: uses the same generic calendar materializer/day-assignment machinery as Core (not independently audited further this phase — no new calendar rule is implicated by anything decided below).

---

## 4. Runway's canonical purpose (repository-backed, not inferred)

`PHASE4G_6A_PREPARATION_RUNWAY_COMPOSITION_POLICY_AUDIT.md` §8, quoted exactly:

> "Preparation Runway is a pre-core, conservative core-entry-preparation period. Its primary responsibility is to converge consistency, frequency, easy aerobic durability, weekly volume, and long-run readiness toward a safe entry into PreferredCore without consuming or extending a race-core phase.
>
> Secondary objectives may include habit/frequency stabilization, general endurance, basic durability, and a controlled pre-specific transition...
>
> Excluded objectives are race-specific rehearsal, goal-pace rehearsal, taper, peak-volume achievement, **aggressive threshold progression**, and indefinite progression/maintenance."

Machine-readable tags: `PREPARATION_RUNWAY_PRIMARY_OBJECTIVE=CONSERVATIVE_CORE_ENTRY_PREPARATION`; `PREPARATION_RUNWAY_EXCLUDED_OBJECTIVES=..._AGGRESSIVE_THRESHOLD_..._AND_INDEFINITE_GROWTH`.

**This objective — explicitly conservative, explicitly excluding aggressive threshold/quality progression — directly informs the weekly-structure decision below**: the existing, already-approved 4D design keeps KEY exposure at 0-or-1 per week, never introduces the plan's full quality cardinality during Runway, and this is not an accident of engineering convenience — it is the documented product intent.

---

## 5. 15-20 horizon decomposition (`RUNWAY_CORE_HORIZON_TABLE`)

Traced from `TenKPreparationRunwayDarkOrchestrator.cs` (`horizon.AvailableFullWeeks - horizon.PreferredCoreWeeks`, with `PreferredCoreWeeks` validated `== 12`) and cross-confirmed against `PHASE4G_6A_4H_DARK_PREPARATION_RUNWAY_END_TO_END_ORCHESTRATION.md` §22:

| Total weeks | RunwayWeeks | CoreWeeks |
|---|---|---|
| 15 | 3 | 12 |
| 16 | 4 | 12 |
| 17 | 5 | 12 |
| 18 | 6 | 12 |
| 19 | 7 | 12 |
| 20 | 8 | 12 |

Core remains unconditionally 12-week preferred whenever Runway is involved — this phase does not touch or reconsider Core 8-14 semantics anywhere.

---

## 6. `DaysPerWeek` product semantics

Two independent pieces of internal architecture evidence, both pointing the same direction:

1. **RunLayout's own shape is a fixed cardinality, not a range.** `RUN_LAYOUT_4D`/`RUN_LAYOUT_5D` each declare a single `runsPerWeek` integer and an exact ordered slot list — there is no "minimum/maximum sessions" concept anywhere in the layout schema. The catalog architecture itself treats "days per week" as an exact per-week commitment, not a target a plan may approach.
2. **Core never ramps session count across its own phases.** Foundation/Build/RaceSpecific/Taper all use the identical `RUN_LAYOUT_5D` (or `RUN_LAYOUT_4D`) every single week — the already-proven, publicly-active Intermediate×5D Core pipeline never reduces to 4 sessions in an early phase and grows to 5 later. If frequency-ramping were an established product pattern anywhere in this codebase, Core itself would be the place it would already exist, and it does not.

**Conclusion: `DaysPerWeek` is a fixed weekly commitment for every full week of the plan, Runway included.** This directly and conclusively rejects Candidate D (frequency ramp, §11/§13 below) on internal-architecture grounds alone, independent of external evidence.

---

## 7. External evidence

Web research (August 2026), classified per the phase's own required taxonomy:

- **COACHING_PATTERN**: "Most runners only need one speed workout and one long run each week with easy mileage making up the other days during base phase" — i.e. the general coaching convention for a base/preparation phase is **one** quality session, not two, with 80-90% of weekly volume at easy effort. [Fleet Feet — Build A Solid Base](https://www.fleetfeet.com/blog/build-a-solid-base), [Strength Running — Base Training Fundamentals](https://strengthrunning.com/2018/02/base-training-fundamentals/)
- **COACHING_PATTERN**: a representative 5-day/week intermediate structure in commercial base-building plans is "2 workouts and 1 long run" (i.e. 2 quality + 1 long + 2 easy) — but this is describing plans that are themselves already race-specific-adjacent, not a pre-Core conservative preparation segment; treated as weaker, contextually-different evidence than the base-phase-specific finding above. [TrainingPeaks — 8 Week Base Building Plan Intermediate](https://www.trainingpeaks.com/training-plans/running/10km/tp-227243/8-week-base-building-plan-intermediate)
- **COACHING_PATTERN**: quality-session count and phase intensity progress over time within a plan ("progressively increase in intensity and frequency as you move through the training phases toward race day"; "2 quality sessions per week during build and specific phases" for marathon, implying fewer during base) — i.e. the pattern is quality-count ramping across phases, not session-count/frequency ramping within a fixed weekly commitment. [Alastair Running — 10K Training Plan](https://www.alastairrunning.com/10k-training-plan/), [Runner's Blueprint — Marathon Training Plan](https://www.runnersblueprint.com/the-complete-marathon-training-blueprint-plans-science-and-race%E2%80%91day-tips/)
- **DIRECT_EVIDENCE (internal)**: the real, already-approved, already-shipped 4D Runway design (§3 above) already embodies exactly this pattern — 0-or-1 KEY session per week during Runway, 2 KEY only at Core entry.

No claim above is presented as physiological certainty — all are coaching-convention-level patterns, used as *supporting* evidence for a decision primarily grounded in the existing internal architecture and product-intent documentation (§4, §6), consistent with this phase's own instruction not to treat external patterns as majority-vote authority.

---

## 8. Training-plan pattern summary (`INTERMEDIATE_5D_PREPARATION_PATTERN_TABLE`)

| Source pattern | Weekly frequency | Quality sessions/week | Long run | Frequency ramps? | Quality ramps? |
|---|---|---|---|---|---|
| General base-phase coaching convention | Fixed, plan-specified | 1 | Yes, always | No | Yes — 0/1→2 as phase progresses |
| 5-day intermediate base-building (TrainingPeaks) | Fixed 5 | 2 | Yes | No | N/A (already at target) |
| Existing Appsel 4D Runway (real, internal) | Fixed 4 | 0-1 | Yes, every week | No | Yes — 0/1 during Runway → 1 at Core (4D has no second KEY anywhere) |

Used as supporting evidence only, per the phase's own instruction — the decision below is anchored primarily in §4/§6/§3.

---

## 9-13. Candidate weekly-structure analysis

**Candidate A — 2 KEY + 2 EASY + 1 LONG, every Runway week** (§8 of the phase prompt): **rejected.** Directly contradicts the documented "conservative," "excluding aggressive threshold progression" purpose (§4) — introducing full Core-cardinality quality exposure from Runway Week 1 is the opposite of what Runway exists to do. No external evidence supports 2 quality sessions during a conservative pre-Core base phase specifically (§7's base-phase-specific source recommends 1). Not selected.

**Candidate B — 1 KEY + 3 EASY + 1 LONG, transitioning to 2 KEY + 2 EASY + 1 LONG at Core entry** (§9 of the phase prompt): **selected.** Directly generalizes the existing, already-approved 4D block-role pattern (§3) by adding one more `EASY_SUPPORT` slot for the 5th day — a mechanical, evidence-free-of-new-judgment extension (the 5th day being easy-effort aerobic volume is squarely within Runway's own "easy aerobic durability" objective, §4, and requires no new numeric or structural invention). Matches external base-phase coaching convention (1 quality/week, §7). Catalog-representable using the same `EASY_STANDARD`/`LONG_RUN_STANDARD`/`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` content Runway already uses (§16). Calendar-representable under the existing single-KEY spacing rule (no multi-KEY interaction needed during Runway itself, §19).

**Candidate C — aerobic/no-KEY 5D Runway** (§10 of the phase prompt): **rejected as the exclusive structure**, but partially already present as a legitimate *sub-state*. The existing 4D design already has genuine zero-KEY weeks (Consistency week 2, all GeneralEndurance weeks) — so "no quality this specific week" is already real, approved behavior, not a new candidate. But a Runway that is *never* anything but aerobic would eliminate the `AerobicStrength` block's entire purpose (already a real, approved, currently-used block whose own product-intent doc names it explicitly: "running-based controlled aerobic-power/economy stimulus," `PHASE4G_6A_2` §4) — rejecting the AerobicStrength block outright is not supported by any evidence found and would be a regression from the existing 4D design's own already-approved scope. Not selected as a standalone candidate; its correct role is what Candidate B already provides (many weeks legitimately have 0 KEY, per the same block-role table logic as 4D).

**Candidate D — frequency ramp (fewer than 5 sessions early, ramping to 5)** (§11 of the phase prompt): **rejected**, conclusively, on §6's grounds — `DaysPerWeek` is architecturally and product-semantically a fixed per-week commitment, not a target the preparation segment may approach. No internal or external evidence found supports session-count ramping for an already-committed frequency; every source found (internal Core precedent, external coaching pattern) describes fixed weekly frequency with *quality*-count (not session-count) progression. Approving Candidate D would also contradict §38's user-expectation concern directly — a user who selected "5 days/week" and received a 4-day Runway week without being told to expect that would experience a silent contradiction of their own explicit selection.

---

## 14. Selected weekly structure

**Intermediate×5D Preparation Runway weekly layout:**
- **1 KEY_SESSION**
- **3 EASY_SUPPORT**
- **1 LONG_RUN**

for every full Runway week, with the same per-block KEY↔LONG override logic 4D already uses (Consistency week 2 and every GeneralEndurance week reassign the KEY slot to a second LONG_RUN, exactly mirroring §3's table) — meaning many Runway weeks legitimately have **0** KEY sessions, a small number (AerobicStrength, PreSpecificTransition) have exactly **1**, and **none ever have 2**. Session count is invariant at **5** across every Runway week (no ramp, §6).

**Second-KEY introduction point: exactly at real Core Week 1** (the first day of the 12-week Core segment), never during Runway. This is a deliberate departure from 4D's literal-structural-match precedent (where Transition happens to equal Core Week 1's shape only because 4D never has more than 1 KEY anywhere) — for 5D, PreSpecificTransition's single week stays 1 KEY + 3 EASY + 1 LONG, structurally *different* in KEY-count from Core Week 1's 2 KEY + 2 EASY + 1 LONG. This is evidence-grounded, not arbitrary: it preserves Runway's own "conservative, no aggressive threshold progression" objective (§4) all the way to the Core boundary, matches the external base-phase convention of introducing the second quality session only once the formal program begins (§7/§8), and is explicitly permitted by the phase's own framing (§35: "Is that transition itself acceptable? Use evidence/product semantics — do not assume 'same frequency' means 'same role pattern'").

**Calendar authority**: reuses the existing single-KEY spacing rule (KEY↔LONG minimum separation) unmodified during Runway — no KEY↔KEY rule is invoked during Runway itself since no Runway week ever has 2 KEY sessions (§19).

**RWS decision inventory** (§7 of the phase prompt), final status:

| ID | Question | Status |
|---|---|---|
| RWS-1 | Full 5 sessions/week throughout Runway? | `DECIDED` — Yes |
| RWS-2 | Does frequency ramp? | `DECIDED` — No |
| RWS-3 | KEY_SESSION slots/week? | `DECIDED` — 0 or 1, never 2 |
| RWS-4 | LONG_RUN every week? | `DECIDED` — Yes (mirrors 4D exactly) |
| RWS-5 | Non-KEY/non-LONG fill? | `DECIDED` — `EASY_SUPPORT` (3/week) |
| RWS-6 | Structure varies across weeks? | `DECIDED` — Yes, by block, mirroring 4D's own per-block-week KEY↔LONG override table |
| RWS-7 | Must final Runway week structurally match Core Week 1? | `DECIDED` — No, KEY count deliberately differs (1 vs 2); numeric continuity (total weekly volume, long run) is still required, see §26 |
| RWS-8 | Reuse `RUN_LAYOUT_5D` or need Runway-specific authority? | `DECIDED` — Needs its own explicit Runway-specific structural authority (a generalized version of `TenKPreparationRunwayWeekMaterializationPolicyFactory`'s block-role table, parameterized for a 5-slot base layout), **not** literal `RUN_LAYOUT_5D` reuse, since that layout is Core's dual-KEY shape |

---

## 15. Quality-session exposure

Resolved above (§14): **0 or 1 KEY session/week during Runway, never 2.** Answering the phase's own explicit questions: Runway is preparing volume *and* frequency-habit *and* long-run tolerance readiness (§4's documented primary objective) *and*, secondarily, single-quality-session tolerance via the `AerobicStrength` block — it is explicitly **not** preparing two-quality-session tolerance; that is introduced only at Core entry (§14's second-KEY introduction point). Current catalog capacity does **not** need to support a legitimate Runway second KEY under this decision, since none is required.

---

## 16. Catalog-capacity result

Candidate B requires no new catalog authoring:

| Slot | WorkoutDefinition | Runway-eligible? | Version used by Runway | Notes |
|---|---|---|---|---|
| KEY (AerobicStrength weeks) | `AEROBIC_STRENGTH_CONTROLLED_INTRO` | Yes | **v1** (Runway's own progression doc) — distinct from Core's v3; both already `eligibleForLegacyDefaultResolution`-scoped and Runway-eligible independently, no conflict | Confirmed real, existing content; no authoring needed |
| KEY (AerobicStrength weeks, step 2) | `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` | Yes | v1 | **Public workout-type mapping gap already flagged in `FREQ.6D.5`** (§25 there) — `V1CatalogPublicWorkoutTypeMappingPolicy` has no arm for this key. Independent of the 5D question (would block 4D Runway activation too); classified `CATALOG_CAPACITY_BLOCKER` for the *implementation* phase, not for this decision |
| KEY (Consistency/PreSpecificTransition weeks) | `EASY_STANDARD` | Yes | v5 | Already mapped (`Easy`) |
| EASY (all weeks) | `EASY_STANDARD` | Yes | v5 | Already mapped |
| LONG (all weeks) | `LONG_RUN_STANDARD` | Yes | v5 | Already mapped |

No new `WorkoutDefinition`, no new `PrescriptionProfile`, no new dose category is needed — the entire selected structure reuses content Runway already prescribes today for 4D, unchanged in identity. The one real gap (`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` mapping) is narrow, pre-existing, and orthogonal to this decision.

---

## 17. Calendar feasibility

Under Candidate B, at most 1 KEY session/week ever appears during Runway — the existing KEY↔LONG spacing rule (already generalized and proven across 3D/4D/5D Core) applies unchanged; the KEY↔KEY multi-slot rule (also already proven for Core's dual-KEY weeks) is simply never invoked during Runway, since it never has 2 KEY sessions in the same week. No new calendar rule is needed. Representative legal day sets: any 5-day selection satisfying the existing KEY↔LONG minimum-separation rule for whichever single day carries the week's KEY/LONG-doubled slots — structurally simpler than Core's own dual-KEY weeks, which already pass. No representative infeasible set was identified specific to this structure (Runway's 5-day selection is a strict subset of the already-proven Core 5-day calendar feasibility space, since Runway never requires 2 KEY separations Core already handles).

---

## 18. Long-run authority

Every Runway week requires `LONG_RUN` (§3, unchanged — Candidate B preserves this exactly, including the doubled-LONG_RUN weeks where the KEY slot converts to a second long run). Starting long-run amount and progression reuse the existing, already-generic formula (§20 below) — no new decision needed here; the 4D policy's long-run mechanics contain no frequency-specific literal.

---

## 19. Starting-volume: current policy (re-derived, not assumed)

`PreparationRunwayNumericMaterializer.ResolveStartingWeeklyVolume` (exact code re-traced, §2 of the research agent's report, reproduced here for the decision record):

```
Provided (positive observed)  → Round(RecentWeeklyVolumeKm, 0.5km)
Missing (no input)            → policy.MissingWeeklyVolumeDefaultKm
NoRecentRunningBase (explicit zero) → policy.NoRecentRunningBaseDefaultKm
```

`TenKPreparationRunwayNumericPolicyFactory.Build()` wires `MissingWeeklyVolumeDefaultKm` and `NoRecentRunningBaseDefaultKm` directly from `V1MissingReadinessStartingVolumePolicy` (16km / 12km, the same class 4D Runway uses today).

---

## 20. Observed-positive authority

**`SV-A` selected, `DECIDED`.** The mechanism (`Round(reported, 0.5km)`, no other clamp) is already 100% frequency-agnostic — it contains no `DaysPerWeek` or candidate literal anywhere. Directly reusable for 5D Runway verbatim, with zero new product judgment required. This exactly mirrors how Core's own `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` treats a positive `Provided` reading (`Round(reported.Value)`, `WeeklyVolumeAnchorSource.RecentFourWeekAverage`) — the same policy shape at both layers.

---

## 21. Missing-readiness authority — the decisive finding

**Direct repository-truth discovery this phase** (`CatalogVolumeAndLongRunPlanner.Build`, lines 24-33):

```csharp
if (request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 4 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
    return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.BeginnerFourDay).Build(request);
if (request.Candidate.DaysPerWeek == 3 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
    return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.ThreeDayIntermediate).Build(request);
```

Only Beginner×4D and Intermediate×3D are special-cased. **Intermediate×5D — the real, already publicly-active Core candidate — has no special case and falls through to `VolumeSafetyPolicy.Default`**, whose `ResolveStartingVolume` (line 120-124) resolves missing/zero readiness via exactly `V1MissingReadinessStartingVolumePolicy` — the identical class, with the identical 16km/12km values, that Preparation Runway's own numeric factory already uses.

**This is not "borrowing 4D's absolute value into 5D."** This is confirming that the real, live, already-shipped Intermediate×5D Core pipeline **already uses this exact policy today**, for the same missing/zero-readiness classification, independent of anything Runway does. Approving the same policy for Runway is adopting the *already-established, already-active Intermediate×5D numeric authority*, not inventing or transplanting a new one. It further explains the 3D/4D-identical-values pattern noted in `FREQ.6D.5`'s own audit (§8 there): the value is genuinely level-scoped (Intermediate) and frequency-invariant by the architecture's own existing dispatch logic, not merely coincidentally equal across two audited cells.

**`DECIDED`**: Intermediate×5D Preparation Runway reuses `V1MissingReadinessStartingVolumePolicy` verbatim — `MissingWeeklyVolumeDefaultKm = 16km`, provenance: *this is the already-real Intermediate×5D Core missing-readiness authority (`CatalogVolumeAndLongRunPlanner`'s own default-policy fallthrough), confirmed by direct code inspection this phase, not a newly-authored or cross-frequency-borrowed value.*

A materially important, favorable consequence follows: since **Core Week 1's own volume** (the number Runway must bridge toward) is resolved through this exact same policy for the identical readiness inputs, **Runway Week 1 and the Core-entry target are, for the missing/zero/matched-observed cases, computed from the same starting point** — collapsing what could have been a difficult progression-feasibility question into a near-trivial one (§28).

---

## 22. Explicit-zero authority

Same reasoning as §21, `NoRecentRunningBaseDefaultKm = 12km`, identical dispatch-logic confirmation. **`DECIDED`** — reused verbatim, same provenance rationale.

`ValidateRequest`'s own typed-evidence invariant (§2 of the research report) already fails closed on any inconsistent evidence-state/value combination — this behavior is candidate-agnostic and requires no change.

Per §24's own caution ("Runway is not an unlimited rescue path"): the explicit-zero case is representable under V1 policy for every 15-20 week horizon, per the feasibility analysis in §28 — it does not require weakening any existing safety constraint, since (per §21's finding) Core's own explicit-zero Week 1 target uses the identical 12km starting point.

---

## 23. Progression/allocation authority (reused, unchanged)

`VolumeSafetyPolicy.Default`'s coefficients (`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5`, `LongRunPreferredMinimumShare=0.30`, `LongRunPreferredMaximumShare=0.36`, `LongRunSelectionShare=0.33`, `LongRunHardCapShare=0.40`, `RoundingIncrementKm=0.5`) are already frequency-agnostic — already reused verbatim across 3D/4D/5D Core and Runway today. **`DECIDED`, no new value.** The linear-interpolation progression mechanism and the "final week forced exactly to target" rule are architecture, not numeric product decisions, and generalize to 5D without modification (the interpolation math has no frequency literal in it).

---

## 24. Minimum weekly-volume result

Given Candidate B's per-role minimums are identical in kind to 4D's own (1 KEY, 3 EASY instead of 2, 1 LONG — one additional generic EASY_SUPPORT slot), and `V1FourDaySessionVolumeAllocationPolicy`'s own doc comment already documents it as reusable for "any 'V1 multi-key' shape sharing the same 2 EASY_SUPPORT + 1 LONG_RUN structure" (i.e. its generalization axis is KEY-count, and a 3-EASY variant is architecturally the same shape family, just with one more EASY instance) — no new per-session minimum distance is introduced by this decision. The allocator's existing per-role minimum-distance enforcement (unchanged from 4D) is expected to be sufficient; this is flagged as a `PRODUCT_DECISION` **not** required here, but as an item for the implementation phase's own dark-verification pass (§34) to confirm empirically rather than re-derive analytically in this decision-only phase, consistent with §32's instruction not to invent minima.

---

## 25. Allocation authority

`V1FourDaySessionVolumeAllocationPolicy.Allocate` operates on `sessions.Count(s => s.StructuralRole == "KEY_SESSION")` generically (confirmed in `FREQ.6D.5`'s own audit) — it does not hardcode "exactly 1 KEY," so it is expected to correctly handle Candidate B's 1-KEY/3-EASY/1-LONG shape (a strict generalization of the existing 1-KEY/2-EASY/1-LONG 4D shape by one additional EASY instance) without new ratios. **`DECIDED` — reuse.** No 4D-specific percentage is being extrapolated; the allocator was already proven KEY-count-generic independent of this decision.

---

## 26. Core-entry target — what "compatible" means

Per this phase's own instruction not to require equality on dimensions the architecture doesn't treat as handoff authorities: **Core-entry compatibility for Intermediate×5D Runway means exact continuity of total weekly volume and long-run distance** (the two dimensions the existing `AnalyzeContinuity` mechanism already checks and that the numeric progression is actually built to converge), **not** per-slot role-count equality. KEY count is explicitly **not** a Core-entry compatibility dimension under this decision (§14) — the second KEY is introduced structurally at Core Week 1 itself, redistributing the already-continuous total weekly volume across one additional quality slot, not requiring a pre-existing second KEY to already be present in the final Runway week.

This is a genuine, disclosed departure from how `AnalyzeContinuity`'s current 4D implementation is structured (per-slot delta comparison including `KeySession`), which assumed literal 1:1 slot correspondence because 4D's KEY count never changes across the boundary. **Flagged explicitly as an engineering item for the implementation phase**: the continuity validator's mechanism needs to generalize from "every named slot must match" to "total weekly volume and long-run distance must match; KEY-count redistribution at the boundary is expected and legitimate." This is not a new numeric decision — it is a specification of what the existing validator's *invariant* should mean once role cardinality can legitimately differ across the boundary, which this product decision now makes true for the first time in the codebase.

---

## 27. Core-entry volume (per Core length)

Per §21's finding, Core Week 1's own volume for Intermediate×5D — for any of the 8/10/12/14-week Core lengths — is resolved through the identical `V1MissingReadinessStartingVolumePolicy`/positive-`Provided`-passthrough mechanism, since `CatalogVolumeAndLongRunPlanner`'s starting-volume resolution (§21's quoted dispatch) runs identically regardless of `BoundPlan.Weeks.Count` (Core length) — the branch that varies by week count is the *peak*-volume computation (`ResolvePeak`), not Week 1's starting value. **Therefore Core Week 1's target volume is the same 16km/12km/observed-value regardless of which Core length (8/10/12/14) the user's total horizon implies** — this is not being newly decided here; it follows directly from the already-approved FREQ.6C Intermediate×5D Core numeric authority, unchanged and unmodified by this phase. Runway's own progression target is therefore identical across all six 15-20 total-week horizons for a given readiness case, regardless of which Core length that horizon selects underneath.

---

## 28. Progression feasibility

Given §21/§27's finding that Runway's own Week-1 starting volume and Core's own Week-1 target volume are resolved through the identical policy for missing/zero/observed-positive readiness alike, the "gap to bridge" across Runway's 3-8 available weeks is **at most the difference introduced by Runway's own internal 0.5km rounding** (negligible, well within a single week's `AbsoluteWeeklyIncrementCapKm=2.5km`/`HardMaxWeeklyIncreaseRatio=0.08` headroom) for every one of the missing, explicit-zero, and matched-observed-volume cases. This holds **for every one of the 3-8 available runway-week counts** (15 through 20 total weeks) — feasibility does not degrade at the shorter (3-week, 15-total) end, since the required growth is already near-zero regardless of how many weeks are available to spread it across.

The one case requiring genuine feasibility analysis is a **low-positive observed volume substantially below what Core Week 1 would otherwise resolve to** — but per §27, Core Week 1's own target for a *given* positive `Provided` reading is *that same reading* (both Runway and Core anchor to the literal same reported number) — so even here, start and target coincide by construction; there is no scenario under the current architecture where Runway's target and Runway's own starting point diverge, because both are the same function of the same evidence. **Progression feasibility is therefore assured, structurally, for the full 15-20 week range and all readiness states, without needing a new numeric decision** — the interpolation mechanism exists and functions, but in practice converges from a value to itself.

---

## 29. Representability matrix (`INTERMEDIATE_5D_RUNWAY_REPRESENTABILITY_MATRIX`)

| Total weeks | Readiness | Runway weeks | Starting volume | Core-entry target | Required growth | Max legal growth (any single week) | Structurally representable? | Numerically representable? | Product eligible? | Reason |
|---|---|---|---|---|---|---|---|---|---|---|
| 15 | missing | 3 | 16km | 16km | ~0 | 2.5km / 8% | Yes | Yes | Yes | Same policy both sides (§21/§27) |
| 15 | zero | 3 | 12km | 12km | ~0 | same | Yes | Yes | Yes | Same |
| 15 | low-positive | 3 | reported | reported (same) | 0 | same | Yes | Yes | Yes | Same |
| 16-19 | missing/zero/positive | 4-7 | as above | as above | ~0 | same | Yes | Yes | Yes | Same reasoning, more weeks only adds slack |
| 20 | missing | 8 | 16km | 16km | ~0 | same | Yes | Yes | Yes | Same |
| 20 | zero | 8 | 12km | 12km | ~0 | same | Yes | Yes | Yes | Same |
| 20 | representative positive | 8 | reported | reported (same) | 0 | same | Yes | Yes | Yes | Same |

Every cell across the full 15-20 range and every readiness state is representable under this decision's structure and existing generic numeric authority — no cell is blocked, and no new numeric point had to be invented to reach this result.

---

## 30. Do not extend Core

No change proposed or implied anywhere above to Core's 8-14 week semantics, phase durations, or numeric authority — every reused value (§20/§21/§22/§23/§27) is the *existing*, unmodified Core/Runway authority, confirmed by direct code inspection, never a new derivation.

---

## 31. Selected weekly structure — restated precisely (§42 required format)

> **Intermediate×5D Preparation Runway weekly layout:**
> 1 KEY_SESSION
> 3 EASY_SUPPORT
> 1 LONG_RUN
>
> for every full Runway week (session count invariant at 5 across all Runway weeks — never fewer, never more).
>
> Role cardinality varies by block exactly as the existing 4D block-role table does: the KEY_SESSION slot is reassigned to a second LONG_RUN for Consistency-block week 2 and every GeneralEndurance-block week (yielding 0 KEY + 3 EASY + 2 LONG for those specific weeks); it remains KEY_SESSION for AerobicStrength-block weeks and the single PreSpecificTransition week.
>
> Second-KEY introduction point: **exactly at real Core Week 1**, never during Runway. Frozen — no evidence supports introducing it earlier.
>
> Calendar authority: the existing, unmodified KEY↔LONG spacing rule (already proven across 3D/4D/5D Core); no KEY↔KEY rule is ever invoked during Runway under this structure.
>
> Structural authority: a new, Runway-specific structural policy (generalizing `TenKPreparationRunwayWeekMaterializationPolicyFactory`'s existing block-role table to a 5-slot base layout), not literal reuse of `RUN_LAYOUT_5D`.

---

## 32. Selected starting-volume rules — restated precisely (§43 required format)

| Case | Input | Formula/default | Clamp | Rounding | Minimum | Eligibility behavior | Provenance |
|---|---|---|---|---|---|---|---|
| A. Observed positive | `RecentWeeklyVolumeKm > 0` | `Round(RecentWeeklyVolumeKm, 0.5km)` | None beyond rounding | Nearest 0.5km | None beyond &gt;0 input | Always eligible | Reuses `PreparationRunwayNumericMaterializer.ResolveStartingWeeklyVolume`'s `Provided` branch verbatim, frequency-agnostic by construction |
| B. Missing readiness | No input provided | `16km` (`V1MissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm`) | Conservative clamp (`WeeklyVolumeAnchorSource.LevelConservativeDefault`) | Already round | N/A | Always eligible | **Confirmed this phase**: the same value already governs real, live Intermediate×5D Core's own missing-readiness Week 1 (`CatalogVolumeAndLongRunPlanner.Build`'s default-policy fallthrough) — not a cross-frequency borrow |
| C. Explicit zero | `RecentWeeklyVolumeKm == 0`, state `Provided`/`NoRecentRunningBase` | `12km` (`V1MissingReadinessStartingVolumePolicy.ExplicitZeroWeeklyVolumeDefaultKm`) | Conservative clamp (`WeeklyVolumeAnchorSource.NoRecentRunningDefault`) | Already round | N/A | Always eligible | Same confirmation as B |

No ambiguous "use normal Runway logic" language — every value, clamp, rounding rule, and provenance is stated exactly.

---

## 33. No-unapproved-values audit

Every numeric value adopted above (16km, 12km, 0.5km rounding, 0.07/0.08/2.5km progression caps, 0.30/0.33/0.36/0.40 long-run shares) is an **already-existing, already-real** value from either `V1MissingReadinessStartingVolumePolicy` or `VolumeSafetyPolicy.Default` — both independently confirmed this phase to already be the live Intermediate×5D Core authority via direct dispatch-logic inspection (§21), not a value copied from a *different* frequency cell's own dedicated policy (contrast: `V1ThreeDayMissingReadinessStartingVolumePolicy`/`V1BeginnerFourDayMissingReadinessStartingVolumePolicy` are genuinely distinct, differently-valued, cell-specific policies that were correctly *not* reused here). No new number was invented; no 4D-only absolute value was transplanted.

---

## 34. Implementation contract (for the next phase, not performed here)

Per the phase's own §46 expected scope:

1. Remove the Intermediate×4D-only routing gate (`PlanServices.IsPreparationRunwayPilotScope`) — dispatch through `V1CatalogPilotIdentityPolicy`/a supported-combination check that includes `(Intermediate, 5)`.
2. Replace `CatalogPreviewGenerator.cs:414-415`'s unconditional 4D candidate load with `ResolveCandidate(level, daysPerWeek)`.
3. Generalize `TenKPreparationRunwayDarkOrchestrator.ValidateRequest`'s hardcoded `CandidateKey`/`DaysPerWeek` literal-equality checks into a real supported-Runway-candidate authority (not a widened `||` literal list, per this phase's own §7 prohibition).
4. Author a new Runway-specific structural policy materializing the selected 1 KEY + 3 EASY + 1 LONG 5-slot layout with the same per-block override table shape as `TenKPreparationRunwayWeekMaterializationPolicyFactory`.
5. Reuse the numeric allocator, `VolumeSafetyPolicy.Default`, and `V1MissingReadinessStartingVolumePolicy` verbatim — no new policy class needed for volume (unlike the 3D/Beginner-4D pattern, since this phase found the *default* fallthrough already correctly applies).
6. Generalize `AnalyzeContinuity` per §26 — total weekly volume + long-run continuity, not per-slot role-count equality, at the Runway→Core boundary.
7. Wire the shared execution-index gap `FREQ.6D.5`'s own audit disclosed (§15/§18 there — `TenKPreparationRunwayDarkOrchestrator` never receives `IPublishedTemplateBundleLoader`/`ExecutionPrescriptionIndex`) — required the moment any ProfileBacked Core session is reached through the Runway→Core handoff, which real 5D Core sessions now always are.
8. Add the `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` public-workout-type mapping arm (narrow, `5E`/`5F`-pattern fix).
9. Preserve 4D behavior byte-for-byte (zero delta) — every reused constant/mechanism above is verified already-shared, so this should hold by construction if items 1-3 are scoped correctly (identity-based dispatch, not behavior change to the 4D path).
10. Remain strictly separate from LongHorizon — no schema, JIT, or lineage work.

No implementation performed in this phase.

---

## 35. Test manifest for the next phase

1. Real 15-week Intermediate×5D Runway+Core dark plan (3 runway weeks + 12 Core).
2. Real 16-week (4 runway weeks).
3. Real 17-week (5 runway weeks).
4. Real 18-week (6 runway weeks).
5. Real 19-week (7 runway weeks).
6. Real 20-week (8 runway weeks).
7. Selected weekly role cardinality (1 KEY/3 EASY/1 LONG, or 0 KEY/3 EASY/2 LONG on override weeks) verified per block.
8. KEY count never exceeds 1 during any Runway week, for any of the six horizons.
9. `LONG_RUN` present every Runway week.
10. 5 preferred days / calendar representability for the selected structure.
11. Starting-volume observed-positive case.
12. Starting-volume missing case (16km).
13. Starting-volume explicit-zero case (12km).
14. Low-positive observed case, still representable.
15. Progression caps never violated across the full interpolation for all six horizons.
16. Runway→Core transition: total weekly volume and long-run continuity exact; KEY count legitimately differs (1→2).
17. Core segment remains the real `TEN_K__5D__INTERMEDIATE` candidate, unmodified.
18. Core Week 1 dual-KEY behavior unaffected by anything Runway does.
19. Public workout-type mapping closure, including the new `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` arm.
20. Public preview succeeds for all six horizons.
21. Public confirmation succeeds for a representative horizon.
22. No silent 4D fallback anywhere in the 5D Runway path.
23. Intermediate×4D Runway zero-delta (full existing regression suite, unchanged).
24. LongHorizon test suite untouched, zero delta.

---

## 36. Technical-debt disposition

`TD-RUNWAY-ARCHITECTURE-HARDCODED-SINGLE-CELL-001` — **reclassified `ENGINEERING_READY`** (not resolved — no code has changed). The product-authority blocker this debt record's own scope implicitly depended on (no decided weekly structure/numeric authority for any cell beyond 4D) is now closed for the Intermediate×5D cell specifically; the debt itself remains open until the implementation phase (§34) actually removes the hardcoding. Debt record preserved, not deleted, per this phase's own instruction.

---

## 37. Remaining blockers

None block *this decision* from closing. One narrow, pre-existing, independent engineering item carries forward to the implementation phase: the `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` public-workout-type mapping gap (§16) and the execution-index wiring gap in `TenKPreparationRunwayDarkOrchestrator` (§34 item 7) — both real, both narrow, neither requiring a further product decision.

---

## 38. Next phase / type

**Preparation Runway architecture + implementation phase** (`IMPLEMENTATION + INTEGRATED VERIFICATION` type, mirroring the `FREQ.6D.4D.5x` discipline: dark proof first, then routing retry, STOP on any new independent blocker) — implements the 10-item contract in §34. LongHorizon remains a distinct, later, unscheduled wave per `FREQ.6D.5`'s own separate-waves finding.

---

## 39. Final classification

**`INTERMEDIATE_5D_RUNWAY_PRODUCT_POLICY_APPROVED`**

Both required product authorities (weekly structure, §14/§31; starting-volume, §32) are now decided, each grounded in at least one of this phase's own required decision-standard categories (§41 of the phase prompt): the weekly structure is grounded in strong internal architecture semantics (§6's RunLayout-cardinality/Core-phase-invariance precedent) plus the existing, already-approved 4D block-role design generalized by exactly one mechanical addition (§14), corroborated by external coaching-pattern evidence (§7/§8); the starting-volume rules are grounded in direct existing Appsel authority — not merely a reusable default, but the literal, already-active Intermediate×5D Core numeric policy itself (§21/§22/§33). No unapproved value was invented (§33), Core 8-14 was not touched or extended (§30), and LongHorizon was not decided or implemented (§48 of the phase prompt, strictly respected). Feasibility across the full 15-20 week range and all readiness states is structurally assured (§28/§29), not merely asserted. The remaining work is real but narrow engineering generalization (§34) — the next phase is implementation-ready.
