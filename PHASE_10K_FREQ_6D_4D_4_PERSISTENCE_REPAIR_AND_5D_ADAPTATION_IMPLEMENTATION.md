# Phase 10K-FREQ.6D.4D.4 (Split D) — Durable Profile Lineage, Schedule-Repair Lineage & Five-Session Adaptation Policy Implementation

**Implementation phase executing Split D only of the FREQ.6D.4D-approved D1 architecture. No product decision, no dosage change, no RunLayout change, no lane/stage re-derivation, no profile selection/content change, no bundle/projection change, no new Adaptation policy, no public 5D activation, no real RUN_LAYOUT_5D/combination authoring.**

## 1. Preflight

`PHASE_LEDGER.md` row 75: `FREQ.6D.4D.3`, `IMPLEMENTATION`, `DONE`, `FREQ6D4D_SPLIT_C_IMPLEMENTED_ADAPTATION_ENGINEERING_GAP_REMAINS`, confirmed. `FREQ.6D.4D`, `FREQ.6`, `FREQ.6D.1B` confirmed `DONE`/`VERIFIED`. Commits `799af2c`, `5572570`, `331c9ca` confirmed reachable from HEAD. Starting HEAD `331c9ca16db873d87072f29e2aae4538513cd7d3`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 21`. `git status --short` → only ` m baseline_tmp` and the two pre-existing, unrelated `plan-catalog/artifacts/audits/*` modifications. `git diff --check` → clean. `FREQ.6D.4D.4` confirmed not already a ledger row.

The real Split-C persistence-gap matrix (`PHASE_10K_FREQ_6D_4D_3...md §24`) was re-read, not reconstructed from chat, and confirmed directly against current code: `StructuralRole`/`ProgressionStageKey`/`WorkoutDefinitionKey`/`Version` already persisted (`TrainingDay.CatalogStructuralRole`/`CatalogProgressionStageKey`/`CatalogWorkoutDefinitionKey`/`Version`, confirmed present); `PrescriptionProfileKey`/`Version` available in memory (`CatalogPrescribedSession.PrescriptionSource`) but not persisted — the one real gap this split closes; execution content/hash classified `BUNDLE_ONLY` — not persisted, confirmed correct per the real `FREQ.6D.4D` architecture §17 (re-read directly, not from memory): *"Executable-prescription source hash/version... not required on TrainingDay... a redundant provenance authority."* `LaneOrdinal` persistence: the same architecture §17 explicitly resolves this — *"DERIVABLE_BUT_SHOULD_PERSIST — no new column recommended... reconstructible from (CatalogProgressionStageKey, progression key+version)"* — **not genuinely ambiguous**, so `FREQ6D4D_SPLIT_D_BLOCKED_ON_LANE_PERSISTENCE_AUTHORITY` does not apply; no `LaneOrdinal` column was added.

## 2. Runtime/DB environment

`docker ps` initially failed (`failed to connect to the docker API`) — Docker Desktop was not running, matching Split B/C's disclosed limitation. Per this phase's own load-bearing §1 gate, **Option A was taken**: Docker Desktop was started (`Start-Process`), the daemon came up (~90s), `docker compose up -d postgres` brought up the repository's own dev Postgres service (`docker-compose.yml`, `appsel-dev-postgres`, already defined for exactly this purpose), and `pg_isready` confirmed `accepting connections`. **Real PostgreSQL-backed verification was genuinely available for this entire phase** — not simulated, not skipped, not classified `VERIFICATION_LIMITED_BY_ENVIRONMENT`. Every DB-backed test claim in this report ran against real Postgres.

## 3. Parent lineage contract

Honored throughout: `CatalogPrescribedSession.PrescriptionSource` (Split C) is read verbatim by the new mapper glue — no profile re-selection, no stage recomputation, no `LaneOrdinal` re-derivation anywhere in this diff (confirmed by review: zero references to `ProgressionStageAllocator`, `CatalogWorkoutBinder`, or any PlanCatalog authoring type). `ExecutionPrescriptionIndex`, `CatalogSessionPrescriptionSource`'s own record shape, `CatalogBundleAssembler`, and `WorkoutPrescriptionExecutionProjector` are all untouched.

## 4. Files inspected

`TrainingDay.cs` (full read); `CatalogPlanConfirmationService.cs` (`BuildCatalogTrainingDay`, full read); `AppDbContext.cs` (confirmed no explicit `TrainingDay` column-type configuration exists for the analogous `CatalogWorkoutDefinitionKey`/`Version` fields — convention-based mapping is sufficient, so none was added for the new fields either); existing EF migrations (`AddPlanCatalogProvenanceFields`, `Phase4F9_CatalogConfirmationPersistence` — confirmed the established nullable-additive-column precedent this migration follows); `CatalogPublicPreviewMaterializer.cs` (the real, live standalone 8-14-week Core confirmation mapper — `MapSession`); `PreparationRunwayPersistablePlanMapper.cs` (the real, live combined runway+Core confirmation mapper — `MapCoreSession`; confirmed it reuses the exact same `CatalogPlanConfirmationService`/`TrainingDay` persistence surface, "the entire reason no new confirmation/persistence machinery was written for the runway," per its own doc comment); `GeneratedCatalogPlanPayload.cs` (`GeneratedCatalogDayProvenance`); `ScheduleRepairPersistenceService.cs` (`BuildReplacement`, `TrySubstituteFutureEasyAsync`, full read); `WindowExecutionSummaryBuilder.cs` (full read — confirmed already generalized, N-role-aware, lineage-correct); `NextWindowLoadDecisionPolicy.cs` (full read, both before/after edit); `AdaptationDomainContracts.cs` (`WindowExecutionSummary`, `AdaptationLineageInvalidException`); `PHASE_10K_FREQ_6D_1B_SEVERITY_TABLE_FIDELITY_AND_OPEN_DECISION_CHECK.md` (full re-read — the real, frozen 24-row table and its own proposed dispatch pseudocode, §7/§25 below); `PlanAdaptationV1DecisionTests.cs` (existing 4-session test conventions, reused); `Freq4TwoKeyCardinalityGeneralizationTests.cs` (found and fixed a genuine pre-existing test-data inconsistency, §26 below).

**Real, load-bearing architectural finding**: `LongHorizonRollingSessionState` (the repair/substitution entity `ScheduleRepairPersistenceService.BuildReplacement`/`TrySubstituteFutureEasyAsync` operate on) carries **no** profile-lineage or lane-identity fields today, and none were added by this split. This is not a gap — `FREQ.6D.1B §Track B` (re-read directly) already resolved this exact question: *"leave the stand-in row's `CatalogProgressionStageKey`/profile columns null, matching today's exact behavior, since FREQ.6 §7 already makes this observationally inert for adherence purposes — simplest choice, no new mechanism required... RESOLVED_NON_BLOCKING."* `BuildReplacement` already copies `WorkoutKey`/`WorkoutVersion`/`SessionRole` verbatim (§7/§18 satisfied by construction — there is no profile field to lose because none exists, matching the explicitly-recommended design), and `TrySubstituteFutureEasyAsync` never invents one (§16 satisfied — an EASY stand-in cannot falsely carry a KEY's profile identity, because no such field exists on this entity at all).

## 5. Files changed

**Persistence** (4 files):
- `Domain/Entities/TrainingDay.cs` — added `CatalogPrescriptionProfileKey` (`string?`), `CatalogPrescriptionProfileVersion` (`int?`), additive, nullable, adjacent to the existing `CatalogWorkoutDefinitionKey`/`Version` pair.
- `Persistence/Migrations/20260819111302_Phase10KFreq6D4D4CatalogPrescriptionProfileLineage.cs` (+ `.Designer.cs`) — new migration, two `AddColumn` operations only.
- `Persistence/Migrations/AppDbContextModelSnapshot.cs` — regenerated (mechanical).

**Mapping glue** (4 files):
- `RuntimeCatalog/Prescription/Execution/CatalogSessionPrescriptionSource.cs` — added `CatalogSessionPrescriptionSourceExtensions.ExactProfileKeyOrNull()`/`ExactProfileVersionOrNull()` (shared, so the two real mapper call sites below do not each reimplement the Legacy/ProfileBacked match).
- `RuntimeCatalog/Schedule/GeneratedCatalogPlanPayload.cs` — added `SourcePrescriptionProfileKey`/`SourcePrescriptionProfileVersion` to `GeneratedCatalogDayProvenance`.
- `RuntimeCatalog/Schedule/CatalogPublicPreviewMaterializer.cs` — `MapSession` now populates the new provenance fields via the shared extension methods.
- `RuntimeCatalog/PreviewRouting/PreparationRunwayPersistablePlanMapper.cs` — `MapCoreSession` likewise (`MapRunwaySession`, the pure-runway session mapper with no `CatalogPrescribedSession` input at all, is untouched — its sessions are never profile-backed).
- `RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs` — `BuildCatalogTrainingDay` copies the two new provenance fields onto the new `TrainingDay` columns verbatim.

**Adaptation policy** (1 file):
- `RuntimeCatalog/Schedule/LongHorizon/Adaptation/NextWindowLoadDecisionPolicy.cs` — implements the real, frozen `FREQ.6 §6` / `FREQ.6D.1B`-verified 24-row five-session severity table, dispatched on `WindowExecutionSummary.ExpectedSessionCount == 5`; legacy (non-5-session) dispatch is byte-for-byte unchanged, in its own untouched code path.

**Tests** (2 new files, 1 modified):
- `Freq6D4DSplitDFiveSessionAdaptationSeverityTests.cs` (new, 44 tests) — all 24 rows verbatim, count-4 role gate, lane equivalence, 0-5/5, legacy 4-session preservation, fail-closed invalid states.
- `Freq6D4DSplitDProfileLineagePersistenceTests.cs` (new, 5 tests) — real-Postgres round-trip matrix.
- `Freq4TwoKeyCardinalityGeneralizationTests.cs` — one pre-existing test's internally-inconsistent fixture data corrected (§26).

Plus mechanical `bin`/`obj` rebuild.

**No** PlanCatalog file, `WorkoutPrescriptionExecutionProjector`, `CatalogBundleAssembler`, `ExecutionPrescriptionIndex`, `WindowExecutionSummaryBuilder`, `RunLayout`, or public-routing file was touched — confirmed by direct diff review, matching §48's expected-no-changes list exactly.

## 6. Persistence authority

Applied exactly as classified by the real architecture and Split-C matrix: `MUST_PERSIST_EXACT_LINEAGE` → `CatalogPrescriptionProfileKey`/`Version` (new columns, this split). `DERIVABLE_FROM_IMMUTABLE_LINEAGE` → `LaneOrdinal` (no column added). `BUNDLE_ONLY` → execution-prescription content/hash/`DoseCategory`/`RecoveryCount` (no column added, none of these were persisted).

## 7. Profile columns

`TrainingDay.CatalogPrescriptionProfileKey` (`string?`), `TrainingDay.CatalogPrescriptionProfileVersion` (`int?`) — the exact repository-conformant naming convention (mirrors `CatalogWorkoutDefinitionKey`/`CatalogWorkoutDefinitionVersion` precisely). Both null for Legacy; both populated together for ProfileBacked — enforced structurally by the mapper (`ExactProfileKeyOrNull`/`ExactProfileVersionOrNull` both derive from the identical `is ProfileBacked` match, so they can never disagree) — proven by `DualLaneSameStage_DifferentExactProfiles_BothRoundTripDistinctly` and `LegacyWeek_RoundTrips_AllProfileFieldsNull`.

## 8. LaneOrdinal persistence decision

**No new column.** Resolved directly from the real `FREQ.6D.4D` architecture §17 (re-quoted in §1 above), not from implementation convenience — this was a genuinely available, already-answered authority, so `FREQ6D4D_SPLIT_D_BLOCKED_ON_LANE_PERSISTENCE_AUTHORITY` does not apply.

## 9. Migration

`20260819111302_Phase10KFreq6D4D4CatalogPrescriptionProfileLineage`:
```csharp
Up:   AddColumn<string>("CatalogPrescriptionProfileKey", "TrainingDays", nullable: true);
      AddColumn<int>("CatalogPrescriptionProfileVersion", "TrainingDays", nullable: true);
Down: DropColumn("CatalogPrescriptionProfileKey", "TrainingDays");
      DropColumn("CatalogPrescriptionProfileVersion", "TrainingDays");
```
Nullable, additive, no default-value backfill, no destructive operation. Applied to the real dev database (`dotnet ef database update`) — `ALTER TABLE "TrainingDays" ADD "CatalogPrescriptionProfileKey" text;` / `ADD "CatalogPrescriptionProfileVersion" integer;`, confirmed via the real EF migration log. Model snapshot regenerated automatically, mechanical delta only (two new nullable properties).

## 10. Legacy-row compatibility

Directly DB-proven, not assumed: `ExistingLegacyPlan_ReadableAfterMigration_ProfileColumnsRemainNull` constructs a `TrainingDay` row exactly as a pre-Split-D writer would have (the two new columns simply never set), persists it, reloads with a fresh context, and confirms both remain `null` while `CatalogWorkoutDefinitionKey` reads back correctly — no read-time inference of a profile from `WorkoutDefinition`/`Stage`/`Role` occurs anywhere (confirmed by code review: `BuildCatalogTrainingDay` only ever copies, never infers).

## 11. Legacy mapping result

`LegacyWeek_RoundTrips_AllProfileFieldsNull` — a real, full `CatalogPlanConfirmationService.ConfirmAsync` call with `SourcePrescriptionProfileKey`/`Version` absent on every session (Legacy) round-trips with both new columns null on every persisted `TrainingDay` row.

## 12. ProfileBacked mapping result

`DualLaneSameStage_DifferentExactProfiles_BothRoundTripDistinctly` and `ProfileVersion1_Persisted_RemainsVersion1_RegardlessOfHypotheticalLaterVersion` — both real, full confirmation-path round-trips with real 5D profile identities (`INTERMEDIATE_5D_FOUNDATION_PRIMARY`/`_SECONDARY_CONTROLLED`, `INTERMEDIATE_5D_TAPER_PRIMARY`/`_SECONDARY_CONTROLLED`, `INTERMEDIATE_5D_BUILD_PRIMARY` — the real production profile identities from `FREQ.6D.4C.3`).

## 13. Profile round-trip result

Exact equality confirmed after save → dispose → fresh context → reload, no catalog re-resolution: `ProfileKey`, `ProfileVersion`, `StructuralRole` (via `CatalogWorkoutDefinitionKey` proxy — `TrainingDay` has no separate structural-role-only assertion point beyond `CatalogStructuralRole`, also asserted), `ProgressionStageKey`, `WorkoutDefinitionKey`, `WorkoutDefinitionVersion` — all directly asserted in `DualLaneSameStage_DifferentExactProfiles_BothRoundTripDistinctly`.

## 14. Dual-KEY round-trip result

Same test: two `KEY_SESSION` rows in one confirmed week, distinct exact profiles (`_PRIMARY` vs `_SECONDARY_CONTROLLED`), both persisted and reloaded with their own, independent, non-swapped, non-collapsed profile identity.

## 15. Same-stage/different-profile round-trip result

Same test, explicitly asserted as its own precondition: `lane0.CatalogProgressionStageKey == lane1.CatalogProgressionStageKey == "FOUNDATION_STAGE"`, yet `lane0.CatalogPrescriptionProfileKey != lane1.CatalogPrescriptionProfileKey` after reload — no stage-only reconstruction occurs.

## 16. Calendar move

`TrainingDay` (the Core confirmation-time snapshot this split extends) has no active "reschedule an existing confirmed session" mutation path in this codebase — confirmed by repository-wide search; the concept this phase's §14/§17 describe (a session moving date while preserving prescription identity) is owned by the separate `LongHorizonRollingSessionState`/`ScheduleRepairPersistenceService` subsystem (§4's architectural finding). That subsystem's `BuildReplacement` already copies `WorkoutKey`/`WorkoutVersion`/`SessionRole` verbatim on every move/repair (pre-existing, unmodified, unchanged behavior — proven still-green by the 192/192 `LongHorizon.Adaptation` regression, §32). No `TrainingDay`-specific calendar-move test was added, honestly, because no such mutation exists to test.

## 17. Not Today

Same architectural boundary as §16 — Not Today/reschedule decisions flow through `ScheduleRepairPersistenceService` against `LongHorizonRollingSessionState`, not `TrainingDay`. Unmodified this split; regression-proven green.

## 18. Substitution

`TrySubstituteFutureEasyAsync`/`BuildReplacement` unmodified. Per `FREQ.6D.1B`'s own resolved recommendation (§4), a substituted EASY stand-in correctly carries no profile lineage — because `LongHorizonRollingSessionState` has no such field, satisfying §16 of this phase's prompt ("must NOT falsely persist the original KEY's ProfileKey/Version") by construction, not by new code.

## 19. Original-vs-current lineage result

Already correctly distinguished by `WindowExecutionSummaryBuilder` (§4, confirmed unmodified and already correct): a root session's own `AdaptedFromId`-lineage chain is followed to its terminal outcome for completion accounting, while the root itself remains the one true "original scheduled expectation" — exactly the `OriginalScheduledSlotLineage` vs `CurrentExecutionPrescriptionLineage` distinction `FREQ.6D.1B` describes. No change was needed or made.

## 20. WindowExecutionSummary result

Zero changes (`WindowExecutionSummaryBuilder.cs` untouched, confirmed by diff review) — already N-role-generalized (`KeySessionExpectedCount`/`CompletedCount`, `EasyExpectedCount`/`CompletedCount`, `LongRunExpected`/`Completed`) since `Phase 10K-FREQ.4`, already lineage-correct for repair/substitution (§27 of the prompt: role-recovery information is already present, no query back to `TrainingDay`/profile catalog exists or was added).

## 21. Stale product-decision comment correction

`NextWindowLoadDecisionPolicy.cs`'s old doc comment claiming the 5-session case was "left for a future decision phase" is removed — replaced with a comment correctly stating this is policy **implementation** of an already-`FREQ.6`-approved, already-`FREQ.6D.1B`-fidelity-verified table (§20 of the prompt honored).

## 22. Five-session severity implementation

`DetermineFiveSessionLoadDecision`, dispatched only when `WindowExecutionSummary.ExpectedSessionCount == 5`: `0-1 → Reduce`, `2-3 → Maintain`, `4 → role-aware (OnlyEasyMissing ? Progress : Maintain)`, `5 → Progress`, plus `ValidateFiveSessionSummary` (fail-closed on any structurally inconsistent input) — exactly `FREQ.6D.1B §2`'s own proposed dispatch shape, reusing the real, already-generalized aggregate `KeySessionExpectedCount`/`CompletedCount` pair rather than inventing separate Key1/Key2 fields (both lanes are severity-equivalent per `FREQ.6 §5`, so every row's outcome is identical regardless of which lane was the sole miss — the aggregate representation is sufficient and keeps Adaptation lane-blind by construction, per `FREQ.6D.4D` architecture §21).

## 23. 24-row table result

**24/24 rows pass**, reproduced verbatim from `FREQ.6D.1B §3`'s full table (itself reproduced verbatim from `FREQ.6 §6`) — `Row_MatchesFrozenFreq6SeverityTable` theory, one `[InlineData]` per row, each asserted against the real `NextWindowLoadDecisionPolicy.Evaluate`. `All24Rows_ExactlyTwentyFourInlineDataCases` independently confirms via reflection that no row was silently dropped from the test file itself.

## 24. Count-4 role gate

`CountFour_SoleMissKeyLane0_Maintain`, `CountFour_SoleMissKeyLane1_Maintain`, `CountFour_SoleMissLong_Maintain`, `CountFour_SoleMissEasySlotA_Progress`, `CountFour_SoleMissEasySlotB_Progress_SameAggregateAsSlotA` (EASY1/EASY2 are symmetric per `FREQ.6 §5` — the real `WindowExecutionSummary` carries no per-slot EASY identity, so both "which EASY slot" cases are the identical aggregate input, honestly documented as such rather than fabricating a distinction the real type doesn't carry).

## 25. KEY lane severity equivalence

`KeyLane0AndLane1_AreSeverityEquivalent_BothProduceMaintainAtCountFour` — directly proves both count-4 KEY-miss variants produce the identical `Maintain` result, never `Progress` for either lane (§28 of the prompt: "Do NOT accidentally allow miss SecondaryControlled → Progress").

## 26. 3D/4D regression

**Zero regression, one real pre-existing test-data bug found and fixed.** `PlanAdaptationV1DecisionTests`'s entire existing 4-session matrix (rows 60-75+) passes unchanged (192/192 `LongHorizon.Adaptation` tests green, real DB). One genuine issue was found in `Freq4TwoKeyCardinalityGeneralizationTests.OnlyEasyMissingBranch_TwoKeyBothMissingOneEasyMissing_ThreeCompleted_DoesNotProgressAsPlanned`: its hand-built `WindowExecutionSummary` (`ExpectedSessionCount: 5`) had role fields (2 EASY + 1 LONG + 1 KEY = 4) that never actually summed to its own `EffectiveCompletedCount: 3` literal — a latent inconsistency from `Phase 10K-FREQ.4`, tolerated only because the pre-Split-D dispatch never validated this invariant for any session count. This split's new fail-closed `ValidateFiveSessionSummary` correctly rejected it. Fixed by correcting `EffectiveCompletedCount` to `4` (the value its own role fields always implied) — the test's originally-asserted outcome (`Maintain`) is unchanged and is now exactly `FREQ.6 §6` row 12/18 (count=4, sole miss one KEY lane), a **stronger**, table-verified assertion than the original. This is the same disclosed, expected-consequence pattern this engagement has used before (`FREQ.6D.4C.2`) for updating a pre-existing test whose assumptions a later, more authoritative phase supersedes.

## 27. Invalid-state failure semantics

Four dedicated tests: role-expected-sum mismatch, role-completed-sum mismatch, `KeySessionCompletedCount` exceeding `KeySessionExpectedCount`, and `LongRunCompleted` true while `LongRunExpected` false — all throw `AdaptationLineageInvalidException` (the existing, reused exception type — no new exception type was needed).

## 28. Future progression result

Zero change to `NextWindowAdaptationResult`'s shape or to any consumer of `Evaluate`'s return value (confirmed: `WeeklyLoadDecisionAggregator`/`ScheduleRepairRuntimeOrchestrator` call sites untouched, and their own tests — part of the 192/192 `LongHorizon.Adaptation` count — remain green). `SafetyReviewRequired` remains fully independent of the new 5-session `LoadDecision`, proven directly by `SafetyReviewRequired_IndependentOfFiveSessionLoadDecision`.

## 29. Public/API zero-delta

No public DTO exposes `CatalogPrescriptionProfileKey`/`Version`, `LaneOrdinal`, or any execution hash (confirmed: no `DTOs/` file was touched by this diff).

## 30. PlanCatalog zero-delta

Zero PlanCatalog file touched (confirmed via `git status`); full `PlanCatalog.Tests` suite re-run: **1,501/1,501 passed**, byte-identical to the `FREQ.6D.4D.3` baseline.

## 31. DB-backed tests

Real Postgres, real `docker-compose` dev database, confirmed live for the entire phase (§2). `Freq6D4DSplitDProfileLineagePersistenceTests`: **5/5 passed**, covering matrix items 1 (Legacy round-trip), 2-3 (ProfileBacked Primary/SecondaryControlled — both exercised via the dual-lane test), 4-5 (dual-KEY distinctness / same-stage-different-profile — same test), 10 (historical null-row readability), plus regeneration-stability and completion-preserves-lineage. Items 6-9 (calendar move / Not Today / substitution) are honestly reported as not applicable to `TrainingDay` — that mutation surface belongs to the separate `LongHorizonRollingSessionState` subsystem, which this split correctly did not touch (§16-18).

## 32. Adaptation test result

`Freq6D4DSplitDFiveSessionAdaptationSeverityTests`: **44/44 passed** (24-row table, count-4 role gate ×5, lane equivalence, 0-5/5 explicit, legacy-4-session preservation ×2, invalid-state ×4, safety-independence). Pre-existing `LongHorizon.Adaptation` namespace: **192/192 passed** (real DB), confirming zero legacy regression across the whole Adaptation subsystem, not just the policy file itself.

## 33. Broader RuntimeCatalog result

Full `RuntimeCatalog`-scoped regression, real Postgres, run twice (once before, once after the §26 test-data fix): first run **2,966 passed, 3 failed**; after the fix, **2,967 passed, 2 failed** — the remaining 2 failures are the identical, pre-existing, unrelated `78`-vs-`91` catalog-inventory-count mismatch (`PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe` and `PackagedPlanCatalogRealHttpSmokeTests.ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview`, which reads the same inventory count) — `PRE_EXISTING_TEST_BASELINE_FAILURE`, unchanged since `FREQ.6D.4D.1` first disclosed it, unrelated to this split's diff (no catalog file was touched).

## 34. Build

`dotnet build backend/RunningApp.sln` and `-c Release`: 0 warnings, 0 errors, both. `git diff --check`: clean (CRLF-normalization warnings only).

## 35. File attribution

| Category | Files |
|---|---|
| `PERSISTED_PROFILE_LINEAGE` | `TrainingDay.cs` |
| `EF_MIGRATION` | `20260819111302_...cs`, `.Designer.cs`, `AppDbContextModelSnapshot.cs` |
| `PLAN_CONFIRMATION_MAPPING` | `CatalogPlanConfirmationService.cs`, `CatalogPublicPreviewMaterializer.cs`, `PreparationRunwayPersistablePlanMapper.cs`, `GeneratedCatalogPlanPayload.cs`, `CatalogSessionPrescriptionSource.cs` (extension methods) |
| `REPAIR_LINEAGE` | None changed — confirmed already correct by construction (§4/§16-18) |
| `EXECUTION_SUMMARY` | None changed — confirmed already correct (§20) |
| `ADAPTATION_POLICY_IMPLEMENTATION` | `NextWindowLoadDecisionPolicy.cs` |
| `ADAPTATION_VALIDATION` | `NextWindowLoadDecisionPolicy.cs` (`ValidateFiveSessionSummary`, same file) |
| `TEST` | `Freq6D4DSplitDFiveSessionAdaptationSeverityTests.cs`, `Freq6D4DSplitDProfileLineagePersistenceTests.cs`, `Freq4TwoKeyCardinalityGeneralizationTests.cs` (corrected) |
| `DOCUMENTATION` | this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | None |

## 36. Split-E input contract

Per §47 of the prompt, all eleven conditions are met: (1) exact profile lineage survives DB persistence — §13; (2) Legacy rows backward-compatible — §10; (3) two KEY profiles cannot collapse after reload — §14/§15; (4) moves preserve same-session lineage — §16 (structurally, by an unmodified, already-correct mechanism); (5) substitutions distinguish original vs. replacement — §18; (6) `WindowExecutionSummary` correctly represents 5-session adherence — §20 (already true, unmodified); (7) the complete 24-state table is implemented — §23; (8) KEY0/KEY1 severity-equivalent — §25; (9) 4D/3D unchanged — §26; (10) future progression unchanged — §28; (11) no remaining catalog/profile/public product decision — confirmed, none surfaced. Split E's own remaining, disclosed work: real `RUN_LAYOUT_5D`/`TEN_K__5D__INTERMEDIATE` catalog authoring, public 5D support-matrix/routing activation, and wiring `PublishedTemplateBundleJsonReader` into `PlanCatalogBundleLoader.LoadCandidateAsync` for a real profile-backed candidate (the one remaining disclosed gap from Split B/C: no production caller sources a real published bundle yet, since none has ever been authored).

## 37. Final classification

**`FREQ6D4D_SPLIT_D_IMPLEMENTED_SPLIT_E_READY`**

Split D (durable profile lineage, schedule-repair lineage audit, five-session Adaptation policy implementation) is fully implemented and DB-verified: two new nullable `TrainingDay` columns persist exact `ProfileKey`/`Version` for `ProfileBacked` sessions through the real, live confirmation path (both standalone Core and combined runway+Core mappers); `LaneOrdinal`/execution-content persistence were correctly *not* added, per the real architecture's own already-resolved authority; the repair/substitution subsystem was audited and found already correct by construction, requiring no change; the complete, real, frozen `FREQ.6 §6` 24-row five-session severity table is implemented in `NextWindowLoadDecisionPolicy`, verified row-for-row, with legacy 4-session behavior byte-for-byte preserved and one genuine pre-existing test-data bug found and fixed along the way. Real PostgreSQL-backed verification succeeded for the entire phase — the environment gate was restored (Option A), not bypassed. `FREQ.6D.4D` overall dual-KEY production integration is **not** complete — Split E (real `RUN_LAYOUT_5D`/combination authoring, public activation, published-bundle file-discovery wiring) remains.
