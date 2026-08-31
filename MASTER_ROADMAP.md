# Appsel Backend Master Roadmap

This is a **living planning document**, not append-only history. For what actually exists/happened, see `PHASE_LEDGER.md` — this file is never a substitute parent authority. A roadmap label is not a Phase ID until its prompt/report is actually created and ledgered.

---

## 1. Backend V1 Scope

**Distances**: 10K · Half Marathon / 21.1K · Marathon / 42.2K

**Levels**: Beginner · Intermediate · Advanced

**Frequency**: 3D · 4D · 5D · 6D · 7D

- `2D = BACKLOG` unless later separately authorized.
- `Expert = OUT OF V1` unless later separately authorized.

A completed cell is not automatically `PUBLICLY_ACTIVE`. Per §12 (Support-State Vocabulary), a cell may end as `PUBLICLY_ACTIVE`, `GATED`, `PRODUCT_INELIGIBLE`, or `PROVEN_NON_SUPPORT` depending on real evidence/product authority. Not every matrix cell must become public.

---

## 2. Current Canonical State

Sourced from `PHASE_LEDGER.md` only (per this roadmap's own rule — never chat history).

### 10K support matrix (current, repository-verified)

|               | 2D | 3D | 4D | 5D | 6D | 7D |
|---|---|---|---|---|---|---|
| **Beginner** | `TWO_D_CORE_EIGHT_AND_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL` (GEN.18, dark-only) — Core confirmed representable 10-14wk (binding/volume/Adaptation dark-verified, GEN.17/GEN.18), 8-9wk formally non-representable final (GEN.18); Runway/LongHorizon disclosed as remaining | `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL` (Core only; GEN.5C) | `PUBLICLY_ACTIVE` (Core; GEN.4E) | `PRODUCT_NON_SUPPORT` (GEN.6 — two-KEY Core eligibility rejected) | `PRODUCT_NON_SUPPORT` (GEN.6 — same as 5D) | `PRODUCT_NON_SUPPORT` (GEN.6 — inherited frequency-global calendar gap) |
| **Intermediate** | `TWO_D_CORE_EIGHT_AND_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL` (GEN.18, dark-only) — Core confirmed representable 10-14wk (binding/volume/Adaptation dark-verified, GEN.17/GEN.18), 8-9wk formally non-representable final (GEN.18); Runway/LongHorizon disclosed as remaining | `PUBLICLY_ACTIVE` (Core; GEN.3B) | `PUBLICLY_ACTIVE` (pre-existing/Adaptation V1 baseline) | `PUBLICLY_ACTIVE`, full horizon (Core/Runway/LongHorizon; `FREQ.6D.22`) — `INTERMEDIATE_5D_FULL_HORIZON_CAPABILITY_COMPLETE` | `PUBLICLY_ACTIVE`, full horizon (Core/Runway/LongHorizon; `FREQ.6D.27`) — `INTERMEDIATE_6D_FULL_HORIZON_CAPABILITY_COMPLETE` | `PRODUCT_NON_SUPPORT` (`FREQ.6D.23` — real calendar-spacing conflict at zero-rest-day cadence plus injury-incidence evidence, final) |
| **Advanced** | `OUT_OF_V1_SCOPE` (product decision, never designed) | `PUBLICLY_ACTIVE` (Core/Runway/LongHorizon; GEN.10) | `PUBLICLY_ACTIVE` (Core/Runway/LongHorizon; GEN.10) | `PUBLICLY_ACTIVE`, full horizon (Core/Runway/LongHorizon; GEN.10) | `PUBLICLY_ACTIVE`, full horizon (Core/Runway/LongHorizon; GEN.10) | `PRODUCT_NON_SUPPORT` (GEN.7 — inherited frequency-global calendar gap) |

Beginner×3D Runway (15-20wk) is separately confirmed non-representable (FREQ.2) with zero live-cell exposure (FREQ.2A) — this is a Runway-horizon finding layered on top of the Core-level `PROVEN_NON_SUPPORT` result above, not a duplicate claim.

**`INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE`** (as of `FREQ.6D.27`): every Intermediate 10K frequency cell (3D/4D/5D/6D/7D) now carries a final, evidenced classification — 4 `PUBLICLY_ACTIVE`, 1 `PRODUCT_NON_SUPPORT`. This does not imply the 10K matrix as a whole is complete — Beginner and Advanced remain largely unopened at 5D/6D/7D.

**`BEGINNER_FREQUENCY_AUTHORITY_COMPLETE`** (as of `GEN.6`): every Beginner 10K frequency cell now carries a final, evidenced classification — 4D `PUBLICLY_ACTIVE`, 3D/5D/6D/7D all non-support (3D for numeric/structural reasons scoped to Core only; 5D/6D because Beginner-level eligibility rejects any RunLayout requiring two structural KEY sessions per week, a decision that cascades to Runway/LongHorizon since both culminate in the same real Core execution; 7D because it inherits the frequency-global calendar week-boundary gap `FREQ.6D.23` found, reinforced by Beginner-specific injury evidence). **Beginner support remains 4D-only.** This is `BEGINNER_FREQUENCY_AUTHORITY_COMPLETE`, not `BEGINNER_PUBLIC_CAPABILITY_COMPLETE` in a new sense — no new frequency was approved, so nothing further requires implementation or activation.

**Advanced axis authority status** (as of `GEN.8`): unlike Beginner, Advanced's two-KEY decision was approved — Appsel's own real, once-`VALIDATED` historical `PEAK_VOLUME_BANDS_V1.v1/v2` Advanced×5D row (`[42,58]`) already modeled a dual-KEY product, and no injury-risk/coaching-convention concern applies to the advanced-tier population. Both of `GEN.7`'s disclosed gaps were closed by `GEN.8`: (1) missing-readiness → `PRODUCT_INELIGIBLE`; (2) 8 new `ADVANCED_*` dual-KEY prescription profiles frozen, reusing Intermediate's exact structural dose verbatim.

**Advanced axis implementation status** (as of `GEN.9`): implemented and dark-verified through real PostgreSQL. Single-KEY 3D/4D reuse the existing Intermediate/Beginner master-template chain with zero new catalog content; dual-KEY 5D/6D required one new, purely additive catalog chain (`TEN_K_MASTER v9`/`TEN_K_WORKOUT_PROGRESSION_V1 v7`) plus a new published release `1.3.0`. Found and fixed 6 real production defects along the way — most significantly, a second, entirely separate Runway-numeric-policy dispatch (`TenKPreparationRunwayNumericPolicyFactory`) that had been silently defaulting every non-Intermediate candidate to the 4D-shaped policy, discovered via a real full lifecycle test failure and root-caused by direct diagnostic tracing. Verified via 38 new dark tests including one full real-PostgreSQL GE→Runway→Core lifecycle for Advanced×5D reaching organic Core with real `ADVANCED_*` dual-KEY profiles; 3D/4D/6D verified via structural/numeric authority directly rather than repeating the full lifecycle proof per frequency (disclosed, matching `FREQ.6D.26`'s own established practice). Public routing gate deliberately left untouched throughout, verified closed by permanent isolation tests.

**2D axis authority status** (as of `GEN.11`): a new product-direction decision (`APPSEL_10K_2D_FREQUENCY_RESEARCH_CLAUDE_HANDOFF.md`) requires `Beginner×2D` and `Intermediate×2D` to become real supported capabilities (`Advanced×2D` remains explicitly out of V1). This reopens the otherwise-`COMPLETE` Intermediate and Beginner frequency axes for exactly this one new cell each — the prior `INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE`/`BEGINNER_FREQUENCY_AUTHORITY_COMPLETE` classifications remain historically accurate for their original 3D-7D scope and are not retracted. `GEN.11` froze the chosen `RUN_LAYOUT_2D` Model B repeating A/B structural pattern (`KEY_SESSION+LONG_RUN` / `EASY_SUPPORT+LONG_RUN`, alternating, taper-overridden), new `PeakVolumeBand`/`ResolvedPeakReference` values for both levels (`Beginner [16,22]km`/19.0, `Intermediate [20,30]km`/25.0, each independently evidence-derived), Core/Runway/LongHorizon representability (proven via a monotonic growth-ratio argument against an already-`PUBLICLY_ACTIVE` analog), taper (reuses canonical 0.53), long-run allocation (new 55%/60% frequency-owned figure), a new 2-session Adaptation dispatch arm, calendar (reuses canonical KEY↔LONG spacing), and missing/zero readiness (`PRODUCT_INELIGIBLE`, no default, for both levels). Confirmed a real, disclosed architecture gap: every existing `RunLayout` catalog document assumes one fixed weekly role list, which cannot represent a week-parity-dependent pattern — Phase `GEN.12`'s first implementation item. **Authority is `COMPLETE` and ready for implementation.**

**2D axis binding blocker — escalated, then Option 2 proposed provisionally** (as of `GEN.14`): `GEN.13` classified `GEN.12`'s disclosed `ProgressionStageAllocator` lane/week-eligibility gap as a genuine training-methodology question and escalated it (no existing precedent anywhere in this codebase handles a lane with less than 100% weekly structural coverage). Direction was given to proceed with Option 2 (author new 2D-specific progression content). `GEN.14` independently re-verified the supplied external evidence base (found it real, found one additional nuance strengthening the maintenance-vs-progression caution, found a stronger primary source than supplied, and confirmed the central evidence gap — no study measures progression under a structurally-alternating zero-quality-session cadence — is genuine) and derived a provisional proposal: Pattern-A-week-denominated lane capacity combined with newly-authored, per-level exposure minimums/maximums halved (`ceil(weekly/2)`) from each level's own existing weekly-cadence numbers, explicitly not reusing unmodified weekly numbers (rejected Option 1) and explicitly not weakening the existing fail-closed capacity guarantee (rejected Option 3). Confidence stated as LOW-TO-MODERATE — an extrapolation from adjacent maintenance evidence, not a direct measurement.

**`GEN.15`** then explicitly compared Halving against two real alternatives (Duration Extension, Min-Only Reduction) on the same evidence base before allowing sign-off to be sought, rather than presenting Halving as the only option considered. Duration Extension was rejected: no evidence source tests it (the closest analog held the calendar window fixed and halved count instead), and it would independently conflict with `GEN.11`/`GEN.12`'s already-locked Core-cycle-length parity across frequencies. Min-Only Reduction was rejected: it targets exposure *count*, not per-session dose (the physiological idea behind it already belongs to `GEN.11`'s own frozen `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D`), and Spiering et al.'s own framing argues against trading reduced frequency for inflated per-session dose. The evidence base's silence on "how much vs. how long" was disclosed explicitly rather than defaulted past. Halving stands, now comparatively justified rather than reached for by default.

**`GEN.16`** recorded an actual human sign-off: the Halving mechanism is approved as-is, no revision, and is now **final authority**, not provisional. The disclosed reasoning for accepting LOW-TO-MODERATE confidence rather than holding for further research is recorded on the ledger row: a planned future product feature will let athletes change their own weekly training-day count, giving anyone who finds 2D's biweekly quality cadence limiting a real, planned escape path — this materially lowers the cost of the proposal being imperfect. **Phase F (completing 2D's remaining scope — binding, volume/long-run planning, Adaptation, Runway, LongHorizon) is unblocked and proceeds next.**

**2D axis Core implementation status** (as of `GEN.12`, `DONE (PARTIAL)`): closed `GEN.11`'s first-disclosed architecture gap with an additive, zero-delta repeating-pattern mechanism (`PlanCatalogCandidateSummary.WeeklyPatternRoles`/`PatternPeriodWeeks`, both `null` for every pre-GEN.12 candidate), authored `RUN_LAYOUT_2D` and both new `TEN_K__2D__BEGINNER`/`TEN_K__2D__INTERMEDIATE` combinations (single-KEY, zero new prescription content, reusing the existing single-lane `TEN_K_MASTER v6` catalog lineage rather than the dual-KEY v9/v10 one), implemented `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D` and the frequency-owned missing/zero-readiness exception per `GEN.11`'s exact frozen values. Found and fixed 3 real defects, all "assumed every week has ≥1 KEY_SESSION" hardcodes exposed for the first time by 2D's zero-KEY_SESSION Pattern-B week (a combinatorics `k==0` edge case and two sibling role-count validators) — dark-verified via 11 new tests proving real structural skeleton generation and calendar day-assignment for both levels, plus zero-delta for existing frequencies. **Explicitly not yet implemented** (at GEN.12 time): workout-content binding (`ProgressionStageAllocator` allocates progression-stage exposure across every literal calendar week with no concept of a week lacking a KEY_SESSION slot for a given lane — a genuine architecture question requiring its own dedicated design pass, correctly disclosed rather than patched around), and everything downstream of it (full volume/long-run planning, Adaptation, Preparation Runway, LongHorizon). Public gate untouched throughout.

