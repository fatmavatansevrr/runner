# TEN_K_MASTER TAPER Phase Workout-Family Conflict — Investigation & Resolution

**Status: RESOLVED_CANONICALLY**

## Statement

TEN_K_MASTER TAPER eligibleWorkoutFamilies omitted QUALITY and RACE even though approved Golden Fixture v3
Week 12 contains both a QUALITY activation workout and a RACE workout in the TAPER phase. The master
definition was corrected to allow EASY, LONG_RUN, QUALITY, and RACE.

## Exact locations

| Item | Path |
|---|---|
| Original artifact | `catalog/templates/ten-k-master.v1.json` (unchanged, still PUBLISHED/immutable) |
| Corrected artifact | `catalog/templates/ten-k-master.v2.json` (new) |
| JSON path | `$.phases[3].eligibleWorkoutFamilies` (the phase object where `phaseKey == "TAPER"`) |
| Original value | `["EASY", "LONG_RUN"]` |
| Corrected value | `["EASY", "LONG_RUN", "QUALITY", "RACE"]` |

## Golden Fixture v3 evidence

- `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.plandocument.json`
  `$.weeks[11].days[0].workout` → `workoutKey: "RACE_PACE_REPEATS"`, `family: "QUALITY"`, week 12's
  `phaseKey: "TAPER"`.
- Same file, `$.weeks[11].days[3].workout` → `workoutKey: "RACE_DAY"`, `family: "RACE"`,
  `dayType: "RACE"`, week 12's `phaseKey: "TAPER"`.

## Validators consuming phase-family eligibility

Only **`WorkoutProgressionValidator`** (`src/PlanCatalog.Core/Validation/WorkoutProgressionValidator.cs`)
reads `PhaseDefinition.EligibleWorkoutFamilies`, via the `WP_CANDIDATE_FAMILY_NOT_ELIGIBLE_FOR_PHASE`
check. `TemplateCombinationValidator` and `CatalogGraphValidator` do not consult it directly (confirmed by
repository-wide search — zero other usages).

## Modeling rule preserved

- `WorkoutFamily` remains exactly `{EASY, LONG_RUN, QUALITY, RACE}` — **no `Taper` member was added.**
- `PhaseKey` remains exactly `{FOUNDATION, BUILD, RACE_SPECIFIC, TAPER}` — unchanged.
- `PhaseKey.Taper` and `WorkoutFamily` remain independent, incomparable .NET enum types (compile-time
  guarantee) — a workout may be eligible for the TAPER phase while its `Family` remains `Quality` or `Race`.

## Versioning decision

`TEN_K_MASTER v1` (contentHash `c6cb0c0b4ebcfbdf946d97c9f03f1b8ec384abb68b8f0fa274a64a2eab9e5214`) is
already **PUBLISHED and immutable** in three existing releases (`1.0.0`, `0.1.0-pilot`, `0.2.0-pilot`). Per
the repository's atomic-publish/immutability rules, it was **not edited in place**. Instead:

1. `catalog/templates/ten-k-master.v2.json` was created with the corrected TAPER family set (metadata
   `version: 2`, `status: "VALIDATED"`) — every other field identical to v1.
2. `catalog/combinations/ten-k-4d-intermediate.v1.json` `$.masterTemplate.version` was changed from `1` to
   `2`, pointing the current pilot dependency graph at the corrected version.
3. `catalog/templates/ten-k-master.v1.json` was left completely untouched.
4. No historical release directory was modified.

## Artifact-version parity (record only)

- **TEN_K_MASTER**: Golden Fixture v3 references `v2`; the catalog now also has `v2` after this task. This
  is **incidental**, not a deliberate parity-forcing action — a new version was mandatory regardless once
  the TAPER content changed (v1 is immutable). No other fixture-derived content was back-filled into v2
  beyond the TAPER family correction.
- **APPSEL_RACE_PLAN_V1**: Golden Fixture v3 references `v3`; the catalog remains at `v1`. Semantic impact
  is unknown (this pilot's rule-pack `policies`/`rules` arrays are empty, so no observable behavioral
  difference between v1/v2/v3 is evidenced by the fixture). **Does not block this correction.** **Not
  upgraded in this task**, per explicit instruction. Required future decision: a dedicated review of what
  APPSEL_RACE_PLAN_V1 v2/v3 actually changed.

## Result

**RESOLVED_CANONICALLY** — the TAPER family conflict recorded in the prior domain-content audit
(`ten-k-pilot-domain-decision-audit.md`, entry `AUD-007`) is now `CANONICAL_CONFIRMED`, sourced directly
from Golden Fixture v3, with the corrected artifact published as `TEN_K_MASTER v2`.
