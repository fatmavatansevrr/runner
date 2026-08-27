# Phase 10K-FREQ.6D.20 — 10K LongHorizon Target-Finish-Time Source Persistence & Restart Authority Decision

**Phase type:** DOMAIN / PRODUCT AUTHORITY + PERSISTENCE SEMANTICS DECISION
**Parent phase:** FREQ.6D.19 (`DONE (PARTIAL)`, `INTERMEDIATE_5D_LONGHORIZON_GE_RUNWAY_CORE_BOUNDARY_AND_DUAL_KEY_REPAIR_DARK_VERIFIED_TARGET_FINISH_TIME_PRODUCT_DECISION_REMAINING`)

No production code, tests, migration, routing, or catalog content is authored in this phase. This is a decision document only.

## 0. Governance verification

`PHASE_LEDGER.md` row 99 confirms FREQ.6D.19 as the latest completed phase, `DONE (PARTIAL)`, with its remaining blocker recorded exactly as: a restarted LongHorizon plan cannot supply the `TargetFinishTimeSource` classification `GOAL_PACE_TEN_K` requires because that provenance is not durably available anywhere. FREQ.6D.19's own report confirms organic GE→Runway→Core transition, real persisted Core 2-KEY materialization with exact lanes 0/1, ProfileBacked lineage reload, secondary-KEY repair preserving `LaneOrdinal=1`, deterministic continuation, and date-order reversal `NOT_REACHABLE_UNDER_VALID_REPAIR_CONSTRAINTS` — none of this is reopened here. `MASTER_ROADMAP.md` recorded `NEXT_PHASE_NOT_YET_SCHEDULED`. Searched `PHASE_LEDGER.md`/`MASTER_ROADMAP.md`/phase-report filenames for `FREQ.6D.20` — unreserved. Scheduled and committed (`c44851c`); proceeding directly into the decision.

## 1. Objective

Resolve: what durable authority must a confirmed/restarted 10K LongHorizon plan carry so later JIT Core materialization can reconstruct the same `TargetFinishTimeSource` evidence semantics that existed at original generation.

## 2. TARGET_FINISH_TIME_PROVENANCE_DATAFLOW

