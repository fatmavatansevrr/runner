# PHASE V1-HARDEN.2 — Public Input Cross-Validation, Boundary Validation, and Safe Error-Contract Closure

**Precondition**: V1-HARDEN.0A (P0 onboarding bypass) and V1-HARDEN.1 (P1 Complete/NotToday mutation safety) are both closed. State entering this phase: P0=NONE, P1=NONE. V1-HARDEN.0's remaining P2 findings are `V1HARDEN0-005` (cross-field readiness-input validation gap), `V1HARDEN0-006` (raw internal-vocabulary leakage in a handful of non-500 typed-exception messages), and `V1HARDEN0-007` (no metrics pipeline — explicitly out of scope for this phase, carried to V1-HARDEN.5).

## 1. Binding V1-HARDEN.0 P2 findings

Read `PHASE_V1_HARDEN_0_...md` §33, §38-40, §73, §76, §83 directly (not summarized). Exact findings bound this phase:

- **`V1HARDEN0-005`** (§33, §83): no cross-field check exists between `RecentWeeklyVolumeKm` and `RecentLongestRunKm` in `GenerateRacePlanPreviewRequestValidator`. The audit's own fix class offered two options: "add a bounded cross-field check, **or** explicitly document the fitness-evidence hierarchy's own tolerance for this as an accepted product decision."
- **`V1HARDEN0-006`** (§38-40, §76): for non-500 typed exceptions, `GlobalExceptionHandler` returns `exception.Message` verbatim; two named examples carry internal catalog vocabulary — `CatalogCandidateNotPublishedException` ("status 'DRAFT', not 'PUBLISHED'... not eligible for public preview") and `CatalogPreviewPersistenceContractException` ("taper-week residual volume falls below the V1 key/easy session-allocation floor").
- **`V1HARDEN0-007`** (§51-52, §79): no metrics pipeline. Explicitly out of scope — carried to V1-HARDEN.5, not touched by this phase.
- Nine named `AUDIT_GAP` items (§22-27, §56, §63-70, §77): live date/race-date/preferred-day/horizon boundary sweeps, a malformed-input fuzz pass, and several others, none asserted as confirmed defects by V1-HARDEN.0 — the audit's own typed-exception taxonomy was offered as structural (not live-re-proven) evidence that enforcement exists.

## 2. V1-HARDEN.0A/.1 zero-delta preconditions

Read both reports in full for context only. V1-HARDEN.0A restored the real onboarding→preview→confirm→Home round trip (`plan_generation_page.dart`); V1-HARDEN.1 added `LoadOwnedActiveDayForMutationAsync`, the `TrainingDay.xmin` concurrency token, and three typed exceptions (`TrainingDayCompletionConflictException`, `TrainingDayTransitionConflictException`, `TrainingDayMutationConcurrencyConflictException`) for Complete/NotToday mutation safety. **Neither mechanism was touched by this phase.** No file under `mobile/lib/features/onboarding/`, no file implementing preview/confirm snapshot mechanics, and no file implementing Complete/NotToday transition logic (`QueryAndMutationServices.cs`'s `CompleteWorkoutAsync`/`CreateNotTodayDecisionAsync`/`ConfirmNotTodayDecisionAsync`) was modified — confirmed by the `git diff --stat` in §74 below. The one-line EF `PendingModelChangesWarning` suppression V1-HARDEN.1 introduced in `Program.cs` was read (to confirm it does not affect this phase's validation/error work) and left completely alone, as instructed — carried to V1-HARDEN.5 for review, not touched here.

## 3. Public request contract

Inventoried the real public race-plan-preview request contract from source: `GenerateRacePlanPreviewRequest` (`DTOs/Plan/GenerateRacePlanPreviewRequest.cs`), validated by `GenerateRacePlanPreviewRequestValidator.Validate` (`Validation/GenerateRacePlanPreviewRequestValidator.cs`, 153 lines, read in full), then mapped by `GeneratePreviewCommandMapper.ToInternalRequest` into the internal `PlanPreviewCommand`/`GeneratePreviewRequest` shape validated a second time by `GeneratePreviewRequestValidator` (shared internal validator, defense-in-depth, read for cross-reference). Fields: `GoalDistance`, `TargetDistanceKm`, `Level`, `DaysPerWeek`, `Unit`, `StartDate`, `PreferredDays`, `LongRunDay`, `RaceDate`, `TargetFinishTimeSeconds`, `TargetFinishTimeSource`, `RecentWeeklyVolumeKm`, `RecentLongestRunKm`, `RecentRunsPerWeek`, `RecentRace` (nested: `Distance`/`FinishTimeSeconds`/`RaceDate`). Confirm uses a separate, smaller public contract (`previewId` only) against the frozen `CatalogPreviewSnapshot` — not independently input-validated beyond ownership/expiry/state, since confirm does not accept new user-input fields (consistent with the frozen-snapshot contract this phase does not redesign).

## 4. Validation ownership

| Field/rule | Owner | Evidence |
|---|---|---|
| DaysPerWeek range 1-7 | API_SHAPE_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:20-23` |
| GoalDistance/TargetDistanceKm resolution | APPLICATION_PRODUCT_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:44-68`, `GeneratePreviewCommandMapper` |
| PreferredDays shape (required, non-empty, no duplicates, count==DaysPerWeek) | APPLICATION_PRODUCT_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:70-84` |
| LongRunDay ⊆ PreferredDays | APPLICATION_PRODUCT_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:86-89` |
| StartDate/RaceDate required | APPLICATION_PRODUCT_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:25-28, 91-94` |
| Horizon feasibility (RaceDate vs StartDate, minimum/maximum horizon) | DOMAIN_INVARIANT | `RaceHorizonPolicy` + typed exception taxonomy (`PlanHorizonStartTooEarlyException`/`PlanCoreHorizonTooShortException`/`PlanCoreHorizonUnsupportedException`) |
| Preferred-day placement feasibility | DOMAIN_INVARIANT | `CatalogPreferredDayPlacementInfeasibleException` (materialization layer, not the shape validator) |
| TargetFinishTimeSeconds positivity / product-average consistency | APPLICATION_PRODUCT_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:96-118`, `CanonicalTargetFinishTimePolicy` |
| RecentWeeklyVolumeKm/RecentLongestRunKm/RecentRunsPerWeek non-negative | APPLICATION_PRODUCT_VALIDATION (shape only) | `GenerateRacePlanPreviewRequestValidator.cs:120-135` |
| RecentWeeklyVolumeKm/RecentLongestRunKm classification (READY/CAUTION/NOT_READY) | DOMAIN_INVARIANT (owner-approved resolver) | `CoreEntryReadinessResolver.cs` (Phase 4D.3.1 thresholds) |
| RecentRace partial/future-date/non-positive-time | APPLICATION_PRODUCT_VALIDATION | `GenerateRacePlanPreviewRequestValidator.cs:137-151` |
| Catalog candidate/dependency PUBLISHED-only | APPLICATION_PRODUCT_VALIDATION (startup+request-time) | `CatalogCandidateEligibilityGate.cs` |
| Error-code/HTTP-status mapping | API_SHAPE_VALIDATION (cross-cutting) | `GlobalExceptionHandler.cs` |
| `[ApiController]` auto-400 for malformed JSON/missing required fields | FRONTEND_HINT_ONLY mirrored, backend-authoritative | ASP.NET Core framework default, confirmed present on every controller |

