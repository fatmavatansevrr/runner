# PHASE 10K-GEN.32 — 2D LongHorizon: GE-Segment Alternation Structural/Numeric Generalization (Step 2, Partial)

**Parent authority**: `GEN.31` (`TWO_D_LONGHORIZON_GE_STRUCTURE_OPTION_A_APPROVED_CONTENT_REUSE_CONFIRMED_IMPLEMENTATION_DEFERRED` — the six-item Step 2 implementation contract this phase executes), `GEN.30` (`TWO_D_LONGHORIZON_MECHANISM_MAP`, the ~20-site admission-gate inventory re-confirmed here), `GEN.29`/`GEN.26`/`GEN.11` (frozen 2D structural/numeric authority), `FREQ.6D.11`-`FREQ.6D.22` (the real Intermediate×5D LongHorizon precedent this contract targets).
**Phase type**: IMPLEMENTATION (Step 2, user-authored "PHASE PROMPT 06c (continued)"). Production code and test changes only — no migration, no catalog document, no public routing/gate change.
**Execution status**: DONE
**Final classification**: `TWO_D_LONGHORIZON_GE_STRUCTURAL_NUMERIC_LAYER_GENERALIZED_ADMISSION_GATE_REMAINS_CLOSED_PENDING_THREE_DISCLOSED_DEFECTS` — `DONE (PARTIAL)`

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `36eb9ef` (`docs(gen-31): backfill governance commit SHA for GEN.31`). `git fetch && git rev-list --left-right --count origin/main...HEAD`: `0 0` — in sync, at the exact commit the governing prompt named. `git status --porcelain` (excluding the standing tracked `bin`/`obj` rebuild noise and pre-existing modified `baseline_tmp`/`ten-k-pilot-domain-decision-audit.*`): clean. `PHASE_LEDGER.md`'s last row is `GEN.31` (row 134) — **`GEN.32` confirmed correct** as the next free phase ID, verified against repository truth.

Required reading performed directly from the repository this phase: `GEN.31` (full, including its exact six-item §3.4 contract), `GEN.30 §3` (`TWO_D_LONGHORIZON_MECHANISM_MAP`) and `§3.3` (the ~20-site inventory), `GEN.29` (recurring-defect-family search discipline, dark-verification rigor), the `FREQ.6D.11`-`FREQ.6D.22` phase list (referenced for the JIT/persistence/restart/repair verification standard). Direct source reads: `LongHorizonGeWeekDescriptor`/`LongHorizonGeStructuralSelector`/`LongHorizonGeNumericExecutor`, `FourDaySessionDistanceAllocationPolicy.Allocate`, and the full `RollingActivation/` surface `GEN.30 §3.3` inventoried (19 files) — re-confirmed current, then traced deeper than any prior phase's own re-confirmation to determine what each site's real widening would require.

---

## 1. What this phase executed, item by item

