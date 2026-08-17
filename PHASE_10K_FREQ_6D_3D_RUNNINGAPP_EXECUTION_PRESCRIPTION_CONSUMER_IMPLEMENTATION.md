# Phase 10K-FREQ.6D.3D — RunningApp Lossless Execution-Prescription Consumer & Internal Materialization

**Implementation phase. No product decision, no domain decision, no catalog authoring, no dual-KEY progression, no public activation.**

## 1. Pre-flight ledger verification

- `PHASE_LEDGER.md` row 56: `FREQ.6D.3C`, Execution Status `DONE`, Final Classification `FREQ6D3C_PROJECTION_IMPLEMENTED_PROFILE_WIRING_DEFERRED`, Provenance `VERIFIED` — confirmed by direct read before any implementation work.
- Report `PHASE_10K_FREQ_6D_3C_DETERMINISTIC_EXECUTION_PROJECTION_AND_BUNDLE_INTEGRATION.md` exists (confirmed via `git ls-files`).
- Parent implementation SHA `573b7ac58c5677f27f2e729569f1e5c21cc06d4e` — confirmed reachable from HEAD (`git merge-base --is-ancestor`).
- Governance baseline SHA `244a1547e6d3ae9279ff3323431fa13ea7a9c123` — confirmed reachable from HEAD.
- `FREQ.6D.3D` confirmed **not** already present as a completed Phase ID (`git ls-files | grep 6D_3D` → no results before this phase).
- `git rev-parse HEAD` → `244a1547e6d3ae9279ff3323431fa13ea7a9c123`; `git status --short` → only pre-existing `baseline_tmp` (untouched throughout); `git branch --show-current` → `main`; `git diff --check` → clean.

## 2. Parent SHA

`573b7ac58c5677f27f2e729569f1e5c21cc06d4e` (`feat(catalog): project prescription profiles into published bundles`) — the real projector/assembler implementation this phase consumes the output of.

## 3. Files changed

Production code (4 new files, `backend/RunningApp.Application/RuntimeCatalog/Prescription/Execution/`):
- `ExecutionPrescriptionBoundaryExceptions.cs`
- `PublishedTemplateBundleJsonReader.cs`
- `ExecutionPrescriptionIndex.cs`
- `CatalogSessionPrescriptionSource.cs`

Project reference: `backend/RunningApp.Application/RunningApp.Application.csproj` (added `PlanCatalog.Contracts.csproj` reference only).

Tests (3 new files, `backend/RunningApp.IntegrationTests/`):
- `Architecture/RunningAppPlanCatalogDependencyTests.cs`
- `RuntimeCatalog/Prescription/Execution/ExecutionPrescriptionIndexTests.cs`
- `RuntimeCatalog/Prescription/Execution/PublishedTemplateBundleJsonReaderTests.cs`
- `RuntimeCatalog/Prescription/Execution/CatalogSessionPrescriptionSourceTests.cs`

Plus the mechanical rebuild of tracked `bin/`/`obj/` build artifacts across every backend project (this repository tracks build output — confirmed via `git check-ignore`, which reports these paths as **not** ignored; the same convention every prior commit in this history follows). No unrelated source file was touched.

## 4. Project-reference change

