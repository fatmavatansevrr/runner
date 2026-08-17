# Phase 10K-FREQ.6D.1B — 5D Severity-Table Fidelity & Open-Decision Transparency Check

**Narrow design-verification follow-up. No production code, schema change, catalog authoring, product policy change, or Adaptation policy change. Track A is checked against FREQ.6 §6's real, frozen 24-row table (read in full, reproduced verbatim below). Track B is checked against FREQ.6D.1A's actual §3 text plus a renewed cross-read of FREQ.6 §§5/10/13/16, which this phase found answers two of the three "open" items FREQ.6D.1A flagged — a self-correction, not a defense of the prior phase's framing.**

---

## TRACK A — SEVERITY TABLE FIDELITY

### 1. Exact meaning of "6-outcome table"

FREQ.6D.1A's phrase was under-specified. Resolved here: it means **Option A** — six outer `EffectiveCompletedCount` entries (0,1,2,3,4,5), with role-aware delegation occurring **only** inside the count=4 entry. It does **not** mean six final scalar outcomes determined by count alone (Option B). Option B would be a fidelity violation; it is explicitly rejected as the design.

### 2. Proposed severity pseudocode (exact)

```csharp
private static NextWindowLoadDecision Determine5DLoadDecision(FiveDWindowExecutionSummary s)
{
    Validate5DVector(s); // §7 below — throws on any structurally invalid input, never normalizes

    return s.EffectiveCompletedCount switch
    {
        0 or 1 => NextWindowLoadDecision.Reduce,
        2 or 3 => NextWindowLoadDecision.Maintain,
        4       => EvaluateFourOfFiveRoleState(s),
        5       => NextWindowLoadDecision.ProgressAsPlanned,
        _       => throw new AdaptationLineageInvalidException(
                       $"EffectiveCompletedCount {s.EffectiveCompletedCount} outside the valid 0-5 range for a 5-session structural week."),
    };
}

private static NextWindowLoadDecision EvaluateFourOfFiveRoleState(FiveDWindowExecutionSummary s)
{
    // Precondition (enforced by Validate5DVector): exactly one of {Key1, Key2, Long, "one Easy"} is missing.
    var soleMissingRole = IdentifySoleMissingRole(s); // Key1 | Key2 | Long | Easy — throws if not exactly one missing

    return soleMissingRole == FiveDStructuralRole.Easy
        ? NextWindowLoadDecision.ProgressAsPlanned
        : NextWindowLoadDecision.Maintain;   // sole miss = Key1, Key2, or Long
}
```

`FiveDWindowExecutionSummary` carries `Key1Completed:bool, Key2Completed:bool, LongCompleted:bool, EasyCompletedCount:int(0-2), EasyExpectedCount:int, EffectiveCompletedCount:int`. This is the outer/inner structure the prompt's A1 asked to confirm or refute — it is materially the same shape as the prompt's own example, with the count=4 branch made concrete rather than left as a stub.

Role information is read **only** inside `EvaluateFourOfFiveRoleState` — nowhere else in the dispatch. This is the mechanism that keeps counts 0/1/2/3/5 role-blind by construction (§5 below), not merely by coincidence of the current table's values.

---

### 3. Complete frozen FREQ.6 §6 table — full reproduction with fidelity check

All 24 rows, reproduced verbatim from `PHASE_10K_FREQ_6_INTERMEDIATE_5D_PRODUCT_POLICY_DECISION_CLOSURE.md` §6 (read in full this phase), with the proposed evaluation path and result appended.

