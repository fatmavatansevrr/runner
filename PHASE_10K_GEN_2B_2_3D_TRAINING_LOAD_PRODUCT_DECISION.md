# Phase 10K-GEN.2B.2 — 3D Training-Load Product Decision Closure

Scope: `TEN_K / INTERMEDIATE / 3D / CORE_PATH / 8–14 weeks`. Product/domain decision only. No production code, catalog JSON, test, or existing 4D policy is changed.

## 1. Binding evidence acknowledgment

This closure consumes `PHASE_10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_SYNTHESIS.md`. It preserves all GEN.2A invariants and GEN.2B structural decisions, including the single dynamic Core authority, ordered `KEY + EASY + LONG` layout, canonical phases, one-KEY workout identity, calendar rules, existing pace/intensity authority, no new cutback, and no Adaptation V1 requirement for Core 3D.

GEN.2B.1 found bounded evidence, not scientific optima. The values below are independently approved 3D Appsel defaults.

## 2. Evidence-status interpretation

- Values closely bounded by load evidence are `EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT`.
- Values selected primarily from coaching practice, internal consistency and mathematical feasibility are `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`.
- Neither label means scientifically proven optimum or universal physiological rule.

## 3. Starting-volume decisions

```text
Missing recent weekly volume = 16 km/week
Explicit zero recent weekly volume = 12 km/week
```

Classification: `3D_STARTING_VOLUME_POLICY_APPROVED` and `EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT`.

Missing means uncertainty about recent load; zero means evidence of no recent running load. They intentionally remain distinct. Valid `RecentWeeklyVolumeKm > 0` remains authoritative. Neither fallback overrides recent-longest-run or other readiness constraints.

Invariant classification: `3D_STARTING_VOLUME_FALLBACK_DOES_NOT_OVERRIDE_READINESS_SAFEGUARDS`.

## 4. Progression decisions

```text
PreferredMaxWeeklyIncreaseRatio = 7%
HardMaxWeeklyIncreaseRatio = 8%
AbsoluteWeeklyIncreaseCapKm = 2.0 km
```

Classification: `3D_WEEKLY_PROGRESSION_POLICY_APPROVED`; status `EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT`.

Science does not prove a universal 7%, 8% or 10% rule. Seven/eight percent lies inside the conservative evidence-compatible envelope; 2.0 km selects the conservative edge of the 2.0–2.5 km envelope. With only three runs, weekly growth is concentrated into fewer sessions, supporting the conservative absolute cap.

For prior weekly volume `W`, preferred candidate growth is conceptually `min(W × 7%, 2.0 km)`. Every non-taper result is independently bounded by 8%. Post-round validation is mandatory: `ROUNDING_MUST_NOT_SILENTLY_BYPASS_3D_PROGRESSION_HARD_CAP`.

The current `VolumeProgressionVerifier` cannot express the approved dual constraint without change: it sets `violatesAbsoluteCap = violatesRatio && changeKm > cap`, so an absolute-cap breach with ratio ≤8% passes. The future implementation must test ratio and absolute cap independently, while the planner must generate within both. This is an implementation gap, not a policy ambiguity.

## 5. Peak-band semantics

The draft 22–32 km/week band is approved here as a peak-volume policy envelope/clamp domain, not a mandatory attainment target:

```text
PEAK_VOLUME_BAND_IS_NOT_A_MANDATORY_ATTAINMENT_TARGET
PROGRESSION_SAFETY_PRECEDES_PEAK_BAND_ATTAINMENT
```

A constrained runner may peak below 22 km. Generation must never accelerate merely to reach the band floor. Values above 32 km are capped by the band when it applies.

## 6. Session allocation decisions

Before rounding and hard-constraint reconciliation:

```text
KEY_SESSION  = 35%
EASY_SUPPORT = 25%
LONG_RUN     = 40%
Total        = 100%
```

Classification: `3D_SESSION_DISTANCE_TARGET_ALLOCATION_APPROVED`; status `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`. These are coupled targets, not scientific optima.

## 7. Session minima and direct-entry floor

```text
Minimum KEY_SESSION total distance = 4.0 km
Minimum EASY_SUPPORT distance      = 3.0 km
Minimum LONG_RUN distance          = 5.0 km
```

