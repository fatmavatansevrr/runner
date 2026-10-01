# PHASE DERIVED-DIST.1 — Custom 10K→Half-Marathon Derived-Distance V1 Guardrails, Production Migration Decision, Existing Projected-Cell Retirement Plan and Public Eligibility Design

**Classification: `CUSTOM_10K_TO_HM_DERIVED_DISTANCE_V1_MIGRATION_AND_PUBLIC_MODEL_CLOSED`**

GOVERNANCE / PRODUCT-SCOPE / MIGRATION-DESIGN PHASE. Zero production, catalog, database, API, or frontend change. This phase consumes DERIVED-DIST.0's proved architecture and decides the V1 production policy around it.

---

## 1. DERIVED-DIST.0 consumed

Read in full: `PHASE_DERIVED_DIST_0_NEAREST_HIGHER_CANONICAL_PLAN_PROJECTION_PROTOTYPE.md` (all 274 lines/60 sections). Its architecture result — `CUSTOM_10K_TO_HM_TARGETS_CAN_BE_DERIVED_WITHOUT_EXACT_DISTANCE_AUTHORITY_CELLS` — and safety result — `DERIVED_PLAN_PREFIX_TAPER_REBINDING_PRESERVES_EXISTING_SAFETY_INVARIANTS` — are **not re-litigated**. This phase treats both as closed, settled fact, and resolves only the product/migration decisions DERIVED-DIST.0 §59/§60 explicitly deferred.

Also read for exact-cell compatibility context: `PHASE_DIST_GEN_0`, `PHASE_DIST_GEN_13`, `PHASE_DIST_GEN_16`, `PHASE_DIST_GEN_17`, `PHASE_DIST_GEN_19A`, `PHASE_DIST_GEN_20`. Source read directly (current repository truth, not report prose): `NearestHigherCanonicalPlanResolver.cs`, `DerivedDistanceEligibilityPolicy.cs`, `DerivedDistanceHorizonAuthority.cs`, `DerivedDistanceArcSelector.cs`, `DerivedDistanceTargetFieldAudit.cs`, `Dark16KPilotEligibilityPolicy.cs`, `ProjectedTargetAuthorityRegistry.cs`, `V1CatalogPilotIdentityPolicy.cs` (the HALF_MARATHON public `IsSupportedLevelFrequency` allow-list, in full), and the real test file `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests.cs` (all 692 lines) for actual trace/assertion data rather than report paraphrase.

## 2. Repository baseline

Pre-phase HEAD `b06d4fe` ("docs(derived-dist-0): backfill governance commit SHA for DERIVED-DIST.0"), confirmed by `git log`. `git fetch origin main` + `git rev-list --left-right --count HEAD...origin/main` → `0 0`, confirmed before any change. Known pre-existing uncommitted noise (`baseline_tmp`, `plan-catalog/artifacts/audits/*` regenerated timestamps, untracked `.distgen13/`, bin/obj churn) left untouched, not staged.

## 3. Current architecture state

Confirmed by direct grep that the five DERIVED-DIST.0 production types (`NearestHigherCanonicalPlanResolver`, `DerivedDistanceEligibilityPolicy`, `DerivedDistanceHorizonAuthority`, `DerivedDistanceArcSelector`, `DerivedDistanceTargetFieldAudit`) are referenced from exactly 8 files total: the 5 files themselves plus the 3 widened production seams (`CatalogPrescriptionContextBuilder.cs`, `CatalogPreviewGenerator.cs`, `PlanServices.cs`). No read path, mutation path, Home/Calendar/TrainingDay/Complete/Cancel handler, or persistence-layer file references any of them — the whole mechanism is isolated to plan-generation time, never plan-operation time. This is direct, current-repository confirmation (not inference from the DERIVED-DIST.0 report alone) supporting §33/§62 below.

