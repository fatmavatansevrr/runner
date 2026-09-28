# PHASE DIST-GEN.15B — 16K × Intermediate × 3D Peak-Volume Exact-Cell Authority Product Decision

## 1. Governing input consumed

DIST-GEN.15A reported `16K_INTERMEDIATE_3D_TARGET_FREQUENCY_PEAK_VOLUME_AUTHORITY_REMAINS_BLOCKED` and, per its own explicit instruction not to select a product default unilaterally, presented a finite three-option candidate envelope for a future explicit product decision, without choosing among them. This phase records that decision, made directly by product authority:

> **WRONG framing** (explicitly rejected): "16K I3D = HM I3D because all 3D peak authority is parent-family-scoped."
> **CORRECT framing** (adopted): 16K I3D's own **exact-cell** authority is `[28,40] / 34 / 20`, frozen as an explicit `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, using the closest available frequency+population+structural-parent anchor after direct exact-cell evidence was genuinely exhausted (per DIST-GEN.15/15A's own research). This does **not** authorize 15K×I×3D, 18K×I×3D, or any other HM-parent×I×3D target.

This correction is the single most important governance content of this phase, and is preserved verbatim in every subsequent section: the decision is **exact-cell authority that happens to numerically equal HM I3D's own values**, never a **parent-family-authority classification** that would imply automatic reuse elsewhere. This is the same "value equality ≠ authority equality" discipline this engagement has applied at every prior closure (most recently to LR-share at 3D in DIST-GEN.15 itself, where level-blind-within-family agreement was carefully distinguished from a target×frequency interaction) — here applied in the opposite direction: numeric equality with a canonical anchor is accepted as a product decision's *starting point*, but explicitly refused as *justification* for calling the field parent-family-owned.

## 2. Repository baseline

Starting commit `80bab3d` on `main`, 0 ahead / 0 behind `origin/main`. This phase is governance-only; it performs zero production edits.

## 3. Why this is EXACT_CELL authority, not PARENT_FAMILY authority

DIST-GEN.15's own methodology audit (§20-24 of that report, consumed and not reopened) classified `PeakVolumeBandMin/Max`, `SelectedPeakReference`, and `CalibrationStartingVolumeKm` as `TARGET_X_FREQUENCY_INTERACTION` / exact-target-owned at 4D, in contrast to LR-share, taper, and horizon-minimum, which were separately and distinctly evidenced as parent-family- or level/frequency-owned. Nothing in DIST-GEN.15A's own research overturned that classification — no new evidence was found showing peak-volume-family fields are governed by parent-family authority in general. What DIST-GEN.15A found was a genuine *evidence gap specific to this one cell*, and what this phase now records is a *product decision that closes that one cell's own gap* by reference to the nearest defensible anchor. The distinction matters concretely: if this were reclassified as parent-family authority, 15K×I×3D and 18K×I×3D would already be implicitly closed by this same decision (since they share the identical HALF_MARATHON parent). They are not. Each would need its own, independent product decision — potentially landing on the same numbers via the same reasoning, potentially not — but never automatically.

## 4. Frozen authority

`PeakVolumeBandMin = 28.0`, `PeakVolumeBandMax = 40.0`, `SelectedPeakReference = 34.0`, `CalibrationStartingVolumeKm = 20.0`, for exactly the cell `TargetDistanceKm=16.0, ParentDistanceFamily=HalfMarathon, Level=Intermediate, RunsPerWeek=3`.

Verified directly against source: `plan-catalog/catalog/policies/peak-volume-bands.v10.json` line 24 confirms canonical HALF_MARATHON×INTERMEDIATE×3 already carries exactly `{"minimumKm": 28, "maximumKm": 40}`; `HalfMarathonIntermediate3D`'s own `VolumeSafetyPolicy` instance carries `SelectedPeakReference=34.0` (the exact midpoint of [28,40], consistent with the band-midpoint convention DIST-GEN.15A's own methodology audit reconstructed) and `GoldenFixtureStartingVolumeKm=20.0`. The product decision adopts these three numbers directly for the new, independent 16K×I×3D exact cell, rather than computing or re-deriving them.

## 5. Confidence tier and reasoning

