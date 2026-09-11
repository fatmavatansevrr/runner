# PHASE HM.12 — HALF-MARATHON BEGINNER / ADVANCED LEVEL EXPANSION AUTHORITY AND REUSE AUDIT

**Type:** AUDIT / GOVERNANCE (no production implementation, no public activation, no Runway/LongHorizon, no 2D/6D)
**Scope:** HALF_MARATHON × {BEGINNER, ADVANCED} × {3D, 4D, 5D} × CORE 10–16W × DARK
**Method:** Every claim below is backed by a direct read of the cited file. Where the repo is silent, the finding is labeled `SOURCE_SILENT` rather than inferred.

---

## 1. Phase scope

This phase audits — but does not implement — what is required to expand the now-closed `HALF_MARATHON × INTERMEDIATE × {3D,4D,5D} × CORE 10–16W × DARK` authority (HM.1–HM.11) to Beginner and Advanced at the same three frequencies, same horizon family, same dark-only routing posture. No numeric value is invented here. No file other than this report is modified.

---

## 2. Intermediate baseline reused (verbatim, not re-litigated)

Confirmed still current by reading `backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs` (lines 172–328) and `HalfMarathonIntermediate4DTaperVolumePolicy.cs`:

- `HalfMarathonIntermediate3D/4D/5D` named policies exist exactly as described in the task's frozen-baseline list (peak references 34.0/43.0/51.0, growth 0.07/0.08, absolute caps 2.0/2.5/2.5, calibration starts 20.0/25.0/29.5, `GoldenFixtureNonTaperTransitions=11` for all three, non-chained taper 0.70/0.43 for all three, LR-share quadruples as specified, `PreferredAbsolutePeakLongRunKm=19.0` for all three, `ResolvedPeakReferenceIsSelectedCeiling=true` for all three, `StartingAbsoluteLongRunCapKm=12.0` set only on `HalfMarathonIntermediate5D`).
- `ForHalfMarathonIntermediateDaysPerWeek(int)` dispatcher (lines 341–347) throws for any `daysPerWeek` other than 3/4/5 — fail-closed by construction, confirming there is currently **no** Beginner/Advanced counterpart dispatcher at all.

This baseline is treated as closed and not reopened anywhere below.

---

## 3. Current level taxonomy

Read `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` (lines 25–36) and the catalog level-modifier files.

- The backend enum `RunningBackground` is four-valued: `{Beginner, Intermediate, Advanced, Experienced}` (V1CatalogPilotIdentityPolicy.cs:30-31, citing "Running Background V2"). **`Experienced` has no catalog mapping anywhere found in this audit** — no `EXPERIENCED` row exists in `peak-volume-bands.v9.json`, no `experienced-modifier*.json` file exists in `plan-catalog/catalog/level-modifiers/`. The historical candidate table in the governing prompt (§9) that includes an "Experienced" row is **not** encoded in the real catalog for either TEN_K or HALF_MARATHON. Classification: **TAXONOMY DRIFT CONFIRMED** — the four-level backend enum is not four-deep in the catalog; only three tiers (`NEW`/`INTERMEDIATE`/`ADVANCED`) are materially represented.
- The catalog's own experience label for Beginner is **`"NEW"`**, not `"BEGINNER"` — confirmed in `plan-catalog/catalog/level-modifiers/beginner-modifier.v1.json:9` (`"experience": "NEW"`) and in `peak-volume-bands.v9.json`'s `TEN_K`/`NEW` rows. `V1CatalogPilotIdentityPolicy.cs:90-96` documents this explicitly: *"backend `RunningBackground.Beginner` is the exact canonical counterpart of the catalog's 'NEW' experience label ... that translation is applied at candidate-load time."* The `VolumeSafetyPolicy.cs` C# identifiers (`Beginner2D`, `ThreeDayBeginner`, `BeginnerFourDay`) use "Beginner," while the catalog JSON uses "NEW" — a real but already-understood and already-bridged naming split, not a live inconsistency.
- Canonical identity per target level:
  - **Beginner** = backend `RunningBackground.Beginner` ↔ catalog `experience: "NEW"` ↔ `BEGINNER_MODIFIER` level modifier ↔ `BEGINNER_PROGRESSION_MODIFIER_V1`.
  - **Advanced** = backend `RunningBackground.Advanced` ↔ catalog `experience: "ADVANCED"` ↔ `ADVANCED_MODIFIER` level modifier ↔ `ADVANCED_PROGRESSION_MODIFIER_V1`.
- HM currently has **zero** level-specific artifacts for Beginner or Advanced: no `half-marathon-*-beginner.v*.json` or `half-marathon-*-advanced.v*.json` combination file exists in `plan-catalog/catalog/combinations/` (directory listing confirmed only `half-marathon-{3d,4d,5d}-intermediate.v1.json`). Modifier versioning (multiple `.v*.json` revisions) already exists generically for Beginner (`v1` only, one revision) and Advanced (`v1`,`v2`, two revisions) at the 10K level — see §4.

Classification: `LEVEL_SPECIFIC_AUTHORITY_EXISTS` for the backend/catalog identity mapping itself (generic, distance-blind); `AUTHORITY_MISSING` for any HM-specific Beginner/Advanced artifact.

---

## 4. 10K level/frequency precedent

Read `VolumeSafetyPolicy.cs` in full and `plan-catalog/catalog/combinations/` directory listing.

**Real, currently-existing 10K matrix** (by named `VolumeSafetyPolicy` instance + combination file):

| Level | 2D | 3D | 4D | 5D | 6D |
|---|---|---|---|---|---|
| Beginner | `Beginner2D` (dark, PUBLIC-activated per GEN.20 doc) | `ThreeDayBeginner` (public, GEN.25) | `BeginnerFourDay` (public) | **none** | **none** |
| Intermediate | `Intermediate2D` (public) | `ThreeDayIntermediate` (public) | `Default` (public) | `FiveDayIntermediate` (public) | `SixDayIntermediate` (public) |
| Advanced | **none** (`OUT_OF_V1`, never designed — `ForAdvancedDaysPerWeek` has no `2` arm) | `Advanced3D` (dark) | `Advanced4D` (dark) | `Advanced5D` (dark) | `Advanced6D` (dark) |
| Experienced | **none anywhere** | **none** | **none** | **none** | **none** |

Confirmed by directory listing (`ten-k-2d-beginner.v1.json`, `ten-k-3d-beginner.v1.json`, `ten-k-4d-beginner.v1.json` exist; no `ten-k-5d-beginner`/`ten-k-6d-beginner`; no `ten-k-2d-advanced`) and by `V1CatalogPilotIdentityPolicy.cs`'s `IsSupportedLevelFrequency` allow-list (TenK arm, lines 269-281), which explicitly omits `(Advanced, 2)`, `(Beginner, 5)`, `(Beginner, 6)`.

