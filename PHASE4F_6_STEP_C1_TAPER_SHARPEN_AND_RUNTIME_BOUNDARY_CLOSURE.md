# Phase 4F.6 Pre-Implementation — Step C.1
## TAPER_SHARPEN Directive Closure, Binding Boundary Clarification, and Pre-4F.6A Checkpoint Audit

Narrow governance-closure task. No stage scheduler, workout binder, or prescription engine was implemented. No catalog value changed. No commit was made.

## 1. Final classification

**STEP_C1_BOUNDARY_CLOSED_WITH_APPEND_ONLY_CLARIFICATION**

## 2. AUD-507 exact assessment

**Exact current text (unchanged, reproduced verbatim from `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs`):**

> STEP C D-C11 RESOLUTION: retains TAPER_SHARPEN's stageKey and EASY_STANDARD workout-identity binding unchanged; no new taper workout key is introduced. Per Step B's central finding (decision D43): TAPER_SHARPEN's name implies an intensity-maintaining purpose, but its bound candidate is a plain EASY-family workout, which by itself does not fulfill Bosquet et al. (2007)'s finding that effective tapers maintain intensity while reducing volume. This decision accepts that gap for V1 and assigns its resolution to Phase 4F.7: the sharpening effect must be produced through a taper-specific prescription modifier (reducing total workload while preserving an appropriate intensity stimulus, using only components/prescription modes already allowed by EASY_STANDARD and the future prescription contract — not a generic 'faster easy pace', and not defined here). Stage context must be available to Phase 4F.7 prescription generation so TAPER_SHARPEN and ordinary EASY_STANDARD sessions do not receive identical prescriptions by accident. Implementation owner: Phase 4F.7. Evidence basis: EVIDENCE_INFORMED.

**Checklist assessment** (against the 8-point `CONCRETE_PRESCRIPTION_DIRECTIVE` test):

| # | Requirement | AUD-507 coverage |
|---|---|---|
| 1 | TAPER_SHARPEN remains an active stage | ✅ Explicit ("retains TAPER_SHARPEN's stageKey ... unchanged") |
| 2 | Workout identity remains EASY_STANDARD | ✅ Explicit |
| 3 | Prescription must be materially distinguishable from ordinary EASY_STANDARD | ⚠️ Only entailed — the entry says sessions must "not receive identical prescriptions by accident," which guards against accidental sameness but never uses an affirmative, freestanding "materially distinguishable" requirement |
| 4 | Must reduce overall workload/volume | ✅ Explicit ("reducing total workload") |
| 5 | Must preserve appropriate intensity stimulus | ✅ Explicit |
| 6 | Must stay within allowed components/prescription modes | ✅ Explicit |
| 7 | Must not be merely "make the whole run faster" | ⚠️ Addressed via "not a generic 'faster easy pace'" but doesn't foreclose other insufficiently-differentiated implementations (e.g. a negligible volume trim with no distinguishable intensity treatment) |
| 8 | Stage context must be available to the prescription layer | ✅ Explicit |

**Classification: `PARTIALLY_CONCRETE_DIRECTIVE`.** AUD-507 is strong and directly addresses 6 of 8 points unambiguously — it is **not** `DELEGATION_ONLY` (it does not merely say "Phase 4F.7 should review sharpening"; it mandates a specific effect: reduced workload + preserved intensity + allowed-components-only). But points 3 and 7 are only satisfied by entailment/caution rather than as standalone, unambiguous sentences, so it falls short of the strict `CONCRETE_PRESCRIPTION_DIRECTIVE` bar, which requires the record to *unambiguously* establish all 8 points.

## 3. TAPER_SHARPEN final directive

Confirmed, unchanged from Step C, now made explicit and unambiguous by the append-only `AUD-508`:

- **TAPER_SHARPEN remains** an active workout-progression stage.
- **Workout identity remains `EASY_STANDARD`** — no new taper workout key introduced.
- **Phase 4F.7 must produce a prescription that is affirmatively, observably materially distinguishable** from an ordinary (non-taper) `EASY_STANDARD` session — not merely "not accidentally identical." The prescription must reduce total workload/volume **and** preserve an appropriate intensity stimulus, using only components and prescription modes already allowed by `EASY_STANDARD` (schemaVersion 3; `allowedPrescriptionModes=[DISTANCE]`, `allowedDistanceAccountingModes=[EXACT_SESSION_TOTAL]` as of v4) and the future prescription contract.
- **This explicitly does NOT mean indiscriminately speeding up the entire easy run.** Also explicitly prohibited: a trivial/negligible volume trim with no distinguishable intensity treatment (technically "reduced," but not materially distinguishable); introducing a new workout key to sidestep the modifier; or deferring the training intent for later review without an enforceable effect.