**`EVIDENCE_INFORMED_PRODUCT_DEFAULT`.** Not `STRONG_EVIDENCE_DERIVED_DECISION` (no direct 16K/10-mile-specific 3-day-Intermediate evidence exists, per DIST-GEN.15/15A's own exhaustive search). Not a bare `PRODUCT_DEFAULT` either, since the choice is not arbitrary within the envelope — it is anchored to the single closest available real reference point on all three dimensions that matter (frequency: exact 3D match; structural parent: exact HALF_MARATHON match, the same parent 16K already uses at every other frequency; population: HM's own Intermediate tier, the same experience-tier label 16K itself uses) after the one dimension that cannot be matched (exact race distance) was shown, via direct research, to have no better-evidenced alternative. This is Option 2 of DIST-GEN.15A's own disclosed three-option envelope, selected by explicit product authority rather than by this phase inventing it.

## 6. Numeric feasibility verification

Performed directly, not assumed, before accepting this decision as implementation-ready:

**Growth trajectory (calibration → peak):** starting at 20.0, applying the preferred weekly-increase rate (0.07, capped at 2.0km/week absolute), the trajectory reaches the 34.0 peak in exactly 8 weekly steps: `20.0 → 21.4 → 22.9 → 24.5 → 26.22 → 28.05 → 30.01 → 32.01 → 34.0`. This fits comfortably within every supported horizon's own non-taper week budget (16K's own horizons run 10-14 weeks, with 2 taper weeks reserved at every horizon per the already-frozen HM-parent taper, leaving 8-12 non-taper weeks — the 8-week minimum trajectory fits exactly at the 10-week minimum horizon and comfortably at every longer one, with the selected-peak-not-chased semantic, §54 of DIST-GEN.15's own governing prompt, correctly allowing the plan to plateau at 34.0 rather than extrapolate past it at longer horizons).

**LR-share interaction with the absolute peak-LR ceiling:** at the 34.0 peak, `LongRunPreferredMinimumShare` (0.34) yields 11.56km, `LongRunPreferredMaximumShare` (0.40) yields 13.6km, `LongRunSelectionShare` (0.38, the value actually used in normal selection) yields 12.92km — all comfortably under the already-frozen `PreferredAbsolutePeakLongRunKm=15.0` ceiling. `LongRunHardCapShare` (0.46) would yield 15.64km, which correctly and expectedly clamps to the 15.0km absolute ceiling — this is not a new defect; it is the identical pattern already present and already correctly handled at 16K's own Intermediate×4D cell (peak 40.0 × hard-cap share 0.40 = 16.0km, likewise clamped below its own 15.0km ceiling). The LR-below-target invariant (15.0 < 16.0) holds throughout, unchanged.

