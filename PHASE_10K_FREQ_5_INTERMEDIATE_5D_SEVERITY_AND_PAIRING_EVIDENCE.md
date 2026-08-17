# PHASE 10K-FREQ.5 — Intermediate 5D Severity and Pairing Evidence

**Scope:** research/evidence synthesis only. No final thresholds, ratios, role weights, phase pairings, catalog values, or code changes are selected here.

**Final classification:** `INTERMEDIATE_5D_SEVERITY_AND_PAIRING_EVIDENCE_READY_FOR_PRODUCT_DECISION`

## 1. Sources and evidence limits

Repository authorities re-read:

- `appsel-adaptation-v1-canonical-spec (2)-rev 5.md`, especially §7 and §7a.
- `PHASE_10K_FREQ_3_INTERMEDIATE_5D_SECOND_KEY_AUDIT.md`, especially §§C, D, G and H.
- Current catalog artifacts `intermediate-modifier.v6.json` and `ten-k-workout-progression.v5.json`.
- GEN.4C's pre-existing FARTLEK/THRESHOLD prescription-structure gap.

External evidence used:

1. Sperlich, Matzka & Holmberg's review of 175 elite endurance training-intensity distributions found phase-dependent variation, usually more heavy/severe work in competition than preparation, but explicitly concluded that heterogeneous methods and context prevent general prescriptions. Running observations remained predominantly low intensity. [PubMed/Frontiers review](https://pubmed.ncbi.nlm.nih.gov/37964776/)
2. Casado et al. found, in 85 elite/world-class male distance runners, that easy volume, tempo work and short intervals each correlated with performance; easy running was not dispensable merely because it was not classified as deliberate practice. This is observational and cannot assign causal “missed-session weights.” [PubMed](https://pubmed.ncbi.nlm.nih.gov/31045681/)
3. Casado et al.'s integration of scientific literature and results-proven practice describes preparation/competition phase changes and reports 1–2 interval sessions weekly in pre-competition/competition among many world-class 5000 m runners. It is elite-practice evidence, not a recreational 10K pairing trial. [Open-access review](https://pmc.ncbi.nlm.nih.gov/articles/PMC8975965/)
4. Solli et al.'s Norwegian best-practice synthesis reports controlled rather than all-out MIT/HIT, more MIT than HIT, hard–easy rhythmicity, complementary adaptations, and avoidance of consecutive hard days. It is coach/practice synthesis from elite sport, not a randomized KEY1/KEY2 prescription study. [Sports Medicine](https://link.springer.com/article/10.1007/s40279-024-02067-4)
5. Bosquet et al.'s meta-analysis of 27 taper datasets supports reducing volume while retaining intensity and, in its optimal aggregate model, retaining frequency. It does not compare “retain both KEY identities” against “drop one KEY” in a 5-day recreational 10K layout. [PubMed](https://pubmed.ncbi.nlm.nih.gov/17762369/)
6. Lenk et al. found similar improvements from two versus three weekly HIIT sessions in recreational runners. This supports two weekly intensive exposures as plausible, with diminishing returns beyond two; it does not identify phase pairings or prove that KEY1 and KEY2 should be equally hard. [Physiological Reports](https://physoc.onlinelibrary.wiley.com/doi/10.14814/phy2.70573)
7. Stöggl & Sperlich's review and Rosenblat et al.'s meta-analysis support maintaining a large low-intensity base and show that intensity-distribution choices affect outcomes, but neither supplies a per-role adherence severity formula. [Frontiers review](https://pubmed.ncbi.nlm.nih.gov/26578968/), [meta-analysis](https://pubmed.ncbi.nlm.nih.gov/29863593/)

The central limitation is explicit: no direct study located randomizes otherwise-equivalent runners to miss one KEY, one LONG, or one EASY session and derives adaptation-decision thresholds from the result. Literature supports distinct training purposes and aggregate distributions, not an Appsel decision score. Any exact role weight or completion boundary is therefore `UNSOURCED_HEURISTIC / PRODUCT_COACHING_METHODOLOGY` unless future product data validates it.

## Track A — 5D adaptation-severity evidence

## 2. What canonical §7 actually says

Rev5 preserves Rev3's stated ordering: **general severity first, role importance second**.

- 0–1 of 4 completed → Reduce.
- 2 of 4 → Maintain, regardless of role combination.
- 3 of 4 → Progress only if the sole missing role is EASY; otherwise Maintain when KEY or LONG is missing.
- 4 of 4 → Progress.

The rationale is neither purely count-based nor purely role-based. Raw count establishes broad severity tiers; role identity only resolves the incomplete-but-high-adherence 3/4 branch. This ordering fixed the earlier role-first paradox where 0/4 could receive a less severe result than some 2/4 cases. The spec explicitly says the matrix is structural for `1K + 2E + 1L`, not a percentage formula.

Consequently, 5D cannot be obtained safely by replacing `/4` with `/5`. It must preserve the invariant “severity cannot improve as effective completion worsens” while deciding how `2K + 2E + 1L` role information affects each ambiguous count.

## 3. Differential role value: what evidence does and does not support

Evidence supports all three propositions below, but not a numeric scoring rule:

- KEY/quality sessions provide specific moderate/high-intensity stimuli and are a central programming object.
- LONG and EASY volume contribute materially to aerobic development and total distance; “easy” does not mean valueless.
- Training stress and recovery depend on intensity, duration and sequencing, so two KEY slots must not automatically mean two equal maximal stresses.

Casado's correlations argue against assigning EASY a zero or trivial weight. Periodization and intensity-distribution sources argue against treating every session as physiologically interchangeable. Neither establishes that KEY must count, for example, twice as much as EASY, nor whether LONG should tie KEY. Exact differential adherence scoring remains a product/coaching design question.

## 4. Candidate severity models — no selection

### Model A1 — extend the existing severity-first, role-aware branches

Retain count bands first, then use KEY1/KEY2/LONG/EASY identity inside selected ambiguous bands. Candidate questions include whether the top incomplete tier requires LONG plus both KEYs, whether missing KEY1 differs from KEY2, and whether one completed KEY plus LONG is distinguishable from two KEYs without LONG.

Evidence fit: closest semantic continuation of canonical §7; respects non-interchangeable roles. Evidence gap: literature does not define the branches.

New FREQ.6 decisions:

- Count-to-tier boundaries for 0–5.
- Which counts receive role-aware branching.
- Whether KEY1 and KEY2 are symmetric for severity.
- Required role combinations for Progress, Maintain and Reduce.
- Treatment of “only EASY missing” when one versus two EASY sessions are absent.

### Model A2 — normalized completion ratio

Map `completed/5` proportionally onto the four-session bands.

Classification: **naive scaling candidate, not evidence-supported**. It erases the canonical matrix's structural role rationale and creates rounding/tie decisions at ratios with no direct analogue. It should remain rejected unless a future empirical calibration demonstrates that completion fraction predicts sustainable next-window load independently of role.

New FREQ.6 decisions if reconsidered:

- Ratio cut points, inclusive boundaries and rounding.
- Whether role gates override ratio outcomes.
- Calibration dataset and minimum evidence quality.

### Model A3 — role-weighted severity score

Assign weights to KEY1, KEY2, LONG and EASY, then map the score to tiers. It can express differential value and asymmetric KEY roles.

Evidence fit: consistent with non-interchangeable session purposes. Evidence gap: every weight and tier boundary is unsourced. Continuous scores also create false precision.

New FREQ.6 decisions:

- Weight authority and normalization.
- Whether weights vary by phase or prescribed dose.
- Tier cut points and ties.
- Whether safety/non-completion reasons alter weights.
- Versioning when workout identity changes but structural role does not.

### Model A4 — minimum-viable-stimulus gates plus monotonic count floor

First impose a monotonic count-based maximum outcome, then require a phase-relevant set of completed roles for that outcome. Unlike a free weighted sum, failure of a required role cannot be offset by several EASY completions.

Evidence fit: preserves canonical severity-first ordering and recognizes specific stimulus. Evidence gap: “minimum viable set” is a product heuristic; phase-specific gates would couple adaptation authority to workout/phase semantics.

New FREQ.6 decisions:

- The maximum outcome allowed at every completion count.
- Required roles per outcome and whether they vary by phase.
- Whether either KEY can satisfy a generic KEY gate or each identity is required.
- Behavior when a KEY was deliberately down-dosed or substituted.

### Model A5 — vector/state model, no scalar score

Represent `(completed count, KEY1, KEY2, LONG, EASY count)` as a finite decision table. This is the most transparent and testable but has the largest explicit policy surface.

New FREQ.6 decisions: every reachable state, symmetry reductions, invalid-state handling, and governance/version ownership.

## 5. Consecutive-window interaction

Rev5's weekly partitioning remains the correct layer boundary: evaluate each structural 5D week with the eventual 5D policy, then apply unchanged B1 `Reduce < Maintain < Progress` worst-week-wins across the window.

Disclosures for FREQ.6:

- A more sensitive 5D weekly model increases the probability that one week controls a four-week window; B1 amplifies weekly classification choices without changing them.
- Recency-blindness remains intentional; no Track A model should silently add recency weighting.
- Mixed-phase windows matter if role gates are phase-specific: each week must use its own original phase lineage before B1 aggregation.
- Numeric anchor selection remains once per window after aggregation. Track A must not introduce per-week numeric anchors.
- Comparability changes at a 4D→5D boundary: B1 is ordinal, but FREQ.6 must decide/document whether differently calibrated weekly policies share the same ordinal meaning.

No change to weekly partitioning, B1, lineage, safety OR aggregation, or numeric-anchor authority is proposed.

## Track B — KEY1/KEY2 pairing evidence

## 6. General pairing envelope

The evidence supports complementary, controlled stimuli and hard–easy separation more strongly than duplicate all-out stimuli. It does not prove one unique pairing.

Candidate type families, without choosing catalog identities:

- Aerobic/neuromuscular support: strides, short controlled hills, economy/technique-oriented work.
- Threshold/MIT: controlled continuous tempo or intervalized threshold work.
- Severe/HIT: VO2-oriented intervals, controlled and non-all-out.
- Race-specific: 10K-pace rehearsal or mixed race-specific work.
- Taper sharpening: retained intensity/specificity with reduced dose.

“KEY” is a structural role, not proof that every candidate is equally hard. KEY2 may be a secondary controlled stimulus rather than a duplicate of KEY1.

## 7. Phase-by-phase evidence envelope

| Phase | KEY1 candidate types | KEY2 candidate types | Should they differ? | Evidence status |
|---|---|---|---|---|
| Foundation | Controlled threshold introduction; aerobic-strength/hill; economy/strides | Economy/strides; controlled hills; lower-dose threshold-format alternative | Evidence favors avoiding two exhaustive or duplicate sessions. Complementary low-to-moderate stress is plausible; exact pair is not established. | Phase literature supports lower heavy/severe proportion in preparation and delayed overuse of all-out work. Exact pairing is `UNSOURCED_HEURISTIC`. |
| Build | Threshold/MIT; controlled VO2/HIT; progressive aerobic-strength | A complementary threshold format; controlled VO2/HIT if KEY1 is threshold; economy/hill support | Different purpose or fatigue profile is better supported than duplication, while two threshold formats remain an eligible hypothesis. | MIT/HIT are important and controlled formats accumulate work; no direct recreational 10K head-to-head pairing trial. |
| RaceSpecific | 10K-pace/race-specific rehearsal; threshold support; controlled VO2 | Complementary threshold/VO2 support or a second lower-dose race-specific format | Likely differentiation by primary specificity versus supporting stimulus; two identical race rehearsals are not evidenced. | Reviews observe more heavy/severe work and 1–2 interval sessions nearer competition, with substantial variability. Exact identities/doses unselected. |
| Taper | Reduced-dose race-specific sharpening; retained-intensity threshold/interval | Either a second reduced-dose complementary sharpening exposure, a very low-dose economy stimulus, or no second KEY | **Unresolved.** Retaining frequency/intensity supports keeping exposures, but evidence does not prove both structural KEYs must remain, nor that dropping one is optimal. | Bosquet supports retained intensity/frequency with reduced volume in aggregate; translating that to two KEY slots is a new product decision. |

## 8. Taper-specific interpretation

The existing one-KEY rule “rehearsal/content identity retained, dosage reduced” generalizes cleanly to the principle that taper should not turn every specific/intensive exposure into generic easy running. It does **not** independently answer 2-KEY cardinality.

Two evidence-compatible envelopes remain:

- Retain both KEY identities/frequency, reduce the dose of each and preserve separation.
- Retain one primary sharpening KEY and remove/reclassify the secondary KEY to reduce total stress.

Bosquet's aggregate frequency result favors testing the first envelope, but it cannot close the product decision because “training frequency” includes all sessions and the meta-analysis does not isolate two KEY exposures. Selecting either is FREQ.6 work.

## 9. Real catalog constraint versus physiological envelope

Current Intermediate eligibility exposes only three KEY-capable identities: `FARTLEK`, `THRESHOLD_TEMPO`, and `GOAL_PACE_TEN_K`. Two identities are enough for distinct slots, but rotation is narrow. More importantly, FARTLEK and THRESHOLD_TEMPO still lack real interval/repetition prescription structure under the pre-existing GEN.4C gap.

Therefore FREQ.6 must keep two sets separate:

- **Physiologically/evidence-compatible candidate types:** the broad envelope in §7.
- **Currently selectable catalog identities:** only candidates with implementable, validated prescription structure.

Catalog absence is not evidence that a physiological type is undesirable; physiological plausibility is not permission to select an unimplemented artifact. FREQ.6 may have to declare a chosen pairing product-blocked pending catalog enrichment rather than silently bind a name without dosage semantics.

## 10. Complete FREQ.6 decision inventory

### Track A closure decisions

1. Select the 5D severity model.
2. Define exact 0–5 completion-tier outcomes.
3. Identify every role-aware branch and preserve monotonic severity.
4. Decide whether KEY1 and KEY2 are severity-equivalent.
5. Decide LONG's gate/weight relative to each KEY and EASY.
6. Decide whether phase changes severity semantics.
7. Decide how repaired/substituted/down-dosed sessions satisfy role gates.
8. Define every tie/boundary/invalid-state rule.
9. Confirm ordinal compatibility with B1 worst-week-wins and 4D decisions.
10. Assign policy key, version, provenance classification and test matrix.

### Track B closure decisions

11. Select KEY1 and KEY2 purpose/type for Foundation.
12. Select KEY1 and KEY2 purpose/type for Build.
13. Select KEY1 and KEY2 purpose/type for RaceSpecific.
14. Select taper cardinality: retain two versus retain one/reclassify one.
15. Select taper dosage relationship without changing the existing taper authority accidentally.
16. Decide whether KEY identities must differ each week and what counts as sufficiently different.
17. Decide KEY1/KEY2 relative dose and whether “primary/secondary” is structural or phase-specific.
18. Confirm KEY↔KEY and KEY↔LONG spacing authority.
19. Select only catalog-implementable workout identities or explicitly block on catalog enrichment.
20. Close or retain the FARTLEK/THRESHOLD prescription-structure gap explicitly.
21. Define fallback behavior when a phase's preferred pair is unavailable/ineligible.
22. Define progression/rotation across weeks, not merely within one week.
23. Confirm two KEYs do not imply two equal hard sessions.
24. Record evidence basis per selected pairing: direct evidence, evidence-informed extrapolation, or `UNSOURCED_HEURISTIC`.

## 11. Non-decisions preserved

- No 4D threshold was scaled by 5/4 or any ratio.
- No severity threshold, ratio or role weight was selected.
- No phase pairing or workout identity was selected.
- No assumption of equal KEY hardness was made.
- `RUN_LAYOUT_5D`, `WindowExecutionSummaryBuilder`, `NextWindowLoadDecisionPolicy`, and FREQ.4 mechanisms were not changed.
- Worst-week-wins, weekly partitioning and numeric-anchor authority were not reopened.

`INTERMEDIATE_5D_SEVERITY_AND_PAIRING_EVIDENCE_READY_FOR_PRODUCT_DECISION`
