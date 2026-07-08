# Cross-Release Hash Consistency Audit — CROSS-HASH-001

## Update (Part 3): 0.6.0-pilot required no new exception

The deterministic-graph candidate artifacts (`TEN_K_WORKOUT_PROGRESSION_V1 v2`, `INTERMEDIATE_MODIFIER v2`,
`TEN_K_MASTER v3`, `TEN_K__4D__INTERMEDIATE v4`) and the retirement of `v1`/`v2`/`v3` introduced **zero**
new cross-release-hash-consistency exceptions — every new artifact identity's hash is, by construction, its
first-ever publication, and every already-published identity's hash is unchanged. The full scan
(`CrossReleaseHashConsistencyTests`) still passes using only the exact historical exceptions already
registered in `cross-release-hash-exceptions.json` — no wildcard, no broadened exception was added. See
`deterministic-graph-part3-completion.md` and `zero-six-pilot-release-audit.md`.

## Update: the 5 newly-discovered defects have been remediated

The 5 previously-undetected mismatches this audit discovered (below) have since been actively remediated
by WORKOUT-IMMUT-001 and PEAK-POLICY-IMMUT-001 — each affected v1 artifact was restored to its exact
earliest historically-published content, and each corrected/intended content now lives on a genuinely new
v2 artifact. `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json` was updated accordingly:
the *canonical* direction for `WORKOUT_DEFINITION`/`PEAK_VOLUME_BAND_POLICY`/`PUBLISHED_TEMPLATE_BUNDLE` v1
now points at the restored (original) hash, with the mutated historical releases (`0.1.0-pilot` through
`0.4.0-pilot`, as applicable) registered as the anomalies — the reverse of this report's original framing
below, which registered the *mutated* value as canonical. The `TEMPLATE_COMBINATION v1` /
`0.3.0-pilot` defect (COMB-IMMUT-001) is unaffected by this update and remains registered exactly as
before. See `published-workout-immutability-remediation.md`, `peak-volume-policy-immutability-remediation.md`,
and `dependency-version-cascade-audit.md` for full detail. The findings below are preserved as the
original discovery record.

## Requirement

For every published artifact identity `(documentType, key, version)`, every historical release manifest
under `artifacts/appsel-plan-catalog/` must pin exactly one content hash, across all versioned artifact
types (`TemplateCombinationDefinition`, `PlanTemplateDefinition`, `RunLayoutDefinition`,
`LevelModifierDefinition`, `ProgressionModifierDefinition`, `WorkoutProgressionDefinition`,
`WorkoutDefinition`, `RulePackDefinition`, `PeakVolumeBandPolicy`,
`RuntimeConditionValueRegistryDefinition`) — plus published bundles.

## ⚠️ Major finding: five additional, previously undetected historical defects