**Taper feasibility:** the already-frozen HM-parent taper (2 weeks, 0.70/0.43, non-chained, applied against the cell's own 34.0 pre-taper anchor) produces Week 1 = 23.8km, Week 2 = 14.62km — both well-formed, both below the 34.0 peak, consistent with the non-chained, fixed-anchor mechanism already governed and not reopened here.

**Inherited production proof:** because this decision adopts HM I3D's own literal band/peak/calibration triple rather than a newly-invented one, the identical calibration→peak growth trajectory, the identical LR-share-to-absolute-cap clamping behavior, and the identical taper application are already exercised, proven, and live in canonical HALF_MARATHON's own production implementation today. The only genuinely new elements for 16K×I×3D are its own already-separately-frozen exact-target fields (`PreferredCoreWeeks=12`/`MaximumCoreWeeks=14` vs HM's own 14/16, and `PreferredAbsolutePeakLongRunKm=15.0` vs HM's own 19.0) and its own target-race-pace/Riegel driving `TargetDistanceKm=16.0` — none of which interact with or complicate the peak-volume/calibration/LR-share numbers just adopted.

**Result: `16K_INTERMEDIATE_3D_NUMERICALLY_FEASIBLE`.**

## 7. Complete implementation-ready authority table

Every row below is unchanged from DIST-GEN.15's own table (§57 of that report) except the four rows this phase closes.

| Field | Value | Owner | Classification | Source |
|---|---|---|---|---|
| TargetDistanceKm | 16.0 | Target | `TARGET_ONLY` | Existing |
| ParentDistanceFamily | HalfMarathon | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §11 |
| Level | Intermediate | — | — | Fixed by experiment scope |
| RunsPerWeek | 3 | — | — | Fixed by experiment scope |
| RunLayout | KEY, EASY, LONG | Frequency | `FREQUENCY_ONLY` | DIST-GEN.15 §13 |
| MinimumCoreWeeks | 10 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §15 |
| PreferredCoreWeeks | 12 | Target | `TARGET_ONLY` | DIST-GEN.15 §16 |
| MaximumCoreWeeks | 14 | Target | `TARGET_ONLY` | DIST-GEN.15 §17 |
| **PeakVolumeBandMin** | **28.0** | **Exact cell (16K×I×3D)** | **`NEW_TARGET_FREQUENCY_CELL_AUTHORITY`** | **This phase, §4-5** |
| **PeakVolumeBandMax** | **40.0** | **Exact cell (16K×I×3D)** | **`NEW_TARGET_FREQUENCY_CELL_AUTHORITY`** | **This phase, §4-5** |
| **SelectedPeakReference** | **34.0** | **Exact cell (16K×I×3D)** | **`NEW_TARGET_FREQUENCY_CELL_AUTHORITY`** | **This phase, §4-5, §6 (band midpoint convention applied)** |
| PreferredWeeklyIncreaseRate | 0.07 | Generic | `GENERIC` | DIST-GEN.15 §25 |
| HardWeeklyIncreaseRate | 0.08 | Generic | `GENERIC` | DIST-GEN.15 §25 |
| AbsoluteWeeklyIncreaseCapKm | 2.0 | Frequency | `FREQUENCY_ONLY` | DIST-GEN.15 §26 |
| **CalibrationStartingVolumeKm** | **20.0** | **Exact cell (16K×I×3D)** | **`NEW_TARGET_FREQUENCY_CELL_AUTHORITY`** | **This phase, §4-5, §6 (sibling-ratio convention applied by direct value adoption, not recomputation)** |
| LR PreferredShareMin | 0.34 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §30 |
| LR PreferredShareMax | 0.40 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §30 |
| LR SelectedShare | 0.38 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §30 |
| LR HardShareCap | 0.46 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §30 |
| PreferredAbsolutePeakLongRunKm | 15.0 | Target | `TARGET_ONLY` | DIST-GEN.15 §32 |
| StartingAbsoluteLongRunCapKm | null | Frequency | `FREQUENCY_ONLY` | DIST-GEN.15 §27 |
| TaperDurationWeeks | 2 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §34 |
| TaperWeek1Multiplier | 0.70 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §34 |
| TaperWeek2Multiplier | 0.43 | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §34 |
| TaperMechanism | Non-chained, fixed-anchor | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §35 |
| PrimaryProgression | HALF_MARATHON_MASTER v1 (reused) | Parent | `PARENT_FAMILY_ONLY` | DIST-GEN.15 §36 |
| QualitySessionCount | 1 (KEY only, no KEY2) | Frequency | `FREQUENCY_ONLY` | DIST-GEN.15 §37 |
| RaceSpecificPaceSemantic | Generic, TargetDistanceKm=16.0 | Generic | `GENERIC` | DIST-GEN.15 §39 |
| RecentRaceEquivalenceMethod | Riegel, unmodified | Generic | `GENERIC` | DIST-GEN.15 §40 |
| GoalFeasibilityBehavior | Generic, unmodified | Generic | `GENERIC` | DIST-GEN.15 §41-42 |
| ReadinessModel | Generic, unmodified | Generic | `GENERIC` | DIST-GEN.15 §43 |
| CalendarReuse | Generic 3D calendar machinery | Frequency | `FREQUENCY_ONLY` | DIST-GEN.15 §46 |
| AdaptationReuse | Generic, unmodified | Generic | `GENERIC` | DIST-GEN.15, unaffected |

**No previously-resolved row was changed.**

## 8. Before/after blocker table

| Field | DIST-GEN.15A status | This phase's value | Authority owner | Confidence tier |
|---|---|---|---|---|
| PeakVolumeBandMin | `DECISION_REQUIRED` | **28.0** | Exact cell (16K×I×3D) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| PeakVolumeBandMax | `DECISION_REQUIRED` | **40.0** | Exact cell (16K×I×3D) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| SelectedPeakReference | `DECISION_REQUIRED` | **34.0** | Exact cell (16K×I×3D) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |
| CalibrationStartingVolumeKm | `DECISION_REQUIRED` | **20.0** | Exact cell (16K×I×3D) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` |

## 9. Non-extension to other cells

Explicitly and emphatically, per the governing input itself: this decision does **not** authorize `15K × Intermediate × 3D`, `18K × Intermediate × 3D`, or any generalized "HM-parent × Intermediate × 3D" peak-volume authority. Each such cell, if ever pursued, would require its own independent product decision — which may or may not land on the same numbers, and must not be inferred, assumed, or silently pre-authorized by this one. `ProjectedTargetAuthorityRegistry` (unmodified by this governance-only phase) has exactly zero registrations beyond the existing 15K/16K/18K×I×4D three; this phase adds no registration and creates no cell — it only completes the authority table a future implementation phase would need for the one cell it addresses.

## 10. Orthogonality and registry status preserved

`PROJECTED_DISTANCE_AND_FREQUENCY_PARTIALLY_ORTHOGONAL` (DIST-GEN.15 §61) and `16K_INTERMEDIATE_3D_FITS_EXISTING_PROJECTED_TARGET_REGISTRY_ARCHITECTURE` (DIST-GEN.15 §62) both remain accurate and unchanged. Closing the previously-blocked fields via an explicit exact-cell product decision does not retroactively make the system "fully orthogonal" — the fact that this cluster needed its own dedicated, disclosed decision (rather than composing for free from existing shared authority) is itself the concrete evidence of the partial, not full, orthogonality result.

## 11. Zero-delta confirmation

Confirmed via `git status --porcelain -- backend plan-catalog mobile PHASE_LEDGER.md MASTER_ROADMAP.md`: zero production, catalog, database, API, or frontend file changed. Canonical TEN_K and HALF_MARATHON authority (including the very values read and adopted here) remain completely untouched. The existing public/dark matrices remain exactly `{15K, 16K, 18K} × Intermediate × 4D` on both sides; 16K×Intermediate×3D remains entirely unimplemented — this phase closes authority only, it does not create a cell, register anything, or activate anything.

## 12. DIST-GEN.16 readiness

**`DIST_GEN_16K_INTERMEDIATE_3D_READY_FOR_DARK_IMPLEMENTATION`.** Every field in the complete authority table (§7) is now closed with an honest, disclosed confidence tier and a verified feasibility proof (§6). DIST-GEN.16 (16K × Intermediate × 3D full dark projected-distance frequency-orthogonality implementation) can proceed without making any further training-science decision — it needs only to register the one new cell in `ProjectedTargetAuthorityRegistry` (and, separately, `ApprovedDarkCells`) using the values this and the preceding two phases have frozen, exactly as every prior dark-implementation phase (DIST-GEN.6, DIST-GEN.10) has done for its own newly-closed target. Public activation is explicitly not implied or authorized by this phase.

## 13. Remaining blockers

None.

---

## Final classification

**16K_INTERMEDIATE_3D_PROJECTED_DISTANCE_FREQUENCY_ORTHOGONALITY_AUTHORITY_CLOSED**

The complete authority table for `16K × Intermediate × 3D` is now closed: every field DIST-GEN.15 left resolved remains unchanged and re-confirmed (§7); the four fields DIST-GEN.15/15A left blocked are now frozen via an explicit product decision, correctly classified as exact-cell authority (not parent-family authority, per §3's own central distinction) at an honestly-disclosed `EVIDENCE_INFORMED_PRODUCT_DEFAULT` tier; numeric feasibility is verified directly, not assumed (§6), and is additionally corroborated by the fact that the adopted numbers are already proven live in canonical HALF_MARATHON's own production implementation; this decision explicitly does not extend to 15K×I×3D, 18K×I×3D, or any other HM-parent×I×3D cell (§9); the standing `PROJECTED_DISTANCE_AND_FREQUENCY_PARTIALLY_ORTHOGONAL` and registry-architecture results are correctly preserved, not reopened (§10); and zero production, catalog, database, API, or frontend change occurred (§11). DIST-GEN.16 may now proceed to dark implementation with no remaining training-science decision. DIST-GEN.16 itself is not begun here.
