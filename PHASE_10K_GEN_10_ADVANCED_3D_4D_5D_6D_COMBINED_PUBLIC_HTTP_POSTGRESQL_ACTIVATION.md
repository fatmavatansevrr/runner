# PHASE 10K-GEN.10 — Advanced 3D/4D/5D/6D Combined Public HTTP/PostgreSQL Verification & Activation

**Parent phase**: `GEN.9`
**Phase type**: IMPLEMENTATION (activation) + DEFECT DISCOVERY/FIX + REAL HTTP/POSTGRESQL VERIFICATION
**Execution status**: DONE
**Final classification**: `ADVANCED_3D_4D_5D_6D_PUBLICLY_ACTIVE` / `ADVANCED_TEN_K_FREQUENCY_AXIS_COMPLETE`

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `git log -5`, `git fetch && diff HEAD origin/main` (in sync), `git status` clean except pre-existing unrelated local modifications (`baseline_tmp`, `plan-catalog/artifacts/audits/*`) predating this session. `GEN.9` row 111 confirmed present with real SHA `d50da27`/`2aa72d4`; roadmap confirmed reflects `GEN.9` as the latest Advanced-axis phase. This phase runs as Phase B of a two-phase prompt whose Phase A (`FREQ.6D.28`, ledger row 112) was already completed, ledgered, and pushed before this phase began, per the prompt's own explicit sequencing gate.

Scope: widen the real public routing gate for Advanced 3D/4D/5D/6D across Core/Runway/LongHorizon, implementing only already-approved `GEN.7`/`GEN.8`/`GEN.9` authority — no new product/numeric/schema decision.

## 1. Public gate widening

`V1CatalogPilotIdentityPolicy`:
- `IsSupportedLevelFrequency`: added `(Advanced, 3/4/5/6)`.
- `ResolveCandidate`: added the four `ADVANCED_*` candidate key/version resolutions.
- `IsSupportedPreparationRunwayLevelFrequency`: added all four Advanced frequencies, **including 3D** (unlike Intermediate, whose Runway/LongHorizon deliberately excludes 3D — Advanced×3D Runway/LongHorizon was separately approved, `GEN.7` §17).

`LongHorizonPublicPlanService.ValidatePilot`: rewritten from a single Intermediate-only check to `(Intermediate && 4/5/6) || (Advanced && 3/4/5/6)`.

Advanced×7D (`PRODUCT_NON_SUPPORT`, `GEN.7`) and Advanced×2D (`OUT_OF_V1`, never designed) were deliberately excluded from every widened allow-list and confirmed, by real HTTP request (not code inspection), to remain unreachable — see §4.

The existing public workout-type mapping (`V1CatalogPublicWorkoutTypeMappingPolicy.Map`, keyed by `(WorkoutDefinitionKey, StructuralRole, ProgressionStageKey)`) was verified by direct code reading to be Level-agnostic and require zero change for Advanced's reused workout definitions — confirmed, not assumed, and re-confirmed empirically in §5's dual-KEY lifecycle test (both lanes resolve to real `ADVANCED_*` profile keys).

## 2. Real defects found and fixed

None of the eight defects below were anticipated by the governing prompt. All were found by building the new public activation test suite against the real production chain and diagnosing every non-obvious failure to a root cause (never worked around, never silently retried with different test inputs without first confirming the failure was a test-input problem and not a production one — five of the eight *were* genuine production defects; the other three test failures encountered during this phase were confirmed to be test-authoring mistakes, documented in §6).

1. **`LongHorizonPublicPlanService` preview generation — hardcoded `Level = RunningBackground.Intermediate`.** Would have silently misclassified every Advanced LongHorizon request internally, specifically rejecting Advanced×3D outright. **Fix**: `Level = command.Level`.

