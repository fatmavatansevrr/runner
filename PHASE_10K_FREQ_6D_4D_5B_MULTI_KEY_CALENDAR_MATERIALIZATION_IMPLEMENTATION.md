# Phase 10K-FREQ.6D.4D.5B — Multi-Key Calendar Materialization Implementation & Second Public-Activation Attempt

**Implementation phase. `CatalogWeekSkeletonCalendarMaterializer` generalized to multi-KEY_SESSION weeks, enforcing the frozen FREQ.6D.4D.5A rule, and proven end-to-end against the real dark Intermediate×5D candidate for every supported horizon. Public activation was retried and reverted a second time: real E2E testing found an independent, calendar-unrelated blocker (`CatalogPrescriptionContextValidator`'s hardcoded `TAPER_SHARPEN` stage-key check). `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic.**

## 1. Preflight

`PHASE_LEDGER.md` row 78: `FREQ.6D.4D.5A`, `EVIDENCE + PRODUCT_DECISION`, `DONE`, `INTERMEDIATE_5D_KEY_SESSION_CALENDAR_SEPARATION_APPROVED`, confirmed. `PHASE_10K_FREQ_6D_4D_5A_KEY_SESSION_CALENDAR_SEPARATION_DECISION.md` read in full; commits `86e78d6` and `2d7215c` confirmed reachable from HEAD (`git merge-base --is-ancestor`). Starting `HEAD` = `2d7215c`, `origin/main` = same, ahead/behind `0/0`, `git diff --check` clean, working tree clean except the two pre-existing unrelated `plan-catalog/artifacts/audits/*` files and `baseline_tmp`. No pre-assigned phase ID existed in `MASTER_ROADMAP.md`'s `[Next, not yet scheduled]` line; this phase uses `FREQ.6D.4D.5B`, matching this engagement's own `<parent>.<letter>` sub-split convention.

## 2. 5A authority (re-verified, not reconstructed from chat)

- Frozen rule: `MinimumKeySessionToKeySessionSeparationDays = 2` — `abs(KEY1.Date − KEY2.Date).Days >= 2`, calendar-date difference, not clock-hours.
- Symmetric, phase-invariant (including Taper), generic for any N≥1 KEY_SESSION count, independent of `LaneOrdinal` chronology.
- Existing tie-break authority confirmed real (already used for `EASY_SUPPORT`): ascending `SlotOrderInWeek` → ascending assigned date, for repeated same-role slots.
- Existing KEY↔LONG authority confirmed unchanged: `MinimumKeySessionToLongRunSeparationDays = 2`, owned by `DatedGeneratedCatalogPlanSkeletonValidator`.
- Both constants are `internal const` on `DatedGeneratedCatalogPlanSkeletonValidator` — the single, canonical calendar-safety authority this split's materializer changes reuse by reference, never redeclare.

## 3. Existing materializer defect (reproduced before any change)

`CatalogWeekSkeletonCalendarMaterializer.ValidateSkeletonRoleStructure` rejected any skeleton with `DaysPerWeek is not (3 or 4)` unconditionally, and `keyCount != 1` separately — both reject a real 5D skeleton outright. Reproduced via `Materialize_FiveDayTwoKeySkeleton_NoLongerThrowsRoleStructureInvalid` (now green; the test's own name documents the pre-fix behavior) and, at the full-pipeline level, via the same real HTTP 500 already disclosed in `FREQ.6D.4D.5`.

## 4. Root cause (confirmed by direct code read, not assumed)

The materializer's date-assignment data model was a scalar, not a collection: `chosenKeySessionDates` was `DateOnly?[]` — **one date per week**, not one date per KEY_SESSION slot. `BuildDatedWeek` assigned that single scalar to every slot whose `StructuralRole == "KEY_SESSION"` — for a 2-KEY week this would have silently collided both slots onto the identical date had the earlier count-guard been removed without this fix. The role-count validator was a separate, additional, simpler defect (an exact `== 1`/`is not (3 or 4)` literal comparison). Both are the same root cause the FREQ.6D.4D.5 report identified; nothing new was found here — this split implements the already-diagnosed fix.

## 5. Files inspected

`CatalogWeekSkeletonCalendarMaterializer.cs` (full read, pre- and post-edit); `DatedGeneratedCatalogPlanSkeletonValidator.cs` (full read — confirmed `MinimumKeySessionToLongRunSeparationDays`/`MinimumKeySessionToKeySessionSeparationDays` are `internal const`, safely referenceable); `ScheduleRepairSpacingValidator.cs` (confirmed already reuses both constants by reference — Adaptation V1's repair-candidate search needed no change); `CatalogCalendarAssignmentFixtures.cs` (confirmed `BuildSkeleton` already accepts an arbitrary `slotRoleOrder`, no fixture change needed); `CatalogWeekSkeletonCalendarMaterializerTests.cs` (full read — one pre-existing fixture needed a mechanical update, §6); `DynamicCoreWorkoutBindingOrchestrator.cs` (full read — the real skeleton→stage-allocation→calendar→binding composition, reused directly for the real dark 5D proof); `CatalogPrescriptionContextBuilder.cs`/`CatalogPrescriptionContextValidator` (read after the second revert, to diagnose the new blocker — §21).

## 6. Files changed

**Calendar materializer** (1 file):
- `CatalogWeekSkeletonCalendarMaterializer.cs` — `ValidateSkeletonRoleStructure` generalized (`DaysPerWeek is not (3 or 4 or 5)`, `keyCount < 1`, `expectedEasy = DaysPerWeek - keyCount - 1`, mirroring `FREQ.4`'s identical generalization of the separate output validator). `WeekPlan` gained a `KeyCount` field. The private duplicate `MinimumKeySessionToLongRunSeparationDays` constant was removed; both rules now reference `DatedGeneratedCatalogPlanSkeletonValidator`'s constants directly (single numeric authority, §11 of the originating prompt). New `Combinations`/`AllPairsSatisfyKeyToKeySeparation` helpers. `TryAssignKeySessionDates` generalized from a scalar-per-week backtracking search to a `keyCount`-sized-combination-per-week search (degenerates exactly to the original single-candidate loop for `keyCount == 1`). `BuildDatedWeek` generalized to assign the `keyCount` chosen dates, sorted ascending, to the `keyCount` KEY_SESSION slots in ascending `SlotOrderInWeek` order (the same tie-break already used for `EASY_SUPPORT`). Doc comment rewritten to document the generalization.

**Test-fixture maintenance** (1 file):
- `CatalogWeekSkeletonCalendarMaterializerTests.cs` — one `InvalidRoleCompositions` case (`KEY_SESSION, KEY_SESSION, EASY_SUPPORT, LONG_RUN`) is no longer structurally invalid post-generalization (2 KEY + 1 EASY + 1 LONG in 4 days is now a legitimate cardinality, even though no real catalog layout uses it); replaced with a case that remains invalid regardless of `keyCount` (`KEY_SESSION, KEY_SESSION, LONG_RUN, LONG_RUN` — wrong `LONG_RUN` count).

**New tests** (2 files):
- `CatalogWeekSkeletonCalendarMaterializerMultiKeyTests.cs` — 14 unit-level tests against synthetic 5-slot fixtures: defect reproduction, Tue/Wed-shaped forced consecutive rejection, Tue/Thu minimum-legal acceptance, Tue/Fri larger-separation acceptance, independent KEY↔LONG enforcement per instance, no-role-collapse, deterministic slot-order→date-order tie-break, lineage preservation, EASY_SUPPORT adjacency unchanged, no-legal-assignment typed failure, determinism (including reversed input order), validator defense-in-depth, and a structural proof that no second numeric literal exists on the materializer type.
- `Freq6D4D5BReal5DDarkPlanTests.cs` — 13 tests against the REAL `TEN_K__5D__INTERMEDIATE v1` candidate (loaded via `LoadForInternalDryRunAsync`, no public-routing consultation), through the real `DynamicCoreWorkoutBindingOrchestrator` composition: 2-distinct-KEY-dates + spacing for 8/10/12/14 weeks, lane-not-derived-from-date proof with real `LaneOrdinal` values from the real binder, determinism, real Taper two-KEY-week spacing, 4 of the 5 representative FREQ.6D.4D.5A §9 preferred-day patterns succeeding against the real materializer, a genuine no-legal-assignment typed-failure case, and one new, honestly-disclosed finding (§7).

**Public-routing (attempted, reverted)** (2 files, net near-zero):
- `V1CatalogPilotIdentityPolicy.cs` — widened then reverted a second time; net change is documentation only (the `FiveDayCandidateKey`/`FiveDayCandidateVersion` doc comment now records the new, independent blocker found — §21).
- No test files retain public-5D-activation assertions; the `Gen5DIntermediatePublicActivationTests.cs` file written during the attempt was deleted after the revert (tests a capability that does not exist).

## 7. Shared calendar-policy authority

No new shared type was introduced. `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays`/`MinimumKeySessionToKeySessionSeparationDays` (both pre-existing, `internal const`) are now the sole numeric authority for both rules across all three consumers: the validator itself, `ScheduleRepairSpacingValidator` (unchanged, already referenced them since `FREQ.4`), and `CatalogWeekSkeletonCalendarMaterializer` (this split — previously had its own private, independently-drifting duplicate of the KEY↔LONG value only). Proven via `KeyToKeySeparationConstant_IsOwnedByTheValidator_NotDuplicated` (reflection-based: zero `const int` fields remain on the materializer type).

## 8. Search/backtracking design

Generalized, not 5D-specific: `TryAssignKeySessionDates` recurses per week (unchanged control flow), and for each week enumerates `Combinations(candidates, keyCount)` — ascending-index `k`-subsets of the existing, unchanged candidate-ranking order (descending distance from LONG_RUN, then ascending date — the pre-existing "materializer authority" FREQ.6D.4D.5A §14 permitted reusing instead of raw chronological order). Each combination is filtered by `AllPairsSatisfyKeyToKeySeparation` (pairwise, generic — not `if keyCount == 2`) before the existing cross-week LONG_RUN checks run against every date in the combination (generalized from a single scalar check). For `keyCount == 1` this reduces to the original algorithm exactly: one candidate tried at a time, in the original order, with the KEY↔KEY filter a no-op (no pairs exist).

## 9. Slot identity

No new "KEY1"/"KEY2" structural role was introduced. Slot identity remains `StructuralRole` + `SlotOrderInWeek` (existing fields); the materializer assigns dates to already-existing distinct slots, ordered by their own `SlotOrderInWeek`, and never manufactures new lane/slot identity. `LaneOrdinal` is not read, written, or referenced anywhere in this file — it is fixed upstream by the binder (Split A) before this class ever runs, confirmed unaffected by `RealFiveDayCandidate_LaneOrdinalNotDerivedFromDate_FirstCanonicalSlotAlwaysEarlierDate`.

## 10. KEY↔KEY enforcement result

Enforced pairwise for every KEY_SESSION pair in a week via `AllPairsSatisfyKeyToKeySeparation`, generic over `keyCount` (loops `C(n,2)` pairs, not hardcoded to 2). Proven: `TueWed_ConsecutiveKeyDates_RejectedWhenNoOtherLegalPairExists` (forced adjacency → typed failure), `TueThu_MinimumLegalSeparation_IsAccepted` (exactly 2 apart → accepted), `TueFri_LargerSeparation_RemainsValid_NotEqualityOnlySeparation` (3 apart → accepted, confirming `>=`, not `==`).

## 11. KEY↔LONG preservation result

Unchanged rule, unchanged constant, now enforced independently for every KEY instance (was already implicitly true for `keyCount==1`; explicitly generalized for `keyCount>1` in both the same-week and cross-week checks). Proven by `KeyToLongSeparation_EnforcedIndependentlyForBothKeyInstances_NotOnlyTheFirst` (6-week synthetic run) and the real dark 5D 8/10/12/14-week tests.

## 12. PreferredDays result

Unchanged authority (`ValidatePreferredDays` untouched). No session is ever moved outside `PreferredDays` to satisfy KEY↔KEY separation — confirmed structurally (the search only ever selects from `plan.KeySessionCandidates`, itself derived exclusively from `preferredDays`) and behaviorally (`NoLegalAssignment_ThrowsTypedFailure_NeverDropsASessionOrMovesLongRun`).

## 13. LongRunDay result

Unchanged: `LONG_RUN` is fixed to `LongRunDayPreference`'s mapped date before the KEY search runs, in every week, and the search never revisits or moves it to improve KEY spacing (structurally impossible — `LongRunDate` is computed once in `BuildWeekPlan` and never reassigned).

## 14. Deterministic tie-break result

Confirmed and proven, not merely asserted: `FirstCanonicalKeySlot_AlwaysReceivesEarlierChosenDate_SecondReceivesLater` (synthetic, 4 weeks) and `RealFiveDayCandidate_LaneOrdinalNotDerivedFromDate_FirstCanonicalSlotAlwaysEarlierDate` (real candidate, real binder, real `LaneOrdinal` values `{0, 1}` confirmed distinct and unaffected by date assignment). Ascending `SlotOrderInWeek` → ascending chosen date, identical to the pre-existing `EASY_SUPPORT` convention, applied to a second repeated role.

## 15. No-legal-assignment result

Typed, fail-closed, unchanged exception (`CatalogPreferredDayConfigurationUnsafeException`) — no new exception type needed. Proven at the unit level (`NoLegalAssignment_ThrowsTypedFailure_NeverDropsASessionOrMovesLongRun`) and against the real candidate/real pipeline (`RealFiveDayCandidate_NoLegalAssignment_ThrowsTypedFailure_NeverSilentCoercion`, `Mon/Tue/Wed/Thu/Fri` with `LONG=Tue`).

**Real, honestly-disclosed refinement of FREQ.6D.4D.5A's own combinatorial analysis, found by genuine multi-week testing, not hidden:** `FREQ.6D.4D.5A` §9 hand-verified `Mon/Wed/Thu/Sat/Sun` (LONG=Sun) as single-week feasible — correct, but that check was explicitly single-week-local (5A's own §9 disclaimer: "a representative check, not an exhaustive proof"). Real 12-week testing here (`CrossWeekRefinesFiveA_MonWedThuSatSun_IsActuallyInfeasibleAcrossMultipleWeeks`) found a genuine cross-week interaction 5A did not check: Monday is the only same-week-safe partner for either Wednesday or Thursday, but Monday is only 1 calendar day after the *preceding* week's own Sunday long run (cross-week distance 1 < 2) — so Monday is cross-week-unsafe for every week except the first, leaving only `{Wednesday, Thursday}` (1 day apart, illegal) for week 2 onward. **No legal recurring assignment exists for this exact combination under the approved K2 rule.** This is not a defect — the algorithm correctly fails closed and typed rather than accepting a week-1-only-safe configuration that would break down later — and it does not change the K2 decision itself (5A's evidence-based reasoning for choosing 2 over 3 stands independently of this one pattern's outcome). Confirmed via isolation: a genuinely single, predecessor-free week with the identical preferred-day/long-run combination *does* have a legal local assignment (proven via the materializer directly, since the real dynamic pipeline has its own unrelated 8-week minimum-horizon floor that a 1-week request cannot satisfy).

## 16. Taper result

`RealFiveDayCandidate_TaperTwoKeyWeek_SpacingEnforced` — every real `PhaseKey == "TAPER"` week in a real 12-week dark plan carries exactly 2 `KEY_SESSION` slots, both spaced `>= 2` calendar days apart, identically to every other phase. No phase-specific exception exists in the materializer (matches FREQ.6D.4D.5A §13's phase-invariance decision).

## 17. 3D result

Zero delta. Full `CatalogWeekSkeletonCalendarMaterializerTests.cs` suite (63 tests, unmodified except the one fixture case in §6) passes unchanged, including every 3D/4D cross-week/deterministic-selection/output-validation test.

## 18. 4D result

Same as §17 — the pre-existing suite exercises 4D shapes throughout; all green, unmodified assertions.

## 19. Beginner×4D result

Covered by the broader focused regression (§29) — `Gen4EBeginnerFourDayPublicActivationTests` and related Beginner×4D suites, all green, zero delta.

## 20. 8/10/12/14-week real dark 5D result

`Freq6D4D5BReal5DDarkPlanTests.RealFiveDayCandidate_DarkMaterialization_ProducesTwoDistinctKeySessionsPerWeek` — real `TEN_K__5D__INTERMEDIATE v1`, real `RUN_LAYOUT_5D`, real dual-lane progression, real 8 profiles, through the real `DynamicCoreWorkoutBindingOrchestrator` composition, for all four horizons: every week has 5 sessions (2 KEY + 2 EASY + 1 LONG), both KEY dates distinct and `>=2` apart, both KEY↔LONG pairs `>=2` apart, LONG on the requested weekday. Independently re-validated by the real, unmodified `DatedGeneratedCatalogPlanSkeletonValidator` (defense in depth — materializer and validator agree). Real workout binding (`BoundPlan`) also succeeded for every horizon, not calendar-only luck.

## 21. Public activation retry (attempted, reverted a second time)

Re-applied the exact `V1CatalogPilotIdentityPolicy` widening Split E had reverted (the calendar blocker it was reverted for is now genuinely fixed). Real E2E HTTP testing (`PublishedCatalogTestRelease`-backed, mirroring `Gen3BThreeDayPublicActivationTests`) found a **second, completely independent, calendar-unrelated blocker**, not previously disclosed by any report:

`CatalogPrescriptionContextValidator.Validate` (in `CatalogPrescriptionContextBuilder.cs`) hardcodes:
```csharp
if (!sessions.Any(s => s.PhaseKey == "TAPER" && s.ProgressionStageKey == "TAPER_SHARPEN"
    && s.StructuralRole == "KEY_SESSION" && s.WorkoutDefinitionKey == "EASY_STANDARD"))
{
    errors.Add("TAPER_SHARPEN_CONTEXT_MISSING");
}
```
`TAPER_SHARPEN` is the legacy 3D/4D/Beginner×4D Taper stage-key naming (confirmed real and still correct for those candidates: present in `ten-k-workout-progression.v1.json` through `.v5.json`). The real 5D dual-lane progression (`.v6.json`, authored in `FREQ.6D.4D.5`) names its Taper stages `TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE` instead — a deliberate, already-approved naming difference for the dual-lane shape, not an oversight. This hardcoded check fails closed for every 5D request whose plan includes a Taper phase, which is effectively every supported 8-14 week horizon — confirmed via the real HTTP 500 (`CATALOG_INTERNAL_WORKOUT_BINDING_FAILED: ... TAPER_SHARPEN_CONTEXT_MISSING`), not assumed from reading the code alone.

A second, secondary issue was also observed in the same test run (a `CatalogSessionPrescriptionMissingExecutionPrescriptionException` for `TEN_K__5D__INTERMEDIATE`'s `ProfileBacked` sessions) — traced to the `PublishedCatalogTestRelease` test fixture's own directory layout (its temp release root does not place an `artifacts/appsel-plan-catalog/{version}/bundles/` sibling where `PublishedTemplateBundleLoader` looks for one), **not** confirmed as a real production gap; the real repo catalog root does have this sibling (`plan-catalog/artifacts/appsel-plan-catalog/1.1.0/`, published in `FREQ.6D.4D.5`). This is disclosed honestly as unconfirmed either way, not resolved, since the `TAPER_SHARPEN_CONTEXT_MISSING` finding alone was already sufficient grounds to revert and stop, per this phase's own §60 ("if materializer works but public activation still hits another independent blocker: do NOT force full closure").

**Action taken**: reverted `V1CatalogPilotIdentityPolicy`'s widening a second time (§6). `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic. Per this phase's own STOP condition §62.10 ("public activation uncovers a new independent architecture/domain blocker"), `CatalogPrescriptionContextValidator`'s Taper-completeness check was **not** modified — generalizing it to recognize dual-lane Taper stage-key naming (or any other fix) is a real, separate decision this phase does not have authority to make unilaterally.

## 22. Public preview result

Not reached (blocked by §21). No public preview succeeded for `TEN_K__5D__INTERMEDIATE`.

## 23. Public confirmation result

Not reached (blocked by §21).

## 24. DB result

Not applicable to public 5D (blocked). The real dark-pipeline tests (§20) do not touch the database at all (`DynamicCoreWorkoutBindingOrchestrator` is pure, dependency-free composition) — no DB round-trip claim is made or needed for this split's actual scope (materializer correctness).

## 25. Representative Adaptation result

Not reached (blocked by §21) — no real, persisted, publicly-confirmed 5D plan exists to run Adaptation against. `Freq6D4DSplitDFiveSessionAdaptationSeverityTests` (44/44) and the broader `LongHorizon.Adaptation` suite were re-run unmodified as part of the full regression (§29) and remain green, confirming this split introduced zero delta to Adaptation itself.

## 26. Unsupported-neighbor result

Confirmed still closed: `Gen3BThreeDayPublicActivationTests.WrongCombination_NeverNearestMatches` (`beginner×3D`, `advanced×3D`, `intermediate×5D` — reverted back to its original assertion, since 5D is not activated) and `Phase4F8_2LivePilotRoutingTests` (reverted back to its original `Intermediate×5` non-pilot assertion) both pass. Beginner×5D, Advanced×5D, Intermediate×6D/7D were never touched by this split.

## 27. No-silent-coercion result

Confirmed by the real dark-pipeline tests: `candidate.CandidateKey == "TEN_K__5D__INTERMEDIATE"` and `candidate.CandidateVersion == 1` asserted directly, `Assert.NotEqual("TEN_K__4D__INTERMEDIATE", ...)`. Since public routing is reverted, this is a dark-only confirmation — no public-path silent-coercion claim is made (there is no public path to make it on).

## 28. Inventory result

Unchanged. `PlanCatalogDeploymentPackagingTests`/`PackagedPlanCatalogRealHttpSmokeTests` (7/7) pass against the existing `97`-file baseline established in `FREQ.6D.4D.5` — no new catalog files were authored this split.

## 29. Full regression

```
Focused suite (Materialization|DynamicCore|Freq6D4D|Freq4|Gen3A|Gen3B|Gen4E|Adaptation|ScheduleRepair):
  1,223 / 1,223 passed

Full backend suite (dotnet test backend/RunningApp.sln), post-implementation, pre-activation-attempt:
  3,639 / 3,640 passed (1 pre-existing, unrelated Sw09 failure — confirmed unrelated to 5D
  across every prior split's regression run, references phases 4F.7C/4F.8.1, the legacy
  4D v10 8-week explicit-zero case)

Full backend suite, post-second-revert (final, this report's evidence baseline):
  3,639 / 3,640 passed (same, single, pre-existing Sw09 failure)

PlanCatalog.Tests: 1,510 / 1,510 passed — zero delta, confirmed by git status (only the two
pre-existing unrelated plan-catalog/artifacts/audits/* files touched, neither by this split)

dotnet build backend/RunningApp.sln:            0 Warning, 0 Error
dotnet build backend/RunningApp.sln -c Release:  0 Warning, 0 Error
git diff --check:                                clean (CRLF-normalization warnings only)
```

Docker/Postgres (`appsel-dev-postgres`) confirmed healthy throughout — no DB-backed test was skipped or simulated.

## 30. Parent FREQ.6D.4D closure

**Not evaluated for closure.** Per §61 of the originating prompt, parent closure is conditional on public activation passing; it did not. `FREQ.6D.4D` overall dual-KEY production integration remains open. Everything up through real 5D catalog content, runtime bundle discovery, and now the calendar materializer itself is implemented and verified; public activation is blocked on a real, independent, newly-disclosed gap in prescription-context Taper-completeness validation.

## 31. Final classification

**`FREQ6D4D5B_MULTI_KEY_CALENDAR_MATERIALIZER_IMPLEMENTED`**

`CatalogWeekSkeletonCalendarMaterializer` is correctly, deterministically, and safely generalized to multi-KEY_SESSION weeks, enforcing the frozen FREQ.6D.4D.5A separation rule with zero legacy delta (proven by full regression, not merely asserted) and zero duplicated numeric authority. The generalization is proven correct against the real `TEN_K__5D__INTERMEDIATE` candidate for every supported Core horizon (8-14 weeks), including Taper, determinism, and lane-identity preservation — success boundary (A) is met in full. Public activation was retried and reverted a second time after real E2E testing surfaced a genuine, independent, previously-undisclosed blocker (`CatalogPrescriptionContextValidator`'s hardcoded `TAPER_SHARPEN` stage-key check, incompatible with the real dual-lane Taper stage naming) — success boundary (B) is not met, and per this phase's own explicit instruction this was not hacked around. `TEN_K__5D__INTERMEDIATE` remains fully dark. The next concrete phase must resolve the Taper-completeness generalization question (a real product/architecture decision — how should prescription-context completeness be verified for a dual-lane Taper shape — not a mechanical fix) before public activation can be attempted a third time.
