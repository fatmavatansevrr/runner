# PHASE DIST-GEN.17 — 16K × Intermediate × 3D Public Readiness, Controlled Activation, and End-to-End Frequency-Orthogonality Proof

## 1. DIST-GEN.16 baseline consumed

Read in full. DIST-GEN.16 closed with the dark, materializable, typed
`ProjectedTargetAuthorityRegistry` cell `(TargetDistanceKm=16.0,
ParentDistanceFamily=HalfMarathon, Level=Intermediate, RunsPerWeek=3)`,
reusing the exact numeric authority frozen by DIST-GEN.15/15B: peak-volume
band 28.0/40.0, selected peak 34.0, calibration starting volume 20.0,
absolute weekly increase cap 2.0, LR shares 0.34/0.40/0.38/0.46,
`PreferredAbsolutePeakLongRunKm`=15.0, horizon 10/12/14 (min/preferred/max),
2-week non-chained taper (0.70/0.43), 1 KEY + 1 EASY + 1 LONG RunLayout. Its
own accepted regression baseline (confirmed still current in §79-83 below):
`PlanCatalog.Tests` 1510/1510; `RuntimeCatalog` subtree 4042/4042; full
monolithic suite 5146 total/5142 passed/4 known durable failures.

## 2. Repository baseline

Read `PHASE_DIST_GEN_15B_16K_INTERMEDIATE_3D_PEAK_VOLUME_EXACT_CELL_AUTHORITY_PRODUCT_DECISION.md`
(numeric-authority source of truth) and
`PHASE_DIST_GEN_7_15K_INTERMEDIATE_4D_PUBLIC_READINESS_CONTROLLED_ACTIVATION_AND_END_TO_END_PROOF.md`
(closest public-activation precedent). Confirmed
`GeneratePreviewCommandMapper.ToInternalRequest` already calls
`Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell(targetDistanceKm,
resolution.CanonicalDistanceFamily, raceCommand.Level,
raceCommand.DaysPerWeek)` — `DaysPerWeek` was already a full participant in
the public gate before this phase began. This file needed, and received,
zero code changes.

## 3. Files changed

Production (1 file):
- `backend/RunningApp.Application/RuntimeCatalog/TargetDistanceProjection/Dark16KPilotEligibilityPolicy.cs`
  — added `(16.0, HalfMarathon, Intermediate, 3)` to `ApprovedPublicCells`
  (now 4 entries); updated adjacent doc comments to describe the new state
  and the registry-separation discipline. `ApprovedDarkCells` untouched.

Tests (1 new file, 3 amended files):
- `backend/RunningApp.IntegrationTests/DistGen17PublicActivationTests.cs`
  (new) — the full real-HTTP/PostgreSQL public-activation proof suite.
- `backend/RunningApp.IntegrationTests/DistGen3PublicActivationTests.cs`
  (amended) — removed `days: 3` from `Pilot16K_WrongFrequency_Rejected`'s
  negative matrix, since 16K×Intermediate×3D is no longer a wrong-frequency
  rejection case; `5`/`6` remain rejected.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/Dark18KTargetDistanceProjectionTests.cs`
  (amended) — updated `ApprovedPublicCells.Count` assertion from 3 to 4 and
  added explicit `RunsPerWeek` discrimination in its `Contains` assertions.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/TargetDistanceProjection/DistGen16Intermediate3DFullDarkImplementationTests.cs`
  (amended) — `PublicRegistry_Never16KI3D_DarkOnly` renamed to
  `PublicRegistry_16KI3D_NowPublic_AsOfDistGen17` and rewritten to assert the
  new, correct state (4 public entries, one at RunsPerWeek=3).

No catalog JSON, no migration, no database schema, no training-authority
policy class, no frontend file was touched. See §85 for the full diff audit.

## 4. Current dark/public matrix before activation

