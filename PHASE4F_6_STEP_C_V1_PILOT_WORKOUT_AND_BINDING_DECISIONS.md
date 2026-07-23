# Phase 4F.6 Pre-Implementation — Step C
## V1 Pilot Workout Progression, Fixed Role Binding, and Governance Decisions

Closes Step C of Phase 4F.6 Pre-Implementation for the `TEN_K / INTERMEDIATE / 4D / 12-week` V1 pilot. This is a decision-record and governance-update task: it formalizes accepted product, domain, evidence, and governance decisions so future agents do not reopen them without explicit superseding evidence or a versioned product decision. **No stage scheduler, workout binding service, prescription engine, or runtime code was implemented. No catalog value changed. v10 was not published or activated.**

## Governing architecture

```
Phase → Stage intent → Workout identity → Personalized prescription
```

- **Phase**: FOUNDATION / BUILD / RACE_SPECIFIC / TAPER.
- **Stage**: phase-relative progression intent for `KEY_SESSION` only. Not a full-week slot population mechanism, not a public workout type, not a personalized prescription, not a fixed calendar week.
- **Workout identity**: the selected catalog `WorkoutDefinition` (`EASY_STANDARD`, `LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`).
- **Personalized prescription**: distance/duration/pace/intensity/segments/repetitions/recovery/taper dosage/weekly volume — remains Phase 4F.7's exclusive responsibility.

## Decision classification model

Two independent axes are preserved for every decision:

- **Evidence basis**: `EVIDENCE_BACKED` / `EVIDENCE_INFORMED` / `NO_DIRECT_EVIDENCE` / `NOT_AN_EVIDENCE_QUESTION`
- **Decision status**: `CANONICAL_CONFIRMED` / `EXPLICIT_PRODUCT_DEFAULT` / `TEMPORARY_V1_SIMPLIFICATION` / `DEFERRED` / `TECHNICAL_ONLY`

Note: `PilotDomainContentAudit.cs`'s `ContentDecisionStatus` enum has only 4 values (`CanonicalConfirmed`/`ExplicitProductDefault`/`PlaceholderUnconfirmed`/`TechnicalOnly`) and was **not modified** by this step (enum changes are a code change, out of scope for a decision-record task). Where a decision below carries `TEMPORARY_V1_SIMPLIFICATION`, the closest-fitting enum value (`ExplicitProductDefault`) is used in the C# audit registry, with the simplification nature recorded as an explicit textual tag in that entry's reason string — consistent with this file's existing convention of embedding sub-classifications as text (e.g. `ARTIFACT_VERSION_PARITY_UNRESOLVED`, `WAVE5-CLARIFICATION`).

## Decisions formalized

### D-C01 — Phase distribution
**Retained**: FOUNDATION 3 / BUILD 4 / RACE_SPECIFIC 4 / TAPER 1 (12-week preferred cycle).
Evidence basis: `EVIDENCE_INFORMED`. Decision status: `EXPLICIT_PRODUCT_DEFAULT`. Scope: this V1 pilot only; not claimed universally optimal.
**Governance status**: already recorded with equivalent semantics — `AUD-003` (`CanonicalConfirmed` against the brief and Golden Fixture v3). No PilotDomainContentAudit.cs change required. This document adds the missing evidence-basis axis: Phase 4F.6 Step B (TDE-001/TDE-002) supports the general-to-specific *principle* but not these exact week counts — `AUD-003`'s `CanonicalConfirmed` status reflects fidelity to the brief/fixture, not a scientific claim, and the two are not in conflict.

### D-C02 — Core-cycle bounds
**Retained**: minimum 8 / default 12 / maximum 14 weeks.
Evidence basis: `EVIDENCE_INFORMED`. Decision status: `EXPLICIT_PRODUCT_DEFAULT`.
**Governance status**: already recorded — `AUD-001` (`CanonicalConfirmed` against the brief and fixture). No change required, same reasoning as D-C01.

