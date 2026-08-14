# Phase 4K.9 — Full 21–52 Rolling Dark Lifecycle Validation, Retry and Boundary Matrix

## 1. Executive result

`LONG_HORIZON_FULL_21_TO_52_ROLLING_DARK_LIFECYCLE_VALIDATION_COMPLETED`.

The internal lifecycle now has an executable validation harness that repeatedly composes the existing Phase 4K.6, 4K.7, 4K.8C, 4K.8 and 4K.8D authorities. All supported 21–52-week horizons reach final Core completion without full-upfront numeric execution.

## 2. Inherited completed runtime state

The phase reuses the structural roadmap and initial activation from 4K.6, TrainingDay aggregation/checkpoint decisions from 4K.7, Runway/Core JIT lifecycle from 4K.8, immutable Runway contracts from 4K.8B, real condition/Core composition from 4K.8C, and authoritative session-date projection from 4K.8D.

## 3. Scope and exclusions

This is dark validation and diagnostic orchestration only. It adds no progression, recovery, maintenance, direction, calendar, window-selection or pace formula. It adds no persistence, API, DTO, preview, confirmation, background job, TrainingDay write, public DI or Flutter behavior.

## 4. Lifecycle harness

`ILongHorizonFullDarkLifecycleHarness.RunLifecycleAsync` is implemented by `LongHorizonFullDarkLifecycleHarness`. The harness invokes production runtimes and accumulates their outputs; it does not implement a parallel numeric lifecycle.

## 5. Scenario contract

`LongHorizonLifecycleScenario` explicitly supplies horizon, readiness profile, onboarding evidence, preferred/long-run days, dates, target/pace evidence, candidate/catalog dependencies, per-window TrainingDay outcomes, safety, availability, expected block and retry evidence. Standard factories still materialize visible deterministic rows.

## 6. Immutable lifecycle state

`LongHorizonFullDarkLifecycleState` records the roadmap, skeleton, per-week lifecycle, accumulated activated values, current window, context, loads, decisions, Runway ownership, Core contexts and audit events. Each transition returns a new record and copied collections; prior snapshots are retained by the result.

## 7. Runtime routing

Initial execution routes to 4K.6. A first pending GE week routes to 4K.7. A partial terminal GE window is passed, without accepting partial lifecycle mutation, into 4K.8C for one atomic GE→Runway mixed result. Runway/Core next-pending weeks route to 4K.8C. No pending week routes to final validation.

## 8. TrainingDay outcome application

Scenario rows become real `LongHorizonTrainingDayEvidenceRow` values backed by domain `TrainingDay` objects. Completed actual distance, lower actual, Missed, Skipped, SoftMissed, Planned, PendingConfirmation, Rescheduled and invalid/null shapes remain accepted inputs to the existing 4K.7 adapter rules. Non-terminal rows do not prematurely terminalize lifecycle state.

## 9. Checkpoint-date progression

Every checkpoint date is an explicit scenario value, strictly later than the prior checkpoint and after the activated window period. No system clock is read. Retry requires a strictly later explicit date.

## 10. GE lifecycle

The matrix covers GE durations 1–32, initial partial windows of 1–3, initial full windows, repeated four-week windows, partial final GE windows and non-error Runway handoff. No future GE value appears before its selected runtime window.

## 11. Growth lifecycle

Canonical terminal completed history produces repeated `GrowthEligible` decisions through `LongHorizonRollingCheckpointRuntime` and the unchanged GE growth materializer. Validation does not require every rounded Growth result to numerically increase.

## 12. Maintenance lifecycle

Explicit incomplete terminal evidence with a fresh `PriorValidatedCheckpointLoad` anchor produces `MaintenanceOnly`. Consecutive/partial paths reuse the existing maintenance materializer; no new threshold or percentage exists.

## 13. Blocked lifecycle

The approved checkpoint and JIT reason types remain the sole taxonomy. Real safety and unresolved-session cases block through the harness. Reasons not reachable through the fully composed adapter are covered at the narrowest production runtime seam; no artificial full-lifecycle state is fabricated.

## 14. Retry transition

`LongHorizonBlockedActivationRetryService` performs only eligibility restoration. It accepts `NumericActivationBlocked -> NumericPending` when a new explicit checkpoint is later, evidence/context identity changed and a deterministic new decision ID results. It never materializes or activates a week.

## 15. Retry-success matrix

Safety-cleared and non-terminal-to-terminal scenarios prove first block, zero activation, retained history, explicit retry, Pending restoration, normal runtime activation and eventual completion. Missing weekly/long-run, availability, pace, goal and conflict resolution are also represented by the same typed retry contract and inherited reason-specific runtime tests.

## 16. Retry-remains-blocked matrix

Same date, unchanged evidence, empty/duplicate blocked selection and direct Blocked→Activated are rejected. A later but still unresolved normal invocation remains governed by the original runtime reason and cannot loop automatically.

## 17. Runway entry

The real condition service and real Core generator run at first entry. One Core target lock and one immutable full eight-week Runway prescription are created; only the selected slice activates and the real session calendar is projected.

## 18. Runway continuation

Continuation retains prescription ID/version, target-lock identity and the full immutable calendar projection. Exact original week references/dates are reused, Core regeneration is skipped when the next window cannot reach Core, and `PreSpecificTransition` remains terminal-only.

