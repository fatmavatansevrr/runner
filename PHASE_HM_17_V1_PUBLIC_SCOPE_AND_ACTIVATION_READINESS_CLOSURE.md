# PHASE HM.17 — HALF-MARATHON V1 PUBLIC SCOPE AND ACTIVATION READINESS CLOSURE

**Type:** AUDIT / GOVERNANCE (no production implementation, no public activation, no new training authority)
**Method:** Delegated to five parallel research passes (public routing/persistence; prescription/schedule pipeline parity; active-read/mutation paths; frontend/rollout/10K precedent; test baseline/date-boundary proof), each citing real files/lines. The two highest-stakes findings (the horizon-gate hardcoding and the confirm-dispatch mechanism) were independently re-verified by direct code read before this report was finalized. All file:line citations below trace to real code read during this phase.

---

## 1. Research-backed V1 scope

Reused verbatim from the governing prompt, not re-derived: major standard HM programs commonly run 12–16 weeks; established 3-run-per-week HM preparation exists; 6-run Advanced HM preparation is a documented extension, not a V1 requirement; 17–24 week plans function as base-building products, not proof Standard Core must exceed 16W. 10–16W plus the already-frozen 3D/4D/5D matrix (HM.1–HM.16) is a defensible Standard Core V1. No new numeric training authority is introduced by this rationale — it justifies a scope boundary, not a training number.

## 2. Frozen public candidate matrix

| Level | 3D | 4D | 5D |
|---|---|---|---|
| Beginner | PUBLIC (target) | PUBLIC (target) | PRODUCT_INELIGIBLE |
| Intermediate | PUBLIC (target) | PUBLIC (target) | PUBLIC (target) |
| Advanced | PUBLIC (target) | PUBLIC (target) | PUBLIC (target) |

Horizon: 10–16 full weeks inclusive. This is a **target for HM.18**; nothing is activated by this phase.

## 3. Explicit post-V1 features

