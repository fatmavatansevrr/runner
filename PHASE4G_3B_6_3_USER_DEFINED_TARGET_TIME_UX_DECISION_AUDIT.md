# Phase 4G.3B.6.3 — UserDefined Goal-Time Hard-Block Product/UX Decision Audit

**Read-only decision audit. No code changed. No TD updated. Nothing implemented.**

This document answers "Open decision 1" recorded in `TD-NOTEVALUATED-FALLBACK-001`
(`plan-catalog/artifacts/audits/activation-readiness-risks.json`, updated Phase
4G.3B.6.2): *"Is hard-blocking UserDefined target-time users without recent-race
evidence the intended product behavior? If it is intended, what actionable
guidance should the user receive?"* See section 16 for the explicit reconciliation.

---

## 1. Current observed runtime behavior

Confirmed by Phase 4G.3B.6.1's real HTTP end-to-end characterization
(`backend/RunningApp.IntegrationTests/GoalFeasibilityNotEvaluatedUserDefinedCharacterizationEndToEndTests.cs`),
re-confirmed here by direct source re-read, not re-executed in this pass (no code
was run beyond the validation checks in section "Validation" below):

**A. ProductAverage** (`target_finish_time_seconds=3480, target_finish_time_source=product_average, recent_race=null`)
→ `GOAL_FEASIBILITY_IN=Evaluated/CHALLENGING/PACE_SOURCE_TARGET_TIME_PRODUCT_AVERAGE_ACCEPTED` → HTTP 200, 12-week schedule.

**B. UserDefined** (`target_finish_time_seconds=3480, target_finish_time_source=user_defined, recent_race=null`)
→ `GOAL_FEASIBILITY_IN=NotEvaluated/PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE` →
`NotEvaluatedReasonClassifier`=`Unsupported` → `CatalogPreviewGenerator.ApplyNotEvaluatedGovernancePolicy`
throws `RuntimeConditionUnsupportedException` → `GlobalExceptionHandler` → **HTTP 422,
errorCode `RUNTIME_CONDITION_UNSUPPORTED`** → no schedule, no stage allocation, no
workout binding, no `PlanPreview`/`TrainingPlan`/`Week`/`Day` persistence.

---

## 2. Current frontend flow

Traced through `mobile/lib/features/onboarding/presentation/` and
`mobile/lib/features/onboarding/data/onboarding_provider.dart`:

1. **Recent-race entry happens *before* goal-time entry.** `RecentRaceResultPage`
   is reached only from `RunnerBackgroundDetailsPage` ("Reached only for
   Intermediate/Advanced/Experienced" — Beginner skips it entirely), which itself
   precedes `GoalTimePage` in the flow (confirmed by `GoalTimePage`'s own back-button
   logic, which returns to `runnerBackgroundDetails`/`runningBackground`). It is
   explicitly labeled **"Recent race result (optional)"** in the UI
   (`runner_background_details_page.dart:180`). **Consequence: by the time the user
   reaches the goal-time screen, `state.recentRaceResult` is already fixed** — a
   client-side guard on the goal-time screen or immediately after it has everything
   it needs, with no additional round trip.
2. **`GoalTimePage`** (`goal_time_page.dart`) offers exactly two actions: an
   `OutlinedButton` labeled *"Go with average pace (...)"* which calls
   `setProductAverageTarget` (atomic value+source write), and a bottom
   `AppPrimaryButton` labeled *"Continue"* which **unconditionally** calls
   `setUserDefinedTarget(totalSeconds)` on the manually-scrolled wheel value —
   even if that value happens to numerically equal the average. There is **no
   warning, hint, or visual distinction on this screen today** about the
   independent-evidence requirement, and no reference at all to whether a recent
   race was or wasn't entered earlier.
3. **`onboarding_provider.dart`** atomically pairs `targetFinishTimeSeconds` +
   `targetFinishTimeSource` on every write (`setProductAverageTarget`/
   `setUserDefinedTarget`), and separately holds `recentRaceResult` (nullable).
   `_generateRacePreview` builds `GenerateRacePlanPreviewRequestDto` directly from
   this state — `recentRace` is sent `null` whenever `state.recentRaceResult` is
   `null`, and **also forced `null` for Beginner regardless of any stored value**
   (not relevant to the Intermediate pilot's own exposure, but confirmed present).
4. **The real network call is currently dead code.** In
   `plan_generation_page.dart._startGeneration()`, the actual
   `ref.read(onboardingProvider.notifier).generatePreview()` call — together with
   its `try/catch` and `planGenerationUserSafeMessage(e)` error-mapping call — is
   **entirely commented out**, replaced by a `"TEST SHORTCUT"` that jumps straight
   to a mock Home screen (`useMockHomeDataProvider`) after a fixed animation delay.
   **No real user, through the current shipped onboarding flow, can reach this 422
   today** — the error UI described below exists as inert scaffold code, not a
   live path.
5. **The (currently unreachable) error UI** is generic: `_buildError()` renders a
   fixed *"Generation Failed"* heading, the raw `_error` string (which would be
   whatever `planGenerationUserSafeMessage` returns), and a single **"Try Again"**
   button that blindly re-invokes `_startGeneration()` with the exact same request
   — no differentiated recovery actions (no "use average" / "add recent race" /
   "edit goal" buttons) exist anywhere in the current UI.
6. **`plan_generation_error_mapper.dart`** has zero special-case handling for
   `RUNTIME_CONDITION_UNSUPPORTED` — it falls through to the generic
   `if (error is ApiException) return error.message;` branch, meaning if this path
   were live today, **the raw backend message string** (which embeds the reason
   code inside technical prose, e.g. `"GOAL_FEASIBILITY_IN could not be evaluated
   (reasonCode=PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE)..."`) **would be
   shown to the user verbatim.**

---

## 3. Current API error visibility

`backend/RunningApp.Api/ErrorHandling/ApiErrorResponse.cs` — the fixed envelope
for every 4xx/5xx response — has exactly three fields:

```csharp
public required string ErrorCode { get; init; }     // "RUNTIME_CONDITION_UNSUPPORTED"
public required string Message { get; init; }        // free-form prose, includes reasonCode as text
public required string CorrelationId { get; init; }
```

- **No structured `conditionType`/`reasonCode`/`allowedRecoveryActions` field
  exists.** The reason code is embedded only in `Message`'s free-form prose.
- `RUNTIME_CONDITION_UNSUPPORTED` is **shared** by every `NotEvaluatedReasonCategory.Unsupported`
  case across all four resolvers (`TIME_ADEQUACY_INSUFFICIENT_DECISION_REQUIRED`,
  `PACE_SOURCE_NONE_TARGET_TIME_REQUESTED`, `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`,
  `PACE_SOURCE_ESTIMATED_NO_APPROVED_METHOD` — see `NotEvaluatedReasonClassifier.cs`),
  so **`errorCode` alone cannot distinguish this specific scenario** from other
  unsupported-condition cases.
- Mobile's `ApiClient._mapError` (`api_client.dart:158-196`) parses exactly
  `errorCode`/`message`/`correlationId` into `ApiException` and nothing else — it
  has no reason-code field to parse even if it wanted to.
- **Conclusion for Q4:** the client today can distinguish this specific case
  from other `RUNTIME_CONDITION_UNSUPPORTED` cases **only by parsing free-form
  message text** — exactly what Decision Principle 7 says not to do. As currently
  built, reliable case-specific frontend branching on this exact scenario is
  **not achievable** without either a structured field or fragile string matching.

---

## 4. Supported recovery actions (verified executable without any backend change)

| Action | Supported today? | Evidence |
|---|---|---|
| **Switch to ProductAverage** | **Yes**, with one caveat | `setProductAverageTarget` exists and atomically pairs value+source. Caveat: the validator (`GenerateRacePlanPreviewRequestValidator.cs:62-77`) requires `TargetFinishTimeSeconds` to *exactly equal* `CanonicalTargetFinishTimePolicy`'s value for the goal distance when source=ProductAverage — the client must overwrite the user's typed seconds with the canonical value, not merely flip the source tag while keeping the custom number, or the request fails 400. |
| **Add a complete RecentRace** | **Yes** | `RecentRaceResultPage` already exists, is optional, and is reachable earlier in the same onboarding flow (or via back-navigation to it). `RecentRaceInput`/`RecentRaceRequest` are already fully wired end to end. |
| **Retry unchanged** | **Yes, but useless** | Mechanically supported (`_startGeneration` re-runs with identical state) but deterministic — will reproduce the identical 422 every time, since nothing about the request changed. |
| **Change goal type** (e.g. to Habit) | **Yes, structurally** | `GenerateHabitPlanPreviewRequest` has no `TargetFinishTimeSource`/target-time fields at all — switching goal type entirely sidesteps this condition. Not a "fix," a different product. |

## 5. Unsupported or hypothetical recovery actions (would require a code change)

| Action | Supported today? | Why not |
|---|---|---|
| **Remove target finish time / continue without one** | **No** | `TargetFinishTimeSeconds` is a `required` field on `GenerateRacePlanPreviewRequest` (`GenerateRacePlanPreviewRequest.cs:51`) and the validator rejects `<= 0`. There is no "no target time" state representable in the current Race contract at all. |
| **Silent current-fitness fallback (plan generated without using the goal pace)** | **No** | Confirmed unreachable in Phase 4G.3B.6.1: `ApplyNotEvaluatedGovernancePolicy` throws before `ProgressionStageAllocator`/stage scheduling ever runs for this specific `Unsupported`-classified reason. The allocator's own `FallbackStageKey` mechanism exists in code but is never reached for this trigger — building UX copy or logic that assumes this fallback fires today would be describing behavior that does not exist. |
| **Backend silently substituting ProductAverage on the server side** | **No** (not implemented, and see Decision Principle 2 — would require explicit user consent even if implemented) | No code path does this; would require a new resolver/validator branch. |

---

## 6. Four-option comparison — independently evaluated

Each option evaluated on its own merits against the Decision Principles and
repository evidence, before any comparison to the hypothesis.

### OPTION A — Keep current generic 422 only (no frontend change)

- **Safety:** High — backend fail-closed behavior is unchanged and already proven correct (never silently substitutes evidence).
- **Transparency:** Low — user sees either nothing (current dead-code state) or, if the commented-out path were restored as-is, a raw technical message string (`planGenerationUserSafeMessage`'s generic `error.message` fallback) with no explanation of *why* or *what to do*.
- **User-intent preservation:** Neutral — nothing is silently changed, but nothing is explained either.
- **Implementation complexity:** None (already the status quo).
- **Backend change required:** None.
- **Frontend change required:** None (or, to even become live, only the pre-existing commented-out call needs uncommenting — a separate, unrelated piece of work already noted as dead code, not part of this decision).
- **API-contract change required:** None.
- **Risk of misleading the user:** Low-to-moderate — not misleading, but unhelpful; a user reading a raw `PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE`-flavored message has no actionable path forward and may abandon onboarding.
- **Compatibility with current governance:** Fully compatible; changes nothing this audit's principles would flag.
- **Recommendation status: `ACCEPTABLE_WITH_CONDITIONS`** — acceptable purely as the safety-net floor already in place, but insufficient as the sole user-facing behavior once the real call path is restored, because it violates Decision Principle 4 ("prefer actionable guidance over generic errors").

### OPTION B — Keep backend fail-closed, add frontend prevention/recovery

- **Safety:** High — identical backend behavior to Option A; adds a purely additive client-side guard that cannot itself introduce an unsafe plan (it either lets a request through unchanged or stops it before submission with explicit user choices).
- **Transparency:** High — the user is told, in plain language, before submission, why a custom time can't currently be used and what their concrete options are.
- **User-intent preservation:** High — nothing is silently changed; the user explicitly chooses among "use average," "add a recent race," or "go back and edit," each of which maps to an already-existing, already-wired state mutation (`setProductAverageTarget`, `RecentRaceResultPage`, back-navigation).
- **Implementation complexity:** Low-to-moderate — one new guard condition (`targetFinishTimeSource == userDefined && recentRaceResult == null`) evaluable entirely from state already resident in `OnboardingState` at or after the goal-time screen; no new backend/API work required for the guard itself.
- **Backend change required:** None.
- **Frontend change required:** Yes — a new guard/prompt UI and (per section 4's caveat) correct handling of the ProductAverage-seconds-must-match-canonical rule if that recovery path is offered.
- **API-contract change required:** None for MVP (see Q5 for an optional, separate improvement).
- **Risk of misleading the user:** Low — copy must be worded carefully to satisfy Decision Principle 1 (never imply the goal is impossible; explain evidence, not capability) and Q6 below, which this document flags explicitly as a copy-quality risk to manage, not eliminate structurally.
- **Compatibility with current governance:** Fully compatible with all 8 Decision Principles, verified individually in the analysis above.
- **Recommendation status: `RECOMMENDED`.**

### OPTION C — Automatic ProductAverage substitution (client or backend silently changes source)

- **Safety:** Nominally high (ProductAverage is already a safe, evaluated path) — but this masks a **user-intent violation**, which this audit treats as its own category of harm, not merely a UX nicety.
- **Transparency:** **Very low** — directly violates Decision Principle 1 ("do not silently claim to use a user-entered target if the plan does not use it"). A user who typed a specific goal time would receive a plan silently paced to a different number without being told.
- **User-intent preservation:** **Violated** — directly contradicts Decision Principle 2 ("do not automatically alter `TargetFinishTimeSource` without explicit user consent").
- **Implementation complexity:** Low (trivial to implement) — complexity is not the problem; correctness is.
- **Backend change required:** Only if implemented server-side (not implemented today).
- **Frontend change required:** Only if implemented client-side (not implemented today).
- **API-contract change required:** None, but would silently repurpose the meaning of an existing, already-`required`, already-audited field (`TargetFinishTimeSource`) — see `TD-GOAL-FEASIBILITY-001`'s own closure history, which exists specifically *because* conflating "product's own planning reference" with "a value attributed to the user" was previously identified as unsafe and fixed by adding this exact field to keep the two distinct.
- **Risk of misleading the user:** **High.**
- **Compatibility with current governance:** **Directly incompatible** with Decision Principles 1 and 2, and in tension with the entire rationale that produced `TargetFinishTimeSource` in the first place (`TD-GOAL-FEASIBILITY-001`, Phase 4D.4.1).
- **Recommendation status: `NOT_RECOMMENDED`.**

### OPTION D — Silent current-fitness fallback (plan generated without using the requested goal pace)

- **Safety:** Nominally plausible in the abstract (a plan without goal-pace guidance is not inherently dangerous) — but is **not what the system actually does today** (see section 5): this path is confirmed structurally unreached by Phase 4G.3B.6.1. Recommending it as an "option" without first building it would be recommending a fiction.
- **Transparency:** **Very low** — same Decision-Principle-1 violation as Option C, arguably worse: the user receives a schedule that quietly ignores their stated goal entirely, with no explanation their target time was disregarded.
- **User-intent preservation:** **Violated** — the user's target time is discarded without consent.
- **Implementation complexity:** Would require reintroducing exactly the ambiguity `TD-NOTEVALUATED-FALLBACK-001` itself already flags as unresolved (distinguishing resolver indecision from resolver rejection) *and* deciding it in favor of "fall back" for this specific reason category — a decision this audit's own principles (3, 4) counsel against making silently.
- **Backend change required:** Yes, substantial — would need `NotEvaluatedReasonClassifier` to reclassify this specific reason code out of `Unsupported` and into a category that reaches the allocator, plus explicit product sign-off that `CURRENT_FITNESS_SPECIFIC_REHEARSAL` is an acceptable substitute here.
- **Frontend change required:** Yes — the user-visible plan would need explicit "this plan doesn't use your custom goal time" messaging to avoid Principle-1 violation, which does not exist today.
- **API-contract change required:** Likely — `fallback_used`/`fallback_reason` are currently vestigial/legacy-only fields (see Phase 4G.3B.6.1 finding); making this option honest would require wiring real fallback visibility into the response, which does not exist today.
- **Risk of misleading the user:** **High**, unless extensively re-engineered first.
- **Compatibility with current governance:** **Directly incompatible** with Decision Principles 1, 2, and 3 as currently implementable; would also require re-opening exactly the ambiguity `TD-NOTEVALUATED-FALLBACK-001` was created to flag, not resolve.
- **Recommendation status: `NOT_RECOMMENDED`.**

### Comparison table

| | A | B | C | D |
|---|---|---|---|---|
| Safety | High | High | High (masks intent) | Uncertain / not built |
| Transparency | Low | High | Very low | Very low |
| User-intent preservation | Neutral | High | Violated | Violated |
| Implementation complexity | None | Low-moderate | Low | High |
| Backend change | None | None | Optional | Yes, substantial |
| Frontend change | None | Yes | Optional | Yes |
| API-contract change | None | None (MVP) | None | Likely yes |
| Misleading risk | Low-moderate | Low | High | High |
| Governance compatibility | Full | Full | Violates P1/P2 | Violates P1/P2/P3 |
| **Status** | ACCEPTABLE_WITH_CONDITIONS | **RECOMMENDED** | NOT_RECOMMENDED | NOT_RECOMMENDED |

**The evidence independently supports Option B.** This happens to match the
hypothesis offered in the prompt, but the reasoning above was derived by
evaluating all four options against the Decision Principles and repository
capabilities directly — not by assuming the hypothesis was correct. Where the
evidence diverges from the hypothesis's framing is noted in section 7.

---

## 7. Recommended MVP decision (and divergence from the hypothesis framing)

**Recommendation: Option B — keep backend fail-closed; add a frontend
pre-preview guard; keep the 422 as defense-in-depth.**

This **matches** the hypothesis's chosen option, but this audit's reasoning
**diverges from the hypothesis's framing in three specific ways**, stated
explicitly per the task's instruction not to let the hypothesis's detail bias
the conclusion:

1. **The hypothesis presumed the guard could be evaluated "when TargetFinishTimeSource
   = UserDefined and complete RecentRace evidence is absent" as if these two
   facts were available simultaneously at an arbitrary point.** This audit found
   something stronger and more specific: because `RecentRaceResultPage` is
   reached *earlier* in the real onboarding order than `GoalTimePage`, the guard
   condition is **fully resolvable already at the goal-time screen itself**
   (Option 2A — "on the goal-time input screen" — see Q2), not merely "before
   preview request" in the abstract (Option 2C). This is a stronger, earlier
   prevention point than the hypothesis implied, and should be the primary
   layer, with a defense-in-depth restatement on "Continue"/pre-submission as a
   secondary layer (Q2's "multiple layers," Option E).
2. **The hypothesis's "Use ProductAverage" recovery option is not a plain
   toggle** — this audit found the validator requires the seconds value to
   exactly match the canonical average, so this recovery action must **replace**
   the user's typed number, not merely retag it. Any implementation must call
   the existing atomic `setProductAverageTarget(canonicalSeconds)` setter, not a
   hypothetical "just flip the source" operation. This is a correction to the
   hypothesis's implicit assumption, not a contradiction of its choice.
3. **The hypothesis is silent on the fact that the real network call is
   currently dead code.** This audit found the entire live error path is
   currently bypassed by a test shortcut. The MVP recommendation therefore has
   an implicit precondition not stated in the hypothesis: *restoring the real
   `generatePreview()` call path is a prerequisite* for any of this guard/recovery
   work to matter in production — today, no real user can reach this 422 at
   all, so the guard's practical value is currently zero until that separate,
   unrelated piece of dead code is reactivated. This audit does not recommend
   reactivating it as part of this decision (out of scope — a separate,
   apparently deliberate "TEST SHORTCUT" whose own removal is its own decision),
   but flags it as a fact any implementation phase must account for.

**Reasoning distinct from the hypothesis's own framing:** the decisive factor
for this audit was not "the hypothesis sounds reasonable" but the direct
finding that (a) every ingredient of the guard condition is already resident,
pre-computed, and correctly ordered in `OnboardingState` with zero new backend
work, (b) every recovery action the guard would offer is independently verified
executable today (section 4), and (c) the two rejected alternatives (C, D) each
concretely violate a numbered Decision Principle this audit was told to apply,
not merely "seem worse." The convergence with the hypothesis is a confirmation
by independent evidence, not an assumption carried in from the prompt.

---

## 8. Recommended future enhancements (not MVP)

- **Q5's structured error contract** (see section 11) — removes the free-form-message-parsing
  fragility identified in section 3, once product confirms the guard's value
  justifies an API-contract change.
- **Honest fallback surfacing** — if a future product decision *does* choose to
  implement a real current-fitness (or other) fallback for some NotEvaluated
  category, `fallback_used`/`fallback_reason` must become real, non-vestigial,
  user-visible fields (today they are legacy/always-false per Phase 4G.3B.6.1) —
  this is a precondition for Option D ever becoming acceptable, not a
  recommendation to build it now.
- **A future goal-time feasibility model** — an approved methodology for
  estimating pace from training-volume evidence alone would let some
  UserDefined-without-recent-race users receive a genuine `Evaluated` result
  instead of being blocked at all; this is exactly the deferred question already
  tracked by `TD-PACESOURCE-001` (ESTIMATED path never emitted) — this audit
  does not duplicate that TD, only cross-references it as the eventual
  structural fix that could shrink Option B's blocked population over time.

---

## 9. Turkish UX copy (draft)

> **DISCLAIMER — MANDATORY, READ BEFORE USING ANY TEXT BELOW:** The following
> Turkish copy is a **draft starting point only**. It has **not** been reviewed
> against any brand voice, tone, or localization guideline, because none was
> supplied to this audit. It must be re-approved by whoever owns product
> copy/tone before any implementation phase uses it verbatim. Only the
> **meaning and required information content** (per Q6) are the actual
> deliverable of this section — the specific wording is illustrative only.

**Inline prevention (goal-time screen, shown when a custom time is selected and no recent race is on file):**
> "Bu özel hedef süreyi şu anda plan hızının temeli olarak kullanamıyoruz çünkü
> destekleyecek yakın zamanlı bir yarış sonucunuz yok. Hedefinizin imkansız
> olduğu anlamına gelmez — sadece güvenle kullanmak için yeterli bağımsız
> kanıtımız yok."

**422 recovery screen:**
> "Plan Oluşturulamadı — Özel hedef sürenizi kullanmak için yeterli bilgimiz yok.
> Aşağıdaki seçeneklerden birini deneyin."

**Action buttons:**
> "Ortalama hızla devam et" · "Yakın zamanlı bir yarış sonucu ekle" · "Hedefi düzenlemeye dön"

**Accessibility-friendly explanation (screen-reader-visible long form):**
> "Girdiğiniz hedef bitiş süresi kabul edilmedi çünkü sistemin bu süreyi güvenle
> kullanabilmesi için ya ortalama bir hız referansına ya da yakın zamanlı bir
> yarış sonucuna ihtiyacı var. Hedefiniz reddedilmedi; sadece plan temeli olarak
> henüz kullanılamıyor."

---

## 10. English UX copy (draft)

> **DISCLAIMER — MANDATORY, READ BEFORE USING ANY TEXT BELOW:** The following
> English copy is a **draft starting point only**. It has **not** been reviewed
> against any brand voice, tone, or localization guideline, because none was
> supplied to this audit. It must be re-approved by whoever owns product
> copy/tone before any implementation phase uses it verbatim. Only the
> **meaning and required information content** (per Q6) are the actual
> deliverable of this section — the specific wording is illustrative only.

**Inline prevention (goal-time screen, shown when a custom time is selected and no recent race is on file):**
> "We can't currently use this custom goal time as your plan's pace basis,
> because there's no recent race result to support it. This doesn't mean your
> goal is out of reach — we just don't have enough independent evidence yet to
> use it safely."

**422 recovery screen:**
> "We Couldn't Build Your Plan — We don't have enough information to use your
> custom goal time yet. Try one of the options below."

**Action buttons:**
> "Use the average pace instead" · "Add a recent race result" · "Go back and edit my goal"

**Accessibility-friendly explanation (screen-reader-visible long form):**
> "The finish time you entered wasn't accepted, because the system needs
> either an average pace reference or a recent race result to use it safely.
> Your goal hasn't been rejected — it just can't be used as the plan's basis
> yet."

**Explicit content rule applied to all copy above (Q6, Decision Principle 1):**
none of the drafted strings state or imply the goal is impossible, unrealistic,
or unachievable — every variant frames the block strictly as an *evidence*
gap, never a *capability* judgment.

---

## 11. Suggested machine-readable error contract (Q5 — not implemented)

Proposed, **not applied**, additive-only extension of `ApiErrorResponse`:

```csharp
public sealed class ApiErrorResponse
{
    public required string ErrorCode { get; init; }         // unchanged: "RUNTIME_CONDITION_UNSUPPORTED" (preserved for back-compat)
    public required string Message { get; init; }            // unchanged: human-readable, non-localized technical detail
    public required string CorrelationId { get; init; }       // unchanged
    public string? ConditionType { get; init; }                // new, optional: "GOAL_FEASIBILITY_IN"
    public string? ReasonCode { get; init; }                    // new, optional: "PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE"
    public IReadOnlyList<string>? AllowedRecoveryActions { get; init; } // new, optional: ["USE_PRODUCT_AVERAGE", "ADD_RECENT_RACE", "EDIT_GOAL"]
}
```

Design rationale against the Decision Principles:
- **Preserves the generic top-level `RUNTIME_CONDITION_UNSUPPORTED`** (Q5's own
  instruction) rather than minting a narrower top-level code like
  `TARGET_TIME_REQUIRES_INDEPENDENT_EVIDENCE` — the new fields carry the
  specificity instead, so existing generic-`errorCode`-based client handling
  (if any exists elsewhere) is not broken.
- **Additive/optional fields only** — existing clients that ignore unknown JSON
  properties continue to work unchanged (backward compatible by construction;
  `System.Text.Json` ignores unmapped properties by default in this codebase's
  existing convention, confirmed by `GenerateHabitPlanPreviewRequestValidatorTests`'s
  own extra-key-ignored assertion referenced in this session's prior audit).
- **`ReasonCode` is a stable machine token, not localized text** — satisfies
  Decision Principle 6 (separate localized copy from machine-readable codes)
  and Principle 7 (removes the need to parse `Message`'s free-form prose for
  reliable branching).
- **`AllowedRecoveryActions` is deliberately a closed, enumerable token list**,
  not free text — lets the frontend render exactly the buttons that are
  actually valid for this specific failure without hardcoding per-reason-code
  UI logic on the client, and lets backend remain authoritative over what
  recovery is actually possible (Decision Principle 8) rather than the
  frontend guessing.
- **Localization implication:** none of the new fields are localized — only
  `Message` (already English/technical, already not shown verbatim per this
  audit's recommendation) carries prose; all user-facing copy would be
  client-side string tables keyed by `ReasonCode`, consistent with Principle 6.

---

## 12. Suggested analytics events (Q9 — not implemented)

All fields listed are non-sensitive: no raw health data, no exact free-form
user-typed values (times/paces are bucketed or omitted, never logged verbatim),
no unnecessary personal data.

| Event | Fields | Notes |
|---|---|---|
| `onboarding_goal_time_manual_selected` | `goal_distance`, `has_recent_race: bool` | Fired when the user commits a custom time via "Continue" (not "Go with average"). No raw seconds logged. |
| `onboarding_preview_blocked_missing_evidence` | `goal_distance`, `reason_code: "PACE_SOURCE_TARGET_TIME_NO_INDEPENDENT_EVIDENCE"` | Fired when the frontend guard intercepts before submission (Option B primary layer) — distinguishable from a real 422 (see next row). |
| `onboarding_preview_422_runtime_condition_unsupported` | `goal_distance`, `error_code: "RUNTIME_CONDITION_UNSUPPORTED"` | Fired only if the guard is bypassed/fails and the real 422 is hit — should be rare if Option B's guard is effective; a nonzero rate here is itself a useful guard-effectiveness signal. |
| `onboarding_goal_time_switched_to_average` | `goal_distance` | Fired when the user chooses the "use average instead" recovery action from the guard/recovery screen. |
| `onboarding_recent_race_added_from_recovery` | `goal_distance` | Fired when the user reaches `RecentRaceResultPage` via the recovery flow specifically (vs. the normal upstream entry point) — requires a navigation-source flag, not built today. |
| `onboarding_abandoned_after_goal_time_block` | `goal_distance`, `screen: "goal_time" \| "recovery"` | Fired on app backgrounding/exit while the block/recovery state is showing — measures drop-off cost of this hard-block. |
| `onboarding_retry_after_block_succeeded` | `goal_distance`, `recovery_action_taken` | Fired when a subsequent `generatePreview()` call succeeds after a prior block/422 in the same session. |

---

## 13. Explicit non-goals of this audit

- Does not implement any frontend guard, copy, or API field.
- Does not restore the commented-out real `generatePreview()` call path in
  `plan_generation_page.dart` — that is a separate, already-existing decision
  this audit only surfaces as a fact, not a recommendation to act on here.
- Does not decide whether/how to implement a real current-fitness (or other)
  fallback for any NotEvaluated category — that remains explicitly open per
  `TD-NOTEVALUATED-FALLBACK-001`'s "Open decision 3" (independently, whether any
  NotEvaluated reason that DOES reach the allocator uncontested should keep
  silently falling back).
- Does not approve, finalize, or localize any UX copy — see the mandatory
  disclaimers in sections 9-10.
- Does not modify, close, or re-scope `TD-NOTEVALUATED-FALLBACK-001`.
- Does not evaluate or recommend a specific ESTIMATED-pace methodology
  (`TD-PACESOURCE-001` remains the owner of that separate question).

---

## 14. Open product decisions (not resolved by this audit)

1. **Copy approval** — the drafted TR/EN copy in sections 9-10 needs sign-off
   from whoever owns product tone/localization; this audit cannot make that
   call.
2. **Whether/when to restore the real `generatePreview()` call path** — until
   this happens, none of Option B's guard has any live user impact; this is a
   prerequisite decision outside this audit's scope.
3. **Whether `AllowedRecoveryActions`/structured reason-code fields (section 11)
   are worth the API-contract change**, or whether the frontend-only guard
   (which needs no backend change at all) is judged sufficient indefinitely.
4. **Analytics instrumentation ownership and event-naming-convention conformance**
   — the names in section 12 are illustrative; the actual analytics/telemetry
   system's existing naming convention (if one exists elsewhere in the
   repository) was not located during this audit and should be confirmed
   before implementation.
5. **Whether recent-race entry should ever become non-optional** — evaluated in
   this audit's Q8 analysis (below) and explicitly rejected as a recommendation,
   but remains a decision only product can make permanently.

**Q8 analysis (recent-race requiredness):** Making `RecentRace` required would
directly contradict its current, deliberate, UI-labeled "(optional)" status and
would force every Intermediate+/UserDefined-goal user through an extra step
regardless of whether they even want a custom goal time — a strictly worse
default for the majority of users who are well-served by "Go with average."
**Not recommended.** Recent race should remain optional; Option B's guard
already gives users who *choose* a custom time a clear, contextual reason to
add one, without penalizing everyone else.

---

## 15. Implementation phases (not started)

1. **Phase A (frontend-only, no backend/API change):** Add the `OnboardingState`-driven
   guard on `GoalTimePage`'s "Continue" action (and, defensively, immediately
   before `generatePreview()` submission as a second layer) using the
   already-existing `targetFinishTimeSource`/`recentRaceResult` fields; wire the
   three recovery actions to already-existing setters/navigation
   (`setProductAverageTarget(canonicalSeconds)`, navigate to
   `RecentRaceResultPage`, pop back to goal-time edit). Requires copy approval
   (Open Decision 1) first.
2. **Phase B (prerequisite, independent):** Decide whether/when to restore the
   real `generatePreview()` call in `plan_generation_page.dart` — without this,
   Phase A's guard has no live effect (Open Decision 2).
3. **Phase C (optional, API-contract):** If Phase A's guard proves insufficient
   in practice (e.g. the guard's own condition drifts out of sync with backend
   validation over time), implement the structured error-contract extension in
   section 11, add reason-code-keyed client string tables, and add automated
   contract tests proving frontend/backend reason-code sets stay in sync.
4. **Phase D (future, out of this audit's scope):** Revisit `TD-PACESOURCE-001`'s
   ESTIMATED-pace methodology question, and/or `TD-NOTEVALUATED-FALLBACK-001`'s
   "Open decision 3" (whether allocator-reached NotEvaluated cases should keep
   silently falling back), independently of this UX decision.

---

## 16. Reconciliation with TD-NOTEVALUATED-FALLBACK-001's "Open decision 1"

The TD's exact recorded wording (Phase 4G.3B.6.2): *"Product/UX decision: is
hard-blocking UserDefined target-time users without recent-race evidence
(HTTP 422 RUNTIME_CONDITION_UNSUPPORTED) the intended product behavior for the
live 12-week pilot? If so, what actionable guidance should the user receive
(e.g. suggesting 'go with average' or completing a recent-race entry) instead
of a generic unsupported-condition error?"*

**This audit answers that question as follows:** Yes — the backend hard-block
(HTTP 422, fail-closed) **should remain** the underlying safety behavior
(Option B, section 7), because no safer alternative is currently supported by
the repository's actual capabilities (Options C and D are `NOT_RECOMMENDED`,
section 6) and reversing it would either violate Decision Principles 1-2 or
require unbuilt infrastructure. The **actionable guidance** the TD's own
wording anticipated — "suggesting 'go with average' or completing a
recent-race entry" — is exactly what this audit's Option B recommendation and
Q6 copy drafts (sections 7, 9-10) specify, plus a third option ("edit the
goal") the TD's wording did not enumerate but this audit found is also already
fully supported. This audit does **not** modify the TD itself (out of scope
per this pass's explicit instruction); a future documentation-only pass may
fold this conclusion back into the TD's own text once the MVP recommendation
here is itself reviewed/accepted.
