# Architectural Claim Verification Governance

## A. Purpose

This document governs claims made in this repository about architectural and behavioral
properties of code — properties such as "generic," "horizon-agnostic," "dark," "unwired,"
"pure," "fail-closed," "production-independent," and "backward-compatible." These claims are
easy to state and easy to repeat across phases, but they are not directly observable from a
single read of the code that motivated them, and they are trivially easy to get wrong by
generalizing from one exercised case (usually the current default/active one) to all cases.
This document exists because exactly that happened once already in this repository — see
section F.

## B. Core rule

Architectural properties such as generic, horizon-agnostic, dark, unwired, pure, fail-closed,
production-independent, and backward-compatible **must be demonstrated through direct source
inspection, call-site or reachability evidence, and executable tests where practical.**

They **must not** be inherited solely from prior reports, phase documents, review summaries, or
narrative descriptions — including this repository's own prior phase reports.

## C. Evidence requirements

1. **A prior report proves what was concluded, not that the conclusion is correct.** Citing an
   earlier phase document as evidence for an architectural claim is citing a claim, not a proof.
   Re-derive it from source before relying on it.

2. **Claims about absence of behavior require negative evidence**, such as:
   - a production call-site search;
   - a symbol/reference search;
   - dependency inspection;
   - DI-registration inspection;
   - structural or reachability tests.

3. **Claims about genericity require exercising the implementation outside the currently
   active/default case.** Reading the code and asserting "this looks parameterized" is not
   sufficient — run it with a different input and observe the result.

4. **Claims about horizon independence or other parameter independence require testing at
   least two distinct parameter values, including one non-default value.** A property proven
   only at the one value that has ever actually executed is not proven independent of that
   value — it is merely unfalsified by the only test that has ever been run.

5. **Claims about purity require checking for:**
   - file access;
   - database access;
   - clock access;
   - environment access;
   - DI/service resolution;
   - mutable shared state.

6. **Claims that a component is dark or unwired require both:**
   - zero production call sites; **and**
   - no indirect activation through DI, reflection, registration, configuration scanning, or
     startup composition.

   Either alone is insufficient: a component can have zero direct call sites and still be
   activated indirectly, and a component can be registered in DI without ever actually being
   resolved on any live path.

7. **Claims of fail-closed behavior require executable negative cases, not only success-path
   inspection.** A guard that has never been exercised against the condition it is supposed to
   reject has not been shown to fail closed — it has been shown to exist.

8. **Claims of backward compatibility require comparison against a recorded baseline or a
   semantic-equivalence test.** "This should behave the same" is a hypothesis, not a
   verification.

## D. Disproven-claim procedure

When a prior claim is disproven:

1. Preserve the original historical record. Do not delete it.
2. Add an explicit correction or addendum, placed close enough to the original statement that
   a future reader encountering the original text cannot reasonably miss the correction.
3. Do not silently rewrite the earlier conclusion to make it appear as though it was always
   accurate.
4. Create or update a governance risk record (e.g. a `TD-*` entry in
   `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`) when the disproven
   claim has activation or production impact.
5. Cross-reference the correction, the risk record, the source evidence, and any tests that
   disproved the claim, from each other.
6. Take whatever steps are practical to prevent the disproven statement from being reused as
   activation evidence in a future pass — at minimum, the correction and the risk record
   together must make the current, accurate state discoverable from the original claim.

## E. Required reporting vocabulary

Future reports evaluating an architectural or behavioral claim should distinguish, per claim:

- `VERIFIED_FROM_SOURCE` — confirmed by direct reading of the relevant source, with file/method/
  symbol references.
- `VERIFIED_BY_REACHABILITY` — confirmed by a call-site, symbol-reference, or DI-registration
  search showing the property holds (e.g. "no production call site").
- `VERIFIED_BY_EXECUTABLE_TEST` — confirmed by a test that actually exercises the property,
  including a non-default case where the claim is parametric.
