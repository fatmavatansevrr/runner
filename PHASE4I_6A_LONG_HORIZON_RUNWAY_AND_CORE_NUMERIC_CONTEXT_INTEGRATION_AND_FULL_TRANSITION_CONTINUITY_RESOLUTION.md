# Phase 4I.6A — Long-Horizon Runway and Core Numeric Context Integration and Full GE→Runway→Core Transition Continuity Resolution

## 1. Executive result

This phase does **not** meet its own success gate ("do not report success while any Runway or Core numeric field remains absent" for every supported horizon), so it is honestly classified **B (blocked)** — but real, substantial, working progress was delivered and precisely bounded. Real, unchanged Preparation Runway numeric/calendar/pace execution and real, unchanged Core numeric/pace/binding/dating execution now run end-to-end for **21- and 24-total-week Long-Horizon plans, both readiness profiles**, using GE's exit state as Runway's entry evidence and real `RuntimeConditionResolutionService` resolution (not a fabricated stand-in) for Core. During verification, a genuine, previously-unknown **existing-pipeline compatibility gap** was discovered and precisely bounded: for longer GE horizons (confirmed failing at 28/40/52 total weeks with a typical baseline), the existing Preparation Runway numeric materializer has no accepted rule for "entry evidence exceeds the Core Week-1 boundary target" outside of Taper — GE's approved compounding progression over many weeks legitimately exceeds Core's own independently-computed peak-volume-band ceiling. This is not a defect in this phase's own new code; it is real, existing, unmodified production validation correctly failing closed. No new numeric rule or constant was invented to paper over it. No commits made.

Final classification:

```
LONG_HORIZON_FULL_NUMERIC_EXECUTION_REMAINS_BLOCKED_BY_EXPLICIT_GE_TO_RUNWAY_ENTRY_CONTEXT_CORE_RUNTIME_CONTEXT_OR_EXISTING_PIPELINE_COMPATIBILITY_GAP
```

## 2. Phase 4I.6 partial state

Phase 4I.6 completed GE numeric execution, Core workout binding (identity only), and full calendar assignment, deliberately leaving Preparation Runway numeric execution and Core numeric/pace execution unexecuted for the 21-52 week path.

## 3. Exact remaining blockers (inherited)

Preparation Runway numeric execution; Core numeric/pace execution; GE→Runway and Runway→Core numeric transition/continuity; final complete-plan numeric validation.

## 4. Scope and non-scope

In scope: GE exit-state contract, Runway entry-context decision/adapter, real Runway numeric/calendar/pace execution, real Core numeric/pace/binding/date execution via real runtime-condition resolution, cross-segment continuity validation, focused/regression tests, governance updates. Out of scope (unchanged): GE progression redesign, new Runway/Core numeric algorithms, public preview, confirmation, persistence, Flutter, 53+, other candidates.

## 5. Artifacts inspected

`CatalogWorkoutBinder`/`ProgressionStageAllocator`/`CatalogWeekSkeletonCalendarMaterializer` (confirmed workout binding is separable from numeric prescription); `PreparationRunwayStartingLoadEvidence`/`PreparationRunwayStartingLoadEvidenceAdapter.FromAuthoritativeCoreContext`; `PreparationRunwayCoreWeekOneTargetAdapter`/`PreparationRunwayCoreWeekOnePaceAdapter`; `PreparationRunwayNumericMaterializer`/`PreparationRunwayCalendarComposer`/`PreparationRunwayPaceMaterializer` and their exact request/policy shapes; `TenKPreparationRunwayCoreGenerator`/`TenKPreparationRunwayCoreGenerationRequest`; `PreparationRunwayCalendarAuthorityAdapter.Create`/`PreparationRunwayDateAuthority.Derive` (proven pure date arithmetic, independent of GE/numeric evidence); `TenKPreparationRunwayFinalInvariantValidator` (proven not standalone-reusable — cross-referential over one orchestration's own state); `TenKPreparationRunwayDarkOrchestratorTests.cs`/`PreparationRunwayHorizonAuthorityTests.cs` (real vs. fabricated runtime-condition-resolution patterns); `RuntimeConditionResolutionService.ResolveAllResults`; `CatalogPreviewGenerator.BuildInputSnapshot`; `CatalogPrescribedPlan`/`CatalogPrescribedWeek`/`CatalogPrescribedSession` shapes; `DynamicCoreCalendarMaterializationResult` and its full sub-result chain (`PrescriptionResult.VolumeResult.{VolumeAndLongRunPlan,PrescriptionContext,BindingResult.Skeleton}`, `PrescriptionResult.FinalPrescribedPlan`).

## 6. Runway required-context inventory

`PreparationRunwayStartingLoadEvidence(WeeklyVolumeState, RecentWeeklyVolumeKm, LongestRunState, RecentLongestRunKm, RecentRunsPerWeek, SourceProvenance)` — existing type, existing consumer (`PreparationRunwayNumericMaterializer`), existing producer for 15-20 week plans (`PreparationRunwayStartingLoadEvidenceAdapter.FromAuthoritativeCoreContext`, Core-derived). For 21-52 week plans this is prior-segment-derived (GE exit), available from Phase 4I.6's own validated GE numeric result, reusable unchanged as the SAME record type via a new adapter (`LongHorizonGeExitState` → `PreparationRunwayStartingLoadEvidence`, built inline in the orchestrator). `PreparationRunwayCoreWeekOneNumericTarget`/`PreparationRunwayCoreWeekOnePaceTarget` (Runway's exit boundary) — candidate-derived + user-evidence-derived, produced by running Core's own numeric decision once with the ORIGINAL (unadapted) evidence; this is a deliberate architecture decision (§9), not a gap.

