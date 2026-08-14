# Phase 10K-GEN.4 — Level Authority Audit

**Audit only. No production code written or modified. No catalog artifacts published. No product numeric decisions made.**

## 1. GEN.3A.3 prerequisite result

Satisfied. GEN.3A.3's binding results (`HISTORY_INCONCLUSIVE` for commit provenance only, `ADAPTATION_V1_CONCURRENCY_EVIDENCE_REVALIDATED`, `HISTORICAL_NUMERIC_TEST_COUNTS_REMAIN_VALID`) are accepted as-is. GEN.4 does not reopen the xUnit test-evidence audit; its residual governance debt (commit-atomicity practice, a permanent runner-config presence assertion) is explicitly out of scope here.

## 2. Current generalization baseline (treated as established)

Verified directly against current code before proceeding (not merely cited from documents): `RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` now reads:
```csharp
public const RunningBackground Level = RunningBackground.Intermediate;
public const int DaysPerWeek = 4;
public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
public const int CandidateVersion = 10;
public const string ThreeDayCandidateKey = "TEN_K__3D__INTERMEDIATE";
public const int ThreeDayCandidateVersion = 1;

public static bool IsSupportedIdentity(...) =>
    goalType == GoalType && goalDistance == GoalDistance && level == Level && daysPerWeek is 3 or DaysPerWeek;

public static (string CandidateKey, int CandidateVersion) ResolveCandidate(int daysPerWeek) => daysPerWeek switch
{
    3 => (ThreeDayCandidateKey, ThreeDayCandidateVersion),
    DaysPerWeek => (CandidateKey, CandidateVersion),
    _ => throw new ArgumentOutOfRangeException(...)
};
```
Real 3D catalog artifacts confirmed on disk (`plan-catalog/catalog/combinations/ten-k-3d-intermediate.v1.json`, `plan-catalog/catalog/layouts/run-layout-3d.v1.json`, plus a published `0.7.3-pilot` release manifest containing both 3D and 4D bundles). TEN_K/Intermediate/3D CORE_PATH is confirmed publicly active, TEN_K/Intermediate/4D remains the existing baseline. Both together demonstrate Frequency variation within one fixed Level. GEN.4 audits the orthogonal axis (fixed Frequency=4D, varying Level: Beginner/Intermediate/Advanced) to determine whether Frequency and Level are genuinely independent compositional axes. Confirmed still out of scope/inactive: Beginner×3D, Advanced×3D, Intermediate×5D+, 3D on Runway/LongHorizon, 2D, Experienced/"Expert".

Binding architectural decisions from prior phases (`CORE_SKELETON_AUTHORITY_SINGLE_DYNAMIC`, `RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY`, `COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST`, `SINGLE_CANONICAL_10K_CORE_APPROVED`, `BEHAVIORAL_EQUIVALENCE_REQUIRED`, `SUPPORT_IS_SEPARATE_FROM_ROLLOUT`, `NO_NEAREST_MATCH_FALLBACK`) are treated as frozen and not reopened; all findings below are checked for compatibility with them, not against them.

## 3. GEN.0 B3 current-code re-verification

Re-audited fresh against current code (not cited from the old document). Findings by sub-area:

- **A1 Peak-volume**: `CatalogPeakVolumeBandLoader.LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct)` remains a genuine, level-agnostic composite-key lookup. Confirmed still the **sole material Level-driven data-selection effect**. Refinement over GEN.0: the live artifact (`plan-catalog/catalog/policies/peak-volume-bands.v3.json`) now contains **only 3 INTERMEDIATE rows**, whereas an earlier release (`plan-catalog/artifacts/appsel-plan-catalog/1.0.0/policies/PEAK_VOLUME_BANDS_V1.v1.json`) contained 12 rows across all four experience tiers (NEW/INTERMEDIATE/ADVANCED/EXPERIENCED × 3 frequencies). The data regressed to Intermediate-only as pilot scope narrowed — this is a real historical fact, not a hypothesis: the schema and Beginner/Advanced row content already existed once and were removed, not merely "never authored."
- **A2 Workout eligibility**: zero Level-conditional branching found anywhere in `Prescription/Session/**` or the resolvers — confirmed identical to GEN.0.
- **A3 Phase/Core**: `PlanCatalogBundleLoader.ReadPhaseAllocations` and `CatalogVolumeAndLongRunPlanner` take no Level parameter; one canonical 10K Core (`TEN_K_MASTER v6`) remains independent of Level, confirmed by direct read (not assumed).
- **A4 Prescription**: `VolumeSafetyPolicy.cs` is **no longer a single record** (Phase 4G.3B added `ThreeDayIntermediate` alongside `Default`), but the new dimension it varies over is **Frequency, not Level** — both instances remain Intermediate-only in scope/name, selected via `ReferenceEquals(_policy, ThreeDayIntermediate)` gated by `DaysPerWeek == 3`. No Level-keyed selection mechanism exists anywhere.
- **A5 Calendar**: no Level influence found over PreferredDays, spacing, or role cardinality — structural cardinality remains exclusively RunLayout/Frequency-owned, consistent with the binding decision.
- **A6 Adaptation V1**: exhaustive fresh grep for `RunningBackground` across the entire `RuntimeCatalog/Schedule/LongHorizon/Adaptation/**` tree returns **zero matches**. Confirmed with current evidence, not inherited.

**Net: GEN.0's finding holds, essentially unchanged, against current code.** One incidental finding surfaced during re-verification: the recent 3D generalization (GEN.2/GEN.3B) was scoped **only** to the CORE_PATH identity policy and volume planner — the LongHorizon/RollingActivation and PreparationRunwayOrchestration subsystems' `DaysPerWeek != 4` gates were **not** updated in lockstep and remain 4D-only, meaning "3D support" is not yet uniform across horizon families (a Frequency-scope observation, orthogonal to this phase's Level focus, noted here since it directly informs §15's duplication table).

## 4. Canonical Level vocabulary

Two intentionally separate vocabularies exist, confirmed distinct and non-conflicting:
- **Backend domain enum**: `RunningApp.Domain/Enums/RunningBackground.cs`: `{Beginner, Intermediate, Advanced, Experienced}` — the sole canonical backend/API contract (doc comment: "these four values are the entire active product contract").
- **Catalog string vocabulary**: `"experience"` field values `NEW / INTERMEDIATE / ADVANCED / EXPERIENCED` (confirmed in `plan-catalog/schemas/peak-volume-band-policy.schema.json`).

**Important naming mismatch, confirmed real, not assumed**: the catalog uses `"NEW"` where the backend enum uses `Beginner`. **No mapping table between `RunningBackground.Beginner` and catalog `"NEW"` exists anywhere in the repository today.** `Advanced`↔`"ADVANCED"` and `Experienced`↔`"EXPERIENCED"` have no such mismatch. The one explicit, tested cross-vocabulary pairing that exists is `RunningBackground.Intermediate` ↔ `"INTERMEDIATE"` (`V1CatalogPilotIdentityPolicy.cs:45,49`).

