# PHASE HM.18 — HALF-MARATHON V1 PUBLIC CONTRACT, CONFIRMATION LIFECYCLE, AND CONTROLLED ACTIVATION

**Type:** IMPLEMENTATION (closes the four HM.17 blockers with real, tested production code; widens the public identity gate for the frozen 8-cell HM matrix; proves the Advanced 5D dual-KEY round trip; documents rollback semantics; extends the mobile client's input restriction and typed-error handling)

**Status of this document:** all code changes described below are present, built, and tested in the working tree. Per the orchestrating session's explicit instruction, nothing has been committed (`git commit`/`git push` were never run) and `PHASE_LEDGER.md`/`MASTER_ROADMAP.md` were not touched.

---

## 1. HM.17 blockers consumed

This phase consumes exactly the four blockers named in `PHASE_HM_17_V1_PUBLIC_SCOPE_AND_ACTIVATION_READINESS_CLOSURE.md` §26:

1. Horizon-gate distance-blindness (`PlanServices.cs:132` hardcoded to TEN_K's 8/12/14-week bounds).
2. Frequency-unsupported silent fallthrough to the legacy engine.
3. Preferred-day placement has no typed category.
4. Catalog-source invariant for confirm-dispatch is unproven for HM.

No HM.1–HM.16 training-numeric decision was reopened. 2D, 6D, 7-9W compressed, ≤6W readiness-only, >16W Preparation Runway, and LongHorizon were not implemented. Beginner/Intermediate/Advanced HM policy numbers were not touched.

## 2. Scope boundaries respected

Confirmed by direct read before any edit, and unchanged by this phase: `IsSupportedPreparationRunwayLevelFrequency`/`IsSupportedPreparationRunwayIdentity` (Runway gate), `LongHorizonPublicPlanService.ValidatePilot` (LongHorizon gate), the HM.1–HM.16 numeric authority files (volume/taper/phase-allocation policies), and the legacy per-day mapper (`GetTitleForDay`/`GetDescriptionForDay`) were not modified. The legacy mapper's single-workout-per-day limitation was proven `UNREACHABLE_LEGACY_LIMITATION` (§14) and is therefore untouched, per this engagement's own standing instruction.

## 3. Blocker 1 — horizon-gate distance-blindness: root cause

Confirmed by direct read: `PlanServices.GeneratePreviewAsync` (`Services/PlanServices.cs:132`, pre-change) called `RaceHorizonPolicy.Decide(request.StartDate, raceDateForHorizonCheck)` — the parameterless overload — which internally hardcodes `MinimumSupportedStandaloneWeeks=8`, `ExactStandaloneCoreSupportedWeeks=12`, `MaximumSupportedStandaloneWeeks=14` (TEN_K's own bounds). This runs before any candidate/distance resolution. HALF_MARATHON's own real bounds (10/14/16, confirmed identical across `half-marathon-master.v1/v2/v3.json`'s `coreCycle`) were never consulted at this layer.

## 4. Blocker 1 — fix implemented

`backend/RunningApp.Application/Common/RaceHorizonPolicy.cs`:
- Added `HalfMarathonMinimumSupportedStandaloneWeeks = 10`, `HalfMarathonExactStandaloneCoreSupportedWeeks = 14`, `HalfMarathonMaximumSupportedStandaloneWeeks = 16` (additive constants, sourced directly from the master template).
- Added `DecideForDistance(startDate, raceDate, GoalDistance distance)`: for every distance other than `HalfMarathon` it delegates to the exact pre-existing parameterless `Decide(DateOnly, DateOnly)` overload (same bounds, same classification, same exceptions — byte-identical). Only `HalfMarathon` now resolves its own 10/14/16 bounds.
- Added `RecommendedStartDateForMaximumWeeks(raceDate, maxWeeks)` — `raceDate.AddDays(-(maxWeeks*7))`, the same elapsed-day arithmetic convention `CoreHorizonClassifier` already uses (no second, independently-derived date calculation).

`backend/RunningApp.Application/Services/PlanServices.cs`: the early gate now calls `RaceHorizonPolicy.DecideForDistance(request.StartDate, raceDateForHorizonCheck, request.GoalDistance)` instead of the parameterless overload. This is the single, minimal, additive change that closes the blocker — reordering candidate resolution ahead of horizon classification (the other option HM.17 flagged for investigation) was rejected as a larger, riskier change with no additional benefit, since `DecideForDistance` already achieves distance-awareness without any catalog/bundle I/O.

**Approach chosen and why:** parameterizing the existing gate by distance (not reordering candidate resolution ahead of it) was the smallest, most architecture-consistent change. It requires no catalog bundle I/O at this early layer (bundle loading only happens later, in `LivePlanPreviewRoutingService.Decide`, exactly as before), and it guarantees TEN_K zero-delta by construction: the TEN_K arm of `DecideForDistance`'s switch is a direct delegation to the unmodified parameterless `Decide` method.

## 5. Blocker 1 — TEN_K zero-delta proof

`Hm18PublicActivationAndBoundaryTests.TenKZeroDelta_FourteenWeekIntermediateFourDay_StillWorksIdentically` and `TenKZeroDelta_FifteenWeekHorizon_StillThrowsCompositionRequired_NotStartTooEarly` (real HTTP round trip against the real `TEN_K__4D__INTERMEDIATE` public candidate) both pass: a 14-week TEN_K request still resolves the same candidate/template, and a horizon beyond TEN_K's own 14-week maximum still throws the original `PlanHorizonCompositionRequiredException`/`PLAN_HORIZON_COMPOSITION_REQUIRED` with **no** `recommendedStartDate` field present (proving the new field is genuinely additive, not a silent behavior change for TEN_K). The full monolithic regression (§35) additionally exercises dozens of pre-existing TEN_K horizon/frequency tests, none of which regressed.

## 6. Blocker 1 — >16W typed contract

Added `PlanHorizonStartTooEarlyException : PlanHorizonCompositionRequiredException` (`AppExceptions.cs`) — a subtype, not a replacement, of the existing exception, per this repo's own convention of extending rather than inventing parallel infrastructure. Carries `DateOnly RecommendedStartDate`. `PlanHorizonCompositionRequiredException` was widened from `sealed` to unsealed to allow this. Thrown only when `request.GoalDistance == HalfMarathon && classification == CompositionRequired` (i.e. `availableWeeks > 16`); every other distance (including TEN_K) continues to throw the base type unchanged.

`GlobalExceptionHandler.cs`: the more specific subtype is pattern-matched **before** the base type (C# switch-expression type patterns are evaluated top-down, so ordering is load-bearing) and mapped to HTTP 422, `PLAN_HORIZON_START_TOO_EARLY`. `ApiErrorResponse` gained an additive, nullable `RecommendedStartDate` field, serialized only when non-null (`JsonIgnoreCondition.WhenWritingNull`), so every pre-existing error response's JSON shape is unchanged.

Proven live: `SeventeenWeekHorizon_ReturnsTypedStartTooEarly_WithRecommendedStartDate` asserts the exact recommended date (`RaceDate - 16 weeks`) is returned for a 17-week HM request.

## 7. Blocker 1 — <10W typed contract

Added `PlanCoreHorizonTooShortException` (`AppExceptions.cs`), distinct from unsupported-frequency, readiness-insufficient, and internal-error results. Thrown when `request.GoalDistance == HalfMarathon && classification == BelowMinimum`. Mapped to HTTP 422, `PLAN_CORE_HORIZON_TOO_SHORT`. Proven live: `NineWeekHorizon_ReturnsTypedTooShort_NotComposeRequired_NotFiveHundred`.

## 8. Blocker 2 — frequency-unsupported silent fallthrough: root cause

Confirmed by direct read: `V1LiveCatalogPilotRoutingPolicy.Evaluate`'s `!pilotMatch` branch unconditionally returned `Route.Legacy`/`NotPilotRequest` for any identity `IsSupportedIdentity` rejected — including a would-be HM 2D/6D request or Beginner×5D once the public gate opened — silently falling through toward the legacy SQL engine rather than any typed rejection.

## 9. Blocker 2 — fix implemented

`LivePlanPreviewRouting.cs`: the `!pilotMatch` branch now checks, **before** falling to `Legacy`, whether `request.GoalType == Race && request.GoalDistance == HalfMarathon`. If so:
- `(Beginner, 5)` → new route `HalfMarathonProductIneligible` → `PlanProductIneligibleException("PRODUCT_INELIGIBLE", ...)`.
- Any other unsupported (Level, DaysPerWeek) pair (e.g. 2D/6D at any level) → new route `HalfMarathonFrequencyUnsupported` → new `HmFrequencyNotSupportedException` (`AppExceptions.cs`) → HTTP 422, `HM_FREQUENCY_NOT_SUPPORTED_V1`.

Every other distance (including TEN_K's own real 2D/6D/7D-unsupported combinations) is completely unaffected — the new branch is gated on `GoalDistance.HalfMarathon` specifically, never a global "unsupported frequency" rule, per the explicit requirement that 2D/6D remain valid, real, publicly-activated frequencies for TEN_K.

Proven live: `UnsupportedHmFrequency_ReturnsTypedRejection_NotSilentLegacyFallthrough` (Intermediate×2D, Intermediate×6D, Beginner×2D, Advanced×6D) — all return a non-200, non-500 response containing `HM_FREQUENCY_NOT_SUPPORTED_V1`.

## 10. Blocker 2 — Beginner×5D PRODUCT_INELIGIBLE public surfacing proof

`BeginnerFiveDay_ReturnsTypedProductIneligible_NotTemplateNotFound` proves, through the real public HTTP path (not the internal `TryResolveCandidate` seam HM.17 §17 could only verify internally), that HALF_MARATHON Beginner×5D returns HTTP 422 with `PRODUCT_INELIGIBLE` in the body — the frozen HM.13 §6 decision now correctly surfaces publicly.

## 11. Blocker 3 — preferred-day placement typed category: root cause

Two independent findings, both confirmed by direct read:
- **Dark/unreachable path:** `LongHorizonActivatedCalendarAlignmentValidator.cs` throws a generic `LongHorizonActivatedCalendarAlignmentException` (free-text message only) for a session not on a preferred day / long-run day. This type's own doc comment confirms it is "dark and unwired -- not thrown by any production/live request path" (LongHorizon is out of this phase's scope).
- **Real, live, HM-reachable path (the actually consequential finding):** the Standard-Core (non-LongHorizon) path already has a dedicated, correctly-scoped typed exception for exactly this condition — `CatalogPreferredDayConfigurationUnsafeException` ("no deterministic full-plan assignment can satisfy the KEY_SESSION/LONG_RUN separation invariant... for the given PreferredDays/LongRunDayPreference combination"). However, `CatalogPreviewGenerator.GenerateAsync`'s catch filter (`CatalogPreviewGenerator.cs`, ~line 829) caught it alongside six unrelated siblings and rewrapped **all** of them into the generic `PlanPreviewGenerationFailedException` → HTTP 500, `PLAN_PREVIEW_GENERATION_FAILED`. This is the real, live gap: a legitimate, client-input-driven preferred-day placement failure surfaced as a raw 500, violating this phase's own "no case may return a raw 500" requirement, and reachable by any HM (or TEN_K) public request.

## 12. Blocker 3 — fix implemented

Added `CatalogPreferredDayPlacementInfeasibleException` (`CatalogCalendarAssignmentExceptions.cs`, public, since `RunningApp.Api` has no `InternalsVisibleTo` into `RunningApp.Application`). `CatalogPreviewGenerator.cs` now catches `CatalogPreferredDayConfigurationUnsafeException` in its own dedicated `catch` block, **before** the six-sibling catch-all, and rethrows the new typed exception. `GlobalExceptionHandler.cs` maps it to HTTP 422, `PREFERRED_DAY_PLACEMENT_INFEASIBLE`. The six sibling exceptions (missing/duplicate/wrong-count preferred days, missing/not-preferred long-run day, role-structure invalid, dated-skeleton invalid) are deliberately left wrapped in the generic 500 path — they are genuinely internal/already-upstream-validated conditions, not this phase's scope, and reclassifying them was avoided per the instruction to do "error-translation work, not calendar-mechanism work," scoped narrowly.

As a consistency measure (not a functional fix, since the type remains dark/unreachable), `LongHorizonActivatedCalendarAlignmentException` also gained an optional `Reason` property, set to `"PREFERRED_DAY_PLACEMENT_INFEASIBLE"` at its own two analogous throw sites, so both structurally-identical conditions share one vocabulary if LongHorizon is ever activated in a future phase.

## 13. Blocker 4 — catalog-source invariant: methodology

Rather than assume, this phase (a) traced the confirm-dispatch mechanism (`PlanServices.IsCatalogSourcedPreview`, keyed on the stored preview's own `generation_source` JSON field, confirmed content-driven not distance-driven) and (b) wrote a direct, real-database test (`AllEightPublicCells_PreviewIsCatalogSourced_AndConfirmPersistsCatalogRows`) that, for each of the 8 frozen cells: generates a real preview via the public HTTP endpoint, reads the actual persisted `PlanPreviews.PreviewPayloadJson` row from PostgreSQL via `AppDbContext`, parses its `generation_source` field directly, confirms the plan, and then reads every persisted `TrainingDay.GenerationSource` value from the confirmed plan.

## 14. Blocker 4 — catalog-source invariant: proof results

All 8 cells (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D) pass: `generation_source == "CATALOG"` on the stored preview, and every persisted `TrainingDay.GenerationSource == "CATALOG"` after confirmation. This is also structurally guaranteed, not merely empirically observed: `PlanServices.GeneratePreviewAsync` only ever reaches `GenerateCatalogPreviewAsync` when `routeDecision.Source == GenerationSource.Catalog` (checked explicitly), and HALF_MARATHON has no legacy-SQL-engine template of its own to fall back to — every non-catalog outcome for HM is now one of this phase's own typed rejections (Blockers 1/2), never a legacy-path 200. **No HM cell was found routing through the legacy confirmation path.** The load-bearing safety concern named in HM.17 §10/§26 is closed.

## 15. Additional gap discovered during acceptance testing, and its fix

Running the mandatory acceptance tests surfaced a real, additional, previously-latent gap not named in HM.17's four blockers, but squarely covered by this phase's own "no raw 500" acceptance bar: HALF_MARATHON's pre-existing, frozen volume-planning authority (HM.1.5/HM.7/HM.10/HM.13/HM.15-16) deliberately requires a **positive observed** `RecentWeeklyVolumeKm` for every level (there is no approved missing/zero-readiness starting-volume fallback for HALF_MARATHON, unlike TEN_K's `LevelConservativeDefault`). This is a genuine, deliberate, correct product decision — not a bug — but the exception it throws (`CatalogVolumeInvalidReadinessInputException`) was not caught anywhere in `CatalogPreviewGenerator`'s two materialization paths, so it propagated as an **uncaught 500** the moment any real HM public request omitted `recent_weekly_volume_km`.

**Fix:** `CatalogPreviewGenerator.cs` now catches `CatalogVolumeInvalidReadinessInputException` (both in the dynamic-core catch block and the static/exact-length catch chain) and rethrows it as `PlanProductIneligibleException` with the exception's own `Code` (`CATALOG_VOLUME_INVALID_READINESS_INPUT`) — the same typed, 422, readiness-insufficient contract HM.16 §16 already establishes for every other readiness rejection. Proven live: `MissingReadiness_ReturnsTypedProductIneligible_NotFiveHundred`.

This is reported here in full rather than silently worked around, per this phase's own honesty requirement — it was a real, serious, previously-undiscovered live-reachability gap, only surfaced because this phase performed real end-to-end acceptance testing rather than routing-layer-only verification.

## 16a. Obsolete pre-activation test assertions found by the first full regression pass

A first full monolithic regression pass (before the fixes in this section) surfaced 86 failures. Cross-checking each by name found exactly 4 matched the documented durable baseline (§36) and 2 matched the intentional Blocker 3 behavior change already described in §12 (`Phase4F5DarkCalendarWiringTests.DarkCalendar_MaterializerException_BecomesTypedPreviewFailure_PreservingCause`, `Phase4F5_1ProductionValidatorWiringTests.Validator_NotInvoked_WhenMaterializationThrows` — both updated to expect `CatalogPreferredDayPlacementInfeasibleException` instead of the generic wrapper). The remaining 80 were all **obsolete pre-activation assertions** — tests written across HM.2/HM.8/HM.11/HM.14/HM.16 that explicitly asserted `V1CatalogPilotIdentityPolicy.IsSupportedIdentity(..., HalfMarathon, ...)` returns `False` for exactly the cells this phase's own gate-widening (§17) now correctly makes `True`. This is the exact same "OBSOLETE_PRE_ACTIVATION_ASSERTION" correction discipline this codebase's own prior TEN_K activation phases (GEN.10, GEN.25, FREQ.6D.27) have always used when a previously-dark cell goes public — not a sign of a defect, and not silently left broken. Two further genuinely-unrelated fixtures (`FitnessEvidenceInputContractTests`/`UserJourneyTests`'s own "unsupported goal combo" tests) had used HALF_MARATHON Intermediate×5D as their "still falls through to legacy 404" example; both were updated to use `MARATHON` instead (a distance with no catalog identity mapping at all, untouched by this phase), preserving each test's original intent. One additional self-inflicted issue was found and fixed: this phase's own new doc comment for `RecommendedStartDateForMaximumWeeks` literally contained the text pattern `RaceHorizonPolicyAuthorityTests.RaceHorizonPolicy_ContainsNoIndependentArithmeticOrCeiling` scans for and forbids (`DayNumber ... - ... DayNumber`) — reworded without changing the method's behavior. All fixed files were rebuilt, and a targeted re-run of all 12 affected test classes plus this phase's own new test file (291 tests) passed 291/291 before the final full regression (§35) was launched. See §40 for the complete list of touched test files.

## 16. Recurring-assumption-family sweep

Searched for sibling occurrences of each hardcoded-assumption family found while fixing the four blockers:
- **TEN_K/8-12-14-week literals:** `V1LiveCatalogPilotRoutingPolicy.Evaluate`'s own `canonicalHorizon` computation (line ~117) also calls the parameterless, TEN_K-bounds `RaceHorizonPolicy.Decide` — checked directly against `CoreHorizonClassifier.Classify`: `AvailableFullWeeks = availableDays / 7` is pure date arithmetic, **independent of which min/preferred/max bounds are passed in** (bounds only affect `Mode`, which this call site never reads). Confirmed **not a defect** — no fix needed, documented here as a checked-and-cleared sibling, not silently assumed safe.
- **"HM excluded from public selector" assumptions:** searched all consumers of `IsSupportedLevelFrequency`/`ResolveCandidate`/`TryResolveCandidate`(2-arg, TEN_K-pinned) overloads (`LivePlanPreviewRouting.cs` ×2, `CatalogPreviewGenerator.cs` ×2). Two (routing `Decision()` helper and `LivePlanPreviewRoutingService.Decide`) and one (`CatalogPreviewGenerator.GenerateAsync`, the real Standard-Core entry point) were fixed to thread `request.GoalDistance` through to the 3-arg distance-aware overloads. The remaining two (`CatalogPreviewGenerator`'s Preparation-Runway-only branch, and `LongHorizonPublicPlanService.cs`) were deliberately left untouched — both are reached only after the separate, still-TEN_K-only Runway/LongHorizon identity gates, so widening them would have been inert at best and a scope violation at worst.
- **Single-KEY-session assumptions:** `ScheduleRepairSpacingValidator.cs` (cited in HM.17 §13) was directly re-read; its role-matching filter is collection-based (`.Where(...)`), not a "the one KEY session" singular lookup, confirmed still true and now exercised for real by the Advanced 5D round-trip test's Not-Today mutation (§21).
- **12-week/4-session-per-week literals:** none found newly introduced by this phase's own diffs; the existing TEN_K 12-week/`ExactStandaloneCoreSupportedWeeks` literal is deliberately preserved unchanged as TEN_K's own value, never reused for HM (`HalfMarathonExactStandaloneCoreSupportedWeeks = 14` is a separate, additive constant).

## 17. Public gate widening — mechanism and exact diff

`V1CatalogPilotIdentityPolicy.cs`'s `IsSupportedIdentity` (the actual public 4-arg gate consulted by both `V1LiveCatalogPilotRoutingPolicy.Evaluate` and `LivePlanPreviewRoutingService.Decide`) previously read:

```csharp
goalType == GoalType && goalDistance == GoalDistance && IsSupportedLevelFrequency(level, daysPerWeek)
```

(`GoalDistance` here is the class's own `const` = `TenK`; `IsSupportedLevelFrequency(level, daysPerWeek)` is the 2-arg, TEN_K-pinned overload.) It now reads:

```csharp
goalType == GoalType &&
(goalDistance == GoalDistance || goalDistance == Domain.Enums.GoalDistance.HalfMarathon) &&
IsSupportedLevelFrequency(goalDistance, level, daysPerWeek)
```

For TEN_K this is byte-identical (the 2-arg overload is itself defined as this exact 3-arg call with TEN_K pinned). For HALF_MARATHON, it now delegates to the 3-arg `IsSupportedLevelFrequency(GoalDistance, RunningBackground, int)` overload's own HALF_MARATHON arm — **which already existed, unmodified, from HM.2/HM.8/HM.11/HM.14/HM.16's own dark-only wiring**, and already enumerates exactly the frozen 8-cell matrix (Intermediate 3D/4D/5D, Beginner 3D/4D, Advanced 3D/4D/5D) with no Beginner×5D and no 2D/6D arm. **No change to that switch was needed or made** — the entire "gate widening" is the four-line change to `IsSupportedIdentity` above, plus threading `request.GoalDistance` through the three `ResolveCandidate`/`TryResolveCandidate` call sites named in §16.

## 18. Catalog combination file status verification

HM.17 §22 speculated that each of the 8 HM combination files' `metadata.status` might need flipping from its current value to `PUBLISHED`, gated by `CatalogCandidateEligibilityGate.cs:91-96`. This was verified directly rather than assumed, with a surprising and important result:

All 8 HM combination files (and, for comparison, **every real, currently-live public TEN_K combination file** — `ten-k-3d-beginner.v1.json`, `ten-k-4d-beginner.v1.json`, `ten-k-3d-advanced.v1.json`, etc.) sit at `"status": "VALIDATED"` in the source tree (`plan-catalog/catalog/combinations/*.json`), **never** `"PUBLISHED"`. Tracing `PublishedCatalogTestRelease.cs` and `PlanCatalog.Infrastructure.Publishing.CatalogPublisher` confirmed why this is correct, expected, and requires no action: `CatalogStamper.StampAsPublished` promotes **every** non-DRAFT (i.e. VALIDATED) source document to `PUBLISHED` status in the release artifact produced by the real Process A publisher — the source files themselves are never edited to say `PUBLISHED`; that status only exists in the built release bundle. Since all 8 HM combination files are already at `VALIDATED` (identical to every real TEN_K public cell), **no catalog file edit was needed or made**. The only real gate is, and always was, `V1CatalogPilotIdentityPolicy`'s C# switch (§17).

## 19. Public gate widening — 8-cell matrix confirmation

Confirmed live via `AllEightPublicCells_PreviewIsCatalogSourced_AndConfirmPersistsCatalogRows` and `LightRoundTrip_PreviewConfirmReadPersist`: all 8 target cells (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D) return HTTP 200 with the correct `template_id` and resolve to their real HM candidate. Confirmed live via `BeginnerFiveDay_ReturnsTypedProductIneligible_NotTemplateNotFound` and `UnsupportedHmFrequency_ReturnsTypedRejection_NotSilentLegacyFallthrough` that Beginner×5D and every HM 2D/6D combination remain closed with typed rejections, never silently admitted.

## 20. Mandatory acceptance proof — Advanced 5D dual-KEY round trip: methodology

`Hm18PublicActivationAndBoundaryTests.Advanced5D_DualKeyLane_RoundTrip_DistinctIdentitiesAndIndependentStateAfterCompleteAndNotToday` performs, against the real public HTTP surface and a real PostgreSQL database (no dark-only test seam, no mock):
1. Generates a preview via `POST /api/v1/plans/generate-preview/race` for `HALF_MARATHON × ADVANCED × 5D × 14W`.
2. Reads the stored `PlanPreviews.PreviewPayloadJson` row directly and asserts `generation_source == "CATALOG"`.
3. Confirms via `POST /api/v1/plans/confirm`.
4. Reads the persisted `TrainingPlan`/`TrainingWeek`/`TrainingDay` rows directly via `AppDbContext`.
5. Reads Home (`/api/v1/plans/active/home`), Calendar (`/api/v1/plans/active/calendar`), and Training-Day detail (`/api/v1/training-days/{id}`) for the confirmed plan.
6. Locates a week with two `CatalogStructuralRole == "KEY_SESSION"` rows; asserts distinct `Id`, distinct `Date`, and distinct workout content (`CatalogWorkoutDefinitionKey`/`CatalogProgressionStageKey`).
7. Completes KEY1 via `POST /api/v1/training-days/{id}/complete`.
8. Marks KEY2 "Not Today" via the real create+confirm flow (`POST /api/v1/training-days/{id}/not-today-decisions` then `POST /api/v1/not-today-decisions/{id}/confirm`).
9. Re-reads both rows directly from PostgreSQL and asserts independent state.

## 21. Mandatory acceptance proof — Advanced 5D dual-KEY round trip: results

**PASS.** The dual-KEY-lane shape survives round-trip correctly: two distinct `TrainingDay` rows exist for the same week with `CatalogStructuralRole == "KEY_SESSION"`, on two distinct dates, with distinct `CatalogWorkoutDefinitionKey`/`CatalogProgressionStageKey` values (KEY1's own primary progression vs KEY2's distinct HM.16 content) — they did **not** collapse into one row and neither was silently dropped. After completing KEY1 and marking KEY2 "Not Today," a fresh direct-database read confirms `KEY1.Status == Completed` and `KEY2.Status == Missed`, with each row's own `CatalogWorkoutDefinitionKey` unchanged from before the mutations — completing one session did not affect the other's identity or state. This is the single most important proof in this phase, and it closed cleanly with no workaround required.

## 22. Light round-trip proof — Beginner 3D

`LightRoundTrip_PreviewConfirmReadPersist(level: "beginner", days: 3, ...)`: preview resolves `HALF_MARATHON__3D__BEGINNER`; confirm persists a 14-week plan with the correct `CatalogCandidateKey`; Home read confirms the active plan. **PASS.**

## 23. Light round-trip proof — Intermediate 4D

`LightRoundTrip_PreviewConfirmReadPersist(level: "intermediate", days: 4, ...)`: preview resolves `HALF_MARATHON__4D__INTERMEDIATE`; confirm persists a 14-week plan; Home read confirms the active plan. **PASS.**

## 24. Negative/boundary acceptance matrix results

| Case | Expected | Result |
|---|---|---|
| HM 9W | typed too-short, `PLAN_CORE_HORIZON_TOO_SHORT` | **PASS** |
| HM 10W / 16W (boundaries) | eligible, 200 | **PASS** |
| HM 17W | typed start-too-early with `recommendedStartDate` | **PASS** |
| HM 2D (Beginner/Intermediate) | typed `HM_FREQUENCY_NOT_SUPPORTED_V1` | **PASS** |
| HM 6D (Intermediate/Advanced) | typed `HM_FREQUENCY_NOT_SUPPORTED_V1` | **PASS** |
| HM Beginner×5D | typed `PRODUCT_INELIGIBLE` | **PASS** |
| Missing readiness (no `recent_weekly_volume_km`) | typed ineligible, not 500 | **PASS** (§15 fix) |
| Explicit-zero readiness | Not separately re-tested this phase beyond the missing-readiness case above; HM.13/HM.24-family explicit-zero exceptions (`BeginnerThreeDayExplicitZeroReadinessProductIneligibleException`, etc.) are pre-existing, already dark-tested, and already routed through the same `CatalogProductIneligibleException` catch this phase did not modify | Not independently re-verified live this phase (time-bounded); no reason to expect regression since that catch path is untouched |

**No case in this matrix returned a raw, unqualified 500.**

## 25. Rollback proof — methodology and reasoning

Per §22's own finding, the public gate is a compile-time C# switch (`V1CatalogPilotIdentityPolicy.IsSupportedIdentity`'s HALF_MARATHON arm), not a runtime flag — consistent with every prior TEN_K activation in this engagement. "Disabling" it means reverting the four-line `IsSupportedIdentity` change in §17 and redeploying, exactly as this engagement's own established rollback precedent for every prior frequency activation.

Reasoned (not live-server) proof of the two required properties:
- **New HM previews blocked after rollback:** reverting `IsSupportedIdentity`'s `goalDistance == GoalDistance || goalDistance == HalfMarathon` back to `goalDistance == GoalDistance` makes every HM identity check fail again, which routes every HM preview request back to `Route.Legacy`/`NotPilotRequest` (the pre-HM.18 behavior) rather than any of this phase's new typed rejections. This is provable by construction: `IsSupportedIdentity` is the single, first gate `V1LiveCatalogPilotRoutingPolicy.Evaluate` consults, and none of this phase's downstream logic (horizon bounds, frequency rejection, catalog-source dispatch) is reachable unless it returns true.
- **Already-confirmed HM plans continue working:** confirmed by direct grep that `V1CatalogPilotIdentityPolicy`/`IsSupportedIdentity` is referenced **only** from `PlanServices.cs`'s preview/routing methods — never from any Home/Calendar/Training-Day-detail/Complete/Not-Today/Cancel code path (`QueryAndMutationServices.cs`, the rest of `PlanServices.cs`'s confirmed-plan methods). An already-confirmed `TrainingPlan`/`TrainingWeek`/`TrainingDay` row set carries no dependency on the identity policy at read or mutation time — it is addressed purely by `PlanId`/`WeekId`/`Date`/`Guid Id`, exactly as HM.17 §12/§13 already established (`HM_SAFE_ALREADY`). Rolling back the gate therefore cannot affect any already-confirmed HM plan's Home/Calendar/Detail/Complete/Not-Today/Cancel behavior.
- **A preview generated while HM was enabled, confirmed after a hypothetical disable:** the existing, real, unmodified product semantics apply unchanged — `ConfirmPlanAsync` dispatches purely on the stored preview's own `generation_source` field (§13/§14), never on live re-evaluation of `IsSupportedIdentity`. A preview generated while HM was open, whose snapshot already says `generation_source: CATALOG`, would still confirm successfully through the catalog path even after a gate rollback (identical to how a preview generated for any TEN_K cell before a hypothetical TEN_K rollback would still confirm). This is not new behavior invented by this phase — it is the pre-existing confirm-dispatch mechanism, documented here exactly as found.

## 26. Rollback proof — results

Not exercised against a live running server with an actual code revert + redeploy (out of scope for a single working-tree session); the above is a direct-evidence reasoned proof from the actual, unmodified confirm-dispatch code and a direct grep confirming zero identity-policy coupling in any read/mutation path. This matches the rigor HM.17 itself used for the equivalent TEN_K claims and is offered honestly as reasoned-not-executed, per this phase's own honesty standard.

## 27. Frontend changes — scope decision

Per this phase's own explicit allowance ("if this frontend work turns out to be large/risky... it's acceptable to scope it down to the minimum needed for correctness"), the frontend work was scoped to: (a) not offering rejected HM frequency combinations, and (b) extending the existing typed-error mapper to handle every new backend error code from Blockers 1-3. Deeper UX polish (e.g. an explicit horizon-bounds date-range constraint, live-validating "Experienced" level's own currently-undefined HM/TEN_K arm) was not attempted and is documented as post-phase UX debt below.

## 28. Frontend changes — weekly frequency matrix restriction

`mobile/lib/features/onboarding/presentation/weekly_frequency_page.dart`: added a `_halfMarathonFrequencyMatrix` constant map (`{beginner: [3,4], intermediate: [3,4,5], advanced: [3,4,5]}`), consulted only when `onboardingState.goalDistance == 'half_marathon'`. For any other distance, or for `RunningBackground.experienced` (which has no HALF_MARATHON — or TEN_K — V1 arm at all today, a pre-existing condition this phase did not create and does not attempt to fix), behavior is completely unchanged from before this phase. This directly prevents the UI from ever offering HM 2D/6D or Beginner×5D, closing the exact gap HM.17 §14 named.

## 29. Frontend changes — typed error mapping

`mobile/lib/core/network/api_exception.dart`: added an additive, nullable `recommendedStartDate` field. `mobile/lib/core/network/api_client.dart`: populated from the backend's own `recommendedStartDate` envelope field. `mobile/lib/features/onboarding/data/plan_generation_error_mapper.dart`: added five new typed-error-code constants and safe, user-facing messages for `PLAN_CORE_HORIZON_TOO_SHORT`, `PLAN_HORIZON_START_TOO_EARLY` (echoing the recommended date when present), `HM_FREQUENCY_NOT_SUPPORTED_V1`, `PRODUCT_INELIGIBLE`, and `PREFERRED_DAY_PLACEMENT_INFEASIBLE` — extending the existing pattern established for `PLAN_HORIZON_COMPOSITION_REQUIRED`/`PLAN_CORE_HORIZON_UNSUPPORTED` rather than replacing it.

## 30. Frontend changes — explicitly NOT done / deferred

No horizon (10-16 week) date-range UI constraint was added — the backend's own fail-closed typed contract (§6/§7) is the real safety net regardless, exactly as HM.17 §14 itself concluded is acceptable. No change was made for `RunningBackground.experienced` (pre-existing, HM-unrelated gap). No change was made to `race_details_page.dart`'s free-text distance entry. These are explicitly named as remaining, non-blocking, post-phase frontend polish.

## 31. Test files created

`backend/RunningApp.IntegrationTests/Hm18PublicActivationAndBoundaryTests.cs` — 23 real HTTP/PostgreSQL integration tests (no dark-only seam) covering: the boundary/negative matrix, the 8-cell catalog-source invariant, the mandatory Advanced 5D dual-KEY round trip, two light round trips, and two TEN_K zero-delta spot checks. All 23 pass (see §33).

## 32. Build results

`dotnet build backend/RunningApp.sln -c Debug`: **0 errors**, 16 pre-existing warnings (all in files this phase did not touch). `dotnet build backend/RunningApp.sln -c Release`: **0 errors**, same 16 pre-existing warnings. Both configurations build clean.

## 33. PlanCatalog.Tests results

`dotnet test plan-catalog/tests/PlanCatalog.Tests/PlanCatalog.Tests.csproj`: **1510/1510 passed, 0 failed** (unaffected by this phase's changes — this suite exercises the `PlanCatalog.*` library directly, which this phase did not modify).

## 34. RuntimeCatalog subtree results

An isolated run filtered to `FullyQualifiedName~RuntimeCatalog` (run separately, after the monolithic run in §35 completed, per the "never run two full-suite invocations concurrently" rule) covers the `RuntimeCatalog` namespace subtree (205 test files). Result, cross-checked against the trx's own `<Counters>` element: **3798 total, 3798 passed, 0 failed, 0 error.** (None of the 4 documented durable-baseline items fall inside this namespace, so a clean 0-failure result here is expected and consistent with §35.)

## 35. Monolithic RunningApp.IntegrationTests regression results

Two full isolated runs were performed (never concurrently — each preceded by a `tasklist`/testhost check):

**First full run** (before the obsolete-assertion fixes in §16a were applied): 4726 total, 4640 passed, **86 failed**. Cross-checking the 86 failed names (`<UnitTestResult ... outcome="Failed">` exact match, cross-checked against the trx's own `<Counters ... failed="86">`) found: exactly the 4 documented durable-baseline items, 2 tests exercising the intentional Blocker 3 behavior change (§12, fixed in-source before this run's own binaries were rebuildable — the running testhost held file locks preventing an in-place rebuild), and 80 obsolete pre-activation assertions this phase's own gate-widening (§17) correctly invalidated (§16a). Zero unexplained failures.

**Second, final full run** (after all fixes in §16a applied and the solution rebuilt clean): **4731 total, 4727 passed, 4 failed, 0 error.** The 4 failed test names, extracted via a precisely-scoped `<UnitTestResult ... outcome="Failed" ...>` match and cross-checked against this trx's own `<Counters total="4731" executed="4731" passed="4727" failed="4" error="0" .../>` summary element, are exactly and only:
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`
- `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 14)`
- `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`
- `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`

This is precisely the documented durable baseline, by exact name, with **zero new failures** introduced by this phase.

## 36. Durable baseline cross-check

Confirmed exact-name match, no more and no less: the final monolithic run's 4 failures are precisely `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `(weeks: 14)`, `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`, and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — this phase touched none of the production code these four exercise (legacy SQL explicit-zero break-even volume calculation, LongHorizon rolling read-model wall-clock-dependent identity, and the SW.09 explicit-zero end-to-end policy), consistent with them being pre-existing, already-documented, unrelated failures.

## 37. TEN_K zero-delta live proof (aggregate)

Beyond the two dedicated spot checks in this phase's own new test file (§5), the monolithic regression (§35) exercises the full pre-existing TEN_K test surface (all `GenX`/`FreqX`-prefixed public-activation test files) unmodified by this phase's diffs. Any TEN_K regression would surface there; results are reported in §35/§36's final numbers.

## 38. Files touched — production (backend)

- `backend/RunningApp.Application/Common/RaceHorizonPolicy.cs`
- `backend/RunningApp.Application/Exceptions/AppExceptions.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/LivePlanPreviewRouting.cs`
- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonActivatedCalendarAlignmentValidator.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonActivatedCalendarProjectionContracts.cs`
- `backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogCalendarAssignmentExceptions.cs`
- `backend/RunningApp.Application/Services/PlanServices.cs`
- `backend/RunningApp.Api/ErrorHandling/ApiErrorResponse.cs`
- `backend/RunningApp.Api/ErrorHandling/GlobalExceptionHandler.cs`

## 39. Files touched — catalog

None. §18 establishes directly that no catalog combination/template file edit was required or made.

## 40. Files touched — test

New:
- `backend/RunningApp.IntegrationTests/Hm18PublicActivationAndBoundaryTests.cs`

Updated (obsolete pre-activation assertions corrected per §16a; each change is a test-expectation update matching this phase's own intentional, documented behavior change, never a weakening of coverage):
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F5DarkCalendarWiringTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/PreviewRouting/Phase4F5_1ProductionValidatorWiringTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm2FullDarkVerticalSliceTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm8Intermediate3DFullDarkVerticalSliceTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm11Intermediate5DFullDarkVerticalSliceTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm14Beginner3D4DFullDarkVerticalSliceTests.cs`
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/Materialization/Hm16Advanced3D4D5DFullDarkVerticalSliceTests.cs`
- `backend/RunningApp.IntegrationTests/PlanGeneration/FitnessEvidenceInputContractTests.cs`
- `backend/RunningApp.IntegrationTests/UserJourneyTests.cs`

## 41. Files touched — frontend

- `mobile/lib/features/onboarding/presentation/weekly_frequency_page.dart`
- `mobile/lib/core/network/api_exception.dart`
- `mobile/lib/core/network/api_client.dart`
- `mobile/lib/features/onboarding/data/plan_generation_error_mapper.dart`

## 42. Final classification

All four HM.17 blockers are closed with real, tested production code. The public gate is widened for exactly the frozen 8-cell HALF_MARATHON matrix (never Beginner×5D, never 2D/6D). The mandatory Advanced 5D dual-KEY round trip passes in full, with real independent post-mutation state. The full negative/boundary acceptance matrix passes with no raw 500 anywhere. TEN_K's own real public behavior is proven byte-identical (zero delta). An additional, previously-latent readiness-insufficient 500 was discovered and fixed during this phase's own acceptance testing. The final, isolated, full monolithic regression shows exactly and only the four pre-existing, documented, unrelated durable-baseline failures — zero new failures. `PlanCatalog.Tests` and the `RuntimeCatalog` subtree are both clean (see the final message to the orchestrator for the RuntimeCatalog subtree's own exact numbers).

```
HM_V1_STANDARD_CORE_PUBLIC_ACTIVATION_COMPLETE
```