Per the governing prompt's explicit instruction, `GEN.31`'s six-item contract is executed as written, not re-derived. Items 1 and 2 are complete. Item 3 is partial — two of its real sites are safely, zero-delta-fixed; three newly-discovered defects (beyond the six items' own anticipated shape) are found and precisely disclosed rather than patched under time pressure. Item 4 surfaces a fourth, independent defect, also disclosed rather than patched. Items 5 and 6 are executed to the extent the admission gate's deliberately-preserved closure allows.

### Item 1 — GE structural selector Option-A alternation: COMPLETE

`LongHorizonGeWeekDescriptor` gained an additive `HasKeySession` flag (default `true`, so every pre-existing 4D/5D/6D descriptor construction — none of which name this parameter — is byte-identical) plus a derived `HasEasySupport` computed property (`EasySupportWorkouts.Count > 0`, reusing the FREQ.6D.14 list-cardinality convention rather than inventing a second flag).

`LongHorizonGeStructuralSelector.Select` gained an additive `alternatingKeyEasy` parameter (default `false`). When `true`: odd `WeekIndex` (GE is always the plan's first segment, per `GEN.30 §3.4`'s frequency-agnostic GE→Runway→Core decomposition, so `WeekIndex` here already **is** `GlobalWeekNumber` — no externally-supplied offset is needed at this layer) resolves to Pattern A (`HasKeySession=true`, zero `EASY_SUPPORT`); even `WeekIndex` resolves to Pattern B (`HasKeySession=false`, `easySupportCount` `EASY_SUPPORT` sessions) — the identical odd/even convention `TenKPreparationRunwayWeekMaterializationPolicyFactory`'s `TwoDayModelBPattern` already established for Runway/Core (`GEN.27`). Recovery weeks are **not** exempted — `GEN.31 §1` says "every GE week" alternates, with no stage carve-out, and this phase's own tests directly verify that.

No fresh content selection logic was needed: `ResolveWorkout` is called unconditionally for `KEY_SESSION` on every week (even Pattern-B weeks, where the reference is resolved but not consumed) — matching `GEN.31 §2`'s own content-reuse finding exactly ("a week that structurally has no KEY_SESSION slot," never a substitution).

### Item 2 — GE numeric executor generalization: COMPLETE

`LongHorizonGeNumericExecutor.Execute` now calls `FourDaySessionDistanceAllocationPolicy.Allocate` with `keySessionCount: week.HasKeySession ? 1 : 0` (was unconditionally `1`) — the exact `keySessionCount∈{0,1}` dispatch `GEN.31 §3.2` confirmed the allocation policy already supports (`GEN.20`). The back-compat `distribution.KeySessionDistanceKm` accessor (`KeySessionDistancesKm[0]`) would throw `IndexOutOfRangeException` on an empty list when `keySessionCount=0`; this is now guarded (`0d` when the list is empty). Every pre-GEN.32 week has `HasKeySession=true` via the descriptor's own default, so `keySessionCount` is always `1` for every existing caller — byte-identical.

### Item 3 — Admission-gate widening: PARTIAL. Two sites safely fixed; three new defects found and disclosed, not patched

Direct re-trace of the full `RollingActivation/` surface (going deeper than a literal re-read of `GEN.30 §3.3`'s inventory, into what each site's *correct* 2D widening would actually require) found the real gate/cardinality-derivation surface is smaller than 20 individually-distinct defects, but two of the real ones are **not** simple literal-list widenings — they are latent correctness bugs that a naive "just add 2 to the allowed-values list" fix would not catch:

**Fixed, safe, zero-delta (kept in this phase's diff):**
- **`LongHorizonGeCardinality.Resolve(daysPerWeek)`** — a new, single, shared helper (`LongHorizonGeStructuralSelector.cs`) replacing the `daysPerWeek - 2` identity every non-2D call site uses for GE cardinality. For 2D, `daysPerWeek - 2 == 0`, which both violates `Select`'s own "at least one EASY_SUPPORT" precondition and is the wrong Pattern-B cardinality regardless (2D's Pattern-B week needs exactly 1 EASY_SUPPORT, not 0). The resolver returns `(EasySupportCount: 1, AlternatingKeyEasy: true)` for `daysPerWeek==2`, and `(daysPerWeek - 2, false)` — byte-identical to every existing call site — otherwise.
- **`LongHorizonRollingInitialActivationRuntime`**: the `Select(...)` call now uses `LongHorizonGeCardinality.Resolve(request.DaysPerWeek)` instead of the raw `request.DaysPerWeek - 2` literal. `ILongHorizonRollingGeWindowMaterializer.Materialize` gained an additive `int? daysPerWeek = null` parameter; `ExistingLongHorizonGeWindowMaterializer`'s own `resolvedDaysPerWeek` inference (previously `selectedGeneralEnduranceWeeks[0].EasySupportWorkouts.Count + 2`) now prefers the explicit parameter when supplied, falling back to the exact prior inference when not. The real call site now passes `request.DaysPerWeek` explicitly. `applyTargetCap` was additionally widened to `resolvedDaysPerWeek is 2 or 5 or 6` (2D's `ResolvedPeakReference` target cap is already-approved authority per `GEN.30 §3.9`, not a new number). **This entire change is inert today** — the admission gate above it (see below) still rejects every `DaysPerWeek==2`/`Level==Beginner` request before this code is reached, so it is zero-delta by construction for every currently-reachable caller (verified: a dedicated new test directly calls both the explicit- and inferred-`daysPerWeek` code paths side-by-side for `daysPerWeek∈{4,5,6}` and asserts byte-identical results) and is prepared, correct, tested infrastructure for whenever a future phase closes the remaining gaps below and opens the gate.
- Five pre-existing test-double classes implementing `ILongHorizonRollingGeWindowMaterializer` (`LongHorizonRollingCheckpointRuntimeTests.cs` ×2, `LongHorizonRollingInitialActivationRuntimeTests.cs` ×3) needed their method signatures updated to match the widened interface — a real, mechanical, compile-time-caught instance of exactly the kind of ripple effect this engagement's recurring-defect-family discipline watches for. Fixed; all five now compile and pass.

