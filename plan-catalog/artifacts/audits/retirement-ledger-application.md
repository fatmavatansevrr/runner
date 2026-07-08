# Retirement Ledger Application — Part 3 Step 2/3 — RETIRE-APPLY-001

## Pre-change state

`artifacts/appsel-plan-catalog/retirements.json` **did not exist** before this task — confirmed by direct
file-existence check both at the start of Part 3 and throughout Part 2. No checksum applies to an absent
file; this is recorded as the pre-change baseline (`fileExists: false`).

## Staged overlay preview (before writing the real ledger)

Built an in-memory `IRetirementLedger` overlay containing exactly the 3 planned entries (never written to
disk) and ran the full preview pipeline against it:

| Check | Result |
|---|---|
| Source-integrity validation (unfiltered snapshot, no ledger consulted) | PASSED |
| Eligible combinations under overlay | `TEN_K__4D__INTERMEDIATE v4` only |
| `CandidatePublishGraphValidator` over overlay-eligible set | PASSED |
| `ActiveVersionUniquenessValidator` against overlay | PASSED — no `ACTIVE_COMBINATION_VERSION_NOT_UNIQUE` |
| `CatalogPublisher.BuildPreview("part3-overlay-preview", Pilot, ...)` | PASSED |
| Manifest bundle list | `TEN_K__4D__INTERMEDIATE v4` only, hash `0a574a7abcefaed04b54844ba06d6ae047286f43562b7c540e3a30ad695f401b` (exact Part 2 match) |
| Manifest `TEMPLATE_COMBINATION` artifact list | `v4` only (v1/v2/v3 excluded) |
| Partial release directory written for the preview name | **No** — `Directory.Exists(...)` = `False` |

Overlay preview satisfied every required proof before the real ledger was touched.

## Real retirement entries applied

Applied via CLI `retire --type TEMPLATE_COMBINATION --key TEN_K__4D__INTERMEDIATE --version {1,2,3}`, one
call per version:

```json
[
  { "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 1, "retiredAtUtc": "2026-07-08T07:40:54.4358331Z" },
  { "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 2, "retiredAtUtc": "2026-07-08T07:40:56.5116376Z" },
  { "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 3, "retiredAtUtc": "2026-07-08T07:40:58.6208197Z" }
]
```

**Post-change SHA-256**: `d629ec93aea3f21dffbecaa501e2f43c256155f34351971dfade00c41633e1e9`

No dependency artifact (workouts, `TEN_K_MASTER` v1/v2, `INTERMEDIATE_MODIFIER` v1,
`TEN_K_WORKOUT_PROGRESSION_V1` v1, rule packs, policies, registries, layouts) was retired — only the 3
combination versions, per the approved plan.

## Post-write revalidation — matches the overlay preview exactly

| Check | Result |
|---|---|
| Source-integrity validation | PASSED |
| `validate-combination TEN_K__4D__INTERMEDIATE v4` | PASSED |
| `validate-combination TEN_K__4D__INTERMEDIATE v1` (explicit retired-root request) | **FAILED** — `RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE` |
| `build-release` preview (real ledger) | Exactly 1 `TEMPLATE_COMBINATION` artifact (v4), exactly 1 bundle (v4), hash `0a574a7abcefaed04b54844ba06d6ae047286f43562b7c540e3a30ad695f401b` |
| Full test suite | 245/245 (1 test updated — see below) |
| All 6 historical releases | PASSED |

Result **matches the staged overlay preview exactly** — no restoration was needed.

## Test updated

`ActiveVersionPreparationTests.RealCatalogRetirementPlan_IsGeneratedButNotExecuted` (Part 2's test, whose
premise — "not yet executed" — is now obsolete) was renamed to
`RealCatalogRetirementPlan_HasBeenExecuted_ExactlyOneEligibleVersionRemains` and updated to assert the new,
correct post-retirement state (ledger exists, v1/v2/v3 retired, v4 not retired, uniqueness validator
passes).

## Historical artifacts: preserved

`catalog/combinations/ten-k-4d-intermediate.v1.json`, `.v2.json`, `.v3.json` were **not** modified, moved,
or deleted — retirement is recorded exclusively in the ledger, never in source JSON. Confirmed identical
hashes before/after (see `deterministic-graph-part3-completion.md`).

## Final status: retirement ledger correctly reflects the approved Part 3 plan; real state matches the staged overlay preview exactly.
