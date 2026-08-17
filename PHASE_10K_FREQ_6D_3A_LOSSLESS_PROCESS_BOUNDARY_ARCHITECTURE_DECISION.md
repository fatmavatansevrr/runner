# PHASE 10K-FREQ.6D.3A — Lossless Process Boundary Architecture Decision

## 1. Scope

Architecture decision only. No production implementation, schema/catalog authoring, binder/progression work, persistence migration, public DTO expansion or routing change is made.

Parents:

- FREQ.6D.2: `bb35747e08e5061c67e90f6bd31eed31c384be15`
- FREQ.6D.3 blocker assessment: `5684376dd01dd8b8dbc8d8b9e69d4af6cddbcfba`

Questions closed: the lossless Process A→B execution boundary and its projection owner. One athlete-facing recovery-cardinality decision remains open.

## 2. Existing Process A→B map

### PROCESS_A_B_CURRENT_BOUNDARY_MAP

| Type | Assembly | Producer → consumer | Visibility | Nature | Persisted / API | Profile-lossless | Version authority |
|---|---|---|---|---|---|---|---|
| `WorkoutPrescriptionProfile` | PlanCatalog.Core | Source loader/validator/projector | Public CLR, authoring assembly | Authoring | Source catalog only; not API | Yes, except recovery cardinality is unspecified | Profile metadata exact version |
| `PublishedTemplateBundle` | PlanCatalog.Contracts | `CatalogBundleAssembler` → release consumer | Public stable boundary | Resolved dependency manifest | Immutable release artifact; not app API | No profile executions today | Bundle version/hash plus exact artifact refs |
| `CatalogArtifactReference` | PlanCatalog.Contracts | Publisher → Process B | Public stable boundary | Exact trace/reference | Serialized in bundle | N/A | Exact key/version/content hash |
| `PlanCatalogCandidateSummary` | RunningApp.Application | `PlanCatalogBundleLoader` → runtime | Public application type | Local mirror/summary | Internal runtime, not persisted as a whole | No components | Exact references copied from source documents |
| `BoundCatalogSession` | RunningApp.Application | `CatalogWorkoutBinder` → prescription planner | Internal | Workout-identity binding | Not persisted/API | No prescription fields | Exact workout key/version |
| `CatalogPrescriptionSegment` | RunningApp.Application | Session planner → finalizer/public mapper | Internal | Legacy executable approximation | Indirectly mapped later | No repetitions, nested recovery or typed intensity union | None |
| `CatalogWorkoutPrescription` | RunningApp.Application | Session planner → downstream | Internal | Legacy session prescription | Indirectly mapped later | Profile-level accounting only partially representable | None |
| `GeneratedCatalogWorkoutSegmentPayload` | RunningApp.Application | Public preview materializer | Public CLR, internal generation payload | Preview/materialization transport | Can cross preview/confirmation path | Has repetition count, but not nested recovery quantity or full typed intensity | Payload schema implied by app version |
| `GeneratedCatalogPlanPayload` | RunningApp.Application | Generator → preview/confirmation | Public CLR | Generated plan transport | Preview/confirmation and persistence mapping | Not profile-lossless through segment child | Generator version/provenance |

Project direction today:

```text
PlanCatalog.Contracts  ←  PlanCatalog.Core  ←  PlanCatalog.Infrastructure

RunningApp.Application  (no PlanCatalog project reference)
RunningApp.IntegrationTests → PlanCatalog.Contracts/Core/Infrastructure (test-only)
```

Direct `RunningApp.Application → PlanCatalog.Core` is invalid because Core contains mutable authoring concepts, source validation and metadata, while Contracts is the deliberately stable Process A→B boundary.

## 3. Current loss points

- `CatalogPrescriptionSegment`: no structure-mode discriminator, repetition count, per-repetition semantics, nested recovery quantity/mode or pace/effort/HR union.
- `GeneratedCatalogWorkoutSegmentPayload`: repetition count exists, but work/recovery atomicity is lost; recovery has only a string type on a separate recovery segment.
- Application’s loader reads source documents into local summaries rather than consuming a profile-derived execution projection.
- Profile `RecoveryQuantity` does not state how many recoveries occur for N repetitions.
- Free-text intensity normalization would erase the FREQ.6D.2 discriminator and is forbidden.

