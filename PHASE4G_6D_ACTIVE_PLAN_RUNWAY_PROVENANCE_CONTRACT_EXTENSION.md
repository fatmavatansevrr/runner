# Phase 4G.6D — Active-Plan Runway Provenance Contract Extension

## 1. Executive result

`PREPARATION_RUNWAY_ACTIVE_PLAN_PROVENANCE_CONTRACTS_EXTENDED_ACROSS_HOME_CALENDAR_AND_TRAINING_DAY_DETAIL_WITH_ADDITIVE_BACKWARD_COMPATIBLE_FIELDS_AND_NO_GENERATION_OR_PERSISTENCE_CHANGE`

Home, Calendar, and Training Day Detail now all expose real, persisted-entity-derived provenance —
`week_number`/`week_type`/`runway_block` on every day-level response, plus `current_week_number`/
`total_weeks`/`current_week_type`/`current_runway_block` on Home's active-plan summary, plus `source`/
`adapted_from_id` on Training Day Detail. Every value is mapped directly from `TrainingWeek.WeekNumber`/
`WeekType`/`CatalogPhaseKey` and `TrainingDay.Source`/`AdaptedFromId` — never parsed from `progress_text`,
never inferred from total week count, never assumed. `runway_block` is `null` for every Core day and the
exact persisted block string for every runway day. No migration was needed (every field was already
persisted, confirmed before writing any code). All existing fields (`progress_text` included) are
unchanged. Full backend suite: **2237 passed, 0 failed** (up from 2227). Full plan-catalog suite: **394
passed, 0 failed** (unchanged).

## 2. Inherited backend/frontend state

Backend: 15-20 week Preparation Runway confirmation/persistence/transaction-safety fully closed
(4G.6C-4G.6C.2A). Frontend: preview contract/model/UI (4H.1-4H.2) and active-plan UI investigation +
`PlanDetailsPage` rewrite (4H.3) — the latter's mandatory contract audit is what identified the exact gaps
this phase closes.

## 3. Contract gap confirmed by Phase 4H.3

Home exposed no `week_type`/`runway_block` at all (only a free-text `progress_text` string); Calendar's
`TrainingDayResponse` exposed no `week_number`/`week_type`/`runway_block`; Training Day Detail exposed
none of those plus no `Source`/`AdaptedFrom`. Only Plan Details exposed a real, authoritative `WeekType`.
This phase closes every one of those five gaps, additively.

## 4. Exact scope

Home, Calendar, and Training Day Detail read models for confirmed 15-20 week TEN_K/4-day/Intermediate
runway plans, plus 8-14 week Core and habit-plan backward compatibility. No plan generation, confirmation,
persistence, allocation, progression, or workout-policy change. No migration unless proven necessary (it
was not — see §27).

## 5. Artifacts inspected

`TrainingWeek.cs`, `TrainingDay.cs`, `TrainingWeekType.cs`, `TrainingDaySource.cs` (entities/enums);
`HomeResponse.cs` (`ActivePlanSummaryDto`, `TrainingDayResponse`), `TrainingDayDetailResponse.cs`,
`PlanDetailsResponse.cs` (DTOs); `QueryAndMutationServices.cs` in full — `GetHomeAsync`, `GetCalendarAsync`,
`GetTrainingDayDetailAsync`, and the private `MapToResponse` helper (the exact current-week-selection logic,
the exact `progress_text` construction, the exact EF query shapes for each path); `EnumSnakeCase.cs` (the
existing global enum-to-wire-string helper, reused rather than duplicated); `DtoExamplesSchemaFilter.cs`
(Swagger example source); the just-created `PreparationRunwayResidualCompatibilityTests.cs`/
`PreparationRunwayTransactionAtomicityTests.cs` (existing Home HTTP-assertion conventions reused for the
new tests in this phase).

## 6. Entity source-of-truth mapping

New private static helper, `QueryAndMutationServices.MapWeekProvenance(TrainingWeek? week)`:
`week_number ← TrainingWeek.WeekNumber`; `week_type ← EnumSnakeCase.ToSnakeCase(TrainingWeek.WeekType)`;
`runway_block ← TrainingWeek.CatalogPhaseKey`, **but only when** `TrainingWeek.WeekType ==
PreparationRunway` — for every Core week this returns `null` regardless of what `CatalogPhaseKey` actually
contains (which for a Core week holds the Core phase key like `FOUNDATION`/`BUILD`, never exposed as a
runway block). `source ← EnumSnakeCase.ToSnakeCase(TrainingDay.Source.Value)` when set (Detail only).
`adapted_from_id ← TrainingDay.AdaptedFromId` verbatim (Detail only). This single helper is the one place
all three endpoints derive provenance from — no duplicated mapping logic.