### D-C03 — Stage exposure values
**Retained unchanged** (all 14 fields: 7×`MinimumExposures`, 7×`MaximumExposures`).
Evidence basis: `NO_DIRECT_EVIDENCE` (repeated/progressive exposure supported in principle; exact counts are not). Decision status: `EXPLICIT_PRODUCT_DEFAULT`.
**Governance change**: `AUD-014` (Placeholder, scoped only to the immutable `WORKOUT_PROGRESSION v1` artifact) is superseded for the *current* `v5` artifact by new entry **`AUD-500`** (`ExplicitProductDefault`). Numeric values unchanged; `AUD-014` itself is left untouched (historical, immutable-version record).

### D-C04 — Compression and extension behavior
**Retained unchanged** (all 14 fields).
Evidence basis: `EVIDENCE_INFORMED` for `GOAL_PACE_REHEARSAL`/`TAPER_SHARPEN`'s `PROTECTED`/`FIXED_EXPOSURE` values (specificity-protection principle); `NO_DIRECT_EVIDENCE` for the remaining `COMPRESSIBLE`/`EXTENDABLE` assignments. Decision status: `EXPLICIT_PRODUCT_DEFAULT`.
**Governance change**: `AUD-015` (Placeholder, `v1` only) superseded for `v5` by new entry **`AUD-501`** (`ExplicitProductDefault`). No `ExtensionPriority`, `StageDistributionBehavior`, or tie-break field was added — those remain a Phase 4F.6A concern.

### D-C05 — One-week taper
**Retained unchanged**: TAPER phase fixed at 1 week.
Evidence basis: `EVIDENCE_INFORMED`. Decision status: `EXPLICIT_PRODUCT_DEFAULT`.
**Evidence tension recorded, not resolved**: Bosquet et al. (2007) found ~2 weeks the most efficient taper window in the analyzed competitive-athlete evidence; this does not directly prove a 10K/intermediate/12-week Appsel plan must use two taper weeks (population and distance extrapolation both required). The 1-week value remains an accepted V1 product default. **Governance change**: new traceability entry **`AUD-506`** (`Technical`, does not reclassify or reopen `AUD-003`). No phase week count was changed.

### D-C06 — Stage semantics
**Canonically confirmed**: for this V1 pilot, `WorkoutProgressionStageDefinition` governs `KEY_SESSION` progression intent only. It does not populate the entire weekly layout and does not directly assign workouts to `EASY_SUPPORT` or `LONG_RUN`.
Evidence basis: `NOT_AN_EVIDENCE_QUESTION` (accepted Appsel domain interpretation, not a sports-science claim). Decision status: `CANONICAL_CONFIRMED`.
Supporting references: `WorkoutProgressionStageDefinition.cs` has no `Role`/`SlotRole`/`StructuralRole` field (direct source confirmation); Step A.1 (`A1-Q01`/`A1-Q04`) found no role-to-workout binding mechanism anywhere; Step A.2 found incomplete weekly role coverage (BUILD/RACE_SPECIFIC have zero EASY-family stages) and zero LONG_RUN-family stages anywhere in the progression.
**Governance change**: new entry **`AUD-502`** (`Confirmed`).

### D-C07 — V1 EASY_SUPPORT binding
**Fixed for V1**: `EASY_SUPPORT → EASY_STANDARD`.
Evidence basis: `EVIDENCE_INFORMED`. Decision status: `EXPLICIT_PRODUCT_DEFAULT` + `TEMPORARY_V1_SIMPLIFICATION`.
> This is not a permanent architectural restriction. It limits the V1 pilot to one accepted canonical workout identity for `EASY_SUPPORT` while preserving future versioned expansion.

**Governance change**: new entry **`AUD-503`** (`ExplicitProductDefault`, tagged `[TEMPORARY_V1_SIMPLIFICATION]` in its reason text).

### D-C08 — V1 LONG_RUN binding
**Fixed for V1**: `LONG_RUN → LONG_RUN_STANDARD`.
Evidence basis: `EVIDENCE_INFORMED`. Decision status: `EXPLICIT_PRODUCT_DEFAULT` + `TEMPORARY_V1_SIMPLIFICATION`.
> This is not a permanent architectural restriction. Future catalog versions may introduce long-run variants or selection policies without changing historical published-plan behavior.

`LONG_RUN_PROGRESSION` is explicitly **not** added. **Governance change**: new entry **`AUD-504`** (`ExplicitProductDefault`, tagged `[TEMPORARY_V1_SIMPLIFICATION]`).

