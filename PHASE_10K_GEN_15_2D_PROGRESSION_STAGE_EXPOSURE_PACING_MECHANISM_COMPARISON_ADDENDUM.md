# PHASE 10K-GEN.15 — 2D Progression-Stage Exposure Pacing: Mechanism Comparison Addendum

**Parent authority**: `GEN.14` (`TWO_D_PROGRESSION_STAGE_EXPOSURE_PACING_AUTHORITY_PROPOSED_PENDING_SIGNOFF`, commits `8beaf1c`/`5244ade`)
**Phase type**: EVIDENCE + PROVISIONAL NUMERIC/POLICY AUTHORITY (addendum) — **no production code**
**Execution status**: DONE
**Final classification**: `TWO_D_PROGRESSION_STAGE_EXPOSURE_PACING_AUTHORITY_PROPOSED_PENDING_SIGNOFF` — **unchanged from `GEN.14`: Halving remains the proposed mechanism, now comparatively justified against two real alternatives rather than presented as the only option considered. Still provisional, still pending human sign-off, still blocking Phase F.**

---

## 0. Startup

`GEN.14`'s report re-read in full. `git log -3`, `git fetch && diff HEAD origin/main` (in sync), `git status` clean except the pre-existing unrelated local modifications predating this session. Next free phase ID confirmed unique: `GEN.15`. This addendum does not re-run the evidence search — `GEN.14`'s source verification stands (Slettaløkken & Rønnestad 2014; Spiering et al. 2021; the confirmed injury-risk and progression-evidence-gap findings). It re-examines what those same sources say about the *mechanism* question specifically, which `GEN.14` had not isolated as its own comparison.

## 1. The three mechanisms, evaluated against the same evidence base

### (1) Halving — `ceil(weekly/2)` applied to both min and max exposure (`GEN.14`'s proposal)

**What it changes**: a stage that would require, say, 2-3 exposures across 4 weekly-cadence weeks now requires 1-2 exposures across the ~2 Pattern-A weeks that same phase length actually offers. Total *quality-session opportunities per phase* is halved; per-session content (which workout, what dose) is untouched — that remains `GEN.11`'s/the existing catalog's own concern, unaffected by this mechanism.

**Evidence fit**: this is not merely consistent with the evidence — it is the literal *shape* of the closest real analog. Slettaløkken & Rønnestad's own design held session content, intensity, and the calendar window (6 weeks) completely fixed, and manipulated *only* total session count (6 sessions vs. 3 — an exact halving) within that fixed window. The result (equivalent maintenance outcome) is direct support for "half the total count, same calendar window, same per-session content" as a defensible default — which is exactly Halving's shape, not an analogy stretched to fit it.

### (2) Duration extension — same per-session exposure dosing, extend how many phase-weeks are needed to satisfy a stage

**What it changes**: instead of reducing how many quality exposures a stage requires, this would extend a phase's *real calendar length* so the original weekly-cadence exposure count is still fully delivered, just spread over roughly double the time.