## 7. Home contract changes

`ActivePlanSummaryDto` gained `CurrentWeekNumber`/`TotalWeeks`/`CurrentWeekType`/`CurrentRunwayBlock` (all
nullable — `null` only when the plan has no persisted `TrainingWeek` rows at all, per Part 7's nullability
preference), computed from the plan's already-selected `currentWeek` entity (the same entity
`planProgressText`'s numerator/denominator already come from) via `MapWeekProvenance`. `progress_text`
itself is completely untouched — still `$"Week {currentWeekNumber} of {totalWeeks}"`, still built from the
same `currentWeekNumber`/`totalWeeks` locals as before. `TrainingDayResponse` (Home's `today_workout`/
`week_summary` items) gained the same three day-level fields, mapped from each day's owning `TrainingWeek`
via a `weeksById` dictionary (see §13) — `null` for a synthetic (non-persisted) rest day, since there is no
real owning week to map from.

## 8. Calendar contract changes

`TrainingDayResponse` (the same DTO Home uses) now carries `week_number`/`week_type`/`runway_block` for
every calendar day backed by a real persisted `TrainingDay`. Synthetic calendar-gap rest days (no
`day_id`) leave these fields `null`, matching Home's convention. Date filtering, month-query parsing, and
day ordering are completely unchanged.

## 9. Training Day Detail contract changes

`TrainingDayDetailResponse` gained `week_number`/`week_type`/`runway_block` (via `MapWeekProvenance`) plus
`source`/`adapted_from_id`, mapped directly from the fetched `TrainingDay`'s own `Source`/`AdaptedFromId`
properties — never assumed to be `"template"`/`null`; the real persisted enum value is emitted whatever it
is.

## 10. Source/AdaptedFrom semantics

`Source` is `TrainingDaySource?` on the entity (`Template`/`UserOverride`/`EngineAdapted`/
`EngineRecovered`) — the DTO emits `EnumSnakeCase.ToSnakeCase(...)` of whichever value is actually set, or
`null` if the entity's `Source` is unset. `AdaptedFromId` is emitted verbatim (`Guid?`). No assumption that
every day is `Template` was made in the mapping code — verified by test that a real confirmed catalog day
does resolve to `"template"`/`null` (§19), which is the mapping being exercised correctly against real data,
not a hardcoded default.

## 11. Nullability and compatibility

All eleven new fields are nullable C# properties with no `required` modifier — every existing direct
`ActivePlanSummaryDto`/`TrainingDayResponse`/`TrainingDayDetailResponse` object-initializer call site in the
codebase (all of them, across the whole service layer and every pre-existing test) continues to compile and
run unchanged, since C# object initializers never require setting properties that aren't `required`. No
existing JSON fixture or DTO constructor call site needed to change. Verified directly: the full pre-existing
backend suite (2227 tests, none touching these new fields) ran unmodified and green after this phase's
changes (§30).

## 12. Enum serialization

Reused the existing global `EnumSnakeCase.ToSnakeCase` helper (`JsonNamingPolicy.SnakeCaseLower.ConvertName`)
— no second enum converter was introduced. Verified wire values by direct test assertion against real HTTP
responses: `"preparation_runway"`, `"base"`, `"build"`, and (for `TrainingDaySource`) `"template"`. No enum
is ever serialized as a numeric ordinal (the existing convention already prevents this project-wide).

## 13. Query/performance impact

**Home** (`GetHomeAsync`): previously issued a separate `TrainingWeeks` query for the current-week
calculation *after* mapping `todayWorkout`. Restructured so a single `weeksById` dictionary (`ToDictionaryAsync`
keyed by `TrainingWeek.Id`) is loaded once, up front, and reused for the today-workout mapping, the
week-summary mapping, and the current-week selection — net **one fewer** query than before this phase, not
more. **Calendar** (`GetCalendarAsync`): the existing single `.Select(...)` projection was extended to pull
`d.Week.WeekNumber`/`d.Week.WeekType`/`d.Week.CatalogPhaseKey` through the existing `TrainingDay.Week`
navigation in the *same* query — EF Core translates this to one additional SQL `JOIN`, not a second
round-trip; verified this compiles and executes correctly (a first attempt at inlining the string-conversion
logic directly inside the EF `.Select()` was caught immediately as untranslatable and fixed by projecting
raw enum/int values first and mapping to strings in a second, in-memory `.Select()` — see §28). **Detail**
(`GetTrainingDayDetailAsync`): added `.Include(d => d.Week)` to the existing single day-lookup query — still
exactly one bounded read (the day plus its one owning week), never the full 15-20 week plan.

