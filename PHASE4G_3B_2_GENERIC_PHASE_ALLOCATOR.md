# Phase 4G.3B.2 — Generic Target-Week-Count Phase Allocator

## 1. Scope and non-goals

**In scope**: extend `CatalogPhaseAllocationResolver` with a second, purely
mechanical method that computes a phase allocation for an arbitrary target
week count, using only catalog-authored constraint fields
(`MinimumWeeks`/`PreferredWeeks`/`MaximumWeeks`/`CompressionPriority`/
`ExtensionPriority`). Produce and record mechanical allocation results for
8–14 weeks as an audit/report exercise.

**Explicitly not in scope** (and not done):
- No new horizon is enabled publicly. 8–11 weeks → 422
  `PLAN_CORE_HORIZON_UNSUPPORTED`; 12 weeks → 200; 13–14 weeks → 422
  `PLAN_CORE_HORIZON_UNSUPPORTED`; 15+ weeks → 422
  `PLAN_HORIZON_COMPOSITION_REQUIRED` — all unchanged (§10, §12).
- `targetWeekCount` is not wired into `CatalogPreviewGenerator`'s live path.
  The new overload has zero call sites in production code (§11).
- None of the nine safety verifiers are implemented (deferred to Phase
  4G.3B.3).
- No support registry (deferred to Phase 4G.3B.4).
- No consumption of `CORE_ENTRY_READINESS_IN` or any runtime condition.
- No reads of `activation-readiness-risks.json`/`evidence-log.json`.
- No changes to `VolumeSafetyPolicy`, `CatalogVolumeAndLongRunPlanner`,
  stage scheduling, workout binding, calendar materialization, public DTOs,
  persistence, migrations, or Flutter code.

## 2. New signature and result shape (as implemented)

```csharp
public enum AllocationMode { Compression, Preferred, Extension }

public sealed record AllocatedPhase(
    string PhaseKey, int AllocatedWeeks, int MinimumWeeks, int MaximumWeeks);

public sealed record PhaseAllocationResult(
    int TargetWeeks, int PreferredWeeks, int Delta, AllocationMode Mode,
    IReadOnlyList<AllocatedPhase> Phases, bool IsMathematicallyFeasible, string ReasonCode);

internal interface ICatalogPhaseAllocationResolver
{
    CatalogPhaseAllocation Resolve(PlanCatalogCandidateSummary candidate);
    PhaseAllocationResult Resolve(PlanCatalogCandidateSummary candidate, int targetWeekCount);
}
```

Implemented exactly as suggested by the task — no additional fields were
invented (no readiness flags, no per-phase reason codes, no stage-level
detail).

## 3. Algorithm as implemented, and the deviation from the prompt's literal wording

The task's suggested outline described two different starting points for
the same 9–11 week range: bullet (b) said to start every phase at its
**MinimumWeeks** and fill upward by `ExtensionPriority`, but also said that
"true compression" (target between sum-of-minimums and sum-of-preferreds)
should walk **down from PreferredWeeks** by `CompressionPriority`. These are
not the same starting point, and hand-simulation confirms they produce
different results — e.g. for target=9, minimum-start-then-extend gives
F3/B3/RS2/T1, while preferred-start-then-compress gives F2/B3/RS3/T1.

Bullet (c) resolves the ambiguity: `Delta = targetWeekCount - PreferredWeeks`
and `Mode` are both defined relative to `PreferredWeeks`, which is only
internally consistent if the **entire** algorithm — both the
below-preferred and above-preferred cases — is anchored at `PreferredWeeks`,
not at `MinimumWeeks`. This is the interpretation actually implemented:

1. Start every phase at its own `PreferredWeeks`.
2. If `targetWeekCount < sumPreferred` (`Delta < 0`, `Mode = Compression`):
   remove one week at a time, in ascending `CompressionPriority` order,
   round-robin, from any phase still above its own `MinimumWeeks`, until
   `-Delta` weeks have been removed.
3. If `targetWeekCount > sumPreferred` (`Delta > 0`, `Mode = Extension`):
   add one week at a time, in ascending `ExtensionPriority` order,
   round-robin, to any phase still below its own `MaximumWeeks`, until
   `Delta` weeks have been added.
4. If `targetWeekCount == sumPreferred` (`Delta == 0`, `Mode = Preferred`):
   no adjustment — every phase stays at `PreferredWeeks`.

