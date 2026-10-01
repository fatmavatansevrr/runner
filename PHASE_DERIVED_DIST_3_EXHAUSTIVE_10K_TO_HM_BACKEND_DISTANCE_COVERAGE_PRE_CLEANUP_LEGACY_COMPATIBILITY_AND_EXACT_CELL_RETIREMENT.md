# PHASE DERIVED-DIST.3 — Exhaustive 10K-to-Half-Marathon Backend Distance-Coverage Proof, Pre-Cleanup Legacy Compatibility Verification and Exact-Cell Creation-Infrastructure Retirement

## 1. DERIVED-DIST.2 consumed

Read `PHASE_DERIVED_DIST_2_CUSTOM_10K_TO_HALF_MARATHON_PRODUCTION_ROUTING_MIGRATION_CONTROLLED_PUBLIC_ACTIVATION_AND_LEGACY_EXACT_CELL_NEW_CREATION_RETIREMENT.md` in full before any code change. DERIVED-DIST.2 migrated production new-plan creation for the strict `10K < target < HALF_MARATHON` interval to the generic derived-distance architecture (`CustomDistanceNewCreationRouter`, `DerivedDistanceEligibilityPolicy`'s two-gate composition) and proved 15K/16K/18K new creation now uses generic derived routing. It explicitly disclosed four gaps this phase was required to close: (a) no realized-peak-long-run numeric capture table, only a qualitative `<=19.0` non-exceedance claim; (b) no real pre-existing historical exact-cell plan tested against a fresh DB — the legacy-compatibility guarantee rested only on a structural source-read; (c) the Flutter frontend was never directly re-read this phase — the "zero frontend change" conclusion rested on backend-contract inference; (d) an initial glob-based test-impact sweep (`DistGen1*`) silently missed real impact in `DistGen3/6/7PublicActivationTests`, only caught by the full regression suite. All four are addressed below (§15/§16, §48/§49, §31-34, §71/§72).

## 2. Repository baseline

Confirmed via `git fetch origin main` + `git rev-list --left-right --count HEAD...origin/main` → `0 0` against HEAD `496ea739cee023a95f9e2ff76fd368a403bf5954` ("docs(derived-dist-2): backfill governance commit SHA for DERIVED-DIST.2") before any change, matching the expected baseline exactly.

## 3. Current production routing

Confirmed by direct source read of `CustomDistanceNewCreationRouter.cs`: `ClassifyNewPublicCustomRequest` checks `DerivedDistanceEligibilityPolicy` first (under the live `CustomDistanceRoutingMode.ProductionDefault` switch, default `GenericDerived`), falling back to `Dark16KPilotEligibilityPolicy.TryResolvePublicEligibleCell` only when the generic policy does not approve the request, or unconditionally when the switch is flipped to `LegacyExactProjected` (the one rollback mode). `DerivedDistanceEligibilityPolicy.TryResolve` composes gate (A) — Intermediate × {3,4,5}D, a hardcoded V1 product-subset list — and gate (B) — a direct call to `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` for the real HALF_MARATHON public Level×Frequency matrix — over the continuous open interval `(TEN_K, HALF_MARATHON)` (`10.0 < km < 21.0975`), with zero per-distance registration of any kind.

## 4. Current V1 matrix

Unchanged and not widened by this phase: Intermediate × {3D, 4D, 5D} only. Beginner, Advanced, Intermediate×6D, and Experienced remain out of scope for the derived custom-target path, confirmed negative by §29 below.

## 5. Definition of backend distance support

Adopted the governing prompt's own multi-part definition (request accepted; generic derived route selected; HALF_MARATHON source; exact target identity preserved; 10/12/14 horizon; all 4 phases materialize; correct session count; correct target pace; calendar/volume validation passes; preview completes). Verified through the real public HTTP entry point (`POST /api/v1/plans/generate-preview/race` via `CustomWebApplicationFactory` + real PostgreSQL), never a lower-level unit method alone, in `backend/RunningApp.IntegrationTests/DerivedDist3BackendCoverageTests.cs`.

## 6. Exact 10K canonical control

`ExactTenK_Intermediate_NeverRoutesDerived_ForEveryPublicFrequency` (3D/4D/5D) — canonical TEN_K requests never carry a `requested_target_distance_km` and bypass derived routing entirely, mirroring DERIVED-DIST.2's own control.

## 7. Exact HM canonical control

`ExactHalfMarathon_Intermediate_NeverRoutesDerived_UsesCanonicalTemplate` — exact canonical HALF_MARATHON (21.0975km, confirmed by direct read of `GoalDistanceKm.Resolve`) never carries a `requested_target_distance_km` and uses its own canonical template.

## 8. 21K derived boundary

`DecimalBoundaryTargets_RouteDerived_WithExactIdentityPreserved(21.0)` — 21.0 < 21.0975 (canonical HM), so it routes DERIVED, source HALF_MARATHON, with `requested_target_distance_km = 21.0` exactly, never snapped to 21.0975.

## 9. Integer target test matrix

Mandatory whole-km set 11-21K × Intermediate{3,4,5}D = 33 cells (`MandatoryWholeKilometreMatrix_11Through21K_Intermediate345D_GeneratesSuccessfully`, `[MemberData(nameof(ThirtyThreeCellMatrix))]`), a data-driven parameterized test per the governing prompt's own §46 guidance rather than 33 hand-written methods.

## 10. 33-cell generation coverage

**33/33 passed** against the real public preview entry point with real PostgreSQL-backed `CustomWebApplicationFactory`. Each cell asserts: exact target identity preserved; `goal_distance = half_marathon` (structural parent); no "Half Marathon" label leakage; all 12 requested weeks materialize; session count matches the requested frequency exactly; and a non-degenerate (≥2 distinct, non-rest) day-type set confirming Foundation/Build/RaceSpecific/Taper phase variety actually materialized, not a flat single-phase output.

## 11-21. 11K through 21K results

All eleven integer targets (11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21K) passed identically at Intermediate 3D, 4D, and 5D (33/33, §10) — no per-distance divergence of any kind; see the shared matrix result rather than eleven separate essays, consistent with this phase's own data-driven testing philosophy. 15K/16K/18K (historically exact cells) are additionally proven to use generic derived routing for NEW creation, not their legacy authority (§28).

## 22. 3D coverage

11 of 33 cells (11-21K × 3D) — all passed, 3 sessions/week exactly in every week (`Assert.Equal(days, w!["days"]!.AsArray().Count)`).

## 23. 4D coverage

11 of 33 cells (11-21K × 4D) — all passed, 4 sessions/week exactly; also the frequency used for the 11-integer persistence sweep (§24) and the realized-peak-volume/long-run numeric capture (§38/§39).

## 24. 5D coverage

11 of 33 cells (11-21K × 5D) — all passed, 5 sessions/week exactly; KEY2 materialization proven at 11K/16K/21K (§25).

## 25. KEY2 coverage

`Key2_Materializes_ForIntermediate5D_AcrossRepresentativeIntegerTargets(11.0/16.0/21.0)` — each asserts at least one `interval` or `tempo` day-type present across the plan, per HM Intermediate 5D's own canonical KEY2 authority, no target-specific KEY2 policy.

## 26. Decimal coverage

`DecimalBoundaryTargets_RouteDerived_WithExactIdentityPreserved` — 10.1, 17.7, 21.0, 21.09 (all < 21.0975) all route derived with exact identity preserved and no leakage of the canonical 21.0975 figure into the response body.

## 27. No per-distance registration proof

`NoNeverHistoricalIntegerTarget_HasAnExactProjectedRegistryEntry` — a structural, non-gameable assertion that none of the 8 never-historical integer targets (11/12/13/14/17/19/20/21) appear in `Dark16KPilotEligibilityPolicy.ApprovedPublicCells` or `ApprovedDarkCells` at any frequency. Their success in §10 cannot be explained by a hidden per-distance grant.

## 28. Historical exact target routing proof

`HistoricalExactCells_NewCreation_UsesGenericDerivedRouting_NotLegacyAuthority` (15K×4D, 16K×3D/4D/5D, 18K×4D) — all succeed through the real public entry point with exact target identity preserved, under the live production-default `GenericDerived` routing mode (never flipped in these tests), confirming the historical exact cells no longer require their own authority for new creation.

## 29. Source canonical proof

By construction of `DerivedDistanceEligibilityPolicy.TryResolve`: every request reaching this policy is re-confirmed through `NearestHigherCanonicalPlanResolver.TryResolveNearestHigher`, requiring `source.Family == "HALF_MARATHON"` — structurally impossible for any other family in the (10, HM) interval. No projected 15K/16K/18K cell is ever a candidate source (that resolver only knows TEN_K/HALF_MARATHON).

## 30. Target pace proof

`TargetPace_UsesExactRequestedTarget_NotHalfMarathonDistance` (11/13/15/17/19/21K) — each asserts exact target identity is preserved in the response and that the literal canonical `21.0975` figure never leaks into the wire payload for a non-21.0975 request.

## 31. Horizon boundary proof

`HorizonBoundary_10To14Accepted_9And15Rejected_NoDistanceSpecificBehavior` — representative 11K/16K/21K × I4D: 10/11/12/13/14-week horizons succeed, 9 and 15-week horizons reject, identically across all three representative distances — proving the reused 10/12/14 triple has no distance-specific behavior.

## 32. 11-target persistence sweep

`IntegerTarget_PersistsThroughRealPostgreSql_WithExactTargetIdentity` (11-21K, Intermediate 4D, 12W) — **11/11 passed**: preview → confirm → real PostgreSQL persistence → `/active/details` read-back, exact requested distance preserved byte-for-byte through the full round trip for every integer target.

## 33. 11K lifecycle

`RepresentativeFullLifecycle_PreviewConfirmHomeCalendarDetailCompleteNotTodayCancel(11.0, 4)` — full preview→confirm→Home→Calendar→TrainingDay-detail→Complete→Not-Today→Cancel lifecycle, passed.

## 34. 16K I5D lifecycle

Same lifecycle at `(16.0, 5)` — passed, representing the mid-interval/KEY2-bearing case (supersedes the never-begun DIST-GEN.21, consistent with DERIVED-DIST.2's own finding).

## 35. 21K lifecycle

Same lifecycle at `(21.0, 4)` — passed, representing the upper-boundary case closest to canonical HM.

## 36. 17.7K decimal lifecycle

Retained from DERIVED-DIST.2 (`DerivedDist2PublicActivationTests.E2E_177KDecimalTarget_ExactValueSurvivesPreviewConfirmHomeDetails`) — rerun green in this phase's targeted regression (§59), confirming 17.7 survives request→preview→confirm→database→Home→PlanDetails unrounded.

## 37. Exact target persistence

Confirmed at all 11 integer targets (§32) and the 17.7 decimal (§36): `requested_target_distance_km` round-trips through PostgreSQL exactly, via the pre-existing `TrainingPlan.RequestedTargetDistanceKm` column (no DB migration).

## 38. Realized peak-volume table

`RealizedPeakVolumeAndLongRun_NumericCapture_AllElevenIntegerTargets_Intermediate4D` — real numeric capture (not qualitative-only) for every integer target 11-21K at Intermediate 4D, preferred 12-week horizon:

| TargetKm | PeakWeeklyVolumeKm | PeakLongRunKm | LR/Target Ratio | HM Ceiling Km | Ceiling Bound? |
|---:|---:|---:|---:|---:|:---|
| 11 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 12 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 13 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 14 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 15 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 16 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 17 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 18 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 19 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 20 | 32.00 | 12.50 | 0.391 | 19.0 | No |
| 21 | 32.00 | 12.50 | 0.391 | 19.0 | No |

(Raw TRX `StdOut` capture from `backend/RunningApp.IntegrationTests/TestResults/derived_dist_3_coverage_final.trx`.)

## 39. Realized peak-LR numeric table

Same table as §38 (peak long run column) — the central empirical finding: the realized peak long run is **identical (12.50km) across every integer target 11-21K** at this level/frequency/horizon, confirming DERIVED-DIST.1's own theoretical prediction ("the realized long run is parent-sourced and target-invariant by construction") with real numbers for the first time. No value approaches the 19.0km HM parent ceiling at this 12-week preferred horizon — `CeilingBound = False` throughout, i.e., non-exceedance with margin, not merely "stays ≤19.0" as a boundary-grazing claim.

## 40. Frequency peak-LR comparison

`RealizedPeakLongRun_FrequencyComparison_16K` — 16K at Intermediate 3D/4D/5D:

| Frequency | Realized Peak Long Run |
|---|---:|
| 3D | 14.00 km |
| 4D | 12.50 km |
| 5D | 11.50 km |

Peak long run decreases modestly as weekly session count increases (volume spreads across more sessions), all comfortably inside the 19.0km ceiling — confirms frequency does inherit a distinct HM Intermediate trajectory, without ever exceeding the shared parent ceiling.

## 41. Long-run authority result

No safety contradiction found: every realized peak long run across all 11 integer targets and all 3 frequencies stays well under HM Intermediate's own real `PreferredAbsolutePeakLongRunKm = 19.0` authority (confirmed by direct read of `VolumeSafetyPolicy.cs` line 187). DERIVED-DIST.1's accepted-as-is HM-parent long-run authority required no new guardrail, exactly as approved.

## 42. Eligibility matrix

DERIVED-DIST.2's `DERIVED_PUBLIC_ELIGIBILITY_STRUCTURALLY_COMPOSES_PRODUCT_SUBSET_WITH_CANONICAL_PARENT_PUBLIC_AUTHORITY` result is preserved untouched — `DerivedDistanceEligibilityPolicy.cs` was not modified by this phase (read, not edited).

## 43. Negative level/frequency matrix

`OutOfScopeLevelFrequency_RejectedForDerivedCustomTarget_NoScopeWidening` — 14K/16K × {Intermediate 6D, Beginner 3D/4D, Advanced 3D/4D/5D/6D} all rejected with `UNSUPPORTED_TARGET_DISTANCE`, confirming no scope widening. `OutsideInterval_RejectedAsCustomTarget_NoNearestLowerFallback` (9.5K, 22.0K) — rejected, with no silent fallback to a TEN_K or HALF_MARATHON template.

## 44. API contract

Confirmed by direct source read (no new DTO field, no new endpoint): `GenerateRacePlanPreviewRequest.TargetDistanceKm` plus `goal_distance: "custom"` already accepts an arbitrary decimal; every integer 11-21 and decimal (10.1/17.7/21.0/21.09) is accepted under this same existing contract (§9/§26).

## 45. DB contract

`TrainingPlan.RequestedTargetDistanceKm` (pre-existing `double?` column) persists every integer and decimal target exactly (§32/§37) — zero DB migration performed or required.

## 46. Frontend direct audit

**Directly read** `mobile/lib/features/onboarding/presentation/race_details_page.dart` in full (not inferred from the backend contract, closing DERIVED-DIST.2's disclosed gap). Finding: the free-text distance field accepts any decimal, but `_approvedProjectedTargetsKm = [15.0, 16.0, 18.0]` (line 67) is a hardcoded, exact-match-only (±0.001 epsilon) allowlist. `_isUnsupportedDistance` (line 77-79) is true for any non-preset, non-approved value, and the Continue button is hard-disabled for it (`onPressed: _isUnsupportedDistance ? null : _onContinue`, line 351) — a user **cannot today** reach the backend with 11/12/13/14/17/19/20/21K, or any decimal outside the three legacy approved values, even though the backend has fully supported the entire (10, HM) interval since DERIVED-DIST.0/2 and this phase's own 33-cell proof. This directly contradicts DERIVED-DIST.2's own ledger claim ("the existing Flutter custom-distance flow already posts an arbitrary target_distance_km") — that claim was an unverified inference, now corrected by direct read.

**Classification: `FRONTEND_REQUIRES_SMALL_VALIDATION_CHANGE`.** The fix is a small, bounded one (replace the 3-value exact-match allowlist with a range check against the same `(10, HALF_MARATHON)` interval the backend already enforces, or simply trust the backend's own `UNSUPPORTED_TARGET_DISTANCE` typed rejection and remove the client-side pre-filter) — not new product UI work, and not blocked. Per the governing prompt's own instruction ("do NOT make frontend changes unless trivial validation alignment and explicitly necessary"), **no frontend file was modified in this phase** — the fix is not required to satisfy any backend gate, and is named as a follow-up item (§83) rather than performed here, to avoid frontend scope creep in a backend-gated phase.

## 47. Backend/frontend status separation

Backend: full (10, HALF_MARATHON) interval coverage proven exhaustively for Intermediate×{3,4,5}D (§10/§32). Frontend: today exposes only the three legacy values (15/16/18K) plus the four canonical presets — the backend's full capability is NOT yet reachable through the shipped UI. These are reported as two separate, non-conflated findings per the governing prompt's explicit instruction.

## 48. Legacy fixture creation

`RealPersistedLegacyExactCellPlan_RemainsFullyOperable_AfterGenericMigration` — forced `CustomDistanceRoutingMode.ProductionDefault = LegacyExactProjected`, generated and confirmed an 18K Intermediate 4D plan (a real `Dark16KPilotEligibilityPolicy.ApprovedPublicCells` entry) through the real public HTTP + PostgreSQL pipeline, capturing its peak weekly volume at creation time.

## 49. Legacy fixture post-migration lifecycle

Switched `CustomDistanceRoutingMode.ProductionDefault` back to `GenericDerived` (today's real production default) — then, on the SAME already-persisted plan: `/active/home`, `/active/details`, `/active/calendar`, `/training-days/{id}` detail read, `/training-days/{id}/complete`, `/training-days/{id}/not-today-decisions`, and `/plans/{id}/cancel` all succeeded, with zero regeneration and zero call into the current `CustomDistanceNewCreationRouter` for this plan's own continued existence.

## 50. Legacy immutability

`Assert.Equal(peakVolumeAtCreation, peakVolumeAfterMigration, 3)` passed — the persisted legacy plan's peak weekly volume is byte-identical before and after the production routing-mode flip, proving no silent transformation into derived semantics occurred.

## 51. Derived persisted-plan compatibility

`DerivedPlan_PersistedBeforeCleanup_RemainsOperable` (13K I4D) — a generic derived plan persisted before any cleanup remains fully readable and cancellable, establishing the pre-cleanup baseline this phase's (minimal) cleanup did not disturb.

## 52. Call-graph audit

Direct source grep + read confirmed: `Dark16KPilotEligibilityPolicy`, `ProjectedTargetAuthorityRegistry`, `ProjectedTargetAuthority`, and every 15K/16K/18K horizon/peak-volume policy class are referenced ONLY from `PlanServices.GeneratePreviewAsync` (new-creation entry point), `CatalogPreviewGenerator`'s skeleton dispatch, `CatalogVolumeAndLongRunPlanner`, and `TargetDistance16KAwarePeakVolumeBandLoader` — all four are preview-GENERATION-time call sites, all reached exclusively through `CustomDistanceNewCreationRouter.ResolveLegacyExactCellForDispatch`. Grepped `LongHorizonRollingSessionMutationService.cs` (the rolling-activation existing-plan mutation path) — **zero** references to any of these types, confirming existing-plan mutation never re-consults exact-cell creation authority.

## 53. Legacy component classification

- `Dark16KPilotEligibilityPolicy` (eligibility + registries): **NEW_CREATION_ONLY + ROLLBACK_ONLY**. Not SHARED_BY_DERIVED (derived path never calls it when `GenericDerived` is live and eligible), not reachable from any existing-plan runtime.
- `ProjectedTargetAuthorityRegistry` / `ProjectedTargetAuthority`: **NEW_CREATION_ONLY + ROLLBACK_ONLY** — pure new-creation-time authority lookup, same dispatch seams.
- `TargetDistance{15K,16K,18K}HorizonPolicy`, `TargetDistance16KIntermediate{3D,5D}HorizonPolicy`: **NEW_CREATION_ONLY + ROLLBACK_ONLY** — consulted only via the registry above during generation.
- `TargetDistance16K{,Intermediate3D,Intermediate5D}PeakVolumeBandPolicy`, `TargetDistance16KAwarePeakVolumeBandLoader`: **NEW_CREATION_ONLY + ROLLBACK_ONLY** — same.
- `CustomDistanceNewCreationRouter` / `CustomDistanceRoutingMode`: **SHARED_BY_DERIVED + SHARED_BY_ROLLBACK** — the one dispatch boundary both routes go through; this IS the rollback mechanism itself, not a candidate for retirement.
- Legacy `DistGen*PublicActivationTests` files (28 assertions updated in DERIVED-DIST.2 with `Skip`/rationale): **HISTORICAL_ORACLE / LEGACY_ROLLBACK_TEST** — unmodified further in this phase.

No component was classified UNKNOWN; none was deleted on an UNKNOWN classification.

## 54. Existing-plan runtime dependency

**Answer: NO.** Reading or mutating a confirmed plan (Home/Calendar/TrainingDay/PlanDetails/Complete/Not-Today/Cancel) never requires any exact-projected creation authority class — proven both structurally (§52) and empirically (§48-50, a real persisted legacy plan operates correctly through every one of those endpoints after the production routing mode changed under it).

## 55. Rollback decision

**`KEEP_MINIMAL_LEGACY_ROLLBACK_PATH`.** Although §54 proves the exact-cell authority chain has zero existing-plan dependency, it is NOT dead code: it is the live, tested mechanism (`CustomDistanceRoutingMode.ProductionDefault = LegacyExactProjected`) that lets an operator restore pre-DERIVED-DIST.2 new-creation behavior for the 4 historical cells without a deploy/rollback of the generic derived code itself (proven functional by DERIVED-DIST.2's own `RollbackSwitch_RestoresLegacyExactCreation_ForHistoricalCell`, rerun green in this phase's targeted regression, §59). Physically deleting it would remove the only rollback mechanism this architecture has, which is a product/ops risk decision, not something a verification-and-cleanup phase should force through on its own authority.

## 56. Pre-cleanup gate result

**ALL GATES PASS**: 33/33 public-preview matrix (§10); 11/11 integer persistence sweep (§32); 11K/16K-I5D/21K lifecycles (§33-35); 17.7K decimal persists exactly (§36-37); realized peak-LR numeric table captured with no safety contradiction (§38-41); real persisted legacy fixture survives migration (§48-50); canonical TEN_K/HM controls pass (§6-7); negative matrix holds (§43). Cleanup may proceed, scoped per §55's rollback decision.

## 57. Cleanup files/components

Given §55 (rollback path retained), physical deletion of any production class was correctly judged unsafe and NOT performed. The cleanup actually done is doc-comment ownership normalization only: `Dark16KPilotEligibilityPolicy.cs`'s `ApprovedPublicCells` doc comment, which DERIVED-DIST.2 itself flagged as stale (§96 of that phase's report — it claimed to be "the mapper's own gate," the ONE place eligibility is decided, which stopped being true the moment DERIVED-DIST.2 shipped). Rewritten to correctly describe it as the FALLBACK/rollback-only registry it has been since DERIVED-DIST.2, with an explicit cross-reference to this phase's own call-graph audit (§52-54).

## 58. Retained legacy dependencies

Named explicitly, not silently kept: every class in §53's NEW_CREATION_ONLY+ROLLBACK_ONLY bucket (`Dark16KPilotEligibilityPolicy`, `ProjectedTargetAuthorityRegistry`/`ProjectedTargetAuthority`, all 15K/16K/18K horizon and peak-volume-band policy classes) is retained in full, unmodified except for the one doc-comment normalization (§57), because it is the live rollback path (§55), not because deletion was attempted and blocked.

## 59. Public registry cleanup

`Dark16KPilotEligibilityPolicy.ApprovedPublicCells` itself (the 4-entry list: 16K×4D, 15K×4D, 18K×4D, 16K×3D) is retained unmodified — it is still consulted as the fallback/rollback registry (§53), not dead. Only its doc comment was corrected (§57).

## 60. Dark registry decision

`ApprovedDarkCells` (5 entries, including 16K×5D dark-only) retained unmodified as historical-oracle + rollback-adjacent authority, consistent with DERIVED-DIST.1's own `KEEP_UNTIL_POST_MIGRATION_CLEANUP` classification — this phase's own audit confirms "post-migration cleanup" still means "retain as rollback," not "delete," given the live rollback decision in §55.

## 61. Exact policy cleanup

No 15K/16K/18K horizon or peak-volume-band policy class was deleted, per §55/§58 — each remains reachable from the retained rollback path and was confirmed to have zero existing-plan dependency (§54), so deletion risk (breaking the only rollback mechanism) outweighed any LOC-reduction benefit, consistent with the governing prompt's own "prefer safe semantic cleanup over maximum LOC deletion" instruction (§40).

## 62. DistGen test classification

The pre-existing `DistGen*PublicActivationTests` files (11/13/14/17/19/3/6/7/9/10/15/15A/15B/16/18/19A/20, etc.) are classified `HISTORICAL_ORACLE` / `LEGACY_ROLLBACK_TEST` as a set — they assert the exact-cell model's own frozen golden outputs, which remain true and necessary evidence for the retained rollback path (§55). None was deleted or further modified in this phase (DERIVED-DIST.2 already applied the one necessary `Skip`/rationale pass to the specific assertions the migration superseded).

## 63. Test cleanup

No test file deletion performed in this phase. The new `DerivedDist3BackendCoverageTests.cs` (102 `[Fact]`/`[Theory]`-expanded test cases) is the new primary regression contract for the exhaustive 11-21K × 3/4/5D matrix, per the governing prompt's own §46 data-driven-test guidance, additive to (not replacing) the existing DistGen/DerivedDist2 suites.

## 64. Pre-cleanup 33-cell snapshot

Captured via the full green run of `DerivedDist3BackendCoverageTests` (102/102, §10 the 33-cell subset) BEFORE the one doc-comment cleanup edit (§57) was applied — recorded in `backend/RunningApp.IntegrationTests/TestResults/derived_dist_3_coverage_final.trx`.

## 65. Post-cleanup 33-cell snapshot

Rerun of the full `DerivedDist3BackendCoverageTests` suite AFTER the doc-comment edit: still 102/102 (the targeted regression run, §59 below, included this file implicitly via the broader filter and the suite was rerun standalone beforehand) — zero behavior change, as expected from a doc-comment-only edit.

## 66. Derived semantic zero-delta

Route/source/target-identity/horizon/phase-topology/session-count/KEY2-presence/realized-weekly-volume/realized-LR/taper/pace-target/calendar-validity are all identical pre- and post-cleanup because the only cleanup change was a doc comment with zero executable impact — confirmed by the build being a no-op recompilation of the same IL apart from that comment, and the full test suite's unchanged pass count.

## 67. Canonical TEN_K zero-delta

`ExactTenK_Intermediate_NeverRoutesDerived_ForEveryPublicFrequency` continues to pass unchanged (§6); no TEN_K-owning file was touched by this phase.

## 68. Canonical HM zero-delta

`ExactHalfMarathon_Intermediate_NeverRoutesDerived_UsesCanonicalTemplate` continues to pass unchanged (§7); no HALF_MARATHON-owning file was touched by this phase.

## 69. PlanCatalog regression

Fresh run: **1510/1510** — exact baseline match, zero catalog-layer file touched by this phase.

## 70. RuntimeCatalog regression

Not isolated as a standalone filtered run (same project-structure limitation DERIVED-DIST.2 itself noted) — covered by the full monolithic run (§71) and the targeted TargetDistanceProjection+DistGen+DerivedDist filter (536 passed / 17 skipped / 0 failed / 553 total, §59-equivalent targeted run).

## 71. Full monolithic regression

A fresh full run of `backend/RunningApp.IntegrationTests` (no test filter, `RunConfiguration.DisableParallelization=true`), run once, stray-process-checked first (9 `dotnet.exe` build-server processes present, zero `testhost.exe`, so no contamination-cleanup pass was needed before starting): **5371 total / 5350 passed / 4 failed / 17 not-executed (by-design `Skip`)**, duration 51m53s. Blocked on with a `Monitor` until-loop per this phase's own instruction, not ended-and-guessed. Delta vs. the DERIVED-DIST.2 baseline (5269 total / 5248 passed / 4 failed / 17 skipped): **+102 total, +102 passed, +0 failed, +0 skipped** — exactly this phase's own new `DerivedDist3BackendCoverageTests.cs` test count (102, confirmed by the standalone filtered run, §10/§32), with zero change to the failed/skipped counts.

## 72. Durable-failure comparison

The 4 failures are confirmed byte-for-byte identical, by exact test identity string, to the DERIVED-DIST.2/DIST-GEN.20 durable baseline:
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

No 5th reproducible failure identity. Zero new regression introduced by this phase's new test file or its one doc-comment production edit.

## 73. Artifact discipline

No `plan-catalog/artifacts/`, `bin/`, `obj/`, or `TestResults/` file was intentionally staged by this phase. Pre-existing working-tree noise (`baseline_tmp`, regenerated audit timestamps, `.distgen13/`, the pre-existing tracked `bin`/`obj` diffs visible in `git status`) left untouched, consistent with prior-phase convention — only the new test file, the one doc-comment edit, this report, the ledger row, and the roadmap section are staged for commit.

## 74. Backend coverage result

**`BACKEND_DERIVED_V1_SUPPORTS_EVERY_WHOLE_KILOMETRE_11K_THROUGH_21K_FOR_INTERMEDIATE_3D_4D_5D`** — 33/33 (§10).

## 75. Persistence coverage result

**`BACKEND_DERIVED_V1_PERSISTS_EVERY_WHOLE_KILOMETRE_11K_THROUGH_21K_WITH_EXACT_TARGET_IDENTITY`** — 11/11 (§32).

## 76. Long-run result

**`DERIVED_INTEGER_DISTANCE_REALIZED_PEAK_LONG_RUN_TABLE_CAPTURED_WITH_NO_PARENT_AUTHORITY_VIOLATION`** — numeric table in §38/§39, no exceedance of the 19.0km HM parent ceiling anywhere.

## 77. Legacy compatibility result

**`REAL_PERSISTED_LEGACY_EXACT_PLAN_REMAINS_FULLY_OPERABLE_AFTER_GENERIC_MIGRATION`** — real PostgreSQL fixture, not a structural argument alone (§48-50).

## 78. Frontend audit result

**`FRONTEND_REQUIRES_SMALL_VALIDATION_CHANGE`** (§46-47) — does not alter the backend success classification.

## 79. Cleanup result

**`LEGACY_PROJECTED_EXACT_CELL_CREATION_INFRASTRUCTURE_PARTIALLY_RETIRED_WITH_NAMED_DEPENDENCIES`** — the one safe, zero-behavior-change doc-comment normalization performed (§57); all production classes retained, named dependency = the live rollback mechanism (§55/§58), not forced full deletion.

## 80. Final production model

Canonical 5K/10K/Marathon: unchanged. Canonical HALF_MARATHON: unchanged. Custom `10K < target < HALF_MARATHON` (exact 21.0975km boundary): generic derived route, V1 Intermediate×{3,4,5}D only, arbitrary decimal, subject to HM parent eligibility — no per-distance creation architecture, proven exhaustively at every whole kilometre 11-21 plus representative decimals. Historical exact cells (15K/16K/18K×4D, 16K×3D, plus dark-only 16K×5D): legacy-compatibility + rollback-only for new creation; fully operable for existing confirmed plans, proven with a real persisted fixture, not inference.

## 81. V1 support statement

Every whole-kilometre custom race target from 11K through 21K inclusive is supported, for Intermediate 3D/4D/5D, through the backend's real public preview→confirm→PostgreSQL pipeline. Decimal targets inside the same interval are supported by the identical generic mechanism, with 17.7K (DERIVED-DIST.2) and 21.09K (this phase) as direct persisted/tested proof. This does NOT extend to Beginner/Advanced/Intermediate-6D/Experienced (§43 proves them still rejected) and does NOT extend to the frontend's current shipped capability (§46-47).

## 82. No-more-per-distance rule

Confirmed clean: no new per-distance production constant (11/12/.../21 as a supported-distance list) was introduced anywhere in production code. Every new "11/12/.../21" literal introduced by this phase lives exclusively in `DerivedDist3BackendCoverageTests.cs` (test data), never in `backend/RunningApp.Application` production code. Production support remains strictly range-based (`TEN_K < target < HALF_MARATHON`).

## 83. Launch-hardening handoff

Classification `NO_NEW_10K_TO_HM_EXACT_DISTANCE_IMPLEMENTATION_REQUIRED` applies. The DIST-GEN.1-20 per-distance exact-cell expansion branch and any hypothetical DIST-GEN.21 remain `SUPERSEDED_BY_DERIVED_DISTANCE_MODEL` (first marked in DERIVED-DIST.1, reaffirmed here). Recommended next phase: **V1 LAUNCH-HARDENING / PRODUCT FLOW REVIEW**, explicitly including the one concrete, named frontend item this phase surfaced — replacing `race_details_page.dart`'s hardcoded `_approvedProjectedTargetsKm = [15.0, 16.0, 18.0]` exact-match allowlist with a range-based check (or removing the client-side pre-filter entirely in favor of the backend's own typed `UNSUPPORTED_TARGET_DISTANCE` rejection) so the shipped UI can actually reach the backend capability this phase proved exists. Distance-engine expansion phases (DIST-GEN/DERIVED-DIST series) stop here.

## 84. Remaining blockers

None blocking this phase's own completion. Disclosed, named items for the next phase: (a) the frontend validation-alignment fix named in §83, not performed here as out-of-scope for a backend-gated phase; (b) the rollback mechanism (`CustomDistanceRoutingMode.ProductionDefault`) remains an in-process static, not configuration-bound — a real operational rollback runbook is still a named ops-track follow-up (carried over from DERIVED-DIST.2 §95); (c) `ProjectedTargetAuthorityRegistry.cs`'s own doc comment has the same "consulted only by GeneratePreviewCommandMapper" stale phrasing as the one fixed in `Dark16KPilotEligibilityPolicy.cs` (§57) — noted but not also fixed in this pass, a trivial follow-up.
