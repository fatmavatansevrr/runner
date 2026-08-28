# PHASE 10K-GEN.13 — ProgressionStageAllocator Lane/Week-Eligibility Classification

**Parent authority**: `GEN.12` (`TWO_D_BEGINNER_INTERMEDIATE_CORE_IMPLEMENTED_AND_DARK_VERIFIED_PARTIAL`, §6's disclosed blocker)
**Phase type**: EVIDENCE + CLASSIFICATION (no implementation)
**Execution status**: DONE
**Final classification**: `DOMAIN_DECISION_REQUIRED` — escalated, unresolved. **Phase F does not proceed.**

---

## 0. Mandatory startup — completed

`PHASE_LEDGER.md`/`MASTER_ROADMAP.md` read; `GEN.12`'s full report re-read verbatim, specifically §6 ("Disclosed remaining blocker"). `git log -5`, `git fetch && diff HEAD origin/main` (in sync), `git status` clean except the pre-existing unrelated local modifications predating this session. Next free phase ID confirmed unique: `GEN.13`.

## 1. The question, restated precisely

`ProgressionStageAllocator.AllocatePhase` allocates progression-stage exposures for one lane across `phaseWeeks` — **every** calendar week the skeleton assigns to that phase, unconditionally (`availableWeeks = phaseWeeks.Count`; the contiguous-block layout walks `phaseWeeks[weekIndex]` sequentially for every index from 0 to `availableWeeks - 1`, never skipping one). For `RUN_LAYOUT_2D`'s Model B, lane 0 (`KEY_SESSION`) has a real structural slot in only the odd-numbered (Pattern A) weeks of a phase — the even-numbered (Pattern B) weeks have zero `KEY_SESSION` slots by design (`GEN.11` §1). The question is: **what should "phase capacity" mean for a lane that is only structurally present in half a phase's weeks — and does changing that meaning change what an athlete is actually prescribed?**

## 2. Classification work performed

Per this phase's own required framework, searched for an existing precedent before concluding either way — this is real investigation, not a default escalation:

- Read `ProgressionStageAllocator.cs` in full (584 lines). Confirmed the exposure/compression/extension math (`ApplyCompression`, `ApplyExtension`) operates entirely in terms of `availableWeeks` (raw calendar-week count for the phase) and produces a strict week-index walk with no gap/skip mechanism anywhere.
- Read the real, existing dual-KEY progression document (`ten-k-workout-progression.v7.json`, the one `TEN_K__5D__INTERMEDIATE`/`TEN_K__5D__ADVANCED`/`TEN_K__6D__*` combinations use) directly. Counted `laneOrdinal` declarations: exactly 4 phases × 2 lanes = 8, confirming both lane 0 and lane 1 are declared once per phase and each lane's own stage-exposure sum is expected to equal *that phase's full week count* — i.e., **every existing dual-KEY lane, in every existing frequency, is structurally present in 100% of its phase's weeks.** There has never been a case in this codebase, before 2D, where a lane's real structural slot count was less than the phase's calendar-week count.
- Confirmed `GEN.11`'s own report is silent on this exact question: its §9 (Adaptation) governs how the system *reacts* to what an athlete actually completed after the fact — a different concern from how `ProgressionStageAllocator` decides what to *prescribe* during initial generation. No existing `FREQ.*`/`GEN.*` report resolves progression-stage exposure pacing for a partial-lane-coverage frequency.

**Result: no precedent exists.** This rules out classification (a).

## 3. Why this is (b), not (a) — the real athlete-facing consequence

Reinterpreting what "phase capacity" means for lane 0 is not a code-organization choice — it directly determines **how many times, and in which real calendar weeks, an athlete is prescribed a given quality-session stage** (e.g. `FARTLEK_INTRO`, `THRESHOLD_INTRO`). Three genuinely different, real interpretations exist, each with different training consequences — see §4. None is dictated by `GEN.11`'s frozen decisions (Model B's structural pattern says nothing about exposure *pacing*), and picking one silently would be exactly the "reframe a real methodology call as an implementation detail" failure mode this phase was explicitly warned against.

## 4. Options (not implemented — presented for an actual decision)

