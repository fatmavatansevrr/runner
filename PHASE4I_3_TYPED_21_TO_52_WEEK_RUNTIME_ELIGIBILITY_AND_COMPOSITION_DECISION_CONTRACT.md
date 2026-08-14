# Phase 4I.3 — Typed 21–52 Week Runtime Eligibility and Composition Decision Contract

## 1. Executive result

The first production runtime authority for 21–52-week horizon
classification and composition is implemented: `LongHorizonCompositionResolver`,
a dark, unwired, deterministic resolver that converts an already-produced
`CoreHorizonDecision` into one typed `LongHorizonCompositionDecision`
covering the full 8-to-52-week range and encoding the Phase
4I.1/4I.2/4I.2A governance policies exactly once. It is not called from
any live request path — zero HTTP-facing behavior changed. This is
proven by new real end-to-end tests (real Api host, real Postgres)
showing 21/24/52/53-week requests all still return HTTP 422
`PLAN_HORIZON_COMPOSITION_REQUIRED` with zero persistence, identical to
before this phase.

```
LONG_HORIZON_TYPED_RUNTIME_ELIGIBILITY_AND_COMPOSITION_DECISION_CONTRACT_COMPLETED_WITH_SINGLE_AUTHORITATIVE_8_TO_52_HORIZON_CLASSIFICATION_AND_21_TO_52_GENERATION_CONTAINMENT
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_GENERAL_ENDURANCE_CATALOG_STRUCTURAL_MATERIALIZATION_NUMERIC_EXECUTION_AND_PUBLIC_PREVIEW
```

## 2. Inherited approved policies

Phase 4I.1: `GE = AvailableFullWeeks - 20`, Runway fixed 8, Core fixed 12
for 21–52; `ShortExtension` 1–3, `FullPhase` 4–32; both readiness
profiles identical durations; 53+ rejected. Phase 4I.2: four-week
mesocycles (3 development + 1 recovery), weekly-role skeleton, prohibited
intensities, `ShortExtension` structures, terminal remainder placement,
GE→Runway structural continuity, advisory-only checkpoints, no automatic
mutation, no universal 10% rule. Phase 4I.2A: `RecoveryWeekTotalVolumeKm
= Round₀.₅ₖₘ(PreviousDevelopmentPeakVolumeKm × 0.85)`,
`RecoveryWeekLongRunKm = Round₀.₅ₖₘ(RecoveryWeekTotalVolumeKm × 0.33)`,
`PREVIOUS_NON_CUTBACK_VOLUME` return baseline, both profiles identical
ratio. This phase represents that policy metadata (via provenance
fields — §12) but executes none of it numerically.

## 3. Scope and non-scope

**In scope**: one typed decision model, one resolver implementing the
Phase 4I.1 formula exactly once, a validator, and tests proving
correctness/containment. **Explicitly not implemented**: GE catalog
entries, plan-skeleton materialization, numeric workout distances,
calendar dates, public preview exposure for 21–52, confirmation,
persistence, 53+ activation. Confirmed zero files outside
`backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/`
(production) and `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/`
(tests) were created; no existing production file was modified.

## 4. Artifacts inspected

`CoreHorizonClassifier.cs`, `RaceHorizonPolicy.cs`, `CatalogPreviewGenerator.cs`
(`BuildDarkInternalDatedSkeleton`, `GeneratePreparationRunwayPreviewAsync`),
`PlanServices.cs` (the live horizon gate), `CatalogPhaseAllocation.cs`
(`CatalogPhaseAllocationResolver`, both `Resolve` overloads),
`PreparationRunwayContracts.cs` (the closest existing typed-decision
pattern), `CoreHorizonClassifierTests.cs`, `RaceHorizonPolicyAuthorityTests.cs`,
`Sw12LongHorizonFailClosedEndToEndTests.cs`, `Sw13ExactTwelveWeekOnlyEndToEndTests.cs`,
`PreparationRunwayPreview15To20WeekEndToEndTests.cs`, `Program.cs` (DI
registration conventions for both live and dark components), and the
governance artifacts `TD-LONG-HORIZON-COMPOSITION-001`/`TD-LONG-HORIZON-GE-SAFETY-001`/
`TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001`/`TD-GENERAL-ENDURANCE-STAGED-PLAN-001`.

## 5. Existing horizon authority

