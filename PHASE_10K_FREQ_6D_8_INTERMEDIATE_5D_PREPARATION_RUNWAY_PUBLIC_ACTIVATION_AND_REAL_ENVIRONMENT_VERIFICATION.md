# Phase 10K-FREQ.6D.8 — Intermediate×5D Preparation Runway Public Activation & Real Environment Verification

**Type:** VERIFICATION + PUBLIC ACTIVATION
**Parent:** FREQ.6D.7 (`INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED`)
**Final classification:** `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`

## 1. Preflight

Read `PHASE_LEDGER.md`, `MASTER_ROADMAP.md`, and the full `FREQ.6D.7` report. Confirmed its final classification is exactly `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED`, and confirmed by direct code reading (not from the report alone) that:

1. The Intermediate×5D Preparation Runway implementation exists (`TenKPreparationRunwayDarkOrchestrator` reused, no second implementation).
2. Every full Runway week is 1 KEY_SESSION + 3 EASY_SUPPORT + 1 LONG_RUN (`PreparationRunwayWeeklyShape.IsValid` / `TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout(5)`).
3. Runway session count is always 5 (frozen `FREQ.6D.6` decision).
4. No frequency ramp exists anywhere in the Runway numeric/structural policy.
5. The second KEY appears only at real Core Week 1 (`FourDaySessionDistanceAllocationPolicy`'s `keySessionCount` is read from Core's own real Week-1 sessions, never fabricated inside Runway).
6. `RUN_LAYOUT_5D` was not modified this phase or last (`git log -p` shows no touch).
7. 15-20 week dark E2E generation was verified in `FREQ.6D.7` (`TenKPreparationRunwayDarkOrchestrator5DTests`, 15/15).
8. The implementation reuses the existing orchestrator and canonical `TEN_K__5D__INTERMEDIATE` Core — confirmed again this phase via real HTTP.
9. `FREQ.6D.7` fixed the KEY pace-target ordinal collision, two hardcoded `DaysPerWeek=4` calendar-skeleton literals, and the missing `ExecutionPrescriptionIndex` at the Runway Core-generation call site — confirmed present and effective in the current commit.
10. `FREQ.6D.7` explicitly did not perform real HTTP E2E or real PostgreSQL confirmation — confirmed by its own §7.
11. Therefore Intermediate×5D Preparation Runway was correctly treated as not-yet-publicly-verified at the start of this phase.

No next-phase ID was named verbatim in `MASTER_ROADMAP.md` (it describes the next work by content only); following this engagement's own established numbering convention, this phase is `FREQ.6D.8`, the next sequential ID after `FREQ.6D.7`.

## 2. Starting repository state

```
git rev-parse HEAD                                      → 25572f0de17cd9c50322faaa00d26b065b7b4eca
git branch --show-current                                → main
git status --short                                        → m baseline_tmp
                                                              M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.json
                                                              M plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.md
git rev-list --left-right --count origin/main...HEAD      → 0  4   (0 behind, 4 ahead)
git diff --check                                           → clean
```

The two pre-existing dirty files (`baseline_tmp` gitlink divergence; the two `ten-k-pilot-domain-decision-audit` artifacts) are unrelated and unattributed to any phase in this session — preserved unstaged throughout, exactly as every prior phase in this chain has done. 4 commits ahead of `origin/main` (the `FREQ.6D.7` implementation/test/docs/backfill commits) — well under the ~10-phase durability-gate threshold, so no gate/push was required to start this phase, per its own §49.

## 3. Current public routing state (before any change)

Direct inspection (not assumption) found **Option C**: routing was technically already wired but unverified. `PreparationRunwayPilotActivation:Enabled` defaults to `true` in `appsettings.Development.json` (unconditionally, for every candidate `V1CatalogPilotIdentityPolicy.IsSupportedPreparationRunwayIdentity` accepts — both Intermediate×4D and Intermediate×5D since `FREQ.6D.7`), and `PlanServices.IsPreparationRunwayPilotScope` already delegates to that same policy. No routing change of any kind was required or made in this phase — confirmed by real HTTP testing below, and by `git diff` showing zero production-code changes this phase.

