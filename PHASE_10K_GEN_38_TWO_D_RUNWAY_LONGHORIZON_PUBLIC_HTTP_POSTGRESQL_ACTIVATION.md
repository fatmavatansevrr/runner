# PHASE 10K-GEN.38 — 2D Preparation Runway (15-20wk) + LongHorizon (21-52wk) Combined Public HTTP/PostgreSQL Activation

**Parent authority**: `GEN.27`-`GEN.29` (2D Preparation Runway repeating-pattern mechanism, block-role/anchor reconciliation including the frozen `AerobicStrength` Pattern-A/B split, Beginner/Intermediate admission, numeric dispatch, 2-slot calendar composition, long-run clamp — dark-implemented and dark-verified, `2D_RUNWAY_BLOCK_ROLE_RECONCILIATION_IMPLEMENTED_AND_DARK_VERIFIED`), `GEN.30`-`GEN.37` (2D LongHorizon GE structure decision, GE alternation generalization, defect closure, admission gate opening, bundle publication, JIT-boundary verification, and finally `GEN.37`'s `TWO_D_LONGHORIZON_FULL_GE_RUNWAY_CORE_CHAIN_COMPLETE_GLOBALWEEKNUMBER_PARITY_STRICT_ACROSS_ENTIRE_PLAN`), `GEN.18` (2D Core's 10-14 week representability boundary, unaffected by this phase), `GEN.20`/`GEN.25` (this engagement's public-activation phase-shape precedent, directly modeled here), `GEN.10` (the original combined-activation pattern and its `OBSOLETE_PRE_ACTIVATION_ASSERTION` correction discipline, reused in §5).
**Phase type**: IMPLEMENTATION (public gate widening) + REAL HTTP/POSTGRESQL VERIFICATION + REGRESSION-CORRECTION (obsolete pre-activation assertions)
**Execution status**: DONE
**Final classification**: `TWO_D_BEGINNER_INTERMEDIATE_FULLY_PUBLICLY_ACTIVE` — Core (10-14wk, `GEN.20`) + Preparation Runway (15-20wk, this phase) + LongHorizon (21-52wk, this phase), both levels, full governed span minus the established, permanent 8-9 week non-support boundary (`GEN.18`).

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `d10c166` (`docs(gen-37): backfill governance commit SHA for GEN.37`). `git fetch && git rev-list --left-right --count origin/main...HEAD`: `0 0` — in sync. `git status --porcelain` (excluding the standing tracked `bin`/`obj` rebuild noise and the pre-existing, untouched `baseline_tmp`/`ten-k-pilot-domain-decision-audit.*` modifications and stray `.trx` result files from prior sessions): clean of unexpected changes. `PHASE_LEDGER.md`'s last row is `GEN.37` (seq 140) — **`GEN.38` confirmed correct** as the next free phase ID, both from the ledger and by direct `PHASE_10K_GEN_*.md` file listing (highest existing is `GEN.37`).

Required reading performed directly from the repository, in full: `PHASE_10K_GEN_29_TWO_D_RUNWAY_BLOCK_ROLE_RECONCILIATION_IMPLEMENTATION.md`, `PHASE_10K_GEN_37_TWO_D_LONGHORIZON_CORE_PACE_ANCHOR_DECISION_EXECUTION_AND_FULL_CHAIN_COMPLETION.md`, `PHASE_10K_GEN_18_2D_CORE_REPRESENTABILITY_BOUNDARY_DETERMINATION.md`, `PHASE_10K_GEN_20_2D_BEGINNER_INTERMEDIATE_CORE_PUBLIC_ACTIVATION.md`, `PHASE_10K_GEN_25_BEGINNER_3D_CORE_PUBLIC_ACTIVATION.md`, `PHASE_10K_GEN_10_ADVANCED_3D_4D_5D_6D_COMBINED_PUBLIC_HTTP_POSTGRESQL_ACTIVATION.md`, and `MASTER_ROADMAP.md`'s full 2D-axis narrative (§2 support matrix cells plus the GEN.11-GEN.37 chronological paragraphs). `LongHorizonActiveReadModelProvider.cs` read directly: confirmed `GetHomeAsync`'s `TodayWorkout` resolution (`sessions.Where(s => s.AssignedDate == today)`, `today = DateOnly.FromDateTime(DateTime.UtcNow)`) is fully generic — no level/frequency-specific branching anywhere in this file — confirming the previously-diagnosed `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` failure is a real test-input date-literal collision (`ConfirmAsync` hardcodes plan-start date `2026-09-07`) against the real, correct calendar date, not a production defect — the same conclusion `GEN.36`/`GEN.37` already reached and this phase independently re-confirms by direct code inspection before treating the failure as expected.

---

## 1. Scope, exactly as governed

Open the real public HTTP/PostgreSQL routing gate for **Beginner×2D** and **Intermediate×2D** across **Preparation Runway (15-20wk)** and **LongHorizon (21-52wk)** — the two horizons `GEN.29` and `GEN.37` respectively left dark-verified but not publicly activated. This is an activation phase implementing already-approved, already-dark-verified authority only — no new product/numeric authority is introduced anywhere in this phase. 2D Core (10-14wk, `GEN.20`) and the 8-9 week non-support boundary (`GEN.18`) are unaffected and unretouched.

---

## 2. Public gate widening — the only two production-code changes

### 2.1 `V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayLevelFrequency`

Widened to admit `(Beginner, 2)` and `(Intermediate, 2)`, alongside the pre-existing `(Intermediate, 4/5/6)` and `(Advanced, 3/4/5/6)`. This is the single allow-list `PlanServices.IsPreparationRunwayPilotScope` consults (`GEN.29`'s own internal `IsSupportedPreparationRunwayCandidate` dark-consistency check was already widened; only the real public gate needed touching). No other file needed a change: `CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync` was already fully generic (resolves the candidate via `V1CatalogPilotIdentityPolicy.ResolveCandidate(request.Level, request.DaysPerWeek)`, validates against the candidate's own `CoreCycle` bounds and the already-computed `CoreHorizonDecision` — confirmed by direct inspection, not assumed), and `PlanServices.GeneratePreviewAsync`'s own routing dispatch was already frequency-agnostic.

### 2.2 `LongHorizonPublicPlanService.ValidatePilot`

Widened `levelFrequencyEligible` to add `(Beginner, 2)` and `(Intermediate, 2)`, alongside the pre-existing `(Intermediate, 2 or 4/5/6)`\* and `(Advanced, 3/4/5/6)`. This is the **only** LongHorizon-specific gate in the entire request path: `LongHorizonRollingInitialActivationContracts` and `LongHorizonRollingCheckpointRuntime` (the two internal dark eligibility gates) already admitted exactly `(Beginner, 2)` alongside Intermediate's existing set — confirmed by direct grep before writing any code — so no dark-layer widening was required, only this one public gate.

\* Corrected inline: prior to this phase the comparison read `command.DaysPerWeek is 4 or 5 or 6` for Intermediate (no 2D at all); this phase's diff adds `2` to that list.

No other file in the LongHorizon or Preparation Runway subsystem required a change. `LongHorizonStructuralMaterializer`'s `daysPerWeek is not (2 or 3 or 4 or 5 or 6)` gate, `LongHorizonFullNumericOrchestrator`'s `daysPerWeek is not (4 or 5)` gate (irrelevant here — never reached by the rolling/JIT path this phase's LongHorizon flow uses), and every other component `GEN.29`/`GEN.33`-`GEN.37` already generalized were re-confirmed unaffected by full regression (§6).

---

## 3. Real HTTP/PostgreSQL verification — new test file (`Gen38TwoDayRunwayLongHorizonPublicActivationTests.cs`, 41 tests)

### 3.1 Preparation Runway (15-20wk)

- **`RunwayHorizons_FifteenThroughTwentyWeeks_PublicPreviewSucceeds_ForBothLevels`** (12 cases: 2 levels × 6 week counts): real `GeneratePreview` HTTP success, `days_per_week=2`, exactly the requested week count of 2-day weeks.
- **`Full15To20Matrix_BothLevels_AllSucceed`**: exhaustive 15-20 sweep, both levels, 6/6 successes each.
- **`Runway_Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek`** (both levels): real confirm (via a dedicated `CustomWebApplicationFactory` instance with `PreparationRunwayPilotActivation:ConfirmationEnabled=true`, per `PreparationRunwayConfirmationEndToEndTests`'s own established pattern — the shared collection factory keeps this flag `false` by design so every pre-existing "not yet confirmable" assertion elsewhere keeps passing unmodified) + fresh-PostgreSQL reload: correct `Level`, `DaysPerWeek=2`, 18 persisted weeks, 36 persisted days, 18 `LONG_RUN` days.
- **`Runway_ConfirmedPlan_HomeCalendarAndTrainingDayReads_AllSucceed`**: real Home/Calendar/TrainingDay-detail reads against a confirmed, persisted 2D Runway plan.
- **`UnsupportedNeighborFrequencies_RunwayHorizon_RemainClosed`** (Beginner×3D/4D at a Runway-range horizon): real HTTP 422, `PLAN_HORIZON_COMPOSITION_REQUIRED`, confirming the Runway widening did not leak to frequencies never designed/approved for Runway.
- **`Core_EightOrNineWeeks_StillFailsClosed_ZeroDeltaAfterRunwayWidening`**: real HTTP 422, `TWO_DAY_CORE_EIGHT_OR_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL`, confirming `GEN.18`'s boundary is untouched by this phase's Runway-only gate change.

### 3.2 LongHorizon (21-52wk)

- **`LongHorizon_RepresentativeHorizons_PublicPreviewSucceeds_ForBothLevels`** (10 cases: 2 levels × 5 representative week counts spanning 21-52): real HTTP success, `schedule_strategy=rolling_long_horizon`, correct `total_weeks`/`days_per_week`.
- **`LongHorizon_Full21To52Matrix_BothLevels_AllRouteToLongHorizon`**: exhaustive 21-52 sweep, both levels, 32/32 successes each (64/64 total).
- **`LongHorizon_Confirm_ReloadsFromFreshPostgres_WithCorrectLevelAndDaysPerWeek`** (both levels): real confirm + fresh-PostgreSQL reload, correct `Level`/`DaysPerWeek`/`ScheduleStrategy`.
- **`LongHorizon_PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithStrictWeekParity`** (both levels) — the centerpiece proof: a real, confirmed, persisted `totalWeeks=21` (`geWeeks=1`) plan is driven through 3 real `POST /api/v1/plans/active/long-horizon/activate-next-window` calls (each preceded by real `POST /api/v1/training-days/rolling/{id}/complete` calls against every Planned session in the current window, mirroring `Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests`'s own established full-lifecycle pattern exactly), reaching real, organically-materialized Core at global week 10 — the same GE(1)→Runway(8, weeks 2-9)→Core(12, weeks 10-21) fixed composition `GEN.30`/`GEN.26 §3.1` established, now reached for the first time through real public HTTP rather than only the dark orchestration/persistence layer `LongHorizonGen35TwoDayJitBoundaryTests` already proved. Asserts: exactly 2 real persisted sessions at global week 10 (one `LONG_RUN`, one of `KEY_SESSION`/`EASY_SUPPORT` — never both, never neither); week 10 (even) correctly carries `EASY_SUPPORT` not `KEY_SESSION` (`GEN.37`'s frozen odd=Pattern-A/even=Pattern-B parity, asserted directly, not merely "some role present"); **strict `GlobalWeekNumber` parity across every activated week from 1 through the final Core window boundary** (GE+Runway+Core combined), read from the real database after the driving HTTP calls completed — the concrete `GEN.37` milestone, now independently reproved through real public HTTP for the first time; Home/Calendar/detail reads still succeed after real organic Core entry.
- **`LongHorizon_MissingReadiness_NeverReturnsIdentityUnsupported`** (both levels): confirms the identity gate and the request's own readiness/evidence eligibility are correctly layered — a missing-readiness request never surfaces as `LONG_HORIZON_PILOT_UNSUPPORTED`.
- **`LongHorizon_UnsupportedNeighbors_RemainClosed`** (Beginner×4D, Advanced×2D): real HTTP failure, `LONG_HORIZON_PILOT_UNSUPPORTED`, confirming the widening did not leak to untouched neighbor cells.

### 3.3 Zero-delta

- **`ZeroDelta_TwoDayCore_TenToFourteenWeeks_StillWorksAfterRunwayAndLongHorizonWidening`** (both levels): real HTTP success at the Core-anchored 12-week horizon, confirming this phase's Runway/LongHorizon-only gate widening did not disturb Core's own routing.
- **`ZeroDelta_EveryOtherPubliclyActiveFrequency_Unaffected`**: real HTTP success for Beginner×4D/3D, Intermediate×3D/4D/5D/6D, Advanced×3D/4D/5D/6D.

**41/41 pass** (standalone run, then again as part of the targeted zero-delta suite in §4, then again as part of the full regression in §6).

### 3.4 A real, low LongHorizon onboarding baseline was required, not a new number

Per `GEN.35`'s own already-disclosed reason (reused verbatim, not rediscovered): 2D's much lower `PeakVolumeBand` ([16,22]km Beginner / [20,30]km Intermediate, `GEN.11`) means a higher onboarding baseline (as 5D/6D's own LongHorizon public tests use) trips the pre-existing, correctly-restrictive `AboveTarget` direction guard for a reason unrelated to 2D-specific code. All of this phase's LongHorizon requests reuse `GEN.35`/`GEN.36`/`GEN.37`'s own already-dark-verified baseline (8km weekly / 3km longest run / 2 runs per week) — a test-input choice appropriate to 2D's already-approved volume range, not a new numeric authority.

---

## 4. Unsupported-neighbor closure — full results, all real HTTP

Run as a dedicated targeted suite (144→185 tests across six files, see §4.1) before the full regression, per this engagement's "verify independently before trusting the number" discipline:

| Check | Result |
|---|---|
| `GEN.18`'s 2D Core 8-9 week non-support | Zero-delta — still real-HTTP fails closed with the identical typed `TWO_DAY_CORE_EIGHT_OR_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL` rejection (re-verified via `Gen20TwoDayCombinedPublicActivationTests` + this phase's own `Core_EightOrNineWeeks_StillFailsClosed_ZeroDeltaAfterRunwayWidening`) |
| 2D Core 10-14wk (`GEN.20`) | Zero-delta — `Gen20TwoDayCombinedPublicActivationTests`'s own Core-horizon tests all still pass unmodified |
| Beginner×3D Core (`GEN.24`/`25`, including explicit-zero `PRODUCT_INELIGIBLE`) | Zero-delta — `Gen25BeginnerThreeDayCorePublicActivationTests` re-run in full, unaffected (no file this phase touched is referenced by any Beginner×3D code path) |
| Every other already-`PUBLICLY_ACTIVE` frequency at both levels | Zero-delta — `Gen10AdvancedCombinedPublicActivationTests` re-run in full (Advanced 3D/4D/5D/6D); this phase's own `ZeroDelta_EveryOtherPubliclyActiveFrequency_Unaffected` covers Beginner×4D/3D and Intermediate×3D/4D/5D/6D directly |
| **Intermediate×5D LongHorizon, public-layer zero-delta** (the distinct, explicitly-required re-verification — 2D's LongHorizon code paths share subsystem with 5D's, and this is the first time either is reachable via real public HTTP with the *other* also open) | Zero-delta — `Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests` (the pre-existing real-public-HTTP suite, including its own full `PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithDualKeyAndSurvivesRepair` GE→Runway→Core→repair proof) re-run in full against this phase's widened `ValidatePilot` and `IsSupportedPreparationRunwayLevelFrequency` — all pass unmodified, confirming the shared `LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingJitCompositionOrchestrator`/`PreparationRunwayCoreWeekOnePaceAdapter` machinery 2D and 5D both now traverse via real public HTTP produces byte-identical 5D behavior |
| `GEN.35`/`GEN.36`/`GEN.37`'s own dark JIT-boundary proofs | Zero-delta — `LongHorizonGen35TwoDayJitBoundaryTests` and `TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests` re-run in full, unaffected (this phase added no production code either file's own call chain depends on beyond the two gate widenings, which neither test's own direct-construction harness even routes through) |

### 4.1 Targeted suite counts

- First pass (before correcting §5's obsolete assertions): 144 tests, **10 failed** — all 10 identified as `OBSOLETE_PRE_ACTIVATION_ASSERTION` (see §5), zero real defects among them.
- Second pass (after correction, `Gen38...` file included): **185 tests, 185 passed, 0 failed** (≈14m48s).

---

## 5. Recurring-defect-family search at the public-routing/gate layer — one real finding, correctly classified

Per this phase's explicit instruction (this layer produced real defects at `GEN.20`, none at `GEN.25`): the first targeted run above surfaced exactly **10** pre-existing test failures, all in `Gen20TwoDayCombinedPublicActivationTests.cs` — `RunwayHorizon_TwoDay_FailsClosed_NeverReachesRunwayOrLongHorizon` (4 cases) and `LongHorizonEndpoint_TwoDay_FailsClosed_NeverReachesLongHorizon` (6 cases). Diagnosed directly: both tests asserted, correctly **at `GEN.20`'s own time**, that a 2D request at a Runway/LongHorizon-range horizon must fail closed — because neither mechanism existed yet. This phase's entire purpose is to make exactly those two horizons succeed, so both assertions became mechanically obsolete the moment the gate widened, not because of any defect in this phase's own diff.

This is **not a new hardcode-assumption defect** — it is the identical `OBSOLETE_PRE_ACTIVATION_ASSERTION` correction class `GEN.10 §6` first established and `GEN.25` also encountered (6 instances there). Per that established discipline: renamed and rewrote both tests (`RunwayHorizon_TwoDay_NowPubliclyActive_PerGen38` / `LongHorizonEndpoint_TwoDay_NowPubliclyActive_PerGen38`) to assert the new, correct, now-decided behavior — real HTTP success, correct template/schedule-strategy markers — rather than deleting them, preserving the file's own historical narrative via an updated class-level doc comment explaining the correction and citing this phase by name. The LongHorizon variant additionally required lowering its onboarding-baseline input (§3.4) since the file's own `TwoDayRequest` defaults are Core-appropriate, not LongHorizon-appropriate.

**Beyond this one (test-only, not production) correction, no new production-code defect was found anywhere in the public-routing/gate layer.** Both `IsSupportedPreparationRunwayLevelFrequency` and `LongHorizonPublicPlanService.ValidatePilot` were simple, additive allow-list widenings against already-generic downstream code (§2) — unlike `GEN.20` (which found two real session-distance-allocation-layer hardcodes the first time 2D reached that layer) or `GEN.29` (six real defects the first time 2D's real Runway orchestration ran end to end), this phase's widening reuses machinery `GEN.29`/`GEN.33`-`GEN.37` already exercised end to end at the dark/internal level — the public gate was the only remaining seam, and it required no compensating fix.

---

## 6. Full regression

Confirmed no other `dotnet test`/`testhost.exe` process running (unfiltered `tasklist | grep -i testhost` empty) immediately before each run; the two solutions' suites were never run concurrently.

### 6.1 `RunningApp.IntegrationTests`

**4386 total, 4382 passed, 4 failed** (≈40m9s). Total reconciles exactly: `GEN.37`'s own baseline `4345` + this phase's `41` new tests (`Gen38TwoDayRunwayLongHorizonPublicActivationTests`) = **4386** — `Gen20TwoDayCombinedPublicActivationTests`'s own two corrected tests (§5) kept their identical `InlineData` case counts (4 + 6 = 10), so no count shift from that correction. The 4 failures are the identical, already-named, pre-existing baseline set:

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks: 13)
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks: 14)
3. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`
4. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` — the disclosed, date-coincidental flake `GEN.36`/`GEN.37` identified. This phase's own execution date is again `2026-09-07` (the identical literal `ConfirmAsync` hardcodes), so the flake appeared again, exactly as the carried-forward operational note anticipated. Independently re-confirmed unrelated to this phase's diff (§0: `LongHorizonActiveReadModelProvider.cs`'s real `TodayWorkout` resolution is fully generic, no 2D/Runway/LongHorizon-specific branching; zero changes to this test file or any Home/Calendar controller/service this phase touched).

**Zero new regressions.**

### 6.2 `PlanCatalog.Tests`

**1510 total, 1510 passed, 0 failed** (≈6s), unchanged from `GEN.37`'s own baseline. No `plan-catalog/` file touched this phase.

---

## 7. Explicit constraints — confirmed respected

- **No new product/numeric authority**: both production changes (§2) are pure allow-list widenings admitting already-approved, already-dark-verified `GEN.27`-`29`/`GEN.30`-`37` identities; no numeric constant, product rule, or eligibility decision was invented.
- **No boundary widened beyond what was actually verified**: Runway remains exactly 15-20 weeks (bounded by the candidate's own `CoreCycle`-derived horizon check, unchanged); LongHorizon remains exactly 21-52 weeks (`LongHorizonCompositionResolver`'s own unchanged bounds); Core's 10-14 week boundary and the 8-9 week non-support classification are untouched and re-verified zero-delta (§4).
- **GEN.18's 8-9 week Core boundary unaffected**: confirmed both by direct inspection (no file this phase touched is in that check's call chain) and by real HTTP re-verification (§3.1, §4).
- **2D Core (GEN.20) zero-delta**: confirmed (§4).
- **Beginner×3D (GEN.24/25) zero-delta**: confirmed (§4).
- **Every other already-`PUBLICLY_ACTIVE` frequency, Advanced 3D-6D (GEN.10) zero-delta**: confirmed (§4).
- **Intermediate×5D's own LongHorizon zero-delta at the PUBLIC layer specifically**: confirmed (§4) — the first time both 2D's and 5D's LongHorizon code paths are reachable via real public HTTP simultaneously; no interaction defect found.
- **Recurring-defect-family search performed and disclosed**: §5 — one real, correctly-classified `OBSOLETE_PRE_ACTIVATION_ASSERTION` correction (test-only), no new production-code defect.
- **No sub-agents spawned**: all investigation, implementation, and verification performed directly with this session's own tools.

---

## 8. Final classification

```
TWO_D_BEGINNER_INTERMEDIATE_FULLY_PUBLICLY_ACTIVE
```

Beginner×2D and Intermediate×2D are now real, public, HTTP/PostgreSQL-verified capabilities across their **entire governed span**: Core (10-14wk, `GEN.20`), Preparation Runway (15-20wk, this phase), and LongHorizon (21-52wk, this phase) — minus the established, permanent, formally-closed 8-9 week non-support boundary (`GEN.18`, unaffected and re-verified zero-delta). This closes the 2D axis in its entirety for the first time in this engagement: every governed 2D horizon at both approved levels now has a final, real-HTTP-evidenced public classification, matching the shape of `INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE` (`FREQ.6D.27`) and `BEGINNER_FREQUENCY_AUTHORITY_COMPLETE` (`GEN.6`) for their own respective axes. Advanced×2D remains `OUT_OF_V1_SCOPE` (never designed, unaffected).

`NEXT_PHASE_NOT_YET_SCHEDULED` — 2D's public activation is now structurally complete across all three horizons at both approved levels. Any follow-on (e.g. adaptation/repair-path parity work analogous to the `FREQ.6D.11`-`22` Intermediate×5D/6D arc, should 2D LongHorizon need its own dedicated repair-path proof beyond what this phase's full-lifecycle test already exercises) is distinct, not-yet-scoped future work.

---

## 9. Governance

`PHASE_LEDGER.md` row appended (`GEN.38`, seq 141). `MASTER_ROADMAP.md`'s 2D axis support-matrix cells and axis-status paragraph updated to record Beginner×2D/Intermediate×2D as fully `PUBLICLY_ACTIVE` across Core+Runway+LongHorizon, closing the 2D axis. Two-commit self-referential SHA-backfill pattern followed: implementation commit (production fixes + new/corrected tests + this report + ledger/roadmap with a `PENDING` placeholder), then a second, documentation-only commit backfilling this row's own real SHA. Normal push only — no force, no force-with-lease.
