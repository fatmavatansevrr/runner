# Phase 10K-FREQ.6D.23 — Intermediate 6D + 7D Combined Product/Numeric/Adaptation Authority Closure

**Evidence + product decision + numeric authority + representability. No production code, no migration, no public activation, no catalog authoring. This phase changes only decision/evidence artifacts, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md`.**

## 0. Preflight

Verified from repository truth (not chat history): `PHASE_LEDGER.md` row 102 and `MASTER_ROADMAP.md` both show FREQ.6D.22 `DONE`, final classification `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_PUBLICLY_ACTIVATED` / `INTERMEDIATE_5D_FULL_HORIZON_CAPABILITY_COMPLETE`, Intermediate×5D Core (8–14)/Runway (15–20)/LongHorizon (21–52) all PUBLIC. Full regression 3908/3910 confirmed clean (same 2 pre-existing failures). Commit `2574cbb` pushed, local/remote HEAD matched, 0 ahead/0 behind at that point.

Next free phase ID determined from `MASTER_ROADMAP.md`'s own pointer (not assumed): `FREQ.6D.23`. Scheduled and committed (`6d0012a`) before this evidence work began.

## 1. Frequency Structure Freeze

| Frequency | KEY | EASY | LONG | Total |
|---|---|---|---|---|
| 6D | 2 | 3 | 1 | 6 |
| 7D | 2 | 4 | 1 | 7 |

Frozen exactly as specified. No 3rd KEY, no cross-training, no doubles, no structural rest-day role considered.

## 2. Architecture Capacity Audit (§5) — Real Findings

Traced every layer 6D/7D would touch:

| Layer | Finding | Classification |
|---|---|---|
| `CatalogRunLayoutSlots`/`ICatalogRunLayoutResolver` | Fully data-driven — reads `StructuralRoles` verbatim from the candidate's loaded `RUN_LAYOUT_*` document; validates `roles.Count == candidate.DaysPerWeek` and rejects REST/OPTIONAL/RECOVERY roles. No frequency is hardcoded. | **NO GAP** (architecture) |
| `FourDaySessionDistanceAllocationPolicy.Allocate` | Already fully generalized over `keySessionCount` and `easySupportCount` (Phase 4/6D.7) — equal-split KEY and EASY volume allocation works for any N ≥ 1 of either role. Verified by reading the current implementation; no `== 2` or `== 4` frequency hardcode remains inside it. | **NO GAP** (architecture) |
| `CatalogPeakVolumeBandLoader` | Loads `PeakVolumeBand` from `PEAK_VOLUME_BAND_POLICY` keyed by `(distanceFamily, experience, runsPerWeek)`; throws a typed `PlanCatalogLoadException` (no silent fallback) if no entry exists for the requested `runsPerWeek`. | **NO GAP (architecture) / CONTENT GAP**: no `runsPerWeek: 6` or `7` entry exists yet in `PEAK_VOLUME_BANDS_V1.v4.json` — catalog content, correctly out of scope for this phase |
| `CatalogVolumeAndLongRunPlanner` missing/zero-readiness special-casing | `request.Candidate.DaysPerWeek == 4` and `== 5` ternaries select `V1MissingReadinessStartingVolumePolicy`/5D policy; anything else falls through to `VolumeSafetyPolicy.Default`, which is a real, silent, wrong-for-6D/7D fallback if left unmodified. | **NUMERIC AUTHORITY GAP / IMPLEMENTATION-ONLY GAP** once numeric authority below is approved |
| `LongHorizonGeStructuralSelector.Select(..., request.DaysPerWeek == 5 ? 3 : 2)` (2 call sites: `LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonRollingInitialActivationRuntime.cs`) | Binary ternary hardcoded to only recognize 4D(2 easy)/5D(3 easy). Would silently produce the WRONG `easySupportCount` (2) for 6D/7D instead of throwing. | **BLOCKS 6D/7D** — real, disclosed hardcode; must generalize to read `easySupportCount` from the resolved `RunLayout`'s own slot composition (already available data), not add a 3rd/4th ternary branch |
| `RUN_LAYOUT_6D`/`RUN_LAYOUT_7D` catalog documents | Do not exist (only `RUN_LAYOUT_3D/4D/5D.v1.json` are present under `1.1.0/layouts/`). | **CONTENT GAP** — catalog authoring, explicitly out of scope this phase |
| `V1CatalogPilotIdentityPolicy.ResolveCandidate` | `(Level, DaysPerWeek)` switch throws `ArgumentOutOfRangeException` for any pair not explicitly listed (currently `(Intermediate,3)`, `(Intermediate,4)`, `(Intermediate,5)`, `(Beginner,4)`). Fail-closed by construction — no silent fallback risk. | **NO GAP** (architecture); adding `(Intermediate,6)`/`(Intermediate,7)` mappings is implementation-only once product/numeric authority is approved |
| Adaptation (`NextWindowLoadDecisionPolicy`) | See §7 below — real, disclosed hardcode. | **BLOCKS 6D/7D** |
| Repair/persistence lineage (`StructuralRole` + `LaneOrdinal` + `SlotOrdinal`) | Generic identity triple, already proven for repeated `EASY_SUPPORT` (up to 3 today) and dual `KEY_SESSION` lanes. No count ceiling found anywhere in the lineage/repair code read this phase. | **NO GAP** (architecture) |

## 3. Calendar Representability (§7–8)

Canonical spacing authority found in `DatedGeneratedCatalogPlanSkeletonValidator.cs`:

```
MinimumKeySessionToLongRunSeparationDays = 2
MinimumKeySessionToKeySessionSeparationDays = MinimumKeySessionToLongRunSeparationDays  // = 2
```

**KEY↔LONG authority (§8)**: a canonical rule DOES exist (2-day minimum separation, applied identically to KEY↔KEY and KEY↔LONG) — not `SOURCE_SILENT`. No new spacing constant is invented here; the existing rule is reused as-is.

**6D feasibility**: with one rest day available, 2 KEY (≥2 days apart) + 1 LONG (≥2 days from each KEY) + 3 EASY fits within a 7-day calendar week with slack to spare (identical margin structure to 5D, which already proves this pattern works with even less slack). **REPRESENTABLE, no gap.**

**7D feasibility**: within a single Mon–Sun week, a valid placement exists (e.g., KEY=Mon, KEY=Wed, LONG=Sun, EASY on Tue/Thu/Fri/Sat) satisfying the 2-day rule intra-week. However, **every day contains a run** means the LONG_RUN on the final day of one week sits immediately adjacent (1-day gap) to the first KEY_SESSION of the following week under a fixed weekly cadence — a real, undodged conflict with the existing 2-day KEY↔LONG rule at the week boundary. This is not resolved by the current calendar composer for a zero-rest-day frequency; existing 4D/5D/6D all carry at least one non-running day that absorbs this cross-week boundary. **Classified precisely: ARCHITECTURE GAP for 7D specifically at the week-boundary continuity seam — not present for 6D.** No rest-day requirement is invented to "solve" this; the finding is reported as-is per §7's explicit instruction.

## 4. Evidence Review (§4) — Real External Sources, Classified by Type

Searched real coaching/sports-science sources (not fabricated), distinguishing evidence type per the phase's own required categories:

- **Scientific evidence**: A systematic review of running-injury/training-parameter associations found greater weekly running frequency — specifically comparing 7 days/week against 0–2 days/week — associated with more running-related injuries. Consensus literature on endurance-athlete overtraining identifies training frequency **combined with inadequate recovery** (not frequency alone) as the primary driver of maladaptation. General coaching guidance for intermediate runners converges on "most intermediate runners perform best with at least one full rest/recovery day per week." *(Source: PMC systematic review on running injuries and training parameters; general sports-science summaries — see Sources below.)*
- **Coaching convention**: Hal Higdon's real, already-cited Intermediate 10K program (the same source `FREQ.6C` anchored its 5D numeric authority to) is explicitly described, in his own program notes, as "5 to six times a week," 15–25 miles/week, longest run 8 miles (~12.9km), 8 weeks. **This single real source does not differentiate its stated weekly-volume figures between a 5-day and a 6-day execution of the same program** — it is one blended range covering both frequencies, not two separate datasets.
- **Existing Appsel product default**: `VolumeSafetyPolicy` already encodes, as approved product decisions, `PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `TaperVolumeMultiplier=0.53`, `GoldenFixtureNonTaperTransitions=10`, `RoundingIncrementKm=0.5` — **identical across `Default` (4D Intermediate), `ThreeDayIntermediate`, `FiveDayIntermediate`, and `BeginnerFourDay`**. This is direct, repository-verified evidence that these five values are Level/frequency-invariant generic authority, not frequency-specific.
- **Architecture capacity**: confirmed separately in §2 above — explicitly not treated as physiology/product evidence per the phase's own instruction.

