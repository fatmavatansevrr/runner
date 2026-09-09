# PHASE HM.2.1 — HM MASTER AUTHORITY AND REGRESSION CLOSURE

## 1. Outcome

`HM_2_POST_VERTICAL_SLICE_AUTHORITY_AND_REGRESSION_CLOSED`

## 2. Authority finding

`HALF_MARATHON_MASTER v1` owns canonical CoreCycle authority. Its former `14/14/14` values incorrectly narrowed canonical product authority to the only runtime-supported slice.

The governing authority is explicit in `HM_21K_Claude_Handoff_and_Build_Plan.md §4.1` and was adopted in `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md`:

- minimum: 10 weeks
- preferred/default: 14 weeks
- maximum: 16 weeks

The effective restriction was therefore temporary runtime support, but it had been encoded as canonical authority. The master is corrected to `10/14/16`.

## 3. Phase-allocation boundary

Only the preferred 14-week allocation is frozen: Foundation 3, Build 4, RaceSpecific 5, Taper 2. Exact allocations for 10–13 and 15–16 remain deferred to WAVE HM-D and were not invented.

`PlanTemplateDefinition` now has the additive `phasesDefineDefaultCycleOnly` marker. It defaults to `false` and is omitted during serialization when false, preserving all existing templates and published hashes. HM master sets it to `true`. `PlanTemplateValidator` still requires preferred-phase sum = default weeks, while no longer falsely requiring default-only phase rows to cover canonical outer bounds.

## 4. Runtime fail-closed proof

No non-14W horizon was activated. The existing `CatalogPhaseAllocationResolver` remains the gate: HM's authored phase minimum, preferred, and maximum sums are all 14.

For 10, 11, 12, 13, 15, and 16 weeks, the real resolver returns infeasible with no phases and the real dynamic Core pipeline throws `DynamicCoreWeekSkeletonInfeasibleException` before materialization. Public and Preparation Runway gates remain closed.

## 5. Fourteen-week zero-delta proof

The HM.2 golden test now also asserts the real loader exposes `PlanCatalogCoreCycle(10,14,16)`. The same production-owned pipeline still produces 14 weeks, 56 sessions, phase shape 3/4/5/2, 43.0 km peak volume, 17.0 km maximum long run, 30.0/18.5 km taper, the same independent-fitness HM pace behavior and approved fallbacks, and a valid dated/materialized preview.

- HM.2/HM.2.1 focused: 10/10 passed.
- Related obsolete-regression/packaging closure set: 34/34 passed.
- Release solution build: 0 errors.
- Release packaged-catalog smoke: 1/1 passed.

## 6. Catalog regression and hash preservation

An initial default-true marker design caused old templates to reserialize with a new field; full catalog regression caught 79 historical hash mismatches. That design was discarded. The final default-false, write-only-when-true shape preserves old serialized content.

Final `PlanCatalog.Tests`: 1510/1510 passed, including cross-release hash consistency.

## 7. Monolithic integration regression

The suite used a 55-minute command timeout. No timeout is treated as a pass.

The first completed run took 41m06s: 4505 passed, 7 failed, 4512 total. Four failures matched baseline; three were HM.2-related obsolete assertions: two tests still treated dark HM Intermediate×4D as unsupported, and the deployment inventory still expected 131 rather than 139 files after HM.2's eight documents. The test intent was preserved by moving unsupported-neighbor coverage to HM Advanced×4D and advancing the exact inventory by eight.

A subsequent run exposed a stale pre-built Release package still containing 131 files. After a successful Release build, its package smoke test passed.

The final completed run took 41m07s: **4508 passed, 4 failed, 4512 total**. The four failures exactly match the documented baseline:

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
3. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`
4. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` (documented date-coincidental baseline)

Result: `backend/RunningApp.IntegrationTests/TestResults/hm21_full_regression_verified.trx`.

## 8. Scope controls

- No allocation table was authored for 10–13 or 15–16 weeks.
- No dynamic HM horizon or public/Runway/LongHorizon route was activated.
- No baseline failure was fixed.
- No unrelated production behavior or numeric authority changed.

## 9. Final classification

`HM_2_POST_VERTICAL_SLICE_AUTHORITY_AND_REGRESSION_CLOSED`
