# Phase 10K-FREQ.6D.4D — Intermediate 5D Dual-KEY Stage → Prescription Profile Production Integration Architecture

**Architecture-design phase. No production code, no product decision, no dosage change, no WorkoutDefinition change, no profile content change, no catalog lifecycle change, no Adaptation policy change, no public activation. Design only — a later implementation phase (or phases) executes it.**

## 1. Preflight

`PHASE_LEDGER.md` rows 56/58/68/69/70/71: `FREQ.6D.3C`, `FREQ.6D.3D`, `FREQ.6D.4B.4`, `FREQ.6D.4C.3`, `FREQ.6D.4C.4`, `FREQ.6D.4C.5` all `DONE`/`VERIFIED`. `FREQ.6D.4C.5` final classification confirmed exactly `FREQ6D4C5_LIFECYCLE_BLOCKER_CLOSED_6D4D_READY`. Commits `7777eee`, `df5c148`, `00de364` all confirmed reachable from HEAD via `git merge-base --is-ancestor`. Starting HEAD `00de364caac1519decfd81f3eab678c2139f9a5a`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 10`. `git status --short` → ` m baseline_tmp` only (preserved, untouched). `git diff --check` → clean. `FREQ.6D.4D` confirmed not already a completed ledger row.

Real reports read in full: `PHASE_10K_FREQ_6D_1A_GENERIC_PRESCRIPTION_DUAL_KEY_DESIGN_EVIDENCE_CLOSURE.md`, `PHASE_10K_FREQ_6D_1B_SEVERITY_TABLE_FIDELITY_AND_OPEN_DECISION_CHECK.md`, `PHASE_10K_FREQ_6D_3C_DETERMINISTIC_EXECUTION_PROJECTION_AND_BUNDLE_INTEGRATION.md`, `PHASE_10K_FREQ_6D_3D_RUNNINGAPP_EXECUTION_PRESCRIPTION_CONSUMER_IMPLEMENTATION.md`, `PHASE_10K_FREQ_6D_4C_3_INTERMEDIATE_5D_REAL_PRODUCTION_PROFILE_AUTHORING.md`, `PHASE_10K_FREQ_6D_4C_5_LEGACY_RESOLVER_ELIGIBILITY_AND_VALIDATION_PROMOTION_IMPLEMENTATION.md`.

**Critical, load-bearing finding of this preflight**: `FREQ.6D.1A`/`FREQ.6D.1B` designed a complete, thorough Lane/Stage/Adaptation architecture (`CatalogWorkoutProgressionLane`, `LaneOrdinal`, `BoundCatalogSession.KeySessionLaneOrdinal`, coordination validator, severity-table widening, persistence columns) — but **none of it was ever implemented**. Direct repository-wide search confirms zero production declarations of `CatalogWorkoutProgressionLane`/`LaneOrdinal`/`KeySessionLaneOrdinal` exist anywhere in `backend/` or `plan-catalog/` (the only two textual hits are a doc-comment cross-reference in `CatalogSessionPrescriptionSource.cs` and a negative-assertion string literal in `plan-catalog/tests/PlanCatalog.Tests/Architecture/PublishedBoundaryTests.cs`). Every gap `FREQ.6D.1A` described (`stageWeeksByNumber` keyed by week only; `ScheduledProgressionWeek.StructuralRole` a hardcoded constant; `keyOrdinal` a transient loop-local variable, never persisted) was independently re-verified against current real code in this phase and is **still accurate today**. `FREQ.6D.1A`/`1B` are therefore treated here as **rigorous prior-art evidence and a proposed design to re-validate and adopt**, not as "already frozen, merely verify" — this phase makes the actual binding architecture selection.

A second, real, disclosed development since `FREQ.6D.1A`/`1B` were written: **Phase 10K-FREQ.4** landed real multi-KEY generalization for distance allocation (`FourDaySessionDistanceAllocationPolicy.Allocate(..., keySessionCount)`) and Adaptation counting (`WindowExecutionSummary.KeySessionExpectedCount`/`KeySessionCompletedCount`, replacing lossy pre-FREQ.4 booleans) — but explicitly, by its own doc comments, **did not** touch `NextWindowLoadDecisionPolicy`'s severity thresholds (still hardcoded to a 4-session week) or introduce any lane/ordinal identity. This is folded into the evidence below (§8/§20).

## 2. Parent state

The catalog-content side of Intermediate×5D is now completely ready: 8 real, `VALIDATED`, exact-reference `WorkoutPrescriptionProfile` documents exist (`FREQ.6D.4C.3`), all four previously-`DRAFT` `WorkoutDefinition` versions are now `VALIDATED` with legacy-bare-key-resolver eligibility explicitly disabled (`FREQ.6D.4C.5`), and the generic exact-reference projection pipeline (`WorkoutPrescriptionExecutionProjector`, `ExactPrescriptionProjectionDependency`, `CatalogBundleAssembler`) and RunningApp consumer seam (`ExecutionPrescriptionIndex`, `PublishedTemplateBundleJsonReader`, `CatalogSessionPrescriptionSource`) are implemented and tested but **entirely unwired** (`FREQ.6D.3C`/`3D`). Both 3C's and 3D's own reports state explicitly: *"FREQ.6D.4 now owns: Week × LaneOrdinal → exact progression stage → exact profile reference → this phase's `ResolveExact` seam."* This phase is exactly that ownership.

## 3. Current stage ownership

`CURRENT_STAGE_OWNERSHIP_MAP` — every type touching week/phase/stage/session-allocation, read directly from current code:

| Type | File | Input | Output | Keying dimension | Lane-aware? | Role-aware? | Phase-aware? | Exact-version aware? |
|---|---|---|---|---|---|---|---|---|
| `CatalogWorkoutProgressionDefinition`/`CatalogPhaseWorkoutProgression`/`CatalogWorkoutProgressionStage` | `Schedule/Progression/CatalogWorkoutProgressionDefinition.cs` | Parsed `WORKOUT_PROGRESSION` document | In-memory stage list per phase | `PhaseKey` → single `Stages[]` list | **No** (one flat `Stages[]` per phase, no `Lanes[]`) | No (stage carries no role) | Yes | Yes (`WorkoutCandidateReferences` are exact `(Key, Version)`) |
| `ProgressionStageAllocator.Allocate`/`AllocatePhase` | `Schedule/Progression/ProgressionStageAllocator.cs:59,106` | One `(Stages, phaseWeeks)` pair per phase, called once | `GeneratedCatalogStageSchedule.Weeks` | `WeekNumber` (one row per week) | **No** | No | Yes | N/A (stage-key only, no workout ref) |
| `ScheduledProgressionWeek` | `Schedule/Progression/ProgressionStageScheduleContracts.cs:71-94` | Allocator output | One row per week | `WeekNumber` | **No** | `StructuralRole` is a **hardcoded constant** `"KEY_SESSION"` (`:85`, not derived) | Yes | No (carries `ProgressionStageKey`, not a workout ref) |
| `CatalogWorkoutBinder.BindAsync` | `Schedule/Binding/CatalogWorkoutBinder.cs:38` | `StageSchedule` + `DatedSkeleton` | `BoundCatalogPlan` (per-session workout binding) | `stageWeeksByNumber = context.StageSchedule.Weeks.ToDictionary(w => w.WeekNumber)` (`:105`) | **No** — one dictionary entry per week; two `KEY_SESSION` slots in the same week would collide onto the identical stage assignment | Yes (`slot.StructuralRole` drives `FixedDefault` vs `StageControlled`) | Yes | Yes (`ResolveDefinitionAsync` is exact `(Key, Version)`) |
| `BoundCatalogSession` | `Schedule/Binding/BoundCatalogPlanContracts.cs:35-55` | Binder output | One row per session | N/A (already a leaf record) | **No field exists** | Yes (`StructuralRole`) | Yes | Yes (`WorkoutDefinitionKey`/`Version`) |
| `CatalogSessionPrescriptionPlanner` | `Prescription/Session/CatalogSessionPrescriptionPlanner.cs:23-38` | `BoundPlan` | `CatalogWorkoutPrescription` (legacy) per session | `boundWeek.Sessions.OrderBy(Date).ThenBy(StructuralRole)`, `keyOrdinal`/`easyOrdinal` computed as **transient loop-local `int`s** (`:32-37`) | **No** — never persisted, never returned, exists only to index `FourDaySessionDistanceAllocation.KeySessionDistancesKm[keyOrdinal]` | Indirect (role) | Indirect | No (legacy prescription is not exact-version-referenced) |
| `TrainingDay` | `Domain/Entities/TrainingDay.cs` | Persisted plan-generation output | Durable row | `Id` (GUID) | **No column exists** | `CatalogStructuralRole`/`CatalogSlotRole` (plain strings) | `CatalogPhaseKey` | `CatalogWorkoutDefinitionKey`/`Version` (workout only — no profile key/version column) |
| `LongHorizonRollingSessionState` (dark repair/substitution subsystem) | `Schedule/LongHorizon/Adaptation/*` | Repair events | Durable row (separate table, not `TrainingDay`) | `SessionOrdinal` = whole-week positional counter (`NextOrdinalAsync` = max+1), **not** a per-role lane index | **No** | `SessionRole` (flat enum: `KeySession`/`EasySupport`/`LongRun`, no `KeySession1`/`KeySession2` variants) | Indirect | No |
| `WindowExecutionSummary` (Adaptation) | `Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs:174-198` | Completed/expected session roll-up | Weekly severity input | `KeySessionExpectedCount`/`KeySessionCompletedCount` — **integer counts**, generalized from a lossy boolean by `FREQ.4` specifically to avoid needing per-instance identity | **No, by design** (proven §8) | Count only, no per-lane split | N/A | N/A |

## 4. Current dual-KEY defect

Re-verified directly against current code (not assumed from `FREQ.6D.1A`'s earlier finding):

```
DEFECT_STILL_PRESENT
```

Exact evidence: `CatalogWorkoutBinder.cs:105`, `var stageWeeksByNumber = context.StageSchedule.Weeks.ToDictionary(w => w.WeekNumber);` — a `Dictionary` keyed solely on `WeekNumber`. If `RunLayout` ever declared two `KEY_SESSION` slots in one week (Intermediate×5D's real, frozen structural cardinality), `ScheduledProgressionWeek` produces exactly one row per week (the allocator has never been invoked per-lane), so both slots would resolve to the **same single stage assignment**, and hence very likely the **same exact profile**, for that week — losing the PRIMARY/SECONDARY_CONTROLLED distinction entirely. `ScheduledProgressionWeek.StructuralRole` is a hardcoded constant (`:85`), carrying no lane information even in principle. No `RUN_LAYOUT_5D` catalog artifact exists yet (confirmed: `plan-catalog/catalog/layouts/` has no 5D file) — the defect has never been exercised in production because 5D has never reached this layer; it is a real, live gap in the code path, not a hypothetical.

## 5. Canonical slot identity

**`WeekNumber + StructuralRole + LaneOrdinal`** — adopting `FREQ.6D.1A §B1/§8`'s proposal, re-confirmed correct against current evidence. This is the minimum identity that distinguishes "Week 5 KEY lane 0" from "Week 5 KEY lane 1" without inventing a second structural role (§2 of this phase's own prompt forbids `PRIMARY_KEY_SESSION`/`SECONDARY_KEY_SESSION` as new `RunLayout` roles — `StructuralRole` remains uniformly `"KEY_SESSION"` for both lanes, confirmed no such split-role string exists or is proposed anywhere in the current codebase).

## 6. Lane assignment authority

**`LaneOrdinal` is catalog-authored, not calendar/date-derived** — a deliberate, load-bearing choice re-confirmed here: KEY1/KEY2 must mean the same physiological purpose every week regardless of which weekday it lands on (which can shift under repair); deriving lane identity from date order would make "which lane is this" a materialization-time accident. The catalog progression artifact is the single canonical owner: a new `CatalogWorkoutProgressionLane` wrapping today's `CatalogWorkoutProgressionStage[]` (see §11) declares `LaneOrdinal` explicitly per lane, once, at authoring time — never recomputed, never re-derived by the binder, publisher, or RunningApp. This is a **distinct concept** from the *structural binding ordinal* (§7), which **is** derived at materialization time from physical slot order — the two are related by a binding rule (structural ordinal N ↔ `LaneOrdinal` N), never by being the same computation.

Assignment timing: `LaneOrdinal` exists on the catalog artifact from the moment it is authored/published — long before any candidate/plan is generated. The *structural binding ordinal* (which slot in a given dated week is "lane 0" vs "lane 1") is assigned once, at bind time (`CatalogWorkoutBinder.BindAsync`), before any profile/prescription selection occurs, and is carried thereafter on `BoundCatalogSession` (new field, §7) through calendar assignment, repair, substitution, persistence, and Adaptation without ever being rediscovered downstream.

## 7. Deterministic ordering

Structural binding ordinal = zero-based rank over `datedWeek.SessionSlots.Where(s => s.StructuralRole == "KEY_SESSION").OrderBy(s => s.SlotOrderInWeek)` — `SlotOrderInWeek` already exists on the dated skeleton (unchanged upstream step, confirmed real field consumed at `CatalogWorkoutBinder.cs:110`) and is itself deterministic per dated-skeleton materialization. No dictionary/enumeration-order dependence: the binder already iterates `context.DatedSkeleton.Weeks.OrderBy(w => w.WeekNumber)` then `datedWeek.SessionSlots.OrderBy(s => s.SlotOrderInWeek)` (`:106,110`) — both are already explicit sorts, not incidental collection order. The binding rule is: **structural ordinal N binds to `LaneOrdinal` N** (an explicit equality, not a coincidence).

**Real, disclosed correction required to a second, independent ordinal**: `CatalogSessionPrescriptionPlanner.cs:34`, `boundWeek.Sessions.OrderBy(s => s.Date).ThenBy(s => s.StructuralRole)`, computes its own `keyOrdinal` by **date-then-role** order — a *different* sort key from `SlotOrderInWeek`. Nothing today guarantees these two orderings coincide (e.g. if slot order is ever non-date-monotonic, or two KEY slots land on the same calendar date under an unusual layout). This is the exact contradiction `FREQ.6D.1A §C2` found in the parent `FREQ.6D.1` design (which had claimed simple reuse) — re-confirmed still present in current code. **Fix**: add `KeySessionLaneOrdinal` (nullable `int`) to `BoundCatalogSession`, computed once by the binder from `SlotOrderInWeek` (§6/§7), and change `CatalogSessionPrescriptionPlanner` to **read this field instead of recomputing its own `keyOrdinal`** — eliminating both the ordering-mismatch risk and the duplicate computation, making the binder the single source of truth for lane/ordinal identity end-to-end.

## 8. Stage semantics

Direct answer, checked against current code (not inferred): **(A)** — PRIMARY and SECONDARY_CONTROLLED lanes are each their own independent stage/progression, differing in exact prescription-profile content, not sharing one `ProgressionStage` progression. Evidence: `ProgressionStageAllocator.Allocate` (`:59-104`) groups by phase and calls `AllocatePhase` **once per phase**; nothing in its signature ties it to "the phase's only stage list" — it consumes exactly one `(stages, phaseWeeks)` pair per invocation, with no cross-invocation shared mutable state (confirmed by reading the full 557-line file). This means each lane can — and must — be its own independent invocation of the *same, unmodified* algorithm, receiving its own `Stages[]` against the shared week budget for that phase. `MinimumExposures`/`MaximumExposures`/`CompressionBehavior`/`ExtensionBehavior`/`Requires`/`FallbackStageKey` generalize unchanged; only the invocation count changes (1 → N per phase). This directly answers §12's "load-bearing" question: **the two lanes may have (and, per FREQ.6's approved matrix, do have) different prescription content without requiring different stage-progression *mechanism*** — each lane runs its own stage allocation, independently, using the existing algorithm verbatim.

## 9. Stage×lane matrix

`DUAL_KEY_STAGE_LANE_MATRIX` — real profile identities from `FREQ.6D.4C.3` (verified via `git ls-files plan-catalog/catalog/prescription-profiles/`):

| Phase | LaneOrdinal | Prescription semantic | Exact profile key (v1) | Exact `WorkoutDefinition` ref | Stage affects exact profile? | Phase alone sufficient? |
|---|---|---|---|---|---|---|
| Foundation | 0 | Primary — controlled aerobic-strength intro | `INTERMEDIATE_5D_FOUNDATION_PRIMARY` | `AEROBIC_STRENGTH_CONTROLLED_INTRO v3` | No — one profile per phase×lane in this V1 matrix (no sub-phase stage variation authored) | Yes, for V1 |
| Foundation | 1 | SecondaryControlled — controlled threshold intro | `INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED` | `THRESHOLD_TEMPO v5` | No | Yes |
| Build | 0 | Primary — threshold pace | `INTERMEDIATE_5D_BUILD_PRIMARY` | `THRESHOLD_TEMPO v4` | No | Yes |
| Build | 1 | SecondaryControlled — surge/fartlek support | `INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED` | `FARTLEK v5` | No | Yes |
| RaceSpecific | 0 | Primary — 10K goal pace | `INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY` | `GOAL_PACE_TEN_K v2` | No | Yes |
| RaceSpecific | 1 | SecondaryControlled — threshold support | `INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED` | `THRESHOLD_TEMPO v4` | No | Yes |
| Taper | 0 | Primary — goal-pace sharpening | `INTERMEDIATE_5D_TAPER_PRIMARY` | `GOAL_PACE_TEN_K v3` | No | Yes |
| Taper | 1 | SecondaryControlled — controlled strides sharpening | `INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` | `FARTLEK v5` | No | Yes |

No slot lacks authority — every one of the 8 (phase × lane) cells has a real, `VALIDATED`, exact profile. This is a **flat phase→profile mapping** for V1 (no multi-stage-per-phase variation was authored — each phase currently has exactly one progression stage per lane); the architecture below (§11) supports finer per-stage variation (matching `FREQ.6's` general model) without requiring it today.

## 10. Profile selection authority

**One deterministic catalog-side authority**: the progression **stage** (a new `PrescriptionProfileCandidateKeys[]` field on `CatalogWorkoutProgressionStage`, sibling to today's `WorkoutCandidateReferences`, same exact-pin discipline, same "declares >1 candidate → ambiguous, throws" rule reused verbatim from `CatalogWorkoutBinder.cs:145-150`). This is resolved entirely inside `CatalogWorkoutBinder` (or a narrow, adjacent prescription-binding step immediately following it) — never by `CatalogSessionPrescriptionPlanner` searching by dose category, never by RunningApp, never at publish/read time by scanning the full profile catalog. Direct answer to §14: candidates are **progression artifact** (chosen) over **combination manifest** (too coarse — a combination doesn't vary per week/lane) or a brand-new allocation table (unnecessary — the existing stage/candidate-reference mechanism already generalizes cleanly, per §8).

## 11. Exact refs

**No selection by `DoseCategory` alone.** `DoseCategory` (`Primary`/`SecondaryControlled`) remains a validation/semantic attribute on the profile (already enforced by `PrescriptionProfileLaneDoseValidator`, `FREQ.6D.4C.3`) — never a runtime lookup key. The catalog-side binding must resolve **exact `ProfileKey` + exact `ProfileVersion`** by the time a candidate/combination is published — no "give me any Foundation Primary profile" runtime search exists or is proposed anywhere. `LaneOrdinal↔DoseCategory` is additionally a **fixed, frozen mapping per `FREQ.6 §13`** (re-confirmed, not re-decided, by `FREQ.6D.1B §B4`: *"KEY1 = PRIMARY; KEY2 = SECONDARY_CONTROLLED in every phase"*) — this phase adds one small, disclosed publish-time invariant: **a lane's referenced profile's `DoseCategory` must equal the FREQ.6 §13-mandated value for that lane's `LaneOrdinal`** (LaneOrdinal 0 → `Primary` required; LaneOrdinal 1 → `SecondaryControlled` required), enforced as a new typed validator, not a runtime check.

Each profile already carries its own exact `WorkoutDefinitionRef` (`WorkoutPrescriptionProfile.WorkoutDefinitionRef`, confirmed real field). The 5D binding layer must **not** independently resolve a `WorkoutDefinition` reference a second time — the correct chain is `slot → exact profile (via progression stage) → profile owns exact WorkoutDefinition ref`, never a parallel workout-reference authority. This directly matches `CatalogWorkoutBinder`'s existing exact-pin discipline (`ResolveDefinitionAsync`, `:53-80`, throws `CatalogWorkoutBindingVersionMismatchException` on any mismatch) — reused, not duplicated.

## 12. Projection dependency

`ExactPrescriptionProjectionDependency` (real, `plan-catalog/src/PlanCatalog.Infrastructure/Projection/ExactPrescriptionProjectionDependency.cs`, confirmed via `FREQ.6D.3C §16`) carries only an exact `VersionedCatalogReference` (`DocumentType = WORKOUT_PRESCRIPTION_PROFILE`, exact `Key`/`Version`). It is created by **whatever assembles the exact dependency closure for a combination's own publication** — for the profile-backed 5D path, this is a new, narrow, catalog-authoring-time step: once the stage/lane→exact-profile mapping (§10/§11) is resolved for a given combination's full progression, the **union of every distinct exact profile reference reachable across all phases/lanes for that combination** becomes the `IReadOnlyList<ExactPrescriptionProjectionDependency>` passed to `CatalogBundleAssembler.Assemble(snapshot, combinationKey, combinationVersion, executionDependencies)` (the existing overload, confirmed real, `CatalogBundleAssembler.cs:20-26`). No new parallel projection route: this reuses the exact mechanism `FREQ.6D.3C` already built and proved, generically, against synthetic dependencies — the only new work is *what supplies the dependency list for a real 5D combination*, not a new projector/assembler.

## 13. Dependency cardinality

Per `FREQ.6D.3C §16` (*"never scans all profiles or selects by family, dose, key-only, latest, lane, or 5D policy"*) and the real `CatalogBundleAssembler.ProjectExecutionPrescriptions` code (`:145-169`, deduplicates by `(DocumentType, Key, Version)` — `EXECUTION_PROJECTION_DUPLICATE_PROFILE_DEPENDENCY` on a literal duplicate tuple, but does **not** deduplicate distinct-but-reused profiles by content): the bundle's dependency set is **the exact, distinct set of `(profile key, version)` pairs actually referenced across the combination's progressions** — not "one dependency per scheduled slot." For a 12-week Intermediate×5D Core plan, only **8 distinct profile artifacts** exist at all (`FREQ.6D.4C.3`), so the real dependency list for a full-plan combination is exactly those 8 entries (or a subset, if a shorter horizon's phase allocation never reaches every phase) — reused across every week/lane that resolves to the same phase/lane pair. `TAP-S`/`BLD-S` both reference `FARTLEK v5` (two *profiles*, one shared *workout definition* — the profile/workout distinction, not a duplicate dependency) — confirmed no collision risk since dependencies are keyed by profile identity, not workout identity.

## 14. Bundle semantics

`PublishedTemplateBundle.ExecutionPrescriptions` (audited directly, `PlanCatalog.Contracts.Bundles`) is **a unique executable-prescription library indexed by source profile identity**, not a per-session/per-slot mapping — confirmed by `ExecutionPrescriptionIndex`'s own internal shape (`Dictionary<ExecutionPrescriptionLookupKey, ExecutableWorkoutPrescription>`, keyed by `(DocumentType, Key, Version)`, `FREQ.6D.3D §8`) and by `CatalogBundleAssembler`'s dedupe-by-tuple behavior (§13). A materialized session binding references the correct library entry by carrying the **exact profile `(Key, Version)`** it was bound to (§15/§16) and calling `ExecutionPrescriptionIndex.ResolveExact(profileRef)` — never by index position, never by duplicating the projected prescription per slot. The bundle is not overloaded with duplicate identical projections; reuse across weeks/lanes that share a phase's profile is handled entirely by the library-lookup pattern, exactly as the real, tested `ExecutionPrescriptionIndex` contract already requires.

## 15. RunningApp consumer

Read directly: `ExecutionPrescriptionIndex.ResolveExact(VersionedCatalogReference profileRef)` is the **only** input RunningApp needs — an exact profile reference. Confirmed via `FREQ.6D.3D §24` item 5 ("No version selection occurs in RunningApp — no `Latest`/`Nearest`/`First` method exists") and item 10 ("no lane/keyOrdinal/progression decision exists in the consumer"). RunningApp must not, and per the real, current, tested implementation **does not**: resolve versions, derive `LaneOrdinal`, choose dose category, choose stage, or search the profile catalog. `CatalogSessionPrescriptionSource.ProfileBacked(ExecutableWorkoutPrescription)` is the discriminator RunningApp uses once it already has the resolved value — the resolution itself happens entirely upstream, at bind/publish time (§10-§12), never in the consumer.

## 16. Session binding contract

**Narrowest existing contract, reused**: `VersionedCatalogReference` (real, `PlanCatalog.Contracts.References`) — the exact same type already used by `ExecutionPrescriptionIndex.ResolveExact`. A materialized session binding (`BoundCatalogSession`, new fields) must carry `PrescriptionProfileKey` (`string?`) and `PrescriptionProfileVersion` (`int?`) — nullable/additive, null for `FixedDefault` (`EASY_SUPPORT`/`LONG_RUN`) roles and any `StageControlled` session not yet resolved through the profile-backed path (§20 legacy boundary). No duplicate lineage field is invented: `ExecutionPrescriptionKey`/stable-source-identity is not a separate concept — the exact `(ProfileKey, ProfileVersion)` pair **is** the stable identity (profiles are immutable once published, per this whole engagement's standing invariant), so RunningApp's lookup key and the persisted lineage key are the same two fields, not two different mechanisms.

## 17. TrainingDay persistence lineage

Audited the real `TrainingDay` entity (`backend/RunningApp.Domain/Entities/TrainingDay.cs`) and its EF model snapshot directly. Classification of every relevant datum:

| Datum | Classification | Basis |
|---|---|---|
| Structural `Role` | `MUST_PERSIST` (already persisted) | `CatalogStructuralRole`/`CatalogSlotRole` columns exist today |
| `LaneOrdinal` | `DERIVABLE_BUT_SHOULD_PERSIST` — **no new column recommended** | Reconstructible from `(CatalogProgressionStageKey, progression key+version)` once stage keys are made unique-across-lanes (§20's new invariant); progression artifacts are immutable once published (standing engagement invariant), so this reconstruction is deterministic forever. Avoids a third new column when the existing `CatalogProgressionStageKey` (already persisted) already carries this information indirectly. |
| `ProgressionStage` | `MUST_PERSIST` (already persisted) | `CatalogProgressionStageKey` column exists today |
| `PrescriptionProfile` key/version | **`MUST_PERSIST`, new columns required** | `TrainingDay` has no column for this today (confirmed: only `CatalogWorkoutDefinitionKey`/`Version` exist, no profile equivalent). Add `CatalogPrescriptionProfileKey` (`string?`), `CatalogPrescriptionProfileVersion` (`int?`) — nullable, additive, matching the exact established pattern of the existing `Phase4F.6B`-era `CatalogWorkoutDefinitionKey`/`Version` columns and the `AddPlanCatalogProvenanceFields` migration convention. |
| `WorkoutDefinition` key/version | `MUST_PERSIST` (already persisted) | `CatalogWorkoutDefinitionKey`/`CatalogWorkoutDefinitionVersion` exist today |
| Executable-prescription source hash/version | `BUNDLE_ONLY` — **not required on `TrainingDay`** | The profile's own `(Key, Version)` is immutable and sufficient to deterministically reconstruct the exact `ExecutableWorkoutPrescription` from any historical published bundle for that exact combination version; persisting a redundant hash on every `TrainingDay` row would be a second, unnecessary provenance authority for the same fact |

## 18. Regeneration stability

A persisted `TrainingDay` row must not change meaning if the catalog later gains a newer profile or `WorkoutDefinition` version, or if progression mappings evolve. Guaranteed by construction: `TrainingDay.CatalogPrescriptionProfileKey`/`Version` (new, §17) and `CatalogWorkoutDefinitionKey`/`Version` (existing) are exact, immutable pins — never re-resolved on read. There is no "resolve latest" path anywhere in the read side (`TrainingDaysController`, `HomeResponse`, etc. — confirmed by the existing pattern already used for `CatalogWorkoutDefinitionKey`, which the real code never re-resolves either). Historical plan stability is guaranteed the same way `FREQ.6D.4C.4`/`4C.5` already guarantee it for exact-version catalog lookups generally: `FindPrescriptionProfile(key, version)`/`FindWorkout(key, version)` never fall back to "latest" for an exact request.

## 19. Calendar moves

`FREQ.6D.1A §C4` scenario 1, re-confirmed against current code by the Explore agent's audit of `ScheduleRepairPersistenceService.cs`: `TryRescheduleToEmptySlotAsync` moves a session to a new date via `BuildReplacement`, which copies `SessionRole = source.SessionRole` verbatim (role preserved exactly) and links `AdaptedFromSessionId = source.Id` (lineage preserved). The same pattern applies to the new `CatalogPrescriptionProfileKey`/`Version` fields once added (§17) — since `BuildReplacement` is a field-copying factory, extending it to also copy the profile-lineage fields is a small, mechanical, additive change, not a new mechanism. Calendar placement must not, and per this factory's existing copy-based design **does not**, reassign PRIMARY/SECONDARY_CONTROLLED based on weekday order — the replacement's role/lane lineage travels with the row, never recomputed from its new date.

## 20. SubstituteFutureEasy semantics

Per `FREQ.6D.1B §B2` (re-confirmed correct, non-blocking): the residual question is narrow — does a `SubstituteFutureEasy` stand-in row (a future EASY day repurposed to cover a missed KEY) acquire a copy of the original KEY session's lane/profile lineage, or remain lineage-null? **Design decision for this phase, following `FREQ.6D.1B`'s own recommendation**: leave the stand-in row's `CatalogPrescriptionProfileKey`/`Version` **null** — it is executing as an EASY session, not the original KEY prescription, and falsely claiming the original KEY profile would misrepresent what was actually run. This does not affect Adaptation, which already (per `FREQ.6 §7`, frozen, unchanged) counts the *recovered priority root* under the original KEY/LONG role via `AdaptedFromSessionId`/`SupersededSessionId` lineage (confirmed real: `ScheduleRepairPersistenceService.TrySubstituteFutureEasyAsync` sets `target.PlanningStatus = Superseded` and returns both `ReplacementId`/`SupersededId`, already wired into `LongHorizonAdaptationDecisionRecord`) — independent of whether the stand-in row itself carries profile lineage. Two distinct concepts, exactly as this phase's own prompt requires: **`OriginalScheduledSlotLineage`** (the missed row's own, untouched `CatalogProgressionStageKey`/lane/profile fields — never rewritten) vs. **`CurrentExecutionPrescriptionLineage`** (the stand-in's own fields, correctly reflecting that it executed as EASY, not the original KEY dose). No athlete policy is invented — this reuses `FREQ.6 §7`'s already-frozen accounting rule exactly.

## 21. Adaptation inputs

`WindowExecutionSummary.KeySessionExpectedCount`/`KeySessionCompletedCount` (real, `AdaptationDomainContracts.cs:174-198`) — confirmed, via direct code read and the type's own doc comment, to be a **plain integer count pair**, generalized by `FREQ.4` specifically *to avoid needing per-KEY-instance identity*. `WindowExecutionSummaryBuilder` increments these counts purely by `Role == KeySession` + terminal outcome — no lane/profile/dose read anywhere in that path (re-confirmed: zero `LaneOrdinal`/`DoseCategory` references in the whole `Adaptation/` directory). This proves, structurally (not by coincidence), that **the two KEY lanes remain severity-symmetric** exactly as `FREQ.6 §13` requires (*"KEY1 and KEY2 are symmetric for adherence severity"*) — persisted lane/profile lineage (§17) is needed for **display/lineage/repair correctness**, never as an Adaptation severity input. Adaptation does not, and must not, distinguish lane identity in its counting; it only needs to know *how many* of the week's KEY roots completed.

## 22. Coordinated dual-KEY progression

"Coordinated" (`FREQ.6`'s own term) means, in implementation terms: **one shared weekly phase/stage-allocation timeline** (both lanes progress through Foundation→Build→RaceSpecific→Taper on the identical week boundaries, per the shared `phaseWeeks` input to each lane's independent `AllocatePhase` call, §8) **plus two deterministic, independently-resolved lane prescriptions for that same timeline** (§9's matrix). This is the model `FREQ.6D.1A §B2-B5` already worked out in full, empirically re-verified here against current code: each lane's `AllocatePhase` invocation is fully independent (no shared mutable capacity budget, no cross-lane coupling), yet both consume the *same* `phaseWeeks` list, so their week-by-week phase boundaries never diverge — this **is** the coordination: shared temporal skeleton, independent prescriptive content. No numeric prescription change is introduced by this framing; it is a description of how the already-frozen 4B/4C content and the already-existing allocator mechanism compose.

## 23. Phase transitions

**Phase alone** owns the profile-family switch (confirmed directly against §9's matrix — every phase×lane cell has exactly one profile in the V1 authored content, with no additional `ProgressionStage`-level branching required today). The architecture does not preclude finer `Phase + ProgressionStage` control (the `PrescriptionProfileCandidateKeys[]` field, §10, is declared per-*stage*, not per-*phase*, so a future authoring pass could introduce multiple stages within one phase, each with its own profile) — but for the currently-approved, currently-authored content, one stage per phase per lane is sufficient and no stale-profile-bleed risk exists: `CatalogWorkoutBinder`'s existing `ValidateInClosureAndPhase` check (`:82-103`, confirmed real) already rejects a resolved `WorkoutDefinition` whose `EligiblePhases` doesn't include the current dated week's phase — the same defense-in-depth applies transitively to profile-backed sessions once the profile's own `WorkoutDefinitionRef` is checked through this existing gate, with no new mechanism required.

## 24. Taper

Taper preserves both structural `KEY_SESSION` slots (per `FREQ.6 §10`'s frozen invariant, re-confirmed by `FREQ.6D.1B §B4`: *"RunLayout retains two identical structural `KEY_SESSION` roles"*) — `INTERMEDIATE_5D_TAPER_PRIMARY`/`INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` (§9) are both real, distinct, `VALIDATED` profiles, confirming the catalog content side never collapsed Taper to one KEY. Audited the skeleton/progression pipeline for an accidental single-quality-session assumption: `V1CatalogWorkoutRoleBindingPolicy` (§ below) treats `KEY_SESSION` generically by structural role, with no phase-specific branch anywhere (confirmed: `ModeFor`/`FixedDefaultWorkoutKeyFor` switch only on role string, never on phase) — nothing in the binder assumes fewer KEY slots during Taper. The only real gap is the *absent* `RUN_LAYOUT_5D` catalog artifact itself (§4) — a downstream, mechanical authoring task for a future phase, not an architectural defect uncovered here.

## 25. Binding option matrix

`WEEK_×_LANE_BINDING_MODEL_OPTIONS`

| Option | Semantic fidelity | Adaptation compatibility | Persistence complexity | Exact-ref determinism | Calendar-repair stability | Reuse 3D-7D | Future HM/Marathon | Implementation complexity |
|---|---|---|---|---|---|---|---|---|
| **D1**: week-level stage + per-lane exact profile mapping | High — matches `FREQ.6D.1A`'s already-proven-correct model | Full (§21) | Low (2 new nullable columns) | Full | Full (§19) | Full — `LaneOrdinal` scales from 1 (3D/4D) to N (future) generically | Full — no distance-specific coupling anywhere | Moderate — 4 layers touched (contract, allocator invocation, binder, planner), each additive |
| **D2**: Week×Lane independent stage *records* (separate persisted rows per lane per week, not just a tag) | Equivalent semantic fidelity to D1 | Equivalent | Higher — doubles row count for a concept that's naturally a tag, not a separate entity | Full | Full | Full, but heavier | Full, but heavier | Higher — no reuse of the existing single-invocation-per-phase allocator shape without restructuring its own output type |
| **D3**: per-session fully materialized prescription-binding records (a new, separate table joining session↔profile independent of `TrainingDay`) | Full | Full | Highest — a new persistence surface duplicating information `TrainingDay` could hold directly | Full | Requires its own repair-lineage logic, duplicating what `TrainingDay`'s existing `AdaptedFromId` chain already provides | Full | Full | Highest — new table, new migration, new read paths, duplicate lineage tracking |
| **D4**: another existing repository-native model | N/A | N/A | N/A | N/A | N/A | N/A | N/A | No repository-native alternative was found that already solves this — the closest existing pattern (`LongHorizonRollingSessionState`'s dark, disconnected persistence surface) was evaluated and rejected as a *template* (its `SessionOrdinal` is whole-week positional, not lane-keyed, per the Explore-agent audit) rather than adopted directly |

## 26. Selected architecture

**D1** — week-level stage allocation (already the existing mechanism, invoked once per lane per phase, §8) **plus** a per-lane `LaneOrdinal` tag threaded additively through the existing pipeline (`ScheduledProgressionWeek` → `BoundCatalogSession` → `TrainingDay`), **plus** exact profile references resolved once at the catalog-authoring/binding boundary (§10-§12) and carried thereafter as immutable lineage. Selected per the phase's own stated priority order: (1) frozen domain semantics — D1 is exactly `FREQ.6D.1A`'s already-evidence-grounded model, re-verified correct against current code; (2) deterministic lane identity — `LaneOrdinal` (catalog-authored) + structural binding ordinal (bind-time, §7); (3) exact profile references — §10-§12; (4) no duplicate authorities — reuses `ProgressionStageAllocator`, `CatalogWorkoutBinder`, `ExecutionPrescriptionIndex` verbatim, adds no parallel mechanism; (5) Adaptation compatibility — proven structurally lane-blind by construction (§21); (6) historical stability — §18; (7) generic 3D-7D extensibility — §27; (8) implementation complexity — the lowest of the four options, confirmed by direct comparison (§25).

## 27. Generic 3D–7D rule

`LaneOrdinal` is a per-role, per-week cardinality tag — never a boolean `IsSecondaryKey`. For 3D/4D (one `KEY_SESSION` slot per week), the design degenerates trivially: exactly one lane, `LaneOrdinal = 0`, matching today's existing single-slot behavior byte-for-byte (structural ordinal N ↔ `LaneOrdinal` N collapses to `(0,0)` when only one slot exists). For 5D/6D/7D, `RunLayout`'s own declared cardinality (already the canonical structural-role-count authority per this engagement's standing `RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY` invariant) determines N generically — the binder's slot-rank computation (§7) scales to however many `KEY_SESSION` slots a given layout declares, with no hardcoded "2" anywhere in the design. `EASY_SUPPORT`/`LONG_RUN` roles use the same generic slot-identity mechanism (`WeekNumber + StructuralRole + SlotOrderInWeek-derived-ordinal`) for consistency, without requiring prescription-profile machinery for those roles (§29) — one generic identity concept, two very different downstream consumption needs (profile-backed for `KEY_SESSION`, `FixedDefault` for the others, unchanged).

## 28. Legacy 3D/4D boundary

**Prefer generic architecture** (Option A per this phase's own §39): the new `LaneOrdinal`/binding fields are additive with a safe, degenerate default (`LaneOrdinal = 0` when only one slot exists), so 3D/4D/Beginner×4D require **zero behavioral change** — confirmed by the exact same reasoning `FREQ.6D.1A §D1` already proved: `CatalogPhaseWorkoutProgression.Stages` remains valid as today's implicit single-lane case (a loader-level normalization wraps existing bare `Stages[]` as one implicit lane, `LaneOrdinal=0`, if a new `Lanes[]` field is absent) — old published 3D/4D bundles remain byte-for-byte unchanged, loadable without modification, behaviorally identical. **Expected exact legacy delta: none** for any currently-published 3D/4D/Beginner×4D artifact; the new fields/tables are purely additive and inert until a real 5D `RunLayout`/combination is authored to exercise them.

## 29. Failure semantics

| Failure | Deterministic behavior |
|---|---|
| Duplicate `LaneOrdinal` in same role/week | Publish-time `DuplicateLaneOrdinalException` (new, catalog-side validator) |
| Missing `LaneOrdinal` for a multi-slot role | Publish-time `LaneCountMismatchException` — resolved lane count for a week ≠ `RunLayout`'s declared same-role slot count |
| Unsupported `LaneOrdinal` (binder sees more structural slots than declared lanes, or fewer) | Same `LaneCountMismatchException`, checked again at bind time as defense-in-depth |
| Profile dose-category mismatch | Publish-time, new validator enforcing `LaneOrdinal ↔ DoseCategory` (§11) |
| Profile phase mismatch | Reuses existing `ValidateInClosureAndPhase` (`CatalogWorkoutBinder.cs:82-103`), extended transitively via the profile's own `WorkoutDefinitionRef` |
| Exact profile missing (key not found) | `CatalogWorkoutBindingCandidateNotFoundException`-equivalent for profiles (new, mirrors the existing workout-definition exception exactly) |
| Exact profile version missing | Same as above — exact `(key, version)` miss, no fallback |
| Projection dependency missing | Existing `EXECUTION_PROJECTION_PROFILE_NOT_FOUND` (`CatalogBundleAssembler`, confirmed real) |
| Bundle execution prescription missing for a `ProfileBacked`-classified session | **Fail-closed, per §41 of this phase's prompt**: an explicitly `ProfileBacked` session with no resolvable exact execution prescription is an error (new `ExecutionPrescriptionBoundaryException` subtype), never silent legacy degradation |
| Ambiguous stage | Existing `CatalogWorkoutBindingAmbiguousCandidateException` (`:145-150`), reused verbatim, now scoped per-lane |
| Stage/profile mapping missing | New `CatalogWorkoutBindingMissingProfileCandidateException` (mirrors the existing `CatalogWorkoutBindingMissingCandidateReferenceException` for workouts) |
| Profile-backed session falling back to legacy | **Forbidden outright** — no fallback path exists or is designed; classification (§40) happens once, deterministically, at bind time |
| Persisted lineage inconsistent with bundle | Persistence-time check (§44): a `ProfileBacked` `TrainingDay` row must have non-null `CatalogPrescriptionProfileKey`/`Version` before save — enforced structurally, not by a runtime reconciliation pass |

## 30. Publish-time validations

Preferred, per this phase's own priority: `DuplicateLaneOrdinalException`, `LaneCountMismatchException` (static, checkable against `RunLayout`'s declared cardinality), the `LaneOrdinal↔DoseCategory` invariant (§11), missing exact profile, wrong phase/profile (extends the existing `ValidateInClosureAndPhase` pattern), duplicate exact profile identity (already enforced, `CatalogBundleAssembler`'s dedupe check), and unprojectable prescription (already enforced, `ExecutableWorkoutPrescriptionValidator`, proven in `FREQ.6D.3C`). All of these can be checked statically against the catalog source snapshot, before any real plan is generated — matching this engagement's established preference for fail-fast, publish-adjacent validation over deferred runtime discovery.

## 31. Generation-time validations

Cannot be checked until a real plan/candidate is materialized: week/lane cardinality against a specific horizon's actual dated skeleton (an 8-week Core plan may allocate phases differently than 14-week — §37), stage assignment for that specific candidate's `ProgressionStageAllocator.Allocate` run, session binding (`CatalogWorkoutBinder.BindAsync`'s per-slot resolution), and calendar lineage (the dated skeleton → `TrainingDay` materialization step). PlanCatalog static (publish-time) validation stays strictly separate from plan-generation dynamic validation — matching the existing, already-established boundary between `CatalogGraphValidator`/`PublishReadinessValidator` (PlanCatalog-side) and `BoundCatalogPlanValidator`/`DatedGeneratedCatalogPlanSkeletonValidator` (RunningApp-side).

## 32. Persistence-time validations

Guaranteed before `TrainingDay` save: every `ProfileBacked`-classified `KEY_SESSION` row has non-null, exact, immutable `CatalogPrescriptionProfileKey`/`Version` (§17) — no partially-bound persisted row. This mirrors the existing implicit guarantee for `CatalogWorkoutDefinitionKey`/`Version` (already always populated together for catalog-sourced rows, confirmed by inspection of `CatalogPlanConfirmationService.BuildCatalogTrainingDay`, the existing row-construction site) — the new profile fields follow the identical all-or-nothing construction discipline, not a new validation mechanism.

## 33. Authority map

`DUAL_KEY_AUTHORITY_MAP`

| Datum | Authoritative owner | Materialized at | Persisted? | Consumer | Must-not-own |
|---|---|---|---|---|---|
| Structural role | `RunLayout` (catalog) | Catalog authoring | Yes (`CatalogStructuralRole`) | Binder, planner, adaptation | Binder must not invent roles |
| `LaneOrdinal` | New `CatalogWorkoutProgressionLane` (catalog) | Catalog authoring | No new column — reconstructible via `CatalogProgressionStageKey` (§17) | Binder (structural-ordinal binding rule, §6/§7) | Publisher, RunningApp consumer, Adaptation must never re-derive it |
| Phase | Dated skeleton (existing) | Plan materialization | Yes (`CatalogPhaseKey`) | Binder, planner | — |
| `ProgressionStage` | `ProgressionStageAllocator` (existing, invoked per-lane) | Plan materialization | Yes (`CatalogProgressionStageKey`) | Binder | — |
| `DoseCategory` | `WorkoutPrescriptionProfile.DoseCategory` (catalog, validated against `LaneOrdinal`, §11) | Catalog authoring | No (derivable from profile key) | Publish-time validator only | RunningApp must never branch on it |
| Profile key/version | Progression stage's `PrescriptionProfileCandidateKeys[]` (catalog, new) | Catalog authoring + bind time | **Yes, new columns** (§17) | `ExecutionPrescriptionIndex.ResolveExact` | RunningApp must never select it |
| `WorkoutDefinition` key/version | Profile's own `WorkoutDefinitionRef` (existing) | Catalog authoring | Yes (existing columns) | Binder (`ValidateInClosureAndPhase`) | 5D binding layer must not resolve independently (§11) |
| Execution prescription | `WorkoutPrescriptionExecutionProjector` (existing) | Publish time | No (bundle-only, §17) | `ExecutionPrescriptionIndex` | RunningApp must never recompute |
| Calendar date | Dated skeleton / repair (existing) | Plan materialization / repair | Yes (`Date`) | Everything downstream | Never determines lane |
| Completion state | `TrainingDay.Status` (existing) | Athlete action | Yes | Adaptation | — |
| Adaptation role lineage | `WindowExecutionSummary` counts (existing, `FREQ.4`) | Adaptation evaluation | No (derived from `TrainingDay`/`LongHorizonRollingSessionState` rows) | `NextWindowLoadDecisionPolicy` | Must remain lane-blind by construction (§21) |

## 34. Target dataflow

```
RunLayout (catalog, existing — RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY)
  → dynamic weekly skeleton (CatalogPlanSkeletonMaterializer, existing)
  → lane assignment: structural binding ordinal derived from SlotOrderInWeek (NEW — CatalogWorkoutBinder, §7)
  → progression/stage resolution: ProgressionStageAllocator.AllocatePhase invoked once per lane (EXISTING algorithm, NEW per-lane invocation loop — CatalogWorkoutProgressionLane wrapper, §8/§11)
  → exact profile mapping: stage's PrescriptionProfileCandidateKeys[] resolved exactly (NEW field on CatalogWorkoutProgressionStage; resolution reuses CatalogWorkoutBinder's existing exact-pin pattern, §10)
  → exact projection dependency: ExactPrescriptionProjectionDependency assembled from the resolved exact profile set (EXISTING type, FREQ.6D.3C; NEW catalog-side assembly step supplying it, §12)
  → bundle execution library: CatalogBundleAssembler.Assemble(..., executionDependencies) (EXISTING, FREQ.6D.3C)
  → generated session binding: BoundCatalogSession gains KeySessionLaneOrdinal + PrescriptionProfileKey/Version (NEW additive fields, §7/§16)
  → dated calendar session: unchanged dated-skeleton→TrainingDay materialization path, extended to copy the new fields (CatalogPlanConfirmationService.BuildCatalogTrainingDay, EXISTING site, NEW fields copied)
  → persistence: TrainingDay gains CatalogPrescriptionProfileKey/Version columns (NEW migration, §17)
  → RunningApp read: ExecutionPrescriptionIndex.ResolveExact(profileRef) (EXISTING, FREQ.6D.3D, now wired — NEW: CatalogSessionPrescriptionSource classification point actually invoked, §40)
  → completion/adaptation: WindowExecutionSummary counts (EXISTING, FREQ.4, unchanged) → NextWindowLoadDecisionPolicy (EXISTING type, NEW severity-table branch for 5-session weeks — the already-FREQ.6-approved 24-row table, currently unimplemented, §21/§32 of the change manifest below)
```

Unknowns explicitly disclosed, not invented: the exact catalog-authoring-time step that assembles the combination-wide `ExactPrescriptionProjectionDependency` list (§12) has no existing named type yet — it is new, narrow glue code, not a redesign of anything.

## 35. Session-lineage transition matrix

`SESSION_LINEAGE_TRANSITION_MATRIX`

| Scenario | Original Role | LaneOrdinal | Stage | Profile lineage | Current executable prescription | Adherence/severity lineage |
|---|---|---|---|---|---|---|
| Normal scheduled KEY | `KEY_SESSION` | Set at bind time, immutable | Set at bind time | Exact `(ProfileKey, Version)`, immutable | Resolved via `ResolveExact` | Counts toward `KeySessionExpectedCount` |
| Calendar-moved KEY | Unchanged (row moves) | Unchanged (travels with row) | Unchanged | Unchanged | Unchanged | Unchanged — same row, new date only |
| Completed KEY | Unchanged | Unchanged | Unchanged | Unchanged | Unchanged | Counts toward `KeySessionCompletedCount` |
| Missed KEY | Unchanged | Unchanged | Unchanged | Unchanged (row remains, marked missed) | Unchanged | Counted as not-completed; original row untouched |
| Not Today KEY | Unchanged | Unchanged | Unchanged | Unchanged | Unchanged | Handled per existing `NotTodayReason`/recovery mechanism, unaffected by this design |
| Substituted future EASY replacing KEY | New row: `EASY_SUPPORT` | Null (not applicable — see §20) | Null | **Null, deliberately** (§20) | `FixedDefault` (EASY), not the original KEY prescription | Original KEY's severity outcome preserved via `AdaptedFromSessionId`/`SupersededSessionId` lineage (`FREQ.6 §7`, unchanged) |
| Rescheduled session | Unchanged | Unchanged | Unchanged | Unchanged | Unchanged | Same row, `AdaptedFromId` chain if applicable |
| Future session after Progress | `KEY_SESSION` (next window) | Resolved fresh per §7 for that future week | Resolved fresh (next stage in that lane's progression) | Resolved fresh, exact | Resolved fresh | N/A (future) |
| Future session after Maintain | Same as above | Same | Same (stage may repeat, per existing allocator behavior) | Same | Same | N/A |
| Future session after Reduce | Same as above | Same | Same (stage/profile selection for "Reduce" is a FREQ.6/6C-adjacent product mechanism, explicitly out of this phase's scope per `FREQ.6D.1A §C4` scenario 3) | Same | Same | N/A |

## 36. 12-week conceptual trace

Representative Intermediate×5D 12-week Core plan (no new dosage, tracing profile *family* only, per §51 of the phase prompt):

| Week | Phase | Lane0 profile family | Lane1 profile family |
|---|---|---|---|
| 1 (Foundation begin) | Foundation | `INTERMEDIATE_5D_FOUNDATION_PRIMARY` (aerobic-strength intro) | `INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED` (threshold intro) |
| ~3 (Foundation end, illustrative) | Foundation | Same family, same stage (V1: one stage per phase, §9) | Same family |
| ~4 (Build begin) | Build | `INTERMEDIATE_5D_BUILD_PRIMARY` (threshold pace) | `INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED` (surge/fartlek) |
| ~7 (Build end, illustrative) | Build | Same family | Same family |
| ~8 (RaceSpecific begin) | RaceSpecific | `INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY` (goal pace) | `INTERMEDIATE_5D_RACE_SPECIFIC_SECONDARY_CONTROLLED` (threshold support) |
| ~10 (RaceSpecific end, illustrative) | RaceSpecific | Same family | Same family |
| 11-12 (Taper) | Taper | `INTERMEDIATE_5D_TAPER_PRIMARY` (goal-pace sharpening) | `INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` (strides sharpening) |

Both lanes transition phase boundaries on **identical week numbers** (shared `phaseWeeks`, §8/§22 — coordinated), while each lane's own profile identity is resolved completely independently within that shared timeline (§9 — independent). No numeric dosage was introduced; illustrative week numbers are placeholders for whatever `ProgressionStageAllocator` actually allocates for a given horizon (§37).

## 37. 8/10/12/14 horizon check

No architectural assumption in this design depends on exactly 12 weeks. `ProgressionStageAllocator.AllocatePhase` already handles variable `phaseWeeks.Count` per horizon (the only per-horizon-varying input today, confirmed `FREQ.6D.1A §B8`), and this design invokes that same, unmodified algorithm once per lane — so 8/10/12/14-week horizons all flow through identically, with each lane's compression/extension math scoped to its own stage list against whatever week budget that horizon's phase allocation produces (worked examples already proven correct in `FREQ.6D.1A §B3-B5` for exactly this variability, re-confirmed applicable since nothing in the allocator itself changes). Profile mapping (§9's matrix) derives from `(Phase, LaneOrdinal)` only — never from week count — so it is horizon-invariant by construction.

## 38. Implementation change manifest

| Category | Classification | Detail |
|---|---|---|
| CORE CONTRACT (`PlanCatalog.Core`/`Contracts`) | `NO_CHANGE` | `WorkoutPrescriptionProfile`, `ExecutableWorkoutPrescription`, `ExactPrescriptionProjectionDependency` all already exist and are sufficient (`FREQ.6D.2`-`3D` era) |
| PLAN-CATALOG MODEL | `CHANGE_REQUIRED` | Add `Lanes[]` to `CatalogPhaseWorkoutProgression` (additive, D1's normalization preserves bare `Stages[]` as implicit single lane); add `PrescriptionProfileCandidateKeys[]` to `CatalogWorkoutProgressionStage`; new `LaneOrdinal↔DoseCategory` and lane-coordination validators (§29/§30) |
| PROGRESSION MATERIALIZER (RunningApp `ProgressionStageAllocator`/`CatalogWorkoutProgressionDefinition` mirrors) | `CHANGE_REQUIRED` | Backend-side mirror types need the same additive `Lanes[]`/lane-tag fields (this codebase deliberately mirrors PlanCatalog types rather than referencing them, per the established `CatalogStageCompressionBehavior` precedent) |
| BINDING MODEL (`CatalogWorkoutBinder`, `BoundCatalogPlanContracts`) | `CHANGE_REQUIRED` | `stageWeeksByNumber` → `(WeekNumber, LaneOrdinal)`-keyed lookup; `BoundCatalogSession` gains `KeySessionLaneOrdinal`, `PrescriptionProfileKey`, `PrescriptionProfileVersion` |
| BUNDLE ASSEMBLER (PlanCatalog `CatalogBundleAssembler`) | `NO_CHANGE` | Existing exact-dependency overload (`FREQ.6D.3C`) already sufficient — only its *caller* (the new catalog-authoring glue, §34) needs to exist |
| RUNNINGAPP APPLICATION | `CHANGE_REQUIRED` | `CatalogSessionPrescriptionPlanner` reads `session.KeySessionLaneOrdinal` instead of recomputing `keyOrdinal` (§7); `CatalogSessionPrescriptionSource` actually wired into the live session-building branch (currently proven-but-dormant, `FREQ.6D.3D`); `PublishedTemplateBundleJsonReader` wired into `PlanCatalogBundleLoader.LoadCandidateAsync` for profile-backed candidates |
| PERSISTENCE DOMAIN (`TrainingDay`) | `CHANGE_REQUIRED` | Add `CatalogPrescriptionProfileKey` (`string?`), `CatalogPrescriptionProfileVersion` (`int?`) — nullable, additive |
| DATABASE MIGRATION | `CHANGE_REQUIRED` | See §17/§39 — one small, additive EF migration, matching the existing `AddPlanCatalogProvenanceFields` precedent |
| ADAPTATION INPUT (`NextWindowLoadDecisionPolicy`) | `CHANGE_REQUIRED` | Implement the already-`FREQ.6`-approved, already-24-row-verified (`FREQ.6D.1B` Track A) 5-session severity table — **not a new product decision**, an unimplemented consequence of an already-frozen one; current code's own doc comment (`:30-48`) explicitly and correctly flags this as unimplemented, but incorrectly frames it as awaiting "a future decision phase" — that phase already happened (`FREQ.6`) |
| TESTS | `CHANGE_REQUIRED` | Full new targeted matrix per each layer above, plus full existing regression baseline re-verified green (per this engagement's standing practice) |

## 39. Database migration decision

```
MIGRATION_REQUIRED
```

Existing `TrainingDay` fields are **not** sufficient — no column exists for prescription-profile key/version (confirmed directly, §17). Exact new fields: `CatalogPrescriptionProfileKey` (`string?`, nullable/additive), `CatalogPrescriptionProfileVersion` (`int?`, nullable/additive) — same shape and naming convention as the existing `CatalogWorkoutDefinitionKey`/`CatalogWorkoutDefinitionVersion` pair. No migration is created in this phase (architecture only); the exact fields are specified precisely enough for a later implementation phase to author it without further design work.

## 40. API/public-contract consequence

`PUBLIC_CONTRACT_ZERO_DELTA` — expected default confirmed correct: no real product/API requirement was found anywhere in this research for exposing `LaneOrdinal`, `PrescriptionProfileVersion`, or internal stage identity on any public DTO (`HomeResponse`, `TrainingDayDetailResponse`, etc. — checked directly, both currently expose only athlete-facing display fields, e.g. `PlannedDistanceKm`/`Title`/`Description`, never catalog implementation identity). Home/training-day behavior can render the correct athlete-facing prescription (distance, duration, intensity — already sourced from `CatalogPrescriptionJson`/legacy fields today, and would source from the new `ExecutableWorkoutPrescription`-backed fields once wired) without leaking `LaneOrdinal` or profile version numbers to any client. No API DTO change is proposed.

## 41. Implementation split

Recommended, evidence-based decomposition (mirroring `FREQ.6D.1A §E`'s already-proven-sound dependency analysis, re-validated against current code and updated for what's actually already implemented vs. still needed):

- **A. Slot/lane identity + stage binding** (PlanCatalog: `Lanes[]`, `PrescriptionProfileCandidateKeys[]`, lane-coordination validators; RunningApp: mirror types, `CatalogWorkoutBinder`'s `(WeekNumber, LaneOrdinal)` lookup, `BoundCatalogSession` new fields) — the highest-regression-risk layer, touches shared single-lane code paths, must be reviewed most carefully and re-verified against the full existing 3D/4D/Beginner×4D regression baseline at this exact point.
- **B. Exact profile dependency materialization + bundle** (the new catalog-authoring-time glue that assembles `ExactPrescriptionProjectionDependency` lists for a real 5D combination; wiring `PublishedTemplateBundleJsonReader` into `PlanCatalogBundleLoader`) — depends on A's `LaneOrdinal`/stage resolution existing.
- **C. RunningApp session lineage** (`CatalogSessionPrescriptionPlanner` reading `KeySessionLaneOrdinal`; `CatalogSessionPrescriptionSource` actually wired live) — depends on A and B.
- **D. Persistence/Adaptation integration** (`TrainingDay` migration; `NextWindowLoadDecisionPolicy`'s 5-session table; repair/substitution field-copying extension, §19/§20) — depends on A-C existing; the Adaptation severity-table work is independently testable against the already-frozen `FREQ.6 §6` 24-row table (`FREQ.6D.1B` Track A) without needing A-C to land first, so it **may proceed in parallel** with A-C once this architecture is approved.
- **E. Integrated closure** — full regression, 5D dual-KEY targeted end-to-end tests, historical-compatibility re-verification (old bundles load unmodified), the actual `RUN_LAYOUT_5D`/combination catalog authoring (§4/§24's disclosed gap) needed to exercise the design against a real candidate for the first time.

Not fabricating phase IDs beyond this immediate roadmap need; the next concrete phase (§below) covers Split A only, as the narrowest safe first increment per this phase's own explicit instruction not to assume one giant commit.

## 42. Readiness

All fifteen conditions from this phase's own §54 checked:

| Condition | Status |
|---|---|
| Canonical lane identity | Resolved — `WeekNumber + StructuralRole + LaneOrdinal` (§5) |
| Assignment point | Resolved — catalog-authored `LaneOrdinal`, bind-time structural ordinal (§6/§7) |
| Deterministic ordering | Resolved — `SlotOrderInWeek`-derived rank (§7) |
| Stage ownership | Resolved — per-lane independent `AllocatePhase` invocation (§8) |
| Stage coordination model | Resolved — shared `phaseWeeks`, independent per-lane allocation (§22) |
| Exact profile selection authority | Resolved — progression stage, catalog-side (§10) |
| Dependency cardinality | Resolved — distinct-profile-set, not per-slot (§13) |
| Bundle semantics | Resolved — unique library indexed by profile identity (§14) |
| RunningApp lookup contract | Resolved — `ResolveExact(VersionedCatalogReference)`, already implemented (§15) |
| Persistence lineage | Resolved — 2 new nullable `TrainingDay` columns (§17/§39) |
| Calendar-move preservation | Resolved — existing field-copying factory pattern, confirmed already correct (§19) |
| Substitution lineage | Resolved — `OriginalScheduledSlotLineage` vs `CurrentExecutionPrescriptionLineage`, null-on-substitute (§20) |
| Adaptation input | Resolved — already lane-blind by construction (§21); severity-table implementation is a disclosed, scoped, non-blocking code gap (§38) |
| Failure semantics | Resolved — full table (§29) |
| Legacy zero-delta boundary | Resolved — additive, degenerate-default design; zero expected delta (§28) |

No item remains a product/domain decision for implementation to make silently — every remaining item is scoped engineering work, consistent with `FREQ.6D.1B`'s own finding that the three items it flagged as potentially-open were all already answered by frozen `FREQ.6` text.

```
ARCHITECTURE READY FOR IMPLEMENTATION
```

## Final classification

**`FREQ6D4D_ARCHITECTURE_APPROVED_MULTI_PHASE_IMPLEMENTATION_REQUIRED`**

The architecture is fully resolved (§42) and requires a real, non-trivial, multi-layer implementation (PlanCatalog model + schema, RunningApp binder/planner/persistence, one Adaptation code fix, one EF migration) — correctly decomposed (§41) rather than attempted as one commit. This is not `FREQ6D4D_DUAL_KEY_INTEGRATION_ARCHITECTURE_APPROVED` (that classification implies a narrow, single-increment closure the real scope does not support) and not a persistence-migration-only classification (the migration is real but is only one of several required, coordinated layers). No unresolved stage/lane authority, no unresolved session-lineage architecture, and no undiscovered product/domain-policy gap remain — `FREQ.6D.4D` is architecturally closed; dual-KEY production integration itself remains **not yet implemented**.
