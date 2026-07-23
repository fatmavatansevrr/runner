# Phase 4F.6 Pre-Implementation — Step A.2: EASY_SUPPORT Partial Coverage and 4F.6B Blocker Classification

Companion document to `phase4f6-step-a2-easy-support-coverage-and-blocker-classification.json`. Read-only, no literature scan, no architecture proposed. LONG_RUN is out of scope (already resolved as a complete absence in Step A.1) — this step concerns only EASY_SUPPORT.

## 1. Verdict

**EASY_SUPPORT_SCOPE_RESOLVED_WITH_4F6B_BLOCKER**

## 2. Executive conclusion

This is **not a current runtime defect** — the dark pipeline was never scoped to assign workout identity to any slot (all 4F.1–4F.5.1 phases), so its absence for EASY_SUPPORT is expected, not broken. It **is** a genuine **4F.6B readiness blocker**, and it is a **combined content-and-contract gap**, plus one **governance gap** on a distinct sub-question:

- **Contract gap** (`CONTRACT_ABSENT`): no mechanism anywhere binds a structural role to a workout identity, for any role, consumed by runtime.
- **Content gap** (conditional): *if* stages are eventually chosen as the binding source, BUILD and RACE_SPECIFIC phases have zero EASY-family stage candidates today.
- **Governance gap**: the workout-key vocabulary question (should `EASY_WITH_STRIDES`/`EASY_SHAKEOUT` be created?) is `KNOWN_DEFERRED_DESIGN` — already documented, same pattern as `LONG_RUN_PROGRESSION`. But the binding-*mechanism* question itself has never been recorded anywhere — `PREVIOUSLY_UNRECORDED_GAP`.

## 3. The two EASY-family stages

| Stage | Phase | Candidate | Exposures | Compression/Extension | Governance |
|---|---|---|---|---|---|
| `FOUNDATION_EASY_BASE` | FOUNDATION | `EASY_STANDARD` v4 | 3–6 | COMPRESSIBLE / EXTENDABLE | AUD-013/014/015, all `PlaceholderUnconfirmed` |
| `TAPER_SHARPEN` | TAPER | `EASY_STANDARD` v4 | 1–2 | PROTECTED / FIXED_EXPOSURE | AUD-013/014/015, all `PlaceholderUnconfirmed` |

Both target the same single general-easy definition — no specialized EASY variant exists as an active catalog artifact. Neither stage carries a structural-role field (confirmed absent, `REPOSITORY_CONFIRMED`), and no test anywhere asserts either stage populates an `EASY_SUPPORT` slot.

## 4. Phase coverage matrix

| Phase | Stages | EASY-family stages | Candidates | EASY coverage? |
|---|---:|---:|---|---|
| FOUNDATION | 1 | 1 | EASY_STANDARD | Yes |
| BUILD | 2 | 0 | — | No |
| RACE_SPECIFIC | 3 | 0 | — | No |
| TAPER | 1 | 1 | EASY_STANDARD | Yes |

Structural fact, independent of the above: `RUN_LAYOUT_4D` places exactly 2 `EASY_SUPPORT` slots in every week of every phase, unconditionally. The mismatch (2 of 4 phases have zero matching stage content, yet every phase has 2 slots/week) is a mechanical observation only — it does not imply a stage must populate every slot, since whether stages are even the intended source is itself unresolved (§6).

## 5. Current runtime behavior

**No.** No step in the dark pipeline (`CatalogPlanSkeletonOrchestrator` → `CatalogWeekSkeletonCalendarMaterializer` → `DatedGeneratedCatalogPlanSkeletonValidator`) assigns workout key, family, prescription mode, pace, distance, or duration to *any* slot role — the underlying contract records (`DatedGeneratedCatalogWeekSkeleton`/`SessionSlotSkeleton`) have no field capable of holding that data at all; their doc comments say so explicitly. This is expected, scoped-out behavior for Phases 4F.1–4F.5.1, not a defect.

A separate, more consequential fact from the fixture audit: the tier-1 canonical Golden Fixture v3 shows `EASY_SUPPORT` populated in **every week of every phase** (23 of 24 slot-instances with `EASY_STANDARD`, one with `EASY_SHAKEOUT` — a non-catalog key) — including BUILD and RACE_SPECIFIC weeks where the current stage system has zero EASY-family output. This is `DOCUMENTATION_EXAMPLE` (a target end-state), not evidence of current runtime capability — the audited pipeline cannot reproduce it today.

## 6. Contract-gap analysis

**`CONTRACT_ABSENT`.** No field on any stage/layout/level-modifier contract references the other; the only role-adjacent mechanism (`CandidatePublishGraphValidator.RoleCompatibleFamilies`) is family-grained, publish-time-only tooling logic never consumed by runtime, used solely for a closure-membership validation rule — it selects nothing.

## 7. Content-gap analysis

**`CONTENT_NOT_APPLICABLE_PENDING_CONTRACT_DECISION`.** Since no contract establishes that stages are responsible for populating `EASY_SUPPORT`, declaring BUILD/RACE_SPECIFIC a confirmed content gap would presume an unconfirmed responsibility. Reported instead as a conditional finding: *if* stage-driven binding is chosen, FOUNDATION and TAPER are content-complete; BUILD and RACE_SPECIFIC are content-absent and would need new stage content.

## 8. Governance classification

