# PHASE 10K-GEN.14 — 2D Progression-Stage Exposure Pacing: Evidence/Authority Derivation (Option 2)

**Parent authority**: `GEN.13` (`DOMAIN_DECISION_REQUIRED`, direction given: proceed with Option 2)
**Phase type**: EVIDENCE + PROVISIONAL NUMERIC/POLICY AUTHORITY — **no production code**
**Execution status**: DONE
**Final classification**: `TWO_D_PROGRESSION_STAGE_EXPOSURE_PACING_AUTHORITY_PROPOSED_PENDING_SIGNOFF`
**Status**: **PROVISIONAL. Not final authority. Phase F must not begin against this proposal until a human sign-off is given and ledgered.**

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `GEN.13`'s full report re-read in full, including its precedent search. `git log -5`, `git fetch && diff HEAD origin/main` (in sync), `git status` clean except the pre-existing unrelated local modifications predating this session. Next free phase ID confirmed unique: `GEN.14`.

## 1. Evidence verification — real sources, not accepted at face value

Per this phase's own explicit instruction, every external claim in the governing prompt was independently re-verified (not treated as pre-approved), the same discipline `GEN.11` applied to its own companion document's 21 sources.

**Claim 2 (biweekly HIT maintenance study) — VERIFIED, real, with an important nuance beyond the prompt's own summary.** Slettaløkken & Rønnestad, *J Strength Cond Res* 2014, "High-Intensity Interval Training Every Second Week Maintains V̇O2max in Soccer Players During Off-Season" (PubMed 24561653). Confirmed real design: 17 semiprofessional soccer players, 6-week off-season, randomized to 1 HIT session/week (HIT 1) vs. 1 HIT session every 2 weeks (HIT 0.5), 5×4-min bouts at 87-97% peak HR. Confirmed result: VO2max maintained equally in both groups (64.0±5.9 vs. 64.3±1.3 mL·kg⁻¹·min⁻¹) — HIT 1 did **not** maintain VO2max better than HIT 0.5. **New finding not in the prompt's summary**: 20-m shuttle-run performance (the more soccer-specific test) was *slightly reduced when both groups were pooled* — i.e. even the *weekly* group did not clearly improve or fully hold the more specific fitness marker over 6 weeks. This *sharpens*, not weakens, the prompt's own caution (point 2's warning against conflating "holds ground" with "builds new capacity"): this population, at either frequency, was in a maintenance-or-slight-decline regime, not a progression regime. Additional caveat found independently: the population is trained semiprofessional soccer players over a 6-week off-season, not novice/recreational 10K runners over an 8-14-week Core cycle — a population and context mismatch that further narrows how much confidence this single study can carry.

**Claims 1 & 4 (weekly frequency threshold; low-frequency running frameworks as maintenance-only) — VERIFIED, and a stronger primary source found than the prompt's own citation.** Spiering, Mujika, Sharp & Foulis, *J Strength Cond Res* 35(5):1449-1458, 2021, "Maintaining Physical Performance: The Minimal Dose of Exercise Needed to Preserve Endurance and Strength Over Time" (PubMed 33629972) — a peer-reviewed narrative review, not a practitioner blog. Confirmed it explicitly frames its whole subject as "the goal of physical training may be to simply **maintain** (rather than improve) physical performance" during frequency-reduced periods, and finds endurance performance can be preserved for up to 15 weeks at as little as 2 sessions/week (or 33-66% volume reduction) — a **maintenance**-scoped finding, never claimed as a progression-equivalence finding. This is the single strongest, most directly on-point source found for the maintenance/progression distinction this proposal turns on, and is adopted as the primary citation over the prompt's own vaguer practitioner-framework citation.

