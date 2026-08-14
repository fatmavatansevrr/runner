# Phase 10K-GEN.4D.2 — Beginner 4D Implementation Completeness Closure

Fixes the two real gaps GEN.4D.1 found. Every number below is from an actual command run in this session.

## A. Gap 1 — stale inventory count

`PlanCatalogDeploymentPackagingTests.ExpectedRuntimeCatalogJsonFiles` was `73`; recounted at fix time via `find plan-catalog/catalog -name "*.json" | wc -l` → **78**. Constant updated to `78`.

A second, previously-undiscovered instance of the same class of bug surfaced during re-verification: `PackagedPlanCatalogRealHttpSmokeTests.ReleaseBuildCatalog_GeneratesRealTwentyOneWeekPreview` (same file, different test class) validates a **packaged** `backend/RunningApp.Api/bin/Release/net9.0/plan-catalog/catalog` snapshot against the same constant. That directory was a stale artifact from before this branch's catalog additions — a `dotnet build -c Release` for `RunningApp.Api.csproj` regenerated it via the existing `CopyToPublishDirectory` mechanism (no csproj change needed), bringing it to 78 files. No source or test code was needed for this half of the fix — only a rebuild.

## B. Gap 2 — missing exception routing (structural, not narrow)

**Investigation**: `ThreeDayCoreProductIneligibleException` and `BeginnerFourDayCoreProductIneligibleException` both already derived from the same abstract `CatalogVolumePlanningException`, but `CatalogPreviewGenerator.cs` caught the concrete `ThreeDayCoreProductIneligibleException` type specifically — not an oversight of "no shared type exists," but of "the wrong level of the hierarchy was caught." Catching the full `CatalogVolumePlanningException` base directly would have been wrong: that base is shared by ~9 other subtypes (`CatalogVolumeRuleInconsistentException`, `CatalogVolumeUnreachablePeakRuleException`, etc.) that are real internal errors and correctly fall through to the existing generic 500 catch-all a few lines below. Only the two *product-ineligibility* subtypes should map to HTTP 422.

**Fix (structural, per instruction §B2)**: introduced `CatalogProductIneligibleException` as a new abstract intermediate class between `CatalogVolumePlanningException` and the two concrete types:

```
CatalogVolumePlanningException (abstract)
└── CatalogProductIneligibleException (abstract, new)
    ├── ThreeDayCoreProductIneligibleException (sealed)
    └── BeginnerFourDayCoreProductIneligibleException (sealed)
```

Purely additive inheritance — `Reason`/`Code`/message construction for both existing types is byte-identical; only their base class changed. `CatalogPreviewGenerator.cs`'s two catch sites now match `is CatalogProductIneligibleException` instead of the concrete 3D type, so any future Level/Frequency cell's ineligibility exception is picked up automatically by deriving from this base — no catch-arm edit required at the next cell.

**Test added**: `Gen4DBeginnerFourDayCoreTests.IneligibilityException_IsCatchableAsSharedProductIneligibleBase_MatchingCatalogPreviewGeneratorRouting` — asserts the real `BeginnerFourDayCoreProductIneligibleException` thrown by the real orchestrator `IsAssignableFrom<CatalogProductIneligibleException>`, locking in the exact relationship the fix depends on.

**Disclosed scope limit (per instruction §B5, explicit, not silent)**: a true end-to-end test that exercises this catch arm *through* `CatalogPreviewGenerator`'s own HTTP-reachable code path (the way 3D's ineligibility is tested via a real `POST /api/v1/plans/generate-preview/race` call in `Gen3BThreeDayPublicActivationTests`) is **not possible for Beginner without further plumbing**, for two independent reasons discovered during this phase:
1. Beginner×4D has no public route — `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` returns `false` for it by design (GEN.4D containment), so no HTTP request ever reaches `CatalogPreviewGenerator`.
2. Even the internal dry-run path (`CatalogCandidateEligibilityGate.LoadForInternalDryRunAsync`) is not currently wired to `CatalogPreviewGenerator.GenerateAsync`, whose only candidate-resolution call is `V1CatalogPilotIdentityPolicy.ResolveCandidate(request.DaysPerWeek)` — a method keyed on `DaysPerWeek` alone, with **no Level parameter at all**. For `DaysPerWeek == 4` this always resolves to `TEN_K__4D__INTERMEDIATE`, regardless of Level. This is currently dead/unreachable for Beginner only because containment (#1) prevents `GenerateAsync` from ever being called with a Beginner identity — but it means `ResolveCandidate` would need a Level parameter added before GEN.4E (public activation) could work for Beginner even after identity widening. **Recorded as a real, disclosed pre-condition for GEN.4E, not fixed here** (fixing candidate-resolution routing is out of scope for a narrow exception-routing fix, and touching it now would be uncovered, unrequested production surface change).

Coverage taken instead: the orchestrator-level exception-hierarchy test above, which is real and exercises the actual production exception types and the actual production `Build()` code path that throws them — just not the HTTP layer on top, for the structural reason above.

## C. Re-verification (all real, all after both fixes)

```
dotnet build backend/RunningApp.sln --no-restore -v:minimal
  -> 0 Warning, 0 Error

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Gen3A|Gen3B|DynamicCoreVolumeAndLongRunOrchestratorTests|Gen4DBeginnerFourDayCoreTests"
  -> 139/139 (138 baseline + 1 new test), 0 failed

dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj --no-restore
  -> 1250/1250, 0 failed

dotnet build backend/RunningApp.Api/RunningApp.Api.csproj -c Release --no-restore
  -> 0 Warning, 0 Error (regenerates the packaged catalog snapshot)

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "PlanCatalogDeploymentPackagingTests|PackagedPlanCatalogRealHttpSmokeTests"
  -> 7/7, 0 failed

dotnet test .../RunningApp.IntegrationTests.csproj --no-build   (full suite, detached background process, ~18m26s)
  -> 3441/3441, 0 failed, EXITCODE=0
```

**`BACKEND_FULL_REGRESSION_PASS`** — real, complete, not fabricated, not environment-blocked.

## D. Final classification

```
BEGINNER_4D_CORE_IMPLEMENTATION_COMPLETE
```

Both GEN.4D.1 gaps are closed:
1. Inventory count corrected in both the source and packaged locations that check it (78, recounted, not hardcoded from a stale prior figure).
2. Exception routing fixed structurally via a shared `CatalogProductIneligibleException` base, closing the entire bug *class* (not just this one instance) for future Level/Frequency cells.

One disclosed, real, non-blocking follow-up recorded for GEN.4E: `V1CatalogPilotIdentityPolicy.ResolveCandidate` needs a Level parameter before Beginner×4D can be safely identity-widened — currently masked by containment, will surface the moment GEN.4E attempts public activation. Not fixed here; not silently omitted either.
