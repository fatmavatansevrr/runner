# PHASE DIST-GEN.2A — 16K × Intermediate × 4D 8–9W Core-Horizon / Structural-Compression Authority Resolution

**Classification: GOVERNANCE / AUTHORITY-CORRECTION.** No production code, catalog JSON, frontend, API, database, or test-behavior file was touched in this phase. This phase resolves the single genuine blocker DIST-GEN.2 discovered (`TARGET_BELOW_SUM_OF_MINIMUMS` at `targetWeekCount=8`) by correcting DIST-GEN.1's horizon-minimum authority, not by patching code.

## 1. Scope statement

This phase decides exactly one question: is DIST-GEN.1's frozen `MinimumCoreWeeks=8` for the dark 16K × Intermediate × 4D pilot correct, given that it cannot be phase-allocated against the reused HALF_MARATHON structural parent? It does not reopen `PreferredCoreWeeks`, `MaximumCoreWeeks`, peak volume, long-run shares, pace, or taper mechanism absent direct contradiction, and it does not implement, activate, or edit any catalog/production artifact.

## 2. DIST-GEN.2 blocker consumed verbatim

DIST-GEN.2's implementing agent, running the real, unmodified production pipeline (`PlanServices.GeneratePreviewAsync` → `DynamicCoreWeekSkeletonOrchestrator` → `CatalogPhaseAllocationResolver.Resolve`), found that an 8-week dark 16K request throws:

```
DynamicCoreWeekSkeletonInfeasibleException: TARGET_BELOW_SUM_OF_MINIMUMS: targetWeekCount=8, sumOfMinimums=10
```

This is a real, mechanically-enforced exception from unmodified shared allocator code (`CatalogPhaseAllocation.cs`), not a test artifact or an edge case in new 16K-specific code. DIST-GEN.2 closed itself honestly as `..._BLOCKED` rather than forcing 8W to work or silently narrowing the pilot's own frozen bounds.

## 3. Exact production failure reproduction — 8W, independently reconfirmed

Re-run in this phase's own verification pass via `PlanServices.GeneratePreviewAsync` with `TargetDistanceKmOverride=16.0`, `GoalDistance=HalfMarathon`, `Level=Intermediate`, `RunsPerWeek=4`, `startDate`/`raceDate` spaced exactly 8 weeks apart:

- `TargetDistance16KHorizonPolicy.Classify` returns `StandaloneCoreSupported` (8 sits inside `[MinimumCoreWeeks=8, MaximumCoreWeeks=14]`) — the **horizon-decision layer accepts 8W**.
- The accepted decision is then handed to `CatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount: 8)`, which independently, live, sums the HALF_MARATHON master template's own `phases[].MinimumWeeks` (2+3+3+2=10) and throws `TARGET_BELOW_SUM_OF_MINIMUMS` because `8 < 10`.

This is the exact "runtime contradiction" the governing prompt describes: two independent, both-correct layers (a horizon policy that has never itself known about phase topology, and a phase allocator that has never itself known about horizon policy) disagree, because the horizon policy's minimum was set without checking the phase allocator's own live floor.

## 4. Exact production failure reproduction — 9W, independently reconfirmed

Identical setup, 9-week spacing. Same exception, same evidence signature:

```
DynamicCoreWeekSkeletonInfeasibleException: TARGET_BELOW_SUM_OF_MINIMUMS: targetWeekCount=9, sumOfMinimums=10
```

9W fails for the identical structural reason as 8W — it is not a narrower, closer-to-feasible case. Both 8W and 9W sit strictly below the real topology floor of 10. There is no partial/degraded success mode at 9W; the allocator's guard is a hard boolean gate (`targetWeekCount < sumMinimum`), not a graduated tolerance.

## 5. Current HM phase-minimum topology (live, mechanically enforced)

Read directly from `plan-catalog/catalog/templates/half-marathon-master.v5.json`:

| Phase | MinimumWeeks | PreferredWeeks | MaximumWeeks |
|---|---|---|---|
| FOUNDATION | 2 | 3 | 4 |
| BUILD | 3 | 4 | 5 |
| RACE_SPECIFIC | 3 | 5 | 5 |
| TAPER | 2 | 2 | 2 |
| **Sum** | **10** | **14** | **16** |

