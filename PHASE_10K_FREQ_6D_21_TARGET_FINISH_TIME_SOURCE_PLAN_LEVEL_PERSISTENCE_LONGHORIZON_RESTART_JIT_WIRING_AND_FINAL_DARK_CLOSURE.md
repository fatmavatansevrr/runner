# Phase 10K-FREQ.6D.21 — 10K Target-Finish-Time Source Plan-Level Persistence, LongHorizon Restart/JIT Wiring & Final Dark Closure

**Phase type:** IMPLEMENTATION + SCHEMA MIGRATION + REAL POSTGRESQL VERIFICATION
**Parent phase:** FREQ.6D.20 (`DONE`, `TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_AUTHORITY_APPROVED`)

## 0. Governance verification

`PHASE_LEDGER.md` row 100 confirms FREQ.6D.20's exact classification and its 11 frozen decisions (semantic provenance, plan-level owner, durable persistence, one new nullable field, mandatory for new plans, `UNKNOWN_LEGACY` for historical rows, never backfilled, restart reads persisted provenance, no rolling-state duplication, must flow into Core JIT/`GOAL_PACE_TEN_K`, never reconstructed from seconds or current defaults) — all implemented exactly, none reopened. `MASTER_ROADMAP.md` recorded `NEXT_PHASE_NOT_YET_SCHEDULED`; searched and confirmed `FREQ.6D.21` unreserved; scheduled and committed (`96da03b`); proceeded directly into implementation.

## 1-8. Implementation

**Entity** (`backend/RunningApp.Domain/Entities/TrainingPlan.cs`): added `public TargetFinishTimeSource? TargetFinishTimeSource { get; set; }` alongside `TargetFinishTimeSeconds`. Discovered this repository already applies a **global** `SnakeCaseEnumConverter<TEnum>` to every enum property in `AppDbContext.OnModelCreating` (no per-property `HasConversion<string>()` needed) — confirmed by inspecting existing migrations, where `TrainingPlan.GoalType`/`GoalDistance` are stored as `text` despite being plain enum properties in the entity. This is a stronger, more idiomatic answer than FREQ.6D.20's own speculative "string" framing: the field is a genuine nullable enum property, not a hand-managed string, and round-trips automatically through the repository's existing convention.

**Migration**: `20260827090333_AddTargetFinishTimeSourceToTrainingPlan` — exactly one nullable `text` column on `TrainingPlans`, no other schema change, `Up`/`Down` both trivial (`AddColumn`/`DropColumn`), zero backfill SQL. Applied to the real configured PostgreSQL database (`antigravity_dev`) via `dotnet ef database update`; confirmed via the executed SQL log (`ALTER TABLE "TrainingPlans" ADD "TargetFinishTimeSource" text;`, no `UPDATE` statement anywhere).

**Ordinary confirmation** (`CatalogPlanConfirmationService.BuildCatalogTrainingPlan`, `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs:583`): added `TargetFinishTimeSource = snapshot.NormalizedInput.TargetFinishTimeSource` immediately beside the existing `TargetFinishTimeSeconds` assignment — the exact FREQ.6D.20-identified loss point. This single service is the sole confirmation path for **every** non-LongHorizon plan (Core-only 8-14, Runway 15-20 alike), so no separate fix was needed for those horizons (§9 objective, §36 manifest item).

**LongHorizon confirmation** (`LongHorizonPublicPlanService.BuildTrainingPlan`, `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/PublicPreview/LongHorizonPublicPlanService.cs:326`): added `TargetFinishTimeSource = snapshot.Command.TargetFinishTimeSource` — `RacePlanPreviewCommand` (the shared command type both the ordinary and LongHorizon confirm paths already consume) already carried this field; it was simply never copied here either.

**Restart/JIT wiring** (`LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`, `backend/RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs`): threaded `planRow.TargetFinishTimeSeconds`/`planRow.TargetFinishTimeSource` — read from the `TrainingPlan` row this method **already** loads at the top of the method (`planRow`, no new query) — into `ContinueJitCompositionAsync`'s existing (FREQ.6D.19-added) `targetFinishTimeSeconds`/`targetFinishTimeSource` parameters. This is the exact "shortest semantic path" the phase's own §20 specified: `TrainingPlan` columns → restart continuation context → JIT input → `ContinueJitCompositionAsync` → Core prescription context → `GOAL_PACE_TEN_K`, with zero redundant recomputation layers and zero rolling-state duplication (`LongHorizonRollingPlanState`/`LongHorizonRollingSessionState` were not touched).

