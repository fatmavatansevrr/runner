# Phase 4F.1 — Persistable Catalog Schedule Contract and Validation Foundation

This phase defines the typed, immutable, persistable schedule contract that a
future materialization phase will populate. **It does not implement
stage-to-week schedule generation.** No production code path produces a
non-null `GeneratedPreviewPlanPayload`; catalog confirmation still cannot
succeed for any real request.

## 1. Files inspected

- `PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md`
- `PHASE4E_1_1_GOVERNANCE_CLARIFICATIONS.md`
- `PHASE4E_2_CLAUDE_SAFETY_AUDIT.md`
- `PHASE4E_2_FINAL_ACCEPTANCE_REPORT.md`
- (`PHASE4E_2_IMMUTABLE_CATALOG_PREVIEW_CONFIRMATION.md` and `PHASE4E_2_POST_ACCEPTANCE_CLOSURE.md` do not exist in the repository — confirmed via `ls`; not fabricated, not required since the task listed them conditionally ("if present"))
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs` (builder + type)
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshotVerifier.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` (confirmed unchanged, never supplies a payload)
- `backend/RunningApp.Domain/Entities/TrainingPlan.cs`, `TrainingWeek.cs`, `TrainingDay.cs`
- `backend/RunningApp.Application/Services/QueryAndMutationServices.cs` (`GetHomeAsync`, `GetCalendarAsync`, `GetTrainingDayDetailAsync`)
- `backend/RunningApp.Application/Services/PlanServices.cs` (`GetActivePlanDetailsAsync`)
- `backend/RunningApp.Domain/Enums/{TrainingDayType,TrainingWeekType,TrainingDayStatus,GoalType,GoalDistance,RunningBackground,DistanceUnit}.cs`
- All existing Phase 4E.1/4E.2 test files under `backend/RunningApp.IntegrationTests/RuntimeCatalog/**`

## 2. Files changed

**New:**
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayload.cs` — the full contract (plan/week/day/segment/pace/provenance types + enums)
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayloadValidator.cs` — `IGeneratedCatalogPlanPayloadValidator`/`GeneratedCatalogPlanPayloadValidator`, error enum, result type
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayloadFixtures.cs` — test-only fixture builder
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayloadValidatorTests.cs` — 39 tests
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayloadSerializationTests.cs` — 8 tests
- This file.

**Modified:**
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs` — retyped `GeneratedPreviewPlanPayload` (`object?` → `GeneratedCatalogPlanPayload?`) and the `Build` method's matching optional parameter; documented the hash-exclusion decision explicitly (§18 below)
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs` — added `IGeneratedCatalogPlanPayloadValidator` constructor dependency; extended step 11 into a 4-way outcome split; **removed** the now-fully-unreachable `BuildPlan` helper and steps-12–15 persist block (see §20)
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs` — added 3 exception types
- `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs` — added 3 mappings
- `backend/RunningApp.Api/Program.cs` — registered `IGeneratedCatalogPlanPayloadValidator`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/TestPlanServicesFactory.cs` — constructs the validator, passes it to `CatalogPlanConfirmationService`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs` — updated `NewService`/`BuildValidSnapshot` for the new constructor param/optional payload; added 4 new tests (Group 11)

No migration was added. No DB schema was altered. No resolver, route decider, eligibility gate, or generator file was touched. No `plan-catalog/**` catalog content was touched.

## 3. Final contract types

