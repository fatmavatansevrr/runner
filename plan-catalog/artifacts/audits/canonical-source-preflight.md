# Canonical Source Pre-Flight — TEN_K / INTERMEDIATE / 4D Domain-Content Review

**Overall status: PASSED.** This run supersedes the prior halted run (2026-07-07T12:30:00Z, `FAILED`, blocked on missing `plan-catalog/docs/README.md`). That document now exists and defines the canonical source hierarchy; the review proceeds.

## Governance document found

`plan-catalog/docs/README.md` (46 lines) defines:

1. A 6-level source precedence hierarchy: Golden Fixture v3 → referenced versioned rule files → Plan Generation Decisions docs → reconciled architecture specs → Plan Catalog canonical brief → existing catalog artifacts/tests.
2. `docs/canonical/` as the sole approved canonical directory.
3. That conflicts must record both source paths, both values, the selected value, precedence reason, and affected artifacts/tests — else the value stays `PLACEHOLDER_UNCONFIRMED`.

## Check results

| Check | Source | Expected | Actual | Status |
|---|---|---|---|---|
| `plan-catalog/docs/README.md` exists | `plan-catalog/docs/README.md` | Exists, defines governance | Exists, defines 6-level precedence + approved directory | PASS |
| `docs/canonical/` exists | `plan-catalog/docs/canonical/` | Directory exists | Exists | PASS |
| DecisionTrace JSON exists | `golden-10k-intermediate-4d-12w.v3.decisiontrace.json` | Exists, readable | Exists, valid JSON | PASS |
| PlanDocument JSON exists | `golden-10k-intermediate-4d-12w.v3.plandocument.json` | Exists, readable | Exists, valid JSON | PASS |
| Golden fixture `.md` exists | `golden-10k-intermediate-4d-12w.v3.md` | Exists, readable | Exists | PASS |
| `progression_rules_v2.yaml` exists | same directory | Exists, readable | Exists, valid YAML | PASS |
| DecisionTrace `schemaVersion` | `$.schemaVersion` | 3 | 3 | PASS |
| PlanDocument `schemaVersion` | `$.schemaVersion` | 3 | 3 | PASS |
| PlanDocument `fixtureRevision` | `$.fixtureRevision` | 3 | 3 | PASS |
| Both fixtures `fixtureKey` | `$.fixtureKey` | `GOLDEN_10K_INTERMEDIATE_4D_12W` | matches in both | PASS |
| `progression_rules_v2.yaml` `schemaVersion` | YAML root | 2 | 2 | PASS |
| **PlanDocument `contentHash` verifies** | `$.contentHash` vs recomputed SHA-256 | `7b2e92dc…` | **`7b2e92dc…` — EXACT MATCH** (see note) | **PASS** |
| Week 1 boundary | `$.weeks[0]` | 2026-08-03 → 2026-08-09 | matches | PASS |
| Week 12 boundary | `$.weeks[11]` | 2026-10-19 → 2026-10-25 | matches | PASS |
| `horizon.raceWeekStartDate` | `$.horizon.raceWeekStartDate` | 2026-10-19 | matches | PASS |
| Taper training distribution | `$.weeks[11].days[*].workout.plannedDistanceKm` | 8 + 8 + 4 km | 8+8+4=20 km training, +10 km race = 30 total | PASS |
| Race day `loadClassification` | `$.weeks[11].days[3]` | RACE | RACE | PASS |
| Race day `stimulusAccountingScope` | `$.weeks[11].days[3]` | `RACE_EXCLUDED_FROM_TRAINING_HARD_COUNT` | matches | PASS |
| Warning Policy Evaluation / Warning Presentation Gate separate | `.md` narrative | Distinct stages | Distinct | PASS |
| Provisional Peak Target / Weekly Volume Curve Generator separate | `.md` narrative | Distinct stages | Distinct | PASS |
| Week 7 cutback reason | decisiontrace/plandocument/`.md` | `PHASE_TRANSITION_DELOAD` | present in all three | PASS |
| `PLANNED_SINGLE_SESSION_SPIKE_CHECK` exists | decisiontrace/plandocument | Present | Present | PASS |
| `FINAL_VALIDATION` references `WARNING_POLICY_V2` | decisiontrace | Present together | Present | PASS |
| `ESTIMATED_THRESHOLD_EFFORT` exists | plandocument/`.md` | Present | Present | PASS |
| Obsolete `CURRENT_LACTATE_THRESHOLD_EFFORT` absent from JSON | decisiontrace/plandocument | 0 occurrences | 0 occurrences (named once in `.md` only as the superseded label) | PASS |
| Fixture template/rulePack artifact-version parity | `$.template.version`/`$.rulePack.version` vs catalog v1 artifacts | two separate conclusions, non-blocking | **SOURCE_SEMANTICS_USABLE** / **ARTIFACT_VERSION_PARITY_UNRESOLVED** — see below | RECORDED (non-blocking) |

## Hash verification note

The prior run's `INCONCLUSIVE` result used a Node.js reproduction (`JSON.parse`/`JSON.stringify`), which silently normalizes numeric literals (`24.0` → `24`) and defaults to strict `\uXXXX` escaping of non-ASCII text. Neither matches the source. Re-verified with a throwaway .NET console harness using `System.Text.Json.Nodes.JsonNode` (which preserves the original numeric token text exactly as written) combined with `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` — i.e. **exactly** the encoder already configured in `PlanCatalog.Infrastructure.Serialization.CanonicalJsonOptions.Canonical`. Four combinations were tried (default/relaxed escaping × indented/non-indented); only **non-indented + relaxed escaping** reproduced the stored hash exactly:

```
expected  = 7b2e92dce611ae27751524ee41cb8c1b6b2b153eea7a125edbf5ab60eb3bc735
computed  = 7b2e92dce611ae27751524ee41cb8c1b6b2b153eea7a125edbf5ab60eb3bc735
```

This is a meaningful positive finding: the fixture's canonicalization convention is consistent with the project's own existing canonical serializer, increasing confidence that the fixture is a genuine, trustworthy artifact of the same authoring pipeline family.

## Artifact-version parity — two separate conclusions (per explicit instruction)

- **`SOURCE_SEMANTICS_USABLE`**: The fixture's field-level domain facts (phase week counts, week boundary dates, peak-volume typical band, taper distribution, race accounting scope, vocabulary spellings, progression percentages in `progression_rules_v2.yaml`) do not depend on which exact template/rule-pack version number is stamped on the document. These may still be cited as evidence for the current v1 pilot artifacts, field by field, where the underlying domain fact is version-independent.
- **`ARTIFACT_VERSION_PARITY_UNRESOLVED`**: `$.template.version = 2` and `$.rulePack.version = 3` in the fixture do not match `TEN_K_MASTER v1` / `APPSEL_RACE_PLAN_V1 v1` in the current catalog. **No catalog artifact was upgraded, cloned, or renamed to force parity.** Any fixture field whose meaning is inherently specific to the v2/v3 revision (e.g., a hypothetical rule or phase that only exists starting at v2/v3) is excluded from use as evidence until that parity question is separately resolved by product/architecture decision.

## Result

Pre-flight **PASSED**. Proceeding with the full domain-content audit reconciliation.
