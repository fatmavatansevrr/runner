# Deterministic-Graph Pre-Change Assessment — PRECHANGE-001

**Read-only assessment. No code, schema, or catalog artifact was modified to produce this report. No
release was published.** Part 1 of a 3-part sequence; this document reports findings only.

## Target architectural outcome (context, not implemented here)

> The same root combination version must always resolve the same exact versioned dependency graph and
> produce the same bundle, regardless of what newer artifact versions are later added to the source
> catalog.

The catalog **currently does not satisfy this outcome** — see Finding 5 below, which is directly
demonstrated by live evidence: `TEN_K__4D__INTERMEDIATE` v1, v2, and v3 all resolve to the identical
effective workout set (`EASY_STANDARD v2, FARTLEK v2, GOAL_PACE_TEN_K v1, THRESHOLD_TEMPO v2`) today, even
though v1 was originally published (in `1.0.0`) when only workout v1s existed. Rebuilding v1's bundle
today produces different content than what `1.0.0` actually pinned for it.

---

## Finding 1 — CatalogPublisher validates the full graph before retired root combinations are filtered

- **Reproduced**: true
- **File(s)**: `src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs`
- **Method / lines**: `CatalogPublisher.BuildRelease`, lines 49–77
- **Current behavior**: `BuildRelease` calls `ValidateSchemas(snapshot)` (line 51), then
  `CatalogGraphValidator.Validate(snapshot, retirementLedger)` (line 57), then
  `CatalogStamper.StampAsPublished(...)` (line 63), then `PublishReadinessValidator.Validate(stamped, ...)`
  (line 65) — all four calls operate on the **full, unfiltered** `snapshot`/`stamped` snapshot, including
  any retired combinations. Only afterward, at line 74–77, is `eligibleCombinations` computed by filtering
  `retirementLedger.IsRetired(...)`. `CatalogGraphValidator.Validate` (line 97–100 of
  `src/PlanCatalog.Core/Validation/CatalogGraphValidator.cs`) iterates **every** combination in
  `snapshot.Combinations` — retired or not — and calls `TemplateCombinationValidator.Validate`, which
  itself checks retirement of the combination's *dependencies* (e.g. `TC_PROGRESSION_MODIFIER_RETIRED`).
  If a combination is retired specifically *because* one of its dependencies was also retired (a normal
  cleanup sequence), this loop still raises an `Error`-severity issue for that already-retired combination,
  making `domainResult.IsValid` false and causing `BuildRelease` to `throw` at line 58–61 — **before** the
  retirement filter at line 74 is ever reached.
- **Risk if left unfixed**: a legitimate new full-catalog release can be blocked entirely by a combination
  that was correctly retired (along with its now-invalid dependencies), even though that combination would
  have been correctly excluded from the release anyway. The publish fails for a combination that isn't
  even going to be published.
