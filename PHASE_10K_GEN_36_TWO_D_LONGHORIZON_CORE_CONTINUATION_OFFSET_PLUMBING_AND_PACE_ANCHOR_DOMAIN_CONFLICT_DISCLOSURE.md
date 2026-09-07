# PHASE 10K-GEN.36 — 2D LongHorizon: Core Continuation-Offset Plumbing and Pace-Anchor Domain Conflict Disclosure

**Parent authority**: a direct user decision message, "DECISION ON GEN.35" (verbatim spec, reproduced in full in this phase's own task brief), authorizing a narrow, additive exception to this arc's standing "do not touch 2D Core" constraint, specifically to close `GEN.35 §2.3/§9`'s disclosed gap: Core's own real content generator has no mechanism to accept an external `GlobalWeekNumber` continuation offset. `GEN.35` (immediate predecessor, `DONE (PARTIAL)`), `GEN.33`/`GEN.34` (the `StartGlobalWeek` precedent this phase generalizes one layer further), `GEN.26 §2` (the "parameterize the mechanism, not a workaround/remap layer" precedent this decision explicitly cites and this phase honors), `GEN.20`/`GEN.25` (2D Core's real public HTTP activation, the exact call sites whose zero-delta this phase must prove).
**Phase type**: IMPLEMENTATION (additive parameter plumbing) + DOMAIN-CONFLICT DISCOVERY AND DISCLOSURE (no fix forced). New tests only; no product/numeric authority changed.
**Execution status**: DONE
**Final classification**: `TWO_D_LONGHORIZON_CORE_CONTINUATION_OFFSET_PLUMBING_ADDED_ZERO_DELTA_PROVEN_ACTIVATION_BLOCKED_ON_PACE_ANCHOR_DOMAIN_DECISION` — **`DONE (PARTIAL)`**, with one item explicitly classified `DOMAIN_DECISION_REQUIRED`.

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `5fac8ed` (`docs(gen-35): backfill governance commit SHA for GEN.35`). `git fetch && git diff HEAD origin/main`: empty — in sync, at the exact commit the governing decision named. `git status --porcelain` (excluding standing tracked `bin`/`obj` rebuild noise and pre-existing modified `baseline_tmp`/`ten-k-pilot-domain-decision-audit.*`/untracked `.trx` files from prior sessions): clean of unexpected changes. `PHASE_LEDGER.md`'s last row is `GEN.35` (seq 138) — **`GEN.36` confirmed correct** as the next free phase ID.

Required reading performed directly from the repository this phase: `PHASE_10K_GEN_35_...md` (full), `PHASE_10K_GEN_33_...md`/`PHASE_10K_GEN_34_...md` (the `StartGlobalWeek` precedent), `PHASE_10K_GEN_26_...md` (the "parameterize directly" precedent), `PHASE_10K_GEN_20_...md`/`PHASE_10K_GEN_25_...md` (2D Core's real public activation phases). Direct source reads of the entire real Core-generation call chain: `TenKPreparationRunwayDarkOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrationContracts.cs`, `TenKPreparationRunwayComponentAdapters.cs`, `DynamicCoreCalendarMaterializationOrchestrator.cs`, `DynamicCoreSessionPrescriptionOrchestrator.cs`, `DynamicCoreVolumeAndLongRunOrchestrator.cs`, `DynamicCoreWorkoutBindingOrchestrator.cs`, `DynamicCoreWeekSkeletonOrchestrator.cs`, `CatalogStageToWeekMaterializer.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`, `LongHorizonFullNumericOrchestrator.cs`, `PreparationRunwayCoreWeekOneTargetAdapter.cs`, `PreparationRunwayCoreWeekOnePaceAdapter.cs`, `TenKPreparationRunwayFinalInvariantValidator.cs`.

---

## 1. Step 0 — existing-mechanism search (per the governing decision's own required first step)

Searched explicitly, before implementing anything, for a pre-existing continuation/offset concept anywhere in Core's generator or its callers.

**Found: a partial mechanism, not a complete one.** `CatalogStageToWeekMaterializationContext.StartGlobalWeek` (the field at the very bottom of Core's real pipeline) **already exists** — it was added by `GEN.34`, with this exact doc comment on the field itself:

> "Phase 10K-GEN.34 — the plan-global ordinal of this materialization's own local week 1... A LongHorizon caller whose Core segment is the plan's third segment (GE then Runway then Core) supplies `GeWeeks + 8 + 1` here..."

`GEN.34` added this specifically anticipating Core's own need, but only ever wired it into the *dark structural-skeleton* path. Tracing every real caller between `LongHorizonRollingJitCompositionOrchestrator` (the real JIT-composition entry point) and this field found **six intermediate layers, none of which threaded a value into it**: `TenKPreparationRunwayDarkOrchestrationRequest` → `TenKPreparationRunwayCoreGenerationRequest` → `DynamicCoreCalendarMaterializationContext` → `DynamicCoreSessionPrescriptionContext` → `DynamicCoreVolumeAndLongRunContext` → `DynamicCoreWorkoutBindingContext` → `DynamicCoreWeekSkeletonOrchestrationContext` → `CatalogStageToWeekMaterializationContext.StartGlobalWeek`. Every one of these six context types simply omitted the field when constructing the next layer's context, leaving it at its implicit default (`1`) regardless of the true `GlobalWeekNumber` — this is the precise, previously-undiscovered mechanism `GEN.35 §2.3` described as absent ("no mechanism ... to accept an external GlobalWeekNumber continuation offset"): the bottom of the mechanism existed, but no caller in the real Core-generation chain had ever been wired to reach it.

**Conclusion**: no complete existing alternative — proceeded to the authorized approach (§2 below), reusing rather than duplicating `GEN.34`'s own mechanism.

---

## 2. Additive continuation-offset parameter — implemented, threaded through six layers

Added `CoreStartGlobalWeek` (default `1`) as a new, additive, trailing parameter on the two outer request records, and `StartGlobalWeek` (default `1`) as a new, additive `init` property on the six intermediate context classes, threading the value one layer at a time down to the pre-existing `CatalogStageToWeekMaterializationContext.StartGlobalWeek`:

| Layer | File | Change |
|---|---|---|
| 1 (outermost) | `TenKPreparationRunwayDarkOrchestrationContracts.cs` | `TenKPreparationRunwayDarkOrchestrationRequest.CoreStartGlobalWeek = 1` (new trailing param) |
| 2 | `TenKPreparationRunwayDarkOrchestrationContracts.cs` | `TenKPreparationRunwayCoreGenerationRequest.CoreStartGlobalWeek = 1` (new trailing param) |
| 3 | `TenKPreparationRunwayDarkOrchestrator.cs` | Stage 8 call now passes `CoreStartGlobalWeek: request.CoreStartGlobalWeek` |
| 4 | `TenKPreparationRunwayComponentAdapters.cs` | `TenKPreparationRunwayCoreGenerator.GenerateAsync` sets `StartGlobalWeek = request.CoreStartGlobalWeek` on `DynamicCoreCalendarMaterializationContext` |
| 5 | `DynamicCoreCalendarMaterializationOrchestrator.cs` | New `DynamicCoreCalendarMaterializationContext.StartGlobalWeek = 1`; threaded to `DynamicCoreSessionPrescriptionContext` |
| 6 | `DynamicCoreSessionPrescriptionOrchestrator.cs` | New `DynamicCoreSessionPrescriptionContext.StartGlobalWeek = 1`; threaded to `DynamicCoreVolumeAndLongRunContext` |
| 7 | `DynamicCoreVolumeAndLongRunOrchestrator.cs` | New `DynamicCoreVolumeAndLongRunContext.StartGlobalWeek = 1`; threaded to `DynamicCoreWorkoutBindingContext` |
| 8 | `DynamicCoreWorkoutBindingOrchestrator.cs` | New `DynamicCoreWorkoutBindingContext.StartGlobalWeek = 1`; threaded to `DynamicCoreWeekSkeletonOrchestrationContext` |
| 9 (innermost, pre-existing) | `DynamicCoreWeekSkeletonOrchestrator.cs` | New `DynamicCoreWeekSkeletonOrchestrationContext.StartGlobalWeek = 1`; threaded to the already-existing `CatalogStageToWeekMaterializationContext.StartGlobalWeek` (`GEN.34`) |

Every new property/parameter defaults to `1`. All six intermediate context classes use `required`/`init` object-initializer construction (not positional records), so every pre-existing construction site across the codebase (production and test) compiles unchanged with no edits required — confirmed by `git status --porcelain` showing no other file touched to accommodate the new fields, and by a full solution build succeeding with 0 errors on the first attempt after adding all nine layers.

`dotnet build RunningApp.sln` (Debug): 0 errors, only pre-existing warnings, immediately after this change.

---

## 3. Attempting activation at the one real call site — a genuine domain conflict found, not a wiring gap

Per the governing decision, only `LongHorizonRollingJitCompositionOrchestrator`'s real JIT-composition call site was to supply a non-default value — `coreSegment.StartGlobalWeek`, the exact same authoritative `StructuralRoadmap` value `BuildBoundedCoreSelection` already uses, unchanged, to stamp each generated Core week's own `GlobalWeekNumber` (so both consumers of "where is Core's week 1 in the global sequence" would agree by construction, mirroring `GEN.33`'s/`GEN.35`'s own `RunwayStartGlobalWeek: GeneralEnduranceWeeks + 1` convention exactly).

Wired this one line and ran a direct diagnostic against the real `TenKPreparationRunwayDarkOrchestrator` (a 2D Beginner 15-week Runway/Core dark composition with `CoreStartGlobalWeek = 10`, reproducing the exact arithmetic the real orchestrator would supply for the `totalWeeks=21`/`geWeeks=1` odd-GE-parity LongHorizon scenario — `GeneralEnduranceWeeks + 8 + 1 = 1 + 8 + 1 = 10`, even). Result:

```
FinalInvariantValidation/OrchestrationInvariantViolation:
Authoritative Core Foundation Week 1 pace target is unavailable.
```

**Root cause, traced precisely**: `PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayPacePrescription/PreparationRunwayCoreWeekOnePaceAdapter.cs`, line 26) has a **pre-existing, hard, unconditional requirement**:

```csharp
if (first is null || keyCount < 1 || longCount != 1 || ... )
    throw new InvalidOperationException("Authoritative Core Foundation Week 1 pace target is unavailable.");
```

`keyCount < 1` means Core's own Foundation Week 1 must carry at least one `KEY_SESSION`. This was always satisfied before `GEN.36`, because Core's local week 1 was *always*, unconditionally, Pattern A (`KEY_SESSION`+`LONG_RUN`) — the exact behavior `GEN.35 §2.3` disclosed as the anchor-continuity gap. Once Core's week 1 legitimately continues the true global parity, an **even** `CoreStartGlobalWeek` correctly resolves to Pattern B (`EASY_SUPPORT`+`LONG_RUN`, **zero** `KEY_SESSION`) via the already-existing, `GEN.34`-added `ResolveWeekRoles` logic in `CatalogStageToWeekMaterializer.cs` — and this adapter has no path for that shape at all.

This exact `keyCount<1` failure occurs **specifically for the odd-`GeneralEnduranceWeeks` case** (`10` is even because `1+8+1=10`) — i.e., precisely the GE-parity case this entire `GEN.33`→`GEN.36` arc exists to fix. The even-`GeneralEnduranceWeeks` case (`totalWeeks=22`, `geWeeks=2` → `CoreStartGlobalWeek=11`, odd → Pattern A, `keyCount=1`) would **not** trip this — which is itself an important, disclosed fact: enabling this parameter naively would have silently "worked" for one parity case and hard-failed the other, exactly the kind of single-parity-case masking `GEN.35 §2.2` already warned about for a different defect.

**Why this is a genuine numeric/pace-continuity authority question, not a mechanical wiring gap**: `PreparationRunwayCoreWeekOnePaceAdapter`'s sibling, `PreparationRunwayCoreWeekOneTargetAdapter` (the *volume* adapter, not pace), was already generalized by `GEN.29` to tolerate a zero-`KEY_SESSION` week (reading the real `keySessionCount`/`easySupportCount` from Core's own output rather than assuming a fixed shape) — proving this *class* of generalization is a known, previously-applied, safe pattern in this codebase. But the *pace* adapter's requirement is different in kind: a `KEY_SESSION` carries goal-pace-derived pacing data; an `EASY_SUPPORT` session structurally does not (confirmed by `TenKPreparationRunwayFinalInvariantValidator`'s own separate pace-continuity check, which explicitly *forbids* Runway's own paced sessions from carrying `GOAL_PACE`/`THRESHOLD`/`RACE_SPECIFIC` labels — i.e., pace type is not interchangeable between roles). Whether Runway's own numeric ramp should instead anchor to the *first KEY_SESSION week* (which, for a 2-week alternating pattern, is always Core's own week 2 when week 1 lands on Pattern B) — or some other resolution — is a genuine product/pace-continuity design decision, not something this authorization covers. The governing decision was explicit: *"If implementing the parameter reveals a need to touch any of those [PeakVolumeBand, taper, session minima, RunLayout], STOP → classify DOMAIN_DECISION_REQUIRED — this authorization does not extend that far."* While the pace adapter is not literally one of those four named categories, it is the same class of thing — a numeric/pace-continuity authority assumption baked into how Runway's own ramp is derived from Core's real output — and resolving it (by relaxing the adapter, changing which week anchors the ramp, or any other approach) would be inventing a new pace-continuity rule unprompted. Per the decision's own explicit instruction, this phase **stopped here and did not attempt a fix**.

**Action taken**: the one line wiring `CoreStartGlobalWeek: coreSegment.StartGlobalWeek` into the real `LongHorizonRollingJitCompositionOrchestrator` call site was **reverted** before this phase's implementation commit — left at its default (`1`), preserving `GEN.35`'s exact prior behavior (Core's own anchor-continuity gap remains open, exactly as `GEN.35` disclosed it, with no new failure mode introduced). Activating it would have caused every real 2D LongHorizon plan whose `GeneralEnduranceWeeks` is odd to newly **fail** the Runway→Core JIT boundary outright — a regression relative to `GEN.35`'s baseline (which succeeded, just with the disclosed wrong anchor) — which this phase's own "prove zero-delta" mandate forbids introducing.

A permanent, dark (no HTTP, no PostgreSQL), reproducible test documents this exact conflict for the next phase: `TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests.CoreStartGlobalWeekEven_CausesCoreWeekOnePatternB_TripsPreExistingKeySessionPaceAnchorRequirement` — asserts the *current, disclosed, still-blocked* behavior (fails with exactly this stage/code/message), with an explicit doc-comment warning it must never be "fixed" by loosening the adapter without an actual domain decision. A sibling test, `CoreStartGlobalWeekDefault_SameRequestShape_StillSucceeds_ConfirmingZeroDeltaAtDefault`, confirms the identical request shape still succeeds at the default offset — isolating the conflict to the non-default value specifically, not a pre-existing failure in this request shape.

---

## 4. Zero-delta proof for every existing Core caller

Because the real call site's activation was reverted, **every existing Core caller — including the real JIT-composition path itself — resolves `StartGlobalWeek`/`CoreStartGlobalWeek` to its default (`1`) everywhere in production code**, with no exceptions. This makes the zero-delta claim unusually strong: it is not merely "the new parameter defaults safely," it is "the new parameter is never supplied a non-default value by any production code path in this phase's final state." Proof, by caller:

- **2D Core's own public HTTP path** (`GEN.20`/`GEN.25`): `CatalogPreviewGenerator`'s dynamic Core composition never constructs a `DynamicCoreCalendarMaterializationContext` with `StartGlobalWeek` set — confirmed by `git diff --stat -- backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` returning empty (this phase touched zero lines of that file). Default `1` applies, byte-identical to pre-`GEN.36`.
- **Every other already-`PUBLICLY_ACTIVE` standalone Core frequency/level** (Intermediate 3D/4D/5D/6D, Beginner 4D/3D, Advanced 3D/4D/5D/6D): all reach Core generation through the identical, unmodified `CatalogPreviewGenerator`/`DynamicCoreCalendarMaterializationOrchestrator` path above — same proof applies.
- **`LongHorizonFullNumericOrchestrator`'s own separate 4D/5D Core-generation call site** (`LongHorizonFullNumericOrchestrator.cs` line 145): constructs `TenKPreparationRunwayCoreGenerationRequest` positionally with 9 arguments, one fewer than the record's new 10-parameter shape — the new trailing `CoreStartGlobalWeek` parameter is omitted and resolves to its default (`1`) automatically; this file was not edited.
- **The real JIT-composition path itself** (`LongHorizonRollingJitCompositionOrchestrator.cs`): explicitly reverted to omit `CoreStartGlobalWeek` from its `TenKPreparationRunwayDarkOrchestrationRequest` construction (§3 above) — resolves to default `1`, identical to `GEN.35`'s own behavior.
- **Every internal-dark Core generation path exercised by this repository's test suite** (`Gen29TwoDayRunwayDarkOrchestrationTests`, `TenKPreparationRunwayDarkOrchestratorTests`, `TenKPreparationRunwayDarkOrchestrator5DTests`, `PreparationRunwayHorizonAuthorityTests`, `PreparationRunwayPublicPreviewMapperEqualityTests`, `LongHorizonNumericActivationBoundaryScanTests`, `LongHorizonRunwayEntryAboveCoreTargetPolicyDiagnosticTests`, `LongHorizonCoreWeekOneEvidenceAuthorityDiagnosticTests`, `DynamicCoreSessionPrescriptionOrchestratorTests`, `Gen23BeginnerThreeDayCoreTests`, `DynamicCoreVolumeAndLongRunOrchestratorTests`, `Gen17TwoDayVolumeAndLongRunDarkVerificationTests`, `Phase4G5GPrerequisiteCrossChecksTests`, `V1FiveDayIntermediateMissingReadinessStartingVolumePolicyTests`, `Gen12TwoDayDarkVerificationTests`, `Gen3AThreeDayCoreTests`, `Gen17TwoDayWorkoutBindingDarkVerificationTests`, `DynamicCoreCalendarMaterializationOrchestratorTests`, `Freq6D4D5GCoreHorizonExecutionContextTests`): none of these construction sites were edited (confirmed by grep — §"construction site inventory" performed before implementing, listing every `new TenK...CoreGenerationRequest`/`new DynamicCore...Context` in the repository), so every one resolves to the new field's default. All pass unmodified (§6.2 below), which is itself the zero-delta proof in executable form, not merely narrative.
- **`Gen35TwoDayJitBoundaryTests`** (the real-Postgres GE→Runway→Core chain test): re-run explicitly this phase, all 4 scenarios (both Levels × both GE-parity cases) still pass with byte-identical outcomes to `GEN.35`'s own baseline (§5 below) — direct, executable confirmation that the real JIT-composition path's behavior is unchanged.

---

## 5. Full GE→Runway→Core chain verification — re-confirmed unchanged from `GEN.35`, NOT newly completed

Per the governing decision's own honesty requirement: the full chain was **not** advanced to full continuity this phase. `Gen35TwoDayJitBoundaryTests.GeToRunwayToCore_RealJitBoundary_SucceedsAgainstRealPostgres_ThroughPublished140Bundle` (4 scenarios: both Levels × `totalWeeks∈{21,22}`) was re-run against real PostgreSQL and passes identically to `GEN.35`'s own result — GE+Runway strict global-parity alternation proven across the entire `1..runwayEnd` range (unchanged, still correct, `GEN.35`'s own fix), and Core's own weeks are internally consistent but still anchored to Core's own local week 1 rather than the continuing global parity (the disclosed gap, still open, now understood precisely to be blocked on §3's domain conflict rather than merely "not yet implemented"). No regression, but also no forward progress on Core's own anchor continuity — the honest state is `DONE (PARTIAL)`, not full completion.

---

## 6. Required verification

### 6.1 Operational note — process-check method used

Every regression run in this phase was preceded by a plain, unfiltered `tasklist | grep -i "testhost\|vstest"` review (never the filtered `tasklist //FI` form found unreliable in `GEN.32`), confirming no stale process before starting; the two suites were never run concurrently.

### 6.2 New-test verification

- `TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests.cs` — 2 new tests, both passing in isolation: the domain-conflict reproduction (asserts the current, disclosed, still-blocked failure precisely) and the default-value zero-delta control (confirms the identical request shape succeeds at the default offset).
- `Gen35TwoDayJitBoundaryTests` (comments updated only, logic restored byte-identical to `GEN.35`'s own version after this phase's activation attempt was reverted) — 4/4 passing, real PostgreSQL, re-verified in isolation.

### 6.3 Targeted zero-delta sweep

`FullyQualifiedName~LongHorizon | FullyQualifiedName~PreparationRunway`: **2009 passed, 1 failed, 2010 total** (16m45s) — `2010 = GEN.35`'s own `2008` + this phase's 2 new tests. The 1 failure, `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, is a genuine but **entirely unrelated pre-existing defect**, discovered incidentally by this run, not caused by any `GEN.36` change: its shared `ConfirmAsync` helper hardcodes the plan start date as `new DateOnly(2026, 9, 7)`; the real wall-clock date during this phase's own execution window is now literally `2026-09-07`, so the assertion `Assert.Null(home["today_workout"])` fails because a real session now legitimately falls on the literal current date. Confirmed unrelated by: (a) `git diff --stat` showing zero changes to this test file or to any Home/Calendar controller/service this phase touched; (b) the failure is deterministic and reproduced identically across two independent full-suite runs (§6.4); (c) direct source read of `ConfirmAsync` (line 307) confirms the literal hardcoded date. This is a **date-coincidence time-bomb** in a pre-existing test, unrelated to Core/Runway continuation offsets, outside this phase's scope to fix, and disclosed here rather than silently absorbed into "known baseline failures."

A separate, second failure was found and **fixed** during this same sweep before the numbers above: `DynamicCoreCalendarMaterializationOrchestratorTests.LiveReachability_OnlyCatalogPreviewGeneratorMayCallTheOrchestrator` — a dark-reachability test asserting exactly one source-tree reference to `DynamicCoreCalendarMaterialization(Orchestrator|Context|Result)` outside two exempted directories, and that the one reference is `CatalogPreviewGenerator.cs`. This phase's own doc comments in `DynamicCoreVolumeAndLongRunOrchestrator.cs`, `DynamicCoreSessionPrescriptionOrchestrator.cs`, and `DynamicCoreWorkoutBindingOrchestrator.cs` (all outside the exempted directories) initially spelled out the literal type name `DynamicCoreCalendarMaterializationContext` in `<see cref>` tags, adding extra matches and breaking the test's `Assert.Single`. Fixed by rewording those three comments to describe the chain without the literal type name (`<c>CatalogStageToWeekMaterializationContext.StartGlobalWeek</c>` instead) — a genuine self-inflicted defect from this phase's own documentation, caught by the existing test exactly as designed, and corrected before this phase's implementation commit. Re-verified in isolation: **27/27 passing**.

### 6.4 `RunningApp.IntegrationTests` full regression

Confirmed no `testhost`/`vstest` process running (unfiltered `tasklist | grep`) immediately before each run; run alone, never concurrent with `PlanCatalog.Tests`. Run twice for reliability (the first run, `gen36_full_regression.trx`, still had the not-yet-fixed reachability defect above; the second, `gen36_full_regression_v2.trx`, is the final, reportable number): `backend/RunningApp.sln` → full solution build + test (Debug): **4341 passed, 4 failed, 4345 total** (35m33s). `4345 - 4343 = 2`, exactly matching this phase's 2 new tests (both passing). The 4 failures are: the identical 3 named pre-existing baseline failures carried since `GEN.17`-`GEN.35` (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` weeks 13 and 14, `Sw09ExplicitZeroReadinessEndToEndTests`) plus the 1 newly-disclosed, unrelated date-coincidence flake documented in §6.3 (`LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`) — **zero regressions caused by this phase's own change**, reconciled precisely: `4341 = 4340 (GEN.35's own passing count) + 2 (new, passing) - 1 (newly-failing, but unrelated to this phase's diff)`.

### 6.5 `PlanCatalog.Tests` full regression

Run explicitly and sequentially after the backend suite fully exited (confirmed via `tasklist | grep` showing zero `testhost`/`vstest` processes beforehand). `plan-catalog/PlanCatalog.sln` → `PlanCatalog.Tests`: **1510 passed, 0 failed, 1510 total** (6s) — clean, unchanged from `GEN.35`'s own baseline. No `plan-catalog/` file touched this phase (confirmed by `git diff --stat -- plan-catalog/` returning empty for source files).

### 6.6 `git diff --check`

Clean on every file this phase touched — no conflict markers, no trailing-whitespace errors (only pre-existing LF/CRLF `autocrlf` warnings, consistent with every prior phase in this engagement).

---

## 7. Explicit constraints — confirmed respected

- **No new product/numeric authority invented.** The additive plumbing reuses `GEN.34`'s own `StartGlobalWeek` mechanism and convention exactly; no new numeric constant, band, multiplier, or structural rule was added anywhere.
- **The one place this phase's own investigation reached toward product/numeric authority — `PreparationRunwayCoreWeekOnePaceAdapter`'s pace-anchor requirement — was explicitly NOT resolved.** Per the governing decision's own instruction, this is disclosed and classified `DOMAIN_DECISION_REQUIRED`, not worked around.
- **2D Core and Runway's frozen product authority untouched.** `PeakVolumeBand`, taper multipliers, session minima, and `RunLayout` structure were not read for modification, let alone changed, anywhere this phase.
- **No workaround/remap layer built.** Per `GEN.26 §2`'s explicit precedent (cited by the governing decision itself), this phase parameterized the actual mechanism directly (`StartGlobalWeek` threaded through the real generator's own call chain) rather than building a post-generation correction layer — and, on discovering the pace-anchor conflict, did not build a workaround for *that* either; it stopped and disclosed.
- **Still dark-only where it matters.** `V1CatalogPilotIdentityPolicy.cs`'s public gate methods untouched. No public HTTP routing added or changed.
- **Zero-delta proven, not narrated**: §4 traces every real and test call site by name and confirms none supplies a non-default value in this phase's final state.

---

## 8. Final classification

**`TWO_D_LONGHORIZON_CORE_CONTINUATION_OFFSET_PLUMBING_ADDED_ZERO_DELTA_PROVEN_ACTIVATION_BLOCKED_ON_PACE_ANCHOR_DOMAIN_DECISION`** — **`DONE (PARTIAL)`**.

Step 0 found `GEN.34`'s own mechanism already existed at the bottom of Core's real pipeline but was never threaded to it; this phase added the additive, default-preserving plumbing across all six missing intermediate layers, confirmed by build and by the full existing test surface that it is genuinely inert everywhere in production. Attempting to activate it at the one real intended call site surfaced a genuine, previously-latent pace-continuity numeric-authority conflict (`PreparationRunwayCoreWeekOnePaceAdapter`'s hard `KEY_SESSION`-at-Core-Week-1 requirement, tripped specifically by the odd-`GeneralEnduranceWeeks` case this arc exists to fix) — classified `DOMAIN_DECISION_REQUIRED` and left unresolved, per the governing decision's own explicit instruction to stop rather than work around it. The activation line was reverted before commit, so this phase introduces **zero behavioral change** to any real request path; `GEN.35`'s own disclosed Core-anchor-continuity gap remains exactly as it was, now understood one layer more precisely and with the concrete blocking question named for the next phase.

## 9. Concrete remaining contract for the next phase

1. A genuine product/pace-continuity decision is needed: when Core's own true local week 1 (continuing the established global parity) lands on the EASY-only pattern (zero `KEY_SESSION`), what should anchor Runway's own numeric/pace ramp toward Core? Candidate resolutions include (a) falling back to the next `KEY_SESSION` week (Core's own week 2, for a 2-week alternating pattern) as the authoritative anchor, (b) deriving a pace target from the `EASY_SUPPORT` week using a different, explicitly-approved rule, or (c) some other approach — this phase deliberately does not recommend one, since choosing is exactly the authority this decision withheld.
2. Once that decision is made, enable the single reverted line in `LongHorizonRollingJitCompositionOrchestrator.cs` (`CoreStartGlobalWeek: coreSegment.StartGlobalWeek`, currently commented with the exact reasoning) and update `PreparationRunwayCoreWeekOnePaceAdapter` accordingly.
3. Re-run `TenKPreparationRunwayGen36CoreContinuationOffsetDomainConflictTests`'s domain-conflict test — it is expected to start failing once resolved (its own doc comment says so explicitly), at which point it should be replaced with a test asserting the new, decided behavior; `Gen35TwoDayJitBoundaryTests`'s own final assertion block (currently documenting Core's disclosed local-anchor behavior) will need the same update `GEN.36`'s own draft (built, then reverted) already sketched — extending strict `GlobalWeekNumber` parity across the *entire* plan, not merely GE+Runway.
4. Budget for the full zero-delta regression this phase already flagged: once activated, prove no change for every already-`PUBLICLY_ACTIVE` standalone Core frequency/level (this phase's own §4 inventory of call sites is the starting checklist).

`NEXT_PHASE_NOT_YET_SCHEDULED`.

---

## 10. Governance

`PHASE_LEDGER.md` row appended (`GEN.36`). `MASTER_ROADMAP.md`'s 2D-LongHorizon backlog item updated to record the additive plumbing, the zero-delta proof, and the disclosed `DOMAIN_DECISION_REQUIRED` pace-anchor conflict. Two-commit self-referential-SHA-backfill pattern followed: implementation commit (production plumbing + new tests + this report + ledger/roadmap with `PENDING` placeholder), this document and the ledger row backfilled with that SHA in a second, documentation-only commit. Normal push only.