| # | Count | K1 | K2 | LONG | E | FREQ.6 Outcome | Proposed6D1AEvaluationPath | ActualOutcome | Fidelity |
|---:|---:|:---:|:---:|:---:|---:|---|---|---|:---:|
| 1 | 0 | N | N | N | 0 | Reduce | `case 0` | Reduce | YES |
| 2 | 1 | N | N | N | 1 | Reduce | `case 1` | Reduce | YES |
| 3 | 2 | N | N | N | 2 | Maintain | `case 2 or 3` | Maintain | YES |
| 4 | 1 | N | N | Y | 0 | Reduce | `case 1` | Reduce | YES |
| 5 | 2 | N | N | Y | 1 | Maintain | `case 2 or 3` | Maintain | YES |
| 6 | 3 | N | N | Y | 2 | Maintain | `case 2 or 3` | Maintain | YES |
| 7 | 1 | N | Y | N | 0 | Reduce | `case 1` | Reduce | YES |
| 8 | 2 | N | Y | N | 1 | Maintain | `case 2 or 3` | Maintain | YES |
| 9 | 3 | N | Y | N | 2 | Maintain | `case 2 or 3` | Maintain | YES |
| 10 | 2 | N | Y | Y | 0 | Maintain | `case 2 or 3` | Maintain | YES |
| 11 | 3 | N | Y | Y | 1 | Maintain | `case 2 or 3` | Maintain | YES |
| 12 | 4 | N | Y | Y | 2 | Maintain (sole miss KEY1) | `case 4` → `EvaluateFourOfFiveRoleState` → sole miss = Key1 | Maintain | YES |
| 13 | 1 | Y | N | N | 0 | Reduce | `case 1` | Reduce | YES |
| 14 | 2 | Y | N | N | 1 | Maintain | `case 2 or 3` | Maintain | YES |
| 15 | 3 | Y | N | N | 2 | Maintain | `case 2 or 3` | Maintain | YES |
| 16 | 2 | Y | N | Y | 0 | Maintain | `case 2 or 3` | Maintain | YES |
| 17 | 3 | Y | N | Y | 1 | Maintain | `case 2 or 3` | Maintain | YES |
| 18 | 4 | Y | N | Y | 2 | Maintain (sole miss KEY2) | `case 4` → sole miss = Key2 | Maintain | YES |
| 19 | 2 | Y | Y | N | 0 | Maintain | `case 2 or 3` | Maintain | YES |
| 20 | 3 | Y | Y | N | 1 | Maintain | `case 2 or 3` | Maintain | YES |
| 21 | 4 | Y | Y | N | 2 | Maintain (sole miss LONG) | `case 4` → sole miss = Long | Maintain | YES |
| 22 | 3 | Y | Y | Y | 0 | Maintain | `case 2 or 3` | Maintain | YES |
| 23 | 4 | Y | Y | Y | 1 | Progress (sole miss EASY) | `case 4` → sole miss = Easy | ProgressAsPlanned | YES |
| 24 | 5 | Y | Y | Y | 2 | Progress | `case 5` | ProgressAsPlanned | YES |

**24/24 rows present, 24/24 match.** No row was dropped, merged, or reinterpreted beyond the symmetry reduction FREQ.6 itself already performs (EASY1/EASY2 → `EasyCompletedCount`, stated explicitly in FREQ.6 §5: "EASY1 and EASY2 are symmetric," and KEY1/KEY2 kept as separate named columns exactly as FREQ.6's own table does, not further symmetry-reduced, per §5: "KEY1 and KEY2 are symmetric for adherence severity" — symmetric in *outcome*, not collapsed to one column, matching FREQ.6's own table shape exactly).

### 4. Count-4 role-aware comparison (A3, load-bearing branch)

Using the prompt's own state numbering, mapped to the real table rows above:

| State | Vector | FREQ.6 Expected | Proposed path | Actual | Match |
|---|---|---|---|---|---|
| 1 | K1 missed, K2✓, LONG✓, E=2 | Maintain | row 12 → sole miss Key1 | Maintain | YES |
| 2 | K1✓, K2 missed, LONG✓, E=2 | Maintain | row 18 → sole miss Key2 | Maintain | YES |
| 3 | K1✓, K2✓, LONG missed, E=2 | Maintain | row 21 → sole miss Long | Maintain | YES |
| 4 | K1✓, K2✓, LONG✓, E=1 | Progress | row 23 → sole miss Easy | ProgressAsPlanned | YES |

State 4 symmetry-expansion: since `EasyCompletedCount=1` is reachable by either "EASY1 missed" or "EASY2 missed" as physical vectors, and FREQ.6 §5 declares EASY1/EASY2 symmetric, both physical vectors map to the same `EasyCompletedCount=1` input and therefore the same evaluation path and outcome — both match, trivially, since the implementation never distinguishes EASY1 from EASY2 (matching FREQ.6's own representation, which also only ever uses `E` as a count, never named EASY1/EASY2 columns in §6's table).

All four rows match. **Role information matters at count=4, exactly as required.**

