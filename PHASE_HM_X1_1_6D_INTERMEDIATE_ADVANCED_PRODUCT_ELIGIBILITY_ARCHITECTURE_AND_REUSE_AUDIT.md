# PHASE HM-X1.1 — HALF-MARATHON 6D INTERMEDIATE/ADVANCED PRODUCT ELIGIBILITY, ARCHITECTURE, AND REUSE AUDIT

**Type:** AUDIT / GOVERNANCE (no production implementation, no catalog authoring, no public activation, no numeric authority freeze)

---

## 1. Current HM V1 baseline

Per `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md` and `PHASE_HM_19_...md` (binding current-state authority, not reopened): public matrix is Beginner 3D/4D (5D `PRODUCT_INELIGIBLE`), Intermediate 3D/4D/5D, Advanced 3D/4D/5D, Standard Core 10-16 weeks, all public. HM-X1 (6D expansion) was already flagged there as "likely product-eligible levels: Intermediate and Advanced (do not assume Beginner)," with 6D correctly noted as never having been in V1 scope — its absence is not a V1 gap. This audit answers exactly the question that section deferred.

## 2. EXP.0 Experienced exclusion

`PHASE_EXP_0_...md` closed with `PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED`. Per that phase's own instruction and this phase's own §2, Experienced is not audited, not reinterpreted, and not reopened anywhere in this report. HM-X1.1 targets only Intermediate × 6D and Advanced × 6D.

## 3. 6D target matrix

| Cell | Disposition |
|---|---|
| HALF_MARATHON × INTERMEDIATE × 6D (Standard Core, 10-16W) | `PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE` (see §29) |
| HALF_MARATHON × ADVANCED × 6D (Standard Core, 10-16W) | `PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE` (see §30) |

Beginner × 6D is explicitly not this track (§34) and is not audited beyond the context note in §3 of the governing prompt: HM's own historical planning material (`HM_21K_Claude_Handoff_and_Build_Plan.md`, WAVE HM-F, line 966-970) only ever lists "Beginner 2D, Intermediate 2D, Intermediate 6D, Advanced 6D" as the deferred-frequency closure set — Beginner 6D was never part of any HM plan, mirroring the same reasoning already underpinning Beginner×5D's `PRODUCT_INELIGIBLE` status. This is recorded as context only; Beginner eligibility is not reopened.

## 4. Real RUN_LAYOUT_6D

Read directly (not assumed): `plan-catalog/catalog/layouts/run-layout-6d.v1.json` (key `RUN_LAYOUT_6D`, `runsPerWeek: 6`, status `VALIDATED`, distance-agnostic — no distance field exists in the artifact) versus `run-layout-5d.v1.json` (`RUN_LAYOUT_5D`, `runsPerWeek: 5`).

| Ordinal | Role | CanonicalToken | LaneSemantics (derived downstream, not declared in the layout file) | Current Consumers |
|---|---|---|---|---|
| 1 | KEY_SESSION | `KEY_SESSION` | Structural ordinal 0 → binds to `laneOrdinal 0` ("primary"/KEY1) | `CatalogRunLayoutSlots.cs`, `CatalogWorkoutBinder.cs:139-155` |
| 2 | EASY_SUPPORT | `EASY_SUPPORT` | Structural ordinal 0 among EASY slots | `CatalogWorkoutBinder.cs:257-313`, `V1FourDaySessionVolumeAllocationPolicy.cs` |
| 3 | KEY_SESSION | `KEY_SESSION` | Structural ordinal 1 → binds to `laneOrdinal 1` ("secondary"/KEY2) | Same as row 1 |
| 4 | EASY_SUPPORT | `EASY_SUPPORT` | ordinal 1 | Same as row 2 |
| 5 | EASY_SUPPORT | `EASY_SUPPORT` | ordinal 2 — the one slot 6D adds over 5D | Same as row 2; enabled by the `EASY = total - KEY - LONG` generalization (`V1FourDaySessionVolumeAllocationPolicy.cs:73-77`) |
| 6 | LONG_RUN | `LONG_RUN` | exactly 1 | `CatalogVolumeAndLongRunPlanner.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs` |

**Answers**: 2 `KEY_SESSION` roles (same as 5D); 3 `EASY_SUPPORT` roles (5D has 2 — this is the entire delta); 1 `LONG_RUN` role. Roles are not distinguished by lane in the layout file itself — lane identity is derived downstream by the binder ("structural ordinal N binds to laneOrdinal N," layout-driven, not frequency-driven). The schema trivially supports repeated same-role slots (no uniqueness constraint). No role is optional — `CatalogRunLayoutSlots.cs` hard-rejects any `REST`/`OPTIONAL`/`RECOVERY` role; "no optional/recovery slot classification exists in this contract." The layout encodes only abstract role names, never hardness — hardness comes entirely from the workout-progression's per-lane stage assignment.

**6D vs 5D structural delta — verified, not assumed**: exactly one additional `EASY_SUPPORT` slot. This does equal "5D + one EASY," but per the phase's own instruction, this is recorded as a **discovered fact** from the real artifact (the schema permits any role combination; the product simply chose to add EASY_SUPPORT capacity rather than a third KEY_SESSION or a second LONG_RUN).

## 5. 10K Intermediate 6D precedent

**Confirmed real and PUBLIC today** (not dark) via `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency` (lines 339-374), which includes `(TenK, Intermediate, 6)` with an explicit citing comment: *"Phase 10K-FREQ.6D.27 public activation, implementing the already-approved FREQ.6D.23/6D.25/6D.26 authority."* (Note: some older doc comments on the `SixDayCandidateKey` constant itself, from before activation, still say "dark-only" — a stale-comment drift, flagged in §33, that does not affect actual runtime behavior, which the switch/dispatch confirms is public.)

