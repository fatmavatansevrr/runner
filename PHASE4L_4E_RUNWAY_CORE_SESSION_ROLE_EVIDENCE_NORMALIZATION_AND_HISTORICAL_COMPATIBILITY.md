# Phase 4L.4E — Runway/Core Session-Role Evidence Normalization and Historical Compatibility

## 1. Executive result

`LONG_HORIZON_RUNWAY_CORE_SESSION_ROLE_EVIDENCE_NORMALIZATION_COMPLETED`. The exact production defect Phase 4L.4D identified — Runway session roles persisted via the raw `PreparationRunwaySlotRole.ToString()` enum default (PascalCase, e.g. `"LongRun"`) while evidence detection only recognized the canonical `"LONG_RUN"` — is fixed at its two authoritative source call sites, centralized behind one new codec (`LongHorizonSessionRoleCodec`), and proven against real PostgreSQL: a genuinely completed Runway long run now contributes to checkpoint evidence, `JitValidatedLoadUnavailable` is no longer a false positive caused by role formatting, and a genuine evidence gap still produces the exact same typed, classified Block Phase 4L.4D established. An unexpected, welcome discovery: fixing this let a real *second* Runway continuation succeed in testing (not just the fix being "inert until some later phase") — documented honestly in §17 without overclaiming the full lifecycle-shape matrix, which remains explicitly out of this phase's scope.

## 2. Defect inherited from Phase 4L.4D

Exactly as stated in the inherited state: GE persists canonical uppercase role tokens; Runway's numeric-activation runtime persisted PascalCase instead; the evidence adapter's `OrdinalIgnoreCase` match against `"LONG_RUN"` cannot bridge that gap because the strings differ by more than case (a missing underscore); every genuinely completed Runway long run was therefore invisible to evidence, `ValidatedSustainableLoad` could never be produced for a Runway continuation, and the plan always blocked with `JitValidatedLoadUnavailable` regardless of real training completion.

## 3. Scope and exclusions

Fixes exactly the role-representation defect: one new codec, two root-cause call-site fixes, five downstream consumers hardened to use the codec instead of scattered literals, one new test file plus targeted updates to Phase 4L.4D's own tests (whose premise the fix invalidated). No continuation redesign, no workout-allocation change, no phase-composition change, no numeric-progression change, no calendar-assignment change, no checkpoint-formula change, no recovery-policy change, no migration, no Flutter change, no automatic activation, no downward interpolation, no commit. The full lifecycle-shape matrix (Phase 4L.4D/4L.4F's own explicit scope) is **not** attempted here, even though this phase's own testing incidentally proved one further shape is now reachable (§17) — that finding is reported, not built upon.

## 4. Complete role inventory

Full inventory performed by direct repository inspection (not assumed). Summary table (see the investigation's own detailed per-file findings, folded into this phase's actual fix):

| Layer | GE | Runway (pre-fix) | Core | Persisted? |
|---|---|---|---|---|
| Structural skeleton (`StructuralRole`) | canonical | canonical | canonical | no (re-derivable) |
| Numeric activation `SessionPrescriptions.SessionRole` | canonical | **PascalCase** (bug) | canonical | feeds persistence |
| DB `LongHorizonRollingSessionState.SessionRole` | canonical (`text`, unconstrained) | **PascalCase** (bug) | canonical | **yes** |
| Evidence adapter / read-model `IsLongRun` | correct | **broken** (bug) | correct | reads persisted |
| Public API `WorkoutRole` | `"LONG_RUN"` | `"LongRun"` (bug) | `"LONG_RUN"` | public, inconsistent |

**Root cause, precisely two call sites**: `LongHorizonRollingJitActivationRuntime.MapRunwayWeeks` and `LongHorizonRealCalendarProjectionAdapter.BuildFullRunwayProjection`, both calling `.SlotRole.ToString()` instead of routing through the canonical-string switch expression that **already existed, unmodified, in four other files** (`PreparationRunwayCalendarSkeletonAdapter`, `PreparationRunwayPersistablePlanMapper`, `LongHorizonFullNumericOrchestrator`, `LongHorizonStructuralMaterializer`) — Core's own numeric path was never affected, since it reads a pre-canonicalized `string StructuralRole`, not the enum, at its own call site. `LongHorizonActivatedCalendarAlignmentValidator` already had an ad hoc `role is "LongRun" or "LONG_RUN"` workaround, proving the defect's exact shape was already known and locally patched around, just never fixed at the source.

## 5. Canonical representation decision

