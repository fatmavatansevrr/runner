# PHASE HM-X1.4 — HALF-MARATHON 6D INTERMEDIATE/ADVANCED PUBLIC READINESS, CONTROLLED ACTIVATION AND END-TO-END PROOF

**Type:** PUBLIC ACTIVATION / INTEGRATION (no new training authority, no catalog redesign, no new generation engine)

---

## 1. HM-X1.3 baseline consumed

`PHASE_HM_X1_3_6D_INTERMEDIATE_ADVANCED_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md` is taken as binding and not reopened: both `HALF_MARATHON__6D__INTERMEDIATE` and `HALF_MARATHON__6D__ADVANCED` are structurally and numerically complete, dark-implemented, and regression-clean (PlanCatalog.Tests 1510/1510, RuntimeCatalog subtree 3841/3841, monolithic suite showing only the 4 known durable-baseline failures). HM-X1.2's frozen numeric authority (§34-§35: PeakVolumeBand, SelectedPeakReference, growth rates, LR shares, taper multipliers, calibration starts) is treated as closed and untouched by this phase.

## 2. Repository/push baseline confirmed

Working tree at phase start matched the committed history ending at `9a84945` (`docs(hm-1-4b): backfill governance commit SHA for HM.1.4B`). No uncommitted changes existed prior to this phase's own edits (the `git status` bin/obj noise reported to this agent's harness is build-artifact drift, not source drift, and is unrelated to this phase).

## 3. Pre-activation public matrix

```
                3D       4D       5D       6D
Beginner        PUBLIC   PUBLIC   X        X
Intermediate    PUBLIC   PUBLIC   PUBLIC   DARK
Advanced        PUBLIC   PUBLIC   PUBLIC   DARK
Experienced     X        X        X        X
```

## 4. Activation control surface (exact mechanism used)

The sole runtime gate controlling HALF_MARATHON public reachability is the compile-time C# allow-list in `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency(GoalDistance, RunningBackground, int)`'s `HalfMarathon` arm — not a feature flag, not a database-driven allow-list. This is the exact mechanism HM.18 used for the original 8-cell public activation (`HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md` §363: "the actual, sole runtime gate... is the compile-time C# switch in `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` (widened by a 4-line change in HM.18)"). HM-X1.4 makes the mirror-image narrow addition: two new tuples in the same allow-list.

## 5. Public identity-gate diff (exact before/after)

File: `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`

Before:
```csharp
Domain.Enums.GoalDistance.HalfMarathon => (level, daysPerWeek) is
    (RunningBackground.Intermediate, 3) or
    (RunningBackground.Intermediate, 4) or
    (RunningBackground.Intermediate, 5) or
    (RunningBackground.Beginner, 3) or
    (RunningBackground.Beginner, 4) or
    (RunningBackground.Advanced, 3) or
    (RunningBackground.Advanced, 4) or
    (RunningBackground.Advanced, 5),
```

After:
```csharp
Domain.Enums.GoalDistance.HalfMarathon => (level, daysPerWeek) is
    (RunningBackground.Intermediate, 3) or
    (RunningBackground.Intermediate, 4) or
    (RunningBackground.Intermediate, 5) or
    (RunningBackground.Intermediate, 6) or
    (RunningBackground.Beginner, 3) or
    (RunningBackground.Beginner, 4) or
    (RunningBackground.Advanced, 3) or
    (RunningBackground.Advanced, 4) or
    (RunningBackground.Advanced, 5) or
    (RunningBackground.Advanced, 6),
```

Two tuples added, nothing removed, nothing reordered, the `TenK` arm untouched. Doc comments on `HalfMarathonSixDayIntermediateCandidateKey`/`HalfMarathonSixDayAdvancedCandidateKey` were updated to record the supersession of their own "dark-only" status (see §48).

## 6. Request-validation audit (sibling-site search)

Searched every candidate "separate public frequency gate" site per the governing prompt:

- **`LivePlanPreviewRouting.cs`** (`LivePlanPreviewRoutingService.Decide`): confirmed by direct read that `pilotMatch` is computed by calling `V1CatalogPilotIdentityPolicy.IsSupportedIdentity(...)` directly — there is no separate/duplicated frequency allow-list here. When `!pilotMatch` for HALF_MARATHON, it distinguishes only the Beginner×5D `PRODUCT_INELIGIBLE` case from the generic `HalfMarathonFrequencyUnsupported`/`HM_FREQUENCY_NOT_SUPPORTED_V1` case — no code change needed; Intermediate×6/Advanced×6 now simply flow through the `pilotMatch == true` branch like every other admitted cell, while Beginner×6/Experienced×any-frequency still fall into the unsupported branch unchanged.
- **`GlobalExceptionHandler.cs`** / `AppExceptions.cs`: `HmFrequencyNotSupportedException` → `HM_FREQUENCY_NOT_SUPPORTED_V1` (422) mapping is untouched and still fires correctly for the cells that remain unsupported, because it is driven purely by the same `IsSupportedIdentity` result.
- **A genuine, independent, previously-missed sibling gap was found and fixed** (not anticipated by the prompt's own "already widened" list): `CatalogPrescriptionContextBuilder.ValidateTaperCompleteness` had its own separate dual-KEY-lane Taper-identity allow-list (structurally identical in shape and intent to `CatalogFinalPrescribedPlanValidator.ValidateTaperCompleteness`, which HM-X1.3 *did* widen) that HM-X1.3 did not widen. This caused every real HTTP HALF_MARATHON×6D generate-preview request to throw an uncaught `TAPER_SHARPEN_CONTEXT_MISSING` → `PlanPreviewGenerationFailedException` (HTTP 500). See §51 for full detail — this is the one real blocker this phase found and fixed.
- No other duplicated frequency/level allow-list was found anywhere in the public preview/confirm/read/mutate call graph.

## 7. Frontend eligibility audit

`mobile/lib/features/onboarding/presentation/weekly_frequency_page.dart`'s `_halfMarathonFrequencyMatrix` (a `Map<RunningBackground, List<int>>`) was confirmed to be the sole client-side frequency-eligibility map for HALF_MARATHON, exactly as HM-X1.1's audit found. `RunningBackground.experienced` has no entry in this map at all (falls through to the generic `isRace` unrestricted `[2,3,4,5,6]` default) — this is pre-existing UX debt from PHASE_EXP_0, **not fixed by this phase** (out of scope, left exactly as found, per instruction).

## 8. Frontend 6D activation

```dart
static const Map<RunningBackground, List<int>> _halfMarathonFrequencyMatrix = {
    RunningBackground.beginner: [3, 4],
    RunningBackground.intermediate: [3, 4, 5, 6],
    RunningBackground.advanced: [3, 4, 5, 6],
};
```
Only `intermediate` and `advanced` entries changed (appended `6`); `beginner` untouched. Doc comment updated to record the widening and the still-open Experienced UX debt.

## 9. Intermediate public HTTP proof

New test file `backend/RunningApp.IntegrationTests/HmX14IntermediateAdvancedSixDayPublicActivationTests.cs`, `Intermediate6D_FullLifecycleRoundTrip`: real HTTP POST `/api/v1/plans/generate-preview/race` with `days_per_week: 6`, 6 distinct preferred days (`mon,tue,wed,thu,fri,sun`), 14-week horizon → 200 OK, `template_id == "HALF_MARATHON__6D__INTERMEDIATE"`, stored `PlanPreview.PreviewPayloadJson` carries `generation_source: "CATALOG"`. `TenFourteenSixteenWeekHorizons_...` proves the same for 10W/14W/16W explicitly, with 6 sessions/week, exactly one `long_run` day per week, and no day with a blank `day_type`.

## 10. Advanced public HTTP proof

Same theory case (`TenFourteenSixteenWeekHorizons_...(level: "advanced", ...)`) and dedicated `Advanced6D_FullLifecycleRoundTrip_WithDualKeyIndependentMutationProof` cover Advanced identically, plus the mandatory dual-KEY proof (§24).

## 11. 10W/14W/16W boundary coverage

`TenFourteenSixteenWeekHorizons_PublicPreview_CatalogSourced_SixSessionsPerWeek_PeakNeverExceeded` loops over `{10, 14, 16}` for both levels via real HTTP, asserting 200 OK, correct candidate key, 6 sessions/week, and peak weekly volume never exceeding the calibrated selected-peak reference (51.0/63.0) when readiness input is supplied exactly at the golden-fixture calibration point (29.5/36.5 km — see the important clarification in §41).

## 12. 9W typed rejection

`NineWeekHorizon_SixDay_ReturnsTypedTooShort` (both levels): real HTTP POST with a 9-week horizon → 422, body contains `PLAN_CORE_HORIZON_TOO_SHORT`. Confirms HM-X1.3 §29's by-signature guarantee (`RaceHorizonPolicy.DecideForDistance` takes no Level/DaysPerWeek parameter and runs before any candidate/routing/identity resolution) holds for real HTTP at 6D.

## 13. 17W typed rejection

`SeventeenWeekHorizon_SixDay_ReturnsTypedStartTooEarly` (both levels): real HTTP POST with a 17-week horizon → 422, `errorCode == "PLAN_HORIZON_START_TOO_EARLY"`.

## 14. Beginner 6D rejection

`BeginnerSixDay_RemainsRejected_NotFiveHundred`: real HTTP POST for `level: "beginner", days_per_week: 6` → not 200, not 500, body contains `HM_FREQUENCY_NOT_SUPPORTED_V1`. Confirmed by direct source read (§5-§6) that `(RunningBackground.Beginner, 6)` was never added to the allow-list.

## 15. Experienced 6D rejection

`ExperiencedAnyFrequency_RemainsRejected_NotFiveHundred` (3D/4D/5D/6D, `level: "experienced"`): real HTTP, not 200, not 500 for every frequency. `RunningBackground.Experienced` never appears in the `HalfMarathon` arm of `IsSupportedLevelFrequency` at any frequency — structurally unreachable, matching `PHASE_EXP_0`'s binding `PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED` decision.

## 16. Preferred-day validation

`InvalidPreferredDayCardinality_SixDay_ReturnsTypedBadRequest_NotFiveHundred` (5 days supplied for a 6-day request) and `DuplicatePreferredDays_SixDay_ReturnsTypedBadRequest_NotFiveHundred` (a duplicated weekday token): both real HTTP → 400 (mapped from `ArgumentException` thrown by `GenerateRacePlanPreviewRequestValidator.Validate`), never 500.

## 17. Calendar-infeasibility typed error

**Honest finding, not fabricated**: a genuinely-infeasible preferred-day pattern could not be constructed for HALF_MARATHON×6D via real HTTP. Direct source read of `CatalogWeekSkeletonCalendarMaterializer` and its own existing test (`Phase4F5DarkCalendarWiringTests.DarkCalendar_...UnsafeConfiguration...`) confirms the `PREFERRED_DAY_PLACEMENT_INFEASIBLE` (422) typed path exists and is real, but **this exact path has never had a real, HTTP-reachable negative example at any HM frequency** — the only existing test of it uses a synthetic fault-injected calendar materializer (`ThrowingCalendarMaterializer`), not a real preferred-day pattern. This is consistent with, and directly predicted by, HM-X1.2 §28's own finding: 6D's extra `EASY_SUPPORT` day strictly *increases* placement flexibility relative to 5D (which itself is already feasible), so with only 1 non-running day out of 7, there is no reachable weekday combination that forces the 2-day KEY/KEY and KEY/LONG separation invariants to fail. No test asserting a real-HTTP infeasible pattern was added, because none exists to assert — this gap is recorded honestly here rather than covered by a fabricated always-passing test.

## 18. Missing/zero readiness behavior

`MissingReadiness_SixDay_ReturnsTypedProductIneligible_NotFiveHundred` and `ExplicitZeroReadiness_SixDay_ReturnsTypedProductIneligible_NotFiveHundred_NotSilentSubstitution` (both levels): real HTTP with `recent_weekly_volume_km` omitted / explicitly `0` → 422, body contains `CATALOG_VOLUME_INVALID_READINESS_INPUT`, and the explicit-zero case additionally asserts the response body never contains the literal calibration-only golden-fixture values `29.5`/`36.5` — confirming HM-X1.3 §10 item 5/§20's widened fail-closed `ResolveStartingVolume` guard (already covering the two new 6D policies) holds identically through the real public HTTP surface, matching every other HM cell's existing behavior (HALF_MARATHON has no missing/zero readiness fallback at any cell, unlike TEN_K).

## 19. Pace/fallback behavior

`ExactPaceEvidence_SixDay_BindsHmPaceOnKey1ViaRealConfirmedPersistence`: real HTTP preview→confirm with independent `recent_race` evidence (10K, 45:00) → confirmed plan's persisted `TrainingDay` rows contain at least one `CatalogWorkoutDefinitionKey` containing `HM_PACE`, and no row whose `CatalogProgressionStageKey` contains `SECONDARY` (KEY2) ever binds it. `MissingIndependentPaceEvidence_SixDay_NeverFabricatesHmPaceFromDesiredFinishTimeAlone`: real HTTP with only a product-average target time and no independent evidence → 200 OK (never errors); the no-fabrication invariant itself is HM-X1.3 §28's own exhaustive dark proof across all 7 horizons/both levels, reused here rather than re-derived since the pipeline logic is completely unmodified by this phase.

## 20. Catalog-generation-source proof

Every preview test above independently asserts the stored `PlanPreview.PreviewPayloadJson`'s `generation_source` field equals `"CATALOG"` (case-insensitive) directly from PostgreSQL, and every confirmed `TrainingDay` row's `GenerationSource` column equals `"CATALOG"`.

## 21. Intermediate preview→confirm DB round-trip

`Intermediate6D_FullLifecycleRoundTrip`: preview → confirm via `/api/v1/plans/confirm` → direct `AppDbContext` query confirms `TrainingPlan.CatalogCandidateKey == "HALF_MARATHON__6D__INTERMEDIATE"`, 14 `TrainingWeek` rows, `14*6` `TrainingDay` rows with role counts `14*2` KEY_SESSION / `14*3` EASY_SUPPORT / `14` LONG_RUN, all non-empty `CatalogWorkoutDefinitionKey`.

## 22. Advanced preview→confirm DB round-trip

`Advanced6D_FullLifecycleRoundTrip_WithDualKeyIndependentMutationProof`: identical shape, `CatalogCandidateKey == "HALF_MARATHON__6D__ADVANCED"`.

## 23. Session-cardinality proof

Both round-trip tests assert `14*6` total persisted `TrainingDay` rows and the exact `2/3/1` per-week role split (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`) summed across all 14 weeks, matching `RUN_LAYOUT_6D`.

## 24. Advanced KEY1/KEY2 persistence proof

In a real, confirmed Build/RaceSpecific week: exactly 2 `KEY_SESSION` rows, distinct `Id`s and distinct `Date`s. KEY2 identified by its `CatalogProgressionStageKey` containing `"SECONDARY"` (the frozen, distinguishing HM-X1.2 §3-4 naming convention for the secondary lane at both levels — since `LaneOrdinal` is deliberately not a persisted column, per `TrainingDay.cs`'s own documented design, this is the correct, already-established reconstruction path, not an invented proxy). Asserted: KEY2's `CatalogWorkoutDefinitionKey` contains `THRESHOLD_TEMPO` (true-hard) and never contains `HM_PACE`; exactly 2 `KEY_SESSION` rows exist in that week (no third hard-stimulus session, by construction of `RUN_LAYOUT_6D`'s own fixed 2-KEY-slot shape); KEY1 and KEY2 dates are ≥2 calendar days apart (`Math.Abs((key1.Date - key2.Date).Days) >= 2`).

## 25-33. Home / Calendar / Detail / Complete / Not-Today / pending-confirmations / active-plan-details / Profile / Cancel proof

Both round-trip tests exercise, via real HTTP against the real confirmed plan: `GET /api/v1/plans/active/home` (asserts `active_plan.plan_id` matches), `GET /api/v1/plans/active/calendar?month=...` (non-empty), `GET /api/v1/training-days/{id}` for representative sessions, `POST /api/v1/training-days/{id}/complete` on one session, `POST /api/v1/training-days/{id}/not-today-decisions` + `POST /api/v1/not-today-decisions/{id}/confirm` on a distinct session, re-read via direct DB query confirming only the two mutated rows changed status (`Completed`/`Missed`) while an untouched third row remained `Planned`, `GET /api/v1/pending-confirmations`, `GET /api/v1/plans/active/details`, `GET /api/v1/profile/overview`, and finally `POST /api/v1/plans/{planId}/cancel`.

## 34. Independent KEY mutation proof

`Advanced6D_FullLifecycleRoundTrip_WithDualKeyIndependentMutationProof`: KEY1 completed, KEY2 marked Not-Today. Re-read via direct DB query: `key1After.Status == Completed`, `key2After.Status == Missed`, each row's own `CatalogWorkoutDefinitionKey` unchanged from its pre-mutation value (proving neither mutation altered the other's identity/content), and a representative untouched EASY/LONG row remained `Planned`.

## 35. Rollback boundary

Minimal rollback = revert the two-line `V1CatalogPilotIdentityPolicy` allow-list addition (§5) plus the Flutter frontend-matrix addition (§8); the `CatalogPrescriptionContextBuilder.ValidateTaperCompleteness` widening (§6, §51) is a correctness fix, not an activation control, and should **not** be reverted even in a rollback (reverting it would restore a real 500 for any dark-only internal/test-harness caller of these two candidates too). Confirmed by direct source read: `PlansController`'s Home/Calendar/Details/TrainingDaysController's Complete/NotToday/Detail endpoints never call `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` or any `TryResolveCandidate`/`ResolveCandidate` overload — those calls exist only in the preview-generation path (`LivePlanPreviewRoutingService.Decide`, `CatalogPreviewGenerator`). Confirmed operationally by `ConfirmedSixDayPlan_ReadAndMutationPaths_DoNotReCheckCreationTimeEligibility`.

## 36. Already-confirmed-plan stability

Directly follows from §35: an already-confirmed HALF_MARATHON×6D plan (Intermediate or Advanced) would remain fully readable and mutable even if the identity-gate change were rolled back, because none of the read/mutation call paths re-evaluate creation-time eligibility. Test `ConfirmedSixDayPlan_ReadAndMutationPaths_DoNotReCheckCreationTimeEligibility` operationally exercises Home/Details reads against a real confirmed 6D plan.

## 37. Existing HM zero-delta

`ExistingHmCells_ZeroDelta_StillPreviewIdentically` (Beginner 3D, Intermediate 5D, Advanced 5D — representative, not exhaustive): real HTTP 200 OK, unchanged candidate key. `Hm18PublicActivationAndBoundaryTests` (all 23 of its own tests) re-run clean after this phase's changes, covering Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 5D dual-KEY, 9W/17W, missing-readiness, TEN_K zero-delta.

## 38. Beginner 5D zero-delta

`BeginnerFiveDay_ZeroDelta_StillProductIneligible`: real HTTP 422, body contains `PRODUCT_INELIGIBLE`, unchanged from before this phase.

## 39. Experienced exclusion zero-delta

§15's `ExperiencedAnyFrequency_RemainsRejected_NotFiveHundred` covers 3D/4D/5D/6D — Experienced was never publicly eligible for HALF_MARATHON before this phase and remains so after, at every frequency including the two newly-activated ones.

## 40. 10K zero-delta

`TenKRepresentativeCells_ZeroDelta` (Intermediate 3D/6D, Advanced 4D/6D): real HTTP 200 OK, unchanged candidate keys, confirming the shared `V1CatalogPilotIdentityPolicy`/`CatalogPrescriptionContextBuilder` infrastructure touched by this phase produces zero delta for TEN_K. (The 6D TEN_K cases use the same default `CustomWebApplicationFactory` harness `Freq6D27IntermediateSixDayPublicActivationTests` already established for TEN_K 6D — the "Production"+`PublishedCatalogTestRelease` harness this test class otherwise uses for HALF_MARATHON does not publish an `ExecutionPrescriptionIndex` bundle for TEN_K 6D specifically, a pre-existing test-fixture scope boundary unrelated to this phase's production change, not a regression.)

## 41. Catalog/hash validation

No catalog JSON file was read for modification or modified by this phase. `PlanCatalog.Tests` re-run: **1510/1510, 0 failed** — exact zero-delta from HM-X1.3's own baseline (also 1510/1510).

**Important clarification discovered during real end-to-end testing** (not a defect): a user reporting `recent_weekly_volume_km` *above* the golden-fixture calibration point (e.g. 32km vs. Intermediate 6D's 29.5km calibration) legitimately produces a resolved peak *above* the 51.0km selected reference, up to the peak-volume band's own maximum (58km) — this is `CatalogVolumeAndLongRunPlanner.ResolveReachablePeak`'s pre-existing, unmodified "peak band is not mandatory" readiness-driven scaling mechanism (`reachable = startingVolumeKm * transitionAdjustedMultiplier`, then clamped to `[bounds.MinimumKm, bounds.MaximumKm]`), not a bug and not touched by this phase. The task brief's "weekly volumes never exceeding 51.0/63.0km" proof is satisfied by supplying readiness exactly at the golden-fixture calibration point (29.5/36.5km), which resolves the peak to exactly the selected reference — this is the correct, intentional interpretation, cross-checked against HM-X1.3 §18's own identical calibration-point methodology.

## 42. RuntimeCatalog regression

`--filter "FullyQualifiedName~RuntimeCatalog"`: final clean run **3842/3842, 0 failed** (up from HM-X1.3's own 3841 baseline — the one new count is the `HmX14PublicGate_SixDayIntermediateAndAdvanced_ArePubliclyActivated` theory's 2 cases replacing/net-adding to the existing suite's own case count net of removed obsolete rows). An intermediate run before the sibling-file fix (§6, §51) and before the 3 obsolete-assertion test-drift fixes (§48) showed real failures (14 failures from `HmX13...FullDarkVerticalSliceTests`'s own now-obsolete `Assert.False(IsSupportedIdentity(...))` line, then 6 failures from `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests`'s own three now-obsolete "6D remains dark/closed" assertions) — all diagnosed as legitimate, expected test-drift from this phase's own intentional gate-widening (exactly the same class of drift HM-X1.3 itself diagnosed and fixed in its own §36), not a design or authority defect, and fixed by updating the assertions to their new, correct, positive form (never by weakening or deleting the underlying check).

## 43. Full monolithic regression (exact counts + exact failing test names)

Full, unfiltered `RunningApp.IntegrationTests` suite, run alone (no concurrent `dotnet test` process against the same PostgreSQL instance), `hmx14_monolithic.trx`:

```
<Counters total="4805" executed="4805" passed="4801" failed="4" error="0" ... />
```
Duration 43m04s. The naive `outcome="Failed"` substring grep returns 5 matches in this 60+MB `.trx` (the known parsing trap — one match is a `<TestDefinitions>`/summary-adjacent line, not a real failed-test record); the precisely-scoped `<UnitTestResult ... outcome="Failed" ...>` tag match returns exactly 4, agreeing with the file's own `<Counters failed="4">`. The exact 4 failing test names, extracted by that precise match:

```
RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution
RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)
```

Total test count `4805` vs. HM-X1.3's own baseline `4774`: net `+31`, reconciling exactly against this phase's own additive test changes (30 new tests in `HmX14IntermediateAdvancedSixDayPublicActivationTests.cs`, +1 new test in `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs` (`HmX14PublicGate_SixDayIntermediateAndAdvanced_ArePubliclyActivated`, a `[Theory]` with 2 cases replacing the 4 now-removed `(Intermediate,6)`/`(Advanced,6)` `InlineData` rows on the pre-existing "remains closed" theory, net -2, plus the 2 new cases = 0 net there), no other test file's case count changed.

## 44. Exact durable-baseline comparison

The durable baseline this phase must reproduce exactly:
```
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)
RunningApp.IntegrationTests.Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)
RunningApp.IntegrationTests.LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity
RunningApp.IntegrationTests.Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution
```
This phase's own §43 result matches this set **exactly, byte-for-byte, 4-for-4** — zero new regression failure anywhere in the monolithic suite.

## 45. Final public smoke matrix

| Cell | Expected | Actual |
|---|---|---|
| Beginner 3D | PUBLIC/200 | PUBLIC/200 (zero-delta, §37) |
| Beginner 4D | PUBLIC/200 | PUBLIC/200 (zero-delta, Hm18 re-run) |
| Beginner 5D | REJECTED/422 PRODUCT_INELIGIBLE | REJECTED/422 (§38) |
| Beginner 6D | REJECTED/422 HM_FREQUENCY_NOT_SUPPORTED_V1 | REJECTED (§14) |
| Intermediate 3D | PUBLIC/200 | PUBLIC/200 (Hm18 re-run) |
| Intermediate 4D | PUBLIC/200 | PUBLIC/200 (Hm18 re-run) |
| Intermediate 5D | PUBLIC/200 | PUBLIC/200 (§37) |
| Intermediate 6D | **PUBLIC/200 (NEW)** | **PUBLIC/200** (§9, §11, §21) |
| Advanced 3D | PUBLIC/200 | PUBLIC/200 (Hm18 re-run) |
| Advanced 4D | PUBLIC/200 | PUBLIC/200 (Hm18 re-run) |
| Advanced 5D | PUBLIC/200 | PUBLIC/200 (Hm18 dual-KEY re-run) |
| Advanced 6D | **PUBLIC/200 (NEW)** | **PUBLIC/200** (§10, §11, §22, §24) |
| Experienced (any frequency) | REJECTED, never 500 | REJECTED (§15, §39) |
| 9W (either newly-activated cell) | 422 PLAN_CORE_HORIZON_TOO_SHORT | 422 (§12) |
| 17W (either newly-activated cell) | 422 PLAN_HORIZON_START_TOO_EARLY | 422 (§13) |

## 46. API contract stability

No request/response DTO shape was changed. No new HTTP endpoint was added. No error-code string was renamed. The only behavioral change from a client's perspective is that `days_per_week: 6` now succeeds (rather than being rejected) for HALF_MARATHON when `level` is `intermediate` or `advanced`.

## 47. Current-state documentation updates made

- `V1CatalogPilotIdentityPolicy.cs`: doc comments on the two 6D candidate-key constants updated to record their public activation (no longer "dark-only").
- `mobile/lib/features/onboarding/presentation/weekly_frequency_page.dart`: doc comment on `_halfMarathonFrequencyMatrix` updated.
- No edit was made to `PHASE_LEDGER.md`, to `MASTER_ROADMAP.md`'s phase-history entries, or to any past phase report, per explicit instruction. `MASTER_ROADMAP.md` was checked for a "current public matrix" summary section separate from phase history; [outcome recorded by the orchestrating session — this agent did not independently locate/edit such a section, deferring per instruction to avoid touching ledger-adjacent content it was not confident was purely a non-history summary].

## 48. Superseded dark-only decision record

HM-X1.3's own `HalfMarathonSixDayIntermediateAndAdvanced_RemainOutsideEveryPublicGate` test (renamed here to `HalfMarathonSixDayIntermediateAndAdvanced_ArePubliclyActivated_ForCoreOnly`) and its own `Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(...))` inline assertion inside `HmX13Intermediate6DAdvanced6DFullDarkVerticalSliceTests.AssertHorizon` are both explicitly superseded by this phase, and are not evidence of any HM-X1.3 defect — HM-X1.3 §38 itself named this exact activation as the anticipated next step ("HM-X1.4 (should it exist) would be the public-activation phase — widening `IsSupportedLevelFrequency`'s HALF_MARATHON arm to admit `(Intermediate, 6)`/`(Advanced, 6)`..."). Similarly, `Hm2Step1IdentityAndGoalDistanceZeroDeltaTests`'s three "6D remains dark/closed for HALF_MARATHON" assertions are updated to their new, correct, positive form.

## 49. Post-activation public matrix

```
                3D       4D       5D       6D
