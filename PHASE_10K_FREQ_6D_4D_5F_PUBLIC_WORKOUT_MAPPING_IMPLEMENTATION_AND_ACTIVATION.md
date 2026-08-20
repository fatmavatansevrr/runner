# PHASE 10K-FREQ.6D.4D.5F — Public Workout-Type Mapping Implementation, Real-5D Mapping Completeness Gate & Fourth Intermediate×5D Public Activation Retry

**Type:** IMPLEMENTATION + INTEGRATED VERIFICATION
**Parent phase:** FREQ.6D.4D.5E
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Everything below is re-derived from the current repository state.

---

## 1. Preflight

- `git rev-parse HEAD` at start: `92259f75f0be330615ee862c960c153b97ca582e`.
- `git branch --show-current`: `main`.
- `git status --short` (non-build-output) at start: `m baseline_tmp`, `M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` — pre-existing, unrelated, preserved untouched throughout.
- `git rev-list --left-right --count origin/main...HEAD`: `0  12`.
- `git diff --check`: clean.
- Commits `a9acb05`/`92259f7` confirmed reachable from `HEAD` via `git merge-base --is-ancestor`.
- `FREQ.6D.4D.5E` report re-read in full; final classification `INTERMEDIATE_5D_PUBLIC_WORKOUT_MAPPING_CLOSURE_APPROVED` confirmed.
- `FREQ.6D.4D.5D` and `FREQ.6D.4D.5B` reports re-read for the calendar-materializer and Taper-validator history this phase must not touch.
- Phase ID: `FREQ.6D.4D.5F`, per `MASTER_ROADMAP.md`'s own `[Next, not yet scheduled]` pointer written at the end of `5E`.

---

## 2. FREQ.6D.4D.5E authority re-verified from its own report

- Real Intermediate×5D closure: 8 stage/lane combinations → 6 distinct (WorkoutDefinition, StructuralRole) publicly-relevant pairs.
- Exactly one gap: `AEROBIC_STRENGTH_CONTROLLED_INTRO` (Foundation Primary, LaneOrdinal 0).
- Decision: map to the **existing** `GeneratedCatalogWorkoutType.Interval` — no new public type, no taxonomy extension.
- Mapping ownership confirmed key-only (no `WorkoutDefinitionVersion` branch).
- `V1CatalogPublicWorkoutTypeMappingPolicy` confirmed the sole mapping authority.
- Fail-closed behavior for unknown workouts must be preserved.

All five re-confirmed by re-reading the report; none contradicted by this phase's own investigation.

---

## 3. Files inspected

`CatalogPublicPreviewMaterializer.cs` (mapping policy), `V1CatalogPilotIdentityPolicy.cs` (routing), `ten-k-workout-progression.v6.json`, all 8 `intermediate-5d-*.v1.json` profiles, `run-layout-5d.v1.json`, `ten-k-5d-intermediate.v1.json`, `V1CatalogWorkoutRoleBindingPolicy.cs` (fixed EASY_SUPPORT/LONG_RUN default constants), `PublishedTemplateBundleLoader.cs`, `CatalogArtifactFileResolver.cs`, `PlanCatalogBundleLoader.cs`, `PlanCatalogDeploymentAuthority.cs` (`PlanCatalogRootResolver`), `CustomWebApplicationFactory.cs`, `appsettings.json`/`appsettings.Development.json`, `CatalogPreviewGenerator.cs` (both the exact-12-week "preferred" pipeline and the `CompressedCore`/`ExtendedCore` dynamic-orchestration branch), `Freq6D4D5BReal5DDarkPlanTests.cs`, `Gen3BThreeDayPublicActivationTests.cs`, `PublishedCatalogTestRelease.cs`, `Phase4F8_2LivePilotRoutingTests.cs`, `DynamicCoreCalendarMaterializationOrchestratorTests.cs`.

---

## 4. Files changed (final state)