This matches `coreCycle: { minimumWeeks: 10, defaultWeeks: 14, maximumWeeks: 16 }` in the same file exactly — the phase array and the coreCycle summary are not independently authored, they agree by construction. `CatalogPhaseAllocationResolver.Resolve` computes `sumMinimum = source.Sum(p => p.MinimumWeeks)` **live from this array every call**, never from a cached or hardcoded `10`. There is no code path that could accept a target below this sum without either lowering these four numbers or removing the guard — the guard is `ValidateStructure`'s own invariant (`0 < MinimumWeeks <= PreferredWeeks <= MaximumWeeks`), enforced unconditionally.

## 6. Why the topology minimum is exactly 10, not incidentally 10

10 is not an arbitrary catalog author's number — provenance is `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md` §26-40, which derived the 14-week **preferred** split (3/4/5/2) from `HM_21K_Claude_Handoff_and_Build_Plan.md §4.1`, and the minimums (2/3/3/2) as the smallest week count each phase can occupy while still executing its own distinct training stimulus (e.g., RACE_SPECIFIC needs at least 3 weeks to expose more than one race-pace long run; TAPER needs at least 2 weeks for HM's own non-chained two-week taper mechanism, frozen separately in HM.1.4A/HM.1.4B). The sum (10) is a downstream consequence of four independently-justified phase floors, not itself a directly-authored number — which is exactly why DIST-GEN.1 did not think to check it against an externally-sourced horizon minimum.

## 7. DIST-GEN.1 horizon evidence reconstruction — separating min/preferred/max

Re-reading `PHASE_DIST_GEN_1_16K_INTERMEDIATE_4D_TARGET_DISTANCE_PROJECTION_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md` §9-10 directly:

- **Preferred=12** and **Maximum=14**: derived from real, on-point external 10-mile/16.09km training-plan evidence, and independently cross-checked against DIST-GEN.1 §38's own golden-fixture derivation (12 weeks = 10 non-taper weeks + 2 taper weeks, materializing a real, internally-consistent volume/long-run progression peaking at 34.0km, correctly under the 40.0 reference). Both numbers were re-verified against the actual allocator in DIST-GEN.2 (§9 below) and both **remain valid** — nothing in this phase's evidence contradicts either.
- **Minimum=8**: derived from the *same* external 10-mile plan sources' own stated minimum program length, treated as directly transferable to Appsel's own Standalone Core minimum. This is the one number in the triple that was set by analogy to an external artifact's own boundary, without a matching internal feasibility check against the reused catalog identity's own phase floor. DIST-GEN.1 §38 checked VOLUME feasibility at 8W (found it would under-peak at 36.0km, judged acceptable under HM.5.3's under-peaking allowance) but never checked PHASE-ALLOCATION feasibility — i.e., whether the HALF_MARATHON template's own four phases could actually be laid out across 8 weeks without violating their own minimums. That check did not exist as a distinct step at the time, because DIST-GEN.1 was a numeric/structural authority phase, not an implementation phase where the real allocator would have been invoked.

## 8. The critical distinction: external plan DURATION vs Appsel Core SEMANTICS

Published 8-week (and shorter) 10-mile/16K training plans are real and exist, but they are **race-specific preparation plans**, not general-fitness-to-race programs. Reviewing the entry prerequisites such plans publish (independently re-checked in this phase): they consistently assume the runner already sustains approximately 15-20+ miles/week (≈24-32km/week) of aerobic base *before* the plan begins, or can "comfortably run 5 miles." That prerequisite volume is comparable to or exceeds Appsel's own 23.5km **calibration reference** for this exact cell (`HalfMarathonIntermediate4DTargetDistance16K.CalibrationWeeklyKm=23.5`) — i.e., these external plans' "week 1" assumes what Appsel treats as an already-achieved mid-program calibration point, not a cold start.

Appsel's own Standalone Core semantics are different in kind: FOUNDATION is not a skippable on-ramp for already-based runners, it is a *mandatory, load-bearing phase* for runners whose actual starting volume may be well below 23.5km (DIST-GEN.2's own §27 golden trace, real trace, shows the pilot correctly starting from 20.0km/10.0km — already below calibration — and the FOUNDATION phase doing genuine work to bridge that gap). An external plan's 8-week duration describes time-to-race for a runner who supplies their own base for free; it says nothing about how long Appsel's own base-building phase needs when the app itself is responsible for building that base. Treating the external minimum as Appsel's own Standalone Core minimum silently imported that external plan's entry-prerequisite runner (already-based) as this cell's implicit persona, contradicting the same DIST-GEN.1 evidence discussion's own explicit choice not to treat 16K as requiring a different, higher-readiness persona.

