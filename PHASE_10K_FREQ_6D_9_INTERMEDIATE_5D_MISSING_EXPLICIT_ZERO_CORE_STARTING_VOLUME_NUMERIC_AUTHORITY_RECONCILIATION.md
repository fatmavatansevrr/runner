# Phase 10K-FREQ.6D.9 — Intermediate×5D Missing/Explicit-Zero Core Starting-Volume Numeric Authority Reconciliation

**Type:** EVIDENCE + PRODUCT/NUMERIC AUTHORITY DECISION
**Parent:** FREQ.6D.8 (`INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`)
**No production code was changed in this phase.**
**Final classification:** `INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`

## 1. Preflight

Read `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, and the complete real reports for `FREQ.6` (`PHASE_10K_FREQ_6_INTERMEDIATE_5D_PRODUCT_POLICY_DECISION_CLOSURE.md`), `FREQ.6B` (`PHASE_10K_FREQ_6B_INTERMEDIATE_5D_NUMERIC_AUTHORITY_EVIDENCE.md`), `FREQ.6C`'s checkpoint (`PHASE_10K_FREQ_6C_CHECKPOINT_5D_PRE_CATALOG_BASELINE.md`) and closure (`PHASE_10K_FREQ_6C_INTERMEDIATE_5D_NUMERIC_DECISION_CLOSURE.md`), `FREQ.6D.6`, and `FREQ.6D.8`.

Confirmed `FREQ.6D.8`'s final classification is exactly `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`, and confirmed its own §16-17 finding by both evidence paths it reported:

- **A (Core-only reproduction)**: independently re-reproduced this phase — a real HTTP `POST /api/v1/plans/generate-preview/race` for Intermediate×5D Core-only at every one of 8, 10, 12, and 14 weeks, for both missing (`recent_weekly_volume_km: null`) and explicit-zero (`recent_weekly_volume_km: 0`) evidence, returns HTTP 500 in all eight cases — confirming the defect is present across the entire already-`PUBLICLY_ACTIVE` Core horizon range, not an isolated 12-week artifact.
- **B (durable baseline reproduction)**: `FREQ.6D.8` already independently verified this against the pre-`FREQ.6D.7` durable baseline commit via a temporary git worktree; not re-run here (no code changed since, nothing to re-verify).

This is correctly classified as pre-existing, not a Runway regression — confirmed again this phase (see §4-9 below for exactly why).

No next-phase ID was named verbatim in `MASTER_ROADMAP.md` (it describes the next work by content only, as in every prior phase in this chain). Following this engagement's established sequential numbering, this phase is `FREQ.6D.9`.

```
git rev-parse HEAD                                     → f41d94170a1733cc8299abe3567112d8634b3945
git branch --show-current                                → main
git status --short                                        → m baseline_tmp
                                                              M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.json
                                                              M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.md
