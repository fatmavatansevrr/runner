# RulePack Ownership Audit — Milestone C — RULEPACK-OWN-001

## Before

`PlanTemplateDefinition.RequiredRules: VersionedCatalogReference[]` and
`TemplateCombinationDefinition.RulePack: VersionedCatalogReference` were two **separate, exact-version**
selections that could — and did — disagree: `TEN_K_MASTER v2` declared `RequiredRules = [APPSEL_RACE_PLAN_V1
v1]` while `TEN_K__4D__INTERMEDIATE v3` (which uses that exact master) declared `RulePack =
APPSEL_RACE_PLAN_V1 v2`. `PlanTemplateValidator` only checked that `RequiredRules`' target existed, never
that it agreed with any combination's `RulePack`. `CatalogBundleAssembler` never read `RequiredRules` at
all — confirmed by `grep -c RequiredRule src/PlanCatalog.Infrastructure/Publishing/CatalogBundleAssembler.cs`
= 0.

## After

- `PlanTemplateDefinition.RequiredRuleKeys: string[]?` (new, schemaVersion >= 2) — **semantic key
  requirement only**. Means "the combination-selected RulePack's key must be one of these" — never selects
  a version.
- `TemplateCombinationDefinition.RulePack` remains the **sole exact RulePack selection** — unchanged,
  still the only thing `CatalogBundleAssembler` reads (confirmed unchanged: still `grep -c RequiredRule
  CatalogBundleAssembler.cs` = 0 after this migration).
- New cross-check: `CandidatePublishGraphValidator.ValidateRulePackSatisfiesMasterRequirement` — if the
  master has `RequiredRuleKeys` (schemaVersion >= 2 only), fails with
  `COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS` unless `combination.RulePack.Key` is in
  that list. **Legacy (schemaVersion 1) masters using `RequiredRules` are never cross-checked this way** —
  historical masters remain readable exactly as before (Decision E).

## C1/C2 — new master schema shape and exclusivity

`TEN_K_MASTER v3` (candidate): `schemaVersion: 2`, `requiredRuleKeys: ["APPSEL_RACE_PLAN_V1"]`,
`requiredRules` field **absent**. Enforced exclusively (never both, never neither) by
`SchemaVersionShapeValidator` — same mechanism as Milestone B (see `exact-workout-reference-migration.md`
for the mechanism-choice rationale, applied identically here).

`TEN_K_MASTER v1` and `v2` (historical, immutable) keep `requiredRules` — untouched:

| Artifact | Version | Hash | Status |
|---|---:|---|---|
| `TEN_K_MASTER` | 1 | `c6cb0c0b4ebcfbdf946d97c9f03f1b8ec384abb68b8f0fa274a64a2eab9e5214` | unchanged |
| `TEN_K_MASTER` | 2 | `9ac7f07666a26ec95080592534cf92e5892d6f0853566cc0ffb4d28e527be4b0` | unchanged |
| `TEN_K_MASTER` | 3 (candidate) | `e2ede6a030b3ea1a09b798c2c21840702375bffc2e489bc58f31d51f407e66bb` | new |

## C3 — combination validation

`COMBINATION_RULE_PACK_DOES_NOT_SATISFY_MASTER_REQUIREMENTS` implemented and proven both ways: a
combination whose `RulePack.Key` is absent from the master's `RequiredRuleKeys` fails; a matching key
passes; **a RulePack *version* bump alone (same key) never requires touching the master** — proven by
`RulePackOwnershipTests.ChangingOnlyRulePackVersion_DoesNotRequireChangingMaster_WhenKeyRemainsAcceptable`.

## C4 — bundle assembly unchanged

`CatalogBundleAssembler` continues to resolve the bundled RulePack exclusively via
`snapshot.FindRulePack(combination.RulePack)` — no second exact resolution path was added. `RulePack v1`
and `v2` are both **unmodified**; no new RulePack version was created by this migration (`combination v4`
references the already-existing, already-correct `RulePack v2` — its serialized content never changed, so
per the explicit "do not create a new RulePack version unless its serialized content changes" instruction,
none was created):

| Artifact | Version | Hash | Status |
|---|---:|---|---|
| `APPSEL_RACE_PLAN_V1` | 1 | `020f9aac902b7816d8d4b4f01a82b143df8d5feb23e8cc75f6d9fda36be66a89` | unchanged |
| `APPSEL_RACE_PLAN_V1` | 2 | `4ea4270573074714993942feb25b25d93aa00aec6ba4c7ad8896aadb91670300` | unchanged, reused by combination v4 |

## C5 — classification

Recorded as a `TECHNICAL_ONLY` architecture/ownership decision — see `deterministic-graph-part2-migration.json`
migration entry for `TEN_K_MASTER`. No `PLACEHOLDER_UNCONFIRMED` domain content was upgraded or invented by
this change; no unrelated domain-content decision was touched.

## Tests (6 — `tests/PlanCatalog.Tests/Validation/RulePackOwnershipTests.cs`)

Tests 14-19 of the Part 2 required list, all passing.

## Final status: `RULE_PACK_OWNERSHIP` = **IMPLEMENTED**
