# PHASE 10K-GEN.21 — Beginner×3D Core Realignment (Phase K)

**Parent phases**: `GEN.5C` (`BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL`), `GEN.2B.3` (Intermediate×3D taper-minimum conflict decision — origin of the numeric mechanism reused unmodified by `GEN.5A.2`/`GEN.5C`), `GEN.6` (Beginner 5D/6D/7D closure, sibling phase, precedent for STOP discipline), `GEN.13`/`GEN.18` (precedent for real precedent-search and clean non-representability classification)
**Phase type**: EVIDENCE + CLASSIFICATION (no implementation)
**Execution status**: DONE
**Final classification**: `DOMAIN_DECISION_REQUIRED` — escalated, unresolved. `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` (`GEN.5C`) remains the current, unretracted authority pending that decision.

---

## 0. Precondition verification

`git log -5`, `git fetch`, `git diff HEAD origin/main` confirmed HEAD == `origin/main` == `1d2caca` before this phase began (0 ahead/0 behind). `PHASE_LEDGER.md`'s last row is Seq 123, `GEN.20`; no `GEN.21`/`GEN.22` row exists anywhere in the ledger or as a `PHASE_10K_GEN_21_*.md`/`PHASE_10K_GEN_22_*.md` file. Confirmed `GEN.21` (this phase) and `GEN.22` (Phase L) are the correct next-free IDs from repository truth, not assumed from the governing prompt's own guess.

This phase does not touch, reopen, or modify any 2D-related authority (`GEN.11`-`GEN.20`), Beginner×4D (`GEN.4E`, unaffected, confirmed by construction — no code touched), or Intermediate×3D (`GEN.2B`/`GEN.3A`/`GEN.3B`, `PUBLICLY_ACTIVE`, unaffected — no code touched).

## 1. Required reading (verified, not paraphrased from memory)

