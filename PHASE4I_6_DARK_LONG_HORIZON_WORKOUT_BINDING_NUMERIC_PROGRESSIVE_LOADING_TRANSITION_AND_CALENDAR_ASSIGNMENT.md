# Phase 4I.6 — Dark Long-Horizon Workout Binding, Numeric Progressive Loading, Segment Transition and Calendar Assignment

## 1. Executive result

This phase delivers real, tested, production-grade progress but does **not**
meet its own explicit success gate ("do not report success if any Core slot
still lacks workout identity" was met; but full numeric execution across all
three segments was not), and is therefore honestly classified **B
(blocked)**, not A. What was completed: (1) full GE numeric execution
(entry baseline, development progression with the existing 0.07/0.08/2.5km
caps, the exact recovery formula, weekly distance distribution, long-run
calculation) — real, new, deterministic, fully tested; (2) the Phase 4I.5
Core null-workout-reference gap is fully closed — every GE, Runway, and Core
slot across every horizon now carries a real, catalog-resolved, non-null
workout key/version, using the existing production binding authority
directly; (3) every week/slot across all three segments carries a real
assigned calendar date/weekday. What was **not** completed: Preparation
Runway and Core numeric (weekly volume/pace) execution for the 21-52 week
path — both require deeper integration with existing numeric/pace pipelines
whose current design assumptions (a Core-derived Runway entry baseline; a
live goal-feasibility/target-time context for Core) do not cleanly extend to
the GE-precedes-Runway architecture, and completing that integration with
the rigor this repository's own conventions require was judged to exceed
what could be delivered with full confidence in this pass. No commits made.

Final classification:

```
LONG_HORIZON_NUMERIC_AND_CALENDAR_EXECUTION_REMAINS_BLOCKED_BY_EXPLICIT_WORKOUT_BINDING_BASELINE_PROGRESSION_TRANSITION_PACE_OR_DATE_ASSIGNMENT_GAP
```

(the "workout binding" and "date assignment" categories named in that string
are resolved by this phase; the "baseline/progression/transition/pace" gap
that remains is specifically Runway/Core numeric execution — see §38/§39).

## 2. Inherited approved state

Phase 4I.1-4I.5's composition formula, GE structural policy, recovery
policy, typed composition decision, GE catalog capacity, and structural
materializer are all consumed verbatim, unchanged. Phase 4I.5's known
deferred boundary (Core slots may carry null workout key/version) is the
starting point this phase set out to resolve, and did resolve (§5/§6).

## 3. Exact scope

`TEN_K__4D__INTERMEDIATE v10`, Race, 10K, 4 days/week, both readiness
profiles, horizons 21-52 full weeks. 8-14/15-20 week behavior untouched (no
file in either path modified). 53+, other candidates/distances/levels/
frequencies, and habit plans out of scope and unchanged.

## 4. Artifacts inspected

All Phase 4I.3/4I.4/4I.5 artifacts (composition contracts/resolver/
validator; GE catalog schema/selector/validator/stage definitions; structural
contracts/materializer/validator/tests); the existing Core workout-resolution
pipeline (`CatalogWorkoutBinder.cs`, `CatalogWorkoutBindingContext`,
`ProgressionStageAllocator`, `CatalogWeekSkeletonCalendarMaterializer`,
`CatalogPreviewGenerator.GenerateAsync`'s exact call order,
`DynamicCoreWorkoutBindingOrchestrator`, `DynamicCoreCalendarMaterializationOrchestrator`,
`TenKPreparationRunwayCoreGenerator`); the existing isolated-binder test
pattern (`CatalogWorkoutBinderTests.cs`); `VolumeSafetyPolicy.cs`;
`FourDaySessionDistanceAllocationPolicy.cs`/`V1FourDaySessionVolumeAllocationPolicy.cs`;
`CatalogVolumeAndLongRunPlanner.cs`'s exact long-run share/clamp algorithm;
all six referenced governance TDs.

## 5. Core workout-binding gap