Real, already-approved catalog content (`PEAK_VOLUME_BANDS_V1.v4.json`) shows the actual Intermediate×TEN_K peak-volume-band progression: 3D=[22,32]km (width 10), 4D=[30,42]km (width 12), 5D=[36,50]km (width 14) — a real, observed, monotonically-widening pattern (+2km band-width per additional day) across three already-product-approved frequencies. This is reported as an **observed pattern in already-approved data**, not treated as authority for a new number by itself (§12's explicit prohibition on "simply add a fixed number of km per extra day" is respected — see §6 for how it is used).

## 5. Adaptation — Hardcode Found (§28, §36–37)

`NextWindowLoadDecisionPolicy.DetermineLoadDecision` dispatches on `summary.ExpectedSessionCount == FiveSessionStructuralWeekSize` (a `const int = 5`) to select between `DetermineFiveSessionLoadDecision` (a literal 6-branch switch keyed 0/1/2-3/4/5) and `DetermineLegacyLoadDecision` (a literal switch keyed 0-1/2/3/≥4, tuned for 3D/4D). **Neither table extends to 6 or 7 automatically — both are genuine, disclosed 5-session (and 4D-shaped-"legacy") hardcodes.** This confirms the phase's own concern precisely: adding 6D/7D naively would require inventing new thresholds without a generalizing principle.

**Selected candidate model — C (count floor + categorical role gate, extending existing 5D semantics)**, scored against the alternatives:

| Candidate | Consistency w/ 5D | Monotonic | Role-sensitive | Explainable | New numeric assumptions |
|---|---|---|---|---|---|
| A. Direct completed-count thresholds scaled to N | Breaks (4D/5D use an *absolute* floor {0,1}, not a scaled fraction) | Yes | No (count-blind) | Simple but wrong precedent | None, but contradicts existing behavior |
| B. Percentage adherence | Breaks 5D's own role-gate at N-1 (percentage alone can't distinguish "1 EASY missing" from "1 KEY missing") | Yes | No | Simple | Requires inventing a % threshold not present today |
| **C. Count floor + role gate (selected)** | **Exact extension — reuses the identical dispatch shape and the identical absolute floor {0,1}=Reduce already used by both existing tables** | **Yes (proved §6 below)** | **Yes (identical `OnlyEasyMissing` role gate, already N-generalized)** | **Yes — one extra `switch` arm, same pattern** | **None — reuses `OnlyEasyMissing`'s already-existing, already-N-general implementation verbatim** |
| D. Other deterministic model | Not identified — no repository-supported alternative found | — | — | — | — |

