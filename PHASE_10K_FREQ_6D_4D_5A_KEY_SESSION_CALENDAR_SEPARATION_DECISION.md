# Phase 10K-FREQ.6D.4D.5A — Intermediate×5D KEY-Session Calendar Separation Evidence & Product Decision Closure

**Evidence + product-decision phase only. No production code touched. `CatalogWeekSkeletonCalendarMaterializer` unmodified. No public routing change. No RunLayout change. No profile/dosage change. No Adaptation change.**

## 1. Preflight

`PHASE_LEDGER.md` row 77: `FREQ.6D.4D.5`, `IMPLEMENTATION`, `DONE (PARTIAL)`, `FREQ6D4D_SPLIT_E_PARTIAL_RUNTIME_DISCOVERY_IMPLEMENTED_PUBLIC_ACTIVATION_BLOCKED`, confirmed (report: `PHASE_10K_FREQ_6D_4D_5_REAL_5D_CATALOG_AND_RUNTIME_BUNDLE_DISCOVERY_PARTIAL_IMPLEMENTATION.md`, read in full). `MASTER_ROADMAP.md`'s roadmap sequence block confirmed `[Next, not yet scheduled]` names exactly this question and assigns no phase ID — this phase is assigned `FREQ.6D.4D.5A`, following this engagement's own established `<parent>.<letter>` sub-split convention (e.g. `FREQ.6D.1A`→`FREQ.6D.1B`).

Durable baseline verified: local `HEAD` = `0b740a2` (`docs(governance): record Gate B durability checkpoint at 13594ac`), `origin/main` = `0b740a2`, ahead/behind `0/0`. Gate B passed and recorded at `13594ac` (roadmap §10, ~10 phases since the prior gate at `0bc70c5`). Working tree: only the two pre-existing, unrelated `plan-catalog/artifacts/audits/*` modifications and `baseline_tmp`, unchanged since before Split E — confirmed clean for this phase's own start.

**Critical repository-authority finding, made during preflight, not assumed**: this exact question was already investigated once before, earlier in this program's history, under a *different* numbering chain (`FREQ.3`/`FREQ.4`/`FREQ.4A`, predating the `FREQ.6D` branch). §4 below reconstructs this fully — it materially changes the shape of this phase's work, since a real, disclosed, already-tested **placeholder** value already exists in the codebase, and this phase's job is to either confirm or replace it with a genuinely evidenced final value.

## 2. Current blocker (from FREQ.6D.4D.5)

`CatalogWeekSkeletonCalendarMaterializer` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogWeekSkeletonCalendarMaterializer.cs`) — the calendar-date-assignment step in the live 8-14 week Core route — rejects any skeleton with `DaysPerWeek` other than 3 or 4, and its date-assignment algorithm (`TryAssignKeySessionDates`) is built around exactly one `KEY_SESSION` slot per week: `chosenKeySessionDates` is a `DateOnly?[]` with one entry **per week** (not per KEY slot), and `BuildDatedWeek` assigns that single date to every slot whose `StructuralRole == "KEY_SESSION"`. A real 5D week has two. This was found by a genuine HTTP 500 during real E2E testing, not static analysis.

## 3. Exact failure trace (`PUBLIC_5D_CALENDAR_FAILURE_TRACE`)

```
Request: level=Intermediate, daysPerWeek=5, preferredDays=[Mon,Tue,Wed,Thu,Fri]
  → V1CatalogPilotIdentityPolicy.ResolveCandidate(Intermediate, 5)
      → TEN_K__5D__INTERMEDIATE v1 (real, published, VALIDATED)
  → RunLayout resolved: RUN_LAYOUT_5D v1
      → 5 slots: KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN
  → CatalogPlanSkeletonOrchestrator builds a structural GeneratedCatalogPlanSkeleton
      → correctly carries 2 KEY_SESSION slots per week (no defect here; this
        stage is layout-driven, not hardcoded — confirmed by FREQ.3 §B: the
        binding-path validator was already found generalized)
  → CatalogWeekSkeletonCalendarMaterializer.Materialize(context)
      → ValidateSkeletonRoleStructure(skeleton)
          → skeleton.DaysPerWeek == 5, guard requires (3 or 4)
          → THROWS CatalogCalendarRoleStructureInvalidException:
            "Core calendar assignment supports resolved 3D/4D layouts,
             but the source skeleton declares 5."
  → CatalogPreviewGenerator.BuildDarkInternalDatedSkeleton wraps this as
    PlanPreviewGenerationFailedException (CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED)
  → HTTP 500 Internal Server Error