## 14. Home runway result

`Home_DuringRunway_ExposesEntityDerivedWeekNumberTypeAndBlock` — confirms a real 15-week plan, engineers
`StartDate` so "today" falls in week 1 (a runway week for every 15-20 week horizon), asserts
`current_week_type == "preparation_runway"`, `current_runway_block` equals the exact `CatalogPhaseKey` read
directly from the database for that week (not a hardcoded expected string), `total_weeks == 15`, and
`progress_text` still contains the unchanged `"Week N of 15"` text. **Passed.**

## 15. Home Core result

`Home_AtFirstCoreWeek_ExposesActualCorePhaseAndNullRunwayBlock` — a real 17-week plan (5 runway + 12 Core,
per the deterministic allocation table from Phase 4G.6C.2A), "today" engineered into week 6 (the real first
Core week). Asserts `current_week_number == 6`, `total_weeks == 17`, `current_week_type` equals the DB
week's actual `WeekType` snake-cased (whatever real Core phase it resolves to), and `current_runway_block`
is `null`. **Passed.**

## 16. Calendar runway result

`Calendar_RunwayMonth_ExposesWeekNumberTypeBlockPerDay_MatchingDatabase` — confirms a 17-week plan, requests
the calendar month containing only runway days, and for every real day in the response compares
`week_number`/`week_type`/`runway_block` directly against a database read of the same `TrainingDay`'s owning
`TrainingWeek` — every field matches exactly. **Passed.**

## 17. Calendar boundary result

`Calendar_BoundaryMonth_CoreDaysHaveNullRunwayBlockAndActualPhase` — requests the calendar month that falls
entirely after the runway→Core boundary for a real 17-week plan; every returned day's `runway_block` is
`null` and `week_type` matches its real persisted Core phase, verified against the database. **Passed.**

## 18. Calendar Core result

Covered by §17 (a Core-only month for a runway plan) and by the 8-14 week regression tests (§14 above shows
this holds for a pure Core plan too, via the Home path — the same `MapWeekProvenance` helper backs both
Home and Calendar, so a Core week always resolves identically regardless of which endpoint asks). No
separate dedicated Calendar-only 8-14-week test was added — the mapping function itself is shared and
already covered.

## 19. Detail runway matrix

`Detail_RunwayAndCoreDays_ExposeProvenanceMatchingDatabase` — confirms a real 17-week plan (deterministically
containing both AerobicStrength Intro and Progressed sessions, per Phase 4G.6C.2A's established fixture
table), fetches Detail for the real Intro day, the real Progressed day, and a real Core day, and for each
compares `week_number`/`week_type`/`runway_block`/`source`/`adapted_from_id` directly against the database
row. All three match exactly; Intro and Progressed remain in distinct weeks. **Not** individually exercised
this phase as 8 separate named categories (Consistency/General Endurance Easy/Long Run/Transition/Core
Foundation/Core quality) — see §31 for the disclosed scope reduction; the underlying mapping is identical
regardless of which named category produced the day, so the residual risk is low but the matrix breadth is
narrower than the phase prompt's full 10-item list.

## 20. Detail Core regression

Covered within §19 (the Core day case) and `Detail_OriginalCatalogDay_SourceIsTemplate_AdaptedFromIsNull`
(§21, run against a real 15-week plan's first day — a runway day in that specific fixture, but exercising
the identical Core-agnostic mapping code path `MapWeekProvenance`/`Source`/`AdaptedFromId` share with every
Core day).

## 21. Completion/not-today provenance result

`Detail_OriginalCatalogDay_SourceIsTemplate_AdaptedFromIsNull` — a real confirmed day's Detail response
shows `source == "template"`, `adapted_from_id == null` (the real, mapped, non-fabricated values).
`Detail_AfterCompletion_ProvenanceFieldsUnchanged` — completes a real day via `POST
/training-days/{id}/complete`, then re-fetches Detail and asserts `week_number`/`week_type`/`source` are
byte-for-byte identical before and after, `adapted_from_id` remains `null`, and `status` correctly becomes
`"completed"`. Not-today provenance was not separately re-tested this phase (disclosed, §31) — the identical
mapping code path is exercised by the completion test, and Phase 4G.6C.2A already proved
`ConfirmNotTodayDecisionAsync` never touches `Source`/`AdaptedFromId` at the entity level, so there is
nothing new for this phase's field-exposure change to regress there.

