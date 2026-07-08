# TEN_K / 4D / INTERMEDIATE Pilot — Domain Review Summary

This review reconciled the pilot catalog's 36 pre-existing domain-content audit entries against Golden
Fixture v3 (`docs/canonical/golden-fixture-v3/`), per the source-governance hierarchy defined in
`plan-catalog/docs/README.md`. It produced 55 field-level entries (`AUD-001`..`AUD-055`) — some original
entries were split into finer-grained fields where the fixture confirmed part but not all of a
previously-lumped field.

**Update (follow-up pass)**: the TAPER phase workout-family conflict originally recorded as an open,
unresolved issue (entry `AUD-007`) was subsequently investigated and resolved — see
`taper-family-conflict-investigation.md` and the "TAPER phase family conflict — now resolved" section below.

**Update (immutability correction)**: the TAPER fix was initially applied by mutating
`catalog/combinations/ten-k-4d-intermediate.v1.json` in place (`masterTemplate.version` changed 1→2 while
the declared version stayed 1), which caused the `0.3.0-pilot` release to pin combination v1 under a hash
that differs from the hash pinned by every earlier release. This has been corrected: v1 was restored to its
exact historical content and hash; a genuine `v2` combination artifact was created; `0.3.0-pilot` was
preserved unchanged and marked superseded; a corrected release (`0.4.0-pilot`) now pins combination v2. See
`combination-immutability-investigation.md` for full detail.

**Update (repository-wide immutability hardening)**: a full cross-release hash-consistency scan (never
previously run repository-wide) found **5 additional, previously-undetected** in-place mutations of the
same class as the combination defect above: all 4 pilot `WORKOUT_DEFINITION` v1 artifacts
(`EASY_STANDARD`/`FARTLEK`/`LONG_RUN_STANDARD`/`THRESHOLD_TEMPO`) and `PEAK_VOLUME_BAND_POLICY
PEAK_VOLUME_BANDS_V1` v1 had each been edited in place during earlier domain-content reconciliation passes.
All 5 have been remediated: each v1 was restored to its exact earliest historically-published content;
each corrected/intended content now lives on a genuinely new v2 artifact. The cascade this triggered
(`RulePackDefinition APPSEL_RACE_PLAN_V1` v2, `TemplateCombinationDefinition TEN_K__4D__INTERMEDIATE` v3)
was handled immutably — no already-published parent was mutated in place. A repository-wide
`CrossReleaseHashConsistencyTest` and a publish-time cross-release hash guard
(`PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION`) now exist to catch this class of defect automatically going
forward. See `published-workout-immutability-remediation.md`, `peak-volume-policy-immutability-remediation.md`,
`dependency-version-cascade-audit.md`, and `cross-release-hash-consistency-audit.md`.

## What changed

1. **Vocabulary correction**: `PrescriptionMode`'s invented values (`PACE_BASED`/`EFFORT_BASED`/`HEART_RATE_BASED`)
   did not match Golden Fixture v3 at all. The fixture's real vocabulary is `DISTANCE`/`MIXED`. Both new
   values were added as `CANONICAL_CONFIRMED`; the three legacy values were kept (not deleted) because
   `GOAL_PACE_TEN_K` — the one pilot workout with zero fixture evidence — still depends on `PACE_BASED`,
   and this review does not invent a replacement for it.
2. **New vocabulary introduced**: `DistanceAccountingMode` (`EXACT_SESSION_TOTAL` / `ESTIMATED_SESSION_TOTAL`
   / `EMBEDDED_COMPONENTS`), confirmed verbatim from the fixture, added as a new optional field on
   `WorkoutDefinition`. Kept strictly separate from `PrescriptionMode` (zero value overlap, test-enforced).
3. **4 of 5 pilot workout definitions upgraded from PLACEHOLDER_UNCONFIRMED to CANONICAL_CONFIRMED** for
   `family`, `eligiblePhases`, `allowedPrescriptionModes`, and the new `allowedDistanceAccountingModes` —
   `EASY_STANDARD`, `LONG_RUN_STANDARD`, `FARTLEK`, and `THRESHOLD_TEMPO` all appear verbatim as workout
   keys in Golden Fixture v3, with directly observable per-key family/phase/prescription/accounting facts.
   `GOAL_PACE_TEN_K` has no fixture evidence and was left entirely unchanged.
4. **Phase preferred-weeks allocation (3/4/4/1)** gained an independent second source — Golden Fixture v3's
   `phaseAllocation` resolves to the exact same 3/4/4/1 split for a real 12-week plan.
5. **Peak-volume 4-day INTERMEDIATE row (30-42km)** gained an independent second source — the fixture's
   `peakVolume.typicalBandKm` matches exactly. (The fixture's `resolvedPeakKm=38` was explicitly NOT stored
   as policy — it is a per-athlete computed instance value, not a reusable band.)
