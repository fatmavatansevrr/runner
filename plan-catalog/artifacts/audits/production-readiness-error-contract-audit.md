# Production Readiness Error Contract Audit — PROD-CONTRACT-001

## The question

Why did the Production negative test report **9 errors** when the active root
(`TEN_K__4D__INTERMEDIATE v4`) contains **13 blocking domain decisions across 9 artifacts**?

## Answer (established by tracing code, not by wording)

**One Production-readiness error represents one blocking artifact identity**
(`DocumentType` + `Key` + `Version`), not one field-level decision. The 9-error count matches
`blockingArtifactCount` (9) by construction, not `blockingDecisionCount` (13). This was already the
correct relationship — the Part 3 report's statement was accurate — but until this audit the 13
decisions nested inside those 9 errors were only reachable by parsing a free-text message string, which
is a real defect. That defect has been corrected in this task (see "Code changes made" below); the
grouping rule itself (Option A, artifact-grouped) was not changed.

## Exact code path (before this task's changes)

```
src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs  BuildRelease()
  bundles = eligibleCombinations.Select(bundleAssembler.Assemble)      // 1 bundle (v4)
  bundleArtifactTuples = bundles.SelectMany(BundleArtifactTuples).Distinct()  // 9 DISTINCT (docType,key,version) tuples
  ↓
src/PlanCatalog.Core/Validation/PublishReadinessValidator.cs  ValidateContentDecisions()
  foreach (documentType, key, version) in bundleArtifactTuples.Distinct():
      blocking = PilotDomainContentAudit.BlockingEntriesFor(documentType, key, version)  // 1..2 DomainContentDecision rows
      if blocking.Count == 0: continue
      jsonPaths = string.Join(", ", blocking.Select(b => b.JsonPath))   // ALL field paths concatenated
      issues.Add(new ValidationIssue(code, Error, "... contains PLACEHOLDER_UNCONFIRMED content ({jsonPaths}) ...", "$"))
      // <-- exactly ONE ValidationIssue per artifact identity, regardless of how many blocking
      //     decisions that artifact carries. JsonPath field on the issue itself is hardcoded "$".
```

**Grouping key**: artifact identity `(DocumentType, Key, Version)`, via `bundleArtifactTuples.Distinct()`.
**Cardinality**: `errorCount = count(distinct artifact identities with ≥1 blocking decision) = 9`.
**Decision detail (before this task)**: present only inside the free-text `Message` string (comma-joined
`JsonPath`s); `Classification`, `Reason`, and audit-entry ID were **not** carried into the
`ValidationIssue` at all. The structured `ValidationIssue.JsonPath` field was always the literal `"$"`
(a placeholder, not the real path) — real field paths existed only as substrings of `Message`.

## Required trace

| Stage | Input count | Output count | Grouping key | Information retained (before fix) |
|---|---:|---:|---|---|
| Active root closure (`TEN_K__4D__INTERMEDIATE v4`) | 1 combination | 1 bundle | n/a | full bundle |
| Bundle → artifact tuples | 1 bundle | 9 distinct `(docType,key,version)` | `Distinct()` over all referenced artifacts | full identity |
| Artifact tuples → `DomainContentDecision` blocking entries | 9 artifacts | 13 entries (`PilotDomainContentAudit.BlockingEntriesFor`, exact-version match) | exact `(docType,key,version)` match | full `DomainContentDecision` record, in memory |
| Blocking entries → `ValidationIssue` | 13 entries across 9 artifacts | **9** issues | `(docType,key,version)` — one issue per artifact, all of that artifact's entries folded in | **only** `Message` (free text) + hardcoded `JsonPath="$"`; `Classification`/`Reason`/`EntryId` dropped |
| `ValidationIssue[]` → CLI output | 9 issues | 9 printed lines (text) / 9 array entries (json) | 1:1 passthrough | same as above — CLI never re-derives or re-groups |
| `ValidationIssue[]` → audit reports | 9 issues (pre-fix) / 13 decisions (separately, via `BlockerScopeMeasurement`) | reported as two explicitly separate numbers | n/a | `BlockerScopeMeasurement` computes 13/9 independently, directly from `PilotDomainContentAudit.Entries` — never derived from the `ValidationIssue` list |