**Claim 3 (novice/recreational populations tolerate fewer high-intensity sessions) — VERIFIED, consistent with this repository's own already-approved authority.** Independent search corroborates the NLstart2run-family evidence already accepted by `GEN.6` (Kluitenberg et al.): higher running *intensity* in the previous week is specifically associated with higher injury risk in novice runners, and novice populations show a trend toward *more* injuries at 3 sessions/week versus 2. This is not new evidence requiring fresh acceptance — it is the same real injury-risk literature `GEN.6` already grounded its Beginner-tier caution in, now confirmed to extend naturally to a lower quality-session-frequency preference for Beginner specifically.

**Claim 5 (no direct progression-under-alternating-week-cadence study exists) — CONFIRMED as a genuine evidence gap, not an unresearched corner.** A deliberate search for any study measuring VO2max/performance *progression* (not maintenance) under a structurally-alternating, zero-quality-session-every-other-week cadence in runners returned no such study. The closest adjacent literature found (Hickson-protocol-style "alternate days" studies) alternates hard/easy *within* the same week at full weekly frequency — a materially different design from 2D's Pattern-B (an entire week with zero quality-session opportunity). **This confirms the prompt's own framing: this is a real gap, and the proposal below is explicitly built as an extrapolation from adjacent maintenance evidence, not a direct progression-equivalence finding.**

## 2. The proposal

**Mechanism — Pattern-A-week-denominated capacity with newly-authored, halved exposure minimums (not Option 1's reuse of existing weekly-calibrated numbers, not Option 3's silent guarantee-weakening):**

For lane 0 (`KEY_SESSION`) in a 2D phase, `ProgressionStageAllocator`'s `availableWeeks` for that lane resolves to the real count of Pattern-A weeks in that phase (not the phase's literal calendar-week count) — this is the mechanical core shared with Option 1. The difference from Option 1, and the reason this is genuinely Option 2: **new, 2D-specific progression-stage content is authored with `MinimumExposures`/`MaximumExposures` values deliberately re-derived to fit that halved denominator**, rather than reusing the existing weekly-frequency catalog's numbers unchanged against a smaller pool (which `GEN.13` already showed would very likely trip `ProgressionPhaseCapacityInsufficientException` for short phases).

**Derivation rule, per stage, per level:**

```
2D_MinimumExposures(stage)  = ceil(WeeklyCadence_MinimumExposures(stage) / 2)
2D_MaximumExposures(stage)  = ceil(WeeklyCadence_MaximumExposures(stage) / 2)
```

...applied to **that same level's own existing weekly-cadence stage content** (Beginner's 2D numbers derive from `BEGINNER_MODIFIER`'s own existing exposure minimums; Intermediate's from `INTERMEDIATE_MODIFIER`'s own), never cross-level. `ceil` (round up, never down) so a stage's floor is never rounded away to zero when a weekly minimum was 1 — preserving that every catalog-declared stage remains genuinely reachable at least once.

**Illustrative worked example (not final — a concrete anchor for sign-off review, using Intermediate's real existing 4D `FOUNDATION` phase minimums as the reference):** if a weekly-cadence `FOUNDATION` phase (3-4 calendar weeks) declares `FOUNDATION_PRIMARY_STAGE` minimum 2 / maximum 3 exposures, the 2D-specific `FOUNDATION_PRIMARY_STAGE` variant would declare minimum 1 / maximum 2 — sized to genuinely fit a Foundation phase whose real Pattern-A week count is roughly half the calendar length (a 4-calendar-week Foundation phase has 2 Pattern-A weeks; a minimum of 1 leaves real headroom, matching the algorithm's own existing compression/extension mechanics unchanged).

## 3. Why this is not Option 1

Option 1 (rejected in `GEN.13`) reinterpreted the *existing* weekly-calibrated numbers against the halved denominator without re-authoring them — `GEN.13`'s own analysis, and evidence point 4 above (no low-frequency running framework claims progression-equivalent pacing at biweekly quality-session frequency), both show this has no supporting precedent: an unmodified weekly-cadence minimum is very likely to simply not fit a Pattern-A-only pool, especially for short phases (`FOUNDATION`'s own 2-week floor could yield as few as 1 real Pattern-A week). This proposal differs precisely because it authors **new** numbers sized to the real constraint, rather than porting old ones onto a denominator they were never calibrated for.

