
# Appsel Canonical Source Governance

This document defines which repository sources may be used as canonical
evidence during Appsel Plan Catalog domain-content authoring.

Existing implementation values, passing tests, artifact filenames, and
historical documents are not canonical evidence by themselves.

---

## 1. Canonical source hierarchy

When approved sources conflict, use the following precedence:

1. Approved Golden Fixture v3
2. Approved versioned rule files referenced by Golden Fixture v3
3. Approved Plan Generation Decisions documents
4. Explicitly approved reconciled architecture specifications
5. Plan Catalog canonical brief
6. Existing catalog artifacts and tests

Lower-priority sources must not override higher-priority sources.

Every conflict resolution must record:

- both source paths
- both values
- selected value
- precedence reason
- affected artifacts
- affected tests

If precedence does not resolve the conflict, the decision remains:

`PLACEHOLDER_UNCONFIRMED`

---

## 2. Approved canonical directories

The following directory is canonical:

```text
docs/canonical/
```

### 2a. `docs/archive/` is non-canonical

`docs/archive/` holds superseded historical material (earlier architecture drafts, the Golden Fixture v2
generation) kept for provenance only. Nothing under `docs/archive/` may be cited as canonical evidence for
a domain-content decision, regardless of how closely it resembles current content. A value that only
exists in `docs/archive/` remains `PLACEHOLDER_UNCONFIRMED` until an approved canonical source confirms it.

### 2b. `docs/pending/` is non-canonical

`docs/pending/` holds candidate evidence awaiting approval. Nothing under `docs/pending/` may be cited as
canonical evidence until it is promoted into `docs/canonical/` through an explicit, recorded approval —
copying a file's *content* into `docs/canonical/` does not implicitly approve the file that remains in
`docs/pending/`; each directory's status is independent. A `docs/pending/` file may only be **deleted** if
it is proven byte-identical (SHA-256) to its already-canonical equivalent (see §25 below); unique pending
evidence must never be deleted merely because it has not yet been approved.

## 3. Golden Fixture v3 scope

`docs/canonical/golden-fixture-v3/` is the single highest-precedence canonical source (§1). It documents
exactly one concrete, real Process-B-generated plan (`TEN_K` / `INTERMEDIATE` / 4-day / 12-week) and its
`DecisionTrace`. It is evidence for:

- field-level facts about the specific `WorkoutDefinition` keys, phase allocations, and layout shape it
  actually exercises,
- the exact `PrescriptionMode`/`DistanceAccountingMode` values used by workouts it references.

It is **not** evidence for:

- experience levels, distances, layouts, or run-frequency combinations it does not exercise,
- generated-output-only, per-instance values (e.g. `resolvedPeakKm`, `DecisionTrace` resolver-internal
  field names) being silently promoted into reusable Process A catalog vocabulary — an explicit ownership
  decision is required first (see `ten-k-pilot-vocabulary-decisions.md` for the working example of this
  rule being applied),
- a general policy merely because one generated plan happens to realize a value consistent with it (one
  data point is not a policy — see `ten-k-pilot-domain-decision-audit.md` AUD-044's
  `MaximumHardSessionsPerWeek` reasoning for the working example).

## 4. Deterministic canonical serialization policy

Every catalog document is serialized through `PlanCatalog.Infrastructure.Serialization.SystemTextJsonCanonicalSerializer`
using `CanonicalJsonOptions.Canonical`: camelCase properties, `UpperSnakeCaseNamingPolicy` for enums,
ordinal key sorting, `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`, non-indented output, NFC Unicode
normalization. This is the **only** serialization path used for content hashing and for published-release
file output. Any tool used to independently reproduce a hash (e.g. a throwaway verification script) must
reproduce this exact canonicalization or its result is not authoritative — see
`canonical-source-preflight.md` for a documented case where a naive Node.js reproduction attempt produced a
false-positive mismatch for exactly this reason.

## 5. Content-hash calculation policy

Content hash = SHA-256 of the canonical JSON serialization (§4) of a document, **excluding** that
document's own hash-bearing field (`contentHash` for catalog artifacts, `bundleContentHash` for
`PublishedTemplateBundle`, `manifestContentHash` for `CatalogReleaseManifest`) — implemented in
`PlanCatalog.Infrastructure.Hashing.CatalogDocumentHasher`. The hash must be a pure function of semantic
content: no timestamps, no release version/channel, no output path, no mutable lifecycle state may ever
enter the hashed representation. A hash that changes between two computations of the *same* declared
`(documentType, key, version)` is by definition an immutability violation (§9), never an expected
"freshness" signal — a prior report's ambiguous phrase "fresh, distinct per-release" was corrected for
exactly this reason (see `combination-v2-hash-and-closure-audit.md`).

