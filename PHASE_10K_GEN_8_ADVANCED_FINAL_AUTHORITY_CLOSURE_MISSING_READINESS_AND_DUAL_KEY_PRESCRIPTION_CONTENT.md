# PHASE 10K-GEN.8 — Advanced Final Authority Closure: Missing-Readiness Policy + 5D/6D Dual-KEY Prescription Content

**Parent phase**: `GEN.7` (Advanced axis authority)
**Phase type**: PRODUCT/NUMERIC DECISION + PRESCRIPTION CONTENT AUTHORITY + IMPLEMENTATION-READINESS CLOSURE
**Execution status**: DONE
**Final classification**: `ADVANCED_3D_4D_5D_6D_FULL_IMPLEMENTATION_AUTHORITY_COMPLETE_7D_PRODUCT_NON_SUPPORT`

---

## 0. Precondition verification

`PHASE_LEDGER.md` row 109 confirms `GEN.7` DONE with exactly the two disclosed gaps this phase closes: (A) Advanced missing-readiness = `DECISION_REQUIRED`; (B) Advanced×5D/6D dual-KEY prescription content = `CATALOG_CONTENT_AUTHORITY_REQUIRED`. All other `GEN.7` authority (support matrix, PeakVolumeBands, ResolvedPeakReferences, progression/taper, Runway/GE structure, Adaptation Level-agnosticism, calendar, `NO_GAP` architecture) is re-confirmed unchanged and **not reopened**. Next free phase ID searched: `GEN.8` confirmed unused.

## Part A — Missing-readiness authority

**Decision: `ADVANCED_MISSING_READINESS_PRODUCT_INELIGIBLE`** — Level-owned, frequency-neutral (identical for 3D/4D/5D/6D), horizon-neutral (identical for Core/Runway/LongHorizon). No new numeric constant introduced.

**Provenance — a direct extension of GEN.7's own already-approved zero-readiness rule, not a new independent decision.** GEN.7 froze zero-readiness as `PRODUCT_INELIGIBLE` on definitional grounds: a runner reporting zero recent running cannot be evidenced as Advanced-tier. "Missing" (`RecentWeeklyVolumeKm` simply absent from the request) and "zero" (explicitly reported as 0) are technically distinct representations, but they present the **identical underlying evidentiary problem** — the product has no positive evidence the runner is genuinely Advanced-tier. Applying GEN.7's own already-approved reasoning consistently to both representations of "no positive evidence" is `EXISTING_PRODUCT_SEMANTICS`, not invention — it introduces zero new numbers and creates zero new mechanism, exactly satisfying this phase's own decision standard (§52).

This closes what `GEN.7` explicitly refused to force: rather than inventing a starting-volume default with no provenance, this phase recognizes that a default was never actually necessary — the correct fail-closed extension of already-approved authority was available the whole time, once framed as "does positive evidence exist" rather than "what number should stand in for absent evidence."

