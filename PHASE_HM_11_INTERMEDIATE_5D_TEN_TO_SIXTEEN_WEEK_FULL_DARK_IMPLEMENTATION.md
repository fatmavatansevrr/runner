# PHASE HM.11 — Half-Marathon Intermediate 5D 10–16W Full Dark Implementation

Implementation phase. The frozen HM.9 structural authority and HM.10 numeric authority are now materialized through the existing generic dynamic-Core pipeline. No `HalfMarathonIntermediate5DGenerator`, horizon table, public activation, Preparation Runway, or LongHorizon path was added.

## 1. HM.9 authority consumed

`RUN_LAYOUT_5D` remains `KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN`. Lane 0 is the unchanged primary HM progression. Lane 1 is phase-aware: Foundation/Taper bind easy-equivalent content; Build/RaceSpecific prefer `FARTLEK`; an ineligible preferred secondary stage falls back to `EASY_STANDARD`. The weekly true-hard ceiling is two, true-hard adjacency is prohibited, and the calendar enforces at least two calendar days between true-hard sessions. `LONG_RUN_STANDARD` remains controlled and contains no quality segment.

## 2. HM.10 authority consumed

| Authority | Encoded value |
|---|---:|
| Peak-volume band | 44–58 km/week |
| Selected peak reference | 51.0 km/week |
| Preferred / hard weekly growth | 0.07 / 0.08 |
| Absolute weekly increase cap | 2.5 km |
| Calibration-only start | 29.5 km/week |
| Golden non-taper transitions | 11 |
| LR preferred min / max | 0.28 / 0.36 |
| LR selected / hard cap | 0.28 / 0.36 |
| Absolute peak LR ceiling | 19.0 km |
| Starting absolute LR cap | 12.0 km |
| Taper multipliers | 0.70 / 0.43, non-chained |
| Selected peak is a ceiling | `true` |

No value was re-derived in HM.11. The 29.5 km value is calibration data only and is rejected as a missing/zero-readiness substitute.

## 3. Catalog lifecycle/version decision

All involved source documents are `VALIDATED`, not `PUBLISHED`. The shared `HALF_MARATHON_MASTER v1` and `HALF_MARATHON_WORKOUT_PROGRESSION v1` remain the exact 3D/4D authority and were not content-mutated. A new master/progression pair was required because changing the shared flat v1 progression to two lanes would change the existing single-lane consumers:

- `HALF_MARATHON_MASTER v2`: canonical Core `10/14/16`, `supportedRunsPerWeek=[5]`, progression v2.
- `HALF_MARATHON_WORKOUT_PROGRESSION v2`: lane 0 is a literal structural copy of v1; lane 1 carries HM.9.
- `FARTLEK v6`: additive version because v4 is Build-only while HM.9 also requires RaceSpecific. Family, prescription modes, distance-accounting mode, and all four v4 components/dose semantics are unchanged; only phase eligibility is widened and legacy-default resolution is explicitly disabled.
- `INTERMEDIATE_MODIFIER v9`: additive exact-version closure including FARTLEK v6 and the already-existing two-hard-session progression modifier v3.
- `HALF_MARATHON__5D__INTERMEDIATE v1`: new combination.
- `PEAK_VOLUME_BANDS_V1 v9`: still VALIDATED; one additive 5D row was appended in place, matching the existing HM.8 lifecycle precedent.

No immutable historical artifact or expected hash was rewritten.

## 4. Files/artifacts changed

Production code:

- `CatalogPrescriptionContextBuilder.cs`, `PrescriptionContracts.cs`, `CatalogFinalPrescribedPlanValidator.cs`: lane identity and dual-lane HM taper completeness.
- `CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`, `HalfMarathonIntermediate4DTaperVolumePolicy.cs`: typed 5D policy, growth/LR/taper authority, calibration fail-closed behavior, and opt-in starting-LR cap.
- `V1CatalogPilotIdentityPolicy.cs`: dark-only distance-aware 5D candidate resolution.
- `DynamicCoreWorkoutBindingOrchestrator.cs`, `CatalogCalendarAssignmentContracts.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`: generic workout-derived calendar hardness seam.
- `V1ThreeDaySessionVolumeAllocationPolicy.cs`: configured-cap error text only.

Catalog:

- New: `templates/half-marathon-master.v2.json`.
- New: `workout-progressions/half-marathon-workout-progression.v2.json`.
- New: `combinations/half-marathon-5d-intermediate.v1.json`.
- New: `workouts/fartlek.v6.json`.
- New: `level-modifiers/intermediate-modifier.v9.json`.
- Additive validated-row edit: `policies/peak-volume-bands.v9.json`.

