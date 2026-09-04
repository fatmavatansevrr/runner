# PHASE 10K-GEN.29 — 2D Preparation Runway: Block-Role Reconciliation Implementation

**Phase type**: IMPLEMENTATION + DEFECT-FAMILY SEARCH + REAL DARK VERIFICATION (no HTTP, no public gate change, no PostgreSQL persistence)
**Parent authority**: `GEN.28` (`DOMAIN_DECISION_REQUIRED` for `AerobicStrength` content combined with `2D_RUNWAY_BLOCK_ROLE_RECONCILIATION_APPROVED` for the mechanism and the other three blocks — the direct predecessor whose §14 implementation contract this phase executes), `GEN.27` (repeating-pattern SELECTION mechanism, `DONE (PARTIAL)`, whose `NotSupportedException` guard this phase removes), `GEN.11` (frozen 2D numeric authority — 55%/60% long-run share, `Beginner2D`/`Intermediate2D` `VolumeSafetyPolicy`), `GEN.20`/`GEN.23`/`GEN.25` (this engagement's "run the full regression, verify clean independently, disclose real defects with root-cause rigor" discipline)
**Execution status**: DONE
**This phase implements a direct user decision message ("DECISION ON GEN.28"), verbatim, resolving GEN.28's one open `DOMAIN_DECISION_REQUIRED` item and authorizing the full GEN.28 §14 contract.**

---

## 0. Preflight

`git log -5`: HEAD `6cf64fb` (`docs(gen-28): backfill governance commit SHA for GEN.28`). `git fetch && git rev-list --left-right --count origin/main...HEAD`: `0` behind — in sync. `git status --porcelain`: only the pre-existing `bin`/`obj` rebuild noise, `baseline_tmp`, `ten-k-pilot-domain-decision-audit.*`, and a pre-existing, untouched worktree directory (`.claude/worktrees/agent-acf98eac352b9f4dd`) — none touched by this phase. `PHASE_LEDGER.md` last row: `GEN.28` (row 131). Next free Phase ID confirmed by direct listing of `PHASE_10K_GEN_*.md`: highest existing is `GEN.28`; **`GEN.29` confirmed correct.**

---

## 1. Frozen decision — implemented exactly as specified

> Pattern-A weeks: use the existing, genuine AerobicStrength `KEY_SESSION`/QUALITY-family content (`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED`), per the already-approved block-local-week progression — unchanged.
> Pattern-B weeks: use `EASY_STANDARD` as `EASY_SUPPORT`. No aerobic-strength-specific content on these weeks.
> Classification: `AEROBIC_STRENGTH_2D_PATTERN_B_CONTENT = EASY_STANDARD, APPROVED_PRODUCT_DEFAULT, WITH_EXPLICIT_STIMULUS_REDUCTION, AND_ZERO_NEW_CONTENT_AUTHORING`

**What this is NOT** (recorded verbatim in spirit, per the decision message's own requirement):
- NOT scientifically equivalent to the AerobicStrength KEY workout.
- NOT a hidden substitute for QUALITY-family content.
- NOT a new training-methodology claim.
- NOT a change to AerobicStrength's behavior for any other frequency — 3D/4D/5D/6D (either level), where this block is not constrained by a Pattern-A/B structure, keep their existing, unmodified behavior. This decision is 2D-only. **Verified, not merely asserted** — see §8.

**Option (a)** (author a new EASY-family aerobic-strength-adjacent workout) — **REJECTED / NOT AUTHORIZED**. No new `WorkoutDefinition` was created; both Pattern-A and Pattern-B content resolve to catalog documents that already existed before this phase (`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` v1, `EASY_STANDARD` v5).

**Option (c)** (steer `AerobicStrength`'s block-local allocation onto Pattern-A weeks) — **NOT EVALUATED, NOT APPROVED, NOT IMPLEMENTED**. No allocation-policy weight, priority, or eligibility for `AerobicStrength` was touched (`TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies` is byte-identical to before this phase).

---

## 2. Implementation — mechanism (GEN.28 §9 Candidate C)

### 2.1 Catalog reader — the `.First()` hardcode fix

`PreparationRunwayBlockProgressionCatalogReader.ReadStep` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayWorkoutBinding/PreparationRunwayBlockProgressionCatalogReader.cs`) previously hard-selected `candidatesEl.EnumerateArray().First()`, silently ignoring any further `workoutCandidates` array entry (GEN.28 §7's own finding — the schema was already plural, the reader was not). Now reads the full array: the primary candidate is the first entry carrying no `"role"` tag (falling back to the array's first entry when every candidate is untagged, preserving byte-identical selection for every pre-GEN.29 progression document); an optional second entry tagged `"role": "EASY_SUPPORT"` is captured separately into a new `PatternBEasySupportReference` field. An unrecognized `"role"` value throws `PlanCatalogLoadException` rather than being silently ignored (fail-closed, per this engagement's standing discipline).

### 2.2 Binding engine — threading the alternate through

`PreparationRunwayBlockProgressionStep` (`PreparationRunwayBlockWorkoutBindingEngine.cs`) gained a new optional trailing field, `PreparationRunwayWorkoutReference? PatternBEasySupportReference = null` — null for every step that declares only an untagged candidate (every pre-GEN.29 progression). `PreparationRunwayBlockWorkoutBinding<TKey>` gained a parallel `IReadOnlyList<PreparationRunwayWorkoutReference?>? OrderedPatternBEasySupportReferences = null`, index-aligned with `OrderedWorkoutReferences`. `Bind`'s Step 5 (exact-prefix selection) now also selects this parallel list from the same prefix of canonical steps; Step 1 (request validation) also validates the alternate reference's shape when present. Both additions are optional/nullable, so every existing positional construction of these records (in `PreparationRunwayWeekMaterializerTests.cs` and elsewhere) continues to compile and behave identically.

### 2.3 Materializer — the real role-conditioned redirection

`PreparationRunwayWeekMaterializer.MaterializeAsync` (`PreparationRunwayWeekMaterialization/PreparationRunwayWeekMaterializer.cs`) is where the actual mechanism lives, deliberately placed here rather than in the binding engine (per GEN.28 §7's own observation that binding happens before per-week role resolution): after `weekRoles` is resolved for the current runway week (`ResolveWeekRoles`), if the block-local week's fixed `anchorRole` (from `BlockRolePolicies`) is **not** present in `weekRoles`, the materializer looks up the progression step's `PatternBEasySupportReference`. If found (and `EasySupport` is present in `weekRoles`), the anchor role and content are redirected to `EasySupport`/that alternate reference; otherwise it fails closed with a named `AnchorRoleIncompatible` error, never a silent wrong answer.

This is a pure conditional branch guarded by `!weekRoles.Contains(anchorRole)`. For every pre-GEN.29 (non-2D) layout, `weekRoles` is the fixed `OrderedRoles` set, which by construction always contains the block's fixed anchor role (`KeySession` or `LongRun`) — the branch is structurally unreachable there, byte-identical behavior, confirmed by the full regression (§9) rather than assumed.

### 2.4 Catalog content — no new content, only new tags

Per the frozen decision and GEN.28's own finding that `Consistency`/`PreSpecificTransition`'s existing content already satisfies Pattern B verbatim:

- `ten-k-consistency-progression.v1.json` step 1: added a second `workoutCandidates` entry, `{EASY_STANDARD v5, role: EASY_SUPPORT}` (identical key/version to the primary entry — same content, tagged for the alternate role).
- `ten-k-pre-specific-transition-progression.v1.json` step 1: identical addition.
- `ten-k-aerobic-strength-progression.v1.json` steps 1 and 2: added `{EASY_STANDARD v5, role: EASY_SUPPORT}` to each — per the frozen decision, this is the one case where the alternate content genuinely differs from the primary (`AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED`, QUALITY family) rather than merely being re-tagged.
- `ten-k-general-endurance-progression.v1.json`: **unchanged** — confirmed zero conflict (its `LongRun` anchor role is present in every 2D week regardless of pattern, so the redirection branch never triggers for this block).

All four documents remain `version: 1`, `status: DRAFT` (unchanged from before this phase) — additive, backward-compatible schema use, not a new document version, since the `workoutCandidates` array was already schema-plural and no existing consumer read anything beyond index 0.

### 2.5 Removing GEN.27's guard

`TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies` no longer branches on `daysPerWeek` at all — the `NotSupportedException` for `daysPerWeek == 2` is gone. This is not a narrowing of a fail-closed guard into a silent wrong answer (which this engagement's own discipline forbids): the anchor-role dictionaries (`AnchorRoleByProgressionStep`) were never actually frequency-dependent — they are a pure function of a block's own progression shape, identical for every frequency. The real fix that makes 2D correct lives entirely in §2.3's materializer redirection; this method's `daysPerWeek` parameter is now purely for call-site/provenance-signature compatibility.

---

## 3. Implementation — Beginner admission gate (GEN.28 §10)

`TenKPreparationRunwayDarkOrchestrator.ValidateRequest` (`PreparationRunwayOrchestration/TenKPreparationRunwayDarkOrchestrator.cs`): the `request.Candidate.Level is not ("INTERMEDIATE" or "ADVANCED")` check is replaced with an explicit switch admitting `"NEW"` (the catalog's own Beginner experience label, GEN.4A) **only when `DaysPerWeek == 2`** — Beginner×3D/4D Runway remain excluded (no prior phase ever designed or approved them; `IsSupportedPreparationRunwayCandidate` also never recognizes `ThreeDayBeginnerCandidateKey`/`BeginnerCandidateKey`, an independent fail-closed layer). The sibling `expectedLevel` two-way ternary (`"ADVANCED" ? Advanced : Intermediate`) had no Beginner branch at all — a second instance of the identical defect family GEN.10 first diagnosed for Advanced — fixed to a three-way switch (`"ADVANCED" => Advanced, "NEW" => Beginner, _ => Intermediate`).

`V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayCandidate` (the internal dark-consistency check, explicitly documented as distinct from the public gate) was widened to recognize `TwoDayBeginnerCandidateKey`/`TwoDayIntermediateCandidateKey`. `IsSupportedPreparationRunwayLevelFrequency`/`IsSupportedPreparationRunwayIdentity` (the real public routing gate) were **not touched** — confirmed by direct inspection and by the full regression's zero-delta on every existing public-routing test.

---

## 4. Implementation — 2D numeric-policy dispatch (GEN.28 §11)

`TenKPreparationRunwayNumericPolicyFactory.Build(PlanCatalogCandidateSummary)` gained two new switch branches: `("TEN_K", "NEW", 2) => Build(VolumeSafetyPolicy.Beginner2D, 0d, 0d)` and `("TEN_K", "INTERMEDIATE", 2) => Build(VolumeSafetyPolicy.Intermediate2D, 0d, 0d)`, mirroring the existing Advanced dispatch pattern exactly (the two default-km parameters are dead values for 2D, since `GEN.11`/`GEN.12` already froze `TwoDayMissingOrZeroReadinessProductIneligibleException` — no missing/zero-readiness default is ever resolved for 2D at either level, re-confirmed unmodified in `CatalogVolumeAndLongRunPlanner`). No new number invented — `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D` (55%/60% long-run share, peak-volume-band-derived growth calibration) already existed, frozen by `GEN.11 §6`/`GEN.12`.

**A real defect found via dark verification, not theorized**: the private `Build(VolumeSafetyPolicy, ...)` helper derives `LongRunShareTolerance` — for policies whose `LongRunPreferredMinimumShare == LongRunSelectionShare` (a "zero nominal gap" shape, already identified for `FiveDayIntermediate`/`SixDayIntermediate`/`Advanced5D`/`Advanced6D`), it uses a wider, rounding-derived tolerance instead of the default tight `0.001` (effectively 0.1%) tolerance. `Beginner2D`/`Intermediate2D` have exactly this same shape (`LongRunPreferredMinimumShare == LongRunSelectionShare == 0.55`, GEN.11 §6/§8) but were not included in the reference-equality condition when this phase's dispatch branches were first added — a real Week 1 long-run share landed ~1.4 percentage points off the exact 55% floor purely from independent 0.5km rounding of weekly volume and long run, and was correctly rejected by the tight default tolerance. Fixed by adding `Beginner2D`/`Intermediate2D` to the same, already-established reference-equality condition — applying an existing, already-approved rule to a shape it always should have covered, not inventing a new tolerance value.

---

## 5. Implementation — calendar composition and long-run clamp (GEN.28 §12)

No hardcoded 2-slot blocker was found in `PreparationRunwayCalendarComposer`/`PreparationRunwayCalendarSkeletonAdapter` (confirmed by direct inspection, matching GEN.28's own finding) — `DaysPerWeek = prescribed[0].OrderedSlots.Count` already derives the slot count generically. Real dark end-to-end verification (§9) confirms a real 2-slot Runway week composes correctly, and the frozen 55%/60% long-run share clamp is applied and satisfied for every prescribed 2D week (§9's `Beginner_TwoDay_CalendarComposition_TwoSlotWeeks_AndLongRunClampApplied` test, both a 15wk and a 20wk horizon).

---

## 6. Real defects found during implementation — full disclosure

Per this engagement's standing "search for and disclose the recurring hardcoded-assumption family" discipline, real dark end-to-end verification (not unit-level isolation alone) surfaced **six** additional, previously-undisclosed-or-newly-reachable instances of the "not every caller shape considered" family this engagement has repeatedly found (GEN.10 found the first three; GEN.12/GEN.17/GEN.19/GEN.20/GEN.27/GEN.28 each found more) — every one is a real bug reached by constructing an actual full orchestration for 2D for the first time, not a theoretical concern:

1. **`PreparationRunwayNumericMaterializer.ValidateRequest`**'s per-week shape check consulted only `PreparationRunwayWeeklyShape.IsValid` (the standard 1 KEY + 1 LONG + N EASY shape), rejecting every real 2D Model B week outright. Fixed to accept `IsValidTwoDayModelB` too, mirroring `PreparationRunwayWeekMaterializer.ValidateWeekCardinality`'s own already-correct dual-shape check.
2. **`PreparationRunwayNumericMaterializer`**'s session-distance-allocation call site never passed the real `keySessionCount` to `FourDaySessionDistanceAllocationPolicy.Allocate` (only `easySupportCount`), silently defaulting to 1 — on a 2D Pattern-B week (zero real KEY_SESSION slots) this reserved volume for a phantom KEY session that never appeared in any output slot, producing a rounded-slot-sum shortfall against the weekly total. Fixed by reading `keySessionCount` from the real materialized week, exactly as `easySupportCount` already did.
3. **`PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior`**'s `easyCount < 1` floor — **explicitly disclosed by `GEN.27 §1`'s own recurring-defect-family search as "still open, unreachable for 2D (no 2D Runway→Core continuity path exists yet at the numeric/pace layer)"**. This phase's real orchestration makes that path reachable for the first time, and the guard threw for every real 2D request (`"Authoritative Core Foundation Week 1 pace target is unavailable."`) since 2D's real Core Week 1 has 1 KEY + 1 LONG, zero EASY. Removed the unconditional `easyCount >= 1` requirement; `keyCount >= 1` and exactly one `LONG_RUN` remain required for every frequency.
4. **`PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior`** defaulted `easySupportCount` to a hardcoded `2` (via `FourDaySessionDistanceAllocationPolicy.Allocate`'s default parameter) rather than reading the real Core Week 1 `EASY_SUPPORT` count, then unconditionally emitted exactly two `EasySupport` slot targets (`FirstEasySupportDistanceKm`/`SecondEasySupportDistanceKm`) regardless of the real week's actual shape — coincidentally correct for every pre-GEN.29 frequency (all of whose real Core Week 1s happen to have exactly 2 EASY_SUPPORT sessions) but silently wrong for 2D (0). Fixed by reading the real count and emitting exactly that many targets, generalizing the same way `keySessionCount` was already generalized (`FREQ.6D.7`).
5. **`PreparationRunwayCalendarComposer`**'s per-week numeric-consistency check (line ~227) had the identical single-shape hardcode as defect 1. Fixed identically.
6. **`TenKPreparationRunwayFinalInvariantValidator`**'s own `structuralExact` cross-component check had the identical single-shape hardcode. Fixed identically.

All six fixes are structurally unreachable/no-ops for every pre-GEN.29 (non-2D) week or shape by construction — confirmed by the full regression (§9), not merely asserted. **The recurring-defect-family search itself** (grep for `keyCount`/`easyCount`/`Level is not`/`DaysPerWeek ==`/`.First()`-shaped patterns across every touched path) found no further instance beyond the six above and the ones already fixed as part of the mechanism/gate/dispatch work in §§2-4.

---

## 7. What this is NOT — explicit disclosure (verbatim in spirit, per the decision message)

- **NOT** scientifically equivalent to the AerobicStrength KEY workout. `EASY_STANDARD` (family `EASY`) is a materially lower, different stimulus than `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` (family `QUALITY`, Runway-exclusive `eligiblePhases`, `CONTROLLED_AEROBIC_POWER` intensity descriptor) — no claim of equivalence is made anywhere in code, catalog, or provenance metadata.
- **NOT** a hidden substitute — `AerobicStrength` Pattern-B weeks are structurally and observably `EasySupport`-role slots (`SlotRole == EasySupport`, not `KeySession`), plainly visible in every materialized week's own structural metadata; nothing disguises the reduction.
- **NOT** a new training-methodology claim — no new `WorkoutDefinition`, `WorkoutPrescriptionProfile`, or intensity descriptor was authored; the mechanism only selects among content that already existed.
- **NOT** a change to `AerobicStrength`'s behavior for 3D/4D/5D/6D (either level) — see §8 for direct verification.

---

## 8. Zero-delta confirmation — verified, not assumed

- **`AerobicStrength` on every non-2D frequency**: `BuildBlockRolePolicies`'s `AerobicStrength` anchor-role dictionary (`{1: KeySession, 2: KeySession}`) is identical across all `daysPerWeek` values (`Gen29TwoDayRunwayDarkOrchestrationTests.BuildBlockRolePolicies_ZeroDelta_AcrossAllPreExistingDaysPerWeek`, iterating 2/3/5/6 against the 4D reference). The materializer's role-conditioned redirection branch (`!weekRoles.Contains(anchorRole)`) is structurally unreachable for any non-2D layout, since `weekRoles` there is always the fixed `OrderedRoles` set containing every anchor role by construction — confirmed by the full 4230+-test regression showing zero behavioral change to any pre-existing `AerobicStrength` test.
- **Every other pre-existing Runway/Core/LongHorizon behavior**: the dedicated `PreparationRunway`-filtered regression subset (640 tests, including every pre-existing 3D/4D/5D/6D/Advanced Runway test and `GEN.27`'s own `FourDayRunway_LayoutAndBlockRolePolicies_AreByteIdenticalAfterGen27` byte-identity assertion) passed 640/640.
- **Full solution regression**: see §9.

---

## 9. Real dark verification

**Materializer-level** (`Gen29TwoDayRunwayBlockRoleReconciliationTests.cs`, 10 tests, real catalog, no fabricated content): `AerobicStrength` 2-week block starting Pattern A confirms week 1 = genuine `AEROBIC_STRENGTH_CONTROLLED_INTRO` on `KeySession`, week 2 = `EASY_STANDARD` on `EasySupport`; a 1-week `AerobicStrength` block forced to start on Pattern B confirms the block's content still materializes (never silently dropped); `Consistency`/`PreSpecificTransition` confirmed redirecting correctly on Pattern B and unchanged on Pattern A; `GeneralEndurance` confirmed zero-conflict across both patterns; a regression guard confirms no `AEROBIC_STRENGTH_CONTROLLED_*` content is ever placed on an `EasySupport`/`LongRun` role; progression-step-number semantics confirmed block-local-week (not occurrence-based) across both patterns; a real multi-block (`GeneralEndurance`→`AerobicStrength`) materialization confirms A/B boundary correctness across a block transition, resolved against the real, computed pattern rather than a hardcoded assumption.

**Orchestrator-level, real dark end-to-end** (`Gen29TwoDayRunwayDarkOrchestrationTests.cs`, 33 tests, real `TenKPreparationRunwayDarkOrchestrator.OrchestrateAsync`, real catalog, real Core generation — no HTTP, no PostgreSQL): Beginner×2D and Intermediate×2D each successfully orchestrated across the full 15-20 week horizon × {READY, NOT_READY} readiness-profile matrix (24 real end-to-end runs total, all succeeding, `IsValid` final invariants, every materialized Runway week confirmed a valid 2D Model B shape); Beginner×3D and Beginner×4D confirmed still excluded (`CandidateNotSupported`, the admission-gate widening did not leak); numeric-policy dispatch confirmed resolving to the frozen 55%/60% `GEN.11 §6` authority for both levels, and an unmapped combination confirmed still falling back to the pre-existing Default policy (zero-delta); a real 2-slot Runway week confirmed composing with exactly 2 dated sessions per week and every prescribed week's long-run share confirmed within the 55%/60% band (both a 15wk and 20wk horizon); the `AerobicStrength` Pattern-A/Pattern-B split confirmed inside a real, complete 20-week orchestration (the horizon most likely to allocate `AerobicStrength` across both pattern letters) — every Pattern-A week's anchor is genuine `AEROBIC_STRENGTH_CONTROLLED_*` content on `KeySession`, every Pattern-B week's anchor is `EASY_STANDARD` on `EasySupport`.

---

## 10. Regression verification

Confirmed no other `dotnet`/`testhost` process before the targeted and full runs (`tasklist`/`wmic process` inspection — only persistent MSBuild-node-reuse/VBCSCompiler processes present, the standard post-`dotnet build` residue, not test hosts).

- Targeted `PreparationRunway`-filtered suite: **640 total, 640 passed, 0 failed** (includes every pre-existing 3D/4D/5D/6D/Advanced test plus this phase's 43 new tests).
- Full regression (single, isolated `dotnet test RunningApp.sln`, `tasklist`/`wmic process` confirmed zero other `dotnet`/`testhost` processes both before launch and after completion): **4276 total, 4273 passed, 3 failed**, 36m36s. Total reconciles exactly: `GEN.25`'s own 4230/4227 baseline + `GEN.27`'s 3 new tests (4233/4230, unchanged through `GEN.28`, a decision-only phase with no test-suite run) + this phase's 43 new tests (10 in `Gen29TwoDayRunwayBlockRoleReconciliationTests`, 33 in `Gen29TwoDayRunwayDarkOrchestrationTests`) = **4276 total / 4273 passed**, zero new regressions.
- The 3 failures independently re-confirmed via a targeted, isolated rerun of just those two test classes (26 total, 23 passed, 3 failed, 1m8s): the identical, named, pre-existing baseline failures carried since `GEN.17`/`GEN.18`/`GEN.20`/`GEN.23`/`GEN.24`/`GEN.25`/`GEN.27` — `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13, 14) and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` (the latter's real HTTP 500, `CATALOG_INTERNAL_WORKOUT_BINDING_FAILED: ... 8-week explicit-zero weekly-volume catalog preview remains unsupported`, verified unrelated to any 2D/Runway path this phase touched). Debug build: 0 errors throughout.
- **`PlanCatalog.Tests`** (`plan-catalog/PlanCatalog.sln` — a separate solution/test suite from `RunningApp.IntegrationTests`, initially omitted from this phase's own regression pass, an independent-verification catch, not a self-caught gap): an independent re-verification found `AerobicStrengthPreparationRunwayCatalogTests.ProgressionMapping_ReferencesExistCatalogWorkoutsAtTheDeclaredVersion` failing 1/1510 (expected `"EASY_STANDARD"`, actual `"AEROBIC_STRENGTH_CONTROLLED_PROGRESSED"`). Root cause: this pre-existing test's `key == IntroKey ? intro-file : progressed-file` binary ternary was written when only two candidate keys ever appeared per `AerobicStrength` step; this phase's addition of a real, legitimate third candidate (`EASY_STANDARD`, the Pattern-B alternate, GEN.28 §9 Candidate C) broke that assumption — a real gap in this phase's own test-coverage update, not a defect in the implementation or catalog content itself. Fixed by replacing the hardcoded ternary with the real, general catalog filename convention every workout document already follows (`lowercase-kebab-case key + ".v{version}.json"`, confirmed directly against the real files on disk) — generalizes to any future legitimate candidate key, not special-cased per key. Checked every other `PlanCatalog.Tests` file referencing `workoutCandidates`/the runway progression documents (`RemainingRunwayBlockCatalogTests.cs`, `LongHorizonGeCatalogCapacityTests.cs`) for the same fixed-2-candidate-shape assumption — all other call sites index `workoutCandidates[0]` explicitly (the primary/untagged candidate, unaffected since this phase's new entries are always appended, never inserted at index 0) or operate on the unrelated Core `ten-k-workout-progression.v5.json` document; no further instance found. Full, isolated `plan-catalog/PlanCatalog.sln` run after the fix: **1510 total, 1510 passed, 0 failed**. This suite is now added to this phase's (and this engagement's going-forward) standard verification checklist alongside `RunningApp.sln`, per this engagement's established pattern since `GEN.20`.

---

## 11. Constraints confirmation

- **No new product/numeric authority beyond `GEN.11`/`GEN.28`/this decision** — every numeric value used (55%/60% long-run share, `Beginner2D`/`Intermediate2D` peak-volume-band calibration, `EASY_STANDARD`/`AEROBIC_STRENGTH_CONTROLLED_*` content references) already existed before this phase.
- **Recurring-defect-family search performed and disclosed** — §6.
- **Zero-delta required for every already-supported frequency/Runway block outside 2D** — §8, verified via full regression.
- **Still dark-only** — no public routing/gate change; `IsSupportedPreparationRunwayLevelFrequency`/`IsSupportedPreparationRunwayIdentity` untouched.

---

## 12. Governance

`PHASE_LEDGER.md` row appended (`GEN.29`). `MASTER_ROADMAP.md`'s 2D axis paragraphs and backlog updated to reflect Preparation Runway's new dark-implemented-and-verified state and the now-met LongHorizon 2D start-gate. Two-commit self-referential SHA-backfill pattern followed. Normal push only — no force, no force-with-lease.

---

## Final classification

**`2D_RUNWAY_BLOCK_ROLE_RECONCILIATION_IMPLEMENTED_AND_DARK_VERIFIED`** — `DONE`. The full `GEN.28` §14 implementation contract is complete against the user's frozen `AerobicStrength` decision: role-conditioned Pattern-A/Pattern-B content selection is implemented and real-dark-verified for all four Runway blocks at both levels across the full 15-20 week horizon; the Beginner admission gate and 2D numeric-policy dispatch are both fixed and verified; calendar composition and the long-run clamp are both real-verified for a 2-slot week; six additional real defects surfaced by genuine end-to-end dark verification (not theorized) are disclosed and fixed; zero-delta for every other frequency and for `AerobicStrength`'s own behavior outside 2D is verified, not assumed. Preparation Runway remains dark-only — public HTTP/PostgreSQL activation and a dedicated LongHorizon 2D implementation phase are both separately-scoped future work, neither scheduled as a Phase ID here.