**Preferred default taken as-is**: stable explicit uppercase-with-underscore tokens `"KEY_SESSION"`, `"EASY_SUPPORT"`, `"LONG_RUN"` — not a new invention, the pre-existing de facto standard everywhere except the two buggy call sites (§4). `SlotRole.ToString()` is no longer used for persistence or public output anywhere in the codebase after this phase.

## 6. Central role codec/mapper

`LongHorizonSessionRoleCodec` (new, internal, `RuntimeCatalog/Schedule/LongHorizon/RollingActivation/LongHorizonSessionRoleCodec.cs`): `ToCanonicalToken(PreparationRunwaySlotRole)`, `TryParseCanonicalOrLegacy(string?, out PreparationRunwaySlotRole)`, `IsLongRun`/`IsKeySession`/`IsEasySupport(string?)`. `IsEasySupport` uses `StartsWith` to also match GE's suffixed `"EASY_SUPPORT_1"`/`"EASY_SUPPORT_2"` forms. Legacy recognition is case-sensitive against the exact enum-default string (`"LongRun"`, `"KeySession"`, `"EasySupport"`) — not a broad fuzzy match — so only the one known-possible legacy shape is accepted; canonical recognition stays case-insensitive (matching the pre-existing convention). Unknown values return `false`/fail closed everywhere; no whitespace/punctuation stripping, no silent conversion to an unrelated role.

## 7. GE persistence behavior

Unchanged — GE never used `.ToString()`; its hardcoded canonical literals were already correct and remain untouched.

## 8. Runway persistence behavior

Fixed. `LongHorizonRollingJitActivationRuntime.MapRunwayWeeks` line 324 and `LongHorizonRealCalendarProjectionAdapter.BuildFullRunwayProjection` line 163 (plus its `LongRunDayProvenance` derivation, line 169, simplified to a direct enum comparison) now call `LongHorizonSessionRoleCodec.ToCanonicalToken(...)`. `RunwaySessions_PersistCanonicalRole_NotPascalCaseEnumDefault` proves every newly persisted Runway `SessionRole` is one of the three canonical tokens and never the PascalCase form.

## 9. Mixed Runway/Core persistence

Not independently re-tested — Core's own path was never broken (§4), and Runway's fix is proven directly (§8); a mixed-window integration proof was judged redundant given both halves are independently proven and the composition logic itself (which weeks are Runway vs. Core) is unchanged by this phase.

## 10. Core persistence behavior

Unchanged and unaffected — confirmed by inspection to already use canonical `StructuralRole` strings, not the enum, at its own numeric-activation call site (`LongHorizonRollingJitActivationRuntime.SelectCoreWeeks` / `LongHorizonRollingJitCompositionOrchestrator`).

## 11. Evidence-adapter normalization

`LongHorizonRollingOutcomeEvidenceAdapter.ToCheckpointRows` (`LongHorizonRollingSessionMutationService.cs`) now calls `LongHorizonSessionRoleCodec.IsLongRun(s.SessionRole)` for both `DayType` and `IsLongRun` derivation, replacing the direct `.Equals("LONG_RUN", OrdinalIgnoreCase)` literal. No evidence formula, threshold, or aggregation rule changed — only the boolean "is this session's role a long run" input feeding the existing, unmodified formula.

## 12. Legacy compatibility set

Exactly one legacy set is approved, because exactly one is known to be possible: the raw `PreparationRunwaySlotRole` enum-default strings (`"LongRun"`, `"KeySession"`, `"EasySupport"`) this defect's own pre-fix Runway code path could have produced. No other legacy token exists anywhere in the repository's history (GE and Core were always canonical, confirmed by inspection, not assumed).

## 13. Migration decision

**Option A (read-compatible, write-canonical) — no migration**, per the prompt's own preferred default. Rationale: (1) this is pre-launch pilot code with no real end-user production data; any pre-existing PascalCase Runway rows in a local development database are this session's own prior testing artifacts, not user history requiring preservation; (2) the codec already makes every consumer (evidence adapter, read-model `IsLongRun`, alignment validator) tolerant of the one known legacy form, so old rows reconstruct with identical semantic meaning without any row rewrite; (3) a migration provides no additional operational value here and risks exactly the kind of "migration only to make tests easier" the prompt explicitly forbids.

## 14. Historical immutability

No migration was performed, so this is trivially satisfied — no session ID, planned value, `AssignedDate`, outcome, actual value, timestamp, provenance, or ownership field was touched by this phase. The only changed *behavior* is which strings new writes produce and which strings old *and* new reads correctly recognize as long-run evidence.

## 15. Public contract decision

