# PHASE 10K-GEN.12 — 2D (Beginner + Intermediate) Core Implementation + Dark Verification

**Parent authority**: `GEN.11` (`TWO_D_BEGINNER_INTERMEDIATE_AUTHORITY_EVIDENCE_CLOSURE_COMPLETE`)
**Phase type**: IMPLEMENTATION + DEFECT DISCOVERY/FIX + DARK VERIFICATION
**Execution status**: DONE (PARTIAL)
**Final classification**: `TWO_D_BEGINNER_INTERMEDIATE_CORE_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL` — structural/calendar/catalog/numeric authority is real, implemented, and dark-verified; workout-content **binding** (and therefore the full volume/long-run plan, Preparation Runway, and LongHorizon) is explicitly **not yet implemented**, blocked on a real, disclosed architecture gap in `ProgressionStageAllocator` (see §6).

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `git log -5`, `git fetch && diff HEAD origin/main` (in sync at phase start), `git status` clean except the pre-existing unrelated local modifications predating this session. `GEN.11` confirmed present with real commit SHAs (`5347f07`/`647315b`). Next free phase ID confirmed unique: `GEN.12`.

## 1. Scope decision — honest, precedented downscoping

The governing prompt's own Phase D scope is Core + Preparation Runway + LongHorizon, both levels, full real-PostgreSQL dark verification. During implementation, the workout-content binding stage surfaced a real, unplanned architecture gap (§6) requiring dedicated design work of its own, not a same-shape mechanical fix. Per this repository's own established precedent for exactly this situation (`FREQ.6D.13`/`FREQ.6D.14`: "deliberately not attempted within this session's remaining budget rather than risk an incomplete or incorrectly-verified change to already-shipped logic"), this phase closes honestly as **DONE (PARTIAL)**: Core's structural/calendar/catalog/numeric layers are real, implemented, and verified; Runway/LongHorizon and full volume/long-run planning are correctly disclosed as not yet done, not silently patched around.

## 2. Architecture: additive repeating-pattern support

The real architecture gap GEN.11 §17 predicted was confirmed by direct code reading: `PlanCatalogCandidateSummary.SlotRoles` (and its 16 consumer files) assumed exactly one fixed weekly role list — no existing frequency needs a week-parity-dependent pattern. Implemented additively, so every pre-GEN.12 candidate is byte-identical:

- `PlanCatalogCandidateSummary`/`CatalogRunLayoutSlots`: two new optional fields, `WeeklyPatternRoles` (`IReadOnlyList<IReadOnlyList<string>>?`) and `PatternPeriodWeeks` (`int?`), both `null` for every existing (non-repeating) layout.
- `PlanCatalogBundleLoader`: parses an optional `patterns`/`patternPeriodWeeks` pair from a `RUN_LAYOUT` document; absent → `(null, null)`, identical to every pre-GEN.12 load.
- `CatalogStageToWeekMaterializer` (the real per-week slot builder every Core generation path already uses): a new `ResolveWeekRoles` step selects `Pattern[(weekNumber-1) % PatternPeriodWeeks]` for the candidate's real global week-ordinal sequence when a pattern is present (falling back to the existing static `RunLayoutSlotRoles` otherwise) — except a `TAPER`-stage week always resolves to `Pattern[0]` (GEN.11 §5's structural taper override). `ValidateWeeklyPatternRoles` extends the existing per-role-count validation to the pattern shape.
- Both real skeleton-generation call sites (`DynamicCoreWeekSkeletonOrchestrator`, and the dormant `CatalogPlanSkeletonOrchestrator`) thread the new fields through from the resolved `CatalogRunLayoutSlots`.

Verified via `Candidate_ResolvesRealTwoDayIdentity_WithRepeatingPattern` (both levels) and `ExistingFrequencies_RemainByteIdentical_ZeroDelta` (`TEN_K__4D__BEGINNER`/`TEN_K__3D__INTERMEDIATE` both resolve `WeeklyPatternRoles == null`).

