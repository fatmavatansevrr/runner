# Phase 10K-GEN.1 — Architecture Generalization Audit

**Audit only. No production code written or modified. No dark components activated. No target-model decision silently changed.**

## Target-model source note

The referenced canonical file `appsel-10k-generalization-v1-frequency-level-framework.md` **does not exist anywhere in this repository** (confirmed via exhaustive filename search). This audit proceeds using the frozen target-model specifics given verbatim inline in the PART 1/PART 2 phase prompts themselves (the §2 WeeklySkeletonCatalog 3D–7D rows, the §4 LevelModifier dimension list, the 3×5 Level×Frequency matrix, and the explicit out-of-scope list) as the authoritative frozen target. This substitution is disclosed here rather than silently assumed.

## 0. GEN.0 baseline acknowledgment

GEN.0 (`PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md`) established, and this audit treats as given without re-deriving: the three-way horizon split (CORE_PATH 8–14w / RUNWAY_PLUS_CORE_PATH 15–20w / LONG_HORIZON_PATH 21+w) via `RaceHorizonPolicy`; the bundled `TEN_K__4D__INTERMEDIATE` identity; Intermediate's limited downstream semantics (one C2 data-selection effect, dominant C1/C3 routing effect); Frequency as the most deeply specialized axis (6+ hardcoded points); the existence and zero-call-site status of `DynamicCoreWeekSkeletonOrchestrator`; `WeeklyWindowPartitioner`'s frequency-agnostic partitioning; and the general shape of the hard-coding/dynamic-seam inventories. This audit verifies these where the PART 2 questions required deeper tracing, and does not re-litigate settled findings.

---

## 1. Horizon architecture findings

### 1.1 Exact current routing/boundary rules

The pure week-count arithmetic is centralized in **`CoreHorizonClassifier.Classify`** (`RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs`, lines 91–151): `availableDays = RaceDate.DayNumber - StartDate.DayNumber` (exclusive), `AvailableFullWeeks = availableDays / 7` (never rounds up), bucketed via `RaceHorizonPolicy`'s constants (`MinimumSupportedStandaloneWeeks=8`, `ExactStandaloneCoreSupportedWeeks=12`, `MaximumSupportedStandaloneWeeks=14`).

**Critical finding: the further 15–20 vs. 21+ split is *not* computed in one place.** `LongHorizonCompositionResolver.Resolve` (a pure, correctly-parameterized resolver with named constants `MinimumLongHorizonFullWeeks=21`, `MaximumSupportedFullWeeks=52`) is explicitly documented as **not called from any live preview-generation request path** (its own doc comment: "PlanServices and CatalogPreviewGenerator continue to call RaceHorizonPolicy exactly as before"). The boundary is instead re-expressed as **independently duplicated bare integer-literal range checks** at three separate live call sites: `PlanServices.cs:147` (`>= 15 and <= 20`), `TenKPreparationRunwayDarkOrchestrator.cs:102` (`< 15 or > 20`), and `LongHorizonPublicPlanService.cs` (`< 21 or > 52`, via its own call into `LongHorizonCompositionResolver.Resolve` — this one resolver call *is* live, just not the one `PlanServices`/`TenKPreparationRunwayDarkOrchestrator` use). This is a pre-existing, Frequency/Level-independent architectural fact: the horizon boundary itself is already fragmented across independently-maintained literals, a latent drift risk unrelated to this generalization effort but relevant to §14 (duplicate authority).

### 1.2 Does each family's routing/internal logic reference DaysPerWeek/Level/Distance?

All three do, but only as an **identity gate**, never inside the pure week-count math itself:

| Horizon family | Identity-gate mechanism | Call sites |
|---|---|---|
| CORE_PATH | Calls the shared `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` | 2 real call sites (`V1LiveCatalogPilotRoutingPolicy.Evaluate`, `LivePlanPreviewRoutingService.Decide`) + 1 inline re-check (`LivePlanPreviewRouteDecisionValidator.Validate`) |
| RUNWAY_PLUS_CORE_PATH | Hand-duplicated inline (`PlanServices.IsPreparationRunwayPilotScope`), does **not** call the shared policy | Re-checked a second time inside `TenKPreparationRunwayDarkOrchestrator.ValidateRequest` (twice, on two different carried objects: `Candidate` and `PreviewRequest`/`ResolverInput`) — and this orchestrator additionally pins the exact `CandidateVersion == 10`, a stricter axis of coupling absent from the other two families |
| LONG_HORIZON_PATH | Hand-duplicated inline (`LongHorizonPublicPlanService.ValidatePilot`) | Re-checked independently in `LongHorizonRollingCheckpointRuntime.cs:368`, `LongHorizonRollingInitialActivationContracts.cs:105`; identity is additionally **silently hardcoded (not even re-derived)** in `LongHorizonPublicPlanService.BuildTrainingPlan` (lines 315–319) and its initial-activation request builder (lines 159–162) — these two sites never read the caller's actual Level/DaysPerWeek/Distance fields at all, meaning generalization here requires adding new plumbing, not just relaxing a conditional |

**Net count of independently-typed identity comparisons** across all three families (excluding the one canonical `V1CatalogPilotIdentityPolicy` definition and its two legitimate call sites): **7**, none derived from a shared source.

### 1.3 Is horizon-family *selection* itself bundled-identity-dependent, or independent?

**Both, and they must be kept separate.** The pure week-count arithmetic (`CoreHorizonClassifier.Classify`, `LongHorizonCompositionResolver.Resolve`) takes no Level/DaysPerWeek/Distance parameter — horizon-family *selection* is already Distance/Level/Frequency-independent. But *reachability* of every non-trivial pipeline built on top of that arithmetic is currently identity-gated in all three families, independently, before generation is allowed to proceed (§1.2). This is the concrete evidence for GEN.0's B2 distinction between ROUTING/IDENTITY ABSENCE and ALGORITHMIC/STRUCTURAL INCOMPATIBILITY: the week-count classifier is already free of the bundled identity; the generation pipelines wired to fire on its output are not yet free of it, but that absence is (mostly) a routing-layer fact, not an algorithmic one, for CORE_PATH — and less so for RUNWAY_PLUS_CORE_PATH/LONG_HORIZON_PATH, per §1.4.

### 1.4 CORE_PATH vs. RUNWAY_PLUS_CORE_PATH vs. LONG_HORIZON_PATH compared explicitly

| Aspect | CORE_PATH | RUNWAY_PLUS_CORE_PATH | LONG_HORIZON_PATH |
|---|---|---|---|
| Identity-gate centralization | 1 shared definition, ≤3 call sites | Duplicated inline, ≤3 sites across 2 files, plus an extra `CandidateVersion` pin | Duplicated inline at ≤5 sites across 4 files, plus 2 hardcoded-literal (non-comparison) sites |
| Non-Core segment's own allocation system | N/A | `PreparationRunwayBlockAllocationEngine` (a completely separate allocation vocabulary from `CatalogPhaseAllocationResolver`) | Fixed formula: 8w Runway + 12w Core + `(weeks−20)` General-Endurance weeks; General-Endurance itself has no `CatalogPhaseAllocationResolver`/`ProgressionStageAllocator` equivalent and is explicitly gated `EligibleButGenerationNotActivated` (open tech-debt item `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`, pre-existing, unrelated to this audit) |
| Reachable via `PlanServices.GeneratePreviewAsync`? | Yes | Yes (branch) | **No** — separate controller action entirely |
| Skeleton-generation authority | `CatalogRunLayoutResolver`/`CatalogStageToWeekMaterializer` (list-based, genuinely generic — §3) | Delegates its 12-week Core segment to the *same* Core pipeline; its own Runway segment uses `PreparationRunwayWeekMaterializer` (independently hardcoded, §6 items 5–11) | Its own, structurally **more rigid** closed-enum representation (`LongHorizonGeStructuralContracts.LongHorizonGeWeekRole`, §9) and its own calendar assigner (`LongHorizonCalendarAssigner`, fixed 4-entry dictionary), sharing **nothing** with the Core-path materializer |

**Do not generalize a finding from one family to another** — confirmed necessary by evidence: none of the three families' skeleton, calendar, or validation components are shared code. Fixing CORE_PATH's frequency assumptions does not touch RUNWAY_PLUS_CORE_PATH's Runway segment or LONG_HORIZON_PATH at all.

### 1.5 HORIZON_PATH_DIVERGENCE flags relevant to future activation sequencing (not proposing sequencing)

1. Gate centralization differs per family (§1.2) — CORE_PATH's gate could plausibly widen via one shared type + 2–3 call sites; the other two require locating and independently updating each duplicated literal.
2. RUNWAY_PLUS_CORE_PATH additionally pins an exact `CandidateVersion`, a coupling axis absent elsewhere.
3. LONG_HORIZON_PATH has silent-hardcode sites (not guard-and-throw sites) that never read the real request fields — a materially different, easier-to-miss class of change.
4. RUNWAY_PLUS_CORE_PATH and LONG_HORIZON_PATH each own a *second*, non-`CatalogPhaseAllocationResolver` allocation system for their non-Core segment — generalizing "the Core segment" alone does not generalize these adjoining segments.
5. The week-boundary literals (15/20/21/52) are independently hardcoded at 2–3 sites per family rather than one shared constant set — a pre-existing drift risk, not created by this audit, but compounding any future generalization coordination cost.

