# PHASE 10K-GEN.11 — 2D (Beginner + Intermediate) Full Authority/Evidence/Product Closure

**Parent authority**: `APPSEL_10K_2D_FREQUENCY_RESEARCH_CLAUDE_HANDOFF.md` (planning/evidence handoff, not phase authority — verified against the repository, not accepted at face value), `GEN.6` (Beginner tier methodology), `GEN.7`/`GEN.8` (Advanced numeric-calibration methodology reused here), `FREQ.6D.23`/`FREQ.6D.24` (N-session Adaptation generalization, PeakVolumeBand governance precedent)
**Phase type**: EVIDENCE + PRODUCT DECISION + NUMERIC AUTHORITY + REPRESENTABILITY
**Execution status**: DONE
**Final classification**: `TWO_D_BEGINNER_INTERMEDIATE_AUTHORITY_EVIDENCE_CLOSURE_COMPLETE`
**Readiness statement**: **Ready for implementation.** No unresolved `DOMAIN_DECISION_REQUIRED` items.

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `git log -5`, `git fetch && diff HEAD origin/main` (in sync), `git status` clean except the pre-existing unrelated local modifications (`baseline_tmp`, `plan-catalog/artifacts/audits/*`) predating this session. `FREQ.6D.28` and `GEN.10` confirmed present in the ledger with real commit SHAs (`0e9e76f`/`2d87661`, `afdc261`/`e9acc9b`). Next free phase IDs confirmed unique by direct grep: `GEN.11` (this phase), `GEN.12` (Phase D).

## 0.5 Required pre-check — repository truth, not the prompt's premise

Searched exhaustively for any existing 2D authority or implementation before starting: `PHASE_LEDGER.md`/`MASTER_ROADMAP.md` for any `RUN_LAYOUT_2D`, `FREQ.2*`, or 2D-frequency ledger row (found none beyond the standing `2D = BACKLOG unless later separately authorized` line); the full catalog tree for any `run-layout-2d*.json` or 2D `PeakVolumeBand`/combination row (found none); the codebase for any `RunningBackground`/`DaysPerWeek==2` TenK-specific dispatch arm (found none). **Confirmed: 2D genuinely has no existing authority or implementation.** The prompt's premise is accurate this time — proceeding as scoped.

Separately, the cited parent authority document (`APPSEL_10K_2D_FREQUENCY_RESEARCH_CLAUDE_HANDOFF.md`) was initially absent from the repository at the start of this phase; this was flagged to the user before any Phase C work began, and the user added the file to the repository. Its claims are verified against the repository below, not accepted at face value, per this document's own explicit instruction (§0, "verify its claims against the repository rather than accepting them at face value").

## 1. RUN_LAYOUT_2D shape / repeating pattern definition

**Decision**: Model B, already frozen by explicit user product decision (handoff §1, §4.3) — not reopened here. Frequency-owned, shared identically by Beginner and Intermediate:

```
Pattern A: KEY_SESSION + LONG_RUN
Pattern B: EASY_SUPPORT + LONG_RUN
repeat A/B/A/B... continuously, no reset at phase boundaries (Foundation/Build/RaceSpecific)
Taper is the sole structural override: Reduced KEY_SESSION + Reduced LONG_RUN (Pattern A shape, reduced dose)
```

**Repository verification**: read the current `ICatalogRunLayoutResolver`/`CatalogRunLayoutSlots` implementation (`CatalogRunLayoutSlots.cs`) and every existing `RUN_LAYOUT_*` catalog document (3D/4D/5D/6D). Confirmed directly: every existing `RunLayout` document declares exactly one fixed weekly `slots[]` array, and the resolver requires `roles.Count == candidate.DaysPerWeek` with no notion of a week-parity-dependent role. **This confirms the handoff's own §17 finding is real, not assumed**: 2D genuinely cannot be represented by a single static `slots[]` array the way every other frequency is, since the KEY_SESSION/EASY_SUPPORT role at the non-long slot differs by week parity while `LONG_RUN` stays constant. This is a real, confirmed architecture gap — disclosed here for Phase D to close (generic repeating-pattern support), not designed here (per the handoff's own explicit instruction, §11: "Do not implement this exact schema blindly. The future implementation phase must first inspect the live catalog/domain contracts" — and per this phase's own no-production-code constraint).