git rev-list --left-right --count origin/main...HEAD     → 0  7   (0 behind, 7 ahead)
git diff --check                                          → clean
```

7 commits ahead of `origin/main` (the `FREQ.6D.7`/`FREQ.6D.8` chain) — 2 phases since the last durability gate, well under the ~10-phase threshold. The two pre-existing dirty files are unrelated, preserved unstaged, exactly as every prior phase.

## 2. FREQ.6C authority reconstruction — `INTERMEDIATE_5D_FROZEN_NUMERIC_AUTHORITY_TABLE`

| Authority | Value | Semantic meaning | Decision provenance | Status | Owner (intended) |
|---|---|---|---|---|---|
| Missing-readiness starting volume | **26.0 km** | Week-1 weekly volume when `RecentWeeklyVolumeKm` is not provided | `FREQ.6C` closure §A — "Direct evidence anchor: Hal Higdon's real Week-1 5-day total" | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, `APPROVED` | Not yet wired into any runtime policy class |
| Explicit-zero starting volume | **19.5 km** | Week-1 weekly volume when `RecentWeeklyVolumeKm` is explicitly reported as 0 | `FREQ.6C` closure §A — `26.0 × 0.75`, reusing 4D's own missing:explicit-zero ratio (16:12) applied to 5D's own anchor, not 4D's absolute values | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, `APPROVED` | Not yet wired |
| Resolved peak reference | **44.5 km** | `GoldenFixtureResolvedPeakKm`-equivalent for 5D — the reference peak used to compute the starting→peak growth multiplier | `FREQ.6C` closure §A/§B — center of Higdon's real peak estimate (43-46km); `canonicalDefaultMultiplier = 44.5 / 26.0 = 1.71154` | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, `APPROVED`, explicitly never `GoldenFixtureDerived` | Not yet wired |
| KEY2:KEY1 relative dose | **70%** | Future asymmetric-allocation prescription-dose target (KEY2 as a percentage of KEY1's distance) | `FREQ.6C` closure §A — midpoint of a Norwegian-method-anchored envelope | `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, `APPROVED`, but "a stored numeric target for the not-yet-built prescription-profile capability... has no runtime effect today" | Not applicable yet — `FourDaySessionDistanceAllocationPolicy`'s real, running mechanism is still equal-split, unmodified |
| Long-run selection share | **28%** | Target long-run share of weekly volume for 5D | `FREQ.6C` closure §A | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, `APPROVED` | Not yet wired (runtime uses `VolumeSafetyPolicy.Default`'s 33%) |
| Long-run hard cap | **36%** | Maximum permitted long-run share for 5D | `FREQ.6C` closure §A | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, `APPROVED` | Not yet wired (runtime uses `VolumeSafetyPolicy.Default`'s 40%) |
| Peak volume band (minimum/maximum) | **36-50 km** | Real, catalog-published `PEAK_VOLUME_BANDS_V1 v3` entry for `TEN_K/INTERMEDIATE/runsPerWeek=5` | `plan-catalog/catalog/policies/peak-volume-bands.v3.json`, referenced by the live `APPSEL_RACE_PLAN_V1 v4` rule pack the real `TEN_K__5D__INTERMEDIATE` combination uses | **Already correctly wired and live** — confirmed by direct code read of `CatalogPeakVolumeBandLoader`/`CatalogVolumeAndLongRunPlanner.Build`'s `bounds` construction | `CatalogPeakVolumeBandLoader` (catalog-driven, works today) |
| Per-role/session minima | KEY = 3.0 km, EASY = 1.5 km | `V1FourDaySessionVolumeAllocationPolicy.MinimumKeySessionDistanceKm`/`MinimumEasySupportDistanceKm` | Pre-existing, shared, unmodified since `FREQ.4` | `DirectCanonicalRule`, unrelated to this reconciliation (never candidate-specific) | `V1FourDaySessionVolumeAllocationPolicy`/`FourDaySessionDistanceAllocationPolicy` (real, running, correct) |
| 8/10/12/14-week trajectory targets | Full reachable/taper table, both readiness states | `FREQ.6C` closure §B (reproduced in §11 below) | `TechnicalDeterministicRule` applied to the approved 26.0/19.5/44.5 inputs | `APPROVED`, all 14 cells `ELIGIBLE` | Not yet wired |

**Terminology precision** (per this phase's own §2 instruction not to conflate axes): `PeakVolumeBand` (36-50km, catalog-driven, clamps the *reachable* peak) is distinct from `ResolvedPeakReference` (44.5km, the *reference* used only to compute the growth-rate multiplier) is distinct from `StartingVolume` (26.0/19.5km, Week-1 volume) is distinct from `CoreEntryVolume` (not a separate concept in Core-only requests — Core-only Week 1 *is* the starting volume) is distinct from `ActualAchievedPeak` (the runtime-computed, clamped `SelectedPeakKm` for a given horizon, always ≤ the band's maximum and ≥ its minimum where reachable). All six are handled correctly and distinctly by the existing `CatalogVolumeAndLongRunPlanner`/`ReachablePeakDecision` contracts — the defect is exclusively in which *inputs* (`GoldenFixtureStartingVolumeKm`, `ResolvedPeakReference.Value`, and the missing/zero starting-volume policy) are fed into that otherwise-correct machinery for a 5D candidate.

## 3. Specific historical value audit (26 / 19.5 / 16 / 12 km)

| km value | First appears as | Exact repository meaning |
|---|---|---|
| 26.0 | `FREQ.6C` closure §A, checkpoint §7-8 | **Final approved Intermediate×5D missing-readiness starting volume.** "Direct evidence anchor — Hal Higdon's real Week-1 5-day total." Also reused as `GoldenFixtureStartingVolumeKm` for the 5D growth-multiplier calculation ("self-referential, not borrowed from 4D"). |
| 19.5 | `FREQ.6C` closure §A, checkpoint §7-8 | **Final approved Intermediate×5D explicit-zero starting volume.** `= 26.0 × 0.75`, reusing 4D's own missing:explicit-zero *ratio* (0.75), not 4D's absolute values. |
| 16 | `V1MissingReadinessStartingVolumePolicy.cs` (live code); `FREQ.6B`, `FREQ.6C` checkpoint/closure (cited only) | **Intermediate×4D's own live missing-readiness constant** (`MissingWeeklyVolumeDefaultKm`). Every `FREQ.6B`/`FREQ.6C` document that mentions it explicitly labels it a 4D reference figure, used only for comparison or to supply the 0.75 ratio — never proposed or selected as a 5D value. `FREQ.6C` closure §B: *"4D's own 16km value shown only as an out-of-envelope reference point — neither is what got selected."* |
| 12 | `V1MissingReadinessStartingVolumePolicy.cs` (live code); `FREQ.6B`, `FREQ.6C` checkpoint (cited only) | **Intermediate×4D's own live explicit-zero constant** (`ExplicitZeroWeeklyVolumeDefaultKm`). Same treatment as 16km — a 4D reference/ratio component, never a 5D selection. |

No document in the `FREQ.6`/`FREQ.6B`/`FREQ.6C` chain ever proposes or approves 16km or 12km as Intermediate×5D values. `FREQ.6` (the earliest document) contains none of these four figures at all — it predates the numeric evidence/selection work and explicitly records "no approved 5D starting-volume missing/explicit-zero authority was found" at that point.

## 4. Current implementation trace — `CURRENT_5D_STARTING_VOLUME_RESOLUTION_TRACE`

```
GeneratePreviewRequest (Intermediate, 5 days/week, TEN_K)
  → V1CatalogPilotIdentityPolicy.ResolveCandidate(Intermediate, 5) → TEN_K__5D__INTERMEDIATE v1
  → CatalogVolumeAndLongRunPlanner.Build(request)
      → request.Candidate.Level == "NEW" && DaysPerWeek == 4 ?  NO
      → request.Candidate.DaysPerWeek == 3 ?                     NO
      → (no third branch exists for DaysPerWeek == 5)
      → _policy remains VolumeSafetyPolicy.Default   ← the SAME policy object Intermediate×4D uses
  → ResolveStartingVolume(context)
      → readiness.WeeklyVolume.State == NotProvided (missing) or Available+0 (explicit zero)
      → ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)? NO
      → ReferenceEquals(_policy, VolumeSafetyPolicy.BeginnerFourDay)?      NO
      → V1MissingReadinessStartingVolumePolicy.Resolve(readiness)
          → missing  → MissingWeeklyVolumeDefaultKm      = 16 km
          → explicit-zero → ExplicitZeroWeeklyVolumeDefaultKm = 12 km
  → ResolvePeak(startingVolumeKm, bounds, boundPlan)
      → bounds = real catalog PeakVolumeBand for runsPerWeek=5 → [36, 50] km  (CORRECT, 5D-specific)
      → canonicalDefaultMultiplier = _policy.ResolvedPeakReference.Value / _policy.GoldenFixtureStartingVolumeKm
                                    = 38 / 24 = 1.5833...          ← VolumeSafetyPolicy.Default's 4D-tuned ratio
      → reachable = startingVolumeKm * transitionAdjustedMultiplier   (≈16*1.58≈25.3 for missing; ≈12*1.58≈19 for zero)
      → reachable < bounds.MinimumKm(36) → selected = reachable (BelowTypicalPeakButValid; no exception here)
  → BuildWeeklyPlan(...)  → interpolates every non-taper week between starting and selected peak; taper = previous*0.53
  → FourDaySessionDistanceAllocationPolicy.Allocate(weekly, longRun, keySessionCount=2, easySupportCount=2)
      → residual = weekly - longRun
      → requiredMinimum = 2*3.0 + 2*1.5 = 9.0 km
      → residual + tolerance < 9.0  →  CatalogSessionPrescriptionInfeasibleException("Residual volume Xkm cannot support V1 key/easy minimums.")
  → surfaces as HTTP 500 (Core-only route, generic exception handling) or HTTP 422 PREPARATION_RUNWAY_PREVIEW_GENERATION_FAILED (Runway route, typed fail-closed wrapping)
```

Confirmed by direct read of `CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`, `V1MissingReadinessStartingVolumePolicy.cs`, and `CatalogPeakVolumeBandLoader.cs` this phase.

## 5. Verified 16/12 behavior

Confirmed from real current code and this phase's own real HTTP reproduction: missing → `V1MissingReadinessStartingVolumePolicy` → **16 km**; explicit zero → **12 km**. Both figures match exactly what `FREQ.6B`/`FREQ.6C` cite as Intermediate×4D's live constants — this is **generic fallthrough behavior**, not an intentional Intermediate×5D decision. No code anywhere branches on `DaysPerWeek == 5` before reaching this policy; the policy's own source comment self-identifies its scope as `"...TEN_K/INTERMEDIATE/4D missing-zero readiness closure"`.

## 6. Authority vs fallthrough classification

**`ACCIDENTAL_IMPLEMENTATION_FALLTHROUGH`.**

Rationale: `CatalogVolumeAndLongRunPlanner.Build` explicitly special-cases exactly two combinations — `Level=="NEW" && DaysPerWeek==4` (Beginner×4D) and `DaysPerWeek==3` (Intermediate×3D) — each dispatching to its own dedicated `VolumeSafetyPolicy` variant and its own dedicated missing-readiness policy class. Intermediate×5D was never given an equivalent third branch. It reaches `VolumeSafetyPolicy.Default`/`V1MissingReadinessStartingVolumePolicy` purely because no `else if (DaysPerWeek == 5)` clause exists — the identical structural situation 3D and Beginner×4D were in *before* their dedicated branches were written, not evidence that 5D's case was ever separately evaluated and deliberately left generic. This is confirmed, not inferred: `V1MissingReadinessStartingVolumePolicy`'s own provenance string names 4D specifically, and `VolumeSafetyPolicy.Default`'s `GoldenFixtureStartingVolumeKm`/`ResolvedPeakReference` are the TEN_K_MASTER v6 4D golden-fixture constants (24km/38km), unrelated to 5D's own approved 26.0/44.5.

## 7. `FREQ6D6_NUMERIC_AUTHORITY_RECONCILIATION`

**Outcome: `SUPERSEDED_BY_EXISTING_FREQ6C_AUTHORITY`.**

`FREQ.6D.6`'s report stated: *"direct repository-truth discovery that `CatalogVolumeAndLongRunPlanner.Build` only special-cases Intermediate×3D and Beginner×4D — Intermediate×5D already falls through to `VolumeSafetyPolicy.Default`, whose missing/zero-readiness resolution already uses the exact same `V1MissingReadinessStartingVolumePolicy` (16km missing / 12km explicit-zero) Runway itself uses — so adopting it for 5D Runway confirms, rather than borrows, the already-live Intermediate×5D Core numeric authority."*

This conclusion is corrected here, as a governance/authority correction, not blame: `FREQ.6D.6` correctly identified the fallthrough *mechanism* (accurate — the code trace is right), but incorrectly characterized reaching a generically-named, 4D-provenance-labeled policy via unconditional fallthrough as "already-live Intermediate×5D Core numeric authority." It did not cross-reference the `FREQ.6C` closure decision, which had *already* approved distinct, 5D-specific, higher values (26.0/19.5) for exactly this purpose roughly two phases earlier in the same numbered chain. `FREQ.6D.6`'s own scope was Preparation Runway product policy (weekly structure, and reuse of *whatever* Core's starting-volume authority turned out to be) — it did not set out to audit Core's starting-volume correctness itself, and its "confirms rather than borrows" framing turned out to rest on an unverified premise. The Runway weekly-*structure* decision (1 KEY + 3 EASY + 1 LONG, second KEY at Core entry) is entirely unaffected by this correction and remains valid — only the starting-volume-authority premise needs correcting.

## 8. Exact failure mechanism

Both `FourDaySessionDistanceAllocationPolicy.Allocate`'s explicit feasibility guard fire in the same way, at the same structural constant: `requiredMinimum = keySessionCount(2)*MinimumKeySessionDistanceKm(3.0) + easySupportCount(2)*MinimumEasySupportDistanceKm(1.5) = 9.0 km`, checked against `residual = weekly - longRun` for every non-taper week and the taper week alike.

- **Explicit-zero (starting = 12 km)**: infeasible immediately at Week 1 — `residual = 12 * (1 - 0.33) ≈ 8.04 km < 9.0 km`. This is the exact, previously-observed real error text: *"Week 1 residual volume 8km cannot support V1 key/easy minimums."*
- **Missing (starting = 16 km)**: Week 1 itself is feasible (`16 * 0.67 ≈ 10.72 km ≥ 9.0 km`), but real HTTP reproduction this phase shows every horizon (8/10/12/14) still fails. The most likely mechanism (not independently re-verified line-by-line this phase, but consistent with every other data point gathered): the *Taper* week, which reduces the previous week's volume by the fixed 0.53 multiplier against a peak trajectory anchored on the under-scaled 4D-golden-fixture growth multiplier (1.58x rather than 5D's own approved 1.71x), lands close enough to the same 9.0km structural floor that post-rounding it dips below it. Either way, the root numeric cause is identical: the wrong (4D-scoped, under-sized) starting-volume/peak-reference authority feeding a real 2-KEY structural minimum it was never sized against.