A full repository-wide scan (this audit's whole purpose) was run for the first time across all five
released manifests. It confirms the **already-known** `TEMPLATE_COMBINATION/TEN_K__4D__INTERMEDIATE v1`
mismatch (`0.3.0-pilot`, see `combination-immutability-investigation.md`) — but it also surfaces **five
more identity groups with the exact same class of defect**, none of which were previously investigated,
because every prior audit in this project was scoped specifically to the `TEN_K__4D__INTERMEDIATE`
combination artifact, not to every artifact type repository-wide.

| # | Identity | Anomalous release(s) | Canonical hash (matches current source) | Root cause |
|---|---|---|---|---|
| 1 | `TEMPLATE_COMBINATION / TEN_K__4D__INTERMEDIATE / v1` | `0.3.0-pilot` | `c6324371a3...` | Already known — `masterTemplate` reference mutated in place (COMB-IMMUT-001). |
| 2 | `WORKOUT_DEFINITION / EASY_STANDARD / v1` | `1.0.0`, `0.1.0-pilot` | `8ddedec6...` | **NEW.** `catalog/workouts/easy-standard.v1.json` was edited in place — the domain-content vocabulary reconciliation pass added `allowedDistanceAccountingModes` (and corrected other fields) without a version bump, sometime before `0.2.0-pilot` was published. |
| 3 | `WORKOUT_DEFINITION / FARTLEK / v1` | `1.0.0`, `0.1.0-pilot` | `c6a27cc2...` | **NEW.** Same pattern as #2, `catalog/workouts/fartlek.v1.json`. |
| 4 | `WORKOUT_DEFINITION / LONG_RUN_STANDARD / v1` | `1.0.0`, `0.1.0-pilot` | `d428aed1...` | **NEW.** Same pattern as #2, `catalog/workouts/long-run-standard.v1.json`. |
| 5 | `WORKOUT_DEFINITION / THRESHOLD_TEMPO / v1` | `1.0.0`, `0.1.0-pilot` | `48504242...` | **NEW.** Same pattern as #2, `catalog/workouts/threshold-tempo.v1.json`. |
| 6 | `PEAK_VOLUME_BAND_POLICY / PEAK_VOLUME_BANDS_V1 / v1` | `1.0.0` | `d187758e...` | **NEW.** `catalog/policies/peak-volume-bands.v1.json` was edited in place (the 4-day INTERMEDIATE band-corroboration pass) between `1.0.0` and `0.1.0-pilot`, without a version bump. |
| 7 | `PUBLISHED_TEMPLATE_BUNDLE / TEN_K__4D__INTERMEDIATE / v1` | `1.0.0`, `0.1.0-pilot`, `0.3.0-pilot` | `3bc63d0f...` (= `0.2.0-pilot` and `0.4.0-pilot`) | **Derived, not independent** — the bundle hash is computed over the full resolved dependency closure, so it necessarily differs at every release boundary where #2–#6 above changed underlying content. Fully explained by, not additional to, the root causes above. |

**Confirmation these are real (not tooling artifacts)**: recomputed each artifact's content hash directly
from today's `catalog/` source using the project's own `CatalogDocumentHasher`/`SystemTextJsonCanonicalSerializer`
(the same code path production uses, via a throwaway console harness referencing
`PlanCatalog.Infrastructure` directly — not reimplemented). Every current-source hash matches the
*canonical* (newer) value in the table above exactly, confirming today's source already reflects the
post-mutation content and the divergence lives only in the two/three oldest, already-superseded release
directories.

**No immutable release directory was modified to make this discovery**, and **none was modified in
response to it** — `1.0.0`, `0.1.0-pilot`, `0.2.0-pilot`, and `0.3.0-pilot` are all already marked
`SUPERSEDED` in the release-status ledger (see `release-status-chain-audit.md`), and per this task's
explicit constraint ("do not rewrite history"), they are left exactly as published. This is intentionally
**not** remediated further in this task (e.g. no new `v2` workout/policy artifacts were created) — that
would be unrequested domain-content/release work outside Task B's scope (detect, register, guard). It is
flagged here prominently for the user's attention and any follow-up decision.

## Test added: `AllHistoricalReleases_SameArtifactIdentityAlwaysHasSameHash`

`tests/PlanCatalog.Tests/Publishing/CrossReleaseHashConsistencyTests.cs` scans every
`artifacts/appsel-plan-catalog/*/release-manifest.json` (both the `artifacts` and `bundles` arrays),
groups records by `(documentType, key, version)`, and fails with a rich diagnostic (every
`releaseVersion=hash` pair) for any group with more than one distinct hash that is not fully accounted for
by the exception registry. Companion tests:

| Test | Proves |
|---|---|
| `AllHistoricalReleases_SameArtifactIdentityAlwaysHasSameHash` | The full scan passes today, given the exception registry. |
| `ScanCoversAllExpectedArtifactTypesAndAtLeastTheKnownReleases` | The scan actually reaches all 5 releases and all 11 artifact/bundle document types — guards against a silent no-op scan. |
| `ExceptionRegistry_EveryAnomalyIsActuallyObservedInSomeRelease` | Every entry in `cross-release-hash-exceptions.json` corresponds to a real, observed (release, hash) pair — the registry cannot silently drift from reality. |
| `UnregisteredMismatch_FailsTheConsistencyCheck_UsingAnIsolatedFixture` | A fabricated, unregistered mismatch (in a fully isolated temp fixture, never the real artifacts tree) still fails the check — proves no wildcard/blanket acceptance. |

## Exception mechanism: `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json`

One entry per artifact identity; each entry declares `canonicalContentHash` plus a list of `anomalies`,
each naming the exact `releaseVersion`, `observedContentHash`, `reason`, `supersedingRelease`, and
`auditReference`. No wildcards, no ranges, no normalization of the bad hash — every field is an exact,
individually-justified string. All 7 anomalies found by the scan (6 concrete + 1 derived) are registered
this way; nothing else is. `tests/PlanCatalog.Tests/TestSupport/CrossReleaseHashExceptionRegistry.cs`
(test-side reader) and `src/PlanCatalog.Infrastructure/Repositories/FileSystemCrossReleaseHashExceptionRegistry.cs`
(production-side reader, used by the publish-time guard below) both parse this same file independently.

## Publish-time guard: verified gap, then closed

**Pre-existing state**: `PublishReadinessValidator.Validate` already detects a same-build duplicate
`(documentType, key, version)` with differing hashes **within one source snapshot** — proven for
`TemplateCombinationDefinition` specifically by the pre-existing
`tests/PlanCatalog.Tests/Publishing/CombinationImmutabilityTests.cs`
(`PublishReadinessValidator_RejectsSameKeyVersion_WithDifferingContentHash_ForCombinations`). This is a
real guard, but it only catches a conflict if both versions are simultaneously present in the *current*
source tree — it cannot catch "today's source, for an already-published identity, now resolves to a
different hash than what a *previous release* already pinned," which is exactly the vulnerability that
allowed `0.3.0-pilot` to publish a mutated `TEN_K__4D__INTERMEDIATE v1` in the first place.

**Gap closed**: `CatalogPublisher.BuildRelease` (`src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs`)
now calls a new `ValidateCrossReleaseHashConsistency` step after assembling the new release's artifact and
bundle reference lists. For every already-published release (via the new
`IPublishedArtifactRepository.ListReleaseVersions()` + `ReadManifest`), it compares every shared identity's
hash against what the new release would publish. A mismatch not covered by
`ICrossReleaseHashExceptionRegistry` throws `CatalogValidationException` with issue code
`PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION` — the **same** code the source-level guard already uses.
Because `Publish` only calls `WriteRelease` after `BuildRelease` succeeds, a rejected publish **writes no
partial release directory** (proven by test).

`CatalogPublisher`'s new `ICrossReleaseHashExceptionRegistry?` constructor parameter defaults to
`NullCrossReleaseHashExceptionRegistry.Instance` (nothing is a known exception, so every mismatch is
rejected) — existing test call sites and the CLI (`InfrastructureFactory.CreatePublisher()`, now wired to
`FileSystemCrossReleaseHashExceptionRegistry` reading the real exceptions file) both continue to work
without other changes.

### Tests added: `tests/PlanCatalog.Tests/Publishing/PublishTimeCrossReleaseHashGuardTests.cs`

| Test | Proves |
|---|---|
| `Publish_SameContentTwiceUnderDifferentReleaseVersions_NeverConflicts` | Baseline: republishing identical, unchanged source never trips the guard. |
| `Publish_WithMutatedDependencyUnderSameVersion_IsRejected_AndWritesNoPartialRelease` | A dependency mutated in place under the same version is rejected with `PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION`; no partial release directory is written; the earlier, correct release is unaffected. |
| `Publish_WithMutatedDependency_ButRegisteredAsAnException_Succeeds` | An explicitly registered exception (exact identity + release + hash) lets the publish through — proving the escape hatch works precisely, not as a wildcard. |
| `RealCatalog_BuildReleasePreview_UsingRealExceptionRegistry_HasNoUnexpectedCrossReleaseMismatches` | Against the **real** `catalog/`, the real `artifacts/appsel-plan-catalog/` releases, and the real `cross-release-hash-exceptions.json`, a fresh `BuildPreview` succeeds cleanly today — the pre-existing historical defects are fully accounted for and do not block a legitimate new publish. |

All isolated tests use in-memory/fake fixtures and disposable temp directories — none touch the real
catalog or the real artifacts tree except the last, read-only preview test, which performs no write.

## Regression verification

- `dotnet build -c Release`: 0 warnings, 0 errors.
- `dotnet test -c Release`: **171/171 passing** (up from 163 after Task A).
- `verify-release` for all five existing releases: **all PASSED**, unchanged.
- No immutable release directory was modified.

## Final status: `CROSS_RELEASE_HASH_CONSISTENCY` = **IMPLEMENTED, with 5 additional pre-existing historical defects discovered and explicitly registered (not remediated further — flagged for follow-up)**
