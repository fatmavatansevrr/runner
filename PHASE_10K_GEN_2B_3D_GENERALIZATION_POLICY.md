# Phase 10K-GEN.2B — 3D Generalization Policy Resolution

**Decision resolution only. No production code, catalog JSON, or tests changed.**

## 1. Binding GEN.2A invariant acknowledgment

Frozen and not reopened: `CORE_SKELETON_AUTHORITY_SINGLE_DYNAMIC`, `RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY`, `COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST`, `SINGLE_CANONICAL_10K_CORE_APPROVED`, `BEHAVIORAL_EQUIVALENCE_REQUIRED`, and `GEN2A-INV-001` through `GEN2A-INV-012`.

The target is `TEN_K / INTERMEDIATE / 3D / CORE_PATH / 8–14 weeks`. Preparation Runway, LongHorizon, adaptation generalization, other Levels, 2D, and 5D–7D remain outside scope.

## 2. Frozen 3D structural policy

```text
RUN_LAYOUT_3D
RunsPerWeek = 3
Slots (ordered):
1. KEY_SESSION
2. EASY_SUPPORT
3. LONG_RUN
```

Decision:

```text
ROLE_CARDINALITY_AUTHORITATIVE
ROLE_ORDER_CONTRACTUALLY_MEANINGFUL
```

RunLayout owns the role multiset and ordered structural slot sequence. Order is contractually meaningful as deterministic materialization input: `CatalogStageToWeekMaterializer` produces `SlotOrderInWeek` and occurrence-keyed `LayoutSlotKey`; the binder iterates by slot order; provenance/public materialization retain structural identity. RunLayout order does **not** imply weekday/date order. Calendar policy separately assigns dates.

## 3. Canonical Core phase reuse

```text
3D_REUSES_CANONICAL_10K_PHASE_ARCHITECTURE
```

3D uses TEN_K_MASTER’s Foundation → Build → RaceSpecific → Taper structure and the existing 8–14-week allocation authority. `CatalogPhaseAllocationResolver`, progression-stage allocation, phase eligibility, and taper selection do not read Easy count or DaysPerWeek. Frequency changes weekly layout, not the Core phase timeline.

Normal 8/9/10/11/12/13/14 phase allocation differences remain horizon allocation, not 3D-specific policy.

## 4. Workout/progression policy

```text
3D_KEY_PROGRESSION_REUSES_EXISTING_MODEL
WORKOUT_IDENTITY_REUSABLE
DOSAGE_POLICY_DECISION_REQUIRED
```

Evidence:

- `GeneratedCatalogStageSchedule` assigns one progression-controlled KEY per eligible week, matching the frozen 3D layout’s one KEY.
- `V1CatalogWorkoutRoleBindingPolicy` binds KEY by progression stage, EASY to `EASY_STANDARD`, and LONG to `LONG_RUN_STANDARD`; it does not inspect total weekly frequency.
- `CatalogWorkoutBinder` consumes structural roles/slot order and has no two-Easy precondition for workout identity.
- TAPER_SHARPEN applies to `TAPER / TAPER_SHARPEN / KEY_SESSION / EASY_STANDARD`; it requires one eligible KEY session and a viable assigned session distance, not a four-session week.
- Pace/effort identity and workout component ordering are not frequency-branched.

However, actual KEY/EASY/LONG distances feed component and taper-sharpen dosage. The only allocation authority is explicitly four-day. Workout identity/progression can be reused, but safe 3D dosage cannot be approved until Section 7’s session-allocation decision is made.

## 5. Calendar policy

### 5.1 PreferredDays and LongRunDay

Approved derived contract:

```text
PreferredDays.Count == ResolvedRunLayout.RunsPerWeek == 3
PreferredDays are distinct
LongRunDayPreference is required
LongRunDayPreference ∈ PreferredDays
LONG_RUN is assigned to LongRunDayPreference
```

Authority: `CatalogWeekSkeletonCalendarMaterializer.ValidatePreferredDays`, `ValidateLongRunDay`, and GEN.2A derived-value invariants, with literal 4 replaced conceptually by resolved layout count.

### 5.2 KEY↔LONG spacing

```text
EXISTING_KEY_LONG_SPACING_REUSED_FOR_3D
```

