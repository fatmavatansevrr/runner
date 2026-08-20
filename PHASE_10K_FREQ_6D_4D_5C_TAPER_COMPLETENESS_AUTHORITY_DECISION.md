# Phase 10K-FREQ.6D.4D.5C — Intermediate×5D Taper Completeness Semantic Authority & Validator Generalization Decision

**Evidence + architecture-decision phase only. No production code touched. `CatalogPrescriptionContextValidator` unmodified. No stage renaming. No progression/profile/dosage change. No public activation. No routing change.**

## 1. Preflight

`PHASE_LEDGER.md` row 79: `FREQ.6D.4D.5B`, `IMPLEMENTATION`, `DONE (PARTIAL)`, `FREQ6D4D5B_MULTI_KEY_CALENDAR_MATERIALIZER_IMPLEMENTED`, confirmed. `PHASE_10K_FREQ_6D_4D_5A_KEY_SESSION_CALENDAR_SEPARATION_DECISION.md`, `PHASE_10K_FREQ_6D_4D_5B_MULTI_KEY_CALENDAR_MATERIALIZATION_IMPLEMENTATION.md`, and `PHASE_10K_FREQ_6D_4D_5_REAL_5D_CATALOG_AND_RUNTIME_BUNDLE_DISCOVERY_PARTIAL_IMPLEMENTATION.md` (the Split-E report that first attempted public activation) all read in full. Commits `8330b69`, `5fe82d6`, `1833d15` confirmed reachable from `HEAD` (`git merge-base --is-ancestor`). Confirmed from `FREQ.6D.4D.5B`'s own report: multi-KEY calendar materialization implemented and proven against real 8/10/12/14-week dark 5D calendars; calendar is not the current blocker; the routing widening attempted in 5B was reverted; the new, exact blocker is `CatalogPrescriptionContextValidator`'s hardcoded `TAPER_SHARPEN` stage-key check; `TEN_K__5D__INTERMEDIATE` remains dark.

