# Wave 5 / D2 — Ownership Decision

**Is `INTERMEDIATE_PROGRESSION_MODIFIER_V1` globally shared?** No. It has exactly one referrer chain today: `INTERMEDIATE_MODIFIER` (all versions) → `TEN_K__4D__INTERMEDIATE` (all versions v1–v7). No other combination family exists in the catalog.

**Can the values be scoped by combination/day-count without ambiguity?** Yes — trivially, since there is only one combination in the repository today.

**Ownership per field:**

| Field | Classification | Reasoning |
|---|---|---|
| `maximumComplexityTier` | `UNUSED_OR_DEAD_FIELD` | Removed — no consumer target exists after Wave 3. |
| `maximumHardSessionsPerWeek` | Schema-shape: `LEVEL_GLOBAL`. Actual reuse: `COMBINATION_SPECIFIC` | Sole referrer is the TEN_K/INTERMEDIATE/4-day combination; kept in place (Shape 1). |
| `mainSetDoseMultiplier` | `TECHNICAL_METADATA` (unused for computation) | No consumer multiplies anything by it in this repo. |
| `allowGoalPaceRehearsal` | ~~`RUNTIME_GUARD_ONLY`~~ → **`TECHNICAL_METADATA` / `PRODUCT_CAPABILITY_DECLARATION (UNCONSUMED)`** (corrected, see below) | Write-only today: not read by any validator/publisher/bundle assembler. Real eligibility guard lives independently on the workout-progression stage and is unaffected by this flag. |
| `allowSecondHardStimulus` | Schema-shape: `LEVEL_GLOBAL`. Actual reuse: `COMBINATION_SPECIFIC` | Same reuse boundary as `maximumHardSessionsPerWeek`; the two are cross-validated together. |

## Correction (clarification pass): `allowGoalPaceRehearsal` ownership label

The original Wave 5 label `RUNTIME_GUARD_ONLY` was **inconsistent** with the field having zero readers — that category implies an actual runtime guard consumes the field, which is not true. A repository-wide search (`grep -rl "AllowGoalPaceRehearsal" src/`) confirms the property is declared only in `ProgressionModifierDefinition.cs` and read nowhere else, now also proven by an executable test (`AllowGoalPaceRehearsal_HasNoReaderAnywhereInThisRepository`). Corrected label: **`TECHNICAL_METADATA` / `PRODUCT_CAPABILITY_DECLARATION (UNCONSUMED)`** — the closest existing equivalent among this repository's ownership categories, since `PRINCIPLE_FLAG_UNCONSUMED` is not a supported enum/category anywhere in the codebase. The `DomainContentDecision` reason text for `AUD-333` was corrected accordingly (append-only; original wording preserved with a `WAVE5-CLARIFICATION` note, not deleted).

## Reuse protection (clarification pass, Issue 3)

An **executable** guard now exists: `DomainWave5D2ResolutionTests.IntermediateProgressionModifierV2_ReuseIsExecutablyGuarded_NoUnapprovedCombinationFamilyReferrer`. It walks every combination that resolves (via its `LevelModifier`) to `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v2` and asserts `DistanceFamily=TenK`, `RunsPerWeek=4`, `Experience=Intermediate`, and combination key `TEN_K__4D__INTERMEDIATE`. It will fail the moment an unrelated combination family gains a reachable reference to this artifact. The pre-existing `OnlyOneLevelModifierReferencesIntermediateProgressionModifierV1_OwnershipReuseBoundaryConfirmed` test was retained but is insufficient alone — it only checks `LevelModifier.Metadata.Key` equality, which is trivially true regardless of which combination family uses that level modifier, since the key is invariant across versions.

This protection is a **test (Process A only)**, not a production runtime validator — chosen to avoid a broader architecture change (e.g. a new `CandidatePublishGraphValidator` rule) for a boundary with no current violation and only one artifact affected.

**Was level-global ownership safe?** Yes. Hard-stop condition 3 (a level-global field that cannot safely represent the current TEN_K/INTERMEDIATE/4-day scope) does **not** apply, because the artifact's schema-level "global" shape and its actual reuse boundary are identical today (one referrer only).

**Was any field moved to a narrower owner?** No. A narrower owner (e.g. a combination-specific or weekly-structure-policy artifact type) was considered but does not exist in this repository's current architecture, and inventing one would be a speculative subsystem the task explicitly forbids. **Shape 1** (keep all surviving fields on `ProgressionModifierDefinition`) was selected.

**Mitigation for future generalization risk:** Explicit scope-boundary documentation was added in three places — the model's XML doc comment, the two evidence-backed `DomainContentDecision` entries (AUD-331, AUD-334), and this report — so that if a future 3-day/5-day or other-distance combination is added and happens to reference this same artifact key, the narrow evidence basis is visible and must not be silently inherited.

No hard-stop condition was triggered.
