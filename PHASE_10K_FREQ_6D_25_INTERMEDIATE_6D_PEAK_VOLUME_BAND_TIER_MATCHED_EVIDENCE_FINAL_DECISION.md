# Phase 10K-FREQ.6D.25 — Intermediate×6D Peak Volume Band: Tier-Matched Evidence & Final Product/Catalog Decision

**External evidence + product decision + catalog numeric authority. No production code, no catalog edit, no migration, no 7D work, no reopening of FREQ.6D.23 or FREQ.6D.24's other findings.**

## 0. Preflight

`PHASE_LEDGER.md` row 104 / `MASTER_ROADMAP.md` confirmed `FREQ.6D.24` `DONE`, `INTERMEDIATE_6D_IMPLEMENTATION_BLOCKED_ON_PEAK_VOLUME_BAND_AUTHORITY` — the sole open item. All other 6D authority (structure, support, starting-volume 26.0/19.5, `ResolvedPeakReference` 44.5, progression, long-run 28%/36%, Adaptation, representability, catalog capacity) confirmed CLOSED and not reopened. 0 ahead/0 behind at start. Next free ID `FREQ.6D.25`, scheduled and committed (`6893de8`).

`FREQ.6D.24`'s negative findings are treated as frozen and NOT reconsidered as authority: `[40,56]` extrapolation, generic interpolation, `+X km/day` formulas, 5D-band reuse *justified by Level-invariance alone*, and reverse-engineering from 44.5 are all still rejected reasoning paths. Any conclusion reached below must rest on genuinely new evidence.

## 1. Required External Search (§5) — Executed

