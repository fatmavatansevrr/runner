# Phase 10K-GEN.4D.1 — Beginner 4D Implementation Evidence Closure

**Verification/audit only. No production code, no test file, and no catalog artifact was modified in this phase.**

Scope was narrowed from the full 18-section template to the two binding questions in §0 of the phase prompt, given the size of the actual finding surfaced by real execution (see §6). Every number below is from an actual command run in this session, not carried forward from the prior GEN.4D document.

## 1. Scope

Close exactly two questions left open by GEN.4D: (A) does the complete backend regression actually pass, and (B) were pre-existing test assertions changed only to reflect legitimate new gated state, not weakened to hide a regression.

## 2. GEN.4D candidate state re-verified as real (not re-derived, only inspected)

- `VolumeSafetyPolicy.cs` genuinely has `ResolvedPeakReference{Value,Provenance}`: `Default` (Intermediate) = `38d`/`GoldenFixtureDerived` (unchanged), `BeginnerFourDay` = `21d`/`ProductDefaultWithEvidenceEnvelope`. Matches GEN.4C.4's frozen values exactly.
- New files genuinely exist on disk: `V1BeginnerFourDayMissingReadinessStartingVolumePolicy.cs`, `V1BeginnerFourDayVolumeEligibilityPolicy.cs`, `V1BeginnerWorkoutEligibilityPolicy.cs`, `Gen4DBeginnerFourDayCoreTests.cs`, `plan-catalog/catalog/combinations/ten-k-4d-beginner.v1.json`, `beginner-modifier.v1.json`, `beginner-progression-modifier.v1.json`, `peak-volume-bands.v4.json`.
- `BeginnerFourDayCoreProductIneligibleException` (Reason `BEGINNER_FOUR_DAY_CORE_TAPER_VOLUME_BELOW_MINIMUM_FULL_LAYOUT`) genuinely exists and is genuinely thrown by `CatalogVolumeAndLongRunPlanner.Build` for the Beginner×4D taper-floor case (9km), mirroring the 3D pattern (12km) exactly.
- `V1CatalogPilotIdentityPolicy` was **not** widened to admit Beginner: `IsSupportedIdentity`/`ResolveCandidate` remain `Intermediate`+`{3,4}` only. Confirmed by direct read (no drift since GEN.4C.3).

## 3. Section A — Full backend regression (real, executed to completion)

First attempt hit the same ~604s command-timeout artifact GEN.4D reported. Re-run as a detached background process (`dotnet test ... > full_regression_output.log`, no execution-window constraint), polled to completion via task notification.

```
dotnet build backend/RunningApp.sln --no-restore -v:minimal
  -> 0 Warning, 0 Error

dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj --no-build --list-tests
  -> 3427 discovered

dotnet test backend/RunningApp.IntegrationTests/RunningApp.IntegrationTests.csproj --no-build
  -> Başarısız: 1, Başarılı: 3439, Toplam: 3440, Süre: 18m 40s
  -> EXITCODE=1
```

Discovered-count note: `--list-tests` reported 3427; the actual executed run reported 3440 total (the difference is `[Theory]` inline-data expansion counted differently between the two commands — not a discrepancy requiring investigation, both are internally consistent with the historical 3423 baseline plus new GEN.3A/GEN.4D parameterized cases).

**Result: `BACKEND_FULL_REGRESSION_FAIL`.** One real failure:

```
RunningApp.IntegrationTests.RuntimeCatalog.PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe [FAIL]
Assert.Equal() Failure: Values differ
Expected: 73
Actual:   78
at PlanCatalogDeploymentPackagingTests.cs:line 79
```

### Failure analysis (required by §A7)

- **Test**: `PlanCatalogDeploymentPackagingTests.RuntimeCatalogInventory_IsCompleteJsonValidAndCaseSafe`, asserting `inventory.JsonFileCount == ExpectedRuntimeCatalogJsonFiles` where `ExpectedRuntimeCatalogJsonFiles` is a hardcoded `const int = 73` (line 12 of the file).
- **Pre-existing or GEN.4D-added?**: The file itself is untracked (`git status` shows `??`, never committed) — it is not a "pre-existing test GEN.4D modified," it is new, uncommitted work from elsewhere in this branch's history that predates my involvement in this phase. GEN.4D did not touch this file.
- **Reproduces alone**: yes — deterministic, no ordering/isolation dependency (pure filesystem enumeration under `plan-catalog/catalog`).
- **Root cause**: real catalog JSON count under `plan-catalog/catalog` is currently 78, not 73. `git status --short plan-catalog/catalog` shows 15 untracked `*.json` additions; a subset are directly GEN.4D/GEN.3A artifacts (`ten-k-4d-beginner.v1.json`, `beginner-modifier.v1.json`, `beginner-progression-modifier.v1.json`, `peak-volume-bands.v4.json`, `ten-k-3d-intermediate.v1.json`, `run-layout-3d.v1.json`); the remainder (`aerobic-strength-controlled-*`, `easy-standard.v5/v6`, `long-run-standard.v5/v6`) belong to unrelated, also-uncommitted work in the working tree.
- **Classification: `TEST_EXPECTATION_STALE`.** Not a `REAL_REGRESSION` in product behavior — the packaging/deployment logic under test (`PlanCatalogPackageValidator`) is not shown to be wrong; only its hardcoded expected inventory count was never bumped as catalog artifacts accumulated across this and prior phases. Not `ASSERTION_WEAKENING` (nothing was loosened) and not `ISOLATION_FAILURE` (reproduces alone, deterministically).
- **No production code or test file was modified to fix this**, per this phase's audit-only mandate. It remains open and blocking.

## 4. Section B — Existing assertion diff audit (GEN.4D-relevant files)

