# PHASE 10K-FREQ.6D.12 — Intermediate×5D LongHorizon GE Segment Weekly Structure & Numeric Policy Decision Closure

**Type:** EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION (no production code, no migration, no routing, no catalog authoring)
**Parent phase:** FREQ.6D.11
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Every finding below is re-derived from the current repository state.

---

## 1. Preflight

- `git rev-parse HEAD`: `3076e13` — matches the scheduling baseline exactly.
- `git branch --show-current`: `main`.
- `git rev-list --left-right --count origin/main...HEAD`: `0  16` — matches the scheduling baseline exactly.
- `git status --short`: only pre-existing, unrelated entries (`baseline_tmp`, `plan-catalog/artifacts/audits/*`), preserved untouched.
- `git diff --check`: clean.
- `MASTER_ROADMAP.md` confirmed to schedule exactly `FREQ.6D.12 — INTERMEDIATE×5D LONGHORIZON GE SEGMENT WEEKLY STRUCTURE & NUMERIC POLICY DECISION CLOSURE`, type `EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION`, "scheduled, not started."
- `PHASE_LEDGER.md` row 91 confirmed: `FREQ.6D.11`, `DONE`, `INTERMEDIATE_5D_LONGHORIZON_ARCHITECTURE_APPROVED_PRODUCT_POLICY_REQUIRED`.
- `PHASE_LEDGER.md` row 90 confirmed: `FREQ.6D.10`, `DONE`, `INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED` — the 5D missing/zero numeric production defect is closed, not an active blocker.

Preflight passes. Proceeding.

---

## 2. FREQ.6D.11 architecture input (frozen, verified not to need reopening)

