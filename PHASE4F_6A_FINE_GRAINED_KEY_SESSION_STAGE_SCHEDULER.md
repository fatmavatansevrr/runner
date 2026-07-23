# Phase 4F.6A — Fine-Grained KEY_SESSION Stage Scheduler Contract and Deterministic Allocation

Implements the internal dark scheduler that assigns the current workout-progression stages to concrete plan weeks for the progression-controlled `KEY_SESSION`. Does not bind workout definitions to structural roles (Phase 4F.6B), does not implement prescription (Phase 4F.7), and does not change any public output, persistence, or hash behavior.

## Contract

New files under `backend/RunningApp.Application/RuntimeCatalog/Schedule/Progression/`:

- **`CatalogWorkoutProgressionDefinition.cs`** — backend-side parse of a WORKOUT_PROGRESSION document's fine-grained stage content (`CatalogWorkoutProgressionDefinition` → `CatalogPhaseWorkoutProgression` → `CatalogWorkoutProgressionStage`, plus `CatalogRuntimeEligibilityCondition`, `CatalogStageCompressionBehavior`, `CatalogStageExtensionBehavior`). Deliberately independent of plan-catalog's own C# enums (backend has no project reference onto `PlanCatalog.*` assemblies — see `PlanCatalogBundleLoader`'s own raw-JSON-parsing precedent). Carries **no workout identity**.
- **`CatalogWorkoutProgressionLoader.cs`** — `ICatalogWorkoutProgressionLoader`/`CatalogWorkoutProgressionLoader`, a new, small, single-purpose loader (mirroring `ICatalogPhaseAllocationResolver`/`ICatalogRunLayoutResolver`'s established pattern) rather than a method added to `IPlanCatalogBundleLoader` — growing that interface would force every existing implementer (including a test `FakeBundleLoader`) to add a matching method for a capability only the scheduler needs.
- **`ProgressionStageScheduleContracts.cs`** — output: `GeneratedCatalogStageSchedule`, `ScheduledProgressionWeek`, `StageAllocationDecisionTrace`, `StageAllocationDecisionTraceStep`, and enums `ProgressionStageEligibilityOutcome`, `ProgressionStageAllocationKind`, `ProgressionPhaseCompressionAction`.
- **`ProgressionStageSchedulingExceptions.cs`** — 12 typed exceptions, one per distinct rejection in the spec.
- **`ProgressionStageAllocator.cs`** — `IProgressionStageAllocator`/`ProgressionStageAllocator`, the deterministic algorithm.
- **`GeneratedCatalogStageScheduleValidator.cs`** — `IGeneratedCatalogStageScheduleValidator`/`GeneratedCatalogStageScheduleValidator`, output validation.

### Naming: `PhaseKey` vs. `ProgressionStageKey`

Every existing Phase 4F.1–4F.5.1 contract's `StageKey` field is **phase granularity** (e.g. `"BUILD"`, `"TAPER"`) — explicitly documented terminology debt (`CatalogStageToWeekContextFactory.Create`'s own doc comment: *"The values themselves are NOT workout-selection `stageKey`s... only the C# property name differs"*). This phase introduces the first backend field for the catalog's **fine-grained** stage identity (`"TAPER_SHARPEN"`, `"GOAL_PACE_REHEARSAL"`, etc.), explicitly named `ProgressionStageKey` throughout every new type, never `StageKey` — no existing field was silently repurposed.

### Required scheduled-week fields (Section 6)

`ScheduledProgressionWeek`: `WeekNumber`, `PhaseKey`, `ProgressionStageKey` (effective), `RequestedProgressionStageKey`/`FallbackOrigin` (original, if a fallback occurred), `StageRelativeOrder`, `StructuralRole` (always the literal `"KEY_SESSION"` — D-C06, not derived from anything else), `ConditionOutcome`, `AllocationReason`. No workout-identity field exists on this type at all.

## Allocation algorithm