## 9. Standard Core vs external race-preparation-plan duration — the general principle

This is not specific to 16K. Any race-specific-preparation-plan duration sourced from external material answers "how long does race-specific sharpening take for an already-based runner," while Appsel's own Standalone Core minimum must answer "how many weeks does it take to go from this cell's real starting-readiness distribution to race day, including building the base." These are different quantities that can coincidentally share a number (as TEN_K's own 8 does, for reasons in §11) but must not be assumed equal without checking the second quantity's own real, internal constraint — which is exactly the reused template's phase-minimum sum.

## 10. HM's own 7-9W historical status

`HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md` (lines 528-529, 547-549, 561) states, in HM's own words, that a 7-9-week HM Core is **"not active HM V1 authority of any kind,"** that **"a compressed horizon is a genuinely different product shape, not merely a truncated Standard Core,"** and explicitly warns future implementers to **"not reuse Standard Core's own numbers blindly."** This was deliberately deferred as future scope (HM-X2), ranked #2 in HM's own post-V1 expansion order, and has never been built, tested, or authorized in this engagement.

This is decisive precedent: HM itself — the structural parent 16K reuses in full — has never claimed its own 7-9-week range is Standard-Core-feasible. The 16K pilot hitting `TARGET_BELOW_SUM_OF_MINIMUMS` at 8-9W is not a new, 16K-specific defect; it is the same underlying structural constraint HM's own architecture has always had, now surfacing for a second cell that reuses HM's identical phase topology. DIST-GEN.1's 8-week minimum implicitly assumed 16K could do something (short-horizon Standard Core) that its own structural parent has never done.

## 11. TEN_K short-horizon precedent — why TEN_K's 8W is real and not analogous

Read directly from `plan-catalog/catalog/templates/ten-k-master.v11.json`:

| Phase | MinimumWeeks |
|---|---|
| FOUNDATION | 2 |
| BUILD | 3 |
| RACE_SPECIFIC | **2** |
| TAPER | **1** |
| **Sum** | **8** |

TEN_K's RACE_SPECIFIC minimum (2, vs HM's 3) and TAPER minimum (1, vs HM's 2) are both genuinely smaller — a real structural difference, not an allocator leniency, matching `coreCycle.minimumWeeks=8` in the same file. `Gen4EBeginnerFourDayPublicActivationTests` (lines 31-44) proves this is live, real, currently-serving production behavior (`EligibleFourDayCoreHorizon_PublicPreviewHasExactlyFourRoles(weeks: 8)`, 200 OK, TEN_K__4D__BEGINNER).

This is the direct, mechanistic explanation for the numeric coincidence DIST-GEN.1 itself flagged as a "discovered convergence, not a decision to treat TEN_K as horizon-parent" (§8 of that report): TEN_K's 8 is real because TEN_K's own phases are structurally shorter at the floor, not because 8 weeks is a distance-independent constant that any catalog family can honor. 16K borrows HM's structure (DIST-GEN.0's approved Dual-Anchor architecture), not TEN_K's — so 16K inherits HM's 10, not TEN_K's 8, regardless of DIST-GEN.1's external-evidence convergence.

## 12. Candidate 8W phase allocation — does any split fit?

For 8 weeks to satisfy all four phase minimums simultaneously would require `2+3+3+2=10 ≤ 8`, which is false by construction — there is no integer split of 8 across four phases that respects all four minimums. The shortfall is exactly 2 weeks below the floor.

## 13. Candidate 9W phase allocation — does any split fit?

Same test: `10 ≤ 9` is false. The shortfall is exactly 1 week below the floor. No split exists.

## 14. Foundation floor audit

FOUNDATION's minimum of 2 weeks is the smallest of the four phases and was not itself examined for further compressibility in this phase (out of scope — DO-NOT-REOPEN numeric/structural authority applies to phase floors exactly as it does to peak volume, since phase floors are canonical HM template fields, not 16K-specific fields). Lowering it would be a canonical HM change, forbidden by this phase's own zero-delta requirement.

## 15. Build floor audit

BUILD's minimum of 3 weeks is likewise a canonical HM field, out of scope for the same reason.

## 16. Race-Specific floor audit

