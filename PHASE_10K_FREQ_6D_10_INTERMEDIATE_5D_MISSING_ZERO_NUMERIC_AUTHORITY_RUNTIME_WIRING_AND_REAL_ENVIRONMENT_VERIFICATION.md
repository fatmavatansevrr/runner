# PHASE 10K-FREQ.6D.10 — Intermediate×5D Missing/Explicit-Zero Numeric Authority Runtime Wiring & Real Public/PostgreSQL Verification

**Type:** IMPLEMENTATION + INTEGRATED VERIFICATION
**Parent phase:** FREQ.6D.9
**Governance note:** CHAT HISTORY IS NOT PHASE AUTHORITY. This report documents production code changes, new tests, and real HTTP/PostgreSQL verification against the current repository.

---

## 1. Preflight

- Starting `git rev-parse HEAD`: `cf0066ea3d72ca78d3b8ee9a54e7c6bda01cd3ae` (FREQ.6D.9's own SHA-backfill commit).
- `git branch --show-current`: `main`.
- `git rev-list --left-right --count origin/main...HEAD` at start: `0  9`.
- `PHASE_LEDGER.md` row 89 confirmed: `FREQ.6D.9`, `DONE`, `INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`.
- Constraints preserved verbatim: no new product decision, no new numeric authority, no Core/Runway structure change, no RUN_LAYOUT_5D change, no catalog content change, no LongHorizon work.

---

## 2. Scope

Wire the `FREQ.6C`-approved, `FREQ.6D.9`-confirmed Intermediate×5D numeric authority (missing=26.0km, explicit-zero=19.5km, resolved peak=44.5km, long-run selection/hard-cap share=28%/36%) into the two real call sites that resolve Intermediate×5D starting volume — Core (`CatalogVolumeAndLongRunPlanner`) and Preparation Runway (`TenKPreparationRunwayNumericPolicyFactory`) — and prove it end-to-end through real public HTTP and real PostgreSQL. No new numeric value was introduced anywhere in this diff.

---

## 3. Implementation

1. **`V1FiveDayIntermediateMissingReadinessStartingVolumePolicy`** (new file) — mirrors `V1ThreeDayMissingReadinessStartingVolumePolicy`/`V1BeginnerFourDayMissingReadinessStartingVolumePolicy` exactly. Owns only `MissingWeeklyVolumeDefaultKm=26.0d` and `ExplicitZeroWeeklyVolumeDefaultKm=19.5d`, with provenance citing `FREQ.6C`'s closure. No allocation/progression/Taper/calendar logic duplicated.
2. **`VolumeSafetyPolicy.FiveDayIntermediate`** (new static instance) — `GoldenFixtureStartingVolumeKm=26.0`, `ResolvedPeakReference=(44.5, ProductDefaultWithEvidenceEnvelope)`, `LongRunPreferredMinimumShare=LongRunSelectionShare=0.28`, `LongRunPreferredMaximumShare=LongRunHardCapShare=0.36` — FREQ.6C approved exactly two 5D long-run figures (28%/36%), no separate preferred-range was calibrated, so the preferred range deliberately collapses to those same two approved figures rather than inventing a third number (see the type's own doc comment).
3. **`CatalogVolumeAndLongRunPlanner`** — added an exact-identity-only dispatch branch (`CanonicalDistanceFamily=="TEN_K" && Level=="INTERMEDIATE" && DaysPerWeek==5`, guarded against re-entrant dispatch the same way the existing 3D/Beginner-4D branches are) that recursively constructs itself with `VolumeSafetyPolicy.FiveDayIntermediate`; extended `ResolveStartingVolume`'s dispatch ternary to route to the new policy for that exact policy instance. No `DaysPerWeek >= 5` or `Level != Beginner` broad condition anywhere.
4. **`TenKPreparationRunwayNumericPolicyFactory`** — added a `Build(PlanCatalogCandidateSummary candidate)` overload with the identical exact-identity check, returning the FREQ.6C-backed policy for the one exact combination and falling back to the untouched, parameterless `Build()` (4D defaults) for everything else. The parameterless `Build()` itself is unchanged in behavior and is still what both LongHorizon call sites (`LongHorizonFullNumericOrchestrator.cs:140`, `LongHorizonRollingJitActivationRuntime.cs:283`) use — LongHorizon is untouched.
5. **`TenKPreparationRunwayDarkOrchestrator`** — its one call to the numeric-policy factory now passes `request.Candidate`, so the Runway pipeline resolves the same dedicated 5D authority Core does.
6. **`PreparationRunwayNumericPolicy`/`PreparationRunwayNumericMaterializer`** — added a `LongRunShareTolerance` field, owned per-policy. Default/ThreeDayIntermediate/BeginnerFourDay keep the exact prior value (`V1FourDaySessionVolumeAllocationPolicy.ToleranceKm = 0.001`, byte-identical behavior — verified below, §6). `FiveDayIntermediate` alone uses a wider tolerance (`RoundingIncrementKm / GoldenFixtureStartingVolumeKm ≈ 0.019`), because FREQ.6C's collapsed 28%/36% preferred range (item 2) sits with zero nominal gap at the selection share, and the approved half-km-rounding rule can legitimately move a real week's ratio up to roughly one rounding increment away from that exact point. This derives from already-approved constants (`RoundingIncrementKm`, `GoldenFixtureStartingVolumeKm`) — no new number.

No production code beyond the six items above was touched. `git diff --check` clean throughout.

---

## 4. Root causes found and resolved during verification (not part of the original diff scope, but required to reach real HTTP 200)

- **Stale test dates.** Every new/modified test in this phase originally used a hardcoded `2026-07-20` start date. Mid-session the real wall-clock date advanced past it (the environment's "today" is `2026-08-26`), which skewed every "N-week" test's actual weeks-to-race math and spuriously tripped an unrelated, pre-existing 56-day/8-week explicit-zero guard in `CatalogPublicPreviewMaterializer` (confirmed pre-existing and unmodified by this phase). Fixed by moving all affected dates to a safely-future `2027-07-19`. This same staleness (not a defect from this phase) explains two now-pre-existing, unrelated failures still present in the full suite — see §7.
- **`CoreEntryReadinessResolver` interaction.** A separate, pre-existing, unrelated resolver (Phase 4D.3.1) independently gates `GOAL_PACE_TEN_K` sessions on `(weekly, longest-run)` evidence — it returns `NOT_READY` only when both fields are present-and-low or both are entirely missing for a Race goal. This is orthogonal to the starting-volume axis this phase owns; test readiness construction was adjusted (supplying a non-null longest-run for "missing weekly volume," and leaving longest-run unset for "explicit-zero weekly volume") to isolate the axis under test without tripping this unrelated gate — no production code was touched for this.
- **Test-assertion bug (this phase's own new tests).** KEY_SESSION days render as `day_type` `"tempo"` (THRESHOLD_EFFORT) or `"interval"` (GOAL_PACE_TEN_K) depending on the bound workout — both are KEY_SESSION structurally. Initial assertions counted only `"tempo"`; fixed to count `"tempo" or "interval"`.

---

## 5. `INTERMEDIATE_5D_MISSING_ZERO_CORE_FIX_MATRIX`

| Horizon | Readiness | Pre-fix resolved volume | Post-fix resolved volume | Pre-fix HTTP | Post-fix HTTP |
|---|---|---|---|---|---|
| 8wk | missing | 16km (4D fallthrough) | 26.0km | 500 | 200 |
| 8wk | explicit-zero | 12km | 19.5km | 500 | 200 |
| 10wk | missing | 16km | 26.0km | 500 | 200 |
| 10wk | explicit-zero | 12km | 19.5km | 500 | 200 |
| 12wk | missing | 16km | 26.0km | 500/422 | 200 |
| 12wk | explicit-zero | 12km | 19.5km | 500/422 | 200 |
| 14wk | missing | 16km | 26.0km | 500 | 200 |
| 14wk | explicit-zero | 12km | 19.5km | 500 | 200 |

All 8 previously-failing Core-only cases now return real HTTP 200 with exact `TEN_K__5D__INTERMEDIATE` identity, `core_confirmable` lifecycle, and exact 2 KEY_SESSION + 2 EASY_SUPPORT + 1 LONG_RUN structure every week (KEY sessions render as `tempo`/`interval` depending on workout, both counted).

---

## 6. `INTERMEDIATE_5D_NUMERIC_POLICY_CALLSITE_AUDIT`

Every construction of `CatalogVolumeAndLongRunPlanner` and every call to `TenKPreparationRunwayNumericPolicyFactory.Build*`:

| Call site | Policy resolution | 5D-aware? |
|---|---|---|
| `CatalogPreviewGenerator.DefaultVolumeAndLongRunPlanner` (`new CatalogVolumeAndLongRunPlanner()`) | Default; internal `Build()` dispatch routes to `FiveDayIntermediate` for the exact 5D identity | Yes (via internal dispatch) |
| `LongHorizonFullNumericOrchestrator.cs:259` (`new CatalogVolumeAndLongRunPlanner()`) | Same internal dispatch | Yes (via internal dispatch; LongHorizon itself never reaches this with a 5D candidate today — no behavior change) |
| `TenKPreparationRunwayComponentAdapters.cs:277` (`new CatalogVolumeAndLongRunPlanner()`) | Same internal dispatch | Yes |
| `TenKPreparationRunwayDarkOrchestrator.cs:263` — `TenKPreparationRunwayNumericPolicyFactory.Build(request.Candidate)` | Candidate-aware overload | Yes — the only Runway numeric-policy call site, now 5D-aware |
| `LongHorizonFullNumericOrchestrator.cs:140` — `TenKPreparationRunwayNumericPolicyFactory.Build()` | Parameterless, unchanged (4D defaults) | No — LongHorizon untouched, exactly as required |
| `LongHorizonRollingJitActivationRuntime.cs:283` — `TenKPreparationRunwayNumericPolicyFactory.Build()` | Parameterless, unchanged | No — LongHorizon untouched |

Dispatch for the starting-volume decision itself is centralized inside `CatalogVolumeAndLongRunPlanner.Build`/`ResolveStartingVolume` — there is exactly one place a 5D candidate's missing/zero readiness resolves, and no second call path bypasses it. A permanent regression test (`Dispatch_MissingReadiness_RealFiveDayCandidate_Resolves26Km_NotSilentFallback` / `..._ExplicitZeroReadiness_..._Resolves19Point5Km_...`) proves this behaviorally (dispatch outcome, not implementation-detail/class-name assertions).

---

## 7. Full regression

- New/modified test files: `V1FiveDayIntermediateMissingReadinessStartingVolumePolicyTests.cs` (19/19), `IntermediateFiveDayMissingZeroNumericAuthorityEndToEndTests.cs` (26/26), `PreparationRunwayFiveDayPublicActivationEndToEndTests.cs` (2 tests updated from asserting the old 422 failure to asserting real 200 success), `TenKPreparationRunwayDarkOrchestrator5DTests.cs` (1 assertion updated 16km→26km, now matching wired authority).
- Targeted regression (LongHorizon + PreparationRunway + 5D-related): **1719/1719**.
- Full `RunningApp.IntegrationTests`: **3787/3791** on the first full run (4 failures diagnosed below); after the two genuine fixes in this phase (§4's tolerance-field fix, §5's test-assertion updates) all four either passed or were confirmed pre-existing/unrelated:
  - `TenKPreparationRunwayDarkOrchestrator5DTests.StartingWeeklyVolume_Missing_ResolvesViaExistingCanonicalAuthority` — fixed (test asserted the pre-fix 16km value; now asserts 26km).
  - `LongHorizonCoreWeekOneEvidenceAuthorityDiagnosticTests.Phase4K8A_RealDirectionMatrix_...` — a genuine regression from an earlier, too-broad tolerance widening; fixed by making `LongRunShareTolerance` policy-owned instead of shared, restoring byte-identical behavior for Default/3D/Beginner4D (re-verified: 18/18 pass, including the specific case that had regressed).
  - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` and `Sw09ExplicitZeroReadinessEndToEndTests...` — both confirmed **pre-existing and unrelated**: both use the same stale hardcoded `2026-07-20` date (now in the past); reproduced identically via `git stash` against this phase's completely unmodified baseline. Neither file was touched by this phase.
- PlanCatalog full suite: **1510/1510** (one transient failure during a concurrent full-suite run diagnosed as a file-lock race with the parallel `dotnet test` process, not a real failure; one governance-test false-positive from this phase's own code comment literally containing the token `Phase4K8A` — reworded, now clean).
- Debug build: clean, 0 errors. Release build: clean, 0 errors.
- `git diff --check`: clean.

---

## 8. Runway starting-volume determination (§32 of the phase prompt)

Determined from real implementation output, not assumption: Preparation Runway's starting volume for Intermediate×5D **does** follow the same 26.0/19.5 FREQ.6C authority as Core — confirmed via `TenKPreparationRunwayDarkOrchestrator5DTests.StartingWeeklyVolume_Missing_ResolvesViaExistingCanonicalAuthority`, which now resolves 26km for missing readiness through the real orchestrator, and via the real HTTP Runway tests in `IntermediateFiveDayMissingZeroNumericAuthorityEndToEndTests`. This is not a separate, distinct entry — it is the same `V1FiveDayIntermediateMissingReadinessStartingVolumePolicy` values, reached via `TenKPreparationRunwayNumericPolicyFactory.Build(candidate)`'s candidate-aware dispatch (§3 item 4).

Runway→Core handoff verified unchanged: last Runway week is 1 KEY + 3 EASY + 1 LONG, first Core week is 2 KEY + 2 EASY + 1 LONG, no hidden rescue logic — asserted directly against real Postgres-confirmed rows.

---

## 9. Error semantics

Every approved-eligible case now succeeds with no exception swallowing. No case remained infeasible despite the approved FREQ.6C values once the real root causes (§4) were found and fixed — the STOP-discipline condition ("if any case remains truly infeasible... investigate, not another fallback") was not triggered; every apparent remaining infeasibility traced to an unrelated, narrowly-scoped, already-fixed defect.

---

## 10. Final classification

**`INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED`**

This is implementation of previously-approved `FREQ.6C` authority, not a new product policy. Missing/explicit-zero readiness now resolves the exact approved 26.0km/19.5km values through both Core and Preparation Runway, proven via real public HTTP and real PostgreSQL, with zero-delta regression for Intermediate×3D/4D/Beginner×4D and Intermediate×5D positive-observed, and LongHorizon strictly untouched.

**Next roadmap capability**: Intermediate×5D LongHorizon ARCHITECTURE/DESIGN (not implementation) — per `MASTER_ROADMAP.md`'s existing pointer and `FREQ.6D.5`'s own recommended sequence (item C).

---

## 11. Push gate

9 commits ahead of `origin/main` at phase start; this phase adds 4 commits (implementation, tests, docs/ledger/roadmap, SHA backfill) → 13 ahead. No push performed — governance threshold recalculated at phase end per the durability-gate rule; push deferred to the next phase boundary unless a gate is explicitly required sooner.