Classification: `3D_SESSION_MINIMUM_VIABILITY_POLICY_APPROVED`; status `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`.

KEY means total session distance, including warm-up, repetitions, recovery and cool-down; it is not high-intensity distance. Since `4 + 3 + 5 = 12`, `3D_DIRECT_PRESCRIPTION_FLOOR = 12 KM/WEEK`. This is a mathematical floor for this layout, not physiological readiness and not permission to bypass eligibility.

## 8. Long-run policy

```text
PreferredLongRunShareRange = 38–42%
SelectedLongRunShare       = 40%
HardLongRunWeeklyShareCap  = 42%
```

Classification: `3D_LONG_RUN_SHARE_POLICY_APPROVED`; status `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`.

One fewer Easy run concentrates weekly distance; the 4D selected 33% is not transferred. Forty percent is feasible across 12–32 km. Forty-two percent accommodates 0.5 km rounding at the low-volume boundary while retaining a conservative ceiling.

The 40% result is only a candidate distance. Final allowance must also satisfy the 42% post-round cap, recent-longest-run/readiness safeguards, 5 km minimum, race/distance context, progression and rounding. `LONG_RUN_SHARE_IS_NOT_THE_ONLY_LONG_RUN_SAFETY_AUTHORITY`. No new numeric recent-longest-run threshold is approved.

## 9. Single-session load-spike interpretation

```text
SINGLE_SESSION_LOAD_SPIKE_EVIDENCE_RETAINED
NO_NEW_UNIVERSAL_10_PERCENT_HARD_RULE_APPROVED
```

The observational 30-day longest-run evidence remains relevant, but its 10% finding is not converted into a universal injury-prevention rule. Existing approved readiness safeguards remain applicable. Any new numeric session-spike authority requires a separate product decision.

## 10. Rounding and reconciliation principles

`DistanceGranularity = 0.5 km` is approved for 3D. Classification: `3D_ROUNDING_AND_RECONCILIATION_PRINCIPLES_APPROVED`.

The required order is: calculate target shares; enforce minima; round at 0.5 km; reconcile exactly to weekly volume; select an eligible adjustment minimizing target-share deviation; then revalidate all constraints. EASY may normally absorb a residual only when it stays valid. Reconciliation may not violate a minimum, the 42% long cap, either weekly progression constraint, or the exact weekly total. The former “second EASY absorbs reconciliation” rule is not reused. Any equal-choice tie-break is a future deterministic implementation detail.

## 11. Coupled-policy mathematical validation

Stress-test allocation uses the approved targets, 0.5 km rounding and least-deviation reconciliation:

| Week km | KEY | EASY | LONG | LONG share | Result |
|---:|---:|---:|---:|---:|---|
| 12 | 4.0 | 3.0 | 5.0 | 41.67% | PASS, boundary |
| 14 | 5.0 | 3.5 | 5.5 | 39.29% | PASS |
| 16 | 5.5 | 4.0 | 6.5 | 40.63% | PASS |
| 18 | 6.5 | 4.5 | 7.0 | 38.89% | PASS |
| 20 | 7.0 | 5.0 | 8.0 | 40.00% | PASS |
| 22 | 7.5 | 5.5 | 9.0 | 40.91% | PASS |
| 24 | 8.5 | 6.0 | 9.5 | 39.58% | PASS |
| 26 | 9.0 | 6.5 | 10.5 | 40.38% | PASS |
| 28 | 10.0 | 7.0 | 11.0 | 39.29% | PASS |
| 30 | 10.5 | 7.5 | 12.0 | 40.00% | PASS |
| 32 | 11.0 | 8.0 | 13.0 | 40.63% | PASS |

Every steady/non-taper stress-test row reconciles exactly, preserves all minima, keeps LONG below 42%, and prevents KEY/EASY collapse. The 22–32 peak band is fully representable. This grid alone is consistent; the taper-coupled test in §12 is not.

## 12. Core-horizon trajectory validation

Illustrative trajectories below apply 7% preferred growth, 2.0 km absolute cap, 8% hard cap, 0.5 km rounding with post-round cap enforcement, one final taper week at the existing 0.53 multiplier, and the 32 km peak ceiling. They do not alter phase allocation and are not implementation pseudocode.

