# PHASE HM.0 — HALF-MARATHON DISTANCE GENERALIZATION REUSE AUDIT

**Type:** EVIDENCE / ARCHITECTURE_DESIGN (audit only — no production code changed)
**Governing prompt:** `APPSEL_HM_PHASE_0_DISTANCE_GENERALIZATION_AUDIT.md`
**Parent:** `GEN.39` (`TEN_K_V1_CAPABILITY_COMPLETE`), read as frozen evidence
**Scope discipline:** static analysis of real repository code (`backend/RunningApp.Application/RuntimeCatalog/**`, plus `Services/`, `Common/`, `Domain/Entities`), no catalog artifacts added, no numeric values invented, no `OPEN-HM-01..07` decided, no 10K behavior changed.

---

## 0. Startup confirmation

- `PHASE_LEDGER.md` (248 lines, 141 rows) and `MASTER_ROADMAP.md` (897 lines) read in full.
- `git fetch` clean; `git rev-list --left-right --count origin/main...HEAD` = `0 0` before starting. Worktree changes limited to the pre-existing tracked `bin`/`obj` rebuild noise called out in the task brief — left untouched, not staged.
- `git log -5`: `2933bea` docs(gen-25) SHA backfill, `f3c3677` GEN.25, `2e512d4` docs(gen-24) SHA backfill, `c57d774` GEN.24, `d7487d8` docs(gen-23) — consistent with the ledger's own tail.
- **Frozen-evidence check on the 10K matrix**: `GEN.39` (ledger row 142, `TEN_K_V1_CAPABILITY_COMPLETE`) states it re-verified the full 10K Level×Frequency×Horizon matrix directly against live gate code (`V1CatalogPilotIdentityPolicy`, `LongHorizonPublicPlanService.ValidatePilot`) and found every cell matching target, full regression `4382/4386` (3 named pre-existing baseline failures + a date-coincidence flake, both disclosed, zero new regressions), `PlanCatalog.Tests` `1510/1510`. **"Frozen" here means**: the ledger records this as a verified, closed state as of commit `958b54a` — it does not mean the underlying code is literally immutable, only that no further 10K product/numeric decision is open and no phase may casually alter its behavior. This audit reads that state as given, does not re-run the full regression (per the governing file's own §11/§14 instruction — static analysis only), and touches zero files under 10K's own reachable paths.
- **Phase-ID convention**: `MASTER_ROADMAP.md` §3 defines `WAVE B — Half Marathon completion` as a distinct engagement layered after the closed `WAVE A — 10K completion`, and §15's `WAVE B` bullet list opens with exactly "10K→HM reuse/gap audit" — this phase. The roadmap's own §13 forbids pre-authoring speculative IDs beyond the near-term block, and no `HM.*` row exists yet in `PHASE_LEDGER.md`. The governing prompt file itself refers to this phase as "PHASE HM.0" in its own title and eight further places, mirroring the pattern the 10K engagement used when it opened a new sub-arc under its own name (`GEN.*` for the main arc, `FREQ.*` for the frequency-focused sub-arc, `APPSEL-BACKEND.GOV.*` for governance). Given (a) HM is explicitly a new, separate engagement layered on the closed 10K one, (b) the governing prompt's own self-reference, and (c) the roadmap's requirement to keep future distances "clearly delineated as a new, separate axis" — **this phase is ledgered as `HM.0`**, establishing a new namespace for the Half-Marathon engagement exactly as `GEN.*` did for 10K. This is a judgment call, not a discovery of an existing convention, and is documented here per the governing prompt's own instruction to reason it out rather than guess silently.

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

Items 1–6 correspond, as best this phase can infer, to `MASTER_ROADMAP.md` §15 `WAVE B` bullets ("HM phase/horizon authority", "HM numeric authority", "HM workout capability", "Intermediate 4D anchor") — no separate document defining `OPEN-HM-01` through `OPEN-HM-07` verbatim was found in this repository as of this audit; the governing prompt references those IDs as if already enumerated elsewhere. **This is itself a finding, not an assumption I am permitted to paper over**: either such a document exists outside what this audit located, or the `OPEN-HM-01..07` numbering is prospective and will be assigned by the phase that actually resolves it. I have not invented definitions for those seven IDs and have not attempted to map items 1–7 above onto specific numbers — see §J.

---

## J. OPEN-HM decisions encountered (not resolved)

Per the governing prompt's explicit instruction, none of these are decided here. Encountered, in the order the reachability trace hits them:

- **Core-cycle bounds for HM×Intermediate×4D** (min/max weeks around the fixed preferred=14) — blocks step 7 of §C.
- **HM numeric progression authority** (peak band, weekly-increase ratios, taper multiplier, long-run share) — blocks step 13 of §C.
- **HM starting-volume/missing-readiness defaults** — blocks the equivalent of `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` for HM.
- **HM 4D session-distance-allocation shape** — blocks step 14 of §C.
- **HM workout catalog content (goal-pace/taper/progression identities)** — blocks steps 4, 8, 11 of §C; also blocked by the phase's own hard boundary against catalog authoring.
- **HM candidate identity/version to mint** — a naming/versioning decision, not purely technical, since it establishes the first row of a new distance's candidate space.
- **Whether HM's public gate should ever reuse `V1CatalogPilotIdentityPolicy`'s exact shape or a sibling policy class** — an architecture-adjacent product/engineering choice for the *next* phase, not resolved here (§P recommends the minimal seam).

I explicitly could not locate a source document enumerating `OPEN-HM-01` through `OPEN-HM-07` by name; the correspondence between the seven items above and those seven IDs is left for the phase that formally opens them, per §10's minimal-generalization rule (do not invent structure the repository does not yet have).

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
2. **`OPEN-HM-01..07` do not appear to be enumerated anywhere in this repository yet.** The next phase should not assume they already exist in some form this audit missed; if they do exist outside the repository (a separate research document), that should be confirmed explicitly rather than inferred from the roadmap's bullet list as this audit had to do.
3. **The Preparation-Runway/LongHorizon `TenK*` family (§G Family 2) is large (≈150 files) and entirely unaudited beyond confirming it is out of scope for this slice.** A future HM Runway/LongHorizon wave should not assume this audit's "READY" verdict extends to that subtree — it explicitly does not.
4. **Session-distance-allocation and workout-progression evidence questions (§I items 4–5) are training-science questions this audit is barred from re-opening.** Treating 10K's 4D session shape as "obviously reusable" without a real evidence pass would repeat the exact copy-10K-numbers-into-HM mistake the governing prompt's hard boundaries forbid.
5. **Additive-overload discipline is required, not optional**, for the `V1CatalogPilotIdentityPolicy` change to be provably zero-delta (§M) — a same-signature reinterpretation (e.g., silently making the existing 3-argument overload assume `GoalDistance.TenK`) is behaviorally identical only if every one of the 17 consumers is re-verified, which is exactly the kind of "probably fine" claim §8 explicitly rejects as insufficient.

---

## P. Exact recommended next phase

**`HM.1` — HM Core-horizon authority closure for Intermediate×4D×14W** (evidence/decision phase, not implementation):
1. Confirm or establish the actual `OPEN-HM-01..07` document/numbering (risk #2).
2. Resolve the Core-cycle min/max bounds around the fixed preferred=14 weeks (§I item 1).
3. Resolve the HM numeric progression authority (peak band, weekly-increase ratios, taper multiplier, long-run share) — evidence synthesis + product decision, mirroring the 10K engagement's own `GEN.2B`-style evidence→decision phase shape (§I item 2).
4. Resolve HM starting-volume/missing-readiness defaults and the 4D session-distance-allocation question (§I items 3–4) — flagging explicitly whether new evidence is required or whether an existing, already-approved evidence envelope can be reused (this audit takes no position).
5. Only after `HM.1` closes: an `HM.2` implementation phase should (a) close the two mechanical fallback defects in §G Family 1 first, (b) widen `V1CatalogPilotIdentityPolicy` additively, (c) author the minimum HM catalog artifact needed for one candidate cell, (d) implement the frozen numeric authority from `HM.1`, and (e) dark-verify the full 14-week slice end-to-end — all strictly before any public-routing consideration, per `MASTER_ROADMAP.md`'s own wave-gating rule.

This sequencing keeps every step consistent with §10's minimal-generalization rule: no Runway/LongHorizon/Marathon generalization is attempted until this one slice demonstrably needs it.

---

## §13 Final classification

```
HM_DISTANCE_GENERALIZATION_READY_FOR_FIRST_DARK_VERTICAL_SLICE
```

Scoped explicitly to the **Core horizon only** for `HALF_MARATHON × INTERMEDIATE × 4D × 14-WEEK PREFERRED CORE × DARK`. No second HM-specific generation engine is required (§B/§C). The first slice has a clear generic route (12 of 22 traced steps already `REUSED_AS_IS`). All required HM-specific inputs can be expressed as typed authorities the architecture already has a slot for (§I). Every required 10K parameterization identified has a credible, constructively-provable zero-delta path (§M), conditioned on additive-overload discipline. The one duplicate authority found (§H) has an unambiguous correct side to converge on and does not block implementation. The Preparation-Runway/LongHorizon `TenK*` family is real, large, and **explicitly excluded** from this verdict — it is not reachable by this slice and was not claimed to be audited to the same depth.
