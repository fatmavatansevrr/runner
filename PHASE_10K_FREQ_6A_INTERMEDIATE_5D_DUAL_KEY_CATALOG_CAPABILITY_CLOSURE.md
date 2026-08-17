# PHASE 10K-FREQ.6A — Intermediate 5D Dual-KEY Catalog & Prescription-Capability Closure

## 1. Scope

This is an architecture and catalog-capability decision only. It creates no public activation, catalog artifact, schema, runtime code, numeric load authority, starting-volume or peak authority, allocation percentage, long-run percentage, or representability matrix.

Question closed: can the current catalog truthfully express the approved Intermediate × 5D policy, and, if not, what is the smallest generic extension?

Answer: **no**. The stable target is a generic, versioned prescription-profile capability plus coordinated progression lanes. No further product/domain decision is required before FREQ.6B; implementation remains outstanding.

## 2. Binding FREQ.6 policy

The following inputs are frozen and are not reopened:

- `RUN_LAYOUT_5D`: `KEY_SESSION × 2`, `EASY_SUPPORT × 2`, `LONG_RUN × 1`.
- Adaptation severity: 0–1 Reduce; 2–3 Maintain; 4/5 Progress only when the miss is EASY, otherwise Maintain; 5/5 Progress.
- KEY1 and KEY2 are adherence-severity equivalent; severity is phase-invariant; B1 worst-week-wins remains.
- Two structural KEY slots remain in Taper.
- Taper is `PRIMARY_SHARPENING + SECONDARY_CONTROLLED_SHARPENING`.
- Primary and secondary are prescription semantics, not structural roles.
- Frozen phase purposes:
  - Foundation: KEY1 controlled aerobic-strength/economy (short hills/strides); KEY2 controlled threshold introduction at lower fatigue/dose.
  - Build: KEY1 threshold/MIT; KEY2 controlled fartlek/VO2 support at lower accumulated stress.
  - RaceSpecific: KEY1 10K-specific rehearsal; KEY2 threshold support.
  - Taper: KEY1 reduced-dose 10K-specific sharpening; KEY2 reduced-dose economy/strides sharpening.

## 3. Current KEY capability inventory

`WORKOUT PURPOSE`, `WORKOUT IDENTITY`, `PRESCRIPTION STRUCTURE`, and `PROGRESSION / ROTATION STRUCTURE` are separate authorities. An identity existing in the catalog does not prove that its dose is executable.

### CURRENT_KEY_WORKOUT_CAPABILITY_MATRIX

| Workout | Role eligibility | Phase eligibility | Current prescription data | Reps / work / recovery | Intensity | Dose scaling / taper dose | Progression compatibility | Safe KEY1 | Safe KEY2 |
|---|---|---|---|---|---|---|---|---|---|
| `FARTLEK v4` | Referencable by a KEY progression stage; definition has no role field | Build | Modes, accounting mode, and ordered component labels | Not representable; only `WARM_UP`, `MAIN_SET`, `RECOVERY`, `COOL_DOWN` labels | `EASY`, `SURGE_AND_FLOAT`, `EASY_JOG` strings | No canonical dose or taper reduction | One candidate in a single phase-stage sequence | Purpose only; not a truthful executable dose | No; lower-stress complement cannot be encoded |
| `THRESHOLD_TEMPO v4` | Same | Build, RaceSpecific | Modes, accounting mode, component labels | Not representable; no repeats, work duration/distance, or recovery | `EASY`, `THRESHOLD` strings | No dose variants or approved minimum | Compatible only as the one selected weekly stage | Purpose only | No; controlled/lower-dose threshold is not distinguishable |
| `GOAL_PACE_TEN_K v2` | Same | RaceSpecific | Pace-based mode and component labels | Not representable | `EASY`, `GOAL_PACE` strings; runtime can derive exact goal pace | No rehearsal/sharpening dose semantics | One selected stage, with goal-feasibility fallback | Purpose and pace source only | No; support dose and independent lane absent |

Additional finding: the current Taper progression points to `EASY_STANDARD v4` and the planner marks it `BaselinePrescribedSharpeningPending`. This is explicit evidence that no executable Taper sharpening prescription currently exists.

The loader retains only component order, type, and intensity descriptor. The planner then invents component distances from session distance (`20%` warm-up/cool-down, `10%` recovery, `50%` other) rather than materializing catalog-owned repeats, durations, distances, and recoveries. Those percentages are implementation mechanics, not canonical workout prescriptions.