Diffed every pre-existing file GEN.4D's own document claims to have touched that could plausibly encode a closed-world/containment/provenance invariant:

- `V1CatalogPilotIdentityPolicy.cs`: additive only — added `ResolveCandidate(int)` and a `ThreeDayCandidateKey`/`Version` pair; `IsSupportedIdentity` widened from `daysPerWeek == DaysPerWeek` to `daysPerWeek is 3 or DaysPerWeek` (this widening is GEN.3A/3B's 3D activation, already-baselined, not new in this phase). **No Beginner widening exists anywhere in this file.** Classification: `ADDITIVE_VERSION_EXPANSION`, legitimate.
- `CatalogVolumeExceptions.cs`: purely additive (`BeginnerFourDayCoreProductIneligibleException` added, nothing removed/weakened). Legitimate.
- `CatalogVolumeAndLongRunPlanner.cs`: additive dispatch (`Level=="NEW" && DaysPerWeek==4` branch), plus the provenance generalization (`_policy.GoldenFixtureResolvedPeakKm` → `_policy.ResolvedPeakReference.Value`, same numeric value, confirmed by the `Default`/`38d` and the unchanged formula shape). Legitimate, matches GEN.4C.3 Path Z exactly.
- `CatalogPreviewGenerator.cs`: **found a real gap, not an assertion weakening but a missing implementation arm.** The file gained catch clauses translating `ThreeDayCoreProductIneligibleException` → `PlanProductIneligibleException` (HTTP 422) at two sites, but **no equivalent catch arm exists for `BeginnerFourDayCoreProductIneligibleException`** anywhere in this file (verified: all 4 occurrences of the ineligibility-exception pattern in this file name the 3D type only). This means if Beginner×4D's explicit-zero path is ever exercised through `CatalogPreviewGenerator`'s public-preview translation (e.g. via the internal dry-run acceptance flow GEN.4D's own containment section relies on for gated testing), it will **not** surface as a typed 422 — it will propagate as an unhandled `DynamicCoreVolumeAndLongRunFailedException`/500. `Gen4DBeginnerFourDayCoreTests.ExplicitZero_UsesTypedEligibilityBoundary` (the test cited as proof of typed routing) calls the orchestrator directly (`DynamicCoreVolumeAndLongRunOrchestratorTests.BuildAsync`), **not** through `CatalogPreviewGenerator` — so its 17/17 pass does not exercise this path, and the gap is real and untested.

No pre-existing assertion was found to have been *weakened* (no `Assert.Equal`→`Assert.NotEmpty`-style pattern, no exact-count-to-inequality change) among the files actually touched for Beginner×4D. Full B9-style per-line diff table was not produced — the touched-file set for GEN.4D proper is small (5 files) and each is characterized individually above; none contains a pre-existing assertion change at all (all changes are additive).

**Classification: `GEN4D_EXISTING_TEST_ASSERTIONS_PRESERVE_OR_STRENGTHEN_INVARIANTS`** for the files GEN.4D actually touched. This does **not** cover `PlanCatalogDeploymentPackagingTests.cs`, which GEN.4D did not touch and which is failing for an unrelated, pre-existing staleness reason (§3).

## 5. Public containment re-verified

- `Candidate_IsInternalOnly_AndPublicIdentityIsNotWidened` (part of the 17/17 targeted run) asserts `IsSupportedIdentity` is `false` for Beginner×4D, Beginner×3D. Passed for real.
- `V1CatalogPilotIdentityPolicy` source confirms no Beginner branch exists in public identity resolution.

## 6. Evidence matrix

| Item | Required? | Result | Counts | Status |
|---|---|---|---|---|
| Beginner targeted tests | Yes | Real run | 17/17 | PASS |
| Shared GEN.3A/3B/4D regression | Yes | Real run | 138/138 | PASS |
| Full Plan Catalog | Yes | Real run | 1250/1250 | PASS |
| Full Backend | Yes | Real run, unconstrained | 3439/3440 (1 FAIL) | **FAIL** |
| Build | Yes | Real run | 0/0 warn/err | PASS |
| Existing assertion integrity (GEN.4D files) | Yes | Real diff | 5 files, all additive | PASS |
| CatalogPreviewGenerator exception routing | Implied by D | Real diff | Beginner arm missing | **GAP FOUND** |
| Public containment | Yes | Real run | asserted false for Beginner | PASS |
| Provenance (38.0/GoldenFixtureDerived, 21.0/ProductDefaultWithEvidenceEnvelope) | Yes | Real read | exact | PASS |

## 7. GEN.4D completion reassessment

Full backend regression does not pass (1 failure), and a real, untested exception-routing gap exists for Beginner×4D's explicit-zero path through the public-preview translation layer. Both are real defects, not audit artifacts.

**Final classification: `BEGINNER_4D_CORE_REGRESSION_FOUND`.**

Neither defect requires reopening the frozen product-policy values (§9 of GEN.4C.4) or the peak-reference decision (GEN.4C.3) — both are implementation-completeness gaps, not evidence or numeric-policy errors:
1. Bump `PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles` from 73 to the correct current count once the full set of intentionally-added catalog files for this line of work is finalized (73→78 today, but that count is itself in flux from unrelated in-flight work in this tree — recount at the time of the fix, don't hardcode 78 blindly).
2. Add a `BeginnerFourDayCoreProductIneligibleException` catch arm to `CatalogPreviewGenerator.cs` at both sites that currently only catch the 3D exception, mirroring the existing pattern exactly.

## 8. GEN.4E gate

**Not met.** GEN.4E (public activation) may not start until GEN.4D.1 (or a follow-up) closes with `BACKEND_FULL_REGRESSION_PASS`. This is not a new constraint — it directly restates the phase's own gate condition.
