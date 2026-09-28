# PHASE DIST-GEN.14 — Post-Normalization Next Projected-Distance Expansion Decision

## 1. Post-DIST-GEN.13 baseline

Repository tip at the start of this phase: `185a2b7` on `main`, 0 ahead / 0 behind `origin/main`. Accepted regression baseline, read from `PHASE_DIST_GEN_13_...md` and re-confirmed unchanged (this phase performs no test runs, since it changes no runtime source): `PlanCatalog.Tests` 1510/1510; `RuntimeCatalog` subtree 4008/4008; full monolithic 5111 total/5107 passed/4 known durable failures, with the one transient fifth failure from DIST-GEN.13's own verification independently shown non-reproducing. This phase does not rerun the suite — DIST-GEN.13 already closed with a byte-for-byte-verified zero-delta result, and this phase touches no source.

## 2. Current public/dark matrix

Frozen, not reopened:

| Target | Dark | Public |
|---|---|---|
| 15.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 16.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| 18.0 × HalfMarathon × Intermediate × 4D | Yes | Yes |
| Everything else | No | No |

Habit Custom unsupported. Experienced excluded product-wide. No current cell is reopened by this phase.

## 3. Post-normalization architecture

Directly re-confirmed from current source (not re-derived from older reports, which describe a dispatch shape that no longer exists):

- **`ProjectedTargetAuthority`** (`internal sealed record`) — composes, per approved cell: the exact `ApprovedDarkTargetDistanceCell` (`TargetDistanceKm`, `ParentDistanceFamily`, `Level`, `RunsPerWeek`); a `PilotLabel`; `MinimumCoreWeeks` (parent-family, sourced via `RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks=10`); `PreferredCoreWeeks`/`MaximumCoreWeeks` (exact-target); bound delegates to that target's own horizon-decision/classification/reason-code methods; a direct reference to that target's own `VolumeSafetyPolicy` instance; a bound delegate to that target's own peak-volume-band builder. Nothing is computed inside the record — every field is a reference or a bound method.
- **`ProjectedTargetAuthorityRegistry`** — a fail-closed `IReadOnlyDictionary<ApprovedDarkTargetDistanceCell, ProjectedTargetAuthority>`, exact-cell keyed (all four dimensions, never distance alone), exactly 3 entries today (15K/16K/18K, all HalfMarathon×Intermediate×4D). `TryResolve` is plain dictionary equality — zero tolerance, zero nearest-match. Duplicate registration throws `InvalidOperationException` at index-build time. Strictly downstream of, never a substitute for, the separate dark/public eligibility gates.
- **`IntermediateFourDayGrowthAuthority`** — the single, Intermediate×4D-scoped owner of growth rates/absolute cap/LR-share quadruple/starting-LR-cap, referenced (not copied) by the three projected targets. Canonical TEN_K and canonical HM Intermediate 4D deliberately keep their own independent literal declarations of the identical values — not because sharing was rejected, but because DIST-GEN.13 scoped the shared component narrowly to the fields whose implementation was actually duplicated across the *projected* targets specifically; the canonical anchors were left untouched by design.
- **`HalfMarathonParentTaperAuthority`** — the single owner of the HM-parent taper fields (2 weeks, 0.70/0.43, non-chained), referenced by 15K/16K/18K and canonical HM's own 4D/6D instances. TEN_K's genuinely different single-week 0.53 taper remains completely independent.

All four former hand-written dispatch cascades (`PlanServices.cs`, `CatalogPreviewGenerator.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`) now call `ProjectedTargetAuthorityRegistry.TryResolve(darkCell)` once and pattern-match on the nullable result — confirmed directly in `PlanServices.cs`, which replaced eight separate three-armed ternary/string expressions with field/delegate reads off one resolved object.

## 4. Registry scalability

DIST-GEN.13's own final finding, independently re-confirmed via direct source read of all four former cascade sites: adding a fourth target today requires **exactly one new `ProjectedTargetAuthority` registration entry plus that target's own new policy files** — zero edits at any of the four former dispatch sites. This is a genuine, verified reduction from the pre-DIST-GEN.13 state (which required a fourth hand-edited branch at four separate sites, eight expressions in `PlanServices.cs` alone).

**Classification: `EXACT_TARGET_REGISTRY_SCALABLE_WITH_CURRENT_NORMALIZATION`**, carried forward unchanged from DIST-GEN.13 — the normalization work is complete and does not need to be reopened for this reason.

## 5. Reduced new-target authority surface

For a hypothetical new Intermediate×4D projected target, the authority surface it would still need to close is now strictly smaller than before DIST-GEN.13:

**Must close (exact-target, no shared component exists or is permitted to exist for these):** `PreferredCoreWeeks`, `MaximumCoreWeeks`, `PeakVolumeBandMin/Max`, `SelectedPeakReference`, `CalibrationStartingVolumeKm`, `PreferredAbsolutePeakLongRunKm`.

**Reused for free (referenced, not re-derived):** growth rates, absolute weekly cap, LR-share quadruple, starting-LR cap (via `IntermediateFourDayGrowthAuthority`, contingent on Intermediate×4D); taper duration/multipliers/mechanism (via `HalfMarathonParentTaperAuthority`, contingent on HALF_MARATHON parentage); `MinimumCoreWeeks=10` (via the existing parent-family constant); phase topology (reused HM candidate, unmodified); every generic mechanism (pace, Riegel, feasibility, readiness, calendar, adaptation).

