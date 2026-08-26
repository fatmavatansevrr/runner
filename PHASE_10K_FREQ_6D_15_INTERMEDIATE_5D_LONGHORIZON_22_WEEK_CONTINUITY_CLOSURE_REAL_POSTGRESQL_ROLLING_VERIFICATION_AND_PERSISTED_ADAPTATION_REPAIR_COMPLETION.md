# PHASE 10K-FREQ.6D.15 — Intermediate×5D LongHorizon 22-Week Continuity Closure, Real PostgreSQL Rolling Verification & Persisted Adaptation/Repair Completion

**Type:** IMPLEMENTATION + REAL DATABASE VERIFICATION + DARK CLOSURE
**Parent phases:** FREQ.6D.13 (rolling lineage/JIT dual-KEY), FREQ.6D.14 (GE 5D structural/numeric implementation)
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. This report documents real production fixes, a real PostgreSQL round trip, and is explicit about what remains open.

---

## 1. Governance preflight

- HEAD at start: `02b9496`, 0/0 divergence from origin.
- `FREQ.6D.15` confirmed unreserved (no collisions in `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, or report filenames) and scheduled — commit `921f8ae`.
- `FREQ.6D.14` confirmed latest completed phase, Final Classification `INTERMEDIATE_5D_LONGHORIZON_GE_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL`, remaining scope confirmed exactly as: (A) 22-week Runway numeric-continuity gap, (B) real PostgreSQL verification of the 5D GE rolling-activation path, (C) persisted adaptation verification, (D) persisted repair verification.

---

## 2. Honest scope summary (read this first)

This phase **root-caused and formally classified** the 22-week gap (§3-9) — it is a genuine, pre-existing, day-count-neutral numeric-authority gap, not fixable from existing authority without inventing a value, and is now proven to be far wider than "22 weeks" (§9). It **completed real PostgreSQL persistence, fresh reload, and lineage-survival verification** for the Intermediate×5D LongHorizon GE rolling-activation window (both a short and a long horizon), fixing two real, previously-undiscovered production defects along the way. It did **not** complete persisted adaptation or persisted repair verification (items I/J of the success boundary) — disclosed explicitly, not glossed over.

---

## 3-9. 22-week failure reconstruction, classification, and authority audit

**Root cause (source-verified in `PreparationRunwayNumericMaterializer.Materialize`, not inferred):** the materializer linearly interpolates Runway's 8 weeks from GE's own exit volume (`startingWeekly`) to Core's independently-computed Week-1 target (`targetWeekly`), and fails closed the instant `startingWeekly - targetWeekly > tolerance` — there is no approved rule for Runway to reduce down to a lower Core boundary.

**Exact trace (LONGHORIZON_22_WEEK_NUMERIC_CONTINUITY_TRACE):**

| Segment | Weeks | GE behavior | Outcome |
|---|---|---|---|
| GE (1 wk, 21-total) | 1 | Week 1 = baseline, unprogressed (no second week exists) | GE exit = baseline (≤ Core target) |
| GE (2 wk, 22-total) | 1-2 | ShortExtension (EntryAlignment, PreRunwayAlignment) — **no Recovery role exists in the ShortExtension vocabulary at all** | Week 2 = baseline + 1 progression step (up to +7%/2.5km) → GE exit > baseline |
| Core Week-1 target | — | Computed independently from the *same raw evidence*, unaffected by GE length | Fixed regardless of GE weeks |
| Runway entry check | — | `startingWeekly (GE exit) - targetWeekly (Core) > tolerance` | **FAILS** for 22 weeks (margin between baseline and Core's target is thin enough that even one progression step tips it over) |

**Classification: (E) actual unresolved product/numeric authority — not (A/B/C/D).** This is not an off-by-one, rounding, tolerance-units, or 4D-assumption defect. It is a genuine three-way numeric interaction (GE's forward-only progression model, Runway's own forward-only linear-interpolation model, Core's independently-fixed boundary) that no existing document defines a reconciliation rule for.

**§8 short-GE-edge sweep (21/22/23/24), extended per the finding's own implications:** 21 succeeds (no growth). 22, 23, 25, 26, 27 all fail identically (`NonRecoveryTerminalShortHorizons_FailClosed_SameRunwayCoreBoundaryGapAs22Weeks`, `RecoveryTerminalHorizon_LowBaseline_StillFailsIfPeakBeforeRecoveryExceedsBoundary`) — **any GE segment whose final week is not itself a Recovery week keeps climbing and eventually exceeds Core's fixed boundary.** 24 weeks succeeds only because its GE segment (1 full mesocycle) happens to end on a Recovery week, landing back near baseline. Even a Recovery-terminal horizon (28 weeks, 2 mesocycles) still fails at a low baseline, because the cumulative pre-recovery peak (not just "ends on Recovery") determines the outcome — proving this is a genuine numeric-magnitude interaction, not a simple structural rule.

**§4 4D baseline reproduction:** re-confirmed via a direct repro against the completely unmodified 4D orchestrator (`TwentyTwoWeeks_AlsoFailsOnUnmodifiedFourDayOrchestrator_PreExistingNotFiveDaySpecific`) — identical failure, identical message, using 4D's own pre-existing 20km baseline. **This is not a 5D regression.**

**§6 authority audit:** No existing document (FREQ.6D.9/6D.10/6D.12/6D.13/6D.14, or any pre-10K Preparation Runway numeric authority) defines what should happen when GE's own exit legitimately exceeds Core's independently-computed boundary outside of a lucky post-recovery landing. Reducing Runway's own entry evidence, capping GE's growth earlier for short segments, or recomputing Core's Week-1 target relative to GE's exit would each be a genuinely new product/numeric decision.

**Per this phase's own STOP discipline (§6/§45): no value was invented. This item is classified `BLOCKED_ON_SHARED_NUMERIC_AUTHORITY`, not fixed.** It affects all LongHorizon frequencies equally (confirmed day-count-neutral) — it is a Preparation Runway / GE authority gap, not a 5D-specific or 4D-specific one. `FREQ.6D.13` and `FREQ.6D.14`'s own work remains fully valid and untouched by this finding.

---

## 10-11. Cross-frequency regression / full 21-52 matrix

No fix was attempted (§3-9 concluded no authority-preserving fix exists), so no cross-frequency regression risk was introduced by this item. The full 21-52 matrix therefore **cannot** reach 32/32 via existing authority — this is now proven, not merely disclosed as untested. `FREQ.6D.14`'s own dark matrix (21/24/28/32/40/52, using a near-cap starting evidence chosen specifically to avoid this gap) remains the accurate characterization of what the current, unmodified pipeline can produce.

---

## 12-19. Real PostgreSQL — 5D GE rolling-activation persistence and reload

Scoped to `LongHorizonRollingInitialActivationRuntime` (numerically activates only GE weeks 1..min(4, GE weeks)) — this runtime never reaches the broken Runway/Core boundary code path at all (confirmed by direct trace: it only calls `LongHorizonGeStructuralSelector`/`LongHorizonGeNumericExecutor`, never `PreparationRunwayNumericMaterializer`), so its own persistence is provable independently of §3-9's finding.

**Fixed to accept 5D:**
- `LongHorizonRollingInitialActivationInputValidator.Validate`'s `DaysPerWeek != 4` eligibility gate and its separate `PreferredDays.Count != 4` calendar-input gate — both relaxed off `request.DaysPerWeek` (default-preserving for every 4D caller).
- `ExistingLongHorizonGeWindowMaterializer.Materialize` — now derives the FREQ.6D.14-approved 5D policy (`VolumeSafetyPolicy.FiveDayIntermediate`, target cap) off the selected descriptors' own `EasySupportWorkouts.Count`, rather than always using the 4D `Default` policy.
- `MapActivatedWeek`'s `RoleKeyFor` helper — was hardcoded `structuralSlotIndex <= 2 ? "EASY_SUPPORT_1" : "EASY_SUPPORT_2"`, silently collapsing a 5D week's 3rd EASY session onto the 2nd session's role key (causing a weekday-assignment collision and a wrong, repeated distance value). Generalized to a 1-based EASY-occurrence counter, producing `EASY_SUPPORT_1..N`.
- `MapActivatedWeek` never populated `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/`ProfileKey`/`ProfileVersion` on the `LongHorizonSessionPrescriptionReference` it constructs — a gap pre-existing for 4D too (simply never observed, since nothing previously asserted these fields for GE-only sessions). Fixed: `SlotOrdinal` = the slot's own week-wide 0-based positional index (GE never has more than one KEY, so no lane ambiguity exists there — `LaneOrdinal` stays null, matching FREQ.6D.13's own established invariant); `ProgressionStageKey` = the roadmap week's own `GeStageFamily`; `ProfileKey`/`ProfileVersion` stay null (GE sessions are always Legacy, never ProfileBacked — satisfies the existing both-null-or-both-present invariant).

