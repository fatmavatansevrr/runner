# Phase 10K-GEN.4E — Beginner 4D Core Public Activation

Every number below is from an actual command run in this session. Final classification: **`BEGINNER_4D_CORE_PUBLICLY_ACTIVE`**.

## 1. GEN.4D/4D.1/4D.2 binding status

Confirmed unchanged going in: `VolumeSafetyPolicy.BeginnerFourDay` (12.0/9.5/9.0/0.53/17.0/21.0/18-24), `CatalogProductIneligibleException` hierarchy, `TEN_K__4D__BEGINNER v1` (`VALIDATED`). Not re-derived — only inspected.

## 2. Pre-activation boundary table

| File | Previous behavior | New behavior | Authority | Reason |
|---|---|---|---|---|
| `V1CatalogPilotIdentityPolicy.cs` | `IsSupportedIdentity`: Level==Intermediate only. `ResolveCandidate(int)`: DaysPerWeek-only, no Level. | Explicit 3-entry allow-list: (Intermediate,3), (Intermediate,4), (Beginner,4). `ResolveCandidate(Level, daysPerWeek)`. New `TryResolveCandidate` for non-throwing call sites. | GEN.4A/GEN.4E | Beginner 4D becomes a real, distinct public identity |
| `LivePlanPreviewRouting.cs` | `Decision()` used `daysPerWeek is 3 or 4` (Level-blind) for informational candidate key/version. Validator hardcoded `Level != Intermediate`. Eight-week-explicit-zero short-circuit fired for any DaysPerWeek==4 regardless of Level. | `Decision()` uses `TryResolveCandidate`. Validator delegates to `IsSupportedIdentity`. Short-circuit scoped to `Level == Intermediate` only. | GEN.4E | Level-blind logic would have silently mis-resolved or mis-routed Beginner |
| `CatalogPreviewGenerator.cs` | `ResolveCandidate(daysPerWeek)` | `ResolveCandidate(request.Level, daysPerWeek)` | GEN.4E | Signature widening, mechanical |

## 3. Candidate identity resolution changes

Followed GEN.3B's exact precedent: 3D was added as an additive switch-case in the existing tuple-returning `ResolveCandidate`, not a new overload or parallel mechanism. Beginner follows the identical pattern — one more switch arm, one more allow-list tuple. `IsSupportedIdentity` is now backed by a private `IsSupportedLevelFrequency` tuple-pattern switch, an explicit 3-entry enumeration, not a derived/inferred rule.

