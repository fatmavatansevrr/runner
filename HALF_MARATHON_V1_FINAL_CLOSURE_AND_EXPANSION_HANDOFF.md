# HALF_MARATHON V1 — FINAL CLOSURE AND EXPANSION HANDOFF

**This is the first document any future agent or developer should read before starting any new HALF_MARATHON (HM) work.** It supersedes the need to re-read all 33 individual `PHASE_HM_*.md` reports for routine reference — those remain the historical record of *how* each decision was reached, but this document is the single source of *what is currently true*.

```
CURRENT STATUS
HALF_MARATHON STANDARD CORE — PUBLIC (V1 8-cell matrix + HM-X1 6D expansion)

Public matrix (updated by PHASE_HM_X1_4, superseding the original V1-only matrix below):
  Beginner       3D  PUBLIC   4D  PUBLIC   5D  PRODUCT_INELIGIBLE   6D  OUT_OF_SCOPE
  Intermediate   3D  PUBLIC   4D  PUBLIC   5D  PUBLIC               6D  PUBLIC
  Advanced       3D  PUBLIC   4D  PUBLIC   5D  PUBLIC               6D  PUBLIC
  Experienced    3D  EXCLUDED 4D  EXCLUDED 5D  EXCLUDED             6D  EXCLUDED  (PHASE_EXP_0, product-wide)

Supported Core: 10–16 full weeks inclusive

Post-V1 (explicitly NOT missing work — separate future expansion tracks; 6D is no longer
in this list, having been closed by HM-X1.1-X1.4):
  2D, 7–9W compressed, ≤6W readiness-only, >16W Preparation Runway / LongHorizon,
  missing/zero-readiness base-building, quality-in-long-run, stretch-goal public warning

Next planned axis (not begun): custom/intermediate race-distance generalization for
10.0km < target distance < 21.1km, pilot candidate 16km (PHASE_HM_X1_4 §50 handoff).
```

**Original V1 (HM.18) 8-cell matrix, for historical reference — accurate as of HM.18/HM.19, superseded above by HM-X1.4's 6D activation:**
```
  Beginner       3D  PUBLIC   4D  PUBLIC   5D  PRODUCT_INELIGIBLE
  Intermediate   3D  PUBLIC   4D  PUBLIC   5D  PUBLIC
  Advanced       3D  PUBLIC   4D  PUBLIC   5D  PUBLIC
```

Final classification of the engagement that produced this state: `HM_V1_STANDARD_CORE_PUBLIC_ACTIVATION_COMPLETE` (HM.18).

---

## 1. Executive completion state

HALF_MARATHON's V1 completion (HM.0 → HM.18, 34 ledger rows / 32 distinct phase IDs) includes, and future agents must treat all of the following as **done, not partial**: canonical Core authority; dynamic 10–16W phase/stage allocation; distance-specific workout progression (including Advanced 5D's own genuinely-different dual-lane content); level modifiers for all three levels; frequency layouts (3D/4D/5D); volume authority; long-run authority; non-chained taper; pace/fallback hierarchy; calendar materialization (including hardness-aware dual-KEY spacing); workout binding; session prescription; adaptation compatibility; preview generation; typed public rejection paths; confirmation; persistence; active reads; mutations; public routing; controlled activation; 10K zero-delta proof; and catalog/regression proof.

**Do not describe HM as** "partially implemented," "dark only," "Intermediate only," "not public," or "14W only" — none of these are current. If a future repository change genuinely regresses this state, that regression must be recorded as a new, dated finding — not assumed from this document going stale silently.

---

## 2–4. Public V1 matrix, supported horizon, and explicit post-V1 scope

Already stated in the opening block. Two framing rules, stated explicitly per the engagement's own closing instruction:

- **2D and 6D are not "missing V1 cells."** They were never in V1 scope. Describing their absence as a gap in V1 is incorrect framing.
- **Beginner×5D is not an oversight.** It is a confident, evidence-backed `PRODUCT_INELIGIBLE` decision (HM.13 §6), not an "authority not yet resolved" placeholder.

---

## 5. Core phase authority (final, frequency- and level-independent)

```
HALF_MARATHON CoreCycle:  Minimum=10   Preferred=14   Maximum=16

Phase headroom (Min/Preferred/Max, all Level×Frequency cells, byte-identical
across half-marathon-master.v1/v2/v3.json):
  Foundation     2 / 3 / 4
  Build          3 / 4 / 5
  RaceSpecific   3 / 5 / 5
  Taper          2 / 2 / 2
```

**Resulting allocation table** (an *outcome* of the generic `CatalogPhaseAllocationResolver` applied to the headroom above — not a per-horizon hardcoded schedule; verify by re-running the resolver, not by hardcoding this table into new code):

| Horizon | Foundation | Build | RaceSpecific | Taper |
|---|---|---|---|---|
| 10W | 2 | 3 | 3 | 2 |
| 11W | 2 | 3 | 4 | 2 |
| 12W | 2 | 3 | 5 | 2 |
| 13W | 2 | 4 | 5 | 2 |
| 14W (frozen preferred) | 3 | 4 | 5 | 2 |
| 15W | 4 | 4 | 5 | 2 |
| 16W | 4 | 5 | 5 | 2 |

9W and 17W are infeasible (`DynamicCoreWeekSkeletonInfeasibleException`, `TARGET_BELOW_SUM_OF_MINIMUMS`/`TARGET_ABOVE_SUM_OF_MAXIMUMS`) — this is correct, frozen behavior, not a gap.

---

## 6. Stage authority (final, single primary lane — lane 0 — used by every HM cell)

| Stage | Min/Preferred/Max exposures | Compression | Extension |
|---|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | 2/3/4 | COMPRESSIBLE | EXTENDABLE |
| `BUILD_FASTER_THAN_HM_INTRO` | 0/1/1 | COMPRESSIBLE, `compressionPriority=1` (compressed first) | EXTENDABLE |
| `BUILD_THRESHOLD_DEVELOPMENT` | 3/3–4/5 | COMPRESSIBLE, `compressionPriority=2` | EXTENDABLE |
| `RACE_SPECIFIC_HM_INTRODUCTION` | 0/1/1 | COMPRESSIBLE | EXTENDABLE |
| `RACE_SPECIFIC_HM_DEVELOPMENT` (+fallback) | 1/2/2 | COMPRESSIBLE | EXTENDABLE |
| `RACE_SPECIFIC_HM_REHEARSAL` (+fallback) | 2/2/2, unchanged | **PROTECTED** | **FIXED_EXPOSURE** |
| `TAPER_HM_ACTIVATION` (+fallback) | 2/2/2, unchanged | **PROTECTED** | **FIXED_EXPOSURE** |

`PreferredExposures` semantics: `Baseline = PreferredExposures ?? MinimumExposures`; the compression floor is `MinimumExposures` whenever `PreferredExposures` is declared (else the historical hardcoded floor of 1). `CompressionPriority` semantics: lowest numeric value is compressed first; `null` sorts last (`int.MaxValue`), preserving byte-identical behavior for every pre-HM stage that never declares it. Both fields are additive, optional, and default to inert for any stage/policy that doesn't declare them — this is why every 10K stage remains zero-delta.

Intermediate 5D / Advanced 5D add a **lane 1** (secondary KEY) with its own, level-specific stage set — see §8/§9.

---

## 7. Beginner final authority table

