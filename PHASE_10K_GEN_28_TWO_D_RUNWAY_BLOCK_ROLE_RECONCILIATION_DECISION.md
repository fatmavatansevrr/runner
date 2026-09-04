# PHASE 10K-GEN.28 — 2D Preparation Runway: Block-Role/Anchor Reconciliation Decision

**Phase type**: DECISION / ARCHITECTURE_DESIGN / CATALOG-SEMANTIC ANALYSIS ONLY (no production code, no migration, no catalog authoring, no public activation, no test-suite regression run — nothing executable changes)
**Parent authority**: `GEN.27` (`TWO_D_PREPARATION_RUNWAY_REPEATING_PATTERN_MECHANISM_IMPLEMENTED_BLOCK_ROLE_RECONCILIATION_REQUIRED` — `DONE (PARTIAL)`, the direct predecessor whose disclosed blocking item this phase resolves), `GEN.26` (`TWO_D_RUNWAY_LONGHORIZON_PATTERN_CONTINUITY_AND_STRUCTURE_ARCHITECTURE_APPROVED` — frozen A/B structure, global ordinal), `GEN.19` (Runway repeating-pattern architecture gap, first confirmation), `GEN.11` (2D Model B authority, frozen), `FREQ.6D.11` (persisted-identity-over-recomputation precedent, reused as the lens for §4's progression-index question)
**Execution status**: DONE
**This phase is decision/architecture/catalog-semantic analysis only** — no production implementation, no LongHorizon implementation, no public activation, no invented workout content, no executable behavior change.

---

## 0. Preflight

`git log -5`: HEAD `8b43e60` (`docs(gen-27): backfill governance commit SHA for GEN.27`). `git fetch && git rev-list --left-right --count origin/main...HEAD`: `0 0` — in sync. `git status --porcelain`: only the pre-existing `bin`/`obj` rebuild noise, `baseline_tmp`, `plan-catalog/artifacts/audits/ten-k-pilot-domain-decision-audit.{json,md}`, and untracked `TestResults/*.trx` files predating this session — none touched by this phase. `PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read in full. `GEN.27` confirmed `DONE (PARTIAL)` in both. Next free Phase ID confirmed by direct listing of `PHASE_10K_GEN_*.md`: highest existing is `GEN.27`; no `GEN.28` file exists. **`GEN.28` confirmed correct.**

No executable behavior changed in this phase (verified: no `.cs`/catalog file staged).

---

## 1. Preservation of GEN.27

`GEN.27`'s repeating-pattern SELECTION mechanism (`Pattern[(weekNumber-1) % PatternPeriodWeeks]`, `WeeklyPatternRoles`/`PatternPeriodWeeks` on `PreparationRunwayCanonicalWeeklyLayout`, `IsValidTwoDayModelB`) is **not modified, not reopened, not reinterpreted** as failed work. Its null-default zero-delta behavior for every pre-GEN.27 (3D/4D/5D/6D/Advanced) layout is unchanged and not touched by this phase. The `NotSupportedException` in `TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(2)` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayWeekMaterialization/TenKPreparationRunwayWeekMaterializationPolicyFactory.cs:148-154`) is **not removed** in this phase — this phase produces the governed semantic resolution the guard was explicitly waiting for (§9), but the guard itself remains in place until a future implementation phase actually wires the resolution through; removing it here without real code behind it would turn a documented fail-closed guard into a silent wrong-answer path, exactly what `GEN.27 §3` and `GEN.12 §6`'s STOP discipline forbid.

---

## 2. Frozen 2D structure — confirmed respected

Pattern A = `{KEY_SESSION, LONG_RUN}`, Pattern B = `{EASY_SUPPORT, LONG_RUN}` (`GEN.11 §1`, re-verified in code: `TwoDayModelBPattern` in `TenKPreparationRunwayWeekMaterializationPolicyFactory.cs:43-47`). No hidden KEY was added to Pattern B anywhere in this phase's analysis. No candidate below coerces B into A, moves KEY-specific content onto LONG_RUN or EASY_SUPPORT to satisfy binding, or performs nearest-role matching — each candidate that would do so (Candidate A, Candidate D) is explicitly rejected or found invalid on exactly that basis. `StructuralRole` semantics (`PreparationRunwaySlotRole`, `PreparationRunwayBlockWorkoutBindingEngine`'s family-compatibility check in `PreparationRunwayWeekMaterializer.ValidateReferenceForRoleAsync`) remain truthful and unmodified.

---

## 3. Reconstructing the real failure — `2D_RUNWAY_BLOCK_ROLE_CONFLICT_MATRIX`

Traced directly from the real catalog (`plan-catalog/catalog/preparation-runway-progressions/*.json`, `plan-catalog/catalog/workouts/*.json`) and the real allocation policy (`TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies`, `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayEngine/TenKPreparationRunwayAllocationPolicyFactory.cs`), not from `GEN.27`'s prose summary.

