# PHASE HM.2 — Intermediate × 4D × 14W Full Dark Vertical Slice

## 1. Frozen authorities consumed

This implementation consumes the approved 14-week `3/4/5/2` phase split; peak reference `43.0km`; peak band `[36,50]km`; weekly progression `0.07/0.08/2.5km`; calibration-only `25.0km` and 11 non-taper transitions; independently anchored taper multipliers `[0.70,0.43]`; long-run shares `0.30/0.36/0.33/0.40`; and the `19.0km` absolute ceiling. It also consumes HM.1.2/HM.1.3's exact stage order and `3 / (1+3) / (1+2+2) / 2` exposure occupancy. No new research or numeric training authority was introduced.

## 2. Files inspected

The implementation traced the prior HM reports, `MASTER_ROADMAP.md`, `PHASE_LEDGER.md`, catalog schemas and current 10K catalog artifacts, `V1CatalogPilotIdentityPolicy`, bundle/eligibility loading, phase allocation, stage allocation, workout binding, prescription context/session planning, volume/long-run planning, final validation, calendar materialization, and public-payload materialization. The most important executable references were `CatalogVolumeAndLongRunPlanner`, `VolumeSafetyPolicy`, `ProgressionStageAllocator`, `CatalogWorkoutBinder`, `CatalogSessionPrescriptionPlanner`, `CatalogFinalPrescribedPlanValidator`, and `CatalogPublicPreviewMaterializer`.

## 3. Files added/modified

Added real source-catalog artifacts: `HALF_MARATHON_MASTER v1`, `HALF_MARATHON_WORKOUT_PROGRESSION v1`, `HM_PACE v1`, `INTERMEDIATE_MODIFIER v8`, `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v4`, `PEAK_VOLUME_BANDS_V1 v9`, `APPSEL_RACE_PLAN_V1 v10`, and `HALF_MARATHON__4D__INTERMEDIATE v1`. Added `Hm2FullDarkVerticalSliceTests.cs`. Modified the shared policy/planner, prescription/final validators, session pace resolver, payload materializer, identity documentation, one catalog artifact-count assertion, and governance/report files. Existing candidates retain their exact dependency versions.

## 4. Generic mechanisms reused

The slice uses the existing candidate loader and internal-dry-run gate, core-horizon allocator, phase resolver, `RUN_LAYOUT_4D v1`, progression allocator with real `requires`/`fallbackStageKey`, dated skeleton materializer/validator, workout binder, shared volume and long-run algorithms, prescription context/session planner, finalizer/validator, race-date alignment verifier, and preview payload materializer. No `HalfMarathonIntermediate4DGenerator` or parallel planning engine exists.

## 5. Shared parameterizations required

`VolumeSafetyPolicy.HalfMarathonIntermediate4D` is an exact-cell named authority and the default planner dispatch recognizes only `HALF_MARATHON/INTERMEDIATE/4D`; other HM cells remain fail-closed. Final long-run validation reads the same policy's 40% cap. Context/final taper completeness validation recognizes exactly two HM activation sessions (or their approved fallbacks). The pace planner recognizes `HM_PACE` and derives its numeric pace only from normalized recent-race evidence. The payload materializer now records the candidate's actual workout-progression key instead of a hardcoded 10K key; for every existing 10K candidate the emitted string is unchanged.

The frozen `25.0km` value remains calibration-only. Missing/zero HM starting weekly volume fails closed and never borrows 10K's `24.0km` default.

## 6. `HALF_MARATHON_MASTER` shape

`HALF_MARATHON_MASTER v1` is `VALIDATED`, distance family `HALF_MARATHON`, exactly 14 minimum/default/maximum weeks, and supports only 4 runs/week. Its phases are exactly Foundation 3, Build 4, RaceSpecific 5, Taper 2. Foundation excludes quality; all later applicable phases use the already-supported family vocabulary. It references `HALF_MARATHON_WORKOUT_PROGRESSION v1` and the existing rule-pack identity.

## 7. `HALF_MARATHON_WORKOUT_PROGRESSION` binding

