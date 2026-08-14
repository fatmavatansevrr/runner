# Phase 4M.4B.1 — Next-Window Numeric Progression Policy Resolution
## Audit + Decision Support Only — No Production Code Changed

## 1. Files inspected
`LongHorizonRollingWindowActivationService.cs`, `LongHorizonRollingJitCompositionOrchestrator.cs`, `LongHorizonRollingJitCompositionContracts.cs`, `LongHorizonValidatedLoadContracts.cs`, `LongHorizonCheckpointEvidenceContracts.cs`, `LongHorizonCheckpointEvidenceAggregator.cs`, `LongHorizonRollingCoreGenerationInputAdapter.cs`, `PreparationRunwayCoreWeekOneTargetAdapter.cs`, `CatalogVolumeAndLongRunPlanner.cs`, `CatalogVolumeContracts.cs`, `CatalogPeakVolumeBandLoader.cs`, `CatalogSessionPrescriptionPlanner.cs`, `FourDaySessionDistanceAllocationPolicy.cs`/`V1FourDaySessionVolumeAllocationPolicy.cs`, `ProgressionStageAllocator.cs`, `ProgressionStageScheduleContracts.cs`, `CatalogWorkoutBinder.cs`, `V1CatalogPilotIdentityPolicy.cs`, `LongHorizonGeNumericExecutor.cs`, `LongHorizonStructuralValidator.cs`, `plan-catalog/catalog/combinations/ten-k-4d-intermediate.v10.json`, `plan-catalog/catalog/level-modifiers/intermediate-modifier.v6.json`, `LongHorizonExplicitNextWindowActivationTests.cs`, `LongHorizonRollingJitCompositionOrchestratorTests.cs`.

