# Phase 4G.5C Addendum — CoreHorizonDecision/Allocator Wiring-Gap TD

## Scope of this addendum

Phase 4G.5C's core decision work (Thirteen-Week Core Extension Decision Finalization) was already fully complete before this pass, recorded in `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` with all 23 required sections and 13 final classification lines. This addendum performs **only** Part 2 of the current prompt: recording the classifier/allocator wiring-gap finding as a formal governance entry. Part 1 (the 13-week decision itself) was not re-run or re-derived.

## Citation of the existing 4G.5C document's finding

Quoted verbatim from `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md`, Section 5 ("Classifier/allocator boundary finding"):

> A repo-wide search for `CoreHorizonDecision` and `CoreHorizonMode` usage outside the classifier's own file found **zero production consumers of either type**, including inside `CatalogPhaseAllocation.cs` (the real `CatalogPhaseAllocationResolver`). This means the literal expectation — that the allocator mechanically consumes a `CoreHorizonDecision` value — **does not hold in the current code**.
>
> This is reported honestly rather than glossed over. On inspection, it does not constitute `CLASSIFIER_ALLOCATOR_BOUNDARY_BROKEN` (an explicit stop condition), because nothing is broken, contradictory, or silently duplicated:
>
> - `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` is unchanged since Phase 4G.3B.2, takes `targetWeekCount` directly, and derives its own `AllocationMode` (`Compression`/`Preferred`/`Extension`) purely from `targetWeekCount - sumPreferredWeeks` — a week-count-only concept, distinct from and not in conflict with `CoreHorizonMode`'s day-accurate composition classification.
> - `CoreHorizonClassifier` was explicitly designed in Phase 4G.5A to have zero production call sites, DI registration, or live routing reference (confirmed in Section 3).
> - No component silently duplicates the other's decision logic: the allocator does not re-derive day/date-based composition classification, and the classifier does not distribute phase weeks.
>
> **Result: `CLASSIFIER_ALLOCATOR_BOUNDARY = CONFIRMED_WITH_ARCHITECTURAL_NOTE`**

The Section-5 finding is used here as-is, without re-deriving it. A confirmatory re-run of the same repo-wide grep in this pass reproduced the identical zero-production-consumer result (see Section "Validation results" below).

## New governance entry

**ID:** `TD-COREHORIZON-ALLOCATOR-UNWIRED-001`

**Title:** "CoreHorizonDecision/CoreHorizonMode Is Not Mechanically Consumed by CatalogPhaseAllocationResolver"

**Classification chosen:** `UNWIRED_COMPONENT_INTEGRATION_UNDECIDED`

Reasoning: the file's closest existing precedent for "two independent, non-contradictory components not yet wired together" is `TD-PACESOURCE-002`'s classification `UNWIRED_CONTEXT_FIELD_LIFECYCLE_UNDECIDED` (a context field, `AsOfDate`, that exists and is correctly typed but whose live wiring/lifecycle decision has not been made). `TD-BACKEND-001` (unclassified, no `classification` field at all) does not fit — it describes a total absence of integration between two systems, not two independently-correct-but-unconnected components. This case is architecturally the same shape as `TD-PACESOURCE-002` but about a *component* relationship (classifier → allocator) rather than a single *context field's* lifecycle, so a parallel classification token was coined following the same naming convention rather than reusing `TD-PACESOURCE-002`'s field-specific one verbatim: `UNWIRED_COMPONENT_INTEGRATION_UNDECIDED`.

**Severity chosen:** `NON_BLOCKING_OBSERVATION`

Reasoning: this matches `TD-TESTFLAKE-001`'s severity token (the only other `NON_BLOCKING_OBSERVATION` in the file), chosen over `ACTIVATION_RISK` (used by every `DECISION_REQUIRED` TD in this file) because — unlike those risks — nothing here is silently wrong, silently duplicated, or capable of producing an incorrect result today. `CoreHorizonClassifier` has zero production call sites of any kind (confirmed Phase 4G.5A dark-boundary scope), so there is no live request path this gap could affect. It is a forward-looking integration question, not a present defect.

**Status:** `OPEN`

**Statement, exact text as recorded in both files:** see the full entries below.

## Exact entry text added — `activation-readiness-risks.json`

