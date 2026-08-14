# Phase 4G.5F — Dynamic Core Volume and Long-Run Prescription

## 1. Executive result

A new, dark, unwired orchestrator — `DynamicCoreVolumeAndLongRunOrchestrator` — generalizes the live 12-week weekly-volume/long-run planning step to any mathematically feasible standalone-core length (8-14 weeks for `TEN_K_MASTER v6`). It performs **zero volume/long-run calculation of its own** — the real `CatalogVolumeAndLongRunPlanner` was already fully week-count-agnostic and required no adaptation; this orchestrator is pure composition, chaining the Phase 4G.5E binding orchestrator into the real, unmodified planner, prescription-context builder, and peak-volume-band loader. **TD-VOLUME-CAP-UNENFORCED-001's closure basis was re-verified at scale — across all 7 horizons and, going beyond the task's own minimum bar, across 6 distinct runner-readiness profiles (42 combinations) — and the algebraic proof holds in every case: zero cap violations found.** This is reported as confirming evidence; the TD is not reopened. No new TD was required. No existing production file was modified beyond one dark-reachability test reconciliation (the same situation encountered in Phase 4G.5D↔4G.3B.2 and Phase 4G.5E↔4G.5D).

## 2. Reuse strategy — which approach was used and why

Per this pass's explicit instruction, `CatalogVolumeAndLongRunPlanner`/`VolumeSafetyPolicy` (`backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/`) were inspected first, before any new code was written.

**`CatalogVolumeAndLongRunPlanner.Build` is already fully generic over week count.** Its only week-count constraint is a runtime range check against `candidate.CoreCycle.MinimumWeeks`/`MaximumWeeks` — `8` and `14` for this candidate, which 8-14 already satisfies exactly; no code change was needed to reach that range. Its `ResolvePeak` reachable-peak formula already scales the peak proportionally: `nonTaperWeeks`/`transitions` are derived live from `boundPlan.Weeks.Count(w => w.PhaseKey != "TAPER")`, not a hardcoded constant, and the peak multiplier is `1 + (canonicalDefaultMultiplier - 1) * transitions / GoldenFixtureNonTaperTransitions` — proportional to whatever `transitions` actually is for the plan at hand. `BuildWeeklyPlan`'s linear interpolation is likewise indexed by `nonTaperWeeks.Count`, not a fixed 12.

**No dark-reachability test constrains `CatalogVolumeAndLongRunPlanner`'s call sites** — it is already `CatalogPreviewGenerator`'s live call site (line 680 of `CatalogPreviewGenerator.cs`), data-gated unreachable only because the pilot candidate's catalog `status` is `DRAFT`, exactly the same situation Phase 4G.5E found for `CatalogWorkoutBinder`.

Given both facts, the same reuse strategy as Phase 4G.5D and 4G.5E applies: **call the real planner's real public entry point directly** (`new CatalogVolumeAndLongRunPlanner().Build(...)` / an injected `ICatalogVolumeAndLongRunPlanner`), with no adaptation, wrapper, or re-derivation of the reachable-peak formula, share-cap logic, or taper-multiplier behavior. `VolumeSafetyPolicy.Default` is consumed unmodified — no new constants were introduced.

## 3. Re-verification of TD-VOLUME-CAP-UNENFORCED-001's closure basis at scale

### 3.1 What the TD's closure already covered

TD-VOLUME-CAP-UNENFORCED-001's closure note (Phase 4G.3B.7/4G.3B.7.1) proved algebraically that the per-transition increase ratio required to reach the catalog peak is `(canonicalDefaultMultiplier - 1) / GoldenFixtureNonTaperTransitions = (38/24 - 1) / 10 ≈ 5.83%`, independent of starting volume and transitions (the `transitions` term cancels algebraically), and stays below `HardMaxWeeklyIncreaseRatio` (8%) by construction. It was empirically confirmed for all 7 target week counts (8-14) — but **only against one fixed runner-readiness fixture** (`RecentWeeklyVolumeKm=24, RecentLongestRunKm=9, RecentRunsPerWeek=4`, the golden-fixture-matching pilot profile), via `VolumeProgressionVerifierTests.Verify_RealNonPilotWeekCounts_ReportsActualOutcome_NotAssumedPass`.

### 3.2 What this pass re-verified

Two things, per this pass's own required steps:

