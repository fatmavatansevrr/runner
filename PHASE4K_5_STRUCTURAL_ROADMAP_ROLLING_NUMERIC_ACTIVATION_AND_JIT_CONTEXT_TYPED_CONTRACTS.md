# Phase 4K.5 — Structural Roadmap, Rolling Numeric Activation and JIT Context Typed Contracts

## 1. Executive result

This phase implements the production typed contracts required by Phases 4K.1–4K.4's approved rolling-lifecycle policies — the first Long-Horizon phase in this whole sequence to write real production code, though deliberately dark and unwired: contracts and validators only, called from no controller, no DI registration, no existing orchestrator, and no database. Eleven contract areas were implemented across 14 files in a new `RollingActivation` namespace: structural roadmap, numeric lifecycle, activation window, numeric week, checkpoint evidence snapshot, validated-load, checkpoint decision, evidence authority, JIT context, Core Week-1 target lock, context versioning, and initial-activation distinction. The single most consequential design decision was the evidence-authority guard: `LongHorizonEvidenceAuthorityRecord.Create` structurally throws if any caller attempts to mark `OriginalOnboardingEvidence` as `Authoritative` for the rolling runtime — the exact silent-defaulting this phase's own "Critical Authority Distinction" section forbids. A new `TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001` was opened, deliberately unresolved, rather than assumed. No commits made.

Final classification:

```
LONG_HORIZON_STRUCTURAL_ROADMAP_ROLLING_NUMERIC_ACTIVATION_AND_JIT_TYPED_CONTRACTS_COMPLETED

LONG_HORIZON_CORE_TARGET_ROLLING_EVIDENCE_AUTHORITY_REMAINS_EXPLICITLY_OPEN_AND_IS_NOT_SILENTLY_DEFAULTED_TO_LEGACY_ONBOARDING_EVIDENCE

LONG_HORIZON_ROLLING_RUNTIME_REMAINS_DARK_AND_BLOCKED_PENDING_CHECKPOINT_EVALUATOR_GE_EVIDENCE_SOURCE_MIGRATION_AND_NUMERIC_RUNTIME_IMPLEMENTATION
```

## 2. Inherited approved policies

Phase 4K.1: structural roadmap, 4-week rolling window, six lifecycle states. Phase 4K.2: validated-load/maintenance formulas. Phase 4K.3: checkpoint evidence, freshness, deterministic transition table, nine checkpoint reasons. Phase 4K.4: Runway/Core JIT timing, atomic resolution, ten JIT reasons, the Core Week-1 target immutability-plus-versioned-refresh hybrid.

## 3. Scope and non-scope

In scope: typed contracts and validators for all nine areas Phase 4K.5's prompt names (Parts 1–14), plus typed exceptions.

Explicitly out of scope: rolling numeric generation, checkpoint aggregation from persisted `TrainingDay` rows, GE/Runway/Core algorithm changes, persistence/API/Flutter changes, public preview activation, resolving `TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001` by assumption.

## 4. Structural roadmap contracts

`LongHorizonStructuralRoadmap`/`LongHorizonStructuralSegment`/`LongHorizonStructuralWeek` (`LongHorizonStructuralRoadmapContracts.cs`), reusing Phase 4I.3's `ReadinessProfile` rather than duplicating it. `LongHorizonStructuralRoadmapValidator` enforces `TotalWeeks = GE + 8 + 12`, `GE = TotalWeeks - 20`, the 21–52 bound, contiguous/unique global week numbers, and GE→Runway→Core segment order.

## 5. Numeric lifecycle contracts

`LongHorizonNumericLifecycleState` (the same six Phase 4K.1 states) plus `LongHorizonNumericLifecycleTransitionValidator` (`LongHorizonNumericLifecycleContracts.cs`). `Completed`/`Missed` have zero legal outgoing transitions. `NumericActivationBlocked → NumericPending` is only reachable via a dedicated `ValidateBlockedRecoveryTransition` overload requiring an explicit `hasNewCheckpointDecision: true` flag — it cannot be triggered by the ordinary transition path.

## 6. Activation-window contract