## 3. Real defects found and fixed

Three real, previously-undiscovered defects were found while making the structural/calendar pipeline actually produce a Pattern-B (zero-`KEY_SESSION`) week — each a genuine "assumed every week has at least one KEY_SESSION" hardcode, none anticipated by `GEN.11`, matching the exact pattern `GEN.10` itself found repeatedly for `Level`. All three are mechanical generalizations (the underlying formula already correctly handled `keyCount == 0`; only an unnecessary floor rejected it), not new authority:

1. **`Combinations(items, k)`** (`CatalogWeekSkeletonCalendarMaterializer`): the combinatorial helper the cross-week backtracking day-assignment search uses guarded `k <= 0 → yield break`, silently treating "zero KEY_SESSION dates to place" as "no valid assignment exists" rather than the one, trivially-valid empty combination it actually is — causing every 2D Pattern-B week to make the *entire plan's* calendar assignment appear infeasible. **Fix**: `k == 0` now yields the empty combination.
2. **`CatalogWeekSkeletonCalendarMaterializer.ValidateSkeletonRoleStructure`**: required `keyCount >= 1` for every week (and a hardcoded `DaysPerWeek is not (3 or 4 or 5 or 6)` gate). **Fix**: widened the `DaysPerWeek` gate to admit `2`; relaxed `keyCount < 1` to `keyCount < 0` (impossible) — the `EASY_SUPPORT`/`LONG_RUN` cardinality formula already correctly computes for `keyCount == 0`.
3. **`DatedGeneratedCatalogPlanSkeletonValidator`**: the sister *output* validator (checked after calendar dating) had the identical `keyCount < 1` assumption. Same fix applied.

Each was found by running the real dark test suite against the real `RUN_LAYOUT_2D` catalog document and its real Pattern-B week, not guessed — and each is a narrow, mechanical, evidence-grounded fix, consistent with this engagement's established defect-discovery discipline.

## 4. Catalog authoring

All single-KEY, zero new prescription content (matching `GEN.7`/`GEN.8`/`GEN.9`'s own finding that single-KEY frequencies need zero new workout definitions — 2D is Model B's `KEY_SESSION`/`EASY_SUPPORT` alternation, never dual-KEY):

- `run-layout-2d.v1.json` — `RUN_LAYOUT_2D`, Model B pattern (`patterns`/`patternPeriodWeeks`), `slots` retained as Pattern A for backward-compatible fallback.
- `ten-k-master.v11.json` — based on the **single-lane** `TEN_K_MASTER v6` lineage (workout progression v5), not the dual-KEY v9/v10 lineage: real domain-graph validation (`TemplateCombinationValidator.ValidateEffectiveWorkoutSet`) confirmed the dual-KEY progression's lane-1 stages are genuinely unreachable for `BEGINNER_MODIFIER` (Beginner never has dual-KEY-eligible content, `GEN.6`), so 2D — being single-KEY — correctly reuses the same v6 lineage `TEN_K__3D__INTERMEDIATE`/`TEN_K__4D__BEGINNER` already use, adding only `2` to the descriptive `supportedRunsPerWeek` list.
- `peak-volume-bands.v7.json` — adds the two `GEN.11`-derived rows (`NEW`/2 → `[16,22]`, `INTERMEDIATE`/2 → `[20,30]`).
- `appsel-race-plan.v8.json` — points at the new band policy version.
- `ten-k-2d-beginner.v1.json` / `ten-k-2d-intermediate.v1.json` — new `TEMPLATE_COMBINATION` documents (`TEN_K__2D__BEGINNER`, `TEN_K__2D__INTERMEDIATE`), reusing `BEGINNER_MODIFIER v1` / `INTERMEDIATE_MODIFIER v6` verbatim (the exact same modifier versions the existing single-KEY combos already use).