| File | Classification |
|---|---|
| `backend/RunningApp.Application/RuntimeCatalog/Schedule/CatalogPublicPreviewMaterializer.cs` | `PUBLIC_WORKOUT_TYPE_MAPPING` |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Freq6D4D5FPublicWorkoutTypeMappingTests.cs` (new) | `REAL_5D_MAPPING_COMPLETENESS_GATE` / `TEST` |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` | `SUPPORT_MATRIX` (doc-comment-only net change — see §6/§15) |
| `PHASE_10K_FREQ_6D_4D_5F_PUBLIC_WORKOUT_MAPPING_IMPLEMENTATION_AND_ACTIVATION.md` (new) | `DOCUMENTATION` |
| `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` | `LEDGER` / `ROADMAP` |

No `UNEXPECTED` files. `Gen5DIntermediatePublicActivationTests.cs` and two scratch probe files were created, used for diagnosis, and deleted before the final commit — see §15/§30; they leave no trace in the final diff.

---

## 5. Mapping implementation

Added exactly one arm to `V1CatalogPublicWorkoutTypeMappingPolicy.Map`'s switch, alongside the existing wildcard-stage arms:

```csharp
("AEROBIC_STRENGTH_CONTROLLED_INTRO", "KEY_SESSION", _) => GeneratedCatalogWorkoutType.Interval,
```

No alias, no prefix matching, no default-mapping fallback, no frequency-specific condition, no version branch. Placed before the `default` arm's `throw`, after the other five arms, preserving their existing order.

---

## 6. Mapping authority and key/version semantics

`V1CatalogPublicWorkoutTypeMappingPolicy` remains the sole mapping owner (only production call site: `CatalogPublicPreviewMaterializer.MapSession`, confirmed unchanged this phase via the same single-call-site check `5E` already performed). The new arm is **key-only**: it does not reference `session.WorkoutDefinitionVersion` at all, matching every other arm and `5E`'s confirmed key-level ownership decision. Verified directly by test (`AerobicStrengthControlledIntro_KeySession_MapsToInterval_AnyVersion`, theory over versions 1/2/3 — all map to `Interval`).

---

## 7. Unknown-workout fail-closed behavior

Unchanged. `UnknownWorkoutKey_StillFailsClosed` proves `CatalogPublicWorkoutTypeUnsupportedException` is still thrown for an unmapped key; no `default -> Interval`/`Easy`/`Other` fallback was added.

---

## 8. Existing mapping regressions

`ExistingMappingArms_RemainUnchanged` (theory, 7 cases) proves every pre-existing arm — `EASY_STANDARD`×2 role/stage combinations, `LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K` — still maps identically. Full regression (§29) additionally confirms zero delta across the whole suite.

---

## 9. Real 5D closure derivation (test-time, not hardcoded)

`Freq6D4D5FPublicWorkoutTypeMappingTests.DeriveRealProgressionClosure()` parses the real, published `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v6.json` (copied into the test project's own build output — the same real file the runtime reads) at test-run time via `System.Text.Json`, walking `phaseProgressions[].lanes[].stages[].workoutCandidates[0]`. This is not a hand-maintained list; it re-derives the closure fresh, exactly as `5E`'s own report instructed for the next phase.

---

## 10. Closure cardinality reproduction

`RealProgressionClosure_HasEightStageLaneCombinations` and `RealProgressionClosure_ReducesToSixDistinctPubliclyRelevantPairs` reproduce `5E`'s own closure numbers from code: 8 stage/lane combinations across 4 phases × 2 lanes, reducing to 6 distinct (WorkoutDefinitionKey, StructuralRole) pairs once the fixed `EASY_SUPPORT`/`LONG_RUN` defaults (read from `V1CatalogWorkoutRoleBindingPolicy`'s own constants, not re-typed literals) are folded in. Both numbers matched on first run — no catalog-truth drift was found, so no STOP was triggered under §11's own instruction.

---

## 11. Exhaustive mapping completeness gate

