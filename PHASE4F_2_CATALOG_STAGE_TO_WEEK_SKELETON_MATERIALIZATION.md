# Phase 4F.2 — Catalog Stage-to-Week Skeleton Materialization

Implements the first schedule-materialization layer: selected catalog stages
→ plan-relative numbered weeks → week date boundaries → stage assignment per
week → structural session-slot skeletons. **Does not generate complete
workout prescriptions, is not wired into any live request path, and does not
enable catalog confirm.**

## 1. Files inspected

- `PHASE4F_1_PERSISTABLE_CATALOG_SCHEDULE_CONTRACT.md`, `PHASE4F_1_CHECKPOINT_REPORT.md`, `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/GeneratedCatalogPlanPayload.cs`, `GeneratedCatalogPlanPayloadValidator.cs` (all Phase 4F.1 contract types)
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs`, `CatalogPreviewGenerator.cs`, `CatalogPlanConfirmationService.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/*` (resolver/selected-condition representations — confirmed no "selected stage sequence" concept exists there; resolvers produce `RuntimeConditionResolutionResult`s, unrelated to week allocation)
- `plan-catalog/catalog/templates/ten-k-master.v6.json` (master-template phase definitions)
- `plan-catalog/catalog/layouts/run-layout-4d.v2.json` (run-layout structure)
- `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v5.json` (finer-grained workout-selection stages, nested within phases)
- `plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`
- `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogBundleLoader.cs`, `PlanCatalogCandidateSummary.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PlanCatalogBundleLoaderTests.cs` (existing `LoadCandidateAsync_ExposesPhaseKeysAndSlotRoles` test — direct, passing, cross-confirming evidence)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/StageEligibilityEvaluatorTests.cs` (Phase 4E.1's fallback-stage governance rule)

## 2. Repository evidence for pilot stage allocation

`templates/ten-k-master.v6.json`'s `phases` array (verbatim):

| phaseKey | minimumWeeks | preferredWeeks | maximumWeeks |
|---|---|---|---|
| `FOUNDATION` | 2 | **3** | 4 |
| `BUILD` | 3 | **4** | 5 |
| `RACE_SPECIFIC` | 2 | **4** | 4 |
| `TAPER` | 1 | **1** | 1 |

Sum of `preferredWeeks` = 3+4+4+1 = **12**, exactly matching the same document's `coreCycle.defaultWeeks = 12`. This is direct, unambiguous, machine-checkable repository evidence for the accepted pilot allocation stated in the task (`Foundation: 3, Build: 4, Race-Specific: 4, Taper: 1`) — **confirmed true**, not assumed.

**Terminology reconciliation (a real finding, reported rather than silently resolved)**: the catalog's own vocabulary distinguishes two nested concepts that the task's prompt refers to jointly as "stage":
- **`phaseKey`** (`FOUNDATION`/`BUILD`/`RACE_SPECIFIC`/`TAPER`) — the week-allocation unit, defined directly in `ten-k-master.v6.json`'s `phases[]`, each carrying its own `preferredWeeks`.
- **`stageKey`** (e.g. `FOUNDATION_EASY_BASE`, `FARTLEK_INTRO`, `THRESHOLD_INTRO`, `TEN_K_SPECIFIC_INTRO`, `GOAL_PACE_REHEARSAL`, `CURRENT_FITNESS_SPECIFIC_REHEARSAL`, `TAPER_SHARPEN`) — a finer-grained, *nested-within-a-phase* workout-selection-progression concept from `workout-progressions/ten-k-workout-progression.v5.json`. Each has `minimumExposures`/`maximumExposures` (a session-slot-exposure count across a phase's weeks), **never its own week-count allocation**.

For **week-allocation purposes** (this phase's entire scope), "stage" = catalog `phaseKey`. This matches the granularity Phase 4F.1 itself already established on `GeneratedCatalogWeekPayload.StageKey`/`GeneratedCatalogWeekProvenance.StageKey` (e.g. its own test fixture value `"BUILD"`). The finer `stageKey` vocabulary (including the one documented fallback, `GOAL_PACE_REHEARSAL → CURRENT_FITNESS_SPECIFIC_REHEARSAL`, gated by `GOAL_FEASIBILITY_IN`) governs *workout selection within* a phase-week and is explicitly out of scope for Phase 4F.2 (no workout prescription/selection occurs here — Decision 8). No phase-level fallback mechanism exists anywhere in the catalog.

**Conclusion**: repository evidence is unambiguous and sufficient. Stage A does not report `PHASE4F_2_BLOCKED_BY_UNRESOLVED_STAGE_ALLOCATION`.

## 3. Repository evidence for pilot run-layout structure

`layouts/run-layout-4d.v2.json` (verbatim): `runsPerWeek: 4`, `slots: [{"role":"KEY_SESSION"},{"role":"EASY_SUPPORT"},{"role":"EASY_SUPPORT"},{"role":"LONG_RUN"}]`. Confirmed identically by the existing, unmodified, passing test `PlanCatalogBundleLoaderTests.LoadCandidateAsync_ExposesPhaseKeysAndSlotRoles` (asserts `summary.SlotRoles` equals exactly `["KEY_SESSION","EASY_SUPPORT","EASY_SUPPORT","LONG_RUN"]` against the real v10 candidate). Matches the task's expected "1 KEY_SESSION, 2 EASY_SUPPORT, 1 LONG_RUN" exactly, in that exact order.

## 4. Files changed

**New** (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/`):
- `GeneratedCatalogPlanSkeleton.cs` — the skeleton contract (plan/week/slot + provenance types)
- `CatalogStageToWeekMaterializer.cs` — `ICatalogStageToWeekMaterializer`/`CatalogStageToWeekMaterializer`, `CatalogStageToWeekMaterializationContext`, `CatalogStageWeekAllocation`, `GeneratedCatalogWeekSkeletonResult`, `CatalogStageToWeekMaterializerVersion`
- `CatalogStageToWeekMaterializationExceptions.cs` — 6 typed exceptions
- `GeneratedCatalogPlanSkeletonValidator.cs` — independent skeleton validator + error enum/result type

**New tests** (`backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/`):
- `CatalogStageToWeekMaterializerFixtures.cs` (test-only)
- `CatalogStageToWeekMaterializerTests.cs` — 37 tests
- `GeneratedCatalogPlanSkeletonValidatorTests.cs` — 8 tests
- `Phase4F2LiveBoundaryRegressionTests.cs` — 4 tests

**Modified**: none. No existing production file (`CatalogPreviewGenerator.cs`, `CatalogPlanConfirmationService.cs`, `Program.cs`, routing, resolvers, `PlanServices.cs`) was touched. No migration, no catalog content, no DTO was touched.

## 5. Materializer interface and implementation

```csharp
public interface ICatalogStageToWeekMaterializer
{
    GeneratedCatalogWeekSkeletonResult Materialize(CatalogStageToWeekMaterializationContext context);
}
```

`CatalogStageToWeekMaterializer` has **zero constructor parameters** (structurally proven by `CatalogStageToWeekMaterializer_HasNoConstructorDependencies`) — no database, clock, HTTP/request, route-decider, resolver, or catalog-loader dependency is even injectable. `Materialize` is a pure function: same input → structurally equivalent output (`Materialize_SameInput_ProducesStructurallyEquivalentOutput`), never mutates its input collections (`Materialize_DoesNotMutate_TheSuppliedContextCollections`).

## 6. Context contract

`CatalogStageToWeekMaterializationContext`: `StartDate`, `AsOfDate`, `PlannedWeekCount`, `DaysPerWeek`, `CanonicalDistanceFamily`, `CandidateKey`, `CandidateVersion`, `DependencyVersions`, `SelectedStageSequence` (ordered, authoritative), `StageWeekAllocations` (ordered, authoritative), `RunLayout` (identity/version), `RunLayoutSlotRoles` (ordered role strings — supplied directly since the materializer has no catalog-loader access). Every field is `required` and immutable (`init`-only). The materializer never reselects a candidate, stage, fallback, or eligibility decision — every input must already be authoritative.

## 7. Skeleton contract

```
GeneratedCatalogPlanSkeleton      — SchemaVersion, StartDate, EndDate, PlannedWeekCount, DaysPerWeek,
                                     CanonicalDistanceFamily, CandidateKey, CandidateVersion,
                                     DependencyVersions, Weeks, Provenance
GeneratedCatalogWeekSkeleton      — WeekNumber, StartDate, EndDate, StageKey, StageWeekIndex,
                                     StageWeekCount, SessionSlots, Provenance
GeneratedCatalogSessionSlotSkeleton — SlotOrderInWeek, LayoutSlotKey, StructuralRole, Provenance
```

Deliberately **not** the final Phase 4F.1 `GeneratedCatalogPlanPayload` — no `PlannedVolumeKm` field exists anywhere (`GeneratedCatalogWeekSkeleton_HasNoPlannedVolumeField`), no prescription/distance/duration/pace/intensity/segment field exists on the slot type (`GeneratedCatalogSessionSlotSkeleton_HasNoPrescriptionFields`), no date/weekday field exists on the slot type (`GeneratedCatalogSessionSlotSkeleton_HasNoWeekdayOrDateField`).

## 8. Skeleton schema/version policy

`GeneratedCatalogPlanSkeleton.CurrentSchemaVersion = 1`, **deliberately independent** of `GeneratedCatalogPlanPayload.CurrentSchemaVersion` (Decision 10). Rationale (documented directly on the type): the skeleton is an internal-only, never-persisted, never-hashed intermediate artifact whose own shape may evolve independently of, and faster than, the final persistable contract's shape. Coupling the two would force every skeleton-shape change to be treated as a final-contract compatibility event, which is not true. `CatalogStageToWeekMaterializerVersion.V1 = "CATALOG_STAGE_TO_WEEK_MATERIALIZER_V1"` is separate, internal-only provenance identifying the implementation that produced a given skeleton.

## 9. Week date algorithm

Identical formula to Phase 4F.1 Decision 6, reapplied at construction time instead of validation time: `WeekStart(N) = StartDate + (N-1)*7 days`, `WeekEnd(N) = WeekStart(N) + 6 days`, `PlanEndDate = StartDate + (PlannedWeekCount*7 - 1) days`. No calendar-week normalization, no Monday alignment, no partial first/last week — proven by 9 dedicated tests including a deliberately non-Monday `StartDate` fixture (a Wednesday).

## 10. Stage allocation algorithm

`StageWeekAllocations`' stage-key sequence must be `SequenceEqual` to the authoritative `SelectedStageSequence` (in order) — this single check simultaneously catches out-of-order allocation, an unknown stage key, and "a fallback stage not present in the selected sequence" (since any allocation entry not already present in the authoritative sequence fails the equality check). Each allocation's `WeekCount` must be positive (rejects zero/negative). The sum of all `WeekCount`s must exactly equal `PlannedWeekCount` (rejects both fewer and excess allocated weeks) — **never rebalanced, rounded, or redistributed**; a mismatch always throws `CatalogStageWeekCountMismatchException`. 13 dedicated tests, including two proving the fallback-acceptance boundary precisely: a resolved fallback is accepted when already present in the authoritative sequence, and rejected (`CatalogStageAllocationInvalidException`) when the materializer would have to select it itself.

## 11. Stage-relative week indexing

Every week carries `StageWeekIndex` (1-based within its stage) and `StageWeekCount` (total weeks allocated to that stage occurrence) — computed by a simple running counter as `BuildWeeks` walks each allocation block sequentially. `Materialize_StageWeekIndex_BeginsAtOneAndIncrementsWithinStage` and `Materialize_StageWeekCount_IsCorrectForEveryWeek` prove this directly against the pilot's own expected sequence (e.g. "Build week 1 of 4" ... "Build week 4 of 4").

## 12. Session-slot construction

For each week, one slot is built per entry in `RunLayoutSlotRoles`, in order: `SlotOrderInWeek = index+1`, `StructuralRole = role` (preserved verbatim from the catalog), `LayoutSlotKey = "{role}_{occurrenceIndexWithinRole}"` (a Phase 4F.2 naming convention — documented explicitly as synthesized, not read from any catalog file, since `run-layout-4d.v2.json` declares only a `role` per slot position, with two slots sharing role `EASY_SUPPORT`). For the pilot: `KEY_SESSION_1`, `EASY_SUPPORT_1`, `EASY_SUPPORT_2`, `LONG_RUN_1` — proven exactly by `Materialize_LayoutSlotKeys_MatchRepositoryRunLayoutDefinition`.

## 13. Rest-day policy

`RunLayoutSlotRoles.Count` must exactly equal `DaysPerWeek` (`CatalogRunLayoutInvalidException` otherwise) — for the 4-day pilot layout, exactly 4 slots are produced, never more, never a rest placeholder. `Materialize_NoRestSlot_IsGenerated` and `Materialize_NoOptionalOrRecoveryJogSlot_IsGenerated` prove only the three accepted structural roles (`KEY_SESSION`, `EASY_SUPPORT`, `LONG_RUN`) ever appear. No REQUIRED/OPTIONAL classification exists anywhere in the type (mirrors Phase 4F.1's own equivalent structural proof).

## 14. Preferred-day deferral

`StartDate`/day-of-week/preferred-running-day/long-run-day-preference/race-day alignment/user availability are never consumed anywhere in the materializer — the context type carries no such field at all, and `GeneratedCatalogSessionSlotSkeleton` carries no date field (§7). Slots are ordered positions, never dated sessions.

## 15. Volume deferral

No `PlannedVolumeKm`, starting/peak/cutback/taper-volume, or long-run-distance field exists anywhere in the skeleton contract (§7) — not a zero-valued placeholder, the field is simply absent, per Decision 7's explicit "do not use zero as a fake resolved volume."

## 16. Workout-prescription deferral

No distance/duration basis, target, estimate, pace, intensity, segment, recovery duration, repetition count, or workout description field exists on `GeneratedCatalogSessionSlotSkeleton` (§7, structurally proven).

## 17. Provenance model

Plan-level (`GeneratedCatalogPlanSkeletonProvenance`): `CandidateKey`, `CandidateVersion`, `DependencyVersions`, `AsOfDate`, `MaterializerVersion`. Week-level (`GeneratedCatalogWeekSkeletonProvenance`): `StageKey`, `SourcePhaseKey` (equal to `StageKey` as of this phase — documented as forward-compatible with a future phase where they might diverge). Slot-level (`GeneratedCatalogSessionSlotSkeletonProvenance`): `SourceStageKey`, `SourceLayout` (full key+version identity). All three are confirmed present by dedicated tests (`Materialize_PlanProvenance_IsPresent`, `..._WeekProvenance_...`, `..._SlotProvenance_...`, `..._MaterializerVersion_IsRecorded`). Confirmed absent from every public DTO type by reflection (`GeneratedCatalogPlanSkeleton_ProvenanceTypes_AreAbsentFromPublicDtos`, scanning the entire `RunningApp.Application.DTOs` namespace for any property whose type lives in the `Materialization` namespace — zero matches).

## 18. Validation rules

`GeneratedCatalogPlanSkeletonValidator` (independent of the materializer's own inline construction-time checks — validates the *output* regardless of provenance): schema version, positive/exact week count, consecutive week numbering, plan-relative week dates, `EndDate` consistency, complete stage allocation (no gaps/duplicates in `StageWeekIndex` within any `StageKey`/`StageWeekCount` group), exact slot count per week, unique consecutive slot order, no `REST`-labeled structural role, provenance presence at all three levels. 8-value `GeneratedCatalogPlanSkeletonValidationError` enum; 8 dedicated tests plus the baseline "materialized pilot skeleton is valid" test.

## 19. Error taxonomy

`CatalogStageAllocationInvalidException` (`CATALOG_STAGE_ALLOCATION_INVALID`), `CatalogStageSequenceInvalidException` (`CATALOG_STAGE_SEQUENCE_INVALID`), `CatalogStageWeekCountMismatchException` (`CATALOG_STAGE_WEEK_COUNT_MISMATCH`), `CatalogRunLayoutInvalidException` (`CATALOG_RUN_LAYOUT_INVALID`), `CatalogSessionSlotCountMismatchException` (`CATALOG_SESSION_SLOT_COUNT_MISMATCH`), `CatalogStageToWeekMaterializationFailedException` (`CATALOG_STAGE_TO_WEEK_MATERIALIZATION_FAILED`, generic catch-all, unused as of this phase since every anticipated failure has a specific type). All six remain **plain internal/application exceptions, not registered in `GlobalExceptionHandler`** — no public endpoint invokes the materializer yet, so mapping them publicly now would be premature per the task's own instruction.

## 20. Determinism and dependency isolation

Proven by: zero-parameter constructor (§5); same-input → structurally-equivalent-output test; no-mutation-of-input test; and the complete absence of any `using` of `Microsoft.EntityFrameworkCore`, `System.Net.Http`, any resolver namespace, `IGenerationRouteDecider`, `ICatalogCandidateEligibilityGate`, or `IPlanCatalogBundleLoader` anywhere in the four new production files (confirmed by direct reading — each file's own `using` list contains only `System`/`System.Linq` and sibling `RuntimeCatalog` namespace types).

## 21. Live preview boundary

`CatalogPreviewGenerator.cs` was **not modified** (confirmed: zero diff against the Phase 4F.1 checkpoint for this file). Its constructor still takes only `(ICatalogCandidateEligibilityGate, RuntimeConditionResolutionService)` — structurally reconfirmed by `CatalogPreviewGenerator_ConstructorDependencies_DoNotIncludeAnyMaterializationType`. `GenerateAsync` still never supplies a `generatedPreviewPlanPayload` argument. Real preview generation for the pilot combination still fails at the `PUBLISHED`-only eligibility gate (`RealCatalogPreviewGeneration_StillProducesNullGeneratedPreviewPlanPayload_AndRejectsAsUnpublished`, re-run and passing against the real, unmodified, still-`DRAFT` `v10` candidate).

## 22. Confirm boundary

`CatalogPlanConfirmationService.cs` was **not modified**. Its constructor still takes only `(AppDbContext, ILogger, IGeneratedCatalogPlanPayloadValidator)` — structurally reconfirmed by `CatalogPlanConfirmationService_ConstructorDependencies_DoNotIncludeAnyMaterializationType`. All 25 existing `CatalogPlanConfirmationServiceTests` (including `ConfirmAsync_ValidCatalogPreview_ThrowsCatalogPreviewNotPersistableException` and `ConfirmAsync_StructurallyValidSchedule_StillThrowsCatalogPreviewMaterializationNotImplementedException_NoMutation`) still pass unchanged. No `TrainingPlan`/`TrainingWeek`/`TrainingDay`/`PlanEvent` is created by any path exercised this phase. No SQL fallback exists (unchanged code). Resolver orchestration is not rerun at confirm (unchanged code, reconfirmed by the still-passing `CatalogPlanConfirmationService_HasNoGenerationOrResolutionDependencies`). `AsOfDate` is not recomputed (unchanged code).

## 23. Public DTO boundary

Confirmed by `GeneratedCatalogPlanSkeleton_ProvenanceTypes_AreAbsentFromPublicDtos` (§17): zero properties anywhere in `RunningApp.Application.DTOs.*` reference any type in the `RunningApp.Application.RuntimeCatalog.Schedule.Materialization` namespace.

## 24. Test results

`dotnet build RunningApp.sln -c Release` → 0 errors, 0 warnings.

Focused Phase 4F.2 tests (`--filter "FullyQualifiedName~Materialization"`): **50/50 passed** (49 new tests + 1 pre-existing Phase 4F.1 test whose method name happens to contain "Materialization" — see §25).

`RuntimeCatalog` focused suite: **445/445 passed** (396 prior + 49 new).

Full suite (`dotnet test RunningApp.sln -c Release --no-build`): **488 passed, 0 failed, 0 skipped, 488 total.**

## 25. Exact test-count reconciliation

| Count | Source |
|---|---|
| 439 | Full-suite baseline at the start of this phase (confirmed via a fresh build+test run before any edit) |
| 488 | Full suite after this phase |

`488 - 439 = 49` — exactly matching the 49 new `[Fact]` methods across the four new test files (37 + 8 + 4 = 49, directly counted via `grep -c "\[Fact\]"` per file). The `RuntimeCatalog`-filtered count (445) and the `Materialization`-filtered count (50) both reconcile exactly once the one pre-existing, coincidentally-named Phase 4F.1 test (`ConfirmAsync_StructurallyValidSchedule_StillThrowsCatalogPreviewMaterializationNotImplementedException_NoMutation`, already counted in the 439 baseline) is accounted for. No test was deleted, renamed, or silently altered.

## 26. Remaining work (deferred to future phases, not started here)

1. Wiring the materializer into an actual `SelectedStageSequence`/`StageWeekAllocations` resolution step (reading `preferredWeeks` from the catalog — the existing `PlanCatalogBundleLoader` does not currently surface it, only bare `phaseKey` strings).
2. Transforming a `GeneratedCatalogPlanSkeleton` into the final Phase 4F.1 `GeneratedCatalogPlanPayload` (workout selection, prescriptions, pace, segments).
3. Preferred-day/calendar assignment (dating the ordered slots).
4. Planned-volume calculation.
5. Actually wiring any of the above into `CatalogPreviewGenerator`/`CatalogPlanConfirmationService` — explicitly not started in this phase.
6. Database-level concurrent-confirmation safety — unrelated, still open from Phase 4E.2.

## 27. Final classification

```text
BACKEND_HAS_DETERMINISTIC_CATALOG_STAGE_TO_WEEK_SKELETON_NOT_YET_DATED_OR_PRESCRIBED
```
