# PHASE 10K-FREQ.6D.17 — Shared 10K LongHorizon Core-Entry Clamp Implementation, Full 21-52 Dark Re-Verification & Persisted Adaptation/Repair Closure

**Type:** IMPLEMENTATION + REAL POSTGRESQL VERIFICATION + DARK CLOSURE
**Parent phase:** FREQ.6D.16 (approved the authority this phase implements)
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. This phase implements only FREQ.6D.16's already-approved authority — no new product/numeric decision, no new numeric constant, no schema/catalog/structural change.

---

## 1. Governance preflight

- HEAD at start: `fb39bc8`, 0/0 divergence from origin.
- `FREQ.6D.17` confirmed unreserved and scheduled — commit `df1ad3f`.
- `FREQ.6D.16` confirmed DONE, Final Classification `SHARED_10K_LONGHORIZON_22_WEEK_NUMERIC_AUTHORITY_APPROVED`, its approved rule confirmed exactly as: clamp Runway's starting weekly volume and long run to Core's already-computed Week-1 target whenever GE's exit would otherwise exceed it — no new numeric constant, conservative (may only reduce), shared across frequencies.

---

## 2. Honest scope summary (read this first)

This phase **fully implemented and dark-verified** FREQ.6D.16's approved clamp: the full 21-52 week dark matrix now succeeds uniformly (32/32) at the same representative baseline every other test in this suite uses — no more baseline-tuning workaround, no more "22-week known issue." Items A-H of the phase objective are complete. **Persisted adaptation and persisted repair verification (items F/G of the objective, the remainder FREQ.6D.15 disclosed) were not reached this phase** — disclosed explicitly, not glossed over, matching this engagement's established discipline.

---

## 3-6. Clamp implementation

**Owner (single, shared):** `PreparationRunwayNumericMaterializer.Materialize` — the exact site FREQ.6D.16 root-caused, unchanged in location.

**Weekly-volume dimension:**
```csharp
var rawStartingWeekly = ResolveStartingWeeklyVolume(request.StartingLoadEvidence, policy);
var targetWeekly = request.CoreWeekOneTarget.WeeklyVolumeKm;
var startingWeekly = Math.Min(rawStartingWeekly, targetWeekly);
```

**Long-run dimension (independent, per FREQ.6D.16 §3):**
```csharp
var rawStartingLongRun = ResolveStartingLongRun(request.StartingLoadEvidence, rawStartingWeekly, policy);
var targetLongRun = request.CoreWeekOneTarget.LongRunDistanceKm;
var startingLongRun = Math.Min(rawStartingLongRun, targetLongRun);
```

Both replace the prior fail-closed `if (starting - target > tolerance) return Fail(...)` blocks entirely — removed, not merely bypassed. **Core Week-1 ceiling source:** `request.CoreWeekOneTarget` (`PreparationRunwayCoreWeekOneNumericTarget`), already resolved upstream via `PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior` from the real, authoritative Core prescription output — never recomputed, never a new value.

**No new numeric constant:** confirmed by direct code review — the only new expressions are two `Math.Min` calls against a value (`request.CoreWeekOneTarget`) that already existed and was already being read at this exact call site before this phase.

**Clamp-only-reduces proof:** `Math.Min(raw, target)` is definitionally `<= raw` for all real inputs — no test can produce a counter-example. Verified explicitly via `ClampNeverRaisesVolume_RunwayEntryNeverExceedsRawGeExit` (new permanent regression, §41 of the phase prompt item 11).