Per phase (grouping the phase/week skeleton's weeks by their phase key):

1. **Structural validation** — reject missing/blank `ProgressionStageKey`, duplicate keys, duplicate `RelativeOrder` within a phase (`ProgressionStageDuplicateOrMissingKeyException`/`ProgressionStageDuplicateRelativeOrderException`). Reject phase/skeleton mismatches in either direction (`ProgressionStagePhaseMismatchException`).
2. **Top-level requested stages** — a phase's own stages, minus any stage that is *only ever referenced as another stage's `FallbackStageKey`* (e.g. `CURRENT_FITNESS_SPECIFIC_REHEARSAL` is never independently requested — it exists solely as `GOAL_PACE_REHEARSAL`'s documented fallback, per `AUD-012`). Without this exclusion, a fallback-only stage would be double-scheduled alongside its own trigger.
3. **Eligibility + fallback resolution** (never re-evaluates a runtime condition — only consults the already-resolved `RuntimeConditionResolutionResult` list): `NotConditioned` (no `Requires`) / `Eligible` / `IneligibleWithFallback` / `IneligibleWithoutFallback` (→ immediate typed rejection). Fallback chains are walked generically (not just one hop), with cycle detection (`ProgressionStageFallbackCycleException`), unknown-target rejection (`ProgressionStageFallbackTargetNotFoundException`), and ambiguous-target rejection (`ProgressionStageAmbiguousFallbackException`, defensive — unreachable via the current catalog's genuinely single `FallbackStageKey` field once structural duplicate-key validation has run).
4. **Duplicate-effective-stage merge** (Section 12) — two distinct merge rules, chosen by scenario:
   - **Single substitution** (the only shape in the current v10 catalog: one requested stage falls back to one target): merged bounds = **max of minimums, max of maximums** between the target's own declared range and the original requested stage's range. Rationale: a fallback exists to preserve the *original* stage's training intent using a substitute — it should never be more restrictive than what either source already explicitly sanctioned.
   - **True convergence** (two or more distinct requested stages independently resolve to the same target — a genuine competing-constraints scenario, not currently present in v10 but explicitly required by the spec): merged bounds = **max of minimums, min of maximums** (conservative — never promises more than the *more* restrictive of the two intents). If `mergedMin > mergedMax`, throws `ProgressionStageFallbackBoundsUnreconcilableException`.
5. **Phase capacity — compression** (Section 10): if `sum(active minimums) > availableWeeks`, reduce `Compressible` stages only (`Protected` stages are never touched — a hard, explicit gate, matching the section's own "reduce ONLY stages whose behavior PERMITS reduction" wording), down to a floor of **1 exposure** (no new "compressed minimum" field is invented — 1 is the literal floor implied by "reduce" against the only fields the catalog declares). Tie-break: highest `RelativeOrder` first, then `ProgressionStageKey` ordinal. Insufficient headroom → `ProgressionPhaseCapacityInsufficientException`.
6. **Phase capacity — extension** (Section 11): if `availableWeeks > sum(active minimums)`, distribute the surplus. Tie-break: **`Extendable` stages before `FixedExposure` stages** (a three-level *sort*, not a binary filter — contrast compression's explicit hard gate above), then highest `RelativeOrder`, then `ProgressionStageKey` ordinal, greedily filling each candidate's own headroom (`Maximum − Minimum`) before moving to the next. A `FixedExposure` stage is **deprioritized**, not excluded — it can still absorb surplus up to its own declared `Maximum` once every `Extendable` candidate's headroom is exhausted. Insufficient combined headroom → `ProgressionPhaseCapacityExceedsMaximumException`.
7. **Contiguous block layout** (Sections 8/9): ascending `RelativeOrder` is authoritative for *where* each stage's weeks land, independent of whichever tie-break decided *how many* weeks it received.
8. **Fill every week exactly once** — no gaps, no duplicates, no cross-phase assignment.

### Why the extension tie-break reading matters (a real, load-bearing finding)

`RACE_SPECIFIC`'s real v10 data (`TEN_K_SPECIFIC_INTRO` min1/max2/Extendable, `GOAL_PACE_REHEARSAL` min1/max2/`PROTECTED`/`FIXED_EXPOSURE`, `CURRENT_FITNESS_SPECIFIC_REHEARSAL` min1/max1) needs 4 weeks from a combined minimum of only 2. Reading `FixedExposure` as a **hard zero-headroom gate** (the more literal-sounding interpretation at first glance) makes the phase mathematically unschedulable for **both** condition branches — this was verified directly: it broke 18 of the then-628 existing tests the moment the scheduler was wired in. Reading it as a **priority-only** signal (per the tie-break language actually used in Section 11, "highest extension eligibility *first*") and combining it with the single-substitution merge rule above (§ algorithm step 4) makes the real default 12-week pilot schedule successfully in every existing test scenario. This is documented here as an explicit **V1 technical scheduler rule**, not a scientific or product claim, and it does not change any catalog value — only how this new algorithm interprets two already-existing fields.

## Decision trace

`StageAllocationDecisionTrace.Steps` — one `StageAllocationDecisionTraceStep` per scheduled week: phase, week number, requested vs. effective stage key, relative order, allocation kind (minimum/extension), compression action, condition outcome, fallback origin, the exact tie-break string used, and source artifact key/version. Deterministic and directly assertable in tests. Not exposed publicly — no existing internal trace wrapper covers this content, so it remains its own dedicated internal type.

## `TAPER_SHARPEN`

The one-week `TAPER` phase's only stage, `TAPER_SHARPEN`, is scheduled like any other stage: `PhaseKey="TAPER"`, `ProgressionStageKey="TAPER_SHARPEN"` (never coerced to `"TAPER"`). No workout identity is bound and no prescription is calculated here — this satisfies `AUD-508`'s own requirement that stage context "must be available to Phase 4F.7 prescription generation" without this phase doing any of 4F.7's work itself.

## Dark wiring

`CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` now runs, in order: (1) `_skeletonOrchestrator.Build` (Phase 4F.3 phase/week skeleton) → **(2) NEW: load the workout-progression document (`_progressionLoader`), allocate (`_stageAllocator`), validate (`_stageScheduleValidator`)** → (3) `_calendarMaterializer.Materialize` (Phase 4F.5 dated skeleton) → (4) `_datedSkeletonValidator.Validate` (Phase 4F.5.1). This is exactly the placement Section 16 calls for ("phase/week skeleton → stage scheduler → calendar assignment"). The result (`GeneratedCatalogStageSchedule`) is validated then immediately discarded — never stored on `CatalogPreviewSnapshot`, never hashed, never returned.

Every typed scheduler exception (`PlanCatalogLoadException`, every `ProgressionStageSchedulingException` subtype, `ProgressionStageScheduleInvalidException`) is caught and re-thrown as the existing `PlanPreviewGenerationFailedException` (message-prefixed `CATALOG_INTERNAL_STAGE_SCHEDULING_FAILED`), exactly mirroring the established Phase 4F.3/4F.5 exception-mapping convention — no new public error code.

### Constructor change (a deliberate, documented exception to the composition-root pattern)

`CatalogPreviewGenerator`'s public constructor gained one genuine new parameter, `ICatalogWorkoutProgressionLoader progressionLoader` — unlike `ICatalogPlanSkeletonOrchestrator`/`ICatalogWeekSkeletonCalendarMaterializer`/`IDatedGeneratedCatalogPlanSkeletonValidator` (all internal, composed from pure, dependency-free collaborators), `ICatalogWorkoutProgressionLoader` genuinely needs a real, environment-configured `PlanCatalog:CatalogRootPath` value — there is no dependency-free default to compose. It is `public` (not `internal`), so no CS0051 issue arises; it is registered in `Program.cs` alongside `IPlanCatalogBundleLoader`. `IProgressionStageAllocator`/`IGeneratedCatalogStageScheduleValidator` remain dependency-free and are still internally composed via the existing pattern. The internal test-seam constructor overloads (used by ~30 pre-existing tests) default the progression loader via a repo-root-walk (identical to this repository's own `TestPlanServicesFactory.RepoRoot()` convention) — safe because those overloads are only ever reachable from `RunningApp.IntegrationTests`, never from production, which always uses the public, DI-resolved constructor.

## Typed failures

12 exceptions in `ProgressionStageSchedulingExceptions.cs`, one per distinct rejection (unknown condition key, missing condition result, ineligible-without-fallback, fallback target not found, fallback cycle, ambiguous fallback, duplicate relative order, duplicate/missing key, phase mismatch, capacity insufficient, capacity exceeds maximum, unreconcilable fallback bounds), plus `ProgressionStageScheduleInvalidException` for output-validation failure.

## Public/persistence boundaries

Unchanged. No `GeneratedPreviewPlanPayload`, public DTO, snapshot, hash, confirm, or persistence code was touched. `CatalogPreviewSnapshot`'s own shape is identical to before this phase.

## Test coverage

`ProgressionStageAllocatorTests.cs` (27 tests): real-v10-data scenarios (default 12-week allocation for both the goal-pace-eligible and ineligible/fallback branches, per-phase exact counts, extension priority, `TAPER_SHARPEN` identity preservation, determinism, input-order independence, no-workout-identity, contiguous blocks, no-cross-phase, validator accept/reject) plus synthetic-fixture structural-failure tests (compression, insufficient capacity, excess-beyond-maximum, conditional eligible/ineligible-with-fallback/ineligible-without-fallback, fallback cycle, duplicate relative order, duplicate stage key, unknown condition key, missing condition result, true convergence success and unreconcilable-bounds failure, phase mismatch). All existing Phase 4F.1–4F.5.1 tests (628) continue to pass unchanged.

## Known deferred items

- **Workout binding** (`EASY_SUPPORT`/`LONG_RUN`/`KEY_SESSION` → exact workout definitions), **eligible-list selection policy**, and **prescription** all remain explicitly out of scope — Phase 4F.6B and 4F.7.
- **Public workout-type mapping** — Phase 4F.8.
- The `RACE_SPECIFIC` phase's genuinely tight exposure/extension-behavior configuration (documented above) works for the current default 12-week pilot under the chosen tie-break interpretation, but leaves **zero slack** — any future change to `RACE_SPECIFIC`'s stage exposure bounds, or to the phase's own week allocation, should be re-verified against this scheduler before being accepted, since the margin is currently exactly zero.
- The stage-scheduler's own `ProgressionStageAllocatorVersion.V1` tag is independent of every other phase's schema version, following the established per-phase-versioning convention (`GeneratedCatalogPlanSkeleton.CurrentSchemaVersion`, `CatalogCalendarDayMaterializerVersion`).
