# Phase 4F.6B — V1 Exact Workout Definition Binding and Dark Materialization

Binds every dated structural run slot in the dark catalog schedule to one exact, versioned workout definition. Assigns workout identity only — no pace, distance, duration, volume, repetitions, recovery, segments, public workout type, or prescription. No public output, snapshot/hash, confirm, or persistence change.

## Preflight verification (confirmed before implementation)

1. **`AUD-508`** concretely requires `TAPER_SHARPEN` to retain `EASY_STANDARD` identity while Phase 4F.7 later produces a materially different, reduced-volume, intensity-preserving prescription — confirmed by direct re-read of the entry text in `PilotDomainContentAudit.cs`.
2. **Phase 4F.6A's output** (`GeneratedCatalogStageSchedule`/`ScheduledProgressionWeek`) preserves `PhaseKey`, `ProgressionStageKey`, condition/fallback provenance (`ConditionOutcome`, `FallbackOrigin`, `RequestedProgressionStageKey`), and source artifact identity/version — confirmed by direct inspection. It does **not** preserve a stage candidate reference (deliberately excluded in 4F.6A); this phase extends `CatalogWorkoutProgressionStage` with a new, optional `WorkoutCandidateReferences` field and the loader that populates it (see Contract below) rather than reopening any 4F.6A decision.
3. **Runtime-condition semantics** — confirmed unchanged: `NotEvaluated` does not trigger fallback on its own (the 4F.6A allocator, unmodified, already treats it as "not satisfied," which only causes a fallback if the stage has a `Requires` clause and a configured fallback — never a separate "NotEvaluated implies fallback" rule). `Evaluated`-but-ineligible stages may use their configured fallback, exactly as 4F.6A already implemented. Phase 4F.6B never re-evaluates a condition or recomputes a fallback — it consumes 4F.6A's `ScheduledProgressionWeek.ProgressionStageKey`/`ConditionOutcome`/`FallbackOrigin` fields exactly as given.

No inconsistency found — proceeded with implementation.

## Binding policy

