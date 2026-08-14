# Phase 4K.6 — Rolling General Endurance Numeric Runtime and Initial Window Implementation

## 1. Executive result

The first real Long-Horizon rolling numeric runtime is implemented as a dark, internal service. It materializes the complete 21–52-week structural roadmap and atomically activates only GE weeks `1..min(4, GeneralEnduranceWeeks)`. Every later GE, Preparation Runway, and Core week remains `NumericPending` with null executable fields.

## 2. Inherited policies

The implementation preserves Phase 4K.1's complete roadmap/four-week activation model, Phase 4K.2's unchanged progression and recovery authorities, Phase 4K.3's distinction between initial and checkpoint activation, Phase 4K.4's deferred Runway/Core JIT boundary, and Phase 4K.5/4K.5A's typed contracts and evidence classifications.

## 3. Scope and explicit exclusions

This phase implements only initial onboarding activation for GE. It does not aggregate `TrainingDay` history, calculate `ValidatedSustainableLoad`, evaluate Growth/Maintenance transitions, activate a second window, recover a blocked window, execute Runway/Core numeric or JIT paths, create a Core target lock, persist results, expose public DTOs, change endpoints, register public DI, or change Flutter.

## 4. Runtime entry point

`ILongHorizonRollingInitialActivationRuntime.BuildInitialActivationAsync` is implemented by `LongHorizonRollingInitialActivationRuntime`. The service is `internal`, independently invokable, and absent from public/live request paths. `ILongHorizonRollingGeWindowMaterializer` is its sole numeric dependency and receives only the already-selected GE prefix.

## 5. Eligibility boundary

The input validator accepts only Race, exact 10K, Intermediate, four days per week, an authoritative supported Long-Horizon decision with 21–52 full structural weeks, a valid readiness profile, valid onboarding load, four distinct preferred days including the long-run day, and supported catalog inputs. Existing 8–14 Core, 15–20 Runway+Core, Habit, other distances/levels/frequencies, and 53+ behavior is untouched.

## 6. Structural roadmap materialization

`LongHorizonStructuralMaterializer` remains the composition authority. Its complete skeleton is projected into `LongHorizonStructuralRoadmap`: `GE = TotalWeeks - 20`, Runway `= 8`, Core `= 12`, GE → Runway → Core, contiguous global weeks `1..TotalWeeks`, and retained stage, role, profile, catalog, materializer, and policy provenance. No second composition algorithm was created.

## 7. Initial activation context

The context uses `InitialOnboardingActivation`. `OriginalOnboardingEvidence` is represented as `LegacyCurrentProductionSource` for this initial-window-only exception, never as `CompletedTrainingHistory`, checkpoint-validated evidence, or rolling Core authority. Safety and feasibility flags remain required. A stable SHA-256-derived decision identity and `LongHorizonContextVersion.Initial(stableId)` avoid random domain output.

## 8. Initial-window selection

The production validator enforces `StartGlobalWeek=1`, `EndGlobalWeek=min(4, GE weeks)`, requested size `4`, actual size equal to that end week, GE as the sole covered segment, initial source, and context sequence `1`. Selection validates before GE numeric execution.

## 9. Partial-window behavior

Total weeks 21/22/23 activate GE weeks 1 / 1–2 / 1–3 only. No Runway week is borrowed and no synthetic recovery is introduced. Total weeks 24–52 activate GE weeks 1–4 only. Only a real structural GE Week 4 receives the existing recovery treatment.

## 10. Existing GE numeric authorities reused

The bounded adapter calls `LongHorizonGeNumericExecutor` unchanged over only the selected descriptors. That executor continues to own `VolumeSafetyPolicy.Default` progression limits (0.07 preferred, 0.08 hard ceiling, 2.5 km absolute cap), 0.5 km rounding, the existing long-run shares and 0.40 hard cap, the 0.85 real Week-4 recovery rule, and `FourDaySessionDistanceAllocationPolicy`. Catalog workout references come from the existing GE selector/materializer. No numeric policy value or formula changed.

## 11. Numeric-week mapping

Each activated GE week maps to `ActivatedNumericWeek` with its global week, GE segment, `NumericActivated`, weekly volume, long run, four positive session references, workout key/version provenance, assigned session dates, week calendar range, pace/evidence context, numeric-policy provenance, and deterministic context version. The opaque session references point to existing catalog identities and numeric results rather than duplicating prescription logic.

## 12. Future NumericPending mapping

