# Phase 4F.7B.1 Canonical Volume Rule Correction

## Authoritative Sources

- `plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`: inspected directly. The requested Doc13 sections `§2`, `§3`, `§5.2`, and `§8.1` are not present in this repository copy; the file contains resolver-focused sections `A-D` and a V1 runtime scope note.
- `plan-catalog/docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json`: provides `weeklyVolumeAnchorKm = 24`, `longRunCompatibilityBand = ACCEPTABLE`, `longRunCompatibilityRatio = 0.375`, `typicalBandKm = [30,42]`, `resolvedPeakKm = 38`, and taper reduction from week 11 `38 km` to week 12 `20 km`.
- `plan-catalog/docs/canonical/golden-fixture-v3/progression_rules_v2.yaml`: provides Intermediate weekly-volume progression caps and four-run absolute increment cap.
- `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs`: existing append-only governance source; latest prior entries are `AUD-507` and `AUD-508`.

## Corrections

Peak-band semantics:

- `PEAK_VOLUME_BANDS_V1` v3 `30-42 km` is a typical accepted peak-week band.
- It is not a Week 1 lower bound.
- It is not a compulsory `42 km` peak target.

Starting volume:

- valid positive recent weekly volume remains the Week 1 anchor and is not raised to `30 km`;
- invalid readiness throws a typed failure;
- missing and explicit-zero starting-volume paths fail closed because no inspected canonical source defines the concrete Intermediate default resolver.

Reachable peak:

- selected peak derives from starting volume and actual cycle length;
- below-band reachable peaks remain valid and are classified `BELOW_TYPICAL_PEAK_BUT_VALID`;
- peak never exceeds `42 km`.

Taper:

- previous multiplier `0.65` is removed from active behavior;
- final V1 multiplier is `0.53`, a `47%` reduction and within the accepted `41%-60%` range;
- the multiplier is explicit product default, not a hidden runtime constant.

Long run:

- compatibility classes remain readiness/confidence classifications;
- four-day preferred share is `30%-36%`;
- hard cap is `40%`;
- deterministic V1 selection uses `33%`, inside the preferred range;
- old `20%-35%` compatibility bounds are removed from active behavior.

## Typed Failures

- `CatalogVolumeInvalidReadinessInputException`
- `CatalogVolumeCanonicalRuleSourceMissingException`
- `CatalogVolumeUnreachablePeakRuleException`
- `CatalogVolumeInvalidTaperRuleException`
- `CatalogLongRunHardCapViolationException`
- `CatalogVolumeInvalidGovernanceConfigurationException`

## Phase 4F.7B.2 Closure

Phase 4F.7B.2 closes the missing/explicit-zero starting-volume blocker with `V1_MISSING_READINESS_STARTING_VOLUME_POLICY` v1: missing weekly volume starts at `16 km`; explicit zero starts at `12 km`; both preserve `INTERMEDIATE` identity and do not use the `30 km` peak-band minimum.

## Remaining Gap

The repository copy of `appsel-v1-canonical-decisions.md` does not contain the requested volume-specific Doc13 sections. Phase 4F.7B.1 therefore used the inspected golden fixture and progression rules for corrected active behavior, and failed closed for missing/zero starting-volume rules until Phase 4F.7B.2 supplied an explicit V1 product default.