This is a **deliberate, reported deviation** from the prompt's literal
(self-contradictory) two-bullet description, per the prompt's own
instruction not to silently pick a direction. It was chosen because it is
the only interpretation that satisfies both required exact-match anchors
simultaneously (§6, §7) — anchoring at `MinimumWeeks` instead would not
reproduce the 12-week 3/4/4/1 result without adjustment, and would not
cleanly reduce to the 4G.3A 8-week finding either.

Feasibility is checked first, before any allocation is attempted:
`sumMinimum <= targetWeekCount <= sumMaximum`. Outside that range, the
method returns immediately with `IsMathematicallyFeasible = false` and a
typed `ReasonCode` — it never throws for this case (§9).

## 4. CompressionPriority / ExtensionPriority direction — evidence

Direction was not assumed; it was read directly from the real catalog
artifact (`plan-catalog/catalog/templates/ten-k-master.v6.json`):

| Phase | compressionPriority | extensionPriority |
|---|---|---|
| FOUNDATION | 1 | 1 |
| BUILD | 2 | 2 |
| RACE_SPECIFIC | 3 | 3 |
| TAPER | 4 | 4 |

Both fields are identical for every phase and increase monotonically with
declaration order (Foundation → Build → Race-Specific → Taper). This
confirms **lower number = adjusted first**, for both compression and
extension, matching the canonical order (Foundation compresses/extends
first, Taper is compression-protected and never adjusted since its
`[Min,Preferred,Max] = [1,1,1]`). Compression and extension priority agree
completely for this candidate — there is no reconciliation conflict to
report.

**Correction (post-4G.3B.2 review) — this equality is confirmed
UNINTENTIONAL/UNINSPECTED, not a deliberate authoring decision.**
`plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.md`
(entry `AUD-008`, group `phase-metadata`) treats `compressionPriority` and
`extensionPriority` as a **single combined placeholder field**, not two
independently reasoned values:

> `$.phases[*].compressionPriority / extensionPriority` — PLACEHOLDER_UNCONFIRMED — "Ordering priorities invented; no canonical source for relative compression/extension priority. Golden Fixture v3 resolves one plan without needing to compress/extend phases (12 available weeks == 12 core weeks, runwayWeeks=0), so it exercises no compression/extension logic at all and cannot confirm these priorities."

This is corroborated by `phase4f6-step-a1-role-ownership-and-gap-clarification.json`
(`missingId: "M08"`, `classification: "EVIDENCE_ASSESSMENT_REQUIRED"`,
`severity: "BLOCKER_FOR_STEP_B"`, `expectedBy: "product/coaching decision"`)
and by `phase4f6-step-a-v10-catalog-audit.json`'s decision-traceability
table, which lists `PHASE-*-COMPRESSION_EXTENSION_PRIORITY` as `INDIRECT`
link quality, sourced only from AUD-008 itself. No later pass (Step B's
`phase4f6-step-b-training-science-evidence-mapping.json`, Step C's
decision list, or any subsequent phase in this repository) revisits or
resolves AUD-008/M08 — a grep for `AUD-008` across the entire repository
returns only these unresolved audit references and the source-code audit
registry entry (`PilotDomainContentAudit.cs`), no closure note. The
canonical compression order in `appsel-v1-canonical-decisions.md` (§C-01)
covers compression only and says nothing about extension order.

**Conclusion**: extensionPriority was not validated, confirmed, or even
separately considered against compressionPriority at any point in this
repository's history — it appears to have been copied/defaulted alongside
compressionPriority as one invented placeholder pair. This finding is
specific to `TEN_K_MASTER v6` and **must not be generalized** to any
future candidate or distance-family catalog artifact: a future template
with genuinely different compression vs. extension needs (e.g. a phase
that should compress early but extend late, or vice versa) would need
AUD-008 revisited and explicitly resolved before this allocator's
priority-ordering logic could be trusted to produce a correct extension
order for it. This is a catalog-authoring/product open question, not a
code defect, and is deliberately not fixed in this pass.

## 5. Mechanical allocation table, 8–14 weeks

Every row below except **12 weeks** (the only publicly supported horizon)
is labeled verbatim:

> **MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED**

