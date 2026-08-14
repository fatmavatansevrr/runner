# Phase 4K.8D — Real Session-Level Calendar Projection and Activated Numeric Week Alignment

## 1. Executive result

The final dark calendar-alignment gap is closed. Selected Runway/Core `ActivatedNumericWeek` session references now expose exact dates already produced by the real calendar composition. Structural week boundaries remain separate, and no second calendar algorithm exists.

LONG_HORIZON_REAL_SESSION_LEVEL_CALENDAR_PROJECTION_AND_ACTIVATED_NUMERIC_WEEK_ALIGNMENT_COMPLETED_DARK

## 2. Gap inherited from Phase 4K.8C

Phase 4K.8C generated and validated real per-session Runway/Core dates in `TenKPreparationRunwayDarkOrchestrationResult.CalendarComposition`, then Phase 4K.8 returned sessions whose `AssignedDate` remained absent and whose `CalendarDates` represented only a seven-day week boundary. Composition provenance and executable rolling session dates were therefore not yet the same authority.

## 3. Scope and exclusions

This phase projects and validates existing dates only. It does not redesign 4K.8/4K.8C, alter preferred-day, long-run-day, KEY/LONG spacing, week selection, direction guards, target locking, lifecycle or numeric formulas, enable downward interpolation, implement full 21–52 lifecycle retry, persist data, expose API/DTO state, register public DI or change Flutter.

## 4. Calendar authority declaration

For selected executable Runway/Core sessions, `RealCompositionResult.CalendarComposition` is authoritative. `LongHorizonCalendarAssigner.WeekStartDate` remains a structural boundary helper only. Executable per-session dates and structural week bounds are intentionally distinct.

LONG_HORIZON_SELECTED_RUNWAY_AND_CORE_ACTIVATED_WEEKS_NOW_EXPOSE_THE_EXACT_SESSION_DATES_PRODUCED_BY_THE_REAL_CALENDAR_COMPOSITION_AUTHORITIES

## 5. Existing calendar representations

`PreparationRunwayDatedSlot` owns Runway session date, weekday, numeric slot and calendar provenance. `DatedGeneratedCatalogSessionSlotSkeleton` owns Core structural ordinal, role, date, weekday and calendar provenance. `CatalogPrescribedSession` owns Core workout key/version and numeric prescription. `ActivatedNumericWeek.CalendarDates` is a week-level `(Start, End)` boundary. `LongHorizonSessionPrescriptionReference` already had `AssignedDate`; this phase adds only an optional stable structural `SessionOrdinal` and fills `AssignedDate` after authoritative mapping.

## 6. Session calendar projection contract

`LongHorizonActivatedSessionCalendarProjection` is an immutable internal record carrying global week, segment, structural ordinal, role, workout identity, exact date/weekday, preferred/long-run provenance, composition identity/version, original composed identity, context and applicable prescription/slice/target identities. One record represents one selected executable session; it calculates nothing.

## 7. Composition-to-activation adapter

`LongHorizonRealCalendarProjectionAdapter.MapSelectedWindow` selects only sessions for the activation result’s Runway/Core weeks. It preserves original ordering/identity and rejects missing, duplicate or unexpected mappings. It never calls a calendar composer, `WeekStartDate`, weekday assignment or long-run movement logic. `AlignActivationResult` copies authoritative dates into session references and validates before returning the enriched result.

## 8. Session identity matching

The key is global week + segment + structural slot ordinal + role + workout key/version. Runway uses its original prescribed structural slot directly. Core combines `DatedCoreWeeks` ordinal/role/date with `FinalPrescribedPlan` workout/distance identity through deterministic per-role occurrence ordering because the final Core session type does not retain layout-slot identity. Duplicate roles are therefore not matched by role alone, and no random ID is invented.

## 9. ActivatedNumericWeek alignment

`CalendarDates` remains the structural seven-day boundary. Every selected Runway/Core `SessionPrescriptions` entry receives the exact real-composition `AssignedDate`, stable ordinal, workout identity and composition-source provenance. An activated Runway/Core week with unresolved session dates cannot cross the 4K.8C success boundary.

## 10. Runway projection

First Runway entry builds one immutable full Runway calendar projection tied to the immutable numeric `PrescriptionId` and full global range. Current slices expose exact sessions while retaining the current `SliceId`, target lock, local/global coordinates, workout/pace/numeric provenance and stage identity. `PreSpecificTransition` remains the original final full-prescription week.

## 11. Core projection

Selected Core weeks combine exact dates/ordinals from real `DatedCoreWeeks` with exact numeric/workout identities from the real `FinalPrescribedPlan`. Original Core local/global numbering and context are retained. Only current selected weeks leave the composition boundary.

## 12. GE→Runway mixed continuity

GE `ActivatedNumericWeek` objects are returned unchanged; no GE date is recomposed. Boundary validation compares the existing GE executable dates when available, otherwise its structural end boundary, against the first real Runway session. Duplicate or non-chronological boundaries block the complete window.

## 13. Runway→Core mixed continuity

Runway sessions come from the original locked full calendar projection; Core sessions come from the current real Core composition. The result requires chronological, non-overlapping, distinct dates, four sessions per full week, preferred/long-run-day validity and preserved identity. A failure blocks every week in the mixed window.

## 14. Core-only continuation

