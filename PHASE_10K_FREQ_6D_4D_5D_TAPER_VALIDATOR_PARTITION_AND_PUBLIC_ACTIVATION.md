# Phase 10K-FREQ.6D.4D.5D — Taper Prescription-Context Validator Partition & Intermediate×5D Public Activation Retry (Third Attempt)

**Implementation + integrated verification. Both real Taper-completeness blockers `FREQ.6D.4D.5B`/`5C` disclosed are implemented and verified. Public activation was retried a third time and reverted again: real E2E testing found a third, genuinely independent, out-of-scope blocker (a public workout-type mapping gap, unrelated to Taper). `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic.**

## 1. Preflight

`PHASE_LEDGER.md` row 80: `FREQ.6D.4D.5C`, `EVIDENCE + ARCHITECTURE_DECISION`, `DONE`, `TAPER_COMPLETENESS_EXISTING_AUTHORITY_CONFIRMED_IMPLEMENTATION_DEFECT`, confirmed. Commits `042dac7`/`d9adb9a` confirmed reachable from `HEAD`. `PHASE_10K_FREQ_6D_4D_5C_TAPER_COMPLETENESS_AUTHORITY_DECISION.md`, `PHASE_10K_FREQ_6D_4D_5B_MULTI_KEY_CALENDAR_MATERIALIZATION_IMPLEMENTATION.md`, and the `FREQ.6D.4D.3` Split-C report all re-read directly, not reconstructed from chat.

Repository truth re-verified before any change: (1) `TAPER_SHARPEN` originates in `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY` (Phase 4F.7D); (2) it injects synthetic sharpening semantics into a legacy `EASY_STANDARD` runtime path; (3) never canonical Taper vocabulary; (4) Legacy 3D/4D/Beginner×4D genuinely still depend on it (confirmed: their real progressions author only `TAPER_SHARPEN`, no other Taper stage); (5) ProfileBacked sessions already have a separate, real completeness authority — `BoundCatalogSession.PrescriptionProfileKey`/`Version` → `CatalogSessionPrescriptionSource.ProfileBacked` → `ExecutionPrescriptionIndex.ResolveExact` → fail-closed `CatalogSessionPrescriptionMissingExecutionPrescriptionException`/exact-resolution exceptions, all pre-existing and unmodified; (6) therefore no new generic Taper-completeness algorithm was built here — only wiring/scoping of what already exists.

Durable baseline: local `HEAD` = `d9adb9a`, `origin/main` = `13594ac`, ahead `7`/behind `0`, working tree clean except the two pre-existing unrelated `plan-catalog/artifacts/audits/*` files and `baseline_tmp`, `git diff --check` clean. Docker/Postgres (`appsel-dev-postgres`) confirmed healthy throughout. No pre-assigned phase ID existed; this phase uses `FREQ.6D.4D.5D`, continuing the established sub-split sequence.

## 2. 5C authority (applied, not reopened)

`FREQ.6D.4D.5C`'s decision was applied exactly as approved: partition Taper-completeness validation along the existing Legacy/ProfileBacked classification, preserving the exact Legacy check unchanged and exempting ProfileBacked instances (their completeness already proven downstream). No new completeness model, no new metadata, no stage renaming — confirmed by direct diff review of every file changed (§6).

## 3. Original TAPER_SHARPEN provenance (re-confirmed, not re-derived)

`PHASE4F_7D_TAPER_SHARPEN_AND_FINAL_PRESCRIPTION_VALIDATION.md`, re-read: `V1_TAPER_SHARPEN_PRESCRIPTION_POLICY v1`'s qualifying identity is exactly `PhaseKey=TAPER, ProgressionStageKey=TAPER_SHARPEN, StructuralRole=KEY_SESSION, WorkoutDefinitionKey=EASY_STANDARD` — the same tuple both validators fixed in this phase hardcoded.

## 4. Existing validator defect (found to be TWO occurrences, not one)

`FREQ.6D.4D.5C` scoped its finding to `CatalogPrescriptionContextValidator`. Real E2E testing in this phase found a **second occurrence of the identical root cause**, not disclosed by any prior report: `CatalogFinalPrescribedPlanValidator.Validate` (a separate, later-pipeline-stage validator, run after runtime dose finalization) independently hardcoded `taperSharpen.Count != 1` using the same `V1TaperSharpenPrescriptionPolicy.IsTaperSharpen` identity check, unconditionally, for every plan regardless of Legacy/ProfileBacked classification. This is the same class of bug FREQ.6D.4D.5C already has full decision authority over (not a new domain question) — both were fixed under the identical, already-approved partition principle.

## 5. Files inspected

`CatalogPrescriptionContextBuilder.cs` (full re-read, both `BuildSessionContext` and `CatalogPrescriptionContextValidator.Validate`); `PrescriptionContracts.cs` (`CatalogSessionPrescriptionContext`'s field list — confirmed `PrescriptionProfileKey`/`Version` were never threaded through, exactly as `FREQ.6D.4D.5C` found); `BoundCatalogPlanContracts.cs` (`BoundCatalogSession.PrescriptionProfileKey`/`Version`, confirmed already real, both-null-or-both-non-null convention); `CatalogSessionPrescriptionPlanner.cs` (`ResolvePrescriptionSource` — the real, unmodified, already-fail-closed Split-C classification/resolution boundary, confirmed partial-lineage/missing-execution/wrong-version are already independently typed and fail-closed there); `CatalogFinalPrescribedPlanValidator.cs` (full read — found the second `TAPER_SHARPEN` occurrence); `CatalogSessionPrescriptionContracts.cs` (`CatalogPrescribedSession.PrescriptionSource`, confirmed the real, already-existing `CatalogSessionPrescriptionSource` discriminated union was already threaded to this later stage — reused directly, not rebuilt); `V1TaperSharpenPrescriptionPolicy.cs` (`IsTaperSharpen`'s exact 4-field identity, confirmed identical to the context validator's check); `Phase4F7APrescriptionContextTests.cs` (the existing `CatalogPrescriptionContextBuilder` fixture pattern, reused as the template for this phase's own new test file); `V1CatalogPublicWorkoutTypeMappingPolicy` (`CatalogPublicPreviewMaterializer.cs`, read only after the third blocker was found, to diagnose it precisely — not modified).

## 6. Files changed

**Taper validator partition** (4 files):
- `PrescriptionContracts.cs` — `CatalogSessionPrescriptionContext` gained `PrescriptionProfileKey`/`PrescriptionProfileVersion` (both nullable, additive).
- `CatalogPrescriptionContextBuilder.cs` — `BuildSessionContext` now copies both new fields verbatim from `BoundCatalogSession`. `CatalogPrescriptionContextValidator.Validate`'s inline `TAPER_SHARPEN_CONTEXT_MISSING` check replaced with a call to a new `ValidateTaperCompleteness` method implementing the approved partition (Legacy instances: every one must independently match the exact identity, unchanged; ProfileBacked instances: exempt; partial lineage: new, distinct `TAPER_KEY_SESSION_PARTIAL_PROFILE_LINEAGE` error, never silently treated as Legacy).
- `CatalogFinalPrescribedPlanValidator.cs` — the inline `taperSharpen.Count != 1` check replaced with a new `ValidateTaperCompleteness` method applying the identical partition principle, using the already-existing `CatalogPrescribedSession.PrescriptionSource` discriminated union (no new field needed at this stage — it was already there).
- `V1TaperSharpenPrescriptionPolicy.cs` — class-level doc comment added, explicitly scoping it `LEGACY_TAPER_RUNTIME_ONLY` (§39 debt/comment disposition, below). No behavioral change.

**Test** (1 new file):
- `Freq6D4D5DTaperCompletenessPartitionTests.cs` — 10 new direct unit tests against `CatalogPrescriptionContextBuilder.Build` (no dedicated test of this validator existed before this phase, confirmed by `FREQ.6D.4D.5C`'s own search).

**Public routing (attempted three times this session, reverted three times; net change is documentation only)**:
- `V1CatalogPilotIdentityPolicy.cs` — widened, tested, reverted. Doc comments updated to record the real, final, third blocker found.
- `Phase4F8_2LivePilotRoutingTests.cs`/`Gen3BThreeDayPublicActivationTests.cs` — briefly edited during the widening window, reverted to original assertions.
- `Gen5DIntermediatePublicActivationTests.cs`, `Freq6D4D5DDevRealCatalogProbeTests.cs` — written during the attempt, deleted after the revert (tested a capability that was reverted; the probe file was scratch-only, never intended to be committed).

## 7. Classification wiring (§4 of the originating prompt)

Minimum additive lineage only: `BoundCatalogSession.PrescriptionProfileKey`/`Version` (real, pre-existing, Split B) threaded one struct further into `CatalogSessionPrescriptionContext`. Never derived from stage name, workout key, `DoseCategory`, `LaneOrdinal`, or bundle presence — confirmed by direct code review of `BuildSessionContext`, which copies the two fields verbatim and touches nothing else new.

## 8. Legacy partition result

Unchanged behavior, now explicit rather than implicit: every Legacy (both-fields-null) Taper `KEY_SESSION` instance must independently match `ProgressionStageKey=="TAPER_SHARPEN" && WorkoutDefinitionKey=="EASY_STANDARD"`. For the single-lane 3D/4D/Beginner×4D shape this is identical to the pre-existing `sessions.Any(...)` check. Proven: `LegacyValidTaperSharpenIdentity_Passes`, `LegacyMissingTaperSharpenIdentity_Fails`.

## 9. ProfileBacked partition result

ProfileBacked (both-fields-non-null) Taper `KEY_SESSION` instances are exempt from the literal identity check entirely — their completeness is proven downstream, not re-checked here. Proven: `ProfileBackedTaperPrimaryStage_PassesWithoutTaperSharpenIdentity`, `ProfileBackedTaperSecondaryStage_PassesWithoutTaperSharpenIdentity`, `FullRealFiveDayTaper_BothLanesProfileBacked_Passes`, and `ArbitraryValidProfileBackedStageName_DoesNotRequireTaperSharpen` (a deliberately non-real, arbitrary stage name — proving validity comes from classification, not any string).

## 10. Partial-lineage result

Exactly one of `PrescriptionProfileKey`/`Version` set is never treated as Legacy — it is its own distinct, always-fail-closed condition (`TAPER_KEY_SESSION_PARTIAL_PROFILE_LINEAGE`), kept separate from `TAPER_SHARPEN_CONTEXT_MISSING` per the required failure taxonomy. Proven: `PartialProfileLineage_AlwaysFails_NeverTreatedAsLegacy` (both directions — key-only and version-only).

## 11. Exact-execution guarantee (untouched, not duplicated)

`CatalogSessionPrescriptionPlanner`'s `ResolvePrescriptionSource` (Split C) is the sole classification/resolution boundary for missing-execution-index, missing-profile, and wrong-profile-version — confirmed unmodified by this phase (zero diff), and confirmed via `git diff` that `CatalogPrescriptionContextValidator`/`CatalogFinalPrescribedPlanValidator` never call `ExecutionPrescriptionIndex.ResolveExact` or inspect `ExecutionPrescriptions` (§16 of the originating prompt). These downstream guarantees were already exercised end-to-end by this session's earlier public-activation attempts (the E2E 500s in §16/§21 below show the pipeline genuinely reaching and correctly enforcing them).

## 12. Malformed Legacy counterexample (from `FREQ.6D.4D.5C` §25, reproduced verbatim)

`MalformedLegacyClassifiedFiveDayStage_StillFails` — a Legacy-classified (no profile lineage), real-5D-shaped stage/workout identity (`TAPER_SECONDARY_STAGE`/`FARTLEK`) that is neither `TAPER_SHARPEN` nor `EASY_STANDARD`. **Fails**, exactly as `FREQ.6D.4D.5C` predicted — the partition does not collapse into acceptance.

## 13. Real 5D Taper result (both validators)

Both `TAPER_PRIMARY_STAGE` and `TAPER_SECONDARY_STAGE` pass `CatalogPrescriptionContextValidator` (§9) without any stage-name special-casing — proven structurally by `NoStageNameAllowListExists_ValidatorLogicHasNoFiveDayStageComparisons` (no `== "TAPER_PRIMARY_STAGE"`/`== "TAPER_SECONDARY_STAGE"` comparison exists anywhere in the validator's source). `CatalogFinalPrescribedPlanValidator`'s partition was proven correct via the real E2E chain (§16/§21) reaching and passing it — both real lanes' `PrescriptionSource` classify `ProfileBacked`, the `legacyTaperKeySessions.Count == 0` branch applies, and no further check runs.

## 14. 3D result

Zero delta. `Gen3BThreeDayPublicActivationTests` (51/51, including the reverted-to-original `WrongCombination_NeverNearestMatches("intermediate", 5)` case) and the broader `Gen3A`/`Freq6D4D` focused suite all pass unchanged.

## 15. 4D result

Zero delta, covered by the same focused suite and the full regression (§30).

## 16. Beginner×4D result

Zero delta, `Gen4EBeginnerFourDayPublicActivationTests` covered in the focused/full regression, unchanged.

## 17. Calendar zero-delta

`CatalogWeekSkeletonCalendarMaterializer`, `MinimumKeySessionToKeySessionSeparationDays`, `DatedGeneratedCatalogPlanSkeletonValidator` — confirmed untouched by `git diff` (zero lines changed in any of the three). `Freq6D4D5BReal5DDarkPlanTests` (13/13) and `CatalogWeekSkeletonCalendarMaterializerMultiKeyTests` (14/14) re-run as regression only, unchanged.

## 18. Profile/progression zero-delta

`TAPER_PRIMARY_STAGE`, `TAPER_SECONDARY_STAGE`, `TAPER_SHARPEN`, every profile ref, `TAP-P`/`TAP-S`, every `WorkoutDefinition`, every `DoseCategory` — confirmed zero PlanCatalog file changed (`git status plan-catalog/` shows only the two pre-existing, unrelated audit files). `plan-catalog/PlanCatalog.sln`: **1,510/1,510**, byte-identical to `FREQ.6D.4D.5C`'s own baseline.

## 19. Public activation retry — third attempt, reverted a third time

Re-applied the exact `V1CatalogPilotIdentityPolicy` widening attempted in Split E and `FREQ.6D.4D.5B`. Real E2E HTTP testing (`PublishedCatalogTestRelease`-backed, mirroring `Gen3BThreeDayPublicActivationTests`) confirmed **both Taper fixes work** — the request progressed past `CatalogPrescriptionContextValidator` (§9) and, once a directory-shape mismatch specific to the synthetic test-release fixture was worked around by probing against the real dev catalog root directly, past `CatalogFinalPrescribedPlanValidator` as well (confirmed by the error changing from `TAPER_SHARPEN_CONTEXT_MISSING` → `FINAL_TAPER_SHARPEN_COUNT_INVALID` → a completely different, Taper-unrelated exception once both fixes were in place).

**Third, genuinely independent blocker found**: `V1CatalogPublicWorkoutTypeMappingPolicy.Map` (in `CatalogPublicPreviewMaterializer.cs`) is a hardcoded switch mapping exact `(WorkoutDefinitionKey, StructuralRole, ProgressionStageKey)` tuples to a public-facing `GeneratedCatalogWorkoutType` enum, covering only the five workout keys the legacy 3D/4D/Beginner×4D catalog ever used (`EASY_STANDARD`, `LONG_RUN_STANDARD`, `FARTLEK`, `THRESHOLD_TEMPO`, `GOAL_PACE_TEN_K`). The real 5D `FOUNDATION` phase's lane0 workout, `AEROBIC_STRENGTH_CONTROLLED_INTRO` (authored in `FREQ.6D.4D.5`), has no entry — `CatalogPublicWorkoutTypeUnsupportedException` fires for every 5D request whose plan includes a Foundation-phase week (every supported horizon).

This is unrelated to Taper, unrelated to calendar, and unrelated to prescription-context/execution-resolution — a distinct concern entirely (which public-facing category a real catalog workout belongs to). Deciding the correct mapping (there is no domain-obvious answer among `Easy`/`LongRun`/`Interval`/`Tempo` for "aerobic strength") is a real product decision this phase has no authority to make. Per this phase's own explicit STOP condition (§25/§44), the routing widening was reverted a third time rather than guessing at a mapping. `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic.

## 20-27. Public preview/confirm/DB/persistence/adaptation/neighbors/no-silent-coercion

Not reached (blocked by §19). No public preview, confirmation, or DB-backed 5D plan exists to test against. The unsupported-neighbor matrix (`Beginner×5D`, `Advanced×5D`, `Intermediate×6D/7D`) remains closed, confirmed by the reverted-to-original `WrongCombination_NeverNearestMatches`/`Phase4F8_2_NonPilotRequest_RoutesLegacyWithoutCatalog` assertions passing (§14).

## 28. No-silent-4D result

Confirmed structurally: the routing widening was reverted before any assertion of this kind could be meaningfully exercised at the public layer; the dark-pipeline tests (`Freq6D4D5BReal5DDarkPlanTests`, unmodified, still green) continue to assert `candidate.CandidateKey == "TEN_K__5D__INTERMEDIATE"`/`CandidateVersion == 1` with `Assert.NotEqual("TEN_K__4D__INTERMEDIATE", ...)`.

## 29. Packaging/discovery result

Not re-verified this phase (no catalog/bundle content changed, no packaging-relevant file touched) — `FREQ.6D.4D.5`'s inventory/packaging closure stands unchanged.

## 30. Full regression

```
Focused suite (Phase4F8_2|Gen3B|Freq6D4D5DTaperCompletenessPartitionTests|Phase4F7A):
  70/70 passed

Full backend suite (dotnet test backend/RunningApp.sln), post-implementation, post-third-revert:
  3,649 / 3,650 passed (1 pre-existing, unrelated Sw09 failure -- confirmed
  unrelated to 5D/Taper across every prior split's regression run)

PlanCatalog.Tests: 1,510 / 1,510 passed -- zero delta

dotnet build backend/RunningApp.sln:            0 Warning, 0 Error
dotnet build backend/RunningApp.sln -c Release:  0 Warning, 0 Error
git diff --check:                                clean (CRLF-normalization warnings only)
```

Docker/Postgres confirmed healthy throughout — no DB-backed test skipped or simulated (though no 5D-specific DB round-trip was reached, per §20-27).

## 31. Technical-debt/comment disposition

No pre-existing technical-debt record referencing `TAPER_SHARPEN`/stage-key coupling was found (re-confirmed, same repository-wide search `FREQ.6D.4D.5C` already performed, no new hits). `V1TaperSharpenPrescriptionPolicy.cs` gained an explicit class-level scope comment (`LEGACY_TAPER_RUNTIME_ONLY`) — the policy itself is not deprecated or removed, only its scope is now stated explicitly rather than implicitly. No duplicate debt record created; this report is the disposition.

**New, disclosed-but-unresolved finding for the next phase**: `V1CatalogPublicWorkoutTypeMappingPolicy`'s hardcoded workout-key allow-list (§19) is itself the same *class* of legacy-pilot-scoped hardcoding this whole `5A`-`5D` sequence has been closing one instance at a time — worth the next phase's own provenance/scope investigation, analogous to `5C`'s treatment of `TAPER_SHARPEN`, rather than a one-line patch.

## 32. Parent FREQ.6D.4D closure decision

**Not evaluated for closure.** Success boundary criterion F (real public Intermediate×5D preview succeeds) is not met — a genuine, third, independent blocker remains. Per §43 of the originating prompt, parent closure is conditional on this attempt succeeding; it did not. `FREQ.6D.4D` overall dual-KEY production integration remains open.

## 33. Next roadmap capability

Resolve `V1CatalogPublicWorkoutTypeMappingPolicy`'s scope/provenance for `AEROBIC_STRENGTH_CONTROLLED_INTRO` (and audit whether any other real 5D-authored workout key has the same gap, before assuming this is the only one) — a real product/evidence question (what public workout-type category, if any existing one fits, or whether the public enum itself needs a new value), then retry public activation a fourth time.

## 34. Final classification

**`FREQ6D4D5D_TAPER_FIXED_PUBLIC_ACTIVATION_BLOCKED_ELSEWHERE`**

Both real Taper-completeness defects `FREQ.6D.4D.5B`/`5C` disclosed and decided are now implemented, verified, and zero-legacy-delta: `CatalogPrescriptionContextValidator` and (a second occurrence found only during this phase's own real E2E testing) `CatalogFinalPrescribedPlanValidator` both correctly partition Taper `KEY_SESSION` completeness along the existing Legacy/ProfileBacked classification, exactly per `FREQ.6D.4D.5C`'s approved authority — no new completeness model, no new metadata, no stage renaming, no weakened legacy validation. The real 5D dual-lane Taper (`TAPER_PRIMARY_STAGE`/`TAPER_SECONDARY_STAGE`, both ProfileBacked) is proven correct through both validators without any stage-name special-casing, and a real malformed-Legacy counterexample is proven still rejected. Public activation was retried a third time and, for the first time, progressed cleanly past *both* Taper-related failure points — but real E2E testing surfaced a third, genuinely independent, out-of-scope blocker (`V1CatalogPublicWorkoutTypeMappingPolicy` has no public workout-type mapping for the real 5D `AEROBIC_STRENGTH_CONTROLLED_INTRO` workout) before this phase's own STOP condition was correctly honored rather than worked around. `TEN_K__5D__INTERMEDIATE` remains fully dark to public traffic; `FREQ.6D.4D` parent closure is not evaluated. The next phase must resolve the public workout-type mapping gap before a fourth public-activation attempt.