### D-C09 — KEY_SESSION binding
**For V1**: `KEY_SESSION → stage-controlled workout candidate resolution`, distinct from `EASY_SUPPORT`/`LONG_RUN`'s fixed defaults.
Evidence basis: `NOT_AN_EVIDENCE_QUESTION`. Decision status: `CANONICAL_CONFIRMED`. The exact stage scheduler and candidate-resolution algorithm remain a Phase 4F.6A task.
**Governance change**: new entry **`AUD-505`** (`Confirmed`).

### D-C10 — Future-extensible binding contract requirement
Recorded requirement (not implemented): the future role-binding contract must model V1 fixed defaults explicitly while allowing versioned expansion to multiple eligible workout definitions. It must not rely on list position, "pick the first item," dictionary iteration, the assumption a role permanently has one workout, or accidental uniqueness in the current eligible set. Must support concepts equivalent to `FIXED_DEFAULT`/`STAGE_CONTROLLED` (without requiring these exact enum names now). A future multi-workout role requires an explicit versioned selection policy before activation.
Evidence basis: `NOT_AN_EVIDENCE_QUESTION`. Decision status: `CANONICAL_CONFIRMED`.
**Governance change**: none in `PilotDomainContentAudit.cs` — this is a forward-looking contract requirement for not-yet-built code, not a fact about an existing artifact/field. Recorded here as the authoritative statement of intent for Phase 4F.6A/4F.6B designers.

### D-C11 — TAPER_SHARPEN
**Retained**: `stageKey=TAPER_SHARPEN`, workout identity `EASY_STANDARD`. No new taper workout key introduced.
The sharpening effect must be produced in **Phase 4F.7** via a taper-specific prescription modifier (may reduce total workload while preserving an appropriate intensity stimulus, using only components/prescription modes already allowed by `EASY_STANDARD` and the future prescription contract — not a generic "faster easy pace"; segments/pace/repetitions/recovery are not defined here).
> Stage context must be available to Phase 4F.7 prescription generation so that `TAPER_SHARPEN` and ordinary `EASY_STANDARD` do not receive identical prescriptions by accident.

Evidence basis: `EVIDENCE_INFORMED`. Decision status: `CANONICAL_CONFIRMED`. Implementation owner: **Phase 4F.7**.
**Governance change**: new entry **`AUD-507`** (`Confirmed`).

### D-C12 — EASY variants
**Not added in V1**: `EASY_SHAKEOUT`, `EASY_WITH_STRIDES`. Remain deferred vocabulary/product decisions for a later catalog version. This does not assert these variants are invalid.
Decision status: `DEFERRED`. No `PilotDomainContentAudit.cs` entry (no existing field/artifact to classify — this is an absence, not a field value).

### D-C13 — EASY_SHAKEOUT activation risk
**Added**: `TD-EASY-WORKOUT-REGISTRY-001` to `activation-readiness-risks.json`/`.md`.
Finding: Tier-1 Golden Fixture v3 references `EASY_SHAKEOUT`, but no validated, versioned `EASY_SHAKEOUT` workout definition exists in the resolved v10 bundle. Category: `ACTIVATION_READINESS / CATALOG_FIXTURE_DIVERGENCE`. Current runtime impact: none. Blocking: `NON_BLOCKING_FOR_STEP_B`, `BLOCKER_FOR_4F6B_COMPLETION`, `BLOCKER_FOR_PUBLICATION_IF_UNRESOLVED`. Resolution owner: future product/schema/catalog decision. Closure criteria: (1) add a validated, versioned definition + explicit binding/policy; (2) revise the fixture to use accepted catalog keys; or (3) reclassify the fixture field as illustrative/non-canonical. `EASY_SHAKEOUT` was **not** added to the catalog by this step.

### D-C14 — Public workout-type mapping
**Deferred to Phase 4F.8**: catalog workout key → public/UI workout type mapping. No mapping (`FARTLEK→INTERVAL`, `THRESHOLD_TEMPO→TEMPO`, `GOAL_PACE_TEN_K→RACE_PACE`, etc.) is created by this step.
Evidence basis: `NOT_AN_EVIDENCE_QUESTION`. Decision status: `DEFERRED`.