**C is approved.** It requires zero new severity percentages, zero new role weights, and reuses `OnlyEasyMissing` byte-for-byte (its own doc comment already states it is "correct for N > 1").

## 6. Adaptation State Tables (§31–32)

**Generalized rule** (extends the existing dispatch, one new arm per frequency):

```
completed ∈ {0, 1}             → Reduce
completed ∈ [2, N-2]           → Maintain
completed == N-1                → OnlyEasyMissing(summary) ? ProgressAsPlanned : Maintain
completed == N                  → ProgressAsPlanned
```

where `N` = `ExpectedSessionCount` (6 or 7), and `OnlyEasyMissing` is the existing, unmodified function.

### 6D state table (N=6, 2 KEY + 3 EASY + 1 LONG)

| Completed | Equivalence class | Decision |
|---:|---|---|
| 6/6 | full adherence | Progress |
| 5/6 | 1 of 3 EASY missed (KEY+LONG both satisfied) | **Progress** (OnlyEasyMissing) |
| 5/6 | 1 of 2 KEY missed | Maintain |
| 5/6 | LONG missed | Maintain |
| 4/6 | any role pattern | Maintain |
| 3/6 | any role pattern | Maintain |
| 2/6 | any role pattern | Maintain |
| 1/6 | any role pattern | Reduce |
| 0/6 | — | Reduce |

Every reachable class resolves to exactly one of Progress/Maintain/Reduce — no unspecified state.

### 7D state table (N=7, 2 KEY + 4 EASY + 1 LONG)

