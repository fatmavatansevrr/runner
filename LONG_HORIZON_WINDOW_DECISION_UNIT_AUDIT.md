# Long-Horizon Window Decision-Unit Audit

**Status: AUDIT EVIDENCE.** No production code was changed to produce this document. All findings are traced directly from real, unmodified production code and a deterministic (non-HTTP) computation using that code's own real formulas.

## B1. What exactly is a "window" in production?

1. **Exact entity/range passed to `WindowExecutionSummaryBuilder`:** the caller (`LongHorizonRollingWindowActivationService.ActivateNextWindowAsync`) computes `windowSessions = aggregate.Weeks.Where(w => w.GlobalWeek >= aggregate.CurrentWindowStartWeek && w.GlobalWeek <= aggregate.CurrentWindowEndWeek).SelectMany(w => w.Sessions)`, maps them via `WindowCheckpointEvidenceMapper.ToEvidence(windowSessions)`, and passes that to `WindowExecutionSummaryBuilder.Build(...)`. `WindowExecutionSummaryBuilder` itself (`Adaptation/WindowExecutionSummaryBuilder.cs`) is range-agnostic — it has no concept of "week," only a flat list of `LogicalSessionEvidence`; the window boundary is decided entirely by the caller.
2. **Exact session set summarized:** every session belonging to every structural week whose `GlobalWeek` falls within `[CurrentWindowStartWeek, CurrentWindowEndWeek]` — i.e. the plan's **currently active rolling activation window**, whatever its real size.
3. **How many calendar weeks may one rolling window contain?** Up to 4, by construction — confirmed by direct read of two independent call sites, both using the identical rule:
   - `LongHorizonRollingInitialActivationRuntime.cs:123`: `var actualWindowSize = Math.Min(RequestedWindowSizeWeeks, skeleton.GeneralEnduranceWeeks);` where `RequestedWindowSizeWeeks = 4` (line 44).
   - `LongHorizonRollingCheckpointRuntime.cs`: `var nextEnd = Math.Min(nextStart + 3, request.StructuralRoadmap.GeneralEnduranceWeeks);`
   Both the plan's very first window and every subsequent checkpoint-driven window use the same "up to 4 structural weeks, capped by remaining General Endurance budget" rule. There is **no separate, smaller "per-week" decision unit anywhere in the real activation path** — 4 weeks (16 sessions) is the *requested*, uncapped size; smaller windows only occur when the GE budget remaining is itself below 4 weeks.
4. **Invocation unit for `NextWindowLoadDecisionPolicy`:** exactly once per real `activate-next-window` HTTP call, evaluated against the full evidence of the window just checkpointed (whatever its real size). **Once per rolling activation window**, never once per training week and never once per checkpoint "block" in any smaller sense.
5. **Intended decision unit when the 0/1/2/3/4+ matrix was created:** every canonical example across Rev3, Rev3.1, and Rev4 (§6's "Kilit örnek," §7's decision-matrix walkthrough, the locked scenario) uses exactly 4 sessions spanning **one single training week** (Mon/Wed/Fri/Sun = Easy/Key/Easy/Long). No canonical document, at any revision, presents or discusses a multi-week window example.
6. **Is `ExpectedSessionCount=4` an invariant of the policy, or merely an example?** **Merely an example — confirmed by the implementation's own source comment**, `NextWindowLoadDecisionPolicy.cs:4-7`:
   > *"The decision matrix below is an explicit PRODUCT DEFAULT calibrated for the current 4-session pilot (Mon/Wed/Fri/Sun = Easy/Key/Easy/Long), not a general formula and not a claimed scientific threshold."*
   The actual `DetermineLoadDecision` switch statement (lines 29-39) reads only `summary.EffectiveCompletedCount` as a raw absolute integer — it never reads or normalizes against `summary.ExpectedSessionCount` at all. The thresholds (0/1/2/3/≥4) are literal, fixed integers, not fractions or percentages of the window's actual size.

## B2. Is 16 sessions normal or an artifact?

**Method:** a deterministic sweep (no HTTP, no database) using the real, unmodified `RaceHorizonPolicy.CalculateAvailableWeeks` (the canonical horizon-week authority) plus the exact window-chunking formula confirmed in B1.3 above, applied to real `(startDate, raceDate)` pairs across:
- 4 start dates spanning different weekdays and seasons (2026-09-07 Mon, 2026-09-09 Wed, 2026-01-05 Mon, 2026-12-01 Tue)
- race durations 21 through 52 weeks (32 values), covering every horizon this repository's own horizon-scan governance work (`LongHorizonNumericActivationBoundaryScanTests`) already treats as the supported range

**Result — full distribution across 576 real windows generated:**

| Session count (weeks) | Occurrences | Share |
|---|---|---|
| 4 (1 week) | 32 | 5.6% |
| 8 (2 weeks) | 32 | 5.6% |
| 12 (3 weeks) | 32 | 5.6% |
| **16 (4 weeks)** | **480** | **83.3%** |

Every 4/8/12-session window observed is the **final, remainder-sized window** of a plan whose total GE-week budget is not an exact multiple of 4 (e.g. `geWeeks=9` → windows `[1-4][5-8][9-9]`, the last being a lone 4-session remainder). No 4-session window was ever observed as the *first* window when the GE budget exceeds 4 weeks — it only appears when it is structurally forced to (the entire remaining budget is small).

