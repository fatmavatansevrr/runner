# PHASE 10K-FREQ.6D.4C.5 — Legacy Resolver Eligibility Containment & WorkoutDefinition Validation Promotion

**Implementation phase executing the FREQ.6D.4C.4-approved lifecycle containment architecture. No product decision, no new CatalogStatus value, no combination manifest redesign, no live 3D/4D exact-reference migration, no historical combination rewrite, no profile content change, no dosage change, no dual-lane wiring, no RunningApp change, no public 5D activation.**

## 1. Preflight

`PHASE_LEDGER.md` rows 69-70: `FREQ.6D.4C.3` (`FREQ6D4C3_PROFILES_AUTHORED_CATALOG_LIFECYCLE_BLOCKER_REMAINS`) and `FREQ.6D.4C.4` (`FREQ6D4C4_CATALOG_LIFECYCLE_ARCHITECTURE_APPROVED`) both `DONE`/`VERIFIED`. Commits `97a2d76`, `6a40045` confirmed reachable from HEAD via `git merge-base --is-ancestor`. Starting HEAD `6a400454bd2b3f9e0f3ea556616ca1b819947e5b`, branch `main`, `git rev-list --left-right --count origin/main...HEAD` → `0 7`. `git status --short` → ` m baseline_tmp` only (preserved, untouched). `git diff --check` → clean. `FREQ.6D.4C.5` confirmed not already in `PHASE_LEDGER.md`.

The real FREQ.6D.4C.4 report was re-read in full and its exact findings extracted (not reconstructed from chat): the real resolver inventory (§4 of that report — `HIGHEST_NON_RETIRED` `FindWorkout(key, ledger)` vs. `EXACT_VERSION_REFERENCE` `FindWorkout(key, version)`); the live-cell proof that `TEN_K__4D__INTERMEDIATE v4` already resolves via exact refs (§6/§16); the historical `v1`-`v3` bare-key dependency on `WorkoutProgression v1`/`LevelModifier v1` (§16); the selected hybrid architecture — exact-reference/manifest activation authority (L5) as primary + a narrow, additive, default-preserving legacy-resolver-eligibility flag (scoped L3) as the permanent containment instrument (§14); the intended final status for all four versions — `VALIDATED` with the flag explicitly `false` (§17/§25); the full failure/default semantics table (§28); and the exact implementation manifest (§29).

## 2. Parent SHAs

`FREQ.6D.4C.3`: `e7a6c07` / `a8de6a8`. `FREQ.6D.4C.4`: `97a2d76` / `6a40045`. All reachable from HEAD, confirmed above.

## 3. Files inspected

