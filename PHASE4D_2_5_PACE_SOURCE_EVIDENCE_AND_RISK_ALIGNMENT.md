# Backend Integration Phase 4D.2.5 — Pace Source Evidence/Risk Alignment

Documentation, evidence-log, and activation-risk alignment pass. No resolver behavior was changed. No new
public API field was added. No registry, golden fixture, or plan-catalog artifact was modified. No resolver
was wired into live generation.

## 1. AsOfDate elevated into the evidence log — `EV-007`

Phase 4D.2's `AsOfDate` deviation (using an explicit evaluation-date field instead of `StartDate`, based on
direct golden-fixture inspection) was previously documented only inside
`PHASE4D_2_PACE_SOURCE_RESOLVER_IMPLEMENTATION.md`. It is now also recorded as `EV-007` in
`plan-catalog/docs/evidence-log.json`/`.md`, `qualityLabel = INTERNAL_CANONICAL_DECISION`,
`status = ACCEPTED_AS_SUPPORTING_EVIDENCE` (same status tier as `EV-006`, the closest existing precedent for
an accepted internal-evidence entry — `evidence-log.md`'s allowed-status list has no
"ACCEPTED_AS_INTERNAL_CANONICAL_SOURCE" option, confirmed by direct re-read, same finding as Phase 4A.3).
Source cited verbatim from the fixture, no bibliographic detail invented:
`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`, `INPUT_SNAPSHOT`
step (`confirmDate="2026-07-30"`, `planStartDate="2026-08-03"` — two distinct fields) and `PACE_CONVERSION`
step (`ageReferenceDate="2026-07-30"`, matching `confirmDate`, not `planStartDate`).

## 2. AsOfDate semantics — clarified explicitly

- **`AsOfDate` is not `DateTime.UtcNow` inside the resolver.** Confirmed by direct source inspection: neither
  `PaceSourceResolver.cs` nor `RuntimeResolverContext.cs` contains a `DateTime.UtcNow`/`DateTime.Now` call
  anywhere in executable code (the only occurrence of `DateTime.UtcNow` in either file is inside a doc
  comment, explicitly stating the resolver does *not* do this).
- **The resolver never reads wall-clock time.** `PaceSourceResolver.Resolve` takes `AsOfDate` exclusively
  from `RuntimeResolverContext.AsOfDate`, supplied by the caller. If absent, recency metadata is omitted
  (`"NOT_COMPUTED_NO_REFERENCE_DATE"`) rather than falling back to any clock read — this was already true
  as of Phase 4D.2 and is unchanged by this pass.
- **`AsOfDate` is an explicit evaluation date supplied through `RuntimeResolverContext`** — a plain
  `DateOnly?` property, populated only by whatever calls `Resolve` (today: only tests).
- **Phase 4D.2 only added the context field; live preview/confirm lifecycle wiring is not implemented.**
  Confirmed by repository-wide search: no file under `RunningApp.Api`, `RunningApp.Application/Services`, or
  `RunningApp.Application/PlanGeneration` constructs a `RuntimeResolverContext` or references `AsOfDate` —
  the only constructors of `RuntimeResolverContext` in the entire repository are inside test files
  (`PaceSourceResolverTests.cs`, `TimeAdequacyResolverTests.cs`, `ResolverContractTests.cs`,
  `ContractOnlyFakeResolutionServiceTests.cs`).

## 3. Preview/confirm consistency — `DECISION_REQUIRED`, tracked as `TD-PACESOURCE-002`

No repository evidence defines how a future live-wired resolver should source `AsOfDate` across the
preview → confirm lifecycle. Two plausible designs exist and neither is evidenced as chosen:

- **Preview computes `AsOfDate` once; confirm reuses the same value** (consistency: a confirmed plan's
  recorded pace-evidence recency matches exactly what the user saw in preview).
- **Confirm recomputes `AsOfDate` independently** (freshness: recency reflects any delay between preview
  and confirm, which — per the existing `PlanPreview.ExpiresAt` 30-minute window already in
  `PlanServices.GeneratePreviewAsync` — could be up to ~30 minutes, and today's UI/product flow duration is
  `UNKNOWN_FROM_REPO_EVIDENCE` beyond that expiry bound).

Per the task's explicit instruction, this is **not guessed** — it is recorded as `DECISION_REQUIRED` here
and tracked durably as activation risk `TD-PACESOURCE-002` (§5 below), rather than silently choosing one
design or defaulting to wall-clock time when wiring eventually happens.

## 4. Activation risk — `TD-PACESOURCE-001`: `ESTIMATED` never emitted

Added to `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`. Restates, as a durable
tracked risk (not just narrative documentation), the Phase 4D.2 finding: `PACE_SOURCE_IN.ESTIMATED` is
registry-valid but `PaceSourceResolver` never produces it, because no approved estimation methodology from
training-volume evidence (`recentWeeklyVolumeKm`/`recentLongestRunKm`/`recentRunsPerWeek`) was found
anywhere in the repository. A user with only training-volume evidence (no recent race, no target time)
resolves to `NONE` today. `classification = DEFERRED_REGISTRY_VALUE_NOT_EMITTED`,
`severity = ACTIVATION_RISK`, `status = OPEN` — **not closed by this pass**. Resolution requires one of:
approving and implementing a real estimation methodology; explicitly deciding V1 will never use
`ESTIMATED`; or confirming product/UX accepts `NONE` for this population.

## 5. Activation risk — `TD-PACESOURCE-002`: `AsOfDate` lifecycle unwired

