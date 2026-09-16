# PHASE DIST-GEN.0 — 10K–HALF-MARATHON INTERMEDIATE-DISTANCE ARCHITECTURE, PARENT-FAMILY, DECOMPOSITION AND PROJECTION AUTHORITY AUDIT

**Type:** GOVERNANCE / ARCHITECTURE / PRODUCT-DECISION (no production implementation, no catalog authoring, no new numeric training authority, no public activation)

---

## 1. Current canonical-distance baseline

Confirmed from current final-state authority (not reconstructed from old prompts): TEN_K and HALF_MARATHON are both mature canonical distance families. HM Standard Core supports 10-16 full weeks. Post-HM-X1, HM public frequencies are Beginner 3D/4D, Intermediate 3D/4D/5D/6D, Advanced 3D/4D/5D/6D (Beginner 5D/6D and Experienced at every frequency remain excluded/out of scope). `RunningBackground.Experienced` remains `PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED` (`PHASE_EXP_0`). None of these decisions are reopened here.

## 2. Current runtime distance model

Audited directly (both agents' findings independently corroborate each other):

- **`GoalDistance` enum** (`backend/RunningApp.Domain/Enums/GoalDistance.cs:3-10`): a fixed 5-value enum `{FiveK, TenK, HalfMarathon, Marathon, Custom}`. `Custom` exists but is vestigial — `GoalDistanceKm.Resolve` (`backend/RunningApp.Application/Common/GoalDistanceKm.cs:13-20`) has no arm for it and it falls through to `5.0`.
- **No wire DTO field carries an arbitrary decimal km value.** `GenerateRacePlanPreviewRequest.cs:26`, `GenerateHabitPlanPreviewRequest.cs:17`, `GeneratePreviewRequest.cs:47`, `RecentRaceInput.cs:14` are all `GoalDistance` enum-only — confirmed by full file reads. A `Custom`-labeled request would pass validation but carries no distance payload at all.
- **A second, more general distance model already exists in the codebase — as dead code.** `ICanonicalDistanceFamilyResolver.Resolve(double requestedDistanceKm)` (`CanonicalDistanceFamilyResolver.cs:14-75`) takes an arbitrary km value and bands it into a family (`≤5.0→FiveK, ≤10.0→TenK, ≤21.1→HalfMarathon, ≤42.2→Marathon`) while preserving the exact requested value in a `DistanceFamilyResolution.RequestedTargetDistanceKm` field. It is DI-registered (`Program.cs:155`) and has its own passing test suite, but has **zero production consumers** — confirmed by `CatalogNotWiredToGenerationTests.cs:17-21`'s own explicit regression-guard statement that plan generation does not consume it.
- **`TrainingPlan` entity already has the exact family/target-distance split** (`backend/RunningApp.Domain/Entities/TrainingPlan.cs`): `GoalDistance` (enum, line 12), `GoalDistanceKm` (fixed family-representative `double?`, line 13), `CanonicalDistanceFamily` (`string?`, line 131, doc-commented as "the catalog's own literal string... per the standing guardrail that catalog vocabulary must be preserved as catalog identity, not collapsed into backend enums"), and **`RequestedTargetDistanceKm`** (`double?`, line 141, doc-commented explicitly for "the user's exact requested target distance... e.g. 8.0 for an '8K' request... must never be overwritten by a family-representative distance"). This column already exists via a real migration (`20260710072851_AddPlanCatalogProvenanceFields.cs`) — **no schema change is needed** to store a genuine custom distance. It is populated today (`CatalogPlanConfirmationService.cs:621`) but only ever with the fixed family value, never a truly distinct one, since nothing upstream produces one yet.
- **Catalog combination/candidate identity is a small, closed, enumerated vocabulary, not a numeric range.** `V1CatalogPilotIdentityPolicy.cs` keys everything on literal strings like `"HALF_MARATHON__6D__ADVANCED"` and switches on the `GoalDistance` enum (`TenK`/`HalfMarathon` only; everything else, including `Custom`, falls to `false`/throws). Catalog JSON keys on `"distanceFamily": "TEN_K"` as a literal string. Structurally, this system cannot represent "16.0 km" — only named families.
- **`RaceHorizonPolicy`'s core engine is already distance-generic; only its outer dispatcher is a closed enum switch.** `Decide(startDate, raceDate, minWeeks, preferredWeeks, maxWeeks)` (lines 148-155) takes plain `int` bounds. `DecideForDistance(startDate, raceDate, GoalDistance distance)` (lines 126-135) is a two-arm switch: `HalfMarathon` gets its own 10/14/16 bounds, **everything else (including a hypothetical future distance, or `Custom`) silently defaults to TEN_K's 8/12/14** — a real, currently-live latent gap, not something DIST-GEN introduces.
- **Flutter frontend already has free-form distance-entry UI in two places — but both silently discard the typed value before it reaches the backend.** `race_details_page.dart` has a free-text distance field (line 21) with a ±0.2km snap-to-preset tolerance (lines 54-62); anything outside all four presets' tolerance windows resolves to the literal string `'custom'` (line 61) — **the actual number the user typed (e.g. "16") is never transmitted.** `custom_goal_page.dart` (habit flow) has an unbounded integer-km stepper (line 20) used only for local client-side pace math; the outgoing request again carries only the string `'custom'`. Confirmed by a full grep of `mobile/lib/core/network/dtos.dart`: no `target_distance_km`/`custom_distance` field is ever serialized into any outgoing request.

**Answer to every §2 question**: wire format cannot represent 16.0km today (in a way that survives to generation). Persistence *can* (the column exists, unpopulated with real values). Runtime *does* currently require a closed enum for identity/routing. Catalog identity *does* require a canonical family string. Frontend *does* let a user type/select an arbitrary distance today, but it is silently discarded — **a real, currently-live product defect, independent of and pre-existing this phase's own work**, disclosed here for completeness (§46) though out of this governance-only phase's scope to fix.

## 3. DistanceFamily vs TargetDistance

```
CURRENT_MODEL: no real separation reaches production. A schema-level separation
(CanonicalDistanceFamily string + RequestedTargetDistanceKm double?) and a
working resolver (CanonicalDistanceFamilyResolver) already exist, dormant and
unconsumed, alongside a closed GoalDistance enum that is the only thing
actually driving routing/catalog identity today.

RECOMMENDED_MODEL: adopt the already-existing separation as real, live
architecture: DistanceFamily (the structural/catalog-identity dimension,
currently {TEN_K, HALF_MARATHON}) stays distinct from TargetDistanceKm (an
arbitrary decimal, runtime-only, never becoming a new catalog identity value).
This is not a new invention — it is activating dormant scaffolding that
already matches this exact recommendation.
```

## 4. Historical intermediate-distance findings

| Finding | Classification |
|---|---|
| `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md:22-23` / `PHASE_HM_X1_4_...md:266-268` §50: "next planned axis... custom/intermediate race-distance generalization for 10.0km < target < 21.1km, pilot candidate 16km" | `ACTIVE_AUTHORITY` — the one real, standing, current, explicitly-unstarted decision; directly the mandate for this phase |
| `PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md:23,112`: "how `RequestedTargetDistanceKm` (e.g. 8K inside TEN_K) should affect `GOAL_FEASIBILITY_IN`... undecided" | `RESEARCH_ONLY` — earlier, narrower framing ("custom distance inside one family"), never resolved, effectively subsumed by the later HM-X1.4 framing |
| `FRONTEND_DOCUMENTATION.md:25,61`: "Custom Distance Selector" screen reference | `HISTORICAL_CONTEXT` — real shipped UI feature reference; the numeric value it collects never reaches the backend (§2) |
| Phase4G.6A cluster's "no intermediate-distance projection" boilerplate disclaimers | `NOT_RELEVANT` (false positive) — refers to Preparation-Runway time-horizon volume interpolation, an unrelated concept; flagged explicitly so it is not miscited |
| `PHASE_HM_0_DISTANCE_GENERALIZATION_REUSE_AUDIT.md` / `HM_21K_Claude_Handoff_and_Build_Plan.md`: "distance generalization" = adding HALF_MARATHON as a second discrete family | `SUPERSEDED_DECISION` (in the sense that it fully executed, now historical) — establishes the product's prior playbook was "enumerate a new fixed family," the opposite pattern from what DIST-GEN.0 evaluates |
| "10K + remainder", "10K + 6K", "parent distance", "anchor distance", "sub-distance", "distance interpolation" | `SOURCE_SILENCE` — zero matches anywhere in the repository for any of these exact framings |

No prior governance decision on this exact question exists to reopen or contradict.

## 5. "10K + 6K" model definitions

- **Model A (Sequential Plan Extension)**: run a complete 10K plan, then append additional weeks for the remaining 6K.
- **Model B (Distance Decomposition)**: use 10K as lower canonical anchor, calculate the 6K remainder as an additional progression component, but still produce one coherent plan.
- **Model C (Numeric Anchor Only)**: use both 10K and HM as reference points without composing two plans.
- **Model D (No Lower-Anchor Role)**: use HALF_MARATHON family authority only, adjusted for target distance.

## 6. Sequential-extension audit

```
SEQUENTIAL_EXTENSION_INVALID
```

Model A fails on training-science grounds, not merely architectural inconvenience. A completed 10K plan's own Taper phase is a deliberate, evidence-based volume/intensity reduction timed to peak fitness for a race that (in this composition) does not actually occur — then Build/quality work must resume from a tapered, de-loaded state to add the remaining 6K. This produces: premature taper (peaking for a phantom race), a training-load reset exactly when continuity is most needed, and race-specific work keyed to the wrong pace domain for most of the composed program (10K-race-specific work for 10 of the 16km, never reoriented toward the actual 16K goal until very late). External evidence (§16) directly corroborates this: no real 10-mile or 15K coaching plan is structured as "run a 10K plan, then extend" — every plan found is one continuous, unified build with a single taper immediately before the actual race. No evidence anywhere supports Model A.

## 7. Coherent single-plan invariant

```
FROZEN: every intermediate-distance plan must preserve one coherent
Foundation → Build → RaceSpecific → Taper arc targeted at the user's actual
race distance, with exactly one Taper (never a mid-program taper for an
intermediate checkpoint).
```

This is directly supported by §6's finding and by every piece of external evidence gathered (§15-16): established intermediate-distance training is uniformly a single continuous arc, never a composition of two race-specific arcs. Phase architecture does not restart at canonical-anchor boundaries.

## 8-9. Parent-family options, decision, and reasoning

Candidate policies audited: UPPER_PARENT, LOWER_PARENT, NEAREST_PARENT, THRESHOLD_BASED, DUAL_ANCHOR.

**Decision**: `HALF_MARATHON` is the correct **structural** parent for the 16km pilot, and — with an explicitly disclosed caveat below — for most of the stated interior range. This is `THRESHOLD_BASED` where the threshold is not an invented number but the **already-existing canonical family boundaries themselves** (TEN_K's own real upper bound, 10.0km; HALF_MARATHON's own real distance, 21.0975km) — i.e., structurally equivalent to `UPPER_PARENT` across the open interval `(10.0, 21.1)`.

**Why, on training-architecture grounds, not proximity**:
1. **Stimulus structure.** Anything meaningfully longer than 10K needs genuine threshold/tempo-dominant quality work rather than 10K's more VO2/speed-oriented emphasis, and often a second distinct quality stimulus — exactly HM's own 2-KEY-lane model (already proven, HM.9-16/HM-X1) versus 10K's simpler single-KEY-dominant structure at lower frequencies.
2. **Pace domain.** External evidence (§16): established 15K coaching plans build their tempo work "around your half marathon pace," not a distinct per-distance pace and not 10K pace — direct evidence that the *relevant pace domain* for intermediate distances above 10K is already HM's, not 10K's.
3. **Core-cycle length.** HM's own Standard Core (10-16W) is structurally closer to what longer intermediate distances need than TEN_K's own shorter 8-14W horizon (`RaceHorizonPolicy`, §2), though see §17's disclosed tension.
4. **Existing dormant architecture already encodes this.** `CanonicalDistanceFamilyResolver`'s own band boundaries (`≤21.1→HalfMarathon`) independently arrive at the identical conclusion — real, tested, if unratified, prior engineering judgment pointing the same direction.

**Disclosed tension, not glossed over**: this audit does **not** claim the exact crossover point near 10.0km itself is proven — e.g., whether a target of 10.5-12km genuinely needs HM-style structure or would still be better served by an extended-10K structural approach is a narrower open question this phase did not gather distance-specific evidence for (my external research covered 10-mile/15K, both meaningfully above 12km, not the immediate 10-12km band). This is recorded as an explicit `DIST-GEN.1`-or-later gap (§50), not resolved here. For the stated pilot (16km), the evidence is strong and undisputed.

## 10. Lower/upper anchor role

- **HALF_MARATHON = STRUCTURAL anchor**: phase architecture, RunLayout family, workout-progression stage identities, core-cycle authority, taper mechanism (2-week non-chained, 0.70/0.43 — see §27).
- **TEN_K = EVIDENTIARY anchor only**: used the same way this engagement already used 10K's own real 5D→6D transition as directional (not literal) evidence for HM-X1.2's magnitude-of-change reasoning. TEN_K's own real numeric envelope provides a lower-bound sanity check and directional evidence for how far HM's own numbers should be scaled down — it never supplies structure.
- Neither anchor is used as a literal NUMERIC interpolation source (§13).

## 11. Distance-projection definition

Projection means: reusing HALF_MARATHON's full structural authority unchanged, while deriving a small, explicitly-bounded set of numeric fields (peak volume, absolute LR ceiling, possibly core-duration preference) specifically for the target distance, using both anchors' real evidence as directional bounds — never a mechanical `value × (target/upperAnchor)` multiplication of every HM number.

## 12. Discrete vs continuous authority table

| Field | Can target distance alter it? | Classification |
|---|---|---|
| Phase order | No | `PARENT_FAMILY_INVARIANT` |
| Phase objective | No (RaceSpecific dose intensity may retune; objective itself does not) | `PARENT_FAMILY_INVARIANT` |
| Core min/preferred/max | Possibly (see §17 tension) | `REQUIRES_EXPLICIT_AUTHORITY` |
| RunLayout | No | `PARENT_FAMILY_INVARIANT` |
| Experience modifier | No — orthogonal to distance | `LEVEL/FREQUENCY_ONLY` |
| Number of KEY roles | No (tied to RunLayout) | `PARENT_FAMILY_INVARIANT` |
| Hardness structure (which family per stage) | No (only the resolved pace target varies) | `PARENT_FAMILY_INVARIANT` |
| Workout progression stages | No | `PARENT_FAMILY_INVARIANT` |
| Race-specific workout identity | No (token stays; resolved pace value varies) | `PARENT_FAMILY_INVARIANT` |
| Peak volume | Yes — real evidence found (§16, §25) | `REQUIRES_EXPLICIT_AUTHORITY` (likely `DISTANCE_BANDED`) |
| Weekly growth rate | No — confirmed literally identical (0.07/0.08) across every named policy in the entire codebase, both families, every level/frequency | `PARENT_FAMILY_INVARIANT` |
| Absolute weekly increase cap | No — same values (2.0/2.5) recur at matching frequencies across both families | `LEVEL/FREQUENCY_ONLY` |
| LR share (Preferred/Selected/HardCap) | No — confirmed byte-identical between TEN_K and HALF_MARATHON at matching level+frequency in the cases directly checked (Intermediate 5D/6D: `0.28/0.36/0.28/0.36` both families) | `LEVEL/FREQUENCY_ONLY` (strong evidence at 5D/6D; not independently re-verified at every frequency) |
| Absolute peak LR | Yes | `REQUIRES_EXPLICIT_AUTHORITY` |
| Starting LR cap | Yes, but likely via reused ratio methodology | `REQUIRES_EXPLICIT_AUTHORITY` |
| Taper duration | No — HM's 2-week mechanism differs structurally from 10K's own single-multiplier approach; HM being parent settles this | `PARENT_FAMILY_INVARIANT` |
| Taper multipliers | No — same reasoning | `PARENT_FAMILY_INVARIANT` |
| Pace semantics | Yes — the resolved value | `DISTANCE_CONTINUOUS` |
| Goal feasibility | Mostly no (ratio-based, generic) — one lookup table (`CanonicalTargetFinishTimePolicy`) is enum-keyed and needs a new entry/generic method | `PARENT_FAMILY_INVARIANT` with one `REQUIRES_EXPLICIT_AUTHORITY` sub-item |
| Calendar rules | No | `PARENT_FAMILY_INVARIANT` |
| Adaptation | No | `PARENT_FAMILY_INVARIANT` |

## 13. Interpolation governance

```
INTERPOLATION_ALLOWED_FIELDS:
  Pace/finish-time equivalence only, via the already-existing, physiologically-
  established Riegel formula (GoalFeasibilityResolver.ResolveFromRecentRace,
  predictedTime = sourceTime * (target/source)^1.06) -- already distance-generic,
  already implemented, dark/unwired. This is race-performance equivalence, not
  training-load scaling, and is explicitly NOT the same category as interpolating
  volume/LR/taper numbers.

INTERPOLATION_PROHIBITED_FIELDS:
  Peak volume, selected peak reference, absolute weekly cap, calibration volume,
  LR shares, absolute peak LR, starting LR cap, taper multipliers, race-specific
  dose, goal-feasibility thresholds, readiness thresholds. No formula-based
  authority exists for any of these anywhere in the current architecture; every
  existing value at every current cell was evidence-derived or product-decided,
  never computed by interpolation. This engagement's own standing "no ungoverned
  extrapolation" discipline (HM-X1.2 §38) extends directly to this track.

INTERPOLATION_DECISION_REQUIRED_FIELDS:
  Core-duration bounds (min/preferred/max) for the interior of the range -- a
  structural, not training-load, field, where interpolation between TEN_K's
  8-14W and HM's 10-16W is mathematically plausible but currently unauthorized;
  DIST-GEN.1 must decide explicitly, informed by real evidence (§17's own
  disclosed tension), not by default.
```

## 14. Distance-band alternative

No evidence was found supporting a genuinely distinct third structural category (e.g., a real "15K-type" family). §16's own evidence shows 15K training already leans toward HM-style pace/structure rather than occupying a separate middle category. The defensible model remains a two-anchor system (TEN_K, HALF_MARATHON) with HALF_MARATHON as sole structural parent across the interior, differentiated only at the numeric layer (potentially via bands, per §13's `DISTANCE_BANDED` classification for peak volume) — not via a third structural family. No `FIFTEEN_K_MASTER`-equivalent is created or implied.

