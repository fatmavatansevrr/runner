# PHASE 10K-GEN.27 — 2D (Beginner + Intermediate) Preparation Runway: Repeating-Pattern Mechanism Implementation

**Phase type**: IMPLEMENTATION + DEFECT-FAMILY SEARCH + REAL DARK VERIFICATION (no HTTP, no public gate change, no PostgreSQL persistence)
**Parent authority**: `GEN.26` (`TWO_D_RUNWAY_LONGHORIZON_PATTERN_CONTINUITY_AND_STRUCTURE_ARCHITECTURE_APPROVED` — the frozen architecture this phase implements against), `GEN.19` (`TWO_D_PREPARATION_RUNWAY_LONGHORIZON_ARCHITECTURE_GAP_CONFIRMED_DEDICATED_DESIGN_PHASE_REQUIRED` — the confirmed absence of a repeating-pattern mechanism this phase builds), `GEN.11` (2D Model B authority, long-run share clamp), `GEN.12` (Core's own repeating-pattern precedent, `WeeklyPatternRoles`/`PatternPeriodWeeks`)
**Execution status**: DONE (PARTIAL)
**This is user-authored "PHASE PROMPT 06b."**

---

## 0. Mandatory startup

`git log -5`: HEAD `0a47d2f` (`docs(gen-26): backfill governance commit SHA for GEN.26`). `git fetch && git diff HEAD origin/main`: no diff, in sync. `git status --porcelain`: only the pre-existing `bin`/`obj` rebuild noise, `baseline_tmp`, `ten-k-pilot-domain-decision-audit.*`, and untracked `TestResults/*.trx` files already present before this session. Next free Phase ID confirmed by direct listing of `PHASE_10K_GEN_*.md`: highest existing is `GEN.26`; `GEN.27` confirmed correct.

Full required reading performed directly from the repository: `PHASE_10K_GEN_26_...md` (in full), `PHASE_10K_GEN_19_...md` (in full), `PHASE_10K_GEN_11_...md` (§1/§4/§6 — long-run share, Model B, Runway continuity), `PHASE_10K_GEN_12_...md` (Core's `WeeklyPatternRoles` precedent — read the real code, not summarized), and direct source reads of `PreparationRunwayWeekMaterializer.cs`, `PreparationRunwayWeekMaterializationContracts.cs`, `TenKPreparationRunwayWeekMaterializationPolicyFactory.cs`, `TenKPreparationRunwayDarkOrchestrator.cs`, `LongHorizonFullNumericOrchestrator.cs`, `LongHorizonStructuralMaterializer.cs`, `CatalogStageToWeekMaterializer.cs` (Core's `ResolveWeekRoles`, the direct model this phase's Runway-side mechanism follows).

---

## 1. Recurring-defect-family search (performed before writing code, per this engagement's standing discipline since `GEN.10`)

Extended `GEN.19`'s own search (which found two additional undisclosed instances beyond `GEN.10`/`GEN.12`/`GEN.17`'s original three) specifically across the Preparation Runway materialization path this phase touches:

```
grep -rn "keyCount\|KeySession.*Count\|KEY_SESSION.*>= *1\|KEY_SESSION.*< *1\|daysPerWeek is not\|DaysPerWeek is not\|daysPerWeek ==\|DaysPerWeek ==" \
  backend/RunningApp.Application/RuntimeCatalog/Schedule/PreparationRunway* \
  backend/RunningApp.Application/RuntimeCatalog/Schedule/LongHorizon --include="*.cs"
```

**Confirmed still open, unchanged since `GEN.19`** (this phase's scope did not touch these; recorded for completeness, not fixed here):
- `PreparationRunwayCoreWeekOnePaceAdapter.FromAuthoritativeCoreBehavior` (`easyCount < 1` floor) — still unreachable for 2D (no 2D Runway→Core continuity path exists yet at the numeric/pace layer; see §5 remainder).
- `LongHorizonStructuralValidator.Validate`'s fixed `expectedKey` assumption — out of scope (LongHorizon explicitly excluded from this phase per its own governing constraints).
- `TenKPreparationRunwayNumericPolicyFactory.Build`'s missing `("TEN_K", *, 2)` switch branch — still absent; confirmed below (§4) as part of this phase's disclosed remainder.
- `TenKPreparationRunwayDarkOrchestrator`'s own admission gate at line 342, newly confirmed this phase: `request.Candidate.Level is not ("INTERMEDIATE" or "ADVANCED")` — excludes Beginner entirely from the one real (if dark) Runway orchestration path that exists today. This is itself an instance of the same "not every caller shape considered" family (here: Level, not DaysPerWeek) `GEN.10` first found — newly disclosed by this phase, not previously listed in `GEN.19 §1`/§3.
- No new `keyCount`/`easyCount`-shaped hardcode instance was found in the two files this phase actually edited (`PreparationRunwayWeekMaterializer.cs`, `TenKPreparationRunwayWeekMaterializationPolicyFactory.cs`) beyond what §2 below already documents and fixes.

---

## 2. What was implemented: the repeating-pattern SELECTION mechanism

This is the real, new engineering `GEN.19 §2` found entirely absent — Preparation Runway had (and still has, for 3D/4D/5D/6D/Advanced, unchanged) exactly one static, fixed weekly role list per `DaysPerWeek`, with no per-week-varying, pattern-period, or week-parity concept anywhere in the data model. Built, directly analogous to `GEN.12`'s Core-side `WeeklyPatternRoles`/`PatternPeriodWeeks` fix (verified against the real `CatalogStageToWeekMaterializer.ResolveWeekRoles` code before building this phase's own version, not assumed from memory):

- **`PreparationRunwayCanonicalWeeklyLayout`** (`PreparationRunwayWeekMaterializationContracts.cs`): two new optional fields, `WeeklyPatternRoles` (nullable list-of-role-lists) and `PatternPeriodWeeks` (nullable int) — both `null` for every pre-`GEN.27` (non-2D) layout, byte-identical to pre-`GEN.27` behavior when null.
- **`PreparationRunwayWeeklyShape.IsValidTwoDayModelB`** (new method, same file): 2D's own Model B shape — exactly one `LONG_RUN`, and exactly one of `{KEY_SESSION, EASY_SUPPORT}` (never both, never neither) — a distinct, explicitly-named shape, never merged into the existing `IsValid`'s own "always 1 KEY + ≥1 EASY" invariant, which 2D's Pattern A (0 EASY) and Pattern B (0 KEY) weeks each genuinely violate by design.
- **`PreparationRunwayWeekMaterializer`** (`PreparationRunwayWeekMaterializer.cs`): `ValidateRequest` now validates every `WeeklyPatternRoles` entry against `IsValidTwoDayModelB` when present (instead of re-checking `OrderedRoles` against the incompatible `IsValid` shape); the main materialization loop now calls a new `ResolveWeekRoles(layout, runwayWeekNumber)` helper — `Pattern[(runwayWeekNumber-1) % PatternPeriodWeeks]` when a pattern is present, `OrderedRoles` unchanged otherwise — exactly mirroring Core's own `ResolveWeekRoles` selection formula (Runway has no TAPER stage of its own, so unlike Core's version there is no stage-key override branch); `ValidateWeekCardinality` now accepts either the standard shape or `IsValidTwoDayModelB`.
- **`TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildLayout`**: new `daysPerWeek == 2` case returning the frozen Model B pattern (`[[KeySession, LongRun], [EasySupport, LongRun]]`, `PatternPeriodWeeks = 2`), with a purely-internal, never-catalog-loaded provenance reference (`PREPARATION_RUNWAY_LAYOUT_2D_MODEL_B_V1`), matching the existing 5D/6D internal-provenance convention.

