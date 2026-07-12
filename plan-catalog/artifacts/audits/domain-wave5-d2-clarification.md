# Wave 5 / D2 — Clarification, Consistency & Safety Pass

Not a new domain wave. Resolves no new decision — D3, D4, D13 remain untouched. No D2 value changed; no v7 candidate value changed.

## Issue 1 — `allowGoalPaceRehearsal` is write-only

**Has any reader?** No. `grep -rln "AllowGoalPaceRehearsal" src/` matches only the property declaration in `ProgressionModifierDefinition.cs`. Confirmed by a new executable test, `AllowGoalPaceRehearsal_HasNoReaderAnywhereInThisRepository`.

**Corrected interpretation** (all confirmed true):
- It is currently write-only authoring metadata / a capability declaration.
- It does not gate anything today.
- `GOAL_FEASIBILITY_IN` (the real `GOAL_PACE_REHEARSAL` stage guard) is structurally independent — it lives on a different artifact (`TEN_K_WORKOUT_PROGRESSION_V1`'s stage) and does not read this boolean.
- `true` alone does not make any workout eligible.
- `false` alone would not currently block anything — enforcement would require a future consumer.

**Runtime validator considered, not added.** The rule "`allowGoalPaceRehearsal==false` → no goal-pace-rehearsal candidates reachable in this modifier's progression graph" would require a new cross-check: `ProgressionModifier` has no existing relationship to `WorkoutProgression`/stages in the data model (reached via a structurally separate `LevelModifier` path, not via `MasterTemplate→WorkoutProgression`). Building that edge is a real architecture change, and the current approved value is `true`, so the `false` branch would be dead code today. Per instruction not to add runtime behavior in this pass, this is recorded as **technical debt** instead:

> **TD-WAVE5-001** — No cross-check between `ProgressionModifier.AllowGoalPaceRehearsal` and `WorkoutProgression` goal-pace-rehearsal stage reachability. If a future value sets this to `false` while a reachable workout progression still contains a goal-pace-rehearsal stage/candidate, nothing detects the inconsistency today. Recommended fix: extend `TemplateCombinationValidator` (which already resolves both `progressionModifier` and `progression` together) to check this when `AllowGoalPaceRehearsal==false`. Not blocking today because the false branch is unreachable in the current catalog.
>
> **D13 forward-looking note (added in the file-attribution follow-up pass):** TD-WAVE5-001 should be revisited during D13 GOAL_PACE_TEN_K resolution because adding or changing goal-pace workout candidates may make the missing `allowGoalPaceRehearsal` cross-check behaviorally relevant.
>
> **D13 revisit result (Wave 8): UPDATED, still OPEN — no active guard added.** D13 resolution (`GOAL_PACE_TEN_K v2`) confirms concrete, real goal-pace candidates now formally exist and are reachable in the `TEN_K__4D__INTERMEDIATE v10` candidate graph (`GOAL_PACE_REHEARSAL` stage, gated only by `GOAL_FEASIBILITY_IN` — never by `AllowGoalPaceRehearsal`). The current state is **consistent** (`AllowGoalPaceRehearsal=true` and candidates do exist — no contradiction today), so no active runtime guard was added: the cross-check would still require a genuine new edge between `ProgressionModifier` and `WorkoutProgression`/stage data (structurally separate paths today), and D13's scope is a training-content decision, not a validator-architecture decision. TD-WAVE5-001 remains open and is now cross-referenced in `activation-readiness-risks.json` alongside TD-D3-001.

## Issue 2 — ownership label conflict

**Was `RUNTIME_GUARD_ONLY` retained or corrected?** **Corrected.** It was inconsistent with the field having zero readers.

**Final ownership label:** `TECHNICAL_METADATA` / `PRODUCT_CAPABILITY_DECLARATION (UNCONSUMED)` — the closest existing equivalent in this repository's ownership taxonomy (`PRINCIPLE_FLAG_UNCONSUMED` is not a supported category anywhere in the codebase).

Files updated: `domain-wave5-d2-ownership.json`/`.md` (label + reasoning corrected), `PilotDomainContentAudit.cs` `AUD-333` (append-only `WAVE5-CLARIFICATION` note; original wording preserved, not deleted — no historical fact rewritten).

## Issue 3 — modifier reuse protection

**Prior state:** No real protection. `OnlyOneLevelModifierReferencesIntermediateProgressionModifierV1_OwnershipReuseBoundaryConfirmed` only checked `LevelModifier.Metadata.Key`, which is trivially true regardless of which combination(s) actually use that level modifier.

**Protection added — EXECUTABLE.** New test: `PlanCatalog.Tests.Validation.DomainWave5D2ResolutionTests.IntermediateProgressionModifierV2_ReuseIsExecutablyGuarded_NoUnapprovedCombinationFamilyReferrer`. It enumerates every combination reaching `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2` (via its `LevelModifier`) and asserts `DistanceFamily==TenK`, `RunsPerWeek==4`, `Experience==Intermediate`, and combination key `TEN_K__4D__INTERMEDIATE`. It fails the moment an unrelated combination family becomes reachable.

A production runtime validator (e.g. a new `CandidatePublishGraphValidator` rule) was **not** added — a test-level guard was judged sufficient given zero current violations and only one artifact affected; a new production rule would be a broader architecture change than this pass warrants.

## Issue 4 — file attribution

See `domain-wave5-file-attribution.json`/`.md` for the exact per-file table.

## Issue 5 — `TEN_K_MASTER v5` cascade

See the "Clarification pass addendum" section appended to `domain-wave5-version-cascade.md`. Summary: `TEN_K_MASTER v5` was created in **Wave 3**, unchanged by Wave 5. `TEN_K_MASTER` does not reference `LevelModifier`/`ProgressionModifier` at all — the changed reference (`INTERMEDIATE_MODIFIER v4→v5`) is owned directly by `TemplateCombinationDefinition.LevelModifier`. **No cascade defect found; the Wave 5 cascade is complete.**
