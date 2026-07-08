# Domain Blocker Version-Cascade Forecast — VERSION-CASCADE-001

Read-only forecast. Nothing was mutated to produce this report. Uses the active, published graph (from
`0.6.0-pilot`'s actual bundle, confirmed in `zero-six-pilot-release-audit.md`):

```
TEN_K__4D__INTERMEDIATE v4
├── masterTemplate      → TEN_K_MASTER v3 → workoutProgression → TEN_K_WORKOUT_PROGRESSION_V1 v2
├── layout               → RUN_LAYOUT_4D v1
├── levelModifier         → INTERMEDIATE_MODIFIER v2 → progressionModifier → INTERMEDIATE_PROGRESSION_MODIFIER_V1 v1
│                                                     → eligibleWorkouts   → [workout definitions]
└── rulePack              → APPSEL_RACE_PLAN_V1 v2 → runtimeConditionValueRegistry → RUNTIME_CONDITION_VALUES_V1 v1
                                                     → peakVolumeBandPolicy          → PEAK_VOLUME_BANDS_V1 v2
```

## Two possible outcomes per decision — critical distinction

Per `docs/README.md` §7/§9 (immutable published artifact rule; exact dependency versioning), **every**
artifact in the closure above is already `PUBLISHED` (as part of `0.6.0-pilot` and earlier releases). This
forecast does **not** assume every audit-classification change requires a new artifact version — the two
possible outcomes of researching any one of the 13 decisions are fundamentally different in cascade impact:

- **Outcome A — confirmation, value unchanged**: research determines the *existing* value is correct.
  Only `PilotDomainContentAudit.cs`'s classification changes (`PLACEHOLDER_UNCONFIRMED` →
  `CANONICAL_CONFIRMED` or `EXPLICIT_PRODUCT_DEFAULT`). This is **audit-only**: no artifact JSON byte
  changes, no content hash changes, no new artifact version, no parent cascade, no new combination
  version, and — because the guard reads `PilotDomainContentAudit` live at publish time — **no new release
  is even required** to make a fresh Production-channel publish of the unchanged `v4` content succeed.
- **Outcome B — correction, value changes**: research determines the existing value is wrong. The leaf
  artifact's JSON content must change, which is a hash-changing edit to an already-published, immutable
  document — this **requires** a new artifact version, triggering the parent-reference cascade described
  per-decision below.

Both outcomes are reported below for every decision, since this task must not choose values yet and
therefore cannot know which outcome will occur.

## Task 6 — per-decision cascade forecast

