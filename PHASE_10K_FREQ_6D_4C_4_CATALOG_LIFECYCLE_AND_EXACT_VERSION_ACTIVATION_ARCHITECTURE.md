# PHASE 10K-FREQ.6D.4C.4 — Catalog Artifact Lifecycle, Exact-Version Activation & Legacy Resolver Containment

**Architecture-design phase. No production code, no WorkoutDefinition status change, no DRAFT→VALIDATED promotion, no resolver modification, no profile change, no product decision, no dosage change, no progression/lane wiring, no RunningApp change, no public activation. Design only — a later implementation phase executes it.**

## 1. Preflight

`PHASE_LEDGER.md` rows 68-69: `FREQ.6D.4B.4` (`FREQ6D4B4_IMPLEMENTED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`) and `FREQ.6D.4C.3` (`FREQ6D4C3_PROFILES_AUTHORED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`) both `DONE`/`VERIFIED`. Commits `e7a6c07`, `a8de6a8`, `7f6c369` all confirmed reachable from HEAD via `git merge-base --is-ancestor`. Starting HEAD `7f6c3691467ffb5b0ddb7ccc6bb2bffe4244bc8c`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 5` (5 ahead, 0 behind). `git status --short` → ` m baseline_tmp` only. `git diff --check` → clean. `FREQ.6D.4C.4` confirmed not already in `PHASE_LEDGER.md`.

Real FREQ.6D.4C.3 report re-read and confirmed against the repository (not report prose alone): 8 real profiles exist (`snapshot.PrescriptionProfiles.Count == 8`); all validate/project losslessly (re-ran `Intermediate5DProductionPrescriptionProfileSourceTests`, 64/64 pass); `LegacyCatalog_ExecutionPrescriptionsRemainNull_DespiteRealProfilesNowExisting` confirms the real `TEN_K__4D__INTERMEDIATE` bundle still yields `ExecutionPrescriptions == null`; `AEROBIC_STRENGTH_CONTROLLED_INTRO v3`, `THRESHOLD_TEMPO v5`, `FARTLEK v5`, `GOAL_PACE_TEN_K v3` all remain `DRAFT` (confirmed by direct file read); none promoted; lifecycle blocker is the sole disclosed pre-6D.4D blocker per both reports.

## 2. Parent state

FREQ.6D.4C.3 authored real content against a catalog where the M3/M4 capability architecture (FREQ.6D.4C.1/4C.2) and the R1 recovery-ownership correction (FREQ.6D.4B.2/4B.4) are both closed and implemented. The only remaining item blocking `FREQ.6D.4D` is: can the four `DRAFT` `WorkoutDefinition` versions become usable for real publication without perturbing existing `Intermediate×3D/4D`/`Beginner×4D` behavior. This phase answers that architecturally; it does not implement or promote anything.

## 3. Status semantics

`CatalogStatus` (`src/PlanCatalog.Core/Enums/CatalogStatus.cs`) is a deliberately closed four-value enum — its own doc comment states *"Deliberately closed to four values."* Real, evidence-derived semantics per value:

| Status | Authoring meaning | Graph validation | Publisher eligibility | Exact-ref resolution | Legacy resolver (`FindWorkout(key, ledger)`) | Bundle inclusion | Historical replay |
|---|---|---|---|---|---|---|---|
| `DRAFT` | Content authored but not yet confirmed durable | Fully validated (schema/graph/skeleton/capability checks all run) | Excluded — `CatalogPublisher.ExcludeDraftArtifacts` strips it from every `Workouts`/`Combinations`/etc. list before `CatalogStamper.StampAsPublished` and bundle assembly | Works — `FindWorkout(key, version)` (exact overload) has no status filter at all | Excluded — `FindWorkout(key, ledger)` filters `Status != Draft` explicitly | Never (removed pre-stamp) | N/A — never publishable, so never replayable |
| `VALIDATED` | Content confirmed durable/correct | N/A (post-authoring) | Included | Works | **Included** — the sole differentiator vs. `Draft` in the filter | Included if referenced | Included |
| `Published` | Stamped output of a real release build (`CatalogStamper`) | N/A | N/A (this is the publisher's own output state, not an authoring input) | Works | Included (same as `Validated`, filter is `!= Draft`) | Included | Included |
| `Retired` | Superseded; kept for audit/historical verification only | Graph validation does not consult retirement (deliberate — a separate, publish-graph concern per `CatalogGraphValidator`'s own doc) | Excluded from **new** releases (`CatalogPublisher.BuildRelease` filters `eligibleCombinations` by `!retirementLedger.IsRetired(...)`); retired dependencies abort assembly (`CatalogBundleAssembler` throws if any dependency is retired) | Works (exact lookup has no retirement awareness by itself — retirement is a separate, explicitly-passed ledger check) | Excluded — `FindWorkout(key, ledger)` also filters `!retirement.IsRetired(...)` | Excluded from new releases; retained for historical verification | Explicitly supported — retirement exists precisely to keep old artifacts auditable without being newly selectable |

## 4. Version-resolver inventory

Every real place a `WorkoutDefinition` version is chosen, found via full-repository search of `FindWorkout(`:

| Call site | Classification | Selection rule | Status filter | Real consumers today | 3D/4D reliance | Should future profile-backed 5D rely on it? |
|---|---|---|---|---|---|---|
| `CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger?)` | `HIGHEST_NON_RETIRED` | Highest `Version` among non-`Draft`, non-retired matches for a bare key | `Status != Draft` | (a) `CatalogBundleAssembler`'s legacy branch (only when both `WorkoutProgression` and `LevelModifier` use the bare-key shape); (b) `WorkoutArtifactImmutabilityTests`/`DependencyVersionCascadeTests` as a direct golden-invariant check; (c) existence-only checks in `LevelModifierValidator`/`WorkoutProgressionValidator`/`TemplateCombinationValidator` (`is not null`, version-agnostic) | **Yes, but only via 3 already-superseded historical combination versions** (see §16) — the real, currently-active combination does **not** use this path (see §16) | **No** — every schemaVersion≥2 (exact) progression/level-modifier already exists and is the established migration target |
| `CatalogSourceSnapshot.FindWorkout(string key, int version)` | `EXACT_VERSION_REFERENCE` | Exact `(key, version)` match, no filtering | None | `CatalogBundleAssembler`'s exact-closure branch; `CatalogGraphValidator` (profile/overlay target resolution); `CandidatePublishGraphValidator`; `WorkoutPrescriptionProfileValidator` (via caller-supplied lookup); `LevelModifierValidator`/`WorkoutProgressionValidator`/`TemplateCombinationValidator` exact-shape checks | **Yes — this is what the real, currently-active `TEN_K__4D__INTERMEDIATE v4` combination actually uses** (see §16) | **Yes — already the established target architecture** |
| `CatalogSourceSnapshot.GetRequiredWorkout(key, version)` | `EXACT_VERSION_REFERENCE` | Thin exact-lookup wrapper that throws if absent | None | Test helpers only | No | Yes (same as above) |

No `LATEST`, `HIGHEST_VALIDATED` (distinct from `HIGHEST_NON_RETIRED`), `MANIFEST_PINNED`-as-a-named-mechanism, or `OTHER` resolver exists — there are exactly two real resolution shapes in the whole repository: exact and legacy-bare-key.

## 5. Regression trace

Reconstructed against real code and real catalog documents, not re-derived from memory:

```
A WorkoutDefinition (e.g. FARTLEK v5) changes DRAFT → VALIDATED
        ↓
