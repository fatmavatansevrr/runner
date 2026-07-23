# Running Background V2.1 — Intermediate Pilot End-to-End Closure and Canonical Four-Level Contract Cleanup

**Classification:** `RUNNING_BACKGROUND_V2_1_CLOSED`

**Publication boundary (unchanged throughout):** `TEN_K__4D__INTERMEDIATE v10` remains `DRAFT` on disk in `plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`. No publication-ledger entry was created. Production activation (`CatalogLivePilotOptions.Enabled`) defaults `false` and was not changed. All successful HTTP flows in this closure used the Development-only `LocalCatalogAcceptanceOptions` override (Phase 4F.9.3), which never mutates the real candidate artifact.

---

## 1. Active contract

Exactly four values, both backend and frontend, wire-identical:

```
beginner
intermediate
advanced
experienced
```

`UsedToRun`/`used_to_run` (and `NewToRunning`/`new_to_running`, `RunningRegularly`/`running_regularly`) are **absent** from the active contract:
- Rejected with a typed HTTP 400 at the public request boundary (`GeneratePreviewRequest.Level`).
- Absent from `RunningBackground.cs`'s type-level converter (now `RunningBackgroundCanonicalJsonConverter`, canonical-only).
- Absent from the frontend `RunningBackground` Dart enum's `parse()`/`tryParse()` — the frontend has no legacy-alias handling at all.
- Absent from Swagger/OpenAPI examples (`DtoExamplesSchemaFilter.cs` — 5 occurrences of `"new_to_running"` replaced with `"beginner"`).

## 2. Historical compatibility

**Classification:** `READ_ONLY_LEGACY_COMPATIBILITY_REQUIRED`

**Evidence gathered before deciding** (direct query against the real local dev Postgres database):

| Store | Legacy rows found | Action taken |
|---|---|---|
| `TrainingPlans.Level` | 320 rows = `"running_regularly"` (144 already `"intermediate"`) | **Migrated** to canonical via `20260716185115_RunningBackgroundV2_1_MigrateLegacyTrainingPlanLevels` — all 464 rows now `"intermediate"`. Verified post-migration: `SELECT "Level", COUNT(*) FROM "TrainingPlans" GROUP BY "Level"` → `intermediate: 464` only. |
| `PlanTemplates.Level` | Already migrated in V2 (`20260716175426_RunningBackgroundV2FourLevelModel`) | No action needed — 3 rows, all `"beginner"`. |
| `PlanPreviews.PreviewPayloadJson` (embedded `normalized_input.level`) | 166 of 221 rows contain `"running_regularly"` | **Not migrated** — this JSON is hash-verified for tamper detection (Phase 4F.9.2); mutating it in place would break that hash. All 166 affected rows are already expired (`ExpiresAt < now()`) — confirmed via `SELECT "IsInvalidated", ("ExpiresAt" < now()) AS expired, COUNT(*) FROM "PlanPreviews" GROUP BY 1,2` → 212 expired / 9 live, and none of the 9 live rows contain legacy text. A narrowly-scoped legacy reader (`RunningBackgroundJsonConverter`, applied only via a property-level attribute on `ResolverInputSnapshot.Level`) is retained permanently for this boundary. |
| `DailyTipSets.Level` | 0 (all 5 rows null) | No legacy data; no action needed. |

