# Backend Integration Phase 4D.3 — CORE_ENTRY_READINESS_IN Resolver: Thresholds Decision Required

Implements `CoreEntryReadinessResolver` as a **contract-conformant placeholder**, not a real readiness
classifier: it always returns `NotEvaluated`, never `READY`/`CAUTION`/`NOT_READY`, and never the invalid
`STANDARD` value. No guessed thresholds were implemented. Blocked pending an owner-approved threshold
mapping — tracked as activation risk `TD-CORE-READINESS-001`.

## Whether approved thresholds were found

**No.** A repository-wide, evidence-only search (no reliance on general running-domain knowledge, per
explicit instruction) found exactly one threshold set anywhere in the repo, and it does not qualify as an
approved V1 mapping for the reasons below.

## Exact threshold source found (and why it is insufficient)

`plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`, step
`CORE_ENTRY_READINESS_RESOLVER`, rule `CORE_ENTRY_READINESS_V1` / `TEN_K_STANDARD_ENTRY`:

```json
"thresholds": { "minimumWeeklyVolumeKm": 20, "minimumLongestRunKm": 8, "minimumRunsPerWeek": 3 },
"result": { "readiness": "STANDARD", "gaps": [] }
```

Three independent reasons this does not constitute an approved V1 threshold mapping:

1. **Single gate, not a three-tier classification.** The fixture evidences exactly one evaluation — a
   pass with `gaps: []`. There is no failing example anywhere in the fixture, so no boundary between
   `CAUTION` and `NOT_READY` (or between `NOT_READY` and anything else) is evidenced at all. Implementing a
   three-way split from a single pass/fail gate would require inventing the missing boundary — exactly what
   this phase was instructed not to do.
2. **Invalid output value.** The evidenced output, `"STANDARD"`, is not a valid `CORE_ENTRY_READINESS_IN`
   registry value (confirmed directly against `runtime-condition-values.v2.json`: allowed values are
   `READY`/`CAUTION`/`NOT_READY`). This is the same anomaly tracked as `TD-REGISTRY-001` (still `OPEN`) —
   the one piece of evidence available is itself a known defect, not usable as a clean threshold source.
3. **Already classified `DECISION_REQUIRED` by a prior phase.** `PHASE4A_RUNTIME_RESOLVER_DECISION_SET_CORRIGENDUM.md`
   §6 investigated this exact threshold set and concluded scope classification **"D — not approved /
   DECISION_REQUIRED"** — undecided whether it applies to compressed-plan readiness override only, general
   core-entry readiness, or both. `docs/canonical/appsel-v1-canonical-decisions.md` §B.5 restates the same
   open question. Nothing since has resolved it.

No other document — `PHASE4A_2`, `PHASE4A_3`, `PHASE4B`, any Phase 4D doc, `appsel-race-plan.v4.json`
(whose `rules`/`policies` arrays are empty), or any other catalog artifact — defines a readiness threshold
of any kind.

## Why thresholds were not invented

Per explicit instruction: "Do not use memory. Do not infer thresholds from general running knowledge. Do
not invent values." A plausible-sounding CAUTION/NOT_READY split (e.g., some fraction of the 20/8/3 minimums)
would be exactly the kind of invented product logic this task forbids — there is a meaningful difference
between "this repo has evidence for one gate" and "this repo has evidence for a three-tier product
classification," and only the former is true.

## `CoreEntryReadinessResolver` — what was implemented

`CoreEntryReadinessResolver : ICoreEntryReadinessResolver` (the interface already existed from Phase 4C —
no interface change needed). `Resolve(RuntimeResolverContext context)` **unconditionally** returns:

```csharp
RuntimeConditionResolutionResult.NotEvaluated(
    "CORE_ENTRY_READINESS_IN",
    "CORE_ENTRY_READINESS_THRESHOLDS_NOT_APPROVED",
    context.InputSnapshot,
    warnings: [ "... see TD-CORE-READINESS-001 ..." ])
```