`V1CatalogWorkoutRoleBindingPolicy` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Binding/V1CatalogWorkoutRoleBindingPolicy.cs`), policy key `V1_CATALOG_WORKOUT_ROLE_BINDING_POLICY`, version `1`. The **one** place `EASY_STANDARD`/`LONG_RUN_STANDARD` appear as literal strings in this subsystem:

| Role | Mode | Resolution |
|---|---|---|
| `EASY_SUPPORT` | `FixedDefault` | `EASY_STANDARD` |
| `LONG_RUN` | `FixedDefault` | `LONG_RUN_STANDARD` |
| `KEY_SESSION` | `StageControlled` | the assigned effective progression stage's own explicit workout-candidate reference |

Both fixed defaults remain explicit `TEMPORARY_V1_SIMPLIFICATIONS` (D-C07/D-C08) — a future multi-workout role requires a new, versioned policy, never an in-place change to this one.

## Contract

New: `backend/RunningApp.Application/RuntimeCatalog/Schedule/Binding/`
- `CatalogWorkoutDefinitionLoader.cs` — `ICatalogWorkoutDefinitionLoader`/`CatalogWorkoutDefinitionLoader` (public — needs real `PlanCatalog:CatalogRootPath`, same reasoning as the 4F.6A progression loader) + `CatalogWorkoutDefinitionSummary`.
- `V1CatalogWorkoutRoleBindingPolicy.cs` — the policy + `CatalogWorkoutBindingMode` enum (`FixedDefault`/`StageControlled`).
- `BoundCatalogPlanContracts.cs` — `BoundCatalogPlan`, `BoundCatalogWeek`, `BoundCatalogSession`, `WorkoutBindingDecisionTrace`/`Step`.
- `CatalogWorkoutBindingExceptions.cs` — 10 typed exceptions.
- `CatalogWorkoutBinder.cs` — `ICatalogWorkoutBinder`/`CatalogWorkoutBinder`, `CatalogWorkoutBindingContext`.
- `BoundCatalogPlanValidator.cs` — `IBoundCatalogPlanValidator`/`BoundCatalogPlanValidator`.

Modified (additive only): `CatalogWorkoutProgressionStage` (4F.6A) gained an optional `WorkoutCandidateReferences` field (defaults empty — every existing 4F.6A test fixture continues to compile/behave unchanged); `CatalogWorkoutProgressionLoader` now parses the `workoutCandidates` array (the schemaVersion≥2 exact-reference shape the current v10 artifact actually uses).

`BoundCatalogSession` carries exactly the required fields (`WeekNumber`, `Date`, `PhaseKey`, `ProgressionStageKey` (nullable), `StructuralRole`, `WorkoutDefinitionKey`/`Version`, `BindingMode`, `BindingPolicyKey`/`Version`, `SourceArtifactKey`/`Version`, `ConditionOutcome`, `FallbackOrigin`, `BindingReason`) — no prescription field exists anywhere on it.

## EASY_SUPPORT / LONG_RUN binding (fixed default)

For each slot: look up the policy's fixed-default key, find its exact `(key, version)` in `PlanCatalogCandidateSummary.ReferencedWorkouts` (the level-modifier's eligible-workouts list — never a hardcoded version), load and validate the definition. `ProgressionStageKey` is always `null` for these sessions. Zero matches → `CatalogWorkoutBindingCandidateNotFoundException`; more than one → `CatalogWorkoutBindingAmbiguousCandidateException` (defensive; unreachable with well-formed `ReferencedWorkouts`).

## KEY_SESSION binding (stage-controlled)

For each slot: read the week's effective `ProgressionStageKey` from the 4F.6A stage schedule (join key: `WeekNumber`) → resolve that stage in the loaded workout-progression artifact → read its `WorkoutCandidateReferences` (must be exactly 1 — zero is `CatalogWorkoutBindingMissingCandidateReferenceException`, more than one is `CatalogWorkoutBindingAmbiguousCandidateException`, since V1 implements no multi-workout selection algorithm) → load and validate the referenced definition. Never infers a workout from stage name or family — always resolves through the explicit reference chain.

## Validation

Every resolved definition is checked for: existence/loadability (`CatalogWorkoutBindingCandidateNotFoundException`), exact key/version match (`CatalogWorkoutBindingVersionMismatchException`), membership in the resolved dependency closure — `ReferencedWorkouts ∪ every progression stage's own candidate references` (`CatalogWorkoutBindingOutsideDependencyClosureException`), and eligible-phase/status validity (`CatalogWorkoutBindingDefinitionInvalidException`). **Family compatibility is intentionally not enforced for `KEY_SESSION` bindings** — `TAPER_SHARPEN`/`FOUNDATION_EASY_BASE` legitimately target EASY-family workouts under `KEY_SESSION` (D-C06's already-accepted scope), and re-imposing the Process-A-side `RoleCompatibleFamilies` gate here would contradict that acceptance. Family compatibility for the two fixed-default roles is implicitly guaranteed by the policy itself (there is only one possible key per role).

`BoundCatalogPlanValidator` additionally checks: every slot bound exactly once, no rest-day session introduced, each week retains exactly 1 `KEY_SESSION`/2 `EASY_SUPPORT`/1 `LONG_RUN` (4-day pilot), binding mode matches the role's policy entry, fixed defaults match the policy's configured keys, ordinary `EASY_SUPPORT`/`LONG_RUN` sessions carry no `ProgressionStageKey`, and trace step count matches session count.

## Decision trace

`WorkoutBindingDecisionTrace.Steps` — one `WorkoutBindingDecisionTraceStep` per bound session: week/date/role/phase, progression stage (requested/effective), binding mode, the configured default or resolved stage candidate, resolved workout key/version, condition/fallback provenance, policy key/version, source artifact key/version, validation result. Deterministic, directly testable, never exposed publicly.

## TAPER_SHARPEN