## 4. FARTLEK / THRESHOLD root-cause classification

The pre-existing GEN.4C issue is classified **F — MULTIPLE LAYERS**:

1. **Schema cannot represent the required structure (C).** `workout-definition.schema.json` has no repetition, work interval, duration/distance quantity, recovery duration/mode, dose variant, or prescription priority.
2. **Artifacts are consequently incomplete (A).** Current FARTLEK and THRESHOLD artifacts contain semantic labels only.
3. **Binder/planner cannot materialize it (D).** The planner uses generic distance heuristics and has no interval materializer.
4. **Loader/runtime drops or lacks the required contract (E).** Loader/runtime DTOs carry only coarse components, and progression/binding exposes one weekly stage.

This is therefore a **schema/progression/runtime architecture gap**, not merely a catalog-data gap. No stable, repository-wide GEN.4C debt identifier was found; this document references the pre-existing GEN.4C gap rather than inventing a duplicate ID.

## 5. Genericity analysis

The required facility is `GENERIC_WORKOUT_CAPABILITY`.

The minimum truthful model separates:

1. **Workout definition** — stable identity, family, purpose/capability, eligibility, supported intensity and prescription modes.
2. **Workout prescription profile** — a versioned generic artifact referencing a workout definition and owning typed ordered components: continuous or repeated work quantity, intensity, recovery quantity/mode, distance-accounting behavior, and an explicit dose category.
3. **Coordinated progression lanes** — an N-lane phase progression; each lane stage references exactly one prescription profile (plus explicit conditional fallback).
4. **Slot-to-lane binding** — deterministic ordinal binding among same-role slots while both structural roles remain `KEY_SESSION`.

No artifact or type may be named `*_5D_SECOND_KEY`. The model must serve other distances, levels, and future N-KEY layouts.

## 6. Foundation capability

Frozen pairing: controlled aerobic-strength/economy plus lower-dose controlled threshold introduction.

Current catalog cannot represent both truthfully. `EASY_STANDARD` substitution would be a fake KEY; duplicate stimuli would violate complementarity; THRESHOLD has no lower-dose structure; and no current Foundation-eligible pair supplies both purposes with executable prescriptions.

Classification: **FOUNDATION_5D_PAIR_REQUIRES_NEW_GENERIC_WORKOUT_CAPABILITY**.

Required capability: generic aerobic-strength/economy and controlled-threshold prescription profiles with typed work/recovery structure and phase eligibility. Whether those profiles reuse an existing workout identity or require a new generic identity is an implementation/catalog-authoring determination constrained by the frozen purposes, not a new 5D product decision.

## 7. Build capability

Frozen pairing: primary threshold/MIT plus secondary controlled fartlek/VO2 support.

FARTLEK and THRESHOLD names exist and are Build-eligible, but neither defines executable dosage, neither expresses primary versus controlled secondary dose, and the current progression cannot select them independently in one week.

Decision: **not currently representable; existing identities require versioned generic prescription profiles plus dual-lane progression**. The gap is not solved by placing two candidates in one stage.

## 8. RaceSpecific capability

Frozen pairing: primary 10K-specific rehearsal plus secondary threshold support.

`GOAL_PACE_TEN_K` can select target pace under its feasibility guard, but it has no rehearsal dose. `THRESHOLD_TEMPO` has no support-dose structure. Current single-stage progression rotates alternatives for one KEY slot and cannot produce a coordinated goal-pace/threshold pair.

Decision: **not currently representable; generic typed profiles and independent coordinated lanes are required**. Conditional goal-feasibility fallback must be lane-local and explicit, never “nearest workout.”

## 9. Taper capability

The system must retain two `KEY_SESSION` slots while reducing stress without changing taper-volume authority.

Current schema/catalog cannot encode two distinct sharpening prescriptions, strongly reduced secondary dose, retained specificity, or independent Taper lane lineage. The current `EASY_STANDARD` placeholder and `BaselinePrescribedSharpeningPending` status are not an implementation of sharpening.

Exact missing capability: **versioned, typed, reduced-dose prescription profiles selectable independently by two coordinated Taper progression lanes**. This keeps intensity/specificity in the profile, structural lineage in the slot, and total-volume authority outside the profile.

No KEY is deleted, reclassified, or coerced to EASY; no taper multiplier or structural minimum is changed.

## 10. Primary / secondary representation

