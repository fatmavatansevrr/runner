# PHASE HM.10 — Half-Marathon Intermediate 5D Numeric Authority Closure

Governance / product-decision phase. No production code, no catalog file, no schema was modified by this phase.

## 1. HM.9 structural authority consumed

`RUN_LAYOUT_5D` = `{K,E,K,E,L}` (unchanged, not reopened). Secondary-KEY semantics per HM.9: Foundation/Taper = `EASY_EQUIVALENT`; Build/RaceSpecific = `LIGHT_QUALITY`, family `FARTLEK`, fallback `EASY_STANDARD`; `MaximumTrueHardStimuliPerWeek = 2`; ~48h minimum / HARD+HARD-prohibited spacing; LONG remains standard/easy. All consumed exactly, none reopened.

## 2. Source inventory

Read directly: `HM_21K_Claude_Handoff_and_Build_Plan.md` §4.2 (peak band); `PHASE_HM_7_INTERMEDIATE_3D_NUMERIC_AUTHORITY_CLOSURE.md` and `PHASE_HM_9_...md` (methodology precedent); `VolumeSafetyPolicy.cs`'s real `FiveDayIntermediate` (10K) instance in full; `CatalogSessionPrescriptionPlanner.cs` and `V1FourDaySessionVolumeAllocationPolicy.cs` in full (real session-allocation mechanism); `half-marathon-workout-progression.v1.json` (confirms `FARTLEK v4` already exists as an approved HM workout).

## 3. Peak band status

`Intermediate 5D: 44–58 km/week` — re-verified directly at `HM_21K_Claude_Handoff_and_Build_Plan.md:165` (already confirmed real in HM.6). Classification: `CURRENT_FROZEN_AUTHORITY` (the band itself; the selected peak is a separate decision, §4).

## 4. Selected peak decision

**`SelectedPeakReferenceKmPerWeek = 51.0`** — the midpoint of `[44,58]`. This is now the **third** HM cell (after 4D's `43.0` = midpoint of `[36,50]`, and 3D's `34.0` = midpoint of `[28,40]`) landing exactly on the band midpoint, strengthening rather than merely repeating HM.7's own observation: this is a real, twice-confirmed, now-doubly-verified Appsel convention, not an arbitrary arithmetic shortcut. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 5. Weekly progression authority

