# PHASE 10K-GEN.37 — 2D LongHorizon: Core Pace-Anchor Decision Execution and Full GE→Runway→Core Chain Completion

**Parent authority**: a direct user decision message, "DECISION ON GEN.36" (verbatim spec, reproduced in full in this phase's own task brief), resolving `GEN.36`'s disclosed `DOMAIN_DECISION_REQUIRED` pace-anchor conflict. `GEN.36` (immediate predecessor, `DONE (PARTIAL)`) built the additive `CoreStartGlobalWeek` continuation-offset plumbing end to end through Core's real generator, but found that activating it at the one real intended call site tripped a pre-existing hard requirement in `PreparationRunwayCoreWeekOnePaceAdapter` that Core's own Foundation Week 1 carry at least one `KEY_SESSION` — genuinely unreachable before `GEN.36` because Core's local week 1 was always, unconditionally, Pattern A. `GEN.29` (the sibling *volume* adapter's own prior generalization to tolerate a zero-`KEY_SESSION` week, the precedent this phase's pace fix follows an analogous-but-not-identical pattern from).
**Phase type**: IMPLEMENTATION (anchor-selection fix, activation, recurring-defect-family fix) + VERIFICATION (full real-PostgreSQL GE→Runway→Core chain, both levels, both GE-parity cases).
**Execution status**: DONE
**Final classification**: `TWO_D_LONGHORIZON_FULL_GE_RUNWAY_CORE_CHAIN_COMPLETE_GLOBALWEEKNUMBER_PARITY_STRICT_ACROSS_ENTIRE_PLAN` — **`DONE`** (full completion, not partial).

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `0ea586f` (`docs(gen-36): backfill governance commit SHA for GEN.36`). `git fetch && git diff HEAD origin/main`: empty — in sync. `git status --porcelain` (excluding standing tracked `bin`/`obj` rebuild noise and pre-existing modified `baseline_tmp`/`ten-k-pilot-domain-decision-audit.*`/untracked `.trx` files from prior sessions): clean of unexpected changes. `PHASE_LEDGER.md`'s last row is `GEN.36` (seq 139) — **`GEN.37` confirmed correct** as the next free phase ID.

Required reading performed directly from the repository this phase: `PHASE_10K_GEN_36_...md` (full, including §3 the exact conflict, §4 the zero-delta proof method, §9 the concrete remaining contract), `PreparationRunwayCoreWeekOnePaceAdapter.cs`, `CatalogStageToWeekMaterializer.cs`'s `ResolveWeekRoles`, `LongHorizonRollingJitCompositionOrchestrator.cs`, `TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests.cs`, `PHASE_10K_GEN_29_...md`'s `PreparationRunwayCoreWeekOneTargetAdapter` precedent, `LongHorizonGen35TwoDayJitBoundaryTests.cs`.

---

## 1. The frozen decision, as implemented

Per "DECISION ON GEN.36": `PreparationRunwayCoreWeekOnePaceAdapter`'s anchor point becomes the first Core week (within local weeks 1-2) that carries a `KEY_SESSION`. If local week 1 is Pattern A (the only case that existed before this arc), anchor to week 1 exactly as before. If local week 1 is Pattern B, anchor to week 2 instead. No new numeric/pace authority — only which week supplies the existing goal-pace data changes.

### 1.1 `PreparationRunwayCoreWeekOnePaceAdapter.cs` — the anchor-selection fix

`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayPacePrescription/PreparationRunwayCoreWeekOnePaceAdapter.cs`:

```csharp
var weeksOrdered = corePlan.Weeks.OrderBy(w => w.WeekNumber).ToArray();
var localWeekOne = weeksOrdered.Length > 0 ? weeksOrdered[0] : null;
var localWeekOneKeyCount = localWeekOne?.Sessions.Count(s => s.StructuralRole == "KEY_SESSION") ?? 0;

var first = localWeekOneKeyCount >= 1
    ? localWeekOne
    : (weeksOrdered.Length > 1 ? weeksOrdered[1] : null);
```

Everything downstream of `first` (the `keyCount<1`/`longCount!=1`/`easyCount` shape validation, the pace-slot extraction, the returned `PreparationRunwayCoreWeekOnePaceTarget`) is byte-identical to `GEN.36`'s own code — only the *selection* of which week feeds it changed. `SourceProvenance` now records the anchor week number and, when it is not local week 1, an explicit note that the anchor moved per this decision — a traceability improvement, not a behavior change to the returned pace data itself.

### 1.2 Re-enabled the reverted line in `LongHorizonRollingJitCompositionOrchestrator.cs`

`CoreStartGlobalWeek: coreSegment.StartGlobalWeek` is now supplied at the real JIT-composition call site, mirroring `runwayStartGlobalWeek`'s own convention exactly. The doc comment explaining `GEN.36`'s revert was replaced with one explaining why `GEN.37` can now safely enable it.

---

## 2. Zero-delta proof for the week-1-Pattern-A case

Every pre-`GEN.37` real Core caller (`CatalogPreviewGenerator`'s standalone 2D/3D/4D/5D/6D public HTTP path, `LongHorizonFullNumericOrchestrator`'s 4D/5D call site, and every even-`GeneralEnduranceWeeks` 2D LongHorizon plan, whose `CoreStartGlobalWeek` resolves odd → Pattern A) has local week 1 already carrying a `KEY_SESSION`. For every such case, `localWeekOneKeyCount >= 1` is true, so `first = localWeekOne` — the exact same week `GEN.36`'s own code selected — and every downstream computation (shape validation, slot extraction, returned target) is unchanged.

Executable proof, not merely narrative:

- **`CoreStartGlobalWeekDefault_SameRequestShape_StillSucceeds_ConfirmingZeroDeltaAtDefault`** (`TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests.cs`) — the identical 2D Beginner Runway/Core request shape at the default `CoreStartGlobalWeek=1` (local week 1 = Pattern A) succeeds, unchanged.
- **`Gen35TwoDayJitBoundaryTests`'s `totalWeeks=22` scenarios (both Levels)** — `geWeeks=2` (even) makes `CoreStartGlobalWeek = geWeeks+8+1 = 11` (odd) → Core's local week 1 lands on Pattern A. These scenarios pass identically to `GEN.35`/`GEN.36`'s own baseline runs.
- **Full regression** (§6 below): every one of the ~2000 pre-existing LongHorizon/PreparationRunway tests that exercises this adapter (all reachable only via local-week-1-Pattern-A shapes, since `GEN.37` is the first phase in which any caller can reach Pattern B) passes unmodified.

This is the same class of proof `GEN.36 §4` used: not "the new code defaults safely" but "every real and test call site that predates this phase resolves through the identical branch this phase added, producing byte-identical output."

---

## 3. Recurring-defect-family search — the volume/numeric adapter had the same defect

Per the decision's explicit constraint ("if any other component turns out to have the same assumption, treat each as its own instance... verify and fix them the same way"), attempting the real activation immediately surfaced a **second, distinct instance** of the identical assumption in `PreparationRunwayCoreWeekOneTargetAdapter` (the sibling *volume* adapter, `GEN.29`'s own prior generalization target) — its `firstPrescribedWeek` lookup still unconditionally took local week 1, and `PreparationRunwayNumericMaterializer.ValidateRequest`'s own downstream `KeySession < 1` floor (a *different* check than the pace adapter's) failed identically for the Pattern-B case: `NumericMaterialization/NumericMaterializationFailed: A complete approved Core Week 1 target (>=1 KEY, exactly 1 LONG_RUN) is required.`

