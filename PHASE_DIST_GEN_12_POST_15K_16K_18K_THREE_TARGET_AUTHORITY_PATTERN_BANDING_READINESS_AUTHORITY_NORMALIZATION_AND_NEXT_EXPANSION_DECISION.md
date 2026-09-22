# PHASE DIST-GEN.12 — Post-15K/16K/18K Three-Target Authority Pattern, Banding Readiness, Authority Normalization, and Next-Expansion Decision

## 1. Current post-DIST-GEN.11 baseline

Repository HEAD at the start of this phase: `c66e5e7` ("docs(dist-gen-11): backfill governance commit SHA for DIST-GEN.11"), branch `main`, `0` ahead / `0` behind `origin/main`. `PHASE_LEDGER.md` carries 56 rows, the last being DIST-GEN.11 (`18K_INTERMEDIATE_4D_PUBLIC_ACTIVATION_COMPLETE`). Working tree carried only pre-existing `bin/`/`obj/`/`TestResults/` build noise. This phase is governance-only: it performs zero production edits and consumes the actual current repository state (source files and prior reports) as its sole evidence base, not memory of earlier governing prompts.

## 2. Five-point authority matrix

All values below are read from current source (`VolumeSafetyPolicy.cs`, `TargetDistance{15,16,18}KHorizonPolicy.cs`, `TargetDistance{15,16,18}KPeakVolumeBandPolicy.cs`, `RaceHorizonPolicy.cs`), Intermediate × 4D at every point.

