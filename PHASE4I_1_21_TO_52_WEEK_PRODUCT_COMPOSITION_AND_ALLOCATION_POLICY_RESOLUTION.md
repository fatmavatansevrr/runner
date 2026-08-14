# Phase 4I.1 — 21–52 Week Product Composition and Allocation Policy Resolution

## 1. Executive result

**Approved.** For `21 <= AvailableFullWeeks <= 52`:

```
LongHorizonGeneralEnduranceWeeks = AvailableFullWeeks - 20
PreparationRunwayWeeks           = 8   (fixed)
CoreWeeks                        = 12  (fixed)
```

This is the direct, forced arithmetic consequence of two facts already
approved by Phase 4G.6A.1 — `MaximumRunwayWeeks = 8` and
`PreferredCoreWeeks = 12` — combined with the required 20-to-21-week
continuity invariant. No new domain/scientific claim was invented. This
phase is **decision/governance only**: no production runtime, resolver,
materializer, catalog workout, schema, migration, preview, confirmation,
persistence, backend, or Flutter file was changed. Runtime activation for
21–52 weeks remains fully blocked, unchanged, by the pre-existing
`TD-GENERAL-ENDURANCE-STAGED-PLAN-001` (still `OPEN`).

```
21_TO_52_WEEK_PRODUCT_COMPOSITION_POLICY_APPROVED
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_LONG_HORIZON_SAFETY_DELOAD_AND_PROGRESSION_POLICY
```

## 2. Decision scope

Composition/allocation formula only — how the total available horizon
divides into Long-Horizon General Endurance, Preparation Runway, and Core.
No runtime generation, no General Endurance workout content, no
confirmation/persistence change, no public preview activation, no
progression/deload/numeric-loading rules, no Flutter change. All of these
are explicitly out of scope per the originating prompt and were not
implemented.

## 3. Inherited 8–20 behavior

- **8–14 weeks** → Core-only plan (`PLAN_CORE_HORIZON_UNSUPPORTED` for 13–14
  today; 12 weeks is the only live HTTP 200 path). Unchanged.
- **15–20 weeks** → Preparation Runway + 12-week Core, approved as direction
  by Phase 4G.6A.1 but still dark/unwired (`PLAN_HORIZON_COMPOSITION_REQUIRED`
  for any 15+ week public request). `RunwayWeeks = AvailableFullWeeks - 12`,
  giving 3→8 runway weeks for 15→20. Unchanged by this phase.
- The approved 8-week runway allocations (`CONSISTENCY_NEEDED`: 2
  Consistency + 5 Runway General Endurance + 1 Pre-Specific Transition;
  `CORE_ENTRY_READY`: 5 Runway General Endurance + 2 Aerobic Strength + 1
  Pre-Specific Transition — Phase 4G.6A.2/4G.6A.2A) are unchanged and are
  explicitly reused by this decision's profile-content rule (§13).
- The existing 12-week Core (Foundation 3 / Build 4 / Race Specific 4 /
  Taper 1, `ten-k-master.v6.json`) is unchanged.

## 4. Evidence reviewed

