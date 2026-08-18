# Phase 10K-FREQ.6D.4C.1 — Workout-Definition Prescription-Capability Metadata Compatibility & Immutable-Version Boundary Decision

**Architecture-design phase. No production code, no catalog JSON modification, no profile authoring, no validator modification, no workout version bump, no product dosage change, no runtime change, no public activation. Design only — a later implementation phase executes it.**

## 1. Preflight

`PHASE_LEDGER.md` rows 60-62: `FREQ.6D.4A`/`4B`/`4C` all `DONE`/`VERIFIED`. `FREQ.6D.4C`'s final classification `FREQ6D4C_BLOCKED_ON_PROFILE_REPRESENTABILITY` confirmed by direct read of its report. Re-verified from the real repository, not the report's prose alone: the 4 approved `WorkoutDefinition` version files exist (`aerobic-strength-controlled-intro.v3.json`, `threshold-tempo.v5.json`, `fartlek.v5.json`, `goal-pace-ten-k.v3.json`, all `status: "DRAFT"`); zero `WORKOUT_PRESCRIPTION_PROFILE` documents exist anywhere in `plan-catalog/catalog/`; historical `v2`/`v4` content is untouched; `dotnet test` on `PlanCatalog.Tests` is green (1353/1353); the isolation-proof test (`ButForAllowedPrescriptionModes_TheFrozenFndPPrescriptionWouldValidateAndProjectLosslessly`) exists and passes. `FREQ.6D.4C.1` confirmed not already ledgered. `git rev-parse HEAD` → `04b4168dda1bb368bcede1f1bcbd1ac814a11f16`; `git status --short` → ` m baseline_tmp` only; `git diff --check` → clean.

## 2. Exact blocker reproduction

Re-run against real code (not re-derived from memory): `WorkoutPrescriptionProfileValidator.cs:54-55` fails closed when `workout.AllowedDistanceAccountingModes is null`; lines 57-68 map each profile component's `PrescriptionIntensityMode` to a `PrescriptionMode` value (`PaceBased→PaceBased`, `EffortBased→EffortBased`, `HeartRateBased→HeartRateBased`) and reject if `!workout.AllowedPrescriptionModes.Contains(requiredMode)`. Both confirmed exactly as FREQ.6D.4C's report described.

## 3. Eight-slot failure matrix

`PROFILE_COMPATIBILITY_FAILURE_MATRIX`

| Slot | WorkoutDefinition | Version | Profile intensity mode | DistanceAccountingMode | Current `allowedPrescriptionModes` | Current `allowedDistanceAccountingModes` | Validator failure | Historical/immutable? | Can a new version alone fix this exact reference? |
|---|---|---|---|---|---|---|---|---|---|
| FND-P | `AEROBIC_STRENGTH_CONTROLLED_INTRO` | v3 (new) | EffortBased | EstimatedSessionTotal | `[MIXED]` | `[ESTIMATED_SESSION_TOTAL]` | `PROFILE_INTENSITY_MODE_NOT_ALLOWED` | No (new, DRAFT) | Not via widening `allowedPrescriptionModes` alone without violating eligibility-only diff intent (§7/§9) |
| FND-S | `THRESHOLD_TEMPO` | v5 (new) | EffortBased | EstimatedSessionTotal | `[MIXED]` | `[ESTIMATED_SESSION_TOTAL]` | `PROFILE_INTENSITY_MODE_NOT_ALLOWED` | No (new, DRAFT) | Same as above |
| BLD-P | `THRESHOLD_TEMPO` | v4 | PaceBased | EstimatedSessionTotal | `[MIXED]` | `[ESTIMATED_SESSION_TOTAL]` | `PROFILE_INTENSITY_MODE_NOT_ALLOWED` | **Yes** | **No** — v4 is immutable |
| BLD-S | `FARTLEK` | v4 | EffortBased | EstimatedSessionTotal | `[MIXED]` | `[ESTIMATED_SESSION_TOTAL]` | `PROFILE_INTENSITY_MODE_NOT_ALLOWED` | **Yes** | **No** — v4 is immutable |
| RS-P | `GOAL_PACE_TEN_K` | v2 | PaceBased | EstimatedSessionTotal | `[PACE_BASED]` | **absent (null)** | `PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED` | **Yes** | **No** — v2 is immutable |
| RS-S | `THRESHOLD_TEMPO` | v4 | PaceBased | EstimatedSessionTotal | `[MIXED]` | `[ESTIMATED_SESSION_TOTAL]` | `PROFILE_INTENSITY_MODE_NOT_ALLOWED` | **Yes** | **No** — v4 is immutable |
| TAP-P | `GOAL_PACE_TEN_K` | v3 (new) | PaceBased | EstimatedSessionTotal | `[PACE_BASED]` | **absent (null)** | `PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED` | No (new, DRAFT) | **Yes** — v3 can still be completed before finalization |
| TAP-S | `FARTLEK` | v5 (new) | EffortBased | EstimatedSessionTotal | `[MIXED]` | `[ESTIMATED_SESSION_TOTAL]` | `PROFILE_INTENSITY_MODE_NOT_ALLOWED` | No (new, DRAFT) | Not via widening `allowedPrescriptionModes` alone |

