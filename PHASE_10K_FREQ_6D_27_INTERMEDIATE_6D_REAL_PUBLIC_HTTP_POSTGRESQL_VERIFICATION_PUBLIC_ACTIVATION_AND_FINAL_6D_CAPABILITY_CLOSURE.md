# PHASE 10K-FREQ.6D.27 — Intermediate×6D Real Public HTTP/PostgreSQL Verification, Public Activation & Final 6D Capability Closure

**Parent phase**: `FREQ.6D.26` (`INTERMEDIATE_6D_CORE_RUNWAY_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`)
**Phase type**: PUBLIC ACTIVATION + REAL ENVIRONMENT VERIFICATION + FINAL CAPABILITY CLOSURE
**Execution status**: DONE
**Final classification**: `INTERMEDIATE_6D_CORE_RUNWAY_LONGHORIZON_IMPLEMENTED_AND_PUBLICLY_ACTIVATED` / `INTERMEDIATE_6D_FULL_HORIZON_CAPABILITY_COMPLETE`

---

## 1. Summary

Opened the real public routing gate for Intermediate×6D across all three horizon bands — Core (8–14 weeks), Preparation Runway (15–20 weeks), and LongHorizon (21–52 weeks) — implementing only the already-approved `FREQ.6D.23`/`FREQ.6D.25`/`FREQ.6D.26` authority. No new product, numeric, schema, or structural decision was made. Verified through real public HTTP requests against a real PostgreSQL database, then reconciled a full-suite regression discrepancy (3 TRX-confirmed failures vs. 2 initially recognized) to an authoritative, evidence-based conclusion before any governance closure, per the user's explicit mid-phase reconciliation protocol.

## 2. Minimal public gate changes (4 sites)

All four changes widen an existing explicit allow-list from `(Intermediate, 5)` to also include `(Intermediate, 6)` — no new dispatch shape, no new candidate-resolution mechanism:

1. `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency` — added `(RunningBackground.Intermediate, 6)`.
2. `V1CatalogPilotIdentityPolicy.ResolveCandidate` — added `(Intermediate, 6) => (SixDayCandidateKey, SixDayCandidateVersion)` (the `SixDayCandidateKey`/`SixDayCandidateVersion` constants themselves were already added, unused, in `FREQ.6D.26`).
3. `V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayLevelFrequency` — added `(Intermediate, 6)`.
4. `LongHorizonPublicPlanService.ValidatePilot` — widened its `DaysPerWeek is not (4 or 5)` guard to `is not (4 or 5 or 6)`.

The Preparation-Runway-specific `IsSupportedPreparationRunwayCandidate` consistency check and `SixDayCandidateKey`/`Version` constants needed no change — `FREQ.6D.26` had already widened/added them for internal dark-orchestration use.

## 3. New public-activation test coverage

`Freq6D27IntermediateSixDayPublicActivationTests.cs` (new, 20 tests) proves, through real HTTP + real PostgreSQL:

- Core/Runway horizons (8, 12, 14, 15, 17, 20 weeks) route correctly via `/generate-preview/race` with exact 6-day identity.
- LongHorizon horizons (21, 22, 23, 24, 32, 52 weeks) route correctly via `/generate-preview/race/long-horizon` with correct GE shape.
- The full 8–52 week matrix routes correctly: exactly 7 Core + 6 Runway + 32 LongHorizon = 45/45.
- `ProductAverage` and `UserDefined` `TargetFinishTimeSource` both confirm and correctly reload from a fresh PostgreSQL context after a LongHorizon plan.
- Missing readiness returns a typed product-ineligible rejection, never a generic "unsupported" error.
- A full public lifecycle (GE → Runway → Core) reaches an organic dual-KEY Core week through real PostgreSQL, including a real repair regression via `ScheduleRepairRuntimeOrchestrator.RunAsync`.
- Unsupported neighbors (Beginner×6D, Advanced×6D, Intermediate×7D) remain closed with zero identity leakage.

## 4. Obsolete pre-activation assertions corrected (5 instances)

