# PHASE 10K-GEN.4D — Beginner 4D Core Implementation

Final classification: **BEGINNER_4D_CORE_IMPLEMENTED_AND_GATED**

## 1. Files inspected

- `VolumeSafetyPolicy.cs`, `CatalogVolumeAndLongRunPlanner.cs`, starting-volume policies and typed volume exceptions.
- GEN.3A artifacts: `run-layout-3d.v1.json`, `ten-k-3d-intermediate.v1.json`, and the Intermediate level/progression modifiers.
- `V1CatalogPilotIdentityPolicy.cs`, `CatalogCandidateEligibilityGate.cs`, `DynamicCoreVolumeAndLongRunOrchestrator.cs`.
- `BoundCatalogPlanValidator.cs`, `CatalogWorkoutBinder.cs`, progression loader/allocator, 4D layout and catalog graph validators.

Audit conclusion: the former peak authority was a scalar `GoldenFixtureResolvedPeakKm`; provenance tagging was genuinely new. `BoundCatalogPlanValidator` already derives cardinality from the resolved layout and requires no Beginner-specific branch. GEN.3A's composition manifest/internal-gate pattern was reused.

## 2. Provenance-tagging implementation

Added typed `ResolvedPeakReference { Value, Provenance }` and `ResolvedPeakReferenceProvenance` with `GoldenFixtureDerived` and `ProductDefaultWithEvidenceEnvelope`. Existing Intermediate 4D remains 38 km and is tagged `GoldenFixtureDerived`. Beginner 4D is 21 km and tagged `ProductDefaultWithEvidenceEnvelope`. The planner consumes `.Value` only and never branches on provenance.

## 3. Catalog artifacts created

- `TEN_K__4D__BEGINNER` v1, composing `TEN_K_MASTER` v6 + reused `RUN_LAYOUT_4D` v1 + `BEGINNER_MODIFIER` v1 + `APPSEL_RACE_PLAN_V1` v5.
- `BEGINNER_MODIFIER` v1 and `BEGINNER_PROGRESSION_MODIFIER_V1` v1 (one hard/KEY session; no second hard stimulus).
- `PEAK_VOLUME_BANDS_V1` v4 with Beginner 4D 18–24 km; a new rule-pack version prevents mutation of previously published v3.
- Runtime policies for 12.0 missing, 9.5 explicit-zero, 21.0 resolved reference, 9.0 full-layout floor, 17.0 break-even and 0.53 taper.

Progression ratios, allocation shares, long-run shares, master template and 4D layout are reused. No duplicated 12-week plan was introduced.

## 4. Eligibility routing implementation

Added `BeginnerFourDayCoreProductIneligibleException` with stable reason `BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`. Explicit-zero horizons 8–12 wrap this typed product ineligibility through the existing dynamic orchestrator pattern; 13–14 pass. Missing-readiness 8–14 passes.

The shared progression still references deferred FARTLEK/THRESHOLD artifacts for graph closure. `V1BeginnerWorkoutEligibilityPolicy` prevents their selection only for `TEN_K__4D__BEGINNER`, falling back its single KEY slot to `EASY_STANDARD`. Definitions and interval/repetition structures were not changed.

## 5. Containment confirmation

The new candidate is loadable only through the internal dry-run gate. `V1CatalogPilotIdentityPolicy` was not widened: Beginner 4D, Beginner 3D and Advanced 4D are false at public identity routing. No nearest-match fallback exists. Intermediate 4D and Intermediate 3D public identities remain unchanged.

This phase is internal/gated only; there is no public rollout.

## 6. Tests added

`Gen4DBeginnerFourDayCoreTests` covers exact policy/provenance values, missing-readiness 8–14, explicit-zero 8–14 eligibility boundary, exact pre-taper/taper projections, 1 KEY + 2 EASY_SUPPORT + 1 LONG cardinality, deferred workout exclusion, and public containment. Existing catalog assertions were updated only where prior closed-world assumptions were intentionally expanded by the new gated level.

## 7. Exact commands

```text
dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~Gen4DBeginnerFourDayCoreTests" -v:minimal
dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Gen3A|FullyQualifiedName~Gen3B|FullyQualifiedName~DynamicCoreVolumeAndLongRunOrchestratorTests|FullyQualifiedName~Gen4DBeginnerFourDayCoreTests" -v:minimal
dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj --no-restore -v:minimal
dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj --no-build -v:minimal
dotnet build backend/RunningApp.sln --no-restore -v:minimal
git diff --check
```

## 8. Exact results

- Beginner targeted: **17 passed, 0 failed, 0 skipped**.
- Shared GEN.3A/GEN.3B/3D/4D regression: **138 passed, 0 failed, 0 skipped**.
- Full catalog suite: **1250 passed, 0 failed, 0 skipped**.
- Final build: **succeeded, 0 warnings, 0 errors** (5.40 s).
- `git diff --check`: **exit 0**; only pre-existing working-tree LF→CRLF notices were emitted, with no whitespace errors.
- Full backend suite: **ENVIRONMENT_BLOCKED** — command produced no result and was terminated by the 604-second execution timeout; no green result is claimed.

## 9. Full regression status versus GEN.3B baseline

The requested historical baseline is 3423/3423. The current full command could not finish within the environment limit, so exact parity cannot be asserted. Targeted shared regressions are green (138/138), but the full status remains `ENVIRONMENT_BLOCKED`, not fabricated success.

## 10. Sourced/unsourced value confirmation

All frozen GEN.4C.4 values were transcribed without re-derivation: 12.0, 9.5, 9.0, 0.53, 17.0, 21.0, 18–24, and 0.07/0.08/2.5. The 21.0 authority is explicitly product-default/evidence-envelope, not golden-fixture-derived. Existing 38.0 is unchanged and tagged golden-fixture-derived.

`TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` remains unresolved and non-blocking; it was not silently fixed.
