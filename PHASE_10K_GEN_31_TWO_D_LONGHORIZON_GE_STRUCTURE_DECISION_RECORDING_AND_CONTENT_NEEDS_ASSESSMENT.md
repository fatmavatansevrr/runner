# PHASE 10K-GEN.31 — 2D LongHorizon GE-Segment Structure: Decision Recording, Content-Needs Assessment, Step 2 Scoping

**Parent authority**: `GEN.30` (`TWO_D_LONGHORIZON_ARCHITECTURE_MAPPED_MECHANICAL_AUTHORITY_CONFIRMED_GE_STRUCTURE_DOMAIN_DECISION_REQUIRED` — the mechanism-by-mechanism mapping this phase acts on), a direct user decision message ("DECISION ON GEN.30") approving Option A and authorizing Step 2 implementation, `GEN.26`/`GEN.29`/`GEN.11` (frozen 2D structural/numeric authority), `FREQ.6D.11`-`FREQ.6D.22` (the real Intermediate×5D LongHorizon precedent).
**Phase type**: GOVERNANCE (DECISION RECORDING) + CONTENT-NEEDS ASSESSMENT + STEP-2 SCOPING. No production code, migration, catalog document, or test was authored or modified this phase.
**Execution status**: DONE
**Final classification**: `TWO_D_LONGHORIZON_GE_STRUCTURE_OPTION_A_APPROVED_CONTENT_REUSE_CONFIRMED_IMPLEMENTATION_DEFERRED` — `DONE (PARTIAL)`

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `4d4f6ed` (`docs(gen-30): backfill governance commit SHA for GEN.30`). `git fetch && git rev-list --left-right --count origin/main...HEAD`: `0 0` — in sync, confirmed at the exact commit the governing prompt named. `git status --porcelain` (excluding the standing `bin`/`obj` rebuild-artifact noise this engagement's own note explains): only `baseline_tmp` and `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`, both pre-existing modified-in-place tracked files unrelated to this phase, left untouched. `PHASE_LEDGER.md`'s last row is `GEN.30` (row 133) — **`GEN.31` confirmed correct** as the next free phase ID, verified against repository truth, not assumed from the prompt.

Full required reading performed directly from the repository this phase:
- `PHASE_10K_GEN_30_TWO_D_LONGHORIZON_ARCHITECTURE_MAPPING_AND_GE_STRUCTURE_DOMAIN_DECISION.md` (full) — §3 (`TWO_D_LONGHORIZON_MECHANISM_MAP`), §4 (Option A/B), §5 (content-reconciliation-does-not-transfer finding), §9 (next-steps sketch).
- `PHASE_10K_GEN_29`, `PHASE_10K_GEN_26`, `PHASE_10K_GEN_11` (full) — frozen 2D authority.
- Direct source reads this phase: `LongHorizonGeStructuralContracts.cs`, `LongHorizonGeStructuralSelector.cs`, `LongHorizonGeNumericExecutor.cs`, `FourDaySessionDistanceAllocationPolicy.cs`, and the `RollingActivation/` directory listing (19 files: `LongHorizonRollingJitCompositionOrchestrator`, `LongHorizonRollingCoreGenerationInputAdapter`, `LongHorizonPublicPlanService`, `LongHorizonRollingStateRepository`, `LongHorizonRollingCheckpointRuntime`, `LongHorizonCheckpointEvidenceAggregator`, `LongHorizonRollingJitActivationRuntime`, `LongHorizonActivatedCalendarAlignmentValidator`, `LongHorizonRealCalendarProjectionAdapter`, `LongHorizonCheckpointStateEvaluator`, and their Persistence/PublicPreview/LifecycleValidation subfolders), confirming `GEN.30 §3.3`'s inventory of the real, non-trivial admission-gate/JIT-threading surface is accurate and current.
- `FREQ.6D.11`/`.12`/`.13`/`.14` re-consulted directly against current source (not from `GEN.30`'s summary alone) for the GE structural-selector/numeric-executor mechanism this phase's content assessment depends on.

---

## 1. Governance decision recorded — Option A approved, Option B rejected

Per the direct user decision message ("DECISION ON GEN.30"), recorded here verbatim as frozen authority:

> 2D LongHorizon GE segment inherits the Model B A/B alternating structure unmodified, continuing the SAME global ordinal (`LongHorizonStructuralWeek.GlobalWeekNumber`) from GE Week 1 straight through Runway and Core.
>
> No new structural authority required — direct, symmetric extension of `GEN.26`'s own reasoning (2D never had a spare lane to construct a separate GE cadence from) applied to GE the same way it was already applied to Runway.

**Option B** (a fixed `1×KEY+1×LONG` every GE week, copying 4D/5D's GE shape literally) is **formally rejected** — not implemented, not partially borrowed from, per explicit instruction.

This closes `GEN.30 §4`'s `DOMAIN_DECISION_REQUIRED` item. The GE segment now has approved structural authority: **every GE week is either Pattern A (`KEY_SESSION + LONG_RUN`) or Pattern B (`EASY_SUPPORT + LONG_RUN`)**, alternating exactly as Runway/Core already do, with `GlobalWeekNumber` continuing unbroken from GE Week 1 through the GE→Runway and Runway→Core handoffs. GE requires no separately-calibrated weekly shape of its own — it inherits Model B's shape, per `GEN.26 §2`'s reasoning extended one segment further back.

---

## 2. GE-specific content needs — assessed and flagged before any implementation

Per the governing prompt's explicit constraint, this section identifies GE content needs as **reuse** or **fresh-authoring-required** *before* any implementation is attempted, rather than deciding implicitly mid-build.

### 2.1 Direct trace of the existing GE stage-family content table

`LongHorizonGeStructuralSelector.StageFamilyRoleAssignments` (re-read in full this phase, current source) already defines, **independently**, for every one of the five stage families (`Entry`/`BaseDevelopment`/`AerobicDurability`/`Consolidation`/`PreRunwayAlignment`):
- A `KEY_SESSION` role assignment (per-family: `EASY_STANDARD` for `Entry`/`Consolidation`/`PreRunwayAlignment` and the `CONSISTENCY_NEEDED` profile branch of `BaseDevelopment`/`AerobicDurability`; `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` for the `CORE_ENTRY_READY` profile branch of `BaseDevelopment`/`AerobicDurability`).
- An `EASY_SUPPORT` role assignment (`EASY_STANDARD`, uniformly, for every family, every profile — `role: "ANY"`).
- A `LONG_RUN` role assignment (`LONG_RUN_STANDARD`, uniformly).

This table was built for 4D/5D's constant-every-week-KEY GE shape, where both role assignments are consumed **in the same week** (KEY_SESSION plus N EASY_SUPPORT sessions, never a choice between them). Under Option A, a 2D GE week consumes **exactly one** of these two already-defined role assignments per week — the `KEY_SESSION` assignment on Pattern-A weeks, the `EASY_SUPPORT` assignment on Pattern-B weeks — never both, never neither, mirroring the Model B shape exactly.

### 2.2 Finding: REUSE — zero fresh content authoring required

Because the stage-family table already carries an independently-authored, already-approved catalog reference for **both** roles in every family, Option A's alternation does not need any new `WorkoutDefinition`, any new stage-family entry, or any new role-tag mechanism analogous to `GEN.29`'s `PatternBEasySupportReference` — that mechanism existed for Preparation Runway specifically because Runway's block-progression catalog (`Consistency`/`GeneralEndurance`/`AerobicStrength`/`PreSpecificTransition`) had **only** a `KEY_SESSION`-anchored entry per block-local week, with no independent `EASY_SUPPORT` alternate defined anywhere until `GEN.28`/`GEN.29` added one. GE's own catalog table never had that gap — `EASY_SUPPORT` was already independently defined for every stage family from the original 4D authoring, precisely because 4D/5D GE weeks always carry both roles simultaneously.

**Classification: `GE_CONTENT_REUSE_CONFIRMED_NO_FRESH_AUTHORING_REQUIRED`.** Selecting between an already-defined `KEY_SESSION` assignment and an already-defined `EASY_SUPPORT` assignment by week pattern is a structural **selection** decision (which of two existing catalog references applies to a given week), not a content-authoring decision. This mirrors `GEN.29`'s own zero-new-content resolution for `AerobicStrength`'s Pattern-B case (`AerobicStrength` content stays on Pattern-A KEY weeks; Pattern-B uses the already-existing `EASY_STANDARD` reference as `EASY_SUPPORT`) — the identical mechanism, generalized to GE's own independently-authored table instead of Runway's block table, and confirmed necessary *because* GE's table already has both role assignments rather than needing one added.

This finding is disclosed as the required decision point per the governing prompt's own instruction, even though the answer resolves to reuse: it was verified by direct trace of the current stage-family table, not assumed from `GEN.29`'s precedent applying automatically.

### 2.3 What is *not* resolved by this finding

This confirms the **content reference** question only (which catalog workout key/version applies to which role in which stage family — already answered, reuse). It does not resolve, and this phase does not attempt to resolve:
- The **structural selector code change** needed to actually emit an alternating (rather than constant-KEY) descriptor sequence for 2D (§3 below — deferred).
- Whether `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` (the `CORE_ENTRY_READY`-profile `KEY_SESSION` content for `BaseDevelopment`/`AerobicDurability`) needs the identical Pattern-A/B stimulus-disclosure treatment `GEN.28`/`GEN.29` gave it for Runway. Because this content only ever occupies `KEY_SESSION` (never `EASY_SUPPORT`) in the existing table, and Option A's Pattern-B weeks simply never select `KEY_SESSION` at all (not a substitution of `AerobicStrength` content with different content, but a week that structurally has no `KEY_SESSION` slot), this is the same "no substitution, only omission" shape `GEN.26 §2` already reasoned about for Runway/Core, not a new disclosure question — noted here for completeness rather than re-litigated.

---

## 3. Step 2 implementation — status, and why it is honestly deferred this phase

### 3.1 What was directly re-confirmed as still accurate

Re-reading `RollingActivation/`'s current source this phase (19 files) confirms `GEN.30 §3.3`'s own characterization is accurate and current: real admission-gate/cardinality-check sites exist in `LongHorizonRollingJitActivationRuntime` (`IsValidFourDayAvailability`/`IsValidAvailability`), `LongHorizonRollingCoreGenerationInputAdapter`, `LongHorizonRollingCheckpointRuntime.ValidateInput`, `LongHorizonCheckpointStateEvaluator.AvailabilityFeasible`, `LongHorizonCheckpointEvidenceAggregator.Aggregate`, `LongHorizonRollingStateRepository.InitializeStructuralStateAsync`, `LongHorizonRealCalendarProjectionAdapter.ValidateRunwayProjectionAuthority`, `LongHorizonActivatedCalendarAlignmentValidator.Validate`, and `LongHorizonPublicPlanService`'s seven public-gate sites — none of which this phase modified. `GEN.30`'s own estimate ("nontrivial, not a one-line fix... realistically a multi-phase effort") is independently re-confirmed by this direct re-read, not merely re-cited.

### 3.2 One safe, mechanically-confirmed building block, verified but not wired in

`FourDaySessionDistanceAllocationPolicy.Allocate` (`RuntimeCatalog/Prescription/Session/FourDaySessionDistanceAllocationPolicy.cs`) was directly re-read this phase and confirmed **already** supports `keySessionCount=0`/`easySupportCount=0` mutual exclusion — this generalization was made by `GEN.20` specifically citing 2D's Model B shape in its own code comment ("2D's real Model B week... either a KEY_SESSION... or an EASY_SUPPORT... never both, never neither"), and re-verified zero-delta for every existing 3D+ caller at the time. This is real, existing, already-regression-proven authority: the numeric-allocation layer GE's own executor would need for an alternating 2D week is **already mechanically ready**, requiring no new code at that layer. This confirms (not merely repeats) `GEN.30 §3.9`'s numeric-reuse finding down to the exact call signature GE's own executor would need to use.

### 3.3 Why full Step 2 (structural selector generalization, admission-gate widening, JIT/persistence/repair wiring, GE→Runway handoff, dark verification) was not attempted this phase

Three reasons, disclosed rather than glossed over:

1. **Scale, independently re-confirmed.** §3.1's direct re-read confirms this is genuinely ~20 real gate/adapter sites across the JIT/persistence/repair/public-gate layers, plus a structural-selector redesign (the current `LongHorizonGeWeekDescriptor`/`LongHorizonGeStructuralSelector` shape assumes constant-KEY-every-week and always-populated `KeySessionWorkout`/non-empty `EasySupportWorkouts` — Option A's alternation requires the descriptor to represent a week with *zero* `KEY_SESSION` or *zero* `EASY_SUPPORT`, a real shape change, not a parameter tweak). This matches `GEN.30`'s own "realistically a multi-phase effort... even though the single hardest piece of the 5D arc does not recur" assessment, independently re-confirmed here.
2. **Zero-delta risk under real time constraints.** The governing prompt requires the zero-delta claim for Intermediate×5D's own LongHorizon behavior to be *verified, not assumed* — touching ~20 real admission-gate/JIT/persistence sites and asserting zero-delta responsibly requires the same class of real dark end-to-end matrix run `FREQ.6D.13`-`.19` performed for 5D (real PostgreSQL, restart, repair, full 21-52wk horizon sweep). Attempting a partial subset of that wiring within this phase's remaining scope, without completing the matching verification matrix, would risk exactly the kind of undisclosed, under-verified change this engagement's own discipline (`GEN.29`'s own six-additional-defect dark-verification finding is the concrete proof that partial/unverified LongHorizon-adjacent changes reliably surface real defects only real execution catches) has repeatedly warned against forcing.
3. **This phase's own honest scope.** This phase's real, defensible contribution is resolving the one open decision (§1), resolving the one open content question (§2), and re-confirming (not merely re-citing) the implementation surface's current shape and scale (§3.1-3.2) — a decision-and-scoping phase, analogous in kind to how `GEN.26`/`GEN.28` preceded `GEN.27`/`GEN.29`'s own separate implementation phases in this same engagement's Preparation Runway arc. Per the governing prompt's own explicit framing, `DONE (PARTIAL)` is the expected, non-failure outcome here.

### 3.4 What remains — the concrete Step 2 implementation contract for the next phase

1. Generalize `LongHorizonGeWeekDescriptor`/`LongHorizonGeStructuralSelector` to emit an Option-A alternating sequence for 2D (new `HasKeySession`/`HasEasySupport` per-week flags or equivalent, additive to the existing 4D/5D constant-KEY shape, zero-delta by construction since no existing call site would be modified).
2. Generalize `LongHorizonGeNumericExecutor` to call `FourDaySessionDistanceAllocationPolicy.Allocate` with the already-supported `keySessionCount∈{0,1}`/`easySupportCount∈{0,1}` pair per §3.2.
3. Widen the ~20 `daysPerWeek`-literal admission/cardinality gates inventoried in `GEN.30 §3.3` (re-confirmed current in §3.1 above) to admit 2D, following the exact narrow-widening-off-`RunLayout` discipline `GEN.29 §6` already demonstrated for Runway.
4. Wire GE→Runway `GlobalWeekNumber` continuity for 2D specifically (the mechanism is already frequency-neutral per `GEN.30 §3.7`; this item is proving/exercising it for 2D, not building it).
5. Real dark end-to-end verification: full 21-52wk×{READY,NOT_READY}×{Beginner,Intermediate} matrix, real PostgreSQL persistence/restart/repair, and a dedicated LongHorizon-filtered zero-delta subset run alongside full regression — the same standard `FREQ.6D.13`-`.19` and `GEN.29` each held themselves to.
6. Recurring-defect-family search (per this phase's own carried-forward instruction) specifically inside whichever of items 1-4 actually gets touched — not yet applicable since nothing was touched this phase.

`NEXT_PHASE_NOT_YET_SCHEDULED`.

---

## 4. Explicit constraints — confirmed respected

- No new product/numeric authority beyond `GEN.11`/`GEN.26`/`GEN.29`/`GEN.30` plus the governing decision message was invented. The one new item this phase resolves (§2) is a content-reference **selection** finding (reuse), not a new number or new structural rule.
- 2D Core and Preparation Runway were not touched — confirmed via `git status --porcelain` showing no production-code, migration, catalog, or test modification this phase (only this report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md`).
- Still dark-only; no public routing/gate change of any kind.
- Zero-delta for every other LongHorizon-active frequency (Intermediate×5D especially) is trivially true this phase by construction — zero executable code was changed — confirmed, not merely assumed, via the full regression run recorded in §5 below.
- `DONE (PARTIAL)` recorded honestly, per the governing prompt's own explicit sanction of this outcome at this scale.

---

## 5. Required verification — both suites re-run, both clean at baseline

Per the mandatory checklist carried forward across this engagement (and independently re-affirmed here since this phase, unlike `GEN.30`, does perform a full regression run rather than skipping it as "nothing executable changed" — done anyway to positively confirm the baseline this phase's own §3.4 next-phase contract will be measured against):

- Confirmed no other `dotnet`/`testhost` process running before starting (`tasklist` clean) and no concurrent invocation; the two suites were run sequentially, never concurrently, per the standing operational discipline.
- `backend/RunningApp.sln` → `RunningApp.IntegrationTests` (Debug, full solution build + test, `gen31_full_regression.trx`): **4273 passed, 3 failed, 4276 total** (35m48s). The 3 failures are the identical, named, pre-existing baseline failures carried since `GEN.17`-`GEN.29` — `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13 and 14, both `InternalServerError` where `OK` expected) and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` (same failure shape). This reconciles exactly to `GEN.29`'s own closing baseline (4273/4276) — zero new failures, zero new passes (no test was added this phase), a byte-for-byte match confirming zero-delta, not merely an absence of regressions in aggregate.
- `plan-catalog/PlanCatalog.sln` → `PlanCatalog.Tests` (Debug, full solution build + test, `gen31_plancatalog.trx`): **1510 passed, 0 failed, 1510 total** (6s) — clean, matching the confirmed baseline exactly. Run explicitly, not skipped, per this engagement's own carried-forward gap warning from `GEN.29`.
- `dotnet build` (Debug): clean, 0 errors across both solutions (both regression runs themselves are full rebuilds; no source changed this phase, confirmed by `git status --porcelain` showing only this report/`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` and the two new `.trx` result files as this phase's additions).

---

## 6. Final classification

**`TWO_D_LONGHORIZON_GE_STRUCTURE_OPTION_A_APPROVED_CONTENT_REUSE_CONFIRMED_IMPLEMENTATION_DEFERRED`** — `DONE (PARTIAL)`.

The GE-segment structural `DOMAIN_DECISION_REQUIRED` `GEN.30 §4` raised is now closed (Option A approved, Option B rejected, §1). The one content question the governing prompt required be resolved before any implementation is resolved as reuse, not fresh-authoring (§2). Step 2 implementation itself — structural selector generalization, admission-gate widening, JIT/persistence/repair wiring, GE→Runway handoff proof, and full dark verification — is honestly not attempted this phase and is scoped concretely for the next phase (§3.4), consistent with `GEN.30`'s own multi-phase estimate for this work and this engagement's own precedent of separating decision/scoping phases from implementation phases.

---

## 7. Governance

`PHASE_LEDGER.md` row appended (`GEN.31`). `MASTER_ROADMAP.md`'s 2D-LongHorizon backlog item (§ Wave A backlog item 1) updated to record Option A's approval, Option B's rejection, and the content-reuse finding, superseding `GEN.30`'s "disclosed, non-binding recommendation" framing with the now-frozen decision. Two-commit self-referential-SHA-backfill pattern followed (implementation+ledger with `PENDING` placeholder, then a second commit backfilling the real SHA). Normal push only — no force, no force-with-lease.