## 6. Artifact version-parity policy

Golden Fixture v3 references `TEN_K_MASTER v2` / `APPSEL_RACE_PLAN_V1 v3` — versions that may not match
what the current catalog carries at any given time (e.g. the catalog was at `TEN_K_MASTER v1` when this was
first discovered). A field-level fact from the fixture is still usable evidence for the *current* artifact
version if the fact is version-independent (`SOURCE_SEMANTICS_USABLE`) — but the artifact must **never** be
silently bumped, cloned, or renamed merely to force its version number to match the fixture's citation. Any
such gap must be recorded explicitly (`ARTIFACT_VERSION_PARITY_UNRESOLVED` or, once resolved incidentally
by an unrelated required change, documented as a side effect — see `ten-k-pilot-domain-decision-audit.md`
AUD-053 for the worked example).

## 7. Immutable published artifact rule

Once a catalog artifact's `(documentType, key, version)` triple has been stamped `PUBLISHED` and appears in
any release manifest, its content must never change again. A content change always requires a **new**
`version` number on a **new** file — never an edit to the existing file. This rule was violated and then
corrected twice in this project's history (`combination-immutability-investigation.md`,
`published-workout-immutability-remediation.md`, `peak-volume-policy-immutability-remediation.md`); both
incidents are preserved as historical exceptions (§25) rather than rewritten.

## 8. Same documentType + key + version must always have the same content hash

This is the direct, machine-checked consequence of §7. It is enforced repository-wide by
`CrossReleaseHashConsistencyTest` (`tests/PlanCatalog.Tests/Publishing/CrossReleaseHashConsistencyTests.cs`),
which scans every release manifest under `artifacts/appsel-plan-catalog/` and fails on any
`(documentType, key, version)` group with more than one distinct hash **unless** that exact
identity+release+hash triple is registered in `artifacts/appsel-plan-catalog/cross-release-hash-exceptions.json`
— a narrow, explicit, non-wildcard exception ledger (see `cross-release-hash-consistency-audit.md`). It is
also enforced at publish time: `CatalogPublisher` rejects a new publish if any artifact it would publish
shares an identity with a previously-published artifact under a different hash
(`PUBLISH_HASH_MISMATCH_FOR_SAME_KEY_VERSION`), unless the exact mismatch is a registered exception.

## 9. Exact dependency versioning

Any field that determines which specific version of another catalog artifact ends up in a
`PublishedTemplateBundle` must be an exact `{ documentType, key, version }` reference
(`VersionedCatalogReference`) — never a bare key, and never resolved by "latest," "highest," or
"first-matching" selection. See `cross-artifact-reference-inventory.md` for the complete inventory of which
fields are genuine cross-artifact dependencies (`ARTIFACT_DEPENDENCY_REQUIRES_VERSION`) versus which are
local identifiers, literal value sets, or already-exact references.

## 10. Legacy key-only references are historical-read-only

`WorkoutProgressionStageDefinition.WorkoutCandidateKeys` (schemaVersion 1) and
`LevelModifierDefinition.EligibleWorkoutKeys` (schemaVersion 1) are unversioned-key fields that predate §9.
They remain valid and readable on already-published schemaVersion 1 documents — historical releases built
from them must continue to verify exactly as originally published — but no new document may use this shape
(schemaVersion 2+ requires the exact-versioned successor field: `WorkoutCandidates` /
`EligibleWorkouts`). Both shapes may never coexist on the same document; exclusivity is enforced by
`SchemaVersionShapeValidator` (`src/PlanCatalog.Core/Validation/SchemaVersionShapeValidator.cs`).

## 11. Latest/highest-version resolution is forbidden in new publish graphs

`CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger?)` (highest-non-retired-version
auto-selection) exists **only** to support reading/verifying historical (schemaVersion 1) material. It must
never be used to assemble a new (schemaVersion 2+) candidate graph — the exact overload
`FindWorkout(string key, int version)` (or `GetRequiredWorkout`) must be used instead. This rule exists
because auto-selection silently changes a bundle's content whenever a newer artifact version is added to
source, even for an already-published combination that never asked for the change — see
`deterministic-graph-prechange-assessment.md` Finding 5 for the discovery and
`exact-workout-reference-migration.md` for the fix.

## 12. TemplateCombinationDefinition owns exact RulePack selection

`TemplateCombinationDefinition.RulePack` is the **sole** exact RulePack-version selector consulted by
bundle assembly. No other field may independently select a competing exact RulePack version for the same
combination — see §13 and `rule-pack-ownership-audit.md` for the incident this rule was written to prevent
recurring (`TEN_K_MASTER v2`'s legacy `RequiredRules` once silently disagreed with its combinations'
`RulePack` selection).