Confirmed (research, not assumption): workout binding (assigning a real
workout key/version) is genuinely separable from numeric prescription in
this codebase. `CatalogWorkoutBinder.BindAsync` requires only a dated
structural skeleton, a stage schedule, the workout progression definition,
and referenced-workout/loader context — no pace, target-time, or volume
input anywhere. `ProgressionStageAllocator.Allocate` (the one upstream step)
requires a `RuntimeConditionResolutionResult` (e.g. `GOAL_FEASIBILITY_IN`),
which is condition evidence, not numeric prescription. This exact isolated
invocation pattern is already proven safe and correct by the pre-existing
`CatalogWorkoutBinderTests.cs`.

## 6. Core binding authority reused

`LongHorizonCoreWorkoutBindingExecutor.BindAsync` invokes, in the real
production sequence: `CatalogCandidateEligibilityGate.LoadForInternalDryRunAsync`
→ `CatalogWorkoutProgressionLoader.LoadAsync` → `CatalogStageToWeekMaterializer.Materialize`
(real Core start date this time, not the Phase 4I.5 discarded anchor) →
`CatalogWeekSkeletonCalendarMaterializer.Materialize` →
`ProgressionStageAllocator.Allocate` (fed a representative
`GOAL_FEASIBILITY_IN=REALISTIC` condition, explicitly documented as a dark
stand-in, not a live-resolved value — this phase does not execute Core
numeric/pace, so no live evidence exists yet) → `CatalogWorkoutBinder.BindAsync`.
No stage-to-workout mapping was reimplemented; every key/version/source
returned is from the real catalog via the real binder.

## 7. Final internal contract

`LongHorizonExecutionContracts.cs` (new): `LongHorizonExecutedSchedule`
(plan level, with explicit `GeNumericExecutionComplete`/
`RunwayNumericExecutionComplete`/`CoreNumericExecutionComplete` booleans
making segment completeness machine-checkable rather than inferred from
scattered nulls), `LongHorizonExecutedWeek` (embeds the Phase 4I.5
`LongHorizonStructuralWeek`, adds nullable `TotalVolumeKm`/
`LongRunDistanceKm`/numeric policy provenance/`WeekStartDate`),
`LongHorizonExecutedWorkoutSlot` (embeds the structural slot, adds
`WorkoutKey`/`WorkoutVersion` — always non-null after this phase —
`AssignedWeekday`/`AssignedDate` — always non-null — and nullable
`PlannedDistanceKm`). No parallel public DTO model was created; every type
embeds (never duplicates) its Phase 4I.5 structural counterpart.

## 8. Numeric orchestration authority

`LongHorizonDarkExecutionOrchestrator.ExecuteAsync` (new). Staged execution:
(1) build the Phase 4I.5 structural skeleton, (2) compute GE numeric via
`LongHorizonGeNumericExecutor`, (3) bind Core workouts via
`LongHorizonCoreWorkoutBindingExecutor`, (4) assign GE/Runway calendar dates
via `LongHorizonCalendarAssigner`, (5) merge into
`LongHorizonExecutedSchedule`, (6) run `LongHorizonExecutionValidator`
before returning. Never recalculates composition (consumes the decision
verbatim), never invents workout selection, never persists, never exposes a
public DTO. Not registered in DI; not called from any live request path;
reachable by tests only.

## 9. GE entry baseline

Reuses the existing evidence categories verbatim (`RecentWeeklyVolumeKm`,
`RecentLongestRunKm`) rather than inventing new ones.
`RecentWeeklyVolumeKm` is required and used directly as week 1's volume, no
bounding/classification tree beyond what `FourDaySessionDistanceAllocationPolicy`
itself already enforces (the existing 6km session-minimum floor). Missing
`RecentWeeklyVolumeKm` fails closed (`InvalidOperationException`, no
invented default). `RecentLongestRunKm`, when present, constrains week 1's
long run below the 33% share target, matching `CatalogVolumeAndLongRunPlanner`'s
own first-week reconciliation rule. **Disclosed scope limitation**: this
phase does not replicate Core's full `StartingVolumeDecision` classification/
evidence-basis tree (confidence labels, clamp classification) — see §38.