Both `13` and `9` were independently correct and consistent — `BlockerScopeMeasurement` (used by the
audit reports) computes them straight from `PilotDomainContentAudit.Entries`, the same source of truth
`PublishReadinessValidator` consults. The Production negative test's `errorCount = 9` was never expected
to equal `13`; it was always expected to equal the **artifact** count. That expectation was correct and
is now also verified by test (`TopLevelErrorCount_MatchesBlockingArtifactCount_NotBlockingDecisionCount`).

## Required decision inventory (active root closure, `TEN_K__4D__INTERMEDIATE v4`)

| Document type | Key | Version | Field/path | Classification | Error group (artifact) |
|---|---|---:|---|---|---|
| RUN_LAYOUT | RUN_LAYOUT_4D | 1 | `$.slots[*].sequenceOrder` | PLACEHOLDER_UNCONFIRMED | RUN_LAYOUT/RUN_LAYOUT_4D v1 |
| PROGRESSION_MODIFIER | INTERMEDIATE_PROGRESSION_MODIFIER_V1 | 1 | `$.maximumComplexityTier, $.maximumHardSessionsPerWeek, $.mainSetDoseMultiplier, $.allowGoalPaceRehearsal, $.allowSecondHardStimulus` | PLACEHOLDER_UNCONFIRMED | PROGRESSION_MODIFIER/INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1 |
| RUNTIME_CONDITION_VALUE_REGISTRY | RUNTIME_CONDITION_VALUES_V1 | 1 | `$.conditionValueSets[PACE_SOURCE_IN,TIME_ADEQUACY_IN,CORE_ENTRY_READINESS_IN]` | PLACEHOLDER_UNCONFIRMED | RUNTIME_CONDITION_VALUE_REGISTRY/RUNTIME_CONDITION_VALUES_V1 v1 |
| PEAK_VOLUME_BAND_POLICY | PEAK_VOLUME_BANDS_V1 | 2 | `$.entries[TEN_K,NEW\|ADVANCED\|EXPERIENCED,3\|4\|5]` | PLACEHOLDER_UNCONFIRMED | PEAK_VOLUME_BAND_POLICY/PEAK_VOLUME_BANDS_V1 v2 |
| WORKOUT_DEFINITION | EASY_STANDARD | 2 | `$.complexityTier` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/EASY_STANDARD v2 |
| WORKOUT_DEFINITION | EASY_STANDARD | 2 | `$.components` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/EASY_STANDARD v2 |
| WORKOUT_DEFINITION | FARTLEK | 2 | `$.complexityTier` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/FARTLEK v2 |
| WORKOUT_DEFINITION | FARTLEK | 2 | `$.components` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/FARTLEK v2 |
| WORKOUT_DEFINITION | GOAL_PACE_TEN_K | 1 | `$.eligiblePhases, $.complexityTier, $.allowedPrescriptionModes, $.components` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/GOAL_PACE_TEN_K v1 |
| WORKOUT_DEFINITION | LONG_RUN_STANDARD | 2 | `$.complexityTier` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/LONG_RUN_STANDARD v2 |
| WORKOUT_DEFINITION | LONG_RUN_STANDARD | 2 | `$.components` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/LONG_RUN_STANDARD v2 |
| WORKOUT_DEFINITION | THRESHOLD_TEMPO | 2 | `$.complexityTier` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/THRESHOLD_TEMPO v2 |
| WORKOUT_DEFINITION | THRESHOLD_TEMPO | 2 | `$.components` | PLACEHOLDER_UNCONFIRMED | WORKOUT_DEFINITION/THRESHOLD_TEMPO v2 |

- **Total decision rows**: 13 (one row per `DomainContentDecision` entry with `Classification = PLACEHOLDER_UNCONFIRMED`, reachable from `v4`'s exact dependency closure). Note: two of these rows (`INTERMEDIATE_PROGRESSION_MODIFIER_V1` and `GOAL_PACE_TEN_K`) each represent **multiple field paths bundled into one audit entry's `JsonPath` string** (5 fields and 4 fields respectively) — the hand-authored audit itself is not perfectly field-atomic in all 13 rows; this is documented, not silently absorbed (see "Known pre-existing granularity note" below).
- **Total unique artifact identities**: 9.
- **Total validation errors (current contract)**: 9 (one per artifact identity).
- **All 13 decisions user-visible / machine-readable?** Yes, **after this task's fix**. Before this fix, all 13 were visible only as substrings of 9 free-text `Message` strings (technically present, not structurally parseable). After the fix, all 13 are present as discrete, typed `ProductionReadinessBlockingDecision` records (`EntryId`, `FieldPath`, `Classification`, `Reason`) nested under their owning `ContentDecisionGuardError`.

No decision is lost, silently merged without visibility, or double-counted, in either the old or new
contract — this audit's independent recomputation (`BlockerScopeMeasurement.ScopedDecisionCount`,
walked straight from `PilotDomainContentAudit.Entries`, entirely independently of
`PublishReadinessValidator`) agrees exactly with the validator's own count (13), and is now asserted by
test (`NoBlockingDecision_IsLostThroughArtifactGrouping`).