Three JSON converters exist for the backend enum, classified: `RunningBackgroundCanonicalJsonConverter` (default, canonical-only) = **LEGITIMATE_TRANSLATION**; `RunningBackgroundJsonConverter` (narrowly scoped to `ResolverInputSnapshot.Level` only, accepts 3 legacy aliases) = **LEGACY_COMPATIBILITY**; `RunningBackgroundCompatibilityConverter` (EF value converter, same legacy aliases) = **LEGACY_COMPATIBILITY**. No `CONFLICTING_AUTHORITY` found — the two vocabularies are a deliberate, cleanly-separated boundary (backend onboarding taxonomy vs. catalog training-science taxonomy) with exactly one explicit tested pairing, not drift.

## 5. Level authority map (material Intermediate references, exhaustive)

21 production references to `RunningBackground.Intermediate`/`"INTERMEDIATE"` across `RuntimeCatalog/**`, classified:

| Category | Count | Representative sites | Classification |
|---|---|---|---|
| Primary owning definition | 1 | `V1CatalogPilotIdentityPolicy.cs:45,49` | **IDENTITY_ROUTING** |
| Genuinely layered defensive re-check | 1 | `LivePlanPreviewRouting.cs:242` (`LivePlanPreviewRouteDecisionValidator.Validate`) | **INTENTIONALLY_LAYERED_VALIDATION** — re-asserts decision-object internal consistency, not a fresh identity computation |
| Independent duplicate reject-gates | 4 | `TenKPreparationRunwayDarkOrchestrator.cs:337,348-349` (3 checks in 1 method), `LongHorizonRollingCheckpointRuntime.cs:368`, `LongHorizonRollingInitialActivationContracts.cs:105`, `LongHorizonPublicPlanService.cs:354` | **SAME_RULE_DUPLICATED** |
| Fallback default | 1 | `CatalogPlanConfirmationService.cs:577` (`?? RunningBackground.Intermediate`) | **DATA_LOOKUP** fallback |
| Hardcoded literal construction (module scope = Intermediate/4D only) | ~12 | `LongHorizonFullNumericOrchestrator.cs:268`, `LongHorizonRollingCoreGenerationInputAdapter.cs:42,71`, `LongHorizonPublicPlanService.cs:161,319`, `LongHorizonFutureCoreRefreshOrchestrator.cs:96`, `LongHorizonRollingRestartContinuationService.cs:64`, `LongHorizonFullDarkLifecycleHarness.cs:51,480` | **ROLLOUT_ACTIVATION** |

**No `ACCIDENTAL_HARDCODE` found.** Every literal is either the single owning constant, a defensive re-check, one of the four duplicate gates, or a construction-site literal inside a module whose entire declared scope is Intermediate/4D. **Materially important, confirmed**: none of the four `SAME_RULE_DUPLICATED` gates (LongHorizon/PreparationRunway subsystems) were updated when the identity policy was widened to accept 3D — they remain hard-locked to `DaysPerWeek == 4` exactly, now structurally inconsistent in shape with `V1CatalogPilotIdentityPolicy`, which they don't reference at all.

## 6. Material Level-driven behavioral effects (full list)

Confirmed exhaustively (§3): **exactly one** — the peak-volume-band clamp lookup (`CatalogPeakVolumeBandLoader`). No workout eligibility, phase structure, calendar, or Adaptation V1 behavior varies by Level anywhere in the current codebase. Everything else that superficially reads as Level-related (`VolumeSafetyPolicy.ThreeDayIntermediate`, the `LevelConservativeDefault` anchor-source enum label, decision-reason strings like `"missing_recent_volume_intermediate_conservative_default"`) is confirmed to be either Frequency-keyed dispatch or a vestigial naming artifact from when only one Level existed, not live Level branching.

## 7-9. Peak-volume, starting-volume, and progression authority analysis (LevelModifier dimensions)

### 7. C1 — StartingWeeklyVolume

**Current authority**: `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume`. Observed user evidence (`RecentWeeklyVolumeKm`, when reported and `>0`) is used directly, unconditionally — confirmed **Level-independent** by direct read; this path never consults `_policy` or any Level field. The missing/zero **fallback default** is genuinely policy-selected, but the selection key is **Frequency** (`DaysPerWeek==3` vs. everything else), not Level, despite the misleading `WeeklyVolumeAnchorSource.LevelConservativeDefault` enum-member name — both existing fallback policies (`V1MissingReadinessStartingVolumePolicy` for 4D, `V1ThreeDayMissingReadinessStartingVolumePolicy` for 3D) resolve to identical numeric defaults today (16km/12km), confirming there is **no existing Level-specific fallback default** to point to.

A policy-resolution interface (constructor injection of an arbitrary `VolumeSafetyPolicy`) already exists, but **no production code selects a policy by Level** — the only selector is the Frequency self-check inside the planner itself. **Classification: EXISTING_GENERIC_MECHANISM** for the plumbing; **PARAMETERIZATION_REQUIRED** for the selection logic (needs to become a real `(Level, DaysPerWeek)` dispatch). **Target authority: DECISION_REQUIRED** — whether fallback defaults should even vary by Level is a product question not resolved by architecture.

### C2 — PeakWeeklyVolume

Confirmed the live schema is exactly `Distance × Level × RunsPerWeek → [min,max]` flat cross-axis policy data (`peak-volume-bands.v3.json` entries: `{distanceFamily, experience, runsPerWeek, minimumKm, maximumKm}`), and this shape already existed with populated Beginner/Advanced/Experienced rows in an earlier release (`1.0.0/policies/PEAK_VOLUME_BANDS_V1.v1.json`, 12 rows). `CatalogPeakVolumeBandLoader.LoadAsync` requires **zero code changes** to serve new rows — it is a pure linear scan. Adding Beginner/Advanced rows is therefore not "inventing new numbers from nothing" architecturally; the schema and a documented historical precedent for exactly this data shape already exist. **Classification: DATA_ONLY. Target authority: CROSS_AXIS_POLICY** (already correctly modeled as such).

### 9. C4 — ProgressionRate

`VolumeSafetyPolicy` is already a typed, constructor-injectable, versioned policy record — adding a new named instance (mirroring how `ThreeDayIntermediate` was added) requires no architecture change to the *type*. However, dispatch has **no Level dimension at all** today — selection is `ReferenceEquals`-based branching baked into 5 separate call sites inside `CatalogVolumeAndLongRunPlanner`, keyed only on Frequency. `VolumeProgressionVerifier` is confirmed to remain **fully generic** — it accepts any policy instance and needs zero changes regardless of how many Level-specific policies are eventually added. **Classification: EXISTING_GENERIC_MECHANISM** for policy definition/verification; **PARAMETERIZATION_REQUIRED** for planner-side selection (restructuring inside the existing class, not new architecture). **Target authority: LEVEL combined with FREQUENCY** — genuinely a 2-axis policy today mis-modeled as if Frequency alone determined it.

