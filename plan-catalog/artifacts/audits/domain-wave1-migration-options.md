# Domain Blocker Resolution Wave 1 — Migration Options & Recommendations — WAVE1-MIGRATION-001

Read-only. No value is chosen, no field is resolved, no classification changed. This report covers Task
6 (options), Task 7 (recommendations), Task 8 (shared components model), and Task 9 (migration forecast)
for D1, D6, D8, D10, D12.

## Task 6 — option analysis

### D1 — `RUN_LAYOUT_4D.sequenceOrder`

**Option A — `KEEP_REQUIRED` (status quo)**
- Description: retain the field exactly as-is; resolve the blocker by having a product owner explicitly bless the current ordering as an `EXPLICIT_PRODUCT_DEFAULT` convention.
- Advantages: zero schema/model change; zero migration; fastest path to clearing the blocker.
- Risks: does not address the demonstrated redundancy with array order, or the demonstrated silent-disagreement risk (`sequence-order-necessity-audit.md` Q3); the "convention" being blessed has no domain backing beyond "it's what was authored."
- Process A ownership: retained. Process B impact: none (never consumed there).
- Backward compatibility: full. Schema migration impact: none. Artifact version cascade: none (audit-only).
- Effect on Production blockers: removes 1/13 decisions, 0/9 artifacts change.
- Product-owner approval required: yes (blessing an arbitrary convention as intentional).