Durable baseline: local `HEAD` = `1833d15`, `origin/main` = `13594ac` (ahead `5`/behind `0` — 5A, 5B, and their governance commits not yet pushed, consistent with `FREQ.6D.4D.5B`'s own disclosed push-gate deferral), working tree clean except the two pre-existing, unrelated `plan-catalog/artifacts/audits/*` files and `baseline_tmp`, `git diff --check` clean.

No pre-assigned phase ID existed in `MASTER_ROADMAP.md`'s roadmap sequence for this question; this phase uses `FREQ.6D.4D.5C`, continuing the established `<parent>.<letter>` sub-split convention (`5A`, `5B`, now `5C`).

## 2. Current blocker (from FREQ.6D.4D.5B)

`CatalogPrescriptionContextValidator.Validate` (in `CatalogPrescriptionContextBuilder.cs`) unconditionally requires at least one session matching the exact identity `PhaseKey=TAPER, ProgressionStageKey=TAPER_SHARPEN, StructuralRole=KEY_SESSION, WorkoutDefinitionKey=EASY_STANDARD`. The real 5D dual-lane progression's Taper stages are named `TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE` and use real catalog workouts (`GOAL_PACE_TEN_K`, `FARTLEK`), never `EASY_STANDARD` — so this check fails closed for every 5D request whose plan includes a Taper phase (effectively all supported 8-14 week horizons).

## 3. Exact failure trace (`PUBLIC_5D_TAPER_CONTEXT_FAILURE_TRACE`)

```
Request: level=Intermediate, daysPerWeek=5, weeks=12
  → V1CatalogPilotIdentityPolicy.ResolveCandidate(Intermediate, 5) [only when widened, per FREQ.6D.4D.5B's revert]
      → TEN_K__5D__INTERMEDIATE v1
  → CatalogPlanSkeletonOrchestrator + ProgressionStageAllocator
      → real dual-lane stage schedule: TAPER phase weeks bind
        lane0 -> TAPER_PRIMARY_STAGE, lane1 -> TAPER_SECONDARY_STAGE
  → CatalogWeekSkeletonCalendarMaterializer (FREQ.6D.4D.5B, now generalized)
      → succeeds: 2 distinct KEY_SESSION dates, >=2 days apart, both >=2
        days from LONG_RUN -- calendar is NOT the failure point (confirmed
        by FREQ.6D.4D.5B's own real dark-plan tests, unmodified here)
  → CatalogWorkoutBinder
      → binds lane0 KEY_SESSION to GOAL_PACE_TEN_K v3 (ProfileBacked:
        PrescriptionProfileKey=INTERMEDIATE_5D_TAPER_PRIMARY, Version=1)
      → binds lane1 KEY_SESSION to FARTLEK v5 (ProfileBacked:
        PrescriptionProfileKey=INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED, Version=1)
      → succeeds -- BoundCatalogSession.PrescriptionProfileKey/Version
        set together for both lanes, exactly as Split B's binder design requires
  → CatalogPrescriptionContextBuilder.Build
      → BuildSessionContext maps each BoundCatalogSession to a
        CatalogSessionPrescriptionContext -- carries PhaseKey,
        ProgressionStageKey, StructuralRole, WorkoutDefinitionKey, but
        does NOT currently carry PrescriptionProfileKey/Version at all
        (confirmed by direct read of PrescriptionContracts.cs:164-182 --
        the field simply isn't threaded through, even though
        BoundCatalogSession already has it)
      → CatalogPrescriptionContextValidator.Validate(boundPlan, sessions, input, ownership)
          → sessions.Any(s => s.PhaseKey=="TAPER" && s.ProgressionStageKey=="TAPER_SHARPEN"
              && s.StructuralRole=="KEY_SESSION" && s.WorkoutDefinitionKey=="EASY_STANDARD")
          → FALSE for the real 5D plan (ProgressionStageKey is
            TAPER_PRIMARY_STAGE/TAPER_SECONDARY_STAGE; WorkoutDefinitionKey
            is GOAL_PACE_TEN_K/FARTLEK, never EASY_STANDARD)
          → errors.Add("TAPER_SHARPEN_CONTEXT_MISSING")
      → CatalogPrescriptionValidationResult.IsValid = false
  → CatalogPreviewGenerator throws CatalogWorkoutBindingPlanInvalidException
    ("Prescription context validation failed: TAPER_SHARPEN_CONTEXT_MISSING")
  → wrapped as PlanPreviewGenerationFailedException (CATALOG_INTERNAL_WORKOUT_BINDING_FAILED)
  → HTTP 500
```

Confirmed by a real HTTP request against a real `CatalogPublisher`-produced test release, not static analysis (`FREQ.6D.4D.5B` §21).

**Real root data reaching the validator**: the validator has full, correct, real `BoundCatalogSession` data for both lanes by the time it runs — the failure is not a missing-data problem, it's a check that encodes an identity assumption (`EASY_STANDARD`/`TAPER_SHARPEN`) specific to one prescription mechanism that the real 5D plan never uses.

## 4. Validator provenance

`CatalogPrescriptionContextValidator`'s `TAPER_SHARPEN_CONTEXT_MISSING` check was introduced with `CatalogPrescriptionContextBuilder.cs` itself (Phase 4F.7A, the "Prescription Input And Rule Contract" phase — confirmed via `PHASE4F_7A_PRESCRIPTION_INPUT_AND_RULE_CONTRACT.md`'s mention of `TAPER_SHARPEN`) and is directly downstream of `PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md` — read in full, the actual origin of the qualifying identity. That report defines `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1`, whose **qualifying identity is exactly** the four-field tuple the validator checks for. This is not a generic completeness marker — it is the real, deliberate, narrow **prescription-content policy** for how the legacy runtime pipeline injects a "controlled sharpening" dose (a synthetic `CONTROLLED_SHARPENING` runtime component, 20% of assigned distance, clamped 0.5-1.5km) into an otherwise-plain `EASY_STANDARD` session, because at the time (single-KEY 3D/4D pilot, pre-`FREQ.6D` catalog architecture) no real catalog-authored "taper workout" existed — 4F.7D's own capability assessment classifies this explicitly as working around a real catalog gap (`SUPPORTED_BY_ADDITIVE_RUNTIME_PRESCRIPTION_CONTRACT`, not a catalog-native mechanism).

