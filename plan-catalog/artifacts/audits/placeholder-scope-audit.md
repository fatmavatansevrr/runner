# Placeholder Scope Audit — Part 3 Step 4 — PLACEHOLDER-SCOPE-001

Recomputed from the **final, post-retirement** graph (not copied from Part 2). Active root:
`TEN_K__4D__INTERMEDIATE v4` (the sole non-retired, publish-eligible version as of this task — see
`active-version-uniqueness-audit.md`). Method: `PlanCatalog.Core.Audit.BlockerScopeMeasurement`
(`src/PlanCatalog.Core/Audit/BlockerScopeMeasurement.cs`), which reads
`PilotDomainContentAudit.Entries.Where(Classification == PlaceholderUnconfirmed)` and groups by exact
`(documentType, key, version)` identity. **Decision-level and artifact-level counts are never compared to
each other** — an artifact with 5 blocking fields contributes 5 to decision-level and 1 to artifact-level.

## Decision-level metrics

| Metric | Value | Exact query | Retired roots excluded? | Active root version | Dependency closure used |
|---|---:|---|---|---|---|
| `totalCatalogPlaceholderDecisionCount` | **36** | `BlockerScopeMeasurement.TotalCatalogPlaceholderDecisionCount()` — count of all `PilotDomainContentAudit.Entries` with `Classification == PlaceholderUnconfirmed`, no scope filter | No — includes retired-root-only entries | n/a | n/a (unscoped) |
| `eligibleReleaseUnionBlockingDecisionCount` | **13** | `ScopedDecisionCount(union of every non-retired combination's exact bundle-dependency tuples)` | **Yes** — only `TEN_K__4D__INTERMEDIATE v4` is non-retired post-Step-3, so the union now equals exactly one combination's closure | `TEN_K__4D__INTERMEDIATE v4` (currently the only eligible combination) | `v4`'s full exact dependency closure |
| `activeRootClosureBlockingDecisionCount` | **13** | `ScopedDecisionCount(TEN_K__4D__INTERMEDIATE v4's exact bundle-dependency tuples)` | Yes (n/a — single named root) | `TEN_K__4D__INTERMEDIATE v4` | `TEN_K_MASTER v3`, `TEN_K_WORKOUT_PROGRESSION_V1 v2`, `RUN_LAYOUT_4D v1`, `INTERMEDIATE_MODIFIER v2`, `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1`, `APPSEL_RACE_PLAN_V1 v2`, `RUNTIME_CONDITION_VALUES_V1 v1`, `PEAK_VOLUME_BANDS_V1 v2`, `EASY_STANDARD v2`, `FARTLEK v2`, `GOAL_PACE_TEN_K v1`, `LONG_RUN_STANDARD v2`, `THRESHOLD_TEMPO v2` |
| `historicalOnlyPlaceholderDecisionCount` | **23** | `TotalCatalogPlaceholderDecisionCount() - ScopedDecisionCount(eligibleReleaseUnion)` — entries whose identity is unreachable from any currently-eligible bundle | Yes (by construction — this scope is specifically the complement of the eligible union) | n/a | n/a |

**Note**: `eligibleReleaseUnionBlockingDecisionCount` now numerically equals `activeRootClosureBlockingDecisionCount`
(13 = 13) because retirement collapsed the eligible set to exactly one combination — before retirement
(Part 2), the eligible union was **24** (spanning v1+v2+v3+v4's combined closures) while the single-root
closure was already 13. This convergence is expected and is itself a correctness signal, not a
coincidence: `eligibleReleaseUnionBlockingDecisionCount` is defined as a union over whatever the eligible
set currently is, and that set is now a singleton.

## Artifact-level metrics

| Metric | Value | Exact query |
|---|---:|---|
| `totalCatalogBlockingArtifactCount` | **17** | `BlockerScopeMeasurement.TotalCatalogBlockingArtifactCount()` — distinct `(documentType,key,version)` identities across the whole audit list |
| `eligibleReleaseUnionBlockingArtifactCount` | **9** | `ScopedArtifactCount(eligible union)` |
| `activeRootClosureBlockingArtifactCount` | **9** | `ScopedArtifactCount(v4 closure)` |
| `historicalOnlyBlockingArtifactCount` | **8** | `HistoricalOnlyArtifactCount(eligible union)` |

## Production readiness — which scope to use

- **Targeted active-root publishing** (e.g. `publish --version X --channel Production` for a specific
  combination): use `activeRootClosureBlockingDecisionCount` (**13**).
- **Full-catalog Production publishing** (`CatalogPublisher.Publish` with `channel: Production`, which
  packages every eligible combination): use `eligibleReleaseUnionBlockingDecisionCount` (**13** — currently
  identical to the active-root count because there is only one eligible root).
- **`totalCatalogPlaceholderDecisionCount` (36) must never be used as an active-root or Production blocker
  count** — this was exactly the conflation Part 1 Finding 3 identified in
  `ten-k-pilot-domain-review-summary.md`, now corrected everywhere (see Step 4 document updates below).

## Stale-wording corrections made in this pass

| File | Before | After |
|---|---|---|
| `ten-k-pilot-domain-review-summary.md` | "36 blocking placeholders remain in the dependency closure" (flat, undifferentiated) | Three explicitly separated numbers: total-catalog (36), eligible-release-union (13, post-retirement), active-root-closure (13) |
| `ten-k-pilot-domain-review-summary.md` | "`catalog/combinations/ten-k-4d-intermediate.v1.json` now points its `masterTemplate` reference at v2" (stale, contradicted by the restored v1 content) | "v1 → `TEN_K_MASTER v1` (unchanged); v2 → `TEN_K_MASTER v2`; v3 → `TEN_K_MASTER v2` + `APPSEL_RACE_PLAN_V1 v2`; v4 (active) → `TEN_K_MASTER v3` + `APPSEL_RACE_PLAN_V1 v2`, exact-versioned dependencies" |

## Production readiness error contract update

The `activeRootClosureBlockingArtifactCount = 9` figure above is exactly what the Production negative
publish test's top-level error count (9) equals — the Production readiness guard groups errors by
artifact identity, not by decision. This is a distinct measurement from
`activeRootClosureBlockingDecisionCount = 13`; the two must never be compared. See
`artifacts/audits/production-readiness-error-contract-audit.md` for the full trace establishing and
now structurally enforcing (via 15 new tests) that `errorCount == blockingArtifactCount`, never
`blockingDecisionCount`, and that all 13 decisions are preserved in structured form nested under the 9
errors (not merely inside a concatenated message string, which was a defect corrected in that task).

## Final status: `PLACEHOLDER_SCOPE_ACCURACY` = **CORRECTED**