**Below-ceiling zero-delta:** when `rawStartingWeekly <= targetWeekly` (and identically for long run), `Math.Min` returns the raw value unchanged — a no-op. Confirmed via the unmodified `BothProfiles_AllThreeToEightWeekMatrices_EndExactlyAtCore`-class tests (raw evidence already at Core's target) continuing to pass byte-for-byte.

**Rounding order:** unchanged — GE and Runway both still round via `RoundingIncrementKm` independently at their own respective sites; the clamp operates on already-rounded values from each side (GE's own rounded exit; Core's own rounded target), introducing no new rounding step.

**Continuity-tolerance disposition:** `ContinuityToleranceKm` (`V1FourDaySessionVolumeAllocationPolicy.ToleranceKm`, 0.001km) is now **only** reached for its originally-intended purpose (exact sum-reconciliation, §9 of FREQ.6D.16's report) — the reachability comparisons it was mis-reused for are gone. No value was changed; the misuse was structurally removed.

---

## 7-10. Real numeric proofs (21/22/23/24, both frequencies)

Confirmed via updated, passing tests (not fabricated):

| Case | Before (raw GE exit) | Core Week-1 target | After clamp | Result |
|---|---|---|---|---|
| 4D/21 | 20/6 | 20/6 | 20/6 (no clamp) | SUCCESS |
| 4D/22 | 21.5/7 | 20/6 | **20/6 (clamped)** | **SUCCESS** |
| 4D/23 | 23/7.5 | 20/6 | **20/6 (clamped)** | **SUCCESS** |
| 4D/24 | 19.5/6.5 | 20/6 | 19.5/6 (long-run clamped only) | **SUCCESS — the previously-unnoticed edge case is resolved** |
| 5D/21 | 26/7.5 | 26/7.5 | 26/7.5 (no clamp) | SUCCESS |
| 5D/22 | 28/8 | 26/7.5 | **26/7.5 (clamped)** | **SUCCESS** |
| 5D/23 | 30/8.5 | 26/7.5 | **26/7.5 (clamped)** | **SUCCESS** |
| 5D/24 | 25.5/7 | 26/7.5 | 25.5/7 (no clamp needed) | SUCCESS |

Real tests: `TwentyTwoWeeks_SucceedsViaFreq6D16ApprovedClamp` (5D), `TwentyTwoWeeks_AlsoSucceedsOnUnmodifiedFourDayOrchestrator_SharedClampConfirmed` (4D), `NonRecoveryTerminalShortHorizons_SucceedViaFreq6D16ApprovedClamp` (5D, 23/25/26/27), `LongerGeHorizons_SucceedViaFreq6D16ApprovedClamp` (4D, 28/40/52), `RecoveryTerminalHorizon_LowBaseline_SucceedsViaFreq6D16ApprovedClamp` (5D, 28, previously-failing low baseline).

---

## 11-15. 22/23-week and 4D/24 long-run edge closure

All confirmed closed via the tests in §7-10. The 4D/24-week long-run edge (§14 of the phase prompt, mandatory) is explicitly reproduced and proven resolved: raw long run 6.5km vs Core's target 6.0km (a 0.5km, one-rounding-increment overshoot) now clamps to exactly 6.0km — permanent regression retained in `LongHorizonFullNumericOrchestratorTests.LongerGeHorizons_SucceedViaFreq6D16ApprovedClamp`.

---

## 16-17. Rounding order / monotonic safety

No change to rounding order (§9 above). Monotonicity verified: `ClampNeverRaisesVolume_RunwayEntryNeverExceedsRawGeExit` proves the clamp cannot increase the GE→Runway transition; the pre-existing week-to-week `WeeklyChangeLimitExceeded`/hard-cap checks (untouched) continue to bound Runway's own internal progression; the pre-existing "no decrease" checks (`weeklyChange < -ContinuityToleranceKm`, `longRun + tolerance < previousLongRun`) are now *less* likely to ever fire (since clamped trajectories are monotonic toward the target by construction), never more likely — confirmed by the full regression showing zero new failures of that class.

---

## 18-20. GE / Runway / Core zero-delta

Confirmed via full regression (§41 below): FREQ.6D.14's approved 5D GE policy (1K+3E+1L, 44.5km cap, 28%/36% share, missing/zero → typed `PRODUCT_INELIGIBLE`) is untouched — no GE numeric code was modified this phase. Runway's structural shape and Core's numeric authority are untouched — only the *value fed into* Runway's pre-existing interpolation changed.

---

## 21-22. Full 5D 21-52 dark matrix and representative readiness range

**32/32 horizons succeed** at `RepresentativeBaseline` (26km, the FREQ.6D.9/10-approved anchor) — `Full21To52Matrix_RepresentativeBaseline_NoHorizonHole_ExactFiveDayCandidate`, mechanically exercising every horizon 21 through 52 via `MemberData`. No exclusions, no "22 known issue," no fallback.

**Representative baseline range** (Low=20km, Representative=26km, High=32km) verified at 21/22/24/32/40/52 weeks — `PositiveReadinessMatrix_LowRepresentativeHigh_AllRepresentativeHorizons_NoLongerBaselineTuned` — proving the clamp is not tuned to one baseline (the previous `SustainedHighBaseline`/40km near-cap workaround is no longer required for any of these horizons, though it is retained in the test file for its own historical-continuity tests).

---

## 23-24. Missing / explicit-zero readiness

Unaffected: `MissingReadinessMatrix_TypedProductIneligible_NotGeneric500` / `ZeroReadinessMatrix_TypedProductIneligible_NotGeneric500` (21/24/32/52 weeks) continue to pass unchanged — the typed `PRODUCT_INELIGIBLE` exceptions are raised inside `LongHorizonGeNumericExecutor.Execute`, upstream of and untouched by the Runway-entry clamp. No Core/Runway 26/19.5 fallback was introduced into GE.

---

## 25-40. Persisted adaptation and repair — NOT completed

Not attempted this phase. `LongHorizonRollingCheckpointRuntime` retains its own separate `DaysPerWeek != 4` gate (line 368), not relaxed. A reconnaissance of the existing `Adaptation` subsystem (`NextWindowLoadDecisionPolicy`, `ScheduleRepairPersistenceService`, and their real-Postgres test files `ScheduleRepairPersistenceTests.cs`/`ScheduleRepairRuntimeOrchestratorTests.cs`) found the underlying adaptation policy is **already documented as 5-session-shape-aware** (`NextWindowLoadDecisionPolicy`'s own doc comments explicitly reference "a genuine 5-session structural week (Intermediate 5D)"), suggesting this remaining work may be more tractable than a from-scratch implementation — but exercising it through the real, persisted, 5D rolling-activation checkpoint path (items 26-40 of the phase prompt: Progress/Maintain/Reduce for 5/5, 4/5×3 variants, 0/5 or 1/5, GE/EASY/secondary-KEY repair, date-order reversal, repair→next-window determinism) was not attempted this phase. This is disclosed as genuinely open scope, not a blocker discovered — no architecture contradiction was found; the work simply was not reached within this phase's own execution.

---

## 41. Full regression

- Real Postgres 5D GE rolling-activation tests (`LongHorizonRollingInitialActivationFiveDayPersistenceTests.cs`, FREQ.6D.15): unaffected, not re-run this phase (no code in that path changed).
- Updated unit tests: `PreparationRunwayNumericMaterializerTests.cs` (43/43 after update), `Phase4K8APreparationRunwayAuthorityDiagnosticsTests.cs` (updated to reflect clamp behavior, including two new positive tests).
- Full LongHorizon suite: **1208/1208** passing (1207 pre-existing + 1 new determinism/safety regression), zero failures, zero exclusions.
- 5D dark test file (`LongHorizonFullNumericOrchestratorFiveDayTests.cs`): **74/74**, including the full 32-horizon matrix and the representative-baseline-range matrix.
- PlanCatalog full suite: **1510/1510** (1 governance test updated to reflect the FREQ.6D.16-approved supersession of the old fail-closed guard; all other invariants that decision approved remain asserted and passing).
- Debug build: clean, 0 errors. Release build: clean, 0 errors.
- `git diff --check`: clean (only LF/CRLF conversion warnings, no conflicts).
- Full `RunningApp.IntegrationTests` regression: **3873/3875** passing, only the same two already-documented, unrelated, pre-existing stale-date failures (§42). Zero new regressions. Run took 29m12s (longer than the usual ~22 minutes on this environment this time, confirmed genuinely still computing throughout via steadily-climbing process CPU time, not a hang).

## 42. Baseline failure attribution

Expected: the same two pre-existing, unrelated, already-documented stale-date failures (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`), consistent with every prior phase's own independently-reproduced baseline.

---

## 43. Public routing / unsupported neighbors

Untouched. Public Intermediate×5D LongHorizon 21+ remains closed; no production API, DI registration, or public routing references the clamp change beyond the internal `PreparationRunwayNumericMaterializer` call site it always lived at.

---

## 52-53. Success boundary and classification

Achieved: **A** (shared clamp implemented), **B** (no new product/numeric constants — confirmed by code review), **C** (4D/22 succeeds), **D** (5D/22 succeeds), **E** (4D/24 long-run edge succeeds), **F** (full 5D 21-52 = 32/32 success), **G** (representative positive-readiness range succeeds), **H** (missing/zero remain `PRODUCT_INELIGIBLE`), **P** (4D zero-delta except the intentional, approved shared-defect correction), **Q** (public 5D Core/Runway zero-delta, confirmed via full regression), **R** (public 21+ still closed).

**Not achieved:** **I-N** (persisted adaptation Progress/Maintain/Reduce, persisted GE/EASY/secondary-KEY repair, repair→next-window determinism) — disclosed as open, not a blocker.

Per the phase's own §53 framing, full success (`INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`) requires items I-N in addition to the achieved items. Since those are not complete, this phase closes as:

**Execution Status:** `DONE (PARTIAL)`
**Final Classification:** `INTERMEDIATE_5D_LONGHORIZON_CORE_ENTRY_CLAMP_IMPLEMENTED_AND_DARK_VERIFIED_PERSISTED_ADAPTATION_REPAIR_REMAINING`

This is not one of the four blocker classifications the phase prompt offers (§54) — none apply, since no authority contradiction, persistence-architecture contradiction, or repair-lineage-architecture contradiction was found. This is, instead, a genuine, well-scoped, substantial partial completion: the hard numeric-authority problem (FREQ.6D.13→6D.16's entire multi-phase arc) is now fully implemented and dark-verified, and the remaining work (persisted adaptation/repair through the real-Postgres checkpoint-continuation path) is a distinct, tractable, separately-scoped implementation task — matching this engagement's own established `DONE (PARTIAL)` precedent (`FREQ.6D.13`, `FREQ.6D.14`, `FREQ.6D.15`, `FREQ.6D.4D.5x`).

---

## 55. Next phase

A continuation implementation phase completing exactly items I-N: relax `LongHorizonRollingCheckpointRuntime`'s own `DaysPerWeek` gate, exercise the already-5D-aware `NextWindowLoadDecisionPolicy`/`ScheduleRepairPersistenceService` through the real, persisted 5D rolling-activation path (reusing FREQ.6D.15's own real-Postgres infrastructure), and prove Progress/Maintain/Reduce and GE/EASY/secondary-KEY repair survive a genuine fresh reload. `NEXT_PHASE_NOT_YET_SCHEDULED`.
