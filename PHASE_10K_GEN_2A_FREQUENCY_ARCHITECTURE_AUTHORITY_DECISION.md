# Phase 10K-GEN.2A — Frequency Architecture Authority Decision

**Decision resolution only. No production code or catalog JSON changed. No new frequency activated.**

## 1. Evidence inputs

Established inputs: `PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md`, `PHASE_10K_GEN_1_ARCHITECTURE_AUDIT.md`, the current catalog composition contracts/artifacts, `CatalogPlanSkeletonOrchestrator`, `DynamicCoreWeekSkeletonOrchestrator`, their equality tests, current production call sites, `CatalogRunLayoutResolver`, identity/rollout gates, and the downstream fixed-4D policies/validators identified by GEN.1.

Current-code reconciliation: GEN.1 described the dynamic skeleton orchestrator as having zero production call sites. Current `CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton` constructs the dynamic Core chain for compressed/extended 8–14-week modes, and `TenKPreparationRunwayDarkOrchestratorFactory.Create` constructs it for the Runway+Core Core segment. The exact-12 preferred Core branch still invokes `CatalogPlanSkeletonOrchestrator`. Therefore the current state is already transitional dual use, not fully dark. `IDynamicCoreWeekSkeletonOrchestrator` still has no DI registration; that is a composition detail, not proof of runtime unreachability. The stale “zero production call sites” comment and structural-test exclusions are not architectural authority.

## 2. Frozen composition architecture

Approved responsibility model:

```text
TEN_K_MASTER
+ resolved RUN_LAYOUT_xD
+ resolved LevelModifier / level prescription
+ RulePack
+ workout catalog / progression
= resolved plan configuration
```

`TEN_K_MASTER` owns Core phases and distance progression vocabulary. `RUN_LAYOUT_xD` owns ordered weekly running roles and their cardinality. Level authorities own approved prescription differences. A combination artifact binds compatible/versioned references. Legitimate cross-axis policy rows such as Distance × Level × RunsPerWeek peak-volume bands remain allowed; they are policy data, not copied Core architecture.

## 3. Core skeleton authority decision

```text
CORE_SKELETON_AUTHORITY_SINGLE_DYNAMIC
```

`DynamicCoreWeekSkeletonOrchestrator` is the future canonical runtime authority for Core skeleton materialization.

Evidence:

- It resolves phase allocation for an explicit supported Core week count.
- It consumes the same `ICatalogRunLayoutResolver`, `ICatalogStageToWeekMaterializer`, and `IGeneratedCatalogPlanSkeletonValidator` contracts as the fixed path.
- `Build_TargetWeekCount12_MatchesExistingFixedWeekOrchestratorExactly` proves full field-level equality for the canonical 12-week/4D result.
- Its run-layout dependency is collection-based and can represent the frozen 3D–7D role sequences at the skeleton layer.
- Current production already uses the dynamic chain for compressed/extended Core modes and the Runway+Core Core segment. Retaining a separate fixed 12-week authority would preserve duplication without a demonstrated domain distinction.

`PERMANENT_COEXISTENCE` is rejected: no semantic responsibility exists uniquely in the fixed orchestrator. `FIXED_RETAINED` is rejected: no invariant violation contradicts the equality proof, and current runtime use already demonstrates dynamic-path acceptability.

## 4. Fixed 4D path lifecycle decision

End state:

```text
B2 — DIRECT_CALLER_MIGRATION
```

The exact-12 caller must eventually invoke the canonical dynamic Core authority directly; `CatalogPlanSkeletonOrchestrator` then retires as an independent production authority. A thin facade is permitted only if compatibility requires the old interface temporarily, and it must delegate without independent skeleton logic.

`B3 — TEMPORARY_PARALLEL_EXECUTION` is permitted solely as a migration guard, not as the end state. Exit criteria:

1. exact-12/4D field-level equality passes for phase allocation, weeks, ordered roles, dates, and provenance;
2. workout binding, prescription, calendar, public payload, and persistence regression tests remain equal;
3. no production caller relies on fixed-only behavior;
4. the fixed implementation is removed or reduced to a delegating facade;
5. any shadow comparison is removed after the agreed observation period.

