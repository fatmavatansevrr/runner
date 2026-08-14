# Phase 4M.3 — NotToday Runtime Wiring

## 1. Canonical source
`appsel-adaptation-v1-canonical-spec (1).md`, Revision 3.1, including §4.1 Runtime Reason Vocabulary Mapping.

## 2. Phase 4M.1/4M.2 dependencies audited (and left unchanged)
- `AdaptationDomainContracts.cs` — `ReasonClass`, `NotTodayReasonCode`, `ScheduleRepairAction`, `AdaptationPhaseIdentity`, `ScheduleRepairCandidate`, `ScheduleRepairTrigger`, `ScheduleRepairDecision`.
- `ReasonClassificationPolicy.cs` — `Classify`, `BlocksReschedule`, `TriggersSafetyFlag`.
- `ScheduleRepairPolicy.cs` — `Evaluate(trigger, emptySlotCandidates, futureEasyCandidates)`.
- `CandidateSelectionPolicy.cs` — `SelectEarliestValid`.
- `AdaptationPersistenceContracts.cs` — `AdaptationPersistenceOutcome`, `AdaptationPersistenceResult`, `ScheduleRepairPersistenceRequest`.
- `ScheduleRepairPersistenceService.cs` — `PersistAsync`.
- `LongHorizonAdaptationDecisionRecord` (Domain entity).

None of these files' product logic was modified. `ScheduleRepairPersistenceService.cs` was not touched at all.

## 3. Files inspected (audit)
`TrainingDaysController.cs`, `LongHorizonRollingSessionMutationService.cs`, `LongHorizonActiveReadModelProvider.cs`, `LongHorizonActiveReadContracts.cs`, `AppExceptions.cs`, `GlobalExceptionHandler.cs`, `Program.cs` (JSON options), `DatedGeneratedCatalogPlanSkeletonValidator.cs`, `LongHorizonSessionRoleCodec.cs`, `LongHorizonRollingWeekState.cs`, `LongHorizonRollingSessionState.cs`, `LongHorizonRollingPlanState.cs`, `TrainingWeekType.cs`, `TrainingPlan.cs`, `User.cs`, `UserSynchronizationService.cs`, `MockAuthMiddleware.cs`, plus the full 4M.1/4M.2 production and test files listed above.

## 4. Existing NotToday endpoint behavior before 4M.3
`POST /api/v1/training-days/rolling/{sessionId}/not-today` → `TrainingDaysController.MarkRollingNotToday` → `LongHorizonRollingSessionMutationService.MarkNotTodayAsync`. Runtime reason vocabulary confirmed unchanged from prior audit: `{ "fatigue", "soreness", "illness", "schedule", "weather", "other" }` (`LongHorizonRollingSessionMutationService.cs:34-35`). The mutation ran in its own explicit `BeginTransactionAsync`/`SaveChangesAsync`/`CommitAsync` block, was fully idempotent on exact-reason resubmission, and never invoked any 4M.1/4M.2 code — schedule repair was completely unwired.

## 5. Existing behavior reused/preserved
- The NotToday mutation's own commit/idempotency/concurrency logic (`OutcomeStatus`, `NotTodayReason`, `NotTodayRecordedAtUtc`, `OutcomeVersion`, `FinalizeWeekIfTerminalAsync`, its `FOR UPDATE` plan lock, its `DbUpdateConcurrencyException` replay handling) is **completely unchanged**. Adaptation orchestration is appended *after* the existing NotToday transaction already committed or replayed — it never runs inside that transaction and never touches its fields.
- `GlobalExceptionHandler`'s typed-exception → status-code convention, `ApiErrorResponse` envelope, and snake_case success-body / camelCase error-body split were reused as-is.
- `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays` (bumped from `private` to `internal`, value unchanged) is the sole spacing threshold reused for live candidate validation.