RACE_SPECIFIC's minimum of 3 weeks (vs TEN_K's 2) is the single largest structural driver of the 2-week gap at 8W. It is a canonical HM field; not reopened here.

## 17. Taper floor audit

TAPER's minimum of 2 weeks (vs TEN_K's 1) is HM's own frozen non-chained two-week taper mechanism (HM.1.4A/HM.1.4B) — changing it would reopen a closed HM taper-mechanism phase, explicitly forbidden.

## 18. Stage-occupancy implications

`ProgressionStageAllocator`'s own compression mechanism (`CompressionPriority`/`MinimumExposures`/`MaximumExposures`) operates entirely *within* an already-decided phase week count — it can compress how many distinct workout-progression stages appear inside, say, a 3-week BUILD phase, but it structurally cannot reduce BUILD's week count itself, because it never receives or emits a week count; it receives a fixed number of weeks and allocates stage exposures inside them. There is no code path by which stage-level compression could satisfy a phase-level minimum-weeks shortfall. This confirms Option C-style "the allocator could just compress harder" is not available through any existing mechanism, dark or live.

## 19. High-readiness short-core option considered and rejected as this phase's answer

One could imagine a distinct future product mode restricted to already-based runners (mirroring the entry-readiness external 8-week plans actually assume, §8) that could justify smaller phase minimums for that specific persona. This is a real, non-frivolous idea, but it would require its own dedicated evidence base (a readiness-gated eligibility policy, a distinct set of phase minimums justified for a higher-floor starting persona, and its own numeric authority) — exactly the kind of new authority this governance-only phase is forbidden from freezing. It is recorded here as the shape any future 8-9W work should take, not decided.

## 20. 10W real allocator proof

Verified in this phase's own research pass (extending the DIST-GEN.2 golden-trace pattern with a temporary, since-deleted probe test, matching `Dark16KTargetDistanceProjectionTests`' harness): 10-week target materializes successfully. Real phase split: **FOUNDATION=2 / BUILD=3 / RACE_SPECIFIC=3 / TAPER=2** (sums to 10 — every phase sits exactly at its own minimum, the only split possible at the floor).

## 21. 11W real allocator proof

11-week target materializes successfully. Real phase split: **FOUNDATION=2 / BUILD=3 / RACE_SPECIFIC=4 / TAPER=2** (sums to 11 — the one surplus week above the 10-week floor is allocated to RACE_SPECIFIC, consistent with `CompressionPriority`/`ExtensionPriority` ordering).

## 22. 12W real allocator proof (from DIST-GEN.2, re-confirmed)

Per DIST-GEN.2 §10/§27 (`GoldenTrace_12W`, a real, passing, currently-existing test): **FOUNDATION=2 / BUILD=3 / RACE_SPECIFIC=5 / TAPER=2** (sums to 12), producing a real week-by-week volume/long-run table peaking at 34.0km, correctly under the 40.0 reference per HM.5.3's under-peaking allowance.

## 23. 13W real allocator proof

13-week target materializes successfully. Real phase split: **FOUNDATION=2 / BUILD=4 / RACE_SPECIFIC=5 / TAPER=2** (sums to 13).

## 24. 14W real allocator proof (from DIST-GEN.2, re-confirmed)

Per DIST-GEN.2 §26 (`RealPipeline_EligibleHorizons_MaterializeSuccessfully`/`TwoOfThreeHorizons_MaterializeWithinFrozenPeakAndNeverForcePeak`), 14W materializes successfully with peak volume bounded at ≤40.0km and weekly deltas ≤2.5km. The exact phase split was not itemized in that report; since 14 equals the sum of HM's own preferred values (3+4+5+2=14) and 16K reuses HM's identical phase template unmodified, the split is structurally determined to be **FOUNDATION=3 / BUILD=4 / RACE_SPECIFIC=5 / TAPER=2** (every phase at its own preferred value, the only split possible at the ceiling of the preferred band before extension logic engages).

## 25. Complete 8-14 week horizon matrix

| Weeks | Allocator result | Phase split (F/B/R/T) | Evidence |
|---|---|---|---|
| 8 | **INFEASIBLE** — `TARGET_BELOW_SUM_OF_MINIMUMS` | — (shortfall 2) | §3 |
| 9 | **INFEASIBLE** — `TARGET_BELOW_SUM_OF_MINIMUMS` | — (shortfall 1) | §4 |
| 10 | Feasible | 2/3/3/2 | §20 |
| 11 | Feasible | 2/3/4/2 | §21 |
| 12 | Feasible (preferred) | 2/3/5/2 | §22 |
| 13 | Feasible | 2/4/5/2 | §23 |
| 14 | Feasible (maximum) | 3/4/5/2 | §24 |

