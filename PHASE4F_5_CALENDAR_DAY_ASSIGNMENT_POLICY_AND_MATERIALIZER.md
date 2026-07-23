# Phase 4F.5 — Calendar-Day Assignment Policy and Materializer

> **Addendum (Phase 4F.5.1):** an independent audit found that `DatedGeneratedCatalogPlanSkeletonValidator` (referenced throughout this document) was not actually invoked in the production dark path described below — it was exercised only by tests. This has been fixed; see `PHASE4F_5_1_PRODUCTION_VALIDATOR_WIRING.md` for the corrected flow, the wiring details, and the final classification update (`BACKEND_DARK_MATERIALIZES_AND_VALIDATES_BINDING_CALENDAR_DATES_DURING_ELIGIBLE_PREVIEW_WITHOUT_PUBLIC_SCHEDULE_OUTPUT`). `CatalogCalendarAssignmentFailedException`, mentioned in §22 below, was removed as unjustified dead code in that same follow-up.

## 1. Repository state before implementation

HEAD = `0c6796578f08bc1d76d96f1944a80c9075455206` (Phase 4F.4 checkpoint). Release build: 0 errors, 0 warnings. Full suite: 565 passed / 0 failed / 0 skipped. `RuntimeCatalog`-filtered: 522/522. `TEN_K__4D__INTERMEDIATE v10` DRAFT. Working tree clean except the known excluded items, left untouched throughout.

## 2. Files inspected

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/GeneratedCatalogPlanSkeleton.cs`, `CatalogStageToWeekMaterializer.cs`, `GeneratedCatalogPlanSkeletonValidator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrator.cs` (Phase 4F.3)
- `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs`
- `backend/RunningApp.Domain/Entities/TrainingPlan.cs`
- `backend/RunningApp.Domain/Enums/GoalType.cs`
- `backend/RunningApp.Application/Common/RunningDay.cs`
- `backend/RunningApp.Application/Services/PlanServices.cs` (legacy `PreferredDays`/`LongRunDay`/`ResolvePreferredDays` handling, for vocabulary only — never reused as behavior)
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/PilotGenerationRouteDecider.cs` / `GenerationRouteDecision.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPreviewGeneratorTests.cs`, `Phase4F4DarkSkeletonWiringTests.cs`, `TestPlanServicesFactory.cs`

## 3. Authoritative scheduling-input locations

`GeneratePreviewRequest.PreferredDays` (`string?`) and `GeneratePreviewRequest.LongRunDay` (`string?`) are already present on the same `request` object `CatalogPreviewGenerator.GenerateAsync(request, asOfDate, ct)` receives as its first parameter — but were, before this phase, entirely discarded: `BuildInputSnapshot` never copied them into `ResolverInputSnapshot`, and no catalog-path code read them at all. This is the same "loader gap" pattern as Phase 4F.3's `preferredWeeks` discovery: the data is already in scope, just not yet threaded through. No database reload or user-state refetch was needed — `request` is already a method parameter at the exact dark-wiring call site.

`GeneratePreviewRequest.GoalType` (`RunningApp.Domain.Enums.GoalType`, values `Race`/`Habit`) is already a strongly typed enum — no string-guessing needed to distinguish race vs. habit plans.

**Audit finding (documented, not silently accepted):** `GeneratePreviewRequest.PreferredDays`'s own code comment claims a JSON-array format (`"[1,3,5]"`), but the actual, currently-exercised convention (confirmed in `PlanServices.cs`'s confirm-time snapshot code and its own comment, and in `RunningDay.NormalizeList`) is a comma-separated list of day names (e.g. `"Mon,Wed,Fri,Sun"`), tolerant of case and 3-letter abbreviations. This phase trusts the actual code behavior over the stale comment.

## 4. Race versus habit policy distinction