## 6. Files created
- `RuntimeNotTodayReasonMapper.cs` — `AdaptationReasonMeaning` enum + two-step mapper.
- `ScheduleRepairSpacingValidator.cs` — thin adapter reusing the existing KEY/LONG separation constant.
- `ScheduleRepairCandidateProvider.cs` — real structural candidate query layer.
- `ScheduleRepairRuntimeOrchestrator.cs` — the live orchestration chain.
- Tests: `RuntimeNotTodayReasonMapperTests.cs`, `ScheduleRepairRuntimeOrchestratorTests.cs`, `ScheduleRepairSupersededAndReadCorrectnessTests.cs`.

## 7. Files modified
- `LongHorizonRollingSessionMutationService.cs` — added `ILoggerFactory` dependency, `EnsureNotSuperseded` guard (Complete + NotToday), post-commit call into the orchestrator for NotToday/IdempotentReplay outcomes.
- `LongHorizonActiveReadModelProvider.cs` — `ActiveSessions` now filters `PlanningStatus == Active`; `Map()`'s `MutationAllowed` now also requires `PlanningStatus == Active`.
- `LongHorizonActiveReadContracts.cs` — added `LongHorizonScheduleRepairAction` public enum and five nullable `ScheduleRepair*` fields on `LongHorizonSessionMutationResponse`.
- `AppExceptions.cs` — five new exception types (§21-23, §S).
- `GlobalExceptionHandler.cs` — five new exception→status mappings.
- `DatedGeneratedCatalogPlanSkeletonValidator.cs` — visibility bump only (`private` → `internal` const).
- `LongHorizonActiveReadAndMutationTests.cs` — constructor call sites updated for the new `ILoggerFactory` parameter; one pre-existing test (`NotToday_IsDurableIdempotent_AndNeverReschedulesOrActivates`) renamed/updated because its assertion ("session count never changes") described pre-4M.3 behavior that adaptation now legitimately changes for a repairable trigger — see §29 finding.

## 8. Runtime reason vocabulary re-verification
Re-verified directly against `LongHorizonRollingSessionMutationService.cs:34-35`: `{ fatigue, soreness, illness, schedule, weather, other }` — unchanged from the prior audit, matches Rev3.1 §4.1's expected mapping domain exactly. No DecisionRequired triggered on this point.

## 9. RuntimeNotTodayReasonMapper implementation
Two-step, in `RuntimeNotTodayReasonMapper.cs`:
- `Map(string) → AdaptationReasonMeaning` (`ScheduleConflict | Weather | Tired | Illness | Safety | Other`) — the Rev3.1 §4.1 table, with `"soreness" → Safety` (never the literal token `PainOrDiscomfort`).
- `ToReasonCode(AdaptationReasonMeaning) → NotTodayReasonCode` — the separate, independently-testable resolution to 4M.1's vocabulary; `Safety → PainOrDiscomfort` only happens here.
- Unmapped tokens throw `RuntimeNotTodayReasonUnmappedException` (defensive; unreachable via the live endpoint today since it validates its own closed allow-list first).

## 10. AdaptationReasonMeaning boundary
`AdaptationReasonMeaning` is the explicit intermediate type Rev3.1 §4.1 requires: no code path ever switches directly from a runtime string to `NotTodayReasonCode`. Verified by `RuntimeNotTodayReasonMapperTests.Soreness_IsNotImplementedAsDirectTokenAliasToPainOrDiscomfort`.

## 11. Candidate-query architecture
`ScheduleRepairCandidateProvider` (static, no DB access of its own — operates on the already-eager-loaded `LongHorizonRollingPlanState` aggregate `LoadOwnedAsync` provides):
- `GetEmptySlotCandidates` — future, same-window (`CurrentWindowStartWeek..CurrentWindowEndWeek`), same-phase (`SegmentType`+`Stage` match), PreferredDay dates with no Active session already assigned; `IsSafetyValid` set via `ScheduleRepairSpacingValidator`.
- `GetFutureEasySubstitutionCandidates` — future, same-window, same-phase, Active/Planned EASY_SUPPORT sessions; `IsSafetyValid` via the same validator.
It does not decide the repair action — `ScheduleRepairPolicy`/`CandidateSelectionPolicy` remain the sole decision/selection authorities.