| Field | 3D | 4D | 5D |
|---|---|---|---|
| Product status | PUBLIC | PUBLIC | **PRODUCT_INELIGIBLE** |
| PeakBandMin/Max | 22 / 32 | 28 / 38 | — |
| SelectedPeak | 27.0 | 33.0 | — |
| PreferredGrowthRate | 0.07 | 0.07 | — |
| HardGrowthRate | 0.08 | 0.08 | — |
| AbsoluteWeeklyCapKm | 2.0 | 2.5 | — |
| CalibrationStart (never a readiness default) | 15.5 | 19.0 | — |
| LR PreferredMin/Max | 0.34 / 0.40 | 0.30 / 0.36 | — |
| LR Selected | 0.38 | 0.33 | — |
| LR HardCap | 0.46 | 0.40 | — |
| AbsolutePeakLR (ceiling) | 16.0 km | 16.0 km | — |
| StartingLRCap | null (N/A) | null (N/A) | — |
| TaperW1 / W2 | 0.70 / 0.43 | 0.70 / 0.43 | — |
| Dose/Modifier | LOW (`BEGINNER_MODIFIER v2`) | LOW | — |
| MaxHardStimuli (allowed) | 1 | 1 | — |
| Actual hard structure | 1 (single KEY, no headroom question — layout has only one KEY slot) | 1 | — |
| HM_PACE policy | Eligible under `PACE_SOURCE_IN:RECENT_RACE` evidence gate; fallback THRESHOLD_TEMPO/EASY_STANDARD | same | — |
| Source phase | HM.13 (numeric), HM.14 (implementation) | HM.13, HM.14 | HM.13 §6 |
| Final artifact | `HalfMarathonBeginner3D` (`VolumeSafetyPolicy.cs`), `half-marathon-3d-beginner.v1.json` | `HalfMarathonBeginner4D`, `half-marathon-4d-beginner.v1.json` | no combination file exists (deliberate) |

**Beginner absolute-peak-LR ceiling (16.0 km) is Beginner-wide but LOWER than Intermediate/Advanced's 19.0 km** — derived from B.A.A. "Level One" evidence (~9–10mi), a deliberately different, lower ceiling for this level, not a typo or an unfinished broadening.

---

## 8. Intermediate final authority table

| Field | 3D | 4D | 5D |
|---|---|---|---|
| Product status | PUBLIC | PUBLIC | PUBLIC |
| PeakBandMin/Max | 28 / 40 | 36 / 50 | 44 / 58 |
| SelectedPeak | 34.0 | 43.0 | 51.0 |
| PreferredGrowthRate | 0.07 | 0.07 | 0.07 |
| HardGrowthRate | 0.08 | 0.08 | 0.08 |
| AbsoluteWeeklyCapKm | 2.0 | 2.5 | 2.5 |
| CalibrationStart | 20.0 | 25.0 | 29.5 |
| LR PreferredMin/Max | 0.34 / 0.40 | 0.30 / 0.36 | 0.28 / 0.36 |
| LR Selected | 0.38 | 0.33 | 0.28 |
| LR HardCap | 0.46 | 0.40 | 0.36 |
| AbsolutePeakLR (ceiling) | 19.0 km | 19.0 km | 19.0 km |
| StartingLRCap | null (N/A) | null (N/A) | **12.0 km** (the *only* HM cell with a real starting-LR cap) |
| TaperW1 / W2 | 0.70 / 0.43 | 0.70 / 0.43 | 0.70 / 0.43 |
| Dose/Modifier | STANDARD (`INTERMEDIATE_MODIFIER v8`) | STANDARD (`v8`) | STANDARD (bespoke `INTERMEDIATE_MODIFIER v9` + `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3`) |
| MaxHardStimuli (allowed) | 1 | 1 | **2** (bespoke exception, scoped only to this one cell) |
| Actual hard structure | 1 (single KEY) | 1 (single KEY) | 1 true-hard (KEY1) + KEY2 = **LIGHT_QUALITY, never counted as true-hard** |
| Secondary KEY (5D only) | — | — | Foundation/Taper = EASY_EQUIVALENT; Build/RaceSpecific = FARTLEK v6 (light-quality), fallback EASY_STANDARD; **never THRESHOLD_TEMPO, never HM_PACE** |
| HM_PACE policy | Eligible (KEY1), evidence-gated, fallback THRESHOLD_TEMPO/EASY_STANDARD | same | same for KEY1; KEY2 never uses HM_PACE |
| Source phase | HM.1.1/HM.1.4B/HM.1.5/HM.1.6/HM.7 (numeric), HM.2/HM.8 (impl) | HM.1–HM.5.3 (numeric+mechanism), HM.2 (impl) | HM.9 (KEY2 structure), HM.10 (numeric), HM.11 (impl) |
| Final artifact | `HalfMarathonIntermediate3D`, `half-marathon-3d-intermediate.v1.json` | `HalfMarathonIntermediate4D`, `half-marathon-4d-intermediate.v1.json` | `HalfMarathonIntermediate5D`, `half-marathon-5d-intermediate.v1.json` |

**Do not let Advanced's second-hard (true-hard) KEY2 semantics leak backward into Intermediate 5D.** Intermediate 5D's own KEY2 is deliberately light-quality-only; this was independently isolation-tested at HM.16/HM.18 (`IntermediateFiveDaySecondaryLane_StaysFartlekBased_UnaffectedByAdvancedFiveDay`) and must remain true.

---

## 9. Advanced final authority table

