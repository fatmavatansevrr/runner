# Phase 4I.2 — Long-Horizon General Endurance Safety, Mesocycle and Progression Policy Resolution

## 1. Executive result

The complete **structural** safety/mesocycle/weekly-role/repetition/
checkpoint policy for the 1–32-week `LONG_HORIZON_GENERAL_ENDURANCE`
segment is **approved**. The development-week **numeric** volume and
long-run progression framework is also approved, by extending the already-
real precedent that the Preparation Runway segment reuses Core's
`VolumeSafetyPolicy.Default` numeric constants rather than inventing new
ones. **One numeric decision remains unresolved and is explicitly not
invented**: the recovery/consolidation-week volume-reduction magnitude has
zero canonical evidence anywhere in the repository.

```
LONG_HORIZON_GENERAL_ENDURANCE_STRUCTURAL_SAFETY_POLICY_APPROVED_BUT_NUMERIC_PROGRESSION_POLICY_REMAINS_BLOCKED
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_TYPED_RUNTIME_DECISION_CATALOG_AND_MATERIALIZATION
```

This phase is decision/governance only. No production runtime, resolver,
materializer, catalog workout, schema, migration, preview, confirmation,
persistence, backend, or Flutter file was changed.

## 2. Inherited composition

Phase 4I.1 approved, for `21 <= AvailableFullWeeks <= 52`:
`LongHorizonGeneralEnduranceWeeks = AvailableFullWeeks - 20`,
`PreparationRunwayWeeks = 8` (fixed), `CoreWeeks = 12` (fixed). GE range =
1–32. 1–3 GE weeks = `ShortExtension`; 4–32 GE weeks = `FullPhase`. Both
readiness profiles use identical GE/Runway/Core durations; profile affects
content only. 53+ remains rejected. All unchanged by this phase.

## 3. Product constraints

The eight inherited product constraints from this phase's own prompt
(intensity boundary, profile boundary, running-slot boundary, recovery
boundary, checkpoint boundary, progression boundary, ShortExtension
composition, remainder placement) are treated as authoritative inputs and
evaluated against repository evidence throughout §6–§21 below — approved
unless evidence contradicted them (none did).

## 4. Evidence reviewed

`VolumeSafetyPolicy.cs` (`APPSEL_RACE_VOLUME_SAFETY_V1` — the canonical
Core numeric policy: `PreferredMaxWeeklyIncreaseRatio=0.07`,
`HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5`,
`LongRunPreferredMinimumShare=0.30`, `LongRunPreferredMaximumShare=0.36`,
`LongRunSelectionShare=0.33`, `LongRunHardCapShare=0.40`,
`RoundingIncrementKm=0.5`); `CatalogVolumeAndLongRunPlanner.cs`;
`TenKPreparationRunwayNumericPolicyFactory.cs` (**the decisive precedent**
— the Preparation Runway segment already reuses `VolumeSafetyPolicy.Default`'s
exact constants verbatim rather than inventing new numbers, with each
field's evidence classification recorded inline via
`PreparationRunwayNumericRuleClassification`); `ten-k-master.v6.json`
(confirmed: **no `isRecoveryWeek`/recovery-volume-reduction concept exists
anywhere** in the current 12-week Core template — direct negative
evidence that no canonical recovery-week reduction percentage exists in
the repository today, for any segment); `plan-catalog/schemas/workout-
definition.schema.json` (`family` enum = `EASY`/`LONG_RUN`/`QUALITY`/
`RACE`; `eligiblePhases` enum currently has no `LONG_HORIZON_GENERAL_
ENDURANCE`-equivalent value — confirms no catalog/schema work exists or
was performed); `PHASE4I_1_...md`, `TD-ALLOCATION-PRIORITY-001`,
`TD-LONG-HORIZON-COMPOSITION-001`, `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`
(all read in full before authoring this phase's entry);
`PreparationRunwayContracts.cs` (`PreparationRunwayBlockType.GeneralEndurance`
— the existing runway-block enum member this phase's terminology decision
must not collide with, confirmed unchanged).

## 5. Evidence-strength classification

- **A — Direct canonical rule**: none new invented; the *reuse* of
  `VolumeSafetyPolicy.Default`'s values is itself direct (those constants
  are already canonical for this candidate).