6. **Runtime condition registry**: `GOAL_FEASIBILITY_IN` and `PLAN_MODE_IN` gained fixture corroboration
   (the generated plan actually uses `REALISTIC` and `STANDARD`, both already-allowed values).

## What was explicitly NOT changed, and why

- **`MaximumHardSessionsPerWeek` / `MainSetDoseMultiplier` / other progression-modifier dosage fields**
  remain `PLACEHOLDER_UNCONFIRMED`. `progression_rules_v2.yaml` (the only versioned rule file actually
  present under `docs/canonical/`) defines weekly-volume percentage caps and cutback ratios — a different
  concept entirely — and never states a hard-session cap or dose multiplier. The fixture realizing exactly
  one hard session/week for this one plan does not, per explicit instruction, prove a general rule.
- **`PACE_SOURCE_IN` / `TIME_ADEQUACY_IN` / `CORE_ENTRY_READINESS_IN` registry values** remain unconfirmed.
  The fixture's DecisionTrace has similarly-named internal resolver fields (`paceSource=RECENT_RACE`,
  `timeAdequacy=ADEQUATE`, `readiness=STANDARD`) that mostly don't match our invented values, but per
  explicit instruction this is Process-B-internal output whose ownership mapping to the Process A registry
  is not established — not silently rewritten.
- **`PeakVolumeBandPolicy` NEW/ADVANCED/EXPERIENCED rows** remain unconfirmed placeholders; not interpolated
  or extrapolated from the INTERMEDIATE-only fixture.
- **Generated-output component labels** (`FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`, `ACTIVATION_REPEATS`, etc.)
  were not added to `WorkoutComponentType`; ownership recorded as unresolved in
  `ten-k-pilot-vocabulary-decisions.md`.