Classification: **GENERIC_PRESCRIPTION_PRIORITY_FIELD_REQUIRED**.

The field belongs to prescription/progression policy, not RunLayout or the persistent structural-role enum. A generic lane/prescription priority such as `PRIMARY` / `SECONDARY_CONTROLLED` disambiguates intended dose and trace provenance. Both slots remain `KEY_SESSION`; adaptation continues to count them symmetrically.

The field is semantic, not merely a friendly name: validators use it to prevent two primary profiles, absent secondary profiles, or accidental duplication. The generated plan should retain lane/profile provenance for replay, but it does not gain a new structural role.

## 11. Dual-KEY progression audit

Current capability: **NO** — one phase/week cannot resolve two coordinated KEY prescriptions independently.

Evidence:

- `TEN_K_WORKOUT_PROGRESSION v5` has one ordered stage sequence per phase and no lane or slot ordinal.
- The stage allocator emits one `ProgressionStageKey` for the week.
- `CatalogWorkoutBinder` applies that same stage to every stage-controlled KEY slot. It requires exactly one candidate and rejects multiple candidates as ambiguous; therefore two KEY slots receive the same workout.
- `CatalogSessionPrescriptionPlanner` has a KEY ordinal only for distance-array lookup, not workout/progression selection.
- Per-slot lineage contains the same stage identity, so coherent independent rotation is impossible.

This model assumes `one phase/week → one selected KEY workout`. It does not implement `KEY slot 1 prescription + KEY slot 2 prescription`.

## 12. Coordination-model comparison

| Model | Authority clarity | Determinism | 8–14 compression | Future N-KEY / levels | Testability | Decision |
|---|---|---|---|---|---|---|
| A. One stage, two candidates | Candidate order is overloaded; current binder calls it ambiguous | Weak | Shared exposure counts cannot express independent lanes | Poor | Ambiguous selection/fallback | Reject |
| B. Two coordinated lanes | Explicit phase, lane, stage, profile, and fallback ownership | Strong ordinal mapping | Each lane uses the existing min/max/compression grammar under phase-level coordination | Generalizes to N lanes and is level-independent | Strong invariants and lineage | **Select** |
| C. Primary plus derived secondary | Secondary authority is hidden in derivation rules | Deterministic only after inventing derivation authority | Coupled and hard to audit | Weak when purposes diverge | Indirect | Reject |
| D. Phase pairing table plus independent dosage progression | Splits identity and dose across parallel authorities | Possible | Risks synchronization and duplicated phase rules | Moderate | More cross-artifact states | Reject |

Selected architecture: **B — coordinated progression lanes**, generalized as one lane per same-role slot rather than hard-coded “two.” A phase owns the synchronized lane set; each lane owns its stage exposure/compression rules and explicit fallback. Phase validation proves all required lanes resolve for every week.

## 13. Rotation

The frozen rotation maps to lane-local phase stages:

| Phase | Primary lane | Secondary-controlled lane |
|---|---|---|
| Foundation | Aerobic-strength/economy profiles | Controlled threshold-introduction profiles |
| Build | Threshold/MIT profiles | Controlled fartlek/VO2-support profiles |
| RaceSpecific | 10K-specific rehearsal profiles | Threshold-support profiles |
| Taper | Reduced-dose 10K-specific sharpening | Reduced-dose economy/strides sharpening |

For Core lengths 8–14, the existing phase allocator determines phase week counts. Each lane independently maps those counts through `minimumExposures`, `maximumExposures`, `compressionBehavior`, and `extensionBehavior`; a coordination validator requires one resolved profile per lane per phase week. This yields seven lengths from shared phase/lane rules, not seven copied plans.

Compression must preserve protected lane stages and deterministic relative order. Extension repeats only stages whose lane-local extension policy permits it. The Taper transition enters both Taper lanes simultaneously.

## 14. Fallback

Current binder does not support the approved dual-lane fallback semantics. It supports a stage-level explicit fallback in the one shared progression, then binds the result to every KEY slot.

Required generic behavior:

- fallback is declared on a lane stage and resolves to an explicit stage/profile in the same lane;
- fallback preserves phase, slot ordinal, structural role, prescription priority, and provenance;
- all lanes must resolve or the candidate is infeasible;
- nearest-workout fallback, KEY1 duplication, KEY2 deletion, EASY substitution, frequency coercion, and level coercion are forbidden.

## 15. Workout-minimum consequences

