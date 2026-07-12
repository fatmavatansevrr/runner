# Backend Integration Phase 4D.2 — PACE_SOURCE_IN Resolver Implementation

Implements the second concrete runtime-condition resolver: `PaceSourceResolver`, producing `PACE_SOURCE_IN`
only. No other condition type's resolver logic was implemented. No pace projection, Riegel conversion, or
race-time equivalence was computed. Not wired into `IPlanGenerationEngine`/`PlanServices`/
`PlaceholderPlanGenerationEngine`.

## Output priority

1. **`RECENT_RACE`** — only when `RecentRaceDistanceKm`, `RecentRaceFinishTimeSeconds`, and
   `RecentRaceDate` are **all three** present. Outranks `TARGET_TIME` when both are usable (test:
   `Resolve_CompleteRecentRaceEvidence_EvenWithTargetTimePresent_StillPrefersRecentRace`).
2. **`TARGET_TIME`** — when recent-race evidence is incomplete but `TargetFinishTimeSeconds` is present
   and positive.
3. **`ESTIMATED`** — never emitted in V1 (see below).
4. **`NONE`** — when nothing usable exists at all.

## Why `NONE` is `Evaluated`, not `NotEvaluated`

`NONE` is a real, registry-valid `PACE_SOURCE_IN` value (confirmed directly against
`runtime-condition-values.v2.json`) — the registry already has a vocabulary word for "no pace evidence."
Returning `NotEvaluated` for this case would be a category error: `NotEvaluated` (Phase 4D.1.5) means the
resolver *could not decide*; here the resolver *did* decide — the decision is "there is no pace evidence,"
which is exactly what `NONE` communicates. A dedicated test
(`Resolve_None_IsEvaluatedStatus_NotNotEvaluated`) asserts this explicitly.

## When `RECENT_RACE` is emitted

All three Phase 4B fields present: `RecentRaceDistanceKm`, `RecentRaceFinishTimeSeconds`, `RecentRaceDate`.
Metadata always includes the three raw values (`recentRaceDistanceKm`, `recentRaceFinishTimeSeconds`,
`recentRaceDate`). Recency metadata (`raceResultAgeDays`, `paceRecencyConfidence`) is added only when
`RuntimeResolverContext.AsOfDate` is supplied (see "Recency metadata" below) — `RECENT_RACE` is still
emitted with none of that recency metadata if `AsOfDate` is absent; the resolver never withholds or
downgrades `RECENT_RACE` for missing recency context.

**Stale evidence is never rejected.** A recent race result 400+ days old still produces
`OutputValue = RECENT_RACE` (test: `Resolve_StaleRecentRace_StillOutputsRecentRace_NeverRejected`) —
only the `paceRecencyConfidence` metadata value changes (to `NOT_USABLE_AS_PACE_ANCHOR`). This matches the
task's explicit instruction: "Do not reject old recentRaceDate in this resolver unless product rules
already require it," and no such product rule was found in this repository.

## When `TARGET_TIME` is emitted

Recent-race evidence is missing or incomplete, and `TargetFinishTimeSeconds` is present and `> 0`. Metadata:
`targetFinishTimeSeconds`. The resolver never converts a target time into `RECENT_RACE` or uses it for
anything resembling goal feasibility — this phase implements no pace projection or feasibility logic of any
kind, only source-type classification.

**Why `> 0`, not just "not null," for `TargetFinishTimeSeconds`:** unlike the five Phase 4B fields (which
already have positivity validation in `PlanServices.GeneratePreviewAsync`), `TargetFinishTimeSeconds` is a
pre-existing field with **no** positivity check anywhere in the backend (confirmed by grep — it appears
only in assignment/mapping code, never in a validation branch). Since this resolver cannot assume upstream
validation caught a non-positive value for this specific field, it defensively treats `<= 0` as "not usable
evidence" (falls through toward `NONE`) rather than throwing — a `<= 0` target time reaching this resolver
is treated as absent data, not as invalid input requiring an exception, since no existing convention
establishes that as an error case for this particular field.

## `ESTIMATED` — explicitly deferred, not emitted in V1

