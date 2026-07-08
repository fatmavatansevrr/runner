# TEN_K__4D__INTERMEDIATE v2 — Hash Stability & Dependency Closure Audit — COMB-V2-HASH-CLOSURE-001

## Inspection table

| Location | Key | Version | Recomputed hash | Pinned hash | Role |
|---|---|---:|---|---|---|
| `catalog/combinations/ten-k-4d-intermediate.v1.json` | TEN_K__4D__INTERMEDIATE | 1 | `c6324371a3...` | `c6324371a3...` (1.0.0, 0.1.0-pilot, 0.2.0-pilot, 0.4.0-pilot v1 bundle) | HISTORICAL_ONLY |
| `catalog/combinations/ten-k-4d-intermediate.v2.json` | TEN_K__4D__INTERMEDIATE | 2 | `b3dab0138...` | `b3dab0138...` (0.4.0-pilot) | **ROOT_COMBINATION** |
| `0.4.0-pilot/bundles/TEN_K__4D__INTERMEDIATE.v1.json` | TEN_K__4D__INTERMEDIATE | 1 | n/a | `c6324371a3...` | EXTRA_PACKAGED_ARTIFACT |
| `0.4.0-pilot/bundles/TEN_K__4D__INTERMEDIATE.v2.json` | TEN_K__4D__INTERMEDIATE | 2 | `b3dab0138...` | `b3dab0138...` | **ROOT_COMBINATION** |

## Hash verification — all four points match

| Point of computation | Hash |
|---|---|
| 1. Directly from source artifact (`catalog/combinations/ten-k-4d-intermediate.v2.json`) | `b3dab01388bfac1de820efa3649007e1bf3cfa1d4980e4070cd9cbacd15e8594` |
| 2. During bundle assembly (`CatalogBundleAssembler.Assemble`) | `b3dab01388bfac1de820efa3649007e1bf3cfa1d4980e4070cd9cbacd15e8594` |
| 3. Stored in `0.4.0-pilot`'s published copy | `b3dab01388bfac1de820efa3649007e1bf3cfa1d4980e4070cd9cbacd15e8594` |
| 4. Through `verify-release 0.4.0-pilot` (whole-file `checksums.sha256` recomputation) | PASSED — file bytes match exactly |

**All four identical. Verdict: STABLE_DETERMINISTIC.**

Confirmed independent of:
- release version/channel (computed identically for `0.4.0-pilot` vs. a fresh in-memory build)
- wall-clock time (re-verified after a real 1.1-second delay in a fresh process — identical hash)
- repeated computation (3 consecutive computations in the same process — identical)
- draft vs. stamped state (drafts carry no `contentHash`; stamping adds `Status=Published` + `ContentHash`,
  which the hash computation itself excludes by design — so pre- and post-stamp hashing of the same
  content is identical)

No accidental inclusion of `generatedAt`, `publishedAt`, `releaseVersion`, `releaseChannel`, output path,
manifest metadata, mutable lifecycle state, or line-ending artifacts was found — none of these fields exist
anywhere in `TemplateCombinationDefinition` or its canonical serialization.

## Why the previous report said "fresh, distinct per-release"

That phrase described the fact that `b3dab0138...` (v2's hash) had **never appeared in any release before
`0.4.0-pilot`** — it is a newly-introduced hash value, as opposed to v1's hash (`c6324371a3...`) which
already existed across three prior releases. It did **not** mean "this hash changes from release to
release." The wording was ambiguous, not evidence of a hashing defect. **Corrected wording**:

> Deterministic v2 content hash, stable across source, bundle, and release.

## Dependency closure — v2's PublishedTemplateBundle

```
TEN_K__4D__INTERMEDIATE v2  (root)
├── TEN_K_MASTER v2
├── TEN_K_WORKOUT_PROGRESSION_V1 v1   (via TEN_K_MASTER v2's workoutProgression reference)
├── RUN_LAYOUT_4D v1
├── INTERMEDIATE_MODIFIER v1
│   └── INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1
├── APPSEL_RACE_PLAN_V1 v1
│   ├── RUNTIME_CONDITION_VALUES_V1 v1
│   └── PEAK_VOLUME_BANDS_V1 v1
└── Workouts: EASY_STANDARD v1, FARTLEK v1, GOAL_PACE_TEN_K v1, THRESHOLD_TEMPO v1
```

**Combination v1 does not appear anywhere in this graph.** Directly confirmed by reading the actual
`bundles/TEN_K__4D__INTERMEDIATE.v2.json` file in `0.4.0-pilot`: its `masterTemplate`, `layout`,
`levelModifier`, `workoutProgression`, `progressionModifier`, `rulePack`, `runtimeConditionValueRegistry`,
`peakVolumeBandPolicy`, and `workouts` fields contain zero references of `documentType: "TEMPLATE_COMBINATION"`
— structurally, a combination bundle can never reference another combination (`PublishedTemplateBundle` has
no such field).

## Why 0.4.0-pilot contains both v1 and v2

**Classification: C — accidentally included because the publisher packages every catalog file** (not D —
v1 is never treated as a dependency of v2; the two bundles are entirely independent).

`CatalogPublisher.BuildRelease` (`src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs`) builds
**one `PublishedTemplateBundle` per combination present in the full catalog source snapshot**:

```csharp
var bundles = stamped.Combinations
    .Select(c => bundleAssembler.Assemble(stamped, c.Metadata.Key, c.Metadata.Version, retirementLedger))
    .ToList();
```

Because `catalog/combinations/` contains both `ten-k-4d-intermediate.v1.json` and `.v2.json`, every release
built from this source produces two independent bundles — one rooted at v1, one at v2 — plus every other
stamped catalog document (all `PlanTemplate`, `RunLayout`, etc. versions) republished into the release's
per-type folders.

**Is this a defect?** No. **Does it match the canonical brief?** Yes — brief §15 states the release
manifest "lists ALL published artifacts and bundles" (`Publish edilen bütün artifact ve bundle'ları
listeler`). Full-catalog packaging per release is the documented design; it is not single-bundle selective
publishing. Distinguishing "the selected root bundle's dependency closure" (proven above to contain only
v2 and its true dependencies) from "everything else the release additionally packages" (v1's own
independent bundle, included because it still exists in source) resolves the apparent ambiguity.

## Runtime/root selection is unambiguous

`CatalogBundleAssembler.Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 2)` and
`Assemble(snapshot, "TEN_K__4D__INTERMEDIATE", 1)` are two distinct, explicit calls keyed by
`(key, version)`. There is no "latest" or "default" resolution that could accidentally select the wrong
version — a caller must always specify the version explicitly (as the CLI's `--version` option, defaulting
to `1` only when omitted, already requires). No key/version ambiguity exists.

## Corrections made

- **None to hashing or bundling logic** — no defect found.
- **Wording correction only**: this report replaces the prior ambiguous phrase "fresh, distinct per-release"
  wherever it appeared (see updated `combination-immutability-investigation.md` and
  `ten-k-pilot-domain-review-summary.md`).

## Final status

- **Hash-stability status: STABLE_DETERMINISTIC** — one hash per (documentType, key, version, content) tuple, proven identical across source, bundle assembly, and published release.
- **Dependency-closure status: CORRECT** — v2's bundle closure contains exactly its true dependencies; v1 is not part of it; both versions are independently, unambiguously selectable.
- **No new pilot release was required or created.**