1. **Re-ran the existing `VolumeProgressionVerifierTests` suite fresh** (not assumed still-passing) — all 8 tests pass, including the 6-case theory covering 8/9/10/11/13/14 weeks plus the separate 12-week fact, every one reporting `Outcome = Pass`, zero findings.
2. **Extended the verification to 6 distinct runner-readiness profiles**, not just the one fixture the existing suite used — going beyond this pass's own minimum instruction ("re-run the same check... for all 7 horizons"), since a different starting volume changes `startingVolumeKm` in the ratio formula, and this pass's own required test matrix demanded multiple runner profiles anyway. Profiles: low-volume intermediate (`14/6/3`), the existing pilot profile (`24/9/4`), higher-volume intermediate (`35/14/5`), missing recent long run (`24/null/4`), missing recent volume (`null/9/4`), explicit zero evidence (`0/0/0`).

**Result: 7 week counts × 6 profiles = 42 combinations, run through the real, unmodified `CatalogVolumeAndLongRunPlanner` → `VolumeProgressionVerifier.Verify` (also real, unmodified, called directly — not re-derived), against the real `TEN_K__4D__INTERMEDIATE` candidate. Zero `HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` violations in any combination.** Every combination reports `VolumeProgressionOutcome.Pass` and an empty `Findings` list — confirmed by `PlanAsync_RealCandidate_ProducesValidPlan_NoCapViolations_TaperRemainsLowerLoad` (42 cases).

### 3.3 Conclusion — confirming evidence, TD not reopened

**The proof holds for all 7 horizons, and additionally across every runner profile tested.** This is stated explicitly as confirming evidence, per this pass's own instruction: `TD-VOLUME-CAP-UNENFORCED-001` remains `CLOSED`, re-cited (not reopened). Its own `currentStateClarification` field and the risk file's "Current-state clarifications" section were both updated (append-only) to record this broader empirical basis — see Section 8. No decision-audit-level negative finding exists to report; this pass did not encounter, and therefore did not need to flag or silently work around, any horizon or profile where the algebraic proof failed to generalize.

**Scope caveat, unchanged from the original closure:** this remains scoped to `TEN_K_MASTER v6`'s exact constants (`GoldenFixtureStartingVolumeKm=24`, `GoldenFixtureResolvedPeakKm=38`, `GoldenFixtureNonTaperTransitions=10`, `PEAK_VOLUME_BANDS_V1`'s 30-42km band for this candidate). A future candidate or catalog revision with different constants would still require re-running this analysis — this pass does not claim universal applicability beyond the current candidate.

## 4. Required design principle — confirmed, not implemented anew

"Do not force every horizon to reach the same 12-week peak" was already true of the real planner before this pass — `ResolvePeak`'s transition-proportional scaling naturally produces a lower peak for compressed horizons and a higher peak for extended horizons. Verified explicitly: `PlanAsync_CompressedHorizon_PeakIsNotForcedToCanonicalTwelveWeekPeak` (8-11 weeks) confirms each compressed horizon's resolved peak is `<=` the 12-week canonical peak for the same starting profile, never forced up to match it.

## 5. Required behavior — verified per horizon band