| Decision ID | Leaf artifact | Audit-only or content-changing? | Next immutable version (if content changes) | Parent references that would change | Expected parent cascade | Root combination new version needed? | New Pilot release required? |
|---|---|---|---|---|---|---|---|
| D1 | `RUN_LAYOUT_4D` v1 | **Either** — Outcome A: audit-only (order confirmed correct or field proven unread). Outcome B: content-changing (new order, or field removed — a schema change) | `RUN_LAYOUT_4D` v2 (Outcome B only) | `v4.layout` | `TEN_K__4D__INTERMEDIATE` v5 (repoint `layout`) | Only under Outcome B | Only under Outcome B (to publish/verify v5) |
| D2 | `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v1 | **Either** | `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v2 (Outcome B only) | `INTERMEDIATE_MODIFIER` v2's `progressionModifier` field | `INTERMEDIATE_MODIFIER` v3 (repoint `progressionModifier`) → `TEN_K__4D__INTERMEDIATE` v5 (repoint `levelModifier`) | Only under Outcome B | Only under Outcome B |
| D3 | `RUNTIME_CONDITION_VALUES_V1` v1 | **Either** | `RUNTIME_CONDITION_VALUES_V1` v2 (Outcome B only) | `APPSEL_RACE_PLAN_V1` v2's `runtimeConditionValueRegistry` field | `APPSEL_RACE_PLAN_V1` v3 (repoint `runtimeConditionValueRegistry`) → `TEN_K__4D__INTERMEDIATE` v5 (repoint `rulePack`) | Only under Outcome B | Only under Outcome B |
| D4 | `PEAK_VOLUME_BANDS_V1` v2 | **Either** | `PEAK_VOLUME_BANDS_V1` v3 (Outcome B only) | `APPSEL_RACE_PLAN_V1` v2's `peakVolumeBandPolicy` field | `APPSEL_RACE_PLAN_V1` v3 (repoint `peakVolumeBandPolicy`) → `TEN_K__4D__INTERMEDIATE` v5 (repoint `rulePack`) — **same RulePack version as D3 if resolved together, see efficiency note below** | Only under Outcome B | Only under Outcome B |
| D5 | `EASY_STANDARD` v2 | **Either** | `EASY_STANDARD` v3 (Outcome B only) | `INTERMEDIATE_MODIFIER` v2's `eligibleWorkouts` entry; possibly `TEN_K_WORKOUT_PROGRESSION_V1` v2's `workoutCandidates` entry if referenced there too | `INTERMEDIATE_MODIFIER` v3 (and/or `TEN_K_WORKOUT_PROGRESSION_V1` v3 → `TEN_K_MASTER` v4) → `TEN_K__4D__INTERMEDIATE` v5 | Only under Outcome B | Only under Outcome B |
| D6 | `EASY_STANDARD` v2 | **Either** | `EASY_STANDARD` v3 (Outcome B only — shared with D5 if both resolve together, see efficiency note) | Same as D5 | Same as D5 | Only under Outcome B | Only under Outcome B |
| D7 | `FARTLEK` v2 | **Either** | `FARTLEK` v3 (Outcome B only) | `INTERMEDIATE_MODIFIER` v2's `eligibleWorkouts` entry; possibly `TEN_K_WORKOUT_PROGRESSION_V1` v2's `workoutCandidates` entry | Same shape as D5 | Only under Outcome B | Only under Outcome B |
| D8 | `FARTLEK` v2 | **Either** | `FARTLEK` v3 (Outcome B only — shared with D7) | Same as D7 | Same as D7 | Only under Outcome B | Only under Outcome B |
| D9 | `LONG_RUN_STANDARD` v2 | **Either** | `LONG_RUN_STANDARD` v3 (Outcome B only) | `INTERMEDIATE_MODIFIER` v2's `eligibleWorkouts` entry **only** (confirmed absent from `workoutCandidates`) | `INTERMEDIATE_MODIFIER` v3 → `TEN_K__4D__INTERMEDIATE` v5 (no `TEN_K_WORKOUT_PROGRESSION_V1`/`TEN_K_MASTER` cascade needed) | Only under Outcome B | Only under Outcome B |
| D10 | `LONG_RUN_STANDARD` v2 | **Either** | `LONG_RUN_STANDARD` v3 (Outcome B only — shared with D9) | Same as D9 | Same as D9 | Only under Outcome B | Only under Outcome B |
| D11 | `THRESHOLD_TEMPO` v2 | **Either** | `THRESHOLD_TEMPO` v3 (Outcome B only) | `INTERMEDIATE_MODIFIER` v2's `eligibleWorkouts` entry; possibly `TEN_K_WORKOUT_PROGRESSION_V1` v2's `workoutCandidates` entry | Same shape as D5 | Only under Outcome B | Only under Outcome B |
| D12 | `THRESHOLD_TEMPO` v2 | **Either** | `THRESHOLD_TEMPO` v3 (Outcome B only — shared with D11) | Same as D11 | Same as D11 | Only under Outcome B | Only under Outcome B |
| D13 | `GOAL_PACE_TEN_K` v1 | **Either** — Outcome A (rare given the evidence gap) or Outcome B, or a third path: removal from the active closure (a `TEN_K_WORKOUT_PROGRESSION_V1`/`INTERMEDIATE_MODIFIER` content change with no new `GOAL_PACE_TEN_K` version at all) | `GOAL_PACE_TEN_K` v2 (Outcome B only) | `TEN_K_WORKOUT_PROGRESSION_V1` v2's `workoutCandidates` entry (RACE_SPECIFIC phase) | `TEN_K_WORKOUT_PROGRESSION_V1` v3 → `TEN_K_MASTER` v4 (repoint `workoutProgression`) → `TEN_K__4D__INTERMEDIATE` v5 | Only under Outcome B (or the removal path) | Only under Outcome B (or the removal path) |

## Efficiency note: batch-level version economy

If multiple decisions within the same batch resolve as Outcome B **together, in one pass**, they collapse
onto a **single** new parent version rather than one per decision:

- D3 + D4 (both consumed by `APPSEL_RACE_PLAN_V1`'s two fields) → **one** `APPSEL_RACE_PLAN_V1` v3, not two.
- D5 + D6 (same `EASY_STANDARD` artifact) → **one** `EASY_STANDARD` v3, not two — this is mechanical (a
  single JSON document can only have one "next version" regardless of how many of its fields change in
  that edit). The same applies to D7+D8, D9+D10, D11+D12.
- Any subset of {D5,D7,D9,D11} and/or {D6,D8,D10,D12} that changes in the same pass still cascades to
  `INTERMEDIATE_MODIFIER` **once** (v3), not once per workout.
- Regardless of how many of the 13 decisions resolve as Outcome B in a given publish wave, the root
  `TEN_K__4D__INTERMEDIATE` needs at most **one** new version (v5) to absorb all of them, provided they are
  authored and published together — not one combination version per decision.

This means the worst-case cascade (all 13 decisions resolve as Outcome B, in one coordinated pass) is
still bounded: `RUN_LAYOUT_4D` v2, `INTERMEDIATE_PROGRESSION_MODIFIER_V1` v2, `RUNTIME_CONDITION_VALUES_V1`
v2, `PEAK_VOLUME_BANDS_V1` v3, `APPSEL_RACE_PLAN_V1` v3, `EASY_STANDARD`/`FARTLEK`/`LONG_RUN_STANDARD`/
`THRESHOLD_TEMPO` v3 each, `GOAL_PACE_TEN_K` v2, `INTERMEDIATE_MODIFIER` v3, `TEN_K_WORKOUT_PROGRESSION_V1`
v3 (only if any of D5/D7/D11/D13 changes and is also referenced there), `TEN_K_MASTER` v4 (only if
`TEN_K_WORKOUT_PROGRESSION_V1` changed), and exactly **one** new `TEN_K__4D__INTERMEDIATE` v5 — a single
Pilot (and eventually Production) release cycle, not 13.

## Batch-level cascade summary

| Batch | Would touch (worst case, Outcome B) | New root version needed? | New Pilot release required? |
|---|---|---|---|
| B1 (D1) | `RUN_LAYOUT_4D` → v5 root | Yes, if content changes | Yes, if content changes |
| B2 (D5,D7,D9,D11) | 4 workout artifacts → `INTERMEDIATE_MODIFIER` (and possibly `TEN_K_WORKOUT_PROGRESSION_V1`/`TEN_K_MASTER`) → v5 root | Yes, if any content changes | Yes, if any content changes |
| B3 (D2) | `INTERMEDIATE_PROGRESSION_MODIFIER_V1` → `INTERMEDIATE_MODIFIER` → v5 root | Yes, if content changes | Yes, if content changes |
| B4 (D6,D8,D10,D12) | Same 4 workout artifacts as B2 (shared versions if run together) → `INTERMEDIATE_MODIFIER` → v5 root | Yes, if any content changes | Yes, if any content changes |
| B5 (D3) | `RUNTIME_CONDITION_VALUES_V1` → `APPSEL_RACE_PLAN_V1` → v5 root | Yes, if content changes | Yes, if content changes |
| B6 (D4) | `PEAK_VOLUME_BANDS_V1` → `APPSEL_RACE_PLAN_V1` (shared with B5 if run together) → v5 root | Yes, if content changes | Yes, if content changes |
| B7 (D13) | `GOAL_PACE_TEN_K` → `TEN_K_WORKOUT_PROGRESSION_V1` → `TEN_K_MASTER` → v5 root | Yes, if content changes (or removal path, still a `TEN_K_WORKOUT_PROGRESSION_V1` content change) | Yes, if content changes |

**Recommendation**: because every batch that changes content converges on the same root version (v5),
resolution work should be **staged into research/decision batches** (per `domain-blocker-resolution-plan.md`'s
execution order) but **published as one combined wave** once all desired resolutions for that wave are
finalized — mirroring exactly how Part 2 bundled multiple technical migrations into one `v4` candidate
rather than publishing a new root version per fix.

## Recommended point to publish the next Pilot release

Not before every decision intended for the next wave has reached a stable classification
(`CANONICAL_CONFIRMED`, `EXPLICIT_PRODUCT_DEFAULT`, or a recorded removal) — publishing partway through a
batch (e.g. only `EASY_STANDARD`'s `complexityTier` resolved but not `components`) would still leave that
same artifact identity blocking Production (both fields live on the same `WORKOUT_DEFINITION/EASY_STANDARD`
error group). A new Pilot release is only worth cutting once a **whole batch** (or several) is complete, to
avoid an unnecessary intermediate root version. If the eventual goal is a **Production** release (not just
another Pilot), it is not reachable until **all 13** decisions across **all 7 batches** are resolved,
because the Production guard rejects the release if `blockingArtifactCount > 0` for the closure regardless
of which specific decisions remain (see `production-readiness-error-contract-audit.md`).

## Final status: version-cascade forecast complete for all 13 decisions and all 7 batches; no artifact was mutated.