Only selected current Core weeks receive executable dates. Future Core weeks remain `NumericPending` and have no lifecycle projection. A later Core context produces its own deterministic composition/projection identity and cannot rewrite dates already returned by an earlier activation.

## 15. Calendar alignment validator

`LongHorizonActivatedCalendarAlignmentValidator` verifies exact session count, one numeric-to-composed mapping, unique dates, date/weekday consistency, structural week membership, preferred-day membership, long-run-day equality, role/workout/global-week/segment/context equality and chronological adjacent boundaries. Typed missing, duplicate, identity and alignment exceptions retain internal detail.

## 16. Existing validator reuse

The real composer continues to run its dated-skeleton, preferred-day, long-run-day, uniqueness, spacing and Runway/Core continuity validators. Phase 4K.8D consumes their validated output and adds projection alignment only. It then invokes `LongHorizonActivatedNumericWeekValidator`, activation-window validation and atomicity validation. No existing formula is copied.

## 17. Phase 4K.8 runtime integration

The accepted alternative integration is used: 4K.8C obtains real composition, invokes the unchanged 4K.8 runtime, enriches that internal success with authoritative dates, validates alignment and only then returns success. Partial undated success never leaves the orchestrator. Window selection and lifecycle logic remain exclusively 4K.8 authorities.

## 18. Atomicity

Projection is part of the composition activation transaction. Mapping, date, identity, availability, boundary or final validation failure returns `CompositionBlocked`, null `ActivationResult`, zero session projection, one reason and an internal typed diagnostic. The caller’s lifecycle is not mutated, and no target/prescription is exposed as an activated success.

LONG_HORIZON_MIXED_WINDOW_CALENDAR_ALIGNMENT_IS_ATOMIC_AND_FAILURE_LEAVES_ZERO_NEWLY_ACTIVATED_WEEKS

## 19. Failure taxonomy

Missing, duplicate and identity mismatch use `JIT_EVIDENCE_CONFLICT_UNRESOLVED`. Preferred-day, long-run-day or within-week infeasibility uses `JIT_AVAILABILITY_INFEASIBLE`. GE→Runway/Runway→Core chronology failure uses `JIT_SEGMENT_TRANSITION_INFEASIBLE`. No public reason was added; internal exceptions provide precise diagnostics.

## 20. Result contract

`LongHorizonRollingJitCompositionResult` now carries the aligned activation result, selected session projection, immutable full Runway calendar projection where applicable, deterministic projection identity, real composition provenance, validation stages, context and an optional blocked diagnostic. Blocked results expose none of the executable projection.

## 21. Determinism

Projection, full Runway calendar and Core context identities use SHA-256 over explicit context/composition/session values. Identical input reproduces identities and dates. Selected window, preferred-day, long-run-day or future Core-context changes affect the relevant identity. No random GUID or current-clock read participates.

## 22. Proof no second calendar authority

The new adapter source contains no calendar-composer invocation, `WeekStartDate`, `AssignedDate` calculation, weekday-selection algorithm, long-run-day algorithm, KEY/LONG spacing rule, session-ordering formula or week-boundary formula. It selects, maps and validates existing output only.

LONG_HORIZON_WEEK_LEVEL_CALENDAR_BOUNDARIES_REMAIN_STRUCTURAL_ONLY_AND_NO_SECOND_SESSION_CALENDAR_ALGORITHM_IS_INTRODUCED

## 23. Dark integration

All new contracts and services are internal and independently testable. No public DI registration, preview/confirmation wiring, persistence repository, migration, endpoint, DTO, `TrainingDay` creation or Flutter change exists.

## 24. Governance artifacts

`TD-LONG-HORIZON-ACTIVATED-SESSION-CALENDAR-PROJECTION-001` is `CLOSED`. Append-only Phase 4K.8D updates are present on `TD-LONG-HORIZON-JIT-REAL-CORE-CONDITION-CALENDAR-COMPOSITION-001`, `TD-LONG-HORIZON-RUNWAY-CORE-JIT-RUNTIME-001` and `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001`. The redesign remains `OPEN`.

## 25. Tests

Focused tests cover authority, caller boundaries, one-to-one identity, exact Runway dates and continuation, terminal-stage dates, selected Core dates and pending futures, GE preservation, Runway/Core continuity, preferred/long-run days, final-stage order, atomic tamper rejection, internal diagnostic retention, deterministic identities and no second calendar formula. Phase 4K.8C, 4K.8 and 4K.8B regressions and the full backend suite are required before completion.

## 26. Public/persistence/API/Flutter status

Unchanged. No rolling result is public or persisted; no API, DTO, DI, confirmation, preview or Flutter surface changed.

LONG_HORIZON_PUBLIC_PREVIEW_PERSISTENCE_API_AND_FLUTTER_REMAIN_UNCHANGED

## 27. Final classification

`LONG_HORIZON_REAL_SESSION_LEVEL_CALENDAR_PROJECTION_ALIGNED_WITH_ACTIVATED_NUMERIC_WEEKS_DARK`. This closes only the final 4K.8C calendar projection gap; it is not public activation or full lifecycle validation.

## 28. Exact next phase

Next: **Phase 4K.9 — Full 21–52 Rolling Dark Lifecycle Validation, Retry and Boundary Matrix**. Do not begin it in this pass.
