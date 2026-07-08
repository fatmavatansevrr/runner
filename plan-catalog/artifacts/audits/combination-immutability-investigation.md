# Combination Artifact Immutability Investigation — COMB-IMMUT-001

**Original ambiguous report wording**: `catalog/combinations/ten-k-4d-intermediate.v1.json (version bump)`

**Finding: v1 WAS mutated in place.**

## What was found

`catalog/combinations/ten-k-4d-intermediate.v1.json` still declared `metadata.version: 1` (filename and
version unchanged), but its `masterTemplate.version` had been changed from `1` to `2` directly in that
file — no new `v2` artifact was ever created. This is exactly the "Invalid state" described in the audit
request: a published, versioned artifact's semantic content was changed without a version bump.

### Hash evidence

| Item | Value |
|---|---|
| Historically pinned v1 hash (releases `1.0.0`, `0.1.0-pilot`, `0.2.0-pilot`) | `c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71` |
| Hash pinned by `0.3.0-pilot` for the same `(TEMPLATE_COMBINATION, TEN_K__4D__INTERMEDIATE, 1)` tuple | `e976d10e4015981fdcdc925312502d12b92e36cc8da73d89775b672f03d0ab57` |
| **Match?** | **No — confirmed hash mismatch under the same declared version.** |

`0.3.0-pilot`'s own `release-manifest.json` is the direct evidence: it pins combination version `1` with
the second hash, while three earlier releases pin version `1` with the first hash. Same `(documentType,
key, version)` tuple, two different hashes — a real immutability violation, baked into an already-published
release.

## Decision: Outcome C

0.3.0-pilot incorrectly pinned combination v1 with mutated content. Per the decision rules:

- `0.3.0-pilot`'s release directory was **preserved unchanged** — not rewritten, not deleted.
- `catalog/combinations/ten-k-4d-intermediate.v1.json` was **restored** to its exact historical content
  (`masterTemplate.version: 1`, `status: "VALIDATED"`, no `contentHash` in the draft source). Recomputing
  its content hash reproduces `c6324371a352a78d744583ee6bd0d36bd434b9214ff46d5ecf107e2656876c71` exactly —
  proven by a new test, `CombinationV1_ContentHash_MatchesHistoricallyPublishedHash`.
- `catalog/combinations/ten-k-4d-intermediate.v2.json` was **created** — a genuinely new, independently
  versioned artifact with `metadata.version: 2` and `masterTemplate.version: 2` (all other references
  unchanged).
- A new pilot release, `0.4.0-pilot`, was built and published, correctly pinning combination **v2**.
- `0.3.0-pilot` was marked `SUPERSEDED` in the release-status ledger (not mutated) once `0.4.0-pilot`
  verified successfully.

## Release impact

| Release | Combination version pinned | Hash | masterTemplate ref | Status |
|---|---:|---|---:|---|
| `1.0.0` | 1 | `c6324371a3...` | 1 | consistent, unaffected |
| `0.1.0-pilot` | 1 | `c6324371a3...` | 1 | consistent, unaffected |
| `0.2.0-pilot` | 1 | `c6324371a3...` | 1 | consistent, unaffected |
| `0.3.0-pilot` | 1 (mislabeled) | `e976d10e...` | 2 | **defective — preserved as-is, marked SUPERSEDED** |
| `0.4.0-pilot` (new) | 2 (correct) | `b3dab01388bfac1de820efa3649007e1bf3cfa1d4980e4070cd9cbacd15e8594` — deterministic, verified identical across source/bundle/release (see `combination-v2-hash-and-closure-audit.md`) | 2 | corrected, current active Pilot release |

## Modeling rule preserved

`WorkoutFamily.Taper` was never introduced by this correction; `PhaseKey` and `WorkoutFamily` remain
unchanged and independent (this investigation only concerns `TemplateCombinationDefinition` versioning).

## Files changed

- `catalog/combinations/ten-k-4d-intermediate.v1.json` — restored to original historical content.
- `catalog/combinations/ten-k-4d-intermediate.v2.json` — new.
- `src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` — ambiguous wording replaced; `AUD-007`
  rationale corrected; new `AUD-055` entry for combination v2.
- `tests/PlanCatalog.Tests/Golden/TaperPhaseFamilyEligibilityTests.cs` — single-combination assumption
  split into v1-specific and v2-specific tests, plus hash-equality/hash-matches-history tests.
- `tests/PlanCatalog.Tests/Publishing/CatalogPublisherTests.cs` — updated to expect 2 bundles (v1 + v2)
  instead of 1.
- `tests/PlanCatalog.Tests/Publishing/CombinationImmutabilityTests.cs` — new; proves
  `PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION` and `GRAPH_DUPLICATE_KEY_VERSION` fire for
  `TemplateCombinationDefinition`, not just `PlanTemplateDefinition`.

## Final immutability status

**RESOLVED.** `v1` is restored to its exact historically-published content; `v2` exists as a distinct,
independently-hashed artifact; no historical release directory was rewritten; the defective `0.3.0-pilot`
release was preserved as an immutable (if imperfect) historical artifact and marked superseded via the
release-status ledger rather than mutated.

## Update: hardened against recurrence (RETIRE-PKG-001 / CROSS-HASH-001)

A follow-up hardening pass (see `full-catalog-retirement-packaging-audit.md` and
`cross-release-hash-consistency-audit.md`) added a **publish-time cross-release hash guard**
(`CatalogPublisher.ValidateCrossReleaseHashConsistency`) that would have rejected the `0.3.0-pilot` publish
outright, before it ever wrote a release directory, had it existed at the time — it compares every
artifact/bundle a new release would publish against every already-published release's manifest for the
same `(documentType, key, version)` identity and rejects on any unregistered mismatch with
`PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION`.

The repository-wide scan performed for that hardening pass also confirms this `TEMPLATE_COMBINATION v1`
mismatch is registered in `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json` — alongside
five additional, previously undiscovered mismatches of the same in-place-mutation class affecting
`WORKOUT_DEFINITION` (`EASY_STANDARD`, `FARTLEK`, `LONG_RUN_STANDARD`, `THRESHOLD_TEMPO`, all v1) and
`PEAK_VOLUME_BAND_POLICY` (`PEAK_VOLUME_BANDS_V1` v1), plus the fully-explained derived
`PUBLISHED_TEMPLATE_BUNDLE/TEN_K__4D__INTERMEDIATE` v1 mismatch. See
`cross-release-hash-consistency-audit.md` for full detail — those five are **not** remediated by this
investigation; they are explicitly registered as known historical defects and flagged for follow-up.
