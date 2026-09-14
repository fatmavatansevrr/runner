# PHASE EXP.0 — PRODUCT-WIDE EXPERIENCED LEVEL DECISION, TAXONOMY, AND ARCHITECTURE AUDIT

**Type:** GOVERNANCE / PRODUCT-DECISION (no production implementation, no catalog modifier authoring, no distance-specific numeric authority, no HM/10K/6D implementation, no public activation)

---

## 1. HM-E1 blocker consumed

HM-E1 (`PHASE_HM_E1_EXPERIENCED_3D_4D_5D_PRODUCT_ELIGIBILITY_REUSE_AND_NUMERIC_AUTHORITY_CLOSURE.md`) closed with:

```
HM_EXPERIENCED_3D_4D_5D_AUTHORITY_BLOCKED
```

with one shared blocker across 3D/4D/5D: **no real, pre-existing, cross-distance `Experienced`-level catalog/modifier/dose/hard-stimulus authority exists anywhere in the product**, and HM was correctly prohibited from unilaterally originating that authority itself (its own §37). This finding is taken as authoritative and **not reopened or re-litigated** — this phase's direct re-verification (§2–§4 below) confirms it independently, with no contradiction found. EXP.0 exists specifically to answer the *higher-level* product question HM-E1 was structurally forbidden from answering: should Experienced become real, product-wide authority at all, for any distance?

## 2. Current product taxonomy

Full repository-wide audit (case-insensitive, `Experienced`/`EXPERIENCED`/`Elite`/`ELITE`, across domain, DTOs, validation, onboarding, frontend, catalog schema/artifacts, identity policies, tests, and docs). Selected, decisive rows (full detail available in the underlying research pass):