**Block allocation shape** (`TenKPreparationRunwayAllocationPolicyFactory.BuildPolicies`, unchanged by `GEN.27`, shared by every frequency including 2D): four blocks, `CanonicalOrder` 1-4 — `Consistency` (0-2wk, conditional on `ConsistencyNeeded` profile), `GeneralEndurance` (1-5wk, always eligible), `AerobicStrength` (0-2wk, conditional on `CoreEntryReady` profile — mutually exclusive with Consistency), `PreSpecificTransition` (exactly 1wk, always eligible, `IsExpandable: false`). An 8-week Runway therefore always ends with a fixed 1-week `PreSpecificTransition` block, and its other 7 weeks split between (Consistency + GeneralEndurance) or (GeneralEndurance + AerobicStrength) depending on the resolved `PreparationRunwayAllocationProfile`.

**`2D_RUNWAY_BLOCK_ROLE_CONFLICT_MATRIX`**

| Block (`CanonicalOrder`) | Progression key/v | Block-relative step | `AnchorRoleByProgressionStep` | Workout candidate (key, family) | `eligiblePhases` | KEY-specific? | Equivalent LONG/EASY content exists? | Runtime consumer / current outcome for 2D |
|---|---|---|---|---|---|---|---|---|
| `Consistency` (1) | `TEN_K_CONSISTENCY_PROGRESSION` v1 | step 1 | `KeySession` | `EASY_STANDARD` v5, family `EASY` | (not phase-restricted) | **No** — genuinely EASY-family content, only *positionally* mapped to the `KeySession` role slot | Yes — `EASY_STANDARD` is the exact same workout `PreparationRunwaySupportWorkoutPolicy` already uses as the non-anchor `KeySessionDefault`/`EasySupportDefault` fallback | `PreparationRunwayWeekMaterializer.ValidateReferenceForRoleAsync`: `KeySession` role accepts family `EASY` or `QUALITY` — compatible with `KeySession`, **incompatible with `LongRun`** (requires family `LONG_RUN` exactly). This is the step `GEN.27` cites as disproving uniform LONG_RUN-anchor mapping. |
| `Consistency` (1) | `TEN_K_CONSISTENCY_PROGRESSION` v1 | step 2 | `LongRun` | `LONG_RUN_STANDARD` v5, family `LONG_RUN` | (not phase-restricted) | No | Already `LONG_RUN`-family — role-compatible everywhere | Compatible with `LongRun` role in every scenario; not a conflict source. |
| `GeneralEndurance` (2) | `TEN_K_GENERAL_ENDURANCE_PROGRESSION` v1 | steps 1-5 | `LongRun` (all 5) | `LONG_RUN_STANDARD` v5, family `LONG_RUN` (all 5) | (not phase-restricted) | No | Already `LONG_RUN`-family | Every step already anchors to `LongRun` — this is the block `GEN.27`'s real dark-verification test exercised, and it is why that test passed: `GeneralEndurance` has zero role-conflict potential by construction, independent of pattern parity. |
| `AerobicStrength` (3) | `TEN_K_AEROBIC_STRENGTH_PROGRESSION` v1 | step 1 (`_INTRO`) | `KeySession` | `AEROBIC_STRENGTH_CONTROLLED_INTRO` v1, family **`QUALITY`** | **`["PREPARATION_RUNWAY"]`** — genuinely Runway-owned, not reused from Core | **Yes** — `QUALITY` family, Runway-exclusive `eligiblePhases`, main-set `intensityDescriptor` = `CONTROLLED_AEROBIC_POWER_INTRO`; this is real evidence `GEN.26` Q2's "Runway-owned KEY-slot content" already exists in the catalog | None found — no `LONG_RUN`- or `EASY`-family workout in the catalog carries an equivalent controlled-aerobic-power stimulus | Compatible with `KeySession` only (`QUALITY` is accepted there, nowhere else). Forcing onto `LongRun` fails family check (`QUALITY` ≠ `LONG_RUN`); forcing onto `EasySupport` also fails (`EasySupport` requires family `EASY` exactly, not `QUALITY`). |
| `AerobicStrength` (3) | `TEN_K_AEROBIC_STRENGTH_PROGRESSION` v1 | step 2 (`_PROGRESSED`) | `KeySession` | `AEROBIC_STRENGTH_CONTROLLED_PROGRESSED` v1, family `QUALITY` (verified same shape as v1 `_INTRO`) | `["PREPARATION_RUNWAY"]` | **Yes** — same reasoning as step 1 | None found | Same conflict as step 1; both `AerobicStrength` steps are `KeySession`-only content with no `LongRun`/`EasySupport` fallback anywhere in the catalog. |
| `PreSpecificTransition` (4) | `TEN_K_PRE_SPECIFIC_TRANSITION_PROGRESSION` v1 | step 1 | `KeySession` | `EASY_STANDARD` v5, family `EASY` | (not phase-restricted) | **No** — genuinely EASY-family content, only positionally mapped to `KeySession` | Yes — identical to the `Consistency` step-1 case | Compatible with `KeySession` (`EASY` accepted); incompatible with `LongRun`. Because `PreSpecificTransition` is fixed at exactly 1 week (`MaxWeeks: 1`, `IsExpandable: false`), this block's single step lands on whatever the final Runway week's global parity happens to be — not guaranteed Pattern A. |