**Verified real, not fabricated.** `Gen27TwoDayPreparationRunwayDarkVerificationTests.TwoDayRunway_AlternatesModelBPatternByGlobalWeekParity_AcrossFullEightWeekHorizon` exercises the real, unmodified `PreparationRunwayWeekMaterializer` against a real block progression (`TEN_K_GENERAL_ENDURANCE_PROGRESSION`, loaded from the real catalog) across a 5-week horizon, confirming: contiguous global week numbers (1..5, GEN.11 §1/§11's never-reset convention — for a standalone Runway product with no preceding GE segment, the materializer's own contiguous week counter already *is* the global ordinal per `GEN.26` Q1), correct A/B role alternation by week parity (odd = KEY_SESSION+LONG_RUN, even = EASY_SUPPORT+LONG_RUN), and the non-anchor slot filled by the existing, unchanged `EASY_STANDARD` support-policy default (no new catalog authority). `FourDayRunway_LayoutAndBlockRolePolicies_AreByteIdenticalAfterGen27` confirms zero-delta for the unmodified 4D path.

---

## 3. A real defect found via implementation, not theorized: the anchor/content-family reconciliation gap

This phase's original plan (documented in-flight, then disproved, then corrected — not silently smoothed over) was to also generalize `BuildBlockRolePolicies` for 2D by uniformly mapping every block's progression-bound anchor onto `LONG_RUN` (reasoning: `LONG_RUN` is the one role present in both Pattern A and B, so it can safely carry the anchor every week regardless of parity, while `KEY_SESSION` cannot, since it does not exist on Pattern B weeks).