2. **`LongHorizonPublicPlanService.BuildTrainingPlan` — hardcoded `Level = RunningBackground.Intermediate`.** The method that constructs the actually-persisted `TrainingPlan` row at confirmation — every confirmed Advanced LongHorizon plan would have persisted to PostgreSQL with the wrong `Level`, a real data-integrity bug affecting downstream Home/Calendar/TrainingDay reads. **Fix**: `Level = snapshot.Command.Level`.

3. **Stale `PublishedBundleReleaseVersion` configuration pin.** `appsettings.json` still read `"1.2.0"` though `GEN.9` had already published release `1.3.0` (the release carrying the four new `ADVANCED_*` execution-profile bundles). `PublishedTemplateBundleLoader.TryLoadAsync` resolves `null` for a combination whose exact release has no bundle file, and a ProfileBacked session never falls back to Legacy — every Advanced Core/Runway/LongHorizon request whose Week-1 KEY_SESSION is ProfileBacked (i.e. every Advanced×5D/6D request, and any 3D/4D request reaching a ProfileBacked week) failed with `DynamicCoreSessionPrescriptionFailedException`/500. Confirmed via diagnostic rethrow that `1.3.0` is a byte-identical superset of `1.2.0` for every pre-existing document (empty `comm -23` diff) before bumping. **Fix**: `appsettings.json` `PublishedBundleReleaseVersion` → `"1.3.0"`.

4. **`TenKPreparationRunwayDarkOrchestrator.ValidateRequest` — hardcoded `Level == RunningBackground.Intermediate` request-identity check.** Line 342 of the same method already correctly admits `Level is "INTERMEDIATE" or "ADVANCED"` on the candidate, but a separate, redundant check two lines later still compared `PreviewRequest.Level`/`ResolverInput.Level` against a hardcoded `RunningBackground.Intermediate` constant — rejecting every real Advanced Runway composition as `InvalidOrchestrationRequest`, surfacing publicly as `PREPARATION_RUNWAY_PREVIEW_GENERATION_FAILED` for every Advanced request at weeks 15-20, and — more seriously — as an opaque `JitEvidenceConflictUnresolved` Block on every Advanced LongHorizon GE→Runway boundary handoff (see defect 7). **Fix**: compare against the candidate's own resolved `Level` (`Advanced` when `request.Candidate.Level == "ADVANCED"`, else `Intermediate`) instead of a hardcoded constant.

5. **`LongHorizonRollingInitialActivationRuntime.BuildInitialActivationAsync` — missing `Level` thread-through to `LongHorizonStructuralMaterializer.MaterializeAsync`.** The call supplied `daysPerWeek` but never `level`, which defaults to `Intermediate`. For Advanced×3D (no Intermediate×3D equivalent exists) this produced a hard structural-validation failure: `ResolveCandidateKey(3, Intermediate)` falls through to the 4D-Intermediate default identity, whose expected GE slot count is 4, against a real 3-slot GE week — `LongHorizonStructuralMaterializer produced an invalid skeleton: ... has 3 slots, expected exactly 4`. For Advanced×4D/5D/6D the same gap was silent: the skeleton's own `CandidateKey` was written as the Intermediate identity even though the request was genuinely Advanced (invisible because those day counts share the same GE slot cardinality with their Intermediate counterparts). **Fix**: thread `request.Level` through.

6. **`LongHorizonRollingStateReconstructionService.ReconstructAsync` — same missing-`Level` gap, at reload.** Every reload of a persisted Advanced LongHorizon plan (confirm+fresh-reload, checkpoint continuation, restart) silently rebuilt an Intermediate-shaped structural skeleton identity regardless of the plan's real, durably-stored `Level`. **Fix**: read the real persisted `plan.Level` (already durably stored — see defect 7) and thread it through, mirroring the exact `FREQ.6D.18` precedent this same method already documents for the analogous `daysPerWeek` gap.