Fixed the same way: `PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayNumericMaterialization/PreparationRunwayCoreWeekOneTargetAdapter.cs`) now resolves a single `anchorWeekNumber` (local week 1 if it carries a `KEY_SESSION`, else local week 2) and sources **all three** of `weekly` (weekly volume target), `longRun` (long-run distance target), and `firstPrescribedWeek` (session-role counts) from that same anchor week — internal self-consistency, not three independently-anchored values. Zero-delta for every pre-`GEN.37` case (anchor always resolves to local week 1, identical to before).

Checked individually, not generalized silently, per the explicit constraint:

| Component | Checked | Finding |
|---|---|---|
| `PreparationRunwayCoreWeekOnePaceAdapter` | Yes — the named conflict | Fixed (§1.1) |
| `PreparationRunwayCoreWeekOneTargetAdapter` (volume) | Yes — found via real activation attempt | Same defect family; fixed (this section) |
| `PreparationRunwayCalendarComposer` (`firstCore`/`firstCoreWindowStart` lookups, 3 sites) | Yes | Date/`PhaseKey`-only checks (`StartDate`, `StageKey != "FOUNDATION"`) — no `KEY_SESSION` dependency, no defect |
| `TenKPreparationRunwayFinalInvariantValidator` | Yes | Only a `PhaseKey == "FOUNDATION"` check on the first dated Core week — no `KEY_SESSION` dependency, no defect |
| `LongHorizonStructuralValidator` | Yes | Already per-week `expectedKey`/`expectedEasy` derivation from each week's own `GlobalWeekNumber` parity (`GEN.33`'s own fix) — not week-1-specific, no defect |
| `LongHorizonCalendarAssigner` | Yes | Operates per-week on whatever roles a week's own slots contain; no week-1-specific `KEY_SESSION` assumption |
| `CatalogVolumeAndLongRunPlanner`, `CatalogPublicPreviewMaterializer`, `PreparationRunwayPersistablePlanMapper` (`week.Sessions.FirstOrDefault(KEY_SESSION)?.ProgressionStageKey ?? week.PhaseKey`) | Yes | Per-week, already null-coalesces to `week.PhaseKey` when a week has no `KEY_SESSION` — correctly handles the zero-`KEY_SESSION` case by design, no defect |
| `DatedGeneratedCatalogPlanSkeletonValidator`, `CatalogWeekSkeletonCalendarMaterializer`, `WorkoutExposureVerifier` | Yes | Operate on `KEY_SESSION` presence/absence per-week generically (counting, calendar-day assignment) — not an assumption that week 1 specifically has one | 