Independent B4 retention is forbidden by the single-authority decision.

## 5. Frequency source-of-truth decision

```text
RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY
```

The resolved `RunLayoutDefinition`/`CatalogRunLayoutSlots` is the sole structural authority for:

- `RunsPerWeek` / effective `DaysPerWeek`;
- ordered weekly slots;
- total slot count;
- role occurrence cardinality.

Repository evidence supports this ownership: `CatalogRunLayoutResolver.Resolve` reads `candidate.SlotRoles`, returns the ordered collection, and explicitly says the layout slot count is the source of truth. Missing downstream 3D–7D support does not create a competing structural authority.

### Derived-value classification

| Value | Classification | Decision |
|---|---|---|
| Resolved RunLayout `RunsPerWeek` and `Slots[]` | `AUTHORITATIVE` | Canonical structural frequency source |
| `Candidate.DaysPerWeek` | `BOUNDARY_VALIDATION` | Manifest compatibility claim; must equal resolved layout count, not independently define it |
| `PreferredDays.Count` | `DERIVED` + `BOUNDARY_VALIDATION` | Must equal resolved layout count; day choices remain user/calendar input |
| Weekly generated session count | `DERIVED` | Equals resolved slot count |
| KEY occurrence count | `DERIVED` | Count resolved KEY roles |
| EASY occurrence count | `DERIVED` | Count resolved EASY roles |
| LONG occurrence count | `DERIVED` | Count resolved LONG roles |
| Dated-week session count | `BOUNDARY_VALIDATION` | Must equal generated structural slot count |
| Persisted `TrainingDay` count per structural week | `BOUNDARY_VALIDATION` | Must equal materialized dated-week count |
| Published combination compatibility | `LEGITIMATE_SEPARATE_POLICY` | Determines whether references may compose, not role structure |
| Live exposure of a frequency | `ROLLOUT_GATE` | May temporarily remain 4D-only |

## 6. Support-vs-rollout authority decision

Current classification:

```text
V1CatalogPilotIdentityPolicy = MIXED_RESPONSIBILITY
```

It currently bundles candidate identity/version, catalog support, and production pilot exposure into one `TEN_K + Intermediate + 4D` predicate. Similar inline checks repeat that mixture in Runway+Core and LongHorizon.

Required future boundary:

- **Catalog compatibility authority:** the published combination/compatibility catalog answers whether Distance × Level × Frequency references form a supported, version-valid composition.
- **Runtime rollout authority:** a separately named activation policy answers whether that already-compatible combination is enabled for live traffic.

`V1CatalogPilotIdentityPolicy` may remain temporarily as `ROLLOUT_ACTIVATION_GATE` for Intermediate/4D. It must not remain the domain source of Frequency, phase structure, or catalog compatibility. Candidate keys/versions are lookup/binding outputs of compatibility resolution, not universal constants.

## 7. Duplicate-authority resolution table

The table resolves the load-bearing GEN.1 findings by responsibility; it does not prescribe implementation order.

