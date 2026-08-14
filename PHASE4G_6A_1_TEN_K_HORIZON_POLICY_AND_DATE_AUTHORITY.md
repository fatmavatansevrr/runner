# Phase 4G.6A.1 — 10K Horizon Policy Formalization and Narrow Preparation-Runway Date-Authority Reconciliation

> This document formalizes product/architecture decisions for `TEN_K__4D__INTERMEDIATE v10` (the only currently executable race-plan pilot) and reconciles a one-day arithmetic conflict in the dark, unwired Preparation Runway contract. It records future-phase direction; it does not implement any of the branches it describes beyond the 8-14 week range already live before this pass. See Section 8 for the exact executable/non-executable boundary and `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` for the machine-readable follow-up record.

## 1. Executive result

**Part 1 (documentation):** the generic `CompositionEntryBoundary`/`MaximumRacePreparationWindowWeeks` formulas are recorded as candidate-scoped policy shapes; the current 10K pilot's numeric instantiation (15-week entry boundary, 20-week runway maximum, 52-week outer product boundary) is recorded and explicitly scoped to this one candidate; the future 8-14/15-20/21-52/53+ horizon-policy table and the 21-52 staged single-plan architecture are recorded as approved direction, not implemented capability, with a new TD (`TD-GENERAL-ENDURANCE-STAGED-PLAN-001`) keeping the unresolved work explicitly blocking.

**Part 2 (technical):** the one-day inclusive/exclusive date-authority conflict identified in `PHASE4G_6A_PREPARATION_RUNWAY_COMPOSITION_POLICY_AUDIT.md` is fixed. `PreparationRunwayContextValidator`'s core-start check now uses the same exclusive-day convention `CoreHorizonClassifier` already uses (removing a stray `-1`), and a new, generic, RaceDate-free `PreparationRunwayDateAuthority.Derive` helper consumes canonical `AvailableFullWeeks`/`LeadingPartialDays`/`PreferredCoreWeeks` values directly, with no knowledge of any specific week-count policy boundary. No allocator, schema, workout, persistence, or public-behavior change was made. All 8-14 week regression tests and both full backend suite runs pass unchanged.

## 2. Current verified executable scope (unchanged by this phase)

- Goal type: Race; `RequestedTargetDistanceKm`: exact 10.0 km; `CanonicalDistanceFamily`: `TEN_K`; `DaysPerWeek`: 4; Background: Intermediate.
- Candidate: `TEN_K__4D__INTERMEDIATE v10`.
- Current preferred core duration: 12 full weeks (`candidate.CoreCycle.DefaultWeeks`, confirmed by a new regression test — Section 9).
- 8-14 available full weeks: standalone core, HTTP 200, unchanged by this phase.
- 15+ available full weeks: HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`, unchanged by this phase.

This phase does **not** broaden executable support to non-exact intermediate distances, `FIVE_K`, `HALF_MARATHON`, `MARATHON`, other day-count candidates, or other background candidates.

## 3. Generic candidate-scoped formulas (A — generic formulas)

```text
CompositionEntryBoundary(candidate) =
    PreferredCoreWeeks(candidate) + MinimumRunwayWeeks(candidate or applicable runway policy)

MaximumRacePreparationWindowWeeks(candidate) =
    PreferredCoreWeeks(candidate) + MaximumRunwayWeeks(candidate or applicable runway policy)
