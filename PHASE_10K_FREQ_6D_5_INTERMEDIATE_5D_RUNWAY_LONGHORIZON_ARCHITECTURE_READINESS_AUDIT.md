# PHASE 10K-FREQ.6D.5 — Intermediate×5D Preparation Runway & Long-Horizon Architecture / Activation Readiness Audit

**Type:** ARCHITECTURE + READINESS AUDIT (evidence only — no production code touched)
**Parent phase:** FREQ.6D.4D.5G
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. Everything below is re-derived from the current repository state, largely via three parallel deep-research passes over the real source tree (not memory).

---

## 1. Preflight

- `git rev-parse HEAD` at start: `6d94415e5effc5c603ced3e9f57dbba27ae1a095`.
- `git branch --show-current`: `main`.
- `git status --short` (non-build-output): `m baseline_tmp`, `M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}` — pre-existing, unrelated, preserved untouched.
- `git rev-list --left-right --count origin/main...HEAD`: `0  18`.
- `git diff --check`: clean.
- Commits `d6ee639`/`a6c3a46`/`6d94415` confirmed reachable from `HEAD`.
- `PHASE_LEDGER.md` row 84 re-read: `FREQ.6D.4D` confirmed closed with final classification `FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED`.
- `TEN_K__5D__INTERMEDIATE` confirmed publicly active for the 8-14 week Core path (`V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency` includes `(Intermediate, 5)`, `ResolveCandidate` returns `FiveDayCandidateKey`/`Version`).

**Phase ID.** `MASTER_ROADMAP.md` §5 ("Next Concrete Block") lists exactly one pre-existing, not-yet-created ID beyond the now-closed `FREQ.6D.4` chain: `FREQ.6D.5` ("persistence/round-trip/full-regression closure"). No report exists for it. Its original one-line description predates the actual `FREQ.6D.4D.1`-`.5G` sub-chain that has since fully superseded and closed that scope, so it does not literally describe this audit's content — but per this phase's own instruction ("use the exact next phase ID assigned by MASTER_ROADMAP; do not invent one if repo already assigns it"), `FREQ.6D.5` is used, since it is the only concretely pre-listed next ID and no other candidate ID exists in the roadmap for this scope. `FREQ.7`/`FREQ.8` remain listed after it, unused, per the pre-existing sequence.

---

## 2. Current public support state

| Horizon mode | Weeks | Intermediate×5D current result |
|---|---|---|
| Core | 8-14 | `PUBLICLY_ACTIVE` (`FREQ.6D.4D.5G`) |
| Preparation-Runway-Plus-Core | 15-20 | `PlanHorizonCompositionRequiredException` (HTTP 422, `PLAN_HORIZON_COMPOSITION_REQUIRED`) — identical outcome to every other non-4D-pilot candidate at this horizon; the Runway pilot gate is never reached |
| LongHorizon | 21-52 | Same `PlanHorizonCompositionRequiredException` — no LongHorizon composition path exists in the public preview pipeline for any candidate; only the Rolling-Activation continuation service (a separate, non-preview entry point) reaches LongHorizon logic, and it independently hardcodes 4D |

---

## 3. Core reference architecture (baseline for comparison)

Core Intermediate×5D's proven chain, used throughout this audit as the comparison baseline: `RUN_LAYOUT_5D` → dual `LaneOrdinal` (0/1) → per-lane `ProgressionStageKey` → exact `PrescriptionProfileKey`/`Version` → published-bundle `ExecutionPrescriptionIndex.ResolveExact` → `CatalogWeekSkeletonCalendarMaterializer` (generalized multi-KEY spacing) → `TrainingDay` persistence (exact profile lineage columns) → `NextWindowLoadDecisionPolicy`'s 5-session severity table → `V1CatalogPublicWorkoutTypeMappingPolicy` → public API. Every section below asks: does Runway/LongHorizon consume this same generic chain, or bypass/reconstruct/hardcode a narrower one?

---

## 4. Preparation Runway pipeline trace (`PREPARATION_RUNWAY_PIPELINE_TRACE`)

```
POST /api/v1/plans/generate-preview/race
  → PlanServices.GeneratePreviewAsync (backend/RunningApp.Application/Services/PlanServices.cs:119)
    → RaceHorizonPolicy.Decide/Classify → CoreHorizonMode.PreparationRunwayPlusCore → CompositionRequired
    → PlanServices.IsPreparationRunwayPilotScope(request)                    [PlanServices.cs:95-99] — hardcoded gate, see §6
    → (if 15-20wk AND pilot-scope AND activation-enabled) PlanServices.GeneratePreparationRunwayPreviewAsync [PlanServices.cs:405-457]
      → ICatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync [CatalogPreviewGenerator.cs:410-524]
        → _gate.LoadForPublicPreviewAsync(V1CatalogPilotIdentityPolicy.CandidateKey/Version)   [CatalogPreviewGenerator.cs:414-415] — hardcoded 4D load, see §6
        → TenKPreparationRunwayDarkOrchestratorFactory.Create(...).OrchestrateAsync(...)        [CatalogPreviewGenerator.cs:491-492]
          → TenKPreparationRunwayDarkOrchestrator (12-stage pipeline):
            Horizon → ReadinessProfile → AllocationPolicy → BlockAllocation
              [PreparationRunwayBlockAllocationEngine, TenKPreparationRunwayAllocationPolicyFactory]
            → ProgressionLoading → WorkoutBinding
              [PreparationRunwayBlockProgressionCatalogReader, PreparationRunwayBlockWorkoutBindingEngine, PreparationRunwayBlockWorkoutReferenceValidator]
            → StructuralMaterialization [PreparationRunwayWeekMaterializer, TenKPreparationRunwayWeekMaterializationPolicyFactory] — hardcoded RUN_LAYOUT_4D, see §7
            → CoreGeneration [TenKPreparationRunwayCoreGenerator → generic IDynamicCoreCalendarMaterializationOrchestrator]
            → NumericMaterialization [PreparationRunwayNumericMaterializer, TenKPreparationRunwayNumericPolicyFactory] — see §8
            → CalendarComposition [PreparationRunwayCalendarComposer]
            → PaceMaterialization [PreparationRunwayPaceMaterializer, TenKPreparationRunwayPacePolicyFactory] — Legacy/EffortOnly only, see §15
            → TenKPreparationRunwayFinalInvariantValidator.ValidateRequest — hardcoded 4D/4-slot invariants, see §6/§7
        → PreparationRunwayPublicPreviewMapper.MapCombinedWeeks [public preview mapping]
        → (if confirmationEnabled) PreparationRunwayPersistablePlanMapper.Map [persistence]
      → CatalogPlanConfirmationService [confirmation-time persistence]
```

---

## 5. LongHorizon pipeline trace (`LONG_HORIZON_PIPELINE_TRACE`)

