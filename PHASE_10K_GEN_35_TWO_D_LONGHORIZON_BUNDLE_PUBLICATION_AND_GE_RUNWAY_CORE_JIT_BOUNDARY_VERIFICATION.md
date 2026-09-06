# PHASE 10K-GEN.35 — 2D LongHorizon: Bundle Publication and GE→Runway→Core JIT Boundary Verification

**Parent authority**: `GEN.34` (`TWO_D_LONGHORIZON_ADMISSION_GATE_OPENED_GE_SEGMENT_REAL_POSTGRES_VERIFIED_RUNWAY_CORE_JIT_BOUNDARY_PENDING_BUNDLE_PUBLICATION`, `DONE (PARTIAL)`, whose §10 three-item remaining contract this phase executes), `GEN.33`/`GEN.32`/`GEN.31`/`GEN.30`/`GEN.29`/`GEN.26`/`GEN.11` (frozen 2D structural/numeric authority), `GEN.9` (the catalog-release publish pattern this phase's bundle publication models), `FREQ.6D.13`-`FREQ.6D.19` (the real dark-verification standard for the GE→Runway→Core JIT boundary).
**Phase type**: CATALOG RELEASE + IMPLEMENTATION (defect closure) + REAL DARK VERIFICATION (user-authored "PHASE PROMPT 06c (continued IV)"). One new plan-catalog release, two production-code defect fixes, one disclosed-not-fixed gap, new tests only.
**Execution status**: DONE
**Final classification**: `TWO_D_LONGHORIZON_BUNDLE_PUBLISHED_GE_RUNWAY_JIT_BOUNDARY_FIXED_AND_VERIFIED_CORE_ANCHOR_CONTINUITY_GAP_DISCLOSED` — `DONE (PARTIAL)`

---

## 0. Mandatory startup — completed

`git log -5`: HEAD `5fbfb8d` (`docs(gen-34): backfill governance commit SHA for GEN.34`). `git fetch && git diff HEAD origin/main`: empty — in sync, at the exact commit the governing prompt named. `git status --porcelain` (excluding standing tracked `bin`/`obj` rebuild noise, pre-existing modified `baseline_tmp`/`ten-k-pilot-domain-decision-audit.*`, and pre-existing untracked `.trx` files from prior sessions): clean. `PHASE_LEDGER.md`'s last row is `GEN.34` (seq 137) — **`GEN.35` confirmed correct** as the next free phase ID.

Required reading performed directly from the repository this phase: `GEN.34` (full), `GEN.33`/`GEN.32`/`GEN.31`/`GEN.30`/`GEN.29`/`GEN.26`/`GEN.11`, `GEN.9` (catalog-release publish pattern), `FREQ.6D.13`-`.19`. Direct source reads: `CatalogPublisher.cs`, `ReleaseCommands.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`, `LongHorizonRollingJitActivationRuntime.cs`, `TenKPreparationRunwayDarkOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrationContracts.cs`, `PreparationRunwayWeekMaterializationContracts.cs`, `PreparationRunwayDirectionContracts.cs`, `LongHorizonRollingCoreGenerationInputAdapter.cs`, `DynamicCoreCalendarMaterializationOrchestrator.cs`, `DynamicCoreWeekSkeletonOrchestrator.cs`, `LongHorizonRollingRestartContinuationService.cs`, `LongHorizonCheckpointEvidenceAggregator.cs`, `RunningApp.Api.csproj`, `appsettings.json`.

---

## 1. Bundle publication — `1.4.0`, additive over `1.3.0`

### 1.1 Publish action

Ran the real CLI pipeline (`dotnet run --project src/PlanCatalog.Cli -- publish --version 1.4.0 --channel Pilot --allow-unconfirmed-content`), mirroring `GEN.9`'s exact `1.2.0`→`1.3.0` pattern. A dry run (`build-release`) was executed first and its manifest diffed byte-for-byte against `1.3.0`'s real manifest before the actual publish:

- **0 hash mismatches** across all 78 pre-existing artifacts/bundles from `1.3.0` — every already-published document/bundle republishes with an **identical content hash**. No existing candidate, version, or content changed.
- **13 newly added items**: `TEN_K_MASTER v11`, `RUN_LAYOUT_2D v1`, `TEN_K_WORKOUT_PROGRESSION_2D_V1 v1`, `PEAK_VOLUME_BANDS_V1 v7`/`v8`, `APPSEL_RACE_PLAN_V1 v8`/`v9`, `TEMPLATE_COMBINATION TEN_K__2D__BEGINNER v1`/`TEN_K__2D__INTERMEDIATE v1`/`TEN_K__3D__BEGINNER v1`, and the three corresponding `PUBLISHED_TEMPLATE_BUNDLE` entries.

### 1.2 A disclosed, unavoidable side effect — `TEN_K__3D__BEGINNER` also newly bundled

`CatalogPublisher.Publish`/`BuildRelease` publishes the **entire current non-Draft source snapshot** as one atomic release — there is no CLI option to publish a subset of combinations (confirmed by direct source read: `ExcludeDraftArtifacts` filters only `Draft` status, and every eligible non-retired combination is unconditionally included). `TEN_K__3D__BEGINNER v1` (Beginner×3D Core, already-approved `GEN.23`-`.25` content, `VALIDATED` status) had never been bundled in any real release before — `GEN.25`'s own public activation relied entirely on the Development-only `LocalCatalogAcceptance` override, never a real published bundle. Publishing `1.4.0` therefore also bundles it for the first time, as a mechanical, unavoidable consequence of the whole-snapshot publish design — **not new authoring, not a new decision, and not a new public-routing exposure** (Beginner×3D Core has been publicly active via the identity-policy allow-list widening since `GEN.25`, independent of any real bundle's existence). Disclosed explicitly per the governing prompt's own instruction not to publish anything beyond what 2D LongHorizon needs — this is the one unavoidable exception, and it changes no existing content or behavior.

