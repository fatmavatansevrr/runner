# Phase 10K-FREQ.6D.24 — Intermediate×6D Peak Volume Band Final Authority Closure

**Evidence + product/catalog numeric decision. No production code, no catalog edit, no migration, no 7D work, no reopening of any other FREQ.6D.23 decision.**

## 0. Preflight

`PHASE_LEDGER.md` row 103 and `MASTER_ROADMAP.md` confirmed FREQ.6D.23 `DONE`, `INTERMEDIATE_6D_AUTHORITY_APPROVED_7D_PRODUCT_NON_SUPPORT_APPROVED`, with exactly one open item: Intermediate×6D's exact `PeakVolumeBand`, explicitly left `DECISION_REQUIRED` (the `[40,56]` figure was a *candidate*, never approved). Confirmed 0 ahead/0 behind at start. Next free ID `FREQ.6D.24`, scheduled and committed (`7328a20`) before this evidence work began.

## 1. Three Concepts, Kept Separate

- **ResolvedPeakReference = 44.5 km** — frozen by `FREQ.6D.23`, **unchanged**, not reopened.
- **PeakVolumeBand** — the only thing this phase decides: the `[minimumKm, maximumKm]` envelope stored in `PEAK_VOLUME_BANDS_V1`.
- **ActualAchievedPeak** — a per-plan runtime outcome (the real reachable/clamped volume a specific generated plan hits), not authority at all — not touched here.

## 2. Existing 10K Peak-Band Authority — Full Inventory

`TEN_K_EXISTING_PEAK_BAND_AUTHORITY_TABLE`, reconstructed by reading `PEAK_VOLUME_BANDS_V1.v1.json` through `.v4.json` directly (current/live artifact is v4):

| Frequency | Level (catalog `experience`) | Band (v4, live) | ResolvedPeakReference (code) | Catalog version history |
|---|---|---|---|---|
| 3D | INTERMEDIATE | [22, 32] | 22.5 (`ThreeDayIntermediate`) | v1: [26,40] → v2/v3/v4: [22,32] (revised down) |
| 4D | INTERMEDIATE | [30, 42] | 38.0 (`Default`) | v1–v4: unchanged [30,42] |
| 5D | INTERMEDIATE | [36, 50] | 44.5 (`FiveDayIntermediate`, `FREQ.6C`) | v1: [34,50] → v2–v4: [36,50] |
| 4D | NEW (Beginner) | [18, 24] | 21.0 (`BeginnerFourDay`) | v1/v2: [24,34] → v3: *removed* → v4: [18,24] (re-added, revised) |
| 3D/5D | NEW (Beginner) | *(removed)* | — | v1/v2 had [20,30]/[28,38]; **removed in v3, never restored** — Beginner only ships 4D today |
| 3D/4D/5D | ADVANCED | *(removed)* | — | v1/v2 had a full 3-cell matrix ([34,46]/[38,52]/[42,58]); **removed in v3** — Advanced is not an implemented level today |
| 3D/4D/5D | EXPERIENCED | *(removed)* | — | v1/v2 had [40,55]/[46,62]/[50,68]; **removed in v3** — same reason |

**6D**: no entry exists in any version.

## 3. `PEAK_VOLUME_BANDS_V1` Artifact Authority (§4)

Read `CatalogPeakVolumeBandLoader.LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct)` directly:

- The axis owning the band is the **triple** `(distanceFamily, experience, runsPerWeek)` — it is genuinely cross-axis, neither purely Level-owned nor purely Frequency-owned. Both level (`experience`) and frequency (`runsPerWeek`) are explicit lookup keys.
- **No interpolation/generalization rule exists.** The loader does a linear scan for an exact `(distanceFamily, experience, runsPerWeek)` match and throws a typed `PlanCatalogLoadException` — fail-closed, no nearest-neighbor, no formula fallback — if no exact row exists.
- **6D has no entry, activated or otherwise.** Adding it requires a genuine new row (new numeric authority), not flipping a flag.
- **Version-history governance pattern (real, observed, not asserted)**: comparing v1→v2→v3→v4 shows this artifact's own convention is to add or remove a `(experience, runsPerWeek)` row **only when that exact cell becomes (or stops being) a genuinely implemented, supported candidate** — v3 stripped every Advanced/Experienced/unused-Beginner row entirely (those levels/frequencies are not implemented), and v4 re-added only the one Beginner×4D row once that specific cell shipped. This is the exact rule that correctly caused `FREQ.6D.23` to leave 6D `DECISION_REQUIRED` rather than silently assume a value: **a canonical row must be deliberately authored and versioned in; it is never inherited or optional.**

## 4. Is `PeakVolumeBand` Load-Bearing at Runtime? (Candidate D)

Traced every real call site of `ICatalogPeakVolumeBandLoader`:

