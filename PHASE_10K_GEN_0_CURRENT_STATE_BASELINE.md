# Phase 10K-GEN.0 — Current-State Plan Generation Baseline

**Read/audit only. No production code was written or modified. This document is descriptive, not prescriptive — it records what exists today, not what a generalized architecture should look like.**

---

## 1. End-to-end current generation flow

### Top-level routing fact (established first, governs everything below)

There is **no single "10K/Intermediate/4D generation pipeline."** Which of three structurally distinct engines handles a request is decided purely by the **StartDate→RaceDate week gap**, computed once by `RaceHorizonPolicy.CalculateAvailableWeeks` (`backend/RunningApp.Application/Common/RaceHorizonPolicy.cs`):

| Available weeks | Engine | Entry point |
|---|---|---|
| 8–14 | Catalog "core" pipeline | `POST /api/v1/plans/generate-preview/race` → `PlanServices.GenerateCatalogPreviewAsync` |
| 15–20 | "Preparation Runway" sub-pipeline | same endpoint → `PlanServices.GeneratePreparationRunwayPreviewAsync` |
| 21+ | Rejected (422 `PLAN_HORIZON_COMPOSITION_REQUIRED`) — client must call the separate LongHorizon endpoint | `POST /api/v1/plans/generate-preview/race/long-horizon` → `ILongHorizonPublicPlanService` |

`TrainingPlan.ScheduleStrategy` (`PlanScheduleStrategy.StaticComplete` vs `PlanScheduleStrategy.RollingLongHorizon`) is the persisted discriminator. **A catalog-routed 8–20-week TEN_K plan has no shared persisted representation with, and is never read/adapted by, the LongHorizon rolling-window Adaptation V1 system** — Adaptation V1 (all the `WindowExecutionSummaryBuilder`/`NextWindowLoadDecisionPolicy`/weekly-aggregation work from prior phases) only ever operates on `RollingLongHorizon` plans. This baseline documents the **8–20-week catalog/PreparationRunway path** as the primary "10K/Intermediate/4D generation" trace, since that is the dominant, non-rolling generation pipeline; the LongHorizon path is covered separately in §7 (Adaptation V1 dependency baseline) since it has its own, structurally independent generation logic under `Schedule/LongHorizon/`.

### Step-by-step trace (8–14 week catalog path, the shortest/most direct case)

**1. Request DTO / API contract**
- `RunningApp.Api/Controllers/PlansController.cs:79` — `[HttpPost("generate-preview/race")]` → `GenerateRacePreview([FromBody] GenerateRacePlanPreviewRequest request, ...)`.
- DTO: `RunningApp.Application/DTOs/Plan/GenerateRacePlanPreviewRequest.cs`. Fields include goal distance, level, days-per-week, preferred days, long-run day, start/race date, target finish time.
- Knows about: Distance ✓ (`goal_distance`), Level ✓ (`level`), DaysPerWeek ✓ (`days_per_week`), CalendarDate ✓ (`start_date`/`race_date`). Phase/SessionRole/WeekIndex: not present at this layer.

**2. Validation**
- `GenerateRacePlanPreviewRequestValidator.Validate(request)` (called from `PlanServices.GeneratePreviewAsync` line 122). Enforces generic bounds (e.g. `DaysPerWeek` 1–7, `PreferredDays.Count == DaysPerWeek`) — **not** hardcoded to 4 at this layer (confirmed by Agent research: PARAMETERIZED here).

**3. Command/domain mapping**
- `GeneratePreviewCommandMapper.ToCommand(request)` (`PlanServices.cs:196`) — maps the wire DTO to an internal `GeneratePreviewRequest`/command shape.

**4. Template/catalog selection (identity gate)**
- `V1CatalogPilotIdentityPolicy.IsSupportedIdentity(goalType, goalDistance, level, daysPerWeek)` (`RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs:59-67`) — a pure 4-field boolean AND: `GoalType==Race && GoalDistance==TenK && Level==Intermediate && DaysPerWeek==4`. On match, resolves the literal constant `CandidateKey = "TEN_K__4D__INTERMEDIATE"`, `CandidateVersion = 10`. On mismatch, the request routes to `LivePlanPreviewRoute.Legacy` (a different, older generation engine, out of scope for this pilot trace) via `LivePlanPreviewRouting.cs`.
- Knows about: Distance ✓, Level ✓, DaysPerWeek ✓. This is the single point where all three axes are checked together, and it is a **flat, hand-authored constant match**, not a computed/interpolated key (confirmed: no `$"{distance}_{level}_{days}"` string-building pattern exists anywhere in the codebase).

**5. Runtime-condition resolution**
- `RuntimeCatalog/Resolvers/` — `TimeAdequacyResolver`, `PaceSourceResolver`, `CoreEntryReadinessResolver`, `GoalFeasibilityResolver`. These evaluate typed runtime conditions (e.g. is there enough time before race day, what pace source to use) against the loaded candidate + request. None of these resolvers key directly on `RunningBackground`/Level per Agent C's findings (`PlanCatalogDomainMapper.ClassifyRuntimeConditionGroups` — zero resolver implementations reference level directly).

**6. Horizon / cycle-length resolution**
- `CoreHorizonClassifier` (`RuntimeCatalog/Schedule/Horizon/`) + `RaceHorizonPolicy.CalculateAvailableWeeks` — the single canonical horizon-week authority (also used, unchanged, by the LongHorizon subsystem per prior-phase work).

**7. Phase allocation**
- `CatalogPhaseAllocationResolver.Resolve` (`RuntimeCatalog/Schedule/Materialization/`) computes per-phase `AllocatedWeeks` from catalog `MinimumWeeks/PreferredWeeks/MaximumWeeks/CompressionPriority/ExtensionPriority`. The four phase keys themselves (`FOUNDATION`, `BUILD`, `RACE_SPECIFIC`, `TAPER`) are a hardcoded **required set** in `PlanCatalogBundleLoader.ReadPhaseAllocations` (lines 175-178) — catalog-configurable per-phase week counts, but not a catalog-configurable phase-name set.
- Knows about: Phase ✓, WeekIndex ✓ (via week-count arithmetic). Distance/Level/DaysPerWeek: only indirectly, via whichever candidate's catalog data was already loaded.