| Target | Mode | Delta | FOUNDATION | BUILD | RACE_SPECIFIC | TAPER | Sum | Status |
|---|---|---|---|---|---|---|---|---|
| 8 | Compression | -4 | 2 | 3 | 2 | 1 | 8 | MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED |
| 9 | Compression | -3 | 2 | 3 | 3 | 1 | 9 | MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED |
| 10 | Compression | -2 | 2 | 3 | 4 | 1 | 10 | MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED |
| 11 | Compression | -1 | 2 | 4 | 4 | 1 | 11 | MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED |
| 12 | Preferred | 0 | 3 | 4 | 4 | 1 | 12 | The only publicly supported horizon — unchanged, existing behavior |
| 13 | Extension | +1 | 4 | 4 | 4 | 1 | 13 | MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED |
| 14 | Extension | +2 | 4 | 5 | 4 | 1 | 14 | MATHEMATICALLY_FEASIBLE_ONLY — NOT VERIFIED SAFE, NOT PUBLICLY SUPPORTED |

Catalog-declared per-phase bounds, for reference: FOUNDATION [2,3,4], BUILD
[3,4,5], RACE_SPECIFIC [2,4,4], TAPER [1,1,1] (fixed, compression-protected).

Every row was verified by direct execution against the real catalog
artifact in `Phase4G3B2GenericPhaseAllocatorTests.cs`
(`Resolve_InRangeNonPreferredTargets_MatchExactPriorityOrderedAllocation`),
not derived by hand and trusted.

## 6. Confirmation: 8-week result matches Phase 4G.3A exactly

`Resolve(candidate, 8)` returns F2/B3/RS2/T1 — byte-identical to the
Phase 4G.3A audit's mathematically-resolved 8-week finding —
with `Mode = Compression`, `Delta = -4`, `IsMathematicallyFeasible = true`,
`ReasonCode = "MATHEMATICALLY_FEASIBLE"`. Asserted literally in
`Resolve_TargetEight_MatchesPhase4G3A_F2B3RS2T1_Exactly`.

## 7. Confirmation: 12-week result equals the existing method's output

`Resolve(candidate, 12)` produces `Mode = Preferred`, `Delta = 0`, and
per-phase weeks `[3, 4, 4, 1]` — identical to `Resolve(candidate)`'s
`Entries` (`TotalWeeks = 12`), cross-checked field-by-field in
`Resolve_TargetTwelve_EqualsExistingCandidateOnlyMethod_3_4_4_1`.

## 8. Purity proof

`Resolve(candidate, targetWeekCount)` was called twice with identical
inputs for `targetWeekCount ∈ {8, 11, 12, 14}`
(`Resolve_CalledTwiceWithIdenticalInputs_ProducesByteIdenticalResults`).
All scalar fields (`TargetWeeks`, `PreferredWeeks`, `Delta`, `Mode`,
`IsMathematicallyFeasible`, `ReasonCode`) and the `Phases` collection
(element-wise) were identical across both calls. No I/O, readiness, or
stage/workout access exists anywhere in the method body (§2 code, reviewed
directly).

## 9. Infeasibility handling and the actual computed feasible range

The actual feasible range was computed, not assumed:
`sum(MinimumWeeks) = 8`, `sum(MaximumWeeks) = 14` for the real
`TEN_K_MASTER v6` candidate — confirmed equal to
`candidate.CoreCycle.MinimumWeeks`/`MaximumWeeks` exactly
(`Resolve_ActualFeasibleRange_IsEightToFourteen_MatchingCoreCycleBounds`).
This means the 8–14 window used throughout this document is not a
coincidental guess: it is the candidate's own structurally-derived bound.

**Correction (post-4G.3B.2 review) — per-step bound enforcement, and what
the 8-week match does/does not prove.** Every single-week adjustment is
gated by a `canAdjust` check evaluated on every loop iteration, not just
against the final total:

```csharp
// CatalogPhaseAllocation.cs:187 (compression)
canAdjust: p => allocated[p.PhaseKey] > p.MinimumWeeks,
// CatalogPhaseAllocation.cs:196 (extension)
canAdjust: p => allocated[p.PhaseKey] < p.MaximumWeeks,
// CatalogPhaseAllocation.cs:234, inside DistributeByPriority's per-pass loop
if (canAdjust(phase)) { adjust(phase); remaining--; progressedThisPass = true; }
```