- `PHASE_10K_GEN_5C_BEGINNER_3D_CORE_FULL_CLOSURE.md` — read in full. Root cause per its §2/§3: GEN.5A.2's exact derivation shows pre-taper volume must reach ≈22.17-22.64 km to clear the 12.0 km taper floor at the retained 0.53 multiplier, but the evidence-grounded Beginner×3D peak band (16.0-20.0 km, ref 17.0) never reaches that pre-taper level at any horizon or readiness state — a mathematically unconditional non-representability, not three independently-failing tables.
- `PHASE_10K_GEN_6_BEGINNER_5D_6D_7D_COMBINED_...md` — read in full (§3 restates the same root-cause figures as comparative evidence; confirms Beginner's own separate, unrelated K=2-eligibility rejection for 5D/6D/7D — this phase does not touch that decision).
- `PHASE_10K_GEN_2B_3_3D_TAPER_MINIMUM_CONFLICT_DECISION.md` — read in full. This is the **origin document** of the 12.0 km figure: `MinimumViableFullLayoutWeeklyVolume = 4 (KEY) + 3 (EASY) + 5 (LONG) = 12 km`, explicitly derived for **Intermediate**×3D, and explicitly classified there as `NORMAL_TRAINING_PRESCRIPTION_MINIMUM`, not a taper-specific number: "No equivalent approved 3D taper-specific minimum exists for EASY or LONG… a complete taper-specific 3D triple cannot be inferred from repository evidence" (§2). `GEN.2B.3` itself named **Option A (taper-specific minima)** as `TAPER_SPECIFIC_MINIMA_ARCHITECTURALLY_COHERENT` but did not select it, purely because the EASY/LONG taper-specific evidence needed to complete it did not exist at that time (§4) — not because Option A was domain-wrong. It selected **Option D (eligibility/routing)** instead, applying the *normal-week* 12 km floor as a gate against the *taper-week* projected volume.
- `PHASE_10K_GEN_13_...DOMAIN_DECISION_REQUIRED.md` and `PHASE_10K_GEN_18_...md` — read in full, as the required precedent for STOP discipline and clean non-representability classification respectively (see §7/§8 below for how they are applied here).

## 2. Reconstructing the real conflict from code (not paraphrase)

Read directly, not assumed:

- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` lines 103-109:
  ```csharp
  if (request.Candidate.DaysPerWeek == 3)
  {
      var projectedTaper = weekly.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm;
      if (projectedTaper < 12d)
          throw new ThreeDayCoreProductIneligibleException(projectedTaper);
  }
  ```
  This gate is **unconditional on Level** — it fires for any `DaysPerWeek == 3` candidate, Beginner or Intermediate alike (confirmed by direct reading: no `Level` check anywhere in this block, unlike the `Level == "NEW" && DaysPerWeek == 4` and `Level == "INTERMEDIATE" && DaysPerWeek == 3` policy-*dispatch* branches earlier in the same method, which only select which `VolumeSafetyPolicy` instance to use for starting-volume/peak/taper resolution, not this eligibility check). This confirms the constraint is genuinely frequency-global, not an accidental Intermediate-only carry-over never generalized for Beginner — it is real, deliberate, shared authority.
- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1ThreeDaySessionVolumeAllocationPolicy.cs`:
  ```csharp
  public const double MinimumKeyKm = 4d;
  public const double MinimumEasyKm = 3d;
  public const double MinimumLongKm = 5d;
  ...
  if (volume < MinimumKeyKm + MinimumEasyKm + MinimumLongKm)
      throw new CatalogSessionPrescriptionInfeasibleException(...);
  ```
  This is the **live implementation** of `GEN.2B.1`'s "4/3/5" normal-week minima. Critically, `CatalogSessionPrescriptionPlanner.cs` line 29-32 dispatches to this policy for **every** week of a 3D candidate — there is no `IsTaperWeek` branch here, unlike 4D's own taper-specific `V1TaperSharpenPrescriptionPolicy` (a real, already-approved, already-used mechanism reserved for 4D's KEY role only during `TAPER`/`TAPER_SHARPEN`).
- By contrast, `backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs` uses `MinimumKeySessionDistanceKm = 3d` and `MinimumEasySupportDistanceKm = 1.5d` (via `FourDaySessionDistanceAllocationPolicy`), also applied every week — and `PHASE_10K_GEN_4C_BEGINNER_4D_PRODUCT_POLICY_DECISION_CLOSURE.md` confirms these same per-role minimums, applied to the *same* proportional-share allocation formula the taper week itself uses, are what makes Beginner×4D's 9.0 km full-layout floor and 9.0 km taper floor **identical by construction** (`GEN4C-INV-015`): "the taper uses the same unconditional long-run/session-allocation formula as every other week."

**This is the exact conflict, reconstructed from real code, not paraphrase**: 3D's per-role minima (4.0/3.0/5.0 = 12.0) were derived in `GEN.2B.1` explicitly for *normal, non-taper* quality-session viability (each role justified as `PRODUCT_DEFAULT`: KEY "enough total distance for warm-up + main set/recoveries + cool-down at low totals"; EASY "protects the sole easy exposure from becoming rounding residue"; LONG "frequency-concentration and feasibility envelope" — `PHASE_10K_GEN_2B_1_3D_TRAINING_LOAD_EVIDENCE_SYNTHESIS.md` §7 table, read directly), then applied **unmodified, every week including taper**, unlike 4D's taper-aware role floors (3.0/1.5/1.5/3.0 = 9.0, self-consistent with its own taper output). `GEN.2B.3` itself flagged the missing taper-specific triple as the reason it *couldn't* pick the architecturally-coherent fix (Option A) — not that Option A was wrong.

## 3. Testing the supplied hypothesis (confirmed, not assumed)

The governing prompt's hypothesis — that the real lever is a narrower mechanism (session-distance minimum / starting-volume semantics / rounding), not `PeakVolumeBand` or the taper multiplier — is **confirmed** by the above reconstruction:

- **PeakVolumeBand [16.0, 20.0] km, ref 17.0**: independently re-verified this phase against real external sources (Hal Higdon Novice 10K peak week fetched directly this phase — real weekly structure: Tuesday 3mi/4.83km, Thursday 2mi/3.22km, Sunday 5.5mi/8.85km long run, total ≈16.9km — matches `GEN.5A.2`'s cited 16.90km figure exactly). McMillan Level-1 Beginner's 16.1-32.2km range corroborates the low end. This band is real, externally corroborated, and — per the prompt's own supplied evidence, now independently checked — sits within the convergent range of real programs. **Not the lever.**
- **Taper multiplier 0.53 (~47% reduction)**: `GEN.2B.3` §3 classifies this `3D_TAPER_MULTIPLIER_RETAINED_AS_PRODUCT_DEFAULT_WITH_4D_CALIBRATION_PROVENANCE`, i.e. reused, not 3D-derived. It sits within the real-world 40-60% final-week reduction range this phase separately verified (PacePercentile's beginner 10K guide, fetched this phase: "reduced by roughly 50-60%"; a competing real source in the same guide: "40% to 50%"). **Not the lever.**
- **The session-distance minimum (12.0 km normal-week floor applied at the taper week)**: confirmed above to be the actual mechanism, and confirmed to be a **narrower, more mutable authority** than either of the above — `GEN.2B.1` itself labels all three component values `PRODUCT_DEFAULT`, and `GEN.2B.3` itself already identified (but did not pursue, for lack of evidence at the time) a taper-specific-minima fix that would not touch the band or the multiplier at all.

**Hypothesis confirmed.**

## 4. Constraint classification (every constraint involved)

| Constraint | Value | Classification | Reasoning |
|---|---|---|---|
| `PeakVolumeBand` (Beginner×3D) | [16.0, 20.0] km, ref 17.0 | `EVIDENCE_SUPPORTED_PRODUCT_RULE` | Independently re-verified this phase against Higdon (fetched directly) and McMillan; convergent with the governing prompt's supplied external evidence |
| `TaperVolumeMultiplier` | 0.53 | `PRODUCT_DEFAULT` (evidence-informed) | `GEN.2B.3`-classified 4D-golden-fixture-calibrated default; independently confirmed this phase to sit within the real 40-60% reduction range used by genuine beginner 3-day programs |
| KEY normal-week minimum | 4.0 km | `PRODUCT_DEFAULT` | `GEN.2B.1` §7: reasoned ("enough distance for warm-up+main set+cool-down"), not evidence-measured; explicitly not derived for the taper context |
| EASY normal-week minimum | 3.0 km | `PRODUCT_DEFAULT` | `GEN.2B.1` §7: reasoned ("protects sole easy exposure from rounding residue"); explicitly not derived for the taper context |
| LONG normal-week minimum | 5.0 km | `PRODUCT_DEFAULT` | `GEN.2B.1` §7: reasoned ("frequency-concentration/feasibility envelope"); explicitly not derived for the taper context |
| **Application of the 12.0 km normal-week floor to the taper week specifically** (the actual gate at `CatalogVolumeAndLongRunPlanner.cs:103-109` and the unconditional-every-week enforcement in `V1ThreeDaySessionVolumeAllocationPolicy.cs`) | 12.0 km taper gate | `IMPLEMENTATION_ARTIFACT` (as applied to the taper week) | `GEN.2B.3` §4/§7 itself: the correct fix (taper-specific minima, Option A) was architecturally coherent but not implemented purely because a `CATALOG_CONTENT_LIMIT` blocked it (no evidence-grounded taper-specific EASY/LONG minima existed); Option D (reusing the normal-week floor as a blanket eligibility gate) was an explicitly-disclosed fallback, not an independent domain requirement that the taper week itself carry full normal-session distances |
| Beginner×3D Core identity-allow-list rejection | `(Beginner,3)` absent from `V1CatalogPilotIdentityPolicy` | `IMPLEMENTATION_ARTIFACT`/mechanism, correctly scoped | Confirmed by `GEN.5C` §3 as the correct, sufficient, already-implemented non-support mechanism *given* the numeric conflict — not itself a separate authority to reclassify |
| TAPER_SHARPEN's 3.0 km KEY floor (4D) | 3.0 km | Existing `EVIDENCE_SUPPORTED_PRODUCT_RULE`/catalog authority (unaffected, not reopened) | Already-approved, cross-frequency-reusable, real catalog transform (`V1TaperSharpenPrescriptionPolicy`) — cited here only as comparative evidence, not modified |

No `HARD_DOMAIN_SAFETY_RULE` or `CATALOG_CONTENT_LIMIT` (in the sense of missing catalog *content*, as opposed to missing *evidence*) governs this conflict directly — the true original blocker (`GEN.2B.3`'s own words) was an evidence gap, now partially addressed by this phase's own external verification (§5), not a structural catalog limitation.

## 5. Real evidence gathered this phase for a taper-specific 3D minimum (independently verified, not accepted from the prompt)

Two real sources were fetched/searched directly this phase (not accepted at face value from the governing prompt, which did not supply these specific citations):

1. **Hal Higdon Novice 10K, real race week (Week 8), fetched directly**: Tuesday 3mi (4.83km), Thursday 2mi (3.22km), Saturday rest, Sunday = the 10K race itself (replacing the long run entirely). Compared to Week 7 (peak): identical Tuesday/Thursday distances — Higdon's real program does **not** shrink the shorter/medium run distances during taper at all; the entire volume reduction comes from eliminating the long run.
2. **PacePercentile's beginner 10K guide** (via search, cross-checked against the prompt's own citation of this source): describes a genuine beginner 3-day final race week as "a 20-minute easy run, a short session with strides, then rest before race day" — i.e., a real beginner program's final week can be **a single ~2.5-3.0km easy run with no long run and no second quality session at all**.

These two real sources **materially disagree on shape** (Higdon keeps short/medium runs near-full-distance and drops the long run to the race; PacePercentile shrinks to a single short run and drops both the second run and the long run). This is disclosed honestly, not synthesized into a single number by picking whichever suits the desired outcome.

## 6. Why this is not resolved as a mechanical reclassification

A defensible new 3D-taper-specific floor could mechanically reuse TAPER_SHARPEN's already-approved 3.0 km KEY floor verbatim (zero new number — direct cross-frequency reuse, matching this engagement's own established `EXISTING_SHARED_POLICY_REUSED_DUE_TO_NO_LEVEL_EFFECT` pattern). But no equally mechanical, already-approved value exists for what a 3D-specific *taper* EASY or LONG minimum should be:

- 4D's own EASY (1.5 km)/LONG (3.0 km) taper-context minima are calibrated for a **2-EASY** structure and were justified in `GEN.2B.1` itself as `INDIRECT_CONTEXT_ONLY` evidence for the *normal-week* 3D minima precisely because 3D's single-EASY structure is materially different (needing, per `GEN.2B.1`'s own stated reasoning, protection "from becoming rounding residue" that a 2-EASY structure does not need as acutely).
- The two real sources gathered this phase (§5) point to genuinely different taper shapes (partial-reduction-plus-race-substitution vs. near-total-reduction-to-one-run), and neither maps directly onto Appsel's fixed KEY/EASY/LONG three-role structure.

Picking an exact new EASY/LONG taper-specific number between these real but divergent sources is a genuine coaching/methodology judgment — precisely the category `GEN.13` (this engagement's own explicit precedent, read in full for this reason) declined to resolve unilaterally when a real methodology question with distinct, materially different real options existed and no existing repository mechanism directly settled it.

## 7. Options (presented, not resolved)

| Option | Description | What changes | Evidence basis | Risk |
|---|---|---|---|---|
| **1 — Author a new 3D taper-specific session-distance minimum triple** | Reuse TAPER_SHARPEN's approved 3.0 km KEY floor verbatim; author new EASY/LONG taper minima (candidates in the 2.0-3.0 km range per §5's two real sources) | New `VolumeSafetyPolicy`-adjacent numeric authority + a taper-specific dispatch branch in `V1ThreeDaySessionVolumeAllocationPolicy`/`CatalogVolumeAndLongRunPlanner`, closing GEN.2B.3's own long-disclosed Option A gap | Real, independently-verified this phase (§5), but the two sources materially disagree on shape — any single number is a judgment call between them, not a mechanical derivation | Opens Beginner×3D Core at some/most/all horizons (exact horizon range depends on which EASY/LONG value is chosen — not computed here since no value is chosen); is a genuine new safety-adjacent numeric authority requiring the same standing `GEN.4C` had for its own 4D floor derivation |
| **2 — Reaffirm non-support, re-grounded** | Keep `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` as final, but supersede its stated *reasoning* — no longer "mathematically unreachable with no identified lever" (which this phase disproves), but "a real, narrower lever exists (taper-specific session minima) but requires a coaching-judgment decision this phase has no standing to make unilaterally, and none has been made" | No code change; the historical report is not deleted or rewritten (per this engagement's rule) but this new phase's own report supersedes its *characterization* of finality-by-impossibility with finality-by-undecided-lever | This phase's own §2-§6 | Zero implementation risk; leaves Beginner×3D Core closed exactly as it is today, honestly re-grounded |
| **3 — Retune `PeakVolumeBand` or taper multiplier instead** | Raise the band ceiling or reduce the taper cut | Reopens two `EVIDENCE_SUPPORTED_PRODUCT_RULE`/well-corroborated values | Contradicted by this phase's own independent evidence check (§3) — explicitly **not recommended**, listed only for completeness since the governing prompt named it as the default worst-case lever | Would weaken two externally well-supported values to work around a narrower, more mutable one — the exact anti-pattern the governing prompt itself warned against |

## 8. Final classification

```
DOMAIN_DECISION_REQUIRED
```

This phase performed the full reconstruction, hypothesis test, classification, and evidence-gathering the governing prompt required, and confirms the real lever is the session-distance-minimum mechanism (§2-§4), not the peak band or taper multiplier (§3) — but declines to invent an exact new EASY/LONG taper-specific kilometre value unilaterally (§6), consistent with this engagement's own `GEN.13` precedent for exactly this shape of situation (a real methodology question with distinct, evidence-backed but materially different real options, and no existing mechanical authority that settles it).

`BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` (`GEN.5C`) is **not retracted** and remains the current, standing classification — this phase supersedes only the *characterization* of why (a real, narrower, more mutable lever now known to exist, not a mathematical impossibility), per §7 Option 2, pending a real product decision on Option 1 vs. Option 2.

No production code, tests, catalog authoring, or migration performed. `GEN.5C`'s report text is unmodified (per instruction, never deleted or rewritten). `GEN.6`'s Beginner 5D/6D/7D closure is untouched.

## 9. Old-vs-new supersession table

| Item | Old authority | Old provenance | Old conflict framing | Mutable/immutable | New authority (this phase) | New provenance | Safety impact |
|---|---|---|---|---|---|---|---|
| PeakVolumeBand | [16.0,20.0]km, ref 17.0 | `GEN.5A.2` | Ceiling too low to ever clear the 12.0km taper floor | Immutable — not reopened | Unchanged; re-confirmed `EVIDENCE_SUPPORTED_PRODUCT_RULE` via independent this-phase source verification | `GEN.21` (re-verification only) | None — unchanged |
| Taper multiplier | 0.53 | `GEN.2B.3`, reused from 4D | Contributes to the 22.17km pre-taper requirement | Immutable — not reopened | Unchanged; re-confirmed within real-world 40-60% range | `GEN.21` (re-verification only) | None — unchanged |
| Session-distance minima (4/3/5=12), applied at taper | `GEN.2B.1`/`GEN.2B.3` | `PRODUCT_DEFAULT`, normal-week-derived, applied unmodified at taper | Framed as the immovable numeric floor causing "mathematically unreachable" | **Mutable** — confirmed by this phase | Not yet changed — `DOMAIN_DECISION_REQUIRED` (Option 1 vs Option 2, §7) | `GEN.21` | Deferred to the pending decision |
| Beginner×3D Core support status | `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` | `GEN.5C` | "Mathematically unreachable at every horizon/readiness state" | N/A — status, not a numeric constraint | **Unchanged status**, re-grounded reasoning: a real lever exists but is undecided | `GEN.21` | None — no support change made |

## 10. Recurring-defect-family search (reported per instruction, even though it found nothing new)

Searched every path Beginner×3D Core would exercise, following `GEN.20`'s own established practice:

- **Readiness/starting-volume**: `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` dispatches on `ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)` (line 184) — a Beginner×3D candidate (never Level-gated into this branch, since only `Level=="INTERMEDIATE"` triggers the `ThreeDayIntermediate` policy dispatch at line 53) would fall through using `VolumeSafetyPolicy.Default`'s own starting-volume resolution, not any Intermediate-specific hardcode. No Level-blind Intermediate-only hardcode found gating Beginner×3D's starting volume differently than intended.
- **Taper/session-minimum**: the 12.0km gate (`CatalogVolumeAndLongRunPlanner.cs:103-109`) and `V1ThreeDaySessionVolumeAllocationPolicy`'s 4/3/5 minima are both genuinely **Level-blind** (§2) — confirmed by direct reading, not a Beginner-specific gap; this is the real, already-identified, already-classified mechanism (§2-§4), not a new recurring-defect-family hardcode.
- **Long-run cap**: `V1ThreeDaySessionVolumeAllocationPolicy`'s `LongHardCap = 0.42d` is likewise Level-blind, applied identically regardless of Level.
- **Calendar spacing**: `DatedGeneratedCatalogPlanSkeletonValidator`'s `MinimumKeySessionToLongRunSeparationDays`/`MinimumKeySessionToKeySessionSeparationDays = 2` constants (confirmed via `GEN.6` §6's own prior direct reading, re-confirmed unchanged since) are frequency-structural, no `Level` parameter.
- **Adaptation**: `NextWindowLoadDecisionPolicy.DetermineLoadDecision` dispatches purely on `WindowExecutionSummary.ExpectedSessionCount` (confirmed by direct reading of `NextWindowLoadDecisionPolicy.cs`) — a Beginner×3D candidate (3 sessions/week) would exercise the same 3-session-count branch Intermediate×3D already exercises live today. No Level-specific Adaptation hardcode exists for 3-session weeks.

**No new instance of the recurring hardcode-assumption defect family (`GEN.10`/`GEN.12`/`GEN.17`/`GEN.19`/`GEN.20`'s own pattern) was found for Beginner×3D.** The identified conflict (§2-§4) is a real, already-known, already-classified numeric-authority gap, not a fresh hardcode defect.

## 11. Governance and closure

No production code, test, or catalog change (evidence/classification/decision phase only). Beginner×3D Core, Beginner×4D, Beginner 5D/6D/7D (`GEN.6`), Intermediate 10K axis, Advanced 10K axis, and every 2D cell are all confirmed unaffected by construction (zero code touched).

**`GEN21_BEGINNER_3D_CORE_REALIGNMENT_DOMAIN_DECISION_REQUIRED`.** Real options presented in §7 for a human product decision. Phase L (`GEN.22`, Beginner×5D) proceeds next per the governing prompt's own explicit ordering — it is an independent question, not blocked by this phase's outcome.
