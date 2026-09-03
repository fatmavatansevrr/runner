# PHASE 10K-GEN.22 — Beginner×5D Realignment (Phase L)

**Parent phases**: `GEN.6` (`BEGINNER_5D_6D_7D_PRODUCT_NON_SUPPORT_APPROVED` — the decision this phase re-examines under a new product requirement), `GEN.4A` (`QUALITY_SESSION_COUNT_IS_LEVEL_ELIGIBILITY_OR_CAP_ONLY` invariant), `GEN.21` (Phase K, sibling phase — independent question, run second per the governing prompt's own ordering)
**Phase type**: EVIDENCE + PRODUCT DECISION (no implementation)
**Execution status**: DONE
**Final classification**: `BEGINNER_5D_SECONDARY_CONTROLLED_NON_SUPPORT_REAFFIRMED` — formal non-support, re-derived and re-grounded under the new product requirement, not merely inherited unexamined from `GEN.6`

---

## 0. Precondition verification

Ran after Phase K's (`GEN.21`) commits were pushed; `git fetch` / `git rev-list --left-right --count origin/main...HEAD` confirmed 0 ahead/0 behind before this phase began. `GEN.22` confirmed as the correct next-free ID (no existing row/file).

This phase does **not** reopen `GEN.6`'s Beginner×6D/7D conclusions (unrelated frequencies, not re-examined here), Beginner×3D (`GEN.21`, independent question), Beginner×4D (re-verified zero-delta throughout, see §8), or any 2D authority.

## 1. What `GEN.6` actually decided, and what is being re-examined

`GEN.6` §4 (read in full) decided Beginner-level eligibility rejects any RunLayout requiring two structural `KEY_SESSION` slots per week, grounded in: (a) Kluitenberg et al.'s novice-vs-recreational injury-incidence gap (17.8/1000h vs 7.7/1000h, >2x), already `SUPPORTED` Tier-2 evidence in this repository's own `GEN.4B`; (b) the observation that Hal Higdon's Novice 10K and McMillan's Beginner Level-1 — both already-accepted Beginner-tier sources in this repository — are structurally single-quality-session-or-none programs; (c) `GEN.4A`'s own invariant that Level may only gate RunLayout eligibility, never redefine structural cardinality.

The new product requirement this phase examines: **can Beginner have Lane0=PRIMARY (a genuine KEY/quality lane, as today) and Lane1=SECONDARY_CONTROLLED — a second lane that is explicitly *not* a second genuinely-hard workout — on `RUN_LAYOUT_5D`'s frozen 2 KEY + 2 EASY + 1 LONG structure, without changing that structure?** This is a different question than `GEN.6`'s binary K=2-reject-or-accept: it asks whether the *second* KEY_SESSION slot can be filled with something categorically lighter than a genuine quality session, while the RunLayout's own structural slot count (K=2) stays exactly as frozen.

## 2. Structural constraint confirmed unchanged (verified from the live catalog, not assumed)

`plan-catalog/catalog/layouts/run-layout-5d.v1.json`, read directly this phase:

```json
"slots": [
  { "sequenceOrder": 1, "role": "KEY_SESSION" },
  { "sequenceOrder": 2, "role": "EASY_SUPPORT" },
  { "sequenceOrder": 3, "role": "KEY_SESSION" },
  { "sequenceOrder": 4, "role": "EASY_SUPPORT" },
  { "sequenceOrder": 5, "role": "LONG_RUN" }
]
```

Confirmed 2 `KEY_SESSION` + 2 `EASY_SUPPORT` + 1 `LONG_RUN`, frozen `VALIDATED` status, unchanged since `FREQ.4`. This phase's own required work (§ preserve the 5D structure) is satisfied by construction — no RunLayout document is touched anywhere in this phase, and none would need to be for either possible outcome (support via SECONDARY_CONTROLLED reuses the existing second `KEY_SESSION` structural slot with different *content*, not a different *slot count*).

## 3. External evidence — independently re-verified this phase, not accepted from the governing prompt at face value

Two of the governing prompt's three cited claims were independently checked this phase via live search (not accepted from the prompt's own text):