`RollingNumericActivationWindow` plus `LongHorizonRollingActivationWindowValidator` (`LongHorizonRollingActivationWindowContracts.cs`): preferred 4-week size, partial windows, contiguous global weeks, and mixed-window atomicity (`ValidateAtomicity` — an `Activated` window's weeks must ALL be `NumericActivated`; a `Blocked` window must carry zero activated weeks).

## 7. Numeric-week contract

`ActivatedNumericWeek` plus `LongHorizonActivatedNumericWeekValidator` (`LongHorizonNumericWeekContracts.cs`): `NumericPending`/`StructurallyPlanned`/`NumericActivationBlocked` weeks must carry only null executable fields (never a fabricated zero); `NumericActivated`/`Completed`/`Missed` weeks require a complete prescription, positive session distances, and session distances summing to the approved weekly total. Session content is represented as an opaque `LongHorizonSessionPrescriptionReference` rather than reusing the existing rich session-prescription types, to avoid creating a second, competing prescription authority.

## 8. Checkpoint evidence snapshot

`LongHorizonCheckpointEvidenceSnapshot` (`LongHorizonCheckpointEvidenceContracts.cs`) — a typed projection of Phase 4K.3's approved conceptual snapshot. Does not aggregate from persisted `TrainingDay` rows (explicitly out of scope).

## 9. Validated-load contract

`ValidatedSustainableLoad` plus `LongHorizonValidatedSustainableLoadValidator` (`LongHorizonValidatedLoadContracts.cs`): a `Valid` load's source must be `CompletedTrainingHistory` or `PriorValidatedCheckpointLoad` (never a planned target); completed-evidence weeks and excluded-recovery weeks can never overlap. Represents the approved output shape only — the mean-of-non-recovery-weeks calculation itself is not implemented.

## 10. Checkpoint decision and reasons

`LongHorizonCheckpointDecision` plus `LongHorizonCheckpointDecisionValidator` (`LongHorizonCheckpointDecisionContracts.cs`): `GrowthEligible` never carries a reason; `MaintenanceOnly`/`NumericActivationBlocked` always carry exactly one checkpoint-taxonomy reason. `LongHorizonCheckpointReasonCode` (`LongHorizonReasonCodeContracts.cs`) implements all nine Phase 4K.3 codes as a typed enum, never free text.

## 11. Evidence source versus authority

`LongHorizonEvidenceSource` (8 values) and `LongHorizonEvidenceAuthorityStatus` (5 values: `Authoritative`/`FallbackApproved`/`ProvenanceOnly`/`LegacyCurrentProductionSource`/`UnresolvedForRollingRuntime`) plus `LongHorizonEvidenceAuthorityRecord.Create` (`LongHorizonEvidenceAuthorityContracts.cs`) — the guarded static factory that throws `LongHorizonEvidenceAuthorityDefaultingException` if any caller marks `OriginalOnboardingEvidence` as `Authoritative`.

## 12. Current production versus approved rolling sources

`LongHorizonEvidenceAuthorityCatalog` records the approved mapping for every named field: Runway's rolling weekly/long-run authority = `CompletedTrainingHistory`/`Authoritative` (an explicit, disclosed future change from today's `LongHorizonGeExitState.From`, not yet implemented in the actual GE-exit computation); planned GE exit = `ProvenanceOnly` (rolling direction) and separately = `LegacyCurrentProductionSource` (describing today's code, both records coexist); Core Week-1's current production source = `OriginalOnboardingEvidence`/`LegacyCurrentProductionSource`; Core Week-1's rolling authority = `OriginalOnboardingEvidence`/`UnresolvedForRollingRuntime` — deliberately not `Authoritative`.

## 13. JIT context contract

`LongHorizonJitContextDecision` (`JitContextApproved`/`JitContextBlocked`) plus `LongHorizonJitContextValidator` (`LongHorizonJitContextContracts.cs`), enforcing Phase 4K.5's own Part 10 invariant: if `RunwayIncluded` is true, `LockedCoreWeekOneTarget` must be non-null, `ResolvedAtomically` must be true, and `RollingAuthorityStatus` must be explicitly stated.

## 14. JIT reasons

`LongHorizonJitReasonCode` (`LongHorizonReasonCodeContracts.cs`) implements all ten Phase 4K.4 codes, deliberately without a duplicate safety value. `LongHorizonReasonCode` is the shared top-level abstraction: a `readonly record struct` wrapping either a checkpoint or a JIT reason, with `LongHorizonReasonCode.SafetyReassessmentRequired` as the single shared instance both taxonomies reuse.

## 15. Core target lock

`LongHorizonLockedCoreWeekOneTarget` (`LongHorizonCoreTargetLockContracts.cs`) — immutable; `Refresh()` returns a new instance. `LongHorizonCoreTargetLockValidator.ValidateRefresh` throws `LongHorizonLockedTargetImmutabilityViolationException` if a refreshed target's locked week range overlaps the prior target's already-locked range, or if its `ContextVersion` isn't strictly later.

## 16. Context versioning

`LongHorizonContextVersion` (`LongHorizonContextVersion.cs`) — immutable, `Initial()`/`Next()` both return new instances. Every checkpoint decision, JIT context decision, activation window, and locked Core target carries one.

## 17. Initial activation distinction

`LongHorizonInitialActivationSource` plus `LongHorizonInitialActivationContext`/`Validator` (`LongHorizonInitialActivationContracts.cs`): rejects `InitialOnboardingActivation` sourced from `CompletedTrainingHistory` (no completed history exists yet) and rejects `CheckpointRollingActivation` sourced from `OriginalOnboardingEvidence` (the two must never be conflated); safety/feasibility validation remains required even for the initial window.

## 18. Validators

9 validator classes covering all 15 areas Part 15 lists (several share one validator where the underlying invariant is identical — e.g. evidence-authority-explicitness and legacy-source-non-silent-defaulting are both enforced together by `LongHorizonEvidenceAuthorityRecord.Create` and `LongHorizonJitContextValidator`). All use typed exceptions extending the new `LongHorizonRollingContractException` base (`Code` + `InvalidOperationException`), matching the existing `CatalogSessionPrescriptionException` convention exactly.

## 19. Integration boundary

Zero existing public DTOs replaced; zero preview-response changes; zero orchestrator behavior changes; zero DI registration changes; zero database entity/migration changes. Confirmed by a clean build of `RunningApp.Application` and `RunningApp.Api` with no file outside the new `RollingActivation` folder touched.

## 20. Unresolved Core target evidence authority

`TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001` (new, **OPEN**, blocking): the rolling runtime's final Core Week-1 evidence authority is not approved. `LongHorizonEvidenceAuthorityCatalog.CoreWeekOneRollingAuthority` records this as `UnresolvedForRollingRuntime`, structurally prevented from silent upgrade. Required resolution (verbatim from this phase's own prompt): determine which current validated evidence becomes authoritative for the Core Week-1 target in rolling JIT execution without retroactively invalidating already-activated Runway weeks — not resolved by assumption here.

