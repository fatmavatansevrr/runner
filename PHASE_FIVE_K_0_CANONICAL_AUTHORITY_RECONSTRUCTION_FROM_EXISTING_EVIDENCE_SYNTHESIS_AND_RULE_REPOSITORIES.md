# PHASE FIVE-K.0 — Canonical FIVE_K Authority Reconstruction from Existing Evidence, Synthesis and Rule Repositories

**Type:** GOVERNANCE / EVIDENCE-RECONSTRUCTION / CANONICAL-AUTHORITY (no production implementation, no runtime wiring, no public activation, no frontend change, no database change, no catalog JSON change, no new workout definition, no invented numeric authority)

---

## 1. Current V1 distance-support state

Reconstructed by direct source read, not from report paraphrase (`NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans`, `DerivedDistanceEligibilityPolicy.cs`, `V1CatalogPilotIdentityPolicy.cs`, `GoalDistanceKm.cs`):

| Distance domain | Route | Parent | Backend status | Public status |
|---|---|---|---|---|
| `0 < target < 5K` | none | — | Unsupported | Blocked (`SUB_5K_DEFERRED_FROM_V1`) |
| `5K` exact (race) | Canonical `GoalDistance.FiveK` | FIVE_K | No governed `RuntimeCatalog` generation authority | Not production-ready for race plans |
| `5K` exact (habit) | Legacy `GenerationSource.LegacySql` template path | — | Works (HTTP 200), but is a *different*, non-catalog, non-`VolumeSafetyPolicy` generation system (§4) | Live, pre-existing, unrelated to race-plan authority |
| `5K < target < 10K` | Derived | TEN_K | Implemented, public (since DERIVED-DIST.4A) | Public |
| `10K` exact | Canonical | TEN_K | Live | Public |
| `10K < target < HM (21.0975km)` | Derived | HALF_MARATHON | Live since DERIVED-DIST.2 | Public |
| `HM` exact | Canonical | HALF_MARATHON | Live | Public |
| `> HM` | none | — | No ready-higher canonical source | Blocked |

The single remaining V1 race-distance gap is exact 5K's own canonical race-plan generation authority — the subject of this phase.

## 2. Exact FIVE_K blocker (from DERIVED-DIST.4/4A, re-verified)

DERIVED-DIST.4A's exact finding, re-verified directly against current `HEAD` (`d9848f9`) rather than trusted from report text: `V1CatalogPilotIdentityPolicy.ResolveCandidate`/`IsSupportedLevelFrequency` has exactly two `GoalDistance` arms (`TenK`, `HalfMarathon`) — no `FiveK` arm. `CatalogVolumeAndLongRunPlanner.Build` has no `"FIVE_K"` dispatch branch. `VolumeSafetyPolicy.cs` (confirmed, still 1559+ lines) has zero named policy instance for any FIVE_K Level×Frequency cell. `NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans` has exactly two entries (TEN_K, HALF_MARATHON) — FIVE_K is not a ready canonical source for anything. `plan-catalog/catalog/` (all 12 subdirectories) has zero case-insensitive `fivek`/`five_k`/`5k` match. Classification carried forward unmodified: **`FIVE_K_CANONICAL_AUTHORITY_INCOMPLETE`** — a genuine research/authority gap, not merely unwired plumbing. This phase does not reopen or re-litigate that conclusion; it attempts to close it from existing internal evidence, per its own distinct governing prompt.

## 3. Required canonical authority schema

Reconstructed from `VolumeSafetyPolicy.cs`, `RaceHorizonPolicy.cs`, `V1CatalogPilotIdentityPolicy.cs`, `HalfMarathonParentTaperAuthority.cs`, and the TEN_K/HM catalog master definitions — categories only, no values copied:

- **CoreCycle**: minimum/preferred/maximum standalone core weeks (TEN_K: 8/12/14 `RaceHorizonPolicy` unqualified constants; HM: 10/14/16 `HalfMarathon*` constants — two genuinely independent triples, not a shared default).
- **Phases**: Foundation/Build/RaceSpecific/Taper names, order, min/preferred/max duration, compression/extension behavior (catalog-declared per master/progression version).
- **Run layouts**: supported RunsPerWeek, public Level×Frequency matrix (`V1CatalogPilotIdentityPolicy.IsSupportedIdentity`).
- **Workout progression**: stage key, objective, workout family, min/preferred/max exposure, relative order, compression/drop priority (catalog `workout-progressions/`).
- **Volume**: `PreferredMaxWeeklyIncreaseRatio`, `HardMaxWeeklyIncreaseRatio`, `AbsoluteWeeklyIncrementCapKm`, `ResolvedPeakReference` (+ provenance), `GoldenFixtureNonTaperTransitions`, `StartingAbsoluteLongRunCapKm`.
- **Long run**: `LongRunPreferredMinimumShare`, `LongRunPreferredMaximumShare`, `LongRunSelectionShare`, `LongRunHardCapShare`, `PreferredAbsolutePeakLongRunKm` (nullable by design — TEN_K itself leaves this null, §11 of DERIVED-DIST.4A).
- **Taper**: `TaperVolumeMultiplier`/`TaperVolumeMultipliers`, `ResolvedTaperVolumeMultipliers.Count` (duration), activation workout, chained-vs-standalone semantics.
- **Pace**: target-race-pace binding (`RequestedTargetDistanceKm ?? GoalDistanceKm`), threshold/VO2/interval/easy roles, fallback hierarchy, `GoalFeasibilityResolver`'s Riegel-based projection (dormant, distance-generic).
- **Calendar/Safety**: hard-day adjacency, KEY-session-vs-long-run spacing, explicit-zero-readiness behavior (`RuntimeConditionResolutionService`, generic validators).

