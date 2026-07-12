# Backend Integration Phase 4D.5.1 — PaceSource / GoalFeasibility Cross-Risk Note

Documentation/risk-note-only pass. Makes visible, in the existing risk-tracking system, a behavioral
dependency between `PaceSourceResolver` and `GoalFeasibilityResolver` discovered during Phase 4D.5
orchestration testing. No resolver behavior was changed. No tests were added (none were required — this is
a pure documentation/risk-tracking update, and the underlying finding is already covered by an existing
Phase 4D.5 test).

## The finding (restated for context, not re-derived)

`GoalFeasibilityResolver` has a documented branch for `PACE_SOURCE_IN=NONE` with `TargetFinishTimeSeconds`
present (returns `NotEvaluated`/`PACE_SOURCE_NONE_TARGET_TIME_REQUESTED`). Phase 4D.5's real end-to-end
orchestration testing (`RuntimeConditionResolutionServiceTests.cs`) found this branch is **currently
unreachable** through `RuntimeConditionResolutionService.ResolveAll`: `PaceSourceResolver`'s own output
priority is `RECENT_RACE > TARGET_TIME > NONE`, so whenever `TargetFinishTimeSeconds` is present,
`PaceSourceResolver` can only ever emit `NONE` if it *also* lacks a target time to fall back to — but
`GoalFeasibilityResolver` already short-circuits to `NOT_REQUESTED` in that exact case, before
`PACE_SOURCE_IN` is even consulted. This is not a bug — both resolvers behave exactly as documented — it is
an **implicit behavioral dependency**: `GoalFeasibilityResolver`'s branch reachability depends on
`PaceSourceResolver`'s specific priority ordering, a fact that was invisible when each resolver was tested
in isolation (Phase 4D.4) and only became visible once they were run together (Phase 4D.5).

## Why this matters for the future

Nothing in the type system enforces this relationship — it is pure runtime behavior of two independently
implemented `Resolve` methods. If a future phase changes `PaceSourceResolver`'s priority order for any
reason (most plausibly: implementing the `ESTIMATED` path tracked by `TD-PACESOURCE-001`, which could
plausibly be inserted anywhere in the priority list), `GoalFeasibilityResolver`'s
`PACE_SOURCE_NONE_TARGET_TIME_REQUESTED` branch could become reachable again — with whatever behavior it
currently has, which was written and tested under the assumption (correct today) that it would rarely or
never actually execute in the composed pipeline. Nothing about this note requires that assumption to be
wrong now; it only records that the assumption exists and should be re-checked if the underlying priority
order ever changes.

## Where this is now tracked

Added as an `implementationNote` field to the existing `TD-PACESOURCE-001` entry in
`plan-catalog/artifacts/audits/activation-readiness-risks.json` (and a corresponding inline annotation in
the `.md` risk table), rather than as a new `TD-*` entry — this cross-risk finding is a direct consequence
of `TD-PACESOURCE-001`'s own subject matter (`PaceSourceResolver`'s `ESTIMATED`/priority-order question),
so attaching it to that existing entry keeps the two facts co-located rather than fragmenting related
context across multiple risk IDs. `TD-PACESOURCE-001` itself is **not closed** — its `status` field remains
`"OPEN"`, unchanged; only the `implementationNote` field was added.

## Exact note added to `TD-PACESOURCE-001`

> Backend Integration Phase 4D.5.1 (cross-risk finding, discovered during Phase 4D.5 orchestration
> testing): if the ESTIMATED path described above is later implemented, OR PaceSourceResolver's output
> priority (currently RECENT_RACE > TARGET_TIME > NONE) changes for any other reason, re-verify
> GoalFeasibilityResolver behavior for the combination PACE_SOURCE_IN=NONE with targetFinishTimeSeconds
> present. Reason: as of Phase 4D.5, GoalFeasibilityResolver's own PACE_SOURCE_NONE_TARGET_TIME_REQUESTED
> branch is unreachable through real RuntimeConditionResolutionService.ResolveAll orchestration, because
> PaceSourceResolver already emits TARGET_TIME (not NONE) whenever targetFinishTimeSeconds exists —
> GOAL_FEASIBILITY_IN never sees PACE_SOURCE_IN=NONE while a target time is also present under the current
> pipeline. This is a behavioral dependency between the two resolvers' logic, not a type-system guarantee —
> nothing prevents a future change to PaceSourceResolver's priority order (e.g. if ESTIMATED is introduced
> ahead of TARGET_TIME, or the priority order is otherwise revised) from making this branch reachable again
> with different semantics than originally intended. See
> PHASE4D_5_RUNTIME_CONDITION_RESOLVER_ORCHESTRATION_SERVICE.md ("a new finding") and
> PHASE4D_5_1_PACESOURCE_GOALFEASIBILITY_CROSS_RISK_NOTE.md for full detail. This note does not change
> PaceSourceResolver or GoalFeasibilityResolver behavior — it only records the dependency for future
> re-verification.

(Verbatim JSON field: `TD-PACESOURCE-001.implementationNote`. The `.md` table row carries the same content
condensed into a bolded inline annotation.)

## Confirmations

- `TD-PACESOURCE-001.status` remains `"OPEN"` — not closed, not modified in any other field besides the new
  `implementationNote` addition.
- No resolver behavior changed: neither `PaceSourceResolver.cs` nor `GoalFeasibilityResolver.cs` nor
  `RuntimeConditionResolutionService.cs` was touched in this pass.
- No test or other code file was changed — the finding is already covered by an existing Phase 4D.5 test
  (`EndToEnd_NoRecentRaceButTargetTimeRequested_PaceSourceIsTargetTime_GoalFeasibilityReturnsDocumentedNotEvaluated`
  in `RuntimeConditionResolutionServiceTests.cs`), so no new test was required to validate this
  documentation-only change.
- No live generation wiring was added or changed.
- No registry value was changed.
- No golden fixture was changed.
- No other plan-catalog artifact was modified beyond the two risk-file edits described above.

**Final classification: `BACKEND_HAS_PACESOURCE_GOALFEASIBILITY_CROSS_RISK_NOTE_NOT_WIRED_TO_GENERATION`.**
