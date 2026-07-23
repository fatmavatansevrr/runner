# Appsel Repository Current-State Audit

Full repository state and architecture audit, covering Plan Catalog, Backend, and their integration. Read-only — no production code, catalog artifact, schema, governance file, test, or migration was modified. Companion to `APPSEL_REPOSITORY_CURRENT_STATE_AUDIT.json`.

## 1. Audit verdict

**REPOSITORY_STATE_MAPPED_WITH_GAPS**

## 2. Executive summary

The system today can, entirely in an internal/dark path never reachable by a real user: resolve a catalog candidate (via an internal-dry-run bypass only — the public path always rejects the pilot because it is `DRAFT`), resolve all four runtime conditions, build a structural phase/week skeleton, assign a fine-grained `KEY_SESSION` progression stage per week (new, Phase 4F.6A), assign calendar dates, and validate all of it. **It cannot yet** bind any exact workout definition to a structural role, calculate any pace/distance/duration, populate the final `GeneratedPreviewPlanPayload`, or confirm/persist a catalog-sourced plan — the confirm service's own `BuildPlan` method was already removed as dead code, since every path through it currently throws. Every public API endpoint that reads/writes plan data today does so exclusively against the **legacy SQL/seeded-template path**, which is fully wired and unaffected by any of this catalog work. The dark catalog pipeline and the live legacy path currently run side by side with zero interaction.

## 3. Current architecture

`RunningApp.Api` (controllers) → `RunningApp.Application` (services + the entire `RuntimeCatalog` dark pipeline) → `RunningApp.Domain`/`RunningApp.Persistence`. `plan-catalog` is a fully separate C# solution with **no project reference in either direction** — the backend reads catalog JSON directly via `System.Text.Json` (`PlanCatalogBundleLoader`, and the new `CatalogWorkoutProgressionLoader`), never referencing `PlanCatalog.Core`/`.Contracts` types. This is a clean, intentional Process A / Process B boundary, confirmed with no forbidden or suspicious dependency direction found.

## 4. Current end-to-end flow

```
Candidate eligibility (dark-reachable only) → runtime conditions (dark) → phase/week skeleton (dark)
→ fine-grained stage scheduling (dark, NEW Phase 4F.6A) → calendar assignment (dark) → dated-skeleton validation (dark)
→ [STOPS HERE] workout binding (not started) → prescription (not started) → GeneratedPreviewPlanPayload (never populated)
→ confirm (unconditionally rejects) → persistence (no producer) → home/calendar reads (legacy-shape data only)
```

The **first genuinely missing step** is workout binding (Phase 4F.6B) — everything before it is implemented and tested (dark); everything after it does not exist yet.

## 5. Plan Catalog status

Pilot candidate `TEN_K__4D__INTERMEDIATE v10`, status `DRAFT`, dependency graph: `TEN_K_MASTER v6` / `RUN_LAYOUT_4D v2` / `INTERMEDIATE_MODIFIER v6` / `APPSEL_RACE_PLAN_V1 v4` / `TEN_K_WORKOUT_PROGRESSION_V1 v5`. All content-decision blockers (D2/D3/D4/D13) are resolved — `D13GoalPaceTenKResolutionTests` confirms the candidate's full dependency-closure blocking-entry count is zero. Publication has simply never been executed via the `PlanCatalog.Cli publish` command, deliberately deferred pending 4F.6B/4F.7. `EASY_SHAKEOUT`/`EASY_WITH_STRIDES`/`LONG_RUN_PROGRESSION` remain absent from the catalog (fixture-referenced only) — tracked as `TD-EASY-WORKOUT-REGISTRY-001`.

## 6. Backend status

655/655 backend tests passing. Runtime-condition resolution, phase/week skeleton materialization, fine-grained stage scheduling (new), calendar assignment, and their respective validators are all implemented, tested, and wired together — but **only** inside `CatalogPreviewGenerator`'s dark path, which itself is never reached by a real request (the eligibility gate always throws `CatalogCandidateNotPublishedException` for the `DRAFT` pilot). `PlanServices`/`PlaceholderPlanGenerationEngine` — the actual live generation path — has zero interaction with any of this: it still only serves legacy seeded SQL templates, and (contrary to an earlier session's summary) no longer silently falls back to an unrelated template — a `TEN_K`/Intermediate/4-day request with no exact seeded match now fails loudly with `PlanTemplateNotAvailableException`.

