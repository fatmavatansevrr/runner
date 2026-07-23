# Phase 4F.5.1 — Production Validator Wiring and Dead-Exception Cleanup

## 1. Independent audit finding

An independent verification audit of Phase 4F.5 confirmed the calendar-assignment algorithm itself was correct and deterministic, but found one real gap: `DatedGeneratedCatalogPlanSkeletonValidator` existed and was well tested, but was never invoked anywhere in the production dark path. As a consequence, `CatalogDatedSkeletonInvalidException` was never thrown by reachable production code, `CatalogCalendarAssignmentFailedException` also appeared to be dead code, and the documented "internal validation" step was not actually performed during an eligible preview.

## 2. Actual previous production flow

```text
Eligible catalog preview
→ structural skeleton materialization (Phase 4F.3, validated internally)
→ dated skeleton materialization (Phase 4F.5)
→ result discarded
```

No dated-skeleton validation occurred between materialization and discard.

## 3. Corrected production flow

```text
Eligible catalog preview
→ structural skeleton materialization (Phase 4F.3, validated internally)
→ dated skeleton materialization (Phase 4F.5)
→ dated skeleton validation (Phase 4F.5.1)
→ dark result discarded
→ public preview behavior unchanged
```

## 4. Where validator invocation now occurs

`CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` (the same private method that already owned the Phase 4F.3 skeleton call and the Phase 4F.5 calendar-materializer call). Immediately after `_calendarMaterializer.Materialize(calendarContext)` returns, the method calls `_datedSkeletonValidator.Validate(datedSkeleton, preferredDays, longRunDay)`. This mirrors the established repository precedent in `CatalogPlanSkeletonOrchestrator.Build`, which already calls its own `IGeneratedCatalogPlanSkeletonValidator.Validate(skeleton)` as the final step before returning — orchestration-level "materialize, then validate" is the pattern this codebase already uses, and `CatalogPreviewGenerator` is the correct place to apply it for the dated skeleton too, since it is already the sole owner of the Phase 4F.3→4F.5 dark sequence. The materializer itself was left as a pure transform — validator logic was not duplicated inside it, and was not moved into it.

## 5. Why this placement is correct