**Key finding not previously disclosed at this level of precision**: only `AerobicStrength`'s two steps are genuinely, irreducibly `KEY_SESSION`-specific content (real `QUALITY`-family, Runway-exclusive workouts with no substitute anywhere in the catalog). `Consistency` step 1 and `PreSpecificTransition` step 1 are positionally mapped to `KeySession` but are themselves `EASY_STANDARD` — the *exact same workout* already used as the materializer's own non-anchor support-policy default for that role. `GeneralEndurance`'s entire progression is already `LongRun`-anchored and has zero conflict potential with 2D's pattern at all. This means the real, irreducible content gap is narrower than `GEN.27`'s prose ("2D Runway anchor reconciliation is blocked") implies at first read: it is concentrated specifically in the `AerobicStrength` block (2 of 13 total block-progression steps across all four blocks), not spread evenly across the whole Runway.

---

## 4. What "progression" is indexed by — determined from repository evidence, not inferred

**Answer: (B) block-local week — with a hard-enforced invariant, not merely a convention.**

Traced through the real code, not assumed from collection position:

- `PreparationRunwayBlockWorkoutBindingEngine.Bind` (§ "Step 5: select exact prefix"): `canonicalSteps.Take(request.AllocatedWeeks)` — selects the first N progression steps by `StepNumber`, where N = the block's resolved `AllocatedWeeks` (a block-local week count, computed upstream by the allocation engine from weights/min/max, with zero knowledge of global week parity).
- `PreparationRunwayWeekMaterializer.ValidateRequest` (line 275, verbatim): `bindingInput.OrderedProgressionStepNumbers.SequenceEqual(Enumerable.Range(1, allocation.AllocatedWeeks))` — this is a **hard-enforced invariant** in the shared, generic (`<TKey>`) materializer used by every frequency, not merely an authoring convention. Step 1's content is deterministically materialized in the block's 1st week, step 2's in the block's 2nd week, and so on, with no gaps and no reordering permitted.
- `PreparationRunwayWeekMaterializer.MaterializeAsync`'s main loop (line 55): `var progressionStep = bindingInput.OrderedProgressionStepNumbers[blockWeekOrdinal - 1];` — `progressionStep` is read directly from `blockWeekOrdinal`, the block-relative week counter (`for (var blockWeekOrdinal = 1; blockWeekOrdinal <= allocation.AllocatedWeeks; blockWeekOrdinal++)`), never from a count of how many times a given `StructuralRole` has occurred.

**Direct consequence for the central question**: the nth progression item unambiguously means **"week n of this block,"** not "nth KEY exposure in this block." There is no code path anywhere in `PreparationRunwayBlockWorkoutBindingEngine`, `PreparationRunwayBlockProgressionCatalogReader`, or `PreparationRunwayWeekMaterializer` that counts role occurrences; `StructuralRole` is resolved *after* progression-step selection (in the materializer's per-slot loop), never *during* it. This holds for every existing frequency (3D/4D/5D/6D/Advanced), where it has never been ambiguous because every week of every existing layout has exactly one `KEY_SESSION` slot — "week n" and "nth KEY exposure" have always coincided by construction, so this distinction was invisible until 2D introduced a frequency where a block-local week can have zero `KEY_SESSION` slots.

This finding is authoritative input to every candidate evaluated below.

---

## 5. Candidate A — sparse anchor application

**Definition** (as scoped): Pattern A weeks consume/apply the block's KEY-specific anchor; Pattern B weeks materialize no KEY anchor, with EASY and LONG receiving only role-compatible (support-policy default) content.

- **Does it preserve the intended block objective?** No, for `AerobicStrength` specifically. That block exists to deliver exactly 2 controlled-aerobic-power stimulus sessions (`_INTRO` then `_PROGRESSED`) to a core-entry-ready athlete. Under sparse application, whichever of the block's 2 (block-local) weeks lands on global-Pattern-B receives no aerobic-strength stimulus at all — the block's defining content is silently and permanently dropped for that week, not deferred, because §4 established the progression index is block-local-week-bound: step 2's content is tied to block-local week 2, period, regardless of what global parity that week has.
- **Does skipping anchor application change the progression's meaning?** Yes, materially. Per §4, "step n" already means "delivered in block-local week n" for every other frequency — a guarantee that is unconditional today. Sparse application would make that guarantee conditional on global week parity for 2D only, silently reducing block-progression completion from 100% (guaranteed) to a data-dependent fraction (typically ~50%, but not exactly 50% because parity alignment depends on preceding blocks' total allocated weeks, which vary by `PreparationRunwayAllocationProfile` and by however many weeks each expandable block's own allocation engine assigns). This is a genuine, undisclosed-until-now semantic weakening specific to 2D, not a neutral reinterpretation.
- **Does the progression index advance on skipped B weeks?** Yes — per §4's hard invariant (`OrderedProgressionStepNumbers` is always `1..AllocatedWeeks`, never renumbered), the block-local counter keeps incrementing regardless of pattern, so a skipped step is not "saved for later" — it is permanently lost. A future implementation could not simply "carry it forward," because doing so would violate the same hard-enforced sequential invariant §4 identified, requiring a second, separate architecture change beyond what Candidate A as scoped describes.
- **Can existing content represent Pattern B without inventing new content?** Yes, trivially — Pattern B's `EasySupport`+`LongRun` slots are already satisfiable by the existing `EASY_STANDARD`/`LONG_RUN_STANDARD` support-policy defaults, exactly as they are for every non-anchor slot today.
- **Does it work for both levels?** Structurally yes (Beginner/Intermediate share Model B, `GEN.11 §1`), but the *severity* differs: `AerobicStrength` is only eligible under the `CoreEntryReady` allocation profile (`aerobicStrengthEligible = profile == CoreEntryReady`), so the athlete population affected is identical at both levels — this candidate's flaw is level-independent.
- **Does it create inconsistency with 3D-6D semantics?** Yes, directly: every other frequency's Runway delivers 100% of every eligible block's designed progression content as an anchor, exactly once, by the same §4 invariant. 2D under Candidate A would be the only frequency where a block's own progression completion rate is non-deterministic and load-bearing training content (the `AerobicStrength` block's entire reason for existing) is silently, permanently omitted roughly half the time, with no signal to the athlete, the catalog author, or any validator that this happened.

