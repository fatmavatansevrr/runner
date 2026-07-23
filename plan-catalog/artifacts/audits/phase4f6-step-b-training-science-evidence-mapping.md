# Phase 4F.6 Pre-Implementation — Step B: Training-Science Evidence Mapping

Companion document to `phase4f6-step-b-training-science-evidence-mapping.json`. Read-only evidence-mapping pass over the existing `TEN_K/INTERMEDIATE/4D` workout progression. No code, catalog, schema, or governance file modified. Role binding, EASY_SUPPORT/LONG_RUN mapping, and new-workout-artifact decisions remain explicitly out of scope and deferred to Step C.

## 1. Audit verdict

**EVIDENCE_MAPPING_COMPLETE_WITH_GAPS**

## 2. Executive conclusion

- **Evidence-backed**: the general-to-specific, low-to-high-intensity phase progression (FOUNDATION→BUILD→RACE_SPECIFIC→TAPER); a distinct taper phase; threshold-zone work as one component within a broader pyramidal approach; low-intensity dominance of overall volume; race-specific/goal-pace exposure increasing near competition.
- **Evidence-informed** (directional, not exact): stage ordering within phases; compression/extension direction (protect specificity-critical work); fartlek as a BUILD-phase entry stimulus; the goal-pace fallback mechanism's underlying principle.
- **Exact product defaults, not evidence-backed**: every stage's minimum/maximum exposure count (14 fields across 7 stages), the exact 4-phase taxonomy and naming, the exact `3/4/4/1` week allocation, and the `8/12/14` core-cycle bounds. Recommend Step C formally reclassify most of these from `PlaceholderUnconfirmed` to `ExplicitProductDefault`, since scientific literature is structurally unlikely to ever resolve exact counts like these.
- **Two genuine evidence tensions found** (the audit's key findings, both flagged for Step C, neither acted on here):
  1. `TAPER_SHARPEN` — the plan's only TAPER-phase stage — is bound exclusively to `EASY_STANDARD` (an EASY-family workout), despite its own name implying an intensity-maintaining purpose and despite a directly-relevant meta-analysis (Bosquet 2007) finding that effective tapers maintain intensity while reducing volume.
  2. Appsel's TAPER phase is fixed at exactly 1 week (min=preferred=max=1), shorter than Bosquet 2007's ~2-week optimal taper window — reported with population/distance-extrapolation caveats, not as a confident contradiction demanding an immediate value change.

## 3. Sources used

| ID | Citation | Type | Verified via |
|---|---|---|---|
| TDE-001 | Kenneally, Casado, Santos-Concejero (2018). *The Effect of Periodization and Training Intensity Distribution on Middle- and Long-Distance Running Performance: A Systematic Review.* IJSPP 13(9):1114-1121. PMID 29182410. | Systematic review | Web search against published IJSPP record + PubMed listing |
| TDE-002 | Casado et al. (2022). *Training Periodization, Methods, Intensity Distribution, and Volume in Highly Trained and Elite Distance Runners: A Systematic Review.* IJSPP 17(6):820. | Systematic review | Web search against published IJSPP record. Full author list and PMID not independently confirmed — noted as `NOT_VERIFIED_IN_THIS_PASS` rather than invented. |
| TDE-003 | Bosquet, Montpetit, Arvisais, Mujika (2007). *Effects of tapering on performance: a meta-analysis.* Medicine & Science in Sports & Exercise. PMID 17762369. | Meta-analysis | Web search against published record + PubMed listing |

Existing repository evidence reused: `evidence-log.json`/`.md` (all 8 entries reviewed, found `EXISTING_INSUFFICIENT` — explicitly scoped to 4 runtime-resolver thresholds only, none touching workout-progression content); `PilotDomainContentAudit.cs` AUD-004/005/006/008/013/014/015 (reused directly as the governance-status baseline, not as scientific evidence); Golden Fixture v3 (structural corroboration only).

## 4. Phase-sequence assessment

| Phase | General-to-specific principle | Exact 4-phase taxonomy | Exact week allocation (3/4/4/1) |
|---|---|---|---|
| All | `EVIDENCE_BACKED` (TDE-001, TDE-002) | `NO_DIRECT_EVIDENCE` → `EXPLICIT_PRODUCT_DEFAULT` | `NO_DIRECT_EVIDENCE` → `EXPLICIT_PRODUCT_DEFAULT` |

TAPER specifically: distinct taper phase is `EVIDENCE_BACKED` (TDE-003); its **fixed 1-week length** is flagged `CONTRADICTED_BY_EVIDENCE` against TDE-003's ~2-week optimal finding, moderated by required population (elite→intermediate) and distance (not 10K-specific) extrapolation — see D51.

## 5. Stage-by-stage assessment

| Stage | Phase | Intent | Placement/order | Exposure counts | Compression/extension |
|---|---|---|---|---|---|
| `FOUNDATION_EASY_BASE` | FOUNDATION | EVIDENCE_BACKED | EVIDENCE_BACKED | NO_DIRECT_EVIDENCE | EVIDENCE_INFORMED |
| `FARTLEK_INTRO` | BUILD | EVIDENCE_INFORMED (fartlek-as-modality unevidenced) | EVIDENCE_INFORMED | NO_DIRECT_EVIDENCE | NOT_AN_EVIDENCE_QUESTION |
| `THRESHOLD_INTRO` | BUILD | EVIDENCE_BACKED (as a component, not as whole-plan model) | EVIDENCE_INFORMED | NO_DIRECT_EVIDENCE | NOT_AN_EVIDENCE_QUESTION |
| `TEN_K_SPECIFIC_INTRO` | RACE_SPECIFIC | EVIDENCE_INFORMED | EVIDENCE_BACKED/INFORMED | NO_DIRECT_EVIDENCE | NOT_AN_EVIDENCE_QUESTION |
| `GOAL_PACE_REHEARSAL` | RACE_SPECIFIC | EVIDENCE_BACKED | EVIDENCE_INFORMED | NO_DIRECT_EVIDENCE | EVIDENCE_INFORMED (PROTECTED directionally well-supported) |
| `CURRENT_FITNESS_SPECIFIC_REHEARSAL` | RACE_SPECIFIC | EVIDENCE_INFORMED | NOT_AN_EVIDENCE_QUESTION | NO_DIRECT_EVIDENCE | flagged: asymmetric vs. GOAL_PACE_REHEARSAL (see §10) |
| `TAPER_SHARPEN` | TAPER | **CONTRADICTED_BY_EVIDENCE** (see §2, key finding) | N/A (only stage) | NO_DIRECT_EVIDENCE | EVIDENCE_INFORMED (PROTECTED) |

Important nuance on `THRESHOLD_INTRO`/`TEN_K_SPECIFIC_INTRO`/`CURRENT_FITNESS_SPECIFIC_REHEARSAL`: TDE-001/TDE-002 found pyramidal/polarized TID models outperform a threshold-*dominant* model as a whole-plan strategy — but the catalog never declares threshold training as its overall TID model, it uses threshold work as one stage within a broader progression. No contradiction; flagged for awareness only.

## 6. Exposure-bound assessment

All 14 minimum/maximum exposure fields (2 per stage × 7 stages) are classified `NO_DIRECT_EVIDENCE`, decision status `PlaceholderUnconfirmed`. Recommendation: reclassify to `EXPLICIT_PRODUCT_DEFAULT` for all 14, since exact exposure counts bounded by phase length are inherently scheduling choices, not open scientific questions awaiting resolution.

## 7. Compression/extension assessment

Directional support exists only where a specificity-protection rationale applies: `GOAL_PACE_REHEARSAL` (PROTECTED/FIXED_EXPOSURE) and `TAPER_SHARPEN` (PROTECTED) are `EVIDENCE_INFORMED`. All other COMPRESSIBLE/EXTENDABLE assignments are `NOT_AN_EVIDENCE_QUESTION` (scheduling policy). One internal-consistency flag: `CURRENT_FITNESS_SPECIFIC_REHEARSAL` is COMPRESSIBLE/EXTENDABLE despite serving the same specificity-protection need as `GOAL_PACE_REHEARSAL` — not a scientific question, but worth Step C review (D42).

## 8. EASY-family assessment

Both EASY-family stages (`FOUNDATION_EASY_BASE`, `TAPER_SHARPEN`) bind to the same single `EASY_STANDARD` definition — no specialized variant exists in the active catalog. `FOUNDATION_EASY_BASE`'s placement and general intent are evidence-backed. `TAPER_SHARPEN` is the audit's central finding (§2, §5) — its content does not fulfill the evidence-supported taper principle. **No role-binding claim (i.e., which structural slot either stage's output should fill) is made here** — that question remains Step A.1/A.2's and Step C's, not Step B's.

## 9. `EASY_STANDARD` assessment

Low-intensity dominance and easy running's appropriateness across all phases are evidence-supported. Whether recovery-pace running needs a separate catalog identity vs. a dosage parameter, and whether strides should be a component or a separate identity (`EASY_WITH_STRIDES`-style), are both classified `INSUFFICIENT_EVIDENCE`/`NO_EXISTING_SOURCE_VERIFIED_THIS_PASS` — honestly unresolved rather than asserted from unverified background knowledge. Both questions are deferred to Step C regardless of evidence outcome (tied to Step A.2's EASY_SHAKEOUT/EASY_WITH_STRIDES governance thread).

## 10. Goal-pace assessment

Race-specific pace exposure nearer competition is evidence-backed generally (TDE-002); its exact runtime-eligibility thresholds (`GOAL_FEASIBILITY_IN` gating) and fallback-key wiring are explicitly `NOT_AN_EVIDENCE_QUESTION` — architectural/resolver concerns already governed separately by `evidence-log.json`'s narrower scope. Whether intermediate runners should receive the identical stage form as advanced runners is `INSUFFICIENT_EVIDENCE` — neither source isolates a recreational-intermediate population.

## 11. Evidence gaps

- **EXACT_NUMBERS**: all 14 exposure fields, the `3/4/4/1` allocation, and the `8/12/14` core-cycle bounds.
- **ORDERING**: stage sequencing within phases (directionally supported, not uniquely mandated).
- **POPULATION**: every claim drawn from TDE-001/TDE-002 requires elite→intermediate extrapolation.
- **DISTANCE**: TDE-003 is not distance-running-specific at all; TDE-001/002 are middle/long-distance broadly, not 10K-isolated.
- **CONFLICT**: `TAPER_SHARPEN`'s EASY-only content vs. TDE-003's intensity-maintenance finding; TAPER's fixed 1-week length vs. TDE-003's ~2-week optimum.
- **NO_SOURCE**: fartlek-as-entry-modality choice, strides-as-component-vs-identity, recovery-run-as-distinct-identity.
- **NOT_SCIENTIFICALLY_DETERMINABLE**: all compression/extension enum mechanics, resolver/registry wiring, fallback-key wiring — correctly excluded from evidence mapping.

## 12. Existing evidence reuse

`evidence-log.json` was reviewed in full and found insufficient for Step B's questions — it is explicitly scoped to 4 runtime-resolver thresholds, none of which concern workout-progression content. This independently confirms Step A.1's earlier finding that no general training-domain evidence registry exists today. `PilotDomainContentAudit.cs` was reused directly for governance-status baselines (a distinct concept from scientific evidence, kept on separate axes throughout this audit).

## 13. Evidence-governance recommendation

**`NEW_TRAINING_DOMAIN_REGISTRY_RECOMMENDED`** — conceptual needs only (not designed): stable evidence IDs, bibliography, atomic claims, `supports`/`doesNotSupport`, population/applicability, affected decision IDs, evidence strength, supersession, append-only history.

## 14. `EASY_SHAKEOUT` risk recommendation

Per the user's own proposal preceding this task, no such risk record currently exists. Recommended (not created): `TD-EASY-WORKOUT-REGISTRY-001`, category `ACTIVATION_READINESS / CATALOG_FIXTURE_DIVERGENCE`. Blocking scope: `NON_BLOCKING_FOR_STEP_B`, `BLOCKER_FOR_4F6B`, `BLOCKER_FOR_PUBLICATION_IF_UNRESOLVED`. Not written to `activation-readiness-risks.md` — recommendation only, per this task's explicit prohibition.

## 15. Decisions escalated to Step C

- **PRODUCT**: reclassify placeholder exposure counts to explicit defaults; review TAPER's fixed 1-week length; review `TAPER_SHARPEN`'s EASY-only content; decide fartlek's status as BUILD entry modality.
- **SCHEMA**: recovery-run and strides identity-vs-dosage question (ties to EASY_SHAKEOUT/EASY_WITH_STRIDES thread).
- **GOVERNANCE**: resolve the `GOAL_PACE_REHEARSAL` vs. `CURRENT_FITNESS_SPECIFIC_REHEARSAL` protection asymmetry; formalize a training-domain evidence registry.

## 16. Validation results

- JSON syntax: valid (`node -e "JSON.parse(...)"`).
- Structural checks (ID uniqueness, reference integrity, non-empty `doesNotSupport`, full stage/phase/field coverage, no LONG_RUN reopening, no role-binding-as-evidence): all pass.
- No repository code/catalog change occurred since Step A.2, so its validation numbers were reused rather than re-run: `plan-catalog` full suite 335/335 passing; backend relevant subset 131/131 passing.

## 17. Files inspected

`ten-k-workout-progression.v5.json`, `ten-k-master.v6.json`, `easy-standard.v4.json`, `evidence-log.json`/`.md`, `PilotDomainContentAudit.cs` (relevant AUD entries), Golden Fixture v3 plandocument, all three Step A/A.1/A.2 artifacts, plus external web-search verification of TDE-001/002/003 against their published records.

## 18. Files created

- `plan-catalog/artifacts/audits/phase4f6-step-b-training-science-evidence-mapping.json`
- `plan-catalog/artifacts/audits/phase4f6-step-b-training-science-evidence-mapping.md`

## 19. Files modified

None. No production, catalog, schema, canonical decision, or evidence-governance file was modified. Step A/A.1/A.2 artifacts untouched. `activation-readiness-risks.md` untouched (risk record recommended only, not created).

## 20. Repository state

Branch `main`, HEAD unchanged at `0c6796578f08bc1d76d96f1944a80c9075455206`. No staged changes. No commit made.

## 21. Step C readiness

**`READY_FOR_STEP_C_WITH_EVIDENCE_GAPS_EXPLICITLY_ACCEPTED`**

## 22. Final conclusion

Scientific evidence can constrain: the overall general-to-specific phase progression, the existence and placement of a distinct taper phase, threshold/goal-pace work as components within a broader intensity distribution, and — most concretely — the content adequacy of the TAPER phase's only stage and that phase's fixed duration. Scientific evidence cannot and structurally will not determine: exact exposure counts, exact phase-week allocations, exact phase taxonomy/naming, or compression/extension enum mechanics — these must remain explicit Appsel product defaults, and Step C should formally reclassify them as such rather than leaving them open as if evidence resolution were pending. Stopping here per instruction — not proceeding to stage scheduling, workout binding, or prescription work.