CatalogSourceSnapshot.FindWorkout(key, ledger)'s `Status != Draft` filter now
includes it. Its higher Version wins the `.OrderByDescending(Version).FirstOrDefault()`
selection for the bare key "FARTLEK".
        ↓
Two real callers are affected:
  (a) WorkoutArtifactImmutabilityTests.CurrentActiveResolution_..., a golden-invariant
      test that calls `snapshot.FindWorkout("FARTLEK")` directly and asserts
      `Assert.Equal(4, resolved.Metadata.Version)` — flips to 5, test fails.
  (b) CatalogBundleAssembler.AssembleInternal's LEGACY branch (`progressionIsExact ==
      levelModifierIsExact == false`), reached only when a combination's exact
      WorkoutProgression+LevelModifier pair both still use bare-key shape
      (`WorkoutCandidateKeys`/`EligibleWorkoutKeys`). Verified by direct inspection: this
      shape exists ONLY in WorkoutProgression v1 + LevelModifier v1, referenced by
      TEN_K__4D__INTERMEDIATE combinations v1–v3 (all superseded, retained for
      historical-replay verification only — see §16). Their bare "FARTLEK"/"THRESHOLD_TEMPO"
      candidate keys resolve through `FindWorkout(key, retirement)` at bundle-assembly
      TIME, not at combination-authoring time — there is no snapshot/freeze of "what
      resolved when this combination was live." Re-assembling combination v3 today
      silently starts returning FARTLEK v5's/THRESHOLD_TEMPO v5's content instead of v4's.
        ↓
DependencyVersionCascadeTests.ActiveCombinationV3_ResolvesAFullyConsistentVersionedGraph
asserts `Assert.All(bundle.Workouts.Where(w => w.Key != "GOAL_PACE_TEN_K"), w =>
Assert.Equal(4, w.Version))` against a live re-assembly of combination v3 — fails for
the same reason.
        ↓
Both failures are true content drift, not test artifacts: the actual JSON documents for
combination v1-v3/progression v1/level-modifier v1 remain byte-identical (confirmed by
their own pinned content-hash assertions), but their *computed* bundle output changes,
because "highest non-Draft non-retired for this bare key" is evaluated fresh, unbounded
by time, on every assembly.
```