1. **McMillan Running's level system** (searched directly this phase, `mcmillanrunning.com`): confirmed **Level 2** ("Novice/Intermediate", 3-5 days/week) is explicitly capped at **1** specialty/hard workout per week even at up to 5 days/week. Two hard workouts/week does not appear until **Level 3** ("Intermediate", 4-6 days/week, "1-2 specialty/hard workouts") and is described as standard only from **Level 5** ("Advanced" — "2 hard workouts per week plus a long run"). This independently confirms the governing prompt's claim and sharpens it: the gating variable in McMillan's own real, published system is explicitly experience/training-level tier, not days-per-week — a Level-2 (novice-adjacent) runner training 5 days/week is still capped at 1 hard session, which is the exact scenario this phase is evaluating for Beginner×5D.
2. **RunnersConnect's second-hard-workout gating** (searched directly this phase, `runnersconnect.net`): the search surfaced RunnersConnect's own real published framing that **total weekly mileage under ~30 miles (48km) favors single-hard-session/easy-daily-running structures**, and that a second high-intensity session added "within 48 hours" of the first "can overwhelm your recovery system," with total weekly mileage described as "the single strongest predictor of injury risk." This is a **partial, not exact, corroboration** of the governing prompt's specific "30 miles/week gate" framing (the retrieved content discusses the same 30-mile figure and the same injury-risk-driven caution around a second hard session, but does not reproduce the prompt's precise sentence) — disclosed honestly rather than overclaimed as an exact match. Beginner×5D's own approved `PeakVolumeBand` scale (§4 below shows Beginner×4D peaks at 24.0 km, roughly half of RunnersConnect's ~48 km/week gating threshold) — well below the mileage tier RunnersConnect associates with a second hard session even in a *general* (not specifically beginner) population.
3. **PacePercentile's beginner-oriented framing** ("add a second quality session as a next step after finishing the beginner plan") was not independently re-verified via a fresh fetch this phase (already directly quoted in the governing prompt with a specific, checkable claim shape); given (1) and (2) above independently corroborate the same underlying conclusion via different real sources, this phase treats the overall evidence base as `SUPPORTED`, consistent with `GEN.6`'s own already-accepted Tier-2 injury evidence.

**Net finding**: real-world practice does **not** generally license a second genuinely-hard session for a beginner-tier runner purely by adding training days — every real source checked (this repository's own `GEN.6` Kluitenberg citation, this phase's own independently re-verified McMillan level system, this phase's own partially-corroborated RunnersConnect mileage-gating search) converges on experience/volume tier, not day-count, as the real gating variable.

## 4. Defining SECONDARY_CONTROLLED honestly — and finding the evidence too thin to specify it

Per this phase's own required work, `SECONDARY_CONTROLLED` must be defined **exactly**, not left as a label — concrete phase-by-phase (Foundation/Build/RaceSpecific/Taper) dose and eligibility, distinct from either a real quality session or a plain `EASY_SUPPORT` session relabeled.

Attempting this exactly, using the real Beginner-tier sources already accepted by this repository (`GEN.5A`/`GEN.5A.2`'s Hal Higdon Novice 10K, McMillan Beginner Level-1) plus the sources found this phase:

- Hal Higdon's Novice 10K (already-accepted Beginner-tier source, re-confirmed this phase via direct fetch, §5 of `GEN.21` above) has **no second run of any distinguishable "controlled" character** — its non-long-run days are a uniform 2-3mi easy run, with no strides, no controlled tempo, no distinguishable second tier of effort. It is a genuine single-quality-session-or-plain-easy-runs program, exactly as `GEN.6` already found.
- McMillan's Level-2 tier (this phase's own independently-verified source, §3) confirms the *cap* (1 hard session even at high day-count) but its publicly available marketing/level-description material does not specify what its *non-hard* sessions look like in exact prescriptive terms (pace zone, structure, or strides content) — that level of detail sits behind McMillan's paid plan content, not retrievable via search in this phase.
- PacePercentile's own framing (cited, not independently re-fetched this phase) explicitly frames a second quality-adjacent session as a **post-beginner-plan progression step**, not a concurrent beginner-plan component — i.e., its own real product design puts `SECONDARY_CONTROLLED`-shaped content entirely *outside* the beginner tier, not inside it as a controlled/reduced variant.

**No real source found or re-verified this phase — in this repository's own prior evidence base or independently searched here — describes a concrete, dosed "controlled second lane" (strides volume, count, pace-zone, or placement) actually used inside a genuinely beginner-tier weekly structure.** The one closest real analog (strides) appears only as a component of an *easy* run or a very-final pre-race week (`GEN.21` §5's PacePercentile citation: "a short session with strides" appears once, in the taper week, not as a recurring weekly second-lane structure), not as a repeatable per-phase dosed lane running through Foundation/Build/RaceSpecific/Taper as this phase's own required output would need.

## 5. Why this is not a `DOMAIN_DECISION_REQUIRED` STOP, but a reasoned non-support conclusion

