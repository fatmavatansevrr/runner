# PHASE 10K-FREQ.6D.14 — Intermediate×5D LongHorizon GE Implementation & Dark 21-52 Verification

**Type:** IMPLEMENTATION + DARK INTEGRATION VERIFICATION
**Parent phases:** FREQ.6D.12 (GE product/numeric decision), FREQ.6D.13 (rolling lineage/JIT dual-KEY implementation)
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. This report documents real production code changes and real regression results, and is explicit about what remains open.

---

## 1. Governance preflight

- HEAD at start: `67c1735` (Gate B push-gate pass, remote/local matched, 0/0 divergence).
- Scheduled `FREQ.6D.14` after confirming it was unreserved (no collision in `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, or report filenames) — commit `fd524cc`.
- `FREQ.6D.13` confirmed latest completed phase, Final Classification `INTERMEDIATE_5D_LONGHORIZON_ROLLING_LINEAGE_AND_JIT_DUAL_KEY_IMPLEMENTED_AND_VERIFIED_GE_IMPLEMENTATION_REMAINING`.
- `FREQ.6D.12` confirmed DONE, `INTERMEDIATE_5D_LONGHORIZON_GE_PRODUCT_AND_NUMERIC_POLICY_APPROVED` / `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTATION_READY`.

## 2. Scope discipline

This phase touched **only** FREQ.6D.13's disclosed remaining scope: GE 5D structural/numeric implementation and dark 21-52 verification. It did **not** reopen rolling-state schema, LaneOrdinal/SlotOrdinal architecture, JIT identity architecture, the applied migration, `PublishedBundleReleaseVersion` threading design, `ExecutionPrescriptionIndex` architecture, Core numeric authority, Preparation Runway product authority, GE product/numeric decisions, or the adaptation severity policy itself. No public LongHorizon 21+ activation was performed or authorized.

---

## 3. Current GE implementation seam (traced before editing)

- `LongHorizonGeStructuralContracts.cs`: `LongHorizonGeWeekRole` was a fixed 4-member enum (`KeySession`/`EasySupportA`/`EasySupportB`/`LongRun`); `LongHorizonGeWeekDescriptor.Roles` was an enum-keyed dictionary — structurally incapable of representing a 3rd EASY slot without a 5th enum member (explicitly prohibited by this phase's own discipline against special-casing).
- `LongHorizonGeStructuralSelector.cs`: hand-authored, catalog-mirroring stage-family role table; `BuildDescriptor` always constructed exactly 2 EASY entries.
- `LongHorizonGeNumericExecutor.cs`: `LongHorizonGeWeekNumericResult` had fixed `FirstEasySupportDistanceKm`/`SecondEasySupportDistanceKm` fields; `Execute` always used `VolumeSafetyPolicy.Default` inline (no policy parameter) and had no target-cap/plateau mechanism; missing/zero baseline threw a bare, untyped `InvalidOperationException`.
- `LongHorizonStructuralMaterializer.cs`: hardcoded `CandidateKey = "TEN_K__4D__INTERMEDIATE"`, `RUN_LAYOUT_4D`, `DaysPerWeek = 4` throughout — no day-count parameter existed at all.
- `LongHorizonFullNumericOrchestrator.cs`: hardcoded `V1CatalogPilotIdentityPolicy.CandidateKey` (always 4D) at its one candidate-load call site; built its own separate Core pipeline with no `ExecutionPrescriptionIndex`/published-bundle wiring at all (a gap distinct from, and not reached by, FREQ.6D.13's fix to the JIT rolling-activation path); called `TenKPreparationRunwayNumericPolicyFactory.Build()` (parameterless, always 4D-default shares) instead of the candidate-aware overload FREQ.6D.10 already added.
- `LongHorizonCalendarAssigner.AssignWeekdays`: hardcoded `preferredDays.Count != 4` and exactly two `EASY_SUPPORT_1`/`EASY_SUPPORT_2` keys.
- `LongHorizonStructuralValidator.cs` / `LongHorizonFullExecutionValidator.cs`: both independently hardcoded "exactly 4 slots per week" — two previously-undiscovered gates, found only by real dark execution, not by static audit.

---

## 4. Generalization approach (no `if daysPerWeek == 5` duplicate algorithm)

`LongHorizonGeWeekDescriptor.Roles` (enum-keyed dictionary) was replaced with three explicit fields: `KeySessionWorkout`, `EasySupportWorkouts: IReadOnlyList<LongHorizonGeWorkoutReference>`, `LongRunWorkout`. GE has exactly one KEY session in every approved shape (4D and 5D alike), so no lane-ordinal ambiguity exists for GE — only EASY cardinality varies, and it is now derived from `EasySupportWorkouts.Count`, threaded from a single `easySupportCount` parameter (default 2, preserving every 4D caller byte-for-byte) added to `LongHorizonGeStructuralSelector.Select`. `LongHorizonGeNumericExecutor.Execute` derives the allocation's EASY count from `week.EasySupportWorkouts.Count` and calls the already-generic `FourDaySessionDistanceAllocationPolicy.Allocate(..., easySupportCount: ...)` (generalized since FREQ.6D.7, unmodified this phase). `LongHorizonStructuralMaterializer.MaterializeAsync` and `LongHorizonFullNumericOrchestrator.ExecuteAsync` both gained a `daysPerWeek` parameter (default 4) that selects the 5D candidate identity, run layout, and GE/Runway EASY count only when explicitly 5; any value other than 4 or 5 fails closed. `LongHorizonCalendarAssigner.AssignWeekdays` gained a `daysPerWeek` parameter (default 4) and emits `EASY_SUPPORT_1..N` dynamically. Both structural validators derive their expected per-week slot/role counts from the schedule's own resolved `CandidateKey` instead of a literal.

---

## 5. GE numeric policy reuse (no new numeric authority)

`VolumeSafetyPolicy.FiveDayIntermediate` — already defined by `FREQ.6D.10` with exactly the FREQ.6D.12-approved values (`ResolvedPeakReference = 44.5`, `LongRunSelectionShare = LongRunPreferredMinimumShare = 0.28`, `LongRunPreferredMaximumShare = LongRunHardCapShare = 0.36`) — is reused verbatim as GE's own policy when `daysPerWeek == 5`, via a new `policy` parameter on `LongHorizonGeNumericExecutor.Execute` (default `VolumeSafetyPolicy.Default`, so every 4D caller's growth ratios and long-run shares are byte-identical to before). **No new numeric constant was introduced anywhere in this phase.**

## 6. Target cap / plateau

A new `applyTargetCap` parameter (default `false`) on `Execute` clamps each non-recovery week's total volume to `policy.ResolvedPeakReference.Value` (44.5 for the 5D policy) once reached — a simple `Math.Min`, not a new semantic: `GE current weekly target`, `ResolvedPeakReference`, and `ActualAchievedPeak` remain the three distinct concepts the phase's own discipline required keeping separate. Because the clamp reapplies every week, the plateau falls out naturally from the existing progression formula with no invented cycling/deload pattern. Default `false` preserves the pre-existing, still-uncapped 4D GE behavior exactly.

---

## 7. Missing/explicit-zero → typed PRODUCT_INELIGIBLE

Two new typed exceptions (`LongHorizonGeMissingReadinessProductIneligibleException` / `LongHorizonGeExplicitZeroReadinessProductIneligibleException`, both deriving from `InvalidOperationException` via a new `LongHorizonGeProductIneligibleException` base, mirroring the established `CatalogProductIneligibleException` convention) replace the prior bare, untyped throw in `LongHorizonGeNumericExecutor.Execute`. Missing (`null`) and explicit-zero (`<= 0`) are now distinguished, each carrying its own typed `Code`. This is the underlying fail-closed rule FREQ.6D.12 already approved (§16/§17/§39) — no new rule, only a typed shape distinguishing the two cases. Verified this only fires for LongHorizon-GE-consuming callers, never for Core-only 8-14 or Runway 15-20 (those routes' own `V1FiveDayIntermediateMissingReadinessStartingVolumePolicy`/existing FREQ.6D.10 authority is untouched).

---

## 8. Real gaps found and fixed (discovered only through actual dark execution, not static audit)

1. `LongHorizonFullNumericOrchestrator`'s own separately-built Core pipeline had no `ExecutionPrescriptionIndex`/published-bundle wiring at all — a distinct gap from the one FREQ.6D.13 fixed in the JIT rolling-activation path, since this orchestrator builds its own `TenKPreparationRunwayCoreGenerator` independently. Fixed by threading a real `PublishedTemplateBundleLoader` (release `1.1.0`, the same version FREQ.6D.13's own 5D dark tests verify against) into its construction.
2. `TenKPreparationRunwayNumericPolicyFactory.Build()` (parameterless) was called instead of the candidate-aware `Build(candidate)` overload FREQ.6D.10 added — meant Runway's own long-run-share validation always used the 4D 30%/36% range even for a 5D candidate, causing a real "29.41% outside approved range" failure. Fixed by passing `candidate`.
3. `LongHorizonStructuralValidator` and `LongHorizonFullExecutionValidator` both independently hardcoded "exactly 4 slots per week" — real Global-week validation failures that only surfaced once real 5D dark execution reached the final validation stage. Both generalized off the schedule's own resolved candidate identity.

## 9. Real, pre-existing, non-5D-specific gap found (not fixed, disclosed)

The Preparation Runway numeric materializer has no accepted rule for "entry evidence exceeds the independently-computed Core Week 1 boundary" outside Taper — a gap **already documented** by `LongHorizonFullNumericOrchestratorTests.cs`'s own pre-existing 4D tests (`LongerGeHorizons_FailClosed_ExistingRunwayNoNonTaperReductionRule`, testing 28/40/52 weeks with a 20km typical baseline). A diagnostic sweep this phase ran confirmed: (a) a starting evidence near the 44.5km target cap (`SustainedHighBaseline`, 40km) reaches every one of 21/24/28/32/40/52 weeks without hitting this gap; (b) 22 weeks specifically fails at every baseline tried (20 through 44km); (c) **22 weeks also fails identically on the completely unmodified 4D orchestrator** with its own pre-existing 20km baseline — confirmed via a direct repro, not merely asserted — proving this is a genuine, pre-existing, day-count-neutral gap, not a 5D regression or something this phase's GE generalization introduced. Fixing it would require a new Preparation Runway numeric continuity rule — explicitly out of this phase's scope (new numeric/architecture authority, reserved for a dedicated future decision).

---

## 10. Full 21-52 dark matrix result

35 new tests in `LongHorizonFullNumericOrchestratorFiveDayTests.cs`, all passing:

- **A.** GE structure exactly 1 KEY + 3 EASY + 1 LONG at 21/28/32/52 weeks.
- **B/C/D.** Positive readiness (low/representative/high) at 21/24 weeks; sustained-baseline positive readiness at 28/32/52 weeks; missing → typed `LONG_HORIZON_GE_MISSING_READINESS_PRODUCT_INELIGIBLE` at 21/24/32/52; explicit-zero → typed `LONG_HORIZON_GE_EXPLICIT_ZERO_READINESS_PRODUCT_INELIGIBLE` at 21/24/32/52.
- **F/G.** 52-week GE never exceeds 44.5km (all 32 GE weeks checked) and never approaches the previously-rejected ~70+km/week uncapped pattern.
- **H.** 28%/36% long-run share band verified at 21/32/52 weeks (non-recovery weeks).
- **O.** GE→Runway structural (1K+3E+1L both sides) and numeric continuity (within the existing 8%/2.5km cap) verified at 24 weeks.
- **P.** Runway→Core dual-KEY continuity verified at 24 weeks: 1 KEY (Runway) → 2 KEY (Core), both surviving independently with distinct assigned dates — exercising the real FREQ.6D.13 `BuildBoundedCoreSelection` fix end-to-end through this orchestrator for the first time.
- Repeated EASY slots remain independently addressable (distinct assigned dates) across every GE/Runway/Core week.
- **M.** Full 21/24/28/32/40/52 matrix: exact `TEN_K__5D__INTERMEDIATE` candidate identity, no 4D fallback, all three numeric-execution-complete flags true. 22 weeks explicitly, honestly excluded and separately tested to fail closed with the exact pre-existing message, both for 5D and (repro) for unmodified 4D.
- Determinism: repeated execution produces byte-identical results.
- Historical 4D zero-delta: the same orchestrator, called with its default `daysPerWeek` (omitted), still produces exactly 4-slot GE weeks and the 4D candidate identity.

## 11. Real PostgreSQL persistence (item N) — NOT completed, disclosed

The rolling-activation persistence path (`LongHorizonRollingInitialActivationRuntime`/`LongHorizonRollingCheckpointRuntime`/`LongHorizonRollingStateRepository`, the real Postgres-backed mechanism FREQ.6D.13 built lineage columns for) has its own, separate `DaysPerWeek != 4` input-validator gate and its own internal call to `LongHorizonStructuralMaterializer.MaterializeAsync` (currently invoked without the new `daysPerWeek` parameter, defaulting to 4). Relaxing this path correctly — gate, structural-materialization threading, and a real end-to-end Postgres persist/reload proof for a 5D GE window — is additional, distinct engineering and verification work not completed this phase. This phase's own dark verification (§10) proves GE 5D structural/numeric correctness at the `LongHorizonFullNumericOrchestrator` level (the same orchestrator LongHorizon's structural/numeric authority flows from); real Postgres persistence specifically for a 5D GE rolling window remains open.

## 12. Adaptation / repair through the persisted path — NOT independently verified

`LongHorizonRollingCheckpointRuntime`'s own GE-window materializer construction was updated only mechanically (to compile against `LongHorizonGeWeekNumericResult`'s new shape, §Errors) — its severity policy itself was not touched (per this phase's explicit instruction to reuse it unmodified) and was not exercised end-to-end for a real 5D window, since doing so requires the same rolling-activation gate relaxation described in §11.

---

## 13. Full regression

- New test file: `LongHorizonFullNumericOrchestratorFiveDayTests.cs` — 35/35 passing.
- Full LongHorizon suite (`FullyQualifiedName~LongHorizon`): **1194/1194** passing (1159 pre-existing + 35 new), zero regressions.
- Full `RunningApp.IntegrationTests`: **3826/3828** — only the same two pre-existing, already-documented, unrelated stale-date failures (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`), confirmed identical in signature to every prior phase's own occurrence. **Zero new regressions.**
- `PlanCatalog.Tests`: **1510/1510**, unaffected.
- Debug build: clean, 0 errors. Release build: clean, 0 errors.
- `git diff --check`: clean (only LF/CRLF conversion warnings, no conflicts).