`LEGACY_VERSION_PROMOTION_REGRESSION_TRACE` — precise production call chain: `WorkoutDefinition.Metadata.Status` (Draft→Validated) → `CatalogSourceSnapshot.FindWorkout(string, IRetirementLedger?)`'s `OrderByDescending(Version)` → `CatalogBundleAssembler.AssembleInternal`'s legacy (non-exact) branch, reached exclusively via `TEN_K__4D__INTERMEDIATE` combinations v1–v3's frozen bare-key `WorkoutProgression v1`/`LevelModifier v1` pair → `bundle.Workouts` content silently changes on re-assembly. The real, currently-active combination (v4) is provably unaffected (§16).

## 6. Exact-reference target architecture

Direct evidence the compositional target architecture already prefers exact references, and that it is already the production reality for the live cell: `TEN_K__4D__INTERMEDIATE v4` (the highest `VALIDATED` — i.e., real, current — combination) resolves through `TEN_K_MASTER v3 → WorkoutProgression v2` (uses `workoutCandidates`, exact refs) and `INTERMEDIATE_MODIFIER v2` (uses `eligibleWorkouts`, exact refs). Both are exact-shaped, so `CatalogBundleAssembler` takes the `progressionIsExact` branch: `WorkoutClosureResolver.ComputeExactClosureRefs` → `snapshot.FindWorkout(key, version)` (exact overload only). **The live production path never calls the legacy bare-key resolver at all.**

Direct answer to §6's question: 5D does **not** need these four `WorkoutDefinition`s to become `VALIDATED` merely to be *usable* — content authoring, graph validation, capability-overlay resolution and profile projection (FREQ.6D.4C.2/4C.3) already work correctly against `DRAFT` sources. VALIDATED is needed **only** for the narrower, later concern of real *publication*: `CatalogPublisher.BuildRelease` calls `ExcludeDraftArtifacts` before `CatalogStamper.StampAsPublished` and bundle assembly — a future exact-closure 5D combination referencing e.g. `AEROBIC_STRENGTH_CONTROLLED_INTRO v3` would find it filtered out of `stamped.Workouts` and fail with `WORKOUT_REFERENCE_VERSION_NOT_FOUND` at publish time. So: **not needed today (authoring/validation); needed before real publication (channel-gated release build), never needed for legacy-resolver "become the new default."**

## 7. Status-vs-activation analysis

The repository currently conflates two of the three questions §14 requires separated onto a single `CatalogStatus` flag:

1. **Artifact validity** ("is this content valid and durable") — correctly, singularly owned by `CatalogStatus`.
2. **Combination/publication activation** ("is this exact artifact used by this combination's published bundle") — correctly, singularly owned by exact `(key, version)` references inside `TemplateCombinationDefinition`'s dependency closure (via `WorkoutProgression.WorkoutCandidates`/`LevelModifier.EligibleWorkouts`) — `CatalogStatus` only gates this indirectly through `ExcludeDraftArtifacts`, which is a legitimate, narrow, single-purpose consequence of (1), not a second authority.
3. **Implicit fallback resolution** ("if a consumer asks for a bare key with no version, what do they get") — **incorrectly conflated onto the same `CatalogStatus` flag** via `FindWorkout(key, ledger)`'s `Status != Draft` filter. Promoting an artifact for reason (1) (it is now valid/durable and should be exactly publishable) has the unintended side effect of also answering (3) (it silently becomes the new global default for anyone still using bare-key resolution), even though these are semantically unrelated for artifacts whose entire purpose is a narrow, additive eligibility extension rather than a full replacement.

Classification: **`STATUS_ACTIVATION_CONFLATION_CONFIRMED`** — an explicit architecture debt, not invented by this phase but exposed and root-caused by it. The conflation is real, narrow (only bites when a same-keyed higher version is promoted while older legacy-shaped bare-key consumers still exist), and does **not** require touching the closed four-value `CatalogStatus` enum to fix (see §14).

## 8. L1 — keep DRAFT for explicit profile-backed use