This is **not** a weekly-structural-minimum defect, **not** an allocation-mechanism defect, **not** a progression-cap defect, and **not** a Taper-floor defect in themselves — `FourDaySessionDistanceAllocationPolicy`, its 9.0km 2-KEY+2-EASY minimum, and the 0.53 taper multiplier are all correct, unmodified, and shared correctly with 4D. The single, precise defect is the *starting-volume/peak-reference input* feeding an otherwise-correct pipeline.

## 9. Minimum 5D weekly representability — `INTERMEDIATE_5D_MINIMUM_WEEKLY_VOLUME_DERIVATION`

Using only real, existing, unmodified minima (`MinimumKeySessionDistanceKm=3.0`, `MinimumEasySupportDistanceKm=1.5`, both from `V1FourDaySessionVolumeAllocationPolicy`, unchanged since `FREQ.4`) and the real 2 KEY + 2 EASY + 1 LONG Core shape:

```
requiredNonLongMinimumKm = 2*3.0 + 2*1.5 = 9.0 km
minimumWeeklyVolumeKm    = requiredNonLongMinimumKm / (1 - LongRunSelectionShare)
```

- Using the runtime's current (wrong, 4D-scoped) `LongRunSelectionShare = 0.33`: **minimum ≈ 13.43 km**.
- Using the `FREQ.6C`-approved 5D `LongRunSelectionShare = 0.28`: **minimum ≈ 12.5 km**.

