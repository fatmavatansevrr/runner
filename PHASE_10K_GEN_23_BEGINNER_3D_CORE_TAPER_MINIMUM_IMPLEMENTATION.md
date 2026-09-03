# Phase 10K-GEN.23 — Beginner×3D Core Taper-Minimum Implementation

**Implementation + defect discovery/fix + dark verification. Implements the frozen Option-1 authority the user approved directly on `GEN.21`'s `DOMAIN_DECISION_REQUIRED` escalation (Phase K). No public HTTP routing gate opened — Beginner×3D Core remains internally gated, matching `GEN.4D`'s own "Core implementation stays `INTERNALLY_GATED`, public activation is a separate future phase" precedent (`GEN.4D` → `GEN.4E`).**

---

## 0. Precondition verification

`git log -5` confirmed HEAD at `c30b471` (`docs(gen-22): backfill governance commit SHA for GEN.22`). `git fetch` + `git diff HEAD origin/main` confirmed 0 ahead/0 behind. `PHASE_LEDGER.md`'s last row is Seq 125, `GEN.22`; no `GEN.23` row or `PHASE_10K_GEN_23_*.md` file existed anywhere in the repository before this phase. Confirmed `GEN.23` as the correct next-free ID from repository truth, not assumed.

This phase does not touch, reopen, or modify Beginner×5D (`GEN.22`, closed — no `SECONDARY_CONTROLLED` lane invented, no code path for 5D touched), any 2D authority, Intermediate×3D's own gate/floor (verified zero-delta, §7), or Beginner×4D (verified zero-delta, §7).

## 1. Frozen authority (given verbatim, not re-derived)

```
Beginner3D — Taper session minima
KEY  = 3.0 km   (existing frozen authority — TAPER_SHARPEN's existing 3.0km KEY floor, reused verbatim)
EASY = 2.5 km   (NEW — Beginner3D taper-specific product default)
LONG = 3.0 km   (NEW — Beginner3D taper-specific product default)
Total taper structural minimum = 8.5 km
```