- Pointer chain: `ten-k-6d-intermediate.v1.json` → `TEN_K_MASTER` v8 → `RUN_LAYOUT_6D` v1, `INTERMEDIATE_MODIFIER` v7, `TEN_K_WORKOUT_PROGRESSION_V1` v6 — **the same progression key used by 4D/5D**, no dedicated `_6D` progression variant exists.
- Both KEY lanes carry a genuine quality/hard stimulus in every phase except Foundation-lane-0: Foundation (lane0 `AEROBIC_STRENGTH_CONTROLLED_INTRO` moderate / lane1 `THRESHOLD_TEMPO` v5 hard), Build (lane0 `THRESHOLD_TEMPO` v4 / lane1 `FARTLEK` v5), RaceSpecific (lane0 `GOAL_PACE_TEN_K` v2 / lane1 `THRESHOLD_TEMPO` v4), Taper (lane0 `GOAL_PACE_TEN_K` v3 / lane1 `FARTLEK` v5).
- Volume policy: `VolumeSafetyPolicy.SixDayIntermediate` (line 539-552) — **byte-identical to `FiveDayIntermediate` on every field** (0.07/0.08 increase ratios, 2.5 absolute cap, 44.5km peak, 0.53 taper multiplier, 0.28/0.36 LR shares), with its own doc comment stating "FREQ.6D.25 approved reusing 5D's exact... figures for 6D (the same real, undifferentiated evidence source spans both 5 and 6 days)." A dedicated `V1SixDayIntermediateMissingReadinessStartingVolumePolicy.cs` exists.
- Session allocation: routes through the same generic `V1FourDaySessionVolumeAllocationPolicy` used by 4D/5D — **no dedicated 6D allocator exists**.
- Confirmation/persistence proof: `Freq6D27IntermediateSixDayPublicActivationTests.cs`'s `PublicFullLifecycle_ConfirmedPlan_ReachesOrganicCoreWithDualKeyThroughRealPostgres` confirms a real plan against real Postgres, asserting `firstCoreWeek.Count == 6`, 2 `KEY_SESSION` (lanes 0/1), 3 `EASY_SUPPORT` (3 distinct ordinals), and proves adaptive repair preserves lane identity after a `NotToday` on the secondary KEY.

## 6. 10K Advanced 6D precedent

Also confirmed real and PUBLIC via the same switch (`(TenK, Advanced, 3/4/5/6)`, citing *"Phase 10K-GEN.10 public activation, implementing the already-approved GEN.7/GEN.8 authority."*). Same `RUN_LAYOUT_6D`, same `TEN_K_WORKOUT_PROGRESSION_V1` progression (v7 for Advanced), identical per-phase KEY1/KEY2 workout-family assignment as Intermediate's table above (only the bound `WORKOUT_PRESCRIPTION_PROFILE` differs by level). Volume policy `VolumeSafetyPolicy.Advanced6D` (line 648-661): 51.0km peak reference, methodology mirrors `Advanced5D`'s own calibration approach (peak 50.0→51.0), same 0.53 taper multiplier, same 0.28/0.36 LR shares. `Gen10AdvancedCombinedPublicActivationTests.cs` mirrors the Intermediate confirmation/persistence proof for Advanced 3D/4D/5D/6D combined.

## 7. Generic mechanisms proven by 10K

| Mechanism | Classification | Evidence |
|---|---|---|
| RunLayout representation | GENERIC_REUSABLE | Distance-agnostic artifact, no distance field; `RUN_LAYOUT_6D` already usable by a new HM combination file with zero new layout authoring |
| Multi-KEY/lane model | GENERIC_REUSABLE | `CatalogWorkoutBinder`'s "structural ordinal N binds to laneOrdinal N" rule is layout-driven, identical code path for 4D (1 lane), 5D/6D (2 lanes) |
| Workout-progression lane binding | GENERIC_REUSABLE | 10K needed **zero** new progression file for 6D — the same 2-lane 5D progression directly serves 6D |
| Session-volume allocator | GENERIC_REUSABLE | `V1FourDaySessionVolumeAllocationPolicy` generalized (Phase 10K-FREQ.6D.7/6D.26) to any KEY/EASY count; no dedicated 6D allocator was ever built |
| Hardness classification | GENERIC_REUSABLE | Purely a function of which workout-family key is bound to a lane/stage — not frequency-coded anywhere |
| Calendar spacing | GENERIC_REUSABLE | `CatalogWeekSkeletonCalendarMaterializer`'s day-distance spacing constants (`MinimumKeySessionToLongRunSeparationDays=2`, same for KEY-to-KEY) are frequency-independent; the `DaysPerWeek` allow-list itself (`is not (2,3,4,5,6)`) was widened for 6D at Phase 10K-FREQ.6D.26 and is **distance-agnostic**, so HM inherits this widening automatically, at zero cost |
| Adaptation | GENERIC_REUSABLE | `PlaceholderAdaptationEngine` has zero branching on frequency/session-count |
| Preview materialization | GENERIC_REUSABLE | `CatalogStageToWeekMaterializer`/`GeneratedCatalogPlanSkeletonValidator` validate `SessionSlots.Count == skeleton.DaysPerWeek` generically, no upper literal bound below 7 |
| Confirmation/persistence | GENERIC_REUSABLE | `CatalogPlanConfirmationService` persists via a variable-length loop over `weekPayload.Sessions`, no fixed-count assumption |
| Active reads/mutations | GENERIC_REUSABLE | Same session-count-agnostic pattern; 10K's own 6D repair/reads are real-Postgres-tested |
| Public identity routing | FREQUENCY-SPECIFIC_GENERIC | The routing *mechanism* (flat additive tuple allow-list + candidate-key switch) is fully generic; the specific HM allow-list entries are closed (DISTANCE-SPECIFIC_10K in the sense that only 10K's own list currently includes 6, not because the mechanism can't extend to HM) |

No mechanism the 10K 6D activation touched required inventing new architecture — every one of the above was a narrow, additive widening of an already-generic class. The two genuinely new artifacts per 10K 6D activation were (a) a new named `VolumeSafetyPolicy` record (largely copied from the 5D sibling) and (b) new `PeakVolumeBand` catalog rows — both **numeric/calibration decisions**, not mechanism work.

## 8. HM Intermediate 5D precedent