`RealIntermediateFiveDay_EveryPubliclySurfacedWorkout_MapsExactlyOnceAndDeterministically` is the load-bearing gate: for every one of the real closure's 8 entries plus the 2 fixed defaults, it constructs a representative `CatalogPrescribedSession` and asserts `V1CatalogPublicWorkoutTypeMappingPolicy.Map` succeeds, and that calling it twice on the same session yields the same result (determinism / exactly-one-mapping). Green on first run — the closure gate this phase's own §24 (from `5E`) called for is now real, not aspirational.

---

## 12. Foundation-Primary public mapping (policy level)

`FoundationPrimary_RealWorkout_MapsToInterval` reads the real closure entry for `(FOUNDATION, LaneOrdinal 0)`, asserts it is `AEROBIC_STRENGTH_CONTROLLED_INTRO` (not assumed), and asserts the mapped public type is `Interval`.

---

## 13. Representative other real-5D mappings

`BuildPrimary_RealWorkout_MapsToTempo` (`THRESHOLD_TEMPO` → `Tempo`), `RaceSpecificPrimary_RealWorkout_MapsToInterval` (`GOAL_PACE_TEN_K` → `Interval`), `TaperBothLanes_RealWorkouts_MapCorrectly` (`GOAL_PACE_TEN_K`/`FARTLEK`, both lanes → `Interval`) — all read the real closure and assert against the real policy, not a hardcoded identity.

---

## 14. Pre-activation gate (§15 of the phase prompt)

All required items ran green **at the policy/unit level** before any routing was touched:

- A. Direct new-mapping test — green (§5/§6 above).
- B. Unknown-workout fail-closed — green (§7).
- C. Existing mapping regressions — green (§8).
- D. Real Intermediate×5D exhaustive closure mapping test — green (§11).
- E–I (real dark generation, multi-KEY calendar, both Taper validators, profile-backed prescription, persistence/adaptation regressions) — covered by the full regression run, see §28/§29; none of these were touched this phase and all remained green.

Only after A–D were green did this phase proceed to §15 (routing retry), per the phase's own required order.

---

## 15. Fourth public-activation retry — attempted, then reverted (the new, fourth blocker)

The exact previously-reverted routing widening was re-applied: `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency`/`ResolveCandidate` widened to include `(Intermediate, 5)`, reusing the already-retained `FiveDayCandidateKey`/`FiveDayCandidateVersion` constants — no routing redesign.

