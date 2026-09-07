# PHASE HM.0 — HALF-MARATHON DISTANCE GENERALIZATION REUSE AUDIT

**Type:** EVIDENCE / ARCHITECTURE_DESIGN (audit only — no production code changed)
**Governing prompt:** `APPSEL_HM_PHASE_0_DISTANCE_GENERALIZATION_AUDIT.md`
**Parent:** `GEN.39` (`TEN_K_V1_CAPABILITY_COMPLETE`), read as frozen evidence
**Scope discipline:** static analysis of real repository code (`backend/RunningApp.Application/RuntimeCatalog/**`, plus `Services/`, `Common/`, `Domain/Entities`), no catalog artifacts added, no numeric values invented, no `OPEN-HM-01..07` decided, no 10K behavior changed.

> **CORRECTIVE-PASS NOTICE (`HM.0.1`, applied in place to this same document — see §0.1):** the original version of this report (committed at `6f5fae5`/`910b6d8`) claimed in §I/§J that no document enumerating `OPEN-HM-01` through `OPEN-HM-07` could be located anywhere in the repository. That claim was **false**: `HM_21K_Claude_Handoff_and_Build_Plan.md`, sitting untracked in the repository root the entire time this audit ran, defines all seven items explicitly (its own §6, lines ~709–802) and was missed because the original search scope covered only git-tracked content. The document is now tracked (this corrective pass adds it) and §I/§J below have been rewritten against it. §B/§C carry a corrective-pass cross-check note confirming consistency with the document's own "Target architecture for HM" section. §A/§D/§E/§F/§G/§H/§K/§L/§M/§N and the final classification in §13 were independently code-grounded and are **unchanged** — the cross-check in §B/§C found no discrepancy requiring their correction. See the new §Q for the methodological lesson this correction establishes.

---

## 0. Startup confirmation