`canAdjust` is re-evaluated fresh for every phase on every pass, so a
phase that has already reached its bound is skipped from that point
forward while other phases continue to be adjusted — this is what makes
the partial-compression/extension cases (9, 10, 11, 13, 14) work
correctly, since not every phase reaches its bound simultaneously there.

Originally, only `Resolve_InRangeNonPreferredTargets_AreInternallyValid_TaperUnchanged`
(covering 9, 10, 11, 13, 14) asserted `Assert.InRange` explicitly; 8 and 12
were proven in-bounds only indirectly, via exact-value equality to
hardcoded expected allocations. A new test,
`Resolve_EveryFeasibleTarget_NeverAllocatesBelowMinimumOrAboveMaximum`
(`[Theory]` over `8, 9, 10, 11, 12, 13, 14`), was added to
`Phase4G3B2GenericPhaseAllocatorTests.cs` to close that gap explicitly —
every target in the actual feasible range now has a dedicated,
uniform `Assert.InRange` proof, not just the 5 partial-adjustment cases.
This is a correction to Phase 4G.3B.2's delivered test suite, not new
feature work; all 30 tests in the file (23 original + 7 new theory cases)
pass.

Whether the 8-week match to Phase 4G.3A's F2/B3/RS2/T1 result is itself
strong evidence that the bound-checking logic works is a separate, more
subtle question, answered directly: **no, not on its own.** At
`targetWeekCount = 8`, `Delta = -4` exactly equals the candidate's total
compressible headroom (`sum(PreferredWeeks - MinimumWeeks) = 1+1+2+0 = 4`).
Whenever the requested reduction exactly exhausts total headroom, *every*
adjustable phase is arithmetically forced down to its own minimum
regardless of priority order or whether the bound check is even present —
any distribution algorithm that respects individual bounds converges to
the same endpoint at this exact boundary. So the 8-week match validates
the algorithm's *overall correctness* at that one boundary value, but it
does not, by itself, demonstrate that `canAdjust` correctly *discriminates*
between phases (stopping some at their bound while continuing to adjust
others) — that discriminating behavior is only exercised, and only
provable, by the partial-adjustment cases (9, 10, 11, 13, 14), where
`Delta` is strictly between 0 and the total headroom and different phases
stop at different points. Both claims — "8 exactly matches 4G.3A" and
"bounds are enforced per-step for every target" — are true, but they are
independent facts and must not be conflated as either or both is missing.

Targets outside `[8, 14]` (tested: 7, 15, 20, 0) all return
`IsMathematicallyFeasible = false`, an empty `Phases` list, and a
non-empty, descriptive `ReasonCode` (`"TARGET_BELOW_SUM_OF_MINIMUMS: ..."`
or `"TARGET_ABOVE_SUM_OF_MAXIMUMS: ..."`) — never an exception
(`Resolve_TargetOutsideActualFeasibleRange_ReturnsInfeasible_NoException`).

## 10. Backward-compatibility proof

`Resolve(candidate)` (no target week count) was not modified — its method
body is unchanged from before this phase (same structural validation,
same `Entries`/`TotalWeeks` construction, same exceptions). Re-verified by
`Resolve_CandidateOnlyMethod_StillProduces3_4_4_1_Unaffected`: still
returns `TotalWeeks = 12` and `[(FOUNDATION,3), (BUILD,4),
(RACE_SPECIFIC,4), (TAPER,1)]`.

## 11. Confirmation: new overload is unreachable from any live request path

`CatalogPlanSkeletonOrchestrator.cs:203` is the sole production call site
of the resolver, and it calls `_phaseAllocationResolver.Resolve(candidate)`
— the original, single-argument overload. Confirmed by direct grep:

```
backend\RunningApp.Application\RuntimeCatalog\Schedule\Materialization\CatalogPhaseAllocation.cs:25:
    /// <see cref="ICatalogPhaseAllocationResolver.Resolve(PlanCatalogCandidateSummary, int)"/>
backend\RunningApp.Application\RuntimeCatalog\Schedule\Materialization\CatalogPlanSkeletonOrchestrator.cs:203:
    var phaseAllocation = _phaseAllocationResolver.Resolve(candidate);
```

