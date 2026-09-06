# PHASE 10K-GEN.33 — 2D LongHorizon: Disclosed-Defect Closure and Structural-Materializer Gap Disclosure

**Parent authority**: `GEN.32` (`TWO_D_LONGHORIZON_GE_STRUCTURAL_NUMERIC_LAYER_GENERALIZED_ADMISSION_GATE_REMAINS_CLOSED_PENDING_THREE_DISCLOSED_DEFECTS`, `DONE (PARTIAL)`, whose §5 four-item remaining contract this phase executes), `GEN.31`/`GEN.30`/`GEN.29`/`GEN.26`/`GEN.11` (frozen 2D structural/numeric authority), `FREQ.6D.13`-`FREQ.6D.19` (the real dark-verification standard this phase's admission-gate decision is measured against).
**Phase type**: IMPLEMENTATION (user-authored "PHASE PROMPT 06c (continued II)"). Production code and test changes only — no migration, no catalog document, no public routing/gate change.
**Execution status**: DONE
**Final classification**: `TWO_D_LONGHORIZON_FOUR_DISCLOSED_DEFECTS_CLOSED_ADMISSION_GATE_REMAINS_CLOSED_PENDING_NEWLY_DISCLOSED_STRUCTURAL_MATERIALIZER_GAPS` — `DONE (PARTIAL)`

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `f627424` (`docs(gen-32): backfill governance commit SHA for GEN.32`). `git fetch && git rev-list --left-right --count origin/main...HEAD`: `0 0` — in sync, at the exact commit the governing prompt named. `git status --porcelain` (excluding the standing tracked `bin`/`obj` rebuild noise and pre-existing modified `baseline_tmp`/`ten-k-pilot-domain-decision-audit.*`): clean. `PHASE_LEDGER.md`'s last row is `GEN.32` (row 135) — **`GEN.33` confirmed correct** as the next free phase ID, verified against repository truth.

Required reading performed directly from the repository this phase: `GEN.32` (full), `GEN.31`/`GEN.30 §3`/`§3.3`, `GEN.29`, the `FREQ.6D.13`-`.19` phase list, and `MASTER_ROADMAP.md`'s `GEN.19` note (confirmed the same `LongHorizonStructuralValidator` uniform-`expectedKey` assumption GEN.32 independently found). Direct source reads (not GEN.32's own summary): `LongHorizonGeStructuralSelector.cs`, `LongHorizonGeNumericExecutor.cs`, `LongHorizonRollingInitialActivationRuntime.cs`, `LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonRollingInitialActivationContracts.cs`, `LongHorizonStructuralValidator.cs`, `LongHorizonStructuralMaterializer.cs`, `LongHorizonCompositionResolver.cs`, `PreparationRunwayWeekMaterializer.cs`/`PreparationRunwayWeekMaterializationContracts.cs`, `VolumeSafetyPolicy.cs`, `FourDaySessionDistanceAllocationPolicy.cs`.

---

## 1. Step 1 — GE segment length: odd length CONFIRMED genuinely reachable