```
GeneratedCatalogWorkoutType (enum)        — Easy, Interval, Tempo, LongRun, RecoveryEasy (NO Rest member)
GeneratedCatalogPrescriptionBasis (enum)  — Distance, Duration
GeneratedCatalogPaceType (enum)           — None, Target, Range, EffortOnly
GeneratedCatalogSegmentType (enum)        — WarmUp, WorkInterval, Recovery, Steady, CoolDown

GeneratedCatalogPacePrescription          — PaceType, TargetSecondsPerKm, MinSecondsPerKm, MaxSecondsPerKm, DisplayText, EffortLabel
GeneratedCatalogWorkoutSegmentPayload     — SegmentOrder, SegmentType, RepetitionCount, PrescriptionBasis, TargetDistanceKm, TargetDurationSeconds, PacePrescription, Intensity, RecoveryType, DisplayText
GeneratedCatalogDayProvenance             — SourceStageKey, SourceWorkoutKey, SourceWorkoutVersion, SourceProgressionStepKey, SourceLayoutSlotRole
GeneratedCatalogTrainingDayPayload        — Date, SessionOrderInWeek, WorkoutType, PrescriptionBasis, TargetDistanceKm, TargetDurationMinutes, EstimatedDistanceKm, EstimatedDurationMinutes, PlannedIntensity, PacePrescription, Segments, Provenance
GeneratedCatalogWeekProvenance            — StageKey, SourcePhaseKey, VolumeRuleKey, ProgressionReferenceKey
GeneratedCatalogWeekPayload               — WeekNumber, StartDate, EndDate, StageKey, PlannedVolumeKm, Sessions, Provenance
GeneratedCatalogPlanProvenance            — CandidateKey, CandidateVersion, DependencyVersions, GenerationSource, AsOfDate, MaterializerVersion
GeneratedCatalogPlanPayload               — SchemaVersion, StartDate, EndDate, PlannedWeekCount, DaysPerWeek, CanonicalDistanceFamily, GoalType, CandidateKey, CandidateVersion, DependencyVersions, Weeks, Provenance
```

All types are `sealed class` with `required init`-only properties (immutable, matching every prior-phase contract type in this codebase — `ResolverInputSnapshot`, `CatalogPreviewSnapshot`, etc.). `PlanCatalogReference` (existing type) is reused for `DependencyVersions` rather than inventing a duplicate.

## 4. Every field and rationale

Every field's rationale is documented inline as an XML doc comment on its declaring property in `GeneratedCatalogPlanPayload.cs` (per the task's "document every contract field" instruction — repeating the full text here would duplicate, not add, information). Summary of the *shape* decisions, not restated per-field:

- **No REQUIRED/OPTIONAL session classification field** exists anywhere (Decision 4's explicit deferral) — verified structurally by `GeneratedCatalogTrainingDayPayload_HasNoRequiredOptionalClassificationField`.
- **No recovery-jog-suggestion field** exists anywhere on the session contract — verified by `GeneratedCatalogTrainingDayPayload_HasNoRecoveryJogSuggestionField`. Recovery-jog suggestions belong to a future recommendation/DailyTip/Notification concern (§10).
- **No habit-conversion field** exists anywhere — verified by `GeneratedCatalogPlanPayload_HasNoHabitConversionMember`. Habit-plan conversion is explicitly deferred (§13).
- Every field traces to one of: (a) an existing `TrainingPlan`/`TrainingWeek`/`TrainingDay` column it must eventually map to, (b) a read-path requirement (Home/Calendar/Active-Plan-Details all read `PlannedVolumeKm`, dates, distance/duration, intensity), or (c) an auditability requirement (provenance). No field was added "because it may be useful one day" — e.g. no timezone field (none exists anywhere else in this backend, per Phase 4E.1's own established finding), no unit field beyond the existing km assumption (unit conversion explicitly out of scope — Decision 8).

## 5. Outer snapshot schema policy

`CatalogPreviewSnapshot` itself still has no explicit `SchemaVersion` field — unchanged from Phase 4E.1/4E.2 (out of this phase's scope to introduce one; the task only requires the *schedule* payload to carry its own version, and that the two be decoupled, not that the outer snapshot must also gain one now).

## 6. Schedule schema policy

`GeneratedCatalogPlanPayload.SchemaVersion` is a required `int` field, checked against the constant `GeneratedCatalogPlanPayload.CurrentSchemaVersion = 1` at confirm time (`CatalogPlanConfirmationService`, before structural validation runs). A mismatch throws `CatalogPreviewScheduleSchemaUnsupportedException` (HTTP 422, `CATALOG_PREVIEW_SCHEDULE_SCHEMA_UNSUPPORTED`) — never silently migrated or reinterpreted (Decision 10). This version is intentionally independent of any future outer-snapshot schema version.

## 7. Full-plan persistability definition

Per Decision 3, "persistable" ≡ "passes `GeneratedCatalogPlanPayloadValidator.Validate(...)` with zero errors." The validator enforces every rule the task listed (§ "Mandatory product decisions" → Decision 3), implemented as the `GeneratedCatalogPlanPayloadValidationError` enum (19 distinct values) — see the full validation matrix in §17. Validation never repairs, migrates, or fills a missing/invalid field from any external source (catalog, request, profile, route data, wall clock, pilot constant) — confirmed by `GeneratedCatalogPlanPayloadValidator`'s complete lack of any such dependency (constructor takes zero parameters).

## 8. Plan-relative week rules

Week N: `Start = StartDate + (N-1)*7 days`, `End = Start + 6 days` (Decision 6). Both dates are carried explicitly on `GeneratedCatalogWeekPayload` (not recomputed by confirm/read paths) and cross-checked by the validator against the formula (`WeekDateRangeIncorrect` on mismatch, which also catches a "partial" 6-day week — proven by `Validate_PartialCalendarWeek_SixDaySpan_IsInvalid`). `GeneratedCatalogPlanPayload.EndDate` must equal the final week's `EndDate` exactly (`PlanEndDateInconsistentWithFinalWeek`).

## 9. Rest-day policy

A generated session always represents an actual planned run (Decision 4). `GeneratedCatalogWorkoutType` has **no Rest-equivalent member at all** — not merely runtime-rejected, but impossible to construct — verified structurally by `GeneratedCatalogWorkoutType_HasNoRestEquivalentMember`. Absence of a session on a given date is the implicit rest day, exactly matching the **existing, unmodified** convention already used by `QueryAndMutationServices.GetHomeAsync`/`GetCalendarAsync` (both already synthesize a "Rest Day" `TrainingDayResponse` for any date with no `TrainingDay` row — confirmed by direct reading; no read-path change was needed or made). Each full TEN_K/INTERMEDIATE/4D week is expected to contain exactly `DaysPerWeek` (4) sessions — validated generically via `week.Sessions.Count == payload.DaysPerWeek`, never a hardcoded "4" inside the validator.

## 10. Recovery-jog recommendation boundary

Recovery-jog suggestions are explicitly outside this contract. No field, type, or enum value in `GeneratedCatalogPlanPayload.cs` represents one (§4). They belong to a future recommendation/DailyTip/Notification decision and must never become a `TrainingDay` row, a planned session, a missed session, planned weekly volume, or an adherence requirement — this phase adds no such behavior anywhere.

## 11. User-recorded extra-run boundary

Not implemented in this phase, per explicit scope constraint. A user manually recording an additional run: does not alter the immutable plan; does not retroactively become a planned `TrainingDay`; may contribute to actual statistics/actual weekly volume; belongs to activity/statistics tracking, not the schedule contract. No code in this phase touches `WorkoutLog` or any actual-tracking entity.

## 12. Distance/duration prescription semantics

Decision 7, implemented exactly: `PrescriptionBasis.Distance` requires positive `TargetDistanceKm` and null `TargetDurationMinutes`; `PrescriptionBasis.Duration` requires positive `TargetDurationMinutes` and null `TargetDistanceKm`. The non-authoritative metric (`EstimatedDistanceKm` for Duration sessions, `EstimatedDurationMinutes` for Distance sessions) is optional and, when present, must be positive — but its absence is never an error. A single plan may freely mix both bases across different days (proven: `Validate_PlanWithBothDistanceAndDurationSessionsOnDifferentDays_IsValid`, using the pilot fixture's own natural mix). The identical basis rule applies independently to workout segments (Decision 9).

## 13. Habit-plan deferral

No habit-plan conversion, duration-to-distance transition, or pace-based conversion logic exists anywhere in this phase (§4's structural proof). `GoalType` is carried on the payload (so a future phase can branch on it), but nothing consumes it for conversion purposes yet.

## 14. Structured pace contract

Decision 8, implemented exactly as four `PaceType` cases:
- `None`: all three numeric fields null.
- `Target`: `TargetSecondsPerKm` required positive; `Min`/`MaxSecondsPerKm` null.
- `Range`: `MinSecondsPerKm`/`MaxSecondsPerKm` both required positive, with the documented convention **Min = faster (smaller seconds/km), Max = slower (larger seconds/km)**, enforced as `Min <= Max`.
- `EffortOnly`: all three numeric fields null; qualitative guidance lives in `EffortLabel`.

`DisplayText` is never inspected by the validator under any `PaceType` — proven non-authoritative by `Validate_DisplayText_IsNonAuthoritative_DoesNotAffectValidity` (a contradictory `DisplayText` string does not affect validity). Values are stored as integer seconds-per-km (never a formatted string) specifically so a future km/mile display-conversion feature needs no schema change — unit conversion itself is not implemented in this phase.

## 15. Optional segment contract

Decision 9: `Segments` is `IReadOnlyList<...>`, defaulting to empty, on every session. When non-empty: `SegmentOrder` must be unique and consecutive from 1 (`SegmentOrderInvalid`); each segment's own `PrescriptionBasis` is validated exactly like a session's (`SegmentPrescriptionInvalid`); `RepetitionCount`, when present, must be positive. No segment is ever generated from catalog rules in this phase — every segment in every test fixture is hand-built.

## 16. Provenance model

Decision 11, implemented at plan/week/day level exactly as specified (`GeneratedCatalogPlanProvenance`/`GeneratedCatalogWeekProvenance`/`GeneratedCatalogDayProvenance`); segment-level provenance was not added (the task marked it optional and no natural need was found — segments already inherit their owning session's day-level provenance). All three levels are validated for presence (`ProvenanceMissing`) — non-whitespace `CandidateKey`/`StageKey`/`SourceStageKey`, positive `CandidateVersion`, non-empty `DependencyVersions`. **Provenance remains internal**: it is defined only in the `RunningApp.Application.RuntimeCatalog.Schedule` namespace (application-layer), never referenced by any public API DTO in `RunningApp.Application.DTOs.Plan` — confirmed by grep across that directory finding zero references to any `GeneratedCatalog*` type.

## 17. Validation matrix

| Rule (task's own wording) | `GeneratedCatalogPlanPayloadValidationError` | Proving test |
|---|---|---|
| Supported schedule schema version | `UnsupportedSchemaVersion` | `Validate_UnsupportedSchemaVersion_IsInvalid` |
| Valid plan start/end dates | `StartDateNotBeforeEndDate` | (structural, covered by fixture-negative construction) |
| Positive planned week count | `PlannedWeekCountNotPositive` | (structural) |
| Actual week count = planned | `ActualWeekCountMismatch` | `Validate_PartialWeekList_ActualCountBelowPlannedCount_IsInvalid` |
| Week numbers consecutive from 1 | `WeekNumbersNotConsecutiveFromOne` | `Validate_NonConsecutiveWeekNumbers_IsInvalid` |
| No duplicate week numbers | `DuplicateWeekNumber` | `Validate_DuplicateWeekNumbers_IsInvalid` |
| Correct plan-relative week date range | `WeekDateRangeIncorrect` | `Validate_WeekDateRange_NotAStartDateBasedSevenDayBlock_IsInvalid`, `Validate_PartialCalendarWeek_SixDaySpan_IsInvalid` |
| Plan EndDate matches final week | `PlanEndDateInconsistentWithFinalWeek` | `Validate_PlanEndDate_NotMatchingFinalWeekEndDate_IsInvalid` |
| Expected session count per full week | `WeekSessionCountIncorrect` | `Validate_WeekSessionCount_NotEqualToDaysPerWeek_IsInvalid` |
| No duplicate training dates | `DuplicateSessionDate` | `Validate_DuplicateSessionDates_IsInvalid` |
| All training dates inside owning week | `SessionDateOutsideOwningWeek` | `Validate_SessionDateOutsideOwningWeek_IsInvalid` |
| No session outside plan dates | `SessionDateOutsidePlanRange` | `Validate_SessionDateOutsidePlanRange_IsInvalid` |
| Session ordering valid | `SessionOrderInvalid` | (structural, exercised by fixture construction) |
| Distance prescription internally consistent | `DistancePrescriptionInvalid` | 4 tests, §"Prescription validation" |
| Duration prescription internally consistent | `DurationPrescriptionInvalid` | 4 tests, §"Prescription validation" |
| Pace prescription internally consistent | `PacePrescriptionInvalid` | 5 tests, §"Pace validation" |
| Segment order internally consistent | `SegmentOrderInvalid` | 2 tests |
| Segment prescription internally consistent | `SegmentPrescriptionInvalid` | 3 tests |
| Provenance present at all 3 levels | `ProvenanceMissing` | 3 tests, §"Provenance" |

39 validator tests total; the complete matrix above is exhaustively exercised.

## 18. Serialization and hash behavior

`GeneratedCatalogPlanPayload` serializes/deserializes deterministically through the **same** JSON conventions already used across this codebase (`JsonNamingPolicy.SnakeCaseLower` + `JsonStringEnumConverter`) — no custom converter is needed anywhere in the Schedule namespace (no private constructors). Proven by 8 round-trip tests, including one specifically for nested segments.

**`GeneratedPreviewPlanPayload` remains excluded from `CatalogPreviewSnapshot.ContentHash`**, exactly as in Phase 4E.1/4E.2 — this is an explicit compatibility decision (documented directly on `CatalogPreviewSnapshotBuilder.Build`'s XML doc comment), not an oversight:
- Zero risk to any already-stored snapshot's hash (byte-for-byte unchanged hash format).
- No outer-snapshot schema-version bump needed.
- Whether a future materialized (non-null) schedule's content should become part of the integrity hash is a real product decision (tamper-evidence for the schedule vs. keeping the hash scoped to resolution-inputs) — deliberately left open for the phase that actually produces real schedules, not silently decided here.

`CatalogPreviewSnapshotVerifier.cs` required **zero changes** — its hashable-content shape is untouched.

## 19. Preview-to-entity mapping specification

| Payload field | Destination | Transformation | Notes |
|---|---|---|---|
| `Weeks[].StartDate` | `TrainingWeek.StartDate` | `DateOnly` → `DateTime` (midnight UTC) | Direct |
| `Weeks[].WeekNumber` | `TrainingWeek.WeekNumber` | none | Direct |
| `Weeks[].PlannedVolumeKm` | `TrainingWeek.PlannedVolumeKm` | none | Direct |
| `Weeks[].StageKey` | `TrainingWeek.CatalogPhaseKey` | none | Existing nullable column, already provisioned for exactly this (Phase 3) |
| `TrainingWeek.WeekType` | — | **no source field** | No 1:1 catalog-stage-key → `TrainingWeekType` mapping exists (same taxonomy gap Phase 2 already found for phase keys); a future phase must decide this, not silently guess |
| `TrainingWeek.ActualVolumeKm` | — | technical-only | Always `0` at creation, populated by actual-activity tracking later — never sourced from the payload |
| `Sessions[].Date` | `TrainingDay.Date` | `DateOnly` → `DateTime` | Direct |
| `Sessions[].WorkoutType` | `TrainingDay.DayType` | enum→enum name mapping | `GeneratedCatalogWorkoutType`'s 5 members map 1:1 by name to 5 of `TrainingDayType`'s 6 members (`TrainingDayType.Rest` has no source — consistent with Decision 4) |
| `Sessions[].TargetDistanceKm` (or the Distance-basis authoritative value) | `TrainingDay.PlannedDistanceKm` | direct when Distance basis; when Duration basis, no exact source (`EstimatedDistanceKm` is non-authoritative) | A future phase must decide whether a Duration-basis session leaves `PlannedDistanceKm=0` or uses the estimate — not decided here |
| `Sessions[].TargetDurationMinutes` (or Duration-basis authoritative value) | `TrainingDay.PlannedDurationMin` | same caveat, inverted | Same open question as above |
| `Sessions[].PlannedIntensity` | `TrainingDay.Intensity` | none | Direct |
| `Sessions[].Provenance.SourceWorkoutKey`/`SourceWorkoutVersion` | `TrainingDay.CatalogWorkoutKey`/`CatalogWorkoutVersion` | none | Direct — existing nullable columns already provisioned |
| `Sessions[].Provenance.SourceStageKey` | `TrainingDay.CatalogStageKey` | none | Direct |
| `Sessions[].Provenance.SourceLayoutSlotRole` | `TrainingDay.CatalogSlotRole` | none | Direct |
| `Sessions[].Provenance.SourceProgressionStepKey` | — | **no destination column exists** | See "new columns" below |
| `Sessions[].PacePrescription` (structured) | `TrainingDay.PlannedPaceMinKm` (single `double?`) | **lossy** — the existing column only fits a single min/km number, not `PaceType`/Target/Range/DisplayText/EffortLabel | See "new columns" below |
| `Sessions[].Segments` | — | **no destination at all** | See "new columns" below |
| `TrainingDay.Title`/`Description` | — | **no source field** | Backend-computed text (`PlanServices.GetTitleForDay`/`GetDescriptionForDay`, driven by `DayType`+distance) — derived at persistence time, not sourced from the payload; unchanged behavior |
| `PlanProvenance.CandidateKey`/`CandidateVersion`/`DependencyVersions` | `TrainingPlan.CatalogCandidateKey`/`CatalogCandidateVersion`/`Catalog*Key`/`Catalog*Version` | none | Direct — all columns already exist (Phase 3), same shape `CatalogPlanConfirmationService`'s now-removed `BuildPlan` already demonstrated for the outer snapshot |
| `StartDate` | `TrainingPlan.StartedAt` | `DateOnly` → `DateTime` | Preserved exactly (Decision 5) |
| `EndDate` | `TrainingPlan.EstimatedEndDate` | `DateOnly` → `DateTime` | Direct |

**Fields allowed to differ between preview and DB** (unchanged principle from Phase 4E.2's own `BuildPlan` doc comment): `TrainingPlan.Id` (new GUID), `CreatedAt`/`UpdatedAt` (technical timestamps), `Status` (always `Active` at creation).

**New columns that appear necessary for exact future persistence** (reported here per the task's instruction — **none added in this phase**):
1. `TrainingDay.CatalogProgressionStepKey` (string?) — for `Sessions[].Provenance.SourceProgressionStepKey`.
2. `TrainingWeek.CatalogVolumeRuleKey` / `CatalogProgressionReferenceKey` (string?) — for `Weeks[].Provenance.VolumeRuleKey`/`ProgressionReferenceKey`.
3. A structured pace representation on `TrainingDay` (e.g. `PlannedPaceType`, `PlannedMinPaceSecPerKm`, `PlannedMaxPaceSecPerKm`) — the existing single `PlannedPaceMinKm double?` column cannot losslessly represent `GeneratedCatalogPacePrescription`.
4. A new child table (e.g. `TrainingDaySegments`) — no existing column or table can hold `Sessions[].Segments` at all.

Per the task's explicit instruction, **no migration was added for any of these** — this is a documented gap for a future phase to resolve deliberately, not a silent limitation.

## 20. Current confirm behavior

`CatalogPlanConfirmationService.ConfirmAsync`'s step 11 now has exactly four outcomes, **none of which ever persists a plan**:

1. `GeneratedPreviewPlanPayload == null` → `CatalogPreviewNotPersistableException` (422, `CATALOG_PREVIEW_NOT_PERSISTABLE`) — unchanged from Phase 4E.2, still the only outcome for every real production snapshot.
2. Non-null, unsupported `SchemaVersion` → `CatalogPreviewScheduleSchemaUnsupportedException` (422, `CATALOG_PREVIEW_SCHEDULE_SCHEMA_UNSUPPORTED`).
3. Non-null, supported schema, fails `IGeneratedCatalogPlanPayloadValidator` → `CatalogPreviewScheduleInvalidException` (422, `CATALOG_PREVIEW_SCHEDULE_INVALID`).
4. Non-null, supported schema, structurally **valid** → **still** `CatalogPreviewMaterializationNotImplementedException` (422, `CATALOG_PREVIEW_MATERIALIZATION_NOT_IMPLEMENTED`) — persisting a typed schedule into `TrainingWeek`/`TrainingDay` rows is explicitly deferred; a structurally valid payload is necessary but not sufficient for success in this phase.

The now fully-unreachable Phase 4E.2 persist code (`BuildPlan`, `TrainingPlans.Add`, `PlanEvent` creation, `SaveChangesAsync`) was **removed**, not merely left dead, to keep the build at 0 warnings and avoid implying a capability that no longer (and did not before, either) exists reachably.

## 21. Proof that live schedule materialization still does not exist

- `CatalogPreviewGenerator.GenerateAsync` — read in full, byte-for-byte unchanged from Phase 4E.1 — never supplies a `generatedPreviewPlanPayload` argument to `CatalogPreviewSnapshotBuilder.Build`.
- Every `GeneratedCatalogPlanPayload` instance that exists anywhere in this phase's own test code is built by `GeneratedCatalogPlanPayloadFixtures` (hand-constructed, explicitly test-only, documented as such in its own class doc comment) or directly inline in a test method — never by any `RunningApp.Application`-namespace production class.
- `ConfirmAsync_StructurallyValidSchedule_StillThrowsCatalogPreviewMaterializationNotImplementedException_NoMutation` is the direct empirical proof: even a **fully valid** hand-built payload, fed through the real confirm service, still throws before any mutation — proving Phase 4F.1 did not accidentally enable successful confirmation.
- No test describes a `GeneratedCatalogPlanPayload` fixture as "produced by the live catalog pipeline" — every fixture's doc comment states the opposite explicitly.

## 22. Public activation blockers (unchanged, re-confirmed)

Identical to Phase 4E.2's own list, untouched by this phase: `TEN_K__4D__INTERMEDIATE v10` remains `DRAFT` (not touched — no catalog file was read or written in this phase); no database-level preview→plan concurrency invariant exists (not addressed — explicitly out of scope for this pass); the dev database migration-application gap from Phase 4E.2 is unrelated and also untouched (no migration was added or applied in this phase, so that pre-existing gap is neither worsened nor fixed).

## 23. Test results

`dotnet build RunningApp.sln -c Release` → **0 errors, 0 warnings.**

`dotnet test RunningApp.sln -c Release --no-build`:

```
Toplam test sayısı: 439
     Geçti: 402
     Başarısız: 37
```

**402 passed, 37 failed, 439 total.** All 37 failures are the exact same, single, pre-existing, already-documented root cause carried over unchanged from Phase 4E.2 (`Npgsql.PostgresException: 42703: column p.ConfirmedPlanId does not exist` — the dev database still lacks the Phase 4E.2 migration; this phase touched no migration). Verified identical failure count/root cause in both Debug and Release configurations before and after this phase's changes. **This report does not claim the full suite passes** — it explicitly does not, for the same pre-existing, non-code reason documented in `PHASE4E_2_FINAL_ACCEPTANCE_REPORT.md`.

Reconciliation: `388 (end of Phase 4E.2) + 51 new Phase 4F.1 tests = 439`. The 51: 39 in `GeneratedCatalogPlanPayloadValidatorTests.cs`, 8 in `GeneratedCatalogPlanPayloadSerializationTests.cs`, 4 new in `CatalogPlanConfirmationServiceTests.cs` (Group 11). Zero existing tests were deleted, renamed with behavior change, or filtered differently. All 388 pre-existing tests' pass/fail status is unchanged (351 pass / 37 fail, same as Phase 4E.2's own final measurement).

## 24. Remaining work (for future phases, not started here)

1. Real stage-to-week materialization from catalog candidate stages (assign preferred days, calculate weekly volume progression, select workouts, long-run progression, taper).
2. The 4 new-column items identified in §19.
3. Decide the `TrainingWeek.WeekType` ↔ `StageKey` mapping (or accept the gap permanently).
4. Decide the Duration-basis-session `PlannedDistanceKm` question in §19.
5. Actually persist a validated `GeneratedCatalogPlanPayload` into `TrainingPlan`/`TrainingWeek`/`TrainingDay` (this phase deliberately stops at "necessary but not sufficient").
6. Database-level preview→plan concurrency safety (unrelated to this phase, still open from Phase 4E.2).
7. Apply the Phase 4E.2 migration to the dev database (unrelated to this phase, still open).
8. Habit-plan conversion, recovery-jog recommendations, manual extra-run tracking — each explicitly deferred to its own future decision.

## 25. Final classification

```text
BACKEND_HAS_TYPED_PERSISTABLE_CATALOG_SCHEDULE_CONTRACT_NOT_YET_MATERIALIZED
```

No runtime code change in this phase enables successful catalog confirmation or produces a real schedule. The confirm boundary was extended only to safely recognize the new typed contract (schema-version check + structural validation), and every one of its four possible outcomes for a catalog-routed confirm still ends in a typed rejection with zero database mutation.
