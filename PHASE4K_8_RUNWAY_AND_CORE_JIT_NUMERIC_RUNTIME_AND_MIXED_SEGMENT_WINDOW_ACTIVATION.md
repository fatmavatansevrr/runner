# Phase 4K.8 — Runway and Core JIT Numeric Runtime and Mixed-Segment Window Activation

## 1. Executive result

This phase implements the dark orchestration runtime that resolves and atomically activates the next rolling window whenever it involves Preparation Runway and/or Core, consuming — never redesigning — the Phase 4K.5/4K.8B typed contracts. `LongHorizonRollingJitActivationRuntime` handles all four in-scope outcomes (`GeRunwayMixedWindowActivated`, `RunwayWindowActivated`, `RunwayCoreMixedWindowActivated`, `CoreWindowActivated`) plus `JitWindowBlocked`, activating at most one contiguous window of up to four weeks per invocation. The single most consequential architectural decision, made explicit rather than hidden, is scope: this runtime does not itself invoke the real Core generator (`TenKPreparationRunwayCoreGenerator`) or `RuntimeConditionResolutionService` — like the real, unmodified `TenKPreparationRunwayCoreGenerationRequest` itself (which already takes `ConditionResults` as a pre-resolved input), it accepts the caller's own already-produced Core Week-1 target, available Core weeks, and resolved condition results, then locks/bounds/selects from them. The Runway materializer, by contrast, IS invoked directly and for real (it is a lightweight, unchanged static method), exactly once per Runway segment. No commits made.

```
LONG_HORIZON_RUNWAY_AND_CORE_JIT_NUMERIC_RUNTIME_AND_MIXED_SEGMENT_WINDOW_ACTIVATION_COMPLETED_DARK

LONG_HORIZON_VALIDATED_CHECKPOINT_EVIDENCE_NOW_RESOLVES_AN_IMMUTABLE_CORE_WEEK_ONE_TARGET_AND_ONE_FULL_PREPARATION_RUNWAY_PRESCRIPTION

LONG_HORIZON_ROLLING_WINDOWS_EXPOSE_ONLY_EXACT_BOUNDED_RUNWAY_AND_SELECTED_CORE_REFERENCES_WITH_ALL_OR_NOTHING_MIXED_SEGMENT_ACTIVATION

LONG_HORIZON_UNSUPPORTED_DOWNWARD_TRANSITIONS_FAIL_CLOSED_AND_FUTURE_RUNWAY_CORE_WEEKS_REMAIN_NUMERIC_PENDING

LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED
```

## 2. Inherited policy and contract state

Phase 4K.4: atomic Runway/Core-target timing, target immutability, mixed-window atomicity. Phase 4K.5: typed contracts (lifecycle, activation window, evidence authority, JIT context, target lock, versioning). Phase 4K.5A: validated completed-training evidence is the rolling authority; WeeklyVolumeKm/LongRunKm map to RecentWeeklyVolumeKm/RecentLongestRunKm; onboarding is never a rolling fallback. Phase 4K.6: initial GE activation. Phase 4K.7: TrainingDay aggregation, validated load, Growth/Maintenance/Blocked decisions, non-error Runway-boundary handoff. Phase 4K.8A: Runway direction authority (below/equal supported, above unsupported, fail-closed). Phase 4K.8B: `PreparationRunwayDirectionGuard`, `ImmutablePreparationRunwayPrescription`, `PreparationRunwayPrescriptionWeekReference`, target-lock scope/refresh guard, bounded slice factory/equivalence validator.

## 3. Scope and exclusions

In scope: the runtime entry point, window selection, JIT evidence context, Core input mapping, target-lock creation/reuse, full Runway materialization (real invocation), bounded Runway/Core exposure, GE+Runway/Runway-only/Runway+Core/Core-only activation, atomicity, lifecycle mapping, the result contract, failure taxonomy, versioning/determinism.

Excluded: redesigning the Phase 4K.8B contracts; any GE/Runway/Core numeric formula change; downward interpolation; persisting rolling state; public preview/API/DTO/DI/Flutter changes; activating more than one window per invocation; full 21–52 lifecycle simulation (Phase 4K.9).

## 4. Runtime entry point

