# Phase 4G.3B.1 — Typed Phase-Constraint Loading

## 1. Scope and non-goals

This phase carries the complete catalog-authored phase constraint contract into the backend runtime
summary. It does not implement a generic allocator, make allocation target-week-aware, consume readiness,
enable another horizon, change routing, change public contracts, or change persistence.

## 2. Pre-change data-loss point

`PlanCatalogBundleLoader.LoadCandidateAsync` enumerated `TEN_K_MASTER.phases[]` but projected each element
to `new PlanCatalogPhaseAllocation(phaseKey, preferredWeeks)`. The other schema-required fields were
successfully present in JSON and then discarded at this projection. `PlanCatalogCandidateSummary` could
therefore expose only the fixed preferred allocation.

## 3. Authoritative catalog fields

`plan-catalog/schemas/plan-template.schema.json` requires these fields on every phase:

- `phaseKey`
- `minimumWeeks`
- `preferredWeeks`
- `maximumWeeks`
- `compressionPriority`
- `extensionPriority`
- `isCompressionProtected`

`extensionPriority` already existed in the authoritative schema and active artifact, so it is carried.
No new allocation-priority field was invented and no catalog artifact or schema was changed.

## 4. Typed runtime model

`PlanCatalogPhaseAllocation` now contains `PhaseKey`, `MinimumWeeks`, `PreferredWeeks`, `MaximumWeeks`,
`CompressionPriority`, `ExtensionPriority`, and `IsCompressionProtected`. JSON property names remain
inside the loader boundary. The existing two-argument constructor remains only for hand-built test
candidates; artifact loading always requires and explicitly maps every field.

## 5. Mapping path

`ten-k-master.v6.json` → `PlanCatalogBundleLoader.ReadPhaseAllocations` →
`PlanCatalogPhaseAllocation` → `PlanCatalogCandidateSummary.PhaseAllocations` → future allocator input.

`CatalogPhaseAllocationResolver` remains unchanged and continues to select only `PreferredWeeks`.

## 6. Validation rules

Loading fails with `PlanCatalogLoadException` rather than correcting, clamping, or defaulting when:

- a required field is absent or has the wrong JSON type;
- a week constraint is negative;
- `minimumWeeks <= preferredWeeks <= maximumWeeks` is false;
- a compression or extension priority is not a positive integer;
- a phase identity is outside the schema's phase vocabulary;
- a phase definition is duplicated; or
- a required pilot phase is absent.

The existing schema already requires every transported field. Requiredness and schema version are unchanged.

## 7. Exact TEN_K_MASTER v6 values

| Phase | Minimum | Preferred | Maximum | Compression priority | Extension priority | Compression protected |
|---|---:|---:|---:|---:|---:|---|
| FOUNDATION | 2 | 3 | 4 | 1 | 1 | false |
| BUILD | 3 | 4 | 5 | 2 | 2 | false |
| RACE_SPECIFIC | 2 | 4 | 4 | 3 | 3 | false |
| TAPER | 1 | 1 | 1 | 4 | 4 | true |

## 8. Cache audit result

`IPlanCatalogBundleLoader` is registered as scoped. It contains no static cache, singleton cache, lazy
initialization, or memoized artifact state. Every `LoadCandidateAsync` call enumerates catalog directories,
opens matching JSON files, and deserializes them again. Consequently catalog loading can repeat per preview
request (and more than once if separate request-scoped consumers call the loader). Loaded `JsonDocument`
instances are disposed after mapping. The returned summary is read-only by contract (`init` properties and
`IReadOnlyList`/`IReadOnlyDictionary` surfaces), though its collection backing is not a deeply immutable
collection implementation.

**NON_BLOCKING_PERFORMANCE_OBSERVATION:** catalog files are re-read and deserialized rather than cached.
This phase intentionally does not change caching or service lifetimes.

## 9. Compatibility proof

The unchanged resolver consumes the same preferred values and still produces FOUNDATION/BUILD/
RACE_SPECIFIC/TAPER = 3/4/4/1, totaling 12 weeks. Existing real-artifact orchestrator tests preserve the
same phase sequence, week skeleton, and four slots per week. Existing HTTP acceptance tests continue to
pin the standard request to 12 weeks, 48 sessions, final session `2026-10-11`, and `fallback_used=false`.
Exact 8 weeks remains `422 PLAN_CORE_HORIZON_UNSUPPORTED`; 9–11 and 13–14 remain unsupported; 15+ weeks
remains `422 PLAN_HORIZON_COMPOSITION_REQUIRED`. No preview is persisted for rejected requests.

## 10. Tests

Focused loader tests pin every field from the real TEN_K_MASTER v6 artifact and prove fail-closed handling
for negative values, invalid ordering, invalid priorities, missing metadata, duplicate phases, and missing
required pilot phases. Existing loader, allocator, orchestrator, horizon, and live HTTP acceptance families
provide compatibility coverage.

Validation results: PlanCatalog tests passed 335/335; the focused backend contract/allocator/orchestrator/
horizon suite passed 77/77. The requested real-host acceptance selection passed 4/4 (the selection also
includes the existing 14-week boundary theory row), and the dedicated 8-week error/no-persistence pair
passed 2/2. A full backend run completed 1071 tests before the
obsolete Phase 4G.3A structural assertion was updated: 1058 passed and 13 failed. One failure was that
now-corrected assertion; the other failures were unrelated shared relational/reset and local activation-
configuration failures. The focused and live acceptance reruns after correction are authoritative for this
phase's changed boundary.

## 11. Public behavior unchanged

There is no endpoint, request/response DTO, Swagger, Flutter, database, migration, persistence, routing,
readiness, activation-risk, or allocation-algorithm change. `TD-FOUNDATION-COMPRESSION-001` remains open
and documentation-only; no readiness rule consumes the new fields.

## 12. Remaining next step

Phase 4G.3B.2 may implement `GenericPhaseAllocator` against this typed contract. It has not started here.