Tests:

- New `Hm11Intermediate5DFullDarkVerticalSliceTests.cs` (19 cases).
- One generic calendar-hardness capability test.
- One configured-cap cosmetic-message test.
- HM identity/gate expectations updated by net +1 case.
- Catalog inventory/workout counts updated for the six additive catalog artifacts (five new files plus one row in an existing file).

## 5. Secondary-lane implementation

The existing `lanes`/`laneOrdinal` schema is reused. Lane 1 has one effective phase stage at a time:

| Phase | Preferred stage/workout | Fallback | Product semantic |
|---|---|---|---|
| Foundation | `FOUNDATION_HM_SECONDARY_EASY` / `EASY_STANDARD v4` | none | Easy-equivalent |
| Build | `BUILD_HM_SECONDARY_FARTLEK` / `FARTLEK v6` | `BUILD_HM_SECONDARY_EASY_FALLBACK` / `EASY_STANDARD v4` | Light quality |
| RaceSpecific | `RACE_SPECIFIC_HM_SECONDARY_FARTLEK` / `FARTLEK v6` | `RACE_SPECIFIC_HM_SECONDARY_EASY_FALLBACK` / `EASY_STANDARD v4` | Light quality |
| Taper | `TAPER_HM_SECONDARY_EASY` / `EASY_STANDARD v4` | none | Easy-equivalent |

The secondary FARTLEK requires evaluated goal feasibility but does not require an exact pace source. Thus product-average/no-independent-fitness-evidence plans can retain effort-only FARTLEK, while a genuinely ineligible/unevaluated goal condition takes the catalog fallback.

## 6. Calendar capability audit

Result: `GENERIC_CALENDAR_HARDNESS_CAPABILITY_GAP`.

The pre-HM.11 calendar ran before workout binding and received only structural roles. Both lanes therefore appeared as `KEY_SESSION`, so it could not distinguish a Foundation/Taper easy-equivalent KEY from a true-hard KEY. Role names alone were insufficient to implement HM.9 exactly.

## 7. Calendar capability fix

The smallest generic seam was added without a new catalog field:

1. After stage allocation/fallback, the binding orchestrator resolves each lane/week's already-selected workout definition.
2. Existing workout `family` metadata maps `EASY`/`LONG_RUN` to `EasyEquivalent`; other families map conservatively to `TrueHard`; missing/invalid metadata maps to fail-safe `Unknown`, treated as hard.
3. The historical all-KEY placement algorithm remains the first pass. Therefore every already-feasible legacy placement is returned unchanged by construction.
4. Only if that pass is infeasible, or violates cross-week true-hard separation, a hardness-aware deterministic retry is used.
5. The dated validator independently checks same-week and cross-week true-hard spacing.

The focused capability test uses a 5D Monday–Friday set with Thursday long run. The historical all-KEY rule is infeasible; the same skeleton succeeds only when one KEY is true-hard and the other easy-equivalent. No distance, level, frequency, lane-2, or weekday special case exists in production code.

## 8. Typed 5D policy dispatch

`VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek` now resolves 3D, 4D, or 5D and throws for every other frequency. Both the volume planner and final LR-cap validator use that authority. The named `HalfMarathonIntermediate5D` policy is the single owner of 51/29.5/2.5/LR/taper numbers; no scattered 5D numeric branch or per-cell generator was introduced.

## 9. Recurring-assumption-family findings

The production/catalog/test sweep classified these reachable assumptions:

- HM numeric dispatch admitted only 3D/4D: widened through the existing typed dispatcher.
- HM distance-aware dark identity admitted only 3D/4D: one additive 5D tuple added; public gates unchanged.
- HM taper validators assumed one KEY lane: retained existing 3D/4D branch and added exact dual-lane validation for the genuinely different 5D taper shape.
- Calendar assumed every `KEY_SESSION` was hard: fixed generically through existing workout-family metadata.
- Prescription context omitted lane ordinal: carried forward from the already-authoritative bound session rather than inferred from dates.
- Adaptation is role-based and treats both KEY lanes identically: conservative but valid for initial dark scope; no new weights invented.
- Generic non-3D session allocation already supports two KEY sessions and required no 5D branch.
- Catalog loader, preview materializer, persistence-independent dark pipeline, and boundary resolver required no HM-specific change.

