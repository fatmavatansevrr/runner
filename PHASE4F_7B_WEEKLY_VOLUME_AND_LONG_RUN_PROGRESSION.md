# Phase 4F.7B Weekly Volume and Long-Run Progression

## Status

This document has been corrected by Phase 4F.7B.1. The original 4F.7B implementation incorrectly treated the peak-volume band as a Week 1 floor, selected the band maximum as peak, used taper multiplier `0.65`, and used a non-canonical `0.20-0.35` long-run range. Those behaviors are superseded as corrections to existing canonical repository decisions, not as new product rules.

## Corrected Scope

Phase 4F.7B remains a dark/internal numeric envelope for the active `TEN_K__4D__INTERMEDIATE` v10 catalog pilot. It produces one planned weekly volume and one planned long-run distance per already-bound week. It does not change phases, dates, stages, roles, workout identities, public preview DTOs, snapshots, hashes, confirmation, persistence, migrations, routing, catalog values, or publication status.

## Corrected Catalog Rule Inventory

Used catalog/canonical facts:

- `TEN_K_MASTER` v6: `coreCycle.minimumWeeks = 8`, `defaultWeeks = 12`, `maximumWeeks = 14`; existing phase structure including protected one-week `TAPER`.
- `PEAK_VOLUME_BANDS_V1` v3: `TEN_K` / `INTERMEDIATE` / `4` runs per week has typical peak band `minimumKm = 30`, `maximumKm = 42`.
- `docs/canonical/golden-fixture-v3/progression_rules_v2.yaml`: Intermediate preferred weekly volume cap `0.04-0.07`, hard cap `0.08`, and four-run absolute weekly increment cap `2.5 km`.
- `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json`: `weeklyVolumeAnchorKm = 24`, `typicalBandKm = [30,42]`, `resolvedPeakKm = 38`, taper week reduces from `38 km` to `20 km`.
- `LONG_RUN_STANDARD` v4: workout identity remains unchanged.

## Corrected Algorithms

Starting volume:

- valid positive `RecentWeeklyVolumeKm`: selected as Week 1 start without raising it to the peak-band minimum;
- missing: Phase 4F.7B.2 `V1_MISSING_READINESS_STARTING_VOLUME_POLICY` v1 selects `16 km`;
- explicit zero: Phase 4F.7B.2 `V1_MISSING_READINESS_STARTING_VOLUME_POLICY` v1 selects `12 km`;
- invalid: typed failure, no silent fallback.

Reachable peak:

- peak band is a typical peak band, not a Week 1 floor;
- selected peak is derived from starting volume and cycle length;
- selected peak may be below `30 km` with `BELOW_TYPICAL_PEAK_BUT_VALID`;
- selected peak never exceeds `42 km`.

Weekly curve:

- deterministic interpolation remains a technical mechanism only after starting volume and reachable peak are resolved;
- non-taper weeks interpolate to the selected reachable peak, not the band maximum;
- no recurring recovery/deload is invented.

Taper:

- V1 taper multiplier is `0.53`, mapping to a `47%` reduction and within the accepted `41%-60%` reduction range;
- taper volume and long-run envelope reduce together;
- TAPER_SHARPEN intensity/component prescription remains deferred.

Long-run progression:

- four-day preferred share is `30%-36%`;
- four-day hard cap is `40%`;
- V1 deterministic selection share is `33%`, within the preferred range;
- Doc13/golden compatibility classes remain readiness/confidence classifications, not target shares.

## Typed Failures

- `CatalogVolumeInvalidReadinessInputException`
- `CatalogVolumeCanonicalRuleSourceMissingException`
- `CatalogVolumeUnreachablePeakRuleException`
- `CatalogVolumeInvalidTaperRuleException`
- `CatalogLongRunHardCapViolationException`
- `CatalogVolumeInvalidGovernanceConfigurationException`
- `CatalogVolumeUnsupportedCycleLengthException`
- `CatalogVolumeRuleInconsistentException`
- `CatalogVolumePlanInvalidException`

## Deferred Work

Phase 4F.7C/4F.7D remain responsible for pace source expansion, full session prescriptions, TAPER_SHARPEN component/dose detail, public materialization, live routing, confirmation/persistence, and publication.