Against this representability floor: **12 km sits below it** (both variants) — confirming explicit-zero's current 12km value is structurally infeasible regardless of which long-run share is used. **16 km sits above the current-share floor (13.43) but only barely, with a rounding-sensitive taper-week margin** — consistent with the observed all-horizons failure. **19.5 km and 26.0 km both sit comfortably above either floor**, with roughly 45-105% headroom.

## 10. KEY2 floor audit

**Classification: `DIFFERENT_FAILURE`, related but distinct.**

`FREQ.6C`'s previously-disclosed "KEY2 floor edge case" (checkpoint §13, closure §D) is a *theoretical, deferred* risk specific to a *future, not-yet-built* asymmetric KEY1/KEY2 allocation mechanism (the approved 70% KEY2:KEY1 dose ratio has "no runtime effect today" — `FourDaySessionDistanceAllocationPolicy`'s real, running mechanism is still an equal-distance split, unmodified since `FREQ.4`). That flag concerns a scenario where `keyTotal` sits at its absolute theoretical floor (6.0km) combined with the *low end* (60%, not 70%) of the dose envelope, producing a KEY2 below its own 3.0km per-session minimum — explicitly "not realized anywhere in this closure's actual matrix," a documentation flag for a future implementer.

The real failure observed this phase and in `FREQ.6D.7`/`FREQ.6D.8` is a coarser, already-realized, root-level failure: the *combined* residual (both KEY sessions plus both EASY sessions together, under the current equal-split mechanism that is actually running) is insufficient for *any* symmetric allocation at all — not an asymmetric-split edge case. The two are related only in both ultimately being about 2-KEY minimum feasibility; the theoretical KEY2 floor concern remains completely unreachable today (no asymmetric allocation exists in running code) and is unaffected by this reconciliation.