---

## 2. 10K phase-structure findings

### 2.1 Canonical allocation (source: `plan-catalog/catalog/templates/ten-k-master.v6.json`, loaded via `PlanCatalogBundleLoader.ReadPhaseAllocations`, which requires exactly the 4-key set `{FOUNDATION, BUILD, RACE_SPECIFIC, TAPER}`)

| Phase | Min | Preferred (12w) | Max | Compression priority | Extension priority | Compression-protected |
|---|---|---|---|---|---|---|
| FOUNDATION | 2 | 3 | 4 | 1 | 1 | false |
| BUILD | 3 | 4 | 5 | 2 | 2 | false |
| RACE_SPECIFIC | 2 | 4 | 4 | 3 | 3 | false |
| TAPER | 1 | 1 | 1 | 4 | 4 | **true** |

13-week: FOUNDATION 3→4 (its max). 14-week: FOUNDATION 3→4, BUILD 4→5 (its max). TAPER is compression-protected and never varies from 1 week at any of the 8–14 week counts. No cutback/recovery-week concept exists anywhere in this allocator or the volume planner (`IsRecoveryOrDeloadWeek` hardcoded `false` for every week; `recoveryRule` field literally `"no_catalog_recurring_recovery_or_deload_rule_present"`).

### 2.2 Per-phase evidence (LABEL / WEEK COUNT / OBJECTIVE / PROGRESSION STAGE / ELIGIBILITY / VOLUME / PACE / LONG-RUN / TAPER-SPECIFIC)

**FOUNDATION** — label `"FOUNDATION"`; declared `intents: ["AEROBIC_BASE"]` (confirmed **descriptive-only**: no grep hit ties `intents` to any runtime decision anywhere in `CatalogPhaseAllocationResolver`/`CatalogVolumeAndLongRunPlanner`/`ProgressionStageAllocator`/`CatalogSessionPrescriptionPlanner`); progression stage `FOUNDATION_EASY_BASE` (unconditional); eligible families `[EASY, LONG_RUN]`; volume = shared linear-interpolation formula (§2.3); pace = `EffortOnly`; long-run = shared formula; no taper behavior.

**BUILD** — label `"BUILD"`; `intents: ["VOLUME_BUILD"]` (same descriptive-only caveat); two progression stages, `FARTLEK_INTRO` then `THRESHOLD_INTRO` (both unconditional); eligible families `[EASY, LONG_RUN, QUALITY]` — QUALITY newly introduced; volume = same shared formula; pace = `EffortOnly` for both quality workouts (no numeric pace anywhere in BUILD); long-run = shared formula; no taper behavior.

**RACE_SPECIFIC** — label `"RACE_SPECIFIC"`; `intents: ["RACE_SPECIFIC_SHARPENING"]` (descriptive-only); three progression stages — `TEN_K_SPECIFIC_INTRO`, `GOAL_PACE_REHEARSAL` (**PROTECTED**/**FIXED_EXPOSURE**, conditionally gated on `GOAL_FEASIBILITY_IN [REALISTIC, CHALLENGING]`, with fallback to `CURRENT_FITNESS_SPECIFIC_REHEARSAL`), `CURRENT_FITNESS_SPECIFIC_REHEARSAL`; eligible families `[EASY, LONG_RUN, QUALITY]` — **identical declared set to BUILD**, despite the actual workout roster differing underneath (FARTLEK+THRESHOLD_TEMPO → THRESHOLD_TEMPO+GOAL_PACE_TEN_K); volume = same shared formula (confirmed no phase branch anywhere in `CatalogVolumeAndLongRunPlanner`); pace = **the only phase where numeric `ExactPace` is ever computed** (`GOAL_PACE_TEN_K` only, `TargetFinishTimeSeconds / GoalDistanceKm`); long-run = shared formula; no taper behavior.

**TAPER** — label `"TAPER"`; `intents: ["TAPER"]`; single progression stage `TAPER_SHARPEN` (**PROTECTED**/**FIXED_EXPOSURE**), whose only scheduled workout is `EASY_STANDARD` — **the same base workout definition as FOUNDATION**, not a distinct "sharpening" workout; declared eligible families `[EASY, LONG_RUN, QUALITY, RACE]` — the broadest declared set of any phase, **but no QUALITY- or RACE-family workout is ever actually scheduled** (a genuine label-vs-runtime-meaning gap, see §2.5); volume = **the one materially distinct formula**: `previousWeek * TaperMultiplier` (multiplicative decay, ~47% reduction by default, gated by a literal `week.PhaseKey == "TAPER"` string check); pace = base `EffortOnly`, but the KEY_SESSION under `TAPER_SHARPEN` receives an additive post-processing step (`V1TaperSharpenPrescriptionPolicy.Complete`) splitting it into three effort-only segments (easy/controlled-sharpen/easy-recovery), still no numeric pace; long-run = shared formula (percentage-of-weekly-volume unchanged, only the base volume it's a percentage of has already decayed); taper-specific = the finalizer flags exactly this one session as `BaselinePrescribedSharpeningPending` and throws if any *other* session is left in that state — architecturally exclusive to TAPER's single key session.

### 2.3 Verified: FOUNDATION/BUILD/RACE_SPECIFIC share one volume formula; TAPER alone diverges

Confirmed directly in `CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan`: `var isTaper = week.PhaseKey == "TAPER"` is the **only** phase-identity branch in the entire weekly-volume computation. The non-taper branch computes `unclamped = starting + ((peak - starting) * index / denominator)` — pure linear interpolation, applied identically and uninterrupted across FOUNDATION→BUILD→RACE_SPECIFIC. **Consequence, confirmed:** these two phase boundaries change *nothing* in the volume formula; only progression-stage selection and phase-week-count allocation change there. Only the RACE_SPECIFIC→TAPER boundary changes the volume formula itself (interpolation → multiplicative decay).

### 2.4 Phase OBJECTIVE vs. PRESCRIPTION — evidence-based answer

**From repository evidence only:** nothing in `CatalogPhaseAllocationResolver`, `CatalogVolumeAndLongRunPlanner`, `V1TaperSharpenPrescriptionPolicy`, or `ProgressionStageAllocator` reads `RunningBackground`/`DaysPerWeek` to decide phase structure or formula shape. The only place Level/Frequency enter numeric prescription is as **inputs to the same unchanged formula** — the peak-volume-band clamp (keyed on `experience`/`runsPerWeek`) and the weekly-increment cap. **The current architecture's observed behavior is: the phase skeleton (FOUNDATION→BUILD→RACE_SPECIFIC→TAPER, TAPER's decay rule) is level/frequency-invariant as built; Level/DaysPerWeek currently reach the system only as numeric inputs to prescription, not as selectors of a different phase structure.** This is a description of what's wired today, not a claim about what a correct generalization *should* do — but it directly supports the "Frequency/Level should only change PRESCRIPTION inside an unchanged phase skeleton" reading of the current architecture, since that is observably how the one populated combination (Intermediate/4D) already works internally, and nothing in the phase-allocation/volume code depends on Frequency or Level to decide *which* phase runs or *what formula* it uses (only TAPER's own, level/frequency-independent, always-active decay rule diverges).

### 2.5 Exact phase-boundary changes

| Boundary | Workout IDs | Progression stage | Volume formula | Pace | Long-run |
|---|---|---|---|---|---|
| Foundation→Build | `EASY_STANDARD` only → +`FARTLEK`, +`THRESHOLD_TEMPO` | `FOUNDATION_EASY_BASE` → `FARTLEK_INTRO`/`THRESHOLD_INTRO` | unchanged | unchanged (`EffortOnly`) | unchanged |
| Build→RaceSpecific | `FARTLEK` drops, `THRESHOLD_TEMPO` reused, +`GOAL_PACE_TEN_K` | → `TEN_K_SPECIFIC_INTRO`/`GOAL_PACE_REHEARSAL`/`CURRENT_FITNESS_SPECIFIC_REHEARSAL` (first runtime-conditioned stage, gated on goal feasibility) | unchanged | **only boundary introducing numeric `ExactPace`** | unchanged |
| RaceSpecific→Taper | Collapses to `EASY_STANDARD` only | → single `TAPER_SHARPEN` | **only boundary changing the formula itself** (interpolation → decay) | numeric pace disappears; additive 3-segment split appears | unchanged formula, decayed input |

### 2.6 Where phase labels encode less runtime meaning than names suggest

1. `intents` field is carried as metadata but has **no runtime consumer** in any inspected file.
2. TAPER's declared eligibility (`QUALITY`, `RACE`) promises workout variety the runtime never exercises — only `EASY_STANDARD` ever runs.
3. RACE_SPECIFIC's declared `eligibleWorkoutFamilies` is **identical** to BUILD's despite different actual rosters — differentiation lives one level deeper, in the progression-stage catalog's per-stage `workoutCandidates`, not the phase-level family-eligibility field.
4. TAPER's "sharpening" is the same `EASY_STANDARD` definition as FOUNDATION, distinguished only by a small, architecturally-exclusive post-hoc segment-split policy — not a differently-selected workout.
5. Phase *behavioral* differentiation is concentrated almost entirely in `ProgressionStageAllocator`'s per-phase stage catalog and, exclusively for RACE_SPECIFIC, the numeric-goal-pace branch — everything upstream (week-count allocation, volume formula for the three non-taper phases) treats them identically.

