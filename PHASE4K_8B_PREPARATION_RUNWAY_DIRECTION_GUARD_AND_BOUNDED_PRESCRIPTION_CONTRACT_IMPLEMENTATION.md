# Phase 4K.8B — Preparation Runway Direction Guard and Bounded Prescription Contract Implementation

## 1. Executive result

This phase implements the dark production contracts Phase 4K.8A's approved authorities require before Phase 4K.8's JIT runtime can resume: a typed direction relation/comparer/policy/guard, an immutable full Runway prescription contract wrapping the unchanged `PreparationRunwayNumericMaterializer` output, a week-reference contract that reads values directly from the real production object, a one-lock-per-full-range target-lock scope with a mid-Runway refresh guard, and a bounded 1–4-week slice contract whose factory structurally cannot invoke the numeric materializer and whose exact-equivalence validator runs in production code. Every test was built from a real `PreparationRunwayNumericMaterializer` result using the existing `PreparationRunwayNumericMaterializerTests` fixture helpers — never a hand-built shortcut. No downward interpolation is enabled; no Runway/Core JIT orchestration exists. No commits made.

```
PREPARATION_RUNWAY_DIRECTION_GUARDS_AND_BOUNDED_PRESCRIPTION_CONTRACTS_COMPLETED_DARK

PREPARATION_RUNWAY_FULL_IMMUTABLE_PRESCRIPTION_REMAINS_THE_SINGLE_NUMERIC_AUTHORITY_AND_BOUNDED_SLICES_EXPOSE_EXACT_ORIGINAL_WEEK_REFERENCES

PREPARATION_RUNWAY_UNSUPPORTED_DOWNWARD_RELATIONS_FAIL_CLOSED_AND_MID_RUNWAY_TARGET_REFRESH_REMAINS_PROHIBITED

PHASE4K_8_JIT_RUNTIME_REMAINS_UNIMPLEMENTED_BUT_ALL_PREREQUISITE_DIRECTION_AND_BOUNDED_EXPOSURE_CONTRACTS_ARE_READY
```

## 2. Inherited 4K.8A authority

Weekly: BelowTarget/EqualTarget conditionally supported via existing capped-upward/flat interpolation; AboveTarget unsupported, fail-closed. Long-run: identical relation matrix after existing 0.30–0.36/0.40 band normalization. No downward percentage, reduction cap, convergence multiplier, target lift, onboarding fallback, GE-exit authority, Taper reuse, or GE-recovery reuse approved. The unchanged 3–8-week materializer remains the single numeric authority; one immutable full prescription is generated once; rolling windows expose exact references; independent slice restart and slice-local interpolation reset are forbidden; one immutable Core Week-1 target governs the complete prescription; mid-Runway refresh is forbidden; `PreSpecificTransition` exists only on the final structural week; internal computed future values are not activated, public, persisted, or checkpoint evidence.

## 3. Scope and exclusions

In scope: the nine contract areas Phase 4K.8B's own prompt names (direction relation, direction policy, direction guard, full-prescription identity/contract, week-reference contract, target-lock scope, bounded-slice contract, exact-equivalence validator, mid-Runway refresh guard), plus supporting validators and a narrow factory boundary.

Excluded: Runway/Core JIT orchestration, invoking the real pipeline from a rolling runtime, mixed-segment window activation, any Runway numeric formula change, enabling downward interpolation, persistence, public API, preview, DI, Flutter.

## 4. Namespace and integration boundary

`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/PreparationRunway/` (9 files), matching the existing `RollingActivation` convention. Everything is `internal`; collections are immutable/read-only; nothing is registered in DI, referenced by a controller, or exposed through a public DTO.

## 5. Direction relation

`PreparationRunwayDirectionRelation` (`BelowTarget`/`EqualTarget`/`AboveTarget`) plus `PreparationRunwayDirectionComparer.Compare`, one deterministic comparer using the existing 0.5km rounding granularity — no new tolerance.

## 6. Direction policy

`PreparationRunwayDirectionPolicy`: `WeeklyDirection`/`LongRunDirection`, `WeeklyDirectionSupported`/`LongRunDirectionSupported`, `OverallSupported`, `FailureReason`, `PolicyProvenance`. Weekly/long-run BelowTarget/EqualTarget → conditionally supported; AboveTarget → unsupported for both, evaluated independently.

