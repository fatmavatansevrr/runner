# Peak-Volume Policy Immutability Remediation — PEAK-POLICY-IMMUT-001

## Scope

Remediates `PEAK_VOLUME_BAND_POLICY / PEAK_VOLUME_BANDS_V1 / v1` — the one non-workout in-place mutation
discovered by `cross-release-hash-consistency-audit.md`.

## Reconstruction table

| Release | Declared version | Hash | INTERMEDIATE rows (3d / 4d / 5d) |
|---|---:|---|---|
| 1.0.0 | 1 | `c6eb3bc4...` | 26-40km / 30-45km / 34-50km |
| 0.1.0-pilot | 1 | `d187758e...` (**mutated**) | 22-32km / 30-42km / 36-50km |
| 0.2.0-pilot, 0.3.0-pilot, 0.4.0-pilot | 1 | `d187758e...` | same mutated content republished |

NEW/ADVANCED/EXPERIENCED rows (9 total) are byte-identical across every release and version — only the
3 INTERMEDIATE rows differ.

## Findings

1. **Earliest historically published v1 content**: `1.0.0`'s content (26-40 / 30-45 / 34-50km).
2. **First release with mutated v1 content**: `0.1.0-pilot` — the INTERMEDIATE band-corroboration pass (an
   earlier domain-content review task) edited `catalog/policies/peak-volume-bands.v1.json` in place
   instead of creating v2.
3. **Current live source content (before this remediation)**: matched the mutated (`0.1.0-pilot`-onward)
   content exactly (22-32 / 30-42 / 36-50km).
4. **Is the current content semantically intended?** Yes for the 4-day row specifically — Golden Fixture
   v3's `$.peakVolume.typicalBandKm=[30,42]` is an exact independent match for the 4-day 30-42km row (see
   `ten-k-pilot-domain-decision-audit.md`, now `AUD-056`). The 3-day/5-day rows were provided as canonical
   source material in an earlier review request but have no independent second source — they remain
   `CANONICAL_CONFIRMED` on that single-source basis, per the existing classification (unchanged by this
   remediation; this task did not re-litigate that decision, only re-attached it to the correct version).
5. **Every active reference that must move to v2**: `RulePackDefinition.PeakVolumeBandPolicy` — a
   versioned reference (see naming/versioning decision and cascade below).

## Naming/versioning decision (required by task)

The key `PEAK_VOLUME_BANDS_V1` contains a `_V1` suffix that could be mistaken for `metadata.version`.
Inspected repository convention before choosing a model:

- `TEN_K_MASTER` v1→v2: same key, `metadata.version` bumped. Key never gained a suffix.
- `TEN_K__4D__INTERMEDIATE` v1→v2→v3: same key, `metadata.version` bumped each time.
- Every other artifact with a `_V1` suffix in its key (`APPSEL_RACE_PLAN_V1`, `RUNTIME_CONDITION_VALUES_V1`,
  `TEN_K_WORKOUT_PROGRESSION_V1`, `INTERMEDIATE_PROGRESSION_MODIFIER_V1`) has `metadata.version: 1` for all
  of them — the `_V1` suffix is a static, human-readable naming-convention artifact baked into the key at
  authoring time, entirely independent of the separate numeric `metadata.version` field used for
  immutability.

**Decision: key and artifact version are independent.** Per existing, consistently-applied precedent:
- key: `PEAK_VOLUME_BANDS_V1` (unchanged)
- `metadata.version`: `2`

Renaming the key to `PEAK_VOLUME_BANDS_V2` would be the *only* artifact in the entire catalog where a
version bump also renamed the key — inconsistent with every other precedent, and not adopted.

## Correction applied

- `catalog/policies/peak-volume-bands.v1.json` — **restored** to its exact `1.0.0` content. Recomputed
  hash (`c6eb3bc4...`) matches the earliest historical hash exactly.
- `catalog/policies/peak-volume-bands.v2.json` — **created**, `metadata.version: 2`, preserving the
  confirmed INTERMEDIATE rows (22-32 / 30-42 / 36-50km) and carrying over the 9 NEW/ADVANCED/EXPERIENCED
  rows byte-for-byte unchanged (not invented, not interpolated).
