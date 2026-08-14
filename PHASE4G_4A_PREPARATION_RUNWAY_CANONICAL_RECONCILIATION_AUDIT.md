# Phase 4G.4A — Preparation Runway Canonical Source Reconciliation Audit

## 1. Scope, method, and source limitation

This is a read-only reconciliation and typed-contract-sketch audit. It does
not implement a planner, allocator, resolver, calendar materializer, catalog
artifact, verifier, or live route.

The repository's current imported artifact,
`plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md`, contains
resolver material in sections A–D and does **not** contain the cited doc13
sections 4, 5, 7, 9, or 10. Its own provenance says its content was imported
from conversation-supplied material. Consequently, this audit treats the
runway rules supplied in the Phase 4G.4A request as a
`PRODUCT_DEFAULT / IMPORT_CANDIDATE`, not as text independently recoverable
from the current repository copy and not as external evidence. The earlier
research findings are reused only at the classification level requested here;
no new literature search was performed.

The only dynamic input considered is experience level. Readiness, recent
weekly volume, longest run, run frequency, block prescriptions, and session
content personalization are deliberately out of scope.

## 2. Vocabulary reconciliation

### 2.1 Experience vocabulary

The current plan-catalog enum is:

```csharp
public enum RunningExperience
{
    New,
    Intermediate,
    Advanced,
    Experienced
}
```

The backend has no `RunningExperience` enum. Its corresponding onboarding
contract is:

```csharp
public enum RunningBackground
{
    Beginner,
    Intermediate,
    Advanced,
    Experienced
}
```

`PlanCatalogDomainMapper` explicitly says that only
`RunningBackground.Intermediate` has an explicit, tested mapping to catalog
`INTERMEDIATE`; Beginner, Advanced, and Experienced currently have no catalog
candidate mapping. Therefore doc13 `New` is an exact lexical match for
plan-catalog `RunningExperience.New`, but **not** for backend
`RunningBackground.Beginner`. `New -> Beginner` is a plausible proposed
mapping, not an existing approved 1:1 mapping. It is `DECISION_REQUIRED`
before a backend runway route consumes it. Advanced and Experienced also need
explicit mapping activation even though their member names match lexically.

| doc13 term | Plan-catalog | Backend | Result |
|---|---|---|---|
| New | `RunningExperience.New` | `RunningBackground.Beginner` | Plan-catalog exact; backend naming mismatch and mapping decision required |
| Intermediate | `RunningExperience.Intermediate` | `RunningBackground.Intermediate` | Exact names; only pairing already explicitly mapped/tested for the live catalog pilot |
| Advanced | `RunningExperience.Advanced` | `RunningBackground.Advanced` | Exact names, but no active catalog candidate mapping |
| Experienced | `RunningExperience.Experienced` | `RunningBackground.Experienced` | Exact names, but no active catalog candidate mapping |

### 2.2 Race-core phases and runway blocks

`TEN_K_MASTER v6` defines the race-core vocabulary as the catalog keys
`FOUNDATION`, `BUILD`, `RACE_SPECIFIC`, and `TAPER`. Backend typed allocation
records carry these in `PhaseKey` strings; repository search found no existing
`RaceCorePhase` enum. The semantic family exists, but the assertion that a
race-core enum already exists is not supported by current source.

Runway names are a separate family: `CONSISTENCY`, `GENERAL_ENDURANCE`,
`AEROBIC_STRENGTH`, and `PRE_SPECIFIC_TRANSITION`. They occur before the core
and must never be passed to the existing `STANDARD_CORE` allocator as though
they were template phase keys.

| doc13 term | Existing repository term | Reconciliation |
|---|---|---|
| CONSISTENCY | none | new runway-block type member required |
| GENERAL_ENDURANCE | none | new runway-block type member required |
| AEROBIC_STRENGTH | none | new runway-block type member required |
| PRE_SPECIFIC_TRANSITION | none | new runway-block type member required |
| AEROBIC_STRENGTH_LIGHT | none | prescription-profile ambiguity; see §2.3 |
| CORE | `FOUNDATION -> BUILD -> RACE_SPECIFIC -> TAPER` | composition boundary, not a runway block |

Recommended distinct names for a future typed pass are:

- `RaceCorePhaseKey` for a future closed typed representation of the existing
  catalog phase-key family (or retain the current `PhaseKey` string without
  pretending an enum already exists).
