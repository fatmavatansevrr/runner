# Phase 4G.5H — Dynamic Core Calendar Materialization (Final Validation)

## 1. Executive result

A new, dark, unwired orchestrator — `DynamicCoreCalendarMaterializationOrchestrator` — is the top-level composition of all five dynamic 8-14-week dark layers built across this session (4G.5D skeleton → 4G.5E workout binding → 4G.5F volume/long-run → 4G.5G pace prescription → 4G.5H calendar/race-alignment). Calendar-day assignment itself required **no new orchestration** — it already happens, unmodified, inside Phase 4G.5E's binding orchestrator, which must produce a dated skeleton before `CatalogWorkoutBinder` can bind anything to it. This phase's one genuine addition is **race-date alignment**, which none of the four prior dark orchestrators threaded through at all — closed by calling the real, already-existing, already-dark `RaceDateAlignmentVerifier` (Phase 4G.3B.3) directly. The final cross-phase pipeline validation (this phase's own title) found **zero cross-layer contradictions** across all 7 horizons, and the 12-week end-to-end composed output is byte/value-identical to the existing live pipeline. No new TD was required.

## 2. Dark/live boundary — confirmed explicitly before writing any verification

Per this phase's own instruction, this was resolved before any code was written, mirroring Phase 4G.5G's own finding that live-wired components are not automatically "dark":

1. **Components already live-reachable today (for the 12-week case), via `CatalogPreviewGenerator`:** `CatalogPhaseAllocationResolver.Resolve(candidate)` (fixed overload), `CatalogStageToWeekMaterializer`, `CatalogWeekSkeletonCalendarMaterializer` (Phase 4F.5 — confirmed live-called at `CatalogPreviewGenerator.cs` line ~559, exactly the same class this pass's calendar work reuses), `ProgressionStageAllocator`, `CatalogWorkoutBinder`, `CatalogPrescriptionContextBuilder`, `CatalogVolumeAndLongRunPlanner`, `CatalogSessionPrescriptionPlanner`, `CatalogFinalPrescribedPlanFinalizer`. All of these are already this candidate's live 12-week machinery, gated unreachable at runtime only by the pilot catalog's `DRAFT` status — not by any dark-reachability code guard.
2. **Components that remain genuinely dark, guarded by their own reachability tests:** the nine Phase 4G.3B.3 verifiers, including `RaceDateAlignmentVerifier` (this phase's one new dependency) — confirmed zero production call sites except the one approved dark orchestrator, both before and after this pass (Section 6).
3. **The actual invariant this phase proves:** `DynamicCoreCalendarMaterializationOrchestrator` (new) and the four Phase 4G.5D-5G orchestrators beneath it have **zero callers from `CatalogPreviewGenerator` or any other live path** — not a claim that every component in the chain lacks a live caller, which would be false for at least nine of them.

This framing was applied consistently throughout the rest of this document — "zero production call sites" below always means the new orchestrator layer specifically.

## 3. Reuse strategy — no parallel date-assignment system built

`CatalogWeekSkeletonCalendarMaterializer` (Phase 4F.5) was not modified, wrapped, or re-derived. It is already called, unmodified, inside `DynamicCoreWorkoutBindingOrchestrator` (Phase 4G.5E, this session's own file) as a prerequisite step for workout binding — `DynamicCoreWorkoutBindingResult` already exposed its output (`DatedSkeleton`) from the moment that orchestrator was written, requiring no adaptation this pass. The one missing piece — race-date alignment — is closed by calling `RaceDateAlignmentVerifier.Verify(datedSkeleton, raceDate)` directly (Phase 4G.3B.3, verifier 9 of 9, already dark, already proven independent of the live guard's non-horizon-agnostic `Weeks.Count != 12` clause per `TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001`'s closure). No new date-comparison, spacing, or alignment logic was written anywhere in this pass.

## 4. Preserved decisions — confirmed, not reinterpreted

**PreferredDays/LongRunDay spacing rules.** The existing backtracking search (`CatalogWeekSkeletonCalendarMaterializer.TryAssignKeySessionDates`, minimum 2-day KEY_SESSION/LONG_RUN separation) is unmodified and untouched. Verified unchanged behavior across horizons via two fixtures: a "safe spacing" configuration (Mon/Wed/Fri/Sun, LongRunDay=Sun) and a genuinely tighter "adjacent easy days" configuration (Mon/Tue/Thu/Sat, LongRunDay=Sat, exercising 2-day boundaries rather than assuming success) — both succeed under the real, unmodified search at 8/12/14 weeks, and `MaterializeAsync_InvalidLongRunPreference_FailsClosed_UnchangedFromExistingBehavior` confirms `CatalogLongRunDayNotPreferredException` still fires, unmodified, when `LongRunDay` isn't a `PreferredDays` member.

**Phase 4G.5F's `PrescriptionContext` field — re-verified, not assumed harmless.** Per this phase's explicit instruction, `DynamicCoreVolumeAndLongRunOrchestratorTests.PlanAsync_TargetWeekCount12_MatchesExistingFixedWeekVolumePipelineExactly` (Phase 4G.5F's own byte/value-level 12-week regression test) was re-run fresh in this pass, standalone. **Result: passes unchanged, no modification required.** The field addition (Section 3 of `PHASE4G_5G_DYNAMIC_CORE_PACE_PRESCRIPTION.md`) did not silently affect it.

## 5. Required rules — verified

- Weeks based on `StartDate` and consecutive 7-day spans: unchanged, `CatalogStageToWeekMaterializer`'s own Decision 1 (`weekStart = StartDate + (N-1)*7`).
- Monday alignment not required: `MaterializeAsync_StartDateMidweek_MondayAlignmentNotRequired` confirms a Wednesday `StartDate` produces week 1 starting exactly on that Wednesday, at 8/12/14 weeks, with no hidden shift.
- PreferredDays remain authoritative; LongRunDay must be a `PreferredDays` member: Section 4.
- Race-date alignment remains correct: `RaceDateAlignmentVerifier` confirms `NoSessionAfterRaceDate`/`FinalSessionWithinTolerance` (0-7 days before, inclusive) for every tested horizon and `StartDate` variant, including the exact-week-boundary case (`RaceDate = StartDate + weeks*7 - 1`).
- Final taper/race boundary valid: `TaperWithinFinalWindow` check (part of `RaceDateAlignmentVerifier`, unmodified) confirms every taper session falls on or before the plan's own final session date, for all tested horizons.
- Partial-day arithmetic creates no hidden week shift: confirmed by the midweek-`StartDate` test above.

## 6. Required verification

**Zero production call sites (for the new orchestrator specifically)**: confirmed by grep (repo-wide search for `DynamicCoreCalendarMaterialization(Orchestrator|Context|Result)` across all four production projects, excluding the new file's own source, returned zero matches) and by `DarkReachability_NoProductionCallSite`. `DarkReachability_NoDiRegistration` confirms `IDynamicCoreCalendarMaterializationOrchestrator` is never referenced in `RunningApp.Api`. `CatalogPreviewGenerator.cs`'s `git diff` remains empty.

**Byte/value-level 12-week regression**: `MaterializeAsync_TargetWeekCount12_MatchesExistingFixedWeekDatedCalendarExactly` builds the dated calendar via the **existing, completely unmodified** fixed-week pipeline and, separately, via the new orchestrator requesting `targetWeekCount=12`, then asserts full field-by-field equality of every week's `StartDate`/`EndDate`/`PhaseKey` and every session's date/day-of-week/structural-role — plus a literal pinned `StartDate`/`EndDate` fixture. Combined with Phase 4G.5G's own already-passing `FinalPrescribedPlan`-level regression (re-confirmed fresh, Section 4), the full composed stack's 12-week output is proven byte-identical at every layer, not just this one.

**Existing-test reconciliation — two distinct cascades, both resolved:**

1. **The now-familiar direct cascade** (fifth occurrence: 4G.5D↔4G.3B.2, 4G.5E↔4G.5D, 4G.5F↔4G.5E, 4G.5G↔4G.5F, now 4G.5H↔4G.5G): adding this orchestrator's call to `DynamicCoreSessionPrescriptionOrchestrator.PrescribeAsync(...)` broke that phase's own `DarkReachability_NoProductionCallSite` test. Renamed to `DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer`, one new exclusion added, doc comment updated.
2. **A structurally different cascade, specific to this phase**: `RaceDateAlignmentVerifier` is one of the nine Phase 4G.3B.3 verifiers governed by a **shared** test helper (`DarkReachabilityAssertions.AssertVerifierIsReachableOnlyFromDarkOrchestrator`), hardcoded to expect exactly one caller file (`SafetyVerificationOrchestrator.cs`) for all nine verifiers. This orchestrator is a second, legitimate caller — but only for `RaceDateAlignmentVerifier`, not the other eight. The shared helper was extended with an additive, opt-in `additionalAllowedCallerFileNames` parameter (default `null`, preserving the other eight verifiers' exact existing behavior unchanged) rather than a blanket loosening. Three call sites needed updating to pass the new file name for `RaceDateAlignmentVerifier` specifically: `RaceDateAlignmentVerifierTests.cs`'s own test, and both aggregate loops in `DarkReachabilityAssertionsTests.cs`.

**Doc-comment self-check** (per Phase 4G.5G's own lesson, verified proactively this time rather than discovered by a failing run): this pass's new production file was grepped for all four prior phases' monitored orchestrator identifiers (`DynamicCoreWorkoutBindingOrchestrator`, `DynamicCoreVolumeAndLongRunOrchestrator`, `DynamicCoreWeekSkeletonOrchestrator`, plus its own `DynamicCoreSessionPrescriptionOrchestrator` type reference) before finalizing. One incidental prose mention of `DynamicCoreWorkoutBindingOrchestrator` was found and reworded (not exclusion-listed, since it was documentation, not a real reference) before the first full-suite run — caught proactively, not by a failing test this time, though the fix pattern is identical to Phase 4G.5G's.

## 7. Test matrix

`StartDate` on Monday, `StartDate` midweek, race date on the exact week boundary, preferred days with safe spacing, preferred days with adjacent easy days, invalid long-run preference — each at 8/12/14 weeks (or once, for the single invalid-preference fail-closed case) — plus the required validation assertions (session dates present/unique, week contiguity, session order, long-run day respected, spacing respected via `RaceDateAlignmentVerifier`'s own checks, race alignment, total sessions = weeks×4) run on every success case via a shared `AssertRequiredInvariants` helper.

```text
DynamicCoreCalendarMaterializationOrchestratorTests: 27 passed, 0 failed, 0 skipped
DynamicCoreSessionPrescriptionOrchestratorTests (re-run after Section 6's reconciliation): 79 passed, 0 failed, 0 skipped
All repo-wide DarkReachability tests: 30 passed, 0 failed, 0 skipped
```

## 8. Final cross-phase pipeline validation

**Item 1 — no cross-layer contradiction, all 7 horizons, pilot profile.** `FinalCrossPhaseValidation_AllSevenHorizons_NoCrossLayerContradiction_PilotProfile` runs the full composed pipeline (allocator → skeleton → binding → volume/long-run → pace → calendar) for 8-14 weeks and cross-checks every adjacent layer boundary: 4G.5D skeleton weeks ↔ 4G.5E bound weeks (identical `WeekNumber`/phase-key sequence); 4G.5E bound weeks ↔ 4G.5F volume-plan weeks (every bound week has a matching volume-plan entry with the same phase key); 4G.5E bound sessions ↔ 4G.5G final-prescribed sessions (every bound session — keyed by `(WeekNumber, Date)`, since two `EASY_SUPPORT` sessions per week share a structural role — has a matching prescribed session with the identical workout identity); 4G.5H's own dated-calendar week/session counts match the final prescribed plan exactly. **Result: 7/7 horizons pass, zero contradictions found.**

**Item 2 — 12-week end-to-end composed output vs. the live pilot's actual output.** Confirmed byte/value-identical at the dated-calendar layer (Section 6) and, independently, at the final-prescribed-session layer (Phase 4G.5G's own regression, re-confirmed fresh in Section 4) — together these cover every field the full five-layer composition produces. No `CatalogPreviewGenerator` HTTP call was possible (the pilot candidate's `DRAFT` catalog status blocks it, as established in every prior phase of this session) — "the live pilot's actual output" is therefore reconstructed by calling the same real, unmodified components `CatalogPreviewGenerator` itself calls, in the same order, exactly as every phase 4G.5D-5H's own regression test has already done.

**Item 3 — consolidated 8-14 horizon reference table**, produced end-to-end through the full composed pipeline, one final time:

| Weeks | Phase allocation (F/B/RS/T) | Sessions | Peak volume (km) | Final session date |
|---|---|---|---|---|
| 8 | 2/3/2/1 | 32 | 32.5 | 2026-09-27 |
| 9 | 2/3/3/1 | 36 | 34.0 | 2026-10-04 |
| 10 | 2/3/4/1 | 40 | 35.0 | 2026-10-11 |
| 11 | 2/4/4/1 | 44 | 36.5 | 2026-10-18 |
| 12 | 3/4/4/1 | 48 | 38.0 | 2026-10-25 |
| 13 | 4/4/4/1 | 52 | 39.5 | 2026-11-01 |
| 14 | 4/5/4/1 | 56 | 41.0 | 2026-11-08 |

(`StartDate = 2026-08-03`, Monday; `RaceDate` = exact week boundary in each case; `RaceDateAlignmentOutcome.Pass` in every row.)

## 9. New findings — none required a TD

No calendar-materialization gap, no cross-layer contradiction (Section 8, Item 1), and no unsafe outcome was found anywhere in this pass's matrix. `TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001` (already `CLOSED`, Phase 4G.3B.4a) is directly relevant context — this pass's use of the dark `RaceDateAlignmentVerifier` across 8-14 weeks is exactly the intended, already-anticipated use of that closure's own "internal-ownership-only" design, not a new finding about it; no addendum was recorded since nothing about that TD's own scope or evidence changed. No other open TD's interaction changed. Per this pass's own governance requirement, this section documents that determination explicitly rather than silently omitting it.

## 10. Full backend suite

```text
Run 1: Failed: 0, Passed: 1738, Skipped: 1, Total: 1739
Run 2: Failed: 0, Passed: 1738, Skipped: 1, Total: 1739
```

Both runs match exactly (+27 vs. Phase 4G.5G's `1711` baseline, matching `DynamicCoreCalendarMaterializationOrchestratorTests`' own 27 new tests exactly — every other test file's own count is unchanged; only reachability-test bodies were renamed/extended).

## 11. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/DynamicCoreCalendarMaterializationOrchestrator.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DynamicCoreCalendarMaterializationOrchestratorTests.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/DynamicCoreSessionPrescriptionOrchestratorTests.cs` (existing file, one test updated — see Section 6.1; no production file modified).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DarkReachabilityAssertions.cs` (existing shared test-infrastructure file — one additive optional parameter; no existing verifier's default behavior changed).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DarkReachabilityAssertionsTests.cs` (existing file, two call sites updated to pass the new parameter for `RaceDateAlignmentVerifier` only).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/RaceDateAlignmentVerifierTests.cs` (existing file, one call site updated).
- `PHASE4G_5H_DYNAMIC_CORE_CALENDAR_MATERIALIZATION.md` (new, this document).

No true production component (`CatalogWeekSkeletonCalendarMaterializer.cs`, `RaceDateAlignmentVerifier.cs`, `CatalogPreviewGenerator.cs`, or any catalog JSON file) was modified.

## 12. Stop conditions — none triggered

- Calendar assignment requires changing established PreferredDays semantics: **not triggered**.
- Race date off by one: **not triggered** — exact-week-boundary case explicitly tested.
- Session collision: **not triggered** — date uniqueness confirmed in every case.
- 12-week dated output changes unexpectedly: **not triggered** — Section 6's regression proves exact equality.
- End-to-end 12-week composed output diverges from the live pilot: **not triggered** — Section 8, Item 2.
- Cross-layer contradiction silently worked around: **not triggered** — Section 8, Item 1 found none to work around.
- Zero-call-site proof (for the new orchestrator) cannot be established: **not triggered** — established via grep and two executable tests.

## 13. Commit/push status

No file was staged. No commit or push was performed.