**Real, previously-undiscovered gap found and fixed:** `LongHorizonRollingStateReconstructionService`'s row-to-`LongHorizonSessionPrescriptionReference` projection (used by `LoadRestartSnapshotAsync`, the actual "fresh reload" path) never copied the five FREQ.6D.13 lineage columns back — they were written correctly on persist (confirmed via a direct raw-row query) but silently dropped on reconstruction. This is not a 5D-specific defect (the same code path serves 4D), simply never previously exercised by an assertion checking these fields through the `ActivatedWeeks` projection specifically. Fixed by copying all five fields (`LaneOrdinal`, `SlotOrdinal`, `ProgressionStageKey`, `CatalogPrescriptionProfileKey`→`ProfileKey`, `CatalogPrescriptionProfileVersion`→`ProfileVersion`) from the persisted row.

**7/7 new real-Postgres tests pass** (`LongHorizonRollingInitialActivationFiveDayPersistenceTests.cs`), every operation opening a genuinely fresh `AppDbContext` (matching FREQ.6D.13's own established convention — never a save-and-continue on the same tracked entity graph):

- `ShortHorizon_ActivatesApproved_FiveSessionsPerGeWeek` (21, 24 weeks) — real activation, 5 sessions per GE week (1K+3E+1L).
- `LongHorizon_FiftyTwoWeeks_ActivatesApproved_StructuralRoadmapCoversAllWeeks` — full 52-week structural roadmap (32 GE weeks) materializes and activates its first window.
- `PersistsToRealPostgres_ExactStructuralWeekCount` — real PostgreSQL row count matches exactly.
- `FreshReload_PersistedLineage_LaneOrdinalSlotOrdinalProgressionStageProfileSurvive` — a genuinely independent `AppDbContext` reload proves every persisted GE session carries a non-null `SlotOrdinal` and non-empty `ProgressionStageKey`.
- `GeStateReload_NextWeekStillFiveSessionShape_WithDistinctSlotOrdinals` — after reload, every activated GE week is still exactly 1K+3E+1L with 5 distinct `SlotOrdinal` values.
- `DuplicateIdentityGuard_RepeatedEasySlotsAcceptedAsDistinct_NoFalsePositive` — 3 real, persisted `EASY_SUPPORT` sessions in one week, each with a distinct `SlotOrdinal`, confirming FREQ.6D.13's corrected duplicate-identity validator has no false positive for the 5D shape.

**Not reached (items F/G/H of the success boundary):** GE→Runway reload, Runway→Core dual-KEY reload, and ProfileBacked Core execution after reload all require the plan to actually cross out of the GE segment — which this scoped-narrow runtime never attempts, and which (per §3-9) the current Runway numeric authority cannot generically support for most GE trajectories. `LongHorizonRollingCheckpointRuntime` (the checkpoint-continuation runtime that would eventually reach this crossing) was not touched or gated for 5D this phase.

## 20-25. Persisted adaptation — NOT completed

Not attempted this phase. `LongHorizonRollingCheckpointRuntime` retains its own separate `DaysPerWeek` gate and its own `MapActivatedWeek`-equivalent construction, not yet audited or fixed for 5D. Disclosed as open scope.

## 26-29. Persisted repair — NOT completed

Not attempted this phase for the same reason — the repair-lineage-preservation mechanism FREQ.6D.11 designed has no executing code path today (confirmed in FREQ.6D.13's own report), and exercising it meaningfully requires the checkpoint/repair machinery this phase did not reach.

## 30. Duplicate-identity guard regression

Re-verified via `DuplicateIdentityGuard_RepeatedEasySlotsAcceptedAsDistinct_NoFalsePositive` (real 5D, 3 EASY sessions, real Postgres) and via the full LongHorizon regression (§13 below) — the FREQ.6D.13 correction (skip validation for sessions with no `SlotOrdinal`) remains intact; no new regression introduced.

## 31. 52-week real DB stress case

Completed (§12-19) — `LongHorizon_FiftyTwoWeeks_ActivatesApproved_StructuralRoadmapCoversAllWeeks` proves the full 52-week (32 GE week) structural roadmap materializes correctly and its first window activates and persists. A full 8-checkpoint walk through all 32 GE weeks (to reach the cap/plateau and the eventual Runway crossing) was not attempted — the checkpoint continuation runtime was not touched this phase.

## 32-33. Missing/zero readiness real path

Not independently re-verified via the real Postgres path this phase (the typed `PRODUCT_INELIGIBLE` exceptions from FREQ.6D.14 are dark-tested at the `LongHorizonGeNumericExecutor`/`LongHorizonFullNumericOrchestrator` level, 41/41 passing per FREQ.6D.14 and this phase's own additions) — not re-run through `LongHorizonRollingInitialActivationRuntime` specifically. Given that runtime's own generic `catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)` block (confirmed in FREQ.6D.13's exploration) will catch these typed exceptions (both derive from `InvalidOperationException`) and map them to a `Blocked` result, no partial rows would be persisted before rejection — but this was not independently proven via a real-Postgres test this phase.

---

## 34. 4D LongHorizon zero-delta

Confirmed via full regression (§13) — 1207/1207 LongHorizon tests pass, including every pre-existing 4D test. `TwentyTwoWeeks_AlsoFailsOnUnmodifiedFourDayOrchestrator_PreExistingNotFiveDaySpecific` explicitly reproduces the shared gap against unmodified 4D rather than merely asserting zero-delta.

## 35-36. 5D Core / Runway zero-delta

Not independently re-run this phase as a distinct step — no Core or Runway product code was touched (only LongHorizon rolling-activation and reconstruction code). Confirmed indirectly via the full regression suite (§13) including the pre-existing 5D Core/Runway public-activation test files, all passing.

## 37-38. Public routing / unsupported neighbors

Untouched. `LongHorizonPublicPlanService.cs`'s own public-routing gates remain unchanged; public Intermediate×5D LongHorizon 21+ remains closed. No neighbor combination was touched.

---

## 41. Full regression

- New test files: `LongHorizonRollingInitialActivationFiveDayPersistenceTests.cs` (7/7, real PostgreSQL), plus 6 new boundary-gap evidence tests appended to `LongHorizonFullNumericOrchestratorFiveDayTests.cs`.
- Full LongHorizon suite: **1207/1207** passing (1194 pre-existing + 13 new), zero regressions.
- PlanCatalog full suite: **1510/1510**, unaffected.
- Debug build: clean, 0 errors.
- Full `RunningApp.IntegrationTests` regression: running at report time; result to be confirmed in the governance commit / final report addendum if it completes after this document is written, per the same discipline established in prior phases (report reflects what was actually verified, not what is merely expected).

## 42. Baseline failure attribution

The two previously-documented, unrelated, pre-existing stale-date failures (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)`, `Sw09ExplicitZeroReadinessEndToEndTests...`) are expected to remain the only failures in the full suite, consistent with every prior phase's own independently-reproduced baseline.

---

## 43-45. Success boundary / classification

Success boundary items achieved: **A** (22-week gap root-caused, classified, and correctly not force-fixed), **B/C/D/E** (real PostgreSQL 5D GE rolling persistence, fresh reload, lineage survival, all proven for both a short and long horizon), **J-partial** (duplicate-identity guard re-confirmed), **K/L** (4D and public 5D zero-delta), **M** (public 21+ remains closed). **Not achieved**: full 21-52 dark matrix (proven structurally unreachable via existing authority, not merely untested — this is the honest, evidenced outcome of §3-9), **F/G/H** (GE→Runway/Runway→Core/ProfileBacked reload — out of this narrower runtime's reach), **I/J** (persisted adaptation/repair — not attempted this phase).

Per §45's own explicit instruction: since the 22-week gap could not be corrected solely from existing authority, this phase's final classification is:

**`INTERMEDIATE_5D_LONGHORIZON_DARK_COMPLETION_BLOCKED_ON_SHARED_22_WEEK_NUMERIC_AUTHORITY`**

Execution Status: `DONE (PARTIAL)`. This blocker affects **all LongHorizon frequencies** (confirmed day-count-neutral, not 5D-specific, not 4D-specific) — it is a shared Preparation Runway/GE numeric-continuity authority gap. `FREQ.6D.13` and `FREQ.6D.14`'s own work remains fully valid and retained; this phase's real Postgres persistence work (§12-19) is genuine, additional, retained progress achieved *despite* the blocker, since it targets a code path the blocker does not reach.

---

## 47. Next phase

Two independent, well-scoped continuations exist, neither yet scheduled: (1) a dedicated PRODUCT_DECISION/NUMERIC_DECISION phase to resolve the shared Runway/GE numeric-continuity authority gap (§3-9) — this is a genuine new-authority decision, not an implementation task, and must precede any further dark-matrix work; (2) a continuation implementation phase extending real Postgres verification to `LongHorizonRollingCheckpointRuntime` (5D gate relaxation, persisted adaptation, persisted repair) — achievable independently of (1) since it doesn't require crossing the Runway boundary either, until GE segments long enough to require multiple checkpoints are exercised. `NEXT_PHASE_NOT_YET_SCHEDULED`.