Every TEN_K/HM field above is a *category* a FIVE_K policy instance would need a value (or an explicit shared-authority pointer) for. None of the category names themselves are new to this phase; this phase's job is to find values, not invent categories.

## 4. Existing FIVE_K source inventory (exhaustive, independently re-run)

Re-ran, independently of DERIVED-DIST.4/4A's own searches, a full-repository case-insensitive search for `5K`, `five k`, `FiveK`, `5000m`, `5-K`, `fivek` across: `plan-catalog/` (all subdirectories — `catalog/`, `artifacts/`, `docs/canonical`, `docs/pending`, `docs/archive`, `docs/specifications`, `schemas/`), every root-level `*.md` file (428 files), and all backend `*.cs` files. Results, in full:

| Hit | Location | Classification |
|---|---|---|
| `GoalDistance.FiveK` enum member | `backend/RunningApp.Domain/Enums/GoalDistance.cs` | Dormant identity only, no authority |
| `GoalDistanceKm.Resolve(FiveK) => 5.0` | `backend/RunningApp.Application/Common/GoalDistanceKm.cs` | Distance constant only (fail-closed for every other unresolved value since DIST-GEN.0.1) |
| `CanonicalTargetFinishTimePolicy.FiveKSeconds = 1680` (28:00) | `backend/RunningApp.Application/Common/CanonicalTargetFinishTimePolicy.cs` | Product-average finish-time default only, not training authority |
| `CanonicalDistanceFamilyResolver.FiveKUpperBoundKm` | `backend/RunningApp.Application/RuntimeCatalog/CanonicalDistanceFamilyResolver.cs` | Band-boundary constant for rejection classification only; the resolver itself is dormant/unwired (`CatalogNotWiredToGenerationTests`) |
| `ZeroDelta_HabitFiveK_StillSucceeds` test, real `five_k` habit-preview HTTP 200 | `backend/RunningApp.IntegrationTests/GoalDistanceCustomFailClosedTests.cs` | **A genuinely new finding this phase adds beyond DERIVED-DIST.4A's own inventory** (§5 below) — the *habit* flow, not the *race* flow, already succeeds for FiveK today, but through an entirely separate `GenerationSource.LegacySql` template system with no connection to `RuntimeCatalog`/`VolumeSafetyPolicy`/the catalog pipeline at all |
| `plan-catalog/catalog/**` (all 12 subdirectories) | — | Zero matches (confirmed again, independently) |
| `plan-catalog/docs/evidence-log.md` / `.json` | — | Zero FIVE_K-authority entries. Two entries (EV-001 and one dated entry) reference a **Riegel 5K→10K pace-*conversion*-formula** already corroborated in-repo via `golden-fixture-v3`'s `RIEGEL_CONVERSION_5K_TO_10K` rule — this is a generic, distance-pair-agnostic pace-projection *technique*, not FIVE_K race-plan training authority (no volume/phase/taper/long-run content); it is the one piece of real evidence directly touching "5K" by name anywhere in the evidence log |
| `plan-catalog/artifacts/audits/*.md`/`*.json` (10 files) | — | Zero FIVE_K references (direct grep, zero hits) |
| `PHASE4G_5K_COMPRESSED_CORE_ACTIVATION.md` | root | **False positive, confirmed by reading in full**: "5K" here means an 8-11-*week* "compressed core" horizon-classification bucket for TEN_K generation, not the 5-kilometre race distance. No relevance to canonical FIVE_K. |
| `PHASE_DIST_GEN_0_1_..._SILENT_FIVE_K_FALLBACK_ELIMINATION.md` | root | A safety/defect-correction phase that **removed** `GoalDistanceKm.Resolve`'s unconditional `_ => 5.0` fallback (so arbitrary/`Custom` distances stop silently becoming 5K plans) — about eliminating an *unsafe shortcut into* FiveK, not about building FiveK race authority. Confirms (§3 of that report) the pre-fix defect and its closure; no FIVE_K training content. |
| `TEN_K__*`/`golden-10k-*` file names containing the substring "5K" inside "10K"/"15K" etc. | plan-catalog artifacts | Substring false positives (e.g. "5D" inside filenames, "15K"/"16K"/"18K" catalog cells) — individually opened/checked where ambiguous, none is FIVE_K-specific |
| `prescription-profiles/advanced-build-secondary-controlled.v1.json`, `intermediate-5d-build-secondary-controlled.v1.json` | plan-catalog/catalog | Matched on "5d" (5-day frequency), not 5K distance — confirmed by content read |

