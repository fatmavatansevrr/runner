# Phase 4G.3B.0 — VolumeSafetyPolicy Contract Extraction Governance Note

**Status: pure refactor. No numeric value changed. No public/live behavior changed.**

## What this note is

A per-field provenance record for `VolumeSafetyPolicy`
(`backend/RunningApp.Application/RuntimeCatalog/Prescription/Volume/VolumeSafetyPolicy.cs`), extracted
from `CatalogVolumeAndLongRunPlanner`'s previously-private inline `const` fields. Every value below is
byte-identical to the constant it replaces — this note records *where each number was already
established*, not a new decision.

## Where this note lives, and why

Neither `plan-catalog/artifacts/audits/activation-readiness-risks.json` nor `plan-catalog/docs/evidence-log.json`
is the right home for this content, confirmed by inspecting both schemas before writing this note:

- `activation-readiness-risks.json` is a living aggregator of **open, unresolved cross-phase risks**
  (`id`/`statement`/`requiredResolution`/`status: OPEN|CLOSED`, etc.) — every field in this policy is
  already resolved/unchanged, not an open risk. Adding an entry there would misrepresent a pure refactor
  as a pending decision.
- `evidence-log.json` tracks **external/scientific evidence for runtime-condition resolver thresholds**
  (`relatedResolver` is always one of `GOAL_FEASIBILITY_IN`/`PACE_SOURCE_IN`/`TIME_ADEQUACY_IN`/
  `CORE_ENTRY_READINESS_IN`, `relatedCanonicalDecision` ties to a resolver reconciliation item). This
  policy governs volume/long-run *planning* math, not a resolver's condition-classification threshold —
  it does not fit that schema either.

This note is therefore a standalone document, per the task's explicit instruction to keep it standalone
rather than guess a schema fit.

## Provenance limitation

No commit-level provenance exists for the runtime planner constants. The relevant Phase 4F.7B and
Phase 4G.3B.0 source files are untracked relative to
`HEAD 0c6796578f08bc1d76d96f1944a80c9075455206`, so runtime chronology for these values is
pass/document-based rather than commit-based. The golden fixture itself does have Git provenance at
commit `fe850446d1382ae950942dff7062ef2a89d941ca`.

## Per-field provenance

