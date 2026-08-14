# Phase 10K-GEN.2B.3 — 3D Taper / Minimum-Volume Conflict Decision

Scope: `TEN_K / INTERMEDIATE / 3D / CORE_PATH / 8–14 weeks`. Product/domain decision only; no code, catalog, or test change.

## 1. Conflict statement

Explicit-zero starts at 12 km. Normal 3D minima are KEY 4 + EASY 3 + LONG 5 = 12 km. With the retained 0.53 taper multiplier, the approved short trajectories produce 9.5 km (8w) and 10.5 km (10w), so they cannot materialize all frozen roles at normal minima. GEN.2B.2 correctly stopped.

## 2. Session-minimum semantic classification

Primary classification: `NORMAL_TRAINING_PRESCRIPTION_MINIMUM`, with an important workout-specific qualification.

The GEN.2B.1 4 km KEY minimum was justified by normal quality-session viability. Repository authority proves it is not universal: `V1TaperSharpenPrescriptionPolicy` applies specifically to `TAPER / TAPER_SHARPEN / KEY_SESSION / EASY_STANDARD` and has an exact 3.0 km minimum composed of 1.5 km easy baseline + 0.5 km controlled sharpening + 0.5 km recovery, with the remaining 0.5 km resulting from exact component selection. Thus taper KEY is `WORKOUT_SPECIFIC_MINIMUM = 3.0 km` under the existing 4D-derived prescription authority.

No equivalent approved 3D taper-specific minimum exists for EASY or LONG. `EASY_STANDARD` has a single easy main set and no distance floor in its catalog definition; `LONG_RUN_STANDARD` likewise does not establish a taper floor. Therefore a complete taper-specific 3D triple cannot be inferred from repository evidence.

## 3. Taper-multiplier authority classification

`TaperVolumeMultiplier = 0.53` is not a scientifically proven or catalog-level universal 10K constant. `PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md` classifies it as an `EXPLICIT_PRODUCT_DEFAULT`, `EVIDENCE_INFORMED`, and `GOLDEN_FIXTURE_CALIBRATED`, approximately reproducing the 4D 38→20 km taper. Current runtime applies it generically, but its provenance is 4D.

This phase does not claim frequency independence. Under the prompt's binding decision, 0.53 is explicitly retained as the V1 3D interaction value; this GEN.2B.3 decision supplies independent 3D product provenance for retaining it while restricting eligibility. Classification: `3D_TAPER_MULTIPLIER_RETAINED_AS_PRODUCT_DEFAULT_WITH_4D_CALIBRATION_PROVENANCE`. No new multiplier is invented.

## 4. Option A — taper-specific minima

`TAPER_SPECIFIC_MINIMA_ARCHITECTURALLY_COHERENT` but numerically incomplete.

The existing policy supports a 3 km taper KEY without changing its identity. It does not support exact lower EASY and LONG minima. Reducing them by inference might preserve role names but would introduce unsupported domain values. Exact complete minima would require narrow external/product evidence. Option A is not selected.

## 5. Option B — 12 km taper floor

For 8w zero, `max(17.5×0.53,12)=12`, a 31.4% reduction instead of about 47%. For 10w zero, `max(19.5×0.53,12)=12`, a 38.5% reduction. Both preserve the layout/minima, but the 8w result falls outside the current accepted 41–60% taper-reduction envelope. The floor is derived from role minima, not taper evidence, and creates frequency-specific taper behaviour.

Classification: `TAPER_VOLUME_FLOOR_WEAKENS_TAPER_SEMANTICS`. Not selected.

## 6. Option C — taper role/cardinality change

`CatalogStageToWeekMaterializer` creates the complete RunLayout role list for every allocated phase, including TAPER; validators, calendar assignment, binding, persistence and public schedule all consume that fixed weekly cardinality. No phase-specific role omission model exists in the Core path.

Classification: `TAPER_ROLE_REDUCTION_REQUIRES_NEW_STRUCTURAL_MODEL`. It would amend the stronger GEN.2A RunLayout invariant and is high-cost. Not selected.

## 7. Option D — eligibility/routing

Classification: `SHORT_ZERO_CORE_ELIGIBILITY_BLOCK_SUPPORTED`.

The domain issue is representability, not lack of 3D implementation support. Existing Appsel vocabulary already distinguishes readiness (`READY / CAUTION / NOT_READY`), and the production pipeline has a precedent: `CatalogPlanConfirmationService` rejects the known 4D 8-week explicit-zero taper/allocation infeasibility through a typed exception. That exact exception is persistence-oriented and must not be reused as the new product outcome, but it proves that fail-closed eligibility is established behaviour.

The correct boundary is derived, not `weeks >= 12`:

```text
ProjectedTaperWeeklyVolume = Round0.5(ProjectedPreTaperVolume × 0.53)
MinimumViableFullLayoutWeeklyVolume = 4 + 3 + 5 = 12 km

DirectCoreEligible only if
ProjectedTaperWeeklyVolume >= MinimumViableFullLayoutWeeklyVolume
and all other readiness/feasibility authorities pass.
```

Failure is `PRODUCT_INELIGIBLE`, with proposed typed product reason `THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`. It must not be reported as generic `TECHNICALLY_UNSUPPORTED` or “3D unsupported.” This reason is a future contract name, not code created here.

## 8. Comparative decision matrix