| Location | Representation | Runtime Recognized? | Catalog Recognized? | Public Recognized? | Historical Only? | Current Meaning |
|---|---|---|---|---|---|---|
| `backend/RunningApp.Domain/Enums/RunningBackground.cs` | `{Beginner, Intermediate, Advanced, Experienced}` | Deserializes fine | No | Yes (wire round-trips) | No, live | Onboarding self-description enum |
| `backend/RunningApp.Domain/Enums/RunningBackgroundCanonicalJsonConverter.cs` | Public-boundary converter | N/A | N/A | **Yes** — `"experienced"` is accepted, not rejected at the wire level | No | Confirms Experienced is a valid public JSON value |
| `backend/RunningApp.Persistence/Converters/RunningBackgroundCompatibilityConverter.cs` | EF/Postgres converter | Yes — persists/reads `"experienced"` | No | Indirect | No | DB can and does store `Level='experienced'` rows |
| `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` (`IsSupportedIdentity`/`IsSupportedLevelFrequency`) | Explicit allow-list | **No — the actual gate** | No | Always `false` for Experienced, any distance/frequency | No, currently enforced | The single authoritative exclusion point |
| `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogDomainMapper.cs:54-71` | Classification comment | No | No | N/A | No | Confirms only Intermediate has a catalog mapping |
| `plan-catalog/src/PlanCatalog.Contracts/Enums/RunningExperience.cs` | `{New, Intermediate, Advanced, Experienced}` (catalog engine's own, separate taxonomy) | Schema-reserved | Schema-reserved, **zero live rows** | No | No | Structurally present, content absent |
| `plan-catalog/schemas/peak-volume-band-policy.schema.json`, `level-modifier.schema.json` | JSON-schema `enum` includes `"EXPERIENCED"` | N/A | Schema allows it | No | No | Schema reservation only, never authored |
| `plan-catalog/catalog/policies/peak-volume-bands.v1.json`/`.v2.json` (dead) | TEN_K/EXPERIENCED rows | No — superseded | No — dead | No | **Yes** | Earliest catalog-engine-era numeric candidate |
| `plan-catalog/tests/.../D4PeakVolumeBandResolutionTests.cs` (`PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows`) | Regression guard, also asserts raw JSON contains no `"ELITE"` string | N/A | Actively prevents reintroduction | No | No | Enforced, positive exclusion |
| `mobile/lib/core/models/running_background.dart` | Flutter enum `experienced`, label "Experienced", description "I have a strong running base and regularly train for longer distances." | N/A | N/A | **Yes — live, selectable, submitted** | No | Frontend mirror of backend enum |
| `mobile/lib/features/onboarding/presentation/running_background_page.dart` | Iterates `RunningBackground.values` unfiltered, one card per value, own icon | N/A | N/A | **Yes — reachable, wired end-to-end** | No | A real user can select it today |

**Elite**: confirmed a pure red herring — never an enum member, schema value, or catalog concept. Every hit in the repository is a negative assertion (a test proving `"ELITE"` is *absent* from a live artifact, or that the wire value `"elite"` is *rejected* as unknown). No 5th tier exists or was ever proposed as real product taxonomy.

**Conclusion**: Experienced is fully *representable* at every technical layer (enum, wire format, database) but is deliberately, actively *excluded* at exactly one authoritative point — the public identity/eligibility gate — for every distance, every frequency, with no exception found anywhere.

## 3. Current Experienced exclusion proof

This exclusion is not a single incidental test. A dedicated research pass found it independently, repeatedly reasserted across many separate generations of work, for both 10K and HM:

- `RunningBackgroundV2Tests.cs` — `UnwidenedNonIntermediateLevels_AreNotSilentlyCoercedToIntermediate(Experienced)`, plus the dedicated `"elite"`-rejection test confirming Elite is unknown at the wire level entirely (distinct from Experienced, which is known-but-rejected).
- `plan-catalog/tests/.../Golden/PeakVolumePolicyImmutabilityTests.cs` — `RestoredPolicyV1_DoesNotInventNewOrAdvancedOrExperiencedRows`.
- `plan-catalog/tests/.../Validation/D4PeakVolumeBandResolutionTests.cs` — `PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows`.
- `Gen9AdvancedCombinedDarkVerificationTests.cs` — `ExperiencedLevel_RemainsUnrecognized_ForEveryFrequency` (10K, every frequency 3-7).
- `Gen4DBeginnerFourDayCoreTests.cs`, `Gen10AdvancedCombinedPublicActivationTests.cs`, `Gen20TwoDayCombinedPublicActivationTests.cs`, `Gen25BeginnerThreeDayCorePublicActivationTests.cs`, `PublishedCatalogNonDevelopmentEndToEndTests.cs` — each independently re-asserts, at its own generation/activation milestone, that Experienced never silently inherits a neighboring level's newly-opened identity (Beginner's, Advanced's, 2D's, 3D's). `Gen25...ExperiencedThreeDay_RemainsClosed_NoFallbackToBeginnerThreeDayIdentity` is a real end-to-end HTTP test whose underlying `.trx` confirms the exact rejection path: `PlanTemplateNotAvailableException` → 404 `PLAN_TEMPLATE_NOT_FOUND`.
- `RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md:153-155` documents the same live behavior in prose: Experienced is accepted at the HTTP boundary, then dead-ends with a 404, "not treated as a defect."

**Determination**: this is intentional, stable, deliberately-reaffirmed product policy — not temporary V1 scope, not an oversight, and not coupled to a specific old catalog version (it holds across every catalog version from v1 through the current live version, and across every generation of public-activation work from GEN.4 through GEN.25 and HM.10-HM.18). No comment or doc anywhere frames it as "temporary until we get to it." Per this phase's own instruction, this is treated as **current, valid architecture**, not an obsolete artifact to route around.

## 4. Original Experienced product intent

| Source | Role of Experienced | Distinction from Advanced | Normative? | Reached runtime/catalog? | Superseded? |
|---|---|---|---|---|---|
| `RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md:19-30` (B: UX copy) | Onboarding self-description option | Advanced: "train consistently, 10km+"; Experienced: "strong running base, regularly train longer distances" — the **only** authored distinguishing prose in the repository | Yes — live, shipped copy, mirrored in Flutter | Reached frontend/backend contract only | Not superseded — current copy |
| `HM_21K_Claude_Handoff_and_Build_Plan.md:160-190` (C: research/planning segmentation) | Pre-catalog-engine numeric candidate table | Higher peak-volume band (40-52/50-66/60-76 vs Advanced's 36-48/46-60/54-70), but starting-LR-cap explicitly *grouped* with Advanced (15km) | No — `DELIBERATE_LEGACY_RECORD` | No — never carried into `VolumeSafetyPolicy.cs` or any live catalog file | Effectively abandoned before the GEN.*/HM.* engine was built |
| `plan-catalog/catalog/policies/peak-volume-bands.v1.json`/`.v2.json` (D→abandoned E: canonical-but-later-reversed) | Catalog-engine-era numeric candidate (TEN_K, 40-55/46-62/50-68) | Disagrees with the HM table above (different era, different source, no reconciliation) | No — removed at v3, now test-locked against reintroduction | Briefly present in early catalog vintages, never used in production | Yes — actively superseded and now guarded |
| `RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md:153-155` | Documents live behavior | Groups Advanced/Experienced as one undifferentiated "accepted-but-unpiloted" pair | Yes — describes shipped behavior | Reaches the HTTP boundary only | Not superseded |
| `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md:510-515` | Historical-candidate register entry | None offered — classified as pure taxonomy drift | Yes — explicit "do not promote" instruction | No | Reaffirmed by HM-E1 |
| `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md:58-68` | Cross-references the two same-named enums | None — identical language used for both rows | Yes — audit finding | No | Not superseded |
| `PHASE_HM_E1_...md` (entire document) | Dedicated cross-distance audit | Concludes the one real signal (onboarding copy) implies "more of the same" volume/duration, not a different training-stimulus philosophy | Yes — normative governance output | No | Most recent, authoritative treatment prior to this phase |

**Category assignment** (per this phase's own A–E framework): the onboarding copy is **(B) UX copy**, real and live but never operationalized into (D) canonical runtime authority. The two numeric tables are **(C) research segmentation / (E) abandoned proposal** respectively — real historical artifacts, never normative, and mutually disagreeing. No source in the repository's history constitutes (D) canonical runtime authority for Experienced. This confirms the instruction not to promote old onboarding copy into training authority: the copy is evidence of *intent to eventually differentiate*, not evidence of *what that differentiation should be*.

## 5. Experienced product purpose

**EXPERIENCED_PRODUCT_PURPOSE**: The only authored, real signal — the onboarding copy contrasting "train consistently, run 10km+" (Advanced) against "strong running base, regularly train longer distances" (Experienced) — points toward volume/duration accumulation and race-distance history as the intended differentiator, not a different kind of training stimulus, workout taxonomy, coaching sophistication, or injury-resilience profile; no source anywhere proposes any of those alternatives with real content. This candidate purpose is thin (one paragraph of UX copy, never elaborated, never operationalized in two separate historical catalog-authoring attempts that were each abandoned before shipping) and is externally undercut (§16): established coaching frameworks that use a fourth tier above Advanced use it for a categorically different, competitive/professional population ("Elite"), not for "more experienced" recreational-to-serious amateur runners, and where "experienced" appears in that literature at all it is used as a *synonym describing* Advanced runners, never as an independent rung with its own volume/frequency norms. No evidence anywhere — internal or external — supports Experienced representing materially greater training history *in a way current onboarding inputs could reliably distinguish from Advanced*, a genuinely different quality-density or hard-stimulus tolerance, or a different readiness/adaptation model. The honest conclusion is that **no meaningful, evidence-backed distinct purpose can currently be justified** beyond "more volume, plausibly," which is exactly the thin, unsupported differentiation this phase's own governing prompt instructs against manufacturing.

## 6. Advanced vs Experienced comparison

| Dimension | Advanced (real, shipped) | Experienced (candidate) | Global Difference? | Distance-Specific? | Evidence |
|---|---|---|---|---|---|
| Training-history expectation | "Train consistently, run 10km+" (onboarding copy) | "Strong running base, train longer distances" (onboarding copy) | Thin candidate difference (volume/duration) | Would be global framing, distance-specific numbers | §4, §16 |
| Recent consistency | Same readiness inputs as every level | No distinct input exists | None found | — | §8 |
| Weekly-frequency tolerance | 3D-5D shipped (HM), 3D-7D shipped (10K, incl. 6D) | No evidence Experienced needs a *different* frequency ceiling than Advanced | None found | Distance-specific if ever built | §10 |
| Volume tolerance | Real, frozen per-distance bands | Two disagreeing historical candidates only | Candidate only, unresolved | Distance-specific | §15 |
| Quality-density tolerance | Real, frozen (max true-hard count per level/frequency) | No evidence proposed anywhere | None found | Distance-specific if ever built | §11 |
| Max true-hard stimuli | Established per level/frequency (e.g., HM Advanced 5D's KEY2) | No evidence any product source ever proposed Experienced needs *more* hard stimuli than Advanced | None found | Distance-specific | §11 |
| Dose tier | Existing modifier-tier architecture (Beginner/Intermediate/Advanced modifiers) | No dedicated Experienced modifier ever authored | None found | N/A — architecture question, see §12 | §12 |
| Second-quality-session eligibility | Level/frequency-gated, real | No evidence proposed | None found | Distance-specific | §13 |
| Race-specific exposure | Real, frozen per distance | No evidence proposed | None found | Distance-specific | — |
| Pace-evidence requirements | Same inputs as other levels | No distinct requirement proposed | None found | — | §8 |
| Recovery/adaptation assumptions | Generic `PlaceholderAdaptationEngine`, level-agnostic | No evidence Experienced needs a different model | None found | — | §14 |
| Readiness expectations | Same missing/explicit-zero-readiness contract as every level | No distinct contract proposed | None found | — | §8 |

**Finding**: across every dimension actually audited, real, current product architecture treats Advanced and Experienced identically except for one thin, unoperationalized piece of onboarding copy suggesting a volume/duration difference. Per this phase's own explicit prohibition ("do not invent a difference in every dimension... artificial differentiation is prohibited"), inventing distinctions across the other eleven dimensions to make the level "feel" distinct would be exactly the fabrication this phase must avoid. The evidence supports at most one candidate distinguishing dimension (volume/duration), and that one candidate is itself unresolved, externally uncorroborated, and previously abandoned twice.

## 7. Global vs distance-specific authority boundary

Not reached as live authority — no cell is being approved (see §17). For completeness, if this decision is ever reopened with new evidence: level identity, training-history meaning, and general dose-tier/frequency-eligibility *philosophy* would be product-wide candidates; peak weekly volume, selected peak, absolute weekly increase cap, calibration start, long-run share, absolute long-run ceiling, race-specific dose, and distance-specific pace work would remain distance-specific, exactly mirroring how Beginner/Intermediate/Advanced authority is currently split between global modifier philosophy and per-distance `VolumeSafetyPolicy`/taper tables. This phase does not freeze any HM or 10K number under this boundary.

## 8. Readiness semantics

Current onboarding/readiness inputs (recent weekly volume, recent longest run, recent runs/week, recent race, pace evidence) are identical across all four `RunningBackground` values — none is level-specific today, and no input captures "years of structured training" or any other longitudinal training-history signal. These inputs **cannot reliably distinguish Advanced from Experienced** as currently defined by the one authored candidate purpose (volume/duration history), since they measure recent state, not accumulated history. Introducing a distinct Experienced readiness bar would require either a new input (e.g., years of structured training — not currently collected) or accepting that self-selection alone (unvalidated) is authoritative, consistent with how Beginner/Intermediate/Advanced already work. No hidden scoring algorithm is proposed or implied by anything in the product. **This is recorded as a real product-capability gap**, not solved here: current inputs cannot support a meaningful, system-validated Experienced distinction; only unvalidated self-selection could support it today, and self-selection is exactly the mechanism already producing the current, real, live "select Experienced → 404" dead end for HM.19-era users.

## 9. User-selection model

Beginner/Intermediate/Advanced are all model **A** (direct, unvalidated user self-selection — no runtime readiness gate blocks a Beginner from selecting Advanced). Per this phase's own preference for consistency absent contrary evidence, and finding no such evidence, the correct model for Experienced — if ever approved — is the same model **A**, not a new derived-classification (B) or hard-gated (C/D) model. This phase does **not** approve Experienced under model A; it records that *if* approved, model A is the only defensible, consistency-preserving choice.

## 10. Frequency orthogonality

`RunningBackground`/`RunningExperience` (level) and `DaysPerWeek` (frequency) are architecturally orthogonal today for every existing level — 10K already ships Beginner/Intermediate/Advanced across multiple frequencies including 6D for Intermediate/Advanced, and HM ships 3D/4D/5D across three levels, independently. Nothing in the repository ties any level definition to a specific frequency. **It is valid product architecture for an Experienced athlete to choose fewer running days** — frequency remains orthogonal by the same reasoning that already governs every other level, with no evidence found anywhere that Experienced should be constrained differently. `Experienced = 6D runner` is explicitly rejected as a definition; this phase does not adopt it, and HM-X1's own 6D scoping (§25) is unaffected by whatever happens to Experienced.

## 11. Hard-stimulus architecture

Advanced's real, shipped modifiers (10K and HM) define exact true-hard-stimulus ceilings per level/frequency (e.g., HM Advanced 5D's KEY2/THRESHOLD_TEMPO authority from HM.15). No product source anywhere proposes, with real content, that Experienced should permit *more* true-hard stimuli than Advanced, nor is there evidence such a widening would be safe or trainable-as-written by the existing session/calendar architecture. Per this phase's own explicit prohibition against inferring "Advanced max N → Experienced max N+1" without authority:

```
DEFER_TO_EXP_1_EVIDENCE_CLOSURE
```

No hard-stimulus number is invented here merely to make the level feel distinct.

## 12. Dose-tier architecture

The real modifier/dose architecture (`beginner-modifier.v2.json`, `intermediate-modifier.v9.json`, `advanced-modifier.v3.json` for HM; equivalent 10K artifacts) currently has exactly three populated dose tiers, one per shipped level — there is no existing "STANDARD/ADVANCED"-style intermediate dose grouping independent of level. No `experienced-modifier` file has ever existed, and no evidence anywhere specifies what a fourth tier's dose content should contain beyond "more volume." Classification:

```
NO_GLOBAL_DOSE_DIFFERENCE — insufficient evidence exists to justify a genuinely new dose tier; if Experienced is ever approved, the most defensible starting hypothesis (not adopted here) is REUSE_EXISTING_HIGHEST_DOSE_TIER (Advanced's), pending a dedicated evidence phase.
```

## 13. Workout family eligibility

No product source anywhere proposes that Experienced needs access to a workout family (FARTLEK, THRESHOLD_TEMPO, VO2/faster-than-race work, race-specific pace, strides/hills, secondary quality) that Advanced currently lacks. Advanced already has broad workout-family access at both 10K and HM (including HM.15/HM.16's own THRESHOLD_TEMPO-based Advanced 5D KEY2). Per this phase's own caution against global permissions that could accidentally widen every distance: **no workout-family eligibility difference is found or proposed**; this remains, if ever revisited, a distance/progression-specific authority question, not a global one.

## 14. Adaptation compatibility

The product's adaptation engine (`PlaceholderAdaptationEngine`) is level-agnostic today — it does not branch on `RunningBackground` at all. No evidence anywhere proposes Experienced-specific adaptation logic, recovery scoring, or weighting. Per this phase's own default preference:

```
GENERIC_ADAPTATION_REUSE_EXPECTED
```

No new recovery score or adaptation branch is invented here.

## 15. Historical Experienced candidate inventory

| Source | Distance | Frequency | Candidate | Status | Why Not Current Authority |
|---|---|---|---|---|---|
| `HM_21K_Claude_Handoff_and_Build_Plan.md:167` | HALF_MARATHON | 3D | 40-52 km/wk | Historical, never adopted | Predates catalog engine; never carried into `VolumeSafetyPolicy.cs` |
| `HM_21K_Claude_Handoff_and_Build_Plan.md:167` | HALF_MARATHON | 4D | 50-66 km/wk | Historical, never adopted | Same |
| `HM_21K_Claude_Handoff_and_Build_Plan.md:167` | HALF_MARATHON | 5D | 60-76 km/wk | Historical, never adopted | Same |
| `HM_21K_Claude_Handoff_and_Build_Plan.md:189` | HALF_MARATHON | all (grouped w/ Advanced) | Starting LR cap 15km | Historical, never adopted | Grouped with Advanced, not independently derived; never shipped |
| `peak-volume-bands.v1.json`/`.v2.json` | TEN_K | 3D | 40-55 km/wk | Dead, superseded, now test-excluded | Removed at v3; disagrees with the HM table above (different source/era) |
| `peak-volume-bands.v1.json`/`.v2.json` | TEN_K | 4D | 46-62 km/wk | Dead, superseded, now test-excluded | Same |
| `peak-volume-bands.v1.json`/`.v2.json` | TEN_K | 5D | 50-68 km/wk | Dead, superseded, now test-excluded | Same |

**Explicit conflict recorded, not resolved**: the HM table's own 3D band (40-52) and the TEN_K table's own 3D band (40-55) are close but not identical, from different eras, for different distances, with no reconciliation ever attempted before both were abandoned. This phase does not select between them, average them, or promote either — per its own charter, these remain candidates for a future distance-specific numeric-authority phase, not resolved here.

## 16. External evidence

Consulted to answer exactly one question: "Is Experienced a defensible distinct product/training tier above Advanced?" (not to derive any numeric band).

- Consumer-facing, widely-used coaching plan systems (e.g., McMillan Running's four-tier structure: **Beginner → Intermediate → Advanced → Elite**) place the tier above Advanced as **Elite** — explicitly "top-level runner competing at a national or international level" — a categorically different, competitive/professional population, not a more-experienced recreational-to-serious-amateur runner. Where these sources use the word "experienced," it describes Advanced runners themselves ("Advanced Level 5 runners are high-volume, experienced trainers/racers"), never a separate rung.
- Strength & conditioning's comparable four-tier training-age model (Novice → Intermediate → Advanced → Elite) places "Elite" at "the top few percent of the training population" — again a rare/exceptional-ability population, not a duration-of-training distinction within the serious-recreational band.
- Exercise-physiology literature does recognize finer gradations ("well-trained" vs. "highly trained") but ties them to continuously-measured physiological markers (VO2max, competition-level performance data) rather than a discrete, self-reportable named tier — not practically usable by a consumer onboarding flow, and not named "Experienced" in this literature.

**Conclusion**: no consulted framework independently validates "Experienced" as a named, distinct tier between "Advanced" and "Elite" for the population Appsel serves. The natural next tier above Advanced in established practice is Elite — a population (competitive/national-level athletes) that is clearly out of scope for Appsel as a consumer training app, and which the product's own current architecture already treats as fully out of scope (Elite does not exist anywhere in the codebase; see §2). This external evidence weighs against, not for, treating Experienced as a real, materially distinct fourth tier.

## 17. Product decision

```
OPTION B — EXPERIENCED_REMAINS_NON_PRODUCT_RESEARCH_SEGMENT
```

**Reasoning**: Per this phase's own explicit governing rule, "if no meaningful distinct purpose can be justified, Experienced should remain excluded." The full audit (§2-§16) found: (a) current architecture already, deliberately, and repeatedly (across many independent generations of both 10K and HM work) treats Experienced as excluded, never as an oversight; (b) the only authored differentiating signal is one paragraph of onboarding copy, never operationalized into any real catalog/backend content in two separate historical attempts, both abandoned; (c) those two historical numeric attempts disagree with each other; (d) across eleven of twelve audited product dimensions, no real distinction from Advanced exists or has ever been proposed with real content; (e) external, respected coaching and exercise-science frameworks do not independently support "Experienced" as a distinct tier between Advanced and a genuinely different (and out-of-scope) Elite/competitive population. This is not an evidence gap that a future phase could close by looking harder at the *existing* record — it is a converged, negative finding. Historical Experienced numeric candidates (§15) are recorded as research-only, not as authority awaiting a future distance-specific phase to arbitrate between them.

## 18. Approved global semantic contract

Not applicable — Experienced is not approved as a first-class product level. No global semantic contract is frozen.

## 19. Backward-compatibility / intended-delta map

No change is made to any current behavior. The existing exclusion (§3) is reaffirmed as **permanent current product policy**, not a temporary state awaiting a future phase:

| Item | Current State | Disposition |
|---|---|---|
| `V1CatalogPilotIdentityPolicy` exclusion tests (10K and HM, all generations listed in §3) | Passing, enforced | **Remain permanently correct** — not superseded, not `EXPECTED_INTENDED_DELTA` |
| `PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows` / `RestoredPolicyV1_DoesNotInventNewOrAdvancedOrExperiencedRows` | Passing, enforced | **Remain permanently correct** |
| Public API acceptance of `RunningBackground.Experienced` at the wire/DB layer | Accepted, then dead-ends at generation (404) | Unchanged — this phase makes no representability change |
| Flutter onboarding UI offering "Experienced" as a live, selectable option that leads to a 404 | Live, reachable, unresolved rough edge | **Not fixed by this governance-only phase** — flagged in §26 as an existing product inconsistency for a future non-EXP UX phase to consider, now that "pending" framing is explicitly retired by this decision |
| `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md` §29-32 (HM-X1 6D scope) | Already scopes HM-X1 6D to "Intermediate and Advanced (do not assume Beginner)," with no mention of Experienced | **Already consistent with this decision** — no correction needed (verified directly, no edit made) |

Existing Beginner/Intermediate/Advanced public behavior for both 10K and HM remains zero-delta; nothing in this phase touches any shipped cell.

## 20. Taxonomy/versioning architecture

Not applicable — no new catalog artifact, modifier tier, or schema evolution is being introduced.

## 21. API compatibility

```
REPRESENTABLE_BUT_REJECTED
```

`RunningBackground.Experienced` is fully representable through the current public API and database layer: `RunningBackgroundCanonicalJsonConverter` accepts and emits `"experienced"` at the wire boundary exactly like the other three canonical values (confirmed by a dedicated test proving the truly-unknown value `"elite"` is rejected with a typed `JsonException`, in contrast), and `RunningBackgroundCompatibilityConverter` persists/reads `Level='experienced'` rows in real Postgres. The request is accepted (a 200-eligible parse) and only fails downstream, at plan-generation time, because `V1CatalogPilotIdentityPolicy.IsSupportedIdentity` returns `false` for every distance/frequency combination — surfacing as HTTP 404 `PLAN_TEMPLATE_NOT_FOUND` via `PlanTemplateNotAvailableException`. No schema change would be required to represent Experienced; the exclusion is a pure policy/business-rule gate, not a representability limitation. This phase does not widen that gate.

## 22. Frontend compatibility

The Flutter UI's "Experienced" option is **live, wired, and reachable** — not dead code, hidden, or feature-gated. `running_background_page.dart` iterates all four `RunningBackground` values unfiltered, each with its own selectable card and icon; a real user can tap "Experienced," proceed through the onboarding details flow, and have the value submitted to the backend, where it is accepted and then dead-ends in a 404 at plan generation (§21). This is existing, current, real product behavior — not something EXP.0 introduces or fixes. Given this phase's decision (§17) that Experienced is not pending future implementation but is now explicitly closed as a non-product runtime concept, this existing UI/backend mismatch (a normal-looking, fully-described onboarding option that silently dead-ends) is recorded as a real product inconsistency for a future, non-EXP, UX-scoped phase to resolve — e.g., by removing the option, or by reframing its copy so it no longer implies a fully-supported path. EXP.1/EXP.2 (§24, not initiated) would have needed this exact frontend surface if the decision had gone the other way; since it did not, no frontend work is required by this phase.

## 23. First-distance pilot decision

Not applicable — no distance pilot is being initiated. Had Experienced been approved, 10K (most mature architecture, richest precedent surface, and the distance whose own exclusion tests are most extensively reaffirmed) would have been the correct first pilot rather than HM, for exactly the reasoning HM-E1 already established (cross-distance semantics should not originate in HM alone). This reasoning is recorded for future reference only.

## 24. Future phase sequence

Not initiated. The EXP.1-EXP.5 sequence proposed in the governing prompt (product-wide modifier authority closure → 10K eligibility/numeric authority → 10K implementation/activation → HM re-closure → HM implementation/activation) is **not recommended to proceed**, because its premise (Experienced approved as a first-class level) is not met. If new product evidence ever emerges (a real training-history input becomes collectable, a genuine coaching rationale is authored, or product strategy explicitly wants a fourth self-selection tier for reasons beyond training-load differentiation), that sequence remains a reasonable template for a future EXP.0-equivalent re-decision — but this phase does not schedule or presuppose that re-decision will happen.

## 25. Existing-product zero-delta obligations

Confirmed: current public 10K behavior (Beginner/Intermediate/Advanced across all shipped frequencies including 6D) and current public HM V1 behavior (Beginner 3D/4D, Intermediate 3D/4D/5D, Advanced 3D/4D/5D; Beginner 5D remains `PRODUCT_INELIGIBLE`) are both fully preserved — no file touching any of that behavior was read for modification, and `git status --porcelain` (see §29 discipline) confirms no `.cs`, catalog JSON, schema, migration, or Flutter runtime file changed. HM-X1's own 6D scope (`Intermediate`, `Advanced` — per the already-correct `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md:529`) is explicitly confirmed, not reopened, by this decision: 6D Experienced is closed as non-product scope, consistent with this phase's own §17 finding, and HM-X1 proceeds targeting only Intermediate and Advanced unless a future product decision reopens Experienced.

## 26. Remaining blockers

None block this phase's own closure — the product decision (§17) is made with full confidence from converged internal and external evidence, not a `BLOCKED` outcome. One item is recorded for future, separate consideration (not a blocker to this phase, and explicitly not resolved here): the live Flutter onboarding flow currently presents "Experienced" as a normal, fully-described, selectable option that silently dead-ends in a 404 at plan generation for every distance — a genuine, pre-existing UX inconsistency, now more clearly a product inconsistency (rather than a "not yet implemented" gap) in light of this phase's closure decision, worth a future, narrowly-scoped, non-EXP UX phase to address. No production, catalog, schema, or frontend file was modified to record this observation.

---

## FINAL CLASSIFICATION

```
PRODUCT_WIDE_EXPERIENCED_LEVEL_REMAINS_EXCLUDED
```

Experienced is not approved as a first-class Appsel training level. The existing, deliberately-enforced runtime/catalog exclusion (§3) is confirmed as correct, current, permanent product policy — not temporary V1 scope, not an oversight, and not something a future distance-specific phase should silently work around. The historical Experienced numeric candidates (§15) and the one authored onboarding-copy distinction (§4-§5) are recorded as research-only, explicitly not promoted to authority. HM Experienced, 10K Experienced, and 6D Experienced are all closed as non-product scope; HM-X1's 6D track proceeds targeting Intermediate and Advanced only. This decision may be reopened only by a future, dedicated product-decision phase presenting genuinely new evidence (a real distinguishing training-history input, an authored coaching rationale, or an explicit product-strategy reason) — not by any single distance's own implementation phase.