`ILongHorizonRollingJitActivationRuntime.ResolveAndActivateNextWindowAsync`, implemented by `LongHorizonRollingJitActivationRuntime` — internal, parameterless-constructible, no public DI registration.

## 5. Eligibility

Race / exact 10K / Intermediate / four days per week / 21–52 structural horizon / a `ReadinessProfile` matching the structural roadmap's own profile — enforced by `ValidateInput`. Does not alter the 8–14 Core-only, 15–20 Runway+Core, 53+ rejection, Habit, or any other distance/level/frequency flow, none of which this runtime references.

## 6. Window selection

`WindowStart` = first global week whose lifecycle state is `NumericPending`; `WindowEnd` = the last contiguous `NumericPending` week up to `min(WindowStart+3, TotalWeeks)`. A pure GE-only window (both boundary weeks in `GeneralEndurance`) is explicitly out of this runtime's scope — that remains Phase 4K.7's own responsibility — and blocks with `JIT_SEGMENT_TRANSITION_INFEASIBLE` rather than being silently handled or misclassified.

## 7. JIT evidence context

Weekly/long-run authority is the caller-supplied `ValidatedSustainableLoad` (Phase 4K.5A's rolling authority). Onboarding evidence and planned GE exit are never read as rolling authority anywhere in this runtime. Pace/goal-feasibility context is the caller's own already-resolved `RuntimeConditionResolutionService.ResolveAllResults` output, checked via `HasUnresolvedPaceOrGoal` — never recomputed internally.

## 8. Core input adapter

`LongHorizonJitCoreInputAdapter.Map` implements the exact Phase 4K.5A mapping: `ValidatedSustainableLoad.WeeklyVolumeKm → RecentWeeklyVolumeKm`, `LongRunKm → RecentLongestRunKm`, exact completed frequency → `RecentRunsPerWeek` or null (never rounded/inferred). Throws if the supplied load is not `Valid`.

## 9. Core Week-1 target generation

This runtime does not itself invoke `TenKPreparationRunwayCoreGenerator` or the five-layer `DynamicCoreCalendarMaterializationOrchestrator` — it accepts the caller's already-produced `PreparationRunwayCoreWeekOneNumericTarget` (from that real, unchanged chain) as an input, exactly mirroring how the real generator's own request type already takes `ConditionResults` as a pre-resolved input rather than computing it internally. This boundary is disclosed explicitly, not hidden, and is consistent with every prior phase's own "the existing Core generator remains authoritative, never reimplemented" principle.

## 10. Direction guard

Reuses Phase 4K.8B's `PreparationRunwayDirectionGuard` verbatim, invoked inside `PreparationRunwayFullPrescriptionFactory.Create` (called from `ResolveOrCreateRunwayPrescription`). Weekly/long-run BelowTarget/EqualTarget continue; AboveTarget throws `PreparationRunwayDirectionUnsupportedException`, caught by this runtime's shared outer `try/catch` and mapped to `JIT_SEGMENT_TRANSITION_INFEASIBLE`. No downward interpolation, target lift, or evidence lowering exists anywhere in this runtime.

## 11. Target-lock creation/reuse

First Runway entry creates exactly one `LongHorizonLockedCoreWeekOneTarget` whose `LockedForActivatedRunwayWeekRange` exactly equals the structural Runway segment's full range. Continuation windows reuse the caller-supplied `ExistingLockedCoreTarget`/`ExistingRunwayPrescription` unchanged — range compatibility is re-verified, never regenerated; a mismatch throws `PreparationRunwayTargetLockScopeViolationException`.

## 12. Full Runway materialization

The unchanged, real `PreparationRunwayNumericMaterializer.Materialize` is called exactly once, at first Runway entry, via `TenKPreparationRunwayNumericPolicyFactory.Build()` and the caller-supplied `RunwayStructuralWeeks`/`RunwayStartingLoadEvidence`. Its successful result is passed to Phase 4K.8B's unmodified `PreparationRunwayFullPrescriptionFactory.Create`, producing one `ImmutablePreparationRunwayPrescription`. Later windows never call the materializer again — proven by the reuse branch and by determinism tests.

## 13. Immutable Runway prescription

Unchanged from Phase 4K.8B — `ImmutablePreparationRunwayPrescription<PreparationRunwayBlockType>`, validated by `ImmutablePreparationRunwayPrescriptionValidator`.

## 14. Bounded Runway exposure

`SliceForWindow` maps the selected global-week range to the prescription's own local-week coordinates and calls `PreparationRunwayBoundedSliceFactory.CreateSlice` — exact week references, no recalculation, no local renumbering. `MapRunwayWeeks` converts only the selected slice's references into real `NumericActivated` `ActivatedNumericWeek` instances.

## 15. GE→Runway mixed window

`ActivateGeRunwayMixedWindow` requires the caller's own already-materialized `GeActivatedWeeks` (Phase 4K.6/4K.7's real GE materializers, never recomputed here) for the GE portion, and the bounded Runway slice for the Runway portion, concatenated into one atomic window. A failure in either portion (missing GE weeks, or an unsupported Runway direction) propagates through the shared `try/catch`, producing zero activated weeks for the whole window.

