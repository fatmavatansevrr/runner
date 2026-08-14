# Phase 4M.5B — Multi-Week Adaptation Decision Semantics: Decision-Design Audit

**Status: DECISION-SUPPORT ONLY.** No production code changed. No canonical spec modified. No model recommended or selected.

---

## A. Real structural-week ground truth

### A1. Structural-week boundaries — **EXPLICIT_AND_PERSISTED**

Confirmed by direct schema and code inspection, not inference:

- `LongHorizonRollingWeekState` (`RunningApp.Domain/Entities/LongHorizonRollingWeekState.cs`) persists `GlobalWeek` (int), `StructuralStartDate`/`StructuralEndDate` (`DateOnly`) directly on the row — an explicit, persisted structural index and date range, not derived at read time.
- `LongHorizonRollingSessionState` (`RunningApp.Domain/Entities/LongHorizonRollingSessionState.cs`) has an explicit `WeekStateId` foreign key — every session is explicitly assigned to exactly one week row; there is no calendar-inference step to determine which week a session belongs to.
- The date values themselves are computed deterministically, once, at initial materialization (`LongHorizonRollingStateRepository.cs:86-87`): `var (start, end) = structuralWeek.CalendarRange ?? (request.PlanStartDate.AddDays((globalWeek - 1) * 7), request.PlanStartDate.AddDays((globalWeek - 1) * 7 + 6));` — i.e. **exactly 7 days per week, always**, offset from the plan's own `PlanStartDate`. This is **not** Monday-aligned in general — it is aligned to whatever weekday `PlanStartDate` itself falls on, and stays consistent from there.

**Representative sample** (from real HTTP responses captured earlier in this engagement, TEN_K/Intermediate/4D, `PlanStartDate=2026-09-07` a Monday): window `[6-9]` — week 6: `2026-10-12`(Mon)–`2026-10-18`(Sun); week 7: `2026-10-19`(Mon)–`2026-10-25`(Sun); week 8: `2026-10-26`–`2026-11-01`; week 9: `2026-11-02`–`2026-11-08`. Each boundary is exactly 7 days after the previous, with no gap or overlap. Because the offset is `(globalWeek-1)*7` days from a single anchor date, **start-date weekday does not change the structure** — it only shifts which weekday every week boundary falls on, uniformly.

### A2. Activation/materialization structure

1. **Does GE/rolling generation materialize all four structural weeks atomically in one activation?** **Yes.** `LongHorizonRollingStateRepository`'s persistence loop iterates `window.Weeks` and writes all of them (plus all their sessions) inside the same activation call/transaction — confirmed in the 4M.4B.2A investigation of `SaveActivationSuccessAsync`. There is no partial/incremental per-week commit.
2. **Does one `activate-next-window` call correspond to one 4-week rolling window in the dominant path?** Yes, per the 4M.5A audit (§B2): 83.3% of real windows are 4 structural weeks (16 sessions); this is the intended, uncapped size (`RequestedWindowSizeWeeks = 4`).
3. **Is there any existing intermediate authority smaller than the full rolling window?**

| Candidate | Type/file | Persisted or derived | Production or test-only | Session-role/outcome aware? |
|---|---|---|---|---|
| Per-week lifecycle/numeric state | `LongHorizonRollingWeekState.LifecycleState`, `.WeeklyVolumeKm`, `.LongRunKm` | Persisted | Production | **No** — tracks only whether the week's numeric target was activated and what it was, nothing about session-level completion/NotToday outcomes |
| `WindowExecutionSummary` | `Adaptation/WindowExecutionSummaryBuilder.cs` | Derived (computed fresh per call) | Production | Yes, but only at the full-window granularity — it has no concept of "week" at all (§B1 below) |
| `LongHorizonGeWeekDescriptor` | `LongHorizonGeStructuralContracts.cs` | Derived (pure structural selector output, not persisted) | Production | No — purely structural/catalog metadata (stage, recovery flag, role assignments), no session outcomes |
| `LongHorizonGeWeekNumericResult` | `LongHorizonGeNumericExecutor.cs` | Derived, transient | Production | No — pure numeric progression output, no adherence data |

**No existing production authority computes adherence/completion at per-week granularity.** The only per-week persisted state (`LongHorizonRollingWeekState`) is entirely about the *numeric target*, never about *what the user actually did*. A weekly adherence authority (Models B1/B2 below) would be new, not a reuse of something already there.

### A3. Real role multiplicity inside 16-session windows

**Confirmed directly from `LongHorizonGeStructuralSelector.BuildDescriptor`** (not inferred): every structural week, in every stage family, unconditionally builds exactly:
```
KeySession: 1, EasySupportA: 1, EasySupportB: 1, LongRun: 1
```
This is uniform across all 5 stage families (`Entry`, `BaseDevelopment`, `AerobicDurability`, `Consolidation`, `PreRunwayAlignment`) and every mesocycle position including recovery weeks — there is no stage family that varies the role count. Therefore a real 4-week/16-session window has exactly:

- **KEY_SESSION × 4**
- **EASY_SUPPORT × 8**
- **LONG_RUN × 4**

**`WindowExecutionSummaryBuilder`'s exact multi-occurrence collapse algorithm** (`Adaptation/WindowExecutionSummaryBuilder.cs:80-133`, unchanged, read directly):

```csharp
var keyCompleted = true; // vacuously true if no KEY root; AND-reduced below.
...
foreach (var root in roots) {
    ...
    switch (root.Role) {
        case KeySession:
            keyExpected = true;
            keyCompleted &= isEffectivelyCompleted;   // <-- ALL semantics
            break;
        case LongRun:
            longExpected = true;
            longCompleted &= isEffectivelyCompleted;  // <-- ALL semantics
            break;
        case EasySupport:
            easyExpected++;
            if (isEffectivelyCompleted) easyCompleted++;  // <-- counted, not collapsed
            break;
    }
}
```

**Exact mechanism: `&=` (logical AND-reduction) across every occurrence of that role in the window — i.e. `All(...)` semantics, not `Any(...)`, not first/last occurrence, not lineage-weighted.** `EasySupport` is the only role that is *counted* rather than collapsed to a boolean (`EasyExpectedCount`/`EasyCompletedCount`).

**Concrete answers to the posed examples:**
- **Four KEY sessions exist, only one completed → `KeySessionCompleted = false`.** (`true & false & false & false = false`; the AND-reduction requires every single KEY occurrence to be individually completed.)
- **Three of four LONG_RUN sessions complete → `LongRunCompleted = false`.** Same reasoning — one incomplete occurrence anywhere in the window zeroes out the whole boolean for that role.

