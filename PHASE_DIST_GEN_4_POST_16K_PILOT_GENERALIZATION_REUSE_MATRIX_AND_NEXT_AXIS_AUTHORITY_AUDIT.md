# PHASE DIST-GEN.4 — Post-16K-Pilot Generalization, Reuse Matrix, and Next-Axis Authority Audit

**Classification: GOVERNANCE / ARCHITECTURE / PRODUCT-EXPANSION DECISION.** No production code, catalog artifact, frontend, API, database, or test-behavior file was touched in this phase. This phase determines which of four candidate next-expansion axes (frequency expansion, level expansion, second target distance, distance-band authority) best follows the successful public activation of the 16K/HalfMarathon-family/Intermediate/4D pilot, and hands off exactly one concrete next phase.

## 1. Current post-DIST-GEN.3 baseline

Read directly from `PHASE_DIST_GEN_3_...md`, `PHASE_LEDGER.md` row 48, and `MASTER_ROADMAP.md`'s DIST-GEN.3 subsection: the public pilot is `TargetDistanceKm=16.0 × ParentDistanceFamily=HalfMarathon × Level=Intermediate × RunsPerWeek=4 × 10–14W Standard Core (preferred 12W)`, reachable via `POST /api/v1/plans/generate-preview/race` with `goal_distance: "custom", target_distance_km: 16.0`. Confirmed HEAD at `9be1849` (DIST-GEN.3's ledger/roadmap commit), 0 ahead/0 behind `origin/main`, working tree clean before this phase began. Durable regression baseline carried forward unchanged: `PlanCatalog.Tests` 1510/1510, `RuntimeCatalog` subtree 3892/3892, full monolithic 4890/4886/4-failed (the 4 known identities: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13/14)`, `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, `Sw09ExplicitZeroReadinessEndToEndTests...`).

## 2. Proven generic infrastructure

Classified from direct code read (research pass, §4-13 below), using the taxonomy this phase defines in §3:

| Mechanism | Classification | Evidence |
|---|---|---|
| `CanonicalDistanceFamilyResolver` (band resolver) | **GENERIC_REUSABLE_INFRASTRUCTURE** | Pure band resolver (≤5.0/≤10.0/≤21.1/≤42.2), zero 16.0-specific literal anywhere in the file. |
| `TargetDistanceKm`/`target_distance_km` public request contract | **GENERIC_REUSABLE_INFRASTRUCTURE** | Additive `double?` field, no value restriction in the DTO/validator layer itself (the restriction lives entirely in the eligibility policy). |
| `GoalDistance.Custom` narrowed fail-closed guard (validator) | **GENERIC_REUSABLE_INFRASTRUCTURE** | The validator's three-way split (no-target / malformed-target / well-formed-target-flows-through) contains no 16.0-specific branch — every well-formed numeric target flows through the identical path. |
| `GeneratePreviewCommandMapper`'s canonicalization gate | **GENERIC_REUSABLE_INFRASTRUCTURE**, with one **16K_SPECIFIC_AUTHORITY** call inside it | The gate itself (resolve family → check eligibility → substitute or reject) is generic; the one call to `Dark16KPilotEligibilityPolicy.IsEligible` is the sole 16K-specific dependency. |
| `TrainingPlan.CanonicalDistanceFamily`/`RequestedTargetDistanceKm` persistence split | **GENERIC_REUSABLE_INFRASTRUCTURE** | Both fields are nullable, generic, already documented as supporting "e.g. 8.0 for an 8K request" — no schema change needed for a second target. |
| `GeneratePreviewResponse`/`PlanDetailsResponse`/`HomeResponse`/`ProfileOverviewResponse`'s `RequestedTargetDistanceKm` fields | **GENERIC_REUSABLE_INFRASTRUCTURE** | Purely additive nullable fields, sourced generically from the persisted/in-flight value, not from any 16K-specific field. |
| `goal_distance_formatter.dart` | **GENERIC_REUSABLE_INFRASTRUCTURE** | `'${requestedTargetDistanceKm.round()}K'` — no 16-specific text anywhere. |
| `GoalFeasibilityResolver`'s Riegel projection | **GENERIC_REUSABLE_INFRASTRUCTURE** | `targetDistanceKm = RequestedTargetDistanceKm ?? GoalDistanceKm`, then `Math.Pow(target/source, 1.06)` — zero distance-value branching. |
| `Dark16KTargetDistanceProjectionTests.cs` | **GENERIC_REUSABLE_INFRASTRUCTURE (test shape)** | Already `[Theory]`/`[InlineData]`-parameterized over target-km values (12.0/15.0/16.0/18.0/20.0 already appear as negative-case data). |
| `Dark16KPilotEligibilityPolicy` (the eligibility gate class itself) | **16K_SPECIFIC_AUTHORITY** | A single hardcoded quadruple (`16.0`/`HalfMarathon`/`Intermediate`/`4`), not a registry — see §38. |
| `TargetDistance16KHorizonPolicy` | **16K_SPECIFIC_AUTHORITY**, but its *shape* (call `RaceHorizonPolicy`'s generic 5-arg `Decide` directly, never the TEN_K fallback arm) is **GENERIC_REUSABLE_INFRASTRUCTURE** | The class itself is one target's authority; the pattern it establishes is reusable verbatim for a second target. |
| `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K` (numeric instance) | **16K_SPECIFIC_AUTHORITY** (in fact **16K × Intermediate × 4D-SPECIFIC**, see §12/§13) | One named static instance; its exact values do not automatically apply to any other cell or target. |
| `race_details_page.dart`'s `_isExactPilot16KMatch` | **PUBLIC_PILOT_ONLY_GATE** | Hardcoded single-value exact-match getter; a second target needs a second near-duplicate getter (see §51). |
| `DistGen3PublicActivationTests.cs`'s `Pilot16KRequest` helper | **FUTURE_GENERALIZATION_GAP** | Hardcodes `target_distance_km = 16.0` inline rather than accepting it as a parameter (see §53). |

## 3. 16K-specific authority inventory

The exact, non-reusable-as-is numeric/structural authority that is specific to the *(16.0, HalfMarathon, Intermediate, 4)* cell: `Dark16KPilotEligibilityPolicy`'s four constants; `TargetDistance16KHorizonPolicy`'s three constants (10/12/14); `VolumeSafetyPolicy.HalfMarathonIntermediate4DTargetDistance16K`'s full field set (peak band [34,46]/reference 40.0, calibration 23.5, LR quadruple 0.30/0.36/0.33/0.40, `PreferredAbsolutePeakLongRunKm=15.0`, `GoldenFixtureNonTaperTransitions=9`, 2-week 0.70/0.43 taper via `TargetDistance16KTaperVolumePolicy`); `TargetDistance16KPeakVolumeBandPolicy`'s band values. None of these six items generalizes to a different frequency, level, or target distance without new authority.

## 4. Public Custom contract (current, frozen baseline for this audit)

Confirmed unchanged since DIST-GEN.3: `Custom` without a numeric target → 422 `GOAL_DISTANCE_CUSTOM_NOT_SUPPORTED`; `Custom` with a malformed target → 400 validation error; `Custom` with a well-formed target that fails `Dark16KPilotEligibilityPolicy` (wrong distance/family/level/frequency) → 400 `UNSUPPORTED_TARGET_DISTANCE`; `Custom` with the exact eligible quadruple at 8W/9W or 15W+ → 422 `DARK_16K_CORE_HORIZON_*`; the exact eligible quadruple at 10-14W → 200. Habit Custom remains categorically unsupported (no numeric field exists on that DTO at all). This is the frozen baseline every candidate axis below is evaluated against, per §54/§55's zero-delta requirement.

## 5. Axis A definition

Hold the target distance fixed at 16.0km and the structural parent fixed at HALF_MARATHON; expand the set of publicly-eligible `(Level, RunsPerWeek)` cells beyond `(Intermediate, 4)`.

## 6. HM parent matrix (current, exact, from ledger/roadmap — not inferred)

| Level | 3D | 4D | 5D | 6D |
|---|---|---|---|---|
| Beginner | PUBLIC (HM.18) | PUBLIC (HM.18) | **PRODUCT_INELIGIBLE** (HM.13 §6) | not supported (`HM_FREQUENCY_NOT_SUPPORTED_V1`) |
| Intermediate | PUBLIC (HM.18) | PUBLIC (HM.18) | PUBLIC (HM.18) | PUBLIC (HM-X1.4) |
| Advanced | PUBLIC (HM.18) | PUBLIC (HM.18) | PUBLIC (HM.18) | PUBLIC (HM-X1.4) |
| Experienced | excluded at every frequency (EXP.0, product-wide) | | | |

2D is not supported for HM at any level (`HM_FREQUENCY_NOT_SUPPORTED_V1`). EXP.0 is confirmed product-wide (`PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED`, reconfirmed across both the 10K and HM engagements) and "may only be reopened by a future dedicated product-decision phase" — no single distance may unilaterally add Experienced.

## 7. 16K structural candidate matrix

| Candidate cell | Classification | Rationale |
|---|---|---|
| 16K Beginner 3D | `ELIGIBLE_FOR_AUTHORITY_CLOSURE` | HM Beginner 3D is PUBLIC; structural reuse is available. |
| 16K Beginner 4D | `ELIGIBLE_FOR_AUTHORITY_CLOSURE` | HM Beginner 4D is PUBLIC. |
| 16K Intermediate 3D | `ELIGIBLE_FOR_AUTHORITY_CLOSURE` | HM Intermediate 3D is PUBLIC. |
| 16K Intermediate 5D | `ELIGIBLE_FOR_AUTHORITY_CLOSURE` | HM Intermediate 5D is PUBLIC; KEY2 mechanism confirmed orthogonal to target-distance override (§18). |
| 16K Intermediate 6D | `ELIGIBLE_FOR_AUTHORITY_CLOSURE` | HM Intermediate 6D is PUBLIC (HM-X1.4); same KEY2 orthogonality. |
| 16K Advanced 3D/4D/5D/6D | `ELIGIBLE_FOR_AUTHORITY_CLOSURE` (all four) | HM Advanced matrix is fully PUBLIC at every frequency. |
| 16K Beginner 5D | `PRODUCT_INELIGIBLE` | Inherits HM Beginner 5D's own frozen product-ineligibility (HM.13 §6) — §8 below explains why this must not be reopened by inference. |
| 16K Beginner 6D | `ARCHITECTURALLY_BLOCKED` | HM itself has no Beginner 6D cell at all (`HM_FREQUENCY_NOT_SUPPORTED_V1`) — there is no structural parent to reuse. |
| 16K \*/2D (any level) | `ARCHITECTURALLY_BLOCKED` | HM itself has no 2D cell. |
| 16K Experienced (any frequency) | `ARCHITECTURALLY_BLOCKED` by product-wide policy | EXP.0 excludes Experienced from every distance; not a 16K-specific decision to make. |

"Eligible for authority closure" means structural-parent reuse is available and unblocked — it explicitly does **not** mean numeric authority is pre-approved (see §12-§13).

## 8. Beginner eligibility analysis

HM Beginner 5D's `PRODUCT_INELIGIBLE` status (frozen in `VolumeSafetyPolicy.cs`'s own `ForHalfMarathonBeginnerDaysPerWeek` dispatch, HM.13 §6) is a **level/frequency-specific product decision** about Beginner readiness at 5 days/week for a half-marathon-length race, not a decision about the 21.1km target distance specifically. A 16km target is shorter than a 21.1km target, but "16K is shorter than HM" does not by itself supply new evidence about Beginner runners' safety at 5 days/week — that is a distinct authority question (about weekly training frequency tolerance for this experience level) that DIST-GEN.0/1's own dual-anchor architecture never claimed to resolve via distance-shortening alone. Per this phase's own explicit instruction (§8 of the governing prompt), this status is **not reopened** here. The same reasoning applies with even more force to a hypothetical Beginner 6D: HM itself has never built a Beginner 6D cell at any distance, so there is no existing authority to reuse at all, structurally blocking it regardless of target distance.

## 9. Intermediate frequency analysis

HM's Intermediate row is fully public at all four frequencies (3D/4D/5D/6D), the widest of any level. This makes Intermediate the level with the *most* structurally eligible frequency-expansion candidates for 16K (3D, 5D, 6D — 4D is already done), and the level where the existing 16K pilot's own evidence (external 10-mile/16km plan sources, per DIST-GEN.1) most directly transfers, since that evidence was itself gathered at the Intermediate level.

## 10. Advanced eligibility analysis

HM's Advanced row is also fully public at all four frequencies. Structurally, all four 16K Advanced cells are `ELIGIBLE_FOR_AUTHORITY_CLOSURE` (§7). However, Advanced-level numeric authority in this codebase consistently reflects a materially different (higher-volume, dual-hard-session) training stimulus than Intermediate — e.g. HM Advanced 4D's own `ResolvedPeakReference=53.0` vs Intermediate 4D's `43.0`, a ~23% difference, not a small adjustment. Extending the 16K pilot to Advanced would require authoring genuinely new peak/LR/calibration authority informed by Advanced-specific evidence, not a scaled reuse of the Intermediate 16K numbers — a materially larger and less precedented authority-closure task than an Intermediate frequency cell, where the DIST-GEN.1 external evidence base already speaks most directly to the Intermediate runner.

