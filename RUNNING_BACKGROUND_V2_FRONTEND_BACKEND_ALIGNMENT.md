# Running Background V2 — Four-Level Contract, Conditional Detail Screen, and Frontend–Backend Alignment

> **Superseded in part by Running Background V2.1** (see
> `RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md`): as of V2.1,
> legacy aliases (`new_to_running`/`used_to_run`/`running_regularly`) are no
> longer accepted at the public HTTP request boundary or anywhere in the
> frontend — §2/§4/§9 below describe the V2 state (legacy aliases accepted
> broadly at the JSON boundary), which was the correct and accurate
> description at the time this document was written. This note is additive;
> the sections below are preserved unchanged as the historical record of the
> V2 milestone.

**Classification:** `RUNNING_BACKGROUND_V2_IMPLEMENTED_LOCAL_UNPUBLISHED`

**Scope boundary:** No catalog candidate was published or activated. No plan-generation science value (volume, taper, long-run, pace, or prescription policy) was changed. No commits were created.

---

## 1. Canonical Contract

Four canonical `RunningBackground` values replace the prior three-value model, on both backend and frontend, wire-identical:

| Value | Wire value | Approved label | Approved description |
|---|---|---|---|
| Beginner | `beginner` | Beginner | "I'm new to running or getting back into it." |
| Intermediate | `intermediate` | Intermediate | "I run regularly and can comfortably complete 5–10 km." |
| Advanced | `advanced` | Advanced | "I train consistently and regularly run 10 km or more." |
| Experienced | `experienced` | Experienced | "I have a strong running base and regularly train for longer distances." |

Only `Beginner` skips the `runner-background-details` screen; the other three open it before proceeding to goal-time / habit-goal.

## 2. Legacy Compatibility Decision (documented, not guessed)

The three legacy values map as follows, accepted on read (JSON request body, EF/Postgres persisted rows, internal snapshot JSON) but never emitted on write:

| Legacy value | Maps to | Evidence |
|---|---|---|
| `new_to_running` | `Beginner` | Direct lexical correspondence — no ambiguity. |
| `used_to_run` | `Beginner` | The newly approved Beginner description text is *"I'm new to running or getting back into it."* — the phrase "getting back into it" is the exact semantic of the old "used to run" / "returning after a break" label. This textual match is the cited justification, not an assumption. |
| `running_regularly` | `Intermediate` | Direct lexical correspondence to the new Intermediate label/description ("I run regularly..."). |

`RunningBackground.Intermediate` (backend) / `RunningBackground.intermediate` (frontend) is the **only** value with a catalog pilot mapping — this exactly replaces the prior informal `RunningRegularly` stand-in used by `V1CatalogPilotIdentityPolicy`. Beginner/Advanced/Experienced have no catalog mapping; broadening pilot eligibility to them is explicitly out of scope for this migration.

## 3. Backend Changes

- `RunningApp.Domain/Enums/RunningBackground.cs` — 4-value enum, `[JsonConverter(typeof(RunningBackgroundJsonConverter))]`.
- `RunningApp.Domain/Enums/RunningBackgroundJsonConverter.cs` (new) — canonical + legacy-alias JSON read, canonical-only write. Property-level `[JsonConverter]` also applied directly to `GeneratePreviewRequest.Level` and `ResolverInputSnapshot.Level` (System.Text.Json `Converters`-list entries outrank type-level attributes; property-level attributes are the one thing that outranks the `Converters` list — see §9).
- `RunningApp.Persistence/Converters/RunningBackgroundCompatibilityConverter.cs` (new) — EF `ValueConverter` with the same legacy-alias read compatibility, registered in `AppDbContext.OnModelCreating` after the generic snake-case enum converter loop (last registration wins).
- `RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` — `Level` constant now `RunningBackground.Intermediate`.
- New request DTO fields (all optional, all suppressed for Beginner): `RecentWeeklyVolumeKm`, `RecentLongestRunKm`, `RecentRaceDistanceKm`, `RecentRaceFinishTimeSeconds`, `RecentRaceDate`.
- Additive EF migration `20260716175426_RunningBackgroundV2FourLevelModel` — updates 3 seeded `PlanTemplates.Level` rows from `new_to_running` → `beginner`. Reversible; `dotnet ef migrations has-pending-model-changes` clean after apply.

## 4. Frontend Changes