Note `RS-P` fails on distance-accounting (root cause 1), `TAP-P` fails on distance-accounting too but is fixable directly since v3 isn't finalized yet, and the remaining six fail on intensity-mode (root cause 2) — three against immutable v4, three against new-but-inheriting-the-same-value v3/v5.

## 4. Legacy `MIXED` metadata semantics — real evidence

`PrescriptionMode.cs`'s own XML doc (quoted verbatim from source): *"Describes HOW a workout's headline dosage is prescribed. Distinct from `DistanceAccountingMode`... the two vocabularies must never overlap."* `Distance` and `Mixed` are `CANONICAL_CONFIRMED` — verbatim from Golden Fixture v3; `PaceBased`/`EffortBased`/`HeartRateBased` are `PLACEHOLDER_UNCONFIRMED` legacy values kept **only** because `GOAL_PACE_TEN_K` (itself flagged as having zero Golden Fixture v3 evidence at all) still uses `PaceBased`.

**Ground truth for what `MIXED` means**, from the real Golden Fixture (`golden-10k-intermediate-4d-12w.v3.plandocument.json`): it is a **whole-session label** applied when a workout's components carry heterogeneous per-component intensity (e.g. `EASY_WITH_STRIDES`: MAIN_SET at an easy pace range, STRIDES at a fast relaxed effort, COOL_DOWN easy — session-level `prescriptionMode: "MIXED"`). `MIXED` never appears as a per-component value anywhere in the fixture. `ten-k-pilot-vocabulary-decisions.md` confirms `FARTLEK`/`THRESHOLD_TEMPO` were **deliberately migrated** from invented `EFFORT_BASED`/`PACE_BASED` values to `MIXED` specifically because that is what the one piece of real evidence shows for structurally similar heterogeneous-intensity workouts — this was a considered correction, not an oversight.

**Answering §4's options directly**: `MIXED` means **(B)** — "a legacy prescription representation" describing session-level dosage heterogeneity, **not (A)** "supports multiple typed intensity families" and not a wildcard/wide-open value. Confirmed independently by the legacy backend runtime: `CatalogSessionPrescriptionPlanner.ModeFor` (`backend/RunningApp.Application`) treats `"MIXED"` as its own **first-class, successfully-handled** mode (`Contains("MIXED") → CatalogPrescriptionMode.Mixed`), never as "compatible with anything." Nothing in the schema, model, or design docs supports meaning (A), (C), or an undocumented (D).

## 5. Distance-accounting metadata semantics — real evidence

`WorkoutDefinition.AllowedDistanceAccountingModes`'s full doc comment (quoted verbatim): *"Optional/omittable: absent means 'not yet source-confirmed for this workout' rather than asserting a guessed value."* This field was **not always mandatory** — it was introduced after several `v1` catalog files already existed (confirmed: `goal-pace-ten-k.v1/v2/v3.json`, `easy-standard.v1.json`, `fartlek.v1.json`, `long-run-standard.v1.json`, `threshold-tempo.v1.json` all omit it, and every one of those is either a pre-field-introduction `v1` or `GOAL_PACE_TEN_K`, the one pilot workout explicitly flagged in every phase doc as having **zero** Golden Fixture v3 evidence). `ten-k-pilot-domain-decision-audit.md` row AUD-245 confirms this directly for `THRESHOLD_TEMPO` v1: *"v1's restored original content predates the AllowedDistanceAccountingModes field entirely... its absence here is the correct, faithfully-restored historical schema shape, not an omission."*