## 13. PlanTemplate RequiredRuleKeys are semantic requirements only

`PlanTemplateDefinition.RequiredRuleKeys` (schemaVersion 2+) declares "the combination-selected RulePack's
*key* must be one of these" — it never selects a version, and a RulePack version bump under an already-
accepted key never requires a template change. The legacy `RequiredRules` (schemaVersion 1, exact-version
list) is superseded by this field going forward but remains valid on already-published schemaVersion 1
templates; the two shapes are mutually exclusive per document (§10-style enforcement, same validator).

## 14. RulePack owns exact RuntimeConditionValueRegistry selection

Runtime-condition validation for a specific combination must resolve its registry via
`combination.RulePack` (exact) → `rulePack.RuntimeConditionValueRegistry` (exact) — never via
`RuntimeConditionValueRegistries.FirstOrDefault()` or any other catalog-wide default. See
`runtime-registry-resolution-audit.md`.

## 15. PublishedTemplateBundle is self-contained

A `PublishedTemplateBundle`, once assembled, must never require its consumer to re-query the mutable
authoring catalog: every dependency — including every workout in its effective workout set — is pinned by
exact `documentType` + `key` + `version` + `contentHash`. For a candidate (schemaVersion 2+) graph, the
effective workout set is the **union** of the progression's exact candidate workouts and the level
modifier's exact eligible workouts (not an intersection) — this is what allows a workout to enter the
bundle through eligibility alone, without also being a progression candidate (see
`bundle-workout-closure-audit.md`, and the `LONG_RUN_STANDARD` worked example it documents).

## 16. Retired roots are excluded from new releases

A `TemplateCombinationDefinition` recorded in the retirement ledger (`artifacts/appsel-plan-catalog/retirements.json`)
must never appear as a bundle, or as a `TEMPLATE_COMBINATION` artifact entry, in any **newly built**
release — enforced by `CatalogPublisher.BuildRelease`'s eligible-combination filter. An explicit request to
build a retired combination fails with `RETIRED_COMBINATION_NOT_ELIGIBLE_FOR_NEW_RELEASE`. See
`full-catalog-retirement-packaging-audit.md`.

## 17. Retired artifacts remain readable for audit/history

Retirement is recorded exclusively in the ledger — the retired artifact's own JSON source file is never
deleted, moved, or rewritten. `validate-combination` (structural check) continues to pass for a retired
combination; only its *new-release publish eligibility* is revoked. See §18-19.

## 18. Historical releases verify under original-era schema/validation rules

A change to current validation rules, schema shapes, or the reference model must never cause an
already-published historical release to stop verifying. `verify-release` re-hashes exactly the files
already present in the release directory against `checksums.sha256` — it does not re-run current-era
source-integrity or publish-graph validation against historical source. Confirmed after every structural
change in this project's history by re-running `verify-release` for every existing release.

## 19. Full-catalog packaging semantics

A release's manifest lists **all** published artifacts and bundles — every stamped catalog document of
every type, plus one bundle per eligible (§16) combination — not a single hand-picked "selected" bundle.
This is deliberate (brief §15), not a bug; multiple sibling bundles for the same combination *key* can
coexist across combination *versions* until superseded versions are retired (§16, §21).

## 20. One publish-eligible combination version per CombinationKey

Beyond the mechanical retirement filter (§16), a release is expected to have **exactly one** non-retired,
publish-eligible version per `CombinationKey` — checked by `ActiveVersionUniquenessValidator`
(`ACTIVE_COMBINATION_VERSION_NOT_UNIQUE`). Multiple simultaneously-eligible versions of the same key are a
transitional state, not a target state — they must be resolved by retiring all but the intended active
version once the replacement is confirmed correct (see `active-version-uniqueness-audit.md` and
`part3-retirement-and-release-plan.md` for the retirement plan that resolved this for
`TEN_K__4D__INTERMEDIATE`).

## 21. Decision-level versus artifact-level blocker metrics

A domain-content "blocker" can be counted two different, non-comparable ways:

- **Decision-level**: the number of individual `PLACEHOLDER_UNCONFIRMED` field-level decisions
  (`PilotDomainContentAudit.Entries`) in scope. One artifact with 5 unconfirmed fields contributes 5.
- **Artifact-level**: the number of distinct `(documentType, key, version)` identities carrying at least
  one blocking decision, in scope. The same artifact contributes 1.

Never state one number when the other, or a different *scope* of either, is meant. See `placeholder-scope-audit.md`
for the canonical measurement implementation (`PlanCatalog.Core.Audit.BlockerScopeMeasurement`) and the
exact four scopes each metric may be reported at (§22).