---

## 3. Level-specific findings

### 3.1 `RunningBackground` enum ground truth

`RunningApp.Domain/Enums/RunningBackground.cs:29-35`: `{Beginner, Intermediate, Advanced, Experienced}` — four values, confirmed. (Per the phase's explicit out-of-scope instruction, `Experienced`/"Expert" was not investigated as a target.)

### 3.2 Every hardcoded Intermediate dependency — classified

All ~21 literal `RunningBackground.Intermediate` references across `RuntimeCatalog` resolve into **exactly one repeated pattern**: an identity reject-gate (`Level != Intermediate → throw/reject`) or an identity default (`Level ?? Intermediate` / hardcoded construction), at call sites spanning `V1CatalogPilotIdentityPolicy.cs`, `LivePlanPreviewRouting.cs`, `CatalogPlanConfirmationService.cs:577`, `LongHorizonPublicPlanService.cs` (×3), `LongHorizonFullNumericOrchestrator.cs:268`, `LongHorizonRollingInitialActivationContracts.cs:105`, `LongHorizonRollingCoreGenerationInputAdapter.cs` (×2), `TenKPreparationRunwayDarkOrchestrator.cs:348-349` (checked twice, on two different carried objects), `LongHorizonRollingCheckpointRuntime.cs:368`, plus test/harness fixture defaults. **Classification: every single hit is `IDENTITY_ROUTING` or `PARAMETER_VALUE` (a default). Zero `ALGORITHMIC_LEVEL_DEPENDENCY` findings exist anywhere in the codebase** — no code branches "if Beginner do X, if Advanced do Y." All are `TRIVIALLY_PARAMETRIZABLE` in isolation (each is a lookup-key comparison or a default value), though the *number* of independently-duplicated sites (§1.2, §14) makes the aggregate change non-trivial in practice.

`IGNORED_AFTER_ROUTING`: once identity gates pass, the request's `Level` field is never read again — `PlanCatalogBundleLoader.cs:82` derives an `experience` string from the resolved catalog artifact (tied to the fixed `candidateKey`), independent of the caller-supplied value.

### 3.3 LevelModifier target-dimension cross-reference

| Dimension | Classification | Evidence |
|---|---|---|
| **PeakWeeklyVolume** | `EXISTING_GENERIC_MECHANISM` | `CatalogPeakVolumeBandLoader.LoadAsync(reference, distanceFamily, experience, runsPerWeek, ct)` — a genuine composite-key lookup over a policy document's `entries[]` array, throwing if no match. Real, working, multi-key infrastructure; only Intermediate/TenK rows currently populated. |
| **StartingWeeklyVolume** | `EXISTING_INTERMEDIATE_PARAMETER` | `VolumeSafetyPolicy.Default.GoldenFixtureStartingVolumeKm = 24d` — single hardcoded double, not level-keyed. |
| **ProgressionRate** | `EXISTING_INTERMEDIATE_PARAMETER` | Same `VolumeSafetyPolicy.Default` (`PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`). **Notable:** a `ProgressionModifier` catalog reference is threaded through the pipeline but confirmed **only ever read for its `.Key`/`.Version` in provenance-string concatenation** (`CatalogVolumeAndLongRunPlanner.cs:242`) — the reference exists but is currently decorative; no code path opens/loads its actual content the way `levelModifier`/`rulePack` documents are opened. |
| **QualitySessionCount** | `FRAMEWORK_DIMENSION_DOES_NOT_MAP_CLEANLY` | No `QualitySession*` concept exists anywhere (zero grep hits). The nearest concept, `SlotRoles`/layout `slots[].role`, is a **frequency/layout** concept keyed by `runsPerWeek`, not a level concept. |
| **IntensityDistribution** | `FRAMEWORK_DIMENSION_DOES_NOT_MAP_CLEANLY` | No `IntensityDistribution`/pace-zone-distribution concept exists anywhere (zero grep hits). Workout typing is template-authored per-workout, not derived from a per-level intensity-distribution formula. |
| **PlanHorizonDefault** | `MISSING_MECHANISM` | No level-keyed horizon-default lookup exists. Horizon is determined entirely by week-count-to-race, independent of Level. |

### 3.4 Does Level affect the three horizon families the same way or differently?

**Identically — a structural symmetry.** All three paths read Level exclusively as an identity reject-gate (§1.2/§3.2). No path branches algorithmically on Level beyond that gate. The three families are level-uniform, not level-divergent, in their current wiring.

### 3.5 What would adding Beginner/Advanced primarily require?

**MULTIPLE — `DATA_ONLY` + `ROUTING_IDENTITY`, plus one genuine `LEVEL_PRESCRIPTION` gap.** `ROUTING_IDENTITY`: the ~7 independently-duplicated identity gates (§1.2) each need new accepted values, ideally centralized through `V1CatalogPilotIdentityPolicy` turned into a real keyed table rather than a single `const`. `DATA_ONLY`: new `level-modifiers`/`PEAK_VOLUME_BAND_POLICY`/`templates`/`layouts` catalog rows — the loading mechanism is already generic enough to consume them without code changes. `LEVEL_PRESCRIPTION` (the one genuine gap): `VolumeSafetyPolicy` supports constructor-injection of an alternate policy already (the injection *mechanism* exists), but **no production code path anywhere selects a policy by Level today — every call site constructs the default.** Closing this requires writing a new, small selection mechanism (a level-keyed policy chooser), not a rewritten algorithm — but it is a genuine, currently-nonexistent control-flow addition, not merely new data. **No architecture change beyond this narrow selector gap is evidenced** — the pipeline's overall shape (template → layout → level-modifier → rule-pack → volume-planner) is level-agnostic by design and specifically populated for one level today.

---

## 4. Frequency-specific findings

### 4.1 Fixed-4D inventory (comprehensive, 24 independent sites)

**Generic/parameterized (no hardcode, confirmed by full read):** `CatalogRunLayoutSlots.cs`/`CatalogRunLayoutResolver` (list-based, validates against `candidate.DaysPerWeek`, zero literal "4"), `CatalogStageToWeekMaterializer.cs` (generic loops throughout), `GeneratedCatalogPlanSkeletonValidator.cs:110` and `GeneratedCatalogPlanPayloadValidator.cs:133` (both compare against the skeleton/payload's own field — `DERIVED_INVARIANT`), the API/command validators (`DaysPerWeek` bounded 1–7 generically), `TrainingPlan`/`PlanTemplate`/`LongHorizonRollingPlanState` DB columns (plain `int`, no CHECK constraint), and seed data (real `DaysPerWeek=3` legacy templates already exist and round-trip through the legacy engine, proving 3-day plans are not universally novel to this codebase).

**Specialized/hardcoded — full 24-site inventory, grouped by horizon family:**

*CORE_PATH (4 sites):*
1. `CatalogWeekSkeletonCalendarMaterializer.cs:124,132,149-157,179-182` — `SCHEMA_CARDINALITY_SPECIALIZED` (load-bearing for the date-assignment algorithm itself, not a redundant guard — see §7).
2. `V1FourDaySessionVolumeAllocationPolicy.cs:30-34` — `SCHEMA_CARDINALITY_SPECIALIZED` (deliberately versioned "V1").
3. `DatedGeneratedCatalogPlanSkeletonValidator.cs:93,139-145` — `SCHEMA_CARDINALITY_SPECIALIZED`.
4. `CatalogFinalPrescribedPlanValidator.cs:21` — `CONTROL_FLOW_SPECIALIZED`.

*RUNWAY_PLUS_CORE_PATH (7 sites, entirely separate from CORE_PATH):*
5-11. `PreparationRunwayWeekMaterializer.cs:333-339`, `PreparationRunwayNumericMaterializer.cs:177,183`, `PreparationRunwayCalendarComposer.cs:228`, `PreparationRunwayCoreWeekOnePaceAdapter.cs:12`, `PreparationRunwayPaceMaterializer.cs:107`, `TenKPreparationRunwayFinalInvariantValidator.cs:55-58`, `TenKPreparationRunwayDarkOrchestrator.cs:336-339` — all `CONTROL_FLOW_SPECIALIZED`; item 11 additionally `CONFLICTING_AUTHORITY` (§4.7).

*LONG_HORIZON_PATH (13 sites, entirely separate again):*
12-23. `LongHorizonRollingInitialActivationContracts.cs:104-105,133`, `LongHorizonRollingCheckpointRuntime.cs:367-368`, `LongHorizonPublicPlanService.cs:353-355`, `LongHorizonRollingJitActivationRuntime.cs:466-467`, `LongHorizonCheckpointStateEvaluator.cs:115`, `LongHorizonActivatedCalendarAlignmentValidator.cs:21-22`, `LongHorizonRealCalendarProjectionAdapter.cs:243-244` (multiplies by literal `4` instead of actual DaysPerWeek), `LongHorizonFinalLifecycleValidator.cs:27`, `LongHorizonFullDarkLifecycleHarness.cs:480-482,488-490` (production dark-harness code), `LongHorizonCalendarAssigner.cs:37-49` (fixed 4-entry dictionary — `SCHEMA_CARDINALITY_SPECIALIZED`, structurally cannot express a 2nd KEY), `LongHorizonFullExecutionValidator.cs:39-40`, `LongHorizonStructuralValidator.cs:73-89` — all `CONTROL_FLOW_SPECIALIZED` except the noted dictionary.
24. `NextWindowLoadDecisionPolicy.cs:1-38` — `CONTROL_FLOW_SPECIALIZED`, and the **single deepest hardcode in the entire audit**: not merely a count-check but a self-disclosed **decision matrix calibrated to a specific 4-session denominator's meaning** ("calibrated for the current 4-session pilot... not a general formula"). Changing DaysPerWeek changes what "2 completed" *means* under this policy, not just a threshold constant.

(Noted separately, **not** frequency-related: `LongHorizonGeStructuralSelector.cs:124-125` (`geWeeks/4`, `%4`) and `LongHorizonStructuralValidator.cs:136` (`%4==0`) are 4-*week* mesocycle-block-length arithmetic, unrelated to DaysPerWeek — flagged so they are not miscounted.)

### 4.2 Explicit 3D hard-minimum result

**`3D_NOT_HARD_ARCHITECTURALLY_BLOCKED`**

Every one of the 24 sites in §4.1 is an **exactly-4 equality gate**, not a `< 4` or `>= 4` floor check — each rejects 3D and 5D/6D/7D symmetrically. No site enforces a hard minimum. Seed data already contains working `DaysPerWeek=3` legacy templates. What actually happens today for a hypothetical 3D TEN_K/Intermediate request: it fails the `V1CatalogPilotIdentityPolicy` identity gate (only 4D is a supported identity) and falls through to the legacy placeholder engine, which does exact-match template lookup and would only succeed if a matching 3-day TEN_K/Intermediate template existed (it doesn't). **What would actually be needed:** new catalog data for the 3D combination, plus updating each of the CORE_PATH/RUNWAY_PLUS_CORE_PATH exactly-4 gates to accept the new count. 3D introduces **no** second-KEY complication (frozen 3D row = 1 KEY + 1 EASY + 1 LONG, strictly a subset shape of 4D), so this is the cheapest non-trivial frequency change in the matrix, magnitude-wise — see §4.3 for why it is still `NEEDS_ARCHITECTURE_CHANGE` rather than pure `NEEDS_PARAMETRIZATION` by the strict definitions given.

### 4.3 `DynamicCoreWeekSkeletonOrchestrator` deep audit

**File:** `RuntimeCatalog/Schedule/Materialization/DynamicCoreWeekSkeletonOrchestrator.cs` (198 lines, read in full).

**Inputs:** `DynamicCoreWeekSkeletonOrchestrationContext { Candidate, TargetWeekCount, StartDate, AsOfDate }`. **Outputs:** `{ PhaseAllocation, Skeleton, Validation }`.

**Algorithm (confirmed, not assumed from doc comments):** delegates to `ICatalogPhaseAllocationResolver.Resolve(candidate, targetWeekCount)` (the already-generic dynamic overload), `ICatalogRunLayoutResolver.Resolve(candidate)`, builds `CatalogStageToWeekMaterializationContext` with `DaysPerWeek = runLayout.StructuralRoles.Count` and `RunLayoutSlotRoles = runLayout.StructuralRoles` (a plain `IReadOnlyList<string>`), delegates to `ICatalogStageToWeekMaterializer.Materialize()`, then `IGeneratedCatalogPlanSkeletonValidator.Validate()`. It performs **no** week/slot/date logic itself.

**Q1. Genuinely generic?** Yes — confirmed by full read of every direct dependency, not doc-comment trust. `CatalogRunLayoutResolver` contains **no literal "4"** anywhere. `CatalogStageToWeekMaterializer.BuildSessionSlots` iterates `for i < RunLayoutSlotRoles.Count` and tracks per-role occurrence via a plain counter dictionary, generating keys like `KEY_SESSION_1`/`KEY_SESSION_2` — it would handle two KEY roles without collision, with no uniqueness assumption anywhere. `GeneratedCatalogPlanSkeletonValidator.cs:110` compares against `skeleton.DaysPerWeek`, never a literal.

**Q2/Q3. Can it represent all frozen 3D–7D rows without code modification?** **Yes, at this layer alone**, including the 5D–7D second-KEY shape — this specific orchestrator and its three direct dependencies are genuinely role-cardinality- and role-uniqueness-agnostic. This holds equally for 3D, 4D, 5D, 6D, 7D.

**Q4. Is 4D output behaviorally equivalent to the live path?** **Yes, proven empirically, not just architecturally** — `DynamicCoreWeekSkeletonOrchestrator` and the live `CatalogPlanSkeletonOrchestrator` share the *identical* resolver/materializer/validator instances, and an existing test (`DynamicCoreWeekSkeletonOrchestratorTests.Build_TargetWeekCount12_MatchesExistingFixedWeekOrchestratorExactly`) asserts full field-by-field output equality at `targetWeekCount=12`. The only difference: the dynamic orchestrator sources week count from an explicit parameter rather than `candidate.CoreCycle.DefaultWeeks`, plus one extra defensive cross-check the live path performs and the dynamic contract makes unnecessary.

**Q5. Why zero production call sites?** Established from repo evidence (`PHASE4G_5D_DYNAMIC_CORE_WEEK_SKELETON.md`, present in repo root, plus the orchestrator's own tests) as **deliberate staged/incremental construction**, not incompleteness, accidental safety-gating, or behavioral incompatibility: it is one documented link in a Phase 4G.5x chain (skeleton → workout binding → volume/long-run → pace → calendar → dark E2E), each phase built and self-tested in isolation before any live-wiring decision was made. Proof: an executable test (`DarkReachability_NoProductionCallSiteOutsideTheOneApprovedDarkConsumer`) re-asserts zero non-test call sites as a CI-enforced invariant, with one named approved dark-consumer exception; another test (`DarkReachability_NoDiRegistration`) confirms the interface has zero DI registrations in `RunningApp.Api` — it cannot even be resolved from a live scope today.

**Q6. Downstream unusability despite this layer's genericity — confirmed real.** Even though this orchestrator and its three direct dependencies are generic, the **live 4D pilot's downstream consumers are not**, and would reject anything the orchestrator produced for a non-4D frequency: `CatalogWeekSkeletonCalendarMaterializer` (exactly-4/1-2-1 gates), `V1FourDaySessionVolumeAllocationPolicy` (exactly-4/1-2-1 gate), `DatedGeneratedCatalogPlanSkeletonValidator` (a validator this orchestrator does not itself call, but that a real end-to-end pilot for another frequency would still need to pass).

**Q7/Q8. Per-row skeleton-generation verdict:**

| Row | Skeleton-generation-layer verdict | End-to-end verdict |
|---|---|---|
| 3D | `NEEDS_PARAMETRIZATION` (data only, at this layer) | `NEEDS_ARCHITECTURE_CHANGE`, narrow magnitude — downstream gates (§4.1 items 1–4) need count widening, no new representational capability |
| 4D | `SUPPORTED_ARCHITECTURALLY` (already live) | `SUPPORTED` |
| 5D/6D/7D | `NEEDS_PARAMETRIZATION` at this layer only | `NEEDS_ARCHITECTURE_CHANGE`, broad magnitude — genuine new representational capability required downstream (§4.5) |

**This orchestrator is genuinely production-capable-but-dark at its own layer**, and the feasibility matrix (§6) reflects that fact rather than treating skeleton generation as an unsolved architecture problem for CORE_PATH — the remaining CORE_PATH work for 3D/5D/6D/7D is concentrated entirely in the four downstream consumers listed in §4.1, not in skeleton generation itself. This finding does **not** extend to RUNWAY_PLUS_CORE_PATH's Runway segment or LONG_HORIZON_PATH, both of which have their own, entirely separate skeleton-representation code (§1.4, §4.5).

### 4.4 Calendar / PreferredDays cardinality (3D–7D)

`CatalogWeekSkeletonCalendarMaterializer.cs` (CORE_PATH, 355 lines, full read): `ValidatePreferredDays` hardcodes `Count != 4` — a pilot-scoped gate independent of the request-level `DaysPerWeek` validated elsewhere. Its per-week date-assignment loop (`BuildDatedWeek`) **already generalizes for multiple EASY slots** (uses an incrementing index into a remaining-EASY-dates list) — but assigns the **single** `keySessionDate` value to every KEY-role slot it encounters (`else if (StructuralRole == "KEY_SESSION")`); with two KEY slots, both would silently receive the identical date. This is prevented today only because an upstream count-gate (`keyCount != 1`) blocks the loop from ever running with two KEY slots — **the loop itself, not just a guard, is single-KEY by design** (the upstream `TryAssignKeySessionDates` machinery is built around choosing exactly one `DateOnly` per week).

`LongHorizonCalendarAssigner.cs` (LONG_HORIZON_PATH) is **entirely separate**, by its own doc comment's admission — a fixed 4-entry `Dictionary<string,DayOfWeek>` with named keys `LONG_RUN`/`KEY_SESSION`/`EASY_SUPPORT_1`/`EASY_SUPPORT_2`, structurally incapable of a second KEY by construction (a dictionary literal has exactly one `"KEY_SESSION"` key) — a **stricter** limitation than CORE_PATH's list-based (algorithmically single-KEY, but not schema-incapable) approach.

**KEY-to-KEY spacing policy: confirmed architecturally undefined, reported as open, not invented.** `ScheduleRepairSpacingValidator.cs` explicitly states in its own comment: *"Same-role (KEY-to-KEY, LONG-to-LONG) spacing has no existing canonical rule, so none is invented here."* The only separation constant in the codebase (`MinimumKeySessionToLongRunSeparationDays`) is KEY-to-LONG only. **This is `DecisionRequired` item D1 (§19).**

### 4.5 Second KEY session (5D/6D/7D) — full trace

| Sub-question | Finding | Classification |
|---|---|---|
| A. Skeleton representation | CORE_PATH: confirmed data-only (§4.3). LONG_HORIZON_PATH: `LongHorizonGeStructuralContracts.cs:38-44` defines a **closed C# enum** `LongHorizonGeWeekRole { KeySession, EasySupportA, EasySupportB, LongRun }`, and the descriptor is `IReadOnlyDictionary<LongHorizonGeWeekRole, ...>` populated one entry per named member in `LongHorizonGeStructuralSelector.BuildDescriptor`. **Structurally more rigid than CORE_PATH — a second KEY cannot be expressed even with new data; the enum itself must gain a member.** | CORE_PATH: `SECOND_KEY_DATA_ONLY`. LONG_HORIZON_PATH: `SECOND_KEY_SKELETON_CARDINALITY_CHANGE` |
| B. Workout resolution | `V1CatalogWorkoutRoleBindingPolicy` itself generalizes fine (a stateless per-role switch). The real collision is one layer up: `GeneratedCatalogStageSchedule.Weeks` is **one progression assignment per week number**, not per-slot (doc comment: "for the progression-controlled KEY_SESSION," singular). `CatalogWorkoutBinder.BindAsync` keys purely by week number — two KEY slots in the same week would both resolve to the **identical** progression stage/workout, a silent identical-content collision, not a crash. | `SECOND_KEY_WORKOUT_INVENTORY_DECISION`, dependent on a `SECOND_KEY_SKELETON_CARDINALITY_CHANGE` (the stage-schedule contract needs to become per-slot before two independently-progressed KEY workouts are possible) |
| C. Validation | `DatedGeneratedCatalogPlanSkeletonValidator.cs:147-148` uses `FirstOrDefault(s => role=="KEY_SESSION")` — silently picks only the **first** KEY slot for all downstream spacing checks. **A second KEY slot would be entirely invisible to spacing validation even after any count-gate is loosened — a latent correctness risk, not merely a threshold to relax.** `LongHorizonStructuralValidator.cs:80-93` independently re-asserts `keyCount != 1`. Two integration tests use `.Single(s => role=="KEY_SESSION")`, which would throw loudly (not gracefully) on a second KEY. | `SECOND_KEY_SKELETON_CARDINALITY_CHANGE` for the count gates; the `FirstOrDefault` case is its own correctness fix, distinct from a threshold relaxation |
| D. Calendar | Covered in §4.4 | `SECOND_KEY_CALENDAR_POLICY_DECISION` |
| E. Persistence | `TrainingDay.CatalogSlotRole` is a plain nullable `string`, no enum, no fixed named columns, no uniqueness constraint on role-per-week in any migration. **Two `"KEY_SESSION"` rows in the same week are not prevented by the DB today.** | `SECOND_KEY_DATA_ONLY` — the one layer requiring zero change |
| F. Tests | No test explicitly documents "exactly one KEY" as a named invariant, but two integration tests (`.Single(...)`-based) would fail loudly on a second KEY | (informational) |
| G. Adaptation (flagged, cross-referenced to §5) | `ScheduleRepairCandidateProvider`/`ScheduleRepairSpacingValidator` build repair candidates around a trigger session's role being KEY_SESSION or LONG_RUN as an implicit singleton concept | `SECOND_KEY_ADAPTATION_CARDINALITY_CHANGE` — see §5.1 |

**No second-KEY workout content, spacing policy, or progression semantics is proposed anywhere above** — both the spacing gap (§4.4) and the workout-inventory gap (row B) are reported strictly as open product decisions.

### 4.6 Horizon-path scoping (frequency findings do not collapse)

- **CORE_PATH-only:** `DynamicCoreWeekSkeletonOrchestrator`, `CatalogRunLayoutResolver`, `CatalogStageToWeekMaterializer`, `GeneratedCatalogPlanSkeletonValidator`, `GeneratedCatalogPlanPayloadValidator`, `CatalogWeekSkeletonCalendarMaterializer`, `DatedGeneratedCatalogPlanSkeletonValidator`, `V1FourDaySessionVolumeAllocationPolicy`, `FourDaySessionDistanceAllocationPolicy`, `CatalogFinalPrescribedPlanValidator`, `V1CatalogWorkoutRoleBindingPolicy`, `ProgressionStageScheduleContracts`/`CatalogWorkoutBinder`.
- **RUNWAY_PLUS_CORE_PATH-only (its Runway segment; shares CORE_PATH's findings for its Core segment):** everything under `PreparationRunway*` (§4.1 items 5–11) — an entirely separate, independently-encoded set of 1/2/1/4-role and identity hardcodes.
- **LONG_HORIZON_PATH-only:** the entire `Schedule/LongHorizon/` tree's hardcodes (§4.1 items 12–24), including the structurally stricter closed-enum skeleton representation and the independently-encoded calendar assigner — none of this is shared with CORE_PATH or RUNWAY_PLUS_CORE_PATH.

**Corollary:** generalizing CORE_PATH's skeleton-generation layer alone (largely data-driven already, per §4.3) does **not** by itself unlock any frequency for RUNWAY_PLUS_CORE_PATH's Runway segment or LONG_HORIZON_PATH — each requires its own separate code changes, with LONG_HORIZON_PATH's 5D–7D case being the most structurally demanding of the three families.

### 4.7 Duplicate frequency authority — full classification

- **`SAME_RULE_DUPLICATED`** (could diverge independently): all 24 sites in §4.1 — none is compile-time bound to any other.
- **`DERIVED_INVARIANT`** (safe by construction): `GeneratedCatalogPlanSkeletonValidator.cs:110`, `GeneratedCatalogPlanPayloadValidator.cs:133`.
- **`INTENTIONALLY_LAYERED_VALIDATION`** (deliberate, self-documented re-check of a named single owner): `LivePlanPreviewRouting.cs:235-239` (re-asserts `V1CatalogPilotIdentityPolicy`'s already-computed decision, per its own comment), `PlanServices.IsPreparationRunwayPilotScope` (a routing gate).
- **`CONFLICTING_AUTHORITY`**: `TenKPreparationRunwayDarkOrchestrator.cs:336-339` hand-inlines the 4-field identity comparison instead of calling `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`, even though that policy's own doc comment states it was created specifically to be the single owner replacing this kind of duplication.

**Lockstep-divergence risk:** all 24 `SAME_RULE_DUPLICATED` sites would need individual manual updates for 3D/5D/6D/7D — none is enforced by a shared interface/constant. A developer updating only the two most visible files (`CatalogWeekSkeletonCalendarMaterializer.cs`, `V1FourDaySessionVolumeAllocationPolicy.cs`) could easily miss the LongHorizon and PreparationRunway copies, or `NextWindowLoadDecisionPolicy.cs`'s decision matrix — whose *meaning*, not just its threshold constant, is calibrated to a 4-session denominator, making an incomplete update silently mis-score adherence rather than error cleanly.

---

## 5. Adaptation findings

### 5.1 Role cardinality (`ADAPTATION_CARDINALITY`)

`WindowExecutionSummary` (`Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs`): `EasyExpectedCount`/`EasyCompletedCount` are already **count-based integers** — this representation is **adequate for every frequency row in the frozen model**, including 3D's single EASY and 6D/7D's 3–4 EASY sessions; no information loss there. `LongRunExpected`/`LongRunCompleted` are booleans — **also adequate for every row**, since the frozen 3D–7D model has exactly one LONG_RUN in every row; boolean LONG representation is not a blocker anywhere in this specific frozen target.

**`KeySessionExpected`/`KeySessionCompleted` are booleans — this is the genuine, confirmed information-loss point, and it is 5D/6D/7D-specific, not present at 3D/4D.** A boolean cannot distinguish `0/2 KEY complete`, `1/2 KEY complete`, and `2/2 KEY complete` — three semantically distinct adherence states collapse into `false, false, true` (the AND-reduction pattern already known from prior Rev5 work: today's builder computes `keyCompleted &= isEffectivelyCompleted` across however many KEY occurrences exist, meaning `1/2 KEY complete` would resolve to the *same* `false` as `0/2 KEY complete`, silently losing the distinction between "attempted and missed one" and "attempted nothing"). **Classification: `ADAPTATION_CARDINALITY`, scoped specifically to 5D/6D/7D (LONG_HORIZON_PATH only, per §5.4).**

3D's shape (1 KEY, 1 EASY, 1 LONG) creates **no** cardinality mismatch — every role count in 3D is within what the current boolean/count representation already handles correctly (Easy=1 is representable by the count fields; Key=1/Long=1 are exactly what the booleans already assume).

### 5.2 Load-decision calibration (`ADAPTATION_CALIBRATION`)

`NextWindowLoadDecisionPolicy` (self-disclosed, `NextWindowLoadDecisionPolicy.cs:1-38`): calibrated to a raw `EffectiveCompletedCount` against a 4-total-session denominator (`0-1→Reduce, 2→Maintain, 3→[OnlyEasyMissing check]→Progress-or-Maintain, ≥4→Progress`), with the "only Easy missing" sub-check implicitly assuming exactly 1 KEY + 1 LONG + N EASY where N≥1.

For each target frequency, would the exact current policy preserve intended semantics:

- **3D (3 total sessions):** **No.** The `≥4` branch becomes structurally **unreachable** (3D can never produce more than 3 completions). The "3 completed, only Easy missing" branch — which at 4D distinctly means "everything except the 1 Easy is done, with 1 more session outstanding" — at 3D collapses to mean "**every** session is complete" (since 3D has only 3 total sessions), because there is no 4th session left to distinguish "all done" from "all-but-Easy done." The two states that are semantically distinct at 4D collapse into the same branch at 3D.
- **4D:** exact current calibration (native case, correct by definition).
- **5D/6D/7D (5–7 total sessions, 2 KEY):** **No.** The raw-count thresholds (0-1/2/3/≥4) were calibrated for a 4-total denominator; at 5–7 total sessions, "2 completed" no longer represents the same fraction of the week, and the KEY-role-aware sub-branch's "only Easy missing" logic cannot correctly express partial 2-KEY completion at all (compounds with §5.1's cardinality gap — the policy reads `KeySessionCompleted`, which is itself ambiguous for 2 KEY slots before the calibration question is even reached).

**Classification: `ADAPTATION_CALIBRATION_REQUIRED` for 3D, 5D, 6D, and 7D — not merely 5D–7D.** Only 4D is `CURRENT_CALIBRATION_FREQUENCY_AGNOSTIC` (trivially, since it *is* the calibration target). This is explicitly kept separate from §5.1's cardinality finding: cardinality is a **representation** problem (can the type express the state at all), calibration is a **decision-semantics** problem (does the threshold logic mean the same thing at a different denominator) — they compound at 5D–7D but calibration alone already breaks at 3D where cardinality does not.

### 5.3 `WeeklyWindowPartitioner` — frequency-agnostic confirmation

Verified: `WeeklyWindowPartitioner.PartitionByStructuralWeekLineage` groups sessions purely by real structural-week identity (`WeekStateId`/`GlobalWeek` — persisted entity identity), independent of how many sessions exist per week or what roles they carry. No contradictory evidence found. **`FREQUENCY_AGNOSTIC_CONFIRMED`** — this component is not a blocker for any frequency row and required no further investigation beyond this confirmation, per the phase's own instruction.

### 5.4 Adaptation coverage by horizon family

| Horizon family | Current adaptation support | Runtime entry point | Adaptation components reached | Frequency-sensitive assumptions | Pre-existing gap? | Evidence |
|---|---|---|---|---|---|---|
| CORE_PATH (8–14w) | `CORE_ADAPTATION_NONE` | `POST /api/v1/training-days/{id}/not-today-decisions` → `NotTodayDecisionsController` → `QueryAndMutationServices.ConfirmNotTodayDecisionAsync` | **None** — calls `IAdaptationEngine.EvaluateNotTodayAsync`, whose only registered implementation, `PlaceholderAdaptationEngine`, is a hardcoded stub always returning `Action=NoChange, PlanAdapted=false` | N/A (no logic runs) | **Yes** | `QueryAndMutationServices.cs:387-536`; `Adaptation/PlaceholderAdaptationEngine.cs:12-31` (doc comment: "Phase 1 placeholder... Replace this with the real engine in a future phase"); `Program.cs:120` DI registration; `TrainingPlan.cs:26` default `ScheduleStrategy=StaticComplete` |
| RUNWAY_PLUS_CORE_PATH (15–20w) | `RUNWAY_CORE_ADAPTATION_NONE` | Same generic endpoints as CORE_PATH — confirmed independently, not assumed by inheritance | Same `PlaceholderAdaptationEngine` stub | N/A | **Yes** | `PreparationRunwayPersistablePlanMapper.cs:16,34,125,199` (explicit doc comment confirming Runway+Core plans persist through the identical `CatalogPlanConfirmationService`/`TrainingPlan`/`TrainingDay` contract as Core); zero Runway-specific `ScheduleStrategy=` assignment found anywhere in the repository |
| LONG_HORIZON_PATH (21+w) | `LONG_HORIZON_ADAPTATION_FULL` (for its calibrated 1K+2E+1L shape only) | `POST /api/v1/training-days/rolling/{id}/not-today` and `/complete` → `LongHorizonRollingSessionMutationService` | `ScheduleRepairPolicy.Evaluate` (via `ScheduleRepairRuntimeOrchestrator.RunAsync`), `WindowExecutionSummaryBuilder.Build`, `WindowCheckpointEvidenceMapper.ToEvidence`, `WeeklyWindowPartitioner.PartitionByStructuralWeekLineage`, `NextWindowLoadDecisionPolicy.Evaluate`, `WeeklyLoadDecisionAggregator.AggregateWorstWeekWins` — all confirmed reached via real call-graph tracing, not name inference | Self-disclosed calibration to "exactly one real structural week (1 KEY + 2 EASY + 1 LONG)" (`LongHorizonRollingWindowActivationService.cs` doc comment) | No — this is the system's home | `LongHorizonRollingSessionMutationService.cs:174-192`; `ScheduleRepairRuntimeOrchestrator.cs:66-81`; `LongHorizonRollingWindowActivationService.cs:95-135` |

**Direct answers:**
1. **Is Adaptation V1 currently LongHorizon-only?** **Yes** — confirmed by call-graph tracing. No CORE_PATH or RUNWAY_PLUS_CORE_PATH code references `ScheduleRepairPolicy`, `WindowExecutionSummaryBuilder`, `WindowCheckpointEvidenceMapper`, `WeeklyWindowPartitioner`, or `NextWindowLoadDecisionPolicy` anywhere.
2. **Does Core have real adaptation behavior?** No — a `NotTodayDecision` is recorded (real audit trail) but the only registered `IAdaptationEngine` is a hardcoded no-op stub.
3. **Does Runway+Core have real adaptation behavior?** No — independently confirmed, shares Core's exact persistence contract and the same stub.
4. **Which horizon families consume `WindowExecutionSummary`?** LONG_HORIZON_PATH only.
5. **Which horizon families consume `NextWindowLoadDecisionPolicy`?** LONG_HORIZON_PATH only.
6. **Which missing behavior is `PRE_EXISTING_HORIZON_ADAPTATION_GAP` rather than a generalization blocker?** CORE_PATH's and RUNWAY_PLUS_CORE_PATH's total absence of adaptation — evidenced directly by `PlaceholderAdaptationEngine`'s own doc comment predating and being unrelated to any Frequency/Level generalization concern. **This gap must not be characterized anywhere in this audit as "Frequency generalization broke adaptation" — it does not exist today for the current, already-supported Intermediate/4D combination on those two paths either.**

---

## 6. Full 15-cell feasibility matrix

**Reading key:** Level axis contributes a uniform floor across every frequency column (§3.5); Frequency axis contributes per-horizon verdicts that are identical across all three Levels (§4, since Level does not change any frequency-path finding). `OVERALL` takes the least-favorable (most-architectural) of the Level floor and the worst horizon-family Frequency verdict, per horizon-sub-classification rules in §6.1–§6.3, with `PRE_EXISTING_HORIZON_ADAPTATION_GAP` displayed separately and never itself elevating a cell's classification (per binding instruction).

### 6.1 Frequency-axis verdicts by horizon family (shared across all 3 Levels)

| Frequency | CORE_PATH | RUNWAY_PLUS_CORE_PATH | LONG_HORIZON_PATH |
|---|---|---|---|
| **3D** | `NEEDS_ARCHITECTURE_CHANGE` (narrow — reason: `SKELETON_CARDINALITY`+`CALENDAR_CARDINALITY`; skeleton-generation layer itself already `NEEDS_PARAMETRIZATION`-only via `DynamicCoreWeekSkeletonOrchestrator`, but 4 downstream exactly-4 gates require code edits) | `NEEDS_ARCHITECTURE_CHANGE` (narrow, same reason, **duplicated across 2 independently-encoded subsystems** — reason also includes `HORIZON_PATH_DIVERGENCE`) | `NEEDS_ARCHITECTURE_CHANGE` (reason: `SKELETON_CARDINALITY` [closed enum needs conditional handling of an absent EasySupportB], `CALENDAR_CARDINALITY` [fixed dict], `ADAPTATION_CALIBRATION` [3-total-session denominator breaks the ≥4/OnlyEasyMissing branch collision, §5.2]) |
| **4D** | `SUPPORTED` | `SUPPORTED` | `SUPPORTED` |
| **5D** | `NEEDS_ARCHITECTURE_CHANGE` (broad — reason: `SKELETON_CARDINALITY`, `CALENDAR_CARDINALITY`, `WORKOUT_INVENTORY` [per-week not per-slot progression schedule], `MULTIPLE`) | `NEEDS_ARCHITECTURE_CHANGE` (broad, same reasons, duplicated, `HORIZON_PATH_DIVERGENCE`) | `NEEDS_ARCHITECTURE_CHANGE` (broad — reason: `SKELETON_CARDINALITY` [enum needs a new member — more invasive than CORE_PATH's list-based approach], `CALENDAR_CARDINALITY` [structurally incapable], `ADAPTATION_CARDINALITY` [§5.1, KEY boolean can't represent 0/1/2 of 2], `ADAPTATION_CALIBRATION` [§5.2, denominator shift], `WORKOUT_INVENTORY`, `MULTIPLE`) |
| **6D** | Same as 5D (plus a 3rd EASY, already representable by the count-based EASY fields — no additional blocker beyond 5D's) | Same as 5D | Same as 5D |
| **7D** | Same as 5D (plus a 4th EASY, same non-blocking note) | Same as 5D | Same as 5D |

### 6.2 Level-axis floor (applies identically to every frequency column)

- **Intermediate:** no Level-driven floor — the frequency-axis verdict above is the cell's determinant.
- **Beginner / Advanced:** floor of `NEEDS_ARCHITECTURE_CHANGE` (reason: `LEVEL_PRESCRIPTION`, `ROUTING_IDENTITY`) applies **even at 4D**, since no catalog data, no identity-gate entry, and no level-keyed `VolumeSafetyPolicy` selection mechanism exists for either value today (§3.5). This floor is uniform across CORE_PATH/RUNWAY_PLUS_CORE_PATH/LONG_HORIZON_PATH (§3.4 — Level affects all three identically, as a routing gate only).

### 6.3 Full 15-cell matrix

| | 3D | 4D | 5D | 6D | 7D |
|---|---|---|---|---|---|
| **Beginner** | `NEEDS_ARCHITECTURE_CHANGE`<br>reason: `LEVEL_PRESCRIPTION`+`ROUTING_IDENTITY`+`SKELETON_CARDINALITY`+`CALENDAR_CARDINALITY`+`MULTIPLE`<br>*(sub-class: CORE/RUNWAY = narrow generation gap + Level floor; LONG_HORIZON = same + `ADAPTATION_CALIBRATION`)* | `NEEDS_ARCHITECTURE_CHANGE`<br>reason: `LEVEL_PRESCRIPTION`+`ROUTING_IDENTITY`<br>*(generation-side architecture already sufficient for 4D itself — this cell is blocked purely by the Level floor, not by any frequency finding)* | `NEEDS_ARCHITECTURE_CHANGE`<br>reason: `LEVEL_PRESCRIPTION`+`ROUTING_IDENTITY`+`SKELETON_CARDINALITY`+`CALENDAR_CARDINALITY`+`ADAPTATION_CARDINALITY`+`ADAPTATION_CALIBRATION`+`WORKOUT_INVENTORY`+`MULTIPLE` | Same as 5D | Same as 5D |
| **Intermediate** | `NEEDS_ARCHITECTURE_CHANGE`<br>reason: `SKELETON_CARDINALITY`+`CALENDAR_CARDINALITY`(+`ADAPTATION_CALIBRATION` for LONG_HORIZON only)<br>*(narrow magnitude; see §6.1 for horizon sub-classification)* | `SUPPORTED`<br>*(the live pilot, all three horizon families)* | `NEEDS_ARCHITECTURE_CHANGE`<br>reason: `SKELETON_CARDINALITY`+`CALENDAR_CARDINALITY`+`WORKOUT_INVENTORY`+`ADAPTATION_CARDINALITY`+`ADAPTATION_CALIBRATION`(LONG_HORIZON)+`MULTIPLE`<br>*(broad magnitude; see §6.1)* | Same as 5D | Same as 5D |
| **Advanced** | Same shape as Beginner×3D | `NEEDS_ARCHITECTURE_CHANGE`<br>reason: `LEVEL_PRESCRIPTION`+`ROUTING_IDENTITY`<br>*(Level-floor-only, same as Beginner×4D)* | Same shape as Beginner×5D | Same shape as Beginner×5D | Same shape as Beginner×5D |

**Architecture capability vs. current product/API reachability (explicit distinction, per instruction):** every cell above describes architecture-layer feasibility. **Current product/API reachability is narrower than this for every non-Intermediate×4D cell**: none of the 14 non-SUPPORTED cells are reachable through the live API today regardless of architecture readiness, because the identity gates (§1.2) reject them before any generation logic runs — this is true even for cells where the underlying generation architecture is comparatively close (e.g., Intermediate×3D's CORE_PATH skeleton layer is `NEEDS_PARAMETRIZATION`-ready per §4.3, but the request would never reach it due to the identity gate). No cell's `NEEDS_ARCHITECTURE_CHANGE` classification above is driven by "the catalog identity string doesn't exist" alone — each cell's reason codes trace to a genuine representational/control-flow/schema/policy fact, per the binding instruction not to classify a cell as architectural merely for missing identity.

**`PRE_EXISTING_HORIZON_ADAPTATION_GAP` note (displayed separately, per instruction, never itself elevating a cell):** for every cell's CORE_PATH and RUNWAY_PLUS_CORE_PATH sub-classification, adaptation is and remains a no-op stub (`PlaceholderAdaptationEngine`) regardless of Level/Frequency — this is true today even for the live, `SUPPORTED` Intermediate×4D cell on those two horizon families. This fact is not counted toward any cell's `NEEDS_ARCHITECTURE_CHANGE` classification above; only LONG_HORIZON_PATH's real, calibrated adaptation chain contributes `ADAPTATION_CARDINALITY`/`ADAPTATION_CALIBRATION` reason codes, and only where the frequency-driven evidence in §5.1/§5.2 genuinely supports it (5D–7D for cardinality; 3D/5D/6D/7D for calibration).

**No cell is `UNKNOWN` or `INFEASIBLE_AS_SPECIFIED`.** No repository evidence contradicted the frozen target model at the level of "this is impossible as specified" — every gap found is a bounded, describable architecture-change requirement, not a hard contradiction.

---

## 7. Cheapest-to-unlock ranking

Ranked strictly on demonstrated repository delta (smallest evidenced change-surface first), **not** an implementation plan or wave proposal:

1. **Intermediate × 3D, CORE_PATH only.** Skeleton-generation layer is already `NEEDS_PARAMETRIZATION`-ready (`DynamicCoreWeekSkeletonOrchestrator` + its 3 generic dependencies, §4.3); no second-KEY complexity; only 4 downstream exactly-4 gates (§4.1 items 1–4) need count-widening plus new 3D catalog data. This is the single smallest evidenced change-surface in the entire matrix.
2. **Intermediate × 3D, RUNWAY_PLUS_CORE_PATH.** Same magnitude as #1 for its shared Core segment, plus 7 additional, independently-encoded Runway-segment gates (§4.1 items 5–11) that must be separately updated — larger surface than #1 purely due to `HORIZON_PATH_DIVERGENCE`, not any new representational problem.
3. **Beginner/Advanced × 4D (any horizon family).** No frequency-driven generation-side work at all (4D is already fully architected) — the entire gap is the Level floor (§3.5, §6.2): new catalog data, new identity-gate table entries, and one new level-keyed `VolumeSafetyPolicy` selector. Evidence supports this as a bounded, well-understood, narrow architecture addition (a selection mechanism, not a new algorithm), distinct in kind from the frequency-driven skeleton/calendar/adaptation work below.
4. **Intermediate × 3D, LONG_HORIZON_PATH.** Requires touching the closed-enum skeleton representation (a real schema change, even if the change itself — conditionally omitting `EasySupportB` — is small), the fixed-dictionary calendar assigner, and reconsidering `NextWindowLoadDecisionPolicy`'s branch collision at a 3-session denominator (§5.2) — a materially larger surface than the CORE_PATH/RUNWAY_PLUS_CORE_PATH 3D cells because LONG_HORIZON_PATH's representations are independently, more rigidly encoded.
5. **Any Level × 5D/6D/7D, CORE_PATH/RUNWAY_PLUS_CORE_PATH.** The second-KEY problem (§4.5) requires genuine new representational capability (per-slot progression scheduling, a KEY-KEY spacing product decision, the `FirstOrDefault` correctness fix) beyond what any of the above require — a distinctly larger, qualitatively different change than count-widening.
6. **Any Level × 5D/6D/7D, LONG_HORIZON_PATH.** The most architecturally demanding cells in the matrix: closed-enum schema change for a wholly new role, structurally-incapable calendar assigner, genuine `ADAPTATION_CARDINALITY` information-loss fix, and `ADAPTATION_CALIBRATION` reconsideration for a 2-KEY/5–7-session denominator, layered on top of the same second-KEY workout-inventory and spacing-policy open decisions as CORE_PATH.

Repository evidence does **not** support a strict universal ordering like "3D always precedes 5D" or "Beginner always precedes Advanced" beyond what's shown above — Beginner and Advanced are evidenced as symmetric (identical gap shape, §3.2–§3.5), and the 3D-before-5D/6D/7D ordering above is a direct consequence of the second-KEY architectural delta (§4.5), not an assumed convention.

---

## 8. DecisionRequired inventory

| ID | Question | Why repository evidence cannot decide it | Blocks | External coaching evidence needed? |
|---|---|---|---|---|
| D1 | What is the spacing policy between two KEY_SESSION slots in the same structural week (5D/6D/7D)? | `ScheduleRepairSpacingValidator.cs` explicitly documents no existing canonical rule for same-role spacing — this is a genuine absence, not a hidden rule to discover | All 5D/6D/7D cells, all Levels, all horizon families | Likely yes (training-load/recovery spacing is a domain question) |
| D2 | What workout content should the second KEY_SESSION use (same as the first, or a distinct workout family)? | No repository artifact defines a second-KEY workout identity; `ProgressionStageScheduleContracts` is architected around exactly one progression-controlled KEY per week | All 5D/6D/7D cells | Likely yes |
| D3 | Should `WindowExecutionSummary`'s KEY representation move to a count pair (mirroring the existing EASY pattern) or some other structure, and what is the correct 0/1/2-of-2 semantic for `NextWindowLoadDecisionPolicy`? | This is a genuine domain-decision question about adherence semantics (is 1-of-2 KEY "half credit," "Reduce," something else?), not resolvable from code alone | All 5D/6D/7D `LONG_HORIZON_PATH` cells | Likely yes |
| D4 | Should `NextWindowLoadDecisionPolicy`'s raw-count thresholds be replaced with a frequency-normalized (percentage or role-aware) formula, and if so, what are the new thresholds? | Prior repository work (`NextWindowLoadDecisionPolicy`'s own doc comment) already discloses this as a deliberate, not-yet-generalized PRODUCT DEFAULT calibration — the replacement formula is an explicit future product decision, not something this audit should infer | All non-4D `LONG_HORIZON_PATH` cells (3D, 5D–7D) | Possibly — depends on whether the answer is purely mechanical (e.g., simple normalization) or requires new coaching judgment |
| D5 | What are the actual `LevelModifier` numeric values (`StartingWeeklyVolume`, `PeakWeeklyVolume`, `ProgressionRate`, etc.) for Beginner and Advanced? | Explicitly out of this audit's scope ("do not propose numeric values") — this is a pure domain/coaching-content question | All Beginner/Advanced cells | Yes |
| D6 | Should `DynamicCoreWeekSkeletonOrchestrator` eventually **replace** the fixed 4D path, or **coexist** alongside it (with the fixed path retained for the live pilot and the dynamic path used only for new frequencies)? | This is a migration-strategy/product-risk decision, not resolvable from the orchestrator's own behavior (which is proven behaviorally equivalent at 4D, §4.3 Q4) | All future implementation-phase sequencing decisions | No — purely an engineering/product judgment call, but not this audit's to make |
| D7 | What is the intended horizon-family activation sequencing for generalization work (which family gets 3D/5D–7D support first)? | Flagged as `HORIZON_PATH_DIVERGENCE` throughout this audit (§1.5) but the actual sequencing choice is a product/roadmap decision outside repository evidence | Cross-cutting, all cells | No |
| D8 | Should the ~24 duplicated frequency-identity/cardinality gates (§4.1, §4.7) be consolidated into shared authorities before or during frequency generalization, or updated independently in place? | A refactoring-strategy question explicitly out of this audit's scope ("do not propose refactoring") | Cross-cutting, all cells (affects the practical cost of any generalization work, not its feasibility) | No |

---

## 9. Files inspected

Full call-graph tracing performed across (non-exhaustive list of the most load-bearing files; every material claim in §§1–8 traces to a file actually read, not inferred from names): `RunningApp.Application/Common/RaceHorizonPolicy.cs`; `RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs`; `RuntimeCatalog/Schedule/LongHorizon/LongHorizonCompositionResolver.cs`; `Services/PlanServices.cs`; `RuntimeCatalog/Schedule/PreparationRunwayOrchestration/TenKPreparationRunwayDarkOrchestrator.cs`; `RuntimeCatalog/Schedule/LongHorizon/RollingActivation/PublicPreview/LongHorizonPublicPlanService.cs`; `RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`, `LivePlanPreviewRouting.cs`, `LivePlanPreviewRoutingService.cs`, `CatalogPlanConfirmationService.cs`, `PreparationRunwayPersistablePlanMapper.cs`; `RuntimeCatalog/Schedule/Materialization/CatalogPhaseAllocationResolver.cs`, `CatalogRunLayoutSlots.cs`, `CatalogStageToWeekMaterializer.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs`, `DatedGeneratedCatalogPlanSkeletonValidator.cs`, `GeneratedCatalogPlanSkeletonValidator.cs`, `GeneratedCatalogPlanPayloadValidator.cs`, `CatalogFinalPrescribedPlanValidator.cs`, `DynamicCoreWeekSkeletonOrchestrator.cs` (full read) and its tests `DynamicCoreWeekSkeletonOrchestratorTests.cs` (full read); `RuntimeCatalog/PlanCatalogBundleLoader.cs`; `RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` (full read), `VolumeSafetyPolicy.cs`, `CatalogPeakVolumeBandLoader.cs`; `RuntimeCatalog/Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs`, `FourDaySessionDistanceAllocationPolicy.cs`, `CatalogSessionPrescriptionPlanner.cs`; `RuntimeCatalog/Schedule/Binding/V1CatalogWorkoutRoleBindingPolicy.cs`, `CatalogWorkoutBinder.cs`, `ProgressionStageScheduleContracts.cs`, `ProgressionStageAllocator.cs`; `RuntimeCatalog/Schedule/PreparationRunway*` (all files under this tree, §4.1 items 5–11); `RuntimeCatalog/Schedule/LongHorizon/LongHorizonCalendarAssigner.cs`, `LongHorizonStructuralValidator.cs`, `LongHorizonGeStructuralContracts.cs`, `LongHorizonGeStructuralSelector.cs`, `LongHorizonRealCalendarProjectionAdapter.cs`; `RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonRollingCheckpointRuntime.cs`, `LongHorizonRollingInitialActivationContracts.cs`, `LongHorizonRollingJitActivationRuntime.cs`, `LongHorizonCheckpointStateEvaluator.cs`, `LongHorizonActivatedCalendarAlignmentValidator.cs`, `LongHorizonFinalLifecycleValidator.cs`, `LongHorizonFullExecutionValidator.cs`; `LifecycleValidation/LongHorizonFullDarkLifecycleHarness.cs`; `RuntimeCatalog/Schedule/LongHorizon/Adaptation/AdaptationDomainContracts.cs`, `WindowExecutionSummaryBuilder.cs`, `NextWindowLoadDecisionPolicy.cs`, `WeeklyLoadDecisionAggregation.cs` (`WeeklyWindowPartitioner`/`WeeklyLoadDecisionAggregator`), `ScheduleRepairSpacingValidator.cs`, `ScheduleRepairCandidateProvider.cs`, `ScheduleRepairRuntimeOrchestrator.cs`; `Services/LongHorizonRollingSessionMutationService.cs`, `LongHorizonRollingWindowActivationService.cs`, `QueryAndMutationServices.cs`; `Adaptation/IAdaptationEngine.cs`, `PlaceholderAdaptationEngine.cs`; `RunningApp.Api/Controllers/TrainingDaysController.cs`, `PlansController.cs`, `Program.cs`; `RunningApp.Domain/Enums/RunningBackground.cs`, `PlanScheduleStrategy.cs`; `RunningApp.Domain/Entities/TrainingPlan.cs`, `TrainingDay.cs`; `plan-catalog/catalog/templates/ten-k-master.v6.json` and sibling `combinations/`, `level-modifiers/`, `layouts/` artifacts; repo-root `PHASE4G_5D_DYNAMIC_CORE_WEEK_SKELETON.md`. Plus GEN.0's own file inventory (`PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md` §11), treated as established baseline per §0.

## 10. Explicit out-of-scope confirmation

**2D, Expert, CrossTrainDay, and DoubleSessionDay were NOT investigated as V1 feasibility targets anywhere in this audit.** No repository evidence about them was incidentally surfaced or reported beyond what the phase prompt itself already stated (the `RunningBackground` enum's fourth member, `Experienced`, was noted only as a ground-truth fact about the enum's shape in §3.1, not investigated as a target).

---

## Final classification

```
10K_GENERALIZATION_ARCHITECTURE_AUDIT_READY_FOR_DECISION_PHASE
```

All PART 1 (B1–B9) requirements addressed; Core/Runway+Core/LongHorizon differences preserved throughout, never collapsed; the phase-objective-vs-prescription question answered from evidence (§2.4); Intermediate semantics mapped to concrete Level-generalization requirements (§3); every meaningful fixed-4D assumption inventoried (24 sites, §4.1); 3D hard-minimum feasibility explicitly resolved (`3D_NOT_HARD_ARCHITECTURALLY_BLOCKED`, §4.2); `DynamicCoreWeekSkeletonOrchestrator` deeply inspected including its zero-call-site cause (§4.3); second-KEY support traced through all six required layers (§4.5); `ADAPTATION_CARDINALITY` explicitly separated from `ADAPTATION_CALIBRATION` (§5.1/§5.2); `WeeklyWindowPartitioner` explicitly resolved as frequency-agnostic (§5.3); Core/Runway+Core/LongHorizon adaptation coverage explicitly resolved with `PRE_EXISTING_HORIZON_ADAPTATION_GAP` distinguished from new generalization work (§5.4); the full 3×5 matrix contains all 15 cells, none guessed, none `UNKNOWN` (§6); DecisionRequired questions identified but not resolved (§8); no production code written; no target-model decision silently changed; 2D/Expert/CrossTrain/Doubles remained out of scope (§10).