### Option A — Reinterpret existing exposure math against the Pattern-A-only week subset
Capacity for lane 0 in a phase becomes "count of Pattern-A weeks in that phase," not the phase's total calendar-week count. The existing compression/extension algorithm's *shape* is reused unchanged; only its capacity input changes.
- **What it changes**: a phase like `FOUNDATION` (2-4 weeks) would offer only 1-2 real `KEY_SESSION` opportunities for 2D instead of 2-4 — the same catalog minimum-exposure values 3D/4D/5D/6D use were calibrated assuming 1 exposure per calendar week, so they very likely **do not fit** the halved denominator (e.g. a stage requiring "minimum 2 exposures" in a 2-week Foundation phase would now demand 2 exposures from as few as 1 available Pattern-A week, immediately tripping `ProgressionPhaseCapacityInsufficientException`).
- **What it preserves**: the contiguous-block-layout algorithm itself; the existing "minimum exposures is always honored, or the request fails closed" guarantee.
- **What it does NOT preserve**: the existing catalog's numeric minimum/maximum exposure values as directly reusable for 2D — in practice this option converges into requiring new, 2D-specific exposure numbers anyway (see Option B), so it is not really cheaper than Option B once the capacity-infeasibility failures are worked through; listed separately because it is the "reinterpret, don't re-author" framing someone might reach for first.
- **Beginner vs Intermediate**: identical mechanism; each level's own progression-stage catalog content would need independent recalibration (they already use different stage keys/workout definitions per modifier).

### Option B — Author new, 2D-specific progression-stage content calibrated for a Pattern-A-only cadence
A dedicated `2D` workout-progression variant (new catalog document) with minimum/maximum exposure values deliberately re-derived for "quality work every other week" rather than reused from the 100%-coverage catalog.
- **What it changes**: this is a real, new coaching/prescription decision — how much quality-stage exposure per phase is appropriate for an athlete who only gets a quality session every second week (not merely "the same amount, less often" — fewer total quality opportunities per phase may call for a *different* stage-progression shape, not just a smaller count of the same shape).
- **What it preserves**: the existing "minimum exposures always honored" safety guarantee (new content is calibrated to genuinely fit); the existing algorithm's mechanics untouched.
- **What it does NOT preserve**: "no new prescription-content authoring" — this is explicitly new catalog content requiring its own evidence basis, mirroring the rigor `GEN.7`/`GEN.8`/`GEN.11` already applied to every other piece of new numeric/content authority in this engagement (not something this phase can responsibly invent unilaterally).
- **Beginner vs Intermediate**: same mechanism; near-certainly *different* numeric content per level (their existing single-KEY progressions already differ).

### Option C — Keep literal calendar-week capacity; let a lane's minimum exposures go under-delivered when Pattern-B weeks reduce real availability
`availableWeeks` stays the phase's full calendar-week count (unchanged algorithm input), but the "every declared minimum must be honored or the request fails closed" guarantee is weakened to best-effort for 2D specifically — Pattern-B weeks simply don't receive a lane-0 assignment, and the stage sequence "catches up" or truncates as best it can.
- **What it changes**: real risk of silently under-delivering the catalog's own declared minimum quality-session exposure for a phase — a genuine training-quality regression, not merely a technical relaxation, since the whole point of `MinimumExposures` is a coaching-authored floor.
- **What it preserves**: no new catalog content required; smallest code change.
- **What it does NOT preserve**: the existing fail-closed safety guarantee (`ProgressionPhaseCapacityInsufficientException` currently exists precisely to prevent under-delivering a declared minimum) — weakening it specifically for 2D, silently, is the kind of change that could mask a real prescription defect for every future maintainer who doesn't know the guarantee was loosened.

**No option is recommended over another here** — each is a genuine, different training-methodology tradeoff, not a technical preference. Options A and C are lowest-effort but either likely fail immediately (A) or weaken an existing safety guarantee (C); Option B is the most defensible long-term but requires new evidence-grounded prescription-content authoring this phase has no standing to invent unilaterally.

## 5. Governance

No production code, tests, or catalog changes in this phase (classification only, per its own explicit scope). `PHASE_LEDGER.md` row appended recording the escalation itself — honestly, not hidden, matching `GEN.12`'s own `DONE (PARTIAL)` disclosure discipline. `MASTER_ROADMAP.md` updated to reflect the 2D axis's true current state: Core structurally implemented and dark-verified (`GEN.12`), workout-content binding blocked on an escalated, unresolved training-methodology decision.

**Phase F does not proceed.** Per this prompt's own explicit closing instruction, this report is returned to the user with the three options above for an actual decision before any further 2D implementation work continues.
