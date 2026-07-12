# Phase 4E.1.1 — Governance Clarification and Reconciliation Pass

Scope: answer four acceptance questions raised against the Phase 4E.1 final
report. Documentation-only pass except two narrow, explicitly-authorized
`resolutionNote`/`implementationNote` appends to
`activation-readiness-risks.json`/`.md` (never closing a TD, per the
established convention). **No runtime behavior was changed.**

## 1. Files inspected

- `PHASE4E_1_CATALOG_PREVIEW_ROUTING_AND_IMMUTABLE_RESOLUTION_SNAPSHOT.md`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/*.cs` (all six files: `GenerationRouteDecision.cs`, `CatalogCandidateEligibilityGate.cs`, `NotEvaluatedReasonClassifier.cs`, `StageEligibilityEvaluator.cs`, `CatalogPreviewSnapshot.cs`, `CatalogPreviewGenerator.cs`)
- `backend/RunningApp.Application/Services/PlanServices.cs` (`GeneratePreviewAsync`, `GenerateCatalogPreviewAsync`, `ConfirmPlanAsync`)
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs`, `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Resolvers/*.cs` (all four resolvers, `RuntimeConditionResolutionResult.cs`, `RuntimeConditionResolutionService.cs`)
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/*.cs` (all six Phase 4E.1 test files)
- `plan-catalog/artifacts/audits/activation-readiness-risks.json` and `.md`
- `PHASE4D_5_RUNTIME_CONDITION_RESOLVER_ORCHESTRATION_SERVICE.md`, `PHASE4D_5_1_PACESOURCE_GOALFEASIBILITY_CROSS_RISK_NOTE.md`
- `PHASE4A_RUNTIME_RESOLVER_DECISION_SET.md`, `PHASE4A_2_RUNTIME_CONDITION_CONFLICT_CLASSIFICATION_AND_RECONCILIATION_PROPOSAL.md`
- `plan-catalog/catalog/{combinations,templates,layouts,level-modifiers,rule-packs}/*` (TEN_K__4D__INTERMEDIATE v10 and its four direct dependencies)
- `git log` for `plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`
- Repository-wide search for "Doc 16", "§5.2", "Decision 3" (see Clarification 1)

## 2. Files changed

- `plan-catalog/artifacts/audits/activation-readiness-risks.json` — added an `implementationNote` to `TD-PACESOURCE-002` (explicitly requested by Clarification 3 if the TD remains OPEN). No `status` field changed; not closed.
- `plan-catalog/artifacts/audits/activation-readiness-risks.md` — mirrored the same note for consistency with the `.json` source.
- This new file.

No backend source, test, or DI code was touched. `dotnet build`/`dotnet test` were re-run only to confirm zero regressions from the documentation-only nature of this pass (see §13).

---

## 3. Clarification 1 — `NotEvaluated` versus `fallbackStageKey`

### 3.0 On "Doc 16 §5.2" and "the original proposed Decision 3"

A repository-wide case-insensitive search for `"Doc 16"`, `"§5.2"`, and
`"Decision 3"` across every `.md`/`.json` file in the repository returned
**zero matches**. No file uses a "Doc N" numbering scheme, and no phase
document contains a numbered "Decision 3." **This citation does not exist in
the repository as evidence — `UNKNOWN_FROM_REPO_EVIDENCE`.** I am not
treating its absence as proof that no such external decision was ever made
outside this repository, only reporting that no repository artifact
substantiates it, so I cannot use it as a governance basis for anything
below.

The closest real precedent is `PHASE4A_2_RUNTIME_CONDITION_CONFLICT_CLASSIFICATION_AND_RECONCILIATION_PROPOSAL.md`
§4 (lines 129–141), which describes the catalog's own pre-existing
`requires`/`fallbackStageKey` stage-authoring mechanism:

> "`UNSUPPORTED` and `NOT_REQUESTED` both fall through to the fallback
> stage... `GOAL_PACE_REHEARSAL`... `"requires": [{"conditionType":
> "GOAL_FEASIBILITY_IN", "allowedValues": ["REALISTIC", "CHALLENGING"]}]`,
> with `fallbackStageKey: CURRENT_FITNESS_SPECIFIC_REHEARSAL`."

Critically, `UNSUPPORTED` and `NOT_REQUESTED` are both **Evaluated**
`GOAL_FEASIBILITY_IN` registry output values (confirmed in
`catalog/registries/runtime-condition-values.v2.json` and in
`GoalFeasibilityResolver.cs`, which returns `RuntimeConditionResolutionResult.Evaluated(...)`
for both), not `RuntimeConditionResolutionStatus.NotEvaluated`. This
pre-existing document — written before Phase 4E.1 — already describes
`fallbackStageKey` as a catalog-authoring gate keyed on an **evaluated
output value that isn't in `allowedValues`**, never on resolver status. It
does not contradict Phase 4E.1's rule; it is consistent with it.

**Conclusion on divergence type**: this is not evidence of options (1)
"intentional governance correction" of a documented prior decision, because
no such prior decision is evidenced in the repository. It is closer to (2)
"required by existing repository semantics" — the catalog's own
`requires`/`fallbackStageKey` schema semantics (evidenced in
`PHASE4A_2...md` and in `ten-k-workout-progression.v5.json`) were never
about resolver status in the first place — reinforced by (3) an explicit,
documented implementation decision made during Phase 4E.1 itself (see
`StageEligibilityEvaluator.cs`'s own doc comments, quoted below), not left
as an unstated assumption. It is **not** (4) accidental: the rule is
enforced by a single `if` check at the top of `StageEligibilityEvaluator.Evaluate`
and is directly unit-tested (see §3.5).

### 3.1 What `NotEvaluated` means in the current model

`RuntimeConditionResolutionResult.cs` defines two factory methods:
`Evaluated(...)` (requires a non-null/non-whitespace `OutputValue`, throws
`ArgumentException` otherwise) and `NotEvaluated(...)` (always sets
`OutputValue = null`). Per the type's own doc comment: "Use only for missing
OPTIONAL evidence/prerequisites — never for invalid request input... and
never as a substitute for a real registry value." `NotEvaluated` is a
structurally distinct status, not a registry value and not itself a failure.

### 3.2 Does the model distinguish the seven categories?

Yes, via `NotEvaluatedReasonClassifier` (`NotEvaluatedReasonCategory` enum,
`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/NotEvaluatedReasonClassifier.cs`),
which maps every reasonCode currently producible by the four resolvers:

| Category | Example reasonCode | Producing resolver |
|---|---|---|
| `NotApplicable` | `NOT_APPLICABLE_NON_RACE_PLAN`, `CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT` | TimeAdequacy, CoreEntryReadiness |
| `UpstreamShortCircuit` | `CORE_ENTRY_READINESS_NOT_EVALUATED`, `TIME_ADEQUACY_NOT_EVALUATED`, `PACE_SOURCE_NOT_EVALUATED` | GoalFeasibility |
| `OptionalInputNotProvided` | *(no reasonCode currently maps here — see §3.3)* | — |
| `RequiredInputNotProvided` | `MISSING_PLAN_TYPE_CONTEXT` | TimeAdequacy |
| `Unsupported` | `TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED`, `PACE_SOURCE_NONE_TARGET_TIME_REQUESTED`, `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`, `PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD` | GoalFeasibility |
| `DependencyUnresolved` | `MISSING_CORE_ENTRY_READINESS_RESULT`, `MISSING_TIME_ADEQUACY_RESULT`, `MISSING_PACE_SOURCE_RESULT` | GoalFeasibility |
| `TechnicalOrConfigurationFailure` | `UNKNOWN_PACE_SOURCE_OUTPUT_VALUE`, and any unrecognized code (fail-loud default) | GoalFeasibility, or any future unmapped code |

This exact mapping is directly unit-tested (16 `[Theory]`/`[Fact]` cases) in
`NotEvaluatedReasonClassifierTests.Classify_KnownReasonCode_ReturnsDocumentedCategory`
and `Classify_UnknownReasonCode_FailsLoudAsTechnicalOrConfigurationFailure`.

### 3.3 Honest limitation, not overclaimed

`OptionalInputNotProvided` has **no reasonCode mapped to it today** — no
resolver currently distinguishes "optional evidence was absent but
generation may still proceed" from any of the other six categories. This is
documented explicitly in `NotEvaluatedReasonClassifier.cs`'s own XML comment
("no reasonCode currently maps here") and in `CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy`'s
comment, which treats a (currently unreachable) `OptionalInputNotProvided`
classification identically to `TechnicalOrConfigurationFailure` — fail loud,
because "no catalog evidence exists yet declaring which inputs are safe to
proceed without." **This is a real, stated technical/design limitation, not
full reason-sensitive governance for every conceivable future NotEvaluated
case** — only for the seven reasonCodes the four resolvers can currently
produce, enumerated exhaustively above.

Separately, `CoreEntryReadinessResolver`'s single reasonCode
`CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT` covers two
distinct upstream causes (Habit-goal "genuinely not applicable" vs.
unknown/null GoalType "can't tell") and both classify as `NotApplicable` —
documented as a "Known limitation" directly in the classifier's own doc
comment, inherited unchanged from Phase 4D.3.1.

### 3.4 What happens to preview for each category — exhaustively

`CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy` (internal,
`backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`)
iterates every one of the four ordered resolver results and switches on
category:

| Category | Preview outcome |
|---|---|
| `NotApplicable` | Continue. The `NotEvaluated` result is still recorded, unmodified, in `CatalogPreviewSnapshot.ResolverResults` — nothing is silently dropped or converted. |
| `UpstreamShortCircuit` | Continue, same as above — the earlier resolver's own `NotEvaluated` decision is trusted, never independently re-decided. |
| `OptionalInputNotProvided` | Currently unreachable (see §3.3); if it ever occurred, throws `PlanPreviewGenerationFailedException`. |
| `RequiredInputNotProvided` | Throws `RuntimeConditionRequiredInputMissingException` — preview generation stops immediately, nothing is persisted. |
| `Unsupported` | Throws `RuntimeConditionUnsupportedException` — same. |
| `DependencyUnresolved` | Throws `RuntimeConditionDependencyUnresolvedException` — same (this indicates an orchestration/wiring defect if it ever fires; `RuntimeConditionResolutionService.ResolveAllResults` always supplies all three dependencies by construction). |
| `TechnicalOrConfigurationFailure` (including unrecognized codes) | Throws `PlanPreviewGenerationFailedException` — same. |

So, to answer directly: preview never "selects another non-fallback path"
and never "selects `fallbackStageKey`" in response to a `NotEvaluated`
result. It either **continues with that `NotEvaluated` result recorded
verbatim** (2 of 7 categories) or **fails immediately with a typed
exception, persisting nothing** (5 of 7 categories, including the
currently-unreachable one). This is directly proven by
`NotEvaluatedReasonClassifierTests.ApplyNotEvaluatedGovernancePolicy_*`
(6 tests exercising every branch) and `CatalogPreviewGeneratorTests.GenerateAsync_RaceGoalMissingRaceDate_ThrowsPlanPreviewGenerationFailed_*`
(one real end-to-end case, via `TimeAdequacyResolver`'s `ArgumentException`
for a Race-goal request missing `RaceDate`, caught by `GenerateAsync` and
re-thrown as `PlanPreviewGenerationFailedException`).

### 3.5 `fallbackStageKey` — the eligibility rule, and an honest wiring caveat

`StageEligibilityEvaluator.Evaluate` (same directory) enforces, exactly:

```csharp
if (conditionResult.Status == RuntimeConditionResolutionStatus.NotEvaluated)
{
    var category = NotEvaluatedReasonClassifier.Classify(conditionResult.ReasonCode);
    return StageEligibilityOutcome.BlockedByNotEvaluated(category, conditionResult.ReasonCode);
}
// Evaluated only, past this point:
if (requirement.AllowedValues.Contains(conditionResult.OutputValue))
    return StageEligibilityOutcome.PrimaryStageEligible(primaryStageKey);
return fallbackStageKey is not null
    ? StageEligibilityOutcome.FallbackStageSelected(fallbackStageKey)
    : StageEligibilityOutcome.NoEligibleStage();
```

`NotEvaluated` **always** returns `BlockedByNotEvaluated` — even when a
`fallbackStageKey` argument is supplied — proven by
`StageEligibilityEvaluatorTests.Evaluate_NotEvaluatedResult_NeverAutoSelectsFallback_EvenWhenFallbackKeyIsSupplied`
and `..._WithNoFallbackKeySupplied_StillBlocksRatherThanNoEligibleStage`.
Only an `Evaluated` result whose value is outside `AllowedValues` may select
`fallbackStageKey`, proven by
`Evaluate_EvaluatedIneligibleValue_WithFallbackKeySupplied_SelectsFallbackStage`.

**Honest caveat, stated rather than glossed over**: `StageEligibilityEvaluator`
is a pure, standalone decision function, unit-tested in isolation. Grep
confirms `CatalogPreviewGenerator.cs` never calls it — it is referenced only
in a doc comment on `CatalogPreviewSnapshot.SelectedStageKeys`/`FallbackStagesUsed`
("Always empty in Phase 4E.1... stage-to-week scheduling remains
unimplemented"). **No live pipeline currently invokes this rule at all** —
there is no stage-to-week scheduling anywhere in the backend yet, in any
phase. This is consistent with (not a regression from) every prior Phase 4D
resolver phase's own explicit boundary, and is exactly the "foundation for
Phase 4E.2" framing the original Phase 4E.1 report used. It should not be
read as "the fallback rule is enforced live" — it is "the fallback rule is
defined and tested, ready to be wired in once stage selection exists."

### 3.6 Public response content per category

For the 5 categories that throw (excluding the 2 that continue), the public
HTTP response is built by `GlobalExceptionHandler.cs`:

- `RuntimeConditionRequiredInputMissingException` → HTTP 400, `errorCode: RUNTIME_CONDITION_REQUIRED_INPUT_MISSING`, `message: exception.Message` (exposed verbatim — **not** masked, since it is not a 500).
- `RuntimeConditionUnsupportedException` → HTTP 422, `RUNTIME_CONDITION_UNSUPPORTED`, message exposed verbatim.
- `RuntimeConditionDependencyUnresolvedException` → HTTP 500, `RUNTIME_CONDITION_DEPENDENCY_UNRESOLVED`, message **masked** to `"An unexpected error occurred."` (per `GlobalExceptionHandler`'s existing rule: any 500 masks the message and logs the real exception server-side only).
- `PlanPreviewGenerationFailedException` → HTTP 500, `PLAN_PREVIEW_GENERATION_FAILED`, message masked, same as above.

**A precise, previously-unstated finding**: for the two non-masked (400/422)
cases, `exception.Message` is built in `ApplyNotEvaluatedGovernancePolicy`
as e.g. `"{result.ConditionType} could not be evaluated because a required
input was not provided (reasonCode={result.ReasonCode})."` — this **does**
expose the internal `ConditionType` (e.g. `"TIME_ADEQUACY_IN"`) and
`reasonCode` (e.g. `"MISSING_PLAN_TYPE_CONTEXT"`) strings to the API
consumer. These are registry condition-type/reason vocabulary, not resolver
class names, catalog file paths, or stack traces, so they do not violate the
letter of "do not expose resolver class names... catalog paths, or stack
traces on public DTOs" (there is no new public *DTO field* — this is
free-text exception message content, and `GeneratePreviewResponse` itself
gained no new fields, confirmed by the existing
`GeneratePreviewResponse_HasNo*Property` reflection tests). It is, however,
a small amount of internal vocabulary leakage worth flagging as a residual
risk for Phase 4E.2 or a future hardening pass — **not fixed in this pass**,
per the "prefer documentation-only" constraint and because it does not
contradict any documented Phase 4E.1 governance rule (which is scoped to
DTO fields and traces, not exception message text).

### 3.7 Does any case silently convert "unknown" into "evaluated and failed"?

No. `NotEvaluated` and `Evaluated` are structurally distinct
(`RuntimeConditionResolutionStatus` enum on the result), and every downstream
consumer (`ApplyNotEvaluatedGovernancePolicy`, `StageEligibilityEvaluator`)
branches on `Status` first, before ever inspecting `OutputValue`. There is
no code path that treats a `null`-`OutputValue` `NotEvaluated` result as
though it were an `Evaluated` result with an ineligible value.

### 3.8 Governance clarification text (added here, per the task's template)

> `fallbackStageKey` is used only when an applicable condition was
> successfully evaluated and its eligibility requirement was not met.
>
> `NotEvaluated` does not itself mean eligibility failure and does not
> automatically trigger fallback. Its behavior is determined by its reason
> and whether the unresolved condition is required for the selected
> generation path.

This is already the enforced Phase 4E.1 rule (§3.4–3.5). **Why treating all
`NotEvaluated` as eligibility failure would be unsafe**: a `NotEvaluated`
result frequently means the condition was never applicable in the first
place (e.g. `TIME_ADEQUACY_IN` for a Habit-goal plan) or that an earlier
resolver already made a considered decision that this resolver is correctly
deferring to (`UpstreamShortCircuit`). Silently routing either case through
`fallbackStageKey` would mean: (a) a plan that never needed a race-readiness
gate at all gets pushed onto a "readiness fallback" stage anyway, producing
a semantically wrong plan for a Habit user; and (b) a resolver would be
independently re-deciding an outcome that a different resolver already
short-circuited, duplicating and potentially contradicting that decision.
Both are exactly the "silently generate an unsafe or semantically incorrect
plan" failure mode the task warns against, which is precisely why
`StageEligibilityEvaluator` blocks unconditionally on `NotEvaluated` instead
of guessing.

---

## 4. Clarification 2 — Complete seven-TD inventory

All seven risks live in the single aggregator file
`plan-catalog/artifacts/audits/activation-readiness-risks.json`
(`auditId: ACTIVATION-READINESS-RISKS-001`, `generatedAtUtc: 2026-07-09T16:00:00Z`).
**This file, and all seven entries in it, predate Phase 4E.1 entirely** —
Phase 4E.1's own work (this session, prior turn) never wrote to this file.
The premise that "the previously known set contained four items" does not
match repository evidence: the file's own `finalStatus` field already
enumerated all seven by name before Phase 4E.1 began. **No new TDs were
created by Phase 4E.1** — the count was never 4, and did not become 7; it
was 7 already.

| # | ID | Title / statement (abridged) | Introduced (pass) | Affected component | Existed before 4E.1? | 4E.1 introduced or merely surfaced? | Status | Blocking classification (this pass's own analysis, not the file's blanket `blocking:false` field, which describes catalog-publish blocking only) |
|---|---|---|---|---|---|---|---|---|
| 1 | `TD-D3-001` | Runtime-condition vocabulary (v1→v2 string) verification required before publish/activate | D3 follow-up (pre-Phase-4A) | Process B/runtime mapping generally | Yes | Neither — unrelated to 4E.1's work; the four resolvers were already built directly against v2 vocabulary in Phase 4D, and 4E.1 added no new registry-validation code (confirmed: no `RuntimeConditionRegistrySnapshot`/`IsValid` call exists anywhere in `PreviewRouting/`) | OPEN | **Non-blocking** to code wiring, internal dry-run, and public preview (resolvers demonstrably already emit only v2-style values, per Phase 4D construction and this pass's own passing tests). **Blocks** formal publish/activation sign-off per its own statement — no explicit "verified" pass has ever been recorded. |
| 2 | `TD-WAVE5-001` | No automated cross-check between `AllowGoalPaceRehearsal` flag and reachable workout-progression stage | Wave 5/D2, revisited D13 | Catalog authoring (`ProgressionModifier`, `WorkoutProgression`) | Yes | Neither — entirely a catalog-authoring-level concern; Phase 4E.1 implements no stage-to-week scheduling at all (`SelectedStageKeys`/`FallbackStagesUsed` always empty) | OPEN (revisited, not closed) | **Non-blocking** to code wiring, dry-run, preview, and confirm — backend does not consume `WorkoutProgression` stages in any phase through 4E.1. **Blocks** only future catalog activation if the flag is ever changed without the recommended validator. |
| 3 | `TD-BACKEND-001` | Backend had zero plan-catalog integration; pilot request silently fell back to 5K/Beginner | Backend/Process B activation review (pre-Phase-4E.1) | `PlanServices`, `PlaceholderPlanGenerationEngine` | Yes | **Surfaced and partially superseded, not introduced.** Its "zero integration" and "silently falls back" claims are now factually stale: Phase 0 already removed silent fallback, and Phase 4E.1 added real routing + resolver invocation for the pilot combination. See finding below. | OPEN | **Non-blocking** to code wiring and internal dry-run (both now exist and are tested). **Blocks** public preview in effect (no real catalog preview is servable, since v10 is DRAFT — see Clarification 4) — same practical blocking outcome as before, reached for a different reason. |
| 4 | `TD-REGISTRY-001` | `CORE_ENTRY_READINESS_IN` golden-fixture `"STANDARD"` value is not a valid registry value | Phase 4A.1 | Golden fixture, registry, `CoreEntryReadinessResolver` | Yes | Neither — `CoreEntryReadinessResolver` was already built (Phase 4D.3.1) to never emit `STANDARD` under any input, confirmed by an existing regression test; Phase 4E.1 added no new code touching this resolver's output vocabulary | OPEN | **Non-blocking** to code wiring, dry-run, or preview — the resolver already avoids the invalid value entirely by construction. Remains open purely as an unresolved fixture/registry documentation inconsistency. |
| 5 | `TD-PACESOURCE-001` | `PACE_SOURCE_IN.ESTIMATED` is registry-valid but never emitted | Phase 4D.2.5 | `PaceSourceResolver` | Yes | Neither — Phase 4E.1 explicitly preserved `PaceSourceResolver`'s existing behavior unchanged (constraint honored, verified: file not modified this session; see §14) | OPEN | **Non-blocking** to code wiring, dry-run, preview, or confirm — `NONE` is a valid `Evaluated` outcome and the resolver functions correctly without `ESTIMATED`. Pure product/UX activation risk. |
| 6 | `TD-PACESOURCE-002` | `AsOfDate` preview/confirm lifecycle reuse-vs-recompute decision undecided | Phase 4D.2.5 | `RuntimeResolverContext.AsOfDate`, confirm lifecycle | Yes | **Partially addressed on the preview side, not introduced.** Phase 4E.1 computes and freezes `AsOfDate` once per preview (see Clarification 3). | OPEN | **Non-blocking** to preview (already solved there). **Blocks confirm** — specifically blocks any future Phase 4E.2 wiring of catalog-confirm until the reuse-vs-recompute decision is made; see Clarification 3 in full. |
| 7 | `TD-CORE-READINESS-001` | `CORE_ENTRY_READINESS_IN` thresholds now approved (4D.3.1) but resolver remained unwired from live generation, untested against real traffic | Phase 4D.3 (resolutionNote 4D.3.1) | `CoreEntryReadinessResolver` | Yes | **Surfaced, not introduced.** Phase 4E.1 now genuinely invokes this resolver in the real (test-proven) preview pipeline for the pilot route — the "remains unwired from live generation" half of its residual risk is now measurably less true. The "untested against real traffic" half is unchanged: no real public request can reach it (v10 is DRAFT). | OPEN | **Non-blocking** to internal dry-run/preview (now genuinely exercised, per `CatalogPreviewGeneratorTests.GenerateAsync_ViaDryRunGate_ProducesFullyEvaluatedSnapshot_SharingOneAsOfDate`). **Blocks** the "real traffic" concern identically to before — public preview is still gated by `PUBLISHED`-only eligibility. Does not block confirm (confirm never touches it). |

**Finding, not corrected in the TD file itself (documentation-only pass, see §14)**: entries 3 and 7
above (`TD-BACKEND-001`, `TD-CORE-READINESS-001`) contain statements that
are now partially stale relative to the current, tested state of the
repository. This report records the precise discrepancy; it does not alter
`activation-readiness-risks.json`'s existing text for either entry, because
the task's explicit instruction to add/verify an implementation note only
applied to `TD-PACESOURCE-002` (Clarification 3). A future pass may choose
to append similar `resolutionNote`s to these two, following the same
never-close convention already used for `TD-CORE-READINESS-001` and now
`TD-PACESOURCE-002`.

---

## 5. Clarification 3 — `TD-PACESOURCE-002`

**Documented closure criteria** (verbatim `requiredResolution` array,
unchanged by this pass):
1. "Decide whether confirm reuses the preview's `AsOfDate` or recomputes it independently."
2. "Wire whichever behavior is chosen into the live preview/confirm flow when `PaceSourceResolver` is eventually connected to generation."
3. "Do not silently default to wall-clock time at confirm without an explicit product decision."

**Completed in Phase 4E.1**: none of the three literally — but the
*preview*-side prerequisite these criteria assume is now in place:
`PlanServices.GenerateCatalogPreviewAsync` computes `AsOfDate =
DateOnly.FromDateTime(DateTime.UtcNow)` exactly once and passes it unchanged
through `RuntimeResolverContext.AsOfDate` and `ResolverInputSnapshot.StartDate`
to every resolver, then freezes it into `CatalogPreviewSnapshot.AsOfDate`
(§7 of the original Phase 4E.1 report; proven by
`CatalogPreviewGeneratorTests.GenerateAsync_ViaDryRunGate_ProducesFullyEvaluatedSnapshot_SharingOneAsOfDate`'s
assertion `Assert.Equal(asOfDate, snapshot.AsOfDate)`).

**Remaining incomplete**: all three criteria, in full — they are all about
**confirm**, and Phase 4E.1 deliberately does not read, validate, or reuse
any `CatalogPreviewSnapshot` from `ConfirmPlanAsync` (confirmed: `ConfirmPlanAsync`
contains no reference to `AsOfDate`, `ICatalogPreviewGenerator`, or
`CatalogPreviewSnapshot` anywhere in `PlanServices.cs`).

**Is confirm snapshot reuse the remaining requirement?** Yes, exactly — the
gap is precisely "does confirm reuse the frozen `AsOfDate` from the stored
snapshot, or recompute a fresh one," which is unanswerable until confirm
reads a snapshot at all.

**Expected to close in Phase 4E.2?** Only if that phase both (a) makes an
explicit, recorded reuse-vs-recompute decision and (b) wires it — closure is
not automatic just because confirm becomes catalog-aware.

**Could it remain open after Phase 4E.2 for governance reasons?** Yes — e.g.
if Phase 4E.2 implements snapshot-reuse mechanically (technically reusing
the stored value) without an explicit product/owner decision being recorded
as evidence (mirroring this repository's own established pattern, e.g.
`EV-008` for `TD-CORE-READINESS-001`), the TD's own required-resolution
item 3 ("do not silently default... without an explicit product decision")
would not yet be satisfiable as *evidenced*, even if the code behaves
correctly by accident.

**Must public catalog confirm remain disabled while it is open?** Not as an
absolute rule for all possible future designs, but under Phase 4E.1's actual
architecture — yes: `PaceSourceResolver`'s recency metadata is only
meaningful if `AsOfDate`'s confirm-time semantics are decided, and Phase
4E.1 confirmed no code anywhere computes or persists a confirm-time
`AsOfDate` today. Enabling catalog confirm before this TD's criteria are met
would mean confirm either silently defaults to wall-clock time (explicitly
forbidden by the TD's own item 3) or silently reuses the preview's frozen
value without a recorded decision — both unacceptable under this
repository's own stated governance.

**TD not closed.** An `implementationNote` was added (see §2 and the diff
in `activation-readiness-risks.json`/`.md`) stating the intended Phase 4E.2
closure path in the exact terms above, per the task's explicit instruction.

---

## 6. Clarification 4 — Current pilot activation reality

**Is `TEN_K__4D__INTERMEDIATE v10` still `DRAFT`?** Yes — confirmed by
direct inspection of `plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`
(`"status": "DRAFT"`), and identically for all four of its directly-loaded
dependencies: `templates/ten-k-master.v6.json`, `layouts/run-layout-4d.v2.json`,
`level-modifiers/intermediate-modifier.v6.json`, `rule-packs/appsel-race-plan.v4.json`
— every one is `"status": "DRAFT"`.

**Has it ever been `PUBLISHED`?** No evidence of this. `git log --follow -p
-- plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json` and
plain `git log --oneline -- <same path>` both return **empty** — the file
has never been committed to this git repository at all (it, and the
overwhelming majority of `plan-catalog/`, sit as untracked working-tree
content across this entire multi-phase session). There is therefore no
possible committed history showing a prior `PUBLISHED` state to point to;
the only status this candidate has ever had, as far as repository evidence
goes, is `DRAFT`.

**Does the current Phase 4E.1 public preview path reject it because of the
`PUBLISHED`-only gate?** Yes — `CatalogCandidateEligibilityGate.LoadForPublicPreviewAsync`
checks `summary.CandidateStatus != "PUBLISHED"` and throws
`CatalogCandidateNotPublishedException` before any dependency check, any
resolver call, or any snapshot construction. Proven directly by
`CatalogCandidateEligibilityGateTests.LoadForPublicPreviewAsync_RealDraftPilotCandidate_ThrowsCatalogCandidateNotPublished`
(asserts `Assert.Contains("DRAFT", ex.Message)` against the real catalog
tree) and `CatalogPreviewGeneratorTests.GenerateAsync_PublicPathAgainstRealDraftCandidate_ThrowsCatalogCandidateNotPublished_NeverProducesASnapshot`.

**Can any real public request currently receive a catalog-generated
preview?** No.

**Is catalog execution currently possible only through tests/internal
dry-run?** Yes — exclusively through `ICatalogCandidateEligibilityGate.LoadForInternalDryRunAsync`,
which bypasses the status check and is called only by test code (the
`DryRunEligibilityGate` test double in `CatalogPreviewGeneratorTests.cs`,
and directly in `CatalogCandidateEligibilityGateTests.LoadForInternalDryRunAsync_RealDraftPilotCandidate_BypassesStatusCheck_LoadsSuccessfully`).
No production code path (`PlanServices`, `Program.cs`'s DI registrations)
ever calls `LoadForInternalDryRunAsync`.

**Does a rejected pilot request ever fall back to SQL?** No. `PlanServices.GenerateCatalogPreviewAsync`
has no `try`/`catch` around `_catalogPreviewGenerator.GenerateAsync(...)` —
every exception propagates unchanged to the caller. Proven directly by
`PlanServicesCatalogRoutingBoundaryTests.GeneratePreviewAsync_PilotCombination_NeverInvokesSqlGenerationEngine`,
which injects a `ThrowIfCalledPlanGenerationEngine` spy (fails the test the
instant `SelectTemplateAsync` is called) into `PlanServices` and asserts
`CatalogCandidateNotPublishedException` is thrown with `spyEngine.WasCalled
== false` and `context.PlanPreviews` empty.

**What exact typed error is returned for the draft candidate?**
`RunningApp.Application.Exceptions.CatalogCandidateNotPublishedException`,
mapped by `GlobalExceptionHandler` to HTTP 409 with `errorCode:
CATALOG_CANDIDATE_NOT_PUBLISHED`, message exposed verbatim (e.g. "Catalog
candidate TEN_K__4D__INTERMEDIATE v10 has status 'DRAFT', not 'PUBLISHED'.
It is not eligible for public preview.").

**Confirm or refute the expected safe state**:

```text
routing and snapshot infrastructure exists,
but no real public catalog preview is currently serviceable,
and the request does not fall back to SQL.
```

**Confirmed, in full, with the test evidence cited above.**

---

## 7. Tests run and results

`dotnet build RunningApp.sln -c Debug` and `dotnet test RunningApp.sln -c
Debug` were re-run after this pass's two `implementationNote` edits (which
touch only JSON/Markdown files outside the backend project tree, so no
recompilation of backend code was actually triggered by them).

**Result: 367 passed, 0 failed — identical to the count at the end of Phase
4E.1.** No test was added, removed, or modified in this pass; no test
assertion changed. This is expected and correct for a documentation-only
governance pass.

## 8. Whether any runtime behavior changed

**No.** Zero `.cs` files were touched in this pass. The only file changes
are the two `activation-readiness-risks.*` `implementationNote` additions
(data/documentation, read by nothing at runtime) and this new report. Every
behavior described in this report — the `NotEvaluated`/`fallbackStageKey`
rule, the seven-category classification, the `PUBLISHED`-only gate, the
no-SQL-fallback guarantee — is the exact Phase 4E.1 behavior verified by the
existing, unmodified 41 Phase 4E.1 tests plus the 326 pre-existing tests.

## 9. Final acceptance recommendation

```text
PHASE4E_1_ACCEPTABLE_AS_IMPLEMENTED
```

All four acceptance questions resolve favorably against repository
evidence: the `NotEvaluated`/`fallbackStageKey` divergence is consistent
with (not a violation of) the catalog's own pre-existing stage-authoring
semantics and is deliberately, correctly implemented and tested, with one
honestly-disclosed limitation (`StageEligibilityEvaluator` not yet wired
into any live pipeline, because no pipeline needing it exists yet); all
seven TDs predate Phase 4E.1 and were accurately reported as OPEN, with two
(`TD-BACKEND-001`, `TD-CORE-READINESS-001`) now carrying minor,
disclosed-but-unedited staleness in their prose; `TD-PACESOURCE-002`
correctly remains OPEN with its Phase 4E.2 closure path now explicitly
documented; and `TEN_K__4D__INTERMEDIATE v10` is confirmed still `DRAFT`,
never `PUBLISHED`, with the `PUBLISHED`-only gate and no-SQL-fallback
guarantee both proven by existing tests. No runtime correction is required.

```text
PHASE4E_1_GOVERNANCE_CLARIFIED_AND_ACCEPTANCE_READY
```