**No 5K Pace and Intensity synthesis, no 5K-Specific Preparation synthesis, no 5K vs 10K vs HM Master Comparison document, and no FIVE_K evidence-log entry exist anywhere in this repository.** This independently corroborates DERIVED-DIST.4A's own exhaustive finding rather than merely trusting it — the search was re-run from scratch, with additional attention to semantically-disguised "5K" hits (phase-count buckets, day-frequency labels, substring matches inside 10K/15K/16K/18K artifact names), and found nothing DERIVED-DIST.4A missed except the one genuinely new, non-authority-bearing item (§4's habit-path row).

## 5. 5K Pace and Intensity findings

**SOURCE_SILENCE.** No such synthesis document exists (§4). The only pace-adjacent artifact touching "5K" by name is the Riegel 5K→10K conversion technique in the evidence log (§4), which is a generic cross-distance projection formula (given a known time at one distance, project a time at another), not a statement of what FIVE_K's own race-pace role, threshold relationship, or interval/VO2 role should be. It cannot be read as 5K-specific pace-intensity authority.

## 6. 5K-Specific Preparation findings

**SOURCE_SILENCE.** No 5K-Specific Preparation synthesis exists anywhere in the repository (§4). No descriptive prose, author-specific preference, or product-simplification document mentioning 5K preparation was found to even classify.

## 7. 5K/10K/HM comparison findings

