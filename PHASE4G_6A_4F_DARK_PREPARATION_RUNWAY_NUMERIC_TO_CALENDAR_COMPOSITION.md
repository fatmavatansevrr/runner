# Phase 4G.6A.4F — Dark Preparation Runway Numeric-to-Calendar Composition

## 1. Executive result

`TEN_K_PREPARATION_RUNWAY_NUMERIC_WEEKS_DATED_AND_COMPOSED_WITH_CORE_DARK_WITH_EXACT_SEGMENT_CONTINUITY`

The Application assembly now owns an internal calendar composer that takes an already continuity-validated numeric runway, the canonical horizon decision, and an existing undated 12-week Core skeleton. It dates the continuous runway-plus-Core structural sequence through the unmodified Phase 4F.5 calendar materializer, restores runway/Core local views, validates the combined sequence through the existing dated-skeleton validator, and emits internal segment/global provenance. It is not registered or publicly/persistently reachable.

## 2. Inherited state

Phases 4G.6A.1–4E supply the authoritative horizon/date decision, two profiles, block allocations, catalog-backed workout anchors, undated four-slot weeks, exact weekly/slot/long-run distances and final-runway-to-Core-Week-1 numeric equality. None of those policies or outputs was changed.

## 3. Pilot scope

Executable proof is restricted to Race, exact 10 km, `TEN_K`, four days, Intermediate, `TEN_K__4D__INTERMEDIATE v10`, 12 Core weeks, 3–8 full runway weeks and the `CONSISTENCY_NEEDED`/`CORE_ENTRY_READY` profiles. Generic wrappers remain reusable, but the composition policy factory rejects another candidate/version.

## 4. Artifacts inspected

Inspection covered `CoreHorizonClassifier`, `CoreHorizonDecision`, `PreparationRunwayDateAuthority`, runway date contracts/validators and Phase 4G.6A.1; `GeneratedCatalogPlanSkeleton`, Phase 4F.5 assignment contracts/materializer/exceptions, `DatedGeneratedCatalogPlanSkeletonValidator`, dynamic Core calendar orchestration and their tests; runway allocator/binder/4D materializer/4E numeric materializer and tests; Core Foundation Week 1 and numbering; preview routing, confirmation/persistability, persistence entities, public DTOs and containment tests.

## 5. Authoritative date source

`PreparationRunwayCalendarAuthorityAdapter` carries one `CoreHorizonContext`, its value-equal canonical `CoreHorizonDecision`, and `PreparationRunwayDateAuthority.Derive` output. The composer recomputes only those existing authorities for consistency validation. It never subtracts race/start dates to derive weeks and never accepts duplicate `RunwayWeeks`, `CoreStartDate`, `AvailableFullWeeks` or `LeadingPartialDays` fields.

## 6. LeadingPartialDays decision

Phase 4G.6A.1 explicitly places the 0–6 remainder days before the first full runway week. They are alignment metadata only: no week, session or allocation unit is created. Therefore `RunwayStartDate = StartDate + LeadingPartialDays`; no workout may occur in the preceding alignment span.

## 7. Calendar input contract

`PreparationRunwayCalendarCompositionRequest<TKey>` contains the typed date authority, PreferredDays, nullable LongRunDayPreference, profile/candidate identity, successful 4E numeric result, authoritative Core Week 1 numeric target, existing undated Core skeleton and versioned calendar-composition policy. It contains no pace, public DTO or persistence contract.

## 8. Segment-boundary derivation

For `N` runway weeks: alignment span is `[StartDate, RunwayStartDate)`; runway windows begin at `RunwayStartDate` and occupy exactly `N×7` days; `CoreStartDate = RunwayStartDate + N×7`; the 12-week Core occupies 84 days; `RaceDate = CoreStartDate + 84 days`. Week ends are start plus six days. No Monday convention or inclusive reverse calculation is used.

## 9. Runway week dating

`PreparationRunwayCalendarSkeletonAdapter` converts the already-prescribed structural slots into the existing `GeneratedCatalogPlanSkeleton` shape without changing workouts or quantities. The existing calendar materializer assigns dates. The resulting dated runway wrapper rejoins each calendar slot to its original prescribed slot by stable slot ordinal.

## 10. Core week dating

The existing Core skeleton is appended, with only a composition-level global week offset, to the runway structural skeleton. The combined structure is passed once through `CatalogWeekSkeletonCalendarMaterializer`. This is required because the existing two-day KEY/LONG separation policy is cross-week and must see the runway/Core boundary. Core segment-local numbering is restored to 1–12 in the result. Standalone Core dating code and its 8–14 callers are untouched.

## 11. PreferredDays behavior

Exactly four distinct weekdays are required. Input collection order does not select dates: the existing materializer maps each preferred weekday into each seven-day window and applies its deterministic ranking/backtracking policy. Fewer, more or duplicate days fail atomically.

## 12. LongRunDayPreference behavior

Race composition requires a non-null long-run day contained in PreferredDays. The existing materializer fixes every runway and Core `LONG_RUN` to that weekday. Missing or non-preferred values return the typed `LONG_RUN_DAY_NOT_PREFERRED` equivalent.