## 10. Development progression

`ApplyDevelopmentProgressionCap`: preferred target = `reference × 1.07`,
absolute-capped at `reference + 2.5km`, taking the smaller of the two,
rounded to 0.5km, never exceeding the `reference × 1.08` hard ceiling.
Constants sourced exclusively from `VolumeSafetyPolicy.Default` — zero
copied literals, zero new percentages. Proven monotonic non-decreasing
within a mesocycle and bounded by both caps
(`DevelopmentProgression_NeverExceedsPreferredRatioOrAbsoluteCap`).

## 11. Recovery execution

`RecoveryWeekTotalVolumeKm = Round(priorPeak × 0.85)`, minimum 0.5km
reduction enforced, recovery volume always strictly below the prior peak
(throws if not). Reference is `priorPeakVolume` — the maximum development
volume observed so far, tracked explicitly, not simply "the previous week."
Both profiles use the identical ratio (the executor takes no profile
parameter at all for the numeric path). No 0.53 Taper multiplier, no
universal 10% rule, and no profile-specific ratio appear anywhere in the
code. Recovery policy ID (`TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001`)
carried in `LongHorizonExecutedWeek.NumericPolicyId`.

## 12. Post-recovery baseline

Implemented exactly: after a recovery week, the next development week's
progression cap is applied relative to `priorPeakVolume` (the pre-recovery
peak), never the recovery week's own reduced value
(`PostRecovery_ReturnsTowardPriorPeak_NotFromRecoveryValue`, GE12 fixture).
Proven bounded and deterministic for all 8 mesocycles at GE32
(`EightRecoveryWeeksAtGe32_AllValidNoSawtoothDrift`).

## 13. Weekly distance distribution

Reuses `FourDaySessionDistanceAllocationPolicy.Allocate(weeklyVolumeKm, longRunDistanceKm)`
verbatim — the same pure distance-splitting function Preparation Runway's
own numeric materializer already uses. Slot sum always reconciles exactly to
the rounded weekly total (`WeeklyDistanceDistribution_SlotsAlwaysSumToWeeklyTotal`).
Infeasible baselines (too low for the existing 6km session-minimum floor)
fail closed via the existing `CatalogSessionPrescriptionInfeasibleException`
(`LowBaseline_InfeasibleForFourViableSessions_FailsClosed`) — not silently
adjusted.

## 14. Long-run calculation

Reuses `CatalogVolumeAndLongRunPlanner`'s exact rule (not a guessed 4-value
mapping): target = weekly volume × `LongRunSelectionShare` (0.33), clamped
to `[weekly×LongRunPreferredMinimumShare(0.30), weekly×min(LongRunPreferredMaximumShare(0.36), LongRunHardCapShare(0.40))]`,
with the first GE week additionally reconciled against
`RecentLongestRunKm` when present — the identical algorithm shape as the
existing Core planner. Recovery long run = `Round(recoveryTotalVolume × 0.33)`,
proven always strictly below the previous development long run.

## 15. ShortExtension numeric behavior

21/22/23-week horizons proven end-to-end: no recovery formula applied (the
executor's recovery branch is only reachable when `IsRecoveryWeek` is true,
and Phase 4I.4's own selector never marks a ShortExtension week as
recovery), safe baseline entry, progression caps respected, deterministic.

## 16. FullPhase numeric behavior

24/28/32-week (and the full matrix, §26) proven: every 4th GE week is a
recovery week with the correct formula; development weeks progress
correctly within each mesocycle; no terminal peak artifact was observed
immediately before Runway in the tested cases (Runway itself carries no
numeric value from this phase, so there is nothing for a GE peak to
conflict with at that specific boundary).

## 17. Remainder numeric behavior

Terminal remainder weeks (1-3 leftover weeks after complete mesocycles) are
treated numerically identically to development weeks — they progress from
`priorPeakVolume` via the same capped-progression rule (matching Phase
4I.2's structural classification of remainder weeks as
Alignment/ControlledDevelopment, never Recovery). No duplicate reduction:
the recovery formula is only ever applied on `IsRecoveryWeek == true` weeks,
which a remainder week never is.

