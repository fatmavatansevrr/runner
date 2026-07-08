# Published Workout Immutability Remediation — WORKOUT-IMMUT-001

## Scope

Remediates the 4 `WORKOUT_DEFINITION` in-place mutations discovered by `cross-release-hash-consistency-audit.md`:
`EASY_STANDARD`, `FARTLEK`, `LONG_RUN_STANDARD`, `THRESHOLD_TEMPO` — all v1.

## Reconstruction table

| Artifact | Release | Declared version | Hash | Exact content source |
|---|---|---:|---|---|
| EASY_STANDARD | 1.0.0 | 1 | `37990edc...` | `EFFORT_BASED`, no `allowedDistanceAccountingModes` |
| EASY_STANDARD | 0.1.0-pilot | 1 | `37990edc...` | same as 1.0.0 |
| EASY_STANDARD | 0.2.0-pilot | 1 | `8ddedec6...` | **mutated**: `DISTANCE` + `allowedDistanceAccountingModes: [EXACT_SESSION_TOTAL]` |
| EASY_STANDARD | 0.3.0-pilot, 0.4.0-pilot | 1 | `8ddedec6...` | same mutated content republished |
| FARTLEK | 1.0.0, 0.1.0-pilot | 1 | `8652ed9a...` | `EFFORT_BASED`, no accounting modes |
| FARTLEK | 0.2.0-pilot, 0.3.0-pilot, 0.4.0-pilot | 1 | `c6a27cc2...` | **mutated**: `MIXED` + `[ESTIMATED_SESSION_TOTAL]` |
| LONG_RUN_STANDARD | 1.0.0, 0.1.0-pilot | 1 | `92d43af5...` | `EFFORT_BASED`, no accounting modes |
| LONG_RUN_STANDARD | 0.2.0-pilot, 0.3.0-pilot, 0.4.0-pilot | 1 | `d428aed1...` | **mutated**: `DISTANCE` + `[EXACT_SESSION_TOTAL]` |
| THRESHOLD_TEMPO | 1.0.0, 0.1.0-pilot | 1 | `3da4f960...` | `PACE_BASED, EFFORT_BASED`, no accounting modes |
| THRESHOLD_TEMPO | 0.2.0-pilot, 0.3.0-pilot, 0.4.0-pilot | 1 | `48504242...` | **mutated**: `MIXED` + `[ESTIMATED_SESSION_TOTAL]` |

## Findings

1. **Earliest historically published v1 content**: the `1.0.0`/`0.1.0-pilot` content (legacy `EFFORT_BASED`/`PACE_BASED` prescription modes, no `allowedDistanceAccountingModes` field at all).
2. **First release with mutated v1 content**: `0.2.0-pilot`, for all 4 keys simultaneously — this is when the domain-content vocabulary reconciliation pass (an earlier task) edited `catalog/workouts/*.v1.json` in place instead of creating v2 files.
3. **Current live source content (before this remediation)**: matched the mutated (0.2.0-pilot-onward) content exactly.
4. **Is the current content semantically intended?** Yes — `DISTANCE`/`MIXED` prescription modes and the `allowedDistanceAccountingModes` field are independently corroborated by Golden Fixture v3 (`docs/canonical/golden-fixture-v3/`), per the prior domain-content review. Per this task's explicit instruction ("do not assume the latest content is automatically correct merely because it is newer"), this was verified against the fixture, not assumed — see `ten-k-pilot-domain-decision-audit.md` AUD-200..259 series.
5. **Every active reference that must move to v2**: `CatalogBundleAssembler`'s `FindWorkout` resolution (see Dependency Cascade section below) — no other artifact references a workout by explicit version (progression/level-modifier reference by key only).

## Domain classification of replacement content (v2)

Versioning fixes provenance; it does not upgrade domain confidence. Per-field classification (unchanged from before this remediation, just correctly re-attached to the artifact that actually carries each value now):

| Field | v1 (restored) | v2 (corrected) |
|---|---|---|
| `family` | CANONICAL_CONFIRMED (unchanged, brief §12.6 + fixture) | CANONICAL_CONFIRMED (identical — field never differed) |
| `eligiblePhases` | CANONICAL_CONFIRMED (unchanged, fixture) | CANONICAL_CONFIRMED (identical — field never differed) |
| `allowedPrescriptionModes` | **PLACEHOLDER_UNCONFIRMED** (restored legacy value, never fixture-corroborated) | **CANONICAL_CONFIRMED** (Golden Fixture v3, exact match) |
| `allowedDistanceAccountingModes` | absent (TECHNICAL_ONLY — field never existed on the true v1 schema shape) | **CANONICAL_CONFIRMED** (Golden Fixture v3, new field) |
| `complexityTier` | PLACEHOLDER_UNCONFIRMED (unchanged — authoring-only, no fixture evidence) | PLACEHOLDER_UNCONFIRMED (same, unresolved) |
| `components` | PLACEHOLDER_UNCONFIRMED (unchanged — ownership of generated-output labels unresolved) | PLACEHOLDER_UNCONFIRMED (same, unresolved) |