**Was this explicitly designed for multi-occurrence, or an accidental consequence of a single-KEY/single-LONG schema?** The evidence points to **accidental** — `WindowExecutionSummaryBuilder`'s own doc comment and every canonical spec revision (Rev3 through Rev4.1) describe and test only the single-week, single-KEY/single-LONG case; `keyCompleted &= isEffectivelyCompleted` reads exactly like a boolean initialized `true` and narrowed by a single conditional check, a pattern that happens to generalize to "AND across N occurrences" without that generalization ever being a deliberate design decision documented anywhere. No test in the repository exercises `KeySessionCompleted`/`LongRunCompleted` with more than one occurrence of that role. **Classification: accidental generalization, not a designed multi-occurrence semantic.**

---

## B. Model A — Window-normalized decision semantics

### B1. Schema implications

`WindowExecutionSummary` (`Adaptation/WindowExecutionSummary.cs` — the record `WindowExecutionSummaryBuilder.Build` returns) currently has: `ExpectedSessionCount`, `EffectiveCompletedCount`, `KeySessionExpected/Completed` (bool), `LongRunExpected/Completed` (bool), `EasyExpectedCount/CompletedCount` (int), `UnrecoveredNotTodayCount`, `SupersededByAdaptationCount`, `HasSafetyFlag`.

**Global normalization** (`EffectiveCompletedCount / ExpectedSessionCount`) requires no new *fields* — both numerator and denominator already exist — only a new *policy* that divides them instead of comparing raw counts.

**Role-count normalization** (per-role expected/completed counts, e.g. `KeyExpectedCount`/`KeyCompletedCount` instead of booleans) is a genuine schema change: `KeySessionExpected`/`KeySessionCompleted`/`LongRunExpected`/`LongRunCompleted` would need to become counts, matching the shape `EasyExpectedCount`/`EasyCompletedCount` already has. This is the **minimum schema change** if role-aware information is to survive window-size scaling at all — the current booleans cannot distinguish "4/4 Key completed" from "1/4 Key completed" (both currently just `false` unless all 4 succeed), which is exactly the information Model A would need to make a role-aware decision at any window size.

**Can the current booleans remain authoritative for multi-week windows?** Not meaningfully — per A3, the AND-reduction already collapses graded information (1 of 4 vs. 3 of 4 vs. 0 of 4) into the same `false`. A window-normalized policy that wants to distinguish "almost all Key sessions done" from "almost none done" cannot do so from the boolean alone.

### B2. Policy semantics

Replacing the literal 0/1/2/3/≥4 thresholds requires **new PRODUCT DEFAULTS**, explicitly enumerated (none proposed as values here):

- A new global adherence cut point (or points) replacing the implicit 25%/50%/75%/100% steps the current absolute thresholds happen to represent at exactly 4 sessions.
- A decision for whether the cut points scale identically regardless of window size (1 week vs. 4 weeks) or whether they are themselves window-size-dependent.
- A minimum proportion (or count) of KEY sessions required for "Key satisfied," replacing the current all-or-nothing boolean.
- A minimum proportion (or count) of LONG_RUN sessions required for "Long satisfied."
- Whether "only Easy missing" generalizes to "only Easy below some threshold, Key and Long above their thresholds" — and if so, what those per-role thresholds are.
- How multiple simultaneously-degraded roles (e.g. 2 of 4 Key **and** 2 of 4 Long both missing) combine — the current matrix's role-aware branch never had to answer this because at exactly 3/4, at most one role's single occurrence could be missing.

### B3. Domain-semantics honesty check

**Does `2/4 → Maintain` becoming `50% → Maintain` preserve original meaning?** The repository's own evidence says **no, not straightforwardly** — `NextWindowLoadDecisionPolicy.cs:4-7`'s doc comment states the matrix is "calibrated for the current 4-session pilot... not a general formula," which is direct, explicit evidence the original authors did **not** intend or verify a percentage-based generalization; they encoded a specific-count-based rule for a specific window size, full stop. There is no spec passage (Rev3, Rev3.1, Rev4, Rev4.1) that frames the matrix in percentage terms at all — every canonical example is stated in raw counts against a 4-session week.

**Does `3/4 with only Easy missing → ProgressAsPlanned` generalize to a percentage claim?** Same conclusion — the "only Easy missing" branch is explicitly role-structural (checks `KeySessionCompleted`/`LongRunCompleted` booleans, not a count or ratio), and per A3, that exact structural check becomes unreachable at any window size where a role occurs more than once and even a single occurrence is missing while others are complete — a case the 4-session model could never encounter (since each role occurs exactly once, "all Key occurrences complete" and "the one Key session is complete" are the same statement).

**Was the original intent adherence-percentage-based, or role-structural (1 Key + 2 Easy + 1 Long)?** The evidence supports **role-structural**, not percentage-based: the entire matrix's third branch is defined *in terms of which role is missing*, not *what fraction is missing* — a percentage-based design would not need to ask "is it only Easy" at all, since 3/4 is 75% regardless of which role the missing one belongs to. The fact that the policy asks a structural question (`OnlyEasyMissing`) rather than a purely numeric one is itself evidence the original model's currency was role-identity, not adherence-percentage.

**Classification: not `PRODUCT_SEMANTIC_NOT_ESTABLISHED`** — the evidence is clear enough to conclude the original intent was role-structural for a specific 4-session shape, not a general percentage model. What is *not* established is how that role-structural intent should generalize to a 4x-larger window with 4 occurrences of each priority role — that is the open product question, not a matter of insufficient evidence about original intent.

### B4. Role-aware normalization variant