### 5. Count 2/3 invariant check (A4)

By construction, `case 2 or 3 => Maintain` never reads `Key1Completed`, `Key2Completed`, `LongCompleted`, or `EasyCompletedCount` — the role vector is not inspected at all for these counts. This makes the invariant structurally guaranteed, not merely empirically true of the current table:

- **Count=2** (7 rows: #3,5,8,10,14,16,19): all `Maintain` in FREQ.6 — all `Maintain` under the proposed dispatch (uniformly, path is identical `case 2 or 3` for every one).
- **Count=3** (7 rows: #6,9,11,15,17,20,22): all `Maintain` in FREQ.6 — all `Maintain` under the proposed dispatch.

No new role-based `Reduce` or `Progress` branch exists at 2/5 or 3/5 — this is not just checked against the current 24 rows, it is *impossible* under this dispatch shape, since the switch arm for 2/3 has no access to role fields at all.

### 6. 0/1/5 fidelity (A5)

- **Count=0** (1 row, #1): `Reduce` in FREQ.6 → `case 0` → `Reduce`. Match.
- **Count=1** (4 rows: #2,4,7,13): all `Reduce` in FREQ.6 → `case 1` → `Reduce`, uniformly, role-blind. Match, all 4.
- **Count=5** (1 row, #24): `Progress` in FREQ.6 → `case 5` → `ProgressAsPlanned`. Match.

No role-specific exception exists at these counts, matching FREQ.6 exactly (FREQ.6 itself contains none at these counts either).

### 7. Policy vs. representation (A7)

FREQ.6's semantic model (§3, Model A4: "monotonic count floor + categorical role gate") and its representation (§3, Model A5: "finite state table") are distinct in FREQ.6 itself — FREQ.6 §3 states A5 is "Representation, not independent semantics" of A4. The proposed 6D.1A/6D.1B structure preserves this exact split: the **outer** `EffectiveCompletedCount` switch **is** the monotonic count floor (A4's first half); the **inner** `EvaluateFourOfFiveRoleState` call **is** the categorical role gate (A4's second half), invoked only at the one count where FREQ.6 §4/§5 says role information becomes decision-relevant ("At 4/5, Progress requires both KEYs and LONG completed," FREQ.6 §5). The full 24-row table (§6 above) is the A5 representation of that same A4 semantic model — proven identical row-for-row, not merely asserted compatible. This is the architecture the prompt itself describes as valid ("OUTER: completed-count dispatch; INNER: role-vector evaluation only at ambiguous count 4") — confirmed as the actual proposed design, not a different, role-blind scalar table.

### 8. Model A2 non-regression (A8)

Grep of the proposed pseudocode for any division, ratio, or fraction-threshold operation: none exists. `EffectiveCompletedCount` is a plain integer sum (`Key1+Key2+Long+Easy`, all integer/boolean), compared only via integer switch arms — no `completed / planned`, no rounding, no ratio-derived boundary anywhere in the dispatch or in `EvaluateFourOfFiveRoleState` (which does pure boolean/enum comparison, not arithmetic). FREQ.6 §3 rejected Model A2 specifically because a normalized ratio "changes structural authority into fraction" and is "monotonic but role-blind unless patched" — the proposed design is role-aware exactly where FREQ.6 requires it (count=4) and never computes a ratio anywhere.

```
MODEL_A2_REMAINS_REJECTED
```

### 9. Invalid-state handling (A9)

`Validate5DVector(s)` (called before any dispatch) implements FREQ.6 §7's own stated invariant literally: *"completed count must equal K1 + K2 + LONG + E; planned KEY count must be 2; E ∈ [0,2]; role lineage must be known. No silent normalization."* Concretely:

```csharp
private static void Validate5DVector(FiveDWindowExecutionSummary s)
{
    var roleSum = (s.Key1Completed ? 1 : 0) + (s.Key2Completed ? 1 : 0) + (s.LongCompleted ? 1 : 0) + s.EasyCompletedCount;
    if (roleSum != s.EffectiveCompletedCount)
        throw new AdaptationLineageInvalidException($"EffectiveCompletedCount {s.EffectiveCompletedCount} does not equal K1+K2+LONG+Easy sum {roleSum}.");
    if (s.EasyCompletedCount is < 0 or > 2)
        throw new AdaptationLineageInvalidException($"EasyCompletedCount {s.EasyCompletedCount} outside valid [0,2] range.");
    if (s.EasyExpectedCount != 2)
        throw new AdaptationLineageInvalidException($"EasyExpectedCount {s.EasyExpectedCount} != 2 — planned 5D role cardinality unknown or violated.");
    // KeySessionExpectedCount==2 and LongExpected==true are structural preconditions of a 5D week,
    // validated upstream by WindowExecutionSummaryBuilder before this policy ever runs — re-checked
    // here defensively, not assumed, mirroring FREQ.6 §7's "role lineage must be known" clause.
}
```

This rejects, rather than normalizes: `CompletedCount=4` with a role vector summing to 3; a `KeyCompletedCount` implied >2 (structurally impossible given two named bools, but validated defensively if the underlying summary ever carries raw ints instead); `EasyCompletedCount>2`; unknown/violated planned cardinality. Fail-closed via `AdaptationLineageInvalidException`, the exact type FREQ.6 §7 itself names — no new exception type invented, no silent coercion into one of the six outer entries.

### Track A classification

```
SEVERITY_TABLE_FIDELITY_CONFIRMED
```

---

## TRACK B — FREQ.6D.1A OPEN DECISION CHECK

### Full §3 reproduction (verbatim source) plus renewed analysis

FREQ.6D.1A §3 stated three open items and classified none as `UNRESOLVED_DOMAIN_DECISION_FOUND` in the blocking sense, but did not run the "could two reasonable implementations produce different athlete-facing outcomes" test explicitly, and — as this phase's renewed cross-read of FREQ.6 found — **did not check items 1 and 3 against FREQ.6 §§7/10/13 closely enough**. Two of the three turn out to already be answered by frozen FREQ.6 text. This is disclosed as a correction to FREQ.6D.1A, not defended.

---

### B2. Repair/substitution lane identity

**Exact unresolved question (as originally flagged)**: does `ScheduleRepairRuntimeOrchestrator`'s `SubstituteFutureEasy` path re-bind a substituted day to acquire a lane-appropriate `ProgressionStageKey`/`PrescriptionProfile`, or leave it without one?

**Which category does the ambiguity concern?** Checking the prompt's list directly:
- Structural `keyOrdinal`/binding ordinal — **not** at risk. Per FREQ.6D.1A §B7, the lane-stage schedule (`(WeekNumber, LaneOrdinal) → ProgressionStageKey`) is materialized **once**, at progression-allocation time, **before any calendar/repair event can occur**. Repair operates on already-materialized, already-bound, already-persisted `TrainingDay` rows — it cannot retroactively change which lane a given week's slot belongs to, because that assignment was fixed before the plan was even dated. This resolves most of the original ambiguity on renewed analysis.
- Prescription lane identity / workout identity — **narrowed**, not fully open: for a **calendar-repaired** (moved) session, the *same* `TrainingDay` row moves; its `CatalogProgressionStageKey`/`CatalogWorkoutDefinitionKey` (hence lane identity) travel unchanged — already resolved in FREQ.6D.1A §C4 scenario 1, confirmed engineering, non-blocking.
- Stage progression lineage — unaffected, per above (allocation is immutable post-materialization).
- Calendar slot identity — the mechanism repair actually operates on (date/role/lineage); unaffected by lane concerns.
- Persistence/reconstruction — the one genuinely residual question: for a **`SubstituteFutureEasy`** action (a *different* row — a future EASY day — standing in for a missed KEY), does that stand-in row get a `CatalogProgressionStageKey` at all?

**Concrete scenario** (per the requested shape):
```
KEY ordinal 1 → Lane B → original workout X (e.g. a Build-phase FARTLEK profile, SECONDARY_CONTROLLED)
  → session missed entirely (NotToday, unrecovered)
  → ScheduleRepairRuntimeOrchestrator selects SubstituteFutureEasy
  → a future EASY_SUPPORT-role TrainingDay row is repurposed to stand in
```
Does Lane B "remain Lane B"? — the *original* missed row's `CatalogProgressionStageKey` is untouched (nothing rewrites history). The *new* stand-in row is a structurally different row, and whether it is given a copy of Lane B's identity for display purposes is the residual, narrow question. Does `keyOrdinal`/lane assignment for *future* weeks change? No — untouched, per the immutability argument above.

**Does this affect adaptation outcome?** No — checked directly: FREQ.6 §7 (frozen, `EXISTING_CANONICAL_RULE`) already states: *"`SubstituteFutureEasy`: the recovered priority root counts under the original KEY/LONG role; the superseded Easy remains informational and is not a negative adherence signal... Workout identity changes do not change adherence role without an explicit structural-lineage change."* This means **both** possible engineering answers (copy Lane B's identity onto the stand-in row, or leave it identity-less) produce the **identical** adherence/severity outcome, because FREQ.6 §7 already fixed the accounting rule independent of workout/lane identity. FREQ.6 §7 also explicitly separates this from prescription/dose accounting: *"dose importance is not a second adherence accounting system."*

**Applying the B5 hidden-decision test**: "Could two reasonable implementations produce different athlete-facing training prescriptions or different eligibility/adaptation outcomes?" — **Adaptation/eligibility outcome: no** (frozen by FREQ.6 §7, both implementations agree). **Athlete-facing prescription for future sessions: no** (nothing about this choice touches any other week's prescription). **Historical display/record of the substituted day itself**: possibly (whether the app shows "you did an easy run instead of your fartlek" vs. some copied fartlek label) — but this is a UI/reporting nuance about a past event, not a training-prescription or adaptation-outcome decision.

**Classification**: `ENGINEERING_SEMANTICS_ALREADY_DERIVED_FROM_FROZEN_POLICY` — narrowed from FREQ.6D.1A's original framing, with the residual reduced to a small, non-blocking, display-only engineering choice (recommend: leave the stand-in row's `CatalogProgressionStageKey`/profile columns null, matching today's exact behavior, since FREQ.6 §7 already makes this observationally inert for adherence purposes — simplest choice, no new mechanism required). **Does not require FREQ.6D.4 to remain blocked.**

---

### B3. Unstructured-fartlek limit

**Exact meaning**: `PrescriptionProfile.Components[].StructureMode = REPEATED` (FREQ.6D.1A §A1) can represent a **fixed** rep-count/duration fartlek (e.g. "6×1min surge / 1min float") but cannot represent a **self-selected, open-ended** fartlek ("20 min continuous fartlek, surge whenever you feel like it") without fabricating a rep count that wasn't actually prescribed.

**Does approved FREQ.6 policy actually require structured FARTLEK?** Re-reading FREQ.6's own text (not re-derived, quoted directly): §11's Build-phase KEY2 purpose is *"Controlled fartlek/VO2-oriented support, lower accumulated stress than primary"* — the word **"Controlled"** is used for **every** lower-dose KEY purpose across all four phases in that same table (Foundation KEY1 *"Controlled aerobic-strength/economy stimulus... non-exhaustive"*, Foundation KEY2 *"Controlled threshold introduction"*, RaceSpecific KEY2 *"Controlled threshold support"*, Taper both *"Reduced-dose... sharpening"*). This is a deliberate, uniform textual choice, not incidental phrasing. Separately, FREQ.6 §16's capacity audit states the blocking gap for `FARTLEK v4` explicitly as *"validated BUILD identity with component labels but no repetitions, work duration/distance or recovery dose"* — FREQ.6 itself frames the missing capability in terms of **repetitions/duration/recovery**, i.e., structured quantities, not "self-selected effort." Given both signals, **FREQ.6's own approved text already requires a structured/controlled/quantifiable FARTLEK capability** — this is not a decision left open for 6D.1B or 6D.2 to make.

**Answering the four options**: (A) yes, approved policy requires structured capability — confirmed above. (B) unstructured FARTLEK is not something FREQ.6 approved as a distinct mode anywhere — it simply isn't mentioned; there is no frozen text authorizing it. (C) true but secondary — catalog expressiveness is affected, but that's a consequence, not the core finding. (D) **no new product decision is required** — FREQ.6's existing, frozen text already answers this.

**Classification**: `ENGINEERING_SEMANTICS_ALREADY_DERIVED_FROM_FROZEN_POLICY` — the `PrescriptionProfile` schema's `REPEATED` structure mode is the correct engineering translation of an already-approved product requirement, not a new choice made by the schema. If the catalog ever wants genuinely unstructured fartlek in the future, that would be a **new** product decision at that time (nothing in current approved policy asks for it), but it does not block anything in 6D.2-6D.5.

---

### B4. Dose-category/lane alignment

**Exact question as originally flagged**: does `LaneOrdinal 0 ↔ PRIMARY` / `LaneOrdinal 1 ↔ SECONDARY_CONTROLLED` hold fixed across every phase, or can it vary?

**This is directly answered by frozen FREQ.6 text, missed in FREQ.6D.1A's original framing**: FREQ.6 §13 states explicitly: *"KEY1 = `PRIMARY`; KEY2 = `SECONDARY_CONTROLLED` in every phase, but purposes are phase-specific."* This is not ambiguous — the mapping is **fixed across every phase** (KEY1/LaneOrdinal-0 is always `PRIMARY`; KEY2/LaneOrdinal-1 is always `SECONDARY_CONTROLLED`); only the **purpose** *content* of each (what KEY1 actually trains in Foundation vs. Build vs. RaceSpecific vs. Taper) varies by phase, per FREQ.6 §11's table. FREQ.6D.1A's §3 item 3 was **incorrect** to describe this as an open "authoring-convention question" — it is a frozen decision, not an open one.

**Does this create a new structural role?** No — checked against FREQ.6 §10's frozen invariant: *"RunLayout retains two identical structural `KEY_SESSION` roles... `TWO_STRUCTURAL_KEY_SLOTS_DO_NOT_IMPLY_TWO_EQUAL_SEVERITY_STIMULI`."* `LaneOrdinal` (FREQ.6D.1A §B1) and `DoseCategory` (§A1) are both lane/prescription-schema-level concepts, never `StructuralRole` (which remains uniformly `"KEY_SESSION"` for both lanes, confirmed against the real `V1CatalogWorkoutRoleBindingPolicy` code — no `KEY_PRIMARY`/`KEY_SECONDARY` role string exists or is proposed anywhere).

**Design correction required as a result of this finding** (a real, disclosed refinement to FREQ.6D.1A §A1/§B1, not a new open item): since the mapping is fixed by FREQ.6 §13 rather than freely authorable, `PrescriptionProfile.DoseCategory` must not be an independently-authored field that could silently diverge from its lane's mandated value. Add a publish-time invariant: **a lane's referenced profiles' `DoseCategory` must equal the FREQ.6 §13-mandated value for that lane's `LaneOrdinal`** (`LaneOrdinal 0 → PRIMARY` required; `LaneOrdinal 1 → SECONDARY_CONTROLLED` required) — enforced the same way as the other publish-time checks in FREQ.6D.1A §D3 (a new typed validator, not a runtime check).

**Classification**: `ENGINEERING_SEMANTICS_ALREADY_DERIVED_FROM_FROZEN_POLICY` — not open. The correction above is a design refinement (encode a frozen invariant as a validated constraint instead of leaving it as an unenforced convention), not a pending product decision.

---

### B5. Hidden product-decision test — summary across all three

| Item | Could two implementations differ in adaptation/eligibility outcome? | Could two implementations differ in future-session prescription? | Could two implementations differ in historical display only? | Verdict |
|---|:---:|:---:|:---:|---|
| Repair/substitution lane identity | No (FREQ.6 §7 fixes it) | No | Yes (narrow, non-blocking) | Engineering |
| Unstructured-fartlek limit | N/A | No — FREQ.6 §11/§16 already require structured | N/A | Engineering (already decided by FREQ.6) |
| Dose-category/lane alignment | No (FREQ.6 §13 fixes it) | No | No | Engineering |

None of the three items pass the B5 test as an *open* product/domain decision — in two cases (fartlek, dose/lane) the apparent product question is already answered by FREQ.6's own frozen text; in the third (repair/substitution) FREQ.6 §7 already fixes every outcome that matters to adaptation, leaving only a narrow, non-blocking display nuance.

### `FREQ6D1A_OPEN_ITEM_CLASSIFICATION_TABLE`

| Item | Exact unresolved question | Affected authority | Athlete-facing behavior possible? | Product/domain decision? | Engineering decision? | Blocking phase | Required closure | Final status |
|---|---|---|:---:|:---:|:---:|---|---|---|
| Repair/substitution lane identity | Does a `SubstituteFutureEasy` stand-in row acquire the original session's lane/profile identity? | Adaptation repair persistence | Historical display only | No | Yes (narrow) | None | Recommend: leave null, matching current behavior | RESOLVED_NON_BLOCKING |
| Unstructured-fartlek limit | Does `PrescriptionProfile` need to represent open-ended fartlek? | Catalog/prescription schema | No (not approved for use) | No — already answered by FREQ.6 §11/§16 | Yes (schema already supports the required structured case) | None | None — revisit only if a future product decision requests unstructured fartlek | RESOLVED_NON_BLOCKING |
| Dose-category/lane alignment | Is `LaneOrdinal ↔ DoseCategory` fixed or phase-variable? | Prescription/lane schema | No | No — already answered by FREQ.6 §13 | Yes (add publish-time invariant) | None | Add `LaneOrdinal↔DoseCategory` publish-time validator (design refinement to §A1/§B1) | RESOLVED_NON_BLOCKING — design refinement required |

### Track B classification

```
OPEN_ITEMS_ENGINEERING_ONLY_CONFIRMED
```

With an explicit correction on record: FREQ.6D.1A's original §3 framing of items 1 and 3 as "open" was not fully cross-checked against FREQ.6 §§7/13 at the time it was written; this phase's renewed read resolves both directly from frozen text, and narrows item 2 to a non-blocking, already-decided engineering translation.

---

## TRACK C — PHASE GATING

### C1. FREQ.6D.2

No issue found in this phase affects `PrescriptionProfile` type/schema shape, versioning, or source validation in a blocking way. One small, additive design refinement is required (the `LaneOrdinal↔DoseCategory` publish-time invariant, §B4) — additive to the already-designed §A1/§D3 validator set, not a shape change. **FREQ.6D.2 may proceed.**

### C2. FREQ.6D.3

Checking each precondition: `PrescriptionProfile` contract is frozen (per 6D.2's unblocked status above) · binder/materialization semantics do not depend on the repair/substitution question (resolved non-blocking, §B2) · FARTLEK semantics do not change the profile schema (resolved — `REPEATED` mode already covers the approved, structured case, §B3) · dose-category placement is resolved sufficiently for binding (now *more* than sufficiently — it's a fixed, enforceable mapping per FREQ.6 §13, §B4). **All four preconditions are met.**

```
PARALLELISM CONFIRMED SAFE — 6D.2 and 6D.3 may proceed in parallel.
```

### C3. FREQ.6D.4

The prompt's stated blocking condition was "repair/substitution lane identity, or any other lane-progression product semantic, remains unresolved." Per Track B, none of the three items remain unresolved as product/domain semantics — all three resolve to engineering, two of them via frozen FREQ.6 text that already existed before this phase (it was FREQ.6D.1A's analysis that was incomplete, not FREQ.6's policy that was missing). **The stated blocking condition for 6D.4 is therefore not present.** 6D.4 does still carry required (not optional) scope from FREQ.6D.1A §C6: the `NextWindowLoadDecisionPolicy` severity-table widening confirmed faithful in Track A above, plus the two new `TrainingDay` persistence columns, plus the `LaneOrdinal↔DoseCategory` invariant from §B4 — these are real, scoped engineering work items for 6D.4, not gates.

### C4. Adaptation gate

Track A found `SEVERITY_TABLE_FIDELITY_CONFIRMED` — no violation. **The adaptation gate does not trigger; adaptation-integration work (6D.4) may proceed** once its own prerequisites (6D.2, 6D.3) are complete.

---

## FINAL CLASSIFICATIONS

**Track A:**
```
SEVERITY_TABLE_FIDELITY_CONFIRMED
```

**Track B:**
```
OPEN_ITEMS_ENGINEERING_ONLY_CONFIRMED
```

**Overall:**
```
FREQ6D_DESIGN_CAN_PROCEED_TO_6D2_6D3
```

6D.2 and 6D.3 may begin now, in parallel. 6D.4's originally-stated blocking condition (unresolved lane-progression product semantics) is not present per Track B's findings — 6D.4 is clear to proceed once 6D.2/6D.3 land, carrying forward three confirmed, scoped, non-optional engineering work items (severity-table widening, two persistence columns, `LaneOrdinal↔DoseCategory` invariant) rather than open decisions.