| Start | Core | Non-taper weekly volumes (km) | Reachable pre-taper peak | Taper week | Band classification |
|---:|---:|---|---:|---:|---|
| 12 | 8w | 12, 12.5, 13.5, 14.5, 15.5, 16.5, 17.5 | 17.5 | 9.5 | Below band, valid |
| 12 | 10w | 12 … 17.5, 18.5, 19.5 | 19.5 | 10.5 | Below band, valid |
| 12 | 12w | 12 … 19.5, 21, 22.5 | 22.5 | 12.0 | Within band |
| 12 | 14w | 12 … 22.5, 24, 25.5 | 25.5 | 13.5 | Within band |
| 16 | 8w | 16, 17, 18, 19, 20.5, 22, 23.5 | 23.5 | 12.5 | Within band |
| 16 | 10w | 16 … 23.5, 25, 27 | 27.0 | 14.5 | Within band |
| 16 | 12w | 16 … 27, 29, 31 | 31.0 | 16.5 | Within band |
| 16 | 14w | 16 … 31, 32, 32 | 32.0 | 17.0 | Upper-band constrained |

Ellipses abbreviate the already-shown monotonic sequence, not hidden jumps. A 12 km start in 8 or 10 weeks legitimately remains below 22 km. `Not reaching the peak-band floor because approved progression/readiness constraints bind is not itself a policy failure.`

However, the taper-coupled result is blocking. The approved minima require 12 km in every week that materializes all three frozen roles. Existing 0.53 taper produces only 9.5 km for the 8-week zero-fallback path and 10.5 km for the 10-week zero-fallback path. Neither can satisfy `KEY 4 + EASY 3 + LONG 5 = 12`. The 12-week zero path lands exactly at 12 km; longer or 16 km-start examples remain feasible.

Exact conflict requiring product-owner review:

```text
Existing TaperVolumeMultiplier = 0.53
+ ZeroFallback = 12 km
+ 8/10-week approved progression
+ frozen three-role weekly materialization
CONFLICTS WITH
minimum total session volume = 4 + 3 + 5 = 12 km
```

No selected value is changed here. Closure requires a separate explicit rule defining at least one of: taper-week minimum relaxation, a taper floor, taper role/materialization variation, or eligibility/routing that prevents the conflicting short zero-fallback paths. Each would alter a frozen or selected policy and therefore cannot be inferred in this phase.

## 13. Policy versioning requirements

Future implementation must introduce or select explicit versioned authorities for:

1. 3D missing/zero starting-volume policy;
2. 3D weekly progression policy;
3. 3D session-allocation/minimum/rounding policy;
4. 3D long-run share and multi-constraint policy.

They must carry `TEN_K / INTERMEDIATE / 3D` provenance and be consumed through selected policy contracts, not scattered literals. This phase creates no runtime or catalog artifact.

## 14. Relationship to existing 4D policies

Conceptually reused: ratio-plus-absolute progression guards, 0.5 km granularity, real recent-volume authority, recent-readiness safeguards, and exact-week reconciliation.

Not reused numerically by assumption: 4D missing/zero provenance, 4D allocation formula, two-EASY reconciliation, and 4D `30–36 / 33 / 40` long-run policy. Coincidentally equal values receive independent 3D version provenance.

## 15. Approved policy table

| Policy | Approved 3D value | Evidence status | Future authority |
|---|---:|---|---|
| Missing starting volume | 16 km | EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT | versioned 3D starting-volume policy |
| Zero starting volume | 12 km | EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT | versioned 3D starting-volume policy |
| Preferred weekly increase | 7% | EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT | versioned 3D progression policy |
| Hard weekly increase | 8% | EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT | versioned 3D progression policy |
| Absolute weekly cap | 2.0 km | EVIDENCE_CONSTRAINED_PRODUCT_DEFAULT | versioned 3D progression policy |
| KEY target share | 35% | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation policy |
| EASY target share | 25% | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation policy |
| LONG target share | 40% | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation/long-run policy |
| KEY minimum | 4.0 km | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation policy |
| EASY minimum | 3.0 km | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation policy |
| LONG minimum | 5.0 km | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation/long-run policy |
| Long preferred range | 38–42% | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D long-run policy |
| Long selected share | 40% | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D long-run policy |
| Long hard cap | 42% | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D long-run policy |
| Rounding granularity | 0.5 km | PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE | versioned 3D allocation policy |