## 7. Core required-context inventory

`GeneratePreviewRequest`/`ResolverInputSnapshot`/`ConditionResults` (including a real `GOAL_FEASIBILITY_IN` result) — all user-evidence/candidate-derived, produced by the existing `RuntimeConditionResolutionService` (four real resolvers: `TimeAdequacyResolver`, `PaceSourceResolver`, `CoreEntryReadinessResolver`, `GoalFeasibilityResolver`), reused unchanged and invoked for real (not fabricated) in this phase. `CoreStartDate` — prior-segment-derived (GE+Runway span), computed via pure date arithmetic (`PreparationRunwayDateAuthority.Derive`), independent of any numeric evidence. No required Core context field was found unavailable — every field is either already present in the Long-Horizon dark flow's own inputs or derivable via an existing, unchanged authority.

## 8. GE exit-state contract

`LongHorizonGeExitState` (new): `FinalGeWeekIndex`, `FinalWeeklyVolumeKm`, `FinalLongRunKm`, `PreviousNonCutbackPeakVolumeKm`, `FinalStageFamily`, `FinalWeekWasRecovery`, `FinalWeekWasTerminalAlignment`, `ReadinessProfile`, numeric policy provenance. `.From(descriptors, numeric, profile)` derives exclusively from Phase 4I.6's own already-validated `LongHorizonGeWeekNumericResult` sequence — never recalculates GE progression.

## 9. Runway entry-context decision

**Decision: `PrecedingGeneralEnduranceExit`** — for 21-52 week plans, Runway's entry evidence is GE's final weekly volume and final long-run distance (`LongHorizonGeExitState.FinalWeeklyVolumeKm`/`FinalLongRunKm`), packaged into the SAME `PreparationRunwayStartingLoadEvidence` type the 15-20 week path already uses, with `SourceProvenance = "PrecedingGeneralEnduranceExit"`. **Architecture decision on Runway's exit boundary** (`PreparationRunwayCoreWeekOneNumericTarget`): computed from Core's own numeric decision run against the ORIGINAL, unmodified user evidence — exactly as the 15-20 week path already does — rather than from any GE- or Runway-adapted value. Rationale: Core's Week-1 target represents a fixed, evidence-derived fitness/volume level appropriate for entering the race-specific final phase, independent of how many weeks precede it; recomputing it from Runway's own output would require running Runway before Core, which needs Core's target to exist first — a circular dependency this decision avoids. This also guarantees the EXISTING Runway→Core transition mechanism and Core's own prescription are reused byte-for-byte, satisfying "do not rebuild Runway or Core numeric algorithms."

## 10. Existing 15-20 entry behavior

Fully preserved and untouched: no file under `PreparationRunwayOrchestration`/`PreparationRunwayNumericMaterialization`/`PreparationRunwayCalendarComposition`/`PreparationRunwayPacePrescription` was modified. `TenKPreparationRunwayDarkOrchestrator` continues to call `PreparationRunwayStartingLoadEvidenceAdapter.FromAuthoritativeCoreContext` exactly as before.

## 11. Long-Horizon Runway entry mode

Implemented as a new, additive construction path (`LongHorizonFullNumericOrchestrator`) that builds a `PreparationRunwayStartingLoadEvidence` directly from `LongHorizonGeExitState`, then calls the same `PreparationRunwayNumericMaterializer.Materialize` static entry point the 15-20 week path uses — no boolean flag, no horizon-number check inside the Runway algorithm, no duplicate numeric engine. This satisfies the "additive typed entry context, exhaustive switch, no nullable-source ambiguity" preference: the source is explicit at the construction call site, not inferred at runtime.