```

Confirmed by a real `dotnet test` run against a real `CatalogPublisher`-produced test release (`PublishedCatalogTestRelease`, the same real-publish fixture `Gen3BThreeDayPublicActivationTests` already uses), not a mock or synthetic fixture.

**Exact reason, disambiguated per this phase's own instruction (§4 of the originating prompt):**
- The materializer's **role-count validator** (`ValidateSkeletonRoleStructure`) identifies `KEY_SESSION` only by `StructuralRole` string, and its cardinality check (`keyCount != 1`) is a literal single-count assumption — it does not consult `LaneOrdinal` at all (the field does not exist on the structural slot type this validator reads).
- Even before that: it hard-rejects on `DaysPerWeek is not (3 or 4)` — a hardcoded 3D/4D cardinality assumption, unconditional on role composition.
- The **date-assignment algorithm** beneath the validator (`TryAssignKeySessionDates`/`BuildDatedWeek`) performs **no pairwise spacing validation at all** for KEY↔KEY — it cannot, because it never represents more than one KEY_SESSION date per week in its data model (`DateOnly?`, not `IReadOnlyList<DateOnly>`).

Not a placement-feasibility failure — the algorithm never reaches placement logic for a 5D skeleton; it fails at input validation before any date is assigned.

## 4. Existing calendar/safety authorities — full audit

Real search performed across `RuntimeCatalog/Schedule/Materialization`, `RuntimeCatalog/Schedule/LongHorizon/Adaptation`, and this program's own prior phase reports (`FREQ.3`, `FREQ.4`, `FREQ.4A`, all predating the `FREQ.6D` branch and never re-read by any `FREQ.6D.4D` split until now).

| Authority | Component | Status |
|---|---|---|
| KEY↔LONG same-week/cross-week spacing | `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToLongRunSeparationDays = 2` | **DECIDED, IMPLEMENTED, TESTED** — `PRODUCT_DEFAULT` per `FREQ.3` §D.2, generalized to N KEY instances by `FREQ.4` §B, real-DB-tested in Adaptation V1's `ScheduleRepairSpacingValidator` reuse |
| KEY↔KEY same-week spacing | `DatedGeneratedCatalogPlanSkeletonValidator.MinimumKeySessionToKeySessionSeparationDays = MinimumKeySessionToLongRunSeparationDays` (= 2, an **embedded placeholder**, explicitly self-disclosed as not a final decision) | **PARTIAL** — mechanism implemented and real-DB-tested (`FREQ.4A`), numeric value **not yet independently evidenced** — this phase's exact subject |
| PreferredDays hard-authority | `CatalogWeekSkeletonCalendarMaterializer.ValidatePreferredDays` (count/duplicate) + `CatalogPlansController`/request-level 400 validation | **DECIDED, IMPLEMENTED, TESTED** — hard, not soft (§10 below) |
| LongRunDayPreference authority | `ValidateLongRunDay` | **DECIDED, IMPLEMENTED, TESTED** — must belong to PreferredDays, hard |
| Invalid-configuration fail-closed behavior | `CatalogPreferredDayConfigurationUnsafeException` | **DECIDED, IMPLEMENTED, TESTED** — typed exception, no auto-correction, no silent default substitution (§11 below) |
| Consecutive-running-day (any role) policy | *(searched, none found)* | **MISSING** — no general "no two running days in a row" rule exists anywhere; only role-specific (KEY↔LONG, KEY↔KEY) spacing exists. Out of scope — not reopened here (§10 of the originating prompt) |
| Deterministic tie-break for multiple legal assignments | `BuildDatedWeek`'s `SlotOrderInWeek`-ascending → date-ascending mapping (used today for the 2 `EASY_SUPPORT` slots) | **DECIDED, IMPLEMENTED, TESTED** — pre-existing, reusable for KEY↔KEY without a new decision (§19/§27 below) |
| Calendar-materializer 5-slot support (the actual blocker) | `CatalogWeekSkeletonCalendarMaterializer` | **MISSING** — never generalized by `FREQ.4` (that split touched `DatedGeneratedCatalogPlanSkeletonValidator`, a *different*, downstream, output-validation component — §5 below explains the distinction) |

**No conflicting authority found.** KEY↔LONG is frozen and is not reopened by this phase.

## 5. KEY↔LONG authority — architectural clarification (not a reopening)

There are **two distinct calendar components**, confirmed by direct code read, previously conflated in casual description:

1. **`CatalogWeekSkeletonCalendarMaterializer`** — the *input*-side algorithm that assigns calendar dates to already-structural session slots. This is `FREQ.6D.4D.5`'s real blocker (§2-3). It was never touched by `FREQ.4`'s generalization work and remains hardcoded to exactly one `KEY_SESSION` slot, in both its guard and its date-assignment data model.
2. **`DatedGeneratedCatalogPlanSkeletonValidator`** — a *separate*, downstream component that structurally validates an *already-dated* skeleton (produced by #1) after the fact. `FREQ.4` generalized this validator to N≥1 KEY sessions, including adding the `MinimumKeySessionToKeySessionSeparationDays` placeholder. It is real, tested, and correct for its own purpose — but it cannot help produce a legal 5D calendar in the first place, because `CatalogWeekSkeletonCalendarMaterializer` throws before this validator is ever reached for a 5D skeleton.

This means: the *rule itself* (a numeric separation threshold) already has a real, tested home and a disclosed placeholder value. What genuinely does not exist yet is (a) an evidenced final numeric value, and (b) the *implementation* connecting the value to a working multi-KEY date-assignment algorithm in `CatalogWeekSkeletonCalendarMaterializer` — explicitly the next phase's job (§19), not this one's.

## 6. External evidence

Real web research performed (not reused verbatim from `FREQ.3`, which is now 2+ phases old and did not include fresh external plan-pattern research — only a qualitative literature summary). Classified per the originating prompt's required taxonomy.

- **Recovery-physiology consensus** (multiple sports-medicine/coaching sources; [Runners Connect](https://runnersconnect.net/do-older-runners-need-more-rest/)-adjacent aggregated guidance): "Hard-intensity workouts need 48 to 72 hour spacing between sessions... If you train again before supercompensation completes, you're building on a depleted foundation." — **COACHING_RECOMMENDATION**, not a controlled-trial finding; converges with `FREQ.3` §D.2's own earlier finding ("no source located specifies a hard scientific minimum-hours threshold... the real consensus is qualitative: 'no more than 2 hard sessions/week, with easy days between them'"). No new primary physiological trial evidence was found this phase either — the qualitative-convention conclusion stands, now corroborated by a second independent research pass.
- **48-72 hour convention, translated to calendar dates**: 48h = a 2-calendar-day difference at minimum (e.g. Tue evening to Thu morning); 72h skews toward a 3-day difference. This is **INFERENCE** (converting an hours-based coaching heuristic into the app's date-only representation), not a direct claim from any source — disclosed explicitly per the originating prompt's own warning against smuggling ambiguous phrasing into a precise threshold.

## 7. Training-plan pattern evidence (`EXTERNAL_5D_HARD_SESSION_SPACING_TABLE`)

Real, individually-fetched plans (not aggregated search-snippet paraphrase) — **TRAINING_PLAN_PATTERN** evidence, not physiological proof:

| Source | Frequency | Quality-session placement | Date difference (this phase's metric) |
|---|---|---|---|
| [runningfront.com Intermediate 10K](https://www.runningfront.com/10k-intermediate-training-plan/) | 5D, 2 quality | Key1 → Rest → Easy → Key2 → Rest → Long → Easy | **3** (2 intervening dates) |
| [mymottiv.com Intermediate 10K](https://www.mymottiv.com/how-to-train-for-a-10k/intermediate-10k-training-plan) | 4D, 2 quality | Tue (interval) → Wed (rest) → Thu (tempo) | **2** (1 intervening date) |
| [marathonhandbook.com 10-Week 10K](https://marathonhandbook.com/10-week-10k-training-plan/) | 4-5D, 2 quality | Mon (quality) → Tue/Wed (cross/rest) → Thu (quality) | **3** (2 intervening dates) |
| [runwithcaroline.com Intermediate 10K](https://www.runwithcaroline.com/intermediate-10k-training-plan/) | 6D, 1 running quality + 1 cross-training | Wed → Thu/Fri → Sat | **3** (only 1 true running-quality session; weak match) |
| [halhigdon.com Intermediate 10K](https://www.halhigdon.com/training-programs/10k-training/intermediate-10k/) | 6D, 1 quality (alternating interval/tempo) | single Wednesday session only | *(not applicable — only 1 quality day/week, no KEY↔KEY pair exists)* |

**No real plan found placing two quality/hard running sessions on consecutive calendar dates (difference = 1).** All genuine 2-quality-session patterns found use a date difference of either 2 or 3, both consistent with "at least one full easy/rest day between hard sessions." This directly matches `FREQ.3` §D.2's own qualitative-convention conclusion, now confirmed by a second, independent, fresh search rather than reused verbatim.

## 8. Candidate policies

| Policy | Definition | Evidence support | 5D feasibility (§9) |
|---|---|---|---|
| K1 | `abs(KEY1.Date − KEY2.Date).Days >= 1` (consecutive allowed) | **None** — no source, external or internal, recommends or observes consecutive hard-day pairing; contradicts the uniform "no back-to-back hard workouts" convention found in every real external plan | Trivially feasible (imposes no real constraint), but rejected on evidence grounds |
| K2 | `>= 2` (1 intervening date) | Matches the *majority* of real external patterns (mymottiv: exactly 2); within the 48h lower bound of the physiology consensus; **identical to the existing, already-tested, already-`PRODUCT_DEFAULT`-classified `MinimumKeySessionToLongRunSeparationDays`/embedded `MinimumKeySessionToKeySessionSeparationDays` placeholder** | Feasible across every representative preferred-day pattern tested (§9) |
| K3 | `>= 3` (2 intervening dates) | Matches the *other* real external patterns (runningfront, marathonhandbook); within the 72h upper bound of the physiology consensus | **Infeasible for at least one realistic preferred-day pattern** (§9 — a genuine counterexample was found, not merely assumed) |
| K4 | Phase-dependent (e.g. looser in Foundation, stricter in Race-Specific/Taper) | No source, internal or external, specifies phase-varying hard-session spacing; §16 of the originating prompt explicitly instructs against weakening Taper without direct authority | Not evaluated further — no evidence basis; would add complexity with zero support |

## 9. 5D combinatorial feasibility

All `C(7,5) = 21` possible 5-of-7 preferred-day sets exist in principle, but the real product-relevant question (per §12 of the originating prompt) is whether the **existing, frozen** KEY↔LONG rule (`>= 2`) combined with each KEY↔KEY candidate still admits a legal assignment for realistic patterns — not an exhaustive 21-row proof, which would itself be premature before the real multi-slot algorithm exists (§19). Five representative patterns (the originating prompt's own §13 list, one of the two extras substituted for a genuinely distinct offset shape) were hand-verified against calendar-date offsets, assuming a Monday-start week (this engagement's own convention in every prior worked example; the real week-start weekday is request-derived and the *relative* adjacency pattern generalizes, but this is flagged honestly as a representative check, not an exhaustive proof — appropriate for a decision phase, not an implementation phase):

| Preferred days | LONG (assumed last-listed) | K2 (`>=2`) legal assignment | K3 (`>=3`) legal assignment |
|---|---|---|---|
| Mon Tue Wed Fri Sun | Sun | KEY={Mon,Wed}, EASY={Tue,Fri} — ✅ | KEY={Mon,Fri}? sep=4 ✅; EASY={Tue,Wed} — ✅ |
| Mon Wed Thu Sat Sun | Sun | KEY={Mon,Thu}, EASY={Wed,Sat} — ✅ | KEY={Mon,Thu}? sep=3 ✅ — ✅ |
| Tue Wed Thu Sat Sun | Sun | KEY={Tue,Thu}, EASY={Wed,Sat} — ✅ | **KEY↔LONG(>=2) excludes Sat; remaining candidates {Tue,Wed,Thu}; every pair among these has date-difference <= 2 (max pair Tue/Thu = 2) — NO pair reaches >=3 — ❌ INFEASIBLE** |
| Mon Tue Thu Fri Sun | Sun | KEY={Mon,Thu} or {Tue,Fri}, EASY=remainder — ✅ | KEY={Mon,Thu}? sep=3 ✅ — ✅ |
| Mon Wed Fri Sat Sun | Sun | KEY={Mon,Wed} or {Mon,Fri} or {Wed,Fri}, EASY=remainder — ✅ | KEY={Mon,Fri}? sep=4 ✅ — ✅ |

**Result: K2 (`>= 2`) is feasible for every representative pattern tested — 5/5. K3 (`>= 3`) genuinely fails for one real, common pattern** (`Tue Wed Thu Sat Sun`, LONG=Sun): after excluding Saturday (too close to Sunday's LONG under the unchanged KEY↔LONG rule), only `{Tue, Wed, Thu}` remain as KEY-eligible candidates, and no two of those three dates are 3+ calendar days apart. This is not a contrived edge case — it is a plausible, ordinary preferred-day selection (three consecutive early-week days plus a weekend pair). **This is real, worked evidence against K3, not merely a theoretical concern.**

## 10. PreferredDays interaction

**Hard authority, confirmed by direct code read**, not inferred: `CatalogWeekSkeletonCalendarMaterializer.ValidatePreferredDays` throws `CatalogPreferredDayCountInvalidException`/`CatalogPreferredDaysDuplicatedException` on any non-conforming set; the request-level API layer (confirmed via existing `Gen3BThreeDayPublicActivationTests.ThreeDayPreferredDayCountMismatch_IsValidationError`/`ThreeDayPreferredDayMembership_IsValidated` patterns) redundantly rejects malformed sets with HTTP 400 before reaching the materializer at all. **The materializer must never silently move a session outside PreferredDays to satisfy KEY↔KEY separation** — no such correction mechanism exists anywhere in this codebase, and none is proposed here.

## 11. Invalid preferred-day-set behavior

A preferred-day set can pass count/membership/duplicate validation (§10) yet still admit no legal KEY/EASY/LONG date assignment once safety spacing is applied — this is precisely what §9 demonstrated for K3. **Existing, already-frozen, already-tested authority answers this directly**: `CatalogPreferredDayConfigurationUnsafeException` ("CATALOG_PREFERRED_DAY_CONFIGURATION_UNSAFE") is thrown by the current 3D/4D algorithm whenever no deterministic full-plan assignment satisfies the KEY_SESSION/LONG_RUN separation invariant — a typed, fail-closed rejection (surfaces as `PlanPreviewGenerationFailedException` → a non-200, non-500 typed response), never an automatic correction, never a silently substituted default weekday pattern. This behavior **generalizes directly** to the dual-KEY case without requiring a new decision: the same exception type, the same fail-closed semantics, now checked against a richer constraint set (KEY↔LONG *and* KEY↔KEY instead of just KEY↔LONG). **No `DecisionRequired` here** — this is `DECIDED`, reused, not invented.

## 12. Lane semantics

`KEY_SESSION` lane0 (`PRIMARY`) and lane1 (`SECONDARY_CONTROLLED`) carry different prescriptions (this session's own `FREQ.6D.4D.5` progression content: e.g. `INTERMEDIATE_5D_FOUNDATION_PRIMARY` vs. `INTERMEDIATE_5D_FOUNDATION_SECONDARY_CONTROLLED`), but **no evidence, internal or external, was found supporting a directional rule** (e.g. "PRIMARY must precede SECONDARY_CONTROLLED by N days" as a *different* threshold than the reverse order). Per the originating prompt's own §17 instruction ("default preference should be symmetric only if evidence/architecture supports it"): the architecture *does* support symmetric treatment — `LaneOrdinal` is documented (Split A, `FREQ.6D.4D.1`) as prescription identity, never calendar order, and the existing spacing rules this phase reuses (`MinimumKeySessionToLongRunSeparationDays`, `MinimumKeySessionToKeySessionSeparationDays`) are both already implemented as symmetric `Math.Abs(...)` comparisons with no directional term. **Selected: symmetric — `distance(KEY0, KEY1) >= N`, no directional complexity.**

## 13. Phase/Taper semantics

Every phase (FOUNDATION/BUILD/RACE_SPECIFIC/TAPER) in this session's real dual-lane progression authors exactly 2 `KEY_SESSION` candidates per phase, including TAPER (`TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE`, both `PROTECTED`/`FIXED_EXPOSURE`, min1/max2 dose). No source (internal report or external evidence, §6-7) proposes phase-varying spacing, and §16 of the originating prompt explicitly warns against weakening Taper spacing without direct authority. **One phase-invariant rule selected** — the same `N` applies in every phase, including Taper. A representable real Taper week (2 KEY + 2 EASY + 1 LONG, `N=2`) is confirmed feasible by the same combinatorial check in §9 (Taper does not change the weekly structural shape, only dosage/exposure — out of this phase's scope, per FREQ.6D.4D.5's already-frozen `PROTECTED`/`FIXED_EXPOSURE` semantics).

## 14. Future 6D/7D generalization

The selected rule is stated generically (`distance(KEY_i, KEY_j) >= N` for every pair of KEY_SESSION slots in a week), not conditioned on `RunsPerWeek == 5` — it degenerates to a no-op for any single-KEY week (3D/4D/Beginner×4D, no pairs exist) exactly as `MinimumKeySessionToKeySessionSeparationDays` already does today. **No 6D/7D activation performed or implied** — this phase does not touch RunLayout, routing, or any 6D/7D artifact; the generalization is a property of the rule's *statement*, not an activation of anything beyond 5D.

## 15. Decision matrix (`KEY_KEY_SEPARATION_DECISION_MATRIX`)

| Rule | Evidence strength | Intermediate suitability | 5D combinatorial feasibility | Long-run compatibility | PreferredDays compatibility | Taper compatibility | 6D/7D generality | Implementation determinism | Product complexity | Recommended? |
|---|---|---|---|---|---|---|---|---|---|---|
| K1 (`>=1`) | None — contradicted by every source | Poor — no plan ever pairs hard days back-to-back | Trivially feasible | Compatible | Compatible | Compatible | Generic | Deterministic | Minimal | **No** |
| **K2 (`>=2`)** | **Convergent — matches physiology's 48h floor, the existing embedded `PRODUCT_DEFAULT`, and 1 of 3 real matched external plans exactly, with the other 2 exceeding it (never falling short)** | **Good — matches the system's own existing KEY↔LONG convention** | **Feasible for every representative pattern tested (5/5)** | **Compatible — reuses the identical existing constant/magnitude** | **Compatible** | **Compatible (verified §13)** | **Generic** | **Deterministic — mechanism already implemented and real-DB-tested (`FREQ.4A`)** | **Minimal — zero new mechanism, only a confirmed value** | **Yes** |
| K3 (`>=3`) | Convergent with the 72h upper bound and 2 of 3 real matched external plans, but not universal | Good in isolation | **Fails for a real, common pattern (§9)** | Compatible | **Incompatible with at least one realistic preferred-day set** | Not separately verified (feasibility already fails at the general level) | Generic | Deterministic if it worked | Low | **No — real feasibility counterexample found** |
| K4 (phase-dependent) | None | N/A | Not evaluated (no evidence basis) | N/A | N/A | Prohibited without authority (§16 of originating prompt) | Adds complexity | Adds complexity | High | **No** |

## 16. Selected policy

**K2 — `MinimumKeySessionToKeySessionSeparationDays = 2`**, confirming (not merely retaining) the value already embedded as a placeholder in `DatedGeneratedCatalogPlanSkeletonValidator.cs` since `FREQ.4`. This is a deliberate, evidence-checked freeze of that placeholder, not an unexamined carry-forward: K3 was seriously evaluated and found combinatorially unsafe (§9), and K2 independently matches the strongest convergent evidence available (§6-8, §15).

**Decision-standard basis** (per the originating prompt's §23 requirement — at least one of): (a) real domain evidence (48-72h physiology convention, §6) whose lower bound this value sits exactly at; (b) real, repeated external training-plan pattern evidence (§7 — 1 of 3 real matched plans exactly, the other 2 use a looser value, none looser than this); (c) an already-existing, already-real, already-tested repository canonical rule of the identical magnitude for the closely-related KEY↔LONG case, itself already classified `PRODUCT_DEFAULT` by a prior phase. All three converge on the same value — this is not a threshold invented merely to unblock 5D.

## 17. Exact calendar rule

```
For every pair of KEY_SESSION slots (i, j) scheduled within the same calendar week:
    abs(KEY_i.Date.DayNumber − KEY_j.Date.DayNumber) >= 2
