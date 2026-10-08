# PHASE V1-HARDEN.0 — End-to-End Launch-Readiness Audit, Public Contract Verification, Failure-Mode Mapping, Data-Integrity Review, Observability Review, and Blocker Prioritization

## 1. Binding V1 state

Consumed as frozen, binding authority (read in full before any audit work): `PHASE_FIVE_K_1_...md` and `PHASE_DERIVED_DIST_4A_...md` in full; `PHASE_DERIVED_DIST_2/3/4` and `PHASE_FIVE_K_0/0A/0B` consulted by targeted grep for idempotency/transaction/immutability/authorization/dark-switch content (no contradicting findings surfaced). Current production routing, re-verified by independent source read, matches FIVE-K.1 §51-54 exactly: exact 5K → canonical `FIVE_K` (Intermediate×{4,5} only); `5K<target<10K` → TEN_K-derived; exact 10K → canonical `TEN_K`; `10K<target<HM` → HALF_MARATHON-derived; exact HM → canonical `HALF_MARATHON`; `target>HM` and `0<target<5K` unsupported. **`git diff --stat` between `a0d50f2` (FIVE-K.1's own implementation commit) and this phase's starting `HEAD` (`8250289`, a SHA-backfill docs-only commit) touches zero files under `backend/`, `mobile/`, or `plan-catalog/`** — independently confirmed via `git diff --stat a0d50f2 HEAD -- backend mobile plan-catalog` returning no output. This means FIVE-K.1's own final regression numbers (PlanCatalog.Tests 1510/1510; full `RunningApp.IntegrationTests` 5447/5426/4/17) describe the exact code this audit examines, not a stale prior state.

## 2. Launch scope

This phase touches API controllers, EF/DB configuration, auth wiring, frontend onboarding flow, logging, and configuration — not the distance engine. Per the governing prompt, the distance engine is frozen; no exact-cell policy, derived-routing, or FIVE_K-matrix change was proposed or made anywhere in this phase.

## 3. Current public matrix (source-derived)

Re-verified directly against `V1CatalogPilotIdentityPolicy.cs`, `DerivedDistanceEligibilityPolicy.cs`, and `NearestHigherCanonicalPlanResolver.cs` (code read, not re-run): matches FIVE-K.1 §54/§51 exactly. Summary (full detail in §74's public-scope table): FIVE_K exact — Intermediate×{4,5}; `5K<target<10K` and `10K<target<HM` derived — Intermediate×{3,4,5} (gate A ∩ gate B); TEN_K exact — Beginner{2,3,4}/Intermediate{2,3,4,5,6}/Advanced{3,4,5,6}; HALF_MARATHON exact — Beginner{3,4}/Intermediate{3,4,5,6}/Advanced{3,4,5,6}. No new combination was added or removed by this audit.

## 4. Frontend-reachable matrix — **the phase's central finding**

This is the one place this audit diverges sharply from every prior phase's own frontend-reachability claim, and it is the headline finding of the whole phase (see `V1HARDEN0-001` in §81).

Direct read of `mobile/lib/features/onboarding/presentation/plan_generation_page.dart` (lines 66-117, current `HEAD`) shows the onboarding-completion screen's `_startGeneration()` method contains the **real** implementation fully written out as a comment block (`await ref.read(onboardingProvider.notifier).generatePreview(); ... context.pushReplacement(isLongHorizon ? ... : AppRoutes.planPreview);`) immediately followed by the **actual, uncommented, executing code**, which unconditionally does this instead:

```dart
if (mounted) {
  setState(() => _completedSteps = 4);
  await Future.delayed(const Duration(milliseconds: 300));
  if (mounted) {
    ref.read(useMockHomeDataProvider.notifier).state = true;
    context.go(AppRoutes.home);
  }
}
```

There is no `kDebugMode`/flavor/environment/feature-flag guard around this branch — it is the only code path, for every build, every user, every onboarding completion. `useMockHomeDataProvider` (`mobile/lib/features/home/data/home_provider.dart:13`) defaults to `false` and is flipped to `true` only by this one call site; `ActiveHomeDispatcherPage` (`mobile/lib/features/home/presentation/active_home_dispatcher_page.dart:19-24`) then renders the static mock `HomePage` whenever that flag is `true`, never calling `GET /plans/active/home`.

**Consequence**: `OnboardingNotifier.generatePreview()` (`mobile/lib/features/onboarding/data/onboarding_provider.dart:321`) — a fully real, validated method that throws if `selectedRunningDays`/`startDate`/`raceDate`/`targetFinishTimeSeconds` are missing — is defined, well-formed, and has **zero live call sites** anywhere in the production widget tree (confirmed by `grep -rn "generatePreview\b" mobile/lib`: the only non-comment, non-definition hit is the method definition itself). `PlanPreviewPage._onConfirm` (`mobile/lib/features/onboarding/presentation/plan_preview_page.dart:40-77`) does call the real `planRepositoryProvider.confirmPlan(previewId)` correctly — but `PlanPreviewPage` itself is never navigated to from the real onboarding flow, because `PlanGenerationPage` never routes there (it calls `context.go(AppRoutes.home)` directly instead of `context.pushReplacement(AppRoutes.planPreview)`).

`git log --oneline -- mobile/lib/features/onboarding/presentation/plan_generation_page.dart` shows this file's most recent touching commit is `1b07cf8` ("snapshot: ... as of 2026-08-14"), **predating the entire DERIVED-DIST/FIVE-K chain**. This means every prior phase's "`Yes (preset)`" / "`Yes (custom)`" frontend-reachability claim (DERIVED-DIST.4A §53/§54, FIVE-K.1 §53) was proven by calling the backend API directly over real HTTP (real-HTTP integration tests) or by reading only `race_details_page.dart`'s input-validation logic in isolation — never by exercising the actual onboarding→Home widget route a real device user follows. Both claims are true and reproducible for what they tested; neither tested the thing this phase tests (the shipped app's actual navigation graph), which is exactly why a holistic launch audit — rather than another distance-correctness phase — was needed to surface this.

**Classification, per §4's three-way distinction**:

| Layer | Status |
|---|---|
| TECHNICALLY_GENERATABLE | Yes — proven exhaustively by DERIVED-DIST/FIVE-K's own real-HTTP+Postgres tests |
| PUBLIC_BACKEND_ELIGIBLE | Yes — `V1CatalogPilotIdentityPolicy`/`DerivedDistanceEligibilityPolicy` admit the matrix in §3 |
| USER_REACHABLE_FROM_CURRENT_FRONTEND | **NO, for the entire race-plan preview/confirm lifecycle, for every distance/level/frequency combination, with no exception** — the onboarding-completion screen never calls the real backend; every real user who finishes onboarding today lands on a hardcoded mock Home screen built from `mock_home_data.dart`, never a real generated plan, and `ConfirmPlan` is never invoked from the shipped navigation graph. |

This is `V1HARDEN0-001`, P0, detailed with full reproduction in §81.

## 5. Plan modes

Three plan modes exist in source: `GoalType.Race` (catalog-generated, the subject of this audit), `GoalType.Habit` (routes through `GenerationSource.LegacySql`, confirmed structurally separate — FIVE-K.1 §43 proves a Habit+FiveK request never returns the canonical Race `FIVE_K__*` identity), and `PlanScheduleStrategy.RollingLongHorizon` (a server-owned long-horizon race mode with its own controller actions, confirmed present in `PlansController`/`TrainingDaysController` — `generate-preview/race/long-horizon`, `confirm/long-horizon`, `activate-next-window`, `retry`, plus rolling-session `complete`/`not-today`). `V1_LAUNCH_SCOPE` for this audit is the standard (non-rolling) Core race flow, since that is what the FIVE-K.1/DERIVED-DIST chain actually built and what §4's finding blocks; the Long-Horizon rolling mode and the Habit/LegacySql mode are out of this audit's primary scope but were confirmed structurally separate (no shared mutable state in the request paths read) and are named as an explicit **audit gap** (not independently re-verified end-to-end in this session — see §AUDIT_GAPS).

## 6. Public contract inventory

Read `GenerateRacePlanPreviewRequest`/`GeneratePreviewCommandMapper.cs`/`GenerateRacePlanPreviewRequestValidator.cs`. Fields: `GoalType`, `GoalDistance` (enum incl. `Custom`), `TargetDistanceKmOverride` (custom-only), `RunningBackground`/Level, `DaysPerWeek`, `StartDate`, `RaceDate` (race-mode required), `PreferredDays`, `LongRunDay`, `RecentWeeklyVolumeKm`, `RecentLongestRunKm`, `TargetFinishTimeSeconds`+`TargetFinishTimeSource`, plus fitness-evidence fields (recent race / time trial / structured test / normal pace). Each is validated server-side in `GenerateRacePlanPreviewRequestValidator`/`GoalFeasibilityResolver` before reaching generation — confirmed by direct read, not inferred. Given §4's finding, this entire contract is currently reachable **only via direct API call (Swagger/Postman/automated test), not via the shipped Flutter app's main flow** for plan generation specifically; the onboarding screens that *collect* these fields (race name, distance, level, days, dates) are reachable and functional up through `race_details_page.dart`, but the collected `onboardingProvider` state is discarded rather than submitted.

## 7. Contract duplication

`race_details_page.dart`'s `_isSupportedCustomDistance` (5.0 < x < 21.0975) is an `INTENTIONAL_UI_MIRROR` of the backend's real derived-distance range — confirmed by its own doc comment (lines 44-59) explicitly disclaiming any Level×Frequency authority and deferring to the backend as sole authority. No `DANGEROUS_DUPLICATED_AUTHORITY` or `STALE_RULE` instance was found in the files read (distance bound, frequency matrix, and preferred-day rules are each enforced exactly once, server-side, in `V1CatalogPilotIdentityPolicy`/`DerivedDistanceEligibilityPolicy`/validators). This finding is unaffected by §4 — the duplication question is about rule *correctness*, not reachability.

## 8. Frontend→backend contract consistency

Not independently re-walked field-by-field against the live request path in this session beyond §6/§7, because §4 makes the question moot for the shipped app's main flow (no field ever reaches the backend through it). Marked as an **audit gap** for the onboarding-collection pages' own internal DTO mapping (`onboarding_provider.dart` state → `GenerateRacePlanPreviewRequest`), since that mapping is dead code today but would need re-auditing the moment §4 is fixed and the real call is restored.

## 9. Silent-fallback audit

Searched the generation/confirmation pipeline for fallback-on-invalid-input behavior. Found only governed, disclosed fallbacks: `RACE_SPECIFIC_REHEARSAL_CURRENT_FITNESS_FALLBACK`/`TAPER_FIVE_K_ACTIVATION_EFFORT_FALLBACK` (FIVE-K.1 §13, an approved effort-pace fallback within the existing fitness-evidence hierarchy) and the shared recent-race→time-trial→structured-test→normal-pace→effort-fallback hierarchy reused unchanged across all distances (FIVE-K.1 §20). `GeneratePreviewCommandMapper.ToInternalRequest` only invokes custom/derived routing when `GoalDistance==Custom` (FIVE-K.1 §33) — no distance/level/frequency identity silently changes. `UnsupportedGoalDistanceException` (`GlobalExceptionHandler.cs:58`) is explicitly a **fail-closed guard**, never a fallback-to-5K path, per its own doc comment. No unapproved product-identity fallback was found. Result: **no new silent-fallback defect found**, consistent with prior phases' own findings.

## 10. Preview idempotency

Repeated identical preview requests: `GeneratePreviewAsync` allocates a new `PreviewId`/`PlanPreview` row and a 30-minute `ExpiresAt` window each call (`PlanServices.cs:523`), so it is **not** byte-identically idempotent at the storage level (each call creates a new preview row) but *is* semantically idempotent — the generated schedule content for identical inputs against an unchanged catalog is deterministic (the whole DERIVED-DIST/FIVE-K chain's own golden-fixture tests rely on this). It allocates no active-plan state, no catalog mutation, and no `TrainingPlan`/`TrainingDay` rows — confirmed by reading `CatalogPreviewGenerator`/`PlanServices.GenerateRacePlanPreviewAsync`, which only write to `PlanPreviews`. **Result: `PREVIEW_IDEMPOTENT`** (semantically; each call is a fresh, independent, side-effect-free-on-plan-state preview row with its own 30-minute lease).

## 11. Confirm double-submit

Read `CatalogPlanConfirmationService.ConfirmPlanAsync` (lines 380-554) in full. Two independent guards exist: (a) an application-level pre-check (`existingActivePlan` read) that returns the existing plan with `AlreadyActive=true` for the common case; (b) the real concurrency guarantee is a **database-level unique partial index**, confirmed in `AppDbContext.cs:297-299` (`IX_TrainingPlans_InternalUserId_ActiveOnly`, `HasFilter("\"Status\" = 'active'")`) plus a second unique partial index on `SourcePreviewId` (`AppDbContext.cs:307-309`, `HasFilter("\"SourcePreviewId\" IS NOT NULL")`). On a `DbUpdateException`, `ClassifyUniqueViolation` (lines 808-832) distinguishes a same-preview race (reloads and returns the winning plan — true idempotent recovery) from a different-preview race for the same user (throws typed `CatalogActivePlanConflictException`, 409, never silently returns someone else's plan). **Result: `CONFIRM_IDEMPOTENT`** for same-preview double-submit (reload-and-return), **`CONFIRM_DUPLICATE_GUARD_EXISTS`** (DB-level, not merely app-level) for the general case. This is a genuinely strong, DB-enforced invariant, not a frontend-button-disable-only guard — recorded as a positive invariant in §72.

## 12. Active-plan uniqueness

Explicit invariant, enforced at the database layer (not merely application code): exactly one `Status='active'` `TrainingPlans` row per `InternalUserId`, via the partial unique index named above. **Result: invariant is real and DB-enforced; multiple unintended active plans are not possible** even under concurrent confirms. Recorded as a positive invariant.

## 13. Preview→confirm consistency

`ConfirmPlanAsync` persists directly from the stored `CatalogPreviewSnapshot` (`snapshot.GeneratedPreviewPlanPayload`) associated with the `previewId` — it does not re-invoke generation or re-resolve catalog state from the raw request inputs. Confirmed by direct read: `BuildCatalogTrainingPlan`/`BuildCatalogTrainingWeek`/`BuildCatalogTrainingDay` all take `snapshot`/`weekPayload`/`session` as their only data sources. **Result: `PREVIEW_CONFIRM_ARTIFACT_STABLE`** — no regeneration risk between preview and confirm.

## 14. Catalog-version behavior

`CatalogPreviewSnapshot` carries `CandidateKey`/`CandidateVersion`/`ContentHash` (persisted into the `CatalogPlanConfirmed` `PlanEvent`'s payload, `CatalogPlanConfirmationService.cs:441-445`), and `CatalogPreviewSnapshotVerifier` exists specifically to detect snapshot drift (`CatalogPreviewSnapshotVerifier.cs:45,77`). Because confirm persists from the snapshot (§13) rather than re-resolving the catalog, a catalog change between preview and confirm cannot silently alter the confirmed plan's content — the snapshot is the frozen artifact. A catalog-version check does exist (`ICatalogCandidateEligibilityGate.LoadForPublicPreviewAsync`, requires `PUBLISHED` status) but it gates preview generation, not confirm; confirm trusts the already-captured snapshot. **Result: catalog-version pinning is real at the snapshot level; a catalog change after preview generation cannot retroactively change what confirm persists.**

## 15. Persisted-plan immutability

Not independently re-proven this session via a live test-seam catalog mutation (no production file was touched, per §87's constraint, and doing so live against the real catalog root was judged unnecessary given the structural evidence): confirm persists a materialized `TrainingPlan`/`TrainingWeek`/`TrainingDay` row set (concrete columns: `PlannedDistanceKm`, `PlannedPaceMinKm`, `Title`, `Description`, etc. — confirmed in `QueryAndMutationServices.MapToResponse`/`GetCalendarAsync`'s raw `Select` projection), and every read path (`GetHomeAsync`, `GetCalendarAsync`, `GetTrainingDayDetailAsync`, `GetActivePlanDetailsAsync`) reads these persisted columns directly, never re-deriving them from the live catalog at read time. **Result: structurally `CONFIRMED_PLAN_IMMUTABLE_FROM_FUTURE_CATALOG_CHANGES`** by construction (reads never touch the catalog), but this is an **audit gap** in the sense that no live "mutate the catalog, re-read the old plan" test-seam proof was executed in this session — classified as `AUDIT_GAP` for direct empirical confirmation, `STRUCTURALLY_PROVEN` for the code-path evidence.

## 16. Workout-version immutability

Follows directly from §15: `TrainingDay` rows store concrete planned values (distance/pace/duration/title/description), not a workout-key reference resolved at read time — confirmed by the DTO projection in `GetCalendarAsync` reading columns, not re-resolving `WorkoutKey`→catalog. **Result: historical plan semantics cannot change if the workout catalog changes later** — same structural proof, same audit-gap caveat as §15.

## 17. Cancellation

`CancelPlanAsync` (`PlanServices.cs:956-995`) is scoped by `planId + InternalUserId + Status==Active` — cancelling an already-cancelled or nonexistent plan throws `NotFoundAppException` (404), it does not silently no-op or re-cancel. `TrainingDay`/`TrainingWeek` rows are never deleted on cancel (only `TrainingPlans.Status`/`CancelledAt` change) — historical readability is preserved by construction, though no dedicated "read a cancelled plan's history" endpoint was found in `PlansController` (only `active/*` endpoints exist) — this is named explicitly in §66/§84 as a product gap, not a data-integrity defect. After cancel, the DB partial-unique-index slot frees (filtered on `Status='active'`), so a new plan can be created immediately — confirmed structurally, not via a live test this session. A cancelled plan cannot "accidentally reactivate" — no code path sets `Status` back to `Active` outside confirm, which always creates a new plan row.

## 18. Replacement/new-plan behavior

Follows from §12/§17: a user with an active plan cannot confirm a second one (blocked by the unique index — `AlreadyActive=true` returned, not an error, for the identical-preview case, or `CatalogActivePlanConflictException` for a different preview); a user with only a cancelled/completed plan has no active-plan row and can confirm freely. No explicit "replace" endpoint exists — replacement is cancel-then-confirm-new, a two-step flow. **Not independently exercised live this session** — classified `STRUCTURALLY_PROVEN`, not `AUDIT_GAP` (the DB constraint alone is sufficient evidence for the uniqueness half; the "can create after cancel" half rests on the filtered-index semantics, which is sound Postgres behavior, not an assumption).

## 19. Complete idempotency — **genuine finding**

Read `CompleteWorkoutAsync` (`QueryAndMutationServices.cs:388-451`) in full. It does **not** check `day.Status` before proceeding. A second identical `Complete` call for the same `trainingDayId`:
- Re-sets `Status=Completed`, `ActualDistanceKm`/`ActualDurationMin`/`CompletedAt`/`UpdatedAt` to the new call's values (silently overwriting the first call's recorded actuals and timestamp — no 409/422, no rejection).
- Appends a **new** `WorkoutLog` row (line 408-420) and a **new** `PlanEvent` row (line 432-442) each call — these accumulate without bound; a user who taps Complete three times from a slow/retried network call gets three `WorkoutLog` rows and three `PlanEvent` rows for one real workout.
- `week.ActualVolumeKm` recomputation (line 424-429) explicitly excludes the current day (`d.Id != day.Id`) before adding the new call's `ActualDistanceKm`, so the **weekly volume total itself stays correct** on replay — this one side effect is genuinely idempotent.

**Result: `AMBIGUOUS`/not idempotent at the audit-log layer, idempotent-in-effect for the single most user-visible field (`week.ActualVolumeKm`, `day.Status`).** No 500s, no data corruption of the active-plan invariant, but duplicate `WorkoutLog`/`PlanEvent` rows are a real, reproducible data-integrity defect (`V1HARDEN0-002`, §81/§82).

## 20. Not-Today idempotency — **genuine finding**

`CreateNotTodayDecisionAsync` (`QueryAndMutationServices.cs:454-487`) also does not check `day.Status`/`CanMarkNotToday` before creating a new `NotTodayDecision` row with `Status=Pending`. A repeated call creates a second, third, etc. `Pending` decision row for the same `TrainingDayId`, each independently resolvable later via `ConfirmNotTodayDecisionAsync`/`ResolvePendingConfirmationAsync` (both of which mutate the same underlying `TrainingDay` idempotently in terms of final status, but each pending decision shows up in `GetPendingConfirmationsAsync`'s list, meaning the Home screen's "pending confirmations" count could show duplicates for one real not-today tap). **Result: same classification as §19** — `V1HARDEN0-003`, P2/P3-leaning (no active-plan corruption, but a duplicate-pending-confirmation UX/data defect), §81/§82.

## 21. Status-transition matrix

See §75 (mutation matrix table). Built from direct code read, not live exercise: Complete→Complete is `UNSAFE` (overwrites actuals, duplicates logs, §19); Complete→NotToday and NotToday→Complete are each individually valid-looking (no explicit guard preventing a day marked Completed from later receiving a NotToday decision, or vice versa — `CreateNotTodayDecisionAsync` has no status check at all) — this is an additional facet of `V1HARDEN0-003`: a day already `Completed` can still receive a new `NotToday` pending decision, which once confirmed sets `Status=Missed`, silently reverting a completed day to missed. This is flagged as `V1HARDEN0-004`, P1 (it can make a genuinely completed workout disappear from the user's completed-count), in §81.

## 22-23. Calendar invariants / StartDate edges

`GetCalendarAsync`/`GetHomeAsync` both derive week boundaries from the persisted `TrainingWeek.StartDate` (never a Monday-anchored calendar week) and explicitly handle the legacy no-`TrainingWeek`-rows fallback with its own Sunday-aware Monday-anchor arithmetic (`QueryAndMutationServices.cs:140-144`). Calendar month materialization (`GetCalendarAsync`) iterates `startOfMonth`..`endOfMonth` inclusive and fills every day (scheduled or rest), so month-length/leap-year/year-boundary differences are absorbed by plain `DateTime` arithmetic, not by any distance-specific logic — no month-boundary-specific code was found that could misbehave at Dec31/Jan1 or a leap Feb. **Not independently exercised with a live Dec31/Jan1/leap-year request in this session — classified as an `AUDIT_GAP`** (the code path is generic `DateTime` arithmetic with no distance/week-count special-casing visible, which is reassuring but not the same as a reproduced test run). True DST-boundary behavior is explicitly **not testable** in this Windows sandbox within this phase's time budget and is named as a permanent, honest `AUDIT_GAP` (all date columns are stored `timestamp with time zone`/UTC per `QueryAndMutationServices.cs:241-243`'s own comment, which structurally avoids most DST ambiguity, but was not empirically re-verified).

## 24-25. RaceDate / horizon mismatch

Not independently re-derived this session; DERIVED-DIST/FIVE-K's own `RaceHorizonPolicy`/`PlanHorizonStartTooEarlyException`/`PlanCoreHorizonTooShortException`/`PlanCoreHorizonUnsupportedException` (all present in `GlobalExceptionHandler.cs:123-129`) show the backend fails closed with typed 422s for infeasible horizons rather than silently truncating or extending — confirmed by the exception taxonomy existing and being wired to specific, non-generic error codes. Classified `STRUCTURALLY_PROVEN` from the taxonomy; full live boundary sweep (exact earliest/latest feasible race date per distance) is an **`AUDIT_GAP`** for this session (prior phases' own tests already cover this ground per their reports, not re-run here).

## 26-27. PreferredDays / LongRunDayPreference

`CatalogPreferredDayPlacementInfeasibleException` (`GlobalExceptionHandler.cs:126-127`) confirms a typed, non-500 rejection path exists for infeasible preferred-day placement. Not independently re-exercised live this session (too-few/too-many/duplicate days) — **`AUDIT_GAP`**, deferring to the existing typed-exception taxonomy as structural evidence that *some* validation exists, without re-proving its exact boundary behavior.

## 28-29. Hard-day safety

FIVE-K.1 §12 already proves, via both a unit-level and a real-HTTP test, that the FIVE_K 5D calendar never classifies the fifth (EASY) session as hard or creates a second quality day (`day_type` never shows two quality-type days in one week). This audit did not re-run that test live but confirms its existence and scope by reading the FIVE-K.1 report directly (binding, not re-derived). No change to this mechanism occurred since (§1). Treated as `CARRIED_FORWARD_PROVEN`, not re-tested fresh.

## 30. Date/time storage

Confirmed by direct code comment and usage: `QueryAndMutationServices.cs:241-243` — "All Date columns are stored as UTC (`timestamp with time zone`)"; `DateTime.SpecifyKind(startOfMonth, DateTimeKind.Utc)` is applied explicitly before any Npgsql query because `DateTime.TryParse` produces `Kind=Unspecified`, which Npgsql rejects outright (a fail-fast behavior, not a silent off-by-one). This is a positive invariant: Npgsql's own strictness here prevents the classic "Unspecified kind silently treated as local" timezone bug class structurally, confirmed by the comment disclosing exactly why the `SpecifyKind` call exists.

## 31-32. Numeric boundaries / distance precision

Confirmed from FIVE-K.1/DERIVED-DIST.4A's own tests (not re-run live): exact target identity is preserved end-to-end for 5.1/7.5/9.9/17.7/20.5-style decimals with no rounding to the nearest whole km (DERIVED-DIST.4A §48-49, FIVE-K.1 §34/§42). Zero/negative/`4.9` rejection confirmed by FIVE-K.1 §35 (typed `UNSUPPORTED_TARGET_DISTANCE`, never 500). Non-numeric frontend input handling confirmed by DERIVED-DIST.4A §29-30 (`double.tryParse` null-check fix). **Carried forward as proven, not re-executed in this session.**

## 33. Volume/longest-run consistency

Searched `GenerateRacePlanPreviewRequestValidator.cs` and `GoalFeasibilityResolver` for a cross-field check between `RecentLongestRunKm` and `RecentWeeklyVolumeKm` (e.g. rejecting longest-run > weekly-volume, or weekly-volume=0 with a nonzero longest-run). **No such cross-field validation was found** in the validator file read. This is a genuine gap: a request with `RecentWeeklyVolumeKm=0, RecentLongestRunKm=30` would not be rejected at the input-validation layer; its downstream behavior depends entirely on how `GoalFeasibilityResolver`'s fitness-evidence hierarchy weighs the two fields, which was not independently traced line-by-line in this session. **Classification: `PRODUCT_FOLLOW_UP`, not a P0/P1 launch blocker** — no evidence this produces a crash or a materially unsafe plan (the volume-safety stack in §VolumeSafetyPolicy still bounds growth/long-run share independent of input consistency), but it is a real missing input-sanity check, recorded as `V1HARDEN0-005`, P2.

## 34-36. Fitness-evidence paths / no-fitness fallback / goal-time behavior

Carried forward from FIVE-K.1 §20 ("the shared fitness-evidence hierarchy... is reused completely unchanged; no FIVE_K-specific override exists") and DERIVED-DIST.4A's own target-pace-binding tests. Not independently re-exercised with a live "user with none of the stronger signals" request in this session. **`AUDIT_GAP`** for a fresh empirical proof of the effort-fallback branch under this exact phase; existing evidence (prior phases' own passing tests, re-confirmed not to have regressed per §1's zero-diff finding) is treated as sufficient to avoid re-deriving from scratch, per the governing prompt's own time-budget guidance.

## 37. Unsupported combinations

FIVE-K.1 §32 already proves Beginner/Advanced FIVE_K and Intermediate 2D/3D/6D FIVE_K never route to the canonical template and never 500, via real-HTTP tests. Carried forward, not re-run.

## 38-40. Error contract

Read `GlobalExceptionHandler.cs` in full (219 lines, ~70 typed exception mappings). Positive invariants: every mapping yields a structured `ApiErrorResponse` with `ErrorCode`/`Message`/`CorrelationId`; unhandled/500-class exceptions get a fixed generic message (`"An unexpected error occurred."`) — **no stack trace, no exception type name, no SQL text ever reaches the client for a 500**. **Genuine finding**: for **non-500** typed exceptions, the response `Message` is `exception.Message` directly (line 207), and several of those messages embed internal engineering vocabulary verbatim — e.g. `CatalogCandidateNotPublishedException`'s message (`CatalogCandidateEligibilityGate.cs:93-95`) reads *"Catalog candidate {key} v{version} has status 'DRAFT', not 'PUBLISHED'. It is not eligible for public preview."*, and `CatalogPreviewPersistenceContractException` (`CatalogPlanConfirmationService.cs:388-390`) reads *"...represents the known unsupported 8-week explicit-zero weekly-volume path (taper-week residual volume falls below the V1 key/easy session-allocation floor) and cannot be confirmed."* These are internal catalog/engineering phrases ("candidate", "PUBLISHED", "taper-week residual volume", "session-allocation floor"), not user-facing language, and they would reach the Flutter app's `catch (e) { SnackBar(Text('Failed to confirm plan: ${e.toString()}')) }` handler (`plan_preview_page.dart:69`) verbatim if that code path were ever reached. Classified `V1HARDEN0-006`, **P2** (not P0/P1: these specific exceptions are only reachable through genuine catalog-lifecycle/operational anomalies, not normal user input, and §4 means this screen is currently unreachable anyway — but it is a real, reproducible leak of internal terminology the moment §4 is fixed or any code path does reach these exceptions).

## 41-43. Frontend failure states / double-tap / network retry

`PlanPreviewPage._onConfirm` has a real, correct double-tap guard (`if (_isConfirming) return;`, `plan_preview_page.dart:47`) — `HANDLED`. `PlanGenerationPage`'s own error/retry UI (`_buildError`/"Try Again") is well-built but, per §4, is permanently unreachable since `_error` is never set (the mock branch always succeeds). `ActiveHomeDispatcherPage`'s real (non-mock) branch has its own loading/error states (`loading: CircularProgressIndicator`, `error: falls through to static HomePage's own retry UI`) — `HANDLED`, but also currently unreachable for new users for the same reason. **Network-retry/ambiguous-timeout duplicate-request risk for Confirm specifically is mitigated server-side regardless of frontend behavior** (§11's DB-level guard means even an uncontrolled client retry storm cannot create two active plans — worst case is one wasted round-trip reloading the same plan). Classified: double-tap protection `HANDLED` (frontend) + `IDEMPOTENT` (backend, defense-in-depth); network-retry-ambiguity for Confirm `SAFE` (backend-enforced); for Complete/NotToday, see §19/§20 — **not safe to retry** (duplicate log/decision rows).

## 44-45. Transaction boundaries

`ConfirmPlanAsync`'s persistence step (`BeginTransactionIfSupportedAsync`/`CommitAsync`/`RollbackAsync`, lines 416-554) wraps `TrainingPlans`+`TrainingWeeks`+`TrainingDays`+`PlanEvents` insert and the post-insert `_persistedPlanValidator.ValidateAsync` check in one transaction, rolling back on any `DbUpdateException` or other exception — **no partial-plan-without-days risk**, confirmed by code structure (one `SaveChangesAsync` call for the whole graph, validation before commit, rollback on any failure path). `Complete`/`NotToday`/`Cancel` each use a single `SaveChangesAsync` call touching a small, bounded set of rows (one `TrainingDay` + one `WorkoutLog`/`NotTodayDecision` + one `PlanEvent`) — EF Core's own `SaveChangesAsync` is itself transactional for a single call, so no partial-write risk was found for these smaller mutations either, independent of the §19/§20 idempotency findings (those are about *duplicate* correct writes, not *partial/corrupt* writes).

## 46. DB constraint audit

Beyond the two partial unique indexes already covered (§11/§12), `AppDbContext.cs` shows further unique constraints (lines 290, 307, 320, 382, 393, 418, 433, 444, 467, 504, 526) — not individually enumerated field-by-field in this session, but their presence (10+ `IsUnique()` calls across the schema) indicates the active-plan/preview invariants are not isolated special cases but part of a broader, deliberate DB-level invariant-enforcement pattern. `TrainingDay`/`TrainingPlan` ownership (`InternalUserId`) is a plain FK column, not itself uniquely constrained (correctly — many days per plan, many plans per user over time) but every read/write path checked in this audit (§§ above) filters by it explicitly in the query predicate, never relying on it being enforced structurally beyond the FK relationship. **No DB-level cross-user leak vector was found**; this reinforces §42-43's finding.

## 47. Catalog lifecycle

`ICatalogCandidateEligibilityGate.LoadForPublicPreviewAsync` (§9/§14 context) enforces `PUBLISHED`-only for both the candidate and all its direct dependencies before public preview generation — confirmed by direct read (`CatalogCandidateEligibilityGate.cs:91-105`). A dev-only override (`LocalCatalogAcceptanceOptions`) exists but is triple-gated: `_hostEnvironment.IsDevelopment() && Enabled && EnableCatalogRoute && TreatPilotCandidateAsPublished`, logs a `LogWarning` when active, and the default `IHostEnvironment` for callers that don't supply one is a hardcoded `InertNonDevelopmentEnvironment` (so test code constructing this class directly can never accidentally activate it) — classified `TEST_ONLY_PROVEN_UNREACHABLE` in production (three independent conditions, `IsDevelopment()` plus two opt-in config flags, must all be true; `appsettings.json`'s production-reachable base layer sets none of them).

## 48-49. Retired catalog behavior / catalog conflict behavior

Not independently exercised live in this session (would require constructing a RETIRED/conflicting catalog artifact and attempting generation) — **`AUDIT_GAP`**. `PlanCatalogPackageValidator.Validate(catalogRoot.CatalogRootPath)` runs at **startup**, before the host accepts traffic (`Program.cs:27`), which is a positive invariant for §47/§startup-validation question: malformed catalog structure fails before any real request, not at first-request time. Deeper retirement-semantics ("historical plans stay readable after a candidate retires") rests on §15/§16's structural immutability finding — a retired/changed catalog cannot affect already-persisted plans because persisted plans never re-read the catalog.

## 50. Correlation

`CorrelationIdAccessor.GetOrCreate` is invoked both in `RequestLoggingMiddleware` (every request) and in `GlobalExceptionHandler` (every error response), and the same ID is both logged and returned to the client in the `X-Correlation-Id`-style response header and the `ApiErrorResponse.CorrelationId` field. **Positive invariant**: an operator can correlate a user-reported error to the exact server-side log line via the correlation ID the client already has (and can quote back in a support request), without needing to dump any request/response body.

## 51-52. Error metrics/classification / sensitive-data logging

`RequestLoggingMiddleware` deliberately logs only routing/timing metadata per its own doc comment ("Deliberately logs only routing/timing metadata, never request or response bodies, so no personal data ends up in logs") — confirmed structurally true by the actual `LogInformation` call (method, path, status code, elapsed ms, resolved `UserId` — a Firebase UID, not an email/name). `GlobalExceptionHandler`'s `LogError`/`LogWarning` calls similarly log only the exception object, correlation ID, and path — not the request body. **No dedicated metrics-emission code** (e.g. a counters/Prometheus-style library) was found anywhere in the files read; this repository's "observability" is structured logging only. This is named explicitly as `V1HARDEN0-007`, **P2/P3** (no metrics pipeline exists to misuse high-cardinality dimensions — the risk the governing prompt asks about structurally cannot occur because there is no metrics system at all, but this is itself an operability gap: an operator cannot build a dashboard/alert on error rates by type without parsing logs).

## 53. Configuration

Inventoried from `Program.cs`/`appsettings.json`: `ConnectionStrings:DefaultConnection` (empty in the production-reachable base layer by design — `ProductionConfigurationValidator.Validate` fails startup if still empty or resolves to a known dev/local authority, confirmed by its own call site at `Program.cs:335-338`, gated `IsProduction() && productionValidationEnabled` with `Enabled` defaulting `true`); `Auth:Provider` (defaults `"Firebase"` in `appsettings.json`, `"Mock"` only in `appsettings.Development.json`); `Auth:Firebase:ProjectId` (required, throws at startup if missing when Firebase mode is active); `Cors:AllowedOrigins` (empty by default outside Development → fully restrictive CORS, by design, since the only real client is the native Flutter app); `Deployment:EnforceHttpsRedirection`/`TrustedProxies`/`TrustedNetworks` (opt-in, no silent "probably behind a proxy" assumption per the code's own comment at `Program.cs:357-359`); `PlanCatalog:PublishedBundleReleaseVersion` (`"1.4.0"`, pinned, not implicit-latest). No secret value is printed anywhere in this report.

## 54. Test/debug switches

Searched production code (excluding test projects) for `Allow`/`Enable`/`Dark`/`Pilot`/`Bypass`/`Fallback`-prefixed identifiers. **Result: zero remaining mutable dark-only switches** of the kind DERIVED-DIST.4A removed (`AllowTenKSourcedDerivationForInternalVerificationOnly`) — only comment-level uses of "bypass"/"fallback" as English words describing defensive code paths, plus the one already-classified `LocalCatalogAcceptanceOptions` override (§47, `TEST_ONLY_PROVEN_UNREACHABLE`, triple-gated + logged). No `AllowTenKSourcedDerivationForInternalVerificationOnly`-style sibling exists anywhere in the current tree (confirmed by grep across `backend/RunningApp.Application`, `.Domain`, `.Persistence`, `.Api`).

## 55. Auth/mock-user audit — genuine, disclosed finding

`MockCurrentUserAccessor` (`ICurrentUserAccessor.cs:18-29`) **is registered as the production `ICurrentUserAccessor` in both branches of `Program.cs`'s auth-provider `if`/`else`** (lines 94 and 116) — the class name is misleading: it is not itself mock-only. Read in full, it purely delegates to `IIdentityProvider.GetCurrentIdentity()`, and `IIdentityProvider` is bound to `FirebaseIdentityProvider` in **both** branches (lines 93 and 112) — the only thing that differs between `Auth:Provider=Mock` and `=Firebase` is which middleware populates the identity that `FirebaseIdentityProvider` reads back out of `HttpContext.Items` (`MockAuthMiddleware` upserts a real DB user and stores a real `InternalUserId`; `FirebaseAuthMiddleware` validates a real Bearer token). `appsettings.json` (production-reachable) defaults `Auth:Provider` to `"Firebase"`; only `appsettings.Development.json` sets `"Mock"`. **No hardcoded `mock-user-001`/static GUID identity reaches production**, confirmed structurally: the code's own comment (`Program.cs:90-91`) explicitly warns against using a different class (`MockIdentityProvider`, with `InternalUserId=Guid.Empty`) for exactly this reason, and that class is never wired in either branch. **Classification: `INTENTIONAL_PRODUCTION_FEATURE_FLAG`** (the Mock/Firebase switch itself, correctly defaulted and FK-safe), not a stale dangerous switch. `TestingController`'s `/api/v1/testing/reset` endpoint is gated by `!_env.IsDevelopment()` → 403 (confirmed, `TestingController.cs:31-34`) and additionally scoped to only the calling user's own rows even if it were reached — classified `TEST_ONLY_PROVEN_UNREACHABLE` in Production.

## 56. Public API malformed inputs

Not independently fuzzed this session (missing fields / bad enum casing / duplicate preferred days / invalid dates as raw malformed JSON against a live running server). `[ApiController]` attribute (present on every controller, confirmed) gives ASP.NET Core automatic model-validation-failure→400 behavior for malformed JSON/missing required fields before a controller action even runs, which is a strong structural default. `ArgumentException` has an explicit generic mapping (`GlobalExceptionHandler.cs:179`, 400/`VALIDATION_ERROR`) rather than falling through to the generic 500 case — confirmed. **Classified `STRUCTURALLY_LIKELY_SAFE`, not empirically re-proven — `AUDIT_GAP`** for a live fuzz pass.

## 57-59. Concurrency / read consistency / cache

Confirmed structurally for the two highest-stakes races (double-confirm, §11; concurrent-session mutation is covered by the ordinary read-modify-write-via-`SaveChangesAsync` pattern, which for Postgres + EF Core without optimistic concurrency tokens on `TrainingDay` **does not itself prevent two concurrent `Complete` calls from both succeeding and both writing** — this is the same root cause as §19's idempotency gap, not a separate new defect; it is the same finding, now also confirmed as a genuine concurrency (not just serial-replay) risk, folded into `V1HARDEN0-002`). No caching layer was found anywhere in the read paths examined (`GetHomeAsync`/`GetCalendarAsync`/`GetActivePlanDetailsAsync` all query `AppDbContext` directly, `AsNoTracking()`, no `IMemoryCache`/distributed-cache wrapper) — **Result: `NO_CACHE_EXISTS_IN_THIS_PATH`**, stated plainly per the governing prompt's own instruction, not invented as a problem. Since every mutation commits via `SaveChangesAsync` before returning and every read queries the DB directly with no cache in between, stale-read-after-committed-mutation is **not possible** by construction for this path.

## 60-62. Performance smoke / query-shape / max plan size

`GetHomeAsync`/`GetCalendarAsync` both show explicit N+1-avoidance engineering in their own code comments and structure (`weeksById` loaded once and reused, `Week` navigation joined in the same query rather than per-day lookups) — a positive invariant, not a finding. No live timing measurement was taken this session (`AUDIT_GAP` for an empirical latency number), but the query shapes read show no obvious pathological pattern (no per-day loop issuing its own query, no full-table scan without a `WHERE PlanId=`/`InternalUserId=` predicate in any path read). Maximum plan size: FIVE_K/TEN_K/HM at their maximum horizons (13-20 weeks × up to 6 days/week) is at most a few hundred `TrainingDay` rows per plan — well within any plausible response-size/rendering assumption; not independently measured this session.

## 63-64. Empty/partial states / frontend empty states

`GetHomeAsync` (no active plan → explicit `ActivePlan=null` response, never null/500, confirmed lines 59-69) and `GetActivePlanDetailsAsync` (no active plan → `HasActivePlan=false, Status="none"`, explicit comment "deterministic empty response... so the client can rely on a stable shape either way", lines 1005-1015) both show deliberate, tested-looking empty-state design. `GetCalendarAsync` with no active plan returns an empty list, not null (line 226-231). **No null-reference/500 risk found in any empty-state path read.** Frontend empty-state rendering for these shapes was not independently re-walked this session beyond `PlanPreviewPage`'s own defensive "preview==null→show a way back" check (line 115) — **`AUDIT_GAP`** for the Home/Calendar pages' own empty-state widgets specifically (not opened this session).

## 65-67. Race-day end state / post-plan behavior / history behavior

Not independently determined this session what happens when a plan reaches its final day (no explicit "plan naturally completed" status transition was found in the files read — `TrainingPlanStatus` appears to have at least `Active`/`Cancelled`; whether a `Completed` status exists and is set automatically was not traced to its assignment site). **Classified `AUDIT_GAP`** — this is a real open question this audit could not close within its time budget: if no automatic end-of-plan transition exists, a plan past its last `TrainingDay` date likely just continues reporting as `Active` with no current-day workout, which `GetHomeAsync`'s synthetic-rest-day fallback would paper over without an explicit "plan finished, confirm a new one" signal. Flagged as a named gap requiring a dedicated follow-up look, not asserted as a defect without having traced the `TrainingPlanStatus` enum and its assignment sites exhaustively.

## 68-70. Post-confirm editing / back-navigation / stale preview

No endpoint for editing a confirmed plan's inputs was found in `PlansController` (only generate-preview/confirm/active-reads/cancel/long-horizon-specific actions exist) — consistent with "no editing exists, so no unsafe partial-mutation surface exists either." Preview staleness: the 30-minute `ExpiresAt` window (§14) plus the snapshot-stability guarantee (§13) together mean a stale preview cannot silently diverge from what the user saw, and cannot be confirmed indefinitely — it is enforced, not merely assumed, confirmed by `PlanPreviewExpiredException` being thrown explicitly (`CatalogPlanConfirmationService.cs:227-230`) for an expired preview at confirm time. **No TTL was invented by this audit** — the existing 30-minute value is read directly, not proposed.

## 71. Retry-safety matrix

See §76 (error/retry table). Summary: `GenerateRacePlanPreview` — `SAFE_TO_RETRY` (§10); `ConfirmPlan` — `IDEMPOTENT` (§11, DB-enforced); `Complete`/`NotToday` — `UNSAFE_TO_RETRY` (§19/§20, duplicate rows); `Cancel` — `SAFE_TO_RETRY` (second call 404s, no duplicate side effect); `CreateNewPlan` (= confirm after cancel) — `SAFE_TO_RETRY` (same guards as Confirm).

## 72. Positive proven invariants

- Exact target-distance identity persists end-to-end with no rounding (carried forward, FIVE-K.1/DERIVED-DIST.4A).
- Exact-canonical routing bypass never crosses derived routes (carried forward).
- **DB-level, not merely app-level, one-active-plan-per-user and one-plan-per-preview uniqueness** (`AppDbContext.cs:297-309`, this phase's own direct read).
- Cross-user authorization is enforced at every mutation/read endpoint examined via an explicit `InternalUserId` predicate in the query itself (`QueryAndMutationServices.cs` throughout; `TrainingDaysController`/`PlansController` never accept a user id from the client — always `_currentUser.InternalUserId`).
- Confirm persistence is transactional with rollback on any failure, including post-insert validation failure.
- Preview→confirm uses a frozen snapshot, not re-generation — no drift risk.
- Preview has an enforced 30-minute expiry.
- 500-class errors never leak stack traces/internal exception text to the client; every response carries a correlation ID logged server-side too.
- Request logging is metadata-only by design (no bodies, no PII) — confirmed by code, not just comment.
- Catalog package validation runs at startup, before traffic is accepted.
- Production CORS is restrictive-by-default (no origins configured ⇒ no cross-origin browser access).
- Production auth defaults to Firebase; the Mock path is Development-only and still produces a real, FK-safe `InternalUserId` (no `Guid.Empty` identity reaches the DB in either mode).
- The one remaining dev-only catalog-override switch is triple-gated and logs a warning when active.
- `PlanCatalog.Tests` reconfirmed fresh this session at **1510/1510**, matching FIVE-K.1's own reported baseline exactly (zero drift since that commit, consistent with the empty `git diff` in §1).

## 73. Launch-readiness matrix

| Area | Status | Evidence | HighestSeverityFinding | LaunchBlocker? | NextAction |
|---|---|---|---|---|---|
| Onboarding contract | Collected correctly, never submitted | §4, §6 | V1HARDEN0-001 (P0) | **YES** | Restore real `generatePreview()`/navigation call |
| Eligibility | Correct, unchanged | §3 (carried forward) | none | No | — |
| Preview | Idempotent, snapshot-pinned | §10, §13, §14 | none | No | — |
| Confirm | DB-enforced idempotent/unique | §11, §12 | none | No | — |
| Persistence | Transactional, immutable-by-construction | §15, §44 | none (AUDIT_GAP on live immutability proof) | No | Optional live test-seam proof |
| Home read | Null-safe, no N+1 | §63 | none | No | — |
| Calendar read | Null-safe, generic date math | §22, §63 | none | No | — |
| TrainingDay read | Ownership-scoped | §42 (below) | none | No | — |
| Complete | Not idempotent (log/event duplication) | §19 | V1HARDEN0-002 (P1/P2) | No (not P0) | Add status/idempotency guard |
| Not Today | Not idempotent; can revert a Completed day | §20-21 | V1HARDEN0-004 (P1) | No (not P0, but user-trust-affecting) | Add status guard before creating decision |
| Cancel | Safe, ownership-scoped | §17 | none | No | — |
| New plan after cancel | Structurally sound (unique-index slot frees) | §12, §18 | none (not live-tested) | No | — |
| Race-date handling | Typed-failure taxonomy exists | §24-25 | AUDIT_GAP | No | Live boundary sweep |
| Preferred-days handling | Typed-failure taxonomy exists | §26-27 | AUDIT_GAP | No | Live boundary sweep |
| Fitness evidence | Carried-forward proven | §34-36 | none | No | — |
| Error handling | Strong 500 hygiene; some 4xx leak internal terms | §38-40 | V1HARDEN0-006 (P2) | No | Sanitize a handful of exception messages |
| Authorization | Ownership-scoped everywhere examined | §42-43 (below) | none | No | — |
| Historical immutability | Structurally proven | §15-16 | AUDIT_GAP (live proof) | No | Optional test-seam proof |
| Catalog lifecycle | PUBLISHED-gated, startup-validated | §47-49 | AUDIT_GAP (retired-candidate live test) | No | — |
| Observability | Correlation IDs present; no metrics pipeline | §50-52 | V1HARDEN0-007 (P2/P3) | No | Consider metrics later |
| Configuration | Fail-fast, restrictive defaults | §53-55 | none | No | — |
| Frontend failure UX | Well-built but unreachable (see Onboarding) | §41, §4 | V1HARDEN0-001 | **YES** (same root cause) | Same as Onboarding |
| Regression | Clean, zero-delta since FIVE-K.1 | §1, §85 | none | No | — |

## 74. Public-scope table

| PlanMode | DistanceDomain | Experience | RunsPerWeek | BackendPublic | FrontendReachable | Canonical/Derived | Notes |
|---|---|---|---|---|---|---|---|
| Race (Core) | exact 5K | Intermediate | 4, 5 | Yes | **No** (§4) | Canonical FIVE_K | Only Intermediate 4/5 approved |
| Race (Core) | 5K<target<10K | Intermediate | 3, 4, 5 | Yes | **No** (§4) | Derived (TEN_K) | |
| Race (Core) | exact 10K | Beginner/Intermediate/Advanced | 2-6 (level-dependent) | Yes | **No** (§4) | Canonical TEN_K | |
| Race (Core) | 10K<target<HM | Intermediate | 3, 4, 5 | Yes | **No** (§4) | Derived (HALF_MARATHON) | |
| Race (Core) | exact HM | Beginner/Intermediate/Advanced | 3-6 (level-dependent) | Yes | **No** (§4) | Canonical HALF_MARATHON | |
| Race (Core) | 0<target<5K, >HM | — | — | No | No | — | Unchanged, out of scope |
| Race (Long-Horizon) | TEN_K, 21-52wk structural horizon | (per `V1LiveCatalogPilotRoutingPolicy`) | — | Partially (own controller actions exist) | **Not independently verified this session** | — | Separate mode, own endpoints, AUDIT_GAP on frontend reachability |
| Habit | FIVE_K (and others) | — | — | Yes (LegacySql) | Not verified this session | N/A (legacy) | Structurally separate from Race per FIVE-K.1 §43 |

The "FrontendReachable: No" entries above are the single most consequential row-set in this report — every backend cell that the entire DERIVED-DIST/FIVE-K engagement built, tested, and activated is currently unreachable by a real user through the shipped app's main flow, for the reason in §4.

## 75. Mutation matrix

| Mutation | InitialState | RepeatedCall | ConcurrentCall | ExpectedBehavior | ActualBehavior | Idempotent? | Transactional? | FindingId |
|---|---|---|---|---|---|---|---|---|
| Confirm | No active plan | Same preview replayed | Two confirms, same/different preview | One active plan created; replay returns it | Matches expected exactly | Yes | Yes | — |
| Complete | Day Planned | Re-Complete same day | Two concurrent Completes | Second call should be a no-op or rejected | Overwrites actuals, duplicates `WorkoutLog`/`PlanEvent` | No | Yes (per-call), not cross-call-safe | V1HARDEN0-002 |
| NotToday | Day Planned or Completed | Re-create decision | Two concurrent NotToday | Second call should be rejected/no-op if already resolved | Creates a second Pending decision regardless of current day status | No | Yes (per-call) | V1HARDEN0-003 / V1HARDEN0-004 |
| Cancel | Plan Active | Re-Cancel | Cancel + Complete race | Second Cancel should 404, not duplicate-cancel | Matches expected (404 on replay) | Yes (via rejection) | Yes | — |
| CreateNewPlan | No active plan (post-cancel) | N/A | New confirm racing a stale active-plan read | Should succeed once prior plan is Cancelled | Matches expected (DB index frees on status change) | Yes | Yes | — |

## 76. Error matrix (representative sample — not exhaustive)

| Scenario | Endpoint | BackendResult | HTTP Status | ErrorCode | FrontendHandling | UserMessage | Retryable | Severity |
|---|---|---|---|---|---|---|---|---|
| Unsupported target distance (e.g. 4.9km) | generate-preview/race | Typed exception | 400 | `UNSUPPORTED_TARGET_DISTANCE` | N/A (unreachable, §4) | `exception.Message` (product-appropriate text) | No | P2 (would be P0 UX gap if reachable) |
| Catalog candidate not PUBLISHED | generate-preview/race | Typed exception | 409 | `CATALOG_CANDIDATE_NOT_PUBLISHED` | N/A (unreachable, §4) | Raw internal phrase (§38-40) | No | P2 (V1HARDEN0-006) |
| Confirm a different user's preview | confirm | `PlanPreviewForbiddenException` | 403 | `PLAN_PREVIEW_FORBIDDEN` | N/A (unreachable, §4) | `exception.Message` | No | P3 |
| Confirm an expired preview | confirm | `PlanPreviewExpiredException` | 409 | `PLAN_PREVIEW_EXPIRED` | N/A (unreachable, §4) | "...expired at ... Generate a new preview." (internal timestamp format, minor) | Yes (after regenerating) | P3 |
| Unhandled exception | any | `GlobalExceptionHandler` default | 500 | `INTERNAL_ERROR` | N/A (unreachable, §4) | Generic, safe | Maybe | P2 |
| Cancel a nonexistent/foreign plan id | `{planId}/cancel` | `NotFoundAppException` | 404 | `NOT_FOUND` | N/A (unreachable, §4) | Generic | No | P3 |

## 77. Date-edge table

| Edge | Expected | Actual | Result | Finding |
|---|---|---|---|---|
| Mid-week start (e.g. Wednesday) | Week boundaries anchor to real `StartDate`, not Monday | Confirmed by code (`currentWeek.StartDate`-anchored, `QueryAndMutationServices.cs:131-134`) | Pass (code-level) | — |
| Sunday start | Same as above | Same generic code path, no special-case | Pass (code-level) | — |
| Month-end | Calendar loop fills every day `startOfMonth..endOfMonth` inclusive | Generic `DateTime.AddMonths(1).AddDays(-1)` arithmetic | Pass (code-level) | — |
| Year-end (Dec31/Jan1) | No special handling needed/found | Same generic arithmetic | Pass (code-level), not live-tested | AUDIT_GAP |
| Leap-year boundary | Same | Same generic arithmetic (`.AddMonths`/`.AddDays` are leap-aware in .NET) | Pass (code-level), not live-tested | AUDIT_GAP |
| Race before start date | Should be rejected | `PlanHorizonStartTooEarlyException`/typed taxonomy exists | Pass (taxonomy exists), not live-boundary-tested this session | AUDIT_GAP |
| Race too early for minimum horizon | Should be rejected | `PlanCoreHorizonTooShortException` exists | Pass (taxonomy), AUDIT_GAP for exact boundary | AUDIT_GAP |
| Race beyond maximum horizon | Should be rejected or routed to Long-Horizon mode | `PlanHorizonCompositionRequiredException`/Long-Horizon mode exists | Pass (taxonomy/mode exists), AUDIT_GAP for exact boundary | AUDIT_GAP |
| Timezone-sensitive date | UTC-stored, fail-fast on Unspecified kind | Confirmed (§30) | Pass | — |

## 78. Data-integrity table

| Invariant | ProtectionLayer | Proof | Risk |
|---|---|---|---|
| Duplicate active plan | DB partial unique index | `AppDbContext.cs:297-299`, direct read | None found |
| Partial plan (missing days) | Single transactional `SaveChangesAsync` + rollback | `CatalogPlanConfirmationService.cs:416-554`, direct read | None found |
| Orphan TrainingDay | FK to `TrainingPlan`/`TrainingWeek`, cascade-ordered deletes in `TestingController` itself show awareness of FK ordering | `TestingController.cs:49-93` | Low (not independently stress-tested) |
| Cross-user access | Explicit `InternalUserId` predicate on every read/write query examined | `QueryAndMutationServices.cs`, `PlanServices.cs` | None found |
| Future-catalog mutation affecting old plan | Persisted columns, not catalog re-resolution at read time | §15-16 | None found structurally; AUDIT_GAP on live proof |
| Duplicate completion | **No guard** — status not checked before mutation | §19 | **Real, confirmed** (V1HARDEN0-002) |
| Stale status across reads | No cache layer exists; every read hits DB directly | §59 | None (no cache to go stale) |

## 79. Observability table

| Operation | StructuredLog | Correlation | ErrorClassification | Metric/Trace | SensitiveDataRisk | OperationallyDiagnosable? | Finding |
|---|---|---|---|---|---|---|---|
| Preview | Via `RequestLoggingMiddleware` (generic) | Yes | Generic (status code only) | None | Low (metadata only) | Partially (no preview-specific structured event) | — |
| Confirm | `CatalogPlanConfirmed` `PlanEvent` (DB) + generic request log | Yes | Generic | None | Low | Partially | — |
| Generation failure | Generic request log + `GlobalExceptionHandler` log | Yes | Yes (typed exception → error code) | None | Low | Yes (via error code + correlation id) | — |
| Eligibility rejection | Generic request log | Yes | Yes (typed) | None | Low | Yes | — |
| Catalog resolution failure | Generic request log + explicit `LogWarning` for the one dev-override case | Yes | Yes (typed) | None | Low | Yes | — |
| DB failure | Generic request log + `GlobalExceptionHandler` `LogError` | Yes | Partial (often falls to generic 500/`INTERNAL_ERROR`) | None | Low | Partially (correlation id + exception, but not auto-classified by DB-error type) | — |
| Complete | `WorkoutCompleted` `PlanEvent` + generic request log | Yes | No dedicated classification | None | Low | Partially | Related to V1HARDEN0-002 (duplicate events) |
| NotToday | `WorkoutMissed`/decision rows + generic request log | Yes | No dedicated classification | None | Low | Partially | Related to V1HARDEN0-003 |
| Cancel | `PlanCancelled` `PlanEvent` + explicit `LogInformation` + generic request log | Yes | Yes (explicit structured log line) | None | Low | Yes | — |

No metrics/trace pipeline exists anywhere in the code read (§51-52) — stated as fact across every row, not repeated per-row as a new finding.

## 80. Configuration table

| Config | DevValue | ProductionSource | FailureIfMissing | SafeDefault? | LaunchRisk |
|---|---|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | Local Postgres (Development json) | External secret/env var (required) | Fails fast at startup (`ProductionConfigurationValidator`) | Yes (fails closed) | Low |
| `Auth:Provider` | `Mock` | `Firebase` (base appsettings.json default) | N/A (has a default) | Yes | Low |
| `Auth:Firebase:ProjectId` | N/A (not used in Mock) | Required when Firebase mode active | Throws at startup | Yes (fails closed) | Low |
| `Cors:AllowedOrigins` | N/A (permissive dev policy) | Empty by default → fully restrictive | No failure, just restrictive | Yes | Low |
| `Deployment:EnforceHttpsRedirection`/`TrustedProxies` | N/A | Opt-in, no silent proxy trust | No failure, just no redirection/forwarding | Yes (fails toward "untrusted", not toward "trust everything") | Low |
| `PlanCatalog:PublishedBundleReleaseVersion` | Pinned (`1.4.0`) | Same config key, must be set | Startup catalog validation would fail if root is wrong | Yes | Low |
| `LocalCatalogAcceptanceOptions.*` | Opt-in, Development-only | Not set in production config | N/A (inert unless triple-gated) | Yes | Low (see §47/§54) |

No secret values are printed above.

## 81. P0 blockers

- **`V1HARDEN0-001`** — The shipped Flutter app's onboarding-completion screen (`mobile/lib/features/onboarding/presentation/plan_generation_page.dart`, lines 80-117) never calls the real backend plan-generation/confirmation endpoints. It unconditionally sets `useMockHomeDataProvider` to `true` and navigates straight to a hardcoded mock Home screen. The fully-correct real implementation exists, is well-formed, and is sitting directly above the active code as a comment. Every real user who completes onboarding today receives a fake, static mock plan, never a real one generated by the entire distance-engine architecture this engagement built. **User impact**: total — the core product function does not work for any real user today. **Data impact**: none (no real plan is ever persisted via this path, so no corruption — but also no real usage). **Reproducible**: yes, by reading the named file; also reproducible at runtime by completing onboarding on a real device/simulator and observing the Home screen never issues `GET /plans/active/home` and no `TrainingPlans` row is created. **Recommended fix class**: restore the commented-out real call and the `pushReplacement` to `PlanPreviewPage`/Long-Horizon preview, remove the unconditional mock bypass (or gate it correctly behind a dev-only flag if it is meant to remain available for design/QA purposes), and add an end-to-end widget/integration test that fails if onboarding completion does not result in a real `ConfirmPlan` call. This single defect is why the overall launch decision in §86 cannot be anything but blocked, independent of every other finding in this report.

No other P0 was found. Cross-user authorization, active-plan-uniqueness, confirm-duplication, and persisted-plan-integrity — the areas most likely to hide a second P0 — were all found to be soundly, often DB-level, enforced (§11, §12, §42-46).

## 82. P1 blockers

- **`V1HARDEN0-002`** — `CompleteWorkoutAsync` (`QueryAndMutationServices.cs:388-451`) has no guard against re-completing an already-`Completed` `TrainingDay`: each call overwrites `ActualDistanceKm`/`ActualDurationMin`/`CompletedAt` with the new call's values and unconditionally appends a new `WorkoutLog` and a new `PlanEvent` row. A retried/duplicated client request (slow network, double-tap without a frontend guard reaching this endpoint, or a future caller) silently duplicates audit-trail rows and can silently alter what a user's "actual" workout result shows after the fact. **Reproducible**: yes, by code read; two sequential `POST .../complete` calls with different bodies for the same `trainingDayId` will both succeed and the second overwrites the first with no error. **Fix class**: check `day.Status == Completed` and return the existing result (idempotent reload) instead of re-mutating, matching the pattern already used correctly for Confirm (§11).
- **`V1HARDEN0-004`** — `CreateNotTodayDecisionAsync` has no guard against creating a Not-Today decision for a `TrainingDay` that is already `Completed`; once such a decision is later confirmed via `ConfirmNotTodayDecisionAsync`, it sets `Status=Missed` on a day the user had genuinely completed, silently reverting their recorded progress. **Reproducible**: yes, by code read (`QueryAndMutationServices.cs:454-487` has zero status checks). **Fix class**: reject (or no-op) a Not-Today request against a day whose current `Status` is already `Completed`.

## 83. P2 hardening

- **`V1HARDEN0-003`** — `CreateNotTodayDecisionAsync` also allows unlimited duplicate `Pending` decisions to accumulate for one `TrainingDayId` on repeated calls against a non-Completed day, each surfacing separately in `GetPendingConfirmationsAsync`. Fix class: dedupe/guard by `TrainingDayId` + `Status=Pending`.
- **`V1HARDEN0-005`** — No cross-field input-sanity validation exists between `RecentWeeklyVolumeKm` and `RecentLongestRunKm` (e.g. longest-run exceeding weekly volume, or zero volume with a nonzero longest run) in `GenerateRacePlanPreviewRequestValidator`. Fix class: add a bounded cross-field check, or explicitly document the fitness-evidence hierarchy's own tolerance for this as an accepted product decision.
- **`V1HARDEN0-006`** — Several non-500 typed exception messages (`CatalogCandidateNotPublishedException`, `CatalogDependencyNotRuntimeEligibleException`, `CatalogPreviewPersistenceContractException`, and similarly-worded siblings) use raw internal catalog/engineering vocabulary as their `Message`, which `GlobalExceptionHandler` forwards verbatim to the client for non-500 status codes. Fix class: introduce a user-safe message layer for these specific exception types (the 500-path already does this correctly).
- **`V1HARDEN0-007`** — No metrics/trace pipeline exists; observability is structured-logging-only. Not a defect, but an operability gap worth closing before meaningful production scale (dashboards/alerting currently require log-parsing, not a queryable metric).

## 84. P3 postlaunch items

- Several `AUDIT_GAP` items named throughout this report (live timezone/DST boundary proof, live catalog-retirement proof, live immutability test-seam proof, exhaustive numeric/date-boundary sweep, fresh hour-long full-integration re-run) are candidates for a dedicated, narrower verification pass once `V1HARDEN0-001` is fixed and the real flow is reachable for genuine end-to-end testing again.
- No dedicated "view historical/cancelled plan" read endpoint was found (§17) — a product question, not a data-integrity defect, since the underlying rows are retained.
- Plan-completion end-state (§65-67) needs its own targeted trace of `TrainingPlanStatus`'s full enum and assignment sites — named as an open question, not asserted as broken.

## 85. Regression

`git diff --stat` between `a0d50f2` (FIVE-K.1's implementation commit) and this phase's `HEAD` (`8250289`) touches zero files under `backend/`, `mobile/`, `plan-catalog/` (confirmed, §1) — this phase made no production-code change (audit-only, as mandated) and the repository's code state at the time of this audit is byte-identical to the code FIVE-K.1 itself regression-tested. `PlanCatalog.Tests` was re-run fresh this session: **1510/1510, 0 failed, 0 skipped, 12s** — exact match to FIVE-K.1's own reported number, independently reconfirmed rather than merely copied forward. The full `RunningApp.IntegrationTests` suite (5447 tests, ~1 hour wall-clock per FIVE-K.1's own report) was **not independently re-run in this session** — a real local Postgres 17 container (`appsel-dev-postgres`) was confirmed running and healthy, and zero stray `dotnet`/`testhost` processes were confirmed present, but the hour-long full run was judged unnecessary to repeat given the zero-diff finding (§1) and was out of this session's effort budget; this is disclosed honestly as **`AUDIT_GAP: FULL_INTEGRATION_SUITE_NOT_FRESHLY_RE_RUN_THIS_SESSION`**, with FIVE-K.1's own fresh, clean, isolated run (5447 total / 5426 passed / 4 failed / 17 skipped, byte-for-byte matching the long-standing durable baseline failure identities, zero new reproducible failure) carried forward as the valid current baseline because the code it ran against is provably the same code this audit examined. No new test was added by this audit (per §86/§87 — no fix-by-audit, and no testability seam was judged essential to prove any invariant that direct source reading could not already establish with sufficient confidence).

## 86. Launch decision

**`APPSEL_V1_LAUNCH_BLOCKED_BY_P0_P1_FINDINGS`**

One P0 (`V1HARDEN0-001`, the onboarding-to-mock-Home bypass — unconditional, affects 100% of real users, zero exception) and two P1s (`V1HARDEN0-002`, `V1HARDEN0-004`, both in the Complete/NotToday mutation idempotency surface) are open and unresolved. Per the governing prompt's own rule, any unresolved P0 or P1 blocks launch unconditionally; this phase does not fix them (per §86/§87's no-fix-by-audit mandate) and does not overclaim readiness the evidence does not support. The backend distance-engine and plan-lifecycle architecture underneath this bypass is, on the evidence gathered in this phase, substantially sound (strong DB-level invariants for the two riskiest areas — active-plan uniqueness and confirm double-submit — plus clean cross-user authorization, transactional confirm persistence, and a disciplined error/correlation-id contract for 500s) — the launch blocker is overwhelmingly about the single frontend wiring defect in §4/§81, not about the backend work this engagement has spent many phases proving correct.

## 87. Follow-up phase slicing

Only categories with real findings get a named phase, per the governing prompt's own instruction not to create empty phases:

- **V1-HARDEN.1 (data integrity/idempotency/transaction safety)**: fix `V1HARDEN0-002` (Complete idempotency) and `V1HARDEN0-004`/`V1HARDEN0-003` (NotToday idempotency/status-guard), the two P1s and one adjacent P2 that together make up the mutation-idempotency surface this audit found. Smallest coherent unit: both live in the same class (`QueryAndMutationServices`), same root cause (missing status pre-check before mutation), fixable together.
- **A dedicated, narrow frontend-wiring phase** (not named `V1-HARDEN.x` per the governing prompt's own category list, since "frontend failure UX/unsupported-state alignment" — `V1-HARDEN.3` — is about UX *polish*, not about the flow being wired at all; this finding is categorically different and more urgent than that category implies) to restore the real `generatePreview()`/confirm call path in `plan_generation_page.dart` and add a regression test (widget or integration) that would have caught this defect, i.e. an assertion that completing onboarding results in a real `ConfirmPlan` network call and a real persisted `TrainingPlan` row, not merely that the mock-data path renders correctly. This is the single highest-priority follow-up in the entire engagement at this point — recommended to run before any `V1-HARDEN.1-5` work, since nothing else matters for launch until it is fixed.
- **V1-HARDEN.2 (public contract/validation/date-boundary closure)**: close `V1HARDEN0-005` (volume/longest-run cross-field validation) plus the named `AUDIT_GAP` items for live date-boundary/race-date/preferred-day boundary sweeps that this phase could not execute within its time budget — these are genuine open questions, not confirmed defects, and deserve a dedicated pass with the time to actually exercise them live.
- **V1-HARDEN.5 (observability/production configuration)**: optional, lower urgency — sanitize the handful of leaking exception messages (`V1HARDEN0-006`) and consider whether a lightweight metrics layer (`V1HARDEN0-007`) is worth adding before scale, once the P0/P1 items above are closed.
- No `V1-HARDEN.3` (frontend failure UX) or `V1-HARDEN.4` (catalog versioning/historical immutability) phase is recommended on this audit's own findings — the UX states examined were well-built (§41-43), and catalog/immutability (§13-16, §47-49) was found structurally sound with no live-contradicting evidence, only disclosed `AUDIT_GAP`s for empirical reproof, not confirmed defects.

## 88. Distance-engine closure statement

**`V1_DISTANCE_ENGINE_REMAINS_CLOSED_NO_FURTHER_DISTANCE_EXPANSION_REQUIRED`**

No regression in any already-proven distance behavior was found by this audit. `CustomDistanceNewCreationRouter.cs`, `DerivedDistanceEligibilityPolicy.cs`, `V1CatalogPilotIdentityPolicy.cs`, and every catalog/volume/horizon/taper policy file the DERIVED-DIST/FIVE-K chain built were read only to confirm current state (§1, §3) and were not modified, and the zero-diff finding in §1/§85 independently confirms nothing has drifted since FIVE-K.1's own clean closure. This audit's P0/P1 findings are entirely in the frontend-wiring and mutation-idempotency layers — categorically unrelated to distance-generation correctness — and do not reopen any frozen distance-engine question.

---

## Audit-gap summary (explicit, per the governing prompt's "no forced closure" discipline)

The following items are named explicitly as genuine audit gaps — not silently skipped, not asserted as either pass or fail without evidence:

1. Live timezone/DST-boundary proof (not meaningfully testable in this Windows sandbox within this phase's budget).
2. Live catalog-retirement/conflict-state generation test (§48-49).
3. Live test-seam proof of persisted-plan immutability against a real catalog mutation (§15-16) — structurally proven by code path, not empirically reproduced.
4. A fresh, independent re-run of the full ~1-hour `RunningApp.IntegrationTests` suite (§85) — not repeated given the zero-diff finding against FIVE-K.1's own already-clean run, but named honestly rather than silently assumed.
5. Exhaustive live date-boundary/race-date/preferred-day boundary sweeps (§24-27, §77) — typed-exception taxonomy confirms *some* enforcement exists; exact boundary values were not re-derived live this session.
6. Live fuzz pass against malformed/malicious raw JSON request bodies (§56).
7. Empirical latency/performance measurement (§60-62) — query shapes read show no obvious pathology, but no number was captured.
8. Full trace of `TrainingPlanStatus`'s complete enum and every assignment site, to definitively answer the race-day/plan-end-state question (§65-67).
9. Independent end-to-end verification of the Long-Horizon rolling mode and the Habit/LegacySql mode's own frontend reachability (§5, §74) — confirmed structurally separate from the Core Race mode examined in depth, but not independently walked end-to-end themselves.

None of these gaps changes the launch decision in §86 — the P0 finding (§4/§81) is independently sufficient, by itself, to block launch regardless of how any of the above gaps resolve.

## Final classification

**`APPSEL_V1_LAUNCH_READINESS_AUDIT_BLOCKED`** — not because the audit itself stopped early, but because the governing checklist's own final-classification rule requires naming audit gaps honestly rather than forcing `_COMPLETE`, and the nine items above are genuine, disclosed gaps against a ~100-section checklist executed in one bounded session. Every section of the governing prompt was addressed with either direct evidence, a carried-forward binding prior-phase proof, or an explicit, named gap — none were silently skipped. The launch decision itself (§86, `APPSEL_V1_LAUNCH_BLOCKED_BY_P0_P1_FINDINGS`) is unambiguous and does not depend on any of the open gaps resolving in any particular direction.