**Classification: `PILOT-SPECIFIC IMPLEMENTATION`** (not `CANONICAL_DOMAIN_AUTHORITY`). The stage key `TAPER_SHARPEN` was never declared canonical domain vocabulary anywhere — it is the specific stage name the 3D/4D/Beginner×4D progressions happen to use (confirmed: present verbatim in `ten-k-workout-progression.v1.json` through `.v5.json`), chosen because it was the only stage the legacy runtime-injection policy needed to recognize, not because the domain requires every Taper stage everywhere to be named that.

## 5. Taper stage inventory (`TAPER_STAGE_INVENTORY`)

| Progression | Phase | LaneOrdinal | Stage key | Structural role | Profile candidate | Dose category | WorkoutDefinition | Exposure semantics |
|---|---|---|---|---|---|---|---|---|
| Intermediate×3D (`.v1`-`.v5`) | TAPER | 0 (implicit, single lane) | `TAPER_SHARPEN` | KEY_SESSION | none (Legacy) | n/a | `EASY_STANDARD` | Runtime-injected controlled-sharpening component on top of an easy-baseline session |
| Intermediate×4D (`.v1`-`.v5`) | TAPER | 0 (implicit) | `TAPER_SHARPEN` | KEY_SESSION | none (Legacy) | n/a | `EASY_STANDARD` | Same runtime mechanism as 3D |
| Beginner×4D | TAPER | 0 (implicit) | `TAPER_SHARPEN` | KEY_SESSION | none (Legacy) | n/a | `EASY_STANDARD` | Same runtime mechanism |
| Intermediate×5D (`.v6`, real, `FREQ.6D.4D.5`) | TAPER | 0 | `TAPER_PRIMARY_STAGE` | KEY_SESSION | `INTERMEDIATE_5D_TAPER_PRIMARY` v1 | `PRIMARY` | `GOAL_PACE_TEN_K` v3 | Real, catalog-authored, ProfileBacked; `PROTECTED`/`FIXED_EXPOSURE` (min1/max2) |
| Intermediate×5D (`.v6`) | TAPER | 1 | `TAPER_SECONDARY_STAGE` | KEY_SESSION | `INTERMEDIATE_5D_TAPER_SECONDARY_CONTROLLED` v1 | `SECONDARY_CONTROLLED` | `FARTLEK` v5 | Real, catalog-authored, ProfileBacked; `PROTECTED`/`FIXED_EXPOSURE` (min1/max2) |

