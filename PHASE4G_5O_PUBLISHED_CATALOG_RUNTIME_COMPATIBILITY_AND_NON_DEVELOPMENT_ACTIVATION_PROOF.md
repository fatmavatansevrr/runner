# Phase 4G.5O — Published Catalog Runtime Compatibility and Non-Development Activation Proof

## Executive verdict

`PUBLISHED_RUNTIME_COMPATIBILITY_COMPLETE_WITH_DEPLOYMENT_DECISIONS`

The backend can consume the standard publisher's immutable release layout, including the complete `TEN_K__4D__INTERMEDIATE` v10 dependency closure. A real `Production` test host with Development `LocalCatalogAcceptance` disabled, `CatalogLivePilot.Enabled=true`, and its catalog root set to that publisher-produced release completed the normal lifecycle and public read-model flow. No deployed environment, source lifecycle, candidate pin, or Preparation Runway behavior was changed.

## Canonical filesystem contracts

Repository publishing architecture establishes the immutable publisher-produced release as the canonical production runtime artifact. The authoring tree remains the mutable development/catalog-authoring input.

- Authoring root: no `release-manifest.json`; filenames are human/source conventions. Resolution is deterministic metadata identity matching within the typed subfolder.
- Published root: contains `release-manifest.json` and `checksums.sha256`; filenames are `{ARTIFACT_KEY}.v{version}.json`. Resolution requires manifest membership, the canonical key/version path, checksum equality, and metadata identity equality.
- Backend root: `PlanCatalog:CatalogRootPath` may intentionally point to either layout. The manifest is the explicit layout marker; the resolver does not probe alternate aliases.

## Loader compatibility audit

| Artifact | Required identity | Authoring path | Published path | Resolution | Classification |
|---|---|---|---|---|---|
| Candidate | `TEN_K__4D__INTERMEDIATE` v10 | `combinations/ten-k-4d-intermediate.v10.json` | `combinations/TEN_K__4D__INTERMEDIATE.v10.json` | shared resolver | `DUAL_COMPATIBLE` |
| Master | `TEN_K_MASTER` v6 | `templates/ten-k-master.v6.json` | `templates/TEN_K_MASTER.v6.json` | shared resolver | `DUAL_COMPATIBLE` |
| Layout | `RUN_LAYOUT_4D` v2 | `layouts/run-layout-4d.v2.json` | `layouts/RUN_LAYOUT_4D.v2.json` | shared resolver | `DUAL_COMPATIBLE` |
| Level modifier | `INTERMEDIATE_MODIFIER` v6 | `level-modifiers/intermediate-modifier.v6.json` | `level-modifiers/INTERMEDIATE_MODIFIER.v6.json` | shared resolver | `DUAL_COMPATIBLE` |
| Rule pack | `APPSEL_RACE_PLAN_V1` v4 | `rule-packs/appsel-race-plan.v4.json` | `rule-packs/APPSEL_RACE_PLAN_V1.v4.json` | shared resolver | `DUAL_COMPATIBLE` |
| Workout progression | `TEN_K_WORKOUT_PROGRESSION_V1` v5 | `workout-progressions/ten-k-workout-progression.v5.json` | `workout-progressions/TEN_K_WORKOUT_PROGRESSION_V1.v5.json` | shared resolver | `DUAL_COMPATIBLE` |
| Progression modifier | `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v2 | `progression-modifiers/intermediate-progression-modifier.v2.json` | `progression-modifiers/INTERMEDIATE_PROGRESSION_MODIFIER_V1.v2.json` | bundle identity resolution | `DUAL_COMPATIBLE` |
| Workouts | five identities listed below | source-convention files under `workouts/` | `{KEY}.v{version}.json` under `workouts/` | shared resolver | `DUAL_COMPATIBLE` |
| Peak bands | `PEAK_VOLUME_BANDS_V1` v3 | `policies/peak-volume-bands.v3.json` | `policies/PEAK_VOLUME_BANDS_V1.v3.json` | shared resolver | `DUAL_COMPATIBLE` |
| Runtime conditions | `RUNTIME_CONDITION_VALUES_V1` v2 | `registries/runtime-condition-values.v2.json` | `registries/RUNTIME_CONDITION_VALUES_V1.v2.json` | shared resolver | `DUAL_COMPATIBLE` |
| Pace/prescription dependencies | rule pack, registry, peak policy and workout definitions above | typed source folders | typed release folders | owning loaders/shared resolver | `DUAL_COMPATIBLE` |

The compatibility defect was not isolated to one alias: the peak loader encoded the source filename directly, while other loaders relied on metadata scanning. `CatalogArtifactFileResolver` now provides one identity-based dual-layout contract for all runtime file loaders. Published loads additionally enforce manifest/checksum integrity.

## Published v10 closure and fixture

The test fixture invokes the real `CatalogPublisher`, JSON-schema validator, bundle assembler, serializer, SHA-256 hasher, filesystem published repository, release manifest, and checksum index. It does not copy a hand-picked fake release or alter authoring files. A test-only source-repository decorator promotes DRAFT metadata to VALIDATED in memory so the real publisher can exercise normal output lifecycle and dependency validation; publisher output is PUBLISHED.

The required closure is:

- candidate `TEN_K__4D__INTERMEDIATE` v10;
- direct dependencies `TEN_K_MASTER` v6, `RUN_LAYOUT_4D` v2, `INTERMEDIATE_MODIFIER` v6, and `APPSEL_RACE_PLAN_V1` v4;
- progression `TEN_K_WORKOUT_PROGRESSION_V1` v5 and modifier `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v2;
- workouts `EASY_STANDARD` v4, `LONG_RUN_STANDARD` v4, `FARTLEK` v4, `THRESHOLD_TEMPO` v4, and `GOAL_PACE_TEN_K` v2;
- `PEAK_VOLUME_BANDS_V1` v3 and `RUNTIME_CONDITION_VALUES_V1` v2.