| Required minimum | Canonical result |
|---|---|
| Minimum representable threshold prescription | **NO_APPROVED_MINIMUM** |
| Minimum secondary controlled KEY dose | **NO_APPROVED_MINIMUM** |
| Minimum race-specific sharpening dose | **NO_APPROVED_MINIMUM** |
| Minimum Taper secondary dose | **NO_APPROVED_MINIMUM** |

The existing generic session-distance floor and heuristic component splits do not establish a viable workout dose. FREQ.6B/FREQ.6C must research and approve minima; this phase supplies only a schema capable of representing them.

## 16. Validator / binder impact

| Area | Classification | Required impact |
|---|---|---|
| Catalog schema | `GENERIC_CODE_EXTENSION` | New versioned prescription-profile schema and lane-capable progression schema |
| Catalog validator | `GENERIC_CODE_EXTENSION` | Typed quantity/recovery validation; lane cardinality, priority, eligibility, fallback, and coordination invariants |
| Workout binder | `GENERIC_CODE_EXTENSION` | Bind same-role slot ordinal to lane and exact profile; preserve provenance |
| Progression resolver | `GENERIC_CODE_EXTENSION` | Allocate/compress/extend stages per coordinated lane |
| Manifest graph | `GENERIC_CODE_EXTENSION` | Resolve profile→definition and progression→profile versioned dependencies |
| Runtime validation | `GENERIC_CODE_EXTENSION` | Require every lane/profile and validate materialized typed prescription/distance accounting |
| Dated skeleton validation | `NO_CHANGE` | It already validates generic KEY cardinality/spacing; it must not interpret priority |
| Adaptation role lineage | `NO_CHANGE` | Both remain symmetric `KEY_SESSION` inputs |
| Persistence | `GENERIC_CODE_EXTENSION` | Persist lane/profile/priority provenance for replay without changing structural-role enum |

No row needs a new product authority. Numeric profile contents remain intentionally deferred.

## 17. Regression containment

- Intermediate × 3D, Intermediate × 4D, and Beginner × 4D remain pinned to their existing rule-pack, manifest, workout, and progression versions.
- Published `FARTLEK v4`, `THRESHOLD_TEMPO v4`, `GOAL_PACE_TEN_K v2`, and `TEN_K_WORKOUT_PROGRESSION v5` remain immutable.
- Typed semantics are introduced through new schema/artifact versions; loaders support old and new contracts explicitly.
- No old component label gains retroactive repetitions, duration, recovery, or dose meaning.
- Single-KEY progressions continue through the legacy/single-lane compatibility path or an explicitly equivalent new version, verified by golden outputs.
- The 5D composition alone opts into the new coordinated-lane progression.

## 18. Versioning

Required strategy, without creating artifacts in this phase:

1. Add a new generic schema version for typed prescription profiles and a new lane-capable workout-progression schema version.
2. Publish new workout-definition versions only where identity capability/eligibility metadata changes; never mutate existing versions.
3. Publish new versioned prescription-profile artifacts for all selected phase/lane purposes and later numeric doses.
4. Publish a new `TEN_K_WORKOUT_PROGRESSION` version using coordinated lanes.
5. Publish a new 5D-specific composition manifest that selects the 5D layout, new progression, and exact profile graph.
6. Bump the rule-pack version that activates that manifest. Existing 3D/4D manifests remain pinned.

Schema capability is generic; the composition manifest is cell-specific by design. No public activation occurs here.

## 19. Required-capability matrix

### INTERMEDIATE_5D_REQUIRED_CAPABILITY_MATRIX

