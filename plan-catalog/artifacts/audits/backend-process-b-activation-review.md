# Backend / Process B Activation Review — TEN_K__4D__INTERMEDIATE v10

Read-only diagnostic review. **No backend files modified. No plan-catalog artifacts modified.**

## Headline finding

**Backend contains zero integration with plan-catalog.** No code path loads a plan-catalog bundle, JSON artifact, or any document keyed by `TEN_K__4D__INTERMEDIATE`, `GOAL_PACE_TEN_K`, or any `RUNTIME_CONDITION_VALUE` (confirmed by repository-wide `grep`, zero matches). Backend generates previews exclusively from its own SQL database (`PlanTemplates` table via EF Core), seeded with exactly **3 rows, all 5K/Beginner**. Most of this review's questions resolve to "not yet applicable" rather than pass/fail, because the integration they presuppose doesn't exist yet.

## Q1 — Bundle loading

- **Can backend load v10?** No.
- **Version currently consumed:** None — zero plan-catalog integration exists.
- **Pinned to an older candidate?** No — never referenced any plan-catalog version.
- **Data source:** Seeded database rows. `PlanTemplate.DataJson` is a free-form string in a bespoke schema (`templateId/version/goalType/goalDistance/level/daysPerWeek/unit/weeks[].days[].{slotIndex,dayType,distanceKm,durationMin,intensity}`), structurally unrelated to any plan-catalog document type. Exactly 3 rows are seeded (baked into EF Core migrations): `habit_5k_beginner_3day_km_v1`, `habit_5k_beginner_4day_km_v1`, `race_5k_beginner_3day_km_v1` — all `goalDistance=five_k`, `level=beginner`. **No 10K, Intermediate, or 4-day-10K row exists anywhere.**
- `PlaceholderPlanGenerationEngine.SelectTemplateAsync` looks for an exact match; if none, it **silently falls back to the first seeded row** with `FallbackUsed=true`. A request for `TenK/Intermediate/4-day` would never match and would receive an arbitrary 5K/Beginner substitute today.

## Q2 — Runtime condition mapping (TD-D3-001)

**Result: `NOT_APPLICABLE_NO_INTEGRATION_EXISTS`.** `PACE_SOURCE_IN`, `TIME_ADEQUACY_IN`, `CORE_ENTRY_READINESS_IN` do not exist anywhere in backend (zero matches, any value, v1 or v2). Backend emits neither the v2 canonical values nor the old v1 strings, because it emits no `RuntimeConditionType`-shaped value at all. TD-D3-001 is **unverifiable because unimplemented**, not satisfied or violated.

**`readiness="STANDARD"` investigation:** Produced **nowhere in backend**. It exists only in `plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json` — a Golden Fixture example document, as `CORE_ENTRY_READINESS_RESOLVER.evaluations[0].result.readiness`. It cannot conflict with backend's `CORE_ENTRY_READINESS_IN` because backend doesn't implement that field. Within the fixture itself, `STANDARD` matches neither v1 nor v2 vocabulary — a pre-existing, already-documented mismatch (`AUD-048`, `domain-d3-followup.md`), not a new backend finding.

## Q3 — Schedule generation proof

**Backend DOES have concrete schedule generation** — `PlanServices.GeneratePreviewAsync` deserializes a template's `DataJson`, computes real calendar dates from the next Monday, and maps `slotIndex` to actual weekdays. This is real, working code — but it operates entirely on backend's own schema, never on any plan-catalog document.

- **Can `GOAL_PACE_TEN_K` appear in a generated plan?** **No.** It's a plan-catalog workout key; the only 3 seeded `DataJson` blobs contain `dayType` values `easy`/`interval`/`long_run` only — no goal-pace equivalent, and no 10K/Intermediate template exists to select in the first place.
- **Weeks/phases containing goal-pace:** None — `GOAL_PACE_TEN_K` is unused by runtime despite being present in the catalog.
- **Second hard day generated:** Not applicable — no goal-pace day is ever generated.
- **Taper protection:** `UNKNOWN_FROM_CODE_EVIDENCE` — all 3 seeded templates contain only 1 week (`weekType=build`); no taper-week generation logic was found to inspect.

