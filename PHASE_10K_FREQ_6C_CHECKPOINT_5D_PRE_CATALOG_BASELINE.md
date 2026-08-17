# Phase 10K-FREQ.6C.CHECKPOINT — Intermediate 5D Pre-Catalog-Implementation Baseline

**Documentation consolidation only. No new research, no new decisions, no code touched. Every claim traces to a real phase document; two claims in this phase's own prompt didn't check out — flagged in §21, not silently accepted.**

## 1. Phase ledger (FREQ.1 → FREQ.6C)

| Phase | Outcome | Final classification |
|---|---|---|
| FREQ.1 | Resolved the 3D/4D trajectory-shape divergence as intentional, evidence-grounded (not accidental); closed `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` | `TRAJECTORY_SHAPE_AUTHORITY_DETERMINED` (`INTENTIONAL_FREQUENCY_POLICY`) |
| FREQ.2 | Found Runway architecturally hardcoded to Intermediate×4D, not generically gated; Beginner×3D non-viable at Runway for two independent reasons | `BEGINNER_3D_RUNWAY_NON_REPRESENTABLE` |
| FREQ.2A | Verified no live-cell exposure risk from FREQ.2's finding; added 2 permanent regression tests | `NO_EXPOSURE_CONFIRMED_SAFE` |
| FREQ.3 | Resolved 5D trajectory authority (Frequency-owned, reuse 4D shape); found 4 real single-KEY cardinality defects (Sections E/F/H) | `INTERMEDIATE_5D_ARCHITECTURE_AUDIT_BLOCKED_ON_SECTION_E_SECTION_F_SECTION_H` |
| FREQ.4 | Generalized all found cardinality defects to N≥1, verified N=1 no-op by full regression; found and fixed a real record-equality bug along the way | `5D_STRUCTURAL_CARDINALITY_GENERALIZED_AND_VERIFIED` |
| FREQ.4A | Found FREQ.4's "no test precedent" disclosure was itself incorrect; closed the gap with real DB-backed coverage through the actual repair pipeline | `KEY_KEY_SPACING_REAL_COVERAGE_CONFIRMED` |
| FREQ.5 | Evidence envelopes for 5D severity model and KEY1/KEY2 pairing; no selections made | `INTERMEDIATE_5D_SEVERITY_AND_PAIRING_EVIDENCE_READY_FOR_PRODUCT_DECISION` |
| FREQ.6 | Selected the full 5D severity/pairing product policy (24-item decision inventory); found a material non-catalog numeric-authority gap | `INTERMEDIATE_5D_PRODUCT_POLICY_APPROVED_WITH_ARCHITECTURE_BLOCKER` |
| FREQ.6A | Found the catalog cannot truthfully represent the approved policy; selected the generic coordinated-lane architecture to fix it | `INTERMEDIATE_5D_CATALOG_CAPABILITY_APPROVED_WITH_IMPLEMENTATION_GAP` |
| FREQ.6B | Real evidence envelopes for 4 missing numeric authorities (starting volume, peak reference, allocation, long-run share); flagged one taper-floor conflict risk | `INTERMEDIATE_5D_NUMERIC_AUTHORITY_EVIDENCE_READY_FOR_PRODUCT_DECISION` |
| FREQ.6C | Selected final numeric values; computed the real 8-14wk matrix — all 14 cells ELIGIBLE, no conflict materialized | `INTERMEDIATE_5D_NUMERIC_AUTHORITY_APPROVED` |

## 2. RUN_LAYOUT_5D (structural, frozen)

`2 × KEY_SESSION + 2 × EASY_SUPPORT + 1 × LONG_RUN`. Cited: FREQ.3's audit target throughout; FREQ.6 §2 restates it as binding, unreopened.

## 3. Trajectory authority

Frequency-owned. 5D reuses 4D's fixed-reference linear-interpolation shape — a *reasoned*, not directly evidenced, conclusion: FREQ.1's finding that 3D's compounding shape is grounded in "how a low-frequency, few-session structure concentrates load week-to-week" points, applied consistently (not by nearest-frequency analogy), *away* from compounding as session count increases past 4. Cited: FREQ.3 §A, re-confirmed without reopening in FREQ.6B §1.