## 11. `INTERMEDIATE_5D_CORE_READINESS_FEASIBILITY_MATRIX`

| Horizon | Readiness | Resolved start (current, wrong) | Min. feasible full-layout volume | Core trajectory (current) | Taper feasibility | Result | Exact failure reason |
|---|---|---|---|---|---|---|---|
| 8 | Missing | 16 km | ~13.4 km | starting→~25.3km peak (below 36-50 band, valid) | Marginal/fails | **FAIL (HTTP 500)** | Residual < 9.0km at taper (rounding-sensitive) |
| 8 | Explicit-zero | 12 km | ~13.4 km | starting→~19km | Fails at Week 1 | **FAIL (HTTP 500)** | Week-1 residual ≈8.0km < 9.0km |
| 10 | Missing | 16 km | ~13.4 km | as above | Marginal/fails | **FAIL (HTTP 500)** | Same as 8wk |
| 10 | Explicit-zero | 12 km | ~13.4 km | as above | Fails at Week 1 | **FAIL (HTTP 500)** | Week-1 residual ≈8.0km |
| 12 | Missing | 16 km | ~13.4 km | as above | Marginal/fails | **FAIL (HTTP 500 / 422 via Runway)** | Same mechanism |
| 12 | Explicit-zero | 12 km | ~13.4 km | as above | Fails at Week 1 | **FAIL (HTTP 500 / 422 via Runway)** | Week-1 residual ≈8.0km |
| 14 | Missing | 16 km | ~13.4 km | as above | Marginal/fails | **FAIL (HTTP 500)** | Same mechanism |
| 14 | Explicit-zero | 12 km | ~13.4 km | as above | Fails at Week 1 | **FAIL (HTTP 500)** | Week-1 residual ≈8.0km |