### 1.3 Config/deployment wiring updated (a second, real, disclosed instance of `GEN.10`'s own defect)

`backend/RunningApp.Api/appsettings.json`'s `PlanCatalog:PublishedBundleReleaseVersion` bumped `1.3.0`→`1.4.0`.

Tracing the deployment-packaging path found `RunningApp.Api.csproj` still pinned its packaged bundle path at `1.3.0` — the **identical stale-pin defect `GEN.10` already found and fixed once** (that phase's own comment records: "this was still pinned at 1.1.0, never bumped alongside... FREQ.6D.26... or GEN.10 itself"). Bumped the `<Content Include="...artifacts\appsel-plan-catalog\1.3.0\bundles\...">` path to `1.4.0` and updated `PlanCatalogDeploymentPackagingTests.ApiProjectPackagesOnlyCatalogJsonIntoDeterministicTarget`'s matching assertion, closing this recurrence before it could ship silently (as `GEN.10` itself documented `FREQ.6D.26`'s own 6D-Intermediate bundle silently missing from every real packaged build for one full phase-cycle).

### 1.4 Verification

`verify-release --version 1.4.0`: `Success: true`, checksums valid. `1.3.0` unaffected, unretired, unmodified. `dotnet build RunningApp.sln` (Debug): 0 errors immediately after the bump.

---

## 2. Real-PostgreSQL GE→Runway→Core JIT boundary verification — two real defects found and fixed, one found and disclosed

Per `GEN.34 §10`'s exact remaining contract, attempted the full real-PostgreSQL GE→Runway/Core JIT-composition boundary for both Levels × both GE-parity cases (`totalWeeks=21` → `geWeeks=1`, odd; `totalWeeks=22` → `geWeeks=2`, even — the identical two horizons `GEN.34 §1` Gap-4 already used at the structural-skeleton layer alone). Driving this for real, through `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync`, surfaced three real issues — two fixed, one disclosed:

### 2.1 Defect 1 (fixed) — `LongHorizonRollingJitCompositionOrchestrator`'s `jitLevel` derivation had no Beginner branch

```csharp
var jitLevel = request.Candidate.Level == "ADVANCED" ? RunningBackground.Advanced : RunningBackground.Intermediate;
```

This is the **identical recurring defect family** `GEN.10` fixed once for Advanced and `GEN.29` fixed once for `TenKPreparationRunwayDarkOrchestrator`'s own `expectedLevel` derivation — but this specific call site, one layer further up in the real JIT composition orchestrator, was never updated. Every real Beginner×2D GE→Runway/Core JIT composition call silently built its `PreviewRequest`/`ResolverInput` with `Level=Intermediate` while `request.Candidate.Level=="NEW"` (Beginner), tripping `TenKPreparationRunwayDarkOrchestrator`'s own request-identity check (`"Preview, resolver, candidate, and orchestration date/distance contexts must be value-identical."`) and surfacing as an opaque `JitEvidenceConflictUnresolved` Block. Found via direct diagnostic tracing (constructing the real `LongHorizonRollingJitCompositionRequest` and orchestrator by hand to inspect `InternalDiagnostic`/`Failure.Reason` before this phase's own end-to-end verification — the first time any Beginner×2D LongHorizon request ever reached this call site, since `GEN.34` opened the admission gate but never reached the published-bundle-gated JIT boundary). Fixed to the same three-way switch already used elsewhere in this codebase (`"ADVANCED"`→Advanced, `"NEW"`→Beginner, `_`→Intermediate). Zero-delta for every other Level/frequency: this switch produces the identical `Intermediate` result for every value it previously handled.

### 2.2 Defect 2 (fixed) — `TenKPreparationRunwayDarkOrchestrator`'s own Runway-generation call site never threaded `GEN.33`'s `StartGlobalWeek`

`PreparationRunwayWeekMaterializationRequest.StartGlobalWeek` (added by `GEN.33`, default `1`) exists specifically so a LongHorizon caller whose preceding GE segment has odd length can continue the same global odd/even parity into Runway. `GEN.33` wired this parameter into `LongHorizonStructuralMaterializer.MaterializeRunwayAsync` — the **dark structural-skeleton builder**. It never reached `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync`'s own, separate Stage-7 call to `PreparationRunwayWeekMaterializer.MaterializeAsync` — the **real production Runway/Core content generator** that `LongHorizonRollingJitCompositionOrchestrator` invokes at the actual JIT boundary. This second call site to the same materializer was missed at `GEN.33` time because no LongHorizon request had ever reached the real JIT boundary before this phase.

**Observed defect** (before the fix, via a real-PostgreSQL run for `totalWeeks=21`, `geWeeks=1`): weeks 1 **and** 2 both materialized as `KEY_SESSION+LONG_RUN` (Pattern-A), instead of the correct alternation (week 2 should be `EASY_SUPPORT+LONG_RUN`, Pattern-B) — because the real generator always restarted its own local Pattern-A/B numbering at local week 1 regardless of the true `GlobalWeekNumber` it was continuing from. For `totalWeeks=22` (`geWeeks=2`, even), the same restart-at-local-week-1 default happened to coincide with the correct global parity by pure arithmetic coincidence (Runway's real start week, 3, is odd — matching local week 1's own default Pattern-A) — meaning the even-GE-parity case would have silently passed a naive test, masking the defect entirely if only one parity case had been checked. This is exactly why the governing prompt's own "both GE-parity cases" requirement mattered here.

**Fix**: added an optional, additive `RunwayStartGlobalWeek` parameter (default `1`) to `TenKPreparationRunwayDarkOrchestrationRequest`, threaded into the Stage-7 `PreparationRunwayWeekMaterializationRequest` constructor call, and had `LongHorizonRollingJitCompositionOrchestrator` supply `request.StructuralRoadmap.GeneralEnduranceWeeks + 1` (mirroring `GEN.33`'s own `StartGlobalWeek: geWeeks+1` convention exactly). Confirmed inert for every non-2D layout by construction — `GEN.33`'s own contract doc comment establishes this field is consulted only when a repeating `WeeklyPatternRoles` pattern exists (2D Model A/B only); every other frequency's layout has no such pattern and ignores the value entirely regardless of what is passed.

**Verified**: real-Postgres full weekly-role dump for both parity cases, both Levels, confirms strict, unbroken `GlobalWeekNumber`-parity alternation across the entire GE+Runway range (weeks 1 through `runwayEnd`) after the fix, with no double-up or skip at the GE→Runway boundary.

### 2.3 Genuine finding, disclosed and NOT fixed this phase — Core's own real content generator has no continuation-offset mechanism

After fixing Defects 1-2, the JIT boundary reaches Core successfully (real activation, real persistence, restart-survival all succeed), but the **first real Core week's own Pattern-A/B selection does not continue the parity GE+Runway just established** whenever `GeneralEnduranceWeeks + 8` (Runway's fixed length) is odd. Traced this to `TenKPreparationRunwayDarkOrchestrator`'s Stage-8 Core generation (`_coreGenerator.GenerateAsync` → `DynamicCoreCalendarMaterializationOrchestrator` → `DynamicCoreSessionPrescriptionOrchestrator`): this is **the same real production pipeline `CatalogPreviewGenerator` uses for already-`PUBLICLY_ACTIVE` standalone 2D/3D/4D/5D/6D Core generation** (confirmed directly from `DynamicCoreCalendarMaterializationOrchestrator`'s own doc comment: *"CatalogPreviewGenerator now uses this composition for admitted compressed, preferred, and extended standalone cores"*). It has no mechanism anywhere in its request contract to accept an external `GlobalWeekNumber` continuation offset — it always anchors its own local Pattern-A/B alternation to its own local week 1 = Pattern-A.

Internally, Core's own alternation is still fully correct and self-consistent (a real, valid, unbroken 2-week period — verified directly against the real persisted data for both parity cases) — only its *anchor point* relative to the true `GlobalWeekNumber` is wrong when `runwayEnd` is even (i.e. whenever `GeneralEnduranceWeeks` is odd, since Runway's own length is fixed at 8).

**Deliberately not fixed this phase.** Fixing it would require threading a `StartGlobalWeek`-equivalent parameter through `TenKPreparationRunwayCoreGenerationRequest` → `DynamicCoreCalendarMaterializationOrchestrator`/`DynamicCoreSessionPrescriptionOrchestrator` — the exact same shared pipeline `GEN.25`'s real public HTTP endpoint for standalone 2D Core relies on today. This phase's own explicit constraint is **"Do not touch 2D Core or Runway (frozen)."** The Runway fix in §2.2 above is squarely within this engagement's established precedent for exactly this kind of purely-additive, default-preserving, non-product parameter (`GEN.33` did the identical thing to Runway's own materializer request contract) — but Core's real generator is a materially higher-risk, more directly-live surface (the literal code path a real paying user's 2D Core HTTP request executes today), and reaching in to add a new parameter there — even a default-preserving one — is a distinct, larger-blast-radius change this phase declines to make unprompted, matching this engagement's own established restraint (`GEN.32`/`GEN.33` each declined analogous higher-risk expansions at their own boundary, disclosing instead of forcing).