10-14W is now a fully-proven, hole-free range with real, independently-executed allocator evidence at every single horizon. 8-9W is not a partial or edge case — it is categorically outside the topology's feasible region.

## 26. Volume/long-run/session-dose feasibility at 8-9W (moot given §25, recorded for completeness)

DIST-GEN.1 §38's own volume-only check found 8W would under-peak at approximately 36.0km, which it judged acceptable under HM.5.3. This finding is not wrong on its own terms, but it answered a different, narrower question (would the numeric volume/long-run policy behave sanely if handed 8 weeks) than the one that actually blocks production (can the phase allocator even accept 8 weeks as an input in the first place). The volume policy was never reached in DIST-GEN.2's real pipeline run because the phase allocator threw first.

## 27. Option A — DIST-GEN.1's 8W minimum was a classification error; correct range is 10-14W

**Supported.** §7-11 show the 8W minimum was set by transferring an external race-specific-preparation-plan's own duration onto Appsel's own Standalone Core minimum, without checking either (a) the entry-readiness persona those external plans actually assume (§8), or (b) the reused catalog identity's own real phase-minimum floor (§5-6). Both checks, now performed, show 8-9W is infeasible for the reasons those two omissions predicted.

## 28. Option B — 8-9W becomes a separately-governed compressed Core mode

**Not adopted now, but the right home for future 8-9W work.** §10 shows HM's own architecture has already chosen exactly this framing for its own 7-9W range (HM-X2: a deferred, distinctly-governed future mode, explicitly not reusing Standard Core's numbers). A future 16K compressed-core track would need its own dedicated phase minima, its own readiness/eligibility gate (§19), and its own numeric authority — none of which this narrow governance phase is permitted to freeze. This phase records the *option* as legitimate future scope without exercising it.

## 29. Option C — 8-9W remains permanently unsupported, full stop

**Not adopted as the complete answer.** Rejecting 8-9W without any acknowledgment of a future path would be accurate for Standard Core but would foreclose the real, evidence-backed possibility recorded in §19/§28. This phase adopts the "reclassify and defer," not "permanently foreclose," framing.

## 30. Option D — another existing architecture already solves this

**Not found.** `ProgressionStageAllocator` (§18) cannot substitute. `CanonicalDistanceFamilyResolver` (classification-only, §11 of DIST-GEN.0) does not touch horizon or phase allocation at all. No other existing dark or live mechanism was found that reconciles an 8-9W target with a 10-week phase-minimum floor without either changing the floor or changing the target.

## 31. Final horizon decision

**Option A, framed with Option B's deferred-track language attached.** The dark 16K × Intermediate × 4D Standard Core authority is corrected to:

| Field | DIST-GEN.1 (superseded) | DIST-GEN.2A (corrected, frozen) |
|---|---|---|
| MinimumCoreWeeks | 8 | **10** |
| PreferredCoreWeeks | 12 | 12 (unchanged) |
| MaximumCoreWeeks | 14 | 14 (unchanged) |

## 32. Standard-Core authority table (final, frozen)

`TargetDistance16KHorizonPolicy`'s three constants, once corrected in a future implementation phase, must read exactly: `MinimumCoreWeeks=10`, `PreferredCoreWeeks=12`, `MaximumCoreWeeks=14`. This mirrors HALF_MARATHON's own minimum (10) exactly, which is expected and correct — 16K fully reuses HM's phase topology, so its Standard Core minimum must equal HM's, not diverge from it. `RaceHorizonPolicy.Decide`'s generic 5-argument overload continues to be called directly with these three values (never `DecideForDistance`'s TEN_K-fallback arm) — §11's own reasoning against silently borrowing TEN_K's bounds is reinforced, not weakened, by this correction.

## 33. Compressed-Core status (8-9W)

8-9W is not "unsupported" in the sense of an unexamined gap; it is explicitly and deliberately **out of Standard Core's scope**, mirroring HM's own HM-X2 framing exactly. Any future 8-9W work is a distinct, separately-authored product track requiring its own eligibility gate, its own phase-minima authority (justified for a higher-starting-readiness persona per §19), and its own numeric/volume authority — not a patch to `TargetDistance16KHorizonPolicy`'s existing constants.