Before this phase: `ApprovedDarkCells` = {16.0×I×4D, 15.0×I×4D, 18.0×I×4D,
16.0×I×3D} (4 entries, frozen by DIST-GEN.1/2, 5/6, 9/10, 15/15B/16).
`ApprovedPublicCells` = {16.0×I×4D, 15.0×I×4D, 18.0×I×4D} (3 entries, frozen
by DIST-GEN.3, 7, 11). 16.0×I×3D was dark-materializable but publicly
unreachable.

## 5. Public eligibility ownership

Confirmed by direct inspection: the sole public canonicalization gate is
`GeneratePreviewCommandMapper.ToInternalRequest`, consulting
`Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell`. No other
call site decides public target-distance eligibility. Unchanged by this
phase.

## 6. Exact 16K I3D public registration

```csharp
new(16.0, GoalDistance.HalfMarathon, RunningBackground.Intermediate, 3), // DIST-GEN.17
```
Added to `ApprovedPublicCells`. Exact-match lookup only (0.001km tolerance),
per the class's established pattern — never a computed range, never a band.

## 7. Same-target two-frequency public key proof

`DistGen17PublicActivationTests.SameTarget_TwoFrequencies_16KIntermediate3DAnd4D_BothPublic_GenuinelyDistinctAuthority`
issues two real HTTP requests differing only in `days_per_week` (3 vs 4) for
the identical `target_distance_km=16.0, level=intermediate`. Both return 200.
Both report `requested_target_distance_km=16.0`. RunLayout differs (3 vs 4
sessions/week). Peak volume differs (≤34.0 vs ≤40.0). Peak long run is
identical (≤15.0 for both) — a genuine frequency-invariance finding, not
cross-contamination. This directly proves `days_per_week` (not merely
`target_distance_km`) participates in public eligibility.

## 8. Public/dark separation

`ApprovedPublicCells` and `ApprovedDarkCells` remain two independently
declared `static readonly` fields in source — neither is computed from the
other. `PublicAndDarkRegistries_BothNowFourEntries_ButRemainIndependentlyDeclaredLists`
confirms both now hold 4 entries (a numeric coincidence, explicitly not a
structural relationship) via `ReferenceEquals` inequality and by asserting
every public cell also appears in the dark list (expected: activation always
reuses an already-dark cell) without deriving one list from the other.

## 9. Structural identity

16K I3D public requests resolve `CanonicalDistanceFamily=HALF_MARATHON`,
reuse the `HALF_MARATHON×Intermediate×3D` catalog candidate identity, and
produce exactly 3 sessions/week (1 KEY_SESSION + 1 EASY_SUPPORT + 1
LONG_RUN) — confirmed across all horizon tests.

## 10. Catalog candidate

`V1CatalogPilotIdentityPolicy.HalfMarathonThreeDayIntermediateCandidateKey`
— the same, pre-existing, already-published HALF_MARATHON×3D×Intermediate
catalog candidate DIST-GEN.16 already reused. No new candidate, no catalog
JSON change.

## 11. Horizon public contract

10/11/12/13/14 core weeks succeed publicly; 9 and 15 are rejected with this
cell's own typed `DARK_16K_I3D_CORE_HORIZON_{weeks}_NOT_SUPPORTED` reason
code (unchanged from DIST-GEN.16's dark authority — public activation only
changes reachability, never the horizon contract).

## 12. 9W rejection

`NineWeekHorizon_16KI3D_TypedRejection_NotFiveHundred_NotFallback`: real
public HTTP returns 422 (`UnprocessableEntity`), never 500, body contains
`DARK_16K_I3D_CORE_HORIZON`, never `template_id`, never
`TARGET_BELOW_SUM_OF_MINIMUMS`. Passed.

## 13-17. 10W/11W/12W/13W/14W public proofs

`EligibleHorizons_16KI3D_PublicPreview_Succeeds_WithCorrectIdentityAndVolume`
(Theory over 10/11/12/13/14): each returns 200, `requested_target_distance_km=16.0`,
`goal_distance=half_marathon`, exactly `weeks` week entries, exactly 3
days/week per week, peak volume ≤34.0km, all long runs <16.0km and ≤15.0km.
All 5 horizons passed. 12W is additionally covered as the preferred-horizon
golden trace in §19.