- `PreparationRunwayBlockType` for the new runway taxonomy.

No common `Phase` base type should be introduced. A runway allocation must
accept only `PreparationRunwayBlockType`; existing core allocation continues
to accept catalog phase allocations/keys. This makes accidental iteration
over both families impossible without an explicit composition adapter.

### 2.3 `AEROBIC_STRENGTH_LIGHT`

The more plausible reading is interpretation (a): it is
`AEROBIC_STRENGTH` block identity with a lighter prescription profile. The
term appears in routing but has no independent §9.1 min/preferred/max row,
whereas every taxonomically independent reusable block is expected to have
one. The `_LIGHT` suffix also describes content intensity rather than a new
temporal purpose.

This remains **DECISION_REQUIRED**, because the locally available canonical
artifact does not contain §9 and the supplied text does not explicitly say
whether omission from §9.1 was intentional. Phase 4G.4B must not silently add
an `AerobicStrengthLight` block member or fabricate duration bounds. The
contract sketch therefore represents it as:

```csharp
internal enum PreparationRunwayBlockType
{
    Consistency,
    GeneralEndurance,
    AerobicStrength,
    PreSpecificTransition
}

internal enum PreparationRunwayPrescriptionProfile
{
    Standard,
    Light
}
```

`Light` is valid only with `AerobicStrength` if interpretation (a) is later
approved. It does not authorize any prescription algorithm in this phase.

## 3. Core-length determinism

`plan-catalog/catalog/templates/ten-k-master.v6.json` was rechecked:

- core minimum: 8 weeks;
- core default/preferred duration: 12 weeks;
- core maximum: 14 weeks;
- phase preferred sum: `3 + 4 + 4 + 1 = 12`.

For a duration beyond the 14-week maximum, runway determination fixes
`coreWeeks` at the catalog preferred/default 12 weeks. It never chooses a
contextual value from 12–14. This is separate from 13–14-week in-core
extension: those targets remain within `STANDARD_CORE` and are governed by
the existing generic allocator and `TD-ALLOCATION-PRIORITY-001`. Runway
composition begins only in the current 15+ week
`PLAN_HORIZON_COMPOSITION_REQUIRED` territory.

## 4. Day-based date arithmetic

The required inclusive model is adopted unchanged:

```csharp
var coreDays = preferredCoreWeeks * 7;
var coreStartDate = raceDate.AddDays(-(coreDays - 1));
var runwayDays = Math.Max(0, coreStartDate.DayNumber - startDate.DayNumber);
var fullRunwayWeeks = runwayDays / 7;
var leadingPartialDays = runwayDays % 7;
```

`RaceDate` is the last core day. For a 12-week core, it is day 84 of the
inclusive core span, so `coreStartDate = raceDate - 83 days`. Naively using
`(raceDate - startDate)` as a week count would shift the boundary by one day.

Calendar order is:

```text
StartDate
  -> optional 0–6-day leading partial runway span
  -> fullRunwayWeeks complete runway weeks
  -> 12-week canonical core
  -> RaceDate (last core day)
```

### 4.1 Contract sketches for Phase 4G.4B

```csharp
internal sealed record PreparationRunwayBlockAllocation(
    PreparationRunwayBlockType BlockType,
    PreparationRunwayPrescriptionProfile PrescriptionProfile,
    int FullWeekCount);

internal sealed record PreparationRunwayAllocation(
    int FullWeekCount,
    int LeadingPartialDayCount,
    IReadOnlyList<PreparationRunwayBlockAllocation> Blocks);

internal sealed record PreparationRunwayCalendarSpan(
    DateOnly StartDate,
    DateOnly EndDate,
    PreparationRunwayBlockType BlockType,
    PreparationRunwayPrescriptionProfile PrescriptionProfile,
    bool IsPartial,
    int CalendarDayCount);
```

These are document sketches only. Required validation invariants are:

1. `FullWeekCount >= 0`, `LeadingPartialDayCount` is 0–6, and the block
   full-week sum equals `FullWeekCount` whenever allocation succeeds.
2. Block allocation reads only `fullRunwayWeeks`, never partial days.
3. A leading partial span inherits the first allocated block's semantic
   identity/profile but contributes zero to that block's min/preferred/max
   week accounting.