The 3D/4D/Beginner×4D rows are **Legacy** (no `PrescriptionProfileCandidateKeys` on the stage — `CatalogWorkoutBinder` never sets `PrescriptionProfileKey`). The 5D rows are **ProfileBacked** (exactly one `PrescriptionProfileCandidateKeys` entry each, per `FREQ.6D.4D.5`'s own stage authoring — confirmed by direct read of `ten-k-workout-progression.v6.json`).

## 6-13. Taper semantics per frequency

**3D/4D/Beginner×4D (§10-12)**: Taper contains exactly one structural `KEY_SESSION` slot; that slot's prescription is entirely the legacy runtime-injection mechanism (§4). "Complete" for these candidates has only ever meant "the one KEY_SESSION slot resolved to the `TAPER_SHARPEN`/`EASY_STANDARD` identity" — there is no dual-lane concept here at all.

**Real 5D (§13)**: two structural `KEY_SESSION` slots (`LaneOrdinal` 0/1), each independently `ProfileBacked` against a real, distinct, catalog-authored `PrescriptionProfile` (confirmed by direct inspection of the authored profile documents, not inferred from stage names alone — both `INTERMEDIATE_5D_TAPER_PRIMARY`/`_SECONDARY_CONTROLLED` carry real `WorkoutDefinitionRef`s, real components, and the `PROTECTED`/`FIXED_EXPOSURE` dose-category semantics `FREQ.6D.4D` architecture already froze for Taper specifically). **Both `TAPER_PRIMARY_STAGE` and `TAPER_SECONDARY_STAGE` are genuine Taper sharpening exposures** — not by name, but by authored content: both are real KEY_SESSION-role, catalog-bound, ProfileBacked, dose-protected Taper prescriptions, structurally and semantically equivalent in kind to what `TAPER_SHARPEN` represents for 4D, just delivered through the newer, real ProfileBacked mechanism instead of the legacy runtime-injection workaround.

## 14-19. Candidate models T1-T6

| Model | Evaluation |
|---|---|
| **T1 — Phase presence only** | Too weak, confirmed: would accept a 5D plan with only Lane0's Taper prescription present and Lane1 entirely missing — a real, silent completeness gap. Rejected. |
| **T2 — At least one Taper KEY present** | Same weakness as T1 for multi-lane layouts — "at least one of N required lanes" is not completeness for N>1. Sufficient only for the single-lane 3D/4D/Beginner×4D case (where it's degenerate-equivalent to "the one lane is present"). Insufficient alone for 5D. |
| **T3 — RunLayout KEY-lane cardinality coverage** | The right *shape* of invariant (every structural KEY lane must have a valid Taper prescription), but building it as a **new** validator mechanism here would duplicate an authority that already exists **downstream and stronger** — see §21/§23 below. Not rejected, but not built as new code; achieved by recognizing existing machinery instead (§21). |
| **T4 — DoseCategory coverage (Primary/SecondaryControlled)** | Wrong axis, per the originating prompt's own §13 warning: `DoseCategory` is prescription *content* semantics (how much/what kind of stimulus), not structural cardinality authority. A plan could have the right DoseCategory labels present while still being missing a real structural lane, or vice versa on a mislabeled profile. Rejected as the primary authority; not needed as a supplement either — cardinality is already provable directly (§21). |
| **T5 — Explicit Taper-capability metadata** | Rejected: the model already expresses this invariant through existing Phase/Role/Lane + ProfileBacked-vs-Legacy relationships (confirmed in §21) — inventing a new `Purpose=TaperSharpening` marker would be a new mechanism duplicating information already derivable, violating the prompt's own §14 instruction not to invent metadata unless the current model genuinely cannot express the invariant. |
| **T6 — Terminal-stage semantics** | Investigated: "terminal" is not determined by any explicit stage metadata today (no `IsTerminal`/ordinal-position field on stage definitions) — it would have to be inferred from phase-boundary position, which is indirect and fragile. More importantly, Taper completeness was never actually about *terminality* — 4D's `TAPER_SHARPEN` is simply "the Taper KEY_SESSION," not "the last stage of anything." Rejected as a mischaracterization of the real invariant. |

## 16. Decision matrix (`TAPER_COMPLETENESS_AUTHORITY_MATRIX`)

| Model | Current-domain fidelity | 3D compat | 4D compat | 5D correctness | 6D/7D generalization | HM/Marathon generalization | Stage-name independence | Catalog determinism | Impl. complexity | Hidden-authority risk | Recommended? |
|---|---|---|---|---|---|---|---|---|---|---|---|
| T1 | Low | ✓ | ✓ | ✗ (too weak) | ✗ | ✗ | ✓ | ✓ | Minimal | Low | No |
| T2 | Low-Med | ✓ | ✓ | ✗ (too weak) | ✗ | ✗ | ✓ | ✓ | Minimal | Low | No |
| **T3 (via existing dual-authority recognition, §21)** | **High** | **✓** | **✓** | **✓** | **✓ (generic, RunLayout-derived)** | **✓ (no distance-specific naming)** | **✓** | **✓** | **Low (mostly deletion/scoping, one field threaded)** | **Low (uses already-proven Split-C guarantee)** | **Yes** |
| T4 | Low (wrong axis) | ✓ | ✓ | Partial/misleading | Weak | Weak | ✓ | Partial | Medium | Medium (conflates content with structure) | No |
| T5 | Medium | ✓ | ✓ | ✓ (if built) | ✓ (if built) | ✓ (if built) | ✓ | ✓ | High (new metadata + authoring) | Medium (new authority surface) | No (unjustified — T3 already achievable without it) |
| T6 | Low (mischaracterizes intent) | ✓ | ✓ | Uncertain | Uncertain | Uncertain | Depends | Weak | Medium | High | No |

## 15/20/21/22/23. Selected authority, cardinality, cases, layer

**Selected: T3-equivalent completeness, achieved by recognizing two already-existing, independent, non-overlapping authorities rather than building one new generic validator mechanism.**

**The real finding**: `CatalogPrescriptionContextValidator` runs *before* `CatalogWorkoutBinder`'s ProfileBacked/Legacy classification is threaded into the type it validates (`CatalogSessionPrescriptionContext` does not currently carry `PrescriptionProfileKey`/`Version`, even though `BoundCatalogSession` — its own upstream source — already does, confirmed §3). Once that gap is closed (additive wiring, not new metadata — the data already exists one struct away), the correct invariant separates cleanly along the Legacy/ProfileBacked axis, which is exactly the axis `FREQ.6D.4D` architecture already uses everywhere else in this pipeline (Split C, Split D):

- **For Legacy Taper `KEY_SESSION` instances** (3D/4D/Beginner×4D, unchanged): the existing `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY` identity check remains authoritative, unmodified, still real and correct — every such instance must independently match `ProgressionStageKey=="TAPER_SHARPEN" && WorkoutDefinitionKey=="EASY_STANDARD"` (generalized from "any" to "every," matching this program's own established generalization pattern from `FREQ.4`/`FREQ.6D.4D.5A`/`5B` — a no-op for the single-lane case).
- **For ProfileBacked Taper `KEY_SESSION` instances** (5D and any future ProfileBacked progression): completeness is **already proven, independently and per-instance, by the existing Split-C fail-closed guarantee** — `CatalogSessionPrescriptionPlanner`'s `CatalogSessionPrescriptionMissingExecutionPrescriptionException` (confirmed by direct code read: iterates every session in every week, throws immediately if *any* ProfileBacked session — Taper or otherwise — lacks a resolvable execution prescription; this is a **stronger** guarantee than "at least one somewhere," since it is evaluated for every instance, not just checked for existence of one). This context-builder-level check therefore does not need to (and structurally cannot correctly) re-prove completeness for ProfileBacked instances — that authority already lives downstream, at the correct, later pipeline layer that actually has the ExecutionPrescriptionIndex resolution result.

**Required cardinality rule**: not a new RunLayout-lane-enumeration mechanism — the two authorities above are each already cardinality-complete for their own domain (Legacy: "every Legacy Taper KEY instance" — degenerates to 1 for 3D/4D; ProfileBacked: "every ProfileBacked KEY instance, anywhere in the plan" — already covers both 5D lanes by construction of the existing per-session loop).

**Exactly-once vs. at-least-once (§33)**: for the current one-week Taper phase (10K's only real Taper shape today, §35), the distinction is moot — "every Legacy instance in the phase" and "the exactly-one Legacy instance in the one Taper week" coincide. The selected rule is stated as "every Legacy Taper KEY_SESSION instance across the phase," which remains correct if a future distance's Taper phase spans multiple weeks (it would then require every week's Legacy instances to individually match — the natural, non-guessed generalization, consistent with how every other per-instance rule in this engagement already works).

**Week-level vs. phase-level (§34)**: phase-level, matching how `PhaseKey == "TAPER"` is filtered today (no week-number literal anywhere in the existing check) — already correct, unchanged.

**Validation layer (§23)**: `CatalogPrescriptionContextValidator` remains correct for the Legacy check (it has all required information: `WorkoutDefinitionKey`/`ProgressionStageKey` are already bound by this point). For the ProfileBacked case, the *existing* Split-C layer (`CatalogSessionPrescriptionPlanner`) is confirmed the correct owner — not because the current implementation is "wrong" in the sense of misplaced, but because it is the earliest layer that actually has the ExecutionPrescriptionIndex resolution result available, and duplicating that check earlier (before resolution even happens) is structurally impossible to do correctly, only speculatively. This is defense-in-depth by construction (§24), not by duplicating identical logic: two different, independent guarantees, adjacent in the pipeline, covering two disjoint session populations.

## 24. Real valid 5D example (§25/§26 of the originating prompt — combined here for clarity)

Under the selected rule, the real 5D plan (`TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE`, both ProfileBacked) is valid because: `legacyTaperKeySessions` is empty (both lanes are ProfileBacked) → the Legacy check is vacuously satisfied; `profileBackedTaperKeySessions` is non-empty (both lanes present) → the "some authority establishes completeness" condition is satisfied by the ProfileBacked branch; and downstream, `CatalogSessionPrescriptionPlanner` will independently re-confirm both lanes resolve real execution prescriptions before the request can ever succeed. Valid **because of real, present, dual ProfileBacked structural coverage**, not because of any stage name.

## 25. Real invalid 5D example (protects against "generalization" silently becoming "removal")

Construct a hypothetical malformed 5D progression stage where `TAPER_SECONDARY_STAGE` authors zero `PrescriptionProfileCandidateKeys` (making it Legacy) but its `WorkoutDefinitionKey` resolves to `FARTLEK` (not `EASY_STANDARD`) and its `ProgressionStageKey` remains `TAPER_SECONDARY_STAGE` (not `TAPER_SHARPEN`). Under the selected rule: `legacyTaperKeySessions = [lane1]`, and `lane1.ProgressionStageKey != "TAPER_SHARPEN"` → the "every Legacy instance must match" condition fails → `TAPER_SHARPEN_CONTEXT_MISSING` is correctly still raised. This is a real, deliberately-constructed counterexample proving the generalization does not collapse into "accept anything" — a malformed Legacy-classified Taper stage is still rejected exactly as before.

## 19/20. Legacy compatibility (re-confirmed)

3D/4D/Beginner×4D plans have zero ProfileBacked Taper instances (no `PrescriptionProfileCandidateKeys` authored on any of their Taper stages) — every one of their Taper `KEY_SESSION` instances is Legacy and must independently match `TAPER_SHARPEN`/`EASY_STANDARD`, which they do by construction (the only stage they author). No existing dedicated unit test for `CatalogPrescriptionContextValidator` was found (confirmed by repository-wide search — it is exercised only indirectly, through full end-to-end preview tests for 3D/4D/Beginner×4D that happen to pass because those real plans always include the correct identity); the implementation phase must add direct positive/negative unit coverage (§28 test manifest), since none currently exists to regress-guard.

## 30/31. Cross-frequency and cross-distance generalization

The selected rule is stated generically: "every **Legacy** Taper KEY_SESSION instance must match the legacy identity; every **ProfileBacked** Taper KEY_SESSION instance is covered by the existing Split-C execution-resolution guarantee" — no `if RunsPerWeek == 5` branch anywhere, and no distance-specific string taxonomy. A hypothetical 6D/7D layout with 3 KEY lanes would work identically (3 ProfileBacked instances, all covered by the same downstream per-session guarantee, zero new code). A hypothetical Half-Marathon/Marathon Taper — even a multi-week one — works identically as long as it follows the same Legacy/ProfileBacked classification already used everywhere else in this architecture; no distance-specific stage-name convention is required or assumed.

## 32. Literal-stage-key disposition (§27 of the originating prompt)

`ProgressionStageKey` remains exact lineage — read, persisted, and replayed unchanged everywhere it already is (Split A/D). It is **not** the authority for Taper completeness under the selected rule (it is one of the two fields the Legacy-branch check still reads, but only within the already-narrow Legacy/`TAPER_SHARPEN` content-policy check, not as a general completeness flag). This phase does not imply stage keys are semantically unimportant — they remain the deterministic-generation/replay lineage identity they have always been; they are simply confirmed **not** to be the correct authority for the specific question "is this plan's Taper prescription context complete."

## 33. Implementation contract (for the next, separate implementation phase)

- Thread `BoundCatalogSession.PrescriptionProfileKey` (and, for completeness, `PrescriptionProfileVersion`) into `CatalogSessionPrescriptionContext` in `BuildSessionContext` — purely additive, no existing field removed or renamed.
- In `CatalogPrescriptionContextValidator.Validate`, replace the single `sessions.Any(...)` check with: partition `sessions.Where(s => s.PhaseKey=="TAPER" && s.StructuralRole=="KEY_SESSION")` into Legacy (`PrescriptionProfileKey is null`) and ProfileBacked (`PrescriptionProfileKey is not null`) subsets; require every Legacy instance to independently match `ProgressionStageKey=="TAPER_SHARPEN" && WorkoutDefinitionKey=="EASY_STANDARD"`; require at least one of (any ProfileBacked instance present) or (the Legacy check already satisfied) — i.e. `TAPER_SHARPEN_CONTEXT_MISSING` fires only when there is genuinely no Taper-completeness authority present at all (no valid Legacy instance and no ProfileBacked instance).
- Do not alter `ClassifyTaperSharpenCapability` (a separate, still-Legacy-specific classification of `EASY_STANDARD`'s own catalog shape — unrelated to this fix, out of scope, §28 of the originating prompt's "no Taper dosage reopen").
- Do not touch `CatalogSessionPrescriptionPlanner`/Split-C's existing exception — it is reused as-is, not modified.
- Do not touch any progression/profile/calendar content, per this phase's own explicit prohibition.

## 34. Public retry contract

Once the validator change above is implemented and its own new unit tests plus the full regression suite are green, retry the exact same `V1CatalogPilotIdentityPolicy` widening already attempted twice (Split E, `FREQ.6D.4D.5B`) — do not redesign routing. Required: real public 5D preview succeeds → prescription-context validation passes → the full ProfileBacked execution path resolves both lanes → DB confirmation persists both lanes' distinct profile lineage. If a third independent blocker appears, the next phase must disclose it and keep public activation dark, per this program's own established discipline — not accumulate compatibility hacks.

## 35. Test manifest (for the next, separate implementation phase)

1. Historical single-lane `TAPER_SHARPEN`/`EASY_STANDARD` (Legacy) case — valid.
2. Real Intermediate×3D full plan — valid (zero delta).
3. Real Intermediate×4D full plan — valid (zero delta).
4. Real Beginner×4D full plan — valid (zero delta).
5. Real 5D `TAPER_PRIMARY_STAGE` (lane0) alone present, ProfileBacked — contributes to validity.
6. Real 5D `TAPER_SECONDARY_STAGE` (lane1) alone present, ProfileBacked — contributes to validity.
7. Full real 5D Taper (both lanes, ProfileBacked) — valid.
8. 5D with lane0 present, lane1 entirely missing from the bound plan — fails (caught by the existing structural/role-cardinality validator upstream, not this one — confirm which layer actually rejects it and assert accordingly, not assumed).
9. 5D with lane1 present, lane0 missing — same as #8, mirrored.
10. Malformed Legacy-classified Taper stage with a non-`TAPER_SHARPEN` `ProgressionStageKey` and non-`EASY_STANDARD` `WorkoutDefinitionKey` (§25's constructed counterexample) — fails, `TAPER_SHARPEN_CONTEXT_MISSING`.
11. A KEY_SESSION in a non-TAPER phase using the `TAPER_SHARPEN`/`EASY_STANDARD` identity does not satisfy Taper completeness (phase filter still applies).
12. An arbitrary stage literally named `TAPER_SHARPEN` but classified ProfileBacked (hypothetical) is judged by the ProfileBacked branch, not the Legacy identity match — proving naming alone is not authoritative post-fix.
13. A renamed-but-structurally-identical 5D stage (e.g. hypothetically `TAPER_LANE_A`/`TAPER_LANE_B`) remains valid — proving the rule is name-independent, not merely re-testing the same two real names.
14. ProfileBacked completeness genuinely requires the downstream Split-C exact execution-prescription resolution, not merely `PrescriptionProfileKey` presence at the context-builder stage (construct a case where the key is present but the published bundle lacks the profile, and confirm the *downstream* exception fires, not a false-positive pass at this layer).
15-18. Real dark 8/10/12/14-week 5D candidates pass full prescription-context validation (reusing `FREQ.6D.4D.5B`'s real dark-plan test pattern, extended past the calendar layer through prescription-context building).
19. Public preview succeeds for a real, eligible 5D request (after the routing retry).
20. Public confirmation persists correctly.
21. No silent 4D fallback (candidate identity assertions, matching this program's existing convention).
22. Full legacy regression — zero delta across 3D/4D/Beginner×4D.

## 36. Technical-debt disposition

No pre-existing debt record referencing `TAPER_SHARPEN`/stage-key coupling was found (repository-wide search across every `*.md` phase report and both `PHASE_LEDGER.md`/technical-debt-style documents). This phase's own finding is the first formal disposition: the `TAPER_SHARPEN` identity check was `PILOT-SPECIFIC IMPLEMENTATION` (§4), not `CANONICAL_DOMAIN_AUTHORITY` — no new debt ticket is created; the finding and its resolution are fully captured by this report and the implementation contract above.

## 37. Remaining blockers

- The validator change itself is not implemented (this phase is decision-only, per its own explicit prohibition).
- Public activation remains dark, unchanged, per this phase's own explicit instruction (§30 of the originating prompt).
- Once implemented, a third public-activation attempt could still surface a further, independent blocker (not predicted here, not assumed absent) — the next phase must disclose honestly, not force closure, consistent with this program's established discipline through `FREQ.6D.4D.5`/`5B`.

## 38. Final classification

**`TAPER_COMPLETENESS_EXISTING_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`**

The real semantic authority for ProfileBacked Taper completeness already exists and is already proven correct — `CatalogSessionPrescriptionPlanner`'s Split-C fail-closed per-session execution-resolution guarantee, which independently covers every KEY lane (including both real 5D lanes) without any new mechanism. `CatalogPrescriptionContextValidator`'s defect was never an under-generalized structural check needing a brand-new RunLayout-cardinality validator (T3 as originally posed) — it was a narrow, deliberately-scoped, `PILOT-SPECIFIC` legacy prescription-content-policy check (`V1_TAPER_SHARPEN_PRESCRIPTION_POLICY`, real and still correct for 3D/4D/Beginner×4D) that was applied unconditionally instead of being scoped to the Legacy population it was actually designed for. The fix is additive and surgical: thread the already-existing `PrescriptionProfileKey` classification one struct further, and partition the completeness check along the Legacy/ProfileBacked axis this architecture already uses everywhere else — no new metadata, no stage renaming, no string-exception list, and full generalization to 6D/7D/HM/Marathon by construction. `TEN_K__5D__INTERMEDIATE` remains fully dark; the next phase must implement the narrow validator change per §33 and re-attempt public activation per §34.
