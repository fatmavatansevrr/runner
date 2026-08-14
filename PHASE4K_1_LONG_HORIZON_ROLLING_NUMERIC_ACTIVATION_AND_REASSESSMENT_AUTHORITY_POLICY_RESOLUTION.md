# Phase 4K.1 — Long-Horizon Rolling Numeric Activation and Reassessment Authority Policy Resolution

## 1. Executive result

A pure governance/policy-decision phase — zero production code, zero diagnostics/simulations (this phase decides the *shape* of the solution, not its numeric content). The rejected full-upfront 21–52-week numeric prescription model (and Path B's narrow one-horizon activation, `TD-LONG-HORIZON-NUMERIC-ACTIVATION-BOUNDARY-001`) is replaced by an approved **rolling numeric activation policy**: the full 21–52 week structural roadmap remains fully supported, while executable numeric prescription is limited to a 4-week rolling activation window, recalculated at each checkpoint from current validated evidence. Six lifecycle states, checkpoint authority scoped to the next window only, a fail-closed missing-evidence principle, structural immutability, and a Runway/Core just-in-time numeric-resolution principle are all approved at the policy-shape level. No maintenance formula, checkpoint evidence threshold, or Runway/Core exact timing rule is defined — all explicitly deferred. No commits made.

Final classification:

```
LONG_HORIZON_ROLLING_NUMERIC_ACTIVATION_AND_REASSESSMENT_AUTHORITY_POLICY_APPROVED

LONG_HORIZON_21_TO_52_STRUCTURAL_ROADMAP_REMAINS_SUPPORTED_WITH_NUMERIC_PRESCRIPTION_LIMITED_TO_A_FOUR_WEEK_ROLLING_ACTIVATION_WINDOW

LONG_HORIZON_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_VALIDATED_LOAD_MAINTENANCE_ENVELOPE_CHECKPOINT_STATE_TRANSITION_AND_JUST_IN_TIME_CONTEXT_POLICIES
```

## 2. Problem established by Phase 4I.6B.1

Real production-pipeline evidence proved full-upfront numeric prescription is universally compatible only at 21 weeks (GE=1, no progression). Every horizon 22 and above fails for virtually every tested input class, because Core's Week-1 numeric target is derived once from initial user evidence and does not remain valid many months later once GE has progressed — generating all future numeric prescriptions from Day-1 evidence is not viable for a 21–52 week product.

## 3. Selected redesign direction

Path A — `LONG_HORIZON_VOLUME_ENVELOPE_AND_MAINTENANCE_PHASE_REDESIGN` — is adopted. This phase approves Path A's high-level shape: **B. Full structural roadmap with rolling numeric activation**, not A (full-upfront numeric prescription, rejected).

## 4. Structural roadmap policy

Unchanged and fully preserved for all 21–52 weeks: segment/stage/week/workout-role structure (Phase 4I.1/4I.4/4I.5), dates where already structurally known. The structural roadmap never requires final future distance or pace values to exist.

## 5. Full-upfront numeric rejection

Explicitly rejected as the product model. Path B's narrow 21-week-only activation is also explicitly rejected as an implementation target — it must not be built.

## 6. Activation-window decision

**`NumericActivationWindowWeeks = 4`** — an explicit Appsel **product default**, not a universal physiological rule, chosen because it matches the already-approved GE 3-development+1-recovery mesocycle exactly (Phase 4I.2), avoiding mesocycle-splitting. Applies to both the initial window and every subsequent window. Partial windows are approved at segment boundaries: a ShortExtension GE segment (1–3 weeks) activates only its own remaining GE weeks; when fewer than 4 GE weeks remain before the Runway boundary, the window may extend into the immediately-following Runway weeks (reusing the existing, real, already-authoritative Runway pipeline) rather than producing an artificially short orphan window.

## 7. Numeric lifecycle states

Exactly six approved: `StructurallyPlanned`, `NumericPending`, `NumericActivated`, `Completed`, `Missed`, `NumericActivationBlocked`. Structurally-planned/pending future weeks carry null (never fabricated zero) distance/duration/pace fields. Only activated weeks carry complete executable prescriptions. Completed/missed weeks are immutable history, never rewritten. Only future, not-yet-started weeks may receive new numeric activation. Structural segment identity (segment/stage/global week number) remains stable across every recalculation.

## 8. Checkpoint authority

A checkpoint evaluates current evidence and returns a typed decision — permit progression / require maintenance / block upward progression / block activation entirely — that is authoritative for activating the *next* numeric window only. It never rewrites completed history and never changes the approved structural roadmap by default. Detailed evidence fields, aggregation, and state-transition thresholds are explicitly deferred to Phase 4K.3.

## 9. Missing-evidence principle

Approved minimum safety rule: no new validated evidence implies no upward numeric progression. Preferred policy adopted: a maintenance-only window may be permitted if the last validated sustainable load and all minimum safety inputs remain available; otherwise the pipeline fails closed (blocks activation). The maintenance anchor/formula itself is explicitly deferred to Phase 4K.2 — not decided here.

## 10. Historical immutability

Race date, total structural horizon, GE/Runway/Core composition, segment order, globally-numbered weeks, completed history, and catalog provenance all remain stable/immutable after confirmation. Structural mutation is out of scope and disallowed by default.

## 11. Future numeric activation semantics

Explicitly distinguished from structural mutation: replacing a not-yet-activated future numeric prescription with a newly-recalculated one (based on current evidence) is expected lifecycle behavior, never treated as retroactive plan mutation.

## 12. Runway/Core just-in-time principle

Runway and Core remain structurally present from plan creation, but their final numeric prescriptions are resolved using current validated evidence near their own activation boundary — not frozen from initial-creation evidence, which may be many months stale by the time a long plan reaches those segments. The existing real Runway/Core pipelines, target-time/goal-feasibility/pace-source policies all remain fully unchanged and authoritative. No Core target is ever arbitrarily lifted from planned GE progression (reaffirming Phase 4I.6B's own rejection); no GE-side ceiling is introduced. Exact checkpoint timing and evidence precedence are explicitly deferred to Phase 4K.4.

## 13. Public product semantics

Conceptual only, no DTO field names decided. A confirmed Long-Horizon plan conceptually contains: (1) the complete structural roadmap, (2) the currently activated numeric window, (3) future numeric-pending weeks (never appearing as executable prescriptions before activation), (4) next-checkpoint information, (5) numeric lifecycle status. User-facing principle: "Appsel shows the full journey; only the near-term training prescription is finalized; later prescriptions are recalculated from updated evidence" — never "your plan is incomplete" framing.

## 14. Non-claims

Recorded verbatim: four weeks is a product default, not a universal physiological rule; rolling activation does not guarantee injury prevention; maintenance is not Taper; checkpoint reassessment is not medical diagnosis; planned progression is not proof of achieved fitness; future Core capacity cannot be inferred solely from the original baseline; HRV or wearable data is not required by this decision.

## 15. Governance artifacts

`TD-LONG-HORIZON-ROLLING-NUMERIC-ACTIVATION-001` — added **CLOSED** (plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}). `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` — updated to reflect the now-approved direction, remains **OPEN** (detailed content still pending). `TD-LONG-HORIZON-NUMERIC-ACTIVATION-BOUNDARY-001` — unchanged, remains CLOSED (its own historical record is preserved append-only; its activation *model* is superseded by this phase, not its record). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — unchanged, remains OPEN.

## 16. Deferred decisions

Validated-load and maintenance policy (Phase 4K.2); checkpoint evidence/state-transition policy (Phase 4K.3); Runway/Core exact just-in-time context policy (Phase 4K.4); rolling contracts; rolling runtime; persistence; public preview; Flutter.

## 17. Tests

16 new governance cross-check tests in plan-catalog (`LongHorizonRollingNumericActivationGovernanceTests.cs`), covering all 22 required proof points from the phase's own test list (several proof points share one test method where the underlying JSON content naturally covers both). All passing. Full plan-catalog suite: **948 passed, 0 failed, 0 skipped** (932 baseline + 16 new). Backend suite not run — no mechanically-consumed shared artifact was changed (zero production files touched).

## 18. Final classification

```
LONG_HORIZON_ROLLING_NUMERIC_ACTIVATION_AND_REASSESSMENT_AUTHORITY_POLICY_APPROVED

LONG_HORIZON_21_TO_52_STRUCTURAL_ROADMAP_REMAINS_SUPPORTED_WITH_NUMERIC_PRESCRIPTION_LIMITED_TO_A_FOUR_WEEK_ROLLING_ACTIVATION_WINDOW

LONG_HORIZON_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_VALIDATED_LOAD_MAINTENANCE_ENVELOPE_CHECKPOINT_STATE_TRANSITION_AND_JUST_IN_TIME_CONTEXT_POLICIES
```

## 19. Exact next phase

**Phase 4K.2 — Validated Load, Growth Eligibility and Maintenance Envelope Policy Resolution** — the next governance phase in sequence, resolving the maintenance-volume formula and validated-load/growth-eligibility rules this phase deliberately deferred.