## 13. Role-to-day mapping

The existing Phase 4F.5 algorithm remains authoritative: LONG_RUN uses its preference; KEY_SESSION uses a date at least two days from long run, including existing cross-week checks; EASY_SUPPORT slots receive remaining dates in chronological order matched to structural slot order. No new separation constant exists.

## 14. Runway/Core composition

The structural runway and Core are combined before dating so the existing bounded backtracking search can validate the segment boundary. The output is then split into typed runway and Core views plus one ordered combined view. There is no overlap and no unexplained gap; the only non-training span is the explicitly approved leading alignment remainder.

## 15. Global and segment-local numbering

Runway global/local weeks are `1..N`. Core global weeks are `N+1..N+12`; Core local weeks remain `1..12`. The wrapper avoids changing standalone Core contracts. Combined numbers must equal the contiguous range `1..N+12`.

## 16. Segment provenance

Two internal `PreparationRunwaySegmentProvenance` records identify `PREPARATION_RUNWAY` ordinal 1 and `RACE_CORE` ordinal 2, with segment start/end, local week range, policy key/version, candidate identity and runway profile where applicable. Each composed week carries global/local numbers and its segment; runway weeks retain block, progression, anchor/support and numeric provenance unchanged.

## 17. Numeric-prescription preservation

Calendar composition never calculates distance. Before dating it verifies each weekly total equals its four prescribed slots, the long-run slot equals the weekly long-run value, and the final runway quantities still equal the authoritative Core Week 1 target. Dated slots reference the original `PreparationRunwayPrescribedSlot`; tests prove value equality for every slot/week.

## 18. Cross-boundary continuity

The final runway week must be `PreSpecificTransition`; the next local Core week must be `FOUNDATION`. Final runway end plus one day equals Core start. The combined existing validator proves no duplicate/out-of-window date and applies the existing same-week and cross-week KEY/LONG separation across the boundary. Numeric continuity remains the exact 4E result. Runway contains only its pre-Core workout artifacts; no race-specific runway content is introduced.

## 19. Remainder 0..6 proof

All seven remainder values pass. Each preserves the same full-week count, creates no partial week/session, starts the runway exactly `remainder` days after StartDate, preserves RaceDate as the exclusive day after the Core’s final window, and assigns no workout inside the alignment span. Mid-week starts and month/year crossings pass independently.

## 20. Invalid-calendar behavior

Focused fixtures cover fewer/more/duplicate PreferredDays, non-preferred long-run day, unsafe separation output, binding/slot count mismatch, runway count mismatch, wrong Core start, duplicate/out-of-window dates, overlap, unexplained gap, missing Transition, numeric mutation, wrong remainder, invalid global numbering, wrong Core count and non-Foundation Core Week 1. Every failure returns no partial runway, Core, combined schedule or continuity result.

## 21. Typed failures

The result distinguishes invalid request/date authority, preferred-day and long-run-day failures, runway/Core count and window failures, Core-start mismatch, duplicate/out-of-window dates, role assignment, overlap/gap, remainder mismatch, segment/global order, numeric mutation, continuity failure and invariant failure. No HTTP mapping was added.

## 22. Deterministic trace

Trace records source authority/version, available full weeks, alignment start/remainder, runway/Core/race boundaries, normalized preferred-day set, long-run day, each global/local week window, every runway slot date, segment identity and final continuity result. Repeated calls are value-identical; reordering PreferredDays leaves assigned dates unchanged.

## 23. Production/dark/public/persistence classification

`PRODUCTION_OWNED`; `DARK_UNWIRED`; `PUBLICLY_UNREACHABLE`; `PERSISTENCE_UNREACHABLE`. Source scans find no call in API, Persistence or PreviewRouting. There is no DI registration.

## 24. Existing 8–14 regression result

No horizon gate, classifier, standalone Core orchestrator, calendar materializer, validator, preview route or confirmation path was modified. Existing 8–14 regression tests and the full backend suite are the acceptance boundary; this new composer has no live caller.

## 25. Deferred pace work

No pace, target-time conversion, effort speed, duration or interval-segment prescription is present. Workout and numeric prescriptions are passed through unchanged.

## 26. Deferred orchestration activation

No public 15–20 orchestration, preview response, confirmation, persistability or entity mapping was added. The combined result is an internal dark artifact only.

## 27. Explicit non-implementation statement

This phase changed no allocator, profile/eligibility rule, binder, 4D structural materializer, 4E numeric materializer, workout/progression/schema/candidate/master/bundle, pace logic, public DTO/HTTP behavior, persistence/migration, horizon gate, 21–52 General Endurance staging, 53+ behavior or intermediate-distance behavior.

## 28. Exact next phase

Recommended next phase: **Phase 4G.6A.4G — Dark Preparation Runway Pace Eligibility and Prescription Contract**, consuming the dated 4F result without public orchestration or persistence activation.

Phase result: `TEN_K_PREPARATION_RUNWAY_NUMERIC_WEEKS_DATED_AND_COMPOSED_WITH_CORE_DARK_WITH_EXACT_SEGMENT_CONTINUITY`.
