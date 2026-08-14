# Phase 4M.4B.2B — Core/Runway JIT "Maintain Rejection" Investigation

## 1. Reproduction

Real HTTP chain, TEN_K/Intermediate/4-day, 21-week race: Window 0 fully completed (real ProgressAsPlanned, establishes rich prior anchor ~20km/6.5km) → Window 1 sparse Reduce evidence (1 of 4 sessions completed) → real Reduce anchor (~10km/4km, `min(current, prior)` picking the small current-window value) → Window 2 Maintain evidence (2 of 16 sessions completed) → real HTTP `activate-next-window`.

**Captured via temporary diagnostics** (`Console.Error.WriteLine`, added and removed within this phase — not retained):
1. Source window range: `[6-9]`.
2. Checkpoint runtime outcome: `NextGeWindowActivated`, `reachesGeBoundary=false` (structurally correct — this is a Runway/Core JIT continuation, not a GE-boundary handoff).
3. `needsRunwayEntry=False`, `mayReachCore=True`, `firstPendingWeek=10`.
4. Selected Maintain anchor: `ValidatedLoad=[Weekly=10, LongRun=4]`, `ValidationStatus=Valid` — a real, valid, non-null anchor.
5. `ExactCompletedFrequency=4`.
6. Composition input: built via `LongHorizonRollingCoreGenerationInputAdapter.Build(request.ValidatedLoad, ...)` — this IS `PriorValidatedCheckpointLoad`, correctly plumbed, not a fabricated value.
7. `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync` → `CoreGeneration` stage → `_coreGenerator.GenerateAsync(...)` → `DynamicCoreCalendarMaterializationOrchestrator` → `FourDaySessionDistanceAllocationPolicy.Allocate` throws.
8. Exact exception: `DynamicCoreSessionPrescriptionFailedException`: *"Session prescription failed for candidate 'TEN_K__4D__INTERMEDIATE' v10' at targetWeekCount=12: Week 12 residual volume 5,5km cannot support V1 key/easy minimums."*

## 2. Exact `CoreJitContextUnavailable` predicate

`LongHorizonRollingJitCompositionOrchestrator.MapCompositionFailure` maps BOTH `TenKPreparationRunwayOrchestrationStage.CoreGeneration` and `.AllocationPolicy` failures to `LongHorizonJitReasonCode.CoreJitContextUnavailable` — a coarse umbrella, not itself diagnostic. The real, underlying cause for this reproduction is `FourDaySessionDistanceAllocationPolicy.Allocate` (`RunningApp.Application/RuntimeCatalog/Prescription/Session/FourDaySessionDistanceAllocationPolicy.cs:25`) throwing `CatalogSessionPrescriptionInfeasibleException` — a real, pre-existing, per-session minimum-volume safety check: once the long run's share is subtracted from the weekly target, the residual left for the other 3 sessions (key + 2 easy) must clear a real minimum floor. `CoreJitContextUnavailable` here means, specifically: **the carried numeric anchor is too small, at this particular target Core week, to construct catalog-compliant sessions** — not evidence-absence, not staleness, not an unsupported authority type, not a phase/context-version mismatch.

## 3. Working Reduce vs. failing Maintain — the actual difference

The original 4M.4B.2A hypothesis ("Maintain-carried anchors are rejected while Reduce-carried ones succeed") was disproven by direct A/B reproduction:

| | "Working" case (window0→window1) | Failing case (window1→window2, Maintain) | Retest: Reduce, same boundary |
|---|---|---|---|
| Decision | ProgressAsPlanned | Maintain | Reduce (forced, same window position) |
| `needsRunwayEntry` | `True` (first Runway entry) | `False` | `False` |
| `mayReachCore` | `False` | `True` | `True` |
| Target Core week | none (Runway structure only) | week 12 | week 1 (own residual check) |
| Anchor | `[20, 6.5]` (rich, unreduced) | `[10, 4]` (Reduce-shrunk) | `[6.5, 2.5]` (Reduce-shrunk, different window) |
| Result | Success | `CoreJitContextUnavailable` | **`CoreJitContextUnavailable`, identical failure class** |