**Evidence fit — argues against, on two independent grounds:**
- **Evidentiary**: no source in `GEN.14`'s base tests this combination. Slettaløkken & Rønnestad's biweekly group did *not* get a longer window to compensate for fewer sessions — they trained the identical 6 weeks as the weekly group and simply received fewer total sessions, and that was sufficient for maintenance. The literature's own natural analog is "fewer sessions, same duration," not "same sessions, longer duration." If anything, this is mild evidence *against* needing a compensating time extension at all.
- **Authority conflict, independent of the evidence question**: `GEN.11` §4/§14 (re-confirmed unchanged through `GEN.12`'s `TEN_K_MASTER v11`, which carries the identical `coreCycle` — `minimumWeeks:8, defaultWeeks:12, maximumWeeks:14` — as the pre-existing single-lane lineage) already froze the Core-cycle *calendar-week* length as identical across every frequency, 2D included. Since a plan's total week count is fixed, extending one phase's real length to preserve its exposure count would require shrinking another phase to compensate — a full re-derivation of phase-length allocation specific to 2D that `GEN.11` never approved and explicitly did not intend (it reused the existing phase structure unchanged). Pursuing this mechanism would reopen already-locked authority, which is out of this phase's scope by construction — a disqualifying problem on its own, separate from whatever the physiological evidence says.

### (3) Min-only reduction — reduce the minimum, leave the maximum unchanged

**What it changes**: a stage's floor drops to fit the halved week count, but its ceiling stays at the original weekly-cadence value, on the stated reasoning that a less-frequent session may tolerate a fuller dose when it occurs.

**Evidence fit — argues against, again on two grounds:**
- **Wrong layer of the system**: `MinimumExposures`/`MaximumExposures` in `ProgressionStageAllocator`'s catalog govern *how many times* a stage's workout is scheduled within a phase — not how large or hard any single session is. The physiological idea behind this option (a less-frequent session absorbing a fuller *per-session* dose) is a real, separate question already owned by a different part of the system — the volume/long-run planner (`VolumeSafetyPolicy.Beginner2D`/`Intermediate2D`, already frozen by `GEN.11`) and the workout definitions' own dose parameters. Leaving `MaximumExposures` unchanged does not implement that idea; it only leaves an exposure-*count* ceiling in place that, in most real 2D phases, the halved real Pattern-A slot count already sits below — meaning it would frequently be an unreachable no-op rather than a meaningful "fuller dose" allowance.
- **The evidence itself argues the opposite of the premise**: Spiering et al.'s own review frames reduced frequency and reduced volume as two *separately sufficient*, not compensating, maintenance levers ("frequency reduced to 2 sessions/week, **or** exercise volume reduced by 33-66%"). Nothing in that framing suggests trading a smaller frequency for a *larger* per-session dose — if anything, the maintenance literature reduces both together as independent economy measures, not one up as the other goes down. The "less-frequent-should-tolerate-more" premise this option rests on has no support in the evidence actually gathered, and is not what this evidence base was ever positioned to answer in the first place.

## 2. Does the evidence distinguish "how much" from "how long"? — stated explicitly, not assumed

Spiering et al.'s own framing (frequency reduction *or* volume reduction, each independently sufficient for maintenance) is the only point in the evidence base that speaks to this distinction at all, and it speaks to a different pairing than any of the three mechanisms directly implement: it supports reducing *count* and reducing *dose* as parallel, substitutable levers for maintenance — not extending *duration* to preserve count (mechanism 2), and not inflating dose to compensate for reduced count (mechanism 3, which the review's own framing argues against, per §1 above). On the specific question this addendum was asked to resolve — is there evidence that frequency-reduction effects concentrate in "how much" versus "how long"? — **the evidence base is genuinely silent**: no source tests a duration-extension compensation strategy at all. This silence is disclosed explicitly rather than defaulted past.

## 3. Conclusion

**Halving remains the proposed mechanism** — not because it is the mechanically simplest option reached for by default, but because, on direct comparison: it is the one mechanism whose *shape* matches the closest real analog study's own actual design (fixed window, halved count); mechanism 2 both lacks any direct evidentiary support and independently conflicts with already-locked `GEN.11` Core-cycle-length authority; and mechanism 3 targets a system layer this exposure-count mechanism doesn't own, resting on a premise the gathered evidence itself argues against. No revision to `GEN.14`'s proposed formula (`ceil(weekly/2)`, per stage, per level, applied to both min and max, never cross-level) is made.

## 4. Confidence level — restated, unchanged

**LOW-TO-MODERATE**, same tier as `GEN.14`. This addendum strengthens the *justification* for the chosen mechanism relative to real alternatives; it does not add new measurement of the underlying progression question `GEN.14` §1 already confirmed as a genuine evidence gap. The proposal remains a reasoned default under uncertainty, not a measured rate.

## 5. Governance

No production code, tests, or catalog changes (evidence/comparison only). `PHASE_LEDGER.md` row appended; `MASTER_ROADMAP.md` updated to reference this addendum alongside `GEN.14`. **Still explicitly provisional. Phase F does not begin until a human sign-off on the (now comparatively justified) proposal is given and ledgered as final.**