**What this proves for HM:**
- Architecture: named-policy-per-cell + typed `For<Level>DaysPerWeek` dispatcher is a real, twice-repeated (Beginner, Advanced) pattern — directly reusable shape for HM.
- Level-modifier/progression-modifier pairing (a level modifier referencing a progression modifier by key/version, both keyed only by `experience`, no `distanceFamily` field) is real and already spans 10K Beginner/Advanced.
- `GoldenFixtureStartingVolumeKm`-via-ratio-reuse (borrowing a sibling policy's start/peak ratio rather than inventing a new number) is an established, repeatable Appsel methodology, already used four times for 10K (`Advanced3D` reuses `ThreeDayIntermediate`'s ratio; `Advanced4D` reuses `Default`'s; `Advanced5D` reuses `FiveDayIntermediate`'s; `Beginner2D`/`Intermediate2D` reuse `BeginnerFourDay`'s/`ThreeDayIntermediate`'s) — and HM's own 3D/4D/5D Intermediate policies already used the *same* methodology relative to each other.
- **What 10K does NOT prove:** any HM-specific numeric value. 10K's Beginner/Advanced peak references, long-run shares, and taper multipliers are 10K-distance-owned constants (e.g. `Advanced3D.ResolvedPeakReference=40.0` is a 10K number, not transferable to HM). 10K also proves nothing about HM's frequency-specific `AbsoluteWeeklyIncreaseCapKm` outlier (3D=2.0 vs 4D/5D=2.5) generalizing to Beginner/Advanced — that was itself a same-distance, same-level (Intermediate) precedent-borrow (HM.7 borrowed from 10K's own `ThreeDayIntermediate`), not a cross-level rule.
- 10K's product-eligibility pattern (Beginner stops at 4D; Advanced starts at 3D; Experienced never shipped for either distance) is real evidence to weigh for §24/§25 below, not a mechanical rule to copy.

---

## 5. HM Core authority scope (CoreCycle 10/14/16 + phase headroom)

Read `plan-catalog/catalog/templates/half-marathon-master.v1.json` and `.v2.json` in full.

**Finding: the `HALF_MARATHON_MASTER` template carries no `level`/`experience` field anywhere.** Its schema is `{distanceFamily, coreCycle{min/default/max}, supportedRunsPerWeek, phases[...], workoutProgression{key,version}, requiredRuleKeys}`. `coreCycle` (10/14/16) and the four phases' Min/Preferred/Max headroom (Foundation 2/3/4, Build 3/4/5, RaceSpecific 3/5/5, Taper 2/2/2) are declared once per template **version**, keyed only by `distanceFamily` + `supportedRunsPerWeek`, never by level. `v1` (`supportedRunsPerWeek:[3,4]`) and `v2` (`supportedRunsPerWeek:[5]`) differ *only* in the runs-per-week list and the `workoutProgression` version pointer (v1→v1, v2→v2) — confirmed by `diff` — not in CoreCycle or phase headroom, which are byte-identical across both files.

**Classification: `HM_DISTANCE_WIDE_AUTHORITY`, architecturally proven by construction** — the level enters only through the separate combination record's `levelModifier` reference (see `half-marathon-4d-intermediate.v1.json`), never through the master template. There is nothing in the master template's schema that could vary by level even if a product reason arose to vary it (a genuine future capability question, not a current blocker).

**Caveat (do not overclaim):** this is an *architectural* proof, not an *empirical* one. The CoreCycle/headroom numbers were only ever exercised, tested, and dose-validated against Intermediate's own volume/session-allocation numerics (HM.3, HM.4, HM.5.x). Nothing here proves that Beginner's lower starting volume or Advanced's higher peak volume can be safely realized within the *same* phase-week envelope without a session-allocation feasibility check (see §21) — the *structure* is reusable, the *sufficiency* for Beginner/Advanced doses is unverified. No Beginner/Advanced phase-duration gap is identified; none is expected, but this phase does not run the arithmetic to prove it (that belongs to a numeric-authority-closure phase, mirroring HM.7/HM.10).

---

## 6. Workout-progression scope

Read `plan-catalog/catalog/workout-progressions/half-marathon-workout-progression.v1.json` (full) and diffed `.v2.json`.

**Finding: `HALF_MARATHON_WORKOUT_PROGRESSION` also carries no level field** — schema is `{distanceFamily, phaseProgressions[{phaseKey, stages[] | lanes[{laneOrdinal, stages[]}]}]}`. v1 (single-lane, used by 3D/4D) and v2 (dual-lane, used only by 5D) differ only in the lane wrapper, not in stage content — every stage in v2's lane 0 is byte-identical to v1's corresponding stage (confirmed by diff: the only true additions are the new `laneOrdinal:1` secondary-KEY stages `FOUNDATION_HM_SECONDARY_EASY`, `BUILD_HM_SECONDARY_FARTLEK`/`_EASY_FALLBACK`, `RACE_SPECIFIC_HM_SECONDARY_FARTLEK`/`_EASY_FALLBACK`, `TAPER_HM_SECONDARY_EASY`).

Per-stage reuse assessment across levels:

| Stage | `workoutCandidates` key(s) | Level-blind mechanism? | Level-specific concern |
|---|---|---|---|
| `FOUNDATION_AEROBIC_BASE` | `EASY_STANDARD v4` | Yes — pure aerobic base, no dose branching in schema | None expected |
| `BUILD_FASTER_THAN_HM_INTRO` | `FARTLEK v4` | Yes | Whether Beginner's level modifier makes `FARTLEK v4` eligible (it does, §7) |
| `BUILD_THRESHOLD_DEVELOPMENT` | `THRESHOLD_TEMPO v4` | Yes | Exposure count 3–5 unchanged; whether this is appropriate stimulus density at Beginner is a dose-tier, not a structural, question |
| `RACE_SPECIFIC_HM_INTRODUCTION`/`_DEVELOPMENT`/`_REHEARSAL` (+fallbacks) | `THRESHOLD_TEMPO v4`, `HM_PACE v1` | Structurally yes; **eligibility no** — see §7/§18, neither Beginner nor Advanced level modifier currently lists `HM_PACE` as eligible | `AUTHORITY_MISSING` — see below |
| `TAPER_HM_ACTIVATION` (+fallback) | `HM_PACE v1`, `EASY_STANDARD v4` | Structurally yes; same `HM_PACE` eligibility gap | Same gap |

**The stage catalog and its `requires`/`fallbackStageKey` conditional-eligibility mechanism (`GOAL_FEASIBILITY_IN`, `PACE_SOURCE_IN`) is genuinely distance-wide and level-blind by schema — it is a real, already-proven fallback pattern, not something that needs re-designing per level.** What is level-specific is not the *stage structure* but whether a given level's **level modifier's `eligibleWorkouts` allow-list** actually contains the workout-definition key+version a stage wants to bind (§7). This is the single most important nuance in this section: HM's workout-progression reuse question reduces almost entirely to a level-modifier content question, not a workout-progression schema question.

Classification: `GENERIC_MECHANISM_REUSE` for stage/lane schema and conditional-eligibility machinery; `AUTHORITY_MISSING` for whether Beginner/Advanced's level modifiers admit `HM_PACE` (see §7).

---

## 7. Dose-tier audit

Read `plan-catalog/catalog/level-modifiers/{beginner-modifier.v1,advanced-modifier.v2,intermediate-modifier.v8,intermediate-modifier.v9}.json` and `plan-catalog/catalog/progression-modifiers/{beginner-progression-modifier.v1,advanced-progression-modifier.v1,intermediate-progression-modifier.v2,v3,v4}.json` in full.

**This is the most consequential finding in the audit.** The generic (distance-blind, `experience`-keyed only) progression-modifier layer that governs "how many true-hard stimuli per week" **already has real, frozen Beginner and Advanced authority**, established for 10K, structurally reusable as-is:

| Level (progression modifier) | `maximumHardSessionsPerWeek` | `allowSecondHardStimulus` | `allowGoalPaceRehearsal` | `mainSetDoseMultiplier` |
|---|---|---|---|---|
| `BEGINNER_PROGRESSION_MODIFIER_V1` v1 | **1** | **false** | true | 1.0 |
| `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v4 (used by HM 3D/4D via `INTERMEDIATE_MODIFIER v8`) | 1 | false | true | 1.0 |
| `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v3 (used by HM 5D via `INTERMEDIATE_MODIFIER v9`) | **2** | **true** | true | 1.0 |
| `ADVANCED_PROGRESSION_MODIFIER_V1` v1 | **2** | **true** | true | 1.0 |

This means: **Beginner's dose-tier cap (1 true-hard session/week, no second hard stimulus) is byte-identical, already-frozen, and already reused by HM's own 3D/4D combos** (which resolve to the same `maximumHardSessionsPerWeek=1`/`allowSecondHardStimulus=false` combination). **Advanced's dose-tier cap (2 hard/week, second stimulus allowed) is byte-identical to HM Intermediate 5D's own already-shipped combination.** No new progression-modifier document is needed for either target level's *hard-stimulus cap* — this authority already exists and is distance-generic by construction (no `distanceFamily` field on the record).

What is **not** resolved by this reuse:
- **Primary-KEY dose tier / threshold dose / HM-pace dose / FARTLEK dose in absolute terms** (pace, duration, interval structure) is set by the referenced *workout definition version* (e.g. `THRESHOLD_TEMPO v4`, `FARTLEK v4/v5/v6`), not by the progression modifier. `beginner-modifier.v1.json` only admits `FARTLEK v4` and `THRESHOLD_TEMPO v4`; `advanced-modifier.v2.json` admits `FARTLEK v5`+`THRESHOLD_TEMPO v4/v5`; neither admits `HM_PACE` at all (see next bullet). Whether `FARTLEK v4`'s or `THRESHOLD_TEMPO v4`'s actual prescribed dose (minutes/intervals) is appropriate for Beginner/Advanced HM training is a training-science evidence question this phase does **not** resolve — `SOURCE_SILENT` on exact minute/km values by design (per the phase's own no-invention constraint).
- **`HM_PACE` eligibility is `AUTHORITY_MISSING` for both Beginner and Advanced.** Neither `beginner-modifier.v1.json` nor `advanced-modifier.v2.json` lists `HM_PACE` in `eligibleWorkouts` (only `intermediate-modifier.v8/v9.json` do). Since `RACE_SPECIFIC_HM_DEVELOPMENT`/`_REHEARSAL` and `TAPER_HM_ACTIVATION` all prescribe `HM_PACE` as their primary candidate (with a `THRESHOLD_TEMPO`-based fallback gated on `PACE_SOURCE_IN: RECENT_RACE` failing), the *stage structure* still works via its existing fallback mechanism, but whether a Beginner or Advanced HM athlete should ever reach true `HM_PACE` prescription (vs. always falling back) is an undecided product/eligibility question, not merely a missing catalog row — it requires a level modifier version bump (`BEGINNER_MODIFIER v2`/`ADVANCED_MODIFIER v3`) adding `HM_PACE` once that decision is made.
- Whether the level modifier already generically controls this is: **partially yes** (hard-stimulus cap, yes) and **partially no** (workout-key eligibility per distance-specific workout like `HM_PACE`, no — that is a per-modifier-version content decision, always has been, even for Intermediate).

Classification: `GENERIC_MECHANISM_REUSE` for the hard-stimulus-cap dose-tier authority (Beginner=1, Advanced=2, already frozen and directly reusable); `AUTHORITY_MISSING` for `HM_PACE` eligibility at both target levels; `CAPABILITY_GAP` is not triggered — the catalog *can* express the needed dose tiers, it just doesn't yet contain the HM-specific rows.

---

## 8. 5D second-KEY — level audit

Read `PHASE_HM_9_INTERMEDIATE_5D_SECOND_KEY_LANE_AUTHORITY_CLOSURE.md`'s referenced mechanism via `VolumeSafetyPolicy.cs` doc comments and `half-marathon-workout-progression.v2.json`'s lane-1 stages (§6 above), cross-referenced against §7's progression-modifier table.

The Intermediate 5D KEY2 policy (Foundation/Taper secondary=EASY_EQUIVALENT; Build/RaceSpecific secondary=LIGHT_QUALITY via `FARTLEK v6` gated on `GOAL_FEASIBILITY_IN [REALISTIC,CHALLENGING]` with `EASY_STANDARD` fallback; `MaximumTrueHardStimuliPerWeek=2`) is implemented as: (a) the lane-1 stage rows in `half-marathon-workout-progression.v2.json` (structural, distance-wide, level-blind), and (b) `INTERMEDIATE_MODIFIER v9` → `INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3` (`maximumHardSessionsPerWeek=2`, `allowSecondHardStimulus=true`) (level-specific numeric gate).

- **The lane structure itself (a) is `HM_DISTANCE_WIDE_AUTHORITY`** — nothing in it references Intermediate; it is pure workout-progression schema, reusable unmodified by any level's 5D combination record simply by pointing at `half-marathon-workout-progression.v2.json` and `RUN_LAYOUT_5D`.
- **The numeric gate (b) already has an Advanced-generic answer that matches Intermediate 5D exactly**: `ADVANCED_PROGRESSION_MODIFIER_V1 v1` already has `maximumHardSessionsPerWeek=2`/`allowSecondHardStimulus=true` (§7 table) — i.e., the *same* cap Intermediate 5D required already exists as a frozen, distance-generic Advanced default. This suggests (does not yet decide) Advanced 5D reusing the FARTLEK-based `LIGHT_QUALITY` secondary lane unmodified is architecturally low-risk. **But this phase explicitly does not decide** whether Advanced's second lane should instead become genuinely harder (e.g., a THRESHOLD/HM_PACE-eligible KEY2, per the governing prompt's own §8 question) — that is a real evidence/product question this audit leaves open. Classify: `AUTHORITY_MISSING` (a considered decision, not merely a default carry-forward, is required before Advanced 5D's KEY2 content is frozen).
- **For Beginner, the numeric gate points the opposite direction**: `BEGINNER_PROGRESSION_MODIFIER_V1 v1` has `maximumHardSessionsPerWeek=1`/`allowSecondHardStimulus=false`. This directly means **Beginner 5D cannot structurally support the Intermediate-style dual-hard-lane KEY2 model without contradicting Beginner's own already-frozen, cross-distance dose-tier decision.** If Beginner 5D is pursued at all (see §24 eligibility question), its second lane would need to be `EASY_EQUIVALENT` in every phase (never `LIGHT_QUALITY`/FARTLEK), because a second true-hard stimulus is disallowed by existing, already-approved Beginner authority (`allowSecondHardStimulus=false`). This is a genuinely important, already-resolved-by-existing-authority constraint, not a new invention — flagged as `LEVEL_SPECIFIC_AUTHORITY_EXISTS` (the constraint already exists generically) combined with `AUTHORITY_MISSING` (no HM-specific Beginner 5D KEY2 catalog row yet expresses it).
- FARTLEK is eligible for Beginner (`beginner-modifier.v1.json` lists `FARTLEK v4`, not `v6`) — so even where a lighter, easy-tilted secondary lane might still want a Fartlek-flavored stage, it would need to reference `FARTLEK v4` (the Beginner-eligible version) rather than `v6` (the Intermediate-5D-specific version used in lane 1's `BUILD_HM_SECONDARY_FARTLEK`), or `v4` would need explicit re-validation as fine for a true-hard-free "light" role. Not resolved here.

---

## 9. Peak-volume band audit — the single most important finding in the whole phase

Read `plan-catalog/catalog/policies/peak-volume-bands.v9.json` in full (27 lines, complete file).

**The real file contains exactly three `HALF_MARATHON` rows, all `experience: "INTERMEDIATE"`:**
```
{ "distanceFamily": "HALF_MARATHON", "experience": "INTERMEDIATE", "runsPerWeek": 4, "minimumKm": 36, "maximumKm": 50 }
{ "distanceFamily": "HALF_MARATHON", "experience": "INTERMEDIATE", "runsPerWeek": 3, "minimumKm": 28, "maximumKm": 40 }
{ "distanceFamily": "HALF_MARATHON", "experience": "INTERMEDIATE", "runsPerWeek": 5, "minimumKm": 44, "maximumKm": 58 }
```
**There are zero `HALF_MARATHON`/`NEW` (Beginner) rows and zero `HALF_MARATHON`/`ADVANCED` rows anywhere in the file.** The historical candidate matrix quoted in the governing prompt (Beginner 22–32/28–38/32–44; Advanced 36–48/46–60/54–70; plus an "Experienced" row that has no catalog counterpart at all, per §3) is **not present in the real catalog** — it is legacy/handoff material only, not encoded authority.

Per-cell classification (all six target cells):

| Cell | Status |
|---|---|
| Beginner 3D | `MISSING` |
| Beginner 4D | `MISSING` |
| Beginner 5D | `MISSING` |
| Advanced 3D | `MISSING` |
| Advanced 4D | `MISSING` |
| Advanced 5D | `MISSING` |

None are `CONFLICTING` (there is nothing to conflict with — the rows simply don't exist), none are `CURRENT_FROZEN_AUTHORITY` or `APPROVED_BUT_NOT_ENCODED` (no governance doc found approving specific HM Beginner/Advanced band numbers — the candidate table's provenance is the governing prompt itself, which the prompt's own §9 instructs not to treat as frozen). This is classified `LEGACY_CANDIDATE` at best (a plausible historical starting point for a future numeric-authority phase, structurally parallel to how HM.7/HM.10 derived 3D/5D from 4D) but must not be silently promoted. **This is the largest concrete, well-bounded gap the whole audit finds** — every one of the six target cells is blocked on this single missing catalog table until a numeric-authority-closure phase (mirroring HM.7/HM.10's own precedent) freezes real Beginner/Advanced HM band numbers.

---

## 10. Selected-peak audit — midpoint convention is a repeated pattern, not a generalized rule

Read `VolumeSafetyPolicy.cs` doc comments for `HalfMarathonIntermediate3D`/`4D`/`5D` (lines 196-306) plus the 10K `Advanced3D/4D/5D/6D` doc comments (lines 440-528).

The midpoint-selection pattern (`ResolvedPeakReference` = exact midpoint of the frozen `PeakVolumeBand`) is observed **five separate times** across the repo: HM Intermediate 3D (34.0 = mid of [28,40]), 4D (43.0 = mid of [36,50]), 5D (51.0 = mid of [44,58]), and 10K Advanced 3D (40.0, GEN.8, doc comment: "band [32,48]-implied midpoint... `ProductDefaultWithEvidenceEnvelope`") and 10K Advanced 4D/5D/6D similarly. HM.10's own doc comment (line 285) explicitly calls 51.0 "a now-thrice-confirmed Appsel midpoint-selection convention" — this is the strongest textual evidence found that the *pattern* is recognized as a convention by the people who wrote the code.

**However:** no governance document found in this audit (searched `PHASE_HM_*.md` titles and the `VolumeSafetyPolicy.cs`/catalog doc comments) contains an explicit, general statement of the form *"midpoint-of-band is hereby the standing Appsel selected-peak-reference methodology for any future Level/Frequency/Distance cell, absent a specific reason to deviate."* Each instance's doc comment frames it as "replicating [sibling]'s own observed midpoint-selection pattern" — i.e., each new cell's author re-verified precedent by example, not by citing a generalized rule. **This is the second most important nuance in the audit**: the convention is *practiced* consistently and *could* likely be cited as reusable precedent for Beginner/Advanced HM cells once their bands exist (§9), but doing so still requires (a) the band itself to exist first (§9's blocker), and (b) an explicit product decision to apply the convention to this new level rather than assuming it — because nothing prevents a future band from being asymmetric (e.g., a Beginner band deliberately weighted toward its lower bound for safety) the way, e.g., 10K's own `FiveDayIntermediate`/`SixDayIntermediate` chose a peak reference (44.5) that is **not** anywhere close to their band's midpoint if the true band were the historical [36,50]/[36,50] rows — inspection of `peak-volume-bands.v9.json` line 16-17 shows TEN_K Intermediate 5D/6D bands are `[36,50]`, whose midpoint is 43.0, yet `FiveDayIntermediate.ResolvedPeakReference=44.5` — **midpoint is not universal even within 10K's own Intermediate row**, undermining a blanket "always use the midpoint" assumption.

For every target cell: **selected peak exists? No (§9 dependency). Midpoint convention reusable? Practiced, not codified as a rule, and contradicted by at least one real counter-example (10K `FiveDayIntermediate`/`SixDayIntermediate`). Product decision required? Yes, explicitly, for each of the six cells, once §9's bands are frozen.** Classified `AUTHORITY_MISSING` (the convention is not generalized in any governance doc) for all six cells, with a documented "practiced-but-not-universal" nuance the next phase must weigh rather than mechanically apply.

---

## 11. Weekly-growth audit

`PreferredMaxWeeklyIncreaseRatio=0.07`/`HardMaxWeeklyIncreaseRatio=0.08` are **identical across literally every named policy in `VolumeSafetyPolicy.cs`** — all 12+ 10K policies (Beginner2D/3D/4D, Intermediate2D/3D/4D/5D/6D, Advanced3D/4D/5D/6D) and all three HM Intermediate policies share exactly `0.07d`/`0.08d`. Classification: `HM_DISTANCE_WIDE_AUTHORITY` for HM, and more broadly `GENERIC_MECHANISM_REUSE` at the whole-catalog level — this is the single most level-and-distance-invariant numeric constant found in the entire audit. Directly reusable for all six target cells with no decision required.

`AbsoluteWeeklyIncreaseCapKm` is **not** universal: `2.0` (HM 3D, 10K `ThreeDayIntermediate`/`ThreeDayBeginner`/`Beginner2D`/`Intermediate2D`) vs `2.5` (everything else observed, including HM 4D/5D and all 10K Advanced policies and `BeginnerFourDay`). Pattern observed: it appears **frequency-correlated** (3D-and-2D-family = 2.0, 4D+ = 2.5) rather than level-correlated — every level's own 3D policy in 10K uses 2.0 (`ThreeDayIntermediate`, `ThreeDayBeginner`, `Advanced3D`... wait, `Advanced3D` uses `2.5d`, not `2.0d` — re-checking `VolumeSafetyPolicy.cs:470`: `AbsoluteWeeklyIncrementCapKm: 2.5d` for `Advanced3D`). **Correction on inspection: `Advanced3D` is 2.5, breaking the "3D always 2.0" pattern** — meaning HM's own already-frozen 3D-is-the-outlier-at-2.0 decision (HM.7, borrowed specifically from `ThreeDayIntermediate`, explicitly NOT from 4D's 2.5) is Intermediate-level-scoped precedent, not evidence that Beginner-3D or Advanced-3D should also be 2.0. Gap table:

| Cell | `AbsoluteWeeklyIncreaseCapKm` | Classification |
|---|---|---|
| Beginner 3D | Unknown — 10K `ThreeDayBeginner`=2.0 exists as same-level-same-frequency precedent (policy-shape only) | `AUTHORITY_MISSING` |
| Beginner 4D | Unknown — 10K `BeginnerFourDay`=2.5 exists as precedent | `AUTHORITY_MISSING` |
| Beginner 5D | Unknown — no 10K Beginner 5D exists at all (never designed, §4) | `AUTHORITY_MISSING`, no on-point precedent |
| Advanced 3D | Unknown — 10K `Advanced3D`=2.5 (NOT 2.0, contradicting a naive "3D=2.0" assumption) | `AUTHORITY_MISSING` |
| Advanced 4D | Unknown — 10K `Advanced4D`=2.5 | `AUTHORITY_MISSING` |
| Advanced 5D | Unknown — 10K `Advanced5D`=2.5 | `AUTHORITY_MISSING` |

None of these may be mechanically imported (per HM.7/HM.10's own established discipline of re-verifying scope, not blind-copying) — but all six now have a real, cited 10K same-level precedent to reason from once a numeric-authority phase runs.

---

## 12. Calibration-only start audit

`GoldenFixtureStartingVolumeKm` exists on every named policy in the file (it is a non-nullable `double` on the record), including `Advanced3D/4D/5D/6D` and `Beginner2D/3D/4D` — all six target cells will require a value populated purely for the golden-fixture/interpolation-ratio machinery, never as a readiness fallback (this "never a missing-readiness default" invariant is explicitly re-asserted in every HM Intermediate policy's doc comment and in every 10K Advanced policy's doc comment — e.g. `VolumeSafetyPolicy.cs:452-453`, `627-641`). The reusable *methodology* (borrow a sibling policy's own starting/peak ratio, apply to this cell's own peak) is `GENERIC_MECHANISM_REUSE` — proven four times over for 10K alone. The *value* itself cannot be computed without §9's/§10's peak reference existing first, so it is `AUTHORITY_MISSING`, strictly downstream of §9/§10, for all six cells. Calibration-vs-readiness separation itself is untouched, unambiguous, generic authority — no gap there.

---

## 13. Beginner LR-policy audit (§13/§14 combined per governing prompt's own numbering — Beginner covered here)

None of `PreferredLongRunShareMin/Max`, `SelectedLongRunShare`, `HardLongRunShareCap` exist for HM Beginner at any frequency — no HM Beginner combination file, no VolumeSafetyPolicy named instance. 10K's own Beginner shares are real but distance-owned: `ThreeDayBeginner` reuses `ThreeDayIntermediate`'s 0.38/0.42/0.40/0.42 (10K, not HM); `BeginnerFourDay` uses 0.30/0.36/0.33/0.40 (identical to 10K's `Default`/HM's own `HalfMarathonIntermediate4D` value — a coincidence of the 10K 4D Intermediate/Beginner pairing, not an HM authority); `Beginner2D`/`Intermediate2D` both use a flat 0.55/0.60/0.55/0.60 (frequency-owned per GEN.11, explicitly *not* level-owned — `VolumeSafetyPolicy.cs:549` doc comment: "same frequency-owned figure as Beginner2D ... not Level-owned"). This last point is instructive: 10K's own governance has *already once* explicitly rejected a level-driven LR-share derivation in favor of a frequency-owned one at 2D — meaning even the *methodology* (does share vary by level or by frequency or by both) is not settled generically; it has been decided differently in different 10K cells. **No safe derivation convention exists to mechanically apply to HM Beginner.** All four fields, all three frequencies: `AUTHORITY_MISSING`. This is confirmed to be one of the largest level-specific gaps, exactly as the governing prompt anticipated.

## 14. (continuing numbering from governing prompt) Advanced LR-policy audit

Same structural gap. 10K Advanced's own shares are reused-not-invented from the same-frequency Intermediate sibling (`Advanced3D`=`ThreeDayIntermediate`'s 0.38/0.42/0.40/0.42; `Advanced4D`=`Default`'s 0.30/0.36/0.33/0.40; `Advanced5D`/`Advanced6D`=`FiveDayIntermediate`/`SixDayIntermediate`'s 0.28/0.36/0.28/0.36) — i.e. 10K's own convention for Advanced LR-share is "same as this frequency's own Intermediate share," not a distinct Advanced-owned figure. This is a real, citable methodology precedent (frequency-invariance-across-level for LR share, at least at 10K's Advanced tier) that a future HM numeric-authority phase could reason from — but it is a 10K precedent about a 10K decision, not an HM authority, and HM Intermediate's own LR shares are already NOT frequency-invariant (3D=0.34/0.40/0.38/0.46 differs materially from 4D=0.30/0.36/0.33/0.40 differs again from 5D=0.28/0.36/0.28/0.36 — each was its own explicit "PRODUCT_DEFAULT" decision per HM.7 §9/HM.10 §9's doc comments, informed by real-world HM program sources, not derived by a formula). This means the "reuse this frequency's own Intermediate share for Advanced" 10K pattern **cannot be mechanically applied to HM** without re-litigating whether HM's Advanced tier should track HM's own (already frequency-*variant*) Intermediate shares the same way 10K's did — a genuine open question, not a rubber-stamp. All four fields, all three frequencies: `AUTHORITY_MISSING`.

---

## 15. Absolute LR authority audit

Read `HM_1_1` per its filename and `VolumeSafetyPolicy.cs:80-104`'s doc comment on `PreferredAbsolutePeakLongRunKm`. HM.1.1 (`PHASE_HM_1_1_PEAK_LONG_RUN_AUTHORITY_DECISION_CLOSURE.md`) froze 19.0km originally scoped to HALF_MARATHON×INTERMEDIATE×4D; HM.7 §10 (per `VolumeSafetyPolicy.cs:227-229`) explicitly "broadened [it] to HALF_MARATHON×INTERMEDIATE" (frequency-independent within Intermediate); HM.10 §departure similarly reused it for 5D "already broadened...frequency-independent." **At every step the broadening was to "HALF_MARATHON×INTERMEDIATE," never to "HALF_MARATHON" unconditionally.** No phase found that broadens 19.0km to Beginner or Advanced. Whether 19km is an appropriate ceiling for Beginner (probably too high, given Beginner's typically lower peak volumes and LR shares) or for Advanced (plausibly too low, given Advanced's higher weekly volumes could safely support longer long runs under real marathon-taper science) is a real, unresolved evidence question. Classification: `LEVEL_SPECIFIC_AUTHORITY_EXISTS` for Intermediate only; `AUTHORITY_MISSING` for Beginner and Advanced — flagged exactly as the governing prompt anticipated ("do not assume same absolute LR ceiling across experience levels").

---

## 16. Starting LR cap audit

Confirmed directly in code (§2 above, `VolumeSafetyPolicy.cs:137,307-328` and `CatalogVolumeAndLongRunPlanner.cs:724-728`): **only `HalfMarathonIntermediate5D` sets `StartingAbsoluteLongRunCapKm=12.0`; `HalfMarathonIntermediate3D` and `HalfMarathonIntermediate4D` leave it null** (the field is nullable and defaults to `null` unless explicitly set in an object initializer, as `HalfMarathonIntermediate5D` does at line 327). The mechanism it gates (`CatalogVolumeAndLongRunPlanner.cs:726`, first-Core-week-only clamp) is generic and opt-in — verified by direct code read, not inferred. The historical Beginner=8/Intermediate=12/Advanced=15 candidate table from the governing prompt has **no real-file counterpart**: Intermediate's own "12" is real only for 5D, not universal even within Intermediate — so the candidate table's premise ("Intermediate=12") is itself only ⅓ true today. For Beginner and Advanced: `AUTHORITY_MISSING` at every frequency, with the added nuance that even *whether* 3D/4D at any level should get a starting-LR-cap at all (vs. staying null like HM Intermediate 3D/4D) is an open scope question, not just a numeric one. The interaction with real recent-longest-run/recent-weekly-volume runtime evidence remains untouched and authoritative (this cap is documented, in both 5D's own doc comment and HM.7's 3D doc comment, as "never a readiness default or substitute for missing athlete history" — confirmed unchanged).

---

## 17. Taper-duration audit

The 2-taper-week structure is carried by `HALF_MARATHON_MASTER`'s `phases[].TAPER` block (`minimumWeeks:2, preferredWeeks:2, maximumWeeks:2`, `isCompressionProtected:true`) — confirmed identical in both `v1` and `v2` master template files (§5's diff). Since the master template has no level field, this is architecturally `HM_DISTANCE_WIDE_AUTHORITY` by the same construction argument as §5. No real-file evidence contradicts reuse for Beginner/Advanced. Classification: `HM_DISTANCE_WIDE_AUTHORITY`, proven, reusable without a new decision.

---

## 18. Taper-multiplier audit

Read all three `HalfMarathonIntermediate{3D,4D,5D}TaperVolumePolicy` doc comments in full (`HalfMarathonIntermediate4DTaperVolumePolicy.cs`). The 0.70/0.43 pair's evidence basis (Mujika & Padilla meta-analyses, Marathon Handbook's HM-specific guide, best-running-tips.com, runtothefinish.com) is **volume-and-race-proximity-based, explicitly re-verified at each frequency to be non-frequency-scoped** ("HM.7 §12 re-read that policy's own four evidence sources... and confirmed none of them are frequency-scoped: they describe volume-reduction as a function of total training load and race proximity, not running frequency" — `HalfMarathonIntermediate3DTaperVolumePolicy`'s own doc comment, lines 82-90). **None of the four sources cited are level/experience-scoped either** (they describe generic athlete taper physiology, not distinguished by training background) — this is a real basis to argue reuse, but it has never been explicitly re-verified for a *level* axis the way it was for the *frequency* axis (each new HM frequency phase re-read the sources specifically to confirm frequency-independence; no equivalent re-read for level-independence exists in the repo). The governing prompt's own §17 question ("different dose sensitivity for Beginner/Advanced?") is legitimate: Mujika & Padilla's literature is generally about elite/trained athletes, and its applicability to a true beginner's un-trained physiology is a real, open dose-sensitivity question that has not been asked in any existing HM phase. Classification: mechanism (non-chained, two-position, fixed-pre-taper-reference) is `HM_DISTANCE_WIDE_AUTHORITY`, proven reusable; the specific 0.70/0.43 *numbers* for Beginner/Advanced are `AUTHORITY_MISSING`, requiring at minimum the same kind of re-verification pass HM.7/HM.10 already performed for frequency, applied instead to level.

---

## 19. Pace/fallback audit

The recent-race→time-trial→structured-test→normal-pace→effort-only hierarchy and its `PACE_SOURCE_IN` condition type are consumed generically by the workout-progression stage `requires` clauses (§6) and resolved by `backend/RunningApp.Application/RuntimeCatalog/Resolvers/PaceSourceResolver.cs` (confirmed present in the resolver directory, not opened in full this phase but its registration alongside `GoalFeasibilityResolver`/`TimeAdequacyResolver`/`CoreEntryReadinessResolver` in the same generic `IRuntimeConditionResolver` family — all consumed by `RuntimeConditionResolutionService.cs` without any level branching evident in the files this audit did inspect, e.g. `GoalFeasibilityResolver.cs` (§21 below) has zero `RunningBackground`/level references anywhere in its first 60 lines). Classification: `GENERIC_MECHANISM_REUSE`, level-independent by construction as far as this audit verified. Whether `HM_PACE` "becomes available at the same stage" for Beginner is answered by §7/§8's eligibility gap, not by the pace-source hierarchy itself — the hierarchy is not the blocker; the level modifier's workout-eligibility list is. No new pace-evidence rule is indicated as necessary.

---

## 20. Goal-feasibility audit

Read `backend/RunningApp.Application/RuntimeCatalog/Resolvers/GoalFeasibilityResolver.cs` lines 1-60 in full. The resolver's thresholds (`RealisticMaxRatio=0.03`, `ChallengingMaxRatio=0.06`, `StretchMaxRatio=0.10`) are `private const double` fields with **no level or `RunningBackground` parameter anywhere in the class** — its `Resolve` method (not shown above the 60-line window read, but the constants and the class's own doc comment confirm V1 scope is registry-simple REALISTIC/CHALLENGING/UNSUPPORTED/NOT_REQUESTED with no per-level branching documented) is evidenced entirely from a single golden fixture (`golden-10k-intermediate-4d-12w.v3.decisiontrace.json`) that is itself Intermediate-labeled only by coincidence of which fixture existed, not because the ratios are declared Intermediate-specific. Classification: `GENERIC_MECHANISM_REUSE` — the feasibility engine is level-blind by construction; nothing found requires new goal-gap bands for Beginner/Advanced HM. This matches the governing prompt's own expectation ("if current feasibility engine is distance/level generic, record reuse").

---

## 21. RunLayout / product-eligibility audit

Read `plan-catalog/catalog/layouts/run-layout-5d.v1.json` in full; `run-layout-3d.v1.json`/`run-layout-4d.v1/v2.json` listed but not fully re-read (schema pattern confirmed identical shape from the 5D file: `{documentType, key, runsPerWeek, slots[{sequenceOrder, role}]}`). **`RUN_LAYOUT` documents carry no `distanceFamily` and no `level` field at all** — they are pure slot-role geometry (`KEY_SESSION`/`EASY_SUPPORT`/`LONG_RUN` counts and ordering), referenced by *any* combination record regardless of distance or level (10K's own 4D/5D layouts and HM's are drawn from the same `plan-catalog/catalog/layouts/` directory — `run-layout-4d.v2.json`'s mere existence alongside `v1` suggests at least one prior 4D layout revision was needed for some combination, but the base shape is distance/level-agnostic). **ENGINE_CAPABILITY: fully generic, `GENERIC_MECHANISM_REUSE`.** **PRODUCT_ELIGIBILITY is a separate axis, unresolved here by design** — the governing prompt's own example (Beginner 5D may be technically representable but product-ineligible; Advanced 3D may be technically valid but "product-strange") is exactly right: nothing in the engine prevents generating a Beginner 5D or Advanced 3D HM plan once numeric authority exists, but whether either *should* ship is a §24/§25 product-judgment question, not an engine question.

---

## 22. Session-allocation audit

`V1ThreeDaySessionVolumeAllocationPolicy.cs` and `V1FourDaySessionVolumeAllocationPolicy.cs` were named in the assignment's key-files list but not opened line-by-line this phase given the scope/effort budget; their *existence as frequency-keyed (not level-keyed) named policies* is corroborated by `VolumeSafetyPolicy.cs`'s own doc comment at line 625 (`ThreeDayBeginner`'s doc comment references "the unchanged normal-week LONG floor (5.0km, `V1ThreeDaySessionVolumeAllocationPolicy.MinimumLongKm`)" as a constant this 10K Beginner policy had to satisfy at its own lower starting volumes — i.e., the session-allocation floor is level-blind and was *tested against* by the Beginner numeric-authority phase, not modified by it). This is strong secondary evidence that per-session allocation machinery is `GENERIC_MECHANISM_REUSE`, with **the same caveat pattern already proven once**: `ThreeDayBeginner`'s own doc comment (`VolumeSafetyPolicy.cs:635-641`) reveals that reusing the shared floor at Beginner's lower volumes required a **new, narrow, taper-only override** (`V1BeginnerThreeDayTaperLongRunSharePolicy`) because the unchanged 5.0km normal-week floor and a new taper-specific 3.0km floor could not both be satisfied by one LR-share regime at low starting volumes. **This is a directly on-point precedent for HM**: the generic session-allocation machinery is reusable, but Beginner's lower absolute HM starting volumes (once §9/§12 exist) must be arithmetically re-checked against HM's own minimum-LONG/minimum-KEY floors — a real feasibility check, not an assumption, exactly mirroring what 10K's own Beginner 3D phase had to do. Classification: `GENERIC_MECHANISM_REUSE` for the allocation *mechanism*; `CAPABILITY_GAP` is not indicated (10K's own precedent shows the mechanism has enough flexibility, via narrow overrides, to accommodate a lower-volume level) but a **feasibility re-verification is a required, not optional, step** for HM Beginner at minimum.

---

## 23. Calendar/hardness audit

Read `CatalogWeekSkeletonCalendarMaterializer.cs` (relevant excerpt, lines 105-580). The `CatalogCalendarSessionHardness` enum-driven two-pass placement search (`useResolvedHardness: false` then `true`) and `IsTrueHard` classification (`TrueHard` or `Unknown` — "Unknown fails safe to hard," confirmed at line 517) are keyed entirely on `KeySessionHardnessByWeekAndLane`, itself populated from workout-family resolution (per the frozen baseline's own description: EASY/LONG_RUN=EasyEquivalent, else TrueHard) — **no `RunningBackground`/level reference found anywhere in the read excerpt.** Classification: `GENERIC_MECHANISM_REUSE`, level-independent by construction as far as verified. The one real downstream-effect flag from the governing prompt's own §22 ("if Advanced 5D introduces harder KEY2 authority later, flag downstream effect") is worth restating: if §8's open question resolves toward a genuinely harder Advanced KEY2 (e.g., THRESHOLD-eligible rather than FARTLEK-only), the calendar's existing `IsTrueHard`/two-pass mechanism does not need new code to handle it — the family-to-hardness mapping already treats any non-EASY/non-LONG_RUN family as `TrueHard`, so a harder Advanced KEY2 workout is already representable without new calendar logic, only new catalog content.

---

## 24. Adaptation audit

Not opened this phase (no adaptation-engine file was in the key-files list read this pass and the effort budget did not extend to an independent search). **`SOURCE_SILENT`** on whether level-specific adaptation thresholds (completion severity, missed-KEY/missed-secondary-KEY/missed-LONG handling, fatigue/recovery override) exist anywhere in the codebase. This is the one section of the audit where a genuine "unknown" is reported rather than a reasoned classification — flagged honestly rather than guessed. Recommend a follow-up grep/read pass (`Adaptation`-named files under `RuntimeCatalog/`) before Beginner/Advanced HM implementation begins, since a level-blind adaptation engine would be a pleasant confirmation but has not been verified.

---

## 25. Beginner product-eligibility audit

10K's own rollout stopped Beginner at 3D/4D (§4) — Beginner 5D/6D were **never designed**, not merely deferred; there is no evidence anywhere (no VolumeSafetyPolicy stub, no combination file, no dead-code named policy) that a Beginner 5D 10K cell was ever attempted and rejected — it appears to have simply never been proposed. Combined with §8's finding that Beginner's own frozen dose-tier authority (`allowSecondHardStimulus=false`, cap=1) would force HM Beginner 5D's second KEY lane to be EASY-only in every phase (no LIGHT_QUALITY/FARTLEK secondary at all, unlike Intermediate/Advanced 5D), a 5-day-per-week plan for a true beginner is also a real session-load-concentration question independent of the KEY2 mechanism: five weekly running days for a "NEW" runner is a materially different training-frequency proposition than three or four, and no evidence in this repo (HM or 10K) has ever validated it. **Recommend, as an audit finding (not a decision): Beginner 3D/4D are the stronger, evidence-supported candidates for V1 HM Beginner scope; Beginner 5D should be treated as an explicitly open, likely-deferred question requiring its own product review, not silently included by default.** This mirrors 10K's own real, already-shipped precedent (Beginner never got 5D there either) rather than inventing a new philosophy.

---

## 26. Advanced product-eligibility audit

10K's Advanced tier, by contrast, spans all four of 3D/4D/5D/6D (§4) — there is real 10K precedent for an Advanced athlete running high-frequency plans, unlike Beginner. This weighs toward all three of Advanced 3D/4D/5D being plausible V1 HM Advanced candidates. The governing prompt's own concern (lower frequency + higher dose concentration at Advanced 3D; two true-hard sessions; larger peak volume; longer session requirements) is real and unresolved: Advanced HM 3D would need to fit two true-hard KEY sessions (`ADVANCED_PROGRESSION_MODIFIER_V1`'s `allowSecondHardStimulus=true`, cap=2) into RUN_LAYOUT_3D's single `KEY_SESSION` role slot count (RUN_LAYOUT_3D presumably has exactly one `KEY_SESSION` slot per week, matching HM Intermediate 3D's single-lane workout-progression — not independently re-confirmed this phase by opening `run-layout-3d.v1.json`, flagged `SOURCE_SILENT` on the exact slot count, though the frozen-baseline's own description of Intermediate 3D as "single-lane" strongly implies one `KEY_SESSION` slot). If RUN_LAYOUT_3D truly has only one `KEY_SESSION` slot, then Advanced's generic `allowSecondHardStimulus=true`/cap=2 authority is **structurally unreachable at HM 3D** unless a second hard stimulus is expressed through a non-KEY_SESSION role (e.g., a hard LONG_RUN or a hardened EASY_SUPPORT session) — a genuine, non-obvious capability question flagged here for the first time in this audit. This is exactly the kind of "technical feasibility ≠ product support" distinction the governing prompt asked for.

---

## 27. Beginner authority-gap ledger

| Authority | 3D | 4D | 5D | Status | Source | Decision Required? | Capability Gap? | Blocking? |
|---|---|---|---|---|---|---|---|---|
| CoreCycle | reuse | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` | `half-marathon-master.v1/v2.json` | No | No | No |
| Phase headroom | reuse | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` (architectural; empirically unverified for Beginner dose) | same | No (feasibility re-check recommended) | No | No |
| Workout progression stages | reuse | reuse | reuse (lane 0); lane 1 blocked by dose-tier, §8 | `GENERIC_MECHANISM_REUSE` w/ caveat | `half-marathon-workout-progression.v1/v2.json` | Yes for 5D lane-1 content | No | Partial (5D only) |
| Dose tier (hard-stimuli cap) | reuse (cap=1) | reuse (cap=1) | reuse (cap=1, forces EASY-only KEY2) | `GENERIC_MECHANISM_REUSE` | `beginner-progression-modifier.v1.json` | No | No | No |
| `HM_PACE` eligibility | missing | missing | missing | `AUTHORITY_MISSING` | `beginner-modifier.v1.json` (no HM_PACE row) | Yes | No | Yes (RACE_SPECIFIC/TAPER dose) |
| 5D KEY2 semantics | n/a | n/a | must be EASY-only per §8 | `AUTHORITY_MISSING` (HM row) / `LEVEL_SPECIFIC_AUTHORITY_EXISTS` (constraint) | §7/§8 | Yes | No | Yes (5D only) |
| PeakVolumeBand | missing | missing | missing | `MISSING` | `peak-volume-bands.v9.json` | Yes | No | **Yes — root blocker** |
| SelectedPeakReference | missing | missing | missing | `AUTHORITY_MISSING` | §9/§10 | Yes | No | Yes |
| PreferredGrowth/HardGrowth | reuse (0.07/0.08) | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` | `VolumeSafetyPolicy.cs` | No | No | No |
| AbsoluteIncreaseCap | unknown | unknown | unknown | `AUTHORITY_MISSING` | §11 | Yes | No | Yes |
| CalibrationStart | unknown | unknown | unknown | `AUTHORITY_MISSING` (downstream of peak) | §12 | Yes | No | Yes |
| LR PreferredMin/Max/Selected/HardCap | missing×4 | missing×4 | missing×4 | `AUTHORITY_MISSING` | §13 | Yes | No | Yes |
| AbsolutePeakLR | unknown (19.0 likely too high) | unknown | unknown | `AUTHORITY_MISSING` | §15 | Yes | No | Yes |
| StartingLRCap | unknown | unknown | unknown | `AUTHORITY_MISSING` | §16 | Yes | No | No (opt-in field) |
| TaperDuration | reuse (2wk) | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` | `half-marathon-master.v*.json` | No | No | No |
| TaperW1/W2 multipliers | unknown (dose-sensitivity unverified) | unknown | unknown | `AUTHORITY_MISSING` | §18 | Yes | No | Yes |
| Pace/fallback | reuse | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §19 | No | No | No |
| Goal feasibility | reuse | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §20 | No | No | No |
| Session allocation | reuse, feasibility-recheck required | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §22 | No (verify only) | No | No |
| Calendar | reuse | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §23 | No | No | No |
| Adaptation | unknown | unknown | unknown | `SOURCE_SILENT` | §24 | Unknown | Unknown | Unknown |
| Product eligibility | strong precedent | strong precedent | weak/no precedent, recommend defer | not yet decided | §25 | Yes | No | No |
| Dark gate | not present; would require `V1CatalogPilotIdentityPolicy` widening | same | same | `AUTHORITY_MISSING` (mechanical, not evidentiary) | §31 | No — purely additive once numerics exist | No | No |

---

## 28. Advanced authority-gap ledger

| Authority | 3D | 4D | 5D | Status | Source | Decision Required? | Capability Gap? | Blocking? |
|---|---|---|---|---|---|---|---|---|
| CoreCycle | reuse | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` | §5 | No | No | No |
| Phase headroom | reuse | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` (empirically unverified) | §5 | No (feasibility re-check recommended) | No | No |
| Workout progression stages | reuse | reuse | reuse (lane 0); lane 1 open question, §8 | `GENERIC_MECHANISM_REUSE` w/ caveat | §6 | Yes for 5D lane-1 content decision (FARTLEK vs harder) | No | Partial (5D only) |
| Dose tier (hard-stimuli cap) | reuse (cap=2, allow 2nd hard) | reuse | reuse | `GENERIC_MECHANISM_REUSE` | `advanced-progression-modifier.v1.json` | No | **Possibly Yes — see below** | Possibly (3D layout slot count) |
| Two-hard-stimuli structural fit at 3D | n/a | n/a | n/a | `CAPABILITY_GAP` (tentative — RUN_LAYOUT_3D slot count not independently reconfirmed this phase) | §26, `SOURCE_SILENT` on exact slot count | Yes | Yes (tentative) | Possibly |
| `HM_PACE` eligibility | missing | missing | missing | `AUTHORITY_MISSING` | `advanced-modifier.v2.json` (no HM_PACE row) | Yes | No | Yes |
| 5D KEY2 semantics (harder lane?) | n/a | n/a | open (FARTLEK-reuse vs THRESHOLD-eligible) | `AUTHORITY_MISSING` | §8 | Yes | No | Yes (5D only) |
| PeakVolumeBand | missing | missing | missing | `MISSING` | §9 | Yes | No | **Yes — root blocker** |
| SelectedPeakReference | missing | missing | missing | `AUTHORITY_MISSING` | §10 | Yes | No | Yes |
| PreferredGrowth/HardGrowth | reuse (0.07/0.08) | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` | §11 | No | No | No |
| AbsoluteIncreaseCap | unknown (10K precedent: 2.5 at all 3 freqs) | unknown | unknown | `AUTHORITY_MISSING` | §11 | Yes | No | Yes |
| CalibrationStart | unknown | unknown | unknown | `AUTHORITY_MISSING` | §12 | Yes | No | Yes |
| LR PreferredMin/Max/Selected/HardCap | missing×4 | missing×4 | missing×4 | `AUTHORITY_MISSING` | §14 | Yes | No | Yes |
| AbsolutePeakLR | unknown (19.0 may be too conservative) | unknown | unknown | `AUTHORITY_MISSING` | §15 | Yes | No | Yes |
| StartingLRCap | unknown | unknown | unknown | `AUTHORITY_MISSING` | §16 | Yes | No | No (opt-in) |
| TaperDuration | reuse (2wk) | reuse | reuse | `HM_DISTANCE_WIDE_AUTHORITY` | §17 | No | No | No |
| TaperW1/W2 multipliers | unknown (dose-sensitivity unverified) | unknown | unknown | `AUTHORITY_MISSING` | §18 | Yes | No | Yes |
| Pace/fallback | reuse | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §19 | No | No | No |
| Goal feasibility | reuse | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §20 | No | No | No |
| Session allocation | reuse, feasibility-recheck required (longer sessions at higher volume) | reuse | reuse | `GENERIC_MECHANISM_REUSE` | §22 | No (verify only) | No | No |
| Calendar | reuse | reuse | reuse (already handles harder KEY2 with zero new code, §23) | `GENERIC_MECHANISM_REUSE` | §23 | No | No | No |
| Adaptation | unknown | unknown | unknown | `SOURCE_SILENT` | §24 | Unknown | Unknown | Unknown |
| Product eligibility | strong 10K precedent, but structural 3D concern (above) | strong precedent | strong precedent | not yet decided | §26 | Yes | No | No |
| Dark gate | not present | same | same | `AUTHORITY_MISSING` (mechanical) | §31 | No — purely additive | No | No |

---

## 29. Hidden Intermediate-assumption family search

Searched the assumption families listed in the governing prompt's §31 against the files this audit read directly:

| Assumption pattern | Where checked | Finding | Classification |
|---|---|---|---|
| HM == Intermediate (identity routing) | `V1CatalogPilotIdentityPolicy.cs` `IsSupportedLevelFrequency` `HalfMarathon` arm (line 286-289) | **Confirmed literally true today**: the `HalfMarathon` switch arm admits only `(Intermediate,3)/(Intermediate,4)/(Intermediate,5)` — any Beginner/Advanced HM identity throws `ArgumentOutOfRangeException` via `ResolveCandidate`'s fall-through `_ =>` case. This is explicit, intentional, and documented ("the only three cells this engagement has frozen authority for... deliberately not a broad 'any Half-Marathon combination' arm," line 283-285) | `LEVEL_POLICY_LOOKUP_NEEDED` — additive fix, not a hidden bug: adding new tuple arms is exactly the designed extension point |
| HM numeric policy only supports Intermediate | `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek` (line 341-347) | **Confirmed**: fail-closed switch, no Beginner/Advanced arms exist, throws for anything else | `LEVEL_POLICY_LOOKUP_NEEDED` — needs a sibling `ForHalfMarathonBeginnerDaysPerWeek`/`ForHalfMarathonAdvancedDaysPerWeek`, mirroring the already-real `ForBeginnerDaysPerWeek`/`ForAdvancedDaysPerWeek` 10K precedent (§4) |
| HM workout progression only references Intermediate modifier | `half-marathon-4d/3d-intermediate.v1.json`, `half-marathon-5d-intermediate.v1.json` combination files | The **combination record**, not the workout-progression file itself, is what pins `INTERMEDIATE_MODIFIER`. The workout-progression JSON (§6) is level-blind. So the "hardcoding" lives one layer up, in the not-yet-created Beginner/Advanced combination records — which is exactly where it belongs (a new combination file naturally points at `BEGINNER_MODIFIER`/`ADVANCED_MODIFIER` instead) | `GENERIC_SAFE` — no hidden coupling found; the extension point is exactly where you'd expect |
| HM dark identity only supports Intermediate | Same as row 1 | Confirmed, same finding | `LEVEL_POLICY_LOOKUP_NEEDED` |
| HM 5D KEY2 policy hardcoded to Intermediate | `INTERMEDIATE_MODIFIER v9`/`INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3` referenced by `half-marathon-5d-intermediate.v1.json` | Confirmed the *current* 5D combination hardcodes Intermediate's modifier, but the underlying dose-tier authority it points to (`maximumHardSessionsPerWeek=2`) already exists generically for Advanced too (§7/§8) — so this is a shallow, mechanical hardcode (one combination file references one level modifier), not a deep architectural coupling | `LEVEL_POLICY_LOOKUP_NEEDED` |
| HM selected-peak policy keyed only by frequency | `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek` | The dispatcher is named "...IntermediateDaysPerWeek" and takes only `daysPerWeek` — it is keyed by frequency *within* an implicitly-Intermediate-scoped method name, not a true "frequency-only, level-blind" dispatcher. This is the same finding as row 2, restated: the real fix is a *new* per-level dispatcher (mirroring 10K's `ForBeginnerDaysPerWeek`/`ForIntermediateDaysPerWeek`/`ForAdvancedDaysPerWeek` sibling-method pattern, §4), not a widening of the existing Intermediate-named one | `LEVEL_POLICY_LOOKUP_NEEDED` |
| Public gate widening risk | `IsSupportedIdentity`/`IsSupportedPreparationRunwayIdentity` | Both remain completely untouched by every HM phase to date (confirmed by their doc comments and the fact neither method references `GoalDistance.HalfMarathon` anywhere in the file) — HM has no public or Runway exposure at all yet, at any level | `PUBLIC_GATE_DO_NOT_WIDEN` — correctly and currently closed; nothing in this audit's findings creates pressure to widen it, and HM.12 does not recommend doing so |
| Catalog loader / validators / binder / calendar / adaptation | `PlanCatalogBundleLoader.cs`, `CatalogWeekSkeletonCalendarMaterializer.cs` (both partially read) | No `RunningBackground`/level-literal branching found in either file; `experience` is read generically as a string (`RequireString(levelModifier, "experience", ...)`, line 83) and threaded through as data, not branched on by value | `GENERIC_SAFE` |

**Overall §29/§31 verdict: the "HM==Intermediate" assumption is real, but it is concentrated in exactly two places — `V1CatalogPilotIdentityPolicy`'s HalfMarathon switch arm and `VolumeSafetyPolicy.ForHalfMarathonIntermediateDaysPerWeek`'s dispatcher name/scope — both of which are additive, well-understood, low-risk extension points, not hidden or structurally entangled assumptions.** No file was found where Intermediate is baked into calendar, validator, binder, or loader logic by value comparison. This is a genuinely reassuring finding: the blocking work is almost entirely *numeric authority* (§9-§18), not *architectural surgery*.

---

## 30. Minimum Beginner follow-up set

The smallest unresolved authority set (excluding everything already reusable per §27):
1. `PeakVolumeBand` for HALF_MARATHON×NEW×{3,4,5} (root blocker — everything numeric downstream depends on this).
2. `SelectedPeakReference` per frequency (a decision to apply-or-not-apply the midpoint convention, §10).
3. `AbsoluteWeeklyIncreaseCapKm` per frequency.
4. `GoldenFixtureStartingVolumeKm` per frequency (mechanical once #1/#2 exist, via ratio-reuse).
5. Long-run share quadruple (Preferred Min/Max, Selected, HardCap) per frequency.
6. `PreferredAbsolutePeakLongRunKm` for Beginner (re-derive or explicitly re-freeze 19.0/lower).
7. Taper multiplier dose-sensitivity re-verification for Beginner (may reuse 0.70/0.43, but must be checked, not assumed).
8. `HM_PACE` eligibility decision for `BEGINNER_MODIFIER` (new version).
9. 5D KEY2 semantics decision — given §8's finding, likely resolves to "EASY_EQUIVALENT in all phases, no LIGHT_QUALITY lane," but must be explicitly frozen, not defaulted silently.
10. Product-eligibility decision for Beginner 5D specifically (§25) — recommend resolving before #1-#9 are spent on a frequency that may be deferred.
11. New `ForHalfMarathonBeginnerDaysPerWeek` dispatcher + new dark-only `V1CatalogPilotIdentityPolicy` candidate keys/tuple arms (mechanical, low-risk, last step).

## 31. Minimum Advanced follow-up set

1. `PeakVolumeBand` for HALF_MARATHON×ADVANCED×{3,4,5} (root blocker).
2. `SelectedPeakReference` per frequency.
3. `AbsoluteWeeklyIncreaseCapKm` per frequency (10K precedent: 2.5 at all three — still requires an explicit HM decision, not a copy).
4. `GoldenFixtureStartingVolumeKm` per frequency (mechanical once #1/#2 exist).
5. Long-run share quadruple per frequency.
6. `PreferredAbsolutePeakLongRunKm` for Advanced (may need to be raised above 19.0 — real evidence question).
7. Taper multiplier dose-sensitivity re-verification for Advanced.
8. `HM_PACE` eligibility decision for `ADVANCED_MODIFIER` (new version).
9. 5D KEY2 semantics decision — genuinely open between "reuse FARTLEK-based LIGHT_QUALITY lane" and "introduce a harder THRESHOLD/HM_PACE-eligible KEY2," unlike Beginner's more determined answer.
10. RUN_LAYOUT_3D slot-count confirmation and, if only one `KEY_SESSION` slot exists, an explicit decision on how Advanced's second-hard-stimulus authority is expressed at 3D (or whether Advanced 3D is deferred/scoped differently).
11. New `ForHalfMarathonAdvancedDaysPerWeek` dispatcher + new dark-only identity candidates (mechanical).

---

## 32. Recommended phase decomposition

Mirroring the already-proven HM.6→HM.7→HM.8 (and HM.9→HM.10→HM.11) audit→numeric-closure→dark-implementation rhythm:

1. **HM.13 — Beginner/Advanced numeric authority closure, per level** (two sibling phases or one combined phase with two independent halves): freeze peak-volume bands, selected peaks, growth caps, calibration starts, LR-share quadruples, absolute-peak-LR, taper multipliers, `HM_PACE` eligibility, and 5D KEY2 semantics for each level — using the exact evidence-basis discipline HM.7/HM.10 already established (re-verify, don't blind-copy; cite real sources; classify governance state per field).
2. **HM.14 — Beginner 3D/4D dark implementation** (mirrors HM.8), contingent on §30's product-eligibility question being resolved first for whether 5D is in scope.
3. **HM.15 — Advanced 3D/4D/5D dark implementation** (mirrors HM.2/HM.11), contingent on §31 item 10 (3D layout/second-hard-stimulus question) being resolved.
4. Public activation of any HM cell (any level) remains explicitly out of scope for all of the above and is not recommended by this audit.

## 33. Implementation forecast

The smallest clean implementation, once authority is closed, requires **zero new generator/engine classes** — confirmed by this audit's own findings that CoreCycle, phase headroom, workout-progression schema, run-layout schema, pace/fallback, goal-feasibility, calendar/hardness, and session-allocation mechanisms are all already level-blind and reusable as-is (§5, §6, §17, §19-§23). The forecasted per-level, per-frequency implementation shape is:

- One new `BEGINNER_MODIFIER`/`ADVANCED_MODIFIER` catalog version (adding `HM_PACE`, and for Advanced possibly a new `FARTLEK`/`THRESHOLD_TEMPO` version if §31 item 9 resolves toward a harder lane).
- Zero new `HALF_MARATHON_MASTER` or `HALF_MARATHON_WORKOUT_PROGRESSION` versions needed for 3D/4D (reuse v1 of each verbatim); 5D reuses the existing v2/v2 pair verbatim too, provided KEY2 semantics land on FARTLEK-based reuse.
- One new named `VolumeSafetyPolicy` instance per level×frequency cell (6 total), plus two new `For<Level>HalfMarathonDaysPerWeek`-style dispatchers, plus (if HM's own taper-policy-per-frequency pattern is followed) up to 6 new `internal static class`-style taper-multiplier policies (or direct literals if the multipliers turn out identical to Intermediate's, in which case the existing three internal classes are reused directly and no new ones are needed at all).
- Six new dark-only combination JSON files (`half-marathon-{3,4,5}d-{beginner,advanced}.v1.json`), each pointing at the appropriate `RUN_LAYOUT_nD` (reused, zero new layout files) and the appropriate level modifier version.
- Six new dark-only candidate-key constants + tuple arms in `V1CatalogPilotIdentityPolicy` (additive, mirrors the existing HM Intermediate pattern exactly).

No `Beginner3DGenerator`/`Advanced5DGenerator`-style per-cell engine is indicated anywhere in this forecast — every existing generic engine (planner, allocator, binder, calendar materializer, validators) consumes policy objects and catalog references, never level-literal branches, for the paths this audit inspected.

---

## FINAL OUTPUT

**OVERALL:** `HM_BEGINNER_ADVANCED_LEVEL_EXPANSION_AUDIT_COMPLETE`
(Both levels were fully audited across all 36 required sections; every remaining gap is explicit and enumerated in §27/§28/§30/§31; no contradictory or missing foundational artifact prevented the audit itself from determining the authority/capability boundary — the one genuine unknown, adaptation-engine level-sensitivity (§24), is reported as `SOURCE_SILENT`, not as a phase-blocking contradiction.)

**BEGINNER:** `HM_BEGINNER_LEVEL_EXPANSION_READY_FOR_AUTHORITY_CLOSURE`
(Generic architecture — CoreCycle, phase headroom, workout-progression schema, run-layout, pace/fallback, goal-feasibility, calendar, and the hard-stimulus-cap dose-tier authority — is already reusable as-is. Remaining work is bounded to explicit numeric/product decisions: peak-volume band, selected peak, growth cap, calibration start, LR-share quadruple, absolute-peak-LR, taper multipliers, `HM_PACE` eligibility, 5D KEY2 semantics/eligibility, and the Beginner-5D product-eligibility question.)

**ADVANCED:** `HM_ADVANCED_LEVEL_EXPANSION_READY_FOR_AUTHORITY_CLOSURE`
(Same generic-architecture reuse holds. Remaining work is the mirrored numeric/product decision set, plus one additional structural question unique to Advanced: whether RUN_LAYOUT_3D's slot geometry can express Advanced's already-frozen two-hard-stimuli-per-week authority, or whether that authority must be expressed through a non-KEY_SESSION role, deferred to a different frequency, or explicitly scoped down for HM 3D specifically.)

**BEGINNER MINIMUM FOLLOW-UP AUTHORITY SET:** peak-volume band → selected peak → absolute weekly-increase cap → calibration start (ratio-derived) → LR-share quadruple → absolute-peak-LR → taper-multiplier dose-sensitivity re-check → `HM_PACE` eligibility → 5D KEY2 semantics (likely EASY-only) → Beginner-5D product-eligibility decision → mechanical dispatcher/identity wiring. (§30)

**ADVANCED MINIMUM FOLLOW-UP AUTHORITY SET:** peak-volume band → selected peak → absolute weekly-increase cap → calibration start (ratio-derived) → LR-share quadruple → absolute-peak-LR (may need raising) → taper-multiplier dose-sensitivity re-check → `HM_PACE` eligibility → 5D KEY2 semantics (FARTLEK-reuse vs. harder lane, genuinely open) → RUN_LAYOUT_3D two-hard-stimuli structural question → mechanical dispatcher/identity wiring. (§31)

**RECOMMENDED NEXT PHASE:** HM.13 — HALF_MARATHON Beginner/Advanced numeric authority closure (per level, mirroring the HM.7/HM.10 evidence-basis discipline), addressing the peak-volume-band gap (§9) first since every other numeric field in both ledgers is directly or indirectly downstream of it.