Traced the real GE-length determination mechanism directly (not inferred from the defect's symptom): `LongHorizonCompositionResolver.Resolve` (the single authoritative composition resolver) sets, for the 21-52 week `LongHorizonGeneralEnduranceRunwayAndCore` path, `GeneralEnduranceWeeks = AvailableFullWeeks - 20`, with `PreparationRunwayWeeks` fixed at 8 and `CoreWeeks` fixed at 12 (`LongHorizonCompositionResolver.cs:129-144`, `TD-LONG-HORIZON-COMPOSITION-001`). `AvailableFullWeeks` itself comes from `CoreHorizonDecision.AvailableFullWeeks`, a calendar-derived integer count of full weeks between `StartDate` and `RaceDate`, constrained only to `21..52` (`LongHorizonCompositionResolver.MinimumLongHorizonFullWeeks`/`MaximumSupportedFullWeeks`) — no parity constraint of any kind exists anywhere in this derivation.

Consequently `GeneralEnduranceWeeks` ranges over every integer `1..32`, both parities: e.g. `AvailableFullWeeks=21` → `GeneralEnduranceWeeks=1` (odd), `AvailableFullWeeks=22` → `2` (even), `AvailableFullWeeks=23` → `3` (odd), etc. Odd `AvailableFullWeeks` values (`21,23,25,...,51` — 16 of the 32 possible horizons) all produce an odd GE length.

**Determination: GE length CAN genuinely be odd.** Defect 4 (GE→Runway `GlobalWeekNumber` continuity) is therefore a real, active, must-fix-before-gate-opens defect, not a defensive-only fix for a currently-impossible condition — given the same priority as defects 1-3, per the governing prompt's own instruction.

---

## 2. Step 2 — the four disclosed defects: all four fixed, with real tests

### Defect 1 — Level dispatch missing Beginner: FIXED, both sites

`ExistingLongHorizonGeWindowMaterializer.Materialize` (`LongHorizonRollingInitialActivationRuntime.cs`) and `LongHorizonGeMaintenanceWindowMaterializer.Materialize` (`LongHorizonRollingCheckpointRuntime.cs`) both widened their two-way `level == Advanced ? ... : ForIntermediateDaysPerWeek(...)` ternary to a three-way switch adding `RunningBackground.Beginner => VolumeSafetyPolicy.ForBeginnerDaysPerWeek(resolvedDaysPerWeek)` — reusing `GEN.12`'s already-approved `ForBeginnerDaysPerWeek`/`Beginner2D` authority verbatim, no new numeric constant. Byte-identical for every pre-GEN.33 caller (Advanced and Intermediate dispatch exactly as before).

Verified: `LongHorizonGen33TwoDayDefectFixesTests.ExistingLongHorizonGeWindowMaterializer_BeginnerLevel_UsesBeginnerPolicy_NotIntermediate` — a baseline/horizon combination (16km baseline, 8 GE weeks) chosen specifically to grow past Beginner2D's own 19.0km `ResolvedPeakReference` (confirmed the test's own premise, `Assert.Equal(19.0d, beginnerResult.Max(...))`, so the assertion has real teeth rather than passing vacuously) while remaining well below Intermediate2D's 25.0km — the pre-fix defect (silently applying Intermediate2D) would have produced materially different, wrongly-uncapped output; `LongHorizonGeMaintenanceWindowMaterializer_BeginnerLevel_UsesBeginnerPolicy_NotIntermediate` confirms the checkpoint-path branch is reachable and produces results. `ExistingLongHorizonGeWindowMaterializer_AdvancedAndIntermediate_StillReachTheirOwnPolicyFamily_ZeroDelta` confirms Advanced/Intermediate dispatch is unaffected at 4D/5D.

### Defect 2 — `LongHorizonStructuralValidator`'s uniform per-week role-count assumption: FIXED, and confirmed to close the same `GEN.19` reference

Confirmed by direct comparison that `GEN.32`'s finding and `MASTER_ROADMAP.md`'s pre-existing `GEN.19` note (`"...also found LongHorizonStructuralValidator's fixed expectedKey assumption"`) describe the identical validator code and the identical assumption — not merely a similarly-worded, distinct issue.