| File / type | Current assumption | Current role | Future classification | Canonical upstream authority | Validator? | Temporary rollout gate? | Later domain decision? | Reason |
|---|---|---|---|---|---:|---:|---:|---|
| `V1CatalogPilotIdentityPolicy` | TenK + Intermediate + 4D + candidate v10 | Core pilot routing/support | `TEMPORARY_ROLLOUT_GATE` after responsibility split | compatibility manifest + rollout policy | No | Yes | No | Mixed today; may gate exposure but not define structure |
| `PlanServices.IsPreparationRunwayPilotScope` | 15–20 and exact bundled identity | Runway+Core admission | `TEMPORARY_ROLLOUT_GATE` + `CONFLICTING_AUTHORITY` today | horizon classifier + compatibility + rollout | No | Yes | No | Hand-duplicates support and activation |
| `TenKPreparationRunwayDarkOrchestrator.ValidateRequest` | exact 4D identity/version | Defensive admission | `VALID_BOUNDARY_ASSERTION` for resolved manifest; fixed 4D portion is rollout-only | resolved compatible manifest + rollout | Yes | Yes | No | Candidate/request agreement is valid; literals are not structural truth |
| `LongHorizonPublicPlanService.ValidatePilot` and hardcoded builders | TenK/Intermediate/4D | LongHorizon pilot admission/input construction | `TEMPORARY_ROLLOUT_GATE`; hardcoded construction is `CONFLICTING_AUTHORITY` | compatibility + rollout + request/resolved manifest | Yes | Yes | No | LongHorizon remains separate, but cannot silently invent identity |
| `LongHorizonRollingInitialActivationContracts` | Level=Intermediate, Days=4 | Activation validation | `VALID_BOUNDARY_ASSERTION` + temporary rollout scope | persisted resolved combination/layout + rollout | Yes | Yes | No | Boundary may validate admitted scope, not define layout |
| `LongHorizonRollingCheckpointRuntime` | Level=Intermediate, Days=4 | Checkpoint admission | `TEMPORARY_ROLLOUT_GATE` | persisted combination + rollout | No structural validator | Yes | No | Not a Core frequency authority |
| `CatalogPlanSkeletonOrchestrator` | default 12-week candidate path | fixed Core skeleton authority | `CONFLICTING_AUTHORITY` until retired/delegating | `DynamicCoreWeekSkeletonOrchestrator` | Regression oracle only | No | No | Equality proves no permanent semantic need |
| `CatalogRunLayoutResolver` | roles/count from candidate layout | Run-layout resolution | `MUST_DERIVE_FROM_RUN_LAYOUT` (already compliant) | published RunLayout | Yes | No | No | Canonical structural authority boundary |
| `FourDaySessionDistanceAllocationPolicy` | one KEY, two EASY, one LONG | Numeric allocation schema | `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY` temporarily; future dispatch must key from resolved layout | RunLayout + frequency-specific allocation policy | Yes | Yes, 4D-only | Yes for non-4D dosage/allocation semantics | Formula is not a mere count assertion |
| `V1FourDaySessionVolumeAllocationPolicy` | count=4 and 1/2/1 | Prescription guard | `VALID_BOUNDARY_ASSERTION` for the explicitly 4D policy | resolved layout + selected frequency policy | Yes | Yes | Yes for new-frequency allocation policy | May say “this selected policy supports 4D”; may not say all plans are 4D |
| `CatalogWeekSkeletonCalendarMaterializer` | 4 days, 1 KEY, 2 EASY, 1 LONG | Calendar algorithm/validation | role counts `MUST_DERIVE_FROM_RUN_LAYOUT`; actual/expected checks `VALID_BOUNDARY_ASSERTION`; placement behavior `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY` | RunLayout + approved calendar policy | Yes | Yes | Yes for 3D placement and multi-KEY spacing later | Mixes structural literals with real placement semantics |
| `DatedGeneratedCatalogPlanSkeletonValidator` | fixed four sessions/roles | Dated boundary validation | `VALID_BOUNDARY_ASSERTION` | resolved layout/generated skeleton | Yes | Yes | No | Expected values must be derived, not literal |
| `CatalogFinalPrescribedPlanValidator` | fixed four per week | Final prescription validation | `VALID_BOUNDARY_ASSERTION` | generated skeleton + resolved layout | Yes | Yes | No | Defensive recheck is valid when expected count is derived |
| `PreparationRunwayWeekMaterializer` | fixed 4/1/2/1 | Runway structure | `HORIZON_SPECIFIC_AUTHORITY` for Runway semantics; literals are `CONFLICTING_AUTHORITY` | later Runway frequency authority, composed with RunLayout where applicable | Yes | Yes | Yes, deferred Runway frequency phase | GEN.2A does not redesign Runway |
| `PreparationRunwayNumericMaterializer` | fixed four-role numeric shape | Runway prescription | `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY` + `HORIZON_SPECIFIC_AUTHORITY` | Runway structure + selected frequency policy | Yes | Yes | Yes | Numeric distribution genuinely changes with layout |
| `PreparationRunwayCalendarComposer` | fixed 4D calendar | Runway calendar | `HORIZON_SPECIFIC_AUTHORITY`; derived counts must come from its future resolved structure | later Runway frequency/calendar authority | Yes | Yes | Yes | Separate subsystem, deferred |
| `PreparationRunwayCoreWeekOnePaceAdapter` | four-role Core boundary | Runway/Core adapter | `VALID_BOUNDARY_ASSERTION` | canonical Core output + resolved layout | Yes | Yes | Potentially | Adapter must validate actual canonical Core output, not invent it |
| `PreparationRunwayPaceMaterializer` | fixed role cardinality | Runway pace materialization | `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY` | Runway resolved structure/prescription | Yes | Yes | Yes | Prescription behavior may vary by role occurrence |
| `TenKPreparationRunwayFinalInvariantValidator` | fixed 4/1/2/1 | Runway final validation | `VALID_BOUNDARY_ASSERTION` | materialized Runway + canonical Core structures | Yes | Yes | No for counts | Literals must become derived expected values |
| `TenKPreparationRunwayDarkOrchestrator` frequency checks | Days=4 | Runway rollout | `TEMPORARY_ROLLOUT_GATE` | rollout policy | No | Yes | No | Scope guard, not domain truth |
| `LongHorizonGeStructuralContracts` / selector | closed 1K+2E+1L enum/map | GE structural representation | `HORIZON_SPECIFIC_AUTHORITY`; literal Core meaning cannot compete with TEN_K_MASTER | later LongHorizon frequency architecture | Yes | Yes | Yes, deferred | Separate structural subsystem; not replaced in GEN.2A |
| `LongHorizonStructuralValidator` | four roles/sessions | LongHorizon boundary validation | `VALID_BOUNDARY_ASSERTION` within current rollout; expected future values derive from LongHorizon resolved structure | LongHorizon structure + rollout | Yes | Yes | Yes for future LongHorizon layouts | Valid boundary, invalid universal literal |
| `LongHorizonCalendarAssigner` | fixed four-key dictionary | LongHorizon calendar policy | `HORIZON_SPECIFIC_AUTHORITY` + `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY` | later LongHorizon layout/calendar authority | Yes | Yes | Yes | Structurally separate; multi-frequency behavior deferred |
| `LongHorizonRealCalendarProjectionAdapter` | literal ×4 | Projection count | `MUST_DERIVE_FROM_RUN_LAYOUT`/LongHorizon generated structure | persisted/generated LongHorizon weeks | Yes | Yes | No | Pure repeated count, not policy |
| `LongHorizonActivatedCalendarAlignmentValidator` | four sessions | Activation boundary | `VALID_BOUNDARY_ASSERTION` | activated structural/calendar output | Yes | Yes | No | Expected count must be supplied/derived |
| `LongHorizonCheckpointStateEvaluator` | fixed weekly opportunity count | Checkpoint validation | structural count `MUST_DERIVE`; decision semantics may be frequency policy | persisted structural week + adaptation policy | Yes | Yes | Yes for generalized adaptation | Separates evidence shape from policy meaning |
| `LongHorizonFinalLifecycleValidator` / `LongHorizonFullExecutionValidator` | four sessions per week | Lifecycle/final validation | `VALID_BOUNDARY_ASSERTION` | persisted generated structure | Yes | Yes | No | Defensive assertion remains; literal does not |
| `WindowExecutionSummary` / builder | singular KEY/LONG, EASY counts | Adaptation evidence representation | `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY`/schema for current 4D; later representation decision required | persisted logical expectations + approved adaptation contract | Yes | Yes | Yes, explicitly deferred | Cannot derive away 2-KEY information loss |
| `NextWindowLoadDecisionPolicy` | thresholds 0/1/2/3/≥4, 1K+2E+1L | Load-decision semantics | `LEGITIMATE_FREQUENCY_SPECIFIC_POLICY` | selected frequency-calibrated adaptation policy | Yes | Yes | Yes, explicitly deferred | Calibration is real policy, not duplicate validation |
| `WeeklyWindowPartitioner` | no fixed count | Structural-week grouping | `MUST_DERIVE_FROM_RUN_LAYOUT` not applicable; already frequency-agnostic derived grouping | persisted structural-week identity | Yes | No | No | Not a duplicate authority or blocker |