| Row | FREQ.6 purpose | Current capability | Gap | Minimal required extension | Generic? | Versioning impact | Complexity | Blocking? |
|---|---|---|---|---|---|---|---|---|
| Foundation KEY1 | Aerobic-strength/economy | Purpose fragments only | No executable Foundation profile | Typed profile + primary lane stage | Yes | New profile/progression | Medium | Implementation |
| Foundation KEY2 | Controlled threshold intro | Threshold identity lacks lower dose and Foundation pairing | Structure, eligibility, independent lane | Typed controlled profile + secondary lane | Yes | New versions/profile | High | Implementation |
| Build KEY1 | Threshold/MIT | Identity and labels | No exact work/recovery/dose | Typed threshold profile | Yes | New profile | Medium | Implementation |
| Build KEY2 | Controlled fartlek/VO2 support | FARTLEK identity and labels | No controlled complement or independent binding | Typed profile + secondary lane | Yes | New profile/progression | High | Implementation |
| RaceSpecific KEY1 | 10K rehearsal | Goal pace derivable | No rehearsal dose | Typed 10K profile + primary lane | Yes | New profile | Medium | Implementation |
| RaceSpecific KEY2 | Threshold support | Threshold identity | No support dose / simultaneous selection | Typed support profile + secondary lane | Yes | New profile/progression | High | Implementation |
| Taper KEY1 | Reduced 10K sharpening | EASY placeholder pending | No sharpening structure | Reduced-dose 10K profile | Yes | New profile/progression | High | Implementation |
| Taper KEY2 | Controlled economy/strides | None executable | No identity/profile/dose/lane | Typed reduced-dose economy profile | Yes | New artifact(s) | High | Implementation |
| Dual-KEY weekly progression | Two coherent prescriptions | One weekly stage copied to both slots | No lanes or per-slot lineage | Coordinated N-lane progression + ordinal binder | Yes | New schema/progression | High | Implementation |
| Fallback | Explicit deterministic lane fallback | Shared-stage fallback only | Cannot preserve secondary purpose | Lane-local exact fallback + fail closed | Yes | New progression/runtime | Medium | Implementation |
| Dose scaling | Primary and lower secondary/taper dose | Heuristic segment percentages | No catalog-owned typed dose or variants | Versioned typed prescription profiles/dose category | Yes | New schema/profiles | High | Implementation + later numeric data |

## 20. Technical debt

- The GEN.4C FARTLEK/THRESHOLD prescription-structure gap is reused by reference; no reliable canonical debt ID exists in the repository, so none is fabricated.
- Dual-KEY progression authority is resolved in this document as coordinated lanes; no new unresolved architecture-decision debt is created.
- The remaining work is an **implementation gap**, tracked by this phase classification: schema/profile contracts, validators, loaders, lane allocator, binder, planner/materializer, manifest graph, persistence provenance, tests, and artifact authoring.
- Numeric prescription values and minima are FREQ.6B/FREQ.6C authority work, not technical debt.
- Unrelated debts remain unchanged.

## 21. FREQ.6B input contract

FREQ.6B may assume:

- `KEY_SESSION × 2`, `EASY_SUPPORT × 2`, `LONG_RUN × 1`; both KEY slots persist in every phase including Taper.
- KEY slots are adaptation-severity equivalent and phase-invariant.
- The phase purposes and primary/secondary-controlled relationship listed in section 2 are binding.
- Primary/secondary is generic prescription priority and progression-lane provenance, never a structural role.
- A future generic typed profile can represent continuous or repeated work by distance/duration, repetitions, intensity, recovery quantity/mode, distance accounting, and dose category.
- Two coordinated lane progressions resolve one exact profile per KEY slot per week, with lane-local compression/extension and explicit fail-closed fallback.
- Taper retains two KEYs and uses reduced primary plus strongly controlled secondary sharpening without changing taper-volume authority.
- No canonical minimum workout dose currently exists for threshold, secondary controlled KEY, race-specific sharpening, or Taper secondary sharpening.

FREQ.6B must determine or leave explicitly unresolved:

- starting volume;
- peak band and peak-reference authority;
- weekly/session allocation shares and any floors/caps;
- long-run share/cap;
- exact interval, repetition, duration/distance, recovery, and minimum viable dose values;
- exact primary/secondary and Taper dose relationships;
- the later full representability matrix.

FREQ.6B must not treat the planner’s current heuristic component percentages or generic KEY session-distance floor as approved workout-dose evidence. It may safely conduct numeric research against the stable capability model, while recognizing that runtime activation remains blocked until implementation and artifacts exist.

## 22. Final classification

**INTERMEDIATE_5D_CATALOG_CAPABILITY_APPROVED_WITH_IMPLEMENTATION_GAP**

Architecture is approved: generic versioned typed prescription profiles, coordinated N-lane progression, deterministic same-role slot-ordinal binding, generic prescription priority, lane-local explicit fallback, and unchanged structural/adaptation roles.

Remaining blocker: implementation and catalog authoring. There is no remaining product-policy or architecture decision required for FREQ.6B numeric-authority research.

**FREQ.6B readiness: READY FOR NUMERIC-AUTHORITY RESEARCH; NOT READY FOR PUBLIC ACTIVATION OR 5D PLAN MATERIALIZATION.**