A richer Model A using per-role completion ratios (`KeyCompletionRatio = KeyCompletedCount/KeyExpectedCount`, similarly for Long and Easy) would **better preserve the role-aware spirit** of the original third branch than naive global-percentage normalization, because it retains the "which role is under-delivered" question the original matrix actually asks — it just answers it gradedly (a ratio) instead of booleanly. It still requires new product thresholds: at minimum, a Key-ratio cut point, a Long-ratio cut point, and a rule for how the three per-role ratios (plus the global count/ratio) combine into one of the four `NextWindowLoadDecision` values — strictly more new decisions than naive global normalization (B2's list) plus the combination rule on top.

### B5. Blast radius

**Pure decision layer (4M.1 scope):**
- `Adaptation/WindowExecutionSummary.cs` (record shape) — LIKELY_CHANGE (role counts vs. booleans, per B1)
- `Adaptation/WindowExecutionSummaryBuilder.cs` — MUST_CHANGE (produces the new shape)
- `Adaptation/NextWindowLoadDecisionPolicy.cs` — MUST_CHANGE (new thresholds/ratios)
- `PlanAdaptationV1DecisionTests.cs` — MUST_CHANGE (matrix and summary tests assume the current shape/booleans)

**Persistence/summary layer (4M.2/4M.4A scope):**
- `Adaptation/WindowCheckpointEvidenceMapper.cs` — NO_CHANGE (maps persisted rows to `LogicalSessionEvidence`, which does not itself encode role-completion booleans — the mapping input is unaffected)
- `Adaptation/LogicalSessionEvidence` contract — NO_CHANGE (role/outcome per session, already granular enough)
- `WindowCheckpointSummaryAndDecisionTests.cs` — LIKELY_CHANGE (asserts on the current summary shape in several tests)

**Numeric activation layer (4M.4B scope):**
- `NextWindowNumericAnchorSelector.cs` — **NO_CHANGE**. Its signature (`NextWindowLoadDecision decision, ValidatedSustainableLoad? current, ValidatedSustainableLoad? prior, int effectiveCompletedCount`) already treats the *decision* as an opaque enum input — it never re-derives role/count information itself. As long as `NextWindowLoadDecisionPolicy` still ultimately produces one `NextWindowLoadDecision` value per window, the selector is untouched.
- `LongHorizonRollingWindowActivationService.cs` — NO_CHANGE to its call site shape (still calls `NextWindowLoadDecisionPolicy.Evaluate(windowSummary)` once, gets one decision).

**Can `ProgressAsPlanned`/`Maintain`/`Reduce` still feed the existing `NextWindowNumericAnchorSelector` unchanged? Yes — proven architecturally**, not assumed: the selector's only decision-shaped input is the 3-value enum itself (`NextWindowLoadDecision`), which Model A does not add values to or change the meaning of — it only changes *how that enum gets computed*, entirely upstream of the selector.

### B6. Existing invariants

- **`one activation = one LoadDecision = one NextWindowTargetAnchor`: PRESERVED.** Model A does not touch the cardinality of decisions or anchors per activation — it only changes what evidence feeds the single decision computation.
- **`WindowBoundaryConstraint`: NOT reopened.** This constraint governs schedule-repair candidate eligibility (§3 of the canonical spec), entirely orthogonal to how the post-hoc window summary is computed.
- **Chronological immutability: NOT reopened.** Same reasoning — Model A is a pure function of already-final window evidence, computed after all repair/immutability concerns have already resolved.
- **Activation idempotency: NOT reopened.** The idempotency key (`activation:{planStateId}:{WindowId}:{sequence}`) has no dependency on how the summary/decision was computed.
- **Window identity: NOT reopened.**
- **Block semantics: NOT reopened.** `IsBlock` propagation (4M.4B.2A) is orthogonal to Model A — it fires based on catalog/JIT-composition feasibility of whatever anchor the (possibly-different) decision selects, not on how the decision itself was computed.

**Model A is, of the three model families, the one with the narrowest blast radius and the fewest reopened invariants** — this observation is reported as an architectural fact for the comparison matrix, not a recommendation.

---

## C. Model B — Structural-week decision semantics

### C1. B-weekly-summary

**Definition (repeated for clarity):** the rolling activation window stays exactly as it is today (up to 4 structural weeks, one activation). Internally, before the final decision, the window's evidence is split into up to 4 per-week evidence groups (using the already-existing, already-persisted `WeekStateId`/`GlobalWeek` partition from A1 — this partition is free, it already exists), each summarized (reusing `WindowExecutionSummaryBuilder` itself, called once per week instead of once per window), optionally decided (reusing `NextWindowLoadDecisionPolicy` itself, called once per week), then aggregated by some rule (D/E below) into one final decision and one final anchor, feeding the existing activation/anchor/composition pipeline completely unchanged from that point on.

**Can this preserve `one activation = one final LoadDecision = one anchor`? Yes, explicitly, by construction** — the aggregation step's entire job is to collapse N weekly decisions back into exactly one value before anything downstream of `NextWindowLoadDecisionPolicy.Evaluate` ever runs. Everything from `NextWindowNumericAnchorSelector` onward is architecturally identical to today.

**Can it avoid reopening activation window identity / checkpoint persistence / idempotency keys / chronology / window-boundary constraints? Yes** — all of those operate on the *rolling activation window* as a unit (its `WindowId`, its `CurrentWindowStartWeek/EndWeek` range, its single activation transaction). B-weekly-summary's weekly split is purely an internal evidence-preparation step inside the *evidence → decision* computation, invisible to everything before or after it. **This is architecturally the important distinction the phase brief asks not to conflate with B-weekly-checkpoint:** B-weekly-summary changes what evidence *shape* feeds one decision; it does not change *when* decisions/activations happen or how many of them there are.

### C2. B-weekly-checkpoint

**Definition:** each structural week becomes an actual, independent checkpoint/decision/activation boundary — i.e. the unit of activation itself shrinks from "up to 4 weeks" to "1 week."

**Blast radius, by area:**

- **Rolling window lifecycle:** MUST_CHANGE. The entire `RequestedWindowSizeWeeks=4`/`Math.Min(nextStart+3, GeneralEnduranceWeeks)` sizing rule (`LongHorizonRollingInitialActivationRuntime.cs`, `LongHorizonRollingCheckpointRuntime.cs`) would need to become 1-week sizing, or a second, smaller activation unit would need to be introduced alongside the existing one.
- **Activation frequency:** MUST_CHANGE. The public `activate-next-window` semantics (one call = one full rolling window today) would either need to mean something different, or a new, additional endpoint/flow for weekly checkpoints would be needed.
- **Persisted window state:** MUST_CHANGE. `LongHorizonRollingPlanState.CurrentWindowStartWeek/EndWeek` currently tracks one 4-week-capable range; per-week tracking would need new or repurposed fields.
- **Idempotency:** MUST_CHANGE. The idempotency key formula (`activation:{planStateId}:{WindowId}:{sequence}`) is keyed to the *window's* identity; a weekly checkpoint would need its own key scheme, and the interaction between a weekly checkpoint's idempotency and the enclosing window's idempotency would be new, undesigned surface.
- **`PriorAnchor(state)`:** LIKELY_CHANGE. This currently reads the single most recent checkpoint's validated load; under weekly checkpoints, "prior" could mean "prior week" or "prior window," a genuinely new ambiguity Rev4's `PriorValidatedCheckpointLoad` concept was never designed to disambiguate.
- **Checkpoint sequencing:** MUST_CHANGE. `LongHorizonCheckpointRecord`'s `SourceWindowStartWeek/EndWeek` fields and the checkpoint-evidence aggregator's window-level assumptions would need re-derivation for a 1-week source range.
- **Block semantics:** LIKELY_CHANGE. `IsBlock` propagation and the typed 409 behavior are currently window-scoped; a Block on one week inside what is still, materially, a 4-week catalog-generation unit raises new questions about whether the other 3 weeks' generation is also blocked, partially committed, or independent.
- **JIT composition:** LIKELY_CHANGE to MUST_CHANGE. `TenKPreparationRunwayDarkOrchestrator`/`DynamicCoreCalendarMaterializationOrchestrator` generate Core/Runway content in larger structural batches (confirmed via 4M.4B.2B/2C investigation — `targetWeekCount` in the real composition request spans multiple weeks); forcing 1-week-at-a-time composition either requires re-architecting that pipeline or running it 4x per window and discarding/caching 3/4 of the output each time.
- **Calendar materialization:** LIKELY_CHANGE, same reasoning.
- **Public activation endpoint semantics:** MUST_CHANGE — a client-visible behavior change (more frequent decisions, more frequent possible Blocks, different `previous_window_range`/`activated_window_range` meaning).

**Does this genuinely reopen previously-frozen 4M.4–4M.4B lifecycle assumptions?** **Yes, unambiguously.** Every one of the explicitly-frozen invariants from those phases (window identity, chronological immutability, activation idempotency at the window level, the single-anchor-per-activation architecture) is built on "the rolling activation window is the atomic unit of decision and materialization." B-weekly-checkpoint directly contradicts that premise at its root, not at some peripheral layer.

### C3. Does weekly interpretation preserve original semantics?

**Yes, with strong direct evidence.** Every canonical example across Rev3/Rev3.1/Rev4/Rev4.1 (§6's "Kilit örnek," the Mon/Wed/Fri/Sun = Easy/Key/Easy/Long pilot description repeated in Rev1 through Rev4.1's own changelog notes) describes exactly one structural training week containing one KEY, two EASY, and one LONG — which A3 confirms is *exactly* the real, persisted structural-week role shape, unconditionally, in production. This is the strongest evidence in this entire audit for any single claim: **the original decision matrix's natural unit is the single structural week**, not the rolling activation window (which the spec, per A1/A2, never once mentions or acknowledges can span up to 4 weeks).

This does **not** by itself mean Model B is "correct" — it means the *evidence granularity* Model B operates on (one week) matches the canonical examples' granularity more closely than Model A's (one multi-week window). Whether the *decision authority* (i.e., which architecture actually computes and applies decisions) should also move to weekly granularity is the separate, harder question C1/C2 exist to separate out.

---

## D. Model B1 — Weekly semantics + worst-week wins

Assumes B-weekly-summary architecture (one activation, weekly evidence internally, one final decision) unless noted.

### D1. Required product decisions

- Does one Reduce week force final Reduce, unconditionally? (Simple worst-week-wins says yes, but this is itself a decision to confirm, not a given.)
- Does one Maintain week force Maintain if no week is Reduce? (Same.)
- Does ProgressAsPlanned require *all four* weeks to independently qualify as ProgressAsPlanned, or does it only need "no week worse than Progress" (i.e. the same thing, restated)?
- What happens when a weekly decision is **unavailable** — e.g. a week with zero expected sessions (can this happen structurally? Per A3, every GE week has exactly 4 sessions, so this may be moot for pure-GE windows, but is not moot in principle for a general model), or a week whose evidence is incomplete because the window itself is mid-materialization?
- Is "worst" defined by the existing implicit severity order `Reduce < Maintain < ProgressAsPlanned` (matching the ordering already asserted by `NextWindowNumericAnchorSelectorTests.SeverityOrdering_...`), or by some new ordering?

### D2. Safety aggregation

`HasSafetyFlag`/`SafetyReviewRequired` today is computed once, across the *entire* window's evidence (`WindowExecutionSummaryBuilder.cs:86-89`: `sessions.Any(s => NotToday && TriggersSafetyFlag(reason))` — already an OR/Any across every session in whatever evidence list is passed in). **Under B-weekly-summary, "OR across weeks" is not a new decision — it is the exact same `Any(...)` semantic the current code already implements**, merely evaluated once per week-slice instead of once per whole window, then OR'd again across the 4 per-week results (which, by the transitive property of OR, is mathematically identical to computing `Any(...)` once across all 16 sessions directly, as today). **Classification: already implied by current window-level behavior — not a new product decision**, *provided* the aggregation literally uses OR/Any. If some other combination were chosen (e.g. "only if 2+ weeks have a safety event"), that would be a new decision; plain OR is not.

### D3. Worked temporal examples

**Example 1:** Week1=Reduce, Week2=Progress, Week3=Progress, Week4=Progress → **B1 (worst-week-wins) final decision = Reduce** (Reduce is the most severe of the four, regardless of position). Under Rev4's existing anchor formula, Reduce then selects `min(ValidatedSustainableLoad(window), PriorValidatedCheckpointLoad)` — using whichever window-level evidence values feed the selector (this itself raises D4's open question: does "window-level" `ValidatedSustainableLoad` still mean the full 16-session aggregate, or something week-scoped?).