No further reachable “HM is only 3D/4D”, “one lane”, “5D is 10K-only”, or “single KEY calendar” assumption remained after the full RuntimeCatalog regression.

## 10. Exact 10–16 phase/stage/lane traces

Legend: `F`=`FOUNDATION_AEROBIC_BASE`; `BF`=`BUILD_FASTER_THAN_HM_INTRO`; `BT`=`BUILD_THRESHOLD_DEVELOPMENT`; `RI`=`RACE_SPECIFIC_HM_INTRODUCTION`; `RD`=`RACE_SPECIFIC_HM_DEVELOPMENT`; `RR`=`RACE_SPECIFIC_HM_REHEARSAL`; `TA`=`TAPER_HM_ACTIVATION`. Secondary notation is phase/count and its effective workout.

| Horizon | Phase F/B/RS/T | Exact primary occupancy | Exact secondary occupancy |
|---:|---|---|---|
| 10 | 2/3/3/2 | F×2, BT×3, RD×1, RR×2, TA×2 | Easy×2, FARTLEK×3, FARTLEK×3, Easy×2 |
| 11 | 2/3/4/2 | F×2, BT×3, RD×2, RR×2, TA×2 | Easy×2, FARTLEK×3, FARTLEK×4, Easy×2 |
| 12 | 2/3/5/2 | F×2, BT×3, RI×1, RD×2, RR×2, TA×2 | Easy×2, FARTLEK×3, FARTLEK×5, Easy×2 |
| 13 | 2/4/5/2 | F×2, BF×1, BT×3, RI×1, RD×2, RR×2, TA×2 | Easy×2, FARTLEK×4, FARTLEK×5, Easy×2 |
| 14 | 3/4/5/2 | F×3, BF×1, BT×3, RI×1, RD×2, RR×2, TA×2 | Easy×3, FARTLEK×4, FARTLEK×5, Easy×2 |
| 15 | 4/4/5/2 | F×4, BF×1, BT×3, RI×1, RD×2, RR×2, TA×2 | Easy×4, FARTLEK×4, FARTLEK×5, Easy×2 |
| 16 | 4/5/5/2 | F×4, BF×1, BT×4, RI×1, RD×2, RR×2, TA×2 | Easy×4, FARTLEK×5, FARTLEK×5, Easy×2 |

All allocations emerge from the existing phase and stage allocators. No row is hardcoded in production.

## 11. Exact 10–16 numeric traces

The fixture uses explicit positive readiness `RecentWeeklyVolumeKm=29.5`. Vectors include both taper weeks.

| H | Weekly-volume vector (km) | LR vector (km) | Peak / peak LR / max actual LR share | Anchor → taper W1/W2 |
|---:|---|---|---|---|
| 10 | 29.5,31.5,33.5,35.5,37.0,39.0,41.0,43.0,30.0,18.5 | 8.5,9.0,9.5,10.0,10.5,11.0,13.0,15.0,8.5,5.0 | 43.0 / 15.0 / 34.88% | 43.0 → 30.0/18.5 |
| 11 | 29.5,31.5,33.5,35.5,37.5,39.0,41.0,43.0,45.0,31.5,19.5 | 8.5,9.0,9.5,10.0,10.5,11.0,12.5,14.5,16.0,9.0,5.5 | 45.0 / 16.0 / 35.56% | 45.0 → 31.5/19.5 |
| 12 | 29.5,31.5,33.5,35.5,37.5,39.0,41.0,43.0,45.0,47.0,33.0,20.0 | 8.5,9.0,9.5,10.0,10.5,11.0,12.5,14.0,15.5,16.5,9.0,5.5 | 47.0 / 16.5 / 35.11% | 47.0 → 33.0/20.0 |
| 13 | 29.5,31.5,33.5,35.5,37.5,39.5,41.0,43.0,45.0,47.0,49.0,34.5,21.0 | 8.5,9.0,9.5,10.0,10.5,11.0,11.5,13.0,14.5,16.0,17.5,9.5,6.0 | 49.0 / 17.5 / 35.71% | 49.0 → 34.5/21.0 |
| 14 | 29.5,31.5,33.5,35.5,37.5,39.5,41.0,43.0,45.0,47.0,49.0,51.0,35.5,22.0 | 8.5,9.0,9.5,10.0,10.5,11.0,11.5,12.0,13.5,15.0,16.5,18.0,10.0,6.0 | 51.0 / 18.0 / 35.29% | 51.0 → 35.5/22.0 |
| 15 | 29.5,31.5,33.0,35.0,36.5,38.5,40.5,42.0,44.0,45.5,47.5,49.0,51.0,35.5,22.0 | 8.5,9.0,9.0,10.0,10.0,11.0,11.5,12.0,12.5,13.5,15.0,16.5,18.0,10.0,6.0 | 51.0 / 18.0 / 35.29% | 51.0 → 35.5/22.0 |
| 16 | 29.5,31.0,33.0,34.5,36.0,38.0,39.5,41.0,42.5,44.5,46.0,47.5,49.5,51.0,35.5,22.0 | 8.5,8.5,9.0,9.5,10.0,10.5,11.0,11.5,12.0,12.5,14.0,15.0,17.0,18.0,10.0,6.0 | 51.0 / 18.0 / 35.29% | 51.0 → 35.5/22.0 |