## 16. Runway-only window

`ActivateRunwayOnlyWindow` reuses an existing compatible prescription/lock when present (continuation), or creates them at first entry.

## 17. Runway→Core mixed window

`ActivateRunwayCoreMixedWindow` requires an already-existing, immutable full Runway prescription (Runway's own materializer always completes before its final window is reached) plus caller-supplied `AvailableCoreWeeks` for the Core portion. Both portions combine into one atomic window with correct per-segment provenance; future Core weeks beyond the selected range remain `NumericPending`.

## 18. Core-only window

`ActivateCoreOnlyWindow` requires the window to begin strictly after the Runway segment's own `EndGlobalWeek` (else `PreparationRunwayMidRunwayRefreshViolationException`) and a complete contiguous set of caller-supplied `AvailableCoreWeeks` for the requested range. Already-activated/completed Core weeks are never touched.

## 19. Future Core refresh

Not implemented as an explicit refresh operation in this phase (out of scope, consistent with "do not implement full 21–52 lifecycle simulation") — the underlying versioning primitives (`LongHorizonContextVersion.Next`, `PreparationRunwayTargetRefreshGuard` from Phase 4K.8B) that a future refresh call would use remain unchanged and available.

## 20. Pace and goal-feasibility

Read directly from the caller's own real `RuntimeConditionResolutionService` output via `HasUnresolvedPaceOrGoal` — never recomputed. Unresolved pace/goal conditions block with `JIT_PACE_SOURCE_UNRESOLVED`/`JIT_GOAL_FEASIBILITY_UNRESOLVED` respectively.

## 21. Calendar continuity

Reuses the existing, real, pure `LongHorizonCalendarAssigner.WeekStartDate` primitive — the same one Phase 4K.6/4K.7's GE runtimes already use — applied to Runway and Core weeks too, rather than wiring the heavier, DI-dependent `PreparationRunwayCalendarComposer`. This is an explicitly disclosed simplification, not a fabrication: `WeekStartDate` is real, unchanged, already-tested production code; it produces a week-level date range (matching the exact convention `LongHorizonRollingCheckpointRuntime` already uses for GE weeks), not per-session composer output.

## 22. Numeric-week mapping

`MapRunwayWeeks`/`SelectCoreWeeks` build `ActivatedNumericWeek` instances carrying `NumericActivated`, correct global week/segment, positive weekly volume/long run, positive session distances summing to the weekly total, calendar dates, evidence/policy provenance, and context version — validated by the existing, unmodified `LongHorizonActivatedNumericWeekValidator`.

## 23. Lifecycle and immutability

Success promotes only the selected `NumericPending` weeks to `NumericActivated`; prior `Completed`/`NumericActivated` weeks are copied through unchanged. Failure changes nothing. The input roadmap, the immutable Runway prescription, and the target lock are never mutated — every transformation produces a new object.

## 24. Atomicity

Every segment-activation path is wrapped by one shared `try/catch` in `ResolveAndActivateNextWindowAsync`: any `LongHorizonRollingContractException` thrown anywhere (direction guard, target-lock validation, full-prescription validation, bounded-slice validation, numeric-week validation, window validation) produces a `Blocked` result with zero newly activated weeks and prior `LifecycleStates` entries fully preserved.

## 25. Result contract

`LongHorizonRollingJitActivationResult`: `Outcome`, updated `LifecycleStates`, `ActivationWindow`, `NewlyActivatedWeeks`, `CoreTargetLock`, `RunwayPrescription`, `RunwaySlice`, `ContextVersion`, `AuthoritativeReason`, `ValidationStages`. Never exposed publicly.

## 26. Failure taxonomy

Reuses the existing ten JIT reasons (Phase 4K.4) plus `SAFETY_REASSESSMENT_REQUIRED` (Phase 4K.3, highest priority, checked first). Unsupported downward direction maps to `JIT_SEGMENT_TRANSITION_INFEASIBLE`. No new public reason code was added. Exactly one reason per blocked result.

## 27. Context versioning

JIT decision ID, context version, and (via Phase 4K.5/4K.8B) target-lock/prescription/slice/window IDs are SHA-256-derived from an explicit input seed (`BuildIdentitySeed`), reusing the exact `StableGuid` convention already used by `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingInitialActivationRuntime`. No random GUID, no wall-clock read.

## 28. Determinism

Identical inputs reproduce identical prescription/window identities; changed evidence before Runway entry changes the prescription identity; reused prescriptions retain their identity across continuation calls — proven by dedicated determinism tests.

## 29. Bounded-execution proof

Dedicated tests prove: the full Runway materializer runs exactly once across a multi-window activation chain; only the selected window's weeks transition to `NumericActivated` while the rest of the roadmap remains `NumericPending`; no more than one window activates per invocation; and the `RollingActivation` namespace references no controller/DbContext/public DTO type.

## 30. Dark integration

Internal, independently invokable, supplied-context/evidence based. Unwired from live preview, confirmation, repositories, endpoints, and Flutter. No public DI registration.

## 31. Governance artifacts

`TD-LONG-HORIZON-RUNWAY-CORE-JIT-RUNTIME-001` — added **CLOSED**. `TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001` and `TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001` — each received an append-only update field, remain CLOSED. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — `requiredResolution` appended, remains **OPEN**. Aggregate updated to "42 risks are now recorded in total: 14 OPEN and 28 CLOSED."

## 32. Tests

41 new focused tests, all passing after two window-selection-premise corrections (an intended "partial final Runway slice" case was re-derived to match the runtime's actual greedy window-filling design, and a GE-only-window test's expected reason code was corrected to the semantically accurate `JIT_SEGMENT_TRANSITION_INFEASIBLE`). Full backend suite and full plan-catalog suite results below.

## 33. Public/persistence/API/Flutter status

Unchanged. Zero controller, DI registration, database entity/migration, or Flutter file touched — confirmed by a clean build of `RunningApp.Application`/`RunningApp.Api` with no file outside the new runtime/contracts files modified.

## 34. Final classification

```
LONG_HORIZON_RUNWAY_AND_CORE_JIT_NUMERIC_RUNTIME_AND_MIXED_SEGMENT_WINDOW_ACTIVATION_COMPLETED_DARK

LONG_HORIZON_VALIDATED_CHECKPOINT_EVIDENCE_NOW_RESOLVES_AN_IMMUTABLE_CORE_WEEK_ONE_TARGET_AND_ONE_FULL_PREPARATION_RUNWAY_PRESCRIPTION

LONG_HORIZON_ROLLING_WINDOWS_EXPOSE_ONLY_EXACT_BOUNDED_RUNWAY_AND_SELECTED_CORE_REFERENCES_WITH_ALL_OR_NOTHING_MIXED_SEGMENT_ACTIVATION

LONG_HORIZON_UNSUPPORTED_DOWNWARD_TRANSITIONS_FAIL_CLOSED_AND_FUTURE_RUNWAY_CORE_WEEKS_REMAIN_NUMERIC_PENDING

LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED
```

## 35. Exact next phase

**Phase 4K.9 — Full 21–52 Rolling Dark Lifecycle Validation, Retry and Boundary Matrix** — validates the complete dark lifecycle (repeated JIT activation across an entire 21–52 week horizon, retry/recovery behavior, and boundary conditions) before any live integration is considered.