## 4. Append-only governance action

**A new entry, `AUD-508`, was added.** Reason: to close the gap identified in §2 (points 3 and 7 not stated as freestanding, unambiguous requirements). `AUD-507` was **not edited, deleted, or reworded** — it remains present, verbatim, exactly as quoted in §2. `AUD-508` is explicitly titled (in its reason text) "TAPER_SHARPEN prescription directive concretized," explicitly links to `AUD-507`/`D-C11`, explicitly states it does not imply any workout-identity change, and does not touch any catalog value.

## 5. Stage-context requirement

```
Must 4F.6A preserve TAPER_SHARPEN stage identity?
YES
```

**Reasoning (verified by direct source inspection, not assumed):** every existing skeleton/materialization contract from Phase 4F.1 through 4F.5.1 (`GeneratedCatalogPlanSkeleton.cs`, `CatalogPlanSkeletonOrchestrator.cs`) uses a `StageKey` field, but this field is **explicitly documented terminology debt referring to phase granularity** (e.g. `"BUILD"`, `"TAPER"`), never the workout-progression's own fine-grained `WorkoutProgressionStageDefinition.StageKey` concept (e.g. `"TAPER_SHARPEN"`, `"GOAL_PACE_REHEARSAL"`). Confirmed directly in `CatalogStageToWeekContextFactory.Create`'s own doc comment: *"The values themselves are NOT workout-selection `stageKey`s — they remain phase-granularity identities throughout; only the C# property name differs."* This means **no field anywhere in the current dark pipeline carries the fine-grained progression stage identity at all.** Phase 4F.6A is the first phase where this concept must be introduced. If 4F.6A does not preserve it, no later phase (4F.6B, 4F.7) can recover which fine-grained stage produced a given session — Phase 4F.7 would then have no way to know a given `EASY_STANDARD` session came from `TAPER_SHARPEN` specifically, and could not apply `AUD-508`'s modifier at all.

**Minimum required context** (classified per the requested taxonomy):

| Candidate element | Classification | Rationale |
|---|---|---|
| `stageKey` (fine-grained, e.g. `TAPER_SHARPEN`) | **`REQUIRED_FOR_4F6A_OUTPUT`** | Currently absent from every skeleton contract (see reasoning above); without it, `AUD-508`'s directive is unenforceable. This is the single load-bearing requirement. |
| `stageVersion` (progression artifact version) | `DERIVABLE_LATER` | Recoverable via the already-pinned `masterTemplate` version, since each `TEN_K_MASTER` version deterministically fixes its referenced `WorkoutProgression` version (this repository's immutable-versioning discipline) — not independently required. |
| `phaseKey` | `REQUIRED_FOR_4F6A_OUTPUT` (already satisfied) | Already present today as the (confusingly-named) `StageKey`/`SourcePhaseKey` fields. |
| `weekNumber` | `REQUIRED_FOR_4F6A_OUTPUT` (already satisfied) | Already present (`StageWeekIndex`/`StageWeekCount`). |
| `structuralRole` | `REQUIRED_FOR_4F6A_OUTPUT` (already satisfied) | Already present (`CatalogRunLayoutSlots.StructuralRoles`). Note: `TAPER_SHARPEN`'s candidate is EASY-family, which under the existing `RoleCompatibleFamilies` mapping is `EASY_SUPPORT`-compatible, not `KEY_SESSION`-compatible — a tension already flagged in Step A.1/A.2 and **not re-litigated here** per this step's explicit boundary; it does not change the propagation conclusion. |
| selected workout key | `NOT_REQUIRED` | Out of 4F.6A's own scope by design (4F.6B's responsibility, §6). |
| stage decision provenance (order/candidate resolution path) | `REQUIRED_FOR_4F6A_OUTPUT` | Directly parallels 4F.6A's own listed responsibility to preserve "condition/fallback provenance." |
| runtime-condition result (e.g. `GOAL_FEASIBILITY_IN` value at scheduling time) | `TRACE_ONLY` | Phase 4F.7 uses phase + stage + workout identity + fresh user context, per the architecture in `PHASE4F_6_STEP_C_...md` — it does not need the *scheduling-time* condition snapshot as a functional input, only for audit trace. |
| fallback origin (e.g. `GOAL_PACE_REHEARSAL` vs. `CURRENT_FITNESS_SPECIFIC_REHEARSAL`) | `DERIVABLE_LATER` | Subsumed by `stageKey` preservation — the fallback IS which stageKey was ultimately assigned; no separate field needed. |
| prescription-intent key (a normalized marker distinct from `stageKey`) | `REQUIRES_LATER_CONTRACT_DESIGN` | Whether 4F.7 keys directly off `stageKey=="TAPER_SHARPEN"` or off a separate, denormalized intent enum is a genuine future design choice, not decided here. |
| taper/sharpen marker (explicit boolean/enum) | `DERIVABLE_LATER` (primary), `REQUIRES_LATER_CONTRACT_DESIGN` (whether to also denormalize it) | Derivable from `stageKey` alone; adding a redundant explicit marker for robustness/readability is a legitimate future contract decision, not required now. |
| source artifact identity/version | `DERIVABLE_LATER` | Same reasoning as `stageVersion`. |

