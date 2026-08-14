# Phase 4M.1 — Adaptation Domain Contracts and Pure Decision Policies

Date: 2026-08-07
Branch: `main`
Canonical source: `appsel-adaptation-v1-canonical-spec-.md — Revision 3.1.md` (frozen)

## 1. Canonical source used

`appsel-adaptation-v1-canonical-spec-.md — Revision 3.1.md`, found at the repository root. Read in full before any implementation. Its embedded external TypeScript reference (`appsel-adaptation-4M1/`) is treated strictly as `REFERENCE_DECISION_MODEL_ONLY` — not production evidence, not backend implementation, not canonical authority. Per the spec's own §13.8, its Superseded-denominator behavior was explicitly rejected in favor of Rev3.1 §6, and this implementation follows Rev3.1, not the TypeScript reference, at that specific point.

## 2. Files inspected

- `backend/RunningApp.Application/Adaptation/IAdaptationEngine.cs`, `AdaptationDecision.cs`, `PlaceholderAdaptationEngine.cs` — the existing legacy static-plan adaptation placeholder (`AdaptationAction`: NoChange/Skipped/Rescheduled/Shortened/RecoveryWeek). Confirmed unrelated to Rev3.1's rolling-window model and out of scope; not modified or extended.
- `backend/RunningApp.Domain/Entities/AdaptationEvent.cs`, `backend/RunningApp.Domain/Enums/AdaptationAction.cs`, migration `20260701093301_AddAdaptationEngineConstraints.cs` — same legacy subsystem, confirmed unrelated.
- `backend/RunningApp.Domain/Entities/LongHorizonRollingSessionState.cs`, `LongHorizonRollingWeekState.cs` — the real rolling-window persistence model. `SessionRole` is a persisted `string`; `LongHorizonRollingSessionOutcomeStatus` (Planned/Completed/NotToday) lives on the session; `SegmentType`/`Stage` live on the week, not the session.
- `backend/RunningApp.Domain/Enums/LongHorizonPersistenceEnums.cs` — `LongHorizonPersistedSegmentType` (GeneralEndurance/PreparationRunway/Core), `LongHorizonPersistedCoreContextStatus` (Active/Superseded — confirms Active/Superseded is already this codebase's established naming pattern for exactly this kind of dimension, reused for `SessionPlanningStatus`).
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayWeekMaterialization/PreparationRunwayWeekMaterializationContracts.cs` — `internal enum PreparationRunwaySlotRole { KeySession, EasySupport, LongRun }`, the canonical strongly-typed role.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonSessionRoleCodec.cs` — the established `internal static class` bridging `PreparationRunwaySlotRole` to its canonical uppercase-underscore wire tokens ("KEY_SESSION"/"EASY_SUPPORT"/"LONG_RUN").
- `backend/RunningApp.Application/DTOs/Plan/LongHorizonActiveReadContracts.cs`, `backend/RunningApp.Application/Services/LongHorizonRollingSessionMutationService.cs` — the live rolling NotToday endpoint's reason handling: a plain `string? Reason` request field, validated against a hardcoded `HashSet<string> { "fatigue", "soreness", "illness", "schedule", "weather", "other" }` (line 35). No `pain_or_discomfort` token exists in production today — see §26 (DecisionRequired).
- `backend/RunningApp.Application/Common/*.cs` (`RaceHorizonPolicy.cs`, `CanonicalTargetFinishTimePolicy.cs`, etc.) — the established location/pattern for pure, distance-agnostic domain policies; confirmed this phase's new code should follow the same "static pure class over typed records" shape.
- `backend/RunningApp.Application/RunningApp.Application.csproj` — confirmed `<InternalsVisibleTo Include="RunningApp.IntegrationTests" />` already exists, so new `internal` types (matching `PreparationRunwaySlotRole`'s own accessibility) are fully testable from the existing test project without any new visibility grant.
- `backend/RunningApp.sln` — confirmed project structure: Domain, Application, Infrastructure, Persistence, Api, and the single `RunningApp.IntegrationTests` project (no separate pure-unit-test project exists; existing pure-policy tests such as `RaceHorizonPolicyAuthorityTests.cs` and `CanonicalTargetFinishTimePolicyTests.cs` already live inside `RunningApp.IntegrationTests` alongside DB-backed tests, confirming that placement, not a new isolated harness, is the repository convention).

## 3. Existing architecture/contracts reused

| Rev3.1 concept | Existing backend type reused | Why |
|---|---|---|
| Session role (KEY_SESSION/EASY_SUPPORT/LONG_RUN) | `PreparationRunwaySlotRole` (`internal enum`) | Already the canonical strongly-typed role across GE/Runway/Core/rolling code; no second parallel vocabulary created. |
| ExecutionOutcome (Planned/Completed/NotToday) | `LongHorizonRollingSessionOutcomeStatus` | Exact match to Rev3.1 §5's three values; reused verbatim. |
| Phase segment component | `LongHorizonPersistedSegmentType` (GeneralEndurance/PreparationRunway/Core) | Reused as one half of the new `AdaptationPhaseIdentity` value type (paired with the Week's free-text `Stage`, since no single existing flat enum captures the full phase/stage granularity — see §6). |
| Active/Superseded naming | `LongHorizonPersistedCoreContextStatus`'s naming pattern | Not reused directly (it is CoreContext-scoped, semantically distinct from session planning status), but its exact `Active`/`Superseded` naming was mirrored for the new `SessionPlanningStatus` enum rather than inventing different names. |
| Pure-policy placement convention | `RunningApp.Application/Common/*.cs` and `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/**` | New code placed under `RuntimeCatalog/Schedule/LongHorizon/Adaptation/`, alongside the rest of the LH subsystem it reasons about, following the same "static pure class + typed record contracts" shape already used throughout that folder. |
| Test placement convention | `RunningApp.IntegrationTests/RuntimeCatalog/Schedule/**` | New pure tests placed at `RuntimeCatalog/Schedule/LongHorizon/Adaptation/`, matching existing pure-policy test placement (no new isolated test harness). |
| `InternalsVisibleTo` | `RunningApp.Application.csproj` | Already grants the test project visibility into `internal` types; no project file change needed. |

## 4. Files created

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs` — all enums, value objects, records, and the two typed exceptions.
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/ReasonClassificationPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/CandidateSelectionPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/ScheduleRepairPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/WindowExecutionSummaryBuilder.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/NextWindowLoadDecisionPolicy.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Adaptation/PlanAdaptationV1DecisionTests.cs`
- This document.

## 5. Files modified

None. Phase 4M.1 is entirely additive — no existing file was changed, consistent with its own scope boundary (no NotToday runtime integration, no reuse-by-modification of the live reason vocabulary).

## 6. Domain contracts implemented

All types are `internal` (matching `PreparationRunwaySlotRole`'s own accessibility, visible to the test project via the existing `InternalsVisibleTo`):

- `SessionPlanningStatus { Active, Superseded }`
- `ReasonClass { Operational, Safety }`
- `NotTodayReasonCode { ScheduleConflict, Travel, Weather, Tired, Illness, PainOrDiscomfort, Personal, Other }` — new, self-contained; see §26 for why it is not the live runtime's string set.
- `ScheduleRepairAction { Skip, RescheduleToEmptySlot, SubstituteFutureEasy }`
- `NextWindowLoadDecision { ProgressAsPlanned, Maintain, Reduce }`
- `AdaptationPhaseIdentity` (`readonly record struct`, wraps `LongHorizonPersistedSegmentType` + `Stage` string) — the typed, comparable phase identity for PhaseBoundaryConstraint.
- `ScheduleRepairCandidate`, `ScheduleRepairTrigger`, `ScheduleRepairDecision` — pure input/output records for schedule repair.
- `LogicalSessionEvidence`, `WindowExecutionSummary` — pure input/output records for the summary builder.
- `NextWindowAdaptationResult { LoadDecision, SafetyReviewRequired }` — the two independent dimensions, as separate fields on one record, never a combined enum (Rev3.1 §7's own corrected model).
- `AdaptationLineageInvalidException`, `ScheduleRepairTriggerInvalidException` (extends `ArgumentException`, matching the existing repository convention where `ArgumentException` maps to a 400-class validation error elsewhere in the API layer — not wired here, since no API work is in scope).

## 7. ReasonClassification behavior

`ReasonClassificationPolicy` (static, pure): `Classify` (pain_or_discomfort → Safety, everything else including illness → Operational), `BlocksReschedule` (pain_or_discomfort and illness both block; every other reason does not), `TriggersSafetyFlag` (pain_or_discomfort only). No diagnosis, severity inference, recovery-duration estimate, or return-to-running prescription anywhere in this type — verified by inspection: it contains exactly three pure boolean/enum-returning functions and nothing else.

## 8. CandidateSelectionPolicy behavior

`CandidateSelectionPolicy.SelectEarliestValid` never queries a database or calendar and never generates a candidate date — it only orders (`OrderBy(c => c.Date)`, a stable sort) an already-supplied list and returns the first entry where `IsSafetyValid` is true, or null. The stable sort is the documented tie-breaker for same-date candidates (input order), with no invented domain significance assigned to it.

## 9. ScheduleRepairPolicy behavior

`ScheduleRepairPolicy.Evaluate` first fails fast (`ScheduleRepairTriggerInvalidException`) unless `trigger.ExecutionOutcome == NotToday`. It then dispatches on `trigger.Role`:
- **EASY_SUPPORT**: always `Skip`, never inspects either candidate list (proven directly by a test that supplies valid candidates and asserts they are ignored).
- **KEY_SESSION**/**LONG_RUN**: if `ReasonClassificationPolicy.BlocksReschedule` is true, `Skip` immediately (with `SafetyFlag` set only for pain_or_discomfort). LONG_RUN additionally hard-`Skip`s whenever `trigger.IsTaper` is true, regardless of reason or candidates. Otherwise, both roles share the same `TryRepair` sequence: filter empty-slot candidates to the trigger's own `AdaptationPhaseIdentity` (defensive re-check of PhaseBoundaryConstraint, even though the caller is expected to have already filtered), select the earliest valid one via `CandidateSelectionPolicy`; if none, filter future-EASY candidates to the same phase **and** `SourceRole == EasySupport` **and** `SourcePlanningStatus == Active`, select the earliest valid one; if still none, `Skip`.
- KEY may never substitute LONG_RUN or another KEY_SESSION, and LONG_RUN may never substitute KEY_SESSION or another LONG_RUN — enforced structurally by the `SourceRole == EasySupport` filter, and proven directly by tests that supply a wrongly-roled candidate and assert it is never selected.

## 10. SingleSessionSubstitution behavior

Modeled exactly as Rev3.1 §3: a `SubstituteFutureEasy` decision only ever names an existing Active `EASY_SUPPORT` session (`ScheduleRepairCandidate.SourceSessionId`/`SourceRole`) as the target; the decision itself carries no makeup/cascade capability (`ScheduleRepairDecision` has exactly one optional session id and one optional date, nothing else). `NoMakeupPolicy`'s "effective active planned session count does not increase" invariant is a consequence of the pure decision shape: the policy never returns more than one selected candidate, and never invents a new session — it names an existing candidate or does nothing.

## 11. Taper/phase/window safety boundaries

- **TaperProtectionRule**: EASY_SUPPORT and LONG_RUN always `Skip` when `trigger.IsTaper` (LONG_RUN via an explicit early-return; EASY_SUPPORT already always Skips regardless of Taper). KEY_SESSION may still select a valid candidate during Taper — proven by test 31 — matching Rev3.1's "KEY rehearsal may only be moved unchanged" allowance. Because Phase 4M.1's decision surface (`ScheduleRepairDecision`) has no field through which distance/duration/intensity/segment/role could be modified, the "moved unchanged" content invariant is structurally guaranteed by the contract shape itself; exact materialized-content equality enforcement (once real workout content exists to compare) is explicitly deferred to Phase 4M.2, per the spec's own instruction not to invent a fake content-comparison layer here.
- **PhaseBoundaryConstraint**: `ScheduleRepairPolicy` defensively filters every candidate list to `candidate.Phase == trigger.Phase` using the typed `AdaptationPhaseIdentity`, before any candidate ever reaches `CandidateSelectionPolicy` — proven directly by tests supplying only wrong-phase candidates and asserting the result is `Skip`.
- **Window boundary**: Phase 4M.1 has no concept of "the caller's candidate list contains cross-window entries" to defend against structurally (window scoping is entirely the caller's responsibility per the spec's own boundary description in §F) — proven indirectly: an empty candidate list (the caller's way of expressing "nothing eligible in this window") always resolves to `Skip`, and the policy has no mechanism to look anywhere outside the supplied list.

## 12. WindowExecutionSummaryBuilder behavior

The single canonical authority for adaptation execution evidence (Rev3.1 §13.2) — no other type in this phase independently computes a completed/adherence count. `WindowExecutionSummaryBuilder.Build`:
1. Builds an id→evidence map, failing fast on a duplicate id.
2. Builds a children-by-source map from every non-null `AdaptedFromId`, failing fast (`AdaptationLineageInvalidException`) the moment a second child for the same source is seen, and failing fast if an `AdaptedFromId` points at an id not present in the supplied evidence.
3. Independently walks each session's `AdaptedFromId` chain backward with a per-session visited set, failing fast on any revisit (cycle).
4. Computes **roots** = every session with `AdaptedFromId == null` (this is `ExpectedSessionCount`, regardless of a root's `PlanningStatus` or `ExecutionOutcome` — see §13 for the exact Rev3.1 denominator rule).
5. For each non-Superseded root, follows its replacement chain forward to the terminal leaf and reads that leaf's `ExecutionOutcome` as the root's "effective" outcome (Completed → counts toward `EffectiveCompletedCount` and the relevant role bucket; NotToday → counts toward `UnrecoveredNotTodayCount`; Planned → counts toward neither, it is still pending).
6. For each Superseded root, increments `SupersededByAdaptationCount` and the relevant role's *expected* bucket only — never `EffectiveCompletedCount`, never `UnrecoveredNotTodayCount`.
7. `HasSafetyFlag` is computed independently over the *entire* supplied evidence set (not just roots): true if any row has `ExecutionOutcome == NotToday` and a `NotTodayReason` for which `ReasonClassificationPolicy.TriggersSafetyFlag` is true.

## 13. Rev3.1 Superseded denominator implementation

Implemented exactly per Rev3.1's own corrected §6 (not the Rev3/TypeScript-reference reading, which the spec itself rejects): a Superseded root **remains in `ExpectedSessionCount`** and in its role's expected bucket (`EasyExpectedCount`, or the boolean `KeySessionExpected`/`LongRunExpected`), is never counted in `EffectiveCompletedCount` or `UnrecoveredNotTodayCount`, and only increments the informational `SupersededByAdaptationCount`. The locked Rev3.1 example (Mon Easy Completed / Wed Key NotToday / Fri Easy Superseded / Fri Key replacement Completed / Sun Long Completed) is pinned as `LockedScenario_Rev3_1_ExactExpectedValues` and produces exactly `ExpectedSessionCount=4, EffectiveCompletedCount=3, EasyExpectedCount=2, EasyCompletedCount=1, SupersededByAdaptationCount=1, UnrecoveredNotTodayCount=0` — matching the spec's own locked numbers precisely. A dedicated test (`Summary_SupersededCount_IsInformationalOnly_DoesNotWorsenLoadDecision`) additionally proves the count never degrades `NextWindowLoadDecisionPolicy`'s output.

## 14. Lineage validation/fail-fast behavior

`AdaptationLineageInvalidException` is thrown for: (a) more than one direct replacement child for a single source session — proven to fail *before* silently picking either child (no first-child-wins fallback exists in the code path at all: the exception is thrown the moment the second child is observed, prior to any selection logic running); (b) a cycle in the `AdaptedFrom` graph, detected via a fresh visited-set walk per session; (c) a duplicate session id in the supplied evidence; (d) an `AdaptedFromId` referencing an id absent from the supplied evidence. No generic graph library was built — only the four checks the current lineage model actually requires.

## 15. NextWindowLoadDecisionPolicy behavior

`NextWindowLoadDecisionPolicy.Evaluate` computes `LoadDecision` purely from `summary.EffectiveCompletedCount` (and, at exactly 3, the role-completion booleans to distinguish "only Easy missing" from "Key or Long missing"), using the exact severity-first thresholds from Rev3.1 §7's PRODUCT DEFAULT table for the 4-session pilot (0–1→Reduce, 2→Maintain, 3-only-Easy-missing→ProgressAsPlanned, 3-otherwise→Maintain, 4→ProgressAsPlanned). `SupersededByAdaptationCount` is never read by this type. The regression constraint "0/4 must never receive a less-conservative decision than 2/4" is directly pinned by `LoadDecision_0Of4_DoesNotOutrank2Of4`.

## 16. SafetyReviewRequired behavior

`NextWindowAdaptationResult.SafetyReviewRequired` is set to exactly `summary.HasSafetyFlag`, computed and returned independently of `LoadDecision` — the two are separate fields on one record, never folded together. Three parameterized tests (`SafetyReviewRequired_IsIndependentOfLoadDecision`) prove `SafetyReviewRequired = true` coexists correctly with all three possible `LoadDecision` values, including the spec's own example of `ProgressAsPlanned` + `SafetyReviewRequired = true` simultaneously.

## 17. Tests added

`PlanAdaptationV1DecisionTests.cs` — 68 test methods (some `[Theory]`-parameterized, collectively exercising every one of the 75 numbered scenarios in the phase brief's test matrix; several closely related items are asserted together within one test method, annotated by matrix number in a trailing comment on each `[Fact]`/`[Theory]`). No database, no HTTP host, no shared mutable state — the class does not join `ApiIntegrationTestCollection` and runs fully in parallel with the rest of the suite.

## 18. Exact targeted test command/result

```
dotnet test RunningApp.IntegrationTests --no-build -c Debug --filter "FullyQualifiedName~PlanAdaptationV1DecisionTests" --nologo
```
**Result: 68 passed, 0 failed, 0 skipped**, 180 ms.

## 19. Exact affected-project test command/result

The only affected project is `RunningApp.Application` (new files only, nothing modified) and `RunningApp.IntegrationTests` (new test file). Both are exercised by the full backend suite run below; no narrower "affected project" test run is distinct from that full run in this repository's single-test-project structure.

## 20. Exact full backend test command/result

```
dotnet test RunningApp.IntegrationTests -c Debug --nologo
```
**Result: 3229 passed, 0 failed, 0 skipped**, 13 min 44 s — exactly the 3161 pre-phase baseline (Phase 4L.6D) plus the 68 new tests added in this phase. Zero regressions.

## 21. Plan-catalog test status

Not required by this phase's own scope rule ("run plan-catalog suite ONLY if shared catalog/domain contracts were touched") — Phase 4M.1 touched no existing shared Domain/Persistence/RuntimeCatalog contract; every new file is additive and none of it is referenced by any plan-catalog code path. Not run in this phase.

## 22. Build result

`dotnet build RunningApp.sln -c Debug --nologo`: **0 warnings, 0 errors** (one transient xUnit analyzer style warning — argument order in an `Assert.NotEqual` call — was found and fixed during development; the final build is clean).

## 23. Static/format/git diff checks

`git diff --check` against every file this phase touched: **no violations**. No repository-standard formatter/static-analysis command beyond the build's own analyzers (already clean) was found configured for this solution.

## 24. Scope/non-goal confirmation

Verified directly against the finished diff: no migration was added, no `AdaptationDecisionRecord` table exists, no `TrainingDays`/`LongHorizonRollingSessionState` row is created or mutated, no controller/endpoint was touched, no `NotToday`/Home/Calendar/detail/confirm/rolling-activation code path was modified, no Maintain/Reduce numeric translation exists anywhere in this code (the decision surface stops at the enum value), and no distance-specific (TenK/HalfMarathon/Marathon) branching exists in any new file — every new policy dispatches only on `PreparationRunwaySlotRole`, `NotTodayReasonCode`, `AdaptationPhaseIdentity`, and `bool IsTaper`.

## 25. Remaining 4M.2+ boundaries

Per Rev3.1 §14/§12 and this phase's own scope: persistence of `AdaptationDecisionRecord` and real replacement/superseded session writes (4M.2); wiring `ScheduleRepairPolicy` into the live rolling `NotToday` endpoint, including reconciling the reason vocabulary gap in §26 (4M.3); next-window numeric Maintain/Reduce translation and activation integration (4M.4); concurrency/idempotency key design for the "one trigger → at most one committed replacement lineage" invariant (referenced in Rev3.1 §13.4, explicitly BACKLOG); exact materialized Taper-KEY "moved unchanged" content-equality enforcement once real workout content is available to compare (§11 above).

## 26. Repository contradictions / DecisionRequired items

**`NOT_TODAY_REASON_VOCABULARY_MISMATCH` — DecisionRequired for Phase 4M.3.** The live rolling `NotToday` endpoint (`LongHorizonRollingSessionMutationService.cs:35`) validates its `reason` field against `{ "fatigue", "soreness", "illness", "schedule", "weather", "other" }` — six free-string tokens with no strong typing, no shared enum, and critically **no `pain_or_discomfort` token**, which Rev3.1's entire safety path (§4) is built on. This is not treated as a "STOP the phase" contradiction, because: (a) it governs a different, already-shipped feature explicitly out of scope for 4M.1 ("no NotToday runtime integration"); (b) it is a hardcoded `HashSet<string>` literal inside one mutation service, not a typed, shared, "authoritative representation" the spec's own §C anticipates reusing. Rather than silently picking a new product rule (e.g., unilaterally deciding `"soreness"` means `pain_or_discomfort`), this phase created a new, self-contained `NotTodayReasonCode` enum scoped to the pure adaptation domain and leaves reconciliation with the live runtime vocabulary as an explicit open decision for whoever performs Phase 4M.3's NotToday wiring — candidate resolutions include: extending the live `HashSet` with `pain_or_discomfort` and mapping the remaining five pilot tokens 1:1 to Rev3.1's eight-token vocabulary (with `personal`/`travel` currently unrepresented in the live set), or maintaining an explicit translation layer at the API boundary. No such decision was made here.

## 27. Final classification

All Phase 4M.1 completion criteria are met with real, executed evidence: real C#/.NET contracts and pure policies exist in the actual Appsel backend; existing authoritative role (`PreparationRunwaySlotRole`), outcome (`LongHorizonRollingSessionOutcomeStatus`), and segment (`LongHorizonPersistedSegmentType`) types are reused rather than duplicated; `WindowExecutionSummaryBuilder` is the single canonical authority for execution evidence; Rev3.1's corrected Superseded-denominator semantics are implemented and pinned by the exact locked example; replacement lineage counts as one logical expectation; ambiguous/cyclic lineage fails fast; the 0/4-vs-2/4 decision-ordering regression is locked; `SafetyReviewRequired` is proven independent of `LoadDecision`; explicit `NotToday` is the only implemented repair trigger (non-`NotToday` triggers fail fast); no DB mutation, API wiring, Flutter work, or numeric adaptation exists anywhere in the diff; targeted tests (68/68), the full backend suite (3229/3229), the build (0 warnings/0 errors), and `git diff --check` (clean) were all actually executed, not assumed.

**`ADAPTATION_V1_DOMAIN_CONTRACTS_AND_PURE_DECISION_POLICIES_IMPLEMENTED_AND_VERIFIED`**