`CoreHorizonClassifier.Classify(CoreHorizonContext)` is the sole,
day-accurate elapsed-time authority (`AvailableFullWeeks`/`LeadingPartialDays`
derivation), consumed exclusively by `RaceHorizonPolicy.Decide`. Its
`CoreHorizonMode` enum has exactly one bucket
(`PreparationRunwayPlusCore`) for every horizon above the standalone-core
maximum (14 weeks) — it does **not** distinguish 15–20 from 21–52 from
53+ at all. `PlanServices.GeneratePreviewAsync` is the live gate: for
`CompositionRequired` classification, it either routes the exact pilot
identity within 15–20 weeks to a real preview, or throws
`PlanHorizonCompositionRequiredException` (HTTP 422) for everything else,
including every 21+ week request today, with no internal distinction
between 21, 52, or 53+.

## 6. Authority-placement decision

**Option B** from this phase's own preferred list: a single resolver
(`LongHorizonCompositionResolver`) invoked using the existing horizon
authority's already-computed output (`CoreHorizonDecision`), not a new
parallel authority and not a modification to `CoreHorizonClassifier`/
`CoreHorizonMode`/`RaceHorizonPolicy`. This was verified as safe by a
pre-existing architecture test
(`CoreHorizonClassifierTests.Classifier_HasOnlyTheCanonicalPolicyCallSite`)
that enforces exactly one production file may reference the classifier
type by name — an early implementation draft's doc comments violated this
by name-dropping the classifier in prose; both mentions were rewritten to
avoid the literal identifier, and the test now passes unchanged (§29
records this as the one real defect found and fixed during this phase).

## 7. Typed decision model

`LongHorizonCompositionDecision` (`internal sealed record`,
`LongHorizonCompositionContracts.cs`): `AvailableFullWeeks`, `HorizonPath`,
`Eligibility`, `GenerationActivationStatus`, `GeneralEnduranceWeeks?`,
`PreparationRunwayWeeks?`, `CoreWeeks?`, `GeneralEnduranceClassification`,
`ReadinessProfile?`, `ReasonCode?`, `PolicyId`, `PolicyVersion`,
`DeferredSafetyPolicyId`, `RecoveryPolicyId`, `Rules`. Every field the
prompt required is present.

## 8. Horizon path model

`LongHorizonPath` enum: `InsufficientOrExistingUnsupportedPath`,
`CoreOnly`, `PreparationRunwayAndCore`,
`LongHorizonGeneralEnduranceRunwayAndCore`, `UnsupportedAboveSupportedWindow`
— the exact five values the prompt's Part 1 requested, no duplicate
enum introduced (this is a **new** type, deliberately distinct from
`CoreHorizonMode`, since `CoreHorizonMode` is a public type with existing
external consumers this phase must not alter).

## 9. Eligibility model

`LongHorizonEligibility`: `NotApplicableBelowLongHorizonBoundary`,
`SupportedLongHorizon`, `UnsupportedAboveAnnualWindow` — matches Phase
4I.1's own originally-recorded typed shape exactly. A second, orthogonal
axis, `LongHorizonGenerationActivationStatus`
(`NotApplicable`/`EligibleButGenerationNotActivated`), was added
specifically to satisfy Part 14's explicit requirement to distinguish
"composition eligible" from "generation activated" without conflating it
with `Eligibility` itself.

## 10. GE classification

`GeneralEnduranceDurationClassification`: `NotApplicable`, `ShortExtension`,
`FullPhase`. Mapping (`GeneralEnduranceWeeks <= 3` → `ShortExtension`,
else `FullPhase`) is implemented in exactly one place (`LongHorizonCompositionResolver.LongHorizon`)
and independently re-verified by 41 tests, including a full 21–51
monotonicity loop confirming the classification changes only at the GE
3→4 boundary.

## 11. Readiness-profile handling

`ReadinessProfile` (`ConsistencyNeeded`/`CoreEntryReady`) is accepted by
`Resolve` and carried through to the output decision verbatim — it is
never read inside any allocation branch, which is itself the structural
proof that profile cannot affect duration (there is no code path where it
could). `BothReadinessProfiles_ReceiveEqualDurations` (21/24/52 weeks)
and `ProfileValueIsPreservedVerbatim` cover this directly.

## 12. Policy provenance