## 15. External architecture evidence

Internal repository synthesis alone could not resolve the parent-family and pace-domain questions with full confidence (no internal precedent exists for a genuinely intermediate distance), so narrow external research was performed, restricted to architecture-level evidence (never numeric training policy — see §48's discipline).

## 16. 10-mile / 16K evidence

| Source finding | Distance | Plan length | Weekly volume | Long run | Quality structure | Taper | Classification |
|---|---|---|---|---|---|---|---|
| Multiple 10-mile plan sources (Marathon Handbook, Snacking in Sneakers, RUN/Outside, Cherry Blossom official program) | 10 mile (≈16.09km) | 8-12 weeks | ~20-35mi (32-56km), peaking ~23mi | builds 6-8mi → 10mi | long run + tempo + intervals + easy + cross-training, 4 days/week typical | not detailed in sources found | Supports architecture closer to a reduced-HM shape than an extended-10K shape (weekly volume and long-run progression sit above 10K's own ~15-25mi range) |
| Multiple 15K sources (Salomon, best-running-tips.com, runninforsweets) | 15K | ~8 weeks (from a 15-20mi/week base) | 20-33mi/week | 8-14.5km (5-9mi), building toward 90-120min | 3-4 days/week: 1 easy, 1 quality (tempo/intervals), 1 long run; tempo work explicitly described as "around your half marathon pace" | not detailed | **Decisive**: 15K's own quality-session pace domain is HM's, not a distinct one — directly supports HALF_MARATHON as the relevant pace-domain parent even for distances well below 16km |
| Native half-marathon plan sources (for LR-vs-race-distance comparison) | 21.1km (HM itself) | 10-16W (matches Appsel's own frozen authority) | 22-34mi (35-55km) | commonly 16-20km (10-12.4mi) — **below** the 21.1km race distance | 4-5 days/week | not the focus of this search | Confirms the general principle: peak long run stays below actual race distance even for the anchor family itself |

**What these sources support**: architecture-level classification (intermediate distances resemble a reduced/scaled HM shape more than an extended 10K shape; quality work is HM-pace-domain-anchored; peak LR generically stays below race distance). **What they do NOT support**: any specific numeric value for Appsel's own 16K authority — no third-party number is imported; these are used only to classify architecture, per §48's discipline.

## 17. Core-duration model

Candidate models: (A) parent-family CoreCycle unchanged; (B) exact distance changes preferred duration; (C) distance bands determine horizon; (D) new authority required.

**Disclosed tension, not resolved**: structural/pace/volume evidence favors HM as parent (§9), which would argue for Model A (reuse HM's 10-16W unchanged). However, the 10-mile evidence gathered (§16) shows real-world 10-mile plans commonly run 8-12 weeks — shorter than HM's own 10-16W range, closer to TEN_K's 8-14W. This is a genuine, disclosed inconsistency: the *structural* case for HM parent is strong, but *core duration specifically* shows real evidence of being more distance-sensitive than the other structural fields. **This phase does not resolve which of Model A/B/C is correct** — it records the tension honestly and assigns it to DIST-GEN.1 as a required decision, informed by further evidence rather than assumed inheritance.

## 18. Phase allocation

`REUSE_GENERIC` — HM's phase allocator (`CatalogPhaseAllocationResolver`) is already fully generic, parameterized purely by target week count, and already proven correct across every horizon 10-16W (HM-X1.3). No per-distance phase table is needed; whether RaceSpecific's own share of total weeks should shift with target distance is a numeric question for DIST-GEN.1, not a new-mechanism question.

## 19. Workout progression

`REUSE_WITH_GENERIC_RENAMING_REQUIRED` (recommended) or `REUSE_SEMANTICALLY_UNCHANGED_IF_CONFIRMED_INTERNAL_ONLY` (acceptable alternative). HM's stage keys (`BUILD_FASTER_THAN_HM_INTRO`, `RACE_SPECIFIC_HM_INTRODUCTION`, etc.) carry "HM" literally in their identifier strings, even though the underlying mechanism (stage → lane binding, fallback chains) is fully generic and already proven reusable at a new frequency (HM-X1). Whether this matters depends on whether these keys ever reach a user-visible surface — this audit did not independently re-verify that boundary (HM.19/HM-X1 material treats them as internal-only progression identifiers, never display strings) and records this as a narrow confirmation DIST-GEN.1 should make explicitly rather than assume.

## 20. Target-race-pace semantics

**Yes, a generic `TARGET_RACE_PACE` semantic concept is warranted, and is architecturally cheap** — not a new paradigm. Both `HM_PACE` (`hm-pace.v1.json`) and `GOAL_PACE_TEN_K` carry symbolic `intensityDescriptor` values (`"HM_PACE"`, `"GOAL_PACE"`), never a baked-in numeric pace — actual pace values are always resolved at runtime from user evidence. A generic descriptor generalizing this existing pattern (rather than hardcoding a third distance-named token) is consistent with, not a departure from, current architecture. This phase does not implement it.

## 21. Pace-evidence compatibility

The full evidence hierarchy (`PaceSourceResolver`: RECENT_RACE → TARGET_TIME → ESTIMATED → NONE) is **already fully distance-generic** — confirmed by direct code read, no distance branching anywhere in `Resolve`. The invariant that a desired/target finish time never manufactures "current fitness" evidence is explicitly enforced in code (`GoalFeasibilityResolver.cs` lines 159-181) and is likewise distance-generic (ratio/source-type logic, no distance branching). **No capability gap found** in this specific pipeline.

## 22. Recent-race normalization

**A distance-generic race-equivalence formula already exists**: the Riegel formula in `GoalFeasibilityResolver.ResolveFromRecentRace` (`predictedTime = sourceTime × (targetDistance/sourceDistance)^1.06`) takes arbitrary `double` distances, not enum-keyed values, and already prefers an arbitrary requested-distance field (`RequestedTargetDistanceKm`) over the fixed enum-derived one when present. **This substantially closes what could otherwise have been a major authority gap** — the exact future work is wiring this already-correct, already-generic formula into the live generation path, not inventing a new equivalency method.

## 23. Long-run conceptual authority

Per §12's own separation:
- `PreferredShare`/`SelectedShare`/`HardShareCap`: `LEVEL/FREQUENCY_ONLY` — strong evidence these are already distance-invariant (identical values recur across TEN_K and HALF_MARATHON at matching level/frequency).
- `AbsolutePeakLongRun`/`StartingLongRunCap`: `REQUIRES_EXPLICIT_AUTHORITY` — genuinely distance-sensitive in principle, no existing ratio/formula found anywhere relating either value to race distance (confirmed: HM's own 19.0km ceiling has **no** documented or computed relationship to its 21.0975km race distance — it is a purely evidence-sourced constant, HM.1.1's own evidence base, explicitly "a ceiling, never a target").

Model selection from §23's candidate list: closest to **(E) another evidence-supported model** — specifically, a *conceptually* distance-aware ceiling (bounded by the general "stay below race distance" principle, §24) whose *exact* value still requires its own dedicated evidence-based authority closure (not derivable by any existing formula), rather than (A) HM's ceiling with a lower exact-distance substituted, (B) share policy with a projected absolute ceiling, or (D) 10K's own ceiling extended upward (10K's own long-run authority doesn't use this same optional-field pattern at all).

## 24. Race-distance vs long-run relationship

**Governing conceptual principle, frozen (not numeric)**: peak long run should remain **below** the target race distance, never at or above it. This is not invented — it is the consistent pattern found across every band of external evidence gathered (§16): HM's own real long runs (16-20km) stay below its own 21.1km race; 15K plans cap long runs at 8-14.5km, below 15K. The corollary constraint for a 16K target: its eventual peak-LR authority must simultaneously satisfy `< TargetDistanceKm` **and** `≤ 19.0km` (HM's own frozen ceiling — nothing found justifies exceeding the structural parent's own ceiling for a *shorter* target distance than the parent's own race distance). Exact value: deferred to DIST-GEN.1.

## 25. Weekly-volume conceptual authority

**Materially dependent on exact target distance, not solely `DistanceFamily × Level × Frequency`.** This is the single strongest piece of evidence for why genuine distance-projection (not mere structural reuse) is necessary: using HM's own full 16-frequency-cell peak-volume authority unchanged for a 16K target would over-prescribe (16K is a meaningfully shorter, less volume-demanding race than 21.1K); using TEN_K's own authority would under-prescribe (external evidence shows 10-mile/15K volumes consistently exceed 10K's own envelope). This is the core numeric-authority gap DIST-GEN.1 must close.

## 26. Growth-rate conceptual authority

Audited, not merely hypothesized: `PreferredWeeklyIncreaseRate = 0.07`, `HardWeeklyIncreaseRate = 0.08` are **literally identical across every single named policy in the entire `VolumeSafetyPolicy.cs` file**, both distance families, every level, every frequency, with zero exception found anywhere. This is `PARENT_FAMILY_INVARIANT` with the strongest possible internal evidence — reuse unchanged. `AbsoluteWeeklyIncreaseCapKm` shows the same value (2.0 at 3D, 2.5 at 4D+) recurring across both families at matching frequencies — classified `LEVEL/FREQUENCY_ONLY`, not distance-continuous.

## 27. Taper conceptual authority

**Mechanism, not just duration, differs genuinely between the two families**: HALF_MARATHON uses a 2-week, non-chained, fixed-anchor mechanism with two independent multipliers (0.70/0.43); TEN_K uses a structurally different single-multiplier (0.53) mechanism. This is a real, evidenced divergence — not an assumption. Given HALF_MARATHON is the chosen structural parent (§9), the working recommendation is: **inherit HM's own 2-week/0.70+0.43 mechanism unchanged** (`PARENT_FAMILY_INVARIANT`), not TEN_K's. No 16K-specific taper values are invented; DIST-GEN.1 should confirm (not assume) this mechanism doesn't itself need distance-sensitivity within the interior, since no evidence either way was gathered for taper specifically.

## 28. Level/frequency orthogonality

Reaffirmed, not reopened: `TargetDistanceKm` must be an additive dimension alongside the existing Level × Frequency authority, exactly as this engagement has consistently treated every prior expansion axis (6D was added as an additive frequency dimension without redefining Level; the same discipline applies here for Distance). No new Level or Frequency semantics are introduced or implied by this audit.

## 29-30. Pilot cell selection and rationale

**Pilot cell: Intermediate 4D** (confirmed, not merely assumed). Reasoning: 4D is HM's structurally simplest frequency (exactly one `KEY_SESSION` role, no second-KEY-lane complexity), avoiding conflation of the distance-projection question with the dual-KEY interaction questions HM-X1 already resolved separately for 5D/6D. Intermediate is the level with the richest, most mature precedent in *both* families (10K's own `Default`/`ThreeDayIntermediate`/`FiveDayIntermediate`/`SixDayIntermediate` are all well-established; HM's own Intermediate authority spans 3D-6D). 4D avoids 3D's own idiosyncratic evidence-based LR-share uplift (HM.7's unique 0.34/0.40/0.38/0.46 quadruple) and avoids Advanced's second-hard-stimulus questions entirely. **Confirmed, matching the prompt's own suggested default.**

**Why 16K as pilot distance**: confirmed defensible — it sits genuinely interior to the range (6km above TEN_K's 10.0, 5.1km below HM's 21.1, not degenerate-near-either-anchor); it corresponds to the well-established, evidence-rich 10-mile (16.09km) race distance, giving real external architecture evidence rather than an arbitrary number; both anchor families are fully mature and publicly activated, isolating the distance-projection question cleanly from any remaining architecture-capability question. No more defensible alternative target was found.

## 31. Target-distance precision

Representation precision and authority precision are distinct, per this phase's own explicit instruction not to confuse them: **representation** (storage/computation) should support arbitrary decimal km, since the existing dormant infrastructure (`RequestedTargetDistanceKm double?`, `CanonicalDistanceFamilyResolver`) already does this natively at zero additional cost. **Authority** (the actual frozen numeric policies) will realistically only ever cover a curated, evidence-backed set of specific distances (16.0km, 10-mile/16.09km, and potentially 15K/12K/18K/20K later), each requiring its own dedicated closure phase mirroring how HALF_MARATHON itself became a second explicit family — not literal infinite-precision authority for every possible decimal.

## 32. Catalog identity

`TargetDistanceKm` should remain a **runtime projection input**, never part of catalog combination identity. Confirmed by direct architecture read: today's combination files are keyed by family+layout+modifier+frequency, not by exact distance. The existing `HALF_MARATHON__4D__INTERMEDIATE` combination would serve both a literal 21.1K request and a projected 16K request identically at the catalog-loading layer; `TargetDistanceKm` would be consumed only by downstream, distance-aware policy resolution — no new catalog artifact per exact distance.

## 33. Catalog-explosion analysis

The `TWELVE_K_MASTER`/`THIRTEEN_K_MASTER`/...-style per-distance-master anti-pattern is explicitly rejected: no evidence anywhere justifies it, and it directly contradicts this phase's own stated central goal (a reusable model, not merely making 16K work). The canonical-families-plus-runtime-projection default is confirmed safe and is the approved direction (§32).

## 34. Runtime projection ownership

Recommended distributed ownership, explicitly avoiding one monolithic `CustomDistanceCalculator`:
- **Distance-family resolution**: the already-existing (dormant) `CanonicalDistanceFamilyResolver` — needs wiring, not redesign.
- **Volume/peak-volume projection**: a new, narrowly-scoped typed policy sitting alongside (not inside) `VolumeSafetyPolicy`, consulted only when `TargetDistanceKm` differs from the family-representative constant.
- **Long-run ceiling projection**: a separate typed authority from volume projection (distinct evidence basis, per §23).
- **Pace/target-race-pace resolution**: extends the *already-existing* dark Riegel-based `GoalFeasibilityResolver` pipeline — no new component, just live-wiring.
- **Horizon/core-cycle resolution**: extends `RaceHorizonPolicy`'s already-generic internal `Decide`/`Classify` engine with a new dispatch path, reusing (not rebuilding) its numeric-parameter core.

## 35. Typed projection-context recommendation

Recommended minimal fields: `ParentDistanceFamily`, `LowerAnchorDistanceKm` (evidentiary), `UpperAnchorDistanceKm` (structural boundary), `TargetDistanceKm`. **`NormalizedPosition` is explicitly excluded** from this recommendation — per §13's own governance, no policy is currently approved to consume a normalized interpolation ratio, and including it as a first-class context field would implicitly suggest interpolation is broadly sanctioned when it is not (§13). If a future evidence-backed field ever needs a ratio, it should be computed locally at the point of use.

## 36. Boundary semantics

`10.0` belongs to the canonical TEN_K path; `21.1` (more precisely, HM's own real `21.0975`) belongs to the canonical HALF_MARATHON path — both are explicitly outside the open interval this track generalizes (`10.0 < TargetDistanceKm < 21.1`), not edge cases the projection mechanism itself must handle. Values like `10.0001` or `21.09` fall inside the open interval and require the projection authority DIST-GEN.1 would freeze. Note recorded: the stated "21.1" boundary literal is an approximation of HM's own real canonical constant `21.0975` — any future boundary check must use the real constant, not a rounded literal, to avoid a definitional gap between "last non-canonical value" and "first canonical value."

## 37. Canonical-anchor zero-delta invariant

```
FROZEN, MANDATORY: TargetDistance = 10.0 -> current TEN_K behavior, exactly,
byte-for-byte. TargetDistance = 21.0975 -> current HALF_MARATHON behavior,
exactly, byte-for-byte. No approximate equivalence, ever, at either anchor.
```

## 38. Monotonicity rules

For continuous/banded numeric fields (peak volume, absolute LR ceiling), monotonicity as target distance increases from 10 toward 21.1 is a reasonable expected invariant **if and only if** a continuous or banded projection model is eventually adopted for that field — not universally required. Categorical fields (taper mechanism, which workout family is assigned to which stage) are legitimately step-function/parent-family-invariant, not monotonic in any numeric sense, and should not be forced into a monotonicity framing.

## 39. Continuity rules

If a banded model is eventually adopted for peak volume/LR ceiling (§13's `DISTANCE_BANDED` classification), any two adjacent bands (e.g. a hypothetical "15.9K" vs "16.0K" split) must not produce a large numeric jump absent a real, evidenced training-category boundary — recorded here as a standing future review standard, not a resolved threshold. No specific band edges are proposed by this phase.

## 40. Goal-feasibility compatibility

Mostly already reusable: the ratio-based classification (`REALISTIC`/`CHALLENGING`/`UNSUPPORTED`/`NOT_REQUIRED`, keyed by percentage gap via the Riegel projection) is distance-generic by construction (§22). The one real, bounded gap: `CanonicalTargetFinishTimePolicy` (`backend/RunningApp.Application/Common/CanonicalTargetFinishTimePolicy.cs:17-33`) is a fixed lookup table of product-average target times keyed by the closed `GoalDistance` enum, returning `null` for anything else — an intermediate distance's own product-average default would need either a new table entry or (more consistent with the rest of this audit's findings) a generic Riegel-based derivation from the two anchors' own existing values, rather than a hand-picked new constant.

## 41. Readiness compatibility

HM's existing readiness inputs (recent weekly volume, recent longest run, recent runs/week) are not distance-family-specific at the input level — they are raw user-reported values consumed generically by the fail-closed `ResolveStartingVolume` mechanism. No distance-specific readiness threshold currently exists, and no evidence gathered in this audit suggests one is structurally necessary — projected readiness (i.e., the same generic mechanism applied against whatever calibration/peak values DIST-GEN.1 eventually freezes) appears safely derivable without new authority. No numeric threshold is proposed here.

## 42. Calibration boundary

Calibration must remain non-runtime, exactly as at every existing cell (`CatalogVolumeAndLongRunPlanner.ResolveStartingVolume`'s fail-closed guard already generically enforces this and requires no new mechanism). The most likely future model, consistent with every prior HM/10K frequency-addition precedent, is **distance-projected calibration**: applying the same already-established ratio-reuse methodology (e.g., an existing frequency's own calibration/peak ratio applied to the new peak) once DIST-GEN.1 freezes the 16K peak — not a wholly new calibration model, and not a pilot-specific ad hoc fixture invented outside that methodology.

## 43. Adaptation compatibility

```
GENERIC_ADAPTATION_REUSE
```

Confirmed by full-file read: `PlaceholderAdaptationEngine.cs` contains no `GoalDistance` reference of any kind — it is a distance-agnostic stub today. No gap found; no custom-distance adaptation engine is needed.

## 44. Confirmation/persistence compatibility

**A future 16K plan CAN already, in principle, preserve both `ParentDistanceFamily = HALF_MARATHON` and `TargetDistanceKm = 16.0` through persistence** — the schema already supports exactly this split (§2-§3). The real, bounded gap is not schema-level but **pipeline- and DTO-level**: (a) nothing upstream of `CatalogPlanConfirmationService.BuildCatalogTrainingPlan` currently produces a genuinely distinct `RequestedTargetDistanceKm` value (it is always set equal to the fixed family constant today); (b) the public API response layer (`HomeResponse`/`ActivePlanSummaryDto`) has **no numeric-distance field at all** — only the enum's snake_case label (`"half_marathon"`) is ever surfaced, confirmed by full-file read finding no `distance_km`-equivalent sibling field anywhere in that DTO. Both gaps are bounded and well-understood, not open-ended architecture questions.

## 45. User-visible distance identity

**Mandatory, and currently unimplemented**: if 16K uses HALF_MARATHON as internal structural parent, the UI must display "16K," never "Half Marathon," unless the product explicitly wants family labels shown. Per §44, this requires extending `HomeResponse`/`ActivePlanSummaryDto` (and equivalent calendar/detail DTOs) with a genuine user-facing numeric target-distance field, separate from the internal family label — a real, disclosed, bounded implementation gap for a future phase, not resolved here.

## 46. Frontend capability

Confirmed real, currently-live capability **and a currently-live silent defect, independent of this phase's own work**: both `race_details_page.dart` (free-text distance field, ±0.2km preset-snap) and `custom_goal_page.dart` (unbounded integer-km stepper) already let a user express an arbitrary distance today — but in both flows, the typed/selected number is discarded before the HTTP request is built; only the coarse string `'custom'` is transmitted, which resolves server-side to a meaningless 5.0km default (§2). **This means a user attempting to enter "16" today silently receives a plan generated as if for a 5K.** This is disclosed here prominently as a real, pre-existing product defect this audit surfaced as a byproduct of its own research — not something DIST-GEN.0 is authorized to fix (governance-only phase), but worth flagging outside this report as well given its user-facing severity.

## 47. UX implications

Both presentation models remain viable and are not mutually exclusive: a genuine free-form "custom race distance" entry (already half-built in the frontend, per §46) alongside additional common preset cards for well-evidenced intermediate distances (16K/10-mile once DIST-GEN.1 exists) are both consistent with the approved architecture. No UI redesign is undertaken or specified here.

## 48. Final architecture comparison

| Option | Training coherence | Authority support | Catalog scalability | Zero-delta safety | Complexity | Pace semantics | LR semantics | Extensibility | Invented-number risk |
|---|---|---|---|---|---|---|---|---|---|
| A: TEN_K + remainder sequential | Poor (§6) | None | N/A | Poor | Moderate-high | Poor | Poor | Poor | High |
| B: TEN_K lower-anchor expansion | Fair | Weak (contradicted by §16) | Fine | Fair | Moderate | Poor fit | Poor fit | Poor for upper interior | Moderate-high |
| C: HALF_MARATHON parent down-projection | Good | Strong | Excellent | Clean | Moderate | Good fit | Needs new evidence, but clear conceptual anchor | Excellent | Low-moderate |
| D: Dual-anchor governed projection | Good | Strong (best) | Excellent | Clean (strongest) | Moderate | Good fit | Best (two real reference points) | Excellent | Low |
| E: Discrete intermediate band (new structural family) | Good if evidenced | Weak (no distinct-category evidence found) | Poor if literal | Fine but pointless | High | Redundant | Redundant | Poor | Moderate |
| F: New distance master per target | Fine per-instance | None | Terrible | Trivial but defeats purpose | High long-term | N/A | N/A | Terrible | High |

## 49. Approved architecture

```
OPTION D — DUAL-ANCHOR GOVERNED PROJECTION
```

Concretely: **HALF_MARATHON is the sole structural parent** for the interior of `(10.0, 21.1)` (phase architecture, RunLayout, workout progression, taper mechanism — all `PARENT_FAMILY_INVARIANT`, reused unmodified). **TEN_K is retained as a secondary, non-structural evidentiary anchor**, used the same disciplined way this engagement already used cross-frequency/cross-distance precedent at HM-X1.2 — informing the *magnitude and direction* of the numeric-authority work (peak volume, LR ceiling) that DIST-GEN.1 must still freeze from real evidence, never supplying structure and never being literally interpolated. This is effectively Option C strengthened by Option D's explicit dual-anchor evidentiary discipline, which is why D (not C) is the named approved architecture — it is the more complete, more rigorous answer, not a competing alternative to C.

## 50. DIST-GEN.1 authority-gap register

- Exact 16K `PeakVolumeBand`/`SelectedPeakReference` for Intermediate 4D (and eventually other cells).
- Exact 16K `PreferredAbsolutePeakLongRunKm`/`StartingAbsoluteLongRunCapKm`, bounded conceptually by `< 16.0` and `≤ 19.0` (§24) but not yet numerically frozen.
- Confirmation (or resolution) of the Core-duration tension disclosed in §17 — does Intermediate 4D at 16K use HM's 10-16W unchanged, a narrower band, or new authority.
- Confirmation of whether HM's stage-key naming needs a variant/rename for non-HM targets, or is safely internal-only (§19).
- Decision on wiring the already-existing dark Riegel/`CanonicalDistanceFamilyResolver` infrastructure live, vs. building new parallel logic.
- `CanonicalTargetFinishTimePolicy`'s new entry/generic method for a 16K product-average default (§40).
- The narrower open question of whether the immediate 10.0-12.0km sub-band genuinely belongs structurally to HALF_MARATHON or needs its own evidence (§9's disclosed caveat) — out of scope for the 16K pilot itself but relevant to any future pilot closer to 10K.
- The `HomeResponse`/`ActivePlanSummaryDto` (and calendar/detail equivalents) numeric-distance-display gap (§44-45) — an implementation-phase item, not an authority item, but must be tracked.
- Taper mechanism's own distance-sensitivity within the interior (§27) — not evidenced either way, assumed inherited from HM pending confirmation.

## 51. Existing-product zero-delta statement

No file governing existing 5K, 10K, or HALF_MARATHON behavior was read for modification or modified. No change to the HM public matrix, 10K catalog behavior, Experienced exclusion, frequency eligibility, or horizon behavior. This track is purely additive, confirmed via `git status --porcelain` before this report was finalized (§52).

## 52. Remaining blockers

None block this phase's own closure. The pre-existing frontend distance-discard defect (§46) is disclosed as a live, user-facing issue independent of this phase's own scope — recorded, not fixed, and not counted as a DIST-GEN.0 blocker since it predates and is orthogonal to whether this architecture track proceeds.

---

## DIST-GEN.1 handoff (per §55, not begun)

If this phase closes, the next phase is approximately: **DIST-GEN.1 — 16K Intermediate 4D Target-Distance Projection Structural and Numeric Authority Closure**, which would freeze exactly the items in §50's gap register (16K-specific peak volume, LR authority, race-specific pace semantics wiring, taper confirmation, readiness/feasibility items) — confirmed as the correct next step only because Intermediate 4D is confirmed here as the correct pilot cell (§29-30). This phase does not begin that work.

---

## FINAL CLASSIFICATION

```
INTERMEDIATE_DISTANCE_10K_TO_HM_ARCHITECTURE_AND_PROJECTION_MODEL_CLOSED
```

Current distance architecture is fully audited (§2-3); historical custom-distance work is classified (§4); the "10K + remainder" concept is explicitly resolved (§5-7, sequential extension rejected on training-science and external-evidence grounds); structural parent policy is frozen (§8-9, HALF_MARATHON, with one narrow, explicitly-disclosed caveat near the 10K boundary that does not block the 16K pilot); `DistanceFamily` vs `TargetDistance` semantics are frozen (§3, activating already-existing dormant schema/resolver infrastructure rather than inventing new); anchor roles are defined (§10, structural HM / evidentiary TEN_K); catalog-identity strategy is defined (§32-33, runtime input, never new catalog identity, avoiding explosion); projection ownership is defined (§34-35, distributed, no monolithic calculator); interpolation governance is explicit (§13, one narrow physiologically-justified exception for pace equivalence, everything else prohibited absent new authority); continuous/banded/invariant authority categories are classified (§12, a full field-by-field table); workout-progression compatibility is understood (§19, mechanism reusable, naming question flagged); race-specific pace architecture is understood (§20-22, including the significant discovery of already-existing dark distance-generic Riegel infrastructure); long-run conceptual model is understood (§23-24, including a real, evidence-grounded conceptual ceiling constraint); persistence can preserve user-visible target distance with the exact gap bounded (§44-45, schema-ready, pipeline/DTO gap identified precisely); canonical 10K/HM zero-delta is mandatory and recorded (§37); a single next pilot cell is selected (§29-30, Intermediate 4D at 16K); no exact unsupported 16K numeric authority was invented anywhere in this report (§51's register lists gaps, never fills them); and no production, catalog, schema, Flutter runtime, or API contract file was changed by this phase.