## 12. Runway numeric execution

All eight Runway weeks receive weekly volume, long-run distance, and per-slot distance from the real, unchanged `PreparationRunwayNumericMaterializer`; pace/effort and workout provenance from the real, unchanged `PreparationRunwayPaceMaterializer`; dates from the real, unchanged `PreparationRunwayCalendarComposer`. Profile block allocation and workout binding are the exact Phase 4I.5 structural output, unchanged. Confirmed for 21/24-week plans (both profiles): exactly 8 weeks, no progression-cap violation, correct `PreSpecificTransition` terminal block.

## 13. GE→Runway volume continuity

`GeToRunwayVolumeContinuity_WithinApprovedCaps` proves Runway Week 1's volume never exceeds GE's final volume by more than `max(GE_final × 0.08, 2.5km)` — the existing approved caps, applied at the transition boundary exactly as within GE itself.

## 14. GE→Runway long-run continuity

Not independently re-validated beyond what the existing `PreparationRunwayNumericMaterializer`'s own internal long-run-share logic already enforces on the supplied starting evidence (reused unchanged) — a disclosed scope reduction (see §38).

## 15. 20→21 numeric comparison

Not executed this phase (disclosed scope reduction, see §38) — Phase 4I.5 already proved 20→21 **structural** suffix continuity; a full 20-week-baseline vs. 21-week Long-Horizon-baseline NUMERIC comparison (classification A/B/C per the phase's own taxonomy) was not performed.

## 16. Runway exit-state contract

Not implemented as a separate named type — the orchestrator consumes `pace.PacedRunwayWeeks` (the real `PreparationRunwayPacedWeek<TKey>` output) directly when merging into the final schedule; no independent reproduction of Runway's final values occurs.

## 17. Core execution-context adapter

Implemented inline in `LongHorizonFullNumericOrchestrator`: `BuildPreviewRequest`/`BuildResolverInputSnapshot` construct the exact existing `GeneratePreviewRequest`/`ResolverInputSnapshot` shapes (mirroring `CatalogPreviewGenerator.BuildInputSnapshot`'s field mapping), then `RuntimeConditionResolutionService.ResolveAllResults` (real, unchanged, four real resolvers) produces real `ConditionResults`. `TenKPreparationRunwayCoreGenerationRequest` is then built directly with these real values plus the pure-arithmetic `CoreStartDate`.

## 18. Runtime-condition reuse

`RuntimeConditionResolutionService` is invoked directly and unchanged (`TimeAdequacyResolver`, `PaceSourceResolver`, `CoreEntryReadinessResolver`, `GoalFeasibilityResolver`, canonical order, `PriorResults` threading internal to the service) — no parallel/reduced runtime-condition implementation was created anywhere in this phase.

## 19. Goal-feasibility handling

The representative pilot input (product-average target time, no recent race, `RecentWeeklyVolumeKm`/`RecentLongestRunKm` from the caller's GE baseline input) resolves through the real `GoalFeasibilityResolver` exactly as any live request would; unsupported/fail-closed behavior is not loosened anywhere.

## 20. Target-time/pace-source handling

`TargetFinishTimeSource.ProductAverage` with `TargetFinishTimeSeconds = 3480` (the canonical TEN_K product-average value) is used for the representative fixture; this flows unchanged through `PreparationRunwayPaceContextAdapter.FromAuthoritativeCoreContext`, reusing `corePrescriptionContext.PaceSource` verbatim — never recomputed independently for Runway.

## 21. Core numeric execution

All 12 Core weeks (Foundation 3/Build 4/Race Specific 4/Taper 1) receive real weekly volume, long-run distance, per-slot distance, pace, and workout key/version from `core.PrescriptionResult.FinalPrescribedPlan` — the exact same `CatalogPrescribedPlan` the existing 8-14/15-20 week paths produce, via the exact same `DynamicCoreCalendarMaterializationOrchestrator` pipeline, unchanged. Confirmed for 21/24-week plans (both profiles): correct 3/4/4/1 phase structure, all weeks numeric.

## 22. Runway→Core continuity

`RunwayToCoreTransition_CoreWeekOneIsFoundation_NoDuplicateTransitionWeek` confirms the last Runway week is `PreSpecificTransition`, the first Core week is `FOUNDATION`, and their global week numbers are exactly adjacent (no extra transition week, no gap) — for 21/24-week plans.

## 23. Full three-segment continuity

Proven for 21- and 24-total-week plans (both profiles): GE→Runway volume continuity within caps, clean single Runway→Core transition, all three segments fully numeric/bound/dated. **Not proven** for 28-52 week plans — see §26/§38.

## 24. Final complete-plan contract

`LongHorizonExecutedSchedule`/`LongHorizonExecutedWeek`/`LongHorizonExecutedWorkoutSlot` (Phase 4I.6's contracts, reused unchanged) now carry non-null numeric/pace/date/workout fields for ALL THREE segments for the horizons where execution succeeds — no zero-as-absence-marker, no placeholder.

## 25. Final validator

`LongHorizonFullExecutionValidator.Validate` — fail-closed, aggregate. Delegates structural checks to `LongHorizonStructuralValidator` (excluding its now-superseded null-Core-workout finding), then checks: full numeric/workout/date completeness per slot across all segments; GE→Runway volume-continuity cap; Runway/Calendar/Pace stage success flags; `ContinuityAnalysis.IsValid` from the existing Runway calendar/pace stages (reused, not re-derived); Runway/Core week counts; Core Week 1 = FOUNDATION; `core.PrescriptionResult.FinalPrescribedPlan.ValidationResult.IsValid`.

## 26. Invalid-context behavior

20-week horizon rejected (delegates to the unchanged Phase 4I.3/4I.5 rejection). Longer GE horizons (28/40/52 weeks, typical baseline) correctly throw with the existing, unmodified Runway reason `"Starting weekly volume exceeds Core Week 1 and no approved non-taper runway reduction rule exists"` — proven as an EXPECTED, correct fail-closed result (`LongerGeHorizons_FailClosed_ExistingRunwayNoNonTaperReductionRule`), not a crash or silently-wrong output.

## 27. Full 21-52 matrix

**Not completed.** Confirmed working: 21 and 24 total weeks, both profiles, typical baseline. Confirmed failing closed (existing-pipeline gap, not a defect): 28, 40, 52 total weeks, typical baseline. The exact boundary between "works" and "fails closed" was not exhaustively bisected (disclosed scope reduction, see §38) — it lies somewhere in GE 5-8 weeks (25-28 total weeks) for the tested baseline, and likely varies with baseline magnitude.

## 28. 52-week proof

**Not achieved.** 52 weeks (both profiles, both a "typical" and a lower "high-baseline-but-still-bounded" 30km input) fails closed on the same existing-pipeline gap. No 52-week full-numeric proof exists for this phase.

## 29. Existing 8-20 regression

No file belonging to the 8-14 or 15-20 week paths was modified by this phase. The full backend suite (§34) is the regression proof; the exact same real production classes used by the unmodified `TenKPreparationRunwayDarkOrchestrator`/`CatalogPreviewGenerator` are reused, never forked or edited.

## 30. Dark integration level

**Level A** — one new internal orchestrator (`LongHorizonFullNumericOrchestrator`), invoked only by focused tests. Not registered in DI. Zero references outside its own production files and its own test file (confirmed by grep). No public preview call site; no public DTO modified.

## 31. Public containment

Not re-verified by a new end-to-end HTTP test this phase — the reasoning is unchanged from Phase 4I.5/4I.6: this phase adds zero call sites into `PlanServices`/`CatalogPreviewGenerator`, so the existing Phase 4I.3 `LongHorizonGenerationContainmentTests.cs` (real Api host + real Postgres, 21/24/52/53 weeks) remains valid, current evidence, included unmodified in the full backend suite result.

## 32. Governance status

`TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001`: **remains OPEN**, updated with Phase 4I.6A's real progress and the newly discovered, precisely-bounded gap. `TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001`: **added OPEN** (not closed — full success was not achieved). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`: remains OPEN.

## 33. Focused tests

14 new tests in `LongHorizonFullNumericOrchestratorTests.cs`: representative-horizon full-numeric success (21/24 weeks, both profiles where applicable), Runway 8-week numeric completeness, Core 12-week numeric completeness with phase-structure regression, GE→Runway volume-continuity cap, Runway→Core transition cleanliness, determinism, 20-week rejection, short-GE-horizon success matrix, longer-GE-horizon fail-closed matrix (proving the discovered gap is a correct, existing, reproducible rejection). Plus 6 governance cross-check tests in plan-catalog (1 new file, 1 updated pre-existing test). All passing.

## 34. Full backend suite

**2446 passed, 0 failed, 0 skipped** (9m40s), up from the Phase 4I.6 baseline
of 2432 due to this phase's 14 new tests, after fixing 6 pre-existing
dark-reachability governance tests (see §37).

## 35. Full plan-catalog suite

**920 passed, 0 failed, 0 skipped** (914 baseline + 6 new/updated governance tests). No plan-catalog production file was modified by this phase (only governance JSON/MD and test files).

## 36. Production defects found

None. The one real finding (existing Runway numeric materializer has no non-Taper reduction rule) is not a defect — it is the existing, unmodified, correctly-fail-closed behavior of production code that was never previously exercised with an entry-exceeds-target input, because no prior phase's dark path ever fed it one.

## 37. Production fixes made

None to pre-existing production ALGORITHM code. All Phase 4I.6A production files are new additions. One production bug in this phase's OWN new code was found and fixed during verification: the Preparation Runway calendar authority was initially anchored at the overall plan `StartDate` instead of GE's own end date, causing its internal `AvailableFullWeeks - PreferredCoreWeeks` invariant to mismatch the actual 8-week Runway numeric output for any GE length other than 0 — fixed by anchoring the authority at `StartDate + geWeeks×7` (GE's end date), matching exactly the same shape the existing 15-20 week path already uses (pre-Core span → Core span), generalized correctly to GE+Runway together.

Additionally, 6 pre-existing dark-reachability governance tests (`DynamicCoreWorkoutBindingOrchestratorTests`, `DynamicCoreWeekSkeletonOrchestratorTests`, `DynamicCoreVolumeAndLongRunOrchestratorTests`, `DynamicCoreSessionPrescriptionOrchestratorTests`, `DynamicCoreDarkEndToEndHarnessTests`, `DynamicCoreCalendarMaterializationOrchestratorTests`) correctly flagged `LongHorizonFullNumericOrchestrator` as a second production call site into Core's dynamic pipeline classes (the first being `TenKPreparationRunwayDarkOrchestrator`'s own construction). This is exactly what those tests are designed to catch, and they caught it correctly — not a defect in the new orchestrator's design (deliberate, disclosed reuse of the exact same real Core pipeline), but a governance allowlist gap this phase's new file created, mirroring the identical situation and fix already established in Phase 4I.5. Each test's exclusion list was extended to recognize `Schedule/LongHorizon` as a second approved dark consumer.

## 38. Explicit non-implementation statement

This phase does **not** implement: numeric execution for GE horizons beyond ~24 weeks total (the existing-pipeline gap, §26-28); a full 20-week-baseline vs. 21-week-Long-Horizon-baseline NUMERIC comparison/classification (Part 8 of the prompt); a separate named `LongHorizonRunwayExitState` contract (the orchestrator consumes the real Runway pace/calendar output directly instead); exhaustive GE→Runway long-run-continuity re-validation beyond what the existing Runway numeric materializer already enforces internally; the full 21-52×2-profile×5-input-category matrix; month/year calendar-boundary-specific fixtures for this phase's own new date arithmetic (Phase 4I.6 already covers the underlying date-arithmetic primitive). Public preview, confirmation, persistence, Flutter, and 53+ remain untouched, as required.

## 39. Residual blockers

`TD-LONG-HORIZON-NUMERIC-CALENDAR-EXECUTION-001` / `TD-LONG-HORIZON-RUNWAY-CORE-NUMERIC-CONTEXT-INTEGRATION-001` (both OPEN): the existing Runway numeric materializer's missing non-Taper volume-reduction rule for longer GE horizons; full 21-52×2-profile matrix execution; a formal 20→21 numeric classification. `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` (OPEN, umbrella): all of the above, plus dark pipeline wiring, public preview, confirmation, and persistence.

## 40. Final classification

```
LONG_HORIZON_FULL_NUMERIC_EXECUTION_REMAINS_BLOCKED_BY_EXPLICIT_GE_TO_RUNWAY_ENTRY_CONTEXT_CORE_RUNTIME_CONTEXT_OR_EXISTING_PIPELINE_COMPATIBILITY_GAP
```

Specifically: the GE→Runway entry-context and Core-runtime-context integration questions this phase set out to answer are RESOLVED (§9/§17); the remaining blocking gap is a genuine, previously-unknown existing-pipeline compatibility limitation in the Preparation Runway numeric materializer, discovered and precisely bounded by this phase's own verification work.

## 41. Exact next phase

**Phase 4I.6B — Preparation Runway Non-Taper Volume-Reduction Rule Resolution (Governance + Dark Implementation)**: this is a governance decision first, not a code phase — determine, with real evidence (canonical rule files, existing product defaults, or an explicit "no evidence exists, cannot invent" outcome matching this session's established pattern), whether and how a Runway entry volume exceeding the Core Week-1 boundary should be handled (a bounded reduction rule? a capped plateau? explicit rejection of that horizon/baseline combination as unsupported?). Only after that decision is recorded should a follow-up implementation phase extend `LongHorizonFullNumericOrchestrator` to the full 21-52 matrix.