## 11. Experienced exclusion

Reconfirmed unconditionally out of scope for this phase and for any future 16K-specific phase — EXP.0 is a product-wide decision, reconfirmed across GEN.4-GEN.25 (10K) and HM.10-HM.18, and its own text states it "may only be reopened by a future dedicated product-decision phase." No 16K Experienced cell of any frequency is `ELIGIBLE_FOR_AUTHORITY_CLOSURE`; all are architecturally blocked by policy, not by catalog structure.

## 12. Same-target numeric reuse (16K frequency expansion)

Direct comparison of `VolumeSafetyPolicy` HALF_MARATHON Intermediate instances by frequency (all real values, read directly):

| Field | Intermediate 3D | Intermediate 4D (16K pilot's structural parent) | Intermediate 5D | Intermediate 6D |
|---|---|---|---|---|
| Peak band | [28,40] | (16K pilot uses its own [34,46], not HM's) | [44,58] | [44,58] |
| ResolvedPeakReference (HM canonical) | 34.0 | 43.0 | 51.0 | 51.0 |
| Calibration/golden start | 20.0 | 25.0 | 29.5 | 29.5 |
| AbsoluteWeeklyIncrementCapKm | 2.0 | 2.5 | 2.5 | 2.5 |
| LR quadruple | 0.34/0.40/0.38/0.46 | 0.30/0.36/0.33/0.40 | 0.28/0.36/0.28/0.36 | 0.28/0.36/0.28/0.36 |
| PreferredAbsolutePeakLongRunKm | 19.0 | 19.0 | 19.0 | 19.0 |
| StartingAbsoluteLongRunCapKm | null | null | 12.0 | 12.0 |

**Every field except `PreferredAbsolutePeakLongRunKm` and the taper mechanism (2-week, 0.70/0.43, all frequencies) varies by frequency even within the same level and family.** This is decisive: the 16K pilot's own frozen numbers (peak band [34,46]/reference 40.0/calibration 23.5/quadruple 0.30/0.36/0.33/0.40/`AbsoluteWeeklyIncrementCapKm=2.5`) are a close numeric neighbor of HM's own Intermediate **4D** row specifically, not a target-distance-only constant — they cannot be reused unmodified for 3D, 5D, or 6D. Each candidate frequency needs its own peak band, selected peak, calibration, LR quadruple, and (for 5D/6D) starting LR cap — a real, non-trivial new-authority task per cell, informed by the same kind of cross-anchor reasoning DIST-GEN.1 used (TEN_K/HM same-frequency comparison), not merely copied from the 4D pilot.

## 13. Level-specific numeric gaps

The same conclusion holds even more strongly across levels: HM Beginner 4D uses `ResolvedPeakReference=33.0`/calibration 19.0/quadruple 0.30/0.36/0.33/0.40 (same quadruple as Intermediate 4D, but different peak/calibration/`PreferredAbsolutePeakLongRunKm=16.0` not 19.0); HM Advanced 4D uses `ResolvedPeakReference=53.0`/calibration 31.0/quadruple 0.30/0.36/0.33/0.40/`PreferredAbsolutePeakLongRunKm=19.0`. A 16K Beginner or Advanced cell would need its own new peak/calibration/peak-LR authority, informed by the corresponding HM/TEN_K level-specific evidence, exactly mirroring how DIST-GEN.1 derived the Intermediate 16K numbers from the Intermediate HM/TEN_K anchors.

## 14. Horizon reuse across cells

**Horizon authority is confirmed frequency-invariant and (for HM) level-invariant.** `RaceHorizonPolicy.DecideForDistance` dispatches purely on the `GoalDistance` enum value — there is no `DaysPerWeek` or `RunningBackground` parameter anywhere in its signature. All HM cells (any level, any frequency) share the identical 10/14/16-week bounds; `TargetDistance16KHorizonPolicy` (10/12/14) has no frequency or level parameter either — its 4D-only reachability today comes entirely from `Dark16KPilotEligibilityPolicy`'s own gate, not from the horizon policy itself. **This means a future 16K Intermediate 3D/5D/6D (or even Beginner/Advanced) cell would almost certainly reuse the identical 10/12/14 horizon authority without needing a new closure phase for horizon specifically** — a materially different and lower-risk situation than DIST-GEN.1's original 8-week mistake, which arose from conflating external evidence with a structural floor that turned out to differ by family, not by frequency/level within one family.

## 15. Phase topology

Confirmed: `plan-catalog/catalog/templates/half-marathon-master.v5.json` defines exactly one phase-topology array (FOUNDATION 2/3/4, BUILD 3/4/5, RACE_SPECIFIC 3/5/5, TAPER 2/2/2, minimum sum 10) shared by every HM frequency/level combination — frequency/level variation is expressed by widening the same master's `supportedRunsPerWeek` array and by routing different `combinations/*.json` files at distinct level-modifier/workout-progression files, never by authoring a second phase-topology JSON. This directly confirms §14's conclusion: no candidate 16K cell would risk a repeat of the DIST-GEN.1 phase-minimum-sum surprise, because that sum is a single, shared, frequency/level-invariant catalog fact for the entire HM family.

## 16. Target-race-pace reuse

Confirmed generic and target-distance-driven, not distance-value-hardcoded: `GoalFeasibilityResolver.cs` line 207 computes `targetDistanceKm = input.RequestedTargetDistanceKm ?? input.GoalDistanceKm`, then applies the Riegel exponent (1.06) generically at line 216 — zero literal `16.0` or any other distance-value branch in this file or in `PaceSourceResolver.cs`. This infrastructure would work unmodified for any future target distance or 16K frequency/level cell without new pace architecture.

## 17. KEY2 / secondary-quality implications

Confirmed structurally orthogonal to target-distance projection: the 4D→5D/6D structural change is a `RunLayout` slot-count change (a second `KEY_SESSION` slot) plus a distinct workout-progression file version (`half-marathon-workout-progression.v3.json` adds "lane-1" content over v2's single lane) — a catalog/binding-layer concern, never a code-level branch on `TargetDistanceKmOverride`/`RequestedTargetDistanceKm`. `VolumeSafetyPolicy.cs`'s own doc comments state plainly that "KEY2's workout-family selection is entirely a workout-progression/binding concern... not a volume-safety concern," and the generic multi-KEY session-volume allocator (`FourDaySessionDistanceAllocationPolicy.cs` and its 5D/6D siblings) splits weekly volume evenly across all KEY instances regardless of how many exist. **This means a future 16K Intermediate 5D/6D cell's dual-lane mechanism would mechanically "just work" once its own new peak/LR/calibration authority (§12) is frozen and `Dark16KPilotEligibilityPolicy` is widened (or a sibling class added) — no new KEY2-specific code is anticipated.**

## 18. (KEY2 detail folded into §17 per the above; this numbered slot records the one explicit caveat)

Caveat: "mechanically just work" is a structural/code-path claim, not a training-science claim — whether a dual-hard-session week is an appropriate stimulus for a runner targeting 16K specifically (rather than the full 21.1km HM distance) is exactly the kind of new numeric-authority question §12 already identifies as required before any 16K 5D/6D cell could close, and is not resolved by this structural orthogonality alone.

## 19. Axis A authority-cost matrix

| Candidate cell | New authority needed |
|---|---|
| 16K Intermediate 3D | Peak band, selected peak, calibration, LR quadruple, `AbsoluteWeeklyIncrementCapKm` (HM's own Intermediate 3D uses 2.0, not 2.5) — horizon and phase topology reused as-is (§14/§15). |
| 16K Intermediate 5D | Peak band, selected peak, calibration, LR quadruple, `StartingAbsoluteLongRunCapKm` — horizon/topology/KEY2 mechanism reused as-is. |
| 16K Intermediate 6D | Same as 5D (HM's own 5D/6D Intermediate values are identical to each other) — horizon/topology/KEY2 mechanism reused as-is. |
| 16K Beginner 3D/4D | Peak band, selected peak, calibration, `PreferredAbsolutePeakLongRunKm` (HM Beginner uses 16.0, not 19.0) — horizon/topology reused as-is. |
| 16K Advanced 3D/4D/5D/6D | Same field list as above per cell, informed by Advanced's own materially higher-volume evidence base (§10) — the largest per-cell authority task of any Axis A candidate. |

No candidate cell requires new horizon or phase-topology authority (§14/§15) or new pace/Riegel architecture (§16). Every candidate cell requires its own new peak-volume/calibration/LR numeric authority (§12/§13) — the eligibility-gate class itself would also need widening from a single hardcoded quadruple to something registry-shaped (§37/§38) to support more than one cell at all.

## 20. Axis B definition

Hold `Level=Intermediate, RunsPerWeek=4` fixed; introduce one additional exact target distance under the same `GoalDistance.Custom + TargetDistanceKm` public contract, reusing the HALF_MARATHON structural parent.

## 21. Candidate target distances

Evaluated per §19 of the governing prompt's criteria (race prevalence, external evidence availability, distance from canonical anchors, architectural test value, numeric ambiguity, product usefulness, continuous-vs-banded diagnostic power) — not chosen merely for numeric proximity to 16K.

## 22. 12K analysis

DIST-GEN.0 §8-9 explicitly disclosed that "whether a target of 10.5-12km genuinely needs HM-style structure or would still be better served by an extended-10K structural approach is a narrower open question this phase did not gather distance-specific evidence for," recorded as an explicit unresolved gap. `CanonicalDistanceFamilyResolver` classifies 12.0km into HALF_MARATHON purely by the `≤21.1` band rule — this is **classification, not approved structural-parent authority** (per §35's own caveat, directly echoing DIST-GEN.0's own warning). Choosing 12K next would require re-litigating this unresolved boundary question as a prerequisite, before any target-distance-generalization learning could even begin — a confounded experiment, not a clean second data point. **12K is a poor next candidate**, not because it lacks product interest, but because it would silently conflate two open questions (does the target-distance projection architecture generalize? / is HALF_MARATHON even the right structural parent this close to TEN_K?) that this engagement's own discipline requires resolving one at a time.

## 23. 15K analysis

15K sits comfortably inside the HALF_MARATHON interior band (10 < 15 ≤ 21.1), clear of the 10-12K boundary DIST-GEN.0 flagged as unresolved, and reasonably far from both canonical anchors (4.9km above TEN_K's 10.0, 6.1km below HM's 21.1) — a genuine interior-band test, not an edge case. It is a well-established, commonly-raced distance with real external training-plan evidence available (comparable in kind to the 10-mile sources DIST-GEN.1 used for 16K), giving it the same "real, on-point external evidence" foundation the 16K pilot itself relied on. Critically, 15K is close enough to 16K's own frozen numbers that this phase can meaningfully ask the exact diagnostic question DIST-GEN.0's own §12/§13 left open — are `PeakVolumeBand`/`SelectedPeakReference`/calibration/LR-share fields **continuous** across nearby target distances, **discretely banded**, or **independently curated per exact target**? — using two adjacent real data points (15K and 16K) rather than one. This is the strongest available architectural-learning candidate.

## 24. 18K analysis

18K sits closer to HM's own 21.1km anchor (only 3.1km away) than to TEN_K's. Because HM's own Intermediate 4D peak reference (43.0) and 16K's own frozen peak reference (40.0) are already close, an 18K target risks converging so near to HM's own numbers that the resulting authority-closure exercise would mostly re-derive "use something close to HM," teaching less about generalization than 15K's more clearly-interior position. Still architecturally valid and eligible for a future phase, but a **weaker choice as the very next experiment** than 15K, since it tests upward-convergence-toward-HM rather than interior-band behavior.

## 25. 20K analysis

20K is only 1.1km from HM's own 21.1km ceiling. This phase's own reasoning (§24, intensified) suggests the optimal product behavior for 20K may simply be to reuse HM's own Intermediate 4D numeric authority nearly verbatim (peak reference 43.0, calibration 25.0, etc.) rather than to author a distinct 20K-specific policy — but this is a hypothesis, not a decision frozen here (per the governing prompt's own instruction not to derive new numbers). If true, 20K would be a good candidate for testing a "near-HM band" concept (§27) specifically, but a poor candidate for testing the *interior* generalization question 15K tests, since it may not meaningfully differ from HM at all.

## 26. 10-mile exact analysis

Public authority today is exactly `16.0`, not `16.0934` (the true 10-mile distance). See §49 for the resolved architectural framing.

## 27. Distance-band hypothesis

With only one target-distance-specific numeric data point (16.0km) authored inside the HALF_MARATHON interior to date, there is **not yet sufficient evidence to determine whether a band model is defensible** — a trend (continuous, discretely banded, or independently curated) cannot be established from a single point. This is not a failure of this phase; it is the expected, honest state DIST-GEN.0 itself anticipated when it classified peak volume as "likely DISTANCE_BANDED" without freezing thresholds. **A second exact-target authority closure (Axis B) is a logical and evidentiary prerequisite to ever answering the banding question with real data — not a parallel, independently-viable option at this point in the engagement.**

## 28. Field-by-field bandability (current evidentiary state, not a final determination)

| Field | Classification (current evidence) |
|---|---|
| Core horizon (Min/Preferred/Max weeks) | `FAMILY_INVARIANT` for the interior-band case — confirmed frequency/level-invariant within HM (§14); 16K's own 10/12/14 differs from HM's 10/14/16 only in Preferred/Maximum, both real, evidence-derived, on-point external-evidence decisions (DIST-GEN.1), not yet provably banded vs exact-target-specific with only one interior point. |
| PeakVolumeBand / SelectedPeakReference | `INSUFFICIENT_EVIDENCE` — one interior data point (40.0 for 16K) between TEN_K's 38.0 and HM's 43.0; could be linear, could be curated; §29 below. |
| Weekly growth % / AbsoluteWeeklyIncrementCapKm | `LEVEL_FREQUENCY_INVARIANT` — 16K reused HM Intermediate 4D's own 2.5km cap directly (DIST-GEN.1), consistent with this cap varying by level/frequency (§12), not by target distance, across every anchor examined. |
| Calibration/GoldenFixtureStartingVolumeKm | `INSUFFICIENT_EVIDENCE` — 16K's 23.5 was derived via HM's own ratio-reuse methodology (not interpolated), a `DISTANCE_BAND_CANDIDATE` at best pending a second data point. |
| LR shares (quadruple) | `LEVEL_FREQUENCY_INVARIANT` — 16K reused HM/TEN_K Intermediate 4D's identical 0.30/0.36/0.33/0.40 quadruple exactly (byte-identical across both canonical anchors at this frequency, per DIST-GEN.1's own strongest evidence), strongly suggesting this field is level/frequency-driven, not target-distance-driven, within the interior band. |
| Absolute peak LR | `EXACT_DISTANCE_SPECIFIC` (bounded by the binding `< target distance` invariant, §29) — cannot be a flat family invariant across different target distances by construction. |
| Starting LR cap | `LEVEL_FREQUENCY_INVARIANT` (mirrors 4D's null value; no interior evidence yet distinguishes distance-specific behavior). |
| Taper (weeks/multipliers) | `FAMILY_INVARIANT` — 16K reused HM's 2-week/0.70/0.43 mechanism directly with real external corroboration (DIST-GEN.1 §10), no target-distance dependence found. |
| Target-race-pace semantics | `FAMILY_INVARIANT` / already generic (§16) — driven by the numeric target itself via Riegel, needs no per-target authority at all. |
| Goal feasibility | `FAMILY_INVARIANT` / already generic — same reasoning as target-race-pace. |

## 29. Peak-volume bandability

Three real anchor points exist: TEN_K Intermediate 4D `ResolvedPeakReference=38.0` (`Default`), 16K `ResolvedPeakReference=40.0`, HM Intermediate 4D `ResolvedPeakReference=43.0`. These three points are not equally spaced against their target-distance deltas (10.0→16.0 is +6.0km for +2.0 peak-km; 16.0→21.1 is +5.1km for +3.0 peak-km) — a naive linear read would suggest a *slightly* accelerating relationship near the HM anchor, but three points (two of which are the pre-existing canonical anchors, not independently authored target-distance data) are not enough to distinguish a genuine continuous trend from curated per-target decisions that happen to be monotonic. **No interpolation is performed or endorsed here** — DIST-GEN.0's own prohibition on ungoverned linear training-load interpolation remains fully binding (§13 of that report's own `INTERPOLATION_PROHIBITED_FIELDS` list explicitly includes peak volume). This is exactly the evidentiary gap a second interior target-distance closure (15K) would meaningfully narrow.

## 30. LR bandability

16K's `PreferredAbsolutePeakLongRunKm=15.0` sits below HM Intermediate's `19.0` and is itself bounded below `16.0` (the target) — consistent with the binding, non-numeric invariant from DIST-GEN.0 §24: "peak long run should remain below the target race distance, never at or above it." This invariant, by construction, means absolute peak LR **cannot** be a flat family-wide invariant across different target distances (a fixed 19.0km ceiling would violate the invariant for any target below 19.0km) — it is necessarily at least partially `EXACT_DISTANCE_SPECIFIC`, bounded above by `min(target distance, the family's own frequency/level ceiling)`. Whether the *specific* value chosen below that ceiling follows a band or a per-target curation remains open, same as peak volume. TEN_K's own peak-LR authority was not separately re-derived in this pass (out of this phase's research scope) — recorded as a residual research gap for whichever future phase closes a second target's numeric authority.

## 31. Horizon bandability

The three real triples — TEN_K 8/12/14, HM 10/14/16, 16K 10/12/14 — are **not a monotonic sequence in every component**: 16K's Preferred (12) sits *below* both anchors' own Preferred (12 vs TEN_K's 12 — actually equal — and HM's 14), while its Maximum (14) equals TEN_K's Maximum and sits below HM's (16). This non-monotonicity is exactly the kind of evidence DIST-GEN.1/DIST-GEN.2A's own history warns against over-reading: DIST-GEN.1 originally set 16K's Minimum to 8 by treating TEN_K's own triple as directionally suggestive external evidence, and DIST-GEN.2A had to correct it to 10 once real catalog-structural evidence (the reused HM template's own phase-minimum sum) was checked. **The corrected conclusion (§14) is that horizon authority is not target-distance-banded at all — it is structural-family-driven** (bounded below by the reused parent's own real phase-minimum sum, per DIST-GEN.2A) **with the Preferred/Maximum values as the only genuinely target-distance-informed (evidence-derived, not banded) choices** within that structural floor. This is a meaningfully different and more precise model than "banding," and should be carried forward as this engagement's working horizon-authority theory rather than re-litigated per target.

## 32. Taper bandability

16K's reuse of HM's exact 2-week/0.70/0.43 mechanism, corroborated by real external taper-percentage guidance independently matching both multipliers almost exactly (DIST-GEN.1 §10), is the strongest single piece of evidence in this engagement that taper mechanism is a **family-wide invariant**, not target-distance-specific authority — one real interior data point already agreeing with the canonical anchor's own mechanism is meaningfully different from (and stronger evidence than) the open peak-volume/calibration questions, which have no such external corroboration of a "this doesn't change with distance" hypothesis. Recorded as `FAMILY_INVARIANT` with real supporting evidence, not merely `INSUFFICIENT_EVIDENCE`.

## 33. Generic pace infrastructure

Confirmed in §16: target-race-pace and Riegel-equivalence infrastructure require zero new architecture for any future target distance — both are driven entirely by the numeric `RequestedTargetDistanceKm`/`TargetDistanceKmOverride` value already flowing through the system, with no distance-value branch anywhere in `GoalFeasibilityResolver.cs`. This is unambiguously `GENERIC_REUSABLE_INFRASTRUCTURE` for any axis chosen.

## 34. API reuse

The public `goal_distance: "custom", target_distance_km: X` shape requires **no API/contract version change** for any future approved target — the field is already generic `double?` with no value restriction at the DTO layer (§2). This holds equally for Axis A (a different level/frequency at the same 16.0 target — no new request field needed, `level`/`days_per_week` already exist) and Axis B (a different target value — no new request field needed either).

## 35. Response/display reuse

`goal_distance_formatter.dart` and the additive `RequestedTargetDistanceKm` response/read-DTO fields are already fully target-value-agnostic (§2) — no frontend display work is anticipated for either axis beyond the one exact-match gate discussed in §51.

## 36. Catalog reuse

Confirmed (§15): a single phase-topology master template per family already serves every frequency/level combination; per-cell numeric variation lives entirely in `VolumeSafetyPolicy`, never in a new catalog JSON. This holds for both axes — neither a new 16K frequency/level cell nor a new target distance (staying within the HALF_MARATHON interior) requires a new catalog combination file, confirming DIST-GEN.0's own "no new catalog artifact" architecture continues to hold under either expansion direction.

## 37. Family-boundary caveat

Reaffirmed from §22/§35 of the governing prompt: `CanonicalDistanceFamilyResolver` returning `HalfMarathon` for a given km value is **classification only**, never itself a grant of approved structural-parent authority or public eligibility. This distinction is precisely why 12K (§22) is disqualified as a next candidate despite the resolver happily classifying it — DIST-GEN.0's own unresolved 10-12K boundary caveat sits underneath that classification and has never been separately closed.

## 38. Projected-policy dispatch scalability

Confirmed (§6 of the research pass): the current mechanism is exactly **one** canonical eligibility class (`Dark16KPilotEligibilityPolicy`) containing a single hardcoded eligible quadruple, consulted identically from seven real production call sites (`PlanServices.cs:137`, `GeneratePreviewCommandMapper.cs:108`, `CatalogPreviewGenerator.cs:644` and `:1114`, `TargetDistance16KAwarePeakVolumeBandLoader.cs:46`, `CatalogVolumeAndLongRunPlanner.cs:110`, `CatalogPrescriptionContextBuilder.cs:351`) — not a registry, not a table, and not yet scaled beyond one entry. Recommended narrowest scalable shape for whichever axis is chosen next: replace the four scalar constants with a small, explicit, in-code list of approved `(TargetDistanceKm, ParentFamily, Level, RunsPerWeek, MinCoreWeeks, PreferredCoreWeeks, MaxCoreWeeks)` tuples — a **typed approved-cell registry**, not a formula — consulted by the same seven call sites via an `IsEligible`/`TryResolve`-shaped API that stays call-site-compatible with today's signature. This avoids both the "giant if/else per km" anti-pattern the governing prompt warns against and any premature banding/interpolation logic. Not implemented here.

## 39. Public eligibility scalability

Same conclusion as §38 — the public canonicalization gate in `GeneratePreviewCommandMapper.cs` already delegates its entire decision to `Dark16KPilotEligibilityPolicy`, so widening that one class into a registry (§38) is sufficient; the mapper itself needs no structural change to support a second approved cell or target, only a corresponding new registry entry.

## 40. Registry-vs-formula analysis

- **A. Explicit target-distance policy registry** — a flat list of fully-authored, independently-evidenced cells (what this engagement has built so far, one entry at a time). Strongest fit for the current evidentiary state (§27-32): most fields remain `INSUFFICIENT_EVIDENCE` for banding.
- **B. Distance-band policy registry** — would require at least two real interior data points per band to justify a shared value, which does not yet exist (§27).
- **C. Formula/interpolation engine** — explicitly prohibited by DIST-GEN.0 for the fields that matter most (peak volume, LR shares, absolute peak LR, taper multipliers, readiness thresholds); DIST-GEN.0 permits interpolation only for pace/Riegel, which is already generic and formula-driven today (§16/§33) without needing a new "engine."
- **D. Hybrid** — the actual current and recommended-forward state: registry (A) for authority that needs independent evidence per cell/target, combined with genuinely generic formula-driven infrastructure (C, already built) for the pace/feasibility layer, which was never distance-specific to begin with. **Recommendation: continue the hybrid model, formalize the registry shape (§38) for eligibility/horizon, and revisit banding (B) only after a second interior target distance's numeric authority exists to compare against.**

## 41. Product-value evidence

No product telemetry/usage-statistics source exists in this repository or was referenced by any prior phase — **SOURCE_SILENCE** on real user demand for any specific frequency, level, or additional target distance. This phase does not invent usage estimates; the axis decision below is made on architectural-learning and authority-cost grounds (per the governing prompt's own §64 standard, which ranks "meaningful product value" below "strongest available evidence" and "smallest unsupported-authority surface"), not on assumed demand.

## 42. Implementation cost

| Option | Engineering surfaces touched | Classification |
|---|---|---|
| 16K new frequency (one cell, e.g. Intermediate 5D) | New `VolumeSafetyPolicy` instance + companion peak-band-policy class; widen `Dark16KPilotEligibilityPolicy` (or its successor registry) by one entry; test additions (largely parameterizable, §13 of research); no catalog/frontend/API change. | **SMALL–MEDIUM** |
| 16K new level (e.g. Beginner or Advanced 4D) | Same shape as above, one new numeric-authority set. | **SMALL–MEDIUM** |
| New exact target (e.g. 15K Intermediate 4D) | New `VolumeSafetyPolicy` instance + peak-band-policy class + horizon-policy class (or a reused/parameterized one, per §31's structural-family-driven theory) + eligibility-registry entry; frontend needs one more exact-match branch (§51) or a small generalization of the existing getter; response/read DTOs and formatter need zero change (§35); test files need the `Pilot16KRequest`-style helper widened to accept a target parameter (§53), a small, mechanical refactor. | **SMALL–MEDIUM** (materially similar in engineering shape to a new cell) |
| Distance-band authority closure | Would require gathering and comparing evidence across at least two interior targets before any code change — not yet actionable; if later actionable, likely touches the same surfaces as "new exact target" but for a policy *class* rather than one instance. | **NOT YET ACTIONABLE** |

Engineering cost is roughly comparable across Axis A and Axis B — neither is meaningfully cheaper or more expensive to build in isolation.

## 43. Authority cost

| Option | Training-authority cost | Rationale |
|---|---|---|
| 16K Intermediate 3D/5D/6D | **MEDIUM** | Strong same-level cross-frequency precedent already exists in this exact codebase (HM's own Intermediate 3D/5D/6D numbers, TEN_K's own 3D/5D/6D numbers) to inform new authority via the same ratio/cross-anchor methodology DIST-GEN.1 already used once. |
| 16K Beginner/Advanced (any frequency) | **MEDIUM–HIGH** | Cross-level extrapolation is less precedented in this specific engagement than cross-frequency-same-level extrapolation; Advanced specifically risks the largest numeric jump (§10). |
| 15K Intermediate 4D | **MEDIUM** | Same evidentiary shape DIST-GEN.1 already used successfully for 16K (external race-plan sources + dual-anchor cross-checking) is directly available for a nearby interior target; no structural-parent boundary risk (§23). |
| 12K Intermediate 4D | **HIGH** | Reopens DIST-GEN.0's own explicitly unresolved 10-12K structural-parent boundary question as a prerequisite (§22) — the highest authority risk of any candidate examined. |
| Distance banding | **UNKNOWN / NOT YET ASSESSABLE** | Cannot be assessed with `INSUFFICIENT_EVIDENCE` at the field level (§27-32); assessing it *is* the reason a second exact-target closure is valuable. |

## 44. Risk matrix

| Option | Invented-number risk | Structural risk | Pace/Riegel risk | LR risk | Horizon risk | Public-regression risk | Catalog-explosion risk | Long-term scalability |
|---|---|---|---|---|---|---|---|---|
| 16K frequency expansion (3D/5D/6D) | Low (strong cross-frequency precedent) | None (§14/§15) | None (already generic) | Low-medium (needs new quadruple/cap per cell, real precedent exists) | None (reuses 10/12/14, §14) | Low (additive-only pattern already proven twice) | None (§36) | Good, but each cell is still one-at-a-time authority work absent a registry (§38) |
| 16K level expansion (Beginner/Advanced) | Medium (less precedented extrapolation, esp. Advanced) | None | None | Medium | None | Low | None | Same as above |
| Second target (15K) | Low-medium (same evidentiary shape as 16K's own closure) | Low (clear interior position avoids the 10-12K boundary risk, §23) | None | Medium (peak-LR must independently satisfy `< 15.0`, a new but well-understood constraint, §30) | Low (structural-family-driven theory, §31, suggests 10/12/14 likely reusable, but not yet proven for a second target — must be explicitly re-verified, not assumed) | Low | None | **Highest** — directly tests and either confirms or refutes reusability of every mechanism this engagement built |
| 12K (if chosen) | High (reopens an explicitly unresolved boundary) | High | None | Unknown | Unknown | Low | Possible (may ultimately need its own structural parent, unresolved) | Poor until the boundary question is separately closed |
| Distance banding now | High if forced without evidence (explicitly prohibited) | N/A | N/A | N/A | N/A | N/A | N/A | Not actionable yet (§27) |

## 45-47. 16K frequency-expansion / 16K level-expansion / second-target options

Summarized directly into the final comparison (§55) to avoid duplicating the tables above.

## 48. 10-mile alias/normalization decision

**Not resolved as a final decision — precisely bounded instead**, per the governing prompt's own instruction not to invent a threshold without strong evidence. The three framings considered: (a) same training authority as 16.0 — rejected for now, since asserting "0.09km is training-insignificant" is itself an unevidenced numeric claim, the exact kind of ungoverned judgment this engagement's discipline exists to prevent; (b) fully distinct target authority — technically safe (fail-closed, matches today's actual behavior) but wastes the fact that 10-mile and 16K plans likely share near-identical real-world evidence sources; (c) **display-normalized alias with exact target preserved** — philosophically the most attractive model (store/authorize the exact user-facing distance, e.g. `16.0934`, but explicitly document that its *training* authority is defined by reference to the 16.0 pilot's own frozen numbers) but this itself constitutes new authority (an equivalence claim) that has not been evidenced here. **Decision: `DECISION_REQUIRED`, deferred to whichever future phase considers a second target closest to 16.0 itself** (if one is ever chosen) — recorded as a live, precisely-framed question, not left as a silent gap. Until then, today's exact-target-only behavior (16.0 supported, 16.0934 rejected) remains correct and must not be silently loosened.

## 49. Representation-vs-authority precision

DIST-GEN.0's original distinction — storage may support arbitrary decimals while training authority may operate at coarser exact targets/bands — is **strengthened, not weakened**, by the successful 16K implementation: `RequestedTargetDistanceKm` already stores and displays the exact user-facing value with full precision (proven in DIST-GEN.3's own persistence/response proofs), while the *training* decision (which numeric policy applies) is gated by a separate, coarser, explicitly-authorized quadruple. This confirms the model generalizes cleanly: any future target distance can be represented exactly without implying its training authority is automatically approved, which is exactly the fail-closed-by-default posture §55 of the governing prompt requires.

## 50. (folded into §48/49 — representation/authority precision fully addressed above)

## 51. Frontend scalability

`race_details_page.dart`'s `_isExactPilot16KMatch` is a single hardcoded-constant getter (§11 of research), not a set. A second approved target would require either (a) a second near-duplicate getter (small, mechanical, but sets a bad precedent if repeated many times), or (b) generalizing to a small approved-target list mirroring the backend registry recommendation (§38) — recommended for whichever future phase implements a second target, not implemented here. The existing ±0.2 preset-snap tolerance remains correctly untouched and must stay reserved for the four canonical presets only, regardless of which axis is chosen.

## 52. Persistence scalability

Confirmed (§9 of research): zero schema change anticipated for either axis — `CanonicalDistanceFamily`/`RequestedTargetDistanceKm` already generically support any future target distance or cell, including two simultaneously-supported target distances at once (nothing in the schema assumes exactly one non-null target value is possible product-wide).

## 53. Test-architecture scalability

`Dark16KTargetDistanceProjectionTests.cs` is already `[Theory]`/`[InlineData]`-parameterized over target-km values, directly extensible to a second target with new data rows, not a rewrite. `DistGen3PublicActivationTests.cs`'s `Pilot16KRequest` helper hardcodes `16.0` inline and would need its signature widened to accept a target parameter before reuse for a second target or cell — a small, mechanical, low-risk refactor, not a rebuild. Recommended for whichever future phase is chosen: widen this one helper's signature as its first mechanical step, before authoring any new-cell/new-target tests, so the ~1000-line bespoke-file-per-target failure mode the governing prompt warns against (§61) is avoided from the start.

## 54. Hardcoded-16K-assumption audit

| Assumption found | Classification |
|---|---|
| `Dark16KPilotEligibilityPolicy`'s four scalar constants (single quadruple, not a list) | `SCALABILITY_DEBT` — intentional and correct for a single-cell pilot, but must be generalized (§38) before a second cell/target is added, not copied. |
| `race_details_page.dart`'s single hardcoded `_pilot16KTargetKm` constant/getter | `SCALABILITY_DEBT` — same reasoning, frontend-side. |
| `DistGen3PublicActivationTests.cs`'s `Pilot16KRequest` helper hardcoding `16.0` inline | `SCALABILITY_DEBT` — test-only, low risk, mechanical fix (§53). |
| `CanonicalDistanceFamilyResolver`'s band thresholds | `NO_ISSUE` — genuinely target-value-agnostic, no 16-specific literal exists here at all (§8 of research). |
| `GeneratePreviewCommandMapper`'s canonicalization gate structure | `NO_ISSUE` — delegates its entire eligibility decision to `Dark16KPilotEligibilityPolicy`, contains no independent 16.0 literal of its own (§7 of research). |
| `goal_distance_formatter.dart` | `NO_ISSUE` — fully generic (§11 of research). |
| Public request/response DTO fields | `NO_ISSUE` — generic `double?`, no value restriction at this layer. |
| Persistence schema | `NO_ISSUE` — generic, already anticipates other exact km values in its own doc comments. |

No `BUG`-classified finding exists — every "Custom means 16K" narrowing found is an `INTENTIONAL_PILOT_GATE` (the eligibility policy itself) or `SCALABILITY_DEBT` (call sites/tests that will need mechanical widening, not correction, before a second cell/target). This is a healthy pre-expansion state.

## 55. Final option comparison

| | Architectural learning | Training-authority cost | Implementation cost | Product-value evidence | Risk | Reuse strength | Long-term scalability |
|---|---|---|---|---|---|---|---|
| **A: 16K frequency expansion** | Low-moderate (frequency orthogonality already strongly suggested by §14/§15/§17-18's structural findings, not newly tested) | Medium | Small-medium | SOURCE_SILENCE | Low-medium | High (reuses horizon/topology/KEY2/pace wholesale) | Good, needs registry eventually |
| **B: 16K level expansion** | Low-moderate (similar reasoning, plus tests cross-level extrapolation) | Medium-high | Small-medium | SOURCE_SILENCE | Medium | High | Good, needs registry eventually |
| **C: Second target distance (15K)** | **Highest** — the only option that tests whether the target-distance projection architecture itself (not just one instance of it) generalizes | Medium | Small-medium | SOURCE_SILENCE | Low-medium | High (every mechanism except the numeric authority itself is already proven reusable) | **Highest** — directly informs the future banding question with real second-point evidence |
| **D: Distance-band closure now** | N/A — not actionable | Unknown/not assessable | N/A | SOURCE_SILENCE | High if forced | N/A | Premature |

## 56. Next-axis decision

**Axis B (target-distance expansion), specifically Option C above: a second exact target distance, 15K, held at Intermediate/4D.**

Reasoning, applying the governing prompt's own §64 standard in order: (1) *strongest available evidence* — 15K has the same kind of real, on-point external race-plan evidence DIST-GEN.1 already successfully used for 16K, and avoids DIST-GEN.0's own flagged 10-12K uncertainty; (2) *smallest unsupported-authority surface* — comparable in size to any Axis A cell (one new numeric-authority set), with the added benefit that horizon authority is very likely already correctly theorized (§31) rather than needing to be independently re-derived from scratch; (3) *highest architectural learning* — this is the decisive factor: Axis A/frequency-or-level expansion primarily exercises already-well-understood HM cross-frequency/cross-level patterns and teaches little new about the *target-distance* dimension specifically, while a second target distance is the only available experiment that tests whether the entire DIST-GEN.0-through-3 architecture (eligibility gate shape, horizon-policy shape, canonicalization mapper, response/read DTOs, frontend formatter, test parameterization) generalizes beyond the one value it has ever been built and proven against; (4) *product value* — SOURCE_SILENCE for both axes, a wash; (5) *maximum reuse of proven infrastructure* — both axes reuse comparably well, but Axis B's success or failure directly de-risks every future expansion (including a later Axis A), while Axis A's success does not similarly inform Axis B; (6) *zero-delta safety* — both axes are equally additive-safe by construction; (7) *no invented numeric rules* — 15K requires the same real-evidence-gathering discipline DIST-GEN.1 already modeled successfully, not interpolation or banding.

12K, 18K, and 20K were each considered and are recorded (§22/§24/§25) as weaker choices for the *next* experiment specifically — 12K for reopening an unresolved boundary, 18K/20K for converging too close to HM to teach as much about the interior-band question. They remain valid candidates for later phases.

## 57. Exact next phase handoff

**DIST-GEN.5 — 15K INTERMEDIATE 4D TARGET-DISTANCE AUTHORITY CLOSURE.** Scope: gather real, on-point external 15K training-plan evidence (mirroring DIST-GEN.1's own methodology); freeze `PeakVolumeBand`/`SelectedPeakReference`/calibration/LR-quadruple/`PreferredAbsolutePeakLongRunKm`/taper authority for the `(15.0, HalfMarathon, Intermediate, 4)` cell; explicitly re-verify (not assume) whether `TargetDistance16KHorizonPolicy`'s own 10/12/14 pattern transfers by re-deriving Preferred/Maximum from 15K-specific evidence, per §31's theory; explicitly compare the resulting numbers against 16K's own frozen values to begin answering the banding question (§27-29) with real second-point data — without freezing any band boundary in that same phase. This phase must not begin any implementation itself (per §66 of this phase's own governing prompt).

## 58. Existing-product zero-delta

Confirmed no behavior change to: 5K, TEN_K, HALF_MARATHON, the 16K public pilot (its own 10-14W horizon and numeric policy remain completely untouched by this audit), `GoalDistance.Custom`'s fail-closed semantics for every currently-unsupported target, Habit Custom, Experienced exclusion, the HM matrix, or any test. This entire phase is analysis only — `git status --porcelain -- backend plan-catalog mobile` is expected empty and confirmed empty.

## 59. Remaining blockers

None for closing this audit. The one live, explicitly-deferred question is §48's 10-mile-exact alias/normalization decision, correctly left `DECISION_REQUIRED` rather than forced — this does not block DIST-GEN.5, since DIST-GEN.5's own scope (15K) does not require resolving it.

---

## Final classification

**POST_16K_PILOT_GENERALIZATION_NEXT_AXIS_CLOSED**

Next axis selected: **Axis B — second exact target distance.** Next phase: **DIST-GEN.5 — 15K Intermediate 4D Target-Distance Authority Closure.** DIST-GEN.5 itself is not begun by this phase.