## Contract chosen: Option A — artifact-grouped top-level errors with complete nested field-level decisions

This matches the task's stated preferred default, and matches the repository's actual pre-existing
architecture (`PublishReadinessValidator.ValidateContentDecisions` was already artifact-grouped before
this task; only the *nested decision detail* was a defect, not the grouping key itself). Reasons this
grouping was kept rather than switched to decision-level (Option B):

- **Avoids noisy duplicate top-level errors.** An artifact like `EASY_STANDARD v2` has 2 blocking fields;
  decision-level errors would print 2 near-identical top-level lines differing only in `FieldPath`,
  obscuring that they concern the same artifact and the same publish decision (do not ship this artifact
  to Production).
- **Preserves artifact context per error** without requiring every consumer to re-derive it by grouping
  on the client side.
- **Matches `BlockerScopeMeasurement`'s existing artifact/decision duality** (`artifact-level` vs
  `decision-level`, "must never be compared directly" — see that file's own doc comment), which the
  audit reports (`placeholder-scope-audit.md`) already rely on and document.
- **Consistency with `docs/README.md`**: the governance doc (§ "Decision-vs-artifact-level metrics") already
  mandates keeping these as two separate units; decision-level top-level errors would blur that separation
  at the point closest to the human operator (the CLI).
- **Backward compatibility**: `ValidateContentDecisions` (the pre-existing `ValidationResult`-returning
  method) keeps returning exactly one `Issue` per artifact, unchanged in cardinality — only its `Message`
  text and the (new, additive) `ContentDecisionGuardResult`/`ContentDecisionDetail` channel changed.
- **Test clarity**: existing tests asserting `ex.Result.Issues` contains a given `Code` (e.g.
  `CatalogPublisherTests.Publish_ProductionChannel_NeverAcceptsUnconfirmedContentEvenWithFlag`) continue
  to pass unmodified.

## Defect found and corrected

