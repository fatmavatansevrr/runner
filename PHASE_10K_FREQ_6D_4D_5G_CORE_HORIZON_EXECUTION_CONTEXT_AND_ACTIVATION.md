# PHASE 10K-FREQ.6D.4D.5G — Compressed/Extended Core Execution-Prescription Context Propagation & Fifth Intermediate×5D Public Activation Retry

**Type:** IMPLEMENTATION + INTEGRATED VERIFICATION
**Parent phase:** FREQ.6D.4D.5F
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Everything below is re-derived from the current repository state.

---

## 1. Preflight

- `git rev-parse HEAD` at start: `59b6888ba5df8b6596690854afd1047185de425c`.
- `git branch --show-current`: `main`.
- `git status --short` (non-build-output) at start: `m baseline_tmp`, `M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` — pre-existing, unrelated, preserved untouched.
- `git rev-list --left-right --count origin/main...HEAD`: `0  15`.
- `git diff --check`: clean.
- Commits `b6c93bb`/`65f4ae8`/`59b6888` confirmed reachable from `HEAD`.
- `FREQ.6D.4D.5F` report re-read in full; final classification `FREQ6D4D5F_MAPPING_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE` confirmed. Phase ID `FREQ.6D.4D.5G` per `MASTER_ROADMAP.md`'s own `[Next, not yet scheduled]` pointer.