- `INFERRED` — reasoned from related evidence but not directly observed; must state what was
  actually checked and what gap remains.
- `INHERITED_UNVERIFIED` — restated from a prior report without independent re-verification in
  this pass. Reports must not present an `INHERITED_UNVERIFIED` claim with the same confidence
  as a verified one.
- `DISPROVEN` — a claim that direct inspection or testing showed to be false or incomplete.
  Requires the section D procedure.
- `DECISION_REQUIRED` — the property depends on a product/engineering decision that has not
  been made; do not resolve it by assumption.
- `NOT_EVALUATED` — explicitly out of scope for the current pass; state this rather than
  omitting the claim silently.

These labels are reporting/governance terminology for this document's own purpose only, unless
an existing runtime contract already uses the same terms for its own, separate purpose (in
which case that runtime contract's meaning governs runtime code, and this document's meaning
governs reports and documentation only — the two must not be conflated).

## F. First illustrative precedent — the race-date alignment guard

This is the finding that motivated this document, used here as a worked example of the
procedure in section D.

- **Prior claim**: `PHASE4G_3A_EIGHT_WEEK_CORE_ALLOCATION_AUDIT.md` section 15 characterized the
  complete live `CatalogRaceDateAlignmentInvalidException` guard as "horizon-agnostic" and
  "ready for Phase 4G.3B as-is... no code change needed."
- **Direct source finding**: reading `CatalogPreviewGenerator.cs`'s `BuildDarkInternalDatedSkeleton`
  method directly showed the live guard actually combines an exact-12-week check
  (`datedSkeleton.Weeks.Count != RaceHorizonPolicy.ExactStandaloneCoreSupportedWeeks`) with the
  date-tolerance check, joined by `OR` in one condition.
  Classification: `VERIFIED_FROM_SOURCE` — `DISPROVEN` (the complete-guard claim), `VERIFIED_FROM_SOURCE`
  (the date-formula-only claim, which remains true as originally stated).
  Classification: `DISPROVEN` for "the complete guard is horizon-agnostic."
- **Executable evidence**: the standalone `RaceDateAlignmentVerifier` test suite confirmed, for
  the real generated 8–14-week `TEN_K_MASTER v6` schedules, that the date-tolerance component
  alone genuinely holds at every one of those non-default week counts. Classification:
  `VERIFIED_BY_EXECUTABLE_TEST` for the date-tolerance component specifically.
- **Activation consequence**: an upstream horizon expansion introducing a second standalone
  week count would still be rejected by the combined live guard purely on week count, even with
  perfectly aligned dates — a real, if currently dormant, activation risk.
- **Correction mechanism**: a historical addendum was added directly beneath the original claim
  in `PHASE4G_3A_EIGHT_WEEK_CORE_ALLOCATION_AUDIT.md` section 15, plus a new open risk record,
  `TD-RACEDATE-CHECK-NOT-HORIZON-AGNOSTIC-001`.
- **Lesson**: the original claim was tested — informally, by inspection — against only the one
  case that had ever actually executed (the fixed 12-week allocation). Testing only the active
  default case cannot establish independence from the parameter that case happens to hold
  fixed. This is exactly the scenario section C.4 above exists to prevent.

## G. Applicability

This governance applies to:

- phase audits;
- final reports;
- activation-readiness decisions;
- refactor claims;
- test plans;
- code-review summaries;
- agent-generated implementation reports.

This document is documentation and governance guidance only. It is not loaded, parsed, or
consumed by any runtime code, and it must not become a mechanically-enforced gate without a
separate, explicit decision to build one (consistent with this repository's existing
`activation-readiness-risks.json`/`.md` convention, which is documentation-only for the same
reason — see `ActivationSafetyGateTests.ActivationReadinessRisksFile_IsNotMechanicallyConsumedByAnySourceFile`
for the established precedent of testing that fact rather than assuming it).