Making the fix non-vacuous required completing a gap the validator's own generalization depends on: `LongHorizonStructuralWeek` (the joined skeleton's own week record) never carried a per-week `HasKeySession` signal — only `LongHorizonGeWeekDescriptor` (one layer downstream) did, added by `GEN.32` item 1. Threaded it through additively:
- `LongHorizonStructuralWeek` gained a trailing `bool HasKeySession = true` field (default true, byte-identical for every existing Runway/Core/non-alternating-GE construction site, all of which use named-argument construction and never name this parameter).
- `LongHorizonStructuralMaterializer.BuildGeWeek` now only emits a `KEY_SESSION` slot when `ge.HasKeySession` is true, and passes `HasKeySession: ge.HasKeySession` through — previously it unconditionally added a `KEY_SESSION` slot to every GE week regardless of the descriptor's own resolved shape, a second, closely-related, previously-undisclosed defect (a genuinely alternating Pattern-B descriptor would have received a spurious extra slot). This path is dormant today (see §3 below — `LongHorizonStructuralMaterializer.MaterializeAsync` itself still rejects `daysPerWeek=2` before reaching `BuildGeWeek`), so this is zero-delta for every currently-reachable caller, and prepared, correct, tested infrastructure for whenever the gaps in §3 close.
- `LongHorizonStructuralValidator`'s `(expectedKey, expectedEasy)` derivation now reads `week.HasKeySession` per week (`hasDualKeyCore` Core case unchanged; otherwise `expectedKey = week.HasKeySession ? 1 : 0`, `expectedEasy = expectedSlotCount - 1 - expectedKey`) instead of a uniform per-skeleton `1`. Also added the approved 2D candidate identities (`V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey`/`TwoDayIntermediateCandidateKey`, already public for 2D Core) to `expectedSlotCount`'s dispatch (`=> 2`), so the validator is correct for 2D the moment a future phase wires a 2D skeleton through to it.

Verified: `LongHorizonStructuralValidator_GeWeek_UsesPerWeekHasKeySession_NotUniformAssumption` (a hand-built, otherwise-fully-valid synthetic 21-week 2D skeleton — GE=1/Runway=8/Core=12 — confirms a real Pattern-A week and a real Pattern-B week each validate cleanly against their own actual shape) and `LongHorizonStructuralValidator_GeWeek_HasKeySessionMismatchWithActualSlots_StillDetected` (confirms the fix is not vacuous: a week whose `HasKeySession=true` but whose actual slots disagree is still flagged).

**Both references closed**: the validator code (this phase) and `MASTER_ROADMAP.md`'s `GEN.19`-flagged status (§4 below records the closure; `GEN.19`'s own historical row is left unedited per this ledger's append-only convention — its own account of what it found at the time remains accurate history).

### Defect 3 — checkpoint-path `Allocate` call ignoring `HasKeySession`: FIXED, plus a same-family sibling found and fixed

`LongHorizonGeMaintenanceWindowMaterializer.Materialize`'s `FourDaySessionDistanceAllocationPolicy.Allocate` call now passes `keySessionCount: week.HasKeySession ? 1 : 0` and `easySupportCount: week.EasySupportWorkouts.Count` — both read per-week, replacing the prior unconditional `keySessionCount` default of `1` and a single `easySupportCount` value derived once from `selectedWeeks[0]` and applied uniformly across the whole window (a second, closely-related bug this phase found: for a real 2D alternating window, `selectedWeeks[0]`'s own `EasySupportWorkouts.Count` — 0 or 1 — is not necessarily correct for every other week in the same window either). Guarded the `KeySessionDistanceKm` back-compat accessor against the now-reachable empty-list case, mirroring `GEN.32`'s own item-2 guard. The interface (`ILongHorizonGeMaintenanceWindowMaterializer.Materialize`) gained an additive `int? daysPerWeek = null` parameter (mirroring the growth materializer's own `GEN.32` fix), since `selectedWeeks[0]`-based inference is unreliable for 2D; both real `LongHorizonRollingCheckpointRuntime` call sites (growth **and** maintenance) now pass `request.DaysPerWeek` explicitly — the growth-materializer call site had never received this fix even though `GEN.32` added the parameter to its interface, a recurring-defect-family instance found during this phase's own re-search (§4).