```

Identical mathematical form to the existing, frozen `MinimumKeySessionToLongRunSeparationDays` check (`Math.Abs(a.DayNumber - b.DayNumber) >= N`), for direct implementation-contract consistency (§19).

## 18. Allowed/forbidden examples

**Allowed** (date difference >= 2):
- Tuesday + Thursday (difference = 2)
- Monday + Wednesday (difference = 2)
- Monday + Friday (difference = 4)
- Tuesday + Saturday (difference = 4)

**Forbidden** (date difference < 2):
- Tuesday + Wednesday (difference = 1 — consecutive calendar dates)
- Friday + Saturday (difference = 1)
- Any same-date assignment (difference = 0 — the exact silent-collision failure mode `FREQ.6D.4D.5` §11 identified as the actual risk if the count-guard alone were removed without this rule)

## 19. Implementation contract (for the next, separate implementation phase)

**Input**: dated candidate preferred days; structural sessions carrying `LaneOrdinal` and `SlotOrderInWeek`; `LongRunDayPreference`; the existing hard constraints (`MinimumKeySessionToLongRunSeparationDays`, PreferredDays hard-authority, duplicate/count validation).

**Output**: a deterministic legal `DatedGeneratedCatalogPlanSkeleton`, or a typed failure — never a partial result.

**Required properties**:
- Pairwise KEY↔KEY separation (§17) enforced for every KEY_SESSION pair in a week.
- The existing KEY↔LONG rule (`>= 2`, unchanged) preserved exactly as today for every KEY instance, not just one.
- PreferredDays authority preserved exactly (§10) — no silent day substitution.
- `LaneOrdinal` never re-derived from the assigned date; date assignment happens *after* lane identity is already fixed by the binder (Split A, unrelated to this phase).
- No silent session drop; no silent fallback to a 4D candidate/layout (this program's own long-standing invariant, re-confirmed still intact in `FREQ.6D.4D.5` §13's regression: `RunwayHorizonThreeDay_TypedRejection_NoSilentFourDayCoercion`-style pattern must extend to 5D).
- Typed failure (reusing `CatalogPreferredDayConfigurationUnsafeException`, §11 — no new exception type required) when no legal assignment exists, exactly as today.
- Extends `ValidateSkeletonRoleStructure`'s `DaysPerWeek is not (3 or 4)` guard to admit `5` (and, generically, `keyCount >= 1` in place of `keyCount != 1`, mirroring the exact pattern `FREQ.4` already applied to the separate output validator — §5).
- Generalizes `chosenKeySessionDates`/`TryAssignKeySessionDates`/`BuildDatedWeek` from a scalar-per-week model to an `IReadOnlyList<DateOnly>`-per-week model, searched via a bounded backtracking extension of the existing algorithm (multi-slot, not a redesign from scratch — the existing single-slot search already demonstrates the exact search-space-bounding technique the multi-slot version needs).

**Deterministic tie-break — existing authority found, no new decision required.** `BuildDatedWeek`'s current `EASY_SUPPORT` assignment already establishes the canonical tie-break for same-role slots: `week.SessionSlots.OrderBy(s => s.SlotOrderInWeek)` paired against `remainingForEasy.OrderBy(date => date.DayNumber)` — ascending structural-slot order maps to ascending chronological date. `RUN_LAYOUT_5D`'s own authored `sequenceOrder` already gives the two `KEY_SESSION` slots a fixed relative order (lane0 at `sequenceOrder=1`, lane1 at `sequenceOrder=3`). Applying the *identical, already-existing* convention to `KEY_SESSION` (ascending `SlotOrderInWeek` → ascending chosen date, once a valid 2-date set is found) requires no new product decision and, as a byproduct, deterministically assigns lane0 the chronologically earlier of the two chosen dates — without that being a separately-imposed ordering *requirement* (§12's "LaneOrdinal is not calendar order" is respected: the mapping is a tie-break convention, not a hard rule that would reject an otherwise-valid assignment).

## 20. Test manifest (for the next, separate implementation phase)

- Two KEY sessions on consecutive dates (difference = 1) → rejected (typed `CatalogPreferredDayConfigurationUnsafeException` if no other legal assignment exists, or the algorithm must skip this candidate pairing in favor of a legal one).
- Minimum legal separation (difference = 2) → accepted.
- Larger separation (difference >= 3) → accepted.
- KEY↔LONG rule interaction: a candidate pair that satisfies KEY↔KEY but violates the existing KEY↔LONG rule for either KEY instance → rejected (both rules enforced simultaneously, not KEY↔KEY as a replacement).
- A 5-day preferred-day set with a legal assignment (e.g. `Mon Tue Wed Fri Sun`, §9) → succeeds, produces the expected 2 KEY + 2 EASY + 1 LONG dated week.
- A 5-day preferred-day set with **no** legal assignment under K3-strength spacing is not applicable (K3 was rejected); confirm instead that every one of the 5 representative §9 patterns succeeds under the approved K2 rule, and that a deliberately pathological set (e.g. 5 mutually-adjacent weekdays with LONG placed to minimize remaining KEY-eligible dates) still succeeds or fails deterministically and typed, never a 500.
- Lane0/Lane1 identity preserved regardless of which one lands on the chronologically earlier date (a real test should construct a case where the search assigns lane1 the earlier date and confirm `LaneOrdinal` on the persisted/bound session is unaffected by calendar order).
- A Taper two-KEY week (both `PROTECTED`/`FIXED_EXPOSURE`) — confirm K2 spacing is enforced identically to Foundation/Build/Race-Specific.
- All four supported Core horizons (8/10/12/14 weeks) — confirm no horizon-specific exception is needed (§21 below).
- Legacy 3D/4D/Beginner×4D zero-delta — the generalized guard (`keyCount >= 1` in place of `keyCount != 1`, `DaysPerWeek is not (3 or 4 or 5)` in place of `is not (3 or 4)`) must reduce to byte-identical behavior for `keyCount == 1`, proven by full regression, not merely asserted.
- Real public 5D preview retry — once implemented, re-run this session's own `Gen5DIntermediatePublicActivationTests.cs`-style E2E suite (deleted in `FREQ.6D.4D.5` §12 after the routing revert; should be rewritten fresh against the real implementation, not resurrected from git history unmodified) to confirm the original 500 no longer occurs and a real 200 with correct role/date structure is returned.

## 21. Remaining blockers

- `CatalogWeekSkeletonCalendarMaterializer` itself remains unmodified (by this phase's own explicit instruction) — the real implementation work (§19) is not done.
- Preparation Runway (15-20w) and Long-Horizon (21-52w) 5D activation remain untouched — both are structurally coupled to a hardcoded 4-slot weekly shape one layer beyond this phase's scope (disclosed in `FREQ.6D.4D.5` §9, not re-litigated here).
- No horizon-specific exception was found necessary (§20 of the originating prompt) — the spacing rule is week-local and applies identically at every supported Core horizon (8/10/12/14 weeks); no STOP condition triggered here.
- `TEN_K__5D__INTERMEDIATE` remains fully non-public. This phase does not and must not change that.

## 22. Final classification

**`INTERMEDIATE_5D_KEY_SESSION_CALENDAR_SEPARATION_APPROVED`**

`MinimumKeySessionToKeySessionSeparationDays = 2` (calendar-date difference, `Math.Abs(...) >= 2`) is approved as the frozen product rule for Intermediate×5D KEY↔KEY spacing, confirming the value `FREQ.4` had already embedded as a disclosed, not-yet-evidenced placeholder in `DatedGeneratedCatalogPlanSkeletonValidator`. The decision is supported convergently by: real recovery-physiology convention (48-72h, this value sitting at the lower bound), real external 5D/4D training-plan pattern evidence (matching one plan exactly, exceeded by the other two, never fallen short of), and the existing repository's own already-real, already-tested KEY↔LONG rule of the identical magnitude. A genuine combinatorial counterexample was found and used to actively reject the stricter K3 alternative (`>= 3`), not merely to justify K2 by default. The rule is stated generically (any-pair, not 5D-specific) and is phase-invariant (including Taper). A deterministic tie-break for lane-to-date assignment was found to already exist in the codebase and requires no new decision. The next phase must implement `CatalogWeekSkeletonCalendarMaterializer`'s multi-slot generalization per the contract in §19 before public activation of `TEN_K__5D__INTERMEDIATE` can be re-attempted; this phase does not perform that implementation and does not re-enable routing.
