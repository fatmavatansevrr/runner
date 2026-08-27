# PHASE 10K-GEN.7 — Advanced 3D+4D+5D+6D Combined Level, Product, Numeric, Workout-Eligibility & Horizon Authority Closure

**Parent phases**: `GEN.4A` (Level authority), `GEN.6` (Beginner remaining-frequency closure), `FREQ.6D.23`/`FREQ.6D.25`/`FREQ.6D.27` (Intermediate 6D/7D + PeakVolumeBand authority)
**Phase type**: EVIDENCE + PRODUCT DECISION + NUMERIC AUTHORITY + REPRESENTABILITY
**Execution status**: DONE
**Final classification**: `ADVANCED_3D_4D_AUTHORITY_APPROVED_IMPLEMENTATION_READY_5D_6D_AUTHORITY_APPROVED_WITH_PRESCRIPTION_CONTENT_BLOCKER_7D_PRODUCT_NON_SUPPORT`

---

## 0. Precondition verification

`PHASE_LEDGER.md` row 108 confirms `GEN.6` DONE, `BEGINNER_5D_6D_7D_PRODUCT_NON_SUPPORT_APPROVED` / `BEGINNER_FREQUENCY_AUTHORITY_COMPLETE`. Row 107 confirms `FREQ.6D.27` DONE, `INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE`. `MASTER_ROADMAP.md`'s Wave A checklist names Advanced as the next open item. Next free phase ID searched and confirmed: the Level-authority family (`GEN.4`=Beginner×4D, `GEN.5`=Beginner×3D, `GEN.6`=Beginner remaining) has last used `GEN.6`; `GEN.7` is unused. Scheduled as `GEN.7` in this same commit (evidence-only phase).

## 1. Advanced canonical identity and Advanced-vs-Experienced mapping

`RunningBackground` enum (`backend/RunningApp.Domain/Enums/RunningBackground.cs:28-35`) has **four** members: `Beginner, Intermediate, Advanced, Experienced`. `GEN.4A` §4 is explicit and controlling: **`EXPERIENCED_OUTSIDE_CURRENT_V1_BUT_PRESERVED`** — `Advanced` and `Experienced` are two distinct canonical Levels; V1's generalization target is exactly `{Beginner, Intermediate, Advanced}`; `Experienced` is not merged into `Advanced`, not activated, not removed — it remains a known-but-unsupported enum value pending a future, separate phase. **This phase does not touch `Experienced` in any way**, consistent with `GEN.4A`'s controlling scope and this phase's own instructions.