```

`PreferredCoreWeeks` must be candidate-resolved (via the candidate's own `CoreCycle.DefaultWeeks`), never independently inferred from distance alone. These formulas are recorded here as the approved generic **shape** — they are not universal numeric constants; 15 and 20 (Section 4) are this one candidate's own instantiation, not a claim about every future distance family or candidate.

## 4. Current 10K pilot instantiation (B — 10K pilot instantiation)

For `Candidate = TEN_K__4D__INTERMEDIATE v10`:

| Value | Number | Classification |
|---|---|---|
| `PreferredCoreWeeks` | 12 | Existing canonical candidate behavior (`candidate.CoreCycle.DefaultWeeks`) |
| `MinimumRunwayWeeks` | 3 | `PRODUCT_DEFAULT_FOR_CURRENT_10K_PILOT` |
| `MaximumRunwayWeeks` | 8 | `PRODUCT_DEFAULT_FOR_CURRENT_10K_PILOT` |
| `CompositionEntryBoundary` | 12 + 3 = **15** | `DERIVED_10K_PILOT_VALUE` |
| `MaximumRacePreparationWindowWeeks` | 12 + 8 = **20** | `DERIVED_10K_PILOT_VALUE` |

**Scope**: `TEN_K__4D__INTERMEDIATE v10` only. 3 and 8 are **not** claimed as already-approved global runway limits for every future candidate — they are this pilot's own product defaults, recorded here for this candidate specifically.

## 5. Approved 10K horizon policy (C — product-wide policy intent, recorded as future direction) (E — approved but not implemented architecture)

| Available full weeks | Future classification | Status |
|---|---|---|
| 8-14 | Existing standalone-core behavior | **D — currently implemented** |
| 15-20 | Future direct Preparation Runway + 12-week Race Core composition. `RunwayWeeks = AvailableFullWeeks - 12`. Future supported runway range: 3-8 full weeks. | **E — approved, not implemented** |
| 21-52 | Future staged single-plan architecture: `GENERAL_ENDURANCE → PREPARATION_RUNWAY → RACE_CORE` | **E — approved direction, not implemented** (see Section 7, `TD-GENERAL-ENDURANCE-STAGED-PLAN-001`) |
| 53+ | Future typed rejection: `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW` | **E — approved, not implemented** |

None of the 15-20/21-52/53+ branches are implemented by this phase. 8-14 remains the only implemented row, unchanged.

## 6. Outer product horizon boundary (C — product-wide policy intent)

```text
MaximumSupportedFullWeeks = 52

AvailableFullWeeks <= 52  → inside the approved outer product window
AvailableFullWeeks >= 53  → outside the approved outer product window
```

Examples: 52w0d through 52w6d are inside the outer boundary; 53w0d and above are outside it.

Future mechanical (non-personalized) recommendation: `recommendedStartDate = RaceDate - 364 days`.

`52_WEEK_OUTER_BOUNDARY = PRODUCT_DEFAULT`. `PolicyIntent = PRODUCT_WIDE_OUTER_BOUNDARY`. `CurrentExecutableScope = TEN_K_PILOT_ONLY`. The 52-week boundary is a **V1 product-support boundary**, not a physiological maximum. The 53+ HTTP behavior and response payload are **not implemented** by this phase (**G — explicitly out of scope**).

## 7. Partial-day policy (C — product-wide policy intent)

`LeadingPartialDays` valid range: 0-6. Partial days: do not create an additional `TrainingWeek`; do not create `TrainingDay` records; do not increase `RunwayFullWeeks`; remain alignment information; are intended to be persisted later as typed alignment metadata (**not part of this phase — G**).

Examples: 14w1d → 14 full weeks, 1 alignment day, no extra week. 15w6d → 15 full weeks, 6 alignment days, no extra week. 52w6d → 52 full weeks, 6 alignment days, still inside the outer boundary.

## 8. Approved staged single-plan architecture (C, E, F)

Recorded as future binding architecture for 21-52 full-week horizons:

- One user-visible Race `TrainingPlan`; one `TrainingPlan.Id` from confirmation through race day; the user never sees a separate hidden Habit `TrainingPlan`.
- Internal segment provenance must eventually distinguish `GENERAL_ENDURANCE` / `PREPARATION_RUNWAY` / `RACE_CORE`.
- Exact distant-future workouts are not frozen at initial confirmation; near-term content is materialized incrementally.
- Transition into the race-preparation window is time-triggered; evidence is an input, not a transition blocker. At transition, the latest application-derived evidence is used where available; missing evidence uses the separately-approved missing-evidence policy.
- A second public plan confirmation is not required. Public UX may present one continuous race plan with passive phase explanation.

| Classification | Value |
|---|---|
| `STAGED_SINGLE_PLAN` | `APPROVED_ARCHITECTURAL_POLICY` |
| `ROLLING_GENERAL_ENDURANCE` | `APPROVED_DIRECTION_NOT_YET_IMPLEMENTED` |
| `HABIT_STYLE_PLANNER_REUSE` | `DESIGN_INTENT_NOT_YET_PROVEN` |

**This document does not claim the Habit planner already supports safe rolling generation for up to 32 General Endurance weeks** (**F — design intent not yet proven**). That claim would require dedicated capacity/safety research this phase does not perform.

## 9. Part 2 — narrow dark date-authority reconciliation

### 9.1 Canonical horizon authority (preserved, unchanged)

`CoreHorizonClassifier`/`CoreHorizonDecision` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs`) remains the sole authority for elapsed horizon, `AvailableFullWeeks`, `LeadingPartialDays`, and horizon classification — unchanged by this phase. `RaceHorizonPolicy` remains a pure mapper over `CoreHorizonClassifier.Classify`'s output (confirmed by direct source inspection — it performs no independent date arithmetic) — unchanged.

