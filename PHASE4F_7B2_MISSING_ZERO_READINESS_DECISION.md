# Phase 4F.7B.2 Missing and Explicit-Zero Weekly-Volume Decision

## Repository Source Findings

`plan-catalog/docs/canonical/appsel-v1-canonical-decisions.md` was inspected directly. The expected Doc13 volume sections for starting-volume defaults are absent in this repository copy. Golden Fixture v3 demonstrates a valid positive `weeklyVolumeAnchorKm = 24`, but does not define missing or explicit-zero weekly-volume behavior. `progression_rules_v2.yaml`, level modifiers, candidate eligibility, and readiness resolver artifacts do not define an Intermediate missing/zero starting-volume resolver.

Therefore this phase records a new V1 product default, not a canonical-confirmed Doc13 rule.

## Policy

Policy key: `V1_MISSING_READINESS_STARTING_VOLUME_POLICY`
Policy version: `1`

For the active `TEN_K / INTERMEDIATE / 4 days` V1 pilot:

- `RecentWeeklyVolumeKm = NOT_PROVIDED` selects a conservative Intermediate Week 1 starting volume of `16 km`.
- `RecentWeeklyVolumeKm = 0` selects a lower no-recent-running Week 1 starting volume of `12 km`.
- valid positive `RecentWeeklyVolumeKm` remains user-derived and unchanged.
- invalid weekly-volume evidence remains a typed failure.
- the selected level remains `INTERMEDIATE`; no Beginner reclassification is performed.
- the peak-band minimum `30 km` is not used as a start default.

Evidence basis: `PRODUCT_PRACTICE_INFORMED`
Decision status: `EXPLICIT_PRODUCT_DEFAULT`

## Cycle Matrix

| Cycle | Missing Start | Missing Peak | Zero Start | Zero Peak | Classification |
|---|---:|---:|---:|---:|---|
| 8 weeks | 16 km | 21.5 km | 12 km | 16 km | `BELOW_TYPICAL_PEAK_BUT_VALID` |
| 12 weeks | 16 km | 25.5 km | 12 km | 19 km | `BELOW_TYPICAL_PEAK_BUT_VALID` |
| 14 weeks | 16 km | 27 km | 12 km | 20.5 km | `BELOW_TYPICAL_PEAK_BUT_VALID` |

All selected peaks remain below the typical `30-42 km` peak band but valid under the reachable-peak rule.

## Feasibility

The policy preserves four run slots: one `KEY_SESSION`, two `EASY_SUPPORT`, and one `LONG_RUN`. Week 1 long-run selection remains within the 30%-36% preferred share and below the 40% hard cap, leaving non-zero residual weekly volume for the other three sessions. No pace, duration, workout segment, or final per-session distance allocation is introduced.

## TD-CORE-READINESS-001

Status: `REMAINS_OPEN`.

This phase closes only the 4F.7B.1 missing/zero weekly-volume starting-rule blocker for the dark prescription-volume planner. It does not close `TD-CORE-READINESS-001`, whose residual concern is broader: readiness resolver wiring and live generation exercise.

## Governance

Append-only entries added:

- `AUD-516`: missing weekly-volume behavior.
- `AUD-517`: explicit-zero weekly-volume behavior.
- `AUD-518`: Intermediate identity, candidate continuation, cycle feasibility, and rejection of peak-band-minimum fallback.

No catalog artifact values, public DTOs, snapshots, hashes, confirmation behavior, persistence, migrations, routing, or publication status changed.
