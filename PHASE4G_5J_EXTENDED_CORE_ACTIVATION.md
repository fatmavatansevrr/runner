# Phase 4G.5J — Extended Core Activation

> This is the first 4G.5 phase that intentionally permits a live caller of the pipeline proven dark in 4G.5D–5I. The former zero-live-caller invariant is lifted only at `CatalogPreviewGenerator` for the activated standalone-core horizons.

> **Scope clarification (Phase 4G.5O):** “Activation” here means runtime activation at the single live composition point. The original HTTP 200 evidence used real-host acceptance tests under Development `LocalCatalogAcceptance`. It did not establish production deployment activation. Normal non-Development activation still required a published lifecycle, published-release loader compatibility, and external `CatalogLivePilot` configuration; those technical proofs are recorded in Phase 4G.5O while actual production activation remains deferred.

## Outcome

13- and 14-week race requests were connected to the dynamic typed pipeline and returned HTTP 200 in Development `LocalCatalogAcceptance` real-host acceptance tests. During that ordered runtime-activation validation, 8–11 remained the next widening step, 12 retained its established path, and 15+ remained HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`.

## Governance gate

- `TD-VOLUME-CAP-UNENFORCED-001` remains `CLOSED`; its caveat is the current `TEN_K__4D__INTERMEDIATE` candidate/master and 8–14 range. The 42-case 4G.5F proof covers this activation.
- `TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001` remains `CLOSED`; its scope is future 15+ runway composition, which remains inactive.
- `TD-ALLOCATION-PRIORITY-001` remains `CLOSED`; its approved Foundation-first extension policy applies to the current candidate, producing 4/4/4/1 at 13 weeks and 4/5/4/1 at 14 weeks.
- `TD-FOUNDATION-COMPRESSION-001` remains `CLOSED`; no phase falls below its minimum for N=8–14 and readiness still has no allocation influence.
- `TD-COREHORIZON-ALLOCATOR-UNWIRED-001` is resolved and closed before activation: `CoreHorizonDecision` is the upstream eligibility gate and sole target-week source; the allocator receives `AvailableFullWeeks` and owns only phase distribution.
- `TD-NOTEVALUATED-FALLBACK-001` remains `OPEN`: real 13/14 requests using the NotEvaluated-triggering pace source return HTTP 422 `RUNTIME_CONDITION_UNSUPPORTED` before scheduling, identical to 12 weeks. Frontend UX remains deferred.
- `TD-PACESOURCE-001` and `TD-PACESOURCE-002` remain open but do not block horizon activation: this change reuses the existing resolver/snapshot lifecycle and introduces no pace-source method or timestamp policy.

## Runtime composition diff

- `RaceHorizonPolicy`: recognizes the catalog’s activated standalone range while preserving 12 as the historical preferred classification and 15+ composition-required behavior.
- `CatalogPreviewGenerator`: consumes `CoreHorizonClassifier`, composes the already-proven five orchestrators at one live call site, passes `AvailableFullWeeks`, and maps the final prescribed plan through the existing public materializer.
- No allocator, verifier, or internal orchestrator algorithm changed.

## Evidence

Development `LocalCatalogAcceptance` real-host relational tests cover 13 and 14: preview 200; exact N weeks and 4N sessions; confirm success; exact DB week/day counts; valid active home, calendar, and details responses; reset success. The same tests prove NotEvaluated is 422 `RUNTIME_CONDITION_UNSUPPORTED` with `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE` and no persistence. Catalog publication and deployment activation are separate lifecycle/configuration concerns.