## 22. Targeted versus full-catalog Production readiness scope

Both counted at either level (§21), across four distinct **scopes**:

- `totalCatalogPlaceholderDecisionCount` / `totalCatalogBlockingArtifactCount` — every blocking decision in
  the entire audit history, regardless of whether it is reachable from anything currently publishable.
  **Must never be used as a publish-readiness gate.**
- `eligibleReleaseUnionBlockingDecisionCount` / `...ArtifactCount` — union across every combination
  currently non-retired/publish-eligible. Use this to gate a **full-catalog** Production publish.
  Retirement (§16) directly shrinks this scope.
- `activeRootClosureBlockingDecisionCount` / `...ArtifactCount` — one specific combination's own exact
  dependency closure. Use this to gate a **targeted** (single-combination) Production publish.
- `historicalOnlyPlaceholderDecisionCount` / `...ArtifactCount` — the complement of the eligible-release
  union: decisions reachable only from retired/superseded material, never from anything currently
  publishable.

## 22a. Production-readiness error contract

`PublishReadinessValidator`'s content-decision guard reports one Production-readiness error **per
blocking artifact identity** (`DocumentType`+`Key`+`Version`), never one error per field-level decision.
An artifact with 5 blocking fields still produces exactly 1 top-level error — that error's
`BlockingDecisions` list carries all 5, structured (`EntryId`, `FieldPath`, `Classification`, `Reason`),
never only inside a concatenated message string. Concretely, for the active root
(`TEN_K__4D__INTERMEDIATE v4`): **9 top-level errors**, `BlockingArtifactCount = 9`,
`BlockingDecisionCount = 13` — the error count matches the artifact-level metric (§21), never the
decision-level metric, by construction.

`PublishReadinessValidator.ValidateContentDecisionsDetailed(...)` is the canonical, structured entry
point (returns `ContentDecisionGuardResult { BlockingArtifactCount, BlockingDecisionCount, Errors }`).
`ValidateContentDecisions(...)` (returning the generic `ValidationResult`) is a backward-compatible
projection of the same data — same error count, same codes, just without the nested structure. Both the
CLI (text and `--json` modes) and `CatalogValidationException.ContentDecisionDetail` read from the same
`ContentDecisionGuardResult` object; neither re-derives structured data by parsing a message string. See
`artifacts/audits/production-readiness-error-contract-audit.md` for the full trace and rationale.

## 23. Release-immutability and supersede semantics

A published release directory under `artifacts/appsel-plan-catalog/{version}/` is written atomically
(`IPublishedArtifactRepository.WriteRelease`, staged then moved — a failed publish leaves no partial
output) and is never modified after. "Retiring" or "superseding" a release never touches its directory:

- **Combination retirement** (§16) is a ledger entry (`retirements.json`) affecting only *future* publishes.
- **Release supersession** is a separate ledger entry (`release-status.json`, via the `supersede-release`
  CLI command) recording that a *later* release replaces an *earlier* one as the recommended/active
  release — it never deletes, hides, or alters the superseded release, which must continue to verify.
  A release may only be superseded after its successor has itself published and verified successfully.

## 24. Retirement-ledger staging and rollback expectations

Before writing a real retirement-ledger change:

1. capture the ledger's pre-change content and SHA-256 checksum (or record its absence, if the ledger does
   not yet exist),
2. build an in-memory/temporary overlay containing the proposed entries and run full validation
   (source-integrity, publish-graph, `ActiveVersionUniquenessValidator`, and a `BuildPreview` — never
   `Publish` — full-catalog dry run) against it,
3. only once the overlay proves the expected outcome (which combinations become ineligible, which bundle
   list results, that the target combination's bundle hash is unchanged) may the real ledger be written,
4. immediately re-run the same validation suite against the real ledger and confirm it matches the overlay
   result exactly.

If real-ledger validation ever diverges from the overlay result, restore the exact pre-change ledger
content (using the recorded checksum to confirm the restoration) and stop — do not proceed to publish. See
`retirement-ledger-application.md` for a worked example of this full staging/apply/reconcile sequence.

## 25. Non-canonical pending-file deletion

A file under `docs/pending/` may be deleted **only** when all of the following are recorded together:

- its exact source path,
- its exact canonical equivalent's path,
- the SHA-256 of both,
- confirmation the two hashes are equal.

If the hashes differ even slightly, the pending file is unique evidence and must never be deleted — it may
only be left in place (still non-canonical, §2b) pending an explicit future approval decision. Deleting a
pending file never promotes it to canonical status; only copying/approving its *content* into
`docs/canonical/` does that, and that approval is a separate, explicitly recorded decision from the
deletion itself.
