# Backend Integration Phase 4D.3.1 — CORE_ENTRY_READINESS_IN Threshold Owner Decision and Resolver Activation

Converts `CoreEntryReadinessResolver` from Phase 4D.3's always-`NotEvaluated` skeleton into a real V1
classifier, using owner-approved thresholds recorded as an explicit internal canonical product decision
(not external scientific sourcing — none is claimed). Updates evidence-log and activation-risk tracking so
the decision is durable and discoverable. Resolver remains unwired from live generation.

## Owner-approved threshold decision

Recorded verbatim from the task's own conversation-provided decision:

| Output | Condition |
|---|---|
| `READY` | `RecentWeeklyVolumeKm >= 15` **AND** `RecentLongestRunKm >= 6` |
| `NOT_READY` | `RecentWeeklyVolumeKm < 8` **OR** `RecentLongestRunKm < 4` |
| `CAUTION` | Everything else: both fields present but in the `[8,15)`/`[4,6)` bands, or exactly one field present (partial evidence) |
| both missing | `NOT_READY` if `GoalType == Race`; otherwise `NotEvaluated` |

`RecentRunsPerWeek` is explicitly **metadata/supporting evidence only** — never a hard gate for any output
value, per the same owner decision.

**Precedence, made explicit in code (not left ambiguous):** when both fields are present, the `NOT_READY`
check runs *before* the `READY` check. This means a low value on either field overrides a high value on
the other — e.g. `weekly=20, longest=3` is `NOT_READY`, not `READY`, even though weekly alone would qualify.
This ordering was derived directly from the owner's stated rules (each condition is an independent `OR`)
and verified against every boundary example the task itself supplied (`weekly=14.9`/`longest≥6` →
`CAUTION`; `weekly≥15`/`longest=5.9` → `CAUTION`; `weekly=8`/`longest=6` → `CAUTION`; `weekly=15`/`longest=4`
→ `CAUTION`) — all eight now pass as tests.

## Evidence-log update — `EV-008` (append-only, `EV-005` not mutated)

The evidence-log's own documented policy is append-only (`plan-catalog/docs/evidence-log.md` §"Entries are
append-only. Do not edit or delete a prior entry's substantive fields..."). Per that policy, `EV-005` (the
broader, still-unreconciled Appsel V1 canonical-decisions import) was **not** mutated — it remains
`PROPOSED`, since it covers a distinct, broader question than this scoped V1 threshold decision. Instead,
**`EV-008`** was appended: `qualityLabel = INTERNAL_CANONICAL_DECISION`,
`status = ACCEPTED_AS_SUPPORTING_EVIDENCE` (same tier as `EV-006`/`EV-007`, the established pattern for an
accepted internal decision — `evidence-log.md`'s allowed-status list has no
"ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE" option, same finding as every prior phase that checked this).
Source: the explicit owner decision text itself, recorded in this document — no external scientific
citation was invented or implied.

## `TD-CORE-READINESS-001` update

Not mechanically closed — this repository's activation-readiness-risks file has never closed a risk
outright (e.g. `TD-WAVE5-001` is explicitly annotated "revisited, not closed" after its own D13 recheck);
this phase follows that same established convention rather than inventing a new closure mechanism. Instead,
a `resolutionNote` field was added to the JSON entry (and the `.md` table row was annotated
"*RESOLVED IN PART — see resolution note*") stating: thresholds were approved and implemented in Phase
4D.3.1 per `EV-008`; `STANDARD` is still never emitted; the risk **remains `OPEN`** because a residual risk
exists — the resolver, though now implementing real logic, is still not wired into live generation and has
not been exercised against real traffic. `status` field itself stays `"OPEN"`.

**`TD-REGISTRY-001` is explicitly confirmed unrelated and untouched** — it tracks the golden-fixture
`STANDARD` vocabulary defect specifically (a fixture/registry mismatch), which is an entirely separate
question from whether READY/CAUTION/NOT_READY thresholds are approved. Resolving the threshold question
does not resolve, and was never claimed to resolve, the `STANDARD` defect. `TD-REGISTRY-001.status` remains
unmodified (`"OPEN"`).

## READY behavior

`RecentWeeklyVolumeKm >= 15 AND RecentLongestRunKm >= 6` → `Evaluated`/`READY`/`CORE_ENTRY_READY`. Verified
including a case with a low `RecentRunsPerWeek` (=1) alongside qualifying weekly/longest values — still
`READY`, proving `RecentRunsPerWeek` is never a hard gate (test:
`Resolve_ReadyEvidence_LowRecentRunsPerWeek_StillReady_NotHardGated`).

## CAUTION behavior

Fires in two distinct situations, both tested at every stated boundary: (1) both fields present but neither
clearly `READY` nor `NOT_READY` (the `[8,15)` weekly band or `[4,6)` longest-run band); (2) exactly one of
the two fields present (partial evidence), regardless of that single field's own value. Metadata always
includes a `triggeredCriterion` entry describing which condition fired.

## NOT_READY behavior

`RecentWeeklyVolumeKm < 8` **or** `RecentLongestRunKm < 4` (checked first, both-present case), or both
fields missing in a `GoalType == Race` context. Metadata includes `triggeredCriterion`.

## NotEvaluated behavior

Only when both `RecentWeeklyVolumeKm` and `RecentLongestRunKm` are missing **and** the plan is not
identifiably a race-based performance plan. `ReasonCode = "CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT"`,
`OutputValue = null`. Two sub-cases, both returning this same result: `GoalType == Habit` (no repo evidence
requires core-readiness classification for a non-race plan — the task's own instruction: "Missing both in a
non-race / habit context may remain NotEvaluated unless repo/product evidence requires core readiness for
that plan type," and none was found), and `GoalType == null` (unknown — see the race-vs-habit context
section below for why this was NOT treated as a race-context guess).

