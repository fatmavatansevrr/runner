# Golden Fixture v3 — Integrity Verification

Generated: 2026-07-08T07:46:23.6788117Z

**Overall status: PASSED**

| Check | Expected | Actual | Status |
|---|---|---|---|
| All four canonical fixture files exist | true | True | PASS |
| DecisionTrace schemaVersion | 3 | 3 | PASS |
| PlanDocument schemaVersion | 3 | 3 | PASS |
| PlanDocument fixtureRevision | 3 | 3 | PASS |
| Both fixtures use fixtureKey GOLDEN_10K_INTERMEDIATE_4D_12W | GOLDEN_10K_INTERMEDIATE_4D_12W / GOLDEN_10K_INTERMEDIATE_4D_12W | GOLDEN_10K_INTERMEDIATE_4D_12W / GOLDEN_10K_INTERMEDIATE_4D_12W | PASS |
| progression_rules_v2.yaml schemaVersion | 2 | 2 | PASS |
| PlanDocument contentHash verifies | 7b2e92dce611ae27751524ee41cb8c1b6b2b153eea7a125edbf5ab60eb3bc735 | 7b2e92dce611ae27751524ee41cb8c1b6b2b153eea7a125edbf5ab60eb3bc735 | PASS |
| Week 1 boundary | 2026-08-03 -> 2026-08-09 | 2026-08-03 -> 2026-08-09 | PASS |
| Week 12 boundary | 2026-10-19 -> 2026-10-25 | 2026-10-19 -> 2026-10-25 | PASS |
| horizon.raceWeekStartDate | 2026-10-19 | 2026-10-19 | PASS |
| Every week spans seven calendar days, Monday->Sunday | true | True | PASS |
| Taper training distribution | 8 + 8 + 4 km | 8.0 + 8.0 + 4.0 km | PASS |
| Week 12 total (training 20 + race 10 = 30 km) | 30 | 30.0 | PASS |
| Race day loadClassification | RACE | RACE | PASS |
| Race day stimulusAccountingScope | RACE_EXCLUDED_FROM_TRAINING_HARD_COUNT | RACE_EXCLUDED_FROM_TRAINING_HARD_COUNT | PASS |
| PHASE_TRANSITION_DELOAD present | present | present | PASS |
| PLANNED_SINGLE_SESSION_SPIKE_CHECK present | present | present | PASS |
| FINAL_VALIDATION present | present | present | PASS |
| WARNING_POLICY_V2 present | present | present | PASS |
| ESTIMATED_THRESHOLD_EFFORT present | present | present | PASS |
| Obsolete CURRENT_LACTATE_THRESHOLD_EFFORT absent from JSON fixtures | 0 occurrences | 0 occurrences | PASS |
| 'Warning Policy' pipeline stage documented | present | present | PASS |
| 'Warning Presentation' pipeline stage documented | present | present | PASS |
| 'Provisional Peak' pipeline stage documented | present | present | PASS |
| 'Weekly Volume Curve' pipeline stage documented | present | present | PASS |
| PrescriptionMode and DistanceAccountingMode vocabularies do not overlap | no shared values | no shared values | PASS |