**Option B — `DERIVE_FROM_COLLECTION_ORDER`**
- Description: remove the explicit `sequenceOrder` field; the slot's position in the `slots` array becomes the sole ordering signal; `RunLayoutValidator`'s contiguity check is replaced by (or becomes redundant with) array indexing.
- Advantages: eliminates the redundancy and the disagreement risk entirely; the array already carries this information today.
- Risks: touches a schema/model shape already used by a published, immutable artifact (`RUN_LAYOUT_4D v1`, in 7 releases); needs a schema-version-exclusive dual-field approach (mirroring Part 2's precedent for exact-versioned references) so `v1` remains readable under its original required-field shape while a new `v2` uses the relaxed shape.
- Process A ownership: retained (validator rewritten, not removed). Process B impact: none.
- Backward compatibility: requires explicit handling (see Task 9); not automatic. Schema migration impact: moderate (new schema version needed). Artifact version cascade: `RUN_LAYOUT_4D` v2 → combination v5 (see Task 9).
- Effect on Production blockers: removes 1/13 decisions; artifact-content-changing (not audit-only).
- Product-owner approval required: yes (schema-shape change).

**Option C — `REMOVE` (pure deletion, no derived-order replacement)**
- Description: delete the field entirely; no ordinal concept survives at all, explicit or derived.
- Advantages: simplest end-state.
- Risks: same migration burden as Option B, plus loses the self-describing convenience of an explicit field when reading a slot in isolation; marginally worse ergonomics than Option B for equal migration cost.
- All other dimensions: same category as Option B.
- Product-owner approval required: yes.

**Rejected without further analysis**: `MOVE_TO_PROCESS_B` — no evidence anywhere in this repository or the
Golden Fixture v3 boundary supports this field having any Process B consumer; Process B already performs
its own independent scheduling (`scheduledDate`) that this field was explicitly designed never to
influence (brief-level prohibition, confirmed in `AUD-017`). `REPLACE_WITH_EXPLICIT_SLOT_ORDINAL` was
folded into Option A/B as not materially different from either (renaming the same field does not change
its redundancy profile).

### D6 / D10 — `EASY_STANDARD.components` / `LONG_RUN_STANDARD.components`

**Option 1 — `MAKE_OPTIONAL`**
- Description: relax `workout-definition.schema.json`'s `required` array so `components` is optional catalog-wide (or conditionally required — see Task 8); `EASY_STANDARD`/`LONG_RUN_STANDARD` may then legitimately omit it, matching the Golden Fixture v3 generated shape (0/23 and 0/10 occurrences ever carry a `components` array for these two keys).
- Advantages: matches the strongest, most direct structural evidence available in this audit (100% consistency across 33 combined fixture instances); removes the need to author a value that doesn't correspond to anything real; relaxing a requirement is backward-compatible by construction (existing documents that already provide the field remain valid).
- Risks: needs a validator or documentation rule stating *when* omission is expected vs. required (tied to the `DistanceAccountingMode` correlation noted in `workout-components-ownership-audit.md`), or it risks becoming an unprincipled per-workout free-for-all — this is exactly why Task 8 recommends a single coherent model rather than an ad hoc per-workout choice.
- Process A ownership: retained. Process B impact: none currently; if Process B is later built to consume `components` as generation guidance, it must already handle "absent = no structural constraint" — worth flagging as a future contract question, not a blocker today.
- Backward compatibility: strong for the schema-only step (relaxing a requirement never invalidates a document that already satisfies the stricter shape). Schema migration impact: schema-only, isolable from any artifact content change. Artifact version cascade: none required for the schema step alone; a **separate**, optional follow-up step (removing the placeholder value from the JSON) would require `EASY_STANDARD`/`LONG_RUN_STANDARD` v3 → `INTERMEDIATE_MODIFIER` v3 → combination v5.
- Effect on Production blockers: resolves D6/D10 only once the placeholder content is actually removed (the schema relaxation alone does not change the audit classification of an existing authored value); 2/13 decisions, 2/9 artifacts, content-changing when completed.
- Product-owner approval required: yes for the schema-shape decision; the evidentiary case is unusually strong (unanimous across 33 instances), which may streamline approval but does not eliminate the need for it.

**Option 2 — `KEEP_BUT_RESHAPE` to a minimal "continuous" descriptor**
- Description: keep `components` required, but formalize a canonical single-entry "this workout is continuous" shape (effectively closer to today's `EASY_STANDARD` value) as the intentional, approved convention for continuous workouts.
- Advantages: preserves a uniform "every workout has a components array" invariant that a not-yet-built Process B contract might expect.
- Risks: still asserts a structure the generated model never produces for these two keys; the existing value is *already* essentially this option, so recommending it changes nothing evidentially and risks "keeping a field merely because it already exists" — the exact anti-pattern this task warns against. Weaker evidentially than Option 1.
- Effect on Production blockers: could resolve D6/D10 as audit-only (classification confirmed, no content change) — but only if a product owner is willing to bless a shape the fixture directly contradicts (0/33 match), which is a materially weaker approval case than Option 1's.
- Product-owner approval required: yes, and harder to justify given the fixture evidence.

**Option 3 (considered and rejected for D6/D10 specifically) — blanket removal of `components` from all four workouts uniformly**
- Rejected because `FARTLEK`/`THRESHOLD_TEMPO` have direct, unanimous fixture evidence of real structure (2/2 each) — removing the field catalog-wide would discard real structural information for those two keys to solve a problem that is specific to `EASY_STANDARD`/`LONG_RUN_STANDARD`. This is the exact "one generic components decision without checking semantic differences" the task instructs against (Task 4/8).

### D8 / D12 — `FARTLEK.components` / `THRESHOLD_TEMPO.components`

**Option 1 — `KEEP_AS_IS_AND_RESEARCH_VALUE`**
- Description: retain the current 3-part shape unchanged; treat the remaining open question as a *value*/vocabulary-precision question (is the generic `MAIN_SET`/`intensityDescriptor` sufficient, or does it need workout-specific labels), to be resolved through ordinary coaching-source research, not a shape change.
- Advantages: matches the strongest fixture evidence for these two keys (2/2 each, always present, always a 3-part warm-up/main-set/cool-down shape); zero schema/model change; cheapest possible resolution path if a coaching source is found.
- Risks: the generic `MAIN_SET` label is measurably less specific than the fixture's own `FARTLEK_MAIN_SET`/`TEMPO_MAIN_SET` — accepting the current shape as final without addressing that gap risks leaving a real precision deficit unaddressed indefinitely.
- Process A ownership: retained. Process B impact: none currently.
- Backward compatibility: full. Schema migration impact: none. Artifact version cascade: none (audit-only, if research succeeds).
- Effect on Production blockers: resolves D8/D12 as audit-only if a coaching source confirms the current descriptors (or an explicit product default is approved for them); 0/9 artifacts change.
- Product-owner approval required: likely yes, to accept the current generic granularity as sufficient rather than pursuing Option 2 below.

**Option 2 — `KEEP_BUT_RESHAPE` via vocabulary expansion**
- Description: expand `WorkoutComponentType` (or add an optional structural sub-field) with workout-specific main-set subtypes mirroring the fixture's demonstrated vocabulary (e.g. distinguishing a "surge-and-float" main set from a "continuous tempo" main set at the type level, not just via free-text `IntensityDescriptor`), without inventing any concrete repetition count or duration.
- Advantages: closes the granularity gap the fixture directly evidences; gives a future Process B contract clearer structural signal.
- Risks: `WorkoutComponentType` is part of the stable, tested Process A/B published boundary (`PublishedBoundaryTests.cs`) — a higher-blast-radius change than reshaping one workout, since it affects every artifact that references the enum; directly overlaps with D3's registry-vocabulary-ownership question (both are "what belongs in the shared Process A/B vocabulary" decisions) and should be researched together, not independently, to avoid two separate ownership conversations reaching inconsistent conclusions.
- Backward compatibility: additive enum expansion is backward-compatible for existing readers; rewriting `FARTLEK`/`THRESHOLD_TEMPO`'s JSON to use new values is a separate, optional, content-changing step. Schema migration impact: schema-only for the expansion itself. Artifact version cascade: only if content is rewritten (`FARTLEK`/`THRESHOLD_TEMPO` v3 → `INTERMEDIATE_MODIFIER`/`TEN_K_WORKOUT_PROGRESSION_V1` v3 → combination v5).
- Effect on Production blockers: resolves D8/D12 only once paired with an actual content update, not from the enum expansion alone.
- Product-owner approval required: yes, jointly with the D3 ownership conversation recommended in `domain-blocker-resolution-plan.md`.

**Option 3 — `MOVE_OWNERSHIP` to a shared component-template/structural-capability model**
- Description: replace the inline `components` array with a reference to a reusable, shared structural template (e.g. a `structuralFamily` enum or a template-reference artifact type), centralizing the "what does a 3-part quality workout look like structurally" concept instead of repeating warm-up/cool-down boilerplate per workout.
- Advantages: eliminates cross-workout duplication of the same warm-up/cool-down shape; scales better if more quality workouts are added later.
- Risks: a substantial schema/model redesign (a new artifact type or shared value-object), a much larger migration than Options 1/2, and warrants its own dedicated architecture review — not a like-for-like alternative to the other two options for a single Wave.
- Effect on Production blockers: would eventually resolve D8/D12, but only after a redesign well beyond this Wave's scope.
- Product-owner approval required: yes, plus an architecture-level review.

## Task 7 — recommendations

| Decision | Recommended status | Architectural rationale | Semantic rationale | Evidence | Risks | Migration implications | Confidence | Product-owner approval required? |
|---|---|---|---|---|---|---|---|---|
| D1 | **`DERIVE`** (`DERIVE_FROM_COLLECTION_ORDER`) | Field is redundant with an already-present, structurally-available ordering signal (array position); its only implemented consumer checks a value-set property fully recoverable from that signal | No domain authority determines which ordinal a role holds; the brief mandates shape, not order | Direct: zero consumers beyond a shape-only validator; zero fixture support for a specific ordering; demonstrated disagreement risk | Schema/model change touches a published artifact type; needs backward-compatible migration design | `RUN_LAYOUT_4D` v2, cascading to combination v5 (see Task 9) | **High** | Yes |
| D6 | **`MAKE_OPTIONAL`** | Field is schema-required today but has zero validator or runtime consumers; nothing in the architecture depends on its presence | Golden Fixture v3's generated model never structures this key with components (0/23 occurrences) | Direct, unanimous, high-volume (23 instances) | Needs a principled trigger condition (Task 8), not an ad hoc per-workout carve-out | Schema-only step is low-risk; content removal (optional follow-up) cascades to combination v5 | **High** | Yes |
| D8 | **`KEEP_AS_IS_AND_RESEARCH_VALUE`** | Current shape matches fixture-evidenced structure directly; no shape defect identified | Fixture confirms a real warm-up/main-set/cool-down structure for this key in both occurrences | Direct, unanimous (2/2), but only structural — no direct evidence for the specific generic label's sufficiency | Generic `MAIN_SET` label is less specific than fixture's `FARTLEK_MAIN_SET`; may need Option 2 later, tied to D3 | None if research resolves the value; none to schema/artifacts either way in the near term | **Moderate** (high on shape, low on final label precision) | Likely yes |
| D10 | **`MAKE_OPTIONAL`** | Same architectural rationale as D6 | Golden Fixture v3's generated model never structures this key with components (0/10 occurrences) | Direct, unanimous (10 instances) | Same as D6 | Same as D6 | **High** | Yes |
| D12 | **`KEEP_AS_IS_AND_RESEARCH_VALUE`** | Current shape matches fixture-evidenced structure directly; additionally confirmed continuous-only (no repetitions field in either occurrence) | Fixture confirms a real, continuous-only warm-up/main-set/cool-down structure for this key | Direct, unanimous (2/2); additional structural evidence the interval/cruise-repeat format belongs to a separate workout key (`TEN_K_REPETITIONS`), not this one | Same granularity risk as D8; additionally, a future interval variant should likely be a new `WorkoutDefinition` key, not a reshape of this one — a design note for later, not a defect today | None to schema/artifacts in the near term | **Moderate-to-high** | Likely yes |

**Where evidence is insufficient, stated explicitly**: D8/D12's recommendation covers *shape* only, with
high confidence; it does **not** cover the specific `intensityDescriptor` *values*, for which evidence is
explicitly insufficient (this mirrors D2/D4's documented source-silence pattern in
`domain-blocker-source-map.md` and is not resolved by this Wave 1 audit).

## Task 8 — shared components model decision

**Recommended model: components required only for structurally composite workouts** (outcome 3 of the
five listed in the task). Rationale: the four in-scope workouts split cleanly and unanimously into two
groups by Golden Fixture v3's own generated shape — `EASY_STANDARD`/`LONG_RUN_STANDARD` (continuous,
0/33 combined occurrences ever have `components`) and `FARTLEK`/`THRESHOLD_TEMPO` (composite, 4/4 combined
occurrences always have `components`). This split is independently corroborated by the already-confirmed
`AllowedDistanceAccountingModes` field (`EXACT_SESSION_TOTAL` for the continuous pair,
`ESTIMATED_SESSION_TOTAL` for the composite pair) — a pre-existing, unrelated field arriving at the same
grouping is strong architectural corroboration, not coincidence.

This is **not** "components optional for all" (outcome 2) — that would discard real, evidenced structure
for `FARTLEK`/`THRESHOLD_TEMPO`. It is **not** "components required for all" (outcome 1, status quo) —
that forces a fabricated structure onto `EASY_STANDARD`/`LONG_RUN_STANDARD` that the generated model never
produces. It is **not** a full "structural-capability model" or "generate elsewhere" redesign (outcomes 4/5)
— both are legitimate longer-term directions (see D8/D12 Option 3 above) but are larger than what the
evidence in this Wave demands; a smaller, evidence-matched step should come first.

**Mechanism recommendation** (not a value, an architectural direction): condition the requirement on
`AllowedDistanceAccountingModes` (or an equivalent explicit `structuralFamily`-style discriminator) rather
than leaving it as an unconditional per-workout free choice — this directly satisfies Task 8's instruction
not to "allow each workout to adopt an unrelated ad hoc interpretation."

## Task 9 — migration forecast (for the recommended outcomes above)

| Decision | Schema version required? | Contract/model change? | New workout version? | New layout version? | Parent refs affected | New master version? | New combination version? | New Pilot release? | Historical compatibility |
|---|---|---|---|---|---|---|---|---|---|
| D1 (`DERIVE`) | **Yes** — `RUN_LAYOUT` needs a relaxed/optional `sequenceOrder` shape at a new schema version, while v1's original required shape must remain independently readable | **Yes** — `LayoutSlotDefinition.SequenceOrder` becomes optional/removed; `RunLayoutValidator`'s contiguity check rewritten to use array index | No | **Yes** — `RUN_LAYOUT_4D` v2 | `TEN_K__4D__INTERMEDIATE v4.layout` | No | **Yes** — v5 | **Yes**, to publish/verify v5 | `RUN_LAYOUT_4D` v1 must remain byte-identical and independently verifiable across all 7 existing releases |
| D6/D10 (`MAKE_OPTIONAL`, schema step only) | **Yes** — relax `required` in `workout-definition.schema.json` | **Yes** — `WorkoutDefinition.Components` becomes nullable, mirroring the existing `AllowedDistanceAccountingModes?` precedent | **No**, for the schema step alone (existing content still satisfies the relaxed schema unchanged) | No | None yet | No | No | No, for the schema step alone | Full — relaxing a requirement never invalidates existing documents |
| D6/D10 (optional follow-up: remove placeholder content) | No additional schema change beyond the above | No additional model change | **Yes** — `EASY_STANDARD`/`LONG_RUN_STANDARD` v3 | No | `INTERMEDIATE_MODIFIER v2.eligibleWorkouts` (and possibly `TEN_K_WORKOUT_PROGRESSION_V1.workoutCandidates`) | Only if `TEN_K_WORKOUT_PROGRESSION_V1` changes | **Yes** — v5 (shared with D1 and any other same-wave change) | **Yes** | v1/v2 of both workouts remain untouched and fully readable |
| D8/D12 (`KEEP_AS_IS_AND_RESEARCH_VALUE`) | No | No | No | No | None | No | No | No | N/A — no change |

**Separation of change types, applied consistently with `domain-blocker-version-cascade-forecast.md`'s
outcome model**:
- **Audit-only reclassification**: D8, D12 (if research succeeds) — zero schema/artifact/release impact.
- **Schema-only change**: D6, D10's first step (relaxing `required`) — no artifact version bump required by itself.
- **Artifact-content change**: D1 (necessarily, since the field's presence itself is the defect); D6/D10's
  optional second step (removing placeholder content) — both require new immutable artifact versions and
  cascade to a shared combination v5, per the efficiency principle already established in
  `domain-blocker-version-cascade-forecast.md` (multiple same-wave content changes collapse onto one new
  root version, not one per decision).
- **Process B contract change**: **none identified for any of the five decisions.** No Process B code
  exists in this repository to change, and no evidence surfaced that any of these five fields is, or should
  become, Process-B-generated output rather than Process-A-authored catalog knowledge. (D3's registry
  ownership question — out of scope for this Wave — is the one blocker in the full 13-decision set with a
  genuine Process B contract dimension; D8/D12's vocabulary-expansion option (Option 2 above) would touch
  the same shared-boundary territory only if pursued later.)

## Acceptance-rule cross-check (informational, not a resolution)

Per the acceptance standard defined in `domain-blocker-resolution-plan.md` (Task 8 of the prior Wave 1
planning task), this audit confirms — without applying — the following prerequisites are now satisfied
for a *future* resolution task to proceed:
- D1: field-removal prerequisites are now evidenced (`no required semantic information is lost`,
  `no current consumer depends on it` — both directly demonstrated in `sequence-order-necessity-audit.md`);
  `historical schemas remain readable` and `bundle compatibility` still require explicit migration design
  (Task 9 above), not yet performed.
- D6/D10: the "field may remain optional" path is well-evidenced; migration impact is now documented
  (Task 9 above).
- D8/D12: no removal or reshape prerequisite is claimed satisfied — these remain `KEEP_AS_IS`-track
  decisions pending ordinary value research, not schema/shape decisions.

## Final status: option analysis, recommendations, shared-model decision, and migration forecast complete for D1/D6/D8/D10/D12. No classification was changed; no schema, model, validator, or catalog file was modified.
