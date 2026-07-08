# Dependency Version Cascade Audit — CASCADE-001

## Update (Part 3): superseded by the deterministic-graph migration

`TEN_K__4D__INTERMEDIATE v3` (this report's "active" root) has since been superseded by **`v4`**, and `v3`
has been retired from new-release eligibility (ledger-only, `v3`'s own JSON is unchanged) — see
`deterministic-graph-part2-migration.md`/`part3-completion.md`. The cascade principles this report
established (immutable version bumps, no improper parent mutation) were followed identically for the `v4`
cascade (`TEN_K_MASTER v3`, `TEN_K_WORKOUT_PROGRESSION_V1 v2`, `INTERMEDIATE_MODIFIER v2`). This report's
trace below remains an accurate historical record of the `v3` cascade; it is not rewritten.

## Full active dependency graph trace (post-remediation, superseded by v4 — see update above)

```
TEN_K__4D__INTERMEDIATE v3 (NEW — active)
├── masterTemplate:      TEN_K_MASTER v2                          — UNCHANGED (already active since 0.3.0-pilot's TAPER fix)
├── layout:               RUN_LAYOUT_4D v1                         — UNCHANGED
├── levelModifier:        INTERMEDIATE_MODIFIER v1                 — UNCHANGED
│     └── progressionModifier: INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1 — UNCHANGED
└── rulePack:             APPSEL_RACE_PLAN_V1 v2                   — NEW
      ├── runtimeConditionValueRegistry: RUNTIME_CONDITION_VALUES_V1 v1 — UNCHANGED
      └── peakVolumeBandPolicy:          PEAK_VOLUME_BANDS_V1 v2   — NEW (corrected)

Effective workouts (resolved by TEN_K_WORKOUT_PROGRESSION_V1 v1's unversioned candidate keys,
filtered by INTERMEDIATE_MODIFIER v1's unversioned eligible-key set, via FindWorkout's
highest-non-retired-version resolution):
  EASY_STANDARD v2, FARTLEK v2, LONG_RUN_STANDARD v2, THRESHOLD_TEMPO v2, GOAL_PACE_TEN_K v1 (only version)
```

## At every versioned node: did its serialized content or pinned reference change?

| Node | Content/reference changed? | Already published? | Action taken |
|---|---|---|---|
| `WORKOUT_DEFINITION` EASY_STANDARD/FARTLEK/LONG_RUN_STANDARD/THRESHOLD_TEMPO v1 | Restored to ORIGINAL (reverted the earlier mutation) | Yes | v1 preserved as restored; v2 created with the corrected content |
| `TEN_K_WORKOUT_PROGRESSION_V1` v1 | **No** — `workoutCandidateKeys` are plain, unversioned strings; byte-identical before/after | Yes | **Not bumped** — no serialized change |
| `INTERMEDIATE_MODIFIER` v1 | **No** — `eligibleWorkoutKeys` are plain, unversioned strings; byte-identical before/after | Yes | **Not bumped** — no serialized change |
| `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v1 | No | Yes | Not bumped |
| `PEAK_VOLUME_BAND_POLICY` PEAK_VOLUME_BANDS_V1 v1 | Restored to ORIGINAL | Yes | v1 preserved as restored; v2 created with the corrected INTERMEDIATE rows |
| `RUNTIME_CONDITION_VALUES_V1` v1 | No | Yes | Not bumped |
| `RULE_PACK` APPSEL_RACE_PLAN_V1 v1 | **Yes, indirectly** — its pinned `peakVolumeBandPolicy` reference must now point at v2 | Yes | v1 preserved unchanged (still correctly points at restored peak-policy v1); **v2 created**, pointing at peak-policy v2 |
| `TEN_K_MASTER` v1/v2 | No | Yes | Not bumped (untouched by this task) |
| `RUN_LAYOUT_4D` v1 | No | Yes | Not bumped |
| `TEMPLATE_COMBINATION` TEN_K__4D__INTERMEDIATE v1/v2 | **Yes, indirectly** — v1/v2 both pin `rulePack` v1 | Yes | v1 and v2 preserved unchanged; **v3 created**, pinning `rulePack` v2 — v3 is the new active pilot combination |
| `PUBLISHED_TEMPLATE_BUNDLE` (derived) | Content changes whenever any resolved dependency changes — not an independently-versioned source artifact; its version is always inherited from its root combination's version | N/A (derived) | Not treated as a source artifact to version; see below |

**No published parent artifact was mutated in place.** Every node whose effective content changed as a
*result* of a child correction got a new version; every node whose own serialized bytes were unaffected
was deliberately left alone (`TEN_K_WORKOUT_PROGRESSION_V1`, `INTERMEDIATE_MODIFIER`,
`INTERMEDIATE_PROGRESSION_MODIFIER_V1`, `TEN_K_MASTER`, `RUN_LAYOUT_4D`, `RUNTIME_CONDITION_VALUES_V1`).

## New parent versions created

| Artifact | New version | Reason |
|---|---:|---|
| `RULE_PACK` APPSEL_RACE_PLAN_V1 | v2 | `peakVolumeBandPolicy` reference must point at the corrected policy v2 |
| `TEMPLATE_COMBINATION` TEN_K__4D__INTERMEDIATE | v3 | `rulePack` reference must point at the corrected rule pack v2 |

No other parent was bumped — `WorkoutProgressionDefinition`, `LevelModifierDefinition`, and
`ProgressionModifierDefinition` reference workouts and each other only by unversioned key, so their own
serialized content never changed and none required a new version.

## Derived bundle-hash mismatch — explanation and re-evaluation

`PublishedTemplateBundle` is **not** an independently-versioned source catalog identity — per this task's
explicit instruction ("do not version a bundle as if it were a source catalog artifact"), its `BundleVersion`
is always inherited from its root `TemplateCombinationDefinition`'s `Metadata.Version`, and its content is
computed fresh at assembly time from whatever the current source resolves to.

**Root cause of the historical bundle-hash mismatch**: `PublishedTemplateBundle`'s `Workouts` list is
resolved via `CatalogBundleAssembler`'s call to `FindWorkout(key)` — key-only, no version pin anywhere in
the reference chain. This means a combination's bundle content is **not** a pure function of that
combination's own version — it also depends on whichever workout versions exist in source at build time.
Before this remediation only one workout version ever existed, so this was invisible; the moment `v2`
workouts exist, `FindWorkout` (by design, now made explicitly deterministic — see
`published-workout-immutability-remediation.md`) resolves the newer version for **every** combination that
references those keys, including combination v1 (a historical/superseded root) and v2.

**Consequence, verified**: rebuilding bundle v1 (and v2) fresh from today's corrected source produces a
hash that matches **none** of their 5 respective historical publications, because:
1. `1.0.0`/`0.1.0-pilot` predate the existence of any second workout version and predate the
   peak-volume-policy correction.
2. `0.2.0-pilot`/`0.3.0-pilot`/`0.4.0-pilot` were built against the *mutated* (pre-restoration) workout and
   policy content.
3. Today's fresh build resolves the newly-created, intentionally-corrected v2 workouts.

**This is not an independent mutable-catalog-identity violation.** It is a fully-explained, derivative
consequence of (a) the two remediated source-artifact mutations above and (b) `PublishedTemplateBundle`'s
inherent design (unversioned-key-based, always-freshest-content resolution). Verified via:
1. **Rebuild the current active bundle** — `DependencyVersionCascadeTests.ActiveCombinationV3_ResolvesAFullyConsistentVersionedGraph` rebuilds combination v3's bundle and confirms every dependency resolves to its correct (v2-where-corrected) version.
2. **Deterministic hash** — `WorkoutArtifactImmutabilityTests`/`PeakVolumePolicyImmutabilityTests` prove each underlying artifact's hash is stable across repeated computation; bundle hash determinism follows directly (proven historically for combination v2's bundle in `combination-v2-hash-and-closure-audit.md`, same mechanism applies here).
3. **Historical bundle hashes remain unchanged** — no historical release directory was modified; `HistoricalCombinations_RemainIndependentlyVerifiable` confirms v1/v2/v3 all validate against the live snapshot, and `verify-release` (see final report) confirms every historical release's own bundle files are untouched.
4. **Documented as derivative, not independently mutable** — see the `PUBLISHED_TEMPLATE_BUNDLE` entries in `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json`, each explicitly annotated `reasonForCanonicalDirection` explaining the derivation. No artifact-level exception was created for a bundle as if it were a first-class versioned identity — the existing bundle-tracking mechanism (identity = `bundleKey` + `bundleVersion`, inherited from the combination) was reused as-is.

## Tests added (`tests/PlanCatalog.Tests/Publishing/DependencyVersionCascadeTests.cs`)

| Test | Proves |
|---|---|
| `RulePackV1_IsUnchanged_StillReferencesRestoredPeakPolicyV1` | v1's hash is untouched; still references peak-policy v1. |
| `RulePackV2_IsNew_ReferencesCorrectedPeakPolicyV2` | v2 correctly references peak-policy v2 (and unchanged registry v1). |
| `WorkoutProgressionAndLevelModifier_AreUnchanged_NoVersionBumpNeeded` | Both remain at v1 with their original, unchanged hash. |
| `CombinationV1AndV2_AreUnchanged_OnlyV3IsNew` | v1/v2 hashes match their historical values exactly; v3's rulePack reference is v2, others unchanged from v2. |
| `ActiveCombinationV3_ResolvesAFullyConsistentVersionedGraph` | Full bundle assembly from v3 resolves every dependency to its correct version, including workout v2s. |
| `HistoricalCombinations_RemainIndependentlyVerifiable` | v1/v2/v3 all pass `TemplateCombinationValidator` against the live snapshot. |
| `NoDuplicateKeyVersionAcrossAnyRemediatedIdentity` | `CatalogGraphValidator` reports no `GRAPH_DUPLICATE_KEY_VERSION` anywhere. |

All pass. Full suite: 203/203.

## Files changed

- `catalog/rule-packs/appsel-race-plan.v2.json` (new)
- `catalog/combinations/ten-k-4d-intermediate.v3.json` (new)
- `src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` (AUD-058, AUD-059 added)
- `tests/PlanCatalog.Tests/Publishing/DependencyVersionCascadeTests.cs` (new)

## Final status: `DEPENDENCY_VERSION_CASCADE` = **VERIFIED, NO IMPROPER PARENT MUTATION**
