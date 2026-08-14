# Phase 4G.6B.1 — Public Preview Activation Authority and Coverage Hardening

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_PUBLIC_PREVIEW_ACTIVATION_HARDENED_WITH_SINGLE_HORIZON_AUTHORITY_COMPLETE_HTTP_MATRIX_AND_TESTED_ROLLBACK`

Final Phase 4G.6B closure classification:
`TEN_K_PREPARATION_RUNWAY_15_TO_20_WEEK_PUBLIC_PREVIEW_ACTIVATION_FULLY_CLOSED_AND_READY_FOR_SEPARATE_CONFIRMATION_PERSISTENCE_PHASE`

Both closure gaps from Phase 4G.6B are resolved: the public runway preview path now carries one
authoritative `CoreHorizonDecision` end-to-end (no second `RaceHorizonPolicy.Decide` call anywhere in
that path), and a real, tested, candidate-scoped activation gate now controls the 15–20 week route with
verified enabled/disabled rollback behavior. A genuine pre-existing pipeline bug was NOT found this time
(unlike 4G.6B); one real HTTP-matrix test-authoring mistake (using canonical values incompatible with
distance/pace-source combinations) was caught and fixed, not a production defect.

## 2. Inherited working vertical slice

Phase 4G.6B's implementation (routing branch, `CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync`,
`GeneratePreviewResponse.Lifecycle`/`PreviewWeekDto.RunwayBlock`, confirmation containment via the
pre-existing `CatalogPreviewNotPersistableException` guard) is the unmodified baseline. This phase adds
hardening around it; the runway/Core algorithms themselves are untouched.

## 3. Closure gaps addressed

1. `GeneratePreparationRunwayPreviewAsync` (both in `PlanServices` and `CatalogPreviewGenerator`) called
   `RaceHorizonPolicy.Decide` a second time, independently of the routing decision already made in
   `PlanServices.GeneratePreviewAsync`. Fixed by carrying the single decision through.
2. The 60-item HTTP matrix from Phase 4G.6B's own prompt was disclosed as only partially covered
   (recent_race, user_defined target, CAUTION band, LeadingPartialDays 0–6, PreferredDays/LongRunDay
   permutations were untested at the HTTP layer). This phase adds real coverage for each category (not
   full permutation coverage — see §29-equivalent disclosure in §19).

## 4. Artifacts inspected

`PlanServices.cs`, `CatalogPreviewGenerator.cs`, `RaceHorizonPolicy.cs`, `CoreHorizonClassifier.cs`,
`TenKPreparationRunwayDarkOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrationContracts.cs`,
`GlobalExceptionHandler.cs`, `AppExceptions.cs`, `Program.cs`, `appsettings.Development.json`,
`GoalFeasibilityResolver.cs`, `PaceSourceResolver.cs`, `CoreEntryReadinessResolver.cs`,
`CatalogPrescriptionContextBuilder.cs`, existing test files (`TenKPreparationRunwayDarkOrchestratorTests.cs`,
`LongHorizonFailClosedTests.cs`, `Phase4F8_2LivePilotRoutingTests.cs`, `TestPlanServicesFactory.cs`,
`PreparationRunwayPreview15To20WeekEndToEndTests.cs`). The mobile Flutter client was not inspected in this
phase — no `mobile/` changes were required or attempted, and no frontend compatibility claim is made (see
§24).

## 5. Single horizon authority

`PlanServices.GeneratePreviewAsync` already computed `horizonDecision` once via `RaceHorizonPolicy.Decide`
before this phase. That exact value (a `CoreHorizonDecision` record) is now passed as a parameter into
`GeneratePreparationRunwayPreviewAsync` (both the `PlanServices` private method and the
`ICatalogPreviewGenerator` interface method), replacing the second, independently-computed decision that
previously existed inside `CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync`.

`CoreHorizonDecision`, `CoreHorizonMode`, and `CoreHorizonDecisionReason` were widened from `internal` to
`public` (no governance test pinned them `internal`) — required because `ICatalogPreviewGenerator` is a
public interface and cannot expose an internal parameter type (CS0051).

## 6. Updated route/request contract

- `ICatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync(request, horizonDecision, asOfDate, catalogOptions, ct)`
  — new `CoreHorizonDecision horizonDecision` parameter.
- `PlanServices.GeneratePreparationRunwayPreviewAsync(internalUserId, request, horizonDecision, asOfDate, ct)`
  — same addition, private method, called with the already-computed value.
- `TenKPreparationRunwayDarkOrchestrationRequest` gained one new, optional, last-positional field:
  `CoreHorizonDecision? HorizonDecision = null`. Additive and backward-compatible — every existing
  dark-orchestrator-level test/call site (20+ in `TenKPreparationRunwayDarkOrchestratorTests.cs`) leaves it
  null and is unaffected.

This is the smallest contract change that satisfies "carry the authoritative decision from route selection
into preview orchestration" without rewriting the deeply-tested dark orchestrator's own internals.

## 7. Duplicate-classification removal

`CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync` no longer calls `RaceHorizonPolicy.Decide`
at all. It validates the carried decision instead:
- `horizonDecision.MinimumCoreWeeks/PreferredCoreWeeks/MaximumCoreWeeks` must exactly match the resolved
  candidate's own `CoreCycle.MinimumWeeks/DefaultWeeks/MaximumWeeks` (value-consistency check, not a
  recompute) — mismatch throws `PreparationRunwayPreviewNotEnabledException`.
- `horizonDecision.Mode == PreparationRunwayPlusCore` and `AvailableFullWeeks` in `[15,20]` (unchanged
  defensive assertion, now checked against the carried value).

`TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync`'s Stage 1 was extended (not rewritten) with the
same pattern: if `request.HorizonDecision` is supplied, it is used directly (with the same core-cycle-bounds
consistency check, failing `InvalidOrchestrationRequest` on mismatch); if null, the orchestrator falls back
to its original, unchanged self-contained computation — preserving every pre-existing orchestrator-level
test byte-for-byte.

Source governance test `CatalogPreviewGeneratorSource_HasExactlyOneRaceHorizonPolicyDecideCallSite_OutsideRunwayMethod`
proves exactly one `RaceHorizonPolicy.Decide(` call site remains in `CatalogPreviewGenerator.cs`, and that
it belongs to the unrelated 8–14 week dynamic-core path (`BuildDarkInternalDatedSkeleton`), never to the
runway method.

## 8. Horizon-policy invocation-count proof

`PreparationRunwayHorizonAuthorityTests.cs` (new file, 9 tests):
- `Orchestrator_CarriedHorizonDecision_IsUsedDirectly_NotRecomputed` — result's `HorizonDecision` equals
  (record value-equality) the exact carried instance.
- `Orchestrator_MismatchedCarriedHorizonDecision_RejectedBeforeOrchestration` — wrong `MinimumCoreWeeks`
  (7 vs. real candidate's 8) rejected at Stage `Horizon`, code `InvalidOrchestrationRequest`.
- `Orchestrator_NullHorizonDecision_PreservesOriginalSelfComputedBehavior` — the no-decision-carried
  fallback still works.
- `Generator_CarriedFourteenWeekDecision_RejectedByRunwayPreviewMethod` / `..._CarriedTwentyOneWeekDecision_Rejected`
  / `..._MismatchedCoreCycleBounds_Rejected` — all three call the real `CatalogPreviewGenerator` (real
  gate/orchestration, dry-run candidate load) directly with a hand-built inconsistent `CoreHorizonDecision`,
  proving rejection before any orchestration work.
- `Generator_ValidCarriedDecision_Succeeds_WithNoSecondHorizonCall` — a correct carried 18-week decision
  produces a real 18-week preview.
- The source-governance test from §7.

## 9. Activation gate design

New `PreparationRunwayPilotActivationOptions` (`RunningApp.Application.RuntimeCatalog.PreviewRouting`),
mirroring the established `CatalogLivePilotOptions` pattern exactly (same `SectionName` const,
`services.Configure<T>(configuration.GetSection(T.SectionName))` registration in `Program.cs`,
per-environment override via `appsettings.Development.json`). `GateKey` const carries the exact semantic
key `TEN_K_4D_INTERMEDIATE_PREPARATION_RUNWAY_PREVIEW` (previously an observational-only constant on
`CatalogPreviewGenerator`, now the value this options section actually controls at runtime).

`PlanServices` gained one new constructor dependency (`IOptions<PreparationRunwayPilotActivationOptions>`)
and the routing predicate became:
```csharp
if (IsPreparationRunwayPilotScope(request) && availableWeeks is >= 15 and <= 20 && _preparationRunwayPilotActivationEnabled)
```
The gate evaluates enabled/disabled state, exact candidate identity (via the existing
`IsPreparationRunwayPilotScope` predicate, unchanged), and the horizon range — exactly the four dimensions
required. It has zero effect on 8–14 week requests (never reaches this branch) or on 21+/other-candidate
requests (already routed elsewhere by the unchanged predicate).

## 10. Enabled behavior

Unchanged from Phase 4G.6B: exact pilot 15–20 week requests route to
`GeneratePreparationRunwayPreviewAsync`, HTTP 200, combined runway+Core preview.

## 11. Disabled rollback behavior

Chosen and documented (in `PreparationRunwayPilotActivationOptions`'s own doc comment): disabled gate
restores the exact pre-4G.6B `PlanHorizonCompositionRequiredException` (HTTP 422
`PLAN_HORIZON_COMPOSITION_REQUIRED`), **not** the newer `PREPARATION_RUNWAY_PREVIEW_NOT_ENABLED` code. This
minimizes public contract drift on rollback — a caller who only ever saw pre-4G.6B behavior sees the
identical response.

## 12. Rollback tests

`LongHorizonFailClosedTests.cs` gained:
- `DisabledGate_ExactPilotFifteenToTwentyWeeks_RestoresPreActivationUnsupportedBehavior` (15 and 20 weeks)
  — asserts `PlanHorizonCompositionRequiredException`, zero orchestrator/generator invocation (counting
  fake), zero `PlanPreview`/`TrainingPlan`/`TrainingWeek`/`TrainingDay` rows.
- `DisabledGate_EightToFourteenWeeks_Unaffected` — the gate has no effect outside the 15–20 pilot branch.
- `EnabledVsDisabledGate_OtherCandidateIdentity_UnaffectedEitherWay` — a non-pilot identity at 17 weeks is
  unaffected by the gate's state either way.

`TestPlanServicesFactory.Create(...)` gained an optional `preparationRunwayPilotActivationEnabled = true`
parameter for the same purpose in other test files.

## 13. Orchestrator invocation behavior

Unchanged: `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync` is still invoked exactly once per
eligible request, via the same factory call.

## 14. Recent-race HTTP proof

`PilotScope_RecentRaceEvidence_Returns200_EffortOnlyRunwayPacing` — HTTP 200, 18 weeks, `lifecycle` still
`preparation_runway_preview_not_confirmable`, every runway session's `intensity` contains neither
`GOAL_PACE` nor `RACE_SPECIFIC`.

## 15. User-target HTTP proof

Two tests, reflecting the REAL (pre-existing, unrelated to this phase) governance rule that a bare
user-defined target time has no independent evidence and cannot be approved on its own
(`GoalFeasibilityResolver`/PHASE4D_4_1):
- `PilotScope_UserDefinedTargetTime_WithCorroboratingRecentRace_Returns200` — user-defined target
  corroborated by real recent-race evidence succeeds (HTTP 200).
- `PilotScope_UserDefinedTargetTime_NoIndependentEvidence_TypedFailure` — the documented typed failure
  (`RUNTIME_CONDITION_UNSUPPORTED`) for a bare user-defined target, proving this pre-existing rule is real
  and typed, not a silent 500.

(Test-authoring note: the first draft of this test omitted recent-race evidence and incorrectly expected
200; the 422 that surfaced was correct pre-existing system behavior, not a bug — corrected in the test, not
the production code.)

## 16. Unsupported target result

See §15's second test — `RUNTIME_CONDITION_UNSUPPORTED`, HTTP 422.

## 17. CAUTION HTTP proof

`PilotScope_CautionEvidenceBand_Returns200_ConsistencyProfile` — evidence (`RecentWeeklyVolumeKm=14`,
`RecentLongestRunKm=5`) chosen to land in the real `CoreEntryReadinessResolver`'s CAUTION band (neither
READY nor NOT_READY thresholds), not a faked profile. Asserts HTTP 200, final runway block
`PRE_SPECIFIC_TRANSITION`, no Core week ever carries a `runway_block`, and no internal
`OrchestrationTrace`/`FailureCode` token leaks into the response body.

(A first evidence choice, `RecentWeeklyVolumeKm=10`, hit an unrelated pre-existing Core volume-progression
edge case — "Week 12 residual volume cannot support V1 key/easy minimums" — at very low starting weekly
volumes. This is a pre-existing Core-pipeline limitation unrelated to the runway path or this phase; the
test evidence was adjusted rather than treating it as a defect to fix, since it is out of this phase's
scope (`Not allowed: Core algorithm changes`) and would affect the 8–14 week path identically.)

## 18. Missing / NoRecentRunningBase result

`PilotScope_MissingEvidence_StillSucceeds` (pre-existing, from Phase 4G.6B) covers fully-missing recent
evidence. A distinct `NoRecentRunningBase`-specific public fixture was not added separately in this
phase — the public `GenerateRacePlanPreviewRequest` contract has no field distinguishing
"explicitly zero base" from "missing," so this is disclosed as not separately representable at the HTTP
layer without a request-contract change, which is out of scope here.

## 19. LeadingPartialDays 0–6 result

Three representative values tested at the HTTP layer (0, 3, 6), not all seven —
`PilotScope_LeadingPartialDays_EmergeFromDatesOnly_NoPartialWeekOrMisalignment`, each derived purely from
`StartDate`/`RaceDate` arithmetic (never submitted directly), asserting exact week/session counts and that
no session falls inside the leading alignment span. The full 0–6 matrix is already exhaustively covered at
the orchestrator level (`TenKPreparationRunwayDarkOrchestratorTests.LeadingPartialDays_ZeroThroughSix_AreAlignmentOnly`,
pre-existing, unmodified). Disclosed as partial HTTP-layer coverage, not full 7-value HTTP coverage.

## 20. PreferredDays results

`PilotScope_ReorderedPreferredDays_ProducesSameSemanticSchedule` — Mon/Wed/Fri/Sun vs. the same set
submitted in reverse order produce byte-identical responses (excluding `preview_id`); every session lands
on an allowed weekday; no duplicate dates. A second explicit layout (Tue/Thu/Sat/Sun) is exercised via the
Saturday-long-run test (§21). Not every possible 4-of-7 combination was tested — disclosed as
representative, not exhaustive, coverage.

## 21. LongRunDay results

`PilotScope_SaturdayLongRun_AllLongRunsLandOnSaturday` — Tue/Thu/Sat/Sun preferred days with long-run-day
Saturday; every `long_run` session lands on a Saturday. Sunday long-run is already covered by every other
test in this file (default). An invalid long-run-day-not-in-preferred-days case was not added as a new
public HTTP test in this phase — this validation is owned by the pre-existing
`GenerateRacePlanPreviewRequestValidator` (`CatalogLongRunDayNotPreferredException`/equivalent), unrelated
to the runway path specifically, and already exercised by pre-existing validator unit tests outside this
phase's scope.

## 22. Month/year/mid-week results

`PilotScope_MonthAndYearCrossing_Returns200_WithCorrectWeekCount` — `StartDate` 2026-12-16 (mid-week,
crossing into 2027 within the 17-week horizon), HTTP 200, exact 17-week count. Mid-week `StartDate` is
already implicit in every other test in this file (2026-07-20 is a Monday, but the LeadingPartialDays tests
use 2026-08-05, a Wednesday).

## 23. Public DTO equality result

Not implemented as a literal orchestrator-vs-DTO field-by-field comparison test seam in this phase (would
require exposing the internal orchestrator result type to the test project's public assertions, a larger
change than this phase's boundary allows) — equality is instead proven indirectly, exhaustively, per-field
by the existing and new E2E assertions (exact counts, exact block sequencing, exact dates, exact intensity
absence-of-forbidden-tokens). Disclosed as a residual gap against the literal "public exposed value ==
orchestrator final value" test-seam request.

## 24. Frontend compatibility result

Not inspected in this phase. No `mobile/` files were read or changed. This is documented as an **external,
unverified compatibility requirement** — no frontend-readiness claim is made, per the task's own explicit
instruction ("If frontend is not in the repo or not testable: document this as an external compatibility
requirement; do not claim frontend readiness").

## 25. Observability result

No new structured logging was added. `CatalogPreviewGenerator` has no `ILogger` dependency, and adding one
would require touching its documented fragile multi-overload constructor chain (deliberately avoided in
Phase 4G.6B and again here). The orchestrator's own deterministic `Trace` remains the observability
mechanism, preserved end-to-end and never exposed publicly. No new telemetry subsystem was introduced.

## 26. Existing 8–14 regression

Re-proven: `PilotScope_EightToFourteenWeeks_RemainOnExistingCorePath_NotPreparationRunwayLifecycle` (8/12/14,
pre-existing) plus the new `DisabledGate_EightToFourteenWeeks_Unaffected`. Full `Sw02`/`Sw12`/`Sw13` suites
re-run and pass unmodified.

## 27. Other-candidate and 21+ containment

Re-proven: pre-existing `PilotScope_TwentyOneWeeks_StillReturns422_PlanHorizonCompositionRequired` and
`OutOfPilotScope_FifteenToTwentyWeeks_StillReturns422_PlanHorizonCompositionRequired`, plus the new
`EnabledVsDisabledGate_OtherCandidateIdentity_UnaffectedEitherWay`. 52/53+ week and Half-Marathon/5K-at-15-20
cases were not separately added in this phase (52/53+ already covered by the pre-existing orchestrator-level
`OutOfScopeHorizons_FailAtHorizon_WithNoPartialOutput` theory covering 14/21/52/53; a public-HTTP 52-week
test was not additionally added — disclosed as a residual gap, not a claimed full public-layer match to the
task's literal list).

## 28. Test results

New tests this phase:
- `PreparationRunwayHorizonAuthorityTests.cs`: 9 new tests (all pass).
- `LongHorizonFailClosedTests.cs`: 3 new rollback tests (all pass).
- `PreparationRunwayPreview15To20WeekEndToEndTests.cs`: 9 new HTTP tests (recent-race, 2× user-target,
  CAUTION, 3× LeadingPartialDays, reordered-PreferredDays, Saturday-long-run, month/year-crossing — all
  pass; the CAUTION test's evidence values were revised once after discovering an unrelated pre-existing
  Core edge case, and the user-target test was split into two after discovering the correct expected
  behavior).

Full suite results (final, current source):
- `dotnet test RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj -c Release`: **2168 passed,
  0 failed, 0 skipped**.
- `dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj -c Release`: **394 passed, 0
  failed, 0 skipped** (unaffected — no plan-catalog files touched this phase).

## 29. Final Phase 4G.6B closure decision

`TEN_K_PREPARATION_RUNWAY_15_TO_20_WEEK_PUBLIC_PREVIEW_ACTIVATION_FULLY_CLOSED_AND_READY_FOR_SEPARATE_CONFIRMATION_PERSISTENCE_PHASE`

Both mandatory closure gaps (single horizon authority, activation gate + tested rollback) are resolved and
tested. The HTTP coverage matrix is substantially expanded (evidence sources, CAUTION band, calendar
boundaries) but — disclosed honestly, consistent with this session's established convention — not
exhaustive against the original 60-item/40-item literal enumerations (see §18/19/20/21/23/27 for the exact
residual gaps: full 0–6 LeadingPartialDays at the HTTP layer, exhaustive PreferredDays permutations,
invalid-long-run-day HTTP case, literal orchestrator-vs-DTO equality test seam, and 52/53+ at the public
HTTP layer). Confirmation and persistence remain fully blocked, unchanged.

## 30. Exact next phase

`PHASE4G_6C` — if further hardening is wanted: exhaustive LeadingPartialDays (1,2,4,5) and PreferredDays
permutation coverage at the HTTP layer, an invalid-long-run-day-not-preferred HTTP fixture, and a literal
orchestrator-result-vs-public-DTO equality test seam. Otherwise, per this document's own closure decision,
the next substantive phase is a **separate, explicitly-scoped** confirmation/persistence phase for 15–20
week previews (not started, not implied, not silently enabled by anything in this phase).
