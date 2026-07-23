# Phase 4F.6 Pre-Implementation — Step A.1: Role Ownership and Missing-Field Clarification

Companion document to `phase4f6-step-a1-role-ownership-and-gap-clarification.json`. Read-only audit. No literature scan performed, no scientific evidence added. Purpose: separate what the repository confirms explicitly, what is only architectural inference, and what requires a product/schema/governance decision, so a future Step C does not conflate repository architecture with training-science decisions.

## 1. Verdict

**ROLE_OWNERSHIP_RESOLVED_WITH_DECISIONS_REQUIRED**

The repository does not leave role ownership "unclear" in a vague sense — it leaves it **precisely and consistently absent**: no structural-role field exists anywhere on the workout-progression contract, no role-to-workout-key binding mechanism exists at runtime, and the one family-grained mechanism that does exist (`RoleCompatibleFamilies`) is publish-time tooling logic, not a catalog artifact or a runtime capability. This absence is uniform across KEY_SESSION, EASY_SUPPORT, and LONG_RUN — it is not that LONG_RUN was singled out for omission.

## 2. Step-B readiness

**READY_FOR_STEP_B_WITH_SCOPED_DECISIONS_DEFERRED_TO_STEP_C**

Step B (structural-role compatibility / runtime-eligibility / fallback-chain evidence mapping against training science) can proceed for the fields that already have confirmed repository facts (stage exposure bounds, compression/extension behavior, phase durations — all `PLACEHOLDER_UNCONFIRMED` per `PilotDomainContentAudit.cs`, which is exactly the kind of gap Step B is meant to evidence-check). The **role-binding mechanism design itself** (A1-Q04/Q05/Q06/Q08) is out of Step B's scope — it is architecture, not training science — and must be escalated to Step C as a PRODUCT/SCHEMA/GOVERNANCE decision, not resolved via literature.

## 3. The central distinction (per the task's own framing)

| Class | Meaning | Count in this audit |
|---|---|---|
| **Repo says this explicitly** (`REPOSITORY_CONFIRMED`) | Directly observable in code/schema/JSON, no interpretation needed | 6 of 12 questions |
| **Repo only resembles this** (`ARCHITECTURAL_INFERENCE` / `MECHANICALLY_DERIVED`) | Pattern-matched from data or derived by tracing consumers, not stated as a contract | 5 of 12 questions |
| **Repo doesn't say — decision needed** (`PRODUCT_DECISION_REQUIRED` / `SCHEMA_DECISION_REQUIRED` / `GOVERNANCE_DECISION_REQUIRED`) | No evidence either way; a human decision is the only path forward | 1 of 12 questions as primary class, but present as `decisionRequired: true` on 7 of 12 |

## 4. Q1 — Is WorkoutProgressionDefinition explicitly KEY_SESSION-scoped?

**REPOSITORY_CONFIRMED: No.** `WorkoutProgressionStageDefinition` has no role field whatsoever. The belief that it's "KEY_SESSION-only" is a data-pattern illusion, not a contract fact — see Q2.

## 5. Q2 — What do current stage candidates actually target?

