# Phase 10K-GEN.3A — Intermediate 3D Core Implementation

## 1. Files inspected

GEN.2A/2B decision documents; catalog layouts/combinations/template/rule-pack/peak policy; dynamic Core skeleton/binding/volume/session/calendar pipeline; 4D volume/allocation policies; taper policy; calendar and structural validators; eligibility/readiness types; catalog and backend tests.

## 2. Files changed

- Added `run-layout-3d.v1.json` and `ten-k-3d-intermediate.v1.json`.
- Added `V1ThreeDayMissingReadinessStartingVolumePolicy.cs`, `V1ThreeDaySessionVolumeAllocationPolicy.cs`, and `Gen3AThreeDayCoreTests.cs`.
- Updated volume policy/planner/exceptions, session planner, calendar materializer/validator, `VolumeProgressionVerifier`, and focused regression tests.
- Updated two catalog tests whose formerly global single-family assumptions were invalidated by the approved 3D composition.

## 3. New/updated catalog artifacts

`RUN_LAYOUT_3D v1` is DRAFT with ordered KEY/EASY/LONG. `TEN_K__3D__INTERMEDIATE v1` is a DRAFT compatibility manifest referencing `TEN_K_MASTER v6`, `INTERMEDIATE_MODIFIER v6`, `APPSEL_RACE_PLAN_V1 v4`, and the new layout. It owns no weeks or progression timeline. Existing peak policy v3 already contains 22–32 km and was not numerically changed.

## 4. RunLayout implementation

The generic loader resolves three slots and the existing materializer emits deterministic `KEY_SESSION_1`, `EASY_SUPPORT_1`, `LONG_RUN_1`. No 3D branch was added to week construction.

## 5. Combination-manifest implementation

Composition is manifest-only and remains rollout-disabled. No canonical template or workout is copied.

## 6. Starting-volume implementation

The versioned 3D policy preserves real positive user evidence; missing resolves to 16 km and explicit zero to 12 km with explicit 3D provenance.

## 7. Progression implementation

`VolumeSafetyPolicy.ThreeDayIntermediate` carries 7% preferred, 8% hard and 2.0 km absolute values. The 3D planner grows sequentially with 0.5 km rounding, then independently enforces both hard constraints. Below-band peaks remain valid; the upper band is a clamp.

## 8. VolumeProgressionVerifier correction

Absolute violation is now `changeKm > absolute cap`, independent of ratio. Focused tests prove ratio-pass/absolute-fail and ratio-fail/absolute-pass both fail. Existing real 4D volume/verifier regression tests pass in the focused suite.

## 9. Session allocation implementation

The separate 3D policy targets 35/25/40, enforces 4/3/5 km minima and 12 km floor, reconciles in 0.5 km steps, rejects hard violations, minimizes absolute target-distance deviation, and uses structural index as equal-error tie-break. The 4D allocator remains separate.

## 10. Long-run implementation

The selected 40% share, 38–42% preferred range and 42% hard cap are provided by the 3D volume policy. Allocation revalidates the post-round 42% cap. Existing recent-longest-run handling remains in the shared planner; no universal session 10% rule was added.

## 11. Calendar generalization

Calendar materialization accepts resolved 3D/4D cardinality. Preferred-day count must equal skeleton `DaysPerWeek`; expected EASY count derives as `DaysPerWeek - 2`. Existing deterministic KEY selection and two-day same/cross-week KEY↔LONG separation remain unchanged.

## 12. Validator generalization

Dated validation derives session count from preferred-day count and EASY count from resolved cardinality. Explicit 4D-only policies were not renamed or made falsely generic.

## 13. Eligibility routing

After the 3D weekly curve is produced, taper volume below 12 km throws typed `ThreeDayCoreProductIneligibleException` with reason `THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT` and explicit `PRODUCT_INELIGIBLE` semantics. The gate is necessary, not sufficient.

## 14. Dynamic Core authority proof

Focused tests load the real 3D catalog candidate and use `DynamicCoreWeekSkeletonOrchestrator` for 8–14 weeks. Every week has exactly one KEY, one EASY and one LONG and preserves the canonical four phases. No 3D orchestrator exists.

## 15. 4D regression analysis

The 4D policy values, allocation, template, public routing and persistence were not intentionally changed. The only shared semantic change is independent absolute-cap verification, which enforces an already-approved hard rule. Focused calendar, volume, session, dynamic-volume and verifier regression coverage passed. No golden fixture was updated.

## 16. Test matrix/results

- Application build: PASS, 0 warnings / 0 errors.
- New skeleton + verifier focused set: PASS 25/25.
- Expanded relevant backend set (calendar, dated validator, 4D volume, session prescription, dynamic volume, GEN.3A, verifier): PASS 251/251.
- Plan-catalog full suite: PASS 1,250/1,250.
- Backend integration full suite: INCOMPLETE. Two attempts timed out after approximately 120 and 300 seconds without a test result; stale testhost processes from the timed-out run caused an intermediate DLL lock and were terminated before the focused suite was rerun successfully.

The required explicit eligibility 8–14 and full 12–32 end-to-end allocation matrices are not yet represented as dedicated committed tests, although the underlying policy and skeleton paths are implemented. This prevents claiming the phase completion standard.

## 17. Catalog validation results

All 1,250 catalog tests pass. The new combination is graph-valid, manifest-only, references existing authorities, and does not duplicate plan data. The DRAFT status preserves support/rollout separation.

## 18. Rollout state

3D remains dark/gated. No public identity gate, controller route, rollout flag or persistence schema was activated.

## 19. Known deferred scope

- Diagnose the full integration-suite hang and obtain an exact full-suite count.
- Add explicit 8–14 taper eligibility regression tests through the real volume pipeline.
- Add the required 12–32 session allocation matrix and broader 3D calendar-pattern tests.
- Complete end-to-end dynamic binding/volume/session/calendar proof for the real 3D candidate.
- Run public/persistence compatibility tests without activating rollout.
- RunLayout expansion beyond 3D/4D, Runway, LongHorizon, adaptation, other levels/frequencies and exact-12 fixed 4D retirement remain out of scope.

## 20. Final classification

```text
10K_GEN_3A_INTERMEDIATE_3D_CORE_IMPLEMENTATION_BLOCKED
```

Reason: implementation work is materially advanced and focused/catalog validation passes, but the prompt's mandatory full backend suite and several explicit regression matrices are not complete. No approved GEN.2 policy was changed to conceal the gap.
