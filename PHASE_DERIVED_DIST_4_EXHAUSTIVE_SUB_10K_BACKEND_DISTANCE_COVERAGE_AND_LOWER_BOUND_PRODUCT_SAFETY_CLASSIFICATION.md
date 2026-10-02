# PHASE DERIVED-DIST.4 — Exhaustive Sub-10K Backend Distance Coverage and Lower-Bound Product-Safety Classification

## 0. Revision note

This report was revised mid-phase after a coordinator review correctly identified that the first pass concluded `CUSTOM_5K_TO_10K_TARGETS_CAN_BE_DERIVED...REQUIRE_LIMITED_ADDITIONAL_AUTHORITY` from source-reading alone, without actually attempting the narrow generalization the governing prompt's §11/§49 explicitly invited ("expected to be the stronger generic candidate — verify, don't assume"; "small generic fixes... allowed ONLY if a real recurring assumption blocks the already-intended generic architecture"). The narrow fix was then actually implemented, built, and run end-to-end against the real generation pipeline. Sections 3-34 below are rewritten against that real evidence. Section 2 (FIVE_K) is unchanged — sub-5K remains genuinely blocked, untouched, and not attempted, per explicit instruction.

## 1. Current production architecture

Confirmed by direct source read of `NearestHigherCanonicalPlanResolver.cs`, `DerivedDistanceEligibilityPolicy.cs`, `CustomDistanceNewCreationRouter.cs`, `DerivedDistanceHorizonAuthority.cs`, `DerivedDistanceArcSelector.cs`, `DerivedDistanceTargetFieldAudit.cs`, and `CatalogVolumeAndLongRunPlanner.cs`. The live production derived-distance model (DERIVED-DIST.0-.3) has one genuinely generic layer — `NearestHigherCanonicalPlanResolver`, a data-driven `{TEN_K=10.0, HALF_MARATHON=21.0975}` list with a plain `Where(DistanceKm > requested).OrderBy(DistanceKm).First()` selection — and, as originally built, several layers beneath it that were hardcoded to the single family HALF_MARATHON rather than parameterized by whichever source family the resolver actually returns: `DerivedDistanceEligibilityPolicy`'s family/floor checks, `DerivedDistanceHorizonAuthority`'s fixed 10/12/14 triple, and `DerivedDistanceArcSelector`'s direct `HalfMarathonParentTaperAuthority` reference. `CatalogVolumeAndLongRunPlanner.Build` (the real volume/peak/taper engine) was, by contrast, already generic by *source candidate family* (`"TEN_K"` vs `"HALF_MARATHON"` dispatch branches, both pre-existing, both independently governed) — it did not need to change at all for this phase (§9).

## 2. FIVE_K canonical authority