No third instance was found. This search was performed per-component, individually, as the decision required — not a blanket "same fix everywhere" pass.

---

## 4. Activation and full-chain re-verification

With both fixes in place, re-ran the targeted suite:

- `TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests` — **replaced** the now-obsolete "still-blocked-conflict" test (per its own doc comment, expected to start failing once resolved) with `CoreStartGlobalWeekEven_CausesCoreWeekOnePatternB_AnchorsPaceToWeekTwo_Succeeds`, asserting the new, decided, working behavior: the identical even-`CoreStartGlobalWeek=10` request that previously failed at `FinalInvariantValidation` now **succeeds**, with local week 1 carrying zero `KEY_SESSION` and local week 2 carrying exactly one (both asserted directly against the real `FinalPrescribedPlan`). The starting-volume/longest-run test inputs were lowered from `GEN.36`'s own (`weekly:12, longest:5`) to (`weekly:8, longest:3`) — anchoring to local week 2 targets a marginally higher numeric value than local week 1 would have (Foundation volume progresses week over week), so the same starting point `GEN.36` used now needs a lower starting point to stay within `PreparationRunwayNumericPolicy`'s approved per-week change limits across Runway's fixed 8-week ramp; mirrors `Gen35TwoDayJitBoundaryFixture`'s own documented reason for choosing its low baseline. Not a new numeric authority, a test-input recalibration.
- `CoreStartGlobalWeekDefault_SameRequestShape_StillSucceeds_ConfirmingZeroDeltaAtDefault` — unchanged, still passes.
- `Gen35TwoDayJitBoundaryTests.GeToRunwayToCore_RealJitBoundary_SucceedsAgainstRealPostgres_ThroughPublished140Bundle` — all 4 scenarios (both Levels × `totalWeeks∈{21,22}`, i.e. both GE-parity cases) pass against real PostgreSQL, through the real production chain (`LongHorizonRollingCheckpointRuntime` → `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync` → `LongHorizonRollingJitCompositionOrchestrator`, the identical real call chain `GEN.35` proved reaches Core organically).