| `VolumeSafetyPolicy` field | Value | Prior location (file/line, pre-refactor) | Source classification |
|---|---:|---|---|
| `PreferredMaxWeeklyIncreaseRatio` | `0.07` | `CatalogVolumeAndLongRunPlanner.cs:12` (`IntermediatePreferredMaxWeeklyIncrease`) | Reused from `docs/canonical/golden-fixture-v3/progression_rules_v2.yaml` `profilePercentageCaps.INTERMEDIATE` (as already cited in `ResolvePeak`'s own decision-trace provenance string) |
| `HardMaxWeeklyIncreaseRatio` | `0.08` | `CatalogVolumeAndLongRunPlanner.cs:13` (`IntermediateHardMaxWeeklyIncrease`) | Same source as above (`profilePercentageCaps.INTERMEDIATE`, hard band) |
| `AbsoluteWeeklyIncrementCapKm` | `2.5` | `CatalogVolumeAndLongRunPlanner.cs:14` (`FourRunAbsoluteWeeklyIncrementCapKm`) | Reused from `progression_rules_v2.yaml` `absoluteWeeklyIncrementCapKm[4]` (4-day-plan row) |
| `GoldenFixtureStartingVolumeKm` | `24` | `CatalogVolumeAndLongRunPlanner.cs:15` (`GoldenFixtureStartingVolumeKm`) | `GOLDEN_FIXTURE_DERIVED_CALIBRATION_CONSTANT`: the fixture's own week-1 `weeklyVolumeAnchorKm`. Not a general product default, readiness fallback, or catalog-authored phase constraint; used only to calibrate or reproduce the current pilot volume progression. |
| `GoldenFixtureResolvedPeakKm` | `38` | `CatalogVolumeAndLongRunPlanner.cs:16` (`GoldenFixtureResolvedPeakKm`) | `GOLDEN_FIXTURE_DERIVED_CALIBRATION_CONSTANT`: the fixture's own resolved peak week volume. Not a general product default, readiness fallback, or catalog-authored phase constraint; used only to calibrate or reproduce the current pilot volume progression. |
| `GoldenFixtureNonTaperTransitions` | `10` | `CatalogVolumeAndLongRunPlanner.cs:17` (`GoldenFixtureNonTaperTransitions`) | `GOLDEN_FIXTURE_DERIVED_CALIBRATION_CONSTANT`: derived from the fixture's 12-week/1-taper-week structure (11 non-taper weeks → 10 week-to-week transitions). Not a general product default, readiness fallback, or catalog-authored phase constraint; used only to calibrate or reproduce the current pilot volume progression. |
| `TaperVolumeMultiplier` | `0.53` | `CatalogVolumeAndLongRunPlanner.cs:18` (`TaperMultiplier`) | Phase 4F.7B.1 changed active behavior from `0.65` (35% reduction, outside that pass's accepted 41%–60% envelope) to `0.53` (47% reduction). It approximately matches the golden-fixture 38km→20km transition (`20 / 38 ≈ 0.5263`) but is not claimed as an exact value directly backed by external evidence. Evidence basis: `EVIDENCE_INFORMED`; decision status: `EXPLICIT_PRODUCT_DEFAULT`; additional provenance: `GOLDEN_FIXTURE_CALIBRATED`. The repository's current Doc13 copy does not contain the cited taper sections. |
| `LongRunPreferredMinimumShare` | `0.30` | `CatalogVolumeAndLongRunPlanner.cs:19` (`LongRunPreferredMinimumShare`) | Phase 4F.7B.1 prompt/document-supplied four-day-plan product-practice rule, replacing the prior non-canonical `0.20–0.35` active range. Evidence basis: `PRODUCT_PRACTICE_INFORMED`; decision status: `EXPLICIT_PRODUCT_DEFAULT`. Sources: `PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md` and `plan-catalog/artifacts/audits/phase4f7b1-canonical-volume-rule-correction.json`. Not attributed to repository Doc13 or directly to the golden fixture. |
| `LongRunPreferredMaximumShare` | `0.36` | `CatalogVolumeAndLongRunPlanner.cs:20` (`LongRunPreferredMaximumShare`) | Phase 4F.7B.1 prompt/document-supplied four-day-plan product-practice rule, replacing the prior non-canonical `0.20–0.35` active range. Evidence basis: `PRODUCT_PRACTICE_INFORMED`; decision status: `EXPLICIT_PRODUCT_DEFAULT`. Sources: `PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md` and `plan-catalog/artifacts/audits/phase4f7b1-canonical-volume-rule-correction.json`. Not attributed to repository Doc13 or directly to the golden fixture. |
| `LongRunSelectionShare` | `0.33` | `CatalogVolumeAndLongRunPlanner.cs:21` (`LongRunSelectionShare`) | Phase 4F.7B.1 prompt/document-supplied four-day-plan product-practice selection point. Evidence basis: `PRODUCT_PRACTICE_INFORMED`; decision status: `EXPLICIT_PRODUCT_DEFAULT`. Sources: `PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md` and `plan-catalog/artifacts/audits/phase4f7b1-canonical-volume-rule-correction.json`. Not attributed to repository Doc13 or directly to the golden fixture. |
| `LongRunHardCapShare` | `0.40` | `CatalogVolumeAndLongRunPlanner.cs:22` (`LongRunHardCapShare`) | Phase 4F.7B.1 prompt/document-supplied four-day-plan product-practice hard cap. Evidence basis: `PRODUCT_PRACTICE_INFORMED`; decision status: `EXPLICIT_PRODUCT_DEFAULT`. Sources: `PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md` and `plan-catalog/artifacts/audits/phase4f7b1-canonical-volume-rule-correction.json`. The fixture's approximate compatibility ratio does not prove this cap, the preferred band, or the selected point; these values are not attributed to repository Doc13 or directly to the golden fixture. |
| `RoundingIncrementKm` | `0.5` | `CatalogVolumeAndLongRunPlanner.cs:23` (`RoundingIncrementKm`) | V1 technical default, no external source — a display/planning-granularity choice, not a training-science claim |
| `RoundingRule` | `"round_nearest_0.5km_after_each_week_value_then_validate"` | `CatalogVolumeAndLongRunPlanner.cs:24` (`RoundingRule`) | V1 technical default, no external source (describes the mechanical rounding procedure paired with `RoundingIncrementKm`) |

## Enforcement-status classification (Phase 4G.3B.7 / 4G.3B.7.1 — TD-VOLUME-CAP-UNENFORCED-001, now CLOSED)

`HardMaxWeeklyIncreaseRatio` and `AbsoluteWeeklyIncrementCapKm` are
**informational/provenance-only**: both are threaded into `ReachablePeakDecision`
purely for decision-trace purposes and are never read back by
`CatalogVolumeAndLongRunPlanner.BuildWeeklyPlan` to clamp or reject an
actual week-to-week transition. This was discovered by
`VolumeProgressionVerifier`'s implementation pass and tracked as
`TD-VOLUME-CAP-UNENFORCED-001`. Phase 4G.3B.7's decision audit
(`PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md`) proved
algebraically that the planner's per-step interpolation ratio —
`(GoldenFixtureResolvedPeakKm / GoldenFixtureStartingVolumeKm - 1) /
GoldenFixtureNonTaperTransitions` — is independent of both starting
volume and target week count for `TEN_K_MASTER v6`'s current constants
(the transition count cancels out of the formula algebraically), and
stays structurally below both fields' values by construction, not by
coincidence of the specific inputs tested. The TD was closed on this
basis (Option B: document as informational, do not implement active
enforcement) rather than resolved by implementing a clamp (Option A,
evaluated and rejected as unnecessary given the proof, and as carrying
real redesign cost with zero benefit to current behavior).

This classification is scoped to `TEN_K_MASTER v6`'s current
`GoldenFixtureStartingVolumeKm`/`GoldenFixtureResolvedPeakKm`/
`GoldenFixtureNonTaperTransitions` constants and core-cycle/peak-volume-band
bounds. It would need re-verifying if any of those constants change, or if
a future candidate/catalog revision introduces different
starting-volume/peak-volume/core-cycle constants for which this algebraic
cancellation may no longer hold. `VolumeProgressionVerifier` remains the
sole place these two bounds are actually checked today, independent of
planner enforcement — see `PHASE4G_3B_7_VOLUME_CAP_ENFORCEMENT_DECISION_AUDIT.md`
for the full proof and evidence.

`PolicyVersion = "APPSEL_RACE_VOLUME_SAFETY_V1"` is new (Phase 4G.3B.0) — a stable version identifier for
this exact set of values, so any future value change can be traced by bumping this string.

## What was deliberately left out of scope

- **`V1MissingReadinessStartingVolumePolicy`** (`MissingWeeklyVolumeDefaultKm=16`,
  `ExplicitZeroWeeklyVolumeDefaultKm=12`) was **not** folded into `VolumeSafetyPolicy`. It is a separate,
  already-independently-versioned policy (`PolicyKey`/`PolicyVersion`, Phase 4F.7B.2) with its own
  governance doc (`PHASE4F_7B2_MISSING_ZERO_READINESS_DECISION.md`) — merging it would have been an
  unrequested architectural consolidation, not the requested "extract inline constants from
  `CatalogVolumeAndLongRunPlanner`" refactor.
- **`CatalogVolumePlanValidator`** was inspected and contains no numeric safety-threshold constants of its
  own — every check there compares already-policy-derived values (`plan.CatalogBounds`,
  `plan.PeakVolumeKm`, etc.), so it required no changes.