The single KEY lane is fixed to: Foundation aerobic base ×3; Build faster-than-HM/FARTLEK ×1 then threshold ×3; RaceSpecific threshold introduction ×1, HM development ×2, HM rehearsal ×2; Taper HM activation ×2. HM pace stages require realistic/challenging feasibility and independent `RECENT_RACE` pace evidence. RaceSpecific fallback is `THRESHOLD_TEMPO`; taper fallback is `EASY_STANDARD`, because the real catalog excludes threshold from Taper. `LONG_RUN_STANDARD v4` remains the only LONG_RUN binding; no quality-in-long-run was introduced.

## 8. Full 14W golden trace

Golden input: start `2026-08-03`, race `2026-11-08`, positive observed weekly volume `25.0km`, observed longest run `10.0km`, desired finish `5700s`, independent recent HM result `6000s`. Percentages below are actual rounded long-run shares.

| Week | Phase | Volume km | LR km | LR share | Workout stage | Bound KEY workout | Taper | Decision/provenance |
|---:|---|---:|---:|---:|---|---|---|---|
| 1 | Foundation | 25.0 | 8.5 | 34.00% | `FOUNDATION_AEROBIC_BASE` | `EASY_STANDARD v4` | — | observed starting-volume anchor |
| 2 | Foundation | 26.5 | 8.5 | 32.08% | `FOUNDATION_AEROBIC_BASE` | `EASY_STANDARD v4` | — | calibrated interpolation |
| 3 | Foundation | 28.5 | 9.5 | 33.33% | `FOUNDATION_AEROBIC_BASE` | `EASY_STANDARD v4` | — | calibrated interpolation |
| 4 | Build | 30.0 | 10.0 | 33.33% | `BUILD_FASTER_THAN_HM_INTRO` | `FARTLEK v4` | — | calibrated interpolation |
| 5 | Build | 31.5 | 10.5 | 33.33% | `BUILD_THRESHOLD_DEVELOPMENT` | `THRESHOLD_TEMPO v4` | — | calibrated interpolation |
| 6 | Build | 33.0 | 11.0 | 33.33% | `BUILD_THRESHOLD_DEVELOPMENT` | `THRESHOLD_TEMPO v4` | — | calibrated interpolation |
| 7 | Build | 35.0 | 11.5 | 32.86% | `BUILD_THRESHOLD_DEVELOPMENT` | `THRESHOLD_TEMPO v4` | — | calibrated interpolation |
| 8 | RaceSpecific | 36.5 | 12.0 | 32.88% | `RACE_SPECIFIC_HM_INTRODUCTION` | `THRESHOLD_TEMPO v4` | — | share ramp position 0 |
| 9 | RaceSpecific | 38.0 | 13.0 | 34.21% | `RACE_SPECIFIC_HM_DEVELOPMENT` | `HM_PACE v1` | — | recent-race-derived pace |
| 10 | RaceSpecific | 39.5 | 14.5 | 36.71% | `RACE_SPECIFIC_HM_DEVELOPMENT` | `HM_PACE v1` | — | recent-race-derived pace |
| 11 | RaceSpecific | 41.5 | 16.0 | 38.55% | `RACE_SPECIFIC_HM_REHEARSAL` | `HM_PACE v1` | — | recent-race-derived pace |
| 12 | RaceSpecific | 43.0 | 17.0 | 39.53% | `RACE_SPECIFIC_HM_REHEARSAL` | `HM_PACE v1` | — | share cap wins below 19km |
| 13 | Taper | 30.0 | 10.0 | 33.33% | `TAPER_HM_ACTIVATION` | `HM_PACE v1` | 1/2 | `43×0.70`, fixed anchor |
| 14 | Taper | 18.5 | 6.0 | 32.43% | `TAPER_HM_ACTIVATION` | `HM_PACE v1` | 2/2 | `43×0.43`, same fixed anchor |

## 9. Workout-binding proof

All 56 dated slots are bound by `CatalogWorkoutBinder` to exact source-catalog identities and versions. Every week has exactly one KEY, two EASY_SUPPORT, and one LONG_RUN. KEY identities follow the table above; EASY_SUPPORT is `EASY_STANDARD v4`; LONG_RUN is `LONG_RUN_STANDARD v4`. The real bound-plan validator and final prescribed-plan validator both pass.

