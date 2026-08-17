# Phase 10K-FREQ.6D.1A — Generic Prescription / Dual-Key Design Contract Evidence Completion

**Design-verification follow-up only. No production code, schema mutation, catalog authoring, version bump, public routing change, or FREQ.6/FREQ.6C policy change. Every claim below is traced to real code read in full during this phase (via a research pass over `backend/`) — file:line citations are given throughout. Where the parent phase (FREQ.6D.1) made a claim this evidence pass could not confirm — or actively contradicts — that is stated explicitly, not smoothed over.**

Parent status before this phase: `DESIGN_EVIDENCE_INCOMPLETE`. This document is the closure attempt.

---

## 1. Binding findings re-confirmed

- `CatalogWorkoutBindingAmbiguousCandidateException` — confirmed real, `CatalogWorkoutBinder.cs:145-150`, thrown when a stage declares more than one `WorkoutCandidateReferences` entry ("V1 does not implement a multi-workout selection policy").
- `stageWeeksByNumber` keyed by week, not lane — confirmed real, `CatalogWorkoutBinder.cs:105`: `context.StageSchedule.Weeks.ToDictionary(w => w.WeekNumber)`. A `Dictionary` keyed solely on week number — would throw on insert if two stage rows existed for one week today. `ScheduledProgressionWeek.StructuralRole` (the allocator's own output type) is in fact a **hardcoded constant `"KEY_SESSION"`**, not a derived/parametrized value (`ProgressionStageScheduleContracts.cs`, `StructuralRole => KeySessionStructuralRole`) — the schema has literally no notion of "which KEY slot" today, confirming the gap even more concretely than the parent phase stated.

These two facts prove the gap. They do not, by themselves, prove the replacement below — every claim in Sections A–E is separately grounded.

---

## SECTION A — PRESCRIPTION PROFILE DOMAIN CONTRACT

### A1. Exact type shape

```csharp
public sealed class PrescriptionProfile
{
    public required string ProfileKey { get; init; }                       // catalog identity, distinct namespace from WorkoutDefinition.key
    public required int ProfileVersion { get; init; }
    public required PlanCatalogReference WorkoutDefinitionRef { get; init; } // exact (key, version) — required, exactly one
    public required PrescriptionDoseCategory DoseCategory { get; init; }    // PRIMARY | SECONDARY_CONTROLLED — FREQ.6's real terms, reused verbatim
    public required PrescriptionDistanceAccountingMode DistanceAccountingMode { get; init; } // MUST be a member of WorkoutDefinitionRef's allowedDistanceAccountingModes
    public required IReadOnlyList<PrescriptionComponent> Components { get; init; }
}

public sealed class PrescriptionComponent
{
    public required int SequenceOrder { get; init; }                       // MUST equal the corresponding WorkoutDefinition.components[].sequenceOrder — see A4
    public required PrescriptionComponentType ComponentType { get; init; }  // WARM_UP | MAIN_SET | RECOVERY | COOL_DOWN — same enum as today, reused not replaced
    public required PrescriptionStructureMode StructureMode { get; init; } // CONTINUOUS | REPEATED
    public PrescriptionWorkQuantity? WorkQuantity { get; init; }            // required for WARM_UP/MAIN_SET/COOL_DOWN; null only for a standalone RECOVERY component type (rare — recovery is normally embedded via RecoveryQuantity, see below)
    public PrescriptionRecoveryQuantity? RecoveryQuantity { get; init; }    // required iff StructureMode == REPEATED; forbidden iff StructureMode == CONTINUOUS
    public required PrescriptionIntensityTarget IntensityTarget { get; init; }
}

public sealed class PrescriptionWorkQuantity
{
    public int? DurationSeconds { get; init; }
    public int? DistanceMeters { get; init; }
    public int? RepetitionCount { get; init; }
    // INVARIANT (validated, not merely documented):
    //   CONTINUOUS: exactly one of DurationSeconds/DistanceMeters set; RepetitionCount MUST be null.
    //   REPEATED:   RepetitionCount required, >= 2; exactly one of DurationSeconds/DistanceMeters set,
    //               and that value is the PER-REPETITION work quantity, never a total.
}

public sealed class PrescriptionRecoveryQuantity
{
    public int? DurationSeconds { get; init; }
    public int? DistanceMeters { get; init; }
    public required PrescriptionRecoveryMode Mode { get; init; }            // JOG | WALK | STATIONARY
    // INVARIANT: exactly one of DurationSeconds/DistanceMeters set.
}

public sealed class PrescriptionIntensityTarget
{
    public required PrescriptionIntensityMode Mode { get; init; }           // PACE_BASED | EFFORT_BASED | HEART_RATE_BASED — MUST be a member of WorkoutDefinition.allowedPrescriptionModes
    public string? PaceDescriptorKey { get; init; }                         // reference into the existing pace-zone vocabulary, never a raw number/string
    public string? EffortDescriptorKey { get; init; }
    public string? HeartRateZoneKey { get; init; }
    // INVARIANT: exactly one of PaceDescriptorKey/EffortDescriptorKey/HeartRateZoneKey populated, matching Mode.
}
```

Direct answers to the required concept list:
| Concept | Representation |
|---|---|
| Repetition count | `WorkQuantity.RepetitionCount`, only meaningful/allowed when `StructureMode == REPEATED` |
| Work duration | `WorkQuantity.DurationSeconds` |
| Work distance | `WorkQuantity.DistanceMeters` |
| Recovery duration | `RecoveryQuantity.DurationSeconds` |
| Recovery distance | `RecoveryQuantity.DistanceMeters` (supported) |
| Recovery mode | `RecoveryQuantity.Mode` |
| Intensity/effort | `IntensityTarget` (typed, discriminated by `Mode`) |
| Warm-up / cool-down | `ComponentType.WARM_UP` / `COOL_DOWN`, each a `CONTINUOUS` component |
| Continuous tempo | One `MAIN_SET` component, `StructureMode = CONTINUOUS` |
| Intervalized threshold | One `MAIN_SET` component, `StructureMode = REPEATED`, `RecoveryQuantity` populated |
| Fartlek (structured) | One `MAIN_SET` component, `StructureMode = REPEATED` — a fixed rep-count/duration surge+float pattern (e.g. "6×1min/1min") maps cleanly |
| Fartlek (unstructured/self-selected) | **Does not map** — see below, disclosed as a real limitation, not silently forced |
| Taper-reduced dose | A distinct, separately-authored `PrescriptionProfile` (own version or own key) referenced only by the TAPER-phase stage — never a runtime-computed scaling of another profile, consistent with this engagement's standing prohibition on deriving one cell's value from another's |

**Disclosed limitation (not resolved here)**: unstructured fartlek ("20 min continuous fartlek, self-selected surges") has no fixed rep count or per-rep duration and cannot be represented as a `REPEATED` component under this model without inventing a fabricated rep count. This is a genuine open item, carried into §3.

### A2. Unit discrimination — no ambiguous values

Every quantity field above is unit-typed at the type level (`DurationSeconds` vs `DistanceMeters` vs `RepetitionCount` are separate nullable properties, never a bare unlabeled number), and each carrying type states its own mutual-exclusion invariant (`WorkQuantity`, `RecoveryQuantity` above). `duration=5, distance=1000, repetitions=4` on one component is structurally impossible for a `CONTINUOUS` component (rejected: `RepetitionCount` must be null) and structurally well-defined for `REPEATED` (duration=5s **or** distance=1000m, whichever populated, is the per-repetition work quantity; `repetitions=4` is separately meaningful) — duration and distance remain mutually exclusive in both modes; they are never allowed to coexist on the same `WorkQuantity` instance, because a single component cannot be prescribed by two independent quantity systems simultaneously without an implied (and unstated) pace — a real ambiguity this design deliberately closes rather than allowing.

### A3. Ownership boundary

`WORKOUT_DEFINITION_OWNERSHIP_TABLE`

| Concept | Owner |
|---|---|
| Workout physiological identity (key) | `WorkoutDefinition` |
| Workout family | `WorkoutDefinition.family` |
| Complexity | `WorkoutDefinition.complexityTier` |
| Allowed prescription modes | `WorkoutDefinition.allowedPrescriptionModes` (ceiling); `PrescriptionProfile.IntensityTarget.Mode` must be a member |
| Default component vocabulary (order/type, coarse) | `WorkoutDefinition.components[]` — kept, unchanged, identity-level |
| Exact reps | `PrescriptionProfile` (`WorkQuantity.RepetitionCount`) |
| Exact work duration/distance | `PrescriptionProfile` (`WorkQuantity`) |
| Exact recovery | `PrescriptionProfile` (`RecoveryQuantity`) |
| Dose category (PRIMARY/SECONDARY_CONTROLLED) | `PrescriptionProfile.DoseCategory` |
| Phase-specific dose | Progression **stage** — which `PrescriptionProfile` version a given phase's stage references |
| Primary/secondary KEY use | **Lane** (§B) — a lane's stages reference profiles; the lane itself carries no dose-category field, orthogonal by design (flagged as an open authoring-convention question, §3) |
| Taper-reduced dose | `PrescriptionProfile` — a distinct artifact/version referenced only by the TAPER-phase stage |

### A4. Relation to the current component model — **Option B, wraps and extends, justified**

Read `workout-definition.schema.json` in full during FREQ.6D.1 (unchanged this phase): `components[]` = `{sequenceOrder, componentType, intensityDescriptor}`, `additionalProperties:false`. This design does **not** replace it (Option C) or create an unrelated parallel structure (Option D) — Option D is explicitly forbidden by this phase's own text ("two competing prescription authorities would be dangerous"), and the evidence gives no reason to choose it. Instead: `PrescriptionProfile.Components[i]` **must correspond 1:1** to `WorkoutDefinition.components[j]` by matching `(sequenceOrder, componentType)` — a validated cross-reference invariant, not a coincidence of naming. `WorkoutDefinition.components[]` remains the sole authority for **structure identity** (how many components, what type, what order — display/documentation-grade, matches its current real use); `PrescriptionProfile.Components[]` becomes the sole authority for **dose** (quantity, structure mode, recovery, intensity) for that same structural skeleton. `intensityDescriptor` on `WorkoutDefinition` is retained as a free-text human-readable label (e.g. for UI), explicitly documented as **non-authoritative for materialization** once a `PrescriptionProfile` exists — this is the one, single, explicit sentence needed to prevent two competing authorities.

Mapping examples:

| Case | `WorkoutDefinition.components[]` (unchanged, identity) | `PrescriptionProfile.Components[]` (dose) |
|---|---|---|
| THRESHOLD continuous | `[WARM_UP, MAIN_SET, COOL_DOWN]` | WARM_UP: CONTINUOUS/600s easy · MAIN_SET: CONTINUOUS/1200s threshold-pace · COOL_DOWN: CONTINUOUS/600s easy |
| THRESHOLD cruise intervals | **same** `[WARM_UP, MAIN_SET, COOL_DOWN]` — component skeleton unchanged, only the MAIN_SET's dose differs | WARM_UP: unchanged · MAIN_SET: **REPEATED**, RepetitionCount=4, per-rep DistanceMeters=1600, RecoveryQuantity(400m, JOG) · COOL_DOWN: unchanged — **this is the concrete case where the SAME `WorkoutDefinition` serves two profiles**, proving the ownership split has real value, not just theoretical cleanliness |
| FARTLEK (structured) | `[WARM_UP, MAIN_SET, COOL_DOWN]` | MAIN_SET: REPEATED, RepetitionCount=6, per-rep DurationSeconds=60 (surge), RecoveryQuantity(60s, JOG — "float") |
| GOAL_PACE_TEN_K | `[WARM_UP, MAIN_SET, COOL_DOWN]` | MAIN_SET: CONTINUOUS, DistanceMeters=<goal-pace segment>, PaceDescriptorKey=goal-10k-pace |
| TAPER secondary sharpening | same `WorkoutDefinition` identity as its Build-phase counterpart | a **distinct** `PrescriptionProfile` version, reduced `WorkQuantity`, referenced only by the TAPER stage |

Note: if a real future case needs a **different** component count/order (e.g. explicit standalone `RECOVERY` rows inserted between reps rather than embedded `RecoveryQuantity`), that is a genuine `WorkoutDefinition` shape change, not a profile change — see A5 example 2.

### A5. Versioning boundary — 7 examples

| # | Change | Required version bump |
|---|---|---|
| 1 | Same Threshold identity, 4×1600 → 5×1600 | `PrescriptionProfile` only |
| 2a | Threshold continuous → cruise intervals, **same** component skeleton (embedded `RecoveryQuantity`, per A4's mapping) | `PrescriptionProfile` only |
| 2b | Threshold continuous → cruise intervals, if instead modeled with standalone `RECOVERY` components inserted between reps (different component **count**) | `WorkoutDefinition` (component shape changed), then a new `PrescriptionProfile` referencing it |
| 3 | Recovery mode changed (jog → walk) | `PrescriptionProfile` only |
| 4 | Physiological purpose reclassified (e.g. `eligiblePhases`/`family` changes) | `WorkoutDefinition` |
| 5 | Intensity source changed (pace-based → HR-based), new mode already inside `allowedPrescriptionModes` | `PrescriptionProfile` only |
| 5b | Same, but new mode is **not** in `allowedPrescriptionModes` | `WorkoutDefinition` first (widen the allow-list), then `PrescriptionProfile` |
| 6 | Taper-only lower dose | new `PrescriptionProfile` (version or key), zero `WorkoutDefinition` change |
| 7 | Correcting malformed prescription data (e.g. a duration typo) | new `PrescriptionProfile` version — **never** an in-place edit of a published version; plans already bound to the erroneous version keep it (published-artifact immutability, already a standing invariant in this engagement) |

### A6. Identity — single exact-version authority

`PrescriptionProfile` has `ProfileKey` + `ProfileVersion`, referenced by explicit exact pin — never latest/highest-version lookup, matching the pattern already proven safe today: `CatalogWorkoutBinder.ResolveDefinitionAsync` (`CatalogWorkoutBinder.cs:53-80`) resolves `WorkoutCandidateReferences` by exact `(Key, Version)` and throws `CatalogWorkoutBindingVersionMismatchException` if the loaded document doesn't match exactly (`CatalogWorkoutBinder.cs:71-76`). The single owner of the exact profile reference is the progression **stage** (a new `PrescriptionProfileCandidateKeys[]` field on `CatalogWorkoutProgressionStage`, sibling to today's `WorkoutCandidateReferences`, same exact-pin discipline, same "declares >1 → ambiguous" rule reused verbatim).

---

## SECTION B — DUAL-KEY LANE / PROGRESSION CONTRACT

### B1. Exact lane type

```csharp
public sealed class CatalogWorkoutProgressionLane
{
    public required string LaneKey { get; init; }         // catalog-authored, stable, e.g. "KEY_LANE_A" — never renumbered once published
    public required int LaneOrdinal { get; init; }         // catalog-authored, 0-based, deterministic binding key (see B7/C2)
    public required string StructuralRole { get; init; }   // "KEY_SESSION" today; generic — any future multi-slot role reuses this
    public required IReadOnlyList<CatalogWorkoutProgressionStage> Stages { get; init; } // EXISTING type, reused verbatim (see B2)
}

public sealed class CatalogPhaseWorkoutProgression
{
    public required string PhaseKey { get; init; }
    public required IReadOnlyList<CatalogWorkoutProgressionLane> Lanes { get; init; }  // single-lane today = Lanes.Count == 1, LaneOrdinal 0
}
```

`LaneOrdinal` is **explicit and catalog-authored, not derived from calendar/date order** — this is a deliberate, load-bearing choice. KEY1/KEY2 must mean the same physiological purpose every week regardless of which day of the week they land on (Monday vs. Tuesday can shift under repair); deriving lane identity from date order would make "which lane is this" a materialization-time accident. Stability across weeks comes from `LaneOrdinal` being a fixed authored fact per lane, unrelated to any per-week computation.

This is a **distinct concept** from the *structural binding ordinal* (§B7/C2), which **is** derived at materialization time from physical slot order — the two are related by a binding rule (structural ordinal N binds to `LaneOrdinal` N), not by being the same value computed once.

### B2. Per-lane semantics of existing stage fields — proven, not inferred

Traced `ProgressionStageAllocator.cs` (557 lines, read in full) directly:

- `Allocate` (lines 59-104) groups skeleton weeks by phase key and calls a private `AllocatePhase(phaseProgression, phaseWeeks, context, weeks, traceSteps)` **once per phase**. Nothing in this signature or body ties it to "the phase's only stage list" — it consumes exactly one `(stages, phaseWeeks)` pair and produces allocated weeks for that pair alone.
- **Answer to B2's forced-choice**: (C) — each lane is its own independent phase-scoped allocation problem. The design change is: invoke the existing, **unmodified** `AllocatePhase` once per lane, each call receiving the lane's own `Stages` and the **same** `phaseWeeks` (the full, shared week list for that phase — not divided or split between lanes). This is provable directly from the method boundary: `AllocatePhase` has no cross-lane state, no shared mutable capacity budget across calls, and no coupling to "how many total exposures exist across all of a phase's stages" beyond its own `activeStages.Sum(...)` computed fresh inside each call.
- `MinimumExposures`/`MaximumExposures`/`CompressionBehavior`/`ExtensionBehavior`/`Requires`/`FallbackStageKey` therefore generalize **exactly as claimed by the parent phase, and this pass confirms it structurally** — each field's meaning is unchanged; only the number of times the whole algorithm runs per phase changes (1 → N).
- **Required, disclosed, additive output change** (not free): `ScheduledProgressionWeek` currently is uniquely keyed by `WeekNumber` alone. For N lanes, the *set* of weeks produced by allocator invocation must carry `LaneKey`/`LaneOrdinal` tags, and downstream uniqueness becomes `(WeekNumber, LaneOrdinal)`. This is a small, mechanical, additive field addition to `ScheduledProgressionWeek` — real, but scoped and generic (default `LaneOrdinal = 0` preserves today's single-lane shape exactly).

### B3. MinimumExposures/MaximumExposures — illustrative worked example (symbolic stage keys, not real catalog authority)

Two lanes, one phase, illustrative values only:

- Lane A: `A1(min2,max3,Compressible)`, `A2(min2,max2,Protected)` → `totalMinimum = 4`
- Lane B: `B1(min3,max4,Compressible)`, `B2(min1,max2,Compressible)` → `totalMinimum = 4`

Each lane is allocated **independently against the same `availableWeeks` for the phase** (per B2):

| `availableWeeks` (Core horizon → phase length) | Lane A result | Lane B result |
|---|---|---|
| 8wk Core → phase gets 4 weeks (illustrative) | `totalMinimum(4) == availableWeeks(4)`: A1=2, A2=2, no compression/extension | `totalMinimum(4) == availableWeeks(4)`: B1=3, B2=1, no compression/extension |
| 9wk Core → phase gets 3 weeks | Deficit=1; only A1 compressible, headroom=`2-1=1`≥1 → A1=1, A2=2 | Deficit=1; B1 & B2 both compressible, headroom=`(3-1)+(1-1)=2`≥1; tie-break = highest RelativeOrder first → B2 has no headroom (already at floor 1), so B1 reduced: B1=2, B2=1 |
| 10wk Core → phase gets 5 weeks | Surplus=1; A1 Extendable (assume), A2 FixedExposure(Protected doesn't block extension — Protected only blocks compression); Extendable tried first → A1=3 (its max), A2=2 | Surplus=1; both Extendable → highest RelativeOrder first → B2 grows to max(2): B1=3, B2=2 |
| 12wk Core → phase gets 4 weeks | back to base case, A1=2,A2=2 | B1=3,B2=1 |
| 14wk Core → phase gets 6 weeks | Surplus=2; A1→3(max, +1), remaining surplus 1 has no more Extendable/FixedExposure headroom on A2 (max=2=min) → **`ProgressionPhaseCapacityExceedsMaximumException`** unless a third stage or higher max exists — illustrates the real, existing failure mode already present today, now surfaced per-lane instead of per-phase | B1→4(max,+1), B2→2(max,+1): exactly absorbs surplus=2 |

Each lane independently satisfies (or independently **fails**, per the last row) its own stage contract with **no cross-lane coupling** — no combined exposure count is ever computed; "impossible combined counts" cannot arise because each lane's capacity math is scoped to its own stage list against the shared week budget, never against the other lane's totals. The 14wk row deliberately shows a **real failure mode**: authors must size each lane's `Maximum Exposures` headroom for the longest horizon independently — this is a genuine authoring constraint disclosed here, not swept under a "generalizes cleanly" claim.

### B4. Compression — worked example

Using the 9wk row above: Lane A's deficit of 1 is resolved entirely inside Lane A's own `ApplyCompression` call (`ProgressionStageAllocator.cs:416-471`), independently of Lane B's own compression call. Direct answers:
- **Both lanes compressed independently** — yes, confirmed: each is a separate invocation of the same algorithm, no shared state.
- **Is one lane "primary" when exposures must be dropped?** — No such concept exists in the algorithm; each lane's compression candidates are chosen from *that lane's own* `Compressible`-tagged stages only, ordered by `(highest RelativeOrder, then ProgressionStageKey ordinal)` — entirely local to the lane.
- **Can a stage disappear from Lane B while remaining in Lane A?** — A stage's exposure count can shrink to **1** (the algorithm's floor: `reducible = candidate.MinimumExposures - 1`) but **never to 0** under the existing algorithm — full disappearance of a stage is not a capability this algorithm has today, in any lane. If a future need requires a stage to vanish entirely under compression, that is new behavior, out of scope here, flagged in §3.
- **Who decides?** — Purely mechanical (`totalMinimum` vs `availableWeeks`), no runtime human/product decision; the product decision is only in what `MinimumExposures`/`Compressible` values catalog authors assign per lane's stages.
- **Can compression accidentally produce duplicate KEY identities?** — Not from the compression algorithm itself (each lane produces its own, independently-computed `ProgressionStageKey` sequence). The real risk is **authoring-time**: if two lanes are ever authored with the *same* `ProgressionStageKey` string for conceptually different stages, that collides at the persistence-disambiguation layer (§C5). This is why the design states an explicit new invariant: **stage keys must be unique across lanes within one progression artifact** — enforced by the coordination validator (§B6, `CrossLaneStageKeyCollisionException`), not merely hoped for.

### B5. Extension — worked example

Using the 10wk row above: Lane A's surplus of 1 is resolved inside Lane A's own `ApplyExtension` call (`ProgressionStageAllocator.cs:493-544`), Lane B's independently. Sort key: `(Extendable before FixedExposure, then highest RelativeOrder, then ProgressionStageKey ordinal)` — identical, deterministic, per-lane. The design does **not** introduce alternation, cycling, or cross-lane coordination of extension — each lane extends by the exact same greedy rule already in production, applied to its own candidate list; determinism is proven by the sort key being a total order over each lane's own stage set (no ties possible given the ordinal tiebreak), exactly as it is today for the single-lane case.

### B6. Coordination validator — exact contract

**Input**: the flattened set of per-lane `ScheduledProgressionWeek` rows (post-allocation, pre-binding) for one phase (or a whole-plan pass across phases), plus the RunLayout's declared same-structural-role slot count per week (already available from the dated skeleton — not a new input).

**Output**: `Valid`, or a typed list of failures. Exact typed errors (not generic "invalid progression"):

| Error | Condition |
|---|---|
| `LaneCountMismatchException` | Resolved lane count for a week ≠ RunLayout's declared same-role slot count for that week |
| `DuplicateLaneOrdinalException` | Two lanes in one phase's `Lanes[]` declare the same `LaneOrdinal` |
| `DuplicateLaneKeyException` | Two lanes declare the same `LaneKey` |
| `LaneStarvedException` | A lane produced zero resolved weeks in a phase where the RunLayout expects ≥1 slot of its role (defense-in-depth; the allocator should never actually produce this given B2's proof, but validated rather than assumed) |
| `CrossLaneStageKeyCollisionException` | Two different lanes' stages share an identical `ProgressionStageKey` |
| `LaneFallbackIncompatibleException` | A stage's `FallbackStageKey` resolves to a stage belonging to a **different** lane than its own |

This validator is a pure post-check over already-independently-computed lane outputs — it never allocates anything itself, mirroring the existing separation this codebase already uses between allocation (`ProgressionStageAllocator`) and validation (e.g. `DatedGeneratedCatalogPlanSkeletonValidator`, `BoundCatalogPlanValidator`).

### B7. Lane-local fallback — decision tree

```
For (lane, phase, week):
  1. Try the lane's currently-assigned stage's PrescriptionProfileCandidateKeys, filtered by Requires[].
  2. If none resolve AND stage.FallbackStageKey is set:
       retry step 1 using that fallback stage — MUST be a stage within the SAME lane
       (enforced by CrossLaneStageKeyCollisionException / LaneFallbackIncompatibleException at publish time).
  3. If still none resolve: this lane fails for this week.
  4. After all lanes attempt 1-3 independently:
       if ANY lane failed -> the candidate-week is infeasible -> typed PRODUCT_INELIGIBLE
       (mirroring the existing typed-exception shape already used for 3D/Beginner×3D taper-floor cases).
```

No fallback ever crosses lanes, duplicates another lane's profile, deletes a lane, substitutes EASY, or coerces Level/Frequency/nearest-identity — matches FREQ.6A §14 exactly, and is now backed by a concrete publish-time enforcement mechanism (§B6) rather than a stated intention alone.

### Materialization pseudocode (full, per B7/E)

```
function MaterializeLaneStageSchedule(progression, datedSkeleton):
    weeksOut = []
    for phaseProgression in progression.PhaseProgressions (ordered by PhaseKey):
        phaseWeeks = datedSkeleton.WeeksInPhase(phaseProgression.PhaseKey)
        for lane in phaseProgression.Lanes (ordered by LaneOrdinal):
            laneWeeks = ProgressionStageAllocator.AllocatePhase(lane.Stages, phaseWeeks, context)  // EXISTING algorithm, unmodified, invoked once per lane
            for w in laneWeeks:
                weeksOut.add(w with { LaneKey = lane.LaneKey, LaneOrdinal = lane.LaneOrdinal, StructuralRole = lane.StructuralRole })
    CoordinationValidator.Validate(weeksOut, datedSkeleton.RunLayoutSlotCountsByWeek)   // §B6, throws typed exceptions
    return weeksOut

function BindWeek(datedWeek, laneStageSchedule):
    roleSlots = datedWeek.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.SlotOrderInWeek)
    for (slot, structuralOrdinal) in roleSlots.WithIndex():          // structuralOrdinal: 0,1,2... derived at bind time
        stageWeek = laneStageSchedule.Lookup(datedWeek.WeekNumber, laneOrdinal: structuralOrdinal)   // binding rule: structuralOrdinal == LaneOrdinal
        // ... existing single-candidate resolution logic (CatalogWorkoutBinder.cs:128-182), UNCHANGED, applied per (week, lane) pair instead of per week ...
```

**Where lane identity is introduced**: at `MaterializeLaneStageSchedule`, by tagging each independent allocator invocation's output — purely additive. **Where eligibility is resolved**: inside each independent `AllocatePhase` call, exactly as today, unchanged code (`ResolveEligibility`, `ProgressionStageAllocator.cs:319-377`). **Where fallback is resolved**: same — inside the existing per-invocation algorithm; cross-lane fallback is blocked at the *catalog-authoring/validation* layer (§B6), not by new runtime branching. **Where stage exposure accounting is stored**: exactly as today (`FinalAllocatedExposures`, trace steps), one independent full set per lane. **Where deterministic ordering is established**: (a) `LaneOrdinal` — explicit, authored, stable; (b) `SlotOrderInWeek` — already exists on the dated skeleton, unchanged; (c) the binder's zero-based rank over role-filtered, `SlotOrderInWeek`-ordered slots — new, small, pure function, equated to `LaneOrdinal` by the binding rule.

### B8. No full-plan duplication — proven

The phase-relative, compression/extension-driven allocator (`ProgressionStageAllocator.AllocatePhase`) is reused **verbatim** per lane; only the *invocation count* changes (1→N per phase), never the mechanism. `phaseWeeks.Count` — the only per-horizon-varying input today — remains the only per-horizon-varying input under this design; nothing introduces an 8w/9w/…/14w branch anywhere. One versioned `CatalogWorkoutProgressionDefinition` artifact continues to serve every horizon.

---

## SECTION C — ADAPTATION V1 COMPOSITION PROOF

### C1. Full runtime trace, boundary by boundary

| Boundary | Does lane/profile identity matter here? |
|---|---|
| Scheduled structural KEY slots (dated skeleton, `SlotOrderInWeek`) | No — purely RunLayout-structural, predates any lane resolution, unchanged by this design |
| → structural binding ordinal (new, binder-local) | **Yes** — this is exactly where lane identity is introduced (§B7) |
| → prescription/workout binding (`CatalogWorkoutBinder`) | Yes — this is where `PrescriptionProfile`/`WorkoutDefinition` are resolved per lane |
| → persisted `TrainingDay` | Yes for row *content* correctness, but **not via a new lane column** — reconstructible via `CatalogProgressionStageKey` (see §C5) |
| → completion (`TrainingDay.Status` / `LongHorizonRollingSessionState`) | No — purely completion status, unchanged pipeline |
| → `WindowExecutionSummary` | **No — confirmed by direct code read.** `LogicalSessionEvidence` (`AdaptationDomainContracts.cs:154-160`) carries only `Id, Role, ExecutionOutcome, PlanningStatus, AdaptedFromId, NotTodayReason` — no workout/stage/lane field exists, and `WindowExecutionSummaryBuilder.Build` (lines 96-153) reads only `Role` + terminal outcome to increment `keyExpectedCount`/`keyCompletedCount`. Every `KeySession`-role root is fungible to this code. |
| → 5D weekly severity policy (`NextWindowLoadDecisionPolicy`) | No lane read — **but a real, disclosed gap exists here, not introduced by this design**: `DetermineLoadDecision` (`NextWindowLoadDecisionPolicy.cs:49-58`) is a raw switch over `EffectiveCompletedCount` hardcoded to a 4-session week (`0 or 1→Reduce, 2→Maintain, 3→conditional, ≥4→Progress`). A 5-session (dual-KEY) week needs this table widened to a 6-outcome (0–5) form consistent with FREQ.6's already-frozen Model A5 24-row severity table (FREQ.6 §5, binding). This gap was already flagged in-repo (FREQ.4's own doc comment on this method) as deliberately unresolved; **this is the phase where it must actually be resolved**, not deferred again, or 5D cannot ship a working severity outcome. |
| → next-window decision | Same as above — downstream of the same counts, same required table widening |

### C2. Composition with FREQ.4's `keyOrdinal` — **correction to the parent phase's claim**

The parent design document (FREQ.6D.1 §C1) claimed the binder reuses FREQ.4's real `keyOrdinal` "exactly, unchanged, as the lane-selection key." **This evidence pass finds that claim is factually wrong as stated**, and corrects it:

- `CatalogSessionPrescriptionPlanner.cs:31-39` computes `keyOrdinal` as a **local variable inside the prescription-planning loop**, which runs on `CatalogSessionPrescriptionRequest.BoundPlan` — i.e. it consumes the **already-bound** plan produced by `CatalogWorkoutBinder.BindAsync`. **Binding happens first; the prescription planner's `keyOrdinal` is computed strictly afterward.** The binder therefore cannot "reuse" an ordinal that does not exist yet at the time it runs.
- Additionally, the two would-be ordinals are **not guaranteed to agree**: the prescription planner sorts `boundWeek.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole)` (date-then-role), while the new structural binding ordinal proposed in §B7 sorts by `SlotOrderInWeek` (a dated-skeleton-level field). Nothing today guarantees these two orderings coincide (e.g. if slot order is ever non-date-monotonic).

**Corrected design**: introduce the structural binding ordinal (§B7) as a genuinely new, small, disclosed field on the *binder's* output — add `KeySessionLaneOrdinal` (nullable int) to `BoundCatalogSession` (in-memory internal contract, `BoundCatalogPlanContracts.cs`), computed once by the binder from `SlotOrderInWeek` at bind time. `CatalogSessionPrescriptionPlanner` is then changed to **read this field instead of recomputing its own ordinal** — eliminating both the ordering-mismatch risk above and the duplicate computation, and making the binder the single source of truth for lane/ordinal identity end-to-end.

Full answers:
- **Who assigns keyOrdinal (FREQ.4's original, distance-allocation-indexing one)?** Unchanged owner (`CatalogSessionPrescriptionPlanner`), but it now reads `session.KeySessionLaneOrdinal` off the bound session rather than computing its own — a small, disclosed, real change, not "zero."
- **Is the (new) structural ordinal stable after calendar materialization?** Yes, by construction — a pure function of `SlotOrderInWeek`, itself deterministic per dated-skeleton materialization (unchanged upstream step).
- **Is it stable after repair?** Not via any *live* mechanism — see below; it is not recomputed by repair at all.
- **Is it persisted?** Not as a new column directly (kept `DERIVED_RUNTIME_ONLY`, §C5) — but effectively durable via the already-persisted, lane-unique `CatalogProgressionStageKey`.
- **Does substitution/down-dosing preserve it?** See C4.
- **Can it change after workout fallback?** No — fallback resolves once, at materialization time, before binding; `ScheduledProgressionWeek.ProgressionStageKey` is explicitly documented as the effective, **post-fallback** key (`RequestedProgressionStageKey` is the separate pre-fallback field) — matches today's existing single-lane behavior exactly, unchanged.
- **Does Adaptation read ordinal directly?** No — confirmed (§C1).
- **How do two KEY completions still produce correct 5D severity given Adaptation never reads ordinal?** Because `WindowExecutionSummaryBuilder` counts all `KeySession`-role roots as fungible by construction (confirmed code read) — severity depends only on *how many* of the week's KEY roots completed, matching FREQ.6's frozen KEY1/KEY2-severity-symmetric decision **by construction**, not by coincidence.

### C3. 5D severity symmetry — proven

Since `WindowExecutionSummaryBuilder.Build` increments `keyExpectedCount`/`keyCompletedCount` purely by `Role == KeySession` + terminal outcome (no lane read), the scenario "ordinal 0 completed, ordinal 1 missed" and "ordinal 0 missed, ordinal 1 completed" produce an **identical** `WindowExecutionSummary` object (`KeySessionExpectedCount=2, KeySessionCompletedCount=1` in both cases) — hence identical classification under `NextWindowLoadDecisionPolicy` (once C1's table-widening is applied). This is structural, not incidental: `doseCategory` (PRIMARY/SECONDARY_CONTROLLED) does not exist anywhere in the Adaptation data flow (`LogicalSessionEvidence` has no such field), so it cannot leak into severity — no contradiction with FREQ.6's frozen decision.

### C4. Repair / substitute / down-dose — three scenarios

Traced `ScheduleRepairRuntimeOrchestrator.cs` (163 lines, read in full) directly. It works purely off `LongHorizonRollingSessionState.SessionRole` → `PreparationRunwaySlotRole` (role only) and `AdaptationPhaseIdentity`; grepped the whole `Adaptation/` directory for `WorkoutDefinitionKey`/`ProgressionStageKey`/`CatalogWorkoutKey` — **zero matches anywhere in that directory**, including the orchestrator, `ScheduleRepairPersistenceService.cs`, and `ScheduleRepairCandidateProvider.cs`. Repair is entirely workout/stage-identity-blind today.

1. **KEY2 calendar-repaired to another day (`RescheduleToEmptySlot`)**: this moves the *same* already-persisted `TrainingDay` row. Its existing `CatalogProgressionStageKey`/`CatalogWorkoutDefinitionKey` travel unchanged with the row. Structural lineage, completion accounting (via unchanged `AdaptedFromId` chain), and lane semantics (unchanged persisted key) are all correct. **No new Adaptation code required.**
2. **KEY2 workout identity substituted through approved fallback**: per §B7, fallback resolution happens *before* binding/persistence — by the time a session is persisted its `ProgressionStageKey` is already the resolved, post-fallback effective key. If instead this means the orchestrator's live `SubstituteFutureEasy` repair action (turning a future EASY_SUPPORT day into a stand-in for a missed KEY session): **the evidence pass could not confirm whether that substituted day is re-bound through `CatalogWorkoutBinder` to acquire a lane-appropriate `ProgressionStageKey`, or left without one** — this file was not in the scope read (`ScheduleRepairPersistenceService.cs` was found but not read in full). **This is a genuine, disclosed open item**, carried to §3 — not assumed either way.
3. **KEY2 prescription down-dosed after Adaptation** (a `Reduce` decision lowering dose for the *next* window): operates at prescription-profile-selection time for **future** weeks only — never mutates an already-persisted row. Structural lineage, completion accounting, and lane semantics for already-persisted weeks are untouched; the future week's lane assignment proceeds through the normal §B7 flow with a lower-dose profile selected via ordinary stage/profile authoring (not a special runtime down-dose path). **No new Adaptation code required** for the structural/lane mechanics; how "Reduce" maps to an actual profile selection is a FREQ.6/6C-adjacent product mechanism explicitly out of this phase's scope.

### C5. Persistence classification

| Field | Classification | Basis |
|---|---|---|
| `LaneKey` (catalog-authored) | `CATALOG_ONLY` | Never touches runtime session data |
| `LaneOrdinal` (catalog-authored) | `CATALOG_ONLY` | Same |
| Structural binding ordinal (bind-time slot rank) | `DERIVED_RUNTIME_ONLY` | Pure function of `SlotOrderInWeek`, deterministic given the same dated skeleton |
| `BoundCatalogSession.KeySessionLaneOrdinal` (new, §C2) | `DERIVED_RUNTIME_ONLY` | `BoundCatalogSession` itself is fully internal and non-persisted ("never exposed on any public DTO, never persisted, never hashed" — `BoundCatalogPlanContracts.cs` doc comment) |
| `PrescriptionProfile` reference (key+version) for a bound session | **`MUST_PERSIST`, new columns recommended** | `TrainingDay` **already** persists `CatalogWorkoutDefinitionKey`/`Version` directly (not reconstructed from a join) for exactly this reason — direct query/render convenience. For consistency with that established pattern, add `CatalogPrescriptionProfileKey`/`CatalogPrescriptionProfileVersion` (nullable, additive — same shape as the existing `Phase4F9_CatalogConfirmationPersistence` migration) rather than leaving this `DERIVED_RUNTIME_ONLY`. This is a real, small, disclosed migration, not zero. |
| Lane identity for an already-persisted row | `DERIVED_RUNTIME_ONLY` | Reconstructible from `CatalogProgressionStageKey` → lane lookup in the immutable, versioned progression artifact (given the stage-key-uniqueness-across-lanes invariant, §B4/B6), or directly from the new profile columns above once added — no new `LaneOrdinal` column needed on `TrainingDay` itself |

Reconstruction determinism proof: progression artifacts are versioned and immutable once published (standing engagement invariant); a `(CatalogProgressionStageKey, progression key+version)` pair therefore always resolves to exactly one lane, deterministically, forever.

### C6. Exact zero-change classification

Given: (a) `NextWindowLoadDecisionPolicy`'s severity switch requires a real, small, generic table-widening (rules out "zero change"); (b) `WindowExecutionSummaryBuilder`'s counting mechanism needs **no** change (confirmed lane-blind by construction); (c) two new nullable, additive `TrainingDay` columns are recommended (small, non-breaking, consistent with the existing persistence pattern); (d) `ScheduleRepairRuntimeOrchestrator`'s move/substitute mechanism needs no change for scenarios 1 and 3 of C4, with scenario 2 flagged open rather than resolved:

```
ADAPTATION_V1_SMALL_GENERIC_EXTENSION_REQUIRED
```

This **corrects** the parent phase's `ADAPTATION_V1_ZERO_CHANGE_PROVEN`-equivalent claim. The required extension is narrow and generic (a count-driven table widening + two additive persistence columns — not new business logic, not a new severity philosophy) and does **not** touch `WindowExecutionSummaryBuilder`'s core counting mechanism or introduce any lane-aware branching anywhere in Adaptation.

---

## SECTION D — BACKWARD COMPATIBILITY / VERSIONING / FAILURE MODEL

### D1. Legacy 3D/4D

`CatalogPhaseWorkoutProgression.Stages` today is a required, non-nullable list (`CatalogWorkoutProgressionDefinition.cs`). Introducing `Lanes[]` as an outright **replacement** field would be a breaking shape change; the design instead makes `Lanes[]` an **additive, coexisting** representation, with loader-level normalization: *if `Lanes[]` is present, use it; else wrap the existing `Stages[]` as a single implicit lane (`LaneOrdinal=0`)*. Old published 3D/4D bundles remain byte-for-byte unchanged, loadable without modification, and behaviorally identical — their sole implicit lane's `LaneOrdinal=0` matches the existing single `KEY_SESSION` slot per week (only one slot exists structurally for 3D/4D, so the binding rule "structural ordinal N ↔ LaneOrdinal N" degenerates to `(0,0)` trivially). No forced republication.

### D2. Ambiguous-candidate elimination — concrete before/after

**Before (today, confirmed real)**: a 5D-shaped week with 2 `KEY_SESSION` slots — if a single stage were naively authored with 2 `WorkoutCandidateReferences` to "cover both slots," `CatalogWorkoutBinder.cs:145-150` throws `CatalogWorkoutBindingAmbiguousCandidateException` immediately, deterministically, every time.

**After**: the two slots are never fed into one stage's candidate list. Each lane owns its own stage with its own single-candidate resolution — the *same* existing >1-candidate-throws rule still applies, now scoped **per lane** rather than per week. The two slots resolve via two separate lookups keyed by `(WeekNumber, LaneOrdinal)` (§B7), each independently satisfying the existing "exactly one candidate" invariant. The fix is structural (giving each slot its own resolution path), not a catch/suppress of the existing exception — which remains fully intact and still correctly fires if a single lane's stage is ever misauthored with >1 candidate.

### D3. Five malformed catalog examples

| # | Malformed input | Expected rejection layer |
|---|---|---|
| 1 | Duplicate `LaneKey` within one phase's `Lanes[]` | **Publish-time** catalog validator (new) |
| 2 | Two lanes both declare `LaneOrdinal=0` | **Publish-time** catalog validator (new) |
| 3 | Lane B's stages have `MinimumExposures` sum exceeding available capacity even with full compression headroom | **Runtime today** (`ProgressionPhaseCapacityInsufficientException`, confirmed real, `ProgressionStageAllocator.cs`) for the single-lane case too — this design does not regress that; **recommended improvement** (not required for 6D.2/6D.3): promote this class of check to a publish-time static validator for typical horizon ranges, mirroring `DatedGeneratedCatalogPlanSkeletonValidator`'s existing role as a separate publish-adjacent layer |
| 4 | A stage's `FallbackStageKey` resolves to a stage belonging to a different lane | **Publish-time**, new `LaneFallbackIncompatibleException`/`CrossLaneStageKeyCollisionException` (§B6) — must exist before 6D.4 |
| 5 | A referenced `PrescriptionProfile`'s `DistanceAccountingMode`/intensity mode is not in its `WorkoutDefinitionRef`'s allowed sets | **Publish-time**, a new validator generalizing the existing bind-time `ValidateInClosureAndPhase` check (`CatalogWorkoutBinder.cs:82-103`) earlier, for fail-fast authoring feedback — while **keeping** the existing runtime check too, as defense-in-depth, matching this codebase's established layered-validation pattern (`BoundCatalogPlanValidator` running after `CatalogWorkoutBinder`) |

Publish-time failure is preferred over runtime wherever the check can be made static (examples 1, 2, 4, 5); example 3 remains runtime today and is not regressed, only flagged as an improvement opportunity.

---

## SECTION E — IMPLEMENTATION DECOMPOSITION

### E1. Dependency graph — real, not artificially serialized

```
   6D.2 ──┐
          ├──▶ 6D.4 ──▶ 6D.5
   6D.3 ──┘
```

**6D.2 and 6D.3 have no hard dependency on each other** and can be built/tested in parallel: 6D.3's lane/allocator generalization only needs the *reference type shape* (`PrescriptionProfileCandidateKeys[]` as an exact-pin list, same shape as today's `WorkoutCandidateReferences`) — it does not need to know what a `PrescriptionProfile`'s *internal* fields (§A) contain. Only 6D.4 (binder/prescription integration) genuinely needs both to exist. This deviates from the prompt's suggested "6D.3 = lane allocation, 6D.4 = dual-KEY progression integration" split — the real dependency boundary instead falls at "does this touch `CatalogWorkoutBinder`/runtime binding," which cleanly separates schema+allocator (6D.2+6D.3, no binder touch) from binder/prescription/Adaptation/persistence integration (6D.4).

### E2. FREQ.6D.2 — PrescriptionProfile domain contract

- **Objective**: Section A only — schema, typed model, deserialization, source (publish-time) validation.
- **Files/types**: new `PrescriptionProfile`/`PrescriptionComponent`/`PrescriptionWorkQuantity`/`PrescriptionRecoveryQuantity`/`PrescriptionIntensityTarget` types + JSON schema; the A4 cross-reference validator against `WorkoutDefinition.components[]`.
- **Must already be frozen**: FREQ.6/6A/6C (all, unchanged); nothing from 6D.3/6D.4.
- **Implements**: profile shape, unit-discrimination invariants, WorkoutDefinition/Profile ownership split, versioning-boundary validation.
- **Does NOT implement**: binder wiring, lane concept, Adaptation changes, persistence columns.
- **Independent test suite**: golden-output round-trip against hand-authored profiles for **existing** single-lane identities (EASY_STANDARD, LONG_RUN_STANDARD, existing THRESHOLD/FARTLEK definitions) — proves the schema represents real current data without needing any lane concept.
- **Exit criteria**: schema validates real existing workout identities; A4's 1:1 component cross-reference check passes for hand-authored fixtures; A5 versioning-boundary rules enforced by a rule-set test (the 7 examples in §A5 encoded as test cases).
- **Rollback boundary**: purely additive new files; zero risk to any running path.
- **Next-phase dependency**: 6D.4 needs the reference type shape only.
- **Recommendation (explicit, per prompt's E2 ask)**: keep 6D.2 narrow exactly as scoped above — no binder/runtime behavior — so the new catalog contract is validated in isolation before any runtime consumer changes.

### E3. FREQ.6D.3 — Lane-capable progression schema + allocator + coordination validator

- **Objective**: Section B (minus binder integration) — `CatalogWorkoutProgressionLane` type, `Lanes[]` replacing bare `Stages[]` with D1's backward-compat normalization, N-times-per-phase `AllocatePhase` invocation, `CoordinationValidator` (§B6).
- **Files/types**: `CatalogWorkoutProgressionDefinition.cs` (add `Lanes[]`), `ProgressionStageAllocator.cs` (loop once per lane), new `ProgressionLaneCoordinationValidator.cs`, `ProgressionStageScheduleContracts.cs` (add `LaneKey`/`LaneOrdinal` to `ScheduledProgressionWeek`).
- **Must already be frozen**: 6D.2's reference-type shape only (not its internal field contents).
- **Implements**: B1–B8 in full except the binder-facing lookup change.
- **Does NOT implement**: `CatalogWorkoutBinder` changes, `BoundCatalogSession.KeySessionLaneOrdinal`, Adaptation table widening, persistence columns.
- **Independent test suite**: synthetic N-lane allocator runs (mirroring FREQ.4's synthetic-test pattern) exercising the §B3/B4/B5 worked examples plus the coordination validator's six typed error conditions — entirely without touching `CatalogWorkoutBinder`.
- **Exit criteria**: compression/extension/coordination-validator behavior verified per-lane in isolation; D1's normalization proven against real existing single-lane progression artifacts (loaded unchanged, `Lanes.Count==1` inferred correctly).
- **Rollback boundary**: additive to allocator/contracts; existing single-lane callers unaffected by construction (D1).
- **Next-phase dependency**: 6D.4 needs `ScheduledProgressionWeek.LaneOrdinal` to exist.

### E4. FREQ.6D.4 — Binder / prescription / Adaptation / persistence integration

- **Objective**: the highest-regression-risk phase — structural binding ordinal computation, `BoundCatalogSession.KeySessionLaneOrdinal`, binder's `(WeekNumber, LaneOrdinal)`-keyed lookup (replacing the `WeekNumber`-only dictionary), `CatalogSessionPrescriptionPlanner` reading the new field instead of recomputing its own ordinal (§C2 correction), `NextWindowLoadDecisionPolicy`'s severity-table widening (§C1/C6), and the two new `TrainingDay` columns + migration (§C5).
- **Files/types**: `CatalogWorkoutBinder.cs`, `BoundCatalogPlanContracts.cs`, `CatalogSessionPrescriptionPlanner.cs`, `NextWindowLoadDecisionPolicy.cs`, `TrainingDay.cs` + new EF migration, `CatalogPlanConfirmationService.BuildCatalogTrainingDay`.
- **Must already be frozen**: 6D.2 + 6D.3 in full, and the open item in §3.1 (repair/substitution lane-identity question) either resolved or explicitly deferred with a stated interim behavior.
- **Implements**: §C1–§C6 in full.
- **Does NOT implement**: any change to `ScheduleRepairRuntimeOrchestrator`'s move/substitute mechanism beyond what §3.1 resolves.
- **Independent test suite**: full existing regression baseline (3480/3480 per FREQ.4A) must stay green before any 5D-specific artifact exists; targeted new tests for the binder's dual-lookup, the widened severity table (all 6 `EffectiveCompletedCount` outcomes 0–5), and persistence round-trip of the two new columns.
- **Exit criteria**: zero regression against the existing baseline; a first illustrative (non-final-dose) 2-lane synthetic candidate binds/persists/round-trips correctly end-to-end.
- **Rollback boundary**: touches shared single-lane code paths — must be the most carefully reviewed of the four phases; D1's normalization must be re-verified against the full existing 3D/4D/Beginner×4D test suite at this exact point, not assumed carried over from 6D.3.
- **Next-phase dependency**: 6D.5 needs this fully working.

### E5. FREQ.6D.5 — Persistence/round-trip/regression closure

- **Objective**: final closure — must include 3D regression, 4D regression, Beginner×4D regression, 5D dual-KEY targeted tests, historical catalog compatibility (old bundles load unmodified), deterministic bundle/hash checks where relevant, full backend regression.
- **Must already be frozen**: 6D.2–6D.4 in full.
- **Implements**: closure documentation, full-suite verification, empirical (not just design-argument) proof of §D1's backward-compatibility claim.
- **Does NOT implement**: any new mechanism — this phase tests, it does not build.
- **Independent test suite**: the full existing regression suite plus every synthetic/targeted test added in 6D.2–6D.4, run together as one closure gate.
- **Exit criteria**: 100% pass, zero net-new failures, backward-compatibility empirically demonstrated (not merely designed).
- **Rollback boundary**: N/A — this phase is verification only.

Catalog authoring (actual FARTLEK/THRESHOLD/GOAL_PACE_TEN_K dose values, new Foundation/Taper identities) remains explicitly out of scope for all of 6D.2–6D.5, per this phase's own DO NOT list — it consumes this schema once built.

---

## 2. DESIGN INVARIANT TABLE — `FREQ6D1_DESIGN_INVARIANT_TABLE`

| # | Invariant | Enforced where |
|---|---|---|
| 1 | `PrescriptionProfile` quantities have explicit units, never ambiguous | Type-level discrimination (§A2), publish-time schema validation |
| 2 | No duplicate prescription authority | `WorkoutDefinition.components[]` = structure identity only, non-authoritative for materialization once a profile exists (§A4) |
| 3 | Workout identity ≠ exact dose | `WorkoutDefinition`/`PrescriptionProfile` ownership split (§A3) |
| 4 | Published artifacts immutable | Standing engagement invariant, reused unchanged; A5 example 7 |
| 5 | Exact version references only, never latest/highest lookup | §A6, mirrors `CatalogWorkoutBinder.ResolveDefinitionAsync`'s existing exact-pin behavior |
| 6 | Lane identity deterministic and stable across weeks | `LaneKey`/`LaneOrdinal` catalog-authored, not date-derived (§B1) |
| 7 | Structural binding ordinal deterministic per bind | Pure function of `SlotOrderInWeek` (§C2) |
| 8 | Two KEY lanes progress independently but coordinate deterministically | Per-lane `AllocatePhase` invocation + `CoordinationValidator` (§B2/B6) |
| 9 | Compression does not silently starve a required lane | `LaneStarvedException` (§B6), floor-at-1 behavior disclosed not hidden (§B4) |
| 10 | Extension deterministic | Total-order sort key per lane (§B5) |
| 11 | Fallback deterministic, never cross-lane | §B7, enforced by `LaneFallbackIncompatibleException` |
| 12 | No nearest-workout/duplication/deletion/coercion fallback | §B7, unchanged from FREQ.6A §14 |
| 13 | Primary/secondary dose category does not alter Adaptation severity | Proven structurally absent from `LogicalSessionEvidence` (§C3) |
| 14 | Adaptation role-count accounting preserved | `WindowExecutionSummaryBuilder` unchanged (§C1/C6) |
| 15 | 3D/4D/Beginner×4D behavior unchanged | D1's additive normalization, empirically re-verified in 6D.4/6D.5 |
| 16 | No full-plan/per-horizon duplication | §B8, proven by inspection |
| 17 | Stage keys unique across lanes within a progression | New, disclosed invariant (§B4), enforced by `CrossLaneStageKeyCollisionException` |

---

## 3. OPEN DECISION CHECK

Three real, unresolved items survive this evidence pass — none block `FREQ.6D.2`, all must close before `FREQ.6D.4`:

1. **Substitution lane-identity question (§C4, scenario 2)**: whether `ScheduleRepairRuntimeOrchestrator`'s `SubstituteFutureEasy` path re-binds a substituted day to acquire a lane-appropriate `ProgressionStageKey`, or leaves it structurally KEY-tagged without one. Not resolved by this evidence pass (`ScheduleRepairPersistenceService.cs` not read in full). Borders a product question (does a substituted KEY session retain its "was this the primary or secondary lane" purpose?) as well as a pure engineering one — recommend a short, targeted evidence read before 6D.4, escalating to an explicit product-adjacent decision only if the engineering read doesn't resolve it cleanly.
2. **Unstructured/self-selected fartlek** (§A1/A4): cannot be represented under this typed model without a fabricated rep count. Whether the catalog will ever need to author unstructured fartlek is a content/product question outside this phase's scope — not resolved here.
3. **Dose-category/lane alignment convention**: this design keeps `LaneOrdinal` (structural) and `DoseCategory` (prescription-priority) fully orthogonal, per FREQ.6A's own separation — a lane *can* reference a profile of either category depending on phase. Whether catalog-authoring convention should instead pin `LaneOrdinal 0 ↔ PRIMARY` is a genuine open authoring-convention question, not an engineering necessity — flagged, not decided.

None of these are `UNRESOLVED_DOMAIN_DECISION_FOUND` in the blocking sense (none require revisiting a frozen FREQ.6/6A/6C decision, and none block 6D.2's narrow schema scope) — they are scoped, enumerable engineering/authoring-convention gaps to close before 6D.4, consistent with this engagement's standing practice of disclosing rather than silently assuming.

---

## 4. COMPLETION STANDARD — checked against the eight required conditions

| # | Condition | Status |
|---|---|---|
| A | PrescriptionProfile exact type/ownership/version contract explicit | Met (§A) |
| B | Lane schema and per-lane stage semantics proven against real current consumers | Met (§B2, direct `ProgressionStageAllocator.cs` trace) |
| C | Coordination validator has exact input/output/failure contract | Met (§B6) |
| D | FREQ.4 keyOrdinal composition traced end-to-end | Met, and **corrected** — parent's "reuse unchanged" claim was wrong; real design introduces a new, disclosed, small field (§C2) |
| E | "Adaptation V1 zero change" proven, narrowed, or corrected | **Corrected** — `ADAPTATION_V1_SMALL_GENERIC_EXTENSION_REQUIRED` (§C6) |
| F | Persistence implications explicit | Met (§C5) |
| G | 6D.2–6D.5 have independent boundaries and testable exits | Met (§E) |
| H | No unresolved product/domain decision remains | Three open items remain (§3), none blocking 6D.2, all scoped before 6D.4 |

---

## 5. FINAL CLASSIFICATION

```
INTERMEDIATE_5D_PRESCRIPTION_ARCHITECTURE_DESIGN_VERIFIED_WITH_MINOR_ENGINEERING_GAPS
```

Not the unconditional "ready" classification: this evidence pass found and corrected one real design error in the parent phase (the `keyOrdinal`/binder-ordering contradiction, §C2) and one real overclaim (the Adaptation "zero change" assertion, now `SMALL_GENERIC_EXTENSION_REQUIRED`, §C6), and surfaced three genuine open items (§3) that were not visible in the parent's higher-level summary. None of these rise to a frozen-policy contradiction or a new product decision — they are exactly the kind of "unstated architectural decision discovered in the middle of schema/runtime work" this follow-up phase exists to catch before it happens during 6D.2–6D.5, not after.

**`FREQ.6D.2` is unblocked and may proceed now** — its narrow scope (PrescriptionProfile schema/type/validation only, §E2) depends on none of the three open items or either correction above. `FREQ.6D.3` may also proceed in parallel (§E1). `FREQ.6D.4` must not start until open item §3.1 is closed (or explicitly, consciously deferred with a stated interim behavior) and the Adaptation severity-table widening (§C1/C6) is treated as required scope, not optional polish.
