# PHASE 10K-FREQ.6D.3A.1 — Recovery Cardinality Domain Closure

## 1. Scope

This document closes `RECOVERY_CARDINALITY_DOMAIN_DECISION_REQUIRED` for the frozen P1 + P3 architecture at commit `37a05178975382f029041e8de7fc67297c5de196`. It is a domain/product authority decision only. It does not change production code, schemas, contracts, projection, RunningApp, progression, or public APIs.

Recovery remains nested in one repeated executable component. This decision does not split it into structural workout components.

## 2. Evidence reviewed

Evidence classes were kept separate:

- **DIRECT_EVIDENCE:** peer-reviewed literature describes interval work as repeated work bouts interspersed with recovery periods. The threshold-interval review gives examples such as `6 × 2000 m` with recovery *between repetitions*. The repeated-sprint review treats inter-repetition recovery as a programming variable. These sources support between-bout recovery as the normal interval concept, but do not define a universal serialization rule for the final recovery. Sources: [HIIT perspective review](https://pmc.ncbi.nlm.nih.gov/articles/PMC10721680/), [threshold interval review](https://pubmed.ncbi.nlm.nih.gov/36900796/), [repeated-sprint review](https://pubmed.ncbi.nlm.nih.gov/37222864/), and [Seiler & Hetlelid interval study](https://pubmed.ncbi.nlm.nih.gov/16177614/).
- **COMMON_COACHING_CONVENTION:** an unqualified recovery attached to `N × work` normally describes inter-repetition recovery, hence `N-1` transitions. This is a convention, not proof that all plans omit deliberate recovery after the final repetition.
- **EXISTING_APPSEL_CANONICAL_RULE:** none for cardinality or placement; see section 3.
- **PRODUCT_DEFAULT:** Appsel's normal repeated-work meaning is `BETWEEN_REPETITIONS`, while authoring must still state the placement explicitly.

No source reviewed establishes a universal interchange/data convention that silently chooses `N` or `N-1` for every repeated prescription.

## 3. Current Appsel convention audit

Repository searches covered profile schemas and models, validators and tests, catalogs, reference/golden fixtures, generated artifacts, documentation, rendering-like strings, and repeated-work vocabulary.

The canonical Golden Fixture v3 contains real paired examples including `EASY_WITH_STRIDES`, `FARTLEK`, `TEN_K_REPETITIONS`, `RACE_PACE_REPEATS`, and taper activation repeats. They carry `repetitions` plus nested recovery duration/mode, but no `recoveryCount`, `recoveryPlacement`, “between reps,” or “after each rep” field. Several totals are expressly estimates or say recovery is embedded in the session total; those totals cannot prove cardinality. The current profile schema likewise has `repetitionCount` and a recovery quantity but no placement authority.

Classification: **`NO_EXISTING_APPSEL_AUTHORITY`**. Appsel has a repeated-work-plus-recovery shape, but not a canonical final-recovery rule. The absence must not be converted retroactively into an implicit `N` or `N-1` claim.

## 4. Recovery-purpose taxonomy

- `INTER_REPETITION_RECOVERY`: a transition from one work repetition to the next, nested in the repeated component.
- `POST_SET_RECOVERY`: deliberately prescribed activity after the final repetition and before another workout component.
- `COOL_DOWN`: the session-ending transition/recovery component, with its own purpose and prescription.

These are not interchangeable. Under the normal `BETWEEN_REPETITIONS` meaning, `4 × 1000 m, 400 m jog recovery` contains three nested jogs. Activity after repetition four is either a separately represented post-set recovery, a cool-down, or the next component.

## 5. R1 — between repetitions only

`RecoveryCount = N - 1` for supported repeated prescriptions (`N >= 2`). This best matches ordinary interval language, makes the recovery a transition inside one atomic repeated block, avoids a recovery/cool-down double count, and gives simple accounting and rendering. It is insufficient as the only authoring shape because it cannot losslessly express a deliberately prescribed recovery after the final work bout.

Result: **accepted as the domain default and one explicit placement value; rejected as an invisible universal serialization rule.**

## 6. R2 — after every repetition

`RecoveryCount = N`. It coherently expresses a repeated work+recovery cycle and is useful when the final recovery is genuinely part of the prescribed block. Before a `COOL_DOWN`, that last recovery is a distinct prescribed segment; authors must ensure it is not merely duplicating the cool-down transition.

As a universal default it conflicts with common “between repetitions” language and creates a material over-count risk.

Result: **accepted as an explicit, non-default placement value; rejected as a universal default.**

## 7. R3 — explicit raw recovery count

Independent raw counts are superficially expressive but admit incoherent current-model states such as four repetitions with two or seven recoveries. The present repeated component has no grouping/placement grammar capable of explaining those values. Raw count also records an execution result without preserving author intent.

Result: **rejected for authoring.** `RecoveryCount` is derived execution data, constrained to `N-1` or `N` for the current model.

## 8. R4 — explicit recovery placement semantic

The author supplies one of exactly two justified values:

- `BETWEEN_REPETITIONS` → `RecoveryCount = N - 1`
- `AFTER_EACH_REPETITION` → `RecoveryCount = N`

This separates authoring intent from execution cardinality, prevents invalid arbitrary counts, preserves both real athlete-facing meanings, and permits deterministic projection. No speculative placement values are approved.

Result: **selected.**

## 9. `RECOVERY_CARDINALITY_OPTION_MATRIX`

| Option | Evidence fit | Authoring clarity | Losslessness | Invalid-state risk | Distance-accounting clarity | Component atomicity | Rendering | Future extensibility | Implementation complexity | Recommended? |
|---|---|---|---|---|---|---|---|---|---|---|
| R1 Between repetitions | Strong normal-convention fit | High | Low for final-recovery intent | Low | High | High | High | Medium | Low | As explicit default value only |
| R2 After every repetition | Valid but not normal default | High | Low for between-only intent | Low | High | Medium; final recovery needs care | High | Medium | Low | As explicit non-default value only |
| R3 Explicit raw count | Weak | Medium | Low; placement is lost | High | Medium | Low | Low | False flexibility | Medium | No |
| R4 Explicit placement | Strongest combined fit | High | High for current forms | Low | High | High | High | High without speculation | Medium | **Yes** |
| Repository-derived alternative | No authority found | N/A | N/A | High if inferred | Low | Unknown | Low | Unknown | Superficially low | No |

## 10. Selected semantic

The authoring model is R4. Every repeated prescription with recovery must explicitly carry `RecoveryPlacement`; there is no omitted-field default.

The domain default/policy preference for an ordinary interval prescription is `BETWEEN_REPETITIONS`. “Default” directs catalog authors; it is not a deserialization fallback. If the intended workout includes recovery after every work repetition, the author explicitly selects `AFTER_EACH_REPETITION`.

## 11. Authority classification

- Normal between-repetition meaning: `DIRECT_EVIDENCE` plus `COMMON_COACHING_CONVENTION`.
- Existing Appsel cardinality: `NO_EXISTING_APPSEL_AUTHORITY`.
- Explicit-placement requirement: `PRODUCT_DEFAULT` / governance decision, justified by two materially different valid meanings.
- Derived formulas: normative Appsel domain policy established by this closure.

No part of this decision is labeled an existing Appsel canonical rule or a universal rule from literature.

## 12. Post-final recovery semantics

With `BETWEEN_REPETITIONS`, no nested recovery follows the final work bout. A deliberate post-set recovery uses an existing separate `RECOVERY` component when the authoritative `WorkoutDefinition` skeleton contains it; a session-ending transition uses `COOL_DOWN`; otherwise the next component begins. For example, the existing FARTLEK structural vocabulary includes `RECOVERY` before `COOL_DOWN` and can express that distinction.

This decision does not add a component role or permit a profile to violate its `WorkoutDefinition` skeleton. If a workout needs a separate post-set recovery not present in its authoritative skeleton, that is a future versioned definition change, not an alternate cardinality inference.

With `AFTER_EACH_REPETITION`, the final recovery is intentionally inside the repeated component; renderers must say so, and validation/content review must prevent accidental duplication with a following recovery or cool-down prescription.

## 13. Distance examples

For `4 × 1000 m` with `400 m` recovery:

- `BETWEEN_REPETITIONS`: work `4 × 1000 = 4000 m`; recovery `3 × 400 = 1200 m`; repeated block `5200 m`.
- `AFTER_EACH_REPETITION`: work `4000 m`; recovery `4 × 400 = 1600 m`; repeated block `5600 m`.

Thus omission of placement changes the athlete-facing block by 400 m and is not harmless metadata.

## 14. Duration examples

For `6 × 60 s` with `60 s` recovery:

- `BETWEEN_REPETITIONS`: work `6 × 60 = 360 s`; recovery `5 × 60 = 300 s`; block `660 s` (11 minutes).
- `AFTER_EACH_REPETITION`: work `360 s`; recovery `6 × 60 = 360 s`; block `720 s` (12 minutes).

No duration-to-distance conversion is implied.

## 15. Rendering meaning

Future rendering must expose placement, not use ambiguous `work + recovery` shorthand:

- BETWEEN: “4 × 1000 m, 400 m jog between reps.”
- AFTER_EACH: “4 × 1000 m, then 400 m jog after each rep (including the last).”

This phase freezes meaning only; it does not implement rendering.

## 16. Taper / KEY compatibility

R4 is workout-generic. Structured FARTLEK, intervalized THRESHOLD, a secondary controlled KEY, and taper sharpening can each choose the placement that matches authored intent. Reducing repetitions during taper deterministically reduces recovery count through the same formula; no 5D-, taper-, family-, or KEY-specific exception is needed.

## 17. FREQ.6D.2A validation contract

FREQ.6D.2A must encode and test the following rules:

1. A repeated work quantity requires `RepetitionCount >= 2`, a valid recovery quantity, and an explicitly authored `RecoveryPlacement`.
2. Only `BETWEEN_REPETITIONS` and `AFTER_EACH_REPETITION` are permitted.
3. Authors do not supply an independent `RecoveryCount`.
4. BETWEEN derives exactly `N-1`; AFTER_EACH derives exactly `N`.
5. Continuous work forbids repetition count, repeated recovery quantity, and recovery placement.
6. Recovery quantity continues to obey its existing one-of/unit and positivity rules; placement does not authorize mixed or absent quantity.
7. Missing placement is invalid, including for migrated/current catalog content; no reader fallback silently chooses a value.
8. Contradictory or structurally impossible combinations are invalid.

The exact schema/typed-contract implementation is deliberately deferred to 6D.2A.

## 18. Execution projection contract

Process A→B projection is deterministic, one-way, and selection-free:

1. Read the explicit authoring placement.
2. Derive exact `RecoveryCount` with the approved formula.
3. Materialize `RecoveryCount + RecoveryPlacement` in the immutable resolved execution value.
4. Preserve recovery quantity and its unit without lossy conversion.
5. Reject invalid authoring before projection; never repair or guess it.

The execution boundary carries both count and placement. Count prevents RunningApp from calculating `N` versus `N-1`; placement preserves semantic round-trip, unambiguous athlete rendering, diagnostics, and auditability. RunningApp consumes the exact count and must not select or reinterpret it. 6D.3B owns the execution contract; 6D.3C owns this derivation/projector behavior.

## 19. Open-decision check

For current supported repeated forms, no athlete-facing cardinality ambiguity remains: placement is mandatory, the allowed meanings are closed, and each maps to one exact count. The particular placement for each future/catalog profile is content authoring, not a missing domain rule.

There is no remaining `RECOVERY_CARDINALITY_*` product or architecture decision. Future patterns needing arbitrary grouping, multiple recovery types, or partial recovery placement are outside the current model and require a separately justified model change; they are not silently encoded as raw counts.

## 20. Revised downstream gates

- **FREQ.6D.2A:** ready; amend authoring schema/typed contract and validation to encode section 17.
- **FREQ.6D.3B:** gated on 6D.2A; define immutable execution `RecoveryCount + RecoveryPlacement`.
- **FREQ.6D.3C:** gated on 6D.2A/3B; implement exact deterministic derivation and lossless projection.
- **FREQ.6D.3D:** gated on 3B/3C; consume exact execution values without policy inference.
- **FREQ.6D.4 / activation:** remains gated on the engineering and verification chain; this decision alone does not activate runtime behavior.

P1 and P3 remain frozen and are not reopened.

## 21. Final classification

**`RECOVERY_CARDINALITY_DOMAIN_POLICY_APPROVED`**

The blocker `RECOVERY_CARDINALITY_DOMAIN_DECISION_REQUIRED` is closed by explicit R4 authoring, a `BETWEEN_REPETITIONS` normal domain preference, and exact deterministic execution cardinality.