Two distinct sub-questions, two distinct answers — do not collapse them:
- Workout-key vocabulary (`EASY_WITH_STRIDES`/`EASY_SHAKEOUT`): **`KNOWN_DEFERRED_DESIGN`** — explicitly documented in three governance artifacts, same non-substitutability framing as `LONG_RUN_PROGRESSION`.
- Binding mechanism itself (how any easy content reaches an `EASY_SUPPORT` slot): **`PREVIOUSLY_UNRECORDED_GAP`** — no governance artifact anywhere discusses this.

## 9. Test and fixture findings

Every production/test reference to `EASY_SUPPORT` (11+ locations across both solutions) is a structural role-string assertion — presence, count, ordering, or slot-key naming (`EASY_SUPPORT_1`/`EASY_SUPPORT_2`). Zero assert workout-identity binding. One test file (`CatalogWeekSkeletonCalendarMaterializerTests.cs:272-275`) contains an explicit comment disclaiming any such check exists — direct evidence of the gap, not an assumption of coverage. The Golden Fixture v3 occurrences are classified `DOCUMENTATION_EXAMPLE`, not `PRODUCTION_BEHAVIOR_ASSERTION`.

## 10. Blocker classification

Primary `BLOCKER_FOR_4F6B`: absence of any role-to-workout-key binding mechanism (runtime-consumed, deterministic); the conditional BUILD/RACE_SPECIFIC content shortfall (secondary, contingent on mechanism choice); absence of any contract field capable of holding workout identity. Primary `BLOCKER_FOR_STEP_C`: whether `EASY_WITH_STRIDES`/`EASY_SHAKEOUT` should be created. Primary `BLOCKER_FOR_STEP_B_SUBTOPIC`: exposure-bounds/compression-extension placeholders for the 2 EASY-family stages specifically (does not block other Step B subtopics).

## 11. 4F.6B readiness

```text
Is this a BLOCKER_FOR_4F6B?
YES
```

Exact unresolved prerequisite: the role-to-workout-key binding contract/mechanism (currently `CONTRACT_ABSENT`), and — contingent on that decision — EASY-family stage content for BUILD/RACE_SPECIFIC phases, plus ownership assignment per Step A.1's A1-Q08 finding (bounded-context split is only "suggested," not finalized).

## 12. Step B readiness

```text
STEP_B_CAN_BEGIN
```

Step B can begin with this exact scope:
- Evidence-check exposure bounds (3–6 / 1–2) for `FOUNDATION_EASY_BASE` and `TAPER_SHARPEN`
- Evidence-check compression/extension classifications for both EASY-family stages
- Evidence-check phase-level duration bounds for all 4 phases (independent of role binding)
- Evidence-check the remaining 5 non-EASY stages (already in Step A's scope, unaffected by this audit)
- Evidence-check `EASY_STANDARD`'s prescription-mode/accounting-mode shape generally, independent of role binding

The following topics are excluded and deferred to Step C:
- Role-to-workout-key binding mechanism design
- Whether/how `EASY_SUPPORT` formally binds to `EASY_STANDARD`, and how BUILD/RACE_SPECIFIC weeks get populated
- Whether `EASY_WITH_STRIDES`/`EASY_SHAKEOUT` should become new catalog artifacts
- Bounded-context ownership of the binding mechanism
- LONG_RUN role binding (already deferred by Step A.1, out of scope here)

## 13. Validation results

- `plan-catalog`: build 0 errors/0 warnings; full suite `dotnet test PlanCatalog.sln -c Release --no-build` → 335/335 passing.
- `backend`: build 0 errors/0 warnings; `dotnet test RunningApp.sln -c Release --no-build --filter "FullyQualifiedName~Phase4F5|...MapperTests|...ResolverTests|...MaterializerTests|...OrchestratorTests|...ValidatorTests"` → 131/131 passing.

## 14. Files inspected

`ten-k-workout-progression.v5.json`, `easy-standard.v4.json`, `run-layout-4d.v2.json`, `catalog/workouts/` directory listing, repo-wide grep for `EASY_WITH_STRIDES`, `ten-k-pilot-vocabulary-decisions.md`, `workout-components-ownership-audit.md`, `Appsel_Master_Template_Catalog_Authoring_Process.md`, `PilotDomainContentAudit.cs`, `activation-readiness-risks.md`, `golden-10k-intermediate-4d-12w.v3.plandocument.json` (programmatic full walk), `CatalogPreviewGenerator.cs`, `CatalogCalendarAssignmentContracts.cs`, `CatalogRunLayoutSlots.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`, `GeneratedCatalogPlanPayload.cs`, `PlanCatalogDomainMapper.cs`, and all test files matching `EASY_SUPPORT` across both solutions.

## 15. Files created

- `plan-catalog/artifacts/audits/phase4f6-step-a2-easy-support-coverage-and-blocker-classification.json`
- `plan-catalog/artifacts/audits/phase4f6-step-a2-easy-support-coverage-and-blocker-classification.md`

## 16. Files modified

None. No production, catalog, evidence, or governance file was modified. Step A and Step A.1 artifacts untouched.

## 17. Repository state

Branch `main`, HEAD unchanged at `0c6796578f08bc1d76d96f1944a80c9075455206`. No staged changes. Unstaged/untracked state matches the pre-Step-A.2 baseline plus the two new artifacts above. No commit made.

## 18. Final conclusion

The EASY_SUPPORT scoping question is closed: the current partial coverage is a combined contract-and-content gap (not a runtime defect), with one governance dimension already documented (workout-key vocabulary) and one previously unrecorded (binding mechanism). It is a confirmed `BLOCKER_FOR_4F6B`. Step B may proceed now on the evidence-mapping questions listed above; the binding-mechanism and content-completion decisions are deferred to Step C. No further archaeology step is recommended — this closes Step A.2.