## 19. Runway→Core boundary

Natural greedy windows prove 1 Runway+3 Core, 2+2 and 3+1. An aligned eight-week Runway also proves Runway-only followed by Core-only. The full lifecycle’s fixed Runway duration makes other duration premises unreachable; none are fabricated.

## 20. Core-only lifecycle

Core weeks activate in repeated exact 1–4-week bounded selections until all 12 are present. Local/global numbering, generated values and assigned dates are preserved; future weeks stay pending and undated until selected.

## 21. Future Core refresh

Core-touching future windows may run a new real composition with a later context. Earlier activated Core values/dates and the historical Runway target lock/prescription/calendar remain unchanged. No mid-Runway or overlapping refresh is introduced.

## 22. Calendar lifecycle

Accumulated Runway/Core sessions retain exact 4K.8D `AssignedDate` values. Preferred days, Sunday long-run preference, four distinct dates, structural bounds, chronological cross-window uniqueness, unchanged GE dates and immutable continuation dates are validated without flattening or recalculation.

## 23. Final completion validation

`LongHorizonFinalLifecycleValidator` consumes accumulated runtime output. It validates exact requested count, contiguous global weeks, terminal states, complete numeric/session fields, unique dates, ownership contracts and final Core presence. It does not regenerate the plan.

## 24. Horizon matrix

Every integer horizon 21–52 completed: 32/32 successful canonical scenarios. Horizon 20 remains outside the rolling runtime and 53 remains rejected. The first snapshot always exposes at most four numeric weeks.

## 25. Profile matrix

Both `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY` complete under identical lifecycle/evidence/direction/calendar rules. Existing profile-specific structural/workout selections remain intact.

## 26. Load matrix

Low 12/5 km, typical 20/8 km and high-supported 30/11 km starting inputs complete. Unsupported above-target direction remains fail-closed under 4K.8A/4K.8B tests and is not forced into success.

## 27. Pace/target matrix

Product-average, recent-race and user-defined target with independent recent-race evidence execute through the real resolver chain. Pace evidence is never used as volume evidence. Missing explicit pace inputs use the existing adapter’s product-average fallback; therefore direct unresolved pace/goal outcomes are proven at the narrower 4K.8 seam.

## 28. Atomicity matrix

Existing production tests inject aggregation, materialization, condition, generation, direction, lock, slice, selection, projection and validator failures. The harness accepts only complete runtime success; prior snapshots remain unchanged on block and no partial target/prescription/calendar is published as activated success.

## 29. Determinism and replay

Identical scenarios reproduce outcome, context versions, target lock, prescription ID and every session date. Changed evidence generates a new decision and may affect only permitted future state. SHA-256 and explicit dates are used; no random GUID or current clock participates.

## 30. Audit trace

`LongHorizonLifecycleAuditEvent` covers all required structural, activation, checkpoint, decision, block/retry, Runway/Core, calendar and completion event categories. Events are internal diagnostic records, not persisted event sourcing.

## 31. Loop safety

The guard is derived as `TotalWeeks + (2 × explicit retries) + 1`. Every accepted activation advances at least one week. No unchanged successful iteration exists, blocks do not auto-retry, and absent explicit retry terminates validation.

## 32. Governance artifacts

`TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001` is `CLOSED`. Append-only updates were added to the 4K.8, 4K.8C, 4K.8D and volume-envelope redesign records. The redesign stays `OPEN` because its own historical definition still requires persistence/public/API/Flutter work outside this phase.

## 33. Tests

The focused 4K.9 suite contains 84 passing cases. It includes 32 successful horizons, 20/53 rejection, profiles, Growth/Maintenance/recovery, typed reason coverage, explicit retries, GE/Runway/Core shapes, Core completion, calendar accumulation, three loads, pace paths, replay and dark-boundary containment. Phase regressions and full solution suites are reported separately after final execution.

## 34. Public/persistence/API/Flutter status

No public response, persistence model/write, migration, endpoint, public DTO, DI registration or Flutter surface changed. No TrainingDay is created outside in-memory supplied validation evidence.

## 35. Final classification

`LONG_HORIZON_FULL_21_TO_52_ROLLING_DARK_LIFECYCLE_VALIDATION_COMPLETED`.

`LONG_HORIZON_ALL_SUPPORTED_HORIZONS_COMPLETE_THROUGH_INITIAL_GE_CHECKPOINT_RUNWAY_CORE_AND_FINAL_COMPLETION_WITHOUT_FULL_UPFRONT_NUMERIC_EXECUTION`.

`LONG_HORIZON_BLOCKED_WINDOWS_REQUIRE_EXPLICIT_RETRY_AND_RECOVER_ONLY_THROUGH_BLOCKED_TO_PENDING_TO_NORMAL_ACTIVATION`.

`LONG_HORIZON_TARGET_LOCK_CONTEXT_VERSION_NUMERIC_HISTORY_AND_SESSION_CALENDAR_REMAIN_IMMUTABLE_ACROSS_REPEATED_WINDOWS_AND_FUTURE_ONLY_REFRESH`.

`LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED`.

## 36. Exact next phase

Recommended next phase: **Phase 4L.1 — Long-Horizon Public Preview Contract and Dark-to-Public Readiness Review**. Phase 4L.1 was not started.