7. **`LongHorizonRollingStateRepository.InitializeStructuralStateAsync` — hardcoded `Level = "Intermediate"` at initial `LongHorizonRollingPlanState` persistence.** Every confirmed Advanced LongHorizon plan's `Level` database column would have been written as `"Intermediate"` regardless of the real request — the root cause defect 6 was reading around, and a real data-integrity bug in its own right (affecting every future checkpoint continuation, which reads `aggregate.Level` to dispatch GE numeric policy and structural materialization). **Fix**: added a `Level` field to `LongHorizonRollingInitializationRequest` (defaulting to `Intermediate`, byte-identical for every pre-existing caller — the same established pattern as `FREQ.6D.18`'s `DaysPerWeek` field), threaded from `LongHorizonPublicPlanService.ConfirmAsync`'s already-known `snapshot.Command.Level`.

8. **`LongHorizonRollingCoreGenerationInputAdapter.Build` — hardcoded `Level = RunningBackground.Intermediate` in both the `GeneratePreviewRequest` and `ResolverInputSnapshot` it constructs for the real JIT Runway/Core composition at a GE→Runway boundary handoff.** This fed directly into defect 4's `ValidateRequest` check, so every real Advanced GE→Runway boundary handoff (the point at which a confirmed Advanced LongHorizon plan's first `activate-next-window` call must compose real Runway/Core content) failed with `InvalidOrchestrationRequest`, reclassified by the JIT composition orchestrator's default fallback into the opaque `LONG_HORIZON_CONTINUATION_BLOCKED` / `JitEvidenceConflictUnresolved` — a genuinely confusing failure mode with no direct textual link back to "Level mismatch." Root-caused via a temporary diagnostic rethrow surfacing `TenKPreparationRunwayDarkOrchestrationResult.Failure.Reason`, which read verbatim "Preview, resolver, candidate, and orchestration date/distance contexts must be value-identical" — the exact message from defect 4's check. **Fix**: added a `level` parameter (defaulting to `Intermediate`, byte-identical for every pre-existing caller) and threaded `request.Candidate.Level` through from `LongHorizonRollingJitCompositionOrchestrator`.

9. **Deployment-packaging gap (pre-existing, discovered not caused by this phase): `RunningApp.Api.csproj`'s packaged published-bundle release was pinned at `"1.1.0"`, never bumped.** `PackagedPlanCatalogRealHttpSmokeTests`/`PlanCatalogDeploymentPackagingTests` only assert the csproj packages *some* exact-pinned release and never exercise a ProfileBacked candidate through the packaged output, so this was never caught. Real impact: release `1.1.0`'s bundle folder never carried `TEN_K__6D__INTERMEDIATE` (added at `1.2.0`, `FREQ.6D.26`) — every real packaged/published build has been silently missing 6D Intermediate's execution-profile bundle since `FREQ.6D.26` — and would have shipped with Advanced entirely non-functional (all four `ADVANCED_*` bundles, added at `1.3.0`, absent). Confirmed via `comm -23` that `1.3.0`'s bundle set is a strict superset of `1.1.0`'s. **Fix**: bumped the csproj's `Content Include` path (and its own governing test assertion) from `1.1.0` to `1.3.0`; verified via a clean `bin/Release` rebuild that the packaged output now contains all nine bundle files including all four Advanced candidates.

## 3. Not new authority

Every fix above corrects an existing wiring/threading/configuration gap against `Level`/release-version values that were already, elsewhere, correctly resolved (`request.Candidate.Level`, `command.Level`, `snapshot.Command.Level`, the `PublishedBundleReleaseVersion` `GEN.9` already published to) — none invents a new numeric constant, product rule, or eligibility decision. No `DOMAIN_DECISION_REQUIRED` STOP condition was reached.

## 4. Real verification (`Gen10AdvancedCombinedPublicActivationTests`, 51 tests, all real HTTP + real PostgreSQL)