## 7. Integration map

Backend reads plan-catalog's JSON output directly (file-system read, no shared assembly). Nothing in plan-catalog knows the backend exists. The only "integration" today is one-directional and entirely dark: `PlanCatalogBundleLoader` + `CatalogWorkoutProgressionLoader` read catalog files; nothing writes back, publishes, or activates anything from the backend side.

## 8. Phase-status matrix

| Phase                                    | Status                                                              |
| ---------------------------------------- | ------------------------------------------------------------------- |
| Plan Catalog authoring phases            | COMPLETE                                                            |
| Runtime-condition phases (4D.1–4D.5.1)  | COMPLETE_DARK                                                       |
| Phase 4E                                 | COMPLETE_DARK                                                       |
| Phase 4F.1                               | COMPLETE_DARK (contract only, zero producers for the final payload) |
| Phase 4F.2 / 4F.3 / 4F.4 / 4F.5 / 4F.5.1 | COMPLETE_DARK                                                       |
| Step A / A.1 / A.2 / B / C / C.1         | GOVERNANCE_COMPLETE                                                 |
| Phase 4F.6A                              | COMPLETE_DARK (this session)                                        |
| Phase 4F.6B                              | NOT_STARTED — ready to start                                       |
| Phase 4F.7                               | NOT_STARTED                                                         |
| Phase 4F.8                               | NOT_STARTED                                                         |
| Phase 4F.9/4F.10/4F.11                   | UNKNOWN — no specification exists yet                              |

## 9. Contract inventory (highlights)