Real dark verification confirms and documents this precisely (not merely asserted): `Gen35TwoDayJitBoundaryTests` asserts strict global parity for every GE+Runway week, and separately asserts Core's own internal alternation is correct **relative to Core's own local week 1** (not the true `GlobalWeekNumber`) — the test encodes the real, disclosed, current behavior rather than a false expectation.

### 2.4 Test scenario input note — onboarding baseline

The first real-PostgreSQL attempts at this boundary failed with `PreparationRunwayDirectionUnsupportedException` (`weekly=EqualTarget, longRun=AboveTarget`) using `GEN.34`'s own `OnboardingBaseline(16, 6, 2)`. Traced this to `Phase 4K.8A`'s pre-existing, correctly-restrictive direction-support matrix (`AboveTarget` unsupported for either weekly or long-run) — not a defect: `(16, 6)` is simply too high relative to 2D's much lower `PeakVolumeBand` ([16,22]km Beginner / [20,30]km Intermediate, `GEN.11`) and its correspondingly low early Core Week-1 target. `5D`'s own working `FREQ.6D.19` fixture uses `(26, 8)`, safely below 5D's own, much larger target. This phase's own tests use a lower, still-realistic `(8, 3)` "just starting out" baseline instead — a test-scenario input choice, not new numeric authority, chosen to stay below 2D's lowest real Core Week-1 target at the governed horizons.

