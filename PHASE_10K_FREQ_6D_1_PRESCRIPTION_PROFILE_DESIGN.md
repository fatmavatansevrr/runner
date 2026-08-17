# Phase 10K-FREQ.6D.1 — Generic Workout Prescription Profile: Implementation Design

**Design only. No code, schema, catalog artifact, or migration written. Every FREQ.6/FREQ.6A/FREQ.6C decision cited from `PHASE_10K_FREQ_6C_CHECKPOINT_5D_PRE_CATALOG_BASELINE.md` (the sole binding input) is treated as frozen. Real current schemas/code were read in full before designing anything, not assumed.**

## A. WorkoutDefinition / PrescriptionProfile separation

### A1. Real current `WorkoutDefinition` shape (audited, `workout-definition.schema.json`)

```
metadata { documentType, schemaVersion, key, version, status, contentHash }
family: EASY | LONG_RUN | QUALITY | RACE
complexityTier
eligiblePhases: [FOUNDATION | BUILD | RACE_SPECIFIC | TAPER | PREPARATION_RUNWAY | LONG_HORIZON_GENERAL_ENDURANCE]
allowedPrescriptionModes: [DISTANCE | MIXED | PACE_BASED | EFFORT_BASED | HEART_RATE_BASED]
allowedDistanceAccountingModes: [EXACT_SESSION_TOTAL | ESTIMATED_SESSION_TOTAL | EMBEDDED_COMPONENTS]
components[]: { sequenceOrder, componentType: WARM_UP|MAIN_SET|RECOVERY|COOL_DOWN, intensityDescriptor: free string }
```

**Confirmed exactly what FREQ.6A §3 found**: `components` has no repetition count, work duration/distance quantity, or recovery duration/mode field anywhere — `intensityDescriptor` is an unconstrained string. This audit is accurate, re-verified directly against the real schema file, not assumed from the checkpoint alone.

**What stays on `WorkoutDefinition` (identity/eligibility — never moves)**: `family`, `complexityTier`, `eligiblePhases`, `allowedPrescriptionModes`, `allowedDistanceAccountingModes`, and the `components[]` array's existing coarse shape (order/type/intensity-descriptor) — this remains the *identity and eligibility* authority: "what kind of workout is this, and where is it allowed to appear." Real workout identities (`FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`) keep their existing keys/versions; nothing about *what a FARTLEK session is* changes.

### A2. `PrescriptionProfile` — new, generic, versioned

```
metadata { documentType: "WORKOUT_PRESCRIPTION_PROFILE", schemaVersion, key, version, status, contentHash }
workoutDefinitionRef: { documentType: "WORKOUT_DEFINITION", key, version }   -- exactly one, required
doseCategory: PRIMARY | SECONDARY_CONTROLLED                                 -- FREQ.6's real terminology (FREQ.6 §10/§13), not invented
workSegments[]: {
  sequenceOrder,
  segmentType: CONTINUOUS | REPEATED,
  # CONTINUOUS:
  workDurationMinutes?  workDistanceKm?          -- at least one required when CONTINUOUS
  # REPEATED:
  repetitionCount?  repetitionWorkDurationMinutes?  repetitionWorkDistanceKm?
  recoveryDurationMinutes?  recoveryDistanceKm?  recoveryMode?: JOG | WALK | STATIONARY
  intensityTarget: { mode: PACE_BASED|EFFORT_BASED|HEART_RATE_BASED, value/range }
}
distanceAccountingMode: EXACT_SESSION_TOTAL | ESTIMATED_SESSION_TOTAL | EMBEDDED_COMPONENTS   -- must be one of workoutDefinitionRef's allowedDistanceAccountingModes
```

**This is exactly, concretely, the shape FREQ.6A §3 found missing**: `workSegments[].repetitionCount` (missing today), `.recoveryDurationMinutes`/`.recoveryMode` (missing today), `.workDurationMinutes`/`.workDistanceKm` as typed quantities rather than a free-text `intensityDescriptor` string (missing today). `doseCategory` is the field FREQ.6A §10 classified as `GENERIC_PRESCRIPTION_PRIORITY_FIELD_REQUIRED` — placed here, on the *profile*, not on `WorkoutDefinition` or the structural-role enum, per FREQ.6A §10's own explicit ownership finding ("belongs to prescription/progression policy, not RunLayout or the persistent structural-role enum").

### A3. Versioning boundary (FREQ.6A §18 flagged as required, unspecified — designed here)

**Rule**: a new `WorkoutDefinition` version is required only when *identity or eligibility* metadata changes (`family`, `eligiblePhases`, `allowedPrescriptionModes`, `allowedDistanceAccountingModes`, or the coarse `components[]` shape). A new `PrescriptionProfile` version is required for *any* dose/structure change (work quantity, repetitions, recovery, intensity target, `doseCategory`) — even when it references the same, unchanged `WorkoutDefinition` version. This directly enables FREQ.6A §17's containment requirement: publishing new `PrescriptionProfile` artifacts for the 8 selected phase/lane purposes never requires touching (and therefore never risks) the pinned `FARTLEK v4`/`THRESHOLD_TEMPO v4`/`GOAL_PACE_TEN_K v2` identities 3D/4D/Beginner×4D already depend on.