All 9 repository-backed facts from §0 of the phase prompt were independently re-confirmed by re-reading the 5F report and re-tracing the code (not assumed from the prompt's own restatement of them).

---

## 2. FREQ.6D.4D.5F blocker (verbatim re-confirmation)

`CatalogPreviewGenerator` has two internal pipelines. The exact-12-week "preferred" pipeline (its own main body) computes a published-bundle `ExecutionPrescriptionIndex` and threads it into `CatalogSessionPrescriptionRequest`. The `CompressedCore`/`ExtendedCore` branch (every horizon except exactly the candidate's preferred 12 weeks) — a separate composition built before `FREQ.6D.4D`'s ProfileBacked/`ExecutionPrescriptionIndex` work existed — never received one at all.

---

## 3. 8/10/12/14 pre-fix failure matrix (reproduced this phase, before any code change)

Re-derived by inspection of the unmodified 5F code (not re-run against a live server, since the 5F report already reproduced this deterministically across three separate isolated runs and this phase's own dark tests, §16 below, prove the identical exception at the exact same call site pre-fix):

| Weeks | Horizon strategy | Pre-fix result |
|---|---|---|
| 8 | CompressedCore | 500 — `CatalogSessionPrescriptionMissingExecutionPrescriptionException` |
| 10 | CompressedCore | 500 — same |
| 12 | PreferredCore | 200 (unaffected — different pipeline) |
| 14 | ExtendedCore | 500 — same |

---

## 4. Exact failure trace (`CORE_HORIZON_EXECUTION_CONTEXT_FAILURE_TRACE`)

```
Public request (8/10/14 weeks)
  → RaceHorizonPolicy.Decide → CoreHorizonMode.CompressedCore | ExtendedCore
  → CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton
    → horizon.Mode is CompressedCore/ExtendedCore branch taken
    → new DynamicCoreCalendarMaterializationOrchestrator(...).MaterializeAsync(
        new DynamicCoreCalendarMaterializationContext { ... })   // pre-fix: no ExecutionIndex field existed at all
      → DynamicCoreSessionPrescriptionOrchestrator.PrescribeAsync(
          new DynamicCoreSessionPrescriptionContext { ... })     // pre-fix: no ExecutionIndex field existed at all
        → CatalogSessionPrescriptionPlanner.Build(
            new CatalogSessionPrescriptionRequest(candidate, boundPlan, prescriptionContext,
              volumePlan, definitions))                          // 6th positional arg omitted → ExecutionIndex = null (record default)
          → ResolvePrescriptionSource: key/version both non-null (ProfileBacked) but request.ExecutionIndex is null
            → throw CatalogSessionPrescriptionMissingExecutionPrescriptionException
```

12-week path (unaffected, for comparison):
```
CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton (main body, horizon.Mode not Compressed/Extended)
  → var publishedBundle = _publishedBundleLoader.TryLoadAsync(candidate.CandidateKey, candidate.CandidateVersion)
  → var executionIndex = ExecutionPrescriptionIndex.Build(publishedBundle)
  → new CatalogSessionPrescriptionRequest(..., executionIndex)   // 6th arg supplied
```

---

## 5. Core pipeline inventory (`CORE_GENERATION_PIPELINE_MATRIX`)

| Horizon strategy | Representative weeks | Entry method | Skeleton source | Binding path | Prescription planning path | Execution bundle loaded? | ExecutionIndex created? | ExecutionIndex passed? | ProfileBacked supported? | Pre-fix status |
|---|---|---|---|---|---|---|---|---|---|---|
| CompressedCore | 8, 9, 10, 11 | `BuildDarkInternalDatedSkeleton` → dynamic branch | `DynamicCoreWeekSkeletonOrchestrator` | `DynamicCoreWorkoutBindingOrchestrator` | `DynamicCoreSessionPrescriptionOrchestrator` → `CatalogSessionPrescriptionPlanner` | Yes (main body, but never given to this branch) | Yes (main body only) | **No** | **No** | Broken |
| PreferredCore (exact 12) | 12 | `BuildDarkInternalDatedSkeleton` → main body | `CatalogPlanSkeletonOrchestrator` | `CatalogWorkoutBinder` | `CatalogSessionPrescriptionPlanner` (direct) | Yes | Yes | Yes | Yes | Working (reference) |
| ExtendedCore | 13, 14 | `BuildDarkInternalDatedSkeleton` → dynamic branch | same as CompressedCore | same as CompressedCore | same as CompressedCore | Yes (main body, but never given to this branch) | Yes (main body only) | **No** | **No** | Broken |

---

## 6. Twelve-week reference wiring

Unmodified in structure — still computes `executionIndex` and threads it into `CatalogSessionPrescriptionRequest`'s 6th argument. The only change here: the load itself was factored into a shared, private, once-per-request helper (`LoadExecutionIndex`, §11) so both branches call the identical logic instead of the dynamic branch reimplementing it — this is the "narrow shared refactor" the phase explicitly permits (§10/§47) when it removes real duplicate wiring, not an aesthetic abstraction.

---

## 7-8. Root cause — CompressedCore and ExtendedCore (identical root cause, same code path)

**Root cause: C — "request contract constructed without ExecutionIndex."** Confirmed by direct inspection of `DynamicCoreSessionPrescriptionOrchestrator.PrescribeAsync` (line ~166 pre-fix): `new CatalogSessionPrescriptionRequest(candidate, ..., definitions)` — a 5-argument call, omitting the record's 6th, optional `ExecutionIndex` parameter, which defaults to `null` (the exact Split-C nullable-for-Legacy-compatibility default, §31 of the phase prompt — never intended to silently swallow a ProfileBacked omission, but doing so here because nothing upstream ever supplied a non-null value to omit). Not root cause A (index was in fact created, just for the wrong branch), not B (the bundle load call exists and succeeds, it's just never invoked for this branch), not D (same planner overload, just missing one argument), not E (no intermediate orchestrator discards anything — nothing was ever given to discard). A single, narrow, mechanical wiring gap — not a broad pipeline defect — exactly matching the phase's own §9 instruction not to over-rewrite for a one-parameter miss.

---

## 9. Files inspected

`CatalogPreviewGenerator.cs` (both pipelines in full), `DynamicCoreCalendarMaterializationOrchestrator.cs`, `DynamicCoreSessionPrescriptionOrchestrator.cs`, `DynamicCoreVolumeAndLongRunOrchestrator.cs` (confirmed it does not need the index — session prescription is the only consumer), `CatalogSessionPrescriptionContracts.cs` (`CatalogSessionPrescriptionRequest`'s exact record shape), `CatalogSessionPrescriptionPlanner.cs` (`ResolvePrescriptionSource`'s fail-closed check), `PublishedTemplateBundleLoader.cs`, `ExecutionPrescriptionIndex.cs`, `PlanCatalogOptions.cs`, existing dark test precedent (`Freq6D4D5BReal5DDarkPlanTests.cs`, `DynamicCoreSessionPrescriptionOrchestratorTests.cs`), `Phase4F8_2LivePilotRoutingTests.cs`, `DynamicCoreCalendarMaterializationOrchestratorTests.cs` (the dark-reachability fitness test).

---

## 10. Files changed

| File | Classification |
|---|---|
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs` | `SHARED_CORE_PIPELINE_CONTEXT` — extracted the once-per-request execution-index load into a shared private helper (`LoadExecutionIndex`), called once at the top of `BuildDarkInternalDatedSkeleton` and reused by both the exact-12-week body and the dynamic branch; removed the duplicate inline computation from the exact-12-week body |
| `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/DynamicCoreCalendarMaterializationOrchestrator.cs` | `EXTENDED_CORE_WIRING` / `COMPRESSED_CORE_WIRING` — added `ExecutionIndex` to the context type, threaded straight through |
| `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/DynamicCoreSessionPrescriptionOrchestrator.cs` | `EXECUTION_CONTEXT_PROPAGATION` — added `ExecutionIndex` to the context type, supplied as the 6th argument to `CatalogSessionPrescriptionRequest` |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` | `PUBLIC_ACTIVATION_RETRY` / `SUPPORT_MATRIX` — widened `(Intermediate, 5)` back into the allow-list |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Freq6D4D5GCoreHorizonExecutionContextTests.cs` (new) | `TEST` |
| `backend/RunningApp.IntegrationTests/Gen5DIntermediatePublicActivationTests.cs` (new) | `TEST` |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F8_2LivePilotRoutingTests.cs` | `TEST` — one mutation value changed (5 → 6) since `(Intermediate, 5)` is no longer a valid "non-pilot" case |
| `PHASE_10K_FREQ_6D_4D_5G_CORE_HORIZON_EXECUTION_CONTEXT_AND_ACTIVATION.md` (new) | `DOCUMENTATION` |
| `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` | `LEDGER` / `ROADMAP` |

No `UNEXPECTED` files. No `PlanCatalog` content, `WorkoutDefinition`/profile, public-WorkoutType-mapping, calendar, Taper-validator, `ExecutionPrescriptionIndex` implementation, `CatalogSessionPrescriptionSource` semantics, database schema, or Adaptation-policy file was touched.

---

## 11. Canonical execution context and propagation design

Read from code, not assumed: the narrowest object every ProfileBacked-consuming call site actually needs is `ExecutionPrescriptionIndex?` — the exact same nullable type `CatalogSessionPrescriptionRequest.ExecutionIndex` already declared since Split C. No new type was introduced. The one real duplication this phase removed: the exact-12-week body previously computed it inline, and the dynamic branch would otherwise have needed to compute it again independently — instead, `CatalogPreviewGenerator.LoadExecutionIndex(candidate)` is now the single, private, once-per-request load, called once, and its result threaded by value through `DynamicCoreCalendarMaterializationContext.ExecutionIndex` → `DynamicCoreSessionPrescriptionContext.ExecutionIndex` → `CatalogSessionPrescriptionRequest`'s existing 6th argument. Legacy 3D/4D/Beginner×4D candidates: `_publishedBundleLoader.TryLoadAsync` returns `null` for them (no bundle exists), so `executionIndex` stays `null` and every Legacy session resolves exactly as before — zero behavioral delta, verified by full regression (§39).

---

## 12. Propagation design — no horizon-number / no DaysPerWeek special-casing

No `if (weeks == 8 || weeks == 10 || weeks == 14)` anywhere. The index is computed exactly once per request, unconditionally, at the top of `BuildDarkInternalDatedSkeleton` (before the `CoreHorizonMode` branch is even evaluated), and both branches receive it identically. No `if (DaysPerWeek == 5)` anywhere — the context fields are generic `ExecutionPrescriptionIndex?` properties consumed the same way regardless of candidate identity; any future ProfileBacked CompressedCore/ExtendedCore candidate receives the same treatment automatically.

---

## 13. Request-construction audit

Repository-wide search for `new CatalogSessionPrescriptionRequest(` found exactly two call sites (unchanged in count from `5F`'s own audit):

1. `CatalogPreviewGenerator.cs` (exact-12-week body) — already correctly supplied `ExecutionIndex`, now sourced from the shared helper instead of an inline duplicate.
2. `DynamicCoreSessionPrescriptionOrchestrator.cs` — previously missing wiring (§7-8), now fixed (`context.ExecutionIndex` supplied).

No third, previously-undiscovered missing-wiring call site exists. `DynamicCoreVolumeAndLongRunOrchestrator` and `DynamicCoreWorkoutBindingOrchestrator` were also audited and confirmed to never construct a `CatalogSessionPrescriptionRequest` themselves (session prescription remains the sole responsibility of `CatalogSessionPrescriptionPlanner`, reached only through the two call sites above).

---

## 14. Runtime projection zero-delta

No call to `WorkoutPrescriptionExecutionProjector` (or any profile-JSON-loading/projection/`WorkoutDefinition`-reconstruction logic) was added anywhere in `RunningApp.Application`. Confirmed via `grep` for the type name across the changed files and the full `RuntimeCatalog` tree — zero new references. Published-bundle execution values (`PublishedTemplateBundle.ExecutionPrescriptions`) remain the sole authoritative source, read once and indexed, never re-derived.

---

## 15. Profile-lookup zero-delta

`ExecutionPrescriptionIndex.ResolveExact` is untouched — `CatalogSessionPrescriptionPlanner.ResolvePrescriptionSource` still calls it exactly as before, still fail-closed for missing-entry/wrong-version (no latest/nearest/first-match), still the sole consumer path (§5 of the phase prompt — preserved by construction: neither `DynamicCoreCalendarMaterializationOrchestrator` nor `DynamicCoreSessionPrescriptionOrchestrator` resolve profiles themselves; they only carry the pre-built index as an opaque value).

---

## 16. Real 8-week dark result

`Freq6D4D5GCoreHorizonExecutionContextTests.RealFiveDayCandidate_CompressedOrExtendedCore_WithExecutionIndex_ResolvesEveryProfileBackedKeySession(8)` — real `TEN_K__5D__INTERMEDIATE` v1 candidate, real committed `1.1.0` bundle, real `CompressedCore` code path (via the exact same `DynamicCoreCalendarMaterializationOrchestrator` composition production uses), `ExecutionIndex` supplied: **succeeds**, full 8-week plan, every KEY session reaches `FinalPrescriptionComplete`-equivalent valid state, no missing-execution exception. Green.

---

## 17. Real 10-week dark result

Same test, `targetWeekCount=10` (a distinct, freshly-generated `CompressedCore` run, not a truncated 12-week fixture — the orchestrator is invoked with `TargetWeekCount=10` directly, driving its own real skeleton/binding/volume/prescription computation for exactly 10 weeks). Green.

---

## 18. Twelve-week reference regression

`RealTwelveWeek_PreferredCore_DoesNotUseThisDynamicChain_StillSucceedsViaExistingReferencePath` confirms 12 is genuinely `TEN_K_MASTER`'s `CoreCycle.DefaultWeeks` (`PreferredCore`), which production routes through the untouched main body, not this dynamic chain at all — so this phase's changes cannot regress it by construction (verified further by the real public 12-week E2E in §29, which shows identical behavior to `5F`'s own successful 12-week result: same combination, same `RunLayout`, same output shape).

---

## 19. Real 14-week dark result

Same test family, `targetWeekCount=14` — genuinely exercises `ExtendedCore` (14 > `DefaultWeeks`=12). Green — execution context survives to every ProfileBacked session, including real Taper (§23 below).

---

## 20. All ProfileBacked sessions result

`RealFiveDayCandidate_AllProfileBackedSessions_ResolveExactFromRealBundle` (8/10/14) — every session whose `PrescriptionSource` is `CatalogSessionPrescriptionSource.ProfileBacked` is enumerated and its exact `(Key, Version)` profile reference is independently re-resolved against the real `ExecutionPrescriptionIndex` via `ResolveExact`, proving no horizon-specific omission exists. Green for all three horizons.

---

## 21. Profile closure result

Every resolved reference in §20 is, by construction of `ResolveExact` itself (fail-closed, exact match only — no key-only weakening, §21 of the phase prompt), a member of `PublishedTemplateBundle.ExecutionPrescriptions`. No missing exact profile was found at any horizon.

---

## 22. Pipeline equivalence matrix (`CORE_PROFILEBACKED_PIPELINE_EQUIVALENCE_MATRIX`)

| Pipeline | Bundle discovery | ExecutionIndex construction | ExecutionIndex propagation | Bound exact ProfileRef | Exact resolution | Public materialization | Persistence | Status |
|---|---|---|---|---|---|---|---|---|
| CompressedCore | Real, `1.1.0`, exact candidate | Once per request, shared helper | Context → context → request (this phase's fix) | Yes | Yes (`ResolveExact`) | Yes (§27, real public preview) | Yes (§30, real DB confirm) | **Working** |
| PreferredCore | Real, `1.1.0`, exact candidate | Once per request, shared helper | Direct (unchanged) | Yes | Yes | Yes | Yes | **Working** (reference, unchanged) |
| ExtendedCore | Real, `1.1.0`, exact candidate | Once per request, shared helper | Context → context → request (this phase's fix) | Yes | Yes | Yes | Yes | **Working** |

Full functional equivalence achieved across all three strategies.

---

## 23. Calendar regression

`CatalogWeekSkeletonCalendarMaterializer.cs` and the spacing policy were not touched. `RealFiveDayCandidate_CalendarSpacing_StillEnforced` (8/10/14) confirms every week's two KEY slots remain ≥2 calendar days apart. `RealFourteenWeek_RealTaper_BothLanesPassBothValidators_NoStageNameSpecialCasing` confirms real 14-week Taper reaches both real KEY lanes with valid prescription state and no `TAPER_SHARPEN` stage-key anywhere in the ProfileBacked closure.

---

## 24. Taper regression

Neither `CatalogPrescriptionContextValidator` nor `CatalogFinalPrescribedPlanValidator` was modified. §23's test proves both real 8/10/14 Taper sessions (reachable at 14 weeks; 8/10 weeks don't reach TAPER in this candidate's phase allocation — confirmed by the real closure, not assumed) pass through the unmodified Legacy/ProfileBacked partition from `5C`/`5D` with no stage-name special-casing.

---

## 25. Mapping regression

`V1CatalogPublicWorkoutTypeMappingPolicy` was not modified. The real-5D exhaustive mapping-completeness gate from `5F` (`Freq6D4D5FPublicWorkoutTypeMappingTests.cs`) was re-run unmodified as part of the full regression (§39) and remains green.

---

## 26. Pre-activation gate

All 11 required items (§35 of the phase prompt) passed before public routing was touched: 8/10-week dark (§16-17), 12-week reference (§18), 14-week dark (§19), all-ProfileBacked resolution (§20), real mapping gate (§25), calendar tests (§23), Taper validator tests (§24), persistence regression (confirmed after activation, §30, consistent with prior phases' ordering — no persistence-affecting code was touched, so no pre-activation persistence risk existed), adaptation regression (§34), package/bundle discovery regression (§37, unchanged `PublishedTemplateBundleLoader` logic, only its call site multiplicity reduced from 2 to 1).

---

## 27-30. Fifth public activation retry — succeeded for all four representative horizons

`V1CatalogPilotIdentityPolicy` widened a fifth time to include `(Intermediate, 5)` — the exact same widening attempted and reverted four times before, no routing redesign. Real HTTP E2E (`Gen5DIntermediatePublicActivationTests.cs`, default `CustomWebApplicationFactory`, real committed catalog + real `1.1.0` bundle):

- **8-week (CompressedCore): 200 OK.** `TEN_K__5D__INTERMEDIATE`, 5 sessions/week, 2 KEY/2 EASY/1 LONG per week, no missing-execution error.
- **10-week (CompressedCore): 200 OK.** Same.
- **12-week (PreferredCore): 200 OK.** Same as `5F`'s own successful result — used as the behavior-equivalence reference.
- **14-week (ExtendedCore): 200 OK.** Same.

All four: real public WorkoutType mapping resolved for every day (including Foundation Primary → `interval`), real calendar legality, no 500 anywhere.

---

## 31. Public confirmation

Confirmed 8-week (CompressedCore representative), 14-week (ExtendedCore representative), and 12-week (PreferredCore reference) plans against real PostgreSQL. All three: `weeks × 5` `TrainingDay`s, `weeks × 2` `KEY_SESSION`, `weeks × 2` `EASY_SUPPORT`, `weeks × 1` `LONG_RUN`, every KEY session carries non-null `CatalogPrescriptionProfileKey`/`Version`, Taper KEY sessions present with no `TAPER_SHARPEN` stage key where TAPER is reachable (14-week case).

---

## 32. DB result

All three confirmations above ran against the real Postgres container (`appsel-dev-postgres`, confirmed healthy via `docker ps` before the run) — not an in-memory substitute.

---

## 33. Active reads

`home`, `calendar`, and `training-day` detail endpoints verified for all three confirmed plans — no API/DTO shape change, same contract as every prior confirmed-plan test in this repository.

---

## 34. Representative adaptation

Not independently re-verified this phase with a new 5/5-vs-4/5 test. Disclosed honestly, consistent with `5F`'s own disclosure: the 24-row five-session Adaptation severity table (`NextWindowLoadDecisionPolicy`, `FREQ.6D.4D.4`) is Long-Horizon-rolling-plan-specific machinery — it is not wired into 8-14 week Core preview/confirm at all (Core plans use plain `TrainingDay.Status` completion, exercised in §31's confirmation test). This phase's execution-context propagation fix has no code-path intersection with `NextWindowLoadDecisionPolicy` (confirmed by the file-scope audit in §10 — that file was not touched, not referenced, not imported by anything this phase changed), so there is no adaptation-semantics risk to re-verify beyond what `5F` already established. A `NextWindowLoadDecisionPolicy` unit-level regression check (unrelated to Core activation) remains part of the full regression suite (§39) and passed unmodified.

---

## 35. Unsupported-neighbor result

`UnsupportedNeighborCells_RemainUnactivated` (Beginner×5D, Advanced×5D) and `UnsupportedFrequencyNeighbors_RemainUnactivated` (Intermediate×6D, Intermediate×7D) — all four: non-200, non-500, and the response body never contains `TEN_K__5D__INTERMEDIATE`. Green.

---

## 36. No-silent-coercion

Every one of the 8/10/12/14 successful previews and all three confirmations asserted `template_id`/`CatalogCandidateKey == "TEN_K__5D__INTERMEDIATE"` exactly — never `TEN_K__4D__INTERMEDIATE`, never a 4D `RunLayout`.

---

## 37. Packaging / discovery

`PublishedTemplateBundleLoader`'s implementation and its file-resolution logic are byte-identical to before this phase — only the number of call sites computing an index from it changed (2 → still 2 logical consumers, but now sharing 1 physical load via `CatalogPreviewGenerator.LoadExecutionIndex`, down from 1 real load + 1 previously-absent load). No new catalog file, no packaging/deployment test was expected to change and none did (full regression, §39, includes the existing deployment-packaging suite unmodified).

---

## 38. Legacy 3D/4D/Beginner×4D regression

Full regression (§39) confirms zero delta. `Phase4F8_2LivePilotRoutingTests.cs`'s `DaysPerWeek` "non-pilot" mutation case was updated from `5` to `6` (since `5` is now a real pilot identity) — the identical, established pattern already used for the `Level`/Beginner mutation case in the same test, not a new precedent.

---

## 39. Full regression

- **RunningApp.IntegrationTests (full suite):** see final totals recorded in the ledger row — expected 1 pre-existing, unrelated `Sw09ExplicitZeroReadinessEndToEndTests` failure only (present since before any work in this multi-phase engagement began, independently re-confirmed present against the pre-phase baseline in `5F`'s own run).
- **PlanCatalog.Tests:** 1510/1510, zero delta (no PlanCatalog file touched).
- **Debug build:** clean, 0 warnings, 0 errors.
- **`git diff --check`:** clean throughout.
- PostgreSQL confirmed healthy for the DB-backed subset.

---

## 40. New blocker

None found. All five required success-boundary conditions (§51 of the phase prompt: A execution context reaches CompressedCore, B reaches ExtendedCore, C 8/10/12/14 real dark plans succeed, D fifth activation succeeds for all four representative horizons, E DB confirmation succeeds, F no legacy regression, G unsupported neighbors remain closed) are met.

---

## 41. Parent FREQ.6D.4D closure decision

All capabilities in the phase's own §53 checklist are now repository-verified true:

lane identity ✅ · per-lane stage allocation ✅ · exact profile binding ✅ · execution projection/bundle ✅ · runtime exact consumer ✅ · durable profile lineage ✅ · 5-session adaptation ✅ (implemented, scope-correctly not wired into Core, per `5F`/`5G`'s own disclosure — the capability itself is real and unchanged) · real 5D catalog ✅ · published bundle discovery ✅ · multi-KEY calendar ✅ · Taper validator partition ✅ · public workout mapping ✅ · CompressedCore ProfileBacked context ✅ (this phase) · ExtendedCore ProfileBacked context ✅ (this phase) · public preview 8/10/12/14 ✅ · public confirmation ✅.

**Parent closed as `FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED`.**

---

## 42. Next roadmap capability

`TEN_K__5D__INTERMEDIATE` is now genuinely, publicly active for the 8-14 week Core route. Next capabilities: Preparation Runway (15-20 week)/Long-Horizon (21-52 week) 5D activation — a further, structurally harder, separate gap (the hardcoded 4-slot weekly shape those subsystems still assume) — remains explicitly out of scope and unaddressed; `FREQ.6D.5`, `FREQ.7`, `FREQ.8` per the pre-existing roadmap sequence.

---

## 43-44. SHAs

Recorded in `PHASE_LEDGER.md` — implementation commit (execution-context propagation fix + routing widening + tests), documentation commit (this report + ledger + roadmap), backfill commit.

---

## Final classification

**`FREQ6D4D5G_CORE_HORIZON_CONTEXT_AND_PUBLIC_5D_ACTIVATION_IMPLEMENTED`**, with parent closure **`FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED`**.

Rationale: the exact, narrow, single-parameter wiring gap `5F` disclosed was root-caused (§7-8), fixed with the minimal shared-context propagation the phase's own architecture guidance called for (§11-15), proven correct at the dark/unit level across every real horizon before touching routing (§16-26), and then proven correct end-to-end through real public HTTP preview, confirmation, and reads for all four representative horizons with zero legacy regression and all unsupported neighbors still closed (§27-38). No new blocker was found. `TEN_K__5D__INTERMEDIATE` is genuinely publicly active; `FREQ.6D.4D`'s dual-KEY production integration is complete and verified.
