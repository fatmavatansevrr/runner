# Runtime Registry Resolution Audit — Milestone D — REGISTRY-RES-001

## Before

`WorkoutProgressionValidator.Validate` (line 16, pre-migration) called
`snapshot.RuntimeConditionValueRegistries.FirstOrDefault()` and validated **every** stage's `Requires`
conditions against that single, globally-selected registry — regardless of which combination/RulePack was
actually in play. Harmless only because exactly one `RuntimeConditionValueRegistryDefinition` has ever
existed in this catalog (confirmed in Part 1's pre-change assessment, Finding 8).

## After

- **D1 — `WorkoutProgressionValidator`**: `RuntimeConditionValueRegistries.FirstOrDefault()` and all
  `Requires`-vs-registry validation logic were **deleted entirely**. It now validates only local structure:
  stage identity, `RelativeOrder` contiguity, candidate-workout existence (both shapes), phase/family
  eligibility, fallback-chain validity, and `Requires` field shape (`WP_CONDITION_ALLOWED_VALUES_EMPTY` —
  a non-empty-AllowedValues check that needs no registry). Confirmed by grep:
  `RuntimeConditionValueRegistry` no longer appears anywhere in `WorkoutProgressionValidator.cs`.
- **D2 — `CandidatePublishGraphValidator.ValidatePinnedRegistry`** (new): for one selected combination,
  resolves `combination.RulePack` (exact) → `rulePack.RuntimeConditionValueRegistry` (exact) → validates
  every stage's `Requires` conditions against **exactly that** registry. Failure code
  `RUNTIME_CONDITION_VALUE_NOT_ALLOWED_BY_PINNED_REGISTRY` (covers both "condition type not in this
  registry" and "value not allowed by this registry").

## Proof (isolated fixtures — two registry versions, never added to the permanent catalog)

`PinnedRegistryResolutionTests.cs` builds two registries in the same snapshot — `REGISTRY_WITH_VALUE`
(allows `REALISTIC`) and `REGISTRY_WITHOUT_VALUE` (allows only `UNRELATED_VALUE`) — added to the source
list in "wrong one first" order, with a RulePack pinning one or the other:

| Test | Proves |
|---|---|
| `ExactRulePackPinnedRegistry_IsUsed_NotAnyOtherRegistryInSource` | Pinning the matching registry passes. |
| `TwoRegistryVersions_ValidateIndependently` | Both scenarios (pinned-matching vs. pinned-non-matching) produce independently correct results in the same run. |
| `SourceOrder_DoesNotAffectRegistryChoice` | The non-matching registry is `RuntimeConditionValueRegistries[0]` (first in source order) yet the matching-registry scenario still passes — proves source order is irrelevant. |
| `InvalidConditionValue_FailsAgainstThePinnedRegistry` | Pinning the non-matching registry correctly fails. |
| `FirstOrDefault_IsAbsentFromTheActiveOrCandidatePath` | Reflection-based proof that `WorkoutProgressionValidator` no longer references `RuntimeConditionValueRegistryDefinition` at all. |

No permanent catalog registry version was added — the real catalog still has exactly one
`RUNTIME_CONDITION_VALUES_V1 v1`, unchanged (content and hash untouched by this migration).

## Tests (5 — `tests/PlanCatalog.Tests/Validation/PinnedRegistryResolutionTests.cs`)

Tests 20-24 of the Part 2 required list, all passing.

## Final status: `PINNED_REGISTRY_RESOLUTION` = **IMPLEMENTED**