## 4. Adaptation severity matrix

Model **A4** semantics (monotonic count floor + role-aware high-adherence gate), represented as **A5** (finite 24-row state table). `0-1 → Reduce`; `2-3 → Maintain`; `4 → Maintain, unless the sole miss is Easy → Progress`; `5 → Progress`. KEY1/KEY2 severity-symmetric (prescription priority does not grant adherence weight); LONG is a high-tier discriminator only, never an independent numeric weight or safety gate. Cited: FREQ.6 §§3-6.

Full 24-row vector table, reproduced exactly (FREQ.6 §6):

| Count | K1 | K2 | LONG | E | Outcome | Rule |
|---:|:---:|:---:|:---:|---:|---|---|
| 0 | N | N | N | 0 | Reduce | count 0–1 |
| 1 | N | N | N | 1 | Reduce | count 0–1 |
| 2 | N | N | N | 2 | Maintain | count 2–3 |
| 1 | N | N | Y | 0 | Reduce | count 0–1 |
| 2 | N | N | Y | 1 | Maintain | count 2–3 |
| 3 | N | N | Y | 2 | Maintain | count 2–3 |
| 1 | N | Y | N | 0 | Reduce | count 0–1 |
| 2 | N | Y | N | 1 | Maintain | count 2–3 |
| 3 | N | Y | N | 2 | Maintain | count 2–3 |
| 2 | N | Y | Y | 0 | Maintain | count 2–3 |
| 3 | N | Y | Y | 1 | Maintain | count 2–3 |
| 4 | N | Y | Y | 2 | Maintain | high-tier role gate |
| 1 | Y | N | N | 0 | Reduce | count 0–1 |
| 2 | Y | N | N | 1 | Maintain | count 2–3 |
| 3 | Y | N | N | 2 | Maintain | count 2–3 |
| 2 | Y | N | Y | 0 | Maintain | count 2–3 |
| 3 | Y | N | Y | 1 | Maintain | count 2–3 |
| 4 | Y | N | Y | 2 | Maintain | high-tier role gate |
| 2 | Y | Y | N | 0 | Maintain | count 2–3 |
| 3 | Y | Y | N | 1 | Maintain | count 2–3 |
| 4 | Y | Y | N | 2 | Maintain | high-tier role gate |
| 3 | Y | Y | Y | 0 | Maintain | count 2–3 |
| 4 | Y | Y | Y | 1 | Progress | high-tier role gate |
| 5 | Y | Y | Y | 2 | Progress | full completion |

## 5. KEY1/KEY2 prescription policy

`PRIMARY`/`SECONDARY_CONTROLLED` are prescription-priority/progression-lane provenance labels, never a structural role — both slots remain `KEY_SESSION` for every structural and adaptation purpose. Cited: FREQ.6 §10-11, FREQ.6A §2/§10 confirm the same.

| Phase | KEY1 / PRIMARY purpose | KEY2 / SECONDARY_CONTROLLED purpose |
|---|---|---|
| Foundation | Controlled aerobic-strength/economy stimulus: short hills/strides, non-exhaustive | Controlled threshold introduction at lower fatigue/dose |
| Build | Controlled threshold/MIT development | Controlled fartlek/VO2-oriented support, lower accumulated stress than primary |
| RaceSpecific | 10K-specific rehearsal | Controlled threshold support |
| Taper | Reduced-dose 10K-specific sharpening | Reduced-dose economy/strides sharpening, secondary controlled |

## 6. Taper policy