## 4. Authoring/execution authority split

Classification: **EXECUTION_PROJECTION_IS_NOT_AUTHORING_AUTHORITY**.

The authoring profile owns selection-independent authored facts and validation. The execution projection contains only already-resolved immutable values plus exact provenance. It has no lookup, selection, eligibility, lane, progression, mutation or “latest version” behavior.

The projection is analogous to compiled output: duplication of values across a process boundary is not duplication of authority when the mapping is one-way, deterministic, hash-covered and non-authorable.

## 5. P1 — new Contracts execution projection

Decision: **APPROVE**.

A dedicated immutable execution-value shape in `PlanCatalog.Contracts` matches that assembly’s stated purpose: stable types Process B may legitimately consume. It does not move `WorkoutPrescriptionProfile`, validation, draft lifecycle or authoring metadata into Contracts.

Architecture tests currently use an allow-list. Adding the new resolved execution types requires an intentional allow-list update plus negative assertions that authoring types remain absent. This is a planned boundary evolution, not evidence that P1 is forbidden.

Benefits: lossless, dependency-correct, generic across frequency/level/distance, deterministic, testable, and independent of RunningApp internals. Risk: it is a published process contract and therefore needs explicit contract schema versioning and compatibility discipline.

## 6. P2 — extend GeneratedCatalogWorkoutSegmentPayload

Classification: **EXISTING_PAYLOAD_EXTENSION_WOULD_OVERLOAD_CONTRACT**.

That type is downstream preview/materialization transport, not the Process A→B catalog boundary. Adding structure mode, work-unit union, recovery-unit union, recovery count/mode, typed intensity union, profile provenance and accounting mode would create a nullable-field explosion and couple PlanCatalog publication to an application/public-preview shape.

It would also force public/confirmation compatibility decisions prematurely. P2 is rejected as the canonical boundary. A later explicit mapper may project the internal execution contract to an evolved public payload in a separately authorized phase.

## 7. P3 — PlanCatalog-side projection

Decision: **APPROVE AND COMBINE WITH P1**.

Projection belongs before the Application boundary. `PlanCatalog.Infrastructure` is the one owner because it already assembles, hashes and publishes immutable release output while depending on Core and Contracts in the correct direction.

The projector validates/resolves an exact profile and workout, creates a Contracts execution value, and includes that value in the immutable published bundle. RunningApp consumes the resolved value; it never sees Core and never resolves a profile version.

Legacy RunningApp binding remains for bundles/artifacts without execution projections. Profile-backed runtime binding consumes the exact projection and fails closed.

## 8. Final execution contract shape

Proposed Contracts shape (names frozen conceptually; exact namespace/file naming may follow repository convention):

```csharp
public sealed record ExecutableWorkoutPrescription
{
    public required int ContractSchemaVersion { get; init; }       // starts at 1
    public required CatalogArtifactReference SourceProfile { get; init; }
    public required CatalogArtifactReference SourceWorkout { get; init; }
    public required ExecutableDoseCategory DoseCategory { get; init; }
    public required DistanceAccountingMode DistanceAccountingMode { get; init; }
    public required IReadOnlyList<ExecutablePrescriptionComponent> Components { get; init; }
}

public sealed record ExecutablePrescriptionComponent
{
    public required int SequenceOrder { get; init; }
    public required WorkoutComponentType ComponentType { get; init; }
    public required ExecutableStructureMode StructureMode { get; init; }
    public required ExecutableWorkQuantity Work { get; init; }
    public ExecutableRecovery? Recovery { get; init; }
    public required ExecutableIntensityTarget Intensity { get; init; }
}

public sealed record ExecutableWorkQuantity
{
    public required ExecutableQuantityUnit Unit { get; init; }     // Seconds | Meters
    public required int Value { get; init; }                       // per repetition when Repeated
    public int? RepetitionCount { get; init; }                     // null Continuous; >=2 Repeated
}

public sealed record ExecutableRecovery
{
    public required ExecutableQuantityUnit Unit { get; init; }
    public required int Value { get; init; }
    public required int RecoveryCount { get; init; }               // explicit; never inferred N or N-1
    public required ExecutableRecoveryMode Mode { get; init; }
}

public sealed record ExecutableIntensityTarget
{
    public required ExecutableIntensityMode Mode { get; init; }    // PaceBased | EffortBased | HeartRateBased
    public required string DescriptorKey { get; init; }            // already validated against Mode
}
```

