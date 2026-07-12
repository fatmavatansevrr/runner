# Phase 4E.2 — Safety Audit (Stage A: Audit Only)

**No runtime source, tests, migrations, or documentation were modified during this audit**, except this report. Stage A ran read-only inspection, a `dotnet build`/`dotnet test` execution (which does not mutate source), and read-only git/history inspection. No commit, push, reset, clean, stash, checkout, revert, or migration regeneration was performed.

## 0. Repository state before audit

Working directory: `C:\Users\vatan\Desktop\runner`, branch `main`. Latest actual git commit: `fe85044` ("plan-catalog-added"). **`git ls-tree -r HEAD --name-only | grep RuntimeCatalog` returns zero matches** — none of the backend catalog-integration work (Phases 1 through 4E.1.1, and now 4E.2) has ever been committed to git. Everything described below exists only in the uncommitted working tree, consistent with every prior phase in this multi-session effort.

## 1. Full modified/untracked file inventory and provenance classification

### 1a. Modified (tracked) files — `git diff --name-status` (excluding `bin/`/`obj/` build artifacts)

| File | Classification |
|---|---|
| `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs` | Phase 4E.2-related (11 new exception mappings added on top of the existing 7 Phase 4E.1 ones) |
| `backend/RunningApp.Api/Program.cs` | Phase 4E.2-related (`ICatalogPlanConfirmationService` DI registration added) |
| `backend/RunningApp.Application/DTOs/Plan/GeneratePreviewRequest.cs`, `GeneratePreviewResponse.cs` | Unrelated pre-existing change (Phase 4B/4E.1 fields; not touched further by 4E.2 per inspection) |
| `backend/RunningApp.Application/Exceptions/AppExceptions.cs` | Phase 4E.2-related (11 new exception types appended after the existing Phase 4E.1 block) |
| `backend/RunningApp.Application/PlanGeneration/IPlanGenerationEngine.cs`, `PlaceholderPlanGenerationEngine.cs` | Unrelated pre-existing change (Phase 0) |
| `backend/RunningApp.Application/RunningApp.Application.csproj` | Unrelated pre-existing change (Phase 4E.1.1's `InternalsVisibleTo`) |
| `backend/RunningApp.Application/Services/PlanServices.cs` | Phase 4E.2-related (constructor gains `ICatalogPlanConfirmationService`; `ConfirmPlanAsync` gains catalog-dispatch branch; `IsCatalogSourcedPreview` helper added) |
| `backend/RunningApp.Domain/Entities/PlanPreview.cs` | Phase 4E.2-related (`ConfirmedPlanId`, `IsInvalidated` added) |
| `backend/RunningApp.Domain/Entities/TrainingDay.cs`, `TrainingWeek.cs` | Unrelated pre-existing change (not touched by 4E.2 per inspection — no diff content reviewed beyond confirming no Phase 4E.2 symbols appear) |
| `backend/RunningApp.Domain/Entities/TrainingPlan.cs` | Unrelated pre-existing change (Phase 3 catalog-provenance fields; consumed but not modified by 4E.2's `BuildPlan`) |
| `backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj` | Test-only change (project reference/package bookkeeping, not inspected line-by-line — low risk, no source behavior) |
| `backend/RunningApp.IntegrationTests/UserJourneyTests.cs` | **Mixed provenance.** The `[Collection(ApiIntegrationTestCollection.Name)]` change is Phase 4E.2-adjacent test-infrastructure (see §1c). The `GeneratePreview_UnsupportedGoalCombo_*` rename/rewrite diff I inspected is **unrelated pre-existing Phase 0 content**, not new in this pass. |
| `backend/RunningApp.Persistence/AppDbContext.cs` | Phase 4E.2-related (`ConfirmedPlanId` FK/index configuration added) |
| `backend/RunningApp.Persistence/Migrations/AppDbContextModelSnapshot.cs` | Migration artifact (auto-generated, reflects the new migration) |
| `API_DOCUMENTATION.md`, `MVP_LIMITATIONS.md` | Unrelated pre-existing change (Phase 0 documentation, inspected and confirmed to predate 4E.2) |
| `plan-catalog/artifacts/audits/{active-v4-domain-blocker-inventory,domain-blocker-resolution-plan,domain-blocker-version-cascade-forecast,golden-fixture-v3-integrity,ten-k-pilot-domain-decision-audit}.{json,md}`, `plan-catalog/schemas/*.json`, `plan-catalog/src/**/*.cs`, `plan-catalog/tests/**/*.cs` | Unrelated pre-existing changes (Process A / plan-catalog authoring-side work from far earlier phases; **not inspected line-by-line in this pass** — outside Phase 4E.2's stated scope, and the task's file list does not name any of them; provenance unclear beyond "pre-existing," not touched here) |

### 1b. Untracked files directly relevant to Phase 4E.2

| File | Classification |
|---|---|
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs` | Phase 4E.2-related (new: `ICatalogPlanConfirmationService`/`CatalogPlanConfirmationService`) |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshotVerifier.cs` | Phase 4E.2-related (new: hash re-verification at confirm time) |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/RuntimeConditionResolutionResultConverter.cs` | Phase 4E.2-related (new: custom `JsonConverter` for the private-constructor result type) |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewSnapshot.cs` | Modified since Phase 4E.1 (untracked, so no `git diff` exists): `CatalogPreviewSnapshotBuilder.Build` gained an optional `generatedPreviewPlanPayload` parameter (default `null`). Verified: `CatalogPreviewGenerator.cs` never passes a non-null value — every real-generated snapshot still has `GeneratedPreviewPlanPayload == null`. |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/{GenerationRouteDecision,CatalogCandidateEligibilityGate,NotEvaluatedReasonClassifier,StageEligibilityEvaluator,CatalogPreviewGenerator}.cs` | Verified byte-for-byte unchanged from Phase 4E.1 (read in full; matches Phase 4E.1's own documented content exactly) |
| `backend/RunningApp.Persistence/Migrations/20260712115640_Phase4E2_CatalogConfirmationState.cs` (+`.Designer.cs`) | Migration artifact — Phase 4E.2-related, single migration, internally consistent `Up`/`Down` |
| `backend/RunningApp.Persistence/Migrations/20260710072851_AddPlanCatalogProvenanceFields.cs` (+`.Designer.cs`) | Migration artifact — **pre-existing** (Phase 3 catalog-provenance fields on `TrainingPlan`, dated before this pass; not Phase 4E.2 work) |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationServiceTests.cs` | Phase 4E.2-related — 21 new `[Fact]` tests |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/TestPlanServicesFactory.cs` | Modified since Phase 4E.1.1 (untracked): now also constructs `CatalogPlanConfirmationService` and passes it into `PlanServices` |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/PlanServicesCatalogRoutingBoundaryTests.cs` | Modified (per the harness's own note at the top of this conversation turn) — 2 tests total (unchanged from Phase 4E.1's count): the spy-engine test is unchanged; the old Phase 4E.1 confirm-boundary test was replaced by a Phase 4E.2 dispatch test. Net test count unchanged in this file. |
| `backend/RunningApp.IntegrationTests/ApiIntegrationTestCollection.cs` | Test-only change, Phase 4E.2-adjacent but **not a Phase 4E.2 product-logic change** — a test-isolation fix (see §1c) |
| All other untracked `RuntimeCatalog/**`, `RuntimeCatalog/Resolvers/**`, and `IntegrationTests/RuntimeCatalog/Resolvers/**` files | Verified pre-existing (Phases 1–4D.5.1), not touched by this pass — confirmed by reading `CatalogPreviewGenerator.cs`'s unchanged content and by the fact that no resolver file appears in this session's Phase 4E.2 read set |

### 1c. `ApiIntegrationTestCollection.cs` — did Gemini start additional work after the last known checkpoint?

Yes, in a narrow, well-scoped sense. This new file's own doc comment explains it precisely: xUnit runs test classes in parallel by default; multiple HTTP-based test classes race a shared hardcoded mock user (`mock-user-001`) against one real Postgres database via `POST /api/v1/testing/reset`, producing intermittent 500s that are a **test-isolation artifact, not a product defect**. Grouping every such class into one named collection forces them to run sequentially relative to each other. This is a legitimate, narrowly-scoped test-infrastructure fix, unrelated to catalog-confirmation logic, and does not affect any assertion. **Provenance: clear, not suspicious.**

### 1d. Files whose provenance remains unclear

None of the files directly read and evidenced above are unclear. The `plan-catalog/**` modified files listed in §1a's last row were **not individually inspected** in this pass (out of Phase 4E.2's stated scope and not named in the task's required-inspection list) — their provenance is presumed pre-existing based on file path and prior-session context, but this audit does not claim certainty for their exact diff content. No file was altered based on this uncertainty.

## 2. Confirm call graph (A2)

`ConfirmPlanAsync` (`PlanServices.cs:303`) now begins with:
1. Load the preview **without a user filter** (`AsNoTracking`, `.FirstOrDefaultAsync(p => p.Id == request.PreviewId)`).
2. `IsCatalogSourcedPreview(previewForDispatch.PreviewPayloadJson)` — parses the stored JSON with `JsonDocument.Parse` and checks for a top-level `generation_source`/`generationSource` property equal to `"CATALOG"` (case-insensitive). **This reads only the already-persisted value; it does not call `IGenerationRouteDecider`.**
3. If catalog-sourced: delegates unconditionally to `ICatalogPlanConfirmationService.ConfirmAsync(internalUserId, request.PreviewId, ct)` and returns its result. **Nothing below this branch executes.**
4. Otherwise: falls through to the legacy SQL confirm logic, byte-for-byte the same code that existed before Phase 4E.1/4E.2 (verified by direct reading), except one dead defensive guard's exception type changed from `ConflictAppException` (Phase 4E.1) to `InvalidOperationException` (Phase 4E.2) — this guard is unreachable for any real catalog-sourced preview now, since step 2/3 catches those first; it remains reachable only as a "should never happen" safety net.

`CatalogPlanConfirmationService.ConfirmAsync` — verified by direct reading (`CatalogPlanConfirmationService.cs`) to have **no dependency on** `IGenerationRouteDecider`, `ICatalogCandidateEligibilityGate`, `ICatalogPreviewGenerator`, `IRuntimeConditionResolutionService`/`RuntimeConditionResolutionService`, any of the four resolvers, `StageEligibilityEvaluator`, or `IPlanGenerationEngine`. Its constructor takes only `AppDbContext` and `ILogger<CatalogPlanConfirmationService>`. This is independently proven by a reflection-based structural test, `CatalogPlanConfirmationService_HasNoGenerationOrResolutionDependencies`, which asserts none of those type-name substrings appear among the constructor's parameter types.

**Answering each sub-question directly:**
- Dispatch is based **solely** on the stored `GenerationSource` field. ✅ Confirmed.
- Route selection is **not** rerun. ✅ Confirmed (no `IGenerationRouteDecider` call anywhere in the confirm path).
- Candidate eligibility is **not** rerun. ✅ Confirmed (no `ICatalogCandidateEligibilityGate` call).
- No resolver is rerun. ✅ Confirmed (no resolver types referenced).
- `AsOfDate` is **not** recomputed — it is read from `snapshot.AsOfDate` (frozen at preview time) and reused for `TrainingPlan.StartedAt` in the (currently unreachable) `BuildPlan` method; `ConfirmedAtUtc = DateTime.UtcNow` is a separate technical timestamp, never used for any domain decision. ✅ Confirmed.
- `StageEligibilityEvaluator` is **not** invoked. ✅ Confirmed (grep across `CatalogPlanConfirmationService.cs` finds zero references; also explicitly documented in code comments at steps 12–13).
- No generation engine (`IPlanGenerationEngine`/`PlaceholderPlanGenerationEngine`) is invoked. ✅ Confirmed.
- **A failed catalog confirm cannot reach SQL logic.** `ConfirmPlanAsync`'s catalog branch is an unconditional `return await _catalogConfirmationService.ConfirmAsync(...)` with no surrounding `try`/`catch` — any exception from `CatalogPlanConfirmationService` propagates directly out of `ConfirmPlanAsync`, never reaching the legacy SQL code below. ✅ Confirmed by code structure (no catch block exists between the dispatch call and the method's exit).

## 3. Persistability findings (A3)

`CatalogPreviewSnapshotBuilder.Build` (in `CatalogPreviewSnapshot.cs`) accepts an optional `generatedPreviewPlanPayload` parameter defaulting to `null`. **`CatalogPreviewGenerator.GenerateAsync`** — the only production code path that builds a real snapshot — calls `CatalogPreviewSnapshotBuilder.Build(...)` **without** that parameter, so every snapshot ever produced by real preview generation has `GeneratedPreviewPlanPayload == null`. `SelectedStageKeys`/`FallbackStagesUsed` are unconditionally `Array.Empty<string>()`. **Real production snapshots today contain no weeks, no days, and none of the fields Home/Calendar/Active-Plan-Details/Training-Day-Detail require** — this was already true as of Phase 4E.1 and remains true as of Phase 4E.2; nothing in this phase changed that.

`CatalogPlanConfirmationService.ConfirmAsync`'s **step 11** ("persistability guard," lines 256–288) checks exactly `if (snapshot.GeneratedPreviewPlanPayload is null)` and, if true, throws `CatalogPreviewNotPersistableException` **before any `_context.Add(...)` call or `SaveChangesAsync`**. Verified by reading the method top-to-bottom: `_context.TrainingPlans.Add(plan)` first appears at line 299, strictly after the guard's `throw` at line 283–287, which unconditionally exits the method via an exception for every currently-possible real snapshot.

**Proven, not merely asserted:**
- Since `GeneratedPreviewPlanPayload` is always `null` for every real snapshot, **the confirmation service always returns `CATALOG_PREVIEW_NOT_PERSISTABLE` for any real catalog-routed confirm attempt today.** The "successful" code path (steps 12–15, `BuildPlan`/persist) is provably unreachable in production, exactly as the source code's own comments state ("Not reachable in Phase 4E.1/4E.2").
- On rejection: no `TrainingPlan`, `TrainingWeek`, `TrainingDay`, or `PlanEvent` row is created (no `_context.Add`/`SaveChangesAsync` call occurs before the throw); `ConfirmedPlanId` is never set (the assignment `preview.ConfirmedPlanId = plan.Id` is at line 330, unreachable); the active-plan uniqueness index (`IX_TrainingPlans_InternalUserId_ActiveOnly`) is never touched since no row is inserted.
- Directly proven by test: `ConfirmAsync_ValidCatalogPreview_ThrowsCatalogPreviewNotPersistableException` and `ConfirmAsync_NonPersistableSnapshot_ThrowsCatalogPreviewNotPersistableException_AndLeavesDatabaseUnchanged` (the latter explicitly asserts `Assert.Empty` on `TrainingPlans`, `PlanEvents`, `TrainingWeeks`, `TrainingDays`, and `Assert.Null` on the reloaded preview's `ConfirmedPlanId`).

**No path was found that creates an active `TrainingPlan` from a null/incomplete payload.** The only path that returns a plan without throwing is the idempotency short-circuit (step 10), which requires `preview.ConfirmedPlanId` to already be non-null — i.e. it can only return a plan that was already correctly created and linked by some prior, successful confirm; it never creates one itself from an unpersistable snapshot.

## 4. Fake happy-path findings (A4)

Reviewed every test in `CatalogPlanConfirmationServiceTests.cs` (21 tests) plus `PlanServicesCatalogRoutingBoundaryTests.cs`, `TestPlanServicesFactory.cs`.

| Test | Uses mock/anonymous payload? | Claims success without a real schedule? | Classification |
|---|---|---|---|
| `ConfirmAsync_ValidCatalogPreview_ThrowsCatalogPreviewNotPersistableException` | Uses `BuildValidSnapshot()` — a real `CatalogPreviewSnapshot` built via the same `CatalogPreviewSnapshotBuilder.Build` production code uses, with a synthetic (non-real-catalog-file) `PlanCatalogCandidateSummary` fixture | No — asserts it **throws** | Validates real production rejection behavior. Not misleading. |
| `ConfirmAsync_DoesNotRequireRouteDecider_OperatesOnStoredSnapshot`, `ConfirmAsync_NewerCatalogVersionDoesNotAlterStoredSnapshot`, `ConfirmAsync_OlderValidPreview_RemainsConfirmable` | Same fixture | No — all assert `CatalogPreviewNotPersistableException` | Valid; each documents in-comment that it "fails at the step 11 persistability guard," not a false claim of success |
| `ConfirmAsync_SnapshotInvalidCandidateVersion_ThrowsPlanPreviewSnapshotUnsupportedException`, `ConfirmAsync_WrongGenerationSource_ThrowsPlanPreviewGenerationSourceInvalidException` | Hand-built anonymous-object JSON (deliberately incomplete/wrong-shaped) | No — both assert specific rejection exceptions | Legitimate, deliberate **negative**-path fixtures. Not a fake happy path — this is the correct way to test malformed-input rejection. |
| `ConfirmAsync_HashMismatch_ThrowsPlanPreviewIntegrityFailedException` | Real snapshot, then string-replaces the hash in the serialized JSON | No — asserts rejection | Legitimate tamper-simulation test |
| `ConfirmAsync_Idempotent_RepeatedCallReturnsSamePlan`, `ConfirmAsync_Idempotent_DoesNotCreateDuplicatePlans` | Directly seeds a `TrainingPlan` **and** a `PlanPreview` with `ConfirmedPlanId` already set — simulating a plan confirmed by a hypothetical future phase | Claims a "successful" idempotent return, but the plan was never created *by* `ConfirmAsync` in this test — it was pre-seeded | **Validates only future infrastructure** (the idempotency-anchor short-circuit in isolation), clearly scoped as such by its own in-line comment: "Under Option B, we cannot confirm a new plan... However, if the preview already has ConfirmedPlanId set (from a prior successful confirm in a future phase)...". Not misleading about what it proves, but a reader skimming only the test name could wrongly assume confirm can create plans today — **recommend the test name/comment be strengthened in a future pass, not urgent.** |
| `ConfirmAsync_NonPersistableSnapshot_ThrowsCatalogPreviewNotPersistableException_AndLeavesDatabaseUnchanged` | Real snapshot | No — explicit multi-assertion rejection proof | Strong, non-misleading test; this is the single most important test in the file for A3's conclusion |
| `SnapshotVerifier_ValidSnapshot_ReturnsTrue` | Real snapshot | No | Valid |
| **`SnapshotVerifier_WrongHash_ReturnsFalse`** | Real snapshot(s) | **Test name promises `Verify()` returning `false` for a tampered snapshot, but the test body never actually constructs a snapshot with a mismatched `ContentHash` and calls `Verify()` on it.** It only asserts `Verify(differentSnapshot) == true` (a *correctly*-hashed different snapshot) and that two different snapshots produce different hashes. The test's own comment admits this: *"Not possible with init properties, so we verify the builder produces different hashes for different inputs, proving the verifier would catch tampered content"* — this is an inference, not a direct proof. | **Misleading — should not exist under this name/assertion set.** The actual `Verify()==false` behavior for a genuinely mismatched hash **is** proven elsewhere, correctly, by `ConfirmAsync_HashMismatch_ThrowsPlanPreviewIntegrityFailedException` (full round-trip through `ConfirmAsync`). Net risk: **low** (the real behavior is covered by a different, correct test), but this specific test's name/assertions are inaccurate and should be corrected or removed in Stage B. |
| `TdPaceSource001_EstimatedPathStillNeverEmitted_ByConfirmService` | Reflection only | No | Valid, narrow structural check |
| `PlanServices_LegacySqlPreview_UsesExistingSqlConfirmPath` | `PreviewPayloadJson = "{}"` (empty object, no `generation_source`) | No — asserts the **defensive** `InvalidOperationException` fires, proving dispatch correctly routed to the SQL path | Valid, legitimate use of `{}` here — it is testing "absence of `generation_source`," which is exactly what `{}` represents; not a fake payload standing in for a real one |

**Summary**: one test (`SnapshotVerifier_WrongHash_ReturnsFalse`) is misleadingly named/scoped and does not test what it claims, but the underlying safety property it fails to directly prove **is** correctly proven by a different, real end-to-end test. No test found claims or implies that a real, production-shaped catalog snapshot can currently be confirmed into an active plan. `{}`/anonymous-object payloads are used only in deliberate negative-path fixtures, never to fake a positive result.

## 5. Snapshot integrity findings (A5)

Compared `CatalogPreviewSnapshotBuilder.Build`'s `hashableContent` anonymous object (`CatalogPreviewSnapshot.cs`) against `CatalogPreviewSnapshotVerifier.Verify`'s reconstruction (`CatalogPreviewSnapshotVerifier.cs`) field-by-field and in declaration order: `normalizedInput, asOfDate, CandidateKey, CandidateVersion, CandidateStatusAtGenerationTime, referencedArtifacts, GenerationSource, routeReason, resolverResults{ConditionType,Status,OutputValue,ReasonCode,Metadata}, createdAtUtc, expiresAtUtc`. **Field names, order, and the `HashSerializerOptions` (`WriteIndented = false`, no naming policy — i.e. declared-casing PascalCase/camelCase-as-written) are identical between builder and verifier.** No second, incompatible canonicalization algorithm was found — verified by reading both files completely; both call the same private `ComputeSha256Hex` pattern (separately implemented but identical: UTF-8 bytes → `SHA256.HashData` → lowercase hex).

`DecisionTrace`, `SelectedStageKeys`, `FallbackStagesUsed`, and `GeneratedPreviewPlanPayload` are excluded from the hash in **both** places consistently (confirmed by the verifier's own doc comment explicitly listing this and matching the builder's actual field list).

`RuntimeConditionResolutionResultConverter` reconstructs `RuntimeConditionResolutionResult` (private-constructor type) via the public `Evaluated`/`NotEvaluated` factories, populating only `ConditionType`, `OutputValue`, `ReasonCode`, `Metadata`, and (implicitly) `Status` — **the same five fields the hash actually depends on** (`InputSnapshot`, `Warnings`, `FallbackApplied`, `ConfidenceLabel` are not part of the hashable content in either builder or verifier, so their loss during round-trip does not affect hash consistency). The converter is registered **only** in `CatalogPlanConfirmationService`'s private `SnapshotDeserializeOptions` — confirmed not present in `PlanServices.SerializerOptions` (the options used to originally *write* the snapshot) — this is by design per the converter's own doc comment and does not create an asymmetry that matters, since `RuntimeConditionResolutionResult` serializes fine via default reflection (all relevant properties have public getters).

**Malformed vs. mismatched hash are correctly distinguished**: a JSON parse failure throws `PlanPreviewSnapshotMalformedException` (step 6, catching `JsonException`) **before** the hash is ever computed; a structurally-valid-but-hash-mismatched snapshot reaches step 9 and throws the distinct `PlanPreviewIntegrityFailedException`. Both are separately, correctly tested (`ConfirmAsync_MalformedJson_Throws...` and `ConfirmAsync_HashMismatch_Throws...`).

**Focused integrity tests were run** as part of the full suite (§12): `SnapshotVerifier_ValidSnapshot_ReturnsTrue` and `ConfirmAsync_HashMismatch_ThrowsPlanPreviewIntegrityFailedException` both **passed** — direct empirical evidence the round-trip (build → serialize with default reflection → deserialize with the custom converter → recompute hash) works correctly in practice, not just by static inspection.

**One naming/documentation inconsistency found** (non-blocking): `PlanPreviewSnapshotMalformedException`'s XML doc comment claims *"the message exposed to the caller is masked"* — but this exception maps to HTTP 422 in `GlobalExceptionHandler`, and per that handler's own logic, messages are masked **only** for HTTP 500. The actual thrown message for this exception (`"Plan preview 'X' snapshot is malformed and cannot be parsed. The stored JSON is structurally invalid."`) contains no sensitive detail, so there is no real leakage — but the doc comment is factually incorrect about masking behavior.

## 6. Migration/database schema findings (A6)

`20260712115640_Phase4E2_CatalogConfirmationState.cs`:
- Adds `PlanPreviews.ConfirmedPlanId` (uuid, nullable). ✅ Present.
- Adds `PlanPreviews.IsInvalidated` (boolean, nullable). ✅ Present.
- Creates `IX_PlanPreviews_ConfirmedPlanId` — **a plain, non-unique index** (no `.IsUnique()` in `AppDbContext.cs`'s Fluent API, no unique constraint in the migration). ✅ Confirmed present, confirmed non-unique.
- Adds `FK_PlanPreviews_TrainingPlans_ConfirmedPlanId` with `onDelete: ReferentialAction.SetNull` (matches `AppDbContext.cs`'s `.OnDelete(DeleteBehavior.SetNull)` configuration exactly — a cancelled/deleted `TrainingPlan` would null out the preview's pointer rather than cascading a delete or blocking it, which is the intended "soft" behavior for an audit-style back-reference). ✅ Consistent.
- `Up`/`Down` are symmetric and internally consistent.
- Migration ordering: `20260710072851_AddPlanCatalogProvenanceFields` → `20260712115640_Phase4E2_CatalogConfirmationState`, chronological, no duplicates, no abandoned migrations found in the directory listing.
- `AppDbContextModelSnapshot.cs` reflects both new columns, the index, and the FK — consistent with the migration.
- **Could not run an authoritative `dotnet ef migrations has-pending-model-changes` check**: the `dotnet-ef` global tool (v10.0.9, confirmed installed via `dotnet tool list --global`) did not resolve on this shell's `PATH`, and a filesystem search for it timed out. This is reported as **UNKNOWN** rather than assumed — the manual field-by-field comparison above (migration vs. `AppDbContext.cs`/`PlanPreview.cs`) is consistent, but an authoritative EF-tooling check was not obtained.
- **Critical, empirically-proven finding**: the real target Postgres database (`antigravity_dev`) **has not had this migration applied**. Running the full test suite against it produces `PostgresException: 42703 column p.ConfirmedPlanId does not exist` (see §12) for every HTTP-based test that calls `POST /api/v1/testing/reset`. This is a **database-application-order gap** (the migration file is correct and consistent; it simply has not been run against this physical database), not a code or migration-authoring defect. Not fixed in this Stage A pass, per the audit's read-only mandate.

## 7. Sequential idempotency findings (A7a)

**Sequential idempotency is proven**: `ConfirmAsync_Idempotent_RepeatedCallReturnsSamePlan` and `ConfirmAsync_Idempotent_DoesNotCreateDuplicatePlans` both pass and directly assert (a) the same `PlanId` is returned on a second call, and (b) exactly one `TrainingPlan` row exists in `ctx.TrainingPlans` after two sequential calls, when `ConfirmedPlanId` is already set on the preview (step 10's short-circuit).

## 8. Concurrent-safety findings (A7b)

**The database invariant "one preview → at most one confirmed plan" is NOT enforced at the database level.** Evidence:
- `PlanPreview.ConfirmedPlanId` has no `[ConcurrencyCheck]`/`[Timestamp]`/`RowVersion` property, and `AppDbContext.cs` configures no `.IsConcurrencyToken()` on it or any shadow property.
- `IX_PlanPreviews_ConfirmedPlanId` is a **plain, non-unique** index (confirmed in §6) — it accelerates idempotency lookups but enforces nothing.
- `TrainingPlan` has **no** `SourcePreviewId` field at all (confirmed by reading `TrainingPlan.cs` in full) — there is no possible unique constraint from the plan side either.
- Step 10's idempotency check is a plain optimistic read (`if (preview.ConfirmedPlanId.HasValue)`) with **no** `SELECT ... FOR UPDATE`, no serializable transaction, and no atomic conditional update (e.g. no `UPDATE ... WHERE ConfirmedPlanId IS NULL RETURNING ...` pattern).
- Per the task's explicit instruction, the existing `IX_TrainingPlans_InternalUserId_ActiveOnly` unique index (one active plan per user) is **correctly not accepted as preview-specific protection** in this audit — it is a different invariant (user-level, not preview-level) that would only accidentally catch a subset of the race (two plans for the *same user*, both `Status=Active`), and even then, no code catches the resulting `DbUpdateException` from that unique-index violation — it would propagate as a raw, unhandled exception (mapped to generic `500 INTERNAL_ERROR` by `GlobalExceptionHandler`'s default case), with **no** "loser reloads the winner" logic anywhere in `CatalogPlanConfirmationService`.
- The test suite **honestly self-discloses this exact gap** — `CatalogPlanConfirmationServiceTests.cs` contains an explicit `WARNING` comment directly above the idempotency test group (lines 533–539): *"These tests verify only sequential idempotency... They DO NOT claim or prove production concurrency safety. The current implementation is an optimistic read-then-write flow with no PostgreSQL row lock... or unique database index constraint on ConfirmedPlanId. Under concurrent load, two requests for the same preview can both read ConfirmedPlanId == null and both attempt plan insertion. This missing preview-specific database concurrency invariant blocks public activation."* This is the same conclusion reached independently in this audit.
- **However**, this defect is **not currently exploitable**: because the persistability guard (step 11, §3) unconditionally rejects every real snapshot **before** any `_context.Add`/`SaveChangesAsync` call, no code path exists today through which two concurrent requests could actually reach the unguarded insert logic with real data. The race is real in the code's *design* but has no reachable trigger *today*.

**Classification: UNSAFE (by design, for the future code path), but not currently exploitable given today's always-rejecting persistability guard.** This must be resolved before `GeneratedPreviewPlanPayload` is ever populated by a future phase, and independently blocks public catalog activation regardless (consistent with the test suite's own self-assessment).

## 9. Atomicity findings (A8)

The (currently unreachable) success path — `_context.TrainingPlans.Add(plan)`, `_context.PlanEvents.Add(planEvent)`, `preview.ConfirmedPlanId = plan.Id` — is followed by exactly **one** `await _context.SaveChangesAsync(ct)` call (line 334). Standard EF Core behavior wraps a single `SaveChangesAsync` invocation in one implicit database transaction, so **when/if this code path is ever reached, the three mutations (plan insert, event insert, preview update) are atomic relative to each other** — confirmed by inspection (no explicit `BeginTransactionAsync`/multiple `SaveChangesAsync` calls exist, and none are needed for atomicity of a single call). No `TrainingWeek`/`TrainingDay` inserts exist yet (stage-to-week scheduling unimplemented), so there is nothing beyond these three to make atomic.

No rollback-specific test exists (none is needed today, since the success path is unreachable), and no test exercises a real relational provider's transactional behavior for this specific method (all `CatalogPlanConfirmationServiceTests.cs` tests use EF InMemory, which does not model real Postgres transaction/locking semantics — consistent with the concurrency-safety gap noted in §8, since InMemory cannot surface a real race condition).

**Rejection occurs strictly before any mutation** — re-confirmed here for A8's specific instruction: verified above in §3 that `_context.TrainingPlans.Add` first appears after the step-11 `throw`.

## 10. Legacy SQL safety findings (A9)

Verified by direct reading of `PlanServices.ConfirmPlanAsync`'s non-catalog branch (lines 331–549): the dispatch check/decision precedes it, but the branch's own logic is unchanged from before Phase 4E.1/4E.2 — same ownership/expiry checks, same existing-active-plan short-circuit, same plan/week/day construction, same `PlanEvent` logging, same response shape. The **one** change is the exception type of one dead defensive guard (`previewData.Weeks.Count == 0` → now `InvalidOperationException` instead of Phase 4E.1's `ConflictAppException`), which is unreachable for any real legacy SQL preview (those always have ≥1 week by construction) and unreachable for any real catalog preview (dispatch now intercepts those earlier). `PlanServices_LegacySqlPreview_UsesExistingSqlConfirmPath` directly and passingly proves a non-catalog-shaped preview (`PreviewPayloadJson = "{}"`) is still routed to and processed by this exact branch.

**Focused legacy tests were run** as part of §12: all `SafeTemplateSelectionTests` (Phase 0 safe-selection guarantees) passed. `UserJourneyTests`/`FitnessEvidenceInputContractTests` (the real-Postgres HTTP suite, which exercises the fullest legacy confirm path) **could not be evaluated for pass/fail on their own merits** in this run because they all fail earlier, at `ResetAsync()`, due to the unrelated DB-schema-drift issue (§6/§12) — this is a test-environment gap, not evidence of a legacy-path regression, but it does mean **this audit cannot currently produce a passing empirical proof of the full HTTP-level legacy confirm flow** until the migration is applied to the dev database.

## 11. Public API safety findings (A10)

All 11 new Phase 4E.2 exceptions are mapped in `GlobalExceptionHandler.cs` with distinct status codes and error codes (`PLAN_PREVIEW_NOT_FOUND` 404, `PLAN_PREVIEW_FORBIDDEN` 403, `PLAN_PREVIEW_EXPIRED` 409, `PLAN_PREVIEW_INVALIDATED` 409, `PLAN_PREVIEW_SNAPSHOT_MISSING` 422, `PLAN_PREVIEW_SNAPSHOT_MALFORMED` 422, `PLAN_PREVIEW_SNAPSHOT_UNSUPPORTED` 422, `PLAN_PREVIEW_INTEGRITY_FAILED` 422, `PLAN_PREVIEW_GENERATION_SOURCE_INVALID` 422, `CATALOG_PREVIEW_NOT_PERSISTABLE` 422, `CATALOG_CONFIRMATION_FAILED` 500). None of the exception messages inspected reference resolver class names, catalog file paths, stack traces, hash *values* (only whether a hash check failed), TD identifiers, or internal lifecycle rule text. `PlanPreviewAlreadyConfirmedException` is defined with a doc comment describing it as an internal-only signal never surfaced to HTTP — **but it is never actually thrown anywhere in the codebase** (confirmed by a repo-wide grep finding only its own definition) — dead code, harmless, but its doc comment describes behavior that does not exist in the current implementation (the idempotency short-circuit is handled inline in `ConfirmAsync` without throwing/catching this type at all).

**Minor, non-blocking finding carried forward from Phase 4E.1.1's own audit**: several 400/422-mapped messages (e.g. `RuntimeConditionRequiredInputMissingException`) expose internal `ConditionType`/`reasonCode` vocabulary strings verbatim (not masked, since only 500s are masked). Phase 4E.2 adds one new instance of this pattern: `CATALOG_PREVIEW_NOT_PERSISTABLE`'s message includes the literal internal field name `"GeneratedPreviewPlanPayload"`, and `PLAN_PREVIEW_SNAPSHOT_UNSUPPORTED`'s message lists internal snapshot property names (e.g. `"CandidateVersion"`). These are schema/property-name leaks, not resolver traces, catalog paths, or stack traces — the task's specific prohibited items are not violated — but this is a real, if minor, internal-vocabulary exposure worth hardening in a future phase.

## 12. Test results — current working tree

`dotnet build RunningApp.sln -c Release` → **0 errors, 0 warnings.**

`dotnet test RunningApp.sln -c Release --no-build`:

```
Toplam test sayısı: 388
     Geçti: 351
     Başarısız: 37
```

**All 37 failures share one exact root cause** (confirmed by grepping the full test log): every failure is `System.Net.Http.HttpRequestException: Response status code does not indicate success: 500 (Internal Server Error)` thrown from each test's own `ResetAsync()` helper, which calls `POST /api/v1/testing/reset`. The server-side stack trace for every one of these terminates in:

```
Npgsql.PostgresException: 42703: column p.ConfirmedPlanId does not exist
  at RunningApp.Api.Controllers.TestingController.ResetDatabase(...)
```

This is a **database-schema-drift issue**: the code (correctly) expects `PlanPreviews.ConfirmedPlanId` to exist (per the new C# model and the new migration), but the actual `antigravity_dev` Postgres database has not had migration `20260712115640_Phase4E2_CatalogConfirmationState` applied. All 37 failing tests are in `UserJourneyTests.cs` and `FitnessEvidenceInputContractTests.cs` — the two classes that use `CustomWebApplicationFactory` against the real database via HTTP. **Zero failures occur in any EF-InMemory-based test class** (all `RuntimeCatalog/**` tests, including all 21 new `CatalogPlanConfirmationServiceTests`, pass), confirming the failures are isolated to the real-database dependency, not to any catalog-confirmation logic.

## 13. Baseline test results and regression comparison

**No git-committed baseline representing "pre-Phase-4E.2, post-Phase-4E.1.1" exists.** `git log` shows the latest actual commit is `fe85044` ("plan-catalog-added"), and `git ls-tree -r HEAD --name-only | grep RuntimeCatalog` returns **zero files** — meaning `fe85044` predates *all* backend catalog-integration work (Phases 1 through 4E.2 inclusive), not just Phase 4E.2. A worktree checkout of `fe85044` would not isolate Phase 4E.2's specific risk; it would show the entire catalog feature absent (and would very likely fail to even compile `RunningApp.IntegrationTests`, since numerous other files reference types that do not exist at that commit) — an apples-to-oranges comparison that would prove nothing about Phase 4E.2 specifically. **Per the task's own explicit instruction — "If a safe baseline cannot be identified, say so and do not claim 'pre-existing'" — no git-based baseline was used, and no failure below is described as "pre-existing" on that basis.**

The most defensible available comparison point is this **same session's own directly-recorded prior measurement**: at the conclusion of Phase 4E.1.1 (immediately before Phase 4E.2 began), this same working tree was built and tested with the result **367 passed, 0 failed, 367 total**, with zero Postgres schema errors of any kind (recorded directly in this conversation, not reconstructed from memory of file contents).

**Regression comparison**: Of the 37 current failures, **all 37 are attributable, by direct reading of their own stack traces, to the single, identified, non-code root cause in §12** (DB migration not applied) — none references any Phase 4E.2 *logic* (dispatch, snapshot validation, hash verification, or persistability). No test that previously passed at the end of Phase 4E.1.1 has been shown to fail for a *logic* reason. **This audit cannot claim these 37 are formally "no worse than baseline" via a reproducible git-diff comparison (no baseline commit exists to diff against), but it can and does claim, from direct stack-trace evidence, that their root cause is a database-application-order gap distinguishable from a code regression** — applying the already-authored, already-reviewed migration to the target database would very likely resolve all 37 (not verified in Stage A, since doing so would mutate a shared external resource).

## 14. Test-count reconciliation (A13)

| Count | Source |
|---|---|
| 367 | End of Phase 4E.1.1 (this session's own directly-recorded prior test run) |
| 388 | Current working tree, `dotnet test` total |
| 351 passed / 37 failed / 388 total | Current working tree, exact — **matches the count cited in the task prompt precisely** |

**Reconciliation, exact**: `367 + 21 = 388`.
- **+21 new tests**: `CatalogPlanConfirmationServiceTests.cs` contains exactly 21 `[Fact]` methods (counted directly: `grep -c "\[Fact\]"`), all new in Phase 4E.2.
- **0 net change** in `PlanServicesCatalogRoutingBoundaryTests.cs`: it has exactly 2 `[Fact]` methods both before (Phase 4E.1) and now — the spy-engine test (`GeneratePreviewAsync_PilotCombination_NeverInvokesSqlGenerationEngine`) is unchanged; the Phase 4E.1 confirm-boundary test (`ConfirmPlanAsync_CatalogShapedPreviewWithNoWeeks_...`) was **replaced** (not added alongside) by the Phase 4E.2 dispatch test (`ConfirmPlanAsync_CatalogSourcedPreview_DispatchesToCatalogConfirmService_NotOldConflictGuard`) — a rename-with-behavior-change, not a net addition.
- No test was found to be silently deleted, filtered out, or renamed elsewhere beyond the one substitution above (all other Phase 1–4E.1.1 test files were confirmed unchanged by direct reading or by the fact that their pass counts are consistent with 367's prior composition).
- **No guesswork was required** — every number above is either a direct `grep -c` count or a literal `dotnet test` summary line.

## 15. Blocking defects

1. **Concurrency: no database-enforced "one preview → at most one confirmed plan" invariant** (§8). Currently non-exploitable (blocked by the always-rejecting persistability guard), but this is a **design-level blocking defect for any future phase that populates `GeneratedPreviewPlanPayload`**, and independently blocks public catalog activation per the codebase's own explicit self-assessment.
2. **Target database schema drift**: the real `antigravity_dev` Postgres database lacks the Phase 4E.2 migration, causing 37/388 tests to fail and (if this reflects any real deployed environment) would cause `POST /api/v1/testing/reset` and any other `PlanPreviews`-querying code path to fail with a 500 in that environment. Not a code defect, but currently blocks a full green test run and blocks confident A9 verification of the HTTP-level legacy confirm path.

Neither defect is triggered by any currently-reachable production code path (per §3's proof that the persistability guard always rejects first), so neither constitutes an *active* safety violation today — both are latent/environmental and must be resolved before further catalog-confirm work proceeds.

## 16. Non-blocking defects

1. `SnapshotVerifier_WrongHash_ReturnsFalse` is misleadingly named/scoped (§4) — the real behavior is correctly covered elsewhere by `ConfirmAsync_HashMismatch_ThrowsPlanPreviewIntegrityFailedException`.
2. `PlanPreviewAlreadyConfirmedException` is dead code whose doc comment describes non-existent behavior (§11).
3. `PlanPreviewSnapshotMalformedException`'s doc comment incorrectly claims message-masking that does not occur for its 422 status code (§5).
4. Minor internal-vocabulary leakage (`ConditionType`/`reasonCode`/snapshot field names) in some 400/422 error messages (§11) — same class of finding as noted in Phase 4E.1.1's own audit, now with one additional instance.
5. `TD-PACESOURCE-002`'s tracked entry in `activation-readiness-risks.json`/`.md` was **not updated** by Phase 4E.2, despite `CatalogPlanConfirmationService.cs`'s and `PlanServices.cs`'s own doc comments explicitly asserting *"(TD-PACESOURCE-002 explicit closure decision, Phase 4E.2)"* and *"closes TD-PACESOURCE-002 for preview/confirm consistency."* The authoritative TD file still shows only the Phase 4E.1.1 `implementationNote` (§17 below) — **the code's claim of a recorded governance decision is not reflected in the actual governance-tracking artifact.** This is a documentation-traceability gap, not a behavior defect (the underlying reuse-not-recompute decision does appear to be correctly implemented and wired — see §17).
6. `ResolverInputSnapshot.CanonicalDistanceFamily` is never populated by `CatalogPreviewGenerator.BuildInputSnapshot` (confirmed: absent from that method's object initializer) — so `CatalogPlanConfirmationService.BuildPlan`'s `CanonicalDistanceFamily = input.CanonicalDistanceFamily` would be `null` if that (currently unreachable) code path is ever exercised. Latent, not currently triggerable.
7. `plan-catalog/**` modified files (§1a) were not individually inspected — low risk (outside this phase's scope) but recorded as an explicit limitation rather than silently ignored.

## 17. TD-PACESOURCE-002 status re-examination

The three documented closure criteria (unchanged since Phase 4E.1.1's own audit):
1. Decide reuse vs. recompute at confirm.
2. Wire that decision into the live preview/confirm flow.
3. Never silently default to wall-clock time at confirm.

**Phase 4E.2's actual code**: `CatalogPlanConfirmationService.BuildPlan` sets `StartedAt = snapshot.AsOfDate.ToDateTime(...)` — reusing the frozen preview `AsOfDate`, never recomputing from `DateTime.UtcNow` for any domain field (`ConfirmedAtUtc` is kept strictly separate and used only for technical timestamps). This **is** a genuine reuse decision, coded and wired — criteria 1 and 3 are substantively satisfied by the code as written. Criterion 2 ("wire... when PaceSourceResolver is eventually connected to generation") is only **partially** true: the wiring exists in `BuildPlan`, but that method is unreachable in production today (§3), so the wiring has never been exercised against a live PaceSourceResolver-driven generation flow.

**This audit does not close `TD-PACESOURCE-002`** (no Stage A file changes to `activation-readiness-risks.*` were made). Consistent with this repository's own established convention (e.g. `TD-CORE-READINESS-001`'s precedent: "implements real logic... but remains unwired from live generation... NOT mechanically closed"), the correct disposition — to be applied only if Stage B is authorized and only as an additive `implementationNote`, never a status change — would record: *decision made and wired in code (Phase 4E.2); not yet exercised in a reachable production path; remains OPEN pending real exercise once the persistability guard is eventually satisfied by a future phase.*

## 18. Complete seven-TD inventory (current status, unchanged by Phase 4E.2 code, verified against the live file)

| # | ID | Current status | Current prose stale re: 4E.2? | Phase 4E.2 changed the file? |
|---|---|---|---|---|
| 1 | `TD-D3-001` | OPEN | No — unrelated to confirm | No |
| 2 | `TD-WAVE5-001` | OPEN (revisited D13, not closed) | No — unrelated to confirm | No |
| 3 | `TD-BACKEND-001` | OPEN | Yes (already noted stale in Phase 4E.1.1's own audit re: "zero integration"/"silent fallback" claims; Phase 4E.2 does not change this staleness) | No |
| 4 | `TD-REGISTRY-001` | OPEN | No — unrelated to confirm | No |
| 5 | `TD-PACESOURCE-001` | OPEN, has a Phase 4D.5.1 `implementationNote` | No — `PaceSourceResolver` untouched by 4E.2 (confirmed: file not in this session's read/modify set; `TdPaceSource001_EstimatedPathStillNeverEmitted_ByConfirmService` structurally confirms no `PaceSourceResolver` field exists on the confirm service) | No |
| 6 | `TD-PACESOURCE-002` | OPEN, has a Phase 4E.1.1 `implementationNote` | **Yes — see §17**; the code now substantively implements the reuse decision the note anticipated, but the tracked file was not updated to reflect it | No |
| 7 | `TD-CORE-READINESS-001` | OPEN, has a Phase 4D.3.1 `resolutionNote` | No — unrelated to confirm | No |

**All seven predate Phase 4E.2. Phase 4E.2 introduced zero new TDs and modified the tracked TD file zero times** (confirmed: the file's content is byte-identical to what Phase 4E.1.1 left it, re-verified by grep in this pass). The only TD whose *real-world accuracy* is affected by Phase 4E.2's actual code changes is `TD-PACESOURCE-002` (§17) — a documentation/traceability gap between code comments and the tracked artifact, not a closure-criteria violation.

## 19. Files safe to retain unchanged

All Phase 4E.2 production source files (`CatalogPlanConfirmationService.cs`, `CatalogPreviewSnapshotVerifier.cs`, `RuntimeConditionResolutionResultConverter.cs`, the `CatalogPreviewSnapshot.cs` builder-signature extension, `PlanServices.cs`'s dispatch addition, `AppExceptions.cs`'s 11 new types, `GlobalExceptionHandler.cs`'s 11 new mappings, `Program.cs`'s DI registration, `PlanPreview.cs`'s two new fields, `AppDbContext.cs`'s FK/index configuration, the migration pair) are internally consistent, correctly isolated from generation/routing/resolution, and correctly enforce the "no empty active plan" invariant today. **Safe to retain as-is.**

## 20. Files requiring correction (Stage B, if authorized)

1. `CatalogPlanConfirmationServiceTests.cs` — rewrite or remove `SnapshotVerifier_WrongHash_ReturnsFalse` so it actually tests what its name claims (or rename it to accurately describe what it proves).
2. `AppExceptions.cs` — either remove the dead `PlanPreviewAlreadyConfirmedException`, or correct its doc comment, or wire it in if a future phase intends to use it.
3. `AppExceptions.cs` — correct `PlanPreviewSnapshotMalformedException`'s doc comment (masking claim does not match its 422 mapping).
4. `activation-readiness-risks.json`/`.md` — append (never replace/close) a `TD-PACESOURCE-002` note reflecting Phase 4E.2's actual reuse-decision implementation, per §17.
5. Database operations (outside source control) — apply migration `20260712115640_Phase4E2_CatalogConfirmationState` to `antigravity_dev` (not a file change; an infrastructure step).
6. `CatalogPreviewGenerator.BuildInputSnapshot` — populate `CanonicalDistanceFamily` on `ResolverInputSnapshot` (currently silently omitted; latent null downstream in `BuildPlan`).

None of these six are safety-critical (all are in currently-unreachable code paths or test-quality issues); none block accepting Phase 4E.2 as a gated foundation.

## 21. Files requiring revert

**None.** No file inspected in this audit was found to implement unsafe, over-scoped, or governance-violating behavior that would warrant reverting rather than correcting.

## 22. Files whose provenance remains unclear

The `plan-catalog/**` modified files listed at the bottom of §1a were not individually diffed/inspected in this pass (outside Phase 4E.2's scope, not named in the task's inspection list). No action was taken on them.

## 23. Recommended next action

Stage B, narrowly scoped to the six items in §20, **is appropriate** — none require architecture changes, none touch the concurrency-safety blocking defect (§15 item 1, which is out of scope for "targeted corrections" and would need its own dedicated, carefully-designed phase: a unique constraint or atomic conditional-update pattern, explicitly deferred here since the task lists "Do not redesign architecture" and this is a genuine design decision, not a typo fix). The database migration-application gap (§15 item 2) is an infrastructure action, not a Stage B code change.

## 24. Final audit classification

```text
PHASE4E_2_SAFE_TO_ACCEPT_AS_GATED_FOUNDATION
```

**Justification**: Every non-negotiable product invariant listed in the task prompt was checked against direct code and test evidence and holds today: (1) DRAFT candidates are never publicly serviceable (§2, §3 — the eligibility gate and persistability guard both still reject every real request); (2) catalog failure never falls back to SQL (§2 — no catch-and-reroute exists anywhere in the dispatch or confirm path); (3) `NotEvaluated` never automatically means fallback (unchanged since Phase 4E.1.1, not touched by 4E.2); (4)/(5) confirm never reruns resolver orchestration, generation, or `AsOfDate` computation (§2, directly proven); (6)/(7) current real non-persistable snapshots never create active plans, and no empty active plan is created (§3, proven with a passing test that asserts zero rows across four tables); (8) legacy SQL behavior is unchanged in substance (§10, one unreachable dead-guard exception-type change only); (9) resolver traces remain internal (unchanged, not touched by 4E.2); (10) public activation remains blocked without production concurrency safety — **and this audit independently confirms the concurrency gap is real, currently non-exploitable, and already self-disclosed in-repo** (§8); (11) no claim of live stage-to-week scheduling is made anywhere inspected; (12) no destructive git action was performed.

The two blocking-defect-shaped findings (§15) are both **latent** (concurrency, gated by an always-firing guard) or **environmental** (DB migration not yet applied, not a code fault), not active safety violations reachable by any currently-possible request. The non-blocking findings (§16) are real but narrow, well-isolated, and already largely self-disclosed by the prior agent's own honest test comments — a notable positive signal for the trustworthiness of this specific body of work, in contrast with the general caution this audit was asked to apply.