```
LongHorizonRollingWindowActivationService (backend/RunningApp.Application/Services/LongHorizonRollingWindowActivationService.cs)
  → LongHorizonPublicPlanService (PublicPreview/) — public entry: preview + confirm, hardcoded CandidateKey/Version = TEN_K__4D__INTERMEDIATE v10 [LongHorizonPublicPlanService.cs:81-82]
    → LongHorizonCompositionResolver.Resolve → LongHorizonCompositionDecision (GE weeks + 8wk Runway + 12wk Core = 21-52 total)
    → LongHorizonRollingInitialActivationRuntime — first-window activation [InputValidator hardcodes DaysPerWeek==4, LongHorizonRollingInitialActivationContracts.cs:104-111]
    → LongHorizonRollingJitActivationRuntime (Phase 4K.8) — window boundaries/direction guard, ≤4-week rolling cap
      → LongHorizonRollingJitCompositionOrchestrator (Phase 4K.8C)
        → LongHorizonRollingCoreGenerationInputAdapter — maps evidence → GeneratePreviewRequest (DaysPerWeek=4 hardcoded, line 43/68)
        → (when needsCoreGeneration) TenKPreparationRunwayDarkOrchestratorFactory.Create(...) — the SAME orchestrator Runway uses, not CatalogPreviewGenerator, see §19/§28
        → BuildBoundedCoreSelection — flattens real CatalogPrescribedSession into LongHorizonSessionPrescriptionReference, discarding LaneOrdinal/ProgressionStageKey/PrescriptionProfileKey, see §25
    → LongHorizonRollingCheckpointRuntime — reassessment; ValidateInput hardcodes DaysPerWeek==4 [line 368-371]
    → LongHorizonCheckpointEvidenceAggregator / LongHorizonCheckpointStateEvaluator
    → NextWindowLoadDecisionPolicy.Evaluate (per real structural week, via WeeklyWindowPartitioner) — already frequency-generic, see §13
    → ScheduleRepairRuntimeOrchestrator / ScheduleRepairSpacingValidator — week/role model only, no lane lineage, see §14
    → LongHorizonRealCalendarProjectionAdapter / LongHorizonActivatedCalendarAlignmentValidator — calendar binding
    → Persistence: LongHorizonRollingStateRepository → LongHorizonRollingSessionState entity — no LaneOrdinal/ProgressionStageKey/PrescriptionProfileKey columns, see §14
    → LongHorizonPublicPreviewMapper — public read surface
```

Separately, a fully **dark** (never-live, "Phase 4I.5/4I.6" prefixed) parallel skeleton exists — `LongHorizonStructuralMaterializer`, `LongHorizonDarkExecutionOrchestrator`, `LongHorizonFullNumericOrchestrator` — explicitly documented as "not called from any live request path." Not part of the real request flow; noted for completeness only.

---

## 6. Runway pilot-scope hardcoding (`RUNWAY_PILOT_SCOPE_HARDCODING_MATRIX`)

