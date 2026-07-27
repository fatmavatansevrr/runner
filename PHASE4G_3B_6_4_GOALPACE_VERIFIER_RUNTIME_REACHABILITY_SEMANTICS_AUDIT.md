# Phase 4G.3B.6.4 — GoalPace Verifier Runtime-Reachability Semantics Audit

**Read-only architectural semantics audit. No verifier, orchestrator, registry,
resolver, or governance-policy code changed. No TD updated. Nothing
implemented. Recommendation only.**

---

## 1. Method-group fix confirmation (pre-check)

**Result: PASS.**

1. Commit located: `276635f test(catalog): enforce two-layer dark verifier
   reachability` (current `HEAD`), preceded by `b32a9f5 feat(catalog): add
   goal-pace reachability verifier`.
2. Current pattern, quoted directly from
   `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DarkReachabilityAssertions.cs:87-88`:
   ```csharp
   private static Regex MemberAccessPattern(string verifierTypeName) =>
       new($@"\b{Regex.Escape(verifierTypeName)}\s*\.\s*Verify\b", RegexOptions.Compiled);
   ```
   No trailing `(` is required — this is a member-access pattern, not an
   invocation pattern, confirmed by the class's own doc comment: *"DOES
   detect... a bare method-group reference used as a value... these do not
   require an invoking `(` immediately after `Verify` and were NOT detected by
   this helper's original, narrower invocation-only pattern."*
3. Tests confirmed present and passing:
   `InvocationScanner_DetectsMethodGroupAssignment`,
   `InvocationScanner_DetectsMethodGroupAsArgument`,
   `InvocationScanner_DetectsMethodGroupAssignedToExplicitDelegateType`,
   `InvocationScanner_DoesNotFalselyMatchADifferentlyNamedMemberSharingThePrefix`
   (`VerifyOrDefault` false-positive guard) — all in
   `DarkReachabilityAssertionsTests.cs`.
4. Re-run in this pass: **`DarkReachabilityAssertionsTests` — 14/14 passed.**
   No blocker. Proceeding with the audit.

---

## 2. Complete NotEvaluated reason-code reachability table