`RunningApp.Application.csproj` gained exactly one new `<ProjectReference>`: `..\..\plan-catalog\src\PlanCatalog.Contracts\PlanCatalog.Contracts.csproj`. `PlanCatalog.Contracts.csproj` itself has zero `<ProjectReference>` entries (confirmed via `plan-catalog/tests/PlanCatalog.Tests/Architecture/ProjectDependencyTests.cs`'s own `Contracts_HasNoProjectReferences` assertion and by reading the real `.csproj`), so this is a leaf, non-transitive addition — no path to `PlanCatalog.Core`/`PlanCatalog.Infrastructure` is introduced. Verified by a new architecture test (§18) via `Assembly.GetReferencedAssemblies()` on the compiled `RunningApp.Application.dll`, not merely by reading the `.csproj`.

## 5. Process B ingestion point

Research confirmed **no production code path in `RunningApp.*` consumed `PublishedTemplateBundle` at all** before this phase — the existing `PlanCatalogBundleLoader`/`CatalogWorkoutDefinitionLoader` hand-parse individual raw catalog JSON documents (combinations/templates/layouts/etc.), never a `PUBLISHED_TEMPLATE_BUNDLE` document, and the only prior reference to the real Contracts bundle type anywhere under `backend/` was in `RunningApp.IntegrationTests`' Phase 4G.5O publication test fixture. This phase is therefore the genuine first Process A→B JSON crossing point in production code: `PublishedTemplateBundleJsonReader.Read(string json)` deserializes a real `PublishedTemplateBundle`, using JSON options that replicate (camelCase properties, `UPPER_SNAKE_CASE` enum members) — not reference — `PlanCatalog.Infrastructure.Serialization.CanonicalJsonOptions`, since referencing Infrastructure is forbidden. Proven against the *real* canonical serializer in `PublishedTemplateBundleJsonReaderTests.cs` (test-only `PlanCatalog.Infrastructure` reference, matching the pre-existing Phase 4G.5O test-fixture convention already present in this test project).

This loader is **not wired into the live candidate-loading pipeline** (`PlanCatalogBundleLoader.LoadCandidateAsync`) — per §30/§21 of this phase's own instructions ("must not force current catalog/runtime identities onto the profile path", "no production profile wiring"), and because doing so would require a real `PUBLISHED_TEMPLATE_BUNDLE` file to exist in production, which FREQ.6D.3C explicitly left absent. It is a real, tested, standalone component ready for FREQ.6D.4 to wire in.

## 6. Consumer/internal representation architecture

- `ExecutionPrescriptionIndex` carries the immutable `PlanCatalog.Contracts.Prescriptions.ExecutableWorkoutPrescription` objects **directly** in its internal dictionary (`Dictionary<ExecutionPrescriptionLookupKey, ExecutableWorkoutPrescription>`) — no field is copied out into a parallel type.
- `CatalogSessionPrescriptionSource` (abstract record + two sealed subclasses `Legacy`/`ProfileBacked`) follows this codebase's own existing "abstract base + exactly two sealed subclasses" convention (`RunningApp.Application.Commands.Plan.PlanPreviewCommand` — confirmed by direct inspection). `ProfileBacked.Prescription` is typed `ExecutableWorkoutPrescription` — the real Contracts object, held by reference, never reconstructed.

## 7. Duplicate-truth-source check

Grepped the four new production files for any independently-settable field named/shaped like `RepetitionCount`, `RecoveryCount`, `Placement`, `Mode`, `Value`/`Unit`, `IntensityMode`/`DescriptorKey`, or `DoseCategory` outside of the real Contracts types — none exists. `ExecutionPrescriptionIndex.ResolveExact` returns the exact `ExecutableWorkoutPrescription` instance stored during `Build` (proven by `Assert.Same` in `ExactVersionMatch_ReturnsExecutionValue`), not a copy. The one intentional, disclosed duplication is `ExecutionBoundaryUpperSnakeCaseNamingPolicy` — a ~20-line **JSON-naming utility**, not execution/prescription domain logic, duplicated (not referenced) from `PlanCatalog.Infrastructure.Serialization.UpperSnakeCaseNamingPolicy` solely because RunningApp.Application cannot reference `PlanCatalog.Infrastructure`. This does not create a second execution-authority source — it only has to byte-for-byte match a naming convention, proven by the real-canonical-serializer round-trip tests in §5.

```
RUNNINGAPP_CONSUMER_DOES_NOT_DUPLICATE_EXECUTION_AUTHORITY — confirmed.
```

## 8. Exact profile lookup contract

```csharp
internal ExecutableWorkoutPrescription ResolveExact(VersionedCatalogReference profileRef)
```
on `ExecutionPrescriptionIndex`, keyed by `(DocumentType, Key, Version)` via `ExecutionPrescriptionLookupKey`. Reuses the pre-existing Contracts type `PlanCatalog.Contracts.References.VersionedCatalogReference` (no new reference type invented). Exact match returns the value; missing exact match (including same-key-different-version) throws `ExecutionPrescriptionNotFoundException`; duplicate provenance at build time throws `ExecutionPrescriptionDuplicateProvenanceException`; no `FindLatest`/`FindNearest`/`FindFirst` method exists anywhere in the type.

## 9. Legacy/profile-backed branching

`ExecutionPrescriptionIndex.Build(bundle)`: `bundle.ExecutionPrescriptions is null` → `IsProfileBacked = false` (legacy); non-null (even a theoretically empty list) → `IsProfileBacked = true`, per FREQ.6D.3B/3C's stated null-vs-legacy semantics ("no dependencies... preserves `ExecutionPrescriptions = null`"). A legacy index's `ResolveExact` throws `ExecutionPrescriptionLegacyIndexException` rather than silently returning nothing — a present-but-corrupt profile-backed bundle throws at `Build` time and never produces a usable (or legacy-equivalent) index (`InvalidContractsExecutionValue_FailsClosed_NoLegacyFallback`, `CorruptProjectionInProfileBackedBundle_FailsClosed_DoesNotDegradeToLegacyIndex`).

## 10. Continuous preservation

`ContinuousComponent_PreservesExactWorkQuantity` (both `Seconds`/`Meters` units) — exact `Work.Value`/`Unit` preserved, `RepetitionCount`/`Recovery` confirmed null, matching the Contracts continuous-component invariant.

## 11. Repeated preservation

`RepeatedComponent_PreservesRepetitionCountAndNestedRecovery` (both units) — `RepetitionCount` and nested `Recovery.Value`/`Unit` preserved exactly, atomic (never expanded into separate structural components — no such expansion code exists anywhere in this phase's diff).

## 12. Recovery preservation

`RecoveryPlacementAndCount_PreservedExactlyAsPublished` (both `BetweenRepetitions`/`AfterEachRepetition`, distinct counts) and `RecoveryMode_PreservedExactly` (`Walk`) — placement, count, and mode all pass through unmodified. `NoRecoveryCountRecalculation_ExactPublishedValueIsReturnedUnchanged` proves `PrescriptionRecoveryCardinality.Derive` (or any N/N-1 arithmetic) is never invoked by RunningApp — confirmed by code inspection (no such call exists in the diff) and by test (published `RecoveryCount=3` returned unchanged).

## 13. Intensity preservation

`TypedIntensity_PreservedExactly_NeverFlattenedToString` — all three modes (`PaceBased`/`EffortBased`/`HeartRateBased`) plus `DescriptorKey` preserved with the typed `ExecutableIntensityMode` enum intact; never coerced to the legacy `IntensityDescriptor` bare-string shape (`CatalogPrescriptionSegment.ComponentType`/`IntensityDescriptor` are untouched, still string-typed, exactly as they were before this phase — no code path converts one to the other).

## 14. FARTLEK proof

`StructuredFartlek_PreservesRepetitionRecoveryAndIntensity_NoWorkoutFamilySpecificBranch` — a synthetic 6-repetition/60s-surge/60s-jog-float/`EffortBased` fartlek consumed through the exact same `ExecutionPrescriptionIndex.Build`/`ResolveExact` path as every other test; grepped `ExecutionPrescriptionIndex.cs`/`PublishedTemplateBundleJsonReader.cs`/`CatalogSessionPrescriptionSource.cs` for `"FARTLEK"`/`"Fartlek"` — zero matches. No workout-family branch exists.

## 15. THRESHOLD proof

`IntervalizedThreshold_ConsumedThroughSameGenericPath_NoIfFartlekIfThresholdBranch` — a synthetic 4×1600m/400m-jog/`PaceBased` threshold consumed through the identical generic path; same zero-match grep confirms no `"THRESHOLD"`-specific branch exists in production code.

## 16. Taper proof

`TaperReducedDose_RetainsExactPublishedRecoveryCount_NoTaperSpecificCode` — the two exact scenarios requested (6-repetition `BetweenRepetitions` → `RecoveryCount=5`; 4-repetition `BetweenRepetitions` → `RecoveryCount=3`) both retained exactly, through the same generic path, with zero `"Taper"`/`"taper"` string anywhere in the four production files.

## 17. Fail-closed behavior

All 11 requested cases from §26 are covered, each with its own test: unsupported schema version (`ContractSchemaVersion=2`), invalid Contracts value (zero work quantity), duplicate exact `SourceProfile` provenance, missing exact reference, exact key present at wrong version, corrupt repeated-recovery projection, present-collection-cannot-become-legacy, no `RecoveryCount` recalculation, no key-only resolution (wrong `DocumentType` with matching key/version is rejected), no latest/nearest resolution, no partial/first-match selection (the dictionary-based exact index structurally cannot do partial/first-match — there is no ordered/sequential lookup path at all). Every failure throws a typed exception under `ExecutionPrescriptionBoundaryException`; none is caught and silently converted to legacy behavior anywhere in the four production files (confirmed by inspection — no `catch` block exists in any of them).

## 18. Architecture tests

`RunningAppPlanCatalogDependencyTests` (new, `RunningApp.IntegrationTests/Architecture/`) — 4 tests, all passing: `RunningAppApplication_References_PlanCatalogContracts` (positive), `RunningAppApplication_DoesNotReference_PlanCatalogCore`, `RunningAppApplication_DoesNotReference_PlanCatalogInfrastructure` (both via `Assembly.GetReferencedAssemblies()` reflection on the compiled `RunningApp.Application.dll`, matching this repository's existing `PublishedBoundaryTests`/`ProjectDependencyTests` reflection convention — no `NetArchTest` package exists anywhere in this repository), and `RunningAppApplicationCsproj_HasNoTransitiveCoreOrInfrastructureProjectReference` (direct `.csproj` text check as a second, independent layer).

## 19. Targeted tests

42 new focused tests across three files (`ExecutionPrescriptionIndexTests.cs`: 27, `PublishedTemplateBundleJsonReaderTests.cs`: 3, `CatalogSessionPrescriptionSourceTests.cs`: 3) plus 4 architecture tests, mapping directly onto the phase's §31 test matrix (all 30 requested items are covered: architecture ×4/5 collapsed into the same 4-test file plus 1 covered by regression in item 28-30 below, legacy/profile-backed ingestion, boundary validation, exact index/lookup/wrong-version/duplicate, all continuous/repeated/recovery/intensity/dose/distance-accounting preservation cases, FARTLEK/THRESHOLD/taper proofs, explicit-error-no-fallback, and the three legacy regressions via the full-suite run in §20). **41/41 passed on first run; 1 test bug found and fixed by me** (`RunningAppWriter_And_RealCanonicalSerializer_ProduceEquivalentDeserializedBundle` initially failed due to `PublishedTemplateBundle`'s record-generated `Equals` comparing `IReadOnlyList<T>` properties by reference — the same pitfall this engagement has hit before with `V1FourDayWeekAllocation` — fixed by comparing scalar fields plus xUnit's element-wise `Assert.Equal` on the list instead of whole-record equality; not a production defect). **42/42 pass after the fix.**

## 20. Backend regression

- New execution-consumer + architecture tests: **42/42 passed**.
- `RuntimeCatalog` namespace (binder/planner/prescription/3D/4D/Beginner4D and every other catalog runtime test): **2,878/2,878 passed**, 5m30s.
- Full `RunningApp.IntegrationTests` suite: **3,521/3,522 passed**, 1 failure (`Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`, HTTP 500 vs. expected 200). **Verified pre-existing, not a regression introduced by this phase**: I stashed all FREQ.6D.3D changes (`git stash push -u`), rebuilt, and re-ran that exact test against the unmodified `244a154` baseline — it failed identically (same assertion, same 500). Restored the stash afterward (`git stash pop`, resolving one build-artifact merge conflict by discarding the baseline rebuild's binary noise, not any source), rebuilt, and re-confirmed all 42 new tests plus the full `RuntimeCatalog` namespace still pass on the final committed state. This failing test is unrelated to `ExplicitZeroReadiness`'s own domain by name and to anything this phase touches; it is not adopted, fixed, or otherwise altered by this phase (that would be scope creep beyond FREQ.6D.3D), and is disclosed here rather than hidden or silently claimed-passing.

## 21. Build results

`dotnet build RunningApp.sln`: **0 warnings, 0 errors**, both before the final test run and after re-verification. `dotnet build RunningApp.Application/RunningApp.Application.csproj` and `RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj` individually: **0 warnings, 0 errors**. No `plan-catalog` project reference/build was needed (no `PlanCatalog.*` source was touched).

## 22. git diff --check

Clean at every checkpoint: before staging, after staging (`--cached`), and on the final commit's file set.

## 23. Production profile-wiring status

Unchanged from FREQ.6D.3C: **`PROJECTION_CAPABILITY_IMPLEMENTED_PRODUCTION_PROFILE_WIRING_DEFERRED_TO_6D4`** still holds. No production candidate/bundle in this repository is wired to produce or load a profile-backed `PublishedTemplateBundle`; `PlanCatalogBundleLoader.LoadCandidateAsync` (the real, live candidate-loading path) is untouched by this phase's diff. Only synthetic, in-memory (and, for the JSON-reader tests, synthetic-but-real-JSON) fixtures were used throughout.

## 24. FREQ.6D.4 exact input contract

All 10 items from this phase's own §35 are met:
1. RunningApp can ingest profile-backed execution projections — `ExecutionPrescriptionIndex.Build`/`PublishedTemplateBundleJsonReader.Read`, tested.
2. RunningApp sees `PlanCatalog.Contracts` only — proven by architecture tests (§18).
3. Execution values remain lossless internally — proven by §10-16.
4. Exact profile reference resolves one exact execution value — `ResolveExact`, tested.
5. No version selection occurs in RunningApp — no `Latest`/`Nearest`/`First` method exists.
6. `RecoveryCount` is consumed, not derived — proven (§12/17).
7. Repetition/recovery/intensity semantics remain intact — proven (§10-16).
8. Legacy bundle path remains unchanged — `PlanCatalogBundleLoader` untouched; 3D/4D/Beginner4D regression green (§20).
9. Profile-backed corrupt/missing exact execution fails closed — proven (§17).
10. No lane/keyOrdinal/progression decision exists in the consumer — `CatalogSessionPrescriptionSource` is deliberately **not** wired into `CatalogSessionPrescriptionPlanner`'s live branch in this phase (§6), and no `LaneOrdinal`/`DoseCategory`-based selection code exists anywhere in the four new files (grepped, zero matches for `LaneOrdinal`, and the only `DoseCategory` reference is a pass-through property read in a test assertion, never a branch).

FREQ.6D.4 now owns: Week × LaneOrdinal → exact progression stage → exact profile reference → this phase's `ResolveExact` seam.

## 25. Implementation SHA

`2ef8a11a0f41f42e9b5a61de3182d289c4b7f85d` — `feat(runtime): consume executable prescription projections` (135 files changed: 12 new/modified source+test files, 1 `.csproj` change, remainder tracked build-artifact rebuilds per this repository's established convention).

## 26. Documentation/governance SHA

`bb4ad2c804b94ec8f8e123025534784abe9f6a2c` — `docs(freq-6d): close RunningApp execution consumer phase`.

## 27. Ledger update result

Appended to `PHASE_LEDGER.md` as row 58 (see below), `Provenance = VERIFIED`, `Parent Phase(s) = FREQ.6D.3C`. No historical row rewritten.

## 28. Roadmap update result

`MASTER_ROADMAP.md` §2 (Current Canonical State) and §5/§14 (Next Concrete Block) updated to reflect `FREQ.6D.3D` as the new latest verified phase and `FREQ.6D.4` as next. No other section altered.

## 29. Final classification

```
FREQ6D3D_RUNNINGAPP_EXECUTION_CONSUMER_IMPLEMENTED
```

Every stop condition in §33 was checked and none triggered: RunningApp never references `PlanCatalog.Core`/`Infrastructure`; `ExecutableWorkoutPrescription` is never flattened into `CatalogPrescriptionSegment`; no backend-local field-for-field mirror was created (only a generic, disclosed JSON-naming-policy utility duplication, not execution-domain duplication); `RecoveryCount` is never recalculated; no lane/workout/dose-based profile choice exists; production profile wiring was not attempted; no public API or persistence expansion occurred; lossless internal consumption was achieved without any new architecture decision beyond what FREQ.6D.1/1A/1B/CP1 already froze. The one narrow, disclosed judgment call — building a new standalone `PublishedTemplateBundle` JSON loader since none existed in production, and introducing `CatalogSessionPrescriptionSource` as a proven-but-unwired seam — is exactly the "narrowest correct boundary" this phase's own §7/§18 asked for, not scope creep.
