# Phase 4G.3A — Eight-Week Race-Core Allocation Audit

**Status: audit and decision-formalization only. No allocator was implemented. No public behavior changed.**

## 1. Scope and non-goals

This document audits whether, and under what conditions, a horizon-aware 8-week
`TEN_K__4D__INTERMEDIATE` race core can eventually be implemented safely. It does not implement it.
Current fail-closed behavior (8–11, 13–14 weeks → `PLAN_CORE_HORIZON_UNSUPPORTED`; 15+ weeks →
`PLAN_HORIZON_COMPOSITION_REQUIRED`; exactly 12 weeks → HTTP 200) is unchanged and re-verified live
and by test at the end of this document.

## 2. Already-canonical decisions — reused, with a citation correction

The task instruction asked this audit to treat three inputs (C-01 compression order, C-02 taper
minimum, C-03 conditional Foundation=1) as `ALREADY_CANONICAL`, citing
`appsel-v1-canonical-decisions.md §5`/`§6`. **That document has no §5 or §6** — it contains only
§A/§B(.1–.5)/§C(.1–.4)/§D, none of which mention compression order, taper-by-distance minimums, or a
Foundation-readiness rule. This is not a new problem: the document's own header describes an identical
prior incident (`PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` — a citation to a "doc13" that
turned out not to exist in-repo). Per that corrigendum's own established discipline ("do not guess...
mark DECISION_REQUIRED, not invented"), this audit does not treat an unverifiable citation as
canonical merely because it was labeled so. Each input was independently re-verified against real
repository artifacts:

| Input | Task's claim | Independent verification | Classification |
|---|---|---|---|
| C-01 compression order (Foundation→Build→RaceSpecific→Taper) | `ALREADY_CANONICAL`, source §6 | **§6 does not exist.** However, the real catalog artifact (`ten-k-master.v6.json`) independently encodes this exact order via each phase's `compressionPriority` (FOUNDATION=1, BUILD=2, RACE_SPECIFIC=3, TAPER=4) and TAPER's `isCompressionProtected: true` | `EVIDENCE_BACKED` (catalog-artifact-sourced, not doc-sourced as claimed) |
| C-02 taper minimum = 1 week for TEN_K | `ALREADY_CANONICAL`, source §5 | **§5 does not exist.** The real catalog artifact directly declares `TAPER.minimumWeeks = 1` | `EVIDENCE_BACKED` (catalog-artifact-sourced, not doc-sourced as claimed) |
| C-03 Foundation may reach 1 week only conditionally on readiness | `ALREADY_CANONICAL` | **No repo artifact of any kind states this.** The catalog declares `FOUNDATION.minimumWeeks = 2` unconditionally — there is no readiness-conditional override field or mechanism anywhere in the schema or code. `CORE_ENTRY_READINESS_IN` itself was left `BLOCKED_BY_PRODUCT_DECISION` by `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md §6`, later given real `READY`/`CAUTION`/`NOT_READY` thresholds by `PHASE4D_3_1_..._RESOLVER_ACTIVATION.md`, but that document never connects readiness to Foundation-phase-length at all | Two-part status — see §5a: `N8_STATUS: NOT_APPLICABLE_DUE_TO_CATALOG_MINIMUM_2`; `GENERAL_STATUS: OPEN_FOR_FUTURE_HORIZONS_OR_CANDIDATES_ALLOWING_FOUNDATION_1` |