No generated-fixture dosage (per-instance scheduled values) was copied into either version — only reusable, workout-key-level facts (family/phases/prescription mode/accounting mode) were used, matching the "do not copy generated fixture dosage into the v2 reusable definition" instruction.

## Correction applied

- `catalog/workouts/easy-standard.v1.json`, `fartlek.v1.json`, `long-run-standard.v1.json`, `threshold-tempo.v1.json` — **restored** to their exact `1.0.0`/`0.1.0-pilot` content. Recomputed hashes match the earliest historical hashes exactly (proven by `WorkoutArtifactImmutabilityTests.RestoredWorkoutV1_MatchesEarliestHistoricalReleaseHash`).
- `catalog/workouts/easy-standard.v2.json`, `fartlek.v2.json`, `long-run-standard.v2.json`, `threshold-tempo.v2.json` — **created**, each `metadata.version: 2`, carrying the corrected/intended content, each with a genuinely new, deterministic content hash distinct from its v1 sibling.
- No historical release directory was modified.

## Reference resolution: deterministic and retirement-aware (required proof)

`WorkoutProgressionDefinition.PhaseProgressions[*].Stages[*].WorkoutCandidateKeys` and `LevelModifierDefinition.EligibleWorkoutKeys` reference workouts **by key only** — never by version. Resolution happens in `CatalogSourceSnapshot.FindWorkout(key)`, consulted from `CatalogBundleAssembler.Assemble`.

**Defect found**: `FindWorkout` used `Workouts.FirstOrDefault(x => x.Metadata.Key == key)` — an arbitrary, list-order-dependent match with **no version disambiguation and no retirement awareness**. This was harmless while only one version of each workout existed, but became a real correctness gap the moment a second version was introduced by this remediation.

**Fix applied** (`src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs`):
```csharp
public WorkoutDefinition? FindWorkout(string key, IRetirementLedger? retirementLedger = null)
{
    var retirement = retirementLedger ?? NullRetirementLedger.Instance;
    return Workouts
        .Where(x => x.Metadata.Key == key && !retirement.IsRetired(x.Metadata.DocumentType, x.Metadata.Key, x.Metadata.Version))
        .OrderByDescending(x => x.Metadata.Version)
        .FirstOrDefault();
}
```
Deterministic (highest non-retired version by key — ties are structurally impossible since `GRAPH_DUPLICATE_KEY_VERSION` already forbids two documents sharing the same `(documentType, key, version)`), and retirement-aware (a retired version is never selected). `CatalogBundleAssembler.Assemble` was updated to pass its `retirement` ledger through. Proven by `WorkoutArtifactImmutabilityTests.CurrentActiveResolution_SelectsV2_DeterministicallyAndRetirementAware` (also proves the retirement fallback path).

Other `FindWorkout` call sites (`LevelModifierValidator`, `WorkoutProgressionValidator`, a structural test) use the new optional parameter's default (`null` → nothing retired) and are otherwise unaffected — they only check existence, not version selection.

## Tests added (`tests/PlanCatalog.Tests/Golden/WorkoutArtifactImmutabilityTests.cs`)

| Test | Proves |
|---|---|
| `RestoredWorkoutV1_MatchesEarliestHistoricalReleaseHash` (×4) | v1's restored hash matches the earliest historical release exactly. |
| `WorkoutV2_HasDistinctDeterministicHash` (×4) | v2's hash is stable across repeated computation and differs from v1. |
| `V1AndV2_CoexistWithoutDuplicateKeyVersionErrors` (×4) | No `GRAPH_DUPLICATE_KEY_VERSION` issue; exactly 2 versions exist per key. |
| `CurrentActiveResolution_SelectsV2_DeterministicallyAndRetirementAware` (×4) | `FindWorkout` resolves v2 by default, and correctly falls back to v1 once v2 is retired. |
| `HistoricalReleases_StillResolveTheirOriginallyPublishedWorkoutV1Content` | Historical release files are untouched and still carry their original hashes. |

All pass. Full suite: 203/203.

## Files changed

- `catalog/workouts/easy-standard.v1.json`, `fartlek.v1.json`, `long-run-standard.v1.json`, `threshold-tempo.v1.json` (restored)
- `catalog/workouts/easy-standard.v2.json`, `fartlek.v2.json`, `long-run-standard.v2.json`, `threshold-tempo.v2.json` (new)
- `src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs` (`FindWorkout` determinism/retirement fix)
- `src/PlanCatalog.Infrastructure/Publishing/CatalogBundleAssembler.cs` (pass ledger through)
- `src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` (re-attached classifications to correct version)
- `tests/PlanCatalog.Tests/Golden/WorkoutArtifactImmutabilityTests.cs` (new)
- `tests/PlanCatalog.Tests/Golden/PilotWorkoutFixtureConfirmationTests.cs`, `PilotCatalogStructuralTests.cs`, `tests/PlanCatalog.Tests/Validation/PilotDomainContentAuditTests.cs`, `tests/PlanCatalog.Tests/Publishing/CatalogPublisherTests.cs` (updated for the v1/v2 split and 3-combination-version bundle count)
- `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json` (canonical direction corrected; see `cross-release-hash-consistency-audit.md`)

## Final status: `WORKOUT_ARTIFACT_IMMUTABILITY` = **REMEDIATED**
