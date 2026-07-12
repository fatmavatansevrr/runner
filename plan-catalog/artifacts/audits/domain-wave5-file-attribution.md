# Wave 5 — Exact File Attribution

Measured via `git status --short -- plan-catalog` (after this clarification pass's own new report files were created). **90 total changed files**: 27 modified tracked, 63 untracked.

## Modified tracked files (27) — full attribution

### The 10 already attributed to Wave 5

| Path | Modified by Wave 5? | Pre-existing before Wave 5? | Attributed wave/task | Evidence | Purpose | Keep? |
|---|---:|---:|---|---|---|---|
| schemas/progression-modifier.schema.json | Yes | No | Wave 5 / D2 | Authored this session; conditional `allOf` added for `maximumComplexityTier` | D2 schema evolution | Yes |
| src/.../Audit/PilotDomainContentAudit.cs | Yes | No | Wave 5 / D2 + clarification | AUD-330..336 added; AUD-333 append-only correction | D2 audit entries | Yes |
| src/.../Models/ProgressionModifierDefinition.cs | Yes | No | Wave 5 / D2 | `MaximumComplexityTier` made nullable | D2 model change | Yes |
| src/.../Validation/ProgressionModifierValidator.cs | Yes | No | Wave 5 / D2 | schemaVersion-branch rejecting legacy field on v2+ | D2 validator | Yes |
| tests/.../DeprecatedFieldRegressionTests.cs | Yes | No | Wave 5 / D2 | Added nullable-legacy-only guard test | D2 regression guard | Yes |
| tests/.../PilotCatalogStructuralTests.cs | Yes | No | Wave 5 / D2 | Version-qualified a lookup made ambiguous by v2 | D2 test fix | Yes |
| artifacts/audits/domain-blocker-resolution-plan.json | Yes (append) | Partially | Wave 2/3 base + Wave 5 append | `wave5ExecutionUpdate` block appended | Living plan doc | Yes |
| artifacts/audits/domain-blocker-resolution-plan.md | Yes (append) | Partially | Wave 2/3 base + Wave 5 append | Wave 5 section appended | Living plan doc | Yes |
| artifacts/audits/domain-blocker-version-cascade-forecast.json | Yes (append) | Partially | Wave 2/3 base + Wave 5 append | `wave5ActualCascade` block appended | Living forecast doc | Yes |
| artifacts/audits/domain-blocker-version-cascade-forecast.md | Yes (append) | Partially | Wave 2/3 base + Wave 5 append | Wave 5 section appended | Living forecast doc | Yes |

### The 17 previously unattributed (this pass's focus)

| Path | Modified by Wave 5? | Pre-existing before Wave 5? | Attributed wave/task | Evidence | Purpose | Keep? |
|---|---:|---:|---|---|---|---|
| artifacts/audits/active-v4-domain-blocker-inventory.json | No | Yes | Wave 2 + Wave 3 | Diff adds only `wave2CandidateState` and `wave3CandidateState` keys, each explicitly named per wave; no `wave5CandidateState` key exists | Manual per-wave candidate-state annotation | Yes |
| artifacts/audits/active-v4-domain-blocker-inventory.md | No | Yes | Wave 2 + Wave 3 | Markdown counterpart, same wave2/3-only content | Same | Yes |
| artifacts/audits/golden-fixture-v3-integrity.json | Mechanically (test side-effect) | Yes | Generated artifact — no single wave | Diff is **only** the `generatedAtUtc` timestamp line; `GoldenFixtureIntegrityReportWriter.Write()` is invoked by the test suite (`DomainContentAuditReportWriterTests`-adjacent infra) and overwrites this file on every `dotnet test` run; content reads only `docs/canonical/golden-fixture-v3/`, unaffected by D2 | Auto-generated integrity report | Yes |
| artifacts/audits/golden-fixture-v3-integrity.md | Mechanically (test side-effect) | Yes | Generated artifact — no single wave | Same — timestamp-only diff | Same | Yes |
| artifacts/audits/ten-k-pilot-domain-decision-audit.json | Mechanically (test side-effect); content reflects Wave 5 source | Yes | Wave 2+3 content, mechanically regenerated to include Wave 5 | `DomainContentAuditReportWriterTests.Write_...` calls `DomainContentAuditReportWriter.Write(RepoRoot())`, unconditionally overwriting this file from the live `PilotDomainContentAudit.Entries` on every `dotnet test` run. On-disk file currently contains AUD-300..336 and the string `WAVE5-CLARIFICATION` (confirmed via grep), with `generatedAtUtc` from this session — proof it was last regenerated after Wave 5 code changes existed, not hand-authored by any wave | Auto-generated full decision dump | Yes |
| artifacts/audits/ten-k-pilot-domain-decision-audit.md | Mechanically (test side-effect); content reflects Wave 5 source | Yes | Wave 2+3 content, mechanically regenerated to include Wave 5 | Same writer/mechanism | Same | Yes |
| schemas/run-layout.schema.json | No | Yes | Wave 2 (D1) | `sequenceOrder` removed from unconditional `required`; schemaVersion-conditional `allOf` added — exact match to `AUD-300` ("WAVE2 D1: schemaVersion 2 removes...sequenceOrder") | D1 schema evolution | Yes |
| schemas/workout-definition.schema.json | No | Yes | Wave 2 + Wave 3 | `componentType` enum narrowed (Wave 2 D6/D8/D10/D12) + `complexityTier` schemaVersion-conditional (Wave 3 D5/D7/D9/D11, the exact template Wave 5 reused) | D6/D8/D10/D12 + D5/D7/D9/D11 schema evolution | Yes |
| src/.../Catalog/CatalogSourceSnapshot.cs | No | Yes | Wave 2 (draft-candidate infra) | Adds `Status != Draft` filter to legacy `FindWorkout` auto-selection — required once Wave 2's first DRAFT candidate workouts (v3) existed | Prevent DRAFT versions leaking into legacy resolution | Yes |
| src/.../Models/LayoutSlotDefinition.cs | No | Yes | Wave 2 (D1) | `SequenceOrder` int → int? — model counterpart of the D1 schema change | D1 model support | Yes |
| src/.../Models/WorkoutDefinition.cs | No | Yes | Wave 2 + Wave 3 | `Components` required→optional (Wave 2), `ComplexityTier` required→optional (Wave 3, the exact template Wave 5 reused) | D6/D8/D10/D12 + D5/D7/D9/D11 model support | Yes |
| src/.../Validation/ActiveVersionUniquenessValidator.cs | No | Yes | Wave 2 (draft-candidate infra) | Excludes `CatalogStatus.Draft` combinations from uniqueness check — required for v5/v6/v7 to coexist with active v4 | Allow DRAFT candidates alongside active root | Yes |
| src/.../Validation/RunLayoutValidator.cs | No | Yes | Wave 2 (D1) | schemaVersion-conditional branch with new `LEGACY_SEQUENCE_ORDER_NOT_ALLOWED_IN_NEW_SCHEMA` code — direct template for Wave 3's/Wave 5's `LEGACY_*_NOT_ALLOWED_IN_NEW_SCHEMA` codes | D1 validator enforcement | Yes |
| src/.../Validation/WorkoutDefinitionValidator.cs | No | Yes | Wave 2 + Wave 3 | `ValidateComponents` added with verbatim string "...shared Wave 2 vocabulary"; `LEGACY_COMPLEXITY_TIER_NOT_ALLOWED_IN_NEW_SCHEMA` added (Wave 3, direct template for Wave 5's own validator) | D6/D8/D10/D12 + D5/D7/D9/D11 validator enforcement | Yes |
| src/.../Publishing/CatalogPublisher.cs | No | Yes | Wave 2 (draft-candidate infra) | `ExcludeDraftArtifacts()` added — prevents force-publishing in-progress DRAFT candidates | Draft-safety at publish time | Yes |
| src/.../Publishing/CatalogStamper.cs | No | Yes | Wave 2 (draft-candidate infra) | `StampAsPublished` now preserves `Draft` status instead of always flipping to `Published` — the exact mechanism Wave 5's own `build-bundle --version 7` validation relied on | Draft-safety at bundle-stamp time | Yes |
| tests/.../WorkoutArtifactImmutabilityTests.cs | No | Yes | Wave 2 (draft-candidate infra) | `Assert.Equal(2,...)` loosened to `>=2` + explicit v1/v2 checks — accommodates growing draft version counts (v3 from Wave 2, v4 from Wave 3) | Test accommodation for draft coexistence | Yes |

### Analysis of the 17 previously unattributed files

**Expected source changes or accidental/local/generated?** All 17 are **expected**: 13 are genuine Wave 2/Wave 3 implementation code (schema, model, validator, publishing infrastructure) that Wave 5 itself reused as a template; 4 (`golden-fixture-v3-integrity.{json,md}`, `ten-k-pilot-domain-decision-audit.{json,md}`) are **mechanically regenerated report files** — test-suite side effects with no hand-authored content of their own. None show any sign of being an accidental local edit, an unrelated experiment, or a genuinely unknown/unattributable change.

**Why were they not committed before Wave 5?** **Not knowable from repository evidence.** `git log` shows only 5 commits, the latest being `plan-catalog-added` (the initial snapshot commit). There is no commit message, comment, or other artifact anywhere in the repository explaining why Wave 2/Wave 3 (or Wave 5) work was left uncommitted. This is stated as unknown rather than guessed.

**Required for v5/v6/v7 candidate validation?** **Yes.** `validate-combination` for v5/v6/v7 and `build-bundle` for v7 structurally depend on this code:
- `WorkoutDefinitionValidator`'s schemaVersion≥3 complexityTier rejection + `WorkoutDefinition.ComplexityTier` nullability — required for `EASY_STANDARD`/`FARTLEK`/`LONG_RUN_STANDARD`/`THRESHOLD_TEMPO` v4 (referenced by v7's `INTERMEDIATE_MODIFIER v5`) to validate.
- `RunLayoutValidator`'s schemaVersion-conditional check + `LayoutSlotDefinition.SequenceOrder` nullability — required for `RUN_LAYOUT_4D v2` (referenced by v7) to validate.
- `ActiveVersionUniquenessValidator` / `CatalogSourceSnapshot`'s Draft-exclusion — required for v4 (active) and v5/v6/v7 (draft) to coexist without a uniqueness violation.
- `CatalogStamper`'s Draft-status preservation — required for `build-bundle` to correctly stamp a DRAFT candidate without falsely publishing its DRAFT-status dependencies.

**Would reverting/removing any of the 17 break v5/v6/v7 validation?** **Yes, for the 13 code/schema files** — reverting them would reintroduce required fields (`sequenceOrder`, `complexityTier`, `components`) that v5/v6/v7's actual artifacts no longer populate, failing schema/validator checks. This conclusion is drawn from direct dependency analysis, **not** from an executed revert (per instruction, nothing was reverted). The 4 generated-report files (`golden-fixture-v3-integrity.*`, `ten-k-pilot-domain-decision-audit.*`) are **not** required for candidate validation — they are informational output only.

## Untracked files created by Wave 5 (20)

| Path | Created by Wave 5? | Purpose | Keep? |
|---|---:|---|---|
| catalog/progression-modifiers/intermediate-progression-modifier.v2.json | Yes | D2 new artifact (maximumComplexityTier removed) | Yes |
| catalog/level-modifiers/intermediate-modifier.v5.json | Yes | D2 cascade (progressionModifier→v2) | Yes |
| catalog/combinations/ten-k-4d-intermediate.v7.json | Yes | D2 candidate root | Yes |
| tests/.../DomainWave5D2ResolutionTests.cs | Yes | D2 test suite (24 tests) | Yes |
| artifacts/audits/domain-wave5-d2-field-inventory.json / .md | Yes | Task A report | Yes |
| artifacts/audits/domain-wave5-d2-ownership.json / .md | Yes | Task B report (incl. clarification corrections) | Yes |
| artifacts/audits/domain-wave5-d2-implementation.json / .md | Yes | Implementation report | Yes |
| artifacts/audits/domain-wave5-d2-evidence-classification.json / .md | Yes | Task F report | Yes |
| artifacts/audits/domain-wave5-version-cascade.json / .md | Yes | Task G report (incl. clarification addendum) | Yes |
| artifacts/audits/domain-wave5-candidate-blockers.json / .md | Yes | Task H report | Yes |
| artifacts/audits/domain-wave5-d2-clarification.json / .md | Yes | **This pass's** clarification report | Yes |
| artifacts/audits/domain-wave5-file-attribution.json / .md | Yes | **This pass's** file attribution report | Yes |

## Untracked files pre-existing from Wave 2 / Wave 3 (43) — not touched by Wave 5

These were already present, uncommitted, in the working tree before any Wave 5 work began (prior wave implementations that were never committed to git).

**Wave 2 (22 files):** `domain-wave2-{activation-plan,candidate-blockers,component-vocabulary,implementation,schema-migration,version-cascade}.{json,md}` (12), `tests/.../DomainWave2ResolutionTests.cs` (1), `catalog/combinations/ten-k-4d-intermediate.v5.json`, `catalog/layouts/run-layout-4d.v2.json`, `catalog/level-modifiers/intermediate-modifier.v3.json`, `catalog/templates/ten-k-master.v4.json`, `catalog/workout-progressions/ten-k-workout-progression.v3.json`, `catalog/workouts/{easy-standard,fartlek,long-run-standard,threshold-tempo}.v3.json` (9).

**Wave 3 (21 files):** `domain-wave3-{activation-plan,candidate-blockers,complexity-removal,d8-d12-evidence-review,schema-migration,version-cascade}.{json,md}` (12), `tests/.../DomainWave3ComplexityRemovalTests.cs` (1), `catalog/combinations/ten-k-4d-intermediate.v6.json`, `catalog/level-modifiers/intermediate-modifier.v4.json`, `catalog/templates/ten-k-master.v5.json`, `catalog/workout-progressions/ten-k-workout-progression.v4.json`, `catalog/workouts/{easy-standard,fartlek,long-run-standard,threshold-tempo}.v4.json` (8).

All are **kept** (`Keep? = Yes`) — none are temporary or preview outputs; deleting them would destroy prior, uncommitted wave work that is not this task's to discard.

## Confirmations

- **Temporary/preview files found:** 0. `ls artifacts/appsel-plan-catalog/` shows exactly the 7 historical releases + `cross-release-hash-exceptions.json` + `release-status.json` + `retirements.json` — no `*preview*` directory exists.
- **No permanent preview release directory remains.**
- **No release-status.json, retirements.json, or cross-release-hash-exceptions.json change** — none appear in `git status`.
- **No files were deleted** in this pass.