Observed non-taper transition maxima are `+2.0 km` and `6.78%`; every transition is below 2.5 km, 7%, and 8%. The trace’s binding clamp is `None` for these calibration fixtures because the interpolation itself is stricter than every guard. Exact transition tuples `(previous>next, delta, percent, clamp)` are retained in `hm11_horizon_traces.trx`; the weekly vectors above are their complete ordered endpoints.

## 12. 14W golden

Common slot identities are `EASY1=EASY_STANDARD v4`, `EASY2=EASY_STANDARD v4`, and `LONG=LONG_RUN_STANDARD v4`. Calendar order is `Mon(EASY1), Tue(KEY1), Thu(KEY2), Fri(EASY2), Sun(LONG)`. `E` means easy-equivalent; `H` means conservatively true-hard for calendar spacing. Provenance codes: `A`=positive readiness anchor, `I`=generic interpolation, `T1/T2`=0.70/0.43 from the same fixed 51.0 km anchor. All binding lineage is the real catalog binder, not a test fake.

| W | Phase | Vol | LR/share | KEY1 stage → workout | KEY2 stage → workout / hardness | Provenance |
|---:|---|---:|---:|---|---|---|
| 1 | Foundation | 29.5 | 8.5 / 28.81% | F → EASY v4 | Secondary Easy → EASY v4 / E | A |
| 2 | Foundation | 31.5 | 9.0 / 28.57% | F → EASY v4 | Secondary Easy → EASY v4 / E | I |
| 3 | Foundation | 33.5 | 9.5 / 28.36% | F → EASY v4 | Secondary Easy → EASY v4 / E | I |
| 4 | Build | 35.5 | 10.0 / 28.17% | BF → FARTLEK v4 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 5 | Build | 37.5 | 10.5 / 28.00% | BT → THRESHOLD v4 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 6 | Build | 39.5 | 11.0 / 27.85% | BT → THRESHOLD v4 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 7 | Build | 41.0 | 11.5 / 28.05% | BT → THRESHOLD v4 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 8 | RaceSpecific | 43.0 | 12.0 / 27.91% | RI → THRESHOLD v4 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 9 | RaceSpecific | 45.0 | 13.5 / 30.00% | RD → HM_PACE v1 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 10 | RaceSpecific | 47.0 | 15.0 / 31.91% | RD → HM_PACE v1 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 11 | RaceSpecific | 49.0 | 16.5 / 33.67% | RR → HM_PACE v1 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 12 | RaceSpecific | 51.0 | 18.0 / 35.29% | RR → HM_PACE v1 | Secondary FARTLEK → FARTLEK v6 / H | I |
| 13 | Taper | 35.5 | 10.0 / 28.17% | TA → HM_PACE v1 | Secondary Easy → EASY v4 / E | T1 |
| 14 | Taper | 22.0 | 6.0 / 27.27% | TA → HM_PACE v1 | Secondary Easy → EASY v4 / E | T2 |

The 14W plan has 70 distinct, bound sessions and passes final plan and public-preview materialization validation.

## 13. KEY1 binding proof

Lane 0 in progression v2 preserves v1’s stage properties, order, exposure bounds, compression/extension behavior, conditions, fallback chain, and exact workout references. The golden binds `EASY_STANDARD v4`, `FARTLEK v4`, `THRESHOLD_TEMPO v4`, and `HM_PACE v1` at the same stages as the 3D/4D authority.

## 14. KEY2 binding/fallback proof

With evaluated feasible goals, Build and RaceSpecific bind `FARTLEK v6`; Foundation/Taper bind `EASY_STANDARD v4`. With the preferred secondary stage ineligible, both quality phases materialize the declared `*_SECONDARY_EASY_FALLBACK` stage and `EASY_STANDARD v4`, with non-null fallback provenance. No `THRESHOLD_TEMPO` or `HM_PACE` can enter lane 1.