**Distinguishing performance evidence from load-readiness evidence** (per §6): `RecentRace` (a past finish time) is not treated as a substitute for `RecentWeeklyVolumeKm` — a fast historical 10K time does not prove current weekly training load, and no repository authority anywhere links `RecentRace`/`TargetFinishTimeSeconds` to load-readiness resolution (confirmed: `PaceSourceResolver`'s race-evidence hierarchy governs *pace* selection only, never volume/readiness). Only `RecentWeeklyVolumeKm` participates in the missing/zero/observed readiness classification.

**Core/Runway/LongHorizon**: identical — positive observed `RecentWeeklyVolumeKm` is required to establish Advanced identity at every horizon; missing or zero both terminate in `PRODUCT_INELIGIBLE` before any plan is generated. No GE-specific fallback is introduced (per §48's explicit prohibition).

**Zero-readiness**: unchanged, `PRODUCT_INELIGIBLE`, not reopened.

## Part B — Advanced 5D/6D dual-KEY prescription content

### Reference matrix (Intermediate's real, current content — `INTERMEDIATE_DUAL_KEY_REFERENCE_MATRIX`)

| Phase/Lane | WorkoutDefinition (version) | Mode | Work | Reps | Recovery | Intensity |
|---|---|---|---|---|---|---|
| Foundation Primary | `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 | Repeated | 30s | 6 | 90s jog, between-reps | Effort: `CONTROLLED_AEROBIC_POWER_INTRO` |
| Foundation Secondary | `THRESHOLD_TEMPO` v5 | Continuous | 1200s | — | none | Effort: `CONTROLLED_THRESHOLD_INTRO` |
| Build Primary | `THRESHOLD_TEMPO` v4 | Continuous | 2400s | — | none | Pace: `THRESHOLD_PACE` |
| Build Secondary | `FARTLEK` v5 | Repeated | 60s | 10 | 60s jog, between-reps | Effort: `SURGE_FASTER_THAN_5K_EFFORT` |
| RaceSpecific Primary | `GOAL_PACE_TEN_K` v2 | Continuous | 1200s | — | none | Pace: `GOAL_PACE_TEN_K` |
| RaceSpecific Secondary | `THRESHOLD_TEMPO` v4 | Continuous | 1500s | — | none | Pace: `THRESHOLD_SUPPORT_PACE` |
| Taper Primary | `GOAL_PACE_TEN_K` v3 | Continuous | 600s | — | none | Pace: `GOAL_PACE_TEN_K` |
| Taper Secondary | `FARTLEK` v5 | Repeated | 20s | 6 | 100s walk, between-reps | Effort: `CONTROLLED_STRIDES_SHARPENING` |

All 8 share identical WARM_UP (600s easy) / COOL_DOWN (300s easy) — universal, not Level-specific content anywhere in the schema.

### Central finding: intensity differentiation is already automatic, no new dose numbers needed

Every workout definition (`FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`, `AEROBIC_STRENGTH_CONTROLLED_INTRO`) carries **zero numeric dose** of its own — no duration, no rep count, no literal pace anywhere (confirmed by direct reading of all 7 workout-definition files: only identity/eligibility/symbolic-descriptor skeleton). All numeric dose lives in the prescription-profile document. Intensity itself is resolved symbolically (`PaceDescriptorKey`/`EffortDescriptorKey`), and `GOAL_PACE_TEN_K`/`THRESHOLD_PACE` resolve dynamically from **each runner's own** `TargetFinishTimeSeconds` (`CatalogSessionPrescriptionPlanner.cs:252-269`: `secondsPerKm = TargetFinishTimeSeconds / GoalDistanceKm`) — a mechanism with zero Level parameter anywhere in it.

This means the **exact same duration/repetition/recovery prescription automatically produces a faster, harder session for an Advanced runner** purely because their own real target time resolves to a faster pace — no separate "Advanced dose" number is needed to express higher intensity. This is `EXISTING_DETERMINISTIC_CROSS_AXIS_AUTHORITY` (the pace-resolution mechanism itself), and using it is the evidence-correct answer, not an evasion: this phase's own §17/§59 explicitly name "simply identical quality dose with higher EASY volume" as a legitimate outcome, and forbids inventing an unsourced "10% harder" adjustment (§33) — which is exactly what a different duration/rep count for Advanced would be, absent a specific rigorously-sourced coaching authority pinning an exact alternative number (none was found or is invented here).

**Decision: Advanced's 8 dual-KEY profiles reuse the exact same structural dose (workout definition, mode, work duration, repetition count, recovery duration/placement, intensity-descriptor semantics) as Intermediate's, verbatim — as new documents under new, Level-owned (not frequency-encoded) keys.** Level differentiation is carried entirely by: (a) the already-approved higher weekly-volume/peak-volume envelope (GEN.7: Advanced 5D peak 50.0km vs. Intermediate 5D peak 44.5km), (b) full/immediate access to the quality-workout catalog with no Foundation-phase deferral (unlike Beginner), and (c) each runner's own faster resolved pace — never a different structural prescription.

### `ADVANCED_DUAL_KEY_FINAL_CONTENT_MATRIX`

| Phase/Lane | New profile key | WorkoutDefinition reused | Dose (identical to Intermediate) | New definition? | New profile? | Approved? |
|---|---|---|---|---|---|---|
| Foundation Primary | `ADVANCED_FOUNDATION_PRIMARY` | `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 | Repeated 30s×6, 90s jog | No | Yes | Yes |
| Foundation Secondary | `ADVANCED_FOUNDATION_SECONDARY_CONTROLLED` | `THRESHOLD_TEMPO` v5 | Continuous 1200s | No | Yes | Yes |
| Build Primary | `ADVANCED_BUILD_PRIMARY` | `THRESHOLD_TEMPO` v4 | Continuous 2400s | No | Yes | Yes |
| Build Secondary | `ADVANCED_BUILD_SECONDARY_CONTROLLED` | `FARTLEK` v5 | Repeated 60s×10, 60s jog | No | Yes | Yes |
| RaceSpecific Primary | `ADVANCED_RACE_SPECIFIC_PRIMARY` | `GOAL_PACE_TEN_K` v2 | Continuous 1200s | No | Yes | Yes |
| RaceSpecific Secondary | `ADVANCED_RACE_SPECIFIC_SECONDARY_CONTROLLED` | `THRESHOLD_TEMPO` v4 | Continuous 1500s | No | Yes | Yes |
| Taper Primary | `ADVANCED_TAPER_PRIMARY` | `GOAL_PACE_TEN_K` v3 | Continuous 600s | No | Yes | Yes |
| Taper Secondary | `ADVANCED_TAPER_SECONDARY_CONTROLLED` | `FARTLEK` v5 | Repeated 20s×6, 100s walk | No | Yes | Yes |

**Exactly 8 new prescription-profile documents required. Zero new workout-definition documents required.**

### 5D/6D reuse (§27/§28)

Per this phase's own instruction to avoid frequency-encoded naming when content is identical: the 8 keys above carry **no `5D`/`6D` token** and are designed as Level-owned documents shared identically by both Advanced×5D and Advanced×6D candidates — mirroring the exact precedent `FREQ.6D.26` already set for Intermediate (whose `INTERMEDIATE_5D_*`-keyed profiles are reused verbatim by its 6D candidate; this phase simply avoids repeating that key's now-inaccurate "5D" token). 6D's only structural difference from 5D (one extra `EASY_SUPPORT` session) does not touch either quality lane.

### Intermediate-vs-Advanced comparison

| Lane | Intermediate | Advanced | Same workout? | Same dose? | Difference | Reason |
|---|---|---|---|---|---|---|
| All 8 lanes | (see reference matrix) | Identical structural dose, new keys | Yes | Yes | None at the structural-dose level | Level differentiation is carried by weekly-volume envelope, full/immediate workout access, and each runner's own resolved pace — not by a different structural prescription (§17/§59; no rigorously-sourced adjustment exists) |

### Execution-model / architecture compatibility (§38-39)

`WorkoutPrescriptionProfile`'s existing schema (`Metadata`, `WorkoutDefinitionRef`, `DoseCategory`, `DistanceAccountingMode`, `Components[]` with `WorkQuantity`/`RecoveryQuantity`/`RecoveryPlacement`/`IntensityTarget`) fully represents every value in the matrix above with **no schema change**. `ExecutionPrescriptionIndex.ResolveExact` is exact-key lookup only (confirmed by its own doc comment: never chooses among candidates) — the 8 new keys slot into this mechanism exactly as the existing Intermediate keys do, no architecture change. `GOAL_PACE_TEN_K` reuse requires no new pace-source semantics — confirmed the pace-resolution chain (`PaceSourceResolver` → `CatalogSessionPrescriptionPlanner`) has zero Level parameter anywhere and reads only `TargetFinishTimeSeconds`/goal feasibility, already compatible with any Level. **No architecture blocker.**

### Taper safety (§37)

Reused verbatim: Taper Primary (600s Goal Pace) and Secondary (20s×6 controlled strides) are already the most reduced-dose entries in the whole matrix relative to Build's (2400s Threshold / 60s×10 Fartlek) — reusing them unchanged preserves the canonical taper load-reduction, introduces no risk of carrying Build-level dose into Taper.

### 3D/4D zero-delta reconfirmation (§40)

Re-confirmed, no contradiction found: 3D/4D use only single-KEY layouts bound directly to the same Level-agnostic workout catalog (`FARTLEK`/`THRESHOLD_TEMPO`/`GOAL_PACE_TEN_K`) Intermediate 3D/4D already use, with no dual-KEY prescription-profile mechanism involved at all. `GEN.7`'s "no new content needed for 3D/4D" conclusion stands unchanged.

## Final implementation-readiness

| Frequency | Support (GEN.7, unchanged) | Missing-readiness (this phase) | Prescription content (this phase) | Readiness |
|---|---|---|---|---|
| 3D | `SUPPORTED` | `PRODUCT_INELIGIBLE` | N/A (no dual-KEY) | `IMPLEMENTATION_READY` |
| 4D | `SUPPORTED` | `PRODUCT_INELIGIBLE` | N/A (no dual-KEY) | `IMPLEMENTATION_READY` |
| 5D | `SUPPORTED` | `PRODUCT_INELIGIBLE` | 8 profiles frozen | `IMPLEMENTATION_READY` |
| 6D | `SUPPORTED` | `PRODUCT_INELIGIBLE` | Same 8 profiles reused | `IMPLEMENTATION_READY` |
| 7D | `PRODUCT_NON_SUPPORT` (unchanged) | N/A | N/A | Not applicable |

No support contradiction found against `GEN.7`'s matrices for Core, Runway, or LongHorizon on any frequency — this phase only closed the two disclosed authority gaps, per its own §45 instruction not to reopen support.

**Advanced authority axis is now `COMPLETE`. Advanced×3D/4D/5D/6D are `FULL_IMPLEMENTATION_AUTHORITY_COMPLETE`. Advanced×7D remains `PRODUCT_NON_SUPPORT`.**

## Governance

No production code, tests, catalog authoring, or migration performed (decision/content-authority only, per this phase's own §51). Intermediate frequency axis, Beginner frequency authority, and Advanced×7D non-support are preserved unchanged.

Per this phase's own §49/§56: since both gaps close completely, no further authority/audit phase is opened. The recommended (not scheduled) next step is a single combined **Advanced 3D+4D+5D+6D combined implementation & dark verification** wave, covering exactly the 24-item scope this phase's own prompt enumerated (candidate identities, eligibility dispatch, the two new authority items closed here, existing progression/long-run reuse, 3D/4D existing content, the 8 new Advanced profiles, Core/Runway/LongHorizon, Adaptation reuse, calendar, persistence, real PostgreSQL, repair, full dark matrices, Beginner/Intermediate zero-delta, 7D unchanged, public gate closed) — followed by one combined public-activation phase. **No phase ID is fabricated — `NEXT_PHASE_NOT_YET_SCHEDULED`.**