Verified directly against `half-marathon-5d-intermediate.v1.json` → `HALF_MARATHON_MASTER` v2 → `RUN_LAYOUT_5D` v1, `INTERMEDIATE_MODIFIER` v9, `HALF_MARATHON_WORKOUT_PROGRESSION` v2. KEY2 (lane 1) per phase: Foundation `FOUNDATION_HM_SECONDARY_EASY`→`EASY_STANDARD` v4; Build `BUILD_HM_SECONDARY_FARTLEK`→`FARTLEK` v6 (fallback `EASY_STANDARD`); RaceSpecific `RACE_SPECIFIC_HM_SECONDARY_FARTLEK`→`FARTLEK` v6 (fallback `EASY_STANDARD`); Taper `TAPER_HM_SECONDARY_EASY`→`EASY_STANDARD` v4. `FARTLEK` v6 has main-set `intensityDescriptor: "SURGE_AND_FLOAT"` — moderate, not the true-hard `"THRESHOLD"` descriptor — **confirming the "frozen light-quality model" claim exactly**: Intermediate's second KEY is a genuinely lighter stimulus than KEY1, never a second true-hard session. Foundation/Taper are easy-equivalent (`EASY_STANDARD`) for both phases. `VolumeSafetyPolicy.HalfMarathonIntermediate5D` fields: 2.5km absolute weekly cap, 29.5km calibration start, 51.0km resolved peak, 0.70/0.43 taper multipliers, 0.28/0.36 LR shares, 19.0km absolute peak LR, 12.0km starting LR cap.

## 9. HM Advanced 5D precedent

Verified against `half-marathon-5d-advanced.v1.json` → `HALF_MARATHON_MASTER` v3 → `RUN_LAYOUT_5D` v1, `ADVANCED_MODIFIER` v3, `HALF_MARATHON_WORKOUT_PROGRESSION` v3. KEY2 per phase: Foundation/Taper identical `EASY_STANDARD` content to Intermediate's (v3's own `_comment` confirms this is deliberately identical between levels); Build `BUILD_HM_SECONDARY_THRESHOLD_ADVANCED`→`THRESHOLD_TEMPO` v4 (fallback `EASY_STANDARD`); RaceSpecific `RACE_SPECIFIC_HM_SECONDARY_THRESHOLD_ADVANCED`→`THRESHOLD_TEMPO` v4 (fallback `EASY_STANDARD`). `THRESHOLD_TEMPO` v4 carries the true-hard `"THRESHOLD"` intensity descriptor — **confirming the "genuine second true-hard stimulus" claim exactly**, matching `VolumeSafetyPolicy.cs`'s own doc comment (lines 930-950): *"5D is the one HM frequency where Advanced's native 2-hard-per-week capacity is genuinely EXERCISED... KEY2 NEVER binds HM_PACE anywhere (HM.15 §7/§8, deliberate)."* `VolumeSafetyPolicy.HalfMarathonAdvanced5D`: 2.5km absolute weekly cap, 36.0km calibration start, 62.0km resolved peak, 0.70/0.43 taper multipliers, 0.28/0.36 LR shares, 19.0km absolute peak LR, no starting LR cap field (null).

