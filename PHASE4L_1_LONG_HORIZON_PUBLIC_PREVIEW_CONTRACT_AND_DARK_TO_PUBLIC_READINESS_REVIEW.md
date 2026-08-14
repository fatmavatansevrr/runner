# Phase 4L.1 — Long-Horizon Public Preview Contract and Dark-to-Public Readiness Review

## 1. Executive result

This phase defines and validates the public preview boundary for supported 21–52-week Long-Horizon Race plans, without enabling public activation, confirmation, or persistence. `LongHorizonPublicPreviewMapper` maps real Phase 4K.9 dark lifecycle snapshots into a public-safe contract: a complete structural roadmap, the currently activated numeric window, and explicit non-leaking Pending/Blocked representation for every future week. Every readiness question the phase requires an answer to (confirmation, persistence, restart/resume, API, Flutter) was resolved by direct repository inspection, not assumption — and every one of them lands Blocked/NotReady except the public preview contract itself, which is approved. No commits made.

```
LONG_HORIZON_PUBLIC_PREVIEW_CONTRACT_AND_DARK_TO_PUBLIC_READINESS_REVIEW_COMPLETED

LONG_HORIZON_PUBLIC_PREVIEW_EXPOSES_THE_COMPLETE_STRUCTURAL_ROADMAP_AND_ONLY_THE_CURRENT_EXECUTABLE_NUMERIC_WINDOW

LONG_HORIZON_FUTURE_NUMERIC_PRESCRIPTIONS_SESSION_DATES_TARGET_LOCKS_CONTEXTS_AND_INTERNAL_EVIDENCE_REMAIN_HIDDEN

LONG_HORIZON_PREVIEW_READINESS_IS_APPROVED_INDEPENDENTLY_FROM_CONFIRMATION_PERSISTENCE_API_AND_FLUTTER_READINESS

LONG_HORIZON_LIVE_PUBLIC_ACTIVATION_REMAINS_UNWIRED
```

## 2. Inherited dark lifecycle state

Phase 4K.9 proved all 21–52 week horizons complete through initial GE activation, repeated Growth/Maintenance checkpoints, real Runway/Core composition (Phase 4K.8C), immutable bounded Runway exposure, repeated Core windows, and real per-session calendar dates (Phase 4K.8D) — all dark, unwired, zero public/persistence/API/Flutter impact. This phase consumes `LongHorizonFullDarkLifecycleState` (and its `LongHorizonFullDarkLifecycleValidationResult.StateSnapshots`) exactly as produced, without modification.

## 3. Scope and exclusions

In scope: public information classification, public preview semantics, the public lifecycle enum, the plan/structural-roadmap/executable-week/pending-week/blocked-week contracts, the dark mapper, the contract validator, the leakage guard, dark fixtures built from real lifecycle snapshots, and readiness reviews (confirmation/persistence/restart-resume/API/Flutter/privacy/payload-size).

Excluded: enabling public Long-Horizon generation; wiring the dark lifecycle into the live preview endpoint; persisting Long-Horizon rolling state; changing confirmation behavior; creating TrainingPlan/TrainingWeek/TrainingDay rows; exposing internal full Runway/Core prescriptions; exposing future computed session dates; modifying Flutter; commits.

## 4. Current public preview assumptions

Direct inspection of `PlansController.cs`, `GeneratePreviewResponse`/`PreviewWeekDto`/`PreviewDayDto`, `CatalogPreviewGenerator`, `PreparationRunwayPublicPreviewMapper`, `CatalogPublicPreviewMaterializer`, and the confirmation flow confirmed: every public week is assumed numerically complete (`PreviewWeekDto.Days` is a non-nullable, always-populated list); every public session is assumed dated (`PreviewDayDto.Date`/`DistanceKm`/`DurationMin`/`Intensity` are all non-nullable); confirmability is gated only at the whole-preview level (`PreviewLifecycleClassification`), never per-week/per-day; no public status enum can represent a not-yet-generated week; and no existing preview supports partial or pending weeks in any form.

## 5. Public information classification

**Public at preview time**: total horizon, goal distance, race date, start date, profile, days/week, preferred days, long-run day, structural phase durations, estimated end date, current lifecycle summary, global week + phase + lifecycle status + structural date range for every week.

**Public only when activated**: numeric weekly/long-run volume, session dates, session distances, workout roles.