**Classification: `INVALID`.** Not merely undesirable — it violates the already-established, hard-enforced progression-index semantics from §4 (silent, non-deterministic content loss) and produces a materially different, unauthorized athlete experience with no supporting product decision. This is a genuine finding, not a stylistic preference: `GEN.11`/`GEN.26` froze that Runway's *structure* differs from Core only in KEY-slot prescription intensity, never that a block's own progression could complete at less than 100% depending on incidental global-parity alignment.

---

## 6. Candidate B — role-occurrence progression

**Definition** (as scoped): KEY progression advances only when a structural KEY occurrence exists in the current pattern (A1→item 1, B1→none, A2→item 2, B2→none...).

- **Does this reflect the actual semantic authority of existing Runway progressions?** No — §4 directly contradicts it. The shared, generic `PreparationRunwayWeekMaterializer.ValidateRequest` enforces `OrderedProgressionStepNumbers == Range(1, AllocatedWeeks)` as a hard invariant for **every** frequency through the same code path 2D would have to share (the materializer and binder are generic over `TKey`, not frequency-specialized). Progression-step numbering is defined as a pure block-local week count everywhere it is used today; there is no existing "role occurrence" counter anywhere in `PreparationRunwayBlockWorkoutBindingEngine`, `PreparationRunwayBlockProgressionCatalogReader`, or `PreparationRunwayWeekMaterializer` to repurpose.
- **Compatibility check across catalog schema, authoring rules, validators, projectors, runtime materializers, persistence, JIT continuation, repair, restart**: every one of these — `PreparationRunwayBlockProgressionCatalogReader` (reads `stepOrder` as a flat sequence), `PreparationRunwayBlockWorkoutBindingEngine.Bind` (`canonicalSteps.Take(AllocatedWeeks)`), `PreparationRunwayWeekMaterializer` (the `1..AllocatedWeeks` invariant) — assumes and enforces block-local-week indexing. Introducing occurrence-based semantics for 2D specifically would require either (a) forking this shared, generic pipeline's core invariant conditionally on `daysPerWeek == 2`, directly contradicting `GEN.27`'s own zero-delta-for-other-frequencies discipline and this engagement's standing "no per-frequency special-case fork in shared code" convention, or (b) redefining the invariant globally, which is out of scope and unjustified (every other frequency's semantics are correct and unambiguous as-is, per §4's own observation that "week n" and "nth KEY exposure" have always coincided there).
- **Is this compatible with the meaning of existing content?** No — `Consistency`/`PreSpecificTransition`'s `KeySession`-anchored steps are literally `EASY_STANDARD` (§3), authored under a "this step occupies this positional week" assumption, not a "this is the block's nth quality exposure" assumption. Re-indexing by occurrence would silently change what "step 1" means for those two blocks specifically (an even more confusing outcome than Candidate A, since it would make `AerobicStrength`'s two genuinely KEY-specific steps behave differently under re-indexing than `Consistency`'s and `PreSpecificTransition`'s superficially-KEY-anchored-but-actually-EASY steps).