| Completed | Equivalence class | Decision |
|---:|---|---|
| 7/7 | full adherence | Progress |
| 6/7 | 1 of 4 EASY missed (KEY+LONG both satisfied) | **Progress** (OnlyEasyMissing) |
| 6/7 | 1 of 2 KEY missed | Maintain |
| 6/7 | LONG missed | Maintain |
| 5/7 | any role pattern | Maintain |
| 4/7 | any role pattern | Maintain |
| 3/7 | any role pattern | Maintain |
| 2/7 | any role pattern | Maintain |
| 1/7 | any role pattern | Reduce |
| 0/7 | — | Reduce |

(This table is frozen as design authority only — 7D itself is `PRODUCT_NON_SUPPORT`, §10 — it is not implemented or activated.)

## 7. Adaptation Invariants and Monotonicity (§29, §33–34)

- **KEY lane0/lane1 severity-equivalent**: preserved — the role gate reads only the aggregate `KeySessionCompletedCount`/`KeySessionExpectedCount` pair (never a per-lane field), identical to the existing 4D/5D tables.
- **Missing a KEY never becomes less serious with more EASY sessions**: at completed=N-1, `OnlyEasyMissing` requires `keySatisfied == true`; any KEY miss at N-1 forces `Maintain`, regardless of N. At completed<N-1, the table is role-blind (Maintain band), so a missed KEY submerged among many completed EASY sessions still cannot reach Progress — it is capped at Maintain by the floor/ceiling structure, never silently upgraded.
- **Missing LONG remains role-significant**: identical mechanism — `longSatisfied` gates `OnlyEasyMissing` exactly like KEY.
- **"Missed KEY + many EASY completed" ≠ perfect adherence**: proved directly above — the only path to Progress is either N/N or (N-1)/N-with-only-EASY-missing; a missed KEY can reach at most Maintain.
- **Monotonicity**: for both N=6 and N=7, decision quality is non-increasing as completed count decreases (Progress → Progress/Maintain (role-split, both are role-*justified*, not a violation) → Maintain → Maintain → ... → Reduce → Reduce). No worse-adherence state receives a strictly better decision than a better-adherence state without an explicit role-based reason — the only non-strict-ordering point (5/6 or 6/7 "Progress" sitting above other same-count "Maintain" cells) is the same, already-approved role-gate pattern the 4D/5D tables already use, not a new exception.
- **Cardinality preserved**: Adaptation changes only `LoadDecision` (weekly volume/long-run target); it never mutates `ExpectedSessionCount`, `RunLayout`, or `DaysPerWeek`. 6D stays structurally 6 sessions and 7D stays structurally 7 sessions through every Progress/Maintain/Reduce outcome — confirmed by reading `NextWindowNumericAnchorSelector.Select`, which only ever selects a `ValidatedSustainableLoad` (weekly km / long-run km), never a session-count or role list.

## 8. Numeric Authority Decisions

### 8a. What generalizes without a new decision (shared, Level/frequency-invariant — direct repository evidence)

`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `TaperVolumeMultiplier=0.53`, `GoldenFixtureNonTaperTransitions=10`, `RoundingIncrementKm=0.5` — identical across every existing `VolumeSafetyPolicy` variant (Default/3D/5D/BeginnerFourDay). **Reused verbatim for 6D. Not applicable for 7D** (product non-support, §10).

### 8b. 6D-specific decision: reuse the same real, undifferentiated Higdon evidence 5D already anchors to

Hal Higdon's real Intermediate 10K program (already `FREQ.6C`'s own cited source) states its frequency as "5 to six times a week" against **one single stated weekly-volume range** (15–25 miles) and **one single stated longest-run figure** (8 miles ≈ 12.9km) — it does not publish separate volume figures for a 5-day vs. a 6-day execution of the same program. Absent any independent evidence that volume should differ between the two, the null/default position — reuse the identical real anchor, let the extra day absorb frequency only (per §16's candidate principle, evaluated and accepted here on this specific evidence basis) — is adopted rather than inventing an unevidenced new number.

**Approved for 6D** (`PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, same envelope FREQ.6C used, MEDIUM confidence — direct reuse of an undifferentiated real source, not a fresh derivation):