The only other match is the resolver's own XML doc comment referencing its
own new overload's signature — not a call site. This is also proven
structurally by test
(`Resolve_TargetWeekCountOverload_HasNoCallSiteInApplicationProductionCode`),
which regex-scans every `.cs` file under `RunningApp.Application` and
`RunningApp.Api` (excluding `obj`/`bin` and the resolver's own definition
file) for a two-argument `phaseAllocationResolver.Resolve(...)` call and
asserts none exists.

`PlanServices.cs` and `CatalogPreviewGenerator.cs` were not touched at all
this phase.

## 12. Public API / DTO / routing — unchanged

No public DTO, persistence model, migration, controller route, or Flutter
file was touched. `RaceHorizonPolicy`, `LivePlanPreviewRouting`, and the
`PLAN_CORE_HORIZON_UNSUPPORTED`/`PLAN_HORIZON_COMPOSITION_REQUIRED`
fail-closed contract are byte-for-byte unchanged in source. Live HTTP
acceptance confirms this end-to-end (§14).

## 13. Files changed / created

- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs` — added `AllocationMode`, `AllocatedPhase`, `PhaseAllocationResult`, the new `Resolve(candidate, targetWeekCount)` overload, `DistributeByPriority`, and `ValidateStructure`. Original `Resolve(candidate)` method body unchanged.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Phase4G3B2GenericPhaseAllocatorTests.cs` — new, 23 tests.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4G3AEightWeekCoreAllocationAuditTests.cs` — disambiguated an existing `GetMethod("Resolve")` reflection call (now ambiguous with two overloads present) to the candidate-only overload; renamed the test for clarity. No assertion weakened.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrationTerminologyTests.cs` — same disambiguation fix for the same reason, plus the missing `using RunningApp.Application.RuntimeCatalog;` this fix's `PlanCatalogCandidateSummary` reference required. No assertion weakened.
- `PHASE4G_3B_2_GENERIC_PHASE_ALLOCATOR.md` — this document.

No file outside `backend/` was modified by this phase (the pre-existing,
already-modified `plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}`
files were touched by an earlier, prior phase in this session — not this
one; see §15).

## 14. Live acceptance results (8 / 12 / 20 weeks)

Verified against a running `RunningApp.Api` instance
(`ASPNETCORE_ENVIRONMENT=Development`), after `POST /api/v1/testing/reset`:

| Weeks | Request | Result |
|---|---|---|
| 8 (`race_date` = start + 56 days) | `POST /api/v1/plans/generate-preview/race` | HTTP 422, `errorCode: "PLAN_CORE_HORIZON_UNSUPPORTED"`, reason `CORE_HORIZON_8_NOT_IMPLEMENTED` |
| 12 (`race_date` = start + 84 days) | `POST /api/v1/plans/generate-preview/race` | HTTP 200, `template_id: "TEN_K__4D__INTERMEDIATE"`, 12 weeks returned, unchanged shape/content |
| 20 (`race_date` = start + 140 days) | `POST /api/v1/plans/generate-preview/race` | HTTP 422, `errorCode: "PLAN_HORIZON_COMPOSITION_REQUIRED"` |

All three match pre-existing, documented Phase 4G.1/4G.2 behavior exactly —
this phase changed none of it.

## 15. Build/test totals and governance state

- **Backend build**: `dotnet build RunningApp.sln` — 0 warnings, 0 errors.
- **Backend tests**: `dotnet test RunningApp.IntegrationTests` — 1092
  passed, 2 failed, 0 skipped, 1094 total. The 2 failures
  (`RealHost_CatalogLivePilotOptions_DefaultsToDisabled`,
  `RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`)
  are the pre-existing baseline failures unrelated to this phase (present
  before this phase's changes and confirmed unaffected by them).
- **Plan-catalog tests**: `dotnet test tests/PlanCatalog.Tests` — 335
  passed, 0 failed, 0 skipped, 335 total. This phase touched no
  `plan-catalog/` source file.
- **Governance risk entries**: `TD-FOUNDATION-COMPRESSION-001` and
  `TD-TESTFLAKE-001` remain present in
  `plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}`,
  both untouched by this phase (last modified by an earlier, prior phase in
  this session). `currentAppendOnlyStatus` still reads "11 risks are now
  recorded in total: 9 OPEN and 2 CLOSED" — unchanged by this phase's work.

## Next step

Phase 4G.3B.3 (Safety Verification Pipeline) is the next planned phase and
is explicitly **not** started by this document or this phase's code.