### Verdict: **DOMINANT_CURRENT_PILOT_BEHAVIOR**

16 sessions is not a test artifact, not a rare edge case, and not exclusively tied to any one phase boundary — it is the single most common real window size for this exact TEN_K/Intermediate/4D pilot across the entire realistic horizon range, occurring in over 4 out of 5 real windows. The earlier-observed 4-session (single-week) window is itself the atypical case, occurring only at specific GE-budget-remainder boundaries — most commonly as the plan's very first window when the total race horizon is short enough (e.g. 21 weeks, `geWeeks=1`) that the entire GE phase fits in under 4 weeks.

## B3. Policy behavior across window sizes

`NextWindowLoadDecisionPolicy.DetermineLoadDecision` (verbatim, unchanged):
```csharp
return summary.EffectiveCompletedCount switch
{
    0 or 1 => Reduce,
    2 => Maintain,
    3 => OnlyEasyMissing(summary) ? ProgressAsPlanned : Maintain,
    >= 4 => ProgressAsPlanned,
    _ => Reduce,
};
```

For a real 16-session window, this produces (percentages shown for readability only — the policy itself never computes or reads a percentage):

| Completed | % adherence | Decision | Why |
|---|---|---|---|
| 0/16 | 0% | Reduce | `0 or 1` branch |
| 1/16 | 6.25% | Reduce | `0 or 1` branch |
| 2/16 | 12.5% | Maintain | `2` branch |
| 3/16 | 18.75% | **Maintain** (never ProgressAsPlanned) | `3` branch, but `OnlyEasyMissing` is structurally unreachable here — see below |
| **4/16** | **25%** | **ProgressAsPlanned** | `>= 4` branch |
| 5/16 | 31.25% | ProgressAsPlanned | `>= 4` |
| 8/16 | 50% | ProgressAsPlanned | `>= 4` |
| 12/16 | 75% | ProgressAsPlanned | `>= 4` |
| 15/16 | 93.75% | ProgressAsPlanned | `>= 4` |
| 16/16 | 100% | ProgressAsPlanned | `>= 4` |

**Direct answer to the example question posed:** yes — **4 completed out of 16 expected currently produces `ProgressAsPlanned`**, proven directly from the unmodified policy source above: `EffectiveCompletedCount >= 4` is the *entire* condition for that branch; it does not matter which 4, or that 12 of the 16 expected sessions are still outstanding.

**Why `OnlyEasyMissing` is structurally unreachable at exactly `3/16`:** `KeySessionCompleted`/`LongRunCompleted` in `WindowExecutionSummaryBuilder` are AND-reduced *across every occurrence of that role in the window* (`keyCompleted &= isEffectivelyCompleted`, once per structural week — so 4 KEY sessions in a 16-session window all have to be individually completed for `KeySessionCompleted` to be true). `OnlyEasyMissing` requires both `KeySessionCompleted` and `LongRunCompleted` to be true — i.e. all 4 Key sessions *and* all 4 Long sessions completed (8 sessions), which alone already exceeds `EffectiveCompletedCount=3`. So at a 16-session window, the entire "3-of-4, only Easy missing → ProgressAsPlanned" sub-branch that Rev3/Rev4/Rev4.1 describe **can never fire** — it collapses to always-Maintain at `EffectiveCompletedCount=3`, a real, silent behavioral divergence from the single-week model the sub-branch was written for.

## B4. Canonical compatibility verdict

### **WINDOW_DECISION_POLICY_NOT_DEFINED_FOR_MULTI_WEEK_WINDOWS**

The runtime legitimately, normally, and predominantly (83.3% of real windows, B2) uses multi-week (most commonly 4-week/16-session) rolling activation windows. This is not a bug in window construction — it is the intended, documented sizing rule (`RequestedWindowSizeWeeks = 4`, B1.3). However:

- The canonical absolute-count matrix (0/1/2/3/≥4) was, by the implementation's **own disclosed source comment**, calibrated only for a 4-session single week and is explicitly *not* a general formula.
- No revision of the canonical spec (Rev3 through Rev4.1) defines, discusses, or even acknowledges that the real decision unit can be 4x larger than the documented example.
- The practical consequence is measurable and real: at a 16-session window, `ProgressAsPlanned` fires at just 25% completion (B3) rather than 100%; `Maintain`'s "only Easy missing" role-aware sub-branch can never fire at all (B3); and the qualitative meaning of "Reduce" (0-1 completed) shifts from "almost nothing done" (0-25% of a 4-session week) to "an extremely small fraction of a much larger window" (0-6.25% of 16) — a materially different severity signal than the one the matrix's own calibration language describes.

Per the explicit instruction, **no normalized/percentage threshold is proposed or invented here.** This is returned as `DecisionRequired`, not resolved.

**`TEN_K_INTERMEDIATE_4D_ADAPTATION_V1_IMPLEMENTED_AND_VERIFIED` remains BLOCKED on this item** until the user decides one of: (a) the absolute matrix is intentionally meant to apply as-is regardless of window size (and the spec should say so explicitly), (b) the matrix needs a window-size-aware redefinition, or (c) some other resolution.
