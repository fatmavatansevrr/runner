# Phase 4L.3 — Long-Horizon Confirmation and Public Preview Wiring

## 1. Executive result

`LONG_HORIZON_CONFIRMATION_AND_PUBLIC_PREVIEW_WIRING_COMPLETED`. Authenticated users can generate a public-safe 21–52-week preview from the real rolling lifecycle and explicitly confirm it into one durable user-owned rolling plan. Existing static and Habit routes remain separate.

## 2. Inherited dark-runtime readiness

This phase reuses the Phase 4L.1 public contract/mapper/validator and the Phase 4L.2–4L.2G PostgreSQL initialization, reconstruction, rollback, idempotency and concurrency authority. It introduces no second planner and changes no numeric, calendar, evidence, direction, checkpoint, retry, Runway or Core formula.

## 3. Scope and exclusions

Scope is the authenticated Race/TenK/Intermediate/4-day v10 pilot at 21–52 complete weeks. Flutter, background activation, downward interpolation and broad Home/Calendar rolling projection are excluded. No commit or push is part of this phase.

## 4. Existing public-flow inspection

The existing race and Habit preview endpoints persist `PlanPreview`; static confirmation uses the authenticated owner and a server-stored preview payload, creates `TrainingPlan`/`TrainingWeek`/`TrainingDay` rows, supports `ConfirmedPlanId` replay and enforces one active plan through a filtered unique index. Its DTO assumes a complete dated numeric schedule and therefore cannot honestly represent Pending rolling weeks. Preview expiry is 30 minutes. Ownership is checked by the internal authenticated user ID. Existing Home/Calendar/detail/completion handlers assume static `TrainingDay` ownership.

## 5. Route decision

Dedicated routes were selected to avoid a breaking or nullable-ambiguous extension of `GeneratePreviewResponse`:

- `POST /api/v1/plans/generate-preview/race/long-horizon`
- `POST /api/v1/plans/confirm/long-horizon`

The Habit path is not overloaded.

## 6. Pilot eligibility

The service accepts exactly Race, TenK, Intermediate, four preferred training days and candidate `TEN_K__4D__INTERMEDIATE v10`. The existing public catalog eligibility gate remains authoritative. Unsupported distance, level or frequency fails through typed validation/pilot errors.

## 7. Horizon routing

`RaceHorizonPolicy.Decide` performs day-accurate StartDate/RaceDate arithmetic and `LongHorizonCompositionResolver` owns the 21–52/53+ distinction. TotalWeeks is never accepted from the client. The dedicated path accepts 21–52; 53+ returns `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW`. Existing 8–14 and 15–20 routes are unchanged.

## 8. Public preview request

The route reuses `GenerateRacePlanPreviewRequest`, its validator and command mapper. User inputs remain goal/race/profile/availability/evidence inputs only. Structural ranges, lifecycle states, contexts, generated sessions, activation ranges and persistence identities cannot be submitted.

## 9. Real preview generation

`LongHorizonPublicPlanService.GeneratePreviewAsync` resolves the canonical horizon/profile, loads the public candidate, invokes `LongHorizonRollingInitialActivationRuntime`, builds the real first executable window and maps its lifecycle state through `LongHorizonPublicPreviewMapper`. `LongHorizonPublicPreviewContractValidator` validates the result before persistence/return. Preview generation creates no confirmed plan or rolling aggregate.

## 10. Public preview response

`LongHorizonPlanPreviewContract` exposes plan summary, complete lightweight structural roadmap, exact current executable sessions/dates/numeric values, public provenance, expiry and confirmation readiness. Future Pending weeks contain structural dates/status only and have no DTO field for numeric sessions or AssignedDate.

## 11. Strategy discriminator

`PlanScheduleStrategy` is independent from `GoalType` and contains `StaticComplete` and `RollingLongHorizon`. The preview and confirmation response state the strategy explicitly. Long-Horizon remains a Race/TenK goal.

## 12. Server-owned preview authority