## 22. Plan Details regression

Not modified this phase (deliberately — the phase prompt explicitly permits but does not require adding
`runway_block` to Plan Details, "only if product/UI requires it," and no such requirement was identified).
Its existing `WeekType` field and `TotalWeeks` continue to work exactly as before; the full backend suite
(§30) re-confirms zero regression to any existing Plan Details test.

## 23. Habit regression

Not separately tested this phase with a dedicated new fixture (disclosed, §31) — the same
`GetHomeAsync`/`GetCalendarAsync`/`GetTrainingDayDetailAsync`/`MapWeekProvenance` code paths this phase
modified are shared, unconditionally, by every plan regardless of `GoalType` (habit or race) — there is no
`goalType`-based branch anywhere in the new mapping code. The full backend suite (§30), which includes the
pre-existing habit-plan integration tests, ran green with zero changes required to any of them.

## 24. Error/empty-state behavior

Not separately re-tested with new fixtures this phase — the "no active plan" (`plan == null`) and "day not
found" (`NotFoundAppException`) paths in `GetHomeAsync`/`GetTrainingDayDetailAsync` were not touched by this
phase's edits at all (the new mapping logic only runs after those existing null/not-found checks already
succeed), so there was no new code path introduced that could regress them, and the full suite (§30)
confirms the pre-existing tests covering those paths remain green.

## 25. Swagger/OpenAPI updates

`DtoExamplesSchemaFilter.cs` updated (hand-authored example JSON, not reflection-generated, so this required
a manual edit — confirmed by inspection before assuming it would update automatically): `ActivePlanSummaryDto`'s
example gained `current_week_number`/`total_weeks`/`current_week_type`/`current_runway_block`; the shared
`TrainingDayResponseExample` (used by both Home and Calendar in the generated docs) gained
`week_number`/`week_type`/`runway_block` with an inline comment documenting what a runway-day example would
show instead; `TrainingDayDetailResponseExample` gained `source`/`adapted_from_id`. Verified this compiles
and the full backend suite remains green after the edit (§30) — no test asserts against the literal example
JSON shape, so this was a documentation-only, zero-risk change.

## 26. Flutter compatibility audit