Per the task's own instruction, this step does **not** design the DTO/contract that carries `stageKey` forward, and does **not** require the scheduler to understand actual taper pace, segments, repetitions, or recovery — only that the fine-grained stage identity itself must not be lost.

## 6. Responsibility matrix

| Responsibility | 4F.6A | 4F.6B | 4F.7 |
|---|:---:|:---:|:---:|
| Stage-to-week allocation | ✅ | | |
| `EASY_SUPPORT` fixed binding | | ✅ | |
| `LONG_RUN` fixed binding | | ✅ | |
| `KEY_SESSION` candidate binding | | ✅ (binds the candidate the assigned stage produced) | |
| Eligible-set validation | | ✅ | |
| Multi-workout selection policy (future, not V1) | | ✅ (when/if activated) | |
| Taper prescription | | | ✅ |
| Pace/distance/dosage | | | ✅ |

4F.6A additionally: preserves deterministic ordering, condition/fallback provenance, and (per §5) the fine-grained progression `stageKey`. 4F.6A explicitly does **not** select `EASY_STANDARD`, select `LONG_RUN_STANDARD`, bind workout definitions to roles, or perform prescription. 4F.6B explicitly does **not** design a new selection-policy schema for V1 (only resolves the two already-accepted fixed defaults plus the stage-controlled `KEY_SESSION` candidate) and does **not** perform prescription.

## 7. Eligible-list ownership

```
ELIGIBLE_LIST_SELECTION_IS_4F6B_RESPONSIBILITY
```