**MECHANICALLY_DERIVED.** 5 of 7 stages target QUALITY-family workouts (KEY_SESSION-compatible under the validator's family rule); **2 of 7 target EASY-family workouts** (`FOUNDATION_EASY_BASE`, `TAPER_SHARPEN` → `EASY_STANDARD`). Zero target LONG_RUN family. This means the progression is not "key-session only" — it already reaches into EASY territory, just never into LONG_RUN territory. This nuance matters for Step C: EASY_SUPPORT already has a toehold in the progression; LONG_RUN has none at all.

## 6. Q3 — How is the eligible-workout field actually used?

**REPOSITORY_CONFIRMED.** v10's `INTERMEDIATE_MODIFIER v6` uses `EligibleWorkouts` (exact, versioned references), not the legacy `EligibleWorkoutKeys` (bare-string set) the task prompt names — an important precision, since the literally-named field isn't even the one v10 exercises. Both variants are consumed only as `VALIDATION_ONLY` or `ELIGIBILITY_FILTER` (closure/whitelist) — never as a selection mechanism, and never order-sensitive (it's a `Set`).

## 7. Q4 — Does an existing role-binding mechanism exist?

**MECHANICALLY_DERIVED.** One mechanism exists: `CandidatePublishGraphValidator.RoleCompatibleFamilies` (role → family, e.g. `LONG_RUN → [LongRun]`). It is explicit, deterministic, but **family-grained** (not workout-key-grained), lives only in Process A publish-time validator source (not a versioned catalog artifact), and is **never consumed by backend runtime** — confirmed by inspecting `CatalogRunLayoutSlots.cs` and `CatalogWeekSkeletonCalendarMaterializer.cs`, which treat `StructuralRole` as an opaque string throughout.

## 8. Q5 — EASY_SUPPORT ownership

**ARCHITECTURAL_INFERENCE**, low ambiguity at the family level (only one EASY-family workout exists at all — `EASY_STANDARD` — so there's no competing candidate), but the mapping is nowhere explicitly encoded. The deeper architectural question: BUILD and RACE_SPECIFIC weeks have **zero** EASY-family stages, so even if `EASY_SUPPORT → EASY_STANDARD` were formalized, those weeks' two EASY_SUPPORT slots still couldn't be filled by any stage today. `nextOwner`: Step C / Product.

## 9. Q6 — LONG_RUN ownership

**MECHANICALLY_DERIVED**, and this is the audit's most significant finding. `LONG_RUN_STANDARD` is reachable only via the level-modifier's eligible list — **zero stages ever reference it**. Meanwhile, the repository's own tier-1 canonical evidence (Golden Fixture v3) contains a real generated long-run session using a **different, more complex** workout key, `LONG_RUN_PROGRESSION` (with `MAIN_SET` + `STEADY_FINISH` components), which does not exist as a catalog artifact and is explicitly, already documented (in `domain-wave1-schema-necessity-audit.md`) as "a distinct, non-substitutable workout key, not evidence for `LONG_RUN_STANDARD` itself." The absence of any LONG_RUN-targeting stage is therefore neither accidental nor simply "not yet done" — it is a **known, tracked, explicitly deferred open decision**, evidenced by `ten-k-pilot-vocabulary-decisions.md`'s own words: "intentionally left as an open decision for a future, explicitly-scoped follow-up."

## 10. Q7 — Was LONG_RUN progression ever accepted as a catalog concept?

**REPOSITORY_CONFIRMED: No.** Every repository occurrence of `LONG_RUN_PROGRESSION` is either inside example/fixture documents (`EXAMPLE_ONLY`) or inside governance prose that discusses and defers it (`PROPOSED_NEVER_ACCEPTED`). Zero occurrences inside `plan-catalog/catalog/` (the live artifact directory).

## 11. Q8 — Which bounded context owns role binding?

**GOVERNANCE_DECISION_REQUIRED.** The only bounded-context split found (`Appsel.PlanCatalog` / `Appsel.PlanGeneration`) is explicitly labeled a *suggestion* ("Önerilen") in its own source document, and that document explicitly scopes itself to Process A only, excluding Process B. Neither side of the codebase currently claims ownership of "assigning workout identity to a structural role" — it is genuinely unowned, not merely ambiguous between two claimants.

## 12. Q9 — Does a public workout-type mapping exist?

**REPOSITORY_CONFIRMED: No**, for all 5 pilot workout keys — `PlanCatalogDomainMapper.ClassifyWorkoutKeys` explicitly marks every one `RequiresNewField` with an explicit "plausible loose family match only" caveat. Neither candidate public enum (`TrainingDayType`, `GeneratedCatalogWorkoutType`) has a confirmed exact mapping.

## 13. Hypotheses summary (H1–H10)

| ID | Verdict |
|---|---|
| H1 (contract is KEY_SESSION-only) | CONTRADICTED |
| H2 (data-only key-session, contract role-agnostic) | PARTIALLY_SUPPORTED (premise about data is false; role-agnosticism is true) |
| H3 (eligible-list is whitelist, not selector) | SUPPORTED |
| H4 (EASY_SUPPORT→EASY_STANDARD unencoded) | SUPPORTED |
| H5 (LONG_RUN→LONG_RUN_STANDARD unencoded) | SUPPORTED |
| H6 (LONG_RUN delegated to Phase 4F.7 prescription) | NOT_SUPPORTED |
| H7 (LONG_RUN progression intended but omitted) | PARTIALLY_SUPPORTED (deliberate, documented deferral — not an oversight) |
| H8 (static role-binding policy required before 4F.6B) | INSUFFICIENT_EVIDENCE (need is clear; correct solution shape is not) |
| H9 (existing mechanism already sufficient) | CONTRADICTED |
| H10 (new evidence registry likely required) | PARTIALLY_SUPPORTED |

## 14. Missing-field classification (Step A's 15 `MISSING` fields)

15 fields classified into: 3 `NON_BLOCKING_INFORMATIONAL`/schema-intentional (public-type gap aside), 2 `INTENTIONALLY_DEFERRED` (numeric dosage, content hash), 5 `EVIDENCE_ASSESSMENT_REQUIRED` (exposure bounds, compression/extension, phase-duration split — feed directly into Step B), 3 `DOCUMENTATION_GAP` (stage-key provenance, stale golden test), 2 `PRODUCT_DECISION_REQUIRED` (phase intents vocabulary, LONG_RUN binding), 1 `SCHEMA_DECISION_REQUIRED` (public workout-type mapping — also blocking). Full table in the JSON artifact (`missingFields[]`).

## 15. Evidence-registry governance

Neither existing artifact is a general training-domain evidence registry: `PilotDomainContentAudit.cs` records decisions/governance status (right scope, wrong shape — no source/claim/supports/doesNotSupport structure); `evidence-log.json` has the right atomic shape but an explicitly narrow, stated scope (4 runtime-resolver thresholds only). **Recommended governance outcome: `NEW_TRAINING_DOMAIN_REGISTRY_RECOMMENDED`** — not created here, per the task's explicit prohibition; this is a recommendation for Step C, not an action taken.

## 16. Join-key audit

Existing join keys (`AUD-xxx`, `EV-xxx`, `TD-xxx`, `stageKey`/`phaseKey`) are each locally well-formed, but none of them cross-reference each other in a structured way — e.g., `AUD-xxx` entries store a prose `JsonPath` rather than a first-class `stageId` field, and `TD-xxx` risk entries reference candidate versions in prose rather than a structured range. A future Step B/C artifact should adopt parallel structured `stageId`/`decisionId`/`evidenceId`/`auditId`/`riskId` fields — proposed, not implemented.

## 17. Validation performed

- `plan-catalog`: build 0 errors/0 warnings; full test suite 335/335 passing.
- `backend`: build 0 errors/0 warnings; `PlanCatalogDomainMapperTests` 10/10 passing.
- No production code, catalog JSON, schema, `evidence-log.json`, `PilotDomainContentAudit.cs`, or canonical decision artifact modified.
- One incidental `generatedAtUtc` timestamp regeneration in an unrelated pre-existing report (side effect of running the plan-catalog test suite) detected and reverted via `git checkout --`.

## 18. Explicit non-actions (per task's prohibited-changes list)

No literature search performed. No new evidence sources, sources, or bibliography entries added. No `IWorkoutStageScheduler` designed. No stage-to-week assignment performed. No workout binding implemented. No new workout definitions added. No catalog JSON, schema, runtime, preview, confirm, or persistence code modified. No layout slots or level-modifier semantics changed. No `WorkoutBindingMode`/`RoleWorkoutBindingPolicy` field added. No `LONG_RUN_PROGRESSION` catalog artifact created. No workout selected, no stage scheduled, no prescription value calculated. Phase 4F.6A/4F.6B/4F.7 not implemented. No commit made.

## 19. Repository state

Branch `main`, HEAD unchanged at `0c6796578f08bc1d76d96f1944a80c9075455206`. All Phase 4F.5 / 4F.5.1 / Step A / Step A.1 work remains uncommitted, exactly as before this step.

## 20. Next step

Per the task's explicit instruction, this stops after Step A.1. Do not proceed to Step B, 4F.6A, or 4F.6B without further explicit instruction.