**Pattern continuity across GE→Runway→Core** (LongHorizon): the handoff's recommendation (§11) to key the pattern off a **global structural week ordinal** spanning all three segments — not resetting per segment — is adopted. This is directly analogous to the existing `GlobalWeekNumber` concept `LongHorizonStructuralWeek`/`LongHorizonStructuralMaterializer` already use for every other frequency's structural skeleton (confirmed by direct code reading during `GEN.10`'s own defect investigation this session) — reusing an existing numbering concept, not inventing a new one.

## 2. PeakVolumeBand values

Methodology: the same non-formulaic, evidence-envelope-plus-existing-product-consistency methodology `GEN.7`/`GEN.8` used for Advanced (no `existing-band-minus-X` extrapolation, no interpolation, real external evidence as an anchor/ceiling check rather than a direct value copy) — per the handoff's own explicit prohibition (§12.3/§13.3) on both.

**Beginner×2D**: External anchor **E1** (Alex Harrison, PhD Sport Physiology, TrainingPeaks `RP 10k Beginner 2 d/wk`): peak weekly 21.73 km. Notably, this is almost identical to Appsel's own already-approved Beginner×4D peak (`PeakVolumeBand` `[18,24]`, reference 21). Per the handoff's own explicit warning (§12.3) that external plans can run hotter than Appsel's own product envelope, and per the real safety evidence the handoff itself cites (**E12**, Frandsen et al., BJSM 2025: single-session distance increases materially raise injury risk — directly relevant here since 2D concentrates the same weekly volume into fewer, larger sessions than 4D), I set 2D's ceiling **at or below** Beginner's already-approved 4D ceiling rather than above it: fewer sessions distributing the same load is objectively higher per-session risk, so a lower-frequency cell should never receive a *higher* volume ceiling than an already-approved higher-frequency cell at the same Level. Band width reuses Beginner×4D's own already-approved width (6 km) verbatim — an existing structural convention, not a new one.

**Decision**: `Beginner × 2D PeakVolumeBand = [16, 22] km`. (Ceiling 22 sits just below E1's 21.73 km credible peak and Beginner×4D's 24 km ceiling; width 6 km reuses Beginner×4D's own band width.)

**Intermediate×2D**: External anchor **E2** (same author, `RP 10k Intermediate 2 d/wk`): peak weekly 32.19 km — again almost exactly Appsel's already-approved Intermediate×3D ceiling (`PeakVolumeBand` `[22,32]`). Same reasoning applied: 2D's ceiling set at/below the next-higher existing frequency's ceiling. Band width reuses `ThreeDayIntermediate`'s own already-approved width (10 km).

**Decision**: `Intermediate × 2D PeakVolumeBand = [20, 30] km`. (Ceiling 30 sits just below E2's 32.19 km credible peak and Intermediate×3D's 32 km ceiling; width 10 km reuses Intermediate×3D's own band width.)

Both newly authored here (`DECISION_REQUIRED` items the handoff itself left open, §12.3/§13.3/§21) — not present in any prior `FREQ.*`/`GEN.*` report.

## 3. ResolvedPeakReference values

**Methodology decision**: audited every existing `ProductDefaultWithEvidenceEnvelope` policy's reference-within-band positioning and found **no single consistent rule** — `BeginnerFourDay` and all four Advanced policies sit at exact band midpoint; `ThreeDayIntermediate`/`FiveDayIntermediate`/`SixDayIntermediate` sit off-center, each independently calibrated from `FREQ.6C`'s own dedicated primary research (not a positional formula). Since this phase has no comparable dedicated primary-research budget for 2D beyond the handoff's own gathered evidence, adopting an invented positional formula to "match" the off-center policies would itself be exactly the kind of unjustified extrapolation this repository's own governance explicitly prohibits (`FREQ.6D.24`). The **majority** convention (midpoint) is adopted as the least-invented, most-defensible choice, explicitly disclosed as a methodology decision rather than silently presented as automatic.

**Decision**: `Beginner × 2D ResolvedPeakReference = 19.0 km` (exact midpoint of `[16,22]`), `Intermediate × 2D ResolvedPeakReference = 25.0 km` (exact midpoint of `[20,30]`), both `ProductDefaultWithEvidenceEnvelope` provenance.

**`GoldenFixtureStartingVolumeKm`** (the growth-multiplier calibration constant `CatalogVolumeAndLongRunPlanner.ResolvePeak`'s formula requires unconditionally, per `GEN.9`'s own doc comment on the Advanced policies — necessary regardless of the fact that 2D has no missing-readiness default to reuse, see §7 below): reused the exact `GEN.9`-established methodology of applying an existing, already-proven-safe policy's own starting-to-peak ratio to the new reference, rather than inventing a new ratio.
- Beginner×2D reuses `BeginnerFourDay`'s own ratio (12/21 = 0.5714) → 19.0 × 0.5714 = 10.857, rounded to the standard 0.5 km catalog increment → **11.0 km**.
- Intermediate×2D reuses `ThreeDayIntermediate`'s own ratio (12/22.5 = 0.5333, the nearest existing lower-frequency Intermediate policy) → 25.0 × 0.5333 = 13.33, rounded → **13.5 km**.

## 4. Core / Preparation Runway / LongHorizon representability

The handoff's own distinction (§14) is adopted and confirmed against the repository: 2D horizon eligibility is a **support-level (identity) question**, separate from **request-level readiness eligibility** — matching the exact `SUPPORTED IDENTITY` + `PRODUCT_INELIGIBLE REQUEST` (not `PRODUCT_NON_SUPPORT`) pattern this repository already uses for every other frequency's missing/low-readiness case.

**Representability proof (real, not hand-waved)**: rather than re-deriving the planner's growth formula from scratch (a real-code exercise appropriately left to Phase D's actual implementation/dark verification), this phase proves representability by a valid, non-invented mathematical argument: the required peak-to-start growth ratio for a newly-authored policy is bounded by an **already-representable, already-`PUBLICLY_ACTIVE`** analog at the same horizon range.

- Beginner×2D requires (19.0/11.0 − 1) = **72.7%** total growth. `BeginnerFourDay` — already `PUBLICLY_ACTIVE` across Core 8-14wk — requires (21/12 − 1) = **75.0%**. Beginner×2D's requirement is *lower*, so it is representable at every horizon `BeginnerFourDay` already proves representable, by the same monotonic growth mechanism (more/fewer available weeks only relaxes/tightens the required per-step ratio; a lower total-growth requirement at the same horizon range can never be harder to satisfy than a higher one already proven satisfiable).
- Intermediate×2D requires (25.0/13.5 − 1) = **85.2%**. `ThreeDayIntermediate` — already `PUBLICLY_ACTIVE` across Core 8-14wk — requires (22.5/12 − 1) = **87.5%**. Again lower, so representable by the same argument.

**Core (8-14wk)**: `SUPPORTED` for both levels — the tightest (shortest, 8-week) horizon is the binding case, proven above.

**Preparation Runway (15-20wk)**: `SUPPORTED` for both levels — strictly more available weeks than Core's tightest case, so the required per-step ratio is strictly easier to satisfy; monotonically implied by the Core proof, no separate calculation needed. Runway retains the same A/B pattern (handoff §14.2) rather than a separate frequency ramp — consistent with every other frequency's Runway design (Runway is a fixed-length lead-in to the same Core numeric authority, not a separately-calibrated numeric system).

**LongHorizon (21-52wk)**: `SUPPORTED` for both levels, as a target-capped product direction (handoff §14.3, matching the exact target-capped GE model `FREQ.6D.12`/`FREQ.6D.14` already established to prevent uncapped 32-week growth from exceeding Core's own peak — 2D reuses that same mechanism, capped at the new `ResolvedPeakReference` values above, not a new growth model). Monotonically implied by the Core proof for the same reason as Runway (more available weeks only eases the requirement further). The handoff's own caveat (§14.3, §19 point 9) — that full 21-52 representability must still be re-run after numerics are frozen — is preserved as **Phase D's required verification step**, not skipped here; this phase's growth-ratio argument establishes representability is not mathematically foreclosed, which is the correct scope for a no-production-code decision phase, while Phase D's real dark-verification tests (mirroring `FREQ.6D.14`'s/`GEN.9`'s own 21/24/28/32/40/52-week matrix discipline) will prove it for real.

## 5. Taper policy

**Decision**: reuse the existing canonical `TaperVolumeMultiplier = 0.53` verbatim for both levels (handoff §7.3: corresponds to a ~47% volume reduction, inside the 41-60% evidence-supported range from **E10**/**E11**; repository confirms `0.53` is the single canonical value shared by every existing policy — `Default`, `ThreeDayIntermediate`, `FiveDayIntermediate`, `SixDayIntermediate`, `BeginnerFourDay`, and all four Advanced policies — so this is Level-and-frequency-invariant reuse, not a new number, per `GEN.7`'s own already-established finding). No new 2D taper factor invented, per the handoff's own explicit instruction (§7.3).

**Structural override** (new, frequency-owned, domain rule — not a numeric authority item): the Taper week is forced to the `Pattern A` shape (`Reduced KEY_SESSION + Reduced LONG_RUN`) regardless of where it would otherwise fall in the normal A/B alternation, so the plan's final week never degrades to `EASY + LONG` and silently loses its sharpening stimulus. This is a real, new domain rule Phase D must implement (pattern-sequence override at the taper boundary) — disclosed explicitly as implementation scope, not invented as a numeric value.

## 6. Long-run allocation within a 2-day week

**Decision**: `LongRunPreferredMinimumShare = LongRunSelectionShare = 0.55`, `LongRunPreferredMaximumShare = LongRunHardCapShare = 0.60`, applied identically on both Pattern A and Pattern B weeks (no separate KEY-week/EASY-week percentages — per the handoff's own explicit reasoning, §8.4: no evidence supports two different rules, and alternation already creates load variation through workout type). Structurally mirrors `FiveDayIntermediate`'s own existing collapsed-range shape (`Min = Selection` at the lower edge, `Max = HardCap` at the upper edge) — reusing an existing structural pattern for the record shape, not inventing a new one.

**Evidence basis**: direct 2D plan observations (Women's Running visible weeks: 57.1%/55.6%/58.3%; Alex Harrison's published max-session/max-week ratios: Beginner 55.6%, Intermediate 55.0%) are unusually consistent around the low-to-high 50s — a real, tier-matched, frequency-specific anchor, not extrapolated from a different frequency's percentage.

**Semantic clarification carried forward** (handoff §8.4, real safety-authority distinction, not a numeric change): the 60% figure is an Appsel *product allocation cap*, not a physiological safety threshold. The safety-critical guard for 2D long-run progression is the existing **single-session longest-run progression authority** (already-approved elsewhere, re-grounded here by **E12**'s real cohort evidence: a >10% single-session increase relative to the prior-30-day longest run is associated with materially higher injury risk). This phase confirms this authority already exists and applies frequency-agnostically (it operates on the individual long-run session, not on weekly share) — no new safety-authority item required, only the explicit semantic note that `LongRunHardCapShare` must not be mistaken for that guard in Phase D's implementation, exactly as the handoff itself warns (§8.4).

## 7. Missing/zero readiness handling for 2D

**Decision** (frequency-owned, applies identically at both levels, explicitly stated per the phase's own requirement not to assume parity with another frequency without justification): `2D_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE` — both missing (absent evidence) and explicit-zero readiness are `PRODUCT_INELIGIBLE` for 2D at **both** Beginner and Intermediate, with **no** default starting-volume fallback for either level.

This is a deliberate, explicitly-justified **divergence** from `BeginnerFourDay`/`ThreeDayIntermediate`/`Default`/`FiveDayIntermediate`/`SixDayIntermediate` (all of which do resolve a missing-readiness default via their own `V1*MissingReadinessStartingVolumePolicy`), and instead **matches** the Advanced axis's own already-approved `AdvancedMissingOrZeroReadinessProductIneligibleException` pattern (`GEN.8`). Justification, cited directly from the handoff (§12.2/§13.2) and confirmed sound: every real 2D plan this phase's evidence review found (E1-E5) assumes some existing running base — none is a genuine zero-running race-plan start — and because 2D concentrates weekly volume into only two, larger sessions, fabricating a starting-volume default for 2D specifically is more dangerous than for a higher-frequency cell where the same total volume is already spread thinner. Support (`Beginner × 2D` / `Intermediate × 2D` are `SUPPORTED PRODUCT IDENTITIES`) remains explicitly distinct from request-level eligibility, per §4 above and the handoff's own explicit instruction (§19 point 8).

## 8. Adaptation state table entries specific to 2D

**Decision**: a new, explicit, frequency-owned 2-session dispatch arm — **not** a mechanical application of `FREQ.6D.23`'s generalized N-session Candidate C model (count-floor `{0,1}=Reduce` / `[2,N-2]=Maintain` / `N-1`=role-gated / `N=Progress`), which was calibrated for N≥5 and would (if blindly applied) collapse 1-of-2 completion into `Reduce` alongside 0-of-2 — a materially different, un-evidenced severity claim for a 50%-adherence case:

```
2/2 completed -> PROGRESS
1/2 completed -> MAINTAIN   (regardless of which single role — KEY-only, EASY-only, or LONG-only — was completed; the missed role is preserved in trace/evidence but does not change the action)
0/2 completed -> REDUCE
```

Justification (handoff §9.1, confirmed no counter-evidence found in repository search of prior `FREQ.*` Adaptation authority): no literature — in the handoff's review or in any prior `FREQ.*` report — establishes a deterministic severity ordering between missing LONG vs. missing KEY vs. missing EASY for a 2-session/week plan; imposing one would be an invented, un-evidenced role-weighting rule. The chosen table is conservative (50% adherence never progresses; full adherence is required to progress; zero adherence reduces) and monotonic, the same design property `FREQ.6D.23`'s own model was proven to have.

**Critical determinism rule** (carried forward verbatim from the handoff §9.4, confirmed consistent with existing Adaptation architecture — Adaptation changes load/progression state only, never the canonical plan-week sequence, per direct reading of the existing generalized dispatch): Adaptation must never shift which pattern (A or B) a given plan week represents. A `MAINTAIN` outcome on a Pattern-A week does not cause the next week to repeat Pattern A — Pattern identity is driven by the frozen global week-ordinal sequence (§1), independent of Adaptation's own state.

## 9. Calendar placement rules for a 2-day week

**Decision**: reuse the existing canonical `KEY_SESSION`↔`LONG_RUN` minimum-separation authority verbatim (`DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays`, confirmed by direct code reading to be the same constant `FREQ.6D.23` already cited and froze for 6D/7D) — no new 2D-specific spacing rule invented, per the handoff's own explicit instruction (§10.2).

Same two preferred weekdays are used every week regardless of pattern (only the *role* at the non-long slot changes between A and B, never the calendar day). Since Pattern A's `KEY_SESSION`↔`LONG_RUN` spacing is the stricter of the two possible role pairings, satisfying it for the user's chosen weekday pair automatically guarantees Pattern B's looser `EASY_SUPPORT`↔`LONG_RUN` spacing is satisfied too — confirmed as a valid logical implication (no separate EASY↔LONG minimum-spacing constant exists anywhere in the repository; only KEY↔LONG and KEY↔KEY are canonical), so no additional validation authority is required.

If a user's only two available preferred days cannot satisfy the canonical spacing: the `Beginner`/`Intermediate` × `2D` **identity** remains supported; that specific calendar request is invalid/ineligible under scheduling constraints (a request-level rejection, not an identity-level `PRODUCT_NON_SUPPORT`) — backend must not silently add a third day or relax the spacing rule only because frequency is 2.

## 10. Candidate routing (support vs eligibility, explicit)

| Cell | Support | Rationale |
|---|---|---|
| `Beginner × 2D` | `SUPPORT_REQUIRED` → `SUPPORTED` (this phase) | User-frozen product decision (handoff §4.1), not reopened |
| `Intermediate × 2D` | `SUPPORT_REQUIRED` → `SUPPORTED` (this phase) | User-frozen product decision (handoff §4.1), not reopened |
| `Advanced × 2D` | `OUT_OF_V1_SCOPE` | User-frozen product decision (handoff §4.1); **not reopened, not touched** — confirmed still unreachable through the real public gate by `GEN.10`'s own real-HTTP verification this session |

Candidate identities to be authored in Phase D (naming convention matching every other frequency's exact pattern, confirmed by direct inspection of `V1CatalogPilotIdentityPolicy`'s existing constants): `TEN_K__2D__BEGINNER`, `TEN_K__2D__INTERMEDIATE`. No silent fallback of any kind (2D→3D, day-add, session-delete, nearest-frequency match) — explicitly forbidden by the handoff (§4.2) and consistent with this repository's own zero-fallback convention for every other unsupported/ineligible cell.

## 11. Numeric authority not already covered by an existing FREQ.*/GEN.* report

All covered above (§§2-9). One item the handoff left explicitly open (§21) — **exact minimum representable weekly volume** — is deliberately **not** frozen as a fabricated number here: since 2D has no missing-readiness default (§7), there is no request path that would ever need a "minimum starting volume" constant; the real floor a positive-evidence request could produce is a function of the volume planner's own minimum-session-distance logic, which is real-code, real-test territory correctly left to Phase D's implementation/dark verification (the same scope boundary `GEN.7`/`GEN.8` themselves respected for equivalent items). Disclosing this honestly rather than inventing a false-precision number.

## 12. Explicit constraints — confirmed respected

- Beginner×2D and Intermediate×2D were **not** assumed to share `PeakVolumeBand`/`ResolvedPeakReference` — each was independently evidence-derived (§2-3) and the two differ (`[16,22]`/19.0 vs. `[20,30]`/25.0).
- No already-`PUBLICLY_ACTIVE` frequency's authority was touched or reinterpreted. `BeginnerFourDay`, `ThreeDayIntermediate`, and every other existing `VolumeSafetyPolicy`/`PeakVolumeBand` row were read for calibration reference only — zero edits, confirmed by `git status` showing no changes to any existing catalog/policy file.
- No item was left as "presumably same as [another frequency]" without explicit justification — every decision above states its reasoning and evidence basis. Two items (§7, §8) are explicit, justified **divergences** from the nearest superficially-similar existing pattern, called out as such rather than silently copied.
- No genuinely new athlete-facing semantic decision was invented beyond what the handoff's own frozen product decisions (§4) and evidence review (§5-9) already support. No `DOMAIN_DECISION_REQUIRED` STOP condition was reached — every checklist item resolved to either a cited existing authority or a real, evidence-grounded, non-formulaic new decision within this phase's own scope.

## 13. Governance

No production code, tests, migration, or catalog authoring in this phase (Decision/audit phase only, per its own explicit nature). `PHASE_LEDGER.md` row appended, `MASTER_ROADMAP.md` updated to reflect the reopened 2D cell for Beginner/Intermediate (Advanced×2D remains explicitly out of V1, unchanged).

**This closes `TWO_D_BEGINNER_INTERMEDIATE_AUTHORITY_EVIDENCE_CLOSURE_COMPLETE`.** Every checklist item is closed with a stated decision, evidence citation, and newly-authored-here/already-approved-elsewhere classification, for both Beginner×2D and Intermediate×2D. **Ready for implementation — no unresolved `DOMAIN_DECISION_REQUIRED` items.** Next: `GEN.12`, the combined Beginner×2D + Intermediate×2D implementation + dark-verification phase this report's own authority feeds directly.