| Field | TEN_K | 15K | 16K | 18K | HALF_MARATHON |
|---|---|---|---|---|---|
| TargetDistanceKm | 10.0 (canonical) | 15.0 (projected) | 16.0 (projected) | 18.0 (projected) | 21.0975 (canonical) |
| Structural family | TEN_K (own) | HALF_MARATHON (reused) | HALF_MARATHON (reused) | HALF_MARATHON (reused) | HALF_MARATHON (own) |
| MinimumCoreWeeks | 8 | 10 | 10 | 10 | 10 |
| PreferredCoreWeeks | 12 | 12 | 12 | 12 | 14 |
| MaximumCoreWeeks | 14 | 14 | 14 | 14 | 16 |
| PeakVolumeBandMin/Max | 30/42 (catalog) | 34.0/46.0 | 34.0/46.0 | 34.0/46.0 | 36/50 (catalog) |
| SelectedPeakReference | 38.0 | 40.0 | 40.0 | 40.0 | 43.0 |
| PreferredWeeklyIncreaseRate | 0.07 | 0.07 | 0.07 | 0.07 | 0.07 |
| HardWeeklyIncreaseRate | 0.08 | 0.08 | 0.08 | 0.08 | 0.08 |
| AbsoluteWeeklyIncreaseCapKm | 2.5 | 2.5 | 2.5 | 2.5 | 2.5 |
| CalibrationStartingVolumeKm | 24.0 | 23.5 | 23.5 | 23.5 | 25.0 |
| LR share (min/max/sel/hardcap) | 0.30/0.36/0.33/0.40 | same | same | same | same |
| PreferredAbsolutePeakLongRunKm | null | **14.5** | **15.0** | **16.0** | **19.0** |
| StartingAbsoluteLongRunCapKm | null | null | null | null | null (at 4D) |
| TaperDurationWeeks | 1 | 2 | 2 | 2 | 2 |
| TaperWeek1/Week2Multiplier | n/a (0.53 single-week) | 0.70/0.43 | 0.70/0.43 | 0.70/0.43 (via 16K's class) | 0.70/0.43 |
| Taper mechanism | single-week | non-chained fixed-anchor | same | same | same |

This table is identical in every field to what DIST-GEN.9/10/11 already froze — no value is re-derived here. One representational asymmetry, confirmed by direct source inspection: `TargetDistance15KTaperVolumePolicy.cs` and `TargetDistance16KTaperVolumePolicy.cs` both exist as independently-declared classes with identical values, while 18K's `VolumeSafetyPolicy` instance references `TargetDistance16KTaperVolumePolicy`'s constants directly rather than having its own class (DIST-GEN.10's own disclosed decision, §33 of that report). This is a real, minor inconsistency in *how* the same governance conclusion (taper = shared parent-family authority) is represented across the three targets — see §12/§39.

## 3. Current public/dark matrix

| Target | Dark | Public | Since |
|---|---|---|---|
| 15.0 × HalfMarathon × Intermediate × 4D | Yes | Yes | dark DIST-GEN.6, public DIST-GEN.7 |
| 16.0 × HalfMarathon × Intermediate × 4D | Yes | Yes | dark DIST-GEN.2/2B, public DIST-GEN.3 |
| 18.0 × HalfMarathon × Intermediate × 4D | Yes | Yes | dark DIST-GEN.10, public DIST-GEN.11 |
| Everything else (12K/14K/17K/19K/20K/16.0934, any non-Intermediate level, any non-4D frequency) | No | No | — |

Supported horizon per projected target: 10–14 weeks, preferred 12. Habit Custom unsupported. Experienced excluded product-wide. This state is frozen and not reopened by this phase.

## 4. DIST-GEN.8 ownership decisions consumed

DIST-GEN.8's central rule — **value equality ≠ authority equality** — and its §7 authority-provenance table are consumed verbatim, not re-derived:

- **`SHARED_LEVEL_FREQUENCY_AUTHORITY`** (identical across all four Intermediate×4D anchors including TEN_K, "far too broad to be a 15-16K-specific pattern"): `PreferredWeeklyIncreaseRate`/`HardWeeklyIncreaseRate` (0.07/0.08), `AbsoluteWeeklyIncreaseCapKm` (2.5), the LR share quadruple (0.30/0.36/0.33/0.40), `StartingAbsoluteLongRunCapKm` (null at 4D — non-null only at 5D/6D, "nothing to do with distance").
- **`SHARED_PARENT_FAMILY_AUTHORITY`**: `MinimumCoreWeeks` (10, "the reused HM template's own real phase-minimum sum, a structural fact, not independently decided"); taper duration/multipliers/mechanism (2-week/0.70/0.43/non-chained, "TEN_K's own genuinely different taper proves this is HM-parent-inherited, not a '15-16K band'"); the calibration *methodology* (HM's own calibration/peak ratio applied to each target's own peak) as distinct from the calibration *value*.
- **`INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE`** (real convergence, not shared authority — each closure phase independently re-ran the same evidence-weighing methodology): `PreferredCoreWeeks`/`MaximumCoreWeeks` (12/14), `PeakVolumeBandMin`/`Max` (34/46), `SelectedPeakReference` (40.0), `CalibrationStartingVolumeKm` (23.5, value only).
- **`INDEPENDENT_TARGET_AUTHORITIES_DIFFERENT_VALUE`** (`EXACT_TARGET_SPECIFIC`): `PreferredAbsolutePeakLongRunKm` — "the one field that actually differs, mechanically forced by the LR-below-target invariant."
- **`SHARED_GENERIC_MECHANISM`**: target-race-pace, Riegel, goal feasibility, readiness, calendar, adaptation.

DIST-GEN.8's own explicit band-definition standard (§28): "no field in this matrix meets the bar... **No field passes the band-definition standard.**" Its §39 componentized-authority-registry recommendation is the direct precedent this phase's own normalization option (§39/§57 below) builds on — DIST-GEN.8 explicitly named "when and if a third target is added" as the trigger point for that normalization to become worthwhile, "since a third instance would repeat the shared fields a third time." That condition is now met.

None of these classifications are casually reopened here; §20-30 below test whether 18K's addition changes any of them.

## 5. DIST-GEN.9 evidence limitations consumed

DIST-GEN.9's final banding result was **`BANDING_STILL_NOT_GOVERNANCE_READY`**, reached specifically because 18K's own dedicated external evidence was sparse — repeated targeted searches for "18K training plan" and "20K training plan" returned marathon/half-marathon content misattributed via plan-*duration* keyword overlap ("18-week"/"20-week" programs), not genuine 18-kilometer or 20-kilometer race-distance content. This confirms 18K and 20K are **both** non-standardized race distances lacking dedicated training-plan literature, unlike 10-mile and 15K.

DIST-GEN.9's own evidence-tier accounting for 18K: 20 fields `DIRECT_EXISTING_AUTHORITY`, 6 fields `EVIDENCE_INFORMED_PRODUCT_DEFAULT`, **zero** fields `STRONG_EVIDENCE_DERIVED_DECISION` — the only one of the three closures to reach zero at the strongest tier (16K: 10 strong-evidence fields; 15K: 3). This phase treats that downgrade as a permanent fact about 18K's evidence base, not something the mere existence of a third internal data point can retroactively upgrade (§86-87 of the governing prompt's own instruction, consumed directly below in §20-22).

DIST-GEN.9's own per-field pattern findings, none of which are re-litigated here: peak volume `INSUFFICIENT_PATTERN_EVIDENCE` (not `INTERIOR_STABILITY_SUPPORTED`); selected peak `INSUFFICIENT_PATTERN_EVIDENCE` (mechanically tied to peak volume, not independent); calibration `INSUFFICIENT_PATTERN_EVIDENCE` (same caveat); absolute LR `POTENTIAL_RULE_SIGNAL` (explicitly not `POTENTIAL_BAND_SIGNAL`).

## 6. Three-target generic infrastructure proof

Classifying each architecture surface using the governing prompt's own five-way taxonomy, based on direct source inspection and the three phases' own regression proofs:

| Surface | Classification |
|---|---|
| Custom + `target_distance_km` public API | `GENERIC_PROVEN_THREE_TARGET` — zero DTO/endpoint change across DIST-GEN.3/7/11 |
| `CanonicalDistanceFamilyResolver` | `GENERIC_PROVEN_THREE_TARGET` — pure band resolver, zero target-specific code ever added |
| Exact-target persistence (`RequestedTargetDistanceKm`/`CanonicalDistanceFamily`) | `GENERIC_PROVEN_THREE_TARGET` — same two columns since DIST-GEN.2, zero migration across three activations |
| Public eligibility registry (`ApprovedPublicCells`) | `GENERIC_PROVEN_THREE_TARGET` — a flat exact-match list, one-line addition each time, zero consumer-code change (§9) |
| Dark eligibility registry (`ApprovedDarkCells`) | `GENERIC_PROVEN_THREE_TARGET` at the registry-container level, but see next row |
| Horizon/volume/LR authority dispatch (4 call sites: `PlanServices.cs`, `CatalogPreviewGenerator.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `TargetDistance16KAwarePeakVolumeBandLoader.cs`) | `GENERIC_PROVEN_BUT_WITH_SCALABILITY_DEBT` — each grew by one hand-edited branch per new target, twice in a row, exactly as DIST-GEN.8 predicted |
| Taper ownership | `GENERIC_PROVEN_BUT_WITH_SCALABILITY_DEBT` — correct in conclusion (parent-family authority, never re-derived) but inconsistent in representation (15K/16K each have a dedicated class; 18K reuses 16K's directly) |
| Pace resolver / Riegel | `GENERIC_PROVEN_THREE_TARGET` — zero changes across all three closures |
| Response DTOs / display formatter | `GENERIC_PROVEN_THREE_TARGET` — the exact-target label formatter (`'${target}K'`) required zero changes for 15K, 16K, or 18K |
| Home / Calendar / Detail / PlanDetails / mutations | `GENERIC_PROVEN_THREE_TARGET` — no target-specific branch exists in any read/mutation path (proven directly in DIST-GEN.7 §55/DIST-GEN.11 §67) |
| Rollback | `GENERIC_PROVEN_THREE_TARGET` — proven independently for 15K and 18K via the identical mechanism (§51 below) |

The one real, twice-repeated scalability debt is the dispatch cascade, not the registries or any downstream surface.

## 7. Authority-provenance matrix

| Field | Provenance |
|---|---|
| Structural family (HALF_MARATHON) | `SHARED_PARENT_FAMILY_AUTHORITY` |
| MinimumCoreWeeks | `SHARED_PARENT_FAMILY_AUTHORITY` |
| PreferredCoreWeeks / MaximumCoreWeeks | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE` |
| PeakVolumeBandMin/Max, SelectedPeakReference | `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE` |
| Growth rates (0.07/0.08) | `SHARED_LEVEL_FREQUENCY_AUTHORITY` |
| AbsoluteWeeklyIncreaseCapKm | `SHARED_LEVEL_FREQUENCY_AUTHORITY` |
| CalibrationStartingVolumeKm | value: `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE`; methodology: `SHARED_PARENT_FAMILY_AUTHORITY` |
| LR share quadruple | `SHARED_LEVEL_FREQUENCY_AUTHORITY` |
| PreferredAbsolutePeakLongRunKm | `INDEPENDENT_TARGET_AUTHORITIES_DIFFERENT_VALUE` (`TARGET_SPECIFIC_PRODUCT_DEFAULT` at the evidence-tier level for 18K specifically, per §5) |
| StartingAbsoluteLongRunCapKm | `SHARED_LEVEL_FREQUENCY_AUTHORITY` |
| Taper (duration/multipliers/mechanism) | `SHARED_PARENT_FAMILY_AUTHORITY` |
| Phase allocation / topology | `SHARED_PARENT_FAMILY_AUTHORITY` (reused HM candidate's own topology, never re-authored per target) |
| Target-race-pace, Riegel, goal feasibility, readiness, calendar, adaptation | `SHARED_GENERIC_MECHANISM` |

No field is newly reclassified by this phase. This table simply restates DIST-GEN.8's own model in the vocabulary this phase's governing prompt requests, confirming §8's "value equality ≠ authority equality" discipline was already fully applied and remains correct after 18K's addition.

## 8. Mechanism-vs-parameter model

Kept explicitly separate, per the governing prompt's own instruction: **mechanism reuse** (Riegel's formula, the generic target-race-pace resolver, `RaceHorizonPolicy.Decide`'s classification engine, the phase allocator, the calendar materializer) is unconditionally proven across all three targets and carries zero numeric authority of its own — it is parametrized entirely by whatever `RequestedTargetDistanceKm`/horizon/policy values are supplied. **Numeric authority reuse** (the actual frozen values above) is a separate question, answered field-by-field in §7. Mechanism genericity is not used anywhere in this report as evidence for numeric banding — a generic mechanism working correctly for three different target-specific inputs proves the mechanism is target-agnostic, not that the inputs it consumes share a band.

## 9. Public-registry scalability

Direct source inspection of `Dark16KPilotEligibilityPolicy.cs` confirms: `ApprovedPublicCells` is a flat `IReadOnlyList<ApprovedDarkTargetDistanceCell>` collection-expression literal, never computed from `ApprovedDarkCells`, never a range. `(TargetDistanceKm, ParentDistanceFamily, Level, RunsPerWeek)` is already a 4-key composite, so level/frequency are already part of the key, not assumed constant. `TryResolvePublicEligibleCell`/`IsPubliclyEligible` and `GeneratePreviewCommandMapper.ToInternalRequest` required and received **zero** code change across both DIST-GEN.7 and DIST-GEN.11 — only a one-line data addition each time. This is real, repeated, source-verified proof that a 4th or 5th target can be added by explicit cell registration alone. A future alias target (§36) could plausibly reuse the same 4-key record shape by pointing at an existing training-authority key rather than duplicating one, but this would need `ApprovedDarkTargetDistanceCell` to carry a distinguishable "training authority owner" field, which it does not yet — a real, small, identified architecture gap for §36-37, not a blocker to today's exact-cell model. Classification: **public registry is `SCALABLE_AS_IS`.**

## 10. Dark-registry scalability

`ApprovedDarkCells` is architecturally identical in shape to `ApprovedPublicCells` and equally `SCALABLE_AS_IS` as a container — the one-line 18.0 addition in DIST-GEN.10 required zero registry-mechanism change. Dark eligibility does not encode public status anywhere: `IsDarkEligible`/`TryResolveDarkEligibleCell` are referenced from six call sites, all internal materialization/dispatch logic; `IsPubliclyEligible`/`TryResolvePublicEligibleCell` are referenced only from the mapper. No call site checks one registry and falls through to the other. The separation invariant (§62 below) holds cleanly at three targets on both sides — the debt is entirely in what the *dark* call sites do with a resolved cell (§11), not in the registry container itself.

## 11. Exact-target authority-dispatch scalability

This is where the real architecture debt lives, confirmed directly in source (not inferred from reports):

- `PlanServices.cs:139-242` — one `TryResolveDarkEligibleCell` call feeding **six** separate ternary/pattern-match expressions (`horizonDecision`, `classification`, two `reasonCode` sites, `minimumCoreWeeks`, `maximumCoreWeeks`), plus two `pilotLabel` string ternaries. Each of the eight expressions has its own three-armed `isDark15KTarget ? ... : isDark18KTarget ? ... : (16K/generic)` shape.
- `CatalogPreviewGenerator.cs:646-705` — an `if/else if/else if/else` chain assigning three locals (`horizonMinimumWeeks`/`PreferredWeeks`/`MaximumWeeks`) before a single `RaceHorizonPolicy.Decide` call (a deliberately preserved single-call-site invariant elsewhere in this file).
- `CatalogVolumeAndLongRunPlanner.cs:111-135` and `TargetDistance16KAwarePeakVolumeBandLoader.cs:54-73` — each a sequential `if (darkCell is { TargetDistanceKm: N })` chain of three returns.

Two further call sites (`CatalogPreviewGenerator.cs:1134`, `CatalogPrescriptionContextBuilder.cs:351`) consult only the boolean `IsDarkEligible` and carry no per-target branching — these are already fully generic and need no future change regardless of target count.

DIST-GEN.6 introduced this cascade shape when generalizing a single boolean into a two-target registry lookup; DIST-GEN.10 extended it to three by hand at all four cascade sites, explicitly declining (its own §57, disclosed) the keyed-lookup generalization DIST-GEN.9 §62 had asked it to "seriously consider... not defer again"; DIST-GEN.11 deferred it a second time (correctly, since its own scope was public-only and the public gate needed no such change). **A fourth target today would require a fourth hand-edited branch at all four sites — eight separate ternary/if-chain edits in `PlanServices.cs` alone.** This is real, demonstrated, twice-repeated branch multiplication, matching DIST-GEN.8 §5's own prediction exactly. Classification: **`NEEDS_TYPED_REGISTRY_GENERALIZATION`** — not `ARCHITECTURAL_BLOCKER` (three extensions have shipped successfully with zero behavioral defects), but no longer honestly `SCALABLE_AS_IS` either.

## 12. Policy-bundle duplication audit

Field-by-field, using the governing prompt's own five-way taxonomy:

| Field | Duplication class | Reasoning |
|---|---|---|
| PreferredCoreWeeks/MaximumCoreWeeks, PeakVolumeBand, SelectedPeakReference | **(A) intentional — independently frozen** | DIST-GEN.8 §7 confirms these were independently re-derived by each closure phase, not copy-pasted; equal values are real convergence, not laziness |
| Growth rates (0.07/0.08), AbsoluteWeeklyIncreaseCapKm, LR share quadruple, StartingAbsoluteLongRunCapKm | **(B) representation duplication of higher-level shared authority** | Governance already settled these as `SHARED_LEVEL_FREQUENCY_AUTHORITY` (DIST-GEN.8 §7); each `VolumeSafetyPolicy` instance nonetheless re-declares its own literal copy. Nothing is at risk of divergence today (regression proves zero-delta), but the code does not express the real ownership |
| Taper duration/multipliers/mechanism | **(B), with an added (D) harmless-inconsistency wrinkle** | 15K and 16K each carry a fully duplicated, independently-declared `TargetDistanceNNKTaperVolumePolicy` class with identical values; 18K instead references 16K's class directly. All three are governance-correct (parent-family authority, never re-derived), but the *pattern* of how that correct conclusion is represented is not itself consistent across the three targets |
| Horizon MinimumCoreWeeks (10 for all three) | **(B)** | Already `SHARED_PARENT_FAMILY_AUTHORITY`; each `TargetDistanceNNKHorizonPolicy` re-declares its own `10` literal rather than referencing one shared HM-parent constant |
| PreferredAbsolutePeakLongRunKm | **(A), correctly not shared** | The one field where independent per-target values are the entire point — no duplication concern applies |
| Dispatch-cascade branches (§11) | **(D) harmless technical debt today, trending toward (E) correctness risk as target count grows** | No defect has yet occurred, but the cascade's linear growth pattern is the single most fragile part of the architecture — an accidental branch-ordering mistake at a 4th/5th target is a real, growing risk, not yet realized |

No field is classified **(C) misleading target ownership** — every duplicated field's *value* is correctly governed even where its *representation* duplicates that governance. This distinction (governance duplication vs implementation duplication) is the entire basis for this phase's normalization-readiness assessment (§13, §66).

## 13. Normalization standard

Applying the governing prompt's own four-part standard to every candidate in §12's "(B)" rows: (1) governance ownership is already closed for all of them (DIST-GEN.8 §7, consumed unchanged in §4/§7 above); (2) existing 15K/16K/18K/TEN_K/HM behavior can be proven strict zero-delta by re-running every existing golden trace and isolation test unchanged, since normalization would only change *where* a value is declared, never its runtime value; (3) no target-specific evidence is lost — `PreferredCoreWeeks`/`PeakVolumeBand`/`SelectedPeakReference`/`PreferredAbsolutePeakLongRunKm` (the genuinely independent or exact-target fields) are explicitly excluded from any normalization candidate; (4) provenance remains fully auditable if the shared component itself carries a doc comment citing DIST-GEN.8 §7 as its authority source, and runtime behavior does not become inferred or interpolated at any point — a shared component is a **direct reference to an already-frozen value**, never a computed one. All four criteria are met for the `SHARED_LEVEL_FREQUENCY_AUTHORITY` and `SHARED_PARENT_FAMILY_AUTHORITY` fields identified in §12. Normalization here is bookkeeping of an already-decided fact, not a new decision — it creates no new authority.

## 14. Shared growth ownership

`PreferredWeeklyIncreaseRate`/`HardWeeklyIncreaseRate` (0.07/0.08) are duplicated as literal fields on `Default` (TEN_K), `HalfMarathonIntermediate4D` (canonical HM), and all three projected-target instances — five separate declarations of the same two numbers, all traceable to the same `SHARED_LEVEL_FREQUENCY_AUTHORITY` classification. **Return: `NORMALIZATION_READY`.**

## 15. Shared absolute-cap ownership

`AbsoluteWeeklyIncreaseCapKm = 2.5` is likewise duplicated identically across the same five instances. Same governance status, same evidence. **Return: `NORMALIZATION_READY`.**

## 16. Shared LR-share ownership

The LR share quadruple (0.30/0.36/0.33/0.40) is duplicated identically across the same five instances (with `HalfMarathonIntermediate6D` as the one documented, correctly *different*-valued exception at 0.28/0.36/0.28/0.36 — confirming this is genuinely frequency-sensitive, not distance-sensitive, exactly as DIST-GEN.8 concluded). **Return: `NORMALIZATION_READY`**, with the frequency-sensitivity already correctly represented in code today (the 6D instance's own distinct values), so any future shared-authority type must be keyed by frequency, not merely by level.

## 17. Shared starting-LR ownership

`StartingAbsoluteLongRunCapKm` is `null` at 4D for every one of TEN_K/15K/16K/18K/HM, and non-null (12.0) only at HM Intermediate 6D and HM Intermediate 5D — confirming the field's real governance is frequency-driven, not distance-driven. This preserves the binding semantic distinction (cap ≠ default, cap ≠ readiness) unchanged. **Return: `NORMALIZATION_READY`**, same caveat as §16 (frequency-keyed, not distance-keyed).

## 18. Parent taper ownership

Taper duration/multipliers/mechanism are governance-closed as `SHARED_PARENT_FAMILY_AUTHORITY` (DIST-GEN.8 §7, reaffirmed unchanged through DIST-GEN.9/10/11). Current code represents this inconsistently (§12): two independently-declared, byte-identical classes (15K, 16K) plus one direct cross-reference (18K → 16K's class). **Return: `NORMALIZATION_READY`** — this is the single clearest normalization candidate in the entire matrix, since the *current* representation is not even internally consistent with itself, let alone with its own governance conclusion. The correct eventual owner is an explicit HM-parent taper authority (e.g. a single `HalfMarathonParentTaperAuthority` referenced by every Intermediate×4D HM-family instance, canonical HM included), not a per-target class at all.

## 19. Phase-topology ownership

Every projected target reuses the identical `HALF_MARATHON__4D__INTERMEDIATE` catalog candidate and its real phase-allocation resolver (`CatalogPhaseAllocationResolver`) unmodified — phase splits (e.g. 12W → 2/3/5/2) are computed once, generically, from the reused candidate's own topology and the resolved horizon, never duplicated per target. No duplication exists here to normalize; this is already `ALREADY_NORMALIZED`.

## 20. Peak-volume three-point pattern

Re-examining DIST-GEN.9's own concern (§37 of the governing prompt: interior stability ≠ band boundary) directly: 15K/16K/18K all converge on [34.0, 46.0]/40.0. This convergence was reached via the *same internal overlap-derivation methodology* applied three times, against sparse-to-moderate external evidence each time (strongest at 15K/16K, weakest at 18K per DIST-GEN.9 §27). A methodology that consistently produces the same output when applied to inputs spanning a wide range (15.0 to 18.0km, a 3km spread against a ~46km peak-volume ceiling) is at least as plausibly explained by the methodology's own limited resolving power at this granularity as by genuine underlying stability — DIST-GEN.9 itself reached exactly this conclusion (`INSUFFICIENT_PATTERN_EVIDENCE`, not `INTERIOR_STABILITY_SUPPORTED`) and nothing in this phase's own re-examination changes that. **Return: `METHODOLOGY_CANNOT_DISCRIMINATE`.** No band boundary is proposed.

## 21. Selected-peak three-point pattern

`SelectedPeakReference` = 40.0 for all three is a direct, deterministic consequence of the peak-volume-band methodology in §20 (the selected reference is derived from the same band each time), not an independently-arrived-at third data point. Counting it as separate confirming evidence would double-count the same underlying methodology-resolution-limit finding. **Return: `METHODOLOGY_CANNOT_DISCRIMINATE`** (inherited from §20, not independently re-earned).

## 22. Calibration three-point pattern

`CalibrationStartingVolumeKm` = 23.5 for all three is likewise a mechanical consequence of the shared HM-derived ratio methodology (peak × a fixed HM calibration/peak ratio, rounded to the nearest 0.5km grid) applied to the already-shared 40.0 peak from §20/§21 — not an independent third confirmation. **Return: `METHODOLOGY_CANNOT_DISCRIMINATE`**, same inheritance caveat.

## 23. Horizon three-point pattern

`MinimumCoreWeeks` = 10 for 15K/16K/18K/HM is `SHARED_PARENT_FAMILY_AUTHORITY` (§4/§7) — a real structural fact about the reused catalog's own phase-minimum sum, not a converged decision, and 18K changes nothing here since it reuses the identical candidate. `PreferredCoreWeeks`/`MaximumCoreWeeks` (12/14) remain `INDEPENDENT_TARGET_AUTHORITIES_SAME_VALUE`, unchanged classification from DIST-GEN.8 — 18K's own independent re-derivation landing on the same numbers is real convergence evidence (unlike §20-22, this was **not** mechanically forced by a shared upstream methodology; DIST-GEN.9 §27-29 shows a genuinely separate research pass was performed), but convergence alone still does not establish a *boundary* (§37 below) — it remains `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY`, unchanged.

## 24. Absolute-peak-LR pattern

Current values: 15K → 14.5, 16K → 15.0, 18K → 16.0 (source-verified, `VolumeSafetyPolicy.cs` lines 247/390 and the 15K instance). This field is the one place three genuinely different, independently-frozen values exist. Per the governing prompt's own explicit prohibition (§28: no `+0.5 per km`, no percentage-of-distance formula, no linear interpolation), this phase does **not** attempt to fit or name a rule from three points. Margins (target − ceiling): 15K = 0.5, 16K = 1.0, 18K = 2.0, HM = ≈2.1 — DIST-GEN.9 §40 already disclosed this sequence as "monotonically non-decreasing," explicitly as an *observation*, never a derivation, and this phase preserves that same discipline. **Return: exact-target curation, with a disclosed but non-authoritative monotonic pattern** — not a rule, not a band signal, not `INSUFFICIENT_EVIDENCE` (three real, distinct, evidence-grounded values exist). This field is correctly and permanently `EXACT_TARGET_SPECIFIC`.

## 25. LR-below-target invariant

Reviewed across DIST-GEN.0/1/5/8/9: the invariant (peak long run must remain strictly below the target race distance) has been affirmed at every single closure — 14.5<15.0, 15.0<16.0, 16.0<18.0, 19.0<21.0975 — with zero counter-example ever found or authorized. DIST-GEN.9 §38 additionally cross-checked (never derived from) one weak external corroboration. No new evidence surfaced in this phase questions it. **Return: `RULE_REAFFIRMED`.**

## 26. Target-race-pace genericity

Unchanged and unmodified across all three public activations (DIST-GEN.3/7/11); each target's own `TargetFinishTimeSeconds`/`TargetFinishTimeSource` flows through the identical generic resolver. Three real public interior targets now exercise this mechanism in production. **Classification: `GENERIC_CONTINUOUS_MECHANISM`, confirmed, not reopened.** No per-target pace authority exists or is warranted.

## 27. Riegel genericity

Same status — zero Riegel-specific code change across any of the nine DIST-GEN implementation/activation phases to date. **`GENERIC_CONTINUOUS_MECHANISM`, confirmed.**

## 28. Goal-feasibility genericity

Feasibility mechanism (continuous, distance-parametric) remains fully separate from any product-average default; no new threshold has been introduced by any of the three activations. **`GENERIC_CONTINUOUS_MECHANISM`, confirmed**, product-average behavior unchanged (`SOURCE_SILENCE` on any distance-specific default per DIST-GEN.9/10/11's own consistent finding).

## 29. Readiness genericity

Readiness semantics (explicit-zero policy, missing-volume handling, recent-longest-run) are target-independent for all three cells — proven by the full monolithic regression's own continued coverage of readiness-adjacent tests at every phase, with zero target-specific score or threshold anywhere in the readiness resolver. **`GENERIC_INFRASTRUCTURE`, confirmed.** Future authority owner: unchanged, level/frequency-scoped exactly as today, no distance dimension.

## 30. Calendar/adaptation genericity

Both remain fully generic infrastructure, confirmed unmodified across all three activations, with zero target-specific branch anywhere. **`GENERIC_INFRASTRUCTURE`.** Neither participates in, nor should ever participate in, distance banding.

## 31. Field-by-field bandability

| Field | Classification |
|---|---|
| Core horizon minimum | `PARENT_FAMILY_INVARIANT` |
| Core horizon preferred | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` |
| Core horizon maximum | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` |
| Phase minima | `PARENT_FAMILY_INVARIANT` |
| Phase allocation | `PARENT_FAMILY_INVARIANT` |
| Peak-volume band | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` — the convergence is a methodology artifact (§20), not distance-band evidence |
| Selected peak | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` (inherited from peak-volume, §21) |
| Growth rates | `LEVEL_FREQUENCY_INVARIANT` |
| Absolute weekly cap | `LEVEL_FREQUENCY_INVARIANT` |
| Calibration | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` (mechanical consequence, §22) |
| LR share quadruple | `LEVEL_FREQUENCY_INVARIANT` |
| Absolute peak LR | `EXACT_TARGET_SPECIFIC` |
| Starting LR cap | `LEVEL_FREQUENCY_INVARIANT` |
| Taper duration | `PARENT_FAMILY_INVARIANT` |
| Taper multipliers | `PARENT_FAMILY_INVARIANT` |
| Taper mechanism | `PARENT_FAMILY_INVARIANT` |
| Target-race-pace | `CONTINUOUS_GENERIC_MECHANISM` |
| Riegel | `CONTINUOUS_GENERIC_MECHANISM` |
| Goal feasibility | `CONTINUOUS_GENERIC_MECHANISM` |
| Readiness | `GENERIC_INVARIANT` |
| Calendar | `GENERIC_INVARIANT` |
| Adaptation | `GENERIC_INVARIANT` |

Zero fields are `DISTANCE_BAND_READY`. Two fields remain genuine, honest band-candidates without a supported boundary (`DISTANCE_BAND_CANDIDATE_BUT_NOT_READY`: horizon preferred/maximum). Three fields that appeared to be band candidates in earlier phases are more precisely reclassified here (not overturned — refined) as `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` once the mechanical-consequence relationship between peak volume, selected peak, and calibration is made explicit (§20-22) — this is the one genuine analytical refinement this phase contributes beyond restating DIST-GEN.8/9, and it moves fields *away* from banding, not toward it.

## 32. Band-boundary problem

Applying the governing prompt's own worked example (§37) directly: even where 15K/16K/18K share a peak-volume value, nothing in the evidence establishes whether that shared behavior begins at 13K, 14K, or 15K, nor whether it ends at 18K, 19K, or 20K. No source consulted in this engagement — internal derivation or external research — has ever produced a boundary claim for any field. The band-definition standard requires **both** (1) legitimate authority-sharing within the band and (2) defensible boundary evidence; only (1) has ever been argued (weakly, per §20-22) for any field, and (2) has never been argued for any field at all. This is not a gap this phase can close with more internal analysis — it requires either dedicated boundary-testing external evidence (unlikely to exist, given DIST-GEN.9's own finding that even single-point 18K/20K evidence was sparse) or additional real target points specifically chosen to bracket a hypothesized boundary. Neither exists today.

## 33. Fourth-target information need

Evaluated against the actual remaining architecture and evidence gaps (not assumed): the two live candidates are 20K (near-HM upper probe) and 12K (near-TEN_K lower probe), with 10-mile-exact as a distinct, non-"fourth-target" precision question (§36). Neither 20K nor 12K is assumed to "automatically win" — both are assessed on their own merits below.

## 34. 20K analysis

Does 20K test where projected authority converges to canonical HM? Plausibly, but DIST-GEN.9's own research finding (§5 above) already shows 20K shares 18K's exact evidence-sparsity problem — a 20K closure would very likely repeat 18K's own `EVIDENCE_INFORMED_PRODUCT_DEFAULT`-heavy outcome rather than newly resolving anything, and DIST-GEN.4 §25/DIST-GEN.8 §34 both already predicted 20K would show "near-total convergence toward HM's own numeric authority... a real, useful data point... but it teaches comparatively less about the ambiguous middle of the interior range than 18K does, since HM's own values are already fully known and 20K is unlikely to surprise." Nothing in this phase's own analysis changes that prediction — if anything, §20-22's mechanical-consequence finding makes a near-HM 20K closure *more* predictable, not less, since peak-volume/selected-peak/calibration would almost certainly land at or very near HM's own 43.0/25.0 pair via the same derivation methodology. Could HM authority already be directly reusable for 20K? This is architecturally plausible but has never been evidenced — asserting it without dedicated 20K evidence would be exactly the kind of unevidenced numeric claim this engagement's discipline prohibits. Dedicated 20K evidence availability: poor, per DIST-GEN.9's own direct finding.

## 35. 12K analysis

Unchanged from DIST-GEN.4/8: 12K still requires resolving the unresolved 10-12K structural-parent boundary (does it truly belong to HALF_MARATHON, or does the immediate post-TEN_K band need its own architecture?) as a *prerequisite* to any numeric authority work — nothing about having three HM-parent projected targets closed changes this, since none of them tested the boundary near TEN_K; all three sit comfortably inside the already-approved interior band. This phase's own three-target proof (§6) demonstrates the *architecture* generalizes cleanly to a third interior point, but says nothing about whether HALF_MARATHON is the correct parent one target-distance closer to TEN_K. 12K therefore remains the higher-risk, boundary-confounded choice DIST-GEN.4/8 both already correctly identified it as. Not chosen here for the same reason, honestly reconfirmed rather than silently assumed a third time.

## 36. 10-mile exact analysis

Reviewing the full chronology (DIST-GEN.4 `DECISION_REQUIRED` → DIST-GEN.8/9 `INSUFFICIENT_EVIDENCE`, with the one intervening data point — DIST-GEN.5's Hal Higdon 15K/10-mile combined-program observation — weighing *against* a clean alias, not for one): three consecutive phases have now examined this question and found nothing new. The three architectural options remain exactly as DIST-GEN.4 framed them: (A) alias to 16K training authority — still not evidenced as equivalent, and the one relevant external data point argues the opposite; (B) fully distinct authority — safe but wastes evidence overlap that may exist; (C) explicit alias model with separate identity/authority keys — architecturally sound (DIST-GEN.8 §37 described it precisely) but never approved, since it would itself constitute a new authority claim (an equivalence claim) without evidence. This phase's own honest assessment: **remains `DECISION_REQUIRED`/`INSUFFICIENT_EVIDENCE`, unchanged**, and the marginal information gain from a fourth consecutive re-examination without new external evidence is low. It is architecturally "ripe" in the sense that the identity/authority-separation model now has three additional real-world proofs it works (§6), but ripeness of *architecture* does not substitute for the missing *training-science* evidence the alias itself would require. Current fail-closed behavior (16.0934 rejected) remains correct and is not loosened here.

## 37. Authority-alias model

Conceptually (not implemented): `RequestedTargetDistanceKm = 16.0934`, `TrainingAuthorityKey` resolving to the already-governed 16K authority, `PublicEligibility` as an explicit, separately-approved alias cell — never nearest-target inference. Current persistence architecture (`RequestedTargetDistanceKm`/`CanonicalDistanceFamily`, two plain columns) already supports storing the exact requested value independently of whatever authority resolves it, so **no schema change would be required** to implement this model if it were ever evidenced and approved. The registry shape (`ApprovedDarkTargetDistanceCell`) would need one additional field (a training-authority-owner key, distinct from its own `TargetDistanceKm`) to represent an alias cleanly — a small, identified gap, not a blocker, and not built here since no alias has been approved.

## 38. Exact-target registry continuation

The registry has now scaled cleanly through three targets with zero mechanism change on the public side (§9) and linear-but-bounded branch growth on the dark-dispatch side (§11). Advantages reconfirmed: fail-closed by construction, clear per-field provenance, easy, twice-proven rollback (§51). Costs reconfirmed: each target still requires its own full authority-closure research phase, and the dispatch cascade's branch count grows with every addition. Assessment: the current architecture can sustain **one, arguably two, more targets before the dispatch-cascade debt becomes a real correctness risk rather than a readability one** — but continuing to add targets without addressing §11 would compound, not merely repeat, that risk, since each new target multiplies the *number* of hand-edited branches across the *same* four call sites simultaneously.

## 39. Shared-component normalization option

A dedicated next phase that adds no target and changes no runtime behavior, moving already-governance-closed authorities (§14-18) to their correct implementation owners while proving strict zero-delta for TEN_K/15K/16K/18K/HM. This directly answers "is normalization now strategically valuable before a fourth target?" — **yes**, for two independent reasons: (1) DIST-GEN.8 §39's own stated trigger condition ("when and if a third target is added") is now met; (2) the dispatch-cascade debt (§11), not merely the `VolumeSafetyPolicy` field duplication, is the higher-value normalization target, and it has now been explicitly flagged and deferred in three consecutive phases (DIST-GEN.9 §62, DIST-GEN.10 §57/§75, DIST-GEN.11 §89/§90) — this is the longest-running, most concretely-specified piece of unaddressed technical debt anywhere in this engagement.

## 40. Frequency-expansion analysis

Genuinely new axis (target × frequency orthogonality has never been tested for any projected target — all three exist only at 4D). Real new authority would be required at minimum for LR-share differences at 3D (no KEY2 lane) and 5D/6D (KEY2 secondary-quality-lane interaction, plus the already-observed frequency-driven `StartingAbsoluteLongRunCapKm` and distinct LR-share-quadruple pattern at HM Intermediate 6D). This is legitimate future work but is evaluated here as *lower* priority than normalization specifically because layering a new axis (frequency) onto the existing un-normalized dispatch cascade (§11) would compound the branch-multiplication problem across two dimensions (target × frequency) rather than one, making the eventual normalization *harder*, not easier, to perform later.

## 41. 3D experiment analysis

3D avoids KEY2 entirely, making it the structurally cleanest frequency probe — but the governing prompt's own hint (§49) that 3D "may have stronger LR-share differences" is a real, disclosed unknown, not a decided fact; a 3D closure would need genuine new evidence-gathering, not simply carrying over 4D's own LR-share values. Deferred behind normalization, not rejected outright.

## 42. 5D experiment analysis

5D introduces KEY2/secondary-quality-lane semantics, meaningfully higher authority cost than 3D for correspondingly more architecture learning (it would test target-distance × KEY2 interaction, an axis never yet exercised for any projected target). Deferred behind both normalization and 3D.

## 43. 6D experiment analysis

6D carries the highest session-distribution complexity of the three frequencies (confirmed directly in source: `HalfMarathonIntermediate6D`'s own distinct LR-share quadruple and non-null `StartingAbsoluteLongRunCapKm`, the most divergent instance in the entire `VolumeSafetyPolicy.cs` file). Correctly the last frequency to attempt, exactly as DIST-GEN.4's own original ordering assumed and as this phase reconfirms.

## 44. Level-expansion analysis

Beginner/Advanced projected targets are evaluated, Experienced correctly left excluded (not reopened). Both carry higher authority cost than any option evaluated above: Beginner because eligible-frequency assumptions cannot be transferred from HM's own existing Beginner matrix without independent verification (a shorter target does not automatically make a previously-ineligible frequency eligible — the governing prompt's own §53 correctly warns against this), Advanced because of hard-stimulus/KEY2 structural differences and higher peak-volume authority that has never been evidenced at any projected target. Deferred behind every other option — the highest training-authority burden of any candidate examined, with no compensating architecture-learning value this phase can identify.

## 45. Product-value evidence

No product telemetry, usage-analytics, or internal demand-signal source exists anywhere in this repository for 15K/16K/18K/20K/10-mile/frequency demand — confirmed by direct search, consistent with DIST-GEN.4 §41 and DIST-GEN.8 §45's own identical prior findings. **`SOURCE_SILENCE`**, unchanged. External race prevalence (10-mile and 15K's own well-documented commercial training-plan ecosystems vs 18K/20K's sparsity, per DIST-GEN.9) is retained as context only, never as a substitute for internal usage evidence, and is not used anywhere in this report's next-phase decision as if it were product-demand evidence.

## 46. Supplemental external research

None performed. Per the governing prompt's own external-research policy (§56: reuse existing evidence first, research narrowly only where a specific new question — 20K information value, 10-mile alias significance, 12K parent-boundary relevance, band-boundary evidence — genuinely requires it): 20K's evidence-sparsity question was already directly answered by DIST-GEN.9's own research (§5/§34 above); 10-mile-alias significance has already been examined three times with diminishing returns (§36); 12K's blocker is an architecture question, not an evidence question, and no external search resolves "is HALF_MARATHON the correct parent near TEN_K" (§35); no field is close enough to boundary-readiness (§32) to make a targeted boundary search worthwhile. No genuine new external-research need was identified this phase.

## 47. Formula/interpolation reassessment

Reassessed per §58 of the governing prompt: nothing in this phase's own analysis (three real target points now available, plus TEN_K/HM) changes the standing prohibition on linear regression, curve fitting, splines, distance-ratio scaling, or nearest-neighbor derivation for training authority. The absolute-peak-LR margin sequence (§24) remains the single most "formula-shaped" pattern in the entire dataset, and this phase explicitly declines to fit it, for the same reason DIST-GEN.9 §40 already gave: three points admit infinitely many curves, and no external evidence supports selecting one over another. **Formula-model authority remains unsupported; this is unchanged, not newly re-confirmed by additional derivation.**

## 48. Test-architecture scalability

Current DIST-GEN test files are a **mixed** structure, per direct inventory: `Dark15K`/`Dark16K`/`Dark18KTargetDistanceProjectionTests.cs` are already `[Theory]`/`[InlineData]`-parameterized internally (scaling cleanly to new data rows within a file), but the public-activation proof files (`DistGen3`/`DistGen7`/`DistGen11PublicActivationTests.cs`) have been **additive new files per phase** rather than one growing parameterized suite — and this additive pattern is precisely what produced the recurring stale-literal failure mode (§59 below): a new target's own admission directly invalidates an old file's "N is not yet supported" InlineData row, an error class that has now occurred **five times** across DIST-GEN.7/10/11 (verified directly: DistGen3's 15.0→14.0 substitution by DIST-GEN.7; Dark15K's two 18.0 removals by DIST-GEN.10; DistGen7's 18.0→17.0, Dark18K's test rewrite, and a second, initially-missed DistGen3 18.0→19.0 substitution, all by DIST-GEN.11 — the last one caught only by a full regression run, not by diff review). Recommended future structure: a shared, data-driven cross-target isolation/unsupported-matrix test parameterized over the full current `ApprovedPublicCells` set (read at test-collection time, not hardcoded per test), so that both the "these targets are supported" and "these targets are not" proofs scale automatically as targets are added, eliminating this entire recurring failure class — while explicit per-target golden-trace tests remain separately authored (§49). No refactor performed here.

## 49. Golden-trace ownership

Explicit per-target golden traces (15K's, 16K's, and 18K's own real full-pipeline W1-W12 traces) must remain independently present and auditable regardless of any future test or implementation normalization — they are the primary evidence artifact proving each target's own authority was correctly implemented, not merely declared. Any future normalization phase (§39) or test-architecture change (§48) must preserve these traces byte-for-byte or re-prove them explicitly; normalization must never be allowed to quietly delete or merge them into a single "representative" trace.

## 50. Three-phase expansion-template decision

The authority-closure → dark-implementation → public-activation sequence has now succeeded three complete times (16K across DIST-GEN.1/2/2A/2B/3; 15K across DIST-GEN.5/6/7; 18K across DIST-GEN.9/10/11) with a consistent shape each time: dark implementation always extends the same registry/dispatch pattern; public activation always amounts to one registry line plus a mirrored test suite; both stages have independently, repeatedly proven zero-delta to every prior target. **Return: `PROJECTED_TARGET_AUTHORITY_DARK_PUBLIC_TEMPLATE_STANDARDIZED`.** This is a governance conclusion only — it does not itself launch a fourth target, and per §39/§64 below, the very next phase recommended is explicitly *not* another instance of this template, since the template's own known weak point (dispatch-cascade growth) should be fixed before the template is run a fourth time.

## 51. Rollback model

Verified conceptually and by direct test-seam execution for two of the three targets (15K via DIST-GEN.7's inspection-plus-lifecycle proof, 18K via DIST-GEN.11's stronger isolated-in-memory-list proof — the exact seam mechanism this phase's own governing prompt names as acceptable): each target's public admission can be removed independently (a single list-entry deletion) with zero effect on the other two targets' own public admission, zero effect on dark authority, and zero effect on an already-confirmed plan's own read/complete/not-today/cancel operability, since none of those call sites ever consult the public registry. No target-specific weakness has been found in either proof. **Classify: `ROLLOUT_BOUNDARY_PROVEN_GENERIC`.**

## 52. Hard-code audit

Full literal-string/pattern grep performed directly against current production source (`backend/RunningApp.Application/`, `backend/RunningApp.Domain/`) for `15.0`, `16.0`, `18.0`, `ApprovedPublicCells`, `ApprovedDarkCells`, `TargetDistance15`, `TargetDistance16`, `TargetDistance18`. Every meaningful hit classifies as `CORRECT_EXACT_TARGET_AUTHORITY`, `CORRECT_PUBLIC_GATE`, `CORRECT_DARK_GATE`, or `SCALABILITY_DEBT` (the dispatch-cascade sites already covered in §11) — consistent with DIST-GEN.4/8's own prior audits reaching the same set with the same classifications. Two incidental, unrelated numeric coincidences were found and correctly excluded: `V1BeginnerThreeDayVolumeEligibilityPolicy.TaperBreakEvenPreTaperKm = 16.0d` (a weekly-volume threshold, not a target distance) and `CoreEntryReadinessResolver.WeeklyVolumeThresholdReady = 15.0` (a readiness threshold, not a target distance). **No `BUG`-classified finding exists.** Two stale, non-functional doc-comment findings were identified (not classified as bugs, since neither affects behavior): `Dark16KPilotEligibilityPolicy.cs`'s own 18.0 dark-entry comment still reads "never added to ApprovedPublicCells" (false since DIST-GEN.11); `goal_distance_formatter.dart`'s doc comment still says the only approved target is 16.0. Both are safe, low-priority comment corrections — recorded here for a future phase's own minimal cleanup, not fixed in this governance-only phase.

## 53. Shared-authority table

| Field | Conceptual owner | Current code owner | Duplicated? | Normalization needed? | Zero-delta risk | Recommended implementation owner |
|---|---|---|---|---|---|---|
| PreferredWeeklyIncreaseRate / HardWeeklyIncreaseRate | Intermediate×4D level/frequency authority | 5 separate `VolumeSafetyPolicy` instance literals | Yes | Yes | Low (identical literal today; a single shared reference cannot diverge) | A shared `Intermediate4DGrowthAuthority` constant/record, referenced (not copied) by every consuming instance |
| AbsoluteWeeklyIncreaseCapKm | Intermediate×4D level/frequency authority | Same 5 instances | Yes | Yes | Low | Same shared component as above |
| LR share quadruple | Intermediate×4D level/frequency authority (frequency-keyed — 6D differs) | Same 5 instances at 4D, one distinct 6D instance | Yes (at 4D) | Yes | Low | A frequency-keyed shared `LongRunShareAuthority` component |
| StartingAbsoluteLongRunCapKm | Frequency authority (null at 4D, non-null at 5D/6D) | Independently declared per instance | Yes (trivially, all null at 4D) | Yes, low priority | Very low | Same frequency-keyed component as above, or left as-is given near-zero risk |
| Taper duration/multipliers/mechanism | HM-parent authority | 2 duplicated classes (15K/16K) + 1 direct cross-reference (18K→16K) | Yes, inconsistently | Yes — highest priority | Low, but representational inconsistency itself is the finding | A single `HalfMarathonParentTaperAuthority`, referenced identically by canonical HM and every projected target |
| MinimumCoreWeeks (10) | HM-parent structural authority | 3 independently declared horizon-policy literals | Yes | Yes, low priority | Very low | Reference the same HM-parent structural constant `RaceHorizonPolicy.HalfMarathonMinimumSupportedStandaloneWeeks` already carries, rather than a fresh per-target literal |

## 54. Exact-target-sensitive table

| Field | 15K | 16K | 18K | HM anchor | Evidence tier | Why not shared | Band status | New evidence needed to generalize |
|---|---|---|---|---|---|---|---|---|
| PreferredCoreWeeks | 12 | 12 | 12 | 14 | 15K/16K: `STRONG_EVIDENCE_DERIVED_DECISION`-adjacent; 18K: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | Independently re-derived each time, not a shared rule | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` | A defensible boundary source (where does 12-week preference stop applying?) |
| MaximumCoreWeeks | 14 | 14 | 14 | 16 | Same as above | Same | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` | Same |
| PeakVolumeBandMin/Max | 34/46 | 34/46 | 34/46 | 36/50 | 18K weakest (§5) | Convergence is a methodology artifact (§20) | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` | Independent (non-overlap-methodology) external evidence, or a genuinely different-value fourth point that breaks the pattern |
| SelectedPeakReference | 40.0 | 40.0 | 40.0 | 43.0 | Derived, see peak-volume | Mechanical consequence | Same as peak-volume | Same |
| CalibrationStartingVolumeKm | 23.5 | 23.5 | 23.5 | 25.0 | Derived | Mechanical consequence of shared peak+ratio | Same | Same |
| PreferredAbsolutePeakLongRunKm | 14.5 | 15.0 | 16.0 | 19.0 | 15K/16K stronger; 18K `EVIDENCE_INFORMED_PRODUCT_DEFAULT` but strengthened per DIST-GEN.9 §59 | Three genuinely distinct values, mechanically forced by the LR-below-target invariant | `EXACT_TARGET_SPECIFIC`, permanently | A dedicated deterministic rule would need external evidence spanning many more real target points than three — not expected to ever generalize |

## 55. Full bandability table

Combines §31's classification with current owner/provenance/normalization-readiness for the full mandatory row set (repeats §31 with the additional required columns):

| Field | TEN_K | 15K | 16K | 18K | HM | Current owner | Provenance | Bandability | Normalization readiness | Recommended future owner |
|---|---|---|---|---|---|---|---|---|---|---|
| Core horizon minimum | 8 | 10 | 10 | 10 | 10 | Per-instance literal | Parent-family | `PARENT_FAMILY_INVARIANT` | `NORMALIZATION_READY` | HM-parent structural constant |
| Core horizon preferred | 12 | 12 | 12 | 12 | 14 | Per-instance literal | Independent-same-value | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` | Not applicable (target-owned) | Unchanged, per-target |
| Core horizon maximum | 14 | 14 | 14 | 14 | 16 | Per-instance literal | Independent-same-value | `DISTANCE_BAND_CANDIDATE_BUT_NOT_READY` | Not applicable | Unchanged |
| Phase minima | — | shared | shared | shared | own | Reused HM candidate | Parent-family | `PARENT_FAMILY_INVARIANT` | `ALREADY_NORMALIZED` | No change |
| Phase allocation | — | shared | shared | shared | own | Reused HM candidate | Parent-family | `PARENT_FAMILY_INVARIANT` | `ALREADY_NORMALIZED` | No change |
| Peak-volume band | 30/42 | 34/46 | 34/46 | 34/46 | 36/50 | Per-instance literal | Independent-same-value (methodology artifact) | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` | Not applicable | Unchanged, per-target |
| Selected peak | 38.0 | 40.0 | 40.0 | 40.0 | 43.0 | Per-instance literal | Derived | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` | Not applicable | Unchanged |
| Growth rates | 0.07/0.08 | same | same | same | same | Per-instance literal | Level-frequency | `LEVEL_FREQUENCY_INVARIANT` | `NORMALIZATION_READY` | Shared growth-authority component |
| Absolute weekly cap | 2.5 | same | same | same | same | Per-instance literal | Level-frequency | `LEVEL_FREQUENCY_INVARIANT` | `NORMALIZATION_READY` | Same component |
| Calibration | 24.0 | 23.5 | 23.5 | 23.5 | 25.0 | Per-instance literal | Derived | `EVIDENCE_FAVORS_SHARED_NON_DISTANCE_AUTHORITY` | Not applicable | Unchanged |
| LR share quadruple | 0.30/0.36/0.33/0.40 | same | same | same | same | Per-instance literal | Level-frequency | `LEVEL_FREQUENCY_INVARIANT` | `NORMALIZATION_READY` | Shared, frequency-keyed component |
| Absolute peak LR | null | 14.5 | 15.0 | 16.0 | 19.0 | Per-instance literal | Independent-different-value | `EXACT_TARGET_SPECIFIC` | Not applicable | Unchanged, permanently per-target |
| Starting LR cap | null | null | null | null | null (4D) | Per-instance literal | Level-frequency | `LEVEL_FREQUENCY_INVARIANT` | `NORMALIZATION_READY`, low priority | Shared, frequency-keyed component |
| Taper duration | 1 | 2 | 2 | 2 | 2 | 2 dup classes + 1 cross-ref | Parent-family | `PARENT_FAMILY_INVARIANT` | `NORMALIZATION_READY`, highest priority | Single HM-parent taper authority |
| Taper multipliers | 0.53 | 0.70/0.43 | same | same | same | Same as above | Parent-family | `PARENT_FAMILY_INVARIANT` | `NORMALIZATION_READY` | Same |
| Taper mechanism | single-week | non-chained | same | same | same | Same as above | Parent-family | `PARENT_FAMILY_INVARIANT` | `NORMALIZATION_READY` | Same |
| Target-race-pace | generic | generic | generic | generic | generic | Shared resolver | Generic mechanism | `CONTINUOUS_GENERIC_MECHANISM` | `ALREADY_NORMALIZED` | No change |
| Riegel | generic | generic | generic | generic | generic | Shared resolver | Generic mechanism | `CONTINUOUS_GENERIC_MECHANISM` | `ALREADY_NORMALIZED` | No change |
| Goal feasibility | generic | generic | generic | generic | generic | Shared resolver | Generic mechanism | `CONTINUOUS_GENERIC_MECHANISM` | `ALREADY_NORMALIZED` | No change |
| Readiness | generic | generic | generic | generic | generic | Shared resolver | Generic infrastructure | `GENERIC_INVARIANT` | `ALREADY_NORMALIZED` | No change |
| Calendar | generic | generic | generic | generic | generic | Shared infrastructure | Generic infrastructure | `GENERIC_INVARIANT` | `ALREADY_NORMALIZED` | No change |
| Adaptation | generic | generic | generic | generic | generic | Shared infrastructure | Generic infrastructure | `GENERIC_INVARIANT` | `ALREADY_NORMALIZED` | No change |

## 56. Current architecture ownership model

```
User exact target (target_distance_km)
        ↓
Public eligibility (ApprovedPublicCells — exact-match, 3 cells today)
        ↓
Canonical structural family (CanonicalDistanceFamilyResolver — pure band resolver)
        ↓
Catalog candidate (reused HALF_MARATHON x INTERMEDIATE x4D, unchanged since DIST-GEN.1)
        ↓
Authority composition:
    SHARED_PARENT_FAMILY_AUTHORITY   (horizon minimum, phase topology, taper)
  + SHARED_LEVEL_FREQUENCY_AUTHORITY (growth rates, absolute cap, LR shares, starting-LR cap)
  + SHARED_GENERIC_MECHANISM         (pace, Riegel, feasibility, readiness, calendar, adaptation)
  + EXACT_TARGET_AUTHORITY           (horizon preferred/max, peak-volume band, selected peak,
                                       calibration, absolute peak LR — the only fields still
                                       requiring a per-target closure phase)
        ↓
plan generation (via ApprovedDarkCells-gated dispatch — currently 4 hand-edited cascade
                  sites, the one architecturally acknowledged debt in this diagram)
```

TEN_K sits outside the HALF_MARATHON-parent branch entirely (its own structural family, its own horizon fallback arm, `Default` policy instance) but shares the same `SHARED_LEVEL_FREQUENCY_AUTHORITY` layer — the strongest single piece of evidence in the whole engagement that this layer is genuinely level/frequency-scoped, not distance-scoped, since TEN_K's inclusion could not otherwise be explained. HM sits at the top of its own parent-family branch as both the *source* of parent-family authority and (at 21.0975) a canonical (non-projected) consumer of it. 15K/16K/18K are three siblings under the same parent-family branch, differing from each other and from HM only in `EXACT_TARGET_AUTHORITY`.

## 57. Option A analysis — shared-component normalization

Uncertainty resolved: none new (governance is already closed for every candidate field, §14-18) — this is an implementation-ownership question, not a training-authority question. New authority required: **zero**. External research required: **none**. Architecture learning: high — directly retires the single most-repeated deferred item in the engagement (flagged by three consecutive phases) and would make a future fourth target meaningfully cheaper (one registry entry + one exact-target-authority record, not four hand-edited cascades). Implementation cost: MEDIUM (four call sites, several `VolumeSafetyPolicy` instances, taper-class consolidation) — bounded, not LARGE, since no new numeric decision is involved and every existing test remains the zero-delta proof. Public-risk surface: none (no public/frontend/API/catalog change). Zero-delta risk: low and directly provable (every existing golden trace and isolation test must pass byte-for-byte unchanged). Long-term scalability: directly improves it (this is the entire point). Product-value evidence: none claimed or needed — this is architecture hygiene, not a user-facing feature. **Why selected: it is the only option with zero new-authority risk, the clearest, most-repeated evidence of need, and the highest leverage over every subsequent option's own future cost.**

## 58. Option B analysis — 20K fourth target

Uncertainty resolved: partial (would test near-HM convergence) but largely predictable in advance per §34/DIST-GEN.4/8's own prior analysis. New authority required: high (a full closure phase, likely landing mostly at `EVIDENCE_INFORMED_PRODUCT_DEFAULT` given confirmed evidence sparsity). External research required: yes, likely thin (per DIST-GEN.9's own direct finding). Architecture learning: low-to-moderate — would exercise the dispatch cascade a fourth time, worsening (not testing) the already-identified debt if attempted before Option A. Implementation cost: MEDIUM-LARGE (full three-phase template). Public-risk surface: real (a fourth public target). Zero-delta risk: manageable, per the now-three-times-proven template, but the recurring stale-literal failure mode (§48) would very likely recur a sixth time. Product-value evidence: `SOURCE_SILENCE`. **Why deferred: predictable outcome, thin evidence, and directly worsens the one debt Option A would fix if done first.**

## 59. Option C analysis — 12K fourth target

Uncertainty resolved: potentially high-value (the unresolved 10-12K structural-parent boundary) but requires an entirely separate architecture decision as a prerequisite, not merely a numeric closure. New authority required: high, and of a fundamentally different kind (parent-family determination, not target-specific numeric authority). External research required: yes, into a boundary question that has never been evidenced in this entire engagement. Architecture learning: potentially the highest of any option, but confounded with the parent-boundary question rather than a clean second data point. Implementation cost: unknown, likely LARGE if the parent-family answer is "no, 12K needs its own structural family." Public-risk surface: real, and higher than 20K's given the open architecture question. Zero-delta risk: unknown until the boundary question is resolved. **Why deferred: identical reasoning to DIST-GEN.4/8 — a confounded experiment, not a clean data point; the boundary question deserves and requires its own dedicated phase before any 12K numeric work, and no evidence gathered since DIST-GEN.8 changes that.**

## 60. Option D analysis — 10-mile exact alias/precision closure

Uncertainty resolved: low expected marginal gain — three prior examinations (DIST-GEN.4/8/9) have already converged on `INSUFFICIENT_EVIDENCE`, with the only new data point since DIST-GEN.4 arguing against equivalence. New authority required: potentially none (if a clean alias were ever evidenced) to moderate (if independent authority were needed instead). External research required: yes, and the same category of research that has already come up thin twice. Architecture learning: real but narrow — the alias *model* (§37) is already fully specified and requires no further architecture exploration, only evidence. Implementation cost: LOW if ever approved (no schema change needed, per §37). Public-risk surface: low (one additional precise identity, not a new training authority). Zero-delta risk: low. Product-value evidence: unknown (10-mile races are a well-established distance externally, but no internal demand signal exists). **Why deferred: this is a real, low-risk, well-specified opportunity, but its binding constraint is evidence, not architecture or implementation effort — three consecutive research passes have not moved it forward, and nothing in this phase identifies a new evidence source that would.**

## 61. Option E analysis — frequency expansion

Uncertainty resolved: genuinely new axis (target × frequency has never been tested for a projected target). New authority required: moderate-to-high depending on which frequency (3D lowest, 6D highest, per §41-43). External research required: likely, for LR-share/KEY2 interaction specific to a projected (non-canonical) target. Architecture learning: real, but this phase's own analysis (§40) identifies a sequencing risk: performing this before Option A compounds the dispatch-cascade debt across a second dimension. Implementation cost: MEDIUM-LARGE. Public-risk surface: real. Zero-delta risk: manageable if sequenced after Option A; higher if attempted before it, since a target×frequency cascade would be considerably harder to retrofit into a keyed lookup than a target-only one. **Why deferred: legitimate future axis, but sequencing behind Option A specifically reduces its own eventual implementation cost.**

## 62. Option F analysis — field-specific band closure

Per §32/§36 of the governing prompt: a field may only become `DISTANCE_BAND_READY` given both legitimate authority-sharing *and* defensible boundary evidence. §31/§55 show zero fields meet this bar today — the two genuine remaining band-candidates (horizon preferred/max) have real value-convergence but zero boundary evidence, and three previously-suspected candidates (peak volume, selected peak, calibration) are more precisely explained as methodology artifacts than as band evidence at all (§20-22). **Why deferred: not merely lower-priority but genuinely unready — this option cannot honestly be selected without inventing a boundary this engagement has explicit standing instructions never to invent (§36 of the governing prompt).**

## 63. Option G analysis — level expansion

Highest new-authority burden of any option (§44), zero compensating architecture-learning value beyond what frequency expansion would already provide, and Experienced correctly remains out of scope entirely. **Why deferred: worst risk/value ratio of any option evaluated.**

## 64. Final option comparison

| Option | New authority | External research | Architecture learning | Impl. cost | Public risk | Zero-delta risk | Scalability impact | Product evidence | Verdict |
|---|---|---|---|---|---|---|---|---|---|
| A. Normalization | None | None | High | Medium | None | Low, provable | Directly improves | N/A (infra) | **Selected** |
| B. 20K | High | Thin (confirmed) | Low-moderate | Medium-large | Real | Manageable | Worsens if before A | Silent | Deferred |
| C. 12K | High + boundary Q | Unresolved | Potentially high but confounded | Unknown, likely large | Real, higher | Unknown | Unclear until boundary resolved | Silent | Deferred |
| D. 10-mile alias | None-moderate | Thin (confirmed twice) | Narrow | Low if approved | Low | Low | Neutral | Silent | Deferred |
| E. Frequency | Moderate-high | Likely | Real | Medium-large | Real | Manageable if after A | Worse if before A | Silent | Deferred |
| F. Band closure | Would require inventing boundary | N/A | N/A | N/A | N/A | N/A | N/A | N/A | Not ready, not selectable |
| G. Level | Highest | Likely | Low | Large | Real, higher | Unknown | Neutral | Silent | Deferred |

Option A is the unique option with zero new-authority risk and the strongest, most concretely evidenced need (three consecutive phases naming the same specific debt). Every other option either reopens a genuinely unresolved architecture question (C), repeats a research pattern that has already come up thin twice (B, D), or adds new authority surface on top of an architecture the engagement's own evidence says should be fixed first (E, G). F is not merely deprioritized but actually unselectable without violating this engagement's own no-invented-boundary discipline.

## 65. Band-readiness result

**`BANDING_STILL_NOT_GOVERNANCE_READY`.** Field-level drivers: the two remaining genuine band-candidates (horizon preferred/maximum) have real convergence but zero boundary evidence (§32); the three fields that most resembled band evidence in earlier phases (peak-volume band, selected peak, calibration) are, on closer analysis this phase performs, better explained as consequences of a shared derivation methodology applied independently three times than as evidence of a true interior distance band (§20-22) — this narrows, rather than expands, the set of genuine band candidates relative to DIST-GEN.8/9's own framing, while reaching the identical bottom-line classification both of those phases already reached.

## 66. Normalization-readiness result

**`SHARED_AUTHORITY_NORMALIZATION_READY`.** Every field recommended for normalization (§14-18, §53) has closed governance ownership, provable zero-delta, no target-specific evidence loss, and full provenance auditability (§13) — the normalization standard's four criteria are met for all of them. This is a readiness assessment of implementation ownership only; it authorizes no new behavior and freezes no new number.

## 67. Registry-scalability result

**`EXACT_TARGET_REGISTRY_SCALABLE_WITH_NORMALIZATION`.** The registry containers themselves (`ApprovedDarkCells`/`ApprovedPublicCells`) are `SCALABLE_AS_IS` (§9-10), proven twice on the public side with zero mechanism change. The dispatch layer that consumes a resolved cell is not — it has grown by one hand-edited branch at four call sites with each of the last two targets, a real and now twice-repeated pattern (§11), matching `NEEDS_TYPED_REGISTRY_GENERALIZATION` exactly. The overall registry-plus-dispatch system is therefore scalable, but only *with* the normalization this phase recommends — not indefinitely as-is.

## 68. Three-phase governance result

**`PROJECTED_TARGET_AUTHORITY_DARK_PUBLIC_TEMPLATE_STANDARDIZED`.** Three complete, independent, successful executions of the same sequence (authority closure → dark implementation → public activation), each proving zero-delta to every prior target and to both canonical anchors, is sufficient repetition to standardize this as the expected lifecycle for any future projected target — while this phase's own decision (§69-70) is explicitly to pause that lifecycle, not repeat it a fourth time immediately, specifically because the template's own known weak point (dispatch-cascade growth) should be retired before it is exercised again.

## 69. Next-axis decision

**Selected: Option A, shared-authority ownership normalization and zero-delta refactor.** This is the single option that (a) requires zero new training authority or external evidence, (b) directly retires a concretely-identified, three-times-deferred, explicitly-named debt rather than a vaguely-felt one, (c) provably reduces the cost and risk of every future option (B/C/D/E/G all become safer and cheaper once the dispatch cascade is a keyed lookup instead of a hand-edited cascade), and (d) carries the lowest possible zero-delta risk of any option, since it changes no runtime value anywhere — only where each already-decided value is declared and referenced from. No other option offers this combination of maximal architecture information gain per unit of new, unsupported authority risk (the governing prompt's own §78 standard), since every other option's information gain requires *first* accepting new authority-closure risk (B, C, E, G) or repeating an evidence search that has already come up empty twice (D), or is outright unselectable without inventing unsupported boundaries (F).

## 70. Exact DIST-GEN.13 handoff

**DIST-GEN.13 — PROJECTED-DISTANCE SHARED AUTHORITY OWNERSHIP NORMALIZATION AND ZERO-DELTA REFACTOR.**

Scope, precisely bounded by this phase's own §53/§55 tables — normalize only fields already closed as `SHARED_LEVEL_FREQUENCY_AUTHORITY` or `SHARED_PARENT_FAMILY_AUTHORITY`:
1. Introduce a shared Intermediate×4D growth/cap/LR-share/starting-LR-cap authority component (frequency-keyed, since 5D/6D genuinely differ), referenced by `Default`, `HalfMarathonIntermediate4D`, and all three projected-target `VolumeSafetyPolicy` instances — never copied.
2. Introduce a single explicit HM-parent taper authority, replacing the current inconsistent mix of two duplicated classes (15K/16K) plus one direct cross-reference (18K→16K), referenced identically by canonical HM and all three projected targets.
3. Have each `TargetDistanceNNKHorizonPolicy`'s `MinimumCoreWeeks` reference the same HM-parent structural constant `RaceHorizonPolicy` already exposes, rather than an independent per-target literal.
4. Explicitly do NOT touch `PreferredCoreWeeks`/`MaximumCoreWeeks`/peak-volume band/selected peak/calibration/absolute-peak-LR — every genuinely exact-target or band-candidate-but-not-ready field (§54) stays exactly as declared today, per-target, unnormalized.
5. As part of the same phase, generalize the four dispatch-cascade call sites (§11) from hand-edited ternary/if-chains into a single keyed per-target authority lookup (e.g. a small `IReadOnlyDictionary<double, ProjectedTargetAuthority>`-shaped resolution, where `ProjectedTargetAuthority` bundles references to that target's own horizon/peak-band/absolute-LR values plus references to the shared components from (1)/(2)), so a future fourth target requires one registry entry, not four hand-edited branches.
6. Mandatory proof: every existing golden trace (15K/16K/18K, all supported horizons), every existing isolation test (three-way LR, dark-vs-public, wrong-cell, unsupported-target matrices), and the full three-suite regression must pass with **zero** behavioral delta — this is a pure implementation-ownership refactor, and any observed behavior change of any kind is an immediate stop-and-report condition.
7. Recommended, not mandatory: adopt the §48 data-driven cross-target isolation/unsupported-matrix test pattern to eliminate the recurring stale-literal failure mode for good.
8. Recommended, not mandatory: correct the two stale doc comments found in §52 as part of the same diff, since they are directly adjacent to the code being touched anyway.

Explicitly out of scope for DIST-GEN.13: any new target, any new frequency, any new level, any catalog/API/database/frontend change, any band implementation, any 10-mile-alias decision.

## 71. Existing-product zero-delta

No production behavior was changed by this phase. `git status --porcelain` shows this phase's own diff consists exactly of this report, `PHASE_LEDGER.md`, and `MASTER_ROADMAP.md` — confirmed before finalizing this report. 5K, TEN_K, HALF_MARATHON, public 15K/16K/18K, unsupported Custom targets, Habit Custom, Experienced exclusion, catalog hashes, and every target policy remain completely unmodified; every fact in §2-§55 above was read from current source or prior reports, never asserted from memory.

## 72. Remaining blockers

None. Every required analysis was completed against real, current source and the actual prior reports rather than reconstructed from this governing prompt's own text; DIST-GEN.8's ownership decisions and DIST-GEN.9's evidence limitations were consumed, not reopened; value equality was kept separate from authority equality throughout (§7, §20-24); implementation success (three real public activations) was never treated as training-science evidence (§86 of the governing prompt, honored explicitly in §5, §20-22, §34); no unsupported band boundary was invented (§32, §62); exactly one next phase was selected with an explicit, evidence-grounded comparison against every alternative (§57-64, §69); no new training values were invented and no production behavior changed (§71).

---

## Final classification

**POST_15K_16K_18K_THREE_TARGET_AUTHORITY_PATTERN_AND_NEXT_EXPANSION_CLOSED**

Every criterion holds: current 15K/16K/18K public state was frozen correctly and not reopened (§3); TEN_K/15K/16K/18K/HM authority values were reconstructed directly from current source, not memory (§2); DIST-GEN.8's ownership decisions and DIST-GEN.9's evidence limitations were consumed and preserved honestly, including the disclosed internal inconsistency in DIST-GEN.9's own tier count (§4-5); implementation success was explicitly never misused as training-science evidence (§5, §20-22, §34, §86-87 of the governing prompt); value equality was rigorously separated from authority equality at every field (§7, §20-24); shared higher-level authorities (§53) and target-sensitive authorities (§54) were both explicitly and separately identified; implementation duplication was distinguished from governance duplication throughout (§12-13); normalization readiness (§66) and exact-target registry scalability (§67) were both assessed from direct, current source inspection, not from report summaries; banding readiness was assessed field-by-field with no boundary invented anywhere (§31-32, §65); 20K was evaluated as a near-HM information probe and found to repeat 18K's own evidence-sparsity problem (§34, §58); 12K was evaluated as a lower-boundary architecture probe and found still confounded by the unresolved structural-parent question (§35, §59); 10-mile-exact identity/alias status was reassessed and found unchanged at `INSUFFICIENT_EVIDENCE` after three prior examinations (§36, §60); frequency and level expansion were both reassessed and explicitly sequenced behind normalization for a stated architectural reason, not dismissed by convenience (§40-44, §61, §63); exactly one next phase — DIST-GEN.13, shared-authority normalization — was selected via an explicit, criterion-by-criterion comparison against every other option (§57-64, §69); no new training value was invented anywhere in this report; and zero production behavior changed, confirmed directly via `git status` before finalizing (§71).