**No numeric "maximum true-hard stimuli per week" field or constant exists anywhere in the codebase** — confirmed by explicit disclaimers in `VolumeSafetyPolicy.cs` itself (Advanced 3D/4D policy comments: "carries no field for a nonexistent KEY2"; Advanced 5D's comment: "this record carries no KEY2-specific field: KEY2's workout-family selection is entirely a workout-progression/binding concern... the generic multi-KEY session-volume allocator was never hardness-aware, only role-aware") and by a full-codebase grep for `TrueHardStimuli|MaxTrueHard|HardStimuliPerWeek|maxHardSessions|MaxQualityPerWeek` returning zero hits. **Hard-stimulus capacity is a purely structural property — the number of `KEY_SESSION` slots in the bound `RUN_LAYOUT` — not a separately configurable numeric cap.**

## 10. 6D vs 5D structural delta

Confirmed (§4): exactly +1 `EASY_SUPPORT` slot, 0 change to `KEY_SESSION` count (stays at 2) or `LONG_RUN` count (stays at 1). Combined with §9's finding that hard-stimulus capacity is structural (KEY-slot-count-determined, not a separate numeric field), this has a direct and important consequence: **6D cannot organically introduce a third true-hard stimulus without a new RunLayout artifact** (a third `KEY_SESSION` slot), which is explicitly out of scope for this phase and not indicated by any evidence gathered. The real, current architecture caps both Intermediate and Advanced 6D at exactly the same 2-KEY-lane structure already proven at 5D.

## 11. Intermediate 6D structural audit

Per the governing prompt's candidate structural possibilities (A-D): the evidence supports **Option A** — same number of true-hard sessions as Intermediate 5D (KEY1 = the existing frozen primary lane content, unchanged by frequency; KEY2 = the existing frozen light-quality FARTLEK model), with the one added `EASY_SUPPORT` slot providing additional easy/support volume. This requires no new quality-capable lane (Option B is moot — the lane already exists and is already capped, structurally, at 2), no additional true-hard session (Option C is not indicated by any evidence and is not adopted), and the existing modifier/layout **can** safely represent Intermediate 6D (Option D is rejected) — the same reasoning and generic mechanisms that let 10K's own `SixDayIntermediate` reuse `FiveDayIntermediate`'s numeric authority verbatim apply structurally to HM. Maximum hard-stimulus capacity (2 KEY slots) remains distinct from — and is not automatically escalated by — adding a 3rd EASY role.

## 12. Advanced 6D structural audit

Advanced HM 5D already exercises 2 true-hard stimuli/week (KEY1 + THRESHOLD_TEMPO KEY2 in Build/RaceSpecific). Per §10, 6D's RunLayout keeps exactly 2 `KEY_SESSION` slots — it does **not** structurally invite or require a third KEY role, and no new lane concept is needed or indicated. The correct reading, consistent with EXP.0's explicit anti-escalation finding ("training tiers should not be created through unsupported escalation") and this phase's own explicit prohibition against inferring "Advanced 6D → 3 true-hard sessions," is that Advanced 6D keeps its existing two true-hard sessions and adds the one `EASY_SUPPORT` slot as additional aerobic volume — **not** a third hard session. Current Advanced global hard-cap authority (2 KEY slots, per RunLayout) remains binding; no direct existing authority indicates otherwise.

## 13. True-hard-stimulus compatibility

| Level | 6D KEY Slots | MaxHardAllowed | PotentialActualHardCount | CapabilityConflict? |
|---|---|---|---|---|
| Intermediate | 2 | 2 (structural, = KEY-slot count; no separate numeric cap exists) | 1 true-hard (KEY1) + 1 moderate (KEY2, FARTLEK) — identical pattern to 5D | No |
| Advanced | 2 | 2 (structural) | 2 true-hard (KEY1 + KEY2 THRESHOLD_TEMPO) — identical pattern to 5D | No |

No cap is frozen in this phase — this table only confirms that 6D's structural ceiling (2 KEY slots) is not exceeded by either level's existing 5D pattern carried forward unchanged. No higher cap is required to make either cell coherent, so no authority blocker exists on this axis.

## 14. HM progression compatibility

`HALF_MARATHON_WORKOUT_PROGRESSION` v2 (Intermediate)/v3 (Advanced) each already define exactly the stage names cited in the governing prompt for the primary (lane 0) sequence — `FOUNDATION_AEROBIC_BASE`, `BUILD_FASTER_THAN_HM_INTRO`, `BUILD_THRESHOLD_DEVELOPMENT`, `RACE_SPECIFIC_HM_INTRODUCTION`, `RACE_SPECIFIC_HM_DEVELOPMENT` (+fallback), `RACE_SPECIFIC_HM_REHEARSAL` (+fallback), `TAPER_HM_ACTIVATION` (+fallback) — verified as real, live catalog content, not inferred from the prompt. The progression remains one logical primary lane (lane 0) plus one secondary lane (lane 1, level-differentiated per §8-9). Lane binding is layout-driven, not frequency-driven (§4, §7): the same v2/v3 progression files that serve 5D would serve 6D **unmodified**, exactly mirroring 10K's own finding that no dedicated `_6D` progression variant was ever needed.

```
HM_6D_PRIMARY_PROGRESSION_REUSABLE
```

No new progression lane, no new stage content, and no new secondary-quality policy authoring is required to represent 6D's structure — only a new combination artifact pointing at the existing progression files plus (per §31) new numeric authority.

## 15. Lane architecture

The C# progression contract (`CatalogWorkoutProgressionLane`/`CatalogPhaseWorkoutProgression`) is schema-generic over an arbitrary number of lanes (`IReadOnlyList<CatalogWorkoutProgressionLane>?`, no compile-time maximum), and `ProgressionStageAllocator` iterates `EffectiveLanes` generically with no hardcoded 1-or-2 assumption. In practice, **no live catalog file anywhere (HM or 10K) has ever used a lane ordinal above 1** — the real, current maximum lane count proven by production code is exactly 2, and this ceiling is enforced not by a lane-count constant but by `CatalogWorkoutBinder`'s "RunLayout is the sole structural-cardinality authority" rule (a progression cannot declare more lanes than the layout has `KEY_SESSION` slots — both `RUN_LAYOUT_5D` and `RUN_LAYOUT_6D` have exactly 2). Fallback is lane-aware by construction (`AllocatePhase` is invoked once per lane with only that lane's own stage list; a lane-1 stage's fallback can only resolve within lane 1). `CatalogFinalPrescribedPlanValidator`'s taper-completeness check is explicitly lane-partitioned (`LaneOrdinal == 0` vs `LaneOrdinal == 1`). Public payload/persistence preserve lane identity through mutation (10K's own 6D repair test proves a `NotToday`'d secondary KEY re-materializes at the same `LaneOrdinal == 1`, never merging with lane 0).

**Maximum capability proven by current production code: 2 lanes.** 6D does not need a 3rd lane (§10-§12), so this ceiling is not a blocker for this phase's target matrix.

## 16. Workout family capability

| Family | CAN_BE_PRIMARY | CAN_BE_SECONDARY | CAN_BE_EASY_EQUIVALENT | Classification |
|---|---|---|---|---|
| `THRESHOLD_TEMPO` v4 | Yes | — | — | Eligible, true-hard |
| `THRESHOLD_TEMPO` v5 | Yes in principle | — | — | Modifier-eligible (Advanced) but not yet wired into any HM progression stage — a pre-existing dose-wiring gap disclosed at HM.15 §4, unrelated to 6D, not resolved here |
| `FARTLEK` v4/v6 | No (never used as HM's sole/primary content) | Yes (Intermediate 5D's KEY2 pattern) | — | Eligible, moderate |
| `HM_PACE` v1 | Yes (race-specific true-hard) | — | — | Eligible, reserved for KEY1/lane 0 only per HM.15 §7-8's explicit decision (never used in KEY2/lane 1 for either level) |
| `EASY_STANDARD` v4/v6 | — | — | Yes | Eligible |
| `LONG_RUN_STANDARD` v4 | — | — | — | Not a KEY-slot candidate; bound only to the `LONG_RUN` role |
| `GOAL_PACE_TEN_K` v2/v3 | — | — | — | `NOT_ELIGIBLE` for HM — 10K-distance-specific, never referenced by any HM progression stage despite appearing in the shared Advanced-modifier `eligibleWorkouts` list |
| `AEROBIC_STRENGTH_CONTROLLED_INTRO` v3 | — | — | — | `NOT_ELIGIBLE` for HM's Build/RaceSpecific KEY slots — its own `eligiblePhases` excludes Build/RaceSpecific entirely |
| Strides/hills | — | — | — | `NOT_ELIGIBLE` — no such workout family exists anywhere in the live catalog (confirmed via file search, zero matches) |

Sufficient existing families exist to represent both plausible HM 6D structures (Intermediate: KEY1 primary + KEY2 FARTLEK moderate; Advanced: KEY1 primary + KEY2 THRESHOLD_TEMPO true-hard) without inventing any new workout family, exactly mirroring the existing 5D pattern.

## 17. Dose representability

Current Intermediate/Advanced modifiers (`intermediate-modifier.v9.json`, `advanced-modifier.v3.json`) contain no session-count-specific dose field — dose is purely a function of allocated distance per role, computed generically (§18). The generic session-volume allocator's minimums (`MinimumKeySessionDistanceKm=3km`, `MinimumEasySupportDistanceKm=1.5km`) and fail-closed feasibility check (`CatalogSessionPrescriptionInfeasibleException`) apply uniformly regardless of role count — a plausible HM 6D volume range (extrapolating from 5D's own 44-70km+ peak range) would not push these minimums into degeneracy any more than 10K's own live 6D does today at similar or lower absolute volumes. No specific 6D dose number is chosen here; this is a capability-only finding. HM-X1.2 must still verify feasibility against HM's own eventually-frozen 6D peak-volume band once selected (§31).

