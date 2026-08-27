# PHASE 10K-GEN.6 — Beginner 5D+6D+7D Combined Support, Numeric, Workout-Eligibility & Horizon Representability Authority Closure

**Parent phases**: `GEN.4C.4` (Beginner×4D numeric closure), `GEN.5C` (Beginner×3D non-support closure), `FREQ.6D.23` (Intermediate 6D/7D authority), `FREQ.6D.27` (Intermediate frequency-axis closure)
**Phase type**: EVIDENCE + PRODUCT DECISION + NUMERIC AUTHORITY + REPRESENTABILITY
**Execution status**: DONE
**Final classification**: `BEGINNER_5D_6D_7D_PRODUCT_NON_SUPPORT_APPROVED`

---

## 0. Precondition verification

`PHASE_LEDGER.md` row 107 / `MASTER_ROADMAP.md` confirm `FREQ.6D.27` DONE, `INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE`: Intermediate 3D/4D/5D/6D `PUBLICLY_ACTIVE`, 7D `PRODUCT_NON_SUPPORT`. Verified from repository truth, not chat memory.

Beginner current repository state (verified, not assumed):
- **Beginner×3D**: final classification is **`BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL`** (`GEN.5C`) — not the phrase `PROVEN_NON_REPRESENTABLE_UNDER_APPROVED_V1_CORE_POLICY` speculated by this phase's own prompt. Scoped explicitly to **Core 8-14 weeks only**; Runway/LongHorizon were never evaluated for Beginner×3D.
- **Beginner×4D**: `PUBLICLY_ACTIVE` (`GEN.4E`), Core only (8-14 weeks; Beginner×4D Runway/LongHorizon do not exist).
- **Beginner×5D/6D/7D**: no policy class, catalog artifact, PeakVolumeBand row, or identity-allow-list entry exists anywhere in the codebase. Genuinely open, as expected.

**Next free phase ID**: searched `PHASE_LEDGER.md`/`PHASE_10K_GEN_*.md` — the Beginner-authority family (`GEN.4`=×4D, `GEN.5`=×3D) last used `GEN.5C`/`GEN.CHECKPOINT.1`; `GEN.6` is unused and unique. Scheduled as `GEN.6` in this same commit (evidence-only phase, no separate scheduling commit).

---

## 1. Canonical Beginner definition (repository authority)

`PHASE_10K_GEN_4A_LEVEL_AUTHORITY_DECISION_RESOLUTION.md` does not define Level as a scored/composite quantity. Beginner is a **categorical identity axis value** (`RunningBackground.Beginner`, successor to the legacy `NewToRunning`), one of `Distance × Level × Frequency`. No training-age/pace/race-experience formula exists — Level is a product-tier label whose concrete meaning is expressed entirely through its approved numeric/workout authority (lower starting volume, lower peak band, restricted workout timing), not an independent definition. **`APPSEL_BEGINNER_TIER_DEFINITION` = a lower-load-tolerance product tier, defined operationally by its approved authority, not by an external training-science taxonomy.**

`GEN.4A`'s controlling invariant (its own §10, restated as `GEN4A-INV-001`): **`QUALITY_SESSION_COUNT_IS_LEVEL_ELIGIBILITY_OR_CAP_ONLY`** — structural KEY/EASY/LONG cardinality is exclusively `RunLayout`-owned; Level may gate whether a given RunLayout-defined structural KEY count `K` is *eligible* for that Level, but may never redefine `K`. This is the exact mechanism this phase's central decision (§4 below) invokes.

## 2. Existing Beginner×4D authority table (verified from repository, not chat memory)