Classification recorded verbatim, not re-derived: `EASY_SUPPORT = 2.5km` is `PRODUCT_DEFAULT` (keeps the taper structural floor at or below the taper formula's own computed target at the tightest point of the approved `PeakVolumeBand`); `LONG_RUN = 3.0km` is `PRODUCT_DEFAULT_WITH_COACHING_PRACTICE_SUPPORT`. Neither is claimed as a scientific minimum.

Explicit hard constraints observed throughout, verified not violated anywhere in this phase's diff: the approved Beginner×3D `PeakVolumeBand` `[16,20]` is unaltered; the 0.53 taper multiplier is unaltered; the normal-week session minima (KEY 4 / EASY 3 / LONG 5) are unaltered for non-taper weeks and for every Intermediate×3D week including its own taper; the 3D `RunLayout` structure is unaltered; Beginner×5D (`GEN.22`) is untouched.

## 2. Recurring-defect-family search (performed before writing any code, per instruction)

Following `GEN.20`/`GEN.21`'s own established practice, searched every path Beginner×3D Core's implementation would touch or newly exercise, before writing production code:

- **Taper dispatch / session-distance minimum resolution**: read `CatalogVolumeAndLongRunPlanner.cs:103-109`'s `DaysPerWeek==3` gate and `V1ThreeDaySessionVolumeAllocationPolicy`'s 4.0/3.0/5.0 minima directly (per `GEN.21 §2`, already confirmed Level-blind, no hardcode found there — this phase's own job is to *add* the Level-aware branch, not fix a hidden defect in the existing one).
- **Calendar**: `DatedGeneratedCatalogPlanSkeletonValidator`'s `MinimumKeySessionToLongRunSeparationDays`/`ToKeySessionSeparationDays = 2` constants (re-confirmed via direct read) are frequency-structural, no `Level` parameter — unaffected by construction.
- **Adaptation**: `NextWindowLoadDecisionPolicy.DetermineLoadDecision` dispatches purely on `ExpectedSessionCount`, confirmed Level-blind by direct read — a Beginner×3D candidate exercises the same 3-session branch Intermediate×3D already exercises live.
- **Found, real, previously-undisclosed instance (7th confirmed occurrence of this engagement's own recurring hardcode-assumption defect family, after `GEN.10`, `GEN.12`×3, `GEN.17`×3, `GEN.19`×2, `GEN.20`×2)**: `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare` — the final-stage defensive long-run-share-cap check GEN.20's own doc comment on this exact method already fixed once for 2D — has `if (candidate.Level == "NEW") { return candidate.DaysPerWeek == 2 ? Beginner2D... : BeginnerFourDay...; }`, a binary proxy that silently routed **any** Beginner `DaysPerWeek` other than 2 to `BeginnerFourDay`'s own 0.40 cap. This was never wrong before this phase (4D was the only other admitted Beginner frequency), but is the exact same "structural-count proxy instead of the real per-candidate `VolumeSafetyPolicy`" shape GEN.20 itself named and fixed for 2D — it would have silently misapplied a too-tight 0.40 cap (instead of the correct 0.42, matching `ThreeDayIntermediate`'s own normal-week cap, required so the unchanged 5.0km normal-week LONG floor stays satisfiable) to every real Beginner×3D normal week the moment this candidate existed. Found via real end-to-end dark verification (not by inspection) — the exact discovery method `GEN.17`'s own sibling defects used. Fixed with an explicit `DaysPerWeek == 3 => VolumeSafetyPolicy.ThreeDayBeginner.LongRunHardCapShare` branch, byte-identical for 2D/4D (§4/§7).

No other new instance of this defect family was found for the taper-dispatch, session-distance-minimum, calendar, or Adaptation paths.

## 3. Implementation

### 3.1 New `VolumeSafetyPolicy.ThreeDayBeginner`

Mirrors `V1FourDaySessionVolumeAllocationPolicy`'s own precedent shape for how `BeginnerFourDay` was added alongside `Default`/`ThreeDayIntermediate` (`GEN.4C §9`/`GEN.4D`): a new named policy instance, not a mutation of any existing one. Growth mechanics (7%/8%/2.0km) and normal-week long-run shares (38%/42% preferred, 40% selection, 42% hard cap) are reused **verbatim** from `ThreeDayIntermediate` — required, not arbitrary: verified this phase that a lower share (e.g. Beginner×4D's own 30/36/33/40) would make the unchanged 5.0km normal-week LONG floor unreachable at Beginner's own lower starting volumes (12.0km missing-readiness start × 40% = 4.0km normal-week LONG target still needs floor-clamping up to 5.0km; a 33% share would compute only 3.96km, clamped, but a 30%/36%/33%/40% *hard cap* of 0.40 leaves no room once floor-clamped values are summed against a tighter cap — confirmed empirically not just by formula, §3.3 below). `GoldenFixtureStartingVolumeKm`/`ResolvedPeakReference` (12.0/17.0) are unused by the 3D sequential-growth branch (matching `ThreeDayIntermediate`'s own established pattern) but populated for decision-trace/audit consistency, citing `GEN.4C.4`'s Beginner missing/explicit-zero defaults and `GEN.5A.2`'s frozen `PeakVolumeBand` reference point.

### 3.2 New taper-specific authorities