## 18. 15W rejection

`FifteenWeekHorizon_16KI3D_TypedRejection_UnderOwnMaximumAuthority`: 422,
body contains `DARK_16K_I3D_CORE_HORIZON`, never `template_id`. Passed.

## 19. Dark/public golden comparison

`DarkVsPublic_16KI3D_12W_SemanticZeroDelta_OnlyReachabilityChanged`: real
public 12W preview matches DIST-GEN.16's own dark real-pipeline golden trace
— peak volume ≤34.0, peak long run ≤15.0, exactly 3 sessions/week, 12 total
weeks. Semantic zero-delta confirmed; only reachability changed.

## 20. Peak band

28.0/40.0 (min/max), reused unchanged from `TargetDistance16KIntermediate3DPeakVolumeBandPolicy`
(DIST-GEN.15B/16 frozen authority). Not touched by this phase.

## 21. Selected peak

34.0 (exact-cell literal, DIST-GEN.16's own independent instance — value-equal
to canonical HM I3D's 34.0 but never the same policy object). Not touched.

## 22. Calibration

`CalibrationStartingVolumeKm=20.0` (exact-cell literal). Not touched.

## 23. Growth rates

`PreferredWeeklyIncreaseRate=0.07`, `HardWeeklyIncreaseRate=0.08`. Not
touched.

## 24. Absolute weekly cap

`AbsoluteWeeklyIncreaseCapKm=2.0` — frequency-owned, genuinely different
from 16K I4D's own 2.5. Confirmed in `GoldenTrace_AllSupportedHorizons_MaterializeWithFrozen16KI3DNumerics`
(pre-existing DIST-GEN.16 test, still passing) and re-confirmed in §7's own
cross-frequency proof.

## 25. LR shares

0.34/0.40/0.38/0.46 — parent-family-owned, genuinely different from 16K
I4D's own 0.30/0.36/0.33/0.40. Not touched by this phase.

## 26. Absolute peak LR

`PreferredAbsolutePeakLongRunKm=15.0` — target-owned, identical to 16K I4D's
own 15.0, a real closed frequency-invariance finding. Re-confirmed at the
real public HTTP layer in §7 (both frequencies' long runs cap at ≤15.0km).

## 27. Starting LR cap

`StartingAbsoluteLongRunCapKm=null`. Not touched.

## 28. RunLayout

KEY_SESSION, EASY_SUPPORT, LONG_RUN — exactly 1 each, confirmed at every
public horizon (`Assert.Equal(3, w!["days"]!.AsArray().Count)` across all
weeks, all horizons, all lifecycle tests).

## 29. Taper

2-week non-chained taper (0.70/0.43 multipliers), referenced from the shared
`HalfMarathonParentTaperAuthority` component. Not touched; verified
unaffected via the pre-existing (still-passing) DIST-GEN.16 golden-trace
taper assertions.

## 30. Pace

Target-race-pace mechanism is fully generic; not touched. `TargetDistanceKm=16.0`
drives Riegel/pace derivation identically at both frequencies (§7).

## 31. Riegel

Fully generic Riegel projection; zero new code; unaffected.

## 32. Target-time fitness separation

Unaffected; generic mechanism, no target-distance- or frequency-specific
branching exists in this layer.

## 33. Readiness

Fully generic readiness/calendar/adaptation mechanisms; zero new code;
unaffected by this phase.

## 34. Frontend frequency eligibility

Investigated `mobile/lib/features/onboarding/presentation/weekly_frequency_page.dart`.
Its frequency options are gated by
`isHalfMarathon = onboardingState.goalDistance == 'half_marathon'` — for a
Custom/projected target (16K, 15K, 18K all set `goal_distance: 'custom'`),
`isHalfMarathon` is `false`, so the page falls through to the generic race
options `[2, 3, 4, 5, 6]`, which already includes 3. There is no separate
frequency gate for projected targets anywhere in the onboarding flow. **No
frontend change was needed or made** — the UI already let a user pick 3
days/week for a Custom 16K target before this phase; only the backend's own
registry determined whether that combination would succeed or be rejected.

## 35. Frontend target zero-delta

`race_details_page.dart`'s `_approvedProjectedTargetsKm = [15.0, 16.0, 18.0]`
list (target-distance admission, orthogonal to frequency) is untouched —
confirmed via `git status`/diff: no frontend file appears in this phase's
diff at all.

## 36. Frontend projected-cell matrix

No frontend eligibility matrix exists that couples target distance to
frequency (unlike `weekly_frequency_page.dart`'s own `_halfMarathonFrequencyMatrix`,
which only applies when `goalDistance == 'half_marathon'`, never `'custom'`).
Nothing to widen or narrow for this phase.

## 37. Request payload

Real public payload used throughout: `goal_distance: "custom",
target_distance_km: 16.0, level: "intermediate", days_per_week: 3` plus the
same shape every other race request already carries (`preferred_days`,
`long_run_day`, `race_date`, `target_finish_time_seconds`, etc.) — no new
field, no new DTO shape.

## 38. Preview response

Confirmed shape: `requested_target_distance_km=16.0`, `goal_distance=half_marathon`,
`weeks[]` with `days[]` (3 per week), `day_type=long_run` for the long-run
day, `distance_km` per day. Identical response schema to every other
public/dark target-distance preview.

## 39. Preview→confirm

`TwelveWeek_16KI3D_FullLifecycle_IdentitySurvivesEveryStep`: preview → real
`POST /api/v1/plans/confirm` → `plan_id` returned → persisted plan confirmed
with matching identity. Passed.

## 40. PostgreSQL persistence

Same test: `db.TrainingPlans` query confirms
`CanonicalDistanceFamily="HALF_MARATHON"`, `RequestedTargetDistanceKm=16.0`,
`Weeks.Count=12`, all weeks with 3 days, via the real PostgreSQL-backed
`AppDbContext` (via `CustomWebApplicationFactory`). Passed.

## 41. Same-target cross-frequency persistence

`SameTargetCrossFrequency_16KI3DAndI4D_BothPersistIndependently`: two
separately confirmed plans (3D then 4D, same target distance) persist as two
distinct `TrainingPlans` rows with distinct IDs, both `RequestedTargetDistanceKm=16.0`,
`CanonicalDistanceFamily="HALF_MARATHON"`, correctly differing session
counts (3 vs 4 days per week). Passed.

## 42. Home

`GET /api/v1/plans/active/home` returns `active_plan.requested_target_distance_km=16.0`
for the confirmed 3D plan. Passed (§39 lifecycle test).

## 43. Calendar

`GET /api/v1/plans/active/calendar?month=2026-07` returns a non-empty
calendar for the confirmed 3D plan. Passed.

## 44. Detail

`GET /api/v1/plans/active/details` returns `requested_target_distance_km=16.0`,
`goal_distance=half_marathon`. Passed.

## 45. PlanDetails

Same endpoint as §44 in this codebase's naming (`active/details`); covered
identically. Passed.

## 46. Profile/overview

No profile/overview-specific target-distance rendering exists distinct from
Home/Details in this codebase; both already covered (§42/§44).

## 47. Complete

`POST /api/v1/training-days/{id}/complete` on the confirmed 3D plan's first
day succeeds (200), and the day's status transitions to `Completed` in
Postgres. Passed.

## 48. Not Today

`POST /api/v1/training-days/{id}/not-today-decisions` on the second day
succeeds (200). Passed.

## 49. Pending confirmation

Preview persists to `PlanPreviews` prior to confirm (implicit in the
row-count assertions of the custom-without-target safety test, §56);
confirm converts it to a `TrainingPlans` row. No separate pending-state
proof was required beyond the standard confirm flow already exercised.

## 50. Cancel

`POST /api/v1/plans/{planId}/cancel` on the confirmed 3D plan returns 200.
Passed (§39 and §74 rollback test both exercise this).

## 51. Same-target cross-frequency public trace

Covered by §7 and §41 together: the same target distance (16.0) reached
publicly at two different frequencies, both producing genuinely distinct,
correctly separated authority and persistence.

## 52. 15K I3D negative

`UnsupportedTargetDistances_AtThreeDays_StillRejected` (InlineData 15.0):
rejected, `UNSUPPORTED_TARGET_DISTANCE`, never 200, never 500. Passed.

## 53. 18K I3D negative

Same test, InlineData 18.0: rejected identically. Passed.

## 54. Wrong-level negatives

`Target16K_WrongLevel_AtThreeDays_Rejected` (beginner, advanced, both at
days=3): rejected, `UNSUPPORTED_TARGET_DISTANCE`. Passed.

## 55. Wrong-frequency negatives

`Target16K_Intermediate_WrongFrequency_5Dand6D_Rejected` (5, 6):
rejected, `UNSUPPORTED_TARGET_DISTANCE`. Passed. (3D and 4D are now both
legitimately public — see §7; this is the correct, narrowed negative set.)

## 56. Custom-without-target safety

`CustomWithoutTargetDistanceKmStillFailsClosed_16KI3DAdditionDidNotWeaken`:
omitting `target_distance_km` still returns 422
`GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`; persisted row counts unchanged
before/after. Passed.

## 57. Habit Custom safety

No habit-goal-type path was touched by this phase; Habit Custom's own
pre-existing unsupported status is unaffected (no habit-shaped request
appears anywhere in this phase's production or test diff).

## 58. Exact-target matching

`TryResolvePublicEligibleCell` remains exact-match only (0.001km tolerance)
— confirmed unchanged in source; no interpolation, no snapping, no band.

## 59. Public-registry keying

Full 4-tuple key `(TargetDistanceKm, ParentDistanceFamily, Level,
RunsPerWeek)` — unchanged shape, one new row.

## 60. No target-only gate proof

`Target16K_WrongLevel_AtThreeDays_Rejected` and
`Target16K_Intermediate_WrongFrequency_5Dand6D_Rejected` directly disprove a
target-only gate: the same `target_distance_km=16.0` is rejected at
Beginner/Advanced×3D and at Intermediate×5D/6D, proving Level and
RunsPerWeek both still gate independently.

## 61. No global projected-3D gate proof

`UnsupportedTargetDistances_AtThreeDays_StillRejected` (15.0, 18.0, 12.0,
20.0, all at Intermediate×3D) disproves a general "any projected target at
3D is public" shortcut — only the exact 16.0 value at 3D is admitted.

## 62. Mapper audit

`GeneratePreviewCommandMapper.cs` diff: empty (confirmed via `git status`/`git diff`
— the file does not appear in this phase's changed-file list at all). Zero
code change, as predicted going in.

## 63. Recurring public-assumption audit

Found and corrected two stale test-level assumptions that this phase's own
change invalidated (both discovered by re-running the DIST-GEN.3/7/11/16
precedent suites after the production change, per the governing prompt's own
disclosed-correction pattern used in DIST-GEN.10/11/13):
1. `DistGen3PublicActivationTests.Pilot16K_WrongFrequency_Rejected` treated
   `days: 3` as a wrong-frequency negative for 16K — now stale; narrowed to
   `5`/`6` only.
2. `Dark18KTargetDistanceProjectionTests`'s own `ApprovedPublicCells.Count`
   assertion (previously updated to 3 by DIST-GEN.11, and left unedited by
   DIST-GEN.16 since DIST-GEN.16 was dark-only) hardcoded 3 — updated to 4
   with explicit `RunsPerWeek` discrimination on the two 16.0 entries.
Both corrections are documented in-place in the amended files' own comments,
mirroring the established precedent for this kind of disclosed correction.

## 64. Public 16K I4D zero-delta

`ZeroDelta_16KI4D_StillPubliclyEligible_Unaffected`: 200,
`requested_target_distance_km=16.0`. Passed. Also re-verified via the full,
unmodified `DistGen3PublicActivationTests.cs`/`DistGen11PublicActivationTests.cs`
suites (203/203 passed together with DistGen7/Dark18K/DistGen16/DistGen17 in
the combined precedent-recheck run, §78).

## 65. Public 15K zero-delta

`ZeroDelta_15K_StillPubliclyEligible_Unaffected`: 200,
`requested_target_distance_km=15.0`. Passed.

## 66. Public 18K zero-delta

`ZeroDelta_18K_StillPubliclyEligible_Unaffected`: 200,
`requested_target_distance_km=18.0`. Passed.

## 67. HM I3D zero-delta

`ZeroDelta_HalfMarathonIntermediate3D_Canonical_Unaffected`: 200,
`template_id=HALF_MARATHON__3D__INTERMEDIATE`, `requested_target_distance_km`
null. Passed — proves the canonical (non-projected) HM I3D path, which
shares the same catalog candidate identity as 16K I3D, is fully unaffected.

## 68. HM I4D zero-delta

`ZeroDelta_HalfMarathonIntermediate4D_Canonical_Unaffected`: 200,
`template_id=HALF_MARATHON__4D__INTERMEDIATE`. Passed.

## 69. TEN_K I3D zero-delta

`ZeroDelta_TenKIntermediate3D_Canonical_Unaffected`: 200,
`template_id=TEN_K__3D__INTERMEDIATE`. Passed.

## 70. TEN_K I4D zero-delta

`ZeroDelta_TenKIntermediate4D_Canonical_Unaffected`: 200,
`template_id=TEN_K__4D__INTERMEDIATE`. Passed.

## 71. Catalog integrity

No catalog JSON file was created, edited, or duplicated. `PlanCatalog.Tests`
1510/1510 (§79), matching baseline exactly.

## 72. Database/schema zero-delta

No migration, no schema change. `git status` confirms no file under any
`Migrations/` directory changed.

## 73. Training-authority zero-delta

`TargetDistance16KIntermediate3D{HorizonPolicy,PeakVolumeBandPolicy,TaperVolumePolicy}`,
`VolumeSafetyPolicy.HalfMarathonIntermediate3DTargetDistance16K`, and every
other DIST-GEN.15B/16-frozen authority file: zero diff (confirmed by `git
status` — none of these files appear in this phase's changed-file list).

## 74. Rollback boundary

`ConfirmedPlan_16KI3D_RemainsFullyOperable_ReadsAndMutationsDoNotRecheckPublicEligibility`
constructs an isolated in-memory `ApprovedPublicCells` copy with the
`(16.0, HM, Intermediate, 3)` entry removed (the same seam
DIST-GEN.11's own rollback proof used), confirming the rolled-back list
still contains 16.0×4D/15.0/18.0 but not 16.0×3D.

## 75. Confirmed-plan operability after rollback

Same test: with the rollback simulated, the already-confirmed 16K I3D
plan's own `GET /home`, `GET /details`, and `POST /cancel` all still succeed
via real HTTP against the real persisted plan — because
`PlanServices`' read/mutation paths never consult `ApprovedPublicCells` at
all (confirmed by inspection); only new-preview creation in
`GeneratePreviewCommandMapper` does. Passed.

## 76. Real PostgreSQL E2E

`TwelveWeek_16KI3D_FullLifecycle_IdentitySurvivesEveryStep` exercises the
complete real HTTP→Postgres chain: preview → confirm → persist → Home →
Calendar → Details → Complete → Not-Today → Cancel, all against the real
`CustomWebApplicationFactory`-hosted API and its real PostgreSQL-backed
`AppDbContext`. Passed.

## 77. Dark/public golden proof

§19 (`DarkVsPublic_16KI3D_12W_SemanticZeroDelta_OnlyReachabilityChanged`):
public 12W output matches DIST-GEN.16's own dark golden trace numerically
(peak volume ≤34.0, peak LR ≤15.0, 3 sessions/week, 12 weeks). Passed.

## 78. Targeted tests

Fresh build + targeted run,
`FullyQualifiedName~DistGen17PublicActivationTests|FullyQualifiedName~DistGen16Intermediate3DFullDarkImplementationTests`:
**63/63 passed**. Combined precedent-recheck run after the two stale-test
corrections (§63),
`FullyQualifiedName~DistGen3PublicActivationTests|DistGen7PublicActivationTests|DistGen11PublicActivationTests|Dark18KTargetDistanceProjectionTests|DistGen17PublicActivationTests|DistGen16Intermediate3DFullDarkImplementationTests`:
**203/203 passed**.

## 79. PlanCatalog regression

Fresh build + fresh run: `PlanCatalog.Tests` **1510/1510 passed, 0 failed** —
matches DIST-GEN.16's own accepted baseline exactly.

## 80. RuntimeCatalog regression

Fresh build (of the already-current build from §78/§79) + run,
`--filter "FullyQualifiedName~RuntimeCatalog"`: **4042 total / 4042 passed /
0 failed** — matches the accepted DIST-GEN.16 baseline exactly (this phase's
new `DistGen17PublicActivationTests.cs` lives in the top-level
`RunningApp.IntegrationTests` namespace, outside this filter's scope; the
`DistGen16Intermediate3DFullDarkImplementationTests.cs` amendment renamed
one existing test without changing the total count).

## 81. PlanGeneration regression

No dedicated `PlanGeneration`-only filter is used separately in this
codebase's regression discipline beyond the full monolithic run (§82);
covered there.

## 82. Full monolithic regression

Fresh build + fresh run (`dotnet test RunningApp.IntegrationTests --no-build`,
47m36s): **5174 total / 5170 passed / 4 failed**. The `.trx`
(`distgen17_full_monolithic.trx`) was parsed directly for `outcome="Failed"`
`testName` attributes — exactly these 4 identities were found, and no
others:
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

## 83. Durable-baseline comparison

| Suite | DIST-GEN.16 baseline | DIST-GEN.17 result | Verdict |
|---|---|---|---|
| PlanCatalog.Tests | 1510/1510 | 1510/1510 | Exact |
| IntegrationTests (RuntimeCatalog filter) | 4042/4042 | 4042/4042 | Exact |
| IntegrationTests (full) | 5146 total/5142 passed/4 named failures | 5174 total/5170 passed/4 named failures (+28 new tests, all passing) | Exact modulo the 28 intentionally-added/net-changed test cases; failing-identity set identical to the known durable baseline, byte-for-byte. |

The +28 delta: `DistGen17PublicActivationTests.cs` adds 24 distinct test
cases (accounting for Theory expansion: 1 central proof + 5 horizon cases +
2 rejection + 1 dark/public golden + 1 lifecycle + 1 cross-frequency
persistence + 1 rollback + 1 registry-separation + 2 wrong-level + 2
wrong-frequency + 4 unsupported-distance + 1 custom-without-target + 6
zero-delta = 28 test methods total, some Theory-expanded); the amendment to
`DistGen3PublicActivationTests.cs` removes one InlineData case (`days: 3`)
from an existing Theory, netting no new case; the `Dark18KTargetDistanceProjectionTests.cs`
and `DistGen16Intermediate3DFullDarkImplementationTests.cs` amendments
rewrite existing tests in place with no case-count change. Net full-suite
delta of +28 matches 5174−5146=28 and 5170−5142=28 exactly.

## 84. Build-freshness verification

Every regression run in §78-82 followed an explicit `dotnet build` (or a
build performed immediately prior in the same session) with 0 build errors,
never relying on a stale prior build. The one transient failure observed
mid-phase (`DistGen3PublicActivationTests.Pilot16K_WrongFrequency_Rejected(days:
3)`) was diagnosed as a genuine, expected consequence of the production
change (not a stale-build or flake artifact) and fixed at the test level
(§63), then re-verified green on the next fresh run.

## 85. Final source-diff audit

`git status --porcelain -- '*.cs' '*.dart'` (excluding build-artifact
`obj/**/AssemblyInfo.cs` noise) shows exactly:
- Modified: `Dark16KPilotEligibilityPolicy.cs` (production, §6)
- Modified: `DistGen3PublicActivationTests.cs`, `Dark18KTargetDistanceProjectionTests.cs`,
  `DistGen16Intermediate3DFullDarkImplementationTests.cs` (test corrections, §63)
- New: `DistGen17PublicActivationTests.cs` (new test suite)

No canonical HM/TEN_K file, no other DIST-GEN target-distance policy file,
no frontend `.dart` file, no migration, no catalog JSON appears in the diff.
A benign `ten-k-pilot-domain-decision-audit.{json,md}` regeneration
(the same kind of artifact DIST-GEN.16's own report disclosed and reverted)
was observed and reverted via `git checkout --` before this report was
written.

## 86. Public registry after activation

`ApprovedPublicCells` — 4 entries: (16.0, HM, Intermediate, 4),
(15.0, HM, Intermediate, 4), (18.0, HM, Intermediate, 4),
(16.0, HM, Intermediate, 3).

## 87. Dark registry after activation

`ApprovedDarkCells` — unchanged, still 4 entries (unchanged since
DIST-GEN.16): (16.0, HM, Intermediate, 4), (15.0, HM, Intermediate, 4),
(18.0, HM, Intermediate, 4), (16.0, HM, Intermediate, 3).

## 88. Registry-separation preservation

Both lists now coincide in content (4 entries each, and in fact identical
sets) — this is explicitly documented (in-source and in this report, §8) as
a numeric coincidence, not a structural merge. They remain two independently
declared, independently auditable `static readonly` fields; neither is
computed from the other; `PublicAndDarkRegistries_BothNowFourEntries_ButRemainIndependentlyDeclaredLists`
proves this via `ReferenceEquals` inequality.

## 89. Public activation result

16K × HalfMarathon-family × Intermediate × 3D is now genuinely reachable
through the real public `POST /api/v1/plans/generate-preview/race` contract,
with zero-delta output from its own dark implementation, zero training-
authority change, and no widening beyond the exact single cell.

## 90. Frequency-aware public architecture result

`DaysPerWeek`/`RunsPerWeek` is now proven, at the real public HTTP layer, to
be a genuine, independent discriminator in public eligibility — the same
target distance resolves two structurally and numerically distinct public
authorities depending on frequency, and every other level/frequency
combination sharing partial dimensions with the newly public cell remains
correctly rejected.

## 91. Orthogonality result

Projected-distance and frequency dimensions compose orthogonally at the
public boundary exactly as they already did at the dark boundary
(DIST-GEN.16): distance selects the parent-family/target authority,
frequency independently selects RunLayout/volume/LR-share/absolute-cap
authority, and public reachability is a separate, third, independently
gated dimension layered on top of both — proven by the full 4-tuple
negative matrix (§54/§55/§60/§61).

## 92. DIST-GEN.18 recommendation

Not evaluated; explicitly out of scope for this phase. No further expansion
(15K/18K×3D, other non-4D frequencies, level expansion) was begun or implied
by any change in this phase.

## 93. Remaining blockers

None identified. Every numeric value was reused unchanged from DIST-GEN.15/15B/16;
the sole production change was the single registry-entry addition the
governing prompt specified; the frontend required, and received, zero
change (investigated and confirmed); all required real-HTTP/PostgreSQL
proofs pass; rollback safety holds; registry separation holds; regression
shows no new failing identity; the source diff is scoped to exactly the
intended files.

---

## Final classification

`16K_INTERMEDIATE_3D_PUBLIC_ACTIVATION_COMPLETE`
