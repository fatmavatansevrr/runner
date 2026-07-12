# Activation Safety Clarification — Post Wave 8 / D13

Focused clarification pass. Does not reopen D13/D2/D3/D4. No publish/activate/retire/supersede performed.

## Question 1 — `PLACEHOLDER_UNCONFIRMED` meaning for `GOAL_PACE_TEN_K` v1

**Conclusion: `FORMAL_APPROVAL_MISSING_ONLY`.**

- `ContentDecisionStatus.PlaceholderUnconfirmed`'s own doc comment: *"Value was authored/invented during implementation with no traceable canonical source"* — this describes **provenance** (missing source/approval), not content quality.
- `docs/README.md` §1: *"If precedence does not resolve the conflict, the decision remains `PLACEHOLDER_UNCONFIRMED`"* — a source-precedence governance state, not a content-defect flag.
- `domain-blocker-source-map.json`'s own D13 question explicitly lists **three** equally sufficient resolution paths, the third being *"an explicit product decision to accept a default / remove the workout"* — i.e., accepting the **existing** default via explicit approval, with **zero content change**, was pre-sanctioned by this repository's own governance design before Wave 8 ran.
- `AUD-249`'s original text (unchanged) never claims the content is deficient — only that no fixture evidence exists and the legacy prescription mode was *"left unmigrated rather than invent a ... guess"* (a deliberate non-guess, not a defect).
- `acceptanceStandard.EXPLICIT_PRODUCT_DEFAULT` requires documented rationale + dated/attributable approval — **not** a changed value. Wave 8 satisfied exactly this.

**No content-level incompleteness was found or flagged. D13 is not reopened.**

## Question 2 — Static catalog consistency vs. runtime schedule generation

**No Process A code path exists that turns the catalog bundle into a concrete weekly schedule.** Searched every namespace in the solution (`PlanCatalog.Cli/Contracts/Core/Infrastructure`, all sub-folders) — there is no `Scheduling`, `Generation`, `WeeklyPlan`, or `Calendar` module. `grep` for schedule/calendar/week-assignment terms matched only incidental string literals (a file-path string, XML-doc prose), never executable logic.

- **Hard-session enforcement** (`TemplateCombinationValidator`) compares the **static** `RunLayoutDefinition`'s `KEY_SESSION` slot *count* against `MaximumHardSessionsPerWeek` — a shape check on the authoring-time layout, not a check against any generated per-week output (none exists).
- **`GOAL_PACE_TEN_K` consumption** is only as a `workoutCandidate` inside a `WorkoutProgressionStageDefinition`, which is explicitly documented as *"Deliberately exclud[ing] week numbers, calendar dates, actual phase duration"*.
- **Was v10 tested through a schedule-generation code path?** Not applicable — no such code path exists in this repository.
- **Is a week containing `GOAL_PACE_TEN_K` generated without a second hard day?** `UNKNOWN_FROM_REPO_EVIDENCE` — that capability, if it exists, lives entirely in Process B, which was not inspected (out of scope).

**Conclusion: D13 (and the whole candidate graph) is proven correct only at static catalog consistency level — not at runtime generated-schedule level.**

## Question 3 — Activation-risk gate status

**Classification: `DOCUMENTATION_ONLY`.**

- Zero references to `activation-readiness-risks.json`, `TD-D3-001`, or `TD-WAVE5-001` anywhere in `src/` or `tests/` executable code (the only source hits are free-text reason strings inside `PilotDomainContentAudit.cs`, never parsed or branched on).
- `PublishReadinessValidator.Validate` gates only on: schema validation, domain/graph validation, `ContentHash` presence, hash collisions, and (via `ValidateContentDecisionsDetailed`) `PLACEHOLDER_UNCONFIRMED` blocking for the Production channel. It has **zero** knowledge of the risk-note files.
- **Practical consequence:** zero domain blockers is today **sufficient** to pass the content-decision guard for a Production-channel `publish` attempt (subject to schema/graph validation also passing) — the two OPEN activation risks would **not** mechanically block it. This is a real, now-documented gap between recorded risk and mechanical enforcement.

**Action taken:** A full publish/activate gate extension was **not** implemented — `PublishReadinessValidator.Validate` is a narrow, pure static method with no existing port for external risk-note lookup; wiring one in would require a new abstraction, CLI changes, and undefined severity/channel decisions — not a minimal, safe extension point. Instead, a **non-invasive regression test** (`ActivationSafetyGateTests.cs`) was added that permanently asserts: v10's blocker closure is empty, `activation-readiness-risks.json` still has ≥1 `OPEN` risk, and no source file mechanically consumes the risk file or TD IDs. If either assumption silently changes, the test suite will surface it.

## Question 4 — Final classification

**`READY_FOR_ACTIVATION_REVIEW_WITH_OPEN_RISKS`**

Domain/catalog completeness is proven (0 remaining blockers, all structural/schema/graph validators pass) — but that proof is static-catalog-only, and the two open activation risks are not mechanically enforced. The candidate is ready for a **human/product activation review**, not for automatic or unreviewed publish/activate, until `TD-D3-001` and `TD-WAVE5-001` are explicitly addressed or consciously waived.

## Status summary

- D2 / D3 / D4 / D13: **CLOSED** (unchanged; D13 not reopened).
- Remaining domain blockers: **0**.
- Open activation risks: **TD-D3-001, TD-WAVE5-001**.
- No publish, activate, retire, or supersede action occurred.
- No files outside `plan-catalog/` were touched.