- `CatalogVolumeAndLongRunPlanner.ResolvePeak` (the ordinary Core-only planner) — **uses it as a hard clamp**: `Round(Clamp(reachable, bounds.MinimumKm, bounds.MaximumKm))`. This is exactly the mechanism `FREQ.6C` cross-checked 5D's real reachable-volume matrix against ("band's real 50km hard clamp").
- `TenKPreparationRunwayComponentAdapters.cs` (Preparation Runway) — injects and uses `ICatalogPeakVolumeBandLoader` directly.
- `LongHorizonFullNumericOrchestrator.cs` — constructs its own `CatalogPeakVolumeBandLoader` and uses it too.

**Conclusion: the band is genuinely load-bearing for both the ordinary Core-only path and the LongHorizon/Runway path** — not descriptive catalog metadata. A 6D request reaching any of these three real code paths will hit `LoadAsync(..., runsPerWeek: 6, ...)`, find no matching row, and throw a typed `PlanCatalogLoadException` today. **Candidate D (band not required for 6D runtime) is REFUTED by direct code inspection**, not assumed.

## 5. Tracing the `[40,56]` Candidate (§5, §16)

`FREQ.6D.23`'s own report derived `[40,56]` by observing the real v2–v4 progression (3D→4D→5D: `[22,32]→[30,42]→[36,50]`, band-width `10→12→14`, a **+2km-width-per-day** pattern) and projecting one more step forward. Reconstructing this exactly:

- **Lower bound (40)**: `36 + 4`, matching the *decreasing* min-delta trend observed between 4D→5D (`+6`) if further decreased by 2 — this is **interpolation of a 2-point trend**, not a stated rule.
- **Upper bound (56)**: `50 + 6`, matching the observed max-delta trend (`+8`) decreased by 2 — same interpolation.

**New finding this phase makes that `FREQ.6D.23` did not check**: `PEAK_VOLUME_BANDS_V1.v1.json` (superseded, but real historical data) shows Advanced and Experienced bands that were **never revised** away from a **constant** per-day delta (Advanced: `+4km`min/`+6km`max per day, exactly linear across 3D→4D→5D; Experienced: similar). Intermediate's own band, by contrast, was **deliberately revised** (v1→v2) *away* from an original constant-delta shape (`[26,40]→[30,45]→[34,50]`, `+4/+5` constant) to the current decreasing-increment shape (`[22,32]→[30,42]→[36,50]`, `+8,+6` then `+10,+8`). This is direct, repository-internal proof that **Intermediate's band was hand-recalibrated per cell, not generated by any formula** — and that a formula-based approach was tried (for Advanced/Experienced) and never carried forward for Intermediate. Extrapolating the *current* decreasing-increment shape one more step is therefore exactly the same category of error the original constant-delta formula turned out to be: **fitting a curve through calibrated points and treating the curve as authority.**

**Both endpoints of `[40,56]` classify as `DERIVED`** (interpolation from adjacent approved cells), not `DIRECT_CANONICAL`, `EVIDENCE_ENVELOPE`, or `PRODUCT_DEFAULT`. Per §21's decision standard, `DERIVED`-by-arithmetic-pattern alone does not meet the bar. **Candidate A is not approved.**

## 6. Candidate B — Frequency-Neutral Reuse of an Existing Intermediate Band

The current, live artifact already differentiates Intermediate's band by frequency (`[22,32]` / `[30,42]` / `[36,50]` — three different bands, not one shared value). Treating the band as frequency-neutral for 6D (e.g., "just reuse 5D's `[36,50]`") would contradict the artifact's own already-observed behavior — every existing Intermediate frequency has its own distinct band. **No authority supports collapsing 6D into 5D's band.** §9's Level-effect question is answered directly here: the band is demonstrably **cross-axis** (§3), not purely Level-owned, so "Level is unchanged, therefore the band shouldn't change" does not hold — frequency really does move this specific authority today. **Candidate B is not approved.**

## 7. Candidate C — Frequency-Adjusted Formula Band

Per §5's finding, no `+X km per extra day` formula is canonical for Intermediate — the one historical attempt at a constant-delta formula (Advanced/Experienced, v1) was never carried into Intermediate's own revision and is not itself still-authoritative (those levels are unimplemented and their rows are removed from the live artifact). Inventing a new formula now — even a "decreasing increment" one — is exactly what §18 forbids. **Candidate C is not approved.**

## 8. Candidate D — Band Not Required for 6D Runtime

Refuted directly by code inspection (§4). **Not approved** as stated; correctly reclassified below.

## 9. External Evidence (§15) — Used Only Because Repository Authority Cannot Determine the Band

Repository authority alone cannot produce an approved exact band (§5–8), so external evidence was consulted, same discipline as `FREQ.6D.23`/`FREQ.6C`:

- **Scientific evidence**: none found that states a precise weekly-km peak-volume envelope specific to 6-day-per-week intermediate 10K training — general sports-science literature addresses injury/frequency association (already used in `FREQ.6D.23` for the 7D decision), not exact volume bands.
- **Coaching/program examples**: a real "Women's Running" 6-day 10K program (2 speed sessions Tue/Thu, 1 long run Sun, 3 easy runs, rest Monday — **structurally identical to Appsel's own frozen 6D shape, 2 KEY + 3 EASY + 1 LONG**) was found describing peak-week long runs of roughly 10–12 miles (~16–19km) and peak Saturday/key-session distances of ~9–10 miles — but this program is explicitly framed as an **"advanced"** 10K plan in its own description, not "intermediate," and the figures are second-hand-summarized rather than a single clean, directly-quotable weekly-total peak figure. General intermediate 10K guidance repeatedly states "15–25 miles/week" (the same undifferentiated range `FREQ.6C`/`FREQ.6D.23` already anchored to) without a distinct 6-day breakout.
- **Appsel product decision**: none made yet for this specific axis.

**Conclusion**: real external evidence exists and is directionally informative (a genuine 6-day 2K+3E+1L intermediate-adjacent program does run somewhat higher peak long runs than the blended 5–6-day Higdon figure implies), but it does not reach the quality bar this phase requires (§21: "do not claim a precise 40 or 56 km scientific threshold unless directly supported" — the same standard applies to any other precise pair, including one grounded in this new source, since the source's own tier (advanced vs. intermediate) and its summarized, non-exact figures do not constitute a clean, directly-quotable single-tier evidence envelope of the kind `FREQ.6C` had for Higdon's Intermediate plan).

## 10. Readiness Interaction (§11)

Read `CatalogVolumeAndLongRunPlanner.ResolvePeak`: `reachable < bounds.MinimumKm` is classified `BelowTypicalPeakButValid` — **not a failure, not blocked** — the band's lower bound is descriptive/classificatory at that point, never an eligibility gate. `reachable` above `bounds.MaximumKm` is silently clamped down to `MaximumKm` (upper bound acts as a real numeric ceiling). This confirms:

- **Lower-bound semantics**: a classification/calibration boundary ("typical" vs "below typical but still valid"), not an eligibility floor and not the minimum representable weekly volume (which is a distinct, catalog-session-minimum concept `FREQ.6D.23` §8e already correctly separated).
- **Upper-bound semantics**: a real numeric ceiling applied to the *reachable* (projected) peak — not a "hard safety cap" in the injury sense, but it does mechanically constrain what a plan's Core weeks can grow to.
- This behavior is **not mutated** by this phase — reported as observed, unchanged.

## 11. Representability Validation Using Each Serious Candidate (§12–14)

`FREQ.6D.23`'s starting-volume (26.0/19.5) and `ResolvedPeakReference` (44.5) are unchanged. The real reachable-volume matrix computed by `FREQ.6C`'s own mechanism (`canonicalDefaultMultiplier = 44.5/26.0 = 1.71154`) is therefore **byte-identical for 6D to the already-proven-safe 5D matrix** (peaks at 48.0km at week 14, missing-readiness; 36.0km at week 14, explicit-zero) — nothing about the band changes this, since the band only clamps `reachable` **if it exceeds `MaximumKm`**.

| Candidate band | Max ≥ 48.0 (missing-readiness week-14 reachable)? | Core 8–14 | Runway 15–20 | LongHorizon 21–52 |
|---|---|---|---|---|
| `[40,56]` (rejected, §5) | Yes (56 ≥ 48) | No clamp triggered — valid | Valid (GE→Runway clamp uses the Core-Week-1 *target*, not the band, §26 `FREQ.6D.23` — untouched) | Valid (GE cap uses `ResolvedPeakReference`, §14 below) |
| `[36,50]` (5D reuse, rejected §6) | Yes (50 ≥ 48) | No clamp triggered — valid | Valid | Valid |
| Any evidence-informed candidate with max ≥ 48 | Yes | Valid | Valid | Valid |

**No candidate seriously considered introduces a representability conflict** — this is genuine, useful validation even though no exact figure is approved: whichever real number is eventually approved (as long as its `maximumKm` is not set irrationally below the already-proven-safe 48.0km ceiling), Core/Runway/LongHorizon representability for 6D remains exactly as sound as 5D's already-public proof. This validation happened **after** evaluating candidates, per §12's own instruction — it was not used to select or invent a number.

**GE cap relationship (§14, §24)**: confirmed the LongHorizon GE target-cap semantics read `VolumeSafetyPolicy.ResolvedPeakReference` (44.5, unchanged), never `PeakVolumeBand.MaximumKm`. These remain two separate concepts; this phase does not conflate them, and no code exists that would need changing regardless of how the band question resolves.

## 12. Required Decision Matrix

`INTERMEDIATE_6D_PEAK_VOLUME_BAND_DECISION_MATRIX`

| Candidate | Lower-bound provenance | Upper-bound provenance | Contains 44.5? | Catalog governance valid? | Core valid? | Runway valid? | LongHorizon valid? | New numeric assumption? | Selected? |
|---|---|---|---|---|---|---|---|---|---|
| A. `[40,56]` | DERIVED (2-point trend interpolation) | DERIVED (2-point trend interpolation) | Yes | No — no row exists, none authored here | Yes | Yes | Yes | Yes (the interpolation itself) | **No** |
| B. Reuse 5D `[36,50]` | UNSUPPORTED (contradicts existing per-frequency differentiation) | UNSUPPORTED | Yes | No | Yes | Yes | Yes | No formula, but contradicts observed behavior | **No** |
| C. Frequency-adjusted formula (`+X/day`) | UNSUPPORTED (no canonical formula; historical formula abandoned for Intermediate) | UNSUPPORTED | Depends on X | No | — | — | — | Yes (a new formula) | **No** |
| D. Band not required for 6D runtime | N/A | N/A | N/A | **No — refuted, band is load-bearing (§4)** | — | — | — | No | **No** |
| E. External-evidence-grounded new figure (from the real 6-day 2K+3E+1L program found, §9) | EVIDENCE_ENVELOPE-adjacent but tier-ambiguous (advanced, not intermediate) and non-exact | Same | Would need verification | Not authored here | — | — | — | Would require a genuine new number this phase is not confident enough to assert | **No** |

**No candidate selected.**

## 13. Required Final Authority

```
PeakVolumeBand: DECISION_REQUIRED
```

(No exact `[min,max]` approved; not inherited from any existing canonical authority; confirmed load-bearing at runtime, so this is not merely a publication-metadata gap either.)

```
ResolvedPeakReference: 44.5 km — UNCHANGED
GE target cap: existing FREQ.6D.23 authority (uses ResolvedPeakReference) — UNCHANGED
Starting volume: 26.0 / 19.5 km — UNCHANGED
Long-run: 28% / 36% — UNCHANGED
Progression: 0.07 / 0.08 / existing absolute cap (2.5km) / 0.5km rounding / 0.53 taper — UNCHANGED
```

## 14. 7D Status (§24)

Untouched. `PRODUCT_NON_SUPPORT` across all horizons, exactly as `FREQ.6D.23` closed it. No 7D PeakVolumeBand was researched or considered.

## 15. Implementation Readiness (§25)

Intermediate×6D's implementation is **blocked on exactly one authority item**: the `PeakVolumeBand` exact figure, because that value is genuinely load-bearing at runtime (§4) for both the ordinary Core-only and LongHorizon/Runway paths — a real `PlanCatalogLoadException` would fire the first time any 6D dark test reaches `CatalogVolumeAndLongRunPlanner.ResolvePeak`, `TenKPreparationRunwayComponentAdapters`, or `LongHorizonFullNumericOrchestrator`. Every other authority `FREQ.6D.23` froze remains genuinely ready (structure, starting-volume, progression, long-run share, Adaptation state tables, catalog capacity for workouts). This is not a broad blocker — it is exactly one number, in exactly one catalog artifact, gating exactly the paths already identified.

## 16. Final Classification

```
INTERMEDIATE_6D_IMPLEMENTATION_BLOCKED_ON_PEAK_VOLUME_BAND_AUTHORITY
```

Not forced into an approved band per §21's explicit prohibition on weak justifications ("makes 44.5 look centered," "lets implementation proceed," "one extra run probably adds X km" — all of which describe exactly the reasoning this phase declined to use for Candidates A/B/C). This is an honest, evidence-traced non-closure, not an oversight: real repository history (§5) and real code tracing (§4) were both required to reach it, and both are now on record for whoever makes the actual product/catalog-content decision next (most likely a short, narrowly-scoped follow-on decision phase, or a direct catalog-governance call by a human stakeholder, selecting one exact figure with real product authority — e.g., commissioning a proper evidence envelope the way `FREQ.6C` did for 5D, using a genuinely tier-matched external source rather than the ambiguous "advanced" program found here).

## 17. Governance

- `PHASE_LEDGER.md`: row appended.
- `MASTER_ROADMAP.md`: updated; Intermediate×5D COMPLETE/PUBLIC and Intermediate×7D PRODUCT_NON_SUPPORT both preserved untouched.
- Next capability: **not** `INTERMEDIATE×6D COMBINED IMPLEMENTATION + DARK VERIFICATION` (that requires the band to close first, per §29's own conditional). `NEXT_PHASE_NOT_YET_SCHEDULED`.
- Push gate recalculated below; normal push only, no force.
