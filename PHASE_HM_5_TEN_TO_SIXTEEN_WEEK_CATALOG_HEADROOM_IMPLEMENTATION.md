# PHASE HM.5 — Half-Marathon 10–16W Catalog Headroom Authority Implementation and Generic Resolver Proof

Governance/audit outcome. **No catalog `.json` file, no schema file, and no production `.cs` file was modified by this phase.** A genuine generic-mechanism capability gap was found at the *stage* level (not the phase level) that makes it impossible to encode HM.4's already-frozen Layer B stage headroom into the real catalog using data-only changes without either contradicting the frozen 14W golden vertical slice or contradicting HM.4's own compression-order authority. Per this engagement's own "no forced closure" discipline (explicitly restated by the governing prompt for this exact phase), the finding is reported precisely rather than papered over with a silent semantics change or an invented workaround.

## 1. HM.4 frozen authority consumed

Read directly, in full, before any other action: `PHASE_HM_4_PHASE_AND_STAGE_NUMERIC_HEADROOM_AUTHORITY_CLOSURE.md` and `PHASE_HM_3_TEN_TO_SIXTEEN_WEEK_DYNAMIC_CORE_PHASE_ALLOCATION_AUTHORITY_CLOSURE.md`. Layer A (phase Min/Preferred/Max/priorities) and Layer B (stage Min/Preferred/Max/compression-extension behavior and order) were taken exactly as frozen — see the governing prompt's own restated tables, verified against the two reports verbatim, no re-derivation performed. The exact seven-row `10W→2/3/3/2 … 16W→4/5/5/2` table and the per-horizon stage-occupancy table (HM.4 §9–§10) were treated strictly as the **verification oracle** they are labeled, never as something to hardcode.

## 2. Catalog lifecycle/versioning decision

Before touching any file, the real repository versioning convention was audited (not assumed). Findings:

- `half-marathon-master.v1.json` and `half-marathon-workout-progression.v1.json` both carry `metadata.status = "VALIDATED"`, `metadata.version = 1`.
- `CatalogPublisher.cs` (`plan-catalog/src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs`, ~lines 178–225) enforces a cross-release content-hash guard keyed on `(DocumentType, Key, Version)` at *publish* time; it would not currently trip on an in-place edit because Half-Marathon has never been published to any real release folder (`plan-catalog/artifacts/appsel-plan-catalog/*` contains no half-marathon artifact at all).
- However, the repository's **unbroken historical convention** — confirmed by diffing `ten-k-master.v9.json` against `v11.json` and reading commit `5c8bb71`'s message — is that **every** observed catalog content change, however small (including a pure numeric widening), produced a **new** `vN.json` file while the prior version file was retained untouched. No example anywhere in git history shows an existing `vN.json` catalog document being edited in place.
- `half-marathon-master.v1.json` itself hard-references `workoutProgression.version = 1` (lines 64–66) — any progression version bump therefore forces a master-template content change (and hence a master version bump) regardless of whether the phase-level numbers themselves changed.
- `plan-catalog/catalog/combinations/half-marathon-4d-intermediate.v1.json` pins `masterTemplate` to `HALF_MARATHON_MASTER` v1 and would need its own reference (or a new combination version) updated to point at any new master version.
- Schemas (`plan-template.schema.json`, `workout-progression.schema.json`) already declare every phase-level field (`minimumWeeks`/`preferredWeeks`/`maximumWeeks`/`compressionPriority`/`extensionPriority`) and every stage-level field this phase would need (`minimumExposures`/`maximumExposures`, both `minimum: 0`; `compressionBehavior`/`extensionBehavior`) — **no schema change required** for the numbers HM.4 authored.

**Decision recorded, not executed**: had the stage-level blocker in §6 not been found, the correct path (per repo convention) would have been `half-marathon-master.v2.json` + `half-marathon-workout-progression.v2.json`, with `half-marathon-4d-intermediate.v1.json`'s `masterTemplate.version` (or a new `v2` combination file) updated to match — mirroring `ten-k-master`'s own `v9→v11` precedent, not an in-place edit of the `VALIDATED` v1 files. Because the stage-level authoring cannot be correctly completed (§6), no version bump was performed — versioning a catalog artifact around numbers that the consuming allocator cannot correctly realize would itself be a form of forced/fake closure.