```json
{
  "id": "TD-COREHORIZON-ALLOCATOR-UNWIRED-001",
  "title": "CoreHorizonDecision/CoreHorizonMode Is Not Mechanically Consumed by CatalogPhaseAllocationResolver",
  "recordedInPass": "Phase 4G.5C — Thirteen-Week Core Extension Decision Finalization/Resolution (2026-07-27)",
  "source": "backend/RunningApp.Application/RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs; backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocation.cs (CatalogPhaseAllocationResolver); PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md section 5",
  "statement": "A repo-wide grep for CoreHorizonDecision and CoreHorizonMode usage outside CoreHorizonClassifier.cs's own file found zero production references, including inside CatalogPhaseAllocation.cs (the real CatalogPhaseAllocationResolver.Resolve phase-week-count allocator). The only non-classifier reference anywhere in the repository is a single dark-reachability test in PreparationRunwayContractsTests.cs (DarkReachability_ClassifierVocabularyIsAllowedWithoutContractConsumption), which exists to prove the classifier's PreparationRunwayPlusCore enum vocabulary is not contract consumption -- it is not a production integration point. CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount) is unchanged since Phase 4G.3B.2 and derives its own AllocationMode (Compression/Preferred/Extension) purely from targetWeekCount minus the sum of preferred phase weeks -- a week-count-only concept that does not read, invoke, or duplicate CoreHorizonClassifier's day-accurate CoreHorizonMode classification (Unsupported/ReadinessOnly/CompressedCore/PreferredCore/ExtendedCore/PreparationRunwayPlusCore/InvalidInput) in any way. The two components are independent and currently non-contradictory -- neither silently duplicates or overrides the other's decision -- but no orchestrator or call path currently connects the classifier's output to the allocator's input. This was confirmed during Phase 4G.5C's validation of Phase 4G.5A/4G.5B.",
  "classification": "UNWIRED_COMPONENT_INTEGRATION_UNDECIDED",
  "severity": "NON_BLOCKING_OBSERVATION",
  "affectedAreas": [
    "CoreHorizonClassifier / CoreHorizonDecision / CoreHorizonMode",
    "CatalogPhaseAllocationResolver.Resolve / AllocationMode",
    "Any future orchestration layer that would wire horizon classification into phase allocation"
  ],
  "requiredResolution": [
    "Before any future phase wires CatalogPreviewGenerator or any live path to consume both CoreHorizonClassifier and CatalogPhaseAllocationResolver together, confirm explicitly whether CoreHorizonDecision is meant to gate/inform allocation behavior, or is intended to remain a permanently separate, allocator-independent classification used only for horizon-eligibility routing upstream of allocation.",
    "Do not assume integration is required, and do not silently wire the two together, without an explicit product/architecture decision being recorded first."
  ],
  "currentRuntimeImpact": "None. Both CoreHorizonClassifier and CatalogPhaseAllocationResolver remain correct and independently verified in isolation (see PHASE4G_5A_DYNAMIC_CORE_HORIZON_CLASSIFIER.md and PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md sections 3-4); CoreHorizonClassifier has zero production call sites of any kind (Phase 4G.5A's own confirmed dark-boundary scope), so the absence of allocator consumption does not affect any live request path today.",
  "blocking": false,
  "appliesToCandidateRootsFrom": "Any candidate whose future generation pipeline would need to combine day-accurate horizon classification with phase-week-count allocation -- not specific to any single catalog candidate version",
  "status": "OPEN"
}
```

Same statement/classification/severity content was mirrored into `activation-readiness-risks.md`'s table (one new row, appended after `TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001`, prose-formatted to match the existing table-row convention), plus a short "Wiring-gap addition (Phase 4G.5C)" note appended after the existing "Allocation-priority verifier clarification (Phase 4G.5B.0)" section. No existing row or section was reordered or edited.

## Updated aggregate counts

`activation-readiness-risks.json`'s `currentAppendOnlyStatus` field was updated (append-only, prior sentences preserved unchanged) to append: *"TD-COREHORIZON-ALLOCATOR-UNWIRED-001 was added OPEN during Phase 4G.5C (Thirteen-Week Core Extension Decision Resolution), sourced from a repo-wide grep confirming CoreHorizonDecision/CoreHorizonMode has zero production consumers outside CoreHorizonClassifier.cs's own file..."* and the final count sentence changed from *"16 risks are now recorded in total: 9 OPEN and 7 CLOSED"* to **"17 risks are now recorded in total: 10 OPEN and 7 CLOSED."**

The `.md` file does not restate this count as its own sentence — per the established convention (confirmed by `ActivationReadinessRiskParityTests.ActivationReadinessRiskMarkdown_HasNoDeclaredAggregate_ActualCountsAreInternallyConsistent`, which explicitly asserts the markdown file contains no aggregate-count sentence of its own), it instead points to the JSON file's `currentAppendOnlyStatus` as the authoritative running count.

## Validation results

- Repo-wide grep re-confirmation (`CoreHorizonDecision`, `CoreHorizonMode` outside `Horizon/CoreHorizonClassifier.cs` and its own test file): zero production matches, one test-only match (`PreparationRunwayContractsTests.cs`'s dark-reachability test) — identical to the finding already recorded in `PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md`.
- `ActivationReadinessRiskParityTests` + `ActivationSafetyGateTests` (`plan-catalog/tests/PlanCatalog.Tests/Architecture/`): initial run **14 passed, 1 failed** — the failure (`ActivationReadinessRiskMarkdown_HasNoDeclaredAggregate_ActualCountsAreInternallyConsistent`) was caused by an aggregate-count sentence I had drafted directly into the `.md` file, violating the file's own established convention. Corrected by removing that sentence and pointing to the JSON file instead. Re-run: **15 passed, 0 failed, 0 skipped** — full parity confirmed, and `ActivationSafetyGateTests` confirms the risk file remains `DOCUMENTATION_ONLY` (not mechanically consumed by any source file).

## Files changed

- `plan-catalog/artifacts/audits/activation-readiness-risks.json` — one new entry appended, `currentAppendOnlyStatus` updated (append-only; no existing entry reordered or edited).
- `plan-catalog/artifacts/audits/activation-readiness-risks.md` — one new table row appended, one new short clarification section appended (append-only; no existing row or section reordered or edited).
- `PHASE4G_5C_ADDENDUM_COREHORIZON_WIRING_GAP_TD.md` — this document (new).

`PHASE4G_5C_THIRTEEN_WEEK_EXTENSION_DECISION.md` was **not** modified by this pass, per the prompt's own instruction not to recreate or modify it when Part 1 is already complete.

## Confirmation: no commit/push

No file was staged. No commit or push was performed.

## Confirmation: no allocator/classifier logic modified

`CoreHorizonClassifier.cs` and `CatalogPhaseAllocation.cs` (`CatalogPhaseAllocationResolver`) were read-only in this pass — neither file's logic was changed. This pass is documentation-only, as required; the wiring gap is recorded, not closed.

## Deviations from the prompt

None. Part 1 was correctly skipped per the prompt's own pre-check instruction (the existing document was already complete with all 23 sections and 13 final classification lines from the immediately preceding pass). Part 2 was completed in full, including the one self-corrected defect (the markdown aggregate-count sentence) caught by the file's own parity test before finalizing.