| Option | Preserves canonical Core? | Preserves RunLayout? | Preserves normal minima? | Preserves taper intent? | New architecture? | New domain values? | Evidence strength |
|---|---|---|---|---|---|---|---|
| A. Taper-specific minima | Yes | Yes | Yes, outside taper | Likely | Policy dispatch only | Yes for EASY/LONG | Partial: exact KEY only |
| B. 12 km taper floor | Yes | Yes | Yes | No for 8w; weakened for 10w | Small policy addition | Floor interaction | Mathematical, weak taper basis |
| C. Role/cardinality change | Core phases yes, weekly structure no | No | Yes for retained roles | Possibly | Major structural change | Role omission rule | Unsupported by current model |
| D. Eligibility/routing | Yes | Yes | Yes | Yes | Typed eligibility integration | Derived boundary/reason | Strongest repository + math support |
| E. Retune 3D multiplier | Yes | Yes | Possibly | Unknown | Versioned policy | New multiplier | Requires new evidence/decision |

## 9. Selected primary resolution

```text
3D_TAPER_CONFLICT_RESOLVED_BY_ELIGIBILITY_ROUTING
```

Priority rationale: this preserves readiness semantics, one canonical Core, RunLayout authority, genuine taper reduction, all approved normal minima, and avoids unsupported taper-specific numbers. It rejects only a readiness/horizon state the approved model cannot represent.

## 10. Exact derived eligibility rule

The authority is the projected taper weekly volume compared with the full-layout floor, not an arbitrary horizon literal. Projection must use the approved starting state, 7% preferred growth, independent 8% and 2 km caps, 0.5 km rounding/post-round validation, applicable peak clamp, and retained 0.53 taper multiplier.

```text
if ProjectedTaperWeeklyVolume < 12 km:
    PRODUCT_INELIGIBLE(
      THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT)
else:
    continue through all other readiness and feasibility checks
```

This is necessary, not sufficient, eligibility. It neither upgrades an otherwise NOT_READY user nor forces peak attainment.

## 11. Horizon/readiness examples

| State | Core weeks | Pre-taper km | Rounded taper km | Derived result |
|---|---:|---:|---:|---|
| Explicit zero (12) | 8 | 17.5 | 9.5 | PRODUCT_INELIGIBLE |
| Explicit zero (12) | 9 | 18.5 | 10.0 | PRODUCT_INELIGIBLE |
| Explicit zero (12) | 10 | 19.5 | 10.5 | PRODUCT_INELIGIBLE |
| Explicit zero (12) | 11 | 21.0 | 11.0 | PRODUCT_INELIGIBLE |
| Explicit zero (12) | 12 | 22.5 | 12.0 | Eligible for further checks |
| Explicit zero (12) | 13 | 24.0 | 12.5 | Eligible for further checks |
| Explicit zero (12) | 14 | 25.5 | 13.5 | Eligible for further checks |
| Missing (16) | 8 | 23.5 | 12.5 | Eligible for further checks |
| Missing (16) | 10 | 27.0 | 14.5 | Eligible for further checks |

These are deterministic illustrations of the approved policy and do not assert that missing evidence is equivalent to readiness. Other readiness authorities remain binding.

## 12. Remaining external-evidence need

No external evidence is required for the selected eligibility solution. External/product evidence would be required only if Appsel later chooses taper-specific EASY/LONG minima or a new 3D taper multiplier. Those alternatives remain unapproved.

## 13. VolumeProgressionVerifier technical gap

`TECHNICAL_IMPLEMENTATION_GAP`: `VolumeProgressionVerifier` currently recognizes an absolute-cap violation only when the ratio is also violated. Approved semantics require independent `ratio limit AND absolute-km limit` checks. This does not reopen a product decision and is separate from taper eligibility.

## 14. Impact on GEN2B2 invariants

GEN2B2-INV-001 through INV-014 remain intact. Add:

- `GEN2B3-INV-001`: Normal 4/3/5 minima remain unchanged; no complete taper-specific minimum triple is inferred.
- `GEN2B3-INV-002`: 0.53 is retained for V1 3D with disclosed 4D golden-fixture calibration provenance, not as a scientific optimum.
- `GEN2B3-INV-003`: Direct Core eligibility requires rounded projected taper weekly volume ≥12 km.
- `GEN2B3-INV-004`: Failure is product ineligibility with a typed taper-volume/full-layout reason, never generic 3D unsupported.
- `GEN2B3-INV-005`: Eligibility routing does not override other readiness, goal-feasibility or time-adequacy outcomes.
- `GEN2B3-INV-006`: Peak acceleration and taper flooring are forbidden as implicit repairs.

GEN2B2's prior blocked classification is superseded only by this explicit derived eligibility decision; its individual numeric approvals remain unchanged.

## 15. Parent GEN.2B closure status

The sole coupled-policy contradiction is resolved without a new contradiction:

```text
10K_GEN_2B_3D_CORE_POLICY_APPROVED
```

Implementation gaps (typed eligibility integration, independent cap verification, deterministic reconciliation tie-break) remain future engineering work, not domain blockers.

## 16. Files inspected

- `PHASE_10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_SYNTHESIS.md`
- `PHASE_10K_GEN_2B_2_3D_TRAINING_LOAD_PRODUCT_DECISION.md`
- `PHASE_10K_GEN_2B_3D_GENERALIZATION_POLICY.md`
- `PHASE4G_3B_0_VOLUME_SAFETY_POLICY_GOVERNANCE_NOTE.md`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1TaperSharpenPrescriptionPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogStageToWeekMaterializer.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/ReadinessEligibilityVerifier.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/VolumeProgressionVerifier.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/CoreEntryReadinessResolver.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPlanConfirmationService.cs`
- `plan-catalog/catalog/templates/ten-k-master.v6.json`
- published `TEN_K_WORKOUT_PROGRESSION_V1` and `EASY_STANDARD` artifacts inspected through repository search

## 17. Final classification

```text
3D_TAPER_CONFLICT_RESOLVED_BY_ELIGIBILITY_ROUTING
```

Parent: `10K_GEN_2B_3D_CORE_POLICY_APPROVED`.