**2D axis Phase F completion status** (as of `GEN.17`, `DONE (PARTIAL)`): implemented `GEN.14`/`GEN.15`/`GEN.16`'s now-final Halving mechanism, closing the `ProgressionStageAllocator` blocker above with a 2-line additive change (a lane's real capacity/layout now resolves against only the skeleton weeks structurally carrying a slot for that lane — reusing `CatalogStageToWeekMaterializer`'s already-resolved per-week `SessionSlots.StructuralRole` data and `CatalogWorkoutBinder`'s own "structural ordinal N = LaneOrdinal N" rule, not a second re-derived mechanism; byte-identical for every pre-GEN.12, 100%-lane-coverage frequency). Found and fixed one sibling hardcode of `GEN.12`'s own defect class in `GeneratedCatalogStageScheduleValidator`. Authored new 2D-specific progression-stage catalog content (`TEN_K_WORKOUT_PROGRESSION_2D_V1 v1`) per `GEN.14`'s exact halving formula, confirming both levels' single-lane lineage has always shared one weekly-cadence source (`TEN_K_WORKOUT_PROGRESSION_V1 v5`), so independently-halved output is correctly identical for both levels. Achieved full, real, dark-verified end-to-end workout-content binding for both `TEN_K__2D__BEGINNER`/`TEN_K__2D__INTERMEDIATE` at the **12-week and 14-week** Core horizons (both levels, both `GOAL_PACE_REHEARSAL` branches); confirmed full volume/long-run planning now closes `GEN.12`'s own disclosed gap for those horizons (`PeakVolumeBand`/long-run-share/missing-readiness authority all verified against a real generated plan); implemented `GEN.11 §9`'s frozen 2-session Adaptation dispatch arm (`2/2 Progress, 1/2 Maintain, 0/2 Reduce`) in `NextWindowLoadDecisionPolicy`, a deliberate non-extrapolation of the N≥5 role-gated model. **Disclosed, not patched around**: the **8-week Core-minimum horizon** is a real, deterministic capacity gap for 2D at both levels — `TEN_K_MASTER`'s own frozen 8-week-minimum phase allocation puts `RACE_SPECIFIC` at exactly 1 real Pattern-A week against 2 halved-minimum top-level stages with zero remaining compression headroom, confirmed empirically (`ProgressionPhaseCapacityInsufficientException`), not invented around by reclassifying any frozen stage semantic or phase bound. **Preparation Runway and LongHorizon remain not implemented** — investigated for real gaps rather than assumed trivial, and found to have their own genuine, disclosed, multi-file scope gaps unrelated to any training-methodology question: `LongHorizonStructuralMaterializer`'s explicit `daysPerWeek` admission gate does not admit 2, the GE structural selector's zero-easy-support case is untested for any frequency, and `TenKPreparationRunwayNumericPolicyFactory` has no 2D dispatch branch — each disclosed with exact file/line locations for a dedicated follow-on phase, rather than patched with a partial, undertested change. 25 new tests (25/25 pass); existing 4D/6D/Advanced dark suites and `Gen12TwoDayDarkVerificationTests` re-run explicitly and confirmed unaffected; `PlanCatalog.Tests` 1510/1510; full regression run with the same 2 durable pre-existing baseline failures, zero new regressions. No public routing/gate change; no already-`PUBLICLY_ACTIVE` frequency affected; no new product/numeric authority beyond `GEN.11`/`GEN.14`/`GEN.15`/`GEN.16`'s own frozen values. Next: a dedicated Preparation Runway 2D phase (starting from the three disclosed gate locations), then a dedicated LongHorizon 2D phase, then a small product decision on the disclosed 8-week Core-minimum gap — none scheduled as Phase IDs here.

