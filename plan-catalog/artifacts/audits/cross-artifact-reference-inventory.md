# Cross-Artifact Reference Inventory — REFINV-001

**Read-only inventory. No code, schema, or catalog artifact was modified to produce this report.**

## Method

Inspected every `record` in `src/PlanCatalog.Core/Models/`, `src/PlanCatalog.Contracts/References/`,
`src/PlanCatalog.Contracts/Bundles/`, `src/PlanCatalog.Contracts/Manifests/`, every JSON schema in
`schemas/`, every validator in `src/PlanCatalog.Core/Validation/`, `CatalogSourceSnapshot`'s `FindX`
methods, and `CatalogBundleAssembler`. Every `string`/`IReadOnlyList<string>`/`IReadOnlySet<string>` field
was checked for whether it names another catalog artifact (a cross-artifact reference) versus a local
identifier, free-text label, or literal value set.

## Genuine key-only cross-artifact references (the complete set — only 2 exist)

| Field | File | Type | Consumed by | Classification |
|---|---|---|---|---|
| `LevelModifierDefinition.EligibleWorkoutKeys` | `src/PlanCatalog.Core/Models/LevelModifierDefinition.cs:17` | `IReadOnlySet<string>` | `CatalogBundleAssembler.Assemble` (membership filter, line 61); `LevelModifierValidator.Validate` (existence check only) | **ARTIFACT_DEPENDENCY_REQUIRES_VERSION** |
| `WorkoutProgressionStageDefinition.WorkoutCandidateKeys` | `src/PlanCatalog.Core/Models/WorkoutProgressionStageDefinition.cs:14` | `IReadOnlyList<string>` | `CatalogBundleAssembler.Assemble` (candidate source, line 59); `WorkoutProgressionValidator.Validate` (existence + family-eligibility checks) | **ARTIFACT_DEPENDENCY_REQUIRES_VERSION** |

Both are resolved through the **same single mechanism**:

| Resolution method | File | Behavior |
|---|---|---|
| `CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger? retirementLedger = null)` | `src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs:42-49` | `Workouts.Where(key match && not retired).OrderByDescending(Version).FirstOrDefault()` — auto-selects the **highest non-retired version** for a bare key. This is the **only** `FindX` method in `CatalogSourceSnapshot` that performs auto-version-selection; every other `FindX` method takes a `VersionedCatalogReference` and does an exact `Key == r.Key && Version == r.Version` match (no auto-selection). |