## B. Coordinated progression lanes

### B1. Generic lane schema (N lanes, never hardcoded to 2)

```
phaseProgressions[].lanes[]: {
  laneOrdinal: integer >= 0,             -- binds to same-role structural slot ordinal (§C), NOT named "primary"/"secondary" in schema
  laneRole: string,                      -- e.g. "KEY_SESSION" -- which structural role this lane's slots belong to
  stages[]: {
    stageKey, relativeOrder,
    prescriptionProfileCandidateKeys[]: { key, version },   -- was workoutCandidateKeys/workoutCandidates, now profile refs not raw workout refs
    minimumExposures, maximumExposures, compressionBehavior, extensionBehavior, requires[],
    fallbackStageKey
  }
}
```

**Confirmed, not merely assumed**: `minimumExposures`/`maximumExposures`/`compressionBehavior`/`extensionBehavior` generalize **without modification** per-lane. Re-read the real schema (`workout-progression.schema.json` lines 45-48) — these fields are already scoped to one `stage` object; nesting the existing `stages[]` array one level deeper (inside a new `lanes[]` array, keyed by `laneOrdinal`) changes *where* the array lives, not the meaning of any field inside it. Each lane's phase allocator run is independent — FREQ.6A §13's own description ("Each lane independently maps those counts through minimumExposures/maximumExposures/compressionBehavior/extensionBehavior") is architecturally correct and requires no change to those four fields' real semantics, only to the loop that currently runs the allocator once per phase to instead run it once per (phase, lane) pair.

