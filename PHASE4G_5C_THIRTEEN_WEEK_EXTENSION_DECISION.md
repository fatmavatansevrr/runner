# Phase 4G.5C — Thirteen-Week Core Extension Decision Finalization

## 1. Executive result

The approved Foundation-first 13-week extension policy is **evidence-informed, generically encoded, deterministic, and executable** under `AllocationOrderCorrectnessVerifier`'s approved-priority mechanism, and is **fully compatible with the current workout progression catalog**. No public behavior changed: 13 weeks (and 14) remain HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`; 12 weeks remains HTTP 200. No new horizon was activated. No `targetWeeks == 13` special-case branch exists anywhere in production code. All work in this pass was read-only validation plus this decision record — no production or catalog file was modified.

## 2. HEAD and working-tree attribution

HEAD is unchanged throughout this pass. This pass made **zero** file modifications; the working tree's pre-existing modified/untracked state (from Phase 4G.5A, 4G.5B.0, 4G.5B.0A, and earlier phases) is attributed to those prior passes, not to 4G.5C. See Section 18/19 for the exact file inventory.

## 3. Phase 4G.5A validation (independent)

Re-verified from source, not by re-running 4G.5A's own report claims verbatim:

- `CoreHorizonClassifier.Classify` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs`) is a pure, static, day-accurate function of `(StartDate, RaceDate, MinCoreWeeks, PreferredCoreWeeks, MaxCoreWeeks)`. No mutable state, no I/O, no DI dependency.
- Confirmed via `grep` that `CoreHorizonClassifier`/`CoreHorizonDecision`/`CoreHorizonMode` have **no production consumer** anywhere outside the classifier's own file — the only non-classifier reference is a single test in `PreparationRunwayContractsTests.cs` (`DarkReachability_ClassifierVocabularyIsAllowedWithoutContractConsumption`), which exists specifically to prove the classifier remains dark and that its `PreparationRunwayPlusCore` enum vocabulary is not contract consumption. No endpoint, controller, DI registration, planner, allocator, or materializer references it.
- Re-ran `CoreHorizonClassifierTests` directly: **22/22 passed**, covering the full boundary table (7/8/11/12/13/14/15/20 weeks, exact and partial-day variants) and the dark-reachability adversarial test.
- Confirmed independently that `RaceHorizonPolicy` (the live policy actually governing HTTP responses) is a textually and structurally separate type; `CoreHorizonClassifier.cs` does not read, import, or duplicate any of its constants.

**Result: `PHASE_4G_5A_VALIDATION = CONFIRMED`.**

## 4. Phase 4G.5B validation (independent)

`AllocationOrderCorrectnessVerifier.cs` (governance-labeled "Phase 4G.5B.0" via its own test file's `GovernanceSource` constant, though no standalone `PHASE4G_5B_0_*.md` report exists in the repository — this gap was already surfaced in the prior cross-phase audit and is not re-litigated here) was independently re-read in full:

- `ApprovedAllocationPriorityPolicy.FromCandidate` builds the approved-priority policy generically from the real candidate's `CompressionPriority`/`ExtensionPriority` fields — no hardcoded phase list, no week-count parameter.
- `AllocationOrderCorrectnessVerifier.Verify(allocation, policy)` independently re-derives the expected priority-ordered allocation (`AllocateByApprovedPriority`) and compares it against the real allocator's actual output — this is a genuine cross-check, not a pass-through.
- Re-ran `AllocationOrderCorrectnessVerifierTests`: **12/12 passed**, including the exact 12/13/14-week theory cases, the 9/10/11-week compression cases, the no-policy-supplied `DecisionRequired` case, three `Invalid` fail-closed cases (duplicate/missing/unknown priority), a synthetic ALPHA/BETA generic-order-dependence proof, and a bounds-violation fail-closed case.
- `PreparationRunwayContractsTests` (52 tests, including the 4G.5B.0A dark-reachability reconciliation) re-run: **52/52 passed**.

**Result: `PHASE_4G_5B_VALIDATION = CONFIRMED`.**

## 5. Classifier/allocator boundary finding

Part B1 of this pass's own instructions expected: *"Confirm the allocator consumes `CoreHorizonDecision`. Verify it does not independently classify horizons using its own week-range conditions."*

A repo-wide search for `CoreHorizonDecision` and `CoreHorizonMode` usage outside the classifier's own file found **zero production consumers of either type**, including inside `CatalogPhaseAllocation.cs` (the real `CatalogPhaseAllocationResolver`). This means the literal expectation — that the allocator mechanically consumes a `CoreHorizonDecision` value — **does not hold in the current code**.

This is reported honestly rather than glossed over. On inspection, it does not constitute `CLASSIFIER_ALLOCATOR_BOUNDARY_BROKEN` (an explicit stop condition), because nothing is broken, contradictory, or silently duplicated:

- `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` is unchanged since Phase 4G.3B.2, takes `targetWeekCount` directly, and derives its own `AllocationMode` (`Compression`/`Preferred`/`Extension`) purely from `targetWeekCount - sumPreferredWeeks` — a week-count-only concept, distinct from and not in conflict with `CoreHorizonMode`'s day-accurate composition classification.
- `CoreHorizonClassifier` was explicitly designed in Phase 4G.5A to have zero production call sites, DI registration, or live routing reference (confirmed in Section 3).
- No component silently duplicates the other's decision logic: the allocator does not re-derive day/date-based composition classification, and the classifier does not distribute phase weeks.

**Result: `CLASSIFIER_ALLOCATOR_BOUNDARY = CONFIRMED_WITH_ARCHITECTURAL_NOTE`** — the two components are functionally independent and non-contradictory, but not mechanically wired together; no orchestrator currently connects classifier output to allocator input, consistent with every prior phase's "dark, unwired" pattern. This is a documented gap for a future wiring phase, not a defect in either component's own correctness.

## 6. Canonical phase constraints (re-confirmed)

Re-read `plan-catalog/catalog/templates/ten-k-master.v6.json` directly (unchanged from all prior phases):

| Phase | Min | Preferred | Max | Compression priority | Extension priority |
|---|---|---|---|---|---|
| FOUNDATION | 2 | 3 | 4 | 1 | 1 |
| BUILD | 3 | 4 | 5 | 2 | 2 |
| RACE_SPECIFIC | 2 | 4 | 4 | 3 | 3 |
| TAPER | 1 | 1 | 1 | 4 | 4 |

`coreCycle`: minimum=8, default=12, maximum=14. All values unchanged from every prior phase's re-confirmation.

## 7. Governance evidence review

`TD-ALLOCATION-PRIORITY-001` (`plan-catalog/artifacts/audits/activation-readiness-risks.md`) is **CLOSED**. Its closure note (Phase 4G.3B.8/4G.3B.8.1) explicitly states: only `targetWeeks=13` is genuinely extension-order-dependent; Foundation-first extension priority is supported by independent coaching-literature evidence (Joe Friel and Stephen Seiler via Roadman Cycling, corroborated by Triathlete.com/COROS) that base/foundation-phase duration is elastic while build-phase duration is comparatively fixed; classified `EVIDENCE_INFORMED` (not `EVIDENCE_BACKED` — coaching-system consensus, not peer-reviewed RCT literature); no catalog data change was required since `compressionPriority`/`extensionPriority` were already `1/2/3/4`. A later addendum ("Allocation-priority verifier clarification (Phase 4G.5B.0)") confirms: *"the 13-week allocation remains theoretically order-dependent, but it is executable and deterministic under the approved Foundation-first extension priority."*

**Evidence-strength classification: `APPROVED_PRODUCT_DEFAULT` supported by `EVIDENCE_INFORMED` coaching literature** — matches the expected classification. Aggregate TD state confirmed unchanged: 16 total, 9 OPEN, 7 CLOSED.

## 8. Foundation-first rationale (scoped)

Scoped strictly to the `TEN_K_MASTER v6` / `TEN_K__4D__INTERMEDIATE` candidate family, not a universal claim: FOUNDATION's role is aerobic base-building, which coaching literature treats as the most elastic phase; BUILD's volume-progression role is comparatively fixed in duration; RACE_SPECIFIC is already at its own maximum (4/4) in the preferred 12-week plan, leaving zero extension headroom; TAPER is fixed at 1 week and `isCompressionProtected`. This produces a rationale that is internally consistent with the catalog's own `compressionPriority`/`extensionPriority = 1/2/3/4` ordering without inventing any new coaching claim beyond what was already evidenced in Section 7.

## 9. Generic policy encoding confirmation

A targeted grep for literal `13` inside `AllocationOrderCorrectnessVerifier.cs`, `CoreHorizonClassifier.cs`, and `CatalogPhaseAllocation.cs` (production sources, tests excluded) found exactly one match: a doc comment in `RaceCoreSupportRegistry.cs` listing example week counts (`9, 10, 11, 13`) for illustrative purposes only — not a branch, condition, or comparison. No `targetWeeks == 13` (or any other week-count-literal) branch exists in the allocator or verifier. The approved-priority mechanism operates entirely on `CompressionPriority`/`ExtensionPriority` ordinals read from the candidate — the same mechanism was independently proven generic in Section 4 via the synthetic ALPHA/BETA test, which uses phase keys that do not exist in any real catalog.

**Result: `NO_TARGET_SPECIFIC_BRANCH_CONFIRMED`.**

## 10. Thirteen-week result

Re-ran (Section 4): `Resolve(candidate, 13)` → `FOUNDATION=4, BUILD=4, RACE_SPECIFIC=4, TAPER=1`. Matches the expected allocation matrix exactly.

## 11. Thirteen-week order-independence status

`IsOrderIndependent = false` (confirmed by test). `RACE_SPECIFIC` has zero extension headroom (already at max=4 in the preferred allocation), so the 13th added week must come from either FOUNDATION or BUILD — the choice is genuinely order-dependent, not structurally forced. `UsesApprovedPriority = true`, `IsExecutable = true` — matches the expected 13-week semantics exactly.

## 12. Fourteen-week regression

Re-ran: `Resolve(candidate, 14)` → `FOUNDATION=4, BUILD=5, RACE_SPECIFIC=4, TAPER=1`. `IsOrderIndependent = true` (14 weeks fully exhausts every phase's headroom regardless of priority order — a structural boundary, not a policy choice), `IsExecutable = true`. Matches expected 14-week semantics exactly; unchanged from all prior phases.

## 13. Twelve-week parity

Re-ran: `Resolve(candidate, 12)` → `FOUNDATION=3, BUILD=4, RACE_SPECIFIC=4, TAPER=1` (the preferred allocation itself). `IsOrderIndependent = true`, `UsesApprovedPriority = false` (no priority walk needed at the preferred anchor), `IsExecutable = true`. Unchanged from every prior phase — `TWELVE_WEEK_PARITY_CHANGED` did not occur.

## 14. Compression regression

Re-ran 9/10/11-week compression cases (`Verify_ApprovedCompressionPriority_PassesWithoutChangingAllocation`): all three `Pass`, `IsOrderIndependent = false`, `UsesApprovedPriority = true`, `IsExecutable = true`, and — critically — the underlying allocation itself is confirmed unchanged from before this policy mechanism existed (the test asserts against the real resolver's own output, not a hardcoded expectation).

## 15. Workout catalog capacity assessment (Foundation extension to 4 weeks)

`plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v5.json`'s FOUNDATION phase progression contains exactly one stage:

| Stage | `relativeOrder` | `minimumExposures` | `maximumExposures` | `compressionBehavior` | `extensionBehavior` |
|---|---|---|---|---|---|
| `FOUNDATION_EASY_BASE` | 1 | 3 | 6 | `COMPRESSIBLE` | `EXTENDABLE` |

The stage is explicitly `EXTENDABLE` with a `maximumExposures` of 6 — comfortably covering a 4-week Foundation phase (4 exposures, well within the 3–6 range) without any unresolved workout ID, unsupported phase-week index, silent looping, duplicate mandatory key, unapproved repetition, or stage-lookup failure. The catalog's `eligibleWorkoutFamilies` model (`EASY`, `LONG_RUN` for FOUNDATION) is family-based rather than fixed-index-based, so extending the phase's week count does not require any new catalog entries.

**Result: `FOUNDATION_WORKOUT_CAPACITY = CAPACITY_CONFIRMED`.**

## 16. Focused tests

```text
AllocationOrderCorrectnessVerifierTests: 12 passed, 0 failed
CoreHorizonClassifierTests: 22 passed, 0 failed
PreparationRunwayContractsTests: 52 passed, 0 failed
(combined filtered run, including catalog-resolver tests): 128 passed, 0 failed, 0 skipped
```

No new tests were added — all required assertions (12/13/14-week matrix, order-independence flags, executability, generic synthetic proof, bounds/invalid fail-closed behavior) were already present in the existing suites re-run above.

## 17. Public horizon regression

```text
Sw12LongHorizonFailClosedEndToEndTests + Sw13ExactTwelveWeekOnlyEndToEndTests: 21 passed, 0 failed
```

Confirmed unchanged: 8 weeks → HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`; 12 weeks → HTTP 200; 14 weeks → HTTP 422 `PLAN_CORE_HORIZON_UNSUPPORTED`; 20 weeks → HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`. 13 weeks was not separately re-asserted by name in this suite (it was already covered by prior phases' regression, and no route or handler for it exists to test) — it remains unreachable via any public endpoint, consistent with `RaceHorizonPolicy` being completely untouched.

**Result: `PUBLIC_HORIZON_BEHAVIOR = UNCHANGED`.**

## 18. Full backend Run 1

```text
Failed: 0, Passed: 1422, Skipped: 1, Total: 1423
```

## 19. Full backend Run 2

```text
Failed: 0, Passed: 1422, Skipped: 1, Total: 1423
```

Totals match Run 1 exactly. The single skip is the pre-existing, intentionally-skipped `RealHost_ServiceResolution_OpensNoDatabaseConnection` (Phase 4G.4C.0).

## 20. Files changed by this pass

None. This pass performed validation only and authored exactly one new file: `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md`.

## 21. Files not changed (explicitly confirmed)

`CoreHorizonClassifier.cs`, `CatalogPhaseAllocation.cs` (the real allocator), `RaceHorizonPolicy` and its consumers, `PlansController.cs`, `LivePlanPreviewRouting`, `ten-k-master.v6.json`, `ten-k-workout-progression.v5.json`, `PreparationRunwayContracts.cs`/`PreparationRunwayValidators.cs`, `activation-readiness-risks.{json,md}`. All pre-existing working-tree modifications visible in `git status` predate this pass (Phase 4G.5A/4G.5B.0/4G.5B.0A and earlier) and were not touched here.

## 22. Runtime non-impact

No endpoint, controller, DI registration, public DTO, database schema, or live routing path was added, removed, or modified. 13 and 14 weeks remain fully unreachable via any public API surface. No week skeleton, workout binding, volume, pace, calendar, persistence, or confirmation logic was implemented.

## 23. Commit/push status

No file was staged. No commit or push was performed.

## Final classifications

```text
PHASE_4G_5A_VALIDATION=CONFIRMED
PHASE_4G_5B_VALIDATION=CONFIRMED
THIRTEEN_WEEK_POLICY_STATUS=APPROVED_PRODUCT_DEFAULT_EVIDENCE_INFORMED
THIRTEEN_WEEK_ORDER_INDEPENDENCE=ORDER_DEPENDENT_BUT_APPROVED_PRIORITY
THIRTEEN_WEEK_EXECUTABILITY=EXECUTABLE
APPROVED_EXTENSION_PRIORITY=FOUNDATION_FIRST
FOURTEEN_WEEK_REGRESSION=UNCHANGED
TWELVE_WEEK_PARITY=UNCHANGED
COMPRESSION_REGRESSION=UNCHANGED
FOUNDATION_WORKOUT_CAPACITY=CAPACITY_CONFIRMED
PUBLIC_HORIZON_BEHAVIOR=UNCHANGED
DYNAMIC_WEEK_SKELETON_READINESS=NOT_STARTED
NEXT_PHASE=PHASE_4G_5D
```