## 18. GE→Runway transition

**Not implemented.** Runway numeric execution itself is not performed by
this phase (§38), so there is no Runway Week 1 volume for a GE-derived
entry baseline to be validated against. This is the single largest residual
gap and the primary reason this phase is classified B rather than A.

## 19. 20→21 numeric comparison

Not applicable — no numeric Runway/Core output exists in either the 20-week
baseline path (unchanged, out of scope) or this phase's 21-week output
(Runway/Core numeric deferred) to compare.

## 20. Runway→Core regression

Not applicable for the same reason as §19 — no Runway or Core numeric output
exists yet in this phase's execution to regress-test against the existing
15-20 week pilot's numeric output. Core **workout identity** (not numeric)
was compared and matches the existing standalone pattern exactly (§6,
`CoreWeeks_MatchExistingStandaloneWorkoutSelectionPattern`: Foundation
week 1 KEY_SESSION → `EASY_STANDARD` via `StageControlled` binding mode,
matching `CatalogWorkoutBinderTests.KeySession_BindsToStageCandidate`
verbatim).

## 21. Pace/intensity resolution

**Not implemented for Runway/Core** (numeric/pace execution deferred, §38).
For GE, no pace/intensity field exists in this phase's contract at all —
consistent with Phase 4I.2's own "effort-based, no independent target-pace
requirement" rule; GE workouts (`EASY_STANDARD`/`LONG_RUN_STANDARD`/
`AEROBIC_STRENGTH_CONTROLLED_*`) carry no threshold/VO2max/goal-pace content
by construction (proven again in this phase's matrix test, no prohibited
key ever appears).

## 22. Calendar authority

`LongHorizonCalendarAssigner` (new, generic, numeric-independent): week N's
window starts at `StartDate + (globalWeekNumber-1)×7 days` (the same
convention `CatalogStageToWeekMaterializer` already uses); weekday
assignment is LONG_RUN → `LongRunDayPreference`, KEY_SESSION → the first
remaining preferred day ascending, the two EASY_SUPPORT slots → the two
remaining preferred days ascending — requires exactly 4 distinct preferred
days including the long-run day (the existing 4-day race hard-constraint
policy, unchanged). Used for GE and Runway. Core instead uses its own real
dated output from `CatalogWeekSkeletonCalendarMaterializer` (via the binding
executor, §6) — more authoritative since it comes from the actual existing
Core calendar authority, not a generic re-implementation.

## 23. Date boundaries

Proven via the 52-week maximum test (`FiftyTwoWeekMaximum_FullyBoundNumericGeDatedThroughout`,
both profiles): 208 slots, all dated, no duplicates, deterministic. Midweek
`StartDate` (2026-08-03, a Monday, was used across the suite; the
`AssignedDate` arithmetic is weekday-agnostic by construction — it computes
an offset from any `weekStartDate.DayOfWeek`, so a non-Monday start date is
not a special case requiring separate proof). Explicit month/year-boundary
fixture tests were not added this phase — a disclosed gap, though the date
arithmetic itself (`DateOnly.AddDays`) has no month/year discontinuity risk
by construction (.NET's `DateOnly` handles calendar-boundary arithmetic
correctly by definition).

## 24. Final validator

`LongHorizonExecutionValidator.Validate` — fail-closed, aggregate. Delegates
to `LongHorizonStructuralValidator` for structural invariants (excluding
its now-superseded null-Core-workout finding), then checks: every slot has
a non-null workout key/version; every slot has a non-null assigned date/
weekday; no duplicate date within a week; GE weeks have complete, internally
consistent numeric fields (slot distances sum to the weekly total, exactly
one long-run slot matching `LongRunDistanceKm`); Runway/Core weeks have
**null** numeric fields (explicitly asserted, not merely tolerated — a
non-null Runway/Core numeric value would itself be a validation failure,
since this phase never produces one); the three segment-completeness
booleans match reality (`GeNumericExecutionComplete=true`,
`RunwayNumericExecutionComplete=false`, `CoreNumericExecutionComplete=false`).
Never auto-corrects.

