# PHASE HM.6 — Half-Marathon Intermediate 3D/5D Frequency Expansion Authority and Reuse Audit

**Type**: AUDIT / GOVERNANCE (no production code change; no new catalog combinations; no public activation)
**Execution Status**: DONE (BLOCKED — both target frequencies require new, undecided HM authority before dark implementation)

---

## 1. Phase ID / ledger confirmation

`PHASE_LEDGER.md` was read in full down to its last row: Seq 21, `HM.5.3`, `DONE`, `HM_DYNAMIC_CORE_VOLUME_AND_LONG_RUN_BOUNDARIES_CLOSED_ZERO_DELTA`. No `HM.6` row exists. **HM.6 is confirmed as the correct, next-free phase ID** — the governing prompt's assumption was correct and required no substitution. This report supersedes nothing; it opens a new phase.

## 2. Current HM 4D baseline (treated as closed, not reopened)

Directly re-verified in `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`, the single named `HalfMarathonIntermediate4D` instance:

| Field | Value |
|---|---|
| PreferredMaxWeeklyIncreaseRatio | 0.07 |
| HardMaxWeeklyIncreaseRatio | 0.08 |
| AbsoluteWeeklyIncrementCapKm | 2.5 |
| GoldenFixtureStartingVolumeKm | 25 |
| ResolvedPeakReference | 43.0 (ProductDefaultWithEvidenceEnvelope) |
| ResolvedPeakReferenceIsSelectedCeiling | **true** (HM.5.3 — the only named policy where this is true) |
| GoldenFixtureNonTaperTransitions | 11 |
| TaperVolumeMultipliers | [0.70, 0.43] (non-chained, `HalfMarathonIntermediate4DTaperVolumePolicy`) |
| LongRun Preferred Min/Max | 0.30 / 0.36 |
| LongRun Selection / HardCap share | 0.33 / 0.40 |
| PreferredAbsolutePeakLongRunKm | 19.0 (ceiling, not target) |

Phase headroom confirmed directly in `plan-catalog/catalog/templates/half-marathon-master.v1.json`: Foundation 2/3/4, Build 3/4/5, RaceSpecific 3/5/5, Taper 2/2/2 (min/preferred/max). CoreCycle min 10 / default 14 / max 16. `supportedRunsPerWeek: [4]` — **only 4 is declared today**. This entire baseline is treated as closed and was not touched.

## 3. Real 3D/4D/5D RunLayout comparison (HIGH-PRIORITY finding, directly read)