| Step | Numeric target available? | Source classification available? | Persisted? | Derived? | Lost? |
|---|---|---|---|---|---|
| `GenerateRacePlanPreviewRequest` (public transport DTO) | Yes — caller-supplied, required | Yes — caller-supplied, required enum (validated: if `ProductAverage`, seconds must exactly equal `CanonicalTargetFinishTimePolicy`'s canonical value or the request is rejected 400) | No (request only) | No — honest client-supplied classification, never computed by the backend | — |
| `GeneratePreviewRequest` (internal shape) | Yes, nullable | Yes, nullable | No | No | — |
| Runtime condition resolution (`PaceSourceResolver` → `GoalFeasibilityResolver`) | Yes, read from request | Yes, read from request (`GoalFeasibilityResolver.cs:176`: `ProductAverage` → `CHALLENGING`; otherwise `NotEvaluated` unless independent recent-race evidence exists) | No (in-memory only) | No — read-through, never derived | — |
| `CatalogPreviewSnapshot`/`ResolverInputSnapshot` (frozen preview record) | Yes | Yes (`NormalizedInput.TargetFinishTimeSource`) | **No** — this snapshot type is never written to `RunningApp.Persistence` anywhere (confirmed by exhaustive grep); its own doc comment says a future "Phase 4E.2" was meant to teach confirm to persist a stored snapshot like it, and nothing ever wired that in | No | **Yes — this is the exact point provenance is lost for every plan type, LongHorizon or not** |
| Plan confirmation — general (`CatalogPlanConfirmationService.BuildCatalogTrainingPlan`) | Yes — copied to `TrainingPlan.TargetFinishTimeSeconds` | **No** — `snapshot.NormalizedInput.TargetFinishTimeSource` is available in-memory at this exact call site but is never copied anywhere; `TrainingPlan` has no column for it | Seconds: yes. Source: **no** | — | Source lost here (confirmed, not merely latent) |
| Plan confirmation — LongHorizon (`LongHorizonPublicPlanService.cs:321`) | Yes — same `TargetFinishTimeSeconds`-only pattern | No, same gap | Seconds: yes. Source: **no** | — | Same loss point |
| Persisted `TrainingPlan` row | Yes (`TargetFinishTimeSeconds` column) | No column exists | Seconds only | — | Confirmed permanently lost from this point on |
| Persisted `LongHorizonRollingPlanState` row | **No** — this entity does not persist `TargetFinishTimeSeconds` either, only loosely-typed `GoalType`/`GoalDistance`/`Level` strings | No | No | — | Both lost, but immaterial: `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` already separately loads the associated `TrainingPlan` row (`planRow`) at the top of the method, so the numeric seconds value IS still reachable via that join even though `LongHorizonRollingPlanState` itself doesn't carry it |
| LongHorizon restart (`LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`) | Reachable via `planRow.TargetFinishTimeSeconds` | **Unreachable — no source of truth exists anywhere by this point** | — | — | Confirmed |
| `ContinueJitCompositionAsync` (FREQ.6D.19's own added parameters) | Accepts `targetFinishTimeSeconds` but no real caller supplies it | Accepts `targetFinishTimeSource` but no real caller supplies it | — | — | Both default null in every real production call today |
| `GOAL_PACE_TEN_K` (`CatalogSessionPrescriptionPlanner.cs:252-261`) | N/A | **Requires** `Evaluated` + `REALISTIC`/`CHALLENGING` | — | — | Throws `CatalogGoalPacePrescriptionUnsupportedException` today for any real 5D LongHorizon restart reaching this workout |

**The exact point provenance is lost: plan confirmation.** The value exists in-memory (`snapshot.NormalizedInput.TargetFinishTimeSource`) at the moment `CatalogPlanConfirmationService`/`LongHorizonPublicPlanService` build the persisted `TrainingPlan` row, and is simply never copied — because `TrainingPlan` has no column to receive it. This is true for **every** plan type (Core-only, Runway, LongHorizon alike), not a LongHorizon-specific gap. It is only a *live, blocking* problem for LongHorizon, because LongHorizon is the only plan type that reconstructs Core generation much later from durable state alone, long after the original in-memory request is gone; a normal Core-only/Runway plan generates once, synchronously, while the value is still in scope, so the same underlying gap there is latent rather than fatal.

## 3. Enum / classification inventory

Canonical type: `RunningApp.Domain.Enums.TargetFinishTimeSource` (`backend/RunningApp.Domain/Enums/TargetFinishTimeSource.cs`). Exactly two values exist — no third "recent-race-derived" source classification exists in this enum (recent-race evidence is a separate concept, `PaceSourceResolver`'s `RECENT_RACE` output, which is evaluated independently and does not set `TargetFinishTimeSource`):

| Value | Evidence meaning | Who creates it | Affects eligibility? | Affects pace semantics? | Currently persisted? |
|---|---|---|---|---|---|
| `ProductAverage` | The numeric target is the canonical, distance-specific product-average finish time (`CanonicalTargetFinishTimePolicy`) — a planning reference, not a demonstrated capability | The client, honestly declaring it used the product's own suggested default (validated server-side: seconds must exactly match the canonical value) | Yes — `GoalFeasibilityResolver` accepts it unconditionally as `CHALLENGING` (PHASE4D_4_1 governance decision), bypassing the independent-evidence requirement `UserDefined` would otherwise need | Yes — this classification is precisely why the backend does not reject an unevidenced target as `UNSUPPORTED` | **No — nowhere** |
| `UserDefined` | The user typed their own specific goal time | The client, on direct user input | Yes — without independent evidence (a real recent race), `GoalFeasibilityResolver` returns `NotEvaluated`, which `CatalogSessionPrescriptionPlanner`'s `GOAL_PACE_TEN_K` gate treats as failing "requires REALISTIC or CHALLENGING" | Yes | **No — nowhere** |

No third value exists, and this phase does not invent one — §37's own instruction ("no new pace-source category unless existing authority proves one is missing") is satisfied: the existing two-value enum is sufficient for every scenario this phase traced; the gap is persistence, not classification richness.

## 4. Source is provenance, not numeric value — frozen

**Frozen.** Direct evidence: `GenerateRacePlanPreviewRequest`'s own validator requires that when `TargetFinishTimeSource = ProductAverage`, `TargetFinishTimeSeconds` must exactly equal `CanonicalTargetFinishTimePolicy`'s fixed canonical value for the goal distance (e.g., 58:00 for TEN_K). This means **a `UserDefined` request can legitimately supply the exact same numeric seconds value** (a user might genuinely want to finish in 58:00) — the two source classifications are not distinguishable by the numeric value alone. Reverse-inferring source from seconds (e.g., "58:00 exactly ⇒ must be ProductAverage") is therefore **rejected outright**: it would misclassify a genuine user-defined 58:00 goal as a product default, silently changing which feasibility rule applies to it. `TargetFinishTimeSource` is confirmed semantic provenance that cannot be safely reconstructed from the numeric value.

## 5. Why GOAL_PACE requires source

Traced to `CatalogSessionPrescriptionPlanner.cs:252-261`: a `GOAL_PACE_TEN_K` workout requires the caller's resolved `GoalFeasibilityResolver` output to be `Evaluated` with `REALISTIC` or `CHALLENGING`, else throws `CatalogGoalPacePrescriptionUnsupportedException`. Tracing one level up, `GoalFeasibilityResolver.cs` requires `TargetFinishTimeSource` specifically because of **prescription semantics governed by an explicit governance decision (PHASE4D_4_1)**: the resolver must never treat an unevidenced user-typed goal as equivalent to the product's own recommended default — `ProductAverage` gets a free pass to `CHALLENGING` precisely because it is the product's own reference value, not a claim about the runner's demonstrated capability, while a bare `UserDefined` target with no independent evidence (no recent race) is correctly left `NotEvaluated` rather than either accepted or rejected. This is not an incidental DTO requirement — it is the load-bearing mechanism that keeps `GOAL_PACE_TEN_K` prescriptions from being generated against an unevidenced, potentially-unrealistic user goal.

## 6. Canonical owner

**Selected: (A) plan-level immutable evidence.** `TargetFinishTimeSource` is a fact about the confirmed plan's own original goal-setting request — identical in kind to `TargetFinishTimeSeconds`, `RaceDate`, `GoalDistance`, `Level`, all of which already live on `TrainingPlan`. It is not:
- (B) rolling LongHorizon state — the value does not vary per rolling window or checkpoint; storing it there would duplicate plan-level fact across every checkpoint row for no benefit, and `LongHorizonRollingPlanState` doesn't even carry the numeric seconds today, confirming this was never treated as rolling-state-owned.
- (C) TrainingDay/session — irrelevant at that granularity.
- (D) runtime-only request context — this is exactly the model that already fails today (lost at confirmation).
- (E) candidate/manifest — the candidate is shared across many users/plans; target finish time is per-plan, per-user evidence, not catalog content.

## 7. Plan-level vs rolling-state persistence — model comparison

| | Semantic ownership | Immutability | Restart determinism | Duplication | Migration impact | Historical compat. | Core/Runway reuse | Future HM/Marathon reuse |
|---|---|---|---|---|---|---|---|---|
| **A: plan-level (`TrainingPlan`)** | Exact — matches where `TargetFinishTimeSeconds` already lives | Natural — set once at confirm, never rewritten | High — one row, one join away from every restart path | None | One column, one table | Trivial — nullable column, existing rows simply null | Automatic — `TrainingPlan` is shared by every plan type | Automatic — race-plan evidence generalizes without change |
| **B: rolling state** | Wrong — source doesn't vary per rolling window | Would require write-once-per-plan discipline duplicated into a table designed for per-window mutation | Adds an extra join/duplication for no benefit | Full duplication of a plan-level fact into every LongHorizon plan's rolling row | New column on a dark-only table, LongHorizon-specific | Same nullability question, but now LongHorizon-only | None — Core-only/Runway plans have no rolling state at all, so this model can't serve them | None |
| **C: derive on restart** | N/A | N/A — nothing to derive from (§4 rules this out: numeric value is ambiguous, and no other deterministic artifact is ever persisted) | **Fails** — cannot be deterministic per §4's own two-source-same-number ambiguity | None, but only because it doesn't actually work | None | N/A | N/A | N/A |
| **D: complete evidence object** | Correct in spirit, but `TrainingPlan` already has the numeric half of exactly this pair; a new wrapper object around two existing/near-existing scalar fields adds an aggregate with no behavior of its own | Same as A | Same as A | None beyond A | Same as A (still just one new column) | Same as A | Same as A | Same as A |

**Selected: Model A**, with the "atomic pair" framing from Model D applied *conceptually* (§8) but implemented as two plain scalar columns on `TrainingPlan` (`TargetFinishTimeSeconds`, already present; `TargetFinishTimeSource`, new) rather than a separate object/table — mirroring how this exact same "both-null-or-both-present" pairing convention is already expressed as plain sibling columns elsewhere in this codebase (`LongHorizonRollingSessionState.CatalogPrescriptionProfileKey`/`CatalogPrescriptionProfileVersion`, FREQ.6D.13), not as a wrapper type. No rolling-state duplication is needed: `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` already loads the associated `TrainingPlan` row (`planRow`) at the top of the method for `GoalType`/`GoalDistance`/`Level`/`DaysPerWeek` — the new `TargetFinishTimeSource` column is read from that exact same already-joined row, zero new queries.

## 8. TARGET_FINISH_TIME_EVIDENCE_INVARIANT

- Source cannot exist without a numeric time (there is nothing to classify the provenance of).
- A numeric time can legitimately exist without source **only** for historical rows created before this decision is implemented (§18-22) — never for a new confirmation going forward.
- A legitimate "no target requested" state already exists independently: `TargetFinishTimeSeconds is null` (both request-level and persisted), which `GoalFeasibilityResolver.cs:69-73` already classifies `NOT_REQUESTED` unconditionally, before source is even consulted. This is a third, orthogonal state — not a source value.
- **Invariant for every NEW confirmation from this decision forward: `TargetFinishTimeSeconds` and `TargetFinishTimeSource` are both-null or both-present** — never one without the other. (Both-null = no target requested; both-present = a target was requested, tagged with its exact provenance.)
- Historical rows may legitimately have `TargetFinishTimeSeconds` present with `TargetFinishTimeSource` null — this is the **only** permitted exception, and it must remain visibly distinguishable from "no target requested" (i.e., the invariant is enforced going forward, not retroactively — see §18-24).

## 9. Confirmation boundary

**Frozen: plan confirmation** (`CatalogPlanConfirmationService.BuildCatalogTrainingPlan` / `LongHorizonPublicPlanService`'s equivalent LongHorizon confirm path) is the exact existing point where `snapshot.NormalizedInput.TargetFinishTimeSeconds` is already read and committed to durable state — `TargetFinishTimeSource` must be captured from the identical, already-in-scope `snapshot.NormalizedInput.TargetFinishTimeSource` at this same call site, not preview generation (too early — nothing is committed yet, a preview can be regenerated/discarded) and not LongHorizon initialization or first Core composition (too late — the original in-memory request is already gone by then, which is exactly the bug this decision closes). This is the repository's own existing "user/product decisions become committed" boundary — every other confirmed field (`RaceDate`, `GoalDistance`, `Level`, `PreferredDays`) already freezes here.

## 10. Restart semantics

A restarted LongHorizon plan must, and per this decision will:
- Read the persisted `TrainingPlan.TargetFinishTimeSource` (and `TargetFinishTimeSeconds`) verbatim — never re-run source selection, never re-derive from seconds, never query "today's" `CanonicalTargetFinishTimePolicy` value, never reinterpret.
- If both are present, project them unchanged into `ContinueJitCompositionAsync`'s existing (FREQ.6D.19-added) `targetFinishTimeSeconds`/`targetFinishTimeSource` parameters.
- If both are null (no target was ever requested), project null for both — `GOAL_PACE_TEN_K` availability is then governed entirely by the pre-existing, unrelated `NOT_REQUESTED`/`UNSUPPORTED` path, unchanged.

## 11. `ProductAverage` special case

- The resulting numeric target (`CanonicalTargetFinishTimePolicy`'s fixed value for the goal distance) is **already persisted** today as `TrainingPlan.TargetFinishTimeSeconds` (confirmed, §2) — no new persistence needed for the number.
- `CanonicalTargetFinishTimePolicy`'s table is a fixed, hardcoded set of constants (28min/58min/2:05/4:21 for 5K/10K/HM/Marathon) — not versioned, not currently subject to change without a code deploy. This decision does **not** require or propose adding versioning; it only requires that restart never re-reads this table to "recompute" what a historical plan's target should be.
- Restart must preserve the original numeric value verbatim (already true — it's a durable column) and the original source classification verbatim (the gap this decision closes) — **never** silently recompute a newer product average, even if `CanonicalTargetFinishTimePolicy`'s table value were to change in a future release. The persisted seconds value is authoritative for that plan forever.

## 12. User-provided target case (control)

Traced identically: a `UserDefined` request persists `TargetFinishTimeSeconds` exactly as typed (already true today) and, per this decision, `TargetFinishTimeSource = UserDefined` (the new column). Restart reads both back unchanged. This proves the design is symmetric — it is not a `ProductAverage`-only special case; both of the enum's two values follow the identical persist/restart contract.

## 13. Other existing source cases

Only two values exist (§3) — both fully covered by the identical persist/restart contract above (§10-12). No source value is left unaddressed.

## 14. LongHorizon vs Core/Runway ownership

Confirmed **not** LongHorizon-specific (§2's own dataflow trace: the identical gap exists in `CatalogPlanConfirmationService`, the general Core-only/Runway confirm path). This decision does **not** create a `LongHorizonTargetFinishTimeSource` or any LongHorizon-specific concept — the new column lives on the shared `TrainingPlan` entity, and LongHorizon restart simply consumes the same generic plan authority every other plan type will also now correctly persist.

## 15. Current persisted plan audit

`TrainingPlan` (`backend/RunningApp.Domain/Entities/TrainingPlan.cs`, full entity read): has `TargetFinishTimeSeconds` (int?) but no `TargetFinishTimeSource` property, and no generic JSON/evidence/snapshot blob column suited to carrying it — `CatalogDependencyVersionsJson` exists but is documented and used exclusively for dependency-version tracking, not goal-evidence provenance; repurposing it would conflate two unrelated concerns for no benefit over a dedicated column. `LongHorizonRollingPlanState` has no numeric target-time field at all today, and no JSON evidence payload column either. `CatalogPreviewSnapshot`/`ResolverInputSnapshot` (which DO carry the value) are confirmed never persisted anywhere in `RunningApp.Persistence` — the "Phase 4E.2" mechanism referenced in `CatalogPreviewSnapshot`'s own doc comment that would have wired snapshot persistence into confirm was never built. **No existing column, JSON blob, or snapshot mechanism already has the correct conceptual slot with unpopulated data** — this is confirmed to require a genuinely new field, not a wiring-only fix.

## 16. Migration necessity

**`NEW_PERSISTED_FIELD_REQUIRED`.** Confirmed by direct inspection (§15) — no existing storage location can receive this value without a schema change.

## 17. New field design (design only, not implemented)

- **Aggregate/table**: `TrainingPlan` (`TrainingPlans` table).
- **Field name**: `TargetFinishTimeSource`, mirroring the existing `TargetFinishTimeSeconds` naming exactly.
- **Type**: the existing `RunningApp.Domain.Enums.TargetFinishTimeSource` enum (no new enum). Persisted as the enum stored the same way this repository already stores comparable enums on this entity (e.g., `GoalType`/`GoalDistance`/`Level` are persisted as `string` on `TrainingPlan` per the reconnaissance — for consistency with that existing convention on this exact table, `TargetFinishTimeSource` should be persisted as its string name, not an integer, matching the table's own established pattern rather than introducing a new enum-storage convention).
- **Nullability**: nullable (`string?` at the column level) — required for historical-row compatibility (§23).
- **Index**: none — this field is never queried/filtered on, only read alongside its owning row.
- **Constraint**: none beyond the application-level both-null-or-both-present invariant for new rows (§8) — not enforced at the database level (matching this repository's own established convention: the identical `CatalogPrescriptionProfileKey`/`CatalogPrescriptionProfileVersion` both-null-or-both-present invariant on `LongHorizonRollingSessionState` is also application-enforced only, no DB constraint).
- **Write boundary**: `CatalogPlanConfirmationService.BuildCatalogTrainingPlan` and `LongHorizonPublicPlanService`'s equivalent confirm call site — both already write `TargetFinishTimeSeconds` from `snapshot.NormalizedInput` at this exact point; add `TargetFinishTimeSource = snapshot.NormalizedInput.TargetFinishTimeSource` alongside it, in both places.
- **Read boundary**: `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`'s already-loaded `planRow`, threaded into `ContinueJitCompositionAsync`'s existing (FREQ.6D.19-added) `targetFinishTimeSeconds`/`targetFinishTimeSource` parameters — no new query.

## 18-19. Historical 4D LongHorizon plans / no fake backfill

Existing plans (4D and 5D alike, LongHorizon or not) have `TargetFinishTimeSeconds` persisted but will have `TargetFinishTimeSource = null` after this column is added — because the value was never captured for them and cannot be recovered (§20). **No backfill is approved.** Explicitly rejected per this phase's own instruction: no `if seconds present → assume ProductAverage`, no `infer UserDefined from a numeric range or from seconds != canonical value`. Both would fabricate provenance that never existed, and §4 already proved the numeric value alone cannot distinguish the two sources.

## 20. Historical source recovery audit

Searched every candidate deterministic-recovery artifact:
- Confirmed request snapshot (`CatalogPreviewSnapshot`/`ResolverInputSnapshot`): never persisted, confirmed by exhaustive grep of `RunningApp.Persistence` — **zero hits**.
- Runtime-condition trace / decision-trace records: no persisted decision-trace table exists for this data.
- Plan-generation evidence / audit/event records: none found that capture `TargetFinishTimeSource` specifically.

**Classification: `UNKNOWN_LEGACY`.** No exact historical source can be deterministically recovered for any plan confirmed before this decision's implementation phase ships. This is explicit and permanent for those rows — not "not yet recovered."

## 21. Legacy restart options

Evaluated:
- **L1 (preserve Legacy path when no ProfileBacked goal-pace evidence is needed)** — already true and unaffected: a 4D LongHorizon plan whose Core progression never reaches a `GOAL_PACE_TEN_K` week is entirely unaffected by this whole gap (4D Core is Legacy throughout per FREQ.6D.19's own finding) — nothing to change here.
- **L2 (fail closed only when a future GOAL_PACE session genuinely requires unavailable provenance)** — **selected**. A historical plan with `TargetFinishTimeSource = null` (`UNKNOWN_LEGACY`) that happens to reach a real `GOAL_PACE_TEN_K` week will correctly continue to hit the exact same typed `CatalogGoalPacePrescriptionUnsupportedException` this gap already produces today — this decision does not change that behavior for historical rows, it only prevents the SAME failure from being permanent and unconditional for every future confirmed plan.
- **L3 (one-time deterministic recovery)** — rejected; §20 confirms no exact evidence exists to recover from.
- **L4 (typed re-confirmation/user action)** — not evaluated further here; out of scope for a persistence-authority decision, and no existing re-confirmation product flow was found to reuse. A future phase could consider prompting an affected user to re-declare their goal, but that is a product-experience decision, not a persistence-semantics one, and is not required to close this phase's own objective.
- **L5** — no other existing mechanism found.

No source is ever silently assigned to a historical row.

## 22-24. New vs historical plans, nullability, no unknown-as-ProductAverage

- **New confirmed plans**: `TargetFinishTimeSource` is mandatory and durable per the both-null-or-both-present invariant (§8) — enforced at the application layer at the confirmation write boundary (§17), not weakened.
- **Historical legacy plans**: `TargetFinishTimeSource` remains nullable at the database level (§17) precisely to represent this legitimate, permanent `UNKNOWN_LEGACY` state without corrupting new-plan invariants.
- **Explicit rejection**: null/unknown provenance is never treated as `ProductAverage` (or `UserDefined`) anywhere in restart or JIT logic — `UNKNOWN_LEGACY` must read back as null, and any downstream consumer (`GoalFeasibilityResolver` via the JIT path) must treat null exactly as it already treats "no source supplied" today (§10-11 of `GoalFeasibilityResolver.cs`'s own existing logic — no change needed there).

## 25-26. JIT context propagation design

**Selected: (A) — the isolated method parameters FREQ.6D.19 already added to `ContinueJitCompositionAsync`** (`targetFinishTimeSeconds`, `targetFinishTimeSource`, `recentRace`), populated for real by `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync` from its already-loaded `planRow` (§7, §17). This is preferred over (C) extending an existing immutable plan/JIT context object, because no such single context object currently exists that both `LongHorizonRollingWindowActivationService` and `ContinueJitCompositionAsync` already share for this purpose — introducing one merely to hold two nullable scalars plus a nullable object would be a larger surface change than reusing the parameters this repository already added last phase specifically for this exact purpose. Two plain parameters populated from one already-loaded row is the minimal-diff design consistent with how every other plan-derived value (`aggregate.DaysPerWeek`, `aggregate.GoalType`) already flows into this exact call chain today.

## 27. Fail-closed semantics

No change needed to the exception type: `CatalogGoalPacePrescriptionUnsupportedException` (already raised by `CatalogSessionPrescriptionPlanner.cs:256/261`) is already the correct, existing typed failure for "TargetFinishTime exists but Source unavailable and a GOAL_PACE session requires it" — this decision's implementation phase must verify this exception is what `LongHorizonRollingJitCompositionOrchestrator`'s existing `MapCompositionFailure`/`catch` handling already converts into a typed, non-500 `LongHorizonReasonCode` (it already does, per FREQ.6D.19's own tracing: `CoreGeneration`/`AllocationPolicy` stage failures map to `CoreJitContextUnavailable`, surfaced publicly as `PlanTransitionUnavailable` — never a raw 500). No new exception type is needed.

## 28-30. No effort-based fallback, no new target time, no latest-product-average lookup

All three explicitly rejected, consistent with this phase's own instructions and with §10-11 above: `GOAL_PACE_TEN_K` catalog/prescription semantics are frozen and untouched; restart never substitutes a new `TargetFinishTimeSeconds`; restart never re-queries `CanonicalTargetFinishTimePolicy`'s current-release value for a historical plan — only the persisted value from confirmation time is ever used.

## 31. Plan reproducibility

**Frozen.** `TargetFinishTimeSource`, once persisted at confirmation and read back verbatim at every restart (§10), is exactly what closes the gap in the invariant "same confirmed plan + same persisted evidence/provenance + same immutable catalog/bundle authority + same completion/repair history → same later Core prescription after restart" — before this decision, the provenance half of that evidence was silently absent, making the invariant unprovable for any plan whose Core progression reaches `GOAL_PACE_TEN_K`. This decision does not itself implement the invariant; it defines the exact missing input needed to satisfy it.

## 32-34. Cross-horizon, cross-frequency, cross-distance neutrality

- **Cross-horizon (Core-only 8-14 / Runway 15-20 / LongHorizon 21-52)**: same authority, same `TrainingPlan` column, same confirm-time write boundary, same read pattern — no horizon-specific persistence semantics (§14).
- **Cross-frequency (3D/4D/5D/future 6D/7D)**: `TargetFinishTimeSource` has no day-count dependency anywhere in its definition, request shape, or resolution logic — frequency-neutral by construction, no new field variant needed.
- **Cross-distance (future HM/Marathon)**: `CanonicalTargetFinishTimePolicy` already defines canonical values for `FiveK`/`TenK`/`HalfMarathon`/`Marathon` today, and `TargetFinishTimeSource`'s own doc comment already speaks of "the selected goal distance" generically — the enum and its persistence design generalize without change. This phase does not implement or scope any HM/Marathon work; it only confirms the concept does not need TEN_K-specific plumbing.

## 35. Security / audit / explainability

Persisting `TargetFinishTimeSource` on the confirmed plan directly answers "did the user supply this goal, or did the product?" for any future audit/debug/replay need, using the plan's own existing provenance-field convention (alongside `TemplateId`, `CandidateKey`/`CandidateVersion`-equivalent fields already on `TrainingPlan`) — no new analytics field is proposed; this is the same kind of provenance the entity already tracks for catalog identity, applied to goal evidence.

## 36-39. Candidate models A-D — see §7 (comparison table) and §6 (owner selection). Model A selected; B, C rejected; D's *concept* (atomic pair) is adopted, implemented as two sibling scalar columns rather than a new wrapper object, per this repository's own established convention for exactly this shape of invariant.

## 40. TARGET_FINISH_TIME_SOURCE_PERSISTENCE_DECISION_MATRIX

| Model | Semantic ownership | Determinism | Historical compat. | Schema cost | Duplication | Core/Runway reuse | LongHorizon suitability | Future-frequency reuse | Future-distance reuse | Selected? |
|---|---|---|---|---|---|---|---|---|---|---|
| Plan-level source (A) | Exact | High | Trivial (nullable) | 1 column | None | Yes | Yes | Yes | Yes | **YES** |
| Rolling-state source (B) | Wrong (doesn't vary per window) | Lower (extra duplication surface) | Same nullability question, narrower scope | 1 column, LongHorizon-only table | Full duplication | No (Core-only/Runway have no rolling state) | Yes but unnecessarily narrow | Yes but narrow | Yes but narrow | No |
| Derive on restart (C) | N/A | **Fails** (§4 ambiguity) | N/A | None | None | N/A | N/A | N/A | N/A | No |
| Complete evidence object (D) | Correct concept, redundant implementation | Same as A | Same as A | Same as A (still 1 column) plus a new wrapper type | None beyond A | Same as A | Same as A | Same as A | Same as A | Concept adopted; object rejected in favor of plain sibling columns |

## 41. TARGET_FINISH_TIME_SOURCE_LEGACY_COMPATIBILITY_TABLE

| Row | TargetFinishTime | Source | Persisted? | Restart behavior | GOAL_PACE allowed? | Typed failure? | Backfill? |
|---|---|---|---|---|---|---|---|
| New `product_average` plan | Present | `ProductAverage` | Both, from confirm | Read verbatim, projected into JIT | Yes (`CHALLENGING`) | N/A | N/A |
| New user-provided plan | Present | `UserDefined` | Both, from confirm | Read verbatim, projected into JIT | Only with independent recent-race evidence (unchanged existing rule) | N/A | N/A |
| Other supported new source | — (only two values exist, §3) | — | — | — | — | — | — |
| Historical plan, recoverable source | N/A — §20 confirms none are recoverable | — | — | — | — | — | — |
| Historical plan, unrecoverable source | Present | `null` (`UNKNOWN_LEGACY`) | Seconds only | Read seconds verbatim; source projected as null | No — fails exactly as it does today | Yes — `CatalogGoalPacePrescriptionUnsupportedException` → `CoreJitContextUnavailable` → `PlanTransitionUnavailable` (unchanged) | **No** |
| Plan with no target time | `null` | `null` | Both null | Read as both null | Governed by pre-existing `NOT_REQUESTED`/`UNSUPPORTED` path, unchanged | N/A | N/A |

## 42. Required final authority (frozen)

- **Canonical owner**: `TrainingPlan` (plan-level immutable evidence).
- **Persist `TargetFinishTimeSource`?** YES.
- **Persist where?** New nullable string column `TargetFinishTimeSource` on `TrainingPlans`, alongside the existing `TargetFinishTimeSeconds`.
- **Atomic with `TargetFinishTimeSeconds`?** YES for every new confirmation (both-null-or-both-present); historical rows are the sole, explicit, permanent exception (`UNKNOWN_LEGACY`).
- **New-plan requirement**: mandatory when a target time is requested; captured verbatim from `snapshot.NormalizedInput.TargetFinishTimeSource` at the existing confirmation write boundary.
- **Historical null behavior**: `UNKNOWN_LEGACY`, permanent, never backfilled.
- **Backfill**: none, ever.
- **Restart resolution**: read the persisted column verbatim via the plan row already loaded by `LongHorizonRollingWindowActivationService`; never re-derive, never re-query current product-average, never infer from seconds.
- **JIT propagation**: via `ContinueJitCompositionAsync`'s existing (FREQ.6D.19-added) `targetFinishTimeSeconds`/`targetFinishTimeSource` parameters, populated for real in production for the first time.
- **Failure behavior**: unchanged existing typed exception chain (`CatalogGoalPacePrescriptionUnsupportedException` → `CoreJitContextUnavailable` → `PlanTransitionUnavailable`), for both historical `UNKNOWN_LEGACY` rows and any other genuinely-unresolvable case.

## 43. Schema decision

**`SCHEMA_CHANGE_APPROVED`** — exactly one new nullable column, `TrainingPlans.TargetFinishTimeSource` (string, storing the enum's name, matching this table's own existing enum-as-string convention), no index, no DB-level constraint. Design specified in full at §17; not implemented in this phase.

## 44. Implementation contract for the next phase

1. Add the `TargetFinishTimeSource` column to `TrainingPlan` + one EF migration (nullable string).
2. Populate it at both confirmation write boundaries (`CatalogPlanConfirmationService.BuildCatalogTrainingPlan`, `LongHorizonPublicPlanService`'s equivalent) from the already-available `snapshot.NormalizedInput.TargetFinishTimeSource` — no new upstream plumbing needed.
3. Verify new confirmations persist both-null-or-both-present via a real-Postgres test.
4. Reload through a fresh PostgreSQL `DbContext` and confirm the value survives.
5. Thread it from `LongHorizonRollingWindowActivationService`'s already-loaded `planRow` into `ContinueJitCompositionAsync`'s existing parameters.
6. Verify `GOAL_PACE_TEN_K` resolves correctly after a real restart for a freshly-confirmed 5D plan with each of `ProductAverage`/`UserDefined` (plus recent-race evidence where required).
7. Verify historical (pre-migration) 4D LongHorizon plans restart unchanged — `UNKNOWN_LEGACY`, same existing typed-failure behavior if they ever reach `GOAL_PACE_TEN_K`, otherwise fully unaffected.
8. Re-run FREQ.6D.19's own organic 5D GE→Runway→Core test suite, now with `TargetFinishTimeSource` sourced from real persisted plan state instead of the dark-verification-only test convention.
9. Re-run the secondary-KEY repair regression (FREQ.6D.19).
10. Re-run the full 21-52 dark regression matrix.
11. If all succeed: close `INTERMEDIATE_5D_LONGHORIZON_IMPLEMENTED_AND_DARK_VERIFIED`.

Public routing/activation is explicitly **not** part of that implementation phase unless a future roadmap decision deliberately combines them.

## 45. Decision standard applied

Every freeze in §42 rests on: (a) a **direct canonical rule** already governing this codebase's provenance semantics (PHASE4D_4_1's own governance decision that `ProductAverage`/`UserDefined` must be distinguished, never inferred); (b) **existing persistence semantics** already established for the identical both-null-or-both-present pairing shape (`CatalogPrescriptionProfileKey`/`Version`, FREQ.6D.13); and (c) a **strong deterministic-replay requirement** (§31's plan-reproducibility invariant, which cannot hold without this exact persisted fact). No design choice here was made merely to minimize code size.

## 46. Final classification

**`TARGET_FINISH_TIME_SOURCE_PLAN_LEVEL_PERSISTENCE_AUTHORITY_APPROVED`.**

## 47-49. No code / next phase / governance

No production code, tests, migration, or catalog content authored (verified — this phase only wrote this report and governance files). Next phase = the narrow implementation contract at §44 (persist + restart + JIT propagation + dark closure), not yet assigned an ID — recorded as `NEXT_PHASE_NOT_YET_SCHEDULED`. Only after that implementation phase succeeds may the final Intermediate×5D LongHorizon public-activation phase be scheduled; this decision does not skip ahead to it.