**Classification**: "groups by artifact but loses decision detail" (per this task's correction rules).

Before this task, `ValidationIssue` (the type `PublishReadinessValidator.ValidateContentDecisions`
returns) is a flat record — `{ Code, Severity, Message, JsonPath }` — with no field for a nested
decision list. The content-decision guard therefore had nowhere structured to put per-decision
`Classification`/`Reason`/audit-entry-ID; it could only concatenate `JsonPath`s into `Message` and hardcode
`JsonPath = "$"`. This is a genuine defect against the required contract ("Do not encode all nested
decision data only inside one concatenated message string").

**Fix** (additive, no breaking changes):

1. New file `src/PlanCatalog.Core/Validation/ContentDecisionGuardResult.cs` — three new records:
   - `ProductionReadinessBlockingDecision(EntryId, FieldPath, Classification, Reason)`
   - `ContentDecisionGuardError(ErrorCode, Severity, DocumentType, Key, Version, BlockingDecisions)`
   - `ContentDecisionGuardResult(BlockingArtifactCount, BlockingDecisionCount, Errors)` with an
     `IsValid` property and a `ToValidationResult()` backward-compatible projection.
2. `PublishReadinessValidator.cs` — added `ValidateContentDecisionsDetailed(...)`, the new canonical
   entry point, returning `ContentDecisionGuardResult`. The pre-existing `ValidateContentDecisions(...)`
   now delegates to it via `.ToValidationResult()` — same signature, same return type, same issue count
   (9), only the `Message` text changed (now states the decision count per artifact instead of only
   concatenating paths).
3. `CatalogValidationException.cs` — added an optional, nullable `ContentDecisionGuardResult?
   ContentDecisionDetail` property (new optional constructor parameter, defaults to `null` — every
   existing call site is unaffected).
4. `CatalogPublisher.cs` — the content-decision-guard stage now calls
   `ValidateContentDecisionsDetailed(...)` and attaches the structured result to the thrown
   `CatalogValidationException`.
5. `ReleaseCommands.cs` (CLI) — `BuildRelease`/`Publish`'s `catch (CatalogValidationException)` blocks now
   pass `ex.ContentDecisionDetail` as the `data` parameter to `CliOutput.Report` (previously always
   `null` for this failure stage). `CliOutput.cs` itself required **no** change — it already serializes
   any non-null `data` object as structured JSON beneath the issue list, in both text and `--json` modes.

No parsing of formatted message strings was introduced anywhere — the CLI and the new structured channel
both read directly from the same `ContentDecisionGuardResult` object.

## Do CLI, machine-readable output, and audit reports now agree?

Verified empirically (`dotnet run ... publish --channel Production --allow-unconfirmed-content`, both
with and without `--json`) against the real, post-retirement active root:

- Text-mode CLI: 9 `Error:` lines (`Issues`), each message now stating `"contains N PLACEHOLDER_UNCONFIRMED
  field-level decision(s) (...)"`, followed by the full structured `Data` object printed as JSON
  (`BlockingArtifactCount: 9`, `BlockingDecisionCount: 13`, `Errors: [ 9 entries, 13 total nested
  BlockingDecisions ]`).
- `--json` mode: identical structured payload, `Success: false`, `Issues.Count = 9`,
  `Data.BlockingArtifactCount = 9`, `Data.BlockingDecisionCount = 13`.
- Audit reports (`placeholder-scope-audit.md`, `BlockerScopeMeasurement`): already stated 13
  decisions / 9 artifacts, computed independently from the same `PilotDomainContentAudit.Entries` source
  — now cross-verified by test to equal the live validator's structured output exactly.

All three surfaces agree. No client needs to parse a message string to recover structured data.

## Release handling

No new numbered release was created and none was required — this task changed only validator
diagnostics, the error payload/model, `CatalogValidationException`, CLI wiring, tests, and audit/governance
documentation. `0.6.0-pilot` was not touched (confirmed: `release-manifest.json` content hash unchanged;
`verify-release --version 0.6.0-pilot` still PASSED). `0.5.0-pilot` remains superseded; no release was
superseded or re-superseded by this task.

## Commands run

- `dotnet restore`, `dotnet build -c Release` (0 warnings, 0 errors)
- `dotnet test -c Release` (260/260 — 245 pre-existing + 15 new contract tests)
- `publish --version <n> --channel Production --allow-unconfirmed-content` (both `--json` and text mode) against the real retirement ledger — confirmed 9 errors, `BlockingArtifactCount=9`, `BlockingDecisionCount=13`, no partial release directory created
- `verify-release` for all 7 releases (`1.0.0` through `0.6.0-pilot`) — all PASSED
- `CrossReleaseHashConsistencyTests` + `PublishTimeCrossReleaseHashGuardTests` — 10/10 PASSED
- `git status --porcelain -- backend/ runner/backend/` — empty (backend untouched)
- `sha256sum artifacts/appsel-plan-catalog/retirements.json` — `d629ec93aea3f21dffbecaa501e2f43c256155f34351971dfade00c41633e1e9` (unchanged from Part 3)

## Known pre-existing granularity note (not corrected in this task — informational only)

Two of the 13 `DomainContentDecision` audit rows (`AUD-044` for `INTERMEDIATE_PROGRESSION_MODIFIER_V1`,
and the `GOAL_PACE_TEN_K` entry) each bundle multiple conceptually-distinct field paths into a single
row's `JsonPath` string (5 fields and 4 fields respectively), rather than one row per field. This is a
pre-existing characteristic of the hand-authored `PilotDomainContentAudit.cs` (predates this task) and is
**not** something this task's error-contract fix introduced or was asked to resolve — the fix here
faithfully preserves whatever `PilotDomainContentAudit.Entries` contains, one-to-one, in the structured
output. Splitting those two rows into fully field-atomic entries would be a *domain-content audit*
change, not an *error-contract* change, and is explicitly out of scope ("Do not continue with domain
placeholder resolution").

## Final status

`PRODUCTION_READINESS_ERROR_CONTRACT`: **ARTIFACT_GROUPED_WITH_STRUCTURED_NESTED_DECISIONS** —
`topLevelErrorCount = 9`, `blockingArtifactCount = 9`, `blockingDecisionCount = 13`. Contract now
documented, structurally enforced by 15 new tests, and machine-readable end-to-end.