## 10. Pace/fallback proof

The six golden `HM_PACE` sessions resolve to `RecentRaceDerived` at `round(6000 / 21.0975, 2)` seconds/km. A focused assertion proves this is not `round(5700 / 21.0975, 2)`; desired finish time is explicitly rejected as the source. With no independent exact pace evidence, the same real catalog progression binds four RaceSpecific exposures to `THRESHOLD_TEMPO` and two taper exposures to `EASY_STANDARD`, all with fallback provenance, and produces a valid final plan.

## 11. Calendar/materialization proof

The real calendar materializer assigns the requested Monday/Wednesday/Friday/Sunday 4D pattern with Sunday LONG_RUN, validates dated cardinality/spacing, crosses month boundaries, and aligns the last plan week to the `2026-11-08` race date. `CatalogPublicPreviewMaterializer` then produces a valid 14-week/4-day payload with `HALF_MARATHON` family, exact workout lineage, session distances, segments, pace representation, and `HALF_MARATHON_WORKOUT_PROGRESSION` provenance. This materialization is invoked directly by the dark test only; it is not public routing.

## 12. Long-run and taper proof

Every long run is at or below 40% after rounding tolerance and below 19.0km; the golden maximum is 17.0km, correctly demonstrating that 19km is not chased. RaceSpecific shares use the already-approved ramp mechanism. Taper weeks are `30.0` and `18.5km`, both independently derived from the fixed `43.0km` pre-taper anchor; no chained `30×0.43` computation occurs. The second taper KEY total is materially smaller due to the deeper whole-week taper, satisfying the non-full-load constraint without inventing a new dose number.

## 13. 10K zero-delta proof

Existing 10K candidates retain exact catalog references, numeric policies, taper lists, nullable long-run ceiling state, binding rules, calendar code, and public gates. Shared branches are additive exact HM identity checks. The materializer's generalized provenance evaluates to the same literal `TEN_K_WORKOUT_PROGRESSION_V1` for existing 10K candidates. The focused regression set includes real 10K 8–14-week materialization and prior policy-equivalence/taper/long-run suites: 118/118 passed.

## 14. Targeted tests

`Hm2FullDarkVerticalSliceTests` contains four passing tests: full 14-week real pipeline plus dark payload; independent-evidence-absent workout fallback; missing-volume fail-closed/no-10K-default; and public/Runway gate closure. The catalog combination validates with zero issues. The application builds with zero warnings/errors attributable to this change.

## 15. Regression results

- `validate-combination HALF_MARATHON__4D__INTERMEDIATE --version 1 --json`: PASS, zero issues.
- Focused HM + shared 10K integration regression: 118 passed, 0 failed.
- `PlanCatalog.Tests`: 1510 passed, 0 failed.
- Full backend integration attempts: exceeded both 300-second and 900-second command windows without emitting a final result; no failure output was produced before either timeout. This does not replace the completed focused regression result above and is recorded rather than misreported as a pass.

## 16. Remaining HM gaps

Missing/zero starting weekly-volume support remains deliberately unavailable because 25km is calibration-only and no HM fallback authority exists. Other HM levels, frequencies, 10–13/15–16-week Core occupancy, Preparation Runway, Long Horizon, quality-in-long-run, public activation, confirmation/persistence activation, and cross-distance current-fitness pace conversion remain outside this slice. None is required for this exact observed-readiness 14W dark target.

## 17. Public-routing state

HM remains dark. `IsSupportedIdentity` and `IsSupportedPreparationRunwayIdentity` both return false for `HALF_MARATHON × INTERMEDIATE × 4D`; only the explicit distance-aware internal resolver returns the candidate. No controller, DI public registration, selector, or persistence route was widened.

## 18. Recommended next phase

Recommended `HM.3`: observe/review this exact dark slice and decide the smallest next boundary—preferably the missing/zero HM starting-volume policy or explicit public-readiness audit—before widening horizons, frequencies, or public routing. Do not combine public activation with new numeric authority.

## Final classification

`HM_INTERMEDIATE_4D_14W_FULL_DARK_VERTICAL_SLICE_COMPLETE`