## 4. Why this does not silently weaken the existing safety guarantee

`ProgressionPhaseCapacityInsufficientException`'s fail-closed behavior (a declared minimum that cannot be met throws, rather than silently under-delivering) is **left completely untouched** by this proposal — no change to `ApplyCompression`/`ApplyExtension`/the capacity-check logic anywhere. The new 2D-specific exposure numbers are deliberately chosen so the guarantee is *naturally satisfied* by construction (they fit the real Pattern-A pool by design), not because the check was relaxed. This is the explicit distinction from Option 3, which the evidence above (claim 2's nuance, claim 5's gap) argues against: silently under-delivering a catalog-declared minimum, when no evidence establishes that a reduced count is still adequate, would risk a real, undetectable training-quality regression — exactly the outcome `GEN.13` flagged as unacceptable to decide unilaterally.

**Halving specifically** (rather than, e.g., a 1:1 pass-through or some other ratio) is grounded in: (a) it is the literal, mechanical consequence of Pattern-A weeks being exactly half of calendar weeks under Model B's frozen A/B alternation (`GEN.11` §1) — not an invented multiplier; (b) the maintenance literature (Spiering et al., the biweekly-HIT study) establishes that roughly-halved stimulus frequency is *not* catastrophic for holding physiological ground over multi-week windows comparable in length to a Core phase, which is the closest evidence-grounded anchor available for "is halved-frequency exposure a defensible default," even though it does not establish halved-frequency *progression* is equivalent to full-frequency progression.

## 5. Stated confidence level — explicitly lower than GEN.7/GEN.8/GEN.11's precedented derivations

**LOW-TO-MODERATE confidence, not the same evidentiary tier as `GEN.11`'s `PeakVolumeBand`/`ResolvedPeakReference` work.** `GEN.11`'s numeric derivations extended an already-well-precedented methodology (real, tier-matched, distance-and-frequency-specific external plans; `GEN.7`/`GEN.8`'s own established evidence-envelope process) into adjacent, still-directly-analogous territory. This proposal instead **extrapolates from adjacent maintenance evidence into a progression context no study has directly measured** (evidence point 5, confirmed as a genuine gap, not merely unresearched). The halving rule is defensible and non-arbitrary — but it is a reasoned default under uncertainty, not a measured, evidence-confirmed rate. This is exactly why this phase produces a **provisional** proposal rather than frozen authority.

## 6. Level differentiation

The rule (halve, per-stage, `ceil`, never cross-level) is identical for Beginner and Intermediate — but its **output** differs by construction, since it operates on each level's own already-different weekly-cadence stage content. Beginner's already-more-conservative injury-risk posture (`GEN.6`, reconfirmed independently in §1 above) is preserved automatically: Beginner's weekly-cadence minimums are already lower than Intermediate's, so Beginner's derived 2D minimums inherit that same conservatism without a separate rule being needed.

## 7. Explicit provisional flag

**This proposal is not final authority.** It rests on a genuine, confirmed evidence gap (§1, claim 5) and an extrapolation (§4-5) rather than a direct measurement. Per this phase's own explicit constraint, it is reported back for an actual human/coaching sign-off before any implementation proceeds — the same discipline that sent `GEN.13` itself to escalation rather than letting the allocator's ambiguity be resolved silently. **Phase F does not begin until that sign-off is given and ledgered as final.**

## 8. Governance

No production code, tests, or catalog changes in this phase (evidence/proposal only, per its own explicit scope). `PHASE_LEDGER.md` row appended recording this as a provisional, pending-signoff proposal — not frozen authority. `MASTER_ROADMAP.md` updated to reflect the 2D axis's true current state: Core structurally implemented (`GEN.12`), binding blocked pending human sign-off on this proposal (`GEN.14`), not yet unblocked.
