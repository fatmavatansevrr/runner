# Phase 10K-FREQ.2 — Beginner 3D Runway+Core (15-20wk) Viability Research

**Research/audit only. No code touched. Beginner×3D×Core's non-support (GEN.5C), the frozen 16-20km/17.0km band (GEN.5A.2), and the trajectory-shape finding (FREQ.1) are all reused exactly as they stand — none reopened.**

## A. Runway architecture re-familiarization — a major finding, checked not assumed

**A1.** Confirmed directly: zero references to `CatalogVolumeAndLongRunPlanner`, `TaperVolumeMultiplier`, or any `VolumeSafetyPolicy` taper concept anywhere in the entire `RuntimeCatalog/Schedule/PreparationRunway*` subsystem (`grep`, zero hits). Runway does **not** reuse Core's taper mechanism at all — this confirms GEN.0/GEN.CHECKPOINT.1's finding that horizon families are structurally separate pipelines, specifically for Runway, not merely assumed by analogy.

**A2. The real finding, more significant than the phase anticipated**: read `TenKPreparationRunwayNumericPolicyFactory.cs` in full. Runway's entire numeric policy is:

```csharp
public const string CandidateKey = "TEN_K__4D__INTERMEDIATE";
public const int CandidateVersion = 10;

public static PreparationRunwayNumericPolicy Build()
{
    var core = VolumeSafetyPolicy.Default;   // hardcoded to Intermediate x4D specifically
    return new PreparationRunwayNumericPolicy(
        ...
        ContinuityToleranceKm: V1FourDaySessionVolumeAllocationPolicy.ToleranceKm,   // 4D-specific type, by name
        ...
        ["FourDaySlotDistribution"] = PreparationRunwayNumericRuleClassification.ReusedCoreBehavior,
        ...);
}
```

**Runway is not "currently gated to Intermediate×4D" the way Core's public identity allow-list gates Beginner×3D (GEN.4E/GEN.5C) — it is architecturally hardcoded to that one specific cell.** There is no Level or Frequency parameter anywhere in this factory; it directly references `VolumeSafetyPolicy.Default` (not a Level/Frequency-dispatched lookup) and `V1FourDaySessionVolumeAllocationPolicy` (a 4D-specific type) by name, and even labels a `"FourDaySlotDistribution"` rule as structurally reused. This is categorically different from Core's identity-allow-list containment (GEN.4E), which is a real, generalized, multi-cell-aware mechanism that simply hasn't admitted every cell yet. Runway has no such generality to admit into — it would need new architecture, not a widened allow-list, to support any cell other than Intermediate×4D, 3D or otherwise.

**This is dispositive on its own, independent of any numeric question**: no mechanism exists today for Beginner×3D (or Intermediate×3D, or Beginner×4D) to even enter Runway's numeric materialization pipeline, regardless of what the compounding math would produce.

## B. Hypothetical numeric extension (completed as requested, for completeness — result does not change the answer)

Section B of this phase asked for the numeric question to be answered as "the single load-bearing question." Given §A1's finding, this is not actually load-bearing (the architectural gap is dispositive by itself), but computed anyway, honestly labeled as counterfactual: *if* a 3D-capable Runway mechanism existed and reused Core's exact frozen compounding/taper mechanism unmodified, would extending to 15-20 weeks help?

Extended GEN.5A.2's real, ceiling-clamped (20.0km) sequence from 14 out to 20 weeks. The ceiling-clamp mechanism (§FREQ.1, confirmed mechanically: once `reachable == ceiling`, every subsequent step re-clamps back to the same ceiling) means growth plateaus permanently once reached — extending the horizon range past the plateau point adds no further reachable volume.

**Missing-readiness (start=12.0km, ceiling=20.0km):** plateaus at 20.0km starting week 11 (already established in GEN.5A.2 §3) and stays flat at 20.0km through week 20. Taper = `Round0.5(20.0 × 0.53) = 10.5km` at every one of weeks 11-20, identical to weeks 11-14's already-ineligible result.

**Explicit-zero (start=9.5km, ceiling=20.0km):** natural growth reaches the ceiling later — continuing GEN.5 §B's real sequence: week 15 reaches 19.5km (still below ceiling), week 16 hits and clamps to 20.0km, and stays flat at 20.0km through week 20.

| Weeks | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16 | 17 | 18 | 19 | 20 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Reachable (km) | 12.5 | 13.5 | 14.5 | 15.5 | 16.5 | 17.5 | 18.5 | 19.5 | 20.0 | 20.0 | 20.0 | 20.0 | 20.0 |
| Taper (km) | 6.5 | 7.0 | 7.5 | 8.0 | 8.5 | 9.5 | 10.0 | 10.5 | 10.5 | 10.5 | 10.5 | 10.5 | 10.5 |
| vs 12.0km floor | — | — | — | — | — | — | — | — | — | — | — | — | — |

**Every single value in both tables remains below the 12.0km floor.** Even the maximum reachable value the frozen 20.0km band ceiling permits (taper = 10.5km) falls 1.5km short of the floor — this is not close, and no realistic additional horizon extension changes it, since the value is already fully plateaued well within the 15-20wk range.

## C. Result

Both the architectural check (§A) and the hypothetical numeric extension (§B) agree, independently, on non-viability — for two different, mutually-reinforcing reasons: (1) no mechanism exists for Beginner×3D to enter Runway's numeric pipeline at all today, and (2) even under a counterfactual where it did and reused Core's exact frozen mechanism unmodified, the frozen evidence-grounded band's ceiling (20.0km) still can't produce a taper-survivable pre-taper volume — the same structural reason established for Core (GEN.5A.2 §4: ≈22.2km required, this band never reaches it), now confirmed to extend unchanged through the full 15-20wk range because the growth mechanism is fully plateaued well before week 15.

**No band loosening was proposed or considered** to force a different result, per explicit instruction.

The real open question this leaves, explicitly not answered here (out of scope): whether Beginner×3D is only representable at LongHorizon (21+wk, where the mechanism is different again — not investigated), or requires a fundamentally different numeric mechanism entirely (e.g., a non-compounding, reference-based approach the way 4D works) — flagged for a future phase, not resolved.

## D. Final classification

```
BEGINNER_3D_RUNWAY_NON_REPRESENTABLE
```

Non-representable for two independent, compounding reasons: no architectural pathway exists in Runway's numeric materialization pipeline for any cell other than Intermediate×4D (§A), and even a hypothetical extension of Core's exact frozen mechanism through 20 weeks never clears the taper floor under the frozen band (§B). Neither Beginner×3D×Core's non-support decision, the frozen peak band, nor the trajectory-shape finding were reopened or modified.