**Registration cost:** one new dark-registry entry, one new (separate) public-registry entry if public activation is ever pursued, one new `ProjectedTargetAuthorityRegistry` entry, and that target's own horizon/peak-band/`VolumeSafetyPolicy` files — nothing else.

This reduction is real and directly relevant to comparing options below, but it is an **implementation-cost** fact, not an **evidence** fact — it makes any fourth *exact 4D target* cheaper to build once its own numbers are closed, but it does not supply those numbers, and it says nothing about non-4D axes (frequency/level), which cannot reuse `IntermediateFourDayGrowthAuthority`'s own scope at all.

## 6. Shared authority reuse

Confirmed unchanged since DIST-GEN.12/13, consumed without re-litigation: growth rates (0.07/0.08), absolute weekly cap (2.5 at 4D), LR-share quadruple (0.30/0.36/0.33/0.40 at 4D), starting-LR cap (null at 4D), and HM-parent taper (2 weeks, 0.70/0.43, non-chained) are all governance-closed shared authority for the Intermediate×4D scope specifically. This phase does not recount or re-derive any of these — per §5 of the governing prompt, they are not charged against any 4D-target option's own new-authority cost below.

## 7. Exact-target authority remaining

Unchanged from DIST-GEN.12 §54: `PreferredCoreWeeks`/`MaximumCoreWeeks`, `PeakVolumeBandMin/Max`, `SelectedPeakReference`, `CalibrationStartingVolumeKm`, and `PreferredAbsolutePeakLongRunKm` remain — and must permanently remain — exact-target-owned. No option evaluated below proposes moving any of these into a shared component.

## 8. Banding status entering phase

DIST-GEN.12 closed `BANDING_STILL_NOT_GOVERNANCE_READY` after refining the analysis to show that three of the fields that once looked like band candidates (peak-volume band, selected peak, calibration) are better explained as consequences of a shared derivation *methodology* applied three times than as genuine interior-stability evidence, leaving only horizon-preferred/maximum as real (if boundary-unready) candidates. DIST-GEN.13 implemented no band and gathered no new banding evidence — it was a pure implementation-ownership refactor. Nothing in this phase's own review of current source or prior reports supplies new boundary evidence for any field. Per the governing prompt's own explicit instruction (§9), architecture normalization is not itself training-science evidence, so this status is **carried forward unchanged**, not re-earned.

## 9. Product-value evidence

Re-confirmed via direct search (fourth independent confirmation, after DIST-GEN.4 §41, DIST-GEN.8 §45, DIST-GEN.12 §45): no internal product telemetry, usage-analytics, or demand-signal source exists anywhere in this repository for 20K, 10-mile, 12K, any frequency, or any level, for any projected target. **`SOURCE_SILENCE`**, unchanged. External race-distance prevalence remains context only and is not used as a substitute for internal demand evidence anywhere in this report.

## 10. Supplemental research scope

None performed. Per the governing prompt's own external-research policy (§29: reuse existing evidence first; research narrowly only where a specific question materially changes the decision): DIST-GEN.9 already directly answered the 20K evidence-availability question (thin, like 18K — see §12 below); the 10-mile-alias evidence question has now been examined four times (DIST-GEN.4/8/9/12) with the same `INSUFFICIENT_EVIDENCE` result and diminishing returns each time; the 12K question is an architecture question (is HALF_MARATHON the correct structural parent near TEN_K), not one further web searches about training plans can resolve. No genuine new external-research need was identified.

## 11. Option A — 20K × Intermediate × 4D

The fourth exact target at the near-HM boundary. Per DIST-GEN.4 §25 and DIST-GEN.8 §34, both predicted — before 18K's own closure existed — that 20K "would most likely show near-total convergence toward HM's own numeric authority." DIST-GEN.12 §34 reconfirmed this prediction and sharpened it using DIST-GEN.9's own methodology-artifact finding: a near-HM 20K closure would very likely reproduce HM's own 43.0/25.0 pair almost exactly, via the same derivation methodology already shown to have limited resolving power at this granularity (§20-22 of DIST-GEN.12). The core information question 20K could answer — "where does projected-target authority converge to canonical HM" — has an already well-predicted answer from two prior phases' own analysis, which lowers, not raises, its marginal information value.

## 12. 20K evidence availability

DIST-GEN.9 §14/§27 directly researched this and found 20K genuinely evidence-sparse: repeated targeted web searches for "20K training plan" and "20km race training plan" returned marathon/half-marathon content misattributed via plan-*duration* keyword overlap ("20-week" programs), not genuine 20-kilometer race-distance content — confirming 20K is not a standardized, commercially-supported race distance with dedicated training-plan literature, in the same category as 18K. This finding is directly reused here, not re-researched, per §10 above. A 20K closure would very likely land at the same thin `EVIDENCE_INFORMED_PRODUCT_DEFAULT` tier 18K's own closure reached (which was the only one of the three closures to record zero fields at the strongest evidence tier).