`laneOrdinal` is deliberately a plain integer, not `PRIMARY`/`SECONDARY`, keeping the schema N-lane-generic (per this phase's own explicit instruction) — `doseCategory` (§A2, on the `PrescriptionProfile`) is where `PRIMARY`/`SECONDARY_CONTROLLED` actually lives, decoupled from lane count.

### B2. Cross-lane coordination validator

**Input**: the fully-allocated set of `(lane, phase, week)` → resolved-stage assignments for one candidate's structural weeks (i.e., after each lane's own independent phase-allocator run, before binding). **Output**: `Valid` or a typed list of `(phase, week, laneOrdinal)` failures, each meaning "this lane has no resolved stage for this week." **Invariant enforced**: for every structural week, the count of resolved lanes must equal the count of same-role structural slots RunLayout declares for that role in that week (2, for `RUN_LAYOUT_5D`'s `KEY_SESSION`; 1, for every existing single-lane role). This is a pure cross-check over already-independently-computed lane outputs — it does not itself allocate anything, mirroring the existing separation between allocation and validation already used elsewhere in this codebase (e.g. `BoundCatalogPlanValidator` versus the allocators it checks).

### B3. Lane-local fallback

```
Decision tree, per (lane, phase, week):
1. Try the lane's own currently-assigned stage's prescriptionProfileCandidateKeys, filtered by `requires[]` conditions.
2. If none resolve AND stage.fallbackStageKey is set: retry step 1 using that fallback stage, WITHIN THE SAME LANE.
3. If still none resolve: this lane fails for this week.
4. After all lanes attempt 1-3 independently: if ANY lane failed, the whole candidate-week is infeasible -> typed PRODUCT_INELIGIBLE (mirroring the existing 3D/Beginner×3D taper-floor pattern's typed-exception shape, not a new mechanism).
```

Explicitly forbidden by this design, matching FREQ.6A §14 exactly: no fallback ever crosses lanes (lane 2 never borrows lane 1's resolved stage), never duplicates another lane's profile, never deletes a lane, never substitutes EASY, never coerces Level/Frequency/nearest-identity.

## C. Slot-to-lane binding

### C1. Deterministic ordinal binding, composing with FREQ.4's real mechanism

`CatalogSessionPrescriptionPlanner.Build` already computes a real `keyOrdinal` per `KEY_SESSION` slot within a week, in date order (`session.StructuralRole == "KEY_SESSION" ? keyOrdinal++ : -1`), confirmed by direct code read (FREQ.4's own real, live implementation). **This design reuses that exact same ordinal, unchanged, as the lane-selection key**: `laneOrdinal == keyOrdinal` selects which lane's resolved stage/profile applies to which physical slot. No second ordinal-assignment mechanism is introduced — the binder, when resolving a `StageControlled` session, now looks up `(laneOrdinal = keyOrdinal, week)` in the coordination validator's output instead of the current single `stageWeek.ProgressionStageKey` (confirmed, `CatalogWorkoutBinder.cs` line ~130, currently applies one stage key to every matching-role session in the week — this is precisely the "two KEY slots receive the same workout" defect FREQ.6A §11 found, now fixed by keying the lookup on `laneOrdinal` instead of being role-uniform).

### C2. Adaptation V1 impact — verified, not just asserted

Checked directly: `WindowExecutionSummaryBuilder` and `NextWindowLoadDecisionPolicy` (both re-read for this design) consume `LogicalSessionEvidence.Role` (a `PreparationRunwaySlotRole`, i.e. `KeySession`/`LongRun`/`EasySupport` — the *structural* role) and completion/outcome status. Neither type has, or under this design gains, any field referencing `laneOrdinal`, `PrescriptionProfile`, or `doseCategory`. Lane/profile selection happens entirely inside `CatalogWorkoutBinder`/`CatalogSessionPrescriptionPlanner`, which sit *upstream* of Adaptation V1 in the pipeline (prescription happens once at generation time; Adaptation V1 only ever observes the resulting structural role and completion status of already-generated sessions). **Confirmed: zero change required to `WindowExecutionSummaryBuilder`, `NextWindowLoadDecisionPolicy`, or any other Adaptation V1 component** — this is not an assumption carried over from FREQ.6 §5, it is independently re-verified here against this specific design's real data flow.

## D. Backward compatibility — single-lane is the real N=1 case, not a shim

A single-lane progression (every current 3D/4D/Beginner×4D artifact) is `lanes: [{ laneOrdinal: 0, laneRole: "KEY_SESSION", stages: [...] }]` — exactly one lane, `laneOrdinal` always `0`. The cross-lane coordination validator (§B2) degenerates correctly: "resolved lane count must equal same-role slot count" becomes "1 must equal 1," trivially true for every existing candidate, with zero special-casing required in the validator itself. `CatalogWorkoutBinder`'s lookup keyed on `(laneOrdinal = keyOrdinal, week)` degenerates to `(0, week)` for every existing single-KEY session, identical in effect to today's single, role-uniform lookup. **Achievable as a genuine degenerate case, not a parallel compatibility code path** — confirmed by tracing both the schema (§B1: `lanes[]` with one element is valid, no `oneOf`/`minItems` conflict with existing single-lane data once `workoutCandidateKeys`/`workoutCandidates` are renamed to `prescriptionProfileCandidateKeys` referencing new-schema profiles) and the binder logic (§C1) through the N=1 path explicitly, not merely asserted from the outside.

**One real, disclosed migration cost**: existing single-lane artifacts (`TEN_K_WORKOUT_PROGRESSION v5` and its referenced workout definitions) still need a new schema-version publication to gain the `lanes[]` wrapper and `PrescriptionProfile` references (FREQ.6A §18 already specified this: "Single-KEY progressions continue through the legacy/single-lane compatibility path or an explicitly equivalent new version, verified by golden outputs"). This design treats that as a real, required, but mechanical migration — not a design gap.

## E. Implementation sequencing proposal (not committed, for a future phase-splitting decision)

Real dependency graph, as designed above:

1. **`PrescriptionProfile` schema + `WorkoutDefinition` boundary clarification** (§A) — no dependency on anything else; can be built and golden-output-tested in isolation against existing single-lane data first (a `PrescriptionProfile` can validly exist for `EASY_STANDARD`/`LONG_RUN_STANDARD` today, proving the schema works, before any lane work starts).
2. **Lane-capable progression schema + phase allocator generalization** (§B1) — depends on (1) only for the `prescriptionProfileCandidateKeys` reference type; independent of (§B2/B3/C).
3. **Cross-lane coordination validator** (§B2) — depends on (2)'s real output shape existing; independently unit-testable against hand-constructed lane-allocation results, mirroring this engagement's own established synthetic-test pattern (FREQ.4).
4. **Lane-local fallback** (§B3) — depends on (2) and (3); the most implementation-adjacent-to-product-risk piece (typed-ineligibility routing), should reuse the exact `CatalogProductIneligibleException`-family pattern already established (FREQ.4D.2/GEN.4D.2), not invent a new one.
5. **Slot-to-lane binder integration** (§C1) — depends on (1)-(4) all existing; this is where `CatalogWorkoutBinder`/`CatalogSessionPrescriptionPlanner` actually change, the highest-real-regression-risk step since it touches the shared code path every 3D/4D/Beginner×4D candidate also runs through — should be built last and verified with the full existing regression suite (3480/3480 baseline, FREQ.4A) before any 5D-specific manifest is authored.
6. **Backward-compat migration** (§D) — can happen in parallel with (5) once (1)-(2)'s schemas are stable, since it's a data-authoring task (new versions of existing artifacts), not a code dependency.

Catalog authoring (actual FARTLEK/THRESHOLD/GOAL_PACE_TEN_K dose values, Foundation/Taper new identities) is explicitly out of scope for all of the above — it consumes this schema once built, per this phase's own DO NOT list.

## F. Final classification

```
GENERIC_PRESCRIPTION_PROFILE_DESIGN_READY_FOR_IMPLEMENTATION
```

No FREQ.6/6A/6C decision was revisited. No code, schema file, or catalog artifact was written. The design is genuinely N-lane-generic (no `laneOrdinal` value, field, or type name is 5D-specific), verified — not merely asserted — to require zero change to any frozen Adaptation V1 component, and confirmed to make single-lane compatibility a real degenerate case rather than a parallel shim.
