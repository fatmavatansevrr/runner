# Phase 4K.8A — Preparation Runway Downward Consolidation and Bounded Rolling Materialization Authority Resolution

## 1. Executive result

Both prerequisite authorities are resolved without changing production behavior. Preparation Runway supports the existing upward or flat path only; an effective weekly or long-run start above the generated Core Week-1 target remains fail-closed. The existing complete 3–8-week prescription remains the single numeric authority, and future rolling activation must expose exact references from one immutable full prescription rather than restart interpolation.

PREPARATION_RUNWAY_DIRECTION_AND_BOUNDED_ROLLING_MATERIALIZATION_AUTHORITIES_APPROVED

## 2. Blockers discovered in Phase 4K.8

The current numeric materializer rejects `RunwayStartingWeeklyVolumeKm > CoreWeekOneTargetWeeklyVolumeKm` and rejects negative weekly change. It also accepts only a complete 3–8-week Runway with local numbering from 1 and a final `PreSpecificTransition`. Consequently neither assumed downward interpolation nor arbitrary slice input is an existing production authority.

## 3. Governance-versus-production conflict

Phase 4K.4 stated that the existing interpolation accepted any starting magnitude and governed upward and downward directions. Direct source and regression-test evidence contradicts that statement. The incorrect direction statement is superseded, append-only, by this decision and the `phase4K8ACorrection` on `TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001`.

RUNWAY_DOWNWARD_INTERPOLATION_PREVIOUSLY_ASSUMED_BUT_NOT_SUPPORTED_BY_CURRENT_PRODUCTION_AUTHORITY

## 4. Exact reopened claims

Only two claims were reopened: that existing Runway interpolation supports downward consolidation, and that the full-block materializer can directly consume bounded rolling slices without an additional exposure contract. No historical decision was silently rewritten.

## 5. Preserved prior decisions

The complete structural roadmap, four-week rolling activation, current validated evidence authority, unchanged Core generator, atomic Runway/Core-target resolution, target immutability, versioned future-only Core refresh, mixed-window all-or-nothing behavior, immutable activated/completed history, `NumericPending` future roadmap state, prohibition on onboarding fallback, planned GE-exit provenance-only status, safety priority, pace/calendar authority and existing JIT taxonomy remain valid.

## 6. Repository dependencies inspected

The review traced the numeric materializer and contracts, Calendar Composer, Pace Materializer, Core generator and target adapter, Runway validators and regression tests, stage/week semantics, interpolation and long-run allocation, the rolling checkpoint and initial runtimes, JIT decision/target lock/window/versioning/lifecycle contracts, Phases 4I.6A–4I.6B.1 and 4K.1–4K.7, the related TD records, and the Preparation Runway catalog progression semantics. Repository searches covered downward consolidation, de-load, regression, reduction, maintain, bounded slice, continuation, offset and cursor terminology.

## 7. Canonical Runway semantics

The supported numeric meaning is to build toward or maintain at Core entry while performing the catalog-authored workout-specific preparation. “Consolidation” does not independently authorize numeric decline. Flat movement is valid. Neither Taper nor GE recovery semantics may be reused as a multiweek Runway reduction rule.

## 8. Direction candidates

Option A, current fail-closed, has direct production and regression support and requires no formula change. Option B, holding above-target load flat, postpones rather than resolves Core incompatibility. Option C, controlled decline, lacks a canonical Runway reduction authority. Option D, regenerating Core from current evidence, already occurs at the approved generator boundary but must not become hidden target lifting. Option E correctly separates weekly and long-run analysis but does not legalize negative effective interpolation. Option F is unnecessary because upward/flat authority and unsupported-case behavior are now explicit. Option A with Option E’s analytical separation is approved.

## 9. Real direction matrix

Diagnostics use unchanged components for validated weekly loads 15, 18, 20, 24, 30 and 38 km; raw long runs below, equal to and above the preferred Core band; frequencies 2, 3, 4 and absent; both `CONSISTENCY_NEEDED` and `CORE_ENTRY_READY`; and product-average, recent-race and explicit-target-time pace sources. The unchanged Core generator preserves each weekly scalar, so the normal Phase 4K.5A mapping produces weekly equality. Long-run normalization can independently change direction. One observed 24 km / 6 km raw-long-run case rounds to a 7.0 km lower bound and fails the existing `LongRunShareViolation`; it is evidence of retained validation, not a reason to alter a formula.

