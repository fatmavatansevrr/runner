# Phase 10K-GEN.5C — Beginner 3D Core: Full Non-Support Closure

## 0. Correction to the phase premise — checked, not assumed

This phase's prompt refers to "GEN.5B" as an already-completed prior phase with a narrower, explicit-zero-only typed non-support pattern to extend. **No such phase exists.** Checked both places it would have to exist:

- No `PHASE_10K_GEN_5B_*.md` (or any `5B`-named) document anywhere in the repository.
- No Beginner×3D-related exception type, reason code, or reference anywhere in `backend/` C# source (`grep` for `BEGINNER_THREE_DAY`, `BeginnerThreeDay`, `Beginner.*3D` returns zero matches outside this document).

The real prior work is: GEN.5 (found explicit-zero universally ineligible, missing-readiness ineligible at 8-11wk, using an *unclamped* mechanistic reachable-growth computation later identified as not real peak evidence), GEN.5A (evidence envelope, unresolved tension), GEN.5A.1 (quantified the tension, still unresolved), GEN.5A.2 (resolved it with real cited evidence: band = 16.0-20.0km, reference 17.0km; **all three readiness states — missing, explicit-zero, and positive-observed — come back universally `PRODUCT_INELIGIBLE` across 8-14 weeks**). This phase proceeds directly from GEN.5A.2's real, verified findings, not from a fabricated "GEN.5B." No content is invented to paper over the discrepancy.

## 1. Peak band — frozen as canonical governance value

```
Beginner × 3D ResolvedPeakBand:
  MinimumKm: 16.0
  MaximumKm: 20.0
  Reference: 17.0
  Provenance: PRODUCT_DEFAULT_WITH_EVIDENCE_ENVELOPE
  Sources: Hal Higdon Novice 10K (real peak 16.90km, week 7), McMillan 10K Level 1
    Beginner 3 runs/week (16.1-32.2km range, low end corroborates)
```

Frozen now, independent of the eligibility outcome below — per instruction, this value does not need to wait for how the Core-support question resolves, since it may be needed for a future Runway/LongHorizon investigation of this same cell (not conducted here).

