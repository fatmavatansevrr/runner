# Local Catalog Acceptance Test

Manual end-to-end HTTP walkthrough of the unpublished TEN_K__4D__INTERMEDIATE
v10 catalog pilot: onboarding → preview → confirm → PostgreSQL persistence →
home/calendar/day-detail, without publishing the candidate.

## Prerequisites

- Docker Desktop running.
- .NET 9 SDK.
- Repo checked out at `c:\Users\vatan\Desktop\runner`.

## 1. Start PostgreSQL

```bash
docker compose up -d postgres
docker exec appsel-dev-postgres pg_isready -U postgres -d antigravity_dev
```

## 2. Apply migrations

```bash
dotnet ef database update --project backend/RunningApp.Persistence --startup-project backend/RunningApp.Api
```

## 3. Start the API with the local-acceptance override

The candidate stays `DRAFT` on disk. These environment variables activate a
**Development-only** in-memory routing override (see
`LocalCatalogAcceptanceOptions` in
`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/LivePlanPreviewRouting.cs`)
that lets this specific request identity reach the catalog path without
publishing anything. All three flags default `false`; every one must be
explicitly `true`, and the process must run with `ASPNETCORE_ENVIRONMENT=Development`.

```bash
cd backend/RunningApp.Api
ASPNETCORE_ENVIRONMENT=Development \
LocalCatalogAcceptance__Enabled=true \
LocalCatalogAcceptance__TreatPilotCandidateAsPublished=true \
LocalCatalogAcceptance__EnableCatalogRoute=true \
CatalogLivePilot__Enabled=true \
dotnet run --no-launch-profile --urls http://localhost:5231
```

`CatalogLivePilot__Enabled=true` is the pre-existing (default-false, already
documented) production activation flag — it must ALSO be set for the local
run, independently of the new override, exactly as it would need to be in a
real published-and-activated environment.

Confirm startup:

```bash
curl http://localhost:5231/health
curl http://localhost:5231/health/database
```

Swagger UI (Development only): `http://localhost:5231/swagger/index.html`

## 4. Authentication

`appsettings.Development.json` sets `Auth:Provider=Mock`. No token/header is
required — `MockAuthMiddleware` resolves every request to the fixed mock user
(`mock-user-001`) and auto-creates/synchronizes its `Users`/`UserProfile` row
on first use. Plain `curl`/Postman requests authenticate automatically.

## 5. Reset before each run

```bash
curl -X POST http://localhost:5231/api/v1/testing/reset
```

Development-only: `TestingController.ResetDatabase` returns 403 outside
Development. Deletes the mock user's own plans/previews/logs/etc. only.

## 6. Preview request (exact payload)

```bash
curl -X POST http://localhost:5231/api/v1/plans/generate-preview \
  -H "Content-Type: application/json" \
  -d '{
    "goal_type": "race",
    "goal_distance": "ten_k",
    "level": "running_regularly",
    "days_per_week": 4,
    "unit": "km",
    "race_date": "2026-10-04",
    "preferred_days": "Mon,Wed,Fri,Sun",
    "long_run_day": "Sun",
    "recent_weekly_volume_km": 20,
    "recent_longest_run_km": 8,
    "recent_runs_per_week": 3
  }'
```

Adjust `race_date` to `today + (8..14)*7` days for a supported cycle. Field
names/enum values come directly from `GeneratePreviewRequest` (snake_case,
verified against the actual API contract, not guessed).

**Checks**: HTTP 200; `template_id == "TEN_K__4D__INTERMEDIATE"`; `weeks` has
the requested cycle length; every week has exactly 4 `days`; no day has
`day_type` indicating rest; the taper week's key session has
`intensity: "EASY_WITH_CONTROLLED_SHARPENING"` (the only public signal
distinguishing TAPER_SHARPEN in this response — see §12 of the accompanying
report for what is NOT exposed at this layer).

Other three request variants:

- **Missing weekly volume** (omit `recent_weekly_volume_km`): 200, `NOT_PROVIDED` policy (16km default) applies internally.
- **Explicit-zero, 12-week (supported)**: same payload with `"recent_weekly_volume_km": 0` and a 12-week `race_date`: 200.
- **Known unsupported 8-week explicit-zero**: `race_date` exactly 8 weeks out (`today + 56 days`) with `"recent_weekly_volume_km": 0`: HTTP 422, `errorCode: "CATALOG_LIVE_PILOT_GENERATION_INFEASIBLE"`, no preview/plan rows created.

## 7. Confirm

```bash
curl -X POST http://localhost:5231/api/v1/plans/confirm \
  -H "Content-Type: application/json" \
  -d '{"preview_id": "<preview_id from step 6>"}'
```

Call it twice — both times return the identical `plan_id`.

## 8. Verify in PostgreSQL

```sql
SELECT "Id", "GenerationSource", "CatalogCandidateKey", "CatalogCandidateVersion",
       "CatalogPreviewContentHash" IS NOT NULL AS has_hash
FROM "TrainingPlans" WHERE "Id" = '<plan_id>';

SELECT COUNT(*) FROM "TrainingWeeks" WHERE "PlanId" = '<plan_id>';
SELECT COUNT(*) FROM "TrainingDays"  WHERE "PlanId" = '<plan_id>';
```

Expect `GenerationSource='CATALOG'`, `CatalogCandidateKey='TEN_K__4D__INTERMEDIATE'`,
`CatalogCandidateVersion=10`, `has_hash=t`, week count matching the requested
cycle, day count = week count × 4 (no rest rows).

## 9. Home / Calendar / Day detail

```bash
curl http://localhost:5231/api/v1/plans/active/home
curl "http://localhost:5231/api/v1/plans/active/calendar?month=2026-07"
curl "http://localhost:5231/api/v1/plans/active/details"
curl http://localhost:5231/api/v1/training-days/<day_id from calendar>
```

**Checks**: home/calendar/day-detail all show real persisted distance,
`day_type`, and `intensity` on actual session dates; calendar synthesizes a
`"rest"` placeholder (zero GUID `day_id`) on dates with no persisted
`TrainingDay` — this is existing, intentional API behavior shared with the
legacy path, not data loss. Dates render as `...T00:00:00Z` consistently, no
off-by-one shift observed near the July/August boundary in this run.

**Known gap** (see accompanying report §12): none of these three endpoints
expose `CatalogPhaseKey`, `CatalogProgressionStageKey`,
`CatalogWorkoutDefinitionKey/Version`, `CatalogStructuralRole`, or the ordered
`CatalogPrescriptionJson` segments. The only public signal distinguishing
catalog-sourced richness (e.g. TAPER_SHARPEN) is the free-text `intensity`
string. This is a real, reported gap — not fixed in this phase (redesigning
these three response DTOs was judged too large a change to make silently).

## 10. Reset and repeat

```bash
curl -X POST http://localhost:5231/api/v1/testing/reset
```

Then repeat step 6 with identical inputs — distance/session values are
deterministic for the same request and the same day (`AsOfDate` is derived
from wall-clock date only).

## Shutdown

```bash
docker compose stop postgres
```

Destructive full reset (local development only — deletes all data):

```bash
docker compose down -v
```

## Troubleshooting

- `CATALOG_CANDIDATE_NOT_PUBLISHED` on preview: the local-acceptance override
  isn't active — check all three `LocalCatalogAcceptance__*` env vars are
  `true` AND `ASPNETCORE_ENVIRONMENT=Development` AND `CatalogLivePilot__Enabled=true`.
- `/api/v1/testing/reset` returns 403: not running in Development.
- `pending_migrations > 0` from `/health/database`: re-run the migration command in step 2.
- Confirm returns `CATALOG_ACTIVE_PLAN_CONFLICT` (409): reset first, or confirm the intended preview belongs to a user with no existing active plan.