Confirmed preserved exactly: `PhaseKey="TAPER"`, `ProgressionStageKey="TAPER_SHARPEN"`, `StructuralRole="KEY_SESSION"`, `WorkoutDefinitionKey="EASY_STANDARD"`. Distinguishable from an ordinary `EASY_SUPPORT` session that also resolves to `EASY_STANDARD` via **all three** of: `StructuralRole` (`KEY_SESSION` vs. `EASY_SUPPORT`), `BindingMode` (`StageControlled` vs. `FixedDefault`), and `ProgressionStageKey` (non-null vs. always null) — verified directly by a dedicated test. No prescription, strides, pace change, segment, or new taper workout definition was created.

## EASY_SHAKEOUT risk closure — `TD-EASY-WORKOUT-REGISTRY-001`

**Closed.** Direct inspection of Golden Fixture v3's `EASY_SHAKEOUT` occurrence (`W12_D3`) shows `raceEveShakeout=true` and `raceEveDistanceCapKm=4` — fields with no `EASY_STANDARD` equivalent, evidencing a genuinely distinct, specialized race-eve shakeout-run semantic. Renaming this occurrence to `EASY_STANDARD` (the first `requiredResolution` option) would be **factually incorrect**, not merely conservative — it would erase real content the fixture encodes. Instead, closed via the third option: formal reclassification, mirroring the repository's own already-accepted precedent for `LONG_RUN_PROGRESSION` (`domain-wave1-schema-necessity-audit.md`: "a distinct, non-substitutable workout key, not evidence for `LONG_RUN_STANDARD` itself"). The fixture itself was **not modified** — only its governance interpretation for V1 binding purposes is now recorded (`activation-readiness-risks.json`/`.md`, `closureNote`). `EASY_WITH_STRIDES` remains a separate, still-open, non-blocking vocabulary question.

## Dark wiring

`CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton`: phase/week skeleton (4F.3) → fine-grained stage schedule (4F.6A) → calendar-day assignment + dated-skeleton validation (4F.5/4F.5.1) → **workout binding + bound-plan validation (new, 4F.6B)** → STOP. This is the exact placement Section 14 calls for (binding after both the fine-grained stage schedule and the dated structural schedule exist). The workout-progression definition and stage schedule computed during the 4F.6A step are reused, never reloaded/recomputed. Result validated then immediately discarded.

`CatalogPreviewGenerator`'s public constructor gained one further genuine parameter, `ICatalogWorkoutDefinitionLoader` (same reasoning as 4F.6A's `ICatalogWorkoutProgressionLoader` — a real environment-configured dependency, not composable as a dependency-free default), registered in `Program.cs`. `ICatalogWorkoutBinder`/`IBoundCatalogPlanValidator` remain dependency-free and are internally composed via the established pattern.

## Typed failures

10 exceptions in `CatalogWorkoutBindingExceptions.cs`: unknown structural role, missing progression stage, unknown progression stage, missing candidate reference, ambiguous candidate, candidate not found, version mismatch, outside dependency closure, definition invalid, bound-plan invalid.

## Public/persistence boundaries

Unchanged. No `GeneratedPreviewPlanPayload`, DTO, snapshot, hash, confirm, or persistence code touched.

## Deferred (multi-workout policy)

V1 implements no multi-workout selection algorithm — zero resolved candidates or more than one unresolved selectable candidate both fail loudly. A future role needing multiple selectable workout definitions requires an explicit, new, versioned policy.

## Test coverage

`CatalogWorkoutBinderTests.cs` (21 tests): real-v10-data end-to-end binding (default 12-week pilot, every slot bound exactly once, `EASY_SUPPORT`/`LONG_RUN`/`KEY_SESSION` bindings, exact version resolution, input-order independence, determinism, fine-grained stage preservation, `TAPER_SHARPEN` identity + distinguishability, fallback provenance, `NotEvaluated`-is-not-fallback, no-prescription-fields, validator accept/reject) plus synthetic-fixture structural-failure tests (missing fixed default, missing/ambiguous stage candidate, candidate not in bundle, version mismatch). All existing Phase 4F.1–4F.6A tests (655) continue to pass unchanged; full suite is 676/676.
