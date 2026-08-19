# Phase 10K-FREQ.6D.4D.5 (Split E) — Real Intermediate×5D Catalog Content, Published-Bundle Runtime Discovery, Public-Activation Blocker Disclosure

**Partial implementation phase. The PlanCatalog-side chain (real RUN_LAYOUT_5D → combination → dual-lane progression → 8 profiles → published bundle) and RunningApp's published-bundle runtime-discovery wiring are implemented and verified. Public activation of `TEN_K__5D__INTERMEDIATE` was attempted, found to trigger a real 500 in the live Core calendar-materialization pipeline, and reverted rather than shipped broken. No new product decision, no new dosage, no new progression mathematics, no new Adaptation policy was made. `FREQ.6D.4D` overall remains open — this split does not close it.**

## 1. Preflight

`PHASE_LEDGER.md` row 76: `FREQ.6D.4D.4`, `IMPLEMENTATION`, `DONE`, `FREQ6D4D_SPLIT_D_IMPLEMENTED_SPLIT_E_READY`, confirmed. `docker ps` confirmed `appsel-dev-postgres` healthy for the entire session (already running from the prior Split D session; real PostgreSQL-backed verification was available throughout — no simulation, no skip).

## 2. What this split set out to do vs. what it delivered

The originating prompt specified an enormous, 74-section integrated implementation+verification+public-activation+closure scope. Mid-session, real end-to-end testing (not just static code review) surfaced a genuine structural blocker inside the public Core route itself — not merely in the already-known-hard Preparation Runway/Long-Horizon layers. Given the size of the remaining gap, this split was deliberately narrowed, with the user's explicit sign-off at two decision points (§3), to:

1. Author real Intermediate×5D catalog content (RUN_LAYOUT_5D, combination, dual-lane progression, 8 profiles promoted to VALIDATED, a real published bundle).
2. Wire RunningApp's runtime published-bundle discovery (the gap Split C disclosed).
3. Investigate, and honestly report on, what public activation actually requires — rather than mechanically flip a routing flag and claim activation.

Public activation itself is **not** delivered. §11 explains why in detail.

## 3. User decision points (both explicit, not inferred)

1. `INTERMEDIATE_PROGRESSION_MODIFIER_V1`'s `MaximumHardSessionsPerWeek` was frozen at 1 by every prior report; RUN_LAYOUT_5D's 2 KEY_SESSION slots require 2. Asked via `AskUserQuestion`; user selected **"Raise the cap to 2"** — authored as a new `v3` progression-modifier version, scoped only to a new `INTERMEDIATE_MODIFIER v7`, leaving the 4D level-modifier's `v6`/`v2` pairing untouched.
2. After the PlanCatalog-side chain was complete and verified (9/9 new tests, 1510/1510 total, a real CLI-published `1.1.0` release), asked whether to continue through runtime wiring + public activation in this session; user selected **"Continue now"**.
3. After a static-analysis map of every hardcoded 4D/v10 routing guard was produced (§9), asked how to scope public activation; user selected **"Activate 8-14w only"** (not the 15-20w Preparation Runway or 21-52w Long-Horizon routes, both structurally coupled to a hardcoded 4-slot weekly shape).
4. After the 8-14w widening itself produced a real 500 in E2E testing (§11), the widening was reverted without asking — this was not a product-scope decision, it was avoiding shipping a confirmed bug. Reported to the user immediately after.
5. Closing this session: asked how to close out; user selected **"Write the phase report now, scoped honestly"** (over "commit code, stop there" and "don't commit, let me review") — this report.

## 4. Real PlanCatalog catalog content authored

All real, VALIDATED-status, source-of-truth catalog documents (not synthetic test fixtures):