## 16. Named invariant set

- `GEN2B2-INV-001`: Valid real recent weekly-volume evidence remains authoritative over fallback defaults.
- `GEN2B2-INV-002`: Missing and explicit-zero readiness remain distinct fallback states.
- `GEN2B2-INV-003`: A 3D starting fallback never overrides stronger readiness constraints.
- `GEN2B2-INV-004`: Weekly progression is independently bounded by ratio and absolute-distance constraints.
- `GEN2B2-INV-005`: Peak-band attainment never overrides progression/readiness constraints.
- `GEN2B2-INV-006`: Pre-round targets are 35% KEY / 25% EASY / 40% LONG.
- `GEN2B2-INV-007`: Minimum session distances are 4 km KEY / 3 km EASY / 5 km LONG.
- `GEN2B2-INV-008`: Direct frozen-layout materialization requires at least 12 km/week.
- `GEN2B2-INV-009`: LONG selected share is 40%, preferred range 38–42%, post-round hard cap 42%.
- `GEN2B2-INV-010`: Long-run percentage alone is never the complete long-run safety authority.
- `GEN2B2-INV-011`: No universal injury-prevention 10% session rule is inferred from observational evidence.
- `GEN2B2-INV-012`: After 0.5 km rounding, sessions reconcile exactly while preserving every hard constraint.
- `GEN2B2-INV-013`: Rounding is followed by independent ratio-cap, absolute-cap and long-share-cap validation.
- `GEN2B2-INV-014`: A below-band reachable peak is valid when progression/readiness constraints bind.

## 17. Parent GEN.2B closure status

The four requested values are individually selected, but the coupled policy is not contradiction-free because short zero-fallback paths cannot preserve both existing taper behaviour and the approved session minima. Therefore the parent phase is **not** upgraded to `10K_GEN_2B_3D_CORE_POLICY_APPROVED`.

The verifier semantic gap and reconciliation tie-break remain implementation work. The taper/minimum conflict is a product-policy blocker and requires explicit owner resolution before implementation.

Post-phase reconciliation: GEN.2B.3 subsequently resolved this blocker through derived eligibility routing (`ProjectedTaperWeeklyVolume >= 12 km`) without changing any numeric decision in this document. See `PHASE_10K_GEN_2B_3_3D_TAPER_MINIMUM_CONFLICT_DECISION.md`. Accordingly, this section records GEN.2B.2's historical result; the current parent status is `10K_GEN_2B_3D_CORE_POLICY_APPROVED` under GEN.2B.3 authority.

## 18. Files and evidence inspected

- `PHASE_10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_SYNTHESIS.md`
- `PHASE_10K_GEN_2B_3D_GENERALIZATION_POLICY.md`
- `PHASE_10K_GEN_2A_FREQUENCY_ARCHITECTURE_AUTHORITY_DECISION.md`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/V1MissingReadinessStartingVolumePolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/VolumeProgressionVerifier.cs`
- `plan-catalog/catalog/templates/ten-k-master.v6.json`
- `plan-catalog/docs/canonical/golden-fixture-v3/progression_rules_v2.yaml`

## 19. Final classification

```text
10K_GEN_2B_2_3D_TRAINING_LOAD_PRODUCT_POLICY_BLOCKED
```

Blocking decision: how taper weeks below the 12 km direct-prescription floor interact with frozen three-role materialization and the approved 4/3/5 km minima. No implementation, activation or publication is authorized by this document.

Supersession note: `10K_GEN_2B_2_3D_TRAINING_LOAD_PRODUCT_POLICY_BLOCKED` remains the correct result of GEN.2B.2 at the time it ran. Its blocker is now closed by GEN.2B.3 classification `3D_TAPER_CONFLICT_RESOLVED_BY_ELIGIBILITY_ROUTING`; no GEN.2B.2 numeric value was amended.