Real HTTP E2E testing (via `CustomWebApplicationFactory`'s default Development configuration — the real, git-committed `plan-catalog/catalog` authoring tree as `CatalogRootPath`, sibling to the real, committed `plan-catalog/artifacts/appsel-plan-catalog/1.1.0/` release whose `bundles/TEN_K__5D__INTERMEDIATE.v1.json` is real; `LocalCatalogAcceptance`'s existing Development-only override, unrelated to and untouched by this phase, treats the real VALIDATED candidate as eligible for local HTTP testing exactly as this repository's own `Freq4A` real-coverage tests already do — empirically verified first against the known-good Intermediate×3D cell, which returned 200 as expected) found:

- **8-week, 10-week, 14-week previews: 500 Internal Server Error**, root cause `CatalogSessionPrescriptionMissingExecutionPrescriptionException`: *"Week 1 KEY_SESSION is ProfileBacked (profile 'INTERMEDIATE_5D_FOUNDATION_PRIMARY' v1) but no ExecutionPrescriptionIndex was supplied to resolve it against."*
- **12-week preview: succeeds.**

This is deterministic and fully reproducible (confirmed across three separate isolated test runs), not flaky — ruling out a test-infrastructure race. Root-caused by direct code inspection: `CatalogPreviewGenerator` has **two separate internal pipelines**. The "exact-12-week preferred" pipeline (the class's own main body) calls `IPublishedTemplateBundleLoader.TryLoadAsync` and threads the resulting `ExecutionPrescriptionIndex` into the session-prescription request. The **`CompressedCore`/`ExtendedCore` branch** — used for every horizon that is *not* exactly the candidate's preferred 12 weeks (i.e. 8, 9, 10, 11, 13, 14 for this candidate) — constructs its own, separate composition chain (built before `FREQ.6D.4D`'s ProfileBacked/`ExecutionPrescriptionIndex` work existed, originally for pure-Legacy 3D/4D content, which never needed an execution index) and **never threads the published-bundle execution index into it at all**.

This is a genuine, real, independent (fourth) blocker — not caused by, and not fixable within the scope of, this phase's mapping work:

- It is unrelated to the workout-type-mapping gap this phase closed (§5–§13 all pass in complete isolation from routing).
- Fixing it would require touching `CatalogPreviewGenerator`'s dynamic-orchestration composition (threading `IPublishedTemplateBundleLoader`/`ExecutionPrescriptionIndex` into the `CompressedCore`/`ExtendedCore` branch) — explicitly out of this phase's declared scope (§18: "Do NOT modify... bundle discovery... unless public retry exposes a NEW independent blocker. If that occurs: STOP.").
- Per §36/§37's own explicit instruction — "If A/B succeed but C-I hit a NEW independent blocker: STOP and classify it separately. Do not undo the valid mapping fix merely because activation remains blocked elsewhere" and "retain independently correct mapping implementation if safe" — the mapping fix (§5) and its completeness gate (§11) are **retained**; only the routing widening was reverted.

**Action taken:** `V1CatalogPilotIdentityPolicy` reverted a fourth time (back to not including `(Intermediate, 5)`), with an updated doc comment recording this exact finding for the next phase (deliberately not naming the internal orchestrator class in that comment, to avoid tripping the repository's own dark-reachability fitness test — see §30). The scratch E2E test file used to find this (`Gen5DIntermediatePublicActivationTests.cs`) and two throwaway diagnostic probe files were deleted before the final commit, per the same convention `5D`/`5B` already established for reverted-capability test files.

---

## 16-19. 8/10/12/14-week public preview results

- 8-week: 500 (blocked, §15).
- 10-week: 500 (blocked, §15).
- 12-week: 200, real `TEN_K__5D__INTERMEDIATE` candidate selected, 5 sessions/week confirmed during the diagnostic run (before revert).
- 14-week: 500 (blocked, §15).

Since public routing was reverted, none of these are reachable via the real public route in the final committed state — `TEN_K__5D__INTERMEDIATE` remains fully dark.

---

## 20-24. Confirmation / DB / calendar / Taper-persisted / adaptation

Not reached — public routing is reverted, so no public confirmation, persistence, or read-path testing against the live route was possible or attempted in the final state. (During the diagnostic window, before reverting, no confirmation/persistence test was run either — the preview-level 500s for 3 of 4 horizons were sufficient to trigger the STOP condition before proceeding further down the pre-activation gate's own required order.)

---

## 25. Unsupported neighbors

Not applicable to verify against live routing this phase (routing reverted), but confirmed structurally unaffected: `V1CatalogPilotIdentityPolicy`'s allow-list is unchanged from its pre-phase state (Intermediate 3/4, Beginner 4 only) — Beginner×5D, Advanced×5D, Intermediate×6D/7D were never touched.

---

## 26. No-silent-4D-coercion proof

Structural: `ResolveCandidate`'s `(Intermediate, 5)` arm is absent again, so an `(Intermediate, 5)` request falls through to `IsSupportedLevelFrequency`'s `false` result and is routed Legacy by `V1LiveCatalogPilotRoutingPolicy` (unchanged), never silently substituting `TEN_K__4D__INTERMEDIATE`/`RUN_LAYOUT_4D`.

---

## 27. Inventory / packaging

Not re-run this phase — no catalog file was added or changed (only one C# switch arm and test files), so no inventory delta is expected or was investigated further.

---

## 28. Legacy 3D/4D/Beginner×4D regression

Full regression (§29) confirms zero delta. `Phase4F8_2LivePilotRoutingTests.cs`'s `DaysPerWeek=5` "non-pilot" mutation case, temporarily changed to `6` during the widening attempt, was reverted back to its original `5` value alongside the routing revert — byte-identical to its pre-phase state (confirmed via `git status` showing no diff on that file in the final commit).

---

## 29. Full regression

- **RunningApp.IntegrationTests (full suite):** 3671/3672 passing on the final, corrected run (1 pre-existing, unrelated failure — `Sw09ExplicitZeroReadinessEndToEndTests`, confirmed present in this session's very first background-test notification, before any work in this phase began; not caused by this phase). An intermediate run (before a doc-comment fix, see §30) additionally showed one *new* failure, `DynamicCoreCalendarMaterializationOrchestratorTests.LiveReachability_OnlyCatalogPreviewGeneratorMayCallTheOrchestrator`, self-inflicted and fixed within this phase (§30) — not a real architectural regression.
- **PlanCatalog.Tests:** 1510/1510, zero delta (no PlanCatalog file was touched).
- **Debug build:** clean, 0 warnings, 0 errors.
- **`git diff --check`:** clean throughout.
- PostgreSQL: confirmed healthy (`docker ps`, `appsel-dev-postgres` container up) for the full regression's DB-backed subset.

---

## 30. Self-found-and-fixed issue during this phase

The revert comment written into `V1CatalogPilotIdentityPolicy.cs` (§15) initially named the internal orchestrator class literally, which is scanned by `DynamicCoreCalendarMaterializationOrchestratorTests.LiveReachability_OnlyCatalogPreviewGeneratorMayCallTheOrchestrator` — a real dark-reachability fitness test asserting the orchestrator has exactly one production call site (`CatalogPreviewGenerator.cs`). The literal class name in a doc comment counted as a second "hit." Fixed by rewording the comment to describe the finding without repeating the exact class/method names (cross-referencing this phase's own report instead), re-verified green. This is disclosed as a self-inflicted, self-caught, and self-fixed issue — not a new blocker, not a pre-existing regression, and not evidence against the mapping fix's correctness.

---

## 31. Parent FREQ.6D.4D state

**Not closed.** Public activation is still blocked — this phase closed one real, disclosed blocker (`V1CatalogPublicWorkoutTypeMappingPolicy`'s `AEROBIC_STRENGTH_CONTROLLED_INTRO` gap) and found a new, fourth, independent one (`CompressedCore`/`ExtendedCore` execution-index threading gap). Per the phase's own §39: "If another independent blocker appears: FREQ.6D.4D stays open. Do NOT close parent merely because mapping closure is complete."

---

## 32. Next roadmap capability

Thread `IPublishedTemplateBundleLoader`/`ExecutionPrescriptionIndex` into `CatalogPreviewGenerator`'s `CompressedCore`/`ExtendedCore` dynamic-orchestration branch, mirroring exactly how the exact-12-week "preferred" pipeline already does it — a real, narrow, additive engineering fix, not a product/evidence decision (unlike the workout-mapping gap, this one has no ambiguity about the correct fix; it is a straightforward parity gap between two pipelines that should behave identically with respect to ProfileBacked execution resolution). Once fixed and regression-proven across 8/10/12/14 weeks, retry public activation a fifth time.

---

## 33-34. Implementation SHA(s) / governance SHA

Recorded in `PHASE_LEDGER.md` row for this phase — implementation commit (mapping fix + test + routing-policy comment update), documentation commit (this report + ledger + roadmap), and backfill commit (this documentation commit's own SHA written into the ledger row).

---

## Final classification

**`FREQ6D4D5F_MAPPING_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`**

Rationale: the approved `FREQ.6D.4D.5E` mapping decision was correctly implemented (§5), proven key-only (§6), proven non-regressive for unknown workouts and every existing mapping (§7-8), and backed by a genuine, catalog-file-driven exhaustive completeness gate (§9-13) — all of which are retained and green regardless of activation status. Public activation was retried a fourth time and found a new, real, independent, fully-diagnosed blocker unrelated to the mapping work (§15), correctly triggering this phase's own STOP condition rather than being patched inside this phase's scope. `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic; `FREQ.6D.4D` remains open.