`schemas/workout-definition.schema.json`; `src/PlanCatalog.Core/Models/WorkoutDefinition.cs`; `src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs`; `src/PlanCatalog.Infrastructure/Serialization/CanonicalJsonOptions.cs`; `src/PlanCatalog.Infrastructure/Hashing/CatalogDocumentHasher.cs`; `src/PlanCatalog.Infrastructure/Publishing/CatalogStamper.cs`, `CatalogPublisher.cs`, `CatalogBundleAssembler.cs`; `tests/PlanCatalog.Tests/Golden/WorkoutArtifactImmutabilityTests.cs`; `tests/PlanCatalog.Tests/Publishing/DependencyVersionCascadeTests.cs`; `tests/PlanCatalog.Tests/Validation/PrescriptionCapabilityMetadataOverlayTests.cs`; the four target `catalog/workouts/*.json` sources; `catalog/combinations/ten-k-4d-intermediate.v{1,2,3,4}.json` and their `templates`/`workout-progressions`/`level-modifiers` dependency chain (to establish the real, exact pinned versions combination `v4` resolves to, since the report's "live cell" claim needed re-verification at the concrete version-number level for this implementation, not just the shape-level claim).

## 4. Files changed

- `schemas/workout-definition.schema.json` — added optional `eligibleForLegacyDefaultResolution: boolean` property (no `additionalProperties: false` at root, no schema-version bump — purely additive).
- `src/PlanCatalog.Core/Models/WorkoutDefinition.cs` — added `bool? EligibleForLegacyDefaultResolution { get; init; }`.
- `src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs` — `FindWorkout(string, IRetirementLedger?)`'s filter predicate gained `&& (x.EligibleForLegacyDefaultResolution ?? true)`.
- `catalog/workouts/aerobic-strength-controlled-intro.v3.json`, `threshold-tempo.v5.json`, `fartlek.v5.json`, `goal-pace-ten-k.v3.json` — `status: DRAFT → VALIDATED`; added `"eligibleForLegacyDefaultResolution": false`. No other field touched.
- `tests/PlanCatalog.Tests/Validation/LegacyResolverEligibilityContainmentTests.cs` (new, 25 tests) — the full targeted matrix.
- `tests/PlanCatalog.Tests/Validation/PrescriptionCapabilityMetadataOverlayTests.cs` — `FourNewWorkoutDefinitionVersions_RemainDraft` (from FREQ.6D.4C.2, asserted the now-intentionally-superseded interim state) renamed/updated to `FourNewWorkoutDefinitionVersions_AreValidatedWithLegacyDefaultResolutionDisabled`.
- `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` — mechanical timestamp-only regeneration (verified via `git diff` with the timestamp lines excluded — zero content delta).
- `PHASE_10K_GEN_CHECKPOINT_1_CURRENT_STATE_AND_GOVERNANCE_BASELINE.md` — added the `TD-CATALOG-LEGACY-RESOLVER-STATUS-CONFLATION-001` debt-registry row (§17 below).

## 5. New eligibility metadata

`WorkoutDefinition.EligibleForLegacyDefaultResolution` — `bool?`. Governs exactly one thing: whether a version is a member of `CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger?)`'s bare-key candidate set. Never consulted by exact `(key, version)` lookup, combination activation, `CatalogPublisher`, phase-eligibility checks, profile validation, or execution projection — verified directly (§8, §12-13, §15, §19).

## 6. Default semantics

Nullable rather than a non-nullable `bool` defaulting to `true`, specifically because `CanonicalJsonOptions` uses `JsonIgnoreCondition.WhenWritingNull` — a non-nullable `bool` is never "null" so it would *always* serialize (including its default `true`), which would change the canonical JSON (and therefore the computed content hash) of every one of the ~40+ pre-existing `WorkoutDefinition` documents that don't have this field in their source JSON, directly violating the phase's own §19 hash-stability requirement. With `bool?`, absence in source JSON deserializes to `null` (the C# default for a nullable value type when a property is simply not touched by the deserializer), which is then omitted on re-serialization — byte-identical round-trip for every historical document. The resolver treats `null` as `true` via `?? true`. Proven directly by `FieldAbsent_PreservesHistoricalLegacyEligibility` and `HistoricalWorkoutV1_ContentHashUnchangedByNewField` (§10).

## 7. Resolver change

`CatalogSourceSnapshot.FindWorkout(string key, IRetirementLedger? retirementLedger = null)`'s `.Where(...)` predicate gained one additional clause: `&& (x.EligibleForLegacyDefaultResolution ?? true)`. This is a candidate-set filter only — the existing `.OrderByDescending(x => x.Metadata.Version).FirstOrDefault()` ranking rule is completely untouched, satisfying the phase's explicit instruction not to introduce new ranking semantics.

## 8. Exact lookup zero-delta

`CatalogSourceSnapshot.FindWorkout(string key, int version)` (the exact overload) was not modified at all — it has no status filter and no eligibility filter, exactly as before. Direct proof: `ExactLookup_IgnoresLegacyEligibilityFlag` constructs a synthetic `Version 2` workout with `EligibleForLegacyDefaultResolution = false` and confirms `FindWorkout(key, 2)` still succeeds. `FourPromotedVersions_ExactLookupSucceeds` proves the same for all four real promoted versions.

## 9. Four status promotions

All four target `WorkoutDefinition` versions promoted `DRAFT → VALIDATED` with `eligibleForLegacyDefaultResolution: false`, in the same atomic implementation commit as the containment mechanism (§21 requirement — never a temporarily-unsafe intermediate state):

| Version | Status before | Status after | Legacy eligibility |
|---|---|---|---|
| `AEROBIC_STRENGTH_CONTROLLED_INTRO v3` | DRAFT | VALIDATED | `false` |
| `THRESHOLD_TEMPO v5` | DRAFT | VALIDATED | `false` |
| `FARTLEK v5` | DRAFT | VALIDATED | `false` |
| `GOAL_PACE_TEN_K v3` | DRAFT | VALIDATED | `false` |

## 10. Historical hash/source safety

`HistoricalWorkoutV1_ContentHashUnchangedByNewField` computes `FARTLEK v1`'s content hash after this phase's model/schema changes and asserts it equals the exact pre-existing hardcoded value from `WorkoutArtifactImmutabilityTests` (`8652ed9aa01a0909ab1efffdacf1e029a164bd5784b505351b7296d6a5f89482`) — unchanged, byte-for-byte, confirming the nullable-field design (§6) achieves true backward-compatible hashing. The four promoted versions' own content hashes legitimately change (new `status`/`eligibleForLegacyDefaultResolution` values) — this is expected and does not retroactively rewrite any already-published historical hash.

## 11. Bare-key selection results

Real, post-promotion bare-key resolution, verified by `FourPromotedVersions_ExcludedFromRealBareKeyDefaultSelection_PriorDefaultStillWins` and `AerobicStrengthControlledIntro_HasZeroLegacyEligibleCandidates_UnchangedByPromotion`:

| Key | Bare-key result | Note |
|---|---|---|
| `THRESHOLD_TEMPO` | `v4` (unchanged) | `v5` excluded by the new flag |
| `FARTLEK` | `v4` (unchanged) | `v5` excluded by the new flag |
| `GOAL_PACE_TEN_K` | `v2` (unchanged) | `v3` excluded by the new flag |
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` | `null` (unchanged) | `v1`/`v2` are both `DRAFT`; `v3` excluded by the flag — this key was never referenced by any real progression/level-modifier bare-key path, so zero real exposure either way, before or after |

## 12. Live 4D result

Direct re-verification (not assumed from the 4C.4 report's shape-level claim): `TEN_K__4D__INTERMEDIATE v4` resolves via `WorkoutProgression v2`'s exact `workoutCandidates` pins — concretely `EASY_STANDARD/FARTLEK/LONG_RUN_STANDARD/THRESHOLD_TEMPO` all at `v2`, `GOAL_PACE_TEN_K` at `v1`. `LiveIntermediate4D_ExactVersionsUnchangedAfterPromotion` re-assembles this real combination post-promotion and confirms these exact same versions resolve, with none of the four newly-promoted `(key, version)` pairs appearing anywhere in the closure — proving the live cell is untouched, exactly as the exact-lookup mechanism guarantees by construction.

## 13. Historical v1–v3 results

`HistoricalCombinations_ResolutionUnchangedAfterPromotion` (parameterized over versions 1, 2, 3) re-assembles each historical combination post-promotion and confirms: every non-`GOAL_PACE_TEN_K` workout still resolves to `v4` (their pre-promotion legacy-resolver answer, unchanged); `FARTLEK v5`, `THRESHOLD_TEMPO v5`, and `GOAL_PACE_TEN_K v3` do not appear in any of their resolved closures. `WorkoutArtifactImmutabilityTests.CurrentActiveResolution_...` and `DependencyVersionCascadeTests.ActiveCombinationV3_ResolvesAFullyConsistentVersionedGraph` — the exact tests that caught the original FREQ.6D.4C regression — both pass unchanged.

## 14. Eight real profile results

All 8 real `WorkoutPrescriptionProfile` documents (from FREQ.6D.4C.3) re-verified post-promotion via the full `Intermediate5DProductionPrescriptionProfileSourceTests` suite: **64/64 passed**, unchanged from before this phase — validation, projection, boundary-validation, capability-overlay behavior, lane-dose representability and hash/provenance are all identical, confirming the legacy-eligibility flag plays no role in profile compatibility (as §12 of the phase brief required).

## 15. Publisher/lifecycle result

`PromotedVersions_SurviveExcludeDraftArtifacts_ViaFullSnapshotStamp` proves all four promoted versions survive `CatalogStamper.StampAsPublished` with status `Published` (the stamper's own non-Draft→Published mapping) and a real, non-null `ContentHash` — confirming they are no longer blocked from real publication purely by being `DRAFT` (the concrete consequence of §21/§6 of the FREQ.6D.4C.4 report). `CatalogPublisher` itself was not modified — it does not inspect the new eligibility flag at all, exactly as the frozen architecture required (the flag is not publication eligibility).

## 16. Golden/cascade regression

Re-ran the exact tests that originally failed when these versions were temporarily promoted in FREQ.6D.4C: `WorkoutArtifactImmutabilityTests` (12 tests) and `DependencyVersionCascadeTests` (7 tests) — **24/24 passed**, now with the four versions genuinely, permanently `VALIDATED` (not merely reverted to `DRAFT` as the interim FREQ.6D.4C fix did). This is the strongest direct proof of lifecycle containment: the same regression trigger (promotion) no longer produces the same regression.

## 17. Legacy debt update

`TD-CATALOG-LEGACY-RESOLVER-STATUS-CONFLATION-001` (identified, not yet recorded, by FREQ.6D.4C.4 §15) added to `PHASE_10K_GEN_CHECKPOINT_1_CURRENT_STATE_AND_GOVERNANCE_BASELINE.md`'s open technical-debt registry (§H), status: narrowly closed for these four versions, general mechanism/conflation remains available/open for any future artifact facing the same narrow-addition-vs-default tension. No duplicate debt created; the legacy resolver itself is explicitly not claimed as removed — it remains in place, unmodified in its core ranking behavior, for historical-replay combinations `v1`-`v3` and any future legacy-shaped consumer.

## 18. Full PlanCatalog result

`dotnet test tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1485 passed, 0 failed, 0 skipped, 1485 total** (1460 FREQ.6D.4C.3 baseline + 25 new containment tests, net zero from the one renamed/updated `FourNewWorkoutDefinitionVersions_...` test).

## 19. Build result

`dotnet build PlanCatalog.sln`: 0 warnings, 0 errors. `dotnet build PlanCatalog.sln -c Release`: 0 warnings, 0 errors. `git diff --check`: clean (only pre-existing CRLF-normalization warnings, no conflict markers).

## 20. File attribution

| Category | Files |
|---|---|
| `WORKOUT_DEFINITION_SCHEMA` | `schemas/workout-definition.schema.json` |
| `WORKOUT_DEFINITION_MODEL` | `src/PlanCatalog.Core/Models/WorkoutDefinition.cs` |
| `LEGACY_RESOLVER` | `src/PlanCatalog.Core/Catalog/CatalogSourceSnapshot.cs` |
| `WORKOUT_DEFINITION_SOURCE_STATUS` / `WORKOUT_DEFINITION_SOURCE_ELIGIBILITY` | `catalog/workouts/aerobic-strength-controlled-intro.v3.json`, `threshold-tempo.v5.json`, `fartlek.v5.json`, `goal-pace-ten-k.v3.json` (both the status change and the new field live in the same 4 files) |
| `TEST` | `tests/PlanCatalog.Tests/Validation/LegacyResolverEligibilityContainmentTests.cs` (new); `PrescriptionCapabilityMetadataOverlayTests.cs` (updated) |
| `TECHNICAL_DEBT` | `PHASE_10K_GEN_CHECKPOINT_1_CURRENT_STATE_AND_GOVERNANCE_BASELINE.md` |
| `DOCUMENTATION` | `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` (mechanical regeneration only); this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | None. No RunningApp, persistence, progression/lane-wiring, or profile-content file appears. |

## 21. 6D.4D readiness

All ten conditions from FREQ.6D.4C.4 §30 and the FREQ.6D.4C.3 input contract are now satisfied: (1)-(4) the four required `WorkoutDefinition`s are `VALIDATED`, exact-reference-resolvable, and legacy-resolver-contained; (5) live Intermediate×4D unchanged (§12); (6) historical replay unchanged (§13); (7) the 8 profiles remain exact and lossless-projecting (§14); (8) the catalog-lifecycle blocker is **CLOSED**; (9)-(10) no athlete-facing content or profile/schema/projector architecture blocker remains (nothing in this phase touched profile content or projection). `FREQ.6D.4D` may now begin — its own dual-lane `Week × LaneOrdinal → ProgressionStage → ProfileRef` engineering was not designed or implemented here, per this phase's explicit scope boundary.

## 22. Implementation SHA

Recorded after commit, §below.

## 23. Final classification

**`FREQ6D4C5_LIFECYCLE_BLOCKER_CLOSED_6D4D_READY`**

The lifecycle containment architecture (FREQ.6D.4C.4) is fully implemented and tested: all four required `WorkoutDefinition` versions are genuinely `VALIDATED` (not held back), exact-reference usable, and permanently excluded from implicit legacy bare-key selection via a narrow, additive, default-preserving, single-purpose field. Live Intermediate×4D, historical `v1`-`v3` replay, and all 8 real production profiles are proven byte-for-byte/behaviorally unchanged. The catalog-lifecycle blocker that has stood since FREQ.6D.4C is now closed. `FREQ.6D.4D` is ready to begin.