`PlanPreview` stores the internal initialization snapshot, candidate identity/version, generated/expiry timestamps, confirmation ownership, normalized-input fingerprint, structural-roadmap fingerprint, executable-window fingerprint and independent public/confirmation/persistence contract versions. SHA-256 fingerprints are verified before confirmation. The public DTO alone is never confirmation authority.

## 13. Preview expiry/staleness

Preview lifetime is 30 minutes, matching the static convention. Missing/cross-owner, expired, invalidated, incompatible-version, corrupt JSON, fingerprint mismatch and unavailable candidate authority fail closed. Confirmation does not regenerate a materially different snapshot.

## 14. Confirmation request

`LongHorizonConfirmPlanRequest` contains only `PreviewId` and `ContractVersion`. It has no UserId, schedule, session, roadmap, context, lock, prescription or persistence-ID field.

## 15. Confirmation ownership model

Option A was selected: confirmation creates a real user-owned `TrainingPlan`, persists `RollingLongHorizon`, and links it to the existing `LongHorizonRollingPlanState`. This integrates the existing one-active-plan policy without fabricating static schedule rows.

## 16. Schedule-strategy persistence

The additive `TrainingPlan.ScheduleStrategy` column defaults every existing row to `StaticComplete`. `LongHorizonRollingPlanStateId` is nullable, unique when present and protected by a restrictive foreign key. Rolling identity is explicit rather than inferred from an empty static schedule.

## 17. Confirmation transaction

One explicit PostgreSQL transaction performs owner/version/expiry/fingerprint/candidate checks, rechecks active-plan ownership, calls the existing `InitializeStructuralStateAsync`, creates `TrainingPlan`, updates `PlanPreview.ConfirmedPlanId`, and commits. Any pre-commit exception rolls back all rows.

## 18. Initial confirmed state

The rolling aggregate contains every structural week and the real initial lifecycle/context/window/session ownership. Only the selected initial window is executable. Future weeks remain Pending with null numeric fields. No future `TrainingWeek` or `TrainingDay` placeholder exists.

## 19. Confirmation idempotency

Exact owner+preview replay returns `AlreadyConfirmed` with the existing PlanId. Unique preview source, active-plan and rolling-state ownership constraints prevent duplicate plan/aggregate creation. Replay creates no duplicate structural weeks, activation or sessions.

## 20. Concurrent confirmation

Two independent HTTP requests use separate scopes, DbContexts and PostgreSQL connections. A same-preview race leaves one durable plan; the loser reloads the committed preview and returns the same plan. Different previews for one user produce one active-plan winner and a typed conflict loser.

## 21. Active-plan conflict

An existing active static or rolling `TrainingPlan` blocks confirmation. The conflict check is repeated inside the transaction and backed by the existing filtered database uniqueness rule. A losing preview remains unconfirmed and no rolling state survives.

## 22. Confirmation rollback

Internal test-only failpoints after rolling initialization, after plan ownership, after preview update and before commit prove full rollback. Fresh-context verification finds no plan/aggregate and an unconfirmed reusable preview; corrected confirmation then succeeds once.

## 23. Post-commit acknowledgement loss

A failpoint after commit but before response acknowledgement proves the commit is not rolled back. Retrying the same preview returns `AlreadyConfirmed` and the same PlanId without a second plan or rolling aggregate.

## 24. Confirmation response

`LongHorizonConfirmPlanResponse` returns contract version, PlanId, PreviewId, outcome, explicit strategy, total weeks, public-safe current executable weeks, next Pending boundary, status/message and confirmation time. It does not return the internal snapshot or future numeric output.

## 25. Public error mapping

Typed mappings cover safe not-found/cross-owner, expired, stale/corrupt, active conflict, unsupported pilot, initialization infeasibility, 53+ and deferred rolling read surfaces. Unexpected provider/persistence failures remain generic 500 responses and do not expose SQL/provider detail.

## 26. Blocked preview behavior

If real initial activation cannot safely generate an executable window, the request returns `LONG_HORIZON_INITIALIZATION_INFEASIBLE`; it neither synthesizes a preview nor falls back to a static/full-upfront planner.

## 27. Home/Calendar safety policy