**8. Weekly skeleton creation**
- `CatalogRunLayoutResolver.Resolve` (`Schedule/Materialization/CatalogRunLayoutSlots.cs`) reads `candidate.SlotRoles` — a **variable-length** list read verbatim from the loaded candidate JSON (`run-layout-4d.v2.json`), validated only against `candidate.DaysPerWeek` (not a literal 4). This resolver is genuinely generic code (confirmed PARAMETERIZED); it happens to always resolve to `[KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN]` today only because that is the one layout file that exists on disk.
- Knows about: DaysPerWeek ✓ (via `candidate.DaysPerWeek`), SessionRole ✓ (`SlotRoles` strings).

**9. Workout/catalog resolution**
- `V1CatalogWorkoutRoleBindingPolicy` (`Schedule/Binding/`) maps each role name (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`) to a binding mode (`FixedDefault`/`StageControlled`) and, for fixed-default roles, one workout key (`EASY_STANDARD`, `LONG_RUN_STANDARD`). `ProgressionStageAllocator.AllocatePhase` supplies the phase-varying `ProgressionStageKey` for stage-controlled roles (e.g. `GOAL_PACE_REHEARSAL` in RACE_SPECIFIC).
- Knows about: Phase ✓ (via progression stage), SessionRole ✓.

**10. Volume/distance prescription**
- `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` (`Prescription/Volume/`) computes weekly volume via linear interpolation to a peak, using `VolumeSafetyPolicy.Default` — a **single, hardcoded record instance** (starting 24km, peak 38km, taper multiplier 0.53, long-run share 0.30–0.36) explicitly commented as `"profilePercentageCaps.INTERMEDIATE"` and `"four_day_long_run_preferred_share"`. TAPER phase is the only phase given a distinct volume rule (multiplicative decay instead of linear interpolation).
- `FourDaySessionDistanceAllocationPolicy.Allocate(weeklyVolumeKm, longRunDistanceKm)` then splits the week's volume into exactly one KEY + two EASY + one LONG distance, with a hardcoded `2 *` multiplier for "two easy sessions" (line 22).
- `V1FourDaySessionVolumeAllocationPolicy.Allocate` gates this: throws `CatalogSessionPrescriptionInfeasibleException` unless `sessions.Count==4 && KEY==1 && EASY==2 && LONG==1` (lines 30-35) — the single most load-bearing hard 4-day/1-2-1 gate in the whole prescription subsystem.
- `CatalogPeakVolumeBandLoader.LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct)` — by contrast, a genuinely composite-key catalog lookup (matches on `(distanceFamily, experience, runsPerWeek)` against a flat `entries[]` array) that clamps (but does not itself compute) the peak.
- Knows about: DaysPerWeek ✓ (hardcoded 4), Level ✓ (via the peak-volume-band lookup's `experience` parameter), SessionRole ✓.

**11. PreferredDays / calendar assignment**
- `CatalogWeekSkeletonCalendarMaterializer.ValidatePreferredDays`/`ValidateSkeletonRoleStructure` (`Schedule/Materialization/`) — hardcodes `if (skeleton.DaysPerWeek != 4) throw ...`, `if (week.SessionSlots.Count != 4) throw ...`, and explicit role-count checks against literal `1`/`2`/`1`/`4`. Class doc comment states the algorithm assumes "exactly one KEY_SESSION, two EASY_SUPPORT, one LONG_RUN per week." This is the second major hard-gate chokepoint (alongside step 10's `V1FourDaySessionVolumeAllocationPolicy`), and it directly contradicts an upstream doc-comment claim in `CatalogRunLayoutSlots.cs` that role counts are "never hardcoded" — that claim only holds through slot *resolution* (step 8); calendar *materialization* (this step) re-hardcodes it downstream.
- Knows about: DaysPerWeek ✓ (hardcoded 4), CalendarDate ✓, SessionRole ✓.

**12. Schedule validation**
- `PhaseConstraintVerifier.Verify` (structural, phase-generic — checks `AllocatedWeeks` against bounds), `VolumeProgressionVerifier.Verify(volumePlan, policy)` (structurally generic, but the only `VolumeSafetyPolicy` ever passed anywhere in production is `.Default`), `GoalPaceReachabilityVerifier.Verify` (keyed on `weeklySlotRoles.Count`, genuinely parameterized on day count, no `Level` parameter at all — level is simply out of scope for this verifier).

**13. Preview materialization**
- `CatalogPreviewGenerator` (`RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`) assembles the above into a `CatalogPreviewSnapshot`, returned to the caller wrapped in `GeneratePreviewResponse`. Per `PlanServices.cs`'s own doc comment (lines 341-344), a catalog-sourced preview is **deliberately not confirmable as-is** through the legacy confirm path — it carries a `GenerationSource` marker (`"CATALOG"`, checked case-insensitively by `IsCatalogSourcedPreview`, `PlanServices.cs:1019`) that the confirm endpoint dispatches on.

**14. Confirm/persistence path**
- `PlansController.ConfirmPlan` → `PlanServices.ConfirmPlanAsync` (line 495) → detects `IsCatalogSourcedPreview` → delegates **all** validation/integrity/idempotency/persistence to `CatalogPlanConfirmationService` (a single, separate service; `PlanServices.cs` does not duplicate that logic inline).
- Persists `TrainingPlan` (with `ScheduleStrategy = StaticComplete`) plus `TrainingDay`/`TrainingWeek` rows. `TrainingDay`/`TrainingWeek` is a plain one-to-many FK relationship with no fixed-cardinality DB constraint (`TrainingPlan.DaysPerWeek` is a plain `int` column) — genuinely schema-parameterized at the persistence layer, even though the code layers above it (steps 10-11) are hard-gated to 4.

**15. Adaptation-related representation**
- **None, for this path.** A catalog/PreparationRunway-routed (8–20 week) TEN_K plan produces no representation Adaptation V1 ever reads — Adaptation V1's entire `LongHorizonRolling*` entity/service family is scoped to `PlanScheduleStrategy.RollingLongHorizon` plans only, which are generated by a structurally separate code path (`Schedule/LongHorizon/`, entry point `LongHorizonPublicPlanService`), not the one traced above. See §7 for the LongHorizon side's own generation-to-adaptation handoff.

---

## 2. Sources of truth

| Concept | Source of truth | Duplicate/competing authority? |
|---|---|---|
| Supported race distance | `GoalDistance` enum (`RunningApp.Domain.Enums`) + `CanonicalDistanceFamilyResolver.Resolve` (km→family thresholds, genuinely generic) + `V1CatalogPilotIdentityPolicy.GoalDistance` (hardcoded TenK gate) | Yes — the enum/resolver are generic, but the pilot-identity gate is a separate, narrower hardcoded check |
| Runner level | `RunningBackground` enum (4 values: Beginner/Intermediate/Advanced/Experienced) | `V1CatalogPilotIdentityPolicy.Level` hardcodes Intermediate; `PlanCatalogDomainMapper.Map` classifies the other 3 as `NotSupported` (no catalog content exists for them: `level-modifiers/` on disk contains only `intermediate-modifier.v1..v6.json`) |
| Days per week | Request DTO `int DaysPerWeek` (schema-generic) | `V1CatalogPilotIdentityPolicy.DaysPerWeek = 4` (hardcoded); re-implemented independently (not routed through the policy) in ≥3 files under `Schedule/LongHorizon/RollingActivation/` — **flagged duplicate authority**, see §8 item 6 |
| Preferred running days | Request DTO, validated generically (`Count == DaysPerWeek`) at intake, then re-validated as `!= 4` specifically downstream (`CatalogWeekSkeletonCalendarMaterializer.ValidatePreferredDays`, `LongHorizonCalendarAssigner.AssignWeekdays`) | Yes — generic at intake, hardcoded at materialization |
| Long-run preferred day | Request DTO field, passed through | No |
| Plan horizon / total week count | `RaceHorizonPolicy.CalculateAvailableWeeks` (single canonical authority, confirmed reused unchanged by both catalog and LongHorizon paths in prior-phase work) | No |
| 10K phase structure | Hardcoded required-set `{"FOUNDATION","BUILD","RACE_SPECIFIC","TAPER"}` in `PlanCatalogBundleLoader.ReadPhaseAllocations` | No |
| Phase week counts | Catalog JSON (`MinimumWeeks/PreferredWeeks/MaximumWeeks/...`), consumed by `CatalogPhaseAllocationResolver` | No |
| Weekly session-role cardinality | `run-layout-4d.v2.json` catalog file (data), but re-asserted as a hardcoded literal in `V1FourDaySessionVolumeAllocationPolicy` (code) and `CatalogWeekSkeletonCalendarMaterializer` (code) | **Yes — flagged.** Data says "whatever the layout file declares"; two independent code layers separately hardcode "exactly 1/2/1" |
| KEY/EASY/LONG counts | Same as above | Same as above |
| Workout eligibility by phase | `ProgressionStageAllocator` + catalog progression-stage definitions | No |
| Starting/peak weekly volume | `VolumeSafetyPolicy.Default` — single hardcoded record (24km start, 38km peak), commented as Intermediate/4-day-specific values, with no lookup-by-key mechanism | Partially — `CatalogPeakVolumeBandLoader` provides a genuine `(distanceFamily, experience, runsPerWeek)`-keyed clamp range, but the actual multiplier/curve shape comes from the single hardcoded policy, not that lookup |
| Weekly progression | `VolumeSafetyPolicy.Default` formula (linear interpolation to peak, TAPER exception) | No |
| Long-run progression | Same policy, `LongRunSelectionShare: 0.33` | No |
| Cutback/recovery behavior | TAPER-only special-case in `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` | No |
| Pace/intensity prescription | `PaceSourceResolver` + progression-stage-driven workout selection | No |
| Calendar-day placement | `CatalogWeekSkeletonCalendarMaterializer` | No |
| Hard-session separation | `MinimumKeySessionToLongRunSeparationDays` constant, reused generically by `ScheduleRepairSpacingValidator` (adaptation side) — not literally 4-gated | No |
| Schedule validation | `PhaseConstraintVerifier`, `VolumeProgressionVerifier`, `GoalPaceReachabilityVerifier`, `DatedGeneratedCatalogPlanSkeletonValidator` | No |
| Adaptation execution-summary role cardinality | `WindowExecutionSummary` record (`Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs`) — KEY/LONG as singular booleans, EASY as a count pair | N/A (single authority, but internally asymmetric — see §7) |

---

## 3. Intermediate semantics (evidenced, not assumed)

**C1 — Identification/routing only:**
- `V1CatalogPilotIdentityPolicy.Level` — one leaf of the flat 4-field identity gate (§1 step 4). No further logic branches on the enum value itself beyond this equality check.

**C2 — Selects data/constants:**
- `CatalogPeakVolumeBandLoader.LoadAsync(..., experience, ...)` — the **one** genuinely data-driven effect: matches `entries[]` on `(distanceFamily, experience, runsPerWeek)` to select a `minimumKm`/`maximumKm` clamp range. This is the strongest example in the whole codebase of Level behaving as an independent lookup axis rather than a bundled identity.
- `PlanCatalogBundleLoader.LoadCandidateAsync` reads the `"experience"` string off `level-modifiers/intermediate-modifier.vN.json` — but this file is only reachable at all once the identity gate (C1) has already passed, so it's C1-gated data selection, not an independent branch.

**C3 — Changes control flow/algorithm:**
- The identity gate itself (`IsSupportedIdentity`) is a control-flow fork: any non-Intermediate value routes the entire request to a different, legacy generation engine (`LivePlanPreviewRoute.Legacy`) rather than merely selecting different data within the same algorithm.

**C4 — Accepted but functionally ignored:**
- `PlanCatalogDomainMapper.Map` explicitly classifies Beginner/Advanced/Experienced as `NotSupported` with a doc comment noting they "have no catalog candidate content" — not silently ignored (they're rejected with a typed reason), but functionally dead weight for this pipeline. `VolumeSafetyPolicy.Default`'s formula itself never re-branches on level once the peak-volume clamp is applied (`ResolvePeak` computes its multiplier from `GoldenFixtureResolvedPeakKm/GoldenFixtureStartingVolumeKm`, both level-agnostic hardcoded constants, using the loaded band only as a clamp, not as the source of the curve shape).

**Net finding:** Intermediate has exactly one genuinely data-driven effect (peak-volume clamp, C2) and one dominant identity/routing effect (C3/C1 combined). Nothing downstream of candidate load (phase allocation, role binding, calendar assignment, spacing) re-branches on level. No workout-difficulty, pace-selection, or eligibility logic keys directly off `RunningBackground` anywhere in the traced pipeline.

---

## 4. 4D semantics (evidenced, not assumed)

| Check | Finding | Classification |
|---|---|---|
| Request validation requires exactly 4? | No — generic 1–7 bound, `PreferredDays.Count == DaysPerWeek` | PARAMETERIZED |
| Catalog identifier bakes in "4D"? | Yes — `V1CatalogPilotIdentityPolicy.DaysPerWeek = 4` const, part of `CandidateKey` | CONTROL-FLOW-SPECIALIZED |
| PreferredDays requires exactly 4? | Yes — `CatalogWeekSkeletonCalendarMaterializer.ValidatePreferredDays` and `LongHorizonCalendarAssigner.AssignWeekdays` both throw if `!= 4` | SCHEMA/CARDINALITY-SPECIALIZED |
| Weekly skeleton hardcodes 4 slots? | Code is generic (`CatalogRunLayoutResolver` reads `candidate.SlotRoles`, a variable-length list, validated against `candidate.DaysPerWeek` not a literal); only one layout file (`run-layout-4d.v2.json`) exists on disk | DATA-SPECIALIZED |
| Role cardinality 1K+2E+1L hardcoded in generation? | Yes — `V1FourDaySessionVolumeAllocationPolicy` throws unless exactly (4, 1, 2, 1); `FourDaySessionDistanceAllocationPolicy` has a hardcoded `2 *` multiplier and named (not list-based) fields | SCHEMA/CARDINALITY-SPECIALIZED + CONTROL-FLOW-SPECIALIZED |
| Calendar assignment assumes 4 sessions? | Yes — `LongHorizonCalendarAssigner.AssignWeekdays` uses a fixed dict `{LONG_RUN, KEY_SESSION, EASY_SUPPORT_1, EASY_SUPPORT_2}`, doc comment: "the existing 4-day race plan hard-constraint policy, unchanged" | CONTROL-FLOW/SCHEMA-CARDINALITY-SPECIALIZED |
| Spacing rules assume 4 specifically or generic N? | Generic — `MinimumKeySessionToLongRunSeparationDays` constant works for any N, never re-derived per N | DATA-SPECIALIZED (not literally 4-gated) |
| Validators assert exactly four? | Mixed — `GeneratedCatalogPlanSkeletonValidator.cs` checks `!= skeleton.DaysPerWeek` (PARAMETERIZED); `DatedGeneratedCatalogPlanSkeletonValidator.cs` checks literal `!= 4` (SCHEMA-CARDINALITY-SPECIALIZED); `LongHorizonStructuralValidator.cs` hardcodes `runwayCount != 8`, `coreCount != 12`, `OrderedWorkoutSlots.Count != 4`, plus 1/2/1 role counts (SCHEMA-CARDINALITY-SPECIALIZED, also distance-coupled) |
| Persistence assumes 4 TrainingDays/Week? | No — plain FK one-to-many, `TrainingPlan.DaysPerWeek` is a plain `int` column, no fixed-cardinality DB constraint | PARAMETERIZED (schema level) |
| Preview DTOs assume four? | No literal `==4` found; pass-through of whatever upstream produced | PARAMETERIZED (contingent) |
| Adaptation side matches generation-side cardinality? | Partial — `WindowExecutionSummary` represents EASY as a count pair (already role-count-generalized) but KEY/LONG as singular booleans (assumes exactly one KEY, one LONG per structural week — cannot represent 2 KEY slots without a breaking schema change) | SCHEMA/CARDINALITY-SPECIALIZED (partial — EASY only) |
| Tests encode "four-session" as domain invariant? | Yes, pervasively — ~90+ test files reference "FourDay"/"4D"; production type names themselves (`V1FourDaySessionVolumeAllocationPolicy`, `FourDaySessionDistanceAllocationPolicy`) are self-documenting | n/a |

**Notable dark exception:** `DynamicCoreWeekSkeletonOrchestrator.cs` derives `DaysPerWeek = runLayout.StructuralRoles.Count` (line 155) — genuinely dynamic — but is explicitly documented (lines 78-81) as having **zero production call sites**. A more general path exists in the codebase but is not wired into any live request today.

**Duplicate-authority finding:** the combined `Level != Intermediate || DaysPerWeek != 4` gate is re-implemented independently (not routed through `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`, despite that type's own doc comment declaring itself "the single owner of that mapping") in at least 3 separate files under `Schedule/LongHorizon/RollingActivation/`: `LongHorizonRollingCheckpointRuntime.cs:368`, `LongHorizonRollingInitialActivationContracts.cs:105`, `LongHorizonPublicPlanService.cs:354`. The de-duplication effort documented in `V1CatalogPilotIdentityPolicy.cs` was not fully propagated to the LongHorizon subsystem.

---

## 5. Distance/Level/Frequency coupling map

| Component | Dependency shape | Evidence | Why |
|---|---|---|---|
| `V1CatalogPilotIdentityPolicy` | **DISTANCE_LEVEL_FREQUENCY (monolithic/bundled)** | `CandidateKey = "TEN_K__4D__INTERMEDIATE"`, one opaque const; `IsSupportedIdentity` ANDs all three | The routing layer fuses all three axes into one flat identity gate — this is the architecture's central finding |
| `CanonicalDistanceFamilyResolver` | DISTANCE_ONLY / GENERIC | km→family threshold mapping, independent of level/frequency | Genuinely generic, but output feeds the monolithic gate above |
| `PlanCatalogBundleLoader`/`CatalogArtifactFileResolver` | GENERIC (mechanically) | Generic `(documentType, key, version)` loader | Only ever invoked with the one hardcoded key in practice |
| `CatalogPeakVolumeBandLoader` | **DISTANCE_LEVEL_FREQUENCY as independent lookup keys** | `LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct)` — 3 separate matched parameters against a flat `entries[]` | The one component treating the three axes as genuinely independent — a template for how generalization could work |
| `CatalogPhaseAllocationResolver` | GENERIC | Driven by catalog `MinimumWeeks/PreferredWeeks/MaximumWeeks/...` | No distance/level/frequency literal anywhere |
| `CatalogRunLayoutResolver` | FREQUENCY_ONLY (parameterized) | Validates `roles.Count == candidate.DaysPerWeek`, data-driven | Generic code, singleton data |
| `V1FourDaySessionVolumeAllocationPolicy`/`FourDaySessionDistanceAllocationPolicy` | FREQUENCY_ONLY (hardcoded) | Literal 4/1/2/1 checks, `2 *` multiplier | Type names themselves are frequency-specific |
| `CatalogVolumeAndLongRunPlanner`/`VolumeSafetyPolicy` | GENERIC algorithm wrapping a DISTANCE_LEVEL_FREQUENCY-specific singleton constant set | `.Default` is the only instance ever constructed in production | "Golden Fixture" numbers are hardcoded, not looked up |
| `CatalogWeekSkeletonCalendarMaterializer` | FREQUENCY_ONLY (hardcoded) | `DaysPerWeek != 4` throw, `SessionSlots.Count != 4` throw | The single most explicit "requires exactly 4" gate |
| `LongHorizonCalendarAssigner` | FREQUENCY_ONLY (hardcoded) | Fixed 4-key dict | Doc comment: "unchanged 4-day hard-constraint policy" |
| `LongHorizonStructuralValidator` | DISTANCE_FREQUENCY (hardcoded) | `runwayCount != 8`/`coreCount != 12` (TEN_K-specific) + frequency 4/1/2/1 checks | Combined coupling |
| `GoalPaceReachabilityVerifier` | DISTANCE_ONLY | Gates `GOAL_PACE_TEN_K` specifically | No level/frequency literal |
| `TenKPreparationRunway*PolicyFactory` family (6 types) | DISTANCE_LEVEL_FREQUENCY (hardcoded by type name) | Docstrings: "policy metadata for TEN_K__4D__INTERMEDIATE" | One-factory-per-fixed-candidate, not parameterized |

**Candidate-key construction — direct answer:** no `(distance, level, days) → candidateKey` resolver exists, dark or live. The key is one hand-authored constant matching one hand-authored catalog file (`plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`). The loading *mechanism* is generic; nothing programmatically *constructs* a key from the three-dimension tuple.

**Catalog data-file inventory (ground truth, confirms the architecture is currently a monolithic single-variant system despite generic-looking code in several layers):** `combinations/`: only `ten-k-4d-intermediate.v1..v10.json`. `templates/`: only `ten-k-master.v1..v6.json`. `level-modifiers/`: only `intermediate-modifier.v1..v6.json`. `layouts/`: only `run-layout-4d.v1..v2.json`. Every axis has exactly one populated value in the source-of-truth catalog data today, independent of what the code could theoretically support.

**Real note on catalog "liveness":** the actual `TEN_K__4D__INTERMEDIATE.v10` combination document's `metadata.status` is `"DRAFT"` — the catalog engine is not "live" outside dev/test without an explicit `LocalCatalogAcceptance` override (`CatalogCandidateEligibilityGate`).

---

## 6. Phase/prescription boundary

Four catalog-declared phases exist as a hardcoded required set (`PlanCatalogBundleLoader.ReadPhaseAllocations`): `FOUNDATION`, `BUILD`, `RACE_SPECIFIC`, `TAPER`.

| Boundary | Separation | What concretely changes across phases |
|---|---|---|
| Plan horizon vs. phase structure | Separate | `RaceHorizonPolicy` (horizon) is phase-agnostic; `CatalogPhaseAllocationResolver` (phase weeks) consumes the horizon as an input |
| Weekly session structure vs. phase | Separate | Role cardinality (1K+2E+1L) is constant across all phases — no phase changes session count/roles |
| Workout content vs. phase | Partially separated, mediated through progression stage | `ProgressionStageAllocator.AllocatePhase` allocates a different `ProgressionStageKey` per phase (e.g. RACE_SPECIFIC → `GOAL_PACE_REHEARSAL`, gated by `GOAL_FEASIBILITY_IN`, falling back to `CURRENT_FITNESS_SPECIFIC_REHEARSAL`); actual workout selection flows through that stage, not a direct phase→workout table |
| Volume/distance vs. phase | Conflated for FOUNDATION/BUILD/RACE_SPECIFIC, separated for TAPER | `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` special-cases only `TAPER` by literal string check (multiplicative decay via `taper.Multiplier`); the other three phases share one linear-interpolation-to-peak formula with no distinguishing logic between them |
| Pace/intensity vs. phase | Mediated through progression stage, same as workout content | No separate pace-per-phase table found |
| Calendar placement vs. phase | Separate | `CatalogWeekSkeletonCalendarMaterializer` operates identically regardless of phase |

**Direct answer to "what concretely changes across a phase transition today":** week-count allocation (data-driven) and progression-stage/workout-eligibility selection (data-driven, mediated by `ProgressionStageAllocator`), plus — for TAPER only — a distinct volume-progression rule. FOUNDATION, BUILD, and RACE_SPECIFIC are otherwise **not** distinguished by separate code branches; they share the same volume formula and only differ via whatever progression stage each is data-configured to allocate.

---

## 7. Adaptation V1 dependency baseline

**Scope reminder:** Adaptation V1 only ever operates on `RollingLongHorizon` plans (§1's routing split) — it never sees a catalog/PreparationRunway-generated (8–20 week) plan. This section documents what the LongHorizon generation side hands to Adaptation V1, and what Adaptation V1's own components assume about that structure, as already established across the extensive prior-phase work in this repository (4M.1–4M.5C) and reconfirmed here at the generation boundary.

- **Structural weeks are explicit and persisted** (`LongHorizonRollingWeekState.GlobalWeek`/`StructuralStartDate`/`StructuralEndDate`), independent of calendar weekday assumptions — confirmed unchanged.
- **Every real structural week the LongHorizon generation side produces has exactly 1 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN**, per `LongHorizonGeStructuralSelector.BuildDescriptor`'s unconditional role-dictionary construction (confirmed in prior-phase deep reads, still true — no change found here) — this is the LongHorizon-side counterpart to §4's catalog-side 1K+2E+1L hardcoding; both sides independently hardcode the same shape rather than sharing one authority.
- **`WindowExecutionSummaryBuilder`** (`Schedule/LongHorizon/Adaptation/WindowExecutionSummaryBuilder.cs`) is the sole adherence/completion authority. It assumes: at most one KEY_SESSION and one LONG_RUN per evaluated evidence set (`KeySessionCompleted`/`LongRunCompleted` are booleans, AND-reduced across however many occurrences of that role are actually present — see prior-phase 4M.5A/4M.5B/4M.5C work for the full multi-occurrence analysis), and an arbitrary *count* of EASY_SUPPORT (`EasyExpectedCount`/`EasyCompletedCount` are integers, not booleans). This asymmetry (Key/Long singular-boolean vs. Easy count-pair) is a real, evidenced schema constraint: a future role layout with 2 KEY_SESSION slots per week could not be represented by this contract without a breaking change.
- **`ScheduleRepairPolicy`/`SubstituteFutureEasy`** assume the existence of at least one EASY_SUPPORT slot to substitute into (the mechanism's entire premise is "priority session may replace a future EASY_SUPPORT slot") — if a future frequency generalization produced a role layout with zero EASY_SUPPORT slots, this repair path would have no substitution target (would fall through to `Skip`, per the existing `ScheduleRepairPolicy` fallback rule — not a crash, but a silent behavior narrowing).
- **`WeeklyWindowPartitioner`** (Phase 4M.5C, `Schedule/LongHorizon/Adaptation/WeeklyLoadDecisionAggregation.cs`) partitions by **structural week identity** (`WeekStateId`), not by any fixed session count — this component is already frequency-agnostic by construction (it groups whatever sessions exist per week, regardless of how many).
- **`NextWindowLoadDecisionPolicy`** (unchanged since Phase 4M.1) is explicitly, by its own doc comment, "calibrated for the current 4-session pilot... not a general formula" — its raw-count thresholds (0/1/2/3/≥4) assume exactly 4 sessions per evaluated week. This is the most load-bearing frequency assumption in the entire adaptation decision chain.
- **Missed-session handling / preferred-slot future-slot behavior**: `ScheduleRepairCandidateProvider` is window-scoped (not week-scoped) and phase-boundary-constrained (`PhaseBoundaryConstraint`), independent of frequency — confirmed unchanged, no new frequency assumption found here beyond the EASY-slot-availability point above.

---

## 8. Hard-coding inventory (consolidated)

| ID | File / Type / Method | Dimension | Current assumption | Representation | Classification | Downstream consumers |
|---|---|---|---|---|---|---|
| H1 | `V1CatalogPilotIdentityPolicy.cs` (const fields + `IsSupportedIdentity`) | Distance, Level, Frequency | Exactly TenK+Intermediate+4 | constant + boolean AND | CONTROL-FLOW-SPECIALIZED | `LivePlanPreviewRouting.cs` (routing gate) |
| H2 | `FourDaySessionDistanceAllocationPolicy.Allocate` | Frequency, Role cardinality | Exactly 1 KEY + 2 EASY + 1 LONG | named record fields + `2 *` literal | SCHEMA/CARDINALITY-SPECIALIZED | `CatalogSessionPrescriptionPlanner` |
| H3 | `V1FourDaySessionVolumeAllocationPolicy.Allocate` | Frequency, Role cardinality | `sessions.Count==4 \|\| KEY==1 \|\| EASY==2 \|\| LONG==1` | literal-guarded exception | CONTROL-FLOW-SPECIALIZED | `CatalogSessionPrescriptionPlanner.Build` (unconditional call site) |
| H4 | `CatalogWeekSkeletonCalendarMaterializer.ValidateSkeletonRoleStructure`/`ValidatePreferredDays` | Frequency, Calendar, Role cardinality | `DaysPerWeek==4`, `SessionSlots.Count==4`, role counts 1/2/1, `PreferredDays.Count==4` | literal-guarded exceptions | CONTROL-FLOW-SPECIALIZED | Calendar assignment step (§1 step 11) |
| H5 | `LongHorizonCalendarAssigner.AssignWeekdays` | Frequency, Calendar | Fixed dict `{LONG_RUN, KEY_SESSION, EASY_SUPPORT_1, EASY_SUPPORT_2}` | fixed dictionary keys | SCHEMA/CARDINALITY-SPECIALIZED | LongHorizon generation side |
| H6 | `LongHorizonStructuralValidator.cs` | Distance, Frequency, Role cardinality | `runwayCount!=8`, `coreCount!=12`, `Count!=4`, 1/2/1 | literal-guarded exceptions | SCHEMA/CARDINALITY-SPECIALIZED | LongHorizon structural materialization |
| H7 | `VolumeSafetyPolicy.Default` | Distance, Level, Frequency | Single hardcoded record (24km start, 38km peak, 0.53 taper, 0.30-0.36 long-run share) | constant record instance | DATA-SPECIALIZED | `CatalogVolumeAndLongRunPlanner`, `VolumeProgressionVerifier` |
| H8 | `CatalogVolumeAndLongRunPlanner` exception/provenance strings (e.g. `CatalogLongRunHardCapViolationException`, `"four_day_long_run_preferred_share_30_to_36_percent..."`) | Frequency | "four-day" baked into user/decision-trace-visible text | string literal | CONTROL-FLOW-SPECIALIZED | Response payload / decision trace |
| H9 | `WindowExecutionSummary` record | Role cardinality (adaptation side) | KEY/LONG singular booleans, EASY count pair | schema/type | SCHEMA/CARDINALITY-SPECIALIZED (partial) | `NextWindowLoadDecisionPolicy`, all Adaptation V1 consumers |
| H10 | `NextWindowLoadDecisionPolicy.DetermineLoadDecision` | Frequency (adaptation side) | 0/1/2/3/≥4 raw-count thresholds calibrated for exactly 4 sessions/week | switch statement | CONTROL-FLOW-SPECIALIZED (self-disclosed in its own doc comment) | Weekly aggregation (Phase 4M.5C), numeric anchor selection |
| H11 | `LongHorizonRollingCheckpointRuntime.cs:368`, `LongHorizonRollingInitialActivationContracts.cs:105`, `LongHorizonPublicPlanService.cs:354` | Level, Frequency | `Level != Intermediate \|\| DaysPerWeek != 4`, independently re-implemented 3×, not routed through H1 | inline boolean guard | CONTROL-FLOW-SPECIALIZED (duplicated authority) | Respective LongHorizon activation/eligibility paths |
| H12 | `RaceCoreSupportRegistry` | Distance, Level, Frequency (schema) | Registry keyed only by week-count; no compound key for (candidate, weekCount) | record schema | SCHEMA-SPECIALIZED (for D/L/F axes; DATA-SPECIALIZED for week-count, which already varies 9-14) | Core-generation safety verification |

---

## 9. Existing dynamic seams

| Seam | What is already dynamic | What currently constrains it |
|---|---|---|
| `CatalogPeakVolumeBandLoader.LoadAsync` | Genuine composite-key lookup on `(distanceFamily, experience, runsPerWeek)` | Data — only entries for the Intermediate/4-day combination are known to exist (band JSON file contents not verified in this audit) |
| `CatalogRunLayoutResolver.Resolve` | Reads `candidate.SlotRoles` as a variable-length list, validates against `candidate.DaysPerWeek`, never hardcodes the 4-slot shape itself | Data — only `run-layout-4d.v2.json` exists on disk |
| `CatalogStageToWeekMaterializer.Materialize` | `DaysPerWeek` is a required input field of its context type, builds a variable-length `slots` collection keyed per role string, loops `for i < RunLayoutSlotRoles.Count` | None found in this file itself — genuinely generic code; doc comment explicitly frames its own `DaysPerWeek` field as "e.g. 4 for the pilot," not a constraint |
| `PhaseConstraintVerifier.Verify` | Purely structural (`AllocatedWeeks` vs. bounds, sum vs. target); no DaysPerWeek/Level parameter needed | N/A — genuinely general with respect to week-count target |
| `GoalPaceReachabilityVerifier.Verify` | Takes `weeklySlotRoles` as a list, derives `DaysPerWeek = weeklySlotRoles.Count` | None — Level is simply out of scope for this verifier (not specialized, not applicable) |
| `VolumeProgressionVerifier.Verify` | Takes `VolumeSafetyPolicy` as an explicit constructor/method parameter, not inline | Validation — no production call site ever constructs anything other than `VolumeSafetyPolicy.Default` |
| `DynamicCoreSessionPrescriptionOrchestrator`/`DynamicCoreVolumeAndLongRunOrchestrator` | `TargetWeekCount` genuinely generalizes to "any mathematically feasible standalone-core target week count (8-14)" per its own doc comment | DaysPerWeek/role cardinality still flow implicitly through the one candidate summary always loaded |
| `DynamicCoreWeekSkeletonOrchestrator` | `DaysPerWeek = runLayout.StructuralRoles.Count` — genuinely derived, not hardcoded | **Zero production call sites** — this seam exists in the codebase but is not reachable from any live request today |
| `TrainingDay`/`TrainingWeek`/`TrainingPlan.DaysPerWeek` persistence schema | Plain FK one-to-many, plain `int` column, no fixed-cardinality DB constraint | None at the schema level — the constraint, where it exists, is entirely in the C# layers above it |
| `WeeklyWindowPartitioner` (adaptation side, Phase 4M.5C) | Groups sessions by structural-week identity, not by any fixed session count | None — already frequency-agnostic by construction |

**This section exists to counterbalance §8**: the system is not uniformly hardcoded. Several layers (peak-volume lookup, run-layout resolution, phase constraint verification, the week-skeleton materializer's own internal loop, week-partitioning on the adaptation side) are already written generically; the actual constraint today is almost entirely **catalog data availability** (only one candidate/layout/level-modifier file exists) plus a small number of **explicit, load-bearing control-flow gates** (H1, H3, H4, H10, H11) that would need real code changes, not just new data files, to generalize past.

---

## 10. Final architecture summary

```
API (PlansController.cs)
  → request validation (GenerateRacePlanPreviewRequestValidator — generic)
  → command mapping (GeneratePreviewCommandMapper)
  → horizon classification (RaceHorizonPolicy.CalculateAvailableWeeks — generic)
      ├─ 8-14 weeks  → catalog "core" path (PlanServices.GenerateCatalogPreviewAsync)
      ├─ 15-20 weeks → PreparationRunway path (PlanServices.GeneratePreparationRunwayPreviewAsync)
      └─ 21+ weeks   → rejected; separate LongHorizon endpoint (structurally independent pipeline)
  → pilot identity gate (V1CatalogPilotIdentityPolicy.IsSupportedIdentity — MONOLITHIC D+L+F bundle)
  → catalog candidate load (PlanCatalogBundleLoader — generic loader, singleton data)
  → runtime-condition resolution (RuntimeCatalog/Resolvers/* — generic, level-agnostic)
  → phase allocation (CatalogPhaseAllocationResolver — generic, hardcoded 4-phase-name set)
  → weekly skeleton / run-layout resolution (CatalogRunLayoutResolver — GENERIC)
  → workout/role binding (V1CatalogWorkoutRoleBindingPolicy + ProgressionStageAllocator)
  → volume/distance prescription (VolumeSafetyPolicy.Default [HARDCODED] → FourDaySessionDistanceAllocationPolicy [HARDCODED 1K+2E+1L] → V1FourDaySessionVolumeAllocationPolicy [HARD GATE])
  → calendar assignment (CatalogWeekSkeletonCalendarMaterializer — HARD GATE: DaysPerWeek==4, roles==1/2/1)
  → schedule validation (PhaseConstraintVerifier/VolumeProgressionVerifier/GoalPaceReachabilityVerifier — mostly GENERIC)
  → preview materialization (CatalogPreviewGenerator → GeneratePreviewResponse, GenerationSource="CATALOG")
  → confirm/persistence (CatalogPlanConfirmationService → TrainingPlan[StaticComplete]/TrainingDay/TrainingWeek — schema GENERIC)
  → [no adaptation consumption for this path]

(separate, structurally independent LongHorizon path:
  LongHorizonPublicPlanService → Schedule/LongHorizon/* generation
  → LongHorizonRolling* persistence [RollingLongHorizon]
  → Adaptation V1 (WeeklyWindowPartitioner [GENERIC] → WindowExecutionSummaryBuilder [PARTIAL cardinality] → NextWindowLoadDecisionPolicy [HARDCODED 4-session calibration] → WeeklyLoadDecisionAggregator))
```

### Answers to the required questions

1. **Is 10K/Intermediate/4D one monolithic variant, or independent axes?** Both, depending on layer. The **routing/identity layer** (`V1CatalogPilotIdentityPolicy`) and the **catalog data files on disk** (one combination, one template, one level-modifier, one layout) are monolithic — a single bundled variant. Several **algorithmic layers beneath that gate** (`CatalogRunLayoutResolver`, `CatalogPeakVolumeBandLoader`, `CatalogStageToWeekMaterializer`, `PhaseConstraintVerifier`) are already written as if the three axes were independent. The system today is best described as **a monolithic identity gate sitting in front of a partially-axis-independent engine**, not a uniformly hardcoded system and not a uniformly generic one.

2. **Most deeply hardcoded dimension:** **Frequency (DaysPerWeek/role cardinality)**. It is hardcoded independently in at least 6 distinct places (H2-H6, H9-H10) across both the catalog and LongHorizon/adaptation sides, including two separate schema-level cardinality assumptions (the FourDay* record shapes, and `WindowExecutionSummary`'s Key/Long booleans).

3. **Least hardcoded / closest to already dynamic:** **Distance**, via `CanonicalDistanceFamilyResolver` (genuine threshold-based classification, `{FiveK, TenK, HalfMarathon, Marathon}`, no level/frequency coupling) and `CatalogPeakVolumeBandLoader`'s composite key. Distance is the one axis with a resolver that would plausibly already work for a different distance value if the corresponding catalog data existed.

4. **Where is weekly role cardinality actually established?** Data-wise, in `run-layout-4d.v2.json` (read genuinely dynamically by `CatalogRunLayoutResolver`). But it is **re-asserted as a hardcoded literal** at two independent downstream code layers regardless of what that data file says: `V1FourDaySessionVolumeAllocationPolicy` (prescription) and `CatalogWeekSkeletonCalendarMaterializer` (calendar). On the LongHorizon/adaptation side, it is separately, independently hardcoded a third time in `LongHorizonGeStructuralSelector.BuildDescriptor` and a fourth time (partially) in `WindowExecutionSummary`'s schema.

5. **Where is Intermediate-specific prescription actually established?** `VolumeSafetyPolicy.Default` (the single hardcoded numeric record) is the true source of Intermediate-specific volume/progression numbers — not a lookup keyed by level, just a comment-documented association. `CatalogPeakVolumeBandLoader` is the only place Level is used as a genuine lookup key, and it only supplies a clamp range, not the underlying curve.

6. **Where does the system first become aware of calendar weekdays?** `CatalogWeekSkeletonCalendarMaterializer` (catalog path) / `LongHorizonCalendarAssigner` (LongHorizon path) — both are also where the hard 4-day/role-count gates live, meaning calendar-awareness and frequency-hardcoding are currently the same architectural boundary, not separate ones.

7. **Earliest architectural boundary affected by making Frequency dynamic:** `V1FourDaySessionVolumeAllocationPolicy`/`FourDaySessionDistanceAllocationPolicy` (prescription/session layer) — this is upstream of calendar assignment and is where the hardest, most literal cardinality gate lives (`sessions.Count==4 && ...`), thrown unconditionally by the one caller (`CatalogSessionPrescriptionPlanner.Build`) with no alternate path.

8. **Earliest architectural boundary affected by making Level dynamic:** `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` (the routing gate itself) — since Level is checked earliest, at the point where the request is first classified as pilot-eligible or routed to the legacy engine, before any catalog data is even loaded.

9. **Duplicate/conflicting sources of truth to understand before generalization:**
   - The `Level != Intermediate || DaysPerWeek != 4` identity guard is independently re-implemented in ≥3 files instead of routing through `V1CatalogPilotIdentityPolicy` (§8 H11) — a concrete, fixable inconsistency that predates any generalization work.
   - Role cardinality (1K+2E+1L) is asserted independently in at least 4 places across two subsystems (catalog prescription, catalog calendar, LongHorizon structural selector, adaptation schema) rather than flowing from one shared authority.
   - `CatalogRunLayoutSlots.cs`'s own doc comment ("role counts are never hardcoded") is contradicted by `CatalogWeekSkeletonCalendarMaterializer` one layer downstream — a documentation/reality mismatch worth resolving before anyone relies on the doc comment's claim during generalization design.

---

## 11. Files inspected

**Directly read by the orchestrating session:** `V1CatalogPilotIdentityPolicy.cs`, `PlansController.cs` (routing section), `PlanServices.cs` (`GeneratePreviewAsync`/`GenerateCatalogPreviewAsync`/`GeneratePreparationRunwayPreviewAsync`/`ConfirmPlanAsync`/`IsCatalogSourcedPreview` signatures and surrounding context).

**Read in full or in substantial part by dispatched research passes (all citations in §§1-9 above trace to files actually read, not inferred from names):** `RunningApp.Api/Controllers/PlansController.cs`; `RunningApp.Application/DTOs/Plan/GenerateRacePlanPreviewRequest.cs`, `GeneratePreviewResponse.cs`; `RunningApp.Application/Validation/GenerateRacePlanPreviewRequestValidator.cs`; `RunningApp.Application/Commands/Plan/GeneratePreviewCommandMapper.cs`; `RunningApp.Application/Services/PlanServices.cs`; `RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`, `V1LiveCatalogPilotRoutingPolicy.cs`, `LivePlanPreviewRoutingService.cs`, `CatalogPlanConfirmationService.cs`, `CatalogPreviewGenerator.cs`; `RunningApp.Application/RuntimeCatalog/Resolvers/TimeAdequacyResolver.cs`, `PaceSourceResolver.cs`, `CoreEntryReadinessResolver.cs`, `GoalFeasibilityResolver.cs`; `RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocationResolver.cs`, `GeneratedCatalogPlanSkeleton.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`, `PhaseConstraintVerifier.cs`, `VolumeProgressionVerifier.cs`, `GoalPaceReachabilityVerifier.cs`, `CatalogRunLayoutSlots.cs`, `CatalogStageToWeekMaterializer.cs`, `RaceCoreSupportRegistry.cs`; `RunningApp.Application/RuntimeCatalog/Schedule/Binding/CatalogWorkoutBinder.cs`, `V1CatalogWorkoutRoleBindingPolicy.cs`; `RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs`, `CatalogPeakVolumeBandLoader.cs`, `VolumeSafetyPolicy.cs`; `RunningApp.Application/RuntimeCatalog/Prescription/Session/FourDaySessionDistanceAllocationPolicy.cs`, `V1FourDaySessionVolumeAllocationPolicy.cs`, `CatalogSessionPrescriptionPlanner.cs`, `DynamicCoreSessionPrescriptionOrchestrator.cs`; `RunningApp.Common/RaceHorizonPolicy.cs`, `RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs`; `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/LongHorizonCalendarAssigner.cs`, `LongHorizonStructuralValidator.cs`, `LongHorizonStructuralMaterializer.cs`, `LongHorizonGeStructuralSelector.cs` (prior-phase knowledge, reconfirmed); `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonRollingInitialActivationContracts.cs`, `LongHorizonRollingCoreGenerationInputAdapter.cs`; `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/PublicPreview/LongHorizonPublicPlanService.cs`; `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/Persistence/LongHorizonFutureCoreRefreshOrchestrator.cs`, `LongHorizonRollingRestartContinuationService.cs`, `LongHorizonRollingStateRepository.cs`; `RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs`, `WindowExecutionSummaryBuilder.cs`, `NextWindowLoadDecisionPolicy.cs`, `WeeklyLoadDecisionAggregation.cs`, `ScheduleRepairCandidateProvider.cs`, `ScheduleRepairPolicy.cs` (prior-phase knowledge, reconfirmed); `RunningApp.Domain/Enums/RunningBackground.cs`, `GoalDistance.cs`; `RunningApp.Domain/Entities/TrainingPlan.cs`, `TrainingDay.cs`, `LongHorizonRollingWeekState.cs`, `LongHorizonRollingSessionState.cs`; plan-catalog data directory structure (`combinations/`, `templates/`, `level-modifiers/`, `layouts/`) file listings.

---

## 12. Final classification

```
10K_GENERALIZATION_CURRENT_STATE_BASELINE_READY
```

No production code was written or modified. No future generalized architecture was designed. No Beginner/Advanced/Experienced or 3D/5D/6D/7D values were proposed.