Added to the same risk file, `severity = ACTIVATION_RISK`, `status = OPEN`. Restates §3 above as a tracked,
durable risk: the preview/confirm `AsOfDate`-sourcing decision is undecided, and until it is, no future
phase should silently wire `AsOfDate` to `DateTime.UtcNow` or to any other guessed value without an
explicit product decision.

## 6. Test results

No resolver code was touched in this pass. Full suite re-run to confirm zero behavioral drift:
**227/227 tests passing** (unchanged from Phase 4D.2's own final count — no test was added, removed, or
modified).

## 7. Non-actions confirmed

- `PaceSourceResolver`'s output behavior is unchanged — the file was not edited in this pass.
- `ESTIMATED` remains unimplemented; no estimation methodology was invented or added.
- No `AsOfDate` lifecycle wiring was implemented — `RuntimeResolverContext`/`PaceSourceResolver` source
  files were not edited.
- No new public API field was added.
- No runtime registry value was changed — `runtime-condition-values.v2.json` was read-only inspected.
- No golden fixture was changed — the golden-fixture-v3 files were read-only inspected.
- No plan-catalog artifact was changed except the two evidence-log files and the two activation-readiness-risk
  files, all under `plan-catalog/docs/` and `plan-catalog/artifacts/audits/` respectively — no `catalog/`,
  `src/`, or `tests/` file was touched.
- No resolver was wired into live generation.
- No generation, `TrainingWeek`, or `TrainingDay` was implemented or created.
- `TD-REGISTRY-001` remains `OPEN`, untouched by this pass.
- `EV-005` remains `PROPOSED`, untouched. `EV-006` remains `ACCEPTED_AS_SUPPORTING_EVIDENCE`, untouched.

## Final report

**1. Files inspected:** `PHASE4D_2_PACE_SOURCE_RESOLVER_IMPLEMENTATION.md`;
`docs/canonical/golden-fixture-v3/golden-10k-intermediate-4d-12w.v3.decisiontrace.json`
(`INPUT_SNAPSHOT`/`PACE_CONVERSION` steps, re-verified directly); `plan-catalog/docs/evidence-log.json`/`.md`;
`plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`;
`catalog/registries/runtime-condition-values.v2.json`; `PHASE4A_2`/`PHASE4A_3` docs;
`PHASE4B_RUNTIME_INPUT_CONTRACT_FOR_FITNESS_EVIDENCE.md`;
`backend/RunningApp.IntegrationTests/RuntimeCatalog/Resolvers/PaceSourceResolverTests.cs`;
`backend/RunningApp.Application/RuntimeCatalog/Resolvers/RuntimeResolverContext.cs` and
`PaceSourceResolver.cs` (re-inspected, not modified).

**2. Files changed:** `plan-catalog/docs/evidence-log.json`/`.md` (EV-007 appended);
`plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md` (`TD-PACESOURCE-001`,
`TD-PACESOURCE-002` appended, `finalStatus` updated); new file
`PHASE4D_2_5_PACE_SOURCE_EVIDENCE_AND_RISK_ALIGNMENT.md` (this document). No backend source file was
changed.

**3. EV-007:** Added — `INTERNAL_CANONICAL_DECISION`, `ACCEPTED_AS_SUPPORTING_EVIDENCE`, sourced from the
golden fixture's `INPUT_SNAPSHOT`/`PACE_CONVERSION` steps, no invented bibliographic detail.

**4. AsOfDate semantics:** Documented in §2 above — explicit `RuntimeResolverContext` field, never the wall
clock, not yet wired into any live request path.

**5. Wall-clock vs. explicit context:** Explicit context field, confirmed by direct source inspection —
zero `DateTime.UtcNow`/`DateTime.Now` calls in either `PaceSourceResolver.cs` or
`RuntimeResolverContext.cs`'s executable code.

**6. Preview/confirm consistency:** `DECISION_REQUIRED` — no repository evidence resolves whether confirm
should reuse or recompute `AsOfDate`. Tracked as `TD-PACESOURCE-002`.

**7–8. TD-PACESOURCE-001:** Added, `status = OPEN`, `severity = ACTIVATION_RISK`. Resolution condition:
approve/implement a V1 estimation methodology, or explicitly decide against `ESTIMATED` for V1, or confirm
product accepts `NONE` for training-volume-only users — not closed in this pass.

**9. TD-PACESOURCE-002:** Added (not merely documented as `DECISION_REQUIRED` in prose alone) — consistent
with the existing risk-taxonomy pattern already used for `TD-REGISTRY-001`, `status = OPEN`.

**10–19.** Confirmed: `ESTIMATED` behavior unchanged; `PaceSourceResolver` behavior unchanged (file
untouched); no public API field added; no registry changes; no golden fixture changes; no plan-catalog
artifact changes beyond the two evidence/risk doc pairs; no live generation wiring; no generation/
`TrainingWeek`/`TrainingDay`; `TD-REGISTRY-001` remains `OPEN`; `EV-005`/`EV-006` unchanged.

**20. Full test results:** 227/227 passing, unchanged from Phase 4D.2.

**21. Final classification:** `BACKEND_HAS_PACE_SOURCE_EVIDENCE_AND_RISK_ALIGNMENT_NOT_WIRED_TO_GENERATION`.

**22. Anything not completed exactly as specified:** None — all six required-work items (evidence-log entry,
AsOfDate semantics clarification, `TD-PACESOURCE-001`, `TD-PACESOURCE-002`, test re-run, non-actions
confirmation) were completed as scoped.