- `mobile/lib/core/models/running_background.dart` (new) — `RunningBackground` enum with `wireValue`, `label`, `description`, `skipsRunnerBackgroundDetails`, `parse`/`tryParse` (legacy-alias-aware, throws `FormatException` on unknown input).
- `mobile/lib/core/models/recent_race_result.dart` (new) — `RecentRaceResult` (distanceKm, finishTimeSeconds, raceDate, optional raceName), `summary({required useKm})` producing e.g. `"10K · 58:30 · 14 Jun 2026"`.
- `mobile/lib/features/onboarding/presentation/running_background_page.dart` — renders all 4 values via `RunningBackground.values`; preserves existing `SelectableCard`/checkmark/progress-bar/back-button conventions; no redesign, no mascot art.
- `mobile/lib/features/onboarding/presentation/runner_background_details_page.dart` (new) — heading/supporting text per spec; "Average weekly distance" and "Longest run" numeric fields (decimal, ≥0, unit suffix read from `OnboardingState.unit`, **no unit toggle**), each with an "I'm not sure" checkbox mapping to `null` (0 is a distinct, explicit value); "Recent race result (optional)" section with an "Add recent result" button, editable/removable summary once saved.
- `mobile/lib/features/onboarding/presentation/recent_race_result_page.dart` (new) — distance presets (5K/10K/Half Marathon/Marathon/Other + free-entry km field), hh/mm/ss typed duration input, race-date picker constrained to `lastDate: now` (not future), fully optional, `Navigator.pop`s a `RecentRaceResult?`. Entirely distinct object from the target race entered in "Enter Race Details" — no shared state, no merging.
- `mobile/lib/core/routing/app_router.dart` — `runnerBackgroundDetails` and `recentRaceResult` routes added; the latter accepts an optional `extra: RecentRaceResult?` for edit-prefill.
- `mobile/lib/features/onboarding/data/onboarding_provider.dart` — `OnboardingState.runningBackground: RunningBackground` (was `level: String`); new nullable fields `recentWeeklyVolumeKm`, `recentLongestRunKm`, `recentRaceResult`, with explicit `clearX` flags in `copyWith` so "unset" is representable distinctly from "leave unchanged". `updateRunningBackground` clears all three recent-running fields immediately on transition **to** Beginner. `generatePreview()` sends `level: runningBackground.wireValue` plus the new fields, defensively suppressed (`null`) for Beginner even though they're already cleared on transition.
- `mobile/lib/core/network/dtos.dart` — `GeneratePreviewRequest` extended with the 5 new nullable fields, each conditionally included in `toJson()` only when non-null (so an unanswered field is omitted from the wire payload entirely, never sent as `0` or `null`-valued key).
- `mobile/lib/features/onboarding/presentation/goal_time_page.dart` and `habit_goal_page.dart` — back-button now routes to `runnerBackgroundDetails` for non-Beginner levels, `runningBackground` for Beginner (mirrors the forward-navigation split).

## 5. Conditional Navigation

```
running-background (4 options)
  ├─ Beginner        → goal-time / habit-goal   (unchanged path)
  └─ Intermediate/Advanced/Experienced
       → runner-background-details
           → (optional) recent-race-result → back to details (compact summary)
           → Continue → goal-time / habit-goal
```

Back-button mirrors this exactly in both directions.

## 6. Testing

- **Backend:** 839/839 `RunningApp.IntegrationTests` passing (full re-run after all frontend work, no backend regressions). Includes the new `RunningBackgroundV2Tests.cs`: canonical/legacy JSON round-trip, unknown-value rejection, DTO-level property-attribute path, non-Intermediate levels not coerced into the pilot mapping, and a real-Postgres round-trip test proving all 6 legacy/canonical text values remain readable.
- **Frontend:** new `mobile/test/running_background_v2_test.dart`, 13 tests — `RunningBackground` wire-value/parse/legacy-alias/skip-logic/copy exactness, `RecentRaceResult.summary` formatting (preset + fallback + mile conversion), and `GeneratePreviewRequest.toJson()` field-suppression (omitted-when-null, explicit-zero-is-sent, all-present-together). All pass. `flutter analyze` clean (no new warnings/errors; only pre-existing lint infos elsewhere in the codebase). Pre-existing `test/widget_test.dart` failure confirmed unrelated (reproduces identically on the pre-change baseline via `git stash`) — a Firebase-initialization gap in the full-app smoke test, not caused by this work.

## 7. HTTP Verification (local API + real Postgres, unpublished catalog candidate, Development-only `LocalCatalogAcceptance` override)

| Level | Request | Result |
|---|---|---|
| `beginner` | race/five_k/3-day | `200`, `template_id=race_5k_beginner_3day_km_v1`, `level=beginner` |
| `used_to_run` (legacy alias) | race/five_k/3-day | `200`, response `level=beginner` (legacy never echoed back) |
| `intermediate` | race/ten_k/4-day (pilot identity) | Routing log shows `route=CatalogRequestUnsupported, reason=UnsupportedCycleLength` — identity **correctly recognized** as the pilot request; failure is an unrelated, pre-existing candidate limitation (race-date-derived cycle length), not caused by this change and out of scope to fix here. |
| `advanced` | race/ten_k/4-day | Routing log shows `route=Legacy, reason=NotPilotRequest` (correctly **not** coerced to the Intermediate pilot); then `404 PLAN_TEMPLATE_NOT_FOUND` (no seeded legacy template for this combination — pre-existing gap, not introduced here). |
| `experienced` | race/ten_k/4-day | Same as Advanced: `route=Legacy, reason=NotPilotRequest`, then `404 PLAN_TEMPLATE_NOT_FOUND`. |
| `not_a_real_level` | any | `400`, `$.level` validation error naming the accepted canonical + legacy values explicitly. |

## 8. Remaining Gaps (reported, not fixed — out of scope for this task)

- No seeded `PlanTemplates` rows exist for Advanced/Experienced legacy-path combinations, so those levels currently 404 on generate-preview outside the (Intermediate-only) catalog pilot. Pre-existing, not a regression.
- Read DTOs (home/calendar/day-detail) still don't expose catalog phase/stage/workout-identity fields — flagged in Phase 4F.9.3, unchanged by this task.
- No changes were made to publication/activation state; the catalog candidate remains `DRAFT` in the real ledger.