`PHASE4G_6A_1_TEN_K_HORIZON_POLICY_AND_DATE_AUTHORITY.md` (the direct
predecessor decision — approved `MaximumRunwayWeeks=8`,
`PreferredCoreWeeks=12`, the 21-52 `GENERAL_ENDURANCE → PREPARATION_RUNWAY →
RACE_CORE` staged-architecture direction, the 52-week outer boundary, and
the `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW` reason name — all as approved
direction, not implemented capability); `PHASE4G_6A_2_TEN_K_RUNWAY_COEFFICIENT_AND_ELIGIBILITY_DECISION.md`
and `PHASE4G_6A_2A_...md` (approved 8-week runway allocations for both
readiness profiles); `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`
(`TD-ALLOCATION-PRIORITY-001`, `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — read
in full before authoring this phase's new entry); `ten-k-master.v6.json`
(canonical 12-week Core phase bounds, unchanged); `PreparationRunwayContracts.cs`
(`PreparationRunwayBlockType.GeneralEndurance` — the existing runway-block
enum member this decision's terminology choice must not collide with);
current live error taxonomy (`PLAN_HORIZON_COMPOSITION_REQUIRED`,
`PLAN_CORE_HORIZON_UNSUPPORTED`, confirmed via `AppExceptions.cs` and
several end-to-end test files — both unchanged, neither touched by this
phase).

## 5. Evidence-strength classification

- **A — Direct canonical evidence**: none new. The formula is not itself
  independently evidenced by new coaching/physiological literature in this
  phase.
- **B — Approved Appsel product default**: `MaximumRunwayWeeks = 8`,
  `PreferredCoreWeeks = 12` (both already `APPROVED_PRODUCT_DEFAULT`/
  canonical from Phase 4G.6A.1 and earlier).
- **C — Strong domain evidence**: none new in this phase.
- **D — Architecture/determinism rationale**: the formula's *shape*
  (`GE = AvailableFullWeeks - 20`, Runway/Core held fixed) is derived
  architecturally — it is the unique arithmetic result of holding B's two
  already-approved fixed maxima constant while satisfying the required
  monotonic continuity invariant (§9). This is the primary classification
  for this phase's decision.
- **E — Unsupported speculation**: explicitly avoided — see §20 non-claims.

No Appsel product default is presented as a scientific fact anywhere in
this decision; §20 states this explicitly.

## 6. Candidate policies considered

1. **Approved (this decision)**: `GE = N - 20`, Runway fixed 8, Core fixed
   12, for `21 <= N <= 52`.
2. **Runway continues shrinking/growing with N** (e.g., some function of N
   beyond 20) — considered and rejected (§7).
3. **Core varies with N** (e.g., a longer Core for longer horizons) —
   considered and rejected (§7).
4. **Readiness profile changes segment duration** (not just content) —
   considered and rejected (§7, §13).
5. **A different GE cap below 32** (e.g., a lower maximum with the excess
   simply unsupported) — considered; no evidence found requiring a cap
   below 32, and 32 is architecturally forced by the 20-week anchor plus
   the 52-week outer boundary already approved by Phase 4G.6A.1
   (`52 - 20 = 32`), so no independent decision was needed to select 32.

## 7. Rejected alternatives

- **Runway shrinking after 20 weeks** (e.g., `20 → 8 Runway + 12 Core`,
  `21 → 4 GE + 5 Runway + 12 Core`): explicitly rejected. This would remove
  three already-approved runway weeks for one extra available week — a
  non-monotonic, discontinuous product boundary with no evidence
  justifying it. No repository evidence was found requiring runway to
  shrink past 20 weeks; on the contrary, Phase 4G.6A.1 already fixed
  `MaximumRunwayWeeks = 8` as the pilot's own approved ceiling.
- **Core varying with horizon length**: no repository evidence found
  requiring Core to be anything other than the canonical 12-week template
  value at any horizon. Core's phase composition (Foundation 3/Build
  4/Race Specific 4/Taper 1) is a fixed, versioned catalog property
  (`ten-k-master.v6.json`), not a function of total plan length.
- **Profile changing segment duration**: no direct canonical evidence
  found requiring `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY` to receive
  different GE/Runway/Core durations. Both profiles' existing 8-week
  runway *content* allocations (Phase 4G.6A.2/4G.6A.2A) already differ
  without differing in duration — the same pattern is extended here rather
  than inventing a new one.
- **A GE cap other than 32, or treating 1 GE week as prohibited**: no
  evidence found for either. 1 GE week is explicitly approved with a
  narrowed semantic role (`ShortExtension`, §11), not prohibited.

None of the "reject/block" trigger conditions in the originating prompt's
PART 10 were found to hold.

## 8. Approved or blocked formula

**Approved**, unconditionally for `21 <= AvailableFullWeeks <= 52`:

```
CoreWeeks = 12
PreparationRunwayWeeks = 8
LongHorizonGeneralEnduranceWeeks = AvailableFullWeeks - 20
```

Invariant: `LongHorizonGeneralEnduranceWeeks + PreparationRunwayWeeks +
CoreWeeks = AvailableFullWeeks` (holds by construction, verified for the
full 21–52 range by `CoreIsAlwaysTwelveAcrossTheFullApprovedRange`/
`RunwayIsAlwaysEightAcrossTheFullApprovedRange`/theory-driven sum
assertions in `LongHorizonCompositionPolicyDecisionTests.cs`).

Monotonicity: increasing `AvailableFullWeeks` by one increases only GE by
exactly one; Runway/Core never move (verified by
`GeneralEnduranceIncreasesMonotonicallyByExactlyOnePerAdditionalWeek`).

## 9. 20→21 boundary

**Approved.** The preferred continuity invariant is adopted verbatim: for
`N` in `21..51`, `Plan(N+1) = one additional Long-Horizon General
Endurance week + the same terminal 20-week Runway/Core structure`, at the
structural-composition level. Concretely: 20 weeks = 8 Runway + 12 Core (0
GE, existing path); 21 weeks = 1 GE + the *same* 8 Runway + the *same* 12
Core — not a redistribution. The rejected alternative (runway shrinking
from 8 to 5 at 21 weeks to make room for 4 GE weeks) is explicitly
disallowed (§7).

## 10. Short GE extension semantics

**Approved.** 1–3 GE weeks → `ShortExtension`. Its role is explicitly
non-physiological-completeness: orientation into the long-horizon plan,
schedule continuity, maintenance of existing running base, preparation for
the existing 8-week runway, and preventing an abrupt horizon-composition
change at the 20/21 boundary. A 1-week GE allocation is explicitly **not**
described, here or anywhere in this decision, as independently producing a
complete physiological adaptation (see §20).

## 11. Full GE phase semantics

**Approved.** 4–32 GE weeks → `FullPhase`. This phase does **not** define
`FullPhase`'s internal structure, mesocycle shape, deload cadence, or
progression rules — that is entirely Phase 4I.2's scope (§22). `FullPhase`
here is a duration classification only, used to distinguish "enough weeks
to plausibly contain internal structure" from `ShortExtension`'s
orientation-only role — it is not itself a claim that such internal
structure has been designed.

## 12. 52-week maximum

**Approved as a composition envelope only.** 52 weeks = 32 GE + 8 Runway +
12 Core. Explicitly **not** approved as: one uninterrupted monotonic
progression, a permanent weekly load increase, repeated identical workouts
for 32 weeks, or one single mesocycle. Composition-envelope approval is
explicitly distinguished from runtime-safety-policy approval (§20); the
latter is deferred to Phase 4I.2 in full (mesocycle structure,
recovery/deload cadence, progression limits, rebaseline/checkpoint rules,
workout repetition constraints, GE→Runway transition loading, long-run
progression, safety gates).

## 13. Profile-duration decision

**Approved final rule: horizon determines segment duration; readiness
profile determines segment content only.** Both `CORE_ENTRY_READY` and
`CONSISTENCY_NEEDED` receive identical `GE = AvailableFullWeeks - 20`,
`Runway = 8`, `Core = 12` at every horizon in 21–52. No direct canonical
evidence was found requiring the Core boundary, or any segment's duration,
to move based on readiness profile — so per the prompt's own instruction
("do not allow the profile to move the Core boundary unless direct
canonical evidence requires it"), it does not move. This is structurally
proven in the governance test suite (`BothReadinessProfilesUseIdenticalSegmentDurations`
— the formula takes no profile parameter at all, which is itself the
proof duration cannot vary by profile).

## 14. Profile-content distinction

Segment **content** (unchanged from Phase 4G.6A.2/4G.6A.2A, not modified
by this phase):

- **`CONSISTENCY_NEEDED`**: stronger consistency/adherence emphasis,
  slower early progression, earlier recovery opportunities within GE
  (design intent for a future phase, not specified numerically here);
  existing 8-week runway allocation unchanged: 2 Consistency + 5 Runway GE
  + 1 Transition.
- **`CORE_ENTRY_READY`**: earlier controlled aerobic support within GE
  (design intent, not specified numerically here); no need to shorten the
  runway; existing 8-week runway allocation unchanged: 5 Runway GE + 2
  Aerobic Strength + 1 Transition.

## 15. Terminology decision

**Approved**, to prevent collision between the long-horizon segment and
the existing Preparation Runway block, both of which relate to "General
Endurance":

- **Long-horizon segment (new)**: internal type name
  `LONG_HORIZON_GENERAL_ENDURANCE`. User-facing label: **"General
  Endurance"** (the plain, product-approved label — chosen over "Base
  Development"/"Aerobic Foundation Extension" as the simplest option
  consistent with existing product voice; no repository precedent argued
  for either alternative over the plain label).
- **Existing Preparation Runway block (unchanged)**: internal type
  remains `PreparationRunwayBlockType.GeneralEndurance`
  (`PreparationRunwayContracts.cs`, confirmed unmodified — **not renamed**,
  since no compatibility analysis for renaming a persisted/referenced
  value was performed and none was requested). User-facing label:
  unchanged, "General Endurance."
- The two are **type-context-unambiguous** even though their user-facing
  labels may read identically: `LONG_HORIZON_GENERAL_ENDURANCE` is a
  segment-level identifier (would live on a future `TrainingWeek.SourceSegment`
  or equivalent), `PreparationRunwayBlockType.GeneralEndurance` is a
  block-level identifier scoped entirely inside the Preparation Runway
  segment. No existing persisted or referenced value was renamed.
  `TerminologyDoesNotCollideWithTheExistingRunwayGeneralEnduranceBlock`
  proves the two identifier strings remain textually distinct.

## 16. 53+ containment

**Retained, unchanged.** `AvailableFullWeeks >= 53` → rejected. Reason
name: reuses the **already-recorded** `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW`
from Phase 4G.6A.1, rather than adopting the prompt's alternative suggestion
(`LONG_HORIZON_EXCEEDS_SUPPORTED_ANNUAL_WINDOW`) — introducing a second name
for the same still-unimplemented concept would create exactly the kind of
duplicate authority this phase's own PART 6 forbids. Neither name is
implemented in code today; this decision records which one a future
implementation phase must use. Explicitly documented: 53+ rejection is a
**V1 product/system support boundary**, not a claim that training longer
than one year is inherently unsafe. No truncation to 52, no silent
StartDate shift, no waiting plan, no multiple annual plans, no
auto-rebaseline, no multi-season system — none of these exist anywhere in
the repository and none were added.

## 17. Typed decision shape for Phase 4I.3

Recorded (not implemented) for a future `LongHorizonCompositionDecision`
type:

```
LongHorizonCompositionDecision
- availableFullWeeks: int
- generalEnduranceWeeks: int
- preparationRunwayWeeks: int   // = 8
- coreWeeks: int                // = 12
- geDurationClassification: ShortExtension | FullPhase
- readinessProfile: ConsistencyNeeded | CoreEntryReady
- eligibility: SupportedLongHorizon | UnsupportedAboveAnnualWindow | NotApplicableBelowLongHorizonBoundary
- reasonCode: string            // e.g. PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW when Unsupported
- policyId: "TD-LONG-HORIZON-COMPOSITION-001"
- policyVersion: string
```

A future implementation phase must consume this one typed authority rather
than re-deriving or duplicating the formula across eligibility, preview,
materialization, persistence, or Flutter — recorded here as the required
constraint for Phase 4I.3, not built here.

## 18. Canonical fixture table

| AvailableFullWeeks | GE | Runway | Core | Classification |
|---|---|---|---|---|
| 20 | 0 | 8 | 12 | Existing 15–20 path (unchanged) |
| 21 | 1 | 8 | 12 | ShortExtension |
| 22 | 2 | 8 | 12 | ShortExtension |
| 23 | 3 | 8 | 12 | ShortExtension |
| 24 | 4 | 8 | 12 | FullPhase |
| 28 | 8 | 8 | 12 | FullPhase |
| 32 | 12 | 8 | 12 | FullPhase |
| 40 | 20 | 8 | 12 | FullPhase |
| 48 | 28 | 8 | 12 | FullPhase |
| 52 | 32 | 8 | 12 | FullPhase |
| 53 | — | — | — | Unsupported, no allocation |

Every row except 20 and 53 is directly asserted by
`ApprovedFormula_ProducesExpectedAllocationForEachFixture` in
`LongHorizonCompositionPolicyDecisionTests.cs`.

## 19. Governance artifacts changed

`plan-catalog/artifacts/audits/activation-readiness-risks.json`: added
`TD-LONG-HORIZON-COMPOSITION-001` (status `CLOSED`), updated the
`currentAppendOnlyStatus` aggregate sentence (19→20 total, 9→10 closed).
`plan-catalog/artifacts/audits/activation-readiness-risks.md`: added the
matching table row plus a new "21-52 week composition/allocation formula
closure (Phase 4I.1)" prose section, updated the aggregate line (19/10/9 →
20/10/10). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` itself was **not**
modified in either file — it remains `OPEN`, cross-referenced by the new
entry, unaffected in scope. No parallel policy registry was created — the
existing established convention was reused, per the prompt's explicit
instruction.

## 20. Tests

New file: `plan-catalog/tests/PlanCatalog.Tests/Architecture/LongHorizonCompositionPolicyDecisionTests.cs`
(27 tests) — a locally re-derived pure formula (deliberately not a
production authority; Phase 4I.3's job) cross-checked against the 20/21/
22/23/24/28/32/40/48/52 fixture table, the 21–52 applicability boundary,
Core-always-12/Runway-always-8 across the full range, monotonicity,
ShortExtension (1–3) / FullPhase (4–32) classification, profile-duration
identity, and canonical-JSON/Markdown cross-checks (decision recorded as
`CLOSED`, no runtime-activation claim in `currentRuntimeImpact`, the
deferred `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` dependency present and
still `OPEN`, and terminology non-collision).

## 21. Non-claims

- 1 GE week is **not** claimed to independently produce a complete
  physiological adaptation.
- 32 GE weeks are **not** claimed to be one continuous monotonic
  progression block, one mesocycle, a permanent weekly load increase, or
  repeated identical workouts.
- This decision does **not** claim runtime activation of any kind for
  21–52 weeks.
- The 53+ product boundary is **not** claimed to be a physiological-safety
  finding.
- No claim is made that the existing Habit-style planner can safely
  support up to 32 rolling GE weeks (`HABIT_STYLE_PLANNER_REUSE` remains
  `DESIGN_INTENT_NOT_YET_PROVEN`, unaffected by this phase).

## 22. Deferred safety dependencies

Explicitly deferred to **Phase 4I.2** (tracked by
`TD-GENERAL-ENDURANCE-STAGED-PLAN-001`, unaffected/still `OPEN`): General
Endurance safety policy, mesocycle structure, recovery/deload cadence,
progression caps, long-run progression, workout repetition constraints,
profile-specific GE content, rebaseline/checkpoint behavior, GE→Runway
transition loading. Deferred to **later phases**: the typed runtime
decision contract (Phase 4I.3, shape recorded in §17), allocation
implementation, catalog construction, structural materialization,
numeric/calendar policies, dark end-to-end proof, public preview,
confirmation/persistence, Flutter.

## 23. Runtime activation status

**Unchanged and still fully blocked.** No allocator, resolver,
materializer, catalog workout, schema, migration, preview, confirmation,
or persistence code exists for 21–52 weeks anywhere in the repository
before or after this phase. Any 21+ week public request continues to
return exactly what it returned before this phase (`PLAN_HORIZON_COMPOSITION_REQUIRED`
for 15+, unchanged since no phase has activated that gate).

## 24. Residual decision risks

- The GE phase's internal design (mesocycle shape, deload cadence,
  progression caps) is entirely unresolved — Phase 4I.2's full scope, not
  narrowed by this phase beyond the duration classification.
- `HABIT_STYLE_PLANNER_REUSE` remains unproven design intent; this phase
  neither advances nor resolves that question.
- The exact segment-provenance persistence shape (how a future
  `TrainingWeek`/`TrainingDay` would record which of
  `LONG_HORIZON_GENERAL_ENDURANCE`/`PREPARATION_RUNWAY`/`RACE_CORE`
  produced it) remains undecided — tracked under
  `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`, not this phase's scope.
- The user-facing label choice ("General Endurance") for the long-horizon
  segment was made without a dedicated product/UX research pass — flagged
  as a low-risk, easily-revisable choice, not evidenced beyond
  simplicity/consistency with existing product voice.

## 25. Final classification

```
21_TO_52_WEEK_PRODUCT_COMPOSITION_POLICY_APPROVED
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_LONG_HORIZON_SAFETY_DELOAD_AND_PROGRESSION_POLICY
```

## 26. Exact next phase

**Phase 4I.2 — Long-Horizon General Endurance Safety, Mesocycle and
Progression Policy Resolution**: resolve (decision/governance only, same
boundary as this phase) mesocycle structure, recovery/deload cadence,
progression caps, long-run progression, workout repetition constraints,
profile-specific GE content shape, rebaseline/checkpoint rules, and
GE→Runway transition loading — the exact list this phase deferred in §22.
Only after 4I.2 does a typed runtime-decision implementation phase (4I.3,
shape recorded in §17) become appropriate.
