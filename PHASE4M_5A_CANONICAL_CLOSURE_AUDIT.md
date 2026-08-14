# Phase 4M.5A — Canonical Reconciliation + Window Semantics + Rounding Distribution Audit

**This phase is audit/evidence only.** No production code was changed. No canonical spec file was modified. No Revision 4.2 was created. No architecture generalization was started. Nothing was committed or pushed.

## Summary of the three questions

### 1. What exactly changed from Rev4 to Rev4.1?

Full clause-by-clause diff in `REV4_TO_REV4_1_CANONICAL_RECONCILIATION.md`. In short: three substantive additions to §7 (a modified severity-ordering paragraph, a new ROUNDING PRODUCT DEFAULT subsection, a new TARGET PRESCRIPTION INFEASIBILITY subsection) plus mechanical, documentation-only follow-ons elsewhere. Both new numeric rules (the 1.5% figure and the infeasibility-Block rule) were **specified by the user directly in the 4M.4B.2C phase prompt** before I wrote them into canonical prose — but the user has not yet reviewed the *resulting spec text itself*. The severity-ordering paragraph's specific factual claim ("always rounding-only") is the one clause that depended on analysis this phase now completes (see question 3).

### 2. Is the 16-session window real/normal, and are the absolute thresholds valid for it?

**Yes, 16 sessions is real and dominant** (83.3% of 576 real windows swept across the full realistic horizon range — `LONG_HORIZON_WINDOW_DECISION_UNIT_AUDIT.md`, §B2). **No, the absolute 0/1/2/3/4+ thresholds are not proven valid for it** — the implementation's own source comment discloses the matrix is "calibrated for the current 4-session pilot... not a general formula," and at a real 16-session window, `ProgressAsPlanned` fires at just 25% completion while the "3-of-4, only Easy missing" sub-branch becomes structurally unreachable. **Classification: `WINDOW_DECISION_POLICY_NOT_DEFINED_FOR_MULTI_WEEK_WINDOWS` — DecisionRequired, not resolved.**

### 3. Why do 94/183 cases show Maintain > ProgressAsPlanned?

**Exclusively session-allocation rounding — 100% of the 94 violations, 0% correlated with any catalog-classified recovery/cutback week** (`MAINTAIN_VS_PROGRESS_ORDERING_FORENSIC_AUDIT.md`). Pre-materialization, the two anchors are identical (0 violations); the entire gap is introduced by the catalog's own discrete session-distance rounding. The distribution is tightly bounded and naturally tapering (median 0.391%, max 1.361%), not clustered at the 1.5% boundary. **Classification: `ROUNDING_TOLERANCE_WELL_SUPPORTED_AS_PRODUCT_DEFAULT`** — evidence supporting the user's freeze decision, not itself a freeze.

## Reassessed Phase 4M.5 classification

**`MULTIPLE_CANONICAL_DECISIONS_REQUIRED`** is the technically precise label for the *set* of open items, but per this phase's explicit completion standard, the accurate current status is:

```
TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_CONDITIONALLY_VERIFIED
```

The rounding question (Part C) resolved cleanly in favor of the proposed tolerance — no residual ambiguity there. The window-semantics question (Part B) did **not** resolve cleanly — it surfaced a real, quantified, unaddressed gap between the canonical spec's own documented calibration and the runtime's dominant real behavior. This alone is sufficient to keep the overall classification below full verification, independent of the governance question in Part A (which is a process/sign-off matter, not itself evidence of a defect).

## Files inspected

`NextWindowLoadDecisionPolicy.cs`, `WindowExecutionSummaryBuilder.cs`, `LongHorizonRollingInitialActivationRuntime.cs`, `LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonCompositionResolver.cs`, `RaceHorizonPolicy.cs`, `LongHorizonGeStructuralContracts.cs`, `LongHorizonGeStructuralSelector.cs`, `LongHorizonGeNumericExecutor.cs`, `LongHorizonRollingWindowActivationService.cs`, both canonical spec files (Rev4, Rev4.1).

## Audit artifacts created

1. `REV4_TO_REV4_1_CANONICAL_RECONCILIATION.md`
2. `LONG_HORIZON_WINDOW_DECISION_UNIT_AUDIT.md`
3. `maintain-vs-progress-catalog-distribution.json` (183-row full case export)
4. `MAINTAIN_VS_PROGRESS_ORDERING_FORENSIC_AUDIT.md`
5. `PHASE4M_5A_CANONICAL_CLOSURE_AUDIT.md` (this file)

## Tests/commands/results

```
dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~NextWindowLoadDecisionPolicy|FullyQualifiedName~WindowCheckpointSummaryAndDecisionTests|FullyQualifiedName~PlanAdaptationV1DecisionTests|FullyQualifiedName~MaintainNotExceedingProgressAsPlannedInvariantTests"
  → 86/86 passed

dotnet test backend/RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizon"
  → 1098/1098 passed (identical to the pre-audit 4M.5 baseline, confirming zero production behavior change)

dotnet build backend/RunningApp.sln
  → 0 warnings, 0 errors

git diff --check -- backend/RunningApp.Application backend/RunningApp.IntegrationTests
  → clean

git status --short -- backend/RunningApp.Application backend/RunningApp.IntegrationTests
  → identical to pre-4M.5A state (all modified/untracked entries pre-existing and unrelated); no new production files
```

Two temporary diagnostic fixtures (`TEMP_WindowDistributionAuditDiagnostic.cs`, `TEMP_MaintainVsProgressForensicExport.cs`) were created to gather the evidence above and deleted before this report — they never appear in the regression counts and left no trace in `git status`.

## Production files changed

**None.**

## Remaining DecisionRequired items

1. **Window decision-unit semantics** (Part B): does the user want the existing absolute 0/1/2/3/4+ matrix to apply as-is regardless of real window size (with the spec updated to say so explicitly), or does it need a window-size-aware redefinition? No normalized/percentage threshold is proposed here — this is returned to the user undecided.
2. **Canonical text review** (Part A): the user has not yet reviewed the exact Rev4.1 prose for the three `PROPOSED_FOR_USER_FREEZE` clauses (ROUNDING PRODUCT DEFAULT, TARGET PRESCRIPTION INFEASIBILITY, and the severity-ordering paragraph) even though the underlying rules/numbers were user-specified in an earlier phase prompt.

## Recommended user canonical decisions

1. Freeze the two `PROPOSED_FOR_USER_FREEZE` clauses that Part C fully supports with clean evidence (ROUNDING PRODUCT DEFAULT at 1.5%, and the severity-ordering paragraph's "always rounding-only" claim) — the forensic data leaves no open question here.
2. Decide the window-decision-unit question (item 1 above) before freezing TARGET PRESCRIPTION INFEASIBILITY's multi-window acceptance language, since that language implicitly assumes the decision-unit semantics are already settled.
3. Once both are decided, a future phase (not this one) would fold any resulting canonical text changes into a reviewed Revision 4.2 — explicitly not created here.

## Current Adaptation V1 classification

```
TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_CONDITIONALLY_VERIFIED
```

## Final classification

```
WINDOW_DECISION_POLICY_REQUIRES_DECISION
```

(Rounding is resolved in evidence, per §C5; window-decision-unit semantics is not — per the explicit instruction, the presence of even one open, unresolved item is sufficient to withhold `TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_IMPLEMENTED_AND_VERIFIED` until the user decides it.)

No commit, no push, no generalization work started, no production behavior changed.