## 11. LevelModifier dimension classifications (all six, consolidated)

| Dimension | Classification | Target authority | Key finding |
|---|---|---|---|
| StartingWeeklyVolume | PARAMETERIZATION_REQUIRED (selection) / EXISTING_GENERIC_MECHANISM (plumbing) | DECISION_REQUIRED | See §7 |
| PeakWeeklyVolume | DATA_ONLY | CROSS_AXIS_POLICY | See §8 |
| QualitySessionCount | see §12 | see §12 | See §12 |
| ProgressionRate | PARAMETERIZATION_REQUIRED (selection) / EXISTING_GENERIC_MECHANISM (definition+verification) | LEVEL + FREQUENCY (cross-axis) | See §9 |
| IntensityDistribution | see §13 | see §13 | See §13 |
| PlanHorizonDefault | see §14 | see §14 | See §14 |

## 12. QualitySessionCount ownership analysis

`CatalogRunLayoutSlots`/`CatalogRunLayoutResolver` is confirmed the sole structural-cardinality authority: `roles.Count == candidate.DaysPerWeek` is an explicit, enforced invariant, and role composition comes entirely from the catalog layout document — nothing in this resolver reads `RunningBackground`/experience. A Level-owned "QualitySessionCount" that produced a structural count would therefore directly duplicate/conflict with the already-enforced RunLayout invariant.

However, Level **does** already have a real, wired mechanism for constraining *which* quality workouts are usable, independent of count: `LEVEL_MODIFIER.eligibleWorkouts[]` (confirmed populated for Intermediate: `[EASY_STANDARD, LONG_RUN_STANDARD, FARTLEK, THRESHOLD_TEMPO, GOAL_PACE_TEN_K]`), consumed into `PlanCatalogCandidateSummary.ReferencedWorkouts`. This constrains eligible KEY_SESSION workout content without ever changing *how many* KEY_SESSION slots exist (fixed by the layout, independent of level-modifier content).

**Classification: `LEVEL_ELIGIBILITY_OR_CAP_ONLY`.** As a structural-count dimension, it would be `REMOVE_FROM_LEVEL_AUTHORITY`-worthy if ever proposed; as a workout-*eligibility* concept it already exists and needs only new Level-modifier artifacts, not new architecture. **Binding constraint for any future decision: a Level must never produce a structural count that contradicts RunLayout** — any future "which/how many quality sessions are offered" Level decision must be expressed as eligibility filtering over the RunLayout-fixed slot sequence, never a competing count.

## 13. IntensityDistribution semantic analysis

No first-class `WeeklyIntensityDistribution` type or policy exists anywhere in `RuntimeCatalog`. Intensity is confirmed fully **emergent**: derived from `CatalogSessionPrescriptionPlanner.PaceFor` (per-workout-key pace resolution, `EffortOnly` for most, numeric only for `GOAL_PACE_TEN_K`) plus workout-component percentage splits — none of which reads Level anywhere.

**Classification: `EMERGENT_DERIVED_BEHAVIOR`.** Per the explicit instruction not to invent a new abstraction merely because the framework document listed this dimension: Beginner/Advanced intensity differentiation almost certainly needs to be expressed as (i) workout-eligibility changes via a new Level-modifier's `eligibleWorkouts` list (already-existing mechanism, §12) and/or (ii) dosage/segment-percentage changes inside workout-definition components — not a standalone distribution-percentage object, since no runtime pipeline exists to consume one.

## 14. PlanHorizonDefault semantic analysis

`RaceHorizonPolicy`/`CoreHorizonClassifier` confirmed purely date-arithmetic — no `RunningBackground` parameter exists anywhere in the type, and its bounds are explicitly sourced from the distance-family template's core-cycle bounds, not Level. Its own design goal is stated directly in code: horizon must "never [be] a second, independently-computed value."

**Classification: `DOESNT_MAP_CLEANLY` to Level ownership as currently architected.** Horizon is fundamentally a function of race date/start date and the distance-family template, not Level. If Beginner/Advanced runners were ever meant to receive systematically different *default* horizons, that is a **new product decision layered on top of** (e.g., a Level-aware UX suggestion surfaced pre-request) rather than a change to the runtime horizon-classification authority itself, which must remain a single race-date-driven computation per its own stated design goal. **Target authority: UX_DEFAULT_ONLY or DECISION_REQUIRED** — Level ownership of the runtime classification authority is explicitly not forced here.

## 15. Identity/routing duplication analysis