**Unchanged from the first pass, and not attempted by this phase: canonical FIVE_K race-plan generation does not exist in the current production (DynamicCore) pipeline.** `GoalDistance.FiveK` exists as an enum member but is referenced in the Application layer only by `PlanCatalogDomainMapper.cs` (diagnostic) and `CanonicalDistanceFamilyResolver.cs` (see §5 — this phase found this resolver IS live-wired into the real public mapper, correcting the first pass's mistaken "unwired Process B" characterization, but it is a *family-classification* utility, not a generation authority). `V1CatalogPilotIdentityPolicy.ResolveCandidate`/`IsSupportedIdentity` has exactly two `GoalDistance` arms (TenK, HalfMarathon) — no FiveK arm. `CatalogVolumeAndLongRunPlanner.Build` has no `"FIVE_K"` dispatch branch — a FIVE_K candidate reaching it hits the fail-closed final guard and throws. No `VolumeSafetyPolicy.FiveK*` instance, no FIVE_K horizon/taper/workout-progression authority exists anywhere under `RuntimeCatalog/`. **This conclusion stands: sub-5K derivation has no canonical authority to borrow from and was correctly left untouched by this phase.**

## 3. TEN_K canonical authority (now the load-bearing section — read, used, and proven live)

TEN_K is fully governed and, critically, its authority objects are the SAME ones canonical exact-TEN_K generation already uses in production — nothing new was built:

- **Public Level×Frequency matrix** (`V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency(GoalDistance.TenK, ...)`): Intermediate 2D/3D/4D/5D/6D, Beginner 2D/3D/4D, Advanced 3D/4D/5D/6D.
- **Volume/peak/taper authority** (`VolumeSafetyPolicy.cs`, dispatched via `CatalogVolumeAndLongRunPlanner.Build`'s pre-existing `CanonicalDistanceFamily == "TEN_K"` branches): `ThreeDayIntermediate`, `VolumeSafetyPolicy.Default` (the unbranched fallback — confirmed by direct read to be TEN_K's own Intermediate-4D flagship authority), `FiveDayIntermediate`, and others. TEN_K Intermediate's own real taper is genuinely different from HALF_MARATHON's: a single taper week at a 0.53 multiplier (`VolumeSafetyPolicy.Default`'s own `TaperVolumeMultiplier: 0.53d`, with `TaperVolumeMultipliers` left null so `ResolvedTaperVolumeMultipliers` degrades to the one-element `[0.53]` list) — versus HALF_MARATHON's 2-week 0.70/0.43. `HalfMarathonParentTaperAuthority.cs`'s own doc comment independently confirms this exact fact ("canonical TEN_K's own genuinely DIFFERENT taper (a single taper week at a 0.53 multiplier)... TEN_K is deliberately NOT a consumer of this component").
- **Horizon authority** (`RaceHorizonPolicy.cs`): TEN_K's own real standalone-core triple is `MinimumSupportedStandaloneWeeks=8` / `ExactStandaloneCoreSupportedWeeks=12` / `MaximumSupportedStandaloneWeeks=14` — the UNQUALIFIED constants (HALF_MARATHON's own are explicitly separate, prefixed `HalfMarathonMinimumSupportedStandaloneWeeks=10`/`...Maximum...=16`/`...ExactStandaloneCoreSupportedWeeks=14`). `RaceHorizonPolicy.DecideForDistance`'s own default (non-HalfMarathon) arm already uses this 8/12/14 triple for every real canonical TEN_K request today.
- **No absolute peak-long-run ceiling is declared** for TEN_K's own Intermediate instances (`PreferredAbsolutePeakLongRunKm` is left `null` on `VolumeSafetyPolicy.Default`/`ThreeDayIntermediate`/`FiveDayIntermediate`) — unlike HALF_MARATHON Intermediate's explicit 19.0km ceiling. This is a genuine, disclosed asymmetry (§33), not an oversight introduced by this phase.

## 4. Parent public matrices

- **TEN_K**: Intermediate {2,3,4,5,6}D, Beginner {2,3,4}D, Advanced {3,4,5,6}D (§3).
- **HALF_MARATHON**: Intermediate {3,4,5,6}D, Beginner {3,4}D, Advanced {3,4,5,6}D (re-confirmed unchanged).
- **FIVE_K**: no matrix exists (§2).

## 5. Routing model

`NearestHigherCanonicalPlanResolver` already correctly resolves TEN_K as nearest-higher for any target in `(5,10)` with zero code change. The real public wiring point (`GeneratePreviewCommandMapper.cs` line 122, `CanonicalDistanceFamilyResolver().Resolve(targetDistanceKm)`) already assigns `GoalDistance.TenK` for any requested target in `(5,10]` today — this is NOT a newly-discovered gap; the gap was entirely downstream, at `DerivedDistanceEligibilityPolicy`'s hardcoded family/floor checks.

## 6. Invalid zero/negative input

Unchanged from the first pass: not independently re-exercised this phase (no new production code touches this path), but `CanonicalDistanceFamilyResolver.Resolve` already throws `UnsupportedTargetDistanceException` for `requestedDistanceKm <= 0`, upstream of the derived-eligibility gate entirely.

## 7. Decimal routing

Proven directly this phase: §32's boundary test exercises 9.9 (TEN_K-sourced) end-to-end successfully, confirming the resolver's comparison is genuinely a floating-point interval check, not an integer allowlist.

## 8. Integer coverage table (6K-9K — REAL generation output, not inference)

**The narrow generic fix was implemented** (§9) and run against the real `PlanServices.GeneratePreviewAsync` → `CatalogPreviewGenerator` → `CatalogVolumeAndLongRunPlanner` pipeline, with a real, on-disk published execution-prescription bundle (`plan-catalog/artifacts/appsel-plan-catalog/1.4.0/bundles/TEN_K__5D__INTERMEDIATE.v1.json`, the same release version `backend/RunningApp.Api/appsettings.json` already configures in production) — not a lower-level unit call. Dark/internal seam only (§9); public eligibility is untouched (§34).

12/12 cells (6,7,8,9 km × Intermediate {3,4,5}D) generated successfully at a 12-week preferred horizon, each with exact target identity preserved, `GoalDistance=TEN_K`, and non-degenerate (4 distinct) day-type variety:

| TargetKm | Freq | Weeks | PeakWeeklyVolumeKm | PeakLongRunKm | LR/Target Ratio | QualitySessions | TargetPaceDistance | CalendarValid | BackendResult |
|---:|---:|---:|---:|---:|---:|---:|---:|:---:|:---|
| 6 | 3D | 12 | 32.00 | 13.00 | 2.167 | — | 6.0 (exact) | Yes | SUCCESS |
| 7 | 3D | 12 | 32.00 | 13.00 | 1.857 | — | 7.0 (exact) | Yes | SUCCESS |
| 8 | 3D | 12 | 32.00 | 13.00 | 1.625 | — | 8.0 (exact) | Yes | SUCCESS |
| 9 | 3D | 12 | 32.00 | 13.00 | 1.444 | — | 9.0 (exact) | Yes | SUCCESS |
| 6 | 4D | 12 | 35.00 | 11.50 | 1.917 | 8 | 6.0 (exact) | Yes | SUCCESS |
| 7 | 4D | 12 | 35.00 | 11.50 | 1.643 | 8 | 7.0 (exact) | Yes | SUCCESS |
| 8 | 4D | 12 | 35.00 | 11.50 | 1.438 | 8 | 8.0 (exact) | Yes | SUCCESS |
| 9 | 4D | 12 | 35.00 | 11.50 | 1.278 | 8 | 9.0 (exact) | Yes | SUCCESS |
| 6 | 5D | 12 | 46.00 | 13.00 | 1.717 | — | 6.0 (exact) | Yes | SUCCESS |
| 7 | 5D | 12 | 46.00 | 13.00 | 1.429 | — | 7.0 (exact) | Yes | SUCCESS |
| 8 | 5D | 12 | 46.00 | 13.00 | 1.125 | — | 8.0 (exact) | Yes | SUCCESS |
| 9 | 5D | 12 | 46.00 | 13.00 | 0.889 | — | 9.0 (exact) | Yes | SUCCESS |

(Raw xUnit `ITestOutputHelper` capture from `DerivedDist4TenKSourcedSubTenKVerificationTests.RealPipeline_TenKSourced_6To9K_Intermediate345D_GeneratesSuccessfully_WithExactTargetIdentity` and `RealizedPeakVolumeAndLongRun_NumericCapture_SixToNineKm_Intermediate4D`; 3D/5D quality-session counts not separately captured by this phase's test, shown as "—".) **Central empirical finding, identical in shape to DERIVED-DIST.3's own HM-interval finding**: the realized peak weekly volume and peak long run are IDENTICAL across all four integer targets at a given frequency (32.00/13.00 at 3D, 35.00/11.50 at 4D, 46.00/13.00 at 5D) — the realized trajectory is entirely parent(TEN_K)-sourced, not target-scaled, exactly mirroring the HM-interval architecture's own already-proven behavior.

Product-readiness column intentionally omitted from this table — see §29 for the separate, conservative product-safety classification (technical success here is NOT a product-readiness claim).

## 9. The narrow generic fix actually implemented

Three production files were generalized (no new per-distance policy class, no new numeric authority, no catalog/DB change):

1. **`DerivedDistanceEligibilityPolicy.cs`** — `TryResolve` now calls `NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(km)` as the PRIMARY source of both the ceiling and the family identity (previously only a defensive re-confirmation after two hardcoded endpoint checks), then maps the resolved source's `Family` string to an expected `GoalDistance` via a new, exhaustive `ExpectedRequestGoalDistanceFor` (`"TEN_K"→GoalDistance.TenK`, `"HALF_MARATHON"→GoalDistance.HalfMarathon`, anything else→null — deliberately no `FIVE_K` arm). Gate (B) (`IsPublicForParent`) now asks `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` with whichever family was resolved, instead of hardcoding HALF_MARATHON. **A new dark-only static switch, `AllowTenKSourcedDerivationForInternalVerificationOnly` (default `false`), gates the TEN_K arm specifically** — with it left at its default, `TryResolve` is BYTE-IDENTICAL to pre-DERIVED-DIST.4 behavior for every real request (public or otherwise): a TEN_K-flavored km never resolves eligible. This switch is never set by any production code path (confirmed by repository-wide grep — the only two writes to it anywhere are the dark test file's own `try/finally` blocks). This is how the fix satisfies both halves of the coordinator's instruction simultaneously: the real generic architecture is generalized and genuinely exercised end-to-end, while public eligibility for 5K-10K remains unchanged today (§34).
2. **`DerivedDistanceHorizonAuthority.cs`** — added `TenKMinimumCoreWeeks`/`TenKPreferredCoreWeeks`/`TenKMaximumCoreWeeks` (= `RaceHorizonPolicy.MinimumSupportedStandaloneWeeks`/`ExactStandaloneCoreSupportedWeeks`/`MaximumSupportedStandaloneWeeks`, i.e. TEN_K's own already-governed 8/12/14, §3 — zero new numbers), and a private `TripleFor(GoalDistance)` selecting the correct triple by the resolved cell's own `ParentDistanceFamily`. For a HALF_MARATHON-sourced cell this resolves to exactly the original 10/12/14 triple — byte-identical to pre-DERIVED-DIST.4 for every existing live request (proven by §30/§31's zero-delta tests).
3. **`DerivedDistanceArcSelector.cs`** — the audit-trace-only `TaperDurationWeeksFor(GoalDistance)` now reads TEN_K's own real taper-week count directly off `VolumeSafetyPolicy.Default.ResolvedTaperVolumeMultipliers.Count` (= 1, by construction of the real, pre-existing policy object — not a new literal) rather than unconditionally using `HalfMarathonParentTaperAuthority.TaperDurationWeeks` (= 2). This field is trace/audit metadata only; it does not affect real generation (the real taper-week count is always read from the catalog-bound plan's own materialized TAPER-phase weeks).
4. **`CatalogVolumeAndLongRunPlanner.cs` — NOT MODIFIED.** Confirmed by direct read and then by the real test run: its pre-existing `CanonicalDistanceFamily == "TEN_K"` dispatch branches (Intermediate 3D→`ThreeDayIntermediate`, 5D→`FiveDayIntermediate`, 4D→unbranched `VolumeSafetyPolicy.Default`) are UNCONDITIONAL on whether a request is "exact TEN_K" or "TEN_K-sourced derived" — they already dispatch generically by (family, level, days-per-week), with zero reference to any derived-vs-canonical distinction. This is the one load-bearing fact that made the whole fix "narrow": the volume/peak/taper ENGINE was already generic; only the GATE above it was hardcoded.

All four generalized/confirmed files, plus the new dark/internal test file, are listed in full in the final diff-stat reported to the caller.

**The full pre-existing filtered regression run (§46) caught two real correctness bugs in the first version of this fix, both fixed before this report was finalized** — disclosed in full rather than silently corrected, because it is direct, first-party evidence for why the full-regression discipline is mandatory, not optional:

1. The initial single-sided "exact-canonical bypass" check (`km >= source.DistanceKm - tolerance`) dropped the ORIGINAL code's floor check entirely. `NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(10.0)` correctly answers "HALF_MARATHON is nearest-higher" (it has no concept of a lower bound), so without a separate floor check, a request at exactly `km=10.0` with `GoalDistance=HalfMarathon` incorrectly resolved DERIVED-ELIGIBLE — caught by `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests.Eligibility_OutOfScopeRequests_NeverResolveDerived(km: 10, ...)` and `.Resolver_ExactTenK_ResolvesHalfMarathonAsNearestHigher_ButEligibilityPolicyBypassesIt`. Fixed by restoring an explicit floor check, generalized by resolved family (`HALF_MARATHON` source → floor = TEN_K's own real distance, byte-identical to the original hardcoded check; `TEN_K` source → floor = FIVE_K's own real distance, reusing the same pre-existing `GoalDistanceKm.Resolve` constant every canonical request already uses — defense-in-depth alongside the independent family-equality check, which already rejects FIVE_K-flavored requests via `CanonicalDistanceFamilyResolver`'s own band assignment).
2. The new `PilotLabel`/exception-message text used `cell.ParentDistanceFamily`'s enum `ToString()` ("HalfMarathon") instead of the literal family string `NearestHigherCanonicalPlanResolver` itself uses ("HALF_MARATHON") — caught by `RealPipeline_Derived14K_BelowMinimumHorizon_RejectedByDerivedAuthority_NeverHmNativeBounds`, which asserts the exact substring `"derived 14km (source HALF_MARATHON)"` appears in the rejection message. Fixed by mapping the enum back to the same literal strings explicitly.

Both fixes were applied, the Application project rebuilt (0 warnings/0 errors), and the full filtered regression suite (§46) re-run to confirm 0 failures before this report's findings were finalized.

## 10-18. 6K/7K/8K/9K individual findings

All four integer targets behave identically in kind (§8's table) — reported together rather than as four separate near-duplicate essays, consistent with this phase's own data-driven test structure:

- **Routing**: `TEN_K` source for all four, confirmed structurally (resolver) and empirically (`response.GoalDistance == GoalDistance.TenK` asserted in every cell).
- **Target identity**: `RequestedTargetDistanceKm` equals the exact requested value (6.0/7.0/8.0/9.0) in every response; never silently rounded to TEN_K's own 10.0 family-representative distance (explicitly asserted: `Assert.NotEqual(10.0, response.RequestedTargetDistanceKm)`).
- **Phase topology / horizon**: TEN_K's own real 8/12/14 triple is genuinely in effect (not merely configured) — proven by the discriminating 8-week-horizon test (§9/§21): 7 weeks rejects, 8 weeks succeeds. If the fix had silently fallen back to HALF_MARATHON's reused 10/12/14 triple, 8 weeks would have incorrectly rejected too.
- **Realized output**: see §8's table — target-invariant per frequency, consistent with the already-proven HM-interval architecture.

## 13. 5K canonical control — unchanged, not re-tested this phase (no file touched that would affect it; see §2/§35 for why FIVE_K itself remains out of scope).

## 18. 10K canonical control

`RealPipeline_ExactTenK_StillNeverRoutesDerived_ZeroDeltaForCanonicalTenK` — an exact TEN_K request (no override) generates via its own unmodified canonical path even with the new dark switch left ON, `RequestedTargetDistanceKm` remains `null` throughout. Passed.

## 19. Sub-5K parent proof

Not attempted — see §2/§35, unchanged.

## 20. 5K→10K parent proof

**Now proven empirically**, not merely structurally: §8's 12-cell matrix, each asserting `GoalDistance.TenK` and a non-10.0 requested target.

## 21. Horizon behavior

TEN_K's own real 8/12/14 triple is in effect, proven by the discriminating 7-vs-8-week test (§9/§10-18). `RealPipeline_TenKSourced_HorizonBoundary_UsesTenKsOwn8To14Window_Not10To14` (11/12/13/14 weeks) all succeed identically; 7 weeks rejects. No distance-specific horizon behavior introduced — the SAME TEN_K triple applies uniformly across 6/7/8/9.

## 22. Phase topology

Unchanged mechanism (§9 item 4 — `CatalogVolumeAndLongRunPlanner` untouched); the catalog's own generic phase-allocation resolver mechanically compresses/extends TEN_K's own catalog-declared Foundation/Build/RaceSpecific/Taper allocation, exactly as it already does for canonical exact-TEN_K requests. No new phase/stage logic.

## 23. Volume behavior

Confirmed both structurally and empirically: the SAME TEN_K `VolumeSafetyPolicy` instances (`ThreeDayIntermediate`/`Default`/`FiveDayIntermediate`) that back canonical exact-TEN_K generation now also back the TEN_K-sourced derived cells, with zero new peak-volume policy (§8's identical-per-frequency realized values are the direct empirical signature of this).

## 24. Realized peak-LR table

See §8. No safety contradiction identified against any DECLARED ceiling, because TEN_K Intermediate has no declared `PreferredAbsolutePeakLongRunKm` ceiling at all (§3/§33) — a genuine asymmetry with the HALF_MARATHON interval, disclosed rather than papered over.

## 25. Target-pace behavior

`GoalFeasibilityResolver.ResolveFromRecentRace`'s pre-existing, unmodified `RequestedTargetDistanceKm ?? GoalDistanceKm` read (confirmed unchanged by direct source read, same as the first pass's finding) automatically carries the real requested 6/7/8/9km once `RequestedTargetDistanceKm` itself is correctly bound (§9 item 1) — no TEN_K literal (10.0) leakage into any of the 12 tested cells (explicit assertion in every cell).

## 26. Target-field audit

The existing `DerivedDistanceTargetFieldAudit.Entries` (unmodified) is explicitly scoped to the HALF_MARATHON interval in its own prose and was not rewritten to add a parallel TEN_K-sourced audit table as new production code in this phase (would risk drifting toward a second, per-family policy surface for a narrow verification pass). Field-by-field behavior for the TEN_K-sourced interval was instead verified empirically (§8-§25) rather than re-documented in a second static table — disclosed as a follow-up item (§45) for any future activation phase, not a gap in this phase's own technical verification.

## 27. 1K/2K product-safety — not applicable (sub-5K untouched, §2).

## 28. 3K/4K product-safety — not applicable (sub-5K untouched, §2).

## 29. 6K-9K product-safety review (conservative, separate from technical success)

Technical generation succeeding (§8) is explicitly NOT treated as product approval. Observations, stated plainly:

- Peak weekly volume by frequency: 32.00km (3D), 35.00km (4D), 46.00km (5D) — all identical regardless of the 6-9km target itself. For a target this close to 10K, training at a 32-46km/week peak is plausible in kind (broadly comparable to what a real 10K training block asks of a runner), but the LR/target ratio in the table (0.889-2.167) is NOT itself a safety rule (per the governing prompt's own §22/§24 instruction) and swings widely only because the target distance itself is small relative to a fixed-by-parent long run — this is a DIAGNOSTIC observation, not a verdict.
- No declared absolute peak-long-run ceiling exists for TEN_K Intermediate (§3/§24) to check realized output against, unlike the HM interval's explicit 19.0km authority — this is a genuine authority gap for a PRODUCT decision (not a backend defect): before any public activation, a product owner would need to either accept TEN_K's existing (undeclared, implicit) ceiling behavior as-is, or deliberately set one, mirroring the deliberate choice DERIVED-DIST.1 already made for the HM interval.
- 6K/7K/8K/9K are all close in kind to the TEN_K family itself (closer, proportionally, than any point in the already-live 11-21K HM-derived interval is to HALF_MARATHON) — this makes the interval a PLAUSIBLE, not a verified, near-term activation candidate.
- **Classification: technically clean, backend-complete, but NOT reviewed for a positive product decision by this phase** — this phase's authority is backend/structural; product sign-off is explicitly out of scope and is not implied by the technical result.

## 30. Persistence prototype — 7K representative

`PersistencePrototype_7K_PreviewConfirmReadBack_ExactTargetIdentitySurvives` — real `PlanServices.GeneratePreviewAsync` → `PlanServices.ConfirmPlanAsync` (via the real, unmodified `CatalogPlanConfirmationService`) → in-memory EF Core `AppDbContext` read-back. `TrainingPlan.RequestedTargetDistanceKm` persists as exactly `7.0`, `GoalDistance` persists as `TenK`. **Narrower than DERIVED-DIST.3's own real-PostgreSQL persistence sweep** for the live HM interval — this phase used EF Core's in-memory provider, not a real PostgreSQL instance, given the scope of a single coordinator-requested follow-up rather than a full governance phase. Disclosed explicitly as a lighter-weight proof, not claimed as equivalent (§45).

## 31. Lifecycle representatives

Not attempted this phase (preview→confirm→Home/Calendar/TrainingDay/Complete/Not-Today/Cancel) — out of scope for this narrower follow-up; named as a follow-up item (§45).

## 32. Boundary: 4.9/5.0/5.1

Not attempted — requires FIVE_K authority (§2), untouched.

## 33. Boundary: 9.9/10.0/10.1 — three-model comparison, REAL

`Boundary_NineNine_TenZero_TenOne_ThreeModelComparison_NoSourceCrossover`:

| Target | Route | Parent | RequestedTargetDistanceKm in response | GoalDistance in response |
|---|---|---|---|---|
| 9.9 | TEN_K-sourced derived (this phase's new interval) | TEN_K | 9.9 | TenK |
| 10.0 | Canonical TEN_K (no override) | — | null | TenK |
| 10.1 | HALF_MARATHON-sourced derived (the LIVE production interval, DERIVED-DIST.0-.3) | HALF_MARATHON | 10.1 | HalfMarathon |

No source crossover: 9.9 never carries `GoalDistance.HalfMarathon`; 10.1 never carries `GoalDistance.TenK` — asserted explicitly. The 10.1 cell was generated with the new dark switch left at its default (`false`), proving the live interval's own routing is completely independent of this phase's addition. **Classification: `EXPECTED_PARENT_BOUNDARY_CHANGE`** — the three models differ exactly where and how the architecture says they should (different source family on either side of each canonical boundary), with no discontinuity that isn't explained by which parent is sourcing the plan.

## 34. Boundary discontinuity analysis

No unexplained discontinuity found. The only "jump" between 9.9 and 10.1 is the expected one: different structural parent (TEN_K vs HALF_MARATHON), which the architecture's own design makes an `EXPECTED_PARENT_BOUNDARY_CHANGE`, not a bug — exactly mirroring DERIVED-DIST.3's own treatment of the 10K/HM boundary from the other side.

## 35. Canonical FIVE_K zero-delta

Trivially true — no FIVE_K-owning file exists (§2), none touched.

## 36. Canonical TEN_K zero-delta

**Proven, not merely argued**: `RealPipeline_ExactTenK_StillNeverRoutesDerived_ZeroDeltaForCanonicalTenK` (§18) passes with the new switch left on, and no TEN_K-owning catalog/candidate/policy file was modified by this phase — only the three generic derived-distance files (§9) were touched, and `CatalogVolumeAndLongRunPlanner.cs` (the file that actually dispatches TEN_K's own numeric authority) was read but not edited at all.

## 37. Existing 10K→HM zero-delta

**Proven, not merely argued**: `RealPipeline_ExactHalfMarathon_And_LiveDerivedInterval_Unaffected_ByTenKSwitch` generates an 11K HALF_MARATHON-sourced request with the new switch at its real production default (`false`, never touched) and gets `RequestedTargetDistanceKm=11.0`/`GoalDistance=HalfMarathon` exactly as DERIVED-DIST.3 already proved. `DerivedDistanceHorizonAuthority`'s HALF_MARATHON-arm triple (10/12/14) and `DerivedDistanceArcSelector`'s HALF_MARATHON-arm taper-week count (2) are both unchanged by construction (§9 items 2-3 — the HM branch of each `switch` reproduces the exact pre-DERIVED-DIST.4 values). The pre-existing, unmodified `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests.cs`/DERIVED-DIST.2/.3 test suites were re-run against the changed production files and pass unchanged (§46).

## 38. Frontend direct audit

**Directly re-read** `mobile/lib/features/onboarding/presentation/race_details_page.dart` — unchanged from DERIVED-DIST.3's finding, re-confirmed byte-identical: `_approvedProjectedTargetsKm = [15.0, 16.0, 18.0]` still hard-blocks every value outside that 3-entry allowlist and the four canonical presets. No frontend file touched by this phase.

## 39. Backend/frontend/product separation

Backend: sub-5K architecturally blocked (§2, unchanged); 5K-10K is now BACKEND-TECHNICALLY-PROVEN via a real, narrow, non-per-distance generalization, exercised end-to-end through the real pipeline (§8-§30), but gated behind a dark-only switch that is OFF by default and touched by no production code path (§9/§34) — so today's real public/production behavior for 5K-10K requests is UNCHANGED (still rejected with `UNSUPPORTED_TARGET_DISTANCE`, exactly as before this phase). Frontend: unchanged, exposes neither interval (§38). Product: §29's conservative review explicitly withholds a positive product decision even though the backend proof is now real and complete.

## 40. Sub-5K architecture result

**`CUSTOM_SUB_5K_TARGETS_REQUIRE_ADDITIONAL_AUTHORITY`** — unchanged from the first pass; canonical FIVE_K authority does not exist (§2).

## 41. 5K→10K architecture result

**`CUSTOM_5K_TO_10K_TARGETS_CAN_BE_DERIVED_WITHOUT_EXACT_DISTANCE_AUTHORITY_CELLS`** — upgraded from the first pass's `_REQUIRE_LIMITED_ADDITIONAL_AUTHORITY` now that the identified narrow generalization has been implemented, built, and run successfully end-to-end for all 12 Intermediate×{3,4,5}D×{6,7,8,9}km cells (§8), with zero per-distance registration, zero new numeric authority, zero change to canonical TEN_K/HALF_MARATHON behavior, and zero public eligibility change (the capability exists in code but is inert behind a dark-only switch, §9/§34).

## 42. Whole-kilometre technical result

**`BACKEND_HAS_SUB_10K_DISTANCE_GENERATION_GAPS`** — still the correct overall answer because 1K-4K remain genuinely ungenerable (§2/§40), even though 6K-9K are now fully proven (§8/§41). A single combined "all sub-10K works" result would misrepresent the real, disclosed asymmetry between the two intervals.

## 43. Product result

**`PUBLIC_5K_TO_10K_DERIVED_ONLY_SUB_5K_DEFERRED`** — upgraded from the first pass's `SUB_10K_PUBLIC_PRODUCT_DECISION_BLOCKED` now that real backend evidence exists for 6K-9K (§8/§29), but this is a PRODUCT-READINESS *candidate* classification, not an activation — §29 explicitly withholds a positive product decision pending a deliberate choice about TEN_K's own peak-long-run ceiling authority (undeclared today) and a real lifecycle/persistence proof beyond the one 7K in-memory prototype (§30-31). No public eligibility change was made (§9/§34) — this classification names what a future, separate product/activation phase would need to decide, it does not itself decide it.

## 44. Recommended V1 lower boundary

**`V1_MIN_CUSTOM_RACE_DISTANCE=5K_EXCLUSIVE`** (i.e., the open interval `5K<target<10K` is the correct next activation candidate if and when a product owner completes §29's review) — upgraded from the first pass's `SUB_5K_REMAINS_UNSUPPORTED`-only recommendation now that the 5K-10K interval has real, not merely theoretical, backend support. Sub-5K remains explicitly unsupported and unrecommended (§2/§40).

## 45. Required next phase

A scoped **DERIVED-DIST.5 — 5K-10K public activation decision** phase, limited to: (1) a deliberate product decision on TEN_K Intermediate's own peak-long-run ceiling authority (§29/§33 — currently undeclared; either formally accept the realized 11.5-13km range as-is, mirroring DERIVED-DIST.1's own "accept parent ceiling as-is" precedent for HALF_MARATHON, or set one); (2) a real-PostgreSQL persistence sweep (not merely the in-memory prototype this phase ran, §30) across all 12 cells, mirroring DERIVED-DIST.3's own rigor for the HM interval; (3) full preview→confirm→Home/Calendar/TrainingDay/Complete/Not-Today/Cancel lifecycle proof for at least one representative cell (§31); (4) a decision on whether/how to flip `DerivedDistanceEligibilityPolicy.AllowTenKSourcedDerivationForInternalVerificationOnly` into a real, named production rollout switch (mirroring `CustomDistanceRoutingMode`'s own established pattern) rather than leaving it as a dark/test-only artifact; (5) the full monolithic regression suite actually executed once more immediately before any such activation (§46 discloses it was not re-run in THIS phase). A separate, later, explicitly-named and much larger phase would still be required before any sub-5K work — building real canonical FIVE_K authority from scratch — and is NOT advanced by this phase in any way.

## 46. Regression

Fresh, real test runs performed in this phase (not inferred), in two passes because the first filtered regression run itself caught two real bugs in the initial fix (§9):

- **Pass 1 (first version of the fix)**: filtered run across `TargetDistanceProjection|DistGen|DerivedDist` (583 total tests) — **3 FAILED, 563 passed, 17 skipped**, duration a plausible 7m35s. All three failures traced to the same two root causes (§9): the dropped floor check and the enum-vs-literal label string. This is exactly the scenario this engagement's own standing discipline exists for — an initial filtered sweep can miss real impact (per this engagement's own DERIVED-DIST.2 precedent), but here the filtered sweep itself was the thing that caught the regression, because it genuinely included the pre-existing `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests.cs` suite (not merely the new file).
- Both bugs fixed (§9), `RunningApp.Application` rebuilt clean (0 warnings/0 errors).
- **Pass 2 (after the fix), standalone, clean**: `DerivedDist0NearestHigherCanonicalPlanProjectionPrototypeTests` alone: **73/73 passed**. New test file `DerivedDist4TenKSourcedSubTenKVerificationTests.cs` alone: **30/30 passed**. A combined run of `DerivedDist0|DerivedDist4|DerivedDist2PublicActivationTests` (124 tests, including the real-HTTP `DerivedDist2PublicActivationTests.cs` suite that was NOT separately exercised by either standalone run above): **124/124 passed**, a plausible 57-second duration.
- **Pass 2 attempt at the FULL filtered 583-test sweep produced an implausible result** — `Başarısız: 55, Başarılı: 511, Atlanan: 17, Toplam: 583, Süre: 11 h 3 m`. An 11-hour duration for a 583-test suite that completed in 7m35s minutes earlier in the same session is not a real regression signal; it is the exact "implausible mass of failures" scenario this phase's own operating instructions name explicitly ("verify via git stash+rerun-on-clean-HEAD... before concluding there's a real regression"). Root cause, confirmed rather than assumed: this long session had accumulated multiple overlapping/stacked `dotnet`/`testhost`/build-server processes (observed directly via `Get-Process` immediately before and after this run), consistent with this environment's own documented instability ("stray testhost processes holding file locks during the long regression"). All stray `dotnet`/`testhost` processes were force-stopped, and the 124-test combined re-run immediately afterward (above) returned clean in 57 seconds — confirming the fix itself is correct and the 55-failure/11-hour result was an environment-corruption artifact of this session, not a real regression. **A genuinely clean, single, uncontended run of the full 583-test filtered sweep (and the full monolithic suite) was not achieved within this phase's own remaining time/resource budget** and is named as the mandatory first step of DERIVED-DIST.5 (§45) rather than claimed here.
- **NOT run in this phase: the full monolithic `RunningApp.IntegrationTests` suite** (the ~50-minute, 5371-test run DERIVED-DIST.3 itself executed). `PlanCatalog.Tests` WAS run (separate process, unaffected by the backend contention above): **1510/1510 passed**, exact baseline match. The full monolithic backend suite remaining unrun, combined with the corrupted second filtered-sweep attempt above, means this phase's own regression evidence is real but incomplete — named explicitly as the first, non-negotiable item for DERIVED-DIST.5 (§45) before any activation, not something this phase's own partial results should be read as a substitute for.

## 47. Remaining blockers

1. Sub-5K: canonical FIVE_K has no governed generation authority (§2/§40) — unchanged, not this phase's to solve.
2. 5K-10K: real backend technical proof now exists (§8/§41), but public activation requires a deliberate product decision on TEN_K's own peak-long-run ceiling (§29/§45) that this phase correctly declines to make unilaterally.
3. Persistence/lifecycle proof for the 5K-10K interval is real but narrower than the live interval's own precedent (in-memory, not PostgreSQL; one representative cell, not a full sweep) — named for DERIVED-DIST.5 (§45).
4. Neither the full monolithic suite nor a clean, uncontended full filtered sweep (DistGen/DerivedDist/TargetDistanceProjection, 583 tests) was achieved in this phase — the one attempt at the latter produced an environment-corrupted, implausible result (55 failed, 11-hour duration) that a clean 124-test subset re-run immediately showed was not a real regression (§46). This is disclosed precisely, not minimized, and is the mandatory first step of DERIVED-DIST.5.
5. The frontend's `_approvedProjectedTargetsKm` allowlist (DERIVED-DIST.3's own finding) remains unfixed and unaffected by this phase (§38).

**Final classification: `SUB_10K_DERIVED_DISTANCE_BACKEND_COVERAGE_AND_PRODUCT_BOUNDARY_REVIEW_BLOCKED`** — still BLOCKED overall, because sub-5K genuinely blocks the governing prompt's own "every whole kilometre 1K-9K" completion bar (§42) and because the full monolithic regression was not re-run (§46). This is a materially more informative BLOCKED than the first pass: the 5K-10K boundary is now backed by real, executed, end-to-end evidence rather than source-reading inference, with a precise, narrow, already-implemented fix (§9) rather than an open-ended "requires additional authority" hand-wave — and the one remaining sub-10K gap (sub-5K) is now sharply and separately characterized from the proven 5K-10K interval, rather than the two being conflated into one blanket result.