- **Artifact version parity (APPSEL_RACE_PLAN_V1 only)**: Golden Fixture v3 references
  `APPSEL_RACE_PLAN_V1 v3`; the catalog remains at `v1`. Not upgraded — semantic impact unknown (this
  pilot's rule pack `policies`/`rules` arrays are empty), recorded as `ARTIFACT_VERSION_PARITY_UNRESOLVED`.

## TAPER phase family conflict — now RESOLVED_CANONICALLY

The TAPER conflict noted above was investigated in a dedicated follow-up pass. Golden Fixture v3 Week 12
(`$.weeks[11].days[0]` and `$.weeks[11].days[3]`) shows a QUALITY-family workout (`RACE_PACE_REPEATS`) and
a RACE-family workout (`RACE_DAY`) both scheduled in the TAPER phase, while `TEN_K_MASTER`'s TAPER
`eligibleWorkoutFamilies` declared only `[EASY, LONG_RUN]`. This was a source-backed catalog-definition
defect, not missing data.

**Correction**: `catalog/templates/ten-k-master.v2.json` was created (v1 is already PUBLISHED/immutable
across releases `1.0.0`, `0.1.0-pilot`, `0.2.0-pilot`, so it was left untouched) with TAPER
`eligibleWorkoutFamilies = [EASY, LONG_RUN, QUALITY, RACE]`. `WorkoutFamily.Taper` was **not** introduced —
TAPER remains exclusively a `PhaseKey`. `AUD-007` is now `CANONICAL_CONFIRMED`. See
`taper-family-conflict-investigation.md` for full detail.

**Corrected wording (Part 3, was stale)**: `catalog/combinations/ten-k-4d-intermediate.v1.json` was
**never** repointed at `TEN_K_MASTER v2` — it remains unchanged, referencing `TEN_K_MASTER v1`, exactly as
originally published. The full, current combination → master/RulePack mapping is:
- `TEN_K__4D__INTERMEDIATE v1` → `TEN_K_MASTER v1` (unchanged, original)
- `TEN_K__4D__INTERMEDIATE v2` → `TEN_K_MASTER v2` (the genuinely new artifact created for the TAPER fix)
- `TEN_K__4D__INTERMEDIATE v3` → `TEN_K_MASTER v2` + `APPSEL_RACE_PLAN_V1 v2` (the peak-volume-policy fix)
- `TEN_K__4D__INTERMEDIATE v4` (**active root as of Part 3**) → `TEN_K_MASTER v3` + `APPSEL_RACE_PLAN_V1 v2`
  + exact-versioned `TEN_K_WORKOUT_PROGRESSION_V1 v2` / `INTERMEDIATE_MODIFIER v2` dependencies — see
  `deterministic-graph-part2-migration.md` and `deterministic-graph-part3-completion.md`.

v1/v2/v3 were retired from new-release eligibility in Part 3 (ledger-only — no combination JSON was
mutated, moved, or deleted) once v4 was confirmed as the sole active root; see
`retirement-ledger-application.md` and `active-version-uniqueness-audit.md`.

## Counts

- Total entries: 83 (up from 55; growth is primarily from WORKOUT-IMMUT-001/PEAK-POLICY-IMMUT-001/CASCADE-001 splitting each remediated identity's field-level classifications across its restored v1 and corrected v2 versions)
- CANONICAL_CONFIRMED: 36
- PLACEHOLDER_UNCONFIRMED (blocking, total-catalog scope): 36 — see `placeholder-scope-audit.md` for the
  eligible-release-union (13) and active-root-closure (13) scopes, which are what actually gate a new publish
- TECHNICAL_ONLY: 11
- EXPLICIT_PRODUCT_DEFAULT: 0 (no value was promoted to this tier — no explicit written product approval with a decision date exists for any candidate)

## Status

- Process A infrastructure: **COMPLETE**
- TEN_K / INTERMEDIATE / 4D pilot domain content: **PARTIALLY CONFIRMED — Production publish remains blocked**.
  Blocker counts are reported at three explicitly separated, non-comparable scopes (never a single flat
  number — see `placeholder-scope-audit.md` for the full methodology):
  - `totalCatalogPlaceholderDecisionCount` (all catalog history, every version): **36 decisions / 17 artifacts**
  - `eligibleReleaseUnionBlockingDecisionCount` (every combination currently eligible for a new release —
    post-Part-3-retirement this is just `TEN_K__4D__INTERMEDIATE v4`): **13 decisions / 9 artifacts**
  - `activeRootClosureBlockingDecisionCount` (the active root `TEN_K__4D__INTERMEDIATE v4`'s own exact
    dependency closure): **13 decisions / 9 artifacts**
  Production readiness for the active root uses the **13/9** figures, not the flat 36.
- TAPER phase family conflict: **RESOLVED_CANONICALLY**
- Combination v2 hash stability: **STABLE_DETERMINISTIC** (verified identical across source, bundle assembly, and published release — no defect found)
- Combination v2 dependency closure: **CORRECT** (v1 is not a dependency of v2; both bundles in `0.4.0-pilot` exist independently because the publisher packages the full catalog per release, matching brief §15 — not a bug)
- Full V1 catalog (other distances/levels/layouts): **INCOMPLETE** (out of scope for this review)
- Full-catalog retirement packaging: **FIXED** (a combination that is itself retired is now excluded from bundles/artifacts in any new release; see `full-catalog-retirement-packaging-audit.md`)
- Cross-release hash consistency: **IMPLEMENTED, THEN REMEDIATED** (repository-wide guard + test found the known `0.3.0-pilot` combination defect AND 5 additional, previously-undetected historical hash mismatches in `WORKOUT_DEFINITION`/`PEAK_VOLUME_BAND_POLICY` v1 artifacts. The combination defect remains a preserved historical exception (0.3.0-pilot is superseded, not rewritten); the 5 newly-discovered defects have since been actively remediated — v1 restored to original content, v2 created with the corrected content — see `published-workout-immutability-remediation.md` and `peak-volume-policy-immutability-remediation.md`)
- Dependency version cascade (RulePack v2, Combination v3): **VERIFIED, NO IMPROPER PARENT MUTATION** (see `dependency-version-cascade-audit.md`)
- Release-status ledger chain (`1.0.0`→`0.1.0-pilot`→`0.2.0-pilot`→`0.3.0-pilot`→`0.4.0-pilot`→`0.5.0-pilot`): **VERIFIED CORRECT** (see `release-status-chain-audit.md`; extended to `0.6.0-pilot` in Part 3 — see `deterministic-graph-part3-completion.md`)
- Deterministic dependency graph (Parts 1-3): **ACTIVE ROOT IS NOW `TEN_K__4D__INTERMEDIATE v4`** — `v1`/`v2`/`v3` retired from new-release eligibility (ledger-only, no source mutation); exact-versioned dependencies throughout; see `deterministic-graph-part3-completion.md`.

See `ten-k-pilot-domain-decision-audit.md` for the full field-by-field table,
`ten-k-pilot-vocabulary-decisions.md` for the dedicated vocabulary reconciliation,
`taper-family-conflict-investigation.md` for the TAPER phase family correction,
`combination-immutability-investigation.md` for the v1-mutation correction,
`combination-v2-hash-and-closure-audit.md` for the hash-stability and dependency-closure verification,
`full-catalog-retirement-packaging-audit.md` for the retired-combination packaging fix,
`cross-release-hash-consistency-audit.md` for the repository-wide hash-consistency guard and the newly
discovered historical defects, `release-status-chain-audit.md` for the release-status ledger
verification, `published-workout-immutability-remediation.md` and
`peak-volume-policy-immutability-remediation.md` for the 5 newly-discovered defects' remediation,
`dependency-version-cascade-audit.md` for the resulting RulePack v2 / Combination v3 cascade,
`placeholder-scope-audit.md` for the decision-level/artifact-level blocker methodology, and
`deterministic-graph-part3-completion.md` for the final active-root retirement and `0.6.0-pilot` release.