## 2. Real next-window generation pipeline
`ActivateNextWindowAsync` → (Runway/Core JIT path, the pilot's common case) `LongHorizonRollingRestartContinuationService.ContinueJitCompositionAsync` → `LongHorizonRollingJitCompositionOrchestrator.ComposeAndActivateNextWindowAsync`, which:
1. Maps `ValidatedSustainableLoad` → a `RecentWeeklyVolumeKm`/`RecentLongestRunKm`-shaped legacy generation-request input (`LongHorizonRollingCoreGenerationInputAdapter.Build`).
2. Runs the **real, unmodified, production onboarding generator** (`TenKPreparationRunwayDarkOrchestratorFactory...OrchestrateAsync`) with that input — the exact same code path used to generate a brand-new plan.
3. Extracts Core Week-1's numeric target from that real composition (`PreparationRunwayCoreWeekOneTargetAdapter.FromAuthoritativeCoreBehavior`).
4. Pulls per-session `DistanceKm`/`WorkoutKey`/`WorkoutVersion` straight from the generator's `FinalPrescribedPlan`/`DatedCoreWeeks`.

## 3. Current progression authority
`CURRENT_NEXT_WINDOW_PROGRESSION_AUTHORITY = LongHorizonCheckpointEvidenceAggregator.Aggregate (a mean-of-actual-completed-km evidence scalar, "ValidatedSustainableLoad") fed as the starting-volume anchor into the same deterministic catalog progression curve (CatalogVolumeAndLongRunPlanner's linear interpolation to a catalog-clamped peak, plus ProgressionStageAllocator/CatalogWorkoutBinder for workout-content sequencing) that a brand-new plan preview uses.`

This is model **F (combination)**: an evidence-derived anchor (closest to model E — recomputed from evidence, specifically the mean of `TrainingDay.ActualDistanceKm` for `Completed` rows, never planned/prescribed distances) feeding a deterministic continuous catalog curve (model A, clamped by one static band, model B-adjacent). `ActivationContextVersionSequence` is confirmed **not** load-numeric — a pure concurrency/version token.

## 4. ProgressAsPlanned numeric trace
`NextWindowLoadDecisionPolicy`'s output is **never read by composition today** — confirmed by the activation service's own code comment (`LongHorizonRollingWindowActivationService.cs`, inserted in Phase 4M.4A): *"This value is computed and carried to the response only; it does not currently influence composition/materialization below."* Therefore, **every** activation today — regardless of what symbolic decision 4M.4A computes — numerically behaves exactly as "ProgressAsPlanned" always has: the evidence aggregator recomputes `ValidatedSustainableLoad` fresh from the just-completed window's actual distances, and the same deterministic catalog curve runs unconditionally.

`PROGRESS_AS_PLANNED_CURRENT_NUMERIC_SEMANTICS`: Window N+1's volume anchor = mean of Window N's actually-completed weekly distances (gated on ≥1 usable week and ≥1 completed long run); everything downstream (peak target, weekly interpolation, long-run share, role split, workout-stage sequencing, workout-key binding) is the unmodified onboarding-style deterministic catalog pipeline applied to that anchor. No day-by-day adherence pattern (which specific sessions were missed) enters the numeric computation — only the aggregate mean of what was actually completed.

## 5. Progression state model
Predominantly **model E → A**: an evidence-recomputed scalar anchor (E) feeding a continuous deterministic interpolation curve (A), clamped by exactly one static catalog band (a weak B element). **No discrete tier/stage/band-index progression state exists that increments window-over-window.** `ProgressionStageAllocator` sequences workout *content* variants, never load.

## 6. Chronology vs. load progression separation
**Confirmed separated.** Window-index/`LifecycleState` advancement (chronology/phase) and `ValidatedSustainableLoad` computation (load) are architecturally independent — nothing in `ActivateNextWindowAsync` couples them; window advancement always proceeds whenever activation succeeds, using whatever load value results. This means a future Maintain/Reduce policy **can** hold only the load anchor while letting chronology/phase advance normally through the unmodified composition call — no architectural conflict.

## 7. Maintain representability
`MAINTAIN_HAS_EXISTING_DETERMINISTIC_REPRESENTATION` (qualified — see §16 for the exact caveat).

The exact existing substrate: `ValidatedSustainableLoad.WeeklyLoadSource`/`LongRunSource` already has a formally-typed, validator-enforced alternate authority source — `LongHorizonEvidenceSource.PriorValidatedCheckpointLoad` — distinct from `CompletedTrainingHistory`. The activation service already has a working helper, `PriorAnchor(state)`, that builds a `LongHorizonPriorValidatedAnchor` from the dark state's `LatestValidatedLoad` field and threads it into `LongHorizonRollingCheckpointRequest.PriorValidatedAnchor` — currently used only for the retry-continuation scenario, never for a Maintain decision. This is precisely the typed "carry forward the last validated load instead of recomputing" mechanism Maintain needs; it is real, already tested (retry-continuation path), and not invented for this audit.
1. Field/state: `state.LatestValidatedLoad` (dark-state) → `LongHorizonPriorValidatedAnchor` → `ValidatedSustainableLoad{ WeeklyLoadSource/LongRunSource = PriorValidatedCheckpointLoad }`.
2. Chronology advances: yes, unaffected (§6).
3. Phase/segment advances: yes, unaffected.
4. Catalog workout selection remains authoritative: yes — nothing downstream of the anchor changes.
5. Exact workouts duplicated: no — the catalog curve still runs at whatever anchor it's given; only the *anchor* is old, not the session content.
6. Only load frozen, not content: correct — content (workout keys/versions/roles) is never touched.
7. Replay deterministic: yes, if `LatestValidatedLoad` is read-only during replay (same immutability argument as 4M.4A's own decision recomputation).
8. Repeated Maintain causes drift: **not if** the policy explicitly does *not* overwrite `LatestValidatedLoad` while holding — see §11/§16.
9. New persisted state required: none beyond confirming `LatestValidatedLoad`'s existing update semantics (§16).
10. Evidence level: `EXISTING_DETERMINISTIC_CATALOG_BEHAVIOR` (the mechanism is real and already used for a structurally analogous purpose).

## 8. Reduce-modifiable dimensions

| Dimension | Classification | Reason |
|---|---|---|
| Weekly total distance | `NO_APPROVED_REDUCTION_RULE` | Correct dimension for a future Reduce policy to constrain, but no percentage/formula/lower-band is approved today. |
| Weekly total duration | `NO_APPROVED_REDUCTION_RULE` | Derived from distance/pace, not an independent authority. |
| EASY session distance | `NO_APPROVED_REDUCTION_RULE` | Derived from the weekly-total role split; inherits from weekly total. |
| LONG_RUN distance | `NO_APPROVED_REDUCTION_RULE` | Has its own existing share/cap formula (`CatalogVolumeAndLongRunPlanner`), but no *adaptation-driven* reduction on top of it is approved. |
| KEY total distance | `NO_APPROVED_REDUCTION_RULE` | Same as EASY — allocated from the weekly total. |
| KEY intensity | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Workout-stage/catalog authority (`ProgressionStageAllocator`/`CatalogWorkoutBinder`), not a volume dimension. |
| Workout type | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Catalog authority. |
| Workout version | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Catalog authority (`CatalogWorkoutBinder`). |
| Interval count | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Workout-content authority. |
| Segment structure | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Workout-content authority. |
| Pace prescription | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Workout-content authority. |
| Role distribution (day pattern) | `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` | Structural/PreferredDays authority, unrelated to load. |

**Conclusion:** the only dimension Adaptation may legitimately constrain, under the "Catalog = workout authority, Adaptation = modifier/constraint authority" rule, is the scalar load anchor (`ValidatedSustainableLoad.WeeklyVolumeKm`/`LongRunKm`) fed into the existing deterministic catalog pipeline — nothing further downstream.

## 9. Forbidden adaptation dimensions
All of §8's `MUST_NOT_BE_MODIFIED_BY_ADAPTATION` rows: KEY intensity, workout type/version, interval count, segment structure, pace prescription, role distribution. Touching any of these would make Adaptation a second workout generator — explicitly rejected by the canonical architecture rule.

## 10. Current catalog capacity
Confirmed: `V1CatalogPilotIdentityPolicy` pins production to exactly one candidate (`TEN_K__4D__INTERMEDIATE` v10). The catalog JSON resolves to exactly one master template/layout/level-modifier/rule-pack — no branching/tier selector field. `CatalogPeakVolumeBandLoader` returns exactly one min/max band per `(distanceFamily, experience, runsPerWeek)` key — a single static clamp, not a set of selectable tiers. **There is exactly one deterministic numeric prescription per (week number, role) for TEN_K/4D/INTERMEDIATE.** No "held" or "reduced" alternative variant exists at a fixed calendar-week position, for GE, Runway, JIT, or any role. A Reduce policy therefore cannot use a catalog-supported lower alternative (R1/R3, §17) — it does not exist.

## 11. Last-successful-baseline analysis
1. Persisted per-activated-window progression state: `LongHorizonFullDarkLifecycleState.LatestValidatedLoad` (a single most-recent `ValidatedSustainableLoad`, not a per-window history array).
2. Exact progression state per historical window: **not individually retrievable** — only the single "latest" value is carried in dark state; no per-window snapshot exists (this is exactly the gap the existing `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` backlog already flags from the decision side, and it applies equally to the load-anchor side).
3. Is "previous window" always "last successful baseline"? **No** — under a Maintain policy, "previous window" and "the anchor that generated it" would deliberately diverge (the previous window's own anchor was itself possibly already a held-over value).
4. If previous window was itself Maintain/Reduce: the *rule I recommend* (§16) is that Maintain always references "the load anchor last **actually validated fresh from evidence** or explicitly reduced" — i.e., held state does not itself become a new baseline.
5. Progress→Maintain→Maintain: both Maintains reference the SAME anchor (the one computed after the first Progress), under the "don't overwrite `LatestValidatedLoad` while holding" rule.
6. Progress→Reduce→Maintain: Maintain references whatever the Reduce policy actually set as the applied anchor for its window (once Reduce is resolved) — Maintain's rule is generic ("hold the last *applied* anchor"), not specific to Progress-only.
7. Progress→Reduce→Progress: a fresh evidence recomputation on the third window naturally supersedes the reduced anchor — no accidental double-progression risk *if* Progress always recomputes fresh from actual evidence (current, unchanged behavior) rather than building on top of the held/reduced value.
8. Sufficient existing lineage/version data: **partially** — `LatestValidatedLoad` gives a single current value; it is sufeficient for a "hold most recent applied anchor" Maintain rule, but **not** sufficient for historical "why did window X get Maintain/Reduce" auditing (already the subject of the recorded `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` backlog). Not implemented in this phase.

## 12. Repeated-decision sequence analysis

| Sequence | Entering state | Chronology/phase | Resulting anchor | Must persist | Deterministic replay? | Drift risk |
|---|---|---|---|---|---|---|
| A. Progress→Progress | fresh evidence each time | advances normally | fresh mean-of-completed each time | nothing new | Yes (current, unchanged behavior) | None |
| B. Progress→Maintain→Progress | window 2 holds window-1's anchor | advances normally throughout | window 2 = window-1 anchor; window 3 = fresh evidence from window 2's actuals | `LatestValidatedLoad` must NOT be overwritten during the Maintain window | Yes, if hold-rule is followed | None if hold-rule enforced |
| C. Progress→Maintain→Maintain→Progress | windows 2 & 3 both hold window-1's anchor | advances normally | window 4 = fresh evidence from window 3's actuals | same as B, across two consecutive holds | Yes, if hold-rule enforced consistently | **Risk if `LatestValidatedLoad` is naively overwritten on the second Maintain with "the same value recomputed" — must genuinely skip the aggregator call, not just recompute-and-happen-to-match** |
| D. Progress→Reduce→Progress | window 2 = reduced anchor (mechanism undefined, §17) | advances normally | window 3 = fresh evidence from window 2's actuals (already-reduced target, naturally lower) | Reduce's applied value must persist as `LatestValidatedLoad` for D's own replay | Only once Reduce is resolved | None, if Progress always recomputes fresh (current behavior) |
| E. Progress→Reduce→Maintain→Progress | window 3 holds window-2's *reduced* applied anchor | advances normally | window 4 = fresh evidence from window 3 | Maintain's hold-rule must reference the Reduce-applied value, not a re-derived one | Only once Reduce resolved | Same drift risk as C if hold-rule not strictly "skip recomputation" |
| F. Progress→Reduce→Reduce→Maintain→Progress | window 4 holds window-3's twice-reduced anchor | advances normally | window 5 = fresh evidence from window 4 | requires Reduce to define its OWN repeated-Reduce semantics (compounding vs. floor) — **entirely undefined today** | No — Reduce mechanism itself is undefined | High — compounding-Reduce semantics is exactly the kind of unapproved numeric rule this audit must not invent |

Sequences involving Reduce (D/E/F) cannot be fully resolved until Reduce itself has an approved mechanism — reported as `DecisionRequired`, not solved by assumption.

## 13. Phase-boundary analysis
1. Chronology can advance while load is held: **yes** — confirmed architecturally separated (§6).
2. Phase can advance while load is held: **yes**, same reasoning; `ProgressionStageAllocator`'s stage sequencing is condition-driven, not load-driven, so a held load anchor does not block or misalign stage progression.
3. Reduce moving to a lower load without moving backward in phase: **yes, structurally possible** — since no discrete "lower state" exists (§10), any future Reduce mechanism would operate on the same scalar-anchor seam as Maintain, which is confirmed phase-independent.
4. Using a lower catalog state would select wrong-phase workouts: **not applicable** — no lower catalog state exists to select (§10); this risk is moot given R1/R3 are rejected.
5. Does the generator couple phase identity and progression load too tightly: **no** — confirmed decoupled (§6); the anchor is a pure scalar input, phase/stage assignment reads runtime conditions, not the anchor.

**No proposed Maintain/Reduce representation examined here breaks phase semantics.**

## 14. Workout-content authority analysis
Both the Maintain candidate (§7, hold the anchor) and any future Reduce candidate operating on the same scalar-anchor seam leave `WorkoutKey`, `WorkoutVersion`, role, segment structure, interval format, and pace/intensity semantics **entirely untouched** — all of that remains 100% catalog-authoritative, computed downstream of the anchor by the unmodified `CatalogVolumeAndLongRunPlanner`/`ProgressionStageAllocator`/`CatalogWorkoutBinder` chain. This matches the required model exactly: *Catalog produces a valid phase-appropriate next window + Adaptation constrains the allowed progression state/load.* No candidate examined violates this.

## 15. Evidence/governance findings

| Item | Finding | Classification |
|---|---|---|
| "deload" | Explicit code comments state **no recurring deload rule exists** in Core/Runway (only a one-time taper); `IsRecoveryOrDeloadWeek` always `false` there. | `HISTORICAL_NON_CANONICAL` (explicitly absent, not a note about a future feature) |
| "recovery week" | Real, implemented — **GE segment only** (`LongHorizonGeNumericExecutor`, fixed `RecoveryVolumeRatio`≈0.85, structural placement validated, excluded from evidence mean). | `EXISTING_DETERMINISTIC_CATALOG_BEHAVIOR` (GE-scoped) |
| `PriorValidatedCheckpointLoad` / `PriorValidatedAnchor` | Real, implemented, typed, validator-enforced; currently used for retry-continuation only. | `EXISTING_DETERMINISTIC_CATALOG_BEHAVIOR` |
| "volume rollback", "progression band", "hold load", "freeze progression", "downshift", "last successful", "baseline load" | Zero hits anywhere in code or docs. | `INSUFFICIENT` |
| Approved numeric percentage for Reduce | None found anywhere. | `INSUFFICIENT` |

No canonical governance `.md` artifact defines Maintain/Reduce numerically anywhere in the repo.

## 16. Maintain options

**M1 — Hold last-applied `ValidatedSustainableLoad` via existing `PriorValidatedCheckpointLoad` authority (RECOMMENDED).**
- Progression-state behavior: skip the evidence-aggregator recomputation for this checkpoint; supply `PriorAnchor(state)` (already-existing helper) as `ValidatedLoad` instead.
- Chronology: advances normally (unaffected).
- Phase: advances normally (unaffected).
- Workout behavior: entirely catalog-authoritative, unaffected (§14).
- Numeric behavior: weekly volume/long-run stay at whatever the last *actually validated* checkpoint produced.
- Persistence need: **none new** — reuses `LatestValidatedLoad`; only the *update rule* needs to explicitly skip overwriting it during a held window (a code-behavior decision for 4M.4B.2, not a schema change).
- Replay behavior: deterministic, provided the skip-overwrite rule is followed consistently (§12).
- Repeated-Maintain behavior: safe under the skip-overwrite rule; **unsafe if implemented as "recompute and happen to match"** instead of a true skip — flagged explicitly as an implementation-correctness requirement for 4M.4B.2, not resolved here.
- Evidence level: `EXISTING_DETERMINISTIC_CATALOG_BEHAVIOR` + `STRONG_DOMAIN_EVIDENCE` (structurally identical, tested mechanism reused for a new trigger).
- Risk: low — the only residual risk is a precise "skip, don't recompute" implementation discipline in 4M.4B.2.

**M2 — Duplicate exact prior session set into the new window.** Rejected: not canonical catalog behavior (the catalog always regenerates content per its own deterministic curve; duplicating specific session rows across windows has no existing precedent and would require inventing new materialization logic).

## 17. Reduce options

**R1 — Discrete lower progression state.** `NO_APPROVED_REDUCTION_RULE` / **rejected**: confirmed no catalog-supported lower tier/band/stage exists at any fixed week position (§10).

**R2 — Previous successful/validated load tier (reused for downward constraint).** Distinguish from Maintain: simply reusing the prior anchor *holds*, it does not constrain *downward*. To make R2 a genuine Reduce, a rule for "how much below the prior anchor" would be required — which is exactly the missing numeric translation this audit must not invent. **Rejected as a complete Reduce policy**; it degenerates into Maintain without an approved downward delta.

**R3 — Bounded catalog-supported lower band.** `NO_APPROVED_REDUCTION_RULE` / **rejected**: only one static peak-volume band exists per candidate (§10); no lower alternate band to bound toward.

**R4 — Percentage-based load reduction.** Explicitly excluded per instruction unless evidence supports it. **No evidence found anywhere** in code, catalog data, or documentation for any specific percentage, band, or formula. **Rejected — remains `DecisionRequired`.**

**Conclusion: no Reduce option can be recommended from existing repository evidence.** This is not a gap in the audit — it is the audited-and-confirmed absence of any implemented or documented numeric downward-constraint mechanism.

## 18. Rejected options
M2 (session duplication) — no precedent, would invent new materialization logic.
R1 (discrete lower state) — does not exist in the catalog.
R2 (reused prior anchor as "Reduce") — degenerates into Maintain without an approved delta.
R3 (bounded lower catalog band) — does not exist.
R4 (percentage reduction) — zero supporting evidence anywhere in the repository.

## 19. Recommended policy
**Maintain: M1** (hold last-applied validated load via the existing `PriorValidatedCheckpointLoad`/`PriorAnchor` mechanism), recommended with the explicit implementation caveat in §16/§12 (skip-overwrite discipline for repeated Maintain, to be verified against `LatestValidatedLoad`'s exact write-site before 4M.4B.2 implementation).
**Reduce: none recommendable.** Remains `DecisionRequired` — no existing mechanism, no approved numeric rule, no supporting governance evidence of any kind.

## 20. Persistence requirements
No new persistence needed for Maintain (M1) beyond confirming/adjusting the existing `LatestValidatedLoad` update-site behavior during a held window — a code-behavior change, not a schema change.

For Reduce, **whichever policy is eventually approved** will need to record, at minimum (per the existing `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` backlog, extended): the applied progression-state/anchor actually used for the window (not just the symbolic decision), the source window it was derived/held/reduced from, and policy/version provenance — so that repeated-Reduce chains (§12, sequence F) can be replayed deterministically without recomputation ambiguity. **Not created in this phase.**

## 21. Future 4M.4B.2 seam (Maintain only — Reduce remains blocked)
1. **Exact input contract:** `NextWindowAdaptationResult` (already computed live by 4M.4A) — specifically `LoadDecision == Maintain`.
2. **Exact output contract:** a `ValidatedSustainableLoad` sourced from `PriorAnchor(state)` instead of `LongHorizonCheckpointEvidenceAggregator.Aggregate(...)`'s fresh computation, for this one checkpoint call.
3. **Component that should receive `LoadDecision`:** `LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`, at the exact point (already present from 4M.4A) where `nextWindowResult` is computed — before constructing `checkpointRequest`/the JIT composition request.
4. **Progression state to constrain:** `LongHorizonRollingCheckpointRequest.PriorValidatedAnchor` (GE path) / the equivalent `ValidatedLoad` parameter passed into `ContinueJitCompositionAsync` (JIT path) — conditionally supplied instead of letting the aggregator run.
5. **Activation context extension needed:** No new fields — reuses existing `LongHorizonPriorValidatedAnchor`/`LatestValidatedLoad`.
6. **Persistence extension needed:** Only the update-rule for `LatestValidatedLoad` during a held window (§16) — no schema change.
7. **Schema migration needed:** None for Maintain.
8. **Components that MUST remain unchanged:** `WindowExecutionSummaryBuilder`, `NextWindowLoadDecisionPolicy`, `ScheduleRepairPolicy`, `ScheduleRepairPersistenceService`, `CatalogVolumeAndLongRunPlanner`, `ProgressionStageAllocator`, `CatalogWorkoutBinder` — none of these should need to change to wire Maintain.
9. **Existing tests that should protect the seam:** `LongHorizonRollingJitCompositionOrchestratorTests.cs` (composition consistency), `LongHorizonExplicitNextWindowActivationTests.cs`/`LongHorizonFullLifecycleMatrixTests.cs` (activation regression), `WindowCheckpointSummaryAndDecisionTests.cs`/`LongHorizonNextWindowDecisionActivationTests.cs` (4M.4A decision correctness — must remain green and unmodified).
10. **Required new integration test scenarios for 4M.4B.2:** (a) Maintain window produces numerically identical `ValidatedSustainableLoad` to the prior checkpoint; (b) repeated Maintain→Maintain does not drift; (c) Maintain→Progress correctly resumes fresh evidence recomputation; (d) `LatestValidatedLoad` is confirmed not overwritten during a held window via fresh-DbContext verification.

Reduce has no seam to specify until a policy is approved.

## 22. Exact tests/commands
```
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonNextWindowDecisionActivationTests.FullyCompletedWindow" → 1/1 passed (real ProgressAsPlanned Window N→N+1 trace)
dotnet test RunningApp.IntegrationTests --filter "FullyQualifiedName~LongHorizonRollingJitCompositionOrchestratorTests|FullyQualifiedName~LongHorizonRollingJitActivationRuntimeTests" → 59/59 passed
dotnet build → 0 warnings, 0 errors
git diff --check → no real errors (pre-existing CRLF warnings only)
```
No temporary instrumentation was added; no production code was modified.

## 23. DecisionRequired items
1. **Reduce numeric mechanism** — no existing catalog-supported lower state, no approved percentage/formula, no governance evidence of any kind. Fully blocked pending a genuine product decision (not derivable from the repository).
2. **Repeated-Reduce compounding semantics** (§12, sequence F) — cannot be defined until item 1 is resolved.
3. **`LatestValidatedLoad` exact update-site confirmation** — needed before 4M.4B.2 implements Maintain's skip-overwrite rule, to guarantee no drift across repeated Maintain windows (§12, sequence C/E). Not a blocker to *recommending* M1, but a precondition to *implementing* it correctly.
4. **Durable per-window progression-state history** — `LatestValidatedLoad` only carries the single most-recent value; historical "why did window X get this anchor" auditing is not currently possible (ties into the existing `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` backlog, extended here to cover the load anchor too, not just the symbolic decision). Not implemented in this phase.

## 24. Final classification

```
MAINTAIN_POLICY_RESOLVED_REDUCE_POLICY_REMAINS_DECISION_REQUIRED
```

Maintain is resolved via **M1** (reuse the existing, already-implemented `PriorValidatedCheckpointLoad`/`PriorAnchor` mechanism to hold the load anchor while chronology/phase advance normally and all workout content stays catalog-authoritative), backed by `EXISTING_DETERMINISTIC_CATALOG_BEHAVIOR` + `STRONG_DOMAIN_EVIDENCE`, with one named implementation-correctness precondition for 4M.4B.2 (§23 item 3). Reduce remains fully `DecisionRequired` — no existing mechanism, no approved numeric rule, and no repository evidence of any kind supports a specific recommendation; inventing one was correctly avoided per instruction.

This is an audit/decision-support classification only. Runtime implementation is not complete and Phase 4M.4B.2 has not been started.

No code committed, no push, Phase 4M.4B.2 not started, Phase 4M.5 not started.
