# PHASE FIVE-K.0B — Canonical FIVE_K Product-Default Resolution for Restricted V1 Matrix

**Type:** PRODUCT DECISION / AUTHORITY FINALIZATION (no external research, no new literature search, no production implementation, no runtime wiring, no public activation, no frontend change, no database change, no catalog JSON change, no new workout implementation, no sub-5K work, no TEN_K/HM value copying, no numeric interpolation, no silent defaults).

---

## 1. FIVE-K.0A binding state

Binding inputs consumed, read in full before any decision: `PHASE_FIVE_K_0A_TARGETED_EXTERNAL_EVIDENCE_CLOSURE_FOR_CANONICAL_FIVE_K_RACE_PLAN_AUTHORITY.md`, `FIVEK_0A_TARGETED_AUTHORITY_SYNTHESIS.md`, and all five `FIVEK-LIT-0{1..5}_*.md` documents. Binding state: `FIVE_K_TARGETED_EXTERNAL_EVIDENCE_CLOSURE_COMPLETE`; authority completeness `FIVE_K_CANONICAL_AUTHORITY_CLOSED_FOR_RESTRICTED_V1_MATRIX`; runtime readiness `FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_WAITING_ON_PRODUCT_DECISIONS`. No external search was performed in this phase; every decision below is bounded strictly by the ranges/options FIVE-K.0A's own five LIT documents and synthesis already established.

**Genuine numbering discrepancy disclosed up front (not silently reconciled):** the governing prompt's own topic sections (§10-40) label PD-FIVEK-01 through 08 with *different subject matter* than FIVE-K.0A's own report §43 assigns to those same IDs (e.g. the governing prompt's "PD-FIVEK-05" heading is "PEAK VOLUME," but FIVE-K.0A's own §43 PD-FIVEK-05 is "exact long-run share value"; the governing prompt's "PD-FIVEK-08" heading is "PUBLIC MATRIX," but FIVE-K.0A's own PD-FIVEK-08 is "whether V1 includes a dedicated VO2max-interval/faster-than-5K workout tier"). Per this phase's explicit instruction ("extract PD-FIVEK-01 through PD-FIVEK-08 exactly as written in FIVE-K.0A... do not reconstruct from memory"), **this report uses FIVE-K.0A's own §43 numbering as ground truth** throughout, and treats the governing prompt's topic sections as substantive guidance to apply when resolving each decision, not as a relabeling authority. This is reported as a genuine surprise to the caller, structurally analogous to FIVE-K.0A's own disclosed 6/15-vs-7/14 table/prose mismatch in FIVE-K.0.

## 2. Exact PD-FIVEK-01..08 decision set (reproduced verbatim from FIVE-K.0A §43/§42, cross-checked against the synthesis doc §2)