The checkpoint runtime's own GE-descriptor selection (`LongHorizonGeStructuralSelector.Select(...)`) was also still using the raw `request.DaysPerWeek - 2` literal, not `GEN.32`'s own `LongHorizonGeCardinality.Resolve` helper — fixed to match the initial-activation runtime's already-correct call site, so 2D's checkpoint-path continuation windows would also alternate correctly (previously would have hit `Select`'s own "≥1 EASY_SUPPORT" precondition at `DaysPerWeek-2==0`).

Verified: `LongHorizonGeMaintenanceWindowMaterializer_TwoDayAlternatingWeeks_RespectsPerWeekHasKeySession` (confirms Pattern-A weeks carry a non-zero `KeySessionDistanceKm`/empty `EasySupportDistancesKm`, Pattern-B weeks the reverse) and `LongHorizonGeMaintenanceWindowMaterializer_ExistingConstantKeyShape_IsUnaffected_ZeroDelta` (every pre-GEN.33, 4D-shaped call unaffected).

### Defect 4 — Runway's local week-numbering losing GE-segment-length parity: FIXED, additive, zero-delta for standalone Runway

`PreparationRunwayWeekMaterializationRequest<TKey>` gained a trailing `int StartGlobalWeek = 1` field (default `1`, reproducing standalone Runway's existing local-numbering behavior byte-for-byte). `PreparationRunwayWeekMaterializer.MaterializeAsync` now computes `globalWeekNumber = request.StartGlobalWeek + runwayWeekNumber - 1` and passes **that** (not the bare local `runwayWeekNumber`) into `ResolveWeekRoles`'s pattern-index calculation — `RunwayWeekNumber` itself (used for contiguity/ordinal identity elsewhere) is untouched. `LongHorizonStructuralMaterializer.MaterializeRunwayAsync` (the real LongHorizon call site) now threads `geWeeks` through and supplies `StartGlobalWeek: geWeeks + 1` (GE is always the plan's first segment, `GEN.30 §3.4`).

This defect, and its fix, are **entirely inert for every non-2D frequency by construction**: `ResolveWeekRoles`'s pattern branch (`layout.WeeklyPatternRoles`) is only ever non-null for a 2D layout (`GEN.27`); every other frequency's layout has no pattern and this new parameter is never consulted for it.

Verified: `PreparationRunwayWeekMaterializer_DefaultStartGlobalWeek_IsByteIdenticalToPreGen33` (omitting the parameter vs. explicitly passing `1` produce identical role sequences, against the real, unmodified materializer and real catalog block) and `PreparationRunwayWeekMaterializer_OddStartGlobalWeek_FlipsPatternParity_ClosingDefect4` (a simulated 3-week-odd-length GE segment preceding a 5-week Runway window, `StartGlobalWeek=4`: confirms local week 1 — global week 4, even — now correctly resolves to Pattern B, where the pre-fix code would have forced Pattern A regardless).

---

## 3. Step 3 — admission gate: deliberately NOT opened; newly-disclosed structural-materializer gaps found first

Per the governing prompt's own instruction to perform the recurring-defect-family search "specifically when a previously-closed/unreachable path becomes reachable," this phase traced the actual production path a real 2D LongHorizon request would need to reach numeric execution — one layer beneath everything `GEN.32` investigated (`LongHorizonRollingInitialActivationContracts`/`LongHorizonRollingCheckpointRuntime`'s own eligibility gates and the GE window materializers) — namely `LongHorizonStructuralMaterializer.MaterializeAsync`, the single joined full-skeleton builder both the initial-activation and checkpoint runtimes depend on. This trace found **the admission gate cannot safely be opened this phase**, because `MaterializeAsync` itself has real, previously-undisclosed defects, independent of the four `GEN.32` disclosed:

1. **`MaterializeAsync`'s own `daysPerWeek is not (3 or 4 or 5 or 6)` guard hard-rejects `daysPerWeek=2` unconditionally**, before any GE/Runway/Core logic runs. Opening the two `RollingActivation`-layer eligibility gates alone (this phase's four fixes notwithstanding) would still throw `InvalidOperationException` on every real 2D request the instant it reaches structural materialization.
2. **`ResolveCandidateKey`/`ResolveCandidateVersion` have no 2D case** — `(level, daysPerWeek)` switch falls through to the bare `_ => CandidateKey` default (`"TEN_K__4D__INTERMEDIATE"`), silently mislabeling a 2D skeleton with the 4D Intermediate identity, even though the correct identities (`V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey`/`TwoDayIntermediateCandidateKey`) already exist and are already public for 2D Core.
3. **The top-level GE-selector call site (`MaterializeAsync` line ~134) never passes `alternatingKeyEasy`** — it calls `LongHorizonGeStructuralSelector.Select(geWeeks, profile, easySupportCount)` with the raw `daysPerWeek - 2` cardinality, not `LongHorizonGeCardinality.Resolve`. Even with defects 1-4 fixed and CandidateKey corrected, the joined full skeleton this method produces would still never alternate — every GE week would resolve to the non-alternating (`alternatingKeyEasy=false`) default.
4. **`MaterializeCore`'s `runLayout`/`runLayoutSlotRoles` dispatch has no `daysPerWeek==2` case**, defaulting to `RUN_LAYOUT_4D`'s 4-slot shape for 2D Core weeks. The real, already-public 2D Core run layout (`RUN_LAYOUT_2D` v1, `plan-catalog/catalog/layouts/run-layout-2d.v1.json`) already exists with its own `patterns`/`WeeklyPatternRoles`-shaped structure, but `CatalogStageToWeekMaterializationContext.RunLayoutWeeklyPatternRoles`/`PatternPeriodWeeks` (the mechanism `DynamicCoreWeekSkeletonOrchestrator` already uses for the real, publicly-active 2D Core path) is never populated here. Correctly wiring this would also need Core's own pattern-index calculation to be `GlobalWeekNumber`-based (the same fix defect 4 applied to Runway), since Core is the plan's third, not first, segment.

Opening the admission gates today — even with items 1-4 fixed — would let a real 2D request reach `MaterializeAsync`, which would either throw immediately (gap 1) or, if that guard alone were naively patched, silently produce an incorrectly-identified (gap 2), non-alternating (gap 3), incorrectly-Core-shaped (gap 4) skeleton — exactly the "worse than remaining closed" outcome `GEN.32`'s own reasoning (repeated verbatim here) warns against. Per the governing prompt's own explicit permission ("`DONE (PARTIAL)` remains an acceptable outcome if Step 2 surfaces something that itself needs its own decision cycle — do not force the gate open"), **the admission gate is deliberately left closed this phase**, and the full real-PostgreSQL `21-52wk×{READY,NOT_READY}×{Beginner,Intermediate}` matrix is **not attempted**, for the same reason `GEN.32` itself declined it — attempting it would require either exercising known-broken paths or building a parallel bypass harness, both rejected as producing under-verified "verification."

No new product/numeric authority is implied by this disclosure: `RUN_LAYOUT_2D`, the 2D candidate identities, and 2D's GE alternation shape are all already-approved, already-existing authority (`GEN.11`/`GEN.26`/`GEN.29`/`GEN.31`) — the gap is purely that `LongHorizonStructuralMaterializer` itself was never wired to reach any of it.

---

## 4. Step 3 — recurring-defect-family search, performed at the now-more-reachable layer

Beyond the four §3 structural-materializer gaps above, this phase's search of the layer it actually touched (defects 1-4's own call sites) found and fixed one additional instance: the checkpoint runtime's **growth**-materializer call site had never received `GEN.32`'s own `daysPerWeek`-threading fix (only the maintenance-materializer call and the initial-activation runtime's growth call had) — a real, previously-undisclosed gap in the same "an interface gains an optional parameter but not every real call site is updated to pass it" family this engagement has hit before (`GEN.32 §1` item 3's own five-test-double ripple). Fixed alongside defect 3 (§2 above).

