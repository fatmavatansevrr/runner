# Phase 10K-FREQ.2A — Live Cell Runway-Horizon Exposure Check

**Urgent verification, resolved safe. Every claim below is from real code trace plus real, executed HTTP tests — not assumed from GEN.3B/GEN.4E's prior claims.**

## 1. Real request path trace

`PlanServices.GeneratePreviewAsync` computes the canonical horizon via `RaceHorizonPolicy.Decide`; for a 15-20wk horizon (`CompositionRequired` classification) it checks:

```csharp
private static bool IsPreparationRunwayPilotScope(GeneratePreviewRequest request) =>
    request.GoalType == GoalType.Race &&
    request.GoalDistance == GoalDistance.TenK &&
    request.Level == RunningBackground.Intermediate &&
    request.DaysPerWeek == 4;
```

**This is an exact-identity gate, not a generic "any 15-20wk request" gate.** It requires `Level == Intermediate` *and* `DaysPerWeek == 4` simultaneously. Neither Intermediate×3D (`DaysPerWeek == 3`, fails the gate) nor Beginner×4D (`Level == Beginner`, fails the gate) can ever match it, regardless of FREQ.2's finding that `TenKPreparationRunwayNumericPolicyFactory` itself is hardcoded downstream — this upstream gate never lets either cell reach that hardcoded factory in the first place. Both fall through to:

```csharp
throw new PlanHorizonCompositionRequiredException(
    "The available race-plan horizon requires a preparation block before the supported race-training core. ...");
```

`GlobalExceptionHandler.cs` line 102 maps `PlanHorizonCompositionRequiredException` → real, typed HTTP 422, reason `PLAN_HORIZON_COMPOSITION_REQUIRED`.

## 2. Real HTTP verification (not just code-read)

Added two new permanent regression tests (not throwaway — these lock in exactly the invariant this phase was checking, matching this engagement's established practice of converting every real finding into permanent coverage):

**`Gen3BThreeDayPublicActivationTests.RunwayHorizonThreeDay_TypedRejection_NoSilentFourDayCoercion`** (weeks 15, 20) — asserts exact `422`, body contains `PLAN_HORIZON_COMPOSITION_REQUIRED`, body does **not** contain `TEN_K__4D__INTERMEDIATE`.

**`Gen4EBeginnerFourDayPublicActivationTests.RunwayHorizonBeginner_TypedRejection_NoSilentIntermediateCoercion`** (weeks 15, 20) — same assertions, for Beginner×4D.

```
dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "RunwayHorizonThreeDay_TypedRejection|RunwayHorizonBeginner_TypedRejection|NonCoreThreeDayHorizons_RemainUnactivated|NonCoreFourDayBeginnerHorizons_RemainUnactivated"
  -> 12/12 passed, 0 failed

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Gen3B|Gen4E"
  -> 50/50 passed, 0 failed
```

**Confirmed for real: both live cells receive a clean, typed 422 with the correct reason code. No 200. No silent coercion to Intermediate×4D content (explicitly checked the response body doesn't contain the wrong candidate key). No untyped 500.**

## 3. Classification

Not merely "safe but untyped" (the second-tier outcome this phase defined) — the actual result is fully typed, with an accurate, pre-existing, user-facing reason code (`PLAN_HORIZON_COMPOSITION_REQUIRED`) that correctly describes the real situation (Runway/preparation-block composition genuinely isn't available for this identity). GEN.3B's and GEN.4E's original "Runway not widened" containment claims **hold**, re-verified specifically against FREQ.2's architectural finding — they were correct, just not previously cross-checked against the reason *why* (identity-exact gate vs. generic gate), which this phase supplied.

## 4. Final classification

```
NO_EXPOSURE_CONFIRMED_SAFE
```

No live defect. No urgent fix required. Two new permanent regression tests added and passing, closing the verification gap FREQ.2's architectural finding opened.
