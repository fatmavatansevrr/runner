# Phase 10K-GEN.4A — Level Authority Decision Resolution

**Decision/governance phase only. No production code written. No catalog modified. No numeric training-load values chosen. No public rollout changed.**

## 1. GEN.4 input acknowledgement

`PHASE_10K_GEN_4_LEVEL_AUTHORITY_AUDIT.md` (final classification `LEVEL_AUTHORITY_AUDIT_READY_FOR_DECISION_PHASE`) is accepted as the factual starting point. No contradiction was discovered while validating the decisions below — every decision here traces directly to a specific GEN.4 finding cited by section number.

## 2. Frozen architecture acknowledgement

Not reopened: `SINGLE_CANONICAL_10K_CORE_APPROVED`, `CORE_SKELETON_AUTHORITY_SINGLE_DYNAMIC`, `RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY`, `COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST`, `SUPPORT_IS_SEPARATE_FROM_ROLLOUT`, `NO_NEAREST_MATCH_FALLBACK`, `BEHAVIORAL_EQUIVALENCE_REQUIRED`. Every decision below was checked against these before being finalized; none required reopening any of them.

## 3. Level vocabulary decision

**`BEGINNER_AND_NEW_ARE_SAME_CANONICAL_LEVEL` (A1) — approved.**

Evidence: `V1CatalogPilotIdentityPolicy.cs`'s own doc comment records that the backend's `RunningBackground` enum was originally three-valued (`NewToRunning`/`UsedToRun`/`RunningRegularly`) before Running Background V2 introduced the current four-valued `{Beginner, Intermediate, Advanced, Experienced}` set — `NewToRunning` is the direct lexical and semantic ancestor of both today's `Beginner` and the catalog's `"NEW"` experience tier (GEN.4 §3-4). The catalog's `"NEW"` tier is not a different domain concept independently authored; it is the pre-existing catalog-side label for the same runner-experience concept the backend later renamed to `Beginner`. This is corroborated by the peak-volume-band schema historically carrying real `"NEW"` rows (GEN.4 §3, §8) alongside `INTERMEDIATE`/`ADVANCED`/`EXPERIENCED` — a four-tier catalog taxonomy that lines up 1:1 with the backend's four-tier enum, with `NEW`↔`Beginner` as the one unmapped pairing.

**Approved canonical responsibilities:**
- API/domain vocabulary of record: `RunningBackground.Beginner`.
- Catalog compatibility key: `"NEW"`.
- The translation is a **new explicit entry** added to the existing single-owner mapping authority (§5/§6 below) — the same shape as the already-tested `Intermediate ↔ "INTERMEDIATE"` pairing, not a renamed persisted value and not a scattered string alias.

**Invariant approved: `LEVEL_VOCABULARY_TRANSLATION_IS_EXPLICIT`.** No code outside the one owning authority may compare `RunningBackground.Beginner` against the literal `"NEW"` or vice versa.

## 4. Experienced status

**`EXPERIENCED_OUTSIDE_CURRENT_V1_BUT_PRESERVED`.**

`Experienced` ↔ `"EXPERIENCED"` already has an unambiguous, pre-existing 1:1 catalog/domain pairing (no naming mismatch, unlike Beginner/NEW). V1's generalization target remains exactly `{Beginner, Intermediate, Advanced}`; `Experienced` is not activated, not merged into `Advanced`, and not removed. The Level identity authority (§5/§6) must represent `Experienced` as a known-but-currently-unsupported value (it fails the same `IsSupportedIdentity` membership test that `Beginner`/`Advanced` fail today, until and unless a future, separate phase explicitly adds it) — never silently reinterpreted as `Advanced` or dropped from the enum.

## 5. Candidate identity authority

**`LEVEL_IS_FIRST_CLASS_CANDIDATE_IDENTITY_DIMENSION` — approved.**

Evaluated identity tuple: `Distance × Level × RunsPerWeek × HorizonFamily`, per instruction, without imposing this shape if a cleaner equivalent exists. **HorizonFamily is explicitly excluded from this tuple**, per GEN.4 §17's own finding: horizon classification is `RaceHorizonPolicy`'s independent, race-date-derived concern by explicit design ("never a second, independently-computed value") and is evaluated as an orthogonal, separate gate, not as a dimension of candidate identity. The approved identity shape is therefore **`Distance × Level × Frequency`**, exactly mirroring the shape `V1CatalogPilotIdentityPolicy` already has for `Distance × Frequency` (post-3D generalization) — Level simply becomes a real, enumerated, multi-value dimension the same way `DaysPerWeek` already is, rather than a bare single-constant equality check.