No selection/resolution method belongs to these types. Enum plus mode-constrained fields matches current repository record/enum conventions better than a polymorphic serializer hierarchy.

## 9. Field projection table

| Profile field | Classification | Boundary handling |
|---|---|---|
| Profile key/version/hash | `MAY_PROJECT_FOR_TRACEABILITY` and selected as required provenance | Exact `SourceProfile`; never used to choose a version |
| WorkoutDefinitionRef | `MAY_PROJECT_FOR_TRACEABILITY` and selected as required provenance | Exact `SourceWorkout` including hash |
| Catalog lifecycle/schema metadata | `MUST_NOT_PROJECT` | Authoring/publish concern |
| DoseCategory | `MUST_PROJECT` | Resolved prescription semantic; not a structural role |
| DistanceAccountingMode | `MUST_PROJECT` | Downstream accounting contract |
| Component sequence/type | `MUST_PROJECT` | Deterministic skeleton correspondence |
| StructureMode | `MUST_PROJECT` | Continuous/repeated distinction |
| Work quantity/unit | `MUST_PROJECT` | Exact executable value |
| RepetitionCount | `MUST_PROJECT` for repeated | Never flattened |
| Recovery quantity/unit/mode | `MUST_PROJECT` for repeated | Nested, atomic |
| Recovery cardinality | `MUST_PROJECT`, but missing from current authoring | Required explicit source amendment/domain closure |
| Intensity mode/descriptor | `MUST_PROJECT` | Typed discriminator retained |

## 10. Profile traceability

Runtime receives exact profile and workout identity/version/content hash for debugging, replay and audit. It must not receive a key-only query or any resolution service. Traceability is a statement that “X vN produced this immutable value,” not runtime selection authority.

## 11. Recovery representation

Decision: **ONE repeated executable component with nested recovery**.

This preserves authored component atomicity, WorkoutDefinition skeleton correspondence, sequence ordering, round-trip/debugging and future rendering. It prevents an invented recovery segment from masquerading as a structural component and avoids changing Taper or adaptation semantics.

Flattening may occur only at a later display renderer as a derived view; it is not the canonical execution representation.

## 12. Recovery cardinality

Finding: the current FREQ.6D.2 authoring contract has `RepetitionCount` and one `RecoveryQuantity`, but no `RecoveryCount`, `RecoveryAfterLastRepetition`, or equivalent.

Therefore `4 × 1000m + 400m jog` does not specify whether total recovery is `3 × 400m` or `4 × 400m`. Both are structurally valid training prescriptions with different athlete-facing work and total distance.

Classification: **RECOVERY_CARDINALITY_DOMAIN_DECISION_REQUIRED**.

No global N or N−1 default is authorized. The lossless execution contract must carry an explicit positive `RecoveryCount`; the authoring contract must first gain an approved exact source semantic. Whether the source uses explicit count or an equivalent placement discriminator is a follow-up design detail, but it must resolve to an exact count before projection.

## 13. Distance accounting

The projector does not compute session totals. It preserves:

- profile `DistanceAccountingMode`;
- work unit/value and repetition count;
- recovery unit/value and explicit recovery count;
- component order/type.

A downstream accounting authority may compute distance contributions only when the quantities are distance-based and according to the selected accounting mode. Work contribution is `repetitionCount × per-repetition work distance`; recovery contribution is `recoveryCount × recovery distance`. Warm-up/cool-down remain their own components. Duration values are never silently converted to distance.