Compared with Model A conceptually: a global-adherence Model A variant, given the same 16-session raw data (3 of 4 weeks fully compliant, 1 week not), would likely land on a *high* completion percentage overall (e.g. if "Reduce" for week 1 meant 0-1 of that week's own 4 sessions completed, that's at most 1/16 ≈ 6% of the window missing from otherwise-full completion, i.e. ~94% overall) — plausibly `ProgressAsPlanned` under a naive global-percentage Model A, the **opposite** conclusion from B1's Reduce. This is reported as a real, structurally-inherent divergence between the two model families for this exact input shape, not a defect of either.

**Example 2a (front-loaded miss):** Week1=Reduce, Week2=Reduce, Week3=Maintain, Week4=Progress.
**Example 2b (back-loaded miss):** Week1=Progress, Week2=Progress, Week3=Progress, Week4=Reduce.

**Does B1 (worst-week-wins) treat these identically despite different recency? Yes — provably, by construction.** Worst-week-wins is a `min` (or `max`, depending on severity-direction convention) over an unordered multiset of 4 values; it has no notion of position/recency at all. Both examples contain exactly one `Reduce` occurrence (2a has two, actually — an even more severe case than 2b's one) among otherwise-better weeks, so 2a resolves to Reduce identically to how a single-Reduce case would, and 2b (a single Reduce in the most recent, last-completed week) *also* resolves to Reduce. **Tradeoff (described, not judged):** worst-week-wins is maximally conservative/safety-biased — any single bad week anywhere in the 4-week window drags the whole window's decision down to that week's severity, with no credit given for the trend being positive (2b) versus negative (2a) leading into the decision point. A recency-sensitive model (B2c) would treat these two examples differently; B1 structurally cannot.

### D4. Numeric anchor architecture

**Two possibilities, analyzed without selecting one:**

**(i) Selector runs once, using the final aggregated decision plus window-level `ValidatedSustainableLoad`/`PriorValidatedCheckpointLoad`, exactly as today.** This is **architecturally the smaller reinterpretation** — `NextWindowNumericAnchorSelector`'s signature and every downstream consumer (composition, JIT, Block/IsBlock propagation) are completely unchanged; only the *decision* enum's derivation (via B-weekly-summary's internal aggregation) differs from today. This preserves the "Catalog=progression authority / Adaptation=anchor-constraint authority" architectural invariant Rev4.1 itself documents (§7's TARGET PRESCRIPTION INFEASIBILITY architectural note) with zero reinterpretation of what `ValidatedSustainableLoad`/`PriorValidatedCheckpointLoad` themselves mean — they remain whole-window concepts, exactly as Rev4 defines them today.

**(ii) Weekly Reduce/Maintain semantics require weekly numeric anchors first, aggregated afterward.** This would mean computing 4 separate `ValidatedSustainableLoad` values (one per week's own evidence) and/or 4 separate anchor selections, then combining *those* into one final anchor — a materially larger reinterpretation, since `ValidatedSustainableLoad` and `PriorValidatedCheckpointLoad` are currently whole-window/whole-checkpoint concepts with no per-week equivalent anywhere in the codebase (confirmed by A2 — no existing per-week adherence or load authority exists). This path would require inventing new domain concepts (a "weekly validated load") that do not exist today in any form, pure or persisted.

**Which preserves the current numeric-authority architecture with the least reinterpretation?** Per the architectural evidence above, **(i)** — the single-final-decision approach — requires zero change to `NextWindowNumericAnchorSelector`, `PriorAnchor(state)`, or the Rev4 anchor formulas themselves; **(ii)** requires inventing new anchor-level concepts that presently have no basis in the codebase at all. This is reported as an architectural-evidence finding, not a model recommendation — B1 could be implemented either way; (i) is simply the path that reuses more of what already exists.

---

## E. Model B2 — Weekly semantics + deterministic aggregation

Four required candidates, plus rationale for why no others were added (the repo's own structural evidence — recovery-week placement, mesocycle position — naturally suggests exactly these four and no clearly-distinct fifth).

### E1. Product-decision inventory per candidate

**B2a — Most-recent-week wins.** Final decision = the latest *complete* structural week's own outcome.
New decisions required: definition of "latest valid/complete week" (a week with `LifecycleState` fully resolved and all its sessions in a terminal outcome state, presumably, but this needs to be made explicit); handling when the nominally-last week of the window is itself incomplete (evidence still pending) — does the model fall back to the second-to-last week, or block, or treat "incomplete" as its own severity level?; tie-breaking is moot for B2a (there is always exactly one "most recent" week by construction), but "unavailable decision" handling is not.

**B2b — Majority decision.** Aggregate 4 weekly decisions by simple majority (3+ of the same value wins).
New decisions required: tie-breaking for a genuine plurality with no majority (e.g. 2 Reduce, 1 Maintain, 1 Progress — no value has 3+); whether ties resolve toward the more severe or less severe value, or toward recency, or are themselves a distinct outcome; whether "majority" is computed over the 3-value ordinal scale directly or requires a defined severity-ranking function first (needed to answer "is 2 Reduce + 2 Maintain a tie, or does severity ordering make Reduce win by some other rule").

**B2c — Recency-weighted severity.** More recent weeks carry more weight in a severity score.
New decisions required: the exact weight schedule (e.g. linear, exponential, or a simple "most recent counts double" rule) — no such weighting scheme exists anywhere in the current codebase to reuse; how severity is represented numerically to be weighted at all (`Reduce`/`Maintain`/`ProgressAsPlanned` are an unordered-until-now enum, not numbers — a numeric severity scale would itself be new); the final decision thresholds against the resulting weighted score (i.e. this candidate inherits *all* of Model A's B2-style threshold-invention problem, but for a derived score instead of a raw count).

**B2d — Evidence aggregation before final decision.** Compute 4 weekly `WindowExecutionSummary`s, then combine their underlying *evidence* (completed counts, role counts, recency) into one aggregate evidence object, and run one decision rule (structurally, `NextWindowLoadDecisionPolicy` itself, or a variant of it) over that aggregate.
New decisions required: exactly how weekly evidence combines (sum? weighted sum? something else) — if a straight sum, this becomes numerically identical to Model A's global-normalization variant (see the false-tradeoff analysis in §K); role-weighting if per-role evidence combines differently than the total-count evidence; recency-weighting if recent weeks' evidence counts more; and, once combined, all of Model A's B2/B3 threshold-invention questions apply again to the now-combined evidence.

### E2. Numeric anchor consequences per candidate

| Candidate | Selector runs once after final decision? | Requires anchor/evidence aggregation? | Blast radius beyond B-weekly-summary's own |
|---|---|---|---|
| B2a (most-recent-wins) | Yes — same as D4(i) | No | Minimal — same "least reinterpretation" path as B1(i) |
| B2b (majority) | Yes — same as D4(i) | No | Minimal, same reasoning |
| B2c (recency-weighted severity) | Yes, but the *decision itself* is now derived from a synthetic score with no existing domain meaning | No (anchor selection still only needs the final enum + window-level load values) | Moderate — the new weighted-severity score is a wholly new concept requiring its own tests/validation, even though the selector itself is unchanged |
| B2d (evidence aggregation) | **Ambiguous by construction** — depends entirely on what "aggregate evidence" is defined to include; if it includes per-week `ValidatedSustainableLoad`, this collapses into D4(ii)'s larger reinterpretation | **Likely yes**, if the aggregate evidence is meant to also inform the anchor (not just the decision) | Largest of the four — this candidate is the only one that structurally invites re-opening whether `ValidatedSustainableLoad` itself becomes a weekly concept |

**This is one of the most important findings of this phase, stated plainly:** three of the four B2 candidates (a, b, c) can preserve the "selector runs once, unchanged" architecture just as cleanly as B1 can; **only B2d genuinely risks reopening the numeric-anchor architecture**, and only if its evidence-combination step is defined to reach into anchor-level concepts rather than staying confined to decision-level evidence (completion counts).

---

## F. Phase-transition / mixed-phase windows

**Confirmed directly from real HTTP response data captured earlier in this engagement** (window `[6-9]`, TEN_K/Intermediate/4D, real activation): week 6 = stage `GeneralEndurance`; week 7 = stage `AerobicStrength`; week 8 = stage `AerobicStrength`; week 9 = stage `PreSpecificTransition`. **A single real 4-structural-week rolling window unambiguously spans three distinct stages within one activation.** This is not a hypothesis — it is directly observed, real production data.

1. **Can weekly summaries remain cleanly phase-scoped?** Yes, trivially — since each week already carries its own `Stage`/`SegmentType` (A1), a per-week summary is automatically phase-scoped by definition; the ambiguity, if any, is entirely in *aggregation*, not in per-week summarization itself.
2. **Would Model A summarize across phases?** Yes, unavoidably — Model A's entire premise is one summary per whole window, and per the evidence above, a whole window can span 3 stages. Model A's summary is therefore inherently phase-blind by construction (as, in fact, the *current* production behavior already is — this is not a new limitation Model A introduces, it is the status quo).
3. **Would Model B naturally isolate them?** Model B (either variant) naturally isolates evidence by week, and each week is phase-labeled, so Model B has the *option* to be phase-aware that Model A structurally does not — but nothing about B-weekly-summary's aggregation step requires it to actually use that phase information; a naive B1/B2 aggregation could still ignore stage entirely and would behave identically to a phase-blind model.
4. **Does any candidate model conflict with existing `PhaseBoundaryConstraint`?** **No direct conflict, because they are different authorities operating at different times and on different concerns.** `PhaseBoundaryConstraint` (§3 of the canonical spec) governs *schedule-repair candidate eligibility* — a same-phase check applied when searching for a reschedule/substitution target, evaluated live at `NotToday`-submission time, entirely before any next-window decision is ever computed. None of Models A/B1/B2 touch repair candidate selection. **This is reported separately, as instructed:** the repair-time phase constraint does not automatically define or constrain next-window adaptation semantics — they are independent concerns that happen to share the word "phase," and this audit found no code-level coupling between them.

---

## G. Repair + weekly-summary interaction

**Confirmed directly from `ScheduleRepairCandidateProvider.cs:92`:** the repair candidate query filters by `w.GlobalWeek >= aggregate.CurrentWindowStartWeek && w.GlobalWeek <= aggregate.CurrentWindowEndWeek` — i.e. candidates are scoped to the **entire rolling window**, not to the trigger session's own structural week. Combined with `PhaseBoundaryConstraint` (candidate must share the trigger's `Phase`/`Stage`) — and per §F, a stage can itself span more than one structural week (e.g. `AerobicStrength` spanning weeks 7-8 in the observed data) — a repaired/substituted session **can** legitimately land in a different structural week than its source session, as long as it stays within the same window and the same phase/stage.

**Direct answers:**
- **Source session and replacement always remain in the same structural week?** **No** — not guaranteed. They remain in the same *phase*, which is a coarser (or, in principle, differently-shaped) grouping than "structural week."
- **Can a rescheduled priority session move into the next structural week while remaining inside the same 4-week rolling window?** **Yes** — confirmed possible by the query scope above, whenever the phase/stage the trigger belongs to spans multiple structural weeks.
- **If yes, which weekly summary owns that logical expectation?** **Genuinely ambiguous under Model B as currently specified.** The source session's original logical expectation (Rev3.1 §5's lineage rule: source + replacement = one logical expected session) would need to be attributed to *one* week's summary for that week's decision to be computed at all — but the replacement's own completion event happens in a *different* week's date range. Neither "attribute to the source week" nor "attribute to the replacement week" is obviously correct without a new rule, and the current lineage-following algorithm (`FollowLineageToTerminalOutcome`) is entirely week-agnostic by design (A2) — it walks a chain of session IDs, never consulting `WeekStateId` at all.

**Classification: `WEEKLY_LINEAGE_ASSIGNMENT_REQUIRES_NEW_RULE`.**

**New rule enumerated under B1/B2's product-decision inventory (§D1/§E1), added here explicitly since neither section above anticipated it:** a rule for which structural week's summary a cross-week-repaired logical expectation belongs to — candidates include "always the source/original week" (preserves "the user was originally expected to run this session in week N" framing) or "always the terminal/replacement week" (preserves "this is when it actually got resolved" framing) or "both, with the source week showing it as resolved-elsewhere and the replacement week not double-counting it" (most correct but most complex). This is a genuinely new decision with no existing precedent to draw on, required by **any** Model B variant that wants per-week summaries to sum back to the same totals the current whole-window `WindowExecutionSummaryBuilder` already correctly produces (via its window-wide, week-agnostic lineage walk).

---

## H. Comparison matrix

| Dimension | Model A | Model B1 (weekly-summary) | Model B2 (weekly-summary) | B-weekly-checkpoint variant |
|---|---|---|---|---|
| Preserves original 4-session semantics | Partially — preserves total-evidence granularity, loses role-occurrence identity (§B3) | Yes — evidence granularity matches original exactly (§C3) | Yes — same as B1 | Yes, at the evidence level; No, at the lifecycle-architecture level (§C2) |
| Requires global adherence thresholds | Yes (§B2) | No (weekly decisions reuse existing exact-count matrix per week) | No, except B2c/B2d if scores are combined numerically (§E1) | No |
| Requires role-specific thresholds | Yes, if role-aware variant chosen (§B4) | No — existing per-week matrix already role-aware, unchanged | No, same as B1 | No |
| Requires weekly aggregation policy | No | Yes — worst-week-wins (§D) | Yes — 4 candidate policies (§E) | N/A — no aggregation, each week stands alone |
| New PRODUCT DEFAULT count/categories | High (thresholds: global cut points, optionally per-role cut points, combination rule) (§B2/B4) | Moderate (worst-week semantics confirmation, unavailable-week handling, cross-week lineage rule) (§D1, §G) | Moderate–High depending on candidate (tie-break/weights/thresholds per §E1) plus §D1/§G items | High (activation-frequency, persistence, idempotency semantics — §C2) |
| `WindowExecutionSummary` schema impact | LIKELY_CHANGE (role counts vs. booleans) (§B1) | NO_CHANGE (existing builder reused per-week, unchanged shape) | NO_CHANGE, same reasoning | NO_CHANGE to the type itself; MUST_CHANGE to its caller's per-call scope |
| `NextWindowLoadDecisionPolicy` impact | MUST_CHANGE (new thresholds) (§B5) | NO_CHANGE (reused as-is, once per week) plus a NEW aggregation function | NO_CHANGE, same reasoning, plus a different NEW aggregation function per candidate | NO_CHANGE to the policy itself; called once per week instead of once per window |
| Numeric anchor selector impact | NO_CHANGE (§B5) | NO_CHANGE under D4(i); reinterpreted under D4(ii) | NO_CHANGE for B2a/b/c under E2's path (i); B2d ambiguous, possibly MUST_CHANGE | LIKELY_CHANGE — anchor selection semantics for a 1-week checkpoint are undefined today |
| `PriorAnchor` impact | NO_CHANGE | NO_CHANGE under D4(i) | Same as anchor selector row | LIKELY_CHANGE (§C2 — "prior" becomes ambiguous: prior week or prior window) |
| One activation = one final decision preserved? | Yes | Yes (§C1) | Yes (§C1, inherited) | **No** — that is the defining characteristic of this variant (§C2) |
| One activation = one anchor preserved? | Yes | Yes under D4(i); UNKNOWN (implementation-dependent) under D4(ii) | Yes for a/b/c under (i); UNKNOWN for B2d | No |
| Requires new persisted weekly state? | No | No — B-weekly-summary's weekly split is computed transiently from already-persisted per-session/per-week data (A1/A2), never written back | No, same reasoning | Yes — MUST_CHANGE to `LongHorizonRollingPlanState` and/or new tables for weekly checkpoint tracking (§C2) |
| Reopens `WindowBoundaryConstraint`? | No (§B6) | No (§C1) | No | UNKNOWN — depends on whether repair candidate scoping also changes; not investigated here since B-weekly-checkpoint is out of scope for implementation |
| Reopens chronological immutability? | No | No | No | Likely, given activation-unit redefinition, but not conclusively determined without deeper investigation — UNKNOWN |
| Reopens activation idempotency? | No | No | No | Yes (§C2 — MUST_CHANGE to the idempotency key scheme) |
| `SafetyReviewRequired` orthogonality preserved? | Yes (unaffected — HasSafetyFlag computation untouched) | Yes — OR-aggregation is mathematically identical to today's `Any(...)` (§D2) | Yes, same reasoning, for all four candidates (none of them touch safety-flag semantics) | UNKNOWN — depends on whether safety is evaluated per-checkpoint or still window-wide; not determined |
| Repair lineage/week ownership complexity | Low — Model A never partitions evidence by week at all, so the cross-week lineage question (§G) never arises | **High — the open question in §G applies directly and must be resolved** | Same as B1 — inherited, applies identically | High, and compounded by activation-unit redefinition |
| Mixed-phase window complexity | Low — summarizes across phases exactly as today already does (§F.2) | Moderate — per-week summaries are phase-clean by construction, but aggregation must still combine across phases (§F.3) | Same as B1 | Moderate–High — a weekly checkpoint could itself span a stage transition mid-week in principle, though not observed in real data |
| Estimated production files touched by layer | Decision layer: 3-4 files; persistence: 0-1; numeric: 0 (§B5) | Decision layer: 1-2 files (new aggregation function) + summary-builder call-site change; persistence: 0; numeric: 0 under D4(i) | Same as B1, ×4 for candidate-specific logic, but only one candidate would ultimately be chosen | Decision layer, persistence layer, rolling checkpoint runtime, activation service, numeric anchor, JIT composition — essentially every layer in §I below |
| Estimated test suites affected | `PlanAdaptationV1DecisionTests`, `WindowCheckpointSummaryAndDecisionTests`, `NextWindowNumericAnchorSelectorTests` (unaffected assertions only) | `PlanAdaptationV1DecisionTests`, `WindowCheckpointSummaryAndDecisionTests`, new aggregation-specific tests | Same as B1 plus per-candidate test suites | All LongHorizon suites — activation, checkpoint runtime, JIT composition, persistence, concurrency (per §I) |
| Implementation slices likely required | 1 (decision-layer only) | 1-2 (decision layer + new aggregation) | 1-2, same shape as B1 | Multiple phases, comparable in scope to the original 4M.4/4M.4B chain |
| Migration/schema persistence change likely? | No | No | No | Yes (§C2) |
| Current frozen 4M.4B lifecycle impact | None | None (§C1) | None (for a/b/c under path (i)) | **Severe — directly reopens window identity, idempotency, chronology, and Block-semantics invariants frozen across 4M.4A through 4M.4B.2C** (§C2) |

---

## I. Blast-radius inventory (by architectural layer)

### Domain contracts
- `Adaptation/WindowExecutionSummary.cs` — Model A: LIKELY_CHANGE; Model B1/B2: NO_CHANGE; B-weekly-checkpoint: NO_CHANGE to the type, but its per-call scope changes
- `LongHorizonRollingWeekState.cs` (`RunningApp.Domain`) — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: LIKELY_CHANGE (new fields for weekly checkpoint tracking)
- `LongHorizonRollingPlanState.cs` (`RunningApp.Domain`) — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: MUST_CHANGE (`CurrentWindowStartWeek/EndWeek` semantics)

### Pure decision policies
- `Adaptation/NextWindowLoadDecisionPolicy.cs` — Model A: MUST_CHANGE; B1/B2: NO_CHANGE to the policy itself, but a NEW aggregation function is added alongside it; B-weekly-checkpoint: NO_CHANGE to the policy, called at new frequency
- New aggregation type (e.g. `WeeklyDecisionAggregationPolicy`, does not exist today) — B1/B2: MUST_CHANGE (new file); Model A: NO_CHANGE (n/a)

### Summary/evidence mapping
- `Adaptation/WindowExecutionSummaryBuilder.cs` — Model A: MUST_CHANGE; B1/B2: NO_CHANGE (reused per-week, unmodified); B-weekly-checkpoint: NO_CHANGE to the builder, new caller
- `Adaptation/WindowCheckpointEvidenceMapper.cs` — All models: NO_CHANGE (maps persisted rows to session-level evidence; unaffected by how that evidence is subsequently grouped/decided)
- `Adaptation/LogicalSessionEvidence` — All models: NO_CHANGE

### Persistence
- `LongHorizonRollingStateRepository.cs` — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: MUST_CHANGE (activation persistence granularity)
- `LongHorizonCheckpointRecord` entity/persistence — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: MUST_CHANGE (`SourceWindowStartWeek/EndWeek` assumptions)

### Rolling checkpoint runtime
- `LongHorizonRollingCheckpointRuntime.cs` — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: MUST_CHANGE (window-sizing formula itself)
- `LongHorizonCheckpointEvidenceAggregator.cs` — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: LIKELY_CHANGE (evidence-aggregation scope)
- `LongHorizonCheckpointStateEvaluator.cs` — All A/B1/B2: NO_CHANGE; B-weekly-checkpoint: UNKNOWN (this frozen file's own eligibility gates, e.g. `AvailabilityFeasible`, `GrowthConfidenceSatisfied`, were designed and validated against multi-week evidence; behavior at 1-week granularity not determined without deeper investigation)

### Activation service
- `LongHorizonRollingWindowActivationService.cs` — Model A: NO_CHANGE to structure, only to the `windowSummary`/`nextWindowResult` computation call; B1/B2: NO_CHANGE to structure, calls the new weekly-then-aggregate path instead; B-weekly-checkpoint: MUST_CHANGE (fundamentally, this is the file whose entire premise — one call handles one window — would be restructured)

### Numeric anchor
- `NextWindowNumericAnchorSelector.cs` — Model A: NO_CHANGE; B1/B2 (path i): NO_CHANGE; B2d (if evidence-level): UNKNOWN; B-weekly-checkpoint: LIKELY_CHANGE
- `LongHorizonRollingWindowActivationService`'s `PriorAnchor(state)` helper — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: LIKELY_CHANGE

### Catalog/JIT
- `LongHorizonRollingJitCompositionOrchestrator.cs`, `TenKPreparationRunwayDarkOrchestrator.cs`, `DynamicCoreCalendarMaterializationOrchestrator.cs` — Model A/B1/B2: NO_CHANGE (all operate on the anchor + window boundary, both unchanged by these models); B-weekly-checkpoint: LIKELY_CHANGE to MUST_CHANGE (§C2)

### Public API contracts
- `LongHorizonActivateNextWindowResponse` and related DTOs — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: LIKELY_CHANGE (`previous_window_range`/`activated_window_range` semantics)

### Read models
- `LongHorizonActiveReadModelProvider.cs` — Model A/B1/B2: NO_CHANGE; B-weekly-checkpoint: LIKELY_CHANGE (what "current window" means to a client)

### Tests
- `PlanAdaptationV1DecisionTests.cs`, `WindowCheckpointSummaryAndDecisionTests.cs` — Model A: MUST_CHANGE; B1/B2: LIKELY_CHANGE (new tests added, existing ones should remain valid since the underlying per-week policy is unchanged); B-weekly-checkpoint: MUST_CHANGE, broadly, across the LongHorizon suite
- `NextWindowNumericAnchorSelectorTests.cs` — Model A/B1/B2 (path i): NO_CHANGE; B-weekly-checkpoint: LIKELY_CHANGE
- `LongHorizonThreeWindowAnchorThreadingE2ETests.cs` and related real-HTTP multi-window suites — Model A/B1/B2: NO_CHANGE to the tests' own logic, though observed decisions for existing fixtures may change (LIKELY_CHANGE to expected values only, not test structure); B-weekly-checkpoint: MUST_CHANGE (entire test architecture assumes window-level activation)

---

## J. Consolidated new-product-decision inventory

### Model A
1. Global adherence cut point(s) replacing the 0/1/2/3/≥4 steps.
2. Whether cut points are window-size-invariant or window-size-dependent.
3. Minimum KEY-role satisfaction (count or ratio) replacing the current all-or-nothing boolean.
4. Minimum LONG_RUN-role satisfaction (count or ratio), same reasoning.
5. Whether/how "only Easy missing" generalizes to a multi-occurrence Easy shortfall.
6. Combination rule when multiple roles are simultaneously degraded.
7. (Role-aware variant only, §B4) Per-role ratio thresholds (Key, Long, Easy) plus a rule for combining all three plus the global count/ratio into one decision.

### Model B1 (worst-week-wins)
8. Confirmation that "worst" uses the existing implicit `Reduce < Maintain < ProgressAsPlanned` ordering (not itself new, but must be made an explicit, citable rule rather than implicit).
9. Handling of a week with unavailable/incomplete decision evidence.
10. (Shared with B2, §G) Cross-structural-week logical-lineage ownership rule for repaired sessions.

### Model B2
11. **B2a:** definition of "latest valid/complete week"; fallback behavior when the nominal last week is incomplete.
12. **B2b:** tie-breaking rule for a no-majority split; whether ties favor severity or recency.
13. **B2c:** exact recency-weight schedule; numeric representation of severity; final thresholds against the weighted score.
14. **B2d:** exact evidence-combination formula (sum/weighted-sum/other); whether role-level evidence combines differently than total-count evidence; whether the combination reaches into anchor-level (`ValidatedSustainableLoad`) concepts or stays confined to decision-level evidence.
15. (Shared with B1, §G) Cross-structural-week logical-lineage ownership rule.

### B-weekly-checkpoint
16. Activation-frequency semantics (what a client-facing "next window" call means once the atomic unit shrinks to 1 week).
17. Persistence/checkpoint schema for weekly tracking (new fields or new table).
18. New idempotency-key scheme, and its interaction with the enclosing 4-week window's own identity (if the window concept is retained at all above the weekly checkpoint layer).
19. Redefinition of `PriorAnchor` — "prior week" vs. "prior window."
20. Block-semantics scope — does a Block on one week block the remaining weeks of the same catalog-generation batch, or are they independent?
21. JIT/Core composition re-architecture or batching strategy to reconcile 1-week decision granularity with the real composition pipeline's multi-week generation unit.

---

## K. False-tradeoff / hybrid check

### Hybrid H1 — window-level decision remains, but summary becomes role-count-aware rather than weekly

This is **not actually a distinct third option** — it is precisely Model A's role-aware variant (§B4) restated. No new architecture is introduced; H1 and "Model A, role-aware" are the same proposal under different names.

### Hybrid H2 — weekly summaries computed, but aggregated evidence feeds one existing window-level `NextWindowLoadDecisionPolicy`-like authority

This is **effectively equivalent to Model B2d** (§E1's "evidence aggregation before final decision" candidate) — both describe: compute per-week evidence, combine the *evidence* (not the *decisions*) into one aggregate, then run one decision rule over the aggregate. H2 is not a new fourth path; it is B2d under a different name, and inherits B2d's exact analysis, tradeoffs, and new-decision list from §E1/§E2 above (including the observation that if the combination is a straight sum, it becomes numerically identical to Model A's global-normalization variant — §B2 — meaning B2d/H2 and "Model A, global-normalized" can collapse into the same runtime behavior even though they are described as different architectures).

### Hybrid H3 — weekly semantic decisions are produced, but one final decision/anchor remains per existing activation

This is **exactly Model B1 or Model B2a/b/c** (§D/§E), all of which already produce N weekly decisions and collapse them to one final decision/anchor while preserving the existing single-activation architecture. H3 is not a new option; it is the defining shared property of B-weekly-summary + (B1 | B2a | B2b | B2c), already fully analyzed above.

**Conclusion for the decision-maker:** the three suggested "hybrids" are not a fourth path between Model A and Model B — they are, respectively, a restatement of Model A's role-aware variant (H1), a restatement of Model B2d (H2), and a restatement of the entire B-weekly-summary family (H3). **The real decision space, once duplicates are removed, is exactly the set already presented: Model A (global or role-aware normalization) vs. Model B-weekly-summary (worst-week, majority, recency-weighted, or evidence-aggregation) vs. the architecturally much larger Model B-weekly-checkpoint.** There is no smaller/simpler architecture hiding between "invent percentage thresholds" and "rewrite the window architecture" beyond what B-weekly-summary itself already is — and B-weekly-summary, per §C1/§H, is architecturally the smaller of the two real options in terms of blast radius (comparable to Model A, far smaller than B-weekly-checkpoint), while preserving per-week evidence granularity that Model A discards.

---

## Unresolved UNKNOWN items

Reported honestly where the repository investigation performed in this phase could not determine the answer without actual implementation:

1. `LongHorizonCheckpointStateEvaluator`'s eligibility gates (availability feasibility, growth-confidence) under B-weekly-checkpoint's 1-week evidence granularity — these frozen gates were designed and tested against multi-week evidence; their behavior at 1-week scope was not traced in this phase.
2. Whether `WindowBoundaryConstraint`/chronological immutability would need to change under B-weekly-checkpoint specifically for schedule-repair candidate scoping (as opposed to activation lifecycle, which is analyzed) — not investigated, since B-weekly-checkpoint was explicitly not to be recommended and its full blast radius was scoped at the level the phase brief required (comparison, not implementation-readiness).
3. Whether safety-flag evaluation under B-weekly-checkpoint would be per-checkpoint or remain window-wide — not determined.

No other cell in the comparison matrix (§H) required an unjustified `UNKNOWN` — every other cell traces to a specific code finding or a direct architectural inference from one.

---

## No recommendation

This document does not recommend, rank, or select Model A, Model B-weekly-summary, Model B-weekly-checkpoint, Model B1, or any Model B2 candidate. All are presented as-is for the human decision-maker.

## Current Adaptation V1 classification

```
TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_CONDITIONALLY_VERIFIED
```

Current blocker: `MULTI_WEEK_WINDOW_DECISION_POLICY` (unchanged from Phase 4M.5A — this phase produced comparison evidence, not a resolution).

## Final classification

```
MULTI_WEEK_DECISION_SEMANTICS_COMPARISON_READY_FOR_PRODUCT_DECISION
```

No production code changed. No canonical spec file changed. No model recommended or selected. No commit, no push, no generalization work started.
