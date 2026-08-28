# PHASE 10K-FREQ.6D.28 — Intermediate×5D LongHorizon Persisted Adaptation + Repair: Stale-Premise Reconciliation

**Parent phases**: `FREQ.6D.18`, `FREQ.6D.19`, `FREQ.6D.20`, `FREQ.6D.21`, `FREQ.6D.22`, `GEN.9`
**Phase type**: VERIFICATION_CLOSURE + GOVERNANCE RECONCILIATION
**Execution status**: DONE
**Final classification**: `INTERMEDIATE_5D_LONGHORIZON_PERSISTED_ADAPTATION_AND_REPAIR_ALREADY_CLOSED_AND_PUBLICLY_ACTIVE_STALE_PROMPT_PREMISE_CORRECTED`

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `git log -5`, `git fetch && diff HEAD origin/main` (empty — in sync), `git status` (clean except pre-existing unrelated local modifications to `baseline_tmp`/`plan-catalog/artifacts/audits/*` predating this session, untouched) all confirmed before starting. `GEN.9` row 111 confirmed present with real commit SHAs (`d50da27`/`2aa72d4`); `MASTER_ROADMAP.md` confirmed reflects `GEN.9` as latest completed phase. Next free phase ID searched and confirmed unique: `FREQ.6D.28`.

## 1. §0.5 pre-check — GEN.9 cross-cutting fix impact

**Question**: does `TenKPreparationRunwayNumericPolicyFactory` — the dispatch `GEN.9` found silently defaulting every non-Intermediate-5D/6D candidate to the 4D-shaped policy, and fixed by adding Advanced arms — also govern `LongHorizonRollingCheckpointRuntime`'s GE→Runway→Core continuity path for Intermediate×5D?

**Finding: yes, it is the same shared code path.** `TenKPreparationRunwayDarkOrchestrator.cs:263` calls `TenKPreparationRunwayNumericPolicyFactory.Build(request.Candidate)`, and this orchestrator is the exact shared engine both Preparation Runway's own Core-handoff and LongHorizon's Core-entry invoke (established architecture fact since `FREQ.6D.11`/`FREQ.6D.19`, re-confirmed here).

**Resolution**: `GEN.9`'s fix already covers the Intermediate case correctly, verified two ways, not assumed:
1. **Direct diff**: `git show d50da27^:...TenKPreparationRunwayNumericPolicyFactory.cs` shows the `("TEN_K","INTERMEDIATE",5)` and `("TEN_K","INTERMEDIATE",6)` dispatch arms existed, byte-identical, **before** `GEN.9`'s commit — `GEN.9` only *added* four new Advanced arms alongside them; it never touched the pre-existing Intermediate arms. The one shared modification (`longRunShareTolerance`'s derivation condition) was widened with an additional `||` clause for the new Advanced5D/6D cases — the pre-existing `FiveDayIntermediate`/`SixDayIntermediate` reference-equality checks are unchanged, so Intermediate's own tolerance derivation is provably identical before and after.
2. **Real test re-verification** (not code-inspection-only): re-ran the exact real-PostgreSQL persisted-adaptation/repair/restart tests `FREQ.6D.18`/`.19`/`.21`/`.22` established — see §2 — all 42 pass unchanged post-`GEN.9`.

This is a disclosed-and-resolved finding, not a silently-assumed one, per this phase's own explicit instruction.

## 2. Scope finding — Phase A's premise contradicted by repository truth

The governing prompt scoped Phase A as implementing/verifying persisted adaptation and persisted repair for Intermediate×5D LongHorizon's GE checkpoint-continuation path, citing `FREQ.6D.15`/`.17` as showing this still open. **Repository truth (`PHASE_LEDGER.md` rows 98-102) shows this scope was already fully closed, in a part of the phase history the prompt's author did not have visibility into**:

- **`FREQ.6D.18`** (`DONE (PARTIAL)`) — first real-PostgreSQL persisted-adaptation/repair verification for the GE segment; Runway/Core boundary scenarios explicitly disclosed as remaining.
- **`FREQ.6D.19`** (`DONE (PARTIAL)`) — closed the Runway/Core boundary: drove a real 21-week Intermediate×5D plan organically from persisted GE through persisted Runway into a real, organically-materialized first Core window, entirely through the real production chain (`LongHorizonRollingCheckpointRuntime` + `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync`). Its own report explicitly documents a **real secondary-KEY repair via `ScheduleRepairRuntimeOrchestrator`** preserving `LaneOrdinal=1` without disturbing the untouched `LaneOrdinal=0` primary, and confirmed continuation past a repaired week remains deterministic (no duplicate/lost session) — exactly the "real interruption/restart, prove correct resumption" rigor this prompt's Phase A demanded. Found and fixed 5 real defects in the process (documented in that phase's own ledger row). Left only `TargetFinishTimeSource` classification for restarted plans as a genuine remaining product decision.
- **`FREQ.6D.20`** (decision) — approved `TargetFinishTimeSource` persistence authority.
- **`FREQ.6D.21`** (`DONE`) — implemented it; its own ledger row states verbatim: *"this closes the entire FREQ.6D.18→19→20→21 arc (persisted adaptation, persisted repair, organic GE→Runway→Core, dual-KEY lineage/repair, real target-time-source-driven Core generation), so the accumulated Intermediate×5D LongHorizon dark-verification capability is complete."* Final classification: `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`.
- **`FREQ.6D.22`** (`DONE`) — opened the real public routing gate and completed the full horizon capability: `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_PUBLICLY_ACTIVATED` / `INTERMEDIATE_5D_FULL_HORIZON_CAPABILITY_COMPLETE`.

Per the governing prompt's own explicit, repeated instruction ("repository truth wins over this prompt"; "do not assume anything below is still accurate if the repository disagrees"), Phase A is correctly executed as a **reconciliation and reverification**, not new implementation — inventing redundant work against an already-closed, already-publicly-active capability would itself violate the no-new-authority/no-silent-shortcut discipline this engagement holds itself to.

## 3. Real reverification performed (not "no exception thrown")

Re-ran, unmodified, the exact real-PostgreSQL test suites `FREQ.6D.18`/`.19`/`.21`/`.22` established, post-`GEN.9`:

- `Freq6D18FiveDayPersistedAdaptationAndRepairTests`
- `Freq6D19FiveDayGeRunwayCoreBoundaryTests`
- `Freq6D21TargetFinishTimeSourceRestartTests`
- `Freq6D22IntermediateFiveDayLongHorizonPublicActivationTests`
- `ScheduleRepairRuntimeOrchestratorTests`

**42/42 pass.** These tests genuinely exercise persist→dispose→fresh-`DbContext`→reload→continue cycles and a real secondary-KEY repair through `ScheduleRepairRuntimeOrchestrator` (not a same-context or in-memory simulation) — this is the real interruption/restart proof the governing prompt required, already established by `FREQ.6D.19` and reconfirmed unchanged here.

## 4. Full regression, representability, zero-delta

The full `RunningApp.IntegrationTests` regression already run for `GEN.9`'s own closure (`gen9_v4.trx`, 3986 total) included every one of the above test files (all part of the standard suite) and showed **zero new regressions** — the same 2 durable pre-existing baseline failures (`Sw09ExplicitZeroReadinessEndToEndTests`, `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates(weeks:13)`) and nothing else. No new full-suite run was needed since no code changed in this phase (reconciliation only) and `GEN.9`'s own already-authoritative TRX already covers this exact scope. The full 21-52 week representability matrix (`FREQ.6D.17`'s own 32/32 proof) and Intermediate×4D zero-delta are both unaffected — no code in this phase touched either.

`git diff --check`: clean (no files changed by this phase besides this report and governance).

## 5. What this makes newly true

Nothing new — `INTERMEDIATE_TEN_K_FREQUENCY_AXIS_COMPLETE` was already correctly classified as complete by `FREQ.6D.27` (Intermediate 3D/4D/5D/6D `PUBLICLY_ACTIVE`, 7D `PRODUCT_NON_SUPPORT`), and that classification already presupposed `FREQ.6D.22`'s full 5D horizon closure. This phase's contribution is **confirming, with real re-run evidence, that `GEN.9`'s unrelated Advanced work did not silently regress any part of that already-complete capability** — a genuine governance gap this phase closes (the risk `GEN.9`'s own report flagged but didn't itself re-verify against Intermediate, since `GEN.9`'s scope was Advanced-only) — and formally correcting the stale premise in the governing prompt so it does not propagate into future planning.

## 6. Governance

No production code, tests, or catalog changes in this phase. `MASTER_ROADMAP.md`'s own historical "Prior phase" log (lines 84-88) was checked directly and found **already accurate** — it correctly documents the full `FREQ.6D.18→19→20→21→22` closure chain and the resulting `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`/`PUBLICLY_ACTIVATED` classifications. The stale premise originated in the incoming governing prompt, not in the repository itself, so no `MASTER_ROADMAP.md` correction was needed. `PHASE_LEDGER.md` gets a new row documenting this reconciliation and its real re-verification evidence, cross-referencing `FREQ.6D.18`-`.22` as the actual closing phases.