Authority: `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays = 2`, reused by the calendar materializer and schedule-repair spacing validator. It is defined between role occurrences, not as a four-day ratio. The same-week and adjacent cross-week checks remain unchanged.

### 5.3 EASY placement

```text
3D_EASY_PLACEMENT_EXISTING_POLICY_REUSED
```

After LONG and KEY are selected, EASY receives the remaining preferred date. Current policy imposes no EASY↔KEY or EASY↔LONG minimum separation; adjacency is therefore allowed. This is reuse of the existing rule, not invention of a new rest-day rule.

### 5.4 Determinism

No new tie-break decision is required. Existing authority ranks valid KEY dates by:

1. greatest calendar-day separation from LONG;
2. chronologically earlier date;
3. bounded week-order backtracking for cross-week validity;
4. first complete valid assignment.

With one EASY, the single remaining preferred date is deterministic. If no full-plan assignment satisfies the spacing constraints, generation fails closed. RunLayout slot order never determines weekdays.

## 6. Weekly-volume authority

Total weekly volume is separate from session allocation.

### 6.1 StartingWeeklyVolume

| Input state | Decision | Authority/status |
|---|---|---|
| Valid recent weekly-volume evidence > 0 | `REUSE_EXISTING_FREQUENCY_INDEPENDENT_POLICY` | `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` uses the reported recent-four-week average; frequency does not alter observed user evidence |
| Missing evidence | `NEW_3D_POLICY_DATA_REQUIRED` / `DECISION_REQUIRED` | Current 16 km fallback provenance explicitly says TEN_K/INTERMEDIATE/4D |
| Explicit zero | `NEW_3D_POLICY_DATA_REQUIRED` / `DECISION_REQUIRED` | Current 12 km fallback is the same explicitly 4D product default |

The implementation must not silently reuse 16/12 km for 3D.

### 6.2 PeakWeeklyVolume

```text
EXISTING_3D_POLICY_DATA
```

`peak-volume-bands.v3.json` contains `TEN_K / INTERMEDIATE / runsPerWeek=3` with 22–32 km. This is the correct cross-axis policy shape approved by GEN.2A. The artifact and the current v10 combination/rule-pack chain are marked `DRAFT`, so production work still requires compatible publication/version binding; no new number should be inferred.

### 6.3 ProgressionRate

```text
DECISION_REQUIRED
```

The runtime interpolator is mechanically reusable, but `VolumeSafetyPolicy.Default` derives its multiplier/caps from a 4D golden fixture (24→38, 10 non-taper transitions; provenance includes Intermediate profile caps and an absolute-cap entry indexed for 4 runs). Repository evidence does not establish that the same starting-to-peak multiplier, 7%/8% ratios, or 2.5 km cap are the approved 3D Intermediate policy. A 3D-specific/versioned progression authority or explicit approval of frequency independence is required.

## 7. Session-level volume allocation

```text
3D_SESSION_VOLUME_ALLOCATION_DECISION_REQUIRED
EXTERNAL_EVIDENCE_REQUIRED
```

The current `V1FourDaySessionVolumeAllocationPolicy` and `FourDaySessionDistanceAllocationPolicy` define only 1 KEY + 2 EASY + 1 LONG. They encode:

- long-run distance supplied by the four-day long-run policy;
- residual split with KEY initially 50%;
- two equalized EASY residuals;
- minimum KEY 3 km and each EASY 1.5 km;
- 0.5 km rounding;
- final reconciliation applied to the second EASY.

Removing one EASY makes the policy’s split, minima, reconciliation target, and per-session load materially different. No canonical 3D policy defines:

- LONG_RUN share/cap;
- KEY share/dosage;
- EASY residual/share;
- 3D per-session viability minima;
- which session absorbs rounding reconciliation.

No percentages or copied 4D residual formula are approved here.

External research question:

> For structured 10K preparation at three running days per week, what evidence-supported workload envelope should govern long-run share of weekly running volume and the remaining distribution between one quality session and one easy session, including safe per-session minima and progression constraints?

External evidence can inform the decision, but an Appsel product/domain authority must still select and version the policy.

## 8. Long-run policy