**SOURCE_SILENCE** for any dedicated 5K/10K/HM master-comparison document — none exists. What *does* exist is the real, governed numeric/structural comparison embedded directly in `VolumeSafetyPolicy.cs`/`RaceHorizonPolicy.cs` themselves between TEN_K and HALF_MARATHON (§3) — e.g., TEN_K's single 0.53-multiplier taper week vs HM's two-week 0.70/0.43 taper, TEN_K's null `PreferredAbsolutePeakLongRunKm` vs HM's 19.0km. This is real evidence of *how two canonical distances can differ*, useful only as a schema/structure precedent (per governing-prompt §4/§9's explicit permission) — it supplies no FIVE_K value and was not used to interpolate one.

## 8. Phase-duration findings

**SOURCE_SILENCE.** No FIVE_K-specific phase-duration (Foundation/Build/RaceSpecific/Taper min/preferred/max) synthesis exists. See §16 for the horizon-authority classification this silence produces.

## 9. Long-run findings

**SOURCE_SILENCE** for FIVE_K specifically. The only existing long-run authority is TEN_K's and HM's own named `VolumeSafetyPolicy` instances (§3) — real, but each is a per-canonical-distance authority object, not a generic "long-run synthesis" document that could be consulted for a new distance without copying another distance's number. No generic (distance-agnostic) long-run synthesis artifact exists in `plan-catalog/docs/` to consult. See §25.

## 10. Weekly-hard-day findings

**SOURCE_SILENCE** for any FIVE_K-specific weekly-hard-day-count document. The existing hard-day/KEY1/KEY2 semantics live in the generic catalog phase-allocation/workout-binding machinery (`CatalogWorkoutBinder`, `CatalogVolumeAndLongRunPlanner`) and in the calendar-safety validators (§29) — these are already distance-agnostic mechanisms, not FIVE_K-specific numeric content, and would apply unchanged to a hypothetical FIVE_K policy instance exactly as they do to TEN_K/HM today.

## 11. Threshold/VO2 findings

**SOURCE_SILENCE.** No FIVE_K-specific threshold/VO2/interval-role synthesis exists. The generic pace-resolution machinery (`PaceSourceResolver`, `GoalFeasibilityResolver`) is distance-agnostic infrastructure (§27) that could in principle serve a FIVE_K policy once one exists, but it carries no FIVE_K-specific threshold/VO2 *content* today.

## 12. Taper findings

**SOURCE_SILENCE** for FIVE_K specifically. `HalfMarathonParentTaperAuthority.cs`'s own doc comment (directly read) explicitly states canonical TEN_K's taper (one week, 0.53 multiplier) is genuinely different from HM's own (two weeks, 0.70/0.43) and that "TEN_K is deliberately NOT a consumer of this component" — i.e., the repository's own authors have already established, for the two distances that do have authority, that taper is NOT assumed shared by default. There is no basis to assume FIVE_K's taper equals either TEN_K's or HM's without dedicated evidence; none exists.

## 13. Catalog capability

Direct read of `plan-catalog/catalog/workout-progressions/`, `plan-catalog/catalog/templates/`, and `plan-catalog/catalog/prescription-profiles/`: no FIVE_K entries anywhere. The *catalog engine* itself (`CatalogVolumeAndLongRunPlanner`, `CatalogWorkoutBinder`, the generic phase allocator) is architecturally distance-agnostic — it already dispatches by `(CanonicalDistanceFamily, Level, RunsPerWeek)` string/enum keys (confirmed unmodified since DERIVED-DIST.4's own audit, §9 of that report) and would accept a `"FIVE_K"` branch with zero structural change *if* real workouts/numeric authority existed to populate it. This is a `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` finding per governing-prompt §13: the pipe is ready; nothing flows through it for FIVE_K. Classification for every FIVE_K-required stage: **`AUTHORITY_MISSING_DESPITE_CATALOG_CAPABILITY`** — the machinery could run a FIVE_K plan the moment real authority exists; it cannot manufacture that authority itself.

## 14. Shared generic authority

Confirmed genuinely distance-agnostic (apply to any canonical distance, FIVE_K included, the moment one exists), per direct source read:

- Calendar adjacency / hard-day spacing rules (generic catalog-phase-allocation + calendar materialization).
- Fitness-evidence hierarchy (`PaceSourceResolver`, recent-race / time-trial / structured-test / normal-pace / effort-fallback order) — distance-generic, dormant/unwired to live generation per DIST-GEN.0.1's own confirmed audit, but real and not FIVE_K-specific.
- `GoalFeasibilityResolver`'s Riegel-formula cross-distance pace projection — distance-generic, dormant, not FIVE_K-specific.
- `GoalDistanceKm`'s fail-closed resolution pattern (DIST-GEN.0.1) — applies identically regardless of which `GoalDistance` is eventually added.
- The generic phase-allocator/compression-extension machinery (`CatalogVolumeAndLongRunPlanner`'s dispatch-by-family pattern).

None of these require FIVE_K-owned duplication; a future FIVE_K policy would consume them as-is, exactly as TEN_K and HM already do.

## 15. Horizon authority

**`FIVE_K_HORIZON_BLOCKED`.** No existing evidence supports a FIVE_K-specific minimum/preferred/maximum core-week triple. Unlike TEN_K (8/12/14, real, shipped, used by live canonical generation) and HM (10/14/16, independently real and shipped), there is no third analogous triple anywhere, dormant or otherwise, for FIVE_K. A product default could theoretically be chosen (5K training blocks are conventionally shorter than 10K blocks in general public discourse), but this phase has no *existing, already-approved* product decision to point to, and inventing one here would violate governing-prompt §31-32 (no silent product-default selection in this phase).

## 16. Phase structure

**SOURCE_SILENCE.** No evidence states whether FIVE_K should use the same Foundation/Build/RaceSpecific/Taper taxonomy TEN_K/HM already use, or a different structure. Given the taxonomy itself is the generic catalog vocabulary (not a per-distance invention — confirmed by direct read of `plan-template.schema.json`/`workout-progression.schema.json`), the *taxonomy* itself is `CLOSED_SHARED_AUTHORITY` (any future canonical distance would use the same named phase vocabulary, this is a schema fact, not a training-science claim), but the *durations/compression priorities* for FIVE_K specifically remain `SOURCE_SILENCE`.

## 17. Workout progression

**SOURCE_SILENCE.** No FIVE_K stage-by-stage progression (purpose, family, min/preferred/max exposure, relative order) exists in any synthesis or catalog artifact. Cannot be reconstructed without either dedicated evidence or copying TEN_K's/HM's own stage counts — both forbidden (governing-prompt §4/§12).

## 18. Exposure authority

**SOURCE_SILENCE**, for the same reason as §17 — exposure counts (min/preferred/max KEY1/KEY2 repetitions per stage) are part of the same missing workout-progression authority.

## 19. Run-layout authority

**SOURCE_SILENCE.** No evidence states which `RunsPerWeek` values FIVE_K should support. `V1CatalogPilotIdentityPolicy` has no FIVE_K arm to consult (§2). Per governing-prompt §14, candidate cells are NOT inferred from TEN_K/HM's own matrices — every candidate FIVE_K Level×Frequency cell is classified `ARCHITECTURALLY_POSSIBLE_BUT_UNGOVERNED` (the catalog engine could run it, §13) rather than `SUPPORTED_BY_EXISTING_AUTHORITY`.

## 20. Public Level×Frequency authority

**SOURCE_SILENCE** for every cell. No FIVE_K row exists in `V1CatalogPilotIdentityPolicy` at any Level×Frequency combination. No cell is classified `SUPPORTED_BY_EXISTING_AUTHORITY`; all are `ARCHITECTURALLY_POSSIBLE_BUT_UNGOVERNED`. Nothing is activated.

## 21. Weekly-volume authority

**SOURCE_SILENCE** for every candidate FIVE_K Level×Frequency cell. No `VolumeSafetyPolicy` instance of any kind (named or default) resolves for FIVE_K.

## 22. Peak-volume authority

**SOURCE_SILENCE**, and per governing-prompt §16, no already-governed *generic derivation rule* exists that could produce a FIVE_K peak-volume band without distance-scaling/interpolation/Riegel-scaling/TEN_K-copying — all of which are explicitly forbidden. **`BLOCKED`** — not merely silent, but structurally un-derivable from any existing generic rule.

## 23. Calibration/start-volume authority

**SOURCE_SILENCE.** No FIVE_K calibration/starting-volume authority exists, named or inferable.

## 24. Growth/cutback authority

**CLOSED_SHARED_AUTHORITY, mechanism only; values SOURCE_SILENCE.** The *mechanism* for weekly growth-rate capping (`PreferredMaxWeeklyIncreaseRatio`/`HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` as named record fields) is generic infrastructure any future FIVE_K `VolumeSafetyPolicy` instance would use verbatim — but the *numeric values* for FIVE_K itself are unestablished; TEN_K's 0.07/0.08/2.0-2.5 and HM's own (independently set) values cannot be assumed to apply to FIVE_K without dedicated evidence.

## 25. Long-run authority

**SOURCE_SILENCE/BLOCKED**, per §9. No FIVE_K-specific `LongRunPreferredMinimumShare`/`LongRunPreferredMaximumShare`/`LongRunSelectionShare`/`LongRunHardCapShare`/`PreferredAbsolutePeakLongRunKm`/`StartingAbsoluteLongRunCapKm` exists. Per governing-prompt §17-18, the absence of an absolute ceiling is *not automatically* a blocker if other constraints sufficiently govern safety (as TEN_K itself already demonstrates, §11 of DERIVED-DIST.4A) — but that conclusion must be evidenced per-distance, not assumed, and no FIVE_K-specific evidence (of either an absolute ceiling or of "share-based bounds alone suffice for FIVE_K") exists to support either conclusion. The share-quadruple *mechanism* is `CLOSED_SHARED_AUTHORITY`; its FIVE_K *values* are `SOURCE_SILENCE`.

## 26. Taper authority

**SOURCE_SILENCE/BLOCKED**, per §12. Neither a duration, a multiplier, nor a "shared with TEN_K/HM" determination exists for FIVE_K. `HalfMarathonParentTaperAuthority`'s own doc comment (§12) is direct evidence that taper is NOT assumed shared across existing distances by default in this codebase's own convention — reinforcing that assuming it for FIVE_K without evidence would violate this engagement's own established practice, not just this phase's instructions.

## 27. Pace authority

**Mechanism: CLOSED_SHARED_AUTHORITY (dormant). Content: SOURCE_SILENCE.** `GoalFeasibilityResolver.ResolveFromRecentRace`'s Riegel-based cross-distance time projection (confirmed real, dormant, unwired to live generation per DIST-GEN.0.1's audit — `GoalFeasibilityResolverTests`/`PaceSourceResolverTests` pass unchanged, no production call site) is distance-generic infrastructure that would, once wired, already be able to project a target pace for ANY distance pair including 5K↔10K, with no FIVE_K-specific authority required for the projection mechanism itself. What remains silent is FIVE_K's *own* threshold/VO2/interval/easy *role* classification (e.g., "is 5K race pace the same tier as TEN_K's own interval pace, or its own tier?") — no synthesis answers this.

## 28. Fitness evidence hierarchy

**CLOSED_SHARED_AUTHORITY.** The recent-race/time-trial/structured-test/normal-pace/effort-fallback hierarchy (`PaceSourceResolver`) is distance-agnostic by construction — it classifies *evidence quality*, not distance — and needs no FIVE_K-specific authority to function once wired. Confirmed unchanged, dormant, and not FIVE_K-specific.

## 29. Calendar safety

**CLOSED_SHARED_AUTHORITY.** HARD+HARD adjacency, KEY-session-vs-long-run spacing, easy-adjacency, minimum spacing, and preferred-day rules are implemented generically in the catalog's phase-allocation/calendar-materialization machinery, consulted identically regardless of canonical distance family. No FIVE_K-specific calendar rule would be required — this is the one category where existing generic authority is already fully sufficient.

## 30. Validator requirements

| Validator | Classification |
|---|---|
| `GoalDistanceKm.TryResolve` fail-closed gate (DIST-GEN.0.1) | `REQUIRES_FIVE_K_CONFIGURATION` (trivial — just add the real arm once authority exists; already present for the enum value itself) |
| `GenerateRacePlanPreviewRequestValidator`/`GenerateHabitPlanPreviewRequestValidator`/`GeneratePreviewRequestValidator` | `ALREADY_GENERIC` (distance-identity-agnostic validation shape) |
| `RaceHorizonPolicy.DecideForDistance` | `REQUIRES_NEW_RULE` (needs a FIVE_K horizon triple branch; none exists, §15) |
| `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`IsSupportedLevelFrequency` | `REQUIRES_NEW_RULE` (needs a FIVE_K arm; structurally trivial to add, substantively `BLOCKED` on what values to put in it, §20) |
| `CatalogVolumeAndLongRunPlanner.Build`'s family dispatch | `REQUIRES_NEW_RULE` (needs a `"FIVE_K"` branch; the dispatch pattern itself is `ALREADY_GENERIC`, §13) |
| Calendar/adjacency validators | `ALREADY_GENERIC` (§29) |

## 31. Authority ownership

For every item above, explicit ownership is recorded rather than left ambiguous:

- **SHARED_GENERIC**: calendar/adjacency rules, fitness-evidence hierarchy, Riegel cross-distance pace-projection mechanism, `GoalDistanceKm` fail-closed pattern, the catalog's family-dispatch pattern, the phase-name taxonomy itself (Foundation/Build/RaceSpecific/Taper as vocabulary, not durations).
- **OTHER_CANONICAL_PARENT (TEN_K / HALF_MARATHON)**: every concrete numeric value currently in `VolumeSafetyPolicy.cs`/`RaceHorizonPolicy.cs`/taper-policy classes — explicitly NOT reusable for FIVE_K without independent evidence (value equality ≠ authority equality discipline).
- **FIVE_K**: nothing — zero FIVE_K-owned numeric authority exists anywhere.
- **PRODUCT_DEFAULT**: none approved; §15/§36 name what *would* require one.

No FIVE_K value was found to coincidentally equal a TEN_K or HM value (there are no FIVE_K values to compare in the first place), so the "value equality ≠ authority equality" trap (governing-prompt §28) was not triggered — there is nothing to mistakenly treat as shared provenance.

## 32. Authority gap matrix

| AuthorityField | RequiredForRuntime? | ExistingSource? | SourceType | ExactSourceReference | FIVE_K-specific? | SharedWithOtherDistance? | Status |
|---|---|---|---|---|---|---|---|
| CoreCycle min/preferred/max | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Phase taxonomy (names/order) | Yes | Yes | Schema | `plan-template.schema.json` | No | Yes (shared vocabulary) | CLOSED_SHARED_AUTHORITY |
| Phase durations (FIVE_K) | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Run-layout RunsPerWeek set | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Public Level×Frequency matrix | Yes | No | — | `V1CatalogPilotIdentityPolicy.cs` (no FiveK arm) | Would be | No | SOURCE_SILENCE |
| Workout-progression stages | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Exposure min/preferred/max | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Growth-rate cap mechanism | Yes | Yes | Direct canonical rule (mechanism) | `VolumeSafetyPolicy.cs` record shape | No | Yes (mechanism) | CLOSED_SHARED_AUTHORITY |
| Growth-rate cap values | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Peak-volume band/reference | Yes | No | — | — | Would be | No | SOURCE_SILENCE / BLOCKED (no derivation rule, §22) |
| Calibration/start volume | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Long-run share quadruple (mechanism) | Yes | Yes | Direct canonical rule (mechanism) | `VolumeSafetyPolicy.cs` record shape | No | Yes (mechanism) | CLOSED_SHARED_AUTHORITY |
| Long-run share quadruple (FIVE_K values) | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Absolute long-run ceiling | Conditionally | No | — | — | Would be | No | SOURCE_SILENCE (TEN_K's own null-ceiling precedent noted, not copied) |
| Taper duration/multiplier | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Pace fallback hierarchy | Yes | Yes | Direct canonical rule (dormant mechanism) | `PaceSourceResolver.cs` | No | Yes | CLOSED_SHARED_AUTHORITY |
| Riegel cross-distance projection | Yes | Yes | Evidence-corroborated technique (dormant mechanism) | `evidence-log.md` EV-001, `golden-fixture-v3` `RIEGEL_CONVERSION_5K_TO_10K` | No | Yes | CLOSED_SHARED_AUTHORITY |
| Target-race pace role for FIVE_K | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Threshold/VO2/interval role for FIVE_K | Yes | No | — | — | Would be | No | SOURCE_SILENCE |
| Calendar/adjacency rules | Yes | Yes | Direct canonical rule (mechanism) | catalog phase-allocation/calendar materialization | No | Yes | CLOSED_SHARED_AUTHORITY |
| Catalog workout capability (engine) | Yes | Yes | Catalog behavior | `CatalogVolumeAndLongRunPlanner.cs` dispatch pattern | No | Yes (engine) | CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY |
| FIVE_K catalog workout content | Yes | No | — | — | Would be | No | SOURCE_SILENCE |

**Summary count**: 22 rows. **CLOSED_SHARED_AUTHORITY: 7** (mechanisms only, no FIVE_K numeric content). **CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY: 1**. **SOURCE_SILENCE (including the one BLOCKED-by-forbidden-derivation case, peak volume): 14**. **CONFLICTING_EVIDENCE: 0. DECISION_REQUIRED: 0** (no existing evidence conflicts, because there is effectively no FIVE_K-specific evidence at all to conflict). **CLOSED_DIRECT_CANONICAL / CLOSED_EXISTING_SYNTHESIS / CLOSED_EXISTING_PRODUCT_DEFAULT: 0** for any FIVE_K-owned field.

## 33. Source silence

Stated explicitly throughout §5-§26/§32 as `SOURCE_SILENCE` rather than any hedge word. No field was marked "likely," "reasonable," "probably similar to TEN_K," or "standard" anywhere in this report.

## 34. Conflicts

**None found.** There is no second source to conflict with the first, because there is no first FIVE_K-specific source at all in most categories. No `CONFLICTING_EVIDENCE`/`DECISION_REQUIRED` row was needed.

## 35. Product defaults already approved

**None.** No prior phase has approved a FIVE_K-specific product default for horizon, volume, long run, taper, or pace. (DIST-GEN.0.1's fail-closed fix is a safety/validation decision, not a FIVE_K training-authority product default; it deliberately does the opposite — it prevents a silent default from ever reaching FIVE_K.)

## 36. Product decisions still required

If a future phase pursues the product-default path instead of (or alongside) external research, these would need deliberate, explicit, non-silent decisions — named here, not chosen:

1. **FIVE_K CoreCycle horizon triple** — would need either dedicated evidence or an explicit `EVIDENCE_INFORMED_PRODUCT_DEFAULT` decision (not made here).
2. **FIVE_K public Level×Frequency rollout matrix** — same.
3. **Whether FIVE_K shares TEN_K's taper mechanism/duration or needs its own** — same.
4. **Whether FIVE_K's long-run safety model relies purely on the existing share-based mechanism (no absolute ceiling, mirroring TEN_K) or needs an explicit ceiling (mirroring HM)** — this specific one is evidence-shaped (needs to know if a share-only model is independently safe for a 5K-paced long run), not a pure preference, so research is the more honest path (§38 item 2).

None of these were decided in this phase.

## 37. External research gaps

After exhausting internal evidence (§4-§32), the atomic, unresolved, genuinely-required claims are:

1. Canonical FIVE_K weekly-volume / peak-volume-band authority (by Level×Frequency).
2. FIVE_K long-run safety authority (share bounds and/or absolute ceiling, and whether TEN_K's "share-only, no absolute ceiling" precedent is independently appropriate at 5K-race long-run proportions).
3. FIVE_K taper authority (duration, multiplier(s)).
4. FIVE_K CoreCycle horizon triple (min/preferred/max weeks) and workout-progression stage/exposure counts.
5. FIVE_K pace-role classification (how 5K race pace relates to threshold/VO2/interval tiers specifically at this distance, as distinct from TEN_K's own tiering).

These five map directly to the five "would be"/SOURCE_SILENCE clusters in §32's gap matrix — nothing broader is proposed.

## 38. Targeted research queue

- **FIVEK-LIT-01** — Canonical FIVE_K weekly-volume/peak-volume-band authority (by Level×Frequency), including calibration/starting-volume and growth-rate applicability.
- **FIVEK-LIT-02** — FIVE_K long-run safety authority (preferred share/range, hard-cap share applicability, and whether an absolute ceiling is warranted at 5K-specific long-run proportions).
- **FIVEK-LIT-03** — FIVE_K taper authority (duration, volume-multiplier schedule, activation-workout semantics).
- **FIVEK-LIT-04** — FIVE_K CoreCycle horizon (min/preferred/max weeks) and workout-progression stage/exposure structure.
- **FIVEK-LIT-05** — FIVE_K pace/intensity role classification (race-pace vs. threshold vs. VO2/interval vs. easy, specific to 5K rather than inherited from TEN_K's own tiering).

Per governing-prompt §33-34, this is a small, exact, atomic queue — not a re-opening of the Daniels/Pfitzinger/Hansons/Hudson/Magness/McMillan/80-20/ACSM/World Athletics corpus broadly; any future external-research phase should target exactly these five claims.

## 39-46. Draft contracts

**Not produced.** Per governing-prompt §35-42, draft contracts are to be written "if sufficient authority exists." Sufficient authority does not exist for CoreCycle, phases, workout progression, volume, long run, taper, or pace (§15-§27, §32) — drafting numeric contracts for any of these fields now would require inventing values this phase has no authority to invent. The only contract-shaped content this phase can honestly assert is the **shared-authority reuse list** (§14/§31, already stated) and the **catalog-capability readiness statement** (§13: the engine is ready to consume real FIVE_K authority the moment it exists, with zero structural change required beyond adding dispatch-branch literals). No `VolumeSafetyPolicy` FIVE_K instance, no horizon triple, no taper multiplier, no public-matrix cell, and no workout-progression stage list is drafted in this report. This itself is a deliberate, disclosed decision, not an oversight.

## 47. Blocker table

| BlockerId | AuthorityField | WhyRequired | ExistingEvidenceReviewed | WhyExistingEvidenceInsufficient | DecisionType | CanProductionProceedWithoutIt? | RequiredNextAction |
|---|---|---|---|---|---|---|---|
| FK0-B1 | Peak-volume band/reference | Volume planner needs a `ResolvedPeakReference` per Level×Frequency | `VolumeSafetyPolicy.cs` (TEN_K/HM instances, schema only), evidence-log (zero FIVE_K entries) | No FIVE_K value exists; no governed generic derivation rule exists (distance-scaling forbidden) | EXTERNAL_RESEARCH_REQUIRED | No | FIVEK-LIT-01 |
| FK0-B2 | Long-run safety authority | Long-run share/ceiling bounds the single highest-risk weekly session | `VolumeSafetyPolicy.cs` share-quadruple mechanism (generic, reusable); TEN_K's own null-ceiling precedent (not copyable) | No FIVE_K-specific long-run evidence of any kind | EXTERNAL_RESEARCH_REQUIRED | No | FIVEK-LIT-02 |
| FK0-B3 | Taper authority | Final-week de-load prevents race-week overreach | `HalfMarathonParentTaperAuthority.cs` doc comment (confirms taper is NOT assumed shared between existing distances) | No FIVE_K-specific taper evidence; cannot assume TEN_K's or HM's taper applies | EXTERNAL_RESEARCH_REQUIRED | No | FIVEK-LIT-03 |
| FK0-B4 | CoreCycle horizon + workout-progression | Determines minimum viable plan length and stage sequencing | `RaceHorizonPolicy.cs` (TEN_K/HM triples, schema only) | No FIVE_K-specific horizon or stage-count evidence | EXTERNAL_RESEARCH_REQUIRED or PRODUCT_DEFAULT (if evidence supports a range) | No | FIVEK-LIT-04 |
| FK0-B5 | Pace/intensity role | Determines how target-race pace relates to threshold/VO2/easy for a 5K effort specifically | `evidence-log.md` Riegel cross-distance technique (projection only, not role classification) | Riegel technique projects a *time*, not an intensity *role*; no FIVE_K role-classification evidence exists | EXTERNAL_RESEARCH_REQUIRED | No | FIVEK-LIT-05 |

## 48. Authority completeness result

**`FIVE_K_CANONICAL_AUTHORITY_REQUIRES_TARGETED_EXTERNAL_RESEARCH`**

Internal project evidence was exhausted (§4, independently re-run beyond DERIVED-DIST.4A's own search) and found genuinely silent on every FIVE_K-specific numeric/structural authority field. This is not an open-ended "more research needed" — five atomic, named claims (§37-38) are the complete and only remaining gap.

## 49. Runtime implementation readiness result

**`FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_WAITING_ON_EVIDENCE`**

The catalog engine itself requires zero structural change to consume real FIVE_K authority once it exists (§13) — this is an evidence gap, not an architecture gap. No implementation should begin before FIVEK-LIT-01 through 05 (or an explicitly approved product-default set per §36) are resolved.

## 50. Continuous-range consequence

If and when FIVE_K canonical authority is established and wired in a future phase, the only remaining gap in the 5K-to-Half-Marathon V1 range would close, making the entire `5K ≤ target ≤ HM` domain continuously supported. **This has not happened yet.** Today, `5K < target < HM` is continuous and user-reachable; exact 5K itself is not. This phase changes no runtime behavior and makes no claim beyond what is already true.

## 51. Sub-5K deferred statement

**`SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED`.** Nothing in this phase's findings about canonical FIVE_K reopens or advances 1K/2K/3K/4K. `0 < target < 5K` remains explicitly out of V1 scope, unchanged from DERIVED-DIST.4/4A.

## 52. Next phase

Recommended: **FIVE-K.0A — Targeted FIVE_K Evidence Closure for: FIVEK-LIT-01 (peak-volume/weekly-volume authority), FIVEK-LIT-02 (long-run safety authority), FIVEK-LIT-03 (taper authority), FIVEK-LIT-04 (CoreCycle horizon and workout-progression structure), FIVEK-LIT-05 (pace/intensity role classification)** — a bounded external-research phase targeting exactly these five atomic claims, not a broad literature review. Only after FIVE-K.0A (or an explicitly approved product-default substitute per §36) resolves these should **FIVE-K.1 — Canonical FIVE_K Catalog/Runtime Wiring, Public Activation, E2E Proof and 5K→HM Continuous-Range Closure** be opened.

---

## Required result enums

- Authority completeness: **`FIVE_K_CANONICAL_AUTHORITY_REQUIRES_TARGETED_EXTERNAL_RESEARCH`**
- Implementation readiness: **`FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_WAITING_ON_EVIDENCE`**
- Sub-5K: **`SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED`**
- Continuous-range: unchanged — `5K < target < HM` public and continuous; exact 5K not production-ready (pre-existing condition, not altered by this phase)

## Final classification (§53 governance self-audit)

**`FIVE_K_CANONICAL_AUTHORITY_RECONSTRUCTION_COMPLETE`**

This governance phase itself is complete against its own full checklist: every existing FIVE_K-adjacent artifact in the repository was inventoried (§4, independently re-run, including semantically-disguised false positives individually opened and ruled out — PHASE4G.5K, DIST-GEN.0.1, the habit-path zero-delta test); internal evidence was fully exhausted before any external-research conclusion was reached; the complete required runtime-authority field set was enumerated against the real TEN_K/HM schema (§3); every field received an explicit authority status (§32's 22-row gap matrix, zero rows left unclassified); no TEN_K or HM numeric value was copied to fill any FIVE_K gap (§39-46 explicitly decline to draft numeric contracts rather than invent values); source silence is stated explicitly throughout, never hedged; catalog engine capability was explicitly distinguished from training authority (§13); shared generic authority was explicitly distinguished from FIVE_K-owned authority, with zero fields incorrectly claimed as FIVE_K-owned (§14/§31); horizon/phase/workout-progression/Level×Frequency/volume/long-run/taper/pace authority were each resolved to an explicit status (SOURCE_SILENCE or CLOSED_SHARED_AUTHORITY, never left ambiguous); calendar/safety authority was resolved as shared explicitly (§29); no unsupported numeric value was invented anywhere in this report; no product default was silently chosen (§36 names candidates without choosing any); the remaining research queue is five atomic, named claims, not a broad literature directive (§38); implementation readiness is stated explicitly (§49); and zero production/catalog/database/frontend files were changed by this phase (confirmed in the final diff-stat reported to the caller). The substantive FIVE_K authority gap itself remains open — correctly reported as `..._REQUIRES_TARGETED_EXTERNAL_RESEARCH`, not papered over as resolved — but the reconstruction *investigation* this phase was charged with is itself fully and honestly executed.