The first semantic difference is **not** Maintain vs. Reduce at all — it's `needsRunwayEntry`/`mayReachCore`, i.e. *which real Core week the carried anchor must support*, combined with *how small the carried anchor is*. The "working Reduce" comparison from 4M.4B.2A was actually comparing a Runway-entry transition (low minimums, any anchor) against a later Core-reaching transition (higher minimums, small anchor) — an apples-to-oranges comparison, not a Maintain-specific asymmetry. Retesting with a forced Reduce decision at the *same* later boundary, carrying an equally small anchor, reproduces the *identical* `CoreJitContextUnavailable`/residual-volume failure. Separately, a real Maintain activation *carrying the rich, unreduced anchor* (see §11) succeeds cleanly.

## 4. Root cause

**There is no technical/plumbing defect.** `PriorValidatedCheckpointLoad` is already a correctly-accepted, correctly-plumbed authority in Core/Runway JIT composition — it flows through the exact same `LongHorizonRollingCoreGenerationInputAdapter.Build(request.ValidatedLoad, ...)` call regardless of which Rev4 branch produced it. The real cause is a genuine, symmetric, real numeric infeasibility: `FourDaySessionDistanceAllocationPolicy`'s per-session minimum-volume floor rejects any sufficiently small carried anchor, from any source, once it must construct sessions for a sufficiently demanding target week. The specific case that reliably triggers this is: Reduce (which, by definition — `EffectiveCompletedCount <= 1` — must select a small anchor) landing exactly on this pilot's Runway→Core boundary, followed by Maintain (the only decision that propagates a value forward *without* re-aggregating fresh evidence — Reduce and ProgressAsPlanned both re-derive from the currently-checkpointed window's own evidence).

## 5. Defect classification

**Not a defect.** This is real, correct, pre-existing catalog behavior (a genuine per-session minimum-volume safety rule) operating exactly as designed, on a numerically small anchor that a real Reduce decision legitimately produced. Classified as a product-level **DecisionRequired** item (§20), not a runtime/plumbing bug.

## 6. Files inspected

`LongHorizonRollingJitCompositionOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrationContracts.cs`, `LongHorizonRollingJitCompositionContracts.cs`, `LongHorizonRollingCoreGenerationInputAdapter` (via call site), `FourDaySessionDistanceAllocationPolicy.cs`, `LongHorizonGeNumericExecutor.cs`, `LongHorizonGeStructuralSelector.cs`.

## 7. Files changed

None retained from the investigation itself. Two temporary `Console.Error.WriteLine` diagnostics were added to `LongHorizonRollingJitCompositionOrchestrator.cs` to capture the A/B comparison in §3, then removed once root-caused. New permanent test files: see §16.

## 8. Exact fix

None applied — none needed. Per this phase's explicit standard ("if the only way to make Maintain pass is to weaken a product/domain rule whose intent is unclear, STOP and report DecisionRequired"), no change was made to `FourDaySessionDistanceAllocationPolicy`, `TenKPreparationRunwayDarkOrchestrator`, or any Rev4 anchor-selection code.

## 9. Evidence-authority handling

Confirmed correct and already-shared: `PriorValidatedCheckpointLoad` (Maintain's anchor) and the Reduce-selected anchor both flow through the identical `request.ValidatedLoad` → `LongHorizonRollingCoreGenerationInputAdapter.Build` → `TenKPreparationRunwayCoreGenerationRequest` → `_coreGenerator.GenerateAsync` path. There is no separate "fresh evidence only" gate for Core generation and no missing authority — the pipeline does not distinguish "current-window evidence" from "carried Maintain anchor" at all, by design (per its own architecture, both are just `ValidatedSustainableLoad`).

## 10. GE-boundary finding

The failing Maintain path does **not** depend on the first GE→Runway boundary specifically — it depends on reaching *any* sufficiently demanding target week (here, Core week 12) with a sufficiently small carried anchor. A Maintain (or Reduce) transition landing on an *early*, low-minimum boundary (Runway entry, or an early Core week) succeeds regardless of anchor size, as long as the anchor isn't below that week's own (lower) floor. This pilot's General Endurance-fully-consumed-by-window-1 characteristic (documented in 4M.4B.2A) is a separate, real finding, unrelated to this one except that it's what pushes real checkpoints into Runway/Core JIT composition so early in the plan's lifecycle.

## 11. Maintain real activation proof

`LongHorizonThreeWindowAnchorThreadingE2ETests.RealMaintainActivation_UsesPriorValidatedCheckpointLoadVerbatim_GenuinelyAdvancesWindow` (passing): Window 0 fully completed (rich anchor) → Window 1 evidenced to Maintain (exactly 2 completed, 1 a LONG_RUN) → real HTTP activation succeeds, `"next_window_load_decision":"maintain"`, window genuinely advances, fresh-DB read confirms Window 2's materialized weekly total matches Window 0's held (not freshly grown) level, all Window 2 sessions `Planned` (no stale reuse), exactly one new activation record.

## 12. Maintain ≤ ProgressAsPlanned invariant

**Built and run — genuine, systematic violations found.** `MaintainNotExceedingProgressAsPlannedInvariantTests.MaintainAnchor_NeverExceedsProgressAsPlannedAnchor_AcrossRandomizedRealCatalogProgression`: 200 randomized cases (weekly volume 5–60km, long-run share 20–50%, 3–6 runs/week, 1–32 GE weeks, both readiness profiles), executed against the real `LongHorizonGeNumericExecutor.Execute` (the actual catalog progression step, not a reimplementation). 183/200 cases produced a valid, feasible result (17 hit the same real per-session minimum-volume floor from §2 and were skipped, not silently discarded). Of those 183: **94 (51%) violated the strict `Maintain <= ProgressAsPlanned` invariant**, by a small, consistent margin (0.02–0.55km, typically ≤1.5% of the input). Root cause: the catalog's real session-distance allocation rounds/snaps individual session distances to its own discrete grid, and the sum of those rounded session distances for week 1 can land slightly *below* the raw baseline input even though week 1 performs no intentional reduction. Examples: `weekly=42.25 → progress=42.00`, `weekly=44.54 → progress=44.50`, `weekly=11.64 → progress=11.50`. Per explicit instruction, **not clamped** — reported as found. **DecisionRequired** (§20).

## 13. 3-window chain

`LongHorizonThreeWindowAnchorThreadingE2ETests` (3/3 passing): (a) `RealMaintainActivation_...` — Maintain succeeds and threads correctly (§11); (b) `RealReduceActivation_...` — Reduce succeeds and threads correctly, unaffected by any of this phase's findings (never reaches Core generation at all); (c) `RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_...` — the specific Reduce→Maintain ordering that lands exactly on the Runway→Core boundary, asserting the real, disclosed `CoreJitContextUnavailable` Block (§4) and the critical no-false-advancement invariant from Phase 4M.4B.2A. The full literal `Reduce → Maintain → ProgressAsPlanned` chain in one continuous run was not constructed as a single passing test, because (a) and (c) above are mutually exclusive outcomes for the same Reduce→Maintain transition on this pilot's fixed roadmap (Reduce inherently produces a small anchor; whether the *next* transition succeeds depends on which Core week it must support, which is fixed by window position, not evidence choice) — this is disclosed, not hidden.

## 14. Fresh DB proof

All of §11–13 assert against fresh `AppDbContext` scopes created after each HTTP call returns.

## 15. Block-routing regression

`RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_...` directly re-proves the Phase 4M.4B.2A invariant: the genuine Block here (`CoreJitContextUnavailable`) is asserted to leave `CurrentWindowStartWeek/EndWeek` unchanged and to add zero new `LongHorizonActivationWindowRecords` rows — it does not regress to a false "activated" response.

## 16. Tests added

- `LongHorizonThreeWindowAnchorThreadingE2ETests.cs` — rewritten (3 tests, replacing the prior single test): `RealMaintainActivation_UsesPriorValidatedCheckpointLoadVerbatim_GenuinelyAdvancesWindow`, `RealReduceActivation_ThreadsAnchorCorrectly_GenuinelyAdvancesWindow`, `RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement`.
- `MaintainNotExceedingProgressAsPlannedInvariantTests.cs` — new, 200-case real-catalog-progression sweep (§12), currently failing honestly (94/183 violations), not clamped.
- Two temporary diagnostic-only test methods used during investigation were removed before finalizing.

## 17. Exact commands/results

```
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests"
  → 3/3 passed

dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~MaintainNotExceedingProgressAsPlannedInvariantTests"
  → 0/1 passed (1 failed -- intentional, documents the real §12 finding, not clamped)

dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonFirstCheckpointNumericAnchorTests|FullyQualifiedName~LongHorizonNumericAnchorMaterializationE2ETests|FullyQualifiedName~ScheduleRepairRuntimeOrchestratorTests|FullyQualifiedName~RuntimeNotTodayReasonMapperTests|FullyQualifiedName~ScheduleRepairSupersededAndReadCorrectnessTests|FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests|FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests|FullyQualifiedName~NextWindowNumericAnchorSelectorTests|FullyQualifiedName~LongHorizonThreeWindowAnchorThreadingE2ETests"
  → 79/79 passed
```

## 18. LongHorizon regression

`dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizon"` → **1094/1095 passed, 1 failed** (the intentional §12 invariant test).

## 19. Full backend regression

`dotnet test RunningApp.sln` — see final chat report for the completed run's exact count (run in background at doc-authoring time).

## 20. Remaining DecisionRequired

1. **`Maintain ≤ ProgressAsPlanned` does not strictly hold** — a real, systematic (51% of valid sampled cases), small-magnitude (≤1.5%, typically 0.25–0.55km) violation caused by the catalog's own real session-distance rounding at week 1. This is a genuine product question: is a sub-1.5% rounding-induced violation of this invariant acceptable (in which case the invariant should be documented as "approximately, not strictly" true, or compared with a tolerance), or does it require a change to session-distance rounding/allocation? Not decided here — no rounding behavior was changed.
2. **The literal Reduce→Maintain→ProgressAsPlanned chain, landing at this pilot's Runway→Core boundary, cannot both succeed and use a genuine Reduce-shrunk anchor** — Reduce inherently produces a small anchor; Maintain inherently propagates it unchanged; if that boundary happens to require more volume than the anchor supports, it Blocks, correctly. Whether product wants Reduce's severity to be bounded, or Maintain to have some floor/refresh behavior near this boundary, is a genuine Rev4-level policy question, not decided here (explicitly out of scope: "do not modify Maintain/Reduce formula").
3. This pilot's GE-phase-consumed-by-window-1 characteristic (Phase 4M.4B.2A finding) remains the reason real checkpoints reach Runway/Core JIT composition as early as they do; not redesigned here per explicit instruction.

## 21. Final classification

```
ADAPTATION_V1_MAINTAIN_CORE_JIT_POLICY_REMAINS_DECISION_REQUIRED
```

No technical/runtime defect was found in Core/Runway JIT composition's handling of Maintain's carried anchor — it is already correctly plumbed and demonstrably succeeds with a real HTTP activation when the anchor is large enough for its target week (§11). What blocks full closure is a genuine, real, well-evidenced product-numeric question: the catalog's own minimum-session-volume floor can legitimately reject a small Reduce-selected anchor at a demanding target week (§4/§13), and the catalog's own session-distance rounding causes a small, systematic violation of the strict Maintain ≤ ProgressAsPlanned invariant (§12). Neither is a bug in this phase's scope to fix; both are disclosed, not hidden, with exact real numbers.

No code committed, no push, Phase 4M.5 not started.

## 22. Addendum — Phase 4M.4B.2C closure

Both DecisionRequired items from §20 were frozen as canonical V1 policy in **Revision 4.1** (`appsel-adaptation-v1-canonical-spec — Revision 4.1.md`), §7 (ROUNDING PRODUCT DEFAULT, TARGET PRESCRIPTION INFEASIBILITY):

1. **Rounding tolerance**: Maintain must not *materially* exceed ProgressAsPlanned, where "material" = relative deviation > 1.5%. The observed maximum (1.36%) is within this frozen tolerance — 0 cases exceeded it. No clamp, no runtime constant; the tolerance lives only in the governance/test acceptance layer (`MaintainNotExceedingProgressAsPlannedInvariantTests`).
2. **Target-week infeasibility**: confirmed to already be the exact real runtime behavior (§F implementation audit in Phase 4M.4B.2C — no production code changed). A too-small anchor (Maintain or Reduce) correctly and symmetrically Blocks via the existing typed `LONG_HORIZON_CONTINUATION_BLOCKED` path; no upward clamp exists or was added.

The suspected "Maintain plumbing bug" this document investigated (§1–§11) remains disproven, exactly as found here — Rev4.1 did not change that finding, only formally closed the two open product questions it left behind (§20). See `PHASE4M_4B_2C_ROUNDING_TOLERANCE_AND_INFEASIBILITY_CLOSURE.md` for the full closure report.