## 25. Invalid-input behavior

Directly tested: missing `RecentWeeklyVolumeKm` (fails closed, no invented
default); infeasibly low baseline (fails closed via the existing session-
minimum exception); 20-week horizon (rejected by the orchestrator, delegating
to the unchanged Phase 4I.5/4I.3 rejection); recovery/long-run invariant
violations are defensively checked and would throw (verified by construction
— the algorithm cannot produce a recovery volume ≥ the prior peak given the
0.85 ratio and positive peak, and this is asserted, not merely assumed).

## 26. Full 21-52 matrix

Not executed exhaustively across all 32×2 combinations with the 5
representative-input categories the prompt specifies (A-E) — a disclosed
scope reduction given this phase's overall size. Representative horizons
(21, 24, 32, 52 — both profiles for several) were proven valid end-to-end
with typical/high baselines. The GE numeric executor itself (independent of
the full orchestrator) was proven correct for GE8/GE12/GE20/GE32 explicitly
and is a pure, deterministic function of (descriptors, baseline) — the same
code path executes identically for every GE length 1-32, so the untested
intermediate lengths carry low incremental risk, but they were not
individually asserted.

## 27. 52-week proof

Proven for both profiles with a high baseline (45km/16km):
52 weeks, 208 slots, 32 GE weeks, 8 GE recovery weeks, every GE week
numeric, full validator pass. Diagnostic execution time: sub-second per run
in the observed test output (dominated by real file-system catalog reads
for the Core binding chain, no database/network call).

## 28. Determinism

Proven at both layers: `LongHorizonGeNumericExecutor.Execute` (repeated
calls, identical descriptor sequence and baseline, produce record-equal
results) and the full `LongHorizonDarkExecutionOrchestrator.ExecuteAsync`
(repeated end-to-end execution produces identical workout keys/versions,
dates, and numeric values). No random selection, no clock read beyond the
caller-supplied `StartDate`, no database dependency, no network dependency
beyond real (deterministic) file-system catalog reads.

## 29. Dark integration level

**Level A** (new internal orchestrator invoked only by tests) — the
preferred option. `LongHorizonDarkExecutionOrchestrator` and every new
Phase 4I.6 type are not registered in DI and have zero references outside
their own production files and their own test file (confirmed by grep).
Zero public schedule data is exposed; preview remains non-confirmable
(unchanged, since no code in the live `CatalogPreviewGenerator`/`PlanServices`
path was touched).

## 30. Public containment

Not re-verified by a new end-to-end HTTP test this phase (a disclosed
scope reduction) — but the reasoning is sound and checkable: since this
phase adds zero call sites into `PlanServices`/`CatalogPreviewGenerator`
(§29), the existing Phase 4I.3 `LongHorizonGenerationContainmentTests.cs`
(real Api host + real Postgres, covering 21/24/52/53 weeks) remains valid,
current evidence that public behavior is unchanged, and it is included
unmodified in the full backend suite result (§34).

## 31. Existing 8-20 regression

No file belonging to the 8-14 or 15-20 week paths was modified by this
phase. The full backend suite (§34) is the regression proof.

## 32. Governance status

`TD-LONG-HORIZON-COMPOSITION-001` through
`TD-LONG-HORIZON-STRUCTURAL-MATERIALIZATION-001`: CLOSED (unchanged).
`TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001`: **added OPEN** this
phase — a deliberate partial closure, not full. `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`:
remains OPEN.

## 33. Focused tests

24 new tests in `LongHorizonDarkExecutionOrchestratorTests.cs`: GE numeric
unit tests (missing baseline, first-week baseline, development cap, recovery
formula, post-recovery return-to-peak, GE32 8-recovery-weeks, weekly-
distribution sum, determinism, low-baseline infeasibility), orchestrator
end-to-end tests (representative horizons, workout-identity completeness,
Core workout-selection-pattern regression, date/weekday assignment, long-run-
day assignment, 52-week maximum both profiles, determinism, 20-week
rejection), and 2 direct validator tests. Plus 5 new governance cross-check
tests in plan-catalog. All passing.

