# Part 3 Retirement and Release Plan — PART3-PLAN-001

**This plan is not executed. No retirement ledger entry was written. No release-status ledger entry was
written. No release was published.** Part 3 will execute this plan after independent review.

## 1. Candidate root

- Key: `TEN_K__4D__INTERMEDIATE`
- Version: `4`
- Content hash: `5c6c701b783ee04794a3d2b19aa0d6b640ea3263f89d415bff396d01a6f3daac`
- Referred to as **"candidate replacement root"** throughout this task, per Milestone G — not "active,"
  since Part 2 does not retire its predecessors.

## 2. Candidate predecessor and rationale

**Predecessor: `TEN_K__4D__INTERMEDIATE v3`** (not v1, not v2). Inspected all three:

| Version | masterTemplate | levelModifier | rulePack | Includes TAPER fix? | Includes RulePack/peak-volume fix? |
|---|---|---|---|---|---|
| v1 | `TEN_K_MASTER v1` | `INTERMEDIATE_MODIFIER v1` | `APPSEL_RACE_PLAN_V1 v1` | No | No |
| v2 | `TEN_K_MASTER v2` | `INTERMEDIATE_MODIFIER v1` | `APPSEL_RACE_PLAN_V1 v1` | Yes | No |
| v3 | `TEN_K_MASTER v2` | `INTERMEDIATE_MODIFIER v1` | `APPSEL_RACE_PLAN_V1 v2` | Yes | Yes |

v3 is the only version carrying **both** prior corrections (TAPER phase-family fix and the corrected
peak-volume-policy-bearing RulePack). v4 extends v3 by exactly the two things this Part 2 migration adds:
exact-versioned dependency references (`TEN_K_MASTER v3`, `INTERMEDIATE_MODIFIER v2`) and the resulting
`LONG_RUN_STANDARD` closure inclusion. v1 and v2 are each missing at least one prior correction and are
therefore not the most complete baseline — they remain superseded-but-not-yet-retired historical versions,
same as before this task.

## 3. Exact old root versions to retire

- `TEMPLATE_COMBINATION / TEN_K__4D__INTERMEDIATE / v1`
- `TEMPLATE_COMBINATION / TEN_K__4D__INTERMEDIATE / v2`
- `TEMPLATE_COMBINATION / TEN_K__4D__INTERMEDIATE / v3`

Dependencies exclusive to the retired chain (`TEN_K_MASTER v1`/`v2`, `INTERMEDIATE_MODIFIER v1`,
`TEN_K_WORKOUT_PROGRESSION_V1 v1`) are **not** proposed for retirement — nothing in the retirement/publish
pipeline requires a dependency to be retired merely because its only combination consumer is; they remain
readable historical artifacts, consistent with Decision E. Retiring only the combination is the
"smallest appropriate" action, matching the precedent set in `full-catalog-retirement-packaging-audit.md`.

## 4. Exact retirement entries to add (Part 3, not yet written)

```json
[
  { "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 1 },
  { "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 2 },
  { "documentType": "TEMPLATE_COMBINATION", "key": "TEN_K__4D__INTERMEDIATE", "version": 3 }
]
```

To be written via the CLI `retire` command (`retire --type TEMPLATE_COMBINATION --key
TEN_K__4D__INTERMEDIATE --version {1,2,3}`), one call per version, in Part 3.

## 5. Retirement-ledger pre-change snapshot

`artifacts/appsel-plan-catalog/retirements.json` **does not exist** (confirmed by direct file-existence
check, both before and after this Part 2 task — see `ActiveVersionPreparationTests.RealCatalogRetirementPlan_IsGeneratedButNotExecuted`).
Pre-change state: absent. Part 3 will create this file for the first time with exactly the 3 entries in
section 4 — no other entries.

## 6. Expected full-catalog bundle list after retirement

After retiring v1/v2/v3 and publishing the next release, `CatalogPublisher.BuildRelease`'s eligible-
combination filter (`!retirementLedger.IsRetired(...)`) will leave exactly **one** `TEN_K__4D__INTERMEDIATE`
bundle: **v4**. All other artifact types (workouts, layouts, rule packs, registries, policies, templates,
progressions — including the now-superseded v1/v2/v3-only dependencies) continue to be republished as
individual artifacts in the release's per-type folders regardless of combination retirement (matching
brief §15's full-catalog-packaging design, unchanged by this task) — only the **bundle** list narrows.