- It is the exact point the independent audit identified as missing.
- It matches the existing, established orchestration-level validation precedent (Phase 4F.3's skeleton orchestrator).
- It requires no new class and no new composition boundary — the same private method, same try/catch shape, same wrapping convention already used for the Phase 4F.3/4F.5 exceptions.
- It preserves "materializer stays pure" (no validation logic added to `CatalogWeekSkeletonCalendarMaterializer`).

## 6. Files inspected

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogWeekSkeletonCalendarMaterializer.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/DatedGeneratedCatalogPlanSkeletonValidator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogCalendarAssignmentContracts.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogCalendarAssignmentExceptions.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrator.cs` (precedent for orchestration-level "materialize then validate")
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogWeekSkeletonCalendarMaterializerTests.cs`, `DatedGeneratedCatalogPlanSkeletonValidatorTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F5DarkCalendarWiringTests.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`, `backend/RunningApp.Application/Services/PlanServices.cs` (confirmed no reference to any Materialization-namespace type)
- `PHASE4F_5_CALENDAR_DAY_ASSIGNMENT_POLICY_AND_MATERIALIZER.md`

## 7. Files changed

| File | Classification |
|---|---|
| `CatalogPreviewGenerator.cs` | Production code (modified — validator field, 5th constructor overload, validator invocation, updated catch filter and doc comments) |
| `CatalogCalendarAssignmentExceptions.cs` | Production code (modified — removed `CatalogCalendarAssignmentFailedException`, updated header comment) |
| `Phase4F5_1ProductionValidatorWiringTests.cs` | Test code (new) |
| `PHASE4F_5_1_PRODUCTION_VALIDATOR_WIRING.md` | Documentation (new) |

No other file was touched. `CatalogWeekSkeletonCalendarMaterializer.cs` and `DatedGeneratedCatalogPlanSkeletonValidator.cs` were inspected but not modified — the materializer remains a pure transform, and the validator's own logic was already correct and untouched.

## 8. Validator wiring details

- **Composition method:** `DefaultDatedSkeletonValidator()`, a `private static` factory returning `new DatedGeneratedCatalogPlanSkeletonValidator()` — mirrors `DefaultSkeletonOrchestrator()`/`DefaultCalendarMaterializer()` exactly.
- **Constructor/test-seam changes:** the public 2-parameter constructor is unchanged; it now delegates through an additional internal 5-parameter constructor (`gate, orchestration, skeletonOrchestrator, calendarMaterializer, datedSkeletonValidator`). The pre-existing internal 3- and 4-parameter constructors are unchanged in their own parameter lists and now delegate to the 5-parameter constructor supplying `DefaultDatedSkeletonValidator()` — so every existing test call site (Phase 4F.4/4F.5 tests using the 2/3/4-arg constructors) continues to compile and run unchanged.
- **Production DI registration:** no change to `Program.cs` — confirmed via `git diff` (empty). The validator, like the orchestrator and calendar materializer before it, is `internal` and composed inside `CatalogPreviewGenerator`, not registered in the container.
- **Invocation order:** materializer → validator → discard, confirmed by a recording-sequence test.
- **Context passed:** the exact `DatedGeneratedCatalogPlanSkeleton` instance returned by the materializer (not a copy), plus the same `preferredDays`/`longRunDay` already parsed for the materializer's own context — no re-parsing, no re-derivation.
- **Success behavior:** `validation.IsValid == true` → the dated skeleton and its provenance are discarded; execution proceeds unchanged.
- **Failure behavior:** `validation.IsValid == false` → throws `CatalogDatedSkeletonInvalidException` with a message listing every validation error, caught by the same `catch (Exception ex) when (...)` filter and wrapped as `PlanPreviewGenerationFailedException(message, ex)`, preserving the typed cause as `InnerException`.

## 9. Exception taxonomy outcome

- **`CatalogDatedSkeletonInvalidException`** is now production-reachable: thrown directly by `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` when `_datedSkeletonValidator.Validate(...)` returns an invalid result. Proven reachable by a real (non-double) production-composed validator rejecting a malformed dated skeleton produced by a test-only malformed-materializer double (`MalformedMaterializerOutput_DuplicateSessionDate_RejectedByRealValidator`).
- **`CatalogCalendarAssignmentFailedException` — removed (Option C).** It had no distinct trigger condition not already covered by one of the other 7 specific exceptions, no production code ever threw it, and no test referenced it. Per the task's own guidance ("prefer removing unjustified dead code over inventing an artificial throw path"), it was deleted from `CatalogCalendarAssignmentExceptions.cs` and from `CatalogPreviewGenerator`'s catch filter. The typed-error taxonomy for calendar assignment is now 8 types, all reachable.

## 10. Public and persistence boundary verification

Confirmed via `git diff --name-status` (empty for each): `CatalogPreviewSnapshot.cs`, all `DTOs/Plan/*.cs`, `CatalogPlanConfirmationService.cs`, `Program.cs`, all `.csproj` files, Domain/Persistence directories. `grep` for `Materialization`/`DatedGeneratedCatalogPlanSkeleton` in `CatalogPlanConfirmationService.cs` and `PlanServices.cs` found zero references (the one incidental match, `CatalogPreviewMaterializationNotImplementedException`, is a pre-existing, unrelated Phase 4E.2/4F.1 exception name, not in the `Materialization` namespace). `GeneratedPreviewPlanPayload` confirmed null after successful validation (`PublicBoundary_UnchangedAfterSuccessfulValidation`). Snapshot property set, hash computation, and DTO shapes are unmodified — no new field was added anywhere.

## 11. Tests added (10, in `Phase4F5_1ProductionValidatorWiringTests.cs`)

1. `Validator_InvokedOnce_ReceivesExactMaterializerOutputAndContext_PublicPayloadUnchanged` — validator called exactly once, receives the exact materializer output instance and correct PreferredDays/LongRunDay, payload stays null.
2. `ExecutionOrder_MaterializerRunsBeforeValidator` — recording-sequence proof of order.
3. `ValidatorRejection_WrappedInPlanPreviewGenerationFailedException_PreservingExactInstance` — throwing validator double, exact inner-exception identity preserved.
4. `MalformedMaterializerOutput_DuplicateSessionDate_RejectedByRealValidator` — real production-composed validator rejects a genuinely malformed dated skeleton (duplicate session date within a week).
5. `Validator_NotInvoked_WhenMaterializationThrows` — materializer throws a typed calendar exception; validator invocation count stays 0.
6. `Validator_NotInvoked_ForDraftV10Candidate` — real DRAFT gate, real catalog data; validator invocation count stays 0.
7. `Validator_NotInvoked_WhenGovernanceRejectsRequest` — missing RaceDate triggers governance failure before the dark path; validator invocation count stays 0.
8. `Validator_NotInvoked_WhenStructuralSkeletonCreationFails` — Phase 4F.3 skeleton orchestrator throws; validator invocation count stays 0.
9. `PublicBoundary_UnchangedAfterSuccessfulValidation` — snapshot shape/hash/payload unchanged after a successful validated dark pass.
10. `ConfirmService_HasNoReferenceToDatedSkeletonOrValidatorTypes` — reflection proof confirm has zero references to any Materialization-namespace type.

## 12. Build and test results

```
dotnet build RunningApp.sln -c Release
  → 0 errors, 0 warnings

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~Phase4F5_1ProductionValidatorWiringTests"
  → 10 passed, 0 failed, 0 skipped, 10 total

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~CatalogWeekSkeletonCalendarMaterializerTests"
  → 39 passed, 0 failed, 0 skipped, 39 total (unchanged)

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~DatedGeneratedCatalogPlanSkeletonValidatorTests"
  → 8 passed, 0 failed, 0 skipped, 8 total (unchanged, no assertion weakened)

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~Phase4F5DarkCalendarWiringTests"
  → 6 passed, 0 failed, 0 skipped, 6 total (unchanged)

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~Phase4F4DarkSkeletonWiringTests"
  → 19 passed, 0 failed, 0 skipped, 19 total (unchanged)

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~CatalogPlanConfirmationServiceTests"
  → 25 passed, 0 failed, 0 skipped, 25 total (unchanged)

dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~RuntimeCatalog"
  → 585 passed, 0 failed, 0 skipped, 585 total

dotnet test RunningApp.sln -c Release --no-build
  → 628 passed, 0 failed, 0 skipped, 628 total
```

## 13. Exact test-count reconciliation

```
618 previous full-suite total (Phase 4F.5 baseline)
+ 10 new tests (Phase 4F.5.1)
= 628 final total — matches the observed full-suite result exactly.
```

RuntimeCatalog: 575 (Phase 4F.5 baseline) + 10 = 585 — matches exactly.

## 14. Final classification

**`BACKEND_DARK_MATERIALIZES_AND_VALIDATES_BINDING_CALENDAR_DATES_DURING_ELIGIBLE_PREVIEW_WITHOUT_PUBLIC_SCHEDULE_OUTPUT`**

Validation is now genuinely executed in the reachable dark production path — proven both by a recording test seam (order + invocation count) and by a real production-composed validator actually rejecting a malformed dated skeleton end-to-end through `CatalogPreviewGenerator`.