- **Affected releases**: none historically (retirements.json has never existed — see Finding 2's evidence),
  so this has not yet caused an observed failure, but the code path is present in every release built by
  the current `CatalogPublisher` (`1.0.0` through `0.5.0-pilot`) and remains latent risk for the very next
  retirement.
- **Affected active root combination(s)**: any — this is a publisher-pipeline-wide ordering defect, not
  specific to `TEN_K__4D__INTERMEDIATE`.
- **Proposed correction (describe only)**: filter `stamped.Combinations`/`snapshot.Combinations` to
  non-retired ones *before* running `CatalogGraphValidator.Validate` and `PublishReadinessValidator.Validate`
  — or, equivalently, have `TemplateCombinationValidator` skip a combination entirely (not just its
  dependency checks) once the combination itself is retired, since a retired combination's internal
  consistency is no longer relevant to a new release.
- **Expected version cascade if corrected**: none. This is a pure ordering/logic change inside
  `CatalogPublisher`/`CatalogGraphValidator`; no catalog artifact needs a new version.

---

## Finding 2 — TEN_K__4D__INTERMEDIATE v1, v2, v3 are all included as active sibling bundles in new full-catalog releases

- **Reproduced**: true
- **File(s)**: `src/PlanCatalog.Infrastructure/Publishing/CatalogPublisher.cs`; live evidence:
  `artifacts/appsel-plan-catalog/0.5.0-pilot/release-manifest.json`
- **Method / lines**: `CatalogPublisher.BuildRelease`, lines 74–81 (`eligibleCombinations` = every
  combination not retired; `bundles` = one bundle per eligible combination)
- **Current behavior**: `0.5.0-pilot`'s manifest lists 3 `PUBLISHED_TEMPLATE_BUNDLE` entries for key
  `TEN_K__4D__INTERMEDIATE` — versions 1, 2, and 3 — each with a distinct `contentHash`
  (`b6420adb...`, `d0f23f4f...`, `5cf81183...`). `artifacts/appsel-plan-catalog/retirements.json` **does
  not exist** on disk, confirmed by direct file-existence check — nothing has ever been retired in this
  catalog, so every combination version ever created remains "eligible" under the current filter.
- **Risk if left unfixed**: as more combination versions accumulate over time (which is the catalog's own
  established practice — see `combination-immutability-investigation.md`, `dependency-version-cascade-audit.md`),
  every new full-catalog release grows an ever-larger set of "sibling" bundles for the same logical
  combination, with no system-level notion of which one is "the" active/default version. This directly
  underlies the "active root combination is ambiguous" condition raised in Task 3 below.
- **Affected releases**: `0.4.0-pilot` (first release with 2 combination versions: v1, v2) and `0.5.0-pilot`
  (first with 3: v1, v2, v3). Earlier releases (`1.0.0` through `0.3.0-pilot`) had only one combination
  version each.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` v1, v2, and v3 — all three,
  simultaneously, since none is retired.
- **Proposed correction (describe only)**: introduce an explicit "current/default version" concept for a
  combination key (e.g., an authoring-time pointer, or formal retirement of superseded versions once a
  new one is confirmed correct) so that full-catalog packaging has a principled way to distinguish "still
  historically valid, keep publishing" from "actively the one that should be selected by default."
- **Expected version cascade if corrected**: none directly — this would be a ledger/policy addition
  (e.g., retiring v1/v2, or adding a new "active pointer" concept), not a change to any existing artifact's
  content or version number.

---

## Finding 3 — Domain reports conflate three different blocker scopes into one misleading number

- **Reproduced**: true
- **File(s)**: `artifacts/audits/ten-k-pilot-domain-review-summary.md`
- **JSON path / lines**: lines 97–98 and line 106
- **Current behavior**: line 97 reports `PLACEHOLDER_UNCONFIRMED (blocking): 36` — this is
  `PilotDomainContentAudit.Entries.Count(e => e.Classification == PlaceholderUnconfirmed)`, i.e. **every**
  placeholder entry across the **entire audit list**, spanning both restored/historical versions (e.g.
  `WORKOUT_DEFINITION/EASY_STANDARD/v1`, which is not reachable from any currently-eligible bundle) and
  active/corrected versions (`v2`). Line 106 then states "(36 blocking placeholders remain in the
  dependency closure)" — using the **exact same number**, but describing it as if it were scoped to "the
  dependency closure" (i.e. an active-root-closure concept). These are two different scopes reported as
  one identical figure. Measured directly in Task 3 below: the true active-root-closure blocker count for
  `TEN_K__4D__INTERMEDIATE` v3 is **11**, not 36; the true eligible-release-union blocker count (union
  across all 3 non-retired combination bundles) is **12**, also not 36.
- **Risk if left unfixed**: a reader (or an automated publish-readiness gate) relying on "36" as "how many
  placeholders block *this specific release/combination*" would be off by more than 3x, and would not be
  able to distinguish "content that blocks the release I'm about to publish" from "content that exists
  somewhere in catalog history and can never block anything I publish today."
- **Affected releases**: none directly (this is a reporting/documentation artifact, not a publish-time
  gate — `PublishReadinessValidator.ValidateContentDecisions` itself correctly scopes to
  `bundleArtifactTuples`, i.e. the eligible-release union, not the flat total). The conflation exists only
  in the human-readable summary documents.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` (all versions, since the flat "36"
  figure doesn't distinguish any of them).
- **Proposed correction (describe only)**: report the three scopes (`totalCatalogPlaceholderCount`,
  `eligibleReleaseUnionBlockerCount`, `activeRootClosureBlockerCount`) as three explicitly labeled,
  separate numbers, per Task 3's methodology below, rather than one flat "blocking" count.
- **Expected version cascade if corrected**: none — documentation/reporting-generation change only.

---

## Finding 4 — Stale wording claiming combination v1 points to TEN_K_MASTER v2

- **Reproduced**: true
- **File(s)**: `artifacts/audits/ten-k-pilot-domain-review-summary.md`
- **Line**: line 89: "`catalog/combinations/ten-k-4d-intermediate.v1.json` now points its `masterTemplate`
  reference at v2."
- **Current behavior (actual, verified)**: `catalog/combinations/ten-k-4d-intermediate.v1.json`'s
  `masterTemplate` field is `{"documentType":"PLAN_TEMPLATE","key":"TEN_K_MASTER","version":1}` — version
  **1**, not 2. Verified by direct read of the file. This wording is a leftover from the original TAPER-fix
  task, written **before** the subsequent immutability investigation discovered that v1 had been mutated
  in place and restored it to reference `TEN_K_MASTER` v1 (see `combination-immutability-investigation.md`,
  `COMB-IMMUT-001) — the paragraph was never updated after that correction.
- **Risk if left unfixed**: a reader trusting this sentence would believe combination v1 is
  version-inconsistent with its own immutable historical hash, potentially re-triggering the exact
  confusion `COMB-IMMUT-001` was created to resolve.
- **Affected releases**: none (documentation only); the actual file content has been correct since the
  `0.4.0-pilot` correction.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` v1 (referenced incorrectly in prose).
- **Proposed correction (describe only)**: update the sentence to state that v1 was **left unchanged**
  (still referencing `TEN_K_MASTER` v1) and that v2 was the new artifact created to reference
  `TEN_K_MASTER` v2 — matching the immutability-investigation's actual outcome and the file's current,
  verified content.
- **Expected version cascade if corrected**: none — documentation wording fix only.

---

## Finding 5 — WorkoutProgressionDefinition + FindWorkout's highest-non-retired-version resolution causes the same combination version to resolve a different bundle over time

- **Reproduced**: true
- **File(s)**: `src/PlanCatalog.Core/Models/WorkoutProgressionStageDefinition.cs`;
  `src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs`;
  `src/PlanCatalog.Infrastructure/Publishing/CatalogBundleAssembler.cs`
- **Class/method**: `WorkoutProgressionStageDefinition.WorkoutCandidateKeys`
  (`IReadOnlyList<string>` — plain, unversioned); `CatalogSourceSnapshot.FindWorkout(string key,
  IRetirementLedger? retirementLedger = null)`, lines 42–49; consumed by
  `CatalogBundleAssembler.Assemble`, line 62 (`.Select(key => snapshot.FindWorkout(key, retirement))`)
- **Current behavior**: `FindWorkout` resolves `Workouts.Where(x => x.Metadata.Key == key &&
  !retirement.IsRetired(...)).OrderByDescending(x => x.Metadata.Version).FirstOrDefault()` — i.e. the
  **highest non-retired version** for a given key, computed fresh every time a bundle is assembled.
  **Live, directly measured proof**: assembling bundles for `TEN_K__4D__INTERMEDIATE` v1, v2, and v3 today
  all produce the identical effective-workout set `EASY_STANDARD v2, FARTLEK v2, GOAL_PACE_TEN_K v1,
  THRESHOLD_TEMPO v2` — including for **v1**, which was originally published in `1.0.0` back when only
  workout v1s existed (and `1.0.0`'s own pinned bundle for v1 references workout v1s, verified in
  `artifacts/appsel-plan-catalog/1.0.0/bundles/TEN_K__4D__INTERMEDIATE.v1.json`). Rebuilding combination
  v1's bundle *today* therefore does not reproduce what was historically published for v1 — it now silently
  incorporates workout v2 content that did not exist when v1 was authored or first published.
- **Risk if left unfixed**: directly violates the stated target architectural outcome. Any future addition
  of a new workout version (e.g. `EASY_STANDARD v3`) would silently change the resolved bundle for
  **every** existing, already-published combination version (v1, v2, v3 alike) the next time a release is
  built — with no version bump, no audit trail entry, and no cross-release-hash-consistency violation
  raised for the *combination* (since the combination's own serialized content never changes — only its
  *bundle*, a derived artifact, changes). This is the same class of derived-bundle drift already
  documented as "expected/explained" in `cross-release-hash-consistency-audit.md`'s
  `PUBLISHED_TEMPLATE_BUNDLE` exception entries — but that precedent normalizes the drift rather than
  eliminating its root cause.
- **Affected releases**: the capability has existed since `CatalogBundleAssembler`/`FindWorkout` was
  written, but had no observable effect until a second version of any workout existed — i.e. it became
  observable starting with `0.5.0-pilot` (the first release published after `WORKOUT_DEFINITION` v2
  artifacts were created). It will affect **every future release** for as long as the reference model is
  unversioned.
- **Affected active root combination(s)**: all three (`TEN_K__4D__INTERMEDIATE` v1, v2, v3) — proven
  identically affected above.
- **Proposed correction (describe only)**: change `WorkoutProgressionStageDefinition.WorkoutCandidateKeys`
  from `IReadOnlyList<string>` to a list of versioned references (key + version), so a stage pins an exact
  workout artifact rather than "whichever version happens to be newest and non-retired at build time."
- **Expected version cascade if corrected**: `WorkoutProgressionDefinition` (`TEN_K_WORKOUT_PROGRESSION_V1`)
  is already `PUBLISHED`/immutable since `1.0.0` — its schema/content cannot change in place, so this
  requires a new `TEN_K_WORKOUT_PROGRESSION_V1 v2`. `PlanTemplateDefinition` (`TEN_K_MASTER`) references
  `WorkoutProgression` by an already-versioned `VersionedCatalogReference`, so `TEN_K_MASTER` would need a
  new version (`v3`) to point at progression v2. `TemplateCombinationDefinition` would in turn need a new
  version (`v4`, since v1/v2/v3 are all already published) referencing `TEN_K_MASTER` v3. This cascade is
  the same parent chain implicated by Finding 9 below and would likely be delivered together.

---

## Finding 6 — LevelModifierDefinition also references eligible workouts by unversioned key

- **Reproduced**: true
- **File(s)**: `src/PlanCatalog.Core/Models/LevelModifierDefinition.cs`
- **Class/property**: `LevelModifierDefinition.EligibleWorkoutKeys` — line 17:
  `public required IReadOnlySet<string> EligibleWorkoutKeys { get; init; }`
- **Current behavior**: `EligibleWorkoutKeys` is a plain `IReadOnlySet<string>` — no version pinning.
  `CatalogBundleAssembler.Assemble` (line 61) intersects `WorkoutProgressionStageDefinition`'s candidate
  keys with `levelModifier.EligibleWorkoutKeys.Contains` (a key-only membership test) before resolving each
  surviving key through the same version-drift-prone `FindWorkout`. Note `LevelModifierDefinition`'s other
  cross-artifact reference, `ProgressionModifier`, **is** a fully-versioned `VersionedCatalogReference` —
  only `EligibleWorkoutKeys` is key-only.
- **Risk if left unfixed**: same class of risk as Finding 5 — `EligibleWorkoutKeys` participates directly
  in determining which concrete `WorkoutDefinition` artifacts end up in a bundle, so it must be
  version-pinned for the same reason `WorkoutCandidateKeys` must be, or the fix to Finding 5 alone remains
  incomplete (a workout could pass the progression's candidate-key gate as an exact version, but then still
  be silently swapped for a different version if `EligibleWorkoutKeys`-driven filtering doesn't itself pin
  versions — though in the current single-intersection design, fixing `WorkoutCandidateKeys` alone would
  already force an exact version through the pipeline; `EligibleWorkoutKeys` remaining key-only would then
  only matter if it started diverging from what `WorkoutCandidateKeys` pins, e.g. eligibility for a version
  that a candidate list didn't intend).
- **Affected releases**: same as Finding 5 — latent since `LevelModifierDefinition` was authored;
  observable since `0.5.0-pilot`.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` v1, v2, v3 (all use
  `INTERMEDIATE_MODIFIER` v1).
- **Proposed correction (describe only)**: same shape as Finding 5 — change `EligibleWorkoutKeys` to a
  versioned-reference set.
- **Expected version cascade if corrected**: `LevelModifierDefinition` (`INTERMEDIATE_MODIFIER`) is already
  `PUBLISHED`/immutable since `1.0.0` — requires a new `INTERMEDIATE_MODIFIER v2`. `TemplateCombinationDefinition`
  references `LevelModifier` via a `VersionedCatalogReference`, so any combination adopting the fix needs a
  new version referencing `INTERMEDIATE_MODIFIER` v2 (the same new combination version implicated by
  Finding 5, e.g. `v4`).

---

## Finding 7 — TEN_K_MASTER v2 and active combination v3 select inconsistent RulePack versions through separate, uncoordinated fields

- **Reproduced**: true
- **File(s)**: `catalog/templates/ten-k-master.v2.json`; `catalog/combinations/ten-k-4d-intermediate.v3.json`;
  `src/PlanCatalog.Core/Models/PlanTemplateDefinition.cs`;
  `src/PlanCatalog.Core/Models/TemplateCombinationDefinition.cs`;
  `src/PlanCatalog.Core/Validation/PlanTemplateValidator.cs`
- **JSON path**: `TEN_K_MASTER v2`'s `$.requiredRules[0]` = `{"documentType":"RULE_PACK","key":"APPSEL_RACE_PLAN_V1","version":1}`.
  `TEN_K__4D__INTERMEDIATE v3`'s `$.rulePack` = `{"documentType":"RULE_PACK","key":"APPSEL_RACE_PLAN_V1","version":2}`.
- **Current behavior**: `PlanTemplateDefinition.RequiredRules` (`IReadOnlyList<VersionedCatalogReference>`)
  and `TemplateCombinationDefinition.RulePack` (`VersionedCatalogReference`) are two **separate** fields
  that each independently name a `RulePack` version, with no cross-check between them.
  `PlanTemplateValidator.Validate` (lines 79–85) only checks that `template.RequiredRules`'s referenced
  RulePack **exists** (`snapshot.FindRulePack(ruleRef) is null`) — it never compares that version against
  any combination's `RulePack` field. `CatalogBundleAssembler.Assemble` (line 35) resolves the rule pack
  used in a bundle **exclusively** from `combination.RulePack` — `RequiredRules` is never read by bundle
  assembly at all. Result: `TEN_K_MASTER v2` declares (via `RequiredRules`) that it requires `APPSEL_RACE_PLAN_V1`
  **v1**, while the active combination (`v3`) that uses this exact master template actually resolves and
  bundles `APPSEL_RACE_PLAN_V1` **v2** — two different, disagreeing answers to "which RulePack does this
  template require," from two different fields, neither of which is reconciled against the other by any
  validator or by bundle assembly.
- **Risk if left unfixed**: `RequiredRules` is effectively dead/decorative data that can silently drift
  arbitrarily far from what a combination actually uses, without any validation error — a maintainer
  reading `TEN_K_MASTER v2`'s `RequiredRules` to understand "what rules does this template need" would be
  misled about what's actually bundled for the combinations using it.
- **Affected releases**: `TEN_K_MASTER v2` has been published since `0.3.0-pilot` (declaring
  `RequiredRules` = RulePack v1 the whole time); the inconsistency with a combination only became concrete
  once combination v3 (referencing RulePack v2) was published in `0.5.0-pilot`.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` v3 (uses `TEN_K_MASTER` v2 +
  `RulePack` v2 — the mismatched pair).
- **Proposed correction (describe only)**: establish a single owner for "which RulePack a template/combination
  pairing requires" — either (a) have `CatalogGraphValidator`/`PlanTemplateValidator` cross-check that every
  combination referencing a given template resolves a RulePack version present in that template's
  `RequiredRules`, or (b) deprecate `RequiredRules` for rule-pack purposes entirely (since bundle assembly
  already ignores it) and document `TemplateCombinationDefinition.RulePack` as the sole source of truth.
- **Expected version cascade if corrected**: if reconciling by updating `RequiredRules` to reference
  `RulePack` v2, `TEN_K_MASTER v2` is already published/immutable — requires a new `TEN_K_MASTER v3`, which
  in turn requires a new `TemplateCombinationDefinition` version (since v1/v2/v3 are all already published)
  referencing it. If instead `RequiredRules` is deprecated/removed, that is a contract/schema-level change
  with its own migration path, not a simple version bump.

---

## Finding 8 — WorkoutProgressionValidator selects the first runtime registry globally, not the one pinned by the active RulePack

- **Reproduced**: true
- **File(s)**: `src/PlanCatalog.Core/Validation/WorkoutProgressionValidator.cs`
- **Line**: line 16: `var registry = snapshot.RuntimeConditionValueRegistries.FirstOrDefault();`
- **Current behavior**: this single registry is used for **every** `Requires` condition check across every
  phase/stage in the progression being validated (lines 59–85), regardless of which `RulePack` (and hence
  which registry) any combination actually using this progression has pinned. There is currently exactly
  one `RuntimeConditionValueRegistryDefinition` in the whole catalog (`RUNTIME_CONDITION_VALUES_V1 v1`), so
  `FirstOrDefault()` is observationally correct today purely by coincidence of cardinality.
- **Risk if left unfixed**: the moment a second registry is added (e.g. for a different rule pack, or a new
  version), this validator will pick an arbitrary one — whichever the source loader happens to enumerate
  first — with no relationship to which registry the progression's actual consumers require, silently
  producing wrong pass/fail validation results.
- **Affected releases**: latent in every release built so far (`1.0.0` through `0.5.0-pilot`); currently
  invisible because only one registry has ever existed.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` v1, v2, v3 (all resolve
  `TEN_K_WORKOUT_PROGRESSION_V1 v1`, the progression this validator checks).
  ProgressionDefinition itself has no direct link to a RulePack/registry — the two are only related
  indirectly through whichever combinations happen to use a given template/progression pair.
- **Proposed correction (describe only)**: thread the actual governing registry into
  `WorkoutProgressionValidator.Validate` (e.g. accept it as a parameter resolved by the caller from the
  relevant combination's `RulePack`), rather than querying the snapshot globally; or, if a progression can
  legitimately be used by combinations with different rule packs/registries, validate against the union/
  intersection of all registries actually reachable from combinations using this progression, not "the
  first one in the list."
- **Expected version cascade if corrected**: none directly — validator logic change only. No catalog
  artifact version change required.

---

## Finding 9 — LONG_RUN_STANDARD is absent from the active bundle despite an eligible LONG_RUN slot and level-modifier eligibility

- **Reproduced**: true
- **File(s)**: `catalog/workout-progressions/ten-k-workout-progression-v1.json` (workoutCandidateKeys);
  live evidence: `artifacts/appsel-plan-catalog/0.5.0-pilot/bundles/TEN_K__4D__INTERMEDIATE.v3.json`
- **Current behavior**: `RUN_LAYOUT_4D` has a `LongRun` slot (`SlotRole.LongRun`), and
  `INTERMEDIATE_MODIFIER.EligibleWorkoutKeys` includes `LONG_RUN_STANDARD` — both independently confirmed.
  However, `grep -n "workoutCandidateKeys" catalog/workout-progressions/*.json` shows **`LONG_RUN_STANDARD`
  never appears as a candidate in any phase/stage** of `TEN_K_WORKOUT_PROGRESSION_V1` (only
  `EASY_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K` are ever listed). `CatalogBundleAssembler`
  computes effective workouts as `progression candidate keys ∩ levelModifier eligible keys` — since
  `LONG_RUN_STANDARD` is never a candidate, the intersection excludes it regardless of eligibility. Directly
  confirmed live: `TEN_K__4D__INTERMEDIATE.v3.json`'s bundled `workouts` array contains
  `EASY_STANDARD, FARTLEK, GOAL_PACE_TEN_K, THRESHOLD_TEMPO` — no `LONG_RUN_STANDARD`. This is true for
  v1 and v2's bundles as well (confirmed by rebuilding all three today — identical effective-workout set).
- **Risk if left unfixed**: this is a genuine **domain-content gap**, not a code defect: the workout
  progression never offers a long-run-family workout despite the layout explicitly reserving a slot for
  one and the level modifier explicitly declaring the athlete eligible for it. Any plan generated from this
  combination has no mechanism to ever schedule a long run, which is very likely unintended given the
  layout's explicit `LongRun` slot.
  reservation.
- **Affected releases**: `1.0.0` through `0.5.0-pilot` — `TEN_K_WORKOUT_PROGRESSION_V1`'s content hash has
  been stable and unchanged across every release (confirmed:
  `a4856b47bf385ad29c148412480620b2584ddf7b0e0fa177664dc3455baf6281` in all five prior releases), so this
  gap has existed since the very first release.
- **Affected active root combination(s)**: `TEN_K__4D__INTERMEDIATE` v1, v2, v3 — all three, identically.
- **Proposed correction (describe only)**: add `LONG_RUN_STANDARD` as a `workoutCandidateKey` to an
  appropriate stage (most naturally a `FOUNDATION`/`BUILD` long-run-building stage) in
  `TEN_K_WORKOUT_PROGRESSION_V1`. This is a domain-content decision requiring product/coaching judgment on
  which phase(s)/stage(s) and exposure counts, not something this read-only assessment should decide.
- **Expected version cascade if corrected**: `TEN_K_WORKOUT_PROGRESSION_V1` is already published/immutable
  since `1.0.0` — requires a new `v2`. Cascades identically to Finding 5's chain: `TEN_K_MASTER` new version
  (`v3`) → new `TemplateCombinationDefinition` version (`v4`). If Finding 5/6/9 are corrected together
  (likely, since they share the same parent chain), a single new progression version, master version, and
  combination version could resolve all three.

---

## Finding 10 — docs/README.md is incomplete relative to the governance rules current audits assume

- **Reproduced**: true
- **File(s)**: `docs/README.md` (46 lines total, read in full)
- **Current behavior**: `docs/README.md` defines exactly two things: (1) a 6-level canonical-source
  precedence hierarchy for resolving domain-content conflicts, and (2) that `docs/canonical/` is the
  approved canonical directory. It defines **nothing else**. Yet the following governance concepts are
  used pervasively and load-bearingly across existing audits, without being defined anywhere in
  `docs/README.md`:
  - Artifact immutability rules (never mutate a published `(documentType, key, version)` tuple; always
    create a new version) — central to `combination-immutability-investigation.md`,
    `published-workout-immutability-remediation.md`, `peak-volume-policy-immutability-remediation.md`,
    `dependency-version-cascade-audit.md`.
  - Retirement-ledger semantics (what "retired" means; how full-catalog packaging must treat retired
    combinations/dependencies) — central to `full-catalog-retirement-packaging-audit.md`.
  - Release-channel policy (`Pilot`/`Draft`/`Production`; when `PLACEHOLDER_UNCONFIRMED` blocks a publish)
    — enforced by `PublishReadinessValidator` and referenced throughout every domain audit.
  - Cross-release hash-consistency invariant and the exception-ledger mechanism — central to
    `cross-release-hash-consistency-audit.md`.
  - The four-tier `ContentDecisionStatus` classification scheme itself
    (`CANONICAL_CONFIRMED`/`EXPLICIT_PRODUCT_DEFAULT`/`PLACEHOLDER_UNCONFIRMED`/`TECHNICAL_ONLY`) — used in
    literally every domain-content audit entry, but its definitions live only in ad hoc task instructions
    across prior sessions, not in any committed governance document.
  - What "active root combination" means when multiple non-retired versions of the same key coexist —
    entirely undefined; this very assessment had to treat it as ambiguous (see Task 3).
- **Risk if left unfixed**: every one of these concepts is currently governed only by convention
  established in individual audit reports and prior task instructions, not by a durable, discoverable
  governance document. A new contributor (human or agent) reading only `docs/README.md` would have no way
  to learn these rules exist.
- **Affected releases**: none directly (documentation-completeness issue, not a release-content defect).
- **Affected active root combination(s)**: none specifically — a repository-governance gap.
- **Proposed correction (describe only)**: extend `docs/README.md` with dedicated sections for artifact
  immutability, retirement-ledger governance, release-channel policy, the cross-release hash-consistency
  invariant, and a formal definition of the `ContentDecisionStatus` four-tier scheme — each citing the audit
  report(s) where the rule was first established, so the rule and its precedent stay linked.
- **Expected version cascade if corrected**: none — documentation-only addition.

---

## Confirmation

No code file, schema file, or catalog JSON artifact was modified to produce this report. No release was
published. No artifact was retired. No test was changed. `runner/backend/` was not touched.