**Required semantics, approved and binding:** identity resolution uses exact membership-set matching on every dimension, never proximity/interval logic. `TEN_K+BEGINNER+4D` must never resolve to `TEN_K+INTERMEDIATE+4D`; `ADVANCED` must never fall back to `INTERMEDIATE` or `BEGINNER`. This directly extends `NO_NEAREST_MATCH_FALLBACK`, already frozen, to the newly-added Level dimension — no new invariant is needed beyond applying the existing one to a wider set, and the existing fail-closed `_ => throw ArgumentOutOfRangeException(...)` catch-all pattern already in `ResolveCandidate` is confirmed (GEN.4 §16) to be the correct, reusable mechanism for preserving this.

## 6. V1CatalogPilotIdentityPolicy responsibility

**`CENTRAL_EXACT_IDENTITY_AUTHORITY_APPROVED`.**

Approved authority boundary: this policy (or its successor) owns **only** exact candidate identity resolution and rollout admission — the answer to "does this `(distance, level, daysPerWeek)` combination have a supported catalog candidate, and if so which exact `(candidateKey, candidateVersion)`." It must **not** become a source of training-load values, workout dosage, phase behavior, or RunLayout role structure — those remain owned by the catalog artifacts and planners the resolved candidate key points to, unchanged.

Conceptual target shape (authority boundary only, API not written): `ResolveCandidate(distance, level, daysPerWeek)` — three parameters, no `horizonFamily` parameter (§5), returning either a resolved `(candidateKey, candidateVersion)` pair or a fail-closed rejection. This is a direct, minimal extension of the existing `ResolveCandidate(int daysPerWeek)` shape to also close over `level`, mirroring the exact generalization already performed once for `daysPerWeek` (GEN.4 §16).

## 7. Support-vs-rollout decision

**`LEVEL_COMPATIBILITY_AND_LEVEL_ROLLOUT_ARE_SEPARATE_AUTHORITIES` — approved.**

This freezes, for Level, the exact separation already proven and exercised for 3D (GEN.4 §18): a `TEMPLATE_COMBINATION` artifact can be catalog-compatible (loadable, `DRAFT`/`VALIDATED`) without being publicly routable (`PUBLISHED` + `activationEnabled=true`), and a development-only `LocalCatalogAcceptance` override already demonstrates this separation is a real, working mechanism, not merely a theoretical one.