Every decision (regardless of path) carries `PolicyId = "TD-LONG-HORIZON-COMPOSITION-001"`,
`PolicyVersion = "PHASE_4I_1_V1"`, `DeferredSafetyPolicyId = "TD-LONG-HORIZON-GE-SAFETY-001"`,
`RecoveryPolicyId = "TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001"` — four
`internal const string` fields on `LongHorizonCompositionResolver`,
referenced once and interpolated into every branch's output, never
hardcoded per-branch. No user-facing DTO references any of this — it is
purely internal.

## 13. Authoritative formula

Implemented exactly once, in `LongHorizonCompositionResolver.LongHorizon`:

```csharp
var generalEnduranceWeeks = availableFullWeeks - 20;
// PreparationRunwayWeeks = 8 (LongHorizonPreparationRunwayWeeks constant)
// CoreWeeks = 12 (LongHorizonCoreWeeks constant)
```

No 32-case switch. Tests independently assert fixture values but never
duplicate the subtraction itself.

## 14. 8–14 compatibility

`CoreOnly` path: `CoreWeeks = AvailableFullWeeks` (the aggregate total
only — confirmed accurate by research: both the fixed 12-week preferred
allocation and the dynamic 8-11/13-14 mechanical allocator already sum to
exactly `AvailableFullWeeks` by construction). Per-phase
Foundation/Build/RaceSpecific/Taper counts remain **exclusively** owned
by the existing, completely unmodified `CatalogPhaseAllocationResolver`
— this decision never recomputes or duplicates them. Regression-tested
at 8/12/13/14.

## 15. 15–20 compatibility

`PreparationRunwayAndCore` path: `PreparationRunwayWeeks = AvailableFullWeeks - 12`,
`CoreWeeks = 12`, matching Phase 4G.6A.1 exactly. `GeneralEnduranceWeeks`
is `null` (not zero — matches the prompt's "null or zero according to
current convention," and `null` is the more honest representation of "GE
does not apply here"). Regression-tested at 15 (→3 Runway) and 20 (→8
Runway).

## 16. 20→21 boundary

20 weeks: `PreparationRunwayAndCore`, 8 Runway, 12 Core, GE `null`. 21
weeks: `LongHorizonGeneralEnduranceRunwayAndCore`, 1 GE, 8 Runway, 12
Core, `ShortExtension`. Both directly tested
(`TwentyWeeks_RemainsEightRunwayTwelveCore_NoLongHorizonGe`,
`TwentyOneWeeks_BecomesLongHorizon`/`Maps_1_8_12`/`ClassifiesShortExtension`).
The terminal 20-week structure (8 Runway + 12 Core) is bit-for-bit
identical on both sides of the boundary — the only change is the
addition of the GE prefix, matching the required continuity invariant.
No existing 20-week production code was touched.

## 17. 21–52 behavior

Full fixture matrix (21/22/23/24/28/32/40/48/52) asserted exactly.
Sum-invariant and monotonicity proven by loop across the entire 21–52
range (32 values), not just fixture points — Runway/Core proven fixed at
every single value in the range, GE proven to increase by exactly 1 per
step.

## 18. 52→53 boundary

52 weeks: eligible, 32/8/12, `FullPhase`. 53 weeks (and 60, and 1000):
`UnsupportedAboveSupportedWindow`, `UnsupportedAboveAnnualWindow`,
`ReasonCode = "PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW"`, all three
allocation fields `null` — no truncation, no fallback. This reason code
is internal-only on the new dark type; the live public HTTP reason for
53+ remains unchanged (`PLAN_HORIZON_COMPOSITION_REQUIRED`, §24).

## 19. Below-8 compatibility

`Unsupported`/`InvalidInput`/`ReadinessOnly` classifier modes all map to
`InsufficientOrExistingUnsupportedPath`/`NotApplicableBelowLongHorizonBoundary`
with every allocation field `null` — the pre-existing below-minimum
decision is preserved verbatim, not redefined
(`BelowMinimum_PreservesExistingInsufficientPath`, 7 weeks).

## 20. Runtime-condition integration

**No.** `CoreHorizonClassifier` itself is not a registered
`IRuntimeConditionResolver` — it is a static classifier consumed directly
by `RaceHorizonPolicy`, entirely outside that registry. Since
`LongHorizonCompositionResolver` sits one layer above that same
classifier's output, it correctly remains a pre-runtime-condition
authority too, for architectural consistency with its own upstream
dependency. There is still exactly one source of truth for this decision
(`LongHorizonCompositionResolver` itself) — no second computation exists
anywhere.

