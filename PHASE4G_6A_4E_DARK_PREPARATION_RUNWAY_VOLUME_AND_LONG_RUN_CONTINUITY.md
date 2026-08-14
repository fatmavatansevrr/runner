# Phase 4G.6A.4E — Dark Preparation Runway Weekly Volume and Long-Run Continuity

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_WEEKLY_VOLUME_AND_LONG_RUN_CONTINUITY_POLICY_IMPLEMENTED_DARK_WITH_VALIDATED_CORE_ENTRY_TRANSITION`

The Application assembly now owns a pure numeric materializer for already-materialized, undated Preparation Runway weeks. It assigns weekly distance, long-run distance and exact four-slot distances; emits per-week provenance; and requires the terminal runway week to reproduce the authoritative Core Week 1 numeric boundary exactly. It is internal, unregistered, publicly unreachable and persistence-unreachable.

## 2. Inherited state

Phases 4G.6A.1–4D closed horizon/date authority, the two pilot profiles, generic block allocation, all four catalog progressions, exact-prefix binding, and undated four-slot structural week materialization. This pass leaves all of those decisions and artifacts unchanged.

## 3. Pilot scope

Executable policy is restricted to Race, exact 10.0 km, `TEN_K`, four running days, Intermediate, `TEN_K__4D__INTERMEDIATE v10`, 12-week PreferredCore, 3–8 full runway weeks, and `CONSISTENCY_NEEDED`/`CORE_ENTRY_READY`. Contracts are structurally generic; the factory is deliberately candidate-specific.

## 4. Artifacts inspected

Inspection covered the 4G.6A.1–4D decisions; runway contracts, allocator, policy factory, progression reader/binder, workout validator and week materializer; their tests; `TEN_K_MASTER v6`, `TEN_K__4D__INTERMEDIATE v10`, `TEN_K_WORKOUT_PROGRESSION_V1 v5`, `RUN_LAYOUT_4D v2`, runway progressions and workout definitions; `CatalogPrescriptionContextBuilder`, normalized readiness and `CoreEntryReadinessResolver`; `CatalogVolumeAndLongRunPlanner`, `VolumeSafetyPolicy`, `V1MissingReadinessStartingVolumePolicy`, `V1FourDaySessionVolumeAllocationPolicy`, Core volume/long-run/session contracts and validators; peak-volume and canonical decision/evidence/risk records.

## 5. Numeric evidence classification

| Value/rule | Classification | Repository authority |
|---|---|---|
| 7% preferred increase, 8% hard increase, 2.5 km absolute increment cap | `DIRECT_CANONICAL_RULE` | `VolumeSafetyPolicy.Default`; canonical progression-rule record |
| 0.5 km rounding | `REUSED_CORE_BEHAVIOR` | Core volume and four-day session policies |
| 30–36% preferred long-run band, 33% selected share, 40% hard cap | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | Phase 4F.7B.1 product-practice correction; not Doc13/golden-fixture attribution |
| Missing weekly default 16 km | `PRODUCT_DEFAULT` | `V1_MISSING_READINESS_STARTING_VOLUME_POLICY v1` |
| NoRecentRunningBase default 12 km | `PRODUCT_DEFAULT` | same policy’s explicit-zero/no-recent-running rule |
| linear start-to-target curve | `REUSED_CORE_BEHAVIOR` | `CatalogVolumeAndLongRunPlanner` non-taper interpolation |
| four-slot residual allocation and 0.001 km comparison tolerance | `REUSED_CORE_BEHAVIOR` | `V1_FOUR_DAY_SESSION_VOLUME_ALLOCATION_POLICY v1` |
| runtime readiness values and Core Week 1 boundary | runtime-derived evidence/current deterministic behavior | typed request evidence and existing Core pipeline |

No provisional coefficient was introduced. Product defaults are not represented as scientific constants.

## 6. Authoritative starting-load inputs

| Input | Numeric use | Eligibility use | Missing behavior |
|---|---|---|---|
| Four-week recent weekly average | starting weekly anchor when positive and Provided | already contributes to readiness/profile selection outside this engine | 16 km recorded default; never zero |
| 30-day recent longest run | upper compatibility input for starting long run, never copied blindly | already contributes to readiness outside this engine | weekly-derived 33% selection |
| Recent runs/week | none | metadata/profile selection only | no numeric substitution |
| Typed evidence state | selects Provided/Missing/NoRecentRunningBase branch | preserves semantic distinction | fail on value/state contradiction |
| CoreEntryReadiness/profile | selects already-resolved runway profile | yes | not recomputed here |
| RunningBackground | candidate eligibility only | yes | never downgraded |
| target finish time/old race | none | none in this policy | ignored |

## 7. Missing/NoRecentRunningBase behavior

Missing weekly evidence selects 16 km; NoRecentRunningBase selects 12 km. Missing longest-run evidence derives the start at 33% of starting weekly volume, bounded to the 30–36% band and 40% hard cap. No state is silently converted to zero, and Intermediate remains Intermediate. Provided-longest-only is supported using the 16 km weekly default plus the bounded longest-run input.

## 8. Core Week 1 numeric target

Core Week 1 is not a fixed catalog number. `PreparationRunwayCoreWeekOneTargetAdapter` reads the first `CatalogWeeklyVolumeWeek` and `CatalogLongRunWeek` produced by the current Core planner, then applies the exact shared four-day allocation policy. For the repository’s current pilot readiness fixture (24 km recent weekly, 9 km recent longest), Foundation Week 1 is:

| Role | Workout binding | Distance |
|---|---|---:|
| KEY_SESSION | `FOUNDATION_EASY_BASE` → `EASY_STANDARD v4` | 8 km |
| EASY_SUPPORT 1 | fixed Core role default → `EASY_STANDARD v4` | 4 km |
| EASY_SUPPORT 2 | fixed Core role default → `EASY_STANDARD v4` | 4 km |
| LONG_RUN | fixed Core role default → `LONG_RUN_STANDARD v4` | 8 km |
| Total | derived | 24 km |

The 12-week current-pilot weekly totals are 24, 25.5, 27, 28, 29.5, 31, 32.5, 34, 35, 36.5, 38 and 20 km. Corresponding long runs are 8, 8.5, 9, 9, 9.5, 10, 10.5, 11, 11.5, 12, 12.5 and 6.5 km. These are runtime-derived for that readiness fixture, not fixed candidate fields. Missing and no-base inputs produce different deterministic Core Week 1 targets (16/5.5 km and 12/4 km respectively), which the runway request must carry from the same evidence evaluation.

## 9. Starting weekly-volume rule

Provided positive weekly evidence is rounded to the nearest 0.5 km and preserved as the anchor. Missing uses 16 km; NoRecentRunningBase uses 12 km. The materializer does not reverse-engineer a more aggressive start from runway length. A supplied Core boundary below the start fails closed because no approved non-taper runway reduction coefficient exists.

## 10. Starting long-run rule

Compute 33% of starting weekly distance, rounded to 0.5 km. When a positive recent longest run is Provided, use the smaller of that value and the selected 33% target, then clamp into the rounded 30–36% preferred band (also below the 40% hard cap). This exactly reuses Core Week 1 reconciliation: recent longest is a compatibility ceiling before the existing lower-band protection, not a literal prescription.

## 11. Consistency trajectory

Consistency follows the conservative bounded target curve. It may modestly increase or remain unchanged; it never forces an increment merely because a second Consistency week exists. Its numeric trace explicitly labels cadence maintenance versus bounded progression.

## 12. General Endurance trajectory

The direct 1–5-week General Endurance block follows the same bounded curve toward Core entry. Maintenance is valid at target. Every actual increment is checked against 8% hard and 2.5 km absolute caps; 7% remains the preferred policy value recorded in trace/policy. No recurring deload is invented and no 21–52-week mesocycle behavior is imported.

## 13. AerobicStrength trajectory

AerobicStrength does not receive a separate total-volume boost or interval prescription. It may inherit a modest bounded curve increment when the overall Core-entry curve still requires one, or maintain total/long-run volume. Workout Step 1/2 content remains owned by the existing progression artifacts.

## 14. Transition trajectory

The terminal `PRE_SPECIFIC_TRANSITION` week exactly repeats the already-reached Core Week 1 total, long run and role quantities. It is therefore a maintenance bridge, never a development spike and not called a deload. The prior development week must already equal the Core boundary.

## 15. Weekly progression constraints

The engine uses a hybrid: linear Core-style interpolation supplies the target curve; deterministic rounding is applied; then every actual transition is validated against 8% hard and 2.5 km absolute increase limits. Unchanged volume is valid. There is no approved general runway decrease rule; negative transitions fail. The preferred 7% value remains policy evidence, while 8% is the fail-closed maximum.

## 16. Long-run progression

The start and exact Core target are interpolated over the same development span, rounded to 0.5 km, checked non-decreasing, and checked inside 30–36% (and necessarily below 40%) every week. The penultimate week reaches the target; Transition holds it. Any incompatible share or required unsupported reduction fails atomically.

## 17. Slot distribution

Distance is the sole quantity mode. The shared Core allocator reserves the long run, allocates 50% of residual distance to KEY_SESSION subject to its 3 km minimum, splits the remaining residual across two EASY_SUPPORT slots subject to 1.5 km minima, and deterministically assigns any rounding residual to EASY_SUPPORT 2. This is reused behavior, not a new runway percentage. The long run normally ties or is largest; a key-session lead of one rounding unit can occur under the already-approved Core residual allocation and is the explicit existing-behavior exception.

## 18. Rounding policy

Every weekly, long-run and slot value is in kilometers and rounds to 0.5 km using `MidpointRounding.AwayFromZero`. The 0.001 km tolerance is the existing Core allocation comparison tolerance. Final Core values are copied exactly after curve validation, so rounding cannot drift the boundary.

## 19. Runway-final to Core Week 1 continuity

For both profiles and every length 3–8, successful prescriptions require zero difference (within 0.001 km arithmetic tolerance) for weekly total, long run, KEY_SESSION, both EASY_SUPPORT sessions, and session count. Workout-family transition remains non-race-specific: runway Transition uses Easy/Long Run v5; Core Foundation uses Easy/Long Run v4 under its unchanged live candidate binding. Numeric equality does not publish or merge the segments.

## 20. Infeasibility behavior

A three-week runway that would need a 20→24 km one-step build fails `WEEKLY_CHANGE_LIMIT_EXCEEDED` with no partial weeks. A starting value above a lower supplied Core target fails `RUNWAY_PROGRESSION_INFEASIBLE`, because inventing a runway reduction percentage is prohibited. State/value conflicts, unavailable/inconsistent Core targets, share failures, slot infeasibility, rounding failures and continuity failures each return typed failure and no partial prescription.

## 21. Typed contracts

Production contracts include `PreparationRunwayStartingLoadEvidence`, `PreparationRunwayNumericPolicy`, `PreparationRunwayCoreWeekOneNumericTarget`, `PreparationRunwayNumericMaterializationRequest`, `PreparationRunwayPrescribedWeek`, `PreparationRunwayPrescribedSlot`, `PreparationRunwayCoreContinuityAnalysis`, decision trace, result and typed failure codes. All are internal and undated.

## 22. Numeric provenance and trace

Each week records unrounded/rounded weekly and long-run values, prior value, absolute/relative change, share, block trajectory, applied constraint, rounding rule, evidence provenance, Core-target provenance, numeric policy key/version and shared slot-policy provenance. Each slot retains its complete 4D structural provenance and adds quantity/unit provenance. Trace is not exposed publicly.

## 23. Matrix proof

Focused tests execute both profiles at 3, 4, 5, 6, 7 and 8 weeks. The canonical 24/8 Core fixture proves maintenance and exact transition for all 12 combinations. Additional fixtures prove a feasible 20→24 bounded build over eight weeks; short-runway infeasibility; weekly-only; longest-only; Missing; NoRecentRunningBase; high longest-run bounding; above-target fail-closed behavior; rounding edges; input-derived block shapes; value-identical repeated calls; exact slot sums; share bounds; and complete failure atomicity. No route table exists.

## 24. Production/dark/public/persistence classification

`PRODUCTION_OWNED`; `DARK_UNWIRED`; `PUBLICLY_UNREACHABLE`; `PERSISTENCE_UNREACHABLE`; `CALENDAR_UNASSIGNED`. Structural tests scan API, Persistence and PreviewRouting and find no materializer call site.

## 25. Deferred pace work

No pace, target-time, effort-speed conversion, interval segment quantity or duration prescription was added. Pace remains a later composition concern.

## 26. Deferred calendar work

No `DateOnly`, start/race date, partial-day handling, preferred-day selection, long-run-day preference or calendar placement appears in the numeric contracts/engine.

## 27. Deferred orchestrator activation

There is no DI registration or orchestrator caller. Public 15–20-week composition remains unchanged and unactivated. The current materializer must later be composed only after an upstream service produces the authoritative Core Week 1 target from the same readiness evidence.

## 28. Explicit non-implementation statement

This phase changed no runway allocation/profile rule, binder, structural role mapping, workout/progression/schema/candidate/master/bundle artifact, pace/calendar logic, preview/confirmation DTO, persistence/migration, public endpoint, 15–20 activation, 21–52 staged General Endurance work, 53+ behavior or intermediate-distance behavior. The live four-day allocation algorithm was mechanically extracted into a shared distance-only core and its existing suite proves value behavior remains unchanged.

## 29. Exact next phase

Recommended next phase: **Phase 4G.6A.4F — Dark Preparation Runway Numeric-to-Calendar Composition Contract**, consuming the already-undated 4D structure and 4E numeric result while preserving the existing horizon/date authority; it must remain dark and must not activate 15–20-week public behavior.

Phase result: `TEN_K_PREPARATION_RUNWAY_WEEKLY_VOLUME_AND_LONG_RUN_CONTINUITY_POLICY_IMPLEMENTED_DARK_WITH_VALIDATED_CORE_ENTRY_TRANSITION`.