regardless of what evidence `context.InputSnapshot` carries — including a snapshot with
`RecentWeeklyVolumeKm=24, RecentLongestRunKm=9, RecentRunsPerWeek=4` (the exact values from the golden
fixture's own `TEN_K_STANDARD_ENTRY` facts), which still returns `NotEvaluated`, not `READY` and not
`"STANDARD"` (test: `Resolve_NeverEmitsStandard`). This is a deliberate design choice, allowed explicitly
by the task's own fallback path ("optionally add resolver class that returns NotEvaluated for
missing/unsupported readiness rule context, only if this does not misrepresent behavior") — it does not
misrepresent behavior because it never claims to have made a real readiness decision.

**This `NotEvaluated` is semantically different from `TimeAdequacyResolver`'s or `PaceSourceResolver`'s use
of `NotEvaluated`** (which fire only for a specific request missing specific optional evidence).
`CoreEntryReadinessResolver` returns `NotEvaluated` for *every* call, including calls with complete
evidence, because the threshold *mapping itself* is unapproved — not because any individual request lacks
data. Documented explicitly in the class's own doc comment to avoid future confusion between these two
different reasons for the same status value.

## READY / CAUTION / NOT_READY behavior

**None implemented.** The resolver never produces any of these three values under any input, by design,
until `TD-CORE-READINESS-001` is resolved.

## NotEvaluated behavior

Always returned, with `ReasonCode = "CORE_ENTRY_READINESS_THRESHOLDS_NOT_APPROVED"`, `OutputValue = null`,
and a `Warnings` entry pointing to `TD-CORE-READINESS-001`. `InputSnapshot` is still attached to the result
for traceability, even though it plays no role in the decision.

## Missing optional evidence behavior

Not distinguished from any other case — since the resolver ignores its input entirely (thresholds don't
exist to apply), missing vs. present evidence produces identical output. This is intentional, not an
oversight: there is nothing to classify either way.

## Invalid numeric evidence behavior

The resolver performs no validation of its own (it never inspects the numeric fields at all), consistent
with the existing convention that Phase 4B's positivity checks in `PlanServices.GeneratePreviewAsync`
remain the sole defensive-validation layer. No duplicate validation was added.

## `STANDARD` anomaly and `TD-REGISTRY-001` status

`STANDARD` is never emitted by this resolver, confirmed by test regardless of input. Registry validation
tests confirm `STANDARD` is invalid for `CORE_ENTRY_READINESS_IN` and valid for `PLAN_MODE_IN` (matching
Phase 4A.2's finding of where it actually belongs). **`TD-REGISTRY-001` is unaffected and remains `OPEN`** —
this phase does not resolve, close, or depend on resolving it; `CoreEntryReadinessResolver`'s
always-`NotEvaluated` design works regardless of `TD-REGISTRY-001`'s eventual resolution.

## Registry validation

`READY`/`CAUTION`/`NOT_READY` confirmed valid `CORE_ENTRY_READINESS_IN` values against the real registry
file; `STANDARD` confirmed invalid; a resolver-produced `NotEvaluated` result confirmed contract-valid via
`RuntimeConditionRegistrySnapshot.IsValid` (which never performs a registry lookup for `NotEvaluated`
results, per the Phase 4D.1.5 contract). Two cross-resolver regression tests additionally confirm
`GOAL_FEASIBILITY_IN.NOT_REQUESTED` (an `Evaluated` registry value) and `PACE_SOURCE_IN.NONE` (also
`Evaluated`) both remain clearly distinct from this resolver's `NotEvaluated` status.

## `TD-CORE-READINESS-001`

Added to `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`,
`classification = UNAPPROVED_THRESHOLD_MAPPING`, `severity = ACTIVATION_RISK`, `status = OPEN` — **not
closed by this pass**. Resolution requires one of: approving full three-tier V1 thresholds with an
evidenced boundary for all three values; explicitly deciding `CORE_ENTRY_READINESS_IN` stays
`NotEvaluated`/unused in V1; or updating registry/docs if the intended shape differs from a three-tier
classification. Guessed thresholds must not be used to close it.

## Confirmation: not wired to live generation

- `Program.cs` registers no `CoreEntryReadinessResolver` in DI.
- `PlanServices`/`PlaceholderPlanGenerationEngine` were not modified; reflection-based tests confirm
  neither constructor takes a `CoreEntryReadinessResolver`/`ICoreEntryReadinessResolver` parameter.
- Re-run, with readiness-evidence fields present in the request, of both the existing-supported-template
  preview flow and the `TEN_K`/`INTERMEDIATE`/4-day `PlanTemplateNotAvailableException` case — both behave
  exactly as before this phase.
- `GeneratePreviewResponse_HasNoCoreEntryReadinessResolutionProperty` structurally confirms no public
  response DTO exposes `RuntimeConditionResolutionResult` or `ResolverDecisionTrace`.

## Remaining work for Phase 4D.4

1. Resolve `TD-CORE-READINESS-001` with product/owner input before implementing real
   `CoreEntryReadinessResolver` logic.
2. Resolve `TD-REGISTRY-001` before any golden-fixture-v4 work that would otherwise re-validate this
   resolver against fixture evidence.
3. Implement `GOAL_FEASIBILITY_IN` — still entirely unstarted; will need
   `RuntimeResolverContext.PriorResults` (potentially including this resolver's own always-`NotEvaluated`
   output) once a feasibility method is separately approved.
4. `PLAN_MODE_IN` remains unimplemented.
5. `IRuntimeConditionResolutionService.ResolveAll` composing all resolvers into one trace remains
   unimplemented (now three resolvers exist: `TimeAdequacyResolver`, `PaceSourceResolver`,
   `CoreEntryReadinessResolver`).
6. Live generation wiring remains entirely out of scope.

## Confirmations

- No plan-catalog artifact was modified — only the two activation-readiness-risk files (append-only) and
  the evidence-log files (untouched by this specific phase) live under `plan-catalog/`.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`, untouched.
- `TD-PACESOURCE-001`/`TD-PACESOURCE-002` remain `OPEN`, untouched.
- `EV-005` remains `PROPOSED`; `EV-006`/`EV-007` remain `ACCEPTED_AS_SUPPORTING_EVIDENCE`, all untouched.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.
- No new public API field was added.

**Final classification: `BACKEND_BLOCKED_CORE_ENTRY_READINESS_THRESHOLDS_DECISION_REQUIRED`.**