Enumerated from direct re-inspection of `NotEvaluatedReasonClassifier.cs`
(the complete, exhaustive `ReasonCodeToCategory` dictionary — 14 entries, no
reason code exists outside this table; an unrecognized code classifies
`TechnicalOrConfigurationFailure` by the classifier's own fallback) and
`ApplyNotEvaluatedGovernancePolicy`'s switch (`CatalogPreviewGenerator.cs:376-417`).

| Owning resolver | Reason code | Governance classification | Mechanically reaches allocator if it fired? (classification-only) | Structurally reachable in production for the Race/catalog pilot? | Why (production reachability) |
|---|---|---|---|---|---|
| TimeAdequacyResolver | `NOT_APPLICABLE_NON_RACE_PLAN` | `NotApplicable` → `continue` | **Yes** | **No** | Only fires for Habit/unknown `GoalType`; `V1CatalogPilotIdentityPolicy` requires `GoalType=Race` to reach the catalog pipeline at all. |
| TimeAdequacyResolver | `MISSING_PLAN_TYPE_CONTEXT` | `RequiredInputNotProvided` → throws `RuntimeConditionRequiredInputMissingException` (400) | No (throws) | No | Same non-Race/missing-goal-type precondition as above. |
| CoreEntryReadinessResolver | `CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT` | `NotApplicable` → `continue` | **Yes** | **No** | Only fires for Habit/unknown `GoalType`; same catalog-identity gate. |
| GoalFeasibilityResolver | `MISSING_CORE_ENTRY_READINESS_RESULT` | `DependencyUnresolved` → throws `RuntimeConditionDependencyUnresolvedException` (500) | No (throws) | No | Would require `RuntimeConditionResolutionService.ResolveAllResults` to omit a dependency — an orchestration wiring defect, not a real input state; never observed. |
| GoalFeasibilityResolver | `CORE_ENTRY_READINESS_NOT_EVALUATED` | `UpstreamShortCircuit` → `continue` | **Yes** | **No** | Only fires when `CORE_ENTRY_READINESS_IN` is itself `NotEvaluated`, which (per the row above) only happens for non-Race — unreachable on the catalog Race-only path. |
| GoalFeasibilityResolver | `MISSING_TIME_ADEQUACY_RESULT` | `DependencyUnresolved` → throws (500) | No (throws) | No | Same orchestration-wiring-defect-only precondition as `MISSING_CORE_ENTRY_READINESS_RESULT`. |
| GoalFeasibilityResolver | `TIME_ADEQUACY_NOT_EVALUATED` | `UpstreamShortCircuit` → `continue` | **Yes** | **No** | Only fires when `TIME_ADEQUACY_IN` is itself `NotEvaluated` — non-Race only, unreachable. |
| GoalFeasibilityResolver | `TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED` | `Unsupported` → throws `RuntimeConditionUnsupportedException` (422) | No (throws) | No | Requires `availableWeeks < catalog MinimumWeeks` (8); `RaceHorizonPolicy` rejects any such request upstream, before the catalog resolver pipeline is ever reached (confirmed Phase 4G.3B.6). |
| GoalFeasibilityResolver | `MISSING_PACE_SOURCE_RESULT` | `DependencyUnresolved` → throws (500) | No (throws) | No | Same orchestration-wiring-defect-only precondition. |
| GoalFeasibilityResolver | `PACE_SOURCE_NOT_EVALUATED` | `UpstreamShortCircuit` → `continue` | **Yes** | **No** | Only fires when `PACE_SOURCE_IN` is itself `NotEvaluated` — but `PaceSourceResolver` has zero `NotEvaluated` call sites in its entire source file (confirmed Phase 4G.3B.6); structurally impossible today. |
| GoalFeasibilityResolver | `PACE_SOURCE_NONE_TARGET_TIME_REQUESTED` | `Unsupported` → throws (422) | No (throws) | No | Requires `PACE_SOURCE_IN=NONE` while a target time is present; `PaceSourceResolver` always emits `TARGET_TIME` (not `NONE`) whenever `TargetFinishTimeSeconds>0` (confirmed, and independently flagged as unreachable by `TD-PACESOURCE-001`'s own implementation note). |
| GoalFeasibilityResolver | **`PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`** | `Unsupported` → throws (422) | No (throws) | **Yes** | Fires whenever `TargetFinishTimeSource=UserDefined`/unset and no complete `RecentRace` — a real, plausible, currently-live 12-week-pilot input (confirmed Phase 4G.3B.6.1's real HTTP test). **This is the only reason code, across all four resolvers, confirmed structurally reachable in production for the current catalog pilot.** |
| GoalFeasibilityResolver | `PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD` | `Unsupported` → throws (422) | No (throws) | No | `PaceSourceResolver` never emits `ESTIMATED` in V1 (`TD-PACESOURCE-001`). |
| GoalFeasibilityResolver | `UNKNOWN_PACE_SOURCE_OUTPUT_VALUE` | `TechnicalOrConfigurationFailure` → throws `PlanPreviewGenerationFailedException` (500) | No (throws) | No | Defensive catch-all for an output value outside `PaceSourceResolver`'s own defined set — impossible by construction given the resolver's own exhaustive switch. |

---

## 3. Does any reason code actually reach allocator fallback in production?

**No.** The table above splits cleanly into two disjoint groups:

- **Mechanically permitted to reach the allocator** (classified
  `NotApplicable` or `UpstreamShortCircuit`, so `ApplyNotEvaluatedGovernancePolicy`
  `continue`s past them): `NOT_APPLICABLE_NON_RACE_PLAN`,
  `CORE_ENTRY_READINESS_NOT_APPLICABLE_OR_INSUFFICIENT_CONTEXT`,
  `CORE_ENTRY_READINESS_NOT_EVALUATED`, `TIME_ADEQUACY_NOT_EVALUATED`,
  `PACE_SOURCE_NOT_EVALUATED` — **every single one of these requires a
  non-Race `GoalType` (or an already-disproven `PaceSourceResolver`
  `NotEvaluated` state) to fire at all**, and `V1CatalogPilotIdentityPolicy`
  guarantees only `GoalType=Race` requests ever reach the catalog pipeline.
  **None of these five is structurally reachable in production today.**
- **Structurally reachable in production today**: exactly one —
  `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE` — and it is classified
  `Unsupported`, which throws before the allocator ever runs.

**The intersection of "mechanically reaches the allocator" and "structurally
reachable in production" is empty.** There is currently no real production
input, across any of the four resolvers, that both survives governance and
reaches `ProgressionStageAllocator`'s `FallbackStageKey` routing for
`GOAL_FEASIBILITY_IN`/`GOAL_PACE_REHEARSAL`. This directly answers the
Required Investigation's step 2: no, there is no real NotEvaluated reason
code today that reaches the allocator fallback uncontested — the earlier
hypothesis that such a case might still exist ("this would mean the
verifier's synthetic check is not entirely redundant, only partially so") is
**not supported by the evidence**.

This finding is contingent on three specific, currently-true upstream
guarantees, named explicitly rather than assumed permanent (per
`ARCHITECTURAL_CLAIM_VERIFICATION_GOVERNANCE.md`'s "claims about scope must
be source-verified" principle): (1) `V1CatalogPilotIdentityPolicy` restricts
catalog routing to `GoalType=Race`; (2) `PaceSourceResolver` never emits
`NotEvaluated`, `NONE`-with-target-time-present, or `ESTIMATED`; (3)
`RaceHorizonPolicy` rejects below-minimum horizons before the resolver
pipeline runs. If any one of these three changes in a future phase, this
finding must be re-verified, not assumed to still hold.

---

## 4. Original verifier intent (quoted from source)

`GoalPaceReachabilityVerifier.cs:49-56` (added Phase 4G.3B.3, commit `b32a9f5`):

> *"Phase 4G.3B.3, verifier 5 of 9. Narrower than StageReachabilityVerifier: it
> does not re-prove stage placement (already confirmed there) -- it answers,
> for every value GOAL_FEASIBILITY_IN can actually take (per the real
> runtime-condition-values registry, supplied by the caller) plus the
> distinct NotEvaluated resolver status, whether GOAL_PACE_REHEARSAL's
> eligibility/fallback decision is confirmed correct and complete, including
> the exact TD-NOTEVALUATED-FALLBACK-001 gap."*

And the outcome-status doc comments (`GoalPaceReachabilityVerifier.cs:14, 30, 33`):

> *"UncertainNotEvaluated: NotEvaluated -- routes to fallback via the
> mechanism TD-NOTEVALUATED-FALLBACK-001 already flagged as not yet
> product-approved."*
> *"Pass: Every registered value maps to Eligible or FallbackConfirmed -- no
> gaps, no open risk. Not achievable today (TD-NOTEVALUATED-FALLBACK-001 is
> still open)."*
> *"PassWithOpenRisk: Every value resolves structurally, but at least one
> UncertainNotEvaluated case exists -- the expected, honest result today."*

**Determination: the original intent was explicitly "theoretical
catalog-contract completeness under every registry-registered value plus the
generic NotEvaluated case," not "prove runtime-governance-reachable safety
for a specific request."** The author deliberately built this to surface
`TD-NOTEVALUATED-FALLBACK-001`'s ambiguity as an *open, honestly-reported
risk* — the doc comments show clear awareness that `Pass` was "not achievable
today" precisely *because* that TD was open, and explicitly designed
`PassWithOpenRisk` as "the expected, honest result." The verifier was never
claimed to model runtime governance interception at all — `ApplyNotEvaluatedGovernancePolicy`
and `NotEvaluatedReasonClassifier` did not exist as governance concepts this
verifier's design ever reasoned about; it evaluates the allocator's own
logic in isolation, exactly as `StageReachabilityVerifier`'s own precedent
does. This is a correct characterization of what the verifier says about
itself, not a criticism of a flawed design — it did what it was built to do.

---

## 5. Option A/B/C evaluation

Each evaluated independently against the five required criteria.

### OPTION A — Verifier remains intentionally universal/theoretical

- **Correctness/honesty of resulting registry semantics:** Correct and
  honest *as a statement of catalog-contract completeness*, but the registry
  currently does not label it that way — `RaceCoreSupportRegistry`'s
  `DecisionRequired` status is not accompanied by a documented distinction
  between "theoretically incomplete" and "runtime-unsafe." Under Option A,
  this ambiguity would need to be resolved by **documentation only** (a
  reframing of what the registry's `DecisionRequired` label means), not by
  code change — matching this option's own stated approach.
- **Implementation cost:** Zero code cost (no code change at all — purely a
  documentation/labeling clarification of existing behavior).
- **Silent-under-report risk:** **None** — this is the maximally conservative
  option; it can never under-report, by construction, since it already
  reports every theoretically constructable NotEvaluated case regardless of
  runtime reachability.
- **Consistency with `ARCHITECTURAL_CLAIM_VERIFICATION_GOVERNANCE.md`:**
  Fully consistent — makes no claim about runtime reachability at all, so
  there is nothing to source-verify beyond what section 2-3 already
  established.
- **Touches other verifiers/orchestrator/registry?** No — no code change of
  any kind.

### OPTION B — Verifier should only evaluate runtime-reachable paths

- **Correctness/honesty of resulting registry semantics:** Would make the
  registry's `DecisionRequired`/`Pass` distinction track *actual runtime
  risk* rather than theoretical completeness — arguably more useful for a
  registry whose stated purpose (Phase 4G.3B.5) is informing real activation
  decisions. However, per section 3's contingency note, its correctness
  going forward depends on the three named upstream guarantees continuing to
  hold — a **conditional** correctness, not an unconditional one.
- **Implementation cost:** Moderate — requires: (1) a new, source-verified
  classification of each `NotEvaluatedReasonCategory` (or specific reason
  code) into `RejectedBeforeScheduling` vs. `StageReachableFallback`
  (essentially productionizing this audit's section 2 table into code); (2)
  new verifier logic to consult that classification instead of unconditionally
  constructing every registered value; (3) a regression-safety mechanism
  (e.g. a test asserting the three upstream guarantees in section 3 still
  hold) so a future change to `PaceSourceResolver`/`V1CatalogPilotIdentityPolicy`/`RaceHorizonPolicy`
  cannot silently invalidate the narrowed check without failing a test first
  — this safety mechanism is not optional under this option, it is what
  prevents it from becoming criterion-3's disqualifying risk.
- **Silent-under-report risk:** **Real, but mitigable, not eliminable by
  design alone.** If a future change makes one of the five
  currently-"mechanically-permitted-but-production-unreachable" reason codes
  actually reachable (e.g. a future Habit-goal catalog route, or a
  `PaceSourceResolver` priority-order change per `TD-PACESOURCE-001`'s own
  cross-risk note), and the narrowed verifier is not updated in lockstep, it
  would silently stop flagging a newly-real risk. This is the single most
  important finding against adopting Option B *without* the regression-safety
  mechanism described above as a mandatory companion, not an optional
  enhancement.
- **Consistency with governance:** Requires the new classification to be
  itself source-verified per file (exactly as this audit did), not assumed —
  consistent with the governance doc's principle **if** implemented that way;
  inconsistent if implemented as an unverified assumption.
- **Touches other verifiers/orchestrator/registry?** No new touch to
  `AllocationOrderCorrectnessVerifier` or the other seven verifiers required
  (confirmed: `AllocationOrderCorrectnessVerifier.cs` contains zero reference
  to `GOAL_FEASIBILITY`/`NotEvaluated`). `RaceCoreSupportRegistry` itself
  needs no code change — it already just consumes `SafetyVerificationOrchestrator`'s
  `OverallOutcome`, which would change automatically once
  `GoalPaceReachabilityVerifier`'s own outcome changes.

### OPTION C — Two separate, explicitly named results

- **Correctness/honesty of resulting registry semantics:** **Most honest of
  the three** — makes both the theoretical-completeness claim (today's
  existing check, unchanged, still valuable for catalog-contract governance)
  and the runtime-reachable-safety claim visible and separately labeled,
  with neither silently overriding or hiding the other. `RaceCoreSupportRegistry`
  (or a future consumer) explicitly chooses which one drives activation
  decisions, rather than that choice being implicit in a single merged
  result.
- **Implementation cost:** **Highest of the three** — requires everything
  Option B requires (the classification, the new logic) *plus* a second
  parallel result type/field, plus a `RaceCoreSupportRegistry` consumption
  decision (which of the two, or both, drives `DecisionRequired`) that itself
  needs an explicit product/engineering decision to avoid silently picking
  one by default.
- **Silent-under-report risk:** **Lowest of the three that involve any code
  change** — because `UniversalCatalogCompleteness` (today's check) remains
  fully intact and unchanged alongside the new narrower check, nothing that
  Option A already reports can be lost; the new `RuntimeReachableMechanicalSafety`
  result is purely additive. The same regression-safety caveat from Option B
  still applies to the *narrower* result specifically, but it no longer risks
  silently replacing the conservative signal — only the registry's own
  *consumption choice* of which result to act on carries residual risk, and
  that choice is now explicit rather than implicit.
- **Consistency with governance:** Fully consistent, same reasoning as Option
  B, with the added benefit that the "claims about scope must be
  source-verified" principle is satisfied *twice over* — once per result
  type, each independently checkable against its own stated scope.
- **Touches other verifiers/orchestrator/registry?** Same as Option B for
  the other eight verifiers (no touch required). `SafetyVerificationOrchestrator`
  and `RaceCoreSupportRegistry` **would** need a real (if small) code change
  under Option C — to carry and expose the second result — unlike Option B,
  where only `GoalPaceReachabilityVerifier`'s own internal outcome changes
  and everything downstream is unaffected by construction.

### Comparison table

| | A | B | C |
|---|---|---|---|
| Correctness/honesty | Correct as stated, but under-labeled today | Correct, but conditional on 3 named invariants | Most honest — both claims visible, neither hidden |
| Implementation cost | Zero (docs only) | Moderate | Highest |
| Silent-under-report risk | None (max conservative) | Real, mitigable only with a mandatory regression-safety companion | Lowest of the code-changing options |
| Governance-doc consistency | Trivially consistent | Consistent if implemented with source-verified classification | Consistent, doubly so |
| Touches other 8 verifiers? | No | No | No (but orchestrator/registry do change) |

---

## 6. Recommendation (NOT YET APPLIED)

**Recommendation: a hybrid of Option A (immediate, zero-cost) and Option C
(future, if/when product wants the registry to drive real activation
decisions), explicitly rejecting adopting Option B alone without its
mandatory regression-safety companion.**

Reasoning:

- **Immediately, Option A requires no code change and carries zero
  under-report risk** — the correct next step *today* is a documentation-only
  clarification (in `RaceCoreSupportRegistry`'s own doc comments and/or
  `TD-NOTEVALUATED-FALLBACK-001`'s text, in a **future, separate pass** — not
  this one) that the registry's current `DecisionRequired` for
  `GoalPaceReachability` means "theoretical catalog-contract completeness is
  not proven," not "a real, currently-reachable runtime risk exists." Section
  3's finding (zero intersection between allocator-reachable and
  production-reachable) is exactly the fact that makes this reframing
  accurate rather than misleading.
- **Option B alone is explicitly not recommended** — the required safety
  criterion ("must never be chosen if it risks masking a genuinely reachable
  danger") is not satisfied by Option B's plain form; it is only satisfied by
  Option B *plus* a mandatory, permanent regression test asserting the three
  named upstream invariants (§3) continue to hold, at which point it is
  functionally indistinguishable from doing Option C's classification work
  without exposing the second result — i.e., Option B done safely already
  requires most of Option C's engineering investment.
- **Option C is the correct target if/when the registry's consumers need to
  act on real runtime risk specifically** — it is more expensive, but it is
  the only option that adds real narrowed information *without* deleting or
  silently superseding the existing, already-useful, zero-risk conservative
  signal. Given no current consumer of `RaceCoreSupportRegistry` beyond this
  audit and Phase 4G.3B.5's own test suite was found to require the narrower
  signal today, this is correctly scoped as **future work**, not immediate
  work.
- The evidence does **not** point to a single clean "pick one and you're
  done" answer, and this audit does not force one: it recommends a staged
  path (A now, C later if warranted) rather than B in isolation, and states
  this explicitly rather than defaulting to whichever option was presented
  first or most elaborately in the prompt.

---

## 7. What a future implementation pass would need to change (scoped, not implemented)

**For the immediate Option-A documentation step:** no code change; a future
pass would edit `RaceCoreSupportRegistry`'s own doc comments and/or
`PHASE4G_3B_5_SUPPORT_REGISTRY.md` to state the theoretical-vs-runtime
distinction explicitly, and (separately, per this pass's own scope
restriction) `TD-NOTEVALUATED-FALLBACK-001`'s text in a subsequent
documentation pass.

**For a future Option-C pass**, in dependency order:
1. Add an internal classification (e.g. a new enum or a lookup consulted by
   the verifier) mapping each `NotEvaluatedReasonCategory`/reason code to
   `RejectedBeforeScheduling` vs. `StageReachableFallback`, mirroring this
   audit's section 2 table — sourced from `NotEvaluatedReasonClassifier` and
   `ApplyNotEvaluatedGovernancePolicy`'s actual switch, not hand-copied from
   this document without re-verification at implementation time.
2. Add a companion regression test asserting the three named upstream
   invariants (§3) still hold, so a future resolver/routing change cannot
   silently invalidate the classification without failing a test.
3. Extend `GoalPaceReachabilityVerificationResult` (or a sibling type) to
   carry a second, explicitly-named `RuntimeReachableMechanicalSafety`
   outcome alongside the existing `OverallOutcome` (renamed/clarified as
   `UniversalCatalogCompleteness` if desired), computed by re-running the
   same check but skipping/passing any outcome classified
   `RejectedBeforeScheduling`.
4. Update `SafetyVerificationOrchestrator`'s per-verifier summary to carry
   both results through unchanged (no aggregation-rule change to the other
   eight verifiers).
5. Make an explicit, recorded product/engineering decision on which result
   `RaceCoreSupportRegistry`'s own `DecisionRequired` classification consumes
   (today's `UniversalCatalogCompleteness`, the new
   `RuntimeReachableMechanicalSafety`, or both, with an explicit combination
   rule) — this decision must be made and recorded, not defaulted silently.
6. Update `PHASE4G_3B_5_SUPPORT_REGISTRY.md` and
   `TD-NOTEVALUATED-FALLBACK-001` to reflect whichever result the registry
   now consumes, in a subsequent, separate documentation pass.

None of the above was implemented in this pass.

---

## 8. Would this change the real 12-week registry result, and under what condition?

**Yes, conditionally — and this audit computed the exact condition using the
real, already-recorded 9-verifier composite table**
(`PHASE4G_3B_4B_SAFETY_VERIFICATION_ORCHESTRATOR.md`, the per-week-count
table): for every one of the 7 real feasible targets (8-14 weeks),
**`GoalPaceReachability` is the *only* one of the 9 verifiers that is not
`Pass`** — `PhaseConstraint`, `RaceSpecificCapacity`, `StageReachability`,
`WorkoutExposure`, `ReadinessEligibility`, `VolumeProgression`,
`LongRunProgression`, and `RaceDateAlignment` are all `Pass` at every target,
including 12 weeks.

**Condition:** if a future Option-B/C implementation causes
`GoalPaceReachabilityVerifier`'s consumed outcome to become `Pass` (which
section 3's finding shows would be the correct, honest result under a
runtime-reachability-scoped check, since no `StageReachableFallback` case
exists for the current catalog data), then:

- `SafetyVerificationOrchestrator.OverallOutcome` would become `Pass` for
  **all 7 targets** (8-14), since `GoalPaceReachability` was the sole
  non-`Pass` contributor at every one of them.
- Combined with `RaceCoreSupportRegistry`'s existing, unchanged
  `AllocationOrderCorrectnessVerifier` gate (per Phase 4G.3B.5's own real
  registry table: `Pass` for 8, 12, 14; `DecisionRequired` for 9, 10, 11, 13
  — driven by `TD-ALLOCATION-PRIORITY-001`, independent of this audit's
  topic), the resulting registry status would become:
  - **Weeks 8, 12, 14: `MechanicallyPassed`** (both gates now `Pass`) — **this
    is the currently-live 12-week pilot.**
  - **Weeks 9, 10, 11, 13: still `DecisionRequired`** (blocked independently
    by `TD-ALLOCATION-PRIORITY-001`'s still-open allocation-order gate,
    unaffected by anything in this audit).

This is reported as a factual consequence of the option, not a
recommendation to pursue it purely to change this number — see section 6:
this audit recommends the immediate step be documentation-only (Option A),
which does **not** change any registry status at all; only a future,
separately-decided Option C implementation would produce the `MechanicallyPassed`
outcome described above for 8/12/14 weeks.

---

## 9. Open questions for product/engineering sign-off before implementing any option

1. Does product/engineering want `RaceCoreSupportRegistry`'s `DecisionRequired`
   to mean "theoretically unproven" (today's actual meaning, Option A) or
   "runtime-reachable risk" (Option B/C's narrower meaning) — or is a
   `MechanicallyPassed` result for the live 12-week pilot (the consequence in
   §8) something that should trigger a *separate* activation-approval
   conversation before it is ever allowed to appear, regardless of how
   technically accurate it is?
2. If Option C is eventually pursued, who owns the decision of which of the
   two results (`UniversalCatalogCompleteness` vs.
   `RuntimeReachableMechanicalSafety`) — or what combination — drives the
   registry's actual `DecisionRequired` classification?
3. Is the regression-safety companion test (§7, step 2) considered a
   mandatory prerequisite for Option B/C, or would engineering accept the
   residual silent-under-report risk this audit flags without it? (This
   audit's own position: mandatory, not optional.)
4. Should this audit's conclusion be folded into
   `TD-NOTEVALUATED-FALLBACK-001`'s own text in a future pass (as Phase
   4G.3B.6.2/4G.3B.6.3 have each already done for their own findings), and if
   so, does it change or merely add detail to that TD's already-recorded
   "Open decision 3" (whether any NotEvaluated reason that DOES reach the
   allocator uncontested should keep silently falling back)? This audit's
   finding — that no such reason code currently exists — is directly relevant
   evidence for that still-open question, but does not resolve it (a reason
   code reaching the allocator remains theoretically possible for a future
   catalog/resolver change, per §3's contingency note).