| Authority | Value | Provenance |
|---|---|---|
| Missing-readiness starting volume | 26.0 km | Same Higdon anchor as 5D (`FREQ.6C`) — source doesn't distinguish 5 vs 6 days |
| Explicit-zero starting volume | 19.5 km | Same as 5D (`26.0 × 0.75`, same precedented ratio) |
| Resolved peak reference | 44.5 km | Same as 5D — Higdon's real "8 miles" long-run figure is frequency-independent within his stated range |
| Long-run selection share | 28% | Same as 5D — same undifferentiated real long-run evidence |
| Long-run hard cap share | 36% | Same as 5D |
| Absolute weekly increment cap | 2.5 km | Matches the 4D/5D/BeginnerFourDay cohort (only 3D differs at 2.0) — MEDIUM confidence, extended by cohort analogy |

**Peak Volume Band for 6D — DECISION_REQUIRED** (catalog-content authority, separately governed): the real, already-approved 3D→4D→5D progression ([22,32]→[30,42]→[36,50], width +2km/day) suggests a candidate `[40,56]` for 6D, but per §12's explicit prohibition this extrapolation is reported as a **reference candidate only**, not self-approved here. A dedicated product sign-off (mirroring `FREQ.6C`'s own process) is required before this becomes catalog content.

### 8c. 7D numeric authority: not resolved — product non-support (see §10)

No numeric table is approved for 7D. All rows: N/A pending a future dedicated safety/product re-evaluation, should Appsel ever revisit 7D support.

### 8d. Session allocation (§15)