| Item | Value | Provenance |
|---|---|---|
| Missing-readiness starting volume | 12.0 km | `GEN.4C` §6; `V1BeginnerFourDayMissingReadinessStartingVolumePolicy.MissingWeeklyVolumeDefaultKm` |
| Explicit-zero starting volume | 9.5 km | `GEN.4C` §7; same class, `ExplicitZeroWeeklyVolumeDefaultKm` |
| PeakVolumeBand | [18.0, 24.0] km | `GEN.4C` §11; `PEAK_VOLUME_BANDS_V1.v5` `TEN_K/NEW/4` row |
| ResolvedPeakReference | 21.0 km | `GEN.4C.3` — midpoint of the approved band, explicitly labeled `ProductDefaultWithEvidenceEnvelope`, **not** derived from Intermediate's arithmetic (a 22.0 km figure derived that way was found improper in `GEN.4C.2` and rejected) |
| Progression preferred/hard/absolute cap | 0.07 / 0.08 / 2.5 km | Reused Intermediate values; **not actually growth-drivers for the 4D path** — the real planner (`CatalogVolumeAndLongRunPlanner`) uses fixed-target linear interpolation toward `GoldenFixtureResolvedPeakKm`/`GoldenFixtureStartingVolumeKm`/`GoldenFixtureNonTaperTransitions`, confirmed by `GEN.4C.2`'s "algorithm revalidation" |
| Taper factor | 0.53 | Reused, unchanged |
| Long-run share (preferred/hard) | 0.33 / 0.40 | `GEN.4C` §19 / `GEN.4B` §12 |
| General/taper weekly floor | 9.0 km | `GEN.4C` §20-21; `V1BeginnerFourDayVolumeEligibilityPolicy.MinimumFullLayoutWeeklyVolumeKm` |
| Break-even (pre-taper) threshold | 17.0 km | `GEN.4C.1` §6; `TaperBreakEvenPreTaperKm` |
| Workout eligibility | `FARTLEK`/`THRESHOLD_TEMPO` deferred `SUPPORTED_ONLY_AFTER_FOUNDATION` for candidate `TEN_K__4D__BEGINNER` (`V1BeginnerWorkoutEligibilityPolicy.IsDeferred`) — but this is **functionally redundant** with the pre-existing, Level-blind rule that `FARTLEK`/`THRESHOLD`'s own `eligiblePhases` already exclude `FOUNDATION` for every Level. Beyond Foundation, Beginner's single KEY slot uses the same `FARTLEK`/`THRESHOLD_TEMPO` content as any other Level | `GEN.4B` §14-15, `GEN.4C` §15 |
| Core representability | Missing: `ELIGIBLE` 8-14wk. Explicit-zero: `PRODUCT_INELIGIBLE` 8-12wk, `ELIGIBLE` 13-14wk | `GEN.4C.3` §15-16 |
| Adaptation | Uses the existing 4-session state table unmodified — no Beginner-specific Adaptation policy exists | Confirmed by absence of any Beginner-specific class in `NextWindowLoadDecisionPolicy` |
| Structural composition | **1 KEY + 2 EASY + 1 LONG** (`RUN_LAYOUT_4D`) — Beginner×4D has exactly **ONE** structural KEY slot | `GEN.4B` §2 |

## 3. Beginner×3D — frozen, used only as comparative evidence

Exact classification: `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL`. Root cause (`GEN.5A.2`): a **numeric/structural conflict**, not policy error — the 3D structural taper floor (12.0 km, from 4.0 KEY + 3.0 EASY + 5.0 LONG minima) requires pre-taper volume ≥22.17 km (since `Round0.5(X × 0.53) ≥ 12.0`), but the evidence-grounded Beginner×3D peak band (16-20 km, sourced from Hal Higdon's Novice 10K and McMillan's Beginner Level-1 programs) has a ceiling (20.0 km) below that requirement — mathematically unreachable at every horizon and every readiness state. **Scoped explicitly to Core 8-14 weeks only** — Runway/LongHorizon were never evaluated and are not reopened or assumed by this phase.

Comparative value for this phase: confirms this repository's established practice of grounding Beginner peak bands in real novice-tier sources (Higdon/McMillan), and that a Beginner structural/numeric conflict can independently and completely block a frequency regardless of readiness state — the same shape of reasoning this phase applies to 5D/6D (§4) via a different, workout-eligibility-based mechanism rather than a numeric floor conflict.

## 4. The central decision: can Beginner Core legitimately carry two structural KEY_SESSION slots?

This is the phase's decisive question (per its own §15/§16), because `RUN_LAYOUT_5D` (2 KEY + 2 EASY + 1 LONG) and `RUN_LAYOUT_6D` (2 KEY + 3 EASY + 1 LONG) are both **frozen at K=2** — confirmed live in `plan-catalog/catalog/layouts/run-layout-5d.v1.json` / `run-layout-6d.v1.json`. Per `GEN.4A`'s own invariant, Level cannot redefine K; it can only accept or reject eligibility for a RunLayout that requires a given K.

**Decision: NO.** Beginner-level eligibility rejects any RunLayout whose Core structure requires two structural KEY_SESSION slots per week. This is Option B named in this phase's own prompt (§15), and is grounded in:

1. **Real, already-accepted repository evidence of markedly elevated novice injury risk**: Kluitenberg et al. (novice runner injury incidence 17.8/1000h [95% CI 16.7-19.1] vs. recreational 7.7/1000h [95% CI 6.9-8.7]) — over twice the baseline injury rate, already classified `SUPPORTED` Tier 2 evidence in `GEN.4B`.
2. **No counter-evidence anywhere** — neither in this repository's own prior Beginner evidence work nor found externally — that any genuinely novice-tier training program prescribes two simultaneous quality/structured-intensity sessions per week. The two real external sources this repository itself already treats as tier-matched Beginner authority (Hal Higdon's Novice 10K program, McMillan's Beginner Level-1 10K program — both cited and accepted in `GEN.5A`/`GEN.5A.2` for the Beginner×3D peak band) are structurally single-quality-session-or-none programs: volume is built through easy running and one weekly long run, with at most one harder-effort session, never two independent KEY-style efforts per week. This is a well-established, near-universal convention across genuinely novice-oriented programs, distinguishing them structurally from intermediate/advanced programs (which routinely carry 2+ quality sessions).
3. **`GEN.4A`'s own existing deterministic mechanism** (`QUALITY_SESSION_COUNT_IS_LEVEL_ELIGIBILITY_OR_CAP_ONLY`) is the exact, already-approved cross-axis authority designed for precisely this kind of decision — no new mechanism, no new structural role, no RunLayout mutation.

This satisfies the phase's own decision standard (§63) as `STRONG_DOMAIN_EVIDENCE` + `EXISTING_DETERMINISTIC_CROSS_AXIS_AUTHORITY`, not an invented value.

**This decision is symmetric across 5D and 6D** — both RunLayouts have identical K=2 (the only structural difference between them is EASY-session count: 2 vs. 3), so there is no basis for accepting one and rejecting the other on this ground; both are equally blocked.

## 5. Cascading consequence to Runway and LongHorizon (verified from real architecture, not assumed)

Per `FREQ.6D.11` §25/§30 (quoted verbatim, confirmed unchanged by any later phase): **LongHorizon = GE (variable weeks) + Preparation Runway (FIXED 8 weeks) + Core (FIXED 12 weeks)**, and Preparation Runway's own standalone 15-20 week product likewise hands off into the same real Core execution at its end (`FREQ.6D.11` §9/§25: "both Runway's Core-handoff and LongHorizon's Core-entry already call" the same `TenKPreparationRunwayDarkOrchestratorFactory`). Runway and GE structures themselves only ever require **one** structural KEY (`FREQ.6D.23`'s own structural table: 6D Runway = 1K+4E+1L, 6D GE = 1K+4E+1L — only Core carries 2 KEY), so the two-KEY blocker does not apply to Runway/GE directly.

However, because both products **literally culminate in a real execution of the frequency's own Core RunLayout**, a Core-level rejection is not separable from Runway/LongHorizon for the same frequency — neither product can complete without passing through the same 2-KEY Core segment this phase rejects for Beginner. **Therefore Beginner×5D and Beginner×6D are `PRODUCT_NON_SUPPORT` across all three horizon bands (Core, Runway, LongHorizon)**, not merely Core.

## 6. Beginner×7D

`FREQ.6D.23`'s two reasons for Intermediate×7D `PRODUCT_NON_SUPPORT` were audited for scope:

1. **Injury-risk evidence** — cited as general running-frequency/injury findings (systematic-review association between zero-rest-day frequency and higher injury incidence, plus consensus guidance recommending ≥1 rest day/week for "this population"), not Intermediate-exclusive science. Beginner has **even stronger** applicable injury evidence in this repository's own record (Kluitenberg's >2× novice-vs-recreational injury rate), reinforcing rather than weakening the conclusion for Beginner.
2. **Calendar week-boundary conflict** — confirmed **frequency-global, not Intermediate-specific**. Exact quote (`FREQ.6D.23` §3): the canonical spacing rule (`DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays = MinimumKeySessionToKeySessionSeparationDays = 2`) is a generic calendar-composer constant with no Level parameter anywhere in its definition or invocation; the conflict arises purely because a 7-day cadence has **zero non-running days** to absorb the cross-week boundary between one week's final LONG_RUN and the next week's first KEY_SESSION — "existing 4D/5D/6D all carry at least one non-running day that absorbs this cross-week boundary." This is a structural property of the frequency itself, independent of which Level uses it.