## 7. Direction guard

`PreparationRunwayDirectionGuard.Evaluate(startingWeekly, targetWeekly, startingLongRun, targetLongRun)` returns the policy above. Unsupported direction maps to the existing `LongHorizonJitReasonCode.JitSegmentTransitionInfeasible`, never a new public reason.

## 8. Full prescription identity/version

`PreparationRunwayPrescriptionId`/`PreparationRunwayPrescriptionVersion` (wrapping `LongHorizonContextVersion`), both deterministic via `PreparationRunwayDeterministicIdentity.StableGuid` — the exact SHA-256 convention already used by `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingInitialActivationRuntime`. No random GUID, no wall clock.

## 9. Immutable full prescription

`ImmutablePreparationRunwayPrescription<TKey>`: duration 3–8, `StartGlobalWeek`/`EndGlobalWeek`, locked target, direction policy, full week references, profile, provenance fields, `ComputedInternalPending=true`, `Immutable=true`.

## 10. Week-reference contract

`PreparationRunwayPrescriptionWeekReference<TKey>.ProductionWeek` IS the original `PreparationRunwayPrescribedWeek<TKey>` instance — `WeeklyVolumeKm`/`LongRunKm`/`Stage` are read from it, never recomputed.

## 11. Local/global coordinates

`GlobalPlanWeek = StartGlobalWeek + LocalRunwayWeek - 1`, enforced by `ImmutablePreparationRunwayPrescriptionValidator`. Local weeks are exactly `1..duration`; global weeks contiguous and unique.

## 12. PreSpecificTransition invariant

`PreparationRunwayTerminalStageValidator`: exactly one week is `PreSpecificTransition`, and it must be the final local week. Bounded slices never synthesize or omit it — they simply expose whatever original week reference falls in range.

## 13. Target-lock scope

`PreparationRunwayTargetLockScope`/`Validator`: one lock covers the full Runway global range exactly; no per-slice lock. Enforced directly inside `ImmutablePreparationRunwayPrescriptionValidator` too (both paths reject a mismatched lock range).

## 14. Mid-Runway refresh guard

`PreparationRunwayTargetRefreshGuard.ValidateRefreshOutsideRunwayRange` rejects any new context range overlapping the locked Runway range, and requires a strictly later `ContextVersion` — reusing the same overlap-check shape as `LongHorizonCoreTargetLockValidator.ValidateRefresh`.

## 15. Bounded-slice contract

`BoundedPreparationRunwayPrescriptionSlice<TKey>` (1–4 contiguous local weeks), carrying `NonExecutableUntilActivation=true`.

## 16. Slice factory

`PreparationRunwayBoundedSliceFactory.CreateSlice` — validates the full prescription first, validates the range, filters (never recomputes) the matching week references, generates a deterministic slice ID, and runs the equivalence validator before returning. Its signature takes an already-built prescription, not a materialization request — it structurally cannot invoke the numeric materializer.

## 17. Exact-equivalence validation

`PreparationRunwayBoundedSliceEquivalenceValidator` runs inside the factory itself, proving every slice week is value-equal (record equality, including the embedded `ProductionWeek` reference) to its full-prescription counterpart.

## 18. Full-prescription factory boundary

`IPreparationRunwayFullPrescriptionFactory<TKey>`/`PreparationRunwayFullPrescriptionFactory<TKey>.Create` accepts an already-completed, successful `PreparationRunwayNumericMaterializationResult<TKey>` plus explicit starting evidence and a locked target, and produces the validated prescription. It does not orchestrate Core generation, evidence mapping, pace resolution, calendar generation, or mixed-window activation — Phase 4K.8's JIT runtime will call it after real full materialization.

## 19. Internal-pending safeguards

`ImmutablePreparationRunwayPrescription<TKey>` and `BoundedPreparationRunwayPrescriptionSlice<TKey>` carry no `LongHorizonNumericLifecycleState` field — they structurally cannot represent `NumericActivated`. No `TrainingDay`, checkpoint-evidence, DTO, or persistence type exists anywhere in the namespace.

## 20. Mixed-window readiness contract

