# Active-Version Uniqueness Audit — Part 3 — ACTIVE-UNIQ-001

## Before retirement

`ActiveVersionUniquenessValidator.Validate(snapshot.Combinations)` against the real catalog (no ledger)
failed with `ACTIVE_COMBINATION_VERSION_NOT_UNIQUE`: `TEN_K__4D__INTERMEDIATE` had 4 non-retired,
publish-eligible versions (`1, 2, 3, 4`) — confirming the exact ambiguity Part 1 flagged (Finding 2) and
Part 2 deliberately left unresolved (Milestone G).

## After retirement

With the real `FileSystemRetirementLedger` (now containing the 3 entries applied in Step 3):

```
ActiveVersionUniquenessValidator.Validate(snapshot.Combinations, realLedger).IsValid == true
```

Exactly **one** non-retired, publish-eligible version remains for `TEN_K__4D__INTERMEDIATE`: **v4**.
`ACTIVE_COMBINATION_VERSION_NOT_UNIQUE` no longer fires — proven by
`ActiveVersionPreparationTests.RealCatalogRetirementPlan_HasBeenExecuted_ExactlyOneEligibleVersionRemains`.

## Full-catalog publisher confirmation

`CatalogPublisher.BuildRelease`'s eligible-combination filter (`!retirementLedger.IsRetired(...)`),
independently of `ActiveVersionUniquenessValidator`, produces exactly **one**
`TEN_K__4D__INTERMEDIATE` bundle (v4) — confirmed by direct `build-release` preview inspection (see
`retirement-ledger-application.md`) and later by the actual `0.6.0-pilot` release manifest (see
`zero-six-pilot-release-audit.md`).

## v1/v2/v3 remain source-readable and historically verifiable

- `catalog/combinations/ten-k-4d-intermediate.v{1,2,3}.json` — present, unmodified, unmoved.
- `validate-combination TEN_K__4D__INTERMEDIATE --version {1,2,3}` (source-integrity structural check,
  ignoring retirement) still PASSES for all three — they remain individually valid documents; only their
  *new-release publish eligibility* is revoked.
- Explicit requests to build v1/v2/v3 into a NEW bundle fail with `RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE`.
- Historical releases `1.0.0` (v1), `0.1.0-pilot` (v1), `0.2.0-pilot` (v1), `0.3.0-pilot` (v1, defective),
  `0.4.0-pilot` (v1+v2), `0.5.0-pilot` (v1+v2+v3) all still `verify-release` PASSED — retirement is a
  ledger-only concept that never touches published release directories.

## Final status: `ACTIVE_COMBINATION_VERSION_UNIQUENESS` = **RESOLVED for the real catalog**
