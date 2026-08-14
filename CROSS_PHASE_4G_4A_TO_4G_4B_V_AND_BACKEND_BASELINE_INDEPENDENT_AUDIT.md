# Cross-Phase Independent Audit — Preparation Runway (4G.4A–4G.4B.V) and Backend Baseline

**Independent, read-only audit. No production/test/config/catalog file
changed. No TD created or modified. No fix applied. No commit or push.**

---

## 1. Executive verdict

All five passes under review are, on independent re-verification, materially
accurate. No unattributed production change, no live wiring, no unapproved
route/prescription logic, and no date/partial-span invariant defect was
found. One genuine (but low-severity, currently-inert) validation gap was
found by this audit's own adversarial source trace, not by the prior passes.
The two backend DI failures are confirmed `STALE_TEST_EXPECTATION` /
`MULTIPLE_CONCERNS_IN_ONE_TEST` from source, and the 12 reset-endpoint
failures reported by 4G.4B.V did **not** reproduce in either of two fresh
full-suite runs performed in this pass — confirming `TRANSIENT_NOT_REPRODUCED`
from this pass's own evidence, not merely from reading the prior reports'
claims.

## 2. HEAD and working-tree attribution

HEAD: `3549a8a1eeef18ca96794fa1056043142d13bc78` (`docs(catalog): clarify
GoalPaceReachabilityVerifier measures theoretical completeness, not runtime
safety`). `git diff --check` returned exit 0 with only pre-existing
LF→CRLF working-copy warnings. `git log --oneline -20` shows no commit
past `3549a8a` — **all five passes under review, and everything since, are
uncommitted working-tree state**, consistent with the mandatory
uncommitted-work warning: `git log -S`/`blame` would show nothing for any
of this work, which is expected and is not evidence against it. Verified
instead from actual file contents and `git status --short`/`git diff`, as
required.

## 3. Five-pass chronology

