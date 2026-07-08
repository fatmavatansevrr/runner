# Full-Catalog Retirement Packaging Audit — RETIRE-PKG-001

## Update (Part 3): the fix proven here is now exercised for real

The fix this report validated with isolated fixtures is now proven against real production data:
`TEN_K__4D__INTERMEDIATE v1`/`v2`/`v3` were retired via the real retirement ledger in Part 3 (see
`retirement-ledger-application.md`), and `0.6.0-pilot` (published in Part 3) correctly contains exactly one
`TEN_K__4D__INTERMEDIATE` bundle (`v4`) — no retired-version bundle was packaged. See
`deterministic-graph-part3-completion.md` and `zero-six-pilot-release-audit.md`.

## Question

Does `CatalogPublisher`'s full-catalog packaging path exclude a combination that is **itself retired**
(not merely one with a retired *dependency*, which was already handled) from new releases?

## Finding (before fix): NO — confirmed defect

`CatalogPublisher.BuildRelease` iterated **every** combination present in the full catalog source
snapshot with no eligibility filter of any kind, and `CatalogBundleAssembler.Assemble` only checked
retirement of a combination's 8 *dependency* objects (`master`, `layout`, `levelModifier`, `rulePack`,
`progression`, `progressionModifier`, `registry`, `peakPolicy`) — never the combination's **own**
`Metadata` (its own documentType/key/version). A retired combination would therefore still receive a
freshly-assembled bundle and be republished as an "active" artifact in every new release.

## Call-path inspection table

| Stage | Implementation | Retirement consulted (before fix) | Behavior for a retired combination (before fix) |
|---|---|---|---|
| Source enumeration | `FileSystemCatalogSourceRepository.LoadSnapshot` (`src/PlanCatalog.Infrastructure/Repositories/FileSystemCatalogSourceRepository.cs`) | No | Loaded unconditionally — correct, this layer must not filter (needed for historical verification and audit). |
| Combination eligibility filtering (pre-bundle) | `CatalogPublisher.BuildRelease` (`stamped.Combinations.Select(...)`) | **No** | **DEFECT: every combination, retired or not, got a bundle assembled and included in the new release.** |
| Domain/graph validation | `CatalogGraphValidator.Validate` → `TemplateCombinationValidator.Validate` | Only for the `progressionModifier` dependency (`TC_PROGRESSION_MODIFIER_RETIRED`) | Did not block a retired combination itself; not designed to (validation runs on full source, including historical combinations). |
| Bundle assembly | `CatalogBundleAssembler.Assemble` | Only for the 8 `dependencyMetadata` objects | **DEFECT: `combination.Metadata` itself was never checked.** |
| Release manifest / file packaging | `CatalogPublisher.BuildReleaseFiles` / `AllMetadata` | No | Republished the retired combination's source file and bundle into the new release's `combinations/` and `bundles/` folders, indistinguishable from an active one. |
| CLI explicit single-bundle request | `ReleaseCommands.BuildBundle` → `CatalogBundleAssembler.Assemble` | Same as bundle assembly | Would have silently succeeded for an explicitly-requested retired combination. |

## Fix applied (smallest appropriate change, reusing `IRetirementLedger`)

1. **`CatalogBundleAssembler.Assemble`** (`src/PlanCatalog.Infrastructure/Publishing/CatalogBundleAssembler.cs`):
   added a check for `combination.Metadata` retirement immediately after resolving the combination and
   before resolving any dependency. Throws `InvalidOperationException` whose message begins with
   `RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE`. This is the defense-in-depth layer and is what
   makes an **explicit** request for a retired combination (e.g. CLI `build-bundle`) fail loudly.
2. **`CatalogPublisher.BuildRelease`** (`src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs`):
   added `eligibleCombinations = stamped.Combinations.Where(c => !retirementLedger.IsRetired(...))` and
   uses this filtered list (via `stampedForRelease = stamped with { Combinations = eligibleCombinations }`)
   for bundle assembly, the release manifest's `Artifacts` list, and the release's packaged files. This is
   the full-catalog packaging path's silent-exclusion layer: a retired combination is simply never
   selected as a root for a **new** release, with no error (matching the required behavior — an
   automatic full-catalog build should skip retired combinations, not fail because one exists).
   Schema validation and domain/graph validation still run against the **unfiltered** original snapshot,
   so retired combinations still get validated for schema/graph correctness (needed for audit and to
   catch corruption), they are just excluded from what gets *newly published*.