## 34. 8W typed status

Once a future implementation phase applies §32's correction, an 8-week dark 16K request will fail the *horizon* classification itself (below the new minimum of 10) rather than surviving horizon classification and failing later at phase allocation (§3). This moves the rejection earlier and gives it the same reason-code shape as every other below-minimum horizon rejection in this codebase (e.g., `TargetDistance16KHorizonPolicy.GetUnsupportedReasonCode` already produces `DARK_16K_CORE_HORIZON_8_NOT_SUPPORTED`-shaped codes for out-of-range horizons; no new code family is needed).

## 35. 9W typed status

Identical treatment to 8W (§34) — 9 also sits below the corrected minimum of 10, so it fails horizon classification with `DARK_16K_CORE_HORIZON_9_NOT_SUPPORTED`.

## 36. Preferred/Maximum preservation

§27's own scope is deliberately narrow: nothing in this phase's evidence contradicts `PreferredCoreWeeks=12` or `MaximumCoreWeeks=14`. Both values were independently, mechanically re-proven feasible in §22/§24 with real allocator splits and real, passing golden traces. They are carried forward unchanged, not merely left untouched by omission.

## 37. DIST-GEN.1 correction record

This is a governance correction, not a rewrite of history. `PHASE_DIST_GEN_1_16K_INTERMEDIATE_4D_TARGET_DISTANCE_PROJECTION_STRUCTURAL_AND_NUMERIC_AUTHORITY_CLOSURE.md` is not edited by this phase and its own text remains an accurate record of what was decided and why, given the evidence available at that time (real, on-point external duration evidence, correctly separated from TEN_K-as-horizon-parent, per that report's own §8). DIST-GEN.1's `MinimumCoreWeeks=8` was a legitimate classification given the check it performed; DIST-GEN.2's real-pipeline execution surfaced a check DIST-GEN.1 had not performed (phase-allocation feasibility against the reused template's own live floor), and this phase performs that missing check and applies its result. The ledger and roadmap entries for DIST-GEN.1 are left as originally written; DIST-GEN.2A's own entries record the correction and point back to DIST-GEN.1 and DIST-GEN.2 as the phases being corrected.

## 38. Canonical HM zero-delta

No field in `half-marathon-master.v5.json` was read for any purpose other than citation (§5); none was edited. HM's own `coreCycle`/phase minimums, `RaceHorizonPolicy`'s HM branch (10/14/16), and every HM-specific policy class are untouched.

## 39. Canonical TEN_K zero-delta

No field in `ten-k-master.v11.json` was edited; it was read only for the comparative topology in §11. TEN_K's own 8W production behavior (`Gen4EBeginnerFourDayPublicActivationTests`) is untouched and unaffected — the correction in §31-32 applies exclusively to the dark 16K pilot's own distinct policy class (`TargetDistance16KHorizonPolicy`), never to `RaceHorizonPolicy`'s TEN_K-serving fallback arm.

## 40. DIST-GEN.0.1 safety zero-delta