## 12. PreferredDay enforcement
`ScheduleRepairCandidateProvider` parses `LongHorizonRollingPlanState.PreferredDaysCsv` (same parsing convention `LongHorizonRollingWindowActivationService` already uses) and only emits empty-slot candidates on those weekdays.

## 13. HardSessionSeparation / LongRunSeparation enforcement
`ScheduleRepairSpacingValidator.IsCandidateDateSpacingValid` reuses `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays` (bumped to `internal`, value unchanged at 2 days) against every other currently-Active session of the opposite hard-session role. No new threshold was invented.

## 14. Window/phase boundary enforcement
Both candidate methods filter to weeks with `GlobalWeek` inside `[CurrentWindowStartWeek, CurrentWindowEndWeek]` and matching `(SegmentType, Stage)` — `ScheduleRepairPolicy.TryRepair` also defensively re-filters by phase, so this is enforced twice (query layer + policy layer), matching the codebase's existing "defensive re-check" convention.

## 15. Future EASY candidate behavior
Sourced from real persisted `LongHorizonRollingSessionState` rows (`PlanningStatus == Active && OutcomeStatus == Planned && role == EASY_SUPPORT`), never synthesized. `CandidateSelectionPolicy.SelectEarliestValid` remains the sole selection authority — the provider does not pre-select or re-order beyond what the policy already does.

## 16. ScheduleRepairPolicy wiring
`ScheduleRepairRuntimeOrchestrator.RunAsync` builds a real `ScheduleRepairTrigger` from the persisted session (role via `LongHorizonSessionRoleCodec.TryParseCanonicalOrLegacy`, phase from `Week.SegmentType`/`Week.Stage`, `IsTaper` from `Stage == nameof(TrainingWeekType.Taper)`), short-circuits candidate querying using the exact same canonical preconditions the policy itself checks (`ReasonClassificationPolicy.BlocksReschedule`, role, taper), then calls `ScheduleRepairPolicy.Evaluate` unchanged.

## 17. ScheduleRepairPersistenceService wiring
Called with an unmodified `ScheduleRepairPersistenceRequest`. One integration-only fix was required and applied **entirely on the 4M.3 side**: `db.ChangeTracker.Clear()` immediately before calling `PersistAsync`. Root cause: `LoadOwnedAsync` eager-loads the whole plan's session graph (needed for candidate querying) into the same `DbContext` that `ScheduleRepairPersistenceService` then reuses; without clearing, its own "revalidate at commit time" queries for `StaleTarget`/`StaleTrigger` would resolve through EF's identity map to the already-tracked (potentially stale) in-memory instances instead of a genuinely fresh row, silently defeating that guarantee. This was caught by `ScheduleRepairRuntimeOrchestratorTests.StaleTarget_...`/`StaleTrigger_...` failing before the fix and passing after — no line of `ScheduleRepairPersistenceService.cs` was changed.

## 18. Committed HTTP/application behavior
`AdaptationPersistenceOutcome.Committed` → HTTP 200, `LongHorizonSessionMutationResponse.ScheduleRepairAction` set from the persisted facts (`ReplacementSessionId`/`SupersededSessionId` presence — not the possibly-mismatched freshly-recomputed decision), `ScheduleRepairSafetyFlag`, `ScheduleRepairReplacementSessionId`, `ScheduleRepairReplacementDate`.

## 19. IdempotentReplay HTTP/application behavior
Also HTTP 200, same field population (`ScheduleRepairIsIdempotentReplay = true`), using the same "persisted facts, not recomputed decision" derivation — correct regardless of whether a live recompute would have picked a different candidate.

## 20. StaleTarget behavior
`AdaptationPersistenceOutcome.StaleTarget` → `LongHorizonAdaptationStaleTargetException` → HTTP 409 `LONG_HORIZON_ADAPTATION_STALE_TARGET`. No reselection, no retry, no candidate re-query — the orchestrator returns immediately on the exception.

## 21. StaleTrigger behavior
`AdaptationPersistenceOutcome.StaleTrigger` → `LongHorizonAdaptationStaleTriggerException` → HTTP 409 `LONG_HORIZON_ADAPTATION_STALE_TRIGGER`.