| Phase/pass | HEAD at start | Files created | Files modified | Committed? | Reported purpose |
|---|---|---|---|---:|---|
| 4G.4A | `3549a8a` | `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md` | none | No | Canonical-source reconciliation audit for the Preparation Runway model |
| 4G.4B | `3549a8a` | `PHASE4G_4B_PREPARATION_RUNWAY_TYPED_CONTRACTS.md`, `PreparationRunwayContracts.cs`, `PreparationRunwayValidators.cs`, `PreparationRunwayContractsTests.cs` | none (4G.4A doc itself unmodified in content, per 4G.4B's own §14 claim — see qualification below) | No | Neutral typed contracts + structural validators, dark/unwired |
| 4G.4B.V | `3549a8a` | `PHASE4G_4B_V_PREPARATION_RUNWAY_CURRENT_STATE_VALIDATION.md` | none | No | Independent current-state validation of 4G.4A/4G.4B |
| Reset/DI baseline stabilization | `3549a8a` | `BACKEND_TEST_BASELINE_STABILIZATION_RESET_AND_DI_FIX.md` | none | No | Attempt to fix reset+DI failures; stopped at mandatory pre-check mismatch |
| DI configuration-intent | `3549a8a` | `BACKEND_DI_BASELINE_RESOLUTION_CATALOG_LIVE_PILOT.md` | none | No | Independent DI/config-intent investigation for the remaining 2 failures |

**Qualification on 4G.4B's "renamed... without substantive edits" claim
(§14 of that document):** no prior-name file or git history exists to diff
against (all uncommitted, per the warning above), so this specific
sub-claim cannot be verified by diff. It is corroborated only indirectly:
the current `PHASE4G_4A_...md` content is internally self-consistent, uses
terminology 4G.4B/4G.4B.V both reference correctly (e.g. the exact
`AEROBIC_STRENGTH_LIGHT` interpretation-(a) conclusion, the exact
`RunningExperience`/`RunningBackground` mismatch framing), and no
discrepancy between what 4G.4B describes 4G.4A as concluding and what
4G.4A's current text actually says was found (see Part 5 below). Classified
`INHERITED_UNVERIFIED` for the rename-mechanics claim specifically, not
`CONFIRMED`.

**No additional interim/partial document was discovered.** Repository-wide
search for other `PHASE4G_4*`/`BACKEND_*` documents found only the five
already listed plus this audit's own new file.

Every changed/untracked source, test, or document file relevant to these
five passes is attributed:

| File | Attribution |
|---|---|
| `PHASE4G_4A_...md`, `PHASE4G_4B_...md`, `PHASE4G_4B_V_...md` | `PHASE_4G_4A` / `PHASE_4G_4B` / `PHASE_4G_4B_V` respectively |
| `BACKEND_TEST_BASELINE_STABILIZATION_RESET_AND_DI_FIX.md` | `RESET_DI_BASELINE_INVESTIGATION` |
| `BACKEND_DI_BASELINE_RESOLUTION_CATALOG_LIVE_PILOT.md` | `DI_CONFIGURATION_INTENT_INVESTIGATION` |
| `backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunway/*.cs` | `PHASE_4G_4B` |
| `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/PreparationRunway/*.cs` | `PHASE_4G_4B` |
| `PHASE4G_3B_0_..md`, `VolumeSafetyPolicy.cs`, `VolumeProgressionVerifier.cs`, `PHASE4G_3B_7_...md`, `PHASE4G_3B_8_...md` | `PRE_EXISTING_OTHER_WORK` (Phase 4G.3B.7/3B.8, already completed and reported in a prior pass, unrelated to this audit's five targets) |
| `activation-readiness-risks.{json,md}`, `ten-k-pilot-domain-decision-audit.{json,md}` | `PRE_EXISTING_OTHER_WORK` |
| `backend/**/bin/**`, `backend/**/obj/**` | `GENERATED_ARTIFACT` |
| `LOCAL_CATALOG_ACCEPTANCE_TEST.md`, `backend/calendar_july.json`, `backend/confirm_response.json`, `backend/preview_response.json`, `baseline_tmp/`, `docker-compose.yml` | `LOCAL_ONLY` |
| `design-references/*.png` | `LOCAL_ONLY` |

**No `UNATTRIBUTED` production or test file was found.** No blocker.

No report's "untouched" claim was found to be false: each of the five
passes' own "files not changed" lists were spot-checked against
`git status --short` and match exactly — none of the pre-existing dirty
files (Phase 4G.3B.7/3B.8, governance JSON/MD pairs) shows any diff
attributable to a later pass; each pass's own diff is confined to the files
it claims to have created.

---

## 4. Phase 4G.4A findings

| 4G.4A conclusion | Evidence quoted | Classification |
|---|---|---|
| doc13 §4/5/7/9/10 are not recoverable from the current repo copy of `appsel-v1-canonical-decisions.md` | Direct grep of that file's headings in this pass: `## Status of this document`, `## Location rationale`, `## A...`, `## B...`, `## C...`, `## V1 Runtime Scope...`, `## D...` — no §4/5/7/9/10 exists. Confirmed independently in this pass (not merely re-reading 4G.4A's own claim). | `CONFIRMED` |
| `RunningExperience.New` (plan-catalog) is an exact lexical match; `RunningBackground.Beginner` (backend) is not | 4G.4A quotes both enums verbatim (`New, Intermediate, Advanced, Experienced` vs. `Beginner, Intermediate, Advanced, Experienced`) | `CONFIRMED` — plan-catalog enum location and members not independently re-verified in this pass (out of scope for this backend-focused audit), but the backend enum name/members (`RunningBackground`) match what this session's earlier `PlanCatalogDomainMapper` work already established | `CONFIRMED_WITH_QUALIFICATION` |
| Race-core phases (`FOUNDATION`/`BUILD`/`RACE_SPECIFIC`/`TAPER`) and runway blocks (`CONSISTENCY`/`GENERAL_ENDURANCE`/`AEROBIC_STRENGTH`/`PRE_SPECIFIC_TRANSITION`) are kept as two separate families with no shared `Phase` base type | Verified directly in `PreparationRunwayContracts.cs` (read in full, this pass): `PreparationRunwayBlockType` is a standalone `enum`, no base type, no reference to `FOUNDATION`/`BUILD`/etc. anywhere in the file | `CONFIRMED` — `VERIFIED_FROM_SOURCE` |
| `AEROBIC_STRENGTH_LIGHT` resolved as interpretation (a) — a prescription-profile distinction on `AerobicStrength`, not an independent block, and marked `DECISION_REQUIRED` | Directly confirmed against `PreparationRunwayContracts.cs`: `PreparationRunwayPrescriptionProfile { Standard, Light }`, and the allocation validator (`PreparationRunwayValidators.cs:55-56`) enforces `Light` is only valid paired with `AerobicStrength` | `CONFIRMED` |
| Deterministic preferred-core rule: `coreWeeks` fixed at preferred (12), never a contextual preferred–maximum range | `ten-k-master.v6.json` re-read in this pass (Phase 4G.3B.7/4G.3B.8 already independently confirmed the same phase sums: FOUNDATION 2/3/4, BUILD 3/4/5, RACE_SPECIFIC 2/4/4, TAPER 1/1/1, preferred sum 12) | `CONFIRMED` |
| Inclusive day-based arithmetic (`coreStartDate = raceDate.AddDays(-(coreDays-1))`) | Present verbatim in both `PHASE4G_4A_...md` and, independently, in `PreparationRunwayValidators.cs:19` (`RaceDate.AddDays(-((PreferredCoreWeeks*7)-1))`) — the code and the doc agree | `CONFIRMED` |
| 11+-week maximum-exhaustion is realistically reachable, not merely a theoretical edge case | 4G.4A's own arithmetic (Consistency 8 + General Endurance 8 + Aerobic Strength 8 = 24 max for New-long; 21 for Intermediate; 13 for Advanced/Experienced as listed) is internally consistent with the supplied §9.1 maxima it quotes; this audit did not re-verify the §9.1 numbers themselves against any external source, because 4G.4A already establishes those numbers are `PRODUCT_DEFAULT`/`IMPORT_CANDIDATE`, not independently recoverable from the repo | `CONFIRMED_WITH_QUALIFICATION` — the arithmetic is correct given the stated inputs; the inputs themselves are acknowledged-unverifiable imports, exactly as 4G.4A itself already states |
| Advanced/Experienced route lists only two blocks while the 11+ rule requires three — an unresolved contradiction, not silently resolved | 4G.4A §5.1 states this explicitly and does not invent a third block | `CONFIRMED` |
| Missing block contracts / missing evidence classifications | 4G.4A §6 fully populates the evidence/governance table requested; no cell was left blank or unclassified | `CONFIRMED` |

Special-attention items:

- **`New == Beginner`**: NOT asserted as equal by 4G.4A — explicitly flagged
  `DECISION_REQUIRED`. `CONFIRMED`.
- **`AEROBIC_STRENGTH_LIGHT`**: resolved to interpretation (a), explicitly
  still `DECISION_REQUIRED` pending §9.1 confirmation. `CONFIRMED`.
- **preferred core vs. preferred–maximum**: fixed-at-preferred rule stated
  unambiguously, no contextual range introduced. `CONFIRMED`.
- **11+ three-block routing**: contradiction flagged, not resolved.
  `CONFIRMED`.
- **Advanced/Experienced route capacity**: contradiction flagged explicitly
  in §5.1, not silently patched. `CONFIRMED`.

**Required verdict: `PHASE_4G_4A_ACCURATE`.**

---

## 5. Phase 4G.4B findings

### 5.0 Cross-phase consistency with 4G.4A (performed first, as required)

| 4G.4A decision/open question | 4G.4B's actual behavior | Verdict |
|---|---|---|
| New/Beginner mapping left `DECISION_REQUIRED`, no mapper | `PreparationRunwayExperienceReference` carries a vocabulary discriminator + raw string; no mapper exists anywhere in `PreparationRunwayContracts.cs`/`PreparationRunwayValidators.cs` (confirmed by direct read) | **Followed exactly** — no deviation |
| `AEROBIC_STRENGTH_LIGHT` = interpretation (a), `DECISION_REQUIRED` | `PreparationRunwayPrescriptionProfile.Light` exists, constrained to `AerobicStrength` only by the validator, but nothing selects it automatically | **Followed exactly** |
| Deterministic-preferred-core rule | `PreparationRunwayContext.PreferredCoreWeeks` is a plain caller-supplied `int`; `PreparationRunwayContextValidator` enforces `CoreStartDate` consistency against whatever value is supplied, but does not itself hard-code "12" or enforce it must equal the catalog's own preferred value | **Left exactly as open as 4G.4A left it** — this is a contract/validator, not an allocator, so it correctly does not yet enforce the *specific* value 12; it enforces only the *arithmetic relationship* given whatever value is supplied. No silent deviation — this is the expected shape of a "neutral contract," and 4G.4A's own §9 anticipates exactly this staging. |
| Day-based/inclusive date arithmetic | `PreparationRunwayValidators.cs:19` reproduces the exact formula from 4G.4A §4 | **Followed exactly** |
| Partial-span-does-not-reduce-full-week-allocation | `PreparationRunwayAllocationValidator.cs:51` (`Block full-week sum must equal FullRunwayWeeks; partial days cannot compensate...`) and test `Allocation_PartialDaysCannotReduceFirstBlocksFullWeeks` | **Followed exactly, and executably tested** |
| Deferred personalization scope (readiness/volume/longest-run) | `PreparationNeedProfile` exists as a carrier record with six `NeedLevel` fields, all `NotEvaluated`/`Low`/`Moderate`/`High` — confirmed no formula, no consumer, no route-selection code references it anywhere in the two production files | **Followed exactly** — represented, not resolved, exactly as 4G.4A §9 specified |

**No silent deviation was found.** This is itself worth stating prominently,
per the task's own instruction, precisely because it is the notable
(negative) finding: 4G.4B did not introduce anything 4G.4A did not
authorize.

### 5.1 Scope compliance

Searched (this pass, independently) `PreparationRunwayContracts.cs` and
`PreparationRunwayValidators.cs` — the only two production files — for
every forbidden term the task lists (`route selection`, `block-count
selection`, `experience mapping`, `readiness resolver`, `runway allocator`,
`calendar materializer`, `week skeleton`, `workout binding`, `volume
planning`, `long-run planning`, `pace planning`, `composer`, `runtime
wiring`, `DI registration`, `public DTO exposure`, `persistence`): **zero
matches for any of them** as actual implemented logic. The two files
contain exactly what §5 of 4G.4B's own document claims: enums, records, and
four static structural validators that only check arithmetic/structural
invariants (confirmed by full read of both files, reproduced in Part 3
above).

### 5.2 Contract inventory

All twelve named types (`PreparationRunwayBlockType`,
`PreparationRunwayPrescriptionProfile`, `PreparationRunwayPlanningStatus`,
`PreparationRunwayPlanningReason`, `PreparationRunwayContext`,
`PreparationNeedProfile`, `PreparationRunwayBlockAllocation`,
`PreparationRunwayAllocation`, `PreparationRunwayLeadingPartialSpan`,
`RacePlanCompositionMetadata`, `PreparationRunwayPlanningResult`, plus
`PreparationRunwayExperienceReference` and `PreparationRunwayValidationResult`)
exist exactly as documented — confirmed by direct read of
`PreparationRunwayContracts.cs` in Part 3 above, not by re-reading 4G.4B's
own inventory table.

### 5.3 Neutrality

No implicit `New → Beginner`, no automatic `AerobicStrengthLight`
selection, no fixed experience route, and no readiness-based block
selection exists anywhere in the two production files — confirmed by full
read, matching 4G.4B's own claim.

### 5.4 Validators

Re-derived directly from source (not from 4G.4B's prose):

- `RunwayDays = FullRunwayWeeks*7 + LeadingPartialDays`: enforced,
  `PreparationRunwayValidators.cs:29-30` (context) and `:46` (allocation).
- `sum(Block.FullWeekCount) = FullRunwayWeeks`: enforced, `:51`.
- `LeadingPartialDays ∈ [0,6]`: enforced, `:32` (context) and `:45`
  (allocation).
- Sequence indices unique/contiguous: enforced, `:49-50`.
- Zero-week blocks rejected: enforced, `:47` (`FullWeekCount <= 0`).
- Partial days cannot compensate for a missing full week: enforced via the
  same `:51` sum check plus the explicit test
  `Allocation_PartialDaysCannotReduceFirstBlocksFullWeeks`.
- Partial span inherits the first block when required: enforced, `:69-71`.
- `Planned` cannot carry unresolved reasons: enforced,
  `PreparationRunwayPlanningResultValidator.cs:108`.
- `DecisionRequired`/`Unsupported` cannot claim executable plans: enforced,
  `:110` (`Non-Planned outcomes cannot claim an executable allocation`).
- `NotApplicable` is distinct from a planned zero-week runway: enforced,
  `:115` — **but see §6 below for a genuine, newly-found gap in how
  completely this is enforced for other statuses.**
- Invalid input is not normalized: confirmed — every validator method only
  appends findings and returns the original values; none of the four
  validator classes contains an assignment back to any input field.

**All of 5.4's claims independently re-confirmed `VERIFIED_FROM_SOURCE`,
except the `NotApplicable`-distinctness claim, which is confirmed accurate
as stated but incomplete in scope — see §6.**

### 5.5 Date model

Re-derived independently in this pass:

```
PreferredCoreDays = PreferredCoreWeeks * 7
CoreStartDate = RaceDate.AddDays(-(PreferredCoreDays - 1))
RunwayDays = CoreStartDate.DayNumber - StartDate.DayNumber
```

For a 12-week core: `PreferredCoreDays = 84`, so `CoreStartDate = RaceDate -
83 days`. For the exact 12-week case (`StartDate == CoreStartDate`),
`RunwayDays = 0`. For a 20-week horizon with a 12-week core:
`RunwayDays = 56`, `FullRunwayWeeks = 8`, `LeadingPartialDays = 0` —
matching both 4G.4A's and 4G.4B.V's stated results, and matching the actual
executed test `Context_ExactTwentyWeekHorizon_HasEightFullRunwayWeeks`
(re-run in this pass, passed). **`CONFIRMED` — independently re-derived,
not merely re-read.**

**Required verdict: `PHASE_4G_4B_SCOPE_COMPLIANT`.**

---

## 6. Phase 4G.4B.V validation-quality findings

### Independence-bar assessment

4G.4B.V's own §16 "Test results" ran exactly two commands: the
`PreparationRunway` filter (41 tests — the exact suite 4G.4B itself wrote)
and the same filter combined with `Architecture` (also 41 — 4G.4B.V's own
text confirms "the runway suite itself contains the architecture/
reachability... tests," i.e. no separate architecture test exists outside
what 4G.4B already wrote). **Per this task's own independence bar, this
constitutes re-running 4G.4B's own suite, not independent verification of
it.** Its dark-reachability and neutrality claims (§12, §5, §6) are stated
as re-derived from "additional searches," but the document does not quote
the actual search commands/output, so those specific claims are
`CONFIRMED_BUT_REPORT_DERIVED` rather than independently reproducible from
the document alone.

**This audit constructed a new adversarial trace 4G.4B.V did not perform**,
per the task's explicit requirement to close this gap:

**Adversarial finding (source-level trace, performed in this pass, not a
re-run of any existing test):** `PreparationRunwayContextValidator` and
`RacePlanCompositionMetadataValidator` enforce `RunwayDays < 0` is invalid
**only when** `CompositionType == PreparationRunwayPlusCore`
(`PreparationRunwayValidators.cs:23-24`). Neither validator enforces the
converse: that `CompositionType == StandaloneCore` implies `RunwayDays ==
0`, or that `RunwayDays > 0` implies `CompositionType ==
PreparationRunwayPlusCore`. `PreparationRunwayPlanningResultValidator` only
cross-checks `CompositionType` against `RunwayDays` for the `NotApplicable`
status specifically (`:115`) — not for `Planned`, `DecisionRequired`,
`Unsupported`, or `InvalidInput`. **This means a `DecisionRequired` (or
`Unsupported`/`InvalidInput`) result could carry a `RacePlanCompositionMetadata`
whose `CompositionType` is internally inconsistent with its own
`RunwayDays`/`FullRunwayWeeks` (e.g. `CompositionType=StandaloneCore` with
`RunwayDays=7`), and no validator in the current file would reject it.**
Confirmed no existing test exercises this: every test helper in
`PreparationRunwayContractsTests.cs` derives `CompositionType` consistently
from `runwayDays` by construction (`Context()` helper, line 22:
`runwayDays == 0 ? StandaloneCore : PreparationRunwayPlusCore`; `Metadata()`
helper, line 31, identical pattern) — no test ever constructs a
deliberately-contradictory pairing.

**Severity assessment:** low/inert today — these contracts are dark and
unwired (confirmed independently, see below), so no live consumer can be
misled by this gap; it is a completeness gap in the *validators' own
claimed exhaustiveness*, not a reachable production defect. This is
reported per Part D's own instruction to construct and report a genuine
adversarial finding, not withheld because of its low current severity.

**Independent re-verification of dark reachability (fresh grep performed
in this pass, not a re-run of 4G.4B's `DarkReachability_...` test):**

```
grep -rln "DbConnectionInterceptor\|DbCommandInterceptor\|ConnectionOpening\|ConnectionOpened\|DiagnosticListener" backend --include=*.cs   → zero matches (used for Part 8, not runway, but demonstrates the same fresh-grep method applied)
```

For the runway-specific claim, this pass independently re-read (not
re-ran) both production `.cs` files in full (Part 3 above) and confirms:
no `using Microsoft.Extensions.DependencyInjection`, no `services.Add*`,
no controller/endpoint reference, no reflection, in either file. This
independently corroborates 4G.4B.V's `DARK_AND_UNWIRED` verdict via a
different method (full manual read vs. the existing test's automated
substring scan) — satisfying the independence bar's second allowed method
("a direct source-level trace... not by re-running a test").

### Claim table

| 4G.4B.V claim | Evidence used | Independent? | Result |
|---|---|---:|---|
| 41 PreparationRunway tests pass | Re-ran the identical filter in this pass | No (same suite) | `CONFIRMED_BUT_REPORT_DERIVED` (re-execution confirms it still holds, but is not new evidence of sufficiency) |
| `DARK_AND_UNWIRED` (no DI/endpoint/public-DTO reachability) | Full independent re-read of both production files by this audit | **Yes** | `INDEPENDENTLY_CONFIRMED` |
| `STRICTLY_SEPARATED` taxonomy (no shared base type, no core-phase member) | Full independent re-read of `PreparationRunwayContracts.cs` | **Yes** | `INDEPENDENTLY_CONFIRMED` |
| Validators "correctly" enforce all stated invariants | This audit's new adversarial trace (CompositionType/RunwayDays gap) | **Yes** | `CONTRADICTED` for full exhaustiveness (a real gap exists); `CONFIRMED` for every invariant the trace did not break |
| Date model `CORRECT` | Independently re-derived formula and boundary cases in Part 5.5 | **Yes** | `INDEPENDENTLY_CONFIRMED` |
| Full-suite run showed 14 failures (12 reset + 2 DI) | This pass's own two fresh full-suite runs (Part 7 below) | **Yes** | `PARTIALLY_CONFIRMED` — the 2 DI failures reproduced identically; the 12 reset failures did not reproduce in either fresh run performed in this pass |

**Required verdict: `VALIDATION_ADEQUATE_WITH_GAPS`** — not `VALIDATION_STRONG`,
because its own test-execution evidence was suite-re-run rather than
independent, and one genuine (if currently inert) validator gap existed
that it did not find; not `VALIDATION_TOO_DERIVATIVE` or
`VALIDATION_INCORRECT`, because its source-level structural claims (dark
reachability, taxonomy separation, date model) all independently
re-confirmed as accurate.

---

## 7. Reset investigation findings

Per Part E's explicit requirement, this pass ran the full backend suite
**twice, fresh, in this pass** (not relying on any prior report's numbers):

**Run 1** (`dotnet test backend/RunningApp.sln -c Release --no-build
--logger "console;verbosity=normal"`): **1385 passed, 2 failed, 0 skipped,
1387 total.** Failures: `DependencyInjectionResolutionTests.RealHost_CatalogLivePilotOptions_DefaultsToDisabled`,
`DependencyInjectionResolutionTests.RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`.

**Run 2** (identical command, re-executed independently): **1385 passed, 2
failed, 0 skipped, 1387 total.** Identical two failure names, identical
counts.

**Neither run reproduced any of the 12 reset-endpoint HTTP 500 failures**
4G.4B.V's own run reported. This is this pass's own observed fact from two
independent executions — not an inference from reading the reset/DI
investigation reports' own claims (which independently also failed to
reproduce them, a third and fourth data point, but not the ones this
audit is required to generate itself).

Per the stop-condition list, `RESET_HTTP_500_REPRODUCES` did **not** fire
in either run — no stop was required for this reason.

**Validated claims from the two prior investigation passes:**
- Exact test commands: both prior passes used
  `dotnet test backend/RunningApp.sln -c Release --no-build`, matching this
  pass's commands exactly — no `--no-build`-vs-build discrepancy exists
  between the passes.
- Whether tests share state/run in parallel: not independently
  re-investigated in this pass beyond confirming the reset endpoint uses a
  shared Postgres instance (`ApiIntegrationTestCollection` — collection
  fixture, meaning tests within it do not run in parallel with each other
  by xUnit's own collection-fixture semantics); this is consistent with,
  not contradicting, "shared state" as a plausible contributing factor.
- Whether the 12 failures all truly failed at the reset helper: confirmed
  from 4G.4B.V's own quoted evidence (§17, "reached the shared
  `POST /api/v1/testing/reset` helper and observed HTTP 500") — this
  audit did not independently re-observe a failure to confirm the
  mechanism, since none reproduced in this pass's own two runs.

**Classification: `TRANSIENT_NOT_REPRODUCED`** — backed by this pass's own
two fresh runs, stated as such rather than inherited from the prior
reports' say-so, per the task's explicit requirement. `MULTIPLE_CAUSES_POSSIBLE`
is not retained as a live hypothesis here because no reproduction occurred
in this pass to investigate causes from; the prior reset/DI investigation
pass's own non-reproduction is consistent, not additional new evidence
generated by this pass.

**Stop-decision correctness:** the reset/DI stabilization pass's own stop
(`INITIAL_FAILURE_SET_MATERIALLY_DIFFERENT` — 14 expected, 2 actually
observed) was appropriate: fixing 12 non-reproducing failures would have
been speculative. **`STOP_CORRECT`.**

A future diagnostic-only instrumentation pass (e.g., a connection-retry
counter or request-timing log around the reset endpoint specifically) is
warranted given the pattern recurred once (matching the repository's own
pre-existing `TD-TESTFLAKE-001` observation) but has now failed to
reproduce in four consecutive fresh runs across two separate investigation
passes plus this audit's own two runs.

---

## 8. DI configuration-intent findings

### Configuration sources (independently re-traced from source in this pass)

| Environment | Effective value | Evidence | Tracked in git? |
|---|---:|---|---:|
| CLR/property default | `false` | `LivePlanPreviewRouting.cs:14`: `public bool Enabled { get; set; } = false;` (quoted verbatim, read directly in this pass) | n/a (source code) |
| base/production host | `false` (no override) | `grep -n "CatalogLivePilot" backend/RunningApp.Api/appsettings.json` → zero matches (this pass's own grep) | **Yes** — `backend/RunningApp.Api/appsettings.json` is tracked |
| Development host | `true` | `backend/RunningApp.Api/appsettings.Development.json:14-16`: `"CatalogLivePilot": { "Enabled": true }` (read directly in this pass) | **Yes** — confirmed via `git ls-files backend/RunningApp.Api/appsettings.Development.json`, which returned the path (tracked); `git diff HEAD -- <path>` returned empty (the committed value already is `true`, not a local uncommitted edit) |
| `CustomWebApplicationFactory` host | `true` (inherits Development) | `CustomWebApplicationFactory.cs:43`: `builder.UseEnvironment("Development");`; `ConfigureAppConfiguration` (lines 45-53) adds only `ConnectionStrings:DefaultConnection` and `PlanCatalog:CatalogRootPath` — no `CatalogLivePilot` key, confirmed by direct read | Test file itself is tracked; the override dictionary contains no live-pilot key at all |

**This directly answers the task's explicit tracked-vs-local question: the
Development config enabling the live pilot is genuinely tracked/committed,
not a local-only or gitignored file.** The DI investigation report's
"environment-specific by design" framing is therefore the correct, stronger
claim — `INTENTIONAL_ENVIRONMENT_SPECIFIC` (a portable, shareable design
decision) rather than `INTENTIONAL_LOCAL_ONLY_NOT_PORTABLE`. This is a
`CONFIRMED` upgrade over what the DI report itself stated (it asserted
"environment-specific" without explicitly checking git-tracked status;
this audit closes that gap with direct evidence).

### Test 1 — `RealHost_CatalogLivePilotOptions_DefaultsToDisabled`

Quoted directly from `DependencyInjectionResolutionTests.cs:102-108`:

```csharp
[Fact]
public void RealHost_CatalogLivePilotOptions_DefaultsToDisabled()
{
    using var scope = _factory.Services.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<CatalogLivePilotOptions>>();
    Assert.False(options.Value.Enabled);
}
```

This resolves `IOptions<CatalogLivePilotOptions>` from the **real**,
Development-forced host — not the CLR default, not base configuration. Per
the table above, that host's effective value is `true` (tracked,
committed). The test name claims "DefaultsToDisabled" but measures the
real host's effective, environment-configured value, not a default.
**Classification: `STALE_TEST_EXPECTATION` — `CONFIRMED`, independently
re-derived from source in this pass, not inherited from the DI report's
own conclusion.**

### Test 2 — `RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`

Quoted directly from `DependencyInjectionResolutionTests.cs:110-127`: five
`GetRequiredService` calls (`IGenerationRouteDecider`,
`ICatalogPreviewGenerator`, `IGeneratedCatalogPlanPayloadValidator`,
`ICatalogPlanConfirmationService`, `ICatalogPeakVolumeBandLoader`) plus a
sixth resolution/assertion (`IOptions<CatalogLivePilotOptions>>().Value.Enabled`
must be `false`). The feature-flag assertion is the only failing line
(126) and is unrelated to the other five resolutions' own correctness.

Searched (this pass, fresh):
`grep -rln "DbConnectionInterceptor\|DbCommandInterceptor\|ConnectionOpening\|ConnectionOpened\|DiagnosticListener" backend --include=*.cs`
→ **zero matches anywhere in the backend tree.** No connection counter, fake
provider, or connection-state assertion exists. The class-level doc comment
(lines 41-48, quoted above in the source read) *asserts* "No real
PostgreSQL connection is opened by anything in this test" as prose
reasoning, but nothing in the test body measures it.

**Classification of the no-database-I/O claim: `NOT_MEASURED`.** The test
name overstates its coverage (`_WithNoDbConnection` implies a verified
invariant; only an absence-of-exception is actually observed, which does
not distinguish "no connection opened" from "a connection opened and
closed without error").

### Stop-condition evaluation — the critical finding

**Could Test 1 safely be corrected independently?** **Yes.** It requires no
new instrumentation — only replacing the single assertion target (real
Development host → CLR default `new CatalogLivePilotOptions().Enabled`) or
adding a second, correctly-named test for the real-host effective value.
Nothing about Test 1's fix depends on solving the no-DB-I/O measurement gap.

**Could Test 2's stale feature-flag assertion be removed/separated without
first implementing a connection interceptor?** **Yes, for the feature-flag
assertion specifically** (line 126 can be deleted or moved to a
correctly-scoped test with zero dependency on connection instrumentation).
**No, for preserving the no-DB-I/O claim under its current name** — that
specific claim cannot be honestly retained without new instrumentation.

**These two answers are different from each other** — confirming the task's
own suspicion that the prior pass's all-or-nothing stop was overly
conservative for at least one of the two tracks.

**Classification: `TRACK_SPECIFIC_STOP_WOULD_HAVE_BEEN_BETTER`.** Test 1
could have been safely, narrowly corrected in the reset/DI stabilization
pass without waiting for connection-interceptor design work; only Test 2's
*specific, currently-unmeasured* no-DB-I/O sub-claim genuinely required the
larger design/instrumentation decision the DI report proposed. The prior
passes' full stop was not incorrect in outcome (no unsafe change was made),
but it bundled a track that had no such precondition together with one that
did.

---

## 9. Stop-condition correctness analysis

None of this audit's own stop conditions
(`ANY_PREPARATION_RUNWAY_LIVE_WIRING_FOUND`,
`UNAPPROVED_ROUTE_OR_PRESCRIPTION_LOGIC_FOUND`,
`DATE_OR_PARTIAL_SPAN_INVARIANT_DEFECT_FOUND`, `RESET_HTTP_500_REPRODUCES`,
`CONFIGURATION_INTENT_CONTRADICTED`, `UNATTRIBUTED_PRODUCTION_CHANGE_FOUND`,
`REPORT_EVIDENCE_CANNOT_BE_RECONSTRUCTED`) fired. Every report's evidence
was reconstructible from source in this pass (Parts 4-8 above each quote
the actual reconstructed evidence, not merely the prior report's
conclusion).

---

## 10. Safe next-fix options (design only, not applied)

### Test 1

Repository evidence supports exactly the structure the task anticipated:

```csharp
[Fact]
public void CatalogLivePilotOptions_ClrDefault_IsDisabled() =>
    Assert.False(new CatalogLivePilotOptions().Enabled);

[Fact]
public void RealHost_Development_CatalogLivePilotOptions_IsEnabled()
{
    using var scope = _factory.Services.CreateScope();
    var options = scope.ServiceProvider.GetRequiredService<IOptions<CatalogLivePilotOptions>>();
    Assert.True(options.Value.Enabled); // real Development host, intentional per appsettings.Development.json
}
```

### Test 2 — option comparison

| Criterion | Option A (rename + defer) | Option B (add instrumentation now) | Option C (split into 3) |
|---|---|---|---|
| Truthfulness | High — test name matches what it measures | High, once built | Highest — each test's name matches exactly one claim |
| Coverage preservation | Coverage for the no-DB claim is *dropped* until a future pass, but honestly, not falsely-green | Preserved immediately | Preserved, explicitly marked `DecisionRequired` until built |
| Implementation cost | Lowest — rename + delete one assertion | Highest — needs an EF Core/Npgsql interceptor + test-host wiring | Medium — same split as A, plus one new explicit-gap test |
| Test isolation | Improved (flag concern removed) | Unchanged | Best — three single-purpose tests |
| Production impact | None | None (test-only interceptor) | None |
| Risk of hidden constructor I/O going unnoticed | Same as today (unmeasured) | Eliminated | Same as today, but explicitly labeled as a known gap rather than silently implied covered |
| Ability to make full suite green honestly | Yes, immediately | Yes, after instrumentation work | Yes, immediately, with an honest `DecisionRequired` marker instead of a false claim |

**Recommended (design only, not implemented): Option C** — it is the only
option that is both immediately achievable (no new instrumentation
required) and does not silently drop the no-DB-I/O claim's visibility the
way Option A does; it makes the gap explicit rather than deferring it
invisibly. Option A is `ACCEPTABLE_WITH_CONDITIONS` as a faster interim
step. Option B is the eventual correct end state but should not block the
other two tests' correction.

---

## 11. Preparation Runway layer-status matrix

| Layer | Status |
|---|---|
| Canonical reconciliation | Complete |
| Typed contracts | Complete |
| Structural validation | Complete (with one newly-found, currently-inert exhaustiveness gap — §6) |
| Decision resolution | Not started |
| Runway allocator | Not started |
| Partial calendar materialization | Not started |
| Week skeleton | Not started |
| Volume/long-run prescription | Not started |
| Pace prescription | Not started |
| Composer | Not started |
| Runtime activation | Not started |

---

## 12. Test results (all runs, reported separately)

| Command | Passed | Failed | Skipped | Total |
|---|---:|---:|---:|---:|
| `dotnet build backend/RunningApp.sln -c Release` | — | 0 errors, 0 warnings | — | — |
| `dotnet test ... --filter PreparationRunway` | 41 | 0 | 0 | 41 |
| Full suite — **Run 1** (this pass) | 1385 | 2 | 0 | 1387 |
| Full suite — **Run 2** (this pass) | 1385 | 2 | 0 | 1387 |

Both full-suite runs' failing tests, identical in both runs:
`DependencyInjectionResolutionTests.RealHost_CatalogLivePilotOptions_DefaultsToDisabled`,
`DependencyInjectionResolutionTests.RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`.

Plan-catalog tests were not run: no plan-catalog code or mechanically
consumed artifact was touched by any of the five passes under review
(confirmed by Part 3's attribution table — every 4G.4A/4G.4B/4G.4B.V/reset/DI
file is either a root-level `.md` or a backend `.cs` file; zero
`plan-catalog/**` paths appear in any of the five passes' own "files
changed" lists, cross-checked against `git status --short` in this pass).

---

## 13. Discrepancies between reports and repository

- 4G.4B.V's independence for its own PreparationRunway test claims is
  weaker than its confident `VALIDATION_STRONG`-adjacent framing implies
  (it did not explicitly self-classify with this task's independence bar,
  since that bar did not exist as a stated requirement for that pass) —
  reclassified here as `VALIDATION_ADEQUATE_WITH_GAPS`.
- A genuine (if currently inert) validator exhaustiveness gap was found in
  this pass (CompositionType/RunwayDays consistency not enforced outside
  `NotApplicable`) that no prior pass identified.
- No other discrepancy between any report's stated file list, test count,
  or architectural claim and this pass's own independent re-derivation was
  found.

---

## 14. Exact next phase

**`READY_FOR_TARGETED_TEST_CORRECTION`** for the two DI failures, using
Option C from Part 10 (split Test 2 into three single-purpose tests;
correct Test 1's target). This is independent of, and does not need to
wait for, any Preparation Runway decision work. Separately,
**`BLOCKED_BY_NAMED_DECISIONS`** remains the correct status for any
Preparation Runway allocator work, unchanged from 4G.4B.V's own
assessment, now independently reconfirmed.

---

## 15. Files changed

Exactly one new file, created by this pass:

```
CROSS_PHASE_4G_4A_TO_4G_4B_V_AND_BACKEND_BASELINE_INDEPENDENT_AUDIT.md
```

No existing file was modified or deleted.

---

## 16. Confirmation no implementation/fix

No production code, test file, configuration file, catalog artifact, TD
inventory, or frontend file was modified by this pass. No
`PreparationRunwayPlanner`/allocator/materializer/composer/resolver was
created. No route was resolved. No km/pace/duration was calculated. Test
execution updated only pre-existing generated `bin`/`obj` build outputs,
which are not deliverables.

## 17. Confirmation no commit or push

No commit, amend, rebase, reset, history rewrite, branch operation, or
push was performed.

---

## Required final classifications

```text
PHASE_4G_4A_ACCURACY=CONFIRMED
PHASE_4G_4A_VS_4G_4B_CONSISTENCY=CONFIRMED
PHASE_4G_4B_SCOPE_COMPLIANCE=SCOPE_COMPLIANT
PHASE_4G_4B_CONTRACT_CORRECTNESS=CONFIRMED_WITH_MINOR_GAPS
PHASE_4G_4B_V_VALIDATION_QUALITY=PARTIALLY_CONFIRMED
RESET_FAILURE_STATUS=TRANSIENT_NOT_REPRODUCED
RESET_STOP_DECISION=STOP_CORRECT
DI_CONFIGURATION_INTENT_STATUS=INTENTIONAL_ENVIRONMENT_SPECIFIC
DI_TEST_1_ROOT_CAUSE=STALE_TEST_EXPECTATION
DI_TEST_2_ROOT_CAUSE=MULTIPLE_CONCERNS_IN_ONE_TEST
NO_DB_IO_COVERAGE_STATUS=NOT_MEASURED
DI_STOP_DECISION_QUALITY=TRACK_SPECIFIC_STOP_PREFERRED
BACKEND_SUITE_STATUS=NOT_GREEN
RUNWAY_ALLOCATOR_READINESS=BLOCKED_BY_NAMED_DECISIONS
RUNWAY_PRESCRIPTION_READINESS=NOT_READY
NEXT_ACTION=READY_FOR_TARGETED_TEST_CORRECTION
```

**Evidence trace for each, per this document's self-consistency
requirement:**

- `PHASE_4G_4A_ACCURACY=CONFIRMED` — Part 4's full evidence table, every row
  independently source-verified.
- `PHASE_4G_4A_VS_4G_4B_CONSISTENCY=CONFIRMED` — Part 5.0's table, no
  silent deviation found across six checked items.
- `PHASE_4G_4B_SCOPE_COMPLIANCE=SCOPE_COMPLIANT` — Part 5.1, zero forbidden
  terms found in a fresh grep of both production files.
- `PHASE_4G_4B_CONTRACT_CORRECTNESS=CONFIRMED_WITH_MINOR_GAPS` — Part 5.4,
  every invariant confirmed except the CompositionType/RunwayDays gap found
  in Part 6.
- `PHASE_4G_4B_V_VALIDATION_QUALITY=PARTIALLY_CONFIRMED` — Part 6's claim
  table: one row `CONTRADICTED`, one row `CONFIRMED_BUT_REPORT_DERIVED`,
  three rows `INDEPENDENTLY_CONFIRMED`.
- `RESET_FAILURE_STATUS=TRANSIENT_NOT_REPRODUCED` — Part 7, two fresh runs
  performed in this pass, neither reproduced.
- `RESET_STOP_DECISION=STOP_CORRECT` — Part 7's stop-decision paragraph.
- `DI_CONFIGURATION_INTENT_STATUS=INTENTIONAL_ENVIRONMENT_SPECIFIC` — Part
  8's configuration-sources table, including the git-tracked verification.
- `DI_TEST_1_ROOT_CAUSE=STALE_TEST_EXPECTATION` — Part 8, quoted source +
  table cross-reference.
- `DI_TEST_2_ROOT_CAUSE=MULTIPLE_CONCERNS_IN_ONE_TEST` — Part 8, quoted
  source showing five unrelated resolutions plus one stale assertion in one
  test.
- `NO_DB_IO_COVERAGE_STATUS=NOT_MEASURED` — Part 8, fresh grep showing zero
  instrumentation mechanisms exist.
- `DI_STOP_DECISION_QUALITY=TRACK_SPECIFIC_STOP_PREFERRED` — Part 8's two
  differently-answered stop-condition questions.
- `BACKEND_SUITE_STATUS=NOT_GREEN` — Part 12's table, 2/1387 failing in
  both fresh runs.
- `RUNWAY_ALLOCATOR_READINESS=BLOCKED_BY_NAMED_DECISIONS` — Part 11's
  layer-status matrix plus 4G.4B.V §17's open-decisions table, independently
  re-confirmed unresolved by this pass's own reading of the same two source
  files.
- `RUNWAY_PRESCRIPTION_READINESS=NOT_READY` — Part 11.
- `NEXT_ACTION=READY_FOR_TARGETED_TEST_CORRECTION` — Part 14, derived from
  Part 8's track-specific-stop finding.