```text
3D_LONG_RUN_POLICY_DECISION_REQUIRED
EXTERNAL_EVIDENCE_REQUIRED
```

`VolumeSafetyPolicy.Default` and `CatalogVolumeAndLongRunPlanner` explicitly describe the current 30–36% preferred range, 33% selection, and 40% hard cap as “four-day” practice/product default. The formula technically accepts a 3D peak band, but technical acceptance is not semantic authority. With only three sessions, this share interacts directly with the missing KEY/EASY allocation and per-session concentration. Reuse is not approved without evidence and an explicit Appsel decision.

Existing user longest-run evidence, weekly-volume relationship, rounding, and fail-closed caps remain candidate mechanisms, but their 3D thresholds/selection share are unresolved.

## 9. Pace/intensity policy

```text
3D_PACE_INTENSITY_REUSES_EXISTING_10K_AUTHORITY
```

EffortOnly behavior, GOAL_PACE_TEN_K exact pace (`TargetFinishTimeSeconds / GoalDistanceKm`), goal-feasibility gating, Fartlek/Threshold effort labels, and TAPER_SHARPEN’s effort-only rule depend on workout/phase/evidence, not frequency. No repository branch varies pace identity by DaysPerWeek.

Distinction: pace/intensity **mode and source** are reusable; session distance/component dosage remains blocked by Sections 7–8.

## 10. Cutback/recovery semantics

```text
3D_INTRODUCES_NO_NEW_CUTBACK_RULE
```

The canonical Core has no recurring recovery/deload rule; all non-taper weeks use the existing progression, and Taper alone reduces volume. 3D inherits that descriptive behavior. GEN.2B neither endorses its coaching optimality nor creates a frequency-specific recovery week.

## 11. Core horizon coverage

```text
3D_FREQUENCY_POLICY_IS_CORE_HORIZON_LENGTH_INDEPENDENT
```

The structural, calendar, workout-identity, and unresolved numeric-policy questions apply consistently to 8–14 weeks. Existing phase compression/extension changes exposure/week allocation only. No genuine 3D policy varies by Core length; numeric feasibility must still be validated for every length once the missing volume policies are approved.

## 12. Exact-12 authority migration interaction

```text
FIXED_12_MIGRATION_MAY_PROCEED_IN_PARALLEL
```

A future 3D path must use the canonical dynamic Core skeleton authority plus RUN_LAYOUT_3D and must not create a 3D-specific orchestrator. Exact-12/4D currently remains on the fixed caller while compressed/extended modes already use dynamic composition. It is not a technical prerequisite to implement 3D behind its own compatibility/rollout boundary, provided:

- no fixed 3D path exists;
- 3D uses only the dynamic authority;
- 4D behavioral equivalence remains protected;
- GEN.2A’s fixed-authority retirement continues with explicit exit criteria;
- 3D is not activated until all its downstream policies are approved and compatible.

## 13. Catalog/data requirements

Required before exposure of `TEN_K × INTERMEDIATE × 3D`:

1. `RUN_LAYOUT_3D` with ordered KEY/EASY/LONG slots.
2. A 3D combination compatibility/version manifest referencing existing TEN_K_MASTER and the resolved 3D layout.
3. A published/version-bound peak-band policy containing the existing 3D Intermediate row.
4. Approved/versioned 3D missing/zero starting-volume policy data.
5. Approved/versioned 3D progression policy or explicit frequency-independent binding.
6. Approved/versioned 3D session allocation and long-run policy artifacts/authorities.
7. A catalog-support entry distinct from a live rollout activation flag.

Must not duplicate TEN_K_MASTER, phase allocation, concrete 8–14 weeks, progression timeline (unless a later legitimate frequency-specific reference is approved), Intermediate LevelModifier if unchanged, or RulePack if unchanged apart from justified policy references.

## 14. Validation model