Interim policy A is implemented. When the active plan strategy is `RollingLongHorizon`, existing static Home and Calendar return `LONG_HORIZON_READ_SURFACE_NOT_YET_SUPPORTED` rather than an empty/misleading complete-static response. Confirmation response remains the approved current-window public read surface for this phase.

## 28. Static backward compatibility

The original race preview/confirm routes, 8–14 core behavior, 15–20 Preparation Runway behavior, Habit behavior and static DTO shapes are unchanged. Static persistence continues to create complete `TrainingWeek`/`TrainingDay` schedules and existing clients need not understand Long-Horizon contracts.

## 29. Public leakage guard

Phase 4L.1 reflection/validator tests remain authoritative. Phase 4L.3 serializes real 21/25/52 previews and verifies Pending roadmap rows do not contain AssignedDate or numeric volume. Internal server snapshot, context, target lock, Runway prescription, evidence fingerprint, persistence entities and failure-injection types are absent from Swagger/public DTO graphs.

## 30. Authorization

Controller identity comes exclusively from `ICurrentUserAccessor`. Preview queries include both PreviewId and authenticated owner. A different user receives the same not-found outcome as a nonexistent ID. The confirmed TrainingPlan owner is the authenticated internal user.

## 31. Contract versioning

Public preview V1, confirmation V1, server snapshot V1 and rolling persistence V1 are stored/checkable independently. Unsupported confirmation or stored authority versions return stale/regenerate rather than being inferred from missing data.

## 32. Observability

Structured logs cover preview requested/generated/blocked and confirmation requested/succeeded/replayed/conflict. They carry user-scoped ID, PreviewId/PlanId, horizon and public outcome; they do not log raw evidence, authentication tokens or snapshot/lock/prescription JSON.

## 33. Swagger

Both dedicated routes appear in OpenAPI with public request/response types and rolling examples. Examples show 21–52 eligibility, explicit strategy, expiry and non-numeric Pending rows. `LongHorizonServerPreviewSnapshot` is internal and absent.

## 34. Migration

`20260804142858_Phase4L3LongHorizonPublicConfirmation` is additive: preview authority fields, TrainingPlan strategy default, optional rolling-state FK and unique index. It creates no rolling/static rows and reclassifies no existing plan. It was applied successfully to local PostgreSQL; Down removes only these additions.

## 35. Governance

`TD-LONG-HORIZON-CONFIRMATION-AND-PUBLIC-PREVIEW-WIRING-001` is `CLOSED`. Append-only updates were added to the public-preview, persistence, dark-lifecycle, mixed-completion, rollback, concurrency and volume-redesign records. The volume-redesign TD remains OPEN because full rolling Home/Calendar integration and Flutter remain incomplete.

## 36. Tests

The focused Phase 4L.3/public-preview slice passed 22/22 integration cases covering 20/21/25/52/53 boundaries, unsupported pilot inputs, real preview authority, confirmation, no fake static rows, exact replay, same-preview concurrency, one-active-plan conflict, four rollback stages, acknowledgement loss, owner isolation, expiry/corruption/version errors, typed Home/Calendar safety and Swagger. The complete Long-Horizon slice passed 832/832, the full backend suite passed 3,048/3,048, and the full plan-catalog suite passed 1,248/1,248. The activation-readiness registry parity slice passed 15/15. All reported runs had zero failures and zero skipped tests.

## 37. Flutter/background status

No Flutter file and no background job/worker was changed. No future rolling activation happens automatically. Completion/not-today handlers receive no fake rolling `TrainingDay` IDs and therefore cannot expose Pending work.

## 38. Final classification

`LONG_HORIZON_CONFIRMATION_AND_PUBLIC_PREVIEW_WIRING_COMPLETED`. Public preview and explicit confirmation are live for the scoped candidate/horizon; broad rolling read-model/UI integration is intentionally not claimed.

## 39. Exact next phase

The next authorized phase should design the rolling active-plan read model for Home/Calendar/detail/completion and then Flutter consumption of that contract. It must not change rolling numeric/calendar formulas or silently enable background activation.