**No code implements this value.** Consistent with every phase since GEN.5, this remains a documentation-only governance freeze — writing a live `VolumeSafetyPolicy` instance or catalog artifact for a cell being formally closed as non-representable would be dead, misleading production surface (implying a launch path that doesn't exist), not a defensible parallel to GEN.4C.4's freeze (which fed a real, launching implementation).

## 2. Full non-support — extended from explicit-zero-only to all readiness states

GEN.5A.2 §3 computed all three readiness states against the frozen band and found:

| Readiness state | Result across 8-14wk |
|---|---|
| Missing-readiness (start 12.0km) | `PRODUCT_INELIGIBLE`, all 7 horizons |
| Positive-observed (18.0km, near top of Beginner's habitual range) | `PRODUCT_INELIGIBLE`, all 7 horizons |
| Explicit-zero (start 9.5km) | `PRODUCT_INELIGIBLE`, all 7 horizons (already found in GEN.5 §B, confirmed unaffected by the band per GEN.5A.1 §4's monotonicity argument) |

GEN.5A.2 §4's exact structural derivation (pre-taper volume must reach ≈22.2km to clear the 12.0km÷0.53 taper floor; this band's 20.0km ceiling never gets there, for any starting point, at any horizon) confirms this is not three coincidentally-failing tables — **it is a single, unconditional, mathematically-guaranteed non-representability for the entire cell**, independent of readiness state.

## 3. Mechanism determination — no new typed exception needed; the existing identity-level gate already, correctly, fully covers this

Checked the real, current production code (`V1CatalogPilotIdentityPolicy.cs`, GEN.4E): the public identity allow-list is exactly `{(Intermediate,3), (Intermediate,4), (Beginner,4)}`. **`(Beginner, 3)` was never admitted** — `IsSupportedIdentity` already returns `false` for it today, in production code that already exists and is already tested (`Gen4EBeginnerFourDayPublicActivationTests.WrongCombination_NeverNearestMatches("beginner", 3)`, real HTTP test, currently passing).

This is the **correct** mechanism for this finding, and the `CatalogProductIneligibleException`/`CatalogPreviewGenerator`-level typed-ineligibility pattern (GEN.4D.2, used for Beginner×4D's explicit-zero-at-8-12wk case) is the **wrong** one to extend here — not because it's technically incompatible, but because it answers a different question:

- **`CatalogProductIneligibleException`** exists for identities that *are* publicly supported but turn out numerically infeasible for a *specific* horizon/readiness combination (Beginner×4D explicit-zero at 8-12wk specifically; 13-14wk of the same identity *is* eligible). It correctly implies "this identity generally works, this particular request doesn't."
- **Beginner×3D has no readiness state and no horizon at which it is ever eligible** (§2). Admitting it to the identity allow-list and then having the volume planner throw `CatalogProductIneligibleException` on every single real request would be actively misleading (implying conditional eligibility that categorically never exists) and would require reopening the GEN.4E containment decision for no benefit.

**Determination: no new typed exception is invented or needed. The existing identity-allow-list rejection is the correct, sufficient, already-implemented, already-tested non-support mechanism.** Nothing changes in code as a result of this phase.

## 4. User-facing routing — disclosed, not overclaimed

Confirmed by GEN.4E's real HTTP test: a `(beginner, 3)` request never returns `200` and never returns `500`. **The exact status code and message path it does take through the Legacy/non-catalog fallback route was not traced in this phase** — `V1LiveCatalogPilotRoutingPolicy.Evaluate` returns `Route = Legacy, Reason = NotPilotRequest` for any identity outside the allow-list, and `LivePlanPreviewRoutingService` maps `Legacy` to `GenerationSource.LegacySql`, i.e. it falls through to the pre-catalog SQL generation path rather than surfacing a clean "not supported, try Beginner×4D instead" message. **Whether that legacy path itself then succeeds, fails, or produces a materially different (untested-by-this-catalog-work) plan for a Beginner×3D request is not characterized here.** This is disclosed as an open question, not resolved — a genuine product/UX gap this finding surfaces (users requesting Beginner×3D today get *something* via the legacy fallback, not a clear redirect to the fully-eligible Beginner×4D or Intermediate×3D alternatives), but tracing or fixing the legacy fallback path is out of scope for this phase.

**Explicitly not claimed**: that Runway (15-20wk) or LongHorizon (21+wk) are viable for Beginner×3D. Neither was investigated in this phase or any prior GEN.5 phase — Core (8-14wk) is the only range examined. The frozen band (§1) may make a future Runway/LongHorizon investigation of this cell possible, but that investigation has not happened.

## 5. Cross-training caveat — real, disclosed, not resolved

A genuine methodological caveat in GEN.5A.2's own evidence use, surfaced while writing this closure: Hal Higdon's real Novice 10K source program is not "3 running days with plain rest otherwise" — it prescribes 2-3 cross-training days (Monday/Wednesday cross-training + Saturday longer cross-training run) alongside its 3 running days. The 16.90km peak figure GEN.5A.2 used is running-volume-only from a program whose original total weekly training stimulus included cross-training this system does not model at all — `CrossTrainDay` was explicitly confirmed out-of-scope for V1 in GEN.1 §10 ("2D, Expert, CrossTrainDay, and DoubleSessionDay were NOT investigated as V1 feasibility targets"). This does not invalidate the running-volume figure itself (it's still real, still an accurate peak *running* volume from a real beginner program), but it means the comparison is running-volume-to-running-volume, not total-training-stress-to-total-training-stress. **Recorded as a disclosed, non-blocking note for any future revisit of this cell or of the `CrossTrainDay` non-goal — not resolved here.**

## 6. Final classification

```
BEGINNER_3D_CORE_NON_SUPPORT_FORMALIZED_FINAL
```

Beginner×3D×Core (8-14wk) is formally, fully closed as non-representable under the approved V1 load policy, for every readiness state, via the existing (not new) identity-allow-list mechanism. Peak band (16.0-20.0km, ref 17.0km) frozen for future reference only. No code changed. Two open, disclosed, non-blocking items remain for future work: the legacy-fallback user-facing path (§4) and the cross-training evidence-comparison caveat (§5).