| Current check | Location | Why it exists | Original phase | Still required? | Generic authority available? | Blocks Intermediate×5D? | Classification |
|---|---|---|---|---|---|---|---|
| `request.DaysPerWeek == 4` (routing gate) | `PlanServices.IsPreparationRunwayPilotScope`, `PlanServices.cs:95-99` | Narrow, deliberate 15-20wk pilot scope for the original 4D activation | 4G.6A/6B/6C series | Yes, as *a* gate — but should dispatch through the identity policy, not a private literal copy | Yes — `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`ResolveCandidate` already recognize `(Intermediate, 5)` for Core; this gate simply never calls it | **Yes — first blocker** | `ENGINEERING_GAP` (narrow fix: dispatch through the generic policy instead of a private 4-field literal copy) |
| Unconditional load of `V1CatalogPilotIdentityPolicy.CandidateKey`/`Version` (always 4D) | `CatalogPreviewGenerator.cs:414-415` | Same original pilot scope | 4G.6A | No — should call `ResolveCandidate(level, daysPerWeek)` | Yes | Yes | `ENGINEERING_GAP` |
| `request.Candidate.CandidateKey != "TEN_K__4D__INTERMEDIATE" \|\| ...DaysPerWeek != 4` reject | `TenKPreparationRunwayDarkOrchestrator.ValidateRequest`, lines 336-339 | Fail-closed defense-in-depth against a wrong candidate reaching orchestration | 4G.6A.4H | Yes as a concept, but literal-value-hardcoded | No generic equivalent exists yet — orchestrator has no notion of "any supported Runway candidate," only the one literal | Yes | `ARCHITECTURE_GAP` — needs a real supported-candidate manifest, not a wider literal list (§7 of the phase prompt explicitly rejects `\|\| DaysPerWeek == 5`) |
| `TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout()` fixed `RUN_LAYOUT_4D`, 1 KEY + 2 EASY + 1 LONG | Same file, lines 6-28 | Runway's own weekly structure was authored once, for 4D | 4G.6A.4D | Unknown — depends on §12/§13 product decision | No — this is itself the missing generic authority | Yes | `PRODUCT_DECISION_REQUIRED` (see §12/§13) |
| `TenKPreparationRunwayFinalInvariantValidator` hard `== 4` slot/session-count checks | `TenKPreparationRunwayFinalInvariantValidator.cs:54-59, 113-116` | Defense-in-depth mirroring the structural layout above | 4G.6A.4H | Depends on the same product decision | No | Yes | `PRODUCT_DECISION_REQUIRED` |
| `TenKPreparationRunwayNumericPolicyFactory`/`TenKPreparationRunwayPacePolicyFactory` `CandidateKey`/`Version` constants (unused at runtime beyond documentation, but signal intended scope) | Both files, top-of-class constants | Same original pilot scope | 4G.6A | Documentation-only today (not enforced), but would silently apply 4D-authored rules to any candidate that got past the gates above | Partially — most underlying coefficients are generic (§8) | Not independently, but reinforces the "single cell" framing | `ENGINEERING_GAP` |

No `\|\| DaysPerWeek == 5` shortcut is recommended anywhere in this audit — every genuine blocker above requires either dispatching through the existing generic `V1CatalogPilotIdentityPolicy` authority or a real product decision, never a widened literal comparison.

---

## 7. Runway weekly-structure and dual-KEY authority

No product/domain authority anywhere in the repository decides whether a 15-20 week Intermediate×5D plan should run 5 sessions/week (full `RUN_LAYOUT_5D` immediately), a reduced Runway-specific frequency, or a Runway-specific role structure. `TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout()` (single-KEY, 4-slot, `RUN_LAYOUT_4D`) is the *only* structure ever authored, and it was authored once, for the 4D pilot, with no evidence of a 5D-scoped design discussion anywhere in the phase-report corpus.

**Marked `PRODUCT_DECISION_REQUIRED`** — per §12/§13 of the phase prompt, this is explicitly not derivable from Core's `RUN_LAYOUT_5D` alone (Runway may legitimately be a different preparation segment with its own role structure), and is not decided by any existing evidence. This is the single most load-bearing open Runway decision (see `RUNWAY_OPEN_DECISION_INVENTORY`, §25 below).

Positive finding: the underlying numeric allocator, `V1FourDaySessionVolumeAllocationPolicy.Allocate` (`backend/RunningApp.Application/RuntimeCatalog/Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs:64-110`), is already KEY-count-generic — its own doc comment (lines 44-53) states it is "reused, unmodified in behavior, for any 'V1 multi-key' shape sharing the same 2 EASY_SUPPORT + 1 LONG_RUN structure (e.g. a hypothetical 5D 2 KEY + 2 EASY + 1 LONG layout)." So *if* the product decision selects dual-KEY Runway, the numeric engine underneath is already prepared for it — only the structural layout policy and the two hardcoded invariants need to change, not the allocator itself.

---

## 8. Runway numeric authority

Auditing every numeric constant Runway touches:

| Value | Location | Classification |
|---|---|---|
| `PreferredMaxWeeklyIncreaseRatio=0.07`, `HardMaxWeeklyIncreaseRatio=0.08`, `AbsoluteWeeklyIncrementCapKm=2.5`, `LongRunPreferredMinimumShare=0.30`, `LongRunPreferredMaximumShare=0.36`, `LongRunSelectionShare=0.33`, `LongRunHardCapShare=0.40`, `RoundingIncrementKm=0.5` | `VolumeSafetyPolicy.Default` (`Prescription/Volume/VolumeSafetyPolicy.cs:76-88`) | `CANONICAL_GENERIC` — DaysPerWeek-agnostic Core policy, already reused verbatim |
| `ContinuityToleranceKm = 0.001` (`V1FourDaySessionVolumeAllocationPolicy.ToleranceKm`) | `Prescription/Session/V1FourDaySessionVolumeAllocationPolicy.cs:62` | `CANONICAL_GENERIC` (per its own doc comment, §7) |
| `MissingWeeklyVolumeDefaultKm=16`, `ExplicitZeroWeeklyVolumeDefaultKm=12` | `V1MissingReadinessStartingVolumePolicy.cs:5-10` | `INTERMEDIATE_4D_PRODUCT_VALUE` — provenance string literally reads "...explicit V1 product default for TEN_K/INTERMEDIATE/4D missing-zero readiness closure" |
| Session-distance split within a week (KEY/EASY/EASY/LONG) | `V1FourDaySessionVolumeAllocationPolicy.Allocate`, same file | `CANONICAL_GENERIC` (KEY-count-generic, §7) |

**No 4D numeric point should be borrowed into 5D without evidence**, per the phase's own instruction. The one open item is `V1MissingReadinessStartingVolumePolicy`'s starting-volume defaults — a same-named 3D sibling class (`V1ThreeDayMissingReadinessStartingVolumePolicy`) exists with identical numeric values but a distinct 3D-labeled provenance string, establishing the repository's own precedent that a new distance/frequency variant gets a distinct, explicitly-labeled class even when the numeric value happens to be unchanged — not silent reuse of the 4D-provenance one. Whether the *value* 16km/12km is still correct for 5D, or needs a fresh evidence pass, is itself a small, narrow, likely-low-risk product question (not a numeric-policy redesign) — flagged in the decision inventory (§25), not resolved here.

---

## 9. Runway → Core handoff authority (`RUNWAY_CORE_HANDOFF_AUTHORITY_MATRIX`)

| Question | Answer |
|---|---|
| How is starting Core volume calculated? | Via `PreparationRunwayCoreWeekOneTargetAdapter`/`PreparationRunwayCoreWeekOnePaceAdapter`, bridging Runway's own numeric continuity into Core Week 1 — generic bridge, no `DaysPerWeek` literal found in either adapter |
| Does Runway converge toward Core readiness? | Yes, that is the entire purpose of the numeric-continuity stage (Stage "NumericMaterialization" in §4's trace) |
| Does the handoff assume 4 sessions/week? | Yes — indirectly, via the fixed Runway layout (§7) and directly via `TenKPreparationRunwayDarkOrchestrator.ValidateRequest`'s hard `DaysPerWeek != 4` reject (§6) and the final invariant `totalSessions == (runwayWeeks + 12) * 4` (`TenKPreparationRunwayFinalInvariantValidator.cs:115`) |
| Does it assume one KEY? | Yes — same source |
| Does it assume the old 4D Intermediate reference volume? | The starting-volume defaults do (§8); the increase-ratio/long-run-share coefficients do not |
| Does it invoke generic 5D FREQ.6C numeric authority? | Not today — it never reaches Core generation for a 5D candidate at all, since the orchestrator rejects non-4D candidates before Core generation is ever attempted |
| Is Core Week 1 materialized through the same now-proven 5D pipeline? | **Partially, and only architecturally, not literally**: `TenKPreparationRunwayCoreGenerator` delegates to the same generic `IDynamicCoreCalendarMaterializationOrchestrator` class Core 8-14 uses — but it is invoked directly, not through `CatalogPreviewGenerator` itself, so it does **not** automatically inherit `CatalogPreviewGenerator`'s `LoadExecutionIndex` execution-context wiring (the exact fix `FREQ.6D.4D.5G` just made) — see §16/§28 |

---

## 10. Runway catalog-capacity result (`INTERMEDIATE_5D_RUNWAY_CATALOG_CAPACITY_MATRIX`)

| Slot/role | WorkoutDefinition available? | Eligible? | Profile available? | Execution projection? | Public WorkoutType mapped? | Calendar representable? | Persistence? | Classification |
|---|---|---|---|---|---|---|---|---|
| CONSISTENCY block (`EASY_STANDARD`, `LONG_RUN_STANDARD`) | Yes (v5/v5, real) | Yes | N/A (Legacy/effort-only, §15) | N/A | Yes (existing `Easy`/`LongRun` arms) | Yes (universal roles) | Yes | `READY` |
| GENERAL_ENDURANCE block (`LONG_RUN_STANDARD` repeated) | Yes | Yes | N/A | N/A | Yes | Yes | Yes | `READY` |
| AEROBIC_STRENGTH block, step 1 (`AEROBIC_STRENGTH_CONTROLLED_INTRO`) | Yes (v1 in Runway catalog; v3 in Core) | Yes | N/A | N/A | Yes (mapped `Interval` since `FREQ.6D.4D.5F`) | Yes | Yes | `READY` |
| AEROBIC_STRENGTH block, step 2 (`AEROBIC_STRENGTH_CONTROLLED_PROGRESSED`) | Yes (v1/v2) | Yes | N/A | N/A | **No arm exists in `V1CatalogPublicWorkoutTypeMappingPolicy`** — would throw `CatalogPublicWorkoutTypeUnsupportedException` | Yes | Yes | `CATALOG_CAPACITY_GAP` (narrow — one new switch arm, same class of fix as `5E`/`5F`, not yet done) |
| PRE_SPECIFIC_TRANSITION block (`EASY_STANDARD`) | Yes | Yes | N/A | N/A | Yes | Yes | Yes | `READY` |
| Weekly structural layout (dual-KEY, if selected) | N/A — no such layout authored | N/A | N/A | N/A | N/A | Would need re-verification against the generalized multi-KEY materializer (structurally should work, §14 of phase prompt) but never tested for Runway specifically | N/A | `PRODUCT_DECISION_REQUIRED` first (§7), then `ENGINEERING_GAP` |

**The catalog *content* itself (`plan-catalog/catalog/preparation-runway-progressions/*.json`) is entirely candidate-agnostic** — no `Level`, `DaysPerWeek`, or `combinationKey` field exists in any of the 4 files; every hardcoding found is runtime/policy code, not catalog data. This is a materially better starting position than it might appear — no catalog re-authoring is blocking Runway generalization, only code.

---

## 11. 15-20 week representability

| Weeks | Structurally representable? | Numerically representable? | Catalog representable? | Publicly activatable? |
|---|---|---|---|---|
| 15 | No — blocked on §7's product decision (weekly structure) and §6's engineering gates | Same | Yes (content itself is ready, §10) | No |
| 16 | Same | Same | Yes | No |
| 17 | Same | Same | Yes | No |
| 18 | Same | Same | Yes | No |
| 19 | Same | Same | Yes | No |
| 20 | Same | Same | Yes | No |

All six horizons are blocked identically — the Runway pipeline has no per-week-count branching that would make one of 15-20 more or less ready than another; the blockers are uniform across the whole 15-20 range.

---

## 12. LongHorizon frequency assumptions (`LONGHORIZON_FREQUENCY_HARDCODING_MATRIX`)

| Location | Exact hardcode |
|---|---|
| `LongHorizonGeStructuralContracts.cs:38-44` | `LongHorizonGeWeekRole` enum fixed at 4 values (`KeySession`, `EasySupportA`, `EasySupportB`, `LongRun`) |
| `LongHorizonGeNumericExecutor.cs:103` | `FourDaySessionDistanceAllocationPolicy.Allocate(...)` — 4-way-only split, no 5-session variant |
| `LongHorizonDarkExecutionOrchestrator.cs:48-49`, `LongHorizonStructuralMaterializer.cs:39-53` | Hardcoded `RUN_LAYOUT_4D`, `["KEY_SESSION","EASY_SUPPORT","EASY_SUPPORT","LONG_RUN"]`, `DaysPerWeek=4` (dark/unwired code, but shows the same assumption baked in twice) |
| `LongHorizonRollingCoreGenerationInputAdapter.cs:43,68` | `DaysPerWeek = 4` in both `GeneratePreviewRequest` and `ResolverInputSnapshot` construction |
| `LongHorizonRollingCheckpointRuntime.cs:368-371` | Throws unless `DaysPerWeek == 4` and `Level == Intermediate`, exact message: *"Checkpoint runtime eligibility is Race/exact-10K/Intermediate/4D/21-52 only."* |
| `LongHorizonRollingInitialActivationContracts.cs:104-111` | Same pattern, message: *"Rolling initial activation is restricted to Race / exact 10K / Intermediate / 4 days per week."* |
| `LongHorizonRollingJitActivationRuntime.cs:303` | Literal string `"TEN_K__4D__INTERMEDIATE v10 (unchanged)"` |
| `LongHorizonPublicPlanService.cs:81-82,162,203,319,354` | `CandidateKey`/`Version` constants + 4 separate `DaysPerWeek = 4` assignments + `ValidatePilot` explicit reject |
| `LongHorizonRollingRestartContinuationService.cs:65`, `LongHorizonRollingStateRepository.cs:62`, `LongHorizonFutureCoreRefreshOrchestrator.cs:97`, `LongHorizonFullDarkLifecycleHarness.cs:52,481` | All `DaysPerWeek = 4` |
| `LongHorizonPublicPreviewContracts.cs:133`, `LongHorizonPublicPreviewMapper.cs:18,99` | `DaysPerWeek` carried as a plain `int`, always caller-supplied as 4 |

**Every live and dark entry point independently hardcodes 4D**, with explicit fail-fast exceptions naming "4D" — this is broader and more redundant than Runway's already-substantial hardcoding (§6). Self-aware code comments (`AdaptationDomainContracts.cs:167-169`, `ScheduleRepairSpacingValidator.cs:20-27`, `NextWindowLoadDecisionPolicy.cs:16-18`) consistently describe Intermediate×5D as "a hypothetical Intermediate 5D layout," even though it is real, proven, and public in Core — confirming LongHorizon's code was never revisited after Core 5D shipped.

---

## 13. Five-session Adaptation reuse

**Positive finding — already generic.** `NextWindowLoadDecisionPolicy.DetermineLoadDecision` (`NextWindowLoadDecisionPolicy.cs:37-40`) dispatches on `summary.ExpectedSessionCount == FiveSessionStructuralWeekSize`, reproducing the real 24-row `FREQ.6 §6` severity table (lines 75-88) — the exact same policy Core 5D uses. `LongHorizonRollingWindowActivationService.cs:118-135` genuinely calls this same shared policy (`NextWindowLoadDecisionPolicy.Evaluate`, via `WeeklyWindowPartitioner.PartitionByStructuralWeekLineage` + `WeeklyLoadDecisionAggregator.AggregateWorstWeekWins`) — there is no separate, parallel LongHorizon-only adaptation mechanism.

**However, this capability is currently reachable-but-dead for LongHorizon**: nothing upstream (session persistence model, structural-week generation, every eligibility gate in §12) ever produces a genuine 5-session `WindowExecutionSummary` for a real LongHorizon plan today, since every entry point rejects non-4D candidates before adaptation logic is ever reached. The 5-session branch is only exercised by Core's own pipeline and by direct unit tests of the policy in isolation (`Freq6D4DSplitDFiveSessionAdaptationSeverityTests.cs`). **Classification: capability generic and ready; wiring to actually invoke it for LongHorizon does not exist yet, because nothing upstream produces the 5-session shape.**

---

## 14. Repair/substitution and JIT lane/stage/profile lineage (`LONGHORIZON_DUAL_KEY_LINEAGE_MATRIX`)

| Component | Model | Lane-aware? |
|---|---|---|
| `LongHorizonRollingSessionState` (persisted entity, `backend/RunningApp.Domain/Entities/LongHorizonRollingSessionState.cs:16-46`) | `SessionOrdinal`, `SessionRole` (string), `WorkoutKey`/`WorkoutVersion`, `DistanceKm`, `AssignedDate`, `AdaptedFromSessionId` | **No** — no `LaneOrdinal`, `ProgressionStageKey`, `PrescriptionProfileKey`/`Version` columns exist at all |
| `ScheduleRepairCandidate`/`ScheduleRepairTrigger` (`AdaptationDomainContracts.cs:109-131`) | `PreparationRunwaySlotRole` (structural role enum), `SourceSessionId` | **No** — repair knows structural role and date only |
| `ScheduleRepairSpacingValidator` (KEY↔KEY spacing, `FREQ.4`-generalized) | Operates on `SessionRole`/`AssignedDate` only | Correctly keeps two KEY sessions apart in time, but **cannot distinguish which lane** each occurrence belongs to — no lane concept exists to preserve |
| `LongHorizonRollingJitCompositionOrchestrator.BuildBoundedCoreSelection` (`.cs:219-269`) | Groups the real `CatalogPrescribedSession` output (which DOES carry `LaneOrdinal`/`ProgressionStageKey`) **by raw `StructuralRole` string only** (`.GroupBy(s => s.StructuralRole, ...)`), then FIFO-dequeues into `LongHorizonSessionPrescriptionReference` (`LongHorizonNumericWeekContracts.cs:33-43`), which has no lane/stage/profile fields at all | **No — this is the single most load-bearing gap.** A dual-KEY 5D week's two `KEY_SESSION` occurrences would land in the same bucket and be dequeued in arbitrary date order; only `WorkoutKey`/`WorkoutVersion`/`DistanceKm`/`AssignedDate` survive, which cannot recover which physical lane produced which prescription if the two lanes' workout keys can overlap |

**This is a real, structural, schema-level gap** — not merely a missing mapping step. `LongHorizonRollingSessionState` would need new persisted columns (a database migration, explicitly forbidden this phase and clearly a distinct, later implementation concern) before dual-KEY lineage could survive a LongHorizon round-trip at all.

---

## 15. Execution-index (published-bundle) propagation

**Zero references** to `ExecutionPrescriptionIndex` or `IPublishedTemplateBundleLoader` anywhere under `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/` (confirmed by direct grep, both research agents independently). Contrast with `CatalogPreviewGenerator.cs`, which owns both (`_publishedBundleLoader` field, `LoadExecutionIndex` helper — the exact mechanism `FREQ.6D.4D.5G` just finished generalizing across Core's own two internal pipelines).

Runway's own Core-handoff (§9) and LongHorizon's own Core-entry (§19) both invoke `TenKPreparationRunwayDarkOrchestrator`, **not** `CatalogPreviewGenerator` — so neither inherits `CatalogPreviewGenerator`'s execution-index wiring automatically. If either Runway or LongHorizon's Core segment ever prescribed a ProfileBacked session, it would hit the *exact same* class of missing-execution-index failure `FREQ.6D.4D.5G` diagnosed and fixed for `CompressedCore`/`ExtendedCore` — except broader, since `TenKPreparationRunwayDarkOrchestrator` has no execution-index parameter or field at all today, not even a partially-wired one. **This is a real, independent, not-yet-discovered engineering gap** that would surface the first time a ProfileBacked Core session is reached through either Runway or LongHorizon's Core-entry path.

---

## 16. LongHorizon catalog capacity

`plan-catalog/catalog/long-horizon-progressions/ten-k-long-horizon-ge-stage-families.v1.json` — single file, `DRAFT` status, 5 stage families (`ENTRY`/`BASE_DEVELOPMENT`/`AEROBIC_DURABILITY`/`CONSOLIDATION`/`PRE_RUNWAY_ALIGNMENT`), role assignments keyed by `role`/`profile` only — **no level/frequency field**, same candidate-agnostic pattern as Runway's catalog content (§10). However, this file is explicitly **not loaded at runtime** — `LongHorizonGeStructuralSelector.cs:6-16` states its role-assignment content is mirrored into a "hand-verified, hardcoded backend constant table" in C#, not read from the JSON. The catalog document is a dark, deliberately-unregistered design artifact, not live content. Generalizing LongHorizon's GE segment would mean editing this backend constant table (or finally wiring real catalog loading), not authoring new catalog JSON — a materially different, more code-centric gap than Runway's.

---

## 17. LongHorizon numeric authority

| Value | Location | Classification |
|---|---|---|
| Volume-ramp ratios/caps/long-run shares (reused from `VolumeSafetyPolicy.Default`) | `LongHorizonGeNumericExecutor.cs` | `GENERIC` |
| `RecoveryVolumeRatio=0.85`, `MinimumRecoveryReductionKm=0.5` | Same file, lines 46, 49 | `DERIVED` (documented rationale: "15% reduction"; not obviously frequency-specific but never evidenced for 5D) |
| Final per-session split via `FourDaySessionDistanceAllocationPolicy.Allocate` | Same file, line 103 | `4D-SPECIFIC` — no 5-session/dual-KEY variant exists |
| Core-segment numeric authority (once Core begins) | Reuses the exact canonical `CatalogWorkoutProgressionLoader`/`ProgressionStageAllocator`/`CatalogWorkoutBinder` Core 5D uses | `GENERIC`, but only reachable via the hardcoded 4D gates in §12 |

No 4D value was found being silently extrapolated into a 5D assumption anywhere in this layer — the gaps are omissions (no 5D variant exists yet), not incorrect reuse.

---

## 18. Core-entry semantics

**LongHorizon does not reuse `CatalogPreviewGenerator` directly.** `LongHorizonCoreWorkoutBindingExecutor.cs:12-33` explicitly states it invokes "the EXISTING, UNCHANGED Core workout-resolution authority... the same real production API sequence `CatalogPreviewGenerator`'s own standard 8-14 week path uses" — i.e. it deliberately mirrors the same low-level primitive classes (`CatalogWorkoutBinder`, `ProgressionStageAllocator`, `CatalogWeekSkeletonCalendarMaterializer`, `CatalogWorkoutProgressionLoader`) but through its **own separate orchestration wrapper**, not a call into `CatalogPreviewGenerator` itself. The live path (`LongHorizonRollingJitCompositionOrchestrator`) invokes `TenKPreparationRunwayDarkOrchestratorFactory` — the same orchestrator Runway itself uses (§9) — not `CatalogPreviewGenerator`.

This is a genuine, if partial, violation of the phase's own required invariant ("once Core begins, the exact public 5D Core pipeline must be reused — no LongHorizon-specific duplicate 5D Core implementation"): the *primitives* are shared, but the *orchestration entry point* (and therefore anything wired only at that entry point, e.g. the execution-index load, §15) is duplicated across three call sites: `CatalogPreviewGenerator` (Core direct), `TenKPreparationRunwayDarkOrchestrator` (used by both Runway and, transitively, LongHorizon).

---

## 19. Public routing audit

| Horizon | Intermediate×5D current result |
|---|---|
| 15-20 | `PlanHorizonCompositionRequiredException` / `PLAN_HORIZON_COMPOSITION_REQUIRED`, HTTP 422 — the Runway pilot gate (`IsPreparationRunwayPilotScope`) is never reached because it independently hardcodes `DaysPerWeek == 4` and short-circuits before consulting `V1CatalogPilotIdentityPolicy` at all |
| 21+ | Identical exception/reason code — no LongHorizon composition path exists in the public *preview* pipeline for any candidate; the only real LongHorizon logic lives behind `LongHorizonRollingWindowActivationService`, a separate continuation/rolling-window service, not the preview route, and it independently hardcodes 4D at every entry point (§12) |

No routing change was made or is proposed here.

---

## 20. Support vs eligibility matrix (`INTERMEDIATE_5D_HORIZON_ACTIVATION_READINESS_MATRIX` — combined with §21)

| Horizon mode | Identity support | Request eligibility | Public rollout |
|---|---|---|---|
| Core 8-14 | `ARCHITECTURALLY_SUPPORTED` | `PRODUCT_ELIGIBLE` | `PUBLICLY_ACTIVE` |
| Preparation Runway 15-20 | Not supported — pilot gate hardcoded to 4D | `BLOCKED` (typed 422, not a 500) | `BLOCKED` |
| LongHorizon 21+ | Not supported — every entry point hardcoded to 4D | `BLOCKED` (typed 422 at the preview layer; rolling-activation entry points independently reject) | `BLOCKED` |

No horizon's current state is collapsed — Core is genuinely all four; Runway/LongHorizon are genuinely blocked at the identity layer already, before eligibility or rollout questions even apply.

---

## 21. Readiness matrix (`INTERMEDIATE_5D_HORIZON_ACTIVATION_READINESS_MATRIX`, full)

| | RunLayout | Numeric authority | Catalog content | Lane/stage support | Profiles | Execution bundle | Calendar | Persistence | Adaptation | Repair | Runtime routing | Public materialization | Tests | Status |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Core 8-14** | `RUN_LAYOUT_5D`, verified | `FREQ.6C` approved, verified | Full real closure authored | Dual-lane, verified | 8 real profiles | Real, resolved | Multi-KEY generalized, verified | Exact lineage columns, verified | 5-session table, verified | N/A (Core has no repair layer) | Publicly active | Verified | Real E2E + dark, extensive | **VERIFIED/PUBLIC** |
| **Prep Runway 15-20** | `RUN_LAYOUT_4D` hardcoded; no 5D layout authored | Coefficients generic; starting-volume 4D-tagged; final split 4D-only | Candidate-agnostic content, ready | None — single-KEY structural model only | N/A (Legacy/effort-only) | N/A (never consulted) | Untested for a hypothetical 5D shape | N/A (Runway itself isn't separately persisted the way LongHorizon is — it flows into the same `TrainingDay` persistence Core uses once confirmed) | N/A (Runway predates the 5-session table; not applicable to its own effort-only sessions) | N/A | Hardcoded gate, never reaches routing decision for 5D | N/A | Mostly 4D-hardcoded at the numeric/orchestrator/mapper layers; generic at the engine/binding/date-authority layers | `BLOCKED — PRODUCT DECISION + ENGINEERING GAP` |
| **LongHorizon 21+** | Hardcoded `RUN_LAYOUT_4D` (dark path); live path never reaches a RunLayout choice at all (rejects before) | Mixed: adaptation policy generic; GE numeric split 4D-only | Dark, unregistered, hand-mirrored-in-code content | **None — schema has no lane/stage/profile columns; JIT composition actively discards lineage** | N/A today (no ProfileBacked session has ever reached LongHorizon) | **Never referenced anywhere in LongHorizon** | Untested for 5D | **Schema-level gap — would require a DB migration** | Generic policy exists but unreachable (dead code path for LongHorizon today) | Week/role model only, no lane awareness | Every entry point hardcoded to 4D, independently, redundantly | N/A | Rolling-activation typed-contract/engine tests generic; JIT-composition/public-preview-mapper tests 4D-hardcoded | `BLOCKED — ARCHITECTURE GENERALIZATION REQUIRED (incl. schema)` |

---

## 22. Technical-debt disposition

- **`TD-RUNWAY-ARCHITECTURE-HARDCODED-SINGLE-CELL-001`** (originated `FREQ.2`, status `preserved` as of `FREQ.6`'s own re-check) — the direct, still-open debt record matching this audit's own Runway findings exactly. Quote from `FREQ.2`'s own audit: *"Runway is not 'currently gated to Intermediate×4D' the way Core's public identity allow-list gates Beginner×3D — it is architecturally hardcoded to that one specific cell... it would need new architecture, not a widened allow-list, to support any cell other than Intermediate×4D."* **Classification: `STILL_OPEN`** — this audit found nothing that resolves it; if anything, this audit adds the specific decomposition (routing/candidate-load/orchestrator-validate/structural-layout, §6) that a future implementation phase would need.
- **No dedicated LongHorizon debt record exists yet.** This audit's own findings (§12-19) constitute the first systematic disclosure of LongHorizon's frequency hardcoding as a distinct concern from Runway's — worth recording as a new debt item (not created here, since this is an evidence phase, but flagged for the next phase's own governance update as `TD-LONGHORIZON-ARCHITECTURE-HARDCODED-SINGLE-KEY-001` or equivalent).
- `TD-5D-SEVERITY-THRESHOLD-GENERALIZATION-001` and `TD-CROSS-FREQUENCY-VOLUME-PROGRESSION-SHAPE-001` — both unrelated to Runway/LongHorizon extension specifically (already `RESOLVED_BY_FREQ6`/closed-as-deliberate-divergence respectively); not re-examined further here.

---

## 23. Test inventory result

**Preparation Runway**: engine/materializer/binding/date-authority layer tests (`PreparationRunwayBlockAllocationEngineTests`, `PreparationRunwayBlockWorkoutBindingEngineTests`, `PreparationRunwayDateAuthorityTests`) are genuinely generic — zero hardcoded 4D literals found. The numeric-materialization, dark-orchestrator, and public-preview-mapper test files (`PreparationRunwayNumericMaterializerTests`, `TenKPreparationRunwayDarkOrchestratorTests`) are pilot-specific, asserting the literal `"TEN_K__4D__INTERMEDIATE"` candidate key and `DaysPerWeek = 4` directly.

**LongHorizon**: the rolling-activation typed-contract/composition-resolver/structural-materializer/full-numeric-orchestrator tests are generic (candidate-agnostic by construction). The JIT-composition and public-preview-mapper tests are 4D-hardcoded (`LongHorizonPublicPreviewMapperTests.cs:22` asserts `DaysPerWeek = 4` directly).

**Pattern (both subsystems)**: lower-level "mechanism" tests were written generically from the start; higher-level "does this produce the right real plan" tests were written against the one real cell that existed at the time (4D) — exactly the same shape of finding, twice. No test anywhere exercises a hypothetical/synthetic 5D Runway or LongHorizon plan — this is a genuine test gap, not merely an implementation gap, for any future generalization effort.

---

## 24. Representative dark failures (§38 of the phase prompt)

No new HTTP calls or code execution were performed this phase (evidence phase — static analysis only, consistent with §44's "NO CODE" instruction extending to not exercising code paths that could be mistaken for implementation validation). The exact first-failure behavior for each representative horizon is derivable with certainty from the static trace above, without needing to execute anything:

| Weeks | First failure (by static trace) |
|---|---|
| 15 | `PlanServices.cs:147` — `IsPreparationRunwayPilotScope(request)` returns `false` for `DaysPerWeek=5` → falls to `PlanHorizonCompositionRequiredException` at `PlanServices.cs:160-163` |
| 20 | Identical |
| 21 | Same exception — `availableWeeks` outside `[15,20]` also fails the same guard regardless of `DaysPerWeek` |
| 24+ | Identical — no per-week-count branching differentiates 21 from 52 in the public preview pipeline |

This satisfies §39's "do not stop after the first runtime failure" instruction differently than a live dry-run would: rather than a serial reveal, the full static trace above (§6, §12) already enumerates every subsequent blocker that a live attempt would hit one at a time — the routing gate, the candidate-load, the orchestrator's own `ValidateRequest`, the structural invariants, and (for LongHorizon specifically) the schema-level lineage gap — none of which would be discovered by a single dry-run failure trace alone.

---

## 25. `RUNWAY_OPEN_DECISION_INVENTORY`

| ID | Question | Why load-bearing | Current evidence | Decision type | Blocks implementation? | Suggested next phase |
|---|---|---|---|---|---|---|
| R-D1 | Does a 15-20 week Intermediate×5D plan run full 5 sessions/week during Runway, a reduced frequency, or a distinct Runway-specific role structure? | Determines the entire Runway weekly-layout shape; not derivable from Core `RUN_LAYOUT_5D` alone | None found anywhere in the repository | `PRODUCT_DECISION` | Yes — blocks everything downstream in §6/§7/§10/§21 | A dedicated product/evidence decision phase (mirrors `FREQ.6`'s original Core numeric-authority decision) |
| R-D2 | If dual-KEY is selected, does the multi-KEY calendar materializer (already generalized for Core, `FREQ.6D.4D.5A/5B`) hold structurally for a Runway-shaped week without reopening the approved KEY↔KEY numeric rule? | Avoids re-litigating an already-approved product decision unnecessarily | The materializer is generic by construction (`5B`); no Runway-specific spacing scenario has been evidenced | `EVIDENCE_REQUIRED` (likely confirmatory, not a new decision) | Only if R-D1 selects dual-KEY | Same phase as R-D1, or the immediately following engineering phase |
| R-D3 | Are `V1MissingReadinessStartingVolumePolicy`'s 16km/12km starting-volume defaults still correct for a 5D Runway, or does 5D need its own evidence-labeled variant (mirroring the existing 3D sibling class)? | Avoids silently borrowing an unevidenced 4D number into 5D | Repository's own precedent (distinct 3D class) suggests a distinct 5D class is expected | `PRODUCT_DECISION` (narrow) | No — can default conservatively or be deferred, but must not be silently reused without a decision | Same phase as R-D1 |
| R-D4 | Should the Runway pilot gate be rearchitected to dispatch through `V1CatalogPilotIdentityPolicy`/a supported-combination manifest, or does a second, Runway-specific gate remain justified? | Determines whether R1 (remove hardcoded identity, dispatch generically) or a narrower fix is appropriate | `V1CatalogPilotIdentityPolicy` already recognizes `(Intermediate, 5)`; Runway's gate simply never calls it | `ENGINEERING_GAP`, architecture-flavored | No — purely an implementation-phase design choice | The Runway implementation phase, after R-D1/R-D2/R-D3 close |
| R-D5 | `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` has no public-workout-type mapping arm | Would 500 the first time this workout is reached publicly, independent of the 5D question (affects 4D Runway too, if ever activated) | `V1CatalogPublicWorkoutTypeMappingPolicy.Map`'s current arm list, confirmed | `ENGINEERING_GAP` (small, same pattern as `5E`/`5F`) | Yes, for any future Runway public activation regardless of frequency | A narrow, `5E`/`5F`-style decision+implementation pair, whenever Runway activation is next attempted |

---

## 26. `LONGHORIZON_OPEN_DECISION_INVENTORY`

| ID | Question | Why load-bearing | Current evidence | Decision type | Blocks implementation? | Suggested next phase |
|---|---|---|---|---|---|---|
| L-D1 | Does the LongHorizon persisted session schema (`LongHorizonRollingSessionState`) need new `LaneOrdinal`/`ProgressionStageKey`/`PrescriptionProfileKey`/`Version` columns to support dual-KEY lineage? | Without this, dual-KEY lineage cannot survive a LongHorizon round-trip at all (§14) | Confirmed absent by direct entity inspection | `ARCHITECTURE_GAP` (implies a DB migration — a distinct, later, more consequential implementation step) | Yes — the most load-bearing LongHorizon gap | A dedicated architecture-design phase, then a migration-carrying implementation phase |
| L-D2 | Should `LongHorizonRollingJitCompositionOrchestrator.BuildBoundedCoreSelection` key its role-bucket grouping on `LaneOrdinal` instead of raw `StructuralRole` string? | Directly causes lane conflation for any dual-KEY week that reaches JIT composition (§14) | Confirmed by direct code trace | `ENGINEERING_GAP` (once L-D1's schema exists) | Yes, but only after L-D1 | Same implementation phase as L-D1 |
| L-D3 | Should `TenKPreparationRunwayDarkOrchestrator` (shared by both Runway's Core-handoff and LongHorizon's Core-entry) receive the same `IPublishedTemplateBundleLoader`/`ExecutionPrescriptionIndex` wiring `CatalogPreviewGenerator` now has? | Without it, any ProfileBacked Core session reached through either Runway or LongHorizon would fail exactly as `FREQ.6D.4D.5F` disclosed for Core's own dynamic branches | Confirmed absent (§15, §18) | `ARCHITECTURE_GAP` | Yes — for either Runway or LongHorizon, independent of the 5D-specific questions | Could be closed as its own narrow phase (affects 4D too, in principle, the moment any ProfileBacked content is ever authored for Runway/LongHorizon use) |
| L-D4 | Does the GE (pre-Core) numeric segment need a frequency-generic distance-allocation policy to replace `FourDaySessionDistanceAllocationPolicy`? | Blocks any 5D LongHorizon plan's pre-Core segment specifically | Confirmed 4-way-only (§17) | `ENGINEERING_GAP` | Yes, for LongHorizon only (Runway's own allocator is already generic, §7) | Same implementation phase as L-D1/L-D2 |
| L-D5 | Should LongHorizon's Core-entry be re-architected to call `CatalogPreviewGenerator` directly, rather than a separate orchestration wrapper sharing only low-level primitives? | Determines whether future Core-pipeline fixes (like `5G`'s execution-context fix) automatically propagate to Runway/LongHorizon, or need to be re-applied at each duplicate entry point | Confirmed as a real, if partial, duplication today (§18) | `ARCHITECTURE_GAP` | Not strictly blocking, but a real design debt that would keep recurring otherwise | Worth deciding explicitly in the same architecture-design phase as L-D1 |
| L-D6 | Frequency semantics — should every hardcoded `DaysPerWeek == 4`/`CandidateKey == "TEN_K__4D__INTERMEDIATE"` guard across LongHorizon (§12, ~10 call sites) be relaxed to dispatch through the generic identity policy? | Same category as R-D4, but far more call sites, all independently hardcoded | Confirmed, exhaustively, in §12 | `ENGINEERING_GAP` (mechanical once L-D1-L-D5 are resolved) | Yes | Final step of the LongHorizon implementation phase |

---

## 27. Architecture options

**Preparation Runway:**
- **R1 (preferred where applicable)**: remove the hardcoded pilot identity at the routing/candidate-load layers (§6's `PlanServices.IsPreparationRunwayPilotScope`, `CatalogPreviewGenerator.cs:414-415`) and dispatch through the already-5D-aware `V1CatalogPilotIdentityPolicy`/a supported-combination manifest, once R-D1's product decision exists.
- **R2**: generalize the Runway schedule/prescription contract (`TenKPreparationRunwayWeekMaterializationPolicyFactory`, `TenKPreparationRunwayFinalInvariantValidator`) to accept a variable slot/KEY-count layout, parameterized by the resolved candidate's `RunLayout` rather than a hardcoded `RUN_LAYOUT_4D` constant — required regardless of what R-D1 decides, since even a single-KEY 5D Runway would need its own distinct layout reference.
- **R3 (rejected unless R-D1 proves otherwise)**: a wholly new, 5D-specific Runway policy duplicating the 4D one. Not recommended — no evidence found that Runway's *product* semantics (readiness buildup, block sequencing, aerobic-strength progression) differ by frequency; only the *weekly structural shape* is an open question (R-D1), which R2's parameterization already accommodates without a full duplicate policy.

**LongHorizon:**
- Prefer threading the generic RunLayout/lane/profile authority through the existing pipeline (mirroring Core's own dual-KEY architecture) over any duplicate 5D-specific LongHorizon implementation. Concretely: extend `LongHorizonRollingSessionState`'s schema (L-D1), fix `BuildBoundedCoreSelection`'s grouping key (L-D2), wire the shared orchestrator's execution-index (L-D3), replace the GE numeric allocator (L-D4), and relax the ~10 hardcoded gates (L-D6) — all additive/parameterizing changes to the existing architecture, not a parallel 5D-only LongHorizon subsystem.
- The one genuine architecture-level design question (not just "generalize the existing code") is L-D5: whether to eliminate the `TenKPreparationRunwayDarkOrchestrator` duplication in favor of both Runway and LongHorizon calling into `CatalogPreviewGenerator`'s own Core-entry method directly. This is worth resolving explicitly, not left implicit, since it determines how many places future Core-pipeline fixes need to be re-applied.

---

## 28. Whether Runway and LongHorizon should split (§33 decision)

**Selected: a variant of OPTION H2 — Runway should close first; LongHorizon depends on it — captured under the required `SEPARATE_WAVES` final classification, since the two also have materially different blocker *types* (Runway: one real product decision + moderate engineering; LongHorizon: no unresolved product decision found, but a genuinely deeper architecture/schema gap), which independently argues for separate waves even setting the dependency aside.**

Evidence for the dependency: LongHorizon's own live Core-entry (`LongHorizonRollingJitCompositionOrchestrator`) invokes the *same* `TenKPreparationRunwayDarkOrchestratorFactory` Runway uses (§18) — so any Runway-layer generalization (R1/R2 above) that touches `TenKPreparationRunwayDarkOrchestrator`'s accepted-candidate surface would need to happen before or alongside LongHorizon's own Core-entry generalization, not after it, or LongHorizon would inherit Runway's still-narrower gate. Options H1 (single combined wave) and H3 (fully independent, either order) are both rejected: H1 because LongHorizon's schema-level gap (L-D1) is a materially larger, DB-migration-carrying unit of work that would make a combined wave's scope unpredictable; H3 because the shared-orchestrator dependency above means "independent, either order" is not actually true — LongHorizon's Core-entry work is only well-defined once Runway's own Core-entry/orchestrator surface is settled. H4 ("both require product/evidence phases first") is partially true for Runway (R-D1) but not accurate for LongHorizon, where no open product decision was found — LongHorizon's blockers are architecture/engineering/schema, not product authority.

---

## 29. Readiness matrix

See §21 above (already produced in the required format and position; not duplicated here to avoid redundancy — cross-referenced per the report's own section numbering versus the phase prompt's, since several of the prompt's requested sections map to the same underlying evidence already presented once).

---

## 30. Recommended sequence

Based on actual findings (not the phase prompt's own illustrative example):

**A. Preparation Runway product-decision phase** — resolve R-D1 (weekly structure/dual-KEY question) and R-D3 (starting-volume evidence), evidence/product type, no code.
**B. Preparation Runway architecture + implementation phase** — R2 (generalized layout contract) + R1 (dispatch through the generic identity policy) + R-D5 (public workout-type mapping arm for `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED`) + L-D3 (execution-index wiring into the shared `TenKPreparationRunwayDarkOrchestrator`, since Runway needs it too and LongHorizon will inherit it) + real dark/public activation retry for 15-20 week Intermediate×5D, mirroring the Core `5A`-`5G` discipline (one blocker at a time, STOP on new independent blockers).
**C. LongHorizon architecture-design phase** — resolve L-D1 (schema design) and L-D5 (Core-entry duplication decision) explicitly as design decisions, informed by whatever Runway's phase B settled for the shared orchestrator.
**D. LongHorizon implementation phase** — the schema migration (L-D1), JIT lineage fix (L-D2), GE numeric generalization (L-D4), and the ~10 hardcoded-gate relaxation (L-D6), followed by its own real dark/public activation retry sequence.

No speculative phase-ID tree is created here — only the next concrete phase (A) should be scheduled now; B-D remain named-but-unscheduled, consistent with `MASTER_ROADMAP.md`'s own §13 rule against pre-authoring IDs beyond the near-term horizon.

---

## 31. Next phase / type

**Next concrete phase: Preparation Runway weekly-structure & starting-volume product decision** (maps to recommended-sequence item A above) — type `EVIDENCE + PRODUCT_DECISION`, mirroring the `FREQ.6`/`FREQ.6D.4D.5E` pattern already established in this engagement for exactly this kind of narrow, load-bearing, non-derivable decision.

---

## 32. Final classification

**`INTERMEDIATE_5D_RUNWAY_LONGHORIZON_SEPARATE_WAVES_REQUIRED`**

Rationale: Preparation Runway is blocked on one genuine, non-derivable product decision (R-D1: dual-KEY vs. single-KEY vs. reduced-frequency Runway structure) plus moderate, well-scoped engineering generalization (§6, §27) — its catalog content is already candidate-agnostic and its numeric coefficients are already mostly generic. LongHorizon has no unresolved product-authority question of its own, but a materially deeper architecture gap, including a real persisted-schema deficiency (no lane/stage/profile lineage columns at all) that would require a database migration to fix, plus a total absence of execution-index wiring and roughly ten independently-hardcoded 4D gates. The two also share a real implementation dependency (both invoke the same `TenKPreparationRunwayDarkOrchestrator` for their respective Core-entries), which argues for sequencing — Runway's own generalization first — rather than either a single combined wave or two fully independent ones. Neither `_ARCHITECTURE_READY` classification applies (both have genuine blockers, not merely gating); `_BLOCKED_ON_CATALOG_CAPACITY` does not apply (catalog content itself is ready in both cases, module one small missing public-workout-type mapping arm); `_NONCORE_ACTIVATION_ENGINEERING_READY` does not apply (Runway has a real open product decision, and LongHorizon has a real architecture/schema gap, not merely gating code to relax).