The governing prompt frames genuine uncertainty or a real, evidence-backed judgment call as the STOP trigger. That is not the situation found here: every real source checked or re-checked this phase (Kluitenberg injury evidence already accepted by this repository; McMillan's real level system, independently re-verified; RunnersConnect's real mileage/recovery framing, independently searched; PacePercentile's real post-beginner-plan framing) points the **same direction** — away from a genuine dosed second lane inside the beginner tier — and none supplies the concrete numbers a real `SECONDARY_CONTROLLED` authority would need. This is the situation the governing prompt itself explicitly anticipated and pre-authorized as a legitimate closure: "If the evidence-based case for `SECONDARY_CONTROLLED` is weak or requires inventing dosing with no real precedent, say so plainly rather than constructing a technically-passable but evidence-thin lane definition."

Constructing an exact `SECONDARY_CONTROLLED` dose table now — e.g., "N strides of duration X at phase Y" — would require inventing numbers with no real precedent anywhere in this repository's accepted evidence or in this phase's own independent search, exactly the outcome the governing prompt asked this phase to avoid rather than paper over with a technically-passable-looking lane definition.

## 6. `QualitySessionCount` semantics — disclosed, not silently reinterpreted

This phase's required work asks whether `QualitySessionCount` means hard-quality exposure, structural KEY count, or prescription-level quality exposure. Checked directly: `QualitySessionCount` is not a literal code symbol anywhere in `backend/` (confirmed via direct search) — it is `GEN.4A`'s own prose name for its invariant (`QUALITY_SESSION_COUNT_IS_LEVEL_ELIGIBILITY_OR_CAP_ONLY`), where "quality session count" means **structural `KEY_SESSION` slot count as defined by the RunLayout document** (`GEN.4A`'s own restated §10, quoted verbatim by `GEN.6` §1: "structural KEY/EASY/LONG cardinality is exclusively RunLayout-owned; Level may gate whether a given RunLayout-defined structural KEY count K is eligible for that Level, but may never redefine K"). It is **not** a hard-quality-exposure or prescription-level-intensity concept in this repository's existing authority — no such concept exists as a distinct tracked quantity anywhere in the codebase today. This phase does not invent one; a `SECONDARY_CONTROLLED` lane, had it been approved, would have introduced exactly such a new distinct concept (a structural `KEY_SESSION` slot whose *prescribed content* is deliberately not hard-quality), which `GEN.4A`'s existing invariant has no vocabulary for today. This gap is disclosed, not resolved, since no lane is being approved.

## 7. Final decision

```
BEGINNER_5D_SECONDARY_CONTROLLED_NON_SUPPORT_REAFFIRMED
```

Beginner×5D remains `PRODUCT_NON_SUPPORT` across Core/Runway/LongHorizon, **superseding `GEN.6`'s reasoning (not its conclusion)**: `GEN.6` rejected Beginner×5D because it interpreted the second structural `KEY_SESSION` slot as necessarily a second genuinely-hard session under the frozen RunLayout. This phase examined the more specific, less absolute new product framing (a controlled, non-hard second lane filling that same slot) and found the real evidence base — now including two independently-verified sources beyond what `GEN.6` cited — converges just as strongly against a genuine, concretely-dosed `SECONDARY_CONTROLLED` lane existing anywhere in real beginner-tier practice as it did against a second hard session outright. The conclusion (non-support) is unchanged; the reasoning is deepened and re-grounded specifically against the new product framing this phase's governing prompt introduced, not merely inherited unexamined.

This is an explicitly legitimate closure per the governing prompt's own instruction, not a failure to find a way to say yes.

## 8. Beginner×4D confirmed unaffected (zero-delta, by construction)

No code, catalog, or configuration was touched in this phase. `V1CatalogPilotIdentityPolicy`'s allow-list (confirmed unchanged, not re-read line-by-line since nothing in this phase's scope could touch it — no file under `backend/` was edited), `CatalogVolumeAndLongRunPlanner`'s `Level == "NEW" && DaysPerWeek == 4` branch, and every other Beginner×4D-specific policy class remain exactly as `GEN.4E` left them. Beginner×4D is `PUBLICLY_ACTIVE`, unaffected, mentioned here explicitly per the governing prompt's own requirement even though this phase made no code changes to verify against.

## 9. Governance and closure

No production code, tests, catalog authoring, or migration performed (evidence/decision phase only). `GEN.6`'s report text is not deleted or rewritten (per instruction) — this phase's own report supersedes only its *reasoning scope* for the specific new SECONDARY_CONTROLLED framing, leaving `GEN.6`'s original K=2-reject reasoning intact as the primary, still-valid basis, now reinforced by this phase's own independent findings. Beginner×6D/7D (`GEN.6`, unrelated to this phase's SECONDARY_CONTROLLED question), Beginner×3D (`GEN.21`, independent), Beginner×4D (§8), Intermediate axis, Advanced axis, and every 2D cell are all confirmed unaffected.

**`BEGINNER_FREQUENCY_AUTHORITY_REAFFIRMED_UNDER_NEW_PRODUCT_FRAMING`.** Beginner support remains 4D-only.