Targeted run: **6/6 passing** (2 domain-conflict + 4 chain-boundary scenarios).

### 4.1 `Gen35TwoDayJitBoundaryTests`'s final assertion — extended to strict parity across the ENTIRE plan

Replaced the test's two separate assertion blocks (GE+Runway strict parity up to `runwayEnd`, then a *separate*, weaker block documenting Core's own disclosed local-anchor-only alternation past `runwayEnd`) with a single merged assertion: every activated week from `1` to `coreWindow.EndGlobalWeek` — spanning GE, Runway, **and** Core — alternates strictly by `GlobalWeekNumber` parity (`expectedKey = week.Key % 2 == 1`), with no double-up, skip, or local-anchor reset at either segment boundary. This is the concrete, real-PostgreSQL confirmation that `CoreStartGlobalWeek`'s activation (§1.2) produces content that agrees with `BuildBoundedCoreSelection`'s own `GlobalWeekNumber` stamping (which already used `coreSegment.StartGlobalWeek` since before this phase) — both consumers of "where is Core's week 1 in the global sequence" now agree by construction, verified against real persisted session rows, not just in-memory computation.

---

## 5. Full GE→Runway→Core chain — now complete, both levels, both GE-parity cases, real PostgreSQL

Unlike `GEN.35`/`GEN.36`, which could only prove GE+Runway strict parity while leaving Core's own anchor disclosed as not-yet-continuous, this phase's `Gen35TwoDayJitBoundaryTests` re-run proves the **entire** activated plan — GE, Runway, and Core — carries strict `GlobalWeekNumber`-parity alternation, for both Levels (Beginner, Intermediate) and both GE-parity cases (`totalWeeks=21` odd-`geWeeks=1`, `totalWeeks=22` even-`geWeeks=2`), driven through the real production rolling-activation/JIT-composition path against real PostgreSQL, verified from a fresh, independent restart reload (never the in-memory graph the driving calls used).

This is the concrete milestone `GEN.33`-`GEN.36` built toward: 2D LongHorizon's GE→Runway→Core chain is now content-continuous at the structural-role/pattern level across its full length, not merely internally consistent within each segment.

---

## 6. Required verification

### 6.1 Operational note — process-check method used

Every regression run in this phase was preceded by an unfiltered `tasklist | grep -i "testhost\|vstest"` review (never the filtered `tasklist //FI` form found unreliable in `GEN.32`), confirming no stale process before starting; the two suites were never run concurrently — `RunningApp.IntegrationTests`' full run was launched, confirmed via `tasklist` that no `testhost.exe` process remained before `PlanCatalog.Tests` began.

### 6.2 Targeted verification

`TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests` + `Gen35TwoDayJitBoundaryTests`: **6/6 passing** (§4).

### 6.3 `RunningApp.IntegrationTests` full regression

