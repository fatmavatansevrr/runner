# APPSEL — HM PHASE PROMPT 0
## PHASE HM.0 — HALF-MARATHON DISTANCE GENERALIZATION REUSE AUDIT
### REAL APPSEL .NET BACKEND — AUDIT ONLY, NO PRODUCTION CODE

**Prepared:** 2026-08-28 (following the 10K V1 engagement's closure at `TEN_K_V1_CAPABILITY_COMPLETE`, `GEN.39`)

---

## 0. Mandatory startup — same discipline as every 10K phase

```text
read PHASE_LEDGER.md in full
read MASTER_ROADMAP.md in full
git log -5 --oneline
git fetch origin && diff HEAD origin/main
git status (clean worktree required)
confirm the 10K V1 matrix's current true state directly from the
    ledger — this audit treats it as frozen evidence, so confirm what
    "frozen" actually contains before relying on it
identify the phase-ID convention to use for this new HM engagement —
    check whether the repo's own convention expects a new namespace
    (e.g. HM.0) alongside GEN.*/FREQ.*, or something else; do not
    assume, confirm from repository truth or ask if genuinely ambiguous
```

Use the verified-reliable process-check method (unfiltered `tasklist` + manual PID review, or `wmic`) established during the 10K engagement, if any process/build verification is needed during this audit.

---

## OBJECTIVE

Determine the minimum architectural and policy-authority work required to add:

```text
HALF_MARATHON
× INTERMEDIATE
× 4D
× 14-WEEK PREFERRED CORE
× DARK ONLY
```

while reusing the Level × Frequency × Horizon architecture already proven by the current 10K implementation.

The purpose of this phase is **NOT** to design the HM plan. The purpose is to prove whether Half Marathon can enter the current generation pipeline as another distance authority without creating a second parallel plan-generation system.

Treat the currently supplied 10K public matrix as frozen evidence that Level × Frequency generalization already exists. Do not re-solve that problem.

---

## 1. HARD PHASE BOUNDARIES

DO NOT:
- write production code
- add `HALF_MARATHON` catalog artifacts
- activate public HM routing
- modify the current 10K public matrix
- change any existing 10K behavior
- invent HM numeric values
- decide `OPEN-HM-01` through `OPEN-HM-07`
- copy 10K numeric values into HM
- reopen external research
- generalize for Marathon or other future distances unless the first HM slice demonstrably requires the same seam
- fix unrelated defects discovered during the audit

If a required generic seam cannot be opened without choosing a new HM numeric authority, classify that dependency as `OPEN_HM_PRODUCT_DECISION` and stop at the boundary.

---

## 2. REQUIRED CLASSIFICATION

For every relevant authority/component, classify it as exactly one of:

```text
A. DISTANCE_GENERIC_REUSE
   Already genuinely distance-independent, reusable without semantic change.

B. TEN_K_DISTANCE_POLICY
   Mechanism is generic, but the selected data/policy belongs specifically
   to 10K and HM requires its own authority.

C. TEN_K_HARDCODE_REQUIRES_PARAMETERIZATION
   Control flow or wiring still contains an implicit/explicit 10K
   assumption that must be parameterized before HM can use the path.

D. HM_NEW_DISTANCE_AUTHORITY_REQUIRED
   The generic seam already exists, but HM needs a new typed/catalog/
   policy authority.

E. OPEN_HM_PRODUCT_DECISION
   The architecture can represent the concept, but the research/product
   decision has not yet been frozen.

F. DUPLICATE_OR_AMBIGUOUS_AUTHORITY
   More than one place currently appears to own the same semantic
   decision. Do not silently choose one authority.
```

---

## 3. MANDATORY AUDIT SURFACE

Inspect the real production path and tests for:

```text
1. Distance identity
2. Candidate identity
3. Combination compatibility
4. Public pilot/product gates
5. RunLayout ownership
6. Level dispatch
7. Frequency dispatch
8. Core horizon classification
9. Dynamic phase allocation
10. Compression
11. Extension
12. Preparation Runway
13. LongHorizon / rolling checkpoint
14. Starting-volume resolution
15. Peak-volume resolution
16. Weekly progression safety
17. Cutback
18. Session-volume allocation
19. Long-run resolution
20. Long-run share/caps
21. Workout progression
22. Workout eligibility
23. Workout dose
24. Pace-source resolution
25. Goal-feasibility handling
26. Calendar materialization
27. PreferredDays
28. LongRunDayPreference
29. Role/cardinality validation
30. Adaptation
31. Preview
32. Confirm
33. Persistence
34. Public routing
35. Tests / golden fixtures / rollout tests
```

Do not audit only class names. Trace the real call path.

---

## 4. SEMANTIC HARDCODE SEARCH

Do not search only for literal `TenK`/`TEN_K`/`ten_k`. Also search for semantic assumptions that may encode 10K without naming it, including but not limited to:

```text
preferred core = 12
core range = 8-14
runway begins at 15
10.0 / 10 km distance assumptions
TEN_K goal-pace workout identities
10K-specific workout progressions
10K-specific peak bands
10K-specific starting-long-run values
10K-specific readiness thresholds
10K-specific phase allocation
10K-specific taper assumptions
switch/default branches whose only currently reachable distance is 10K
constructors/services whose apparent genericity is only because TenK is
    the sole caller
test fixtures that accidentally act as production authority
```

For every discovered assumption, answer: is the mechanism actually distance-specific, or is the mechanism generic and only its selected policy data 10K-specific?

---

## 5. RECURRING-ASSUMPTION-FAMILY SEARCH — MANDATORY

Whenever one hardcoded or implicit distance assumption is found, **do not stop at that occurrence.** Search the entire recurring defect/assumption family across: resolver, allocator, adapter, orchestrator, validator, eligibility policy, preview path, runway path, long-horizon path, persistence mapper, tests, sibling implementations.

For each family, produce:
```text
ASSUMPTION_FAMILY
PRIMARY_OCCURRENCE
SIBLING_OCCURRENCES
CURRENT_AUTHORITY
CONSUMERS
REQUIRED_ACTION
```

Example principle: if a core adapter assumes "week 1 contains the KEY session," search every sibling volume/pace/workout adapter for the same assumption. If a horizon resolver assumes 12 preferred weeks, search all runway and long-horizon consumers for the same assumption. **Do not classify the family as closed after inspecting only one instance** — this exact failure mode (finding one instance, fixing it, discovering siblings later) recurred repeatedly across the 10K engagement (`GEN.10/12/17/19/20/23/29/32/33/34/35/36/37/38`, 15+ confirmed instances).

---

## 6. AUTHORITY OWNERSHIP AUDIT

For each major decision, answer **who owns this value** — not merely where it is consumed. At minimum resolve ownership for: distance core min/preferred/max, phase allocation, run-layout role cardinality, level eligibility, frequency eligibility, peak-volume band, starting-volume bounds, progression coefficients, session allocation, starting-long-run bounds, long-run share/cap, workout progression, workout dose, pace source, goal feasibility, horizon routing, runway composition.

If the same semantic decision is owned by multiple places, classify `DUPLICATE_OR_AMBIGUOUS_AUTHORITY`. Do not silently preserve duplication.

---

## 7. FIRST-SLICE REACHABILITY TRACE

Trace the exact hypothetical route for `HALF_MARATHON × INTERMEDIATE × 4D × 14 WEEKS × DARK` from request/input through: identity → eligibility → horizon classification → phase allocation → skeleton → catalog binding → numeric policies → workouts → volume → long run → pace → calendar → validation → preview-dark materialization.

For every step classify: `REUSED_AS_IS` / `NEEDS_DISTANCE_PARAMETER` / `NEEDS_HM_AUTHORITY` / `BLOCKED_BY_OPEN_DECISION`.

This trace must make the minimum implementation surface explicit.

---

## 8. BEHAVIOR-PRESERVATION PROOF

Do not say only "this can be generalized safely." For every required parameterization, explain why existing reachable 10K behavior can remain unchanged. Required proof shape:

```text
CURRENT 10K INPUT
-> current authority
-> current branch/value

AFTER PROPOSED GENERALIZATION
-> TenK selects the same authority/value
-> same branch
-> same observable output
```

The target standard is **zero 10K behavioral delta by construction**, not merely "tests probably protect it." Identify any proposed seam where this proof cannot currently be made.

---

## 9. SIBLING-CONSUMER IMPACT

Whenever an authority is proposed to become distance-keyed, enumerate every consumer (e.g. a distance-keyed core duration may affect the core allocator, compression, extension, runway, long horizon, checkpoint runtime, validators, plan metadata, tests). Do not recommend parameterizing the producer without checking the sibling consumers.

---

## 10. MINIMAL-GENERALIZATION RULE

Prefer existing generic engine + distance-keyed authority over new HM control flow. However, do not introduce abstractions solely because they might later help Marathon, 5K, or another distance. The allowed generalization target is the minimum seam required for `HM × Intermediate × 4D × 14W × DARK`. Future-proofing alone is not sufficient justification.

---

## 11. 10K REGRESSION CONTRACT

The proposed HM path must preserve: no 10K numeric delta, no 10K workout delta, no 10K calendar delta, no 10K adaptation delta, no 10K horizon delta, no 10K eligibility delta, no 10K public rollout delta — unless a completely separate approved fix is explicitly required. List the exact test groups that would later prove each invariant. Do not run a large regression suite in this audit unless necessary to inspect existing behavior — this phase is primarily static architecture/authority analysis.

---

## 12. REQUIRED OUTPUT

Create `PHASE_HM_0_DISTANCE_GENERALIZATION_REUSE_AUDIT.md` containing:

```text
A. Executive classification
B. Real current architecture map
C. First-slice reachability trace
D. Reusable generic authorities/components
E. 10K-specific policy data
F. Hardcoded 10K assumptions requiring parameterization
G. Recurring-assumption-family inventory
H. Duplicate/ambiguous authority inventory
I. HM authorities required for Intermediate x 4D x 14W
J. OPEN-HM decisions encountered (reference OPEN-HM-01...07, do not resolve them)
K. Minimal file-change forecast (exact likely files/classes, NEW/MODIFY/NO CHANGE, reason)
L. Sibling-consumer impact matrix
M. 10K behavioral-preservation proof
N. Regression-test forecast
O. Risks
P. Exact recommended next phase
```

---

## 13. FINAL CLASSIFICATION

Return exactly one:

```text
HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE

    only if: no second HM-specific generation engine is required;
    the first HM slice has a clear generic route; all required
    HM-specific inputs can be expressed as typed authorities; every
    required 10K parameterization has a credible zero-delta
    preservation path; no unresolved duplicate authority blocks
    implementation.

OR

HM_DISTANCE_GENERALIZATION_ARCHITECTURE_BLOCKED

    and identify the exact blocking coupling(s).
```

Do not classify `READY` merely because the architecture looks conceptually generic. Prove it from the actual repository.

---

## 14. Governance closure — same discipline as every 10K phase

Even though this phase makes no production-code change, it establishes real findings that future phases depend on. Close it the same way every decision-only 10K phase was closed (`GEN.13`/`26`/`30` and others):

```text
PHASE_LEDGER.md row recording this audit's classification and key findings
MASTER_ROADMAP.md update establishing the HM engagement's starting state
commit, push per gate (two-commit SHA-backfill pattern if that remains
    this repo's convention)
```

---

## 15. What happens next is determined by the result, not assumed in advance

```text
IF HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE:
    -> proceed toward HM authority closure (OPEN-HM-01/02/03/04 resolution
       for the Intermediate x4D x14W slice specifically), then the first
       dark vertical slice implementation
    -> do not assume the full OPEN-HM-01...07 set must close before this;
       only what the first slice actually needs

IF HM_DISTANCE_GENERALIZATION_ARCHITECTURE_BLOCKED:
    -> resolve only the identified blocking coupling
    -> re-run an HM.0 closure check afterward
    -> do not proceed to authority/implementation work until unblocked
```

Report back with the full audit output, the final classification, and the recommended next phase before any further prompt is drafted.