## Q4 — Hard-session safety (TD-WAVE5-001)

| Field | Classification |
|---|---|
| `maximumHardSessionsPerWeek` | `UNKNOWN_FROM_CODE_EVIDENCE` — does not exist in backend's model at all |
| `allowSecondHardStimulus` | `UNKNOWN_FROM_CODE_EVIDENCE` — same |
| `allowGoalPaceRehearsal` | `UNKNOWN_FROM_CODE_EVIDENCE` — same |

This is a **more fundamental gap** than TD-WAVE5-001's original framing assumed: backend has neither the flag nor the candidate reachability it would guard. Both are absent, not merely unguarded.

## Q5 — End-to-end preview smoke test

**Not executed as a live test.** `RunningApp.IntegrationTests` requires a real database context, and per Q1/Q3, no request could exercise v10 regardless — no matching template exists to select. Running the existing suite would not exercise this candidate, so it was not run (to avoid presenting irrelevant test execution as evidence).

**Predicted outcome from static code reading:** `SelectTemplateAsync` would find no exact match for `Race/TenK/(closest to Intermediate)/4-day` and would silently return `FallbackUsed=true` with the **first seeded 5K/Beginner/3-day** template — a materially different plan, with no error surfaced beyond an internal `FallbackReason` string.

## Q6 — Output/API compatibility

| Concept | Backend representation | Detail |
|---|---|---|
| `GOAL_PACE_TEN_K` | **None** | `TrainingDayType` has no goal-pace case; closest lossy fits are `Tempo`/`Interval` |
| `GOAL_PACE_REHEARSAL` (stage) | **None** | Backend has no workout-progression-stage concept |
| `KEY_SESSION` (slot role) | **None** | `TrainingDayType` conflates type and role; no "key session" designation |
| `EASY_SUPPORT` (slot role) | **Partial** | `Easy`/`RecoveryEasy` cover workout type, but no distinct "support slot" role |
| `LONG_RUN` | **Present** | `TrainingDayType.LongRun`, used in all 3 seeded templates |
| Phase names | **Partial** | `TrainingWeekType {Base,Build,Recovery,Peak,Taper,RaceWeek}` conceptually overlaps (`Base~FOUNDATION`, `Build~BUILD`, `Peak~RACE_SPECIFIC`, `Taper~TAPER`) but no explicit mapping code exists; only `build` is ever populated in seed data |
| Intensity labels | **Free string** | `Intensity` is a nullable free-form string (e.g. `"z2"`) — structurally able to carry a new label without a schema change, but no translation code exists |

## Final classification

**`BACKEND_BLOCKED_BY_SCHEDULE_GENERATION`**

Not classified as `BACKEND_BLOCKED_BY_RUNTIME_MAPPING` specifically, because the mapping gap (TD-D3-001) is a symptom of the same root cause, not a separate defect: the schedule-generation engine itself is explicitly documented in its own source as a **"Phase 1 placeholder"** with **"no real generation logic"**, reads from a schema structurally unrelated to plan-catalog, and has zero seeded content for TEN_K/Intermediate/4-day. `BACKEND_READY_FOR_PILOT_SMOKE_TEST` and `BACKEND_READY_WITH_MANUAL_ACTIVATION_RISKS` are both ruled out — there is no code path to smoke-test against v10 at all, not merely unresolved risk.

## Validation

- `dotnet build -c Release` (in `backend/`) — **SUCCESS**, 0 warnings, 0 errors.
- `dotnet test` — **not run**. No test in `RunningApp.IntegrationTests` touches plan-catalog vocabulary (confirmed absent by grep); running it would require live DB infrastructure and would produce no evidence relevant to v10.
- No backend files were changed. No plan-catalog artifacts were changed (only this new report pair was added).
- No publish, activate, retire, or supersede action occurred.