## 10. Weekly-volume direction decision

Start below target uses existing capped upward interpolation only when all existing validators pass. Start equal target is flat. Start above target is unsupported and fails closed. Core generation normally preserves validated weekly input, but any reachable divergence still blocks rather than inventing a reduction or lifting the target.

## 11. Long-run direction decision

Raw long-run evidence is first normalized by the existing 0.30–0.36 preferred band, 0.40 hard cap, four-session feasibility and 0.5 km rounding. An effective start below target may use existing upward interpolation if every share, allocation, rounding and continuity validator passes; equality is flat. An effective start above target fails `LongRunContinuityViolation`. Weekly equality does not erase an independent long-run conflict, and the 0.85 recovery rule is not reused.

## 12. Final downward-consolidation authority

`PreparationRunwayDirectionPolicy` is governed as follows: weekly below/upward supported conditionally; weekly equal/flat supported conditionally; weekly above/downward unsupported. Effective long-run below/upward and equal/flat are supported conditionally; effective long-run above/downward is unsupported. There is no hidden target lift, onboarding fallback, planned GE-exit authority, Taper reuse or recovery reuse.

PREPARATION_RUNWAY_SUPPORTED_DIRECTION_RELATIONS_ARE_EXPLICIT_AND_UNSUPPORTED_DOWNWARD_CASES_FAIL_CLOSED_WITHOUT_NEW_FORMULA

## 13. Current validator behavior

Production rejects start-weekly above target before interpolation, rejects a negative weekly change, checks the complete duration and numbering, and preserves weekly-change, long-run-change, share, hard-cap, rounding, session allocation, continuity, pace and calendar validation. Supported direction is not unconditional feasibility. Future orchestration should map the incompatible transition to the existing typed `JIT_SEGMENT_TRANSITION_INFEASIBLE` reason.

## 14. Full Runway prescription authority

The unchanged materializer’s complete 3–8-week output is the sole numeric prescription authority. It is generated once at the Runway JIT boundary against the resolved start, full structural block and locked Core Week-1 target. A rolling view never becomes another numeric authority.

## 15. Bounded-materialization candidates

Option A computes one immutable full prescription and exposes bounded references. Option B carries continuation state. Option C computes a bounded result using global full-block coordinates. Option D restarts each slice. Option E remains blocked. All were evaluated for equivalence, formula impact, lock/version behavior, terminal-stage correctness, calendar continuity, atomicity, determinism and complexity.

## 16. Candidate comparison

Option A is exact by construction and uses the existing authority unchanged. Options B and C could be designed to be equivalent but add another calculation/continuation surface and proof burden. Option D changes coordinates, duplicates local Week 1 and can synthesize or omit terminal semantics; diagnostics also show a restarted middle slice can fail where the full prescription succeeds. Option E is unnecessary because Option A is supportable.

## 17. Selected bounded strategy

Option A is approved: generate the full block once, bind it to one immutable prescription/context version and expose only requested 1–4-week references. The full internal output is not full activation. Phase 4K.8B must implement the narrow direction guard, immutable prescription and bounded-exposure contracts before the JIT runtime resumes.

PREPARATION_RUNWAY_FULL_BLOCK_REMAINS_THE_SINGLE_NUMERIC_AUTHORITY_WITH_BOUNDED_ROLLING_EXPOSURE_PRESERVING_EXACT_WEEK_VALUES_TARGET_LOCK_AND_TERMINAL_STAGE

## 18. Interpolation coordinates

Authoritative coordinates are full Runway duration, original starting value, immutable target, original local week, global plan week, progress numerator equal to the original full-block index, denominator `fullDuration - 2`, and selected slice start/end offsets. A bounded slice returns the identical original week values; slice-local progress reset is prohibited.

## 19. Local/global week mapping

Local Runway week remains 1 through N. Its global plan week remains `GeneralEnduranceWeeks + localRunwayWeek`. Selecting a slice changes neither coordinate and never renumbers the slice’s first element to local Week 1.