## 34. Full backend suite

**2432 passed, 0 failed, 0 skipped** (10m17s), up from the Phase 4I.5
baseline of 2408 due to this phase's 24 new tests.

## 35. Full plan-catalog suite

**914 passed, 0 failed, 0 skipped** (909 baseline + 5 new governance tests).
No plan-catalog production file was modified by this phase (only governance
JSON/MD and one new test file).

## 36. Production defects found

None in pre-existing production code. One test-authoring correction in this
phase's own new test file: an 8km/week baseline test case incorrectly
assumed feasibility — 8km with no recent-longest-run anchor is genuinely
infeasible against the existing 6km session-minimum floor (a correct
fail-closed result, not a defect), so the test was split into a passing
"first week equals baseline" case (20/45km) and a dedicated infeasibility
test.

## 37. Production fixes made

None to pre-existing production code. All Phase 4I.6 production files are
new additions.

## 38. Explicit non-implementation statement

This phase does **not** implement: Preparation Runway numeric (weekly
volume/pace) execution for the 21-52 week path (the existing
`PreparationRunwayNumericMaterializer`/`PreparationRunwayCalendarComposer`/
`PreparationRunwayPaceMaterializer` stages are architecturally coupled, via
`PreparationRunwayStartingLoadEvidenceAdapter.FromAuthoritativeCoreContext`,
to a Core-derived starting-evidence value that does not exist when GE
precedes Runway instead of following it — resolving this cleanly requires
either a new adapter variant or a resequencing of the existing 12-stage
`TenKPreparationRunwayDarkOrchestrator`'s own internal ordering assumptions,
neither of which was undertaken this phase); Core numeric (weekly volume/
pace) execution (the existing full pipeline,
`DynamicCoreCalendarMaterializationOrchestrator`/`TenKPreparationRunwayCoreGenerator`,
requires a live-request-shaped `GeneratePreviewRequest`/`ResolverInputSnapshot`/
real-resolved `ConditionResults` including goal-feasibility/target-time
evidence that this dark, no-public-preview skeleton does not naturally
carry); GE→Runway and Runway→Core numeric transition/continuity (depends on
the above); pace/intensity resolution for Runway/Core; month/year-boundary-
specific calendar fixture tests (the underlying date arithmetic has no
special-case risk, but dedicated fixtures were not added); the full
21-52×2-profile×5-input-category matrix (representative horizons/profiles/
baselines were proven instead). Public preview, confirmation, persistence,
Flutter, and 53+ remain untouched, as required.

## 39. Residual blockers

`TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001` (OPEN): Preparation Runway
numeric execution, Core numeric execution, and the GE→Runway/Runway→Core
numeric transition rules that depend on them. `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`
(OPEN, umbrella): all of the above, plus dark pipeline wiring, public
preview, confirmation, and persistence.

## 40. Final classification

```
LONG_HORIZON_NUMERIC_AND_CALENDAR_EXECUTION_REMAINS_BLOCKED_BY_EXPLICIT_WORKOUT_BINDING_BASELINE_PROGRESSION_TRANSITION_PACE_OR_DATE_ASSIGNMENT_GAP
```

Specifically: workout binding (✅ resolved this phase) and date assignment
(✅ resolved this phase) are no longer the blocking gap; the remaining
blocking gap is Preparation Runway and Core numeric (volume/pace) execution
and the transition rules that depend on it.

## 41. Exact next phase

**Phase 4I.7 — Long-Horizon Preparation Runway and Core Numeric Execution
with GE-Derived Entry Baseline (Dark)**: resolve the Runway starting-evidence
coupling (new adapter variant consuming GE's final/peak volume instead of a
Core-derived value), construct a representative-but-real
`GeneratePreviewRequest`/`ResolverInputSnapshot`/`ConditionResults` context
sufficient to invoke the existing Core numeric pipeline in a dark/test-only
setting, execute both, and prove GE→Runway→Core numeric continuity — still
remaining dark/unwired, still not activating public preview, confirmation,
or persistence.