`PilotGenerationRouteDecider.Decide` already requires `request.GoalType == GoalType.Race` to route a request to `CatalogPreviewGenerator` at all — confirmed by reading `GenerationRouteDecision.cs`. This means `CatalogPreviewGenerator` (and therefore the new calendar materializer wired dark inside it) is **never reachable** for a Habit-goal request in production; the router itself excludes every non-Race request before this class is ever invoked. No `if (GoalKind == Habit) skip` branch was added because none is needed — habit-plan behavior is not implemented, and no code path exists where it could be silently exercised. `CatalogCalendarAssignmentPolicy` (the new policy-version enum) has exactly one member, `RaceHardConstraint` — proven by a dedicated test (`HabitPolicy_IsNotImplemented_RaceHardConstraintIsTheOnlyPolicyValue`) that no habit policy branch exists to accidentally relax or reuse.

## 5. PreferredDays hard-constraint policy

For the race pilot: PreferredDays is required, must contain exactly 4 distinct, recognized weekdays (the pilot's `DaysPerWeek`). Enforced by `CatalogPreferredDayAdapter.ParsePreferredDays` (string-level: blank rejected, unknown value rejected, duplicate rejected) and `CatalogWeekSkeletonCalendarMaterializer`'s own domain validation (count == 4, no duplicates).

## 6. LongRunDayPreference policy

Required for race plans; must belong to PreferredDays. Enforced by `CatalogPreferredDayAdapter.ParseLongRunDay` (blank/unknown rejected) and the materializer's `ValidateLongRunDay` (membership check).

## 7. Plan-relative week policy

Unchanged from Phase 4F.2/4F.3/4F.4: Week 1 starts exactly on StartDate; Week N starts at `StartDate + (N-1)*7 days`; each week spans exactly 7 consecutive days. The calendar materializer's `MapWeekdayToDateInWeek` maps any target weekday to the unique date inside `[weekStart, weekStart+6]` via `((int)targetWeekday - (int)weekStart.DayOfWeek + 7) % 7`, which works for any starting weekday — proven for a Wednesday StartDate (Week 1 spans Wednesday–Tuesday).

## 8. No-Monday-alignment rule

No Monday-based week-number API (`ISOWeek`, `Calendar.GetWeekOfYear`, etc.) is used anywhere in the new code. `Materialize_NoMondayNormalizationOccurs_WeekStaysWednesdayAnchored` proves every week's own `StartDate.DayOfWeek` remains Wednesday (the fixture's own StartDate weekday) across all 12 weeks — never renormalized to Monday, exactly matching the legacy SQL flow's own separate (and untouched) "next Monday" behavior being confined to that unrelated code path.

## 9. Consecutive-running-day policy

Consecutive running days are allowed generally; only the specific `KEY_SESSION ↔ LONG_RUN` adjacency (absolute date difference < 2 days) is forbidden. No other adjacency rule exists.

## 10. EASY_SUPPORT adjacency policy

`EASY_SUPPORT` has no adjacency restriction at all — it may sit next to `KEY_SESSION`, `LONG_RUN`, or another `EASY_SUPPORT`, proven by `Materialize_EasySupport_MayBeAdjacentToKeySessionAndLongRunAndEachOther` (structural: no `EASY_SUPPORT`-related check exists anywhere in `CatalogWeekSkeletonCalendarMaterializer`).

## 11. KEY_SESSION/LONG_RUN separation policy