**Conclusion: Beginner×7D inherits the same frequency-global calendar architecture gap.** No new evidence work, no PeakVolumeBand, no numeric authority was derived for 7D (per this phase's own §19/§52 efficiency instruction) — the architecture blocker alone is sufficient and is reinforced, not contradicted, by injury evidence. **Beginner×7D is `PRODUCT_NON_SUPPORT` across Core, Runway, and LongHorizon.**

## 7. Numeric/workout/Adaptation authority for 5D/6D/7D

Per this phase's own sequencing rule (§20/§52: support viability before numeric authority, and do not derive numeric policy for a frequency already `PRODUCT_NON_SUPPORT`), **no numeric authority table, PeakVolumeBand, ResolvedPeakReference, progression, long-run share, workout-profile-capacity matrix, or Adaptation table was derived for Beginner×5D, ×6D, or ×7D** — all are `PRODUCT_NON_SUPPORT` before any such derivation would be meaningful. Per this phase's own §55 pattern, generalized: all numeric cells for 5D/6D/7D are marked **`NOT_APPLICABLE_DUE_TO_PRODUCT_NON_SUPPORT`**.

## 8. Support matrix (`BEGINNER_10K_REMAINING_FREQUENCY_SUPPORT_MATRIX`)

| Cell | StructuralRepresentability | FinalSupportStatus | Reason |
|---|---|---|---|
| 5D Core | 2-KEY RunLayout frozen, technically representable | `PRODUCT_NON_SUPPORT` | Beginner-level eligibility rejects K=2 (§4) |
| 5D Runway | 1-KEY, technically representable | `PRODUCT_NON_SUPPORT` | Cascades from Core rejection (§5) |
| 5D LongHorizon | GE(1K)+Runway(1K)+Core(2K) | `PRODUCT_NON_SUPPORT` | Cascades from Core rejection (§5) |
| 6D Core | 2-KEY RunLayout frozen, technically representable | `PRODUCT_NON_SUPPORT` | Same as 5D (§4) |
| 6D Runway | 1-KEY, technically representable | `PRODUCT_NON_SUPPORT` | Cascades (§5) |
| 6D LongHorizon | GE(1K)+Runway(1K)+Core(2K) | `PRODUCT_NON_SUPPORT` | Cascades (§5) |
| 7D Core | Frequency-global calendar gap | `PRODUCT_NON_SUPPORT` | §6 |
| 7D Runway | Frequency-global calendar gap | `PRODUCT_NON_SUPPORT` | §6 |
| 7D LongHorizon | Frequency-global calendar gap | `PRODUCT_NON_SUPPORT` | §6 |

## 9. Cross-frequency Beginner comparison

| Frequency | Support | Public | Reason |
|---|---|---|---|
| 3D | `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` | No | Numeric/structural taper-floor conflict, Core-scoped only (`GEN.5C`) |
| 4D | `SUPPORTED` | Yes (`GEN.4E`) | Canonical Beginner reference/control |
| 5D | `PRODUCT_NON_SUPPORT` (this phase) | No | Two-KEY Core eligibility rejection |
| 6D | `PRODUCT_NON_SUPPORT` (this phase) | No | Same as 5D |
| 7D | `PRODUCT_NON_SUPPORT` (this phase) | No | Inherited frequency-global calendar gap + injury evidence |

**Beginner support remains 4D-only** — an explicitly acceptable outcome per this phase's own §61/§67 ("valid if evidence supports it... close without unnecessary implementation").

## 10. Architecture/hardcode audit (reconnaissance only, no code changed)

Confirmed via direct code reading: `CatalogVolumeAndLongRunPlanner.cs` gates Beginner-specific policy narrowly (`Level == "NEW" && DaysPerWeek == 4`, never a broad `DaysPerWeek >= 5` or `Level != Beginner` condition — the code's own comment explicitly anticipates this: "so a future Beginner x5D/Advanced x5D candidate can never silently inherit this"). `V1CatalogPilotIdentityPolicy`'s allow-list contains no `(Beginner, N>4)` entries; its `ArgumentOutOfRangeException` message is explicit: "Only the activated Intermediate 3D/4D/5D/6D and Beginner 4D Core pilot identities are resolvable." **No Beginner==4D-only silent-assumption hardcode found** — every Beginner-specific gate is narrowly and explicitly scoped, meaning no architecture debt blocks a future Beginner frequency were one ever approved. Classification: **`NO_GAP`** (a correctly-scoped closed system, not an accidental omission).

## 11. Governance and closure

No production code, tests, catalog authoring, or migration performed (evidence/decision phase only, per this phase's own §68). Intermediate frequency axis, Intermediate×7D `PRODUCT_NON_SUPPORT`, Beginner×3D non-support, and Beginner×4D public status are all preserved unchanged.

**`BEGINNER_FREQUENCY_AUTHORITY_COMPLETE`** (not `BEGINNER_PUBLIC_CAPABILITY_COMPLETE`, since no new frequency was approved for implementation — there is nothing further to implement or activate for 5D/6D/7D; `BEGINNER_PUBLIC_CAPABILITY_COMPLETE` was already achieved at 4D by `GEN.4E` and remains unchanged).

Per this phase's own §71, the next major Level axis is expected to be Advanced (3D-6D/7D authority) per `MASTER_ROADMAP.md`'s own roadmap — not begun here. **`NEXT_PHASE_NOT_YET_SCHEDULED`.**