## 20. PreSpecificTransition handling

`PreSpecificTransition` occurs exactly once, on full local Runway week N. First and middle slices retain their original stages and never synthesize a terminal stage. A final slice exposes the already-generated terminal week unchanged.

## 21. Target-lock scope

One immutable Core Week-1 target and one prescription version govern the entire Runway global range. Separate slice locks are rejected. Already computed or activated Runway values cannot be rewritten.

## 22. Future evidence refresh authority

New checkpoint evidence cannot refresh the target midway through the locked Runway prescription because no canonical split policy exists. After Runway ends, a later version may govern non-overlapping, not-yet-activated Core-only weeks. It cannot retroactively alter Runway.

## 23. Mixed-window implications

A GE→Runway window combines Phase 4K.7 GE output with exact locked Runway references and activates atomically. A Runway-only window exposes the requested exact slice. A Runway→Core window combines final locked Runway references with approved Core output while retaining calendar/pace continuity and atomicity. A Core-only window is unaffected except for normal context/version provenance. None is implemented here.

## 24. Internal full-computation safeguards

`ComputedInternalPending` describes the internal prescription conceptually and is not a new public lifecycle enum. Future roadmap weeks remain `NumericPending`; unselected values are non-executable, create no `TrainingDay`, are absent from public DTOs and persistence, cannot represent achieved capacity, and cannot become checkpoint evidence.

## 25. Failure taxonomy

The existing reasons remain sufficient. Unsupported direction and transition incompatibility use `JIT_SEGMENT_TRANSITION_INFEASIBLE`; missing Runway/Core context continues to use `RUNWAY_JIT_CONTEXT_UNAVAILABLE` or `CORE_JIT_CONTEXT_UNAVAILABLE`; evidence conflict, general numeric infeasibility and safety continue to use their existing typed reasons. New `RUNWAY_DOWNWARD_CONSOLIDATION_UNSUPPORTED` and `RUNWAY_BOUNDED_MATERIALIZATION_UNAVAILABLE` public reasons are not added.

## 26. Governance artifacts

`TD-LONG-HORIZON-RUNWAY-DOWNWARD-CONSOLIDATION-AUTHORITY-001` and `TD-LONG-HORIZON-BOUNDED-RUNWAY-MATERIALIZATION-001` are recorded CLOSED. `TD-LONG-HORIZON-RUNWAY-CORE-JIT-CONTEXT-001` carries an append-only correction while preserving unrelated decisions. `TD-LONG-HORIZON-VOLUME-ENVELOPE-REDESIGN-001` remains OPEN and records the narrowed remaining work.

## 27. Diagnostics

Unchanged-pipeline diagnostics cover direction separation, both profiles, the real Core input matrix, durations 3–8, first/middle/final slices, independent-restart rejection, one-lock behavior, overlapping-refresh rejection and later non-overlapping Core-only refresh. Exact bounded exposure is reference-equivalent to the serialized authoritative full weeks.

## 28. Tests

Diagnostic tests prove upward, flat and unsupported downward behavior; independent weekly/long-run normalization; all full durations and terminal stages; exact slice equivalence; and target-lock/refresh rules. Governance tests prove the append-only correction, retained prior decisions, both records and fields, aggregate parity, absence of invented formulas/fallbacks and the documentation/implementation boundary.

## 29. Production/public/persistence status

No production contract, algorithm, runtime wiring, DI registration, persistence mapping, public preview/API or Flutter surface is changed. Phase 4K.8 JIT execution and mixed-segment activation remain unimplemented. No future Runway output is persisted or publicly exposed.

## 30. Final classification

The direction authority and exact bounded-exposure design are approved governance prerequisites. They are not runtime completion or public activation.

PHASE4K_8_RUNTIME_REMAINS_UNIMPLEMENTED_PENDING_THE_APPROVED_AUTHORITY_CONTRACT_IMPLEMENTATION

## 31. Exact next phase

Next: **Phase 4K.8B — Preparation Runway Direction Guard and Bounded Prescription Contract Implementation**. After 4K.8B, return to **Phase 4K.8 — Runway and Core JIT Numeric Runtime and Mixed-Segment Window Activation**.