## 14. Public routing / unsupported neighbors

Not touched this phase. `LongHorizonPublicPlanService.cs`'s own `DaysPerWeek != 4` public-routing gates remain unchanged — public Intermediate×5D LongHorizon 21+ remains closed, exactly as before. Beginner×5D, Advanced×5D, Intermediate×6D/7D LongHorizon were not touched and remain unsupported.

---

## 15. Files changed (production)

`LongHorizonGeStructuralContracts.cs`, `LongHorizonGeStructuralSelector.cs`, `LongHorizonGeNumericExecutor.cs`, `LongHorizonStructuralMaterializer.cs`, `LongHorizonFullNumericOrchestrator.cs`, `LongHorizonCalendarAssigner.cs`, `LongHorizonStructuralValidator.cs`, `LongHorizonFullExecutionValidator.cs`, `LongHorizonRollingCheckpointRuntime.cs` (mechanical compile-only fix).

## 16. Test files

`LongHorizonFullNumericOrchestratorFiveDayTests.cs` (new, 35 tests), `LongHorizonDarkExecutionOrchestratorTests.cs` (2 tests updated/added for the new typed exceptions).

## 17. Final classification

**`INTERMEDIATE_5D_LONGHORIZON_GE_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL`**

Execution Status: `DONE (PARTIAL)`. Full success boundary items A-M, O, P substantially achieved and dark-verified (with the 22-week horizon honestly excluded as a proven pre-existing, non-5D-specific gap, not a hole introduced by this phase). Item N (real PostgreSQL persistence for the 5D GE rolling-activation path specifically) and full adaptation/repair verification through that same persisted path were not reached — disclosed as open, not blocked on architecture, product, or numeric authority (no STOP condition fired; every new/reused number traces to FREQ.6D.12/FREQ.6C/FREQ.6D.10; no new catalog content; no second migration).

Not `INTERMEDIATE_5D_LONGHORIZON_GE_IMPLEMENTED_AND_DARK_VERIFIED` (unqualified) because item N is genuinely incomplete, and not `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED` (combined-scope wording) because the rolling-activation persistence path FREQ.6D.13 built lineage columns for has not yet been exercised for a real 5D GE window.

## 18. Next phase

The remaining work is narrow and well-scoped: relax `LongHorizonRollingInitialActivationInputValidator`'s/`LongHorizonRollingCheckpointRuntime`'s own `DaysPerWeek != 4` gates (the same class of fix FREQ.6D.13 made to `IsValidFourDayAvailability`), thread `daysPerWeek` into their own `LongHorizonStructuralMaterializer.MaterializeAsync` call sites, and add real-PostgreSQL persist/reload proof for a 5D GE rolling window — then real environment verification + public activation. Not yet scheduled as a Phase ID.