## 15. Session-volume proof

The existing generic non-3D allocator materializes `KEY1/EASY1/KEY2/EASY2/LONG` for all 91 generated weeks across the seven horizons: 455 sessions, five per week, no zero/negative unintended distance, no residual, no minimum violation, no duplicated component accounting, and exact weekly reconciliation. FARTLEK v6 uses v4’s existing mixed/estimated-session-total component model; no new dose number was introduced.

## 16. Peak-volume proof

Actual peaks are 43/45/47/49/51/51/51 km for 10–16W. Short horizons remain below 51 rather than accelerating to it; 15W/16W saturate at 51 through the existing opt-in selected-ceiling mechanism rather than extrapolating above it. The catalog band remains 44–58, while actual peak is the intersection of reachable progression, selected authority, and safety.

## 17. LR/rounding proof

Every rounded LR is on the 0.5 km grid, at or below the exact 36% hard cap and 19 km absolute ceiling. The largest observed values are 18.0 km and 35.71%. The ordinary selected share remains 28%; the generic RaceSpecific ramp may raise later weeks toward, but never through, 36%. Ceiling rounding uses the existing floor-to-grid safety rule; tolerance was not widened.

## 18. Calibration safety

Dedicated tests prove missing and explicit-zero recent weekly volume both throw `CatalogVolumeInvalidReadinessInputException`; neither substitutes 29.5. Positive observed weekly volume remains the runtime anchor. The opt-in 12 km starting-LR field only narrows week 1: observed 16 km longest-run evidence is capped to 12, while absent longest-run evidence with a smaller weekly-derived candidate stays below 12 and retains `WeeklyVolumeDerived` provenance. Twelve is never manufactured as history.

## 19. Taper proof

All horizons have exactly two Taper weeks. The table in §11 records the fixed pre-taper anchor and both outputs. Each is independently `Round0.5(anchor×0.70)` and `Round0.5(anchor×0.43)`; week 2 is never derived from week 1. Taper composition remains primary activation + secondary easy-equivalent + two easy supports + controlled long run, distributable at every horizon.

## 20. Calendar/spacing proof

Every 10–16W real plan uses five distinct preferred days, respects Sunday long run, aligns the race date, and assigns deterministic Tue/Thu KEY lanes in the golden fixture. Build/RaceSpecific’s two conservatively true-hard sessions are exactly two days apart; same-week and cross-week checks pass. Easy adjacency remains legal. A mid-week-safe generic mapping remains covered by the unchanged calendar suite.

Representative existing 10K zero-delta vectors are preserved by the strict-first-pass construction and the unmodified test fixtures: 5D `[Mon,Tue,Wed,Fri,Sun]` with Sunday LR retains first-week KEY `[Mon,Wed]` and later safe KEY `[Tue,Fri]`; 6D `[Mon,Tue,Wed,Thu,Fri,Sun]` retains first-week `[Mon,Wed]` and later `[Tue,Thu]`. Before and after vectors are identical; all existing real 5D/6D deterministic/spacing tests pass in the 3699-test RuntimeCatalog run.

## 21. Adaptation result

The existing five-session generic table returns: all complete → `ProgressAsPlanned`; one EASY missed → `ProgressAsPlanned`; primary KEY missed → `Maintain`; secondary FARTLEK KEY missed → `Maintain`; LONG missed → `Maintain`. Adaptation is lane-blind and therefore treats secondary light quality conservatively as a KEY loss. This is valid for initial dark scope and does not contradict HM.9’s safety ceiling; no new adaptation weight was invented.

## 22. Pace/fallback proof

Independent recent-race evidence permits primary `HM_PACE` with exact recent-race-derived pace. Without that evidence, the unchanged primary chain binds current-fitness `THRESHOLD_TEMPO` and taper `EASY_STANDARD` fallbacks; all seven horizons still produce valid effort-only plans. A product-average desired finish time does not create synthetic training pace: primary exact-pace stages fall back, while secondary FARTLEK remains selected with `EffortOnly` pace prescription because it does not require exact HM pace.

## 23. 9W/17W boundary proof

The unmodified phase resolver returns `TARGET_BELOW_SUM_OF_MINIMUMS` for 9W and `TARGET_ABOVE_SUM_OF_MAXIMUMS` for 17W; the real pipeline throws `DynamicCoreWeekSkeletonInfeasibleException`. Neither becomes ordinary Core, compressed Core, Runway, or LongHorizon.