**Classification: `INVALID`.** This is not introduced merely because it would solve 2D in isolation (the task's own caution against exactly that trap is heeded) — it is incompatible with the real, hard-enforced, shared invariant §4 traced directly from code, and would require a special-cased fork of generic, cross-frequency infrastructure with no precedent and no independent justification.

---

## 7. Candidate C — explicit 2D alternating content

**Definition** (as scoped): author explicit role-compatible Runway content for Pattern A and Pattern B; no authoring in this phase — classify the gap only.

**A concrete, previously-undisclosed mechanism check performed this phase**: `PreparationRunwayBlockProgressionCatalogReader.ReadStep` reads a step's `workoutCandidates` JSON array (plural by schema — every existing progression document already declares it as an array) but hard-selects only the first element: `candidatesEl.EnumerateArray().First()`. The plural schema shape already exists; the *selection logic* to choose among multiple candidates by resolved role does not. This is real, direct evidence bearing on the gap classification, not inferred:

- **Are existing `WorkoutDefinitions` sufficient?** Partially. `Consistency` step 1 and `PreSpecificTransition` step 1 need no new content — their existing `EASY_STANDARD` reference is already `EasySupport`-role-compatible; a Pattern-B-week candidate could reuse it verbatim. `GeneralEndurance` needs nothing (already `LongRun`-anchored throughout, zero conflict). **`AerobicStrength`'s two steps have no existing role-compatible substitute anywhere in the catalog** (§3) — a genuine content gap, not merely a selection-logic gap.
- **Are existing `WorkoutPrescriptionProfiles` sufficient?** Not evaluated as capable of resolving this by themselves — prescription profiles govern numeric dosage/intensity parameters for an already-selected workout, not which workout family occupies a structural slot; they cannot substitute a `QUALITY`-family session for an `EASY`- or `LONG_RUN`-family requirement.
- **Are new `WorkoutDefinitions` required?** Yes, specifically for `AerobicStrength`'s Pattern-B case: either (a) an `EASY`-family variant of the controlled-aerobic-power stimulus (if one is judged trainingly meaningful — a real coaching-methodology question this phase has no standing to answer), or (b) a decision that `AerobicStrength`'s Pattern-B weeks simply receive the standard `EASY_STANDARD` default with no aerobic-strength-specific content at all (i.e., the block's stimulus is only ever delivered on whichever of its two weeks lands on Pattern A — different from Candidate A's blanket sparse-application because it would be a *deliberate, authored, disclosed* reduction for this one block only, not a silent universal one, and would require its own explicit product sign-off).
- **Is a new progression artifact/version required?** Yes if role-conditioned candidate selection is the chosen mechanism: `PreparationRunwayBlockProgressionCatalogReader` would need new selection logic (reading beyond `.First()`, keyed by resolved role), and each progression document's schema-already-plural `workoutCandidates` array would need to be populated with role-tagged entries — a new document version per progression, plus new binder-engine logic to receive the resolved role (which today is computed in `PreparationRunwayWeekMaterializer`, *after* binding already happened in `PreparationRunwayBlockWorkoutBindingEngine.Bind`) — a real, non-trivial call-order restructuring, not a cosmetic change.

**Classification: content gap for `AerobicStrength` is real and requires a genuine coaching/product decision (partially `DOMAIN_DECISION_REQUIRED`: whether an EASY-family aerobic-strength variant is trainingly meaningful, or whether the block deliberately delivers its stimulus only on its Pattern-A week); the `Consistency`/`PreSpecificTransition` cases require no new content, only new role-conditioned selection code (mechanical, `IMPLEMENTATION_ONLY`) since their existing `EASY_STANDARD` reference already satisfies Pattern B.** This candidate is architecturally compatible with §4's block-local-week indexing (it does not reinterpret the index at all — it changes *which workout* a given block-local week's already-fixed step resolves to, conditioned on that week's already-resolved pattern), which is the property that distinguishes it from Candidates A and B.

---

## 8. Candidate D — role-independent anchor

**Definition** (as scoped, not preferred a priori): a single "development anchor" content model independent of structural role, mapped onto whichever role is present each week.

Direct catalog evidence (§3) settles this decisively: `AEROBIC_STRENGTH_CONTROLLED_INTRO`/`_PROGRESSED` are `family: "QUALITY"`, `eligiblePhases: ["PREPARATION_RUNWAY"]` — genuinely `KEY_SESSION`-specific content (real, Runway-exclusive, controlled-intensity quality-family work), not a role-agnostic "development" concept that happens to be labeled `KEY_SESSION` today. `GEN.26` Q2's own finding ("Runway-owned KEY-slot content already exists in the catalog") is independently reconfirmed here by reading the workout documents directly, not merely cited.

**Classification: `REJECTED`, not merely "not preferred."** The real content requires `KEY_SESSION`/`QUALITY` semantics specifically; erasing that distinction to simplify the runtime architecture would misrepresent genuinely different training stimuli (a controlled-aerobic-power quality session vs. an easy-pace long run) as interchangeable, directly contradicting §2's frozen truthful-`StructuralRole` requirement. `Consistency`/`PreSpecificTransition`'s superficially-KEY-anchored-but-actually-`EASY_STANDARD` steps do not rescue this candidate either — they show that *some* current KEY-slot content happens to be role-flexible, not that all of it is, and `AerobicStrength`'s content proves the opposite for the one block that matters most.

---

## 9. Selected canonical model

**`DOMAIN_DECISION_REQUIRED`** for the `AerobicStrength` content question specifically; **`2D_RUNWAY_BLOCK_ROLE_RECONCILIATION_APPROVED`** for the mechanism/architecture question.

Stated precisely, because the task correctly anticipates that a single clean answer may not cover every sub-question:

1. **Architecture/mechanism**: Candidate C (explicit, role-conditioned Pattern-A/Pattern-B content selection per block-local week, keyed off the already-resolved weekly pattern) is the approved direction — the minimal model consistent with §4's real, hard-enforced block-local-week progression-index semantics, the only candidate not independently found `INVALID`/`REJECTED` by direct code and catalog evidence (Candidates A and B fail §4's invariant or produce silent content loss; Candidate D is rejected by direct catalog content). This is a direct-canonical-rule outcome, not a convenience choice: A and B are excluded because they conflict with already-established, load-bearing invariants (§4, §5, §6), not because C was merely preferred.
2. **Content**: for `Consistency` and `PreSpecificTransition`, the reconciliation is **already mechanically closed by existing content** — both blocks' `KeySession`-anchored steps are `EASY_STANDARD`, already role-compatible with Pattern B's `EasySupport` slot verbatim; only new selection *code* (not new catalog authoring) is required.
3. **Content**: for `AerobicStrength` specifically, the reconciliation is **`DOMAIN_DECISION_REQUIRED`** — whether to (a) author a genuinely new `EASY`-family aerobic-strength-adjacent workout for Pattern-B weeks (a real training-methodology question about whether a lower-intensity variant preserves the block's intent), or (b) accept that `AerobicStrength`'s stimulus is deliberately, visibly delivered only on the block's Pattern-A week(s) — a real, disclosed, bounded reduction affecting exactly one block (unlike Candidate A's blanket, silent, cross-block reduction), which would itself need explicit product sign-off before implementation, or (c) reconsider `AerobicStrength`'s own block-local allocation shape for 2D so its 1-2 weeks are steered toward Pattern-A alignment specifically (a variant not enumerated in the task's A-D list, noted here for completeness but not evaluated in depth, since inventing a new candidate is outside this phase's charter to decide unilaterally). This phase does not choose among (a)/(b)/(c) — that choice requires product/coaching judgment this phase has no standing to invent, exactly the STOP discipline `GEN.19`/`GEN.27`/`GEN.12 §6` already established for structurally identical situations.