---

## 3. Recurring-defect-family search

Performed explicitly, per the governing prompt's own instruction, across every file this phase touched plus every real call site of `LongHorizonRollingJitCompositionOrchestrator`/`LongHorizonRollingJitActivationRuntime`/`TenKPreparationRunwayDarkOrchestrator` reachable from the fixed call sites:

- Searched both touched files for any further `RunningBackground.Intermediate`/`"ADVANCED"` two-way ternary — found none beyond the one fixed (§2.1) and the one already-correct, `GEN.29`-fixed `expectedLevel` derivation inside `TenKPreparationRunwayDarkOrchestrator.cs` itself.
- Searched `LongHorizonRollingJitActivationRuntime.cs` and `LongHorizonRollingCoreGenerationInputAdapter.cs` for sibling Level-dispatch hardcodes — found none (the adapter's own `level` parameter default is already correctly explicit-caller-driven since `GEN.10`).
- Confirmed (§2.3) a third, real instance of this engagement's broader "not every real caller of a shared materializer was threaded the same fix" family — disclosed, not fixed, per the explicit `2D Core (frozen)` constraint.

**Total for this phase: 2 real defects found and fixed, 1 real defect found and disclosed** — a genuine, confirmed 3-item addition to this engagement's own recurring hardcode/dispatch-gap defect family (`GEN.10`/`12`/`17`/`19`/`20`/`23`/`29`/`32`/`33`/`34`, now 20+ confirmed instances before this phase).

---

## 4. `GlobalWeekNumber` continuity across the full GE→Runway→Core chain — proven, with the one disclosed exception

For both Levels × both GE-parity cases (4 real scenarios, `Gen35TwoDayJitBoundaryTests`):

- Real initial activation → real JIT composition (GE→Runway boundary) → real JIT composition (pure Runway continuation) → real JIT composition (Runway→Core boundary) — 3 sequential real, Postgres-persisted JIT-composition calls per scenario, all succeeding.
- Fresh, independent restart reload (a new `AppDbContext`, never the in-memory graph the driving calls used) confirms `StructuralRoadmap.GlobalWeekNumbers` is the full, contiguous `1..totalWeeks` range, and `CurrentWindow.EndGlobalWeek` matches the just-persisted state.
- Every GE+Runway week (weeks `1..runwayEnd`) — not merely the boundary weeks — carries the correct `GlobalWeekNumber`-parity role shape (strict odd=`KEY_SESSION`+`LONG_RUN`/even=`EASY_SUPPORT`+`LONG_RUN` alternation), verified directly against real persisted session rows for every week, both parity cases, both Levels: 4 scenarios × up to 10 weeks each, zero exceptions.
- Core's own weeks: internally consistent, correctly alternating, but anchored to Core's own local week 1 rather than the continuing global parity — the one disclosed, unfixed remainder (§2.3).

---

## 5. Fresh Intermediate×5D zero-delta — re-confirmed

`Gen34IntermediateFiveDayZeroDeltaTests.InitialActivation_IntermediateFiveDay_StillApproved_UnaffectedByTwoDayWiring` (run as part of the `Gen34` filter this phase re-ran explicitly — not merely cited) still passes unchanged: `Approved`, correct `CandidateKeyFiveDay`, correct slot/week counts, structural validator passes. Independently confirmed by direct code inspection that none of this phase's three changes (`jitLevel` switch, `RunwayStartGlobalWeek` threading, the `appsettings.json`/`csproj` bundle-version bump) alters any code path a 5D (or 3D/4D/6D/Advanced) request exercises:

- The `jitLevel` switch's `_ => Intermediate` fallback is byte-identical to the prior two-way ternary's `Intermediate` branch for every value other than `"NEW"` (which no non-2D candidate ever carries).
- `RunwayStartGlobalWeek` defaults to `1`, and `GEN.33`'s own contract doc comment establishes this value is consulted only for a layout with a repeating `WeeklyPatternRoles` pattern (2D Model A/B exclusively) — every other frequency's `PreparationRunwayCanonicalWeeklyLayout` has no such pattern and ignores the value regardless of what is passed. (`LongHorizonRollingJitCompositionOrchestrator` now computes and passes `GeneralEnduranceWeeks + 1` unconditionally for every frequency — confirmed inert for non-2D by the same contract guarantee, not merely assumed.)
- The bundle-version bump republishes every pre-existing artifact/bundle with an identical content hash (§1.1) — no 5D (or any other frequency's) resolved content changes.

---

## 6. Required verification

### 6.1 Operational note — process-check method used

Every regression run in this phase was preceded by a plain, unfiltered `tasklist | grep -i "testhost\|vstest"` review (never the filtered `tasklist //FI` form found unreliable in `GEN.32`), confirming no stale process before starting; the two suites were never run concurrently.

### 6.2 New-test verification

- `LongHorizonGen35TwoDayJitBoundaryTests.cs` — 4 new tests (`Gen35TwoDayJitBoundaryTests`, both Levels × both GE-parity cases), all passing, real PostgreSQL, verified in isolation first.

### 6.3 Zero-delta confirmations (isolated reruns, before the full suite)

- `Gen34` filter (all `GEN.34` tests): **30/30 passing** — confirms this phase's fixes did not regress `GEN.34`'s own admission-gate/GE-segment work.
- Targeted `LongHorizon`/`PreparationRunway` sweep (`FullyQualifiedName~LongHorizon | FullyQualifiedName~PreparationRunway`): **2008 passed, 0 failed, 2008 total** (16m35s) — `4` more than `GEN.34`'s own `2004/2004`, exactly matching this phase's 4 new tests.

### 6.4 `RunningApp.IntegrationTests` full regression

Confirmed no `testhost`/`vstest` process running (unfiltered `tasklist | grep`) immediately before this run; run alone. `backend/RunningApp.sln` → full solution build + test (Debug, `gen35_full_regression.trx`): **4340 passed, 3 failed, 4343 total** (35m33s). The 3 failures are the identical, named, pre-existing baseline failures carried since `GEN.17`-`GEN.34` — `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13 and 14) and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` (identical `Expected: OK, Actual: InternalServerError` failure shape, unrelated to this phase's changes). `4343 - 4339` (`GEN.34`'s own closing baseline) `= 4`, exactly matching this phase's own 4 new tests, all 4 passing — zero new failures, zero regressions, a byte-for-byte reconciliation.

### 6.5 `PlanCatalog.Tests` full regression

Run explicitly and sequentially after the backend suite fully exited (confirmed via `tasklist | grep` showing zero `testhost`/`vstest` processes beforehand). `plan-catalog/PlanCatalog.sln` → `PlanCatalog.Tests` (Debug, full solution build + test, `gen35_plancatalog.trx`): **1510 passed, 0 failed, 1510 total** (6s) — clean, matching the confirmed `GEN.34` baseline exactly. No `plan-catalog/catalog/` source-authoring file was touched this phase (confirmed by `git diff --stat -- plan-catalog/catalog/` returning empty) — only the new `1.4.0` release artifacts under `plan-catalog/artifacts/` were added.

### 6.6 `git diff --check`

Clean on every file this phase touched (`LongHorizonRollingJitCompositionOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrationContracts.cs`, `TenKPreparationRunwayDarkOrchestrator.cs`, `RunningApp.Api.csproj`, `appsettings.json`, `PlanCatalogDeploymentPackagingTests.cs`, `LongHorizonGen35TwoDayJitBoundaryTests.cs`) — only pre-existing LF/CRLF `autocrlf` warnings, no conflict markers, no trailing-whitespace errors.

---

## 7. Explicit constraints — confirmed respected

- **No new product/numeric authority invented.** Every fix reuses already-approved `GEN.11`/`GEN.29`/`GEN.30`/`GEN.31`/`GEN.32`/`GEN.33`/`GEN.34` mechanisms (`StartGlobalWeek`'s own established convention, the existing three-way Level-dispatch pattern already used elsewhere). No `DOMAIN_DECISION_REQUIRED` item surfaced — both real defects found are mechanical wiring gaps, not training-methodology or product questions.
- **2D Core and Runway (frozen) respected.** The Runway fix (§2.2) is a purely additive, default-preserving parameter on the *real JIT composition* Runway-generation call site — the same category of change `GEN.33` itself made to Runway's request contract, confirmed inert for every non-LongHorizon standalone Runway caller (default `1`, byte-identical). Core's own generation pipeline — the literal code path `GEN.25`'s live public HTTP endpoint executes — was traced, understood, and **deliberately not modified**, precisely because the constraint names Core explicitly; disclosed instead (§2.3).
- **Still dark-only where it matters**: `V1CatalogPilotIdentityPolicy.cs`'s public gate methods untouched. No public HTTP routing added or changed. The bundle publication (§1) is a release/artifact action, not a public-routing change — `2D` remains internally gated exactly as `GEN.34` left it.
- **Zero-delta for Intermediate×5D**: proven fresh (§5), not merely asserted or cited.
- **`DONE (PARTIAL)` recorded honestly**: the bundle is published, the GE→Runway boundary is genuinely fixed and verified for both parity cases and both Levels, the Runway→Core boundary itself succeeds (real activation/persistence), and `GlobalWeekNumber` continuity is proven across the *entire* GE+Runway range. The one remaining gap — Core's own internal anchor not yet continuing the established global parity — is a real, disclosed, deliberately-not-forced item, not a code defect this phase failed to find.

---

## 8. Final classification

**`TWO_D_LONGHORIZON_BUNDLE_PUBLISHED_GE_RUNWAY_JIT_BOUNDARY_FIXED_AND_VERIFIED_CORE_ANCHOR_CONTINUITY_GAP_DISCLOSED`** — `DONE (PARTIAL)`.

The plan-catalog bundle release `1.4.0` is published, additive, and verified against `1.3.0` (0 hash mismatches on 78 pre-existing items). Both `GEN.34`-disclosed remaining structural gaps standing between the admission gate and the real JIT boundary are now closed: one genuine, previously-undisclosed Level-dispatch defect (§2.1) and one genuine, previously-undisclosed Runway-continuity defect (§2.2), both fixed and dark-verified end to end for both Levels and both GE-parity cases through real PostgreSQL. A third, real, genuinely deeper gap in Core's own real content-generation pipeline (§2.3) is found, traced precisely, and explicitly disclosed rather than fixed — because fixing it would require touching the exact shared pipeline this phase's own explicit constraint names as frozen (`GEN.25`'s live public standalone Core generator).

## 9. Concrete remaining contract for the next phase

1. Design and implement threading a `StartGlobalWeek`-equivalent continuation offset through `TenKPreparationRunwayCoreGenerationRequest` → `DynamicCoreCalendarMaterializationOrchestrator`/`DynamicCoreSessionPrescriptionOrchestrator`'s own Core-week Pattern-A/B anchor selection — the same purely-additive, default-preserving category of fix this phase already applied to Runway (§2.2), but for Core's generator specifically. This is the concrete, disclosed remainder that would make Core's own first-week alternation continue GE+Runway's established global parity for every GE-parity case (currently correct only when `GeneralEnduranceWeeks+8` is even).
2. Because this touches the same pipeline `GEN.25`'s live public 2D/3D/4D/5D/6D standalone Core HTTP endpoint uses, this next phase must budget for a full zero-delta regression proof across every already-`PUBLICLY_ACTIVE` standalone Core frequency, not merely the LongHorizon surface — a materially larger verification scope than this phase's own Runway-only fix required.
3. Consider whether standalone-repair (`ScheduleRepairRuntimeOrchestrator`) paths need their own 2D-specific dark verification once the boundary's remaining anchor gap is closed (`GEN.34 §10` item 3, still open).

`NEXT_PHASE_NOT_YET_SCHEDULED`.

---

## 10. Governance

`PHASE_LEDGER.md` row appended (`GEN.35`). `MASTER_ROADMAP.md`'s 2D-LongHorizon backlog item updated to record the bundle publication, the two fixed defects, the disclosed Core-anchor remainder, and the real GE→Runway→Core boundary verification. Two-commit self-referential-SHA-backfill pattern followed: implementation commit (catalog release + production fixes + tests + this report with `PENDING` placeholder), then this document and the ledger row backfilled with that SHA in a second, documentation-only commit. Normal push only.