Resolution rule: literal cardinalities may remain only inside an explicitly selected 4D policy or a clearly named temporary 4D rollout gate. Generic boundary validators must compare actual values with authoritative resolved/generated values.

## 8. Combination artifact semantics

```text
COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST
```

A combination may bind master, RunLayout, LevelModifier, RulePack, workout/progression references, compatible versions, publication state, and justified cross-axis references. It must not own concrete weeks, copied phase allocation, copied role structure, copied level prescription, or copied policy values already owned elsewhere. No repository/domain evidence requires independent complete-plan logic per combination.

## 9. Single canonical 10K Core decision

```text
SINGLE_CANONICAL_10K_CORE_APPROVED
```

`TEN_K_MASTER` with preferred 12-week Foundation/Build/RaceSpecific/Taper architecture remains the canonical 10K Core. Horizon allocation may compress/extend or compose it; Frequency supplies weekly roles; Level supplies approved prescription/eligibility changes. Missing Beginner/Advanced or 3D–7D data is not evidence for a different phase timeline.

## 10. 4D behavioral-equivalence invariant

```text
BEHAVIORAL_EQUIVALENCE_REQUIRED
```

Generalizing the runtime authority must preserve externally/domain-observable `10K / Intermediate / 4D` behavior unless a later explicit product decision changes it:

- Core phase allocation and structural week count for the same horizon;
- ordered weekly role layout where order is contractually consumed;
- workout/stage binding;
- numeric volume, long-run, pace, and session prescription;
- calendar dates and long-run-day semantics;
- public preview and confirmation behavior;
- persistence shape/identity semantics;
- lineage/provenance required for replay and historical integrity.

```text
INTERNAL_IMPLEMENTATION_MAY_CHANGE
```

Type names, DI/manual composition, wrappers, internal call depth, temporary comparison machinery, derived-value calculation location, and removal of redundant literal validators may change if the observable/domain result and necessary provenance remain equivalent.

## 11. Horizon-family applicability

| Horizon family | Authority decision applicability |
|---|---|
| `CORE_PATH` | Direct. Single dynamic Core skeleton authority replaces the remaining exact-12 fixed authority; RunLayout is Core frequency structure authority. |
| `RUNWAY_PLUS_CORE_PATH` | Its Core segment uses the same canonical TEN_K_MASTER and dynamic Core authority (current factory already constructs that chain). Its Preparation Runway remains a separate horizon-specific subsystem; frequency implementation is deferred. |
| `LONG_HORIZON_PATH` | Do not replace its structural/calendar system with the Core orchestrator. Its embedded/composed 10K Core meaning must still reference TEN_K_MASTER rather than independently authoring a second Core. LongHorizon frequency structure/adaptation remain later decisions. |

This is deliberate `HORIZON_PATH_DIVERGENCE`, not permission for duplicated Core architecture.

## 12. Migration principles

1. Prove and preserve 4D behavioral equivalence before retiring the exact-12 fixed authority.
2. Move structural Frequency ownership to resolved RunLayout; replace scattered universal literals with derived expectations or explicit selected-policy scope.
3. Downstream components may remain temporarily 4D-specialized only when named as a 4D rollout/policy limitation.
4. Do not activate a frequency until every required consumer in that horizon path supports the resolved layout and its required domain policies.
5. Never use a new combination key as justification to duplicate TEN_K_MASTER or a complete week-by-week plan.
6. Separate compatibility publication from live activation before broad rollout.
7. Preserve horizon-family boundaries; reuse canonical Core semantics, not necessarily identical outer orchestration code.
8. Remove temporary parallel comparison once its explicit exit criteria are met.

## 13. Named invariant set