## 21. Traceability

Every decision carries a `Rules: IReadOnlyList<string>` field recording
the exact rule(s) applied (e.g., "21-52 available full weeks: GE =
AvailableFullWeeks - 20, Runway fixed 8, Core fixed 12 (Phase 4I.1
TD-LONG-HORIZON-COMPOSITION-001)."), plus all the structured fields
(`AvailableFullWeeks`, `HorizonPath`, segment durations, GE
classification, readiness profile, policy ID/version, reason). No public
schedule or workout data is exposed — the type is entirely internal.

## 22. Validation

`LongHorizonCompositionValidator.Validate` (fail-fast, not database
validation): for `SupportedLongHorizon` decisions, asserts weeks in
21–52, GE in 1–32, Runway exactly 8, Core exactly 12, sum invariant,
classification-matches-GE, readiness profile present, policy provenance
present. For non-eligible decisions, asserts no misleading GE allocation
and (for truly unsupported paths) no misleading Runway/Core allocation,
plus the exact required reason code for `UnsupportedAboveAnnualWindow`.
`Validator_AcceptsEveryRealResolvedDecision` (7–60 weeks) and two
adversarial hand-constructed-`with`-expression tests
(`Validator_RejectsAnImpossibleHandConstructedDecision`,
`Validator_RejectsUnsupportedDecisionMissingReasonCode`) prove both
directions.

## 23. Error/reason taxonomy

Reused: `PLAN_HORIZON_COMPOSITION_REQUIRED` (the live, unchanged public
HTTP reason for every 15+ week request today — not touched). New,
internal-only, on the dark decision type: `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW`
(53+, reusing the exact name Phase 4G.6A.1/4I.1 already recorded) and
`LONG_HORIZON_GENERATION_NOT_ACTIVATED` (21–52, matching the prompt's own
preferred naming). The two are never conflated — a dedicated test
(`GenerationNotActivatedReason_IsNeverUsedFor53Plus`) proves it. Neither
new reason is exposed to any public API today.

## 24. Public activation containment

**Proven, not merely asserted.** `LongHorizonGenerationContainmentTests`
(real Api host, real Postgres) shows 21/24/52-week requests for the
exact pilot identity still return HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`
with `error["weeks"]` null (no preview materialized) and zero
`PlanPreview`/`TrainingPlan`/`TrainingWeek`/`TrainingDay` row-count
change. 53 weeks returns the identical public reason (not a new one —
the public API doesn't yet distinguish 53+ from 21-52; only the internal
dark decision does). `LongHorizonCompositionResolver` has zero call
sites in `PlanServices.cs`/`CatalogPreviewGenerator.cs` — confirmed by
direct source inspection, not merely by the passing tests.

## 25. DI changes

**None.** `LongHorizonCompositionResolver`/`LongHorizonCompositionValidator`
are dependency-free `internal static class`es, following the exact
precedent of `PreparationRunwayDateAuthority` and
`TenKPreparationRunwayDarkOrchestratorFactory` — both dark, both
constructed via `new`/static call, neither DI-registered while dark. No
`Program.cs` change was made or needed.

## 26. Production files changed

**3 created, 0 modified.** `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/LongHorizonCompositionContracts.cs`,
`LongHorizonCompositionResolver.cs`, `LongHorizonCompositionValidator.cs`.
No existing production file (`CoreHorizonClassifier.cs`,
`RaceHorizonPolicy.cs`, `CatalogPreviewGenerator.cs`, `PlanServices.cs`,
`CatalogPhaseAllocation.cs`, `Program.cs`) was touched.

## 27. Focused tests

`LongHorizonCompositionResolverTests.cs` (new, 41 tests) +
`LongHorizonGenerationContainmentTests.cs` (new, 4 real E2E tests) = 45
new tests, plus 54 other pre-existing tests matched by the `~LongHorizon`
filter substring — **99 passed, 0 failed** on the combined focused run.

## 28. Regression tests

`Sw12LongHorizonFailClosedEndToEndTests` (21/24-week unsupported cases,
8/14-week boundary, 12-week success) and `CoreHorizonClassifierTests`
(including the architecture test this phase's first draft violated and
then fixed) were re-run as part of the full suite — unchanged, all
passing.

## 29. Backend suite

```
dotnet test RunningApp.sln --no-build
Başarısız: 0, Başarılı: 2282, Atlanan: 0, Toplam: 2282, Süre: 9m 55s
```

**2282 passed, 0 failed, 0 skipped.** One real defect was found and
fixed during this phase (not a test bug, a production doc-comment
violation of an existing architecture test — §6): two doc comments in
the new contracts/resolver files literally named `CoreHorizonClassifier`,
which an existing architecture test
(`Classifier_HasOnlyTheCanonicalPolicyCallSite`) enforces has exactly one
non-self reference in the entire `RunningApp.Application` project
(`RaceHorizonPolicy.cs`). Both mentions were rewritten to describe the
classifier without naming it; the test now passes, and this is the only
production change beyond the three new files.

## 30. Plan-catalog suite

Re-run (this phase touched `plan-catalog/artifacts/audits/activation-readiness-risks.{json,md}`
to close the governance blocker, §31):

```
dotnet test (plan-catalog/tests/PlanCatalog.Tests)
Başarısız: 0, Başarılı: 516, Atlanan: 0, Toplam: 516, Süre: 2s
```

**516 passed, 0 failed, 0 skipped** — no aggregate-count parity failure
this time (the `currentAppendOnlyStatus` sentence was updated correctly
in the same edit that added the new TD entry).

## 31. Governance status

`plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`:
added `TD-LONG-HORIZON-TYPED-RUNTIME-DECISION-001` (status `CLOSED`).
`TD-LONG-HORIZON-COMPOSITION-001`, `TD-LONG-HORIZON-GE-SAFETY-001`,
`TD-LONG-HORIZON-GE-RECOVERY-MAGNITUDE-001` all remain `CLOSED`
(unchanged). `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` remains **`OPEN`** —
this decision narrows its scope further (the typed-contract gap it
implicitly depended on is now closed) but does not close it: rolling
materialization, segment-provenance persistence, transition
orchestration, catalog/schema content, and the unproven
`HABIT_STYLE_PLANNER_REUSE` claim are all still entirely unresolved.
Aggregate updated to 23 risks total, 10 `OPEN`, 13 `CLOSED`.

## 32. Explicit non-implementation statement

No GE catalog entry, workout definition, or `eligiblePhases` schema value
was added. No week/workout skeleton was materialized. No numeric
distance/duration/pace was computed or executed (the Phase 4I.2A recovery
formula is represented as provenance metadata only — `RecoveryPolicyId`
— never executed). No calendar date was assigned. No public preview
route was added or changed for 21–52. No confirmability or persistence
path exists for any Long-Horizon plan. 53+ remains fully unsupported,
unchanged. No other candidate/distance family was touched.

## 33. Residual blockers

Everything `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` still tracks: GE
catalog/schema construction, rolling-materialization mechanics
(slice length, idempotency, duplicate-week prevention), segment-
provenance persistence shape, transition orchestration (retry/failure,
missing-evidence handling), the unproven Habit-planner-reuse claim, and
— now newly relevant — the decision of *when and how* a future phase
wires `LongHorizonCompositionResolver` into the live `PlanServices`/
`CatalogPreviewGenerator` request path (not decided by this phase;
intentionally left dark).

## 34. Final classification

```
LONG_HORIZON_TYPED_RUNTIME_ELIGIBILITY_AND_COMPOSITION_DECISION_CONTRACT_COMPLETED_WITH_SINGLE_AUTHORITATIVE_8_TO_52_HORIZON_CLASSIFICATION_AND_21_TO_52_GENERATION_CONTAINMENT
21_TO_52_WEEK_RUNTIME_ACTIVATION_REMAINS_BLOCKED_PENDING_GENERAL_ENDURANCE_CATALOG_STRUCTURAL_MATERIALIZATION_NUMERIC_EXECUTION_AND_PUBLIC_PREVIEW
```

## 35. Exact next phase

**Phase 4I.4 — Long-Horizon General Endurance Catalog and Schema
Capacity**: mirroring the Preparation Runway line's own precedent
(Phase 4G.6A.4A authored the `AEROBIC_STRENGTH` catalog content only
after the composition/coefficient/safety policy phases closed), author
the `eligiblePhases` schema value and initial GE-eligible workout
definitions needed before any structural materialization can begin —
still explicitly not wiring `LongHorizonCompositionResolver` into any
live request path, which remains a separate, later, deliberate decision.