`APPSEL_ADVANCED_TIER_DEFINITION`: like Beginner, Advanced is a categorical product-tier identity, not a scored composite. Its concrete meaning is expressed through its approved authority (higher peak-volume envelope, full/immediate access to the existing quality-workout catalog with no Foundation-phase deferral, and — per this phase's decision — a positive-training-evidence eligibility requirement) rather than an external training-science taxonomy.

## 2. Historical Advanced/Experienced authority inventory

`plan-catalog/catalog/policies/peak-volume-bands.v1.json`/`.v2.json` (superseded, but real, once-`VALIDATED`-status artifacts) contain:

| Experience | 3/week | 4/week | 5/week |
|---|---|---|---|
| ADVANCED | [34,46] | [38,52] | [42,58] |
| EXPERIENCED | [40,55] | [46,62] | [50,68] |

Removed entirely in v3 (`FREQ.6D.24`'s own finding, quoted): *"v3 stripped every Advanced/Experienced/unused-Beginner row entirely (those levels/frequencies are not implemented)... a canonical row must be deliberately authored and versioned in; it is never inherited or optional."* — i.e. removal reflected non-implementation, not incorrectness. `FREQ.6D.24` additionally found these Advanced/Experienced rows followed a **constant per-day-km delta** (Advanced: +4km min/+6km max per day, exactly linear 3D→4D→5D) that was never carried forward for Intermediate's own (hand-recalibrated) band — a caution against using this shape to *extrapolate new* cells, not a finding that the existing cells are wrong.

**Classification**: the 3 real ADVANCED rows (3D/4D/5D) are **`HISTORICAL_PRODUCT_EVIDENCE`** — once-shipped, `VALIDATED`-status product values for exactly the cells this phase needs, usable as a genuine evidentiary anchor (not a draft, not unsafe, not unknown-provenance) but requiring corroboration before being restored as *current* authority, consistent with this repository's own established practice (`FREQ.6D.25` already legitimately reused this exact Advanced×5D `58km` ceiling as one of its two real cross-tier evidence points when approving Intermediate×6D's band). `EXPERIENCED` rows are `HISTORICAL_PRODUCT_EVIDENCE` for a Level not in this phase's scope — recorded but not used.

## 3. The central structural decision: does Advanced support two structural KEY_SESSION slots?

Unlike `GEN.6`'s Beginner decision, the answer here is **YES** — Advanced Core may legitimately use `RUN_LAYOUT_5D`/`RUN_LAYOUT_6D` (frozen at K=2), grounded in:

1. **Real Appsel-internal precedent**: the v1/v2 `PEAK_VOLUME_BANDS_V1` artifact's Advanced×5D row (`[42,58]`) was itself designed for a 5-day/2-KEY product — Appsel's own historical product design already envisioned Advanced runners on a dual-KEY layout, at `VALIDATED` status, before Advanced was deprioritized (not rejected) for V1.
2. **No injury-risk or coaching-convention concern applies** — the evidence that blocked Beginner's two-KEY eligibility (`GEN.6` §4: markedly elevated novice injury risk, universal single-quality-session novice-program convention) is specific to novice/beginner populations; no repository or general-knowledge evidence suggests elevated risk or inappropriate load for advanced/experienced runners handling two quality sessions per week — this is, in fact, the standard, well-tolerated shape of advanced-tier distance-running programs.

This is symmetric across 5D and 6D (both K=2, identical KEY cardinality; only EASY count differs) — same conclusion for both, matching `GEN.6`'s own precedent of treating structurally-identical K values symmetrically.

**One-KEY (3D/4D) semantics**: also **SUPPORTED** — per this phase's own §59 caution against assuming "Advanced = more of everything," single-KEY 3D/4D expresses Advanced identity through eligibility/peak-volume/full workout access rather than requiring additional structural KEY slots. The same historical v1/v2 artifact also modeled Advanced at 3D (`[34,46]`) and 4D (`[38,52]`), confirming Appsel itself once intended Advanced at single-KEY frequencies too.

## 4. Workout eligibility

Confirmed via direct catalog reading: `FARTLEK` (`eligiblePhases: [BUILD, TAPER]`), `THRESHOLD_TEMPO` (`[BUILD, RACE_SPECIFIC, FOUNDATION]`), `GOAL_PACE_TEN_K` (`[RACE_SPECIFIC, TAPER]`) are **Level-agnostic content** — no `ADVANCED`/`EXPERIENCED`/`INTERMEDIATE` string exists anywhere in the `workouts/` catalog folder. Beginner's own `V1BeginnerWorkoutEligibilityPolicy.IsDeferred` defers these workouts specifically for `TEN_K__4D__BEGINNER` — no equivalent restriction exists, or is needed, for Advanced. **Advanced×3D/4D reuse the exact same generic workout catalog Intermediate 3D/4D already use, with full/immediate access (no Foundation-deferral)** — zero new workout-definition content required.

For 5D/6D dual-KEY lanes, the binding mechanism is different: the existing prescription-profile documents (`INTERMEDIATE_5D_BUILD_PRIMARY`, `INTERMEDIATE_5D_BUILD_SECONDARY_CONTROLLED`, etc. — all 8 phase×lane combinations) are confirmed **Level-specific by construction** (every document key literally contains `INTERMEDIATE`; `FREQ.6D.26`'s own words: "genuinely Level+Distance-owned via `TEN_K_MASTER`'s shared `TEN_K_WORKOUT_PROGRESSION_V1`... despite the historical '5D' naming" — Level-owned, not Level-agnostic, which is exactly why they were reusable across 5D→6D **frequency** change but would not be reusable across a **Level** change). **No Advanced-named or Level-agnostic prescription-profile document exists anywhere in the repository.**

## 5. Prescription content blocker (5D/6D only)

Per §46 of this phase's own instructions ("if exact prescription authority itself is missing, mark `DECISION_REQUIRED`; implementation cannot invent it"): Advanced×5D/6D's dual-KEY structural/eligibility support is approved (§3), but the exact dose/intensity/repetition/recovery content for the 8 new `ADVANCED_5D_*`/`ADVANCED_6D_*` prescription-profile documents this would require is **not derivable from any evidence gathered in this phase** and is correctly out of scope for a support/authority decision (specifying interval reps/recovery/pace-targets is prescription-content authoring, forbidden here regardless). **Classification: `CATALOG_CONTENT_AUTHORITY_REQUIRED`** — this is the sole remaining blocker for Advanced×5D/6D implementation readiness. Advanced×3D/4D carry no equivalent blocker (§4).

## 6. Numeric authority (for structurally/eligibility-approved frequencies only)

Progression rates (0.07 preferred / 0.08 hard / 2.5km absolute cap) and taper factor (0.53) are confirmed **identical across every existing Level and frequency** in `VolumeSafetyPolicy.cs` (Intermediate 4D/5D/6D, Beginner 4D) — genuine Level-and-frequency-invariant authority, reused verbatim for Advanced with no new evidence needed (consistent with §26's own caution against inventing a faster progression merely because Advanced tolerates higher absolute load).

Long-run share is confirmed **frequency-owned, not Level-owned** (Beginner×4D's shares are byte-identical to Intermediate×4D's; 5D/6D share a distinct pattern from 4D, and 3D a third distinct pattern) — reused verbatim per frequency for Advanced:

| Frequency | Long-run pref/hard (selection/hardcap) | Source |
|---|---|---|
| 3D | 0.38 / 0.42 (0.40 / 0.42) | Reused from `ThreeDayIntermediate` |
| 4D | 0.30 / 0.36 (0.33 / 0.40) | Reused from `Default`/`BeginnerFourDay` |
| 5D | 0.28 / 0.36 (0.28 / 0.36) | Reused from `FiveDayIntermediate` |
| 6D | 0.28 / 0.36 (0.28 / 0.36) | Reused from `SixDayIntermediate` |

**`ADVANCED_3D_NUMERIC_AUTHORITY_TABLE`** / **`ADVANCED_4D_NUMERIC_AUTHORITY_TABLE`** / **`ADVANCED_5D_NUMERIC_AUTHORITY_TABLE`** / **`ADVANCED_6D_NUMERIC_AUTHORITY_TABLE`**:

| Item | 3D | 4D | 5D | 6D | Provenance |
|---|---|---|---|---|---|
| Observed readiness | Used directly | Used directly | Used directly | Used directly | Existing Intermediate principle, reused |
| Missing readiness | `DECISION_REQUIRED` | `DECISION_REQUIRED` | `DECISION_REQUIRED` | `DECISION_REQUIRED` | No provenance for a specific default (§7) |
| Zero readiness | `PRODUCT_INELIGIBLE` | `PRODUCT_INELIGIBLE` | `PRODUCT_INELIGIBLE` | `PRODUCT_INELIGIBLE` | Definitional — self-contradictory with claimed Advanced status |
| PeakVolumeBand | [34,46] km | [38,52] km | [42,58] km | [42,60] km | 3D/4D/5D: `HISTORICAL_PRODUCT_EVIDENCE` (real, once-`VALIDATED` Appsel v1/v2 rows, §2). 6D: `PRODUCT_DEFAULT_WITH_TIER_MATCHED_EVIDENCE_ENVELOPE`, derived directly from `FREQ.6D.25`'s own already-vetted real external Advanced-tier 6-day source (Women's Running, 59.55km calculated peak, rounded to 60) — not a new research cycle, reusing existing tier-exact evidence |
| ResolvedPeakReference | 40.0 km | 45.0 km | 50.0 km | 51.0 km | Band midpoint, `ProductDefaultWithEvidenceEnvelope` — no fixture-calibration source exists for Advanced (same situation `GEN.4C.3`'s "Path Z" resolved for Beginner; reusing that exact already-approved methodology, not inventing a new one) |
| Progression pref/hard/cap | 0.07/0.08/2.5 | 0.07/0.08/2.5 | 0.07/0.08/2.5 | 0.07/0.08/2.5 | Level+frequency-invariant, reused verbatim |
| Taper factor | 0.53 | 0.53 | 0.53 | 0.53 | Reused verbatim |
| Long-run pref/hard | 0.38/0.42 | 0.30/0.36 | 0.28/0.36 | 0.28/0.36 | Frequency-owned, reused verbatim |
| GE target cap | 40.0 km | 45.0 km | 50.0 km | 51.0 km | = ResolvedPeakReference, existing architecture |
| GE missing/zero rule | Same as Core (`DECISION_REQUIRED`/`PRODUCT_INELIGIBLE`) | Same | Same | Same | Consistent extension of §7's Core rule |

## 7. Advanced Level eligibility contract and the missing/zero readiness product question

Existing public input (`GeneratePreviewRequest`: `RecentWeeklyVolumeKm`, `RecentLongestRunKm`, `RecentRunsPerWeek`, `RecentRace`) is **confirmed sufficient** — no new field is added in this phase, per its own §62 instruction.

**Zero readiness → `PRODUCT_INELIGIBLE`** for every Advanced frequency: this is close to a direct canonical consequence of what "Advanced" categorically means — a request reporting zero recent running volume cannot be evidenced as Advanced-tier by definition, regardless of which frequency is requested.

**Missing readiness → `DECISION_REQUIRED`**, honestly left open rather than invented: unlike zero (a definitional contradiction), a merely-absent `RecentWeeklyVolumeKm` field does not by itself prove non-Advanced status, but no repository evidence establishes what a "missing-but-claims-Advanced" default starting volume should be, and inventing one (e.g. reusing Intermediate's 24km) would have no provenance, violating this phase's own decision standard (§63). This is a narrow, disclosed remainder — matching the precedent `FREQ.6D.24` set (an honest single-item `DECISION_REQUIRED` alongside an otherwise-closed authority set) rather than blocking the rest of the phase.

## 8. 7D — inherited, not re-evaluated

Per this phase's own §2 instruction: `FREQ.6D.23`'s calendar week-boundary conflict is confirmed frequency-global (the canonical `MinimumKeySessionToLongRunSeparationDays`/`MinimumKeySessionToKeySessionSeparationDays = 2` spacing rule has no Level parameter anywhere in its definition — re-confirmed unchanged since `GEN.6`'s own identical audit). **Advanced×7D is `PRODUCT_NON_SUPPORT` across Core, Runway, and LongHorizon** — inherited directly, with zero new evidence, numeric authority, workout eligibility, or Adaptation work performed for it, per this phase's own §2/§19 efficiency instruction.

## 9. Runway and LongHorizon structure (for approved frequencies)

Runway/GE structures remain frequency-owned, one-KEY, reused unchanged for Advanced (no evidence found requiring an Advanced-specific exception): 3D=1K+1E+1L, 4D=1K+2E+1L, 5D=1K+3E+1L, 6D=1K+4E+1L. Second-KEY onset for 5D/6D Core remains at Core Week 1 (existing frequency/horizon authority, not Level-specific, unchanged). Runway numeric authority (starting reference, progression, long-run share, Core-entry target) reuses the same per-frequency values as §6 and the existing shared GE→Runway/Core-entry clamp unchanged — no new continuity tolerance.

## 10. Adaptation

Confirmed via direct grep: zero `RunningBackground` references anywhere in `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/Adaptation/` — `NextWindowLoadDecisionPolicy.Evaluate(WindowExecutionSummary summary)` takes no Level parameter. **Adaptation is confirmed Level-agnostic.** Advanced reuses whichever N-session Adaptation state table already services each frequency today (the existing 3-session/4-session mechanisms already exercised by Intermediate×3D/4D's public activation, and the frozen 5-session/6-session tables) — zero new Adaptation authority for any Advanced frequency.

## 11. Architecture/hardcode audit (reconnaissance only)

No `Advanced`-related silent-assumption hardcode found: `VolumeSafetyPolicy.cs` has no `Advanced*` member at all (a clean absence, not a broken assumption); `V1CatalogPilotIdentityPolicy`'s allow-list has no `(Advanced, N)` entries and fails closed with an explicit exception message naming exactly which identities are resolvable. **Classification: `NO_GAP`** (correctly and narrowly scoped, as with `GEN.6`'s identical finding for Beginner) — no architecture blocks a future Advanced activation once its authority and content are complete.

## 12. Final support matrix

| Cell | Support | Blocker |
|---|---|---|
| 3D Core/Runway/LongHorizon | `SUPPORTED` | None — implementation-ready except missing-readiness `DECISION_REQUIRED` |
| 4D Core/Runway/LongHorizon | `SUPPORTED` | Same |
| 5D Core/Runway/LongHorizon | `SUPPORTED` | `CATALOG_CONTENT_AUTHORITY_REQUIRED` (prescription-profile dose content) |
| 6D Core/Runway/LongHorizon | `SUPPORTED` | Same as 5D |
| 7D Core/Runway/LongHorizon | `PRODUCT_NON_SUPPORT` | Inherited frequency-global calendar gap |

## 13. Cross-Level comparison (what is owned by what axis)

- **Distance-owned**: canonical 10K phase sequence (Foundation/Build/RaceSpecific/Taper), workout definitions and their `eligiblePhases`.
- **Frequency-owned**: structural KEY/EASY/LONG cardinality (RunLayout), long-run share, Runway/GE one-KEY structure and second-KEY-at-Core-Week-1 onset.
- **Level-owned**: starting volume, PeakVolumeBand, ResolvedPeakReference, readiness eligibility (missing/zero rules), workout-eligibility *timing restrictions* (Beginner's Foundation-deferral; none for Advanced), and — for dual-KEY frequencies specifically — prescription-profile *content* (dose/intensity per lane).
- **Level+Frequency-invariant (cross-axis) authority**: progression rates, taper factor, shared Core-entry/GE→Runway clamp, Adaptation state tables.

This confirms the phase's own architectural principle (§3): no per-cell duplicated plan templates were needed; every approved cell composes from `TEN_K_MASTER` + the frequency's RunLayout + a new Advanced Level policy (numeric authority only — see §6) + the existing shared RulePack/workout catalog.

## 14. Governance and closure

No production code, tests, catalog authoring, or migration performed. Intermediate frequency axis, Beginner frequency authority, Beginner×4D public status, and the frequency-global 7D non-support are all preserved unchanged.

**`ADVANCED_3D_4D_AUTHORITY_APPROVED_IMPLEMENTATION_READY_5D_6D_AUTHORITY_APPROVED_WITH_PRESCRIPTION_CONTENT_BLOCKER_7D_PRODUCT_NON_SUPPORT`.**

Per this phase's own §66/§72: since exact prescription content is the *only* remaining blocker for 5D/6D, and 3D/4D have none, the recommended (not scheduled) next step is a single narrow **Advanced 5D/6D prescription-profile / workout-dose content-authority closure**, after which one combined implementation/dark-verification wave can cover all four Advanced frequencies together (avoiding the wasteful per-frequency split §65 warns against). **No phase ID is fabricated here** — `NEXT_PHASE_NOT_YET_SCHEDULED`.