`PreparationRunwayBoundedExposureSelection<TKey>`/`Validator` (Part 16): one bounded slice plus externally supplied GE/Core references, `AtomicityRequired` always true. Defined but never activated by this phase.

## 21. Failure taxonomy

Reuses `JIT_SEGMENT_TRANSITION_INFEASIBLE` for unsupported direction/invalid transitions; seven new internal typed exceptions (`PreparationRunwayDirectionUnsupportedException`, `PreparationRunwayFullPrescriptionInvalidException`, `PreparationRunwayTargetLockScopeViolationException`, `PreparationRunwayMidRunwayRefreshViolationException`, `PreparationRunwayBoundedSliceInvalidException`, `PreparationRunwaySliceEquivalenceViolationException`, `PreparationRunwayTerminalStageViolationException`), all extending the existing `LongHorizonRollingContractException` base. No new public reason code.

## 22. Validator chain

`PreparationRunwayFullPrescriptionFactory.Create` runs: (1) normalized direction comparison, (2) direction-policy support, (3) target-lock validation, then delegates (4)–(7) duration/coordinates/terminal-stage/immutability to `ImmutablePreparationRunwayPrescriptionValidator`. `PreparationRunwayBoundedSliceFactory.CreateSlice` runs (8) range validation and (9) exact-equivalence validation. Proven behaviorally by tests asserting each later step still fires when earlier steps pass.

## 23. Immutability

All contracts are `sealed record` types with `init`-only properties and `IReadOnlyList` collections. `Refresh()`/`with` expressions always return new instances; no method mutates an existing prescription or target.

## 24. Determinism

Identical materializer output + target lock → identical `PrescriptionId`/`PrescriptionVersion`; identical slice range → identical `SliceId`. Changed target lock, evidence, duration, or slice range → different identity. No random GUID or wall clock — proven by 6 dedicated tests.

## 25. Proof numeric formulas unchanged

`PreparationRunwayNumericMaterializer.cs` was not modified (confirmed — no file outside the new `PreparationRunway` subfolder was touched). All full-prescription tests call the real, unchanged materializer via the existing `PreparationRunwayNumericMaterializerTests` fixtures.

## 26. Governance artifacts

`TD-LONG-HORIZON-RUNWAY-BOUNDED-PRESCRIPTION-CONTRACTS-001` — added **CLOSED**. `TD-LONG-HORIZON-RUNWAY-DOWNWARD-CONSOLIDATION-AUTHORITY-001` and `TD-LONG-HORIZON-BOUNDED-RUNWAY-MATERIALIZATION-001` — each received an append-only `addendum` field, remain CLOSED. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — `requiredResolution` appended, remains **OPEN**. Aggregate updated to "41 risks are now recorded in total: 14 OPEN and 27 CLOSED."

## 27. Tests

70 new production-contract tests in `PreparationRunwayDirectionAndBoundedPrescriptionContractsTests.cs`, all passing on first run. Full backend suite and full plan-catalog suite results reported in the final report below (backend suite run in background due to its size).

## 28. Public/persistence/API/Flutter status

Unchanged. Zero controller, DI registration, database entity/migration, or Flutter file touched.

## 29. Final classification

```
PREPARATION_RUNWAY_DIRECTION_GUARDS_AND_BOUNDED_PRESCRIPTION_CONTRACTS_COMPLETED_DARK

PREPARATION_RUNWAY_FULL_IMMUTABLE_PRESCRIPTION_REMAINS_THE_SINGLE_NUMERIC_AUTHORITY_AND_BOUNDED_SLICES_EXPOSE_EXACT_ORIGINAL_WEEK_REFERENCES

PREPARATION_RUNWAY_UNSUPPORTED_DOWNWARD_RELATIONS_FAIL_CLOSED_AND_MID_RUNWAY_TARGET_REFRESH_REMAINS_PROHIBITED

PHASE4K_8_JIT_RUNTIME_REMAINS_UNIMPLEMENTED_BUT_ALL_PREREQUISITE_DIRECTION_AND_BOUNDED_EXPOSURE_CONTRACTS_ARE_READY
```

## 30. Exact next phase

**Phase 4K.8 — Runway and Core JIT Numeric Runtime and Mixed-Segment Window Activation** — now unblocked at the contract level; implements the actual JIT orchestration that calls these contracts.
