# Phase 4G.3B.3 — Safety Verification Pipeline: Verifier Registry

Definitive, numbered list of every verifier in this pipeline: completed,
remaining, and exact names. Created to resolve a self-flagged
inconsistency ("nine verifiers" vs. a count of ten once
`AllocationOrderCorrectnessVerifier` is included).

## Framing decision

**`AllocationOrderCorrectnessVerifier` is a separate, earlier addition —
not a member of the "nine safety verifiers" set.** It was implemented
before the nine-verifier plan was named, addresses a narrower and
different concern (whether a specific target week count's allocation
*order* can be trusted, tied directly to `TD-ALLOCATION-PRIORITY-001`),
and its own doc comment never claims membership in the nine. Every
subsequent verifier's doc comment (starting with `PhaseConstraintVerifier`)
explicitly frames itself as one of nine, with the others named as
"remaining eight" at that point. This framing is used consistently from
this document forward — do not recount `AllocationOrderCorrectnessVerifier`
as verifier "one of nine" in any future report.

## The nine safety verifiers

| # | Verifier | Status | File |
|---|---|---|---|
| 1 | `PhaseConstraintVerifier` | **Done** (Phase 4G.3B.3) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/PhaseConstraintVerifier.cs` |
| 2 | `RaceSpecificCapacityVerifier` | **Done** (Phase 4G.3B.3a) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/RaceSpecificCapacityVerifier.cs` |
| 3 | `StageReachabilityVerifier` | **Done** (Phase 4G.3B.3b) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/StageReachabilityVerifier.cs` |
| 4 | `WorkoutExposureVerifier` | **Done** (Phase 4G.3B.3, undated sub-letter) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/WorkoutExposureVerifier.cs` |
| 5 | `GoalPaceReachabilityVerifier` | **COMPLETE — DARK / UNWIRED** (Phase 4G.3B.3, checkpoint commit `b32a9f5`) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/GoalPaceReachabilityVerifier.cs` |
| 6 | `ReadinessEligibilityVerifier` | **COMPLETE — DARK / UNWIRED** (Phase 4G.3B.3, checkpoint commit `1d9596e`) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/ReadinessEligibilityVerifier.cs` |
| 7 | `VolumeProgressionVerifier` | **COMPLETE — DARK / UNWIRED** (Phase 4G.3B.3, checkpoint commit `a11203b`) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/VolumeProgressionVerifier.cs` |
| 8 | `LongRunProgressionVerifier` | **COMPLETE — DARK / UNWIRED** (Phase 4G.3B.3, checkpoint commit `80e9f99`) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/LongRunProgressionVerifier.cs` |
| 9 | `RaceDateAlignmentVerifier` | **COMPLETE — DARK / UNWIRED** (Phase 4G.3B.3, checkpoint commit `1b5f376`) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/RaceDateAlignmentVerifier.cs` |

**9 of 9 complete.**

> **Status note (Phase 4G.3B.4a checkpoint audit):** all nine canonical
> standalone safety verifiers listed above are implemented, tested, and
> committed. "COMPLETE — DARK / UNWIRED" is a status vocabulary addition
> introduced by this note (this document previously had no completed-status
> term beyond the informal **Done** used for verifiers 1-4) — it means
> exactly what "Done" already meant for verifiers 1-4 (implemented, tested,
> committed, zero production call sites, not registered in DI) and carries
> no different or stronger claim. Completion of these nine verifiers does
> **not** mean any of the following: no safety-verification orchestration
> pipeline exists yet (remains **not started** — see the "Common
> properties" section below, unchanged); none of the nine is wired into
> `CatalogPreviewGenerator`, `PlanServices`, or any live request path;
> no support registry exists; no new public race-plan horizon has been
> enabled by this or any verifier's completion. This note updates only the
> stale status cells above to reflect repository truth as of the Phase
> 4G.3B.4a checkpoint audit — it does not revise the design, the canonical
> nine-verifier list, or the framing decision below.

## Separate, earlier addition (not one of the nine)

| Verifier | Status | File |
|---|---|---|
| `AllocationOrderCorrectnessVerifier` | **Done** (Phase 4G.3B.3, predates the nine-verifier plan) | `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/AllocationOrderCorrectnessVerifier.cs` |

## Common properties across all verifiers implemented so far

- All are `internal static class`, pure functions of their explicit
  parameters, no I/O, no constructor/instance state.
- None are called from any file under `RunningApp.Application` or
  `RunningApp.Api` production code — each has its own reflection or
  regex-based "no call site" test proving this.
- None wire into `CatalogPreviewGenerator`, `PlanServices`, or any live
  request path.
- No orchestrating pipeline exists yet that composes any of them — each
  is a standalone, independently testable unit until an explicit future
  phase wires them together.

## Next step

Phase 4G.3B.3 continues with `GoalPaceReachabilityVerifier` (or whichever
of the five remaining verifiers is picked next) as an explicit,
separately authorized pass — not started by this document.