## 21. Governance artifacts

`TD-LONG-HORIZON-ROLLING-TYPED-CONTRACTS-001` — added **CLOSED**. `TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001` — added **OPEN**, blocking. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — updated, remains **OPEN** (not closed, per this phase's explicit instruction — rolling generation/persistence/preview/Flutter remain unimplemented). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — unchanged, remains OPEN. Aggregate updated to "36 risks are now recorded in total: 15 OPEN and 21 CLOSED."

## 22. Tests

**Backend**: 52 new tests in `LongHorizonRollingActivationTypedContractsTests.cs`, all passing on first run, covering all 32 required proof points. Full backend suite: **2506 passed, 0 failed, 0 skipped**. **Plan-catalog**: 14 new governance cross-check tests in `LongHorizonRollingTypedContractsGovernanceTests.cs`, all passing on first run. Full plan-catalog suite: **1049 passed, 0 failed, 0 skipped** (1035 baseline + 14 new).

## 23. Final classification

```
LONG_HORIZON_STRUCTURAL_ROADMAP_ROLLING_NUMERIC_ACTIVATION_AND_JIT_TYPED_CONTRACTS_COMPLETED

LONG_HORIZON_CORE_TARGET_ROLLING_EVIDENCE_AUTHORITY_REMAINS_EXPLICITLY_OPEN_AND_IS_NOT_SILENTLY_DEFAULTED_TO_LEGACY_ONBOARDING_EVIDENCE

LONG_HORIZON_ROLLING_RUNTIME_REMAINS_DARK_AND_BLOCKED_PENDING_CHECKPOINT_EVALUATOR_GE_EVIDENCE_SOURCE_MIGRATION_AND_NUMERIC_RUNTIME_IMPLEMENTATION
```

## 24. Exact next phase

**Phase 4K.5A — Core Week-1 Rolling JIT Evidence Authority Resolution** — a narrow governance phase resolving `TD-LONG-HORIZON-CORE-TARGET-EVIDENCE-AUTHORITY-001`, required before any Runway/Core rolling runtime implementation.