## 24. HM 3D zero-delta

The named 3D policy remains reference-equal through the typed dispatcher and retains peak 34, LR cap 46%, all existing vectors, two-week non-chained taper, single lane, pace fallback, calendar, and dark gates. The new starting-LR field is null for 3D. All unchanged HM.8 tests pass inside RuntimeCatalog.

## 25. HM 4D zero-delta

The named 4D policy remains reference-equal through the dispatcher and retains peak 43, LR cap 40%, existing vectors, taper, binding, calendar, pace fallback, and dark state. Master/progression v1 remain its exact references. The new starting-LR field is null for 4D. All unchanged HM.2 tests pass inside RuntimeCatalog.

## 26. 10K multi-lane zero-delta

No 10K catalog document, policy number, gate, or workout reference changed. Unknown hardness defaults to historical hard behavior, and the old all-KEY algorithm is always attempted first. Full RuntimeCatalog coverage, including Intermediate 3D/4D/5D/6D, Beginner 3D, and reachable Advanced frequencies, is green. Existing public state is unchanged.

## 27. Catalog/hash proof

`PlanCatalog.Tests`: 1510/1510. This includes schema, graph/combination, exact-version, cross-release hash, and publication validation. Source inventory is 145 JSON files. A Release rebuild copied all 145; the real packaged-catalog HTTP smoke then passed. Existing v1 HM files and published artifacts were not content-mutated.

## 28. Cosmetic-message correction

The two 3D allocation errors now interpolate the resolved configured cap instead of saying “42%” unconditionally. A focused test supplies 46%, proves the message contains `46% hard cap`, and proves it does not contain the stale 42% literal. Validation arithmetic is untouched.

## 29. Regression results

Personally observed on 2026-09-11:

- HM.11 + hardness + cosmetic target: 21/21 passed before the final trace-only additions; all 19 HM.11 cases also compile/run in subsequent focused/full runs.
- Exact 10–16 trace theory: 7/7 passed.
- 14W golden: 1/1 passed.
- `PlanCatalog.Tests`: 1510/1510 passed.
- Release API build: 0 warnings, 0 errors; packaged real-HTTP smoke: 1/1 passed.
- Full `RuntimeCatalog`: 3699/3699 passed in 6m03s.
- Isolated monolithic `RunningApp.IntegrationTests`: 4605 passed / 4 failed / 4609 total in 41m01s; no timeout.

The four monolithic failures match the documented durable baseline by exact identity:

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`.
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`.
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`.
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`.

Two earlier monolithic attempts are not counted as passes: the first shell window ended at 30 minutes while its child test process continued; accidentally starting a second run concurrently caused PostgreSQL reset collisions (`23505`, duplicate `IX_UserProfiles_InternalUserId`) and 53/63 cascading failures. After both processes ended, the single isolated 60-minute-window run above completed naturally with exactly the four baseline identities. Test-count reconciliation is exact: HM.8/HM.9/HM.10 baseline 4587 + HM.11 net 22 = 4609.

## 30. Public/Runway/LongHorizon state

Unchanged and explicitly tested: `IsSupportedIdentity` and `IsSupportedPreparationRunwayIdentity` return false for HM Intermediate 5D. No public controller/selector, Runway gate, LongHorizon composition, activation, or persistence path was widened. HM 3D/4D remain dark too.

## 31. Remaining blockers

None inside `HALF_MARATHON × INTERMEDIATE × 5D × CORE 10–16W × DARK`. Public activation and longer-horizon composition remain intentionally closed future scopes, not HM.11 blockers. The four durable full-suite baseline failures remain repository debt and were not changed here.

## 32. Intermediate HM completion conclusion

Intermediate HM Core is now complete in dark scope for 3D, 4D, and 5D across every canonical 10–16W horizon. All three reuse the same master/phase authority, primary progression semantics, generic allocators/materializers, and typed per-frequency numeric policy dispatch; only 5D adds the frozen phase-aware secondary lane.

## 33. Recommended next phase

Use a separate, explicitly authorized governance/activation phase to decide whether Intermediate HM 3D/4D/5D should enter public Core routing and what Preparation Runway/LongHorizon support is required. Do not begin Beginner or Advanced HM work as part of this completed phase.

## Final classification

`HM_INTERMEDIATE_5D_10_16W_FULL_DARK_IMPLEMENTATION_COMPLETE`