### D-C15 — Prescription ownership
**Confirmed**: distance, duration, pace, intensity, segment structure, repetition count, recovery, weekly volume, long-run progression, taper dosage, and user-fitness personalization all remain **Phase 4F.7** responsibilities.
Evidence basis: `NOT_AN_EVIDENCE_QUESTION`. Decision status: `CANONICAL_CONFIRMED`.

## Governance updates made

| File | Change |
|---|---|
| `plan-catalog/src/PlanCatalog.Core/Audit/PilotDomainContentAudit.cs` | Added `AddStepCV1PilotBindingGovernanceEntries` with 8 new entries: `AUD-500` (exposure counts, D-C03), `AUD-501` (compression/extension, D-C04), `AUD-502` (stage semantics, D-C06), `AUD-503` (EASY_SUPPORT binding, D-C07), `AUD-504` (LONG_RUN binding, D-C08), `AUD-505` (KEY_SESSION binding, D-C09), `AUD-506` (TAPER evidence-tension traceability, D-C05), `AUD-507` (TAPER_SHARPEN retention, D-C11). No existing entry was modified or removed; no numeric/structural catalog value changed. |
| `plan-catalog/artifacts/audits/activation-readiness-risks.json` | Added `TD-EASY-WORKOUT-REGISTRY-001` (D-C13). Risk count 7→8, all still `OPEN`. |
| `plan-catalog/artifacts/audits/activation-readiness-risks.md` | Same addition, human-readable form. |
| `evidence-log.json`/`.md` | **Not modified** — its schema/scope (4 runtime-resolver thresholds) does not require a change for this step, per explicit instruction. |
| Canonical pilot decision document(s) | Inspected (`docs/canonical/appsel-v1-canonical-decisions.md`); no change required — it does not cover workout-progression/role-binding content, and this document (`PHASE4F_6_STEP_C_...md`) is the authoritative record for these new decisions. |

## Reclassification integrity

| Field | Prior status | New status | Value changed? | Approving reference |
|---|---|---|---|---|
| 7× `minimumExposures` / 7× `maximumExposures` (WORKOUT_PROGRESSION v5) | `PLACEHOLDER_UNCONFIRMED` (`AUD-014`, v1 only) | `EXPLICIT_PRODUCT_DEFAULT` (`AUD-500`, v5) | **NO** | D-C03 |
| `compressionBehavior` / `extensionBehavior` (7 stages, WORKOUT_PROGRESSION v5) | `PLACEHOLDER_UNCONFIRMED` (`AUD-015`, v1 only) | `EXPLICIT_PRODUCT_DEFAULT` (`AUD-501`, v5) | **NO** | D-C04 |

`AUD-014`/`AUD-015` themselves are untouched (historical, immutable-version records, per this repository's established convention — see `AUD-049`/`AUD-050`/`AUD-056`/`AUD-057`'s precedent of leaving superseded-version entries unchanged).

## Explicit statement: V1 fixed bindings are temporary simplifications

`EASY_SUPPORT → EASY_STANDARD` (D-C07) and `LONG_RUN → LONG_RUN_STANDARD` (D-C08) are **temporary V1 simplifications**, not permanent architectural restrictions. Future catalog versions may introduce additional eligible workouts, variant identities, or an explicit selection policy for either role without changing historical published-plan behavior (D-C10's future-extensible contract requirement governs how that must be designed when it happens).

## Next-step boundaries

Not implemented by this step: role-binding service/schema/artifact, stage scheduler, workout selector, workout materializer, prescription engine, preview payload mapper. The next implementation target (Phase 4F.6A, stage-scheduler design) consumes the decisions formalized here as its fixed contract target.

## Prohibited actions confirmed not taken

No phase week counts, core-cycle bounds, exposure numbers, stage order, workout candidate lists, or compression/extension values were changed. No workout definitions were added (`EASY_SHAKEOUT`, `EASY_WITH_STRIDES`, `LONG_RUN_PROGRESSION` all remain absent from the catalog). No role binding, stage scheduling, or prescription logic was implemented. No public workout-type mapping was created. Preview payload, snapshot/hash, confirm, and persistence are unchanged. No migration was created. v10 was not published or activated. No commit was made.