4. The partial span is additional. Its existence can never reduce the first
   block's full-week allocation; e.g. three General Endurance partial days
   cannot justify allocating one fewer full General Endurance week.
5. Partial sessions are exactly `PreferredDays` intersected with the partial
   date interval. Zero sessions is valid.
6. A partial span permits no threshold, interval, goal-pace, aggressive
   long-run, or new intensity progression content.
7. `IsPartial` belongs only to dated materialization spans. There is no
   `PARTIAL_BLOCK` taxonomy member.
8. `CalendarDayCount` equals the inclusive span length and is 1–6 when
   `IsPartial`; complete spans contain seven days.

## 5. Block-count and route selection

The supplied doc13 §9.2 routing is recorded as a product import candidate:

| Full runway weeks | Imported selection rule |
|---:|---|
| 0–3 | one block: Pre-Specific Transition |
| 4–6 | one primary block plus transition |
| 7–10 | two blocks plus transition |
| 11+ | three blocks, each capped at its own maximum |

The supplied routes are:

```text
New:          CONSISTENCY -> GENERAL_ENDURANCE -> CORE
New (long):   CONSISTENCY -> GENERAL_ENDURANCE -> AEROBIC_STRENGTH[Light] -> CORE
Intermediate: GENERAL_ENDURANCE -> AEROBIC_STRENGTH -> PRE_SPECIFIC_TRANSITION -> CORE
Advanced/
Experienced:  AEROBIC_STRENGTH -> PRE_SPECIFIC_TRANSITION -> CORE
```

Experience is the only personalization input. This selection must not read
`CORE_ENTRY_READINESS_IN`, `RecentWeeklyVolumeKm`, `RecentLongestRunKm`, or
`RecentRunsPerWeek`. Content personalization from those signals is deferred
Option C, not part of this model.

### 5.1 Unresolved route/count tensions

The imported statements are not yet a complete allocation algorithm:

- The `<=3` rule always selects Pre-Specific Transition, even though the
  displayed New routes do not contain that block.
- “One primary” and “two blocks” do not identify which prefix/suffix members
  are retained for every experience route.
- The `11+` rule requires three blocks, but the Advanced/Experienced route
  lists only two. A third block may not be invented in this audit.
- `AEROBIC_STRENGTH_LIGHT` has no independent maximum unless the profile
  interpretation in §2.3 is approved, in which case it inherits the
  Aerobic Strength block's duration bounds.

These are blocking decisions for a deterministic allocator, though they do
not prevent Phase 4G.4B from defining neutral typed contracts and typed
failure outcomes.

### 5.2 Maximum-exhaustion arithmetic

Supplied §9.1 maxima are Consistency 8, General Endurance 8, Aerobic
Strength 8, and Pre-Specific Transition 5 weeks.

- New-long under interpretation (a): `8 + 8 + 8 = 24` full runway weeks.
- Intermediate: `8 + 8 + 5 = 21` full runway weeks.
- Advanced/Experienced as actually listed: `8 + 5 = 13` full runway weeks;
  the required third block is unspecified.

Exhaustion is realistically reachable because start date has no documented
upper lead-time bound. With a 12-week core, 25 runway weeks requires 37 total
weeks to race and exceeds New-long capacity; 22 runway weeks requires 34
total weeks and exceeds Intermediate capacity; 14 runway weeks requires 26
total weeks and exceeds the listed Advanced/Experienced capacity. A one-year
lead time would leave roughly 40 runway weeks after reserving the core,
exceeding every route. The future allocator therefore needs an explicit typed
unallocated-capacity result until product decides whether to reject, cap,
repeat, or introduce another concept. This audit chooses none of them.

## 6. Evidence and governance classification

The taxonomy is evidence basis × decision status. `EVIDENCE_INFORMED` means
general evidence constrains the direction; it does not make exact routing
numbers scientifically proven.