- `plan-catalog/catalog/layouts/run-layout-5d.v1.json` — `RUN_LAYOUT_5D v1`: 5 slots, `KEY_SESSION, EASY_SUPPORT, KEY_SESSION, EASY_SUPPORT, LONG_RUN` (sequenceOrder 1-5), `runsPerWeek: 5`.
- `plan-catalog/catalog/templates/ten-k-master.v7.json` — `TEN_K_MASTER v7`: FOUNDATION's `eligibleWorkoutFamilies` extended to include `QUALITY`; pins `workoutProgression → TEN_K_WORKOUT_PROGRESSION_V1 v6`.
- `plan-catalog/catalog/workout-progressions/ten-k-workout-progression.v6.json` — new lane-authored progression version. For each of FOUNDATION/BUILD/RACE_SPECIFIC/TAPER, two independent lanes (`laneOrdinal` 0/1), each with its own single stage carrying real `workoutCandidates` + real `prescriptionProfileCandidates`, exactly matching the 8 real profiles authored in `FREQ.6D.4C.3`.
- `plan-catalog/catalog/level-modifiers/intermediate-modifier.v7.json` — `INTERMEDIATE_MODIFIER v7`: `eligibleWorkouts` extended to the 5D-relevant workout versions; `progressionModifier → INTERMEDIATE_PROGRESSION_MODIFIER_V1 v3`.
- `plan-catalog/catalog/progression-modifiers/intermediate-progression-modifier.v3.json` — `maximumHardSessionsPerWeek: 2`, `allowSecondHardStimulus: true` (§3.1's user-approved decision).
- `plan-catalog/catalog/combinations/ten-k-5d-intermediate.v1.json` — `TEN_K__5D__INTERMEDIATE v1`, status `VALIDATED`, pins the four documents above plus the unchanged, reused `APPSEL_RACE_PLAN_V1 v4` rule pack.
- All 8 `plan-catalog/catalog/prescription-profiles/intermediate-5d-*.json` documents: `status` promoted `DRAFT → VALIDATED` (mechanical field change only — content unmodified; confirmed safe: no legacy bare-key resolution risk, never previously included in any real release's hash-pinned file set).

## 5. PlanCatalog source changes (mechanical bug fixes, not new decisions)

- `TemplateCombinationValidator.ValidateEffectiveWorkoutSet` was not lane-aware (read `.Stages` directly instead of iterating `.EffectiveLanes`); fixed — the same class of bug already fixed twice in Split B for `WorkoutClosureResolver`/`CatalogBundleAssembler`. Without this fix, `TC_EFFECTIVE_WORKOUT_SET_EMPTY` incorrectly rejected the real dual-lane 5D progression.
- `ICatalogBundleAssembler` gained a new port-level `Assemble(..., IReadOnlyList<VersionedCatalogReference> exactPrescriptionProfileRefs, ...)` overload (Contracts-level type, usable from Core) — the existing exact-dependency overload was Infrastructure-only, so `CatalogPublisher` could not call it through the port interface.
- `CatalogBundleAssembler` implements the new overload by wrapping into `ExactPrescriptionProjectionDependency` and delegating to the existing internal logic — zero new bundle-assembly behavior.
- `CatalogPublisher.AssembleBundle` (new private method) now computes `PrescriptionProfileClosureResolver.ComputeExactClosureRefs` per combination and picks the exact-dependency overload only when the combination's progression actually has profile candidates — the real publish path (`build-release`/`publish` CLI commands) now correctly produces profile-backed bundles; the legacy 3-arg path remains byte-identical for every combination without candidates (proven: `Legacy4DCombination_RemainsExecutionPrescriptionsNull_ZeroDelta`).

## 6. New PlanCatalog tests

`plan-catalog/tests/PlanCatalog.Tests/Golden/Intermediate5DRealCatalogIntegrationTests.cs` — 9 tests against the REAL catalog directory (not synthetic fixtures): `RUN_LAYOUT_5D` shape (2 KEY + 2 EASY + 1 LONG), exact-pinned dependency resolution, full-graph validation, both-lanes-authored-per-phase, all 8 real profiles reachable, `ExecutionPrescriptions` non-null with all 8 profiles present, deterministic hash/order across repeated assembly, real content-fidelity assertions on two specific profiles (BLD-S: 10 reps/60s work/60s Jog recovery; TAP-S: 6 reps/20s work/100s Walk recovery), and zero-delta confirmation that the pre-existing 4D combination remains `ExecutionPrescriptions: null`.

Two pre-existing tests needed a mechanical ambiguity fix (`assembler.Assemble(stamped, key, version, [])` became ambiguous between the two new `IReadOnlyList<T>` overloads once the port gained the second one) — changed to `Array.Empty<ExactPrescriptionProjectionDependency>()` explicitly. No behavioral change.

**Result: 1510/1510 PlanCatalog.Tests, including the 9 new (7 pre-existing test-count baseline + 2 fixed + net new).**

## 7. Real published release

`plan-catalog/artifacts/appsel-plan-catalog/1.1.0/` — a complete, real, immutable release directory, produced by the actual `publish --version 1.1.0 --channel Pilot --allow-unconfirmed-content` CLI command (the `--allow-unconfirmed-content` flag was required by a pre-existing, unrelated `PLACEHOLDER_UNCONFIRMED` content gate on four historical workout versions — confirmed unrelated to 5D and resolved the same way every prior `-pilot` release presumably was). Contains `bundles/TEN_K__5D__INTERMEDIATE.v1.json` with all 8 real profile keys present in `executionPrescriptions`, and `bundles/TEN_K__4D__INTERMEDIATE.v4.json` unchanged with no `executionPrescriptions` field — confirmed zero legacy delta via grep on the real file content, not just test assertions.

## 8. RunningApp runtime published-bundle discovery

Closes the exact gap Split C disclosed: `PlanCatalogRootResolver` reads raw source catalog only; no published-bundle file-discovery convention was ever wired into the live runtime path.

- `backend/RunningApp.Application/RuntimeCatalog/Prescription/Execution/PublishedTemplateBundleLoader.cs` (new) — `IPublishedTemplateBundleLoader.TryLoadAsync(combinationKey, combinationVersion)`. Resolves the real, already-existing Process A publish convention as a sibling of the catalog root (`{catalogRoot}/../artifacts/appsel-plan-catalog/{PublishedBundleReleaseVersion}/bundles/{key}.v{version}.json`) — reuses the existing publisher output convention rather than inventing a side-channel, per the phase's own explicit instruction. Pinned by exact configuration (`PlanCatalogOptions.PublishedBundleReleaseVersion`), never "latest"/scanning. Returns `null` (not a failure) when no bundle exists for a candidate — the pre-existing Split-C `CatalogSessionPrescriptionMissingExecutionPrescriptionException` provides fail-closed behavior downstream if a bound plan genuinely needs profile-backed execution but has none.
- `PlanCatalogOptions.PublishedBundleReleaseVersion` (new, nullable `string?`) — null/absent means no lookup occurs at all, byte-identical to every deployment before this split.
- `CatalogPreviewGenerator.cs` — full constructor chain widened (public DI-facing constructor + the internal 16→17-param leaf constructor + a new `DefaultPublishedBundleLoader()`/`NullPublishedTemplateBundleLoader` no-op default threaded through every intermediate test-seam constructor that doesn't explicitly override it, so every existing internal-constructor call site's behavior is unchanged). The live preview-generation path now calls `_publishedBundleLoader.TryLoadAsync(candidate.CandidateKey, candidate.CandidateVersion)` immediately before `CatalogSessionPrescriptionRequest` construction, builds `ExecutionPrescriptionIndex.Build(bundle)` when a bundle is found, and passes it as the request's existing (Split-C, previously always-null-from-this-call-site) `ExecutionIndex` parameter.
- `backend/RunningApp.Api/Program.cs` — registers `IPublishedTemplateBundleLoader` via a factory reading `IOptions<PlanCatalogOptions>` + the already-resolved `catalogRoot.CatalogRootPath`; `PlanCatalogOptions.PublishedBundleReleaseVersion` is now read from `PlanCatalog:PublishedBundleReleaseVersion` configuration.
- `backend/RunningApp.Api/appsettings.json` — pins `PlanCatalog:PublishedBundleReleaseVersion = "1.1.0"` (the real release from §7).
- `backend/RunningApp.Api/RunningApp.Api.csproj` — added a `<Content Include>` glob packaging exactly `plan-catalog/artifacts/appsel-plan-catalog/1.1.0/bundles/**/*.json` as a sibling of the existing `plan-catalog/catalog/**/*.json` glob (deployed/packaged builds can discover the pinned bundle too — §53-54's clean-deployment-discovery requirement).

**No candidate reaches this new lookup path in production today** — `TEN_K__5D__INTERMEDIATE` is not publicly routable (§11), and no other candidate has a published bundle in `1.1.0` with real execution prescriptions. This wiring is genuinely dormant-but-live infrastructure, exactly like Split C's `ExecutionPrescriptionIndex` was before this split activated its input.

## 9. Public-routing audit (static analysis, produced before the E2E finding in §11)

A full read-only map of every hardcoded `TEN_K__4D__INTERMEDIATE`/`v10`/`DaysPerWeek == 4` guard across `RunningApp.Application`/`RunningApp.Api` was produced (via a dedicated Explore-agent audit). Summary:

- **Routing layer** (`V1CatalogPilotIdentityPolicy`, `LivePlanPreviewRouting`, `PlanServices.IsPreparationRunwayPilotScope`, `CatalogPreviewGenerator`'s hardcoded Runway candidate load, `LongHorizonPublicPlanService.ValidatePilot`) — mechanically small, one-line-per-file widenable, but `V1CatalogPilotIdentityPolicy`'s own doc comment explicitly frames its allow-list as `EXPLICIT_PRODUCT_DEFAULT`/"deliberately enumerated so a future cell can never be admitted by accident" — widening it is itself the product decision, not a bug fix.
- **Preparation Runway materialization** (`PreparationRunwayWeekMaterializer`/`NumericMaterializer`/`PaceMaterializer`/`CoreWeekOneTargetAdapter`) — structurally hard: hardcodes a 4-slot weekly shape (`OrderedSlots.Count != 4`, a literal `[KEY_SESSION, EASY_SUPPORT, EASY_SUPPORT, LONG_RUN]` canonical layout, a `FourDaySessionDistanceAllocationPolicy`, per-position block-role dictionaries). A 5-day week needs a real training-design decision (what is the 5th weekly slot?) before this is even attemptable, not a literal swap. Also found: `TenKPreparationRunwayDarkOrchestrator`'s own doc comment claiming it's off the live public-preview call graph is **stale** — it is genuinely reached from the real 15-20w route via `CatalogPreviewGenerator.GeneratePreparationRunwayPreviewAsync`.
- **Long-Horizon** (`LongHorizonPublicPlanService`) — its own independent hardcoded lock, mechanically trivial in isolation, but almost certainly inherits the same Runway materialization structural problem for any horizon touching that block internally (not tested this split).

Full findings preserved in this session's transcript; not separately filed as their own document since they are superseded by the more load-bearing, code-verified finding in §11.

## 10. Public activation: attempted, reverted

Per the user's §3.3 decision, only the routing layer was widened for the 8-14 week Core route (the horizon this split's real catalog content targets): `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency`/`ResolveCandidate` gained an `(Intermediate, 5)` arm. `LivePlanPreviewRouting` required no change (it consults the policy dynamically). `PlanServices.IsPreparationRunwayPilotScope`/`LongHorizonPublicPlanService.ValidatePilot` were deliberately left untouched (15-20w/21-52w out of scope per §3.3).

A new end-to-end test file (`Gen5DIntermediatePublicActivationTests.cs`, mirroring the existing `Gen3BThreeDayPublicActivationTests.cs` pattern, run against a real `CatalogPublisher`-produced test release) was written to verify the widening. It found a real bug on the first run (§11) rather than confirming success.

## 11. The real blocker: `CatalogWeekSkeletonCalendarMaterializer`

`backend/RunningApp.Application/RuntimeCatalog/Schedule/Materialization/CatalogWeekSkeletonCalendarMaterializer.cs` — the Core route's calendar-date-assignment step, positioned **before** any Preparation Runway/Long-Horizon code, inside the exact 8-14w route this split targeted:

- `ValidateSkeletonRoleStructure` throws unless `skeleton.DaysPerWeek is (3 or 4)`, and separately requires `keyCount == 1` (exactly one `KEY_SESSION` slot per week) — both would reject a real 5D skeleton (2 KEY_SESSION slots) outright.
- More seriously: the actual date-assignment algorithm (`TryAssignKeySessionDates`, a bounded backtracking search) and its output (`chosenKeySessionDates`, a `DateOnly?[]` **one entry per week**, i.e. one date total) and consumer (`BuildDatedWeek`, which assigns the single `keySessionDate` value to **every** slot whose `StructuralRole == "KEY_SESSION"`) are built around exactly one KEY_SESSION slot per week. If the count-guard above were removed without fixing this, a 5D week's two `KEY_SESSION` slots would silently receive the **identical calendar date** — a real, silent data-integrity bug, not merely a rejected request.

Confirmed by a genuine HTTP 500 from a real `dotnet test` run against a real published test release (`PublishedCatalogTestRelease`, the same real-`CatalogPublisher` fixture `Gen3BThreeDayPublicActivationTests` uses), not by static analysis:

```
RunningApp.Application.RuntimeCatalog.Schedule.Materialization.CatalogCalendarRoleStructureInvalidException:
Core calendar assignment supports resolved 3D/4D layouts, but the source skeleton declares 5.
```

Widening this correctly requires: (a) generalizing `chosenKeySessionDates` to carry N dates per week, (b) a real multi-slot backtracking search satisfying both the existing LONG_RUN-separation invariant and a genuinely new, currently undecided invariant — the minimum separation between the two weekly KEY sessions themselves (no report anywhere in this engagement's history specifies this number), and (c) generalizing `BuildDatedWeek` to assign each `KEY_SESSION` slot (by `SlotOrderInWeek`/`LaneOrdinal`) its own distinct date. This is genuine training-design + algorithm work, squarely inside this phase's own STOP-condition language ("no new progression mathematics," "if any athlete-facing value lacks repository authority").

**Action taken:** the `(Intermediate, 5)` routing widening was reverted (§12) rather than shipped with this bug live. `TEN_K__5D__INTERMEDIATE` is not publicly reachable at any horizon as of this report.

## 12. Revert detail

- `V1CatalogPilotIdentityPolicy.IsSupportedLevelFrequency` — `(Intermediate, 5)` arm removed; a code comment at the removal site names the exact blocker (§11) for whoever picks this up.
- `V1CatalogPilotIdentityPolicy.ResolveCandidate` — the corresponding switch arm removed (was reachable only via the now-removed allow-list entry; removed for consistency, not because it was independently dangerous).
- `V1CatalogPilotIdentityPolicy.FiveDayCandidateKey`/`FiveDayCandidateVersion` constants **retained** (unused, `TEN_K__5D__INTERMEDIATE`/`1`) — a documented pointer to the real, already-published candidate identity, so the next split does not need to rediscover it.
- `Gen5DIntermediatePublicActivationTests.cs` — deleted (it tested a capability that no longer exists after the revert; would fail-to-compile/fail-at-runtime otherwise).
- `Gen3BThreeDayPublicActivationTests.WrongCombination_NeverNearestMatches` and `Phase4F8_2LivePilotRoutingTests.Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog` — both had been edited to use `(Intermediate, 6)` in place of `(Intermediate, 5)` as their "genuinely unsupported combination" test case (since the widening briefly made `(Intermediate, 5)` a valid combination); both reverted to their original `(Intermediate, 5)` literal now that the widening itself is reverted.

## 13. Full regression evidence

Three full `dotnet test backend/RunningApp.sln` runs this session:

1. **Post runtime-wiring, pre-routing-widening**: 3609/3613 initially (4 failures, 3 confirmed transient file-lock/stale-Release-binary collateral from a build racing the test run — clean on isolated rerun — and 1 pre-existing `Sw09ExplicitZeroReadinessEndToEndTests` gap, unrelated: references phases 4F.7C/4F.8.1, the legacy 4D v10 8-week explicit-zero case).
2. **Post routing-widening (before the blocker was found)**: 3611/3613 (the pre-existing `Sw09` failure, plus one genuine new regression in `Phase4F8_2LivePilotRoutingTests` caused by the widening making its `(Intermediate, 5)` "non-pilot" test fixture no longer non-pilot — fixed in that same pass, §12).
3. **Post revert (final, this report's evidence baseline)**: **3612/3613** — only the pre-existing, unrelated `Sw09` gap remains. `dotnet build backend/RunningApp.sln` and `-c Release`: 0 warnings, 0 errors, both.

`plan-catalog/PlanCatalog.sln`: **1510/1510**, unchanged from §6.

## 14. File attribution

| Category | Files |
|---|---|
| `REAL_CATALOG_CONTENT` | `run-layout-5d.v1.json`, `ten-k-master.v7.json`, `ten-k-workout-progression.v6.json`, `intermediate-modifier.v7.json`, `intermediate-progression-modifier.v3.json`, `ten-k-5d-intermediate.v1.json`, 8× `intermediate-5d-*.json` (status promotion only) |
| `PLANCATALOG_BUGFIX` | `TemplateCombinationValidator.cs` (lane-aware fix) |
| `PLANCATALOG_PUBLISHING` | `ICatalogBundleAssembler.cs`, `CatalogBundleAssembler.cs`, `CatalogPublisher.cs` |
| `PLANCATALOG_TEST` | `Intermediate5DRealCatalogIntegrationTests.cs` (new), `PrescriptionBundleProjectionIntegrationTests.cs`, `Intermediate5DProductionPrescriptionProfileSourceTests.cs` (ambiguity fixes) |
| `REAL_RELEASE_ARTIFACT` | `plan-catalog/artifacts/appsel-plan-catalog/1.1.0/` |
| `RUNTIME_BUNDLE_DISCOVERY` | `PublishedTemplateBundleLoader.cs` (new), `PlanCatalogOptions.cs`, `CatalogPreviewGenerator.cs`, `Program.cs`, `appsettings.json`, `RunningApp.Api.csproj` |
| `ROUTING_ATTEMPTED_AND_REVERTED` | `V1CatalogPilotIdentityPolicy.cs` (net: unused candidate-identity constants retained, allow-list unchanged) |
| `TEST_INVENTORY_FIX` | `PlanCatalogDeploymentPackagingTests.cs` (count 78→97, packaging-invariant assertion widened for the new bundle glob) |
| `DOCUMENTATION` | this report |
| `LEDGER` / `ROADMAP` | `PHASE_LEDGER.md`, `MASTER_ROADMAP.md` |
| `UNEXPECTED` | None — the calendar-materializer finding was disclosed, not hidden or worked around |

## 15. What is NOT delivered (explicit, for the next split)

- `TEN_K__5D__INTERMEDIATE` public routing at any horizon (blocked on §11).
- `CatalogWeekSkeletonCalendarMaterializer` multi-KEY-slot generalization — needs a product decision (minimum inter-KEY-session separation) before implementation.
- Preparation Runway (15-20w) / Long-Horizon (21-52w) 5D activation — structurally coupled to the same class of problem, one layer further (§9).
- DB-backed round-trip tests with real 5D content on a live, publicly-routable candidate (blocked — there is no live route to exercise yet).
- Preview/confirmation/Adaptation E2E against a real, publicly-generated 5D plan (same blocker).
- Legacy 3D/4D/Beginner×4D full regression beyond what §13 already covers incidentally.
- `FREQ.6D.4D` parent-phase closure — explicitly not evaluated; the parent remains open pending real public activation.

## 16. Final classification

**`FREQ6D4D_SPLIT_E_PARTIAL_RUNTIME_DISCOVERY_IMPLEMENTED_PUBLIC_ACTIVATION_BLOCKED`**

The complete PlanCatalog-side chain for Intermediate×5D (real RUN_LAYOUT_5D, combination, dual-lane progression, 8 real production profiles, a real published bundle with all 8 `ExecutionPrescriptions`) is implemented and verified — 1510/1510 PlanCatalog.Tests, a genuine CLI-published `1.1.0` release with grep-verified file content. RunningApp's published-bundle runtime-discovery gap (disclosed by Split C) is closed — `IPublishedTemplateBundleLoader` is wired end-to-end through `CatalogPreviewGenerator`, DI, configuration, and deployment packaging. Public activation was attempted for the narrowest possible scope (8-14w Core only, per explicit user decision) and reverted after real E2E testing found a genuine, previously-undisclosed structural blocker in `CatalogWeekSkeletonCalendarMaterializer` — a single-KEY-session-per-week date-assignment algorithm that a 5D week's two KEY_SESSION slots break, requiring both a real algorithm change and an undecided product question (minimum separation between the two weekly KEY sessions). This blocker was disclosed rather than worked around. `FREQ.6D.4D` overall dual-KEY production integration is **not** complete; `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic. The next concrete phase must resolve the calendar-materializer product question before attempting public activation again.