**Answering §5 directly**: absence is historically meaningful and **deliberate** — it means "genuinely unconfirmed," not "unsupported" and not an implicit wildcard. It is consumed only by `WorkoutDefinitionValidator` (rejects present-but-empty) and the new `WorkoutPrescriptionProfileValidator` (fails closed on null). No implicit-wildcard interpretation is adopted here — none is justified by the evidence.

## 6. Validator ownership audit — the load-bearing finding

Direct evidence the two checks are **not** on the same semantic footing:

- **Distance-accounting**: the profile's own `DistanceAccountingMode` field genuinely describes the same concept as the workout's `AllowedDistanceAccountingModes` — "how does this session's total distance reconcile with its components." This is **`CORRECT_AXIS_DIFFERENT_VALUES`** — the check is comparing the right two things; the historical `GOAL_PACE_TEN_K` values simply never had the workout-side value populated.
- **Intensity mode**: `PrescriptionIntensityMode` (`PaceBased`/`EffortBased`/`HeartRateBased`, a **per-component**, typed execution-targeting mechanism on a `PrescriptionProfileComponent`) is being checked against `AllowedPrescriptionModes` (a **session-level**, legacy, "how is the headline dosage prescribed" descriptor whose only confirmed real values are `Distance`/`Mixed`). These are different axes describing different things at different granularities. This is **`SEMANTIC_AXIS_MISMATCH`**.

**Direct, concrete evidence the mismatch was never noticed**: `WorkoutPrescriptionProfileValidatorTests.cs`'s own `StructuredControlledFartlek_IsRepresentable` test builds a **synthetic** `FARTLEK`-named workout declaring `AllowedPrescriptionModes = [EffortBased, PaceBased]` — values that **no real `FARTLEK` catalog file has ever declared** (the real file has always said `[MIXED]`, confirmed unchanged since the vocabulary-migration pass). The validator's own test suite passed (28/28) while exercising an idealized shape that never matched real catalog content — direct proof the cross-check was designed and verified without accounting for real, already-migrated `MIXED`-only workouts.

## 7. Immutability constraints