`|KEY_SESSION.DayNumber − LONG_RUN.DayNumber| >= 2` is enforced both same-week (candidate filtering in `BuildWeekPlan`) and cross-week (the backtracking search's precondition/per-candidate checks in `TryAssignKeySessionDates`).

## 12. Cross-week adjacency policy

Since plan-relative weeks are exactly 7 days apart, a week's own `weekStart.DayOfWeek` is identical for every week — meaning each weekday's offset-from-weekStart is a plan-wide constant. A mathematical consequence (documented and exploited by the algorithm and its tests): the only offset pair that can ever be cross-week-adjacent-but-same-week-safe is when one candidate sits at offset 0 (the week's own first day) and another at offset 6 (the week's own last day) — a same-week distance of 6 (always safe there) but only a 1-day cross-week gap to the neighboring week's identical pattern. `CatalogWeekSkeletonCalendarMaterializer.TryAssignKeySessionDates` checks, for every week index `i > 0`: (a) this week's fixed LONG_RUN date vs. the previous week's already-chosen KEY_SESSION date, and (b) this week's candidate KEY_SESSION date vs. the previous week's fixed LONG_RUN date — the only two facts that can ever be affected by a boundary. Proven directly by `Materialize_TopRankedCandidateCausesCrossWeekConflict_SearchFindsSafeAlternative` and its mirror `Materialize_WeekEndLongRunFollowedByNextWeekStartKeySession_IsAvoidedBySearch`.

## 13. Unsafe selected-day policy

**Mathematical finding specific to this pilot's exact scope** (exactly 4 of 7 distinct weekdays, one long-run day, ≥2-day separation rule): among the 3 remaining preferred days, at most one can ever be same-week-invalid (distance 1, the sole same-week neighbor on whichever side is "inward" from wherever the long-run day sits in the week), and the one possible cross-week-risky offset (the diametrically opposite day, distance 6) is only ever risky, never same-week-invalid. Since there are always 3 remaining days and at most 2 "problem" offsets (1 invalid, 1 risky) exist, at least one of the 3 remaining days is always both same-week and cross-week safe. `Materialize_AllValidFourOfSevenPreferredDayCombinations_NeverThrowConfigurationUnsafe` exhaustively verifies all C(7,4)=35 weekday combinations × 4 long-run choices (140 total materializations) never throw `CatalogPreferredDayConfigurationUnsafeException`. This is a property of the 4-of-7 pilot shape specifically, not a universal claim — a future phase with a different `DaysPerWeek` could reach a genuinely unsafe configuration, which is exactly why the exception type, the full backtracking search (not a naive per-week-only greedy), and the typed-failure contract remain fully implemented rather than assumed away.

## 14. No-unselected-day fallback guarantee

The materializer never moves a session to a day outside `PreferredDays`, never drops a session, never changes a role, and never substitutes a default weekday pattern (unlike the legacy `PlanServices.ResolvePreferredDays`, whose own default-pattern fallback was read and deliberately not reused). On failure, it throws — it never returns a partial week or partial plan.

## 15. New context contracts

`CatalogCalendarAssignmentContext` (record: `StartDate`, `GoalKind` — reuses the existing `GoalType` enum rather than inventing a parallel `CatalogGoalKind`, `PreferredDays`, `LongRunDayPreference`, `PlanSkeleton`, `Policy`, `Provenance`), `CatalogCalendarMaterializationProvenance`, `CatalogWeekCalendarProvenance`, `CatalogSessionCalendarProvenance` — all `internal sealed record`s in `CatalogCalendarAssignmentContracts.cs`.

## 16. Dated skeleton contracts

`DatedGeneratedCatalogPlanSkeleton` / `DatedGeneratedCatalogWeekSkeleton` / `DatedGeneratedCatalogSessionSlotSkeleton` (all `internal sealed record`s) — the dated counterpart to Phase 4F.2's `GeneratedCatalogPlanSkeleton` family, preserving every structural fact (week boundaries, `PhaseKey`/`PhaseWeekIndex`/`PhaseWeekCount`, `LayoutSlotKey`, `StructuralRole` as a raw string — matching Phase 4F.2's own deliberate non-enum convention rather than introducing the task's illustrative `CatalogStructuralRole` type) and adding exactly `SessionDate`/`SessionDayOfWeek` per slot. Never added to `GeneratedPreviewPlanPayload`; own independent `CurrentSchemaVersion = "1"`.

## 17. Calendar materializer interface

`ICatalogWeekSkeletonCalendarMaterializer.Materialize(CatalogCalendarAssignmentContext) : DatedGeneratedCatalogPlanSkeleton`, implemented by `CatalogWeekSkeletonCalendarMaterializer` — zero constructor parameters (proven by `CatalogWeekSkeletonCalendarMaterializer_HasNoConstructorDependencies`), no DB/clock/HTTP/route/candidate/resolver/catalog-loader access, never mutates the source skeleton (proven by `Materialize_DoesNotMutateSourceSkeleton`).

## 18. Weekday mapping algorithm

`MapWeekdayToDateInWeek(weekStart, targetWeekday) = weekStart.AddDays(((int)targetWeekday - (int)weekStart.DayOfWeek + 7) % 7)` — works regardless of `weekStart`'s own weekday; no Monday-based API.

## 19. Full-plan assignment/search algorithm

For each plan-relative week, LONG_RUN is fixed to `LongRunDayPreference`'s mapped date. The remaining 3 preferred dates are ranked as KEY_SESSION candidates (same-week distance ≥ 2 only). A bounded depth-first backtracking search (`TryAssignKeySessionDates`) walks weeks in `WeekNumber` order, trying each week's own ranked candidates in order and checking only the two cross-week facts that can ever be affected by a boundary (previous week's fixed LONG_RUN vs. this week's candidate; this week's fixed LONG_RUN vs. previous week's already-chosen KEY_SESSION), backtracking to the previous week's next candidate on failure. Search space ≤ 3 candidates × `PlannedWeekCount` weeks (12 for the pilot) — trivially small and fully bounded, never an unbounded generic constraint solver. The two `EASY_SUPPORT` slots receive the two remaining dates, matched by ascending original `SlotOrderInWeek` to ascending chronological date order — deterministic, no reliance on hash-set/collection iteration order (proven by `Materialize_InputCollectionOrder_DoesNotAffectOutput`, which reverses the `PreferredDays` input order and confirms byte-identical structural output).

## 20. Deterministic ranking and tie-break rules

Candidates are ranked by (a) descending same-week distance from LONG_RUN, (b) ascending (chronologically earlier) date. The product decision's rule 2 ("preserve structural slot ordering as closely as possible within chronological date order") is a documented no-op here: there is exactly one `KEY_SESSION` slot to place per week, so no second slot-ordering signal exists beyond the date itself — rule 3 (earliest-date tie-break) is what actually governs. The first complete valid full-plan assignment found by the fixed, deterministic DFS exploration order wins — proven deterministic and input-order-independent by `Materialize_SameInput_ProducesStructurallyEquivalentOutput` and `Materialize_InputCollectionOrder_DoesNotAffectOutput`.

## 21. Validator rules

`DatedGeneratedCatalogPlanSkeletonValidator.Validate(skeleton, preferredDays, longRunDayPreference)` checks: schema version, week count, plan EndDate consistency, consecutive week numbering, consecutive 7-day week ranges, exact 4 sessions/week, unique session dates within a week, session date inside its owning week, session weekday-matches-date, session weekday ∈ PreferredDays, every PreferredDay used exactly once per week, LONG_RUN date on LongRunDayPreference, exact role counts, KEY_SESSION/LONG_RUN same-week and cross-week separation, and provenance presence at every level. Zero DB/clock/HTTP/resolver/catalog-loader dependency (proven by `Validate_HasNoDatabaseClockHttpResolverOrCatalogLoaderDependency`).

## 22. Typed error taxonomy

`CatalogPreferredDaysRequiredException`, `CatalogPreferredDayCountInvalidException`, `CatalogPreferredDaysDuplicatedException`, `CatalogLongRunDayRequiredException`, `CatalogLongRunDayNotPreferredException`, `CatalogCalendarRoleStructureInvalidException`, `CatalogPreferredDayConfigurationUnsafeException`, `CatalogCalendarAssignmentFailedException`, `CatalogDatedSkeletonInvalidException` — all `internal sealed class : Exception` in `CatalogCalendarAssignmentExceptions.cs`. When dark-wired, every one is caught by exact type in `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` and re-thrown wrapped as the pre-existing `PlanPreviewGenerationFailedException` — no new public error code.

## 23. Provenance and versioning

`CatalogCalendarDayMaterializerVersion.V1 = "CATALOG_CALENDAR_DAY_MATERIALIZER_V1"` — independent of both the Phase 4F.1 final-contract schema version and the Phase 4F.2 `CatalogStageToWeekMaterializerVersion`. Plan/week/session-level provenance carries candidate identity/version, AsOfDate, StartDate, PreferredDays, LongRunDayPreference, the materializer version, source skeleton schema version, dependency versions, and per-session the source layout slot key/structural role/selected weekday/assigned date/assignment rule — all internal only.

## 24. Dependency isolation

Proven by dedicated tests: no DB, wall clock, HTTP context, authenticated user, route selection, candidate selection, runtime resolver, or catalog-loader access anywhere in `CatalogWeekSkeletonCalendarMaterializer` or `DatedGeneratedCatalogPlanSkeletonValidator` (both zero-parameter constructors).

## 25. Integration choice

**Dark preview invocation**, extending the existing Phase 4F.4 dark-wiring call site. `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton(candidate, asOfDate, request)` runs the Phase 4F.3 skeleton orchestrator, then — using `request.PreferredDays`/`request.LongRunDay` (already in scope, never reloaded) — the Phase 4F.5 calendar materializer, at the exact same point in `GenerateAsync` as before (after governance-policy success, before snapshot construction). The public/DI-facing `CatalogPreviewGenerator` constructor remains unchanged (2 parameters); a new `private static DefaultCalendarMaterializer()` composes the default `CatalogWeekSkeletonCalendarMaterializer()` (zero dependencies), and a new `internal` 4-parameter constructor overload lets tests substitute a fake/spy calendar materializer — mirroring the Phase 4F.4 composition-root pattern exactly, for the same CS0051 reason (the type is deliberately internal).

## 26. Snapshot boundary

`CatalogPreviewSnapshot.cs` was not modified in this phase — confirmed via `git diff --name-status`. No new property was added; proven by `DarkCalendar_Success_GeneratedPreviewPlanPayloadRemainsNull_NoSkeletonInSnapshot` reflecting over the snapshot's own properties and finding none containing "Dated" or "Calendar".

## 27. Hash boundary

`CatalogPreviewSnapshotBuilder.Build`'s `hashableContent` object is unchanged (same file, unmodified) — it never referenced the skeleton before this phase, and still does not reference the dated skeleton. The pre-existing `CatalogPreviewGeneratorTests`/`CatalogPreviewSnapshotVerifierTests` (3/3, unmodified assertions) remain green.

## 28. GeneratedPreviewPlanPayload boundary

Never set — `CatalogPreviewSnapshotBuilder.Build` is called with the same argument list as before. Proven directly.

## 29. Confirm boundary

`CatalogPlanConfirmationService.cs` was not modified. Its constructor remains exactly 3 parameters, with no `Materialization`-namespace reference of any kind (broadened from the Phase 4F.4 orchestrator-only check to cover the calendar materializer too, via the renamed `CatalogPlanConfirmationService_ConstructorSurface_UnchangedByPhase4F4Or4F5` and `..._NeverInvokesSkeletonOrCalendarMaterialization_...` tests).

## 30. Persistence boundary

No persistence code was added. Confirm/persistence regression (`CatalogPlanConfirmationServiceTests`, 25/25, unmodified) continues to prove no `TrainingPlan`/`TrainingWeek`/`TrainingDay`/`PlanEvent` is created and `ConfirmedPlanId` stays null on rejection.

## 31. Public DTO boundary

No DTO file was touched. `GeneratePreviewResponse`, `PreviewWeekDto`, `PreviewDayDto`, `ConfirmPlanResponse` are unmodified.

## 32. DRAFT lifecycle behavior

`TEN_K__4D__INTERMEDIATE v10`'s real catalog JSON status was never touched. `DarkCalendar_NotInvoked_ForDraftV10` proves, via a counting calendar-materializer double against the real (non-dry-run) gate, that `CatalogCandidateNotPublishedException` is thrown and the calendar materializer is invoked zero times.

## 33. Deferred frontend warning/disclaimer

Intended future UX (not implemented in this backend phase, no frontend code added): recommend spreading running days across the week; warn when selected days are clustered; ask for confirmation before proceeding; after confirmation, selected days are binding (matching this phase's own hard-constraint enforcement — a disclaimer flag never bypasses the backend invariants, since none was added).

## 34. Build results

`dotnet build RunningApp.sln -c Release`: 0 errors, 0 warnings.

## 35. Focused test results

- `CatalogWeekSkeletonCalendarMaterializerTests.cs`: 39/39 (incl. the 140-combination exhaustive safety proof)
- `DatedGeneratedCatalogPlanSkeletonValidatorTests.cs`: 8/8
- `Phase4F5DarkCalendarWiringTests.cs`: 6/6
- `Phase4F4ConfirmAndLegacyRegressionTests.cs` (extended): 2/2

## 36. RuntimeCatalog test results

`dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~RuntimeCatalog"` → **575 passed, 0 failed, 0 skipped, 575 total**.

## 37. Full-suite results

`dotnet test RunningApp.sln -c Release --no-build` → **618 passed, 0 failed, 0 skipped, 618 total**, duration ~6s.

## 38. Exact test-count reconciliation

565 (Phase 4F.4 baseline) + 53 (Phase 4F.5 new: 39 materializer + 8 validator + 6 dark-calendar-wiring) = **618**, matching the observed full-suite result exactly. Two pre-existing tests were updated (not counted as new): `CatalogPreviewGeneratorTests.cs`/`Phase4F4DarkSkeletonWiringTests.cs`'s shared `PilotRequest` fixtures gained `PreferredDays`/`LongRunDay` values (their assertion logic is otherwise unchanged), and `Phase4F4ConfirmAndLegacyRegressionTests.cs`'s two tests were broadened/renamed to also cover the calendar materializer (byte-for-byte same assertion style, wider match string).

## 39. Files changed

Modified: `CatalogPreviewGenerator.cs`, `CatalogPreviewGeneratorTests.cs`, `Phase4F4DarkSkeletonWiringTests.cs`, `Phase4F4ConfirmAndLegacyRegressionTests.cs`.
New: `CatalogCalendarAssignmentContracts.cs`, `CatalogCalendarAssignmentExceptions.cs`, `CatalogPreferredDayAdapter.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`, `CatalogCalendarAssignmentFixtures.cs`, `CatalogWeekSkeletonCalendarMaterializerTests.cs`, `DatedGeneratedCatalogPlanSkeletonValidatorTests.cs`, `Phase4F5DarkCalendarWiringTests.cs`, this document.

## 40. Deferred work

Habit-plan calendar assignment, final workout-type prescription, weekly volume/distance/duration/pace/intensity/segment calculation, long-run distance/taper prescription, week/day persistence, catalog confirm enablement, concurrent-confirm protection, v10 publication, frontend clustering warning/disclaimer, `StageKey`→`PhaseKey` rename migration.

## 41. Public activation blockers

`TEN_K__4D__INTERMEDIATE v10`'s real catalog JSON `metadata.status` remains DRAFT; the PUBLISHED-only eligibility gate (unchanged) continues to block every real, non-dry-run request before the dark calendar-assignment call site is reachable.

## 42. Final classification

**`BACKEND_DARK_MATERIALIZES_BINDING_CALENDAR_DATES_DURING_ELIGIBLE_PREVIEW_WITHOUT_PUBLIC_SCHEDULE_OUTPUT`**