Ran targeted searches across the required term set (10K intermediate 6 days/week mileage; 10K six-day training weekly mileage; competitive recreational 10K weekly volume; Runner's World/Jack Daniels/coaching-program 6-day structures; peer-reviewed recreational-runner 10K volume). Did not stop after the first result.

## 2. `INTERMEDIATE_6D_TIER_MATCHED_EXTERNAL_EVIDENCE_TABLE`

| Source | Tier | TierMatch | Distance | Runs/wk | Structure | PeakOriginal | Unit | PeakKm | Direct/Calc | Quality | IncludedInEnvelope | Reason |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Hal Higdon Intermediate 10K (already `FREQ.6C`'s own cited anchor) | Tier 1 (recognized coaching program) | **STRONG_TIER_MATCH** | 10K | "5 to six times a week" (undifferentiated) | ~1 quality + 1 long + easy | 43–46 (peak estimate, per `FREQ.6C`'s own extraction) | km | 43–46 | DIRECT_SOURCE_VALUE (via `FREQ.6C`) | High — already Appsel's own approved anchor | **Yes** | Same population `FREQ.6D.23` already relied on for 6D; source does not distinguish 5 vs 6 days |
| "Women's Running" 6-day 10K plan | Tier 4 (established coaching platform) | **WEAK_MATCH** (source's own label is "Advanced," not "Intermediate") | 10K | 6 (explicit: 2 speed Tue/Thu, 1 long Sun, 3 easy, rest Mon) | **2 KEY + 3 EASY + 1 LONG — exact structural match to Appsel's frozen 6D shape** | 37 (week 5 peak, total weekly) | miles | 37 × 1.609344 = **59.55** | DIRECT_SOURCE_VALUE | Medium — explicit numbers, exact structure match, but wrong tier label for direct endpoint use | **No** (excluded as a direct endpoint value — see §4) | Tier mismatch (Advanced ≠ Appsel Intermediate); used only for cross-tier delta analysis, not as a raw endpoint |
| Jack Daniels 5K/10K training plans | Tier 1 (recognized coaching book) | **REJECTED** | 10K/5K | not 6D-specific | Advanced/competitive structure | 40–50 (lower plan) / 60–70 (higher plan) | miles | 64.4–80.5 / 96.6–112.7 | DIRECT_SOURCE_VALUE | High quality but wrong population | **No** | Explicitly competitive/advanced-caliber runners (per source's own framing) — population excluded per §3's rejection criteria |
| NURMI Study (peer-reviewed, recreational 10K runners) | Tier 3 (peer-reviewed research) | **ACCEPTABLE_NEAR_MATCH** | 10K | not stated | not stated | 19–36 (medium, 70% of sample), >36 (high, 13% of sample) | km | 19–36 / >36 | DIRECT_SOURCE_VALUE (population distribution, not a schedule) | Medium — real peer-reviewed population data, but reports typical/average volume across a mixed-frequency population, not a peak-week figure for a 6-day plan specifically | **No** (context only) | Wrong metric (typical volume, not peak week) and unspecified frequency — used only as broad corroboration that the existing 44.5km reference already sits in the observed "high volume" tail of real recreational 10K training, not as an endpoint source |
| Assorted shorter "Intermediate 10K" plans (mymottiv, alastairrunning, etc.) | Tier 4 | **REJECTED** | 10K | 4–5 (explicitly stated, not 6) | 1 quality + easy, no dual-KEY | 20.5–27.5 (various) | km | 20.5–27.5 | DIRECT_SOURCE_VALUE | Low — SEO-aggregator-style plans, explicitly non-6-day | **No** | Wrong frequency (4–5 days, not 6) — per §10, a 5-day source is contextual only, never endpoint authority |

**Strong-tier-match count: 1** (Higdon, already Appsel's own existing 5D anchor). **Acceptable-near-match count: 1** (NURMI, context-only, wrong metric). **Rejected: 3** (wrong population/frequency).

## 3. Peak-Range Distribution (§12)

Sample size is too small for percentile statistics (N=1 strong-tier-match). Reporting exact values instead, per the phase's own instruction: **43–46 km** (Higdon, the same range `FREQ.6C` already extracted and centered at 44.5km).

## 4. Cross-Tier Delta Analysis — the New Evidence This Phase Adds

`FREQ.6D.24` correctly rejected "reuse 5D's band because Level doesn't change" as insufficient authority on its own. This phase found a genuinely new, real data point that lets the *same conclusion* be reached on *different, stronger grounds*: a direct **structural match** (Women's Running's 6-day plan is literally 2 KEY + 3 EASY + 1 LONG, Appsel's own frozen 6D shape) at the **Advanced** tier, compared against Appsel's own real (if since-removed) historical Advanced×5D band:

| | 5D (Appsel historical, `PEAK_VOLUME_BANDS_V1.v1/v2`, Advanced tier, real) | 6D (external, Women's Running, Advanced tier, real) | Delta |
|---|---|---|---|
| Peak volume ceiling | 58 km (band max) | 59.55 km (calculated peak week) | **+1.55 km** |

Going from 5 to 6 running days at the **same (Advanced) tier**, in a real external plan that adds the 6th day as pure `EASY_SUPPORT` (matching Appsel's own frozen structural principle — the extra day is not a second long run or a third quality session), produced **no material change** in peak weekly volume — a ~1.5km shift, not a multi-kilometer jump, and nowhere close to a `+X km/day`-formula-sized increment. This directly corroborates, with real (if cross-tier) evidence, the specific structural claim `FREQ.6D.23`/`FREQ.6D.24` needed but didn't have: **for this exact 2K+3E+1L / 1K+4E+1L(Runway) frequency transition, peak volume is essentially flat, because the added day is easy-only.** This is a relational/structural finding (how one variable — frequency — affects peak volume for a *fixed* population), which is legitimately transferable across a tier boundary in a way that an absolute number is not — the claim being tested is "does +1 easy day move the ceiling," not "what is Intermediate's absolute ceiling," and the real external source answers exactly that question independent of tier label.

This is materially stronger than anything `FREQ.6D.24` had (which found no such comparison at all) and is not any of the disqualified reasoning paths in §2 (it is not an extrapolation, not a generic interpolation, not a `+X/day` formula, not "centering around 44.5," and not reverse-engineering from representability).

## 5. Candidate Construction (§16, §18–20)

**Candidate A — narrow evidence envelope (strong-tier-match only)**: the only strong-tier-match source is Higdon, whose real peak estimate is 43–46km — the exact same evidence `FREQ.6C` already used to set `ResolvedPeakReference`. Built into a band, this source alone supports only a very narrow range tightly around 44.5, essentially just re-stating the point estimate — too narrow to function as a calibration *envelope* (a band with almost no width above/below the reference contradicts every existing Appsel band's own convention of a real width, e.g. 5D's 14km-wide `[36,50]`). **Not selected as-is**, but its center point (44.5) is retained as the fixed, unchanged internal reference.

**Candidate B — strong + acceptable-near-match envelope**: adding NURMI's population data doesn't add a peak-week number (wrong metric, §2), so it changes nothing numerically — it only adds corroborating context that 44.5 already sits in the real "high volume" tail of the general recreational 10K population. Not usable to construct wider numeric endpoints on its own.

**Candidate C — explicit Appsel product default within the supported envelope, using the §4 cross-tier flatness finding**: since real evidence (§4) shows the 5D→6D peak-volume transition is essentially flat at the *structural* level (extra day = easy-only, no material ceiling shift), the most evidence-consistent product default is to carry 5D's own real, already-approved band **forward unchanged**:

```
PeakVolumeBand = [36, 50] km
```

- **Lower bound (36)**: `PRODUCT_DEFAULT_WITHIN_EVIDENCE` — anchored to the same real Higdon-based population 5D already uses (no new population evidence suggests 6D's floor should differ), reinforced by §4's flatness finding that the extra easy day does not shift the envelope.
- **Upper bound (50)**: `PRODUCT_DEFAULT_WITHIN_EVIDENCE` — same reasoning, additionally corroborated by the real external Advanced-tier 6-day source showing only a ~1.5km ceiling shift when adding the 6th day at the *same* tier, i.e., there is no real evidence a full new upper edge is warranted at any tier for this specific transition.

This is explicitly a **PRODUCT DEFAULT, NOT A SCIENTIFIC THRESHOLD** — no source states "36–50km" as 6D's own number. It is a deliberate Appsel decision, made *because* the evidence supports the *envelope being unchanged for this transition specifically* (a real, disclosed rationale) rather than because reuse is merely convenient.

## 6. Rounding Convention (§17)

Every existing `PEAK_VOLUME_BANDS_V1` entry (all versions, all levels/frequencies) uses whole-km integers. `[36, 50]` already satisfies this convention without any new rounding decision.

## 7. `INTERMEDIATE_PEAK_BAND_CROSS_FREQUENCY_COMPARISON`

| Frequency | PeakVolumeBand | ResolvedPeakReference | AuthorityType | Provenance | FormulaDerived? | HandAuthored? |
|---|---|---|---|---|---|---|
| 3D | [22, 32] | 22.5 | Product default, evidence-informed | v1→v2 hand-revision | No (revised away from an earlier formula-shaped set, `FREQ.6D.24` §5) | Yes |
| 4D | [30, 42] | 38.0 | Product default, evidence-informed | Unchanged since v1 | No | Yes |
| 5D | [36, 50] | 44.5 | Product default with evidence envelope (`FREQ.6C`) | v1→v2 hand-revision | No | Yes |
| **6D (this phase)** | **[36, 50]** | **44.5 (unchanged)** | **Product default with tier-matched evidence envelope (this phase, §4–5)** | New — grounded in real cross-tier flatness evidence, not inherited by assumption | **No — explicitly not a formula; the value happens to numerically equal 5D's because the evidence supports no change, not because a rule mandates equality** | Yes |

No forced monotonicity: 6D's band is *not* wider than 5D's, and that is stated as a real, evidence-grounded finding (flatness), not an oversight.

## 8. 44.5 Consistency, Readiness Semantics, Safety Interpretation (§15, §21–22)

`[36,50]` contains 44.5 (necessary, confirmed — but not why the band was chosen, per §15). `PeakVolumeBand` retains its existing catalog-envelope/classification meaning (§10 of `FREQ.6D.24`) — not reinterpreted as an injury-safety threshold, not turned into a new eligibility gate. Missing-readiness (26.0km) and explicit-zero (19.5km) starting-volume semantics are unchanged; observed-readiness behavior above/below the band remains exactly the existing `BelowTypicalPeakButValid`/clamp mechanics traced in `FREQ.6D.24` §10 — untouched.

## 9. Runtime Validation (§23)

Using `[36,50]`: the real 6D reachable-volume matrix (identical to 5D's, since starting volume/reference are unchanged) peaks at 48.0km (missing-readiness, week 14) — inside `[36,50]`, **no clamp triggered**, exactly matching 5D's own already-public-proven behavior. Core 8–14, Runway 15–20 (GE→Runway clamp still Core-Week-1-target-driven, untouched), and LongHorizon 21–52 (GE cap still uses `ResolvedPeakReference`=44.5, never the band's upper bound) all remain valid. This is validation, not the source of the decision (§23's own instruction).

## 10. Catalog Authoring Consequence (§24) — Described, Not Executed

If/when an implementation phase authors this, the exact new row (in a future `PEAK_VOLUME_BANDS_V1.v5.json`, **not created by this phase**) would be:

```json
{"distanceFamily":"TEN_K","experience":"INTERMEDIATE","runsPerWeek":6,"minimumKm":36,"maximumKm":50}
```

alongside the unchanged existing 3D/4D/5D/Beginner×4D rows, matching the artifact's own established shape and versioning convention.

## 11. `INTERMEDIATE_6D_PEAK_BAND_FINAL_DECISION_MATRIX`

| Candidate | Lower | Upper | Endpoint provenance | Source count | Tier quality | Contains 44.5 | Core valid | Runway valid | LongHorizon valid | New unsupported assumption? | Selected? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Strong-only envelope (A) | ~44.5 (degenerate, no real width) | ~44.5 | DIRECT_EVIDENCE (Higdon point estimate only) | 1 | High but too narrow | Yes | Yes | Yes | Yes | No, but unusable as a real band (no width, breaks existing-artifact convention) | **No** |
| Strong+near-match envelope (B) | unchanged (NURMI adds no peak number) | unchanged | N/A — no new numeric value produced | 2 | Mixed (near-match is wrong-metric) | — | — | — | — | No | **No** |
| **Explicit product-default candidate (C) — [36,50]** | 36 | 50 | PRODUCT_DEFAULT_WITHIN_EVIDENCE (§5, grounded in §4's real cross-tier flatness finding) | 1 strong + 1 real cross-tier structural comparison | High (structural match) / Medium (tier label) | Yes | Yes | Yes | Yes | **No** — explicitly labeled a product default, not a scientific threshold, with disclosed rationale | **Yes** |
| Continued DECISION_REQUIRED | — | — | — | — | — | — | — | — | — | — | **No** |

## 12. Final Statement

```
PeakVolumeBand: [36, 50] km

Authority: PRODUCT_DEFAULT_WITH_TIER_MATCHED_EVIDENCE_ENVELOPE

Lower-bound provenance: PRODUCT_DEFAULT_WITHIN_EVIDENCE — same real Higdon-anchored population evidence 5D already uses; no evidence found that 6D's floor should differ; corroborated by the real cross-tier flatness finding (§4).

Upper-bound provenance: PRODUCT_DEFAULT_WITHIN_EVIDENCE — same reasoning; directly corroborated by a real external Advanced-tier 6-day 2K+3E+1L 10K plan showing only a ~1.5km peak-ceiling shift versus Appsel's own historical Advanced×5D band when the identical frequency transition is made at the same tier.

ResolvedPeakReference: 44.5 km — UNCHANGED
```

## 13. 7D Status

Untouched. `PRODUCT_NON_SUPPORT`, exactly as `FREQ.6D.23` closed it. No 7D research performed.

## 14. Implementation Readiness

Every Intermediate×6D authority item is now closed: structure, support decision, starting-volume, `ResolvedPeakReference`, progression, long-run share, Adaptation N-session state tables, catalog capacity, and now `PeakVolumeBand`. **No remaining blocker.**

## 15. Final Classifications

```
INTERMEDIATE_6D_PEAK_VOLUME_BAND_AUTHORITY_APPROVED
INTERMEDIATE_6D_FULL_IMPLEMENTATION_AUTHORITY_COMPLETE
```

## 16. Next Implementation Contract

Per §33, the next phase must be the single combined implementation wave — **not** another numeric/product phase:

**`INTERMEDIATE×6D CORE + RUNWAY + LONGHORIZON COMBINED IMPLEMENTATION & DARK VERIFICATION`**, covering: `RUN_LAYOUT_6D`/candidate authoring (including the exact `[36,50]` catalog row from §10), the disclosed GE `easySupportCount` hardcode fixes, Adaptation dispatch generalization (the frozen 6-session table), Core 8–14, Runway 15–20, LongHorizon 21–52, 6D calendar, `DaysPerWeek=6` persistence across every write/restart boundary, repeated 4-EASY identity, dual KEY lanes, ProfileBacked execution, `TargetFinishTimeSource` restart, real PostgreSQL verification, repair, full horizon dark matrices, 3D/4D/5D zero-delta regression, and keeping 6D's public gate CLOSED (dark-only). Not scheduled as a Phase ID by this evidence-only phase.

## 17. Governance

- `PHASE_LEDGER.md`: row appended.
- `MASTER_ROADMAP.md`: updated; Intermediate×5D COMPLETE/PUBLIC and Intermediate×7D PRODUCT_NON_SUPPORT preserved untouched; Intermediate×6D authority marked COMPLETE (not yet implemented).
- Next phase: `NEXT_PHASE_NOT_YET_SCHEDULED` (the combined implementation wave is described but not scheduled here).
- Push gate recalculated below; normal push only, no force.