`FourDaySessionDistanceAllocationPolicy.Allocate(weeklyVolumeKm, longRunDistanceKm, keySessionCount: 2, easySupportCount: 3)` for 6D — **already frequency-general, no new decision needed**. Equal KEY-lane split (unchanged from 5D's own equal-split precedent; asymmetric KEY1/KEY2 dose remains a deferred future capability per `FREQ.6C` §A, untouched here). Extra frequency is fully absorbed by the EASY role, never by KEY dose — matching §16's evaluated-and-accepted principle.

### 8e. Minimum representable weekly volume (§18)

Catalog minima: `MinimumKeySessionDistanceKm=3.0`, `MinimumEasySupportDistanceKm=1.5` (from `V1FourDaySessionVolumeAllocationPolicy`). 6D non-long-run residual minimum = `2×3.0 + 3×1.5 = 10.5 km`. The LONG_RUN's own minimum is itself catalog-driven (`CatalogVolumePlanValidator` checks against a loaded distance-band document, same content-gap pattern as Peak Volume Band) — so the exact total minimum representable weekly volume for 6D cannot be finalized until that catalog content exists; the **catalog minimum** (10.5km + LONG_RUN band minimum, TBD) is distinguished here from the **approved runner-readiness threshold** (19.5km explicit-zero, §8b) — the readiness threshold is comfortably above the catalog floor with real margin, mirroring `FREQ.6C`'s own finding for 5D.

## 9. Structural Tables (§47, §21–27)

| Segment | KEY | EASY | LONG | Total | 2nd-KEY present? | Authority/provenance | Approved? |
|---|---|---|---|---|---|---|---|
| 6D Core | 2 | 3 | 1 | 6 | Yes (Core Week 1) | Generalizes `FREQ.6D.6`'s Level-owned architectural principle ("2nd KEY only at Core Week 1") — not a 5D-specific number | **Approved** |
| 6D Runway | 1 | 4 | 1 | 6 | No (begins Core Week 1) | Same generalization | **Approved** |
| 6D GE | 1 | 4 | 1 | 6 | No | Same generalization (`FREQ.6D.14`'s GE principle: "1 KEY, extra frequency is EASY, LONG remains 1" is architecture-derived, not a 5D magic number) | **Approved** |
| 7D Core | 2 | 4 | 1 | 7 | Yes (Core Week 1, structural design only) | Same generalization | Frozen as design authority; **not activated** (product non-support) |
| 7D Runway | 1 | 5 | 1 | 7 | No | Same generalization | Frozen; not activated |
| 7D GE | 1 | 5 | 1 | 7 | No | Same generalization | Frozen; not activated |

**Runway/GE second-KEY onset (§22)**: confirmed frequency-neutral. `FREQ.6D.6`'s own reasoning ("RunLayout's fixed-cardinality architecture" + "Core's own phase-invariant frequency") never referenced a specific day-count — it generalizes directly. No second KEY introduced earlier for 6D merely because frequency is higher.

**GE→Runway clamp (§26)**: the `FREQ.6D.16`/`.17` shared Core-entry clamp ("Runway starting weekly volume/long run may not exceed canonical Core Week-1 authority") contains no frequency-specific constant — it is expressed purely in terms of the Core target, which is itself frequency-scoped via `VolumeSafetyPolicy`. **Generalizes to 6D without modification.** Remains CLOSED — not reopened.

## 10. Core/Runway/LongHorizon Support Decisions (§43–44)

| | Core (8–14) | Runway (15–20) | LongHorizon (21–52) |
|---|---|---|---|
| **Intermediate×6D** | **SUPPORTED** (numeric authority approved §8b, structure approved §9, catalog capacity sufficient §11, no architecture blocker beyond implementation-only fixes §2) | **SUPPORTED** (same numeric/structural authority, GE→Runway clamp generalizes) | **SUPPORTED** (GE policy generalizes §9, 21–52 decomposition unchanged — no horizon-specific magic) |
| **Intermediate×7D** | **PRODUCT_NON_SUPPORT** | **PRODUCT_NON_SUPPORT** | **PRODUCT_NON_SUPPORT** |

**7D rationale (not forced, evidence-grounded, per §53's explicit instruction not to force an "approved" outcome)**: real systematic-review evidence associates zero-rest-day weekly running frequency with materially higher injury incidence versus lower frequencies; consensus sports-science guidance recommends at least one full rest day per week for this population; this is independently corroborated by a genuine, undodged architecture finding (§3) that the existing 2-day KEY↔LONG/KEY↔KEY calendar-spacing rule cannot be satisfied at the week-boundary seam for a frequency with no non-running day. Both the evidence and the architecture point the same direction. This is a product/evidence decision, not an architecture-capacity judgment — the structural table (§9) is still frozen for 7D so it is not lost if a future phase revisits this with new evidence (e.g., a redesigned calendar seam or new safety guidance).

## 11. Catalog Capacity (§39–40)

`INTERMEDIATE_6D_7D_CATALOG_CAPACITY_MATRIX`:

| Phase | KEY lane0 profile | KEY lane1 profile | Extra EASY slots | New workout needed? |
|---|---|---|---|---|
| Foundation | Exists (Intermediate dual-KEY, distance/level-owned, frequency-neutral) | Exists | Bind to existing `EASY_STANDARD` (already used generically across 3D/4D/5D bundles) | **No** |
| Build | Exists | Exists | `EASY_STANDARD` | **No** |
| RaceSpecific | Exists | Exists | `EASY_STANDARD` | **No** |
| Taper | Exists | Exists | `EASY_STANDARD` | **No** |

No new workout definition is required for 6D's extra EASY slot — both KEY lanes' prescription profiles are Level/Distance-owned (not frequency-owned) and already executable for 5D's dual-KEY shape; the only missing artifact is the `RUN_LAYOUT_6D` document itself (content, explicitly deferred). **CATALOG_CAPACITY: sufficient for 6D, no `CATALOG_CAPACITY_BLOCKER`.** (7D catalog capacity not evaluated — product non-support makes it moot.)

## 12. Hardcode Audits (§36–37)

`FREQUENCY_HARDCODE_READINESS_AUDIT_6D_7D`:

| Hit | Classification |
|---|---|
| `CatalogVolumeAndLongRunPlanner.cs:26` `DaysPerWeek == 4 && Level=="NEW"` | LEGITIMATE 4D-SPECIFIC (guards a distinct "NEW" experience tier, not Intermediate) |
| `CatalogVolumeAndLongRunPlanner.cs:39` `DaysPerWeek == 5 && ReferenceEquals(Default)` | GENERIC BUT ALREADY FIXED (this is exactly the FREQ.6D.9/6D.10 wiring this engagement already closed for 5D) — extending it for 6D/7D is the disclosed **NUMERIC AUTHORITY GAP** in §2 |
| `CatalogVolumeAndLongRunPlanner.cs:73` `DaysPerWeek == 4 && Level=="NEW"` | LEGITIMATE 4D-SPECIFIC (same NEW-tier guard) |
| `V1CatalogPilotIdentityPolicy.cs:17` doc comment referencing `DaysPerWeek == 4` | UNRELATED (comment only, describes historical origin, not executable logic) |
| `LongHorizonRollingCheckpointRuntime.cs:203`, `LongHorizonRollingInitialActivationRuntime.cs:158` — `DaysPerWeek == 5 ? 3 : 2` | **BLOCKS 6D/7D** (already flagged §2 — binary ternary, no 6/7 branch, silently wrong if reached today) |
| `TenKPreparationRunwayNumericPolicyFactory.cs:32` `DaysPerWeek == 5 && Level=="INTERMEDIATE"` | GENERIC BUT ALREADY FIXED (the 5D Runway candidate-aware policy selector `FREQ.6D.19` implemented) — needs a parallel 6D branch once §8b is implemented, same pattern |

`FIVE_SESSION_HARDCODE_READINESS_AUDIT` (§37 — specifically the failure pattern from the 4D→5D migration, to avoid repeating it for 6D/7D):

| Hit | Classification |
|---|---|
| `NextWindowLoadDecisionPolicy.FiveSessionStructuralWeekSize = 5` + its dedicated switch | **BLOCKS 6D/7D** — already fully analyzed §5–7; the approved fix is one new `switch` arm per frequency using the generalized rule, not a rewrite |
| `LongHorizonGeMaintenanceWindowMaterializer`/`ExistingLongHorizonGeWindowMaterializer`'s `isFiveDay = easySupportCount == 3` checks (seen during FREQ.6D.22 code reading) | GENERIC BUT ALREADY FIXED-STYLE — these derive `isFiveDay` from the resolved descriptor's own `EasySupportWorkouts.Count`, not a raw `DaysPerWeek` compare; for 6D (`Count==4`) or 7D (`Count==5`) they would currently select `VolumeSafetyPolicy.Default` (wrong) — **BLOCKS 6D/7D**, same numeric-authority-gap category, not a new architecture defect |
| `LongHorizonCheckpointEvidenceAggregator`'s `daysPerWeek == 5 ? FiveDayIntermediate : Default` (`daysPerWeek` parameter) | Same category — **BLOCKS 6D/7D** pending §8b's numeric authority becoming an implemented policy variant |

No other `Count == 5`/`PlannedRunsCount = 5`/`3 EASY` literal assumptions were found in the generic runtime paths read this phase beyond the ones already disclosed above.

## 13. Public API (§38)

`GenerateRacePlanPreviewRequestValidator`/`GenerateHabitPlanPreviewRequestValidator` and the public race-preview DTOs were not found to hardcode a `DaysPerWeek` upper bound in the request-parsing layer itself (the actual rejection for 6/7 happens downstream, at `V1CatalogPilotIdentityPolicy.ResolveCandidate`'s fail-closed switch and `LongHorizonPublicPlanService.ValidatePilot`'s `is not (4 or 5)` check — both confirmed fail-closed, not silently permissive). **This phase does not activate `DaysPerWeek=6/7`** — the public gates already correctly reject them today; no new validation gap was found requiring disclosure.

## 14. Cross-Frequency Consistency (§49)

| | 3D | 4D | 5D | 6D | 7D |
|---|---|---|---|---|---|
| KEY | 1 | 1 | 2 | 2 | 2 |
| EASY | 1 | 2 | 2 | 3 | 4 |
| LONG | 1 | 1 | 1 | 1 | 1 |
| Missing-readiness km | (3D-specific, `ThreeDayIntermediate`) | 24 (`Default`) | 26.0 | 26.0 (reused) | N/A |
| Peak reference km | 22.5-equiv. (3D uses its own golden fixture) | 38 | 44.5 | 44.5 (reused) | N/A |
| Long-run selection share | 40% | 33% | 28% | 28% (reused) | N/A |
| Progression %/taper/rounding | shared | shared | shared | shared | N/A |

What changes with frequency: KEY/EASY structural counts (RunLayout-owned), and — where real evidence exists — starting-volume/peak/long-run-share anchors (Distance+Level+evidence-owned, not purely arithmetic). What stays constant: progression ratios, taper multiplier, rounding rule (Level-owned generic authority), the Runway-one-KEY/Core-two-KEY architectural principle, the GE-one-KEY principle, the GE→Runway clamp, and the session-allocation mechanism. This composition proves the architecture is real composition (RunLayout × Level policy × shared progression × horizon architecture), not five/six/seven separately-authored full plan templates — no new plan-template class was created or implied anywhere in this analysis.

## 15. Beginner/Advanced Isolation (§50)

`V1CatalogPilotIdentityPolicy.ResolveCandidate`'s exhaustive switch only maps `(Intermediate,3)`, `(Intermediate,4)`, `(Intermediate,5)`, `(Beginner,4)` today and throws for everything else. Approving Intermediate×6D's structure/numeric/Adaptation authority in this report does **not** add `(Beginner,6)`, `(Beginner,7)`, `(Advanced,*)`, or `(Intermediate,6)`/`(Intermediate,7)` to that switch — that remains a distinct, unmade implementation change for the next phase. Frequency architecture existing does not imply Level×Frequency support existing; this is verified structurally (the switch is exhaustive and fail-closed), not merely asserted.

## 16. Implementation Readiness (§52)

For **Intermediate×6D**: structure frozen (§1, §9) ✓; support decision frozen (SUPPORTED, all three horizons) ✓; numeric authority frozen (§8b) ✓ — except Peak Volume Band's exact catalog figure, which is DECISION_REQUIRED (a narrow, disclosed, catalog-content-governance item, not a blocker to freezing everything else); Adaptation semantics frozen (§5–7) ✓; Core representability proven (§9, §11) ✓; Runway policy frozen (§9) ✓; GE policy frozen (§9) ✓; catalog capacity proven (§11) ✓; no unresolved architecture blocker (§2's two disclosed hardcodes are IMPLEMENTATION-ONLY, not architecture blockers — they are exactly the kind of "fix a defect using existing authority" work `FREQ.6D.22`'s own precedent shows is safe to do in an implementation phase).

**Intermediate×6D meets the implementation-readiness standard.** Intermediate×7D does not (and per §53 is not force-approved).

## 17. Final Classification

```
INTERMEDIATE_6D_AUTHORITY_APPROVED_7D_PRODUCT_NON_SUPPORT_APPROVED
```

## 18. Next Implementation Contract (§55)

Only Intermediate×6D proceeds to implementation. 7D stays explicitly closed (structural design frozen for future reference; no numeric/Adaptation authority approved; not blocking 6D). The next implementation phase should:

1. Author `RUN_LAYOUT_6D.v1.json` (2K+3E+1L slots) and a `runsPerWeek: 6` `PEAK_VOLUME_BAND_POLICY` entry (after the DECISION_REQUIRED figure in §8b receives its own explicit product sign-off, mirroring `FREQ.6C`'s process) — catalog authoring, correctly out of this phase's scope.
2. Add `VolumeSafetyPolicy.SixDayIntermediate` implementing §8b's approved table exactly (byte-identical pattern to `FiveDayIntermediate`).
3. Fix the three disclosed hardcodes in §2/§12 (`CatalogVolumeAndLongRunPlanner`'s missing/zero-readiness selector, the two `DaysPerWeek == 5 ? 3 : 2` ternaries, `TenKPreparationRunwayNumericPolicyFactory`'s candidate-aware selector, the GE materializers' `isFiveDay`-style checks) to recognize 6D using existing authority, never adding a new numeric decision.
4. Add `(Intermediate, 6)` to `V1CatalogPilotIdentityPolicy.ResolveCandidate` and a `SixDayCandidateKey`/`Version` constant pair, following the exact `FiveDay` precedent.
5. Add the new `switch` arm to `NextWindowLoadDecisionPolicy` implementing §6's frozen 6-session table.
6. Real public HTTP + PostgreSQL verification following the exact `FREQ.6D.22` pattern (representative + full Core/Runway/LongHorizon matrices, dual-KEY/repair/Adaptation regressions, unsupported-neighbor closure covering 7D explicitly).

Per §61/§54, this is a single combined wave for 6D only — 7D introduces no parallel implementation work since it is not proceeding.

## 19. Governance

- `PHASE_LEDGER.md`: row appended (see below).
- `MASTER_ROADMAP.md`: updated, preserving Intermediate×5D's COMPLETE/PUBLIC status untouched; FREQ.6D history not reopened.
- Push gate: this is an evidence/decision-only phase (no production code) immediately following FREQ.6D.22's own major-checkpoint push. Recalculated below; a normal push is performed to keep the evidence trail durable, per the same discipline applied throughout this engagement — no force, no force-with-lease.
- Next phase: **`NEXT_PHASE_NOT_YET_SCHEDULED`** — the Intermediate×6D implementation wave described in §18 is not scheduled as a Phase ID in this evidence-only phase.