### 9.2 Conflicting arithmetic found and removed

`PreparationRunwayContextValidator.Validate` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunway/PreparationRunwayValidators.cs`) computed:

```csharp
// REMOVED
var expectedCoreStart = context.RaceDate.AddDays(-((context.PreferredCoreWeeks * 7) - 1));
```

an **inclusive**, race-anchored 84-calendar-day span (both the core-start day and RaceDate counted), which disagreed with `CoreHorizonClassifier`'s **exclusive** elapsed-day convention (`RaceDate.DayNumber - StartDate.DayNumber`) by exactly one day. Concretely, a real 15w0d exclusive-elapsed horizon previously validated a 22-day (3w1d) runway instead of the correct 21-day (3w0d) runway — exactly the discrepancy `PHASE4G_6A_PREPARATION_RUNWAY_COMPOSITION_POLICY_AUDIT.md` section 14 identified.

**Fix**: the `-1` was removed:

```csharp
// CURRENT
var expectedCoreStart = context.RaceDate.AddDays(-(context.PreferredCoreWeeks * 7));
```

This is now algebraically consistent with the canonical exclusive convention: for any `AvailableFullWeeks`/`LeadingPartialDays`/`PreferredCoreWeeks`, `StartDate + RunwayDays + PreferredCoreWeeks*7 = RaceDate` exactly, with no off-by-one. The pre-existing `RunwayDays = CoreStartDate.DayNumber - StartDate.DayNumber` check (also in `PreparationRunwayContextValidator`) was already exclusive and correct — it required no change.

### 9.3 New generic dark arithmetic

A new type, `PreparationRunwayDateAuthority` (added to `PreparationRunwayContracts.cs` — no new file was added; the Preparation Runway production folder still contains exactly 2 `.cs` files, matching its own established `Neutrality_...` governance test), consumes decision-derived values directly:

```csharp
public static PreparationRunwayDateAuthorityResult Derive(
    DateOnly startDate, int availableFullWeeks, int leadingPartialDays, int preferredCoreWeeks)
{
    var runwayFullWeeks = availableFullWeeks - preferredCoreWeeks;
    var runwayPartialDays = leadingPartialDays;
    var runwayDays = (runwayFullWeeks * 7) + runwayPartialDays;
    var coreStartDate = startDate.AddDays(runwayDays);
    return new PreparationRunwayDateAuthorityResult(runwayFullWeeks, runwayPartialDays, runwayDays, coreStartDate);
}
```

This method takes **no `RaceDate` parameter at all** (structurally proven by a reflection-based test — Section 10) — it cannot reintroduce race-anchored arithmetic even by accident. It is generic: it contains no branch, comparison, or knowledge of 15/20/21/52/53 weeks or any composition-eligibility/staged-eligibility/outer-bound-rejection judgment. Eligibility and product policy remain owned by later orchestration phases, per this phase's own restriction.

### 9.4 `PreferredCoreWeeks` authority — finding

Repository inspection confirmed `PlanCatalogCoreCycle.DefaultWeeks` (`backend/RunningApp.Application/RuntimeCatalog/PlanCatalogCandidateSummary.cs`) is **already candidate-scoped** — it is populated from each master template's own `coreCycle.defaultWeeks` JSON field (12 for `TEN_K__4D__INTERMEDIATE v10`'s `ten-k-master.v6.json`) and is already the value every live horizon call site (`RaceHorizonPolicy.Decide`, `CatalogPreviewGenerator`) passes as `preferredCoreWeeks`. **No redesign was needed or performed.**

Separately, `PreparationRunwayContext.PreferredCoreWeeks` itself was found to have **no production producer at all** — it is a plain field with exactly one construction site in production code (`RacePlanCompositionMetadataValidator.Validate`, which only re-wraps an already-given value for validator reuse, never derives 12 from a candidate). This is unchanged by this phase — wiring a real candidate → `PreparationRunwayContext` pipeline would be allocator/composition work, explicitly out of scope. `PreparationRunwayDateAuthority.Derive` instead takes `preferredCoreWeeks` as a plain parameter, so no value is hard-coded inside the Preparation Runway component; a new regression test (Section 10) proves the real candidate's own resolved value is 12 and, fed through `Derive`, produces the documented 10K-pilot instantiation.

## 10. Test coverage added

- `PreparationRunwayDateAuthorityTests.cs` (new): the full required matrix (14w0d/1d/6d, 15w0d/1d/6d, 20w0d/6d, 21w0d, 52w6d, 53w0d — arithmetic-only for the last three, no interpretation), a genericity proof running 8-14-week standalone horizons through the identical `Derive` call, a reflection-based structural proof that `Derive` has no `DateOnly` race-date parameter, a source-scan proof that the removed inclusive formula fragment is gone from both production files, a regression test reproducing the exact original 15w0d one-day mismatch and confirming the corrected validator now rejects the old (buggy) value and accepts only the corrected one, and a real-candidate regression proving `TEN_K__4D__INTERMEDIATE v10 → PreferredCoreWeeks = 12`.
- `PreparationRunwayContractsTests.cs` (existing file, updated): `CoreStart`/`Metadata()` fixture constants shifted by exactly one day to match the corrected exclusive convention — all 52 pre-existing tests re-run and pass unchanged in substance (same relationships, corrected absolute reference frame).

## 11. Follow-up phase sequence (recorded, not implemented)

```text
Phase 4G.6A.1 → horizon decision formalization + narrow date-authority reconciliation (this phase)
Phase 4G.6A.2 → 10K Preparation Runway coefficient/min/max/priority/eligibility decision resolution
Phase 4G.6A.3 → generic deterministic constrained proportional allocator, dark only
Phase 4G.6A.4+ → runway catalog/schema/progression, workout binding, volume, long run, pace, calendar, persistence
Later activation phase → 15-20 week direct runway + 12-week core localhost activation
Separate research phase → General Endurance evidence review and policy resolution
Separate implementation series → 21-52 week staged General Endurance materialization and transition
Separate future phase → intermediate-distance family classification and projection
```

## 12. Governance gate

- `TD-RUNWAY-VALIDATOR-EXHAUSTIVENESS-001`: remains `CLOSED`; its own status-independent `CompositionType`/`RunwayDays` consistency check is untouched by this phase's day-count fix.
- `TD-COREHORIZON-ALLOCATOR-UNWIRED-001`: remains `CLOSED`; this phase does not touch the allocator/`CoreHorizonDecision` wiring it closed.
- `TD-ALLOCATION-PRIORITY-001`, `TD-FOUNDATION-COMPRESSION-001`, `TD-VOLUME-CAP-UNENFORCED-001`: remain `CLOSED`, unaffected — this phase touches only the Preparation Runway folder and documentation.
- `TD-NOTEVALUATED-FALLBACK-001`: remains `OPEN`, unaffected.
- **New**: `TD-GENERAL-ENDURANCE-STAGED-PLAN-001` — added `OPEN`, recording the 21-52 week staged architecture as approved direction, not implemented capability, per this phase's own Section 8/13.

## 13. Explicit non-claims

- General Endurance was neither researched nor implemented in this phase.
- No Preparation Runway allocator, block minima/maxima/coefficients/weights/eligibility, deterministic largest-remainder allocation, General Endurance contracts/services, Habit-planner reuse, staged materialization, composition scheduler, transition service, `TrainingPlan` composition status, `TrainingWeek` `SourceSegment`, segment-provenance schema, database migration, workout binding, workout-catalog addition, volume/long-run progression, pace resolution, calendar-assignment change, read-model change, preview/confirmation response change, `recommendedStartDate` response, new HTTP status mapping, or any activation was implemented.
- No `TrainingWeek` or `TrainingDay` was generated by this phase.
- Public and persistence behavior are unchanged (Section 14).

## 14. Public and persistence containment

Public: 8-14 week behavior unchanged; 15+ week public preview behavior remains blocked (HTTP 422 `PLAN_HORIZON_COMPOSITION_REQUIRED`); no runway/General-Endurance/staged-plan output is exposed; no new response fields; no new HTTP status mapping.

Persistence: no new `TrainingPlan`/`TrainingWeek`/`TrainingDay` fields; no segment rows; no alignment metadata; no migration; no new plan persistence; no hidden second Habit plan.