| Field | 3D | 4D | 5D |
|---|---|---|---|
| Product status | PUBLIC | PUBLIC | PUBLIC |
| PeakBandMin/Max | 36 / 48 | 46 / 60 | 54 / 70 |
| SelectedPeak | 42.0 | 53.0 | 62.0 |
| PreferredGrowthRate | 0.07 | 0.07 | 0.07 |
| HardGrowthRate | 0.08 | 0.08 | 0.08 |
| AbsoluteWeeklyCapKm | 2.5 | 2.5 | 2.5 |
| CalibrationStart | 24.5 | 31.0 | 36.0 |
| LR PreferredMin/Max | 0.34 / 0.40 (=Intermediate3D verbatim) | 0.30 / 0.36 (=Intermediate4D verbatim) | 0.28 / 0.36 (=Intermediate5D verbatim) |
| LR Selected | 0.38 | 0.33 | 0.28 |
| LR HardCap | 0.46 | 0.40 | 0.36 |
| AbsolutePeakLR (ceiling) | **19.0 km — NOT increased above Intermediate**, a deliberate user decision at HM.15 | 19.0 km | 19.0 km |
| StartingLRCap | null (N/A) | null (N/A) | null (N/A — no feasibility trigger found even here) |
| TaperW1 / W2 | 0.70 / 0.43 (clean fit) | 0.70 / 0.43 (**disclosed evidence tension**: band ceiling 60.0 exceeds the cited ~56km/week "lighter taper" threshold, though the selected peak 53.0 itself doesn't) | 0.70 / 0.43 (**disclosed evidence tension**: selected peak 62.0 itself EXCEEDS the ~56km/week threshold — the one HM cell where this is genuinely triggered, not just at a band edge) |
| Dose/Modifier | HIGH (`ADVANCED_MODIFIER v3`) | HIGH | HIGH |
| MaxHardStimuli (allowed) | 2 | 2 | 2 |
| Actual hard structure | **1** (headroom, not quota — `RUN_LAYOUT_3D` has exactly one KEY_SESSION slot) | **1** (same reasoning, `RUN_LAYOUT_4D` also has exactly one KEY slot) | **2** — the *only* HM cell where the level's full capacity is genuinely exercised |
| Secondary KEY (5D only) | — | — | Foundation = EASY_EQUIVALENT; **Build = THRESHOLD_TEMPO (true-hard)**; **RaceSpecific = THRESHOLD_TEMPO (true-hard)**; Taper = EASY_EQUIVALENT; fallback EASY_STANDARD; **KEY2 must NEVER use HM_PACE in HM V1** (reserved for KEY1 only, avoids duplicating race-pace-specific rehearsal) |
| HM_PACE policy | Eligible (KEY1), evidence-gated | same | same for KEY1 only |
| Source phase | HM.15 (numeric+structural, two user-escalated decisions), HM.16 (impl) | HM.15, HM.16 | HM.15 §7/§8 (KEY2 model), HM.16 |
| Final artifact | `HalfMarathonAdvanced3D`, `half-marathon-3d-advanced.v1.json` | `HalfMarathonAdvanced4D`, `half-marathon-4d-advanced.v1.json` | `HalfMarathonAdvanced5D`, `half-marathon-5d-advanced.v1.json`, `half-marathon-master.v3.json`, `half-marathon-workout-progression.v3.json` |

**No mismatch was found between any final report's own frozen table and the real, current source-code/catalog values** — every field above was independently cross-checked against `VolumeSafetyPolicy.cs`, the taper-policy classes, and the real catalog JSON files during this closure phase.

---

## 10. Volume/peak semantics (frozen, HM.5.3, applies to every V1 cell)

```
SelectedPeakReference = ceiling / reference, NOT a mandatory endpoint.

Shorter Core (10–13W): may peak below the selected reference — the generator
  must NOT accelerate weekly growth merely to force the peak to be reached.

Longer Core (15–16W): must NOT extrapolate above the selected reference solely
  because more weeks exist — governed by ResolvedPeakReferenceIsSelectedCeiling
  (an additive, opt-in VolumeSafetyPolicy field; every HM policy sets it true;
  every 10K policy defaults false and is unaffected).

Actual peak = safe reachable progression ∩ selected-peak authority ∩ volume
  safety (absolute/percentage growth caps) ∩ runtime readiness evidence.
```

This rule is frozen identically across all 8 public HM cells — do not reinterpret it per-cell.

---

## 11. Long-run semantics (frozen, applies to every V1 cell)

```
PreferredLongRunShareMin / Max  = normal desired operating range
SelectedLongRunShare            = ordinary target
HardLongRunShareCap             = absolute share safety boundary — NEVER the
                                   routine target
PreferredAbsolutePeakLongRunKm  = ceiling, not target
StartingAbsoluteLongRunCap      = starting upper bound, not a default
```

A future phase must not reinterpret any cap as a target. The post-rounding LR safety mechanism uses floor-rounding (`RoundDown`) specifically for hard-ceiling checks (never for ordinary target/selection values) — this exists specifically because nearest-rounding could push a computed value fractionally above its true percentage ceiling (HM.5.3's own fix for the 12W/15W overshoot defect).

---

## 12. Taper authority (frozen, applies to every V1 cell)

```
Duration: 2 weeks, HM_DISTANCE_WIDE (architecturally proven — HALF_MARATHON_MASTER
  has no level field; byte-identical Taper headroom across v1/v2/v3)
Mechanism: NON-CHAINED. Both taper weeks are computed independently against
  ONE fixed pre-taper anchor (the resolved peak/ceiling). Taper Week 2 is
  NEVER derived as Week1 × a second multiplier — that was a real, confirmed,
  since-fixed defect (see §14, "chained taper").
```

| Cell | Anchor (km) | Week1 (×0.70) | Week2 (×0.43) |
|---|---|---|---|
| Beginner 3D | 27.0 | 19.0 | 11.5 |
| Beginner 4D | 33.0 | 23.0 | 14.0 |
| Intermediate 3D | 34.0 | 24.0 | 14.5 |
| Intermediate 4D | 43.0 | 30.0 | 18.5 |
| Intermediate 5D | 51.0 | 35.5 | 22.0 |
| Advanced 3D | 42.0 | 29.5 | 18.0 |
| Advanced 4D | 53.0 | 37.0 | 23.0 |
| Advanced 5D | 62.0 | 43.5 | 26.5 |

**Advanced 5D's own taper multipliers were deliberately, explicitly retained despite a disclosed high-volume evidence tension (§9/§14). This is a settled decision — do not reopen it without new evidence.** No exact alternative "lighter taper" percentage exists anywhere in this engagement's evidence base; inventing one would violate the no-fabrication discipline this entire engagement has followed.

---

## 13. Pace authority (frozen, level-blind mechanism)

```
Hierarchy: recent reliable race → time trial → structured test → normal pace
  evidence → effort-only fallback.

Desired finish-time goal NEVER manufactures current training pace.
No independent pace evidence → approved fallback/effort semantics (the plan
  must still materialize).
```

**Advanced 5D's own KEY2 never uses HM_PACE**, regardless of evidence state — a deliberate, permanent rule (avoids duplicating KEY1's own race-pace-specific rehearsal in the same week). `eligibleWorkouts` in a level-modifier JSON is **not** the runtime binder's actual authority — it is consulted only by catalog-packaging/dependency bookkeeping (`PlanCatalogBundleLoader.cs`). The real runtime gate is the workout-progression stage's own `requires` conditions (`PACE_SOURCE_IN`, `GOAL_FEASIBILITY_IN`), which are level-blind by construction. Do not "fix" a future HM_PACE eligibility question by editing `eligibleWorkouts` alone.

---

## 14. Hard-stimulus authority matrix

| Level | Frequency | MaxHardAllowed | ActualStructuralHardCount | SecondaryKeySemantics | LongRunHardness |
|---|---|---|---|---|---|
| Beginner | 3D | 1 | 1 | n/a (no KEY2) | controlled/easy |
| Beginner | 4D | 1 | 1 | n/a | controlled/easy |
| Intermediate | 3D | 1 | 1 | n/a | controlled/easy |
| Intermediate | 4D | 1 | 1 | n/a | controlled/easy |
| Intermediate | 5D | 2 (bespoke) | 1 | LIGHT_QUALITY (FARTLEK), never true-hard | controlled/easy |
| Advanced | 3D | 2 | 1 | n/a (headroom, not quota) | controlled/easy |
| Advanced | 4D | 2 | 1 | n/a (headroom, not quota) | controlled/easy |
| Advanced | 5D | 2 | **2** | TRUE-HARD (THRESHOLD_TEMPO), the only cell exercising full capacity | controlled/easy |

**`MaximumTrueHardStimuliPerWeek = 2` does NOT mean every Advanced frequency must contain 2 hard sessions.** It is a capacity ceiling; only Advanced 5D's own structural layout (two `KEY_SESSION` slots) and its own frozen product decision (HM.15 §7) actually exercise it. 3D/4D's single-KEY-slot layouts leave the extra capacity as genuine, permanent, unused headroom — this is not an implementation gap.

---

## 15. Calendar authority (final, generic, level-blind)

Rules unchanged from the generic mechanism, applied identically to every HM cell:
- `PreferredDays`/`LongRunDayPreference` drive placement; `StartDate` anchors the schedule.
- HARD+HARD adjacency is prohibited; approximately ≥48h required between two true-hard stimuli where two exist in the same week.
- `LONG_RUN` remains controlled/easy in every HM cell (no quality-in-long-run in V1).
- Hardness resolution is workout-family-derived (EASY/LONG_RUN → EasyEquivalent; everything else → TrueHard; unknown/unresolved → fails safe to hard) and threaded into calendar placement generically — the calendar itself required **zero new code** to correctly handle Advanced 5D's two true-hard KEY sessions per week; the existing two-pass placement search (legacy all-KEY-equal first, hardness-aware retry only if infeasible) already handled it.
- **Advanced 5D's dual-hard placement is public-tested**, not just dark-tested: `Hm18PublicActivationAndBoundaryTests.Advanced5D_DualKeyLane_RoundTrip_...` proves real dated placement, correct spacing, and independent post-mutation state via the real public HTTP surface and a real PostgreSQL database.

No new calendar rule was added by any HM phase — do not add one in future expansion work without first re-proving the existing mechanism is genuinely insufficient.

---

## 16. Public error contract (final, FE/API-usable reference — use these exact codes, do not paraphrase)

| Condition | Exception type | HTTP | Reason code |
|---|---|---|---|
| HM horizon <10 full weeks | `PlanCoreHorizonTooShortException` | 422 | `PLAN_CORE_HORIZON_TOO_SHORT` |
| HM horizon 10–16 full weeks | — (eligible) | 200 | — |
| HM horizon >16 full weeks | `PlanHorizonStartTooEarlyException` (subtype of `PlanHorizonCompositionRequiredException`; carries advisory `RecommendedStartDate = RaceDate − 16 weeks`) | 422 | `PLAN_HORIZON_START_TOO_EARLY` |
| Beginner×5D | `PlanProductIneligibleException` | 422 | `PRODUCT_INELIGIBLE` |
| HM 2D (any level) | `HmFrequencyNotSupportedException` (HALF_MARATHON-scoped; 2D/6D remain valid, real, publicly-activated frequencies for other distances, e.g. TEN_K — this is never a global rule) | 422 | `HM_FREQUENCY_NOT_SUPPORTED_V1` |
| HM 6D (any level) | `HmFrequencyNotSupportedException` | 422 | `HM_FREQUENCY_NOT_SUPPORTED_V1` |
| Missing recent-weekly-volume readiness | `PlanProductIneligibleException` wrapping `CatalogVolumeInvalidReadinessInputException` | 422 | `CATALOG_VOLUME_INVALID_READINESS_INPUT` (or the specific level's own reason code, e.g. `ADVANCED_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE`-family) |
| Explicit-zero readiness | Same family as above (pre-existing, unmodified by HM.18) | 422 | Same family |
| Preferred-day/long-run-day placement infeasible | `CatalogPreferredDayPlacementInfeasibleException` | 422 | `PREFERRED_DAY_PLACEMENT_INFEASIBLE` |
| No independent exact pace evidence, otherwise eligible | *(not a rejection)* — plan still materializes via approved fallback (THRESHOLD_TEMPO/EASY_STANDARD for KEY1; KEY2 never uses HM_PACE) | 200 | — |

**No case in this table returns a raw, unqualified 500** — proven by HM.18's own full negative/boundary acceptance matrix.

---

## 17. Confirmation/persistence model (final, proven invariant)

```
Every public HM V1 preview is catalog-sourced (generation_source == "CATALOG")
  and therefore follows catalog confirmation — never legacy SQL confirmation.
```

- **Why this holds structurally, not just empirically:** `HALF_MARATHON` has no legacy-SQL-engine template of its own to fall back into — every non-catalog outcome for HM is one of the typed rejections in §16, never a legacy-path 200.
- **Confirm-dispatch mechanism** (unchanged by HM.18, documented exactly as found): `ConfirmPlanAsync` dispatches catalog-vs-legacy based on the *stored preview's own* `generation_source` JSON field — content-driven, not `GoalDistance`-driven. This is why HM.18 had to add a direct, real-database test (`AllEightPublicCells_PreviewIsCatalogSourced_AndConfirmPersistsCatalogRows`) rather than assume distance alone guarantees the right dispatch.
- **Advanced 5D real HTTP/PostgreSQL proof** (`Advanced5D_DualKeyLane_RoundTrip_...`): two distinct `TrainingDay` rows persist for the two KEY sessions, with distinct `Id`, distinct `Date`, and distinct `CatalogWorkoutDefinitionKey`/`CatalogProgressionStageKey`. Completing KEY1 and marking KEY2 "Not Today" leaves both rows with independently correct state (`Completed`/`Missed`) and unchanged workout identity — confirmed by a fresh direct database read after both mutations.
- **Legacy single-workout-per-day mapper** (`PlanServices.cs`'s `GetTitleForDay`/`GetDescriptionForDay`): proven, by the structural argument above plus the direct test proof, to be **`NON_BLOCKING_LEGACY_LIMITATION`** for HM — it is unreachable by any public HM request. Per this engagement's own rule, unreachable legacy limitations are not turned into future work items; do not create a task to "fix" it unless route reachability genuinely changes in a later phase.

---

## 18. Active-plan flow index

| Flow | HM compatibility | Proof level |
|---|---|---|
| Home | Generic, HM-safe | Explicit HM round-trip (all 3 acceptance tests read Home) |
| Calendar | Generic, HM-safe | Explicit HM round-trip |
| Training Day / session detail | Generic, HM-safe | Explicit HM round-trip |
| Complete | Generic, HM-safe | Explicit HM round-trip (Advanced 5D KEY1 completed) |
| Not Today (create + confirm) | Generic, HM-safe | Explicit HM round-trip (Advanced 5D KEY2 marked Not Today) |
| Pending Confirmation | Generic, HM-safe | Proven generic by direct code read (HM.17 §12); not separately HM-round-tripped |
| Plan Details / active plan retrieval | Generic, HM-safe | Explicit HM round-trip |
| Profile / overview | Generic, distance-blind (no plan-specific branching found) | Proven generic by direct code read; not separately HM-round-tripped |
| Cancel | Generic, HM-safe | Proven generic by direct code read (HM.17 §12); not separately HM-round-tripped this engagement |

**Do not re-audit the "proven generic by direct code read" rows without new cause** — HM.17 already confirmed zero `TEN_K`/`TenK`/distance-literal branching in `QueryAndMutationServices.cs`/`PlanServices.cs`'s own read/mutation methods for these flows.

---

## 19. Catalog version matrix (real, current, verified against source files)

| Public cell | Combination file | Master | Progression | Level modifier | RunLayout | Rule pack |
|---|---|---|---|---|---|---|
| Beginner 3D | `half-marathon-3d-beginner.v1.json` | v1 | v1 | `BEGINNER_MODIFIER v2` | `RUN_LAYOUT_3D v1` | `APPSEL_RACE_PLAN_V1 v10` |
| Beginner 4D | `half-marathon-4d-beginner.v1.json` | v1 | v1 | `BEGINNER_MODIFIER v2` | `RUN_LAYOUT_4D v1` | v10 |
| Intermediate 3D | `half-marathon-3d-intermediate.v1.json` | v1 | v1 | `INTERMEDIATE_MODIFIER v8` | `RUN_LAYOUT_3D v1` | v10 |
| Intermediate 4D | `half-marathon-4d-intermediate.v1.json` | v1 | v1 | `INTERMEDIATE_MODIFIER v8` | `RUN_LAYOUT_4D v1` | v10 |
| Intermediate 5D | `half-marathon-5d-intermediate.v1.json` | **v2** | **v2** | **`INTERMEDIATE_MODIFIER v9`** (bespoke) | `RUN_LAYOUT_5D v1` | v10 |
| Advanced 3D | `half-marathon-3d-advanced.v1.json` | v1 | v1 | `ADVANCED_MODIFIER v3` | `RUN_LAYOUT_3D v1` | v10 |
| Advanced 4D | `half-marathon-4d-advanced.v1.json` | v1 | v1 | `ADVANCED_MODIFIER v3` | `RUN_LAYOUT_4D v1` | v10 |
| Advanced 5D | `half-marathon-5d-advanced.v1.json` | **v3** | **v3** | `ADVANCED_MODIFIER v3` | `RUN_LAYOUT_5D v1` | v10 |

**Do not assume all HM cells share one master/progression version — they explicitly do not.** Three distinct master/progression version pairs exist:
- **v1/v1**: single-lane, used by all six 3D/4D cells across all three levels (byte-identical content).
- **v2/v2**: dual-lane, Intermediate 5D's own FARTLEK-based light-quality KEY2 content.
- **v3/v3**: dual-lane, Advanced 5D's own genuinely-different THRESHOLD_TEMPO-based true-hard KEY2 content (byte-identical to v2 in the master file except the progression-version pointer; the progression file's lane 0 is byte-identical across all three versions — only lane 1's Build/RaceSpecific content differs between v2 and v3).

**Level modifier versions in use**: `BEGINNER_MODIFIER v2` (v1 remains pinned for 10K's own Beginner combinations, untouched), `INTERMEDIATE_MODIFIER v8` (3D/4D) and the bespoke `v9` (5D only, raises the hard-stimulus cap to 2 for that cell alone, pairs with `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3` instead of `v8`'s own `v4`), `ADVANCED_MODIFIER v3` (all three Advanced frequencies; v2 remains pinned for 10K's own Advanced combinations).

**Peak-volume-band artifact**: `peak-volume-bands.v9.json`, edited in place across HM.7/HM.8/HM.10/HM.13/HM.14/HM.15/HM.16 (this file's lifecycle status — `VALIDATED`, not `PUBLISHED` — permits in-place row addition without a version bump; confirmed correct, not an oversight). Contains all 8 public HM rows plus the three Intermediate rows that predate the level-expansion work.

**Direct workout-definition versions materially pinned**: `FARTLEK v6` (Intermediate 5D KEY2 only — widens eligible phases vs. the primary lane's own `FARTLEK v4`), `THRESHOLD_TEMPO v4` (used by both the primary lane everywhere and Advanced 5D's own KEY2 — `THRESHOLD_TEMPO v5` exists and is Advanced-eligible per `advanced-modifier.v3.json` but is **not yet wired into any HM progression stage**, a disclosed, non-blocking content-differentiation gap from HM.15/HM.16), `HM_PACE v1` (primary lane only, every level).

---

## 20. Catalog lifecycle state (as it actually exists, not an imagined model)

All 8 public HM combination files sit at `"status": "VALIDATED"` in the source tree — **never `"PUBLISHED"` in source**, identical to every real, live, currently-public TEN_K combination file (verified by direct comparison, e.g. `ten-k-3d-beginner.v1.json` also reads `VALIDATED`). `CatalogStamper.StampAsPublished` promotes every non-DRAFT (i.e., `VALIDATED`) source document to `PUBLISHED` status only inside the **built release artifact** produced by the real publisher pipeline — source files are never edited to say `PUBLISHED`. This means: **do not "fix" a future HM combination file by changing its status field to `PUBLISHED`** — that would be a deviation from this repository's own real, working architecture, not a correction.

The actual, sole runtime gate controlling HM's public reachability is the compile-time C# switch in `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` (widened by a 4-line change in HM.18) — not a runtime feature flag, not a database-driven allow-list, not a catalog-status flag. "Rollback" means reverting that code change and redeploying (reasoned, not live-server-executed, to be safe per HM.18 §25–26 — no read/mutation path for an already-confirmed plan depends on that identity check).

---

## 21. Immutability / versioning rule

**Published/frozen historical catalog artifacts must not be mutated merely to support future HM expansion.** Future changes should prefer a new artifact version plus a new combination/reference, while existing HM V1 public cells remain pinned to their current versions.

**Version splits created during HM work** (do not silently collapse these back to "one master" in future work):
- `half-marathon-master.v1.json` — pinned by all six 3D/4D public combinations.
- `half-marathon-master.v2.json` — pinned only by Intermediate 5D.
- `half-marathon-master.v3.json` — pinned only by Advanced 5D.
- `half-marathon-workout-progression.v1/v2/v3.json` — same split, same reasons.
- `intermediate-modifier.v8.json` vs `v9.json` — v9 exists solely to raise the hard-stimulus cap for Intermediate 5D; v8 remains pinned for 3D/4D.
- `beginner-modifier.v1.json` (10K, untouched) vs `v2.json` (HM, adds `HM_PACE`).
- `advanced-modifier.v2.json` (10K, untouched) vs `v3.json` (HM, adds `HM_PACE` and a previously-missing `FARTLEK v4` declaration).

---

## 22. Final test baseline (exact, from HM.18's own final, independently re-verified regression)

```
PlanCatalog.Tests:               1510 / 1510  PASS
RuntimeCatalog subtree:          3798 / 3798  PASS
Full monolithic regression:      4727 / 4731  (4 failed, 0 error)
```

**The exact 4 durable, pre-existing, documented failures** (by full test identity — do not record this as merely "4 known failures"):

1. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
3. `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
4. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

None of these four are caused by, or related to, any HM production code — they are pre-existing (legacy SQL explicit-zero break-even volume calculation, a LongHorizon wall-clock-dependent read-model identity artifact, and the SW.09 explicit-zero end-to-end policy). Any future HM-X phase's own regression must match this exact 4-item baseline by name — a changed count OR a changed name set means a real regression occurred, even if the total count happens to coincidentally match.

---

## 23. Acceptance-test index

| Behavior | Test class | Test method(s) | What it proves |
|---|---|---|---|
| All 8 public cells resolve/preview | `Hm18PublicActivationAndBoundaryTests` | `AllEightPublicCells_PreviewIsCatalogSourced_AndConfirmPersistsCatalogRows` | Every public cell's preview is catalog-sourced and confirms into catalog-backed persisted rows |
| Beginner 3D round trip | same | `LightRoundTrip_PreviewConfirmReadPersist("beginner", 3, ...)` | Preview→confirm→Home read for a real Beginner 3D plan |
| Intermediate 4D round trip | same | `LightRoundTrip_PreviewConfirmReadPersist("intermediate", 4, ...)` | Same, Intermediate 4D |
| **Advanced 5D dual-KEY round trip (mandatory acceptance proof)** | same | `Advanced5D_DualKeyLane_RoundTrip_DistinctIdentitiesAndIndependentStateAfterCompleteAndNotToday` | Real HTTP/PostgreSQL: preview→catalog-source assertion→confirm→persisted dual-KEY inspection→Home/Calendar/Detail reads→Complete KEY1→Not-Today KEY2→independent-state re-read |
| 10W/16W boundaries eligible | same | `BoundaryWeeks_TenAndSixteen_AreEligible` | Both Core boundary horizons return 200 |
| 9W typed rejection | same | `NineWeekHorizon_ReturnsTypedTooShort_NotComposeRequired_NotFiveHundred` | `PLAN_CORE_HORIZON_TOO_SHORT`, not a 500 or the wrong exception |
| 17W typed rejection + advisory date | same | `SeventeenWeekHorizon_ReturnsTypedStartTooEarly_WithRecommendedStartDate` | `PLAN_HORIZON_START_TOO_EARLY` with correct `RecommendedStartDate = RaceDate − 16w` |
| Beginner 5D product-ineligible (public) | same | `BeginnerFiveDay_ReturnsTypedProductIneligible_NotTemplateNotFound` | Real public HTTP path, not just the internal `TryResolveCandidate` seam |
| HM 2D/6D unsupported | same | `UnsupportedHmFrequency_ReturnsTypedRejection_NotSilentLegacyFallthrough` | `HM_FREQUENCY_NOT_SUPPORTED_V1`, HM-scoped, no silent legacy fallthrough |
| Missing readiness rejection | same | `MissingReadiness_ReturnsTypedProductIneligible_NotFiveHundred` | Typed 422, not an uncaught 500 (a real gap this same phase discovered and fixed) |
| TEN_K zero-delta (2 spot checks) | same | `TenKZeroDelta_FourteenWeekIntermediateFourDay_StillWorksIdentically`, `TenKZeroDelta_FifteenWeekHorizon_StillThrowsCompositionRequired_NotStartTooEarly` | TEN_K's own public behavior is provably unaffected by HM's horizon-gate fix |
| Beginner 5D internal ineligibility (pre-public seam) | `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests` | (lines 375–393) | `TryResolveCandidate(HalfMarathon, Beginner, 5)` returns null while Intermediate×5D resolves non-null |
| Intermediate 5D KEY2 stays FARTLEK (isolation) | (HM.16/HM.18 test suite) | `IntermediateFiveDaySecondaryLane_StaysFartlekBased_UnaffectedByAdvancedFiveDay` | Advanced 5D's different KEY2 model does not leak into Intermediate 5D |
| Every dark 10–16W horizon per cell | `Hm2FullDarkVerticalSliceTests`, `Hm8Intermediate3DFullDarkVerticalSliceTests`, `Hm11Intermediate5DFullDarkVerticalSliceTests`, `Hm14Beginner3D4DFullDarkVerticalSliceTests`, `Hm16Advanced3D4D5DFullDarkVerticalSliceTests` | (multiple, per-horizon theories) | Full phase/stage/volume/LR/taper/calendar/binding materialization for every cell at every one of the 7 canonical horizons |
| Stage occupancy across all horizons | `Hm52StageOccupancyAllHorizonsTests`, `Hm52aStageCompressionPriorityOccupancyTests` | (multiple) | The generic phase/stage allocator produces the exact frozen occupancy table at every horizon |
| Taper mechanism (non-chained) | `Hm14bTaperVolumeMultiplierMechanismTests` | (multiple) | Taper Week 2 ≠ Week1 × second multiplier, for every taper-bearing cell |
| Long-run share peak-week mechanism | `Hm16LongRunSharePeakWeekMechanismTests` | (multiple) | LR share ramp/ceiling mechanism holds at peak week for every cell |

Do not duplicate any of this test code in a future expansion phase — extend these files/patterns instead.

---

## 24. 10K zero-delta map

HM generalization touched the following shared 10K architecture; each was re-verified zero-delta at the point it was generalized, and again in HM.18's own final regression:

| Generic mechanism | Generalized at | Live 10K zero-delta proof |
|---|---|---|
| Phase allocation (`CatalogPhaseAllocationResolver`) | Already generic before HM; no change needed | Every pre-existing 10K phase-allocation test, unmodified |
| `PreferredExposures` (stage allocator) | HM.5.2 | Additive/nullable; every 10K stage leaves it null, byte-identical |
| `CompressionPriority` (stage allocator) | HM.5.2A | Additive/nullable; same |
| Selected-peak ceiling (`ResolvedPeakReferenceIsSelectedCeiling`) | HM.5.3 | Additive/opt-in; every 10K policy defaults `false`, including `BeginnerFourDay` (whose own legitimate 13–14W extrapolation past its own reference was specifically preserved — this was caught as a real regression risk mid-phase and fixed by making the cap opt-in rather than unconditional) |
| Non-chained taper (`TaperVolumeMultipliers` list) | HM.1.4B | Additive/optional trailing field; all 17 pre-existing named 10K policies unaffected, degrade to single-element list |
| LR final rounding (`RoundDown` for hard ceilings) | HM.5.3 | Scoped only to the hard-ceiling clamp path, never ordinary target/selection values |
| 3D absolute-cap parameterization (`longRunHardCapShare` param) | HM.8 | Defaults to the renamed-but-unchanged `DefaultLongHardCap=0.42d` for every existing 10K caller |
| Hardness-aware calendar (two-pass placement) | HM.11 | First pass is byte-identical to the pre-existing all-KEY-equal algorithm; hardness-aware retry only activates when the first pass is infeasible |
| Level/frequency dispatch (`ForHalfMarathon{Level}DaysPerWeek`) | HM.8/HM.14/HM.16 | New, parallel, HM-specific dispatchers — never modifies 10K's own `ForBeginnerDaysPerWeek`/`ForIntermediateDaysPerWeek`/`ForAdvancedDaysPerWeek` |
| Public routing / typed contracts | HM.18 | `RaceHorizonPolicy.DecideForDistance` delegates to the exact unmodified parameterless `Decide` for every non-HalfMarathon distance; `IsSupportedIdentity`'s widened condition is byte-identical for TEN_K by construction; two dedicated TEN_K zero-delta spot-check tests plus the full monolithic regression |

This table is the regression map for any future HM-X phase touching shared code — re-run (or re-verify by direct read) the specific row's own zero-delta proof before considering the change safe.

---

## 25. Defect-family lessons (architectural, not chronological)

| Defect family | Root assumption | Observed HM manifestation | Generic fix | Regression guard |
|---|---|---|---|---|
| Distance-blind routing/gates | "There is only ever one distance" | `V1CatalogPilotIdentityPolicy`'s 2-arg TenK-pinned overloads; `PlanServices`'s horizon gate hardcoded to TEN_K's 8/12/14 bounds | Distance-aware N-arg overloads / `DecideForDistance`, each non-target distance delegating unchanged to the original | TEN_K zero-delta tests at every widening point |
| Frequency fallthrough | "An unsupported combination can just fall through to the old engine" | Unsupported HM frequencies silently routed to the legacy SQL engine instead of a typed rejection | HM-scoped (never global) typed rejection, reusing existing exception/reason-code families | Explicit negative-matrix acceptance tests |
| Fixed-frequency/fixed-week assumptions | "This is the only frequency/week-count this code will ever see" | 4D-only volume dispatch; TEN_K-only single-taper-week eligibility check applied unconditionally | Typed per-frequency dispatchers; distance-family-scoped guards | Cross-frequency zero-delta tests at each widening |
| Single-KEY assumptions | "There is exactly one KEY session per week" | Taper-completeness validators, calendar hardness classification, session-volume allocator all originally implicit single-KEY | `LaneOrdinal`-aware validators; family-based (not role-count-based) hardness classification; role-aware (not hardness-aware) volume allocator proven to already generalize | Intermediate-5D/Advanced-5D isolation tests; dual-KEY round-trip acceptance test |
| Chained taper | "Each week's reduction can be computed from the previous week's own output" | 2-week taper produced compounding — 72.1% cumulative reduction, far outside any evidence envelope | Fixed pre-taper anchor; every taper week computed independently against it | Explicit non-chained assertion (`taper[1] != round(taper[0] × mult2)`) in every HM taper test |
| Stage-compression ordering | "Descending `RelativeOrder` is a sufficient compression tie-break" | Contradicted the intended "drop lowest-info stage first" compression order for Build-phase stages | `CompressionPriority` field, explicit ascending priority, `RelativeOrder` only as final tie-break | Full stage-occupancy-across-all-horizons regression table |
| Selected-peak extrapolation | "Linear growth projection has no natural ceiling" | 16W horizon overshot the selected peak reference (46.5km vs. the 43.0km reference) | Opt-in `ResolvedPeakReferenceIsSelectedCeiling`, capping transitions at the golden-fixture count | Peak-volume-never-exceeded assertion at every horizon, every cell |
| LR rounding boundary | "Nearest-rounding is safe for a hard ceiling too" | 12W/15W could round a computed LR share fractionally above its true hard-cap percentage | `RoundDown` (floor) used exclusively for hard-ceiling/absolute-ceiling clamps | LR-share-never-exceeds-hard-cap assertion at every horizon, every cell |
| Catalog-version pinning | "One master/progression version can serve every frequency at a given level" | 5D's own dual-lane content needed real, different lane-1 content per level — reusing one shared version would have leaked Intermediate's FARTLEK content into Advanced, or vice versa | New, additive master/progression versions per genuinely-different content shape; existing single-lane 3D/4D cells stay pinned to v1 | Explicit version-reference tables (§19/§21 above) |
| Typed expected-product errors | "An exception is an exception; the generic 500 handler will catch it" | Raw, uncaught 500s for genuinely expected user states: horizon boundaries, unsupported frequency, preferred-day infeasibility, missing readiness | A typed exception hierarchy with stable, machine-readable reason codes for every expected outcome | The "no case may return a raw 500" negative/boundary acceptance matrix |
| Catalog-source confirmation containment | "Confirmation dispatch will naturally follow generation dispatch" | Confirm-time dispatch is keyed on a stored, content-driven `generation_source` field, not `GoalDistance` — an unproven assumption until directly tested | Direct, real-database proof test across every public cell | `AllEightPublicCells_PreviewIsCatalogSourced_AndConfirmPersistsCatalogRows` |

---

## 26. Do-not-reopen register

**These decisions may be reopened ONLY with new evidence + a new authority phase + an explicit intended-delta review — never silently, never as a side effect of unrelated work.**

- HM CoreCycle = 10 / 14 / 16.
- Phase headroom (Foundation 2/3/4, Build 3/4/5, RaceSpecific 3/5/5, Taper 2/2/2).
- Stage headroom/compression semantics (`PreferredExposures`, `CompressionPriority`, protected/fixed-exposure stages).
- Selected-peak = ceiling/reference, never a mandatory target.
- Non-chained 2-week taper mechanism.
- HM quality-in-long-run: unsupported in V1.
- Beginner×5D: `PRODUCT_INELIGIBLE`.
- Beginner 3D/4D authority (§7 table).
- Intermediate 3D/4D/5D authority (§8 table).
- Advanced 3D/4D/5D authority (§9 table).
- Advanced 5D KEY2: THRESHOLD_TEMPO true-hard in Build/RaceSpecific, EASY_EQUIVALENT in Foundation/Taper, never HM_PACE.
- Advanced absolute-peak-LR ceiling = 19.0 km (explicitly NOT raised above Intermediate — a deliberate, user-confirmed decision, not an oversight).
- Pace-evidence hierarchy (recent race → time trial → structured test → normal pace → effort-only).
- Calibration-value-is-never-a-readiness-fallback invariant (§27 below).
- Desired-goal-pace ≠ current-training-pace invariant.
- The frozen V1 public matrix itself (§2).
- The 10–16W V1 horizon boundary.
- HM 2D/6D post-V1 classification.

---

## 27. Superseded decision register

| Old decision | Superseded by | Final authority |
|---|---|---|
| HM master `CoreCycle` = 14/14/14 (runtime-narrowed) | 10/14/16 | HM.2.1 |
| Point-valued phase/stage headroom (no compression/extension range) | Dynamic Min/Preferred/Max headroom | HM.4 (design), HM.5.1/HM.5.2 (implementation) |
| Compression tie-break by descending `RelativeOrder` alone | Explicit `CompressionPriority` field | HM.5.2A |
| Naive/unconditional linear peak extrapolation | `ResolvedPeakReferenceIsSelectedCeiling` opt-in cap | HM.5.3 |
| Chained taper (single scalar applied twice) | Fixed-anchor, non-chained, two distinct per-week multipliers | HM.1.4A (diagnosis) → HM.1.4B (fix) |
| HM.3's finding "RaceSpecific's true floor is provably 5" | Corrected: true canonical floor is 3 (a consequence of then-current point-valued bounds, not a true minimum) | HM.4 |
| HM.5's finding "the whole Layer B stage-allocator effort is blocked" | Narrowed: exactly one field (`BUILD_FASTER_THAN_HM_INTRO.minimumExposures`) was the actual blocker | HM.5.1 |
| 4D-only hardcoded volume/identity dispatch | Typed per-frequency dispatchers (3D/4D/5D, later Beginner/Advanced siblings) | HM.6/HM.8 (3D/5D), HM.14 (Beginner), HM.16 (Advanced) |
| TEN_K-hardcoded public horizon gate (8/12/14 for every distance) | `RaceHorizonPolicy.DecideForDistance`, HM's own real 10/14/16 bounds | HM.18 |
| Historical numeric candidates never adopted (see §28) | Real frozen authority per level/frequency | HM.7/HM.10/HM.13/HM.15 |

These were legitimate intermediate states at the time, not errors to be embarrassed about — this register exists so a future reader doesn't mistake an early report's own numbers for current truth.

---

## 28. Historical candidate register (NOT active authority — do not promote these)

The following values appear in older handoff/research material (`HM_21K_Claude_Handoff_and_Build_Plan.md` and early phase reports) but were either superseded or never became live authority. **They are historical/research candidates only.**

- **`Experienced` level**: the backend `RunningBackground` enum has a 4th value with **no catalog mapping anywhere** for either HM or 10K — real, confirmed taxonomy drift (HM.12), not active authority for anything.
- **Beginner×5D candidate peak-volume band** ([32,44] km/week, from the original handoff table): never frozen; the cell itself is `PRODUCT_INELIGIBLE` (HM.13 §6) — this number is moot, not merely unfrozen.
- **HM 2D/6D candidate volume/LR bands**: mentioned in early planning material as eventual expansion targets; no numeric authority was ever frozen for either.
- **7–9W "compressed" and ≤6W "readiness-only" thresholds**: named as future product tiers in the original handoff document; no authority exists for HM's own version of either.
- **Advanced absolute-peak-LR "candidate" ~20.9–22.5km (B.A.A. Level Three citation)**: real, already-accepted evidence (via HM.1.1's own citation set), explicitly considered and explicitly **not adopted** at HM.15 (user decision: keep 19.0km, unchanged from Intermediate) — this is a considered-and-rejected candidate, not an oversight.
- **8.0/12.0/15.0 km "starting long-run cap" candidates for Beginner/Intermediate/Advanced** (original handoff table): only Intermediate 5D's own 12.0km was ever actually frozen as real authority; the Beginner 8.0km and Advanced 15.0km candidates were each explicitly examined and explicitly **not promoted** (HM.13 §14, HM.15 §17 — no feasibility trigger was found to justify adopting them).

---

## 29–32. Post-V1 expansion tracks

These are **new product expansions**, not continuations of unfinished V1 work. None of their authorities are resolved by this document. Do not begin implementation on any of these without a new, dedicated authority phase.

### HM-X1 — 6D Expansion

**Status: CLOSED — publicly activated.** Resolved across four phases: HM-X1.1 (eligibility/architecture audit), HM-X1.2 (structural/numeric authority closure), HM-X1.3 (10-16W dark implementation), HM-X1.4 (public activation and end-to-end proof). Intermediate×6D and Advanced×6D are now `PUBLIC`, exactly as this section originally anticipated (Beginner was correctly not assumed eligible, and remains out of scope — HM-X1 never evaluated or froze Beginner×6D authority). See `PHASE_HM_X1_4_6D_INTERMEDIATE_ADVANCED_PUBLIC_READINESS_CONTROLLED_ACTIVATION_AND_END_TO_END_PROOF.md` for the final public matrix and end-to-end proof. The paragraph below is retained as the original pre-work framing, for historical continuity.

**Likely product-eligible levels**: Intermediate and Advanced (do not assume Beginner — 10K's own real precedent never gave Beginner a 5D or 6D cell at all, and this same reasoning underpinned Beginner×5D's own `PRODUCT_INELIGIBLE` decision).

**Must audit before any decision**: `RUN_LAYOUT_6D`'s real slot structure; true-hard count and any secondary/tertiary quality-role structure it implies; the same-frequency 10K precedent (10K already has real, shipped Intermediate/Advanced 6D cells — mechanism precedent only, never numeric authority, per this engagement's own established discipline); HM's own peak-volume band, selected peak, absolute weekly cap, calibration start, LR shares, absolute LR ceiling, and taper multipliers for each eligible level; session concentration and calendar spacing feasibility. Determine whether the existing Advanced/Intermediate progression content can represent 6D without inventing new training families.

### HM-X2 — 7–9W Compressed Core

**Recorded existing product direction**: 7–9W is `COMPRESSED`, historically named, but **not active HM V1 authority of any kind**. Future work must resolve: minimum readiness threshold, volume starting point, stage compression behavior at this shorter horizon, whether `RACE_SPECIFIC_HM_REHEARSAL`'s protection remains appropriate, long-run progression feasibility at compressed length, goal feasibility, whether all levels/frequencies even qualify for a compressed variant, and the typed product semantics for it. **Do not reuse Standard Core's own numbers blindly** — a compressed horizon is a genuinely different product shape, not merely a truncated Standard Core.

### HM-X3 — >16W Preparation Runway / LongHorizon

**Current state**: >16W is handled today as Standard Core's own `START_TOO_EARLY` typed rejection (§16), with an advisory `recommendedStartDate` only — no Runway/LongHorizon composition exists for HM. Future expansion may compose a Preparation Runway block ahead of the existing 10–16W Core, or use LongHorizon rolling orchestration. **A future audit must distinguish generic LongHorizon machinery from TEN_K-specific historical orchestration assumptions** (`LongHorizon*.cs` files are confirmed, by direct grep, to contain real `GoalDistance.TenK` literals throughout their own Preparation-Runway-specific logic) — **do not treat 10K's own existing Runway behavior as automatically HM-approved.**

### HM-X4 — 2D Low-Frequency Product

**Do not model HM 2D as "3D minus one EASY session."** It is a separate low-frequency product problem. Future authority must resolve: minimum viable quality-session frequency, long-run share/session-concentration behavior at only 2 sessions/week, hard-stimulus structure, its own peak-volume band, readiness threshold, whether it's a Standard-Core or compressed-horizon variant, calendar semantics, and product warning/positioning. **2D is intentionally lower priority than 6D** in the recommended order below — 6D has real, direct same-distance 10K precedent (Intermediate/Advanced 6D already ship for 10K); 2D's own low-frequency HM product shape has no comparably direct precedent.

**Recommended expansion order** (a recommendation for planning purposes, not canonical training authority):
1. ~~HM-X1 — 6D~~ **CLOSED** (HM-X1.1-X1.4, publicly activated)
2. HM-X2 — 7–9W compressed
3. HM-X3 — >16W Runway/LongHorizon
4. HM-X4 — 2D

---

## Future safety rules (mandatory for every HM-X phase)

1. **Zero-delta obligation, doubled.** Any HM-X phase touching shared generic code must prove zero delta for BOTH the existing public HM V1 matrix (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D) AND the existing live 10K matrix. The existing public HM V1 matrix is now a regression baseline exactly like 10K — treat it with the same rigor.
2. **Catalog pinning discipline.** Post-V1 expansion must not silently repoint an existing HM public combination to a new artifact version unless: (a) the change is intentionally meant to change existing V1 behavior, (b) a new authority phase explicitly approves it, and (c) zero-delta or intended-delta proof is documented. Prefer a new combination + new version for new frequency/horizon scope, exactly as HM.11/HM.16 already established.
3. **Dark-first rollout.** New HM-X expansion tracks should begin dark: authority → dark implementation → real materialization → regression → a dedicated public-readiness audit (mirroring HM.17) → controlled activation (mirroring HM.18). Do not make a new HM-X surface public merely because base HM V1 is already public — each new product surface earns its own activation separately.
4. **Beginner×5D stays closed by default.** It must remain intentionally ineligible unless a new authority phase explicitly changes that policy with new evidence.

---

## Full HM phase index (source of record: `PHASE_LEDGER.md`)

| Seq | Phase | Final classification | Commit | Report |
|---|---|---|---|---|
| 1 | HM.0 | `HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE` | 6f5fae5 | `PHASE_HM_0_DISTANCE_GENERALIZATION_REUSE_AUDIT.md` |
| 2 | HM.0.1 | (correction-in-place, confirmed on corrected evidence) | 737c039 | same file |
| 3 | HM.1 | Core/taper-duration/hard-budget frozen; peak-LR + full workout-progression escalated | (decision phase, no commit) | `PHASE_HM_1_CORE_HORIZON_AUTHORITY_CLOSURE_INTERMEDIATE_4D_14W.md` |
| 4 | HM.1.1 | Peak long-run frozen 19.0km; HM.1 fully closed | 4d85703 | `PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md` |
| 5 | HM.1.2 | Stage catalog built; exposure counts blocked | (no commit, blocked) | `PHASE_HM_1_2_WORKOUT_PROGRESSION_STAGE_CATALOG_CLOSURE.md` |
| 6 | HM.1.3 | Exposure counts frozen; HM.1.2 closed | 2c281a3 | `PHASE_HM_1_3_WORKOUT_PROGRESSION_EXPOSURE_COUNT_AUTHORITY_DECISION_CLOSURE.md` |
| 7 | HM.2 (prerequisites/identity) | Identity widening done; volume authority still blocked | 0bc0f3b | `PHASE_HM_2_CORE_VERTICAL_SLICE_PREREQUISITES_AND_IDENTITY_WIDENING.md` |
| 8 | HM.1.4 | 3 fields frozen, 10 genuine gaps disclosed | dadea15 | `PHASE_HM_1_4_VOLUME_SAFETY_POLICY_AUTHORITY_FIELD_CLASSIFICATION.md` |
| 9 | HM.1.5 | 5 more fields frozen; LR-share blocked on mechanism gap | b5c3918 | `PHASE_HM_1_5_VOLUME_SAFETY_POLICY_PARTIAL_FREEZE_LONG_RUN_SHARE_SEMANTICS_GAP.md` |
| 10 | HM.1.4A | Compounding taper defect diagnosed (audit only) | b67dee8 | `PHASE_HM_1_4A_TAPER_VOLUME_MULTIPLIER_TWO_WEEK_TAPER_MECHANISM_AUDIT.md` |
| 11 | HM.1.4B | Non-chained taper fixed; 0.70/0.43 frozen | a51fbd9 | `PHASE_HM_1_4B_TAPER_VOLUME_MULTIPLIER_NON_CHAINED_MECHANISM_FIX_AND_TWO_WEEK_TAPER_FREEZE.md` |
| 12 | HM.1.6 | LR-share peak-week ramp mechanism closed (mechanism only, not numbers) | 9beebc6 | `PHASE_HM_1_6_LONG_RUN_SHARE_PEAK_WEEK_MECHANISM_CLOSURE.md` |
| 13 | HM.2 (full-slice) | `HM_INTERMEDIATE_4D_14W_FULL_DARK_VERTICAL_SLICE_COMPLETE` | 4fdbc2a | `PHASE_HM_2_INTERMEDIATE_4D_14W_FULL_DARK_VERTICAL_SLICE.md` |
| 14 | HM.2.1 | CoreCycle corrected 14/14/14→10/14/16; regression closed | 1cc7a40 | `PHASE_HM_2_1_HM_MASTER_AUTHORITY_AND_REGRESSION_CLOSURE.md` |
| 15 | HM.3 | 10–16W allocation authority blocked (no headroom authored) | e2c75eb | `PHASE_HM_3_TEN_TO_SIXTEEN_WEEK_DYNAMIC_CORE_PHASE_ALLOCATION_AUTHORITY_CLOSURE.md` |
| 16 | HM.4 | Phase/stage headroom authority closed (design only) | 9377725 | `PHASE_HM_4_PHASE_AND_STAGE_NUMERIC_HEADROOM_AUTHORITY_CLOSURE.md` |
| 17 | HM.5 | Catalog headroom implementation blocked (stage allocator gap found) | b558e68 | `PHASE_HM_5_TEN_TO_SIXTEEN_WEEK_CATALOG_HEADROOM_IMPLEMENTATION.md` |
| 18 | HM.5.1 | Gap narrowed to one field; Layer A implemented; new volume blockers found | 4359dce | `PHASE_HM_5_1_TEN_TO_SIXTEEN_WEEK_CATALOG_HEADROOM_COMPLETION_AND_VOLUME_BOUNDARY_AUDIT.md` |
| 19 | HM.5.2 | `PreferredExposures` mechanism closed | 615ddd0 | `PHASE_HM_5_2_PROGRESSION_STAGE_ALLOCATOR_OPTIONAL_PREFERRED_SEMANTIC_CLOSURE.md` |
| 20 | HM.5.2A | `CompressionPriority` mechanism closed; Build occupancy matches HM.4 exactly | 10f615d | `PHASE_HM_5_2A_STAGE_COMPRESSION_PRIORITY_SEMANTIC_CLOSURE.md` |
| 21 | HM.5.3 | Volume/LR boundary defects (16W overshoot, 12W/15W cap overflow) fixed | 2c2ec5e | `PHASE_HM_5_3_VOLUME_AND_LONG_RUN_BOUNDARY_SEMANTIC_CLOSURE.md` |
| 22 | HM.6 | 3D/5D frequency-expansion audit; blocked pending numeric authority | 34191d3 | `PHASE_HM_6_INTERMEDIATE_3D_5D_FREQUENCY_EXPANSION_AUTHORITY_AND_REUSE_AUDIT.md` |
| 23 | HM.7 | Intermediate 3D numeric authority closed | a1f82e5 | `PHASE_HM_7_INTERMEDIATE_3D_NUMERIC_AUTHORITY_CLOSURE.md` |
| 24 | HM.8 | Intermediate 3D 10–16W full dark implementation complete | 916181e | `PHASE_HM_8_INTERMEDIATE_3D_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md` |
| 25 | HM.9 | Intermediate 5D second-KEY-lane structural authority closed | fdfbc5f | `PHASE_HM_9_INTERMEDIATE_5D_SECOND_KEY_LANE_AUTHORITY_CLOSURE.md` |
| 26 | HM.10 | Intermediate 5D numeric authority closed | d8e8d36 | `PHASE_HM_10_INTERMEDIATE_5D_NUMERIC_AUTHORITY_CLOSURE.md` |
| 27 | HM.11 | Intermediate 5D 10–16W full dark implementation complete | e98221c | `PHASE_HM_11_INTERMEDIATE_5D_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md` |
| 28 | HM.12 | Beginner/Advanced level-expansion authority and reuse audit complete | 6e029f7 | `PHASE_HM_12_BEGINNER_ADVANCED_LEVEL_EXPANSION_AUTHORITY_AND_REUSE_AUDIT.md` |
| 29 | HM.13 | Beginner 3D/4D/5D numeric authority closed (5D `PRODUCT_INELIGIBLE`) | 0436e8f | `PHASE_HM_13_BEGINNER_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md` |
| 30 | HM.14 | Beginner 3D/4D 10–16W full dark implementation complete | 491c88b | `PHASE_HM_14_BEGINNER_3D_4D_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md` |
| 31 | HM.15 | Advanced 3D/4D/5D numeric authority closed | 8ca8ab2 | `PHASE_HM_15_ADVANCED_3D_4D_5D_NUMERIC_AND_LEVEL_AUTHORITY_CLOSURE.md` |
| 32 | HM.16 | Advanced 3D/4D/5D 10–16W full dark implementation complete | 8b73e74 | `PHASE_HM_16_ADVANCED_3D_4D_5D_TEN_TO_SIXTEEN_WEEK_FULL_DARK_IMPLEMENTATION.md` |
| 33 | HM.17 | V1 public scope and activation readiness closed (4 blockers identified) | f37a8f5 | `PHASE_HM_17_V1_PUBLIC_SCOPE_AND_ACTIVATION_READINESS_CLOSURE.md` |
| 34 | HM.18 | **V1 Standard Core public activation complete** | a2f4f1d | `PHASE_HM_18_V1_PUBLIC_CONTRACT_CONFIRMATION_LIFECYCLE_AND_CONTROLLED_ACTIVATION.md` |

**HM.2 correctly has two distinct entries** (rows 7 and 13) — the prerequisites/identity-widening phase and the later full 14W dark vertical slice are separate, sequential pieces of work, not one collapsed event.

See `PHASE_HM_19_V1_FINAL_CLOSURE_AUTHORITY_INDEX_AND_EXPANSION_HANDOFF.md` for this document's own governance record.