## `RecentRunsPerWeek` metadata-only behavior

Included in `Metadata["recentRunsPerWeek"]` whenever present; never referenced in any conditional branch of
the classification logic (confirmed by inspection — the field is read only inside `BuildBaseMetadata`, never
inside the READY/CAUTION/NOT_READY decision tree). `Metadata["recentRunsPerWeekUsedAsHardGate"] = "false"`
is emitted on every result as an explicit, inspectable confirmation of this rule, per the task's own
metadata specification.

## Missing evidence behavior

One field missing → always `CAUTION` (unconditional partial-evidence rule, regardless of the present
field's value). Both missing → `NOT_READY` (race context) or `NotEvaluated` (non-race/unknown context), per
above. Missing evidence is never treated as a validation error — consistent with every prior resolver in
this track.

## Race vs. habit context behavior

Uses the **existing** `ResolverInputSnapshot.GoalType` field (already present since Phase 4C, already used
for exactly this race/non-race distinction by `TimeAdequacyResolver` since Phase 4D.1.5) — no new context
field was needed or added, satisfying the task's "use existing context only" instruction.

**`GoalType == null` (unknown) is treated the same as `GoalType == Habit`** (both → `NotEvaluated`), not as
`GoalType == Race`. This is a deliberate, documented conservative choice, not a guess: the resolver cannot
assume a "race-based performance plan context" it has no evidence for. This directly mirrors
`TimeAdequacyResolver`'s own established precedent (Phase 4D.1.5) of not assuming race context for a null
`GoalType`, and is the more conservative of the two possible defaults (it never silently produces a
`NOT_READY` verdict for a plan whose type is genuinely unknown). This is recorded here explicitly as the
chosen interpretation — the task allowed "document `DECISION_REQUIRED`... keep missing-both behavior
conservative but explicit" as a fallback if repo evidence didn't clearly resolve this; here, `GoalType`
itself IS clear existing repo evidence for the Race/Habit split, and the null case's resolution
(conservative, non-guessing) is the explicit design choice being documented, not a residual open question.

## Invalid numeric evidence behavior

Unchanged convention: the resolver performs no positivity validation of its own — Phase 4B's existing
checks in `PlanServices.GeneratePreviewAsync` remain the sole defensive-validation layer. Test
(`Resolve_DoesNotThrowOnValidPositiveNumericInput`) confirms the resolver's own logic path adds no
redundant validation and correctly classifies small-but-valid positive values.

## `STANDARD` anomaly and `TD-REGISTRY-001` status

`STANDARD` is never emitted under any input, confirmed by test even with evidence exactly matching the
golden fixture's own `TEN_K_STANDARD_ENTRY` facts (`weeklyVolumeKm=24, longestRunKm=9`) — which now
correctly classifies as `READY` (since `24>=15` and `9>=6`), not `STANDARD`. `TD-REGISTRY-001` remains
**`OPEN`**, untouched by this phase — it is a separate, unrelated defect (see "TD-CORE-READINESS-001
update" above).

## Registry validation

`READY`/`CAUTION`/`NOT_READY` confirmed valid `CORE_ENTRY_READINESS_IN` registry values against the real
file; `STANDARD` confirmed invalid (and confirmed valid for `PLAN_MODE_IN`, matching Phase 4A.2's finding
of where it belongs). All resolver-produced `Evaluated` results across READY/CAUTION/NOT_READY scenarios,
and the `NotEvaluated` result, confirmed contract-valid via `RuntimeConditionRegistrySnapshot.IsValid`.

## Confirmation: not wired to live generation

Unchanged from every prior phase: `Program.cs` registers no `CoreEntryReadinessResolver`; reflection-based
tests confirm neither `PlanServices` nor `PlaceholderPlanGenerationEngine` takes it as a constructor
dependency; the existing-supported-template preview flow and the `TEN_K`/`INTERMEDIATE`/4-day
`PlanTemplateNotAvailableException` case were both re-run with readiness-evidence fields present and behave
exactly as before this phase; no public response DTO exposes `RuntimeConditionResolutionResult` or
`ResolverDecisionTrace`.

## Remaining work for `GOAL_FEASIBILITY_IN`

1. `GOAL_FEASIBILITY_IN` remains entirely unimplemented — the fourth and final V1 condition resolver.
2. `PLAN_MODE_IN` remains unimplemented.
3. `IRuntimeConditionResolutionService.ResolveAll` composing all resolvers (now three real classifiers plus
   the pattern for a fourth) into one `ResolverDecisionTrace` remains unimplemented.
4. `TD-CORE-READINESS-001`'s residual risk (unwired-to-generation) should be re-reviewed, not silently
   dropped, before any future phase wires this resolver into live generation.
5. Live generation wiring for any resolver remains entirely out of scope.

## Confirmations

- No plan-catalog artifact was modified beyond the two evidence-log files and the two activation-risk
  files, all append-only edits.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`, untouched.
- `TD-PACESOURCE-001`/`TD-PACESOURCE-002` remain `OPEN`, untouched.
- `EV-006`/`EV-007` unchanged.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.
- No new public API field was added.

**Final classification: `BACKEND_HAS_CORE_ENTRY_READINESS_RESOLVER_NOT_WIRED_TO_GENERATION`.**