**2D axis Core representability boundary** (as of `GEN.18`, `DONE`): individually tested 2D Core representability at every week count 8 through 14 (not just the 12/14 endpoints `GEN.17` had directly tested), both levels, via the same real pipeline harness `GEN.17` itself used — no production code changed, verification/classification only. Derived the exact mechanical outcome analytically first, directly from `TEN_K_MASTER v11`'s frozen phase-week bounds (`FOUNDATION [2,3,4]`, `BUILD [3,4,5]`, `RACE_SPECIFIC [2,4,4]`, `TAPER [1,1,1]`), the frozen global odd/even week-parity pattern (`GEN.11 §1/§11`), and `GEN.16`'s now-final halved RACE_SPECIFIC minimums, then confirmed the derivation empirically (18 new tests, 18/18 pass): **weeks 8 and 9 both fail** with the identical `ProgressionPhaseCapacityInsufficientException`/`RACE_SPECIFIC` shortfall `GEN.17 §6` diagnosed at 8 weeks (RACE_SPECIFIC's own span contains exactly 1 real Pattern-A week against a combined halved minimum of 2, zero compression headroom, at both horizons — confirmed the same mechanism, not a distinct defect, before concluding); **weeks 10 through 14 all succeed**, identically for both Beginner and Intermediate (both levels independently tested, not assumed identical — confirmed identical for a stated, verified reason: both reuse the same shared `TEN_K_WORKOUT_PROGRESSION_V1 v5`-derived halved content). **Confirmed final 2D Core representable range: 10-14 weeks, both levels.** Classified the 8-9 week range `TWO_D_CORE_EIGHT_AND_NINE_WEEK_NON_SUPPORT_FORMALIZED_FINAL`, modeled on this repository's real Beginner×3D precedent shape (`GEN.5C`'s `BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL`, confirmed via direct repository search — not the differently-shaped `PROVEN_NON_REPRESENTABLE_UNDER_APPROVED_*_POLICY` string an earlier phase once speculated and `GEN.6` itself corrected as non-existent in this repository's real convention). Did **not** reopen `GEN.14`/`GEN.16`'s Halving formula: rescuing 8-9 weeks would require reopening one of three already-frozen, out-of-scope authorities (`GOAL_PACE_REHEARSAL`'s `GEN.7`-frozen Protected/FixedExposure semantic, `RACE_SPECIFIC`'s `TEN_K_MASTER`-frozen phase-week bound, or the Halving formula itself) — none reopened, so this closes as a formal, final non-representability determination, not a `DOMAIN_DECISION_REQUIRED` escalation. Full `RunningApp.IntegrationTests` regression run alone (confirmed zero concurrent dotnet processes before and throughout the ~41-minute run): **4088 total, 4085 passed, 3 failed** — the identical 3 named pre-existing baseline tests `GEN.17` already root-caused (`Gen4EBeginnerFourDayPublicActivationTests` at weeks:13/14, `Sw09ExplicitZeroReadinessEndToEndTests`), zero new regressions (4088 = GEN.17's own 4070 baseline + this phase's 18 new tests). `PlanCatalog.Tests` unaffected (no catalog change in this phase). Zero production code changed; zero already-`PUBLICLY_ACTIVE` frequency affected by construction. **Phase I (`GEN.19` — Preparation Runway + LongHorizon for 2D) proceeds next**, unmodified by this phase's outcome (no Halving-formula reopening occurred).

**Advanced axis public activation status** (as of `GEN.10`): the real public HTTP/PostgreSQL routing gate is now open for Advanced 3D/4D/5D/6D across Core (8-14wk), Preparation Runway (15-20wk), and LongHorizon (21-52wk) — widened in `V1CatalogPilotIdentityPolicy`/`LongHorizonPublicPlanService.ValidatePilot`, implementing only already-approved `GEN.7`/`GEN.8`/`GEN.9` authority (no new product/numeric decision). Found and fixed 8 real production defects during activation, all pre-existing and newly surfaced by this being the first time Advanced traffic reached these code paths: two hardcoded `Level = RunningBackground.Intermediate` bugs in `LongHorizonPublicPlanService` (preview generation and `BuildTrainingPlan` — the latter would have persisted every confirmed Advanced plan's `Level` column as Intermediate); a stale `PublishedBundleReleaseVersion` configuration pin (`appsettings.json` still read `1.2.0` though `GEN.9` had already published `1.3.0`, so every ProfileBacked Advanced session failed closed with "no ExecutionPrescriptionIndex"); a hardcoded `Level == RunningBackground.Intermediate` request-identity check in `TenKPreparationRunwayDarkOrchestrator.ValidateRequest` that rejected every real Advanced Runway composition outright; two missing `Level` parameter thread-throughs to `LongHorizonStructuralMaterializer` (`LongHorizonRollingInitialActivationRuntime` and `LongHorizonRollingStateReconstructionService`) that silently defaulted every Advanced LongHorizon skeleton's structural identity to Intermediate (hard-failing for 3D, which has no Intermediate equivalent to coincidentally match slot counts, and silently mis-identifying 4D/5D/6D); a hardcoded `Level = "Intermediate"` at initial `LongHorizonRollingPlanState` persistence in `LongHorizonRollingStateRepository`; a hardcoded `Level` in `LongHorizonRollingCoreGenerationInputAdapter` that caused every real Advanced GE→Runway boundary handoff to fail as an opaque `LONG_HORIZON_CONTINUATION_BLOCKED`; and a stale packaged-bundle-release pin in `RunningApp.Api.csproj` (still `1.1.0`, never bumped alongside `appsettings.json` at `FREQ.6D.26`'s `1.2.0` or `GEN.9`'s `1.3.0` — every real packaged/published build had been silently missing 6D Intermediate's execution bundle since `FREQ.6D.26`, and would have shipped with Advanced entirely non-functional). Verified via 51 new tests (`Gen10AdvancedCombinedPublicActivationTests`) proving real public HTTP + PostgreSQL across all three horizon bands for all four frequencies, confirm+fresh-reload with correct persisted `Level`/`DaysPerWeek`, a full real GE→Runway→Core dual-KEY lifecycle with a real repair regression, Home/Calendar reads, and explicit real-HTTP confirmation that Advanced×2D/7D remain unreachable. Opening Advanced 3D/4D/5D/6D as real public identities made 12 pre-existing "Advanced is unsupported" test assertions factually obsolete across 9 files — each corrected by advancing/removing only the specific obsolete row, matching this engagement's established pattern. Full regression 4034/4034 (4032 passed, same 2 durable pre-existing baseline failures, zero new regressions). **Advanced×3D/4D/5D/6D are all `PUBLICLY_ACTIVE`.** Advanced×7D remains `PRODUCT_NON_SUPPORT` (GEN.7); Advanced×2D remains `OUT_OF_V1` (never designed, confirmed still unreachable through the widened gate by real HTTP request). **`ADVANCED_TEN_K_FREQUENCY_AXIS_COMPLETE`** — the Advanced 10K frequency axis now matches Intermediate's own completion shape (4 `PUBLICLY_ACTIVE`, 1 `PRODUCT_NON_SUPPORT`, plus 2D out of V1 scope).

### Current active phase / next phase

**Latest verified completed phase**: `FREQ.6D.12` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_GE_PRODUCT_AND_NUMERIC_POLICY_APPROVED` / `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTATION_READY`. Closed `FREQ.6D.11`'s one remaining gap: the LongHorizon GE segment's own 5D weekly structure and numeric coefficients. Selected 1 KEY + 3 EASY + 1 LONG (5 sessions, constant, no ramp) as a direct generalization of both existing 4D GE (1 KEY + 2 EASY + 1 LONG) and the already-approved 5D Preparation Runway shape; catalog capacity confirmed sufficient, zero new content. Missing/explicit-zero readiness resolved as `PRODUCT_INELIGIBLE` by carrying forward GE's own existing fail-closed rule (never had a fallback default, even for 4D) rather than borrowing Core/Runway's 26.0/19.5km values. Quantified a real problem with extending 4D GE's uncapped growth model to 32 GE weeks (simulated ~70+km/week by week 32, exceeding Core's own 44.5km peak) and replaced it with a target-capped model plateauing at the existing, already-approved 44.5km peak reference and 28%/36% long-run share — zero new numbers invented anywhere. Full 21-52 week representability verified for positive-observed readiness. No Core/Runway/production-code/migration/routing/catalog change. Next: the combined LongHorizon implementation wave (`FREQ.6D.11` Splits A-D + this phase's GE policy) — not yet scheduled as a Phase ID.

Prior phase: `FREQ.6D.11` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_ARCHITECTURE_APPROVED_PRODUCT_POLICY_REQUIRED`. Designed the complete Intermediate×5D LongHorizon dual-KEY lineage/JIT-composition/persistence/execution-context architecture: session identity (`LaneOrdinal`+`SlotOrdinal`, mirroring `BoundCatalogSession`'s own shape), 5 new nullable `LongHorizonRollingSessionState` columns (zero backfill, zero-delta for historical 4D rows), JIT composition keyed on `(StructuralRole, LaneOrdinal, SlotOrdinal)` (fixing a confirmed real dual-KEY collision in `BuildBoundedCoreSelection`'s raw-`StructuralRole` grouping), frozen-bundle profile-binding lifecycle, and `ExecutionPrescriptionIndex` propagation reusing the existing shared `TenKPreparationRunwayDarkOrchestrator` (no duplicate engine). Discovered LongHorizon's real Runway relationship directly from `LongHorizonCompositionResolver`: 21-52wk = GE (variable) + the real Preparation Runway (fixed 8wk, already 5D-capable since `FREQ.6D.10`) + the real Core (fixed 12wk) — so Runway/Core segments (20 of every 21-52 weeks) inherit existing approved authority automatically. Only the LongHorizon-only GE segment has no approved 5D weekly structure/numeric coefficients (its catalog is `DRAFT`/never-loaded, 4D-shaped by construction) — correctly flagged `PRODUCT_DECISION_REQUIRED`/`NUMERIC_DECISION_REQUIRED` rather than inferred. Re-audited 4D-only gates exhaustively: 21 lines/14 files (vs. `FREQ.6D.5`'s ~10 estimate), each classified. Confirmed 5-session Adaptation already generic/lane-blind, no new policy needed. Defined a 6-split implementation decomposition with explicit dependency order. No production code/migration/routing touched. Next: a narrow GE-segment product/numeric-decision phase.

Prior phase: `FREQ.6D.10` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED`. Wired the `FREQ.6D.9`-confirmed existing `FREQ.6C` Intermediate×5D numeric authority into Core and Preparation Runway; real HTTP + real PostgreSQL verified across all previously-failing horizons.

Prior phase: `FREQ.6D.9` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`. Reconciled the `FREQ.6D.8`-disclosed Intermediate×5D missing/explicit-zero Core starting-volume failure against the complete `FREQ.6`/`FREQ.6B`/`FREQ.6C` authority chain; found `FREQ.6C` already approved exact 5D-specific values, all `ELIGIBLE`, but runtime never wired to them. No production code touched — evidence/decision only.

Prior phase: `FREQ.6D.8` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED`. Real HTTP + real PostgreSQL verification for Intermediate×5D Preparation Runway (15-20 weeks); found and disclosed (not fixed) the pre-existing Core starting-volume defect `FREQ.6D.9` reconciled.

Prior phase: `FREQ.6D.7` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED`. Generalized the Intermediate×4D Preparation Runway implementation to also support Intermediate×5D, reusing the existing Runway engine and existing generic Core pipeline (no second implementation, `RUN_LAYOUT_5D` untouched).

Prior phase: `FREQ.6D.6` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_RUNWAY_PRODUCT_POLICY_APPROVED`. Resolved both open Preparation Runway product authorities for Intermediate×5D (weekly structure 1K+3E+1L, starting-volume reuse of the already-live Core numeric authority). No production code touched.

Prior phase: `FREQ.6D.5` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_RUNWAY_LONGHORIZON_SEPARATE_WAVES_REQUIRED`. Full pipeline trace of Preparation Runway and LongHorizon against Core 5D; found Runway hardcoded to 4D at three layers with no product authority for its 5D weekly shape (the gap `FREQ.6D.6` closed), and LongHorizon hardcoded even more pervasively with a deeper schema-level gap remaining fully open.

Prior phase: `FREQ.6D.4D.5G` — Execution Status `DONE`, Final Classification `FREQ6D4D5G_CORE_HORIZON_CONTEXT_AND_PUBLIC_5D_ACTIVATION_IMPLEMENTED`. Fixed the CompressedCore/ExtendedCore execution-context propagation gap and completed the fifth public-activation retry across all four Core horizons. **Parent `FREQ.6D.4D` closed as `FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED`.**

Prior phase: `FREQ.6D.4D.5F` — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D5F_MAPPING_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`. Implemented the approved public workout-type mapping and found the CompressedCore/ExtendedCore execution-context gap `FREQ.6D.4D.5G` fixed.

Prior phase: `FREQ.6D.4D.5E` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_PUBLIC_WORKOUT_MAPPING_CLOSURE_APPROVED`. Derived the complete real Intermediate×5D workout closure fresh from the catalog and decided the `AEROBIC_STRENGTH_CONTROLLED_INTRO` → `Interval` mapping `FREQ.6D.4D.5F` implemented.

Prior phase: `FREQ.6D.4D.5D` — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D5D_TAPER_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`. Implemented the approved `FREQ.6D.4D.5C` Legacy/ProfileBacked Taper-completeness partition in `CatalogPrescriptionContextValidator`, and found + fixed a second occurrence of the identical root cause in `CatalogFinalPrescribedPlanValidator`. Both real 5D Taper lanes proven valid through both validators without stage-name special-casing; zero legacy delta. Retried public activation a third time, progressed past calendar and both Taper checks for the first time, then found the `V1CatalogPublicWorkoutTypeMappingPolicy` gap `FREQ.6D.4D.5E` resolves.

Prior phase: `FREQ.6D.4D.5C` — Execution Status `DONE`, Final Classification `TAPER_COMPLETENESS_EXISTING_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`. Traced `CatalogPrescriptionContextValidator`'s hardcoded `TAPER_SHARPEN` check to its real origin and found the real completeness authority for ProfileBacked Taper already exists downstream (Split-C's execution-resolution guarantee); approved the Legacy/ProfileBacked partition `FREQ.6D.4D.5D` implements.

Prior phase: `FREQ.6D.4D.5` (Split E, partial) — Execution Status `DONE (PARTIAL)`, Final Classification `FREQ6D4D_SPLIT_E_PARTIAL_RUNTIME_DISCOVERY_IMPLEMENTED_PUBLIC_ACTIVATION_BLOCKED`. Authored the complete real Intermediate×5D PlanCatalog chain — `RUN_LAYOUT_5D`, `TEN_K__5D__INTERMEDIATE` combination, dual-lane progression, all 8 real production profiles promoted `VALIDATED`, a real CLI-published `1.1.0` release with `ExecutionPrescriptions` present for all 8 profiles — 1,510/1,510 PlanCatalog.Tests. Wired RunningApp's runtime published-bundle discovery end to end (`IPublishedTemplateBundleLoader`/`PublishedTemplateBundleLoader`, threaded through `CatalogPreviewGenerator`'s full constructor chain, DI, `PlanCatalog:PublishedBundleReleaseVersion` config, deployment packaging) — the exact gap Split C disclosed, now closed, though no candidate reaches it in production yet. Attempted public activation of `TEN_K__5D__INTERMEDIATE` for the 8-14 week Core route only (widest activation the user approved); real end-to-end HTTP testing (not static analysis) found `CatalogWeekSkeletonCalendarMaterializer` hardcodes exactly one `KEY_SESSION` slot per week in both its validation and its date-assignment backtracking algorithm — a real 5D week has two (LaneOrdinal 0/1), which would either be rejected outright or, if the count guard were removed, silently collide both slots onto the same calendar date. The routing widening was reverted rather than shipped with a confirmed live 500. Full regression: 3,612/3,613 RuntimeCatalog (1 pre-existing unrelated `Sw09` failure, unrelated to 5D), 1,510/1,510 PlanCatalog.

Prior phase: `FREQ.6D.13` — Execution Status `DONE (PARTIAL)`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_ROLLING_LINEAGE_AND_JIT_DUAL_KEY_IMPLEMENTED_AND_VERIFIED_GE_IMPLEMENTATION_REMAINING`. Implemented and real-PostgreSQL-verified rolling-session lineage persistence (5 new nullable columns, zero 4D delta) and the JIT dual-KEY collision fix (`BuildBoundedCoreSelection` now matches by exact `SlotOrdinal` identity). Discovered and fixed two real gaps blocking any 5D candidate from reaching real Core generation via LongHorizon rolling-activation: `PublishedBundleReleaseVersion` threading (correcting `FREQ.6D.11`'s stale finding that `ExecutionPrescriptionIndex` propagation was entirely missing — it was already present since `FREQ.6D.7`, just unreachable from this one call site) and `IsValidFourDayAvailability`'s hardcoded `==4` gate. Did **not** implement `FREQ.6D.12`'s approved GE 5D structural/numeric policy or the dependent dark 21-52 week verification — deliberately scoped out rather than risk an incomplete change to already-shipped 4D GE logic, disclosed explicitly as remaining work, not a blocker (no architecture/product/numeric-authority contradiction found).

Prior phase: `FREQ.6D.14` — Execution Status `DONE (PARTIAL)`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_GE_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL`. Implemented the `FREQ.6D.12`-approved GE 5D structural/numeric policy (1 KEY + 3 EASY + 1 LONG, 44.5km target cap with plateau and 28%/36% long-run share both reused verbatim from `FREQ.6D.10`'s `VolumeSafetyPolicy.FiveDayIntermediate`, missing/explicit-zero → typed `PRODUCT_INELIGIBLE`), generalized `LongHorizonGeWeekDescriptor` off its own resolved EASY count rather than a hardcoded shape. Dark-verified the full 21/24/28/32/40/52 week matrix (35 new tests: structure, readiness matrices, cap/plateau, long-run share, GE→Runway and Runway→Core dual-KEY continuity exercising `FREQ.6D.13`'s own fix end-to-end, determinism, 4D zero-delta). Found and fixed three real gaps only surfaced by actual dark execution (missing `ExecutionPrescriptionIndex` wiring in this orchestrator's own separate Core pipeline, a non-candidate-aware Runway numeric-policy call, two independently-hardcoded "exactly 4 slots" validators). Found, and honestly excluded rather than hid, a genuine pre-existing, non-5D-specific 22-week Runway numeric-continuity gap (confirmed via direct 4D repro). Did **not** complete real PostgreSQL persistence for the 5D GE rolling-activation path specifically, or full adaptation/repair verification through that path — both disclosed as open, not a blocker.

Prior phase: `FREQ.6D.15` — Execution Status `DONE (PARTIAL)`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_DARK_COMPLETION_BLOCKED_ON_SHARED_22_WEEK_NUMERIC_AUTHORITY`. Root-caused the 22-week gap: `PreparationRunwayNumericMaterializer` linearly interpolates from GE's exit volume to Core's independently-computed Week-1 target and fails closed with no reduction rule when GE's exit exceeds it. Proved this is systemic, not narrow — 23/25/26/27 weeks fail identically (any GE segment not ending on a Recovery week keeps climbing), even a Recovery-terminal 28-week horizon fails at a low baseline — and day-count-neutral (direct 4D repro fails identically). Classified as a genuine unresolved numeric authority gap and correctly did not invent a value to force a fix. Separately completed real PostgreSQL persistence/fresh-reload verification for the 5D GE rolling-activation window (`LongHorizonRollingInitialActivationRuntime`, which never reaches the broken boundary), fixing two real gaps found only via actual dark/DB execution (a role-key collision silently merging a 5D week's 3rd EASY session into the 2nd; a reload-projection that silently dropped the FREQ.6D.13 lineage columns despite writing them correctly). Did **not** complete persisted adaptation/repair verification — `LongHorizonRollingCheckpointRuntime` not touched.

Prior phase: `FREQ.6D.16` — Execution Status `DONE`, Final Classification `SHARED_10K_LONGHORIZON_22_WEEK_NUMERIC_AUTHORITY_APPROVED`. Resolved the shared 22-week GE→Runway numeric-continuity gap via real numeric traces (temporary, uncommitted, read-only diagnostics against production code, fully reverted — zero residual code changes). Root-caused two compounding issues in `PreparationRunwayNumericMaterializer.Materialize`: Runway's starting evidence is always the raw, unclamped GE exit with no reconciliation against Core's own Week-1 target; the reachability check reuses a 0.001km floating-point sum-reconciliation epsilon (`V1FourDaySessionVolumeAllocationPolicy.ToleranceKm`) for an unrelated product-level question — the same class of mistake `FREQ.6D.10` already fixed once, on a different field. Discovered a previously-unnoticed 4D/24-week long-run edge case (Recovery week's reduced long run still exceeds Core's target by exactly one rounding increment) alongside the already-known 22/23-week weekly-volume failures. Approved a generic, no-new-number rule: clamp Runway's starting weekly volume and long run to Core's own already-computed Week-1 target whenever GE's exit would otherwise exceed it — day-count-neutral, conservative (only reduces, never raises), zero GE/Core/Runway structural change. Evidence/decision only, no production code authored.

Prior phase: `FREQ.6D.17` — Execution Status `DONE (PARTIAL)`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_CORE_ENTRY_CLAMP_IMPLEMENTED_AND_DARK_VERIFIED_PERSISTED_ADAPTATION_REPAIR_REMAINING`. Implemented `FREQ.6D.16`'s approved GE→Runway Core-entry clamp at its single shared owner (`PreparationRunwayNumericMaterializer.Materialize`) — no new numeric constant, conservative by construction. Re-verified via real numeric traces that 4D/22, 4D/23, 5D/22, 5D/23, and the 4D/24 long-run edge all now succeed; mechanically re-ran the full 21-52 week dark matrix at 32/32 success using the same representative baseline every other test already uses (the forced near-cap workaround baseline is no longer required). Did **not** complete real PostgreSQL persisted adaptation or persisted repair verification (`LongHorizonRollingCheckpointRuntime` not touched) — `FREQ.6D.15`'s own disclosed remaining scope, still open, not a blocker.

Prior phase: `FREQ.6D.18` — Execution Status `DONE (PARTIAL)`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_PERSISTED_ADAPTATION_AND_REPAIR_VERIFIED_FOR_GE_SEGMENT_RUNWAY_CORE_BOUNDARY_SCENARIOS_REMAINING`. Performed the mandatory repair-authority reconnaissance before writing any repair test, discovering two separate adaptation authorities coexist (`LongHorizonRollingCheckpointRuntime`'s coarse Growth/Maintenance dispatch vs. the actual frozen 5-session severity table implemented by `WindowExecutionSummaryBuilder`/`NextWindowLoadDecisionPolicy`, called only from the real `LongHorizonRollingWindowActivationService`). Found and fixed eight real 4D-hardcoding/lineage-drop defects blocking Intermediate×5D LongHorizon persisted adaptation and repair, most significantly `ScheduleRepairPersistenceService.BuildReplacement` never copying `LaneOrdinal`/`SlotOrdinal`/`ProgressionStageKey`/profile lineage onto a repair replacement session (a 4D+5D bug, not 5D-specific). Traced and fixed the deepest root cause: `LongHorizonRollingStateRepository.InitializeStructuralStateAsync` hardcoded `DaysPerWeek = 4` at plan creation — every 5D plan's persisted `DaysPerWeek` silently reverted to 4, causing every reload to rebuild a 4D-shaped structural skeleton regardless of the plan's real shape. Verified 9 new real-PostgreSQL persist→dispose→reload→continue tests: the full 6-row 5-session severity table against real reloaded rows, end-to-end checkpoint-continuation cardinality/lineage survival, and persisted GE KEY repair/repeated-EASY identity preservation. Did **not** attempt GE→Runway/Runway→Core persisted-adaptation scenarios or Core secondary-KEY repair (no real persisted Core session exists in any available fixture). No new numeric constant, schema, catalog content, or identity-model redesign. Full regression 3882/3884 (same 2 pre-existing failures, +9 new passing).

Prior phase: `FREQ.6D.19` — Execution Status `DONE (PARTIAL)`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_GE_RUNWAY_CORE_BOUNDARY_AND_DUAL_KEY_REPAIR_DARK_VERIFIED_TARGET_FINISH_TIME_PRODUCT_DECISION_REMAINING`. Drove a real 21-week Intermediate×5D LongHorizon plan organically (never a fabricated Core row) from persisted GE through persisted Runway into a real, organically-materialized first Core window via the real production chain. Found and fixed five real defects only surfaced by actually reaching Core for a 5D plan for the first time: `LongHorizonRollingWindowActivationService` never threaded the real, already-configured `PublishedBundleReleaseVersion` into JIT composition (fixed via a new `IOptions<PlanCatalogOptions>` constructor overload); `LongHorizonRollingJitActivationRuntime` called the parameterless `TenKPreparationRunwayNumericPolicyFactory.Build()` instead of the candidate-aware overload, rejecting a genuine approved 5D long-run share; `LongHorizonRealCalendarProjectionAdapter`/`LongHorizonActivatedCalendarAlignmentValidator` each hardcoded an expected 4-sessions-per-week count; `ContinueJitCompositionAsync` had no way to supply `TargetFinishTimeSeconds`/`Source` at all (added as optional parameters, default null, byte-identical for every existing caller). Verified organic first Core week (2K+2E+1L, distinct lanes), ProfileBacked lineage survival, real secondary-KEY repair preserving `LaneOrdinal=1`/untouched `LaneOrdinal=0`, and deterministic repair→continuation, all via 4 new real-PostgreSQL tests. Confirmed the requested date-order-reversal scenario is genuinely `NOT_REACHABLE_UNDER_VALID_REPAIR_CONSTRAINTS` (every repair candidate is structurally restricted to strictly-later dates) rather than a gap. Did **not** resolve the one remaining real gap: no `TargetFinishTimeSource` classification is persisted anywhere for a restarted LongHorizon plan, and choosing how to reclassify it is a genuine product decision correctly not made here. No new numeric constant, schema, catalog content, or identity-model redesign. Full regression 3886/3888 (same 2 pre-existing failures, +4 new passing).

Prior phase: `FREQ.6D.20` — Execution Status `DONE`, Final Classification `TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_AUTHORITY_APPROVED`. Traced the full `TargetFinishTime`/`TargetFinishTimeSource` provenance dataflow and found the exact loss point: the value is already available in-memory at plan confirmation (`snapshot.NormalizedInput.TargetFinishTimeSource`) but never copied onto `TrainingPlan`, which has no column for it — a plan-wide gap (identical in the ordinary Core-only/Runway confirm path), only live and blocking for LongHorizon because it alone reconstructs Core generation later from durable state. Confirmed two source classifications (`ProductAverage`/`UserDefined`, the only two existing values) can legitimately share the same numeric target time, so reverse-inference from seconds is unsafe. Approved plan-level persistence (`TrainingPlan`, alongside the existing `TargetFinishTimeSeconds`) over rolling-state duplication, derive-on-restart, or a new wrapper object — one new nullable string column, no new enum, no index, no DB constraint, `SCHEMA_CHANGE_APPROVED`. Froze the confirmation boundary, restart semantics (read verbatim, never re-derive), and historical-plan handling (`UNKNOWN_LEGACY`, permanent, never backfilled). Specified (design only) the write/read boundaries and an 11-step implementation contract. Confirmed frequency- and distance-neutral. No production code, tests, migration, or catalog content authored.

Prior phase: `FREQ.6D.21` — Execution Status `DONE`, Final Classification `TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_IMPLEMENTED_AND_VERIFIED` / `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`. Implemented `FREQ.6D.20`'s approved authority exactly: added `TrainingPlan.TargetFinishTimeSource` (a genuine nullable enum, round-tripping automatically via this repository's own pre-existing global `SnakeCaseEnumConverter` convention), migrated real PostgreSQL with one nullable column and zero backfill, and populated it at both real confirmation write boundaries (ordinary Core/Runway and LongHorizon) from the already-in-scope normalized request — never recomputed. Threaded the persisted value from `LongHorizonRollingWindowActivationService`'s already-loaded `TrainingPlan` row into `ContinueJitCompositionAsync`'s existing parameters — zero new queries, zero rolling-state duplication. Verified via 8 new tests, including the mandatory same-seconds-different-source regression and a real-PostgreSQL proof that a real Intermediate×5D plan reaches organic Core generation with `GOAL_PACE_TEN_K` succeeding for the first time via a `ProductAverage` source read from the persisted plan alone, with `UserDefined`/historical-null sources failing closed identically without reclassification. No new source classification, no derive-on-restart, no rolling-state duplication, no public activation (confirmed the public gate still rejects 5D). Full regression 3894/3896 (same 2 pre-existing failures, +8 new passing). This closes the entire FREQ.6D.18→19→20→21 arc — the accumulated Intermediate×5D LongHorizon dark-verification capability is now complete.

Prior phase: `FREQ.6D.22` — Execution Status `DONE`, Final Classification `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_PUBLICLY_ACTIVATED` / `INTERMEDIATE_5D_FULL_HORIZON_CAPABILITY_COMPLETE`. Opened the real public routing gate for Intermediate×5D LongHorizon 21–52 by widening 7 hardcoded-4D sites in `LongHorizonPublicPlanService.cs` (`ValidatePilot`, candidate resolution via the existing `V1CatalogPilotIdentityPolicy.ResolveCandidate` dispatch, initial-activation/preview-mapper/confirm-time `DaysPerWeek`, `TemplateId`, `BuildTrainingPlan`'s `DaysPerWeek`) — no new identity invented. Real end-to-end HTTP verification surfaced a genuine 7th hardcoded-4D site the initial reconnaissance missed: `LongHorizonRollingInitializationRequest.DaysPerWeek` defaults to `4` and the confirm-time caller never overrode it, silently reverting every publicly-confirmed 5D plan's persisted `DaysPerWeek` to 4 and blocking the first real continuation call. Root-caused via systematic reverted diagnostics (never guessed); fixed with one line, implementation-only, no new schema/product/numeric decision (§49 boundary respected). Verified via 14 new tests through real public HTTP + PostgreSQL: representative and full 21–52 matrix routing (32/32), ProductAverage/UserDefined persistence across fresh reload, missing-readiness typed rejection, the full public GE→Runway→Core lifecycle reaching organic dual-KEY Core with a real repair proof and a real Adaptation regression (full-adherence GE drives genuine `ProgressAsPlanned`), unsupported neighbors (Beginner×5D, Intermediate×6D/7D) remain closed. Full regression 3908/3910 (same 2 pre-existing failures, +14 new passing, zero new failures, verified twice). This closes the entire Intermediate×5D arc.

## Intermediate×5D: full horizon capability status

**COMPLETE AND PUBLIC.** Core (8–14) = PUBLIC. Preparation Runway (15–20) = PUBLIC. LongHorizon (21–52) = PUBLIC. `INTERMEDIATE_5D_FULL_HORIZON_CAPABILITY_COMPLETE` (FREQ.6D.22). This does **not** mean all 10K is complete — Beginner×5D, Advanced×5D, and Intermediate×6D/7D remain unresolved/closed; only Intermediate×5D's full horizon capability is complete.

**Gate D (PASS: 863bad6)** — pre-activation durability checkpoint before the final Intermediate×5D public-activation phase (a major architecture/capability checkpoint per Hard push gate rule D): remote SHA matched local HEAD, ahead/behind 0/0. 8 completed phase prompts since the prior Gate B (fc34d7e): FREQ.6D.14, .15, .16, .17, .18, .19, .20, .21.

Prior phase: `FREQ.6D.23` — Execution Status `DONE`, Final Classification `INTERMEDIATE_6D_AUTHORITY_APPROVED_7D_PRODUCT_NON_SUPPORT_APPROVED`. Resolved Intermediate×6D and ×7D together. Found the real architecture (`CatalogRunLayoutSlots`, `FourDaySessionDistanceAllocationPolicy`, `CatalogPeakVolumeBandLoader`) is already fully generic/data-driven — the only real architecture gaps disclosed were two `DaysPerWeek == 5 ? 3 : 2` ternaries and the Adaptation policy's 5-session-only dispatch. Confirmed the canonical 2-day KEY↔KEY/KEY↔LONG calendar-spacing rule already exists and is satisfiable for 6D but genuinely conflicts at the week-boundary seam for a zero-rest-day 7D cadence. Grounded 6D's numeric authority in a provenance-matched reuse of `FREQ.6C`'s real 5D anchors (same undifferentiated Hal Higdon source spans both 5 and 6 days) rather than inventing new numbers; classified 7D `PRODUCT_NON_SUPPORT` on real injury-risk evidence plus the calendar-seam conflict, not forced into approval. Designed and froze a generalized N-session Adaptation state-table model (count-floor + role-gate, extending the existing 4D/5D dispatch shape) for both 6 and 7 sessions, proved monotonic and role-sensitive. Produced the required hardcode audits, catalog-capacity matrix, numeric-authority table, and structure table. Intermediate×6D meets the full implementation-readiness standard; 7D's structure is frozen for reference but not implemented. No production code, migration, public activation, or catalog authoring. Preserved Intermediate×5D's COMPLETE/PUBLIC status untouched.

Prior phase: `FREQ.6D.24` — Execution Status `DONE`, Final Classification `INTERMEDIATE_6D_IMPLEMENTATION_BLOCKED_ON_PEAK_VOLUME_BAND_AUTHORITY`. Attempted final closure of Intermediate×6D's `PeakVolumeBand`. Reconstructed the real `PEAK_VOLUME_BANDS_V1` v1→v4 history and proved its own governance convention (rows added/removed only for genuinely-implemented cells — Advanced/Experienced/unused-Beginner rows were fully stripped in v3). Proved by direct code tracing (`CatalogVolumeAndLongRunPlanner.ResolvePeak`, `TenKPreparationRunwayComponentAdapters`, `LongHorizonFullNumericOrchestrator`) that the band is genuinely load-bearing at runtime, refuting the "metadata-only" candidate. Found new historical evidence that Intermediate's band was deliberately hand-recalibrated away from a constant-per-day formula (still visible, abandoned, in Advanced/Experienced's v1 rows) — disqualifying `FREQ.6D.23`'s own `[40,56]` trend-extrapolation candidate and any new frequency-formula candidate. Evaluated and rejected 5D-band reuse (contradicts the artifact's own per-frequency differentiation) and a tier-ambiguous external 6-day program's figures (insufficient precision/tier match). Validated no seriously-considered candidate introduces a Core/Runway/LongHorizon representability conflict, since the already-proven-safe 5D reachable-volume matrix carries over unchanged. No candidate met the decision standard, so `PeakVolumeBand` remains honestly `DECISION_REQUIRED` — not forced into an approved figure. Every other FREQ.6D.23 authority (structure, starting-volume, `ResolvedPeakReference`=44.5, progression, long-run share, Adaptation state tables) is explicitly unchanged.

## Intermediate×6D: authority status

**Blocked on exactly one item.** Structure, support decision, starting-volume, `ResolvedPeakReference`, progression, long-run share, Adaptation N-session state tables, and catalog capacity are all frozen/approved (`FREQ.6D.23`). `PeakVolumeBand`'s exact figure remains `DECISION_REQUIRED` (`FREQ.6D.24`) — confirmed genuinely load-bearing at runtime, not a publication-only gap. Intermediate×6D combined implementation cannot safely begin until this single item closes.

Prior phase: `FREQ.6D.25` — Execution Status `DONE`, Final Classification `INTERMEDIATE_6D_PEAK_VOLUME_BAND_AUTHORITY_APPROVED` / `INTERMEDIATE_6D_FULL_IMPLEMENTATION_AUTHORITY_COMPLETE`. Closed the last open Intermediate×6D item with materially stronger evidence than `FREQ.6D.24` had: found a real, structurally-exact (2K+3E+1L) but tier-mismatched ("Advanced") external 6-day 10K plan, and used its peak ceiling (59.55km) cross-checked against Appsel's own real historical Advanced×5D band ceiling (58km) to show the 5D→6D frequency transition is essentially flat (~1.5km delta, not a formula-shaped jump) when the extra day is easy-only — exactly matching Appsel's own frozen structural principle. Approved `PeakVolumeBand=[36,50]km` for Intermediate×6D as an explicit `PRODUCT_DEFAULT_WITH_TIER_MATCHED_EVIDENCE_ENVELOPE` (numerically equal to 5D's band, but for a new, disclosed reason — real cross-tier flatness evidence — not the level-invariance argument `FREQ.6D.24` rejected). Confirmed 44.5 containment, full Core/Runway/LongHorizon representability, GE-cap/band separation, and existing whole-km rounding convention. No production code, catalog edit, migration, or 7D work.

## Intermediate×6D: authority status

**COMPLETE.** Structure, support decision, starting-volume, `ResolvedPeakReference`, progression, long-run share, Adaptation N-session state tables, catalog capacity, and `PeakVolumeBand` (`[36,50]km`) are all frozen/approved. **No remaining numeric/product/catalog blocker.** Not yet implemented — Core/Runway/LongHorizon dark verification has not been built.

**Next phase**: `FREQ.6D.26` — **INTERMEDIATE×6D CORE + PREPARATION RUNWAY + LONGHORIZON COMBINED IMPLEMENTATION & DARK VERIFICATION**. Phase type: **IMPLEMENTATION + CATALOG AUTHORING + REAL POSTGRESQL VERIFICATION + DARK CAPABILITY CLOSURE**. Implements existing approved authority only — no new product/numeric/schema decision.

Prior phase: `FREQ.6D.26` — Execution Status `DONE`, Final Classification `INTERMEDIATE_6D_CORE_RUNWAY_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED` / `INTERMEDIATE_6D_FULL_HORIZON_DARK_CAPABILITY_COMPLETE`. Implemented Intermediate×6D Core/Runway/LongHorizon as one compositional capability, publishing plan-catalog release `1.2.0` (byte-identical superset of `1.1.0`) carrying `RUN_LAYOUT_6D`, `TEN_K_MASTER` v8, `PEAK_VOLUME_BANDS_V1` v5 (`[36,50]` row), and the `TEN_K__6D__INTERMEDIATE` combination — no new prescription profiles authored, since the shared dual-KEY execution profiles are genuinely Level+Distance-owned. Added `VolumeSafetyPolicy.SixDayIntermediate` and the 6-session Adaptation state table, both implementing `FREQ.6D.23`/`FREQ.6D.25`'s frozen authority verbatim. Found and fixed 8 real production hardcodes blocking 6D (three `easySupportCount` ternaries, two internal eligibility gates, an EASY-count allow-list, an internal candidate-identity check, a calendar `DaysPerWeek` range guard, and a genuine structural limitation in the session-distance-allocation record that would have silently mis-assigned 6D Core's 3rd EASY session's distance) — the public routing gates were deliberately left untouched. Verified via 22 new tests including one full organic GE→Runway→Core dual-KEY lifecycle through real PostgreSQL. Full regression 3928/3932 (same 2 pre-existing failures + 2 genuinely-fixed catalog-inventory-count assertions), 1510/1510 PlanCatalog, Debug+Release clean. Test coverage real but not the full requested manifest — disclosed honestly. Not publicly activated.

## Intermediate×6D: implementation status

**COMPLETE AND PUBLIC.** Core (8–14), Preparation Runway (15–20), and LongHorizon (21–52) are all implemented, dark-verified, and now `PUBLICLY_ACTIVE` (`FREQ.6D.27`). Numeric/product/catalog authority fully closed since `FREQ.6D.25`.

Prior phase: `FREQ.6D.27` — Execution Status `DONE`, Final Classification `INTERMEDIATE_6D_CORE_RUNWAY_LONGHORIZON_IMPLEMENTED_AND_PUBLICLY_ACTIVATED` / `INTERMEDIATE_6D_FULL_HORIZON_CAPABILITY_COMPLETE`. Opened the real public routing gate via 4 minimal allow-list widenings implementing only already-approved authority. Added 20 new tests proving the full 8-52 week matrix (45/45) through real public HTTP + PostgreSQL. Opening `(Intermediate, 6)` made 5 pre-existing "unsupported neighbor" test rows obsolete; each corrected in place. A full-suite regression discrepancy (3 vs. 2 known failures) was reconciled per explicit user instruction before any closure: the 3rd failure was traced by direct code reading to the same obsolete-assertion class (`Phase4F8_2LivePilotRoutingTests`'s "non-pilot" `DaysPerWeek=6` probe stopped being non-pilot), fixed by advancing the probe to 7, and the full suite re-verified authoritative via TRX at 3948 total / 2 failures (exact durable pre-existing baseline, zero new regressions). This closes the Intermediate×6D arc and the entire Intermediate 10K frequency axis — see the updated support matrix above. `INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE`.

**Next phase**: not selected by `FREQ.6D.27` (per its own §70). `MASTER_ROADMAP.md`'s own Wave A remaining-work list (§section below) names two open candidates — completing the Advanced level across proven frequencies, and completing Beginner's remaining frequencies. `NEXT_PHASE_NOT_YET_SCHEDULED`.

---

## 3. Wave Sequence

```
WAVE A — 10K completion
        ↓
WAVE B — Half Marathon completion
        ↓
WAVE C — Marathon completion
        ↓
WAVE D — Cross-distance backend closure / release readiness
```

**Rule**: do NOT open Half Marathon implementation while 10K closure remains architecturally incomplete. Do NOT open Marathon while Half Marathon distance-generalization remains incomplete. Exceptions require an explicit roadmap/governance decision (recorded in `PHASE_LEDGER.md` as a `GOVERNANCE` phase).

---

## 4. Current Wave

**WAVE A — 10K completion.** Intermediate×5D Core (8-14 weeks) is `PUBLICLY_ACTIVE` (FREQ.6D.4D.5G, §2). Preparation Runway (15-20w) is `PUBLICLY_ACTIVE` for all readiness states including missing/explicit-zero, real HTTP/DB verified (FREQ.6D.8/FREQ.6D.10, §2). Long-Horizon (21-52w) 5D activation remains an open gap, but is now fully implementation-ready — architecture (FREQ.6D.11) and GE-segment product/numeric policy (FREQ.6D.12) are both approved, with no product or numeric decision remaining; only the implementation wave itself is left. No Half Marathon or Marathon work may begin under this roadmap's own rule until 10K's full architectural closure (§25/Wave A milestones, including the Long-Horizon 5D gap) is reached.

---

## 5. Next Concrete Block

Populated from real repository state (§2), not assumption:

1. `FREQ.6D.3D` — RunningApp execution consumer (parent: `FREQ.6D.3C`, `VERIFIED`).
2. `FREQ.6D.4` — dual-KEY progression/runtime integration, 5D severity-table widening (per FREQ.6D.1B's confirmed-required scope), persistence lineage (parent: `FREQ.6D.3D` once it exists; architecturally previewed by FREQ.6D.CP1/6D.1A/6D.1B).
3. `FREQ.6D.5` — persistence/round-trip/full-regression closure (parent: `FREQ.6D.4`).
4. `FREQ.7` — first real Intermediate×5D candidate (parent: `FREQ.6D.5`).
5. `FREQ.8` — 5D activation decision (parent: `FREQ.7`).

None of these have a report yet — they are **not** Phase IDs until created; listed here only as the concrete next block, per §13's rule against pre-authoring fake IDs beyond the near-term horizon.

---

## 6. Future Capability Milestones

See §25 for the full milestone list (no speculative Phase IDs assigned).

---

## 7. Phase Type Taxonomy

| Type | Code | Purpose | Required |
|---|---|---|---|
| **A. EVIDENCE** | NO | Derive evidence envelope; no product selection | Sources/evidence attribution, uncertainty disclosed, no invented defaults |
| **B. DECISION** | NO | Select/freeze domain/product authority from an existing evidence envelope | Decision inventory, internal arithmetic/consistency, exact final classification |
| **C. ARCHITECTURE_DESIGN** | NO | Define contracts/ownership/data flow | Exact type/contract shape, DO NOT list, dependency boundaries, no product decisions hidden as engineering |
| **D. DESIGN_VERIFICATION** | NO | Challenge a design against its own frozen authorities and real code | Fidelity checks, real consumers/data-flow, open-decision audit |
| **E. IMPLEMENTATION** | YES | Implement one frozen contract/policy | Tight allowed scope, explicit DO NOT TOUCH list, tests, regression, file attribution, atomic commit |
| **F. VERIFICATION_CLOSURE** | NO production behavior change by default | Prove claimed implementation actually works | Real tests, failure-path evidence, regression, no invented evidence |
| **G. CHECKPOINT** | NO product/domain implementation | Consolidation/durability/governance | No new decision, repository attribution, commit state |

`GOVERNANCE` is retained as an operational subtype for repo/ledger gates (e.g. this roadmap's own bootstrap phase).

---

## 8. Prompt Construction Standard

Every future phase prompt must begin by declaring:

```
PHASE ID
PHASE TYPE
OBJECTIVE
AUTHORITATIVE PARENTS
ALLOWED SCOPE
FORBIDDEN SCOPE
```

Then include: repository baseline check · authority invariants · exact required work · stop conditions · tests/evidence standard · file attribution · documentation requirement · ledger update · commit boundary · final classifications. Exact content differs by phase type; the skeleton is mandatory.

---

## 9. Batching Rules

**Allowed** when: the structural architecture has already been proven; cells differ only along an already-modeled authority axis; evidence questions are materially the same; one matrix output can preserve per-cell distinctions.

Examples likely allowed later: Advanced 3D/5D/6D/7D evidence matrix after the Advanced anchor + frequency architecture are proven; Intermediate 6D/7D reuse/numeric matrix after the 5D dual-KEY architecture is proven.

**Forbidden** for: first new structural pattern; first second-KEY architecture; first new Distance; first new Level authority; first new persistence/boundary architecture; cells with materially different domain questions.

**Principle**: `FIRST STRUCTURAL INSTANCE = NARROW`. `REPEATED PROVEN PATTERN = MAY BATCH`.

---

## 10. Commit / Push Gates

**Commit hygiene**: every phase must end with an attributable atomic local commit, or an explicitly documented reason it is documentation-only/no-change. Implementation and documentation commits may be separate when useful; `PHASE_LEDGER.md` records both.

**Hard push gates** (mandatory remote-durability checkpoints):

- **A.** At the start of every new Wave.
- **B.** After every block of approximately 10 completed phase prompts.
- **C.** Before starting another Distance.
- **D.** After any major architecture checkpoint where losing local history would cause substantial recovery cost.

A push gate verifies: working-tree attribution · local commit graph · remote/upstream · ahead/behind · no unknown commits · push dry run · actual push · remote SHA == expected local gate SHA. **The next block cannot start until the gate PASSes.**

---

## 11. Parent Validation Rules

Before ANY future phase begins:

1. Read `PHASE_LEDGER.md`.
2. Verify the proposed parent Phase ID exists there.
3. Verify the report link exists.
4. Verify parent provenance is `VERIFIED`.
5. Verify the required parent commit is reachable from current HEAD.
6. Verify no duplicate Phase ID exists.
7. Determine phase type.
8. Read only the relevant authoritative reports.
9. Check the working tree.
10. Only then begin.

If a proposed parent exists only in `MASTER_ROADMAP.md` but not the ledger: **STOP**. Classification: `PARENT_PHASE_NOT_REPOSITORY_VERIFIED`.

---

## 12. Support-State Vocabulary

- **`UNSUPPORTED` / `PROVEN_NON_SUPPORT`** — identity/cell is not supported under the approved policy.
- **`PRODUCT_INELIGIBLE`** — identity is supported but this specific request does not qualify.
- **`GATED`** — internally supported/resolvable but public routing is closed.
- **`PUBLICLY_ACTIVE`** — normal public routing can reach it.

A `DONE` phase does not necessarily mean a `PUBLICLY_ACTIVE` cell (e.g. `FREQ.6D.3C` is `DONE`; Intermediate×5D remains not publicly active).

---

## 13. Roadmap Update Rules

At the end of every phase: `PHASE_LEDGER.md` appends the actual phase result; `MASTER_ROADMAP.md` updates only the planning/status fields affected. Never rewrite historical ledger truth merely to make the roadmap cleaner. If a phase discovers a blocker, the roadmap sequence changes — do not force a previously predicted next prompt merely because it was written earlier.

MASTER_ROADMAP must NOT pre-author speculative phase IDs beyond the near-term block (§5). Future work beyond that is represented as capability milestones (§6/§25), never as invented IDs like "GEN.17B.4" before a real prompt/report exists for it.

---

## 14. Near-term roadmap block (populated from repository audit, `APPSEL-BACKEND.GOV.0`)

Repository evidence (see `PHASE_LEDGER.md` rows 59-72) confirms the chain through `FREQ.6D.4D`'s dual-KEY production-integration architecture approval. The real near-term sequence is now:

```
FREQ.6D.4C.2 (DONE)             → IMPLEMENTATION: narrowed WorkoutPrescriptionProfileValidator's
                                    intensity-mode check (M4); added the new capability-overlay
                                    artifact + GOAL_PACE_TEN_K v2 entry (M3); completed
                                    GOAL_PACE_TEN_K v3's DRAFT content. All 8 approved slots now
                                    proven representable and lossless-projecting.
FREQ.6D.4B.1 (DONE)             → EVIDENCE: all full-component fields inventoried; warm-up/cooldown
                                    envelopes established; FARTLEK structural RECOVERY conflicts
                                    with nested-recovery ownership in the current model.
FREQ.6D.4B.2 (DONE)             → ARCHITECTURE: R1 selected; nested MAIN_SET recovery is sole owner;
                                    BLD-S v4→v5 product-reference amendment required.
FREQ.6D.4B.3 (DONE)             → PRODUCT DECISION: WU=600s EASY, CD=300s EASY; BLD-S→v5;
                                    FC1-FC10 complete, no athlete-facing implementation choice.
Gate B (PASS: 0bc70c5)          → remote SHA matched local gate SHA; ahead/behind 0/0.
FREQ.6D.4B.4 (DONE)             → IMPLEMENTATION: corrected DRAFT skeletons and lifecycle-aware
                                    validation; BLD-S now targets v5; all-eight/no-double-count
                                    tests pass; immutable FARTLEK v4 preserved.
FREQ.6D.4C.3 (DONE)             → IMPLEMENTATION: authored all 8 real production
                                    WorkoutPrescriptionProfile documents using the corrected exact
                                    references and frozen full-component policy; 8/8 catalog
                                    capacity READY; zero infrastructure delta; legacy bundles
                                    architecturally unaffected.
FREQ.6D.4C.4 (DONE)             → ARCHITECTURE: root-caused the legacy regression exactly (only
                                    the frozen historical combinations v1-v3 + golden/cascade tests
                                    are exposed; the real, live v4 combination already resolves via
                                    exact refs). Selected exact-reference/manifest activation
                                    authority (already realized) + a narrow, additive legacy-
                                    resolver-eligibility flag as the permanent containment
                                    instrument. No CatalogStatus change; no legacy-pin migration.
FREQ.6D.4C.5 (DONE)             → IMPLEMENTATION: added the narrow, nullable, hash-stable
                                    EligibleForLegacyDefaultResolution flag; extended
                                    FindWorkout(key, ledger)'s filter; promoted all four DRAFT
                                    versions to VALIDATED with the flag false in the same atomic
                                    commit. Live v4 combination, historical v1-v3 replay, and all 8
                                    profiles proven unchanged; golden/cascade regressions green.
                                    CATALOG_LIFECYCLE_BLOCKER now CLOSED.
FREQ.6D.4D (DONE)               → ARCHITECTURE: re-verified FREQ.6D.1A/1B's proposed Lane/Stage/
                                    Adaptation design against current code (never implemented, every
                                    gap still real); selected Option D1 (catalog-authored LaneOrdinal
                                    + bind-time structural ordinal, per-lane independent allocator
                                    invocation, exact profile refs at catalog/binder boundary); full
                                    authority map/dataflow/failure-semantics/A-E implementation split
                                    produced. Zero remaining product decision; zero legacy delta.
FREQ.6D.4D.1 (DONE)             → IMPLEMENTATION (Split A): catalog-authored LaneOrdinal + bind-time
                                    structural ordinal (from SlotOrderInWeek); (WeekNumber, LaneOrdinal)-
                                    keyed stage schedule replacing the defective WeekNumber-only
                                    dictionary; per-lane independent ProgressionStageAllocator
                                    invocation (math unchanged); BoundCatalogSession.LaneOrdinal now
                                    sole ordinal authority. 21 new tests; 2,898/2,899 RuntimeCatalog,
                                    1,485/1,485 PlanCatalog. Legacy 3D/4D/Beginner×4D unchanged.
FREQ.6D.4D.2 (DONE)              → IMPLEMENTATION (Split B): additive PrescriptionProfileCandidates
                                    stage-authoring field (PlanCatalog + RunningApp mirror); exact
                                    cardinality-only profile resolution in CatalogWorkoutBinder
                                    (fail-closed on ambiguity); PrescriptionProfileLaneDoseValidator
                                    reused verbatim; new PrescriptionProfileClosureResolver /
                                    PrescriptionProjectionDependencyResolver glue feeding the
                                    unmodified FREQ.6D.3C CatalogBundleAssembler exact-dependency
                                    overload — dual-lane bundle ExecutionPrescriptions proven
                                    non-null/deterministic. 21 new tests; 1,501/1,501 PlanCatalog.
                                    RunningApp DB-backed subset could not execute (Docker/Postgres
                                    unavailable this session — environment limitation, not a
                                    regression; all 197 non-executing failures independently
                                    Npgsql-confirmed). Real RUN_LAYOUT_5D authoring remains Split E.
FREQ.6D.4D.3 (DONE)              → IMPLEMENTATION (Split C): CatalogSessionPrescriptionSource /
                                    ExecutionPrescriptionIndex (FREQ.6D.3D, previously dormant)
                                    wired into CatalogSessionPrescriptionPlanner's live path;
                                    exact per-session Legacy/ProfileBacked classification off
                                    BoundCatalogSession profile lineage; fail-closed on partial
                                    lineage/missing index/missing exact profile/wrong version/
                                    workout-provenance mismatch; never falls back to Legacy. 16
                                    new tests via the real end-to-end planner. 500/500 scoped
                                    in-memory regression; 1,962/1,972 broader (10 environmental
                                    Npgsql failures, Docker unavailable). Legacy 3D/4D unchanged.
FREQ.6D.4D.4 (DONE)              → IMPLEMENTATION (Split D): 2 new nullable TrainingDay columns
                                    (CatalogPrescriptionProfileKey/Version) via a real, applied EF
                                    migration; both live confirmation mappers wired to thread exact
                                    profile lineage through; LaneOrdinal/execution-content
                                    deliberately not persisted (derivable/bundle-only, per
                                    architecture); repair/substitution subsystem audited, already
                                    correct; complete real FREQ.6 24-row 5-session Adaptation
                                    severity table implemented in NextWindowLoadDecisionPolicy,
                                    legacy 4-session behavior unchanged. 44 + 5 new tests, all
                                    DB-backed proof real (Docker/Postgres restored this phase).
                                    2,967/2,969 RuntimeCatalog (2 pre-existing unrelated), 1,501/1,501
                                    PlanCatalog, 192/192 LongHorizon.Adaptation.
FREQ.6D.4D.5 (DONE, PARTIAL)    → IMPLEMENTATION (Split E, partial): real RUN_LAYOUT_5D/
                                    TEN_K__5D__INTERMEDIATE combination/dual-lane progression/8
                                    profiles/published 1.1.0 release with ExecutionPrescriptions;
                                    RunningApp published-bundle runtime discovery wired end to end
                                    (IPublishedTemplateBundleLoader, DI, config, packaging) — the
                                    Split-C gap now closed. Public activation attempted for 8-14w
                                    Core only, reverted: CatalogWeekSkeletonCalendarMaterializer
                                    hardcodes one KEY_SESSION slot/week, a real algorithm gap plus
                                    an undecided product question (min. inter-KEY-session
                                    separation) confirmed by a real E2E 500, not static analysis.
                                    TEN_K__5D__INTERMEDIATE remains fully dark to public traffic.
                                    3,612/3,613 RuntimeCatalog (1 pre-existing unrelated), 1,510/1,510
                                    PlanCatalog.
Gate B (PASS: 13594ac)          → remote SHA matched local gate SHA; ahead/behind 0/0. ~10
                                    completed phase prompts since the prior Gate B (0bc70c5):
                                    FREQ.6D.4B.4 through FREQ.6D.4D.5.
FREQ.6D.4D.5A (DONE)            → EVIDENCE + PRODUCT_DECISION: reconstructed prior FREQ.3/FREQ.4/
                                    FREQ.4A authority (predating the FREQ.6D branch); found
                                    DatedGeneratedCatalogPlanSkeletonValidator already generalized
                                    to N>=1 KEY with an embedded, disclosed, not-yet-evidenced
                                    MinimumKeySessionToKeySessionSeparationDays placeholder;
                                    clarified CatalogWeekSkeletonCalendarMaterializer (the real
                                    blocker) is a distinct, never-generalized upstream component.
                                    Fresh external evidence (48-72h recovery convention; 5 real
                                    fetched intermediate 10K/5-day plans, none consecutive-day)
                                    converged with the placeholder; a real combinatorial
                                    counterexample rejected the stricter >=3 alternative. Approved
                                    MinimumKeySessionToKeySessionSeparationDays = 2 (calendar-date
                                    difference), phase-invariant, symmetric, reusing an existing
                                    tie-break and exception type. No code touched. Full multi-slot
                                    implementation contract + test manifest produced for the next
                                    phase.
FREQ.6D.4D.5B (DONE, PARTIAL)   → IMPLEMENTATION: generalized CatalogWeekSkeletonCalendarMaterializer
                                    to multi-KEY_SESSION weeks (keyCount>=1), enforcing the frozen
                                    FREQ.6D.4D.5A KEY<->KEY rule via a generalized backtracking
                                    search degenerating exactly to the pre-existing algorithm for
                                    keyCount==1; single numeric authority (removed the
                                    materializer's own duplicate constant). Proven against the real
                                    TEN_K__5D__INTERMEDIATE candidate for 8/10/12/14 weeks, incl.
                                    Taper, determinism, lane-identity preservation; zero legacy
                                    delta (3,639/3,640, 1,510/1,510 PlanCatalog). Public activation
                                    retried and reverted again: CatalogPrescriptionContextValidator
                                    hardcodes a TAPER_SHARPEN stage-key check incompatible with the
                                    real dual-lane Taper naming -- a second, independent,
                                    calendar-unrelated blocker, not worked around.
FREQ.6D.4D.5C (DONE)            → EVIDENCE + ARCHITECTURE_DECISION: traced TAPER_SHARPEN to its
                                    real origin (PHASE4F_7D's V1_TAPER_SHARPEN_PRESCRIPTION_POLICY,
                                    a pilot-specific legacy runtime-injection content policy, never
                                    canonical vocabulary). Found ProfileBacked Taper completeness
                                    already proven downstream by Split-C's per-session execution-
                                    resolution guarantee (covers both real 5D lanes, stronger than
                                    "at least one"). Rejected weaker/wrong-axis models. Selected:
                                    partition the existing check along the Legacy/ProfileBacked axis
                                    (thread BoundCatalogSession.PrescriptionProfileKey one struct
                                    further -- additive, not new metadata); every Legacy Taper KEY
                                    instance still must match TAPER_SHARPEN/EASY_STANDARD exactly as
                                    today (zero 3D/4D/Beginner4D delta); ProfileBacked instances
                                    exempted, covered downstream. Real invalid-5D counterexample
                                    constructed proving no collapse into blanket acceptance. No code
                                    touched. Full implementation contract + 22-item test manifest
                                    produced for the next phase.
FREQ.6D.4D.5D (DONE, PARTIAL)   → IMPLEMENTATION + INTEGRATED VERIFICATION: implemented the
                                    approved FREQ.6D.4D.5C Legacy/ProfileBacked Taper-completeness
                                    partition in CatalogPrescriptionContextValidator; found and fixed
                                    a second occurrence of the identical root cause in
                                    CatalogFinalPrescribedPlanValidator (via the already-existing
                                    CatalogPrescribedSession.PrescriptionSource classification). Both
                                    real 5D Taper lanes proven valid without stage-name special-
                                    casing; malformed-Legacy counterexample still rejected; zero
                                    legacy delta (3,649/3,650, 1,510/1,510 PlanCatalog). Public
                                    activation retried a third time -- progressed past calendar and
                                    both Taper checks for the first time -- then found a third,
                                    independent, out-of-scope blocker: V1CatalogPublicWorkoutTypeMappingPolicy
                                    has no public workout-type mapping for the real 5D
                                    AEROBIC_STRENGTH_CONTROLLED_INTRO workout. Reverted a third time.
FREQ.6D.4D.5E (DONE)            → EVIDENCE + PRODUCT_DECISION: derived the complete real
                                    Intermediate x5D workout closure fresh from the catalog (8
                                    stage/lane combinations, 6 distinct reachable pairs), cross-
                                    referenced exhaustively against V1CatalogPublicWorkoutTypeMappingPolicy
                                    -- exactly one gap found (AEROBIC_STRENGTH_CONTROLLED_INTRO), no
                                    others. Traced the policy's real intent (coarse athlete-facing
                                    display taxonomy) and the workout's real semantics from FREQ.6D.4B
                                    product authority, without name-based inference. Approved mapping
                                    to the existing Interval type on strong structural/family
                                    precedent (same shape/family as the already-Interval-mapped
                                    FARTLEK); no taxonomy extension needed; key-only mapping ownership
                                    confirmed correct; zero UI/API/analytics impact. One-arm
                                    implementation contract + 18-item test manifest produced. No code
                                    touched.
FREQ.6D.4D.5F (DONE, PARTIAL)   → IMPLEMENTATION + INTEGRATED VERIFICATION: implemented the
                                    approved FREQ.6D.4D.5E mapping (AEROBIC_STRENGTH_CONTROLLED_INTRO
                                    -> Interval, one key-only arm, zero taxonomy change) plus a real
                                    catalog-file-driven exhaustive completeness gate reproducing the
                                    real 8-combination/6-pair closure and proving every pair maps
                                    exactly once, deterministically. Retried public activation a
                                    fourth time via real HTTP E2E testing (real committed catalog +
                                    real published 1.1.0 bundle): 12-week 5D preview genuinely
                                    succeeds (furthest point yet); 8/10/14-week previews 500 --
                                    root-caused to a fourth, independent blocker:
                                    CatalogPreviewGenerator's CompressedCore/ExtendedCore dynamic-
                                    orchestration branch (every horizon except exactly 12 weeks) never
                                    threads the published-bundle execution index through, unlike the
                                    exact-12-week pipeline. Reverted the widening a fourth time;
                                    retained the independently-correct mapping fix. Zero legacy delta
                                    (3,671/3,672, 1,510/1,510 PlanCatalog).
FREQ.6D.4D.5G (DONE)            → IMPLEMENTATION + INTEGRATED VERIFICATION: root-caused the
                                    FREQ.6D.4D.5F blocker exactly -- DynamicCoreSessionPrescriptionOrchestrator
                                    constructed CatalogSessionPrescriptionRequest with ExecutionIndex
                                    omitted (defaulted null), the single narrow wiring gap affecting
                                    every CompressedCore/ExtendedCore horizon. Fixed by threading the
                                    same published-bundle ExecutionPrescriptionIndex through both
                                    dynamic-orchestration context types, computed once per request via
                                    a new shared CatalogPreviewGenerator.LoadExecutionIndex helper -- no
                                    new prescription logic, no horizon-number special-casing,
                                    ExecutionPrescriptionIndex.ResolveExact remains the sole ProfileBacked
                                    consumer authority, Legacy sessions unaffected. Proved dark first (18
                                    new tests: real 8/10/14-week CompressedCore/ExtendedCore generation,
                                    every ProfileBacked session resolves exact execution, omitted-context
                                    still fails closed, Taper/calendar/determinism zero-delta) before
                                    retrying public activation a fifth time. Real HTTP E2E: 8/10/12/14-
                                    week previews all succeed; confirmed 8-week (CompressedCore),
                                    14-week (ExtendedCore), 12-week (PreferredCore reference) plans
                                    against real PostgreSQL with correct persistence; unsupported
                                    neighbors remain closed; zero legacy regression (3,704/3,705,
                                    1,510/1,510 PlanCatalog). TEN_K__5D__INTERMEDIATE genuinely
                                    publicly active. Parent FREQ.6D.4D closed as
                                    FREQ6D4D_DUAL_KEY_PRODUCTION_INTEGRATION_IMPLEMENTED_AND_VERIFIED.
FREQ.6D.5 (DONE)                → ARCHITECTURE + READINESS AUDIT: full pipeline trace of Preparation
                                    Runway (15-20w) and LongHorizon (21-52w) against proven Core 5D. No
                                    code touched. Runway hardcoded to Intermediate x4D at three
                                    independent layers (routing gate, unconditional candidate load,
                                    orchestrator ValidateRequest) plus a fixed single-KEY 4-slot layout
                                    with no product authority for 5D's Runway weekly structure
                                    (PRODUCT_DECISION_REQUIRED); catalog content itself is candidate-
                                    agnostic and most numeric coefficients already generic. LongHorizon
                                    hardcoded even more pervasively (~10 independent DaysPerWeek==4
                                    gates); genuine architecture gap -- persisted session schema has no
                                    LaneOrdinal/ProgressionStageKey/PrescriptionProfileKey columns,
                                    real JIT Core-week composition discards dual-KEY lineage by
                                    grouping on raw structural-role string (DB-migration-carrying gap).
                                    5-session Adaptation policy already frequency-generic but
                                    unreachable for LongHorizon. Zero ExecutionPrescriptionIndex
                                    references anywhere in LongHorizon; both Runway's and LongHorizon's
                                    Core-entry share the same orchestrator rather than
                                    CatalogPreviewGenerator directly. Classification:
                                    INTERMEDIATE_5D_RUNWAY_LONGHORIZON_SEPARATE_WAVES_REQUIRED.
FREQ.6D.6 (DONE)                → EVIDENCE + PRODUCT_DECISION: approved both open Preparation Runway
                                    product authorities. Weekly structure: 1 KEY_SESSION + 3
                                    EASY_SUPPORT + 1 LONG_RUN every Runway week (session count
                                    invariant at 5, never ramps -- grounded in RunLayout's fixed-
                                    cardinality architecture, Core's own phase-invariant frequency, and
                                    external base-phase coaching evidence), generalizing the existing
                                    4D block-role override table by one additional EASY slot; second
                                    KEY introduced only at real Core Week 1. Starting-volume: direct
                                    repository-truth discovery that CatalogVolumeAndLongRunPlanner.Build
                                    only special-cases Intermediate x3D and Beginner x4D --
                                    Intermediate x5D already falls through to the same
                                    V1MissingReadinessStartingVolumePolicy (16km missing / 12km
                                    explicit-zero) Runway itself uses, confirming rather than borrowing
                                    the already-live 5D Core numeric authority. This also proves
                                    Runway's own Week-1 volume and its Core-entry target resolve
                                    through the identical policy, making feasibility structurally
                                    assured across all 15-20 week horizons and every readiness state --
                                    zero blocked cells in the representability matrix. No production
                                    code, catalog, or LongHorizon work touched. Classification:
                                    INTERMEDIATE_5D_RUNWAY_PRODUCT_POLICY_APPROVED.
FREQ.6D.7 (DONE)                → CHECKPOINT GATE + IMPLEMENTATION + INTEGRATED VERIFICATION:
                                    generalized the Intermediate x4D Preparation Runway implementation
                                    to also support Intermediate x5D (1 KEY + 3 EASY + 1 LONG every
                                    Runway week, second KEY only at real Core Week 1), reusing the
                                    existing Runway engine and existing generic Core pipeline -- no
                                    second implementation, RUN_LAYOUT_5D untouched. Generalized
                                    FourDaySessionDistanceAllocationPolicy's EASY_SUPPORT the same way
                                    KEY_SESSION was already generalized (FREQ.4); PreparationRunway-
                                    CoreWeekOneTargetAdapter now reads the real KEY count from Core's
                                    own Week 1; Runway/Core boundary continuity (numeric, calendar,
                                    pace) now applies per-slot KEY/EASY comparison only when role
                                    composition actually matches, per FREQ.6D.6's "KEY count is not a
                                    Core-entry compatibility dimension." Found and fixed two more
                                    hardcodings only reachable once 5D's real dual-KEY Core Week 1
                                    was exercised through this path: a KEY-session pace-target ordinal
                                    collision and two hardcoded DaysPerWeek=4 calendar-skeleton
                                    literals; threaded a real ExecutionPrescriptionIndex into Runway's
                                    own Core-generation call site (the missing-context class
                                    FREQ.6D.4D.5G fixed elsewhere, at a call site that fix didn't
                                    reach). Proved dark end-to-end: all six 15-20 week horizons x
                                    READY/NOT_READY produce the exact approved shape, real Core Week 1
                                    is exactly 2K+2E+1L, Intermediate x4D remains byte-for-byte
                                    unchanged (1767/1767 full regression). Public routing is code-
                                    wired to the exact approved combination; real HTTP E2E and real
                                    PostgreSQL confirmation were not performed this session --
                                    disclosed explicitly. Classification:
                                    INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED.
FREQ.6D.8 (DONE)                → VERIFICATION + PUBLIC ACTIVATION: closed FREQ.6D.7's own disclosed
                                    gap with real HTTP E2E (real Api host, real Postgres, no mocks) for
                                    all six 15-20 week Intermediate x5D Preparation Runway public
                                    previews -- every one 200 with exact TEN_K__5D__INTERMEDIATE
                                    identity, exact 1 KEY+3 EASY+1 LONG Runway shape, exact 2 KEY+2
                                    EASY+1 LONG Core Week 1. Real confirmation/persistence verified for
                                    15/17/20-week horizons: exact role cardinality, exact last-Runway/
                                    first-Core transition re-asserted after a fresh DB reload (permanent
                                    regression), real profile-key/version lineage on Core KEY sessions.
                                    Home/calendar/training-day-detail succeed; unsupported neighbors and
                                    21+/24-week LongHorizon remain closed; Intermediate x4D Runway and
                                    Intermediate x5D Core-only remain byte-for-byte unchanged. Found and
                                    disclosed (not fixed, per explicit scope) a real, pre-existing
                                    Core-side defect: missing/explicit-zero starting-volume evidence
                                    fails real Core generation for Intermediate x5D at the real 2-KEY
                                    minimum -- independently proven pre-existing by reproducing
                                    identically against the already-active Core-only route and against
                                    the pre-FREQ.6D.7 durable baseline commit. No routing change needed
                                    (already wired); no production code touched. Full regression
                                    3744/3746 (2 pre-existing failures, both re-verified against the
                                    durable baseline), 1510/1510 PlanCatalog. Classification:
                                    INTERMEDIATE_5D_PREPARATION_RUNWAY_IMPLEMENTED_AND_PUBLICLY_ACTIVATED.
FREQ.6D.9 (DONE)                → EVIDENCE + PRODUCT/NUMERIC AUTHORITY DECISION: reconciled the
                                    FREQ.6D.8-disclosed Intermediate x5D missing/explicit-zero Core
                                    starting-volume failure against the complete FREQ.6/FREQ.6B/FREQ.6C
                                    authority chain. FREQ.6C already approved exact 5D-specific values
                                    two phases earlier -- missing=26.0km (Hal Higdon Week-1 evidence
                                    anchor), explicit-zero=19.5km (26.0*0.75, reusing 4D's own ratio
                                    applied to 5D's own anchor), peak reference=44.5km, long-run
                                    share=28%/36% cap -- all 14 representability cells already ELIGIBLE.
                                    Runtime never wires to this: CatalogVolumeAndLongRunPlanner.Build
                                    special-cases only Intermediate x3D/Beginner x4D, so 5D falls
                                    through unconditionally to the generic, 4D-provenance-labeled
                                    V1MissingReadinessStartingVolumePolicy (16km/12km) -- below the real
                                    5D 2-KEY structural minimum (~12.5-13.4km, computed from real
                                    unmodified per-session minima). Corrected FREQ.6D.6's prior premise
                                    that this fallthrough was already-live 5D authority
                                    (SUPERSEDED_BY_EXISTING_FREQ6C_AUTHORITY); Runway's own weekly-
                                    structure decision is unaffected. Distinguished this real,
                                    already-realized failure from FREQ.6C's separate, still-theoretical
                                    KEY2-floor asymmetric-allocation risk (unreachable today).
                                    Re-reproduced all 8 Core-only missing/zero HTTP 500s (8/10/12/14wk x
                                    missing/zero) against the real host/DB. No production code touched.
                                    Classification:
                                    INTERMEDIATE_5D_MISSING_ZERO_EXISTING_NUMERIC_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT.
FREQ.6D.10 (DONE)               → IMPLEMENTATION + INTEGRATED VERIFICATION: wired the FREQ.6D.9-
                                    confirmed existing FREQ.6C authority into CatalogVolumeAndLongRunPlanner
                                    (Core) and TenKPreparationRunwayNumericPolicyFactory (Runway) via
                                    exact-identity-only dispatch (mirroring ThreeDayIntermediate/
                                    BeginnerFourDay, never a broad DaysPerWeek>=5 condition). Fixed a
                                    latent units bug in PreparationRunwayNumericMaterializer's long-run-
                                    share validation, isolated per-policy (Default/3D/Beginner4D byte-
                                    identical). Real HTTP + real PostgreSQL: all 8 previously-failing
                                    Core-only cases and all 6 representative Runway cases now return 200.
                                    Full regression 3787/3791 (all diagnosed), 1719/1719 targeted,
                                    1510/1510 PlanCatalog. Classification:
                                    INTERMEDIATE_5D_MISSING_ZERO_NUMERIC_AUTHORITY_IMPLEMENTED_AND_VERIFIED.
FREQ.6D.11 (DONE)               → ARCHITECTURE DESIGN: selected the complete dual-KEY session-identity
                                    model (LaneOrdinal+SlotOrdinal), 5-column LongHorizonRollingSessionState
                                    schema (zero backfill), (StructuralRole,LaneOrdinal,SlotOrdinal) JIT
                                    composition key (fixing a confirmed real collision in
                                    BuildBoundedCoreSelection), frozen-bundle profile-binding lifecycle,
                                    and ExecutionPrescriptionIndex propagation reusing the shared
                                    TenKPreparationRunwayDarkOrchestrator. Found LongHorizon 21-52wk =
                                    GE(variable) + real Runway(fixed 8wk) + real Core(fixed 12wk) -- Runway/
                                    Core segments inherit existing FREQ.6C/6D.6/6D.10 authority automatically;
                                    only the GE segment has no approved 5D weekly structure/numeric
                                    coefficients. Re-audited gates: 21 lines/14 files. Defined 6-split
                                    implementation decomposition. No code touched. Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_ARCHITECTURE_APPROVED_PRODUCT_POLICY_REQUIRED.
FREQ.6D.12 (DONE)                → EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION: selected the 5D GE
                                    weekly structure (1 KEY + 3 EASY + 1 LONG, constant, no ramp -- direct
                                    generalization of existing 4D GE and approved 5D Runway). Missing/
                                    explicit-zero readiness -> PRODUCT_INELIGIBLE (GE's own existing fail-
                                    closed rule, never had a fallback default). Quantified that extending
                                    4D GE's uncapped growth to 32 weeks reaches ~70+km/week -- rejected,
                                    replaced with a target-capped model plateauing at the existing 44.5km
                                    peak reference and 28%/36% long-run share (both reused, not invented).
                                    Full 21-52wk representability verified for positive-observed readiness.
                                    No code touched. Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_GE_PRODUCT_AND_NUMERIC_POLICY_APPROVED /
                                    INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTATION_READY.
FREQ.6D.13 (DONE, PARTIAL)      → IMPLEMENTATION + DARK INTEGRATION VERIFICATION: implemented and real-
                                    PostgreSQL-verified rolling-session lineage persistence (5 new nullable
                                    columns) and the JIT dual-KEY collision fix (SlotOrdinal-exact identity
                                    replacing StructuralRole-grouped FIFO dequeue). Fixed two real gaps
                                    (PublishedBundleReleaseVersion threading, IsValidFourDayAvailability's
                                    hardcoded ==4). GE 5D structural/numeric policy (FREQ.6D.12) and the
                                    dependent dark 21-52wk verification NOT implemented -- disclosed as
                                    remaining scope, not a blocker. Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_ROLLING_LINEAGE_AND_JIT_DUAL_KEY_IMPLEMENTED_
                                    AND_VERIFIED_GE_IMPLEMENTATION_REMAINING.
Gate B (PASS: fc34d7e)          → remote SHA matched local gate SHA; ahead/behind 0/0. 16
                                    completed phase prompts since the prior Gate B (13594ac):
                                    FREQ.6D.4D.5A through FREQ.6D.13.
FREQ.6D.14 (DONE, PARTIAL)      → IMPLEMENTATION + DARK INTEGRATION VERIFICATION: implemented the
                                    FREQ.6D.12-approved GE 5D structural/numeric policy (1K+3E+1L,
                                    44.5km target cap w/ plateau, 28%/36% long-run share -- all reused
                                    from FREQ.6D.10's VolumeSafetyPolicy.FiveDayIntermediate, zero new
                                    numeric constants; missing/zero -> typed PRODUCT_INELIGIBLE).
                                    Dark-verified the full 21/24/28/32/40/52 week matrix (35 tests).
                                    Found+fixed 3 real gaps (missing ExecutionPrescriptionIndex wiring
                                    in this orchestrator's own Core pipeline, non-candidate-aware Runway
                                    numeric policy call, 2 hardcoded "exactly 4 slots" validators).
                                    Found and honestly disclosed (not hidden) a pre-existing, confirmed
                                    non-5D-specific 22-week Runway numeric-continuity gap. Real
                                    PostgreSQL persistence for the 5D GE rolling-activation path and
                                    full adaptation/repair verification NOT completed -- disclosed as
                                    remaining scope, not a blocker. Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_GE_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL.
FREQ.6D.15 (DONE, PARTIAL)      → IMPLEMENTATION + REAL DATABASE VERIFICATION + DARK CLOSURE:
                                    root-caused the 22-week gap to a genuine, pre-existing, day-count-
                                    neutral Preparation Runway numeric-continuity authority gap (GE's
                                    forward-only growth vs Runway's forward-only interpolation vs Core's
                                    fixed boundary) -- proved systemic (23/25/26/27wk fail identically;
                                    even Recovery-terminal 28wk fails at low baseline), confirmed via
                                    direct 4D repro. Correctly did not invent a value to force a fix.
                                    Completed real PostgreSQL persistence + fresh reload for the 5D GE
                                    rolling-activation window (short + 52wk long case), fixing 2 real
                                    gaps (EASY role-key collision; reload projection silently dropping
                                    FREQ.6D.13 lineage columns). Persisted adaptation/repair NOT
                                    completed. Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_DARK_COMPLETION_BLOCKED_ON_SHARED_22_
                                    WEEK_NUMERIC_AUTHORITY.
FREQ.6D.16 (DONE)               → EVIDENCE + PRODUCT_DECISION + NUMERIC_DECISION: resolved the shared
                                    22-week gap via real numeric traces (temporary, uncommitted,
                                    reverted diagnostics -- zero residual code changes). Root cause:
                                    Runway's starting evidence is the raw, unclamped GE exit with no
                                    reconciliation against Core's own Week-1 target; the reachability
                                    check reuses a 0.001km sum-reconciliation epsilon for an unrelated
                                    product question (same class of mistake as FREQ.6D.10's own
                                    LongRunShareTolerance fix, different field). Found a previously-
                                    unnoticed 4D/24wk long-run edge case alongside the known 22/23wk
                                    failures. Approved: clamp Runway's starting weekly/long-run to
                                    Core's own Week-1 target whenever GE's exit would exceed it -- no
                                    new number, day-count-neutral, conservative, zero GE/Core/Runway
                                    structural change. No production code authored. Classification:
                                    SHARED_10K_LONGHORIZON_22_WEEK_NUMERIC_AUTHORITY_APPROVED.
FREQ.6D.17 (DONE, PARTIAL)      → IMPLEMENTATION + REAL POSTGRESQL VERIFICATION + DARK CLOSURE:
                                    implemented the FREQ.6D.16-approved clamp at its single shared
                                    owner (PreparationRunwayNumericMaterializer.Materialize) -- no new
                                    numeric constant, conservative by construction. Re-verified 4D/22,
                                    4D/23, 5D/22, 5D/23, and the 4D/24 long-run edge all now succeed.
                                    Mechanically re-ran the full 21-52 week dark matrix: 32/32 success
                                    at the same representative baseline every other test already uses
                                    (the forced near-cap workaround is no longer needed for any
                                    horizon). Real PostgreSQL persisted adaptation/repair verification
                                    (LongHorizonRollingCheckpointRuntime) NOT completed -- FREQ.6D.15's
                                    own disclosed remaining scope, still open, not a blocker.
                                    Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_CORE_ENTRY_CLAMP_IMPLEMENTED_AND_DARK_
                                    VERIFIED_PERSISTED_ADAPTATION_REPAIR_REMAINING.
FREQ.6D.18 (DONE, PARTIAL)      → REAL DATABASE VERIFICATION + DARK INTEGRATION CLOSURE:
                                    reconnaissance found two separate adaptation authorities coexist
                                    (checkpoint runtime's coarse Growth/Maintenance dispatch vs. the
                                    real 5-session severity table in NextWindowLoadDecisionPolicy).
                                    Found and fixed eight real 4D-hardcoding/lineage-drop defects,
                                    most significantly ScheduleRepairPersistenceService never copying
                                    LaneOrdinal/SlotOrdinal/ProgressionStageKey onto a repair
                                    replacement (a 4D+5D bug). Root cause: InitializeStructuralStateAsync
                                    hardcoded DaysPerWeek=4 at plan creation, so every 5D plan's
                                    persisted DaysPerWeek silently reverted to 4 and every reload
                                    rebuilt a 4D-shaped skeleton. Verified 9 new real-Postgres
                                    persist/reload tests: full 5-session severity table, checkpoint-
                                    continuation cardinality/lineage, GE KEY + repeated-EASY repair.
                                    Did NOT attempt GE->Runway/Runway->Core persisted-adaptation
                                    scenarios or Core secondary-KEY repair (no real persisted Core
                                    session in any available fixture). No new numeric constant,
                                    schema, catalog content, or identity-model redesign.
                                    Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_PERSISTED_ADAPTATION_AND_REPAIR_
                                    VERIFIED_FOR_GE_SEGMENT_RUNWAY_CORE_BOUNDARY_SCENARIOS_REMAINING.
FREQ.6D.19 (DONE, PARTIAL)      → REAL POSTGRESQL INTEGRATION VERIFICATION + DARK CLOSURE:
                                    drove a real 21-week 5D plan organically from persisted GE
                                    through persisted Runway into a real, organically-materialized
                                    first Core window (2K+2E+1L, distinct lanes) via the real
                                    production chain -- never a fabricated Core row. Found and fixed
                                    five real defects only surfaced by actually reaching Core for 5D:
                                    PublishedBundleReleaseVersion never threaded into JIT composition;
                                    LongHorizonRollingJitActivationRuntime called the parameterless
                                    (always-Default) numeric policy factory instead of the candidate-
                                    aware overload; two calendar validators each hardcoded an expected
                                    4-sessions-per-week count; ContinueJitCompositionAsync had no way
                                    to supply TargetFinishTimeSeconds/Source at all. Verified real
                                    secondary-KEY repair preserves LaneOrdinal=1 without disturbing
                                    LaneOrdinal=0, and deterministic repair->continuation. Confirmed
                                    date-order reversal is NOT_REACHABLE_UNDER_VALID_REPAIR_CONSTRAINTS
                                    (every repair candidate is structurally later-date-only) rather
                                    than a gap. Did NOT resolve one remaining real gap: no
                                    TargetFinishTimeSource classification is persisted anywhere for a
                                    restarted LongHorizon plan -- a genuine product decision, correctly
                                    not made here. No new numeric constant, schema, catalog content,
                                    or identity-model redesign.
                                    Classification:
                                    INTERMEDIATE_5D_LONGHORIZON_GE_RUNWAY_CORE_BOUNDARY_AND_DUAL_KEY_
                                    REPAIR_DARK_VERIFIED_TARGET_FINISH_TIME_PRODUCT_DECISION_REMAINING.
FREQ.6D.20 (DONE)               → DOMAIN / PRODUCT AUTHORITY + PERSISTENCE SEMANTICS DECISION:
                                    traced the full TargetFinishTime/TargetFinishTimeSource
                                    provenance dataflow -- found the exact loss point at plan
                                    confirmation (value is in-memory, never copied to TrainingPlan,
                                    which has no column for it). Plan-wide gap, only live/blocking
                                    for LongHorizon's restart-from-durable-state path. Confirmed
                                    ProductAverage/UserDefined (only two values) can share a numeric
                                    target time, so reverse-inference is unsafe. Approved plan-level
                                    persistence on TrainingPlan (one new nullable string column, no
                                    new enum, SCHEMA_CHANGE_APPROVED) over rolling-state duplication
                                    or derive-on-restart. Froze confirmation boundary, restart
                                    semantics (read verbatim, never re-derive), and historical
                                    handling (UNKNOWN_LEGACY, permanent, never backfilled). Specified
                                    an 11-step implementation contract, design only. No production
                                    code, tests, migration, or catalog content authored.
                                    Classification:
                                    TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_AUTHORITY_APPROVED.
FREQ.6D.21 (DONE)               → IMPLEMENTATION + SCHEMA MIGRATION + REAL POSTGRESQL VERIFICATION:
                                    added TrainingPlan.TargetFinishTimeSource (nullable enum, rides
                                    the repository's existing global SnakeCaseEnumConverter), one
                                    nullable-column migration with zero backfill applied to real
                                    Postgres, populated at both confirmation write boundaries, threaded
                                    from LongHorizonRollingWindowActivationService's already-loaded
                                    TrainingPlan row into ContinueJitCompositionAsync -- zero new
                                    queries, zero rolling-state duplication. Verified via 8 new tests
                                    including the mandatory same-seconds-different-source regression
                                    and a real-Postgres proof that a real Intermediate x5D plan reaches
                                    organic Core generation with GOAL_PACE_TEN_K succeeding via a
                                    ProductAverage source read from the persisted plan alone;
                                    UserDefined/historical-null sources fail closed identically,
                                    never reclassified. No new source classification, no derive-on-
                                    restart, no public activation (public gate confirmed still
                                    rejects 5D). Full regression 3894/3896 (+8 new passing). Closes
                                    the entire FREQ.6D.18->19->20->21 arc.
                                    Classification:
                                    TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_IMPLEMENTED_AND_
                                    VERIFIED / INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED.

INTERMEDIATE×5D LONGHORIZON DARK IMPLEMENTATION: COMPLETE (public activation still pending).

NEXT (NOT_YET_SCHEDULED)        → final Intermediate×5D LongHorizon capability phase: real public
                                    HTTP/PostgreSQL verification and public activation (widen public
                                    routing to 21-52, real GeneratePreview HTTP, representative
                                    21/22/24/32/52, positive readiness, missing/zero typed
                                    PRODUCT_INELIGIBLE, real confirmation + fresh reload, Home/
                                    Calendar/TrainingDay detail, persisted GE->Runway->Core,
                                    ProfileBacked Core, unsupported-neighbor closure, 4D/5D zero-delta).
                                    FREQ.7 / FREQ.8 (legacy placeholder IDs) remain further out
```

Then (capability milestones, no Phase IDs yet):

- 6D/7D evidence/reuse matrix
- 6D/7D numeric/product closure
- 6D/7D implementation/generalization
- Roadmap checkpoint / push gate (Gate B — approaching ~10 completed phase prompts since the last gate; this governance phase itself functions as an out-of-cycle gate per Gate D, see governance report)

---

## 15. Future Milestones — no fake IDs

### WAVE A — 10K

- Finish Intermediate 5D. **DONE** (`FREQ.6D.22`).
- Generalize Intermediate 6D/7D. **DONE** (`FREQ.6D.23`/`FREQ.6D.27`) — 6D public, 7D `PRODUCT_NON_SUPPORT`.
- Complete Advanced Level across proven frequencies. **DARK-COMPLETE, PUBLIC-CLOSED** (`GEN.9`) — 3D/4D/5D/6D all `IMPLEMENTED_AND_DARK_VERIFIED` (real PostgreSQL-proven for 5D; structural/numeric authority directly verified for 3D/4D/6D); 7D `PRODUCT_NON_SUPPORT`. Public activation not yet started.
- Complete Beginner remaining frequencies. **DONE** (`GEN.6`) — 5D/6D/7D all `PRODUCT_NON_SUPPORT`; Beginner remains 4D-only. `BEGINNER_FREQUENCY_AUTHORITY_COMPLETE`.
- Produce final 10K 15-cell support matrix.
- Full 10K backend regression.
- 10K release-readiness closure.

### WAVE B — HALF MARATHON

- 10K→HM reuse/gap audit.
- HM evidence synthesis.
- HM phase/horizon authority.
- HM numeric authority.
- HM workout capability.
- Intermediate 4D anchor.
- HM frequency matrix.
- Beginner matrix.
- Advanced matrix.
- HM full backend closure.

### WAVE C — MARATHON

- HM→Marathon reuse/gap audit.
- Marathon phase/horizon.
- Long-run/volume/pace/taper authority.
- Intermediate anchor.
- Frequency matrix.
- Beginner matrix.
- Advanced matrix.
- Marathon full backend closure.

### WAVE D — CROSS-DISTANCE

- Distance routing authority audit.
- Catalog graph audit.
- Persistence/replay audit.
- Public API integration.
- Cross-distance regression.
- Production release readiness.

No speculative phase IDs are assigned to any of the above until their own prompt/report is created.