## 18. Session-volume allocator

`CatalogSessionPrescriptionPlanner.Build` special-cases only 3-day (`isThreeDay`); every other frequency, including 6D, routes through `V1FourDaySessionVolumeAllocationPolicy.Allocate`, which was explicitly generalized (Phase 10K-FREQ.6D.7/6D.26) from a hardcoded `keySessionCount==1`/`easySupportCount==2` shape to `EASY = total - KEY - LONG`, with `DistanceFor`'s ordinal-indexing also generalized beyond a binary ternary specifically to support 6D's 3-EASY shape. No `daysPerWeek <= 5` or fixed-count assumption remains in this path, and 10K's own live 6D (both levels) routes through this exact code today.

```
HM_6D_SESSION_ALLOCATION_CAPABILITY_READY
```

## 19. Long-run mechanism capability

`VolumeSafetyPolicy`'s long-run fields (`LongRunPreferredMinimumShare/MaximumShare/SelectionShare/HardCapShare`, `PreferredAbsolutePeakLongRunKm`, `StartingAbsoluteLongRunCapKm`, `ResolvedPeakReferenceIsSelectedCeiling`) are plain data fields on a named record, consumed generically by `CatalogVolumeAndLongRunPlanner` — none of these mechanisms special-case 3/4/5 in code; 10K's own `SixDayIntermediate`/`Advanced6D` reuse the identical mechanism with zero code changes, only new field values. Existing mechanics can represent a 6D-specific numeric policy (peak, shares, caps) **without any code change** — only a new named policy record (data), exactly the same pattern used for every prior HM frequency addition. Numeric values themselves are explicitly deferred to HM-X1.2.

## 20. Volume-policy dispatcher capability

`VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek` (3,4,5 only) and `ForHalfMarathonAdvancedDaysPerWeek` (3,4,5 only) are both closed switches that throw for any unmatched value — by design (the same pattern HM.11 used to add 5D, and the same pattern 10K used to add 6D to its own `ForIntermediateDaysPerWeek`/`ForAdvancedDaysPerWeek`, which already contain live `6 =>` arms). Adding `case 6:` to each HM dispatcher, paired with matching branches in `CatalogVolumeAndLongRunPlanner.Build`'s HM `DaysPerWeek==3||4||5` checks and `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`'s identical HM checks, is a **narrow, additive change** — the exact same recipe used 4 times already (3D→4D→5D→Advanced-3/4/5D). No RunLayout assumption, validation assumption, or catalog-identity assumption blocks this widening; it is correctly classified `INTENTIONAL_PRODUCT_GATE` (HM simply hasn't run a 6D numeric-authority phase yet), not an architecture blocker.

## 21. Historical 6D numeric evidence inventory

| Field | Candidate | Source | Level | Status | AuthorityStrength |
|---|---|---|---|---|---|
| HM peak-volume band, 6D | None — explicit silence | `HM_21K_Claude_Handoff_and_Build_Plan.md:180`: *"No equivalent HM 2D or 6D peak-volume authority is considered frozen yet."* | Intermediate & Advanced | Not authored | `SOURCE_SILENCE` |
| HM 6D numeric-policy scope | Peak volume, long-run share/cap, two-KEY interaction, threshold-vs-HM-pace sequencing, quality budget, session allocation — all flagged as open, none valued | `HM_21K_Claude_Handoff_and_Build_Plan.md:790-800` (`OPEN-HM-06`), explicitly: *"Do not extrapolate the 5D HM bands without governance."* | Both | Directional scope list only, zero values | `DIRECTIONAL_ONLY` |
| HM 6D as a planned closure wave | "Intermediate 6D, Advanced 6D" named as WAVE HM-F targets, "mostly product-policy/evidence closure, not architecture work" | `HM_21K_Claude_Handoff_and_Build_Plan.md:961-974` | Both | Confirms this is the expected next phase, no numbers | `DIRECTIONAL_ONLY` |
| 10K 6D `VolumeSafetyPolicy` values (peak, shares, caps, taper multiplier) | `SixDayIntermediate` byte-identical to `FiveDayIntermediate`; `Advanced6D` methodology mirrors `Advanced5D` | `VolumeSafetyPolicy.cs:539-552,648-661` | 10K Intermediate/Advanced | Real, live, shipped | `ACTIVE_EXISTING_AUTHORITY` — **for 10K only**; per this engagement's standing discipline (reaffirmed at HM.7, HM.10, HM-E1), cross-distance precedent is never automatic HM training authority |
| HM 5D's own frozen numbers (peak, shares, caps, 0.70/0.43 taper) | Real, live, shipped HM authority | `VolumeSafetyPolicy.cs` (HM sections) | HM Intermediate/Advanced 5D | Real, live, shipped | `ACTIVE_EXISTING_AUTHORITY` — same distance, adjacent frequency; directly relevant same-distance evidence for HM-X1.2 to weigh, but still not automatically the 6D answer (10K's own methodology of reusing the adjacent-frequency's evidence-envelope, rather than mechanically escalating it, is the more relevant precedent than the specific numbers) |

No number is frozen by this inventory. It becomes direct HM-X1.2 input.

## 22. Calendar capability

`CatalogWeekSkeletonCalendarMaterializer`'s `DaysPerWeek is not (2,3,4,5,6)` guard (widened at Phase 10K-FREQ.6D.26) is **distance-agnostic** — HM automatically inherits this widening at zero cost. Hard-session spacing is a pure day-distance rule (`MinimumKeySessionToLongRunSeparationDays=2`, reused as the KEY-to-KEY minimum), never a rest-day-count rule. 10K's own live 6D (`Freq6D26IntermediateSixDayDarkVerificationTests.cs`) already schedules 6 running days with exactly 1 rest day (`PreferredDays=[Mon,Tue,Wed,Thu,Fri,Sun]`, Saturday rest) successfully, real end to end, including LongHorizon repair after a real Postgres restart. No PreferredDays cardinality ceiling below 7 exists.

```
HM_6D_CALENDAR_CAPABILITY_READY
```

## 23. Recovery-day implications

No rule anywhere requires ≥2 rest days, ≤5 running days, or an EASY day between every hard/long combination — confirmed by direct read of the spacing validator and materializer (§22); the only enforced constraint is the ≥2-calendar-day separation between any two true-hard sessions (KEY↔KEY, KEY↔LONG), which is satisfiable within a 6-running/1-rest week exactly as 10K's own live 6D already proves. No structural conflict found; no new recovery policy is created here.

## 24. Taper capability

The existing 2-week non-chained taper mechanism (0.70/0.43 multipliers, `VolumeSafetyPolicy.TaperVolumeMultipliers`) applies against a fixed pre-taper reference and is redistributed across sessions by the same generic allocator audited in §18, which fails closed (`CatalogSessionPrescriptionInfeasibleException`) rather than silently degenerating if a tapered week's residual volume can't support the role minimums. Every existing HM taper-policy doc comment (3D/4D/5D, both levels) confirms its four cited evidence sources are volume/proximity-based, not frequency-scoped — the multiplier pair itself needs no per-frequency variant. 10K's own 6D uses its single 0.53 taper multiplier without incident at higher absolute session counts. Capability conclusion: **6 sessions in a taper week can be redistributed safely by the existing mechanism**; HM-X1.2 must still numerically verify feasibility once HM's own 6D peak-volume band is chosen, exactly as every prior HM frequency phase did — this is a numeric-fit check, not a mechanism gap.

## 25. Adaptation capability

`PlaceholderAdaptationEngine` (55 lines, both `EvaluateNotTodayAsync`/`EvaluatePendingConfirmationAsync`) unconditionally returns `AdaptationAction.NoChange` with static text — zero branching on frequency, session count, level, or distance. (Separately, real LongHorizon schedule-repair logic exists and is already frequency-generic, proven at 10K's own live 6D via a real-Postgres-restart repair test.)

```
GENERIC_ADAPTATION_SUFFICIENT
```

## 26. Confirmation/persistence/read compatibility

`CatalogPlanConfirmationService` persists via a variable-length `foreach` over `weekPayload.Sessions`, not a fixed array/tuple/DTO field count; no `sessions.Count <= 5` check exists anywhere in this file. Upstream materialization/validation checks `SessionSlots.Count == skeleton.DaysPerWeek` generically with no upper literal bound below 7. 10K's own live 6D public-activation test suite exercises the full preview→confirm→persist→Home/Calendar/repair pipeline against real Postgres with 6 sessions and 2 lanes, including verifying lane identity survives a `NotToday`/repair cycle — the strongest available architecture precedent, and it shows no count-based rejection anywhere in the pipeline.

## 27. Public-routing capability

`V1CatalogPilotIdentityPolicy`'s HALF_MARATHON arm is a flat, deliberately-enumerated tuple allow-list (`IsSupportedLevelFrequency`) plus a parallel candidate-key/version switch (`ResolveCandidate`) — both additive, independent-entry by design (per the class's own doc comment: "deliberately enumerated rather than derived, so a future cell can never be admitted by accident"). Every prior HM frequency widening (HM.2→HM.8→HM.11→HM.14→HM.16→HM.18) followed exactly this shape. Adding `(Intermediate,6)`/`(Advanced,6)` plus two new candidate-key constants is a narrow additive change, structurally identical to the 4 prior widenings and to 10K's own two 6D activations. Experienced remains excluded (no change to that exclusion); Beginner remains outside this track (no Beginner tuple is added or implied).

## 28. Frontend capability

`mobile/lib/features/onboarding/presentation/weekly_frequency_page.dart` already contains a `_halfMarathonFrequencyMatrix` — a `Map<RunningBackground, List<int>>` gating HM's frequency options **per level** (`{beginner:[3,4], intermediate:[3,4,5], advanced:[3,4,5]}`), explicitly documented as needing to be "kept in exact sync with the backend's own real, frozen gate." Adding `6` to `intermediate`/`advanced` only (leaving `beginner` unchanged) is a pure data change to this existing map — zero new UI architecture. The underlying day-picker (`running_days_selection_page.dart`) is already fully generic over `daysPerWeek`. **6D is already a live, selectable UI option for other distances today** (the `[2,3,4,5,6]` race-frequency list used for TEN_K), proving the day-selection UI itself requires no change. No Flutter runtime file is modified by this phase.

## 29. Intermediate final eligibility classification

```
HM_INTERMEDIATE_6D_PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE
```

**Reason**: `RUN_LAYOUT_6D` is structurally representable and distance-agnostic (§4); Intermediate's existing 2-KEY hard-stimulus authority (1 true-hard + 1 moderate FARTLEK, unchanged from 5D) fits within 6D's own 2-KEY structural ceiling with no conflict (§9, §11, §13); the existing HM primary and secondary progression lanes are directly reusable, unmodified (§14-§15); every generic mechanism audited (session allocation, long-run, calendar, taper, adaptation, confirmation/persistence, public routing, frontend) is already proven capable, largely via 10K's own live Intermediate 6D implementation (§7, §18-§28); no fundamental product contradiction was found. Missing exact numeric authority (peak band, selected peak, LR shares/caps, calibration start) does not block this classification per §28's own eligibility standard — that work belongs to HM-X1.2 (§31).

## 30. Advanced final eligibility classification

```
HM_ADVANCED_6D_PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE
```

**Reason**: identical structural/architecture basis to §29. Additionally confirmed: Advanced's existing 2-true-hard-stimulus pattern (already exercised at 5D via KEY1 + THRESHOLD_TEMPO KEY2) fits within 6D's unchanged 2-KEY structural ceiling with no escalation to a third hard session — 6D's RunLayout cannot structurally support a third KEY role without a new layout artifact, which is out of scope and not evidenced (§10, §12-§13). This explicitly avoids the escalation EXP.0 and this phase's own governing prompt warn against ("Advanced 6D → 3 true-hard sessions" is not inferred and not adopted). Missing exact numeric authority is deferred to HM-X1.2, consistent with §28's eligibility standard.

## 31. Reuse matrix

| Mechanism | 10K 6D Proven? | HM 5D Proven? | Reusable for HM 6D? | Needs New Authority? | Needs New Code? |
|---|---|---|---|---|---|
| RunLayout | Yes | Yes (5D) | Yes — `RUN_LAYOUT_6D` already exists, distance-agnostic | No | No |
| Phase allocator (progression stage allocator) | Yes | Yes | Yes — fully generic over N lanes | No | No |
| Stage allocator | Yes | Yes | Yes | No | No |
| Lane model | Yes (2 lanes) | Yes (2 lanes) | Yes — same 2-lane ceiling as 5D | No | No |
| Workout binder | Yes | Yes | Yes — layout-driven, frequency-agnostic | No | No |
| Session-volume allocator | Yes | Yes (generic 4-day-named class already serves 5D) | Yes | No | No |
| Volume progression (VolumeSafetyPolicy dispatch) | Yes (`case 6` exists for 10K) | Yes (closed at 3/4/5 for HM) | Yes, additive | **Yes — new named policy record + numeric values** | Minor — one new `case 6:` arm per HM dispatcher (mechanically identical to 4 prior widenings) |
| Long-run mechanics | Yes | Yes | Yes | **Yes — numeric values** | No |
| Taper mechanism | Yes | Yes | Yes | Possibly — feasibility re-verification once 6D volume is set; multipliers likely reusable as-is | No |
| Calendar | Yes (distance-agnostic widening already live) | Yes | Yes, at zero cost | No | No |
| Adaptation | Yes | Yes | Yes | No | No |
| Preview | Yes | Yes | Yes | No | No |
| Confirmation | Yes | Yes | Yes | No | No |
| Persistence | Yes | Yes | Yes | No | No |
| Active reads | Yes | Yes | Yes | No | No |
| Mutations (Complete/NotToday/repair) | Yes (lane identity survives repair) | Yes | Yes | No | No |
| Public gate | Yes (10K's own list includes 6) | N/A (HM's list stops at 5) | Yes, additive | No | Minor — allow-list + candidate-key entries (identical shape to 4 prior HM widenings) |
| Frontend frequency selection | Yes (6D already selectable for other distances) | N/A | Yes | No | Minor — one data-map entry, not touched in this phase |

**Overall**: the only real new work HM-X1.2 must do is numeric/calibration authority (peak band, selected peak, rates, calibration start, LR shares/caps, taper feasibility check) plus a handful of narrow, additive, well-precedented code widenings (four small switch/list additions) — no new engine, no new schema, no new lane concept, no new workout family.

## 32. HM-X1.2 authority gap register

For both Intermediate 6D and Advanced 6D, HM-X1.2 must freeze:

- `PeakVolumeBand` (per level)
- `SelectedPeakReferenceKmPerWeek` (per level)
- `PreferredWeeklyIncreaseRate` / `HardWeeklyIncreaseRate`
- `AbsoluteWeeklyIncreaseCapKm`
- `GoldenFixtureStartingVolumeKm` (calibration start)
- `LongRunPreferredMinimumShare` / `MaximumShare`
- `LongRunSelectionShare` / `HardCapShare`
- `PreferredAbsolutePeakLongRunKm`
- `StartingAbsoluteLongRunCapKm` (or explicit decision it doesn't apply, as HM Advanced 5D already shows precedent for)
- Taper-multiplier feasibility re-verification (numeric values 0.70/0.43 are likely directly reusable per §24, but HM-X1.2 must confirm no degeneracy at the chosen 6D volume)
- Secondary-lane (KEY2) numeric-family confirmation per level — this phase found the *structural* pattern (Intermediate: FARTLEK-moderate; Advanced: THRESHOLD_TEMPO-true-hard) is the only architecturally-consistent option, but HM-X1.2 must formally freeze this as authority, not merely inherit it by inference
- Actual hard-session count declaration (formalizing §13's finding: Intermediate=1 true-hard+1 moderate, Advanced=2 true-hard — both unchanged from 5D)
- Workout-family preference confirmation (FARTLEK v6 vs v4; THRESHOLD_TEMPO v4 vs the unwired v5 — HM-X1.2 should explicitly decide whether to close the pre-existing v5 dose-wiring gap noted in §16, or continue deferring it, as it is not a 6D-specific gap)
- Fallback-chain confirmation for the new 6D combination files (mirroring 5D's own fallback declarations)
- `HM_PACE` secondary-lane eligibility (this phase confirms the existing HM.15 §7-8 decision — HM_PACE stays reserved to KEY1/lane 0 only — is unaffected by 6D and should be explicitly re-affirmed, not silently inherited, by HM-X1.2)
- `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek`/`ForHalfMarathonAdvancedDaysPerWeek` new `case 6:` arms (implementation, HM-X1.3+ scope, not authority per se, but noted here since it's gated by the numeric authority HM-X1.2 produces)
- Matching `CatalogVolumeAndLongRunPlanner`/`CatalogFinalPrescribedPlanValidator` HM branch widenings (same gating)
- `V1CatalogPilotIdentityPolicy` allow-list/candidate-key additions (implementation/activation scope, not this phase's or HM-X1.2's numeric-authority scope)
- Flutter `_halfMarathonFrequencyMatrix` data update (implementation/activation scope)

## 33. Recurring assumption-family findings

| Location | Pattern | Classification |
|---|---|---|
| `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek` | closed switch 3,4,5 | `INTENTIONAL_PRODUCT_GATE` — HM hasn't run a 6D authority phase yet; proven additive by 10K's own `case 6` |
| `VolumeSafetyPolicy.ForHalfMarathonAdvancedDaysPerWeek` | closed switch 3,4,5 | `INTENTIONAL_PRODUCT_GATE` — same |
| `VolumeSafetyPolicy.ForHalfMarathonBeginnerDaysPerWeek` | closed switch 3,4 (5D excluded) | `INTENTIONAL_PRODUCT_GATE` — HM.13's explicit Beginner×5D decision, unrelated to this phase's Intermediate/Advanced scope, not reopened |
| `CatalogVolumeAndLongRunPlanner.Build` HM branches | `DaysPerWeek==3\|\|4\|\|5` per level | `INTENTIONAL_PRODUCT_GATE` — mirrors the dispatcher gate exactly |
| `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare` HM branches | same pattern | `INTENTIONAL_PRODUCT_GATE` |
| `CatalogFinalPrescribedPlanValidator.ValidateTaperCompleteness` HM candidate-key checks | closed, named-identity list | `INTENTIONAL_PRODUCT_GATE / additive` — every new HM identity has required one more explicit branch here; the dual-lane branch shape is already proven generic from 5D |
| `V1CatalogPilotIdentityPolicy` HALF_MARATHON allow-list | flat tuple list, no 6 | `INTENTIONAL_PRODUCT_GATE / additive` |
| `weekly_frequency_page.dart` `_halfMarathonFrequencyMatrix` | flat per-level list, no 6 | `INTENTIONAL_PRODUCT_GATE / additive` |
| `VolumeSafetyPolicy.ForIntermediateDaysPerWeek`/`ForAdvancedDaysPerWeek` (10K) | `case 6:` present | `NO_ISSUE` — positive precedent |
| `CatalogWeekSkeletonCalendarMaterializer` `DaysPerWeek is not (2,3,4,5,6)` | already admits 6, distance-agnostic | `NO_ISSUE` |
| `V1CatalogPilotIdentityPolicy`'s `SixDayCandidateKey`/`AdvancedSixDayCandidateKey` doc comments | say "dark-only, public gate closed" while the live switch/dispatch prove both are public | `HISTORICAL_ASSUMPTION` (stale doc comment, not a functional gap) — flagged so a future phase doesn't misread 10K 6D as still-dark; not corrected here since no production file is touched by this audit-only phase |
| HM test fixture `expectedKeyCount = daysPerWeek==5 ? 2 : 1` | test-local ternary | `TEST_FIXTURE` — HM's own tests only cover up to 5D today because no HM×6D candidate exists yet; not a production gap |
| `LongHorizonGeStructuralContracts.cs`/`LongHorizonRollingCheckpointRuntime.cs`/`LongHorizonRollingInitialActivationRuntime.cs` comments noting prior `DaysPerWeek==5`-only hardcodes | already fixed, historical note only | `HISTORICAL_ASSUMPTION, already fixed` — available to HM 6D for free |
| 10K `TEN_K_WORKOUT_PROGRESSION_V1`'s bound `WORKOUT_PRESCRIPTION_PROFILE` keys still literally named `INTERMEDIATE_5D_*`/`ADVANCED_5D_*` despite also serving 6D | naming lag, not a functional issue | `HISTORICAL_ASSUMPTION` (cosmetic) |

No patch was applied to any of these; all are recorded for HM-X1.2/HM-X1.3 awareness.

## 34. Existing HM V1 zero-delta statement

No file governing existing public HM V1 behavior (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D) was modified. Beginner 5D remains `PRODUCT_INELIGIBLE`, untouched. No Experienced work was performed (§2). `git status --porcelain` confirms only this report, the ledger, and the roadmap were touched (§37).

## 35. 10K zero-delta statement

10K's own 6D catalog/runtime behavior (Intermediate and Advanced) was read exhaustively as precedent evidence but not modified in any way — no file under `plan-catalog/catalog/**` or the 10K-specific runtime paths was edited. 10K 6D remains exactly as it was: real, live, public precedent, never redefined as HM training authority.

## 36. Recommended HM-X1.2 scope

HM-X1.2 should be a numeric/product-policy authority-closure phase (governance, not implementation — mirroring HM.13/HM.15's own pattern) that: (a) selects HM Intermediate 6D's and Advanced 6D's peak-volume bands, selected peaks, weekly-increase rates/caps, calibration starts, and long-run share/cap/ceiling values, using HM 5D's own adjacent-frequency evidence and 10K 6D's reuse-the-adjacent-frequency methodology (not its literal numbers) as the primary reasoning tools, per this engagement's standing cross-distance-evidence discipline; (b) formally freezes the secondary-lane (KEY2) workout-family assignment this phase found structurally necessary (Intermediate: FARTLEK-moderate; Advanced: THRESHOLD_TEMPO-true-hard, HM_PACE excluded from KEY2); (c) verifies taper-multiplier feasibility at the newly chosen 6D volume; (d) explicitly decides whether to also close the pre-existing, 6D-unrelated `THRESHOLD_TEMPO v5` dose-wiring gap (§16) or continue deferring it. HM-X1.2 should **not** perform any production implementation (that belongs to a subsequent HM-X1.3-style dark-implementation phase, following the same delegate-then-verify discipline used for HM.14/HM.16), and should not reopen Experienced, Beginner 6D, or any existing HM V1 cell.

---

## FINAL PHASE CLASSIFICATION

```
HM_6D_INTERMEDIATE_ADVANCED_ELIGIBILITY_AND_REUSE_AUDIT_CLOSED
```

The real `RUN_LAYOUT_6D` is fully understood (2 KEY / 3 EASY / 1 LONG, distance-agnostic, one `EASY_SUPPORT` slot more than 5D — a discovered fact, not an assumption). 10K's Intermediate and Advanced 6D precedent is fully audited, confirmed genuinely public (despite stale dark-only doc comments, flagged not fixed), and shown to have generalized nearly every mechanism HM would need via narrow, additive widenings rather than new engines. HM's own Intermediate and Advanced 5D precedent is fully audited and verified against real catalog artifacts, confirming the "light-quality FARTLEK" vs "genuine THRESHOLD_TEMPO true-hard" KEY2 asymmetry exactly as the governing prompt described. Both Intermediate 6D and Advanced 6D reach an exact product disposition (`PRODUCT_ELIGIBLE_FOR_AUTHORITY_CLOSURE`, §29-§30). Generic architecture capability is classified mechanism-by-mechanism (§7, §18-§28), with every one found already generically capable, mostly already 6D-proven by 10K. Every remaining numeric/structural authority gap is explicitly listed (§31-§32) without being filled. No new 6D numeric value was silently frozen anywhere in this report — every number cited is either existing 5D/10K-6D authority presented as precedent, or explicitly marked as an open gap for HM-X1.2. Experienced remains excluded (§2); Beginner 6D is not reopened (§3); existing HM V1 and 10K both remain fully unchanged (§34-§35); no production code, catalog artifact, schema, or Flutter runtime file was modified by this phase.