All weeks after the activated prefix map to `NumericPending`. Weekly volume, long run, session prescription, and calendar executable fields remain null; zero is never used for absence. Structural stage, role, segment, numbering, and provenance remain available in the complete roadmap. No hidden future GE prescription is computed.

## 13. Atomicity

Activation is all-or-nothing. A catalog, numeric, allocation, calendar, week-contract, or atomicity failure returns zero activated weeks, a blocked window, `NumericActivationBlocked` for the selected prefix, `NumericPending` for the remaining suffix, and one typed failure. The original exception type and message are retained; no partially activated result is returned.

## 14. Failure taxonomy

The minimal Phase 4K.6 taxonomy distinguishes invalid structural horizon, invalid eligibility, invalid onboarding evidence, catalog resolution, GE numeric infeasibility, session allocation infeasibility, calendar assignment, activated-week contract failure, activation-window atomicity, safety/feasibility, and final-result validation. Existing typed exceptions are mapped without replacing their diagnostic type/message with a generic catch-all.

## 15. Validator chain

Production invokes validators in this order: (1) input/eligibility, (2) structural roadmap, (3) initial activation context, (4) pre-execution activation-window selection, (5) bounded GE materializer/result shape, (6) each activated numeric week, (7) window atomicity and final window shape, and (8) final result. The result records completed validation stages for direct verification.

## 16. Dark integration level

Integration level A was selected: an independently invokable internal production service exercised by tests. Preview wiring would add unnecessary scope, so no dark-discard call was added to the live preview pipeline and no DI registration was introduced.

## 17. Proof no full-upfront execution occurred

The runtime's only numeric port accepts an already-bounded descriptor list. A capturing test proves a 52-week roadmap sends exactly GE week indexes 1–4 in one call. Later GE results are never requested. The runtime contains no Runway numeric materializer, Core generator, JIT-context builder, or target-lock dependency; all future GE/Runway/Core weeks remain pending.

## 18. Determinism

Identical request, catalog, and policy inputs reproduce the same structural roadmap, activation window, numeric prescriptions, dates, provenance, decision identity, window identity, and context version. No timestamp or random GUID is emitted by this runtime.

## 19. Governance artifacts

`TD-LONG-HORIZON-ROLLING-GE-INITIAL-RUNTIME-001` is recorded `CLOSED` for this bounded dark capability. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` remains `OPEN`: the initial window narrows the implementation gap but checkpoint aggregation/evaluation, later-window activation, Runway/Core JIT, persistence, public preview, and Flutter remain outstanding. Registry JSON/Markdown counts are 37 total, 14 open, 23 closed.

## 20. Tests

Focused real-catalog runtime: 10 passed, 0 failed, 0 skipped. Partial-window: 9 passed, 0 failed, 0 skipped. No-full-upfront: 2 passed, 0 failed, 0 skipped. Atomicity/validator: 8 passed, 0 failed, 0 skipped. Combined new backend coverage: 29 passed, 0 failed, 0 skipped. Existing rolling typed-contract regression: 52 passed, 0 failed, 0 skipped. Focused governance/parity: 31 passed, 0 failed, 0 skipped. Full backend: 2,547 passed, 0 failed, 0 skipped. Full plan-catalog: 1,069 passed, 0 failed, 0 skipped. Final Release build: 0 warnings, 0 errors.

## 21. Public/persistence/API/Flutter status

Public responses are unchanged. No public DTO, controller, endpoint, API routing, persistence entity/repository/migration, DI registration, or Flutter file was changed. Current Core/Runway behavior and 53+ rejection remain owned by their existing paths.

## 22. Final classification

`LONG_HORIZON_ROLLING_GENERAL_ENDURANCE_INITIAL_NUMERIC_RUNTIME_COMPLETED_DARK`

`LONG_HORIZON_21_TO_52_STRUCTURAL_ROADMAP_MATERIALIZES_WITH_ONLY_THE_INITIAL_ONE_TO_FOUR_GENERAL_ENDURANCE_WEEKS_NUMERICALLY_ACTIVATED`

`LONG_HORIZON_FUTURE_GENERAL_ENDURANCE_RUNWAY_AND_CORE_WEEKS_REMAIN_NUMERIC_PENDING_WITHOUT_FULL_UPFRONT_EXECUTION`

`LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_AND_CHECKPOINT_RECALCULATION_REMAIN_UNCHANGED`

## 23. Exact next phase

Phase 4K.7 — Checkpoint Evidence Aggregation, State Evaluation and Next-Window Activation Runtime.