- `GeneratedCatalogPlanPayload` (Phase 4F.1's final contract): fully defined, **zero producers anywhere in the codebase**.
- `GeneratedCatalogPlanSkeleton.StageKey`: phase granularity, ambiguous name if read casually — the new `ProgressionStageKey` (4F.6A) is the fine-grained sibling, kept deliberately distinct.
- `TrainingDay.CatalogStageKey` (existing persistence field, added earlier): its own granularity (phase vs. fine-grained) was **not verified** in this pass — a future persistence-wiring phase must explicitly reconcile it against the new `ProgressionStageKey` before mapping anything into it.
- `TrainingPlan`/`TrainingWeek`/`TrainingDay` all carry nullable catalog-provenance fields (added Phase 3 / 4F.5.1) that no code has ever populated.

## 10. User-input utilization

Well-used: `GoalType`, `DaysPerWeek`, `Level`, `RaceDate`, `TargetFinishTimeSeconds`, `PreferredDays`, `LongRunDay`. Collected but **not yet read by any resolver logic**: `RecentLongestRunKm`, `RecentWeeklyVolumeKm`, `RecentRunsPerWeek`, `RecentRaceDistanceKm/FinishTimeSeconds/Date`. Collected but **entirely unused by any catalog code path**: `Unit`, `RaceName`, `WeeklyAvailability`, `PreferredPace`, `HabitPlanType`, and all `Custom*` fields. `GoalDistanceKm` is hardcoded to `10.0` in `ResolverInputSnapshot` construction rather than derived from the request.

## 11. Preview/confirm/persistence status

```
Can a real catalog-generated schedule be confirmed today?
NO
```

Exact blocking boundary: `CatalogPlanConfirmationService` throws `CatalogPreviewNotPersistableException` because `snapshot.GeneratedPreviewPlanPayload` is always null, and even in a hypothetical non-null case throws `CatalogPreviewMaterializationNotImplementedException` — the file's own comment states the previous `BuildPlan` implementation was removed as dead code because every path above it already unconditionally throws.

## 12. Public API status

Every plan-related endpoint (`generate-preview`, `confirm`, `active/home`, `active/calendar`, `active/details`, `training-days/*`, `pending-confirmations`, `profile/overview`, `cancel`) currently reads/writes exclusively against the **legacy SQL/seeded-template data path** — none of them is reachable via the catalog dark pipeline today.

## 13. Test status

- `dotnet test PlanCatalog.sln -c Release --no-build` → **335/335 passing**.
- `dotnet test RunningApp.sln -c Release --no-build` → **655/655 passing** (628 pre-existing + 27 new from Phase 4F.6A).

## 14. Governance and risks

Open, non-blocking-today risks: `TD-BACKEND-001` (dark wiring now substantially supersedes this in substance, but public reachability remains blocked), `TD-EASY-WORKOUT-REGISTRY-001` (blocks 4F.6B completion and publication, not 4F.6B's start), `TD-REGISTRY-001`, `TD-PACESOURCE-001/002`, `TD-CORE-READINESS-001`, `TD-WAVE5-001` (all pre-existing, unrelated to this session). New non-blocking finding from Phase 4F.6A: the `RACE_SPECIFIC` phase's exposure/extension-behavior configuration has **zero capacity slack** for the current default 12-week pilot shape.

## 15. Missing connections

`RuntimeConditionResolutionService` ↔ live `PlanServices`/`PlaceholderPlanGenerationEngine` (never wired); `GeneratedCatalogStageSchedule` (4F.6A) ↔ workout binder (expected, by design — 4F.6B's job); `GeneratedCatalogPlanPayload` ↔ any producer (contract with zero producers); `CatalogPreviewSnapshot` ↔ confirm materialization (removed dead code); `TrainingDay.CatalogStageKey` ↔ `ProgressionStageKey` (unreconciled naming/granularity).

## 16. Recommended next sequence

1. **Phase 4F.6B** — bind `EASY_SUPPORT`/`LONG_RUN` fixed defaults and the `KEY_SESSION` stage-controlled candidate to exact workout definitions. Ready now.
2. **Phase 4F.7** — personalized prescription (per `AUD-508`'s `TAPER_SHARPEN` directive and general dosage).
3. **Phase 4F.8** — public workout-type mapping.
4. **Unscheduled** — wire the entire dark pipeline into live generation (`TD-BACKEND-001`'s real closure).
5. **Unscheduled** — rebuild confirm/persistence materialization, resolving the `TrainingDay.CatalogStageKey` granularity question first.
6. **Publication** — after `TD-EASY-WORKOUT-REGISTRY-001` is resolved and the above are in place.

## 17. Next action

```
NEXT_4F6B
```

## 18. Files inspected

See `filesInspected` in the JSON companion — backend `RuntimeCatalog` tree, API controllers, `Program.cs`, `TrainingPlan`/`TrainingWeek`/`TrainingDay` entities, migrations list, `PlaceholderPlanGenerationEngine.cs`, `CatalogPlanConfirmationService.cs`, `GeneratePreviewRequest.cs`/`ResolverInputSnapshot.cs`, the full v10 dependency chain, `PublishReadinessValidator.cs`, `PilotDomainContentAudit.cs`, `activation-readiness-risks.json`, plan-catalog test folder structure, `PlanCatalog.Cli`, and every artifact produced earlier in this session (Steps A through Phase 4F.6A).

## 19. Files created

- `plan-catalog/artifacts/audits/APPSEL_REPOSITORY_CURRENT_STATE_AUDIT.json`
- `plan-catalog/artifacts/audits/APPSEL_REPOSITORY_CURRENT_STATE_AUDIT.md`

## 20. Files modified

None.

## 21. Repository state

Branch `main`, HEAD unchanged at `0c6796578f08bc1d76d96f1944a80c9075455206`. No staged changes. No commit made. Working tree contains accumulated uncommitted work from Phases 4F.4 through 4F.6A and Steps A through C.1, all previously attributed; no unexpected files found.

## 22. Final conclusion

```
Today the repository is at:
A fully-implemented, fully-tested, entirely DARK internal pipeline reaching from candidate
resolution through fine-grained KEY_SESSION stage scheduling and calendar-date assignment —
never reachable by any real user request, and never producing any output that preview,
confirm, or persistence can consume. Every live-facing endpoint still runs exclusively on
the legacy SQL/seeded-template path.

The next implementation target is:
Phase 4F.6B — binding EASY_SUPPORT/LONG_RUN fixed defaults and the KEY_SESSION
stage-controlled candidate to exact workout definitions, the first step after which the dark
pipeline will carry actual workout identity, not just structural/temporal scheduling.
```