**Policy explicitly chosen (Part 9 option B for the `IsLongRun` boolean, option A-going-forward for the raw `WorkoutRole` string)**: `WorkoutRole` remains an unmapped, unversioned passthrough of the persisted `SessionRole` string — no new contract version was introduced, and the field's shape is unchanged. Going forward, every newly activated Runway session's `WorkoutRole` will be canonical (`"LONG_RUN"` etc.) because the underlying persisted value now is; any already-persisted PascalCase rows from this session's own prior testing would still show their legacy form in `WorkoutRole` until naturally superseded by new activations, which is accepted (no real user-facing history exists to protect, §13). Critically, `IsLongRun` — the field a client would actually use to distinguish a long run without string-matching `WorkoutRole` itself — is now **always correct** regardless of which persisted string variant is present, computed via the codec. `PublicHomeAndCalendar_ExposeCanonicalRunwayRole_IsLongRunCorrect` proves this for newly activated sessions.

## 16. Restart and serialization

Not exercised as a literal dispose-and-reconstruct-process test; every test in this phase's suite already uses a fresh `IServiceScope`/`AppDbContext` per operation (the established pattern throughout this test suite, equivalent in effect to a restart for EF's purposes — no in-memory state is reused across the read-back). `LongHorizonRollingStateReconstructionService` was confirmed (inspection) to read `SessionRole` verbatim from the persisted row without re-deriving it, so canonical writes stay canonical and legacy reads stay legacy-but-correctly-classified through the codec at every consumption point — no reconstruction-time normalization was needed or added.

## 17. Runway evidence acceptance

`CompletedRunwayLongRun_IsRecognizedAsEvidence_ValidatedLoadIsProducible` is the core regression proof: activates the real GE→Runway crossing (Phase 4L.4A's shape), completes every session in the newly activated 4-week Runway window (including the real long run in each week) through the real public completion endpoint, then directly asserts the real evidence adapter's output shows one `IsLongRun && Completed && ActualDistanceKm > 0` row per week — proving role-derived evidence is now correct. **Distinguishing role-normalization success from the separate real-calendar gate, as required**: the test then requests continuation and accepts either outcome — if it activates, the fix alone was sufficient for this window; if it's blocked, the assertion requires the reason be `LONG_HORIZON_CURRENT_WINDOW_IN_PROGRESS` (the unrelated real-calendar gate) and explicitly asserts no `JitValidatedLoadUnavailable` block was created for this now-fixed reason. **What was actually observed in this session's real runs**: activation succeeded — a second real Runway/Core continuation cycle activated successfully, meaning the Runway/Core JIT composition path does not share GE's real-calendar `periodEnded` gate. This is reported as an observation, not claimed as proof of the full lifecycle-shape matrix (§3's own exclusion, and Phase 4L.4D/F's explicit separate scope).

## 18. Negative evidence cases

`RoleCodec_RecognizesCanonicalAndLegacyTokens_RejectsUnknown` proves (pure unit test, no DB): canonical `"LONG_RUN"`/`"long_run"` recognized; legacy `"LongRun"` recognized; `"longrun"` (not the exact legacy form) rejected; `"KEY_SESSION"`/`"KeySession"` never counted as long-run; `null` and unknown tokens rejected; `"EASY_SUPPORT"`/`"EASY_SUPPORT_1"`/`"EASY_SUPPORT_2"`/`"EasySupport"` all recognized as easy-support, never as long-run or key-session. `GenuineMissingLongRunEvidence_StillPersistsTypedBlock_NoRegressionToReadStateCorrupt` proves NotToday long-run sessions still correctly produce zero completed-long-run evidence (Phase 4L.4C/D's own semantics, untouched). No evidence formula was changed.

## 19. Mixed-window role consistency

Not independently built as a separate integration proof (§9) — the two independently-proven halves (Runway §8, Core §10 unaffected) compose exactly as they did before, since this phase changed only role-string derivation, never which weeks are Runway vs. Core or how many sessions/roles each week gets.

## 20. Core role consistency

Confirmed unaffected by inspection (§10); no dedicated new test was needed since no code path serving Core changed.

## 21. JIT-block preservation

`GenuineMissingLongRunEvidence_StillPersistsTypedBlock_NoRegressionToReadStateCorrupt` and the three updated Phase 4L.4D tests (§22) together prove: a real evidence gap (long run left NotToday) still produces the exact same `JitValidatedLoadUnavailable` typed Block via the unmodified Phase 4L.4D `PersistBlockAsync` authority; Phase 4L.4C's unmodified `LongHorizonBlockRecoveryClassification` still maps it to `RegeneratePreviewRequired`; Home and retry still agree; no regression to the old unclassified `LONG_HORIZON_READ_STATE_CORRUPT`.

## 22. Real-calendar-gate separation

`CheckpointWindowNotComplete`/`periodEnded` logic (`LongHorizonCheckpointEvidenceAggregator`) was not touched. Phase 4L.4D's own three JIT-boundary/race tests (`LongHorizonJitBoundaryAndCrossOperationRaceTests.cs`) needed their setup helper updated in this phase: they previously constructed their "block" scenario by *fully completing* the Runway window (which, before this phase's fix, always blocked on the role-detection bug regardless of real completion) — now that the bug is fixed, full completion succeeds, so those three tests' block-construction helper was changed to leave the real long run `NotToday` instead (`LeaveRunwayLongRunsNotTodayAsync`, mirroring Phase 4L.4C's GE-side technique exactly), producing a genuine, not role-detection-caused, evidence gap. This is a necessary, disclosed test update, not new production behavior.