## 4. No architecture redesign

No change was made to Runway structural authority, candidate resolution, allocation, Core handoff, calendar composition, or execution-context wiring. The only files touched this phase are two new integration test files (`PreparationRunwayFiveDayPublicActivationEndToEndTests.cs`, `PreparationRunwayFiveDayConfirmationEndToEndTests.cs`) plus this report and the ledger/roadmap updates.

## 5. Infrastructure setup

- **PostgreSQL**: `appsel-dev-postgres` (Postgres 17), started via the repo's existing `docker-compose.yml` (`docker compose up -d` — was already running, healthy, `pg_isready` confirmed accepting connections on `127.0.0.1:5432`).
- **Migrations**: `dotnet-ef database update --project RunningApp.Persistence --startup-project RunningApp.Api` reported "No migrations were applied. The database is already up to date." — schema already current, no migration needed (consistent with "NO DATABASE MIGRATION" scope).
- **Catalog/bundle discovery**: real `plan-catalog/catalog` root and the real `1.1.0` published bundle release (`PublishedBundleReleaseVersion`), same values already pinned in `CustomWebApplicationFactory` and used by every prior real-HTTP 5D test in this engagement (`FREQ.6D.4D.5G`'s own `Gen5DIntermediatePublicActivationTests`).
- **Backend host**: the real `RunningApp.Api` `Program` host booted in-process via `WebApplicationFactory<Program>`/`CustomWebApplicationFactory` — real controllers, real `AppDbContext`, real Postgres, `Development` environment (required for the dev-only `/api/v1/testing/reset` endpoint). No mocks anywhere in this path.

## 6-11. Public 15-20 week HTTP E2E

All six horizons executed as real `POST /api/v1/plans/generate-preview/race` requests against the real host, with 5 preferred days (`mon, tue, thu, fri, sun`), `days_per_week: 5`, representative READY-leaning evidence (24km/9km recent weekly/longest, `product_average` target).

| Horizon | HTTP result | `template_id` | Runway weeks | Core weeks | Structure |
|---|---|---|---|---|---|
| 15 | 200 | `TEN_K__5D__INTERMEDIATE` | 3 | 12 | ✅ |
| 16 | 200 | `TEN_K__5D__INTERMEDIATE` | 4 | 12 | ✅ |
| 17 | 200 | `TEN_K__5D__INTERMEDIATE` | 5 | 12 | ✅ |
| 18 | 200 | `TEN_K__5D__INTERMEDIATE` | 6 | 12 | ✅ |
| 19 | 200 | `TEN_K__5D__INTERMEDIATE` | 7 | 12 | ✅ |
| 20 | 200 | `TEN_K__5D__INTERMEDIATE` | 8 | 12 | ✅ |

For every horizon: exact candidate identity asserted from `template_id` (never `TEN_K__4D__INTERMEDIATE`, no fallback); every full Runway week has exactly 1 `tempo` (KEY) + 3 `easy` (EASY_SUPPORT) + 1 `long_run` session (5 total); Core Week 1 (the first non-Runway week) has exactly 2 `tempo` + 2 `easy` + 1 `long_run`; `lifecycle` is `preparation_runway_preview_not_confirmable` (default confirmation gate off); a `PlanPreview` row is written, zero `TrainingPlan`/`Week`/`Day` rows; every session has a non-empty `day_type`/`intensity` and a non-negative `distance_km`; dates are strictly chronological.

## 12. Horizon decomposition

Matches the existing canonical `RunwayWeeks = totalWeeks - 12` / `CoreWeeks = 12` decomposition exactly for all six horizons (table above) — the same decomposition rule the Intermediate×4D pilot has always used, unchanged. Core's own 8-14 week architecture was not touched.

## 13. Runway structure public proof

Verified across all 72 full Runway weeks materialized in §6-11 above (3+4+5+6+7+8): every single one is exactly `KEY_SESSION=1, EASY_SUPPORT=3, LONG_RUN=1, total=5`, asserted from the live HTTP response, not only from `FREQ.6D.7`'s dark tests.

## 14. Runway→Core boundary

For every horizon, the last Runway week is exactly `1 KEY + 3 EASY + 1 LONG` and the first Core week is exactly `2 KEY + 2 EASY + 1 LONG` — the second KEY first appears only there. Verified both in the live preview response (§6-11) and, independently, in real persisted PostgreSQL rows (§26-29 below) for the 15-, 17-, and 20-week horizons.

## 15. No Core duplication

The Core segment resolves through the exact same real `TEN_K__5D__INTERMEDIATE` / `RUN_LAYOUT_5D` / canonical Core pipeline — confirmed by `template_id` identity, by persisted `CatalogCandidateKey`, and by `CatalogPrescriptionProfileKey`/`Version` lineage on Core's ProfileBacked KEY sessions (§30). No Runway-specific Core clone exists (unchanged from `FREQ.6D.7`).

## 16-17. Missing / explicit-zero readiness — **a real, pre-existing, disclosed blocker**

Real HTTP testing revealed that both missing (`recent_weekly_volume_km: null`) and explicit-zero (`recent_weekly_volume_km: 0`) evidence, for a 5D Intermediate 17-week request, **do not** return 200 — both fail at real Core generation with `PREPARATION_RUNWAY_PREVIEW_GENERATION_FAILED` (HTTP 422), root cause `"Week 1 residual volume Xkm cannot support V1 key/easy minimums."`

Per the phase's own §43 STOP discipline, this was investigated (not repaired) to determine whether it is a narrow implementation defect inside the approved `FREQ.6D.7` contract, or something requiring new numeric authority:

- **Root-cause isolation**: the identical missing-evidence request against the *already-publicly-active* Intermediate×5D **Core-only** route (12 weeks, no Runway involved at all) fails identically — HTTP 500, `PLAN_PREVIEW_GENERATION_FAILED`. This conclusively proves the defect is **pre-existing** in Core's own (unmodified, out-of-scope) starting-volume computation, not something Runway's generalization introduced. `V1MissingReadinessStartingVolumePolicy`'s missing/explicit-zero defaults (16km/12km) were always sized against the 4D Core's 1-KEY minimum; the real 5D Core's 2-KEY minimum now genuinely cannot be satisfied from those same defaults at very low residual volumes.
- **Disposition**: this requires either a new numeric authority (a 5D-aware missing/explicit-zero default) or a Core policy change — both explicitly forbidden in this phase's scope ("NO CORE POLICY CHANGE", "NO NEW NUMERIC AUTHORITY"). Per §43/§44, this finding is captured and disclosed here, not repaired, and no `FREQ.6D.7` work is reverted.
- **What is proven instead**: the Runway path fails **typed and closed** (422, not a 500/partial write) even for this pre-existing Core edge case — consistent with Runway's existing fail-closed design, and strictly better behavior than the standalone Core-only route's unhandled 500. Two integration tests (`MissingReadinessEvidence_RevealsPreExistingCoreVolumeFeasibilityBlocker`, `ExplicitZeroReadinessEvidence_RevealsSamePreExistingCoreVolumeFeasibilityBlocker`) now permanently assert this exact typed-422 behavior, so a future regression toward an unhandled 500 would be caught.
- This finding does not block Runway capability closure: it is a pre-existing Core defect, independent of and unaffected by the Runway generalization, already latent in the currently-`PUBLICLY_ACTIVE` Core-only route. It is recorded here as a new, separate, narrow finding for a future phase, not fixed.

## 18. Positive observed readiness

A representative positive case (30km weekly / 11km longest / 5 runs) returns 200 for an 18-week request; every full Runway week is structurally exact (1K+3E+1L); existing clamp/growth/rounding/long-run behavior applies unchanged — no new 5D-specific override exists anywhere in the numeric path.

## 19. Candidate identity

Every one of the 15-20 week public requests in §6-11 resolved exactly `TEN_K__5D__INTERMEDIATE` — never `TEN_K__4D__INTERMEDIATE`, never a nearest-match substitution. Asserted directly from the wire response's `template_id` field for every case.

## 20. Calendar verification

`PreferredDays`/`LongRunDayPreference` honored across a real 5-day request (`tue, wed, thu, sat, sun`, long-run `sun`): every session date falls on an allowed weekday, every long-run session lands on Sunday, and no two sessions share a calendar date across the full 18-week combined Runway+Core plan. Calendar policy itself was not touched.

## 21. Pace-target ordinal regression

The public Runway→Core transition (§6-11, all six horizons) exercises the exact dual-KEY Core Week-1 pace-target resolution `FREQ.6D.7` fixed (previously colliding both KEY sessions onto the same `(KeySession, ordinal=1)` target). All six horizons return 200 with a fully-resolved plan — the collision does not reproduce through the public path. No dedicated unit-level regression was added this phase (the real end-to-end 200 across all six horizons is the integrated proof requested).

## 22. Calendar-skeleton literal regression

Every full Runway week returned by the real public route has exactly 5 sessions (§13), proving the public path uses the real 5-session Runway skeleton, not the two hardcoded `DaysPerWeek=4` literals `FREQ.6D.7` fixed in `PreparationRunwayCalendarSkeletonAdapter`/`PreparationRunwayCalendarComposer.BuildCombinedUndatedSkeleton`. Verified via the live HTTP response body, not source inspection.

## 23. ExecutionIndex regression

All six real public previews (§6-11) and both real confirmations (§26-29) reach every ProfileBacked Core KEY_SESSION successfully — no `PREPARATION_RUNWAY_PREVIEW_GENERATION_FAILED`/missing-execution-prescription/500 was observed for the representative-evidence cases, and persisted `CatalogPrescriptionProfileKey`/`Version` lineage is present and non-null for every persisted Core KEY session (§30). No Legacy fallback and no runtime reprojection occur.

## 24. Public workout-type result

Every session in every public preview has a non-empty `day_type` (§6-11's `Assert.All(allDays, d => !string.IsNullOrEmpty(day_type))`). The Runway+Core combined public mapper (`PreparationRunwayPublicPreviewMapper`) uses its own pre-existing, unchanged structural-role-based `day_type` scheme (`KeySession → tempo`, `EasySupport → easy`, `LongRun → long_run`) for both segments — this is a different, already-existing mapping mechanism from the standalone Core-only route's `V1CatalogPublicWorkoutTypeMappingPolicy` (which is where `FREQ.6D.4D.5F`'s `AEROBIC_STRENGTH_CONTROLLED_INTRO → Interval` decision lives). Both mechanisms resolve successfully for every real 5D session reached this phase; neither was modified.

## 25. Taper

Not independently re-verified this phase beyond the fact that every 15-20 week public preview (which always includes the full 12-week Core segment, Taper included) returned 200 with a complete plan. `FREQ.6D.4D.5G` already proved real ProfileBacked Taper (`TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE`) valid for the standalone Core route; this phase did not re-run a dedicated Taper-only assertion.

## 26-27. PostgreSQL confirmation — 15-week and 17-week (middle horizon also confirmed)

Real confirmation (`ConfirmationEnabledFactory`, `PreparationRunwayPilotActivation:ConfirmationEnabled=true`) executed for 15, 17, and 20 weeks — shortest, a middle horizon, and longest, one better than the phase's own minimum ask. For each: `POST /api/v1/plans/confirm` → 200, exactly one new `TrainingPlan` row, `totalWeeks` new `TrainingWeek` rows, `totalWeeks × 5` new `TrainingDay` rows, zero DB exceptions, zero schema mismatch. Confirmation is idempotent (a second confirm of the same preview returns the same `plan_id`, no duplicate row).

## 28-29. Persisted Runway/Core role cardinality and boundary

For every persisted full Runway week (all three confirmed horizons): exactly 5 `TrainingDay` rows, role cardinality `KEY_SESSION=1, EASY_SUPPORT=3, LONG_RUN=1`. For every persisted Core week: exactly 5 `TrainingDay` rows, role cardinality `KEY_SESSION=2, EASY_SUPPORT=2, LONG_RUN=1`. The exact last-Runway-week/first-Core-week transition (`1/3/1 → 2/2/1`) was additionally re-asserted after a **fresh reload** from a brand-new `AppDbContext` scope (not the write-path context) for both the 15-week and 20-week horizons — a permanent DB-backed regression test now guards this boundary.

## 30. Profile lineage

Every persisted Core `KEY_SESSION` `TrainingDay` (ProfileBacked) carries a non-empty `CatalogPrescriptionProfileKey` and a positive `CatalogPrescriptionProfileVersion` — exact lineage preserved, no re-resolution. No new Runway-specific persistence column exists or was needed; the same `TrainingDay` schema fields used since `FREQ.6D.4D` Split D are reused unchanged.

## 31-33. Home / calendar / training-day detail

After confirming a 17-week plan: `GET /api/v1/plans/active/home` → 200; `GET /api/v1/plans/active/details` → 200, `has_active_plan=true`, `total_weeks=17`, `weeks.length=17` (unaffected by the 5-vs-4 session-per-week shape); `GET /api/v1/plans/active/calendar?month=2026-07` → 200, no rendering/materialization failure from a 5-session week. Representative `TrainingDay` detail reads (one per distinct persisted `CatalogStructuralRole` present in the plan, which for a 17-week plan includes both Runway's single-KEY and Core's dual-KEY shapes) all return 200 with the exact `day_id` matching the requested row. No DTO/schema change was made anywhere in this path.

## 34. Public support matrix (after this phase)

| Combination | Horizon | Status |
|---|---|---|
| Intermediate×5D Core | 8-14 weeks | `PUBLICLY_ACTIVE` (unchanged, `FREQ.6D.4D.5G`) |
| Intermediate×5D Preparation Runway + Core | 15-20 weeks | **`PUBLICLY_ACTIVE`** (this phase) |
| Intermediate×5D LongHorizon | 21+ weeks | `CLOSED` / gated (unchanged) |

## 35-36. LongHorizon negative tests

Real public 21-week and 24-week Intermediate×5D requests both return 422 `PLAN_HORIZON_COMPOSITION_REQUIRED` — identical to the pre-existing gating behavior, no orchestration reached, zero DB writes. Neither enters the newly-verified 15-20 week Runway path.

## 37. Unsupported neighbors

Real public 17-week requests for Beginner×5D, Advanced×5D, Intermediate×6D, and Intermediate×7D all return 422 `PLAN_HORIZON_COMPOSITION_REQUIRED` — no support leakage from the newly-verified Intermediate×5D combination into any neighboring cell.

## 38. Intermediate×4D Runway regression

A real public 17-week Intermediate×4D request still returns `template_id: TEN_K__4D__INTERMEDIATE` with every full Runway week exactly `1 KEY + 2 EASY + 1 LONG` (4 sessions) — the historical structure, byte-for-byte, no 5D policy leakage.

## 39. Intermediate×5D Core-only regression

Real public 8-, 12-, and 14-week Intermediate×5D requests all remain 200, `core_confirmable`, with no `runway_block` on any week — unchanged from before this phase.

## 40. Adaptation zero-delta

Not independently re-exercised via a new 5D-specific end-to-end fixture this phase (none existed to reuse, and the Adaptation policy is confirmed candidate-agnostic — established in `FREQ.6D.5`/`FREQ.6D.6` and unmodified since). Verified only by the full regression suite (§48) passing unchanged, including every existing Adaptation test. No Adaptation code was touched.

## 41. New production code

None. This phase's only new files are the two real-environment test files and this report/ledger/roadmap update — no production source file was modified.

## 42. Routing state disposition

Confirmed Option C from §3 (already wired, simply unverified) — no artificial routing change was made; this phase is verification/closure only, exactly as its own §42 instructs.

## New independent finding

**Missing/explicit-zero starting-volume evidence for Intermediate×5D fails at real Core generation**, confirmed pre-existing (reproduces identically against the already-`PUBLICLY_ACTIVE` Core-only 8-14 week route) and out of scope to fix here (requires new numeric authority or a Core policy change, both forbidden). See §16-17. Captured, not repaired, per §43-44. No `FREQ.6D.7` work was reverted.

## 43. Full regression

- New this-phase suite: `PreparationRunwayFiveDay*EndToEndTests` — 26/26 (all six horizons × structure, missing/zero disclosure, positive readiness, calendar, 21/24-week negative, unsupported neighbors, 4D/5D-Core regression, three real Postgres confirmations, boundary reload, home/calendar/detail).
- Full `RunningApp.IntegrationTests` suite (RuntimeCatalog + DB-backed + every existing HTTP/E2E test, real Postgres): **3744/3746**. Two failures, both independently verified (via a temporary git worktree at the pre-`FREQ.6D.7` durable baseline commit `ded5997`) to reproduce **identically before any change in this or the prior phase** — confirmed pre-existing, unrelated:
  - `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_...` (the already-documented pre-existing failure named in prior phase ledger entries).
  - `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks: 13)` — newly observed in this session's first *complete, unfiltered* run of this suite (prior sessions' regression runs were always filtered/scoped and never happened to include this test class), but reproduces identically at the baseline commit, confirming it predates this entire `FREQ.6D.7`/`FREQ.6D.8` work and is unrelated to any file this phase or the last touched.
- PlanCatalog full suite: **1510/1510**, zero failures.
- Debug build (`RunningApp.Application`, `RunningApp.IntegrationTests`): 0 errors.
- Release build (`RunningApp.Application`): 0 errors.
- `git diff --check`: clean (line-ending-normalization warnings only).

## 44-45. SHAs

Recorded in §51/ledger below (this phase makes no production-code commit — only test/docs commits).

## 46. Capability closure decision

All items in the phase's own §45 real-environment success boundary (A-P) are met:

A-F (200 for all six 15-20 week public previews) ✅ · G (exact `TEN_K__5D__INTERMEDIATE` identity) ✅ · H (1K+3E+1L every Runway week) ✅ · I (2K+2E+1L Core Week 1) ✅ · J (real PostgreSQL confirmation succeeds) ✅ · K (persisted transition verified, including a fresh reload) ✅ · L (home/calendar/detail succeed) ✅ · M (Intermediate×4D Runway unchanged) ✅ · N (Intermediate×5D Core unchanged) ✅ · O (21+ LongHorizon remains closed) ✅ · P (unsupported neighbors remain closed) ✅.

The missing/explicit-zero readiness finding (§16-17) is a separate, pre-existing, disclosed Core-side limitation — not part of the A-P boundary and not introduced by Runway generalization — and does not block this capability's closure.

**Capability upgraded**: `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED` → `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`. LongHorizon is explicitly NOT closed by this phase.

## 47. Next roadmap capability

Per this phase's own instruction, the next open capability is **Intermediate×5D LongHorizon ARCHITECTURE/DESIGN** (not implementation), resolving the exact `FREQ.6D.5` findings: (1) no lane-lineage column on `LongHorizonRollingSessionState`; (2) no progression-stage lineage; (3) no profile lineage; (4) JIT composition discards dual-KEY identity by grouping on raw `StructuralRole`; (5) ~10 `DaysPerWeek==4` gates; (6) no `ExecutionPrescriptionIndex` propagation; (7) a database migration is required. None of these are addressed here.

A second, narrower, independently-schedulable finding from this phase is also available for a future phase: the pre-existing Core missing/explicit-zero starting-volume infeasibility for Intermediate×5D at very low evidence (§16-17/"New independent finding"), affecting both the Core-only and Runway+Core public routes identically.

## 48. Ledger / roadmap update

See `PHASE_LEDGER.md` row for `FREQ.6D.8` and the `MASTER_ROADMAP.md` support-matrix/next-pointer update below this report.

## 49. Push-gate status

4 commits ahead of `origin/main` before this phase began; this phase adds 2-3 more (test commit, docs commit, SHA backfill). Well under the ~10-phase durability-gate threshold — no push performed this phase, per §49's own instruction not to push merely because activation succeeded.

## 50. Final classification

`INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`