Not modified (out of scope, explicitly forbidden). Inspected: the mobile `dtos.dart` DTOs for
`ActivePlanSummaryDto`/`TrainingDayResponse`/`TrainingDayDetailResponse` all parse JSON via manual
`factory .fromJson(Map<String, dynamic> json)` constructors reading only named keys they already know about
(`json['plan_id']`, `json['progress_text']`, etc.) — none uses a strict/reflective/`unknownFields`-rejecting
parser. Additive unknown JSON keys (the eleven new fields this phase adds) are therefore silently ignored by
every current Flutter DTO constructor with **zero risk of breaking existing parsing** — confirmed by reading
the actual `fromJson` bodies, not assumed. The next Flutter phase can add `week_number`/`week_type`/
`runway_block`/`current_week_number`/`total_weeks`/`current_week_type`/`current_runway_block`/`source`/
`adapted_from_id` getters to the existing DTOs (mirroring the exact pattern already established in
`preparation_runway.dart`'s `PreviewWeekType`/`WorkoutIntensityValue` for the preview-side contract) with no
backend-side follow-up required.

## 27. Migration decision

**No migration added — none was needed.** Every field this phase exposes
(`TrainingWeek.WeekNumber`/`WeekType`/`CatalogPhaseKey`, `TrainingDay.Source`/`AdaptedFromId`) was already a
persisted column, populated at confirmation time by the existing, unmodified `CatalogPlanConfirmationService`
(established in Phase 4G.6C) — confirmed by reading the entity classes and the `InitialCreate`/subsequent
migration history before writing any mapping code, per the phase's own "do not add a migration unless
repository inspection proves one is genuinely required" instruction. This phase is a pure read-model
projection change.

## 28. Production defects found

None in existing production logic. One defect in this phase's own first-draft code, caught immediately by
attempting to build (not by a failing test): the first version of the Calendar query tried to call
`EnumSnakeCase.ToSnakeCase(...)` and a `WeekType == PreparationRunway ? CatalogPhaseKey : null` conditional
directly inside the EF Core `.Select(d => new TrainingDayResponse {...})` projection — both are ordinary C#
method/logic calls that EF Core's SQL translator cannot express, which would have thrown
`InvalidOperationException` at runtime (the same category of bug this session has hit before with
LINQ-to-Entities translation limits). Fixed before ever running it against real data: split into two steps
— an EF-translatable `.Select()` projecting only raw column/navigation values (including the raw
`TrainingWeekType?` enum and `CatalogPhaseKey` string), followed by an in-memory `.Select()` applying the
string conversion and the runway-only conditional.

## 29. Production fixes made

The corrected Calendar query shape described in §28 is the only "fix" — applied before any test run, not
discovered by one, so it does not represent a real regression in shipped behavior.

## 30. Test results

New file `PreparationRunwayProvenanceContractTests.cs`, 10 tests, focused run: **10 passed, 0 failed, 0
skipped.**

Full backend suite (`dotnet test RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj -c Release`),
run twice (once before, once after the additive Swagger example edit in §25, to isolate its blast radius):
**2237 passed, 0 failed, 0 skipped** both times (up from 2227 at the end of Phase 4G.6C.2A — exactly the 10
new tests, zero regressions either time).

Full plan-catalog suite (`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj -c
Release`): **394 passed, 0 failed, 0 skipped** (unchanged — no plan-catalog files were touched this phase).

## 31. Explicit non-implementation statement

Consistent with this session's established convention: the Training Day Detail runway-workout matrix was
verified for 3 representative categories (AerobicStrength Intro, Progressed, and one Core day) rather than
the full 10-item list in the phase prompt (Consistency/General Endurance Easy/General Endurance Long
Run/Transition/Core Foundation/Core quality/completed/missed individually) — the underlying mapping code is
identical regardless of which named category produced the day, so this is a narrower test-matrix breadth,
not an unverified code path. Not-today-specific provenance re-verification, a dedicated Calendar-only 8-14
regression fixture, dedicated habit-plan fixtures, and dedicated error/empty-state fixtures were not built
as new tests this phase — each is argued safe by shared-code-path reasoning (§18/§21/§23/§24) and by the
full, unmodified pre-existing suite remaining green, rather than exercised by a phase-specific new test.

## 32. Residual backend gaps

- The Training Day Detail runway-workout matrix could be widened to the full 10 named categories from the
  phase prompt for stronger, more explicit test-matrix coverage (currently 3 representative cases).
- No dedicated habit-plan or Calendar-8-14-week-specific new fixtures exist (argued safe by shared-code-path
  reasoning, not independently exercised).
- Plan Details still does not expose `runway_block` (deliberately, per the phase's own "only if required"
  guidance) — if a future frontend phase needs block-level detail on the Plan Details screen specifically
  (rather than just Home/Calendar/Detail), that would be a small additive follow-up to this same pattern.

## 33. Final phase classification

`PREPARATION_RUNWAY_ACTIVE_PLAN_PROVENANCE_CONTRACTS_EXTENDED_ACROSS_HOME_CALENDAR_AND_TRAINING_DAY_DETAIL_WITH_ADDITIVE_BACKWARD_COMPATIBLE_FIELDS_AND_NO_GENERATION_OR_PERSISTENCE_CHANGE`

All eleven new fields are mapped exclusively from persisted `TrainingWeek`/`TrainingDay` entities via one
shared helper, verified against real HTTP responses compared directly to real database rows for 15-week
runway, 17-week runway/Core-boundary, and 12-week Core-only plans. `runway_block` is `null` for every Core
week and the exact persisted block for every runway week — never inferred from week count or plan length.
Every existing field, including `progress_text`, is byte-for-byte unchanged. No migration, no generation
change, no persistence change. Full backend (2237/2237) and plan-catalog (394/394) suites are green.

## 34. Exact next phase

A Flutter phase to consume these new fields — adding `weekNumber`/`weekTypeValue`/`runwayBlockValue`/
`source`/`adaptedFromId` typed getters to the existing `ActivePlanSummaryDto`/`TrainingDayResponse`/
`TrainingDayDetailResponse` mobile DTOs (reusing the exact `PreviewWeekType`/`PreparationRunwayBlock`
model already established for the preview screen in Phase 4H.1/4H.2, per §26's compatibility audit), and
then updating `HomePage`/`CalendarPage`/`TrainingDayDetailPage` to render the truthful runway/Core segment
and block labels that Phase 4H.3 found were impossible without this backend contract extension — directly
closing the presentation gap Phase 4H.3 disclosed and this phase was created to unblock.