## 7. Next Pilot release version

`0.6.0-pilot` — the next version after the current latest (`0.5.0-pilot`), per repository reality (no
version number was assumed in advance; this follows directly from `artifacts/appsel-plan-catalog/`'s
actual directory listing).

## 8. Previous Pilot release to supersede

`0.5.0-pilot` — to be marked `SUPERSEDED` via the existing non-destructive `supersede-release` ledger
mechanism (never mutating `0.5.0-pilot`'s own directory), **only after** `0.6.0-pilot` publishes and
verifies successfully.

## 9. Expected decision-level Production blocker count (candidate root v4 closure)

**13** — computed via `BlockerScopeMeasurement.ScopedDecisionCount` over `TEN_K__4D__INTERMEDIATE v4`'s
exact dependency closure. See `deterministic-graph-part2-migration.md`/final report for the full breakdown
and the 9 distinct blocking artifacts this maps to (item 10).

## 10. Expected artifact-level blocker count (candidate root v4 closure)

**9** distinct blocking `(documentType, key, version)` identities:
`PEAK_VOLUME_BAND_POLICY/PEAK_VOLUME_BANDS_V1/v2`, `PROGRESSION_MODIFIER/INTERMEDIATE_PROGRESSION_MODIFIER_V1/v1`,
`RUN_LAYOUT/RUN_LAYOUT_4D/v1`, `RUNTIME_CONDITION_VALUE_REGISTRY/RUNTIME_CONDITION_VALUES_V1/v1`,
`WORKOUT_DEFINITION/EASY_STANDARD/v2`, `WORKOUT_DEFINITION/FARTLEK/v2`, `WORKOUT_DEFINITION/GOAL_PACE_TEN_K/v1`,
`WORKOUT_DEFINITION/LONG_RUN_STANDARD/v2`, `WORKOUT_DEFINITION/THRESHOLD_TEMPO/v2`. Production-channel
publish will therefore remain correctly blocked in Part 3 (`PublishReadinessValidator.ValidateContentDecisions`)
until these are resolved — this plan does **not** propose resolving them; that is unrelated domain-content
work, out of scope for both Part 2 and this plan.

## 11. Rollback procedure if retirement, publish, or verification fails

1. **If a `retire` CLI call fails partway** (e.g. 2 of 3 entries written): the ledger is a simple additive
   JSON array — remove the partially-written entries by hand (or re-run `retire` for the missing ones;
   `retire` entries are idempotent additions, not a transactional batch). No release has been touched at
   this point, so no further action is needed.
2. **If `publish 0.6.0-pilot` fails validation** (schema/source-integrity/publish-graph/cross-release-hash):
   `CatalogPublisher.Publish` never calls `WriteRelease` until every validation stage passes, so **no
   partial release directory is ever written** — confirmed by the existing atomic-publish design and its
   tests. Fix the underlying cause and re-run `publish`; no rollback of the ledger is needed (the
   retirement entries remain valid regardless of publish outcome).
3. **If `publish 0.6.0-pilot` succeeds but `verify-release 0.6.0-pilot` fails**: do **not** delete or edit
   `artifacts/appsel-plan-catalog/0.6.0-pilot/` (never mutate an immutable release directory, even a
   just-published one). Do **not** run `supersede-release` for `0.5.0-pilot` (step 8 is conditioned on
   successful verification). Investigate and fix forward with a new corrected release (e.g. `0.6.1-pilot`
   or `0.7.0-pilot`) using the exact same non-mutating pattern already established by this project's prior
   sessions (`combination-immutability-investigation.md`, `dependency-version-cascade-audit.md`) — never
   retroactively alter `0.6.0-pilot`.
4. **In all failure cases**: `0.5.0-pilot` and every earlier release remain completely untouched and
   continue to verify — retirement and publish are additive/non-destructive by construction.

## Confirmation

This plan is generated only. No retirement entry was written, no release-status ledger entry was written,
no numbered release was published, and no immutable release directory was modified in the process of
producing this plan.