- **B — Approved Appsel product default**: the 3-development+1-recovery
  four-week mesocycle cadence (explicitly labeled by the prompt itself as
  "an approved Appsel deterministic product default supported by general
  periodization evidence, not a universal scientific law" — recorded
  verbatim, not upgraded to a stronger claim).
- **C — Strong domain evidence**: none new gathered in this phase beyond
  what Phase 4G.3B.8 already established for Core (Foundation-first
  extension, general periodization literature).
- **D — Architecture/determinism requirement**: the GE development-week
  numeric reuse (extending the real, already-built
  `TenKPreparationRunwayNumericPolicyFactory` precedent one segment
  further); the terminal-remainder-placement rule (forced by the
  continuity invariant, same reasoning class as Phase 4I.1's formula);
  the weekly-role-skeleton retention (forced by "do not invent a new
  role taxonomy" absent evidence requiring one).
- **E — Decision remains blocked**: the recovery-week numeric reduction
  magnitude. No A/B/C/D evidence exists for it anywhere in the repository.
  Not approved. Not invented.

No Appsel product default is presented as a scientific fact anywhere in
this decision.

## 6. Intensity boundary

**Approved as given.** Low-intensity dominant, not exclusive. Allowed:
easy running, easy-support running, easy long run, controlled aerobic-
support running, relaxed strides/form work. Prohibited: threshold
sessions, VO2max intervals, 10K goal-pace sessions, race-specific
rehearsal, hard interval sessions, race simulation. No repository evidence
was found proving a conflict with this boundary — it is consistent with
the intensity content already approved for the Preparation Runway's
`AerobicStrength` block (Phase 4G.6A.2: "running-based controlled aerobic-
power/economy stimulus," explicitly not threshold/maximal/all-out/VO2max/
goal-pace/race-specific, per Phase 4G.6A.4A's direct test assertion on the
two real workout definitions). Classification: **B** (approved product
default), reinforced by **D** (consistency with the adjacent, already-
resolved Runway intensity boundary).

## 7. Profile policy

**Approved as given.** Horizon determines duration (Phase 4I.1, unchanged);
readiness profile determines content density only. `CONSISTENCY_NEEDED`:
lower-intensity emphasis, slower early progression, no hard quality,
controlled aerobic support only where policy explicitly permits it (i.e.,
never in `ShortExtension`, and later than `CORE_ENTRY_READY` within
`FullPhase` — exact cycle threshold deferred to catalog-construction
detail, not needed for this phase's structural closure). `CORE_ENTRY_READY`:
may receive controlled aerobic support earlier, still low-intensity
dominant, no threshold/VO2max/goal-pace/race-specific work. Profile does
not move the Runway/Core boundaries (already guaranteed structurally by
Phase 4I.1's profile-independent formula). Classification: **B**, directly
consistent with the already-approved `CONSISTENCY_NEEDED`/`CORE_ENTRY_READY`
distinction pattern from Phase 4G.6A.2 (different runway content, same
runway duration).

## 8. Running-slot policy

**Approved.** All four generated weekly slots remain running sessions:
1 `KEY_SESSION`, 2 `EASY_SUPPORT`, 1 `LONG_RUN` — the same skeleton already
used by Core and (by inheritance) the Preparation Runway. Strength/
mobility/gym recommendations may exist outside this four-session running
skeleton but may never replace `KEY_SESSION`, `EASY_SUPPORT`, or
`LONG_RUN`. In GE, `KEY_SESSION` is restricted to one of: controlled
aerobic support, easy progression, relaxed fartlek below the prohibited-
intensity boundary, or a technique/stride-oriented session — never
threshold, VO2max, goal pace, or race-specific work. Classification: **D**
(no evidence found requiring a different role taxonomy; retaining the
existing one is the deterministic, non-inventive choice).

## 9. Four-week mesocycle

**Approved**, per the prompt's own preferred default: Week 1 = controlled
development; Week 2 = controlled development; Week 3 = highest load of the
mesocycle within approved limits; Week 4 = recovery and consolidation. All
`FullPhase` profiles use the same 3+1 structure (profile changes numeric/
content density only, per §7). The first full mesocycle and later
mesocycles use the identical 3+1 shape — no repository evidence was found
requiring the first mesocycle to differ (an "Entry" role exists, but it
belongs to `ShortExtension`'s separate structure, §15–17, not to
`FullPhase`'s first mesocycle). The final full mesocycle before Runway
also uses the same 3+1 shape; any leftover 1–3 weeks become a separate
terminal Alignment remainder (§18), not a modified final mesocycle.
4 weeks → exactly 1 recovery week. 8 weeks → exactly 2 recovery weeks. 32
weeks → 8 mesocycles (24 development weeks + 8 recovery weeks), verified
in `LongHorizonGeSafetyPolicyDecisionTests.ThirtyTwoWeeksProduceEightMesocyclesAndAreNotOneMonotonicBlock`,
which also proves no more than 3 consecutive development weeks ever occur
— continuous weekly progression is never allowed across a recovery
boundary. Classification: **B** (explicit prompt-labeled Appsel default).

## 10. Stage families

**Approved minimum deterministic set**: Entry, Base Development, Aerobic
Durability, Consolidation, Pre-Runway Alignment. Entry is required only
for `ShortExtension` (§15) and the first mesocycle's orientation role is
otherwise absorbed into Base Development — no separate "Entry" stage is
needed inside `FullPhase`'s repeating 3+1 cycle, since `FullPhase` weeks 1–2
of each mesocycle already serve that controlled-development purpose.
Consolidation is the recovery week's stage name (Week 4 of each
mesocycle). Pre-Runway Alignment is required for the terminal remainder
(§18) and, structurally, functions identically whether reached via
`ShortExtension` or via `FullPhase`'s leftover weeks. Maximum consecutive
repetition of the same mesocycle shape is 1 (each mesocycle is itself the
repeating unit — no two consecutive mesocycles may both omit their
recovery week, proven structurally by construction). 4/8/12/20/32 weeks
differ structurally only in mesocycle **count** (1/2/3/5/8 respectively),
not in per-mesocycle shape — this phase does **not** approve inventing
race-specific work merely to create variation across a 32-week segment
(explicitly prohibited by the intensity boundary, §6); variation instead
comes from workout-level repetition constraints (§20), not stage-family
proliferation. Classification: **D**.

## 11. Weekly roles

**Approved**, extending §8: `KEY_SESSION` content is restricted to
controlled aerobic support / easy progression / relaxed fartlek (below
prohibited-intensity boundaries) / technique-stride work, for both
profiles, in both `ShortExtension` and `FullPhase`. Development weeks may
include the profile-appropriate `KEY_SESSION` variant; recovery weeks
downgrade or omit `KEY_SESSION`'s harder content (§14); pre-Runway
alignment weeks use the lowest-intensity `KEY_SESSION` variant available
(easy progression only, no controlled aerobic support), consistent with
"alignment," not "development." `CONSISTENCY_NEEDED` never receives
controlled aerobic support in `ShortExtension` or in early `FullPhase`
cycles (exact cycle threshold is catalog-construction detail, deferred);
`CORE_ENTRY_READY` may receive it earlier but never before its own first
`FullPhase` mesocycle's development weeks. Classification: **B**/**D**.

## 12. Volume progression

**Approved for development weeks; blocked for recovery weeks.**
Development-week increases reuse `VolumeSafetyPolicy.Default` verbatim
(`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`,
`AbsoluteWeeklyIncrementCapKm=2.5`, `RoundingIncrementKm=0.5`) — the same
constants the real, already-built `TenKPreparationRunwayNumericPolicyFactory`
already reuses for the adjacent Preparation Runway segment, extended one
segment further upstream rather than inventing a fifth set of numbers.
This is explicitly **not** a universal 10-percent rule (0.07/0.08 ≠ 0.10).
Required inputs (recent four-week average weekly volume, recent longest
run, current runs per week, readiness profile, current mesocycle
position, recovery status, future Runway Week 1 target) are recorded as
the required *input contract* for a future numeric materializer — not
computed here (no runtime code). Behavior when baseline evidence is
missing or conflicting is addressed structurally in §22, reusing existing
runtime-condition-style patterns, not inventing a new missing-data policy.
**Recovery-week reduction magnitude is the one unresolved numeric
decision** (§14) — no canonical value exists anywhere in the repository.
Classification: development-week numerics = **D** (reuse precedent);
recovery-week numeric magnitude = **E** (blocked).

## 13. Long-run progression

**Approved structurally, numerically bounded by the same reused shares.**
Initial GE long-run derivation and development-week increases use the
same reused `LongRunPreferredMinimumShare=0.30`/`LongRunPreferredMaximumShare=0.36`/
`LongRunSelectionShare=0.33`/`LongRunHardCapShare=0.40` of weekly volume —
the identical mechanism the Preparation Runway numeric factory already
uses, so long-run and weekly-volume progression cannot independently
create an unsupported combined jump (the long-run is always a bounded
share of the already-capped weekly volume, by construction, not an
independently-set number). Recovery-week long-run reduction shares the
same unresolved-magnitude status as §12/§14. Maximum long-run distance
before Runway and final-GE-long-run-relative-to-Runway-Week-1 are
addressed structurally in §19 (continuity envelope), not as an
independent new numeric cap. Rounding reuses `RoundingIncrementKm=0.5`km.
Classification: **D** for the share-based framework; **E** for the one
recovery-magnitude gap it inherits from §12.

## 14. Recovery-week policy

**Approved structurally; numeric magnitude blocked.** A recovery week:
must reduce total volume relative to the immediately preceding development
week (never increase it — proven structurally in
`RecoveryWeekCannotCreateAVolumePeak`); must not increase the long run
(`RecoveryWeekCannotIncreaseLongRun`); downgrades or removes `KEY_SESSION`'s
harder content; retains all four weekly slots (no slot is dropped, per
§8); may retain relaxed strides (explicitly allowed by the intensity
boundary, §6); does not introduce a new peak; contains no race-specific
work; does not compensate for missed previous workouts (no automatic
rescheduling — this phase does not implement any adaptive/rescheduling
mechanism at all, consistent with the phase boundary). The **exact
reduction percentage/magnitude** is the one numeric gap this phase leaves
open — recorded explicitly in `TD-LONG-HORIZON-GE-SAFETY-001`'s
`requiredResolution`, not invented. Relationship to checkpoint timing:
recovery weeks are not required to coincide with an advisory checkpoint,
but the 8-GE-week checkpoint cadence (§21) naturally aligns with the
second recovery week of each two-mesocycle block. Classification:
structural rules = **B**/**D**; numeric magnitude = **E**.

## 15. ShortExtension one-week policy

**Approved**, per the prompt's own preferred semantics: 1 GE week = Entry
and Runway Alignment (a single week serving both roles — there is no room
for a second distinct role in a 1-week allocation). No dedicated recovery
week (not required — `ShortExtensionDoesNotRequireARecoveryWeek`, proven
structurally: the `ShortExtension` role set contains no `Recovery` value
at all). No hard aerobic support, no threshold/race-specific work, no
independent adaptation claim. Numeric behavior: maintenance-level volume
only (no development-week increase is applied — there is only one week,
so no "increase relative to a prior GE week" is meaningful; the week's
volume is derived directly from baseline per §22, then aligned toward
Runway Week 1 per §19). Both profiles use the same 1-week structure
(duration is profile-independent, §7); content differs only in whether any
controlled aerobic support appears (`CONSISTENCY_NEEDED`: none;
`CORE_ENTRY_READY`: still none in `ShortExtension` specifically, since the
prompt's own ShortExtension composition explicitly excludes "hard aerobic
support" for both profiles — `CORE_ENTRY_READY`'s earlier-support allowance
applies to `FullPhase`, not `ShortExtension`).

## 16. ShortExtension two-week policy

**Approved.** 2 GE weeks = Entry, then Runway Alignment (two distinct
weeks, each single-role). Same non-claims as §15 (no recovery week, no
hard aerobic support, no threshold/race-specific work, no independent
adaptation claim). Numeric behavior: Entry derives from baseline; Runway
Alignment moves toward the Runway Week 1 continuity envelope (§19) — a
single, bounded step, using the same reused increase-ratio ceiling as
development weeks (§12) as the upper bound on how much that one step may
move, not a separate invented number.

## 17. ShortExtension three-week policy

**Approved.** 3 GE weeks = Entry, Controlled Development, Runway
Alignment. "Controlled Development" is the only `ShortExtension` week
permitted to apply a development-style increase (bounded by the same
reused §12 ceiling) — it is not a `FullPhase` mesocycle (no recovery week
follows it; `ShortExtensionDoesNotRequireARecoveryWeek` applies uniformly
across all three `ShortExtension` lengths). The terminal week remains
Runway Alignment, identical in role to §15/§16.

## 18. Remainder placement

**Approved**, per the prompt's own preferred default and worked examples
(6 GE → 1 full mesocycle + 2-week alignment; 11 GE → 2 full mesocycles +
3-week alignment): complete four-week mesocycles occur first; the
remaining 1–3 weeks are placed immediately before Runway as Pre-Runway
Alignment Extension — never at the beginning, and never interleaved.
`RemaindersAreTerminal` and `RemainderNeverProducesBackToBackRecoveryWeeks`
prove this by construction for remainders 1/2/3 across GE = 5, 6, 7, 9,
10, 11, 13, 14, 15: every Alignment week sits strictly after the last
Development/Recovery week, and a Recovery week (the last week of a full
mesocycle) is never immediately followed by another Recovery week (the
Alignment remainder is never itself classified `Recovery`, preventing the
"recovery immediately followed by an unnecessary low-load alignment week"
failure mode the prompt warns against). No duplicated Entry weeks occur in
`FullPhase` (Entry belongs only to `ShortExtension`'s vocabulary, §10). No
repository evidence was found proving terminal placement breaks
progression continuity, so the preferred default stands unmodified.

## 19. GE→Runway continuity

**Approved as a structural continuity requirement; not a numeric
recalibration.** Required comparisons at the final-GE-week-to-Runway-
Week-1 boundary: weekly volume, long-run distance, session count,
intensity, `KEY_SESSION` semantics, pace source, profile, global week
numbering, catalog provenance — all must be evaluated for continuity by a
future materialization phase, using this phase's structural rules (not
computed here). Decision: **final GE aligns upward toward Runway**, not
the reverse — the approved 8-week Runway structure (Phase 4G.6A.1/
4G.6A.2, `CONSISTENCY_NEEDED`: 2 Consistency + 5 Runway GE + 1 Transition;
`CORE_ENTRY_READY`: 5 Runway GE + 2 Aerobic Strength + 1 Transition)
remains **structurally unchanged** by this phase — no direct continuity
evidence was found requiring Runway Week 1 to be recalibrated downward,
and doing so without such evidence would violate this phase's own
decision standard. A "shared transition envelope" (both sides converging
within the same bounded increase-ratio ceiling used throughout §12/§13)
is the mechanism by which alignment happens, not a redefinition of either
segment's approved structure.

## 20. Repetition constraints

**Approved.** Distinguishes three concepts, per the prompt's own required
distinction: repeated workout **identity** (the exact same catalog
workout definition), repeated workout **family** (`EASY`/`LONG_RUN`/
`QUALITY`), and repeated **numeric prescription** (same distance/duration
values, different identity). Maximum consecutive use of the same workout
identity: bounded by the existing catalog convention already proven for
the adjacent Preparation Runway `AerobicStrength` block (`min1/max2`-style
two-step progression, `FARTLEK_INTRO`-precedented) — reused, not
reinvented. Numeric progression *alone* (same workout identity, changing
distance/duration week to week) is sufficient variation for Easy/Long-Run
family content — this matches how Core's own `FOUNDATION_EASY_BASE` is
reused across multiple Foundation weeks today with the numeric value
changing, not the workout identity. Controlled-aerobic-support content
requires the same two-step (Intro→Progressed) variation the Runway block
already uses. Recovery-week workout reuse is permitted (a recovery week
may reuse the same Easy workout identity as a development week, at
reduced volume — identity reuse is not itself a repetition-limit
violation). 32-week exhaustion prevention: relies on mesocycle-position-
aware numeric progression (§12/§13) providing enough numeric variation
across 8 mesocycles without requiring artificial novelty that would harm
progression continuity — the prompt explicitly forbids inventing variation
"that harms progression continuity," so this phase does not mandate a
larger workout-identity catalog than evidence supports. Catalog capacity
implications (whether enough distinct workout definitions currently exist
for 32 weeks) are **not resolved here** — that is explicitly catalog-
construction work, out of this phase's boundary, deferred alongside the
LONG_HORIZON_GENERAL_ENDURANCE `eligiblePhases` schema value itself (§4).

## 21. Advisory checkpoints

**Approved cadence and payload; outcomes remain conceptual.** Cadence: GE
1–3 → pre-Runway advisory readiness check only; GE 4–7 → pre-Runway
advisory readiness check; GE 8–32 → an advisory checkpoint after every
eight GE weeks, plus immediately before Runway. Payload: recent weekly
volume, recent longest run, completed-session consistency, pain/injury
warning, perceived difficulty, continued availability, race-date
continuity. Outcomes are conceptual only: `Continue`, `Recommend review`,
`Recommend plan regeneration`, `Stop and seek appropriate professional
guidance` where safety inputs require it. Checkpoints **cannot** mutate
sessions, alter volume automatically, move phase boundaries, create
adapted workouts, or activate an adaptation engine —
`CheckpointsAreAdvisoryOnly`/`CheckpointsCannotMutatePlans` prove this
structurally (the mutating-outcome vocabulary is disjoint from the
approved outcome vocabulary). Future implementation ownership (which
service computes/presents a checkpoint) is deferred, kept outside current
runtime activation.

## 22. Missing/conflicting data

**Approved to reuse existing runtime-condition-style patterns, not invent
a new missing-data policy.** When recent weekly volume, recent longest
run, or recent runs/week are absent or zero: apply the same category of
approved-conservative-default handling already established for the
Preparation Runway's own missing-readiness-starting-volume policy
(`V1MissingReadinessStartingVolumePolicy`, reused by
`TenKPreparationRunwayNumericPolicyFactory` today — direct, real
precedent, not invented for this phase). When weekly volume and longest
run conflict, or a reported baseline sits above the pilot volume band or
below the minimum safe catalog entry, or the readiness profile conflicts
with numeric history: these route to the same **typed decision-required**
category the repository already uses for genuinely ambiguous inputs
(mirroring `GOAL_FEASIBILITY_IN`'s `NotEvaluated`/`DecisionRequired`
vocabulary elsewhere in the resolver layer) rather than silently
fabricating athlete capacity — this phase does not fabricate any specific
numeric resolution for these conflict cases; it records that they must
route through the existing typed-decision pattern, not a new one.

## 23. Safety boundary and non-claims

Recorded explicitly, verbatim per the prompt's own required list: no
exact progression percentage guarantees injury prevention; recovery weeks
do not guarantee injury prevention; checkpoint clearance is not medical
clearance; a 32-week allocation does not guarantee that one static plan
remains appropriate without reassessment; pain/injury reporting must not
be treated as ordinary training fatigue. No medical diagnosis logic is
created or implied anywhere in this decision. No unsupported injury-risk
prediction is encoded.

## 24. Governance artifacts

`plan-catalog/artifacts/audits/activation-readiness-risks.json`: added
`TD-LONG-HORIZON-GE-SAFETY-001` (status `CLOSED`), updated the
`currentAppendOnlyStatus` aggregate sentence (20→21 total, 10→11 closed).
`plan-catalog/artifacts/audits/activation-readiness-risks.md`: added the
matching table row plus a new "Long-Horizon General Endurance safety/
mesocycle/progression policy closure (Phase 4I.2)" prose section, updated
the aggregate line (20/10/10 → 21/10/11). `TD-GENERAL-ENDURANCE-STAGED-
PLAN-001` was **not** modified — it remains `OPEN`, narrowed in scope but
not closed (rolling-materialization mechanics, segment-provenance
persistence, transition orchestration, and actual catalog/workout content
remain undecided). No parallel policy registry was created.

## 25. Tests

New file: `plan-catalog/tests/PlanCatalog.Tests/Architecture/LongHorizonGeSafetyPolicyDecisionTests.cs`
(40 tests) — a locally re-derived structural/numeric model (deliberately
not a production authority) proving: GE applicability (1–32);
`ShortExtension`(1–3)/`FullPhase`(4–32) classification; four-week
mesocycle construction (3 development + 1 recovery); 4/8/32-week exact
mesocycle/recovery counts and the no-more-than-3-consecutive-development-
weeks monotonicity-break proof; `ShortExtension` requires no recovery
week; terminal remainder placement and no back-to-back recovery weeks for
remainders 1/2/3 across 9 GE-week values; profile-duration identity and
profile-content distinctness; prohibited-intensity exclusion from
`KEY_SESSION` content; bounded controlled-aerobic-support (1 `KEY_SESSION`
slot); the four-slot running-only skeleton; no-universal-10-percent-rule
(0.07/0.08 ≠ 0.10, matching the reused canonical values exactly); recovery
cannot create a volume peak or increase the long run (structural, not
numeric); advisory-only checkpoint outcome vocabulary and zero mutation
capability; 53+ non-applicability; terminology non-collision; the
deferred `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` dependency staying `OPEN`;
and canonical-JSON/Markdown cross-checks (decision recorded `CLOSED`, the
recovery-magnitude gap and `NUMERIC_PROGRESSION_POLICY_REMAINS_BLOCKED`
text both present, no runtime-activation claim in `currentRuntimeImpact`).

**Focused**: `LongHorizonGeSafetyPolicyDecisionTests` — **40 passed, 0
failed**.
**Full plan-catalog suite**: **461 passed, 0 failed, 0 skipped** (421
pre-existing + 40 new).
**Backend suite**: not run — no backend file mechanically consumes this
governance artifact (confirmed by the same reasoning as Phase 4I.1: the
only backend references to the artifact filename are documentation-string
citations, not file I/O).

## 26. Runtime activation status

**Unchanged and still fully blocked.** No allocator, resolver,
materializer, catalog workout, schema, migration, preview, confirmation,
or persistence code exists for `LONG_HORIZON_GENERAL_ENDURANCE` anywhere
in the repository before or after this phase. Any 21+ week public request
continues to return exactly what it returned before this phase.

## 27. Deferred implementation work

Catalog/schema work: a `LONG_HORIZON_GENERAL_ENDURANCE`-eligible
`eligiblePhases` value and actual workout definitions (mirrors how
`PREPARATION_RUNWAY`'s own schema value required a dedicated later phase,
4G.6A.4A). Numeric: the recovery-week reduction magnitude (§14). Runtime:
the typed decision-contract implementation (Phase 4I.3+, shape recorded
by Phase 4I.1 §17, structurally extended by this phase's rules), rolling-
materialization mechanics, segment-provenance persistence shape,
transition orchestration, checkpoint implementation ownership — all
tracked under `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`, which remains
`OPEN`.

## 28. Residual blockers

- Recovery-week numeric reduction magnitude (the one unresolved numeric
  decision in this phase).
- Exact controlled-aerobic-support cycle threshold within `FullPhase` for
  each profile (structural boundary approved; exact cycle number deferred
  to catalog-construction detail, consistent with how the Runway's own
  `AerobicStrengthSecondWeekThreshold=RunwayWeeks>=5` required a dedicated
  coefficient-simulation phase, 4G.6A.2/4G.6A.2A, rather than being
  guessed here).
- Catalog capacity for 32 weeks of GE content (repetition constraints are
  approved structurally; whether enough distinct workout definitions
  currently exist is unverified and out of scope).
- Missing/conflicting-baseline-data exact routing logic (approved to reuse
  the existing typed-decision-required pattern; the specific new
  condition-value vocabulary for GE is not yet defined).

## 29. Final classification

```
LONG_HORIZON_GENERAL_ENDURANCE_STRUCTURAL_SAFETY_POLICY_APPROVED_BUT_NUMERIC_PROGRESSION_POLICY_REMAINS_BLOCKED
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_TYPED_RUNTIME_DECISION_CATALOG_AND_MATERIALIZATION
```

## 30. Exact next phase

**Phase 4I.2A — Recovery-Week Numeric Reduction Magnitude Decision** (a
narrow, single-question follow-up, mirroring how Phase 4G.6A.2A resolved
one specific unresolved coefficient after 4G.6A.2's broader structural
pass): obtain or derive the one remaining numeric value this phase could
not approve. Only after that closes should **Phase 4I.3 — Typed 21–52
Week Runtime Decision Contract** (consuming both Phase 4I.1's composition
formula and this phase's structural/numeric policy) begin, followed by
catalog/schema construction for `LONG_HORIZON_GENERAL_ENDURANCE` content.