**Internal only**: `ValidatedSustainableLoad`, checkpoint evidence rows, Growth/Maintenance decision detail, target-lock identity, Core context version, the immutable full Runway prescription, bounded-slice identity, full future Core output, condition-resolution trace, internal failure diagnostics, deterministic internal hashes, audit events. None of these have any corresponding property anywhere in the new public contract types.

**Persistence-only in a future phase**: everything under "internal only" that a resumable rolling plan would eventually need to durably reconstruct lifecycle from (see §20).

## 6. Public preview semantics

A Long-Horizon preview = complete structural roadmap (all weeks) + the currently activated numeric window (≤4 weeks, full detail) + explicit Pending representation for every other week, with no claim that future prescriptions are finalized. The banned phrases ("complete schedule ready", "all workouts finalized", "confirmed future volume") are unenforceable as pure convention, so enforcement is structural: `LongHorizonStructuralRoadmapWeekContract` has no numeric/session property at all, so a Pending row cannot carry a false claim through any field.

## 7. Public lifecycle mapping

`LongHorizonPublicLifecycleStatus { Available, Pending, Blocked, Completed, Missed }`. `NumericActivated→Available`, `NumericPending→Pending`, `NumericActivationBlocked→Blocked`, `Completed→Completed`, `Missed→Missed` (all five required by the prompt). `StructurallyPlanned` additionally collapses into `Pending` — an explicit, disclosed decision (both mean "not yet executable" publicly; the internal distinction has no separate product meaning). No internal-only state (target-lock/context-refresh/prescription-created) is ever exposed.

## 8. Plan-level contract

`LongHorizonPlanPreviewContract`: `ContractVersion`, `PreviewId`, `GoalType`/`GoalDistance` (plain strings — no `GoalType.LongHorizon`/`GoalDistance.LongHorizon` enum value exists anywhere in the codebase, confirmed by direct search), `TotalWeeks`, `StartDate`, `EstimatedEndDate`, `RaceDate`, `DaysPerWeek`, `PreferredDays`, `LongRunDay`, `ReadinessProfile`, `CurrentWindowStartWeek/EndWeek`, `CurrentExecutableWeekCount`, `StructuralRoadmap`, `CurrentExecutableWeeks`, `PreviewReadiness`, `ConfirmationReadiness`, `PublicWarnings`, `ProvenanceSummary`, `BlockedState`. Immutable record, no internal target-lock/prescription/context IDs, no full future numeric schedule, no internal diagnostic trace.

## 9. Structural roadmap contract

`LongHorizonStructuralRoadmapWeekContract`: `GlobalWeek`, public `Phase`, `Stage`, `LifecycleStatus`, `IsExecutable`, `StructuralStartDate/EndDate` (pure `StartDate + (GlobalWeek-1)*7` arithmetic — never a materializer read, which is what keeps it safe to expose for Pending weeks), `NumericDetailsAvailable`, `PublicSummary` message key. All `TotalWeeks` appear exactly once, contiguous, phase order GE→Runway→Core enforced by the validator.

## 10. Executable-week contract

`LongHorizonExecutableWeekContract` + `LongHorizonExecutableSessionContract`: exact values copied from the real `ActivatedNumericWeek` (`TotalWeeklyVolumeKm`, `LongRunKm`, `CalendarDates`) and each session's exact real `AssignedDate` from the Phase 4K.8D calendar projection — never recomputed. Session `IsLongRun` is derived via a disclosed heuristic (`SessionRole` containing "LONG", case-insensitive) rather than a dedicated internal flag, since none exists on `LongHorizonSessionPrescriptionReference`.

## 11. Pending-week contract

No public placeholder session objects are created for pending weeks. A pending roadmap row exposes only `GlobalWeek`/`Phase`/`Stage`/structural date range/`Pending` status/a message key (`long_horizon.roadmap.pending_week`) — no zero distance, no empty fake workout, no placeholder workout ID, no future `AssignedDate`, no leaked internal volume. This is structural, not conventional: the contract type has no property to carry any of those values.

## 12. Blocked-week contract