| Rule | Evidence basis | Decision status | Rationale |
|---|---|---|---|
| Preferred core stays fixed at catalog preferred duration for runway determination | `PRODUCT_PRACTICE_INFORMED` | `EXPLICIT_PRODUCT_DEFAULT` | Deterministic composition policy; current catalog independently supplies 12 |
| Inclusive day-based arithmetic, not week-rounded subtraction | `TECHNICAL_CORRECTNESS` | `EXPLICIT_TECHNICAL_INVARIANT` | Date-boundary correctness, not training science |
| Full-runway thresholds `<=3`, `4–6`, `7–10`, `11+` | `PRODUCT_PRACTICE_INFORMED` | `IMPORT_CANDIDATE` | Exact bands are prompt/doc-supplied defaults, not evidence-backed |
| Experience-based three-route selection | `EVIDENCE_INFORMED` | `IMPORT_CANDIDATE_PRODUCT_DEFAULT` | General novice restraint/general-to-specific evidence informs direction; exact routes remain product defaults |
| `AEROBIC_STRENGTH_LIGHT` as Aerobic Strength + Light profile | `SOURCE_STRUCTURE_INFORMED` | `DECISION_REQUIRED` | Missing §9.1 row supports but does not prove profile interpretation |
| Partial-span low-intensity restraint | `EVIDENCE_INFORMED` | `EXPLICIT_PRODUCT_DEFAULT` | Reuses prior low-intensity-dominance/novice-restraint basis; exact prohibition is a safety/product rule |
| Partial span excluded from block week totals | `TECHNICAL_CORRECTNESS` | `EXPLICIT_TECHNICAL_INVARIANT` | Prevents unit/category mixing and substitutive under-allocation |
| Per-block maximum enforcement | `PRODUCT_PRACTICE_INFORMED` | `IMPORT_CANDIDATE` | Exact maxima are source-authored product values |

Prior research is not overstated: novice injury-risk and low-intensity
dominance findings support conservative direction; the cited RCT's negative
finding that longer preconditioning did not reduce injury does not establish
the exact thresholds, routes, or durations above.

## 7. Relationship to open TDs

### 7.1 `TD-ALLOCATION-PRIORITY-001`

Entirely separate. It governs priority-dependent allocation within the
8–14-week race core, particularly 13–14-week extension. Runway composition
reserves the preferred 12-week core and operates only on pre-core time in
15+ week territory. Runway blocks must not be fed into the core priority
allocator.

### 7.2 `TD-FOUNDATION-COMPRESSION-001`

This TD concerns a future readiness decision for compressing Foundation to
one week. Runway personalization is also expected eventually to consider
`CORE_ENTRY_READINESS_IN`, but that shared future signal does not merge the
two scopes. Their relationship is a noted future dependency only; neither is
resolved here.

### 7.3 Proposed future risk — not created

Proposed ID: `TD-RUNWAY-PERSONALIZATION-001`.

> Preparation-runway block content is currently selected only by runway
> duration and the explicitly mapped four-level experience value. No approved
> rule defines how `CORE_ENTRY_READINESS_IN`, recent weekly volume, recent
> longest run, or recent runs per week should alter block content, intensity,
> progression, or eligibility. Before any runway planner consumes those
> signals, product and coaching owners must define signal precedence,
> thresholds, missing-data behavior, ownership, and typed fail-closed outcomes
> without duplicating resolver evaluation or changing race-core allocation.

No risk inventory was modified.

## 8. Decisions and work deferred after this audit

1. Approve or reject `RunningExperience.New -> RunningBackground.Beginner`
   and activate explicit mappings for the non-Intermediate levels.
2. Decide whether `AEROBIC_STRENGTH_LIGHT` is a prescription profile or a
   distinct block with independently authored duration bounds.
3. Resolve the route/count tensions in §5.1, especially the missing third
   Advanced/Experienced long-route block.
4. Define a typed outcome for runway weeks beyond simultaneous block maxima;
   do not silently discard weeks.
5. Decide exact distribution within the imported count bands where the
   supplied rules name a count but not which route members or week totals.
6. Add the proposed personalization TD only in a separately reviewed
   documentation pass.
7. Defer all readiness/volume/longest-run/frequency personalization, exact
   prescriptions, partial-span materialization, and live activation.

## 9. Phase 4G.4B readiness

Phase 4G.4B may proceed with **neutral typed contracts** for runway duration,
block identity, profile identity, allocations, calendar spans, and explicit
failure/decision-required outcomes. The day arithmetic, family separation,
partial-span invariants, fixed 12-week core rule, and non-overlap with core
allocation are sufficiently reconciled.

Phase 4G.4B must not implement a successful end-to-end allocator for every
route until the New/Beginner mapping, `AEROBIC_STRENGTH_LIGHT` status,
route/count contradictions, and maximum-exhaustion outcome are decided. It
must not start personalization or materialization work.