The 5K/10K/HM/Marathon taper-minimum **table** (10K=1, Marathon=2, etc.) the task describes is also not
present anywhere in the repository under that shape — only the TEN_K candidate's own `TAPER.minimumWeeks
= 1` is real, in-repo, artifact-backed fact. The multi-distance table is not verified and is out of scope
(only TEN_K is audited here).

**This citation-correction does not change the audit's practical conclusions** — C-01 and C-02's
*substance* holds, just via a different (and stronger, because machine-readable) source. Only C-03's
substance is genuinely open, and §7 below shows it turns out not to matter for the exactly-8-week case.

## 3. Actual catalog phase bounds (`ten-k-master.v6.json`, read verbatim)

| Phase | minimumWeeks | preferredWeeks | maximumWeeks | compressionPriority | isCompressionProtected |
|---|---:|---:|---:|---:|---|
| FOUNDATION | 2 | 3 | 4 | 1 | false |
| BUILD | 3 | 4 | 5 | 2 | false |
| RACE_SPECIFIC | 2 | 4 | 4 | 3 | false |
| TAPER | 1 | 1 | 1 | 4 | true |

`coreCycle`: `minimumWeeks=8`, `defaultWeeks=12`, `maximumWeeks=14`. Sum of `preferredWeeks` = 3+4+4+1 =
12, matching `defaultWeeks` and the fixed allocation `CatalogPhaseAllocationResolver` always emits today
(confirmed live and by test — see §12).

Verified programmatically against the live artifact by
`Phase4G3AEightWeekCoreAllocationAuditTests.RealCatalogArtifact_TenKMasterV6_PhaseBounds_MatchAuditReconciliationTable`.

## 4. Canonical-versus-catalog reconciliation table

| Phase | Canonical minimum (task-asserted, unverifiable as cited) | Catalog minimum (real) | Catalog preferred | Catalog maximum | Conflict? |
|---|---:|---:|---:|---:|---|
| Foundation | 1 (conditionally) | 2 | 3 | 4 | **Yes** — catalog floor is 2, not 1; no conditional-override mechanism exists |
| Build | not asserted | 3 | 4 | 5 | No conflict (no competing claim) |
| Race-Specific | not asserted, "limited reduction" only | 2 | 4 | 4 | No numeric conflict; "limited" is directionally consistent with `compressionPriority=3` (compressed after Foundation/Build) |
| Taper | 1 | 1 | 1 | 1 | No conflict — matches exactly |

The one real conflict (Foundation) does not block 8-week feasibility — see §5.

## 5. Minimum-total calculation (Audit Question B)

`FoundationMin(2) + BuildMin(3) + RaceSpecificMin(2) + TaperMin(1) = 8`.

**The sum of actual catalog minimums equals exactly 8** — the target horizon — with **zero spare
weeks**. Verified programmatically by
`Phase4G3AEightWeekCoreAllocationAuditTests.RealCatalogArtifact_SumOfPhaseMinimums_EqualsEightExactly_NoSpareWeeks`.

This is the audit's central structural finding: **8 weeks is mathematically feasible, but only via the
single allocation `Foundation=2, Build=3, RaceSpecific=2, Taper=1` — every phase pinned to its own
catalog-declared minimum simultaneously.** There is no "spare weeks under canonical compression rules"
question to answer (§ Audit Question B's own final sub-question) because there are no spare weeks: the
compression-priority ordering (Foundation first, then Build, then Race-Specific, Taper protected) never
even gets exercised at exactly 8 weeks, since every phase is already forced to its floor. It would matter
for a 9–11-week core (where 1–3 spare weeks above the 8-week minimum would need distributing according to
that priority order) — out of scope for this audit and for Phase 4G.3B per the task's own non-goals.

**This is why C-03 (conditional Foundation=1) turns out not to matter for exactly 8 weeks**: the only
feasible split already uses Foundation=2 (the catalog's real, unconditional floor), never Foundation=1.
The task's assumed "spare-weeks-to-redistribute-toward-Foundation=1" scenario does not exist at N=8 under
the real catalog data.

### 5a. Clarification — N=8 mootness does not resolve the general C-03 question

`N8_STATUS: NOT_APPLICABLE_DUE_TO_CATALOG_MINIMUM_2`
`GENERAL_STATUS: OPEN_FOR_FUTURE_HORIZONS_OR_CANDIDATES_ALLOWING_FOUNDATION_1`

Stated explicitly, so a future pass cannot read §5's finding as broader than it is:

1. For the exact 8-week `TEN_K__4D__INTERMEDIATE` core specifically, the Foundation=1 readiness-gating
   question is **moot** — not answered, not disproven, simply inapplicable — because the catalog's own
   hard minimum for FOUNDATION is 2, and the unique 8-week allocation never asks for less than that.
2. **This does not prove or disprove the general product rule** that Foundation may be reduced to 1 week
   for sufficiently ready users. That rule was never tested against real data here; it was simply never
   *invoked* by this specific arithmetic case.
3. That broader rule has **no confirmed repository artifact support** anywhere — not in
   `appsel-v1-canonical-decisions.md` (no §5/§6, no such content anywhere in the document), not in the
   catalog schema (no conditional-override field exists), not in any `CORE_ENTRY_READINESS_IN` governance
   document (`PHASE4D_3_1_..._RESOLVER_ACTIVATION.md` defines READY/CAUTION/NOT_READY thresholds but never
   connects them to Foundation-phase-length). It remains exactly as unverified after this audit as before
   it — §5's finding narrowed the *scope* in which the question matters, it did not answer the question.
4. **If a future horizon (e.g. 9, 10, or 11 weeks — where genuine spare weeks exist above the 8-week
   minimum), a future candidate, or a future catalog revision changes `FOUNDATION.minimumWeeks` below 2 or
   otherwise permits Foundation=1**, the relationship between that allocation and `CORE_ENTRY_READINESS_IN`
   **must be audited again from scratch** before enabling it — this audit's N=8 finding does not transfer.
5. **Phase 4G.3C/4G.3D (or any future phase) must not treat the general Foundation=1/readiness question as
   already resolved merely because it was irrelevant for N=8.** `GENERAL_STATUS` remains `OPEN` until an
   explicit product decision and repository artifact exist.

## 6. `CORE_ENTRY_READINESS_IN` flow audit (Audit Question C)

1. **Is it already resolved for catalog preview generation?** Yes. `CoreEntryReadinessResolver` is
   registered in production DI (`Program.cs`) as `ICoreEntryReadinessResolver`, and
   `RuntimeConditionResolutionService.ResolveAllResults` (which includes it) is invoked by
   `CatalogPreviewGenerator.GenerateAsync` for every catalog-routed request today — including today's
   exactly-12-week requests.
2. **Is its result available at phase-allocation time?** Yes, in scope: the same `conditionResults` list
   returned by `ResolveAllResults` is threaded into `BuildDarkInternalDatedSkeleton` →
   `CatalogPlanSkeletonOrchestrationContext.ConditionResults`, and `CatalogPlanSkeletonOrchestrator.Build`
   calls `_phaseAllocationResolver.Resolve(candidate)` from within that same method, after
   `ConditionResults` is already populated on the context.
3. **Is it currently consumed by any phase-allocation or cycle-compression logic?** No.
   `ICatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary candidate)` takes exactly one
   parameter — the candidate — confirmed by reflection
   (`CatalogPhaseAllocationResolver_Resolve_TakesOnlyCandidate_NoCycleLengthOrReadinessParameter`) and by
   a same-input/same-output determinism test
   (`CatalogPhaseAllocationResolver_Resolve_IsPureFunctionOfCandidate_...`, asserts `TotalWeeks == 12`
   regardless of any hypothetical request context, since none can reach it).
4. **Can `CatalogPhaseAllocationResolver` receive it without duplicating resolver logic?**
   **Yes.** This is the task's own headline conditional instruction, and the answer determines whether
   this must be classified `IMPLEMENTATION_BLOCKER`. It does **not** apply: `CORE_ENTRY_READINESS_IN`'s
   result is computed exactly once per request, by the existing `RuntimeConditionResolutionService`
   pipeline, and is already in-scope (as `conditionResults`) at the exact call site
   (`CatalogPlanSkeletonOrchestrator.Build`) where `_phaseAllocationResolver.Resolve(candidate)` is
   invoked. Threading it through requires a **signature/wiring change** (adding a parameter to `Resolve`,
   or to the orchestrator context passed into it) — not a second, independent readiness evaluation. **No
   second readiness-evaluation path is required.**
5/6. **What readiness result should permit Foundation=1, and what should happen for READY/CAUTION/
   NOT_READY?** See §7 — this question turns out to be inapplicable for exactly 8 weeks (Foundation is
   always 2, never 1, at N=8), so it is reframed as a whole-core eligibility gate rather than a
   phase-split selector.

## 7. Readiness eligibility table (Audit Question C, mandatory table)

| `CORE_ENTRY_READINESS_IN` | Foundation minimum allowed | 8-week eligibility | Reason |
|---|---:|---|---|
| `READY` | 2 (catalog floor — Foundation=1 is never needed at N=8) | Structurally eligible | Sufficient base readiness is exactly the condition `CORE_ENTRY_READINESS_IN=READY` already certifies; a runner starting an 8-week cycle with zero slack in any phase should have that certified base |
| `CAUTION` | 2 (same — no allocation ever uses Foundation=1) | `DECISION_REQUIRED` | The 8-week core has *zero slack anywhere* (§5) — Foundation is already at its structural floor with no room to add margin for a partially-evidenced runner. Whether "partial evidence, ambiguous band" (CAUTION's actual meaning per `PHASE4D_3_1`) is safe to combine with zero-slack compression is a genuine product-safety judgment this audit does not have standing to make unilaterally |
| `NOT_READY` | 2 (same) | Not eligible (recommended) | `NOT_READY` means `RecentWeeklyVolumeKm < 8` or `RecentLongestRunKm < 4` (or both missing in a race context) — offering a zero-slack, all-phases-at-floor 8-week cycle to a runner already below the resolver's own base-fitness gate is not defensible without an explicit product override, which does not exist |

**Reframing, stated explicitly:** because Foundation is always 2 (never 1) at exactly 8 weeks, readiness
does not select *which* phase split to use — there is only one. Readiness instead gates **whether the
whole zero-slack 8-week core is offered at all.** This reframing is itself a product decision this audit
surfaces but does not make; the `READY`-only recommendation above is offered as a starting default, not
an approved threshold.

**Classification:** `DECISION_REQUIRED` (CAUTION/NOT_READY eligibility), `EVIDENCE_INFORMED_PRODUCT_CONSTRAINT`
(READY eligibility recommendation — informed by the zero-slack structural finding, not externally sourced).

## 8. Feasible phase-allocation candidates (Audit Question D)

Given catalog bounds (§3) and the exact 8-week target, enumerate every combination of
`(F,B,RS,T)` with `F∈[2,4], B∈[3,5], RS∈[2,4], T=1` (Taper is fixed: min=preferred=max=1) summing to 8:

- The minimum sum alone (2+3+2+1) already equals 8. Any increase to one phase within its own
  `[min,max]` band requires an equal decrease elsewhere, but every phase is already at its floor —
  there is no room to increase any phase without violating another phase's own minimum.
- **Exactly one candidate exists: `F=2, B=3, RS=2, T=1`.**

Outcome: **A — one allocation is clearly valid** (mathematically; structural, not a safety approval).

- Catalog bounds valid? Yes — each value sits exactly at its own `minimumWeeks`.
- Canonical compression order respected? Yes, trivially — every phase is already at its floor, so no
  compression *decision* (which phase to reduce next) is ever exercised; the priority ordering is moot
  at this exact total.
- Readiness requirement? See §7 — `DECISION_REQUIRED` for CAUTION/NOT_READY.
- Race-specific minimum preserved? Yes — RS=2 equals its own catalog minimum, not below it.
- Stage inventory sufficient? Plausible, unverified — see §10.
- Taper preserved? Yes — T=1, unchanged from every other supported horizon, `isCompressionProtected=true`
  honored trivially (never asked to compress below its own fixed value of 1).
- Progression continuity plausible? Plausible for volume/long-run (see §13); untested for the
  fine-grained stage scheduler at RS=2 (see §10).

**Exact split classification:** `MATHEMATICALLY_DETERMINED_FROM_CANONICAL_CATALOG_CONSTRAINTS` (two-axis
form: evidence basis `NOT_AN_EVIDENCE_CLAIM`; decision status `DETERMINED_BY_ACTIVE_CATALOG_CONSTRAINTS`).
**Not** `EVIDENCE_BACKED`, and not `EXPLICIT_PRODUCT_DEFAULT` either — the split is neither a scientific
claim nor a chosen preference among alternatives; it is the sole arithmetic solution given the catalog's
own currently-active minimums. To be precise about what *is* and *is not* evidence-backed here: the
**phase sequence and training direction** (Foundation→Build→RaceSpecific→Taper, compression priority,
taper protection) are genuinely `EVIDENCE_BACKED` — real catalog-artifact fields, per §2. The **exact
numeric split 2/3/2/1** is not itself a training-science prescription and carries no such claim — it is
simply the one combination the currently-active catalog constraints permit for a 8-week total. If those
constraints change (e.g. a future `FOUNDATION.minimumWeeks` revision), the "determined" split changes with
them — it was never an independent product choice to begin with.

## 9. Compression-order validation

Already addressed in §8: at exactly 8 weeks, compression order is **never exercised as a decision** —
every phase is simultaneously at its floor, so there is nothing left to choose between reducing Foundation
vs. Build vs. Race-Specific. The compression-priority ordering (Foundation→Build→RaceSpecific→Taper,
catalog-evidenced per §2) remains correct and relevant for any *future* 9–11-week work (where genuine
spare-weeks-to-distribute decisions exist) but is not a live decision point for the 8-week case itself.

## 10. Race-Specific capacity finding (Audit Question E)

**Original finding** (`PHASE4F_6A_FINE_GRAINED_KEY_SESSION_STAGE_SCHEDULER.md`, §"Design decisions"):
RACE_SPECIFIC's real v10 stage data (`TEN_K_SPECIFIC_INTRO` min1/max2/Extendable,
`GOAL_PACE_REHEARSAL` min1/max2/Protected/FixedExposure with a `CURRENT_FITNESS_SPECIFIC_REHEARSAL`
fallback, min1/max1) "needs 4 weeks from a combined minimum of only 2" — i.e., at RACE_SPECIFIC's
*preferred* allocation of 4 weeks (today's only live value), the two active stages' combined minimum (2)
is 2 weeks short, requiring the phase-capacity **extension** algorithm to distribute 2 surplus weeks. The
doc explicitly flags: *"the margin is currently exactly zero... any future change to RACE_SPECIFIC's...
week allocation should be re-verified against this scheduler before being accepted."*

**Current code/catalog state:** confirmed unchanged (`ten-k-master.v6.json` RACE_SPECIFIC still
min=2/preferred=4/max=4; `ten-k-workout-progression.v5.json` still declares the same three RACE_SPECIFIC
stages with the same min/max/behavior flags).

**Relevance to 8-week compression, precisely traced:** the original finding is about the **extension**
direction at RACE_SPECIFIC=4 (today's only live value) — 2 active stages' minimum (2) vs. 4 allocated
weeks, needing 2 surplus weeks distributed. For 8-week **compression**, RACE_SPECIFIC would instead be
allocated exactly 2 weeks (its catalog minimum, per §8) — which happens to **exactly match** the same two
active stages' combined minimum (2) with **zero** surplus or deficit. This is a different, unexercised
arithmetic case: no extension algorithm invocation needed, and (if `FixedExposure` compression treats
minimums as floors, per the existing compression rule "reduce ONLY stages whose behavior PERMITS
reduction, down to a floor of 1 exposure") no compression algorithm invocation needed either — every stage
would sit at exactly its own 1-exposure minimum.

**Blocker status:** **not extension-only** (correcting a premature assumption) — it is relevant to *both*
directions of RACE_SPECIFIC's week allocation, but the specific 8-week (RS=2) case is a clean, exact fit
that has simply **never been run through the fine-grained scheduler or its test suite** (the doc's own
explicit warning: zero margin, must be re-verified before being accepted). Classification: `DECISION_REQUIRED`
→ specifically, `IMPLEMENTATION_BLOCKER` in the narrow sense of "must be verified/tested before Phase
4G.3B can trust it," not in the sense of "structurally impossible." Not resolved elsewhere in the repo.

## 11. Stage reachability (Audit Question F)

| Phase | Allocated weeks (8-week case) | Required stages | Reachable? | Missing/duplicated behavior |
|---|---:|---|---|---|
| Foundation | 2 (floor) | `EASY_STANDARD`-family + `LONG_RUN` (per `eligibleWorkoutFamilies: [EASY, LONG_RUN]`) | Plausible — no stage-count-vs-weeks conflict found; not independently traced stage-by-stage in this pass | Unverified in fine detail |
| Build | 3 (floor) | `EASY`/`LONG_RUN`/`QUALITY` families (Fartlek/Threshold-Tempo-class); 3 is already a value the catalog's own `minimumWeeks` permits at any horizon | Plausible — 3 is a real, already-declared valid Build length (not a new value being invented) | Unverified against the fine-grained scheduler at exactly 3 weeks |
| Race-Specific | 2 (floor) | `TEN_K_SPECIFIC_INTRO` + (`GOAL_PACE_REHEARSAL` or fallback `CURRENT_FITNESS_SPECIFIC_REHEARSAL`), each min1 | Plausible, exact stage-minimum fit (§10) | **Never run through `ProgressionStageAllocator`/its 27-test suite — genuinely untested, not merely "assumed fine"** |
| Taper | 1 (unchanged) | `TAPER_SHARPEN` | Reachable — identical to every currently-supported horizon; no change at all |

No hidden C# workout substitution was found or is implied by any of the above — every stage referenced is
an existing, named catalog artifact (`ten-k-workout-progression.v5.json`). This table intentionally does
not claim final confirmation for Foundation/Build/Race-Specific — that requires actually running
`ProgressionStageAllocator` against an 8-week `CatalogPlanSkeletonOrchestrationContext`, which is
implementation/test work reserved for Phase 4G.3B, not this audit.

## 12. `GOAL_PACE_REHEARSAL` reachability (Audit Question G)

From `ten-k-workout-progression.v5.json`: `GOAL_PACE_REHEARSAL` — `minimumExposures=1`,
`maximumExposures=2`, `compressionBehavior=PROTECTED`, `extensionBehavior=FIXED_EXPOSURE`,
`requires: [{"conditionType":"GOAL_FEASIBILITY_IN","allowedValues":["REALISTIC","CHALLENGING"]}]`,
`fallbackStageKey: CURRENT_FITNESS_SPECIFIC_REHEARSAL`.

- **1. READY + product_average:** product-average target times are classified `CHALLENGING` (per this
  session's own prior fix, `PHASE4D_4_1_PRODUCT_AVERAGE_TARGET_TIME_GOAL_FEASIBILITY_CLASSIFICATION.md`)
  → satisfies `requires` → `GOAL_PACE_REHEARSAL` reachable.
- **2. READY + user_defined + evidence:** feeds the real Riegel-based feasibility ratio → `REALISTIC` or
  `CHALLENGING` depending on the gap → reachable if within either band, else falls back.
- **3. READY + user_defined without evidence:** no independent evidence to classify against → `NotEvaluated`
  (governance-policy exception today, unrelated to core-entry readiness) — not a `GOAL_PACE_REHEARSAL`
  question specifically.
- **4/5. CAUTION / NOT_READY:** `GOAL_FEASIBILITY_IN` and `CORE_ENTRY_READINESS_IN` are **independent**
  resolvers (confirmed: neither reads the other's `ResolverInputSnapshot` fields) — a CAUTION/NOT_READY
  core-entry-readiness result has no bearing on `GOAL_PACE_REHEARSAL`'s own `REALISTIC`/`CHALLENGING` gate.

**Conclusion:** `GOAL_PACE_REHEARSAL` remains **mandatory** (min exposure 1, `PROTECTED` — never reduced
by compression) whenever goal feasibility is `REALISTIC`/`CHALLENGING`, and substitutes to
`CURRENT_FITNESS_SPECIFIC_REHEARSAL` otherwise — this behavior is **horizon-independent by catalog
design** (the `PROTECTED` flag exists precisely so compression can never remove it). No product decision
is needed to omit it at 8 weeks — the catalog already guarantees it is never omitted, only substituted per
existing, unrelated (`GOAL_FEASIBILITY_IN`) rules. Classification: `ALREADY_CANONICAL` (catalog-artifact-sourced).

## 13. Workout exposure counts (Audit Question H)

8-week, 4-day plan: 32 total sessions = 8 `KEY_SESSION` + 16 `EASY_SUPPORT` + 8 `LONG_RUN` (the same
1/2/1 weekly role shape used at every currently-supported horizon — role counts are a per-week layout
property, not phase-length-dependent, so this scales linearly and introduces no new structural question).
Exact per-workout-key exposure counts (e.g. how many `THRESHOLD_TEMPO` vs. `GOAL_PACE_TEN_K` sessions)
depend on the stage schedule derived in §11/§12, which is not yet run for this horizon — reported here as
`DEFERRED` to Phase 4G.3B's actual scheduler run, not invented.

## 14. Volume and long-run progression contracts (Audit Question I)

`CatalogVolumeAndLongRunPlanner` was inspected directly: it derives its denominators from
`request.BoundPlan.Weeks.Count`, filters by `PhaseKey != "TAPER"`, and indexes via `FindIndex`/relative
position (`nonTaperWeeks.Count - 1` as denominator) — **not** a hardcoded `week == 12` or 12-entry-array
assumption. Peak-week placement, taper reduction, and the starting-volume policy
(`V1MissingReadinessStartingVolumePolicy`, `PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md`) are all
structurally relative to whatever `Weeks.Count` the bound plan actually has.

**However:** the specific numeric rules (max weekly increase 7%/8%, `TaperMultiplier=0.53`, peak-volume
bands) were calibrated and tested only against 12-week golden-fixture data — their *safety* at an 8-week
compression (steeper effective progression per week, since the same total build must happen faster) is
**not evidenced or tested**, only structurally not-hardcoded-against-12. Required contracts for future
implementation:

| Rule | Status |
|---|---|
| Starting-volume ownership (`V1MissingReadinessStartingVolumePolicy`) | `ALREADY_CANONICAL` (Phase 4F.7B.2) — read-in structurally, untested at N=8 |
| Maximum weekly increase (7%/8% intermediate bands) | `ALREADY_CANONICAL` value, `DECISION_REQUIRED` whether it remains safe over a compressed 8-week ramp |
| Long-run increase constraint | `ALREADY_CANONICAL` mechanism, untested at N=8 |
| Peak-placement window | Relative-position-derived (not hardcoded); untested at N=8 |
| Taper reduction rule (`TaperMultiplier=0.53`) | `ALREADY_CANONICAL` value; applies to whichever week is `IsTaperWeek`, so mechanically compatible with an 8-week Taper=1 — untested |
| Transition rules by readiness state | Not present at all today — `DECISION_REQUIRED` |

Classification: `EVIDENCE_BACKED` for the mechanism (relative, not hardcoded), `DECISION_REQUIRED` for
whether the existing numeric constants remain safe at this compression ratio.

## 15. Race-date alignment invariant for 8 weeks (Audit Question J)

For `StartDate=2026-07-20`, `RaceDate=2026-09-14`, `PreferredDays=Mon/Wed/Fri/Sun`, `LongRunDay=Sun`:
8 weeks × 7 days = 56 days; `StartDate + 55 days = 2026-09-13` (a Sunday, the `LongRunDay`) —
**exactly one day before `RaceDate`**, matching the exact same established convention already proven for
the 12-week case (`StartDate=2026-07-20`/`RaceDate=2026-10-12` → final session `2026-10-11`).

**Is `CatalogRaceDateAlignmentInvalidException`'s activated invariant (final session within 7 days before
RaceDate, never after) genuinely horizon-agnostic, or does it depend on the fixed 12-week allocation?**
**The rule's formula is horizon-agnostic** — `EndDate = StartDate + N*7 - 1`, compared against `RaceDate`,
never references `N=12` specifically. **Its only proof of correctness today is horizon-*coincidental***:
it has only ever been exercised (live, and by the `Phase4G.2` edge-case test) with `N` fixed at 12 by the
allocator, matched against 12-week requests. It has never been exercised against a genuinely-variable
allocator output at `N=8`. Once a real 8-week allocator exists, this exact invariant (no code change
needed) should immediately and correctly validate an 8-week plan's alignment — this is a positive finding:
**the alignment invariant is ready for Phase 4G.3B as-is**, unlike the phase-allocation/loader layers.

Confirmed rule for future implementation: exactly 8 plan weeks; 32 run sessions; first window starts at
`StartDate`; final training session on `RaceDate - 1` day (on the `LongRunDay`, per the established
Sunday-before-Monday-race convention); no session after `RaceDate`; Taper is Week 8; no persisted rest
rows (unchanged, synthetic-rest-at-read-time convention from the Home-endpoint work).

## 16. Final 8-week eligibility classification (Audit Question K)

**D. `NOT_YET_SUPPORTABLE`** — not because the arithmetic is infeasible (§5/§8 determine it uniquely is: 8
weeks = `F2/B3/RS2/T1`, no alternative — a `MATHEMATICALLY_DETERMINED_FROM_CANONICAL_CATALOG_CONSTRAINTS`
result, not an evidence claim, per §8), but because:

- The candidate-summary/loader layer does not carry the catalog fields (`minimumWeeks`, `maximumWeeks`,
  `compressionPriority`, `isCompressionProtected`) a horizon-aware allocator needs at all (§17,
  `IMPLEMENTATION_BLOCKER`).
- `CatalogPhaseAllocationResolver.Resolve` cannot receive a target week count or a readiness signal
  without a signature change (§17, `IMPLEMENTATION_BLOCKER`, but explicitly **not** requiring a second
  readiness-evaluation path — see §6.4).
- The RACE_SPECIFIC=2 stage-compression path, while an exact arithmetic fit, has never been run through
  `ProgressionStageAllocator`/its test suite (§10/§11, `DECISION_REQUIRED`/needs-verification).
- Volume/long-run numeric constants are untested at this compression ratio (§14, `DECISION_REQUIRED`).
- Readiness-based whole-core eligibility (§7) is an open product decision, not yet approved for any tier.

Fail-closed (current `PLAN_CORE_HORIZON_UNSUPPORTED` behavior) remains the correct, evidence-respecting
choice until these are addressed.

## 17. Required implementation changes (for Phase 4G.3B, not built here)

1. Extend `PlanCatalogPhaseAllocation`/`PlanCatalogBundleLoader`/`PlanCatalogCandidateSummary` to load
   `minimumWeeks`, `maximumWeeks`, `compressionPriority`, `isCompressionProtected` per phase from the raw
   catalog JSON — currently only `preferredWeeks` is loaded.
2. Change `ICatalogPhaseAllocationResolver.Resolve` to accept a target week count (and, pending §7's
   product decision, a `CORE_ENTRY_READINESS_IN` result or an eligibility flag derived from it) —
   confirmed feasible without duplicating resolver evaluation (§6.4).
3. Run `ProgressionStageAllocator` against a synthetic `F2/B3/RS2/T1` allocation to verify stage
   reachability for real (§10/§11) before trusting it.
4. Verify (or explicitly re-derive) volume/long-run numeric safety at an 8-week compression ratio (§14).
5. Obtain an explicit product decision on whole-core readiness eligibility (§7 table) — recommend `READY`-
   only as the starting default, pending that decision.

## 18. Blockers

- `IMPLEMENTATION_BLOCKER`: catalog-field loader gap (§17.1).
- `IMPLEMENTATION_BLOCKER` (verification, not architecture): RACE_SPECIFIC stage-scheduler untested at
  RS=2 (§10/§11).
- **Not** an `IMPLEMENTATION_BLOCKER`: threading `CORE_ENTRY_READINESS_IN` into phase allocation — this
  audit's central answer to the task's own opening conditional instruction. The data is already computed
  once and in scope; only a signature/wiring change is needed, so no second readiness-evaluation path is
  required.

## 19. Product decisions still required

- Whole-core readiness eligibility (§7): is 8-week core `READY`-only, `READY`+`CAUTION`, or something
  else?
- Whether the existing volume/long-run numeric constants (7-8% weekly cap, `TaperMultiplier=0.53`) remain
  approved at an 8-week compression ratio, or need distinct values (§14).
- Whether a genuine "Appsel V1 Canonical Decisions" document with the taper-by-distance table and named
  compression-order section exists outside this repository and should be imported (§2 — same open item
  `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md` already flagged for product, still unresolved).

### 19a. Cross-phase risk/governance mechanism check (recommendation only, not acted on)

The repository **does** have an established living aggregator for exactly this kind of cross-phase,
non-blocking-but-activation-relevant open question: `plan-catalog/artifacts/audits/activation-readiness-risks.json`
(+ companion `.md`). Confirmed conventions, read directly from the file before making any recommendation:

- Explicitly documented as append-only ("This file is additive across passes — future passes should
  append new risk entries here rather than creating a new aggregator") and `"readOnly": true` at the
  document level, reviewed (not necessarily edited) before any publish/activate/release action.
- Each entry has a stable `id` (pattern: `TD-<AREA>-<NNN>`, e.g. `TD-CORE-READINESS-001`), `title`,
  `recordedInPass`, `source` (file citations), `statement`, `classification`, `severity`, `affectedAreas`,
  `requiredResolution`, `blocking` (bool), `appliesToCandidateRootsFrom`, and `status` (`OPEN`/`CLOSED`).
  Some entries additionally carry `resolutionNote`/`closureNote`/`implementationNote` for later
  partial-progress updates — but the file's own established convention (per `TD-WAVE5-001` and
  `TD-CORE-READINESS-001`'s own precedent) is to **never mechanically close a risk whose resolution has
  not been exercised against real, live traffic** — a directly relevant precedent for this audit's own
  findings, since none of Phase 4G.3A's blockers have been exercised live either.
- **`TD-CORE-READINESS-001`** (already tracked there) is the closest existing entry: it tracks whether
  `CORE_ENTRY_READINESS_IN` thresholds are approved and wired — but its scope is the resolver's approval/
  wiring status in general, not the specific Foundation-phase-length question this audit raises. They are
  related but distinct questions.

**Recommendation (not acted on in this pass):** the general, still-open C-03 question
(`GENERAL_STATUS: OPEN_FOR_FUTURE_HORIZONS_OR_CANDIDATES_ALLOWING_FOUNDATION_1`) is exactly the kind of
cross-phase risk this aggregator exists to make visible beyond a single audit document, and a new entry
(e.g. `TD-FOUNDATION-COMPRESSION-001`) following the established field/status conventions above would be
a reasonable candidate for a future pass to add. This audit does **not** add that entry itself — the task
scope for this documentation-only follow-up was limited to this file, and a new risk entry is a
substantive addition to a separate, actively-relied-upon governance artifact that deserves its own
deliberate pass rather than being folded silently into an unrelated correction. Left as an explicit
recommendation only.

## 20. Evidence/governance classification summary

| Item | Classification |
|---|---|
| Compression order (Foundation→Build→RaceSpecific→Taper) | `EVIDENCE_BACKED` (catalog `compressionPriority`/`isCompressionProtected`, not the cited doc section) |
| Taper minimum = 1 week (TEN_K) | `EVIDENCE_BACKED` (catalog `TAPER.minimumWeeks`, not the cited doc section) |
| Foundation=1-only-if-ready — N=8 | `NOT_APPLICABLE_DUE_TO_CATALOG_MINIMUM_2` (§5a) |
| Foundation=1-only-if-ready — general rule | `DECISION_REQUIRED`, `GENERAL_STATUS: OPEN_FOR_FUTURE_HORIZONS_OR_CANDIDATES_ALLOWING_FOUNDATION_1` — no artifact supports it; N=8 mootness does not resolve it; must be re-audited before any future horizon/candidate permits Foundation=1 (§5a) |
| Exact 8-week phase split (`F2/B3/RS2/T1`) | `MATHEMATICALLY_DETERMINED_FROM_CANONICAL_CATALOG_CONSTRAINTS` (evidence basis: `NOT_AN_EVIDENCE_CLAIM`; decision status: `DETERMINED_BY_ACTIVE_CATALOG_CONSTRAINTS` — not `EVIDENCE_BACKED`, see §8) |
| Phase sequence (Foundation→Build→RaceSpecific→Taper) | `EVIDENCE_BACKED` (catalog phase order + `compressionPriority`) |
| Race-specific-near-competition (GOAL_PACE_REHEARSAL late-phase placement) | `EVIDENCE_BACKED` (catalog `relativeOrder`/phase membership) |
| Exact workout exposure counts by key | `DEFERRED` (requires an actual scheduler run, §13) |
| `CORE_ENTRY_READINESS_IN` → Foundation mapping | `DECISION_REQUIRED` (reframed as whole-core eligibility, §7) |
| `CORE_ENTRY_READINESS_IN` → phase-allocation wiring feasibility | `EVIDENCE_BACKED` — feasible without duplicating resolver logic (§6.4) |
| Race-date alignment invariant readiness for 8 weeks | `EVIDENCE_BACKED` — formula-ready, empirically unexercised at N=8 (§15) |
| RACE_SPECIFIC=2 stage reachability | `DECISION_REQUIRED` / needs verification (§10/§11) |
| Volume/long-run numeric safety at 8-week ratio | `DECISION_REQUIRED` (§14) |

## Live/test re-verification that current fail-closed behavior is unchanged

- Live: 8-week request (`StartDate=2026-07-20`/`RaceDate=2026-09-14`) → HTTP 422,
  `PLAN_CORE_HORIZON_UNSUPPORTED`. 12-week request (`RaceDate=2026-10-12`) → HTTP 200, unchanged.
- Full backend suite: 1057/1059 pass (2 pre-existing, unrelated `CatalogLivePilotOptions`-default
  failures, present before this task) — zero regressions, zero new production-code behavior changes (only
  a new documentation artifact and 5 new audit-protection tests were added).

**Documentation-only follow-up pass (this correction):** re-checked `Phase4G3AEightWeekCoreAllocationAuditTests.cs`
for any assertion on the string `"EVIDENCE_BACKED"` or on C-03's old phrasing/status — none exists; every
`Assert` in that file is structural (phase-bound values, sum-of-minimums, resolver signature, resolver
determinism, missing loader properties), not a classification-label string match. No test change was
required or made. Full backend suite rerun: unchanged, 1057/1059 pass, same 2 pre-existing failures. Live
8-week request re-verified: HTTP 422, `PLAN_CORE_HORIZON_UNSUPPORTED`, unchanged. Only this `.md` file was
changed in this pass.