- `PHASE_LEDGER.md` (248 lines, 141 rows) and `MASTER_ROADMAP.md` (897 lines) read in full.
- `git fetch` clean; `git rev-list --left-right --count origin/main...HEAD` = `0 0` before starting. Worktree changes limited to the pre-existing tracked `bin`/`obj` rebuild noise called out in the task brief — left untouched, not staged.
- `git log -5`: `2933bea` docs(gen-25) SHA backfill, `f3c3677` GEN.25, `2e512d4` docs(gen-24) SHA backfill, `c57d774` GEN.24, `d7487d8` docs(gen-23) — consistent with the ledger's own tail.
- **Frozen-evidence check on the 10K matrix**: `GEN.39` (ledger row 142, `TEN_K_V1_CAPABILITY_COMPLETE`) states it re-verified the full 10K Level×Frequency×Horizon matrix directly against live gate code (`V1CatalogPilotIdentityPolicy`, `LongHorizonPublicPlanService.ValidatePilot`) and found every cell matching target, full regression `4382/4386` (3 named pre-existing baseline failures + a date-coincidence flake, both disclosed, zero new regressions), `PlanCatalog.Tests` `1510/1510`. **"Frozen" here means**: the ledger records this as a verified, closed state as of commit `958b54a` — it does not mean the underlying code is literally immutable, only that no further 10K product/numeric decision is open and no phase may casually alter its behavior. This audit reads that state as given, does not re-run the full regression (per the governing file's own §11/§14 instruction — static analysis only), and touches zero files under 10K's own reachable paths.
- **Phase-ID convention**: `MASTER_ROADMAP.md` §3 defines `WAVE B — Half Marathon completion` as a distinct engagement layered after the closed `WAVE A — 10K completion`, and §15's `WAVE B` bullet list opens with exactly "10K→HM reuse/gap audit" — this phase. The roadmap's own §13 forbids pre-authoring speculative IDs beyond the near-term block, and no `HM.*` row exists yet in `PHASE_LEDGER.md`. The governing prompt file itself refers to this phase as "PHASE HM.0" in its own title and eight further places, mirroring the pattern the 10K engagement used when it opened a new sub-arc under its own name (`GEN.*` for the main arc, `FREQ.*` for the frequency-focused sub-arc, `APPSEL-BACKEND.GOV.*` for governance). Given (a) HM is explicitly a new, separate engagement layered on the closed 10K one, (b) the governing prompt's own self-reference, and (c) the roadmap's requirement to keep future distances "clearly delineated as a new, separate axis" — **this phase is ledgered as `HM.0`**, establishing a new namespace for the Half-Marathon engagement exactly as `GEN.*` did for 10K. This is a judgment call, not a discovery of an existing convention, and is documented here per the governing prompt's own instruction to reason it out rather than guess silently.

---

## 0.1 Corrective-pass scope (`HM.0.1`, applied in place to this document)

A real gap was found after `HM.0`'s original commit: `HM_21K_Claude_Handoff_and_Build_Plan.md` was present at the repository root, with a file mtime (`17:48:53`) **before** `HM.0`'s own commits (`18:34:21`/`18:34:36`) — meaning it sat in the working directory for the entire original audit run — but was untracked in git (`git status` showed `??`). `HM.0`'s original search, scoped to tracked repository content, missed it, and its §J incorrectly concluded that no document enumerating `OPEN-HM-01` through `OPEN-HM-07` existed anywhere. In fact this document's own §6 defines all seven items explicitly, gives a target architecture (§2), lists existing HM research candidates (§4: core-horizon, peak-volume bands, starting-long-run caps, readiness/short-horizon routing, pace-evidence hierarchy) and closed research conclusions (§5, `HM-LIT-15`–`HM-LIT-27`), and lays out a recommended build sequence (§7/§8).

This corrective pass:
1. Tracks the document in git (`git add HM_21K_Claude_Handoff_and_Build_Plan.md`) so it cannot be silently missed by a future phase's search again.
2. Re-reads it in full and re-classifies each `OPEN-HM-01`…`07` item individually against the governing prompt's A–F scheme (§I/§J below), rather than leaving them uniformly "still open."
3. Cross-checks `HM.0`'s original §B (architecture map) and §C (reachability trace) — both grounded in direct code reads — against the handoff document's own §2 "Target architecture for HM" (see the cross-check notes appended to §B and §C below).
4. Confirms the final classification in §13 either still holds or needs revision, on the corrected evidence.
5. Records the methodological lesson (§Q): search the full working directory, tracked and untracked, not only git-tracked content.

**What did NOT change**: §A, §D, §E, §F, §G, §H, §K, §L, §M, §N were all grounded in direct reads of the real repository's C# source, independently spot-checked by the coordinator at the time, and the §B/§C cross-check in this pass found no discrepancy between them and the handoff document's target architecture — so none of them are rewritten. Only §I, §J (rewritten), §B/§C (cross-check note appended), §O (risk #2 resolved), §P (updated to point at the now-tracked document), and §13 (corrective confirmation appended) are touched by this pass.

---

## A. Executive classification

**The Level×Frequency×Horizon architecture is real and substantially distance-generic at the framing layer** (identity resolution, horizon classification, candidate-summary data shape, day/calendar composition, workout-role binding, condition resolvers, persistence entities). Half Marathon can enter through the same pipeline shapes 10K uses today — no second generation engine is required.

**However, "distance-generic" is true of the *mechanism*, not of two specific, load-bearing layers that currently hardcode a single distance:**

1. **The public/internal identity-and-candidate-routing authority** (`V1CatalogPilotIdentityPolicy`) is not merely "10K-configured" — its routing methods (`ResolveCandidate`, `IsSupportedLevelFrequency`, `IsSupportedPreparationRunwayLevelFrequency`) have **no distance parameter in their signatures at all**. `GoalDistance` is checked once, as a single hardcoded constant, at the outer gate (`IsSupportedIdentity`) — it never participates in candidate-key routing. This is a structural gap, not a config value: adding a second distance requires widening the method signatures and every call site, not just adding constants.
2. **The numeric/progression authority** (`VolumeSafetyPolicy.*` and its per-cell siblings) is 10K training-science policy encoded directly in C# static classes, not catalog data. `CatalogVolumeAndLongRunPlanner`'s per-cell dispatch is exact-tuple-keyed and explicitly checks `CanonicalDistanceFamily == "TEN_K"` for most branches — but its **fallback case has no distance check at all** and silently reuses `VolumeSafetyPolicy.Default` (the TEN_K/Intermediate/4D authority). This is currently inert only because the upstream gate (finding 1) already rejects every non-TenK request before this code runs.

A further, well-evidenced **duplicate-authority finding**: `GoalDistance → km` mapping is owned in two places — `RunningApp.Application.Common.GoalDistanceKm.Resolve` (already fully generic, already includes `HalfMarathon => 21.0975`) and `CatalogGoalDistanceResolver` embedded in `CatalogPrescriptionContextBuilder.cs` (fail-closed to `TEN_K` only, duplicating the same concept narrowly). Neither authority is aware of the other.

**Crucially, the first requested HM slice — `HALF_MARATHON × INTERMEDIATE × 4D × 14-WEEK PREFERRED CORE × DARK` — is a Core-horizon-only request.** Preparation Runway (15-20wk composition) and LongHorizon (21-52wk rolling) are not reachable for a 14-week request under the existing `CoreHorizonClassifier` (day-accurate, purely a function of the catalog's own `CoreCycle` bounds — see §B/§C). This materially narrows the real blocking surface: the ~150 files under `Schedule/PreparationRunway*`, `Schedule/LongHorizon/**`, and the `TenKPreparationRunwayDarkOrchestrator` + its six `TenK*PolicyFactory` siblings are **not on the critical path for this specific slice** and require no change for it (though they remain hardcoded to TEN_K and will need their own dedicated audit before a future HM Runway/LongHorizon wave).

**Final classification (§13): `HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE`**, scoped explicitly to the Core horizon only, conditioned on:
- the identity/candidate-routing seam (`V1CatalogPilotIdentityPolicy`) being widened with a distance parameter (mechanical, no product decision),
- the duplicate `GoalDistance→km` authority being reconciled (mechanical — `HalfMarathon = 21.0975` is a physical constant, not a policy choice),
- a genuine `HM_NEW_DISTANCE_AUTHORITY_REQUIRED` numeric policy (peak-volume trajectory, starting-volume defaults, taper multiplier, long-run share) being frozen by product/evidence decision before any real (non-placeholder) number can flow — this is `OPEN_HM_PRODUCT_DECISION`, not an architecture blocker,
- an actual HM catalog artifact existing (explicitly out of scope for this phase; blocks *execution*, not the architecture verdict).

No unresolved duplicate authority blocks the *architecture*; the duplicate found (§H) has one clearly correct side to converge on. No second parallel generation engine is needed anywhere in the traced first-slice path.

---

## B. Real current architecture map (Core-horizon path, as traced from source)

```
Request (GoalType, GoalDistance, RunningBackground level, DaysPerWeek, dates, readiness…)
  │
  ▼
CanonicalDistanceFamilyResolver.Resolve(requestedDistanceKm)          [A — GENERIC]
  → km-banded switch: ≤5→FiveK, ≤10→TenK, ≤21.1→HalfMarathon, ≤42.2→Marathon
  → already produces GoalDistance.HalfMarathon for any 10.01–21.1km request today
  │
  ▼
V1CatalogPilotIdentityPolicy.IsSupportedIdentity(goalType, goalDistance, level, daysPerWeek)   [C — HARDCODED]
  → goalType == Race && goalDistance == GoalDistance.TenK (single const) && (level,days) in allow-list
  → HALF_MARATHON is rejected here, unconditionally, today
  │
  ▼
V1CatalogPilotIdentityPolicy.ResolveCandidate(level, daysPerWeek) → (CandidateKey, CandidateVersion)  [C — HARDCODED, STRUCTURAL]
  → method signature has NO distance parameter; routes purely on (level, daysPerWeek)
  → e.g. (Intermediate, 4) → ("TEN_K__4D__INTERMEDIATE", 10)
  │
  ▼
PlanCatalogBundleLoader / CatalogArtifactFileResolver → PlanCatalogCandidateSummary   [A — GENERIC]
  → loads by (CandidateKey, CandidateVersion) from the published bundle; CanonicalDistanceFamily
    is carried as a plain string field read from the artifact, not asserted against a hardcoded value here
  │
  ▼
CatalogPreviewGenerator.GeneratePreviewAsync (+ GeneratePreparationRunwayPreviewAsync for Runway only)
  │
  ▼
CoreHorizonClassifier.Classify(CoreHorizonContext{StartDate, RaceDate, Min/Preferred/MaxCoreWeeks})  [A — FULLY GENERIC]
  → day-accurate; bounds come from candidate.CoreCycle (catalog-declared), never hardcoded per distance
  → Mode ∈ {Unsupported, CompressedCore, PreferredCore, ExtendedCore, PreparationRunwayPlusCore, InvalidInput}
  │
  ▼ (Core-only path — PreparationRunwayPlusCore is the ONLY mode that reaches PreparationRunway/LongHorizon)
  │
CatalogPrescriptionContextBuilder → CatalogGoalDistanceResolver.Resolve(catalogDistanceFamily, requestGoalDistance)  [C — HARDCODED, fail-closed to TEN_K]
  │
  ▼
DynamicCoreWeekSkeletonOrchestrator → CatalogPlanSkeletonOrchestrator (phase allocation)          [mostly A, verify per-cell PhaseAllocation data is catalog-sourced]
  │
  ▼
DynamicCoreWorkoutBindingOrchestrator → CatalogWorkoutBinder / V1CatalogWorkoutRoleBindingPolicy   [A — GENERIC, role-keyed not distance-keyed]
  │
  ▼
DynamicCoreVolumeAndLongRunOrchestrator → CatalogVolumeAndLongRunPlanner.Build(request)            [B/C — MECHANISM GENERIC, POLICY DATA + fallback branch HARDCODED]
  → per-cell VolumeSafetyPolicy dispatch; several branches explicitly gate on CanonicalDistanceFamily=="TEN_K";
    the Intermediate/4D pilot cell itself falls through to NO explicit branch and uses whatever _policy the
    caller constructed with — VolumeSafetyPolicy.Default (the TEN_K/Intermediate/4D numbers)
  │
  ▼
DynamicCoreSessionPrescriptionOrchestrator (FourDaySessionDistanceAllocationPolicy, taper/sharpen policy)  [B — TEN_K session-shape policy, role-generic mechanism]
  │
  ▼
DynamicCoreCalendarMaterializationOrchestrator → CatalogWeekSkeletonCalendarMaterializer / CatalogPreferredDayAdapter  [A — GENERIC, no distance coupling found]
  │
  ▼
GeneratedCatalogPlanPayload / CatalogPublicPreviewMaterializer (dark preview shape)                 [needs distance-aware public workout-type mapping only at the PUBLIC layer — dark path unaffected]
  │
  ▼
(Confirm) CatalogPlanConfirmationService → PreparationRunwayPersistablePlanMapper / TrainingPlan entity   [A — GENERIC; CanonicalDistanceFamily and CatalogCandidateKey are free-form strings]
```

Preparation Runway (`TenKPreparationRunwayDarkOrchestrator` + its six `TenK*PolicyFactory` siblings) and the entire `LongHorizon/**` subtree sit **downstream of `CoreHorizonMode.PreparationRunwayPlusCore` only** — a 14-week request cannot reach that mode (see §C) and is excluded from this map's critical path.

**Corrective-pass cross-check (`HM.0.1`) against `HM_21K_Claude_Handoff_and_Build_Plan.md` §2 "Target architecture for HM":** the handoff document's own preferred composition —

```
HALF_MARATHON_MASTER
+ RUN_LAYOUT_{2D|3D|4D|5D|6D}
+ {BEGINNER|INTERMEDIATE|ADVANCED}_MODIFIER
+ HALF_MARATHON_WORKOUT_PROGRESSION
+ DISTANCE/FREQUENCY/LEVEL NUMERIC POLICIES
+ GENERIC RUNTIME RESOLVERS
```

— is **consistent** with the map above, derived independently from the real code. Each element maps cleanly onto something this map already found: `RUN_LAYOUT_{…}` and `GENERIC RUNTIME RESOLVERS` correspond to the `[A — GENERIC]`-tagged stages (workout binding, calendar materialization, pace/goal-feasibility resolvers — §D); `{LEVEL}_MODIFIER` and `DISTANCE/FREQUENCY/LEVEL NUMERIC POLICIES` correspond to the `VolumeSafetyPolicy`-shaped authority this map tags `[B/C]` at the volume/long-run stage; `HALF_MARATHON_WORKOUT_PROGRESSION` corresponds to the workout-catalog-content gap this map flags as `BLOCKED_BY_OPEN_DECISION` in §C step 11; and `HALF_MARATHON_MASTER` corresponds to the new candidate identity plus `CoreCycle`/phase-allocation authority this map flags at §C steps 4/7/8. The handoff's own §3 "Reuse the mechanism, not necessarily the numeric data" list (peak-volume bands, starting-volume caps, progression coefficients, session-volume allocation, long-run share/cap, workout eligibility/dose, phase allocation, taper allocation, horizon thresholds) matches this map's Classification-B findings (§E) item for item. **One nuance, not a conflict**: the handoff's §3 "Generic structural/runtime mechanisms" list includes "Level/Frequency compatibility and public rollout gate mechanism" as reusable. This map agrees the *gate mechanism/shape* (an allow-listed level×frequency combination check) is conceptually reusable, but its concrete current implementation, `V1CatalogPilotIdentityPolicy`, has no distance parameter in its method signatures at all (§F.1) — a mechanical widening is required before that reuse is literal, not merely conceptual. This refines rather than contradicts the handoff's characterization, and does not change any finding in this map. **No discrepancy was found that requires correcting any part of this section.**

---

## C. First-slice reachability trace — `HALF_MARATHON × INTERMEDIATE × 4D × 14W PREFERRED CORE × DARK`

| # | Stage | Real component | Classification |
|---|---|---|---|
| 1 | Distance identity | `CanonicalDistanceFamilyResolver.Resolve` | `REUSED_AS_IS` — already resolves 10.01–21.1km to `GoalDistance.HalfMarathon` |
| 2 | Public/dark identity gate | `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` | `NEEDS_DISTANCE_PARAMETER` — hardcoded `GoalDistance == TenK` const |
| 3 | Candidate identity resolution | `V1CatalogPilotIdentityPolicy.ResolveCandidate` | `NEEDS_DISTANCE_PARAMETER` — method has no distance parameter; needs a `(GoalDistance, Level, DaysPerWeek)` → candidate map |
| 4 | Candidate summary load | `PlanCatalogBundleLoader`/`CatalogArtifactFileResolver` | `BLOCKED_BY_OPEN_DECISION` — mechanically `REUSED_AS_IS`, but no `HALF_MARATHON__4D__INTERMEDIATE` artifact exists and this phase is barred from creating one |
| 5 | Combination compatibility | allow-list inside `V1CatalogPilotIdentityPolicy` | `NEEDS_DISTANCE_PARAMETER` |
| 6 | Core horizon classification | `CoreHorizonClassifier.Classify` | `REUSED_AS_IS` — fully generic given HM's own `CoreCycle` bounds |
| 7 | HM CoreCycle bounds (min/preferred=14/max) | none exist yet | `BLOCKED_BY_OPEN_DECISION` (`OPEN-HM`, phase/horizon authority) |
| 8 | Phase allocation | `CatalogPlanSkeletonOrchestrator` + candidate's own `PhaseAllocations` | `REUSED_AS_IS` mechanically; data is `BLOCKED_BY_OPEN_DECISION` (needs HM's own phase week-bounds authored in the (barred) catalog artifact) |
| 9 | Goal-distance/km cross-check | `CatalogGoalDistanceResolver` (`CatalogPrescriptionContextBuilder.cs`) | `NEEDS_DISTANCE_PARAMETER` — fail-closed switch, TEN_K only |
| 10 | Workout binding mechanism | `CatalogWorkoutBinder`/`V1CatalogWorkoutRoleBindingPolicy` | `REUSED_AS_IS` — role-keyed, not distance-keyed |
| 11 | Workout catalog identities (goal-pace, easy, long-run workouts) | catalog artifact content | `BLOCKED_BY_OPEN_DECISION` (barred catalog authoring + `OPEN-HM` workout capability) |
| 12 | Volume/long-run numeric dispatch mechanism | `CatalogVolumeAndLongRunPlanner.Build` | `REUSED_AS_IS` mechanically |
| 13 | Volume/long-run numeric authority (peak band, starting volume, taper, long-run share) | `VolumeSafetyPolicy.*` | `NEEDS_HM_AUTHORITY` — new typed policy required; values are `OPEN_HM_PRODUCT_DECISION` |
| 14 | Session-distance allocation (4D slot split) | `FourDaySessionDistanceAllocationPolicy`/`V1FourDaySessionVolumeAllocationPolicy` | `NEEDS_HM_AUTHORITY` if HM's 4D session-shape numeric split differs; mechanism `REUSED_AS_IS` |
| 15 | Pace-source resolution | `PaceSourceResolver` | `REUSED_AS_IS` — condition-based (recent race / target time / estimated), no distance literal found |
| 16 | Goal-feasibility handling | `GoalFeasibilityResolver` | `REUSED_AS_IS` — uses `RequestedTargetDistanceKm`/`GoalDistanceKm` generically |
| 17 | Calendar materialization / PreferredDays / LongRunDayPreference | `CatalogWeekSkeletonCalendarMaterializer`, `CatalogPreferredDayAdapter` | `REUSED_AS_IS` — no distance coupling found |
| 18 | Role/cardinality validation | `GeneratedCatalogPlanSkeletonValidator`, `DatedGeneratedCatalogPlanSkeletonValidator` | `REUSED_AS_IS` (light-touch verification — role/count checks are layout-shape-keyed, not distance-keyed, consistent with every other generic component in this path) |
| 19 | Adaptation | `Schedule/LongHorizon/Adaptation/**` | `REUSED_AS_IS` — zero `TEN_K`/`GoalDistance.TenK` hits found by repo-wide grep of that subtree; scope is Core-week/session-role based |
| 20 | Preview (dark materialization) | `CatalogPublicPreviewMaterializer`, `GeneratedCatalogPlanPayload` | `REUSED_AS_IS` for the dark shape; a public workout-type mapping gap only matters if/when HM is made public (out of scope, `DARK ONLY`) |
| 21 | Confirm / Persistence | `CatalogPlanConfirmationService`, `PreparationRunwayPersistablePlanMapper`, `TrainingPlan` entity | `REUSED_AS_IS` — `CanonicalDistanceFamily`/`CatalogCandidateKey` are free-form strings, no enum/constraint tying them to `TEN_K` |
| 22 | Public routing | N/A for this phase | `BLOCKED_BY_OPEN_DECISION` — explicitly out of scope (`DARK ONLY`) |

**Net reading**: of 22 traced steps, 12 are `REUSED_AS_IS`, 4 need a mechanical distance parameter (no product decision), 1 needs a new mechanism-preserving typed authority whose *values* are a product decision, and 5 are blocked purely by the phase's own hard boundaries (no catalog artifact, no invented numbers, no OPEN-HM resolution) rather than by any architecture defect.

**Corrective-pass cross-check (`HM.0.1`):** the handoff document's own §7/§8 (build strategy, WAVE HM-A through HM-I, recommended rollout order) independently arrives at the identical scoping this trace derived from code: WAVE HM-C names the first target as exactly `HALF_MARATHON × INTERMEDIATE × 4D × 14-WEEK PREFERRED CORE`, for the same reasons this audit's own §C row 6/7 identifies (14-week preferred core has the clearest candidate, 4D is architecturally the most mature path, Intermediate is the least edge-case-heavy level). The handoff also independently confirms Preparation Runway/LongHorizon are out of scope for this slice (WAVE HM-H: "Only after standalone HM Core is stable," listed after WAVE HM-C/D/E/F/G) — consistent with this trace's row 22 / §B's finding that a 14-week request cannot reach `CoreHorizonMode.PreparationRunwayPlusCore`. No discrepancy found; the two independently-derived scopings (one from code, one from the product document) agree exactly on what is and is not reachable for this slice.

---

## D. Reusable generic authorities/components (Classification A)

| Component | File | Evidence |
|---|---|---|
| `CanonicalDistanceFamilyResolver` | `RuntimeCatalog/CanonicalDistanceFamilyResolver.cs` | Pure km-banded switch already producing `HalfMarathon`/`Marathon`; no code change needed |
| `GoalDistanceKm.Resolve` | `Application/Common/GoalDistanceKm.cs` | Already includes `GoalDistance.HalfMarathon => 21.0975` |
| `CanonicalTargetFinishTimePolicy` | `Application/Common/CanonicalTargetFinishTimePolicy.cs` | Already includes a `HalfMarathonSeconds` branch |
| `CoreHorizonClassifier` | `RuntimeCatalog/Schedule/Horizon/CoreHorizonClassifier.cs` | Takes Min/Preferred/Max weeks as parameters from the candidate's own `CoreCycle`; zero distance literals anywhere in the class |
| `PlanCatalogCandidateSummary` | `RuntimeCatalog/PlanCatalogCandidateSummary.cs` | `CanonicalDistanceFamily` is a plain `string`; every field is generic reference/identity data |
| `PlanCatalogBundleLoader`/`CatalogArtifactFileResolver` | `RuntimeCatalog/*.cs` | Loads by `(CandidateKey, CandidateVersion)`, distance-blind |
| `V1CatalogWorkoutRoleBindingPolicy`/`CatalogWorkoutBinder` | `RuntimeCatalog/Schedule/Binding/*.cs` | Keyed on structural role (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN`), not distance |
| `CatalogPreferredDayAdapter` | `RuntimeCatalog/Schedule/Materialization/CatalogPreferredDayAdapter.cs` | Zero distance references found |
| `PaceSourceResolver`, `GoalFeasibilityResolver`, `TimeAdequacyResolver`, `CoreEntryReadinessResolver` | `RuntimeCatalog/Resolvers/*.cs` | Condition-based on recency/evidence state, not distance literals |
| `Schedule/LongHorizon/Adaptation/**` (10 files) | — | Zero `TEN_K`/`GoalDistance.TenK` hits repo-wide in this subtree |
| `TrainingPlan` entity (persistence) | `Domain/Entities/TrainingPlan.cs` | `CanonicalDistanceFamily`/`CatalogCandidateKey` stored as free strings, doc-commented with `"TEN_K"` only as an *example*, not a constraint |
| `RaceCoreSupportRegistry` | `RuntimeCatalog/Schedule/Materialization/RaceCoreSupportRegistry.cs` | Dark, unused (not in DI, not on any live path), distance-blind by construction |

---

## E. 10K-specific policy data (Classification B — generic mechanism, 10K-specific data)

| Authority | File | Nature |
|---|---|---|
| `VolumeSafetyPolicy.Default`/`.BeginnerFourDay`/`.ThreeDayIntermediate`/`.FiveDayIntermediate`/`.SixDayIntermediate`/`.Advanced*`/`.Beginner2D`/`.Intermediate2D`/`.ThreeDayBeginner` | `RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` | Every named instance encodes 10K training-science constants (`PreferredMaxWeeklyIncreaseRatio: 0.07d` appears identically across every one of them — a 10K-wide constant, not distance-generic), peak-reference multipliers, long-run shares, taper multiplier. The *record type* (`VolumeSafetyPolicy`) and the *engine* consuming it (`CatalogVolumeAndLongRunPlanner`) are generic; the *data* is exclusively 10K. |
| `FourDaySessionDistanceAllocationPolicy`, `V1FourDaySessionVolumeAllocationPolicy`, `V1ThreeDaySessionVolumeAllocationPolicy`, `V1TaperSharpenPrescriptionPolicy` | `RuntimeCatalog/Prescription/Session/*.cs` | Session-shape splitting rules calibrated to 10K's own weekly-volume ranges |
| `V1*MissingReadinessStartingVolumePolicy` family (Beginner4D/ThreeDay/FiveDayIntermediate/SixDayIntermediate) | `RuntimeCatalog/Prescription/Volume/*.cs` | Missing/explicit-zero readiness default km values, all 10K-calibrated |
| `TenKPreparationRunwayNumericPolicyFactory`/`...AllocationPolicyFactory`/`...ProgressionPolicyFactory`/`...PacePolicyFactory`/`...WeekMaterializationPolicyFactory`/`...CalendarCompositionPolicyFactory` | `RuntimeCatalog/Schedule/PreparationRunway*/*.cs` | Runway-only; **out of scope for the 14-week Core slice** (§B/§C), but flagged here for completeness — all six carry literal `"TEN_K"` `PolicyKey`/`CandidateKey` constants |

---

## F. Hardcoded 10K assumptions requiring parameterization (Classification C)

1. **`V1CatalogPilotIdentityPolicy`** (`RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`)
   - `public const GoalDistance GoalDistance = GoalDistance.TenK;` (line 44) — single hardcoded distance for the entire policy.
   - `ResolveCandidate(RunningBackground level, int daysPerWeek)` (line 254) and `IsSupportedLevelFrequency(RunningBackground level, int daysPerWeek)` (line 184) — **structurally lack a distance parameter**; this is not a constant swap, it is a signature change.
   - `IsSupportedPreparationRunwayLevelFrequency`/`IsSupportedPreparationRunwayCandidate` — same shape, Runway-scoped (out of scope for this slice, in scope for a future Runway wave).
   - **17 real production consumers** repo-wide (verified by grep): `CatalogPreviewGenerator.cs`, `GenerationRouteDecision.cs`, `LivePlanPreviewRouting.cs`, `PlanCatalogDeploymentAuthority.cs`, `PlanCatalogDomainMapper.cs`, `PlanServices.cs` (Core-critical-path subset), plus 11 more in the LongHorizon/PreparationRunway subtree (out of scope for this slice).

2. **`CatalogGoalDistanceResolver`** (embedded static class inside `RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs`, lines 298–324)
   - `catalogDistanceFamily switch { "TEN_K" => TenKDistanceKm, _ => throw UNSUPPORTED_CATALOG_GOAL_DISTANCE }` and the identical shape for `requestGoalDistance`. Fail-closed (good safety property) but requires an explicit `"HALF_MARATHON" => 21.0975` / `GoalDistance.HalfMarathon => 21.0975` branch before any HM request can build a prescription context. See §H — this duplicates `GoalDistanceKm.Resolve`, which already has the correct value.

3. **`CatalogVolumeAndLongRunPlanner.Build`** (`RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs`, lines 24–89)
   - Several branches explicitly gate on `CanonicalDistanceFamily == "TEN_K"` (2D Beginner/Intermediate, 5D, 6D, Advanced) — correctly scoped.
   - **But the branches for `Level=="NEW"&&DaysPerWeek==4"`, `Level=="INTERMEDIATE"&&DaysPerWeek==3`, and `Level=="NEW"&&DaysPerWeek==3` check no distance field at all**, and the pilot cell itself (`Intermediate`/`4D`, matching *no* explicit branch) falls through to the constructor-supplied `_policy`, which the sole production call site (`CatalogPreviewGenerator.DefaultVolumeAndLongRunPlanner()`, line 316) sets to `VolumeSafetyPolicy.Default` — the literal TEN_K/Intermediate/4D authority — **unconditionally**, with no distance check whatsoever. This is currently inert only because finding 1 (the identity gate) rejects every non-TenK request before this class ever runs. It is the exact "switch/default branch whose only currently reachable distance is 10K" pattern the governing prompt's §4 names verbatim, and it is a genuine latent defect that generalization must close, not merely note.

4. **`TenKPreparationRunwayDarkOrchestrator.ValidateRequest`** (`RuntimeCatalog/Schedule/PreparationRunwayOrchestration/TenKPreparationRunwayDarkOrchestrator.cs`, lines 331–400) — Runway-scoped, out of this slice's critical path, but a textbook instance of the same family: `request.Candidate.CanonicalDistanceFamily != "TEN_K"` (line 363), `request.ResolverInput.RequestedTargetDistanceKm != 10d` (line 390), `resolver.RecentRaceDistanceKm == 10d` (line 409) — three independent hardcoded-10K literal checks in one method, all fail-closed (safe today, but each one is a site that must be parameterized before any future HM Runway wave).

---

## G. Recurring-assumption-family inventory (mandatory §5 search — do not stop at one instance)

### Family 1: "Exact-identity-tuple dispatch with a distance-blind fallback that silently inherits another distance's numeric authority"

- **`ASSUMPTION_FAMILY`**: A dispatcher enumerates explicit `(CanonicalDistanceFamily, Level, DaysPerWeek)` branches for the cells it was built to support, but its residual/default case does not itself check `CanonicalDistanceFamily` — it simply keeps whatever policy the caller was constructed with, which today is always the TEN_K authority because only TEN_K can reach the dispatcher at all.
- **`PRIMARY_OCCURRENCE`**: `CatalogVolumeAndLongRunPlanner.Build` (lines 24–89) — the Intermediate/4D pilot cell (and the two Level-only branches at lines 53/62 for 3D) match no explicit `CanonicalDistanceFamily` check.
- **`SIBLING_OCCURRENCES`**: `TenKPreparationRunwayNumericPolicyFactory.Build(candidate)` (lines 31–70) — its own `_ => Build()` fallback (line 69) reuses the unparameterized `Build()` overload, itself hardcoded to `VolumeSafetyPolicy.Default`. (Runway-scoped; not reachable for this slice, but the identical shape.) I searched every remaining `TenK*PolicyFactory` (Allocation, Progression, Pace, WeekMaterialization, CalendarComposition) for the same "candidate-keyed dispatch with a silent generic fallback" shape: **none of the other five take a candidate parameter at all** — they are unconditional single-`Build()` static methods with no dispatch, so they cannot exhibit this specific sub-shape (they exhibit Family 2 instead, below).
- **`CURRENT_AUTHORITY`**: `VolumeSafetyPolicy.Default` (10K/Intermediate/4D) is the de facto "authority of last resort" for any identity that reaches these two dispatchers without a matching branch.
- **`CONSUMERS`**: `CatalogPreviewGenerator.DefaultVolumeAndLongRunPlanner()` (Core path, in scope), `LongHorizonFullNumericOrchestrator` and `TenKPreparationRunwayComponentAdapters` (both construct `new CatalogVolumeAndLongRunPlanner()` directly — Runway/LongHorizon scoped, out of scope for this slice but must be re-audited before a future wave).
- **`REQUIRED_ACTION`**: Before HM can safely reach either dispatcher (i.e., before the identity gate in §F.1 is widened), each dispatcher's fallback path must be converted from "silent default" to an explicit, closed `switch` over `CanonicalDistanceFamily` that throws (or routes to a real HM authority) for any family it does not recognize — mirroring the fail-closed pattern `CatalogGoalDistanceResolver` already uses. This is a **zero-product-decision, mechanical safety fix**, but it is required *before* generalization, not optional cleanup.

### Family 2: "Single hardcoded `TEN_K__4D__INTERMEDIATE` identity constant embedded in a static policy factory with no dispatch at all"

- **`ASSUMPTION_FAMILY`**: A `Build()`-only static factory (no candidate parameter) declares `CandidateKey`/`CandidateVersion`/`PolicyKey` constants equal to the TEN_K pilot identity purely as documentation/provenance, then is invoked unconditionally from the one orchestrator that currently only ever runs for that identity.
- **`PRIMARY_OCCURRENCE`**: `TenKPreparationRunwayPacePolicyFactory` (`CandidateKey = "TEN_K__4D__INTERMEDIATE"`, line 7).
- **`SIBLING_OCCURRENCES`**: `TenKPreparationRunwayNumericPolicyFactory` (`CandidateKey`/`CandidateVersion` at lines 11–12, same identity), `TenKPreparationRunwayAllocationPolicyFactory` (no candidate constant, but same "TenK-named, single-identity" framing in its doc comment), `TenKPreparationRunwayCalendarCompositionPolicyFactory`, `TenKPreparationRunwayProgressionPolicyFactory`, `TenKPreparationRunwayWeekMaterializationPolicyFactory` (not individually re-opened in full given they are out of this slice's scope, but confirmed present via the file listing and the orchestrator's own unconditional static references to all six by name at `TenKPreparationRunwayDarkOrchestrator.cs` lines 125, 146, 191–202, 265, 280, 298).
- **`CURRENT_AUTHORITY`**: the orchestrator class itself (`TenKPreparationRunwayDarkOrchestrator`) — it is the *only* place that decides which policy-factory family runs, and it does so by static name, not by any runtime distance check.
- **`CONSUMERS`**: `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync` (sole caller of all six factories).
- **`REQUIRED_ACTION`**: **Out of scope for the current 14-week Core slice** (Runway/LongHorizon only reachable via `CoreHorizonMode.PreparationRunwayPlusCore`, which a 14-week request cannot produce). Recorded here in full per the governing prompt's explicit instruction not to declare a family closed after one instance — this family has (at minimum) two confirmed occurrences and four structurally-identical siblings, all requiring the same treatment (a distance-selecting layer above the orchestrator, or the orchestrator itself renamed/parameterized) in a future, dedicated Runway-generalization phase.

### Family 3: "`GoalDistance ↔ km` mapping owned in more than one place"

- Covered fully in §H (duplicate authority) — flagged here too because it is exactly the shape §5 warns about: a hardcoded 10K-only version (`CatalogGoalDistanceResolver`) coexists with an already-correct generic version (`GoalDistanceKm.Resolve`) and a third, also-already-correct generic version (`CanonicalTargetFinishTimePolicy`) elsewhere in the same codebase — a future fix that "adds a HalfMarathon branch" to only the first one without checking the other two would repeat the exact one-instance-at-a-time pattern `GEN.10`/`GEN.12`/`GEN.17` etc. institutionalized a defense against.

I did **not** find further siblings of Family 1 or Family 2 in the Core-only critical path (`DynamicCoreWeekSkeletonOrchestrator`, `DynamicCoreWorkoutBindingOrchestrator`, `DynamicCoreSessionPrescriptionOrchestrator`, `DynamicCoreCalendarMaterializationOrchestrator` were each grepped individually for `CanonicalDistanceFamily`/`TEN_K`/`GoalDistance` — see §B; only doc-comment mentions of `TEN_K_MASTER v6` as an illustrative example were found, no branching logic).

---

## H. Duplicate/ambiguous authority inventory (Classification F)

| Decision | Owner A | Owner B | Owner C | Verdict |
|---|---|---|---|---|
| `GoalDistance → km` | `GoalDistanceKm.Resolve` (`Application/Common/GoalDistanceKm.cs`) — generic, correct, includes HalfMarathon/Marathon | `CanonicalTargetFinishTimePolicy` (`Application/Common/CanonicalTargetFinishTimePolicy.cs`) — generic seconds-based sibling, also includes HalfMarathon | `CatalogGoalDistanceResolver` (embedded in `CatalogPrescriptionContextBuilder.cs`) — **hardcoded to TEN_K only, fail-closed** | **`DUPLICATE_OR_AMBIGUOUS_AUTHORITY`.** Owner C should be parameterized to delegate to Owner A's already-approved canonical value (`21.0975`) rather than re-declaring a second, narrower private constant — this both closes the HM gap and removes the duplication in one mechanical change. Not silently resolved here per the governing rule; flagged for the implementing phase to make an explicit, not-invented, choice. |

No other duplicate ownership was found for the items enumerated in the governing prompt's §6 checklist within the Core-only critical path: distance core min/preferred/max (single owner — the candidate's own `CoreCycle`, catalog-declared), phase allocation (single owner — candidate's `PhaseAllocations`), run-layout role cardinality (single owner — `CatalogRunLayoutSlots`/layout's `SlotRoles`/`WeeklyPatternRoles`), level/frequency eligibility (single owner in-scope — `V1CatalogPilotIdentityPolicy`, itself the hardcode finding in §F, not a duplication), peak-volume band (loaded from the catalog artifact's own `PeakVolumeBandPolicy` reference — single owner), starting-volume/session/pace/goal-feasibility (each has exactly one `V1*Policy`/resolver owner per cell). Preparation-Runway-scoped ownership questions (block allocation, runway composition) were not re-audited for duplication beyond what §G Family 2 already surfaces, since that subtree is out of scope for this slice.

---

## I. HM authorities required for Intermediate × 4D × 14W (Classification D/E)

All of the following are genuinely new typed authorities the architecture has a slot for but no content for — none of them require a new *mechanism*:

1. **HM `CoreCycle` bounds** (min/preferred/max weeks for Intermediate×4D) — the governing prompt fixes `preferred = 14`; min/max are undecided. `OPEN_HM_PRODUCT_DECISION`.
2. **HM numeric progression authority** — a new `VolumeSafetyPolicy`-shaped record (peak-volume reference/band, `PreferredMaxWeeklyIncreaseRatio`, `HardMaxWeeklyIncreaseRatio`, `AbsoluteWeeklyIncrementCapKm`, `TaperVolumeMultiplier`, long-run share bounds) calibrated to Half-Marathon training science, not copied from 10K's `0.07d`/taper-multiplier/etc. `OPEN_HM_PRODUCT_DECISION` (values), `HM_NEW_DISTANCE_AUTHORITY_REQUIRED` (typed slot already exists as the `VolumeSafetyPolicy` record shape).
3. **HM starting-volume / missing-readiness / explicit-zero defaults** — new `V1*MissingReadinessStartingVolumePolicy`-shaped authority, HM-calibrated. `OPEN_HM_PRODUCT_DECISION`.
4. **HM 4D session-distance allocation** — whether HM's own 4-day slot split (KEY/EASY/EASY/LONG_RUN proportions) matches 10K's `FourDaySessionDistanceAllocationPolicy` shape or needs its own. `OPEN_HM_PRODUCT_DECISION` (evidence question, explicitly barred from re-opening external research this phase — flag only).
5. **HM workout catalog content** — goal-pace workout identity for a Half-Marathon pace target, taper/peak workout progression stages. Requires catalog authoring, explicitly barred (`HARD PHASE BOUNDARIES` item 2). `BLOCKED_BY_OPEN_DECISION` + explicit boundary.
6. **HM candidate identity itself** (`HALF_MARATHON__4D__INTERMEDIATE`, some version) — a new `PlanCatalogCandidateSummary`-shaped artifact. Same boundary as #5.
7. **HM public/dark gate widening in `V1CatalogPilotIdentityPolicy`** — mechanical (§F.1), no new numeric decision, but must be paired with #1–#3 existing before it can be exercised meaningfully even dark.

**[Corrected by `HM.0.1`]** Items 1–7 above correspond directly to `HM_21K_Claude_Handoff_and_Build_Plan.md` §6's `OPEN-HM-01` through `OPEN-HM-07` (now tracked in git — see §0.1 for why the original version of this section could not find it). The correspondence is not one-to-one by number; rather, each numbered `OPEN-HM` item bundles one or more of the authority gaps this map independently found from code. See §J for the full per-item re-classification, including — critically — which parts of each `OPEN-HM` item already have a specific, evidence-backed candidate in the handoff document (needing governance sign-off, not new research) versus which parts remain genuinely unresourced product decisions, and which items are relevant to the first slice at all versus safely deferrable.

---

## J. OPEN-HM decisions encountered — corrected per-item classification (`HM.0.1`)

**[This entire section is rewritten by the `HM.0.1` corrective pass — see §0.1.]** The original version of this section stated that no document enumerating `OPEN-HM-01` through `OPEN-HM-07` could be located; that was incorrect (§0.1). `HM_21K_Claude_Handoff_and_Build_Plan.md` §6 defines all seven explicitly. None of the seven is *decided* here — per the governing prompt's explicit boundary, this remains an audit, not a decision phase — but each is now individually re-classified against the governing prompt's A–F scheme (§APPSEL_HM_PHASE_0…§2), rather than left as a uniform "still open" bucket, per the corrective-pass instruction to evaluate whether the handoff document already supplies a specific-enough candidate.

Two axes matter for each item: **(1)** does a specific, evidence-backed candidate already exist in the handoff document (making the item closer to "ready for explicit governance sign-off" than "needs research from scratch"), and **(2)** is the item actually needed to unblock the first slice (`HALF_MARATHON × INTERMEDIATE × 4D × 14W`) or is it safely deferrable to a later wave.

| Item | Classification | Candidate status | First-slice relevance |
|---|---|---|---|
| **OPEN-HM-01** — Exact 10–16 week phase allocation | **E** `OPEN_HM_PRODUCT_DECISION` | **Split.** The 14-week split itself — `3 Foundation / 4 Build / 5 RaceSpecific / 2 Taper` — is a specific, named candidate (handoff §4.1), not an unresourced question; it needs explicit governance sign-off, not new research. The other six week-counts (10/11/12/13/15/16) have **zero candidate** — the handoff explicitly marks them "Unknown/final decision required" and instructs "do not invent the exact allocation table yet." | **In scope (14w part only), ready for sign-off.** The 10/11/12/13/15/16-week table is **out of scope for the first slice** — it is needed only for the handoff's own WAVE HM-D ("Dynamic 10–16 week core"), which explicitly runs *after* HM-C (the first slice) is green. Deferrable without blocking the first slice. |
| **OPEN-HM-02** — Peak long-run authority | **E** `OPEN_HM_PRODUCT_DECISION` | **Weak/unfrozen candidate only.** The handoff cites "approximately 18–19 km" as a legacy figure but explicitly labels it "not yet treated as a frozen canonical peak-long-run authority" (§OPEN-HM-02), and lists five genuinely open sub-questions (absolute-distance vs. duration vs. weekly-share interaction; level effects; frequency effects; peak placement; whether any user should exceed 21.1 km; quality-long-run interaction). This is real unresolved product/research territory, not merely awaiting sign-off. Note this is distinct from the peak *weekly-volume* bands in §4.2, which already exist for 3D–5D (see OPEN-HM-05/06 below for the frequencies those bands don't cover). | **In scope, genuinely blocking.** Intermediate×4D×14W needs a peak long-run distance for its `LONG_RUN` role — this is part of the `HM.0` §I item 2 "long-run share bounds" gap and remains a real `OPEN_HM_PRODUCT_DECISION` for the first slice, not a formality. |
| **OPEN-HM-03** — Exact HM workout progression | **E** `OPEN_HM_PRODUCT_DECISION` (becomes **D** `HM_NEW_DISTANCE_AUTHORITY_REQUIRED` once decided — the catalog/workout-identity slot the architecture needs already exists structurally, per §D/§I item 5) | **Conceptual shape only, not a frozen candidate.** The handoff gives a phase-relative skeleton (Foundation → aerobic/strides/basic quality; Build → threshold; RaceSpecific → HM-specific intro/development/rehearsal; Taper → reduced-volume HM activation) but says explicitly: "exact stages, candidate workout identities and exposure minima/maxima must be adopted explicitly." | **In scope, but doubly blocked** — both by the missing decision and by every HM phase's own standing boundary against authoring catalog content. Needs a dedicated evidence/decision phase before any implementation phase can author workout identities. |
| **OPEN-HM-04** — Quality-long-run vs. weekly hard-budget policy | **E** `OPEN_HM_PRODUCT_DECISION` for the exact per-frequency rule table, but **substantially pre-informed** by already-closed research | **Stronger candidate than the other open items.** `HM-LIT-17` (dose-tier model: `workoutFamily × progressionStage × doseTier`, plus the explicit rule "threshold/HM-pace/structured-hard finish ⇒ consumes quality budget; controlled aerobic steady finish does not automatically count as a hard stimulus") and `HM-LIT-21` (KEY-vs-quality-LONG spacing: "hard minimum ~48h, preferred ≥72h"; HARD+HARD prohibited) are both listed in the handoff's §5 as "researched and considered closed enough for product decision/adoption." The exact per-3D/4D/5D/6D table `OPEN-HM-04` asks for is not yet assembled, but the governing principles it would be built from are not blank-slate research — they are an encoding/governance-adoption exercise on top of already-closed evidence. | **In scope.** 4D's weekly template (one `KEY_SESSION` + `LONG_RUN` + easy sessions) directly needs the KEY-vs-quality-LONG interaction rule. Recommend the next phase treat `HM-LIT-17`/`HM-LIT-21` as the starting encoding rather than re-opening research from zero. |
| **OPEN-HM-05** — HM 2D numeric policy | **E** `OPEN_HM_PRODUCT_DECISION`, true open | **Zero candidate.** No 2D peak-volume/session-concentration/long-run-share/dosage/readiness-threshold figure appears anywhere in the handoff document; §4.2 explicitly states "No equivalent HM 2D or 6D peak-volume authority is considered frozen yet," and `OPEN-HM-05` itself says "Do not derive by mechanically copying 10K 2D numbers." | **Out of scope for the first slice.** 2D is not part of Intermediate×4D×14W. The handoff's own rollout order (§8, items 6–7) and WAVE HM-F place 2D closure *after* 3D–5D stabilizes. Confirmed safely deferrable — does not block the first slice or `HM.1`. |
| **OPEN-HM-06** — HM 6D numeric policy | **E** `OPEN_HM_PRODUCT_DECISION`, true open | **Zero candidate.** Same shape as OPEN-HM-05; `OPEN-HM-06` explicitly warns "Do not extrapolate the 5D HM bands without governance." | **Out of scope for the first slice**, same reasoning as OPEN-HM-05 — deferred to WAVE HM-F (Intermediate 6D, Advanced 6D), after 3D–5D is stable. |
| **OPEN-HM-07** — `>16` week HM composition | **E** `OPEN_HM_PRODUCT_DECISION`, true open | **Zero candidate.** No composed-core length, minimum-runway, maximum-horizon, runway-route, or core-entry-deload value exists anywhere in the handoff document for the `>16`-week case; `OPEN-HM-07` explicitly instructs "Do not copy 10K's '15+ ⇒ runway + 12W core' rule." | **Out of scope, confirmed unreachable for the first slice.** This is exactly the Preparation-Runway/LongHorizon composition question `HM.0`'s own §B/§C already found unreachable by a 14-week request (`CoreHorizonMode.PreparationRunwayPlusCore` cannot be produced by this slice). Relevant only to a future WAVE HM-H phase. |

**None of the seven items is actually `DISTANCE_GENERIC_REUSE` or `TEN_K_DISTANCE_POLICY` once the real candidates are seen.** Every candidate figure in the handoff document (the 3/4/5/2 phase split, the 3D–5D peak-volume bands, the starting-long-run caps, the dose-tier mapping) is presented as a new, HM-specific value distinct from 10K's own numbers — the handoff repeatedly warns against copying 10K figures into HM (§0, §4.2, `OPEN-HM-05`, `OPEN-HM-06`, `OPEN-HM-07`) precisely because architecture reuse does not imply numeric reuse. This confirms rather than contradicts `HM.0`'s original Classification-D framing (§I): the *mechanism* is reused, the *authority* is new and HM-specific in every case.

**Net effect on the first slice**: of the seven items, four are directly relevant to Intermediate×4D×14W (`OPEN-HM-01`'s 14-week part, `OPEN-HM-02`, `OPEN-HM-03`, `OPEN-HM-04`), and three are confirmed deferrable without blocking it (`OPEN-HM-05`, `OPEN-HM-06`, `OPEN-HM-07`, plus the non-14-week part of `OPEN-HM-01`). Of the four in-scope items, none has a value that can be adopted without a governance decision — but two (`OPEN-HM-01`'s 14-week split and, more weakly, `OPEN-HM-04`'s hard-budget interaction) are much closer to "sign off on an existing, specific candidate" than "commission new research," which was not visible in the original version of this report because the source document was missing from its search entirely.

---

## K. Minimal file-change forecast

| File | Action | Reason |
|---|---|---|
| `RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` | **MODIFY** | Add a `GoalDistance`/`CanonicalDistanceFamily` parameter to `ResolveCandidate`/`TryResolveCandidate`/`IsSupportedLevelFrequency` (and the Runway-scoped siblings, later); convert the internal candidate map from `(Level, DaysPerWeek)` to `(Distance, Level, DaysPerWeek)`-keyed. Purely additive if 10K's existing calls are updated to pass `GoalDistance.TenK` explicitly — zero-delta by construction (§M). |
| `RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` (`CatalogGoalDistanceResolver`) | **MODIFY** | Add `HALF_MARATHON`/`GoalDistance.HalfMarathon` branches, ideally by delegating to `GoalDistanceKm.Resolve` instead of re-declaring a private constant (closes §H's duplication in the same change). |
| `RuntimeCatalog/Prescription/Volume/CatalogVolumeAndLongRunPlanner.cs` | **MODIFY** | Close the distance-blind fallback (§G Family 1) with an explicit, closed dispatch that throws for unrecognized `CanonicalDistanceFamily` values, then add the real HM branch once #2 in §I is frozen. |
| `RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` | **MODIFY (additive)** | New named static instance(s) for HM/Intermediate/4D, once numeric authority is frozen (blocked on `OPEN_HM_PRODUCT_DECISION`). |
| `RuntimeCatalog/PreviewRouting/CatalogPreviewGenerator.cs`, `GenerationRouteDecision.cs`, `LivePlanPreviewRouting.cs`, `RuntimeCatalog/PlanCatalogDeploymentAuthority.cs`, `RuntimeCatalog/PlanCatalogDomainMapper.cs`, `Application/Services/PlanServices.cs` | **MODIFY (call-site only)** | Pass the new distance parameter through to the widened `V1CatalogPilotIdentityPolicy` methods. No logic change beyond threading the parameter. |
| A new HM candidate-identity/version constant set (naming pattern TBD — e.g. mirrored constants inside `V1CatalogPilotIdentityPolicy` or a sibling class) | **NEW** | Required once #6/#7 in §I are decided; explicitly not authored in this phase. |
| Plan-catalog artifact for `HALF_MARATHON__4D__INTERMEDIATE` | **NEW** (barred this phase) | Required for real execution; this phase produces no such file. |
| Everything under `RuntimeCatalog/Schedule/PreparationRunway*/**`, `RuntimeCatalog/Schedule/LongHorizon/**` (≈150 files) | **NO CHANGE** | Not reachable by a 14-week Core-only request (§B/§C); would only matter for a future dedicated HM Runway/LongHorizon wave. |
| `CoreHorizonClassifier.cs`, `PlanCatalogCandidateSummary.cs`, `CanonicalDistanceFamilyResolver.cs`, `GoalDistanceKm.cs`, `CanonicalTargetFinishTimePolicy.cs`, `V1CatalogWorkoutRoleBindingPolicy.cs`, `CatalogPreferredDayAdapter.cs`, `Schedule/LongHorizon/Adaptation/**`, `TrainingPlan.cs` | **NO CHANGE** | Already fully generic (§D). |

---

## L. Sibling-consumer impact matrix

| Authority proposed to become distance-keyed | Direct consumers found (grep-verified) | In scope for this slice? |
|---|---|---|
| `V1CatalogPilotIdentityPolicy.ResolveCandidate`/`IsSupportedLevelFrequency`/`IsSupportedIdentity` | `CatalogPreviewGenerator.cs`, `GenerationRouteDecision.cs`, `LivePlanPreviewRouting.cs`, `PlanCatalogDeploymentAuthority.cs`, `PlanCatalogDomainMapper.cs`, `Services/PlanServices.cs` (Core path); `LongHorizonCoreWorkoutBindingExecutor.cs`, `LongHorizonFullNumericOrchestrator.cs`, `LongHorizonStructuralMaterializer.cs`, `LongHorizonStructuralValidator.cs`, `LongHorizonRollingInitialActivationContracts.cs`, `LongHorizonPublicPlanService.cs`, `PreparationRunwayCalendarComposer.cs`, `PreparationRunwayNumericMaterializer.cs`, `TenKPreparationRunwayDarkOrchestrator.cs`, `PreparationRunwayPaceMaterializer.cs` (Runway/LongHorizon, out of scope) | 6 of 17 in scope |
| `CatalogVolumeAndLongRunPlanner` | `CatalogPreviewGenerator.cs` (Core), `LongHorizonFullNumericOrchestrator.cs`, `TenKPreparationRunwayComponentAdapters.cs` (Runway/LongHorizon) | 1 of 3 in scope |
| `CatalogGoalDistanceResolver` | `CatalogPrescriptionContextBuilder.cs` (its own file — need to verify no external callers beyond the context builder itself; none found by grep) | in scope, single owner |
| `GoalDistanceKm.Resolve` | `PlanServices.cs` (already generic; no change needed, just noted as the correct convergence target for §H) | already correct |

Roughly 35 test files reference `V1CatalogPilotIdentityPolicy` directly or indirectly (see §N); a signature change to its public methods is a source-breaking change for all of them unless done as an additive overload (recommended: add a new overload accepting the distance parameter, keep the existing overload defaulting to `GoalDistance.TenK` for 10K's own byte-identical call sites, per §M's zero-delta requirement).

---

## M. 10K behavioral-preservation proof

For every required parameterization in §F/§K, the "zero 10K delta by construction" shape:

**1. `V1CatalogPilotIdentityPolicy`**
```
CURRENT 10K INPUT: (Intermediate, 4)
-> ResolveCandidate(Intermediate, 4)
-> matches (Intermediate, 4) branch -> ("TEN_K__4D__INTERMEDIATE", 10)

AFTER PROPOSED GENERALIZATION: (Intermediate, 4) called via a new overload
    ResolveCandidate(GoalDistance.TenK, Intermediate, 4), OR the existing
    overload is kept and internally defaults distance to TenK
-> the internal candidate map's (TenK, Intermediate, 4) entry is byte-identical
    to today's (Intermediate, 4) entry -> ("TEN_K__4D__INTERMEDIATE", 10)
-> same branch, same output
```
This is provable *by construction* only if the migration is additive (new overload / new required-but-defaulted parameter at existing call sites, never a behavior-changing reinterpretation of the existing map) — which is exactly what §K recommends.

**2. `CatalogGoalDistanceResolver`**
```
CURRENT 10K INPUT: ("TEN_K", GoalDistance.TenK, requestedTargetDistanceKm: 10.0)
-> catalogKm = 10.0, requestKm = 10.0 -> match -> returns 10.0

AFTER PROPOSED GENERALIZATION: same input
-> "TEN_K" branch unchanged, GoalDistance.TenK branch unchanged (whether
    inlined or delegated to GoalDistanceKm.Resolve(GoalDistance.TenK) = 10.0,
    numerically identical) -> returns 10.0, same as before
-> new "HALF_MARATHON"/GoalDistance.HalfMarathon branches are additive arms
    of the same switch; unreachable for any 10K request
```

**3. `CatalogVolumeAndLongRunPlanner.Build` fallback closure**
```
CURRENT 10K INPUT: candidate.CanonicalDistanceFamily == "TEN_K", Level ==
    "INTERMEDIATE", DaysPerWeek == 4 (matches no explicit branch, falls
    through to _policy = VolumeSafetyPolicy.Default)
-> VolumeSafetyPolicy.Default consumed, exact same numeric plan as today

AFTER PROPOSED GENERALIZATION: fallback becomes an explicit switch arm
    for ("TEN_K", "INTERMEDIATE", 4) => VolumeSafetyPolicy.Default (the
    SAME reference, not a new instance)
-> identical branch reached, identical object identity, identical output
-> a request for ("HALF_MARATHON", "INTERMEDIATE", 4) with no matching arm
    now throws instead of silently reusing VolumeSafetyPolicy.Default —
    this is a BEHAVIOR CHANGE, but only for a request shape that is
    currently unreachable in production (blocked by the identity gate in
    finding 1), so it is zero-delta for every real, reachable 10K request
    while closing the latent defect for any future distance
```

**Where the proof cannot yet be made**: the *values* of any new HM `VolumeSafetyPolicy` instance are, by definition, new code — there is no "current 10K input" to compare against for a request that never reached this system before. The zero-delta claim in this section is scoped to **10K's own existing reachable requests**, exactly as the governing prompt's §8 defines it; it says nothing about HM's own correctness, which is not this phase's question.

---

## N. Regression-test forecast

Per §11/§14, no large regression suite was run this phase (static analysis only). The test groups that a future implementing phase must run to prove each invariant:

- **No 10K numeric delta**: `V1FiveDayIntermediateMissingReadinessStartingVolumePolicyTests`, `Gen4DBeginnerFourDayCoreTests`, `DynamicCoreVolumeAndLongRunOrchestratorTests`, `Phase4G5GPrerequisiteCrossChecksTests` — all currently reference `V1CatalogPilotIdentityPolicy` directly and would immediately surface a signature-compatibility break.
- **No 10K workout/binding delta**: `CatalogWorkoutBinderTests`, `DynamicCoreWorkoutBindingOrchestratorTests`.
- **No 10K calendar delta**: `DynamicCoreCalendarMaterializationOrchestratorTests`, `Freq6D4D5GCoreHorizonExecutionContextTests`.
- **No 10K adaptation delta**: none of the `Adaptation/**` production files reference the identity policy at all (§D); no dedicated Adaptation regression is predicted to be at risk, but the full `LongHorizonFullDarkLifecycleHarnessTests` sweep is the closest available proxy if Runway/LongHorizon are ever touched.
- **No 10K horizon delta**: `PreparationRunwayHorizonAuthorityTests`, `PreparationRunwayDateAuthorityTests` (both exercise `CoreHorizonClassifier`/`CoreHorizonDecision`, unchanged by this audit's proposals).
- **No 10K eligibility delta**: `CatalogCandidateEligibilityGateTests`, `RunningBackgroundV2Tests`.
- **No 10K public-rollout delta**: `PublishedCatalogRuntimeCompatibilityTests`, `Gen38TwoDayRunwayLongHorizonPublicActivationTests` (Runway/LongHorizon-scoped, only relevant once a future phase touches that subtree).
- A brand-new, dedicated test class (naming pattern consistent with this repo's convention, e.g. `HmZeroDeltaTests` or similar) asserting `V1CatalogPilotIdentityPolicy`'s widened methods produce byte-identical output for every existing 10K `(Level, DaysPerWeek)` pair before/after the signature change — this is the direct, mechanical proof of §M's claim and should be the first test written in the implementing phase, before any HM-specific test.

No test currently exists for any HM identity, candidate, or numeric shape (confirmed: 0 files matching `*HalfMarathon*`/`*HM*Test*` under `RunningApp.IntegrationTests`), consistent with this being a pre-implementation audit.

---

## O. Risks

1. **The distance-blind fallback in `CatalogVolumeAndLongRunPlanner` (and its Runway sibling) is a live latent defect**, not merely a generalization inconvenience — if any future change widens the identity gate (§F.1) before this fallback is closed, an HM request could silently execute against 10K's numeric authority and produce a plausible-looking but scientifically wrong plan with no error. This must be sequenced correctly: close the fallback *before* widening the gate, never the other way around.
2. **[RESOLVED by `HM.0.1`, corrective pass]** ~~`OPEN-HM-01..07` do not appear to be enumerated anywhere in this repository yet.~~ They are enumerated in `HM_21K_Claude_Handoff_and_Build_Plan.md` §6 (now tracked in git). The original version of this risk was itself a symptom of the defect §0.1/§Q describe: the audit's search covered only tracked repository content and missed a document that was physically present in the working directory the entire time. See §J for the corrected per-item classification and §Q for the search-discipline lesson this establishes going forward.
3. **The Preparation-Runway/LongHorizon `TenK*` family (§G Family 2) is large (≈150 files) and entirely unaudited beyond confirming it is out of scope for this slice.** A future HM Runway/LongHorizon wave should not assume this audit's "READY" verdict extends to that subtree — it explicitly does not.
4. **Session-distance-allocation and workout-progression evidence questions (§I items 4–5) are training-science questions this audit is barred from re-opening.** Treating 10K's 4D session shape as "obviously reusable" without a real evidence pass would repeat the exact copy-10K-numbers-into-HM mistake the governing prompt's hard boundaries forbid.
5. **Additive-overload discipline is required, not optional**, for the `V1CatalogPilotIdentityPolicy` change to be provably zero-delta (§M) — a same-signature reinterpretation (e.g., silently making the existing 3-argument overload assume `GoalDistance.TenK`) is behaviorally identical only if every one of the 17 consumers is re-verified, which is exactly the kind of "probably fine" claim §8 explicitly rejects as insufficient.

---

## P. Exact recommended next phase

**[Updated by `HM.0.1`]** `HM_21K_Claude_Handoff_and_Build_Plan.md` is now tracked and read in full (§0.1), which removes the need for item 1 below in its original "confirm or establish the document" form — it replaces it with a narrower, sign-off-shaped item.

**`HM.1` — HM Core-horizon authority closure for Intermediate×4D×14W** (evidence/decision phase, not implementation):
1. **Governance sign-off on the already-candidate items**: the 14-week phase split (`3/4/5/2`, `OPEN-HM-01`'s in-scope part) and, if accepted as the starting encoding, `HM-LIT-17`/`HM-LIT-21`'s dose-tier/hard-adjacency principles feeding `OPEN-HM-04` — these need an explicit product decision to adopt, not new research (§J).
2. Resolve the Core-cycle min/max bounds around the fixed preferred=14 weeks (§I item 1) — the handoff's core-horizon candidate (min 10 / preferred 14 / max 16 weeks, §4.1) is a plausible source but is itself still `OPEN_HM_PRODUCT_DECISION` per `OPEN-HM-01`'s own text.
3. Resolve `OPEN-HM-02` (peak long-run authority) — this one has only a weak, explicitly-unfrozen legacy candidate (~18–19 km) and several genuinely open sub-questions (§J); treat as real evidence/decision work, not a formality.
4. Resolve `OPEN-HM-03` (exact HM workout progression) — the handoff's phase-relative skeleton is a starting shape only; exact stages/identities/exposure minima still need adoption, and catalog authoring remains barred until a dedicated implementation phase.
5. Confirm `OPEN-HM-05`/`OPEN-HM-06` (2D/6D numeric policy) and `OPEN-HM-07` (`>16`wk composition) are correctly deferred past the first slice (§J confirms this; `HM.1` should not attempt to close them).
6. Only after `HM.1` closes: an `HM.2` implementation phase should (a) close the two mechanical fallback defects in §G Family 1 first, (b) widen `V1CatalogPilotIdentityPolicy` additively, (c) author the minimum HM catalog artifact needed for one candidate cell, (d) implement the frozen numeric authority from `HM.1`, and (e) dark-verify the full 14-week slice end-to-end — all strictly before any public-routing consideration, per `MASTER_ROADMAP.md`'s own wave-gating rule.

This sequencing keeps every step consistent with §10's minimal-generalization rule: no Runway/LongHorizon/Marathon generalization is attempted until this one slice demonstrably needs it.

---

## Q. Methodological lesson (`HM.0.1` corrective pass) — search the full working directory, not only tracked content

**The defect this pass corrects is a search-method blind spot, not an isolated one-off mistake, and must be treated with the same discipline this engagement applies to recurring hardcoded-assumption families (§G; the governing 10K engagement's own `GEN.10/12/17/19/20/23/29/32/33/34/35/36/37/38` pattern of "find one instance, fix it, discover siblings later").**

`HM_21K_Claude_Handoff_and_Build_Plan.md` was physically present in the working directory for the entire original `HM.0` run — its file mtime predates `HM.0`'s own commits by roughly 46 minutes — but `HM.0`'s search process apparently covered only git-tracked content (or tooling/habit defaulted to a tracked-content-only view), so an untracked file sitting in plain sight in the repository root was never opened. This produced a materially wrong conclusion (§J's original claim that no `OPEN-HM` enumeration existed) that a plain `ls`/directory listing of the repo root would have caught immediately.

**The general lesson, for any future HM phase (or a fresh session with no memory of this correction):**
- A phase's search for "what exists in the repository" must cover the full working directory — tracked **and** untracked files — not just `git grep`/tracked-file search tools, which silently exclude anything not yet `git add`-ed.
- Before concluding "no such document exists," run at least one broad, untracked-inclusive listing (e.g. `git status --short` read in full, not just skimmed for the files you expect; a root-level `ls`/`Glob` sweep for plausible filenames) in addition to any tracked-content grep.
- Untracked files are not automatically scratch/noise — this engagement has already seen one land as the single authoritative source for seven numbered open-decision items. Treat an unexpected untracked file at the repository root as a signal to investigate its purpose, not to skip it as build noise (this repo does have real, ignorable untracked noise — `.trx` test-result files, `bin`/`obj` rebuild artifacts — but "untracked" alone is not sufficient grounds to ignore something; its content and placement must be checked).
- When a phase's own conclusions later prove to rest on an incomplete search, the correction should name the blind spot explicitly (as this section does) so it closes for every future phase, not just patch the one instance found — exactly the same discipline the governing prompt's §5 requires for recurring hardcoded-assumption families, applied here to a recurring *search-method* gap instead of a recurring *code* gap.

---

## §13 Final classification

```
HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE
```

Scoped explicitly to the **Core horizon only** for `HALF_MARATHON × INTERMEDIATE × 4D × 14-WEEK PREFERRED CORE × DARK`. No second HM-specific generation engine is required (§B/§C). The first slice has a clear generic route (12 of 22 traced steps already `REUSED_AS_IS`). All required HM-specific inputs can be expressed as typed authorities the architecture already has a slot for (§I). Every required 10K parameterization identified has a credible, constructively-provable zero-delta path (§M), conditioned on additive-overload discipline. The one duplicate authority found (§H) has an unambiguous correct side to converge on and does not block implementation. The Preparation-Runway/LongHorizon `TenK*` family is real, large, and **explicitly excluded** from this verdict — it is not reachable by this slice and was not claimed to be audited to the same depth.

**`HM.0.1` corrective-pass confirmation: this classification is UNCHANGED, and is not automatically assumed correct merely because the original code reads were solid — it was re-derived on the corrected evidence.** The classification was never conditioned on numeric-authority completeness (it explicitly names `OPEN_HM_PRODUCT_DECISION` items as a condition on, not a blocker to, readiness — see the four bullet points earlier in this classification). Newly locating `HM_21K_Claude_Handoff_and_Build_Plan.md` (§0.1) does not introduce a new blocker: it replaces "these authorities are entirely unresourced, unknown whether evidence exists at all" with "these authorities have specific, named candidates (14-week phase split, 3D–5D peak-volume bands, starting-long-run caps, dose-tier model, calendar-spacing rules) that need governance sign-off, in several cases against research the handoff's own §5 already calls closed" (§J). This makes the architecture verdict *more* confidently supportable, not less — the path from `HM.0`/`HM.0.1` to a real `HM.1` sign-off phase is now visibly shorter than the original report could show. The one thing that would have changed this classification — the handoff document describing a fundamentally different target architecture requiring a second generation engine — was checked directly (§B/§C cross-check) and found not to be the case.
