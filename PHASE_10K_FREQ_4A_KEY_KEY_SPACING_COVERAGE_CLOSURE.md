# Phase 10K-FREQ.4A — ScheduleRepairSpacingValidator KEY↔KEY Real Coverage Closure

**Test coverage only. No production logic changed.**

## 1. FREQ.4's claim, checked — found incorrect

FREQ.4 §C stated: *"no existing lightweight in-memory test-construction precedent found in this codebase"* for `LongHorizonRollingPlanState`/`LongHorizonRollingSessionState`. **This was wrong.** Checked directly: `ScheduleRepairRuntimeOrchestratorTests.cs` (Phase 4M.3) already contains real, working, real-Postgres fixture-construction helpers — `CreatePlanAsync`/`CreateWeekAsync`/`CreateSessionAsync`/`MarkNotTodayAsync` — using the exact same `CustomWebApplicationFactory`/`AppDbContext` pattern every other real-DB Adaptation V1 test in this codebase uses. FREQ.4's disclosure was made without a real search of the adjacent test files in the same directory; it should have been checked before being asserted as a limitation.

## 2. Real test added, through the real entry point

Built `Freq4AKeyKeySpacingRealCoverageTests.cs`, mirroring the exact real-Postgres fixture pattern (kept local to the new file rather than editing the shared `ScheduleRepairRuntimeOrchestratorTests.cs`, since a genuine violation required an adjustable preferred-days parameter the shared helper doesn't expose — additive only, nothing shared was modified).

**Real construction problem solved**: the standard 4D preferred-day vocabulary (Mon/Wed/Fri/Sun) has a minimum inter-preferred-day gap of exactly 2 days by construction — the same reason `ScheduleRepairRuntimeOrchestratorTests`'s own KEY↔LONG spacing-invalid test (`EmptySlot_HardSessionSpacingInvalid_MarkedInvalid_ButNextValidChosen`) could never construct an actual violation and had to test "earliest valid selection" instead (its own comment says so). Used a genuinely adjacent-day preferred set (Mon/Tue/Wed/Sun) instead, so a real 1-day-apart candidate could exist.

Two tests:

1. **`GetEmptySlotCandidates_SecondKeySessionTooCloseToRemainingActiveKey_IsFlaggedSpacingInvalid`** — real DB: KEY #1 Active Wednesday 8/12, KEY #2 (trigger) NotToday from Monday 8/10. Candidate Tuesday 8/11 (1 day from the remaining Active KEY) is real, structurally eligible, and correctly flagged `IsSafetyValid = false`; candidate Sunday 8/16 (4 days away) is `IsSafetyValid = true`.
2. **`RealRepairPipeline_SkipsKeyKeySpacingInvalidCandidate_SelectsNextValidOne`** — same scenario, but through `ScheduleRepairRuntimeOrchestrator.RunAsync` (the real entry point, not a unit-isolated validator call, per explicit instruction). The chronologically-earliest candidate (Tuesday) is spacing-invalid and correctly skipped; the pipeline selects Sunday 8/16 instead — proving the new rule genuinely influences real candidate selection end-to-end, not just an isolated validator return value.

## 3. Verification

```
dotnet build backend/RunningApp.sln --no-restore -v:minimal
  -> 0 Warning, 0 Error

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Freq4AKeyKeySpacingRealCoverageTests"
  -> 2/2 passed, 0 failed

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Adaptation|ScheduleRepair"
  -> 193/193 passed, 0 failed   (191 FREQ.4 baseline + 2 new)

dotnet test .../RunningApp.IntegrationTests.csproj --no-build   (full suite, detached background, ~20m5s)
  -> 3480/3480 passed, 0 failed, EXITCODE=0   (baseline was 3478 per FREQ.4 -- +2 is exactly the new tests)
```

## 4. Final classification

```
KEY_KEY_SPACING_REAL_COVERAGE_CONFIRMED
```

FREQ.4's "genuinely infeasible" disclosure was itself incorrect — real precedent existed and was found on a proper search. The gap is now closed with real, DB-backed, end-to-end coverage through the actual repair pipeline, not a synthetic unit-level substitute.