## 13. 20K architecture information value

20K could, in principle, answer whether canonical HM authority becomes directly reusable rather than requiring an independent projected-target policy file once a target sits close enough to HM's own 21.0975. This is a real, legitimate architectural question — but answering it with confidence requires dedicated evidence distinguishing "genuinely converges" from "the derivation methodology cannot discriminate at this resolution," and §12 shows that evidence does not exist and is unlikely to be found by further searching. Absent that evidence, a 20K closure would most likely produce another thin, predictable, `EVIDENCE_INFORMED_PRODUCT_DEFAULT`-heavy authority table that confirms what two prior phases already predicted, rather than teaching something genuinely new.

## 14. 20K authority cost

Post-DIST-GEN.13, the **implementation** cost of registering a fourth 4D target is low (§5) — but the **training-authority** cost is unchanged: `PreferredCoreWeeks`/`MaximumCoreWeeks`, peak-volume band, selected peak, calibration, and `PreferredAbsolutePeakLongRunKm` would all need independent closure, against evidence already shown to be thin. Authority cost: **MEDIUM** (bounded surface, per §5, but genuinely new numeric decisions against sparse evidence). Engineering cost: **SMALL** (one registry entry, per §5). Net: cheap to build, but the number-closure step is evidence-constrained, not implementation-constrained — DIST-GEN.13's own cost reduction does not touch this constraint at all.

## 15. Option B — 10-mile exact / 16.0934K

The explicit authority-alias / identity-precision question DIST-GEN.4 first framed and DIST-GEN.8/9/12 each re-examined. Current state: 16.0 is public; 16.0934/16.09 is deliberately, explicitly unsupported (verified still true in current source — no tolerance, no tolerance widening anywhere in `ProjectedTargetAuthorityRegistry` or the eligibility gates). The central question remains whether exact race identity (`RequestedTargetDistanceKm=16.0934`) can be explicitly, auditably mapped to 16K's own approved training authority without becoming disguised nearest-target matching.

## 16. Alias evidence standard

Unchanged since DIST-GEN.8 §37: an alias may be approved only if evidence shows the training-authority difference between 16.0 and 16.0934 is not meaningful enough to require independent authority — never merely because the numeric gap (0.0934km) is small. DIST-GEN.5's own Hal Higdon 15K/10-mile combined-program observation (peak long run landing near the *full* 10-mile distance, a materially tighter margin than 16K's own 15.0/16.0 ratio) remains the only relevant data point, and it argues weakly *against* a clean alias, not for one. No new evidence has been gathered on this question in DIST-GEN.9, 12, or 13.

## 17. Alias architecture compatibility

This is the one sub-question DIST-GEN.13's own new architecture could plausibly have changed, so it was checked directly rather than assumed. Finding: **it has not changed**. `ProjectedTargetAuthority.Cell` is literally the same `ApprovedDarkTargetDistanceCell` record the eligibility gates use — a plain `(TargetDistanceKm, ParentDistanceFamily, Level, RunsPerWeek)` quadruple with no separate field distinguishing "the exact identity the user requested" from "the training-authority key that identity resolves to." DIST-GEN.8 §37's own architectural gap — needing one additional field to represent an explicit alias without collapsing into nearest-target matching — is **still present, unchanged, in the post-DIST-GEN.13 registry**. DIST-GEN.13's own scope was strictly the *dispatch* layer (replacing branches with one lookup); it did not touch the *key shape* the lookup uses. **Architecture impact classification: MEDIUM** — not a large rewrite (the registry's own `TryResolve` signature would only need a second, optional identity-passthrough concept), but a real, non-trivial, currently-unbuilt extension, not a trivial one.

## 18. Alias authority cost