## 9-24. Test verification

**`CatalogPlanConfirmationServiceTests.cs`** (extended, in-memory EF, matching this file's own established convention): 4 new tests — `ConfirmAsync_ProductAverageSource_PersistsSourceAlongsideSeconds`, `ConfirmAsync_UserDefinedSource_PersistsSourceAlongsideSeconds`, `ConfirmAsync_SameNumericTarget_DifferentSource_RemainsDistinctAfterConfirmation` (the mandatory §14 regression: two independently-confirmed plans sharing the identical `TargetFinishTimeSeconds=3480` retain `ProductAverage` and `UserDefined` respectively after confirmation — proving source is never derived from the number), `ConfirmAsync_NoTargetTimeRequested_BothSecondsAndSourceRemainNull`. All 4 pass; 38/38 total in this file.

**`Freq6D21TargetFinishTimeSourceRestartTests.cs`** (new, real PostgreSQL, drives the real `LongHorizonRollingWindowActivationService` directly — the public HTTP preview/confirm surface remains correctly gated to 4D-only, `LONG_HORIZON_PILOT_UNSUPPORTED`, and was **not** touched per §43):
- `ProductAverage_OrganicRestartThroughCore_SucceedsAndPreservesSource` — **the principal closure proof (§30)**: a real Intermediate×5D `TrainingPlan` row, persisted with `TargetFinishTimeSource=ProductAverage`, is driven through the real production continuation service (fresh `AppDbContext`, fresh service instance, every call) and reaches the first real, organically-materialized Core window (weeks 10-13) whose full 12-week Core plan includes a `GOAL_PACE_TEN_K` workout — succeeding for the first time ever for a real (non-dark-test-supplied) 5D plan. Verified 2 KEY (lanes 0/1, both distinct) + source unchanged after the full continuation.
- `UserDefined_WithoutIndependentEvidence_FailsClosedTyped_NeverReclassified` — control (§13/§32): a genuine `UserDefined` target with no recent-race evidence correctly still fails closed (typed `LongHorizonContinuationBlockedException`, not a 500) — this is a real, unrelated, pre-existing `GoalFeasibilityResolver` rule, unaffected by this phase; critically, `TargetFinishTimeSource` remains `UserDefined` after the failed attempt — never silently reclassified to `ProductAverage` to make the failure disappear.
- `HistoricalNullSource_NeverInferred_FailsClosedTypedAtCore` — historical-legacy control (§15/§17): a real confirmed plan with `TargetFinishTimeSource` nulled after confirmation (simulating `UNKNOWN_LEGACY`) fails closed identically (typed exception), and the source remains null afterward — never inferred as a side effect of the failed attempt.
- `RollingStateEntities_DoNotDuplicateTargetFinishTimeSource` — governance guard (§35): reflection-asserts neither `LongHorizonRollingPlanState` nor `LongHorizonRollingSessionState` gained a `TargetFinishTimeSource` property.

All 4 pass.

Discovery during test construction: Core generation (and therefore `GOAL_PACE_TEN_K` validation) is attempted eagerly on the very **first** Runway-entry continuation call (matching FREQ.6D.19's own reconnaissance — the full 12-week Core plan is generated upfront, not deferred to the physical Core weeks), not on the third call as initially assumed; the UserDefined/historical-null tests were adjusted to expect the typed failure on that first call. This is a pre-existing behavior, not something this phase changed.

## 25-41. Regression scope

Re-ran FREQ.6D.19's full organic GE→Runway→Core / dual-KEY / repair suite (unaffected by any of this phase's changes — all additive: a new column, two new field assignments, two new-but-defaulted method parameters now actually populated) — all still passing, confirming:
- First Core cardinality (2K+2E+1L), lane 0/1 identity, ProfileBacked lineage, `PublishedBundleReleaseVersion`/`ExecutionPrescriptionIndex` resolution — unchanged, still green.
- Secondary-KEY repair, GE KEY repair, repeated-EASY repair regressions — unchanged, still green (this phase touched no repair code).
- Shared clamp regressions (4D/22, 5D/22, 4D/24 edge), full 21-52 matrix, persisted Adaptation, 4D LongHorizon, 5D Core/Runway — all covered by the unchanged full-suite pass count (see below); no isolated re-run of each was needed beyond the full suite, since none of this phase's five changed files intersect with any of that logic (confirmed by the file list in §1-8: only `TrainingPlan`, the two confirm services, the one JIT-wiring call site, and their own tests were touched).
- Missing/zero readiness → `PRODUCT_INELIGIBLE`: unaffected, not reopened.
- Public 21+ closed: confirmed still closed — the public LongHorizon preview endpoint rejected the 5D request in this phase's own test attempt with `LONG_HORIZON_PILOT_UNSUPPORTED`, proving the gate is untouched.
- Unsupported neighbors (Beginner×5D, Advanced×5D, 6D/7D): untouched, no widening.

## 42. Full regression results

- New tests: `CatalogPlanConfirmationServiceTests` 38/38 (34 existing + 4 new); `Freq6D21TargetFinishTimeSourceRestartTests` 4/4 (all new).
- Full LongHorizon integration-test subset: **1253/1253** (unchanged from FREQ.6D.19/20 baseline — this phase's own new tests are not named with "LongHorizon" and are verified separately above).
- Full `RunningApp.IntegrationTests`: **3894/3896** — same 2 pre-existing failures durably documented since FREQ.6D.17/18/19 (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`), unrelated to this phase; 3886 (FREQ.6D.19 baseline) + 8 new (4 + 4) = 3894, confirming zero new failures.
- `PlanCatalog.Tests`: **1510/1510**.
- Debug build: clean, 0 errors. Release build: clean, 0 errors. `git diff --check`: exit 0 (line-ending warnings only).

## 43. STOP-condition audit

No new `TargetFinishTimeSource` value was needed or added (only `ProductAverage`/`UserDefined`, exactly as FREQ.6D.20 approved). No new target-time numeric value. No derive-on-restart — every read is a verbatim column read. No `ProductAverage` recomputation at restart — `CanonicalTargetFinishTimePolicy` was not consulted anywhere in the restart path. No rolling-state duplication (verified by test). No LongHorizon architecture redesign, no GE/Runway/Core structure change, no catalog change, no public 21-52 activation (the public gate was confirmed still rejecting 5D). No schema beyond the single FREQ.6D.20-approved column. No STOP condition fired.

## 44. Success boundary (§54) — item by item

A-F: verified (persist, zero backfill, ordinary Core/Runway confirm persists, LongHorizon confirm persists, fresh-reload round-trip, same-seconds-different-source distinct). G: verified (no inference, `CanonicalTargetFinishTimePolicy` not consulted at restart). H: verified (`UNKNOWN_LEGACY` null after failed attempt). I: verified (typed `LongHorizonContinuationBlockedException`, not a 500). J: verified (`ProductAverage_OrganicRestartThroughCore` test). K: verified (the same test — `GOAL_PACE_TEN_K` succeeds after real restart for the first time). L: verified (2K+2E+1L, distinct lanes, unaffected). M: verified by regression parity (FREQ.6D.19's own repair test unaffected). N: verified by regression parity (FREQ.6D.18's own persisted-Adaptation tests unaffected). O: verified by regression parity (full-suite pass count, no reduction). P: verified by regression parity (shared clamp tests untouched, unaffected files). Q: verified (public gate still rejects 5D).

## 45. Final classification

Every item in the success boundary closed with no disclosed remainder — the one gap FREQ.6D.19/20 identified (a real, restarted 5D LongHorizon plan cannot supply `TargetFinishTimeSource` for `GOAL_PACE_TEN_K`) is now closed for real, non-dark-test-supplied plans, proven via `ProductAverage_OrganicRestartThroughCore_SucceedsAndPreservesSource`.

**`TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_IMPLEMENTED_AND_VERIFIED`**

Since this closes the last disclosed item from the FREQ.6D.18→19→20→21 arc (persisted adaptation, persisted repair, organic GE→Runway→Core, dual-KEY lineage/repair, and now real target-time-source-driven Core generation), the accumulated Intermediate×5D LongHorizon implementation and dark-verification capability is now complete:

**`INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`**

This is dark-verification completeness only — not public activation, per this phase's own explicit instruction (§55: "Do NOT say publicly activated").

## 46-49. Governance

- `PHASE_LEDGER.md`: row 101 appended, closing this classification.
- `MASTER_ROADMAP.md`: updated; `TargetFinishTimeSource` persistence marked COMPLETE; Intermediate×5D LongHorizon dark implementation marked COMPLETE; next phase recorded as `NEXT_PHASE_NOT_YET_SCHEDULED` — the final Intermediate×5D LongHorizon real public HTTP/PostgreSQL verification & public activation phase (§58), explicitly not scheduled or executed here.
- Push gate: recalculated below.
