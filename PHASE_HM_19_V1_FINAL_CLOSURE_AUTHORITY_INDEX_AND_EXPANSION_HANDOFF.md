# PHASE HM.19 — HALF-MARATHON V1 FINAL CLOSURE, AUTHORITY INDEX, AND FUTURE EXPANSION HANDOFF

**Type:** DOCUMENTATION / GOVERNANCE CLOSURE (no production implementation, no public routing change, no catalog behavior change, no new training authority)

---

## 1. Files inspected

All 33 `PHASE_HM_*.md` report files at the repo root (HM.0 through HM.18, including every lettered/numbered sub-phase: HM.0.1 embedded in HM.0's own file; HM.1.1–HM.1.6 including HM.1.4A/HM.1.4B; HM.2 in its two distinct forms plus HM.2.1; HM.5.1/HM.5.2/HM.5.2A/HM.5.3). `PHASE_LEDGER.md`'s full HM table (34 rows). `MASTER_ROADMAP.md`'s HM narrative section (HM.0 through HM.18). Ground-truth source verification against: `VolumeSafetyPolicy.cs` (all 8 named HM policies), `HalfMarathonIntermediate4DTaperVolumePolicy.cs` (all 8 taper policy classes), `V1CatalogPilotIdentityPolicy.cs`, `RaceHorizonPolicy.cs`, `AppExceptions.cs`, `CatalogCalendarAssignmentExceptions.cs`, `CatalogVolumeExceptions.cs`, `half-marathon-master.v1/v2/v3.json`, `half-marathon-workout-progression.v1/v2/v3.json`, `beginner-modifier.v1/v2.json`, `intermediate-modifier.v8/v9.json`, `advanced-modifier.v2/v3.json`, all 8 public HM combination files, `run-layout-3d/4d/5d.v1.json`, `peak-volume-bands.v9.json`, and `Hm18PublicActivationAndBoundaryTests.cs` plus the other `Hm*.cs` test files. A repository-wide grep for stale/contradictory current-state HM claims (`MVP_LIMITATIONS.md`, `DEVELOPER_HANDOFF.md`, `plan-catalog/docs/specifications/*.md`, and all `PHASE_HM_*.md`/`PHASE_10K_*.md` files).

## 2. Files created

- `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md` (repo root) — the main handoff document, structured per this phase's own required 32-section content, consolidated into one navigable file (a companion `HALF_MARATHON_V1_AUTHORITY_INDEX.md` was judged unnecessary — the numeric/version tables remain clean and readable within the single file).
- `PHASE_HM_19_V1_FINAL_CLOSURE_AUTHORITY_INDEX_AND_EXPANSION_HANDOFF.md` (this file).

## 3. Files modified (narrowly, per this phase's own explicit allowance to fix genuinely stale current-state documentation)

- `MVP_LIMITATIONS.md` — the single obsolete current-state claim ("If an onboarding user selects a Marathon or Half Marathon goal... returns 404 PLAN_TEMPLATE_NOT_FOUND") was corrected to state that this claim applies only to the legacy seeded-template mechanism itself (still true for that specific mechanism), while HALF_MARATHON is now served by the real catalog engine and is public for the frozen 8-cell V1 matrix; Marathon remains genuinely unimplemented. The surrounding historical Antigravity-MVP discussion was left untouched.
- `DEVELOPER_HANDOFF.md` — the single obsolete future-work line ("Additional Plan Templates: Seed additional schedules for Marathon, Half Marathon, and 10K distances") was struck through and annotated with the current state (10K and HALF_MARATHON are both implemented and public via the real catalog engine; only Marathon remains genuinely future work). No other roadmap content was touched.
- `PHASE_LEDGER.md` — HM.19's own row added (see §11 below).
- `MASTER_ROADMAP.md` — a short HM.19 closure entry added, per repository convention.

**No `.cs`, catalog JSON, schema, database migration, or Flutter runtime source file was touched.** Confirmed via `git status`/`git diff --stat` before finalizing this report (see §12).

## 4. Current public matrix (as recorded in the handoff document)

```
Beginner       3D PUBLIC   4D PUBLIC   5D PRODUCT_INELIGIBLE
Intermediate   3D PUBLIC   4D PUBLIC   5D PUBLIC
Advanced       3D PUBLIC   4D PUBLIC   5D PUBLIC
Horizon: 10-16 full weeks inclusive
```

## 5. Authority tables reconstructed

Full Beginner/Intermediate/Advanced numeric authority tables (peak bands, selected peaks, growth rates, absolute caps, calibration starts, LR-share quadruples, absolute-peak-LR, starting-LR-cap, taper multipliers, dose/modifier identity, hard-stimulus limits, HM_PACE policy) were reconstructed from the final accepted phase reports **and cross-verified directly against the real, current `VolumeSafetyPolicy.cs`/taper-policy source code**. **No mismatch was found between any final report's own frozen numbers and the actual, current source values** — every field in the handoff document's §7/§8/§9 tables is independently confirmed by direct code read, not merely copied from a report.

## 6. Catalog version matrix

Reconstructed by direct read of all 8 public HM combination files plus their referenced master/progression/modifier/layout documents. Confirmed three distinct master/progression version pairs are genuinely in use (v1/v1 for six 3D/4D cells; v2/v2 for Intermediate 5D; v3/v3 for Advanced 5D) — this assumption-check was explicitly required by this phase's own §7, since HM.16 already proved a "one master version for all cells" assumption false once. Confirmed the bespoke `INTERMEDIATE_MODIFIER v9`/`ADVANCED_MODIFIER v3` version splits and their exact reasons.

## 7. Test baseline

Recorded exactly as HM.18's own final, independently-verified numbers: `PlanCatalog.Tests` 1510/1510; `RuntimeCatalog` subtree 3798/3798; monolithic 4727/4731 passed, with the 4 durable failures recorded by their full, exact test identity (not merely "4 known failures") — see the handoff document §22. This phase did not re-run any test suite itself (a pure documentation phase has nothing new to verify by testing); the numbers are taken directly from HM.18's own report and ledger row, both of which the orchestrating session independently re-verified at the time via its own build/test runs and its own `.trx` parse.

## 8. Stale-doc findings

Two genuine `STALE_DOCUMENTATION` findings, both corrected narrowly (§3 above): `MVP_LIMITATIONS.md:40` and `DEVELOPER_HANDOFF.md:86`. Both were pre-GEN/pre-HM "Antigravity Phase 1 MVP Skeleton" onboarding documents never revisited as the GEN.*/HM.* catalog engine superseded the legacy seeded-template system.

Everything else found by the repository-wide grep was correctly classified `HISTORICAL_CONTEXT` (phase reports and specs correctly describing HM's state at their own point in the sequence — e.g. "dark-only" while HM was pre-HM.18, or "PRODUCT_INELIGIBLE" for Beginner 5D, both still true today for the specific cell/phase in question), `DELIBERATE_LEGACY_RECORD` (the original `HM_21K_Claude_Handoff_and_Build_Plan.md` and `APPSEL_HM_PHASE_0_DISTANCE_GENERALIZATION_AUDIT.md`, still correctly cited as historical evidence sources by the ledger), or `TEST_FIXTURE` (strings inside test code/`.trx` result files). No further correction was made or is needed.

## 9. Superseded-decision findings

Ten superseded-decision pairs were identified and recorded in the handoff document §27 (e.g. the `14/14/14`→`10/14/16` CoreCycle correction; point-valued headroom → dynamic Min/Preferred/Max; `RelativeOrder`-only compression → `CompressionPriority`; chained taper → non-chained fixed-anchor taper; HM.3's own "provably 5" RaceSpecific-floor finding later corrected by HM.4 to the true floor of 3; HM.5's own "whole Layer B blocked" finding later narrowed by HM.5.1 to exactly one field). None of these are described as errors — each was a legitimate intermediate state at the time it was recorded.

## 10. Do-not-reopen register

Fourteen items frozen in the handoff document §26, spanning Core structural authority, all three levels' full numeric tables, Advanced 5D's KEY2 model, the pace hierarchy, the calibration-not-readiness invariant, and the frozen V1 public matrix/horizon/2D6D-post-V1 classification itself. Each may only be reopened with new evidence, a new authority phase, and an explicit intended-delta review.

## 11. Post-V1 expansion roadmap

Four separate, explicitly-not-unfinished-V1-work tracks defined in the handoff document (§29–§32): HM-X1 (6D expansion), HM-X2 (7–9W compressed Core), HM-X3 (>16W Preparation Runway/LongHorizon), HM-X4 (2D low-frequency product), with a recommended (non-canonical) priority order 6D → 7-9W → Runway/LongHorizon → 2D, and a mandatory future-safety-rule set (doubled zero-delta obligation, catalog-pinning discipline, dark-first rollout, Beginner×5D stays closed by default).

## 12. Git diff classification

| File | Classification |
|---|---|
| `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md` | `NEW_CLOSURE_DOC` |
| `PHASE_HM_19_V1_FINAL_CLOSURE_AUTHORITY_INDEX_AND_EXPANSION_HANDOFF.md` | `NEW_CLOSURE_DOC` |
| `MVP_LIMITATIONS.md` | `INDEX_UPDATE` (narrow current-state correction) |
| `DEVELOPER_HANDOFF.md` | `INDEX_UPDATE` (narrow current-state correction) |
| `PHASE_LEDGER.md` | `LEDGER_UPDATE` |
| `MASTER_ROADMAP.md` | `LEDGER_UPDATE` (short closure entry) |

No `UNEXPECTED` entries. No `.cs`, catalog JSON, schema, migration, or Flutter runtime file appears in the diff — confirmed directly via `git status --porcelain` immediately before this report was finalized.

---

## FINAL CLASSIFICATION

```
HM_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF_COMPLETE
```

HM V1 public completion is recorded accurately; every public Level×Frequency cell is represented; every final numeric authority traces to an accepted report AND was independently cross-checked against the real, current source artifact with zero mismatches found; catalog versions are indexed from real files, including the three distinct master/progression version splits; the current public error contract is recorded with exact, real reason codes; the confirmation/persistence invariant (including the Advanced 5D dual-KEY proof and the legacy mapper's `NON_BLOCKING_LEGACY_LIMITATION` classification) is recorded; the final test baseline is exact, by full test identity; superseded decisions are separated from current authority; historical candidates are explicitly not promoted; the do-not-reopen register is explicit; post-V1 work is separated into four new expansion tracks with an explicit future-safety-rule set; and no runtime, catalog, schema, or frontend behavior was changed by this phase.