**`ESTIMATED` is registry-valid but is never produced by this resolver.** Repository evidence was searched
for an approved method to derive a pace estimate from `RecentWeeklyVolumeKm`, `RecentLongestRunKm`, or
`RecentRunsPerWeek` alone — none was found. No formula, rule file, or golden-fixture step in
`plan-catalog/` or any Phase 4A document defines "estimate pace from recent training volume" as an approved
technique; the only pace-estimation technique evidenced anywhere in the repository is the Riegel
race-to-race projection (`RIEGEL_CONVERSION_5K_TO_10K` in the golden fixture), which requires an actual
race result — i.e., it's the `RECENT_RACE` path, not a distinct estimate-from-training-volume method. Per
the task's explicit instruction ("Potential inputs such as recentWeeklyVolumeKm... are not by themselves
approved pace-estimate methods unless repository docs explicitly say so"), `ESTIMATED` is left unimplemented.
A test (`Resolve_OnlyWeeklyVolumeLongestRunRunsPerWeek_NoRecentRaceNoTargetTime_ReturnsNone_NotEstimated`)
confirms a snapshot carrying only those three fields produces `NONE`, not `ESTIMATED`.

## Partial recent race behavior

Any one or two (but not all three) of `RecentRaceDistanceKm`/`RecentRaceFinishTimeSeconds`/`RecentRaceDate`
being present is **partial** evidence — never treated as `RECENT_RACE`. The resolver falls through to
`TARGET_TIME` (if available) or `NONE`, and adds a `Warnings` entry stating partial recent-race evidence was
ignored. No exception is thrown for partial evidence — recent race evidence is entirely optional, and a
client legitimately might submit only one field (e.g. a UI that lets a user enter distance before finish
time). Six tests cover every partial-field combination (distance-only, finish-time-only, date-only,
distance+time-without-date) crossed with both the `TARGET_TIME` and `NONE` fallback paths, plus a test
confirming no warning is emitted when zero recent-race fields are present at all (there's nothing "partial"
about complete absence).

## Recency metadata

**Reference date: `RuntimeResolverContext.AsOfDate`, not `StartDate`.** The task suggested preferring
`StartDate` as the reference date "if Phase 4 docs define evidence recency relative to plan start." Direct
inspection of the golden fixture contradicts that premise: `docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`'s
`INPUT_SNAPSHOT` step has **two distinct date fields** — `confirmDate: "2026-07-30"` and
`planStartDate: "2026-08-03"` — and the `PACE_CONVERSION` step's own `ageReferenceDate` fact is
`"2026-07-30"`, i.e. **`confirmDate`, not `planStartDate`**. This is direct, verifiable evidence that
evidence-age is computed relative to when the plan is generated/confirmed, not relative to the plan's
future start date. Since no equivalent "confirm date" field exists anywhere in the current backend request
pipeline, a new **internal-only** `RuntimeResolverContext.AsOfDate` field was added (not a new public API
field — nothing populates it from any request today, since no resolver is wired to live generation). If
`AsOfDate` is absent, the resolver does **not** fall back to `DateTime.UtcNow` or any other guessed date —
`raceResultAgeDays` is omitted entirely and `paceRecencyConfidence` is set to the literal
`"NOT_COMPUTED_NO_REFERENCE_DATE"`, per the task's explicit "do not invent it" instruction.

**Confidence ladder:** implemented using the exact 5-band Appsel V1 ladder already recorded as
owner-approved-for-metadata-use in `PHASE4A_3_OWNER_SCOPE_APPROVAL_FOR_RESOLVER_VOCABULARY.md` §C
("Recency confidence ladder is stored as trace metadata, not registry output"): `0–30→FULL`,
`31–60→HIGH`, `61–90→MODERATE`, `91–180→LOW_CONFIRMATION_NEEDED`, `>180→NOT_USABLE_AS_PACE_ANCHOR`. This is
a judgment call, stated explicitly: Phase 4A.2 had marked the *exact day boundaries* `DECISION_REQUIRED`
(only one weak corroborating data point existed — 49 days → `HIGH`, consistent with but not proof of the
31–60 boundary). Phase 4A.3's subsequent owner approval, however, explicitly named this exact ladder and
approved it for metadata use — read together, this is treated as sufficient authorization to implement the
ladder **as non-binding trace metadata only** (never as `OutputValue`, never gating any behavior). All 9
boundary values (0, 30, 31, 60, 61, 90, 91, 180, 181, 400 days) are covered by parameterized tests, verified
against the real reference-date-vs-race-date arithmetic.

`ConfidenceLabel` (the dedicated field on `RuntimeConditionResolutionResult`, distinct from `Metadata`) is
also set to the same value when computable, for symmetry with `Metadata["paceRecencyConfidence"]` — both
are `null`/absent when `AsOfDate` is not supplied.