Authoring/validation/projection already prove this works today (FREQ.6D.4C.2/4C.3). But `DRAFT` semantically means "not yet confirmed durable" (its own consumers treat it that way: `ExcludeDraftArtifacts`, `WorkoutDefinitionValidator`'s DRAFT-specific skeleton rules). Real publication is permanently blocked while `DRAFT` (§6). **Not sufficient as a terminal architecture** — it is the correct *current* interim state (already in effect, zero risk), but cannot be the permanent answer once 5D needs to actually publish. Classification: `NECESSARY_INTERIM_INSUFFICIENT_TERMINAL`.

## 9. L2 — VALIDATED + legacy resolver pinning (migrate supported cells to exact pins)

Evaluated directly against real data: **this migration already happened** for the real, currently-active `TEN_K__4D__INTERMEDIATE v4` combination (§6) — `WorkoutProgression`/`LevelModifier` versions ≥2 are already exact-shaped; this is the established, already-completed migration pattern (`artifacts/audits/deterministic-graph-part2-migration.md`, Milestone B/E). The **only** remaining legacy-shaped, bare-key artifacts are `WorkoutProgression v1`/`LevelModifier v1`, deliberately retained byte-frozen for historical-replay verification (`DependencyVersionCascadeTests`'s own comments: *"must remain byte-for-byte unchanged"*). Migrating **them** to exact refs would mutate already-published historical artifacts and defeat the entire purpose of retaining them (they exist specifically to prove the *old*, bare-key resolution mechanism remains reproducible). Classification: **already done where it should be done; forbidden where it would break historical-replay intent.** L2 is not an additional action this phase needs to recommend — it is already-realized fact, confirmed, not proposed.

## 10. L3 — VALIDATED + resolver scope filter

The phase brief cautions this "might require capability/status metadata" and to reject if authority becomes ambiguous. Evaluated narrowly (not as a broad second status system): a single, additive, fail-closed-compatible boolean on `WorkoutDefinition` metadata — e.g. `EligibleForLegacyDefaultResolution` — defaulting to `true` for every existing artifact (zero behavior change for anything already in the catalog) and set `false` only for narrowly-additive versions never meant to become anyone's silent default. This is **not** ambiguous because it answers exactly one, single, already-isolated question (§14 item 3, implicit fallback) and is consulted by exactly one method (`FindWorkout(key, ledger)`) — it does not touch `CatalogStatus` (still solely answers item 1), does not touch exact-reference resolution (still solely answers item 2 via combination manifests). Classification: **viable, narrowly scoped, single-purpose — not rejected.**

## 11. L4 — new artifact lifecycle state

`CatalogStatus` is deliberately, explicitly closed to four values by its own doc comment. Adding a fifth (e.g. `APPROVED_EXACT_REFERENCE_ONLY`) would reverse a documented, deliberate prior design decision, ripple through every one of the ~10 real consumers enumerated in §3, and is unnecessary — L3's narrow metadata addition solves the identical problem without touching the enum. Classification: **`REJECTED_UNNECESSARY`** — not invented merely to solve this one case, per the phase's own explicit instruction.

## 12. L5 — combination/manifest activation authority

Confirmed, via §6/§16's direct evidence, to already be the real, live, proven architecture for every schemaVersion≥2 (exact) combination — including the one currently serving `TEN_K__4D__INTERMEDIATE`. `WorkoutDefinition VALIDATED` already means "the artifact itself is valid [and eligible for exact publication]"; combination/manifest exact dependency closure already, independently, determines which product cell actually uses it. The legacy resolver does **not** currently auto-activate every new `VALIDATED` version for every compatible cell **for any real, currently-active combination** — only for the retained legacy-shaped historical-replay combinations, which is a narrower, already-understood, already-isolated exposure (§5/§16). Classification: **`CONFIRMED_ALREADY_REALIZED_PRIMARY_ARCHITECTURE`** — this is the correct target model and requires zero new mechanism for any future (5D or beyond) exact-closure combination.

## 13. Option matrix

`CATALOG_LIFECYCLE_OPTION_MATRIX`

| Option | Semantic correctness | Artifact validity clarity | Legacy zero-delta | Exact-ref alignment | Historical replay | Migration cost | Hidden authority risk | Future-distance generality | Implementation complexity | Long-term fit | Recommended? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| L1 (DRAFT-only) | Partial (blocks eventual publication) | Clear | Perfect (current state) | Full | N/A (never published) | None | None | Full | None | Interim only | Interim, not terminal |
| L2 (legacy pin migration) | Correct where applicable | Clear | Already achieved for live cell; would break intent if applied to frozen historical docs | Full | Preserved (already realized) | Zero (already done); forbidden for the 3 frozen historical docs | None | Full | None (already complete) | Already the realized target for live cells | **Confirmed fact, not a new action** |
| L3 (narrow resolver scope filter) | Correct, single-purpose | Clear (does not touch status) | Full — existing artifacts unaffected by default | Full | Preserved | Low (one additive field + one filter clause) | Low if strictly single-purpose and default-preserving | Full (generic per-artifact flag, any distance) | Low | Fits as the narrow permanent fix for additive-only versions | **Recommended (narrow instrument)** |
| L4 (new lifecycle state) | Would work but violates deliberate 4-value closure | Would fragment | Full | Full | Preserved | High (touches ~10 consumers + enum) | Low if done carefully, but unnecessary complexity | Full | High | Unnecessary given L3 suffices | Rejected — unnecessary |
| L5 (manifest activation authority) | Correct — matches already-realized reality | Clear | Full (already true for the live cell) | Full (this **is** exact-reference alignment) | Preserved | Zero (already realized) | None | Full — the generic model for any distance | None (already implemented) | **This is the target architecture** | **Recommended (primary)** |

## 14. Selected architecture

**Hybrid: L5 (primary) + a narrow, single-purpose instance of L3 (permanent containment instrument).**

- **L5 is confirmed, not newly designed**: every future (5D, HM, Marathon) combination should — and, by established precedent, will — use schemaVersion≥2 exact `WorkoutProgression`/`LevelModifier` shapes, resolving exclusively through `CatalogSourceSnapshot.FindWorkout(key, version)` (exact overload). This requires **zero** new mechanism; it is already how the real, live `TEN_K__4D__INTERMEDIATE v4` combination works today.
- **A narrow L3 instrument is additionally required**, because L5 alone only governs *new* combinations' activation — it does not, by itself, prevent the still-live legacy bare-key resolver (`FindWorkout(key, ledger)`) from silently changing its answer once a same-keyed higher version becomes `VALIDATED`. This matters permanently (not just as a temporary promotion-ordering concern) because the four new versions are, by FREQ.6D.4B.1/4B.2's own frozen "eligibility-only diff" invariant, narrow *additions* (new `eligiblePhases`), never intended to become the new default for every other consumer of that key. The legacy resolver's "highest wins" rule has no concept of "additive, not a replacement" — so a single, additive, fail-closed-compatible per-artifact flag governing *only* legacy bare-key default-resolution eligibility is required to make this distinction durable, not merely accidental (today's accidental safety is "it's still DRAFT," which cannot survive real publication).

Authority separation (§14 requirement) under the selected architecture:

1. **Artifact validity** → `CatalogStatus` alone (unchanged meaning).
2. **Combination activation** → exact `(key, version)` references in a combination's dependency closure alone (unchanged meaning, already proven for the live cell).
3. **Implicit bare-key fallback** → `CatalogStatus != Draft` **and** the new narrow, additive, single-purpose legacy-eligibility flag (default `true`, so every pre-existing artifact is 100% unaffected; explicitly `false` only for the four narrowly-additive new versions).

No ambiguity: exactly one method (`FindWorkout(key, ledger)`) consults the new flag; nothing else does.

## 15. Legacy fallback debt

Searched the real repository for `TD-LEGACY-FALLBACK-NO-SILENT-COERCION-001` or an equivalent — not found. The only pre-existing, superficially-similar ID (`TD-LEGACY-FALLBACK-PATH-UNTRACED-001`, from `PHASE_10K_GEN_CHECKPOINT_1_...md`) concerns an unrelated RunningApp SQL legacy-routing fallback, not the PlanCatalog resolver — not the same debt, not reused. `FindWorkout(string key, IRetirementLedger?)`'s own doc comment already self-declares its intended scope (*"LEGACY resolution only... must never be used to assemble a new (schemaVersion ≥ 2) candidate graph"*), confirming the highest-non-retired behavior is **understood, disclosed, intentional legacy-compatibility debt**, not canonical forward-looking authority. This phase does not resolve that debt broadly; it closes only the specific instance blocking these four versions, via the narrow L3 instrument in §14. A new debt ID, `TD-CATALOG-LEGACY-RESOLVER-STATUS-CONFLATION-001`, is recorded to track the general conflation (§7) for any future artifact that faces the same narrow-addition-vs-default tension — not to be treated as already resolved by this phase's narrow fix.

## 16. Manifest audit

Direct inspection of every `catalog/combinations/ten-k-4d-intermediate.v*.json` and its dependency chain:

| Combination version | Status | Master template | WorkoutProgression | LevelModifier | Resolution shape |
|---|---|---|---|---|---|
| v1 | VALIDATED | TEN_K_MASTER v1 | v1 (bare-key) | INTERMEDIATE_MODIFIER v1 (bare-key) | Legacy |
| v2 | VALIDATED | TEN_K_MASTER v2 | v1 (bare-key) | INTERMEDIATE_MODIFIER v1 (bare-key) | Legacy |
| v3 | VALIDATED | TEN_K_MASTER v2 | v1 (bare-key) | INTERMEDIATE_MODIFIER v1 (bare-key) | Legacy |
| **v4 (highest VALIDATED — real, current)** | VALIDATED | TEN_K_MASTER v3 | **v2 (exact `workoutCandidates`)** | **INTERMEDIATE_MODIFIER v2 (exact `eligibleWorkouts`)** | **Exact** |
| v5–v9 | DRAFT | (various, unpublished experiments) | — | — | Not eligible for publication |
| v10 | DRAFT | TEN_K_MASTER v6 | (not yet inspected — out of scope, DRAFT, unpublished) | — | Not eligible for publication |

Direct answer to §16: **yes**, `TEN_K__4D__INTERMEDIATE` already pins exact `WorkoutDefinition` versions — as of combination v4, the real currently-active one. The reason a bare-key path still exists at all is that combinations v1–v3 are deliberately retained, byte-frozen, for historical-replay verification (`DependencyVersionCascadeTests`) — they are not "another path" competing with v4 for live traffic; they are archival. No further manifest migration is required for the live cell. No historical combination should be migrated (that would violate their own immutability tests).

## 17. Four DRAFT versions

| Version | Content complete? | Passes current validation? | Only blocker besides resolver side-effect? | Referenced by which real profiles | Requires non-DRAFT for future 5D publication? | Ultimate intended lifecycle state |
|---|---|---|---|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO v3` | Yes | Yes (schema + graph green) | None — zero legacy-resolver exposure at all (never referenced by any progression/level-modifier, bare-key or exact; confirmed by repository search) | `INTERMEDIATE_5D_FOUNDATION_PRIMARY` v1 | Yes, before real publication | `VALIDATED`, legacy-eligibility flag irrelevant (never at risk) but set `false` for consistency/documentation |
| `THRESHOLD_TEMPO v5` | Yes | Yes | None besides resolver side-effect — same key as `v4`, which the legacy resolver would supersede | `INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED` v1 | Yes | `VALIDATED`, legacy-eligibility flag `false` |
| `FARTLEK v5` | Yes (corrected 3-component skeleton per FREQ.6D.4B.4) | Yes | None besides resolver side-effect — same key as `v4` | `INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED` v1, `INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` v1 | Yes | `VALIDATED`, legacy-eligibility flag `false` |
| `GOAL_PACE_TEN_K v3` | Yes | Yes | None besides resolver side-effect — same key as `v2`; also excluded from `DependencyVersionCascadeTests`'s v4-uniform assertion already (a pre-existing tolerance, not new) | `INTERMEDIATE_5D_TAPER_PRIMARY` v1 | Yes | `VALIDATED`, legacy-eligibility flag `false` |

## 18. Historical exact versions

`THRESHOLD_TEMPO v4`, `GOAL_PACE_TEN_K v2` (and `FARTLEK v4`) remain exactly as referenced by `INTERMEDIATE_5D_BUILD_PRIMARY`, `INTERMEDIATE_5D_RACE_SPECIFIC_PRIMARY`/`_SECONDARY_CONTROLLED`, and the live combination v4's own exact closure. Nothing in the selected architecture upgrades these automatically — exact references always win by construction (`FindWorkout(key, version)` never consults the legacy-eligibility flag or version ordering at all). No product/version migration is proposed or required for these.

## 19. Legacy 3D/4D behavior contract

Golden fixtures directly confirm current exact versions for the zero-delta contract:

- `WorkoutArtifactImmutabilityTests.CurrentActiveResolution_...`: `FindWorkout("EASY_STANDARD")`, `("FARTLEK")`, `("LONG_RUN_STANDARD")`, `("THRESHOLD_TEMPO")` (no ledger) all resolve to **v4**.
- `DependencyVersionCascadeTests.ActiveCombinationV3_ResolvesAFullyConsistentVersionedGraph`: re-assembling combination v3 yields `Workouts` at **v4** for every key except `GOAL_PACE_TEN_K` (already tolerant of a differing value there).
- The real, live combination (v4) resolves its own `Workouts` closure via exact refs, independent of any of the above.

The selected architecture preserves all of these unchanged: with the narrow legacy-eligibility flag `false` on the four new versions, `FindWorkout(key, ledger)`'s answer for `FARTLEK`/`THRESHOLD_TEMPO`/`GOAL_PACE_TEN_K` remains `4`/`4`/`2` forever, regardless of the new versions' `CatalogStatus`.

## 20. Public vs gated support

Intermediate×5D remains gated; this phase does not use public activation as a justification for anything above — the selected architecture supports internally valid, exactly-referenced `VALIDATED` artifacts **before** any public product-cell rollout decision, exactly as required. Public activation remains a wholly separate, later decision (outside this phase and outside `FREQ.6D.4D`).

## 21. Publisher semantics

`CatalogPublisher.BuildRelease` (§6 above) requires every artifact included in `stampedForRelease`/bundle assembly to be non-`Draft` (via `ExcludeDraftArtifacts`, applied before stamping and before `CatalogBundleAssembler.Assemble`). It does not separately require non-`Retired` for inclusion beyond the existing retirement-ledger checks already present in `CatalogBundleAssembler`/`CandidatePublishGraphValidator`. `PublishedTemplateBundle` (`Contracts.Bundles`) records only exact artifact provenance (`CatalogArtifactReference`s with key/version/contentHash) — it does not itself carry or re-expose `CatalogStatus`, confirming status is purely an authoring-time concept, never part of the Process A→B published boundary (matching `CatalogStatus`'s own doc comment). This confirms L1 (keep `DRAFT` forever) is **not** technically feasible for real publication — `VALIDATED` promotion is unavoidable before any future 5D combination can be published, which is exactly why the narrow L3 containment (rather than perpetual `DRAFT`) is the correct terminal architecture.

## 22. Replay semantics

The selected architecture preserves historical replay exactly: `CatalogBundleAssembler`'s exact-closure branch (used by the live cell and by any future exact-shaped historical artifact) is untouched — hash/provenance derivation is unaffected by anything in this design. The legacy branch (used only by combinations v1–v3) continues to resolve identically to its current, tested behavior, because the new legacy-eligibility flag defaults to `true`/is a no-op for every artifact already in the catalog and is only ever set `false` on the four new versions — nothing about the resolution for `v1`-`v4`-referenced historical content changes. No historical bundle hash, exact source reference, profile provenance, or `WorkoutDefinition` provenance is altered by this design.

## 23. New-plan generation semantics

Distinguished explicitly: **exact-pinned profile-backed path** (already fully proven — FREQ.6D.4C.2/4C.3's profiles, the live v4 combination, `CatalogBundleAssembler`'s exact branch, `ExactPrescriptionProjectionDependency`) vs. **legacy implicit version path** (bare-key resolution, now understood to be a bounded, disclosed, historical-replay-only mechanism, never to be extended to new content). Long-term, the architecture should indeed eliminate implicit version selection from all *newly authored* combinations — but this is not a new migration this phase must perform: it is already the de facto rule (every schemaVersion≥2 document already uses exact refs; nothing new has used the legacy shape since `WorkoutProgression v1`/`LevelModifier v1`). Classification: **bounded technical debt, already contained, not requiring active migration** — tracked under the same `TD-CATALOG-LEGACY-RESOLVER-STATUS-CONFLATION-001` debt note (§15) for visibility, not urgency.

## 24. Future-distance generalization

The selected architecture is fully generic: `CatalogStatus`, exact `(key, version)` references, and the narrow legacy-eligibility flag are all distance-agnostic — nothing in this design mentions 10K, 5D, or any workout family by name. Half Marathon and Marathon combinations, when opened, will follow the same already-proven pattern (author new content as `DRAFT`, exact-reference it from new/updated combinations, promote to `VALIDATED` with the legacy-eligibility flag `false` whenever a new version narrowly extends rather than replaces an existing key's default). No distance-specific lifecycle rule, no 5D-only version filter, and no per-workout exception is introduced.

## 25. Intended final statuses

All four: eventual `VALIDATED`, with the narrow legacy-default-resolution-eligibility flag explicitly `false` (never eligible for the bare-key resolver's default selection), enabling real publication for a future 5D exact-closure combination while permanently preserving `FindWorkout(key, ledger)`'s current, tested answers (`FARTLEK`→4, `THRESHOLD_TEMPO`→4, `GOAL_PACE_TEN_K`→2). **No status change is performed in this phase.**

## 26. Resolver consequences

Classification: **`NARROW_CONTAINMENT_CHANGE`**. `CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger?)`'s selection predicate gains one additional, additive, default-`true` condition (the new legacy-eligibility flag) alongside its existing `Status != Draft` and retirement checks. Blast radius: exactly one method; its only real production caller is `CatalogBundleAssembler`'s legacy branch, itself reachable only via `TEN_K__4D__INTERMEDIATE` combinations v1–v3 (frozen, historical); the golden/cascade tests are the direct, deliberate safety net already in place to catch any unintended change. No other resolver, no exact-lookup path, and no publisher/graph-validator logic requires modification.

## 27. Manifest changes required

None. No existing supported cell (v1–v4, or any other real combination) requires an explicit exact-version pin it doesn't already have — v4 (the live cell) is already fully exact-pinned (§16); v1–v3 must **not** be touched (their entire purpose is remaining byte-frozen). A future 5D combination will need its own new, exact-shaped `WorkoutProgression`/`LevelModifier`/`TemplateCombination` documents (§6/§23) — that authoring work belongs to `FREQ.6D.4D`, not to this lifecycle-closure phase.

## 28. Failure semantics

| Case | Behavior |
|---|---|
| Exact reference to `DRAFT` | Succeeds for authoring/graph-validation/projection (already proven); fails at real publication (`ExcludeDraftArtifacts` removes it, `WORKOUT_REFERENCE_VERSION_NOT_FOUND` on assembly) |
| Exact reference to `VALIDATED` | Succeeds unconditionally, regardless of the new legacy-eligibility flag (exact lookup never consults it) |
| Exact reference to `RETIRED` | Assembly throws (`CatalogBundleAssembler`'s existing retired-dependency check); publish-graph validation also rejects (`CandidatePublishGraphValidator`) |
| Implicit (bare-key) resolver with multiple versions, no flag set | Existing behavior: highest non-Draft, non-retired wins — unchanged for all pre-existing artifacts (flag defaults `true`) |
| Implicit (bare-key) resolver, flagged version present | The flagged (narrowly-additive) version is skipped; resolution falls through to the next-highest eligible version, preserving today's answer |
| Manifest missing exact version | Existing behavior: `InvalidOperationException` (`WORKOUT_REFERENCE_VERSION_NOT_FOUND` / dependency-resolution failure) — no silent nearest/latest coercion, unchanged |
| Exact version absent from catalog entirely | Existing behavior: `FindWorkout(key, version)` returns `null`; caller-specific `InvalidOperationException` — unchanged |
| New `VALIDATED` version not explicitly activated (no combination references it) | No effect on any bundle — activation is combination-scoped (§14 item 2), never automatic |
| Historical replay reference | Unchanged — legacy branch continues resolving exactly as tested today (§22) |

No silent nearest/latest coercion is introduced anywhere; the one already-existing legacy "highest wins" coercion is explicitly, permanently narrowed rather than removed (removing it entirely would itself be a behavior change to already-tested historical-replay semantics, out of scope here).

## 29. Implementation manifest

Dependency-ordered, for the future `IMPLEMENTATION` phase (not performed here):

1. `ARTIFACT_STATUS_RULE` — add the narrow, additive, default-`true` legacy-eligibility field to `WorkoutDefinition`'s metadata (or an adjacent, single-purpose model) and its schema; confirm `additionalProperties: false` shapes are updated consistently.
2. `LEGACY_VERSION_RESOLVER` — extend `CatalogSourceSnapshot.FindWorkout(string, IRetirementLedger?)`'s filter predicate to also require the new field to be `true` (or absent/default); update its doc comment to state the containment explicitly.
3. `CATALOG_GRAPH` — no new check strictly required (the flag is inert everywhere except the one resolver), but consider a lightweight graph-validator assertion that flags exist and are explicit (not silently defaulted) on any version sharing a key with an already-`VALIDATED` artifact, to prevent future accidental omission.
4. `WORKOUT_DEFINITION_VERSION` — set the new field explicitly `false` on the four affected `DRAFT` sources as part of (not before) their eventual `DRAFT → VALIDATED` promotion, performed together in one deliberate, reviewed change.
5. `TESTS` — add direct unit coverage proving: (a) the flag defaults `true`/preserves current resolution for every pre-existing artifact; (b) a flagged version is skipped by `FindWorkout(key, ledger)` even when it is the highest `VALIDATED` version; (c) exact-reference resolution is completely unaffected by the flag; (d) `WorkoutArtifactImmutabilityTests`/`DependencyVersionCascadeTests` continue passing unchanged after the four versions are promoted.
6. `PUBLISHER_VALIDATION` — no change required; `ExcludeDraftArtifacts`/`PublishReadinessValidator` already correctly gate on `CatalogStatus` alone.
7. `TECHNICAL_DEBT_UPDATE` — record `TD-CATALOG-LEGACY-RESOLVER-STATUS-CONFLATION-001` (§15) in the appropriate governance/debt tracking location, scoped as narrowly closed for these four versions, generally still open.

## 30. 6D.4D readiness gate

`FREQ.6D.4D` may begin only after, in order: (1) this architecture is approved (this phase); (2) the `IMPLEMENTATION` phase in §29 lands and passes full regression; (3) the four `WorkoutDefinition` versions are promoted `DRAFT → VALIDATED` with the legacy-eligibility flag explicitly `false`; (4) `WorkoutArtifactImmutabilityTests`/`DependencyVersionCascadeTests` (and the new tests from §29 item 5) all still pass, proving existing 3D/4D version selection is unchanged; (5) the 8 production profiles remain exactly as authored (no re-authoring required — they already reference the exact versions, and exact references are never affected by the flag); (6) `CatalogPublisher` accepts the new `VALIDATED` status for these artifacts without requiring further lifecycle change; (7) no silent highest-version promotion is observed anywhere in the full regression suite; (8) historical replay (`ActiveCombinationV3_...`, the golden immutability tests) remains deterministic. Only after all eight conditions hold may `FREQ.6D.4D` (dual-lane `Week × LaneOrdinal → ProgressionStage → ProfileRef` engineering) begin — this phase does not design any part of that.

## 31. Final classification

**`FREQ6D4C4_CATALOG_LIFECYCLE_ARCHITECTURE_APPROVED`**

The architecture is fully resolved: L5 (exact-reference/manifest activation authority) is confirmed as the already-realized primary model requiring no new mechanism, and a narrow, single-purpose, fail-closed-compatible legacy-resolver containment instrument (a scoped instance of L3) closes the one remaining, permanent, disclosed gap — the legacy bare-key resolver's inability to distinguish "narrowly additive" from "replaces the default." No legacy exact-pin migration is required (already complete for the live cell; forbidden for the frozen historical cells). No unresolved status-policy question remains — the intended final status and containment mechanism for all four versions are fully specified (§17/§25). `FREQ.6D.4D` remains correctly gated behind the `IMPLEMENTATION` phase defined in §29/§30, not yet begun.
