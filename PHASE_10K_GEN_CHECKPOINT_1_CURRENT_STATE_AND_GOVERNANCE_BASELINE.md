# Phase 10K-GEN.CHECKPOINT.1 — Current State & Governance Baseline

**Documentation consolidation only. No code touched, no test re-run, no new decision made. Every claim below traces to a specific prior phase document; anything that doesn't is marked `UNTRACED`.**

## 0. Correction to the phase premise — checked, not assumed

Section C of this phase's prompt asks to "mirror GEN.4.5's Section 3-style map." **No `PHASE_10K_GEN_4_5_*` (or any `4.5`-numbered) document exists anywhere in the repository** — checked via directory listing. The real authority-map source used below is `PHASE_10K_GEN_4_LEVEL_AUTHORITY_AUDIT.md` and `PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md`, the two real documents that actually contain authority-inventory content. This follows the same verify-before-trusting pattern already established for the "GEN.5B" premise correction in GEN.5C §0.

## A. Public support matrix (current, real)

| Level | 3D | 4D |
|---|---|---|
| Beginner | **UNSUPPORTED** — proven never-representable at Core (GEN.5A.2 §4, GEN.5C §2) | **PUBLIC** (GEN.4E) |
| Intermediate | **PUBLIC** (GEN.3B) | **PUBLIC** (original pre-engagement pilot, predates GEN.0) |
| Advanced | NOT YET INVESTIGATED | NOT YET INVESTIGATED |

## B. Unsupported cells — exact reasoning

**Beginner×3D×Core (8-14wk)**: `GEN.5A.2 §4` derived the exact structural reason — 3D's taper mechanism requires `Round0.5(X × 0.53) ≥ 12.0`, solving to `X ≥ 22.17km` pre-taper, i.e. the first reachable value clearing the floor is 22.5km. The evidence-grounded peak band (16.0-20.0km, `GEN.5A.2 §2`, real sources: Hal Higdon Novice 10K peak 16.90km, McMillan 10K Level 1 Beginner 16.1-32.2km) never reaches 22.2km at any horizon or readiness state. `GEN.5C §3` determined the existing `V1CatalogPilotIdentityPolicy` allow-list (which never admitted `(Beginner,3)`, per `GEN.4E`) is the correct, sufficient, already-implemented mechanism — no new typed exception was created or needed.

## C. Canonical distance/frequency/level authority map

Real, current production authorities (file paths verified against the actual source tree across GEN.0-GEN.5C; anything not explicitly confirmed by a cited phase is marked accordingly):

| Authority | File | Responsibility | Competing authority? |
|---|---|---|---|
| `V1CatalogPilotIdentityPolicy` | `RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` | Public identity allow-list + candidate-key resolution, Level-aware since GEN.4E | None — single owner, confirmed `GEN.4E §3` |
| `RUN_LAYOUT_*` artifacts | `plan-catalog/catalog/layouts/*.json` | Canonical Frequency/role-cardinality authority (`RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY`, frozen `GEN.2A`) | None |
| `TEMPLATE_COMBINATION` artifacts | `plan-catalog/catalog/combinations/*.json` | Compatibility/version manifest only (`COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST`, frozen `GEN.2A`) | None |
| `LEVEL_MODIFIER` artifacts | `plan-catalog/catalog/level-modifiers/*.json` | Level-owned eligible-workout/experience/progression-reference data | None |
| `VolumeSafetyPolicy` (+ `ResolvedPeakReference`) | `RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` | Numeric growth/taper/long-run-share constants per policy instance (`Default`, `ThreeDayIntermediate`, `BeginnerFourDay`) | None — dispatched by `CatalogVolumeAndLongRunPlanner.Build`'s explicit Level/DaysPerWeek checks |
| `CatalogVolumeAndLongRunPlanner` | `RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` | Single volume/long-run/taper planner for all cells; provenance-agnostic re: `ResolvedPeakReference` (`GEN.4C.3` Path Z, `GEN.4D §B`) | None |
| `CatalogProductIneligibleException` hierarchy | `RuntimeCatalog/Prescription/Volume/CatalogVolumeExceptions.cs` | Typed product-ineligibility for identity-supported-but-numerically-infeasible cases | None — see §F |
| `V1ThreeDaySessionVolumeAllocationPolicy` | `RuntimeCatalog/Prescription/Session/V1ThreeDaySessionVolumeAllocationPolicy.cs` | 3D session-role minimums/shares — confirmed Level-agnostic (`GEN.5 §A`), despite an Intermediate-named `PolicyKey` string | None |
| `peak-volume-bands.v4.json` | `plan-catalog/catalog/policies/` | Distance×Level×RunsPerWeek→[min,max] cross-axis data (`PEAK_VOLUME_BAND_IS_LEGITIMATE_CROSS_AXIS_POLICY_DATA`, frozen `GEN.4A`) | None |
| `LivePlanPreviewRouting` (`V1LiveCatalogPilotRoutingPolicy`) | `RuntimeCatalog/PreviewRouting/LivePlanPreviewRouting.cs` | Public route decision (CatalogLive/Legacy/etc.), delegates identity check to `V1CatalogPilotIdentityPolicy` since `GEN.4E` | None |