**Exact implementation:**
- **Public HTTP request boundary** (`GeneratePreviewRequest.Level`): `RunningBackgroundCanonicalJsonConverter` — canonical values only, typed 400 for anything else. No legacy acceptance, no versioned legacy endpoint exists or was added.
- **EF/Postgres persistence boundary** (any `RunningBackground`/`RunningBackground?` entity property): `RunningBackgroundCompatibilityConverter` — retained as a permanent, documented safety net (this session's local dev database is not a stand-in for verifying the absence of legacy rows in any other environment) even though the one relational table found holding legacy data (`TrainingPlans`) was actively migrated.
- **Internal preview-snapshot JSON boundary** (`ResolverInputSnapshot.Level` only): `RunningBackgroundJsonConverter` — retained because the snapshot JSON is immutable/hash-verified and genuinely cannot be migrated in place.
- Frontend: **no compatibility retained anywhere** — there is no legacy local client store equivalent to the backend's persisted rows/snapshots.

## 3. UnsupportedCycleLength root cause

**Call chain traced:** `PlansController.GeneratePreview` → `PlanServices.GeneratePreviewAsync` → `LivePlanPreviewRoutingService.Decide` → `V1LiveCatalogPilotRoutingPolicy.Evaluate`.

**Raw week-count computation** (`LivePlanPreviewRouting.cs`, `Evaluate`): `cycleLength = Ceiling((request.RaceDate.DayNumber - asOfDate.DayNumber) / 7d)`, where `asOfDate = DateOnly.FromDateTime(DateTime.UtcNow)` (server date, computed fresh per request — no client-supplied `AsOfDate`).

**Root cause, precisely:** the range check that decided whether a computed cycle length was supported was a **hardcoded literal `< 8 or > 14`**, duplicated from — but not sourced from — the real candidate's master-template core-cycle (`plan-catalog/catalog/templates/ten-k-master.v6.json`: `coreCycle.minimumWeeks=8, defaultWeeks=12, maximumWeeks=14`). The literal happened to match the artifact exactly, so a genuinely 12-week-out request (e.g. `race_date` = today + 84 days) always correctly routed to `CatalogLive` when the artifact's DRAFT status was also overridden — **this was not, in itself, the failure previously observed in this session's HTTP verification**. Re-examining that specific earlier request: it used `race_date: "2026-12-01"` against a server date of `2026-07-16` — a ~20-week-out race, genuinely outside the `[8,14]` window by the (also correct) old hardcoded check. That specific failure was a test-input choice, not a code defect.

The **actual defect** — the reason this fix was still required — is architectural: the `[8,14]` bounds were a **duplicated literal with no connection to the real artifact**. Any future change to `TEN_K_MASTER`'s `coreCycle` (a version bump, a new candidate) would silently desynchronize this gate from the real, authoritative bounds, with no compile-time or even obvious runtime signal — the routing policy and the downstream `CatalogVolumeAndLongRunPlanner` (which *does* read the real artifact via `PlanCatalogBundleLoader.ReadCoreCycle`) could disagree on what "supported" means.

**Fix ownership:** `V1LiveCatalogPilotRoutingPolicy.Evaluate` (the routing-layer gate) — the same component that already owned the decision, now sourcing its bounds from the same place `CatalogVolumeAndLongRunPlanner` already did (`PlanCatalogCandidateSummary.CoreCycle`, read via `IPlanCatalogBundleLoader.LoadCandidateAsync`).

## 4. Cycle-selection fix

**Before:**
```csharp
if (cycleLength is null or < 8 or > 14) { ... UnsupportedCycleLength ... }
```

**After:**
```csharp
var withinSupportedCycle =
    cycleLength is { } weeks &&
    candidateMinimumWeeks is { } min &&
    weeks >= min &&
    (candidateMaximumWeeks is not { } max || weeks <= max);
if (!withinSupportedCycle) { ... UnsupportedCycleLength ... }
```
`candidateMinimumWeeks`/`candidateMaximumWeeks` are new required parameters on `Evaluate`, sourced by `LivePlanPreviewRoutingService.Decide` from `PlanCatalogCandidateSummary.CoreCycle` (the same real, on-disk `TEN_K_MASTER v6` artifact `CatalogVolumeAndLongRunPlanner` already trusted). `Decide` was restructured: identity-mismatch and missing-race-date requests still short-circuit with zero bundle I/O (unchanged from before); any pilot-identity-matching request with a race date now loads the candidate **before** the cycle-length check can run (previously, an out-of-range cycle length short-circuited before that load) — this is a deliberate, necessary consequence of sourcing real bounds: "out of range" cannot be determined without first knowing the real range. The `LivePlanPreviewRouteDecisionValidator`'s own redundant hardcoded `< 8 or > 14` invariant check was also removed (replaced with a non-null check only), since it duplicated the same drift risk in a second location.

**This belongs in the routing policy/service** because that is the single, already-existing owner of "is this cycle length supported for live routing" — the alternative locations named in the task (candidate duration resolver, prescription planner) already correctly read the artifact; the routing layer was the one place still guessing.

The 8-week-explicit-zero infeasibility check (`cycleLength == 8 && RecentWeeklyVolumeKm == 0`) was left untouched — same literal `8`, same behavior, per explicit task instruction not to touch it.

## 5. Intermediate successful HTTP flow

Request (Development environment, `Auth:Provider=Mock`, `CatalogLivePilot:Enabled=true`, `LocalCatalogAcceptance:{Enabled,TreatPilotCandidateAsPublished,EnableCatalogRoute}=true`, real Postgres):

```json
POST /api/v1/plans/generate-preview
{
  "goal_type": "race", "goal_distance": "ten_k", "level": "intermediate", "days_per_week": 4,
  "unit": "km", "race_name": "Fall 10K", "race_date": "2026-10-08" (84 days from server date 2026-07-16 = 12 weeks),
  "target_finish_time_seconds": 3300, "preferred_days": "Mon,Wed,Fri,Sun", "long_run_day": "Sun",
  "recent_weekly_volume_km": 30, "recent_longest_run_km": 12,
  "recent_race_distance_km": 10, "recent_race_finish_time_seconds": 3200, "recent_race_date": "2026-05-01"
}
```

**Result:** HTTP `200`. Routing log: `route=CatalogLive, candidateKey=TEN_K__4D__INTERMEDIATE, candidateVersion=10, cycleLengthWeeks=12, lifecycleStatus=PUBLISHED (local-acceptance override; real on-disk status remains DRAFT, logged at Warning level), reason=PilotPublishedAndActivated`. Response: `preview_id=991b534c-6a0d-4172-a584-4648806ad105`, `template_id=TEN_K__4D__INTERMEDIATE`, 12 weeks returned.

## 6. Intermediate confirmation

```
POST /api/v1/plans/confirm { "preview_id": "991b534c-..." } → 200, plan_id=f0ca08fd-50e7-465e-abe2-3c5734a2d316, status=active
POST /api/v1/plans/confirm { "preview_id": "991b534c-..." } (repeated) → 200, SAME plan_id=f0ca08fd-...
```
Relational verification (real Postgres): `TrainingPlans` row count for this id = 1 (no duplicate created by the repeated confirm); `GenerationSource=CATALOG`, `Level=intermediate`, `DaysPerWeek=4`. `TrainingWeeks` count = 12. `TrainingDays`: 4 per week × 12 weeks = 48 total, day-type breakdown `easy=28, interval=3, long_run=12, tempo=5`, **0 rows with `DayType='rest'`**.

## 7. Cycle matrix (via real production classes: `V1LiveCatalogPilotRoutingPolicy.Evaluate` + real bundle-loaded `CoreCycle`, and `LivePlanPreviewRoutingService.Decide` end-to-end)

| Scenario | Weeks | Outcome |
|---|---|---|
| Minimum supported | 8 | `CatalogLive` (with non-zero recent volume; separately, `RecentWeeklyVolumeKm=0` at 8 weeks → `CatalogGenerationInfeasible`/`KnownInfeasibleEightWeekExplicitZero`, unchanged) |
| Default | 12 | `CatalogLive` — also proven via full `Decide()` HTTP-equivalent path with the real on-disk DRAFT candidate |
| Maximum supported | 14 | `CatalogLive` |
| Between-supported | 9, 10, 11, 13 | `CatalogLive` (inclusive range, not a discrete `{8,12,14}` set — confirmed and preserved) |
| Above maximum | 15, 20 | `CatalogRequestUnsupported`/`UnsupportedCycleLength` — no clamp, no fallback (verified both in isolation and through the full `Decide()` path, including with the local-acceptance override active, proving the override never touches cycle bounds) |
| Below minimum (insufficient time) | 1, 5, 7 | `CatalogRequestUnsupported`/`UnsupportedCycleLength` |
| 8-week explicit-zero | 8, volume=0 | `CatalogGenerationInfeasible`/`KnownInfeasibleEightWeekExplicitZero` — unchanged, not "fixed" |
| Deterministic AsOfDate | 12 (fixed `AsOfDate`) | Identical decision on repeated evaluation with the same inputs |

Real candidate `CoreCycle` bounds confirmed via `PlanCatalogBundleLoader` against the actual on-disk artifact: `minimum=8, maximum=14` (matches `ten-k-master.v6.json`'s `coreCycle`).

## 8. Frontend cleanup

- `mobile/lib/core/models/running_background.dart`: `parse()`/`tryParse()` now reject `new_to_running`/`used_to_run`/`running_regularly` (previously mapped them to canonical values) — throws `FormatException`, matching the backend's public-boundary behavior. Only 4 enum values exist; no fifth value added.
- `mobile/lib/features/onboarding/presentation/plan_preview_page.dart`: `_levelLabel` display-mapping's 3 legacy-value switch cases removed (dead code — the backend never emits them and the model no longer accepts them).
- `mobile/test/running_background_v2_test.dart`: legacy-acceptance test replaced with a rejection test.
- Confirmed via repo-wide search: zero remaining `new_to_running`/`used_to_run`/`running_regularly` references anywhere under `mobile/lib/` (only in the updated test file and in `running_background.dart`'s doc comment, which documents their absence, not their presence).

## 9. Backend cleanup

- **Public JSON boundary**: `RunningBackgroundCanonicalJsonConverter` (new, canonical-only) is now `RunningBackground`'s type-level default and `GeneratePreviewRequest.Level`'s property-level override. Verified via live HTTP: all 3 legacy aliases → typed 400 (`$.level: "Unknown RunningBackground value '...'. ... were removed in Running Background V2.1..."`); all 4 canonical values accepted.
- **Persistence boundary**: `RunningBackgroundCompatibilityConverter` retained (read-compat only, canonical-only write), 320 real legacy `TrainingPlans` rows migrated to canonical.
- **Pilot mapping**: `V1CatalogPilotIdentityPolicy.Level = RunningBackground.Intermediate` — confirmed no reference anywhere in production code to `RunningRegularly`/`used_to_run`/`running_regularly` as an active identifier (only in historical doc comments and migration snapshots).
- **Swagger/OpenAPI**: `DtoExamplesSchemaFilter.cs` fixed (5 `"new_to_running"` → `"beginner"`); live-served `/swagger/v1/swagger.json` confirmed to contain zero legacy value occurrences.

## 10. Legacy occurrence classification (full repo search)

| File | Classification |
|---|---|
| `RunningBackgroundCompatibilityConverter.cs` | `MIGRATION_COMPATIBILITY` (retained, persistence boundary) |
| `RunningBackgroundJsonConverter.cs` | `MIGRATION_COMPATIBILITY` (retained, internal snapshot boundary) |
| `ResolverInputSnapshot.cs` | `MIGRATION_COMPATIBILITY` (consumer of the above) |
| `RunningBackgroundV2Tests.cs` | `TEST_FIXTURE_REQUIRING_UPDATE` → updated (now tests rejection at bare/public boundaries, acceptance only at the explicit historical-compat converter) |
| `UserJourneyTests.cs` | `TEST_FIXTURE_REQUIRING_UPDATE` → updated (canonical values in request fixtures; stale regression comment corrected) |
| `20260716185115_..._MigrateLegacyTrainingPlanLevels.cs` (new) | `MIGRATION_COMPATIBILITY` (the fix itself) |
| `20260716175426_RunningBackgroundV2FourLevelModel.cs` | `HISTORICAL_DOCUMENTATION` (prior migration, untouched) |
| `RunningBackground.cs`, `RunningBackgroundCanonicalJsonConverter.cs`, `GeneratePreviewRequest.cs` | `HISTORICAL_DOCUMENTATION` (doc comments describing what is rejected, not accepting it) |
| `V1CatalogPilotIdentityPolicy.cs`, `AppDbContext.cs` | `HISTORICAL_DOCUMENTATION` |
| `DtoExamplesSchemaFilter.cs` | `STALE_ACTIVE_CODE` → **fixed** (Swagger examples used a literal legacy value) |
| `FitnessEvidenceInputContractTests.cs`, `ResetEndpointRelationalScenarioTests.cs` | `TEST_FIXTURE_REQUIRING_UPDATE` → **fixed** (unrelated tests using stale example values as filler) |
| All `Migrations/*.Designer.cs`, `InitialCreate.cs` | `HISTORICAL_DOCUMENTATION` (auto-generated EF snapshots — never rewritten) |
| `plan-catalog/artifacts/audits/*.json`, `PilotDomainContentAudit.cs` | `HISTORICAL_DOCUMENTATION` (audit records describing Phase 4F.8.2-era decisions using the terminology current at that time — not rewritten, per "do not rewrite historical audit facts") |

No `STALE_ACTIVE_CODE` remains after the fixes above.

## 11. Advanced/Experienced boundary

Both values are accepted by the canonical contract (typed 200-eligible requests), never coerced to Intermediate (`V1CatalogPilotIdentityPolicy.IsSupportedIdentity` exact-matches only `Intermediate`; confirmed via live HTTP: Advanced/Experienced against the pilot's exact Race/TenK/4-day shape correctly route `Legacy`/`NotPilotRequest`, then `404 PLAN_TEMPLATE_NOT_FOUND` — no template fabricated, no candidate created). This is an accepted, pre-existing, deferred gap — plan authoring for these levels is out of scope for this closure and is not treated as a defect here.

## 12. Test results

- **Backend** (`RunningApp.sln`): build clean (0 warnings, 0 errors), `dotnet test` → **861/861 passing** (was 839 before this closure; +22 net new: cycle-matrix tests, updated legacy-rejection tests, historical-compat-converter test).
- **PlanCatalog** (`PlanCatalog.sln`): build clean, `dotnet test` → **335/335 passing**, unaffected.
- **EF**: `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration."
- **Frontend**: `flutter analyze` → 0 errors/warnings introduced (142 pre-existing lint infos, unrelated). `flutter test` → **14/14 passing** in the Running Background suite + api_client test; full suite 14 pass / 1 pre-existing unrelated failure (`widget_test.dart`, a Firebase-initialization gap confirmed unrelated in the prior V2 phase via git-stash baseline reproduction).
- **HTTP/relational**: real Postgres, real local API, Development-only local-acceptance override — successful Intermediate preview → confirm → repeated confirm (idempotent, single row) → persisted round-trip (1 plan, 12 weeks, 48 days, 0 rest days, `GenerationSource=CATALOG`). Legacy alias rejection (3/3) and canonical acceptance (4/4, with Advanced/Experienced correctly 404ing rather than fabricating a plan) verified live.

## 13. Files created

- `backend/RunningApp.Domain/Enums/RunningBackgroundCanonicalJsonConverter.cs`
- `backend/RunningApp.Persistence/Migrations/20260716185115_RunningBackgroundV2_1_MigrateLegacyTrainingPlanLevels.cs` (+ `.Designer.cs`)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/RunningBackgroundV2_1CycleMatrixTests.cs`
- `RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md` (this file)
- `plan-catalog/artifacts/audits/running-background-v2-1-intermediate-pilot-closure.json`

## 14. Files modified

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/LivePlanPreviewRouting.cs` (cycle-selection fix, validator simplification, log enrichment, `Decide()` restructure)
- `backend/RunningApp.Domain/Enums/RunningBackground.cs` (type-level converter → canonical-only)
- `backend/RunningApp.Domain/Enums/RunningBackgroundJsonConverter.cs` (re-scoped doc comments — historical-compat-only role)
- `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs` (property-level converter → canonical-only)
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/ResolverInputSnapshot.cs` (doc comment clarification only — converter unchanged)
- `backend/RunningApp.Persistence/Converters/RunningBackgroundCompatibilityConverter.cs` (doc comment update)
- `backend/RunningApp.Api/Swagger/DtoExamplesSchemaFilter.cs` (5 legacy example values → canonical)
- `backend/RunningApp.IntegrationTests/RunningBackgroundV2Tests.cs` (legacy tests updated for the new contract)
- `backend/RunningApp.IntegrationTests/UserJourneyTests.cs` (fixture values + stale comment)
- `backend/RunningApp.IntegrationTests/PlanGeneration/FitnessEvidenceInputContractTests.cs` (fixture values)
- `backend/RunningApp.IntegrationTests/ResetEndpointRelationalScenarioTests.cs` (fixture value)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F8_2LivePilotRoutingTests.cs` (updated `Evaluate` call signatures)
- `mobile/lib/core/models/running_background.dart` (legacy alias handling removed)
- `mobile/lib/features/onboarding/presentation/plan_preview_page.dart` (legacy display-mapping cases removed)
- `mobile/test/running_background_v2_test.dart` (legacy-acceptance test → rejection test)
- `RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md`, `plan-catalog/artifacts/audits/running-background-v2-alignment.json` (both amended with a pointer to this closure, not rewritten — see §16)

## 15. Remaining gaps

- Advanced/Experienced plan authoring is deferred (explicitly out of scope — not a defect).
- The `already_active` flag on a repeated `confirm` call for the *same* preview_id after the plan is already active returns `false` rather than `true` on both calls in this session's manual HTTP check — idempotency (same `plan_id`, no duplicate row) is proven regardless, but this flag's exact semantics for repeated-same-preview confirms were not deeply audited here and may warrant a small follow-up look.
- Read DTOs (home/calendar/day-detail) still don't expose catalog phase/stage/workout-identity fields — flagged in Phase 4F.9.3, unchanged, out of scope here.

## 16. Documentation and audit note

`RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md` and `plan-catalog/artifacts/audits/running-background-v2-alignment.json` describe the V2 (not V2.1) state as of their original writing and are **not retroactively rewritten** here (their facts were true when written — e.g., the legacy JSON converter was universally applied at that time). This document supersedes them for anything relating to legacy-alias handling and cycle selection; both older artifacts remain as an accurate historical record of the V2 milestone.