No source files were deleted. `catalog/combinations/*.json` is untouched by this change; a retired
combination's file remains on disk and remains part of every **historical** release it was already
published in.

## Tests added (`tests/PlanCatalog.Tests/Publishing/FullCatalogRetirementPackagingTests.cs`)

Uses a fully isolated, in-memory `ICatalogSourceRepository` fake built from `CombinationFixture` plus a
second, independently-keyed combination (`RETIREMENT_TEST_COMBINATION`) sharing the same valid
dependency graph — never the real permanent catalog data, and never the real `retirements.json` ledger.

| Test | Proves |
|---|---|
| `BuildRelease_WithNoRetiredCombinations_PackagesBothAsBundlesAndArtifacts` | Baseline: with nothing retired, both combinations get bundles and artifact entries. |
| `BuildRelease_WithOneCombinationRetired_ExcludesItFromBundlesAndArtifacts_ButKeepsTheOtherActive` | The retired combination is excluded from both `Bundles` and `Artifacts`; the active one is unaffected. |
| `Publish_WithOneCombinationRetired_WritesReleaseWithOnlyTheActiveCombinationBundle` | End-to-end: the written release directory contains no bundle file for the retired combination, and does contain one for the active combination. |
| `BuildBundle_ExplicitlyRequestingARetiredCombination_ThrowsWithSuggestedCode` | Explicit single-bundle request for a retired combination throws with `RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE` in the message. |
| `BuildBundle_ExplicitlyRequestingANonRetiredCombination_StillSucceeds` | The new check has no effect on a non-retired combination. |
| `RetiredCombinationSourceFile_IsNotDeleted_AndRemainsInSnapshot` | Source enumeration still loads the retired combination (no deletion, no discovery-level filtering). |
| `HistoricalRelease_BuiltBeforeRetirement_StillVerifiesAfterCombinationIsLaterRetired` | A release built and published *before* a combination was retired keeps its bundle for that combination, byte-for-byte identical, even after the combination is later retired and a new release is built that excludes it. |

All 7 new tests pass. Full suite: **163/163 passing** (up from 156).

## Regression verification against the real repository

- `dotnet build -c Release`: succeeded, 0 warnings, 0 errors.
- `dotnet test -c Release`: 163/163 passed.
- `verify-release` for all five existing releases (`1.0.0`, `0.1.0-pilot`, `0.2.0-pilot`, `0.3.0-pilot`,
  `0.4.0-pilot`) against the real `artifacts/appsel-plan-catalog/` tree: **all PASSED**, unchanged.
- `build-release --version preview-check --channel Pilot --allow-unconfirmed-content` against the real
  `catalog/` tree still produces exactly 2 `PUBLISHED_TEMPLATE_BUNDLE` artifacts (v1 + v2 of
  `TEN_K__4D__INTERMEDIATE`) — identical to before the fix, because `artifacts/appsel-plan-catalog/retirements.json`
  does not exist (nothing is currently retired in the real catalog). This confirms the fix is a true no-op
  for the current, non-retired production data and only changes behavior once a combination is actually
  retired.
- The real, permanent pilot catalog data was **not** modified and no combination in it was retired to
  produce this proof — all retirement scenarios were exercised against the isolated in-memory fixture
  described above.

## Invariant now enforced

| Requirement | Status |
|---|---|
| A retired combination remains in source/historical releases for audit | ✅ unaffected — no source deletion, no discovery filtering |
| A retired combination may still be used to verify historical releases | ✅ proven by `HistoricalRelease_BuiltBeforeRetirement_StillVerifiesAfterCombinationIsLaterRetired` |
| A retired combination must NOT produce a bundle in any newly built release | ✅ `CatalogPublisher.BuildRelease` filters it out |
| A retired combination must not appear as an active/newly-published root combination | ✅ excluded from the new release's `Artifacts` and `Bundles` |
| A retired combination must not be selected by default | ✅ full-catalog packaging silently skips it |
| Explicit requests for a retired combination fail clearly | ✅ `RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE` from `CatalogBundleAssembler.Assemble` |
| No duplicate retirement mechanism introduced | ✅ reuses the existing `IRetirementLedger` port exclusively |

## Final status: `FULL_CATALOG_RETIREMENT_FILTERING` = **FIXED**