`WeeklyWindowPartitioner` and `WeeklyLoadDecisionAggregator`, named in this phase's own prompt as items to include, were **not found** anywhere in the real source tree (`grep` for both names returns zero matches outside this document) — marked `UNTRACED`, not fabricated into the table above.

## D. Peak-volume-band registry

| Distance×Level×Frequency | Band | Provenance | Status |
|---|---|---|---|
| 10K×Intermediate×3D | 22-32 km | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` — approved as a draft evidence envelope in `GEN.2B.2 §"Peak volume"` (not `GoldenFixtureDerived`; the 3D branch of the planner doesn't consume `ResolvedPeakReference` at all, only the band's `MaximumKm` as a clamp — confirmed `GEN.5A §1`) | PUBLIC |
| 10K×Intermediate×4D | 30-42 km | Band range itself: pre-existing catalog data, provenance not established by any GEN.0-5C phase — `UNTRACED`. The distinct `ResolvedPeakReference` point value (38km) used by the 4D planner branch is separately tagged `GoldenFixtureDerived` (`PHASE4G_3B_0` governance note, `GEN.4C.4 §9`) | PUBLIC |
| 10K×Intermediate×5D | 36-50 km | `UNTRACED` — exists in `peak-volume-bands.v4.json` but no GEN.0-5C phase derived, approved, or even investigated it; pre-existing catalog data | NOT LAUNCHED |
| 10K×Beginner×4D | 18-24 km | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, reference 21.0km (`GEN.4C.3` Path Z, `GEN.4C.4 §9`) | PUBLIC |
| 10K×Beginner×3D | 16-20 km | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, reference 17.0km (`GEN.5A.2 §2`) | **FROZEN, NEVER LAUNCHES** (`GEN.5C §1`) — no code implements it |

## E. ResolvedPeakReference provenance model

Established `GEN.4C.3` (Path Z), restated `GEN.4D §B`: `ResolvedPeakReference{Value, Provenance}`, `Provenance ∈ {GoldenFixtureDerived, ProductDefaultWithEvidenceEnvelope}`. The planner (`CatalogVolumeAndLongRunPlanner`) is provenance-agnostic — it consumes only `.Value`, never branches on `.Provenance`; the tag is audit metadata only.

| Instance | Value | Provenance |
|---|---|---|
| `VolumeSafetyPolicy.Default` (Intermediate×4D) | 38.0 km | `GoldenFixtureDerived` (unchanged, re-tagged not re-derived, `GEN.4D §B`) |
| `VolumeSafetyPolicy.ThreeDayIntermediate` | 22.5 km | `ProductDefaultWithEvidenceEnvelope` |
| `VolumeSafetyPolicy.BeginnerFourDay` | 21.0 km | `ProductDefaultWithEvidenceEnvelope` (`GEN.4C.3`) |
| Beginner×3D (frozen, unimplemented) | 17.0 km (reference only, band 16-20) | `ProductDefaultWithEvidenceEnvelope` (`GEN.5A.2`) — never instantiated in code |

Note: `ThreeDayIntermediate.ResolvedPeakReference` (22.5km) is present in the record but **not actually consumed** by the 3D planner branch, which uses ratio-compounding from the starting volume instead (confirmed `GEN.5 §B`, `GEN.5A §1`) — the field exists for record-shape consistency, not because the 3D algorithm reads it.

## F. Typed eligibility patterns

`CatalogVolumePlanningException` (abstract) → `CatalogProductIneligibleException` (abstract, added `GEN.4D.2`) → `ThreeDayCoreProductIneligibleException` / `BeginnerFourDayCoreProductIneligibleException` (both sealed).

- **This pattern applies to**: identities that *are* publicly supported but fail for a *specific* horizon/readiness combination — Intermediate×3D taper-floor conflicts (`GEN.2B.3`, `GEN.3A`), Beginner×4D explicit-zero at 8-12wk specifically (`GEN.4C.4`, 13-14wk of the *same identity* is eligible).
- **This pattern does NOT apply to**: identities that are never eligible under any input (Beginner×3D). For those, the correct mechanism is upstream identity-allow-list rejection (`V1CatalogPilotIdentityPolicy.IsSupportedIdentity`), not a planner-level exception — determined explicitly in `GEN.5C §3`, no third pattern was invented.

## G. Core 8-14 containment state (Runway/LongHorizon)

Confirmed closed for every currently-launched cell, per each cell's own containment section — not assumed:

- Intermediate×3D: Runway/LongHorizon not widened (`GEN.3B §8/§17`, cited in original engagement scope).
- Intermediate×4D: Runway/LongHorizon exist as a **separate, real, already-implemented feature** (`TenKPreparationRunwayDarkOrchestrator`, `LongHorizonRollingWindowActivationService` and related — found live in the working tree, `GEN.2A §1`'s "current-code reconciliation" note) — this is Intermediate×4D-specific pre-existing functionality, not part of this 10K-Generalization engagement's scope, and not investigated by any GEN.0-5C phase.
- Beginner×4D: Runway/LongHorizon confirmed NOT widened, real HTTP test (`GEN.4E §7`, `NonCoreFourDayBeginnerHorizons_RemainUnactivated`).
- Beginner×3D: never reaches Runway/LongHorizon at all (rejected at identity level); its own Runway/LongHorizon viability was never investigated (`GEN.5C §4`, explicit).

## H. Open technical debt registry

| ID | Description | Origin phase | Status |
|---|---|---|---|
| `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` | 3D uses ratio-compounding growth; 4D uses fixed-reference linear interpolation — divergence may be an intentional Frequency distinction or unexamined historical specialization | `GEN.4C.1`/`GEN.4C.4 §9` | Non-blocking, not scheduled |
| `TD-LEGACY-FALLBACK-PATH-UNTRACED-001` | Exact status/message a Legacy-routed (non-pilot-identity) request receives was not traced — confirmed non-200/non-500 only, real behavior of the SQL fallback path unknown | `GEN.5C §4` | Non-blocking, not scheduled |
| Peak-volume-band provenance gap (Intermediate×4D 30-42km band range, Intermediate×5D 36-50km band) | Neither band's numeric origin was ever established by any phase in this engagement — both are pre-existing catalog data | `GEN.CHECKPOINT.1 §D` (identified here, not previously named as debt) | Non-blocking, not scheduled |
| Cross-training evidence-comparison caveat | Beginner×3D's evidence source (Higdon) included cross-training days this system doesn't model (`CrossTrainDay`, out-of-scope since `GEN.1`); running-only comparison, not total-training-stress comparison | `GEN.5C §5` | Non-blocking, not scheduled |

## I. Public vs internal/gated state summary

| Cell | Reachability |
|---|---|
| Intermediate×4D | Public HTTP (pre-existing pilot) |
| Intermediate×3D | Public HTTP (`GEN.3B`) |
| Beginner×4D | Public HTTP (`GEN.4E`) |
| Beginner×3D | **Never launches** — identity-level rejection is permanent by design (`GEN.5C`), not a staged/internal-gated state awaiting future widening |
| Advanced×4D, Advanced×3D, Beginner×5D+, all other Level/Frequency pairs | Unreachable, no-nearest-match, not investigated |

## J. One-line phase ledger

| Phase | Outcome | Final classification |
|---|---|---|
| GEN.0 | Read-only baseline audit of the current generation pipeline | `10K_GENERALIZATION_CURRENT_STATE_BASELINE_READY` |
| GEN.1 | Level×Frequency architecture feasibility audit; found 3D not architecturally blocked | `10K_GENERALIZATION_ARCHITECTURE_AUDIT_READY_FOR_DECISION_PHASE` |
| GEN.2A | Froze Frequency composition architecture (single dynamic Core skeleton, RUN_LAYOUT as Frequency authority) | `10K_GEN_2A_FREQUENCY_ARCHITECTURE_AUTHORITY_APPROVED` |
| GEN.2B | Approved frozen 3D structural policy (role cardinality, session shares) pending domain decisions | `10K_GEN_2B_3D_CORE_POLICY_REQUIRES_DOMAIN_DECISIONS` |
| GEN.2B.1 | 3D training-load literature/evidence synthesis | `10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_READY_FOR_PRODUCT_DECISION` |
| GEN.2B.2 | 3D training-load product decision (session minimums 4/3/5km, 35/25/40% shares, 22-32km peak band, long-run 38-42%/40%/42%) | `10K_GEN_2B_2_3D_TRAINING_LOAD_PRODUCT_POLICY_BLOCKED` |
| GEN.2B.3 | Resolved 3D taper-minimum conflict via typed eligibility routing | `3D_TAPER_CONFLICT_RESOLVED_BY_ELIGIBILITY_ROUTING` |
| GEN.3A | First Intermediate×3D implementation attempt | `10K_GEN_3A_INTERMEDIATE_3D_CORE_IMPLEMENTATION_BLOCKED` |
| GEN.3A.1 | Verification closure attempt; remaining backend-suite/partition gaps | `10K_GEN_3A_INTERMEDIATE_3D_CORE_IMPLEMENTATION_BLOCKED` |
| GEN.3A.2 | Final verification; resolved remaining allocation/prescription/containment/backend-suite gaps | `10K_GEN_3A_INTERMEDIATE_3D_CORE_IMPLEMENTED_AND_GATED` |
| GEN.3A.3 | Retroactive audit of whether a missing `xunit.runner.json` testhost copy weakened historical Adaptation V1 evidence | `ADAPTATION_V1_CONCURRENCY_EVIDENCE_REVALIDATED` (+ `HISTORY_INCONCLUSIVE` on exact provenance) |
| GEN.3B | Intermediate×3D public activation | `10K_GEN_3B_INTERMEDIATE_3D_CORE_PUBLICLY_ACTIVE` |
| GEN.4 | Level authority audit (peak-volume-band as the sole material Level-driven effect) | `LEVEL_AUTHORITY_AUDIT_READY_FOR_DECISION_PHASE` |
| GEN.4A | Level authority decisions resolved (Beginner=NEW mapping, Level as first-class identity dimension, 11 invariants) | `LEVEL_AUTHORITY_DECISIONS_APPROVED` |
| GEN.4B | Beginner×4D training-load/workout literature evidence synthesis | `BEGINNER_4D_EVIDENCE_ENVELOPE_READY_FOR_PRODUCT_DECISION` |
| GEN.4C | Beginner×4D product policy decision closure (12.0/9.5/9.0/0.53/17.0 frozen) | `BEGINNER_4D_PRODUCT_POLICY_APPROVED_WITH_CATALOG_GAP` |
| GEN.4C.1 | Corrected 8-week eligibility matrix using the real (not approximated) planner algorithm | `BEGINNER_4D_MISSING_8W_ELIGIBILITY_APPROVED` |
| GEN.4C.2 | Found the 22.0km peak reference improperly derived from Intermediate; reopened | `BEGINNER_4D_22KM_REFERENCE_DECISION_REOPENED` |
| GEN.4C.3 | Resolved via Path Z (provenance-tagged authority model); selected 21.0km independently | `BEGINNER_4D_PEAK_REFERENCE_RESOLVED_FINAL` |
| GEN.4C.4 | Final numeric semantics/governance closure before implementation | `BEGINNER_4D_GEN4C_FINAL_NUMERIC_SEMANTICS_VERIFIED` |
| GEN.4D | Beginner×4D core implementation (internal/gated) | `BEGINNER_4D_CORE_IMPLEMENTED_AND_GATED` |
| GEN.4D.1 | Real evidence closure found 2 real gaps (stale test count, missing exception catch-arm) | `BEGINNER_4D_CORE_REGRESSION_FOUND` |
| GEN.4D.2 | Fixed both gaps structurally (shared exception base, corrected count) | `BEGINNER_4D_CORE_IMPLEMENTATION_COMPLETE` |
| GEN.4E | Beginner×4D public activation; found and fixed 2 more real test-staleness issues from real regression | `BEGINNER_4D_CORE_PUBLICLY_ACTIVE` |
| GEN.5 | Beginner×3D composition attempt; found universal explicit-zero ineligibility, missing-readiness ineligible 8-11wk | `BEGINNER_3D_COMPOSITION_CONFLICT_FOUND` |
| GEN.5A | Peak-band evidence envelope; found unresolved tension vs. proportional-scaling sanity check | `BEGINNER_3D_PEAK_BAND_EVIDENCE_ENVELOPE_READY_FOR_PRODUCT_DECISION` |
| GEN.5A.1 | Quantified the tension: both hypothesis ceilings (13/18km) make the entire matrix ineligible | `PEAK_BAND_TENSION_QUANTIFIED_READY_FOR_PRODUCT_DECISION` |
| GEN.5A.2 | Resolved with real cited evidence (Higdon/McMillan); band 16-20km; all readiness states ineligible | `BEGINNER_3D_PEAK_BAND_FROZEN_AND_MATRICES_RECOMPUTED` |
| GEN.5C | Formalized full non-support for the entire cell via the existing identity-allow-list mechanism | `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` |

## K. Open questions for the next wave (Advanced×4D) — restated from GEN.4, not answered

Per `GEN.4 §"Beginner×4D and Advanced×4D feasibility tables"`: both classified `NEEDS_ARCHITECTURE_CHANGE`, narrow/bounded. Specifically flagged and unresolved: Advanced's high-volume capacity ceiling (no evidence envelope exists yet), workout/component dosage limits at higher volumes (QualitySessionCount eligibility/cap questions, `GEN.4A`'s `QUALITY_SESSION_COUNT_IS_LEVEL_ELIGIBILITY_CAP_ONLY` decision applies but no Advanced-specific values exist), and Advanced's own peak-volume-band (no entry exists in `peak-volume-bands.v4.json` for Advanced at any Frequency — confirmed by inspection of the same file read in `GEN.5 §C`).

## L. Explicitly deferred (not investigated, not scheduled)

- **5D+ second-KEY architecture wave**: named `NEEDS_ARCHITECTURE_CHANGE` in `GEN.1`/`GEN.4`, never revisited since.
- **Beginner×3D Runway/LongHorizon (15wk+)**: never investigated (`GEN.5C §4`, explicit).
- **2D, Expert, CrossTrainDay, DoubleSessionDay**: confirmed out-of-scope since `GEN.1 §10` ("NOT investigated as V1 feasibility targets anywhere in this audit"); untouched since.

## M. Final classification

```
10K_GENERALIZATION_CHECKPOINT_1_BASELINE_READY
```

Note: the phase prompt's requested classification string was `10K_GENERALIZATION_CHECK` (truncated/incomplete in the prompt as given). Used the evidently-intended full form above rather than the literal truncated string, since emitting a classification token that doesn't match this document's own established naming convention (`{SCOPE}_{OUTCOME}_{STATE}`) would itself be a fidelity error of the same kind this checkpoint exists to prevent.