- **GEN2A-INV-001 — One canonical 10K Core.** `TEN_K_MASTER` owns the Core phase architecture.
- **GEN2A-INV-002 — RunLayout owns structural Frequency.** Ordered slots and role counts come from the resolved layout.
- **GEN2A-INV-003 — Combination is a manifest.** It composes/version-binds references and does not own independent week-by-week plan logic.
- **GEN2A-INV-004 — Structural counts are derived.** Generated, dated, and persisted counts derive from layout/generated structure.
- **GEN2A-INV-005 — Validators do not become authorities.** Boundary validators may re-check derived invariants but may not assert universal literal 4/1/2/1.
- **GEN2A-INV-006 — Support differs from rollout.** Catalog compatibility/publication and live activation are separate authorities.
- **GEN2A-INV-007 — 4D is the regression baseline.** Generalization alone may not change observable TEN_K/Intermediate/4D behavior.
- **GEN2A-INV-008 — Horizon scope remains explicit.** Core generalization does not automatically generalize Preparation Runway or LongHorizon.
- **GEN2A-INV-009 — Frequency and Level compose with Core.** They do not trigger independently authored complete Core plans.
- **GEN2A-INV-010 — One Core skeleton runtime authority.** The dynamic orchestrator is the end-state authority; fixed logic may exist only as a temporary facade/oracle/migration guard.
- **GEN2A-INV-011 — Frequency-specific policy is explicit.** A genuine calendar, allocation, or adaptation calibration may vary by frequency, but it must consume the resolved layout and may not redefine it.
- **GEN2A-INV-012 — Cross-axis policy data is legitimate but bounded.** Distance × Level × Frequency policy rows may own their policy value, never duplicated Core/layout architecture.

## 14. Explicit deferred decisions

Not resolved here: exact 3D implementation/calendar algorithm; 3D LongHorizon adaptation calibration; second-KEY content; KEY↔KEY spacing; per-slot two-KEY progression; two-KEY `WindowExecutionSummary`; generalized `NextWindowLoadDecisionPolicy`; Beginner/Advanced/level numeric values; 5D–7D activation; Preparation Runway frequency implementation; LongHorizon frequency implementation; Core/Runway adaptation gap; Expert; 2D; doubles; cross-training.

## 15. Compact decision summary

| Decision | Result | Evidence basis |
|---|---|---|
| Core skeleton runtime authority | `CORE_SKELETON_AUTHORITY_SINGLE_DYNAMIC` | Generic typed inputs, shared dependencies, exact 12-week equality, existing live use for non-12 Core modes |
| Fixed 4D path lifecycle | Direct caller migration; temporary facade/oracle/comparison only | No fixed-only domain behavior; remaining exact-12 branch is duplicate authority |
| Frequency structural source | `RUN_LAYOUT_IS_CANONICAL_FREQUENCY_AUTHORITY` | Resolver reads ordered layout roles and declares layout count source of truth |
| Support vs rollout | Current `MIXED_RESPONSIBILITY`; future compatibility authority + rollout gate split | Bundled predicate currently answers both questions |
| Duplicate authority policy | Derive structural facts; retain derived validators; isolate rollout gates and genuine frequency policies | GEN.1 fixed-site inventory plus responsibility analysis above |
| Combination semantics | `COMBINATION_IS_COMPATIBILITY_AND_VERSION_MANIFEST` | Existing reference-oriented catalog composition; frozen architectural intent |
| Canonical Core reuse | `SINGLE_CANONICAL_10K_CORE_APPROVED` | Phase architecture is not Level/Frequency-branched |
| 4D compatibility | `BEHAVIORAL_EQUIVALENCE_REQUIRED` | Live pilot and existing exact equality proof |
| Core applicability | Direct, all supported 8–14 Core modes | Dynamic Core chain already exists |
| Runway+Core applicability | Canonical authority applies to Core segment only | Separate Runway subsystem; current factory already composes dynamic Core |
| LongHorizon applicability | Canonical Core semantics only; no orchestrator replacement | Separate, more rigid structural/calendar/adaptation system |

## 16. Files inspected

- `PHASE_10K_GEN_0_CURRENT_STATE_BASELINE.md`
- `PHASE_10K_GEN_1_ARCHITECTURE_AUDIT.md`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/DynamicCoreWeekSkeletonOrchestrator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogPlanSkeletonOrchestrator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogRunLayoutSlots.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayOrchestration/TenKPreparationRunwayComponentAdapters.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/DynamicCoreWeekSkeletonOrchestratorTests.cs`
- Load-bearing Core, Runway, LongHorizon, calendar, validation, and adaptation files enumerated in the GEN.1 report and resolved in Section 7.

## 17. Final classification

```text
10K_GEN_2A_FREQUENCY_ARCHITECTURE_AUTHORITY_APPROVED
```

The authority model is resolved. Later 3D work may begin only under the invariant set above and its own phase authorization; this phase implements nothing.