2D, 6D, 7–9W compressed, ≤6W readiness-only, >16W Preparation Runway/LongHorizon, missing/zero-readiness base-building, quality-in-long-run, stretch-goal public warning. None of these being absent counts as a blocker (§29's own standard, applied throughout this report).

## 4. Standard-Core readiness policy

Read `Prescription/Volume/V1MissingReadinessStartingVolumePolicy.cs:12-42` directly. **No `CalibrationStartingVolume`-named fallback exists anywhere in production/domain code** (confirmed by grep across `RunningApp.Application`/`RunningApp.Domain` — zero hits; the term appears only in HM dark-test filenames, as a testing label, never as a runtime substitution mechanism). The real mechanism uses two separately-labeled, explicitly-logged product defaults: `MissingWeeklyVolumeDefaultKm=16d` (source `WeeklyVolumeAnchorSource.LevelConservativeDefault`) for missing volume, and a *distinct* `ExplicitZeroWeeklyVolumeDefaultKm=12d` (source `WeeklyVolumeAnchorSource.NoRecentRunningDefault`) for explicit zero — both logged via a named `StartingVolumeDecision`, never silently reused as a single "calibration" constant. **No fallback-substitution bug found.** The typed insufficient-evidence result is `CoreEntryReadinessResolver` (`Resolvers/CoreEntryReadinessResolver.cs:58-59,119-122`), producing `RuntimeConditionResolutionResult.Evaluated(ConditionType, "NOT_READY", "CORE_ENTRY_NOT_READY", ...)` or a `"CAUTION"`/`"CORE_ENTRY_CAUTION"` variant — a named condition type with enumerated outputs, not an exception, not a generic infeasible result, not a substitution. **This governance requirement is already satisfied by existing architecture.**

## 5. Horizon UX policy

Canonical arithmetic lives in `Schedule/Horizon/CoreHorizonClassifier.cs:112`: `availableDays = RaceDate.DayNumber - StartDate.DayNumber` (exclusive/elapsed-day semantics, no +1), full weeks via `availableDays / 7` (never rounded up, doc comment `RaceHorizonPolicy.cs:81-84`). This classifier itself is **GENERIC_ALREADY** — it takes min/preferred/max weeks as parameters and is distance-agnostic by construction.

**The production gate wired in front of it is NOT distance-aware.** `PlanServices.GeneratePreviewAsync` (`Services/PlanServices.cs:130-187`) calls `RaceHorizonPolicy.Decide(request.StartDate, raceDateForHorizonCheck)` at line 132 — confirmed by direct read this is the **parameterless overload**, hardcoded to `MinimumSupportedStandaloneWeeks=8`, `ExactStandaloneCoreSupportedWeeks=12`, `MaximumSupportedStandaloneWeeks=14` (`RaceHorizonPolicy.cs:64,72,78,89-90`) — TEN_K's own bounds, not HM's real `10/14/16` bounds (`half-marathon-master.v3.json:11-15`). This runs **before** `_routeDecider.Decide(request)` (line 210), i.e. before any candidate/distance resolution occurs.

**Confirmed consequence, traced directly through the real control flow (`PlanServices.cs:130-187`):**
- A genuinely valid HM **15W or 16W** request computes `classification == CompositionRequired` (since `availableWeeks > 14`, TEN_K's own ceiling) at line 136, and — since it will not match `IsPreparationRunwayPilotScope(request)` (a TEN_K-pilot-scoped check, `PlanServices.cs:97-99`) — falls through to `throw new PlanHorizonCompositionRequiredException(...)` (line 160) **before candidate resolution ever runs**, incorrectly rejecting a request HM's own real, frozen authority fully supports.
- A genuinely invalid HM **9W** request (below HM's real 10-week minimum, but within TEN_K's own `[8,14]` window) **passes this early gate silently** and proceeds to `_routeDecider.Decide(request)` at line 210, where a separate, candidate-aware check (`LivePlanPreviewRouting.cs:145-162`, confirmed by a parallel research pass to use real per-candidate `candidateMinimumWeeks/candidateMaximumWeeks`) correctly rejects it — but via a **different mechanism than the early gate**, and not with the clean, HM-specific `TOO_SHORT` semantic §6/§14 of the governing prompt asks for.

**Classification: this is a genuine, confirmed `HM_PUBLIC_BLOCKER` — real new/modified production code is required** (parameterize or reorder this early gate to be candidate/distance-aware), not a routing-only fix. This is the single most consequential finding of this phase.

No existing typed result named `HM_STANDARD_CORE_START_TOO_EARLY` or equivalent exists; the closest current mapping for "horizon too long" is `PlanHorizonCompositionRequiredException` (HTTP 422, reason `PLAN_HORIZON_COMPOSITION_REQUIRED`) — real, typed, but currently mis-triggers for valid HM 15-16W requests per the bug above, and carries no `recommended_start_date` field today.

## 6. Frequency UX policy

`V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency`/`ResolveCandidate` (`PreviewRouting/V1CatalogPilotIdentityPolicy.cs:339-480`) is an explicit, centrally-owned `(GoalDistance, Level, DaysPerWeek)` allow-list — not exception-driven. Confirmed: an identity that fails this check does **not** reach a typed rejection today; `V1LiveCatalogPilotRoutingPolicy.Evaluate` returns `Route.Legacy`/`NotPilotRequest` (`LivePlanPreviewRouting.cs:128`), meaning an unsupported HM frequency (e.g. a hypothetical HM 2D/6D request, or Beginner 5D before this engagement froze it ineligible) **silently falls through to the old legacy SQL generation engine** rather than receiving any typed catalog-level rejection. The one hard-fail path, `ResolveCandidate`'s catch-all `throw new ArgumentOutOfRangeException(...)` (line 479), is a generic BCL exception, defensively unreachable in the live flow since callers always guard with `IsSupportedIdentity` first.

**Classification: real gap, `HM_PUBLIC_BLOCKER`.** The reusable typed-exception precedent (`CatalogVolumeUnsupportedDistanceFamilyException`, thrown at `CatalogVolumeAndLongRunPlanner.cs:160`/`CatalogFinalPrescribedPlanValidator.cs:153`) and the `LivePlanPreviewRoute`/`LivePlanPreviewRouteReason` enum family (`LivePlanPreviewRouting.cs:49-74`, e.g. `CatalogRequestUnsupported`/`UnsupportedCycleLength`) are the closest existing scaffolding a new `HmFrequencyNotSupportedV1`-style reason could extend — this must be **scoped to the HALF_MARATHON V1 matrix specifically**, not a global "days-per-week unsupported" rule (2D/6D are valid elsewhere in the product, e.g. TEN_K).

## 7. Public routing gates

Traced the complete path: `PlansController.cs` (thin dispatcher, zero distance logic — the only "TEN_K" text in the file is a doc comment at line 51) → `GenerateRacePlanPreviewRequestValidator.Validate` → `GeneratePreviewCommandMapper.ToCommand` → `PlanServices.GeneratePreviewAsync` → (early horizon gate, §5 above) → `_routeDecider.Decide` (`LivePlanPreviewRouting.cs`) → `V1CatalogPilotIdentityPolicy.IsSupportedIdentity`/`TryResolveCandidate`/`ResolveCandidate` (2-arg, **TenK-pinned** overloads only, lines 120/239/280/354 of `LivePlanPreviewRouting.cs`) → catalog generation or legacy fallback.

**The public gate itself** (`IsSupportedIdentity`, `V1CatalogPilotIdentityPolicy.cs:422-429`):
```csharp
public static bool IsSupportedIdentity(GoalType goalType, GoalDistance goalDistance, RunningBackground level, int daysPerWeek) =>
    goalType == GoalType && goalDistance == GoalDistance && IsSupportedLevelFrequency(level, daysPerWeek);
```
`GoalDistance` here is the class's own `const` (line 44) = `TenK` — so `goalDistance == GoalDistance` fails closed for any `HalfMarathon` request as a **double lock**, before the level/frequency check is even reached. `IsSupportedLevelFrequency(level, daysPerWeek)` (the 2-arg form, lines 324-325) delegates only to the 3-arg TenK-pinned overload.

The HM candidate keys (all 8 dark cells) are wired only into the **distance-aware** `IsSupportedLevelFrequency(GoalDistance,...)`/`ResolveCandidate(GoalDistance,...)` overloads (lines 339-374, 452-480) — confirmed by doc comments at every HM candidate constant (e.g. lines 178-197) to be "deliberately NOT added" to the public overload, and confirmed structurally separate by direct read.

| Gate | Classification |
|---|---|
| `PlansController` request validation | GENERIC_ALREADY |
| Early horizon gate (`PlanServices.cs:132`) | **OBSOLETE_SINGLE-DISTANCE_ASSUMPTION** (hardcodes TEN_K bounds; real blocker, §5) |
| `_routeDecider.Decide` / `V1LiveCatalogPilotRoutingPolicy` | GENERIC_ALREADY (candidate-aware, distance-blind by construction) |
| `IsSupportedIdentity` (public 2-arg overload) | **INTENTIONAL_DARK_GATE** (by design, per HM.1-HM.16's own doc comments) |
| `ResolveCandidate`/`TryResolveCandidate` (public 2-arg overloads) | **INTENTIONAL_DARK_GATE**, same reason |
| Distance-aware 3-arg overloads (already HM-wired) | GENERIC_ALREADY / **HM_PUBLIC_ACTIVATION_REQUIRED** (just needs the public overload's call site switched to consult these for the frozen HM tuples) |
| Session/volume planner HM branches (`CatalogVolumeAndLongRunPlanner.cs`, `CatalogFinalPrescribedPlanValidator.cs`) | GENERIC_ALREADY — HM arms already written and frozen (HM.1-HM.16) |
| Workout binding, calendar materialization, resolvers (`CoreEntryReadinessResolver`, `TimeAdequacyResolver`) | GENERIC_ALREADY — confirmed no distance branching anywhere |

## 8. Dark/public pipeline parity

**Conclusion: `PUBLIC_ACTIVATION_REQUIRES_ADDITIONAL_PRODUCTION_BEHAVIOR`**, not routing-only — because of §5's confirmed horizon-gate bug and §6's confirmed frequency-fallthrough gap. Every other stage in the pipeline (session/volume allocation, workout binding, calendar materialization, final validation, pace/fallback resolvers) is either already distance-generic or already carries a frozen, already-tested HALF_MARATHON branch reached identically by dark and (would-be) public traffic — confirmed directly: `CatalogVolumeAndLongRunPlanner.cs` and `CatalogFinalPrescribedPlanValidator.cs` both have explicit `CanonicalDistanceFamily == "HALF_MARATHON"` branches (lines 107/120/135 and 116/125/134 respectively) sitting alongside the TEN_K branches, guarded by the same catch-all `CatalogVolumeUnsupportedDistanceFamilyException` any unrecognized distance would hit. Once the two confirmed gaps (§5, §6) are closed, the two paths converge identically after candidate selection.

## 9. Preview readiness

`GenerateRacePlanPreviewRequest.cs` and `RecentRaceInput.cs` already carry every HM-needed field: `GoalDistance` (enum already includes `HalfMarathon`), `Level`, `DaysPerWeek`, `PreferredDays`, `LongRunDay`, `StartDate`, `RaceDate`, `RecentLongestRunKm`, `RecentWeeklyVolumeKm`, `RecentRunsPerWeek`, `RecentRace` (typed, `Distance`/`FinishTimeSeconds`/`RaceDate`). **No missing fields identified.** The DTO layer is already distance-agnostic.

## 10. Confirmation readiness

`GoalDistance` enum (`RunningApp.Domain/Enums/GoalDistance.cs:3-10`) already has `HalfMarathon`. `TrainingPlan` entity fields (`GoalDistance`, `RunningBackground Level`, `int DaysPerWeek`) are plain typed columns with no TEN_K-only constraint. `CatalogCandidateKey`/`Version`/`CanonicalDistanceFamily` are free-text/nullable strings — `CanonicalDistanceFamily` is explicitly documented in its own entity doc comment as "the catalog's own literal string... not collapsed into backend enums," HM-safe by design. **Grep for `TEN_K`/`TenK`/`HALF_MARATHON` across the entire `RunningApp.Persistence` project (entities + every migration) returned zero matches** — no distance-specific column, default, or CHECK constraint exists anywhere in the schema history. `TrainingWeek.WeekNumber` and `TrainingDay` fields carry no max-week or max-session-count constraint. **Verdict: `HM_CONFIRMATION_PERSISTENCE_READY` at the schema level.**

**However, the dispatch mechanism itself is a real, confirmed risk, independently re-verified by direct read** (`PlanServices.cs:506,519-521,1019-1041`): `ConfirmPlanAsync` dispatches catalog-vs-legacy confirmation based solely on whether the *stored preview's own* `generation_source` JSON field equals the catalog sentinel value (`IsCatalogSourcedPreview`, checked via `doc.RootElement.TryGetProperty("generation_source", ...)`) — **not** on `GoalDistance`. This means the persistence-safety guarantee for any given HM plan depends entirely on that specific preview having actually been generated via the catalog route (`routeDecision.Source == GenerationSource.Catalog`, `PlanServices.cs:215`), not on it being a HALF_MARATHON request per se. **No existing invariant proves this holds for every public HM V1 cell — this must be established with direct tests, not assumed** (this becomes HM.18's own §15 mandatory item).

## 11. Persistence readiness

Session/day representation: **one `TrainingDay` row = one workout on one calendar date**, with a single `CatalogSlotRole`/`Title`/`Description`. For Advanced 5D's dual-KEY-lane days, this legacy shape has **no field for a second same-day workout** — two KEY sessions on a date would need two separate `TrainingDay` rows sharing that date. No unique `(WeekId, Date)` constraint was found in any migration searched (`AddAdaptationEngineConstraints`, `AddOnboardingSnapshotFields`, `FixLongRunDayCheckConstraintFullDayNames`, and others), so this is not schema-prohibited, but it is **untested** for the legacy path. Separately, the **RollingLongHorizon** read/mutation model (`LongHorizonActiveReadModelProvider.cs`, `LongHorizonRollingSessionMutationService.cs`) already stores **one row per session** (not per day) and its role codec (`LongHorizonSessionRoleCodec.cs:85-90`) already explicitly anticipates multiple same-role sessions per week (matching suffixed forms like `EASY_SUPPORT_1`/`EASY_SUPPORT_2` via `StartsWith`) — this model is **structurally ready** for dual-KEY-lane sessions without any schema or code change.

**The open question is which of these two models an HM public plan actually lands in**, which is determined by the confirm-time dispatch (§10) — not a schema defect in either model individually.

## 12. Active-read readiness

Traced `QueryAndMutationServices.cs` (Home, Calendar, TrainingDay detail, Complete, Not-Today create/confirm, Pending confirmations), `PlanServices.cs` (Cancel, Active plan details), `PlaceholderAdaptationEngine.cs`, and the RollingLongHorizon equivalents directly. **Zero `TEN_K`/`TenK`/`HalfMarathon` string or enum branches found anywhere in these files.** All reads key off persisted entity data generically (`PlanId`, `WeekId`, `Date`, `Status`), with week/session counts derived from the DB (`weeks.Count`, `TrainingWeek.StartDate` boundaries), never a fixed literal. Session identity is the entity's own `Guid Id`, safe for any distance. The only schedule-type branch found anywhere (`plan.ScheduleStrategy == PlanScheduleStrategy.RollingLongHorizon`) is a generic strategy enum, not a `GoalDistance` check.

**Verdict for both models: `HM_SAFE_ALREADY`** for Home/Calendar/TrainingDay-detail/Profile (generic, `NOT_APPLICABLE` to distance) — with the one caveat already named in §11 (legacy single-workout-per-day shape, conditional on dispatch, not itself a read-path defect).

## 13. Mutation readiness

Complete session, Not-Today (create+confirm), Pending confirmations, Cancel plan, adaptation (`PlaceholderAdaptationEngine.cs`, always returns static `AdaptationAction.NoChange` with no distance logic) — all confirmed **`HM_SAFE_ALREADY`** for both the legacy and RollingLongHorizon models, by direct read, with no `TEN_K`-only/single-KEY/four-sessions/12-week literal found anywhere in the mutation code paths. `ScheduleRepairSpacingValidator.cs:49-70` (Not-Today-triggered adaptation) filters **all** sessions in a week matching a role via a collection `.Where(...)` check, not a "the one KEY session" singular lookup — already tolerant of two KEY sessions per week by construction, though not yet exercised against a real dual-KEY-lane HM fixture (an untested-but-structurally-sound state, not a known defect).

## 14. Frontend contract

**A frontend exists in this repo** at `mobile/lib/` (Flutter). Confirmed by direct read: it does **not** currently constrain user input to backend-supported combinations at all.
- **Distance**: `race_details_page.dart:44-61` takes a free-text numeric distance field, mapping `21.1`→`half_marathon` — **HALF_MARATHON is already fully reachable from the UI today**, with no allow-list.
- **Level**: `running_background.dart:17-21` presents all four levels regardless of chosen distance — no distance-conditioned filtering.
- **Frequency**: `weekly_frequency_page.dart:65` offers `[2,3,4,5,6]` for any race-goal distance/level — **2D and 6D are directly selectable for HALF_MARATHON today**, a cell the backend allow-list has never exposed for any level.
- **Horizon**: no 10–16-week UI constraint exists; `onboarding_provider.dart:15`'s `_staticRaceMaxWeeks=20` only picks which backend endpoint to call, it doesn't reject a span. Users can submit any horizon; rejection is left entirely to the backend, surfaced via `plan_generation_error_mapper.dart:8-17` (`PLAN_HORIZON_COMPOSITION_REQUIRED`, `PLAN_CORE_HORIZON_UNSUPPORTED`).

**Conclusion: the backend's own allow-list is the only real gate today** — the frontend does not, and currently cannot, prevent a user from submitting a HALF_MARATHON/2D or /6D/Beginner-5D/out-of-range-horizon request; it will submit it and show whatever typed (or untyped 500) error comes back. This is not itself a blocker (the backend fails closed regardless), but it does mean **every typed-error gap identified in this report (§5, §6, plus the readiness/placement gaps below) is directly user-reachable today the moment HM's public identity gate opens**, since nothing on the client side pre-filters bad requests.

## 15. Typed horizon errors

No existing type named for `<10W` or `>16W` specifically. `PlanHorizonCompositionRequiredException` (`AppExceptions.cs:487-509`, `PLAN_HORIZON_COMPOSITION_REQUIRED`) is the real, typed mechanism currently used for "too long" (mis-triggering for HM 15-16W per §5) — no dedicated `<10W` type exists; a too-short HM request today would fall through to whatever the later candidate-aware check in `LivePlanPreviewRouting.cs` produces (not confirmed to be a clean, HM-labeled "too short" result — likely a generic `RuntimeConditionUnsupportedException` or similar, per the family found in §16 below).

## 16. Typed readiness errors

Confirmed real, working, typed infrastructure already exists (§4): `CoreEntryReadinessResolver` produces named `NOT_READY`/`CORE_ENTRY_NOT_READY` results. Separately, `PlanProductIneligibleException` with a `Reason` string (`AppExceptions.cs:171-180`) plus concrete reason codes in `CatalogVolumeExceptions.cs` (e.g. `ADVANCED_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE`, `TWO_DAY_MISSING_OR_ZERO_READINESS_NOT_ELIGIBLE`) is the real, established pattern for "insufficient readiness → typed ineligibility." **This governance requirement's underlying infrastructure already exists**; HM.18's own job is to confirm/wire an HM-scoped reason code through this same architecture, not invent a new one.

**Two real, confirmed gaps in the broader typed-error inventory:**
- **Stretch goal**: exists only as internal metadata (`GoalFeasibilityResolver.cs:258-265`, `"STRETCH"` band), explicitly documented "Never returned as OutputValue" — no typed warning surfaces this to any caller today. Per the governing prompt's own §13, this is explicitly **`POST_V1_NON_BLOCKING_UX_DEBT`**, not a blocker.
- **Preferred-day placement infeasibility**: `LongHorizonActivatedCalendarAlignmentValidator.cs:38` throws a generic `LongHorizonActivatedCalendarAlignmentException` with a free-text message ("is not on a preferred day") — not a distinct typed/enum category. **This is a real, confirmed gap** requiring a stable reason code (e.g. `PREFERRED_DAY_PLACEMENT_INFEASIBLE`) layered onto the existing exception, not a calendar-policy redesign.

## 17. Beginner-5D product-ineligible behavior

Confirmed via a real, existing internal test seam: `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.cs:375-393` directly asserts `V1CatalogPilotIdentityPolicy.TryResolveCandidate(GoalDistance.HalfMarathon, RunningBackground.Beginner, 5)` returns **null** (while Intermediate×5D resolves non-null) — the distance-aware resolver already correctly distinguishes this cell. **The remaining gap is purely on the public-surfacing side**: today, since HALF_MARATHON isn't in the public gate at all, there is no public-facing typed result to inspect yet; once the public gate opens for the other 8 HM cells, Beginner×5D specifically must be proven to still return a `PRODUCT_INELIGIBLE`-equivalent typed result (not "template not found") through the *public* path, not just the internal `TryResolveCandidate` seam. Not yet proven end-to-end publicly, since no public path exists yet — this becomes an explicit HM.18 acceptance test, not a currently-broken behavior.

## 18. 2D/6D unsupported behavior

No dedicated typed-rejection test exists for HM 2D/6D specifically (both frequencies are valid, supported frequencies for other distance/level combinations — e.g. TEN_K 6D Intermediate is real public production per `Freq6D27IntermediateSixDayPublicActivationTests.cs`), so a **global** "unsupported frequency" rule would be actively wrong. The exact required behavior (per §6/§10 of the governing prompt) is a HALF_MARATHON-scoped typed rejection, layered onto the same allow-list mechanism already governing every other HM cell — mechanically identical in shape to the Beginner-5D case, just for a different reason code (`HM_FREQUENCY_NOT_SUPPORTED_V1` vs `PRODUCT_INELIGIBLE`). No new catalog combination should ever be created for these.

## 19. Date-boundary proof

Canonical arithmetic confirmed in §5 (`CoreHorizonClassifier.Classify`, `RaceHorizonPolicy.cs`). **Existing boundary tests are all keyed to TEN_K's own `[8,12,14]` window**: `CoreHorizonClassifierTests.ExactWeekHorizons_ClassifyAgainstCatalogBounds` covers `InlineData(7)` through `InlineData(20)` against TEN_K's bounds (7→Unsupported, 8→CompressedCore, 12→PreferredCore, 14→ExtendedCore, 15→PreparationRunwayPlusCore) and `PartialDayHorizons_AreClassifiedByDays_NotCeilingWeeks` proves the no-ceiling-rounding invariant generically. **No existing test exercises HM's own real `9/10/16/17`-week boundary values.** This is a confirmed, real test-coverage gap (not a code defect — the underlying classifier is generic and would very likely classify these correctly once fed HM's own real bounds) that HM.18 must close directly, per its own §27/§30 requirements.

## 20. Payload compatibility

Not independently re-verified in full detail this phase (time-bounded); based on the DTO/entity findings in §9-11, no `TEN_K`-specific field or hardcoded label was found anywhere in the request/response/persistence layer that would leak into an HM payload. This should be re-confirmed directly against the actual preview response DTO during HM.18's own implementation work as a lightweight sanity check, not re-audited from scratch.

## 21. Warning categories

Inventory (existing infrastructure, confirmed): insufficient readiness (`PlanProductIneligibleException`+reason, §16); goal-not-evaluated/missing-pace-evidence (`NotEvaluatedReasonCategory`/`NotEvaluatedReasonClassifier.cs:13-88`, e.g. `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`→`Unsupported`); horizon too short/too long (§15, one real gap); frequency unsupported (§6/§18, one real gap); preferred-day-placement-infeasible (§16, one real gap); stretch goal (§16, explicitly non-blocking per governing prompt §13, internal-only by design).

## 22. Rollout/feature-gate readiness

`V1CatalogPilotIdentityPolicy.cs`'s `(GoalDistance, Level, DaysPerWeek)` pattern-match switch **is** the rollout mechanism — not a runtime flag, a **compile-time, per-cell C# switch** requiring a code change + redeploy per activation (not instant runtime toggle). This is the exact same mechanism that has governed every prior TEN_K frequency's own historical public activation (named precedents referenced in this repo's own doc comments: GEN.20, GEN.25, FREQ.6D.27). All 8 target HM cells already have named candidate constants, real catalog bundles, and full dark test coverage — turning one cell public requires exactly two changes: (1) widen the public overload's consulted switch to include that `(HalfMarathon, Level, Days)` tuple, (2) flip that candidate's catalog-bundle `metadata.status` to `PUBLISHED` (gated by `CatalogCandidateEligibilityGate.cs:91-96`). No database table or JSON config drives this decision — `appsettings.json`'s `CatalogLivePilot.Enabled`/`PreparationRunwayPilotActivation.Enabled` are coarse, all-or-nothing, Development-only-guarded booleans (`CatalogCandidateEligibilityGate.cs:75-79`), not a per-cell mechanism.

**Verdict: reversible in the sense of "revert the code change and redeploy," not "flip a runtime switch."** This satisfies the governing prompt's own "prefer a reversible rollout... without code deletion/redeploy gymnastics" bar loosely (per-cell activation needs no *deletion*, just a small additive/revertable diff) but should be documented honestly as code-change-based, not instant-toggle-based.

## 23. DB/schema/migration result

**`NO_MIGRATION_REQUIRED`** — confirmed by direct grep across all of `RunningApp.Persistence` (entities and every migration file): zero `TEN_K`/`TenK`/`HALF_MARATHON` references anywhere, zero CHECK constraints referencing `GoalDistance`/`Level`/`DaysPerWeek`/`WeekNumber`, and `GoalDistance.HalfMarathon` already exists as an enum value. See §10-11 for the one *content-shape* caveat (dual-KEY-lane days via the legacy single-workout-per-day mapper), which is a code/dispatch question, not a schema question.

## 24. Performance/count assumptions

**None found that would block HM's larger shape.** The generation pipeline is catalog/JSON-bundle-driven, not fixed-size-array based; the only pre-allocations found (`LongHorizonDarkExecutionOrchestrator.cs:91,119`, `new List<T>(4)`) are growable initial-capacity hints, not hard caps. The one real numeric ceiling in the pipeline, `RaceHorizonPolicy`'s own `MaximumSupportedStandaloneWeeks=14`, is exactly the confirmed bug in §5 — not a performance constraint, a distance-scoping bug.

## 25. V1 cell-by-cell readiness matrix

| Cell | Dark-gen | Confirmation | Persistence | Active reads | Mutations | FE contract | Rollout gate |
|---|---|---|---|---|---|---|---|
| Beginner 3D | ✅ (HM.14) | ⚠️ needs catalog-source proof | ✅ schema-ready | ✅ | ✅ | ⚠️ FE doesn't restrict yet | ✅ mechanism exists |
| Beginner 4D | ✅ (HM.14) | ⚠️ same | ✅ | ✅ | ✅ | ⚠️ same | ✅ |
| Beginner 5D | N/A (`PRODUCT_INELIGIBLE`, HM.13) | N/A | N/A | N/A | N/A | ⚠️ FE currently *allows* selecting it | ✅ (must stay closed) |
| Intermediate 3D/4D | ✅ (HM.2/HM.8) | ⚠️ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| Intermediate 5D | ✅ (HM.11) | ⚠️ | ✅ (rolling model already dual-KEY-ready) | ✅ | ✅ | ⚠️ | ✅ |
| Advanced 3D/4D | ✅ (HM.16) | ⚠️ | ✅ | ✅ | ✅ | ⚠️ | ✅ |
| Advanced 5D | ✅ (HM.16, genuine 2nd true-hard KEY2) | ⚠️ **highest-priority proof needed** — dual-KEY round-trip unverified end-to-end | ✅ structurally (rolling model), ⚠️ legacy model has no same-day-second-workout field | ✅ (rolling model already anticipates multi-same-role sessions) | ✅ (collection-based role filters, not singular) | ⚠️ | ✅ |

Legend: ✅ = confirmed ready by direct evidence; ⚠️ = plausible/structurally sound but not yet proven by a real end-to-end test — this is exactly what HM.18 §17-18 must close.

## 26. Exact PRE-PUBLIC blockers

Applying the governing prompt's own §29 standard (a blocker only if HM V1 cannot safely perform request-eligibility / preview / confirm-persist / active-read / active-mutation / typed-error / rollout-control for an in-scope user):

1. **Horizon-gate distance-blindness** (§5): `PlanServices.cs:132`'s early gate hardcodes TEN_K's `8/12/14`-week bounds, which would incorrectly reject valid HM 15-16W requests and incorrectly under-reject invalid HM 9W requests at the wrong layer. **Real production code change required.**
2. **Frequency-unsupported silent fallthrough** (§6/§18): unsupported catalog identities (including future HM 2D/6D, and Beginner 5D once the public gate opens) fall through to the legacy engine rather than a typed rejection. **Real production code addition required** (a new, HM-scoped typed reason, reusing existing exception/enum families).
3. **Preferred-day placement has no typed category** (§16): only a generic exception with a free-text message exists. **Small, additive typed-error work required.**
4. **Catalog-source invariant is unproven** (§10): the confirm-dispatch mechanism is content-driven (a stored `generation_source` field), not distance-driven — no existing test proves every public HM V1 preview will actually be catalog-sourced. **Direct tests required before activation; this is the load-bearing safety proof for the entire persistence chain.**

These four map directly to the four blockers named in the next phase's own objective — this report is their evidentiary basis.

## 27. Explicit NON-BLOCKERS

- Stretch-goal public warning (§16, §21) — explicitly `POST_V1_NON_BLOCKING_UX_DEBT` per governing prompt §13.
- Legacy single-workout-per-day mapper's dual-KEY limitation (§11) — **conditionally** non-blocking: it is only a real defect if an HM public preview can ever reach legacy confirmation, which depends entirely on blocker #4 above being closed. If blocker #4 is proven (every HM preview is catalog-sourced), this legacy limitation becomes `UNREACHABLE_LEGACY_LIMITATION` and must not be modified.
- Frontend's current lack of input restriction (§14) — not itself a backend blocker (the backend fails closed regardless of what the client submits), though it does mean every typed-error gap above is immediately user-reachable the moment any HM cell opens, which is a real reason to close them promptly, not a reason to delay activation further.
- 2D/6D/7-9W/≤6W/>16W/missing-readiness-base-building (§2/§3) — explicit, permanent post-V1 scope per the governing prompt itself; their absence is definitionally not a blocker.
- Database migration (§23) — none required.
- Performance/payload-size constraints (§24) — none found.
- Rollout mechanism granularity (§22) — existing per-cell C# switch is sufficient; no new infrastructure needed.

## 28. HM.18 activation plan

Close the four blockers in §26 with real, tested, additive code (typed horizon contract including a stable `>16W` result with advisory `recommended_start_date`; typed HM-scoped frequency-unsupported result; typed preferred-day-infeasible result; a proven catalog-source invariant for every public HM V1 cell, especially Advanced 5D's dual-KEY shape). Only after all four are proven, plus a full regression showing zero new failures beyond the documented durable baseline, should the public identity gate itself be widened for the frozen 8-cell matrix (never Beginner 5D, never 2D/6D). Recommend the same phased, mirrored governance→implementation split this entire engagement has already used successfully (HM.12→HM.13, HM.15→HM.16) be applied here too: a focused implementation phase closing the four blockers with full test proof, followed by — only after explicit review — the actual gate-widening commit.

---

## FINAL CLASSIFICATION

```
HM_V1_PUBLIC_SCOPE_AND_ACTIVATION_READINESS_CLOSED
```

V1 scope is frozen (10-16W Standard Core); the frequency/level matrix is frozen; 2D/6D/compressed/readiness-only/runway/longhorizon are explicit, permanent post-V1 scope; missing/zero history is confirmed already handled as typed ineligibility, not fake substitution (§4); every dark-complete V1 cell has a known, mapped path through preview/confirm/persistence/read/mutation with exactly four explicit, engineering-bounded gaps identified (§26); every expected out-of-scope request has a defined required typed behavior even though not yet implemented (§6/§18); rollout activation is achievable via the existing per-cell mechanism, documented honestly as code-change-based rather than instant-toggle (§22); every remaining blocker is explicit, small, and engineering-bounded — none requires new architecture, new training authority, or reopening any frozen HM.1-HM.16 decision.