| Validator/policy | 3D role | Generic derived invariant? | Frequency policy? | Rollout gate? |
|---|---|---:|---:|---:|
| `CatalogRunLayoutResolver` | Validate candidate claim against layout | Yes: candidate DaysPerWeek equals layout count | No | No |
| `GeneratedCatalogPlanSkeletonValidator` | Validate weeks/slots | Yes: session count and role counts derive from layout/generated structure | No | No |
| `CatalogWeekSkeletonCalendarMaterializer` structural checks | Validate input layout/PreferredDays | Yes | Placement algorithm uses reusable KEY↔LONG policy | May temporarily reject non-4D until implemented |
| `DatedGeneratedCatalogPlanSkeletonValidator` | Validate dates, used days, roles, spacing | Yes: role/session/PreferredDay counts | Reuses spacing policy | May temporarily reject non-4D |
| `V1FourDaySessionVolumeAllocationPolicy` | Not a 3D validator | No | Yes, explicitly 4D | Yes |
| Future 3D session-allocation policy | Validate 1K/1E/1L input and reconcile totals | Counts derive from RunLayout | Yes; unresolved | Yes until approved |
| `CatalogPrescribedPlanValidator` | Validate accounted weekly/session totals | Yes | Uses selected allocation policy | May remain rollout-scoped |
| `CatalogFinalPrescribedPlanValidator` | Validate final prescription/taper | Counts derive from layout/plan | Taper dosage uses selected session distance | May remain rollout-scoped |
| `VolumeProgressionVerifier` | Verify weekly progression | Structure generic | Selected 3D progression policy required | Yes until approved |
| `LongRunProgressionVerifier` | Verify long-run progression/caps | Weekly records generic | Selected 3D long-run policy required | Yes until approved |
| Compatibility catalog | Validate TEN_K/Intermediate/3D reference graph | N/A | Legitimate cross-axis data | No |
| Rollout policy | Enable live 3D traffic | N/A | No domain authority | Yes |

Generic invariants:

```text
Candidate.DaysPerWeek == RunLayout.RunsPerWeek
PreferredDays.Count == RunLayout.RunsPerWeek
GeneratedWeek.SessionCount == RunLayout.Slots.Count
role counts == corresponding RunLayout role counts
dated/persisted counts == generated structural counts
```

Literal 3/1/1/1 may appear in the published RUN_LAYOUT_3D data and an explicitly selected 3D policy test, not as universal validator truth.

## 15. Adaptation scope

```text
CORE_3D_GENERALIZATION_DOES_NOT_REQUIRE_ADAPTATION_V1
```

CORE_PATH currently uses the pre-existing no-op `PlaceholderAdaptationEngine`, including supported Intermediate/4D. `CORE_ADAPTATION_NONE` is a `PRE_EXISTING_HORIZON_ADAPTATION_GAP`, not a Frequency regression or 3D activation promise. GEN.2B does not implement adaptation or resolve LongHorizon 3D calibration.

## 16. Final 3D policy table

| Dimension | 3D decision | Authority | Implementation-blocking? |
|---|---|---|---:|
| Structural layout | 1 KEY + 1 EASY + 1 LONG; ordered | RUN_LAYOUT_3D | Data required, policy resolved |
| Core phase architecture | Reuse Foundation/Build/RaceSpecific/Taper and 8–14 allocator | TEN_K_MASTER | No |
| KEY progression | Reuse one-KEY stage/workout identity; dosage pending allocation | progression catalog/binder | **Yes for dosage** |
| PreferredDays cardinality | Three distinct days, derived from layout | RunLayout + calendar boundary | No policy ambiguity |
| LongRunDayPreference | Required, member of PreferredDays, owns LONG date | existing calendar policy | No |
| KEY↔LONG spacing | Reuse minimum 2 calendar days, same/cross-week | dated-skeleton validator | No |
| EASY placement | Remaining preferred date; adjacency allowed | existing calendar policy | No |
| Calendar tie-break | Max KEY↔LONG separation, then earlier date, bounded backtracking | calendar materializer | No |
| Starting weekly volume | User evidence reused; missing/zero 3D defaults unresolved | readiness + future 3D policy | **Yes** |
| Peak weekly volume | Existing 3D Intermediate 22–32 km DRAFT row | peak-band policy v3 | Publication/data binding required |
| Progression rate | 3D multiplier/caps not approved | future 3D/frequency-independent decision | **Yes** |
| Session volume allocation | Unresolved KEY/EASY/LONG split/minima/rounding | future 3D allocation policy | **Yes** |
| Long-run allocation | Four-day 30–36/33/40 policy not approved for 3D | future 3D long-run policy | **Yes** |
| Pace/intensity | Reuse current 10K modes/sources | workout/prescription authorities | No; dosage still blocked |
| Cutback behavior | No new recurring cutback; existing taper only | TEN_K_MASTER/volume planner | No |
| 8–14 horizon behavior | Same frequency policy for all lengths | canonical Core allocator | No; validate numeric feasibility later |
| Adaptation requirement | Not required for Core 3D parity | current Core contract | No |