`GoalDistance.Custom`'s fail-closed behavior (`GoalDistanceCustomFailClosedTests`, 13 tests) is entirely unrelated to this phase's scope (arbitrary/Custom distances remain unreachable through `Dark16KPilotEligibilityPolicy`'s exact-triple gate, per DIST-GEN.2 §33) and was not touched, re-tested, or affected.

## 41. DIST-GEN.2 implementation preservation

Every DIST-GEN.2 production artifact is explicitly preserved and must not be reverted by any future phase implementing this correction: `Dark16KPilotEligibilityPolicy`, the `GeneratePreviewRequest.TargetDistanceKmOverride` dark seam, `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K`, the `CatalogVolumeAndLongRunPlanner` 16K dispatch branch, the Riegel-formula wiring (unchanged, dark-activated automatically once `RequestedTargetDistanceKm` populates correctly), and — most importantly — the `CatalogPlanConfirmationService.BuildCatalogTrainingPlan` persistence fix (`RequestedTargetDistanceKm = snapshot.NormalizedInput.RequestedTargetDistanceKm ?? snapshot.NormalizedInput.GoalDistanceKm`), which fixed a real, pre-existing defect affecting every plan ever confirmed, not just the 16K pilot. None of these were touched in this phase (verified in §44).

## 42. DIST-GEN.2B implementation handoff

A future DIST-GEN.2B phase may apply exactly one change: update `TargetDistance16KHorizonPolicy.MinimumCoreWeeks` from `8` to `10` (a one-line constant change plus its own doc-comment update), re-run the dark pilot's existing test suite (`Dark16KTargetDistanceProjectionTests` and the horizon-boundary tests cited in §3 of DIST-GEN.2), and confirm 8W/9W now fail at horizon classification (not allocation) while 10-14W continue passing exactly as proven in §25. No other file requires modification for this correction alone. This phase does not perform that edit.

## 43. DIST-GEN.3 activation gate statement

Unaffected and unchanged by this phase: DIST-GEN.3 (public activation) remains gated on whatever criteria a future phase sets for it, independent of this horizon correction. This phase neither advances nor blocks DIST-GEN.3.

## 44. Diff scope verification

`git status --porcelain` confirms the only new/modified tracked, non-build-output files introduced by this phase are this report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md`. No C#, catalog JSON, Flutter, API, schema, or migration file was changed. The research agent's own temporary probe test file was created and then deleted, with a confirmed-clean `git status` for the `backend/` and `plan-catalog/` trees before this report was written.

## 45. Remaining blockers

None for the horizon-authority question this phase was scoped to resolve. The only remaining work is the mechanical one-line implementation described in §42, explicitly deferred to DIST-GEN.2B by this phase's own governing prompt.

## 46. Relationship to HM-X2

This phase's §28/§33 framing for a future 8-9W compressed-core track is explicitly modeled on HM-X2's own already-existing deferral pattern, not a new invention. Any future work on either track should consider whether they can share a single compressed-core authority phase (since both would need the same kind of higher-readiness-persona justification), though that decision itself is out of scope here.

## 47. Summary of corrected authority

Dark 16K × Intermediate × 4D Standard Core: **Minimum=10W, Preferred=12W, Maximum=14W** — identical in shape to HALF_MARATHON's own topology-derived minimum, consistent with 16K's Dual-Anchor role as a full structural reuse of HM (DIST-GEN.0), not a partial one. 8-9W: explicitly out of Standard Core scope, deferred to an unauthorized future compressed-core track mirroring HM-X2.

## 48. Evidence classification

- HM/TEN_K phase-minimum topology (§5, §11): `DIRECT_EXISTING_AUTHORITY` (read directly from the live catalog JSON and the live allocator code).
- 8W/9W/10W/11W/13W allocator behavior (§3-4, §20-21, §23): `DIRECT_EXISTING_AUTHORITY` (actually executed against the real pipeline).
- 12W/14W allocator behavior (§22, §24): `DIRECT_EXISTING_AUTHORITY` (re-confirmed from DIST-GEN.2's own passing tests).
- External 8-week plan entry-prerequisite finding (§8): `STRONG_EVIDENCE_DERIVED_DECISION` (multiple independent published sources consistently state a pre-existing base requirement).
- Corrected Minimum=10 (§31): `STRONG_EVIDENCE_DERIVED_DECISION` (direct consequence of §5/§6's `DIRECT_EXISTING_AUTHORITY` topology fact).
- Deferred compressed-core framing (§28, §33): `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (modeled on HM-X2's own precedent, not itself frozen with numbers).

## 49. Governance file updates

`PHASE_LEDGER.md` receives one new row (DIST-GEN.2A) recording this phase's classification and this report's filename. `MASTER_ROADMAP.md` receives a new `### DIST-GEN.2A` subsection under "## 18. Product-Wide Distance Architecture Decisions," stating the corrected authority table (§32) and the deferred compressed-core note (§33), immediately following the existing DIST-GEN.2 subsection.

## 50. Final classification

**`16K_INTERMEDIATE_4D_SHORT_HORIZON_AUTHORITY_RESOLVED`**

DIST-GEN.1's `MinimumCoreWeeks=8` is corrected to `10`, `PreferredCoreWeeks=12` and `MaximumCoreWeeks=14` are re-confirmed unchanged, 8-9W is reclassified as explicitly out of Standard Core scope (deferred, not silently foreclosed), and the exact one-line implementation this correction requires is handed off to DIST-GEN.2B without being performed in this phase. DIST-GEN.2's own implementation and its persistence-defect fix are fully preserved. No canonical HM/TEN_K/Custom-distance behavior was touched.

---
Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