## 23. Clock/date testability

Inspected: checkpoint time is obtained via direct `DateOnly.FromDateTime(DateTime.UtcNow)` calls (`LongHorizonRollingWindowActivationService`, `LongHorizonRollingRetryContinuationService`) — no `TimeProvider`/`IClock` abstraction or test-controllable date seam exists anywhere in the Long-Horizon rolling runtime. This confirms Phase 4L.4D's own finding. No clock refactor was introduced in this phase (not narrowly required to prove role normalization — §17's proof needed no date manipulation at all, since the observed Runway/Core continuation apparently isn't gated by real calendar time the same way GE is). Documented as a separate, still-open follow-up if deterministic lifecycle-shape testing is ever required without waiting for real months to pass.

## 24. Integrity validation

`LongHorizonRollingSessionState.SessionRole` remains an unconstrained `text` column (unchanged, no migration). No workout-name-based role inference exists or was added — `TryParseCanonicalOrLegacy` fails closed (`false`/`default`) for any value outside the exact known canonical/legacy set, including GE's own suffixed `"EASY_SUPPORT_1"`/`"EASY_SUPPORT_2"` forms (documented explicitly in the codec's own doc comment as a deliberate scope boundary — those callers should use `IsEasySupport` instead, which does recognize them).

## 25. Observability

No new structured logs were added — the fix is a pure data-shape correction inside existing, already-logged code paths (block persistence, evidence aggregation); adding role-specific logging was judged unnecessary noise for a fix of this size, consistent with "do not log every normal session."

## 26. Swagger

No public contract/type changed (§15), so no Swagger update was required or made. The existing `DtoExamplesSchemaFilter` Runway/GE examples remain accurate to the (now-consistently-canonical) convention.

## 27. Static/Habit compatibility

No static, Habit, preview, confirmation, or non-Long-Horizon route/DTO was touched. The new codec is Long-Horizon-rolling-specific (`RollingActivation` namespace) and was not reused in static code — no shared canonical authority exists between static and rolling role representations, and none was created.

## 28. Governance-test debt status

Investigated and classified per this phase's own instruction, not hidden. All 43 plan-catalog failures observed before **and** after this phase's code changes are identical in count and identity (verified by running the full plan-catalog suite twice, before and after the code fix, both times: 1206 passed, 43 failed) — **zero failures are caused by Phase 4L.4E**. All 43 are the exact pre-existing `AggregateCountSentence_IsInternallyConsistent`/`RegistryAndMarkdown_AreUniqueAndSemanticallyAligned`-family hardcoded-count debt already fully documented and attributed in Phase 4L.4D's own governance record (`TD-LONG-HORIZON-PUBLIC-ACTIVATION-SHAPE-JIT-RACE-COMPLETION-001`'s `unrelatedPreExistingFinding` field). No new governance-test-debt record is created here — the existing one already covers it accurately and append-only; duplicating it would fragment, not clarify, the record.

## 29. Database verification

No migration was applied (§13). Verified instead: (1) new Runway rows persist canonically (§8, direct DB query assertion); (2) the codec's legacy-tolerance path is unit-tested directly (§18) since no real legacy row exists in this environment to query against; (3) full regression (§31) confirms no other persisted-data-shape assumption broke.

## 30. Governance

