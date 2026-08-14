# Phase 4K.7 — Checkpoint Evidence Aggregation, State Evaluation and Next-Window Activation Runtime

## 1. Executive result

Phase 4K.7 completes the first dark end-to-end checkpoint chain from supplied real `TrainingDay` history to an atomic next General Endurance (GE) window. The runtime returns exactly `NextGeWindowActivated`, `NextGeWindowBlocked`, or the non-error Runway-boundary handoff. It activates at most one GE window and remains internal and unwired.

## 2. Inherited policy/runtime state

Phases 4K.1–4K.5A supply the approved rolling lifecycle, evidence, load, state-transition, JIT-boundary, typed-contract and Core-authority policies. Phase 4K.6 supplies the complete structural roadmap and bounded initial GE window. Existing GE, volume, allocation, catalog and calendar authorities remain unchanged.

## 3. Scope and exclusions

Scope is Race, exact 10K, Intermediate, four days/week, 21–52 structural weeks, and valid readiness/catalog inputs. Runway/Core JIT, mixed-segment execution, persistence, public preview, API/DTO changes, DI registration, background jobs and Flutter are excluded.

## 4. Runtime entry point

`ILongHorizonRollingCheckpointRuntime.EvaluateAndActivateNextGeWindowAsync` is implemented by `LongHorizonRollingCheckpointRuntime`. It accepts the roadmap/skeleton/lifecycle, most recent window, supplied `TrainingDay` rows, explicit checkpoint date, availability, long-run day, safety/readiness, optional prior validated anchor and prior context version.

## 5. TrainingDay evidence adapter

`LongHorizonTrainingDayEvidenceRow` associates a real domain `TrainingDay` with its global week. Completed actual distance is usable; planned distance is never substituted. Missed, Skipped and SoftMissed contribute no actual distance. Rest rows are excluded. Negative or status/actual contradictions fail typed.

## 6. Evidence-window selection

Only rows whose global week belongs to the most recently activated window enter aggregation. Older completed history and future `NumericPending` rows are excluded. A prior validated anchor is a separate input and is never blended into current partial evidence.

## 7. Terminal-window behavior

The calendar period has ended only after the final prior-window date. All planned sessions must resolve to Completed, Missed, Skipped or SoftMissed. Planned, PendingConfirmation and Rescheduled remain non-terminal. A period still in progress blocks; ended unresolved evidence can authorize Maintenance only through a fresh prior anchor.

## 8. Weekly-volume aggregation

Completed usable `ActualDistanceKm` values are grouped and summed per global week. Structural recovery weeks are excluded from the mean. The mean of usable non-recovery weekly totals is rounded using `VolumeSafetyPolicy.Default.RoundingIncrementKm` (0.5 km). Completed null/zero evidence is unusable; lower actual completion remains a real lower contribution.

## 9. Long-run aggregation

Only usable Completed long-run actuals enter the mean. The rounded mean is bounded by validated weekly volume multiplied by the existing `LongRunHardCapShare` (0.40). Planned long-run distance, onboarding longest run, maxima and missing-value substitutes are not used. Missing current and permitted-prior long-run evidence blocks specifically.

## 10. Adherence confidence

Adherence reuses the repository formula `Completed / planned sessions * 100`, rounded to one decimal, as provenance. No threshold is invented. Growth confidence instead applies the approved deterministic condition: every non-recovery evidence week has at least one Completed session with usable actual evidence.

## 11. Safety handling

`LongHorizonSafetyState` is an explicit typed input seam because no live safety producer is wired. Unresolved safety-critical state is evaluated first and blocks with `SAFETY_REASSESSMENT_REQUIRED`; no free-text medical inference is performed.

## 12. Checkpoint snapshot

`LongHorizonCheckpointEvidenceSnapshot` carries deterministic checkpoint identity, explicit date/window/terminal flags, weekly actuals and week mapping, excluded recovery weeks, completed long runs/counts, adherence/misses, availability, safety, segment, weeks until Runway, prior-load projections, completed-history provenance and context version. Production validation runs immediately after aggregation.

## 13. Validated-load result

`ValidatedSustainableLoad` carries rounded weekly and long-run values, evidence boundaries, included/excluded weeks, rounding/cap provenance and context version. Current authority is `CompletedTrainingHistory`. A prior anchor must use `PriorValidatedCheckpointLoad`; weekly and long-run source validators reject planned/onboarding authority.

## 14. Transition-table implementation

`LongHorizonCheckpointStateEvaluator` implements the safety-first eight-case order: safety; terminal/prior; weekly authority; long-run authority; feasibility; Growth confidence; then Growth. Outcomes are mutually exclusive. Growth has no reason; Maintenance and Blocked have exactly one checkpoint-taxonomy reason.

## 15. Prior-anchor behavior

`LongHorizonPriorValidatedAnchor` carries the validated load, explicit freshness-for-this-invocation metadata and its source context sequence. It must be valid, positive and prior-authority sourced. It may authorize Maintenance for incomplete/stale current evidence, never Growth, and is not reused when marked stale.

## 16. Next GE window selection

The start is the first week after the most recently activated window. The end is `min(start + 3, final GE week)`. Every selected week must be contiguous `NumericPending` GE. Requested size remains four and actual size is one through four. No Runway week is borrowed.