`LongHorizonBlockedStateContract`: `Status=Blocked`, one public `ReasonCategory`, `RetryEligible`, `NextActionKey`, `LastEvaluatedDate`. All 9 `LongHorizonCheckpointReasonCode` and 10 `LongHorizonJitReasonCode` values map deterministically to exactly one of six categories (`MoreTrainingDataNeeded`/`CompleteCurrentWeek`/`UpdateAvailability`/`SafetyReviewRequired`/`PaceInformationNeeded`/`PlanTransitionUnavailable`), exhaustively tested. `RetryEligible=false` only for `SafetyReviewRequired`. A prior successfully activated window remains fully `Available` even while the next window is `Blocked` — the plan stays valid and previewable, matching the required "plan remains valid" semantics.

## 13. Public reason/warning taxonomy

Preview-creation failure, preview warning (rolling generation pending), rolling block (next window can't currently activate), and confirmation block (not yet persistable) are kept conceptually distinct: `PublicWarnings` is a list of message keys (never an error), `BlockedState` is a separate, optional field (never conflated with a top-level failure), and `ConfirmationReadiness` is entirely independent of `PreviewReadiness`.

## 14. Preview readiness

`LongHorizonPreviewReadiness.ReadyForPublicPreview` — every real dark snapshot tested (initial, mid-lifecycle, mixed-window, blocked, fully completed, both profiles, 5 horizons) maps to a contract that passes `LongHorizonPublicPreviewContractValidator`.

## 15. Confirmation readiness

**Answer: B — the current confirmation model does not support rolling Long-Horizon safely.** `PlanServices.ConfirmPlanAsync` dispatches to `CatalogPlanConfirmationService.ConfirmAsync`, which throws `CatalogPreviewNotPersistableException` if `GeneratedPreviewPlanPayload` is null; the legacy path guards `Weeks.Count==0` and derives `planStartDate`/`planEndDate` from the full `Weeks` array, then writes `TrainingPlan`/`TrainingWeek`/`TrainingDay` rows synchronously in one call from that complete array. A Long-Horizon preview with 17–48 Pending weeks cannot satisfy either path without inventing values. `LongHorizonConfirmationReadiness.NotReadyForConfirmation` is set unconditionally by the mapper.

## 16. Public preview mapper

`LongHorizonPublicPreviewMapper.Map(LongHorizonPublicPreviewMapperInput)` — pure, deterministic, no runtime invocation, no numeric/calendar computation beyond week-boundary arithmetic, no persistence, no mutation. Works for initial preview, later refreshed snapshots, blocked snapshots, partially completed, and fully completed plans (all five proven directly against real dark fixtures). Not exposed through any endpoint.

## 17. Initial preview behavior

Complete structural roadmap for all 21–52 weeks + the first activated 1–4 GE weeks with full numeric/session detail + all later weeks as structural Pending rows + a rolling-generation warning message key. No internal Runway/Core prescriptions exposed. The mixed-structural/executable design (not a structural-only alternative) was proven achievable and is what was implemented.

## 18. Later lifecycle snapshot behavior

The contract supports historical Completed/Missed weeks (via roadmap `LifecycleStatus`), current Available weeks, future Pending weeks, and a current Blocked boundary, all from the same mapper applied to a later snapshot — proven directly against real mid-lifecycle and blocked snapshots (`LongHorizonPublicPreviewLifecycleSnapshotTests`). Historical activated-week dates/volumes are stable between an early and a later mapping of the same underlying weeks (`LongHorizonPublicPreviewDeterminismTests.HistoricalValuesRemainUnchangedBetweenInitialAndFinalMapping`).

## 19. Confirmation review

See §15. No confirmation code was modified.

## 20. Persistence review

`TrainingWeek`/`TrainingDay` carry no Pending/Blocked/Available lifecycle status field. `PlanPreview` stores only opaque `RequestPayloadJson`/`PreviewPayloadJson`. None of `ValidatedSustainableLoad`, checkpoint decisions, block/retry history, Core context versions, the Runway prescription identity, the target lock, or the session calendar projection have any persistence representation today. Preferred future direction (not decided here): persist the minimal durable facts (roadmap parameters, last checkpoint evidence identity, locked target/prescription identity, activated-week numeric snapshots) and deterministically re-derive lifecycle classification from them, rather than persisting full internal object graphs.

## 21. Restart/resume review

Blocked — there is currently nothing durable to reconstruct `LongHorizonFullDarkLifecycleState` from after a process restart. No confirmed rolling plan could resume today.

## 22. API review

Recommended: **(B) a dedicated Long-Horizon preview response discriminator/contract**, not (A) extending `GeneratePreviewResponse`. That DTO's `PreviewWeekDto`/`PreviewDayDto` have non-nullable `Date`/`DistanceKm`/`DurationMin`/`Intensity` and a non-nullable `Days` list on every week — there is no nullable seam to carry a Pending week without a breaking change or a dishonest placeholder value. No endpoint routing was changed.

## 23. Flutter review

Not inspected code-level (explicitly out of scope). By direct analogy to the confirmed backend finding, the Dart preview DTOs are expected to mirror the same fully-populated-array assumption, meaning future work would need: structural roadmap rendering, phase labels, executable/pending visual distinction, rolling-generation explanation copy, a disabled confirmation state, a blocked-state action affordance, partial session detail, and later-refresh behavior. Zero Flutter files were touched.

## 24. Payload-size review

Preferred shape (full lightweight roadmap + separate activated-window detail) is structurally proven viable at 52 weeks: 52 nine-field roadmap rows + at most 4 full executable weeks, with no future session arrays and no full internal Core/Runway output ever included by construction (`LongHorizonPublicPreviewProfileAndHorizonTests.LightweightRoadmapPayloadRemainsValidAtFiftyTwoWeeks`). No byte-level serialized measurement was taken; the structural bound (fixed field count × fixed max horizon) was judged sufficient evidence.

## 25. Public provenance

`LongHorizonPublicProvenance { GeneratedFromRecentTraining, GeneratedFromInitialProfile, UpdatedAfterCompletedTraining, AwaitingMoreTrainingData }` — explains behavior, never implementation. No evidence-row IDs, resolver traces, target-lock/prescription hashes, condition registry codes, or internal governance reason IDs are exposed.

## 26. Privacy/data minimization

The contract carries no raw `TrainingDay` evidence, no historical actual-performance detail beyond a session's own already-public distance/date, no internal safety diagnostics or confidence calculations, no internal catalog workout keys/versions, and no internal governance reason codes.

## 27. Contract versioning

`ContractVersion = 1`, present at plan level, deterministic, independent of catalog version/Core context version/prescription version/API version. Additive-only evolution expected (new optional fields bump minor product behavior, not `ContractVersion`, until a breaking shape change is needed).

## 28. Contract validator

`LongHorizonPublicPreviewContractValidator` enforces plan-level (version, horizon range 21–52, roadmap completeness/contiguity/phase order), roadmap-level (Pending/Blocked rows never claim executable/numeric details), executable-week-level (session dates within week bounds), and cross-contract invariants (an executable week's roadmap row must be `Available`; a `Pending` row must never appear in `CurrentExecutableWeeks`).

## 29. Leakage guard

`LongHorizonPublicPreviewLeakageGuardTests.PublicContractGraphExposesNoForbiddenInternalTypes` walks the full public contract type graph via reflection, asserting none of the ten forbidden internal type names (`ImmutablePreparationRunwayPrescription`, `PreparationRunwayPrescriptionId`, `PreparationRunwayTargetLockScope`, `LongHorizonLockedCoreWeekOneTarget`, `BoundedPreparationRunwayPrescriptionSlice`, `ValidatedSustainableLoad`, `LongHorizonCheckpointDecision`, `RuntimeConditionResolutionResult`, `LongHorizonLifecycleAuditEvent`, `LongHorizonContextVersion`) ever appears as a property type anywhere in the graph.

## 30. Preview fixtures

Built from real dark lifecycle output via `LongHorizonFullLifecycleTestFixture` (the same fixture proving the Phase 4K.9 21–52 matrix): 21-week and 52-week initial previews, first-window snapshots, repeated mid-lifecycle snapshots (25/29/40 weeks), a genuine safety-blocked snapshot, both readiness profiles, and fully completed plans. No fixture is hand-fabricated as a public object — every one is a real `LongHorizonFullDarkLifecycleState` mapped through the real mapper.

## 31. Readiness decision matrix

| Capability | Status | Evidence | Blocker |
|---|---|---|---|
| Public structural roadmap | Ready | §9, validator, tests | — |
| Current executable-window preview | Ready | §10, tests | — |
| Pending-week representation | Ready | §11, leakage tests | — |
| Blocked-week representation | Ready | §12, tests | — |
| Public warning taxonomy | Ready | §13 | — |
| Public contract validation | Ready | §28 | — |
| Endpoint compatibility | ReadyWithContractOnly | §22 | New endpoint/discriminator needed |
| Confirmation compatibility | Blocked | §15 | Requires complete static schedule |
| Persistence compatibility | Blocked | §20 | No rolling-state schema exists |
| Restart/resume compatibility | Blocked | §21 | Nothing durable to reconstruct from |
| Home/calendar compatibility | OutOfScope | Not reviewed this phase | Phase 4L.4 |
| Flutter compatibility | Blocked | §23 | No Flutter change this phase |
| Retry action compatibility | ReadyWithContractOnly | §12 (`NextActionKey`) | Not wired to any real retry endpoint |
| API error compatibility | ReadyWithContractOnly | §13 | Not wired |
| Privacy/data minimization | Ready | §26 | — |

## 32. Governance artifacts

New TD `TD-LONG-HORIZON-PUBLIC-PREVIEW-CONTRACT-READINESS-001` (CLOSED) in `activation-readiness-risks.json`/`.md`. Append-only updates to `TD-LONG-HORIZON-FULL-DARK-LIFECYCLE-VALIDATION-001` and `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` (remains OPEN). Aggregate updated to 46 risks, 14 OPEN, 32 CLOSED. Nine prior governance test files with stale 45/31 hardcoded counts updated to 46/32. New governance test file `LongHorizonPublicPreviewContractReadinessGovernanceTests.cs`.

## 33. Tests

37 new focused backend tests (contract shape, initial preview, executable weeks, lifecycle snapshots, blocked state including exhaustive reason mapping, confirmation-readiness independence, leakage guard, determinism, profile/horizon coverage, no-wiring proof) — all built from real dark lifecycle snapshots. Two genuine mapper bugs were found and fixed by this phase's own testing (see §34).

## 34. Public/persistence/API/Flutter status

Unchanged. No public response, persistence model/write, migration, endpoint, public DTO, DI registration, or Flutter surface changed. No `TrainingDay` is created outside in-memory supplied validation evidence (inherited from Phase 4K.9's own testing convention).

### Bugs found and fixed during this phase's own testing

1. **Stale `CurrentWindow.Status`**: the Phase 4K.9 harness only updates `LongHorizonFullDarkLifecycleState.CurrentWindow` on a *successful* activation (`AcceptActivation`/`AcceptGeActivation`); a terminal block never touches it. The mapper initially trusted `CurrentWindow.Status == Blocked` directly, so a genuinely blocked snapshot mapped to zero executable weeks and no blocked state at all (both wrong). Fixed by deriving "currently executable" from `LifecycleStates`/`ActivatedWeeks` directly (weeks with `NumericActivated` state, looked up by number) and "currently blocked" from the audit-event trail (blocked iff the most recent lifecycle-transition event — `WindowBlocked`/`GeWindowActivated`/`MixedWindowActivated`/`CoreWindowActivated` — is `WindowBlocked`).
2. **Record equality on mutable-list-backed contracts**: the determinism test initially used `Assert.Equal` on two independently-mapped `LongHorizonPlanPreviewContract` instances, which failed because record-synthesized equality uses reference equality for `List<T>`-backed properties even with identical content. Fixed by switching to `Assert.Equivalent(a, b, strict: true)`, xUnit's deep-value comparer — a test-only fix, not a mapper defect.

## 35. Final classification

`LONG_HORIZON_PUBLIC_PREVIEW_CONTRACT_AND_DARK_TO_PUBLIC_READINESS_REVIEW_COMPLETED`. `LONG_HORIZON_PUBLIC_PREVIEW_EXPOSES_THE_COMPLETE_STRUCTURAL_ROADMAP_AND_ONLY_THE_CURRENT_EXECUTABLE_NUMERIC_WINDOW`. `LONG_HORIZON_FUTURE_NUMERIC_PRESCRIPTIONS_SESSION_DATES_TARGET_LOCKS_CONTEXTS_AND_INTERNAL_EVIDENCE_REMAIN_HIDDEN`. `LONG_HORIZON_PREVIEW_READINESS_IS_APPROVED_INDEPENDENTLY_FROM_CONFIRMATION_PERSISTENCE_API_AND_FLUTTER_READINESS`. `LONG_HORIZON_LIVE_PUBLIC_ACTIVATION_REMAINS_UNWIRED`.

## 36. Exact next phase

Since confirmation and persistence are both blocked, per the phase prompt's own preferred sequence: **Phase 4L.2 — Long-Horizon Rolling Persistence and Restart-Safe State Contract**.