**This hypothesis was built, then run against the real block-progression catalog, and empirically falsified**, not merely reasoned about:

```
Workout 'AEROBIC_STRENGTH_CONTROLLED_INTRO' family 'QUALITY' is incompatible with structural role 'LongRun'.
Workout 'EASY_STANDARD' family 'EASY' is incompatible with structural role 'LongRun'.
```

`TEN_K_CONSISTENCY_PROGRESSION` step 1 and `TEN_K_AEROBIC_STRENGTH_PROGRESSION`'s real anchor content are genuine EASY/QUALITY-family workouts (including the literally Runway-owned-controlled-intensity `AEROBIC_STRENGTH_CONTROLLED_INTRO` — real evidence that `GEN.26` Q2's "Runway-owned KEY-slot content" already exists in the catalog) authored specifically to occupy the `KEY_SESSION` role. `PreparationRunwayWeekMaterializer`'s own family-compatibility check (`LONG_RUN` role requires `LONG_RUN`-family content) correctly rejects forcing this content onto `LONG_RUN`.

Redirecting that content back to `KEY_SESSION` does not resolve the underlying question either: those blocks' progression steps have no guaranteed relationship to which weeks land on Pattern A (global-odd) versus Pattern B (global-even) — the block-relative progression-step system (`AnchorRoleByProgressionStep`, keyed by position within a block) and the global-parity-driven weekly pattern (keyed by absolute week number) are two independently-varying axes with no existing reconciliation. A Pattern B week has no `KEY_SESSION` slot to place a quality anchor into at all.