Executable compatibility tests assert all identities are present in the generated manifest, lifecycle is PUBLISHED, every checksum entry matches its file, all runtime loaders succeed, and the temporary published root is independent of `plan-catalog/catalog`. Therefore `SOURCE_TREE_FALLBACK_USED=NO`.

## Non-Development lifecycle evidence

The E2E host uses `Environment=Production`, test-local mock authentication, `CatalogLivePilot.Enabled=true`, all `LocalCatalogAcceptance` switches false, and the temporary published release root. Mock authentication is only a test identity mechanism; candidate status, loaders, lifecycle gate, generation, persistence, confirmation, and reads are real. A separate Development host is used only for the repository's test reset endpoint; the requests under proof never receive a Development lifecycle override.

| Request | Result | Evidence |
|---|---|---|
| 8w0d | HTTP 200 | 8 weeks, 32 sessions, no fallback, PUBLISHED v10 provenance, confirm and reads pass |
| 12w0d | HTTP 200 | PreferredCore, 12 weeks, 48 sessions, same lifecycle/read evidence |
| 13w0d | HTTP 200 | 13 weeks, 52 sessions, same lifecycle/read evidence |
| 14w0d | HTTP 200 | 14 weeks, 56 sessions, same lifecycle/read evidence |
| 14w1d | HTTP 422 | `PLAN_HORIZON_COMPOSITION_REQUIRED`; no new plan graph persisted |
| NotEvaluated | HTTP 422 | `RUNTIME_CONDITION_UNSUPPORTED`, `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`; no new plan graph persisted |
| unsupported advanced combination | non-200 | remains outside catalog scope; no new plan graph persisted |

For successful horizons, active details, home, calendar, and representative training-day detail reconcile to the confirmed plan. Preparation Runway remains inactive.

## Rollback, retirement, and version transition

With the same published release and `CatalogLivePilot.Enabled=false`, the established legacy behavior is HTTP 404 `PLAN_TEMPLATE_NOT_FOUND` for 8, 12, 13, and 14 weeks, with no new plan graph persistence. This is the observed rollback contract; it is not rewritten into a new 422 policy.

A preview created while enabled can be confirmed after the flag is disabled. Its resulting active plan remains readable through details, home, and calendar. Confirmation consumes the frozen snapshot rather than reevaluating route eligibility.

When v10 is excluded from a newly publisher-produced release through the retirement ledger, new v10 preview routing fails HTTP 422 `CATALOG_LIVE_PILOT_REQUEST_UNSUPPORTED`. A previously frozen v10 preview still confirms, and the active plan remains readable after retirement. Published v9 and v10 can coexist; `V1CatalogPilotIdentityPolicy` remains pinned to v10 and does not select v9 or a newer version implicitly.

## Documentation correction

Phase 4G.5J now describes 13–14 as runtime activation at the single live composition point and scopes its original HTTP evidence to Development `LocalCatalogAcceptance`. Phase 4G.5K likewise scopes the 8–14 matrix to Development/runtime acceptance. Implementation activation, catalog publication, and deployment activation are distinct; actual production activation remains deferred.

## External deployment configuration contract

No deployment manifest or real environment was changed. A future deployment must provide and own at least:

```text
ASPNETCORE_ENVIRONMENT=Production
Auth__Provider=Firebase
Auth__Firebase__ProjectId=<deployment project id>
GOOGLE_APPLICATION_CREDENTIALS=<deployment-managed service-account path>
PlanCatalog__CatalogRootPath=<immutable publisher-produced release root>
CatalogLivePilot__Enabled=true
LocalCatalogAcceptance__Enabled=false (or absent)
LocalCatalogAcceptance__TreatPilotCandidateAsPublished=false (or absent)
LocalCatalogAcceptance__EnableCatalogRoute=false (or absent)
ConnectionStrings__DefaultConnection=<deployment-managed value>
```

`DEPLOYMENT_OWNER=DECISION_REQUIRED`: repository evidence does not identify the configuration owner. Enabling or changing these values requires a restart/redeployment because options are bound at host construction. Readiness verification must check release manifest/checksum accessibility, database connectivity, one supported preview/confirmation/read flow, fail-closed 14w1d behavior, and logs/health endpoints available to the deployment. Rollback is to set `CatalogLivePilot__Enabled=false` and restart/redeploy; frozen previews and already active plans remain usable under the verified current architecture.

## Boundaries

No real production environment was modified, no source authoring lifecycle was changed, no published test release was retained, no candidate identity was changed, and no Preparation Runway/cohort/pace/allocation/workout/volume/calendar behavior was activated or altered. No commit, stage, or push is part of this phase.