Re-confirmed directly against current source this phase (not carried from memory): `LongHorizonRollingSessionState` still has no `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/profile-pair columns; `LongHorizonRollingJitCompositionOrchestrator.BuildBoundedCoreSelection` still groups by raw `StructuralRole`; no `ExecutionPrescriptionIndex` reference exists under `RuntimeCatalog/Schedule/LongHorizon/`. None of `FREQ.6D.11`'s design decisions (session-identity model, schema, JIT key, `ExecutionPrescriptionIndex` reuse, historical-4D compatibility) required any change to close this phase — all remain frozen exactly as designed.

`LongHorizonCompositionResolver.cs` re-verified this phase: `LongHorizonPreparationRunwayWeeks = 8` (const), `LongHorizonCoreWeeks = 12` (const), `GeneralEnduranceWeeks = availableFullWeeks - 20` for the `LongHorizon` branch (21-52 available full weeks). `FREQ.6D.11`'s decomposition finding holds exactly.

---

## 3. LongHorizon decomposition — full 21-52 GE length matrix

`GEWeeks = TotalWeeks - 20`, `PreparationRunwayWeeks = 8` (fixed), `CoreWeeks = 12` (fixed), for every supported total:

| TotalWeeks | GEWeeks | RunwayWeeks | CoreWeeks |
|---|---|---|---|
| 21 | 1 | 8 | 12 |
| 22 | 2 | 8 | 12 |
| 23 | 3 | 8 | 12 |
| 24 | 4 | 8 | 12 |
| 25 | 5 | 8 | 12 |
| 26 | 6 | 8 | 12 |
| 27 | 7 | 8 | 12 |
| 28 | 8 | 8 | 12 |
| ... | ... | 8 | 12 |
| 40 | 20 | 8 | 12 |
| ... | ... | 8 | 12 |
| 52 | 32 | 8 | 12 |

Confirmed: `GEWeeks` ranges 1-32 across the full supported 21-52 range, matching `LongHorizonGeStructuralSelector.Select`'s own `ArgumentOutOfRangeException` bound (`geWeeks is < 1 or > 32`) exactly — the existing GE structural selector already enforces this identical range as a hard precondition, confirming the decomposition table above is not just arithmetic but matches the real code's own validated domain.

---

## 4. GE canonical objective

Repository-backed, not imposed terminology: **"General Endurance"** (`LongHorizonGeStructuralContracts.LongHorizonGeWeekDescriptor.LongHorizonGeneralEnduranceSegmentType = "LONG_HORIZON_GENERAL_ENDURANCE"`), explicitly distinct from `PreparationRunwayBlockType.GeneralEndurance` (a same-named but architecturally separate concept — the doc comment at line 77 is explicit: "distinct from... Phase 4I.1/4I.2/4I.4/4I.5 governance"). Its five stage families (`Entry`, `BaseDevelopment`, `AerobicDurability`, `Consolidation`, `PreRunwayAlignment`) describe a **progressive base-building period preceding Preparation Runway** — the objective is base/aerobic development leading toward Runway-readiness, not indefinite maintenance and not merely "excess-horizon absorption." This matches the phase prompt's own "general endurance / progressive preparation" framing more than "maintenance" or "base building" alone — it is explicitly a *staged, developing* segment (Entry → BaseDevelopment/AerobicDurability mesocycles → Consolidation recovery weeks interleaved → PreRunwayAlignment), not a flat/static block.

---

## 5. Full 21-52 GE length matrix (see §3 — identical table, not duplicated)

---

## 6. 4D GE behavior reconstruction — `INTERMEDIATE_4D_LONGHORIZON_GE_AUTHORITY_RECONSTRUCTION`

Traced directly from source (`LongHorizonGeStructuralContracts.cs`, `LongHorizonGeStructuralSelector.cs`, `LongHorizonGeNumericExecutor.cs`, `LongHorizonGeExitState.cs`, `LongHorizonFullExecutionValidator.cs`):

| Rule | Value | Classification |
|---|---|---|
| Weekly role structure | 1 `KEY_SESSION` + 2 `EASY_SUPPORT` (A/B) + 1 `LONG_RUN` = 4 sessions/week, **constant** through every GE week regardless of stage family | `DIRECT_CANONICAL_AUTHORITY` (Phase 4I.2 approved skeleton, `LongHorizonGeStructuralContracts.cs:37` doc comment quotes it explicitly) |
| KEY workout content | `EASY_STANDARD` v6 (ConsistencyNeeded profile) or `AEROBIC_STRENGTH_CONTROLLED_INTRO` v2 / `..._PROGRESSED` v2 (CoreEntryReady profile, staged by mesocycle) — never Core's ProfileBacked dual-KEY content | `DIRECT_CANONICAL_AUTHORITY` (Phase 4I.4, `LongHorizonGeStructuralSelector.cs:29-65`) |
| EASY/LONG workout content | `EASY_STANDARD` v6 / `LONG_RUN_STANDARD` v6 | `DIRECT_CANONICAL_AUTHORITY` (same source) |
| Starting-volume: positive observed | `Round(RecentWeeklyVolumeKm)`, direct reuse, unmodified | `DIRECT_CANONICAL_AUTHORITY` (Phase 4I.6, `LongHorizonGeNumericExecutor.cs:69-73`) |
| Starting-volume: missing/zero | **No fallback — `InvalidOperationException` thrown** if `RecentWeeklyVolumeKm is not (> 0)` (line 56-58, own comment: "no fallback/default is invented... its absence fails closed") | `DIRECT_CANONICAL_AUTHORITY` (an explicit, deliberate, already-approved fail-closed rule — not an accidental gap) |
| Weekly growth | Preferred 7% / hard 8% / absolute +2.5km cap (whichever preferred/absolute is smaller, never exceeding hard), reusing `VolumeSafetyPolicy.Default`'s ratios verbatim | `4D_SPECIFIC` for the anchor values it operates on, but the **ratios themselves** are `APPROVED_GENERIC_LONGHORIZON_RULE` (already the same ratios Core/Runway/3D/Beginner4D all share) |
| Recovery cadence | Every 4th week (`RecoveryConsolidation` mesocycle position): `Round(priorPeak × 0.85)`, minimum 0.5km reduction; next mesocycle's growth resumes from `priorPeak`, never from the reduced recovery value | `DIRECT_CANONICAL_AUTHORITY` (Phase 4I.2A, `TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001`) |
| Long-run share | `VolumeSafetyPolicy.Default.LongRunSelectionShare` (33%), clamped to `[PreferredMinimumShare(30%), min(PreferredMaximumShare(36%), HardCapShare(40%))]` | `4D_SPECIFIC` (these are `VolumeSafetyPolicy.Default`'s own 4D-tuned figures — `FREQ.6D.9` already proved these specific shares are wrong for 5D) |
| Plateau/target cap | **None found** — growth continues, capped only by the per-week ratio/absolute increment, with no absolute ceiling tied to Runway-entry appropriateness | `IMPLEMENTATION_BEHAVIOR_WITHOUT_AUTHORITY` (no doc comment, policy ID, or phase reference anywhere establishes an intentional absolute cap — see §19 for the numeric consequence) |
| Adaptation interaction | None found directly in GE's own numeric executor (GE's numeric baseline is computed once per plan via `Execute`, not re-evaluated per rolling window in the code inspected this phase) | `UNKNOWN` at the GE-numeric-executor layer specifically — Adaptation's own severity policy is separately confirmed generic (§20) but GE's *own* consumption of it was not found wired in the dark `LongHorizonFullNumericOrchestrator`/`LongHorizonDarkExecutionOrchestrator` path inspected |
| Transition to Preparation Runway | One-sided upper-bound check only: `RunwayWeek1Volume <= GEExitVolume + max(GEExitVolume × 0.08, 2.5) + 0.01` tolerance — Runway is **not** required to start at or above GE's exit volume, only not to exceed it by more than the approved hard-growth cap | `DIRECT_CANONICAL_AUTHORITY` (Phase 4I.6A, `LongHorizonFullExecutionValidator.cs:57-69`) |
| Catalog workout selection | Hand-mirrored backend constant table (`LongHorizonGeStructuralSelector.StageFamilyRoleAssignments`), sourced from a `DRAFT`, never-runtime-loaded catalog document (`ten-k-long-horizon-ge-stage-families.v1.json`) | `4D_SPECIFIC` structurally (the table itself has no level/frequency field per its own JSON, but was authored/mirrored assuming the 4-role, 4D-shaped structure) |

---

## 7. Frequency-semantics decision — `FIXED_5_SESSION_GE`

**`FIXED_5_SESSION_GE`, not `FREQUENCY_RAMP_ALLOWED`.** `FREQ.6D.6`'s own reasoning for Preparation Runway's fixed-5-session decision is explicitly re-examined, not blindly reused: its grounding was "RunLayout's fixed-cardinality architecture and Core's own phase-invariant frequency" — a generic architectural principle about `RUN_LAYOUT_5D` itself (5 sessions, always, at every phase, by construction — confirmed again this phase: `RUN_LAYOUT_5D`'s catalog definition has exactly 5 `slots`, no phase/segment-conditional cardinality anywhere in its schema). `FREQ.6D.11` established that LongHorizon should derive session count from the resolved `RunLayout` rather than hand-set literals (§9/§60 of that report). Since GE, once generalized, would resolve against the *same* `TEN_K__5D__INTERMEDIATE` candidate's `RUN_LAYOUT_5D`, the identical fixed-cardinality reasoning applies transitively — not because `FREQ.6D.6`'s conclusion was copied, but because its underlying architectural premise (RunLayout cardinality is phase-invariant for a given candidate) is itself a generic principle that governs GE the same way it governs Runway and Core. Confirmed independently: existing 4D GE is *already* fixed at exactly 4 sessions/week for every stage family and every mesocycle position, with no ramp anywhere in `LongHorizonGeStructuralSelector` — this is corroborating evidence, not the sole basis.

---

## 8. Structural candidates

**A — 1 KEY + 3 EASY + 1 LONG (5 sessions).** Direct generalization of existing 4D GE's own approved 1 KEY + 2 EASY + 1 LONG shape (§6) plus one additional `EASY_SUPPORT` slot — the exact same generalization pattern `FREQ.6D.6`/`FREQ.6D.7` already applied to Preparation Runway's own 1 KEY + 2 EASY → 1 KEY + 3 EASY generalization. Uses `FourDaySessionDistanceAllocationPolicy.Allocate(weeklyVolumeKm, longRunDistanceKm, keySessionCount: 1, easySupportCount: 3)` — already supported, no change (confirmed this phase: the method's `easySupportCount` parameter is already free-form `int`, not hardcoded to 2, per its real signature at `FourDaySessionDistanceAllocationPolicy.cs:62`).

**B — 0 KEY + 4 EASY + 1 LONG.** Rejected at the allocator level: `FourDaySessionDistanceAllocationPolicy.Allocate` throws `CatalogSessionPrescriptionInfeasibleException` when `keySessionCount < 1` (`.cs:64-67`, confirmed this phase) — this specific shared allocator cannot produce a 0-KEY week. A 0-KEY GE week would require routing through Runway's own *separate* 0-KEY allocation path (the one Runway's block-role table already uses for its own "many weeks legitimately 0-KEY" rows, per `FREQ.6D.5`'s finding) rather than GE's currently-wired allocator — a real, additional engineering surface with no corresponding product benefit identified (see §9 below for why sustained single-KEY is actually preferred, not merely convenient).

**C — Mixed GE (some 0-KEY weeks, some 1-KEY weeks).** Evaluated and rejected: no repository evidence anywhere (4D GE's own stage-family table, Runway's approved decision, or external evidence) proposes alternating KEY presence within a single progressive base-building segment — Runway's own 0-KEY weeks exist for a *different* reason (a scoped, shorter 15-20 week pre-Core runway with specific block-sequencing semantics, not a variable-length up-to-32-week endurance-building segment) and are not evidence for mixing within GE specifically.

**D — Frequency ramp (fewer than 5 sessions initially, later 5).** Rejected per §7's `FIXED_5_SESSION_GE` determination — `RUN_LAYOUT_5D` has no phase-conditional cardinality, and introducing one for GE specifically would be a genuinely new, unevidenced product invention, not a generalization of anything that already exists.

**E — no other candidate found supported by real evidence.**

**Candidate A selected** — see §26/§29 for the full scored decision matrix.

---

## 9. Quality-session duration problem (§8 of the phase prompt)

**1 KEY every GE week for up to 32 weeks is already the existing, approved, shipped 4D behavior** (§6 — no ramp, no reduction, every stage family carries exactly one `KEY_SESSION` slot). This is not merely "appropriate," it is the repository's own already-live design: `LongHorizonGeStructuralSelector.SelectFullPhase` assigns a `KeySession` role to every `BuildDescriptor` call for every mesocycle position and every remainder week, unconditionally. Critically, the GE `KEY_SESSION` workout content itself is **moderate, not maximal**: `EASY_STANDARD` (an easy-effort workout, for the `ConsistencyNeeded` profile) or `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`..._PROGRESSED` (a controlled, progressively-introduced aerobic-strength session, not a hard interval/tempo session — contrast with Core's own `GOAL_PACE_TEN_K`/`THRESHOLD_TEMPO` KEY content). Sustaining one moderate-intensity "quality" session weekly for an extended base-building period is a coaching-plausible pattern (a single weekly strides/tempo-light or controlled-progression session across months-long base phases is common in general-endurance program design) and, most importantly, does not need new evidence to be approved for 5D — it is a direct generalization of already-approved 4D behavior, not a new invention.

**0 KEY for up to 32 weeks was evaluated (Candidate B, §8) and rejected** — both for the allocator-support reason (§8) and because it would create exactly the "undesirable abrupt transition into Preparation Runway" the phase prompt warns about: a runner completing 32 GE weeks with zero quality-session exposure, then immediately entering Runway's 1 KEY + 3 EASY + 1 LONG structure, would face a first-ever KEY session at the very moment Runway also begins ramping volume toward Core — compounding two step-changes (quality-session introduction and volume progression) at the same boundary, which the existing approved Runway→GE continuity check (§25) does not anticipate and which no repository evidence supports as safe.

---

## 10. External evidence review

Repository-internal evidence (§6, §9) is dominant and sufficient for the structural decision — per the decision standard (§50), `DIRECT_CANONICAL_AUTHORITY`/`EXISTING_APPROVED_PRODUCT_DEFAULT` outranks external literature, and both apply directly here (existing 4D GE structure, existing 5D Runway structure). Targeted external corroboration for "sustained single weekly quality session across extended base-building periods":

- **COACHING_PATTERN**: Established base-building periodization (e.g., Daniels'/Pfitzinger-style base phases, Higdon's own base-mileage-before-training-cycle guidance already cited in `FREQ.6C`'s closure) commonly prescribes one weekly "strides," "hill," or "controlled tempo" session during extended base periods, with the remaining volume as easy running plus a weekly long run — structurally matching Candidate A's 1 KEY + N EASY + 1 LONG shape. Classified `COACHING_PATTERN`, not `DIRECT_EVIDENCE` — no single source specifies "exactly 3 EASY_SUPPORT for a 5-day week," that count follows from `RUN_LAYOUT_5D`'s own cardinality (§7), not from external literature.
- **PRODUCT_DEFAULT_SUPPORT**: No external source specifies exact Appsel km values for GE — none is claimed here; §14-17 resolve numeric authority from repository-internal precedent, not literature.

No external conclusion is treated as providing exact Appsel numeric authority.

---

## 11. Catalog capacity audit — `INTERMEDIATE_5D_GE_CATALOG_CAPACITY_MATRIX`

| Content | Real file confirmed this phase | Candidate-agnostic? | Sufficient for Candidate A (5D)? |
|---|---|---|---|
| `EASY_STANDARD` v6 | `plan-catalog/catalog/workouts/easy-standard.v6.json` | Yes (no level/frequency field, same content Runway/Core already reuse in Legacy/effort-only mode) | Yes |
| `LONG_RUN_STANDARD` v6 | `plan-catalog/catalog/workouts/long-run-standard.v6.json` | Yes | Yes |
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` v2 | `plan-catalog/catalog/workouts/aerobic-strength-controlled-intro.v2.json` | Yes | Yes |
| `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` v2 | `plan-catalog/catalog/workouts/aerobic-strength-controlled-progressed.v2.json` | Yes | Yes |
| Profiles | N/A — GE is Legacy/effort-only throughout (§6, confirmed zero `PrescriptionProfileKey`/`ProfileBacked` references anywhere in GE's structural/numeric code this phase) | N/A | N/A (no profile needed) |
| Execution prescriptions | N/A (same reason) | N/A | N/A |
| Public workout-type mapping | `EASY_STANDARD`/`LONG_RUN_STANDARD` already mapped (Easy/LongRun arms, live since before this engagement); `AEROBIC_STRENGTH_CONTROLLED_INTRO` already mapped to `Interval` (`FREQ.6D.4D.5F`) | — | Yes — `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` was the one workout `FREQ.6D.5` flagged as unmapped (its R-D5 finding), but that gap belongs to Preparation Runway's own public-activation scope, not GE (GE has never been publicly activated for any frequency, so this is pre-existing, out-of-scope debt, not a new GE-specific blocker) |
| `FourDaySessionDistanceAllocationPolicy.Allocate` (allocator itself, not catalog content, but the mechanism that consumes it) | Confirmed `keySessionCount`/`easySupportCount` parameterized (§8) | Yes | Yes — supports `(1, 3)` directly, no change needed |

**No `CATALOG_CAPACITY_BLOCKER` for Candidate A.** All required content exists, is candidate-agnostic, and is already used identically by the already-5D-activated Preparation Runway pipeline. No new content authoring required.

---

## 12. GE KEY semantics

Existing 4D GE already uses a **generic, frequency-independent, Legacy (effort-only)** workout for its single `KEY_SESSION` slot (§6/§11) — never Core's ProfileBacked, dual-lane architecture. This answers the phase's own explicit concern directly: **GE does not and will not create a second quality lane** — Candidate A keeps exactly one `KEY_SESSION` slot per week (§8's Candidate C, mixed/multi-KEY, was not even evaluated as a serious candidate, since nothing in repository evidence or Core's own dual-KEY rationale — which exists specifically for Core's own progression-stage/profile architecture — applies to GE's simpler, Legacy-only design).

## 13. GE EASY/LONG semantics

`EASY_SUPPORT` → `EASY_STANDARD` v6; `LONG_RUN` → `LONG_RUN_STANDARD` v6 (§6/§11) — reused verbatim, generic, already-approved content. No new workout identity is introduced for the third `EASY_SUPPORT` slot Candidate A adds — it uses the identical `EASY_STANDARD` v6 reference the existing two `EasySupportA`/`EasySupportB` slots already use (per §15 of `FREQ.6D.11`'s own design principle: disambiguate repeated-role slots by ordinal, never by inventing new content or new role names).

---

## 14. Starting-volume authority

Resolved per §6's reconstruction, **not** by reusing Core/Runway's 26.0/19.5km values (explicitly rejected per the phase's own instruction, §14/§42/§43) — GE's own existing, already-approved authority (Phase 4I.6) is evidence-gating, not a specific-km default, and generalizes directly:

- **Positive observed**: direct reuse of `RecentWeeklyVolumeKm`, rounded to the existing 0.5km increment — `APPROVED_GENERIC_LONGHORIZON_RULE`, frequency-independent at the total-volume level (only the per-session split, already handled by the generalized allocator, depends on frequency).
- **Missing**: `PRODUCT_INELIGIBLE` — carrying forward the existing, already-approved fail-closed rule (`RecentWeeklyVolumeKm is not (> 0)` throws) unchanged. This is not a new decision invented to make arithmetic work; it is the literal existing 4D authority, which contains no frequency-specific number to borrow or reinterpret — the rule itself ("missing evidence has no invented fallback for GE") is what generalizes, verbatim.
- **Explicit zero**: also `PRODUCT_INELIGIBLE` under the identical existing rule (`0 is not > 0`, same code path) — remaining a **distinct state** from missing (both are independently evaluated against the same `> 0` gate, never normalized into each other), but coinciding in *outcome* under the currently-approved authority.

---

## 15. Positive observed — trace

`LongHorizonGeNumericExecutor.Execute` (§6): `totalVolume = Round(baseline.RecentWeeklyVolumeKm.Value)` for the first GE week — **direct use**, no clamp/floor/ceiling/frequency-adjustment applied at the total-volume level. `Round` uses the existing 0.5km increment (`VolumeSafetyPolicy.Default.RoundingIncrementKm`, itself already established generic per `FREQ.6D.5`'s own audit). This existing generic behavior is preferred and reused unmodified.

## 16. Missing readiness — resolved

`PRODUCT_INELIGIBLE`, per §14. No numeric value is chosen "merely so later arithmetic works" — the existing rule's own logic (fail closed, no invented default) is the authority, carried forward exactly.

## 17. Explicit zero — resolved

`PRODUCT_INELIGIBLE`, per §14, kept distinct in *classification* from missing (both independently traced through the same existing `RecentWeeklyVolumeKm is not (> 0)` check — a genuine repository-evidenced rule, not a silent zero→missing normalization) while coinciding in *practical outcome* under current authority. The phase prompt's own framing ("long horizon gives time for gradual preparation, but that alone does not prove eligibility") is directly honored: no new evidence exists that would justify overriding GE's existing fail-closed rule merely because more weeks are theoretically available to absorb a gradual buildup — that would be exactly the kind of new, unevidenced numeric invention §43 forbids.

---

## 18. Minimum representability — `INTERMEDIATE_5D_GE_MINIMUM_REPRESENTABILITY_TABLE`

Using real session minima (`MinimumKeySessionDistanceKm = 3.0km`, `MinimumEasySupportDistanceKm = 1.5km`, confirmed this phase) and the FREQ.6C-approved 5D long-run share (28% selection — see §26 for why this share, not Default's 33%, is the correct one to apply here):

| Structure | Non-long minimum | Minimum weekly volume (share=28%) |
|---|---|---|
| 1 KEY + 3 EASY + LONG (Candidate A) | 3.0 + 3×1.5 = 7.5km | 7.5 / (1-0.28) ≈ 10.4km |
| 0 KEY + 4 EASY + LONG (Candidate B, for comparison only — rejected §8) | 4×1.5 = 6.0km | 6.0 / (1-0.28) ≈ 8.3km |

This is a **feasibility floor only**, not a starting-volume default (§18's own explicit caveat, honored) — it is used in §29's representability check to confirm that low positive-observed baselines (e.g. 12km) remain representable under Candidate A, which they are (12km > 10.4km floor).

---

## 19. Current GE progression — numeric coefficient audit

| Coefficient | Value | Classification |
|---|---|---|
| Preferred weekly growth | 7% (`VolumeSafetyPolicy.Default.PreferredMaxWeeklyIncreaseRatio`) | `GENERIC_LONGHORIZON_AUTHORITY` (ratio-level; same ratio Core/Runway/3D/Beginner4D/5D all already share) |
| Hard weekly growth | 8% | `GENERIC_LONGHORIZON_AUTHORITY` (same reasoning) |
| Absolute km cap | +2.5km | `GENERIC_LONGHORIZON_AUTHORITY` (same reasoning — also the exact value `VolumeSafetyPolicy.FiveDayIntermediate` itself already uses, per `FREQ.6D.10`) |
| Rounding | 0.5km increment | `GENERIC_LONGHORIZON_AUTHORITY` |
| Long-run progression/share | 33% selection / [30%,36%] clamp | `4D_SPECIFIC` — these are `VolumeSafetyPolicy.Default`'s own figures; `FREQ.6D.9` already proved this exact share is wrong for 5D (see §26) |
| Target/cap behavior | None found | `IMPLEMENTATION_BEHAVIOR_WITHOUT_AUTHORITY` — real numeric consequence quantified in §21 below |

---

## 20. Cross-frequency numeric reuse — provenance documented

The growth ratios (7%/8%/2.5km) are reused because they are **demonstrably frequency-independent** — they are not `VolumeSafetyPolicy.Default`-*specific* despite living on that instance; the identical ratios appear verbatim on `VolumeSafetyPolicy.FiveDayIntermediate`, `ThreeDayIntermediate`, and `BeginnerFourDay` (confirmed by direct inspection of `VolumeSafetyPolicy.cs` — all four instances share `PreferredMaxWeeklyIncreaseRatio: 0.07d, HardMaxWeeklyIncreaseRatio: 0.08d, AbsoluteWeeklyIncrementCapKm: 2.5d`). This is the one genuine case in this whole policy family where a *formula* (not an absolute km value) is proven frequency-independent by cross-instance repetition, not merely asserted. The long-run share, by contrast, is proven **frequency-dependent** by the same evidence (`VolumeSafetyPolicy.Default`'s 33%/30%/36% differ from `FiveDayIntermediate`'s 28%/28%/36%, `ThreeDayIntermediate`'s 40%/38%/42%, and `BeginnerFourDay`'s 33%/30%/36%) — so it is **not** reused from `Default`; §26 resolves it via the already-approved 5D-specific figures instead.

---

## 21. Long-horizon growth problem — quantified

A representative simulation (baseline weekly = 24km, the same representative figure used throughout `FREQ.6D.10`'s own real-candidate tests, growth ratios per §19/§20, mesocycle cadence per §6) shows: for reference volumes below ≈35.7km, the 7%-preferred/8%-hard ratio caps dominate (compounding growth); above ≈35.7km, the +2.5km absolute cap becomes the binding constraint (0.07 × 35.7 ≈ 2.5), so growth becomes linear (+2.5km per growth week, ≈+7.5km per 4-week mesocycle) rather than compounding. Carried to 32 GE weeks (8 mesocycles) with the existing recovery-resumes-from-peak rule (§6), this trajectory reaches **approximately 70+ km/week by GE week 32** — a volume that exceeds even Core's own FREQ.6C-approved 5D peak reference (44.5km, `FREQ.6D.9`) by more than 50%, before the runner has even entered Preparation Runway (which itself only builds toward that 44.5km peak across its own subsequent 8 Runway + 12 Core weeks).

**This confirms the phase prompt's own concern (§21/§28) is real, not hypothetical, and is already latent in the existing (never-yet-publicly-activated) 4D GE implementation** — extending it unchanged to a 32-week 5D GE segment would produce an unrealistic Runway entry. **GEP-A (continuous, uncapped growth) is rejected** for exactly this reason, notwithstanding that it is technically the current 4D-approved pattern — the phase's own explicit instruction ("reject endless growth if it produces unrealistic Runway entry... prove stable behavior") overrides silent inheritance of that pattern once the real numeric consequence is quantified.

---

## 22. Target-capped model — selected (GEP-B)

**Selected: GE builds from the observed starting volume toward a target, then maintains around that target (via existing Adaptation, not a frozen value) until Runway begins.** The target is **not a new invented number**: it is the already-approved `FREQ.6C` 5D resolved peak reference, **44.5km** (`VolumeSafetyPolicy.FiveDayIntermediate.ResolvedPeakReference`, `FREQ.6D.9`/`FREQ.6D.10`) — the same ceiling this exact population (Intermediate×5D) is already approved to reach during Core. Using it as GE's own plateau ceiling is a *decision* this phase is chartered to make (not a re-derivation of a new number, and not "choosing a value merely so arithmetic works" — §43's prohibition is honored because 44.5km is not being reverse-engineered from a target week-count, it is the pre-existing, independently-approved ceiling for this exact runner population, being *applied* to a new segment). Confirmed existing 4D behavior does **not** already implement this or an equivalent — §6/§19 found no target-cap anywhere in the current GE numeric executor; this is a genuine, new (for GE), evidence-backed product decision, not a rediscovery of hidden existing behavior.

---

## 23. Plateau / maintenance — resolved

**Maintain with Adaptation-driven Progress/Maintain/Reduce around the target**, not a frozen flat value and not a cycle/deload scheme. Once GE's weekly volume reaches the 44.5km target (or the runner is already at/above it from a high positive-observed baseline — see §29's higher-positive-baseline horizon cases), subsequent GE weeks hold at that target, modulated only by the same generic `NextWindowLoadDecisionPolicy` Adaptation authority `FREQ.6D.11` already confirmed generic/reachable (§13/§20 below) — no new periodization complexity is invented, honoring §23's explicit warning. The existing recovery-week cadence (every 4th week, `Round(priorPeak × 0.85)`, §6) is preserved unchanged during the pre-plateau growth phase; once plateaued, ordinary Adaptation supersedes the fixed recovery cadence as the load-modulation mechanism (consistent with how Core/Runway themselves use Adaptation post-Taper rather than a second, competing recovery schedule).

---

## 24. Preparation Runway entry authority — traced

Real first Runway week target under the now-`FREQ.6D.10`-wired Intermediate×5D implementation (traced via `TenKPreparationRunwayNumericPolicyFactory.Build(candidate)` → `VolumeSafetyPolicy.FiveDayIntermediate`, confirmed this phase): **missing → 26.0km; explicit zero → 19.5km; positive observed → the reported value directly** (same `ResolveStartingWeeklyVolume` dispatch `FREQ.6D.10` implemented). **GE does not need to hand off a value equal to these** — §25's continuity validator (already existing, `FREQ.6D.11`-reconciled) only enforces a one-sided *upper* bound (Runway Week 1 ≤ GE-exit + hard-growth-cap), never a lower bound or exact match. This separates **volume continuity** (the numeric handoff, bounded above only) from **structural continuity** (which the existing validator does *not* require — GE's 4-session shape and Runway's 5-session shape are different by design, exactly as §24 of the phase prompt anticipates: "GE does NOT necessarily have to equal Runway structurally before the boundary").

---

## 25. GE→Runway continuity — audited (existing validator, unmodified)

`LongHorizonFullExecutionValidator.Validate` (§6's reconstruction, re-quoted): `maxAllowedIncrease = Math.Max(geExit.FinalWeeklyVolumeKm × 0.08, 2.5) + ToleranceKm(0.01)`; violation only if `RunwayWeek1Volume > GEExitVolume + maxAllowedIncrease`. Units: both sides are plain km quantities (not a ratio/share comparison, so the `FREQ.6D.10`-corrected ratio-vs-km-epsilon units lesson does not apply here — this check was already dimensionally consistent). No structural-role assumption is enforced at this boundary (§24) — only `week.OrderedSlots.Count != 4` is checked **within GE's own weeks**, a hardcoded 4D-shaped assumption that Candidate A's engineering implementation (a future phase, not this one) must generalize to the resolved layout's slot count, exactly as `FREQ.6D.11`'s own §7/§60 already anticipates for the rest of LongHorizon's `DaysPerWeek`-literal sites. **No implementation change is made this phase** — this is evidence only.

---

## 26. Long-run authority — resolved

**Preferred share = selection share = 28%; hard cap = 36%** — the `FREQ.6C`-approved, `FREQ.6D.9`/`FREQ.6D.10`-implemented Intermediate×5D-specific figures (`VolumeSafetyPolicy.FiveDayIntermediate.LongRunSelectionShare`/`LongRunHardCapShare`), **not** `VolumeSafetyPolicy.Default`'s 33%/30%/36% GE currently uses for 4D. This is not "automatically reusing Core/Runway's 28%/36%" in the forbidden sense (§26 of the phase prompt warns against exactly that without proof) — proof exists: `FREQ.6D.9` already established, with real minimum-representability arithmetic, that `Default`'s 4D-tuned shares are specifically *wrong* for a 5-day/week Intermediate runner (too high a long-run share relative to the real 2-KEY/1-KEY+N-EASY session-minimum floor), and that 28%/36% is the evidenced correction for this exact runner population — a population-level correction, not a segment-level one, so it applies to GE for the identical reason it applies to Core/Runway: the runner is the same Intermediate×5D athlete in every segment. No minimum-share floor beyond the existing `LongRunPreferredMinimumShare = LongRunSelectionShare = 28%` collapse (`FREQ.6D.10`'s own already-approved design, §3 of that report) is introduced — reused verbatim, no third number invented. Growth behavior: the long-run distance is recomputed each week from the week's own total volume via the identical `ResolveDevelopmentLongRun`/`ResolveRecoveryLongRun` formulas (§6), unchanged in *shape*, only in which `VolumeSafetyPolicy` instance supplies the share.

---

## 27. 21-week edge

GEWeeks = 1 (§3). Under Candidate A + §22's target-capped model: GE Week 1 uses the positive-observed/ineligible-otherwise starting volume directly (§15-17) — with only one GE week, no growth step occurs before Runway entry (the "first GE week: entry baseline itself, unprogressed" branch in `LongHorizonGeNumericExecutor.Execute`, §6, applies unconditionally). This is **already representable without violating continuity**: Runway Week 1's own upper-bound check (§25) only requires `RunwayWeek1 ≤ GEWeek1 + hard-growth-cap`, which is satisfiable for any positive-observed baseline within GE's own minimum-representability floor (§18) — no impossible progression is required, exactly as §27 of the phase prompt requires.

## 28. 52-week edge

GEWeeks = 32 (§3). Under §21's rejected uncapped model this would reach ≈70+km/week — genuinely unstable. Under §22's selected target-capped model (plateau at 44.5km, the same ceiling Core itself already approves for this population), the trajectory instead grows toward 44.5km over the early mesocycles and then **holds** (Adaptation-modulated, §23) for the remaining GE weeks — stable, bounded, and consistent with Core's own approved peak, satisfying §28's explicit "must not grow indefinitely / overtrain by design / arrive massively above Runway / require hidden resets" requirements without inventing new periodization.

---

## 29. Representative horizons

| TotalWeeks | GEWeeks | Missing/Zero | Low positive (12km) | Representative positive (24km) | Higher positive (40km) |
|---|---|---|---|---|---|
| 21 | 1 | `PRODUCT_INELIGIBLE` | GE W1=12km (unprogressed) → Runway | GE W1=24km → Runway | GE W1=40km → Runway |
| 22 | 2 | `PRODUCT_INELIGIBLE` | 12→~14.5km (growth step, +2.5 cap dominates at this range) | 24→~25.7km | 40→~42.5km (approaching 44.5 target) |
| 24 | 4 | `PRODUCT_INELIGIBLE` | grows through mesocycle 1 (incl. 1 recovery week), well below target | grows toward mid-30s by week 4 | reaches/nears 44.5km target, begins plateau |
| 28 | 8 | `PRODUCT_INELIGIBLE` | still growing, below target | approaching target | plateaued at 44.5km (Adaptation-modulated) |
| 32 | 12 | `PRODUCT_INELIGIBLE` | approaching target (3 mesocycles) | plateaued | plateaued |
| 40 | 20 | `PRODUCT_INELIGIBLE` | plateaued | plateaued | plateaued |
| 52 | 32 | `PRODUCT_INELIGIBLE` | plateaued (long maintenance period, Adaptation-modulated) | plateaued | plateaued |

All positive-observed cells: GE weekly cardinality constant at 5 sessions (§7); volume evolution per §19/§22; plateau point reached once the 44.5km target is hit (varies by baseline, all reachable within the supported 1-32 week GE range given the growth-rate arithmetic in §21); last GE volume ≤ 44.5km always (never exceeds — growth is explicitly clamped to the target once reached); first Runway volume continuity satisfied by construction (§25's one-sided upper-bound check, since GE never exceeds 44.5km and Runway's own missing/zero/positive starting values are all ≤ that ceiling per `FREQ.6D.10`); representable and eligible for every positive-observed case at every horizon. Missing/zero: `PRODUCT_INELIGIBLE` uniformly across all seven representative horizons (§16/§17 — the rule is horizon-independent, since it fires at GE Week 1 regardless of how many GE weeks exist).

---

## 30. Full 21-52 representability

Mechanically verified via §3's decomposition (GEWeeks = TotalWeeks-20, valid 1-32 for every TotalWeeks in 21-52, matching `LongHorizonGeStructuralSelector`'s own enforced domain) combined with §22's target-capped growth model (bounded, monotonic-until-plateau, defined for any GE week count 1-32): **no unsupported internal hole exists across 21-52** for positive-observed readiness. Missing/explicit-zero is uniformly `PRODUCT_INELIGIBLE` across the same range (§29) — a deliberate, evidence-backed eligibility boundary, not a representability gap.

---

## 31. Structure decision matrix — `INTERMEDIATE_5D_LONGHORIZON_GE_STRUCTURE_DECISION_MATRIX`

| Candidate | GE-purpose fit | Long-duration suitability | External evidence | Appsel precedent | Fixed-freq compat. | Catalog capacity | Adaptation compat. | Runway transition | Min. representability | 6D/7D generality | Selected? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 KEY + 3 EASY + LONG | Strong (progressive base-building) | Strong (§9, moderate KEY content) | `COACHING_PATTERN` | Direct generalization of existing 4D GE (§6) | Yes | Ready (§11) | Yes | Smooth (no abrupt step, §9) | ≈10.4km floor (§18) | Yes (N-EASY generalizes) | **YES** |
| 0 KEY + 4 EASY + LONG | Weak (no quality exposure for months) | Poor (abrupt Runway transition, §9) | None found favoring | None (no existing GE or Runway precedent for 0-KEY *throughout*) | Yes | Blocked (allocator rejects, §8) | Yes | Abrupt (§9) | ≈8.3km floor | Yes | No |
| Mixed quality | Unclear purpose | Unclear | None found | None | Yes | Would need new engineering | Untested | Untested | N/A | Partial | No |
| Frequency ramp | N/A | N/A | N/A | Contradicts `RUN_LAYOUT_5D`'s fixed cardinality (§7) | No | N/A | N/A | N/A | N/A | No | No |

---

## 32. Numeric decision matrix — `INTERMEDIATE_5D_LONGHORIZON_GE_NUMERIC_DECISION_MATRIX`

| Policy | Existing authority | External support | 21w feasibility | 52w feasibility | Continuity | Adaptation compat. | New number required? | Selected? |
|---|---|---|---|---|---|---|---|---|
| Start (positive) | Yes — existing GE rule, direct reuse (§15) | N/A | Yes | Yes | Yes | N/A | No | **YES** |
| Start (missing/zero) | Yes — existing GE fail-closed rule (§16/17) | N/A | Yes (ineligible, uniformly) | Yes | N/A | N/A | No | **YES** |
| Progression (ratios) | Yes — generic, cross-instance-proven (§19/§20) | N/A | Yes | Yes (bounded by plateau) | Yes | N/A | No | **YES** |
| Plateau (target=44.5km) | Yes — reused from existing `FREQ.6C`/`FREQ.6D.10` peak reference (§22) | N/A | Trivial (rarely/never reached) | Yes (prevents §21's overshoot) | Yes | Yes (§23) | No (reused, not invented) | **YES** |
| Long-run share (28%/36%) | Yes — reused from `FREQ.6D.9`/`.10`'s existing 5D-specific authority (§26) | N/A | Yes | Yes | Yes | N/A | No | **YES** |
| Runway-entry target | N/A (GE hands off *whatever* its own final volume is; Runway independently resolves its own missing/zero/positive start, §24) | N/A | Yes | Yes | Yes (one-sided upper bound only, §25) | N/A | No | **YES** |

**Zero new numbers were invented anywhere in this decision set** — every value is either an existing generic ratio (proven cross-instance, §20), an existing GE-specific rule carried forward verbatim (§15-17), or an existing `FREQ.6C`-approved 5D-specific figure applied to a new segment by explicit product decision (§22, §26).

---

## 33. Authority conflict table — `GE_NUMERIC_AUTHORITY_CONFLICT_TABLE`

| Candidate rule | Source | Semantic scope | Winner | Why |
|---|---|---|---|---|
| Long-run share 33%/30%/36% | `VolumeSafetyPolicy.Default`, 4D GE's current runtime behavior | Population-level (which share fits an Intermediate×5D runner) | **`FREQ.6C`/`FREQ.6D.9`-approved 28%/36%** | `FREQ.6D.9` already proved, with real arithmetic, that `Default`'s shares are specifically miscalibrated for 5D — this is a population question, not a 4D-vs-GE-segment question, and the population-correct answer already exists and wins |
| Growth ratios 7%/8%/+2.5km | `VolumeSafetyPolicy.Default`, shared by every policy instance | Formula-level (frequency-independent by construction, §20) | **Reused unchanged (no conflict)** | Identical across all four `VolumeSafetyPolicy` instances — not actually in conflict, since the value doesn't vary by frequency to begin with |
| Uncapped growth (GEP-A, current 4D behavior) | 4D GE's current runtime behavior, `Phase 4I.2` | Segment-duration-scaling (does open-ended growth remain safe as GE length grows to 32 weeks) | **This phase's target-capped model (GEP-B, §22)** | §21's quantified simulation proves the uncapped model produces an unrealistic (~70+km/week) Runway entry at the 52-week extreme — the phase's own explicit "reject endless growth" instruction (§21 of the phase prompt) overrides silent inheritance of the current 4D pattern once the real numeric consequence is known |
| Missing/zero start value | `FREQ.6C`/`FREQ.6D.9`/`.10`'s 26.0km/19.5km (Core/Runway-scoped) vs. GE's own existing fail-closed rule | Segment-scope (does Core/Runway's specific missing/zero *default* also apply to GE) | **GE's own existing fail-closed rule (`PRODUCT_INELIGIBLE`)** | The Core/Runway values are explicit product defaults scoped to those two segments' own decision closures (`FREQ.6D.6`/`FREQ.6C`) — GE was never in scope for those decisions, and GE's own, separately-approved rule (no invented fallback) is the correct, non-borrowed authority for GE specifically |

---

## 34. Selected structure — frozen

**Intermediate×5D LongHorizon GE:**
- `KEY_SESSION` count = **1**
- `EASY_SUPPORT` count = **3**
- `LONG_RUN` count = **1**
- Total sessions = **5**
- Structure constant through GE? **YES**
- Frequency ramp? **NO**
- Adaptation may change role cardinality? **NO** (§13/§20 of this report — Adaptation modulates load only, never structure; confirmed by the same lane-blind/role-aggregate design `FREQ.6D.11` already verified for Core/Runway, which GE inherits unchanged)

---

## 35. Selected start policy — frozen

- **Positive observed**: `Round(RecentWeeklyVolumeKm, 0.5km)`, direct reuse, no floor/cap beyond the existing rounding convention. Provenance: existing `LongHorizonGeNumericExecutor.Execute` first-week branch, unchanged.
- **Missing**: `PRODUCT_INELIGIBLE`. Provenance: existing `LongHorizonGeNumericExecutor`'s own fail-closed `RecentWeeklyVolumeKm is not (> 0)` guard, carried forward verbatim (`TD-LONG-HORIZON-GE-SAFETY-001`).
- **Explicit zero**: `PRODUCT_INELIGIBLE`. Same provenance, same guard (0 fails `> 0` identically to null) — distinct classification, coincident outcome, never silently normalized.

## 36. Selected progression policy — frozen

- Preferred growth: **7%** (owner: `VolumeSafetyPolicy.FiveDayIntermediate.PreferredMaxWeeklyIncreaseRatio` — referencing the 5D-specific instance rather than `Default`, for consistency with §26's population-correction reasoning, though the numeric value is identical across instances).
- Hard growth: **8%** (same owner).
- Absolute increase cap: **+2.5km** (same owner).
- Rounding: **0.5km**, `AwayFromZero` (existing convention, unchanged).
- Target cap: **44.5km** (owner: `VolumeSafetyPolicy.FiveDayIntermediate.ResolvedPeakReference`, reused per §22 — not a new field, an existing field applied to a new consumer).
- Plateau semantics: hold at target, Adaptation-modulated (§23).
- Adaptation interaction: load-only, post-plateau (§13/§20/§34).

## 37. Selected long-run policy — frozen

- Preferred share = selection share: **28%**.
- Hard cap: **36%**.
- Minimum: none beyond the existing preferred-minimum-equals-selection-share collapse (§3 of `FREQ.6D.10`'s report, reused unchanged — no new "minimum if applicable" field).
- Rounding: 0.5km (unchanged).
- Relationship to weekly progression: recomputed each week from that week's own total volume via the existing `ResolveDevelopmentLongRun`/`ResolveRecoveryLongRun` formula shape (§6), unchanged in structure.
- Runway-entry continuity: governed entirely by §25's existing one-sided upper-bound validator — no new continuity rule needed.

## 38. Selected Runway-entry contract — frozen

- 5-session frequency valid at Runway entry: **not a GE requirement** — GE itself is fixed at 5 sessions/week throughout (§34), so this is trivially satisfied, not a new gate.
- Weekly-volume continuity: **existing one-sided upper bound** (`RunwayWeek1 ≤ GEExit + max(GEExit×0.08, 2.5) + 0.01`), unmodified (§25).
- Long-run continuity: no separate GE-specific rule found or introduced — the existing validator (§25/§6) does not check long-run continuity at the GE→Runway boundary specifically (only at Runway→Core, per `LongHorizonFullExecutionValidator.cs:73-77`); not redesigned here.
- Minimum quality exposure: satisfied structurally by construction — GE always carries exactly one `KEY_SESSION`/week (§34), so there is no "first-ever KEY at Runway entry" discontinuity (§9's rejected-Candidate-B concern does not apply to the selected structure).
- Numeric tolerance: existing `0.01km` epsilon plus the existing `max(×0.08, 2.5km)` band, unmodified — no `FREQ.6D.10`-style units-mismatch bug found here (§25 confirmed both sides of the comparison are already plain km, not a ratio).

Preparation Runway itself is **not redesigned** anywhere in this section (§38 of the phase prompt honored).

---

## 39. Product eligibility

**Intermediate×5D LongHorizon remains an identity-level support candidate** (unchanged — `V1CatalogPilotIdentityPolicy` already recognizes `(Intermediate, 5)`, per `FREQ.6D.5`/`FREQ.6D.11`). **Request-level `PRODUCT_INELIGIBLE` cases, defined exactly**: any 21-52 week LongHorizon request where `RecentWeeklyVolumeKm` is missing (null) or explicit zero (§16/§17) — because GE (mandatory for every 21-52 week horizon, §3) has no approved starting-volume authority for either state. Positive-observed requests at every horizon and every representative baseline (§29) remain eligible.

---

## 40. No Core/Runway delta

Confirmed: no decision in this report touches `FourDaySessionDistanceAllocationPolicy`'s existing behavior, Preparation Runway's approved 1+3+1 structure or its `FREQ.6D.10`-wired numeric authority, or Core's approved 2+2+1 structure or its own numeric authority. §26/§37's reuse of the 28%/36% long-run share and §22's reuse of the 44.5km peak reference are read-only references to existing `VolumeSafetyPolicy.FiveDayIntermediate` fields — no field on that record, or on `V1FiveDayIntermediateMissingReadinessStartingVolumePolicy`, is modified.

## 41. No hidden reset

Confirmed: GE→Runway continuity is governed entirely by the existing, explicit, one-sided upper-bound validator (§25/§38) — no silent volume reset is introduced or relied upon anywhere in this design. Runway independently resolves its own Week-1 starting volume via its own already-approved missing/zero/positive authority (§24) regardless of what GE's exit volume was, which is itself the existing, explicit (not hidden) LongHorizon architecture — not a reset, a genuinely separate, already-approved per-segment numeric resolution.

## 42. No 4D absolute-value copying

Confirmed: no 4D absolute km value (16km/12km/24km/38km, or any literal from `VolumeSafetyPolicy.Default`) is used as 5D GE authority anywhere in this report. Every reused number (7%/8%/2.5km/0.5km rounding) is reused because it is proven frequency-independent (§20), and every 5D-specific number (28%/36%/44.5km) comes from the already-approved `FREQ.6C`/`FREQ.6D.9`/`FREQ.6D.10` authority, not from 4D.

## 43. No reverse-engineered default

Confirmed: the 44.5km plateau target (§22) was not chosen because it makes 32-week arithmetic land neatly — it is the pre-existing, independently-derived-and-approved Core peak reference for this exact runner population, selected *because* it is already-approved authority for the same population, with §21's arithmetic used only to *validate* (per §43's own instruction: "representability arithmetic validates a decision; it does not create one") that this choice produces stable, bounded behavior — not to reverse-engineer the number itself.

---

## 44. Catalog blocker check

**No catalog blocker.** §11 confirms all required content (workouts, allocator parameterization) already exists and is candidate-agnostic. Classification `INTERMEDIATE_5D_LONGHORIZON_GE_BLOCKED_ON_CATALOG_CAPACITY` does **not** apply.

## 45. Numeric blocker check

**No numeric blocker.** §32/§33 close every required numeric decision using only existing authority. Classification `INTERMEDIATE_5D_LONGHORIZON_GE_STRUCTURE_APPROVED_NUMERIC_POLICY_REQUIRED` does **not** apply.

## 46. Product blocker check

**No product blocker for the weekly structure.** §31 closes the structural decision with strong internal precedent (direct generalization of both existing 4D GE and existing 5D Runway). Classification `INTERMEDIATE_5D_LONGHORIZON_GE_PRODUCT_DECISION_REQUIRED` does **not** apply as a residual blocker — the product decision this classification would describe is exactly what §7-§34 of this report closes.

---

## 47. Implementation readiness

All of GE structure (§34), start policy (§35), progression (§36), plateau (§36/§23), long-run (§37), Runway entry (§38), eligibility (§39), and catalog capacity (§44) close in this phase. **`INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTATION_READY`.** The next phase may implement `FREQ.6D.11`'s architecture together with this phase's GE product/numeric policy as a single combined wave (§48).

---

## 48. Next implementation wave contract

Combined implementation wave (not micro-phases), per `FREQ.6D.11`'s own Splits A-D plus this phase's GE closure:

1. EF migration for `LongHorizonRollingSessionState` (5 new nullable columns, `FREQ.6D.11` §27).
2. Persist `LaneOrdinal` + `SlotOrdinal` + `ProgressionStageKey` + `PrescriptionProfileKey`/`Version` (`FREQ.6D.11` §13-§17).
3. JIT key `(StructuralRole, LaneOrdinal, SlotOrdinal)` (`FREQ.6D.11` §28).
4. Exact profile/stage lineage propagation (`FREQ.6D.11` §16-§18).
5. `ExecutionPrescriptionIndex` propagation into the shared `TenKPreparationRunwayDarkOrchestrator` (`FREQ.6D.11` §20/§29).
6. LongHorizon 4D-only cardinality/support-gate generalization (`FREQ.6D.11` §7/§60, this report's §7 `FIXED_5_SESSION_GE` confirmation, and §25's `week.OrderedSlots.Count != 4` generalization).
7. Intermediate×5D GE structure: 1 KEY + 3 EASY + 1 LONG (§34 of this report), generalizing `LongHorizonGeStructuralContracts`/`LongHorizonGeStructuralSelector`'s 4-role table to a resolved-layout-driven role set.
8. Intermediate×5D GE numeric policy: start (§35), progression+plateau (§36), long-run (§37), all sourced from `VolumeSafetyPolicy.FiveDayIntermediate` rather than `Default`.
9. GE→Runway→Core continuity: reuse the existing validator (§25/§38) unmodified, generalized only for the 5-session week-slot count.
10. Dark 21/24/32/52 verification (§49).
11. 4D zero-delta (existing `Default`-backed GE behavior byte-identical for historical/live 4D plans).

Public activation remains a later, separate verification phase (§62 of `FREQ.6D.11`'s own report, unchanged).

---

## 49. Future test manifest

21w/22w/24w/28w/32w/40w/52w × {missing, zero, positive} — missing/zero assert `PRODUCT_INELIGIBLE` uniformly (§39); positive asserts real GE weekly cardinality (5 sessions, §34), volume evolution matching §21's growth-then-plateau curve, plateau reached at 44.5km (§22/§23), GE→Runway boundary continuity (§25/§38), Runway→Core boundary (existing, unmodified), dual-KEY Core entry (`FREQ.6D.11`'s own test plan), Adaptation (existing 5-session table, now genuinely reachable for GE's own load-only interaction, §23), repair (existing `FREQ.6D.11` lineage-preservation rule, extended to GE-originated rolling sessions), real PostgreSQL reload, profile lineage (Legacy throughout GE, ProfileBacked from Core entry onward — the existing Split-C invariant, unchanged), `ExecutionIndex` (Runway/Core segments only — GE itself never needs one, being Legacy-only), catalog version drift (`FREQ.6D.11` §19/§71, unchanged), historical 4D (byte-identical, §6's `Default`-backed behavior untouched), 5D Core/Runway zero-delta (unaffected by any GE-specific decision, §40).

---

## 50. Decision standard — self-check

Every approved rule in this report traces to one of: `DIRECT_CANONICAL_AUTHORITY` (existing 4D GE structure/rules, existing GE fail-closed missing/zero rule, existing Runway/Core numeric authority), `EXISTING_APPROVED_PRODUCT_DEFAULT` (28%/36% share, 44.5km peak reference, all reused from `FREQ.6C`/`FREQ.6D.9`/`FREQ.6D.10`), or `APPROVED_DETERMINISTIC_PRODUCT_BEHAVIOR` (the target-capped plateau model, §22-23, a new but fully-specified, evidence-quantified decision this phase is chartered to make). No rule rests on implementation convenience or accidental runtime behavior alone.

---

## 51. Success classification

**`INTERMEDIATE_5D_LONGHORIZON_GE_PRODUCT_AND_NUMERIC_POLICY_APPROVED`** and **`INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTATION_READY`.**

---

## 52. No code

Confirmed: no production code, test-behavior change, EF migration, routing change, or catalog content was authored this phase. This report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md` are the only files touched.

---

## 53. Ledger / roadmap

See the ledger row and roadmap update accompanying this report.
