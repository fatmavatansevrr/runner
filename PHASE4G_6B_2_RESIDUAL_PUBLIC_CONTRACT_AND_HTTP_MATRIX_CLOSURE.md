# Phase 4G.6B.2 — Residual Public Contract and HTTP Matrix Closure

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_PUBLIC_CONTRACT_AND_HTTP_MATRIX_FULLY_CLOSED_WITH_EXACT_DTO_EQUALITY_AND_NO_CONFIRMATION_ACTIVATION`

Final Phase 4G.6B closure:
`TEN_K_PREPARATION_RUNWAY_15_TO_20_WEEK_PUBLIC_PREVIEW_ACTIVATION_UNCONDITIONALLY_CLOSED_AND_READY_FOR_CONFIRMATION_PERSISTENCE_ACTIVATION`

All six residual gaps from Phase 4G.6B.1 are resolved and tested: the LeadingPartialDays 0–6 HTTP matrix is
complete, invalid-long-run-day is proven through the real endpoint, 52/53+/60-week containment is proven
through the real endpoint (with gate-state irrelevance also proven), a literal orchestrator-result-to-
public-DTO equality seam exists and passes, `NoRecentRunningBase` is formally decided (Option A — already
representable, no new field), and the `CoreHorizonDecision` public-API exposure from 4G.6B.1 is audited and
deliberately retained (Option D) with a documented reason and governance tests.

## 2. Inherited closed behavior

Everything from Phase 4G.6B.1's own closure (single horizon authority, real activation gate with tested
rollback, dead-exception removal, partial HTTP matrix) is the unmodified baseline. This phase closes the
residual gaps it explicitly disclosed.

## 3. Residual gaps addressed

1. LeadingPartialDays HTTP coverage was 0/3/6 only → now 0/1/2/3/4/5/6, all in one theory.
2. No invalid-long-run-day HTTP test existed → added, reusing the existing validator (no duplicate rule).
3. No 52/53+ HTTP test existed → added, plus gate-state-irrelevance at the `PlanServices` level.
4. No literal orchestrator-to-DTO equality seam existed → `PreparationRunwayPublicPreviewMapper` extracted
   as a pure, internal, production-owned mapper; equality tests compare the SAME orchestrator result object
   against its own mapped output.
5. `NoRecentRunningBase` vs. `Missing` was undecided → Option A confirmed and tested (already
   representable via explicit `0` vs. omitted/`null`).
6. `CoreHorizonDecision`/`CoreHorizonMode`/`CoreHorizonDecisionReason` public exposure (introduced in
   4G.6B.1) was unaudited → audited, Option D chosen (retain public, narrowly, for a concrete DI-visibility
   reason), documented, and governance-tested.

## 4. Artifacts inspected

`CatalogPreviewGenerator.cs`, `PreparationRunwayPublicPreviewMapper.cs` (new), `CoreHorizonClassifier.cs`,
`ICatalogPreviewGenerator`, `Program.cs` (DI registration), `GenerateRacePlanPreviewRequestValidator.cs`,
`TenKPreparationRunwayDarkOrchestrator.cs`/`...Contracts.cs`, `CatalogPrescriptionContextBuilder.cs`
(`CatalogPrescriptionInputNormalizer.NormalizeDistance`), `TenKPreparationRunwayComponentAdapters.cs`
(`PreparationRunwayStartingLoadEvidenceAdapter`), `GlobalExceptionHandler.cs`,
`PreparationRunwayPreview15To20WeekEndToEndTests.cs`, `LongHorizonFailClosedTests.cs`,
`PreparationRunwayHorizonAuthorityTests.cs`.

## 5. LeadingPartialDays 0–6 HTTP proof

`PilotScope_LeadingPartialDays_EmergeFromDatesOnly_NoPartialWeekOrMisalignment` (theory, now
`[InlineData(0..6)]`, 18-week horizon, remainder derived purely from `StartDate`/`RaceDate` arithmetic,
never submitted directly). For every value: HTTP 200, exact 18-week/72-session count, no session before
`StartDate + LeadingPartialDays`, every week has exactly 4 sessions (no partial week), the Core boundary
(`runwayWeekCount = 6`) and final-runway-block (`PRE_SPECIFIC_TRANSITION`) remain correct, `lifecycle`
stays non-confirmable, and exactly one new `PlanPreview` row is created with zero `TrainingPlan`/
`TrainingWeek`/`TrainingDay` writes.

## 6. Invalid long-run-day HTTP proof

`PilotScope_LongRunDayNotInPreferredDays_ReturnsTypedValidationError_NoOrchestrationOrPersistence` —
`preferred_days=[mon,wed,fri,sun]`, `long_run_day=tue` (not a member). Reuses the existing, unmodified
`GenerateRacePlanPreviewRequestValidator` rule (`ArgumentException`) — no runway-specific duplicate
validator was added. Asserts HTTP 400, `errorCode=VALIDATION_ERROR`, and zero row-count delta across
`PlanPreview`/`TrainingPlan`/`TrainingWeek`/`TrainingDay` (validation runs before any candidate load or
orchestration, so the counting-fake-style proof used elsewhere isn't needed — the row-count delta itself
proves the orchestrator was never reached).

## 7. Public 52/53+ containment

`PilotScope_FiftyTwoAndAbove_StillReturns422_PlanHorizonCompositionRequired_NoOrchestration` (52/53/60
weeks) — same `PLAN_HORIZON_COMPOSITION_REQUIRED` as every other unsupported horizon, no `weeks` field in
the error body, zero row-count delta. `LongHorizonFailClosedTests.FiftyTwoAndAbove_GateStateDoesNotAlterResult_ExactPilotIdentity`
(52/53 weeks × enabled/disabled gate, 4 cases) proves the activation gate has zero effect at 52/53+ — the
same `PlanHorizonCompositionRequiredException`, zero orchestrator/generator invocation, zero `PlanPreview`
rows, regardless of gate state. 21 weeks and exact-pilot 15–20 success are re-proven unchanged
(pre-existing tests, unmodified).

## 8. DTO-equality seam design

`PreparationRunwayPublicPreviewMapper` (new file,
`RunningApp.Application/RuntimeCatalog/PreviewRouting/PreparationRunwayPublicPreviewMapper.cs`) — an
`internal static` class containing `MapCombinedWeeks(TenKPreparationRunwayDarkOrchestrationResult)` and
`RunwayBlockPublicName(PreparationRunwayBlockType)`, extracted verbatim (no logic change) from
`CatalogPreviewGenerator`'s former private methods. `CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync`
now calls `PreparationRunwayPublicPreviewMapper.MapCombinedWeeks(orchestrationResult)` instead of a local
private method — the exact same code path, just relocated so `RunningApp.IntegrationTests` (which has an
`InternalsVisibleTo` grant into `RunningApp.Application`) can call it directly against a real orchestrator
result, satisfying "expose the existing mapping method internally... through the repository's existing
InternalsVisibleTo convention" (option 2 from the task's own preferred list) without making any domain type
public and without duplicating the mapping logic in the test.

## 9. Runway week equality

`PreparationRunwayPublicPreviewMapperEqualityTests.MappedWeeks_AreFieldByFieldEqual_ToAuthoritativeOrchestratorResult`
(theory, READY and NOT_READY profiles) iterates every mapped runway week and asserts, per session:
`Date`, `DistanceKm`, `Intensity`, and `DayType` (derived from `SlotRole`) all equal the corresponding
`result.PacedRunway.PacedRunwayWeeks[...].ChronologicalSlots[...]` fields directly — read from the SAME
result object the mapper consumed, never recalculated or reconstructed independently. Also asserts
`RunwayBlock` equals `PreparationRunwayPublicPreviewMapper.RunwayBlockPublicName(blockType)` from
`result.PacedRunway`'s own `BlockType`, and per-week chronological ordering.

## 10. Core week equality

The same test iterates every mapped Core week and asserts each session's `Date`/`DistanceKm`/`Intensity`/
`DayType` against `result.CoreResult.PrescriptionResult.FinalPrescribedPlan.Weeks[...].Sessions[...]`
directly, plus `WeekType` against the session's own `PhaseKey`, and `RunwayBlock == null` for every Core
week.

## 11. Plan-level equality

Total week count (`mapped.Count == result.CalendarComposition.OrderedCombinedWeeks.Count == 18`), total
session count (`mapped.Sum(w => w.Days.Count) == totalOrchestratorSessions`, computed independently from
the orchestrator's own paced-runway/Core-week collections, not hardcoded), final runway block
`PRE_SPECIFIC_TRANSITION`, and no Core week ever carrying a `RunwayBlock`.

## 12. Mapper ownership and purity

`PreparationRunwayPublicPreviewMapper` is `internal`, pure (no I/O, no DbContext, no generation/allocation
component invocation), and reads only from the supplied, already-completed orchestration result.
`Mapper_IsValueDeterministic_RepeatedMappingOfSameResult_IsIdentical` (mapping the same result twice
produces identical flattened output) and `Mapper_InputCollectionOrder_DoesNotAlterNormalizedOutput`
(reversed `PreferredDays`/`ConditionResults` in the orchestrator request produce identical mapped output)
both pass. No internal trace/progression/policy identifier is ever written into `PreviewWeekDto`/
`PreviewDayDto` (neither type has a field capable of carrying one — verified structurally, not just by
absence-of-token search).

## 13. NoRecentRunningBase public-contract decision

**Option A** — the existing public request contract already distinguishes `Missing` from
`NoRecentRunningBase`, no new field needed. Traced through
`CatalogPrescriptionInputNormalizer.NormalizeDistance` (`value is null → PrescriptionInputState.NotProvided`;
any provided value including `0` → `PrescriptionInputState.Available`) and
`PreparationRunwayStartingLoadEvidenceAdapter.State` (`Available` + `value == 0` →
`PreparationRunwayLoadEvidenceState.NoRecentRunningBase`; `NotProvided` → `Missing`) — two genuinely
distinct internal states already reachable from two genuinely distinct public HTTP payloads (`0` vs.
omitted/`null`), with zero request-contract change required.

## 14. Missing/no-base normalization

`PilotScope_ExplicitZeroRecentEvidence_NoRecentRunningBase_StillSucceeds` — explicit
`recent_weekly_volume_km: 0, recent_longest_run_km: 0, recent_runs_per_week: 0` — HTTP 200, 17 weeks,
non-confirmable lifecycle, proving the approved (successful, effort-only) public behavior for this state.
`PilotScope_MissingEvidence_StillSucceeds` (pre-existing, from Phase 4G.6B) proves the `Missing`
(omitted/`null`) case remains stable and unaffected by this phase.

## 15. CoreHorizonDecision API-surface audit

Audited per the task's own decision tree:
1. Is `ICatalogPreviewGenerator` required to be public? **Yes** — `RunningApp.Api`'s `Program.cs` registers
   it via `AddScoped<ICatalogPreviewGenerator, CatalogPreviewGenerator>()`, and `RunningApp.Api` has no
   `InternalsVisibleTo` grant into `RunningApp.Application` (confirmed: only `RunningApp.IntegrationTests`
   has that grant, per `RunningApp.Application.csproj`). A public interface cannot expose an internal
   parameter type (CS0051).
2. Splitting an internal `IPreparationRunwayPreviewGenerator` (Option B/C) was evaluated and rejected: the
   DI container would still need `RunningApp.Api` to name that internal type at a registration call site,
   hitting the identical visibility wall — a real fix would require a new Application-side public DI
   extension method (e.g. `AddPreparationRunwayPreviewGenerator(this IServiceCollection)`), a materially
   larger change than this audit's proportionate scope.
3. No external (non-`RunningApp.Application`/`RunningApp.IntegrationTests`) assembly consumes
   `CoreHorizonDecision` directly.

**Decision: Option D — retain public, deliberately and narrowly.** Documented in
`CoreHorizonClassifier.cs`'s own doc comment (the type's declaration site). Governance tests prove the
exposure is intentional and safely narrow:
- `CoreHorizonDecisionTypes_AreIntentionallyPublic_APISurfaceGovernanceAssertion` — confirms the three
  types are public and that the only public method referencing `CoreHorizonDecision` is
  `ICatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync`.
- `CoreHorizonDecisionTypes_AreNeverUsedAsHttpResponseFields` — reflects over
  `GeneratePreviewResponse`/`PreviewWeekDto`/`PreviewDayDto` and asserts no property has any of the three
  types.
- `CoreHorizonDecisionTypes_NotReferencedInApiControllersOrSwaggerFilters` — source-scans every `.cs` file
  under `RunningApp.Api` and asserts none references `CoreHorizonDecision`.

## 16. Interface/route-context decision

No interface split and no new internal route-context type were introduced — Option D (§15) makes both
unnecessary for now. This is recorded as the explicit decision, not an oversight.

## 17. Single horizon authority regression

Unchanged and re-verified: all 9 `PreparationRunwayHorizonAuthorityTests` from Phase 4G.6B.1 still pass
unmodified; the mapper extraction in this phase touches only where the mapping method is *defined*, not
the horizon-decision carrying logic.

## 18. Lifecycle behavior

Unchanged: every new test in this phase that reaches a successful runway preview asserts
`lifecycle == "preparation_runway_preview_not_confirmable"`.

## 19. Confirmation containment

Not modified in this phase. Pre-existing `RunwayPreview_ConfirmIsRejected_NoTrainingPlanWeekOrDayWritten`
re-run and passes unchanged (part of the full-suite re-run, §27).

## 20. Persistence zero-write proof

Every new HTTP test in this phase (LeadingPartialDays theory, invalid-long-run-day, 52/53+/60, explicit-
zero-evidence) asserts row-count deltas across `PlanPreview`/`TrainingPlan`/`TrainingWeek`/`TrainingDay`
via `CountRowsAsync()`.

## 21. Failure atomicity

The two new failure fixtures (invalid long-run-day, 52/53+/60) both assert: single typed error code, no
`weeks` field in the error body (no partial schedule), zero row-count delta (no persistence), and no
generic 500.

## 22. Swagger/API contract

No response-schema changes were needed in this phase (no new public DTO field was added — `NoRecentRunningBase`
required no contract change per §13, and the mapper extraction is fully internal). The pre-existing
`DtoExamplesSchemaFilter.cs` (`runway_block`/`lifecycle` examples, from Phase 4G.6B) is unaffected. The new
`CoreHorizonDecisionTypes_NotReferencedInApiControllersOrSwaggerFilters` governance test (§15) is the
closest thing to a "no CoreHorizonDecision exposure in HTTP schema" proof the task asks for, given no
response-schema test infrastructure exists for this endpoint (confirmed in Phase 4G.6B's own report).

## 23. Existing 8–14 regression

Re-proven unchanged: `PilotScope_EightToFourteenWeeks_RemainOnExistingCorePath_NotPreparationRunwayLifecycle`
(pre-existing) plus `DisabledGate_EightToFourteenWeeks_Unaffected` (from 4G.6B.1) both pass in the full
suite re-run.

## 24. Existing 15–20 regression

Re-proven unchanged: all six horizons (15–20) still succeed via the pre-existing proof-matrix theory; the
disabled-gate rollback tests from 4G.6B.1 still pass.

## 25. 21+/other-candidate containment

Re-proven unchanged (21 weeks, other-candidate-at-17-weeks, gate-state-irrelevance for other candidates —
all pre-existing from 4G.6B/4G.6B.1) plus this phase's new 52/53/60-week and gate-state-for-long-horizons
tests.

## 26. Production/public/confirm/persistence classification

Unchanged from 4G.6B.1: `TenKPreparationRunwayDarkOrchestrator` remains `PRODUCTION_OWNED /
DARK_INTERNAL_CALLABLE / PUBLICLY_UNREACHABLE(directly) / PERSISTENCE_UNREACHABLE`. The new
`PreparationRunwayPublicPreviewMapper` is `PRODUCTION_OWNED / INTERNAL / PUBLICLY_UNREACHABLE(directly,
only reachable through `CatalogPreviewGenerator`) / PERSISTENCE_UNREACHABLE` (pure, no side effects). The
public path (`CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync` → `PlanServices.
GeneratePreparationRunwayPreviewAsync`) is unchanged: `PUBLICLY_REACHABLE(scoped, preview-only) /
PERSISTENCE_UNREACHABLE(confirm blocked, PlanPreview row only)`.

## 27. Test results

New/modified tests this phase:
- `PreparationRunwayPreview15To20WeekEndToEndTests.cs`: LeadingPartialDays theory extended (0/1/2/3/4/5/6,
  strengthened assertions), new invalid-long-run-day test, new 52/53/60-week theory, new explicit-zero
  (NoRecentRunningBase) test — 4 test methods, 13 total cases including theories.
- `LongHorizonFailClosedTests.cs`: new `FiftyTwoAndAbove_GateStateDoesNotAlterResult_ExactPilotIdentity`
  theory (4 cases).
- `PreparationRunwayHorizonAuthorityTests.cs`: 3 new API-surface governance tests.
- `PreparationRunwayPublicPreviewMapperEqualityTests.cs` (new file): 4 tests (1 theory ×2 cases + 2 facts).

Focused run (this phase's new/modified test files): **91 passed, 0 failed, 0 skipped**.

Full suite results (final, current source):
- `dotnet test RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj -c Release`: **2188 passed,
  0 failed, 0 skipped**.
- `dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj -c Release`: **394 passed, 0
  failed, 0 skipped** (unaffected — no plan-catalog files touched).

## 28. Residual frontend compatibility note

Not inspected in this phase (unchanged from 4G.6B.1) — no `mobile/` files were read or changed, no
frontend-readiness claim is made.

## 29. Final Phase 4G.6B closure decision

`TEN_K_PREPARATION_RUNWAY_15_TO_20_WEEK_PUBLIC_PREVIEW_ACTIVATION_UNCONDITIONALLY_CLOSED_AND_READY_FOR_CONFIRMATION_PERSISTENCE_ACTIVATION`

All six mandatory items from this phase's own success conditions are met: complete LeadingPartialDays 0–6
HTTP coverage, endpoint-proven invalid-long-run-day, endpoint-proven 52/53+ containment, a real (not
JSON-snapshot) orchestrator-to-DTO equality seam, an explicitly decided `NoRecentRunningBase` contract
(Option A, tested), and an explicitly decided/audited horizon-decision visibility (Option D, governance-
tested). Confirmation and persistence remain fully blocked and untouched. This closes the public-contract
and HTTP-verification track of Phase 4G.6B; what remains outside this track (frontend integration, a real
confirmation/persistence design for 15–20 week previews) is explicitly out of scope and not implied by
this closure.

## 30. Exact next phase

A **separate, explicitly-scoped** confirmation/persistence design phase for 15–20 week Preparation Runway
previews — the first phase in this track that would need to design what "confirming" a combined runway+Core
preview actually means for `TrainingPlan`/`TrainingWeek`/`TrainingDay` persistence (today's schema/writer
was built for a single, homogeneous 8–14 week Core plan only). Not started, not implied, not silently
enabled by anything in this phase or its predecessors.