**`AsOfDate` before `RecentRaceDate`** (a race dated in the future relative to the reference date) throws
`ArgumentException` — this is a logical impossibility, not a missing-evidence case, mirroring
`TimeAdequacyResolver`'s `raceDate < startDate` precedent.

## Validation layering

Unchanged: Phase 4B's five positivity checks in `PlanServices.GeneratePreviewAsync` remain the sole
defensive-validation layer for `RecentLongestRunKm`/`RecentWeeklyVolumeKm`/`RecentRunsPerWeek`/
`RecentRaceDistanceKm`/`RecentRaceFinishTimeSeconds`. `PaceSourceResolver` performs no redundant positivity
re-validation of these five fields — it assumes any present value already passed that check (test:
`Resolve_ResolverDoesNotThrowOnValidPositiveRecentRaceFields` confirms the resolver's own logic path adds
no extra validation). `TargetFinishTimeSeconds` is the one exception, handled as described above (treated
as absent if `<= 0`, not thrown on, since no existing convention validates it).

## Registry-simple / metadata-rich V1 scope

Unchanged, re-confirmed at the code level: `PaceSourceResolver.OutputValue` is always exactly one of
`NONE`/`RECENT_RACE`/`ESTIMATED`(never emitted)/`TARGET_TIME` — verified against the real registry file.
Recency confidence and the evidence hierarchy (certified race / time trial / structured test /
user-reported pace / effort-only — none of which this resolver can currently distinguish, since
`paceEvidenceType` remains withheld) live only in `Metadata`/`ConfidenceLabel`. Confidence-label strings
(`HIGH`, `MODERATE`, `FULL`, `LOW_CONFIRMATION_NEEDED`, `NOT_USABLE_AS_PACE_ANCHOR`) are confirmed, by
direct registry test, to be **invalid** `PACE_SOURCE_IN` values — they could never accidentally leak into
`OutputValue`.

## Confirmation: not wired to live generation

- `Program.cs` registers no `PaceSourceResolver` in DI.
- `PlanServices`/`PlaceholderPlanGenerationEngine` were not modified; reflection-based tests confirm
  neither constructor takes a `PaceSourceResolver` or `IPaceSourceResolver` parameter.
- Re-run, with all six Phase 4B fields plus `TargetFinishTimeSeconds` present in the request, of both the
  existing-supported-template preview flow and the `TEN_K`/`INTERMEDIATE`/4-day
  `PlanTemplateNotAvailableException` case — both behave exactly as before this phase.
- `GeneratePreviewResponse_HasNoPaceSourceResolutionProperty` structurally confirms no public response DTO
  exposes `RuntimeConditionResolutionResult` or `ResolverDecisionTrace`.

## Remaining work for Phase 4D.3

1. Implement `CORE_ENTRY_READINESS_IN` — still blocked on `TD-REGISTRY-001` (`STANDARD` fixture defect).
2. Implement `GOAL_FEASIBILITY_IN` — will need `RuntimeResolverContext.PriorResults` (e.g. this resolver's
   own `PACE_SOURCE_IN` output) once a pace-projection/feasibility method is separately approved; no such
   method is approved or implemented anywhere in this repository yet.
3. Decide the `paceEvidenceType`/evidence-hierarchy mapping (certified race / time trial / structured test
   / user-reported pace / effort-only → registry value) — still `DECISION_REQUIRED` per Phase 4A.2 §6; this
   phase's resolver has no way to distinguish evidence layers because the field remains withheld.
4. Decide whether an approved `ESTIMATED`-evidence method should ever be added, and if so, from what input
   — deliberately left unresolved here, not guessed.
5. `IRuntimeConditionResolutionService.ResolveAll` composing multiple resolvers (now two:
   `TimeAdequacyResolver`, `PaceSourceResolver`) into one `ResolverDecisionTrace` remains unimplemented.
6. Live generation wiring remains entirely out of scope.

## Confirmations

- No plan-catalog artifact was modified.
- No runtime registry value was changed.
- No golden fixture was changed.
- `TD-REGISTRY-001` remains `OPEN`.
- `EV-005` remains `PROPOSED`; `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`.
- No `TrainingWeek`/`TrainingDay` was generated from the catalog.
- The existing SQL `PlanTemplate` flow is unchanged; `TEN_K`/`INTERMEDIATE`/4-day still throws
  `PlanTemplateNotAvailableException`.
- No new public API field was added; `AsOfDate` is internal to `RuntimeResolverContext`, not populated from
  any request today.

**Final classification: `BACKEND_HAS_PACE_SOURCE_RESOLVER_NOT_WIRED_TO_GENERATION`.**