`DerivedDistanceEligibilityPolicy` is Intermediate-only, 3/4/5 runs/week only (hardcoded `EligibleLevels`/`EligibleRunsPerWeek` lists). `DerivedDistanceHorizonAuthority` reuses the exact 10/12/14 triple, declared `MinimumCoreWeeks = RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks` (structural, not invented) and `PreferredCoreWeeks=12`/`MaximumCoreWeeks=14` (convergent-reused, per its own doc comment's disclosed rationale). Neither type is reachable from any public DTO today — `TargetDistanceKmOverride` is never set by `GeneratePreviewCommandMapper`.

## 4. Current public/dark state

`Dark16KPilotEligibilityPolicy.ApprovedDarkCells` (5 entries, unchanged): 15K×I×4D, 16K×I×4D, 18K×I×4D, 16K×I×3D, 16K×I×5D. `ApprovedPublicCells` (4 entries, unchanged): 15K×I×4D, 16K×I×4D, 18K×I×4D, 16K×I×3D. **16K×I×5D remains dark-only** — this is the cell the superseded DIST-GEN.21 would have activated.

`V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency(GoalDistance.HalfMarathon, …)` (the real, current HM **canonical public** Level×Frequency allow-list, confirmed by direct source read of the `switch` arm) is:
`Intermediate×{3,4,5,6}D`, `Beginner×{3,4}D`, `Advanced×{3,4,5,6}D` — **10 public cells**. Never `Experienced` at any frequency (binding `PHASE_EXP_0` product-wide exclusion), never `Beginner×{2,5}D`, never `Advanced×2D`, never `Intermediate×2D` for HALF_MARATHON specifically (2D is TEN_K-only in the current allow-list).

## 5. Product-direction consequence

If the generic derived model is adopted for V1 production, the three independent per-distance DIST-GEN exact-cell authorities (15K, 16K, 18K) stop being the model new custom 10K–HM targets are built on. They are not deleted or invalidated — they become comparison oracles, legacy/compatibility surface for already-confirmed plans, and (per §23 below) an explicit `NO_NEW_10K_TO_HM_EXACT_DISTANCE_CELLS_FOR_V1` stop rule applies going forward.

## 6. Actual prototype output matrix

Reconstructed directly from `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests.cs`'s real assertions (not invented). All rows Intermediate×4D, source=HALF_MARATHON, horizon=12W (PreferredCore) unless noted:

| RequestedKm | SourceCanonical | MinWk | PrefWk | MaxWk | ChosenWk | TaperWk | PeakRealizedVol(I4D) | PeakLR ceiling | Ceiling bound? | TargetPaceDistance | Validation | Direct test evidence |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 10.1 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 | ≤19.0 | not directly asserted (no dedicated assertion at 10.1) | 10.1 | Valid | `DerivedMatrix_4D_AllInRangeTargets…`, `Eligibility_InRangeIntermediateSupportedFrequency_Resolves(10.1)` |
| 12.0 | HALF_MARATHON | 10 | 12 | 14 | 10/12/14 (all 3 tested) | 2 | ≤43.0 | ≤19.0 | asserted non-exceedance only | 12.0 | Valid, F/B/RS/T all present, Taper last | `Derived12K_EarlyCutoffTest…` (the phase's own "most important test") |
| 14.0 | HALF_MARATHON | 10 | 12 | 14 | 10/12/14 (all 3 tested via real HTTP pipeline) | 2 | ≤43.0 | ≤19.0 | not directly asserted | 14.0 | Valid | `RealPipeline_Derived14K_I4D_EligibleHorizons_MaterializeSuccessfully` |
| 16.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0, confirmed ≠16K's own exact-cell 40.0 | ≤19.0 | not directly asserted | 16.0 | Valid | `ExistingProjectedCellsNeverUsedAsSource_Derived16KUsesCanonicalHmPeak…` |
| 18.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 | ≤19.0 | not directly asserted | 18.0 | Valid | `DerivedMatrix_4D_AllInRangeTargets…(18.0)` |
| 20.0 | HALF_MARATHON | 10 | 12 | 14 | 10/12 | 2 | ≤43.0 | ≤19.0 | **directly asserted**: `peakLongRun <= 19.0+0.001`, non-chained taper confirmed | 20.0 | Valid | `Derived20K_LateCutoffTest_TaperAttachesCorrectly_NoHmIdentityLeakage_NoLrSynthesis` |
| 21.0 | HALF_MARATHON | 10 | 12 | 14 | 12 | 2 | ≤43.0 | ≤19.0 | not directly asserted | 21.0 | Valid | `DerivedMatrix_4D_AllInRangeTargets…(21.0)` |

**Honest evidence-tier note:** only the 12K (`Derived12K_EarlyCutoffTest`) and 20K (`Derived20K_LateCutoffTest`) rows carry a *direct, dedicated* real-LR/phase-structure assertion in the actual test file; the remaining rows (10.1/14/16/18/21) are proved only for phase-presence/validation/identity-preservation via the shared `DerivedMatrix_4D_AllInRangeTargets_…` theory and real-HTTP-pipeline tests, not a per-target LR/volume trace. `ChosenBodyEndWeek = ChosenWeeks − 2` holds at every tested horizon. No row's **exact** realized peak volume/long-run numeric value (as opposed to its ceiling) is captured anywhere in the test file's assertions — `_output.WriteLine` statements log it at runtime but the written report text never recorded the concrete number. This is disclosed honestly in §11 rather than invented.

## 7. Horizon open question

DERIVED-DIST.0 §25/§60 disclosed: the reused 10/12/14 triple is **not an invented number** (it is `RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks` plus two independently-re-converged constants from DIST-GEN.1/2/2A, DIST-GEN.5/6, DIST-GEN.9/10), but it is also not independently re-evidenced at every point of the continuous (10, 21.0975) interval — only 15/16/18K ever had dedicated per-distance evidence gathering.

## 8. Horizon boundary analysis

At **10.1K** (closest to TEN_K): `Eligibility_InRangeIntermediateSupportedFrequency_Resolves(10.1)` and `DerivedMatrix_4D_AllInRangeTargets_…(10.1)` both pass — a 10-week minimum plan materializes with full Foundation/Build/RaceSpecific/Taper. No test asserts that a 10-week body is *credible* for a 10.1K target specifically (e.g., that it isn't needlessly long for an athlete essentially already at 10K fitness) — this is a product-feel judgment, not a structural failure; no structural defect found.

At **21.0K** (closest to HM): `DerivedMatrix_4D_AllInRangeTargets_…(21.0)` passes at the 12-week default; `RealPipeline_ExactHalfMarathon_NeverEntersDerivedProjector` confirms exactly 21.0975 (the true HM distance) correctly bypasses derivation. No test demonstrates a 10-week *minimum* plan specifically for a 20.9–21.0K near-HM target (all 21.0K assertions use the 12-week default), so the "is 10W credible this close to HM" question is genuinely less evidenced than the 10.1K side — honestly flagged, not treated as a blocker, because `CatalogPhaseAllocationResolver`'s own structural invariant (never below any phase's catalog minimum) mechanically guarantees a full four-phase arc regardless of target distance; the open item is product *feel*, not structural safety.

The phase allocator's own minimum-respecting mechanism (confirmed unmodified, pre-existing, Backend Integration Phase 4G.3B.2) is the actual guarantor that canonical phase minimums are preserved at every horizon — this is structural authority, independent of which specific target distance is requested, so the "does 14W over-extend near 10K" question is answered the same way: the maximum is a product ceiling, not a per-distance training requirement, and nothing in the evidence shows a defect from allowing it.

## 9. Horizon decision

**`DERIVED_10K_TO_HM_HORIZON_10_12_14_APPROVED_AS_V1_PRODUCT_DEFAULT`**

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (not stronger — per DERIVED-DIST.0's own §25 disclosure, this is three independently-converged real authorities applied generically across a continuous interval, not independently re-closed per point). Standard applied (§6 of the governing prompt): generic phase allocation succeeds, canonical phase minimums are structurally preserved at every tested horizon, Foundation/Build/RaceSpecific/Taper arcs are complete in every real test, no saturation-chasing behavior was found (§13's 12K early-cutoff test is direct proof), and one simple, predictable, auditable V1 contract is strongly preferred over any no-evidence distance→weeks interpolation (explicitly forbidden by §39 of the governing prompt). No per-distance horizon rule is adopted.

## 10. Long-run ceiling vs realized LR

DERIVED-DIST.0's own §60 disclosure states plainly: canonical HM's `PreferredAbsolutePeakLongRunKm=19.0` is applied uniformly **regardless of requested target**. Critically, DERIVED-DIST.0's own §32 trace-table narrative states the realized trajectory (peak weekly volume, and by the same mechanism the long-run progression) is **"identical across every row by construction, since none of these fields are target-distance-dependent."** This means the realized peak long run for a given level/frequency/readiness-input combination is the **same absolute number** whether the requested target is 12K or 20K — it is not scaled to the target at all.

This is genuinely distinction (B), not (A), in the sense the governing prompt's §8 asks for: the ceiling does not "never bind" in the sense of being irrelevant — the realized trajectory is parent-sourced and identical regardless of target, so whatever value canonical HM's own progression naturally reaches (bounded above by 19.0km) is what every derived target in the interval receives, unscaled. It is simultaneously true that no code path artificially "synthesizes" a long run toward the ceiling (§19/§20, directly proven by the 20K test never exceeding 19.0) — so it is not (B) in the sense of a defect inventing a larger number; it is (B) in the sense that the **same real HM-production figure reaches every derived target without adjustment**.

## 11. Realized LR ratio table

| Target (km) | Realized peak LR (km) | Race distance (km) | HM ceiling (km) | Ceiling bound? | Observed concern? | Guardrail needed? | Decision |
|---|---|---|---|---|---|---|---|
| 10.1 | **INSUFFICIENT_EVIDENCE** — no test captures the exact realized number, only that it is ≤19.0 by the shared mechanism | 10.1 | 19.0 | Not directly measured | Qualitatively: ratio would be large (potentially >1.5×) if the realized value is near the 19.0 ceiling, same as every other row (§10) | See §13 | No per-row guardrail; global decision in §13 |
| 12.0 | INSUFFICIENT_EVIDENCE (exact value); confirmed ≤19.0 by the real 12K test's own shared mechanism | 12.0 | 19.0 | Not directly measured (test asserts phase structure, not exact LR value) | Same qualitative concern as 10.1 | See §13 | — |
| 14.0 | INSUFFICIENT_EVIDENCE (exact value) | 14.0 | 19.0 | Not directly measured | Moderate | See §13 | — |
| 16.0 | INSUFFICIENT_EVIDENCE (exact value) | 16.0 | 19.0 | Not directly measured | Low-moderate | See §13 | — |
| 18.0 | INSUFFICIENT_EVIDENCE (exact value) | 18.0 | 19.0 | Not directly measured | Low | See §13 | — |
| 20.0 | **Directly measured**: `peakLongRun <= 19.0 + 0.001`, confirmed never synthesized above 19.0 | 20.0 | 19.0 | **No** — realized LR stays strictly at or below the 19.0 ceiling, i.e. below race distance | None | No | Option A acceptable as-is for this row |
| 21.0 | INSUFFICIENT_EVIDENCE (exact value) | 21.0 | 19.0 | Not directly measured | None (LR < race distance structurally expected) | No | — |

**Honest disclosure**: this table's "realized LR" column cannot be filled with exact numbers from actual repository evidence — DERIVED-DIST.0's tests assert a non-exceedance bound (`<=19.0+0.001`), never the concrete realized figure, and no `_output.WriteLine` value was captured into the written report. The *qualitative* shape of the table (same absolute LR for every target, by construction per §10) is directly evidenced; the *exact numeric* ratio is `INSUFFICIENT_EVIDENCE`. This gap does not block the policy decision in §13, because the policy decision does not depend on the exact figure — it depends on the already-confirmed qualitative fact (uniform, non-target-scaled, parent-sourced, never-exceeding-19.0, never-synthesized) and on the already-existing safety validation (§12 below).

## 12. Long-run options

- **Option A — inherit canonical HM LR authority unchanged.** Pro: zero new policy, the exact value real paying HM customers already train under today; con: produces a target-invariant realized LR, proportionally large relative to a 12K race.
- **Option B — add a simple requested-target hard upper bound** (e.g., `PeakLongRun <= f(TargetDistanceKm)`). Forbidden shape explicitly ruled out by governing prompt §10 (no linear interpolation/percentage formula/per-distance table); a *simple* hard cap (e.g., `PeakLongRun <= TargetDistanceKm` or a similarly blunt rule) is not forbidden in principle but would require inventing new numeric policy — out of scope for this governance-only phase, and would mechanically fight the existing share-based volume mechanism without a real training-science redesign (which this phase is also barred from doing).
- **Option C — restrict public derived support for lower targets until a guardrail is governed.** Achievable with zero new numeric authority by simply not exposing the lowest-target end of the interval publicly in V1 (a scope decision, not a training-science one).
- **Option D — composition using an already-existing governed rule.** No existing governed rule in this codebase ties long-run magnitude to requested target distance (confirmed by source read of `VolumeSafetyPolicy.cs`/`CatalogVolumeAndLongRunPlanner.cs` references already made in DERIVED-DIST.0) — no such composition exists to reuse.

## 13. Long-run decision

**`HM_PARENT_LONG_RUN_AUTHORITY_ACCEPTABLE_FOR_DERIVED_V1_AS_IS`**

Classification: `PRODUCT_DEFAULT` (not `DIRECT_EXISTING_AUTHORITY` — the acceptability judgment for the *derived* use of this authority is this phase's own product call, even though the underlying 19.0km figure is itself pre-existing, already-shipped canonical HM authority).

Reasoning, applying §12's "prefer no new rule if realized output is already sane": the realized long run is never synthesized beyond what canonical HM's own already-safety-validated progression produces (directly proven for the 20K case, structurally guaranteed for every other case by the same unmodified mechanism per §10). It is literally the identical number real paying HALF_MARATHON customers of the same level/frequency/readiness profile already train under in production today — not a new, unvalidated figure. Per §11's own forced-20.0K requirement is explicitly *not* required by any existing invariant (§19 of the governing prompt), and inventing a target-scaled cap here would require new numeric policy this governance-only phase is barred from creating. The one genuine, disclosed residual concern — a 12K-labeled plan's long run being proportionally large relative to the 12K race distance — is a **UX/product-communication** question, not a safety defect, and is carried forward explicitly (not silently dropped) as a DERIVED-DIST.2 telemetry/UX item (§47 below), and as the reason the V1 rollout matrix (§25) stays conservative rather than immediately exposing every target/level/frequency combination at once.

## 14. Frequency cross-check

`DerivedFrequencyMatrix_16K_3D4D5D_AllDeriveFromCanonicalHmFrequencyAuthority` (real test, all three frequencies) directly confirms: 3D realizes ≤34.0km (canonical `HalfMarathonIntermediate3D`), 4D realizes ≤43.0km (`HalfMarathonIntermediate4D`), 5D realizes ≤51.0km (`HalfMarathonIntermediate5D`) — each genuinely distinct from 16K's own exact-cell peaks (34.0/40.0/51.0 numerically coincidental at 3D/5D but sourced from structurally different, independently-declared authority objects, per direct assertion `Assert.NotEqual(VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K…, peak)`). KEY2 presence at 5D (2 `KEY_SESSION` roles/week) is directly asserted and passes. The horizon/LR conclusions above hold identically across frequency because neither the horizon triple nor the LR ceiling mechanism varies by `daysPerWeek` in the derived path (the `VolumeSafetyPolicy` instance varies, but the horizon triple and LR-ceiling-applies-uniformly finding do not). No frequency-specific derived-distance policy is created or needed.

## 15. Public arbitrary-target model

Evaluated and **approved in restricted form** (see §25): `10.0 < TargetDistanceKm < HALF_MARATHON_DISTANCE` AND nearest-higher canonical parent (always HALF_MARATHON for this interval) supports the requested `(RunningExperience, RunsPerWeek)` pair, where "supports" means the canonical parent's own **public** eligibility (§4's 10-cell matrix), not merely its existence as a dark candidate. No exact-distance registry is required for the targets this model approves. This is a rule-based model, not an enumerated distance list (§28 satisfied by construction — `DerivedDistanceEligibilityPolicy` already has no per-distance list).

## 16. Canonical exact-target bypass

Confirmed unchanged and structurally enforced: `RealPipeline_ExactTenK_NeverEntersDerivedProjector` / `RealPipeline_ExactHalfMarathon_NeverEntersDerivedProjector` directly prove a request with no override (or an override numerically equal to either canonical endpoint) never reaches the derived path. 10K exact routes to canonical `TEN_K`; HM exact (21.0975) routes to canonical `HALF_MARATHON`. No nearest-lower fallback exists anywhere in `NearestHigherCanonicalPlanResolver` (confirmed: it only ever answers "strictly higher" or null).

## 17. Decimal target support

`Eligibility_InRangeIntermediateSupportedFrequency_Resolves` is directly parameterized with `13.5` and `17.7` (not just integers) and passes — exact decimal targets are already supported by the eligibility gate with zero snapping/rounding logic anywhere in `DerivedDistanceEligibilityPolicy.TryResolve`. If public rollout is approved, decimal targets (10.1/12.0/13.5/16.0/17.7/20.0/21.0, etc.) are supported identically, subject to existing request-level precision/validation (unchanged by this phase).

## 18. HM parent eligibility inheritance

Principle adopted and directly verifiable in current source: `DerivedDistanceEligibilityPolicy` requires `requestGoalDistance == GoalDistance.HalfMarathon` and checks only its own `EligibleLevels`/`EligibleRunsPerWeek` — it has **no independent knowledge of HALF_MARATHON's own public eligibility list** (`V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency`). This is a real, disclosed gap between the current (dark) implementation and the §17/§46 principle this phase adopts: **public derived eligibility must not exceed public canonical-parent eligibility**. Today the derived policy's own hardcoded Intermediate-3/4/5D scope happens to be a strict subset of HM's public matrix (§4), so the gap is latent, not yet exploitable — but DERIVED-DIST.2 must explicitly wire the derived eligibility gate to consult HM's own public allow-list (or a deliberately-curated subset of it, per §25) rather than relying on today's accidental containment. This is recorded as a required DERIVED-DIST.2 implementation item, not a DERIVED-DIST.1 blocker (no public exposure exists today to be exploited).

## 19. Beginner analysis

HM public: `Beginner×{3,4}D` (2 cells). DERIVED-DIST.0 proof status: **none** — `DerivedDistanceEligibilityPolicy.EligibleLevels` does not include `Beginner` at all; no test exercises a derived Beginner request. Classification: `Beginner×3D` → `NEEDS_TARGETED_CONFIRMATION`; `Beginner×4D` → `NEEDS_TARGETED_CONFIRMATION`. Not automatically enabled merely because Intermediate succeeded (governing prompt §19's explicit instruction honored).

## 20. Intermediate analysis

HM public: `Intermediate×{3,4,5,6}D` (4 cells). DERIVED-DIST.0 proof status: 3D/4D/5D **directly proven** by real pipeline and direct-orchestrator tests (§6/§14). 6D: **not tested** — `DerivedDistanceEligibilityPolicy.EligibleRunsPerWeek = [3,4,5]` excludes 6 entirely. Classification: `Intermediate×3D` → `READY_FOR_DERIVED_PUBLIC_REUSE`; `Intermediate×4D` → `READY_FOR_DERIVED_PUBLIC_REUSE`; `Intermediate×5D` → `READY_FOR_DERIVED_PUBLIC_REUSE`; `Intermediate×6D` → `NEEDS_TARGETED_CONFIRMATION`.

## 21. Advanced analysis

HM public: `Advanced×{3,4,5,6}D` (4 cells, all `PRODUCT_ELIGIBLE` per HM.15/§9). DERIVED-DIST.0 proof status: **none** — `Advanced` is entirely absent from `DerivedDistanceEligibilityPolicy.EligibleLevels`. Classification: all four (`Advanced×3D/4D/5D/6D`) → `NEEDS_TARGETED_CONFIRMATION`.

## 22. Experienced exclusion

`RunningBackground.Experienced` appears nowhere in `DerivedDistanceEligibilityPolicy.EligibleLevels`, nowhere in HM's own public `IsSupportedLevelFrequency` HALF_MARATHON arm (confirmed by direct source read — the arm enumerates only Intermediate/Beginner/Advanced tuples), and is explicitly documented in that same source as deliberately excluded per `PHASE_EXP_0`'s binding `PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED` decision. Derived-distance public rollout inherits this exclusion automatically via §18's principle (public derived eligibility cannot exceed public canonical-parent eligibility, and Experienced is never in the canonical-parent public set) — it is not a route around the existing exclusion.

## 23. Proposed Level×Frequency matrix

| Level | 2D | 3D | 4D | 5D | 6D |
|---|---|---|---|---|---|
| Beginner | HM canonical: not public; Derived: BLOCKED | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` | HM canonical: `PRODUCT_INELIGIBLE` (HM.13 §6, frozen) → `BLOCKED` | HM canonical: not public → `BLOCKED` |
| Intermediate | HM canonical: not public for HALF_MARATHON → `BLOCKED` | HM public, proven → `READY_FOR_DERIVED_PUBLIC_REUSE` | HM public, proven → `READY_FOR_DERIVED_PUBLIC_REUSE` | HM public, proven (incl. KEY2) → `READY_FOR_DERIVED_PUBLIC_REUSE` | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` |
| Advanced | HM canonical: not public → `BLOCKED` | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` | HM public, not proven → `NEEDS_TARGETED_CONFIRMATION` |
| Experienced | BLOCKED (product-wide exclusion, every frequency) | BLOCKED | BLOCKED | BLOCKED | BLOCKED |

## 24. Full-parent vs validated-subset comparison

**Option A — `FULL_CANONICAL_PARENT_MATRIX`** (all 10 HM-public cells immediately derived-public): lowest implementation friction (one eligibility-list widening), but 7 of the 10 cells are `NEEDS_TARGETED_CONFIRMATION` with zero direct test evidence today — highest behavioral-confidence risk, would require DERIVED-DIST.2 to add targeted tests for every one of those 7 cells before this could be honestly called proven, and carries real accidental-expansion risk (§47 — e.g., Beginner's known HM.13-frozen 5D exclusion or Advanced's genuinely different KEY2/taper authority at some frequencies could interact with derivation in ways never tested).

**Option B — `VALIDATED_SUBSET`** (expose only Intermediate×3D/4D/5D): matches exactly what DERIVED-DIST.0 directly tested against the real production pipeline; zero additional tests required before activation; unsupported cells fail closed by construction (today's `DerivedDistanceEligibilityPolicy.EligibleLevels`/`EligibleRunsPerWeek` already encode exactly this subset — **zero widening needed** to adopt this option, only the public-DTO/mapper wiring DERIVED-DIST.0 deliberately left unbuilt). Lower immediate user value (narrower matrix) but highest behavioral confidence and lowest risk.

## 25. Public rollout matrix decision

**`DERIVED_PUBLIC_V1_INTERMEDIATE_3D_4D_5D_ONLY`**

Chosen because: (a) it is the only option requiring zero new tests before activation — it is exactly DERIVED-DIST.0's own proven scope; (b) it naturally satisfies §18's parent-eligibility-inheritance principle without further wiring, since the derived gate's existing hardcoded scope is already a strict subset of HM's public matrix; (c) it avoids the accidental-expansion risk §47 warns against; (d) the alternative (Option A) would require this governance-only phase to implicitly bless 7 untested cells, several of which (Beginner, all of Advanced, Intermediate 6D) involve materially different authority (different taper/KEY2 structures per HM.15/HM-X1.2) that has never been exercised through the derivation mechanism. DERIVED-DIST.2's scope (§58) explicitly includes adding targeted tests to widen this matrix later, cell by cell, never a bulk blanket widening.

## 26. Existing exact projected cells

Future role: **`LEGACY_COMPATIBILITY_ONLY`** for 15K×I×4D, 16K×I×4D, 16K×I×3D, 18K×I×4D (today's 4 public cells) and 16K×I×5D (today's 1 dark-only cell). No deletion in this phase. See §49 (migration table) for the full per-cell routing disposition.

## 27. Old/new 15K comparison

Same shape as DERIVED-DIST.0's own §43 18K comparison (15K was never re-derived with its own dedicated test in DERIVED-DIST.0, but the mechanism is target-invariant per §6/§10): peak realized volume 40.0km (15K's own exact-cell `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance15K`) → 43.0km (canonical HM I4D) = `EXPECTED_MODEL_CHANGE`; long-run ceiling 14.5km (15K's own frozen `PreferredAbsolutePeakLongRunKm`) → 19.0km (canonical HM ceiling) = `EXPECTED_MODEL_CHANGE`, and per §10/§13 the realized figure itself likely approaches the new, larger ceiling (not merely a raised bound that never binds) = `USER_VISIBLE_CHANGE`; horizon 10/12/14 → 10/12/14 = identical (`EXPECTED_MODEL_CHANGE`, differently owned, no user-visible delta); taper mechanics identical; target pace/identity preserved identically; phase coverage identical; safety: both pass the same unmodified validator chain = identical. No `SAFETY_RELEVANT_CHANGE`/`BUG`/`UNKNOWN` found for 15K specifically, mirroring DERIVED-DIST.0's own 18K finding exactly.

## 28. Old/new 16K comparison

Directly reproduced from DERIVED-DIST.0 §42 (verbatim real-test evidence, not re-derived): peak 40.0→43.0km (`EXPECTED_MODEL_CHANGE`); long-run ceiling 15.0→19.0km (`EXPECTED_MODEL_CHANGE`, and per §10/§13 of this report, `USER_VISIBLE_CHANGE` since the realized value itself likely rises, not only the bound); horizon 10/12/14→10/12/14 identical; taper identical mechanism, different declaring class; target pace/calendar/safety identical. Zero `SAFETY_RELEVANT_CHANGE`/`BUG`/`UNKNOWN`.

## 29. Old/new 18K comparison

Directly reproduced from DERIVED-DIST.0 §43: same shape as §28, peak 40.0→43.0km, long-run ceiling 16.0→19.0km, all `EXPECTED_MODEL_CHANGE` plus one `USER_VISIBLE_CHANGE` (realized LR magnitude), zero `SAFETY_RELEVANT_CHANGE`/`BUG`/`UNKNOWN`.

## 30. 16K I5D decision

**Option (C) — require a small public-lifecycle proof first**, via the `DERIVED_PUBLIC_V1_INTERMEDIATE_3D_4D_5D_ONLY` matrix (§25): 16K×Intermediate×5D becomes publicly reachable **together with every other eligible 10K–HM target at Intermediate×5D**, through the generic derived path, once DERIVED-DIST.2 proves the real HTTP/Postgres lifecycle end-to-end (governing prompt §54's own requirement) — not via DIST-GEN.21's originally-planned narrow single-cell exact-registry activation, which this phase formally supersedes (§56). This is **not** "(A) make generic 16K I5D public together with all eligible custom targets" blindly — it still requires DERIVED-DIST.2's own real-lifecycle proof gate before activation, and it is **not** "(B) keep 5D custom public off despite generic capability" — the capability is already proven (§14), and withholding it indefinitely with no path forward would be the accidental-expansion risk's mirror image (needless conservatism with no safety basis).

## 31. Mixed-semantics risk

Directly assessed: today, 15K/16K/18K hit their own exact-cell authority (different peak/LR numbers per §27–29) while 12K/13K/14K/17K/19K/20K (any other 10K–HM target) are entirely unreachable in production (no public DTO field exists at all). If `DERIVED_PUBLIC_V1_INTERMEDIATE_3D_4D_5D_ONLY` is adopted **without also migrating 15K/16K/18K's own new-creation routing**, nearby distances would permanently follow different training models for purely historical reasons — exactly the risk §22/§67 warn against. This is the core argument for §33's migration decision: new-creation routing for the entire interval must converge on one model, not leave three islands un-migrated indefinitely.

## 32. New-creation migration model

Adopted: **before** — 15K/16K/18K new requests may hit their own exact projected cells, directly via `Dark16KPilotEligibilityPolicy`/`ProjectedTargetAuthorityRegistry`, consulted first at every one of the three widened seams. **After** — all strict 10K–HM custom new-creation requests (within the §25 matrix) hit the generic derived path; existing exact-cell policies remain in place, unmodified, but become unreachable from new-creation requests once the public mapper is repointed, retained only for historical-plan compatibility/rollback/tests/oracles (§26) until a future, dedicated DERIVED-DIST.3 physical cleanup. DERIVED-DIST.0's own existing dispatch order (exact registry consulted first, generic derived path as fallback) is the exact mechanical seam DERIVED-DIST.2 must invert or bypass for 15/16/18K specifically — not rewritten from scratch.

## 33. Existing-plan compatibility

Directly confirmed by current source grep (§3): none of the five derived-distance types, nor the three widened seams, are referenced anywhere outside plan-generation code. `CatalogPlanConfirmationService` (the persistence boundary) and every Home/Calendar/TrainingDay/Complete/Not-Today/Cancel/mutation handler operate on the already-persisted plan entity and its already-materialized calendar — none of them re-run eligibility, horizon, or routing logic. Creation routing is therefore structurally **not consulted** when operating an existing plan today, and migrating new-creation routing changes nothing about how an existing plan is read or mutated.

## 34. Persisted-plan immutability

No migration path, background rewrite, or recomputation job exists anywhere in the current codebase for `TrainingPlan` rows (confirmed by the same absence-of-reference grep in §33, plus DIST-GEN.2's own historical finding, reused here as existing evidence, that `RequestedTargetDistanceKm`/`CanonicalDistanceFamily` are persisted once at confirmation time). A historical exact-cell plan's persisted calendar/sessions/volume numbers are not touched by any future routing-policy change — generation-time routing only ever affects a *new* plan's own generation, never an existing row. This phase requires **no** persisted-plan migration, consistent with DERIVED-DIST.0's own §48 database zero-delta finding.

## 35. Preview/confirm compatibility

**Direct source confirmation required and performed**: `CatalogPlanConfirmationService` persists the already-fully-materialized preview payload (confirmed by DIST-GEN.2's own historical fix: it carries the preview's own `RequestedTargetDistanceKm`/`CanonicalDistanceFamily`/full calendar through to the persisted row, rather than recomputing anything at confirm time). This means a preview generated under the **old** exact-cell route and confirmed **after** a deployment switches new-creation routing to derived would still confirm successfully and persist exactly what the stale preview computed — there is no re-validation against "current routing rules" at confirm time in the existing mechanism. **This is the one genuine, concrete migration-guard gap this phase identifies**: a conceptual guard is required (not implemented here) — e.g., stamping the preview response with the routing model/version it was generated under, and having confirm reject (or silently accept, as a deliberate product choice) a preview whose stamped routing model predates a routing-boundary flip, rather than silently blending old-exact-cell numbers into a plan nominally created "after" migration. DERIVED-DIST.2 must design this guard explicitly; it is named here as a required implementation item, not resolved.

## 36. Rollback model

The current architecture **does** permit the required rollback shape: `DerivedDistanceEligibilityPolicy`/`DerivedDistanceHorizonAuthority` are consulted only as a fallback *after* `Dark16KPilotEligibilityPolicy`/`ProjectedTargetAuthorityRegistry` return null at all three widened seams (confirmed unchanged, `RealPipeline_ExistingProjectedCells_StillUseTheirOwnExactAuthority_NeverDerived` directly proves dispatch order). Reverting production to "exact projected cells are primary for 15/16/18K, derived is primary only elsewhere" requires no code deletion — it requires only re-pointing whichever single routing boundary DERIVED-DIST.2 introduces (§37) back to its pre-migration state (a config/feature-toggle flip), since both mechanisms already coexist in the current codebase side by side. No DB rollback, no catalog rollback, no plan mutation is required for this rollback, consistent with DERIVED-DIST.0's own zero-DB-delta finding.

## 37. Routing boundary

Recommended single owner: the **public target-distance routing/eligibility seam** — concretely, wherever `GeneratePreviewCommandMapper.ToInternalRequest` currently consults `Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell` (confirmed by direct source-text test `PublicZeroDelta_NoNewPublicCellAdded_DerivedPolicyNeverConsultedByPublicMapper` to be the one public-reachable gate today). DERIVED-DIST.2 should introduce exactly one new decision point there (e.g., "does this request match the derived-public matrix first; if not, fall back to the exact-cell public registry; if neither, reject") rather than sprinkling per-distance conditionals through `PlanServices.cs`/`CatalogPreviewGenerator.cs`/`CatalogPrescriptionContextBuilder.cs` a second time.

## 38. Existing registry future

`Dark16KPilotEligibilityPolicy` (`ApprovedDarkCells`/`ApprovedPublicCells`): **still required** for legacy-plan generation-time compatibility and test oracles until Stage 4 cleanup (§39); **not required** for new public creation once migration completes, if the routing boundary (§37) is flipped; **not** safe to retire immediately — runtime operation of already-confirmed plans does not depend on it (§33/§34), but *re-generating a preview* for comparison/oracle purposes, and the full existing `DistGen*Tests.cs` suite, both still do.

`ProjectedTargetAuthorityRegistry`: same disposition — downstream of the dark gate, still required while any caller (legacy generation path, oracle tests) can reach it; safe to retire physically only after DERIVED-DIST.3 confirms zero remaining callers.

## 39. Exact policy future

15K/16K/18K `VolumeSafetyPolicy` instances, horizon policies (`TargetDistance{15,16,18}KHorizonPolicy`, `TargetDistance16KIntermediate{3,5}DHorizonPolicy`), and peak-volume-band policies: **`KEEP_UNTIL_POST_MIGRATION_CLEANUP`** — they remain the sole authority for any legacy-path regeneration/oracle use during Stages 1–3 (§53), and for the existing DistGen test suite (§40); eligible for `SAFE_TO_REMOVE_AFTER_ALL_CALLERS_GONE` only after a future DERIVED-DIST.3 physically removes the legacy generation call path per its own dedicated audit of remaining callers. No deletion in this phase.

## 40. DistGen test future

The 225 existing `DistGen*Tests.cs` tests are classified **`historical regression oracle`** — they continue to assert the exact-cell path's own behavior (never the derived path), remain the canonical proof that legacy-plan generation (used by Stage 2/3 compatibility and any oracle regeneration) is unaffected by migration, and must keep passing unmodified through Stages 1–3. None are `obsolete new-creation assertion` yet, because the exact-cell generation mechanism they test is not removed by this phase or by DERIVED-DIST.2's own staged plan (§53) — only DERIVED-DIST.2's *public new-creation routing* changes, not the underlying exact-cell generation code the tests exercise directly. They become `cleanup candidate`s only at DERIVED-DIST.3, after the legacy generation code itself is confirmed to have zero remaining callers.

## 41. API contract

**No new API contract required for the approved V1 scope.** `GeneratePreviewRequest.TargetDistanceKmOverride` (internal DTO field) already accepts an arbitrary `double?`, including exact decimals (§17); the gap is entirely on the **public wire-DTO** side — no public `GenerateRacePlanPreviewRequest`/`GenerateHabitPlanPreviewRequest` field currently carries an arbitrary numeric target at all for non-16K/15K/18K distances (the existing public `TargetDistanceKm` field introduced by DIST-GEN.3 is itself already wire-compatible with an arbitrary decimal — it was simply never validated against anything but the fixed exact-cell registry). DERIVED-DIST.2's required migration: widen `GeneratePreviewCommandMapper`'s own canonicalization/eligibility check to also recognize the §25 derived-public matrix — an additive mapper-logic change, not a new field.

## 42. Frontend contract

Classification: **`FRONTEND_PRODUCT_DECISION_REQUIRED`**. DIST-GEN.0 (read for this phase, historical finding reused as existing evidence) already disclosed that both the race-goal free-text distance field and the habit-goal distance stepper let a user type an arbitrary distance today but silently discard values outside the fixed presets — a pre-existing, previously-disclosed defect/gap, not newly found here. Whether to build a genuine arbitrary-custom-distance entry UI (vs. continuing to offer only a fixed preset list expanded to include more named stops) is a product decision this governance phase does not make — it is named explicitly so DERIVED-DIST.2 does not silently inherit an assumption either way.

## 43. DB contract

Confirmed, consistent with DERIVED-DIST.0 §48: `TrainingPlan` already separates `RequestedTargetDistanceKm` from `CanonicalDistanceFamily`/`GoalDistanceKm` (confirmed by direct source read earlier in this engagement's own DIST-GEN.0/DIST-GEN.2 history, re-verified here by the absence of any new persistence-shape requirement in DERIVED-DIST.0's own zero-DB-delta finding). **No DB migration required** for the approved V1 scope.

## 44. Target identity

Confirmed by direct test (`TargetSensitiveFieldTest_NeverLeaksHalfMarathonIdentity`): a 14K/16K derived request's `RequestedTargetDistanceKm` equals the real requested value and never equals HALF_MARATHON's own 21.0975km representative constant, while `CanonicalDistanceFamily`/`GoalDistanceKm` legitimately still read HALF_MARATHON as the internal structural-parent concept. A derived 14K plan must persist/display as "14K", never relabeled "Half Marathon" — this is already how the mechanism behaves today; DERIVED-DIST.2 must preserve it exactly when widening the public-facing response/read DTOs the way DIST-GEN.3 already did for 16K.

## 45. Target pace

Confirmed unchanged (§21 of DERIVED-DIST.0, re-verified by source read of `GoalFeasibilityResolver.ResolveFromRecentRace`'s own `input.RequestedTargetDistanceKm ?? input.GoalDistanceKm` read — zero lines changed): migration does not introduce any old exact-cell pace dispatch ahead of the generic binder. The new route (once §37's boundary flips) remains requested-target → generic pace resolver, with zero new pace code.

## 46. HM label leakage

Audited conceptually against `DerivedDistanceTargetFieldAudit.Entries`: every `KeepFromParent` field (candidate identity, `RunLayout`, phase topology, `VolumeSafetyPolicy`, taper mechanics, KEY1/KEY2 content) is an **internal structural concept**, never a response/display field by construction — the response-facing identity fields (`RequestedTargetDistanceKm`, target pace) are the `RebindToRequestedTarget`/`RecomputeThroughExistingGenericResolver` ones, directly proven never to leak HM's 21.0975km value (§44). The one term requiring explicit product-copy discipline going forward: canonical HM's 19.0km long-run ceiling (§10/§13) must be documented and user-copy-reviewed as a **"source-parent safety ceiling"**, never as "your 12K/14K/16K peak long-run target" — this is a naming/communication discipline item for DERIVED-DIST.2's UX work, not a code defect. No current code path was found that emits an HM-specific user-facing string for a derived plan.

## 47. Telemetry

Searched for existing telemetry distinguishing canonical/legacy-exact/generic-derived creation, requested custom target, level/frequency, or generation-failure reason by the requested-distance dimension: none found beyond the existing exception-type/reason-code mechanism already used for routing decisions themselves (e.g., `DARK_16K_CORE_HORIZON_*` vs `DERIVED_DISTANCE_CORE_HORIZON_*` reason-code strings, which are diagnostic text, not telemetry events). **`SOURCE_SILENCE`.** Minimal recommended rollout observability for DERIVED-DIST.2 (not built here): a per-request log/metric tagging which routing path served the request (legacy-exact vs. derived vs. canonical), the requested target distance, and the realized peak long run — specifically to empirically close §11's own disclosed evidence gap (the exact realized-LR values) and to monitor the §13 UX concern (large LR relative to shorter targets) post-launch rather than pre-guessing it.

## 48. Product analytics/source silence

No real user-demand data for arbitrary custom distances (12K/13K/14K/17K/19K/20K) exists in this repository or was found by this phase's search. **`SOURCE_SILENCE`** — this phase does not claim users need any particular additional distance; the architecture now supports the full continuous interval, and the §25 rollout scope is chosen for simplicity/safety/confidence, not assumed demand.

## 49. Migration table

| Row | Current new-creation path | Future new-creation path | Legacy operation path | Old policy retained? | Rollback route | Removal phase |
|---|---|---|---|---|---|---|
| 15K×I×4D | Exact registry (`Dark16KPilotEligibilityPolicy`/`ProjectedTargetAuthorityRegistry`), public | Generic derived (§25 matrix covers I×4D) | Unaffected — persisted plans read/mutated with zero routing dependency (§33) | Yes, `KEEP_UNTIL_POST_MIGRATION_CLEANUP` | Flip §37 routing boundary back | DERIVED-DIST.3 (after zero-caller audit) |
| 16K×I×3D | Exact registry, public | Generic derived | Unaffected | Yes | Flip boundary back | DERIVED-DIST.3 |
| 16K×I×4D | Exact registry, public | Generic derived | Unaffected | Yes | Flip boundary back | DERIVED-DIST.3 |
| 16K×I×5D | Exact registry, **dark only** (no public mapper entry) | Generic derived, newly **public** via §25/§30 | Unaffected | Yes | Flip boundary back, or simply leave public-mapper entry absent again | DERIVED-DIST.3 |
| 18K×I×4D | Exact registry, public | Generic derived | Unaffected | Yes | Flip boundary back | DERIVED-DIST.3 |
| Any other in-range custom target (e.g. 12K/13K/14K/17K/19K/20K), I×3D/4D/5D | Unreachable (no public DTO/registry entry today) | Generic derived (newly public, §25) | N/A (no existing plans) | N/A | Disable public derived routing (feature toggle) | N/A |
| Any target/level/frequency outside §25 (Beginner/Advanced/I6D, or Experienced) | Unreachable | Still unreachable in V1 (`NEEDS_TARGETED_CONFIRMATION`/`BLOCKED`, §23) | N/A | N/A | N/A | Future phase, after targeted proof |

## 50. Level/frequency matrix

See §23 (reproduced there in full per the governing prompt's own required column set).

## 51. Horizon decision table

| Candidate policy | 10.1K/12K/14K impact | 16K/18K impact | 20K/21.0K impact | Evidence/support | Product simplicity | Risk | Decision |
|---|---|---|---|---|---|---|---|
| 10/12/14 shared triple (current) | Full F/B/RS/T arc proven at 10/12/14W for 12K (`Derived12K_EarlyCutoffTest`); 10.1K proven eligible/materializing at default horizon | Full arc proven at 10/12/14W for 14K via real HTTP pipeline; 16K/18K proven at 12W default | 20K proven at 12/14W with correct taper/no-LR-synthesis; 21.0K proven at 12W default | Three independent real-evidence closures converged on this exact triple (DIST-GEN.1/2/2A, 5/6, 9/10) — `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, not invented | High — one triple for the whole interval, zero per-distance lookup | Low — structural phase-allocation minimum invariant guarantees a full arc regardless of target; near-boundary "feel" (10W near HM, 14W near 10K) untested but not structurally unsafe | **Adopted** |
| Distance-sensitive rule (e.g. shorter horizon for targets near 10K) | Would require new, uninvented numeric authority | Same | Same | None — would violate the "no distance→weeks lookup" prohibition (§39) | Lower — a second axis of configuration | Unknown — never evidenced | Rejected |
| Horizon decision blocked | N/A | N/A | N/A | N/A | N/A | N/A | Rejected — sufficient evidence exists to decide |

## 52. Long-run decision table

See §11 (reproduced there in full, including the honest `INSUFFICIENT_EVIDENCE` disclosure for exact realized-LR figures) and §13 for the decision itself.

## 53. Future routing model

```
if request is exact canonical target (10.0 or 21.0975, within tolerance):
    route to canonical TEN_K / HALF_MARATHON generation (unchanged)
elif request.TargetDistanceKm is strictly inside (10.0, 21.0975)
     and (Level, RunsPerWeek) is in the §25 derived-public matrix
     (Intermediate × {3,4,5}D only, for V1):
    route to generic derived generation (NearestHigherCanonicalPlanResolver
    + DerivedDistanceEligibilityPolicy + DerivedDistanceHorizonAuthority)
else:
    reject as unsupported (existing fail-closed exception types/reason codes)
```
The legacy exact-cell route (`Dark16KPilotEligibilityPolicy`/`ProjectedTargetAuthorityRegistry`) is **not** part of this future primary rule for new creation — it remains reachable only as the legacy/compatibility path during Stages 1–3 (§57) and for internal oracle/test use, never as the rule a new public request is evaluated against once migration completes.

## 54. V1 stop rule

**`NO_NEW_10K_TO_HM_EXACT_DISTANCE_CELLS_FOR_V1`.** No new per-distance exact projected cell (e.g., a hypothetical 17K or 19K exact authority) should be created going forward; any further 10K–HM distance coverage comes from the generic derived path's own eligibility/matrix widening (§25), never a new `TargetDistanceNNKHorizonPolicy`/`VolumeSafetyPolicy` pair. No exceptional safety reason was found requiring an exception to this rule.

## 55. Superseded DIST-GEN work

- 12K structural-boundary question (DIST-GEN.0's own disclosed near-10K caveat): `SUPERSEDED_BY_DERIVED_DISTANCE_MODEL` — the derived model handles any exact decimal target generically; no discrete 12K cell is needed.
- 10-mile-exact-vs-16K authority-equivalence question (DIST-GEN.4/DIST-GEN.5's own bounded-not-resolved item): `SUPERSEDED_BY_DERIVED_DISTANCE_MODEL` — a 16.09km exact target is simply another point the derived interval already covers; the equivalence question dissolves once no per-distance authority is being frozen.
- 15/16/18K banding question (whether their numeric authorities should be interpolated/banded together): `SUPERSEDED_BY_DERIVED_DISTANCE_MODEL` for all **new** creation once migration completes — `STILL_RELEVANT` only as read-only legacy/historical context for understanding already-confirmed plans generated under the old model.

## 56. DIST-GEN.21 status

**`SUPERSEDED_BY_DERIVED_DIST_PRODUCTION_MIGRATION`**, confirmed (not merely expected) — direct `grep` of `PHASE_LEDGER.md` found no DIST-GEN.21 row was ever added (the ledger's last DIST-GEN-numbered row is DIST-GEN.20, #68; DERIVED-DIST.0 is #69), confirming DIST-GEN.21 was never begun. §30 of this report additionally shows its originally-planned narrow single-cell exact-registry activation is superseded by the broader, already-proven generic mechanism for the identical cell (16K×Intermediate×5D), which this phase's §25/§30 decisions bring into the V1 public rollout scope by a different, more general route.

## 57. Roadmap normalization

`MASTER_ROADMAP.md` §18 ("Product-Wide Distance Architecture Decisions") is updated by this phase (see the `### DERIVED-DIST.1` subsection appended below in that file) to mark the DIST-GEN.1–20 per-distance exact-cell expansion branch (further 15K/18K frequency widening, 16K×I×6D, any new discrete NNK cell) as superseded/deferred by the derived-distance model for **new creation**, while explicitly preserving those phases' own historical validity as oracle/legacy authority — so a future agent does not continue that branch without first reopening it deliberately.

## 58. DERIVED-DIST.2 scope

Named, not begun: **DERIVED-DIST.2 — Custom 10K→HM Derived-Distance Production Routing Migration, Controlled Public Activation and Legacy Exact-Cell Creation Retirement**. Its unambiguous implementation contract, per this phase's decisions:
1. Wire the public mapper's routing boundary (§37) to recognize the §25 matrix (Intermediate×{3,4,5}D, any exact decimal target strictly in (10.0, 21.0975)) as derived-eligible, consulted alongside (and for 15/16/18K, ahead-of-dispatch-order-inverted relative to) the existing exact-cell public registry per §32.
2. Wire `DerivedDistanceEligibilityPolicy` to consult HM's own public `IsSupportedLevelFrequency` allow-list directly (§18), rather than relying on today's accidentally-contained hardcoded subset.
3. Design and implement the preview/confirm routing-version guard (§35).
4. Enable the approved public range (§25) end-to-end, proven via real HTTP/Postgres lifecycle tests (preview→confirm→read→mutate→cancel), mirroring DIST-GEN.3's own proof shape.
5. Stop new public creation from reaching 15K/16K/18K's own exact cells once proven (§53's routing model becomes real), while leaving all exact-cell/legacy code, tests, and registries completely in place (§38–40).
6. Prove the rollback path (§36) actually works by exercising the toggle in a test.
7. Do not physically delete any exact-cell policy, registry entry, or DistGen test.
8. Do not widen the public matrix beyond §25 without its own targeted-confirmation evidence per cell (§50).

## 59. DERIVED-DIST.3 future cleanup

Named, not begun: **DERIVED-DIST.3 — Legacy Projected-Distance Exact-Cell Code and Test Retirement Cleanup**, scoped to run only after DERIVED-DIST.2 succeeds in production: audit remaining callers of `Dark16KPilotEligibilityPolicy`/`ProjectedTargetAuthorityRegistry`/the five `TargetDistanceNNK*` policy classes, and physically remove any with zero remaining callers, reclassifying the corresponding DistGen tests from `historical regression oracle` to `cleanup candidate` only at that point.

## 60. Final classifications

- **Horizon**: `DERIVED_10K_TO_HM_HORIZON_10_12_14_APPROVED_AS_V1_PRODUCT_DEFAULT` (§9)
- **Long-run**: `HM_PARENT_LONG_RUN_AUTHORITY_ACCEPTABLE_FOR_DERIVED_V1_AS_IS` (§13)
- **Public rollout**: `ARBITRARY_10K_TO_HM_CUSTOM_TARGETS_APPROVED_WITH_RESTRICTED_LEVEL_FREQUENCY_MATRIX` — specifically `DERIVED_PUBLIC_V1_INTERMEDIATE_3D_4D_5D_ONLY` (§25/§51)
- **Migration**: `EXISTING_PROJECTED_NEW_CREATION_SHOULD_MIGRATE_TO_GENERIC_DERIVED_PATH` (§32), executed via the staged model (§53/§58) rather than a single risky cutover
- **Retirement**: `EXACT_PROJECTED_CELLS_CAN_MOVE_TO_LEGACY_COMPATIBILITY_AFTER_MIGRATION` (§26/§38/§39)
- **Next-implementation readiness**: `DERIVED_DIST_PRODUCTION_MIGRATION_READY`

## 61. Remaining blockers

**No implementation blocker.** Two genuine, disclosed, non-blocking evidence gaps carried forward explicitly (not silently resolved):

1. **Exact realized peak-long-run figures (§6/§11)** are not captured anywhere in DERIVED-DIST.0's own test assertions — only the `<=19.0` non-exceedance bound is directly proven for every target except 20K's own dedicated test. The qualitative shape (uniform, parent-sourced, never-synthesized) is sufficient to make the §13 policy decision, but DERIVED-DIST.2 should capture the exact figures via the telemetry recommended in §47 to empirically confirm (or revisit) this governance decision post-launch.
2. **The derived eligibility gate's current accidental (not deliberate) containment within HM's public matrix (§18)** must be replaced with an explicit dependency on HM's own public allow-list before DERIVED-DIST.2 widens the matrix beyond today's Intermediate-3/4/5D scope — named as a required DERIVED-DIST.2 implementation item, not a blocker to closing this governance phase, since the current V1-approved scope (§25) does not depend on it.

Neither item blocks `CUSTOM_10K_TO_HM_DERIVED_DISTANCE_V1_MIGRATION_AND_PUBLIC_MODEL_CLOSED`: every checklist item in the governing prompt's §81 is genuinely met — actual derived outputs were reviewed against real test evidence (§6, with gaps honestly disclosed rather than papered over); horizon policy is explicitly decided (§9); realized LR is explicitly distinguished from the 19km ceiling (§10); the LR guardrail policy is explicitly decided (§13); the public arbitrary-target policy is explicitly decided (§15/§25); the Level×Frequency public matrix is explicit (§23/§25); existing exact-projected new-creation migration is explicitly decided (§32/§60); historical-plan compatibility is resolved (§33/§34); the preview/confirm migration risk is resolved — a concrete conceptual guard is specified (§35), not silently dropped; the rollback path is explicit (§36/§61); exact-cell retirement staging is explicit (§53/§58/§59); no permanent mixed new-creation semantics remain once DERIVED-DIST.2 executes the migration (§31/§32); no new exact cells are required for V1 (§54); DIST-GEN.21 is explicitly superseded with reason (§56); DERIVED-DIST.2 has one unambiguous implementation contract (§58); and no production files were changed by this phase (§2/§3, confirmed by `git status`/`git diff --stat` before committing).