No mutation of `THRESHOLD_TEMPO` v4, `FARTLEK` v4, `GOAL_PACE_TEN_K` v2, or any other historical version is proposed anywhere in this design. `v3`/`v5` (the new `DRAFT` versions authored by FREQ.6D.4C) are **not** treated as historical — they were created by this engagement, remain `DRAFT`, and are not yet referenced by any real published bundle; completing their content (adding a field FREQ.6D.4C's own eligibility-only-diff self-constraint omitted) before they are finalized/promoted is not a violation of historical immutability.

## 8. 4B exact-reference preservation

Checked against all eight approved references (§3's matrix, last column). **All eight retain their exact FREQ.6D.4B-approved `(key, version)` reference under the selected architecture** (§21-22) — none requires a v4→v5 or v2→v3 substitution. `FREQ.6D.4B` **remains fully intact** — no `PRODUCT/DECISION` amendment is required.

## 9. Option M1 — reinterpret legacy metadata in the validator

Two sub-cases, evaluated separately per §14's own instruction not to force symmetry:

- **For intensity mode**: **REJECTED.** §6 already established `SEMANTIC_AXIS_MISMATCH`. Mapping `MIXED → "permits any typed intensity mode"` would assert something false (§4: `MIXED` means session-level heterogeneity, not "compatible with everything") purely to make profiles pass — exactly the failure mode §9 warns against. Blast radius is also large and silent: every other `MIXED`-declaring workout in the catalog (not just the 3 touched here) would instantly, silently gain "supports any typed intensity" status.
- **For distance-accounting**: a bare `null → wildcard` reinterpretation is **also rejected** — §5 established absence means "genuinely unconfirmed," not "unsupported" and not "anything goes." Treating null as a wildcard would silently let a profile-backed session claim a distance-accounting method the workout was never actually confirmed to support.

**M1 rejected for both root causes**, on semantic-correctness grounds, not mere distaste for enum-widening.

## 10. Option M2 — versioned WorkoutDefinition metadata only

Adding the missing typed capability metadata **only** to new versions (v3/v5) is semantically clean and requires no historical reinterpretation. **But the fatal question (§10) applies directly**: `BLD-P`/`BLD-S`/`RS-S`/`RS-P` pin **v4/v4/v4/v2 exactly**, per FREQ.6D.4B's explicit, frozen decision. A metadata-only fix on v3/v5 does nothing for those four references. **M2 alone is `INSUFFICIENT`** for 4 of 8 slots (all four `PROFILE_INTENSITY_MODE_NOT_ALLOWED` slots pinned to immutable v4, plus RS-P pinned to immutable v2) — consistent with the phase's own explicit expectation.

## 11. Option M3 — separate versioned capability overlay

A new artifact (e.g. `WorkoutPrescriptionCapability`), keyed by exact `(WorkoutDefinition key, version)`, declaring supplementary typed compatibility metadata **without mutating historical documents**, is evaluated for **each root cause separately**:

- **For distance-accounting** (RS-P → `GOAL_PACE_TEN_K` v2, immutable, genuinely missing metadata): this is exactly the right shape of fix. The overlay is purely additive/fallback — it never contradicts an explicitly-declared value on the workout itself (single-authority preserved: the workout's own field wins when present; the overlay only fills a genuine, disclosed historical gap). Authority duplication risk is low **if** scoped narrowly (only for historical versions genuinely missing the field, never overriding a present value) — this is a real design constraint the implementation must honor. Versioning/hash/graph-integrity: the overlay is its own versioned document, hashed independently, does not alter any existing document's hash or historical bundle replay. Maintenance cost: small, bounded to the one real gap found (`GOAL_PACE_TEN_K` v2) plus any future similarly-affected historical version. Not a "hidden override layer" as long as it can only ever supplement absent metadata, never override present metadata — this constraint must be an explicit, testable invariant in the implementation.
- **For intensity mode**: not needed — root cause 2 is a validator-check defect (§6), not a missing-metadata problem; an overlay would be solving the wrong problem (would still be asserting `MIXED`-based workouts "support" typed intensity modes they were never confirmed to, same objection as M1).

**M3 approved, narrowly, for the distance-accounting root cause only.**

## 12. Option M4 — profile owns execution capability / validator narrowing

Given `PrescriptionProfile` is architecturally the authority for executable dosage (structure mode, work quantity, intensity target — established, frozen, FREQ.6D.1A's own ownership split: WorkoutDefinition owns "default component vocabulary"/identity/eligibility, Profile owns "exact intensity mode/descriptor"), and given `AllowedPrescriptionModes` is proven (§6) to be the *wrong* axis to gate a profile's typed `IntensityTarget.Mode` at all: **removing (or fundamentally narrowing) `PROFILE_INTENSITY_MODE_NOT_ALLOWED`'s cross-check against `AllowedPrescriptionModes`** preserves safety rather than weakening it, because the check was never verifying a real invariant — it was comparing two unrelated vocabularies and happened to reject content it should never have been evaluating in the first place. The profile schema's own existing internal checks (`PROFILE_INTENSITY_MODE_DESCRIPTOR_MISMATCH` — exactly one matching descriptor per mode; `PROFILE_INTENSITY_MODE_INVALID` — undefined enum rejection) remain fully in force and are the **correct, sufficient** replacement invariant: they guarantee internal profile consistency without asserting anything false about the referenced `WorkoutDefinition`. No semantically-impossible profile/workout pairing becomes newly representable — `MIXED` continues to mean exactly what it always meant for its own real, original consumer (the legacy backend runtime planner), completely undisturbed.

**M4 approved, narrowly, for the intensity-mode root cause only.**

## 13. Option M5 — new versions + narrow 4B reference amendment

Evaluated and available as a fallback, but **not selected**, because M3+M4 together already achieve full 4B-reference preservation (§8) without requiring any reference change. Recording the classification anyway per §13's instruction: had M3/M4 not been viable, M5 would be `ARCHITECTURALLY_VALID` `BUT_REQUIRES_NARROW_PRODUCT_DECISION_AMENDMENT` (re-pinning `BLD-P`/`BLD-S`/`RS-S`/`RS-P` from v4/v4/v4/v2 to v5/v5/v5/v3 is a real product-adjacent change to FREQ.6D.4B's frozen exact references, requiring a DECISION phase, not an ARCHITECTURE_DESIGN phase, to approve). Not performed here or recommended, since it is strictly worse than the selected hybrid on the "4B exact-ref fidelity" criterion.

## 14. Selected architecture — hybrid, deliberately not forced through one mechanism

Per §14's own explicit instruction, the two root causes get **different, independently-justified fixes**:

- **Intensity-mode axis mismatch** → **M4**: remove/narrow `PROFILE_INTENSITY_MODE_NOT_ALLOWED`'s cross-check against `AllowedPrescriptionModes` (a validator change, in a later implementation phase).
- **Distance-accounting missing-metadata gap, on immutable v2** → **M3**: a new, narrowly-scoped capability-overlay artifact supplying `GOAL_PACE_TEN_K` v2's genuinely-missing `allowedDistanceAccountingModes` value, additive/fallback-only, never overriding an explicitly-declared value.
- **Distance-accounting gap on the new, not-yet-finalized v3** (`TAP-P`) → direct completion: add `allowedDistanceAccountingModes` to `GOAL_PACE_TEN_K` v3's content when it is finalized in the next implementation phase (not a historical-immutability violation — v3 is a new, `DRAFT`, engagement-authored artifact, not yet promoted or referenced by any published bundle).

## 15. Authority map — one owner per semantic

| Semantic | Owner |
|---|---|
| A. Structural workout capability (components, family) | `WorkoutDefinition` |
| B. Eligible phases | `WorkoutDefinition.EligiblePhases` |
| C. Typed prescription intensity compatibility | `WorkoutPrescriptionProfile` itself (no `WorkoutDefinition` gate — the profile schema's own internal descriptor/mode-consistency checks are sufficient and correctly scoped) |
| D. Allowed distance-accounting compatibility | `WorkoutDefinition.AllowedDistanceAccountingModes`, **with** the new narrow capability overlay as an additive fallback for historical versions genuinely missing it (never a contradicting second authority — the workout's own declared value always wins when present) |
| E. Executable dosage | `WorkoutPrescriptionProfile` (`WorkQuantity`, `RepetitionCount`, etc.) |
| F. Recovery semantics | `WorkoutPrescriptionProfile` (`RecoveryQuantity`/`RecoveryPlacement`) + `PrescriptionRecoveryCardinality.Derive` (Core) for the derived `RecoveryCount` |

No case exists where two artifacts assert conflicting values for the same semantic, and the validator never has to "guess priority" — each semantic has exactly one authority, with the overlay explicitly subordinate (fallback-only) to D's primary owner.

## 16. DRAFT lifecycle audit — load-bearing, only partially closeable here

- **Can a DRAFT `WorkoutDefinition` be referenced by a production profile?** Yes — `CatalogSourceSnapshot.FindWorkout(key, version)` (the exact-lookup overload used for all schemaVersion≥2 candidate-graph assembly, confirmed by its own doc comment: *"the only resolution path permitted for candidate graph assembly"*) performs **no status filtering** at all.
- **Can a DRAFT definition participate in a published bundle?** **No** — `CatalogPublisher.ExcludeDraftArtifacts` strips every `Draft`-status document (including `Workouts`) from the snapshot before publication, confirmed by direct code read. A profile pinned to a still-`DRAFT` `WorkoutDefinition` can be authored, validated, and graph-checked today, but **cannot yet appear in a real published bundle**.
- **Does exact-reference resolution permit it?** Yes (see above) — authoring/validation is unaffected by status.
- **Does publisher validation permit it?** Whether `CatalogBundleAssembler`/`PublishReadinessValidator` **fail closed** when an `ExactPrescriptionProjectionDependency` resolves to a workout that publish-time exclusion will strip is **not confirmed by this research pass** — this is disclosed honestly as unverified, not asserted either way. **Required verification item for the next implementation phase** (§23/§26), not resolved here.
- **Does FREQ.6D.4D require these versions to be `VALIDATED`?** Yes — real publication (the actual point of FREQ.6D.4D's bundle wiring) cannot include a `DRAFT` workout, so promotion is a genuine precondition for that phase, not for profile-authoring itself.
- **If promoted later, does legacy "highest non-retired" resolution still silently move 4D?** **Yes, this remains a real, unresolved risk** — see §17.

## 17. Legacy highest-non-retired resolver — classification

The resolver's own doc comment states plainly it is *"exactly the drift-prone behavior documented as a defect... must never be used to assemble a new (schemaVersion ≥ 2) candidate graph."* This is **known, pre-existing, documented debt**, not intentional canonical design for new work. Every real production call site found (`CatalogBundleAssembler`'s schemaVersion-1 legacy branch, `LevelModifierValidator`, `TemplateCombinationValidator`, `WorkoutProgressionValidator`) is consistently gated on a bare-key legacy field that only exists on schemaVersion-1 documents — confirming the doc comment's claim holds for every call site actually found. However, `FREQ.6D.4C`'s own real regression (setting the 4 new versions to `VALIDATED` broke `WorkoutArtifactImmutabilityTests`/`DependencyVersionCascadeTests`, both real, golden, live-4D-relevant tests) proves this legacy path **is** exercised by something real enough to matter for `THRESHOLD_TEMPO`/`FARTLEK` today.

Classification: **`SAFE_ONLY_WHILE_DRAFT`** (the current, actually-safe state, proven by the regression fix) **combined with a disclosed `ARCHITECTURE_DEBT_BLOCKS_ACTIVATION` consequence**: promoting these versions to `VALIDATED` — a genuine precondition for real publication — remains unsafe until this legacy resolution path is either migrated away from (whatever live artifact currently resolves `FARTLEK`/`THRESHOLD_TEMPO` via bare key should move to exact `(key,version)` references) or otherwise made safe (e.g. an explicit "pin active version" mechanism). **This phase does not refactor the resolver** (explicitly forbidden, §17 of the prompt) — it is recorded as a required, separate, not-yet-scheduled decision that must close before `FREQ.6D.4D` can promote these versions.

## 18. Status vs. exact-reference architecture

Confirmed two genuinely distinct notions already coexist in the real codebase, not invented here: **catalog authoring lifecycle status** (`Draft`/`Validated`/`Published`/`Retired`, gates publication) and **combination-specific exact dependency eligibility** (exact `(key,version)` resolution, used by schemaVersion≥2 candidate graphs, status-blind). New profile-backed paths safely use only the second; legacy paths use a mix (the versioned overload for schemaVersion≥2 fields, the bare-key overload for schemaVersion-1 fields) — the "two incompatible notions of active" risk (§18) is real but is entirely a consequence of the schemaVersion-1 legacy path's own known debt (§17), not something this metadata architecture introduces or must solve.

## 19. Historical bundle replay

For the selected hybrid: **no historical `WorkoutDefinition` or bundle content hash changes.** `v2`/`v4` files are never touched. The capability overlay (M3) is a wholly new, separately-hashed artifact — it cannot retroactively alter any already-computed historical hash, since nothing about the historical document's own serialized bytes changes. The validator narrowing (M4) changes **global** validation behavior for `MIXED`-declaring workouts going forward, but since no profile-backed content has ever existed for `THRESHOLD_TEMPO`/`FARTLEK`/`AEROBIC_STRENGTH_CONTROLLED_INTRO` before this engagement (confirmed: zero production profiles exist anywhere, per FREQ.6D.4C), there is no existing generated plan whose replay could be affected — the check being narrowed has never yet gated anything that actually shipped.

## 20. Future Half Marathon / Marathon suitability

Confirmed distance-family-agnostic: `PrescriptionMode`, `DistanceAccountingMode`, `AllowedPrescriptionModes`/`AllowedDistanceAccountingModes`, and `WorkoutPrescriptionProfileValidator` contain zero references to `DistanceFamily`, `5D`, `10K`, or any race-distance concept (confirmed by direct search — zero matches). The selected fix (narrow the intensity-mode check globally; add a distance-family-agnostic capability overlay keyed only by `WorkoutDefinition` key+version) generalizes to Half Marathon/Marathon workouts without any per-distance validator branch, per-distance overlay, or 5D-specific hack — satisfying §20's explicit generalization requirement.

## 21. Option matrix

`PRESCRIPTION_CAPABILITY_METADATA_OPTION_MATRIX`

| Option | Semantic correctness | Single authority | Historical immutability | 4B exact-ref fidelity | Legacy behavior | DRAFT/VALIDATED lifecycle | Replay safety | Genericity | Impl. complexity | Migration | Recommended? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| M1 (legacy reinterpretation) | Poor — asserts false compatibility on both axes | Preserved but wrong | Preserved | Full | Untouched | N/A | Preserved | Generic but semantically wrong everywhere | Low | None | **No** |
| M2 (new-version metadata only) | Good where applied | Preserved | Preserved | **Broken** — 4 slots pinned to immutable versions unfixed | Untouched | N/A | Preserved | Generic | Low | None | **No — insufficient alone** |
| M3 (capability overlay) | Good, narrowly scoped | Preserved (fallback-only) | Preserved | Full | Untouched | N/A | Preserved | Generic | Moderate (new artifact type) | None | **Yes — for distance-accounting only** |
| M4 (profile-owned / validator narrowing) | Best — removes a check comparing the wrong axes | Improved (removes a spurious second gate) | Preserved | Full | Untouched (MIXED's real consumer unaffected) | N/A | Preserved (nothing shipped depended on the old check) | Generic | Low (deletion/narrowing) | None | **Yes — for intensity-mode only** |
| M5 (new version + 4B amendment) | Good | Preserved | Preserved | **Broken** — requires re-pinning 4 references | Untouched | N/A | Preserved | Generic | Low | Requires a DECISION phase | No (unnecessary given M3+M4 suffice) |
| **Selected hybrid (M3+M4+direct v3 completion)** | **Best** | **Preserved, one owner per semantic (§15)** | **Preserved** | **Full — 4B fully intact** | **Untouched** | **Unresolved — see §16-17** | **Preserved** | **Fully generic, no per-distance coupling** | **Moderate** | **None required for 4B** | **Yes** |

## 22. Selected architecture

The hybrid in §14/§21, in priority order per §22's own ranking: (1) semantic correctness — satisfied, each fix addresses its real root cause rather than papering over it; (2) no historical mutation — satisfied, zero historical documents touched; (3) single clear authority — satisfied per §15's map; (4) exact-reference compatibility — satisfied, all 8 FREQ.6D.4B references preserved unchanged; (5) fail-closed behavior — satisfied, missing overlay/metadata still fails closed, no implicit wildcard introduced anywhere; (6) no silent legacy behavior change — satisfied for the metadata fix itself, though the **separate, disclosed** DRAFT→VALIDATED/legacy-resolver risk (§17) remains a real, open item; (7) generic future-distance support — satisfied (§20); (8) deterministic versioning — satisfied, overlay is its own versioned, hashed artifact; (9) testability — satisfied, both fixes are narrow and directly testable; (10) implementation cost — moderate, not optimized away at the expense of correctness, consistent with §22's explicit "do not optimize for fewest changed files."

## 23. 4B amendment requirement

**NO.** All eight frozen profiles retain their exact `WorkoutDefinition` references unchanged (§8). `FREQ.6D.4B` remains fully intact — no `DECISION`-type amendment phase is required before implementation can proceed.

## 24. Required vs. optional artifact/schema changes

**REQUIRED**:
- A narrow change to `WorkoutPrescriptionProfileValidator.cs`: remove or fundamentally rescope the `PROFILE_INTENSITY_MODE_NOT_ALLOWED` cross-check against `AllowedPrescriptionModes` (M4).
- A new capability-overlay artifact type (schema + Core model + snapshot list + loader wiring + a fallback lookup inside the `AllowedDistanceAccountingModes` check, additive-only, never overriding an explicitly-declared workout value) (M3), plus exactly one real overlay entry: `(GOAL_PACE_TEN_K, v2) → [ESTIMATED_SESSION_TOTAL]`.
- Completing `GOAL_PACE_TEN_K` v3's content (adding `allowedDistanceAccountingModes: [ESTIMATED_SESSION_TOTAL]`) before it is finalized/promoted — a direct edit to a still-`DRAFT`, not-yet-immutable, engagement-authored file, not a historical mutation.
- Before `FREQ.6D.4D` (real bundle publication): resolve the DRAFT→VALIDATED/legacy-resolver risk (§17) — exact mechanism not yet decided, flagged as an open decision (§26).
- Verification (not yet performed) of whether `CatalogBundleAssembler`/`PublishReadinessValidator` fail closed on a `DRAFT`-resolving `ExactPrescriptionProjectionDependency` (§16).

**OPTIONAL / not required by this design**: no schema change to `WorkoutPrescriptionProfile` itself; no status/lifecycle model change; no resolver refactor (explicitly out of scope).

## 25. Failure semantics

| Case | Behavior |
|---|---|
| Unsupported typed intensity | No longer checked against `WorkoutDefinition` (M4) — governed entirely by the profile schema's own internal mode/descriptor consistency checks, unchanged, still fail-closed on malformed input |
| Unsupported accounting mode | Fails closed via `PROFILE_DISTANCE_ACCOUNTING_MODE_NOT_ALLOWED`, now checking workout value first, then the narrow overlay as fallback; still fails closed if neither declares it |
| Missing compatibility metadata, no overlay entry either | Fails closed — no implicit wildcard, matching §5's finding that absence means "unconfirmed," never "anything goes" |
| Legacy `MIXED` encounter | No special handling needed — remains fully meaningful for its real, original, undisturbed consumer (legacy backend runtime prescription mode selection) |
| Unknown future enum value | Fails closed via existing `Enum.IsDefined` checks, unchanged |
| DRAFT artifact reference | Legal for authoring/graph-validation; **must** fail closed at publish time if it would otherwise produce a bundle referencing content excluded by `ExcludeDraftArtifacts` — verification required (§16/§24), not yet confirmed which way current behavior falls |
| Historical exact version | Resolves to exact, hash-stable content forever, unaffected by any part of this design |

## 26. Implementation sequence

Since no 4B amendment is required, the sequence is direct:

```
FREQ.6D.4C.2  → implement the M4 validator narrowing + M3 capability-overlay artifact
                 + complete GOAL_PACE_TEN_K v3's content
                    ↓
FREQ.6D.4C.3  → retry production profile authoring (all 8 slots) against the now-fixed
                 catalog, exactly per FREQ.6D.4B's frozen matrix
                    ↓
[separate, not-yet-scheduled item] → resolve the DRAFT→VALIDATED / legacy-resolver
                 architecture debt (§17) before any real publication is attempted
                    ↓
FREQ.6D.4 (resumed) / FREQ.6D.4D  → dual-lane progression/runtime integration, bundle wiring
```

## 27. Open decisions (§27 stop check, answered explicitly)

- Would an implementation agent still have to decide what `MIXED` means? **No** — resolved (§4/§6/§12): it is left fully alone, meaningful only for its real, original consumer.
- Whether missing accounting means wildcard? **No** — resolved (§5): never a wildcard, always fail-closed absent an explicit value or overlay entry.
- Whether to change v4→v5 (or v2→v3) for any of the 8 references? **No** — resolved (§8/§23): none change.
- Whether DRAFT may publish? **Not fully resolved** — genuinely deferred (§16/§24), disclosed as a required verification item, not silently assumed either way.
- Whether to mutate historical metadata? **No** — resolved (§7/§19): never proposed.

One real open item remains (DRAFT-publish-time behavior verification, plus the separate DRAFT→VALIDATED/legacy-resolver debt) — both explicitly scoped, both assigned to specific future steps, neither left as an implicit choice for an implementation agent to invent an answer to.

## 28. Final classification

```
FREQ6D4C1_ARCHITECTURE_APPROVED_WITH_CATALOG_LIFECYCLE_BLOCKER
```

Not the unconditional `_ARCHITECTURE_APPROVED` (4B is fully intact, but a real, disclosed catalog-lifecycle question — DRAFT→VALIDATED promotion safety against the legacy highest-non-retired resolver — remains genuinely open and must close before `FREQ.6D.4D` can publish). Not `_WITH_PRODUCT_REF_AMENDMENT_REQUIRED` (no reference changes needed). Not `_CAPABILITY_MODEL_DESIGN_INCOMPLETE` (both root causes have a fully specified, evidence-grounded fix — §24's required artifacts are exact, not open-ended). Not `_METADATA_SEMANTICS_EVIDENCE_INCOMPLETE` (real, direct evidence was found and quoted for every semantic question asked).