Full `PlanCatalog.Tests` suite (1510/1510) passes, including the real production-readiness/domain-graph/publish-pipeline governance tests — not just schema validation. One pre-existing governance test (`DomainWave5D2ResolutionTests`, an exact enumerated allow-list of combinations reaching a shared artifact) was extended to admit the new, legitimate `TEN_K__2D__INTERMEDIATE` reuse, matching the same "advance the allow-list, never weaken the check" discipline `GEN.10` established for its own obsolete-assertion corrections.

## 5. Numeric authority

`VolumeSafetyPolicy.Beginner2D`/`Intermediate2D` implement `GEN.11`'s exact frozen values (§2-3 of the GEN.11 report): `PeakVolumeBand` `[16,22]`/`[20,30]`, `ResolvedPeakReference` 19.0/25.0, `GoldenFixtureStartingVolumeKm` 11.0/13.5 (ratio-derived from `BeginnerFourDay`/`ThreeDayIntermediate` respectively, per `GEN.9`'s own established methodology), long-run shares 55%/60% (frequency-owned, identical at both levels). `ForBeginnerDaysPerWeek`/`ForIntermediateDaysPerWeek` dispatch functions extended with an exact `(CanonicalDistanceFamily=="TEN_K", Level, DaysPerWeek==2)` typed match in `CatalogVolumeAndLongRunPlanner.Build`, mirroring every other frequency's own dispatch-branch discipline (never a broad `DaysPerWeek==2` condition alone).

`TwoDayMissingOrZeroReadinessProductIneligibleException` implements `GEN.11` §7's frozen readiness authority (missing/zero → `PRODUCT_INELIGIBLE`, no default, for both levels — matching Advanced's own pattern, not the other Beginner/Intermediate policies' defaults) — wired into `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` identically to the existing `AdvancedMissingOrZeroReadinessProductIneligibleException` branch, caught generically via the existing `CatalogProductIneligibleException` base type (no new catch-arm required anywhere).

**Disclosed**: this numeric authority is implemented and unit-reachable, but not yet dark-verified end-to-end through a real volume/long-run plan, because that requires workout-content binding to succeed first (§6).

## 6. Disclosed remaining blocker — not implemented, not patched around

**`CatalogWorkoutBinder`/`ProgressionStageAllocator`**: binding actual prescribed workout content into Pattern A's `KEY_SESSION` slot requires `ProgressionStageAllocator.AllocatePhase` to know which calendar weeks in a phase have a `KEY_SESSION` structural slot at all. Direct code reading confirms it currently allocates progression-stage exposure across **every literal calendar week** in a phase, for every declared lane, with no concept of "this week has zero structural slots for this lane" — real for every existing frequency too (which never has a zero-`KEY_SESSION` week), but a **genuine, non-mechanical architecture question** for 2D: should exposure/pacing (`minimumExposures`/`maximumExposures`, stage transitions) count across literal calendar weeks, or only the subset of weeks structurally eligible for that lane? Both are defensible; neither is dictated by `GEN.11`'s own frozen decisions, and choosing wrong risks either under- or over-exposing quality content across the plan. This is a real semantic/design question requiring its own dedicated pass — reached the phase's own explicit STOP condition ("if implementation surfaces a real unresolved semantic gap, STOP → `DOMAIN_DECISION_REQUIRED`; do not patch around it") for *implementation approach*, not product authority (no `DOMAIN_DECISION_REQUIRED` on the product side — `GEN.11`'s authority remains complete).

Confirmed via direct reproduction: `CatalogWorkoutBindingLaneCountMismatchException` fires because the allocator declares 1 progression lane for a Pattern-B week (0 real `KEY_SESSION` slots) — the exact, correctly-firing guard `FREQ.6D.4D` Split A installed ("a catalog lane cannot manufacture an extra structural session"), doing its job precisely as designed.