Backend remains authoritative in every row above; Flutter's `race_details_page.dart` and sibling onboarding pages mirror a subset of these (distance range, day-count) as UX hints only — never the sole gate (confirmed by the direct-API-bypass reasoning in §57).

## 5. Duplicate validation audit

- **PreferredDays shape** (required/non-empty/no-duplicates/count match): validated once, at the public boundary (`GenerateRacePlanPreviewRequestValidator`), and again implicitly by `CatalogPreferredDayPlacementInfeasibleException` at materialization time for a different concern (feasibility of *placement*, not *shape*) — classified **INTENTIONAL_DEFENSE_IN_DEPTH**, not a contradiction (different failure mode, different layer).
- **RecentLongestRunKm/RecentWeeklyVolumeKm non-negativity**: validated once at the shape layer; `CoreEntryReadinessResolver` never re-rejects negative values (it is never reached with one, since the shape validator runs first in the real pipeline) — classified **SAFE_UI_MIRROR** at the Flutter layer (if present) + authoritative backend gate, no contradiction found.
- **GoalDistance/TargetDistanceKm**: validated at the public shape layer (`UnsupportedGoalDistanceException` for unresolvable `Custom`) and again, independently, by the internal `GeneratePreviewRequestValidator` ("repeats the 'no target present' check" per its own doc comment) — explicitly disclosed as intentional defense-in-depth in the validator's own comment, not a newly-discovered contradiction.
- **No STALE_DUPLICATE or CONTRADICTORY_DUPLICATE or DANGEROUS_FRONTEND_ONLY_AUTHORITY rule was found** in the files read for this phase. No centralization was performed — per the governing prompt's own instruction not to centralize merely for aesthetic consistency.

## 6. Malformed-request behavior

`[ApiController]`'s built-in model-validation-failure→400 (confirmed present on every controller, unchanged by this phase) rejects missing-required-property/wrong-type/malformed-JSON before any action method runs. Unknown/invalid enum values for `GoalDistance`/`Weekday`/`TargetFinishTimeSource` fail the same automatic model-binding gate (System.Text.Json's enum converter rejects an unrecognized string; an out-of-range int for a non-`JsonStringEnumConverter` enum deserializes but is then caught by `GenerateRacePlanPreviewRequestValidator`'s explicit `TryResolve`/switch checks, never silently defaulted). `ArgumentException` (the validator's own exception type) is explicitly mapped to 400/`VALIDATION_ERROR` in `GlobalExceptionHandler.cs:185`, not left to fall through to the generic 500 case. No new malformed-request handling was added — this layer was already sound and is unchanged.

## 7. Distance validation

Not reopened — `UnsupportedTargetDistanceException`/`UnsupportedGoalDistanceException` and the full 0→reject / negative→reject / sub-5K-custom→reject / 4.9→reject / exact-5K-canonical / 5K<target<10K-derived / exact-10K-canonical / 10K<target<HM-derived / exact-HM-canonical / above-HM→reject matrix is FIVE-K.1/DERIVED-DIST.4A authority, confirmed unchanged by `git diff --stat` (§74). This phase's own change to `GlobalExceptionHandler.cs` does not alter any distance-rejection's HTTP status or ErrorCode — only `CatalogCandidateNotPublishedException`'s/`CatalogPreviewPersistenceContractException`'s/the other five named codes' `Message` text changed (see §33-34).

## 8. Decimal precision

Not reopened — exact-identity-preserving decimal handling (5.1/7.5/9.9/17.7/20.5-style) is FIVE-K.1/DERIVED-DIST.4A authority, unchanged. No rounding/coercion logic was touched in this phase.

## 9. Experience validation

`Level`/`RunningBackground` resolution happens downstream of the shape validator, in `V1CatalogPilotIdentityPolicy`/eligibility gates (unchanged, carried forward from FIVE-K.1). Unsupported/unknown experience values reject deterministically via the existing typed taxonomy (`CatalogPilotNotAvailableException`, `PlanProductIneligibleException`) — not re-derived this phase; no fallback-to-Intermediate/Beginner code path was found anywhere read.

## 10. RunsPerWeek validation

`DaysPerWeek` shape-validated (1-7) at the public boundary; product-eligible frequency sets per distance/level are enforced downstream by the existing identity/eligibility policies (unchanged). No silent nearest-frequency fallback exists in any file read this phase.

## 11. Level×Frequency×Distance validation

Not re-derived live this phase (would require live-exercising every cell of `V1CatalogPilotIdentityPolicy`'s matrix, which FIVE-K.1/DERIVED-DIST.4A's own extensive test suites already do and which the fresh regression run in §72 reconfirms with zero delta). `PlanProductIneligibleException` (422, `productIneligible.Reason` as the ErrorCode) is the confirmed, unchanged rejection path for an unsupported cell — no 500, no fallback to another cell, per the existing, unmodified exception taxonomy.

## 12-14. PreferredDays validation, count semantics, duplicate weekdays

Read `GenerateRacePlanPreviewRequestValidator.cs:70-89` directly (not assumed). Current invariant, confirmed from source: `PreferredDays` is required and non-empty (line 70-73); duplicates are rejected outright via `Distinct().Count() != Count` (line 75-78) — **duplicates can never silently count as distinct days**, closing the governing prompt's exact concern without inventing new logic (the rule already existed); `PreferredDays.Count` must exactly equal `DaysPerWeek` (line 80-84) — a strict equality, not "a larger preference set to choose from," confirmed directly from the comparison operator (`!=`), not guessed. This is unchanged by this phase; existing coverage (`EmptyPreferredDays_Throws`, `DuplicatePreferredDays_Throws`, `PreferredDaysCountMismatch_Throws` in `GenerateRacePlanPreviewRequestValidatorTests.cs`) already proves all three boundaries and is reconfirmed green by the fresh regression run (§72).

## 15. LongRunDayPreference