`RETAIN_TWO_KEY_EXPOSURES` — both remain structural KEY roles in Taper; KEY1 reduced-dose race-specific sharpening, KEY2 lower-dose economy/strides sharpening; existing total taper multiplier (`0.53`, unchanged) and structural authority untouched. Cited: FREQ.6 §12, FREQ.6A §12 (§9 in FREQ.6A's own numbering) confirms the frozen phase purposes.

## 7. Numeric authorities — final selected values

| Authority | Value | Provenance |
|---|---|---|
| Missing-readiness starting volume | 26.0 km | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` |
| Explicit-zero starting volume | 19.5 km | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE`, weaker evidentiary basis (extrapolated ratio) |
| Resolved peak reference | 44.5 km | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` — **never** `GoldenFixtureDerived` |
| KEY2:KEY1 relative dose | 70% | `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, categorical per FREQ.6 §13 (see §10 below — this is not FREQ.4's live mechanism) |
| Long-run selection share | 28% | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` |
| Long-run hard cap | 36% | `PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE` |

Cited exactly: FREQ.6C §A.

## 8. Approved starting-volume values (distinct citation)

26.0km (missing-readiness) anchored directly on Hal Higdon's real, directly-fetched Intermediate 10K program's Week-1 5-day total; 19.5km (explicit-zero) = `26.0 × 0.75`, reusing 4D's own real missing:explicit-zero ratio (16:12=0.75) rather than an independent source, since FREQ.6B disclosed no direct 5-day explicit-zero source exists. Cited: FREQ.6B §2, FREQ.6C §A.

## 9. Approved peak/reference values (distinct citation)

44.5km, center of Higdon's real peak-week estimate (43-46km), confirmed to fall inside the real, pre-existing `36-50km` band already present in `peak-volume-bands.v4.json`. Explicitly **not** derived from Intermediate×4D's 38km by ratio or analogy — checked against the band only for plausibility after independent derivation, per the standing prohibition (GEN.4C.2/4C.3, re-confirmed FREQ.6B §3). Cited: FREQ.6B §3, FREQ.6C §A.

## 10. Allocation authority — two different layers, not conflated

- **FREQ.4's real, currently-running mechanism** (`FourDaySessionDistanceAllocationPolicy`/`V1FourDaySessionVolumeAllocationPolicy`, actually implemented and regression-tested): equal distance split across N KEY instances. Unmodified by FREQ.6/6A/6B/6C.
- **The future asymmetric dose authority** (70% KEY2:KEY1, FREQ.6C §A): a stored numeric target for the *not-yet-built* prescription-profile capability FREQ.6A found missing (§14 below) — has no runtime effect today, since no code consumes it yet.

These are explicitly different layers. Cited: FREQ.6C §D.

## 11. Long-run authority

28% selection / 36% hard cap. Margin (8 points) chosen in the same style as 4D's own real 7-point margin (33%→40%); confirmed `28% < 36%` with real margin, not a boundary case. Cited: FREQ.6C §A.

## 12. 8-14wk representability conclusion

**All 14 cells ELIGIBLE** (7 horizons × 2 readiness states). No horizon routes to typed `PRODUCT_INELIGIBLE`. Reproduced exactly (FREQ.6C §C):

| Horizon | Missing-readiness | Explicit-zero |
|---:|---|---|
| 8 | ELIGIBLE | ELIGIBLE |
| 9 | ELIGIBLE | ELIGIBLE |
| 10 | ELIGIBLE | ELIGIBLE |
| 11 | ELIGIBLE | ELIGIBLE |
| 12 | ELIGIBLE | ELIGIBLE |
| 13 | ELIGIBLE | ELIGIBLE |
| 14 | ELIGIBLE | ELIGIBLE |

## 13. KEY2 floor edge case (flag, not a defect)

At the *theoretical* structural floor (`keyTotal` at its absolute 6.0km minimum — never actually realized anywhere in the real computed 8-14wk matrix) combined with the *low end* of the dose-ratio envelope (60%, not the selected 70%), KEY2 would compute to ~2.25km, below its own 3.0km per-session minimum. Not realized in this closure's real numbers (Intermediate's actual volumes stay well clear of the structural floor throughout). Flagged explicitly for whoever eventually implements the asymmetric-allocation mechanism — that implementation will need its own floor-priority handling for KEY2, mirroring EASY's existing floor protection, not assumed safe by extrapolation. Cited exactly: FREQ.6C §D.

## 14. Exact remaining catalog blockers (FREQ.6D's real scope)

Reproduced from FREQ.6A §16 (`GENERIC_CODE_EXTENSION` rows) and §19 (required-capability matrix), in full:

| Area | Classification | Required impact |
|---|---|---|
| Catalog schema | `GENERIC_CODE_EXTENSION` | New versioned prescription-profile schema and lane-capable progression schema |
| Catalog validator | `GENERIC_CODE_EXTENSION` | Typed quantity/recovery validation; lane cardinality, priority, eligibility, fallback, coordination invariants |
| Workout binder | `GENERIC_CODE_EXTENSION` | Bind same-role slot ordinal to lane and exact profile; preserve provenance |
| Progression resolver | `GENERIC_CODE_EXTENSION` | Allocate/compress/extend stages per coordinated lane |
| Manifest graph | `GENERIC_CODE_EXTENSION` | Resolve profile→definition and progression→profile versioned dependencies |
| Runtime validation | `GENERIC_CODE_EXTENSION` | Require every lane/profile; validate materialized typed prescription/distance accounting |
| Dated skeleton validation | `NO_CHANGE` | Already validates generic KEY cardinality/spacing (FREQ.4); must not interpret priority |
| Adaptation role lineage | `NO_CHANGE` | Both KEY slots remain symmetric `KEY_SESSION` inputs |
| Persistence | `GENERIC_CODE_EXTENSION` | Persist lane/profile/priority provenance for replay, without changing the structural-role enum |

§19's per-slot required-capability matrix (Foundation KEY1/KEY2, Build KEY1/KEY2, RaceSpecific KEY1/KEY2, Taper KEY1/KEY2, dual-KEY weekly progression, fallback, dose scaling) is all marked `Blocking? = Implementation` — every row is real, outstanding work, not summarized away here since FREQ.6D needs the complete list, not a paraphrase.

## 15. FARTLEK gap

Referencable by a KEY progression stage; definition has no role field; Build-phase-eligible only. Current prescription data is modes/accounting-mode/ordered-component-labels only — **not representable**: no repetition count, work duration/distance, or recovery dose; intensity is free-text strings (`EASY`, `SURGE_AND_FLOAT`, `EASY_JOG`); no dose variants or taper reduction; one candidate in a single phase-stage sequence; not safe as KEY2 (lower-stress complement cannot be encoded). Cited exactly: FREQ.6A §3, §7.

## 16. THRESHOLD gap

Same structural limitation: Build/RaceSpecific-eligible, modes/accounting-mode/component-labels only, no repeats/work-duration/distance/recovery representable, `EASY`/`THRESHOLD` intensity strings only, no dose variants or approved minimum, compatible only as the single selected weekly stage, not distinguishable as a controlled/lower-dose KEY2. Root-cause classified **F — MULTIPLE LAYERS** (schema cannot represent the structure; artifacts consequently incomplete; binder/planner cannot materialize it; loader/runtime drops or lacks the contract) — a schema/progression/runtime architecture gap, not merely a data gap. Cited exactly: FREQ.6A §3, §4, §16.

## 17. Dual-KEY progression gap

Current capability: **NO** — "one phase/week cannot resolve two coordinated KEY prescriptions independently." Cited exactly, with its four supporting evidence bullets (FREQ.6A §11):

- `TEN_K_WORKOUT_PROGRESSION v5` has one ordered stage sequence per phase and no lane or slot ordinal.
- The stage allocator emits one `ProgressionStageKey` for the week.
- `CatalogWorkoutBinder` applies that same stage to every stage-controlled KEY slot — it requires exactly one candidate and rejects multiple as ambiguous, so two KEY slots receive the same workout today.
- `CatalogSessionPrescriptionPlanner` has a KEY ordinal only for distance-array lookup (FREQ.4's real, live fix), not workout/progression selection.

## 18. FREQ.6D expected blast radius

Reproduces §14's table verbatim as the explicit scope boundary — see §14 above (not duplicated a second time in this document; §14 *is* this section's content, per FREQ.6A §16).

## 19. Frozen non-goals for FREQ.6D

> FREQ.6D must not revisit any FREQ.6/FREQ.6A/FREQ.6C product or numeric decision merely to make catalog implementation easier. If FREQ.6D finds that an approved decision is genuinely unimplementable as specified (not just inconvenient), it must stop and report a specific conflict for a new, narrow decision phase — mirroring how FREQ.6's own decisions were never silently adjusted to fit prior code shape.

Also frozen: no `RUN_LAYOUT_5D` public activation; no numeric value changes (§7-9, §11); no severity-matrix changes (§4); no KEY1/KEY2 purpose changes (§5); no taper-policy changes (§6).

## 20. Regression baselines and commit state

**Last real, verified full-backend regression count**: **3480/3480 passed, 0 failed**, cited from FREQ.4A (the last phase in this chain that actually ran the full suite; FREQ.5/FREQ.6/FREQ.6A/FREQ.6B/FREQ.6C are pure research/decision phases with no code changes, so this count has not been superseded by any later real run). **Not re-run for this checkpoint**, per explicit instruction.

**Commit state**: verified via `git status`/`git log` only (no test re-run). `git log` top commit is still `1b07cf8` (CHECKPOINT.1D's snapshot commit) — nothing has been committed since. **137 paths currently uncommitted** (every FREQ.1-FREQ.6C document plus FREQ.4/FREQ.4A's real code and test changes), sitting on top of CHECKPOINT.1D's already-disclosed "attribution cleanup deferred" state, which remains current and unchanged. `git log origin/main..main` still shows 32 local-only commits — nothing pushed.

## 21. Technical-debt inventory refresh — two real discrepancies found, not silently resolved

| ID | Status (as last stated) | Source |
|---|---|---|
| `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` | Closed by FREQ.1 (`INTENTIONAL_FREQUENCY_POLICY`, not unification-requiring debt) | FREQ.1, restated non-blocking FREQ.6 §21 |
| `TD-5D-SEVERITY-THRESHOLD-GENERALIZATION-001` | `RESOLVED_BY_FREQ6`; history preserved until FREQ.7 implements | FREQ.6 §21 |
| `TD-KEY-LONG-SPACING-TEST-NEVER-TESTS-REAL-VIOLATION-001` | Preserved — a distinct history from FREQ.4A's KEY↔KEY closure | FREQ.6 §21 |
| `TD-RUNWAY-ARCHITECTURE-HARDCODED-SINGLE-CELL-001` | Preserved (real, from FREQ.2's finding) | FREQ.6 §21 |
| Legacy-fallback-path debt | **Naming discrepancy found, not silently resolved**: GEN.CHECKPOINT.1 §H originally registered this as `TD-LEGACY-FALLBACK-PATH-UNTRACED-001`; FREQ.6 §21 instead references `TD-LEGACY-FALLBACK-NO-SILENT-COERCION-001` and itself hedges with "preserved **if present**" — suggesting FREQ.6 was not fully certain of its own reference either. Both IDs describe the same real, still-untraced underlying gap (the exact status/message a Legacy-routed request receives). **Not merged or renamed here** — flagged for whoever owns debt-tracking to reconcile into one ID. |
| Peak-volume-band provenance gap (Intermediate 4D 30-42km range, 5D 36-50km range) | Still real, still `UNTRACED` origin — neither has ever been evidence-derived within this engagement (5D's 36-50km band predates FREQ.6B, which only *checked* the selected 44.5km reference against it, never derived the band itself) | GEN.CHECKPOINT.1 §D/§H, unchanged |
| Cross-training evidence-comparison caveat (Beginner×3D) | Unchanged | GEN.CHECKPOINT.1 §H |

**`TD-MISLEADING-FOURDAY-NAMING-001` — this exact ID does not exist anywhere in the repository.** Checked directly (`grep` across every phase document): zero matches. The real, underlying concern is genuine and was disclosed in FREQ.4 (`V1FourDaySessionVolumeAllocationPolicy`'s name now misdescribes a 2-KEY, 5-session shape; a rename was considered and deliberately deferred, documented inline in the type's own doc comment) — but it was never assigned a formal tracked `TD-` identifier. **Not fabricated here to fill the gap.** If this concern should be formally tracked, that is a decision for whoever owns this repository's debt-numbering convention, not something to be silently minted by this checkpoint.

## 22. Final classification

```
INTERMEDIATE_5D_PRE_CATALOG_IMPLEMENTATION_BASELINE_FROZEN
```

Every section above traces to a real, specific phase document. Two discrepancies in this checkpoint's own source prompt were found and disclosed rather than silently accepted: a fabricated tech-debt ID (`TD-MISLEADING-FOURDAY-NAMING-001`, never assigned) and a real naming mismatch between two different IDs used for the same legacy-fallback concern across GEN.CHECKPOINT.1 and FREQ.6. No new research, no new decisions, no code touched, no test re-run.