**Classification rationale**: both fields directly determine **which specific published
`WorkoutDefinition` artifact instances** end up in a `PublishedTemplateBundle` — they are not abstract
capability/family filters (contrast with `PhaseDefinition.EligibleWorkoutFamilies`, an enum-based family
allow-list, or `WorkoutDefinition.EligiblePhases`, which do not name specific artifact instances). Per the
target architectural outcome ("the same root combination version must always resolve the same exact
versioned dependency graph"), any field that lets bundle *content* silently change as new artifact versions
are added must become an exact key+version reference — matching Findings 5 and 6 in
`deterministic-graph-prechange-assessment.md`.

## Fields named in the task's "at minimum inspect" list that are NOT key-only (already versioned — do not reproduce as a key-only reference)

| Field named in task | Actual field / type found | Verified content |
|---|---|---|
| `PlanTemplateDefinition.WorkoutProgressionKey` | `PlanTemplateDefinition.WorkoutProgression` — `VersionedCatalogReference` (`src/PlanCatalog.Core/Models/PlanTemplateDefinition.cs:17`) | Already carries `documentType` + `key` + `version`. Resolved via `CatalogSourceSnapshot.FindWorkoutProgression(VersionedCatalogReference)` — exact match, no auto-selection. **Not key-only.** |
| `LevelModifierDefinition.ProgressionModifierKey` | `LevelModifierDefinition.ProgressionModifier` — `VersionedCatalogReference` (`src/PlanCatalog.Core/Models/LevelModifierDefinition.cs:19`) | Already carries `documentType` + `key` + `version`. Resolved via `CatalogSourceSnapshot.FindProgressionModifier(VersionedCatalogReference)` — exact match. **Not key-only.** |
| RulePack policy/registry references | `RulePackDefinition.RuntimeConditionValueRegistry`, `RulePackDefinition.PeakVolumeBandPolicy` — both `VersionedCatalogReference`; `RulePackDefinition.Policies`, `RulePackDefinition.Rules` — both `IReadOnlyList<VersionedCatalogReference>` (`src/PlanCatalog.Core/Models/RulePackDefinition.cs`) | All four fields already carry exact versions. Resolved via `FindRuntimeConditionValueRegistry`/`FindPeakVolumeBandPolicy` — exact match. **Not key-only.** |
| master required-rule fields | `PlanTemplateDefinition.RequiredRules` — `IReadOnlyList<VersionedCatalogReference>` (`src/PlanCatalog.Core/Models/PlanTemplateDefinition.cs:19`) | Already carries exact versions. **Not key-only** — but flagged separately as **Finding 7** in the pre-change assessment: it's a versioned reference that is *never consulted by bundle assembly* and can silently disagree with `TemplateCombinationDefinition.RulePack`'s version. This is a duplicate-ownership/consistency problem, not a key-only-reference problem — a different defect class from Findings 5/6. |

## Fields inspected and found to be non-dependency identifiers or local/free-text values (no cross-artifact meaning)

| Field | Type | Why it's not a cross-artifact reference |
|---|---|---|
| `WorkoutProgressionStageDefinition.StageKey` | `string` | A local identifier scoped to the owning progression's own stage list (used for `FallbackStageKey` self-reference within the same document), never resolved against another catalog artifact. |
| `WorkoutProgressionStageDefinition.FallbackStageKey` | `string?` | Same document-local scope as `StageKey`. |
| `RuntimeConditionValueSet.AllowedValues` / `RuntimeEligibilityCondition.AllowedValues` | `IReadOnlySet<string>` | Literal permitted value strings (e.g. `"REALISTIC"`, `"STANDARD"`), not references to other catalog documents. |
| `WorkoutComponentDefinition.IntensityDescriptor` | `string` | Free-text/enum-like label describing intensity, not an artifact key. |
| `PublishedTemplateBundle.BundleKey`, `CatalogReleaseManifest.ReleaseKey`/`ReleaseVersion` | `string` | Identity fields of the artifact/release itself, not references to a different artifact. |
| `CatalogArtifactReference.Key` / `VersionedCatalogReference.Key` | `string` (component of an already-fully-versioned reference tuple) | Only meaningful paired with the same record's `DocumentType`+`Version` fields — not a standalone key-only reference. |

## FindX auto-selection audit (complete)

All 9 `FindX` methods on `CatalogSourceSnapshot`:

| Method | Selector | Auto-selects a version? |
|---|---|---|
| `FindPlanTemplate(VersionedCatalogReference)` | exact `Key == r.Key && Version == r.Version` | No |
| `FindRunLayout(VersionedCatalogReference)` | exact match | No |
| `FindLevelModifier(VersionedCatalogReference)` | exact match | No |
| `FindWorkoutProgression(VersionedCatalogReference)` | exact match | No |
| `FindProgressionModifier(VersionedCatalogReference)` | exact match | No |
| **`FindWorkout(string key, IRetirementLedger?)`** | **key-only, `OrderByDescending(Version).FirstOrDefault()`** | **Yes — the only auto-selecting method** |
| `FindRuntimeConditionValueRegistry(VersionedCatalogReference)` | exact match | No |
| `FindPeakVolumeBandPolicy(VersionedCatalogReference)` | exact match | No |
| `FindRulePack(VersionedCatalogReference)` | exact match | No |

`FindWorkout` is structurally isolated as the sole source of auto-version-selection in the entire
snapshot — confirming Findings 5 and 6 are a contained, well-scoped defect (two source fields, one
resolution method), not a systemic pattern spread across many lookups.

## Legacy-schema-only fields

None found. Every document type in `schemas/` has exactly one schema version (`schemaVersion: 1`)
currently in use across every catalog artifact — there is no schema evolution history in this catalog yet,
so the `LEGACY_SCHEMA_ONLY` classification does not apply to anything found.

## Summary table (all fields inspected)

| Field | Classification |
|---|---|
| `LevelModifierDefinition.EligibleWorkoutKeys` | ARTIFACT_DEPENDENCY_REQUIRES_VERSION |
| `WorkoutProgressionStageDefinition.WorkoutCandidateKeys` | ARTIFACT_DEPENDENCY_REQUIRES_VERSION |
| `PlanTemplateDefinition.WorkoutProgression` | not key-only (already `VersionedCatalogReference`) |
| `LevelModifierDefinition.ProgressionModifier` | not key-only (already `VersionedCatalogReference`) |
| `RulePackDefinition.RuntimeConditionValueRegistry` | not key-only (already `VersionedCatalogReference`) |
| `RulePackDefinition.PeakVolumeBandPolicy` | not key-only (already `VersionedCatalogReference`) |
| `RulePackDefinition.Policies` / `.Rules` | not key-only (already `List<VersionedCatalogReference>`) |
| `PlanTemplateDefinition.RequiredRules` | not key-only; separately flagged as a dual-ownership consistency defect (Finding 7) |
| `WorkoutProgressionStageDefinition.StageKey` / `.FallbackStageKey` | NON_DEPENDENCY_IDENTIFIER |
| `RuntimeConditionValueSet.AllowedValues` / `RuntimeEligibilityCondition.AllowedValues` | NON_DEPENDENCY_IDENTIFIER (literal values) |
| `WorkoutComponentDefinition.IntensityDescriptor` | NON_DEPENDENCY_IDENTIFIER (free-text label) |
| `PublishedTemplateBundle.BundleKey`, manifest key/version fields | NON_DEPENDENCY_IDENTIFIER (self-identity, not a reference) |

No field was classified `SEMANTIC_REQUIREMENT_KEY_ONLY` or `LEGACY_SCHEMA_ONLY` — every genuine key-only
field found (`EligibleWorkoutKeys`, `WorkoutCandidateKeys`) directly determines bundle content and
therefore requires version pinning, and no schema-version history exists yet to produce a legacy-only case.

## Confirmation

No code file, schema file, or catalog JSON artifact was modified to produce this report.