**Reason**: per the task's own decision standard (direct canonical rule > existing catalog authoring semantics > existing deterministic runtime behavior > strong precedent, never implementation convenience) — §4's direct-code-traced progression-index semantics is the direct canonical rule that eliminates Candidates A and B; existing catalog authoring semantics (the `EASY_STANDARD` content already present for 2 of the 3 affected blocks) closes most of the mechanism question without invention; only the one block whose content is irreducibly `KEY_SESSION`-specific (`AerobicStrength`, confirmed by reading the actual `QUALITY`-family, Runway-exclusive workout documents) genuinely lacks existing authority and cannot be closed without a real product decision.

---

## 10. Beginner admission defect

**Classification: `BEGINNER_2D_RUNWAY_ADMISSION_GATE_IMPLEMENTATION_DEFECT`.**

`TenKPreparationRunwayDarkOrchestrator`'s admission gate (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayOrchestration/TenKPreparationRunwayDarkOrchestrator.cs:342`): `request.Candidate.Level is not ("INTERMEDIATE" or "ADVANCED")` throws/rejects, unconditionally excluding Beginner. Checked directly against existing authority: `GEN.11 §4` explicitly froze "Preparation Runway (15-20wk): `SUPPORTED` for both levels" (Beginner and Intermediate, identically, Model B being "frequency-owned, shared identically by Beginner and Intermediate," `GEN.11 §1`). `GEN.26` re-verified and extended this authority (Q1-Q3) without narrowing it to Intermediate only. **No authority is missing** — the product/domain decision that Beginner×2D Runway is supported already exists and was never conditioned on Level the way this gate implies. This is a pure code defect: the gate's `Level` filter simply never accounted for Beginner as a valid caller shape (the same "not every caller shape considered" family `GEN.10` first found, `GEN.27 §1` correctly classified this way already — this phase reconfirms that classification, does not reinterpret it, and does not treat it as evidence of a Beginner product non-support decision that was never made). Added to the next implementation contract (§14, item 3).

---

## 11. Numeric policy dispatch

**Classification: `AUTHORITY_COMPLETE_IMPLEMENTATION_MISSING`, for both Beginner and Intermediate.**