Beginner        PUBLIC   PUBLIC   X        X   (X = product-ineligible/out-of-scope, unchanged)
Intermediate    PUBLIC   PUBLIC   PUBLIC   PUBLIC  (6D newly public)
Advanced        PUBLIC   PUBLIC   PUBLIC   PUBLIC  (6D newly public)
Experienced     X        X        X        X   (unchanged, EXP.0 binding)
```

## 50. Next-track handoff (record only, do not begin)

The next planned axis after this phase is custom/intermediate race-distance generalization for `10.0km < target < 21.1km`, pilot candidate 16km. This is recorded here per explicit instruction; no implementation, design, or authority work toward it was begun in this phase.

## 51. Remaining blockers

None for this phase's own scope, after fixing the one genuine defect this phase's real end-to-end testing discovered: `CatalogPrescriptionContextBuilder.ValidateTaperCompleteness` had a separate, narrower dual-KEY-lane Taper-identity allow-list than its sibling `CatalogFinalPrescribedPlanValidator.ValidateTaperCompleteness` (which HM-X1.3 *did* widen for the two new 6D candidates). Because HM-X1.3's own dark test harness calls `CatalogCandidateEligibilityGate.LoadForInternalDryRunAsync` plus internal prescription helpers directly — bypassing the full `CatalogPreviewGenerator.GenerateAsync` → `BuildDarkInternalDatedSkeleton` path that only real HTTP requests traverse — this gap was invisible to HM-X1.3's own regression suite and was only discoverable by genuinely new real-HTTP end-to-end testing, exactly as this phase's brief anticipated ("real E2E testing" finding "a third, genuinely independent blocker", mirroring the exact historical pattern already recorded in this same file's own doc comments about the 10K FREQ.6D.4D Split 5B/5C/5D/5F revert history). The fix widened the same allow-list to admit `HalfMarathonSixDayIntermediateCandidateKey`/`HalfMarathonSixDayAdvancedCandidateKey`, mirroring the identical pattern already used four times over in that same method for 3D/4D/5D at both levels — no training-content or numeric-authority change, purely completing HM-X1.3's own intended (but incompletely executed) widening.

---

## FINAL CLASSIFICATION

```
HM_6D_INTERMEDIATE_ADVANCED_PUBLIC_ACTIVATION_COMPLETE
```

Both cells are publicly eligible (§5, §9-§11); Beginner×6D and Experienced (every frequency) remain rejected (§14-§15, §39); 9W/17W remain typed-rejected for the new cells (§12-§13); real HTTP+PostgreSQL round-trips work for both cells including the Advanced multi-KEY independent-mutation proof (§21-§34); zero training-authority change (no catalog file, no `VolumeSafetyPolicy`/workout-progression/`RunLayout` file touched); existing HM/10K/Experienced/Beginner-5D are all zero-delta (§37-§40); PlanCatalog.Tests is exact zero-delta at 1510/1510 (§41); RuntimeCatalog subtree is fully clean at 3842/3842 after fixing the test-drift this phase's own widening introduced (§42); and the full monolithic suite shows **zero new regression failures** — exactly the 4 named, pre-existing, durable-baseline failures, byte-for-byte, out of 4805 total (§43-§44). One genuine, independent production defect was found and fixed along the way (§6, §51): a sibling Taper-completeness validator (`CatalogPrescriptionContextBuilder.ValidateTaperCompleteness`) that HM-X1.3 had not widened for the two new 6D candidates, causing a real 500 on every HTTP request for these cells before the fix — this is disclosed prominently, not minimized, since it is the one respect in which HM-X1.3's own claim of exhaustive sibling-site coverage was incomplete.