- No historical release directory was modified.

## Domain classification

| Field | v1 (restored) | v2 (corrected) |
|---|---|---|
| INTERMEDIATE rows (3/4/5-day) | **PLACEHOLDER_UNCONFIRMED** (restored original, never independently corroborated) | **CANONICAL_CONFIRMED** (4-day independently corroborated by Golden Fixture v3; 3/5-day confirmed on single-source basis, per pre-existing unrelated decision — not re-litigated here) |
| NEW/ADVANCED/EXPERIENCED rows (9) | PLACEHOLDER_UNCONFIRMED (unchanged — no canonical source, not interpolated) | PLACEHOLDER_UNCONFIRMED (same 9 rows, byte-identical, still unconfirmed and Production-blocking) |

## Dependency cascade (RulePack → Combination)

`RulePackDefinition APPSEL_RACE_PLAN_V1` v1 references `PeakVolumeBandPolicy` by an explicit versioned
reference (`{key: PEAK_VOLUME_BANDS_V1, version: 1}`). v1 is already PUBLISHED (immutable since `1.0.0`),
so it cannot be repointed at the corrected policy v2 in place.

- `catalog/rule-packs/appsel-race-plan.v1.json` — **unchanged**, still correctly references the restored
  `PEAK_VOLUME_BANDS_V1` v1 (its own content hash is untouched: `020f9aac90...`, stable across all 5
  historical releases).
- `catalog/rule-packs/appsel-race-plan.v2.json` — **created**, referencing `PEAK_VOLUME_BANDS_V1` v2;
  `runtimeConditionValueRegistry` and all other fields unchanged from v1.

`TemplateCombinationDefinition TEN_K__4D__INTERMEDIATE` v1 and v2 are both already PUBLISHED (v1 since
`1.0.0`; v2 since `0.4.0-pilot`), both referencing `RulePack` v1 — both preserved unchanged.
`ten-k-4d-intermediate.v3.json` was **created**, identical to v2 except `rulePack.version: 2`; this is the
new active pilot combination. See `dependency-version-cascade-audit.md` for the full trace.

## Tests added (`tests/PlanCatalog.Tests/Golden/PeakVolumePolicyImmutabilityTests.cs`)

| Test | Proves |
|---|---|
| `RestoredPolicyV1_MatchesEarliestHistoricalReleaseHash` | v1's restored hash matches `1.0.0` exactly. |
| `CorrectedPolicyV2_HasDistinctDeterministicHash` | v2's hash is stable and differs from v1. |
| `CorrectedPolicyV2_PreservesConfirmedIntermediateBands` | v2 carries exactly 22-32/30-42/36-50km for INTERMEDIATE 3/4/5-day. |
| `RestoredPolicyV1_DoesNotInventNewOrAdvancedOrExperiencedRows` | The 9 non-INTERMEDIATE rows are identical between v1 and v2 — nothing invented. |
| `UnconfirmedBands_RemainBlockingForProductionPublish` | Both v1 and v2 still have blocking placeholder content (v1 entirely; v2's 9 non-INTERMEDIATE rows). |
| `ConfirmedIntermediateRows_AreNotBlocking_OnV2` | v2's INTERMEDIATE rows carry no blocking entry. |

All pass. Full suite: 203/203.

## Files changed

- `catalog/policies/peak-volume-bands.v1.json` (restored)
- `catalog/policies/peak-volume-bands.v2.json` (new)
- `catalog/rule-packs/appsel-race-plan.v2.json` (new)
- `catalog/combinations/ten-k-4d-intermediate.v3.json` (new)
- `src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` (AUD-049 moved to v1-restored wording; AUD-056/057/058/059 added)
- `tests/PlanCatalog.Tests/Golden/PeakVolumePolicyImmutabilityTests.cs` (new)
- `tests/PlanCatalog.Tests/Golden/PilotCatalogStructuralTests.cs` (updated to target v2)
- `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json` (canonical direction corrected)

## Final status: `PEAK_POLICY_IMMUTABILITY` = **REMEDIATED**
