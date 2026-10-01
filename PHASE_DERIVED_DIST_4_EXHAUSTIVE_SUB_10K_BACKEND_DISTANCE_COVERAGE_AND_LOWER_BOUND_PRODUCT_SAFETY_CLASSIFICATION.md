# PHASE DERIVED-DIST.4 — Exhaustive Sub-10K Backend Distance Coverage and Lower-Bound Product-Safety Classification

## 1. Current production architecture

Confirmed by direct source read of `NearestHigherCanonicalPlanResolver.cs`, `DerivedDistanceEligibilityPolicy.cs`, `CustomDistanceNewCreationRouter.cs`, `DerivedDistanceHorizonAuthority.cs`, `DerivedDistanceArcSelector.cs`, `DerivedDistanceTargetFieldAudit.cs`, and `CatalogVolumeAndLongRunPlanner.cs`. The live production derived-distance model is the DERIVED-DIST.0-.3 "nearest-higher-canonical-plan" mechanism, but it is **not as generic as its own doc comments describe for the purpose of this phase's hypothesis**. The resolver (`NearestHigherCanonicalPlanResolver.ReadyCanonicalPlans`) is a data-driven two-entry list (`TEN_K=10.0`, `HALF_MARATHON=21.0975`), and selection-by-nearest-higher is genuinely generic at that one layer. But every layer below it is hardcoded to the single family HALF_MARATHON, not parameterized by whichever source family the resolver picks:

- `DerivedDistanceEligibilityPolicy.TryResolve` hard-rejects any `requestGoalDistance != GoalDistance.HalfMarathon` (line 145-148) and hard-rejects any `km <= tenKKm + tolerance` (line 133) — i.e. it structurally only ever accepts the open `(TEN_K, HALF_MARATHON)` interval, by construction, not by having tested and rejected sub-10K.
- `DerivedDistanceHorizonAuthority.TryResolve` builds its `PilotLabel` as `"derived {km}km (source HALF_MARATHON)"` literally (line 103) — not parameterized by the resolved source.
- `DerivedDistanceArcSelector.BuildTrace` reads `HalfMarathonParentTaperAuthority.TaperDurationWeeks` directly (line 70) — not a source-indexed taper authority lookup.
- `CatalogVolumeAndLongRunPlanner.Build`'s dispatch (the real volume/long-run/taper engine) has two families of explicit `CanonicalDistanceFamily =="..."` branches: one set keyed `"TEN_K"` (lines 31-96, for canonical TEN_K requests) and one set keyed `"HALF_MARATHON"` (lines 138-212, for canonical HALF_MARATHON requests AND every HM-sourced derived request, because a derived request's `Candidate.CanonicalDistanceFamily` is deliberately kept at `"HALF_MARATHON"` per `DerivedDistanceTargetFieldAudit`'s own `KeepFromParent` classification). **There is no `"FIVE_K"` branch anywhere in this file.** Any candidate that reaches this method with `CanonicalDistanceFamily` not equal to `"TEN_K"` and not covered by one of the HALF_MARATHON/Advanced/Beginner branches falls into the final guard (line 230-233) and throws `CatalogVolumeUnsupportedDistanceFamilyException`.

This is the single most important architectural fact this phase found: **the "generic nearest-higher-canonical" claim is true only for the one interval it was built and proven for (10K→HM). Extending it to a new source family is not merely flipping a range check — it requires either (a) FIVE_K already having its own fully-governed volume/horizon/taper/workout authority to fall through to (see §2), which does not exist, or (b) generalizing the dispatch to be parametric by source family, which is a real code change, not a test-only exercise.**

## 2. FIVE_K canonical authority

**Finding: canonical FIVE_K race-plan generation does not exist in the current production (DynamicCore) pipeline at all.** Evidence, all from direct source read:

- `GoalDistance.FiveK` exists as an enum member (`backend/RunningApp.Domain/Enums/GoalDistance.cs`), but a repository-wide search of the Application layer shows it is referenced in exactly two files: `PlanCatalogDomainMapper.cs` (a diagnostic/gap-analysis mapper, not the generation pipeline) and `CanonicalDistanceFamilyResolver.cs` (see §5 — a separate, "Process B" resolver with no proven wiring into live generation).
- `V1CatalogPilotIdentityPolicy.cs` — the single, centrally-owned source of truth for public Level×Frequency eligibility and candidate-key resolution for the live DynamicCore pipeline — has exactly two `GoalDistance` arms: `TenK` and `HalfMarathon`. There is no `FiveK` arm in `ResolveCandidate`, `IsSupportedIdentity`, or any switch in that file.
- `CatalogVolumeAndLongRunPlanner.cs` (the real volume/peak/taper/long-run engine) has no `"FIVE_K"` dispatch branch (§1). A FIVE_K candidate reaching this method would hit the fail-closed final guard and throw.
- `VolumeSafetyPolicy.cs` has no `FiveK*`-named instance and no `ForFiveKIntermediateDaysPerWeek`-style factory — every named instance is `TenK`-flavored (unqualified, e.g. `ThreeDayIntermediate`, `Advanced3D`) or `HalfMarathon`-flavored.
- `plan-catalog/artifacts/audits/backend-process-b-activation-review.md` (a pre-existing, undated diagnostic artifact already in the repo) independently documents, from an earlier snapshot, that backend had "zero integration with plan-catalog" and only 3 seeded `five_k/beginner` database rows through a placeholder engine. That document predates the DynamicCore/DIST-GEN/DERIVED-DIST architecture this phase actually exercised (it describes a `PlaceholderPlanGenerationEngine` that no longer matches current source) and is **not** treated as current evidence by this phase — it is cited only as corroborating, independently-authored confirmation that FIVE_K has never had a governed generation path in this codebase, at any point in its history that left a written trace.
- No `TargetDistance5K*` policy class, no `FiveKHorizonPolicy`, no `FiveKParentTaperAuthority`, and no FIVE_K workout-progression document were found anywhere under `backend/RunningApp.Application/RuntimeCatalog/`.

**Conclusion: there is no "canonical FIVE_K" for a derived sub-5K target to borrow authority from.** This is a harder blocker than "needs a generalization" — it needs an entire new per-distance-family body of governed authority (horizon, peak-volume band, taper, workout progression) that this phase is explicitly forbidden from inventing (governing prompt: NO NEW TRAINING-SCIENCE NUMBERS, NO NEW DISTANCE-SPECIFIC POLICY, NO NEW PEAK-VOLUME POLICY, NO NEW WORKOUT DEFINITION). The sub-5K half of this phase's hypothesis is therefore not testable by any narrow, generic, zero-new-policy mechanism today.

## 3. TEN_K canonical authority

By contrast, canonical TEN_K is fully governed and live. Confirmed from `V1CatalogPilotIdentityPolicy.cs` and `VolumeSafetyPolicy.cs`:

- TEN_K public Level×Frequency matrix (from `ResolveCandidate`'s TenK arm): Intermediate 2D/3D/4D/5D/6D, Beginner 2D/3D/4D, Advanced 3D/4D/5D/6D — each with its own named candidate key/version.
- TEN_K has its own named `VolumeSafetyPolicy` instances (`ThreeDayIntermediate`, `FiveDayIntermediate`, `SixDayIntermediate`, `Intermediate2D`, `ForAdvancedDaysPerWeek`, `ForBeginnerDaysPerWeek`, `Beginner2D`, `ThreeDayBeginner`, `BeginnerFourDay`, etc.) and its own `CatalogVolumeAndLongRunPlanner.Build` dispatch branches (§1), all gated on `CanonicalDistanceFamily == "TEN_K"`.
- TEN_K has its own catalog-declared `CoreCycle` (minimum/maximum weeks) referenced from `V1CatalogPilotIdentityPolicy.cs` line 158 ("the candidate's own TEN_K_MASTER-inherited CoreCycle bounds") — this phase did **not** extract TEN_K's exact Minimum/Preferred/Maximum week triple from the TEN_K_MASTER catalog document in the time available; this is disclosed as an open item rather than guessed (§45).
- There is no evidence TEN_K's own horizon triple is 10/12/14 — that triple is HALF_MARATHON's own `RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks = 10` plus the two literal constants 12/14 that `DerivedDistanceHorizonAuthority` reuses. No equivalent `TenKMinimumSupportedStandaloneWeeks` constant was found anywhere in `RaceHorizonPolicy.cs`.

**Conclusion: TEN_K is structurally capable of being a derivation source (it has a complete, independent volume/level/frequency authority), but its own horizon authority was not located/confirmed in this pass, and the derived-distance pipeline does not currently read it for anything (the pipeline is wired to HALF_MARATHON's authority specifically, not the resolved source's).**

## 4. Parent public matrices

- **TEN_K** (from §3): Intermediate {2,3,4,5,6}D, Beginner {2,3,4}D, Advanced {3,4,5,6}D.
- **HALF_MARATHON** (already established by DERIVED-DIST.1-.3, re-confirmed by this phase's read of `V1CatalogPilotIdentityPolicy.cs` lines 524-543): Intermediate {3,4,5,6}D, Beginner {3,4}D, Advanced {3,4,5,6}D.
- **FIVE_K**: no matrix exists (§2) — there is no Level×Frequency authority to enumerate.

## 5. Routing model

`NearestHigherCanonicalPlanResolver.TryResolveNearestHigher` is a pure `Where(DistanceKm > requested).OrderBy(DistanceKm).First()` over its two-entry list. Mechanically, if `FIVE_K` were ever added to that list, the resolver would correctly select `FIVE_K` for `(0,5)` and `TEN_K` for `(5,10)` with zero change to the resolver itself — confirming the resolver layer genuinely is data-driven and generic, exactly as its own doc comment claims. **The blocker is everything downstream of the resolver** (§1): `DerivedDistanceEligibilityPolicy`'s hardcoded family/floor checks, and `CatalogVolumeAndLongRunPlanner`'s HALF_MARATHON-only derived dispatch. For the `5K<target<10K` interval specifically, the resolver would already correctly pick `TEN_K` as nearest-higher **today**, with zero resolver change — only the eligibility gate and the downstream dispatch would need to stop assuming the source is always HALF_MARATHON.

## 6. Invalid zero/negative input

Not exercised through the derived pipeline itself in this phase (no code path reaches it — see §45), but `CanonicalDistanceFamilyResolver.Resolve` (the separate Process-B resolver, §2/§7) already throws `UnsupportedTargetDistanceException` for `requestedDistanceKm <= 0`, and `NearestHigherCanonicalPlanResolver.TryResolveNearestHigher(0)` would mechanically return `TEN_K` (10.0 > 0) under today's two-entry list — i.e. the resolver alone has no floor check; `DerivedDistanceEligibilityPolicy`'s own floor check (`km <= tenKKm`) is what actually rejects non-positive and low values for the live interval, and it would also reject 0/negative values if ever reached (0 ≤ 10). This phase did not add a test exercising 0K/-1K end-to-end because the request never reaches generation under current production code (frontend blocks it, §38, and no public DTO path for sub-10K custom targets exists).

## 7. Decimal routing

Same limitation as §6: the resolver is interval/decimal-based by construction (floating-point `DistanceKm > requestedTargetDistanceKm`), so 0.1/1.5/4.9/5.1/7.5/9.9 would all resolve through the identical generic comparison once `FIVE_K` is added to the ready-canonical list — this is a structural property of the existing resolver, confirmed by code read, not by a new test run in this phase.

## 8. Integer coverage table

Not captured with real numbers in this phase. See §45/§47 — exercising the real public/internal generation pipeline for 1K-9K requires either (a) a production code change to `DerivedDistanceEligibilityPolicy`/`CatalogVolumeAndLongRunPlanner` to stop hardcoding HALF_MARATHON, which this phase declined to make without the full regression capacity to validate it safely against the hard zero-delta requirement for the live 10K-HM interval, or (b) building FIVE_K's canonical authority from scratch, which is out of scope by the governing prompt's own rules. No fabricated numbers are reported here — see §47 for the disclosed gap this leaves.

## 9-18. 1K through 10K individual findings

Not independently exercised (§8). Structural reasoning only, from §1-§5:

- **1K-4K (would-be FIVE_K-derived)**: `DERIVATION_BLOCKED` — no canonical FIVE_K authority exists to derive from (§2).
- **5K (canonical)**: Not materially answerable — FIVE_K canonical itself does not exist as a generation path (§2), so "exact FIVE_K" has no canonical template to bypass to. This is a separate, more severe finding than DERIVED-DIST.3's exact-10K/exact-HM controls, which both have real canonical templates.
- **6K-9K (would-be TEN_K-derived)**: `TECHNICALLY_PROMISING_BUT_NOT_YET_TESTABLE_WITHOUT_A_CODE_CHANGE` — TEN_K's own authority exists in full (§3), and the resolver would already pick TEN_K correctly (§5), but `DerivedDistanceEligibilityPolicy` and `CatalogVolumeAndLongRunPlanner`'s derived-dispatch branch are both hardcoded to HALF_MARATHON only; no safe, narrow seam exists today to exercise a TEN_K-sourced derived cell without changing production code.
- **10K (canonical)**: Unchanged, live, fully governed (re-confirmed by this phase's source read of §1/§3, no file touched).

## 19. Sub-5K parent proof

Not provable: there is no FIVE_K parent to prove routing to (§2).

## 20. 5K→10K parent proof

Not empirically proven in this phase. Structurally argued (§5) that `NearestHigherCanonicalPlanResolver` would select TEN_K correctly for any value in `(5,10)` with zero resolver change, but this was not exercised via an actual generation call because the eligibility gate above it (§1) would reject the request before the resolver's answer could matter end-to-end.

## 21. Horizon behavior

TEN_K's own horizon triple was not located/confirmed in the time available (§3) — this phase explicitly declines to reuse HM's 10/12/14 triple for a hypothetical TEN_K-derived interval (per the governing prompt's own §18-19 instruction), and also declines to invent a new one. This is reported as an open gap, not resolved by assumption.

## 22. Phase topology

Not independently exercised. `DerivedDistanceArcSelector`'s mechanism (mechanical catalog-driven phase-allocation compression/extension) is itself source-family-agnostic in principle (it operates on whatever `BoundCatalogPlan` it's given), but its one literal HALF_MARATHON-specific line (`HalfMarathonParentTaperAuthority.TaperDurationWeeks`, §1) was not generalized or tested against a TEN_K body.

## 23. Volume behavior

Confirmed by direct source read only (§1): `CatalogVolumeAndLongRunPlanner.Build` has a complete, independent TEN_K dispatch branch, so if a TEN_K-sourced derived candidate's `CanonicalDistanceFamily` were set to `"TEN_K"` (mirroring exactly how a HM-sourced derived candidate's family is kept at `"HALF_MARATHON"`, per `DerivedDistanceTargetFieldAudit`), it would in principle reach TEN_K's own, already-governed volume/peak/taper logic with no new policy class. This was not executed end-to-end in this phase.

## 24. Realized peak-LR table

Not captured — no generation was run for any sub-10K target in this phase (§8/§45).

## 25. Target-pace behavior

Not independently tested. `GoalFeasibilityResolver.ResolveFromRecentRace` already reads `RequestedTargetDistanceKm ?? GoalDistanceKm` generically (per `DerivedDistanceTargetFieldAudit`, unchanged pre-existing code) — this mechanism is not HALF_MARATHON-specific and would plausibly behave correctly for a TEN_K-derived or FIVE_K-derived request with zero new code, but this was not exercised.

## 26. Target-field audit

The existing `DerivedDistanceTargetFieldAudit.Entries` (§ Application source) was read in full (§1 citations above). It is explicitly scoped to "HALF_MARATHON Intermediate x {3,4,5}D" and several of its entries' own `Mechanism` text literally names HALF_MARATHON-specific classes (`HalfMarathonParentTaperAuthority`, `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek`). A separate FIVE_K-derived or TEN_K-derived field audit was not built as new production code in this phase (that itself would be close to "new distance-specific policy" if done as a bespoke per-family class, and the generic alternative — parameterizing the existing audit by source family — is a real code change this phase did not make, consistent with §45's disclosed scope limitation).

## 27. 1K/2K product-safety

Not reviewable — no 1K/2K plan was ever generated (§2/§9). No realized output exists to assess for over-training/race-mismatch risk. This phase makes no claim, positive or negative, about 1K/2K product-appropriateness.

## 28. 3K/4K product-safety

Same as §27 — not reviewable, no realized output.

## 29. 6K-9K product-safety

Not reviewable from realized output (no generation was run), but structurally, 6K-9K deriving from TEN_K (an actual race distance only 1-4km below the targets, versus HM-derived 11-20K being 1-10km below a 21K parent) is, on its face, a materially closer analog pairing than the HM interval already in production — this is a plausibility observation from reading the architecture, not a verified product-safety finding, and is explicitly not a readiness claim.

## 30. Persistence prototypes

Not run. No preview was ever generated for any sub-10K target (§45).

## 31. Lifecycle representatives

Not run, for the same reason.

## 32. 4.9/5/5.1 boundary

Not run. The resolver change needed to make 4.9/5.1 reachable (adding FIVE_K to `ReadyCanonicalPlans`) was not made (§1/§45), and 5.0 (canonical FIVE_K) has no canonical template to compare against (§2).

## 33. 9.9/10/10.1 boundary

Not run for 9.9 (needs the TEN_K-derived eligibility gate, not built). 10.0 (canonical TEN_K) and 10.1 (live HM-derived, already proven by DERIVED-DIST.3) were not independently regenerated in this phase because doing so required no code change and this phase made none — their behavior is unchanged by construction (zero files touched, §35-37).

## 34. Boundary discontinuity analysis

Not performed — depends on §32/§33's unrun cells.

## 35. Canonical FIVE_K zero-delta

Trivially true: no FIVE_K-owning file exists to begin with (§2), and none was created or touched by this phase.

## 36. Canonical TEN_K zero-delta

True by construction: zero files under `VolumeSafetyPolicy.cs`, `V1CatalogPilotIdentityPolicy.cs`, or any TEN_K-owning class were modified by this phase (confirmed by `git status`/`git diff --stat` at the end of this phase, §48 of the final report-to-caller). No regression run was required to prove this because no TEN_K-reachable code path was edited.

## 37. Existing 10K→HM zero-delta

True by construction, same reasoning as §36: `DerivedDistanceEligibilityPolicy.cs`, `CustomDistanceNewCreationRouter.cs`, `NearestHigherCanonicalPlanResolver.cs`, `DerivedDistanceHorizonAuthority.cs`, `DerivedDistanceArcSelector.cs`, and `CatalogVolumeAndLongRunPlanner.cs` were all read in full but not edited. The live 10K-HM derived interval's behavior is unchanged with certainty, because nothing it depends on was touched.

## 38. Frontend direct audit

**Directly re-read** `mobile/lib/features/onboarding/presentation/race_details_page.dart` in this phase (not inferred). Finding: **unchanged from DERIVED-DIST.3.** `_approvedProjectedTargetsKm = [15.0, 16.0, 18.0]` (same line, same value) still gates `_isUnsupportedDistance`, which still hard-disables the Continue button (`onPressed: _isUnsupportedDistance ? null : _onContinue`) for any custom value outside that 3-entry allowlist or the four canonical presets. The UI cannot today reach 1K-9K or any sub-10K decimal, for the same reason it cannot reach 11K-21K (DERIVED-DIST.3's own finding, still true). This phase made no frontend change, per the governing prompt's explicit prohibition.

## 39. Backend/frontend/product separation

Backend: sub-5K is architecturally blocked (no canonical FIVE_K authority exists, §2) — this is a stronger statement than "not product ready," it is "not technically derivable by this mechanism today." 5K-10K is structurally plausible but unproven — the one identified blocker (§1) is a real, narrow, named gap, not an open-ended one, but this phase did not close it. Frontend: unchanged, exposes neither interval (§38). Product: no readiness claim is made for any sub-10K value because no backend generation was ever run to inspect (§8-§31).

## 40. Sub-5K architecture result

**`CUSTOM_SUB_5K_TARGETS_REQUIRE_ADDITIONAL_AUTHORITY`** — specifically, a canonical FIVE_K volume/horizon/taper/workout authority does not exist anywhere in the current codebase (§2); this is new per-distance-family authority, explicitly out of scope for this phase (and for any "narrow generic fix").

## 41. 5K→10K architecture result

**`CUSTOM_5K_TO_10K_TARGETS_REQUIRE_LIMITED_ADDITIONAL_AUTHORITY`** — TEN_K's own volume/level/frequency authority already exists in full (§3) and the resolver layer is already generic enough to select it (§5); the limited gap is real and named: (a) add `FIVE_K`/confirm `TEN_K` nearest-higher selection is exercised below 10, (b) generalize `DerivedDistanceEligibilityPolicy`'s hardcoded `requestGoalDistance == HalfMarathon` and `km <= tenKKm` checks to be source-family-parametric instead of HM-only, (c) generalize `CatalogVolumeAndLongRunPlanner`'s derived-dispatch branch (currently HALF_MARATHON-only) to also recognize a TEN_K-sourced derived candidate and route it to TEN_K's own existing policies, (d) confirm/derive TEN_K's own horizon triple rather than reusing HM's 10/12/14 (§21, not yet done). None of (a)-(d) require a new per-distance policy class or a new numeric authority — they are generalizations of existing generic classes — but they are genuine code changes this phase did not make, given the inability to validate them against the mandatory full regression and the hard zero-delta requirement for the live 10K-HM interval within this phase's own execution.

## 42. Whole-kilometre technical result

**`BACKEND_HAS_SUB_10K_DISTANCE_GENERATION_GAPS`** — proven structurally (§1-§5), not by an exhaustive generation sweep (none was run, §8).

## 43. Product result

**`SUB_10K_PUBLIC_PRODUCT_DECISION_BLOCKED`** — no realized output exists for any sub-10K target (§8/§24/§27-29), so no product-safety judgment, positive or negative, can be made yet. This is the conservative, honest answer the governing prompt itself asks for in preference to overclaiming.

## 44. Recommended V1 lower boundary

**`SUB_5K_REMAINS_UNSUPPORTED`** — this is the one confident, evidence-backed recommendation this phase can make: sub-5K has no backend authority to build on today, independent of any product judgment. No recommendation is made for the 5K-10K interval's lower boundary — that requires the §41 gap to be closed and real output to be inspected first, which this phase did not do.

## 45. Required next phase

A scoped **DERIVED-DIST.5 — 5K-to-10K generic authority extension** phase, explicitly limited to: (1) confirming TEN_K_MASTER's own catalog-declared `CoreCycle` minimum/preferred/maximum weeks (this phase found the reference but did not extract the numbers — §3/§21); (2) the four narrow, named generalizations in §41(a)-(d); (3) re-running this phase's entire checklist (§8-§34) with real generation, real HTTP+Postgres, and the full monolithic regression actually executed and compared by exact test identity against the DERIVED-DIST.3 baseline (5371/5350/4/17) before any such change is considered for even dark/internal activation. A separate, later, explicitly-named phase would be required before any sub-5K work is attempted, and only after a real canonical FIVE_K authority is deliberately built and governed (a materially larger undertaking than this phase or DERIVED-DIST.5).

## 46. Regression

**Not run in this phase.** This phase made zero production code changes (§36/§37) — every file read in §1-§5 was read-only. Because nothing reachable by any existing test was edited, no new reproducible failure is possible, and the DERIVED-DIST.3 baseline (PlanCatalog.Tests 1510/1510; full monolithic 5371 total/5350 passed/4 known durable failures/17 not-executed-by-design) stands unchanged by construction. The full monolithic suite was not re-executed in this phase to re-confirm that baseline number-for-number; this is disclosed as a real, named gap rather than silently assumed (see §47).

## 47. Remaining blockers

1. Canonical FIVE_K has no governed generation authority at all — a precondition for any sub-5K derivation, and out of scope for a "narrow fix" phase (§2/§40).
2. The 5K-10K interval's four named, narrow generalizations (§41 a-d) were identified but not implemented or tested — this phase made a deliberate, disclosed choice not to touch production code for a live-adjacent derived-distance pipeline without the capacity, in this same execution, to run the full mandatory regression and validate the hard zero-delta requirement for the live 10K-HM interval.
3. TEN_K's own horizon triple (Minimum/Preferred/MaximumCoreWeeks) was not extracted from the TEN_K_MASTER catalog document in this pass (§3/§21/§45).
4. No integer (1K-9K) or boundary (4.9/5.0/5.1/9.9/10.0/10.1) table was populated with real generation output — every number that would have appeared there is correctly left blank rather than fabricated (§8/§24/§32/§33).
5. The full monolithic regression suite was not re-executed this phase (§46) — believed unaffected by construction (zero files touched) but not re-confirmed by a fresh run.
6. The frontend's `_approvedProjectedTargetsKm = [15.0, 16.0, 18.0]` allowlist, already named by DERIVED-DIST.3, remains unfixed and unaffected by this phase — re-confirmed current (§38).

**Final classification: `SUB_10K_DERIVED_DISTANCE_BACKEND_COVERAGE_AND_PRODUCT_BOUNDARY_REVIEW_BLOCKED`.** The governing prompt's own completion checklist requires, among other things, 1-4K and 6-9K each actually tested against their respective derived sources, decimal/boundary controls actually exercised, and a full regression run with no new reproducible failure identity confirmed — none of the exhaustive execution items were completed in this phase. What this phase does deliver, with real evidence rather than inference, is the single most consequential finding the governing prompt's own hypothesis depended on: canonical FIVE_K does not exist as a generation authority today, and the "generic" derived-distance mechanism is generic only at its resolver layer, not at its volume/horizon/taper dispatch layer. No sub-10K target was, or is, publicly activated by this phase. No training-science number, per-distance policy, peak-volume policy, workout definition, or database/catalog change was introduced.