This formula is not implementation-ready until recovery count is authored explicitly.

## 14. Intensity projection

Use enum plus one required descriptor key. The projector maps the validated profile discriminator to the execution discriminator one-to-one:

- PaceBased → pace descriptor;
- EffortBased → effort descriptor;
- HeartRateBased → heart-rate zone descriptor.

It performs no pace science, numeric resolution or normalization. RunningApp may later resolve a descriptor through the existing authorized pace/intensity policy, while the original mode/key remains intact.

## 15. Legacy coexistence

```text
Published bundle has exact execution projection
    → profile-backed lossless path
    → RunningApp consumes Contracts execution value
    → common session/finalization orchestration

Published bundle has no execution projection
    → current legacy loader/binder/planner path unchanged
    → common session/finalization orchestration

Projection declared but missing/invalid/unsupported
    → fail closed; never legacy fallback
```

The convergence point is downstream orchestration, not `CatalogPrescriptionSegment`: profile-backed execution values must not first be degraded into the legacy segment type. Shared finalization may consume a discriminated internal session source or be extended generically after 6D.3B/3C.

## 16. Public containment

Classification: **INTERNAL_ONLY_FOR_FREQ6D**.

- Earliest boundary: immutable Process A published bundle consumed internally by Process B.
- Persistence boundary: FREQ.6D.4’s already-planned exact profile lineage; no execution component persistence is added here.
- Public boundary: current preview/API remains unchanged through 6D.3/6D.4. A future explicit public projection decision is required before richer fields are exposed.

`PlanCatalog.Contracts` is a public CLR assembly but is not the consumer-facing RunningApp API.

## 17. Persistence analysis

| Value | Classification |
|---|---|
| Exact profile/workout identity/version | `MUST_PERSIST_FOR_REPLAY` under previously planned lineage |
| Content hashes / bundle identity | `MUST_PERSIST_FOR_HISTORICAL_STABILITY` or remain reachable through immutable plan/bundle provenance |
| Execution components | `RECOMPUTABLE_FROM_PINNED_CATALOG` |
| Structure/work/recovery/intensity | `RUNTIME_ONLY` projection, recomputable |
| Display strings | `DISPLAY_ONLY` |

Pinned identity is sufficient only if immutable historical release artifacts remain available. Existing release directories and content hashes establish that governance intent; deletion/garbage collection without archival would break replay and must remain forbidden or separately mitigated.

## 18. Projection owner

Selected sole owner: **PlanCatalog.Infrastructure publish-side `WorkoutPrescriptionExecutionProjector` (conceptual name)**.

Inputs are already source-validated exact Core objects. Output is a Contracts execution value included in the hash-covered immutable bundle. The mapper is pure and one-way. RunningApp does no authoring projection and references Contracts, never Core.

## 19. Failure semantics

| Failure | Earliest owner | Runtime defense |
|---|---|---|
| Missing exact profile/workout | Source graph / bundle assembly | Reject missing projection; no fallback |
| Profile/workout mismatch | FREQ.6D.2 validator | Projector rechecks impossible state and fails |
| Unsupported structure/intensity/recovery | Schema/source validator | Contract deserializer rejects unknown discriminator |
| Missing recovery cardinality | New source/publish validation | Projection cannot be emitted |
| Unsupported execution contract version | N/A at source | RunningApp typed fail-closed error |
| Corrupt/hash-mismatched projection | Publisher/release verification | RunningApp rejects bundle/projection |

## 20. Contract versioning

`ContractSchemaVersion` starts at 1 and is independent of Profile version. A Profile v7 may project to Contract v1. Contract version changes only for incompatible execution/transport shape or semantics. Additive compatibility still requires explicit reader tests; unsupported versions fail closed.

## 21. Architecture tests

Implementation must prove:

- RunningApp.Application references PlanCatalog.Contracts but not Core/Infrastructure.
- Core authoring profile and validation types remain absent from Contracts.
- Contracts execution values have no selectors, repositories, “latest,” “nearest,” eligibility, lane or mutation APIs.
- Projector is PlanCatalog-side, deterministic and pure; same exact inputs produce canonical-identical output/hash.
- Projection retains every typed quantity/discriminator and exact provenance.
- Unsupported contract version and corrupt/missing exact projection fail closed.
- No type/name contains 5D, KEY1, KEY2, Intermediate or Beginner.
- No RunningApp public API/preview type gains a dependency on the new contract.
- Legacy bundles deserialize and execute unchanged.
- The published-boundary allow-list is expanded only for the exact resolved execution-value types.

## 22. Option comparison matrix

### PROCESS_A_B_PRESCRIPTION_PROJECTION_OPTION_MATRIX

| Option | Lossless | Authority purity | Dependency direction | Backward compatibility | Public leakage risk | Persistence impact | Generic | Complexity | Migration | Recommended |
|---|---|---|---|---|---|---|---|---|---|---|
| P1 new Contracts execution value | Yes with recovery count | High | Correct | Additive bundle field/path | Low if kept off API | Trace identity only | Yes | Medium | Bundle reader addition | **Yes** |
| P2 extend Generated payload | Possible but overloaded | Low/medium | App-owned downstream shape | Risky nullable expansion | High | Entangles preview | Generic but app-coupled | Medium/high | Public reader risk | No |
| P3 PlanCatalog-side projection | Yes with P1 | Highest | Correct | Legacy path preserved | Low | Projection recomputable | Yes | Medium | Publisher addition | **Yes, combined with P1** |
| Dedicated new boundary assembly | Yes | High | Could be correct | Additive | Low | Same as P1 | Yes | High | New project/deployment | No; no repository need |
| Backend-local mirror/adapter | Technically | Low; duplicate shape | Avoids Core but duplicates authority | Additive | Medium | App-local | Yes | Low initially/high drift | Continuous sync | No |

## 23. Selected architecture

**P1 + P3**:

1. Define a new immutable, versioned, lossless resolved execution-value contract in `PlanCatalog.Contracts`.
2. Keep `WorkoutPrescriptionProfile` and all authoring validation in Core.
3. Project on the PlanCatalog Infrastructure/publish side after exact source validation.
4. Include exact projected values and source provenance in the immutable, hash-covered published bundle.
5. RunningApp.Application references/consumes Contracts only and performs no profile resolution.
6. Keep profile-backed execution internal; do not route through lossy legacy segments or public payloads.

## 24. Revised implementation sequence

```text
6D.3A  architecture decision (this document)
   ↓
6D.3A.1 recovery-cardinality product/domain closure
   ↓
6D.2A  authoring-contract amendment: explicit recovery cardinality + validation/schema tests
   ↓
6D.3B  Contracts execution shape + bundle field/version + architecture/serialization tests
   ↓
6D.3C  PlanCatalog-side projector + immutable bundle integration + exact/hash/failure tests
   ↓
6D.3D  RunningApp internal consumer/materialization + legacy coexistence + focused regressions
   ↓
6D.4   dual-KEY progression/runtime integration, severity widening and persistence lineage
```

No temporary duplicate DTO is permitted while these steps land.

## 25. Open-domain-decision check

One open question changes the athlete-facing workout and accounting:

> For each repeated profile, how many recoveries occur relative to the authored repetition count—after every repetition, only between repetitions, or an explicitly authored count/placement?

This affects total duration/distance, final recovery behavior, rendering and dose. Current FREQ.6D.2 data cannot answer it.

Result: **DOMAIN_DECISION_REQUIRED / RECOVERY_CARDINALITY_DOMAIN_DECISION_REQUIRED**.

No other open architecture question requires reinterpretation of component atomicity, intensity or distance-accounting ownership. Actual production prescription values remain later catalog-authoring work.

## 26. Final classification

**FREQ6D3A_ARCHITECTURE_APPROVED_WITH_DOMAIN_BLOCKER**

The Process A→B architecture is stable and approved. FREQ.6D.3 implementation and FREQ.6D.4 are not ready until recovery cardinality is explicitly resolved and added to the authoring contract.