- **Core/Runway** (`CoreOrRunwayHorizons_PublicPreviewSucceeds_ForEveryAdvancedFrequency`, 16 cases): real `GeneratePreview` success for all four frequencies across representative Core (8/14wk) and Runway (17/20wk) horizons.
- **LongHorizon** (`LongHorizonHorizons_PublicPreviewSucceeds_ForEveryAdvancedFrequency`, 12 cases): real preview success across 21/32/52wk for all four frequencies.
- **Full 8-52 matrix** for Advanced×5D specifically (45/45 cells, mirroring `GEN.9`'s own disclosed-coverage choice of proving the full matrix once at the highest-value dual-KEY point rather than repeating it four times).
- **Confirm + fresh-PostgreSQL-reload**, both Core and LongHorizon: asserts the reloaded persisted `TrainingPlan.Level == RunningBackground.Advanced` and correct `DaysPerWeek` — this is the real regression test for defects 2, 6, and 7.
- **Missing-readiness** returns the typed `ADVANCED_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE` product-ineligible rejection (`GEN.8` authority), not an unsupported/routing error — confirms the eligibility gate and the public gate are correctly layered.
- **`PublicFullLifecycle_FiveDay_ReachesOrganicCoreWithAdvancedDualKeyProfiles_ThroughRealPostgres`**: a real GE→Runway→Core dual-KEY lifecycle for Advanced×5D through three real `activate-next-window` calls (the exact real code path defects 4, 5, 7, and 8 all sit on), real Home/Calendar reads confirming `rolling_long_horizon` surfaces correctly, direct database verification that both Week-10 KEY_SESSION lanes resolve to real `ADVANCED_*` profile keys (not silently falling back to Intermediate's), and a real repair regression through `ScheduleRepairRuntimeOrchestrator` (secondary-KEY `NotToday` → real replacement session, primary lane untouched) — proving the same real-interruption/repair rigor already established for Intermediate×5D.
- **Unsupported-neighbor closure**, both Core/Runway and LongHorizon endpoints, real HTTP requests (not code inspection): Beginner×3D/5D/6D, Intermediate×7D, Experienced×4D all remain closed; **Advanced×7D and Advanced×2D explicitly asserted unreachable** through the widened gate, per the governing prompt's own non-negotiable requirement — confirmed with a real request, and confirmed no candidate-key fallback leakage (`DoesNotContain` every `TEN_K__{N}D__ADVANCED` key in every negative response body).
- **`IntermediateAndBeginner_RemainPubliclyActive_ZeroDelta`**: real HTTP previews for Intermediate 4D/5D/6D and Beginner 4D all still succeed unchanged.

51/51 pass.

## 5. Regression

Full `RunningApp.IntegrationTests` regression (post-fix, post-obsolete-assertion-correction): **4034 total, 4032 passed, exactly the 2 durable pre-existing baseline failures** (`Sw09ExplicitZeroReadinessEndToEndTests`, `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks:13)` — exact name/exception/message match to prior documentation), **zero new regressions** — verified via TRX (`gen10_full_v2.trx`), not console summary. `PlanCatalog.Tests`: 1510/1510. Debug and Release builds both clean (0 errors). `git diff --check` clean on every source file changed (`*.cs`/`*.csproj`/`*.json`, excluding build-output trees).

## 6. Obsolete pre-activation assertions corrected

Opening Advanced 3D/4D/5D/6D as real public identities made 12 pre-existing assertions across 9 test files factually obsolete — each of the form "Advanced (or 6D Beginner/Advanced) remains unrecognized/closed," now legitimately false. Corrected by advancing/removing only the specific obsolete row per file, matching this engagement's established pattern (`FREQ.6D.27` did the same for Intermediate×6D):

- `Gen9AdvancedCombinedDarkVerificationTests.cs` (`Gen9AdvancedIsolationTests`): split the blanket "Advanced unsupported at every frequency" theory into a positive `PublicIdentityPolicy_RecognizesAdvancedActivatedFrequencies` (3/4/5/6, asserting `True` + correct resolved candidate key/version) and a negative `PublicIdentityPolicy_DoesNotRecognizeAdvancedOutOfScopeFrequencies` (2/7, unchanged `False`).
- `PreparationRunwayFiveDayPublicActivationEndToEndTests.cs`: removed `("ten_k","advanced",5)` from the unsupported-neighbors theory.
- `Phase4F8_2LivePilotRoutingTests.cs`: the `Level` mutation case switched from `RunningBackground.Advanced` (now a real pilot identity) to `RunningBackground.Experienced` (still genuinely unwidened at every frequency).
- `Freq6D26IntermediateSixDayDarkVerificationTests.cs` (`Freq6D26IsolationTests`): removed the Advanced×6D clause, renamed to `PublicIdentityPolicy_DoesNotRecognizeBeginnerSixDay` (Beginner×6D alone remains correctly closed).
- `RunningBackgroundV2Tests.cs`: removed `RunningBackground.Advanced` from the "unwidened non-Intermediate levels" theory (Experienced alone remains correct).
- `Gen4DBeginnerFourDayCoreTests.cs`: the exact-allow-list check's `(Advanced,4) → False` assertion flipped to `True`; `Experienced,4 → False` added to keep a negative check in the same test.
- `PublishedCatalogNonDevelopmentEndToEndTests.cs`: the "unsupported Level" sub-case switched from `"advanced"` to `"experienced"`.
- `Gen5DIntermediatePublicActivationTests.cs`: removed `("advanced", 5)` from the unsupported-neighbors theory.
- `Freq6D27IntermediateSixDayPublicActivationTests.cs`: removed `("advanced", 6)` from the unsupported-neighbors theory.

Each correction was independently classified `OBSOLETE_PRE_ACTIVATION_ASSERTION` (never `POSSIBLE_REAL_REGRESSION`) by reading the failing assertion, its surrounding comments (several already documented the identical pattern from prior widenings), and confirming the newly-`True`/removed cell is exactly the cell this phase's own gate widening (§1) made legitimately reachable — not a broader, unexplained loosening.

Separately, one test failure (`PlanCatalogDeploymentPackagingTests.ApiProjectPackagesOnlyCatalogJsonIntoDeterministicTarget`) in an interim regression run was identified as a build/edit-ordering race (the compiled test binary's `"1.1.0"` assertion ran against the live-on-disk csproj already showing `"1.3.0"`, since the csproj fix — defect 9 — was applied while a background regression run was already executing against an older build) rather than a genuine regression; resolved naturally once both files were rebuilt together and re-verified in the final clean run (§5).

## 7. Governance

`PHASE_LEDGER.md` row 113 appended. `MASTER_ROADMAP.md` §2 updated: the 10K support matrix's Advanced row now reads `PUBLICLY_ACTIVE` for 3D/4D/5D/6D (was `IMPLEMENTED_AND_DARK_VERIFIED`); a new "Advanced axis public activation status (as of GEN.10)" paragraph added documenting the gate widening, all nine defects, and verification evidence, alongside the unchanged `GEN.9` implementation-status paragraph. No other roadmap section required correction.

**This closes `ADVANCED_TEN_K_FREQUENCY_AXIS_COMPLETE`**: every Advanced 10K frequency cell (3D/4D/5D/6D/7D) now carries a final, evidenced, publicly-verified classification — 4 `PUBLICLY_ACTIVE`, 1 `PRODUCT_NON_SUPPORT` — matching Intermediate's own completion shape. Advanced×2D remains out of V1 scope, confirmed still unreachable through the widened gate by real HTTP request. Beginner and Experienced levels are unaffected by this phase. Next: not selected by this phase — per the governing prompt, the "2D" prompt in the agreed sequence is explicitly deferred until both Phase A and Phase B of this prompt are fully ledgered, pushed, and reflected as the two most recent completed phases in `MASTER_ROADMAP.md`.