No additional instances were found inside `LongHorizonGeStructuralSelector.cs`, `LongHorizonGeNumericExecutor.cs`, or the four defect-fix sites themselves beyond what is already disclosed in §2/§3.

---

## 5. Required verification

### 5.1 Operational note — process-check method used

Per the operational note carried forward from `GEN.32`, every regression run in this phase was preceded by a plain, unfiltered `tasklist | grep -i "dotnet\|testhost\|vstest"` review (never the filtered `tasklist //FI` form `GEN.32` found unreliable), manually confirming no stale `testhost.exe`/`vstest.console.exe` process before starting, and never running the two suites concurrently.

### 5.2 Targeted new-test verification

`LongHorizonGen33TwoDayDefectFixesTests.cs` (12 new tests, all passing) — directly exercises each of the four defect fixes in isolation, per §2 above.

### 5.3 Targeted LongHorizon + PreparationRunway sweep

Before the full-suite run, `dotnet test --filter "FullyQualifiedName~LongHorizon | FullyQualifiedName~PreparationRunway"` (the entire test surface most likely to be touched by this phase's changes, real HTTP/PostgreSQL tests included): **1974 passed, 0 failed, 1974 total** (16m27s) — clean, confirming the four defect fixes and the additive `HasKeySession`/`StartGlobalWeek` contract changes are zero-delta across the full existing LongHorizon/Runway surface before committing to the full-suite run.

### 5.4 `RunningApp.IntegrationTests` full regression

Confirmed no `dotnet`/`testhost`/`vstest` process running via the reliable unfiltered `tasklist | grep -i "dotnet\|testhost\|vstest"` form (never the filtered `tasklist //FI` form `GEN.32` found unreliable) immediately before this run; run alone, not concurrently with `PlanCatalog.Tests`. `backend/RunningApp.sln` → `RunningApp.IntegrationTests` (Debug, full solution build + test, `gen33_full_regression.trx`): **4306 passed, 3 failed, 4309 total** (35m32s). The 3 failures are the identical, named, pre-existing baseline failures carried since `GEN.17`-`GEN.32` — `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13 and 14, both `InternalServerError` where `OK` expected) and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` (same failure shape). `4309 - 4297 (GEN.32's own closing baseline) = 12`, exactly matching this phase's own 12 new tests (`LongHorizonGen33TwoDayDefectFixesTests.cs`), all 12 passing — zero new failures, zero regressions, a byte-for-byte reconciliation confirming zero-delta for every pre-existing test including every Intermediate×5D LongHorizon test in the suite.

### 5.5 `PlanCatalog.Tests` full regression

Run explicitly and sequentially after the backend suite fully exited (confirmed via `tasklist | grep` showing zero `dotnet`/`testhost`/`vstest` processes beforehand). `plan-catalog/PlanCatalog.sln` → `PlanCatalog.Tests` (Debug, full solution build + test, `gen33_plancatalog.trx`): **1510 passed, 0 failed, 1510 total** (9s) — clean, matching the confirmed `GEN.32` baseline exactly. No `plan-catalog/` source file was touched this phase (confirmed via `git status --porcelain -- plan-catalog`, showing only pre-existing modified/untracked audit artifacts from prior phases).

### 5.6 `git diff --check`

Clean (only pre-existing LF/CRLF `autocrlf` warnings on files this phase touched, no conflict markers, no trailing-whitespace errors).

---

## 6. Explicit constraints — confirmed respected

- No new product/numeric authority invented — every fix reuses already-approved `GEN.11`/`GEN.12`/`GEN.26`/`GEN.29`/`GEN.31`/`GEN.32` authority (`Beginner2D`, `RUN_LAYOUT_2D`, the 2D candidate identities, the `HasKeySession`/alternation mechanism). The §3 disclosure names existing catalog/identity authority that already exists but is not yet wired — not a request for new authority.
- 2D Core and Preparation Runway (as previously implemented by `GEN.20`/`GEN.25`/`GEN.27`/`GEN.29`) are not reopened as a product/numeric matter. `PreparationRunwayWeekMaterializer.cs` **was** edited this phase (defect 4), but only via a purely additive, default-preserving parameter — re-verified zero-delta against the real, unmodified 2D dark-verification fixture (`Gen27TwoDayPreparationRunwayDarkVerificationTests`'s own request-builder pattern) and confirmed inert for every other frequency by construction (§2 defect 4).
- Still dark-only. `V1CatalogPilotIdentityPolicy.cs`'s public gate methods were not touched. The two internal/dark eligibility gates (`LongHorizonRollingInitialActivationContracts`, `LongHorizonRollingCheckpointRuntime.ValidateInput`) remain closed, per §3's disclosed reasoning.
- Zero-delta for Intermediate×5D's own LongHorizon: proven by full regression (§5.3), not merely asserted.
- `DONE (PARTIAL)` recorded honestly, per the governing prompt's own explicit permission not to force the gate open. All four originally-disclosed defects are genuinely, individually fixed and tested — this is real, verified progress, not a repeat of `GEN.32`'s own disclosure. The admission gate remains closed for a new, more precisely diagnosed reason than `GEN.32` left it.

---

## 7. Final classification

**`TWO_D_LONGHORIZON_FOUR_DISCLOSED_DEFECTS_CLOSED_ADMISSION_GATE_REMAINS_CLOSED_PENDING_NEWLY_DISCLOSED_STRUCTURAL_MATERIALIZER_GAPS`** — `DONE (PARTIAL)`.

Step 1 is definitively answered (GE length can genuinely be odd). All four of `GEN.32`'s disclosed defects are fixed, individually tested, and confirmed zero-delta for every non-2D caller. Attempting to open the admission gate surfaced four additional, previously-undisclosed defects in `LongHorizonStructuralMaterializer` (the full-skeleton builder beneath the layer `GEN.32` investigated) that make opening the gate today unsafe in exactly the same sense `GEN.32`'s own three defects did — disclosed precisely, not patched under this phase's own time budget, and not used to force a "fully complete" result.

## 8. Concrete remaining contract for the next phase

1. Widen `LongHorizonStructuralMaterializer.MaterializeAsync`'s `daysPerWeek is not (3 or 4 or 5 or 6)` guard to admit `2`.
2. Add a 2D case to `ResolveCandidateKey`/`ResolveCandidateVersion`, dispatching to `V1CatalogPilotIdentityPolicy.TwoDayBeginnerCandidateKey`/`TwoDayIntermediateCandidateKey` by `(level, daysPerWeek)`.
3. Wire `LongHorizonGeCardinality.Resolve(daysPerWeek)` into `MaterializeAsync`'s own GE-selector call site (currently raw `daysPerWeek - 2`, `alternatingKeyEasy` never passed).
4. Wire `RUN_LAYOUT_2D`'s `WeeklyPatternRoles`/`PatternPeriodWeeks` into `MaterializeCore`'s `CatalogStageToWeekMaterializationContext`, and extend Core's own pattern-index calculation to be `GlobalWeekNumber`-based (Core is the plan's third segment; needs the same offset treatment defect 4 gave Runway, using `geWeeks + 8` as Core's own `StartGlobalWeek - 1`).
5. Only after 1-4 are fixed and individually verified: open the two internal eligibility gates and run the full real-PostgreSQL `21-52wk×{READY,NOT_READY}×{Beginner,Intermediate}` matrix plus restart/repair, exactly as `FREQ.6D.13`-`.19`/`GEN.29` did for their own frequencies, including the odd-GE-length case confirmed real by this phase's own Step 1.

`NEXT_PHASE_NOT_YET_SCHEDULED`.

---

## 9. Governance

`PHASE_LEDGER.md` row appended (`GEN.33`). `MASTER_ROADMAP.md`'s 2D-LongHorizon backlog item (§15 Wave A item 1) updated to record the four defects' closure and the newly-disclosed structural-materializer gaps as the next phase's concrete starting point. Two-commit self-referential-SHA-backfill pattern followed. Normal push only.