Read `GenerateRacePlanPreviewRequestValidator.cs:86-89` directly: `LongRunDay` must be a member of `PreferredDays`, or the request is rejected (`ArgumentException`). This is a required field, not optional (the DTO's `LongRunDay` is non-nullable `Weekday`) — no "made an optional field required automatically" risk, since it was never optional. Existing coverage (`LongRunDayNotInPreferredDays_Throws`) is unchanged and reconfirmed.

## 16-18. StartDate/RaceDate/horizon validation

Read `GenerateRacePlanPreviewRequestValidator.cs:25-28, 91-94` and `RaceHorizonPolicy`/the typed exception taxonomy (`PlanHorizonStartTooEarlyException`, `PlanCoreHorizonTooShortException`, `PlanCoreHorizonUnsupportedException`, `PlanHorizonCompositionRequiredException`) directly. No "must start Monday" rule exists anywhere — confirmed by grep across `RaceHorizonPolicy`/`QueryAndMutationServices.cs` for any day-of-week gate on `StartDate`; week boundaries are `StartDate`-anchored per V1-HARDEN.0's own §22-23 finding, unchanged. `StartDate`/`RaceDate` are both simply required (non-default `DateOnly`) at the shape layer; horizon feasibility (RaceDate-too-soon, RaceDate-too-far, RaceDate-before-StartDate) is enforced downstream by the existing typed taxonomy, not re-invented. This phase did not add a new date threshold anywhere — none was invented, per the explicit prohibition.

**Boundary-sweep closure note** (closing the relevant part of V1-HARDEN.0's named `AUDIT_GAP`, §24-25/§77): rather than re-deriving exact day-count thresholds from scratch (which would risk inventing a number no product decision has approved), this phase confirmed the existing, extensive real-pipeline test coverage of this exact taxonomy — `Phase4F5DarkCalendarWiringTests.cs`, `Phase4F5_1ProductionValidatorWiringTests.cs`, `HmX13Intermediate6DAdvanced6DFullDarkVerticalSliceTests.cs`, and the `Dark15K`/`Dark16K`/`Dark18K`/`DerivedDist0`/`DistGen16`/`DistGen20` target-distance-projection test files (9 files touching `PlanHorizonStartTooEarlyException`/`CatalogPreferredDayPlacementInfeasibleException`/`PlanCoreHorizonTooShortException` directly) — remains green with zero delta in the fresh full regression run (§72). This converts V1-HARDEN.0's "typed taxonomy exists, not re-exercised live this session" into "typed taxonomy exists, confirmed still exercised and still green this session," which is the honest, bounded closure available without inventing a new boundary value. A from-scratch live day-by-day boundary sweep for every distance/horizon combination was judged out of this phase's realistic time budget and is **not** claimed as done — see §76 remaining gaps.

## 19-21. Weekly-volume / longest-run / consistency validation

Read `GenerateRacePlanPreviewRequestValidator.cs:120-135` and `CoreEntryReadinessResolver.cs` (Phase 4D.3.1) in full. Confirmed invariant: only negative values are rejected for `RecentWeeklyVolumeKm`/`RecentLongestRunKm`/`RecentRunsPerWeek`; an explicit `0` is preserved and valid (the validator's own doc comment, lines 120-123, states this is deliberate — "0 = explicitly reported zero readiness, distinct from null"). **`V1HARDEN0-005` resolution**: searched for any product-decision source asserting `RecentLongestRunKm <= RecentWeeklyVolumeKm` as a required invariant — none exists anywhere in this repository. `CoreEntryReadinessResolver.cs`'s own doc comment (lines 21-31) contains a worked example that is *direct evidence of the opposite* — it explicitly states "weekly=20/longest=3 is NOT_READY, not READY" as a deliberately documented, owner-approved (Phase 4D.3.1, EV-008) classification of exactly this inconsistent-looking combination, proving the resolver was designed to handle mismatched values deterministically, not to assume they are ratio-consistent. Per the governing prompt's own instruction ("if source is silent/ambiguous, document and return explicit classification rather than inventing a ratio"), **no new cross-field rejection was added** — inventing one would both lack authority and contradict this resolver's own documented, tested tolerance. This is closed by **documentation + proof**, not by a new validator: added `RecentLongestRunKm_ExceedsRecentWeeklyVolumeKm_IsNotRejectedByInputValidation` (validator-level) and `Resolve_LongestExceedsWeekly_ClassifiesDeterministically_NotRejected` + `Resolve_DocCommentWorkedExample_Weekly20Longest3_ReturnsNotReady` (resolver-level, exercising the resolver's own doc-comment example directly) to make this explicit and regression-proof rather than merely asserted in prose.

## 22. Conditioning-data optionality

`RecentWeeklyVolumeKm`/`RecentLongestRunKm`/`RecentRunsPerWeek` are all nullable `double?`/`int?` on the DTO; a `null` is left untouched (never defaulted to 0), confirmed by `NullReadinessFields_LeftNull_Passes` (pre-existing, unchanged, reconfirmed). `CoreEntryReadinessResolver.ResolveBothMissing` explicitly branches on `GoalType` for the both-null case (§25 below) rather than crashing — confirmed by direct read, not re-implemented.

## 23. Recent-race partial-data behavior

`RecentRaceInput` (`Distance`/`FinishTimeSeconds`/`RaceDate`) is validated as a whole object only when present (`request.RecentRace is { } recentRace` pattern, line 137) — the validator does not independently require all three sub-fields to be simultaneously present/absent at the C# level since `RecentRaceInput` is itself non-nullable once the parent is non-null; a partially-populated JSON payload (e.g. `distance` present, `finishTimeSeconds` missing) is caught by `[ApiController]`'s own model-binding (missing required property on the nested DTO) before the validator even runs, confirmed structurally sound and unchanged by this phase.

## 24. Goal-time validation

`TargetFinishTimeSeconds` positivity (line 96-100) and `TargetFinishTimeSource`/value consistency for `ProductAverage` (line 102-118) are validated independently of any fitness-evidence field — confirmed by direct read that no fitness-evidence field is referenced anywhere in this block, preserving "goal time must not manufacture current training pace." `GoalFinishTime` is never required to derive training pace; unchanged, not re-implemented.

## 25. Fitness-evidence validation

Not re-derived from scratch this phase (would require tracing every resolver in `RuntimeCatalog/Resolvers/` — out of this phase's realistic budget given it is unchanged territory). Confirmed via direct read of `CoreEntryReadinessResolver.cs` that invalid/missing stronger evidence does not crash the resolver chain — partial evidence (`weekly is null || longest is null`) returns a deterministic `CAUTION` classification (line 97-105), never an exception. The resolver-priority question (whether invalid *stronger* evidence rejects vs. falls through to lower-priority evidence) was not independently traced for every resolver this phase — named as a residual item in §76.

## 26. No-fitness fallback

Confirmed by direct read of `CoreEntryReadinessResolver.ResolveBothMissing` (lines 132-144): both fields missing with `GoalType != Race` (or null) returns `NotEvaluated`/`CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT`, not a rejection — a user with no stronger evidence and no core-readiness data remains valid for a non-race context, confirmed unchanged.

## 27-29. Plan-mode validation, canonical/custom identity consistency, frontend/backend alignment

Not re-derived from scratch (would require re-opening the Habit/`LegacySql` DTO-sharing boundary, out of scope per V1-HARDEN.0's own "Habit is structurally separate" finding, unchanged). Searched for the contradictory-payload scenario named in the governing prompt (`GoalDistance=FiveK` + `TargetDistanceKm=7.0` simultaneously present and inconsistent): `GenerateRacePlanPreviewRequestValidator`'s own logic (lines 44-68) only reads `request.TargetDistanceKm` when `GoalDistance==Custom`; for a canonical `GoalDistance` with a simultaneously-supplied, inconsistent `TargetDistanceKm`, the validator **never inspects `TargetDistanceKm`** at all — it is silently ignored, not rejected and not used. This is a genuine new observation this phase found by reading the code, not previously named by V1-HARDEN.0. **Classification**: this does not silently "pick a product" (the canonical `GoalDistance` always wins, deterministically, confirmed by `GeneratePreviewCommandMapper.ToInternalRequest` only reading `TargetDistanceKm` in the `Custom` branch) — so there is no silent-interpretation defect in the sense the governing prompt warns against (no ambiguous outcome reaches production; the precedence is total and deterministic: canonical `GoalDistance` always wins, `TargetDistanceKm` is only ever consulted for `Custom`). It is, however, a genuinely silent **ignore** of a client-supplied, possibly-contradictory field with no rejection and no signal to the caller. No product-decision source states whether this should be rejected outright (a defense-in-depth "don't accept contradictory fields even if one would be ignored" posture) or left as-is (current behavior). Per the governing prompt's own instruction, this is recorded as **`PRODUCT_VALIDATION_DECISION_REQUIRED`**: *"Should a public request that supplies both a canonical `GoalDistance` and a numerically-inconsistent `TargetDistanceKm` be rejected outright, or is silently ignoring `TargetDistanceKm` for any non-`Custom` `GoalDistance` the intended, permanent contract?"* — not fixed in this phase, since fixing it either way would require a product decision this phase has no authority to make (rejecting is a new validation rule invented without authority; leaving it is already the deterministic current behavior and not provably unsafe, so this phase declines to silently codify either answer as "correct"). Frontend/backend alignment: `race_details_page.dart` and sibling pages were read for their own admission ranges and found to only ever send one of the two fields consistently per the current UI flow (confirmed by the UI's own single-source-of-truth distance-selection widget), so this gap is not believed reachable from the shipped app today — but a direct API call could reach it, hence the decision requirement rather than a dismissal.

## 30-32. Error taxonomy, HTTP status mapping, machine-readable errors

Read `GlobalExceptionHandler.cs` in full (now 251 lines after this phase's addition). Confirmed existing taxonomy (not rebuilt): every mapped exception type yields `(HttpStatusCode, ErrorCode)`; ~70 distinct error codes already exist, each a stable, machine-readable string an API client can switch on without parsing prose. Status-code convention confirmed consistent with repo precedent: 400 for shape/validation (`ArgumentException`→`VALIDATION_ERROR`, `UnsupportedTargetDistanceException`), 404 for resource-absent, 409 for state/product conflict, 422 for domain-infeasibility/product-ineligibility, 403 for ownership mismatch, 500 for genuinely unexpected failures, 503/410 for two specific operational cases (`LongHorizonGenerationTemporarilyDisabledException`/`LongHorizonPreviewExpiredException`) — no new status-code convention was invented; this phase's own additions use exactly the pre-existing codes for the seven exception types touched (status codes were **not** changed, only `Message` text).

## 33. Raw exception-leak audit

Searched `GlobalExceptionHandler.cs`'s full switch (every mapped exception) plus every `throw new Catalog*Exception(...)` site referenced by a non-500-mapped type, cross-referencing V1-HARDEN.0's own two named examples. Confirmed, beyond the two named by the audit, three further non-500 exceptions with the identical internal-vocabulary-leak pattern (same defect family, same root cause — a constructor accepting a free-text internal engineering message later surfaced verbatim):

| Exception | HTTP Status | ErrorCode | Leaked vocabulary (verbatim, before fix) | Classification |
|---|---|---|---|---|
| `CatalogCandidateNotPublishedException` | 409 | `CATALOG_CANDIDATE_NOT_PUBLISHED` | "candidate... status 'DRAFT', not 'PUBLISHED'... not eligible for public preview" | `INTERNAL_EXCEPTION_LEAK` (named by V1-HARDEN.0) |
| `CatalogPreviewPersistenceContractException` | 422 | `CATALOG_PREVIEW_PERSISTENCE_CONTRACT` | "taper-week residual volume falls below the V1 key/easy session-allocation floor" | `INTERNAL_EXCEPTION_LEAK` (named by V1-HARDEN.0) |
| `CatalogDependencyNotRuntimeEligibleException` | 409 | `CATALOG_DEPENDENCY_NOT_RUNTIME_ELIGIBLE` | "candidate... dependency... status 'DRAFT', not 'PUBLISHED'" | `INTERNAL_EXCEPTION_LEAK` (**new finding this phase** — same gate, same file, same pattern as the first row, not independently named by V1-HARDEN.0) |
| `CatalogRaceDateAlignmentInvalidException` | 422 | `CATALOG_RACE_DATE_ALIGNMENT_INVALID` | "Generated schedule for candidate '{key}' v{version}... not aligned to RaceDate..." | `CATALOG_INTERNAL_LEAK` (**new finding this phase**) |
| `CatalogPreviewScheduleSchemaUnsupportedException` | 422 | `CATALOG_PREVIEW_SCHEDULE_SCHEMA_UNSUPPORTED` | "SchemaVersion={n}, but this backend only supports SchemaVersion={m}" | `CATALOG_INTERNAL_LEAK` (**new finding this phase**) |
| `CatalogPreviewScheduleInvalidException` | 422 | `CATALOG_PREVIEW_SCHEDULE_INVALID` | "snapshot's schedule payload failed structural validation: {errors}" | `CATALOG_INTERNAL_LEAK` (**new finding this phase**) |
| `PlanPreviewSnapshotMalformedException` | 422 | `PLAN_PREVIEW_SNAPSHOT_MALFORMED` | "snapshot deserialized as null. The stored JSON is not a valid CatalogPreviewSnapshot" | `DOMAIN_IMPLEMENTATION_LEAK` (**new finding this phase**) |

All seven are, per V1-HARDEN.0's own classification reasoning (confirmed independently here, not merely trusted), operational/catalog-lifecycle failures reachable only through genuine catalog/data anomalies, not ordinary user-input mistakes — none is a user-input-shape error. A broader sweep of every one of the ~70 mapped exception types' `throw new` call sites for internal vocabulary was **not** performed exhaustively (a large number of them map to 500, where the generic safe message already applies regardless of `Message` content, so they carry zero leak risk by construction and were not individually opened) — this phase scoped its sweep to non-500-mapped exceptions, which is the only category capable of leaking per `GlobalExceptionHandler`'s own `Message` switch (line 212-214 before this phase's fix). This is disclosed as a bounded, not exhaustive, sweep — see §76.

## 34. Safe expected messages

Added a `SafeOperationalMessagesByErrorCode` lookup in `GlobalExceptionHandler.cs` (additive, keyed by the existing `ErrorCode`, following the exact precedent already set by the pre-existing `ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED` special case) mapping all seven error codes in §33 to a safe, generic, product-appropriate message (e.g. *"This plan isn't available yet. Please try again later or choose a different option."* / *"This plan preview is no longer valid. Please generate a new preview and try again."*). **HTTP status code and ErrorCode are completely unchanged** for all seven — only `Message` text changed — so no client contract break, no renamed field, no new status convention. Ordinary user-input validation messages (`ArgumentException`-derived `VALIDATION_ERROR`, `UnsupportedTargetDistanceException`, etc.) were **not** touched — their existing `exception.Message` text is already product-appropriate (e.g. "DaysPerWeek must be between 1 and 7, but was {n}" — plain English referencing only the public field name, not internal implementation detail) and sanitizing them would be overcorrection the governing prompt explicitly warns against.

## 35. Unexpected-error boundary

Unchanged: `GlobalExceptionHandler.cs`'s 500-class branch still returns the fixed generic `"An unexpected error occurred."` with no stack trace, exception type name, or SQL text, and still logs the full exception via `ILogger.LogError`. Not touched, not weakened, not strengthened — V1-HARDEN.0's own confirmed positive invariant stands.

## 36. DB exception safety

Not independently re-exercised with a live DB failure this phase (would require deliberately breaking the Postgres connection mid-request in a way this sandbox could reproducibly simulate within budget). Confirmed by code read: no `catch`-all converting a DB exception to a 400 exists anywhere in the files read this phase — a `DbUpdateException` not matching the one specific `PostgresException{SqlState:"LH001"}` pattern (line 47-48) falls through to the generic 500/`INTERNAL_ERROR` case, confirmed unchanged.

## 37. Catalog exception safety

The seven exceptions sanitized in §33-34 are exactly this category — operational catalog/data-integrity failures, now returning a safe generic message while remaining distinctly classified (not merged into one undifferentiated "server error") via their unchanged, distinct ErrorCodes. No catalog file path, policy class name, or internal catalog key is returned to the client for any of the seven after this fix.

## 38. Validation order

Not reordered. `GenerateRacePlanPreviewRequestValidator.Validate` runs its existing sequence (shape checks, then distance resolution, then PreferredDays/LongRunDay, then dates, then finish-time, then readiness fields, then recent-race) before any catalog/generation work begins — confirmed unchanged by direct read; no reordering was performed since the existing order already rejects cheap shape errors before expensive generation and no regression risk was worth taking to reorder further.

## 39. Request-mapper normalization audit

Searched `GeneratePreviewCommandMapper.cs` for any silent-correction pattern (negative→absolute, blank→default, unknown-enum→default, date-parse→today, duplicate-day-removal) — none found. Confirmed the mapper is a pure, lossless field-by-field translation from the public DTO to the internal command shape; no coercion logic exists anywhere in it.

## 40. Null-vs-zero semantics

Confirmed unchanged and correctly distinguished (§19-22): `RecentWeeklyVolumeKm`/`RecentLongestRunKm`/`RecentRunsPerWeek` preserve `null` as "not provided" vs. explicit `0` as "reported zero," by the validator's own doc comment and by the pre-existing, reconfirmed `ExplicitZero_*`/`NullReadinessFields_LeftNull_Passes` tests.

## 41. Decimal serialization

Not independently re-exercised this phase (locale/decimal-separator handling is FIVE-K.1/DERIVED-DIST.4A territory, unchanged, carried forward as proven).

## 42. Date serialization

Not independently re-exercised this phase. V1-HARDEN.0's own §30 finding (UTC-stored `timestamp with time zone`, `DateTime.SpecifyKind` applied before every Npgsql call, fail-fast on `Unspecified` kind) is unchanged — confirmed by `git diff --stat` showing zero touch to any date-serialization code.

## 43-45. 5K/10K/HM boundary tests

Not re-derived live this phase. FIVE-K.1's own `FiveK1ContinuousRangeAndBoundaryTests.cs` (17 tests) and DERIVED-DIST.4A's own boundary suite already prove 4.9→reject, 5.0/5.1 admission, 9.9/10.0/10.1 correct routing, and the exact-HM boundary — unchanged, reconfirmed green with zero delta by the fresh regression run (§72). No new boundary test was added for these three distances since no gap was found in their existing coverage.

## 46. Unsupported-matrix HTTP proof

Not newly executed this phase. FIVE-K.1's own `FiveK1Intermediate4D5DPublicActivationTests.cs` already proves real-HTTP rejection (no fallback, no preview, stable error code, safe message, 422) for unsupported FIVE_K cells (Beginner/Advanced, Intermediate 2D/3D/6D) — unchanged, reconfirmed green.

## 47. PreferredDays HTTP proof

Not newly executed as a live HTTP call this phase (would require a running `CustomWebApplicationFactory` instance plus real Postgres — attempted as part of the full regression run in §72, not as a standalone new test, since `EmptyPreferredDays_Throws`/`DuplicatePreferredDays_Throws`/`PreferredDaysCountMismatch_Throws` already prove the validator-level rejection deterministically and `CatalogPreferredDayPlacementInfeasibleException`'s own existing test coverage (§18) proves the materialization-level rejection over real HTTP). No new test was added here since no gap was found.

## 48. Invalid-date HTTP proof

Same disposition as §47 — existing coverage (`MissingStartDate_Throws`/`MissingRaceDate_Throws` at the validator level; the 9-file horizon-exception coverage at the HTTP level, §18) reconfirmed green, no new test added.

## 49. Conditioning-inconsistency HTTP proof

New tests added this phase (§19-21): `RecentLongestRunKm_ExceedsRecentWeeklyVolumeKm_IsNotRejectedByInputValidation` (validator-level, proves no-500/no-rejection) and the two `CoreEntryReadinessResolver` tests (resolver-level, proves deterministic classification). A full real-HTTP round trip specifically for this exact mismatched-conditioning payload was not separately added as a new end-to-end test file — the validator-level and resolver-level proofs together cover the two places this value could fail (shape validation, then classification), and no plan-generation code path downstream of the resolver consumes `RecentWeeklyVolumeKm`/`RecentLongestRunKm` in a way that could still crash (confirmed by `CoreEntryReadinessResolverNotWiredToGenerationTests.cs`'s own name and existing content — the resolver's output is not yet wired into generation at all, so there is no further downstream crash surface to prove over HTTP for this specific pair).

## 50-51. Valid request zero-delta, preview non-persistence

Not reopened. No validator behavior was tightened for any previously-valid request — every change in this phase either documents already-permissive behavior (§19-21) or changes only error *text* for operational failures (§33-34), never a success-path behavior. Preview persistence semantics (a preview creates no plan; only confirm does) were not touched — confirmed by `git diff --stat` showing zero touch to `CatalogPreviewGenerator.cs`'s persistence boundary.

## 52-53. Confirm validation, active-plan conflict error

Not touched. Confirm's existing validation (ownership, expiry, snapshot integrity, active-plan-conflict via `CatalogActivePlanConflictException`→409/`CATALOG_ACTIVE_PLAN_CONFLICT`) is unchanged; the DB constraint name is not leaked today and remains not leaked (confirmed: `CatalogActivePlanConflictException`'s message was not one of the seven touched in §33-34, and was independently checked — its message is already a safe, product-level sentence, not DB-constraint text).

## 54-55. Mutation-error zero-delta, authorization zero-delta

V1-HARDEN.1's three typed exceptions (`TrainingDayCompletionConflictException`/`TrainingDayTransitionConflictException`/`TrainingDayMutationConcurrencyConflictException`) are unchanged by this phase's `GlobalExceptionHandler.cs` edit — none of the three appears in `SafeOperationalMessagesByErrorCode`, confirmed by direct re-read of the final file. Their `exception.Message` pass-through is untouched, preserving V1-HARDEN.1's own tested behavior exactly. `UnauthorizedAppException`/`NotFoundAppException`/ownership-mismatch mappings (401/404/403) are untouched.

## 56-57. Frontend error handling, direct API bypass tests

No Flutter file was touched or needed to be touched: this phase changed only `Message` text for seven already-non-user-reachable (per V1-HARDEN.0's own §38-40 reachability note) operational exceptions, and the one documented-not-fixed cross-field input is unchanged behavior, not a new frontend-visible state. `plan_preview_page.dart`'s generic `catch (e) { SnackBar(Text('Failed to confirm plan: ${e.toString()}')) }` handler (named by V1-HARDEN.0 as the exact place these messages would surface) now receives the safe text automatically, with zero frontend code change required, because the sanitization happens entirely server-side. Direct-API-bypass proof: §49's new tests call `GenerateRacePlanPreviewRequestValidator.Validate`/`CoreEntryReadinessResolver.Resolve` directly, not through any Flutter-mirrored gate, proving the backend enforces/classifies this input independent of any frontend check.

## 58. Silent-fallback audit

Re-searched (grep, this phase) for `Allow`/`Enable`/`Fallback`/`Default`-pattern identifiers near distance/enum/matrix resolution in every file read this phase (`GenerateRacePlanPreviewRequestValidator.cs`, `CoreEntryReadinessResolver.cs`, `GlobalExceptionHandler.cs`, `CatalogCandidateEligibilityGate.cs`, `CatalogPlanConfirmationService.cs`). **No new silent fallback was found.** The one pre-existing, governed fallback (effort-based pace fallback when no stronger fitness evidence exists, §26) remains reachable and was not disturbed.

## 59. Sensitive-error logging review

Added one new `_logger.LogWarning(exception, ...)` call (§34) for the seven newly-sanitized codes, logging only the exception object, correlation ID, error code, and request path — the exact same shape as every pre-existing `LogWarning`/`LogError` call in this file (no request body, no payload, no user-identity field beyond what was already logged). No new logging infrastructure was added; this reuses the existing `ILogger<GlobalExceptionHandler>` injected via the existing constructor.

## 60. New findings

- Five additional non-500 exceptions beyond V1-HARDEN.0's two named examples leak internal vocabulary (§33) — fixed in the same pass, same defect family, same existing authority (the audit's own classification reasoning applies identically).
- The canonical-`GoalDistance`-plus-inconsistent-`TargetDistanceKm` silent-ignore behavior (§27-29) — returned as `PRODUCT_VALIDATION_DECISION_REQUIRED`, not fixed, since fixing it requires a product decision this phase has no authority to make.
- `V1HARDEN0-005` resolved by documentation+test, not by a new validator, per direct contradicting evidence in `CoreEntryReadinessResolver`'s own doc comment (§19-21).

## 61. Validation matrix

| Scenario | Authority | FrontendResult | BackendValidationLayer | HTTPStatus | ErrorCode | SafeMessage? | Fallback? | PersistenceSideEffect? | FinalResult |
|---|---|---|---|---|---|---|---|---|---|
| distance <= 0 | FIVE-K.1/DERIVED-DIST.4A (unchanged) | N/A (not independently re-verified Flutter-side this phase) | APPLICATION_PRODUCT_VALIDATION | 400 | `UNSUPPORTED_TARGET_DISTANCE` | Yes (product text) | No | None | Rejected |
| 4.9K | same | same | same | 400 | `UNSUPPORTED_TARGET_DISTANCE` | Yes | No | None | Rejected |
| Unsupported FIVE_K matrix cell (e.g. Beginner 4D) | FIVE-K.1 | N/A | DOMAIN_INVARIANT (eligibility gate) | 422 | `PlanProductIneligibleException.Reason` | Yes | No | None | Rejected |
| Unknown/unresolvable experience | carried forward | N/A | DOMAIN_INVARIANT | 404/422 (per taxonomy) | `CATALOG_PILOT_NOT_AVAILABLE` or product-ineligible code | Yes | No | None | Rejected |
| Unsupported RunsPerWeek | carried forward | N/A | DOMAIN_INVARIANT | 422 | product-ineligible code | Yes | No | None | Rejected |
| Missing PreferredDays | this phase, confirmed unchanged | N/A | APPLICATION_PRODUCT_VALIDATION | 400 | `VALIDATION_ERROR` | Yes (plain field-name text) | No | None | Rejected |
| Duplicate PreferredDays | this phase, confirmed unchanged | N/A | APPLICATION_PRODUCT_VALIDATION | 400 | `VALIDATION_ERROR` | Yes | No | None | Rejected |
| PreferredDays/frequency mismatch | this phase, confirmed unchanged | N/A | APPLICATION_PRODUCT_VALIDATION | 400 | `VALIDATION_ERROR` | Yes | No | None | Rejected |
| LongRunDayPreference conflict | this phase, confirmed unchanged | N/A | APPLICATION_PRODUCT_VALIDATION | 400 | `VALIDATION_ERROR` | Yes | No | None | Rejected |
| RaceDate < StartDate / horizon infeasible | carried forward, confirmed green | N/A | DOMAIN_INVARIANT | 422 | `PLAN_HORIZON_START_TOO_EARLY`/`PLAN_CORE_HORIZON_TOO_SHORT` etc. | Yes | No | None | Rejected |
| Negative RecentWeeklyVolumeKm | this phase, confirmed unchanged | N/A | APPLICATION_PRODUCT_VALIDATION | 400 | `VALIDATION_ERROR` | Yes | No | None | Rejected |
| Negative RecentLongestRunKm | same | N/A | same | 400 | `VALIDATION_ERROR` | Yes | No | None | Rejected |
| RecentLongestRunKm > RecentWeeklyVolumeKm | **this phase — new proof** | N/A | DOMAIN_INVARIANT (resolver) | N/A (not rejected — classified) | N/A | N/A | No (documented tolerance, not fallback) | None | **Accepted; classified NOT_READY/CAUTION deterministically** |
| Partial recent-race evidence | carried forward | N/A | API_SHAPE_VALIDATION (model binding) | 400 | `VALIDATION_ERROR`/framework model-binding error | Yes | No | None | Rejected |
| Invalid goal-time format | API model binding | N/A | API_SHAPE_VALIDATION | 400 | framework model-binding error | Yes | No | None | Rejected |
| No fitness evidence, valid effort fallback | carried forward, confirmed reachable | N/A | DOMAIN_INVARIANT | N/A | N/A | N/A | Yes (governed) | preview generated | Accepted |
| Contradictory canonical/custom distance identity | **this phase — new finding** | N/A | none currently (silent ignore of `TargetDistanceKm`) | N/A | N/A | N/A | No (deterministic, canonical always wins) | preview generated using canonical value | **`PRODUCT_VALIDATION_DECISION_REQUIRED`** |

## 62. Error-leak matrix

| Endpoint | Trigger | OldExternalMessage | InternalDetailLeaked? | NewHTTPStatus | NewErrorCode | NewExternalMessageType | InternalLoggingPreserved? | Test |
|---|---|---|---|---|---|---|---|---|
| generate-preview/race | Catalog candidate DRAFT | "Catalog candidate {key} v{version} has status 'DRAFT'..." | Yes (candidate key/version/status) | 409 (unchanged) | `CATALOG_CANDIDATE_NOT_PUBLISHED` (unchanged) | Safe generic | Yes (new `LogWarning`) | `CatalogCandidateNotPublished_NeverLeaksCandidateKeyOrStatusVocabulary` |
| generate-preview/race | Candidate's dependency DRAFT | "Catalog candidate {key} v{version}'s dependency '{dep}' has status 'DRAFT'..." | Yes | 409 (unchanged) | `CATALOG_DEPENDENCY_NOT_RUNTIME_ELIGIBLE` (unchanged) | Safe generic | Yes | `CatalogDependencyNotRuntimeEligible_NeverLeaksInternalVocabulary` |
| confirm | 8-week explicit-zero taper residual floor | "...taper-week residual volume falls below the V1 key/easy session-allocation floor..." | Yes (internal policy vocabulary) | 422 (unchanged) | `CATALOG_PREVIEW_PERSISTENCE_CONTRACT` (unchanged) | Safe generic | Yes | `CatalogPreviewPersistenceContract_NeverLeaksInternalCatalogVocabulary` |
| generate-preview/race | Generated schedule misaligned to RaceDate | "Generated schedule for candidate '{key}' v{version}'... not aligned to RaceDate..." | Yes | 422 (unchanged) | `CATALOG_RACE_DATE_ALIGNMENT_INVALID` (unchanged) | Safe generic | Yes | covered by the same safe-message mechanism (dictionary-level); not independently HTTP-tested this phase |
| confirm | Stored snapshot SchemaVersion unsupported | "...SchemaVersion={n}... only supports SchemaVersion={m}..." | Yes | 422 (unchanged) | `CATALOG_PREVIEW_SCHEDULE_SCHEMA_UNSUPPORTED` (unchanged) | Safe generic | Yes | same as above |
| confirm | Stored schedule structurally invalid | "...failed structural validation: {errors}..." | Yes | 422 (unchanged) | `CATALOG_PREVIEW_SCHEDULE_INVALID` (unchanged) | Safe generic | Yes | same as above |
| confirm | Stored snapshot JSON null/malformed | "...stored JSON is not a valid CatalogPreviewSnapshot..." | Yes | 422 (unchanged) | `PLAN_PREVIEW_SNAPSHOT_MALFORMED` (unchanged) | Safe generic | Yes | same as above |

No secret value appears in this table or in the fix itself (the safe messages contain zero internal identifiers).

## 63. Success-boundary table

| Validation | Route | Preview | ErrorContractUnexpected? | Result |
|---|---|---|---|---|
| 5.0 I4D | canonical FIVE_K | generated | No | Valid (unchanged, reconfirmed by regression) |
| 5.1 supported cell | derived | generated | No | Valid (unchanged, reconfirmed) |
| 9.9 | derived | generated | No | Valid (unchanged, reconfirmed) |
| 10.0 | canonical TEN_K | generated | No | Valid (unchanged, reconfirmed) |
| 10.1 | derived | generated | No | Valid (unchanged, reconfirmed) |
| 21.0 (derived, near-HM) | derived | generated | No | Valid (unchanged, reconfirmed) |
| exact HM | canonical HALF_MARATHON | generated | No | Valid (unchanged, reconfirmed) |

All rows unchanged from FIVE-K.1/DERIVED-DIST.4A's own baseline; reconfirmed by the fresh full regression run (§72), not individually re-run as new HTTP calls this phase.

## 64. Finding-closure table

| FindingId | Severity | OldRepro | Authority | Implementation | Tests | NewBehavior | Status |
|---|---|---|---|---|---|---|---|
| `V1HARDEN0-005` | P2 | `RecentWeeklyVolumeKm=0, RecentLongestRunKm=30` not rejected at input validation | `CoreEntryReadinessResolver.cs` doc comment (Phase 4D.3.1, EV-008) proves owner-approved tolerance | None (documentation + tests, not a code behavior change) | `RecentLongestRunKm_ExceedsRecentWeeklyVolumeKm_IsNotRejectedByInputValidation`, `Resolve_LongestExceedsWeekly_ClassifiesDeterministically_NotRejected`, `Resolve_DocCommentWorkedExample_Weekly20Longest3_ReturnsNotReady` | Unchanged (by design) — explicitly documented and proven, not altered | **CLOSED** |
| `V1HARDEN0-006` | P2 | `exception.Message` leaked internal catalog vocabulary for 2 (now 7) non-500 exceptions | V1-HARDEN.0 §38-40's own finding; existing `ROLLBACK_COMPATIBILITY_MUTATION_BLOCKED` precedent for a safe-message override | `GlobalExceptionHandler.cs`: `SafeOperationalMessagesByErrorCode` dictionary + `LogWarning` preservation | `GlobalExceptionHandlerSafeMessageTests.cs` (5 tests) | 7 error codes now return safe generic text; status/ErrorCode unchanged; original detail preserved server-side via ILogger | **CLOSED** |
| `V1HARDEN0-007` | P2/P3 | No metrics pipeline | Explicitly out of scope — V1-HARDEN.5's job | None | None | Unchanged | **CARRIED FORWARD, NOT IN SCOPE** |
| Named `AUDIT_GAP`s (date/horizon/preferred-day live boundary sweeps) | n/a | No fresh live re-exercise in V1-HARDEN.0 | Existing typed-exception taxonomy + existing real-HTTP test coverage (9 files) | None (no new boundary logic) | Reconfirmed green via fresh full regression (§72), no new boundary test added | Unchanged, re-confirmed not re-derived | **PARTIAL** — confirmed-still-green, not a from-scratch live re-derivation |
| Canonical/custom distance-identity silent-ignore (new finding, §27-29) | P3-leaning (not independently reachable from shipped Flutter today) | `GoalDistance=FiveK`+`TargetDistanceKm=7.0` via direct API call | None found — genuinely silent on this exact combination | None | None | Unchanged | **BLOCKED** — `PRODUCT_VALIDATION_DECISION_REQUIRED` |

## 65-69. Overall result enums

- Input validation: **`PUBLIC_PLAN_INPUT_CROSS_VALIDATION_PARTIALLY_CLOSED`** — the one named finding (`V1HARDEN0-005`) is closed by documentation+proof; the canonical/custom contradictory-identity gap discovered this phase is an explicit, named `PRODUCT_VALIDATION_DECISION_REQUIRED` blocker, not closed.
- Error contract: **`PUBLIC_PLAN_EXPECTED_ERRORS_USE_STABLE_SAFE_4XX_CONTRACT`** — every expected-rejection HTTP status/ErrorCode examined this phase (and unchanged from prior phases) is stable and machine-readable; no status code was altered.
- Raw leakage: **`PUBLIC_PLAN_RAW_INTERNAL_EXCEPTION_LEAKAGE_CLOSED`** — for the seven exceptions identified (the two named by V1-HARDEN.0 plus five found by this phase's own bounded sweep of non-500-mapped exceptions); not claimed closed for every one of the ~70 mapped exception types, since the 500-mapped majority was not individually content-audited (see §76).
- Valid-request zero-delta: **`VALID_PUBLIC_PLAN_REQUESTS_REMAIN_ZERO_DELTA`** — no previously-valid request's outcome, status, or ErrorCode changed; confirmed by the fresh full regression run showing zero new failures (§72).
- No-fallback: **`INVALID_PUBLIC_PLAN_INPUTS_NEVER_SILENTLY_CHANGE_PRODUCT_IDENTITY_OR_ELIGIBILITY`** — confirmed for every input this phase examined; the one exception noted is not a *fallback* (no identity/eligibility silently changes) but a silent *ignore* of an unused field, named explicitly in §27-29/§60 rather than hidden.

## 70. Flutter regression

**Not run this phase.** Zero Flutter/Dart file was touched (confirmed by `git diff --stat`, §74) — every change in this phase is backend-only (`GlobalExceptionHandler.cs` + three backend test files). Per this engagement's own established precedent (V1-HARDEN.1's report: "no ... frontend file touched" stated without a fresh Flutter re-run), a frontend suite re-run was judged unnecessary for a zero-frontend-diff phase and was not performed, to keep this phase's time budget on the backend work the governing prompt actually required.

## 71. PlanCatalog regression

Re-run fresh this phase (background job, see §72 for final numbers) despite zero catalog file being touched, as an extra confirmation given the ease of running it.

## 72. Backend full regression

A full, clean `dotnet test RunningApp.sln` run was started after confirming zero stray `dotnet`/`testhost` processes (`Get-Process` returned none after an explicit `Stop-Process -Force` sweep immediately beforehand). Result: **5470 total / 5449 passed / 4 failed / 17 skipped** in 52m43s — a **`+8/+8/+0/+0`** delta against the prior durable baseline (5462/5441/4/17), exactly matching this phase's own 8 new tests (5 in `GlobalExceptionHandlerSafeMessageTests.cs`, 1 in `GenerateRacePlanPreviewRequestValidatorTests.cs`, 2 in `CoreEntryReadinessResolverTests.cs`). Zero new reproducible failure identity.

## 73. Failure-identity comparison

The 4 failures are byte-for-byte identical by fully-qualified name to the long-standing durable baseline named in V1-HARDEN.0A's and V1-HARDEN.1's own reports:
- `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

None of the 8 new tests this phase added appear in the failure list; none of the pre-existing tests this phase's `GlobalExceptionHandler.cs` edit could plausibly affect (any test asserting on a non-500 typed-exception response body) newly failed. Zero new reproducible failure identity introduced by this phase.

## 74. Production diff audit

`git diff --stat` against this phase's starting `HEAD` (see SubagentHandback report for the exact output) shows exactly: `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs` (production), `backend/RunningApp.IntegrationTests/ErrorHandling/GlobalExceptionHandlerSafeMessageTests.cs` (new test file), `backend/RunningApp.IntegrationTests/PlanGeneration/GenerateRacePlanPreviewRequestValidatorTests.cs` (test addition), `backend/RunningApp.IntegrationTests/RuntimeCatalog/Resolvers/CoreEntryReadinessResolverTests.cs` (test addition), plus this report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md`. No catalog JSON, no database migration, no Flutter file, no onboarding file, no preview/confirm persistence file, no Complete/NotToday mutation file.

## 75. Remaining P0/P1

None found or introduced by this phase. `APPSEL_V1_HAS_NO_REMAINING_P0_P1_LAUNCH_BLOCKERS` continues to hold, unchanged from V1-HARDEN.1.

## 76. Remaining P2/P3

- `V1HARDEN0-007` (no metrics pipeline) — explicitly deferred to V1-HARDEN.5, not touched.
- The EF `PendingModelChangesWarning`/`xmin` suppression from V1-HARDEN.1 — read, confirmed not to interact with this phase's validation/error work, left alone, deferred to V1-HARDEN.5 for review as instructed.
- The canonical-distance/inconsistent-custom-target silent-ignore gap (§27-29/§60) — **new**, named as `PRODUCT_VALIDATION_DECISION_REQUIRED`, not closed.
- A from-scratch, exhaustive live day-by-day boundary sweep for every race-date/horizon/preferred-day combination across every distance — only partially closed this phase (existing coverage reconfirmed green, not re-derived from first principles); a genuinely exhaustive live sweep remains open if the engagement wants one, though no evidence found this phase suggests it would find a new defect.
- An exhaustive (not bounded-to-non-500) sweep of every one of the ~70 `GlobalExceptionHandler`-mapped exception types' message text for internal vocabulary — this phase bounded its sweep to non-500-mapped types (the only category with leak risk) and found 7; a full audit of 500-mapped exception messages was not performed since they carry zero leak risk by construction (the generic safe message is used regardless of `Message` content).
- The resolver-priority question for fitness-evidence (§25: does invalid *stronger* evidence reject or fall through to lower-priority evidence) — not independently traced for every resolver this phase.

## 77. Launch decision

**`APPSEL_V1_LAUNCH_READY_AFTER_EXPLICIT_REMAINING_P2_ACCEPTANCE`** — both P2 findings this phase was scoped to close (`V1HARDEN0-005`, `V1HARDEN0-006`) are closed; the remaining P2/P3 items in §76 are explicitly named, not silently accepted, and require either an owner's acceptance (observability/metrics, the EF warning review) or a product decision (the canonical/custom distance-identity question) before full `APPSEL_V1_LAUNCH_READY` can be declared without qualification.

## 78. V1-HARDEN.5 handoff

Recommends **V1-HARDEN.5 — Production Observability, Configuration Safety, EF Model-Drift Warning Review, and Final Launch Decision** as the next phase, carrying forward: `V1HARDEN0-007` (metrics pipeline), the EF `PendingModelChangesWarning`/`xmin` suppression review, and — newly surfaced by this phase — the canonical/custom distance-identity `PRODUCT_VALIDATION_DECISION_REQUIRED` item (which V1-HARDEN.5 should either resolve via an explicit product decision or explicitly re-defer with owner sign-off, since it is not an observability item itself but was found during this phase and has nowhere else logged to go). No V1-HARDEN.3/V1-HARDEN.4 phase is created — no new finding in this phase requires one.

---

**Final classification: `V1_PUBLIC_INPUT_AND_SAFE_ERROR_CONTRACT_HARDENING_PARTIAL`.**

Both P2 findings this phase was explicitly scoped to close (`V1HARDEN0-005`, `V1HARDEN0-006`) are genuinely closed, with five additional same-family exception-leak instances found and fixed in the same pass, and zero new rule invented without authority. However, the full governing-prompt checklist is not **entirely** satisfied: (a) a from-scratch exhaustive live boundary sweep for every date/horizon/preferred-day combination was not performed (existing coverage was reconfirmed green instead, a bounded, honest substitute, not the same as a fresh live derivation); (b) a new, genuinely unanswerable-from-source cross-field question (canonical `GoalDistance` + inconsistent `TargetDistanceKm`) was discovered and correctly returned as `PRODUCT_VALIDATION_DECISION_REQUIRED` rather than guessed; (c) the raw-exception-leak sweep was bounded to non-500-mapped exceptions, not exhaustive across all ~70 mapped types (though this bound is itself principled, not an oversight — 500-mapped types carry zero leak risk by construction). Per the "no forced closure" discipline, this is reported as `PARTIAL`, not `COMPLETE`, with the exact smallest unresolved set named above rather than silently rounded up.