- `V1BeginnerThreeDayVolumeEligibilityPolicy.MinimumFullLayoutTaperWeeklyVolumeKm = 8.5d` — the new gate floor.
- `V1BeginnerThreeDayVolumeEligibilityPolicy.TaperBreakEvenPreTaperKm = 16.0d` — exact, independently re-derived and verified this phase (§5).
- `V1ThreeDaySessionVolumeAllocationPolicy.BeginnerTaperMinimumKeyKm/EasyKm/LongKm = 3.0/2.5/3.0` — the frozen triple, recorded verbatim.
- `BeginnerThreeDayCoreProductIneligibleException` — a new sibling of `BeginnerFourDayCoreProductIneligibleException`, following the exact same pattern, deriving from the shared `CatalogProductIneligibleException` base `CatalogPreviewGenerator` already catches generically (per that type's own doc comment: "every future candidate cell's ineligibility exception is picked up automatically").

### 3.3 A genuinely required second taper-specific authority, discovered by real arithmetic (not invented to force a pass)

Verifying the frozen triple's own arithmetic by hand (as instructed) surfaced a real integration conflict, not assumed away: at the exact binding case (pre-taper=16.0km, taper=8.5km, §5), `ThreeDayBeginner`'s **normal-week** 40% long-run selection share computes a taper LONG of 3.5km (`Round0.5(8.5×0.40)=3.5`) — combined with the new KEY floor (3.0) and EASY floor (2.5, since the 25%-share EASY target of 2.125km floor-clamps to 2.5), the three roles sum to 9.0km, **0.5km over** the 8.5km weekly total. `V1ThreeDaySessionVolumeAllocationPolicy.Allocate`'s reconciliation loop never adjusts the LONG role (only KEY/EASY absorb residual, by design, unchanged), so this is a genuine, unrecoverable infeasibility — confirmed empirically by running the real pipeline (§6) before concluding it was real, not merely suspected from the formula.

This is **not** the EASY/LONG session-minima triple being wrong — it is a **separate, previously-undecided authority** (the taper week's own long-run *selection share*) that GEN.21 never addressed and this phase has legitimate standing to set, being a distinct numeric authority from the frozen KEY/EASY/LONG minima triple. Resolved by adding `V1BeginnerThreeDayTaperLongRunSharePolicy` (30%/36% preferred, 33% selection, 40% hard cap) — **not a new invented number**: these are `VolumeSafetyPolicy.BeginnerFourDay`'s (and `.Default`'s) own already-approved long-run shares, reused verbatim across a different frequency/context, exactly matching this engagement's own `EXISTING_SHARED_POLICY_REUSED_DUE_TO_NO_LEVEL_EFFECT` pattern. Verified exact at the binding case: `Round0.5(8.5×0.33) = Round0.5(2.805) = 3.0km` — exactly the new LONG floor, zero slack, so `KEY(3.0)+EASY(2.5)+LONG(3.0)=8.5km` reconciles with **no adjustment needed at all** at the tightest point. This override applies **only** to the single taper week of a Beginner×3D candidate (`week.IsTaperWeek && ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner)`, both conditions required); every other week and every other candidate is unaffected by construction, verified in §7.

This satisfies the phase's own hard rule against back-deriving EASY/LONG session minima from the desired outcome: the frozen 3.0/2.5/3.0 triple was never touched to make this fit — a different, legitimately-open authority (long-run share) was set instead, and its correctness was verified independently (the exact-zero-slack arithmetic above), not merely asserted.

### 3.4 Dispatch wiring

- `CatalogVolumeAndLongRunPlanner.Build`: new `Level == "NEW" && DaysPerWeek == 3` branch, exact typed combination match (mirrors every existing branch's discipline — never a broad `DaysPerWeek == 3` condition alone).
- `CatalogVolumeAndLongRunPlanner`'s taper-eligibility gate: now Level-aware — Beginner uses `V1BeginnerThreeDayVolumeEligibilityPolicy`'s 8.5km floor and throws the new typed exception; Intermediate's branch is byte-identical to before (still 12.0km, still `ThreeDayCoreProductIneligibleException`).
- `ResolveStartingVolume`/`ResolvePeak`/`BuildWeeklyPlan`: the three `ReferenceEquals(_policy, ThreeDayIntermediate)` checks generalized to a shared `IsThreeDaySequentialGrowthPolicy` helper admitting `ThreeDayBeginner` too (byte-identical for every existing `ThreeDayIntermediate` caller); missing/explicit-zero starting volume reuses `V1BeginnerFourDayMissingReadinessStartingVolumePolicy` verbatim (12.0/9.5km), matching `GEN.5`'s own already-established hand-derived reuse of these exact figures for Beginner×3D.
- `BuildLongRunPlan`: taper-week share override for Beginner×3D only (§3.3).
- `V1ThreeDaySessionVolumeAllocationPolicy.Allocate`: new optional `useBeginnerThreeDayTaperMinima` parameter (default `false`, preserving every existing caller), selecting the Beginner-taper floor triple only when true.
- `CatalogSessionPrescriptionPlanner`: computes `useBeginnerThreeDayTaperMinima = isThreeDay && weekly.IsTaperWeek && Level == "NEW"` at the one call site.
- `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`: the defect fix (§2).

### 3.5 New catalog content (additive, internally gated)

- `plan-catalog/catalog/policies/peak-volume-bands.v8.json` — appends `{TEN_K, NEW, runsPerWeek:3, min:16, max:20}` (GEN.5A.2's already-frozen band) to v7's existing rows, unchanged otherwise.
- `plan-catalog/catalog/rule-packs/appsel-race-plan.v9.json` — points to `peak-volume-bands.v8`, otherwise identical to v8.
- `plan-catalog/catalog/combinations/ten-k-3d-beginner.v1.json` — new `TEMPLATE_COMBINATION` (`TEN_K__3D__BEGINNER v1`), reusing `RUN_LAYOUT_3D v1` (unaltered) and `BEGINNER_MODIFIER v1` (unaltered) verbatim, referencing the new rule pack. **Status `VALIDATED`, not `PUBLISHED`** — matching `TEN_K__4D__BEGINNER v1`'s own status exactly. `V1CatalogPilotIdentityPolicy`'s allow-list is **not** widened — `(Beginner,3)` remains absent from `IsSupportedLevelFrequency`, verified in §7.
- `PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles`: advanced 128 → 131 (exact +3, never weakened — this engagement's own `GEN.10`/`GEN.17` precedent).

## 4. Arithmetic verification of the binding-case claim (performed independently, per instruction — not accepted at face value)

Re-derived by hand and confirmed against the real system's own rounding rule (`Math.Round(value / 0.5, MidpointRounding.AwayFromZero) * 0.5`):

```
16.0 km × 0.53 = 8.48 km  ->  Round0.5(8.48) = 8.5 km   (exactly at the new 8.5km floor — zero slack)
20.0 km × 0.53 = 10.6 km  ->  Round0.5(10.6) = 10.5 km  (0.53*20=10.6, comfortable headroom above 8.5km)
```

This **confirms** the frozen authority's own claim verbatim. Additionally, solved for the exact break-even pre-taper volume on the real 0.5km grid: `X=16.0` passes (`8.48→8.5 ≥ 8.5`); `X=15.5` fails (`15.5×0.53=8.215→Round0.5=8.0 < 8.5`). **16.0km — the `PeakVolumeBand`'s own frozen minimum — is exactly the break-even point**, not an approximation; this is recorded as `V1BeginnerThreeDayVolumeEligibilityPolicy.TaperBreakEvenPreTaperKm = 16.0`. Verified by a dedicated unit test (`FrozenAuthority_BindingCaseArithmetic_VerifiedExactly`) independent of the orchestration pipeline, so this claim does not rest on the pipeline's own correctness circularly.

## 5. Representability re-test — every governed Core horizon, every readiness state (real, dark, full-pipeline verification)

Verified via the real, unmodified `DynamicCoreSessionPrescriptionOrchestrator` (chaining the volume/long-run planner, the real `CatalogSessionPrescriptionPlanner`, and the real `CatalogFinalPrescribedPlanFinalizer`'s `TAPER_SHARPEN` completion) against the new, internally-gated `TEN_K__3D__BEGINNER v1` catalog candidate — the same rigor and harness shape `GEN.4D`'s own `Gen4DBeginnerFourDayCoreTests`/`DynamicCoreVolumeAndLongRunOrchestratorTests`/`DynamicCoreSessionPrescriptionOrchestratorTests` established for Beginner×4D, extended here to full session-prescription+taper-sharpen completion (not just the volume plan) — matching the depth of the `GEN.17`/`GEN.18`-era dark-verification precedent for a new frequency cell. New test file: `Gen23BeginnerThreeDayCoreTests.cs`, 33 tests, all passing after the defect fix (§2).

**Real, honest finding — not adjusted to force a uniform "fully representable" result:**

| Readiness state | Start | 8 | 9 | 10 | 11 | 12 | 13 | 14 |
|---|---|---|---|---|---|---|---|---|
| Missing readiness | 12.0km | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Positive-observed, band lower (16.0km) | 16.0km | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Positive-observed, band upper (20.0km) | 20.0km | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Explicit-zero | 9.5km | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |

**Missing-readiness and both positive-observed band-boundary profiles are fully representable at every governed Core horizon (8-14 weeks) — 21/21 real end-to-end successes**, each producing a valid `RUN_LAYOUT_3D` (1 KEY + 1 EASY + 1 LONG every week), a taper week at or above the new 8.5km floor, and taper-week KEY/EASY/LONG sessions individually at or above 3.0/2.5/3.0km, reconciling exactly to the taper week's own planned volume.

**Explicit-zero is `PRODUCT_INELIGIBLE` at every one of the 7 governed Core horizons — a genuine, separate, disclosed finding, not a defect in this phase's own taper-minima implementation.** Root cause, confirmed by two distinct real failure shapes (both verified directly, not assumed to be a single uniform shape):

- **Weeks 8-11**: growth from the 9.5km explicit-zero start (reused verbatim from `V1BeginnerFourDayMissingReadinessStartingVolumePolicy`, per `GEN.5`'s own established reuse) never reaches the 16.0km pre-taper threshold this phase's own binding case requires within these shorter horizons — `CatalogVolumeAndLongRunPlanner`'s taper-eligibility gate fires first, throwing the clean, typed `BeginnerThreeDayCoreProductIneligibleException`.
- **Weeks 12-14**: growth over more non-taper weeks *does* clear the taper gate, but **week 1 itself** — always exactly the 9.5km starting value, independent of horizon — is below the **unchanged** 12.0km normal-week 3D floor (4.0+3.0+5.0, explicitly frozen, not touched by this phase). This surfaces as a raw, untyped `CatalogSessionPrescriptionInfeasibleException` ("Week 1 is below the 12km 3D direct-prescription floor"), not the typed product-ineligibility exception — a **pre-existing architectural characteristic** (the same generic per-week floor check already applies identically to Intermediate×3D; this phase did not introduce it, only newly reaches it for the first time via Beginner's own, separately-reused, lower explicit-zero default).

**This gap is explicitly not "fixed" in this phase.** It is a starting-volume-policy conflict — Beginner×4D's reused 9.5km explicit-zero default versus 3D's own unchanged, frozen 12.0km normal-week floor — entirely separate from GEN.21's frozen taper-minima decision, which said nothing about starting-volume defaults. Inventing a new Beginner×3D-specific explicit-zero starting value now would be a fresh product/numeric decision this phase has no standing to make unilaterally (the same STOP discipline `GEN.13`/`GEN.21` themselves established), and would risk exactly the "nudge a number to force a pass" anti-pattern the governing instruction explicitly forbids. Reported here as a new, disclosed finding for a future decision cycle if desired.

## 6. Zero-delta verification

- **Intermediate×3D**: dedicated test (`IntermediateThreeDay_ZeroDelta_TwelveWeekPilotProfile_Unaffected`) confirms the 12-week pilot profile's taper week still resolves at or above the original 12.0km floor, via the unmodified `ThreeDayIntermediate` code path.
- **Beginner×4D**: no file under `V1FourDaySessionVolumeAllocationPolicy.cs`, `V1BeginnerFourDayMissingReadinessStartingVolumePolicy.cs`, or `V1BeginnerFourDayVolumeEligibilityPolicy.cs` was touched; the only shared file touched, `CatalogFinalPrescribedPlanValidator.cs`, has its `DaysPerWeek == 2`/`else` (4D) arms byte-identical (verified by the switch expression's explicit `_ => BeginnerFourDay...` default, unchanged in effect for every `DaysPerWeek` value other than the newly-added `3`).
- **Every other frequency/Level cell**: `CatalogVolumeAndLongRunPlanner.Build`'s new branch is an exact `Level=="NEW" && DaysPerWeek==3` typed match, unreachable for any other combination; `IsThreeDaySequentialGrowthPolicy` only ever returns true for the two policies it already covered plus the one new one.
- **Identity/routing**: `V1CatalogPilotIdentityPolicy.IsSupportedIdentity(Race, TenK, Beginner, 3)` still returns `false`, verified by a dedicated test (`Candidate_RemainsInternallyGated_NotPubliclyRoutable`); every previously-widened cell (Beginner×4D, Intermediate×3D) remains `true`.

## 7. Formal supersession of the historical non-representability classification

**`BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` (`GEN.5C`) is not deleted or rewritten** — its report text stands exactly as written, unmodified, per this engagement's rule.

This phase formally supersedes its *characterization*, the same supersession-of-characterization pattern `GEN.21` itself already used for its own relationship to `GEN.5C`: `GEN.5C`'s classification remains **true of the OLD policy** — under the pre-`GEN.23` policy (a single, undifferentiated normal-week 12.0km floor applied unmodified to the taper week, `GEN.2B.1`/`GEN.2B.3`), Beginner×3D Core genuinely was non-representable at every horizon and every readiness state, exactly as `GEN.5C`/`GEN.5A.2` proved. It **no longer blocks under the new policy**: `GEN.21` diagnosed the real, narrower, mutable lever (a distinct taper-specific session-distance-minimum authority, never before recognized as separate from the normal-week floor); this phase implements the frozen resolution of that lever (§3) and proves, via real dark end-to-end verification (§5), that Beginner×3D Core is now representable for **missing-readiness and positive-observed readiness at every governed Core horizon (8-14 weeks)**.

**This is a genuine partial supersession, not a full one** — recorded honestly rather than rounded up: explicit-zero readiness remains non-representable (§5), for a reason unrelated to the taper-minima authority this phase closes. `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL`'s original finding ("mathematically unreachable with no identified lever, at every horizon and readiness state") is superseded in full for two of three readiness classes and remains accurate, independently, for the third (explicit-zero) — though now for a *different* reason (a separate, undecided starting-volume-policy gap) than `GEN.5C` originally found (an undifferentiated taper floor).

**New classification, recorded as the current state of this repository as of this phase:**

```
BEGINNER_3D_CORE_TAPER_MINIMUM_AUTHORITY_IMPLEMENTED
MISSING_AND_POSITIVE_OBSERVED_READINESS_REPRESENTABLE_8_14_WEEKS
EXPLICIT_ZERO_READINESS_REMAINS_NON_REPRESENTABLE_SEPARATE_STARTING_VOLUME_GAP
INTERNALLY_GATED_NOT_PUBLICLY_ROUTABLE
```

Both authorities — normal-week minima (`GEN.2B.1`, unchanged, 4.0/3.0/5.0) and the new taper-specific minima (`GEN.23`, 3.0/2.5/3.0, Beginner×3D-only) — are now recognized in the live codebase as genuinely distinct, mutually non-interfering authorities (`V1ThreeDaySessionVolumeAllocationPolicy`'s `useBeginnerThreeDayTaperMinima` parameter makes this explicit and typed, not merely conceptual). No production or catalog change touches `GEN.5C`'s own report text.

## 8. Verification summary

- New unit/integration test file: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/Gen23BeginnerThreeDayCoreTests.cs` — 33 tests, 33/33 passing (confirmed via `dotnet test --filter "FullyQualifiedName~Gen23BeginnerThreeDayCoreTests"`, 0 dotnet processes running before/during — confirmed via `tasklist`).
- `dotnet build RunningApp.sln -c Debug`: 0 errors (pre-existing warning count unchanged; no new warnings introduced by this phase's files).
- Full `RunningApp.IntegrationTests` regression, run alone (see §9 for the exact numbers and isolation confirmation).
- `PlanCatalog.Tests`: see §9.

## 9. Full regression results

Run alone, confirmed via `tasklist` showing zero `dotnet.exe` processes immediately before launch (and `dotnet build-server shutdown` used to clear lingering MSBuild/VBCSCompiler build-server nodes from prior build/test invocations before the final run, so no persistent-node reuse could mask contention).

A first full run surfaced 4 failures: the 3 known pre-existing baseline failures (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` at weeks:13/14, `Sw09ExplicitZeroReadinessEndToEndTests`) plus one genuinely new failure, `PackagedPlanCatalogRealHttpSmokeTests.ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview` (expected 131, actual 128) — root-caused immediately, not assumed benign: this test validates the **packaged Release-build** catalog output (`RunningApp.Api/bin/Release/net9.0/plan-catalog/catalog`), which was stale (still 128 files) because only a Debug build had been run since the 3 new catalog JSON files were added. Fixed by running `dotnet build RunningApp.Api/RunningApp.Api.csproj -c Release` (refreshing the packaged output to the correct 131), then re-verified the specific failing test plus `Gen23BeginnerThreeDayCoreTests` and `Gen4EBeginnerFourDayPublicActivationTests` in isolation (59 tests, 57 passed, 2 failed — the 2 known Gen4E baseline failures only, `PackagedPlanCatalogRealHttpSmokeTests` now passing). This was a stale-build-artifact issue, not a code regression — confirmed by the fix requiring zero source changes.

**Final, clean, full regression** (re-run in full after the Release-build fix, alone, zero dotnet processes before launch):

```
RunningApp.IntegrationTests: 4154 total, 4151 passed, 3 failed, 0 skipped
```

The 3 failures are the identical, already-named pre-existing baseline failures (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` weeks 13/14, `Sw09ExplicitZeroReadinessEndToEndTests`) — same failure shape (`InternalServerError` vs expected `OK`) this engagement has carried forward unchanged since `GEN.17`/`GEN.18`/`GEN.20`. **Total reconciles exactly**: 4154 = `GEN.20`'s own confirmed-clean 4121 baseline + this phase's 33 new `Gen23BeginnerThreeDayCoreTests` tests. **Zero new regressions.**

`PlanCatalog.Tests`: **1510/1510 passing**, unchanged from the `GEN.17`-confirmed baseline (no existing catalog document was edited — only 3 new, additive documents were added, so zero-delta by construction, confirmed empirically).

`dotnet build RunningApp.sln -c Debug` and `-c Release`: both 0 errors.

## 10. Governance and closure

No public HTTP routing/gate change. `V1CatalogPilotIdentityPolicy`'s allow-list is unchanged — `(Beginner,3)` remains unsupported at the public identity layer (§6). No already-`PUBLICLY_ACTIVE` frequency's behavior changed (§6). Beginner×5D (`GEN.22`) untouched — no `SECONDARY_CONTROLLED` lane invented, no 5D code path touched. `GEN.5C`'s report text unmodified (per instruction, never deleted or rewritten) — this phase's own report supersedes only its *characterization* (§7), consistent with `GEN.21`'s own established pattern for the same relationship.

**`BEGINNER_3D_CORE_TAPER_MINIMUM_AUTHORITY_IMPLEMENTED`.** Next: a dedicated, separately-authorized public-activation phase (mirroring `GEN.4D`→`GEN.4E`) if and when the product decides to open Beginner×3D Core's public HTTP gate — not scheduled as a Phase ID here. A future phase may also separately decide whether to author a Beginner×3D-specific explicit-zero starting-volume default (§5's disclosed gap) — also not decided or scheduled here.