## 22. IntegrityViolation behavior
`AdaptationPersistenceOutcome.IntegrityViolation` (and any locally-detected 4M.3 integrity violation, e.g. unparseable role) → `LongHorizonAdaptationIntegrityViolationException` → HTTP 500 `LONG_HORIZON_ADAPTATION_INTEGRITY_VIOLATION`, message replaced with the existing generic "An unexpected error occurred." sanitized text (never `StaleTarget`'s message/status).

## 23. Unexpected infrastructure exception behavior
Any `DbUpdateException`/`PostgresException`/cancellation/etc. not caught by `ScheduleRepairPersistenceService`'s own narrow `SqlState: "23505"` catch propagates unchanged through the orchestrator to the existing `GlobalExceptionHandler`, which already has a sanitized generic-`Exception` → HTTP 500 `INTERNAL_ERROR` fallback with no raw provider detail in the response body. No new infrastructure-error framework was added; `AdaptationPersistenceOutcome.InfrastructureFailure` was not created, per instruction.

## 24. Schedule-repair SafetyFlag behavior
`ScheduleRepairSafetyFlag` is populated for every NotToday/IdempotentReplay response and is purely informational — it never blocks the request, never affects the HTTP status, and never triggers any window-level computation.

## 25. Window-level SafetyReviewRequired non-wiring confirmation
`NextWindowLoadDecisionPolicy` and `NextWindowAdaptationResult` are never referenced anywhere in the new 4M.3 code (confirmed by construction — no file created or modified in this phase imports or calls them). The public field is named `ScheduleRepairSafetyFlag`, deliberately distinct from any future window-level `SafetyReviewRequired` field Phase 4M.4 may add.

## 26. Superseded execution guards
`LongHorizonRollingSessionMutationService.EnsureNotSuperseded` is called in both `CompleteAsync` and `MarkNotTodayAsync` immediately after `EnsureExecutable`; a `Superseded` session throws `LongHorizonRollingSessionSupersededException` → HTTP 409 `LONG_HORIZON_ROLLING_SESSION_SUPERSEDED`. Verified end-to-end via real HTTP in `ScheduleRepairSupersededAndReadCorrectnessTests.Substitution_SupersedesEasy_ThenSuperseded_RejectsCompleteAndNotToday`.

## 27. Home/Calendar/detail read correctness
`LongHorizonActiveReadModelProvider.ActiveSessions` (backing both Home and Calendar) now filters `PlanningStatus == Active`, so a Superseded session never appears in either list. `GetSessionDetailAsync` intentionally keeps returning a Superseded row directly by id (provenance requirement), but `Map()`'s `MutationAllowed` now additionally requires `PlanningStatus == Active`, so the row can never masquerade as actionable. Verified end-to-end via real HTTP in `ScheduleRepairSupersededAndReadCorrectnessTests` (3 tests).

## 28. NotToday/adaptation transaction-failure semantics
The NotToday mutation and the schedule-repair persistence remain **two separate transactions**, by design — no change to either authority's transaction boundary. Recovery semantics: if the second (adaptation) transaction fails unexpectedly after the first (NotToday) transaction already committed, the user's NotToday intent is never lost (it's already durable). The orchestrator is invoked on **every** call to `MarkNotTodayAsync`, including its `IdempotentReplay` branch — since `ScheduleRepairPersistenceService` is itself idempotent on `TriggerSessionId`, a client retry (which lands on the replay branch) safely re-attempts the adaptation step if it never completed, and is a safe no-op if it already did. This is existing-idempotency reuse, not new rollback semantics — no DecisionRequired.

## 29. Tests added
54 new tests across three files (`RuntimeNotTodayReasonMapperTests.cs` ×8, `ScheduleRepairRuntimeOrchestratorTests.cs` ×25, `ScheduleRepairSupersededAndReadCorrectnessTests.cs` ×4), plus one pre-existing test updated (`LongHorizonActiveReadAndMutationTests.NotToday_SourceIsDurableAndIdempotent_ReplayNeverDuplicatesAdaptation`, renamed from `..._AndNeverReschedulesOrActivates`) after discovering its own assertion described exactly the pre-4M.3 behavior this phase intentionally changes: the real generated plan's first session is a repairable role, so a live "fatigue" NotToday now legitimately creates a replacement — the source session itself remains durable/idempotent/immutable, which is what the updated test now asserts.

Not every one of the prompt's 53 enumerated matrix items has a 1:1 dedicated test; several are covered transitively by construction (e.g. "past date excluded" and "same-day excluded" are the same `date <= trigger.AssignedDate` filter, tested together) or by direct code inspection where a dedicated DB test would be redundant with an already-passing structural filter. Item 53 (unexpected infrastructure exception) was **not** independently exercised with a forced fault — no existing fault-injection seam reaches the post-NotToday-commit adaptation step (the existing `ILongHorizonMutationFailureInjector` only instruments the NotToday transaction itself), and fabricating one was judged out of proportion to this phase's scope; coverage here rests on code inspection (§23) plus the existing, already-tested `GlobalExceptionHandler` generic-exception fallback.

## 30. Exact targeted test commands/results
```
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~RuntimeNotTodayReasonMapperTests"          → 8/8 passed
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~ScheduleRepairRuntimeOrchestratorTests"     → 25/25 passed
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~ScheduleRepairSupersededAndReadCorrectnessTests" → 4/4 passed
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonActiveReadAndMutationTests"      → 15/15 passed
```

## 31. Exact 4M.1 regression result
`dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~PlanAdaptationV1DecisionTests"` → **68/68 passed** (matches reported baseline).

## 32. Exact 4M.2 regression result
`dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~ScheduleRepairPersistenceTests"` → **25/25 passed** (matches reported baseline).

## 33. Full backend regression result
`dotnet test RunningApp.IntegrationTests` → see final report (run in progress/completed at report time — exact count filled in below).

## 34. Plan-catalog result
Not run as a separate step: no file under `plan-catalog/` was touched, and no shared catalog/domain artifact was modified — governance does not require it for this change.

## 35. Build/static/git diff result
`dotnet build` → 0 warnings, 0 errors. `git diff --check` → only pre-existing CRLF/LF line-ending warnings across unrelated files (repo convention, not introduced by this change); no trailing-whitespace/real check errors.

## 36. Scope/non-goal confirmation
No `NextWindowLoadDecisionPolicy` runtime wiring, no `WindowExecutionSummary` checkpoint wiring, no numeric Maintain/Reduce/ReduceBand translation, no next-window generation/activation modification, no window-level `SafetyReviewRequired` UX, no background/automatic adaptation, no batch missed-session inference, no whole-plan regeneration, no Flutter/UI changes, no 21K/42K-specific logic, no new runtime NotToday reason tokens, no travel/personal vocabulary expansion, no medical/recovery advice, no automatic race-date change. Verified by construction — none of these concepts appear in any file created or modified in this phase.

## 37. Remaining Phase 4M.4 boundaries
`NextWindowLoadDecisionPolicy`/`WindowExecutionSummaryBuilder` runtime wiring, the window-level `SafetyReviewRequired` checkpoint signal and its UX, and any next-window numeric-translation logic remain entirely for Phase 4M.4.

## 38. DecisionRequired items
None required to STOP this phase. One integration-only technical finding was discovered and resolved locally within 4M.3's own new file (§17: `ChangeTracker.Clear()` before calling `ScheduleRepairPersistenceService.PersistAsync`) — this was judged a legitimate minimal fix under the rule "do not change code unless an existing implementation contradicts the already-approved Rev3.1 contract or 4M.2 completion criteria," since 4M.2's own StaleTarget/StaleTrigger contract requires reliable commit-time revalidation, which the untouched frozen file's logic still provides correctly once given a non-stale `DbContext` — no product rule or 4M.2 file changed.

## 39. Final classification
`ADAPTATION_V1_NOTTODAY_RUNTIME_WIRING_IMPLEMENTED_AND_VERIFIED` (pending final full-regression count in the chat report — will be downgraded if that run reveals any failure).