See §5's table for the full classification. Synthesis: the exact same duplication *pattern* documented for the Frequency gate in an earlier audit recurs for Level — one primary owning policy, one legitimately-layered defensive re-check, and **four independent `SAME_RULE_DUPLICATED` gates** in the LongHorizon/PreparationRunway subsystem, none referencing `V1CatalogPilotIdentityPolicy` at all. **No `CONFLICTING_AUTHORITY` found** — all gates currently agree on the rule (Level must == Intermediate); they simply don't share a single source of truth, and (per §3's incidental finding) are already out of sync with the identity policy on the Frequency dimension, meaning any future Level widening would need to be independently propagated to these same four sites or they would continue rejecting valid non-Intermediate LongHorizon/Runway requests even after CORE_PATH supported them.

## 16. V1CatalogPilotIdentityPolicy Level-extensibility (D2)

`IsSupportedIdentity` already takes `level` as an explicit parameter compared via `==` against a single named constant — structurally this already reads as one arm of a would-be `(Level, DaysPerWeek)` check, not level baked into unreachable control flow. `ResolveCandidate`, by contrast, is effectively `ResolveIntermediateCandidate(daysPerWeek)` — it takes no Level parameter at all; Level is implicit, enforced only by the upstream `IsSupportedIdentity` precondition, never re-verified inside `ResolveCandidate` itself.

**Minimum generalization required** (directly analogous to the DaysPerWeek edit already performed once): (1) replace the single `Level` const with an explicit supported-set, (2) change `IsSupportedIdentity`'s `level == Level` to a membership test against that set (the same edit class as `daysPerWeek is 3 or DaysPerWeek`), (3) give `ResolveCandidate` a `level` parameter and switch on the `(level, daysPerWeek)` tuple, mirroring the existing switch shape exactly, preserving the `_ => throw ArgumentOutOfRangeException(...)` fail-closed catch-all.

**No-nearest-match-fallback verified preserved today**: `level == Level` is pure value equality with no interval/default logic; a Beginner request evaluates `false`, `IsSupportedIdentity` returns `false`, and the caller routes to `Legacy`/`NotPilotRequest` — it can never reach `ResolveCandidate`, which itself has no Level-awareness to silently substitute.

**Classification: `SMALL_IDENTITY_POLICY_GENERALIZATION_REQUIRED`.** Not a parallel resolver, not an architecture change — the same shape of edit already performed once (for DaysPerWeek), applied to a second dimension.

## 17. Exact-match / no-nearest-match analysis (D3)

**CURRENT_IDENTITY_DIMENSIONS**: `GoalType`(const, single value), `GoalDistance`(const, single value), `RunningBackground Level`(const, single value), `DaysPerWeek`(2-arm set). "HorizonFamily" is confirmed **not** an identity-check dimension inside this policy — horizon is a separate, later-evaluated concern owned entirely by `RaceHorizonPolicy`, consumed only after `IsSupportedIdentity` has already passed. The real current identity shape is `Distance(const) × Level(const) × Frequency(2-arm)`.

**MISSING_LEVEL_DIMENSION**: Level is present as a parameter/comparison but not yet as a true *dimension* — structurally identical to how DaysPerWeek looked before its own generalization (a bare `==` check) rather than how it looks now (an enumerated set).

**TARGET_EXACT_MATCH_RULE**: a 4-dimension `Distance × Level × Frequency × HorizonFamily` shape is **not** the natural target — the existing pattern generalizes cleanly by testing each dimension via membership in an explicit finite set (never range/nearest), with the same fail-closed catch-all preserving exact-match-only behavior. HorizonFamily is deliberately **not** folded into candidate identity today (it's `RaceHorizonPolicy`'s independent, date-derived concern by explicit design — "never a second, independently-computed value"); adding it as a 4th identity dimension would be a genuine new decision, not a mechanical extension of the existing pattern.

## 18. Support-vs-rollout analysis (D4)

**Confirmed: catalog-compatibility and public-routability are already two separate, independently-gated states**, and this exact separation is what 3D used before its own public activation. Evidence: a `TEMPLATE_COMBINATION` artifact can exist and be loadable while its `metadata.status` is `DRAFT`/`VALIDATED` rather than `PUBLISHED`; `LivePlanPreviewRoutingService` checks `candidateLifecycleStatus == "PUBLISHED"` and `activationEnabled` as two independent gates beyond identity match. A `LocalCatalogAcceptanceOptions` development-only override (triple-flag-gated, `IsDevelopment()`-checked) already lets a developer exercise a DRAFT candidate through the real preview→confirm flow without publishing it — direct, working evidence this pattern is currently exercised, not hypothetical.

**No coupling problem found.** Adding Beginner/Advanced catalog data would not, by itself, activate public rollout — it would reach at most `CatalogSupportedButNotPublished`/`CatalogRequestUnsupported` states, never `CatalogLive` without an independent `PUBLISHED` status flip and `activationEnabled=true`, and (until §16's identity-policy generalization is separately made) `IsSupportedIdentity` would still reject non-Intermediate levels regardless of catalog data or publication status.

## 19. Taper/eligibility pattern findings

The real 3D mechanism (`PHASE_10K_GEN_2B_3_3D_TAPER_MINIMUM_CONFLICT_DECISION.md` §5-10, verified against runtime code): `CatalogVolumeAndLongRunPlanner.Build` computes the taper week's volume as `previousWeekVolume × TaperVolumeMultiplier (0.53)`, then — gated strictly by `DaysPerWeek == 3` — compares it against a hardcoded `12d` floor (`MinimumKeyKm=4 + MinimumEasyKm=3 + MinimumLongKm=5`), throwing `ThreeDayCoreProductIneligibleException` → mapped by name in `CatalogPreviewGenerator.cs` to `PlanProductIneligibleException` → HTTP 422, reason `THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`.

**Beginner×4D**: 4D's own residual-only session floor (`MinimumKeySessionDistanceKm=3 + 2×MinimumEasySupportDistanceKm(1.5)=6`) plus a separately-enforced long-run-share minimum (not captured in one combined constant today) is structurally lower/differently-shaped than 3D's flat 12km, but the underlying conflict mechanism — `(starting volume, progression, 0.53 decay, layout session-floor sum)` — is identical in shape and none of its four inputs are load-bearing on DaysPerWeek specifically being 3. A sufficiently low Beginner starting volume, decayed by the same multiplier, risks landing under whatever the (not-yet-derived) 4D floor works out to. **Classification: `BEGINNER_4D_TAPER_FLOOR_CONFLICT_STRUCTURALLY_POSSIBLE`.**

**Advanced×4D**: the check's risk direction is confirmed one-sided by the formula's own structure (`previous × 0.53` compared to a fixed lower floor) — a higher starting/peak volume only ever increases the taper-week volume, never decreases it below a fixed floor. **Classification: `NOT_STRUCTURALLY_EXPECTED`** for this specific floor-violation direction. (Advanced×4D is not risk-free by symmetry elsewhere — see §21 rows 10-11 for unresolved high-volume-direction questions.)

**Reusability**: the outer public contract (`PlanProductIneligibleException` → HTTP 422 with `Reason` as error code) is directly reusable with zero changes. The inner detection/translation layer is hand-wired to `DaysPerWeek==3` specifically (the `==3` gate, the `12d` literal, the `ThreeDayCore`-named exception type, two hardcoded catch clauses by concrete type name) and would need a new sibling exception type + new reason code + two new catch-clause arms — small, additive work in the same three files, not a new subsystem. **Classification: `EXISTING_ELIGIBILITY_PATTERN_REUSABLE`.**

**Important adjacent finding**: `CatalogSessionPrescriptionInfeasibleException` (the 4D session-allocation floor failure, distinct from the taper check) is **not** currently one of the explicitly-caught types in `CatalogPreviewGenerator.cs` — it falls into the generic catch-all and surfaces as **HTTP 500**, not a clean 422. A genuinely-infeasible low-volume Beginner×4D request would today present as an internal-server error, the exact anti-pattern the 3D taper work was built to avoid. This is flagged as a real, current gap (§21 row 15), not merely a future risk.

## 20. Beginner×4D feasibility table

| Subsystem | Classification | Evidence / missing dimension |
|---|---|---|
| Canonical Core | SUPPORTED | `ten-k-master.v6.json` is level-agnostic by construction, already shared across live 3D/4D Intermediate candidates |
| RunLayout | SUPPORTED | `CatalogRunLayoutResolver` reads `candidate.SlotRoles` generically, no Level reference |
| Combination/manifest model | NEEDS_PARAMETRIZATION | New `TEMPLATE_COMBINATION` artifact `TEN_K__4D__BEGINNER` needed (same shape as existing ones) |
| Workout binding | SUPPORTED (mechanism) | Binder reads from candidate's `eligibleWorkouts`, not hardcoded keys |
| Workout eligibility | NEEDS_PARAMETRIZATION | New `LEVEL_MODIFIER` artifact needed; **plus a naming-reconciliation gap**: catalog vocabulary uses `"NEW"`, backend uses `Beginner`, no mapping exists (§4) |
| Starting volume | NEEDS_ARCHITECTURE_CHANGE | `V1MissingReadinessStartingVolumePolicy`'s constants are hardcoded Intermediate/4D-provenanced, selected only by a 2-way Frequency branch with no Level parameter in the call chain at all |
| Peak volume | NEEDS_PARAMETRIZATION (data) + NEEDS_ARCHITECTURE_CHANGE (selection) | New row is pure data, but selection of which `experience` string to query with derives only from `V1CatalogPilotIdentityPolicy.CatalogLevel`, a single hardcoded constant with no lookup mechanism |
| Progression | NEEDS_ARCHITECTURE_CHANGE | `VolumeSafetyPolicy.Default`'s ratios are Intermediate/4D-calibrated; every construction site passes no Level argument at all; no Level-keyed selection exists |
| Long-run | SUPPORTED (mechanism) / NEEDS_PARAMETRIZATION (numbers) | Share-driven formula is level-blind; numbers inherit the Progression gap |
| Session allocation | SUPPORTED (mechanism); floor risk under NEEDS_PARAMETRIZATION | Level-blind mechanism; a low Beginner volume risks the residual<6km infeasibility check |
| Pace/intensity | UNKNOWN | Not verified in this pass beyond confirming no Level-specific logic was found in the files read; flagged for follow-up, not asserted |
| Calendar | SUPPORTED | Calendar materialization operates on already-resolved plans, no Level branch found |
| Validation | SUPPORTED (mechanism) | `CatalogVolumePlanValidator` validates produced plans generically |
| Taper | NEEDS_ARCHITECTURE_CHANGE | Eligibility check hardcoded to `DaysPerWeek==3`; no 4D floor exists yet, and the 4D floor isn't a single derivable constant today (§19) |
| Eligibility | NEEDS_ARCHITECTURE_CHANGE | No typed 4D-taper exception exists; `CatalogSessionPrescriptionInfeasibleException` currently surfaces as HTTP 500, not 422 (§19) |
| Persistence | SUPPORTED | `TrainingPlan.Level` is already typed `RunningBackground` (all 4 values), no restriction |
| Public routing | NEEDS_ARCHITECTURE_CHANGE | `V1CatalogPilotIdentityPolicy.Level`/`ResolveCandidate` are single-value constants with no Level dimension (§16) |
| Adaptation | UNKNOWN | Not inspected in this pass beyond §3's confirmed zero-re-branching finding at the Adaptation-code level; Beginner-specific consequence not separately assessed |

**Overall: `BEGINNER_4D_NEEDS_ARCHITECTURE_CHANGE`.** Several rows are pure data gaps, but the identity-routing constant (§16), the volume-safety-policy dispatch gap (§7/§9), and the taper/eligibility-exception-mapping gap (§19) are structural — not satisfiable by adding JSON rows alone.

## 21. Advanced×4D feasibility table

Same subsystem list and mechanism as §20 for rows 1-6, 9(mechanism), 12-14, 16, 18 (all identical findings — not re-tabulated for brevity, see §20). Advanced-specific deltas:

| # | Subsystem | Classification | Evidence |
|---|---|---|---|
| 5 | Workout eligibility | NEEDS_PARAMETRIZATION | No naming mismatch for Advanced (catalog `"ADVANCED"` matches enum `Advanced` directly) — only a missing artifact, cleaner than Beginner's case |
| 7 | Peak volume | NEEDS_PARAMETRIZATION, **with an open-question qualifier** | No hard ceiling exists anywhere independent of the level-specific row itself — the JSON schema declares no `maximum` bound on `minimumKm`/`maximumKm`. **Repository evidence does NOT establish "Advanced = Intermediate + more kilometers" as a validated structural relationship** — no such formula exists in code; Advanced's peak/starting-volume dosage model is confirmed an **open product question**, not merely an unfilled numeric slot with an implied scaling rule |
| 8 | Progression | NEEDS_ARCHITECTURE_CHANGE, **with partial documented evidence** | `progression_rules_v2.yaml` (docs-only, not runtime-loaded by any C# code found) documents Advanced-specific ratios (`preferred:[0.05,0.08], hardCap:0.10` vs. Intermediate's `[0.04,0.07]/0.08`) — real evidentiary basis exists in docs, just not wired to runtime |
| 10 | Long-run absolute cap | **CANNOT_ASSESS_WITHOUT_FURTHER_SEARCH** | No hardcoded absolute-km long-run maximum was found in the files read (only a percentage-of-weekly-volume hard cap, actively enforced); `LongRunProgressionVerifier.cs` was not opened in this pass and could contain a relevant check — flagged honestly as unverified, not asserted safe |
| 11 | KEY-session/workout-component distance ceiling | **UNKNOWN** | No maximum constant found on KEY/EASY distances in the files read (only minimums); actual `WORKOUT_DEFINITION` artifacts were not opened to check for component-level ceilings — genuinely open, not assumed resolved |
| 15 | Taper | NOT_STRUCTURALLY_EXPECTED for the floor-violation direction specifically (§19), but the underlying `DaysPerWeek==3`-only gate is still NEEDS_ARCHITECTURE_CHANGE in general (Advanced simply wouldn't be the Level that trips this particular check) |
| 17 | Recent-volume handling | SUPPORTED | The "user reported real recent volume" path is numerically generic and not Level-gated; only the missing/zero default (row 6-equivalent) is Intermediate-hardcoded |
| 19 | Public routing | NEEDS_ARCHITECTURE_CHANGE | Same constant/dispatch gap as Beginner — `V1CatalogPilotIdentityPolicy` hardcodes exactly one Level, blocking Advanced identically |
| 20 | Adaptation | UNKNOWN | Same caveat as Beginner |

**Overall: `ADVANCED_4D_NEEDS_ARCHITECTURE_CHANGE`.** The same blocking structural gaps as Beginner apply identically (identity policy, VolumeSafetyPolicy dispatch, taper/eligibility exception mapping). Additionally and importantly: Advanced carries **more open evidentiary questions than Beginner** — no validated "Advanced=Intermediate+X" relationship for peak/starting volume, and two genuinely unverified capacity-ceiling questions (long-run absolute cap, KEY-session/component distance ceiling) that would need dedicated follow-up research before even a decision inventory could be completed with full confidence. These should not be silently assumed resolved by analogy to Intermediate.

## 22. Level×Frequency authority matrix

| Concern | Current owner | Target owner | Cross-axis inputs | Current implementation | Conflict? | Decision required? |
|---|---|---|---|---|---|---|
| Canonical phases | Distance | Distance | none | `TEN_K_MASTER v6`, no Level/Frequency parameter | No | No |
| RunsPerWeek | Frequency | Frequency | none | `RUN_LAYOUT_{3,4}D`, `candidate.DaysPerWeek` | No | No |
| Role cardinality | Frequency | Frequency | none | `CatalogRunLayoutResolver`, enforced invariant `roles.Count==DaysPerWeek` | No | No |
| KEY count | Frequency | Frequency | none | Same as role cardinality; Level's `eligibleWorkouts` only filters content, never count (§12) | No | No |
| Starting volume | Frequency (dispatch) | LEVEL + FREQUENCY | readiness evidence | `ResolveStartingVolume`, Frequency-only fallback dispatch today | **Yes — mis-modeled** | Yes (§7) |
| Peak volume | none live beyond Intermediate | CROSS_AXIS_POLICY (Distance×Level×RunsPerWeek) | Distance, Level, RunsPerWeek | `CatalogPeakVolumeBandLoader`, already correctly shaped, data-only gap | No (shape correct) | No (values only) |
| Progression | Frequency (dispatch) | LEVEL + FREQUENCY | none additional | `VolumeSafetyPolicy` dispatch, Frequency-only today | **Yes — mis-modeled** | Yes (§9) |
| Session allocation | Frequency | Frequency | volume magnitude (risk only) | `V1{Three,Four}DaySessionVolumeAllocationPolicy`, level-blind mechanism | No | No |
| Long-run policy | Frequency (share formula) | LEVEL + FREQUENCY (once progression is fixed) | Progression policy | `BuildLongRunPlan`, inherits Progression's gap | Indirectly, via Progression | Inherits §9 |
| Workout eligibility | Level (identity-gated) | LEVEL | none | `LEVEL_MODIFIER.eligibleWorkouts` — real, wired, data-only gap | No | No (data only) |
| Workout identity | Distance + Level | DISTANCE + LEVEL | none | Same mechanism as eligibility | No | No |
| Workout dosage | emergent (workout components) | LEVEL (via eligibility/dosage, not a standalone object) | none | No first-class object exists; emergent (§13) | No | Partial (§13 — semantic, not architectural) |
| Pace/intensity | Distance (workout-key-driven) | DISTANCE (+ possibly LEVEL via eligibility) | none confirmed | `CatalogSessionPrescriptionPlanner.PaceFor`, level-blind | No | No |
| Calendar | Frequency | Frequency | none | Level-blind, confirmed | No | No |
| Taper | Frequency (multiplier) + Frequency-gated eligibility check | FREQUENCY (multiplier) + LEVEL×FREQUENCY (eligibility floor) | Starting volume, Progression | `DaysPerWeek==3`-only gate today (§19) | **Yes — 4D has no equivalent check** | Yes (§19) |
| Eligibility (exception routing) | Frequency-specific type/catch-clause pattern | should be LEVEL×FREQUENCY-generic pattern | none | Hardwired to `ThreeDayCore`-named type (§19) | **Yes — incomplete for 4D** | Yes (§19) |
| Adaptation | Frequency (structural-week evidence only) | FREQUENCY (confirmed) | none | Zero Level re-branching found (§3/§6) | No | No |
| Candidate identity | Distance(const)×Level(const)×Frequency(2-arm) | should add explicit Level dimension | none | `V1CatalogPilotIdentityPolicy` (§16/§17) | **Yes — Level not yet a true dimension** | Yes (§16) |
| Rollout | separate DRAFT/VALIDATED/PUBLISHED + activation-flag gate | unchanged, already correct | none | Confirmed already separated from support (§18) | No | No |

**This matrix directly supports GEN.0 B3's reassessment (§25)**: the architecture is **substantially compositional already** — Distance, Frequency, and Level each own clean, non-overlapping concerns for the majority of rows (phases, role cardinality, calendar, adaptation, workout eligibility/identity, rollout). The genuine conflicts cluster narrowly around **numeric prescription dispatch** (starting volume, progression — currently Frequency-only where it should be Level×Frequency) and **the taper/eligibility exception-routing pattern** (currently Frequency-specific where it needs to become general), plus the **identity-policy constant itself** not yet treating Level as a true dimension. This is a small, well-bounded set of gaps, not a systemic compositional failure.

## 23. Cross-axis policy findings

| Policy | Classification | Rationale |
|---|---|---|
| PeakVolumeBand (Distance × Level × RunsPerWeek → [min,max]) | **LEGITIMATE_CROSS_AXIS_POLICY** | Flat, composite-keyed lookup data; does not duplicate a complete plan, only a numeric range |
| Volume-safety progression ratios (StartingVolume, PreferredMaxWeeklyIncreaseRatio, HardMaxWeeklyIncreaseRatio, TaperVolumeMultiplier) | **SHOULD_BE_LEVEL×FREQUENCY_CROSS_AXIS** (currently mis-modeled as Frequency-only) | Confirmed genuinely 2-axis by evidence (§9); current single-record-per-Frequency shape needs to become a genuine cross-axis lookup, not a redesign of what it computes |
| Session-allocation minimum floors (`MinimumKeySessionDistanceKm` etc.) | **SHOULD_BE_FREQUENCY_ONLY** (confirmed correctly scoped today) | Level-blind by design and evidence; no reason found to introduce a Level dimension here |
| Taper-floor eligibility threshold (currently the `12d` 3D-specific literal) | **SHOULD_BE_FREQUENCY_ONLY per-layout, but currently incomplete** — needs a 4D-equivalent derived constant, not a Level dimension | The floor is a function of layout session minimums (Frequency), not Level; Level only affects whether a given numeric trajectory *lands* below that floor |
| Workout eligibility (`LEVEL_MODIFIER.eligibleWorkouts`) | **LEGITIMATE_CROSS_AXIS_POLICY** bounded by Distance+Level | Already correctly modeled; a list of workout references, not a duplicated plan |
| Hypothetical "complete week-by-week plan keyed by Distance×Level×RunsPerWeek" | **DUPLICATED_PLAN_AUTHORITY_RISK** (not found in current code, flagged as the explicit boundary example the phase asked to distinguish) | Would violate `SINGLE_CANONICAL_10K_CORE_APPROVED`/`COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST` — confirmed no such artifact exists; combinations remain reference manifests, not embedded plans |

## 24. Decision inventory

| ID | Question | Classification | Dependency order note |
|---|---|---|---|
| D1 | Beginner missing/zero starting volume | `PRODUCT_DECISION_REQUIRED` | Needs D16 (identity/routing authority) resolved first structurally, but the numeric value itself is independent evidence work |
| D2 | Advanced missing/zero starting volume | `PRODUCT_DECISION_REQUIRED` | Same as D1 |
| D3 | Beginner peak-volume band | `EXTERNAL_EVIDENCE_REQUIRED` | Schema/mechanism ready (§8); only the row's numbers are missing |
| D4 | Advanced peak-volume band | `EXTERNAL_EVIDENCE_REQUIRED`, with an added `REPOSITORY_EVIDENCE_REQUIRED` component (long-run/KEY ceilings, §21 rows 10-11) before the number can be safely chosen | Should follow resolution of D4a (the ceiling questions), not precede it |
| D5 | Beginner progression policy | `PRODUCT_DECISION_REQUIRED` | Docs (`progression_rules_v2.yaml`) provide a starting evidence basis for Advanced but **not** confirmed to exist for Beginner/"NEW" in the same file — verify before assuming parity |
| D6 | Advanced progression policy | `PRODUCT_DECISION_REQUIRED`, partially `ALREADY_RESOLVED` at the evidence-documentation level (docs exist) but not runtime-wired | Docs → runtime-wiring is an architecture task (§9), independent of the number's correctness |
| D7 | Beginner session-allocation policy | `NOT_APPLICABLE` | Session-allocation mechanism is confirmed Level-blind and Frequency-only (§20/§22) — no Level-specific policy is needed here at all |
| D8 | Advanced session-allocation policy | `NOT_APPLICABLE` | Same reasoning as D7 |
| D9 | Beginner long-run policy | `PRODUCT_DECISION_REQUIRED` | Inherits D5's dependency (long-run share formula reuses progression policy inputs) |
| D10 | Advanced long-run policy | `PRODUCT_DECISION_REQUIRED` | Inherits D6; additionally blocked on D4's ceiling-verification sub-item |
| D11 | Beginner workout eligibility/dosage | `PRODUCT_DECISION_REQUIRED` | Mechanism exists (§12/§13); content is the open question |
| D12 | Advanced workout eligibility/dosage | `PRODUCT_DECISION_REQUIRED` | Same mechanism; additionally needs D4a's capacity-ceiling verification if higher-dosage workouts are intended |
| D13 | QualitySessionCount ownership | `ALREADY_RESOLVED` | §12 — `LEVEL_ELIGIBILITY_OR_CAP_ONLY`, resolved by this audit with repository evidence, not deferred |
| D14 | IntensityDistribution semantics | `ALREADY_RESOLVED` | §13 — `EMERGENT_DERIVED_BEHAVIOR`, resolved by this audit |
| D15 | PlanHorizonDefault semantics | `ALREADY_RESOLVED` (architecturally) / `PRODUCT_DECISION_REQUIRED` (only if a UX-default feature is ever desired) | §14 — the architecture question is resolved; a UX feature decision remains optional and separate |
| D16 | Level identity/rollout authority | `ARCHITECTURE_DECISION_REQUIRED` | §16/§17 — the minimum generalization shape is already identified by this audit; the remaining work is an implementation-authorization decision, not further research |
| D17 | Taper/readiness eligibility interaction | `ARCHITECTURE_DECISION_REQUIRED` (4D floor derivation + exception-routing generalization) + `REPOSITORY_EVIDENCE_REQUIRED` (deriving the exact 4D floor constant, §19) | Should resolve before D1/D9 (Beginner starting-volume/long-run choices), since the taper floor constrains the feasible range of those numbers |
| D18 | Same one-KEY progression identity vs. Level-specific stage/workout eligibility for Beginner/Advanced | `PRODUCT_DECISION_REQUIRED` | Directly follows D11/D12 |

**Dependency order (synthesis, not an implementation plan)**: `D16 (identity authority)` → `D17 (taper/eligibility architecture generalization)` → `{D3/D4 evidence gathering, D4a capacity-ceiling verification}` → `{D1/D2/D5/D6/D9/D10/D11/D12/D18 product-policy synthesis}` → implementation. D13/D14/D15 are already resolved and do not block this chain. D7/D8 are confirmed not applicable and drop out of the chain entirely.

## 25. GEN.0 B3 classification

**`GEN0_B3_CONFIRMED`.**

Support, distinguishing the four required categories:
- **Level identity/routing work**: real but small and mechanical — `V1CatalogPilotIdentityPolicy` needs the same shape of edit already performed once for Frequency (§16), plus propagation to 4 already-duplicated LongHorizon/PreparationRunway gates (§15).
- **Level policy/data work**: the dominant category by volume — new catalog artifacts (level-modifier, combination, peak-volume-band rows), all confirmed `DATA_ONLY`/`DATA_EXTENSIBLE` with existing, already-generic loading mechanisms (§8, §16 in the underlying research).
- **True architectural blockers**: exactly two, both narrow and well-characterized: (1) `VolumeSafetyPolicy`'s dispatch mechanism has zero Level dimension (§7/§9) — a restructuring inside one existing class, not new types/interfaces; (2) the taper/eligibility exception-routing pattern is hand-wired to the `DaysPerWeek==3` case specifically (§19) — additive new-sibling-type work in three known files, following an established pattern.
- **Unresolved semantic dimensions**: QualitySessionCount, IntensityDistribution, and PlanHorizonDefault are **fully resolved by this audit itself** (§12-14), not left open — none required a genuinely new architectural mechanism; each mapped cleanly onto an existing or emergent concept.

This directly confirms GEN.0's original proposition: Level generalization is substantially more parametrization/data-heavy than architecture-heavy. The architectural surface (§16, §9, §19) is narrow, additive, and follows patterns the codebase has already exercised once (for Frequency/3D) — it is not a comparable undertaking to, say, the Frequency axis's second-KEY-session problem found in the earlier GEN.1 architecture audit (which required genuine new representational capability across multiple independently-encoded subsystems). No repository evidence was found that would justify rejecting or only partially confirming the original proposition.

## 26. Cheapest safe Level unlock

**`BEGINNER_4D_CHEAPER`.**

Both Beginner×4D and Advanced×4D share the identical architectural floor (identity policy, VolumeSafetyPolicy dispatch, eligibility-exception generalization — §20/§21, all rows outside the Advanced-specific deltas are identical). The determining factors, per the required evaluation criteria:

- **Unresolved domain decisions**: Beginner has one dominant open question (starting/peak/progression numbers in the low-volume direction, with a known, already-solved-once conflict pattern — the 3D taper work — directly reusable as a template). Advanced carries the same numeric-decision burden **plus two genuinely unresolved repository-evidence questions** (long-run absolute cap, KEY-session/workout-component distance ceiling, §21 rows 10-11) that must be answered before its numeric decisions can even be safely scoped — these are not present for Beginner, where the risk direction is well-understood and bounded (low volume → known floor-violation class).
- **External evidence required**: both need Level-specific numeric policy; Advanced's is compounded by needing capacity-ceiling verification first.
- **Workout-catalog compatibility**: Advanced has a cleaner catalog naming match (`"ADVANCED"` = `Advanced`, no mismatch) vs. Beginner's `"NEW"`/`Beginner` naming gap — a point in Advanced's favor, but a small, purely mechanical one (one mapping constant), not a structural blocker.
- **Taper/readiness risk**: Beginner's risk (`BEGINNER_4D_TAPER_FLOOR_CONFLICT_STRUCTURALLY_POSSIBLE`) is the **same class of problem already solved once** for 3D, with a directly reusable, well-understood pattern (§19). Advanced's risk profile is `NOT_STRUCTURALLY_EXPECTED` for the taper-floor case specifically, but carries unquantified risk elsewhere (rows 10-11) with no existing template to reuse.
- **Current data availability**: both have zero populated catalog rows currently; historically-existing-but-removed data (§3, §8) was Intermediate-agnostic in principle for both.
- **Current identity/routing support**: identical for both (§16/§17).
- **Regression risk**: identical for both (neither touches any currently-live code path differently).

Net: Beginner's remaining work is dominated by decisions with a **known shape and a reusable precedent**; Advanced's remaining work includes **open-ended repository investigation** (two genuinely unverified capacity questions) before its decision inventory can even be considered complete.

## 27. Recommended next phase

```
PHASE 10K-GEN.4A — LEVEL AUTHORITY DECISION RESOLUTION
```

**Rationale**: per §24's dependency order, the earliest unresolved dependency blocking *any* further Level work (Beginner or Advanced) is `D16` (Level identity/rollout authority — an architecture-authorization decision, not further research) followed immediately by `D17` (taper/eligibility architecture generalization — also an authorization decision with the exact minimum shape already identified in §16/§19). Both are architecture decisions this audit has already fully specified but explicitly must not resolve itself. Beginning numeric evidence-synthesis work (a plausible alternative next phase, e.g. training-load evidence gathering) before these two authority decisions are made would risk producing Level-specific product numbers with no architecturally-sanctioned place to plug them in yet — the same ordering discipline the 3D generalization line already followed (GEN.2A architecture-authority decision → GEN.2B evidence/product-decision phases → GEN.3A/3B implementation/activation). This is the earliest unresolved dependency, not merely the easiest coding task (the easiest task, by file-count, might be adding a single new catalog data row — but that data has nowhere sanctioned to route to without D16/D17 first).

## 28. Files inspected

`RunningApp.Domain/Enums/RunningBackground.cs`, `RunningBackgroundCanonicalJsonConverter.cs`, `RunningBackgroundJsonConverter.cs`; `RunningApp.Persistence/Converters/RunningBackgroundCompatibilityConverter.cs`; `RunningApp.Application/DTOs/Plan/{GeneratePreviewRequest,GenerateRacePlanPreviewRequest,GenerateHabitPlanPreviewRequest,PlanDetailsResponse,GeneratePreviewResponse}.cs`; `RunningApp.Domain/Entities/{TrainingPlan,PlanTemplate}.cs`; `RuntimeCatalog/PreviewRouting/{V1CatalogPilotIdentityPolicy,LivePlanPreviewRouting,PlanCatalogDomainMapper,CatalogPlanConfirmationService,CatalogPreviewGenerator}.cs`; `RuntimeCatalog/PlanCatalogCandidateSummary.cs`, `PlanCatalogBundleLoader.cs`; `RuntimeCatalog/Prescription/Volume/{VolumeSafetyPolicy,CatalogVolumeAndLongRunPlanner,CatalogPeakVolumeBandLoader,V1MissingReadinessStartingVolumePolicy,V1ThreeDayMissingReadinessStartingVolumePolicy,CatalogVolumeExceptions}.cs`; `RuntimeCatalog/Prescription/Session/{V1FourDaySessionVolumeAllocationPolicy,V1ThreeDaySessionVolumeAllocationPolicy,FourDaySessionDistanceAllocationPolicy,CatalogSessionPrescriptionPlanner}.cs`; `RuntimeCatalog/Schedule/Materialization/{CatalogRunLayoutSlots,VolumeProgressionVerifier}.cs`; `RuntimeCatalog/Schedule/PreparationRunwayOrchestration/TenKPreparationRunwayDarkOrchestrator.cs`; `RuntimeCatalog/Schedule/LongHorizon/{RollingActivation/LongHorizonRollingCheckpointRuntime,RollingActivation/LongHorizonRollingInitialActivationContracts,RollingActivation/PublicPreview/LongHorizonPublicPlanService,RollingActivation/LifecycleValidation/LongHorizonFullDarkLifecycleHarness,RollingActivation/Persistence/LongHorizonFutureCoreRefreshOrchestrator,RollingActivation/Persistence/LongHorizonRollingRestartContinuationService,RollingActivation/LongHorizonRollingCoreGenerationInputAdapter,LongHorizonFullNumericOrchestrator}.cs`; `RuntimeCatalog/Schedule/LongHorizon/Adaptation/**` (exhaustive grep, all files, zero `RunningBackground` matches); `Common/RaceHorizonPolicy.cs`; `Exceptions/AppExceptions.cs`; `RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`. Catalog artifacts: `plan-catalog/catalog/policies/peak-volume-bands.v3.json`; `plan-catalog/artifacts/appsel-plan-catalog/1.0.0/policies/PEAK_VOLUME_BANDS_V1.v1.json`; `plan-catalog/catalog/level-modifiers/intermediate-modifier.v{1-6}.json`; `plan-catalog/catalog/combinations/{ten-k-4d-intermediate.v10,ten-k-3d-intermediate.v1}.json`; `plan-catalog/schemas/peak-volume-band-policy.schema.json`; `plan-catalog/docs/canonical/golden-fixture-v3/progression_rules_v2.yaml`. Prior-phase documents read for baseline verification: `PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md`, `PHASE_10K_GEN_1_ARCHITECTURE_AUDIT.md`, `PHASE_10K_GEN_3B_INTERMEDIATE_3D_CORE_PUBLIC_ACTIVATION.md`, `PHASE_10K_GEN_2B_3_3D_TAPER_MINIMUM_CONFLICT_DECISION.md` (§5-10 for the taper mechanism's product-decision provenance).

## 29. Final classification

```
LEVEL_AUTHORITY_AUDIT_READY_FOR_DECISION_PHASE
```

All completion-standard criteria met: GEN.3A.3 prerequisite acknowledged satisfied without reopening it; current code re-audited fresh throughout (not inherited from GEN.0); every material Level-driven branch/lookup inventoried (§5-6); canonical Level vocabulary made explicit including the confirmed `NEW`/`Beginner` naming gap (§4); all six LevelModifier dimensions individually classified (§11-14); QualitySessionCount ownership explicitly resolved, not deferred (§12); IntensityDistribution and PlanHorizonDefault semantics explicitly evaluated (§13-14); `V1CatalogPilotIdentityPolicy`'s Level extensibility classified (§16); no-nearest-match preservation explicitly verified against current code (§17); Beginner×4D and Advanced×4D feasibility both classified with full subsystem tables (§20-21); Frequency×Level authority independence evaluated via the full matrix (§22); legitimate cross-axis policy separated from duplicated-plan-authority risk (§23); remaining decisions ordered (§24); GEN.0 B3 explicitly confirmed with evidence (§25); exactly one next phase recommended (§27). No architectural contradiction invalidating the compositional model was found — `LEVEL_AUTHORITY_ARCHITECTURE_CONTRADICTION_FOUND` does not apply.
