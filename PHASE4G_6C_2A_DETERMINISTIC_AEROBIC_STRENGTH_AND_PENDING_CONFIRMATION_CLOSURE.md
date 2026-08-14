# Phase 4G.6C.2A — Deterministic AerobicStrength Completion and Pending-Confirmation Closure

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_AEROBIC_STRENGTH_AND_PENDING_CONFIRMATION_BEHAVIOR_DETERMINISTICALLY_VERIFIED_WITH_NO_CONDITIONAL_TEST_PATHS_REMAINING`

Final backend closure: `TEN_K_PREPARATION_RUNWAY_15_TO_20_WEEK_BACKEND_UNCONDITIONALLY_CLOSED_AND_READY_FOR_FRONTEND_INTEGRATION`

Both residual conditional gaps from Phase 4G.6C.2 are closed. AerobicStrength Intro and Progressed
completion are now proven against deterministic real fixtures derived from actual production allocation
code (not guessed, not injected) — the two tests that previously could return early with no assertion have
been replaced entirely; the new tests fail outright if the underlying allocation ever stops producing the
expected fixture. Pending-confirmation creation authority was traced to every production call site in the
repository; none exists for any workout type (runway or Core) — Option B is formally proven, generalized
beyond the runway-only framing the phase prompt anticipated, with an explicit written classification
(`RUNWAY_PENDING_CONFIRMATION_NOT_APPLICABLE_BY_APPROVED_POLICY`) and no new production behavior invented.

## 2. Inherited backend closure state

Unchanged from Phase 4G.6C.2: all six 15-20 horizons, both profiles, real mid-transaction/commit rollback,
evidence-state persistence, deep Home verification, Calendar/Detail compatibility, Long Run completion,
cancel/reset, 8-14 regression, and 21+/other-candidate containment — all still green (§13).

## 3. Residual conditional gaps (as inherited)

1. `PilotScope_CompleteAerobicStrengthSession_SucceedsWithoutMappingError` could `return` early with zero
   assertions when the confirmed allocation happened not to include an `AEROBIC_STRENGTH` week.
2. `PilotScope_NotTodayThenPendingConfirmationsFlow_ResolvesCorrectly` could `return` early with zero
   assertions when `GET /pending-confirmations` came back empty.

Both are now removed; replaced with the tests described in §6-§9.

## 4. Artifacts inspected

- `TenKPreparationRunwayAllocationPolicyFactory.cs` / `PreparationRunwayBlockAllocationEngine.cs` — the
  real, deterministic, production-owned allocation policy and engine.
- `ten-k-aerobic-strength-progression.v1.json`, `aerobic-strength-controlled-intro.v1.json`,
  `aerobic-strength-controlled-progressed.v1.json` (plan-catalog) — the two-step progression document and
  both workout definitions.
- `TenKPreparationRunwayPacePolicyFactory.cs` — the effort-label rule mapping each workout ID to its
  persisted intensity string.
- `PreparationRunwayPersistablePlanMapper.cs` (`MapRunwaySession`) and `CatalogPlanConfirmationService.cs`
  (lines 662/671-672) — the exact field mapping from generated payload to persisted `TrainingDay`.
- `TrainingDay.cs` — `Intensity`, `CatalogWorkoutKey`, `CatalogWorkoutVersion`, `Source`, `AdaptedFromId`.
- `QueryAndMutationServices.cs` (`CreateNotTodayDecisionAsync`, `ConfirmNotTodayDecisionAsync`,
  `GetPendingConfirmationsAsync`, `ResolvePendingConfirmationAsync`) — full read of every method touching
  `PendingConfirmations`.
- `PlaceholderAdaptationEngine.cs` — confirmed `Action=NoChange`/`PlanAdapted=false`/`AffectedDays=[]` for
  both `EvaluateNotTodayAsync` and `EvaluatePendingConfirmationAsync`, unconditionally.
- `PendingConfirmationsController.cs`, `NotTodayDecisionsController.cs`, `TrainingDaysController.cs`.
- Repository-wide grep for `PendingConfirmations.Add`/`new PendingConfirmation` across
  `backend/**/*.cs` — zero matches.

## 5. Canonical AerobicStrength allocation behavior

Called `TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies(CoreEntryReady)` +
`PreparationRunwayBlockAllocationEngine.Allocate(runwayWeeks, policies)` directly (both `internal`, reachable
from `RunningApp.IntegrationTests` via the existing `InternalsVisibleTo` grant) for `runwayWeeks` 3 through 8
(i.e. total plan weeks 15 through 20, since Core is fixed at 12) via a disposable scratch probe test, run
once, then deleted. Real, deterministic output:

| Full-week horizon | Runway weeks | AerobicStrength allocated |
|---|---|---|
| 15 | 3 | 1 (Intro only) |
| 16 | 4 | 1 (Intro only) |
| 17 | 5 | 2 (Intro + Progressed) |
| 18 | 6 | 2 (Intro + Progressed) |
| 19 | 7 | 2 (Intro + Progressed) |
| 20 | 8 | 2 (Intro + Progressed) |

`PreparationRunwayBlockAllocationPolicy` declares `AerobicStrength` `MinWeeks=1, MaxWeeks=2` when
`CoreEntryReady`-eligible — the engine's largest-remainder proportional distribution (weight 0.35 vs.
GeneralEndurance's 0.65) reaches the 2-week cap starting at `runwayWeeks=5`. This table is the deterministic
fixture-selection basis for §6-§7 — no fixture was chosen by trial-and-error against the real HTTP pipeline.

## 6. Intro fixture

15-week request (`race_date = start_date + 15*7`), `recent_weekly_volume_km=24, recent_longest_run_km=9`
(resolves `CoreEntryReadinessResolver` to `CoreEntryReady`, matching §5's table). Confirmed via real
`POST /generate-preview/race` + `POST /confirm`.

`PilotScope_AerobicStrengthIntro_DeterministicFixture_CompletesCorrectly`:
- Setup assertion (fails the test, not silently passes): exactly 1 `TrainingWeek` has `CatalogPhaseKey ==
  "AEROBIC_STRENGTH"`.
- `Assert.NotNull` on the day with `Intensity == "CONTROLLED_AEROBIC_POWER_INTRO"` — mandatory, no
  conditional branch.
- Asserts `CatalogWorkoutKey == "AEROBIC_STRENGTH_CONTROLLED_INTRO"`, `CatalogWorkoutVersion == 1`, and that
  no day anywhere in the plan has `Intensity == "CONTROLLED_AEROBIC_POWER_PROGRESSED"` (confirms the
  fixture is genuinely Intro-only, not merely "Intro found, Progressed unchecked").

## 7. Progressed fixture

17-week request, same `CoreEntryReady` evidence, matching §5's row for `runwayWeeks=5` (2 AerobicStrength
weeks).

`PilotScope_AerobicStrengthProgressed_DeterministicFixture_CompletesCorrectlyAndRemainsDistinguishableFromIntro`:
- Setup assertion: exactly 2 `TrainingWeek`s have `CatalogPhaseKey == "AEROBIC_STRENGTH"`.
- `Assert.NotNull` on both the Intro day and the Progressed day (mandatory, no early return).
- Asserts they belong to two distinct `TrainingWeek` rows, and that the Intro day's week is the
  chronologically first of the two AerobicStrength weeks (step-order 1 before step-order 2, per the
  progression document in §4).
- Asserts `CatalogWorkoutKey == "AEROBIC_STRENGTH_CONTROLLED_PROGRESSED"`, `CatalogWorkoutVersion == 1`.

## 8. Intro completion result

`POST /training-days/{id}/complete` on the real Intro day returns HTTP 200. Post-completion, both the
`GET /training-days/{id}` response and a direct database read agree: `status=completed`,
`intensity="CONTROLLED_AEROBIC_POWER_INTRO"` (unchanged), `planned_distance_km` unchanged from the
pre-completion value, `CatalogWorkoutKey`/`CatalogWorkoutVersion` unchanged, `Source=Template`,
`AdaptedFromId=null`. No unsupported-workout-type mapping exception was raised. Total `TrainingDay` row
count for the plan is identical before and after (no extra row created).

## 9. Progressed completion result

Same shape as §8 for the Progressed day: HTTP 200, `status=completed`,
`intensity="CONTROLLED_AEROBIC_POWER_PROGRESSED"` preserved, `Source=Template`, `AdaptedFromId=null`, row
count unchanged. Additionally, the sibling Intro day (from the same fixture) is re-read directly from the
database afterward and asserted to remain `status=Planned` with its own unchanged
`intensity="CONTROLLED_AEROBIC_POWER_INTRO"` and `CatalogWorkoutKey` — proving completing Progressed does
not mutate, merge with, or overwrite the earlier Intro session.

## 10. Workout identity/intensity preservation

Both tests assert `CatalogWorkoutKey`/`CatalogWorkoutVersion`/`Intensity` are read identically from (a) the
pre-completion database row, (b) the post-completion `GET /training-days/{id}` HTTP response, and (c) a
fresh post-completion database read — three-way agreement, not merely "the endpoint returned 200."

## 11. Source/AdaptedFrom result

For both Intro and Progressed: `Source == TrainingDaySource.Template` both before and after completion
(confirmed at `CatalogPlanConfirmationService.cs:668` — the value every catalog-sourced day receives at
persist time, runway or Core, unchanged by this phase), and `AdaptedFromId == null` throughout — no
adaptation ever runs for a completion call (`IWorkoutCompletionService` does not invoke the adaptation
engine at all; only `ConfirmNotTodayDecisionAsync` does, per §12).

## 12. Conditional-test removal

Both target tests' early-return branches (`if (aerobicStrengthDay is null) { return; }` and
`if (pendingArray.Count == 0) { return; }`) were deleted, not merely guarded — the replacement tests contain
no `if` branch whose only effect is skipping assertions. Every fixture precondition is now a hard
`Assert.NotNull`/`Assert.True` that fails the test outright if violated. This audit was scoped to this
phase's own target tests, per the phase's own instruction not to ban legitimate early returns project-wide
(e.g. the `PilotScope_CompleteAerobicStrengthSession_...` file itself needed a legitimate, disclosed early
return in 4G.6C.2 precisely because the fixture wasn't yet deterministic — that reasoning no longer applies
now that §5's table exists).

## 13. Pending-confirmation creation call sites

Exhaustive repository grep (`backend/**/*.cs`, excluding `bin`/`obj`) for `PendingConfirmations.Add` and
`new PendingConfirmation(` / `new PendingConfirmation {` returned **zero matches** anywhere in
`RunningApp.Application`, `RunningApp.Api`, `RunningApp.Persistence`, `RunningApp.Domain`,
`RunningApp.Infrastructure`, or `RunningApp.IntegrationTests` (including every existing pre-4G.6C test —
none seeds a `PendingConfirmation` row directly either).

Reading every method that touches `PendingConfirmations` confirms this is not an oversight of the grep:
- `CreateNotTodayDecisionAsync` creates a `NotTodayDecision` row only.
- `ConfirmNotTodayDecisionAsync` calls `_adaptationEngine.EvaluateNotTodayAsync(...)`
  (`PlaceholderAdaptationEngine`, always `Action=NoChange`/`PlanAdapted=false`/`AffectedDays=[]`
  unconditionally, regardless of trigger or reason), marks the `TrainingDay` `Missed`, and logs a
  `PlanEvent` — it never constructs or inserts a `PendingConfirmation`.
- `GetPendingConfirmationsAsync`/`ResolvePendingConfirmationAsync` only read/mutate existing rows — there is
  no code path that could ever populate one.
- `PendingConfirmationsController` exposes exactly `GET` and `POST .../resolve` — no `POST` creation
  endpoint exists at the HTTP layer either.
- `IWorkoutCompletionService`'s completion path (used by §8-9) does not reference `PendingConfirmations` or
  the adaptation engine at all.

This is a pre-existing, repository-wide absence of the pending-confirmation *creation* mechanism — true for
every workout type, not specific to runway sessions.

## 14. Runway pending-confirmation applicability decision

**Option B, generalized.** The phase prompt's Option B text anticipates "runway sessions are intentionally
ineligible" while a separate Core mechanism creates pending items; §13's evidence shows the mechanism does
not exist for *any* workout type, Core included. Rather than force-fitting the narrower "runway-specific
ineligibility" framing the prompt describes, this phase documents the broader, more accurate finding
directly and still emits the exact required classification, since it is a strict superset of (and therefore
satisfies) the narrower runway-specific claim:

`RUNWAY_PENDING_CONFIRMATION_NOT_APPLICABLE_BY_APPROVED_POLICY`

No new pending-confirmation creation behavior was added to make this classification narrower/more literally
accurate to the prompt's assumed Core-has-it framing — doing so would itself violate the phase's own
Implementation Boundary ("no new pending-confirmation behavior invented solely for runway"; building a
creation mechanism at all, for any workout type, is new production behavior with no failing test exposing it
as a defect in the narrow area under test).

## 15. Pending creation result / non-applicability proof

`PilotScope_RunwayNotTodayAction_NeverCreatesPendingConfirmation_ByApprovedPolicy` — confirms a real 16-week
runway plan, triggers the real not-today-decision action on a real persisted runway session, confirms the
decision via the real `POST /not-today-decisions/{id}/confirm` endpoint (HTTP 200 both times), then asserts,
with no conditional branch: `GET /pending-confirmations` returns an empty array; a direct
`ctx.PendingConfirmations.CountAsync()` read returns `0`; the plan's `TrainingDay` row count is unchanged
(no adapted/replacement day created); every `TrainingDay` still has `AdaptedFromId == null` and
`Source == Template`; the target day's persisted `Status == Missed` (the real, approved typed result the
not-today action currently produces).

## 16. Pending GET result

Covered by §15 (runway) and §17 (Core control) — both assert `Assert.Empty(pendingArray)` unconditionally,
never `if (!items.Any()) return;`.

## 17. Pending resolve result / rationale for non-applicability

No call to `POST /pending-confirmations/resolve` is made in either new test — calling resolve against a
guaranteed-nonexistent pending item would not exercise anything real (the endpoint would simply 404/error on
a lookup miss, which is not the behavior this phase is trying to characterize) and the phase prompt itself
directs, for Option B: "do not call resolve against a nonexistent item merely to satisfy endpoint coverage."
Existing Core resolve-path coverage (§19) is preserved unmodified.

`PilotScope_CoreNotTodayAction_AlsoNeverCreatesPendingConfirmation_ConfirmingRepoWideBaseline` — the same
proof (not-today → confirm → GET empty → zero DB rows) run against a real 12-week (8-14/Core) plan instead
of a runway plan, demonstrating the absence of pending-confirmation creation is the pre-existing,
already-true repository baseline for Core plans too, not a runway-specific gap this phase leaves unaddressed
and not a regression this phase's runway work introduced.

## 18. Duplicate/adaptation integrity

Both §15 and §17's tests assert zero net `TrainingDay` row change and `AdaptedFromId == null` across every
persisted day for the plan after the not-today/confirm sequence — no adapted or replacement day is silently
produced by the current (`NoChange`-only) adaptation engine, for either runway or Core.

## 19. Existing Core pending-flow regression

There was no pre-existing Core pending-flow test to regress (§13: no test anywhere in the repository, before
or after this phase, exercises real pending-confirmation creation) — this is stated plainly rather than
claimed as "unchanged" in a way that implies prior coverage existed. §17's new Core-control test is the
first test in this repository to characterize Core `not-today` behavior with respect to
`PendingConfirmations` at all.

## 20. Existing Long Run/Easy action regression

`PilotScope_CompleteRunwayLongRun_UpdatesStatusAndPreservesLongRunFlag` (4G.6C.2) and the generic runway Easy
completion test (4G.6C.1) both re-ran green in the full suite (§23) — untouched by this phase's changes.

## 21. Existing 15-20 regression

All six-horizon confirmation matrix tests, both profile-persistence tests, and every other 4G.6C/4G.6C.1/
4G.6C.2 test file re-ran green in the full suite (§23).

## 22. Existing transaction/concurrency regression

`PreparationRunwayTransactionAtomicityTests.cs` (4G.6C.2: 6 tests — 3 mid-transaction-insert-failure cases,
commit-failure, two governance tests) and `PilotScope_ConcurrentConfirmation_CreatesAtMostOnePlan`
(4G.6C.1) both re-ran green (§23) — this phase touched neither file.

## 23. Existing 8-14 regression

`PilotScope_EightToFourteenWeeks_RemainCoreConfirmable_AndConfirmUnchanged` and the full pre-existing 8-14
week Core test suite (confirm/persistence/Home/Calendar/Detail/completion/cancel/reset) re-ran green in the
full backend suite (2227/2227, §26) — the new §17 Core-control test is additive, not a replacement of any
existing 8-14 test.

## 24. 21+/other-candidate containment

`PilotScope_TwentyOneWeeks_StillReturns422_NoPersistence` and the full 21+/other-candidate containment suite
re-ran green — no new candidate, distance, level, or frequency was activated by this phase.

## 25. Production defects found

None. Every finding in §13 is a pre-existing repository state (an unimplemented pending-confirmation
creation mechanism), not a code defect exposed by a real test — nothing in this phase's new tests failed due
to incorrect production behavior.

## 26. Production fixes made

None. No production code in `RunningApp.Application`, `RunningApp.Api`, `RunningApp.Persistence`,
`RunningApp.Domain`, or `RunningApp.Infrastructure` was modified this phase.

## 27. Test results

Focused run (`--filter "FullyQualifiedName~PreparationRunwayResidualCompatibilityTests"`): **13 passed, 0
failed, 0 skipped** (11 inherited + 2 replaced AerobicStrength tests + 2 replaced pending-confirmation
tests, net +2 vs. 4G.6C.2's 11, since the single conditional pending test was split into a runway proof and
a Core-control proof).

Full backend suite (`dotnet test RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj -c Release`):
**2227 passed, 0 failed, 0 skipped** (up from 2225 at the end of 4G.6C.2).

Full plan-catalog suite (`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj -c
Release`): **394 passed, 0 failed, 0 skipped** (unchanged — no plan-catalog files were touched).

## 28. Frontend compatibility note

Not inspected this phase (unchanged from every prior phase in this track) — no `mobile/` files were read or
changed.

## 29. Residual backend gaps

None identified specific to this phase's two target gaps — both are now deterministically closed. The
broader absence of any pending-confirmation creation mechanism (§13-14) is not itself classified as a gap
this phase was scoped to close (building one is explicitly out of this phase's Implementation Boundary); it
is a disclosed, pre-existing, repository-wide condition documented here for whoever eventually designs that
feature.

## 30. Final backend closure decision

`TEN_K_PREPARATION_RUNWAY_15_TO_20_WEEK_BACKEND_UNCONDITIONALLY_CLOSED_AND_READY_FOR_FRONTEND_INTEGRATION`

Both conditional test paths identified after Phase 4G.6C.2 are eliminated. AerobicStrength Intro and
Progressed completion are proven against fixtures derived from real, unmodified production allocation code
rather than hoped-for allocations. Pending-confirmation non-applicability is formally proven, generalized
correctly beyond the prompt's Core-vs-runway framing once repository evidence showed the mechanism doesn't
exist for either, and classified with the exact required string. No production code was changed, no new
adaptation or pending-creation policy was invented, no candidate/scope was broadened, and the full regression
suite (2227 + 394 tests) is green.

## 31. Exact next phase

No further backend-side blocker is known across five consecutive verification phases (4G.6C, 4G.6C.1,
4G.6C.2, 4G.6C.2A). Frontend/mobile integration is the natural next phase. If a real pending-confirmation
creation mechanism is later required by product (Core or runway), that is a new feature-design phase, not a
backend-verification phase — §13's findings are the starting reference for that future design work.