(Matches the expected classification `4F6B_RESPONSIBILITY` from the task's own verification framework.)

**Clarification, tested rather than assumed:** for V1, `EASY_SUPPORT` and `LONG_RUN` each have **one formally accepted default workout identity** (`AUD-503`/`AUD-504`, D-C07/D-C08) — the binder **resolves an explicit default**, it does not *infer* the choice merely because the eligible set (`INTERMEDIATE_MODIFIER`'s `eligibleWorkouts`) happens to contain exactly one family-compatible item today. This distinction matters: `EligibleWorkouts` is a `Set`/`List` with no ordering semantics (confirmed in Step A.1, A1-Q03) — a future implementation that silently did "pick the first item" or "if count==1, treat uniqueness as the policy" would be indistinguishable from the correct behavior *today*, but would silently break the moment a second EASY-family or LONG_RUN-family workout is added to the eligible set in a future catalog version (e.g. if `EASY_SHAKEOUT`/`EASY_WITH_STRIDES` are ever added per `TD-EASY-WORKOUT-REGISTRY-001`'s first closure option). This is exactly why D-C10 (Step C, not reopened here) requires the future binding contract to model V1 fixed defaults **explicitly** rather than rely on list position or accidental uniqueness, and why any future multi-workout role requires an explicit, versioned selection policy before activation.

## 8. 4F.6A blocker result

```
NO_4F6A_BLOCKER_BUT_TRACE_REQUIREMENT_MUST_BE_RECORDED
```

**Exact reasoning:** a missing exact prescription algorithm (segments, pace, repetitions, recovery) is not a 4F.6A blocker — those are legitimately out of scope for stage scheduling and remain Phase 4F.7 design details. However, **failure to preserve the assigned fine-grained stage identity (`stageKey`) would become a genuine 4F.6A design blocker**, because Phase 4F.7 would then have no way to fulfill `AUD-508`'s directive at all — there would be no signal distinguishing a `TAPER_SHARPEN`-sourced `EASY_STANDARD` session from an ordinary one. Since this requirement is now explicitly recorded (§5) *before* Phase 4F.6A's design begins, it does not currently block 4F.6A — but it **must** be honored by that design, and its omission there would immediately become a blocker discovered too late (at Phase 4F.7). Hence: no blocker now, but the trace/propagation requirement must be recorded and honored, which is exactly what this document does.

## 9. Files inspected

`PilotDomainContentAudit.cs` (full `AUD-507` entry + surrounding Step C block); `PHASE4F_6_STEP_C_V1_PILOT_WORKOUT_AND_BINDING_DECISIONS.md`; `phase4f6-step-b-training-science-evidence-mapping.json`/`.md` (decisions D43/D51); current `ten-k-workout-progression.v5.json` (`TAPER_SHARPEN` stage); current `easy-standard.v4.json`; `GeneratedCatalogPlanSkeleton.cs` (Phase 4F.2 skeleton contracts); `CatalogPlanSkeletonOrchestrator.cs` (Phase 4F.3 orchestration + `CatalogStageToWeekContextFactory`); `CatalogCalendarAssignmentContracts.cs` (Phase 4F.5 dated-skeleton contracts); `activation-readiness-risks.json`/`.md`; `ContentDecisionStatus.cs`; `PilotDomainContentAuditTests.cs`; `ActivationSafetyGateTests.cs`.

## 10. Files created

- `PHASE4F_6_STEP_C1_TAPER_SHARPEN_AND_RUNTIME_BOUNDARY_CLOSURE.md` (this document)

## 11. Files modified

- `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` — one new append-only entry, `AUD-508`. `AUD-507` and every other prior entry unchanged.

No new activation risk was created — direct inspection found no distinct risk beyond what `TD-EASY-WORKOUT-REGISTRY-001` (Step C, D-C13) already represents; this closure is a governance clarification, not a new risk.

## 12. Build and test results

- `plan-catalog`: `dotnet build PlanCatalog.sln -c Release` → 0 errors, 0 warnings. `dotnet test PlanCatalog.sln -c Release --no-build` → **335/335 passing**, including `PilotDomainContentAuditTests.AllEntryIds_AreUnique` and all `ActivationSafetyGateTests` (the literal-string mechanical-consumption guard was specifically re-verified).
- `backend`: `dotnet build RunningApp.sln -c Release` → 0 errors, 0 warnings. `dotnet test RunningApp.sln -c Release --no-build` (full suite) → **628/628 passing**.
- Incidental generated-artifact drift (the recurring `ten-k-pilot-domain-decision-audit.{json,md}` timestamp regeneration and backend nuget-cache files, a known side effect of running these test suites) was detected and reverted via `git checkout --`, consistent with every prior phase in this session.

## 13. Proposed checkpoint plan

No commit was executed. The following is a **proposal only**.

### Checkpoint 1 — Phase 4F.4 + 4F.5 + 4F.5.1 (combined; see note)
- **Proposed commit title**: `Phase 4F.4/4F.5/4F.5.1: dark skeleton, calendar assignment, and validator wiring`
- **Final classification**: 4F.4 = accepted dark wiring; 4F.5 = `VERIFIED_WITH_MINOR_GAPS` (per the independent audit); 4F.5.1 = `VERIFIED_COMPLETE`
- **Exact files**:
  - Modified: `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`, `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/CatalogPreviewGeneratorTests.cs`, `.../Phase4F4ConfirmAndLegacyRegressionTests.cs`, `.../Phase4F4DarkSkeletonWiringTests.cs`
  - New (4F.5): `CatalogCalendarAssignmentContracts.cs`, `CatalogCalendarAssignmentExceptions.cs`, `CatalogPreferredDayAdapter.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `CatalogCalendarAssignmentFixtures.cs`, `CatalogWeekSkeletonCalendarMaterializerTests.cs`, `Phase4F5DarkCalendarWiringTests.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`, `DatedGeneratedCatalogPlanSkeletonValidatorTests.cs`, `PHASE4F_5_CALENDAR_DAY_ASSIGNMENT_POLICY_AND_MATERIALIZER.md`
  - New (4F.5.1): `Phase4F5_1ProductionValidatorWiringTests.cs`, `PHASE4F_5_1_PRODUCTION_VALIDATOR_WIRING.md`
- **Dependency on previous checkpoint**: none (first proposed checkpoint in this plan; builds on the last real commit `0c6796578f08bc1d76d96f1944a80c9075455206`).
- **Mixed-attribution files**: **`CatalogPreviewGenerator.cs` and its 3 modified test files contain squashed changes from all three phases (4F.4 wired the skeleton orchestrator; 4F.5 added the calendar materializer + `PreferredDays`/`LongRunDay` fixture fields; 4F.5.1 added the validator invocation and removed the dead `CatalogCalendarAssignmentFailedException`).** No intermediate commit or diff exists to mechanically separate these three phases' contributions within these 4 files — they were never committed individually.
- **Generated artifacts**: none tracked in this set (build-artifact `bin/`/`obj/` directories excluded, per established convention).
- **Files that must not be included**: `baseline_tmp/` (explicitly excluded per standing instruction), `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md` (explicitly excluded per standing instruction), any `bin/`/`obj/` path.
- **Confidence**: HIGH for the *new* files' phase attribution; **LOW for the 4 modified files' phase-level attribution** (see below).

  **`CHECKPOINT_SEPARATION_REQUIRES_EXPLICIT_USER_APPROVAL`** applies specifically to the 4 modified files above if the user wants Phase 4F.4 / 4F.5 / 4F.5.1 as three genuinely separate historical commits. Reason: separating them would require patch-level (hunk-by-hunk) staging (`git add -p`) to reconstruct which lines belong to which phase, since all three phases' edits to the same files are currently flattened together in the working tree with no earlier commit boundary. This step does not perform patch surgery, per its own instructions. The combined single-commit proposal above avoids this problem entirely and is the safe default; if genuinely separate historical commits are wanted, that requires an explicit follow-up instruction authorizing patch surgery.

### Checkpoint 2 — Step A (v10 Catalog Audit)
- **Proposed commit title**: `Phase 4F.6 Step A: CATALOG_STATE_RESOLVED_WITH_GAPS`
- **Exact files**: `plan-catalog/artifacts/audits/phase4f6-step-a-v10-catalog-audit.json`, `.md`
- **Dependency**: none (read-only audit, no source dependency)
- **Mixed-attribution files**: none
- **Generated artifacts**: none
- **Files that must not be included**: none applicable
- **Confidence**: HIGH

### Checkpoint 3 — Step A.1 (Role Ownership and Gap Clarification)
- **Proposed commit title**: `Phase 4F.6 Step A.1: ROLE_OWNERSHIP_RESOLVED_WITH_DECISIONS_REQUIRED`
- **Exact files**: `phase4f6-step-a1-role-ownership-and-gap-clarification.json`, `.md`
- **Dependency**: logically follows Step A; no file-level git dependency
- **Mixed-attribution files**: none
- **Generated artifacts**: none
- **Files that must not be included**: none applicable
- **Confidence**: HIGH

### Checkpoint 4 — Step A.2 (EASY_SUPPORT Coverage and Blocker Classification)
- **Proposed commit title**: `Phase 4F.6 Step A.2: EASY_SUPPORT_SCOPE_RESOLVED_WITH_4F6B_BLOCKER`
- **Exact files**: `phase4f6-step-a2-easy-support-coverage-and-blocker-classification.json`, `.md`
- **Dependency**: logically follows Step A.1; no file-level git dependency
- **Mixed-attribution files**: none
- **Generated artifacts**: none
- **Files that must not be included**: none applicable
- **Confidence**: HIGH

### Checkpoint 5 — Step B (Training-Science Evidence Mapping)
- **Proposed commit title**: `Phase 4F.6 Step B: EVIDENCE_MAPPING_COMPLETE_WITH_GAPS`
- **Exact files**: `phase4f6-step-b-training-science-evidence-mapping.json`, `.md`
- **Dependency**: logically follows Step A.2; no file-level git dependency
- **Mixed-attribution files**: none
- **Generated artifacts**: none
- **Files that must not be included**: none applicable
- **Confidence**: HIGH

### Checkpoint 6 — Step C (V1 Pilot Workout Progression, Fixed Role Binding, and Governance Decisions)
- **Proposed commit title**: `Phase 4F.6 Step C: STEP_C_DECISIONS_FORMALIZED (AUD-500..AUD-507)`
- **Exact files**: `PilotDomainContentAudit.cs` (AUD-500 through AUD-507 only — see mixed-attribution note), `activation-readiness-risks.json`, `activation-readiness-risks.md`, `PHASE4F_6_STEP_C_V1_PILOT_WORKOUT_AND_BINDING_DECISIONS.md`
- **Dependency**: logically follows Step B; no file-level git dependency on Checkpoints 2-5
- **Mixed-attribution files**: **`PilotDomainContentAudit.cs` now also contains `AUD-508` (Step C.1), added in this same working-tree session before Step C's own additions (AUD-500-507) were ever committed.** No commit boundary exists between them, so a clean "Step C only" vs. "Step C.1 only" split of this file requires either (a) bundling `AUD-508` into this same commit (recommended — both are governance additions to the same audit registry, entirely reasonable to bundle), or (b) patch-level surgery to isolate `AUD-508` into Checkpoint 7 alone, which this step does not perform.
- **Generated artifacts**: none
- **Files that must not be included**: none applicable
- **Confidence**: MEDIUM (content attribution is exact and known, but the file-level split from Checkpoint 7 requires either the recommended bundling or explicit-approval surgery)

### Checkpoint 7 — Step C.1 (TAPER_SHARPEN Directive Closure and Runtime Boundary Closure)
- **Proposed commit title**: `Phase 4F.6 Step C.1: STEP_C1_BOUNDARY_CLOSED_WITH_APPEND_ONLY_CLARIFICATION (AUD-508)`
- **Exact files**: `PHASE4F_6_STEP_C1_TAPER_SHARPEN_AND_RUNTIME_BOUNDARY_CLOSURE.md`, and `AUD-508`'s portion of `PilotDomainContentAudit.cs` **if and only if** Checkpoint 6's file is patch-split; otherwise `AUD-508` ships as part of Checkpoint 6 and this checkpoint contains only the new document.
- **Dependency**: depends on Checkpoint 6 (references `AUD-507`, `D-C11`, and the Step C document by name)
- **Mixed-attribution files**: same `PilotDomainContentAudit.cs` note as Checkpoint 6
- **Generated artifacts**: none
- **Files that must not be included**: none applicable
- **Confidence**: HIGH for the new document; MEDIUM for the `PilotDomainContentAudit.cs` split (same caveat as Checkpoint 6)

**Recommended default**: bundle `AUD-508` with Checkpoint 6 (Step C) as a single `PilotDomainContentAudit.cs` commit covering `AUD-500`-`AUD-508`, and let Checkpoint 7 contain only the new Step C.1 document. This avoids all patch surgery and keeps the audit-registry file's git history simple (one commit touching it in this pass), while both steps' narrative content remains fully attributable via each step's own dedicated `.md` document. If the user specifically wants `AUD-508` isolated into its own commit, that requires an explicit follow-up instruction, since it means patch-level staging.

## 14. Commit status

```
NO_COMMIT_PERFORMED
```

## 15. Repository state

Branch `main`. HEAD unchanged at `0c6796578f08bc1d76d96f1944a80c9075455206`. No staged files. Unstaged (tracked, modified): `CatalogPreviewGenerator.cs` + 3 test files (backend), `PilotDomainContentAudit.cs`, `activation-readiness-risks.json`, `activation-readiness-risks.md`. Untracked: all Phase 4F.5/4F.5.1 new files, `baseline_tmp/` (excluded per standing instruction), `PHASE4E_2_DEV_DATABASE_MIGRATION_APPLICATION_AND_BASELINE_VERIFICATION.md` (excluded per standing instruction), all Step A/A.1/A.2/B/C artifacts, and this step's new document. No unexpected files. No commit made.

## 16. Next-step readiness

```
READY_FOR_4F6A_WITH_RECORDED_TRACE_REQUIREMENT
```

## 17. Final conclusion

Step C — including this closure pass — is now fully closed. `AUD-507` remains exactly as Step C recorded it; `AUD-508` closes the one genuine interpretive gap (materially-distinguishable-prescription language) without reopening, editing, or superseding it. The next agent may proceed to Phase 4F.6A stage-scheduler design **without** reopening `TAPER_SHARPEN`'s stage/workout identity, the fixed V1 `EASY_SUPPORT`/`LONG_RUN` role bindings, or Phase 4F.7's prescription ownership — but **must** honor the one recorded, non-optional trace requirement: the fine-grained workout-progression `stageKey` (not the phase-granularity `StageKey` already present in the Phase 4F.1-4F.5.1 contracts) must be threaded through whatever output Phase 4F.6A produces, or Phase 4F.7 will be unable to fulfill `AUD-508`'s directive. This is the sole condition attached to the `READY_FOR_4F6A_WITH_RECORDED_TRACE_REQUIREMENT` verdict.
