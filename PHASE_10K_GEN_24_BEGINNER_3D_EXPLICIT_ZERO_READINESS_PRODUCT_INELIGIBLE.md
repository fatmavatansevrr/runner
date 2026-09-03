# Phase 10K-GEN.24 — Beginner×3D Explicit-Zero Readiness: PRODUCT_INELIGIBLE

**Implementation + defect-family search + dark verification. Resolves `GEN.23`'s own disclosed gap (§5/§9 of that report) via a direct user decision message, whose exact text is the spec for this phase. Implements the same mechanism class `GEN.9` already established for Advanced's missing/zero readiness (`PRODUCT_INELIGIBLE` via a typed exception) for a different Level/Frequency cell — not a new mechanism.**

**Governance commit**: `c57d774` (implementation + ledger row, backfilled self-referentially per this engagement's established two-commit pattern).

---

## 0. Precondition verification

`git log -5` confirmed HEAD at `d7487d8` (`docs(gen-23): backfill governance commit SHA for GEN.23`). `git fetch` + `git diff HEAD origin/main` confirmed 0 ahead/0 behind — HEAD, `origin/main`, and `d7487d8` all identical before this phase began. `PHASE_LEDGER.md`'s last row is Seq 126, `GEN.23`; no `GEN.24` row or `PHASE_10K_GEN_24_*.md` file existed anywhere in the repository before this phase. Confirmed `GEN.24` as the correct next-free ID from repository truth, not assumed from the governing prompt's own guess.

This phase does not touch, reopen, or modify: Beginner×4D's own explicit-zero handling (`GEN.4C`/`GEN.4D`, verified zero-delta §6), Intermediate×3D (`GEN.2B`/`GEN.3A`/`GEN.3B`, verified zero-delta §6), Beginner×5D (`GEN.22`, `PRODUCT_NON_SUPPORT`, untouched), any 2D authority (`GEN.11`-`GEN.20`), Advanced (`GEN.7`-`GEN.10`), or `V1CatalogPilotIdentityPolicy`'s public allow-list (Beginner×3D Core remains internally gated — verified §8).

## 1. Required reading (verified, not paraphrased from memory)

- `PHASE_10K_GEN_23_BEGINNER_3D_CORE_TAPER_MINIMUM_IMPLEMENTATION.md` — read in full. Its §5 disclosed the exact gap this phase closes: explicit-zero readiness (9.5km start, reused verbatim from Beginner×4D) is non-representable at all 7 governed Core horizons because it sits below the unchanged 12.0km normal-week floor, so week 1 itself is infeasible independent of horizon or GEN.23's own taper-minima mechanism. Two distinct raw failure shapes were disclosed: weeks 8-11 threw `BeginnerThreeDayCoreProductIneligibleException` (the taper-eligibility gate firing first); weeks 12-14 threw a raw, untyped `CatalogSessionPrescriptionInfeasibleException` ("Week 1 is below the 12km 3D direct-prescription floor").
- `PHASE_10K_GEN_9_ADVANCED_3D_4D_5D_6D_COMBINED_IMPLEMENTATION_AND_DARK_VERIFICATION.md` — read in full. §3 shows the exact mechanism this phase reuses: `AdvancedMissingOrZeroReadinessProductIneligibleException`, deriving from the shared `CatalogProductIneligibleException` base already caught generically by `CatalogPreviewGenerator` for HTTP 422 translation, thrown in `CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` before any per-Level default-resolution path runs. `GEN.12`'s `TwoDayMissingOrZeroReadinessProductIneligibleException` (read via `CatalogVolumeExceptions.cs`) is the same mechanism class applied a second time, confirming this is a real, established, repeatable pattern rather than a one-off.
- `PHASE_10K_GEN_21_BEGINNER_3D_CORE_TAPER_MINIMUM_REALIGNMENT.md` and `PHASE_10K_GEN_5C_BEGINNER_3D_CORE_FULL_CLOSURE.md` — read in full, for the exact current/historical classification language this phase corrects (§7 below).

## 2. Recurring-defect-family search (performed before writing any code, per instruction)

Searched every path this decision touches, before writing production code:

- **Files GEN.23 already modified for Beginner×3D**: `CatalogVolumeAndLongRunPlanner.cs`, `CatalogVolumeExceptions.cs`, `CatalogFinalPrescribedPlanValidator.cs`, `CatalogSessionPrescriptionPlanner.cs`, `V1ThreeDaySessionVolumeAllocationPolicy.cs`, `VolumeSafetyPolicy.cs`, `V1BeginnerThreeDayTaperLongRunSharePolicy.cs`, `V1BeginnerThreeDayVolumeEligibilityPolicy.cs` (grep for `ThreeDayBeginner`/`BeginnerThreeDay`, 8 files total, matching GEN.23's own §2 file list exactly). This decision only requires touching `CatalogVolumeAndLongRunPlanner.cs` (`ResolveStartingVolume`) and `CatalogVolumeExceptions.cs` (one new exception type) — a strict subset.
- **The exact insertion site**: `ResolveStartingVolume` already contains two directly analogous, already-approved checks immediately above the insertion point — `GEN.9`'s Advanced missing-or-zero check (`ReferenceEquals(_policy, VolumeSafetyPolicy.Advanced3D) || ...`) and `GEN.12`'s 2D missing-or-zero check (`ReferenceEquals(_policy, VolumeSafetyPolicy.Beginner2D) || ...`), both scoped by exact `ReferenceEquals(_policy, ...)` policy-instance identity, never a broad `Level`/`DaysPerWeek` condition. This phase's new check uses the identical scoping discipline (`ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner)`), so it cannot silently widen to Intermediate×3D, Beginner×4D, or any other cell.
- **No new hardcode-assumption instance found.** This is the 8th time this engagement's `PRODUCT_INELIGIBLE`-via-typed-exception mechanism class has been applied (Beginner×4D explicit-zero-at-short-horizons via `BeginnerFourDayCoreProductIneligibleException`, GEN.4D; Advanced missing-or-zero, GEN.9; 2D missing-or-zero, GEN.12; Beginner×3D taper-volume-below-floor, GEN.23; this phase is the 5th distinct *readiness-state* rejection of this class) — not a fresh defect being fixed, a decision being implemented via an already-proven mechanism.

## 3. Frozen authority (given verbatim by direct user decision, not re-derived)

```
Beginner × 3D  =  SUPPORTED
Missing readiness            = ELIGIBLE   (GEN.23, representable)
Positive observed readiness  = ELIGIBLE   (GEN.23, representable)
Explicit-zero readiness      = PRODUCT_INELIGIBLE   (this decision, GEN.24)
```

Beginner×3D is **not** reclassified as non-support by this phase. It remains `SUPPORTED` at the frequency/identity level (internally gated, exactly as `GEN.23` left it). The distinction this phase implements is at the request/readiness level, mirroring `GEN.9`'s exact pattern for Advanced's missing/zero readiness: "this identity works; a specific request shape does not," never "this identity does not work."

Explicit hard constraints observed throughout, verified not violated anywhere in this phase's diff:
- The reused 9.5km Beginner×4D explicit-zero default is **not** raised, and no new Beginner×3D-specific explicit-zero starting-volume number is invented anywhere.
- `GEN.23`'s taper-specific minima (3.0/2.5/3.0 = 8.5km), `V1BeginnerThreeDayTaperLongRunSharePolicy`, `GEN.21`'s frozen `PeakVolumeBand` ([16,20]km), and the taper multiplier (0.53) are all unchanged.
- Beginner×4D's own explicit-zero handling (`V1BeginnerFourDayMissingReadinessStartingVolumePolicy`, `BeginnerFourDayCoreProductIneligibleException`, `V1BeginnerFourDayVolumeEligibilityPolicy`) is untouched — zero-delta verified §6.

## 4. Implementation

### 4.1 New typed exception

`CatalogVolumeExceptions.cs` — `BeginnerThreeDayExplicitZeroReadinessProductIneligibleException`, deriving from the shared `CatalogProductIneligibleException` base (the same base `AdvancedMissingOrZeroReadinessProductIneligibleException` and `TwoDayMissingOrZeroReadinessProductIneligibleException` derive from — per that base type's own doc comment, "every future candidate cell's ineligibility exception is picked up automatically" by `CatalogPreviewGenerator`, with no corresponding catch-arm edit required). `Reason = "BEGINNER_THREE_DAY_EXPLICIT_ZERO_READINESS_NOT_ELIGIBLE"`.

### 4.2 Dispatch wiring

`CatalogVolumeAndLongRunPlanner.ResolveStartingVolume` — one new check inserted immediately after `GEN.12`'s 2D missing-or-zero check and before the final policy-dispatch ternary chain (the same chain `GEN.23` already generalized to route `ThreeDayBeginner` to `V1BeginnerFourDayMissingReadinessStartingVolumePolicy.Resolve`):

```csharp
if (ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner) &&
    readiness.WeeklyVolume.State == PrescriptionInputState.Available && reported == 0)
{
    throw new BeginnerThreeDayExplicitZeroReadinessProductIneligibleException();
}
```

Scoped by exact policy-instance `ReferenceEquals`, matching every other check in this method — unreachable for Intermediate×3D (`ThreeDayIntermediate` policy), Beginner×4D (`BeginnerFourDay` policy), or any other cell. Missing readiness (`PrescriptionInputState.NotProvided`) is **not** matched by this check and falls through unchanged to the existing ternary dispatch, which still resolves it via `V1BeginnerFourDayMissingReadinessStartingVolumePolicy.Resolve(readiness)` → 12.0km, exactly as `GEN.23` left it.

No other file was touched. No numeric constant, catalog document, schema, or migration was authored or changed.

## 5. Representability re-verification — every governed Core horizon, every readiness state (real, dark, full-pipeline verification)

Re-ran the real, unmodified `DynamicCoreSessionPrescriptionOrchestrator` pipeline (the identical harness `GEN.23` itself established) against the same internally-gated `TEN_K__3D__BEGINNER v1` catalog candidate, for all 7 governed Core horizons (8-14 weeks) and all 3 readiness states relevant here.

| Readiness state | Start | 8 | 9 | 10 | 11 | 12 | 13 | 14 |
|---|---|---|---|---|---|---|---|---|
| Missing readiness | 12.0km | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Positive-observed, band lower (16.0km) | 16.0km | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Positive-observed, band upper (20.0km) | 20.0km | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| Explicit-zero | — (rejected before a start volume is ever resolved) | 422 `PRODUCT_INELIGIBLE` | 422 | 422 | 422 | 422 | 422 | 422 |

**Missing and positive-observed readiness are re-confirmed representable at every governed Core horizon (21/21 real successes)** — zero-delta from `GEN.23`'s own finding, re-run rather than assumed (`Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.MissingAndPositiveObserved_RemainRepresentable_EveryGovernedHorizon_ZeroDelta`, 21 new theory cases, 21/21 pass).

**Explicit-zero now fails closed uniformly at all 7 horizons with one clean, typed rejection** (`Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.ExplicitZero_AllGovernedHorizons_FailsClosed_WithClearTypedRejection`, 7 theory cases, 7/7 pass), replacing `GEN.23`'s two previously-disclosed raw/untyped failure shapes:
- The check fires at readiness-resolution time, **before** the taper-eligibility gate (weeks 8-11's old failure point) or the per-week session-distance floor (weeks 12-14's old failure point) is ever reached.
- The exception is `BeginnerThreeDayExplicitZeroReadinessProductIneligibleException`, assignable to `CatalogProductIneligibleException`, translated by `CatalogPreviewGenerator` to the public `PlanProductIneligibleException` (HTTP 422) shape — the same clean shape every other `PRODUCT_INELIGIBLE` rejection in this codebase already produces, not a generic 500 or an opaque internal error.
- Verified this is **never** silently misrouted to `BeginnerThreeDayCoreProductIneligibleException` (GEN.23's taper-volume exception) or any other type — explicit `Assert.IsNotType` checks in both the updated `Gen23BeginnerThreeDayCoreTests.cs` test and the new `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.cs` suite.

## 6. Zero-delta verification (re-run, not assumed)

- **Beginner×4D**: `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.BeginnerFourDay_ExplicitZeroHandling_IsCompletelyUnaffected_ZeroDelta` re-runs `Gen4DBeginnerFourDayCoreTests`'s own real pipeline for both an ineligible horizon (8 weeks, taper 7.5km, still throws `BeginnerFourDayCoreProductIneligibleException` unchanged) and an eligible horizon (13 weeks, taper 9.5km, still resolves the unchanged 9.5km explicit-zero default) — confirmed never routed to the new Beginner×3D-specific exception. No file under `V1BeginnerFourDayMissingReadinessStartingVolumePolicy.cs`, `V1BeginnerFourDayVolumeEligibilityPolicy.cs`, or `V1FourDaySessionVolumeAllocationPolicy.cs` was touched by this phase.
- **Intermediate×3D**: `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.IntermediateThreeDay_ZeroDelta_Unaffected` re-confirms a real 12-week pilot profile still resolves successfully via the unmodified `ThreeDayIntermediate` code path — the new check's `ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner)` guard makes this unreachable by construction, confirmed empirically rather than only by inspection.
- **Every other frequency/Level cell**: unreachable by construction — the new check is scoped to a single named `VolumeSafetyPolicy` instance identity, exactly matching the discipline `GEN.9`'s Advanced check and `GEN.12`'s 2D check already established at this same call site.
- **Beginner×5D**: untouched — no `SECONDARY_CONTROLLED` lane invented, no 5D code path touched, no file relevant to `GEN.22` modified.

## 7. Historical classification — corrected, exactly as specified

`GEN.5C`'s report text (`PHASE_10K_GEN_5C_BEGINNER_3D_CORE_FULL_CLOSURE.md`) is **not** deleted or rewritten — it stands exactly as written, unmodified, per this engagement's rule. Its finding (`BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL`) remains historically accurate as a statement about the OLD, undifferentiated-floor policy (pre-`GEN.23`), under which Beginner×3D Core genuinely was non-representable at every horizon and every readiness state.

Current, correct classification of the OLD vs. NEW state, recorded here precisely:

- **Old** (pre-`GEN.23`, `GEN.5C`'s own finding): `PROVEN_NON_REPRESENTABLE_UNDER_APPROVED_V1_CORE_POLICY` — still true of the OLD undifferentiated 12.0km-at-every-week floor, not rewritten.
- **New** (as of this phase, `GEN.21`-`GEN.24`): the general representability blocker `GEN.5C` found is resolved for two of three readiness classes by `GEN.23`'s taper-specific minima (missing/positive-observed readiness, `ELIGIBLE`, representable at every governed Core horizon). The remaining explicit-zero case is a **REQUEST-LEVEL READINESS INELIGIBILITY** (`PRODUCT_INELIGIBLE`), not a representability blocker — Beginner×3D Core itself is representable and supported; one specific request shape (explicit-zero readiness) is formally rejected, exactly as Advanced's missing/zero readiness is rejected without Advanced itself being non-support (`GEN.9`).

**It is incorrect to state or record anywhere that "Beginner×3D is non-support for zero-readiness users."** The correct statement, used throughout this report and the ledger/roadmap updates below: *"Beginner×3D is supported; a specific request shape (explicit-zero readiness) is product-ineligible."*

## 8. Public gate — verified unchanged

`V1CatalogPilotIdentityPolicy.IsSupportedIdentity(Race, TenK, Beginner, 3)` still returns `false` — verified by `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.PublicGate_RemainsClosed_BeginnerThreeDayNotWidened`, alongside confirmation that neighboring identities (Beginner×4D, Intermediate×3D) remain unaffected. This phase is dark-only implementation, matching `GEN.23`'s own scope exactly — no HTTP routing, controller, DTO, or `V1CatalogPilotIdentityPolicy` change of any kind.

## 9. Verification summary

- New test file: `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Session/Gen24BeginnerThreeDayExplicitZeroIneligibilityTests.cs` — 33 tests (7 explicit-zero-rejection cases + 1 frozen-authority sanity check + 21 missing/positive-observed zero-delta re-verification cases + 2 Beginner×4D zero-delta cases + 1 Intermediate×3D zero-delta + 1 public-gate check).
- Updated test file: `Gen23BeginnerThreeDayCoreTests.cs` — the two now-superseded raw-failure-shape tests (`ExplicitZero_ShortHorizons_FailsTaperGate_TypedException`, `ExplicitZero_LongerHorizons_FailsWeekOneNormalFloor_PreExistingGenericException`) replaced with one 7-horizon theory (`ExplicitZero_AllHorizons_FailsClosed_TypedReadinessIneligibilityException_GEN24`) reflecting the new, current behavior; doc comments updated to point forward to this phase rather than silently going stale. Net test-file delta: -2 old methods (7 InlineData cases total) / +1 new method (7 InlineData cases) = net 0 change within that file; +33 in the new GEN.24 file.
- Focused run (`Gen23BeginnerThreeDayCoreTests` + `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests`, zero dotnet processes confirmed via `tasklist` before launch): **66 total, 66 passed, 0 failed.**
- `dotnet build RunningApp.sln -c Debug`: 0 errors, only pre-existing warnings (unchanged count).
- Full `RunningApp.IntegrationTests` regression, run alone: see §10.
- `PlanCatalog.Tests`: unaffected by construction (no catalog document added, edited, or removed by this phase) — confirmed §10.

## 10. Full regression results

Run alone, confirmed via `tasklist` showing zero `dotnet.exe` processes immediately before launch, and `dotnet build-server shutdown` used first to clear any lingering MSBuild/VBCSCompiler build-server nodes from prior build/test invocations in this session, so no persistent-node reuse could mask contention.

```
RunningApp.IntegrationTests: 4187 total, 4184 passed, 3 failed, 0 skipped
```

The 3 failures are the identical, already-named pre-existing baseline failures this engagement has carried forward unchanged since `GEN.17`/`GEN.18`/`GEN.20`/`GEN.23` (`Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` at weeks:13 and weeks:14, `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`) — same failure shape (`InternalServerError` vs. expected `OK`), same stack traces. **Total reconciles exactly**: 4187 = `GEN.23`'s own confirmed-clean 4154 baseline + this phase's 33 new `Gen24BeginnerThreeDayExplicitZeroIneligibilityTests` tests. **Zero new regressions.** Run took 32m50s, single dotnet.exe test-host process throughout (verified no contention).

`PlanCatalog.Tests`: **1510/1510 passing**, unchanged from `GEN.23`'s own confirmed baseline (no catalog document was added, edited, or removed by this phase — confirmed empirically, not merely by construction).

`dotnet build RunningApp.sln -c Debug` and `-c Release`: both 0 errors (only the same pre-existing warning set, unchanged count).

## 11. Backlog item recorded (worded exactly as specified, not paraphrased)

Filed in `MASTER_ROADMAP.md` as a distinct backlog item, separate from the existing 2D Preparation Runway/LongHorizon backlog item:

> "Can a genuinely zero-current-running Beginner enter a 10K Core race-preparation plan directly, or does this require a separate zero-readiness on-ramp / run-walk capability?"

This phase does **not** attempt to answer this question. It formally rejects the explicit-zero request; it does not design, propose, or scope a replacement flow. Whether the eventual answer involves a different starting-volume number, lower session minima, a run/walk prescription, an on-ramp/pre-Core phase, or a different progression model entirely is explicitly undetermined and out of scope here.

## 12. Governance and closure

No public HTTP routing/gate change (§8). No already-`PUBLICLY_ACTIVE` frequency's behavior changed (§6). No already-representable Beginner×3D readiness state's behavior changed (§5 — missing/positive-observed remain exactly as `GEN.23` left them). Beginner×5D (`GEN.22`) untouched. `GEN.5C`'s and `GEN.23`'s report text unmodified (per instruction, never deleted or rewritten) — this phase's own report supersedes only the *remaining* characterization of the explicit-zero gap `GEN.23` itself disclosed as open, consistent with `GEN.21`'s and `GEN.23`'s own established supersession-of-characterization pattern.

**`BEGINNER_3D_SUPPORTED_EXPLICIT_ZERO_PRODUCT_INELIGIBLE`.** Next: whether/how to answer §11's backlog question is not scheduled as a Phase ID here. A future, separately-authorized public-activation phase for Beginner×3D Core (mirroring `GEN.4D`→`GEN.4E`) also remains not scheduled, unaffected by this phase's outcome.