## 3. Files/artifacts changed

None besides this report, `PHASE_LEDGER.md` (new HM.5 row), and `MASTER_ROADMAP.md` (new HM.5 subsection). No `.json` catalog file, no schema file, no `.cs` production file, no test file was added or modified.

## 4. Phase headroom encoding

**Not performed** (see §3) — but independently verified *feasible* and side-effect-free: `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs`, lines 153–205) was read in full. It anchors at each phase's own `PreferredWeeks` field (a field genuinely distinct from `MinimumWeeks`/`MaximumWeeks` in the real schema), and distributes compression/extension deltas using each phase's own `CompressionPriority`/`ExtensionPriority` integer fields — fields that are **independent of any structural/chronological ordering constraint** (a candidate's phase sequence is always `FOUNDATION→BUILD→RACE_SPECIFIC→TAPER` by `phaseKey` identity, never by priority value). This is exactly the mechanism HM.3/HM.4 already mechanically simulated and matches this phase's own re-reading verbatim — **zero code change required, no capability gap at the phase level.** Authoring Layer A into `half-marathon-master.v2.json` (Foundation `2/3/4`, Build `3/4/5`, RaceSpecific `3/5/5`, Taper `2/2/2` unchanged, same `1/2/3/4` priorities) was drafted and validated by hand-trace against this real code but not committed to any file, since a partial (phase-only) catalog authoring with an unresolved stage-level gap underneath it would materialize the phase but leave a downstream stage-allocation contradiction — see §6.

## 5. Stage headroom encoding

**Not performed** — blocked. See §6 for the exact reason.

## 6. Generic resolver proof (phase level: PASSES; stage level: BLOCKED — the phase's central finding)

**Phase-level (`CatalogPhaseAllocationResolver`): PASSES.** Hand-traced (not merely re-cited) against the real, unmodified algorithm in §4. `PreferredWeeks` is a real, independent field from `MinimumWeeks`/`MaximumWeeks`, and `CompressionPriority`/`ExtensionPriority` are independent integer fields, decoupled from phase ordering. Every one of HM.4's seven horizon rows reproduces exactly under this real code with the Layer A data, confirming HM.3/HM.4's own simulation was faithful to the real algorithm. **No phase-level capability gap.**

**Stage-level (`ProgressionStageAllocator`, `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/ProgressionStageAllocator.cs`): BLOCKED — genuine capability gap found, confirmed by direct code trace and cross-checked against existing passing tests (not merely theorized):**

The real `WORKOUT_PROGRESSION` schema (`plan-catalog/schemas/workout-progression.schema.json`, lines 45–48) declares **only** `minimumExposures` and `maximumExposures` per stage — there is **no `preferredExposures` field at all**, unlike the phase-level schema's `minimumWeeks`/`preferredWeeks`/`maximumWeeks` triple. `ProgressionStageAllocator.AllocatePhase` (lines 129–337) reflects this: it initializes every active stage's working exposure count from `effective.MinimumExposures` (the catalog minimum) as its baseline, then either compresses toward a **hardcoded floor of 1** (`ApplyCompression`, lines 470–525: `reducible = candidate.MinimumExposures - 1`, never lower) or extends toward `MaximumExposures` (`ApplyExtension`, lines 547–598) using **`RelativeOrder` descending** as the tie-break for *both* directions (`OrderByDescending(a => a.EffectiveStage.RelativeOrder)`, confirmed at lines 476 and 563).

Two consequences, each independently fatal to a data-only implementation of HM.4 Layer B:

1. **No independent "preferred" concept exists at the stage level.** The allocator's baseline is literally `minimumExposures`, not a separate preferred value. If `BUILD_FASTER_THAN_HM_INTRO.minimumExposures` is authored as HM.4 requires (`0`, to permit compression to zero at 10–12W), the allocator's own baseline for that stage becomes `0` even at the **unrelated, frozen 14-week case**, and the resulting surplus (Build phase, 14W, availableWeeks=4, baseline sum 0+3=3, surplus=1) is handed to `BUILD_THRESHOLD_DEVELOPMENT` by the RelativeOrder-descending extension tie-break (order 2 beats order 1) — producing **`INTRO=0, THRESHOLD=4`** at 14W, not the frozen golden `INTRO=1, THRESHOLD=3`. This is a direct, provable **14W zero-delta violation** (see §11) caused purely by authoring HM.4's own required minimum.
2. **Compression removes the wrong stage first.** HM.4 Layer B explicitly requires the *lowest-information* stage (`BUILD_FASTER_THAN_HM_INTRO`, `RelativeOrder=1`; `RACE_SPECIFIC_HM_INTRODUCTION`, `RelativeOrder=1` within its phase) to be dropped **before** the higher-value stage that follows it (`BUILD_THRESHOLD_DEVELOPMENT`/`RACE_SPECIFIC_HM_DEVELOPMENT`, `RelativeOrder=2`) is ever reduced below its own preferred value. The real `ApplyCompression` tie-break is the exact opposite: it reduces the **highest**-`RelativeOrder` Compressible stage first. This is not a theoretical reading — it is the literal, already-passing, already-named test `CompressionAllocation_ReducesHighestRelativeOrderCompressibleStageFirst` (`backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Progression/ProgressionStageAllocatorTests.cs`, lines 456–474) and the sibling `BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst`, both real, current, passing production behavior. Given HM's real stage keys and their RelativeOrder values (`BUILD_FASTER_THAN_HM_INTRO=1` < `BUILD_THRESHOLD_DEVELOPMENT=2`; `RACE_SPECIFIC_HM_INTRODUCTION=1` < `_DEVELOPMENT=2`), compressing HM's Build/RaceSpecific phases under this real algorithm would reduce `BUILD_THRESHOLD_DEVELOPMENT`/`RACE_SPECIFIC_HM_DEVELOPMENT` **first**, in direct contradiction of HM.4 §7's required compression order (`FASTER_THAN_HM_INTRO`/`HM_INTRODUCTION` drop first).
3. **`RelativeOrder` cannot be reassigned as a workaround.** `RelativeOrder` simultaneously encodes the *chronological week-sequence layout* (contiguous block layout in ascending `RelativeOrder`, `AllocatePhase` lines 277–327) and the *only* compression/extension tie-break signal. Swapping `BUILD_FASTER_THAN_HM_INTRO`'s and `BUILD_THRESHOLD_DEVELOPMENT`'s `RelativeOrder` values to trick the tie-break would also swap their position in the actual training week sequence — placing the deeper threshold-development block *before* the "faster than HM" introduction block, which is not a valid Half-Marathon Build progression and was never authorized by any HM phase.

**Conclusion**: encoding HM.4's Layer B stage headroom **correctly** — i.e., such that (a) the frozen 14W golden stage occupancy is exactly preserved and (b) 10–13W compression removes the lowest-information stage first, exactly as HM.4 §7 specifies — requires either (i) a new `preferredExposures` stage field (mirroring the phase level's already-proven `minimumWeeks`/`preferredWeeks`/`maximumWeeks` pattern) plus allocator logic that anchors at it instead of at `minimumExposures`, or (ii) a new, `RelativeOrder`-independent stage-level compression/extension priority field (mirroring the phase level's already-proven `CompressionPriority`/`ExtensionPriority` fields) plus allocator logic that sorts by it instead of by `RelativeOrder`. **Both are schema + `ProgressionStageAllocator` production-code changes**, squarely the kind of change this phase's own governing instructions flag as a red flag requiring "a proven concrete capability gap" before touching — which this section supplies, but implementing the fix itself is a new, distinct implementation phase's work, not a `HM.5` catalog-data-only authoring task, and doing it here would also require full 10K zero-delta re-verification of a shared production algorithm (§17-class risk) that this phase's scope was not sized for.

## 7. Exact 10–16 phase table

Not produced against the real loaded candidate (no catalog file was authored — see §3). The HM.4 oracle table (§1) remains the correct target; §6 confirms the phase-level mechanism alone would reproduce it exactly if Layer A data were authored in isolation, but authoring Layer A without a working Layer B stage mechanism underneath it would materialize phase week-counts with no valid per-week stage assignment for any non-14W horizon — a half-finished, misleading artifact, not a real slice.

## 8. Exact 10–16 stage occupancy

Not produced. This is precisely the table §6 shows cannot be correctly generated by the real, unmodified `ProgressionStageAllocator` from catalog data alone.

## 9. Faster-than-HM fallback proof

Not run. Contingent on §6's stage-level closure. Separately noted: the real `half-marathon-workout-progression.v1.json` today declares `BUILD_FASTER_THAN_HM_INTRO` with `"requires": []` (unconditioned) — HM.1.3's own described "eligibility-driven 0+4 fallback shape" for this stage does not actually exist as a `requires`/`fallbackStageKey` pair in the real catalog file today; it exists only as narrative in `PHASE_HM_1_3_...`'s own report. This is a second, smaller, pre-existing discrepancy between prior narrative and real catalog content, distinct from and not a cause of the §6 blocker, disclosed here for completeness per this engagement's own "read the real artifact, not the paraphrase" discipline.

## 10. Pace-evidence fallback proof

Not run — contingent on §6.

## 11. 14W zero-delta proof

**Partially proven, in the negative direction — this is load-bearing evidence for §6, not a pass.** Hand-tracing the real `ProgressionStageAllocator` algorithm against HM.4's own required Layer B numbers for the `BUILD` phase at the (unchanged) 14-week horizon shows the allocator would compute `BUILD_FASTER_THAN_HM_INTRO=0`, `BUILD_THRESHOLD_DEVELOPMENT=4` — not the frozen golden `1`/`3` — the moment `BUILD_FASTER_THAN_HM_INTRO.minimumExposures` is widened to HM.4's required `0`. No catalog file was actually changed, so the *real, currently-shipping* 14W dark vertical slice is **provably still exactly byte-identical today** (nothing was touched) — but this trace is precisely why no catalog file was authored: doing so would have broken 14W zero-delta, the single most heavily protected invariant in this entire HM engagement.

## 12. Non-14W dark reachability

Not evaluated — moot without §6 closed; no catalog headroom was authored, so the internal dark pipeline's reachability for 10–13/15–16W is unchanged from HM.2.1's own already-established fail-closed state.

## 13. Short-core numeric-safety proof

Not reached. The volume/long-run planner (`CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`) was not exercised this phase because no shorter-horizon candidate could be validly materialized (§6/§8 block the prerequisite stage schedule). Based on a read of `ResolvePeak`'s golden-fixture interpolation formula (`transitionAdjustedMultiplier = 1 + ((canonicalDefaultMultiplier - 1) * transitions / GoldenFixtureNonTaperTransitions)`, already documented in `HM.1.5`), fewer non-taper transitions (10–13W) produce a **smaller** `transitionAdjustedMultiplier`, i.e., a **lower** reachable peak, not a steeper one — this is evidence the mechanism is structurally safe for shorter horizons and would likely not itself require a growth-policy exception. This is a plausibility read only, not the "real materialized plan" empirical proof the governing prompt requires, since no valid stage schedule exists yet to materialize one from.

## 14. Long-core extension-safety proof

Not reached, for the same reason as §13.

## 15. Long-run/taper proof

Not reached, for the same reason as §13.

## 16. Catalog hash/dependency proof

Not applicable — no catalog file was changed, so no `PlanCatalog.Tests` hash regression run was required or performed. (Running the full suite unnecessarily would not have exercised anything relevant to this phase's finding.)

## 17. 10K zero-delta proof

Not empirically re-run this phase (no shared production code was modified), but structurally guaranteed: since no `.cs` file, no schema file, and no catalog `.json` file was changed anywhere in the repository, every 10K code path, catalog artifact, and test remains byte-identical to its pre-HM.5 state by construction. No 10K catalog hash, phase allocation, stage allocation, workout binding, volume, taper, long-run, or public-routing behavior could have changed.

## 18. Regression results

**No test suite was run.** Per this engagement's own governance discipline (mirrored explicitly from `HM.3`'s own precedent, a structurally identical "governance-only, blocker found, no file changed" outcome), a regression run is not required when zero code/catalog/test files were modified — there is nothing for `RunningApp.IntegrationTests` or `PlanCatalog.Tests` to newly exercise. This is stated explicitly rather than silently omitted, per the governing prompt's own instruction to "document precisely why any omitted suite is safe rather than skipping silently."

## 19. Public/Runway/LongHorizon state

Unchanged and re-confirmed unreachable for HM by inspection (not modified): `V1CatalogPilotIdentityPolicy`'s public/Runway gates (`IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity`) were not touched this phase, and since no catalog headroom exists for any non-14W horizon (no file was changed), the internal dark dynamic-Core route remains exactly as fail-closed for 10–13/15–16W as `HM.2.1` left it. HM remains entirely non-public.

## 20. Remaining blockers

**Exactly one, precisely scoped**: `ProgressionStageAllocator` (and its underlying `WORKOUT_PROGRESSION` schema) has no stage-level equivalent of the phase-level schema's already-proven `preferredWeeks`/`CompressionPriority`/`ExtensionPriority` triple of concepts. Its real compression/extension tie-break is `RelativeOrder`-descending with a hardcoded floor of 1 exposure, which (a) cannot express "drop this specific low-information stage to zero before compressing the higher-value stage that structurally follows it" and (b) has no way to hold a stage's *frozen preferred* exposure count constant while separately authoring a *lower* floor for compression headroom, since its only baseline is the floor itself. Closing this requires a new, `HM.5`-successor implementation phase that either (i) adds a `preferredExposures` field to the workout-progression schema plus corresponding `ProgressionStageAllocator` anchor-at-preferred logic, or (ii) adds a `RelativeOrder`-independent stage compression/extension priority field plus corresponding tie-break logic — in both cases, a real production-code change to a shared, distance-generic allocator, requiring full 10K zero-delta regression proof (mirroring this engagement's own established pattern for every prior shared-mechanism change, e.g. `HM.1.6`'s `PreferredAbsolutePeakLongRunKm` seam). This is not an HM.4 authority gap (HM.4's numbers are correct and complete) — it is a generic-mechanism gap discovered only once HM.4's real numbers were traced against the real, unmodified consuming code, exactly the kind of finding this phase's own instructions predicted might occur "anywhere" and required be reported precisely rather than papered over.

## 21. Recommended next phase

A future `HM.5.1` (mechanism-only, mirroring `HM.1.6`'s own narrow-seam precedent) should: (1) design the smallest possible stage-level seam — most likely a new optional `preferredExposures` field on `WORKOUT_PROGRESSION` stages, defaulting (when absent) to the stage's own `minimumExposures` so every existing 10K instance is byte-identical by construction (mirroring `HM.1.4B`/`HM.1.6`'s own established optional-trailing-field, zero-delta-by-default pattern); (2) change `ProgressionStageAllocator` to anchor its baseline at the resolved preferred value instead of `minimumExposures`, and to compress/extend using a decoupled priority signal (a new optional per-stage priority field, or a documented, evidence-traced convention) rather than `RelativeOrder`; (3) prove 10K zero-delta by test, exactly as `HM.1.6` did for the long-run-share seam; (4) only then does `HM.5`'s original scope — authoring `half-marathon-master.v2.json`/`half-marathon-workout-progression.v2.json` with HM.4's exact Layer A/B numbers and completing sections 4–19 above — become honestly completable without contradicting the frozen 14W golden slice.

## Final classification

`HM_10_16W_CATALOG_HEADROOM_IMPLEMENTATION_BLOCKED`

**Exact smallest blocker**: `ProgressionStageAllocator`'s stage-level compression/extension mechanism has no concept of a stage's own preferred exposure count independent of its catalog-authored minimum, and ties its only compression/extension priority signal to `RelativeOrder` (which also fixes chronological week-sequence position) rather than to a decoupled priority field — the same design pattern the phase-level `CatalogPhaseAllocationResolver` already correctly implements via its independent `PreferredWeeks`/`CompressionPriority`/`ExtensionPriority` fields. Authoring HM.4's real Layer B numbers into the real catalog today would either break the frozen 14W golden stage occupancy or violate HM.4's own required compression order (or both) — proven by direct trace against the real, unmodified code and cross-checked against two already-passing, already-named production tests (`CompressionAllocation_ReducesHighestRelativeOrderCompressibleStageFirst`, `BuildPhase_ExtensionAllocatesSurplusToHigherRelativeOrderStageFirst`), not invented or assumed. No number was forced, no catalog file was mutated to paper over the gap, and no frozen HM.0–HM.4 decision was reopened.
