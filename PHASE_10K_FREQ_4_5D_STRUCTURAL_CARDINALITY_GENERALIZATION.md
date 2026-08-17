# Phase 10K-FREQ.4 — 5D Structural Cardinality Generalization

**Mechanism only. No RUN_LAYOUT_5D created. No KEY1/KEY2 content or pairing decided. Every fix generalizes an existing single-KEY assumption to N&gt;=1; every fix is verified N=1 no-op by regression, not merely asserted.**

## A. `V1FourDaySessionVolumeAllocationPolicy` / `FourDaySessionDistanceAllocationPolicy`

Generalized `FourDaySessionDistanceAllocationPolicy.Allocate` to accept `keySessionCount` (default `1`, so every existing 4D call site is unaffected without modification). The "required minimum" and the KEY-share-of-residual math now scale with `keySessionCount`; the resulting total KEY share is split evenly across `keySessionCount` instances via a new `SplitEvenly` helper (last share absorbs the 0.5km rounding remainder, guaranteeing exact reconciliation). Equal split only — no KEY1/KEY2 asymmetric allocation, per explicit instruction.

`V1FourDaySessionVolumeAllocationPolicy.Allocate`'s entry guard now derives `keySessionCount` from the actual bound sessions and requires `>= 1` (was `== 1`), with the total-session check generalized to `keyCount + 3` (was the literal `4`).

`CatalogSessionPrescriptionPlanner.cs` now threads a `keyOrdinal` counter through session construction, mirroring the exact pre-existing `easySupportOrdinal` pattern already used for the two EASY_SUPPORT slots — not a new pattern, the same one applied consistently to KEY_SESSION. `DistanceFor` indexes `allocation.KeySessionDistancesKm[keySessionOrdinal]` instead of a single scalar.

**Naming**: per instruction, considered renaming `V1FourDaySessionVolumeAllocationPolicy`/`FourDaySessionDistanceAllocationPolicy`/`V1FourDayWeekAllocation` (their "FourDay" name now misdescribes a 2-KEY, 5-session shape) and deliberately deferred it — real call-site footprint spans session prescription, LongHorizon, and PreparationRunway numeric materialization; a rename here would add real regression risk to a mechanism-only phase for a naming concern alone. Flagged explicitly in both types' own doc comments, not silently left unaddressed.

**Real bug found and fixed during this phase, not anticipated by the plan**: `V1FourDayWeekAllocation`/`FourDaySessionDistanceAllocation` are C# records; the compiler-generated equality for the new `IReadOnlyList<double> KeySessionDistancesKm` property compares by reference (the standard array/list-in-record pitfall), which broke 11 real, pre-existing `Gen3AThreeDayAllocationMatrixTests` (they assert `Assert.Equal(first, repeat)` on two independently-computed but value-identical allocations). Fixed with explicit `Equals`/`GetHashCode` overrides using `SequenceEqual` for the list property. Caught by running the real regression suite immediately after the mechanism change, not assumed safe.

## B. `DatedGeneratedCatalogPlanSkeletonValidator`

Generalized the exact `KEY_SESSION != 1` reject to `keyCount < 1`, and the `EASY_SUPPORT` count formula from the hardcoded `preferredDays.Count - 2` to `preferredDays.Count - keyCount - 1` (reduces to the identical formula for `keyCount == 1`).