| File | runsPerWeek | Slots (in order) |
|---|---|---|
| `run-layouts/run-layout-3d.v1.json` | 3 | KEY_SESSION, EASY_SUPPORT, LONG_RUN |
| `run-layouts/run-layout-4d.v1.json` (the version HM's own `HALF_MARATHON__4D__INTERMEDIATE v1` combination actually references) | 4 | KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN |
| `run-layouts/run-layout-5d.v1.json` | 5 | **KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN** |

**Confirmed, not assumed**: 3D = 1 KEY + 1 EASY + 1 LONG (matches the governing prompt's default assumption). 5D = **two** KEY_SESSION slots + 2 EASY + 1 LONG — it does **not** match a naive "KEY+3EASY+LONG" assumption. This is the single most consequential structural fact in this audit: any HM×Intermediate×5D dark cell inherits a RunLayout with a second quality-capable lane, and everything downstream (workout progression, hard-session budget, session allocation) must answer what fills it.

(A second `run-layout-4d.v2.json`, status `DRAFT`, also exists with the same 1K+2E+1L shape — not the version HM references; noted for completeness, not consumed.)

## 4. 10K frequency mechanism precedent — MECHANISM_REUSE vs NUMERIC_INHERITANCE

Confirmed directly in code that 10K's dual-KEY 5D content is expressed through a **structurally different progression schema** than the one HM currently uses:

- `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v7.json` (10K's dual-KEY-capable progression) is organized per phase as a `"lanes"` array, each lane carrying its own `laneOrdinal` (0 = primary, 1 = secondary) and its own stage list (e.g. `FOUNDATION_PRIMARY_STAGE` / `FOUNDATION_SECONDARY_STAGE`, `BUILD_PRIMARY_STAGE` / `BUILD_SECONDARY_STAGE`, etc.).
- `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` has **no `lanes` concept at all** — each phase carries one flat `stages` array (`FOUNDATION_AEROBIC_BASE`, `BUILD_FASTER_THAN_HM_INTRO`/`BUILD_THRESHOLD_DEVELOPMENT`, the `RACE_SPECIFIC_HM_*` stages, the `TAPER_HM_*` stages) — this is genuinely single-lane by schema, not merely by data.
- The consuming engine, `CatalogSessionPrescriptionPlanner.cs`, already reads a generic `session.LaneOrdinal` for `KEY_SESSION` roles and indexes `allocation.KeySessionDistancesKm[keySessionOrdinal]` — i.e. the **session-allocation mechanism** is already lane-count-generic (MECHANISM_REUSE available). What is **not** generic is the **content authority**: HM's progression file has never had to say what goes in a second KEY lane, because 4D only ever had one.
- 10K's per-frequency `VolumeSafetyPolicy` dispatch is a clean `switch`-based function (`ForIntermediateDaysPerWeek(daysPerWeek)`); no such HM dispatcher exists (see §19/§20).
- No `FiveDaySessionVolumeAllocationPolicy`/`ThreeDaySessionVolumeAllocationPolicy`-shaped HM class exists; only 10K-named `V1ThreeDaySessionVolumeAllocationPolicy` and `V1FourDaySessionVolumeAllocationPolicy` were found under `Prescription/Session/`. Whether HM's existing, already-dark 4D cell depends on any 10K-named session policy, or runs entirely through the frequency-generic engine HM.5.1–HM.5.3 hardened, was **not exhaustively traced this phase** — flagged honestly as a residual verification item rather than asserted either way (see §12).

Classification for every 10K item inspected: RunLayout representation, role/cardinality mechanism, the lane/laneOrdinal session-allocation mechanism, and the phase/stage allocator itself are **MECHANISM_REUSE** (already distance-blind by construction, confirmed via HM.5.2/HM.5.2A's own "zero HM-specific branch in `ProgressionStageAllocator.cs`" grep-proof, re-spot-checked this phase). Every 10K **numeric** value (peak volume, long-run share, taper multipliers, session minima) is explicitly **NOT** inherited — none of it appears anywhere in the frozen HM 4D authority, and this phase invents none of it for 3D/5D either.

## 5. HM workout-progression frequency compatibility

- **3D**: The existing flat, single-lane `HALF_MARATHON_WORKOUT_PROGRESSION v1` requires exactly one KEY_SESSION-shaped lane per week, which 3D's RunLayout supplies exactly one of. Structurally, 3D can consume the **same progression document unmodified** — no second-lane question arises. This is the strongest reuse case in the entire audit.
- **5D**: Cannot be answered "yes" without inventing content. The progression document has no lane concept; RunLayout 5D has two KEY_SESSION slots. Filling the second lane with **anything** (a repeat of the first lane's stage, a distinct new stage family, or leaving it deliberately unfilled by demoting the second KEY slot to an EASY-shaped session at the prescription layer) is a **new HM workout-progression authority decision**, not a reuse of existing content. The prompt's own instruction not to infer a second quality session merely because 5D has more running days is honored: no such inference is made here.
- Whether "extra frequency can be satisfied entirely by EASY_SUPPORT without changing HM quality count" is **structurally blocked** for 5D specifically because RunLayout 5D's second slot is typed `KEY_SESSION`, not `EASY_SUPPORT`, in the catalog document itself — changing that typing would itself be a RunLayout-authoring decision (out of scope: RunLayout documents are shared, immutable, 10K-proven artifacts, not to be forked or mutated per distance family without cause).

## 6. Hard-session/KEY-lane analysis

- **3D**: exactly one structurally possible hard stimulus per week (one KEY_SESSION slot); LONG remains an easy-effort role by construction; the existing single-lane HM progression already caps hard-session count at 1. `HM_INTERMEDIATE_3D_HARD_SESSION_COUNT`: **GENERIC_REUSE**, no decision required.
- **5D**: architecture supports up to 2 hard stimuli per week (two KEY_SESSION slots exist), but no HM research or governance phase in this engagement (HM.0 through HM.5.3) approved a second hard session for Intermediate HM at any frequency. Per the prompt's own instruction, this is classified exactly as directed: **`HM_5D_SECOND_HARD_STIMULUS_AUTHORITY_MISSING`**. The second KEY lane is architecturally fillable but not HM-authorized, and this phase does not fill it from 10K precedent.

## 7. Peak-volume authority audit

`HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2 was read directly (not paraphrased). It contains, verbatim:

```
                  3D       4D       5D
Beginner/New      22–32    28–38    32–44
Intermediate      28–40    36–50    44–58
Advanced          36–48    46–60    54–70
Experienced       40–52    50–66    60–76
```

explicitly labeled "product envelopes, not compulsory targets," with the invariant "peak_target must never be selected independently of starting_volume." **The governing prompt's citation of Intermediate 3D 28–40km/week and 5D 44–58km/week is accurate** — unlike several of this engagement's prior citation gaps, this one checks out against the real source. However: **no runtime authority encodes these bands for HM 3D or 5D today.** `VolumeSafetyPolicy.cs` contains exactly one HM-named instance (`HalfMarathonIntermediate4D`); there is no `HalfMarathonIntermediate3D`/`HalfMarathonIntermediate5D`, no HM-named `PeakVolumeBand` catalog row, and no `ResolvedPeakReference` for either target frequency.

Classification: **APPROVED_BUT_NOT_ENCODED** for the band range itself (it is a real, internally-consistent research candidate, not a fabrication), but **MISSING** for the single, selected `ResolvedPeakReference` point value each frequency would need — per the prompt's own explicit instruction, this phase does **not** derive 3D/5D's selected peak by midpoint arithmetic, ratio-from-4D, or interpolation. `HM_INTERMEDIATE_3D_SELECTED_PEAK_REFERENCE`: **BLOCKED_BY_PRODUCT_DECISION**. `HM_INTERMEDIATE_5D_SELECTED_PEAK_REFERENCE`: **BLOCKED_BY_PRODUCT_DECISION**.

## 8. Weekly-growth authority audit

`PreferredMaxWeeklyIncreaseRatio` (0.07), `HardMaxWeeklyIncreaseRatio` (0.08), and `AbsoluteWeeklyIncrementCapKm` (2.5) are, on direct inspection of every named `VolumeSafetyPolicy` instance in the file (10K Beginner/Intermediate/Advanced across every frequency, 2D through 6D, plus HM 4D), **identical across every single instance except 2D/3D-frequency policies, which use 2.0km instead of 2.5km for the absolute cap** (`ThreeDayIntermediate`, `ThreeDayBeginner`, `Beginner2D`, `Intermediate2D` all use 2.0; every 4D/5D/6D policy uses 2.5). This is a real, frequency-correlated (not distance-family-correlated) pattern already established by 10K: **3-day-per-week policies use a lower absolute cap (2.0km) than 4/5/6-day policies (2.5km) across every distance family that has both**. Applying this same frequency-correlated convention to HM would mean: HM 3D → 2.0km cap (GENERIC_REUSE of an already-established cross-distance convention, not a new invention); HM 5D → 2.5km cap (same). The two ratios (0.07/0.08) are identical across literally every named policy in the file regardless of distance or frequency — classified **GENERIC_REUSE**, frequency-independent by the file's own uncontradicted evidence. This is the one area of the audit where reuse is comfortably supported by pattern-consistency across the whole file, not merely a hope.

## 9. Calibration/start-volume audit

`GoldenFixtureStartingVolumeKm` and `GoldenFixtureNonTaperTransitions` are **calibration constants for the peak-interpolation formula**, not runtime starting-volume defaults with product meaning of their own (confirmed via the `ResolvedPeak`/HM.5.3 doc comments and the Advanced/2D policies' own explicit disclosure that this field is "a growth-multiplier calibration constant... never a real request's actual starting point" where no missing-readiness default exists). Per §233's own doc comment (`HalfMarathonIntermediate4D.ResolveStartingVolume`), HM×Intermediate×4D **requires positive observed `RecentWeeklyVolumeKm`; no missing/zero starting-volume fallback exists** — confirmed directly in `CatalogVolumeAndLongRunPlanner.cs` line ~229-232. Whether this same fail-closed, observed-only rule extends to 3D/5D is **not yet decided** because no 3D/5D policy exists to carry it; nothing in this audit suggests inventing a missing-readiness fallback would ever be appropriate without an explicit product decision — none is proposed here. `HM_INTERMEDIATE_3D/5D_CALIBRATION_START`: **BLOCKED_BY_PRODUCT_DECISION** for the GoldenFixtureStartingVolumeKm value itself (once a selected peak reference exists, HM's own established ratio-reuse convention — e.g. 4D's 25/43 = 0.581 — is *available* as a derivation method should HM choose to reuse its own cross-frequency convention, but that is a decision for whoever closes §7's peak-reference gap, not this audit).

## 10. Long-run share authority audit — genuine premise gap found

The governing prompt cites specific candidate long-run share figures: "3D preferred ~35–40%/hard cap ~45%; 5D preferred ~27–33%/hard cap ~38%." **These figures were searched for directly in `HM_21K_Claude_Handoff_and_Build_Plan.md` and do not appear anywhere in the document.** Every occurrence of "long-run share" or "share/cap" in the file (lines 121, 201, 784, 794, and the section 4.x headers) is a bare category placeholder — e.g. "long-run share/cap;" as a bullet in a checklist of *things a future decision must cover* — never followed by a number for 3D or 5D. Section 4.2 (peak-volume bands) and 4.3 (starting long-run caps) are the only numeric tables in the relevant chapter; neither contains a long-run-share percentage. **This mirrors exactly the kind of governing-prompt premise gap this engagement has repeatedly caught (HM.1.2's HM-LIT-06-11 citation gap; HM.1.4's non-existent 41–60% single-step taper figure)** — the prompt's own instruction to verify rather than assume was followed, and the citation does not hold up. Classification: the specific percentages quoted in the prompt are **not present in any governance source found** and must not be treated as APPROVED_BUT_NOT_ENCODED or even LEGACY_CANDIDATE — they are **MISSING outright**, with no textual anchor. `PreferredLongRunShareMin/Max`, `SelectedLongRunShare`, and `HardLongRunShareCap` for both 3D and 5D: **MISSING**, and per the prompt's own instruction, this phase does not derive them as a midpoint or copy the 4D 33%/40% policy. `HM_INTERMEDIATE_3D/5D_LONG_RUN_SHARE`: **BLOCKED_BY_PRODUCT_DECISION**.

## 11. Absolute peak-LR scope audit

`PreferredAbsolutePeakLongRunKm: 19d` is set **only** on `HalfMarathonIntermediate4D` — every other named policy in the file (all 10K instances, 12 of them) leaves this field at its `null` default. HM.1.1's own scope note (cited in this engagement's ground truth) deliberately did not generalize 19.0km beyond 4D when it was frozen. No later HM phase (HM.1.6, HM.2, HM.2.1, HM.5.3) revisited or widened that scope — HM.5.3's own change (`ResolvedPeakReferenceIsSelectedCeiling`) is a distinct, separately-scoped mechanism and does not touch `PreferredAbsolutePeakLongRunKm`. Classification: **FREQUENCY_SPECIFIC_DECISION_REQUIRED** for both 3D and 5D — not `DISTANCE_LEVEL_AUTHORITY_REUSABLE_ACROSS_FREQUENCY`. 19.0km remains a discrete HM.6-disclosed follow-up decision, preserved as a ceiling-not-target semantic if and when it is ever extended.

## 12. Session-allocation audit

The generic role-cardinality/session-allocation mechanism (`CatalogSessionPrescriptionPlanner.cs`) is confirmed lane-count-generic by direct code inspection (§4). What was **not** fully traced this phase, and is disclosed honestly rather than assumed: whether HM's own already-dark 4D cell currently depends, anywhere in its live call path, on a 10K-named policy class (`V1FourDaySessionVolumeAllocationPolicy` or similar) carrying 10K-specific per-role minima, or instead runs entirely through the fully generic, RunLayout-plus-LR-share-driven mechanism that HM.5.1–HM.5.3 hardened. This distinction matters directly for 3D/5D: if HM 4D silently reuses a 10K-named minima class today, that would itself be an undisclosed numeric-inheritance risk this audit did not invent but also cannot rule out without a dedicated trace. **This is flagged as the audit's own remaining verification gap**, not resolved here, and is listed explicitly in §23's smallest-follow-up set rather than glossed over.

## 13. Taper scope audit

`HalfMarathonIntermediate4DTaperVolumePolicy` (read directly) is explicitly named and scoped to `HALF_MARATHON × INTERMEDIATE × 4D`; its own doc comment justifies the 0.70/0.43 multiplier pair partly by reference to "the frozen 36–50km/week band **for this cell**" sitting below the 56km/week threshold at which a lighter-taper exception would apply. This is an explicit, disclosed 4D-band-dependent justification — **not** a Level-wide, frequency-blind derivation. The **mechanism** (2-week, non-chained, independently-applied-against-a-fixed-anchor taper) is confirmed general (`VolumeSafetyPolicy.TaperVolumeMultipliers`/`ResolvedTaperVolumeMultipliers` is a generic optional field, null-safe for every other policy). The **numbers** are not proven frequency-independent — 3D's lower peak band (28–40) and 5D's higher band (44–58) are exactly the kind of difference the 4D policy's own justification text says matters. Classification: **taper duration (2 weeks) and mechanism: GENERIC_REUSE. Taper multiplier values (0.70/0.43): FREQUENCY_SPECIFIC_AUTHORITY_MISSING for 3D and 5D**, requiring their own evidence-informed derivation, not a copy.

## 14. Pace/fallback audit

`RACE_SPECIFIC_HM_DEVELOPMENT`/`RACE_SPECIFIC_HM_REHEARSAL`/`TAPER_HM_ACTIVATION`'s `GOAL_FEASIBILITY_IN`/`PACE_SOURCE_IN` gating and `fallbackStageKey` mechanism, read directly in the progression file, is expressed at the **stage** level, inside phases (RACE_SPECIFIC, TAPER) whose stage lists are identical regardless of which RunLayout lane consumes them for 3D. Nothing in this mechanism references `DaysPerWeek`, `KeySessionOrdinal`, or any RunLayout shape. Classification: **GENERIC_REUSE** for 3D (single-lane, no structural conflict). For 5D, the mechanism itself is still frequency-blind, but it is bound to lane 0 by construction in a document with no lane concept — so it is reusable for 5D's *first* KEY lane unmodified, while a second lane (if HM ever authorizes one) would need its own fallback/eligibility wiring, which does not yet exist. No change to goal feasibility, recent-race derivation, desired-finish-time prohibition, or fallback semantics is proposed.

## 15. Adaptation audit

Not exhaustively re-traced against live Adaptation dispatch code this phase (disclosed limitation, consistent with this audit's "no forced closure" discipline rather than a false claim of completeness). Based on the Roadmap's own confirmed precedent that 10K Intermediate 3D/4D/5D/6D Adaptation is already `PUBLICLY_ACTIVE` and frequency-generic (§33 of `MASTER_ROADMAP.md`), and that HM 4D Adaptation already reuses this same generic dispatch with no HM-specific branch disclosed anywhere in HM.0–HM.5.3's own reports, the working classification is **GENERIC_REUSE (high confidence, not independently re-verified this phase)** for both 3D and 5D completion semantics (2/3, 4/5 or 3/5-if-a-second-KEY-role-exists). If HM.6's follow-up work ever authorizes a second 5D KEY lane, Adaptation's per-role-missed scoring for that second lane should be spot-checked once real content exists for it — flagged, not solved.

## 16. Calendar/spacing audit

Not independently re-derived this phase beyond confirming RunLayout cardinality (§3). 10K's KEY↔LONG spacing and EASY-adjacency mechanism is documented in this engagement's own ground truth as already frequency-generic and reused verbatim by HM 4D. For 5D specifically: per the prompt's own explicit instruction, **calendar capability to place a second KEY-typed slot does not create training authority to use it as hard** — until §6's `HM_5D_SECOND_HARD_STIMULUS_AUTHORITY_MISSING` gap is closed, the second 5D KEY lane's calendar placement should default to being filled with EASY-effort content at the prescription/binding layer, not scheduled as a second hard day. This audit does not implement that binding decision; it only names the constraint it must respect.

## 17. Core 10–16 reuse proof

`half-marathon-master.v1.json`'s `coreCycle` (10/14/16), `phases` array (Foundation/Build/RaceSpecific/Taper with their min/preferred/max headroom and compression/extension priorities), and its single `workoutProgression` pointer are **not** frequency-keyed anywhere in the document — the only frequency-specific field in the whole master is `supportedRunsPerWeek: [4]`. This confirms the preferred architecture the prompt asks to check for is already the real shape: **one `HALF_MARATHON_MASTER` + a per-frequency `RUN_LAYOUT` + `INTERMEDIATE_MODIFIER` + frequency-specific numeric policies is exactly how the existing 4D combination (`half-marathon-4d-intermediate.v1.json`) is already composed** (`masterTemplate` → `HALF_MARATHON_MASTER v1`; `layout` → `RUN_LAYOUT_4D`; `levelModifier` → `INTERMEDIATE_MODIFIER v8`; `rulePack` → `APPSEL_RACE_PLAN_V1 v10`). Expanding `supportedRunsPerWeek` to `[3, 4, 5]` (or per-combination-only, without touching the master, if that is how the schema actually gates it) is the only master-level touchpoint this audit identifies — and that is a metadata field, not new phase/stage/numeric content. **Core reuse across 3D/4D/5D: GENERIC_REUSE, high confidence, directly proven by the existing document shape.**

## 18. Catalog combination/version forecast

For `HALF_MARATHON__3D__INTERMEDIATE` and `HALF_MARATHON__5D__INTERMEDIATE`, mirroring the real `HALF_MARATHON__4D__INTERMEDIATE v1` shape exactly:

| Reference | 4D (existing) | 3D (forecast) | 5D (forecast) |
|---|---|---|---|
| masterTemplate | HALF_MARATHON_MASTER v1 | same, unmodified (once `supportedRunsPerWeek` admits 3) | same, unmodified (once it admits 5) |
| layout | RUN_LAYOUT_4D v1 | RUN_LAYOUT_3D v1 (already exists, 10K-proven, reusable as-is) | RUN_LAYOUT_5D v1 (already exists, 10K-proven, reusable as-is) |
| levelModifier | INTERMEDIATE_MODIFIER v8 | same (Level-owned, not frequency-owned) | same |
| workoutProgression | HALF_MARATHON_WORKOUT_PROGRESSION v1 | same, unmodified (§5) | **new authority required** if a second lane is ever approved (§5/§6); otherwise same document, lane-0-only |
| numeric policy | `HalfMarathonIntermediate4D` (VolumeSafetyPolicy) | **new named policy required** (§7, §10, §13) | **new named policy required** (§7, §10, §13) |
| taper policy | `HalfMarathonIntermediate4DTaperVolumePolicy` | **new authority required** (§13) | **new authority required** (§13) |
| rulePack | APPSEL_RACE_PLAN_V1 v10 | same (distance/rule-pack level, not frequency-owned) | same |
| dispatch (volume planner) | hardcoded exact-match branch in `CatalogVolumeAndLongRunPlanner.cs` (§19) | needs a new branch or a switch-based dispatcher | same |
| dispatch (dark identity routing) | hardcoded exact-match in `V1CatalogPilotIdentityPolicy.cs` (§19) | needs a new resolvable-identity entry | same |

No artifact is created by this phase. Existing `VALIDATED`-status HM documents (master, progression) are not mutated (no lifecycle/versioning violation risk from this audit).

## 19. Recurring 4D-assumption-family findings

Two concrete, directly-code-verified hardcoded 4D-only assumptions were found (not merely hypothesized):

1. **`CatalogVolumeAndLongRunPlanner.cs` (~line 99–104)**: 
   ```csharp
   // HM.2 — exact dark-cell dispatch only; every other HM cell remains fail-closed.
   if (request.Candidate.CanonicalDistanceFamily == "HALF_MARATHON" &&
       request.Candidate.Level == "INTERMEDIATE" && request.Candidate.DaysPerWeek == 4 &&
       ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
   {
       return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.HalfMarathonIntermediate4D).Build(request);
   }
   ```
   This is an exact-match, single-frequency conditional — not a `switch`/dispatcher keyed on `DaysPerWeek` the way `VolumeSafetyPolicy.ForIntermediateDaysPerWeek(daysPerWeek)` already is for 10K. Classification: **HIDDEN_4D_ASSUMPTION** (deliberately fail-closed today, correctly, but structurally would need a parallel `ForHalfMarathonIntermediateDaysPerWeek`-shaped dispatcher, not a copy-pasted second `if`, to stay consistent with 10K's own established pattern).

2. **`V1CatalogPilotIdentityPolicy.cs`**: the dark-routing candidate-key resolver's `HalfMarathon` arm is a single tuple match `(HalfMarathon, Intermediate, 4) => HalfMarathonFourDayIntermediateCandidateKey`, falling through to a hard `throw new ArgumentOutOfRangeException(...)` for any other `(Level, daysPerWeek)` combination — confirmed by reading the switch expression and its exception message, which literally enumerates "the dark-only HALF_MARATHON Intermediate 4D identity" as the only resolvable HM shape. Classification: **HIDDEN_4D_ASSUMPTION**, same family as (1), same fix shape (extend the tuple match once real 3D/5D candidate keys and authority exist — not before).

Sibling files named in the governing prompt (`CatalogFinalPrescribedPlanValidator.cs`, `CatalogPrescriptionContextBuilder.cs`) were confirmed to reference `HalfMarathon`/`HALF_MARATHON` (via grep) but their exact branch shapes were **not individually re-read this phase** — disclosed as an incomplete sweep rather than silently assumed clean. Recommendation for the actual implementation phase: apply the identical "grep every `HALF_MARATHON`/`HalfMarathon` occurrence in `RuntimeCatalog/`, classify each as GENERIC_SAFE / FREQUENCY_POLICY_LOOKUP / HIDDEN_4D_ASSUMPTION" sweep exhaustively before writing any 3D/5D code — this phase found the pattern exists in at least 2 of the ~11 files that reference `HalfMarathonIntermediate4D`/`HalfMarathon`, which is enough to prove the *family* is real without claiming every instance was traced.

## 20. Complete 3D/5D authority-gap ledger

| Authority | 3D status | 5D status | CanReuse4D? | CanReuse10KMechanism? | NumericDecisionRequired? | BlockingFirstSlice? | Evidence/Source |
|---|---|---|---|---|---|---|---|
| CoreCycle (10/14/16) | Present (master, frequency-blind) | Present | Yes | Yes | No | No | `half-marathon-master.v1.json` §17 |
| Phase headroom | Present (master, frequency-blind) | Present | Yes | Yes | No | No | `half-marathon-master.v1.json` |
| Stage progression | Present, single-lane | **Missing second-lane authority** | Yes (lane 0 only) | Partial (schema differs) | Yes for 5D lane 2 | No for 3D / **Yes for 5D** | `half-marathon-workout-progression.v1.json` vs `ten-k-workout-progression.v7.json` |
| RunLayout | Exists, VALIDATED (1K+1E+1L) | Exists, VALIDATED (**2K+2E+1L**) | N/A (10K-owned artifact) | Yes | No | No | §3 |
| PeakVolumeBand | APPROVED_BUT_NOT_ENCODED (28–40) | APPROVED_BUT_NOT_ENCODED (44–58) | No | No | Yes (candidate exists) | No (band ≠ selection) | §7 |
| SelectedPeakReference | MISSING | MISSING | No | No | **Yes** | **Yes** | §7 |
| WeeklyGrowthPreferred (0.07) | GENERIC_REUSE | GENERIC_REUSE | Yes | Yes | No | No | §8 |
| WeeklyGrowthHard (0.08) | GENERIC_REUSE | GENERIC_REUSE | Yes | Yes | No | No | §8 |
| AbsoluteIncreaseCap | GENERIC_REUSE (2.0, cross-distance 3D convention) | GENERIC_REUSE (2.5) | Yes (pattern, not HM-specific) | Yes | No | No | §8 |
| CalibrationStart | BLOCKED_BY_PRODUCT_DECISION | BLOCKED_BY_PRODUCT_DECISION | Method available, value not | Partial | Yes | Yes (downstream of peak) | §9 |
| TaperWeek1Multiplier | FREQUENCY_SPECIFIC_AUTHORITY_MISSING | FREQUENCY_SPECIFIC_AUTHORITY_MISSING | No (4D-band-justified) | No | Yes | Yes | §13 |
| TaperWeek2Multiplier | FREQUENCY_SPECIFIC_AUTHORITY_MISSING | FREQUENCY_SPECIFIC_AUTHORITY_MISSING | No | No | Yes | Yes | §13 |
| LR PreferredMin/Max | MISSING (no textual anchor) | MISSING | No | No | Yes | Yes | §10 |
| LR Selected | MISSING | MISSING | No | No | Yes | Yes | §10 |
| LR HardCap | MISSING | MISSING | No | No | Yes | Yes | §10 |
| AbsolutePeakLR (19km) | FREQUENCY_SPECIFIC_DECISION_REQUIRED | FREQUENCY_SPECIFIC_DECISION_REQUIRED | No (explicitly non-generalized) | No | Yes (or: leave null) | No (optional field) | §11 |
| Session allocation | REQUIRES_PARAMETERIZATION (untraced dependency risk) | REQUIRES_PARAMETERIZATION | Mechanism yes, dependency untraced | Yes (mechanism) | Unclear pending trace | Possibly | §12 |
| Hard-session count | GENERIC_REUSE (1, by RunLayout) | **HM_5D_SECOND_HARD_STIMULUS_AUTHORITY_MISSING** | N/A | N/A | Yes (product) | **Yes for 5D** | §6 |
| Pace / fallback | GENERIC_REUSE | GENERIC_REUSE (lane 0 only) | Yes | Yes | No | No | §14 |
| Adaptation | GENERIC_REUSE (not re-verified) | GENERIC_REUSE (not re-verified; second lane untested) | Presumed yes | Yes | No | No (disclosed low-confidence) | §15 |
| Calendar | GENERIC_REUSE (not re-derived) | GENERIC_REUSE, second-lane placement policy-gated | Presumed yes | Yes | No | Possibly (second lane) | §16 |
| Public eligibility | Correctly fail-closed today (§19) | Correctly fail-closed today | N/A | N/A | No | N/A (out of scope, §24 of prompt) | §19 |

## 21. 3D readiness classification

**`HM_INTERMEDIATE_3D_BLOCKED_BY_AUTHORITY_GAPS`** — but with the narrowest gap set of the two targets. No structural/architectural blocker exists (RunLayout, progression, session mechanism, calendar, adaptation, pace/fallback all reuse cleanly). The blockers are purely numeric-authority: SelectedPeakReference, CalibrationStart, LR share triple, taper multiplier pair, and the two hardcoded-dispatch fixes in §19.

## 22. 5D readiness classification

**`HM_INTERMEDIATE_5D_BLOCKED_BY_AUTHORITY_GAPS`** — strictly more blocked than 3D. Carries every 3D-shaped numeric gap **plus** the second-KEY-lane structural question (§5/§6/§16), which is a genuine training-methodology decision this audit correctly declines to answer unilaterally (mirroring HM.5.2A's/HM.0's own precedent of escalating rather than inventing).

## 23. Smallest remaining authority set (do not reopen closed HM topics)

For **3D** specifically: (a) a selected `ResolvedPeakReference` point value within the already-approved 28–40km band; (b) a `GoldenFixtureStartingVolumeKm` calibration value (derivable via HM's own established cross-frequency ratio convention once (a) exists, or a fresh evidence-based figure — a product choice, not this audit's to make); (c) a long-run share quadruple (min/max/selected/hard-cap) — **genuinely absent from any source**, requiring fresh evidence, not extraction from the handoff document; (d) a 2-week taper multiplier pair, evidence-derived for 3D's own (lower) peak band; (e) the two mechanical dispatcher fixes named in §19 (small, generic-pattern code changes, not numeric decisions — these belong to the implementation phase, not this audit).

For **5D**: all of the above, frequency-recalibrated to the 44–58km band, **plus** (f) an explicit product decision on whether the second `KEY_SESSION` RunLayout slot is used as a second hard stimulus (requiring new HM workout-progression lane authority) or deliberately downgraded to easy-effort content at the binding layer for this cell only (requiring no new progression authority, only a binding-layer decision) — this is the single highest-leverage decision for 5D, since it determines whether §5/§6/§16's gaps exist at all.

Also carried forward, not fabricated: (g) the session-allocation dependency-trace gap disclosed in §12, and (h) the incomplete `HALF_MARATHON`-reference sweep disclosed in §19 (2 of ~11 referencing files traced in depth).

## 24. Recommended implementation phase

Once (and only once) the above authority items receive real product/evidence decisions, the recommended implementation shape is exactly the one this audit found already latent in the repository: reuse `HALF_MARATHON_MASTER` (widening `supportedRunsPerWeek`), reuse `RUN_LAYOUT_3D`/`RUN_LAYOUT_5D` verbatim, reuse `HALF_MARATHON_WORKOUT_PROGRESSION` verbatim for 3D (and for 5D's first lane; a second lane only if product decision (f) above chooses to author one), add two new named `VolumeSafetyPolicy` instances plus two new taper-multiplier-policy classes, add two new `TEMPLATE_COMBINATION` catalog documents mirroring the existing 4D one exactly, and extend the two hardcoded dispatch sites (§19) to a switch/dispatcher shape consistent with 10K's own established `ForIntermediateDaysPerWeek` pattern — all while leaving `V1CatalogPilotIdentityPolicy`'s public allow-list untouched (dark-only, mirroring HM.2's own precedent) until a separate, later public-activation phase is explicitly authorized. No new generation engine, no per-frequency generator, and no premature 2D/6D/Beginner/Advanced/Runway/LongHorizon work is proposed anywhere in this recommendation.

---

## Trivial metadata-only correction

No trivial metadata-only correction was found that the phase scope explicitly permits making without inventing a new decision. `supportedRunsPerWeek: [4]` in the master template is the one field this audit could technically widen with a one-line edit, but doing so would misrepresent 3D/5D as catalog-supported before any of the numeric authorities in §20 exist — this would not be "trivial," it would silently open a combination path this audit's own final classification says is not ready. **No production/catalog edit was made.** This phase is a pure `docs(hm-6)` commit.

## Final classification

**`HM_INTERMEDIATE_3D_5D_FREQUENCY_EXPANSION_AUTHORITY_BLOCKED`**

- 3D: `HM_INTERMEDIATE_3D_BLOCKED_BY_AUTHORITY_GAPS` (narrow gap set — numeric authority only, no structural blocker)
- 5D: `HM_INTERMEDIATE_5D_BLOCKED_BY_AUTHORITY_GAPS` (narrow-plus set — every 3D gap, plus the second-KEY-lane product decision)

Both targets require substantive, undecided authority before a full dark 10–16W implementation — the honest result this audit's own governing instructions asked for, not a forced or premature closure.