**8-11 weeks (compressed):** `PlanAsync_RealCandidate_ProducesValidPlan_NoCapViolations_TaperRemainsLowerLoad` confirms, for every profile: no cap violation (progression is not artificially aggressive — the real, unmodified `HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` checks via `VolumeProgressionVerifier` pass); every planned value is positive and non-null (weekly caps fail closed via the planner's own existing exceptions — `CatalogVolumeUnreachablePeakRuleException` etc. — if infeasible, none were thrown in this matrix); long-run growth stays within the policy's own `LongRunHardCapShare` (40%) of that week's volume; `PlanAsync_CompressedHorizon_PeakIsNotForcedToCanonicalTwelveWeekPeak` confirms the target peak is reduced (not forced to canonical) when the horizon is shorter; the taper week's volume is confirmed strictly below the resolved peak in every case (taper remains meaningful).

**12 weeks:** preserved exactly — see Section 6's byte/value-level regression.

**13-14 weeks (extended):** `PlanAsync_ExtendedHorizon_NoEndlessGrowth_EveryWeekCarriesExplicitDecisionReason` confirms every week's planned volume is bounded at or below the resolved peak (no endless increase — the linear interpolation is capped at `peak.SelectedPeakKm` by construction, and `VolumeProgressionVerifier` independently re-confirms no transition exceeds the hard cap); every week carries a non-blank `DecisionReason` (explicit progression/maintenance semantics, never a silent/undocumented week — e.g. `"technical_linear_interpolation_from_start_to_selected_reachable_peak_across_non_taper_weeks"` for ordinary progression weeks); long-run values remain within the same 40% hard-cap share as every other horizon.

## 6. Required output

This pass reuses the existing, already-complete output contract types (`CatalogVolumeContracts.cs`, unmodified) rather than inventing new ones — every field this task's spec named already exists:

| Task vocabulary | Existing field |
|---|---|
| PlannedWeeklyVolumeKm | `CatalogWeeklyVolumeWeek.PlannedWeeklyVolumeKm` |
| PlannedLongRunKm | `CatalogLongRunWeek.PlannedLongRunDistanceKm` |
| VolumeDecision | `CatalogWeeklyVolumePlan.StartingVolumeDecision`/`ReachablePeakDecision`/`TaperVolumeDecision` |
| LongRunDecision | `CatalogLongRunWeek.LongRunAnchorSource` / `CatalogLongRunProgression.WeeklyShareDecision` |
| SafetyFindings | `CatalogVolumeValidationResult.Errors` (structural) + the separate, already-dark `VolumeProgressionVerificationResult.Findings` (transition-safety) |
| Provenance | `CatalogWeeklyVolumeWeek.Provenance` / `CatalogLongRunWeek.Provenance` (per-week) |

## 7. Test matrix

7 week counts (8-14) × 6 runner profiles = 42 combinations, plus targeted compressed/extended-horizon tests and the 12-week regression:

```text
DynamicCoreVolumeAndLongRunOrchestratorTests: 51 passed, 0 failed, 0 skipped
VolumeProgressionVerifierTests (re-run, existing, unmodified): 15 passed, 0 failed, 0 skipped
DynamicCoreWorkoutBindingOrchestratorTests (re-run after Section 8's reconciliation): 73 passed, 0 failed, 0 skipped
Combined focused run: 139 passed, 0 failed, 0 skipped
```

Every one of the 42 (week × profile) combinations confirmed: `WeeklyVolumePlan.ValidationResult.IsValid` and `LongRunProgression.ValidationResult.IsValid` both true; `VolumeProgressionVerifier.Verify(...) == Pass` with zero findings; every week's planned volume and long-run distance strictly positive; taper week's volume below the resolved peak; long-run never exceeds the 40% hard-cap share.

## 8. Required verification

**Zero production call sites**: confirmed by grep (repo-wide search for `DynamicCoreVolumeAndLongRun(Orchestrator|Context|Result)` across all four production projects, excluding the new file's own source, returned zero matches) and by an executable structural test, `DarkReachability_NoProductionCallSite`, matching this session's established pattern. A companion test, `DarkReachability_NoDiRegistration`, confirms `IDynamicCoreVolumeAndLongRunOrchestrator` is never referenced in `RunningApp.Api`. `CatalogPreviewGenerator.cs`'s `git diff` remains empty — confirmed untouched.

**Byte/value-level 12-week regression**: `PlanAsync_TargetWeekCount12_MatchesExistingFixedWeekVolumePipelineExactly` builds the volume/long-run plan via the **existing, completely unmodified** fixed-week pipeline (mirroring `VolumeProgressionVerifierTests.RealVolumePlanAsync`'s own established construction, extended through the planner) and, separately, via the new `DynamicCoreVolumeAndLongRunOrchestrator` requesting `targetWeekCount=12`, then asserts full field-by-field equality of every week's `PlannedWeeklyVolumeKm`/`VolumeClassification`/`IsTaperWeek` and `PlannedLongRunDistanceKm` — a concrete comparison, not a bare claim. The existing pipeline's own output is additionally pinned to literal values (`FirstWeekVolumeKm=24`, `PeakVolumeKm=38`, matching the golden-fixture constants exactly). All assertions pass.

**Existing-test reconciliation** (mirroring the Phase 4G.5D↔4G.3B.2 and Phase 4G.5E↔4G.5D precedent): adding this new orchestrator's call to `DynamicCoreWorkoutBindingOrchestrator.BindAsync(...)` (via `IDynamicCoreWorkoutBindingOrchestrator`) made it a second reference to that Phase 4G.5E type, which broke that phase's own `DarkReachability_NoProductionCallSite` test. The test was updated, not weakened: renamed to `DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer`, with one new file-path exclusion (`DynamicCoreVolumeAndLongRunOrchestrator.cs`) and an updated doc comment explaining why — the new consumer is itself proven dark by its own two reachability tests. This is the only existing production/test file modified by this pass beyond the governance file updates in Section 9.

## 9. New findings — governance update recorded, no new TD

Per this pass's own governance requirement, a genuine new problem (an unsafe combination, a horizon where the proof fails to generalize, a new interaction with an open TD) would need a proper new `activation-readiness-risks.json`/`.md` entry. **None was found** — Section 3 confirms the proof generalizes cleanly across every tested combination. Because this pass's finding is *confirming* rather than *problematic*, and the risk file's own established convention (see the existing `currentStateClarification` fields on `TD-FOUNDATION-COMPRESSION-001`/`TD-VOLUME-CAP-UNENFORCED-001`, and the "Current-state clarifications"/"Allocation-priority verifier clarification" sections added by earlier phases) is to record broadened-scope confirming evidence as an **append-only clarification to the existing closed TD**, not a new risk entry, this pass followed that same pattern:

- `activation-readiness-risks.json`: `TD-VOLUME-CAP-UNENFORCED-001`'s `currentStateClarification` field had one new sentence appended (original text preserved unchanged) recording the 42-combination re-verification.
- `activation-readiness-risks.md`: a new "Volume-cap algebraic proof re-verification at scale (Phase 4G.5F)" section was appended after the existing "Wiring-gap addition (Phase 4G.5C)" section, mirroring the exact prose style of the prior "Allocation-priority verifier clarification (Phase 4G.5B.0)" addendum.
- No aggregate count changed (16 OPEN/CLOSED totals unaffected — no new risk was added, no existing risk's status changed).
- `TD-NOTEVALUATED-FALLBACK-001` was reviewed for a new interaction: none found — this pass's `GOAL_FEASIBILITY_IN` condition results were always `Evaluated`/`REALISTIC` in every one of the 42 combinations (readiness/volume profile is orthogonal to goal-pace eligibility), so no new NotEvaluated-fallback interaction was exercised or discovered by this pass.

`ActivationReadinessRiskParityTests`/`ActivationSafetyGateTests` re-run after both edits: **15/15 passed**, parity confirmed, file remains `DOCUMENTATION_ONLY`.

## 10. Full backend suite

```text
Run 1: Failed: 0, Passed: 1586, Skipped: 1, Total: 1587
Run 2: Failed: 0, Passed: 1586, Skipped: 1, Total: 1587
```

Both runs match exactly (+51 vs. Phase 4G.5E's `1535` baseline, matching `DynamicCoreVolumeAndLongRunOrchestratorTests`' own 51 new tests exactly — `DynamicCoreWorkoutBindingOrchestratorTests`' own test count (73) is unchanged, only its dark-reachability test was renamed).

## 11. Files changed

- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/DynamicCoreVolumeAndLongRunOrchestrator.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/DynamicCoreVolumeAndLongRunOrchestratorTests.cs` (new).
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Binding/DynamicCoreWorkoutBindingOrchestratorTests.cs` (existing file, one test updated — see Section 8; no production file modified).
- `plan-catalog/artifacts/audits/activation-readiness-risks.json` — one field appended (`TD-VOLUME-CAP-UNENFORCED-001.currentStateClarification`), append-only.
- `plan-catalog/artifacts/audits/activation-readiness-risks.md` — one new clarification section appended, append-only.
- `PHASE4G_5F_DYNAMIC_CORE_VOLUME_AND_LONG_RUN.md` (new, this document).

No production `CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`, `VolumeProgressionVerifier.cs`, `CatalogPreviewGenerator.cs`, or catalog JSON file was modified.

## 12. Stop conditions — none triggered

- Compressed plan violates caps: **not triggered** — zero violations across all compressed-horizon combinations.
- Planner requires invented safety thresholds: **not triggered** — `VolumeSafetyPolicy.Default` was consumed unmodified; no new constant was introduced.
- 14-week plan grows indefinitely: **not triggered** — bounded at the resolved peak, verified explicitly.
- 12-week output changes without approval: **not triggered** — Section 8's regression test proves exact equality.
- Dark verifier and live planner semantics conflict: **not triggered** — `VolumeProgressionVerifier` (dark) and `CatalogVolumeAndLongRunPlanner` (live) were both called directly, unmodified, and agree in every one of the 42 combinations.
- Volume-cap algebraic proof fails to generalize to some horizon and is silently worked around: **not triggered** — the proof generalized cleanly in every tested case; had it not, Section 3 would report that finding prominently rather than proceeding.
- Zero-call-site proof cannot be established: **not triggered** — established via grep and two executable tests.

## 13. Commit/push status

No file was staged. No commit or push was performed.