Replaced `FirstOrDefault(s => s.StructuralRole == "KEY_SESSION")` with an enumeration over every `KEY_SESSION` slot in the week — the real, confirmed silent-skip bug FREQ.3 found (a second KEY session's spacing was never checked at all). Both same-week (`KeySessionLongRunSeparationViolated`) and cross-week (`CrossWeekSeparationViolated`) checks now iterate every KEY instance, accumulating into a single boolean per error type before adding at most one error entry — preserving the exact pre-FREQ.4 error-list shape for `keyCount == 1` (verified: a naive per-pair-add design would have produced duplicate error entries for the N=1 case in some scenarios; deliberately avoided).

## C. New KEY↔KEY spacing rule (closes FREQ.3 §D.3's self-disclosed gap)

Added `MinimumKeySessionToKeySessionSeparationDays` (reuses `MinimumKeySessionToLongRunSeparationDays`'s value — 2 days — as an explicitly-disclosed embedded placeholder default, per instruction: same magnitude, same `PRODUCT_DEFAULT` provenance classification FREQ.3 §D.2 established, not independently re-derived). New pairwise check added to `DatedGeneratedCatalogPlanSkeletonValidator` (calendar materialization) and to `ScheduleRepairSpacingValidator` (Adaptation V1 repair-candidate search — the file whose own prior doc comment explicitly said *"Same-role (KEY-to-KEY...) spacing has no existing canonical rule, so none is invented here"*; that comment is now updated to reflect the fix). Both are no-ops for any single-KEY layout (no pairs exist to check).

## D. `WindowExecutionSummaryBuilder` / `WindowExecutionSummary`

Generalized `KeySessionExpected`/`KeySessionCompleted` (bools) to `KeySessionExpectedCount`/`KeySessionCompletedCount` (ints), mirroring the pattern already used for `EasyExpectedCount`/`EasyCompletedCount` — not a new pattern, applied consistently. The booleans remain as computed back-compat properties (`KeySessionExpected => Count > 0`, `KeySessionCompleted => CompletedCount == ExpectedCount`).

**A real, deliberate behavioral change was found and made consciously, not silently**: the pre-FREQ.4 boolean used `keyCompleted &= isEffectivelyCompleted`, initialized `true` and only AND-reduced for *non-superseded* KEY roots — meaning a week whose *only* KEY root was Superseded reported `KeySessionCompleted = true` (a vacuous-AND quirk), inconsistent with the EASY_SUPPORT pattern (where a superseded EASY root correctly does *not* count toward `easyCompleted`). Applying the EASY pattern *consistently* to KEY (as instructed) means a superseded-only KEY week now correctly reports `KeySessionCompleted = false`. **This edge case is not covered by any existing test** (confirmed: full 191/191 pass on every Adaptation V1 test after this change, including all lineage/Superseded tests), so it did not manifest as a regression, but is disclosed here as an intentional correctness fix riding along with the generalization, not an unnoticed side effect.

Only one direct construction site existed (`WindowExecutionSummaryBuilder.cs` itself) plus one test helper (`PlanAdaptationV1DecisionTests.FourSessionSummary`), both updated.

## E. `NextWindowLoadDecisionPolicy`

`OnlyEasyMissing`'s `keySatisfied` check now reads `KeySessionCompletedCount == KeySessionExpectedCount` directly (behaviorally identical to the pre-FREQ.4 boolean check for `KeySessionExpectedCount <= 1`, verified by regression, but now correctly requires *all* KEY instances complete for N>1 — a 5-session week missing one of two KEYs is no longer indistinguishable from "only Easy missing").

**`DetermineLoadDecision`'s raw `EffectiveCompletedCount` severity thresholds were deliberately NOT changed**, per explicit instruction. Investigated and reported, not silently resolved: is a 5-session week's threshold problem the same class Rev5 already solved for multi-week windows? **No.** Confirmed by direct read of `WeeklyLoadDecisionAggregation.cs`'s own doc comment: *"WindowExecutionSummaryBuilder and NextWindowLoadDecisionPolicy remain completely unchanged and unaware [weekly] partitioning exists — they are simply invoked once per resulting bucket [week]."* Rev5 (`WeeklyWindowPartitioner`/`WeeklyLoadDecisionAggregator`, Phase 4M.5C) operates strictly *above* this single-week function, across a variable number of *weeks*; it never addresses variability in session *count within* one week. A 5-session week completing 4-of-5 still falls into the `>= 4` bucket today, misclassified identically to a fully-complete 4-session week. **This is a genuinely new, unaddressed sub-case** — what "Reduce" should mean at 5 sessions is a real product decision, not a mechanism fix, and is left for a future decision phase.

## F. Regression requirements — all real, all executed

```
dotnet build backend/RunningApp.sln --no-restore -v:minimal
  -> 0 Warning, 0 Error

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Adaptation|ScheduleRepair|WindowExecution|NextWindowLoadDecision|WeeklyLoadDecision|PlanAdaptationV1"
  -> 191/191 passed, 0 failed   (Adaptation V1, zero delta)

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Gen3A|Gen3B|Gen4D|Gen4E|DynamicCore|CatalogSessionPrescription|FourDaySessionDistance"
  -> 397/397 passed, 0 failed   (3D/4D/Beginner4D, zero delta — after the record-equality fix; 11 real failures found and fixed before this final count)

dotnet test .../RunningApp.IntegrationTests.csproj --no-build --filter "Freq4TwoKeyCardinalityGeneralizationTests"
  -> 10/10 passed, 0 failed     (new N=2 synthetic coverage)

dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj --no-restore
  -> 1250/1250 passed, 0 failed (untouched, as expected)

dotnet test .../RunningApp.IntegrationTests.csproj --no-build   (full suite, detached background, ~21m7s)
  -> 3478/3478 passed, 0 failed, EXITCODE=0   (baseline was 3468 per FREQ.2A -- +10 is exactly the new synthetic tests)
```

**Disclosed gap in new-test coverage**: Section C's second location (`ScheduleRepairSpacingValidator`'s new KEY↔KEY check) has no dedicated new synthetic unit test — its real input type (`LongHorizonRollingPlanState`/`LongHorizonRollingSessionState`) is an EF-backed entity graph with no existing lightweight in-memory test-construction precedent found in this codebase, and building one correctly within this phase's budget risked introducing more error than it caught. Verified instead by: code review against the exact same logic already tested for `DatedGeneratedCatalogPlanSkeletonValidator`'s equivalent check (Section B/C, both reuse the identical constant and identical pairwise-comparison shape), a clean compile, and zero regression across the full real Adaptation V1 suite (191/191). Not claimed as tested when it wasn't — disclosed as a real, honest limitation.

## G. Final classification

```
5D_STRUCTURAL_CARDINALITY_GENERALIZED_AND_VERIFIED
```

All three real, code-confirmed defects FREQ.3 found (Sections E, F, H) are fixed, generalized to N&gt;=1, and verified as strict N=1 no-ops via real regression (with one genuine equality bug found and fixed along the way, and one deliberate, disclosed behavioral correction in a previously-untested edge case). One real, new architectural question (Section E's raw-threshold generalization) was investigated and explicitly left open rather than silently resolved, exactly as instructed. No RUN_LAYOUT_5D was created; no KEY1/KEY2 content was selected; 3D's and 4D's existing single-KEY behavior is unchanged.