**Consequently not implemented in this phase**: full volume/long-run planning through the real pipeline (needs binding), Preparation Runway (15-20wk, needs the same allocator), LongHorizon (21-52wk, needs both Runway and the GE segment's own analogous pattern-awareness), Adaptation's 2-session dispatch arm (only reachable once real sessions exist to adapt against), and TargetFinishTimeSource/repair/JIT-restart (all downstream of binding).

## 7. Verification performed (dark, no public HTTP — public gate untouched)

`Gen12TwoDayDarkVerificationTests`, 11 tests, all real (no fabricated skeleton), against the real `TEN_K__2D__BEGINNER v1`/`TEN_K__2D__INTERMEDIATE v1` catalog candidates via `LoadForInternalDryRunAsync`:

- **Candidate resolution**: both candidates resolve `DaysPerWeek=2`, the real `WeeklyPatternRoles`/`PatternPeriodWeeks` from the catalog.
- **Structural skeleton** (`DynamicCoreWeekSkeletonOrchestrator`, the real production Core skeleton builder), 8/12/14-week horizons, both levels: every week has exactly 2 slots including `LONG_RUN`; the non-long role alternates `KEY_SESSION`/`EASY_SUPPORT` by the frozen global week-ordinal parity (`GEN.11` §1/§11 — confirmed *not* reset at the Foundation→Build→RaceSpecific phase boundaries the 8/12/14-week matrix actually crosses); the `TAPER` week is always `KEY_SESSION` (`GEN.11` §5's override), for every horizon tested.
- **Calendar assignment** (`CatalogWeekSkeletonCalendarMaterializer`, the real production day-assignment engine): a real Pattern-B (zero-`KEY_SESSION`) week receives a valid, deterministic date assignment with no default day substituted, no session dropped — the real regression proof for defects 1-2 above.
- **Zero-delta**: `TEN_K__4D__BEGINNER`/`TEN_K__3D__INTERMEDIATE` resolve `WeeklyPatternRoles/PatternPeriodWeeks == null`; their real structural skeleton and calendar assignment are unaffected (4 slots, exactly 1 `KEY_SESSION`, unchanged).

Full `RunningApp.IntegrationTests` regression (post-fix): **4045 total, 4043 passed, exactly the 2 durable pre-existing baseline failures** (`Sw09ExplicitZeroReadinessEndToEndTests`, `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks:13)`), **zero new regressions** — verified via TRX (`gen12_full.trx`), not console summary. `PlanCatalog.Tests`: 1510/1510. Debug and Release builds both clean (0 errors). `git diff --check` clean on every changed source file.

## 8. Explicit constraints — confirmed respected

- No public routing/gate change anywhere — `V1CatalogPilotIdentityPolicy` untouched; both new candidates are loaded exclusively via the internal dry-run gate, never reachable through public HTTP.
- No already-`PUBLICLY_ACTIVE` frequency affected — confirmed via the explicit zero-delta test plus the additive-only design (every new field defaults to `null`/absent for every pre-GEN.12 candidate).
- No new product/numeric authority beyond what `GEN.11` approved — every numeric value here is a direct implementation of an already-frozen `GEN.11` decision; the one real implementation defect class found (three `keyCount>=0` generalizations) required no new decision, only relaxing an unnecessary floor the existing formula didn't need.
- The one genuine architecture question found (§6) was disclosed and left for dedicated follow-up, not patched around with an invented rule.

## 9. Governance

`PHASE_LEDGER.md` row appended, `MASTER_ROADMAP.md` updated to reflect Beginner×2D/Intermediate×2D as `CORE_STRUCTURALLY_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL` (not yet `IMPLEMENTED_AND_DARK_VERIFIED` in the full sense `GEN.9` achieved for Advanced — full volume/Runway/LongHorizon closure remains). Public gates remain closed for both new candidates — no public activation in this phase.

**Explicit statement of what remains** (per this phase's own required output): a dedicated `ProgressionStageAllocator` lane/week-eligibility design-and-implementation phase (closing §6), then full volume/long-run plan dark verification, then Preparation Runway, then LongHorizon, then — only after all three horizons are genuinely dark-verified for both levels — the separate public-activation phase this engagement's own established `GEN.9`→`GEN.10` sequencing pattern requires. Not scheduled as a Phase ID here.