## 17. Remaining implementation blockers

### RESOLVED_FOR_IMPLEMENTATION

- RUN_LAYOUT_3D structure/order and dynamic skeleton authority.
- Canonical Core phase reuse for 8–14 weeks.
- One-KEY workout identity/progression reuse.
- PreferredDays/LongRunDay contract.
- KEY↔LONG spacing, EASY placement, and deterministic tie-break.
- Pace/intensity source/mode reuse.
- No new cutback rule.
- Exact-12 migration may proceed in parallel.
- Generic derived validation model.
- No Adaptation V1 prerequisite for Core.

### DATA_REQUIRED

- RUN_LAYOUT_3D artifact.
- 3D combination manifest.
- Publication/version binding for the existing DRAFT 3D peak-band row.
- Compatibility and rollout entries after policies are approved.

### DOMAIN_DECISION_REQUIRED

1. Missing-evidence and explicit-zero 3D starting weekly volume.
2. 3D progression multiplier/weekly increase caps or explicit approval of a frequency-independent policy.
3. KEY/EASY/LONG session-distance allocation, minima, and rounding reconciliation.
4. 3D long-run preferred range, selection share, and hard cap.

These four items prevent the first deterministic 3D implementation phase from completing/activating. Structural scaffolding could be written later only if separately authorized, but product-complete 3D prescription cannot be implemented from current authority.

### EXTERNAL_EVIDENCE_REQUIRED

External coaching/scientific research is required to inform items 3–4 and may be useful for items 1–2. Repository evidence alone cannot choose the product defaults. Research must address the question in Section 7 and distinguish observed training practice from an Appsel-approved safety/product rule.

### TECHNICAL_IMPLEMENTATION_ONLY

Once the domain decisions are approved: implement derived validators, parameterize calendar cardinality, add/select 3D policy contracts, generalize session planner dispatch, add catalog manifests/data, and keep rollout disabled until end-to-end validation. These are future-phase tasks, not performed here.

## 18. Files/artifacts inspected

- `PHASE_10K_GEN_2A_FREQUENCY_ARCHITECTURE_AUTHORITY_DECISION.md`
- `PHASE_10K_GEN_1_ARCHITECTURE_AUDIT.md`
- `DynamicCoreWeekSkeletonOrchestrator.cs`, `CatalogStageToWeekMaterializer.cs`, `CatalogRunLayoutSlots.cs`
- `CatalogWeekSkeletonCalendarMaterializer.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`
- `ProgressionStageAllocator.cs`, `ProgressionStageScheduleContracts.cs`, `CatalogWorkoutBinder.cs`, `V1CatalogWorkoutRoleBindingPolicy.cs`
- `CatalogSessionPrescriptionPlanner.cs`, `V1FourDaySessionVolumeAllocationPolicy.cs`, `FourDaySessionDistanceAllocationPolicy.cs`, `V1TaperSharpenPrescriptionPolicy.cs`
- `CatalogVolumeAndLongRunPlanner.cs`, `VolumeSafetyPolicy.cs`, `V1MissingReadinessStartingVolumePolicy.cs`
- `peak-volume-bands.v1/v2/v3.json`, `appsel-race-plan.v4.json`, `ten-k-4d-intermediate.v10.json`
- `ten-k-master.v6.json`, `ten-k-workout-progression.v5.json`, `run-layout-4d.v2.json`, `intermediate-modifier.v6.json`
- Relevant GEN.1/GEN.2A call-site and validator evidence.

## 19. Final classification

```text
10K_GEN_2B_3D_CORE_POLICY_REQUIRES_DOMAIN_DECISIONS
```

Architecture and non-numeric policy are clear, and no repository contradiction blocks the frozen 3D target. The four domain decisions in Section 17 must be approved before a deterministic, product-complete 3D Core implementation can begin.