`TenKPreparationRunwayNumericPolicyFactory.Build` (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayNumericMaterialization/TenKPreparationRunwayNumericPolicyFactory.cs:32`, switch on `(CanonicalDistanceFamily, Level, DaysPerWeek)`) has no `("TEN_K", *, 2)` branch — confirmed still absent, unchanged since `GEN.19`/`GEN.27`. The underlying numeric values it would dispatch to are **not missing**: `VolumeSafetyPolicy.ForBeginnerDaysPerWeek(2)`/`ForIntermediateDaysPerWeek(2)` already correctly resolve to `Beginner2D`/`Intermediate2D` (`GEN.11 §2-3, §6`'s frozen `PeakVolumeBand`, `ResolvedPeakReference`, `LongRunPreferredMinimumShare=0.55`/`LongRunHardCapShare=0.60` — re-verified present in `VolumeSafetyPolicy.cs` lines 301-342 per `GEN.19 §1`/`GEN.26 §3.2`'s own direct citation). This is exactly a missing dispatch *branch*, not a missing *value* — a mechanical, low-risk addition once §9's block-role reconciliation is closed enough to give this dispatch a real caller to verify against (per `GEN.27 §4` item 2's own reasoning, reconfirmed correct here: wiring a numeric policy with no reachable caller cannot be honestly dark-verified).

---

## 12. Calendar / long-run wiring

- **`PreparationRunwayCalendarComposer`/`PreparationRunwayCalendarSkeletonAdapter`** (`backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunwayCalendarComposition/`): direct inspection this phase found no hardcoded slot-count assumption (e.g., no `Count == 4` or `easyCount`-style literal) blocking a 2-slot week structurally — `DaysPerWeek = prescribed[0].OrderedSlots.Count` (`PreparationRunwayCalendarSkeletonAdapter.cs:64`) derives the count generically from the actual slot collection, and `GEN.11 §9` already froze that 2D's Pattern-A `KeySession`↔`LongRun` spacing (the stricter of the two possible role pairings) automatically satisfies Pattern-B's looser `EasySupport`↔`LongRun` spacing using the existing, unmodified `MinimumKeySessionToLongRunSeparationDays` constant — no new spacing authority needed. **Classification: `IMPLEMENTATION_ONLY`** (untested for a real 2-slot week end-to-end, per `GEN.27 §4` item 4's own disclosure, but no discovered hardcode blocks it structurally — this is a verification gap, not a code-defect or domain gap).
- **Long-run share clamp (55%/60%) wiring through Runway's own numeric path**: not verified this phase (unchanged from `GEN.27 §4` item 5) — blocked exclusively on §11's missing dispatch branch, since `CatalogVolumeAndLongRunPlanner.BuildLongRunPlan` needs a resolved `VolumeSafetyPolicy` instance to clamp against, and no 2D Runway caller can reach it until the numeric factory dispatches to `Beginner2D`/`Intermediate2D`. **Classification: `IMPLEMENTATION_ONLY`** — the values exist (`GEN.11 §6`, re-confirmed unmodified), only the wiring path is unbuilt; not an `ARCHITECTURE_BLOCKER` (the mechanism itself, `CatalogVolumeAndLongRunPlanner`, is frequency-agnostic and already correctly evaluates a 2-session week per `GEN.26 §3.2`'s own direct trace) and not a `DOMAIN_DECISION_REQUIRED` (no new number or rule is needed).

Existing 2D calendar and long-run authority (`GEN.11 §6, §9`) is preserved, not touched.

---

## 13. LongHorizon dependency — start-gate condition

**Not implemented, not started, this phase.** LongHorizon may begin its own dedicated design/implementation phase only when **all** of the following hold, matching the task's specified minimum gate exactly:

1. Runway deterministically materializes every 2D A/B week with semantically valid, role-compatible content for **all four** blocks (`Consistency`, `GeneralEndurance`, `AerobicStrength`, `PreSpecificTransition`) — i.e., §9's `AerobicStrength` `DOMAIN_DECISION_REQUIRED` item is resolved and implemented, and §9's `Consistency`/`PreSpecificTransition` mechanical selection-logic gap is implemented.
2. Runway does not throw `TenKPreparationRunwayWeekMaterializationPolicyFactory`'s `NotSupportedException` for any supported 2D case (i.e., item 1 is fully wired, not merely decided).
3. Beginner/Intermediate routing reaches the correct Runway path (§10's admission-gate defect is fixed).
4. `TenKPreparationRunwayNumericPolicyFactory`'s 2D dispatch exists and is dark-verified with a real caller (§11).
5. Calendar composition and long-run clamp wiring are real-verified for a 2-slot week, not merely judged structurally unblocked (§12).

Only once all five hold may a future LongHorizon 2D phase consume Runway as its downstream handoff — consistent with `GEN.19 §6`/`MASTER_ROADMAP.md`'s own standing estimate that 2D LongHorizon additionally requires replicating a meaningful fraction of `FREQ.6D.11`-`22`'s real-PostgreSQL rolling-activation/persistence/adaptation/repair effort, a separate, later scope this phase does not shrink or expand.

---

## 14. Implementation contract (partial — mechanism approved, one content item pending product decision)

Since §9 is **not** a clean `2D_RUNWAY_BLOCK_ROLE_RECONCILIATION_APPROVED` across the board (the `AerobicStrength` content sub-question remains `DOMAIN_DECISION_REQUIRED`), this is disclosed as a **combined contract with one explicitly-marked pending item**, not a fully green-lit implementation wave:

1. **Role-conditioned progression-step selection mechanism** (Candidate C, mechanism half): extend `PreparationRunwayBlockProgressionCatalogReader.ReadStep` to read beyond `workoutCandidates[0]`; extend `PreparationRunwayBlockWorkoutBindingEngine.Bind`'s signature (or a new wrapping adapter) to accept the block-local week's already-resolvable weekly pattern (computable from `ResolveWeekRoles` once block-local-to-global week offset is threaded through — a real, disclosed call-order change, since binding currently precedes per-week role resolution); select the role-compatible candidate deterministically. Zero-delta for 3D/4D/5D/6D/Advanced (single-candidate blocks resolve identically regardless of the new selection logic).
2. **`Consistency`/`PreSpecificTransition` content**: no new catalog authoring — populate each affected step's `workoutCandidates` array with an explicit second, Pattern-B-tagged entry referencing the already-existing `EASY_STANDARD` v5 (same key/version already used).
3. **`AerobicStrength` content**: **blocked on the pending product decision** (§9 item 2) — do not implement until (a)/(b)/(c) is chosen.
4. **Beginner admission gate** (§10): widen `TenKPreparationRunwayDarkOrchestrator`'s `Level is not (...)` check to admit Beginner, mirroring the existing Intermediate/Advanced path exactly.
5. **2D numeric dispatch** (§11): add the `("TEN_K", "NEW"/"INTERMEDIATE", 2)` branch(es) to `TenKPreparationRunwayNumericPolicyFactory.Build`, dispatching to `VolumeSafetyPolicy.Beginner2D`/`Intermediate2D`.
6. **Calendar composition** (§12): real dark-verification of a 2-slot Runway week through `PreparationRunwayCalendarComposer`/`Adapter` end-to-end.
7. **Long-run clamp wiring** (§12): real dark-verification that the 55%/60% share clamp is applied within the Runway numeric path once item 5 closes.
8. **A/B deterministic tests**: both patterns' full weekly materialization, including the newly-resolved anchor content.
9. **No-KEY-forced-onto-EASY/LONG tests**: regression-guard the family-compatibility rejections this phase's matrix (§3) documents.
10. **Beginner×2D and Intermediate×2D dark tests**: full Runway materialization for both levels once item 4 closes.
11. **Zero-delta tests**: every pre-existing fixed weekly layout (3D/4D/5D/6D/Advanced), re-confirmed byte-identical, matching `GEN.27`'s own existing test.

Not implemented now — this section is a contract for a future phase, contingent on §9's pending product decision for `AerobicStrength`.

---

## 15. Test contract (for the future implementation phase)

Required, at minimum: Pattern A Runway materialization; Pattern B Runway materialization (including the resolved `AerobicStrength` outcome, whichever of §9's (a)/(b)/(c) is chosen); no KEY content forced onto `EASY_SUPPORT`; no KEY content forced onto `LONG_RUN`; progression ordinal/index semantics (block-local-week, per §4, re-asserted as a regression guard so a future change cannot silently drift toward occurrence-based indexing); block boundary correctness; A/B boundary correctness; Beginner admission; Intermediate admission; numeric-policy dispatch; calendar composition for a 2-slot week; long-run clamp application; pre-existing (3D/4D/5D/6D/Advanced) layouts' null-pattern zero-delta. Occurrence-based progression was found `INVALID` (§6) and is not approved, so no restart/JIT/repair determinism test for an occurrence ordinal is required.

---

## 16. Success standard — restated, not claimed complete

This phase's success is that the semantic relationship between Runway block, progression content, `StructuralRole`, A/B pattern, and progression ordinal is now **fully deterministic and approved for 3 of 4 blocks**, with the 4th (`AerobicStrength`) narrowed to a single, well-scoped, explicitly-disclosed product decision rather than an open architecture question. **This does not mean Runway is implemented** (§14 is a contract, not code) and **does not mean LongHorizon may be considered complete or started** (§13's five-item gate is unmet).

---

## 17. Governance

`PHASE_LEDGER.md` row appended (`GEN.28`). `MASTER_ROADMAP.md`'s 2D backlog item updated to record this phase's findings, superseding `GEN.27`'s "block-role reconciliation is the next blocking item" framing with the narrower, evidence-based picture this phase establishes (3 of 4 blocks mechanically closed; `AerobicStrength` alone requires a product decision). `GEN.27`'s own `DONE (PARTIAL)` classification and text are preserved verbatim, not rewritten as failure. Two-commit self-referential SHA-backfill pattern followed. Normal push only — no force, no force-with-lease.

---

## Final classification

**`DOMAIN_DECISION_REQUIRED`** (scoped precisely to the `AerobicStrength` Pattern-B content question, §9 item 2) **combined with `2D_RUNWAY_BLOCK_ROLE_RECONCILIATION_APPROVED`** for the reconciliation mechanism and for `Consistency`/`GeneralEndurance`/`PreSpecificTransition`'s block-role content, which required no invented architecture and are fully closed by existing catalog content plus a mechanical selection-logic addition. Not a forced clean answer across the whole gap — the one genuinely irreducible sub-question is named exactly, narrowly, and left open for its own dedicated product decision, per this engagement's standing discipline.