| DecisionId | AuthorityField(s) | Evidence-supported range/options | Evidence classification | Runtime requirement | What remains undecided | Why a deterministic choice is needed |
|---|---|---|---|---|---|---|
| PD-FIVEK-01 | CoreCycle Min/Preferred/Max weeks | 8-13 structured weeks (Higdon 8W low end; Pfitzinger/Hansons 10-12W mid-cluster; Brooks/Hansons-affiliated 9W) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | `RaceHorizonPolicy` FiveK branch | Exact triple | No source states a deterministic triple the way TEN_K's 8/12/14 or HM's 10/14/16 are stated |
| PD-FIVEK-02 | Foundation/Build/RaceSpecific/Taper week allocation | ~60-70% Foundation+Build, ~30-40% RaceSpecific+Taper of chosen CoreCycle (Hansons' explicit "6-8 weeks... then about four weeks of sharpening") | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | Phase-duration table | Exact per-phase min/preferred/max | Only proportion, not absolute per-phase counts, is stated |
| PD-FIVEK-03 | V1 public Level×Frequency scope | Intermediate × {4D, 5D} only; Beginner/Advanced/other frequencies `INSUFFICIENT_EVIDENCE` | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` for Intermediate×4D/5D | `V1CatalogPilotIdentityPolicy` FiveK arm | Whether 4D-only or 4D+5D is the deterministic V1 matrix ("loosely 5D" is not sufficient) | Evidence closes a band for the cell, not a binary matrix decision |
| PD-FIVEK-04 | Peak-volume value for chosen V1 cell(s) | Approximate mid-30s-to-mid-40s km/week band, Intermediate×4D/5D only (derived, not directly cited) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | `ResolvedPeakReference` | Exact numeric band bounds + selected reference per cell | No source gives a defensible exact km figure |
| PD-FIVEK-05 | Exact long-run share value | Generic ~20-30% band (up to ~35% in one weaker source), explicitly cross-distance | `CLOSED_MULTI_SOURCE_SYNTHESIS` (band); exact value is PD | `LongRunPreferred*Share`/`LongRunHardCapShare` | Exact share value(s) within band | Band applicability is closed; exact value is not |
| PD-FIVEK-06 | Taper duration (1 vs 2 weeks) and exact multiplier(s) | ~1-2 week duration band; ~0.40-0.60 remaining-volume band (40-50% reduction 5K-specific, 41-60% general) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | `TaperVolumeMultiplier(s)` | Exact week count + exact multiplier(s) | No deterministic value stated by any source |
| PD-FIVEK-07 | Whether to add an explicit absolute long-run ceiling | Share-only governance plausible (smaller 5K total volume structurally bounds absolute LR) OR explicit Advanced-informed ceiling (Higdon's single 19.3km data point) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | `PreferredAbsolutePeakLongRunKm` (nullable) | Ceiling or null | Both options are legitimate; TEN_K's own real precedent leaves it null |
| PD-FIVEK-08 | Whether V1 includes a dedicated VO2max-interval/faster-than-5K workout tier | VO2/interval centrality for 5K is `CLOSED_MULTI_SOURCE_SYNTHESIS` (LIT-05 Claim C); faster-than-5K/repetition work is `OPTIONAL_SUPPORTED` (LIT-05 Claim D) | Mixed — see §19 decomposition | Workout authoring scope for FIVE-K.1 | Whether either/both new workouts are in V1 scope | New catalog workout required if adopted; evidence treats the two sub-tiers differently |

**Decomposition disclosure (not a ninth decision):** FIVE-K.0A's own §43 bundles VO2max/interval and faster-than-5K/repetition under one PD-FIVEK-08 label, but its own LIT-05 classifies them with *different* evidence grades (Claim C: VO2/interval centrality is `CLOSED_MULTI_SOURCE_SYNTHESIS`, not optional; Claim D: faster-than-5K/repetition is `OPTIONAL_SUPPORTED`, not required). Resolving PD-FIVEK-08 honestly requires treating these as two sub-questions (PD-FIVEK-08a, PD-FIVEK-08b) within the same named decision — this is the same "mixed-row decomposition" discipline the governing prompt already requires at the matrix level (§3), applied here to one PD pack item rather than inventing new numbered authority. See §19.

## 3. Normalized 15-row authority matrix

Reproducing FIVE-K.0A §39's 15-row table (itself FIVE-K.0's own table-verified 15, not the prose-claimed 14), now resolved to final status by this phase:

| # | AuthorityField | FIVE-K.0A status | FIVE-K.0B resolution | Final classification |
|---|---|---|---|---|
| 1 | CoreCycle min/pref/max | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | PD-FIVEK-01: 8/10/13 weeks | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| 2 | Phase durations (FIVE_K) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | PD-FIVEK-02: full table, §12-13 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (proportions) / `EXPLICIT_PRODUCT_DEFAULT` (individual phase split within each proportion pair) |
| 3 | Run-layout RunsPerWeek set | `SOURCE_SILENCE_REMAINS` | §10: {4,5} for Intermediate only | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (closed by overlap rule, not "all frequencies valid") |
| 4 | Public Level×Frequency matrix | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` (Intermediate×4D/5D); `SOURCE_SILENCE_REMAINS` elsewhere | PD-FIVEK-03: Intermediate×{4D,5D} V1; all else deferred | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (selected cells); `DEFERRED_INSUFFICIENT_EVIDENCE` (all other cells, not required for selected V1) |
| 5 | Workout-progression stages | `CLOSED_MULTI_SOURCE_SYNTHESIS` | Frozen 5-stage V1 sequence, §16 | `MULTI_SOURCE_SYNTHESIS` (unchanged) |
| 6 | Exposure min/preferred/max | `SOURCE_SILENCE_REMAINS` | §17: mechanically derived from phase allocation | `EXPLICIT_PRODUCT_DEFAULT` |
| 7 | Growth-rate cap values | `CLOSED_SHARED_AUTHORITY_CONFIRMED` | Reuse generic mechanism/values unchanged | `SHARED_AUTHORITY` (unchanged) |
| 8 | Peak-volume band/reference | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` (Intermediate); silent elsewhere | PD-FIVEK-04: band 35.0-44.0km; 4D=38.0, 5D=42.0 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (band + per-cell selected reference, kept distinct) |
| 9 | Calibration/start volume | `CLOSED_SHARED_AUTHORITY_CONFIRMED` | §24: mechanism shared; fixture-default number mechanically derived (4D=25.0, 5D=28.0) | `SHARED_AUTHORITY` (mechanism) / `EXPLICIT_PRODUCT_DEFAULT` (fixture number) |
| 10 | Long-run share quadruple | `CLOSED_MULTI_SOURCE_SYNTHESIS` (band); exact value PD | PD-FIVEK-05: 0.25/0.30/0.28/0.33 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| 11 | Absolute long-run ceiling | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | PD-FIVEK-07: `NO_FIVE_K_SPECIFIC_ABSOLUTE_LR_CEILING_REQUIRED`, null | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (null is the decision, not an omission) |
| 12 | Taper duration/multiplier | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` | PD-FIVEK-06: 1 week, multiplier 0.55 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| 13 | Target-race pace role | `CLOSED_MULTI_SOURCE_SYNTHESIS` | Unchanged; frozen into RaceSpecific-phase workout | `MULTI_SOURCE_SYNTHESIS` (unchanged) |
| 14 | Threshold/VO2/interval role | `CLOSED_MULTI_SOURCE_SYNTHESIS` (roles); faster-than-5K separate PD | PD-FIVEK-08a/b, §19 | `MULTI_SOURCE_SYNTHESIS` (roles) / `EXPLICIT_PRODUCT_DEFAULT` (V1 scope inclusion/exclusion) |
| 15 | FIVE_K catalog workout content | `EVIDENCE_INFORMED_PRODUCT_DEFAULT_REQUIRED` (VO2-interval scope) + `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` (engine) | §48-49: feasibility mapped, 2 small catalog additions named conceptually | `EXPLICIT_PRODUCT_DEFAULT` (scope) / `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` (engine, unchanged) |

All 15 rows now carry a terminal classification for the selected V1 matrix. Zero rows remain `SOURCE_SILENCE`/`DECISION_REQUIRED`/`CONFLICTING_EVIDENCE` for any field the selected V1 cells actually require.

## 4. Mixed-row decomposition

Three rows required explicit decomposition to avoid conflating evidence authority with product-selected values:

- **Row 2 (Phase durations):** the Foundation+Build / RaceSpecific+Taper *proportion* (60-70%/30-40%) is `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (Hansons' explicit breakdown). The *individual* Foundation-vs-Build split and RaceSpecific-vs-Taper split within each proportion pair has no sub-evidence and is `EXPLICIT_PRODUCT_DEFAULT`.
- **Row 8 (Peak volume):** the band (35.0-44.0 km/week) is `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (derived from LIT-01 Claim D's cross-checked approximate band). The per-cell `SelectedPeakReference` (38.0 for 4D, 42.0 for 5D) is a separate, narrower `EVIDENCE_INFORMED_PRODUCT_DEFAULT` choice within that band — never merged into one authority claim.
- **Row 14 (Threshold/VO2/interval role):** the *role/placement* (threshold=foundational, VO2=central/race-specific) is `MULTI_SOURCE_SYNTHESIS`, unchanged. The *V1-inclusion scope decision* (build VO2max-interval workout: yes; build faster-than-5K/repetition workout: no) is a separate `EXPLICIT_PRODUCT_DEFAULT` layered on top — see §19's PD-FIVEK-08a/08b split.

## 5. Product-default decision standard

Applied throughout: evidence defines the allowable space (a range, a band, an option set); this phase chooses one deterministic point within that space. No decision below cites "easier to code," "same as TEN_K," "matches HM," "midpoint seems reasonable," or "catalog already contains it" as its rationale — each decision's "why" is recorded in §12-19/§40 and traces to a specific evidence claim, a specific mechanical/arithmetic derivation from an already-made decision, or an explicit disclosed product-minimalism principle (restricted-V1 discipline). No decision is upgraded to `DIRECT_EVIDENCE`; the two available post-approval labels are `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (evidence privileges a sub-range or a direction) and `EXPLICIT_PRODUCT_DEFAULT` (evidence is genuinely silent and determinism requires a choice).

## 6. Restricted-V1 principle

Applied: the smallest coherent FIVE_K V1 matrix with *complete* authority is preferred over maximal breadth. Beginner/Advanced and all non-4D/5D frequencies are deferred without blocking. Within Intermediate, both 4D and 5D are retained only because §7 shows 5D is governable without inventing any new authority (KEY2 absent, extra day reuses the already-closed EASY family) — not because the engine happens to support 5D generically.

## 7. 4D vs 5D analysis

| Dimension | Intermediate 4D | Intermediate 5D |
|---|---|---|
| Evidence coverage | LIT-01 Claim D band covers "4-5 days/week" jointly; no source independently isolates 4D | Same joint band; notably, Higdon's own *directly cited* Intermediate plan is literally 5 days/week — 5D is, if anything, the more directly-anchored point within the joint band, a nuance FIVE-K.0A's own prose ("4D, loosely also 5D") understates and is disclosed here as a genuine finding |
| Peak-volume authority | Within shared band, SelectedPeakReference=38.0 | Within shared band, SelectedPeakReference=42.0 |
| Long-run authority | Shared share-based mechanism; no frequency-specific authority needed since share is volume-relative, not frequency-relative | Same mechanism; absolute long-run km differs only as a mechanical consequence of the different SelectedPeakReference, not as independent new authority (same "mechanical consequence vs. independent confirmation" distinction already established in this program's DIST-GEN.12 precedent, applied here, not derived from it) |
| Workout progression | KEY1 only (single quality slot/week) | KEY1 identical to 4D; 5th day is `EASY` (KEY2 absent) |
| KEY2 requirement | N/A (4-day week has no KEY2 concept) | Resolved: absent. No source supports independent frequency-differentiated long-run/quality authority (LIT-01 Claim C, LIT-02 Claim D), and the only evidence-backed candidate content for a second quality day (faster-than-5K/repetition) is itself deferred as `OPTIONAL_SUPPORTED` (PD-FIVEK-08b) — governing a 5D KEY2 cleanly without inventing content is possible precisely because the extra day is EASY, not a new quality tier |
| Exposure authority | §17 table applies | Same §17 table (exposures are phase-derived, frequency-agnostic) |
| Calendar support | Existing generic calendar/adjacency rules (`SHARED_AUTHORITY`, unchanged) support any RunsPerWeek value already | Same |
| Catalog support | `goal-pace-five-k` + `vo2-interval-five-k` small additions (shared across both cells) | Same two additions; no 5D-specific new workout needed |
| Remaining product assumptions | SelectedPeakReference exact value; exposure counts | Same, plus the explicit KEY2-absent decision |
| Authority completeness | Complete for selected V1 | Complete for selected V1 |

No numeric score was used; both cells reach the same authority-completeness state once PD-FIVEK-01 through 08 are resolved, and 5D requires zero additional invented authority beyond what 4D already requires.

## 8. Final V1 matrix

**`FIVE_K_V1_MATRIX_INTERMEDIATE_4D_AND_5D`** — deterministic, not "loosely 5D." Deciding factor: §7's governability analysis shows 5D's only distinguishing requirement (a KEY2 policy) resolves cleanly to "absent, extra day is EASY" using already-closed authority (the EASY family, LIT-05 Claim E) plus an already-made deferral (PD-FIVEK-08b), with zero new invented content — not because the catalog engine happens to support arbitrary frequencies.

## 9. Source-silence runtime consequence

Two residual `SOURCE_SILENCE_REMAINS` items from FIVE-K.0A §41, resolved to runtime consequence (not evidence):

- **Exact exposure counts:** `CAN_BE_CLOSED_BY_EXPLICIT_PRODUCT_DEFAULT` — see §17. Exposure counts are mechanically derived from the already-decided phase-week allocation (§12-13), not from counting sample-plan session occurrences (which FIVE-K.0A explicitly forbade treating as authority, LIT-04 Claim G).
- **Independent (non-level-conflated) RunsPerWeek authority:** `CAN_BE_CLOSED_BY_EXPLICIT_PRODUCT_DEFAULT` — see §10. Resolved as the overlap of evidence-supported structure (LIT-01 Claim D's "4-5 days/week"), Appsel's existing frequency model, catalog capability, and volume/long-run authority coverage — explicitly `{4, 5}` for Intermediate only, not "all frequencies are valid."

## 10. Exposure-count decision

See §17 for the full stage-by-stage table. Classification: `EXPLICIT_PRODUCT_DEFAULT` throughout — every exposure count is a direct mechanical consequence of the phase-week allocation already decided in PD-FIVEK-02 plus the evidence-ordered stage sequence (threshold before VO2, VO2 before race-specific rehearsal), not an independently evidenced number and not derived by counting sample-plan sessions.

## 11. RunsPerWeek decision

Supported set: **{4, 5}**, Intermediate level only. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`. All other RunsPerWeek values (2D, 3D, 6D, 7D) for Intermediate, and every RunsPerWeek value for Beginner/Advanced/Experienced, remain `OUT_OF_V1_SCOPE` / `INSUFFICIENT_EVIDENCE` / excluded (Experienced, unchanged) — not silently expanded to universal support.

## 12. PD-FIVEK-01 horizon

**Decision: CoreCycle = {Minimum: 8, Preferred: 10, Maximum: 13} weeks.**

- Minimum=8: directly evidenced low end (Higdon, all three levels, reputable direct source).
- Maximum=13: top of the evidence-supported 8-13 week range. **A genuine internal inconsistency in FIVE-K.0A §43 is disclosed here, not silently used:** that section offers two "evidence-supported options," `{min=8,pref=10,max=13}` and `{min=9,pref=12,max=14}`, describing both as "within the 8-13W evidence range" — but 14 is arithmetically outside the 8-13 range FIVE-K.0A itself states. This phase resolves the inconsistency conservatively, selecting only the option that is actually within the stated evidence range, per this engagement's "evidence defines allowable space, never expand beyond it" discipline, rather than silently using the wider, internally-unsupported figure.
- Preferred=10: grounded directly in Hansons' own explicit stated breakdown — "6-8 weeks of increasing distances at pace... followed by about four weeks of sharpening" — whose stated *minimum* sums to exactly 6+4=10 weeks. This is the only source in the corpus that states its own internal phase breakdown explicitly; anchoring Preferred to it (rather than to Pfitzinger's bare 12-week figure, which carries no internal breakdown) keeps the phase-allocation decision in §13 traceable to a real stated structure rather than an invented proportion split.

Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 13. PD-FIVEK-02 phase allocation

At CoreCycle Preferred=10, using Hansons' own 6+4 breakdown: Foundation+Build=6, RaceSpecific+Taper=4. Taper is fixed at 1 week (§14/PD-FIVEK-06 below — chosen in part *because* a 1-week taper leaves RaceSpecific=3 weeks at preferred, enough to meaningfully represent both the VO2-continuation and race-specific-rehearsal sub-stages; a 2-week taper would compress RaceSpecific to 2 weeks, too tight for both). Foundation/Build split (6 weeks, no direct sub-evidence): Foundation=2, Build=4 — Build gets the larger share because it hosts both the threshold-development and the start of VO2-interval-development stages, while Foundation is explicitly "workout-content-free" per Daniels (LIT-04 Claim E) and does not need comparable length at the preferred horizon.

| Phase | Min (@CoreCycle=8) | Preferred (@10) | Max (@13) |
|---|---|---|---|
| Foundation | 1 | 2 | 5 |
| Build | 3 | 4 | 4 |
| RaceSpecific | 3 | 3 | 3 |
| Taper | 1 | 1 | 1 |
| **Total** | **8** | **10** | **13** |

Classification: proportion `EVIDENCE_INFORMED_PRODUCT_DEFAULT`; individual Foundation/Build split and RaceSpecific/Taper split `EXPLICIT_PRODUCT_DEFAULT` (§4).

## 14. Compression

Order: **Foundation first (down to its floor of 1 week), then Build (down to its floor of 3 weeks), then RaceSpecific (floor 3, never actually reached within the 8-13W supported range), Taper never compressed (fixed at 1 week at every horizon).** Foundation-first is grounded in LIT-04 Claim E (Foundation is the most elastic/compressible element, directly attributed to Daniels' own description of Phase I as workout-content-free) — `EVIDENCE_INFORMED_PRODUCT_DEFAULT`. Build-second has no direct cross-source evidence ranking it above RaceSpecific specifically; it is ordered second because it is the next-most-general phase before race-specific/taper content begins — `EXPLICIT_PRODUCT_DEFAULT`. Taper-never is grounded in the same Claim E ("taper is the least compressible").

## 15. Extension

**Extension ownership: Foundation absorbs 100% of weeks above Preferred, up to Maximum.** At CoreCycle Max=13 (+3 weeks above Preferred=10), Foundation grows from 2 to 5; Build/RaceSpecific/Taper stay fixed at their preferred values (4/3/1). This is not an arbitrary "Foundation-first" convention copied from TEN_K/HM: it is grounded in LIT-04 Claim A's own finding that Daniels' total plan length varies (8-24 weeks across sources) specifically because Daniels' own longer variant adds an extended, optional, non-workout-prescribing Phase I/Foundation window for runners without an existing base — the same phase this phase already identified as elastic. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 16. PD-FIVEK-03 workout progression

Frozen restricted-V1 stage sequence (5 stages; faster-than-5K/repetition explicitly excluded, §19):

| StageKeyCandidate | Phase | Objective | WorkoutFamily | RelativeOrder | CompressionPriority |
|---|---|---|---|---|---|
| `FIVE_K_AEROBIC_BASE` | Foundation | General aerobic development | `EASY` | 1 | Highest (compresses first) |
| `FIVE_K_THRESHOLD_DEVELOPMENT` | Build | LT/threshold capacity | `QUALITY` (`threshold-tempo`, existing) | 2 | Medium |
| `FIVE_K_VO2_INTERVAL_DEVELOPMENT` | Build→RaceSpecific boundary | VO2max/aerobic-ceiling development | `QUALITY` (new: `vo2-interval-five-k`, small catalog addition) | 3 | Medium-low |
| `FIVE_K_RACE_SPECIFIC_REHEARSAL` | RaceSpecific | Race-pace rehearsal | `QUALITY` (new: `goal-pace-five-k`, small catalog addition, structural analog of `goal-pace-ten-k`) | 4 | Low |
| `FIVE_K_TAPER_ACTIVATION` | Taper | Maintain sharpness at reduced volume | `QUALITY` (reused `goal-pace-five-k` at reduced dose) / `EASY` | 5 | Lowest (never compressed) |

Sequence/objective classification: `MULTI_SOURCE_SYNTHESIS` (unchanged from FIVE-K.0A). Stage-to-family mapping for threshold/race-pace/taper: `EXISTING_WORKOUT_SUPPORTS_AUTHORITY` (threshold) / structural-analogy-supported (race-pace). VO2-interval family: catalog gap, named conceptually only, not authored (§48-49).

## 17. Stage exposures

Mechanically derived from §13's phase-week table (not from counting sample-plan sessions). Build weeks split Threshold-first/VO2-second (threshold is the evidence-ordered prerequisite, LIT-04 Claim F); RaceSpecific's 3 weeks (fixed at every horizon) split 1 week VO2-continuation / 2 weeks rehearsal (rehearsal is the later-sequenced, race-proximate stage per Claim A).

| StageKeyCandidate | Min exposures (@CoreCycle=8) | Preferred (@10) | Maximum (@13) |
|---|---|---|---|
| `FIVE_K_AEROBIC_BASE` | N/A — ambient Foundation backdrop, not a discrete quality exposure | N/A | N/A |
| `FIVE_K_THRESHOLD_DEVELOPMENT` | 2 | 2 | 2 |
| `FIVE_K_VO2_INTERVAL_DEVELOPMENT` | 2 (1 Build + 1 RaceSpecific) | 3 (2 Build + 1 RaceSpecific) | 3 (unchanged — Build/RaceSpecific don't change between Preferred and Max; all extension goes to Foundation) |
| `FIVE_K_RACE_SPECIFIC_REHEARSAL` | 2 | 2 | 2 |
| `FIVE_K_TAPER_ACTIVATION` | 1 | 1 | 1 |

Classification: `EXPLICIT_PRODUCT_DEFAULT` for every numeric exposure value; `NOT_REQUIRED` for `FIVE_K_AEROBIC_BASE`'s exposure field (it is the ambient default, not a scheduled quality slot — null is semantically correct here, not an omission).

## 18. KEY1

KEY1 (the single structured-quality slot present every week at 4D and 5D) is **phase-driven**: its content is whichever stage from §16 is active for the current phase/week (Threshold during its Build weeks, VO2-interval during its Build/RaceSpecific weeks, Rehearsal during remaining RaceSpecific weeks, Taper-Activation during the Taper week). This reuses the same generic "phase-dependent KEY1 content" *shape* TEN_K/HM already use — shape reuse only, no FIVE_K-specific numeric value borrowed. Classification: `SHARED_AUTHORITY` (mechanism/shape).

## 19. KEY2

**Decision: KEY2 is ABSENT at 5D.** The 5th weekly session is an additional `EASY` run, not a second quality slot. Reasoning: (a) no source independently varies long-run or quality-session authority by frequency (LIT-01 Claim C, LIT-02 Claim D — both `SOURCE_SILENCE`), so no FIVE_K-specific evidence exists to govern *what* a second quality day should contain; (b) the only evidence-backed candidate content for a genuinely distinct second quality tier — faster-than-5K/repetition pace work — is independently classified `OPTIONAL_SUPPORTED`, not required (LIT-05 Claim D), and this phase defers it from V1 scope entirely (PD-FIVEK-08b, below); (c) easy running is independently `STRONG_MULTI_SOURCE_SYNTHESIS`-closed as the dominant, always-safe volume category (LIT-05 Claim E). Making the 5th day EASY therefore requires inventing zero new authority, while making it a second quality day would require inventing session content this phase has no evidence basis to invent. This is the direct, disclosed reason 5D is governable cleanly (§7-8) rather than forcing 4D-only.

**Decomposition of PD-FIVEK-08 (not a ninth decision — see §2):**
- **PD-FIVEK-08a (VO2max/interval tier):** **INCLUDED in V1.** Role/centrality is `MULTI_SOURCE_SYNTHESIS`-closed and required for 5K specifically (LIT-05 Claim C — distinct from, and more central than, the optional faster-than-5K tier). Scope decision to actually author it for V1 (accepting a small catalog addition) is `EXPLICIT_PRODUCT_DEFAULT`.
- **PD-FIVEK-08b (faster-than-5K/repetition tier):** **DEFERRED, excluded from V1.** `OPTIONAL_SUPPORTED`, not required (LIT-05 Claim D — at least one complete, reputable published plan, Higdon, across all three levels, ships without it). Classification: `EXPLICIT_PRODUCT_DEFAULT`, chosen for restricted-V1 minimalism (governing-prompt §16-19 instruction to keep the V1 workout model minimal), not because it is "extra work."

## 20. PD-FIVEK-04 pace/intensity

Easy: dominant volume, phase-agnostic `EASY` family — `MULTI_SOURCE_SYNTHESIS`, unchanged. Threshold: Build-phase, `threshold-tempo` family — `MULTI_SOURCE_SYNTHESIS`, unchanged. VO2/Interval: Build→RaceSpecific boundary, new `vo2-interval-five-k` family (small catalog addition) — role `MULTI_SOURCE_SYNTHESIS`, V1-inclusion `EXPLICIT_PRODUCT_DEFAULT` (PD-FIVEK-08a). 5K race pace: RaceSpecific phase, late-sequenced, `goal-pace-five-k` analog family — `MULTI_SOURCE_SYNTHESIS`, unchanged. Faster-than-5K/repetition: excluded from V1 (PD-FIVEK-08b). Fitness-evidence hierarchy and Riegel projection: unchanged `SHARED_AUTHORITY`. Goal-time separation (goal time does not manufacture fitness; current fitness comes from the governed fitness-evidence hierarchy): unchanged `SHARED_AUTHORITY`, reconfirmed, frozen where 5K target pace appears (RaceSpecific phase only, never Foundation/Build).

## 21. 80/20 non-rule

**No strict ratio rule introduced.** `EXACT_INTENSITY_DISTRIBUTION_RATIO_NOT_REQUIRED_FOR_V1_AUTHORITY` is recorded explicitly. FIVE-K.0A found the exact quantitative intensity-distribution ratio (e.g. 80/20) to be actively, genuinely contested in the literature itself (`CONFLICTING_EVIDENCE`, LIT-05 Claim F) — no 80/20 validator, exact easy/hard percentage, or weekly intensity quota is added by this phase. Only the qualitative role placement already closed by synthesis (easy dominant, threshold/VO2/race-pace minority, late-sequenced) is retained.

## 22. Target-race pace

5K race pace is frozen to appear exclusively within the RaceSpecific phase (`FIVE_K_RACE_SPECIFIC_REHEARSAL` and, at reduced dose, `FIVE_K_TAPER_ACTIVATION`), never in Foundation or Build — consistent with LIT-05 Claim A's "race-specific, late-sequenced" finding. Goal time does not manufacture fitness: current fitness continues to come from the unchanged, shared fitness-evidence hierarchy (recent-race → time-trial → structured-test → normal-pace → effort-fallback); no FIVE_K-specific override is introduced.

## 23. PD-FIVEK-05 peak volume

**Decision: PeakVolumeBandMin=35.0 km/week, PeakVolumeBandMax=44.0 km/week** (an exact numeric framing of FIVE-K.0A's prose "approximate mid-30s-to-mid-40s km/week" band — disclosed honestly as this phase's own numeric framing of that prose description, not as a directly-evidenced edge value). **SelectedPeakReference: 4D=38.0 km/week; 5D=42.0 km/week** — differentiated within the shared band because more running days correlates with higher sustainable volume with no FIVE_K-specific contradiction (LIT-01 Claim C), while both values stay inside the undifferentiated evidence band (the band itself does not vary by frequency — only the selected point within it does). Classification: band `EVIDENCE_INFORMED_PRODUCT_DEFAULT`; per-cell selected reference `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, kept as a separate, narrower classification from the band per §4.

## 24. Peak band vs selected reference

Explicitly kept distinct, never merged: PeakVolumeBand (35.0-44.0, `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, applies identically to both cells) is the evidence-bounded envelope; SelectedPeakReference (38.0 for 4D, 42.0 for 5D) is this phase's own deterministic point-selection within that envelope, separately classified and separately justified (§23).

## 25. Calibration volume

No FIVE_K-specific calibration/starting-volume *authority* exists or is needed (LIT-01 Claim E, `CLOSED_SHARED_AUTHORITY_CONFIRMED_BY_ABSENCE_OF_CONTRARY_EVIDENCE` — the generic current-volume-adaptation mechanism applies unchanged). However, the runtime schema's `GoldenFixtureStartingVolumeKm` field is a non-nullable `double` (confirmed by direct read of `VolumeSafetyPolicy.cs`), so a concrete fixture-default number is still required even though no independent *authority* governs its exact value. This phase derives it mechanically from already-decided values rather than inventing an unrelated number: using the shared `PreferredMaxWeeklyIncreaseRatio` (0.07, unchanged generic value) compounded over the 6 preferred Foundation+Build ramp weeks (§13), Start ≈ Peak / (1.07)^6 ≈ Peak / 1.50.

- 4D: 38.0 / 1.50 ≈ **25.0 km/week**
- 5D: 42.0 / 1.50 ≈ **28.0 km/week**

**Disclosed numeric coincidence:** 4D's derived value (25.0) is numerically identical to HM's own `GoldenFixtureStartingVolumeKm` (25d). This is flagged explicitly as coincidence of the shared arithmetic formula applied to FIVE_K's own independently-selected peak, not as evidence of authority equality or as a copied value — consistent with this engagement's established "value equality ≠ authority equality" discipline (the same discipline FIVE-K.0A applied to TEN_K's 0.53 taper multiplier). Classification: mechanism `SHARED_AUTHORITY`; the resulting fixture number `EXPLICIT_PRODUCT_DEFAULT`.

## 26. Growth/cutback

Reuse the existing generic growth-cap mechanism and values unchanged (`PreferredMaxWeeklyIncreaseRatio`, `HardMaxWeeklyIncreaseRatio`, `AbsoluteWeeklyIncrementCapKm`) — LIT-01 Claim F confirmed no FIVE_K-specific deviation exists or is warranted. Cutback authority: reuse the existing generic mechanism; no FIVE_K-specific evidence found either way, inherits the shared default unchanged. Classification: `SHARED_AUTHORITY` for both (this is reuse of a generic, distance-agnostic mechanism/value set already confirmed applicable by FIVE-K.0A — not "copying a TEN_K-owned value into FIVE_K," since the mechanism was never TEN_K-owned in the first place).

## 27. PD-FIVEK-06 long run

PreferredMinimumShare=0.25, PreferredMaximumShare=0.30, SelectionShare=0.28, HardCapShare=0.33 — see §28. AbsoluteCeiling=null — see §29. StartingCap=null — see §30. QualityInLongRun=false (easy-only) — see §31. ProgressionRule: reuse the existing generic position-in-phase-weighted progression mechanism unchanged (`SHARED_AUTHORITY`).

## 28. LR share

**Decision: LongRunPreferredMinimumShare=0.25, LongRunPreferredMaximumShare=0.30, LongRunSelectionShare=0.28, LongRunHardCapShare=0.33.** SelectionShare is placed in the upper-middle of the primary 20-30% band (not the literal midpoint 0.25) because FIVE_K's overall weekly volume is comparatively low in absolute terms (35-44 km/week) — per LIT-02 Claim C's own structural reasoning, a slightly higher share still keeps the absolute long-run distance safely small (0.28 × 38-42 ≈ 10.6-11.8 km at SelectionShare; 0.33 × 38-42 ≈ 12.5-13.9 km even at HardCapShare), reinforcing rather than straining the share-only governance decision in §29. HardCapShare (0.33) sits above the stronger-tier source's 30% ceiling but still within the full disclosed evidence universe (the weaker-tier source's ~35% figure, explicitly down-weighted per LIT-02's own source-tiering but not discarded) — this keeps the hard cap inside the evidence actually reviewed rather than inventing a margin. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 29. Absolute ceiling

**Result: `NO_FIVE_K_SPECIFIC_ABSOLUTE_LR_CEILING_REQUIRED`.** AbsoluteCeiling (`PreferredAbsolutePeakLongRunKm`) = **null**, consistent with TEN_K's own real precedent of leaving this field unset. Grounded in LIT-02 Claim C: 5K's structurally smaller total weekly volume naturally bounds absolute long-run size even without an explicit ceiling (computed worst case ≈13.9 km at 5D's HardCapShare — far below HM's 19.0 km ceiling and well inside any plausible safety margin). Not invented "because HM owns one" — the opposite: explicitly declining to add a ceiling HM-style, because the evidence-consistent reasoning (and TEN_K's own real precedent) supports leaving it null. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (the null is the decision, not an omission).

## 30. Starting LR

**StartingAbsoluteLongRunCapKm = null.** No FIVE_K-specific starting-cap authority exists or is needed (LIT-02 Claim E, `CLOSED_SHARED_AUTHORITY_CONFIRMED_BY_ABSENCE_OF_CONTRARY_EVIDENCE`); the generic current-conditioning-based starting-long-run governance applies unchanged, and this field is nullable in the existing schema (unlike `GoldenFixtureStartingVolumeKm`), so no concrete number need be invented. This also mirrors TEN_K's own real precedent of leaving this field null at every frequency (confirmed by direct schema read and by this program's own DIST-GEN.19A finding that "TEN_K remains null at every frequency") — reuse of mechanism and of the null convention, not value-copying. Classification: `SHARED_AUTHORITY`.

## 31. Quality-in-LR

**QualityInLongRun = false (long run stays controlled/easy).** No evidence found either way for embedding quality content in the FIVE_K long run specifically; per the restricted-V1 minimalism principle, the simpler option (controlled/easy long run) is selected rather than adding unsupported complexity. Classification: `EXPLICIT_PRODUCT_DEFAULT`.

## 32. PD-FIVEK-07 taper

TaperWeeks=1 — see §33. TaperVolumeMultiplier=0.55 — see §34. Quality-retention semantics and activation content — see §35.

## 33. Taper duration

**Decision: 1 week.** Within the ~7-14 day / 1-2 week evidence band (LIT-03 Claim A, `STRONG_MULTI_SOURCE_SYNTHESIS`). Selected specifically because, at CoreCycle Preferred=10, a 1-week taper leaves RaceSpecific at 3 weeks (§13) — enough to meaningfully represent both the VO2-continuation and race-specific-rehearsal sub-stages (§17); a 2-week taper would compress RaceSpecific to 2 weeks, leaving only one week for each sub-stage, which this phase judges too thin given both are independently `MULTI_SOURCE_SYNTHESIS`-closed as required (not a "fits the math better" rationale alone — it is a genuine training-design tradeoff between taper length and race-specific-stage representation). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 34. Taper multiplier

**Decision: TaperVolumeMultiplier = 0.55** (single value, non-chained — consistent with the 1-week taper decision; `TaperVolumeMultipliers` list left null, reusing TEN_K's own single-multiplier *shape*, not its 0.53 *value*). 0.55 remaining-volume corresponds to a 45% reduction, landing inside the narrower, 5K-specific secondary source's 40-50% reduction range (rather than the wider, general cross-endurance 41-60% range) — this phase explicitly prioritizes the more population-specific evidence over the more general evidence, per this engagement's own source-hierarchy standard, rather than taking a blind "midpoint of everything." 0.55 is numerically distinct from both TEN_K's 0.53 and HM's 0.70/0.43 — no coincidence to flag here. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 35. Taper quality

Intensity/frequency are relatively preserved while volume drops (LIT-03 Claim C, `STRONG_MULTI_SOURCE_SYNTHESIS`, qualitative). Concretely: `FIVE_K_TAPER_ACTIVATION` reuses the already-closed `goal-pace-five-k` race-pace workout family at reduced dose/volume (not a new invented workout), rather than dropping to pure easy running — this satisfies the evidence's qualitative principle without inventing new exact content for the genuinely silent "activation-workout content" question (LIT-03 §38, `SOURCE_SILENCE_REMAINS`). Classification: qualitative principle `MULTI_SOURCE_SYNTHESIS` (unchanged); exact reduced-dose reuse decision `EXPLICIT_PRODUCT_DEFAULT`.

## 36. PD-FIVEK-08 public matrix

See §8 (final matrix), §7 (4D vs 5D analysis), §19 (PD-FIVEK-08a/08b decomposition that made the matrix decision possible). Final: **Intermediate × {4D, 5D}**, all else deferred/out of scope.

## 37. Deferred Beginner

**`DEFERRED_TO_FIVEK_LIT_06_OR_FUTURE_EXPANSION`.** No numeric peak-volume or long-run band exists for Beginner at any frequency (LIT-01 §8, `INSUFFICIENT_EVIDENCE`). Not reopened; does not block the selected V1.

## 38. Deferred Advanced

**`DEFERRED_TO_FIVEK_LIT_06_OR_FUTURE_EXPANSION`.** Same reasoning as Beginner — only a single-source, single-level data point exists (Higdon's 19.3 km peak long run), insufficient for independent Advanced-tier authority (LIT-02 Claim B). Not reopened; does not block the selected V1.

## 39. Experienced exclusion

Unchanged. "Experienced" remains excluded from V1 scope for FIVE_K exactly as it is for every other distance in this program; not reopened, no new finding.

## 40. Product-default ledger

| DecisionId | Field | Evidence-supported options | Selected value | Why selected | Authority classification | Future reconsideration trigger |
|---|---|---|---|---|---|---|
| PD-FIVEK-01 | CoreCycle Min/Pref/Max | 8-13W range; no deterministic triple stated | 8 / 10 / 13 weeks | Min=Higdon's direct 8W; Max=top of stated evidence range (14 explicitly rejected as exceeding FIVE-K.0A's own stated range); Preferred=Hansons' own explicit 6+4=10 breakdown | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | FIVEK-LIT-06 or new primary-source access to Daniels/Pfitzinger full text revealing a stated deterministic triple |
| PD-FIVEK-02 | Phase allocation | 60-70%/30-40% proportion only | Foundation/Build/RaceSpecific/Taper table, §13 | Hansons' explicit 6+4 split at Preferred; Foundation/Build sub-split follows Daniels' "workout-content-free Foundation" finding | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (proportion) / `EXPLICIT_PRODUCT_DEFAULT` (sub-split) | New source stating an explicit Foundation-vs-Build or RaceSpecific-vs-Taper split |
| PD-FIVEK-03 | V1 Level×Frequency scope | Intermediate×{4D,5D} band-level only; "loosely 5D" | Intermediate × {4D, 5D}, deterministic | §7-8/§19: 5D governable with zero invented authority once KEY2=absent and faster-than-5K is deferred | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | FIVEK-LIT-06 closing Beginner/Advanced, or new evidence independently varying frequency within Intermediate |
| PD-FIVEK-04 | Peak-volume band + selected reference | Approximate mid-30s-mid-40s km/week band | Band 35.0-44.0; 4D=38.0, 5D=42.0 | Band = numeric framing of evidence prose; selected refs differentiated by frequency per Claim C's general more-days-more-volume correlation | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | FIVEK-LIT-06 or any source independently measuring FIVE_K peak volume by exact frequency |
| PD-FIVEK-05 | Long-run share quadruple | Generic 20-30% (up to ~35% weaker tier) band | Min=0.25, Max=0.30, Selection=0.28, HardCap=0.33 | Selection placed upper-middle per Claim C's absolute-volume-safety reasoning (not literal midpoint); HardCap bounded by the full reviewed evidence universe including the weaker-tier 35% figure | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | New source independently measuring FIVE_K long-run share by frequency |
| PD-FIVEK-06 | Taper duration + multiplier | ~1-2 week duration; ~0.40-0.60 remaining-volume band | 1 week; multiplier 0.55 | Duration chosen to preserve 3-week RaceSpecific phase for 2 required sub-stages; multiplier prioritizes 5K-specific narrower reduction range over general wider range | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | New FIVE_K-specific primary-source taper study, or a future 2-week-taper V1 expansion decision |
| PD-FIVEK-07 | Absolute long-run ceiling | Share-only OR explicit Advanced-informed ceiling | Share-only; AbsoluteCeiling=null | Claim C's structural volume-safety reasoning; mirrors TEN_K's own real null precedent; worst-case computed LR (≈13.9km) is already far below any plausible ceiling | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | If Advanced tier is later added (larger absolute volumes may re-open the ceiling question) |
| PD-FIVEK-08a | VO2max/interval tier inclusion | Central/required per Claim C | Included in V1; new `vo2-interval-five-k` workout (small catalog addition) | Claim C: centrality/placement is `MULTI_SOURCE_SYNTHESIS`-closed, not optional, for 5K specifically | Role `MULTI_SOURCE_SYNTHESIS`; inclusion-for-V1 `EXPLICIT_PRODUCT_DEFAULT` | None identified — unlikely to reverse given Claim C's evidence strength |
| PD-FIVEK-08b | Faster-than-5K/repetition tier inclusion | `OPTIONAL_SUPPORTED`, not required | Excluded from V1 | Claim D: at least one complete reputable plan (Higdon) ships without it; restricted-V1 minimalism | `EXPLICIT_PRODUCT_DEFAULT` | A future V2/expansion phase explicitly scoping Advanced-tier speed work |

**Forbidden-rationale audit:** none of the eight rows above (or their sub-decompositions) cite "easier to code," "same as TEN_K," "matches HM," "midpoint seems reasonable," or "catalog already contains it." Two numeric coincidences with TEN_K/HM were found during derivation (CalibrationStartingVolumeKm 4D=25.0 matching HM's own fixture value; none for the taper multiplier, which is explicitly distinct at 0.55) — both are disclosed explicitly as coincidences of a shared, previously-confirmed-applicable generic mechanism applied to FIVE_K's own independently-selected inputs, never used as justification for selecting that value.

## 41. Final authority matrix

See §3's normalized 15-row table for the row-level view. Consolidated by field, for the selected Intermediate×{4D,5D} V1 cells only (every required field below has a terminal, non-silent classification):

| Field | Classification | Null/absent, and why |
|---|---|---|
| Phase taxonomy | `SHARED_AUTHORITY` | — |
| CoreCycle Min/Pref/Max | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| Phase-duration table | `EVIDENCE_INFORMED_PRODUCT_DEFAULT`/`EXPLICIT_PRODUCT_DEFAULT` | — |
| Compression order | `EVIDENCE_INFORMED_PRODUCT_DEFAULT`/`EXPLICIT_PRODUCT_DEFAULT` | — |
| Extension ownership | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| Workout-progression stages | `MULTI_SOURCE_SYNTHESIS` | — |
| Exposure counts | `EXPLICIT_PRODUCT_DEFAULT` | `FIVE_K_AEROBIC_BASE` exposures `NOT_REQUIRED` (ambient, not a scheduled slot) |
| KEY1 semantic owner | `SHARED_AUTHORITY` | — |
| KEY2 (5D) | `EXPLICIT_PRODUCT_DEFAULT` | Absent by design — extra day is `EASY`, not a second quality slot |
| Easy/threshold/race-pace/VO2 roles | `MULTI_SOURCE_SYNTHESIS` | — |
| Faster-than-5K tier | `EXPLICIT_PRODUCT_DEFAULT` | Excluded from V1 — not a missing field, an explicit scope decision |
| Intensity-distribution ratio | `NOT_REQUIRED` | No ratio rule needed; qualitative role placement suffices |
| Fitness-evidence hierarchy / goal-time separation / Riegel | `SHARED_AUTHORITY` | — |
| PeakVolumeBand | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| SelectedPeakReference (per cell) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| CalibrationStartingVolumeKm (fixture default) | `EXPLICIT_PRODUCT_DEFAULT` | — |
| GrowthAuthority (ratios/cap) | `SHARED_AUTHORITY` | — |
| CutbackAuthority | `SHARED_AUTHORITY` | — |
| LongRun share quadruple | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| AbsoluteCeiling | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | Null by design — share-only governance decision |
| StartingAbsoluteLongRunCapKm | `SHARED_AUTHORITY` | Null — reuses TEN_K's real null precedent + generic mechanism |
| QualityInLongRun | `EXPLICIT_PRODUCT_DEFAULT` | False — kept simple, no evidence either way |
| LR ProgressionRule | `SHARED_AUTHORITY` | — |
| TaperWeeks | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| TaperVolumeMultiplier | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| Taper quality/activation | `MULTI_SOURCE_SYNTHESIS`/`EXPLICIT_PRODUCT_DEFAULT` | — |
| Public V1 matrix | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| RunsPerWeek supported set | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | — |
| Calendar/adjacency rules | `SHARED_AUTHORITY` | — |
| Catalog engine dispatch readiness | `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY` | Unchanged, confirmed ready |

Zero `SOURCE_SILENCE` / `DECISION_REQUIRED` / `CONFLICTING_EVIDENCE` remain for any field required by the selected V1 matrix.

## 42. Final FIVE_K master contract

- **Distance:** `FiveK`
- **CoreCycle:** Minimum=8, Preferred=10, Maximum=13 weeks
- **Phases:** Foundation {1/2/5}, Build {3/4/4}, RaceSpecific {3/3/3}, Taper {1/1/1} (Min/Preferred/Max weeks, §13)
- **Supported RunsPerWeek:** {4, 5}, Intermediate level only
- **Compression policy:** Foundation first → Build second → RaceSpecific (floor, not reached in-range) → Taper never (§14)
- **Extension policy:** Foundation absorbs 100% of weeks above Preferred (§15)
- **Structural intent:** short-horizon, VO2max-central, race-specific preparation distinct from TEN_K/HM's own tiering (reaffirmed from FIVE-K.0A §45)
- **WorkoutProgressionKey candidate:** `FIVE_K_WORKOUT_PROGRESSION_V1` (naming review deferred to FIVE-K.1, per FIVE-K.0A §45 — not created here)
- **Selected V1 public matrix:** Intermediate × {4D, 5D}; Beginner/Advanced/Experienced and all other frequencies deferred/excluded (§8, §37-39)

Every field above cites its authority classification in §41; no required field is TBD.

## 43. Final workout-progression contract

See §16-17 tables in full (StageKeyCandidate, Phase, Objective, WorkoutFamily, Min/Preferred/MaximumExposures, RelativeOrder, CompressionPriority). KEY1 = phase-driven (§18, `SHARED_AUTHORITY`). KEY2 (5D only) = absent, extra day `EASY` (§19, `EXPLICIT_PRODUCT_DEFAULT`). No unresolved placeholders remain for the selected V1 matrix.

## 44. Final volume contract

Per Intermediate×4D/5D cell: PeakVolumeBand=35.0-44.0 km/week (`EVIDENCE_INFORMED_PRODUCT_DEFAULT`, shared); SelectedPeakReference=38.0 (4D)/42.0 (5D) (`EVIDENCE_INFORMED_PRODUCT_DEFAULT`); CalibrationStartingVolumeKm=25.0 (4D)/28.0 (5D) (`EXPLICIT_PRODUCT_DEFAULT`, mechanically derived, §25); GrowthAuthority=generic shared ratios/cap, unchanged (`SHARED_AUTHORITY`); AbsoluteGrowthCap=generic shared value, unchanged (`SHARED_AUTHORITY`); CutbackAuthority=generic shared mechanism (`SHARED_AUTHORITY`).

## 45. Final long-run contract

PreferredMinimumShare=0.25, PreferredMaximumShare=0.30, SelectionShare=0.28, HardCapShare=0.33 (`EVIDENCE_INFORMED_PRODUCT_DEFAULT`, §28); AbsoluteCeiling=null, `NO_FIVE_K_SPECIFIC_ABSOLUTE_LR_CEILING_REQUIRED` (`EVIDENCE_INFORMED_PRODUCT_DEFAULT`, §29); StartingCap=null (`SHARED_AUTHORITY`, §30); ProgressionRule=generic shared mechanism, unchanged (`SHARED_AUTHORITY`); QualityInLongRun=false (`EXPLICIT_PRODUCT_DEFAULT`, §31). No ambiguous "TBD."

## 46. Final taper contract

Weeks=1 (`EVIDENCE_INFORMED_PRODUCT_DEFAULT`, §33); Anchor=single, non-chained (reuses TEN_K's own shape, not value); Multiplier=0.55 (`EVIDENCE_INFORMED_PRODUCT_DEFAULT`, §34); Quality behavior=intensity/frequency relatively preserved, reduced-dose race-pace touch retained (`MULTI_SOURCE_SYNTHESIS`/`EXPLICIT_PRODUCT_DEFAULT`, §35); Activation semantics=reuse of `goal-pace-five-k` family at reduced dose, not new content (`EXPLICIT_PRODUCT_DEFAULT`). No TBD.

## 47. Final pace contract

Easy=dominant, `EASY` family, all phases (`MULTI_SOURCE_SYNTHESIS`). Threshold=Build phase, `threshold-tempo` (`MULTI_SOURCE_SYNTHESIS`). VO2/interval=Build→RaceSpecific boundary, new `vo2-interval-five-k` (role `MULTI_SOURCE_SYNTHESIS`; inclusion `EXPLICIT_PRODUCT_DEFAULT`). 5K race pace=RaceSpecific phase, late-sequenced, `goal-pace-five-k` (`MULTI_SOURCE_SYNTHESIS`). Faster-than-5K=excluded from V1 (`EXPLICIT_PRODUCT_DEFAULT`). Fitness hierarchy=unchanged, shared. Goal-time separation=unchanged, shared. Effort fallback=unchanged, shared. No strict 80/20 ratio (§21).

## 48. Runtime-schema feasibility

| Contract element | Feasibility |
|---|---|
| CoreCycle triple | New typed configuration needed (new FiveK constants in `RaceHorizonPolicy`, same pattern as existing per-distance constants); no new policy class |
| Phase-duration table | New typed data needed; existing phase-duration table pattern/schema already supports it |
| Workout-progression stage table | New `workout-progressions/`-shaped artifact (new content); existing JSON schema shape already supports it |
| VolumeSafetyPolicy instance (volume/LR/taper fields) | New typed configuration (a new named `FiveKIntermediate4D`/`FiveKIntermediate5D` record instance); existing `VolumeSafetyPolicy` record type fully supports every field this contract needs (confirmed by direct read) — no new policy class |
| `V1CatalogPilotIdentityPolicy` | New FiveK arm/enumerated identity list + new dispatch branch; existing enumerated-identity-list pattern (already used for TenK/HalfMarathon) structurally supports it as-is |
| `CatalogVolumeAndLongRunPlanner.Build` | New FiveK dispatch branch; confirmed `CATALOG_BEHAVIOR_ONLY_NOT_AUTHORITY`, architecturally ready (unchanged finding from FIVE-K.0/0A) |
| Existing generic validators (calendar/adjacency, growth-cap enforcement) | Already support FIVE_K with zero change (distance-agnostic) |

No new policy *class* is required anywhere; every gap is new typed configuration data or a new dispatch branch within existing, already-proven architecture.

## 49. Catalog feasibility

| Stage | Workout | Result |
|---|---|---|
| Aerobic base | `EASY`/`easy-standard` | `FULLY_SUPPORTED_BY_EXISTING_CATALOG` |
| Threshold development | `QUALITY`/`threshold-tempo` | `FULLY_SUPPORTED_BY_EXISTING_CATALOG` |
| VO2/interval development | New structured interval concept, distinct from unstructured `fartlek` | `SMALL_CATALOG_ADDITION_REQUIRED` — concept named only (`vo2-interval-five-k`), not authored |
| Race-specific rehearsal / taper activation | New 5K race-pace concept, structural analog of `goal-pace-ten-k` | `SMALL_CATALOG_ADDITION_REQUIRED` — concept named only (`goal-pace-five-k`), not authored |
| Long run | `LONG_RUN`/`long-run-standard` | `FULLY_SUPPORTED_BY_EXISTING_CATALOG` |
| Faster-than-5K/repetition | N/A — excluded from V1 | Not applicable |

Overall: **`FIVE_K_V1_REQUIRES_SMALL_CATALOG_ADDITION`** — exactly two new workout concepts, both named conceptually only, neither authored in this phase.

## 50. Matrix result

**`FIVE_K_V1_MATRIX_INTERMEDIATE_4D_AND_5D`**

## 51. Product-default result

**`PD_FIVEK_01_THROUGH_08_ALL_RESOLVED`** (PD-FIVEK-08 resolved via its disclosed 08a/08b decomposition, §19, not via a ninth decision)

## 52. Authority result

**`FIVE_K_CANONICAL_AUTHORITY_FULLY_CLOSED_FOR_SELECTED_V1_MATRIX`**

## 53. Runtime-readiness result

**`FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_READY_FOR_RESTRICTED_V1_MATRIX`**

## 54. Catalog result

**`FIVE_K_V1_REQUIRES_SMALL_CATALOG_ADDITION`**

## 55. Continuous-range consequence

Unchanged by this phase: `5K < target < HM` remains public and continuous; exact 5K remains not production-ready. This phase's product decisions are a necessary precondition for FIVE-K.1, not an implementation — if FIVE-K.1 succeeds for the Intermediate×{4D,5D} cells this phase scopes, the only remaining gap in the 5K-to-HM V1 range would close for those cells, with Beginner/Advanced following in later phases per FIVEK-LIT-06. No runtime change was made in this phase; no closure is claimed here.

## 56. Sub-5K deferred

**`SUB_5K_DERIVED_PRODUCT_WORK_REMAINS_DEFERRED`.** Nothing in this phase advances 1K/2K/3K/4K. `0 < target < 5K` remains explicitly out of scope.

## 57. FIVE-K.1 implementation contract

Recommended next phase: **FIVE-K.1 — Canonical FIVE_K Catalog/Runtime Implementation, Controlled Public Activation, Real PostgreSQL Lifecycle Proof, and 5K→Half-Marathon Continuous-Range Closure**, scoped to Intermediate × {4D, 5D}. FIVE-K.1 receives this phase's §41-47 contracts as **frozen authority**: implementation must not re-decide horizon, phase allocation, exposure counts, peak volume, long-run share/ceiling, taper, pace roles, or the public matrix. Any contradiction FIVE-K.1 discovers while implementing (e.g., a feasibility check this phase could not run, since no code was touched) must return to governance rather than being silently re-decided in an implementation phase. FIVE-K.1 must author the two named catalog concepts (`vo2-interval-five-k`, `goal-pace-five-k`) and the two new typed-configuration instances (`FiveKIntermediate4D`, `FiveKIntermediate5D`) plus the FiveK dispatch branches named in §48 — nothing else is newly authorized.

## 58. Remaining blockers

None block FIVE-K.1 from beginning for the Intermediate × {4D, 5D} V1 cells. All eight PD-FIVEK decisions (with PD-FIVEK-08's disclosed internal decomposition) are resolved; the two previously-residual source-silence items (exposure counts, independent RunsPerWeek authority) have explicit, non-invented runtime consequences; the one conflicting-evidence item (exact intensity-distribution ratio) correctly remains unresolved and unneeded. Beginner/Advanced and all non-4D/5D frequencies remain blocked pending FIVEK-LIT-06 or an equivalent future evidence phase — this does not block the scoped V1.

---

## Required overall result enums

- Matrix: **`FIVE_K_V1_MATRIX_INTERMEDIATE_4D_AND_5D`**
- Product defaults: **`PD_FIVEK_01_THROUGH_08_ALL_RESOLVED`**
- Authority: **`FIVE_K_CANONICAL_AUTHORITY_FULLY_CLOSED_FOR_SELECTED_V1_MATRIX`**
- Runtime readiness: **`FIVE_K_CANONICAL_RUNTIME_IMPLEMENTATION_READY_FOR_RESTRICTED_V1_MATRIX`**
- Catalog: **`FIVE_K_V1_REQUIRES_SMALL_CATALOG_ADDITION`**

## Final phase classification

**`FIVE_K_CANONICAL_PRODUCT_DEFAULT_RESOLUTION_COMPLETE`**

Checklist self-audit: all eight PD-FIVEK decisions explicitly evaluated, including a disclosed internal decomposition of PD-FIVEK-08 into 08a/08b rather than a silently-invented ninth decision (§2, §19); every selected default stays inside FIVE-K.0A's own evidence-supported options/ranges, including the explicit rejection of the internally-inconsistent {9,12,14} CoreCycle option (§12); no new research performed (five LIT documents and synthesis doc only, plus a read-only schema-shape check of `VolumeSafetyPolicy.cs`/`RaceHorizonPolicy.cs`/`V1CatalogPilotIdentityPolicy.cs` for field shape, never values); no TEN_K/HM value copied without authority (two numeric coincidences disclosed and explicitly flagged as coincidence, not justification — CalibrationStartingVolumeKm 4D=25.0 and the general reuse of already-shared generic mechanism values); 15-row matrix normalized (§3) and mixed evidence/product-default fields separated (§4); the two residual source-silence items (exposure counts, RunsPerWeek) given explicit, non-invented runtime consequences (§9-11); exact exposure behavior deterministic for selected V1 (§17); selected RunsPerWeek deterministic ({4,5}, Intermediate only, §11); final V1 matrix explicit with no "loosely 5D" ambiguity (§8); horizon/phase-allocation/compression/extension/workout-stages/exposures/pace-intensity all deterministic where required; no strict 80/20 rule introduced (§21); peak-volume authority deterministic with band and selected-reference kept distinct (§23-24); long-run authority deterministic with no unsupported LR formula (share-only, no invented ceiling, §29); taper authority deterministic (§33-34); final master contract has no required TBD (§42); final selected-V1 authority matrix has no required `SOURCE_SILENCE` (§41); catalog feasibility explicit (§49); runtime implementation readiness explicit (§53); no production behavior changed (governance-only diff, confirmed in final commit); sub-5K remains deferred (§56). FIVE_K_0B_UNEXPECTED_EVIDENCE_GAP was **not** triggered — no genuine evidence gap reappeared that FIVE-K.0A should have closed; the one irregularity found (the CoreCycle {9,12,14} inconsistency) is a drafting slip in FIVE-K.0A's own §43, not an evidence gap, and was resolved conservatively within the stated range rather than treated as blocking.