`backend/RunningApp.sln` → full solution build + test (Debug): **4341 passed, 4 failed, 4345 total** (35m35s). Test count is byte-identical to `GEN.36`'s own baseline total (`4345`) — this phase modified two existing test methods' assertions/inputs rather than adding new test methods, so no count shift is expected, and none occurred. The 4 failures are the identical, named, pre-existing baseline set carried since `GEN.17`-`GEN.36`:

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks: 13)
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks: 14)
3. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`
4. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity` — the disclosed, date-coincidental flake `GEN.36 §6.3` identified (`ConfirmAsync` hardcodes plan-start date `2026-09-07`; this phase's own execution date is `2026-09-07`, so a real session now legitimately falls on the literal current date, tripping the `today_workout`-is-null assertion). Confirmed unrelated: zero changes to this test file or any Home/Calendar controller/service this phase touched.

**Zero new regressions.** Every failure is a named, pre-existing, previously-disclosed baseline item; none is newly introduced by this phase's diff.

### 6.4 `PlanCatalog.Tests` full regression

Run sequentially after `RunningApp.IntegrationTests` fully exited (confirmed via `tasklist` showing zero `testhost.exe`/`vstest` processes beforehand). `plan-catalog/PlanCatalog.sln` → `PlanCatalog.Tests`: **1510 passed, 0 failed, 1510 total** (6s) — clean, unchanged from `GEN.36`'s own baseline. No `plan-catalog/` file touched this phase.

### 6.5 `git diff --check`

Clean on every file this phase touched.

---

## 7. Explicit constraints — confirmed respected

- **No new product/numeric authority invented.** The pace-anchor fix reads existing goal-pace data from a different, already-computed week; it derives nothing new. The volume/numeric-adapter fix reads existing allocation-policy inputs from the same anchor week for internal consistency; it invents no new allocation rule.
- **Did not extend the reasoning blindly.** Per §3, every other Core-consuming component this arc's own predecessors touched or mentioned (`PreparationRunwayCalendarComposer`, `TenKPreparationRunwayFinalInvariantValidator`, `LongHorizonStructuralValidator`, `LongHorizonCalendarAssigner`, `CatalogVolumeAndLongRunPlanner`, `CatalogPublicPreviewMaterializer`, `PreparationRunwayPersistablePlanMapper`, `DatedGeneratedCatalogPlanSkeletonValidator`, `CatalogWeekSkeletonCalendarMaterializer`, `WorkoutExposureVerifier`) was checked individually; only `PreparationRunwayCoreWeekOneTargetAdapter` shared the defect, and was fixed the same way, not generalized without checking.
- **No new `DOMAIN_DECISION_REQUIRED` surfaced.** The one secondary blocker found (the volume adapter's identical assumption) was the *anticipated* recurring-defect-family case the decision explicitly authorized fixing the same way — not a genuine new product question. No case was found where neither local week 1 nor week 2 could supply valid data.
- **2D Core's frozen product authority untouched.** `PeakVolumeBand`, taper multipliers, session minima, and `RunLayout` structure were not read for modification, let alone changed, anywhere this phase. This decision is scoped to anchor-selection logic in two adapters only.
- **Deriving a pace/volume target from an `EASY_SUPPORT` session was not implemented anywhere** — the fix always resolves to a real `KEY_SESSION`-bearing week, never an `EASY_SUPPORT`-only week, per the frozen decision's explicit rejection of that alternative.

---

## 8. Final classification

**`TWO_D_LONGHORIZON_FULL_GE_RUNWAY_CORE_CHAIN_COMPLETE_GLOBALWEEKNUMBER_PARITY_STRICT_ACROSS_ENTIRE_PLAN`** — **`DONE`**.

This closes the `GEN.33`-`GEN.37` arc: 2D LongHorizon's GE→Runway→Core chain now produces strict, continuous `GlobalWeekNumber`-parity content across its entire activated length, for both Levels and both GE-parity cases, driven through the real production rolling-activation/JIT-composition path against real PostgreSQL and independently re-verified from a fresh restart reload. `GEN.36`'s disclosed `DOMAIN_DECISION_REQUIRED` pace-anchor conflict is resolved exactly per the frozen user decision, a second instance of the same recurring defect (the volume/numeric adapter) was found and fixed the same way, and no other Core-consuming component this arc touched or mentioned shares the defect. Full regression is clean modulo the 3 long-standing named baseline failures plus the 1 disclosed date-coincidental flake — zero new regressions, both suites.

`NEXT_PHASE_NOT_YET_SCHEDULED` — 2D LongHorizon's structural/pace-continuity engineering is complete; any follow-on (e.g. adaptation/repair-path parity with the `FREQ.6D.11`-`22` Intermediate×5D precedent, or public HTTP activation of 2D LongHorizon itself) is a distinct, not-yet-scoped body of work.

---

## 9. Governance

`PHASE_LEDGER.md` row appended (`GEN.37`). `MASTER_ROADMAP.md`'s 2D-LongHorizon backlog item updated to record the resolved decision, the recurring-defect-family fix, and the full-chain completion. Two-commit self-referential-SHA-backfill pattern followed: implementation commit `157803f` (production fixes + updated tests + this report + ledger/roadmap with `PENDING` placeholder), this document and the ledger row backfilled with that SHA in a second, documentation-only commit. Normal push only.