**This is a genuine, now empirically-confirmed (not merely theorized) architecture question** this phase has no standing to invent unilaterally — matching `GEN.19 §2`'s own STOP discipline and `GEN.12 §6`'s precedent (which escalated `ProgressionStageAllocator`'s own lane/week-eligibility question to a dedicated `GEN.13` classification phase rather than guessing). Candidate resolutions include: constraining block/week allocation so quality-anchor-bearing progression steps always land on Pattern A weeks; a product-approved family-compatibility exception; or a 2D-specific block-role model distinct from the shared one. None of these is a "no new authority" mechanical fix, and none is decided here.

**Disposition**: `TenKPreparationRunwayWeekMaterializationPolicyFactory.BuildBlockRolePolicies(int daysPerWeek)` was parameterized (zero-delta for 3D/4D/5D/6D, verified byte-identical via `FourDayRunway_LayoutAndBlockRolePolicies_AreByteIdenticalAfterGen27`), but the `daysPerWeek == 2` case throws a documented `NotSupportedException` explaining exactly this gap, rather than returning a plausible-looking-but-wrong policy. `TwoDayBlockRolePolicies_ThrowsRatherThanSilentlyMisplacingRealAnchorContent` guards this as a deliberate, permanent regression test, not a TODO.

---

## 4. Explicit remainder — what this phase does NOT complete (honest, itemized)

Per this phase's own required output and this engagement's `GEN.12`/`GEN.19` precedent of an honest `DONE (PARTIAL)` over a forced completion:

1. **Block-role/anchor reconciliation for 2D** (§3) — the actual blocking item for a fully working Runway. Requires a dedicated design decision this phase correctly declines to invent.
2. **`TenKPreparationRunwayNumericPolicyFactory`** — still has no `("TEN_K", *, 2)` dispatch branch (`GEN.17`/`GEN.19`'s own already-disclosed gap, confirmed still open, not touched this phase since it has no real caller to dark-verify against until §3 closes).
3. **`TenKPreparationRunwayDarkOrchestrator`'s admission gates** — `Level is not ("INTERMEDIATE" or "ADVANCED")` still excludes Beginner entirely (newly disclosed, §1); `DaysPerWeek` is not explicitly gated but `BuildLayout`/`BuildBlockRolePolicies` now at least fail loudly for 2D rather than silently.
4. **Preparation Runway calendar composer** (`PreparationRunwayCalendarComposer`/`PreparationRunwayCalendarSkeletonAdapter`) — not touched; still consumes `DaysPerWeek` only as a slot-count parameter with no pattern-awareness, per `GEN.19 §2`'s own finding, unverified for 2-slot weeks.
5. **Long-run share clamp (55-60%) wiring through Runway's own numeric path** — not verified this phase (blocked on item 2: no numeric policy dispatch exists for 2D Runway to apply the clamp within).
6. **The combined Runway+Core plan orchestrator** for a standalone 15-20wk 2D product (Runway weeks 1-8 + Core weeks 9-N, continuing the same global ordinal into Core per `GEN.26` Q1) — not located/built/verified this phase.
7. **Real PostgreSQL persistence and HTTP activation**, both levels — not attempted; no representable end-to-end path exists yet for 2D Runway (same honest basis `GEN.19 §5` used to decline fabricating a synthetic verification around an open architecture gap).
8. **Beginner×2D specifically** — blocked doubly: by items 1-7 generally, and by the `Level` admission gate (item 3) specifically excluding Beginner from the one Runway orchestration path that exists.

None of items 2-8 could be honestly implemented and dark-verified without item 1 closing first — matching `GEN.19 §4`'s own reasoning for why it, too, stopped at investigation rather than forcing partial wiring through an open architectural question.

---

## 5. Zero-delta and constraint confirmation

- **3D/4D/5D/6D/Advanced Runway**: zero-delta, verified not assumed — `FourDayRunway_LayoutAndBlockRolePolicies_AreByteIdenticalAfterGen27` directly asserts `BuildLayout(4)`/`BuildBlockRolePolicies(4)` are unchanged; full regression (§6) confirms no behavioral change anywhere else.
- **2D Core**: untouched — no file under `RunningApp.Application/RuntimeCatalog/Schedule/Materialization` or `Prescription` was modified this phase; only Preparation-Runway-specific files were touched.
- **LongHorizon**: untouched except the two mechanical parameter-threading edits to `LongHorizonFullNumericOrchestrator.cs`/`LongHorizonStructuralMaterializer.cs` required to keep `BuildBlockRolePolicies`'s call sites compiling after it gained a required `daysPerWeek` parameter — both call sites already had `daysPerWeek`/`candidate.DaysPerWeek` in scope and now simply pass it through; neither file's own logic, admission gates, or behavior for any already-supported frequency changed.
- **No new product/numeric authority**: the 2D Model B pattern (`GEN.11 §1`), the global ordinal (`GEN.26` Q1), and the non-anchor `EASY_STANDARD` support-policy convention (already used by every existing frequency) are the only numeric/structural authorities this phase relies on — none invented.
- **Still dark-only**: no route, gate, or `V1CatalogPilotIdentityPolicy` change.

---

## 6. Regression verification

Confirmed no other `dotnet`/`testhost` process before the targeted runs: only persistent MSBuild-node-reuse/VBCSCompiler processes present (standard after `dotnet build`, not test hosts).

Targeted verification first: `--filter "FullyQualifiedName~PreparationRunway"` — **597 total, 597 passed, 0 failed** (includes this phase's 3 new tests plus every pre-existing 3D/4D/5D/6D/Advanced Runway test, confirming zero-delta directly, not merely by inspection).

Full regression required two attempts before an honest, isolated number could be trusted, exactly the contention risk this engagement's own operational discipline warns about — disclosed here rather than smoothed over:

- **Attempt 1**: launched via the harness's own backgrounding, appeared to lose its shell attachment; a second full run was launched believing the first had died. Both were, in fact, real, simultaneously-running `dotnet test` processes against the same PostgreSQL test database. Attempt 1 finished at 36m15s reporting **4233 total, 4225 passed, 8 failed** — the 5 failures beyond the known 3-failure baseline were all `HttpRequestException: 500 (Internal Server Error)` inside `ResetAsync()` (`FitnessEvidenceInputContractTests`, `Freq6D27IntermediateSixDayPublicActivationTests`), the exact signature of two suites concurrently resetting the same test database — not a code regression. Both concurrent processes were killed, confirmed zero `dotnet`/`testhost` processes remaining, before proceeding.
- **Attempt 2 (isolated, trusted)**: single `dotnet test` process, confirmed alone throughout. Result: **4233 total, 4230 passed, 3 failed**, in 36m11s. The 3 failures are the identical, named, pre-existing baseline failures carried since `GEN.17`/`GEN.18`/`GEN.20`/`GEN.23`/`GEN.24`/`GEN.25`: `Gen4EBeginnerFourDayPublicActivationTests.ExplicitZeroAtOrAboveBreakEven_Generates` (weeks 13, 14) and `Sw09ExplicitZeroReadinessEndToEndTests.Sw09Request_ExplicitZeroReadiness_GeneratesFullPreview_UsingExplicitZeroPolicy_NotDefaultOrMissingSubstitution`. Total reconciles exactly: `GEN.25`'s own 4230 total/4227 passed baseline + this phase's 3 new tests (all passing) = 4233 total/4230 passed, same 3 failures, **zero new regressions**.
- `PlanCatalog.Tests` unaffected by construction (no catalog document added, edited, or removed this phase).
- `dotnet build` (Debug): 0 errors throughout, confirmed after every edit round.

---

## 7. Final classification

**`TWO_D_PREPARATION_RUNWAY_REPEATING_PATTERN_MECHANISM_IMPLEMENTED_BLOCK_ROLE_RECONCILIATION_REQUIRED`** — `DONE (PARTIAL)`.

The repeating-pattern SELECTION mechanism `GEN.19 §2` found entirely absent from Preparation Runway is now implemented, real-dark-verified against real catalog content, and zero-delta-confirmed for every existing frequency. Full Runway completion for 2D (both levels, real PostgreSQL, calendar placement, numeric dispatch) is **not** reached this phase: implementation surfaced a genuine, previously-undisclosed, now empirically-confirmed architecture question (block-relative progression-anchor placement vs. global-parity week pattern) that blocks the remaining wiring and requires its own dedicated design decision, matching this engagement's `GEN.12`/`GEN.19` precedent of disclosing an honest partial result rather than forcing a fake completion.