## 17. Growth runtime

Growth delegates selected descriptors to the existing `ILongHorizonRollingGeWindowMaterializer`, mapping validated weekly and long-run load into `LongHorizonGeEntryBaselineInput`. Existing 0.07/0.08/2.5 km caps, 0.5 km rounding, 0.85 recovery, long-run shares, session feasibility, catalog references and profile-aware content remain authoritative. Growth does not promise an increase.

## 18. Maintenance runtime

The narrow `LongHorizonGeMaintenanceWindowMaterializer` keeps non-recovery weeks flat at the validated weekly anchor, uses the validated long-run as a ceiling, and reuses `FourDaySessionDistanceAllocationPolicy`. It adds no maintenance percentage, long-run share, recovery multiplier or drift rule.

## 19. Recovery behavior

A real structural recovery week applies the existing 0.85 ratio, existing minimum 0.5 km observable reduction and existing rounding. Its baseline is the current maintenance anchor or Growth executor’s existing development reference. Recovery weeks are excluded from the next weekly-load mean, preventing recovery feedback decline.

## 20. Calendar assignment

Only the new window receives dates, through `LongHorizonCalendarAssigner` and current availability/long-run preference. Its first week begins after the prior window boundary. Invalid availability becomes `NUMERIC_WINDOW_INFEASIBLE`; no days are auto-corrected and historical dates are untouched.

## 21. Numeric-week/lifecycle mapping

Every new `ActivatedNumericWeek` is `NumericActivated`, has complete positive four-session prescriptions, catalog key/version, dates, evidence and policy provenance, checkpoint decision ID and new context version. Selected pending weeks transition atomically; prior lifecycle entries and the later pending suffix are copied unchanged.

## 22. Atomicity

Evidence conflict, state, materialization, calendar, numeric-week, window or final-result validation produces zero activated weeks and a typed blocked result for the selected boundary. Original exception type/message is retained in validation provenance. No partial selected window is returned.

## 23. Context versioning

Checkpoint, decision, context and window GUIDs derive deterministically from immutable request/evidence inputs using SHA-256. `LongHorizonContextVersion.Next(deterministicId)` returns a new sequence. No wall clock or random GUID contaminates results; changed evidence or authority inputs change identity.

## 24. Boundary handoff

If the next week is beyond final GE, the result is `GeCheckpointCompletedWithoutGeWindowBecauseRunwayBoundaryReached`. It is not an error, carries no blocked reason or GE decision, activates nothing, and leaves Runway/Core `NumericPending` for Phase 4K.8.

## 25. Failure taxonomy

The runtime reuses all nine approved checkpoint reasons: window incomplete, stale evidence, load unavailable, long-run unavailable, insufficient Growth confidence, maintenance anchor unavailable, numeric infeasibility, safety reassessment and evidence conflict. It adds no public reason; boundary handoff uses none.

## 26. Dark integration

Integration is an independently invokable internal application service over supplied entity collections. It is not registered as a public service and does not load from a repository. Tests exercise the full production orchestration chain through real catalog/GE authorities.

## 27. Proof no Runway/Core execution

The selection is capped at `GeneralEnduranceWeeks`; the boundary branch returns before any later-segment runtime. Results contain only GE weeks, no `LongHorizonJitContextDecision`, no JIT decision ID and no Core target lock. No Runway/Core materializer or generator is referenced by the checkpoint runtime.

## 28. Governance artifacts

`TD-LONG-HORIZON-CHECKPOINT-NEXT-WINDOW-RUNTIME-001` is added `CLOSED`. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` receives an append-only 4K.7 progress update and remains `OPEN`. JSON/Markdown aggregate is 38 total, 14 OPEN, 24 CLOSED.

## 29. Tests

Focused groups cover TrainingDay aggregation, terminal/prior-anchor behavior, state transitions, Growth, Maintenance, partial/boundary/no-JIT behavior, atomicity, versioning, determinism, profiles and validator order. Governance tests enforce fields, parity, uniqueness, open redesign status and exactly 32 document sections. Final exact suite counts are recorded in the delivery report.

## 30. Public/persistence/API/Flutter status

No public DTO, endpoint, controller, confirmation, persistence entity/repository/schema, API behavior, DI registration or Flutter file is part of this phase. Existing short-horizon, Runway+Core and 53+ behavior remains unchanged.

## 31. Final classification

`LONG_HORIZON_CHECKPOINT_EVIDENCE_AGGREGATION_STATE_EVALUATION_AND_NEXT_GE_WINDOW_RUNTIME_COMPLETED_DARK`

`LONG_HORIZON_COMPLETED_TRAINING_HISTORY_NOW_PRODUCES_TYPED_VALIDATED_LOAD_AND_DETERMINISTIC_GROWTH_MAINTENANCE_OR_BLOCKED_DECISIONS`

`LONG_HORIZON_NEXT_GENERAL_ENDURANCE_WINDOW_ACTIVATES_ATOMICALLY_WITHOUT_FULL_UPFRONT_OR_RUNWAY_CORE_EXECUTION`

`LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_AND_RUNWAY_CORE_JIT_RUNTIME_REMAIN_UNCHANGED`

## 32. Exact next phase

Phase 4K.8 — Runway and Core JIT Numeric Runtime and Mixed-Segment Window Activation. This phase was not started.
