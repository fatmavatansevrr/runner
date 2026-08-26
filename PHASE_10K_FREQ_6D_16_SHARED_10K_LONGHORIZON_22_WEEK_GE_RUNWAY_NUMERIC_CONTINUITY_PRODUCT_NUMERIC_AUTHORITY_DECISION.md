# PHASE 10K-FREQ.6D.16 — Shared 10K LongHorizon 22-Week GE→Runway Numeric Continuity Product / Numeric Authority Decision

**Type:** EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION
**Parent phase:** FREQ.6D.15 (root-caused and disclosed the blocker this phase resolves)
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. No production code, test-fix implementation, migration, routing, or catalog content was authored this phase — evidence and decision only, per its own explicit scope.

---

## 1. Governance preflight

- HEAD at start: `510157b`, 0/0 divergence from origin.
- `FREQ.6D.16` confirmed unreserved (no collisions) and scheduled — commit `f9e2a2d`.
- `FREQ.6D.15` confirmed latest completed phase, Final Classification `INTERMEDIATE_5D_LONGHORIZON_DARK_COMPLETION_BLOCKED_ON_SHARED_22_WEEK_NUMERIC_AUTHORITY`, confirmed to have established: (1) the defect reproduces on both 5D and unmodified 4D, (2) it is day-count-neutral, (3) it is not caused by any FREQ.6D.13/6D.14 change (both phases' own work is untouched by this finding), (4) it cannot be safely fixed without a real product/numeric decision.

---

## 2. Method

All numeric evidence in this report was gathered via temporary, uncommitted, read-only diagnostic tests directly against the real production code (`LongHorizonGeStructuralSelector`, `LongHorizonGeNumericExecutor`, `LongHorizonFullNumericOrchestrator`, `PreparationRunwayCoreWeekOneTargetAdapter`) — never fabricated. The diagnostics (including one temporary `Console.Error.WriteLine` inside `LongHorizonFullNumericOrchestrator.ExecuteAsync`, reverted immediately after use) were discarded before this report was written; `git status`/`git diff` confirm zero residual production-code changes from this phase.

---

## 3-4. SHARED_22_WEEK_LONGHORIZON_CONTINUITY_TRACE

**5D, baseline evidence (26.0km weekly, 7.8km longest-run, 5 runs/week) — the FREQ.6D.9/10-approved missing-readiness anchor:**

| Horizon | GE weeks | GE trajectory (weekly / long-run, per week) | Core Week-1 target (independent) | GE exit vs target | Result |
|---|---|---|---|---|---|
| 21 | 1 | wk1: 26 / 7.5 | weekly=26, longRun=7.5 | Δweekly=0, Δlongrun=0 | **SUCCESS** |
| 22 | 2 | wk1: 26/7.5, wk2: 28/8 | weekly=26, longRun=7.5 | Δweekly=+2, Δlongrun=+0.5 | **FAIL** (weekly) |
| 23 | 3 | wk1: 26/7.5, wk2: 28/8, wk3: 30/8.5 | weekly=26, longRun=7.5 | Δweekly=+4, Δlongrun=+1.0 | **FAIL** (weekly) |
| 24 | 4 | wk1-3 as above, wk4 (Recovery): 25.5/7 | weekly=26, longRun=7.5 | Δweekly=−0.5, Δlongrun=−0.5 | **SUCCESS** |

**4D, baseline evidence (20.0km weekly, 6.0km longest-run, 4 runs/week) — the pre-existing `TypicalBaseline`-adjacent anchor:**

| Horizon | GE weeks | GE trajectory (weekly / long-run, per week) | Core Week-1 target (independent) | GE exit vs target | Result |
|---|---|---|---|---|---|
| 21 | 1 | wk1: 20 / 6 | weekly=20, longRun=6 | Δweekly=0, Δlongrun=0 | **SUCCESS** |
| 22 | 2 | wk1: 20/6, wk2: 21.5/7 | weekly=20, longRun=6 | Δweekly=+1.5, Δlongrun=+1.0 | **FAIL** (weekly) |
| 23 | 3 | wk1: 20/6, wk2: 21.5/7, wk3: 23/7.5 | weekly=20, longRun=6 | Δweekly=+3, Δlongrun=+1.5 | **FAIL** (weekly) |
| 24 | 4 | wk1-3 as above, wk4 (Recovery): 19.5/6.5 | weekly=20, longRun=6 | Δweekly=−0.5, **Δlongrun=+0.5** | **FAIL** (long-run) |

**Critical, previously-unobserved finding:** at 4D/24-weeks the weekly-volume check passes (GE exit 19.5 ≤ target 20), but the **long-run** check independently fails: the Recovery week's reduced long run (6.5km) still exceeds Core's target long run (6.0km) by exactly one rounding increment (0.5km). This is a *different* failure than 5D/24-weeks (which succeeds on both checks) — proving the defect is not a single, narrow off-by-one, but a genuine, magnitude-sensitive interaction that can trip on either axis depending on the specific evidence.

---

## 5. Authority ownership

| Value | Owner | Classification |
|---|---|---|
| GE start volume | `LongHorizonGeNumericExecutor.Execute` (first-week = raw baseline, unprogressed) | GE_SPECIFIC (shared formula, frequency-neutral) |
| GE weekly growth | `ApplyDevelopmentProgressionCap` (reuses `VolumeSafetyPolicy.PreferredMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm`/`HardMaxWeeklyIncreaseRatio`) | CANONICAL_SHARED_LONGHORIZON (same formula for 4D/5D, only the policy instance's constants differ by frequency) |
| GE cap (5D only) | `VolumeSafetyPolicy.FiveDayIntermediate.ResolvedPeakReference` (44.5km, FREQ.6D.12) | FREQUENCY_SPECIFIC, already approved, untouched |
| GE rounding | `VolumeSafetyPolicy.RoundingIncrementKm` (0.5km, shared) | CANONICAL_SHARED_LONGHORIZON |
| Runway starting/reference volume | `PreparationRunwayNumericMaterializer.ResolveStartingWeeklyVolume` — currently **always the raw GE exit, unclamped** | GE_SPECIFIC by construction today (no independent Runway-owned floor/ceiling) |
| Runway progression | `PreparationRunwayNumericMaterializer.Materialize`'s linear interpolation (`start + (target-start)*progress`) | RUNWAY_SPECIFIC, existing, correct in isolation |
| Runway rounding | Same `RoundingIncrementKm` as GE, via the candidate-aware policy | CANONICAL_SHARED_LONGHORIZON |
| Weekly-volume continuity tolerance | `ContinuityToleranceKm` = `V1FourDaySessionVolumeAllocationPolicy.ToleranceKm` = **0.001km** | **IMPLEMENTATION_ONLY** — see §9 |
| Long-run continuity tolerance | Same `ContinuityToleranceKm` (0.001km), reused for a second, semantically distinct check | **IMPLEMENTATION_ONLY** — see §9 |

---

## 6. 4D historical authority audit

Searched `PHASE4I*`/`PHASE4K*`/`PHASE_10K_FREQ_6D_*` reports for "22-week," "short-GE continuity," or "GE→Runway numeric closure." **No report anywhere explicitly tests or discusses a 21-23 week (1-3 GE week) LongHorizon plan's GE→Runway numeric transition.** `LongHorizonFullNumericOrchestratorTests.cs` (Phase 4I.6A) tests exactly 21 and 24 weeks as its "short horizon success" cases — both of which, per §3-4 above, happen to land at or below Core's target by construction (21 has zero GE growth; the 4D/24-week case was evidently never checked against long-run continuity specifically, or was tested with a baseline whose long-run evidence didn't trip this exact 0.5km margin). **Conclusion: 22 weeks was never tested — not "known but accepted," not "masked by an earlier fixture." This is a genuine, previously-undiscovered gap in LongHorizon's own historical test coverage, not a regression from any recent phase.**

---

## 7. Continuity semantics (per existing repository authority)

Two genuinely distinct continuity concepts exist in the current architecture, and this phase's evidence shows they have been conflated:

1. **GE→Runway growth bound** (`LongHorizonFullExecutionValidator.cs`, confirmed in FREQ.6D.14's own trace): Runway's own Week 1 must not exceed GE's exit by more than the approved 8%/2.5km progression cap. This is a **one-sided upper bound in the growth direction** — already correct, already generic, already approved (FREQ.6D.12's ledger entry explicitly names this "the existing one-sided upper-bound validator"). **Untouched, correct, not the source of the failure.**
2. **Runway-entry-vs-Core-boundary reachability** (`PreparationRunwayNumericMaterializer.Materialize`, the actual failing check): asks whether an 8-week linear interpolation can realistically bridge from Runway's *starting* evidence to Core's *independently-computed* Week-1 target. **No repository document defines this check's intended semantics at all** — it exists only as an implementation artifact (`startingWeekly - targetWeekly > tolerance`), reusing an epsilon authored for an unrelated purpose (see §9).

---

## 8. Rounding order audit

Both GE and Runway round via the identical shared convention (`round_nearest_0.5km_after_each_week_value_then_validate`, `VolumeSafetyPolicy.RoundingIncrementKm`). No rounding-order defect was found: GE rounds its own weekly/long-run values before they become Runway's starting evidence; Runway rounds its own interpolated values independently. The 4D/24-week long-run failure (§4) is not a rounding-order bug — it is the *correct*, exactly-one-rounding-increment consequence of GE's Recovery-week reduction landing one increment above Core's target. **No `EXISTING_AUTHORITY_CONFIRMED` classification applies to rounding order itself** — but this exact 0.5km-scale finding directly informs §9's conclusion.

---

## 9. Tolerance authority audit — the exact defect

`TenKPreparationRunwayNumericPolicyFactory.Build`'s own code comment (line 40-43, written during FREQ.6D.10) **already explicitly distinguishes** `LongRunShareTolerance` (a ratio-scale epsilon, corrected during FREQ.6D.10 to avoid exactly this class of conflation) from `ContinuityToleranceKm`, describing the latter as **"a km-scale epsilon for exact sum-reconciliation checks."** Its value, `V1FourDaySessionVolumeAllocationPolicy.ToleranceKm = 0.001d` (one meter), is correctly sized for verifying that per-slot session distances sum to the weekly total after floating-point rounding — a machine-precision concern.

**This same 0.001km field is reused, unmodified, at `PreparationRunwayNumericMaterializer.Materialize` lines 32 and 41 to gate a completely different, product-level question: how far can Runway's starting evidence legitimately sit from Core's independently-computed boundary before the 8-week bridge is considered infeasible?** No product tolerance for this second question has ever been separately authored. A 1-meter epsilon trivially rejects even the smallest possible real-world overshoot (the 4D/24-week case above is exactly 0.5km — 500× the epsilon).

**This is precisely the same *class* of mistake FREQ.6D.10 already found and fixed once** (a km-scale epsilon silently governing a decision it was never sized for) — but it is a **different instance**, on a different field, at a different call site, not previously touched by FREQ.6D.10's own fix. **I am not repeating that mistake by inventing a new epsilon here; I am identifying that this field's dual reuse is itself the defect.**

---

## 10. Runway entry authority — current answer

**Currently: (B) derived unconditionally from GE's exit, with no clamp, floor, or ceiling of any kind.** `ResolveStartingWeeklyVolume`/`ResolveStartingLongRun` read `request.StartingLoadEvidence` (built directly from `geExit.FinalWeeklyVolumeKm`/`FinalLongRunKm` in `LongHorizonFullNumericOrchestrator.cs`) with no reconciliation against Core's target at all before the `Materialize` call's own hard rejection. **No existing authority currently reconciles GE's independent, forward-only progression with Core's independent, fixed boundary — they are computed by two fully separate code paths that never previously needed to agree, and this phase's evidence is the first to force them into direct comparison at a horizon short enough to expose the gap.**

## 11. GE exit authority

Confirmed (FREQ.6D.12, unchanged): GE follows **generic progression** (7%/2.5km-capped growth, target-capped at 44.5km for 5D) with **no handoff-targeting semantics** — GE was never designed to "aim for" Runway or Core's boundary. This is correct and must not change (§25-27 forbid any GE structural/numeric delta). The mismatch is therefore *necessarily* Runway's/the boundary-check's problem to resolve, not GE's.

## 12. Target-capped GE interaction

Confirmed independent of the 5D 44.5km cap: the failure reproduces identically for 4D (§3-4), which has no target cap at all. **The 44.5km authority is untouched and not implicated.**

## 13. Frequency-neutral root cause

The shared layer is **`PreparationRunwayNumericMaterializer.Materialize`'s reachability check at lines 32/41**, which is invoked identically regardless of `daysPerWeek` (the same method, same two comparisons, same reused `ContinuityToleranceKm` field, for both 4D and 5D `PreparationRunwayNumericPolicy` instances — confirmed in `TenKPreparationRunwayNumericPolicyFactory.Build`, where only the *growth-ratio* constants differ by frequency, not `ContinuityToleranceKm` itself, which is set identically to `V1FourDaySessionVolumeAllocationPolicy.ToleranceKm` for every policy variant). This is why the defect is exactly, unavoidably day-count-neutral.

## 14. External evidence

Not needed. Repository authority (§7-11) is sufficient to identify both the exact defect and a safety-preserving, non-arbitrary resolution without consulting external training literature.

---

## 15-20. Candidate resolutions evaluated

- **(A) Existing-defect fix, standalone:** Partially applicable — the *tolerance field reuse* is a genuine implementation defect (wrong epsilon for the purpose), but fixing only the epsilon's *value* (without addressing what it should represent) risks exactly the "smallest number that passes" anti-pattern this phase's own discipline forbids. **Not selected alone.**
- **(B) Continuity tolerance clarification via existing `RoundingIncrementKm`:** Resolves *only* the 4D/24-week long-run case (a single 0.5km, one-rounding-increment overshoot) — mirrors FREQ.6D.10's own precedent of deriving a tolerance from `RoundingIncrementKm` rather than inventing a value. **Does not resolve 22/23 weeks** (2-4km overshoots, far beyond any plausible rounding-scale tolerance). **Necessary but insufficient alone.**
- **(C) Runway entry derived/clamped from GE, bounded by Core's own target:** Directly supported by existing architecture — Runway's entry is *already* "derived from GE" (§10, current behavior B); this candidate adds the missing half of that same relationship: bounding it against the *other* pre-existing, already-authoritative value in play, Core's own independently-computed Week-1 target (already computed, always available at this point in `LongHorizonFullNumericOrchestrator.ExecuteAsync`, never a new number). Resolves 21/22/23/24 **and** the 4D long-run case **uniformly**, with a single generic rule, no per-horizon branching. **Selected — see §21 for the exact rule.**
- **(D) GE-exit targets Runway:** Rejected — would require GE to know about Runway/Core's boundary, violating §11's confirmed GE-exit authority (generic progression only) and §25-27's prohibition on any GE numeric-semantic change.
- **(E) 22-week PRODUCT_INELIGIBLE:** Rejected — the evidence (§3-4, and FREQ.6D.15's own wider sweep across 23/25/26/27/28) shows this is not an isolated horizon; a large fraction of the 21-52 range would need the same classification, which would not be "a single isolated unsupported horizon" (the phase's own bar for this candidate) but a systemic product retreat with no supporting evidence that these horizons are inherently unsafe or unrepresentable — only that the current *code* fails to reconcile two already-correct authorities.
- **(F) Genuinely new numeric authority:** Not needed — the resolution in §21 introduces no new numeric *value*; it reuses Core's own already-computed target as the clamp ceiling.

---

## 21. Selected rule (no horizon-specific magic number)

**Preparation Runway's starting evidence (`startingWeekly`, `startingLongRun`) is clamped to Core's independently-computed Week-1 target whenever GE's exit would otherwise exceed it, before Runway's own 8-week linear interpolation runs.** Formally: `effectiveStartingWeekly = Min(geExitWeekly, coreWeekOneTarget.WeeklyVolumeKm)`, and identically for long run. When GE's exit is already at or below Core's target (the common case, including every currently-passing horizon), this is a no-op — byte-identical behavior. When GE's exit exceeds Core's target, Runway's own interpolation degenerates gracefully to a flat 8-week bridge at Core's target (the existing formula `start + (target-start)*progress` already handles `start == target` correctly with zero code change to the interpolation itself — only the *value fed into it* as `start` changes).

This single rule is generic across 21-52 weeks, both 4D and 5D (§13), and requires **no new numeric constant** — the ceiling is Core's own already-computed, already-authoritative target. `ContinuityToleranceKm`'s dual-purpose reuse (§9) is resolved by this same change becoming moot for the reachability check specifically (the clamp means `startingWeekly` can never exceed `targetWeekly` by more than true floating-point noise, so the *original* 0.001km epsilon becomes appropriate again for its originally-intended purpose at that comparison) — no separate tolerance-widening fix is needed once the clamp exists.

## 22. 3D / future frequency compatibility

The rule is expressed entirely in terms of GE exit, Core's Week-1 target, and Runway's own interpolation — none of which are 4D/5D-specific concepts. It applies identically to a hypothetical 3D LongHorizon (not currently supported) or future 6D/7D LongHorizon without modification, since it never references day count, session count, or role composition.

## 23. Representability matrix (selected rule, computed from real evidence)

| Days | Horizon | GE exit (wk/lr) | Core target (wk/lr) | Effective Runway start (wk/lr) | Result under selected rule |
|---|---|---|---|---|---|
| 4D | 21 | 20/6 | 20/6 | 20/6 (no clamp) | SUCCESS (unchanged) |
| 4D | 22 | 21.5/7 | 20/6 | **20/6 (clamped)** | **SUCCESS (newly fixed)** |
| 4D | 23 | 23/7.5 | 20/6 | **20/6 (clamped)** | **SUCCESS (newly fixed)** |
| 4D | 24 | 19.5/6.5 | 20/6 | 19.5/6 (long-run clamped only) | **SUCCESS (newly fixed — long-run case resolved)** |
| 4D | 32, 52 | *(not traced this phase — same mechanism applies uniformly)* | — | clamped where needed | Expected SUCCESS, to be confirmed by real dark tests in the implementation phase |
| 5D | 21 | 26/7.5 | 26/7.5 | 26/7.5 (no clamp) | SUCCESS (unchanged) |
| 5D | 22 | 28/8 | 26/7.5 | **26/7.5 (clamped)** | **SUCCESS (newly fixed)** |
| 5D | 23 | 30/8.5 | 26/7.5 | **26/7.5 (clamped)** | **SUCCESS (newly fixed)** |
| 5D | 24 | 25.5/7 | 26/7.5 | 25.5/7 (no clamp needed) | SUCCESS (unchanged) |
| 5D | 32, 52 | *(FREQ.6D.14 already proved SUCCESS with a near-cap baseline; the clamp only ever helps, never hurts, low-baseline cases)* | — | clamped where needed | Expected SUCCESS at a wider range of baselines than FREQ.6D.14's own near-cap-only workaround |

This is **evidence of expected representability**, not a substitute for real dark/DB re-verification, which is explicitly deferred to the implementation phase per §32.

## 24. Safety / monotonicity

The rule only ever **reduces** Runway's own starting point toward Core's already-approved boundary — it never raises it, never weakens the existing GE→Runway growth cap (§7 item 1, untouched), never introduces a new hard-safety ceiling (Core's target already *was* the ceiling FREQ.6D.9-12 approved; this rule simply makes Runway respect it instead of silently failing when GE legitimately arrives above it), and applies identically regardless of frequency (no silent per-frequency fallback).

---

## 25-27. No Core / Runway structural / GE structural delta

Confirmed: the selected rule touches only *which value* is fed as Runway's starting evidence into its own pre-existing interpolation formula. Core's numeric authority, Preparation Runway's structural shape (4D: existing canonical structure; 5D: 1K+3E+1L), and GE's structural shape (4D: 1K+2E+1L; 5D: 1K+3E+1L) are all untouched by this decision.

---

## 28. SHARED_LONGHORIZON_22_WEEK_NUMERIC_AUTHORITY_TABLE

| Row | Current runtime behavior | Canonical authority | Source/provenance | Shared/frequency-specific | Conflict? | Selected authority |
|---|---|---|---|---|---|---|
| GE progression | 7%/2.5km-capped growth from previous week | `VolumeSafetyPolicy.PreferredMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm`/`HardMaxWeeklyIncreaseRatio` | Phase 4G.3B.0, reused unmodified | Shared formula, frequency-specific constants | No | Unchanged |
| GE exit | Result of generic progression, no handoff targeting | Same as above | Same | Shared | No | Unchanged |
| Runway entry | **Raw GE exit, unclamped** | *(none previously existed)* | — | Shared (same code path both frequencies) | **Yes — no reconciliation with Core's target existed** | **Clamped to Core's Week-1 target (§21) — newly made explicit** |
| Weekly continuity | `startingWeekly - targetWeekly > 0.001km` | *(none previously existed — reused sum-reconciliation epsilon)* | `V1FourDaySessionVolumeAllocationPolicy.ToleranceKm`, mis-reused | Shared | **Yes — wrong-purpose epsilon** | Becomes moot once §21's clamp is applied; original epsilon reverts to its intended narrow purpose |
| Long-run continuity | Same 0.001km epsilon, same mis-reuse | Same | Same | Shared | **Yes** | Same resolution as above |
| Rounding | 0.5km, shared convention | `VolumeSafetyPolicy.RoundingIncrementKm` | Phase 4G.3B.0 | Shared | No | Unchanged |
| Tolerance (general) | 0.001km, sum-reconciliation only | Correct for its *original* narrow purpose | Same | Shared | No (once decoupled from the reachability check) | Unchanged for its original use |

## 29. SHARED_LONGHORIZON_22_WEEK_RESOLUTION_MATRIX

| Candidate | Canonical support | New number? | 4D result | 5D result | 21 | 22 | 23 | 24 | Safety | Generality | Selected? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Existing-defect fix (epsilon value only) | Partial | No | Fails 22/23 still | Fails 22/23 still | ✓ | ✗ | ✗ | maybe | Neutral | Low | No |
| Rounding/tolerance clarification alone | Partial (FREQ.6D.10 precedent) | No | Fixes 24 only | N/A (24 already OK) | ✓ | ✗ | ✗ | ✓ | Neutral | Low | No (subsumed by clamp) |
| **Runway-entry clamp to Core's target** | **Yes — reuses the already-authoritative Core target** | **No** | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | Conservative (only reduces) | High (generic, day-count-neutral) | **Yes** |
| GE-exit targeting | No (violates GE-exit authority, §11) | Would require new semantics | — | — | — | — | — | — | — | — | No |
| 22-week PRODUCT_INELIGIBLE | No (evidence shows it's systemic, not isolated) | No | Rejects too much | Rejects too much | ✓ | ✗ activation | ✗ activation | ✓ | Safe but overbroad | Low (arbitrary boundary) | No |
| New numeric authority | Not needed | Would be | — | — | — | — | — | — | — | — | No |

## 30. Decision standard

The selected rule is supported by **DIRECT CANONICAL RULE**: Core's Week-1 target is already the product-approved authoritative boundary for this exact transition (FREQ.6D.9-12's own "GE→Runway→Core continuity" design, described in FREQ.6D.12's ledger entry as governed by "the existing one-sided upper-bound validator" — this decision makes that same boundary bind in the *other* direction, which was simply never encoded). No new product default, external evidence, or invented deterministic behavior was required.

---

## 31. Final outcome

**`SHARED_10K_LONGHORIZON_22_WEEK_NUMERIC_AUTHORITY_APPROVED`**

The approved rule (§21): clamp Preparation Runway's starting weekly volume and long run to Core's independently-computed Week-1 target whenever GE's exit would otherwise exceed it, before Runway's own existing interpolation runs. No new numeric constant. No GE, Core, or Runway structural change. Generic across all 21-52 week horizons and all supported day counts (§22).

---

## 32. Next implementation contract

The next phase should implement **only**:

1. The shared clamp (§21) inside `PreparationRunwayNumericMaterializer.Materialize`'s `ResolveStartingWeeklyVolume`/`ResolveStartingLongRun` resolution (or an equivalent single call site before the reachability check) — generic, not `if TotalWeeks == 22`.
2. Full 21-52 week dark re-verification for both 4D and 5D, at a *representative range of baselines* (not only the near-cap baseline FREQ.6D.14 was forced to use) — this is the real test of whether the clamp actually closes the gap generically, not merely in the four traced cases above.
3. Real PostgreSQL persisted adaptation completion (`LongHorizonRollingCheckpointRuntime`, FREQ.6D.15's own disclosed remaining item).
4. Real PostgreSQL persisted repair completion (same runtime).
5. Full real-Postgres dark closure re-confirmation.

Public activation remains explicitly out of scope until dark closure is complete.

---

## 33. No code

Confirmed: `git status`/`git diff` show zero production-code, test, migration, or catalog changes from this phase (the temporary evidence-gathering diagnostic was fully reverted before this report was written).

---

## 34. Governance

Ledger and roadmap updated below (§ committed separately). Since this phase's authority question closed (not left unresolved), the next capability is the implementation contract in §32 — not yet scheduled as a Phase ID.