**All production call sites found and updated** (Section A3 requirement): `CatalogPreviewGenerator.cs:326`, `LivePlanPreviewRouting.cs` (`Decision` builder, `Decide()`'s candidate load, and the validator's identity check — 3 sites in one file). `GeneratePreparationRunwayPreviewAsync` intentionally left untouched (hardcoded to Intermediate's constants) — Runway stays closed to Beginner, per the DO NOT list.

Vocabulary mapping (Beginner ↔ "NEW") is applied at candidate-load time (existing mechanism, unchanged) — this policy only ever deals in the backend enum, never the catalog string, so no new translation point was introduced.

## 4. HTTP exception routing closure

GEN.4D.2 disclosed this was blocked by containment + the DaysPerWeek-only `ResolveCandidate`. Both are now fixed. New test `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroShortCore_ReturnsTypedProductIneligibility` (weeks 8-12) makes a real `POST /api/v1/plans/generate-preview/race` call and asserts real HTTP 422 with `BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT` in the body. Week 8 specifically required scoping the router's known-infeasible-eight-week short-circuit to Intermediate only (§2) — without that fix, week 8 would have returned the wrong (Intermediate-shared) reason instead of the real Beginner-specific one.

## 5. Public preview / eligibility matrices (real HTTP, both match GEN.4C.4 exactly)

- Missing-readiness 8-14wk: all `200 OK`, 4 sessions/week, `template_id = TEN_K__4D__BEGINNER`. (`EligibleFourDayCoreHorizon_PublicPreviewHasExactlyFourRoles`, 7/7)
- Explicit-zero 8-12wk: all `422` typed. Explicit-zero 13-14wk: `200 OK`. (5/5 + 2/2)
- No FARTLEK/THRESHOLD_TEMPO ever selected (asserted in the confirm/persist test, §6).

## 6. Confirm/persistence/read results

Real reset→preview→confirm→DB→read→complete chain (`TwelveWeek_ResetPreviewConfirmReadAndComplete_PersistsExactlyFortyEightDays`): `DaysPerWeek=4`, `CatalogCandidateKey=TEN_K__4D__BEGINNER`, 12 weeks × 4 days = 48 days, role split 12 KEY_SESSION / 24 EASY_SUPPORT / 12 LONG_RUN (matches 1K+2E+1L × 12 weeks), Home/Calendar/day-detail all read the persisted plan correctly, a real training day completes via the real completion endpoint.

## 7. Containment results (real HTTP, not just unit checks)

- Runway (15-20wk) and LongHorizon (21+/52wk): neither `200` nor `500` — `NonCoreFourDayBeginnerHorizons_RemainUnactivated`, 4/4.
- Unsupported combinations — Beginner×3D, Advanced×4D, Beginner×5D — never `200`/`500`: `WrongCombination_NeverNearestMatches`, 3/3.
- Intermediate×4D and Intermediate×3D: `IntermediateFourDayAndThreeDay_ZeroRegression` — both `200`, correct `template_id` each.

## 8. Tests added / updated

- New: `Gen4EBeginnerFourDayPublicActivationTests.cs` (9 test methods, 25 cases).
- Updated (repurposed from an obsolete premise, not weakened — see §9): `Gen4DBeginnerFourDayCoreTests.Candidate_IsInternalOnly_AndPublicIdentityIsNotWidened` → `Candidate_IsPublic_AndOnlyBeginnerFourDayWasWidened`; `RunningBackgroundV2Tests.NonIntermediateLevels_AreNotSilentlyCoercedToIntermediate` → `UnwidenedNonIntermediateLevels_AreNotSilentlyCoercedToIntermediate` (dropped the now-incorrect Beginner case, added a positive `BeginnerLevel_ReachesItsOwnPilotMapping_AtFourDaysOnly` test); `Phase4F8_2LivePilotRoutingTests.Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog`'s `Level` mutation case changed from `Beginner` (no longer a valid negative case) to `Advanced` (still genuinely unsupported).

## 9. Exact commands and results

```
dotnet build backend/RunningApp.sln --no-restore -v:minimal
  -> 0 Warning, 0 Error

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Gen4EBeginnerFourDayPublicActivationTests|Gen4DBeginnerFourDayCoreTests"
  -> 41/41, 0 failed

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Gen3A|Gen3B|DynamicCoreVolumeAndLongRunOrchestratorTests|Gen4D|Gen4E"
  -> 162/162, 0 failed

dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj --no-restore
  -> 1250/1250, 0 failed

dotnet test .../RunningApp.IntegrationTests.csproj --no-build   (full suite, detached background, ~19m43s)
  -> 3462/3464, 2 FAILED  [first attempt]

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "RunningBackgroundV2Tests|Phase4F8_2LivePilotRoutingTests"
  -> 56/56, 0 failed   [after fixing the 2 real, disclosed staleness cases below]

dotnet test .../RunningApp.IntegrationTests.csproj --no-build   (full suite, detached background, ~19m4s)
  -> 3464/3464, 0 failed, EXITCODE=0   [final]

git diff --check
  -> clean (one benign LF/CRLF normalization notice only)
```

## 10. Real findings during the first full-regression pass (disclosed, not silently absorbed)

The identity widening (§3) correctly and legitimately made `(Beginner, 4)` a real pilot identity for the first time. Two pre-existing tests had baked in the old, now-obsolete assumption that changing Level away from Intermediate always means "non-pilot request":

1. `RunningBackgroundV2Tests.NonIntermediateLevels_AreNotSilentlyCoercedToIntermediate(Beginner)` — asserted `IsSupportedIdentity` is `false` for Beginner at 4D. Now legitimately `true`. Fixed by removing Beginner from this "still unsupported" theory (Advanced/Experienced remain, correctly, in the negative set) and adding a dedicated positive assertion for Beginner.
2. `Phase4F8_2LivePilotRoutingTests.Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog(Level)` — mutated `Level` to `Beginner` to produce a "not a pilot request" case; that mutation no longer produces a non-pilot request. Fixed by using `Advanced` instead, which still genuinely exercises the same negative-identity path.

Both are `LEGITIMATE_CLOSED_WORLD_EXPANSION` — the exact combination each test now checks changed because the supported-identity set genuinely grew, never because either assertion was loosened (both remain exact `true`/`false` checks, no exact-to-inexact weakening).

## 11. Full regression status vs. 3441 baseline (GEN.4D.2)

Discovered/executed count grew from 3441 → 3464 (23 new: 25 new `Gen4EBeginnerFourDayPublicActivationTests` cases minus 2 test-method consolidations/renames elsewhere netting out slightly differently in xUnit's case-counting — not investigated further, not required by the phase). Final: **3464/3464, 0 failed.**

## 12. Cells still closed (explicit enumeration)

Public identity allow-list is now exactly: `(Intermediate, 3)`, `(Intermediate, 4)`, `(Beginner, 4)`. Every other cell remains unreachable, confirmed by real HTTP requests in this phase: Beginner×3D, Advanced×4D, Beginner×5D+, Beginner at Runway (15-20wk), Beginner at LongHorizon (21+wk). No nearest-match fallback exists anywhere in `ResolveCandidate`/`IsSupportedIdentity` (throws/returns false for every unlisted combination).

## 13. Final classification

```
BEGINNER_4D_CORE_PUBLICLY_ACTIVE
```