If ever approved, authority cost is genuinely **LOW** (no new numeric value — the entire premise is reusing 16K's own already-frozen authority). But approval itself is gated on evidence this engagement has searched for four times without finding, and the marginal value of a fifth examination without a new evidence source is low, exactly as DIST-GEN.12 §60 already concluded. Nothing in this phase changes that calculus.

## 19. Option C — Intermediate × 3D frequency orthogonality

A genuinely untested axis: no projected target exists at any frequency other than 4D. Direct inspection of `VolumeSafetyPolicy.cs` across every level/frequency combination (not merely the projected-target instances) surfaced a finding no prior DIST-GEN report appears to have called out explicitly: **at Intermediate×3D, canonical TEN_K's own LR-share quadruple (0.38/0.42/0.40/0.42) genuinely disagrees with canonical HALF_MARATHON's own (0.34/0.40/0.38/0.46)**, even though both share the identical `AbsoluteWeeklyIncreaseCapKm=2.0`. This is a materially different picture from 4D, 5D, and 6D, where TEN_K and HM agree exactly on every one of these fields (4D: both 0.30/0.36/0.33/0.40; 5D/6D: both 0.28/0.36/0.28/0.36) — the exact agreement DIST-GEN.8 used as its strongest evidence that these fields are level/frequency-invariant rather than distance-scoped. At 3D specifically, that agreement breaks down for the LR-share quadruple (though not for the absolute cap, which still agrees at 2.0 across both families).

This is new information, surfaced by this phase's own direct source reconstruction, not previously classified by any prior report (none of DIST-GEN.0-13 appears to compare TEN_K's and HM's own 3D values against each other explicitly). It is not a defect and does not reopen DIST-GEN.13's own normalization — no shared component was ever built for the 3D scope, so there is nothing mis-implemented; it is simply evidence that the *assumption* "LR-share quadruple is level/frequency-invariant" — true and load-bearing at 4D/5D/6D — may not generalize to 3D, where it could instead be parent-family-scoped (like taper) or independently target-specific. This is precisely the kind of real, previously-unknown architectural uncertainty the governing prompt's own decision standard (§43) weighs most heavily: the highest unresolved architectural information value of any candidate examined in this phase.

## 20. 3D authority surface

A first Intermediate×3D projected-target closure would need to independently determine: (a) whether LR-share at 3D is genuinely level/frequency-scoped for the *projected-target* family specifically (as at 4D/5D/6D) or parent-family-scoped (as the TEN_K-vs-HM 3D disagreement suggests is at least possible) — this is itself new architectural learning, not merely a number to freeze; (b) the target's own exact-target fields at 3D (peak-volume band, selected peak, calibration, absolute peak LR — all independently re-derived per this engagement's own established discipline, never assumed transferable from that same target's own 4D values merely because the distance is unchanged); (c) whether `AbsoluteWeeklyIncreaseCapKm=2.0` (agreeing across TEN_K/HM at 3D) is safely reusable as already-shared authority for this scope, which the evidence supports, unlike the LR-share quadruple. This is a real, bounded, single-cell authority-closure exercise of the same shape and size as any prior 4D closure (DIST-GEN.1/5/9) — not larger in scope, but genuinely informative in a way a fourth 4D target is not.

## 21. 3D experiment target selection

Per the governing prompt's own instruction not to activate all three targets at 3D simultaneously, and to weigh external-evidence availability, distance-specific authority burden, interior-range position, and existing-evidence maturity: **16K** is the strongest candidate. Its own DIST-GEN.1 closure reached the highest evidence-tier count of any of the three projected targets (14 `DIRECT_EXISTING_AUTHORITY` / 10 `STRONG_EVIDENCE_DERIVED_DECISION` / 3 `EVIDENCE_INFORMED_PRODUCT_DEFAULT` — the only closure of the three to reach double-digit strong-evidence fields), it was the original, most-tested, longest-public pilot, and choosing the best-evidenced target isolates the frequency variable most cleanly: any authority difference discovered at 16K×3D is more confidently attributable to the frequency axis itself, rather than confounded with residual uncertainty in the target's own 4D closure. 15K (moderate evidence) and 18K (the weakest-evidenced of the three, zero strong-evidence-tier fields) would both make a noisier first frequency experiment.

## 22. Option D — 12K × Intermediate × 4D

The lower-interior structural-parent question DIST-GEN.0 originally disclosed and DIST-GEN.4/8/12 each independently reconfirmed as unresolved. Unlike the other exact-target option (20K), 12K is not merely evidence-thin — it requires answering a *different kind* of question (which structural family owns this distance) before any numeric authority work can even begin.

## 23. 12K parent-boundary uncertainty

Unchanged since DIST-GEN.0 §8-9: whether the immediate 10-12km band genuinely belongs to HALF_MARATHON's own structure, or needs its own distinct structural treatment closer to TEN_K's, has never been evidenced in either direction. `CanonicalDistanceFamilyResolver` classifying 12.0km into HALF_MARATHON is a pure band-boundary classification rule, not approved structural-parent authority for a real target — DIST-GEN.4 §22's own distinction, never revisited with new evidence since.

## 24. 12K evidence availability

No phase since DIST-GEN.0 has gathered dedicated evidence on this specific boundary question, and nothing in this phase's own review supplies any. This remains the single highest-uncertainty, least-evidenced open question in the entire engagement — potentially the most architecturally important one to eventually resolve, but not resolvable by a numeric-authority-closure phase; it would need its own dedicated structural-parent investigation first, with real risk of concluding `BLOCKED` before any 12K number is ever frozen. This is explicitly a different risk profile than 20K's (evidence-thin but structurally safe) or 3D's (structurally safe, bounded, and — per §19 — actively informative).

## 25. Option E — field-specific distance banding

Reassessed, not assumed. No field newly satisfies both halves of the band-definition standard (shared authority + defensible boundary evidence). The two remaining genuine candidates (horizon preferred/maximum) still have zero boundary evidence for where such a band would begin or end — DIST-GEN.12 §32's own worked example (sharing at 15K/16K/18K says nothing about whether the shared behavior starts at 13K or ends at 20K) applies with undiminished force here, since nothing in DIST-GEN.13's implementation-only scope produced any boundary evidence. Three fields that once looked bandable (peak-volume band, selected peak, calibration) remain correctly reclassified by DIST-GEN.12 as consequences of a shared derivation methodology, not band evidence.

## 26. Current band candidates

Per DIST-GEN.12 §31/§55, carried forward unchanged: only horizon preferred (12) and horizon maximum (14) remain genuine (if boundary-unready) band candidates. Every other field is either `LEVEL_FREQUENCY_INVARIANT`, `PARENT_FAMILY_INVARIANT`, `EXACT_TARGET_SPECIFIC` (absolute peak LR, permanently), `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` (peak-volume/selected-peak/calibration), or a generic mechanism — none of which are band candidates at all.

## 27. Band-boundary evidence

None exists for either remaining candidate field, and this phase's own supplemental-research scope (§10) found no reason to expect a targeted search would surface any — the boundary question ("does the shared preferred/maximum horizon apply from 13K to 20K, or some narrower range?") is not the kind of question general training-plan literature answers, since it depends on this engagement's own internal derivation methodology, not external race-distance convention.

## 28. Option F — Intermediate × 5D

A stronger frequency-orthogonality test than 3D, since 5D/6D introduce genuine KEY2/secondary-quality-lane semantics beyond what 3D exercises. Current source confirms 5D and 6D share an identical LR-share quadruple (0.28/0.36/0.28/0.36) and identical non-null starting-LR-cap (12.0) at canonical HM, with TEN_K's own 5D/6D instances agreeing exactly — unlike 3D, the 4D/5D/6D shared-authority pattern (TEN_K matches HM exactly) holds cleanly here, so 5D would not surface the same kind of authority-ownership ambiguity 3D does. It would, however, be a genuinely harder first frequency experiment given the added KEY2 complexity, for less architectural-uncertainty payoff than 3D offers right now.

## 29. KEY2 / secondary-quality cost

5D/6D's KEY2 semantics represent a materially larger training-authority and session-structure surface than 3D's own (which has no secondary-quality lane at all) — this is real, additional complexity a 5D projected-target closure would need to resolve on top of the same exact-target fields 3D would need. Given 3D already offers the higher information-value finding (§19) at lower complexity, 5D is correctly sequenced behind it, not rejected outright.

## 30. Option G — level expansion

Beginner or Advanced projected targets. Highest new-authority burden of any option examined, confirmed again directly from source: HM Advanced 4D's own `ResolvedPeakReference=53.0` is materially different (not a small adjustment) from Intermediate 4D's 43.0, and HM Beginner's own matrix already excludes two of four frequency cells entirely at the canonical level (Beginner 5D `PRODUCT_INELIGIBLE`, Beginner 6D has no cell at all) — meaning a projected-target Beginner closure would need to independently re-verify eligibility at every frequency, per DIST-GEN.12's own explicit warning that a shorter target distance cannot be assumed to inherit HM's own frequency-eligibility pattern.

## 31. Beginner analysis

Real product-safety constraints exist (HM's own Beginner exclusions at 5D/6D), meaning a Beginner projected-target closure carries genuine safety-classification work beyond ordinary numeric authority — determining eligibility itself, not just its values. No evidence exists that this is currently the most valuable open question, and `SOURCE_SILENCE` applies equally here.

## 32. Advanced analysis

Advanced's own materially higher peak-volume authority (53.0 vs 43.0 at HM 4D, a ~23% difference) means an Advanced projected-target closure is not a small extrapolation of Intermediate's own numbers — it would need genuinely independent evidence-gathering at a harder training-load tier, the highest-authority-cost single-cell closure of any option considered in this report.

## 33. Authority-cost matrix

| Row | A: 20K | B: 10-mile | C: 3D (16K) | D: 12K | E: Banding | F: 5D | G: Level |
|---|---|---|---|---|---|---|---|
| Structural parent | Reused, low | N/A (identity only) | Reused, low | **Unresolved — the entire question** | N/A | Reused, low | Reused, low |
| Horizon | New, MEDIUM (thin evidence) | N/A | New, MEDIUM (bounded, precedented) | Blocked on parent | N/A | New, MEDIUM-HIGH | New, MEDIUM |
| Peak volume | New, MEDIUM (thin) | N/A | New, MEDIUM | Blocked | N/A | New, MEDIUM-HIGH | New, HIGH |
| Selected peak | New, MEDIUM (thin, likely near-HM) | N/A | New, MEDIUM | Blocked | N/A | New, MEDIUM-HIGH | New, HIGH |
| Calibration | New, MEDIUM (mechanical, thin) | N/A | New, MEDIUM | Blocked | N/A | New, MEDIUM-HIGH | New, HIGH |
| LR share | Reused (4D shared) | N/A | **Genuinely ambiguous — new architectural question, not just a number** | Blocked | N/A | Reused (5D/6D shared) | New, HIGH |
| Absolute peak LR | New, MEDIUM (thin, likely ~17-18 given monotonic margin pattern, never derived from it) | N/A (reuses 16K's) | New, MEDIUM | Blocked | N/A | New, MEDIUM | New, HIGH |
| Taper | Reused, low | N/A | Reused, low (HM-parent) | Blocked | N/A | Reused, low | Reused, low |
| Pace | Generic, none | N/A | Generic, none | Blocked | N/A | Generic, none | Generic, none |
| Quality structure | N/A | N/A | N/A (no KEY2 at 3D) | N/A | N/A | New, HIGH (KEY2) | New, MEDIUM-HIGH |
| Readiness | Generic, none | N/A | Generic, none | Blocked | N/A | Generic, none | Generic, none |

## 34. Engineering-cost matrix

| Surface | A: 20K | B: 10-mile | C: 3D | D: 12K | F: 5D | G: Level |
|---|---|---|---|---|---|---|
| Authority registry | SMALL (one entry, §5) | MEDIUM (needs new key-shape concept, §17) | SMALL (one entry, new scope owner) | Unknown until parent resolved | SMALL-MEDIUM | SMALL per cell |
| Dispatch | NONE (already generic) | NONE (resolver unaffected until alias approved) | NONE | Unknown | NONE | NONE |
| Tests | SMALL | SMALL if approved | SMALL-MEDIUM (new scope-containment proofs) | Unknown | MEDIUM | MEDIUM |
| Frontend | NONE (authority-only phase) | NONE (authority-only phase) | NONE | NONE | NONE | NONE |
| API | NONE | NONE | NONE | NONE | NONE | NONE |
| Persistence | NONE | NONE (no schema change needed, §17) | NONE | NONE | NONE | NONE |
| Catalog | NONE | NONE | NONE | NONE | NONE | NONE |
| Public activation | Not in this phase | Not in this phase | Not in this phase | Not in this phase | Not in this phase | Not in this phase |

Post-DIST-GEN.13, registry/dispatch cost is uniformly cheap across every option — this axis no longer discriminates between candidates, exactly as DIST-GEN.13's own normalization intended.

## 35. Evidence-risk matrix

| Option | Invented-number risk | Source-sparsity risk | Boundary ambiguity | Identity ambiguity | Frequency interaction | Quality-session complexity |
|---|---|---|---|---|---|---|
| A: 20K | LOW (disciplined process) | HIGH (confirmed) | N/A | N/A | N/A | N/A |
| B: 10-mile | LOW | HIGH (confirmed x4) | N/A | **HIGH (the entire question)** | N/A | N/A |
| C: 3D (16K) | LOW | MEDIUM (some 3D-specific literature likely exists, unresearched) | N/A | N/A | **MEDIUM — the genuinely new finding** | LOW (no KEY2) |
| D: 12K | LOW | MEDIUM-HIGH | **HIGH (the entire question)** | N/A | N/A | N/A |
| E: Banding | HIGH if forced | N/A | HIGH (unresolved) | N/A | N/A | N/A |
| F: 5D | LOW | MEDIUM | N/A | N/A | MEDIUM | HIGH |
| G: Level | LOW | MEDIUM-HIGH | N/A | N/A | HIGH (re-verify eligibility per cell) | MEDIUM (Advanced) |

## 36. Architecture-information-gain matrix

| Option | Unknown it resolves |
|---|---|
| A: 20K | Near-HM convergence — but the answer is already well-predicted by two prior phases; low marginal gain |
| B: 10-mile | Identity-vs-authority precision — unchanged question, four prior examinations, diminishing returns |
| C: 3D (16K) | **Whether LR-share authority ownership is genuinely level/frequency-invariant outside 4D, or parent-family-scoped like taper — a real, just-surfaced, previously uncharacterized ambiguity** |
| D: 12K | Parent-family lower boundary — potentially the highest-value unknown in the whole engagement, but not resolvable by a numeric-authority phase; needs its own structural investigation first |
| E: Banding | Numeric boundary ownership — no new evidence exists to resolve this now |
| F: 5D | Distance × frequency × secondary-quality interaction — real, but sequenced behind 3D given no comparable authority-ambiguity signal was found at 5D/6D (TEN_K and HM already agree there) |
| G: Level | Distance × experience composition — real but the highest-cost, least-differentiated option; safety-classification work dominates over training-science learning |

## 37. Zero-delta-risk matrix

All options are authority-only or governance-only phases (per this phase's own decision standard, §35 of the governing prompt: whichever option wins, the next phase is authority-closure or governance-only, never combined dark/public). DIST-GEN.13 already reduced dispatch risk to near-zero for any new 4D registration (one dictionary entry). A new frequency scope (3D/5D) or level would require a *new* shared-authority component analogous to `IntermediateFourDayGrowthAuthority`, but this is additive, not a modification of the existing one — TEN_K/HM/15K/16K/18K's own 4D behavior is provably unaffected by construction, since nothing in any option touches the existing `IntermediateFourDayGrowthAuthority`, `HalfMarathonParentTaperAuthority`, or any of the three existing `ProjectedTargetAuthority` registrations. Zero-delta risk to existing targets is **LOW across every option** — this axis, like engineering cost, no longer discriminates.

## 38. Fourth-target marginal cost

For 20K specifically: registration cost is now SMALL (§5), but the numeric-closure cost is unchanged and evidence-constrained (§12/§14) — DIST-GEN.13's normalization does not touch training-science evidence availability. For 12K: registration cost is moot, since the structural-parent question must be answered first, and DIST-GEN.13's normalization work says nothing about that question either.

## 39. Frequency marginal cost

For 3D: genuinely smaller than a full fourth exact target in one respect (horizon minimum, taper, and — per §20 — the absolute weekly cap all appear safely reusable), but larger in another (the LR-share quadruple cannot simply be assumed shared at 3D the way it demonstrably is at 4D/5D/6D, per §19's own finding) — a real, bounded, single-cell authority question of the same overall size as a 4D closure, not smaller, not larger, but differently shaped. For 5D: larger than 3D due to KEY2, though the LR-share/starting-cap fields do appear safely reusable at 5D/6D given TEN_K/HM agreement there.

## 40. Authority-alias value

Solving the alias question would deliver real, durable architectural value (identity precision separated from training-authority precision, reusable for any future standard-unit race distance), but is gated entirely on evidence this engagement has searched for four times without success (§16, §18). The architecture gap DIST-GEN.8 identified (§17 of this report) remains open and would need to be built regardless of when this option is eventually pursued — DIST-GEN.13 did not close it, so pursuing this option today carries the same combined evidence-and-architecture cost it always has.

## 41. Three-phase template status

DIST-GEN.12/13 already established, and this phase does not need to re-decide, that the authority-closure → dark-implementation → public-activation sequence is the standard template for any future exact target (`PROJECTED_TARGET_AUTHORITY_DARK_PUBLIC_TEMPLATE_STANDARDIZED`, DIST-GEN.12 §68). Whichever axis is selected here, the next phase is authority-only, never combined with implementation or activation — consistent with this phase's own instruction (§53 of the governing prompt) to keep the next phase narrow.

## 42. Current exact-target registry status

Carried forward from §4: `EXACT_TARGET_REGISTRY_SCALABLE_WITH_CURRENT_NORMALIZATION`. Not reopened, not blocking.

## 43. Option comparison

| | Primary unknown resolved | New structural authority | New numeric authority | Evidence availability | Source-sparsity risk | Architecture learning | Impl. complexity | Public-risk surface | Zero-delta risk | Product-value evidence | Long-term leverage | Selected/Deferred reason |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| A: 20K | Near-HM convergence | None | Yes, MEDIUM | Thin (confirmed) | HIGH | LOW (predictable) | SMALL | None (authority-only) | LOW | Silent | LOW-MEDIUM | Deferred — predictable outcome, low marginal gain |
| B: 10-mile | Identity-vs-authority | Yes, MEDIUM (key shape) | None if approved | Thin (confirmed x4) | HIGH | LOW (unchanged Q) | MEDIUM | None | LOW | Silent | MEDIUM | Deferred — evidence-gated, 4th null result expected |
| **C: 3D (16K)** | **LR-share ownership boundary at non-4D frequency** | **None** | **Yes, MEDIUM (bounded)** | Moderate, unresearched | MEDIUM | **HIGH (new, just-surfaced)** | SMALL | None | LOW | Silent | **HIGH** | **Selected** |
| D: 12K | Parent-family lower boundary | Yes, potentially HIGH | Blocked until resolved | Unresearched (architecture Q) | HIGH | HIGH but confounded | Unknown, likely LARGE | High if pursued | Unknown | Silent | HIGH but unsafe now | Deferred — wrong kind of phase for this question |
| E: Banding | Numeric boundary | None | None (forbidden) | No new evidence | N/A | None (no new evidence) | N/A | N/A | N/A | Silent | N/A | Not selectable — would require inventing a boundary |
| F: 5D | Distance×frequency×KEY2 | None | Yes, MEDIUM-HIGH | Moderate, unresearched | MEDIUM | MEDIUM (no ambiguity signal found) | MEDIUM | None | LOW | Silent | MEDIUM | Deferred — sequenced behind 3D |
| G: Level | Distance×experience | None | Yes, HIGH | Unresearched | MEDIUM-HIGH | LOW-MEDIUM | LARGE | Highest of all options | LOW | Silent | MEDIUM | Deferred — highest cost, safety-classification-dominated |

## 44. Banding result

**`BANDING_STILL_NOT_GOVERNANCE_READY`.** No new boundary evidence exists for either remaining candidate field (horizon preferred/maximum); the three fields that once looked bandable remain correctly reclassified as methodology artifacts, not band evidence. Not selected, and — per the governing prompt's own explicit instruction — not forced.

## 45. Registry result

**`EXACT_TARGET_REGISTRY_SCALABLE_WITH_CURRENT_NORMALIZATION`**, unchanged from DIST-GEN.13. Not a blocker to any option; not reopened.

## 46. Next-axis decision

**`NEXT_AXIS_INTERMEDIATE_3D`.**

This is the one option in §43 that is simultaneously cheap post-normalization (§5, §34), evidence-safe (no architecture question must be answered first, unlike 12K), and genuinely high-information — not because frequency is "the obvious next step now that distance is mature" (the governing prompt's own §46 warning, explicitly not the basis for this decision), but because direct reconstruction of the current numeric matrix in this phase surfaced a specific, previously uncharacterized fact: the level/frequency-invariance property that made growth/cap/LR-share/starting-cap safely shareable at 4D (and, separately, at 5D/6D) does **not** hold uniformly at 3D, where canonical TEN_K and canonical HALF_MARATHON disagree on the LR-share quadruple. This is a real, concrete, previously-unknown architectural question a 3D projected-target closure would directly resolve — whether LR-share ownership at 3D is level/frequency-scoped for the *projected-target* family (as elsewhere) or parent-family-scoped (as taper already is, and as the TEN_K/HM 3D disagreement suggests is at least plausible). No other option offers a comparably concrete, comparably cheap, comparably safe piece of new architectural learning: 20K's likely answer is already predicted by two prior phases (§11-14); 12K's question is real but requires a different, riskier kind of phase (§22-24); 10-mile's evidence has been searched for four times without success (§15-18); banding has no boundary evidence (§25-27); 5D and level are real but costlier without a comparable ambiguity signal motivating them yet (§28-32).

## 47. Exact DIST-GEN.15 handoff

**DIST-GEN.15 — 16K × INTERMEDIATE × 3D — PROJECTED-DISTANCE FREQUENCY-ORTHOGONALITY AUTHORITY CLOSURE.**

Authority-only, per the standardized three-phase template (§41) — no dark implementation, no public activation, no frontend, no API, no database, no catalog change inside that phase. Its scope, narrowly defined per this phase's own §53 instruction (one high-information experiment, not a rollout roadmap): close 16K's own exact-target numeric authority at Intermediate×3D (horizon preferred/maximum, peak-volume band, selected peak, calibration, absolute peak long run), and — as its own primary information objective, not a side effect — directly determine whether the LR-share quadruple at this cell should be treated as level/frequency-scoped (matching TEN_K's own 3D value, `0.38/0.42/0.40/0.42`) or parent-family-scoped (matching canonical HM's own 3D value, `0.34/0.40/0.38/0.46`), using the same evidence-weighing discipline already established for every prior authority closure. It should explicitly confirm whether `AbsoluteWeeklyIncreaseCapKm=2.0` remains safely reusable as already-shared authority at this scope (the one field where TEN_K and HM do still agree at 3D). It must not extend to 15K or 18K at 3D, must not activate anything dark or public, and must not reopen DIST-GEN.13's own normalization work.

## 48. Current product zero-delta

No production, catalog, database, API, or frontend file was changed by this phase. `git status --porcelain` before finalizing shows a diff consisting exactly of this report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md`.

## 49. Remaining blockers

None. DIST-GEN.13's completed architecture was consumed accurately and directly re-verified from current source rather than assumed from its own report's claims (§3-4); public/dark matrices were frozen correctly and not reopened (§2); DIST-GEN.13's normalization work was not reopened, since no new blocking defect was found — the 3D LR-share observation (§19) is new *evidence relevant to the expansion decision*, not a defect in the normalization DIST-GEN.13 actually performed (it never claimed to cover 3D); the reduced post-normalization new-target authority surface was documented (§5); 20K was evaluated specifically as a near-HM boundary probe and found low-marginal-value given its already-predicted outcome (§11-14); 10-mile was evaluated as an explicit authority-alias problem and found evidence-gated for a fourth consecutive time (§15-18); 3D was evaluated as distance×frequency orthogonality and selected on the strength of a genuinely new architectural finding (§19-21); 12K was evaluated as the lower structural-parent boundary and correctly deferred as the wrong kind of phase for an authority-closure track (§22-24); field-specific banding was reassessed without inventing any boundary (§25-27); 5D was evaluated with its KEY2 cost explicitly weighed (§28-29, §39); level expansion was evaluated and found highest-cost (§30-32); implementation ownership (cheap, post-normalization) was never confused with training-science evidence (unchanged) anywhere in this report; exactly one next axis was selected (§46) and exactly one DIST-GEN.15 phase was named (§47); no training number was invented; no production behavior changed; no production source file was touched.

---

## Final classification

**POST_NORMALIZATION_NEXT_PROJECTED_DISTANCE_EXPANSION_DECISION_CLOSED**

Every criterion holds: DIST-GEN.13's completed architecture is accurately consumed directly from current source (§3-4); current public/dark matrices are frozen correctly (§2); no normalization work is reopened — the one new finding (§19) is evidence for the expansion decision, not a defect in DIST-GEN.13's own actual scope; the reduced post-normalization new-target authority surface is fully documented (§5-7); 20K is evaluated specifically as a near-HM boundary probe and found low-marginal-value (§11-14); 10-mile is evaluated as an explicit authority-alias problem and found evidence-gated for the fourth consecutive time, with its architecture gap independently re-verified still open (§15-18); 3D is evaluated as distance×frequency orthogonality and found to carry genuinely new architectural information value (§19-21); 12K is evaluated as the lower structural-parent boundary and correctly identified as needing a different kind of phase (§22-24); field-specific banding is reassessed without inventing any boundary evidence (§25-27); 5D is evaluated with its KEY2 cost explicitly weighed and correctly sequenced behind 3D (§28-29); level expansion is evaluated and found highest-cost with the least differentiated learning (§30-32); shared implementation ownership is never confused with training-science evidence anywhere in this report; implementation convenience (uniformly cheap across every option post-DIST-GEN.13, §34) was explicitly not used as the deciding criterion — the decision instead rests on the one option (3D) carrying genuinely new, concretely-evidenced architectural information value; exactly one next axis is selected (`NEXT_AXIS_INTERMEDIATE_3D`) and exactly one DIST-GEN.15 phase is named (16K × Intermediate × 3D, authority-only); no training number is invented anywhere in this report; no production behavior changes; and no production source file is touched, confirmed directly via `git status` before finalizing.