All eight cells re-verified this phase via real HTTP against the real host/DB (§1). Using the `FREQ.6C`-approved 26.0/19.5 starting values and 44.5 peak reference instead, every one of `FREQ.6C`'s own already-computed 14 cells (7 horizons × 2 readiness states, closure §B/§C) is `ELIGIBLE` — comfortably above both the current-share and approved-share minimum floors at every horizon, including the taper week.

## 12-13. Runway impact and Runway-start vs Core-entry distinction

Runway itself (1 KEY + 3 EASY + 1 LONG, `FREQ.6D.6`) does successfully represent the current 16/12 values at its own Week 1 — confirmed by `FREQ.6D.8`'s real HTTP proof that positive-observed and (before this reconciliation) even lower Runway-side evidence generates without incident, because Runway's own minimum floor is lower (1 KEY + 3 EASY = 1×3.0 + 3×1.5 = 7.5 km non-long minimum, versus Core's 9.0 km for 2 KEY + 2 EASY). The failure occurs exactly at the **Runway→Core boundary**: the Runway engine does not re-resolve or grow toward a separate Core-entry target — per direct code trace of `TenKPreparationRunwayDarkOrchestrator` (`PreparationRunwayCoreWeekOneTargetAdapter`), Core generates its own real Week 1 independently (via the same `CatalogVolumeAndLongRunPlanner.Build` traced in §4, with the same missing/zero starting-volume policy), and Runway's own numeric materializer interpolates its final ("PreSpecificTransition") week to exactly match Core's Week 1 target as a boundary-continuity requirement. When Core's own Week 1 generation throws (§4/§8), the entire combined Runway+Core generation fails together — this is why `FREQ.6D.8`'s Runway-side missing/zero tests failed identically to the Core-only route, at the same underlying cause, not a separate Runway defect.

This confirms `FREQ.6D.6`'s Runway weekly-*structure* decision was correct and remains unaffected — the error was specifically in believing Core's *starting-volume authority* was already 5D-correct, not in anything about Runway's own 1K+3E+1L representability.

## 14. Core-entry authority

Confirmed by direct code trace (`TenKPreparationRunwayDarkOrchestrator.cs`, `PreparationRunwayCoreWeekOneTargetAdapter.cs`): the Runway engine does **not** carry its own volume directly into Core, does **not** target a separate fixed Core reference, and does **not** grow during Runway toward a distinct Core minimum. It **re-resolves Core's real starting volume independently** (Core computes its own genuine Week 1 through the exact same `CatalogVolumeAndLongRunPlanner.Build` path traced above), then Runway's own final week is numerically interpolated to land exactly on whatever Core produced (proven correct and unchanged by `FREQ.6D.7`'s `AnalyzeContinuity` fix). Core is the single authority; Runway follows it, never the reverse.

## 15. Positive-observed control group

Empirically (from `FREQ.6D.8`'s and this engagement's existing real-HTTP test fixtures, all of which use `RecentWeeklyVolumeKm` in the 20-30km range and consistently succeed across every horizon 8-20 weeks): real observed evidence at or above roughly 20km reliably generates successfully for every Core horizon. This is empirical representability evidence only — consistent with, but not converted into, product authority; the already-approved 26.0/19.5 `FREQ.6C` values happen to sit within/near this same empirically-successful range, reinforcing (not establishing) their soundness.

## 16-19. Authority conflict table — `INTERMEDIATE_5D_NUMERIC_AUTHORITY_CONFLICT_TABLE`

| Source | Missing-readiness value | Explicit-zero value | Peak reference | Long-run share |
|---|---|---|---|---|
| `FREQ.6C` decision artifacts | 26.0 km | 19.5 km | 44.5 km | 28% / 36% cap |
| `FREQ.6D.6` interpretation | (assumed) 16 km, "already-live 5D authority" | (assumed) 12 km | (not addressed) | (not addressed) |
| Current `CatalogVolumeAndLongRunPlanner` runtime behavior | 16 km | 12 km | 38 km (via 4D `ResolvedPeakReference`) | 33% / 40% cap |
| `FREQ.6D.8` real runtime evidence | Fails all 8/10/12/14wk horizons | Fails all 8/10/12/14wk horizons | n/a | n/a |

**Every disagreement resolves in favor of `FREQ.6C`**: it is the earliest-in-time, most specific (Intermediate×5D-only), most evidence-grounded (Higdon anchor + explicit envelope selection), and most procedurally authoritative artifact (a dedicated `EVIDENCE + PRODUCT/NUMERIC AUTHORITY DECISION` phase whose own final classification was `INTERMEDIATE_5D_NUMERIC_AUTHORITY_APPROVED`). `FREQ.6D.6`'s interpretation is superseded per §7 above. The current runtime and `FREQ.6D.8`'s real-evidence failures are simply the observable *symptom* of runtime never having been wired to `FREQ.6C`'s authority — decision priority (§16 of the phase prompt) resolves cleanly at "**FIRST: can an existing approved value already resolve this?**" — yes.

Per §16-19 of this phase's instructions ("decision priority... FIRST: existing approved value"): **Outcome A (Existing FREQ.6C Authority) applies.** No new numeric research, no product-ineligibility evaluation, and no external evidence research (§21) were required or performed — `FREQ.6C` already did that work and already selected final values.

## 20. Eligibility semantics

Intermediate×5D remains a fully supported/public identity — this reconciliation finds an implementation-authority mismatch, not a representability gap. No `PRODUCT_INELIGIBLE` classification is warranted: `FREQ.6C`'s own closure §C already proved all 14 cells (7 horizons × missing/explicit-zero) `ELIGIBLE` using the correct 26.0/19.5 inputs. Missing and explicit-zero are correctly kept distinct throughout (26.0 vs 19.5, never conflated) — matching this phase's own §20 instruction.

## 21. Runway policy disposition

**`CLARIFIED_RUNWAY_START_VS_CORE_ENTRY`.** `FREQ.6D.6`'s Runway weekly-*structure* decision (1 KEY + 3 EASY + 1 LONG, second KEY only at Core entry) requires no change and is not reopened. Its starting-volume-authority *premise* ("adopting [16/12] for 5D Runway confirms... the already-live Intermediate×5D Core numeric authority") is superseded per §7 — the correction is that Runway's numeric materializer already correctly targets *whatever Core's real Week 1 authority is*; once Core is wired to `FREQ.6C`'s 26.0/19.5, Runway's existing, unmodified interpolation mechanism will automatically produce the correct boundary-continuous value, with no Runway-side code or documentation change required beyond this one corrected sentence.

## 22. Selected behavior

**Missing-readiness**: use the `FREQ.6C`-approved **26.0 km** (not the current 16 km). **Explicit-zero**: use the `FREQ.6C`-approved **19.5 km** (not the current 12 km). Both remain distinct, never conflated. Peak reference: **44.5 km**. Long-run selection/hard-cap share: **28%/36%**.

## 23. Implementation contract (for the next phase)

Per this phase's own §36 ("if existing numeric authority wins... next implementation should only..."), the next phase should be a **narrow implementation + real verification** phase that:

1. Adds a dedicated `VolumeSafetyPolicy.FiveDayIntermediate` (or exact repo-conformant name) carrying `GoldenFixtureStartingVolumeKm=26.0`, `ResolvedPeakReference=(44.5, ProductDefaultWithEvidenceEnvelope)`, `LongRunPreferredMinimumShare`/`Maximum`/`SelectionShare=0.28`/`HardCapShare=0.36` per `FREQ.6C`'s exact table (§A of that closure), mirroring the existing `ThreeDayIntermediate`/`BeginnerFourDay` pattern exactly.
2. Adds a dedicated `V1FiveDayMissingReadinessStartingVolumePolicy` (or exact repo-conformant name) with `MissingWeeklyVolumeDefaultKm=26.0`, `ExplicitZeroWeeklyVolumeDefaultKm=19.5`, provenance citing this phase and `FREQ.6C`'s closure — mirroring `V1ThreeDayMissingReadinessStartingVolumePolicy`/`V1BeginnerFourDayMissingReadinessStartingVolumePolicy` exactly.
3. Adds the dispatch branch in `CatalogVolumeAndLongRunPlanner.Build`/`ResolveStartingVolume` for `DaysPerWeek == 5` (Intermediate), removing the accidental fallthrough.
4. Adds direct numeric-policy unit tests proving the new policy resolves exactly 26.0/19.5 and that `VolumeSafetyPolicy.FiveDayIntermediate`'s peak/share fields match `FREQ.6C`'s table exactly.
5. Adds real 8/10/12/14-week Core-only missing/zero HTTP tests (all 8 currently-failing cases from §11 above), proving 200 with the correct starting volume observable in the response/trace.
6. Adds real 15/17/20-week Runway+Core missing/zero HTTP tests, proving the same, plus the correct Runway-final-week/Core-Week-1 boundary values.
7. Adds real PostgreSQL confirmation for at least one missing and one zero case.
8. Re-verifies positive-observed behavior remains byte-for-byte unchanged (positive evidence never reaches this policy at all, so this should be a pure regression check, not a new code path).
9. Explicitly does **not** touch `FourDaySessionDistanceAllocationPolicy`'s minima, the KEY2 asymmetric-dose mechanism (still not built), Runway structure, Core phase architecture, or catalog content.

## 24. Remaining blocker

None for this reconciliation's own scope — `FREQ.6C` authority is confirmed sufficient and complete for missing/explicit-zero closure. The `FREQ.6C`-approved 70% KEY2:KEY1 asymmetric dose remains stored but unimplemented (unrelated, pre-existing, deliberately deferred per `FREQ.6C` itself — "no runtime effect today," not blocking anything here).

## 25. Next phase

Narrow **IMPLEMENTATION + REAL VERIFICATION** per §23's contract above — wire `CatalogVolumeAndLongRunPlanner` to the confirmed existing `FREQ.6C` authority for Intermediate×5D, add the required test matrix, and re-run real HTTP/PostgreSQL verification for the 8 currently-failing Core-only cases plus the corresponding Runway+Core cases. LongHorizon (`FREQ.6D.5`'s architecture-design gap) remains untouched and unscheduled, exactly as this phase's own §38 requires.

## 26. Final classification

`INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`