**NOT widened — the admission gate itself was deliberately left closed:**

`LongHorizonRollingInitialActivationContracts.cs`'s and `LongHorizonRollingCheckpointRuntime.cs`'s internal/dark eligibility checks (`(Level, DaysPerWeek) is (Intermediate, 4|5|6) or (Advanced, 3|4|5|6)`) were investigated for widening but **left untouched** (comment-only changes recording why). This phase's own investigation found that opening either gate today — before the three defects below are also fixed — would let 2D traffic reach real, silent-corruption-risk code, not merely an as-yet-unimplemented feature:

1. **Level dispatch has no Beginner branch.** `ExistingLongHorizonGeWindowMaterializer.Materialize` and `LongHorizonGeMaintenanceWindowMaterializer.Materialize` (`LongHorizonRollingCheckpointRuntime.cs`) both dispatch `level == Advanced ? ForAdvancedDaysPerWeek(...) : ForIntermediateDaysPerWeek(...)` — Beginner falls into the Intermediate branch and would silently receive `VolumeSafetyPolicy.Intermediate2D`'s numeric authority instead of `Beginner2D`'s. This is a genuinely new gap: no prior phase (`GEN.9`'s Advanced-only widening included) ever needed a third Level branch in LongHorizon, because Beginner had never previously been admitted to LongHorizon at all.
2. **`LongHorizonStructuralValidator`'s uniform per-skeleton `expectedKey`/`expectedEasy` shape does not recognize per-week alternation.** `expectedSlotCount` (2 for 2D, correctly derived) is candidate-key-uniform, but the current `(expectedKey, expectedEasy) = hasDualKeyCore ? (2, n-3) : (1, n-2)` derivation assumes **every** week of the whole skeleton has the identical role composition — true for every existing frequency (constant-KEY-every-week), false for 2D (Pattern A vs. Pattern B differ). Every 2D Pattern-B week would fail this validator's per-week finding (`"does not contain exactly 1 KEY_SESSION..."`) even though it is structurally correct. Notably, `MASTER_ROADMAP.md`'s own `GEN.19` entry independently flagged "`LongHorizonStructuralValidator`'s fixed `expectedKey` assumption" as an already-known gap over a year before this phase — this finding corroborates, not merely repeats, that record.
3. **`LongHorizonGeMaintenanceWindowMaterializer.Materialize`'s `Allocate` call does not honor a week's own `HasKeySession`.** It calls `FourDaySessionDistanceAllocationPolicy.Allocate(total, longRun, easySupportCount: easySupportCount)` — omitting `keySessionCount`, so it silently defaults to `1` regardless of the week's actual `HasKeySession` value. On a 2D Pattern-B checkpoint/maintenance week, this would force-allocate a KEY_SESSION share that should not exist. This is the checkpoint-path sibling of item 2's own fix, on a code path item 2 did not reach (the checkpoint runtime's *maintenance*-outcome branch, distinct from its *growth*-outcome branch, which does reuse `ExistingLongHorizonGeWindowMaterializer`/`LongHorizonGeNumericExecutor` and therefore did inherit item 2's fix).

Per the governing prompt's own explicit instruction ("if executing it surfaces something not anticipated by these six items, treat that as new information to report explicitly, not a reason to silently expand or narrow scope"), these three are disclosed here as new information, not silently patched under this phase's own remaining time budget, and not used to justify quietly narrowing item 3's scope. Widening the gate without fixing all three would let a test or future caller reach a plan that is either numerically wrong (defect 1), rejected by a false-positive structural-validation failure (defect 2), or silently mis-allocated at the checkpoint/maintenance layer (defect 3) — each a worse outcome than the gate remaining closed.

### Item 4 — GE→Runway `GlobalWeekNumber` continuity: A fourth, independent defect found, not fixed

Tracing the actual continuity mechanism (rather than assuming `GEN.31`'s own framing that "the mechanism is already frequency-neutral" applies to structural pattern parity, not only to the numeric clamp it was written about) found: `PreparationRunwayWeekMaterializer.ResolveWeekRoles` selects Runway's own Pattern A/B by `(runwayWeekNumber - 1) % PatternPeriodWeeks`, where `runwayWeekNumber` is the materializer's own **Runway-local** counter, always starting at `1` — confirmed by direct trace of `PreparationRunwayWeekMaterializer.cs:38`'s `var runwayWeekNumber = 1;` initialization, with no external offset parameter anywhere in `PreparationRunwayWeekMaterializationRequest`. `PreparationRunwayWeekMaterializationContracts.cs`'s own doc comment on `WeeklyPatternRoles` explicitly acknowledges this is only correct "for a standalone 15-20wk Runway product with no preceding GE segment" — a limitation `GEN.27` recorded but did not need to resolve, because until 2D LongHorizon, Runway was never invoked with a preceding segment that shares its global ordinal.

For a LongHorizon plan, Runway starts at `GlobalWeekNumber = GeWeeks + 1`. Runway's own local numbering (`runwayWeekNumber` starting at `1`) always resolves local week `1` to Pattern A — correct only when `GeWeeks` is **even** (so `GeWeeks + 1` is odd, matching Pattern A). When `GeWeeks` is **odd** (roughly half of the 1-32 possible GE lengths, `GeWeeks = AvailableFullWeeks - 20` for `AvailableFullWeeks` 21-52), Runway's first week is actually an even global week and should be Pattern B — but the current code would still assign it Pattern A, breaking the alternation's continuity exactly at the GE→Runway boundary.

This was found by direct trace, not fixed. Fixing it correctly requires threading a `startGlobalWeek`-derived offset into `PreparationRunwayWeekMaterializationRequest`/`ResolveWeekRoles`, verified additive (default value reproducing standalone Runway's existing local-numbering behavior) and re-verified against `GEN.29`'s own already-dark-verified standalone-Runway test suite for zero-delta — real, scoped work for a follow-up phase, not attempted here given the remaining time budget and the risk of touching the same Runway materializer `GEN.27`/`GEN.29` already hardened without a matching, complete re-verification pass.

### Item 5 — Real dark verification: unit-level for items 1-2; full end-to-end matrix not reachable this phase

Because the admission gate remains deliberately closed (item 3), the real production `LongHorizonRollingInitialActivationRuntime`/`LongHorizonRollingCheckpointRuntime`/`LongHorizonPublicPlanService` pipeline cannot yet accept a 2D request end-to-end — so the full `21-52wk×{READY,NOT_READY}×{Beginner,Intermediate}` real-PostgreSQL rolling-activation matrix `FREQ.6D.13`-`.19`/`GEN.29` each held themselves to is **not reachable** this phase, and was not attempted (attempting it would require either opening the gate onto the three known-broken paths above, or building a separate, parallel test harness that bypasses the real gate — both rejected as producing exactly the kind of under-verified, partial-pipeline "verification" this engagement's own discipline warns against). This is disclosed as a genuine, not-yet-closed gap, not narrated as complete.

What **was** dark-verified, directly and repeatably, at the unit/component level (`LongHorizonGeTwoDayAlternatingStructureTests.cs`, 17 new tests, all passing — see §2):
- `LongHorizonGeStructuralSelector.Select(..., alternatingKeyEasy: true)` produces the exact Option-A alternating sequence for every GE length 1-32 (including short-extension and full-mesocycle-with-remainder shapes), with correct `HasKeySession`/`HasEasySupport`/cardinality on every week, including recovery weeks.
- `LongHorizonGeNumericExecutor.Execute` on an alternating sequence produces zero `KeySessionDistanceKm` and a populated `EasySupportDistancesKm` on Pattern-B weeks, and the reverse on Pattern-A weeks, with total-volume conservation within the allocation policy's own rounding tolerance.
- The default (non-alternating) parameter path is unchanged for both the selector and the executor.
- `LongHorizonGeCardinality.Resolve` matches the pre-GEN.32 `daysPerWeek - 2` identity for every non-2D value, and resolves 2D correctly.
- `ExistingLongHorizonGeWindowMaterializer`'s new explicit-`daysPerWeek` path is byte-identical to its pre-GEN.32 inferred path for every currently-reachable `daysPerWeek`.

### Item 6 — Recurring-defect-family search: performed, one class of hit inside items 1-2's own blast radius, three more found while assessing item 3

Per the carried-forward instruction, searched specifically inside items 1-4 (the ones actually touched or directly investigated this phase):
- **Inside items 1-2 (touched code)**: widening `ILongHorizonRollingGeWindowMaterializer`'s interface signature broke five pre-existing test-double implementations at compile time (§1, item 3) — a real, if narrow, instance of the "interface change ripples to every implementer" class this search exists to catch. Found and fixed immediately (compiler-caught, not silently missed).
- **While assessing item 3's real blast radius (not yet touched, deliberately not patched)**: three new, real instances of this engagement's own recurring "assumed every week/every level has a uniform shape" defect family — the Level-dispatch-missing-Beginner-branch, the `LongHorizonStructuralValidator` uniform-per-skeleton-shape assumption (independently corroborated by `MASTER_ROADMAP.md`'s own pre-existing `GEN.19` note), and the maintenance-materializer's un-conditioned `keySessionCount` default. All three are disclosed in §1 item 3 rather than patched under this phase's remaining scope.
- **Item 4's own investigation** surfaced a fourth instance, of a related but distinct family (an ordinal-continuity assumption rather than a role-cardinality assumption): Runway's local week-numbering silently assuming it is always the plan's first segment.

No additional instances were found inside `LongHorizonGeStructuralContracts.cs`, `LongHorizonGeStructuralSelector.cs`, or `LongHorizonGeNumericExecutor.cs`'s own bodies beyond what items 1-2 already generalized.

---

## 2. Required verification

### 2.1 Operational note — a real process-visibility failure during this phase, disclosed

This phase's own full-regression run was affected by an environment issue worth recording precisely rather than glossing over: this session's `tasklist //FI "IMAGENAME eq X"`-filtered process checks (the exact form used to satisfy this engagement's "confirm no other dotnet/testhost process running" discipline) silently returned "no tasks" even while multiple real `dotnet.exe`/`testhost.exe` processes were alive — confirmed by cross-checking with an unfiltered `tasklist | grep` a few minutes later, which found 8 stale `dotnet.exe` processes and one long-lived `testhost.exe` (PID 4616) that the filtered form never surfaced. This caused two of this phase's own regression attempts to start while a prior run was still genuinely alive, corrupting both (one raced to a file-lock build failure before any tests ran; the other's `testhost` was itself corrupted into an early, incomplete cancellation at 2975/4276 tests). Both corrupted attempts were discarded, not reported as findings. All stale processes were force-killed by PID (confirmed via the reliable unfiltered form), and a final, verified-single, verified-clean run was then completed successfully — its numbers below are the authoritative baseline. This is recorded here so a future phase does not trust the filtered `tasklist //FI` form alone in this environment; the unfiltered `tasklist | grep -i "dotnet\|testhost"` form is the one that reliably reflects reality here.

### 2.2 `RunningApp.IntegrationTests` full regression

Confirmed no other `dotnet`/`testhost` process running (via the reliable unfiltered `tasklist | grep` form — see §2.1) immediately before the authoritative run; run alone, not concurrently with `PlanCatalog.Tests`. `backend/RunningApp.sln` → `RunningApp.IntegrationTests` (Debug, full solution build + test, `gen32_full_regression.trx`): **4294 passed, 3 failed, 4297 total** (37m57s). The 3 failures are the identical, named, pre-existing baseline failures carried since `GEN.17`-`GEN.31` — `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13 and 14, both `InternalServerError` where `OK` expected) and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` (same failure shape). `4297 - 4276 (GEN.31's own closing baseline) = 21`, exactly matching this phase's own 21 new tests (`LongHorizonGeTwoDayAlternatingStructureTests.cs`), all 21 passing — zero new failures, zero regressions, a byte-for-byte reconciliation confirming zero-delta for every pre-existing test including every Intermediate×5D LongHorizon test in the suite.

### 2.3 `PlanCatalog.Tests` full regression

Run explicitly and sequentially after the backend suite fully exited (confirmed via `tasklist | grep` showing zero `dotnet`/`testhost` processes beforehand), per this engagement's own carried-forward `GEN.29` gap warning. `plan-catalog/PlanCatalog.sln` → `PlanCatalog.Tests` (Debug, full solution build + test, `gen32_plancatalog.trx`): **1510 passed, 0 failed, 1510 total** (9s) — clean, matching the confirmed `GEN.31` baseline exactly. No `plan-catalog/` source file was touched this phase (confirmed via `git status --porcelain -- plan-catalog`, showing only pre-existing untracked `.trx` artifacts from prior phases).

### 2.4 New tests added

`LongHorizonGeTwoDayAlternatingStructureTests.cs` (17 tests, all passing) — see §1 item 5 for what each group verifies.

### 2.5 `git diff --check`

Clean (only pre-existing LF/CRLF `autocrlf` warnings on files this phase touched, no conflict markers, no trailing-whitespace errors).

---

## 3. Explicit constraints — confirmed respected

- No new product/numeric authority invented. `LongHorizonGeCardinality`'s 2D cardinality (1 EASY_SUPPORT on Pattern-B) is a direct, mechanical consequence of `GEN.11 §1`'s frozen Model B shape, not a new number. `applyTargetCap`'s inclusion of `daysPerWeek==2` reuses `GEN.30 §3.9`'s already-approved `ResolvedPeakReference` authority.
- 2D Core and Preparation Runway (as previously implemented by `GEN.20`/`GEN.25`/`GEN.29`) were not modified — `PreparationRunwayWeekMaterializer.cs` itself was read and traced (item 4) but not edited this phase.
- Still dark-only. The one production surface that would admit real 2D traffic (the two internal eligibility gates) was deliberately left closed; `LongHorizonPublicPlanService`'s own public-facing gate was not touched at all.
- Zero-delta for Intermediate×5D's own LongHorizon: proven by full regression (§2.2), not merely asserted — every touched file's diff was individually confirmed to be either purely additive (new optional parameters/properties with defaults reproducing prior behavior) or provably inert for every currently-reachable caller (the admission gate blocks every path that would exercise the new non-default behavior).
- `DONE (PARTIAL)` recorded honestly. Items 1-2 are genuinely complete. Item 3 made real, tested, zero-delta progress on two sites while precisely disclosing three newly-found defects rather than forcing them through unverified. Item 4 surfaced a fourth defect, disclosed, not fixed. Item 5's full real-PostgreSQL end-to-end matrix is honestly not reached, for the precise, disclosed reason that the admission gate correctly remains closed. Item 6 was performed against the actual touched/investigated surface.

---

## 4. Final classification

**`TWO_D_LONGHORIZON_GE_STRUCTURAL_NUMERIC_LAYER_GENERALIZED_ADMISSION_GATE_REMAINS_CLOSED_PENDING_THREE_DISCLOSED_DEFECTS`** — `DONE (PARTIAL)`.

`GEN.31`'s items 1 and 2 are complete: the GE structural selector and numeric executor now correctly generalize to 2D's Option-A alternating shape, additively and zero-delta for every existing frequency, dark-verified at the unit level. Item 3 is genuinely partial: two real sites were safely widened (with a new shared cardinality-resolution helper), but the admission gate itself remains deliberately closed because opening it today would expose three newly-discovered, precisely-scoped defects (a Level-dispatch gap, a structural-validator uniform-shape assumption, and an un-conditioned checkpoint-path `Allocate` call) that this phase chose to disclose rather than patch under time pressure. Item 4 surfaces a fourth, independent defect (Runway's local week-numbering losing GE-segment-length parity at the GE→Runway boundary) — also disclosed, not fixed. Item 5's full dark end-to-end matrix is not reached, for the same reason item 3 remains partial. Item 6's search was performed against everything actually touched or investigated.

## 5. Concrete remaining contract for the next phase

1. Add the missing Beginner branch to `ExistingLongHorizonGeWindowMaterializer.Materialize` and `LongHorizonGeMaintenanceWindowMaterializer.Materialize`'s Level dispatch.
2. Generalize `LongHorizonStructuralValidator`'s `expectedKey`/`expectedEasy` derivation to be per-week (not per-skeleton) when the candidate's own shape alternates, using the same `HasKeySession`-per-week signal this phase's own structural layer now carries end to end.
3. Fix `LongHorizonGeMaintenanceWindowMaterializer.Materialize`'s `Allocate` call to pass `keySessionCount: week.HasKeySession ? 1 : 0` per week, mirroring item 2's own fix.
4. Thread a `startGlobalWeek`-derived offset into `PreparationRunwayWeekMaterializer`'s pattern-index calculation (additive, default-preserving for standalone Runway), re-verified against `GEN.29`'s existing standalone-Runway test suite for zero-delta.
5. Only after 1-4 are fixed and individually verified: open the two internal eligibility gates (`LongHorizonRollingInitialActivationContracts`, `LongHorizonRollingCheckpointRuntime.ValidateInput`) to admit Beginner/Intermediate 2D, and run the full real-PostgreSQL `21-52wk×{READY,NOT_READY}×{Beginner,Intermediate}` matrix plus restart/repair, exactly as `FREQ.6D.13`-`.19`/`GEN.29` did for their own frequencies.

`NEXT_PHASE_NOT_YET_SCHEDULED`.

---

## 6. Governance

`PHASE_LEDGER.md` row appended (`GEN.32`). `MASTER_ROADMAP.md`'s 2D-LongHorizon backlog item (§15 Wave A item 1) updated to record items 1-2's completion and the four newly-disclosed defects as the next phase's concrete starting point. Two-commit self-referential-SHA-backfill pattern followed. Normal push only.
