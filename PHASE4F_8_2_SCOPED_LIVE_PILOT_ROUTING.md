# Phase 4F.8.2 Scoped Live Pilot Routing

## Supported Pilot Identity

Policy `V1_LIVE_CATALOG_PILOT_ROUTING_POLICY` version `1` is the only live catalog preview routing policy. It matches only typed requests with:

- `GoalType = Race`
- `GoalDistance = TenK`
- `Level = RunningRegularly` as the backend's established intermediate stand-in
- `DaysPerWeek = 4`
- valid race date producing an 8-14 week cycle
- candidate `TEN_K__4D__INTERMEDIATE` v10

5K, half marathon, marathon, habit plans, non-intermediate stand-ins, non-4-day requests, missing race dates, and unsupported cycle lengths are not catalog-live routes.

## Route Decision Model

`LivePlanPreviewRouteDecision` records policy key/version, typed request identity, matched candidate key/version, candidate lifecycle status, activation state, cycle length, route outcome, reason code, fallback permission, and provenance. Outcomes are:

- `CatalogLive`
- `Legacy`
- `CatalogSupportedButNotPublished`
- `CatalogSupportedButActivationDisabled`
- `CatalogRequestUnsupported`
- `CatalogGenerationInfeasible`
- `RequestInvalid`

`LivePlanPreviewRouteDecisionValidator` rejects contradictory decisions, including any catalog-live route without `PUBLISHED`, activation enabled, pilot identity, and a supported cycle length.

## Lifecycle Gate And Activation Control

The activation control is `CatalogLivePilot:Enabled`, represented by `CatalogLivePilotOptions.Enabled`. Its default is `false`; no production default enables it.

The lifecycle/activation matrix is:

- DRAFT + disabled: non-live, exact legacy boundary if fallback is permitted.
- DRAFT + enabled: non-live, activation cannot override lifecycle.
- PUBLISHED + disabled: non-live, catalog-supported but activation-disabled.
- PUBLISHED + enabled: eligible for `CATALOG` generation.

The current v10 artifact remains `DRAFT`; this phase does not publish it and does not add a publication ledger entry.

## Preview Behavior

`LivePlanPreviewRoutingService` is the production `IGenerationRouteDecider`. It evaluates the request once, loads the exact candidate lifecycle for pilot-shaped requests, logs sanitized structured route fields, validates the route decision, and returns either `CATALOG` or `LEGACY_SQL`. It does not invoke both generators or use catalog exceptions to select a normal route.

Catalog-live requests proceed through the existing `CatalogPreviewGenerator` path. The 4F.8.1 non-null `GeneratedCatalogPlanPayload`, snapshot content hash, public preview mapping, and `GenerationSource = CATALOG` provenance are preserved.

Legacy requests continue through the existing SQL/template generator. Exact-template miss semantics are unchanged: no silent fallback is restored, and missing templates still throw `PlanTemplateNotAvailableException`.

## Fallback Matrix

Fallback is permitted for:

- request shapes outside the catalog pilot that already have legacy support;
- catalog-supported pilot requests while the candidate is not published;
- catalog-supported pilot requests while activation is disabled.

Fallback is prohibited for:

- invalid requests;
- unsupported catalog cycles;
- 8-week explicit-zero readiness infeasibility;
- candidate/request mismatch;
- catalog artifact inconsistency;
- payload validation failure;
- snapshot/hash failure;
- goal-feasibility safety failure;
- public materialization failure;
- catalog confirmation.

## Unsupported 8-Week Explicit-Zero Path

The exact `8-week cycle + RecentWeeklyVolumeKm = 0` pilot path is classified as `CatalogGenerationInfeasible` with reason `KnownInfeasibleEightWeekExplicitZero`. It throws a typed routing failure before catalog or legacy generation, creates no snapshot, returns no payload, and does not silently increase weekly volume.

## Snapshot And Confirmation Boundary

No persistence schema or migration is added. Catalog snapshots continue to carry explicit `GenerationSource = CATALOG`, and legacy previews remain legacy response JSON. Confirmation dispatch uses stored provenance and does not rerun routing.

Catalog confirmation remains disabled and fail-closed: a catalog snapshot can validate but still throws `CatalogPreviewMaterializationNotImplementedException` before any `TrainingPlan`, `TrainingWeek`, or `TrainingDay` writes.

## Observability

Routing logs include policy key/version, selected route, candidate key/version, lifecycle status, activation state, reason code, and fallback permission. Logs do not include free-form onboarding text, full payloads, secrets, or normal route stack traces.

## Validation

Focused 4F.8.2 tests cover policy identity, typed pilot matching, lifecycle/activation matrix, default-disabled activation, deterministic decisions, service-level routing, generator exclusivity, invalid/no-fallback behavior, 8-week explicit-zero no-fallback behavior, and real v10 DRAFT status.

Existing 4F.8.1 preview materialization, snapshot/hash, and confirmation tests remain the regression surface for generated payload and confirmation separation.

## Publication And Activation Boundary

This phase implements the boundary only. Candidate publication, production activation, and catalog confirmation/persistence remain deferred.

## Next-Step Readiness

The live routing boundary is present and default-closed. Catalog confirmation/persistence can be implemented next without changing the publication status or enabling production activation.