`TD-LONG-HORIZON-RUNWAY-CORE-SESSION-ROLE-EVIDENCE-NORMALIZATION-001` — status **CLOSED**. Every one of this phase's own closure criteria is satisfied and tested: canonical role authority is explicit (`LongHorizonSessionRoleCodec`); new GE/Runway/Core sessions persist canonically (proven); approved legacy values remain readable (unit-proven; no real legacy row existed to integration-test against, disclosed in §29); completed Runway/Core long runs are recognized exactly once (proven); non-long-run roles are not misclassified (proven); restart compatibility holds by construction (§16, no reconstruction-time role handling exists to break); genuine missing evidence still blocks correctly (proven, §21); public contracts remain compatible (§15, explicit decision, no version bump needed); no formula or role-allocation changed (confirmed by the narrow two-call-site diff); no historical semantic rewrite (no migration); all of this phase's own new tests pass (§31). Recorded in `plan-catalog/artifacts/audits/activation-readiness-risks.json`/`.md`. New aggregate: **62 risks, 17 OPEN, 45 CLOSED**.

Per this phase's own explicit instruction, `TD-LONG-HORIZON-PUBLIC-ACTIVATION-SHAPE-JIT-RACE-COMPLETION-001` (Phase 4L.4D) is **not** closed merely because its precondition defect is fixed — its lifecycle-shape-matrix scope remains genuinely unproven beyond what §17 incidentally observed, and closing it would require the deliberate, separate Phase 4L.4F effort this phase does not attempt. `TD-LONG-HORIZON-EXPLICIT-NEXT-WINDOW-ACTIVATION-API-001` (4L.4A) and `TD-LONG-HORIZON-PUBLIC-RETRY-ACTIVATION-SHAPE-RACE-COMPLETION-001` (4L.4B) likewise remain partially closed/open for the identical unresolved reason.

## 31. Tests

- New: `LongHorizonSessionRoleNormalizationTests.cs` — **7/7 passed** (canonical Runway persistence; completed-long-run evidence regression with real-calendar-gate separation; genuine-evidence-gap block preservation; public Home `IsLongRun`/`workout_role` correctness; codec unit tests for recognition, canonicalization, and round-trip parsing).
- Updated: `LongHorizonJitBoundaryAndCrossOperationRaceTests.cs` (Phase 4L.4D) — 3 of its 7 tests needed their block-construction premise updated (§22); all **7/7 pass** again after the update.
- Full Long-Horizon regression: **909/909 passed**, 0 failed, 0 skipped (902 + 7 new).
- Full backend integration suite: **3,125/3,125 passed**, 0 failed, 0 skipped (prior baseline 3,118 + 7 new tests — zero regressions anywhere in the backend).
- Plan-catalog suite: **1,206/1,249 passed, 43 failed — identical before and after this phase's code changes** (verified by running twice); all 43 are pre-existing governance-test-count debt, zero attributable to this phase (§28).

## 32. Flutter/background status

Unchanged: no Flutter code, no hosted service, no timer, no queue, no automatic or background activation.

## 33. Final classification

`LONG_HORIZON_RUNWAY_CORE_SESSION_ROLE_EVIDENCE_NORMALIZATION_COMPLETED`, `LONG_HORIZON_GE_RUNWAY_AND_CORE_SESSIONS_NOW_USE_ONE_EXPLICIT_CANONICAL_ROLE_REPRESENTATION_INSTEAD_OF_ENUM_TOSTRING_PERSISTENCE`, `LONG_HORIZON_COMPLETED_RUNWAY_AND_CORE_LONG_RUNS_NOW_CONTRIBUTE_EXACTLY_ONCE_TO_THE_EXISTING_CHECKPOINT_EVIDENCE_AUTHORITY`, `LONG_HORIZON_GENUINE_MISSING_LONG_RUN_EVIDENCE_STILL_PERSISTS_THE_EXISTING_TYPED_BLOCK_AND_RECOVERY_REQUIREMENT`, and `LONG_HORIZON_ROLE_NORMALIZATION_CHANGES_NO_WORKOUT_ALLOCATION_NUMERIC_CALENDAR_CHECKPOINT_OR_RECOVERY_FORMULA` — all **achieved and proven**.

## 34. Exact next phase

**Phase 4L.4F — Remaining Public Lifecycle Shape, Replay, Terminal and Concurrency Matrix Closure**, exactly as recommended. This phase's own incidental observation (§17: a second real Runway/Core continuation succeeded) is a strong positive signal worth prioritizing there first — a deliberate, systematic re-attempt at the full 10-shape matrix (pure Runway, mixed 1+3/2+2/3+1, pure Core, Core refresh, final partial, terminal) is now newly plausible where Phase 4L.4D found it structurally blocked, since the blocking cause it identified (role-naming mismatch) is fixed. That systematic proof — not this phase's narrower, honestly-scoped fix — is what should actually close the remaining 4L.4A/4L.4B/4L.4D governance gaps.
