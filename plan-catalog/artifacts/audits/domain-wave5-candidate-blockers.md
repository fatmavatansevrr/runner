# Wave 5 — Candidate Blocker Measurement

Measured via `BlockerScopeMeasurement` over each root's exact dependency closure (never hardcoded independently of `PilotDomainContentAudit.Entries`).

| Root | Decision-level | Artifact-level | Notes |
|---|---|---|---|
| `TEN_K__4D__INTERMEDIATE v4` (active) | 13 | 9 | Unaffected — still references `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1`. |
| `TEN_K__4D__INTERMEDIATE v5` (Wave 2 candidate) | 8 | 8 | Unaffected — preserved unchanged. |
| `TEN_K__4D__INTERMEDIATE v6` (Wave 3 candidate) | 4 | 4 | Unaffected — preserved unchanged. Remaining: D2, D3, D4, D13. |
| `TEN_K__4D__INTERMEDIATE v7` (Wave 5 / D2 candidate, **new**) | **3** | **3** | D2 removed. Remaining: D3, D4, D13. |

**Exact D2 decision removed:** D2 — `INTERMEDIATE_PROGRESSION_MODIFIER_V1` dosage/settings (`maximumComplexityTier` removed; `maximumHardSessionsPerWeek=1`, `mainSetDoseMultiplier=1.00`, `allowGoalPaceRehearsal=true`, `allowSecondHardStimulus=false` all resolved to non-blocking classifications on v2).

**Exact remaining decisions on the new candidate:** D3 (`RUNTIME_CONDITION_VALUES_V1` vocabulary), D4 (`PEAK_VOLUME_BANDS_V1` non-INTERMEDIATE rows), D13 (`GOAL_PACE_TEN_K` evidence gap).

**Wave 4 status:** not implemented — no `domain-wave4-*` audit files or `AddWave4*` audit entries exist in the repository as of this task, so `TEN_K__4D__INTERMEDIATE v6` (Wave 3) was the correct starting point, matching the task's documented expectation for this scenario.
