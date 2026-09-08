# PHASE HM.2 — HALF-MARATHON CORE VERTICAL SLICE: PREREQUISITE CLOSURE, DISTANCE-PARAMETER WIDENING, AND DISCLOSED NUMERIC-AUTHORITY BLOCKER

**Type:** IMPLEMENTATION (Step 1 prerequisites: production code + tests; Step 2: partial, dark-only, no catalog artifacts authored, no public routing change)
**Parent:** `HM.0`/`HM.0.1` (architecture reuse audit), `HM.1`/`HM.1.1` (Core-horizon numeric authority: 14-week phase split, 19.0km peak long-run ceiling), `HM.1.2`/`HM.1.3` (workout progression stage catalog + exposure counts, fully deterministic)
**Governing prompt:** user-authored "HM PHASE PROMPT 2" — verbatim text defines scope; followed precisely.
**Final classification:** `HM_2_STEP_1_PREREQUISITES_COMPLETE_STEP_2_VERTICAL_SLICE_DONE_PARTIAL_BLOCKED_ON_UNFROZEN_VOLUME_NUMERIC_AUTHORITY`

---

## 0. Startup confirmation

- `git status`/`git fetch origin` clean at start; `git rev-list --left-right --count origin/main...HEAD` = `0 0`. Worktree changes limited to the pre-existing tracked `bin`/`obj`/`.trx`/`baseline_tmp` rebuild-artifact noise this engagement has repeatedly called out — left untouched, not staged.
- `git log -5` at start: `225abe1` docs(hm-1-3) SHA backfill, `2c281a3` HM.1.3, `b647e01` docs(hm-1-2) SHA backfill, `328580c` HM.1.2, `4a52944` docs(hm-1-1) SHA backfill — consistent with `PHASE_LEDGER.md`'s HM Program table (rows 1–6: `HM.0`, `HM.0.1`, `HM.1`, `HM.1.1`, `HM.1.2`, `HM.1.3`).
- `PHASE_LEDGER.md`'s HM Program table confirms no `HM.2` row exists yet. **Next free phase ID confirmed: `HM.2`** — the name both `HM.1.1 §4` and `HM.1.2 §15`/`HM.1.3 §4` already reserved for this implementation phase.
- All five required documents (`PHASE_HM_0...md` as corrected by `HM.0.1`, `PHASE_HM_1...md`, `PHASE_HM_1_1...md`, `PHASE_HM_1_2...md`, `PHASE_HM_1_3...md`) read in full before writing anything, per this prompt's own "Required reading" section. The actual code (`V1CatalogPilotIdentityPolicy.cs`, `CatalogPrescriptionContextBuilder.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `TenKPreparationRunwayNumericPolicyFactory.cs`, `GoalDistanceKm.cs`, `CanonicalDistanceFamilyResolver.cs`, `VolumeSafetyPolicy.cs`) read directly, not assumed from the audit's own description. `HM_21K_Claude_Handoff_and_Build_Plan.md` §3/§4 re-read directly (not only through HM.1/HM.1.1's own excerpts) to confirm exactly what numeric authority the handoff document supplies vs. what remains a "research-supported candidate... not automatically repository authority until explicitly encoded" (its own §4 framing).

---

## 1. A load-bearing finding made before writing any code: the volume/long-run numeric authority gap was never closed

Before touching any file, this phase re-derived — independently, not by assuming `HM.0`'s own framing was still accurate — exactly what numeric authority `HM.1`/`HM.1.1`/`HM.1.2`/`HM.1.3` had actually frozen, because the governing prompt's Step 2 requires "numeric policies... volume... long run" as pipeline stages.

**What is frozen**: `HALF_MARATHON_PHASE_ALLOCATION` (Foundation 3/Build 4/RaceSpecific 5/Taper 2, `HM.1`); `PreferredAbsolutePeakLongRunKm = 19.0km` — a **ceiling on the `LONG_RUN` role's target distance**, explicitly not a mandatory target (`HM.1.1`); `HALF_MARATHON_QUALITY_LONG_RUN_POLICY` (`HM.1`); the full workout-progression stage catalog, phase×family eligibility, ordering, `STANDARD` dose-envelope binding, taper semantics, pace-evidence fallback, and exact exposure counts (`HM.1.2`/`HM.1.3`).

**What was never frozen by any phase**: `HM.0 §I` item 2 named this explicitly — "a new `VolumeSafetyPolicy`-shaped record (peak-volume reference/band, `PreferredMaxWeeklyIncreaseRatio`, `HardMaxWeeklyIncreaseRatio`, `AbsoluteWeeklyIncrementCapKm`, `TaperVolumeMultiplier`, long-run share bounds) calibrated to Half-Marathon training science, not copied from 10K's `0.07d`/taper-multiplier/etc." — and classified it `OPEN_HM_PRODUCT_DECISION`. Re-reading `HM.1`'s own four-item scope (`OPEN-HM-01` 14-week split, `OPEN-HM-02` peak long-run, `OPEN-HM-03` workout progression, `OPEN-HM-04` quality-long-run policy) confirms none of the four is this item — `OPEN-HM-02` freezes only the `LONG_RUN` role's own ceiling distance, a narrower, different quantity from the full weekly-volume growth/starting-volume/taper-multiplier/long-run-share authority `VolumeSafetyPolicy` represents. `HM.1.1 §2.2`'s own text cites "the Intermediate×4D peak-volume envelope (36–50 km/week, `HM_21K...`§4.2)" only as a supporting analogy for the ceiling-not-target principle — it does not freeze that figure as implementation authority, and the handoff document's own §4.2 explicitly frames its 3D–5D bands as "product envelopes, not compulsory targets" that are "ready for governance adoption" but "not automatically repository authority until explicitly encoded" (§4's own intro line). No phase performed that encoding. `VolumeSafetyPolicy.Default`'s own `PreferredMaxWeeklyIncreaseRatio: 0.07d` and `TaperVolumeMultiplier: 0.53d` are identical across every one of the 17 named 10K instances in `VolumeSafetyPolicy.cs` — `HM.0 §E` itself flags this as "a 10K-wide constant, not distance-generic," i.e. exactly the number this phase is forbidden from copying into a new HM instance without a decision phase.

**Consequence, decided before writing code, not discovered mid-implementation**: Step 2's "numeric policies / volume / long run" stages cannot be implemented to real, executable numeric authority in this phase without violating the explicit constraint "No new product/numeric authority beyond what `HM.0`/`HM.1`/`HM.1.1`/`HM.1.2`/`HM.1.3` already froze... STOP → classify `DOMAIN_DECISION_REQUIRED`, do not improvise." This is classified here as `DOMAIN_DECISION_REQUIRED`, not invented around, and shapes the honest scope of Step 2 below (§6).

---

## 2. Step 1a — distance-parameter threading through `V1CatalogPilotIdentityPolicy` (COMPLETE)

**File:** `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`

Per `HM.0 §K`/`§M`'s explicit additive-overload recommendation (the only construction that makes the zero-10K-delta claim provable, not merely intended):

- `ResolveCandidate(RunningBackground, int)` now delegates to a new `ResolveCandidate(GoalDistance, RunningBackground, int)` overload with `GoalDistance.TenK` pinned. The new overload's `GoalDistance.TenK` arm is the byte-identical original switch (same match order, same resolved key/version for all 12 existing cells); the `GoalDistance.HalfMarathon` arm is new and additive, admitting exactly one cell: `(Intermediate, 4) → (HalfMarathonFourDayIntermediateCandidateKey, HalfMarathonFourDayIntermediateCandidateVersion)`, matching `HM.1`/`HM.1.1`/`HM.1.2`/`HM.1.3`'s own frozen scope exactly (no broader admission).
- `TryResolveCandidate` mirrors the same delegate-then-widen shape.
- The private `IsSupportedLevelFrequency(RunningBackground, int)` (consulted only by the **public gate**, `IsSupportedIdentity`) now delegates to a new `IsSupportedLevelFrequency(GoalDistance, RunningBackground, int)` overload, same byte-identical-`TenK`-arm-plus-additive-`HalfMarathon`-arm shape.
- **`IsSupportedIdentity` and `IsSupportedPreparationRunwayIdentity` — the public and Runway gates — are untouched, not one character changed.** Both still call only the 2-arg, TenK-pinned overloads. This is the mechanism by which "dark-only, no public routing/gate change" is enforced by construction, not by promise: HALF_MARATHON is structurally unreachable through either gate no matter what the internal candidate-resolution seam now supports.
- Added `HalfMarathonFourDayIntermediateCandidateKey = "HALF_MARATHON__4D__INTERMEDIATE"`, version `1` — a dark-only identity constant. No catalog artifact exists for it (§6 below); resolving it via the new overloads does not imply a real candidate can be loaded, only that the identity/candidate-resolution seam itself is now distance-parameterized.

**Behavior-preservation proof (real tests, not narrative)** — `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1IdentityAndGoalDistanceZeroDeltaTests.cs`:

| Test | What it confirms |
|---|---|
| `ResolveCandidate_TwoArgOverload_ByteIdenticalTo_ThreeArgTenKOverload` (12 cases, one per existing Level×Frequency pair) | `ResolveCandidate(level, days)` output is unchanged AND identical to `ResolveCandidate(GoalDistance.TenK, level, days)` — the "CURRENT INPUT → current branch/value" vs "AFTER GENERALIZATION → same branch → same output" proof `HM.0 §M` item 1 required, executed for every one of the 12 real 10K cells, not asserted narratively. |
| `TryResolveCandidate_TwoArgOverload_ByteIdenticalTo_ThreeArgTenKOverload` (12 cases) | Same proof for the non-throwing counterpart. |
| `ResolveCandidate_UnsupportedTenKCombination_StillThrows_UnaffectedByDistanceWidening` (4 cases) | Every previously-rejected TEN_K combination (e.g. Beginner×5D) still throws `ArgumentOutOfRangeException` through both overloads — the widening did not accidentally admit anything new on the TenK side. |
| `HalfMarathon_IntermediateFourDay_ResolvesDarkOnly_ThroughDistanceAwareOverloadOnly` | The one frozen HM cell resolves to the new identity constant. |
| `HalfMarathon_AnyOtherLevelFrequency_IsNotResolvable_OnlyTheFrozenCellIsAdmitted` (4 cases) | No HM cell beyond the one `HM.1`–`HM.1.3` actually froze is admitted — the widening is exactly as narrow as the frozen authority, not a blanket "any HalfMarathon" arm. |
| `PublicGate_IsSupportedIdentity_TenKUnaffected_HalfMarathonStillRejected` (12 cases) | For every existing 10K cell: `IsSupportedIdentity(..., TenK, ...)` still returns `true` (byte-identical gate), AND `IsSupportedIdentity(..., HalfMarathon, ...)` returns `false` — the public gate is proven closed to HM at every cell the internal seam now supports, not merely asserted closed. |
| `PublicGate_IsSupportedIdentity_RejectsTheOneCellHm2DarklyResolves` | Direct proof the one cell HM.2 makes internally resolvable is still publicly rejected. |
| `PreparationRunwayGate_IsSupportedPreparationRunwayIdentity_TenKUnaffected_HalfMarathonRejected` | Same proof for the Runway gate. |

All 8 test groups (≈40 individual assertions across parameterized cases) passed on first run (see §7).

---

## 3. Step 1b — duplicate `GoalDistance → km` authority resolved (COMPLETE)

**File:** `backend/RunningApp.Application/RuntimeCatalog/Prescription/CatalogPrescriptionContextBuilder.cs` (`CatalogGoalDistanceResolver`)

Per `HM.0 §H`'s finding and its own recommendation ("`GoalDistanceKm.cs` is the more likely correct owner since it's already distance-generic and already includes `HalfMarathon`, but verify this yourself rather than assuming" — verified: `GoalDistanceKm.Resolve` is a pure, generic switch with no other callers depending on `CatalogGoalDistanceResolver`'s own private constant):

- `CatalogGoalDistanceResolver`'s private `TenKDistanceKm = 10d` constant is **retired entirely** (confirmed zero external references by repository search before removal — it was consumed only within this same class). `GoalDistanceKm.Resolve` is now the sole owner of the `GoalDistance → km` mapping.
- Both switch arms (`catalogDistanceFamily` and `requestGoalDistance`) now delegate to `GoalDistanceKm.Resolve(...)`. The `"TEN_K"`/`GoalDistance.TenK` arms are numerically byte-identical (`GoalDistanceKm.Resolve(GoalDistance.TenK) == 10.0`, same value the retired constant held). A new, additive `"HALF_MARATHON"`/`GoalDistance.HalfMarathon` arm resolves to `GoalDistanceKm.Resolve(GoalDistance.HalfMarathon) == 21.0975` — dark-only today, since no production call site can reach it until a real HALF_MARATHON candidate exists (§6).
- The cross-check guard (`catalogKm` vs `requestKm` vs `requestedTargetDistanceKm` must all agree, fail-closed otherwise) is untouched in shape — only its two operands' *sources* changed, not its logic.

**Behavior-preservation proof** (same test file, §2 above):

| Test | What it confirms |
|---|---|
| `CatalogGoalDistanceResolver_TenK_ByteIdenticalToPriorConstant_AndMatchesGoalDistanceKm` | `Resolve("TEN_K", TenK, 10)` still returns exactly `10.0`, and equals `GoalDistanceKm.Resolve(TenK)` independently. |
| `CatalogGoalDistanceResolver_HalfMarathon_NewAdditiveArm_MatchesGoalDistanceKmSoleAuthority` | New arm returns `21.0975`, matching `GoalDistanceKm.Resolve(HalfMarathon)` — proves single-authority convergence, not a second hardcoded value. |
| `CatalogGoalDistanceResolver_UnsupportedCatalogFamily_StillThrows_ZeroDelta` | `"MARATHON"` still fails closed with `UNSUPPORTED_CATALOG_GOAL_DISTANCE` (Marathon was never approved for either owner — this proves the resolver didn't accidentally widen beyond TEN_K/HALF_MARATHON). |
| `CatalogGoalDistanceResolver_CrossDistanceMismatch_StillThrows_ZeroDelta` | The mismatch guard (`"TEN_K"` catalog vs. `HalfMarathon` request) still throws `GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH` — the safety property `HM.0` explicitly praised as "good" is unchanged. |

---

## 4. Step 1c — distance-blind fallback closure, including a THIRD occurrence this phase's own search found (COMPLETE)

`HM.0 §G` Family 1 ("exact-identity-tuple dispatch with a distance-blind fallback that silently inherits another distance's numeric authority") named two occurrences: `CatalogVolumeAndLongRunPlanner.Build` (primary) and `TenKPreparationRunwayNumericPolicyFactory.Build(candidate)` (sibling, Runway-scoped). Per this phase's own explicit instruction to search one layer deeper before considering the family closed (mirroring 10K's own `GEN.32`→`GEN.37` four-repeat pattern), a repo-wide grep for `Level ==`/`CanonicalDistanceFamily` across `RuntimeCatalog/**` was run before touching any file.

### 4.1 New shared exception

`CatalogVolumeUnsupportedDistanceFamilyException` added to `CatalogVolumeExceptions.cs` (extends the existing `CatalogVolumePlanningException` base, code `CATALOG_VOLUME_UNSUPPORTED_DISTANCE_FAMILY`) — one shared, named type for all three sites below, rather than three ad-hoc throws.

### 4.2 Primary occurrence — `CatalogVolumeAndLongRunPlanner.Build`

Three branches (`Level=="NEW"&&Days==4`, `Level=="INTERMEDIATE"&&Days==3`, `Level=="NEW"&&Days==3`) previously checked **no distance field at all** — exactly `HM.0 §F.3`'s own finding, verbatim. Each now requires `CanonicalDistanceFamily == "TEN_K"` explicitly (zero delta: only TEN_K could reach any of them today, since the identity gate blocks everything else). A new closing guard was added immediately after the last `ReferenceEquals(_policy, VolumeSafetyPolicy.Default)`-gated branch (the Advanced dispatch), before the generic algorithm begins: `if (ReferenceEquals(_policy, VolumeSafetyPolicy.Default) && CanonicalDistanceFamily != "TEN_K") throw CatalogVolumeUnsupportedDistanceFamilyException(...)`. Since every specific branch now requires TEN_K explicitly, this guard is a complete, closed dispatch — the only candidate reaching it with `_policy` still equal to `Default` and `CanonicalDistanceFamily == "TEN_K"` is the real Intermediate×4D pilot cell (unchanged); anything else now fails closed instead of silently executing 10K's own numeric authority.

### 4.3 Sibling occurrence — `TenKPreparationRunwayNumericPolicyFactory.Build(candidate)`

The catch-all `_ => Build()` arm (matching any unrecognized `(distance, level, days)` tuple, including any future non-TEN_K distance) is now `("TEN_K", _, _) => Build()` followed by `_ => throw CatalogVolumeUnsupportedDistanceFamilyException(...)`. Zero delta: the only TEN_K identity that legitimately reaches this arm today (`TEN_K/INTERMEDIATE/4`, the pilot cell's own Runway numeric authority) is unaffected.

### 4.4 THIRD occurrence found by this phase's own deeper search — `CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`

Not caught by `HM.0`'s original audit (which inspected only the two sites above). This private helper — used by the Core-path final-prescribed-plan validation step, reachable for every real generation, not Runway-scoped — dispatches purely on `Level`/`DaysPerWeek` with **zero `CanonicalDistanceFamily` check anywhere in the method**, and its final arm (`_ => VolumeSafetyPolicy.Default.LongRunHardCapShare`) would silently apply TEN_K's own long-run hard-cap share to any future distance family's Intermediate/4D (or any other unmatched `DaysPerWeek`) request with no error — the exact Family 1 shape, root-caused the same way as the other two: a "structural-session-count-shaped" dispatcher whose designer reasoned about `Level`/`DaysPerWeek` cardinality and never considered a second distance could reach the same cardinality. **Root cause, common to all three sites**: `PlanCatalogCandidateSummary.CanonicalDistanceFamily` is a plain string field that participates in *some* of a dispatcher's branches but not structurally enforced to participate in *all* of them — nothing in the type system forces every `Level`/`DaysPerWeek`-keyed switch to also key on distance, so a reviewer must notice the omission by inspection each time. Fixed with a single top-level guard (`if (CanonicalDistanceFamily != "TEN_K") throw ...`) before the existing `Level`/`DaysPerWeek` dispatch — zero delta for every existing TEN_K caller, fails closed for anything else.

### 4.5 Deeper search performed, no fourth instance found

Searched: `DynamicCoreVolumeAndLongRunOrchestrator` (pure DI composition wrapper around the already-fixed planner — no dispatch of its own); `CatalogSessionPrescriptionPlanner` (its one `Level == "NEW"` reference gates a boolean flag for taper-minima selection within an already-`DaysPerWeek`-gated 3D branch — not a distance-blind numeric-authority fallback, a different shape); `LongHorizonGeNumericExecutor.Execute`'s `policy ?? VolumeSafetyPolicy.Default` (an explicit optional-parameter default, documented as reproducing every pre-existing 4D caller byte-for-byte, not a `Level`/`DaysPerWeek`-keyed dispatch with a silent fallback arm — a different shape, and LongHorizon-scoped, unreachable for this Core-only 14-week slice per `HM.0 §B`/`§C`'s own finding); `TenKPreparationRunwayComponentAdapters.cs`'s sole `CanonicalDistanceFamily` reference is a pass-through field copy, not a dispatch. No fourth instance of Family 1's specific shape was found on the Core-only reachable path.

**Behavior-preservation and fail-closed proof (real tests)**:
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Phase4F7BVolumeAndLongRunTests.cs` — new test `Hm2Step1c_UnrecognizedHalfMarathonDistanceFamily_FailsClosed_InsteadOfSilentlyReusingTenKDefault`: builds a full, real `CatalogPrescriptionContextBuilder` + `CatalogVolumeAndLongRunPlanner` pipeline for a synthetic `HALF_MARATHON`/Intermediate/4D request (matching `GoalDistance`/`CanonicalDistanceFamily` throughout so Step 1b's resolver succeeds) and asserts it now throws `CatalogVolumeUnsupportedDistanceFamilyException` — proving the fix end-to-end through the real prescription-context-building path, not merely at the unit level.
- `backend/RunningApp.IntegrationTests/RuntimeCatalog/HalfMarathon/Hm2Step1cFailClosedDistanceBlindFallbackTests.cs` — `TenKPreparationRunwayNumericPolicyFactory_TenKIdentities_StillResolve_ZeroDelta` (8 TEN_K cells, all still resolve); `TenKPreparationRunwayNumericPolicyFactory_UnrecognizedDistanceFamily_FailsClosed` (HALF_MARATHON and a synthetic `UNKNOWN_FUTURE_DISTANCE` both now throw); `CatalogFinalPrescribedPlanValidator_ResolveLongRunHardCapShare_TenKIdentities_StillResolve_ZeroDelta` (10 TEN_K `Level`/`DaysPerWeek` cells, invoked via reflection since the method is private, all still resolve to their unchanged share value); `CatalogFinalPrescribedPlanValidator_ResolveLongRunHardCapShare_UnrecognizedDistanceFamily_FailsClosed` (both synthetic families now throw).

All passed on first run (§7).

---

## 5. Recurring-assumption-family search across everything this slice touches

Beyond Family 1 (§4), searched the Core-only critical path (`DynamicCoreWeekSkeletonOrchestrator`, `DynamicCoreWorkoutBindingOrchestrator`, `DynamicCoreSessionPrescriptionOrchestrator`, `DynamicCoreCalendarMaterializationOrchestrator`, `CatalogPreviewGenerator`) for any other hardcoded-`TEN_K`/`GoalDistance.TenK` literal not already catalogued by `HM.0 §B`/`§F`. No new instance found beyond the three already fixed in §4 and the two already-mechanical items fixed in §2/§3 (`V1CatalogPilotIdentityPolicy`, `CatalogGoalDistanceResolver`). The Preparation-Runway/LongHorizon `TenK*` subtree (~150 files, `TenKPreparationRunwayDarkOrchestrator.ValidateRequest`'s three hardcoded-10K checks named in `HM.0 §F.4`) remains **out of scope** — unreachable for a 14-week Core-only request, confirmed unchanged this phase, not touched.

---

## 6. Step 2 — vertical slice: what was implemented, and the precise, disclosed remainder

Per §1's finding, Step 2 is executed only as far as frozen authority permits, then stopped at the numeric-authority gap rather than improvised past it.

**Implemented and real (dark-only, zero catalog authoring, zero public routing change)**:
- **Identity → eligibility**: the Step 1a widening itself IS this stage for the dark path — `ResolveCandidate(GoalDistance.HalfMarathon, Intermediate, 4)` resolves the frozen cell; every other Level/Frequency/Distance combination correctly fails.
- **Horizon classification**: `CoreHorizonClassifier` (already `HM.0 §D`-classified `REUSED_AS_IS`, zero distance literals) requires no code change; not independently re-tested this phase since no HM-specific behavior exists in it to test beyond what 10K's own existing `CoreHorizonClassifier` test coverage already exercises generically.
- **Phase allocation**: `HM.1`'s frozen `3/4/5/2` split is structurally representable by the existing `PlanCatalogPhaseAllocation` type unmodified (verified by direct construction in the new tests' `MinimalCandidate` helper, §4's tests) — `3+4+5+2=14`, consistent with the frozen `CoreCycle`.

**NOT implemented, precisely disclosed, not invented around**:
- **No HM catalog artifacts were authored** (`WORKOUT_PROGRESSION`, `PLAN_TEMPLATE`/master, `TEMPLATE_COMBINATION`, or the new HM-parameterized `HM_PACE` workout definition `HM.1.2 §12` classified `EXISTING_WORKOUT_REQUIRES_HM_PARAMETERIZATION`). Reason: `PlanCatalogCandidateSummary.PeakVolumeBandPolicy` and the volume-planning pipeline's `CatalogPeakVolumeBand`/`VolumeSafetyPolicy`-equivalent numeric authority are structurally required by any real candidate artifact, and §1 establishes that authority is not yet frozen. Authoring a candidate that references a peak-volume band or growth-ratio policy this phase would have to invent violates the explicit "do not improvise" constraint. `HM.1.2`/`HM.1.3`'s own workout-progression content (stage catalog, exposure counts, dose envelopes) IS fully frozen and ready to author — but authoring it into an orphaned catalog document with no loadable candidate to bind it to would not constitute real, verified progress, only unverifiable paperwork.
- **No numeric policies / volume / long-run stages were implemented** for HM — this requires the `VolumeSafetyPolicy`-shaped authority §1 shows was never frozen.
- **No workout binding, calendar materialization, or preview-dark materialization was exercised for HM** — each of these stages requires a real bound catalog plan, which requires the candidate artifact above.
- **The Build stage's eligibility-driven exposure split (1/3 vs 0/4), RaceSpecific's fixed 1/2/2, and Taper's fixed 2/2** (all frozen, `HM.1.3`) were **not wired into runnable code** this phase — there is no `ProgressionStageAllocator`-equivalent HM stage-allocation implementation yet, because (as `HM.1.2 §15` itself anticipated) that implementation only becomes meaningful once real catalog `WORKOUT_PROGRESSION` content exists to allocate against, which is blocked by the same numeric-authority gap above (a `WORKOUT_PROGRESSION` document is loaded as part of a `PlanCatalogCandidateSummary`, which requires the full candidate, which requires `PeakVolumeBandPolicy`).
- **Real PostgreSQL lifecycle test (GeneratePreview → confirm → fresh reload) for `HALF_MARATHON × INTERMEDIATE × 4D × 14W` was NOT run and could not honestly be run to a successful outcome** — there is no loadable catalog candidate for this identity (confirmed: `HalfMarathonFourDayIntermediateCandidateKey` resolves in-memory via the new overloads, but `PlanCatalogBundleLoader`/`CatalogArtifactFileResolver` would fail to find any corresponding artifact file, since none was authored, per the constraint above). Attempting this test would either (a) fail with a file-not-found-shaped error that proves nothing about the numeric-authority gap specifically, or (b) require authoring a fabricated candidate with invented numeric authority to make it pass, which is exactly what this phase must not do. Instead, §4's tests prove the **specific, correct failure mode** the real pipeline will hit once a candidate does exist: `CatalogVolumeUnsupportedDistanceFamilyException` at the volume-planning stage, not a silent wrong-numbers success and not an unrelated crash.

**Honest classification of Step 2**: `DONE (PARTIAL)`. The identity/eligibility seam is real, tested, and dark-only. Horizon and phase-allocation shapes are confirmed structurally compatible. Everything downstream of volume/long-run numeric authority is correctly and precisely blocked, not improvised past.

---

## 6.1 A real regression found by the full-suite run, root-caused and fixed — not hidden

The first full `RunningApp.IntegrationTests` run (before this section was written) surfaced **5** failures, not the expected 4 named baseline items. Isolating the 5th (`Phase4F7APrescriptionContextTests.GoalDistanceResolver_DerivesTenKAndRejectsMismatches`) found a genuine, disclosed side effect of Step 1b: this pre-existing test asserted `CatalogGoalDistanceResolver.Resolve("TEN_K", GoalDistance.HalfMarathon)` throws `UNSUPPORTED_REQUEST_GOAL_DISTANCE`. Step 1b's additive `GoalDistance.HalfMarathon` arm on the *request*-side switch (necessary so a genuine, internally-consistent HALF_MARATHON request can ever resolve a km value at all) means this exact input now legitimately reaches the *mismatch* check instead (`catalogKm=10` for `"TEN_K"` vs. `requestKm=21.0975` for `HalfMarathon` — a real disagreement, correctly caught) and throws `GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH` instead.

**This is not a 10K production behavioral delta**: the `("TEN_K", GoalDistance.HalfMarathon)` combination this test constructs is not, and never was, a reachable real request shape — `V1CatalogPilotIdentityPolicy`'s identity gate guarantees `request.GoalDistance` always matches the loaded candidate's own `CanonicalDistanceFamily` before `CatalogGoalDistanceResolver` ever runs, for every real 10K request past or present. The test was exercising the resolver's internal defensive logic directly, with a synthetic, always-invalid combination, and asserting one specific one of two plausible failure codes for it. Reverting Step 1b's request-side widening to avoid this would defeat Step 1b's own purpose (a genuine future HALF_MARATHON request must be able to resolve a km value through this same sole-authority resolver).

**Fix applied**: `Phase4F7APrescriptionContextTests.cs` — the "unsupported" assertion now uses `GoalDistance.Marathon` (genuinely unsupported by either resolver arm, preserving the test's original intent unchanged), and a new, separate assertion explicitly covers the intentional new behavior: `Resolve("TEN_K", GoalDistance.HalfMarathon)` now correctly throws `GOAL_DISTANCE_REQUEST_CATALOG_MISMATCH`. Re-run confirmed **78/78** passing (up from 77, the one updated test now covering one more explicit case) with **zero** remaining unexplained failures. This is disclosed here in full rather than silently folded into the "COMPLETE" narrative above — it is exactly the kind of subtle side effect this engagement's own regression discipline exists to catch before it reaches a ledger claim.

---

## 7. Full regression — both suites

**Process-check method** (per this engagement's carried-forward operational note): `tasklist`/`wmic process where "name='testhost.exe'" get processid` run before starting each full-suite invocation to confirm no concurrent run against the same PostgreSQL test database; `PlanCatalog.Tests` has no PostgreSQL/Npgsql dependency (confirmed by source/csproj search) and was run concurrently with `RunningApp.IntegrationTests` deliberately, not by oversight.

- **`PlanCatalog.Tests`**: **1510/1510 passing, 0 failed** — exact match to the known baseline, zero regressions (run concurrently with the backend suite; confirmed to have zero PostgreSQL/Npgsql dependency by source/csproj search first, so concurrency was deliberate, not an oversight of the "never run two full-suite invocations concurrently against the same database" rule).
- **`RunningApp.IntegrationTests`** — run twice:
  - **First run** (before §6.1's fix): `4455 passed, 5 failed, 4460 total`. The 5th failure (`Phase4F7APrescriptionContextTests.GoalDistanceResolver_DerivesTenKAndRejectsMismatches`) was a real, new, disclosed side effect of Step 1b — root-caused and fixed in place (§6.1), not carried forward or hidden.
  - **Second run** (after the fix, isolation re-verified via `wmic process where "name='testhost.exe'" get processid` returning empty before starting): **`4456 passed, 4 failed, 4460 total`**. The 4 failures are byte-for-byte the same named baseline items `GEN.39` itself documented: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks: 13), the same test (weeks: 14), `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_...`, and `LongHorizonActiveReadAndMutationTests.HomeCalendarDetail_ExposeOnlyActivatedSessions_WithStableIdentity`. **The 4th, date-coincidental failure still appears** — today's date (`2026-09-07`) is exactly the calendar date `GEN.38`/`GEN.39` identified as the trigger for this test's own hardcoded-date-literal bug ("self-resolves once the real calendar date moves past `2026-09-07`" — today has not yet moved past it). Total test count is `4460` vs. the prior baseline's `4386`, an increase of `74` — this phase's own new/modified tests (two new files plus one appended test plus one modified pre-existing test's added sub-assertion), independently confirmed by the isolated targeted run below. **Zero new regressions**: exactly the same 4 named items, same failure messages, same stack-trace line numbers as the pre-existing baseline.

Targeted, isolated run of every HM.2-related test (both new files, the appended volume-planner test, and the corrected pre-existing resolver test) before the second full run: **78/78 passing**, 141ms (no PostgreSQL required — pure in-memory unit-level construction).

`git diff --check` on every changed file: clean (no trailing-whitespace/conflict-marker errors; only pre-existing CRLF/LF line-ending advisories on files this phase did not create the line-ending convention for).

---

## 8. What this phase explicitly did NOT do

- Did not invent any HM-specific `VolumeSafetyPolicy` value, peak-volume-band number, starting-volume default, or long-run-share figure — the one item `HM.0 §I` item 2 flagged and no subsequent phase closed.
- Did not author any catalog artifact (`WORKOUT_PROGRESSION`, `PLAN_TEMPLATE`, `TEMPLATE_COMBINATION`, or a new `HM_PACE` workout definition) — correctly barred by the numeric-authority gap, not by phase-boundary policy alone.
- Did not widen `IsSupportedIdentity`, `IsSupportedPreparationRunwayIdentity`, or any other public/Runway gate — verified by dedicated tests (§2), not merely by claim.
- Did not touch any 10K-reachable numeric value — every fixed dispatcher's TEN_K arm is proven byte-identical by direct test.
- Did not touch the ~150-file Preparation-Runway/LongHorizon `TenK*` subtree.
- Did not force a fabricated "full completion" — Step 2's real remainder is disclosed precisely in §6, with the exact next micro-decision needed (§9).

---

## 9. Recommended next phase

1. **A direct product decision closing `HM.0 §I` item 2** — the `VolumeSafetyPolicy`-shaped numeric authority for `HALF_MARATHON × INTERMEDIATE × 4D`: peak-volume band (the handoff's own §4.2 candidate, 36–50 km/week, is available to adopt or reject explicitly — it is not yet repository authority), `PreferredMaxWeeklyIncreaseRatio`/`HardMaxWeeklyIncreaseRatio`/`AbsoluteWeeklyIncrementCapKm` (HM-calibrated, not copied from 10K's `0.07d`/`0.08d`/`2.5km`), `TaperVolumeMultiplier` (not copied from 10K's `0.53d`), starting-volume missing/explicit-zero defaults, and long-run share bounds (distinct from the already-frozen 19.0km absolute ceiling). Mirrors `HM.1.1`'s own direct-decision-shaped resolution pattern — this is plausibly a single decision message, not a new research phase, given `HM.1.1`/`HM.1.3` already established the precedent of freezing `EVIDENCE_INFORMED_PRODUCT_DEFAULT` values from convergent external evidence.
2. Only after that closes: author the real HM catalog artifacts (candidate/combination, master template with the frozen phase allocation and eligibility matrix, `WORKOUT_PROGRESSION` document encoding `HM.1.2`/`HM.1.3`'s frozen stage catalog and exposure counts verbatim, the new `HM_PACE` workout definition parameterizing `GOAL_PACE_TEN_K`'s already-generic schema) and re-run this phase's disclosed remainder (workout binding, volume/long-run planning, calendar materialization, the real PostgreSQL `GeneratePreview → confirm → fresh reload` lifecycle test) to completion.
3. This phase's own Step 1 work (distance-parameter threading, duplicate-authority resolution, fallback closure) requires no further action — it is complete, tested, and does not need to be revisited when the numeric-authority decision above closes.

---

## Final classification

```
HM_2_STEP_1_PREREQUISITES_COMPLETE
HM_2_STEP_2_VERTICAL_SLICE_DONE_PARTIAL
BLOCKED_ON_UNFROZEN_VOLUME_NUMERIC_AUTHORITY (HM.0 §I item 2, never closed by HM.1/HM.1.1/HM.1.2/HM.1.3)
```

Step 1 (1a/1b/1c) is fully complete, real, and proven zero-delta for 10K by direct test — including a third instance of the `HM.0 §G` Family 1 recurring-assumption family this phase's own deeper search found and fixed (`CatalogFinalPrescribedPlanValidator.ResolveLongRunHardCapShare`), not caught by `HM.0`'s original audit. Step 2 is honestly `DONE (PARTIAL)`: the identity/eligibility seam is real and dark-only tested; the volume/long-run/workout-materialization/calendar/preview stages are correctly blocked on a numeric-authority gap this phase discovered was never closed by any prior phase, disclosed precisely rather than improvised past. No 10K behavior changed (proven by test, not asserted). No public routing or gate widened. No catalog artifact authored.