**Four conceptually distinguishable states, approved, not to be collapsed into one boolean:**
- **`UNSUPPORTED`** — the `(distance, level, frequency)` tuple fails `IsSupportedIdentity`'s exact-match test entirely; no candidate can be resolved.
- **`COMPATIBLE_BUT_GATED`** — identity resolves to a real candidate, but that candidate's lifecycle status is not `PUBLISHED` and/or `activationEnabled=false`; maps directly onto the existing `CatalogSupportedButNotPublished`/`CatalogSupportedButActivationDisabled` route outcomes.
- **`PUBLICLY_ACTIVE`** — identity resolves, candidate is `PUBLISHED`, activation is enabled; maps onto `CatalogLive`.
- **`PRODUCT_INELIGIBLE`** — **explicitly a different axis, not a fourth rollout-lifecycle rung.** This is a **per-request runtime outcome** (a specific user's specific numeric inputs produce an unrepresentable prescription under an otherwise `PUBLICLY_ACTIVE` candidate — §14/§18), not a property of the candidate's own compatibility/rollout state. A candidate can be `PUBLICLY_ACTIVE` and still yield `PRODUCT_INELIGIBLE` for a particular request. This distinction is recorded explicitly here so it is not accidentally flattened into a single rollout-state enum later.

A Beginner/Advanced artifact reaching `COMPATIBLE_BUT_GATED` must never automatically become `PUBLICLY_ACTIVE`; rollout policy (activation flag + publication status) must never manufacture compatibility (identity/candidate existence) either — the two gates remain independently controlled, exactly as they are today for the existing candidates.

## 8. Policy-dispatch authority

**`LEVEL_FREQUENCY_POLICY_DISPATCH_APPROVED`.**

Approved mechanism, not values: Level-and/or-Frequency-dependent training-load policy values must be selected through **explicit, versioned policy authorities** (named policy-object instances or catalog-artifact lookups, resolved by an explicit key), never through `if Beginner ... else if Intermediate ... else if Advanced ...` control flow scattered through planners. This directly targets the confirmed gap (GEN.4 §7/§9): `VolumeSafetyPolicy` is already a typed, versioned, constructor-injectable record — the approved fix is to make its **selection** a real keyed dispatch (extending the existing `ReferenceEquals`-based two-instance pattern into a genuine lookup), not to introduce a new policy-type hierarchy. `VolumeProgressionVerifier` is confirmed to already be fully generic with respect to whichever policy it is handed (GEN.4 §9) and requires no change under this decision.

**Required rule, approved and binding:** not every dimension needs to vary by Level — dispatch key granularity is decided per-concern in §9, not uniformly.

## 9. Policy key-granularity matrix

| Concern | Target key shape | Rationale |
|---|---|---|
| StartingWeeklyVolume fallback | `DISTANCE_LEVEL_FREQUENCY` | Currently Frequency-only (two instances, identical numbers today — GEN.4 §7); a conservative-default fallback is plausibly Level-sensitive (less-experienced runners warrant a more conservative missing/zero default) and should adopt the same shape as the sibling PeakVolumeBand mechanism for architectural consistency. Distance is currently trivially single-valued (TEN_K only) but included for shape-consistency with the peak-volume precedent, at zero present cost. |
| PeakWeeklyVolume | `DISTANCE_LEVEL_FREQUENCY` | Already the live, tested schema shape (GEN.4 §8) — confirmed, not newly decided. |
| ProgressionRate | `LEVEL_FREQUENCY` | GEN.4 §9 confirmed this is genuinely a 2-axis concern today mis-modeled as Frequency-only; documented evidence (`progression_rules_v2.yaml`) clearly establishes the Level axis, while the Frequency axis's necessity is less directly evidenced by that same document (the existing 3D/4D split was built to fix a specific taper conflict, not confirmed as a general belief that progression rate itself differs by frequency) — retained as a target dimension pending confirmation in evidence synthesis, not removed outright. |
| SessionAllocation | `FREQUENCY_ONLY_UNLESS_EVIDENCE_PROVES_LEVEL_EFFECT` | Confirmed level-blind mechanism today (GEN.4 §20/§21); the 4D layout and its minimum-floor constants are identical across all three target Levels with no repository evidence of a needed Level effect — do not create three versions of an identical policy without evidence. |
| LongRunPolicy | `FREQUENCY_ONLY_UNLESS_EVIDENCE_PROVES_LEVEL_EFFECT` | Same reasoning as SessionAllocation; inherits ProgressionRate's shape indirectly (the long-run share formula consumes progression-policy inputs) but is not itself assumed Level-varying independent of that. |
| WorkoutEligibility | `DISTANCE_LEVEL` | Already the live, working shape (`LEVEL_MODIFIER.eligibleWorkouts`, keyed by distance-family candidate + level, GEN.4 §12) — no Frequency dimension found in this mechanism; confirmed, not newly decided. |
| WorkoutDosage | `DECISION_DEFERRED_PENDING_EVIDENCE` | GEN.4 §21 found no maximum/dosage-scaling mechanism exists to evaluate against, and no repository evidence establishes whether dosage needs to vary by Level, Frequency, both, or neither — genuinely open, deferred rather than guessed. |

## 10. QualitySessionCount decision

**`QUALITY_SESSION_COUNT_IS_LEVEL_ELIGIBILITY_CAP_ONLY` — approved.**

Structural KEY/EASY/LONG cardinality remains exclusively `RunLayout`-owned (3D→1 KEY, 4D→1 KEY, future 5D+→2 KEY), consistent with the frozen `RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY` invariant. Level may express whether a given RunLayout-defined structural count `K` is **eligible/supported for that Level** (a product/routing decision — e.g., a future policy could in principle decline to offer a 2-KEY 5D+ layout to Beginner runners at all), but Level may never **silently redefine** `K` to a different value. This is a pure eligibility/routing concept layered on top of an unchanged structural count, exactly parallel to how `eligibleWorkouts` already filters *which* workout fills a KEY slot without ever changing *how many* KEY slots exist (GEN.4 §12).

**Invariant approved: `LEVEL_NEVER_OVERRIDES_RUNLAYOUT_KEY_CARDINALITY`.**

## 11. IntensityDistribution decision

**`INTENSITY_DISTRIBUTION_IS_DERIVED_VALIDATION_ONLY`.**

No first-class `LevelModifier.IntensityDistribution` authority is approved for V1 — none is created, per GEN.4 §13's confirmation that no first-class runtime concept exists to extend and intensity is fully emergent from workout identity + components + pace prescription. Level-specific workout eligibility and (once resolved, per §9) dosage may change the *resulting* distribution as an observable/derived property, but the distribution itself remains explicitly **not** a separate source of plan authority — no new abstraction is created merely because the original framework document listed this dimension.

## 12. PlanHorizonDefault decision

**`PLAN_HORIZON_DEFAULT_IS_UX_RECOMMENDATION_ONLY`.**

`RaceHorizonPolicy`'s date-arithmetic horizon classification remains the sole runtime horizon authority, entirely independent of Level, per GEN.4 §14's confirmation and its own explicit design goal ("never a second, independently-computed value"). If a Level-aware default/suggestion is ever wanted (e.g., nudging a Beginner runner toward a longer preparation window during onboarding), that is explicitly scoped as a **non-runtime-authority UX feature**, layered entirely outside this Level authority, and is not decided or authorized by this phase.

**Invariant approved: `RACE_TIMELINE_AUTHORITY_PRECEDES_LEVEL_DEFAULT`.**

## 13. Peak-volume authority

**`PEAK_VOLUME_BAND_IS_LEGITIMATE_CROSS_AXIS_POLICY_DATA` — approved.**

Key shape: `Distance × Level × RunsPerWeek` (matches the live schema exactly — GEN.4 §8; zero mechanism change required, `CatalogPeakVolumeBandLoader` is already fully generic on all three keys). Confirmed this does not create duplicated plan authority: the band owns only an allowed/target volume **envelope** (a `[min, max]` range), never week-by-week Core structure, phase allocation, workout sequence, or RunLayout content. Previously-established semantics are preserved: the peak-band floor is not a mandatory-attainment guarantee when another approved constraint (e.g. a taper-eligibility floor) binds more tightly. No Beginner/Advanced numeric rows are chosen here.

## 14. Starting/progression authority

**Starting volume**: `OBSERVED_RECENT_VOLUME_IS_USER_EVIDENCE_AUTHORITY` — approved. Confirmed already Level-independent by direct code read (GEN.4 §7); this decision freezes that it must **remain** so — valid observed evidence is never overridden merely because Level differs, unless a separately-approved readiness safeguard explicitly constrains it (no such safeguard is proposed here). Fallback-policy target authority: `DISTANCE_LEVEL_FREQUENCY_FALLBACK` (§9's StartingWeeklyVolume row — reasoning given there).

**Progression**: `GENERIC_ENGINE_WITH_LEVEL_POLICY_APPROVED`. The generic progression engine (`CatalogVolumeAndLongRunPlanner`) and the generic verifier (`VolumeProgressionVerifier`) remain unchanged and continue enforcing whatever policy instance they are handed — confirmed already fully policy-agnostic (GEN.4 §9). Only the **policy-selection dispatch** needs to become Level-aware (§8), not the engine or verifier's own logic. No percentage or absolute-cap values are chosen here.

## 15. Session allocation/long-run authority

Per §9: **`FREQUENCY_ONLY_UNLESS_EVIDENCE_PROVES_LEVEL_EFFECT`** for both `SessionAllocation` and `LongRunPolicy`. GEN.4 explicitly did not establish that these values differ by Level, and the 4D layout (and its minimum-floor constants) is confirmed identical across Beginner/Intermediate/Advanced today. This decision deliberately avoids creating three versions of an identical policy without evidence, while equally avoiding the opposite mistake of assuming Intermediate's specific values are automatically correct for other Levels merely because the role layout happens to match — the target key shape remains open to a future evidence-driven upgrade to `LEVEL_FREQUENCY` if and only if evidence synthesis (§25) later demonstrates a real Level effect.

## 16. Workout eligibility/dosage authority

**Eligibility**: `LEVEL_WORKOUT_ELIGIBILITY_AUTHORITY_REQUIRED` — but the authority **already exists** as a working mechanism (`LEVEL_MODIFIER.eligibleWorkouts`, `DISTANCE_LEVEL`-keyed, §9) and needs only new per-Level artifact content, not new architecture. Canonical 10K progression retains the same phase/stage vocabulary; Level filters which candidate workouts are eligible within that unchanged vocabulary — confirmed structurally sufficient, not requiring a duplicated progression timeline per Level (explicitly prohibited by instruction and consistent with `SINGLE_CANONICAL_10K_CORE_APPROVED`).

**Dosage**: `EXTERNAL_EVIDENCE_REQUIRED_TO_DECIDE`. No mechanism exists today to evaluate against, and no repository evidence establishes whether the same workout identity should carry Level-specific dosage. Whether a different workout identity would still belong to the same canonical stage is likewise unresolved from repository evidence alone. Both sub-questions are deferred to evidence synthesis (§25) rather than answered here.

## 17. Taper/product-ineligibility authority

**`GENERIC_TYPED_PRODUCT_INELIGIBILITY_ROUTING_APPROVED`.**

Frozen principle, approved and binding: **`INFEASIBLE_POLICY_COMPOSITION_MUST_FAIL_TYPED`** — never a silent clamp, synthetic workout, role deletion, or policy-value mutation. The existing outer public contract (`PlanProductIneligibleException` → HTTP 422, `Reason` as error code) is confirmed already fully generic (GEN.4 §19) and requires zero changes to serve additional Levels. The inner detection/translation layer is currently hand-wired to the specific `DaysPerWeek==3` case (a hardcoded `12d` floor literal, a concretely-named `ThreeDayCoreProductIneligibleException` type, two hardcoded catch-clause arms) — the approved pattern for any new Beginner/Advanced×4D conflict is a **new sibling internal exception type** with its own reason code, plus matching catch-clause arms in the same two known translation sites, following the exact shape already established — **not** a Level-specific exception-type hierarchy. Reason codes may legitimately be Level/policy-specific (e.g. a hypothetical future `FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM` distinct from `THREE_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`); the shared exception-routing infrastructure itself does not. No specific Beginner/Advanced reason code is approved or named here — that remains a later, evidence-informed step.

## 18. Runway/LongHorizon gate ownership

Governing principle, approved and binding: **`CORE_LEVEL_GENERALIZATION_DOES_NOT_AUTOMATICALLY_GENERALIZE_OTHER_HORIZONS`** — Core-path Level work must not automatically activate Beginner/Advanced for `PreparationRunway` or `LongHorizon`. Each of the four identified duplicate gates (`TenKPreparationRunwayDarkOrchestrator.ValidateRequest`, `LongHorizonRollingCheckpointRuntime.ValidateInput`, `LongHorizonRollingInitialActivationInputValidator.Validate`, `LongHorizonPublicPlanService.ValidatePilot` — GEN.4 §5/§15) receives a **two-part classification**, since the gate's *existence* and its *literal content* are separable concerns:

- **Gate existence/boundary**: `INTENTIONALLY_INDEPENDENT_DOMAIN_GATE` — approved to **remain** horizon-specific. Whether Runway/LongHorizon activate a new Level at all is correctly a separate, later decision from whether Core does, per the binding principle above.
- **Gate's internal literal comparison** (currently an independently-spelled `RunningBackground.Intermediate`/`"INTERMEDIATE"` check, not derived from the central authority): `TEMPORARY_DUPLICATE_TO_BE_RETIRED`. Each gate's *meaning of "Intermediate"* (or, eventually, whichever Levels it is willing to accept) should be sourced from the central identity/vocabulary authority (§5/§6) rather than independently re-spelled, even while the gate's own activation boundary stays intentionally horizon-specific. This directly satisfies the required principle stated in the phase prompt: horizon containment must be **explicit**, not accidental duplicated literals.

No `CONFLICTING_AUTHORITY` classification applies — all four gates currently agree with each other and with the central policy on what "Intermediate" means; they simply don't share one source of truth for that agreement, which is the specific defect this decision targets for eventual retirement (not implemented here).

## 19. Adaptation V1 Level decision

**`ADAPTATION_V1_REMAINS_LEVEL_AGNOSTIC_FOR_CURRENT_SCOPE` — approved.**

Confirmed by GEN.4's fresh, exhaustive re-grep (§3/§6): zero `RunningBackground` references anywhere in `Schedule/LongHorizon/Adaptation/**`. No Level-aware Adaptation behavior is introduced merely because Level generalization begins. Any future Level-specific Adaptation calibration (e.g., if a Beginner-specific missed-session tolerance were ever desired) explicitly requires its own separate decision/evidence phase and is not authorized, scoped, or implied by this decision.

## 20. Beginner/Advanced sequencing

**`BEGINNER_4D_FIRST_APPROVED`.**

Confirms GEN.4's `BEGINNER_4D_CHEAPER` finding as an approved implementation-sequencing decision (evidence-phase ordering only — not an implementation authorization). Rationale, weighing the given criteria: Beginner's primary risk (a low-volume taper-floor conflict) is a **known, already-solved-once problem class** with a directly reusable precedent (the 3D taper-eligibility work, §17) and a well-understood risk direction. Advanced carries the same architectural-gap burden as Beginner (§6 of GEN.4 — both are `NEEDS_ARCHITECTURE_CHANGE` for identical reasons) **plus two genuinely unresolved repository-evidence questions** (a long-run absolute-distance ceiling and a KEY-session/workout-component distance ceiling, both `CANNOT_ASSESS_WITHOUT_FURTHER_SEARCH`/`UNKNOWN` per GEN.4 §21) that would need dedicated investigation before Advanced's own decision inventory could be considered complete. Sequencing Beginner first does not block Advanced work from starting later, nor does it imply Advanced is deprioritized beyond ordering — it only governs which Level's evidence-synthesis phase (§26) begins first.

## 21. Final Level authority matrix

**`LEVEL_AUTHORITY_DECISION_MATRIX`**

| Concern | Final authority | Inputs/dimensions | Structural or policy? | Versioned data required? | External evidence required? | Product decision required? | Implementation impact |
|---|---|---|---|---|---|---|---|
| Level vocabulary | Central identity/vocabulary authority (§5/§6) | Backend enum ↔ catalog string | Structural (mapping) | No (code-level constant addition) | No | No | Small, additive (one new mapping entry) |
| Candidate identity | `V1CatalogPilotIdentityPolicy` (extended) | Distance × Level × Frequency | Structural | No | No | No (mechanism only; which Levels to enable is §7) | Small, mirrors existing Frequency extension |
| Rollout | Existing lifecycle-status + activation-flag mechanism (unchanged) | Candidate status, activation flag | Structural (already exists) | Yes (per-candidate artifact status) | No | Yes (when to flip to PUBLISHED — a later phase) | None to the mechanism itself |
| RunLayout | `RunLayout`/`RUN_LAYOUT_*` (unchanged, not Level-owned) | Frequency only | Structural | N/A — unaffected by Level work | No | No | None |
| KEY cardinality | `RunLayout` (unchanged); Level may only gate eligibility | Frequency (structure), Level (eligibility only) | Structural (RunLayout) + policy (Level eligibility) | Possibly (eligibility cap data, if ever used) | No | Deferred — no eligibility cap currently proposed | None required now |
| Starting observed volume | Unchanged (`ResolveStartingVolume`'s evidence path) | User-reported | Policy (already Level-independent) | No | No | No | None |
| Starting fallback | New Level-aware dispatch | Distance × Level × Frequency | Policy | Yes | Yes (whether/what values) | Yes | Dispatch-mechanism change; values deferred |
| Peak volume | `CatalogPeakVolumeBandLoader` (mechanism unchanged) | Distance × Level × RunsPerWeek | Policy (cross-axis data) | Yes (new rows) | Yes (values) | Yes (values) | Data-only |
| Progression | `VolumeSafetyPolicy` dispatch (extended) | Level × Frequency | Policy | Yes | Yes (confirm Frequency axis necessity + values) | Yes | Dispatch-mechanism change; values deferred |
| Session allocation | Unchanged (Frequency-only) pending evidence | Frequency | Policy | No (unless evidence changes this) | Yes (to justify any change) | Deferred | None now |
| Long-run | Unchanged (Frequency-only) pending evidence | Frequency | Policy | No (unless evidence changes this) | Yes | Deferred | None now |
| Workout eligibility | `LEVEL_MODIFIER.eligibleWorkouts` (mechanism unchanged) | Distance × Level | Policy (cross-axis data) | Yes (new modifier artifacts) | Yes (which workouts) | Yes | Data-only |
| Workout dosage | `DECISION_DEFERRED_PENDING_EVIDENCE` | Unresolved | Unresolved | Unresolved | Yes | Yes | Fully deferred |
| Intensity distribution | Not a first-class authority (§11) | N/A (derived only) | N/A | No | No | No | None |
| Plan horizon default | Not a runtime Level authority (§12) | N/A (UX-only, out of runtime scope) | N/A | No | No | Optional, separate UX feature | None to runtime |
| Taper | Generic typed-exception pattern (extended per-Level as needed) | Level × Frequency (floor derivation) | Policy + structural (exception routing) | No new type hierarchy | Yes (deriving 4D floor formula) | Yes (per-conflict reason codes) | Small, additive per conflict class |
| Product eligibility | Existing `PlanProductIneligibleException` contract (unchanged, reused) | N/A (routing only) | Structural | No | No | No | None to the outer contract |
| Adaptation | Unchanged, Level-agnostic (§19) | N/A | N/A | No | No | No | None |

Status legend applied: rows marked "No"/"Unchanged" across data/evidence/decision columns = **`NOT_LEVEL_AUTHORITY`** or **`APPROVED`** (mechanism frozen as-is); rows requiring new data/evidence = **`DEFERRED_TO_EVIDENCE`**; Workout dosage = **`DECISION_REQUIRED`** (fully open). No ownership is left implicit in prose beyond what is explicitly stated in this table.

## 22. Cross-axis policy boundary

**Frozen: `CROSS_AXIS_POLICY_DATA_DOES_NOT_OWN_PLAN_STRUCTURE`.**

Legitimate cross-axis data families identified and approved as a category (no numeric content invented):
1. **PeakVolumeBand** — `Distance × Level × Frequency → [minimumKm, maximumKm]` envelope. Already live and correctly shaped.
2. **StartingVolumeFallback** — `Distance × Level × Frequency → default km` (missing/zero-readiness case only; observed evidence is never cross-axis policy, it is raw user data, §14).
3. **ProgressionRate** — `Level × Frequency → {preferredRatio, hardCapRatio, taperMultiplier, cutbackReductionRange}`. Distance omitted from this family's key today since only one distance family is live; would extend to `Distance × Level × Frequency` if/when a second distance is generalized (out of scope here).
4. **WorkoutEligibility** — `Distance × Level → [eligible workout key/version references]`.

**Explicitly prohibited, per the frozen boundary and confirmed absent from the repository today**: any `Distance × Level × Frequency → authored complete week-by-week Core plan` artifact. No such artifact exists; combinations remain reference/compatibility manifests only (`COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST`, unchanged). SessionAllocation/LongRunPolicy are **not** approved as cross-axis families at this time (§15/§21) — they remain Frequency-only pending evidence, explicitly to avoid manufacturing a cross-axis family without justification.

## 23. Named invariants

The ten proposed invariants are adopted, verified consistent with every decision above, and one additional invariant is added:

- **GEN4A-INV-001** — Level never overrides RunLayout structural cardinality. *(§10)*
- **GEN4A-INV-002** — Candidate identity uses exact Level matching; no nearest-Level fallback. *(§5)*
- **GEN4A-INV-003** — Catalog compatibility and public Level rollout remain separate. *(§7)*
- **GEN4A-INV-004** — Valid observed recent-volume evidence remains user evidence; Level fallback policy does not silently replace it. *(§14)*
- **GEN4A-INV-005** — Level-specific policy values are selected through explicit/versioned authorities, not scattered control-flow literals. *(§8)*
- **GEN4A-INV-006** — Peak-volume cross-axis data owns an envelope, not Core structure. *(§13)*
- **GEN4A-INV-007** — IntensityDistribution is not allowed to become a duplicate workout/prescription authority unless separately approved. *(§11)*
- **GEN4A-INV-008** — Race timeline/horizon-family authority is not silently overridden by a Level default. *(§12)*
- **GEN4A-INV-009** — Unrepresentable Level policy combinations fail through typed product eligibility rather than silent policy mutation. *(§17)*
- **GEN4A-INV-010** — Core Level support does not activate Runway or LongHorizon Level support. *(§18)*
- **GEN4A-INV-011 (new)** — Horizon-specific Level gates (Runway/LongHorizon) must derive their canonical Level vocabulary/meaning from the central identity authority rather than independently re-spelling it, even while their own activation boundary remains intentionally horizon-specific. *(§18)*

## 24. External evidence backlog

**`LEVEL_EXTERNAL_EVIDENCE_BACKLOG`**

### BEGINNER
- Fallback starting weekly volume (missing/zero-readiness default).
- Progression rate (preferred/hard-cap weekly-increase ratios, cutback reduction range) — confirm whether Frequency is a genuine second axis or whether Level alone suffices.
- Peak weekly volume band (`[min, max]` envelope for 4D).
- Workout eligibility (which existing workout keys are appropriate; whether any new workout definitions are needed).
- Workout dosage (if evidence shows dosage should differ from Intermediate).
- Session allocation / long-run distribution — only if evidence later shows a genuine Level effect exists (current default posture is Frequency-only, §15).
- 4D taper-floor feasibility, once the above numeric values are chosen (derive whether the same conflict class GEN.4 flagged as structurally possible actually manifests for the chosen values).

### ADVANCED
- Same six dimensions as Beginner (fallback starting volume, progression rate, peak volume band, workout eligibility, dosage, session allocation/long-run if evidence-justified).
- Higher-volume/workout-component capacity: confirm whether any absolute long-run distance ceiling should exist (none found in code today — GEN.4 §21 row 10).
- KEY-session/workout-component distance ceiling: confirm whether any absolute maximum should exist on KEY-session or component distances (none found today — GEN.4 §21 row 11).
- Whether "Advanced = Intermediate + more volume" is even the correct structural relationship, or whether Advanced requires a materially different workout/dosage model (GEN.4 §21 explicitly found no repository evidence for the "+more km" assumption).

### SHARED
- Whether the `NEW`/`Beginner` catalog-content precedent from the historical 12-row peak-volume-band artifact (`1.0.0/policies/PEAK_VOLUME_BANDS_V1.v1.json`) carries any residual evidentiary value for the current values, or whether it should be treated as historical/superseded and re-derived independently.
- Whether `progression_rules_v2.yaml`'s documented-but-unwired Beginner/Advanced progression figures should be treated as a starting evidentiary reference for the progression-rate synthesis work, or independently re-validated.

No literature review or numeric synthesis was performed in this phase — this is a backlog inventory only.

## 25. Recommended next phase

```
PHASE 10K-GEN.4B — BEGINNER 4D TRAINING-LOAD & WORKOUT EVIDENCE SYNTHESIS
```

Per §20 (`BEGINNER_4D_FIRST_APPROVED`) and the dependency graph established across §8-§17 (every remaining open item is a *value*, not an *authority-shape*, question — all authority-shape decisions were resolved in this phase), the correct next step is evidence synthesis, not implementation and not a second decision phase. GEN.4B should consume the `BEGINNER` section of §24's backlog and produce candidate values/ranges for starting volume, progression rate, peak-volume band, and workout eligibility, explicitly deferring dosage and session-allocation/long-run per §9/§21 unless its own research surfaces a genuine Level effect. GEN.4B must not implement Beginner×4D — a subsequent product-decision phase (mirroring GEN.2B's structure for 3D) remains required after evidence synthesis before any implementation phase.

## 26. Files inspected

This phase performed no new repository exploration — it is a pure decision synthesis over `PHASE_10K_GEN_4_LEVEL_AUTHORITY_AUDIT.md`'s already-verified findings (all citations in that document, listed in its own §28, remain the evidentiary basis for every decision above; no new files were read and no new code/catalog state was queried in this phase).

## 27. Final classification

```
LEVEL_AUTHORITY_DECISIONS_APPROVED
```

Every non-numeric authority question required by the completion standard was explicitly resolved: Beginner↔NEW mapping resolved (§3); Experienced status preserved without merge (§4); Level assigned exact first-class identity authority with no-nearest-match preserved (§5); support/rollout separated into four distinguishable states (§7); Level policy-dispatch authority approved with per-concern key granularity, not uniform three-dimensionality (§8-9); structural KEY cardinality confirmed to remain RunLayout-owned with Level limited to eligibility-cap semantics (§10); IntensityDistribution and PlanHorizonDefault both resolved to non-first-class-authority status (§11-12); peak-volume cross-axis ownership approved (§13); starting/progression authority shape approved with observed-evidence primacy preserved (§14-15); session-allocation/long-run ownership explicitly deferred to evidence with a defined default posture, not left unresolved (§15); workout eligibility authority approved, dosage explicitly and honestly deferred (§16); generic typed-exception infeasibility routing approved (§17); Runway/LongHorizon containment made explicit via a two-part gate classification, closing the "accidental duplicated literal" gap identified by GEN.4 (§18); Adaptation V1 Level-agnosticism reconfirmed and scoped (§19); Beginner-first sequencing approved with evidence-based rationale (§20); exactly one next phase recommended (§25). No contradiction with the frozen compositional architecture was found at any point — `LEVEL_AUTHORITY_ARCHITECTURE_CONTRADICTION_FOUND` does not apply. All remaining numeric/value questions are cleanly separated into the evidence backlog (§24) and were not answered here.
