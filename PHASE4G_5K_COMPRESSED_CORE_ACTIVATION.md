# Phase 4G.5K — Compressed Core Activation

> This phase intentionally widens the live caller introduced in 4G.5J to compressed 8–11-week cores. The 4G.5D–5I zero-live-caller invariant is therefore superseded only by the single `CatalogPreviewGenerator` composition point.

> **Scope clarification (Phase 4G.5O):** 4G.5K widened the same runtime composition point. Its 8–14 matrix was Development `LocalCatalogAcceptance` and runtime acceptance evidence, not a production deployment claim. Published lifecycle/release compatibility and normal non-Development configuration were still required; production activation remains deferred.

## Outcome

8, 9, 10, and 11 weeks returned HTTP 200 through the same dynamic typed pipeline used by 13–14 in the Development/runtime acceptance matrix. That matrix was 8–14 standalone HTTP 200; 15+ remained HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`. Preparation Runway was not activated.

The 4G.5J wiring was reused, not duplicated. 4G.5K is a policy-range widening over the same classifier-to-orchestrator composition.

## Compressed-core safety

Development `LocalCatalogAcceptance` real-host requests revalidate `TD-FOUNDATION-COMPRESSION-001`: exact requested week counts are produced for 8–11, all phase minima hold through the existing final validation chain, and readiness does not influence allocation. The previously proven workout binding, volume/long-run progression, pace prescription, calendar, final validators, and persistability checks execute at the single live composition point without silent fallback.

Partial-day classification uses full elapsed weeks as the allocation target and retains the remainder as classifier metadata; it does not round into an extra allocated week.

## E2E matrix

The Development `LocalCatalogAcceptance` real-host relational matrix runs N=8,9,10,11,12,13,14. Every case verifies:

- HTTP 200 and exactly N weeks / 4N sessions;
- `fallback_used=false`;
- successful confirmation and exact N `TrainingWeek` / 4N `TrainingDay` persistence;
- valid active-plan details, home summary, and calendar data;
- successful reset before and after the flow.

For every N=8–14, a real NotEvaluated-triggering request returns HTTP 422 `RUNTIME_CONDITION_UNSUPPORTED`, includes `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`, and persists nothing. `TD-NOTEVALUATED-FALLBACK-001` remains open solely for its deferred frontend UX decision.

## Boundaries

No Preparation Runway type is constructed and 15+ remains composition-required. No internal orchestrator, allocator, or verifier logic was changed in this runtime-activation pass. Catalog publication and production deployment activation were not performed in 4G.5K. No commit or push was performed.