**`PreferredWeeklyIncreaseRate = 0.07`, `HardWeeklyIncreaseRate = 0.08`** — reused directly; confirmed genuinely universal across every named 10K/HM `VolumeSafetyPolicy` instance (`DIRECT_EXISTING_AUTHORITY`, unchanged from HM.7's own finding).

## 6. Absolute increase cap decision

**`AbsoluteWeeklyIncreaseCapKm = 2.5`** — **not** interpolated from 3D's `2.0`/4D's `2.5`. Direct, on-point 10K precedent: `VolumeSafetyPolicy.FiveDayIntermediate` (the exact same Level×Frequency cell, distance differs) already uses `2.5`, identical to 4D's own value — confirming this field does not uniformly decrease with frequency (3D is the outlier at `2.0`, tied to its own on-point 3D precedent; 5D and 4D both sit at `2.5`, each independently confirmed from their own real 10K same-cell sibling). Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`.

## 7. Calibration decision

**`GoldenFixtureStartingVolumeKm = 29.5`** (calibration-only). Derived via the same established methodology HM.7 used for 3D: reuse `HalfMarathonIntermediate4D`'s own proven ratio (`25.0/43.0 = 0.5814`) against this cell's own new peak (`51.0`): `51.0 × 0.5814 = 29.65`, rounded to the nearest 0.5km grid increment = `29.5`. Reusing the 4D ratio (rather than 3D's) keeps a single, consistent reference cell across every subsequent HM frequency decision, avoiding an arbitrary choice between two available siblings. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT`. Explicit guardrail, unchanged in kind from 3D/4D: calibration-only, never a missing-readiness fallback, explicit-zero substitution, or readiness threshold.

**`GoldenFixtureNonTaperTransitions = 11`** — reused directly, `DIRECT_EXISTING_AUTHORITY` (frequency-generic structural derivation from HM's own phase-week structure, unchanged).

## 8. LR evidence

No approved prior 5D long-run-share percentage set exists merely because an earlier prompt mentioned approximate values (re-confirmed: `HM_21K_Claude_Handoff_and_Build_Plan.md` contains no percentage figures for any frequency, per HM.6's own original finding, re-verified). Real, on-point 10K methodology evidence: `VolumeSafetyPolicy.FiveDayIntermediate`'s own real values (`0.28/0.36/0.28/0.36`) versus `Default`'s (4D, `0.30/0.36/0.33/0.40`) show a real, already-shipped 10K pattern — 5D's own selected share and hard cap are measurably *lower* than 4D's, directionally confirming (not merely hypothesizing) that more sessions → lower long-run share is a genuine, already-observed Appsel pattern, not an invented assumption.

## 9. LR quadruple decision

Applied the **exact same directional deltas** 10K's own real data shows between its Intermediate 4D and 5D policies (selected `−0.05`, hard cap `−0.04`, preferred min `−0.02`, preferred max `0`) to HM's own already-frozen 4D quadruple (`0.30/0.36/0.33/0.40`):

```
PreferredLongRunShareMin = 0.30 - 0.02 = 0.28
PreferredLongRunShareMax = 0.36 - 0.00 = 0.36
SelectedLongRunShare     = 0.33 - 0.05 = 0.28
HardLongRunShareCap      = 0.40 - 0.04 = 0.36
```

**Honest disclosure**: this derivation coincidentally lands on numbers identical to 10K's own real `FiveDayIntermediate` values. This is a consequence of applying an independent, principled *delta methodology* to HM's own baseline — not a numeric copy of 10K's values — but the coincidence is disclosed plainly rather than obscured. Unlike HM.7's 3D uplift (a share *increase*, a genuine departure from HM's conservative baseline requiring explicit user sign-off), this is a share *decrease* — inherently the more conservative direction, consistent with HM's own established safety philosophy without introducing new risk, so it was not separately escalated. Classification: `PRODUCT_DEFAULT` (methodology-informed, explicitly not claimed as evidence-backed, and explicitly not claimed as an "HM.9 authority" merely because 10K happens to use the same digits).

## 10. Absolute LR ceiling scope

**`PreferredAbsolutePeakLongRunKm = 19.0`, reused directly** — HM.7 already broadened this to `HALF_MARATHON × INTERMEDIATE` (frequency-independent); no contradictory authority was found for 5D. Feasibility: at `51.0 × 0.36 (hard cap) = 18.36 → 18.5km` (nearest-rounded) — comfortably below 19.0km at the band midpoint; even at the band's own upper bound (`58km × 0.36 = 20.88 → 21.0km`, which *would* exceed 19.0km) the ceiling correctly binds and caps the result, exactly as designed — the ceiling is a real, load-bearing safety bound for this cell's upper band range, not decorative. Classification: `DIRECT_EXISTING_AUTHORITY` (already-broadened scope, unchanged).

## 11. Starting LR scope

**`StartingAbsoluteLongRunCap = 12.0`, reused directly** — HM.7's own broadening (frequency-independent, no frequency column in the source table) already covers 5D; no 5D-specific cap invented. Classification: `DIRECT_EXISTING_AUTHORITY`.

## 12. Taper scope

**`TaperWeek1VolumeMultiplier = 0.70`, `TaperWeek2VolumeMultiplier = 0.43`, reused directly.** HM.7's own re-verification already established these four evidence sources (Mujika & Padilla; Marathon Handbook; best-running-tips.com; runtothefinish.com) are volume/proximity-based, not frequency-based. 5D's own peak (`51.0`) sits below best-running-tips.com's own named lighter-taper-exception threshold (`~56 km/week`), so the standard reduction regime applies unchanged, same as 3D and 4D. Classification: `EVIDENCE_INFORMED_PRODUCT_DEFAULT` (reused with re-verified scope, not blind copy). Weekly taper-volume reduction (this decision) and taper session *composition* (HM.9's own, unchanged — secondary KEY stays `EASY_EQUIVALENT` in Taper, no new hard stimulus) are correctly kept as separate concepts, per this phase's own instruction.

## 13. Secondary KEY dose feasibility

**No new dose authority is required.** Direct read of `half-marathon-workout-progression.v1.json` confirms `FARTLEK v4` is already a real, approved HM workout (bound today to `BUILD_FASTER_THAN_HM_INTRO`, HM's own existing primary-lane exposure). The secondary-KEY lane's `FARTLEK` content (HM.9 §8) can reuse this exact, already-approved `WORKOUT_DEFINITION` verbatim — no new exact distance/duration number is invented; the existing `FARTLEK v4` definition's own already-approved dose parameters apply directly. Classification: `DIRECT_EXISTING_AUTHORITY`. `SECONDARY_KEY_DOSE_AUTHORITY_MISSING` is explicitly **not** the finding here.

## 14. Session allocation feasibility

Read `CatalogSessionPrescriptionPlanner.cs` and `V1FourDaySessionVolumeAllocationPolicy.cs` directly, not assumed. Critical, previously-undocumented finding: **the real dispatch is binary** — `isThreeDay ? V1ThreeDaySessionVolumeAllocationPolicy.Allocate(...) : V1FourDaySessionVolumeAllocationPolicy.Allocate(...)`. Every non-3-day candidate — including every one of 10K's own currently-reachable real 4D/5D/6D combinations — is already routed through `V1FourDaySessionVolumeAllocationPolicy` today. Its own `KeySessionDistancesKm`/`EasySupportDistancesKm` fields are genuinely `IReadOnlyList<double>`, confirmed already generalized (a code comment cites this exact prior generalization: *"generalized from a binary ordinal==0?First:Second ternary... to indexing the full resolved list"*), and — most importantly — another comment in the same file names **"6D Core's 2K+3E+1L"** as a real, already-shipped, currently-reachable 10K production shape. **This means the generic mechanism for allocating volume across two real `KEY_SESSION` slots is not merely theoretically capable — it is already exercised in real, live 10K production today.** Classification: `GENERIC_REUSE`, confirmed real, not assumed. No new HM-specific session-allocation policy class is needed; HM 5D would only need wiring into the same existing dispatch branch 4D already uses (mirroring HM.8's own precedent of widening an existing boolean-style dispatch condition, not adding a new allocator).

Numeric feasibility check at the 14W golden peak (`51.0 km/week`, ordinary LR `≈51×0.28=14.28→14.5km`): remaining volume for `KEY1+KEY2+EASY1+EASY2` = `51.0 − 14.5 = 36.5km` across 4 sessions, averaging `≈9.1km/session` — comfortably above every existing HM/10K session-distance minimum this engagement has established, and not implausibly large. No negative residual, no impossible KEY prescription.

## 15. Exact 10–16 numeric simulations

Using the frozen phase allocations (unchanged, `10W→2/3/3/2` … `16W→4/5/5/2`) and this phase's frozen 5D authority, hand-traced against the same, already-proven-generic `ResolvePeak` mechanism (HM.5.3): with `ResolvedPeakReferenceIsSelectedCeiling = true` reused unmodified for this new policy (exactly as 3D/4D already do, `DIRECT_EXISTING_AUTHORITY`, zero new code), transitions cap at the calibration count (`11`) for any horizon at or beyond it. **14W** (exactly 11 transitions) resolves to the calibration ratio itself, `29.5 × (51.0/29.5) = 51.0` — clean and self-consistent, matching the same no-residual-scaling pattern already proven for 4D/3D's own 14W cases. **15W/16W** (13/16 non-taper transitions respectively, both exceeding 11) saturate at `51.0`, never extrapolating past it — the exact mechanism that already fixed the 16W 46.5km 4D defect (HM.5.3) applies automatically here with zero new logic. **10W–13W** (fewer transitions) resolve proportionally below `51.0`, never forced upward. No horizon-specific table was authored — this table is the *output* of the existing generic formula, not an input to it.

## 16. LR rounding proof

Preserves HM.5.3's `RoundDown`-for-safety-ceiling mechanism unmodified and automatically. Worked example at the 14W peak: weekly volume `51.0`, hard cap `0.36` → raw ceiling `18.36km` → `RoundDown` to the 0.5km grid → `18.0km` (never `18.5`, which nearest-rounding would have produced and which would still be `<19.0` here, but the floor discipline holds regardless of whether the specific example happens to matter) — the final materialized value can never exceed the true 36% fraction after rounding, by the same construction already proven zero-delta for 3D/4D.

## 17. Calendar spacing feasibility

`RUN_LAYOUT_5D`'s own slot sequence (`K,E,K,E,L`) places an `EASY_SUPPORT` slot between the two `KEY_SESSION` slots in sequence order — structurally consistent with the ~48h HM.9 spacing rule once real weekday assignment applies, and this exact 2-KEY shape is (per §14 above) already real, shipped 10K production reality for 6D today. **However**, this phase cannot confirm from governance-level reading alone whether the generic calendar day-placement algorithm is *hardness-aware* — i.e., whether it specifically enforces the ~48h/HARD+HARD-prohibited rule HM.9 froze, or merely places slots by role/day-of-week preference with no concept of which `KEY_SESSION` instance is "hard" versus "light quality." This is honestly flagged, not silently assumed resolved: **`CALENDAR_HARDNESS_SEMANTIC_GAP` — status undetermined, requires direct code verification in the implementation phase.** This is classified as an `IMPLEMENTATION_CAPABILITY_GAP`, not a numeric-authority gap (§22).

## 18. Adaptation feasibility

Not implemented or redesigned here (out of scope). Qualitatively, unchanged from HM.9's own finding: existing role-aware generic adaptation treats both `KEY_SESSION` instances identically today (no primary/secondary role-subtype exists). For **initial dark generation** (this cell's own current scope — materialization and validity, not live user-adaptation scoring), this is classified `SAFE_FOR_INITIAL_DARK`: a dark vertical-slice proof does not exercise live completion/adaptation scoring, so the absence of a primary/secondary distinction does not block HM.11's own dark-materialization objective. Whether adaptation eventually needs a role-subtype is a real, separately-tracked downstream question (HM.9 §16, not reopened or resolved here).

## 19. Pace/fallback compatibility

Unchanged. Primary HM pace semantics are untouched. The secondary `FARTLEK` lane does not depend on exact HM pace evidence at all (per `CatalogSessionPrescriptionPlanner.PaceFor`'s own real switch, `FARTLEK` falls to the generic effort-only branch, same as `EASY_STANDARD`) — confirmed directly, not assumed: a 5D plan remains fully representable with or without independent HM pace evidence, exactly matching HM.9 §21's own requirement.

## 20. Complete authority table

| Authority | Value | Classification | Scope |
|---|---|---|---|
| PeakVolumeBand | 44–58 km/week | `CURRENT_FROZEN_AUTHORITY` | HM × Intermediate × 5D |
| SelectedPeakReference | 51.0 km/week | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| PreferredWeeklyIncreaseRate | 0.07 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| HardWeeklyIncreaseRate | 0.08 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| AbsoluteWeeklyIncreaseCapKm | 2.5 km | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| CalibrationStartingVolume | 29.5 km (calibration-only) | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| GoldenFixtureNonTaperTransitions | 11 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| LR PreferredShareMin | 0.28 | `PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| LR PreferredShareMax | 0.36 | `PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| LR SelectedShare | 0.28 | `PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| LR HardShareCap | 0.36 | `PRODUCT_DEFAULT` | HM × Intermediate × 5D |
| AbsolutePeakLongRunKm | 19.0 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| StartingAbsoluteLongRunCap | 12.0 | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate (frequency-independent) |
| TaperWeek1Multiplier | 0.70 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate (frequency-independent) |
| TaperWeek2Multiplier | 0.43 | `EVIDENCE_INFORMED_PRODUCT_DEFAULT` | HM × Intermediate (frequency-independent) |
| SecondaryKeyExactDoseAuthority | Reuse existing `FARTLEK v4` | `DIRECT_EXISTING_AUTHORITY` | HM × Intermediate × 5D |
| Session allocation mechanism | `V1FourDaySessionVolumeAllocationPolicy` (existing, generic) | `DIRECT_EXISTING_AUTHORITY` (`GENERIC_REUSE`) | Frequency-generic, already-proven in 10K production |
| Calendar hardness-awareness | Undetermined | N/A — `IMPLEMENTATION_CAPABILITY_GAP`, not numeric | Deferred to HM.11 |

## 21. Evidence/product-default classifications — summary

No value is classified `EVIDENCE_BACKED`. `DIRECT_EXISTING_AUTHORITY`: weekly progression rates, calibration transition count, absolute LR ceiling and starting cap (both already frequency-broadened), the secondary-KEY dose reuse, and the session-allocation mechanism itself. `EVIDENCE_INFORMED_PRODUCT_DEFAULT`: selected peak (now a twice-confirmed midpoint pattern), absolute weekly cap (direct on-point 10K precedent), calibration start (established ratio-reuse methodology), taper multipliers (re-verified frequency-blind evidence). `PRODUCT_DEFAULT`, explicitly disclosed as judgment not evidence: the long-run-share quadruple — its coincidental numeric match to 10K's own `FiveDayIntermediate` values is called out plainly rather than obscured.

## 22. Remaining implementation capability gaps

One, explicitly separated from numeric authority per this phase's own required distinction: **`CALENDAR_HARDNESS_SEMANTIC_GAP`** — whether the generic calendar day-placement algorithm is aware of which `KEY_SESSION` instance is "hard" versus "light quality" for enforcing HM.9's ~48h/HARD+HARD-prohibited spacing rule is undetermined by this governance-only phase and requires direct code verification in the implementation phase. This is an `IMPLEMENTATION_CAPABILITY_GAP`, not a `NUMERIC_AUTHORITY` gap — it does not block this phase's own closure, per §25's explicit instruction to keep the two concerns separate.

## 23. HM.11 readiness conclusion

Every numeric field required by §23/§27 is frozen: selected peak, weekly progression, absolute increase cap, calibration start, the LR share quadruple, the 19.0km/12.0km scope resolutions, the taper multiplier scope, and the secondary-KEY dose authority (needs no invented number, reuses `FARTLEK v4` directly). Session distribution is numerically feasible (§14) and the underlying mechanism is already real and already proven in shipped 10K production, not merely theorized. 10–16W simulations are feasible with no horizon chasing or overshooting selected-peak authority (§15), LR rounding remains safe (§16), and pace fallback remains valid (§19). The one open item (§22) is a real, disclosed, correctly-classified implementation-capability question, not a numeric gap, and does not block this phase's own classification.

## Final classification

`HM_INTERMEDIATE_5D_NUMERIC_AUTHORITY_CLOSED`