Opening `(Intermediate, 6)` as a real, public pilot identity made five pre-existing, pre-dating "unsupported neighbor" test rows factually obsolete — each asserted "(Intermediate, 6) is unsupported" as one row of a broader parameterized theory. Each was corrected by removing/advancing only that specific row (never weakening the theory's remaining real intent), with an explanatory comment left in place:

1. `Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests.UnsupportedNeighbors_RemainClosed_NoFallbackToFourOrFiveDayIdentity` — removed `("intermediate", 6)`.
2. `Freq6D26IntermediateSixDayDarkVerificationTests` — removed two now-obsolete isolation tests (`PublicIdentityPolicy_DoesNotRecognizeIntermediateSixDay`, `PreparationRunwayIdentityPolicy_DoesNotRecognizeSixDay`), replacing the latter with a 7D-specific equivalent.
3. `PreparationRunwayFiveDayPublicActivationEndToEndTests.UnsupportedNeighbors_FifteenToTwentyWeeks_StillReturns422` — removed `("ten_k", "intermediate", 6)`.
4. `Gen5DIntermediatePublicActivationTests.UnsupportedFrequencyNeighbors_RemainUnactivated` — removed `("intermediate", 6)`.
5. `Phase4F8_2LivePilotRoutingTests.Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog` — found during full-suite reconciliation (see §5). Its `DaysPerWeek` mutation case used `6` to construct a "non-pilot" request; since `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` now recognizes `(Intermediate, 6)`, this stopped producing a non-pilot request. Advanced the probe value to `7` (genuine `PRODUCT_NON_SUPPORT` per `FREQ.6D.23`), matching the exact precedent already established by fix #1 above (which made the identical adjustment for its own `DaysPerWeek` case, from 5→6, when `FREQ.6D.4D.5G` widened 5D).

## 5. Full-regression discrepancy reconciliation (the user's explicit mid-phase directive)

A full regression run reported **3** failures; the console tail only surfaced **2**, matching this engagement's long-documented durable baseline. Per the user's explicit instruction, governance closure was withheld until the third failure was identified and correctly attributed through direct reproduction — not assumption.

**Authoritative TRX evidence** (`freq6d27_final.trx`, 3948 total tests, precise `outcome="Failed"` extraction):
1. `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution` — `Assert.Equal() Failure: Expected OK, Actual InternalServerError`, `Sw09ExplicitZeroReadinessEndToEndTests.cs:79`. Exact name/exception/message/stack-frame match to this engagement's long-documented durable pre-existing baseline failure.
2. `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — `Assert.Equal() Failure: Expected OK, Actual InternalServerError`, `Gen4EBeginnerFourDayPublicActivationTests.cs:69`. Exact match to the second long-documented durable pre-existing baseline failure.
3. `Phase4F8_2LivePilotRoutingTests.Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog(fieldToChange: "DaysPerWeek")` — previously unattributed.

**Classification of failure #3** (Category A — a direct, disclosed consequence of this phase's own gate-widening — but of the same well-established "obsolete pre-activation assertion" class already found and fixed 4 times this phase, confirmed by direct code reading, not assumption): read the test file directly (`Phase4F8_2LivePilotRoutingTests.cs:65-88`) and traced `V1LiveCatalogPilotRoutingPolicy.Evaluate` (line 118 of `LivePlanPreviewRouting.cs`) to confirm it calls `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` — the exact identity gate this phase widened. The test's `DaysPerWeek` mutation case set `request.DaysPerWeek = 6` specifically to construct a request the policy would classify `NotPilotRequest`; since `(Intermediate, 6)` is now a real pilot identity, the assertion is stale, not a regression in generation behavior. Fixed per §4 item 5 above. Re-ran the corrected test class in isolation: 26/26 passed.

**A separate stale-run artifact was also investigated and ruled out**: an earlier background test run had additionally reported a failure in `PreparationRunwayFiveDayPublicActivationEndToEndTests.UnsupportedNeighbors_FifteenToTwentyWeeks_StillReturns422` with a mismatched error code (`RUNTIME_CONDITION_UNSUPPORTED` vs. expected `PLAN_HORIZON_COMPOSITION_REQUIRED`). Re-running that test class in isolation against the current, already-fixed source (item #3 in §4) passed 19/19 — confirming that earlier failure was a stale/pre-rebuild snapshot from mid-edit, not a live defect, consistent with this engagement's known build-cache gotcha (the integration-test project's own `bin/` copy of `RunningApp.Application.dll` requiring an explicit rebuild after Application-side edits).

## 6. Final authoritative regression (post-reconciliation)

Full suite re-run with TRX logging after the fix (`freq6d27_reconciled.trx`):

- **3948 total, 3946 passed, 2 failed, 0 skipped.**
- The only 2 failures are the exact same durable pre-existing baseline failures (`Sw09...`, `Gen4E...weeks:13`) — identical name, exception, message, and stack frame to every prior regression run across this engagement.
- **Zero new attributable regressions.**

Additional verification: `PlanCatalog.Tests` 1510/1510 pass; Debug build 0 errors; Release build 0 errors; `git diff --check` clean (only benign CRLF-normalization warnings, no real whitespace conflicts).

## 7. Intermediate 10K frequency-axis status

| Frequency | Status |
|---|---|
| 3D | `PUBLICLY_ACTIVE` (GEN.3B) |
| 4D | `PUBLICLY_ACTIVE` (pre-existing/Adaptation V1 baseline) |
| 5D | `PUBLICLY_ACTIVE`, full horizon (`FREQ.6D.22`) |
| 6D | `PUBLICLY_ACTIVE`, full horizon (this phase) |
| 7D | `PRODUCT_NON_SUPPORT` (`FREQ.6D.23`, final — real evidence of a calendar-spacing conflict at zero-rest-day cadence plus injury-incidence evidence, not a placeholder) |

Every cell of the Intermediate 10K frequency axis now carries a final, evidenced classification — 4 public, 1 deliberately and permanently non-supported. **`INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE` is classified as achieved.**

## 8. Explicit disclosures / non-overclaims

- This phase's own prompt enumerated a very large (104-item) final-report/success-boundary manifest; this report documents the real work performed and the real evidence gathered, and does not mechanically restate every enumerated item where no distinct new evidence exists beyond what is already narrated above.
- No new product, numeric, schema, or catalog decision was made anywhere in this phase — every change implements already-approved `FREQ.6D.23`/`FREQ.6D.25`/`FREQ.6D.26` authority.
- Beginner×6D, Advanced×6D, and Intermediate×7D remain exactly as they were (Beginner/Advanced×6D unopened; Intermediate×7D `PRODUCT_NON_SUPPORT`) — verified closed by this phase's own new isolation/unsupported-neighbor tests.
- Per this phase's own §70, the next capability is not assumed here. `MASTER_ROADMAP.md`'s own Wave A remaining-work list names two open candidates — completing the Advanced level across proven frequencies, and completing Beginner's remaining frequencies — neither of which this phase selects or schedules. **`NEXT_PHASE_NOT_YET_SCHEDULED`.**

## 9. Push-gate

Full regression, PlanCatalog, Debug/Release builds, and `git diff --check` all pass. Governance (this report, `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`) committed together with the implementation. Normal push (no force, no force-with-lease) performed after verifying local/remote HEAD divergence.
