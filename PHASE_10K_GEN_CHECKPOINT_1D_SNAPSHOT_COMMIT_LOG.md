# Phase 10K-GEN.CHECKPOINT.1D — Durability-First Snapshot Commit

**Supersedes CHECKPOINT.1C's clean-phase-grouping attempt, which was abandoned mid-way after finding the underlying premise didn't hold (see prior turn: Adaptation V1 and the explicitly out-of-scope Long Horizon composition work share production files, e.g. `LongHorizonRollingWindowActivationService.cs`). Local commits only.**

## 0. Self-correction found and fixed before proceeding (important, disclosed)

While starting this phase, discovered that the prior CHECKPOINT.1B commit `49af873` had a wrong-target amend applied: `git commit --amend` targets HEAD, and by the time the correction was attempted, HEAD was the **documentation** commit (`e90e0dd`), not the intended **code** commit. The doc commit briefly carried the code commit's message. This was caught immediately by checking `git show --stat HEAD` after the amend, before any further action.

**Fix applied** (safe — neither commit had been pushed, confirmed via `git log origin/main..main` both before and after): `git reset --soft HEAD~2` to uncommit both back to staged state (verified all 19 original files remained correctly staged, no content lost), then re-committed each with its correct, distinct message:

- `80f6323` — documentation commit, original correct message restored.
- `be5cd1a` — code commit, now carrying the corrected message that honestly discloses the `CatalogPreviewGenerator.cs` bundling issue found in the prior turn (that commit's diff for that file includes real, pre-existing, working PreparationRunway code — `GeneratePreparationRunwayPreviewAsync` and related orchestrator wiring — that predates this session and was swept in by committing the whole file, not introduced by GEN.4D.2/GEN.4E).

## 1. Step 1 — commit message correction (done above, folded into §0)

## 2. Step 2 — snapshot commit

Excluded (left uncommitted, confirmed disposable): `backend/calendar_july.json`, `backend/confirm_response.json`, `backend/preview_response.json`, `baseline_tmp/` — stray debug/scratch artifacts, not source.

Staged and committed everything else via `git add -A` + selective `git reset` on the 4 excluded paths:

```
1b07cf8 snapshot: uncommitted Adaptation V1 + 10K-Generalization + PreparationRunway
        engagement work, as of 2026-08-14
        2272 files changed, 2544560 insertions(+), 1930 deletions(-)
```

Commit message explicitly states the contents are not cleanly separated by phase, names every work stream actually mixed together (Adaptation V1 4M.1-4M.5D, 10K-Generalization GEN.0-GEN.5C — already separately committed by this point, PreparationRunway 4G.6, Long Horizon 21-52wk composition 4H/4I/4K/4L, mobile/Flutter), states its purpose is durability not organization, and names the 4 explicitly excluded files. No phase attribution was invented for content not independently verified.

## 3. Step 3 — verification

```
git log --oneline -5
  1b07cf8 snapshot: ...
  be5cd1a feat(catalog): close Beginner 4D gaps and activate Beginner 4D publicly
  80f6323 docs(catalog): document Beginner 4D closure and Beginner 3D non-support chain
  3549a8a docs(catalog): clarify GoalPaceReachabilityVerifier ...
  80f742e docs(product): UX decision audit ...

git log origin/main..main --oneline | wc -l   -> 32   (nothing pushed)

git status --short | wc -l   -> 4   (exactly the excluded stray files, nothing else remains)

dotnet build backend/RunningApp.sln --no-restore -v:minimal
  -> 0 Warning, 0 Error

dotnet test .../RunningApp.IntegrationTests.csproj --no-build   (full suite, detached background, ~20m7s)
  -> 3464/3464 passed, 0 failed, EXITCODE=0
```

**Exact match to the pre-snapshot baseline (3464/3464).** Not a coincidence requiring explanation: `dotnet test` executes whatever is physically present in the working directory regardless of git-commit status, so this count already included every Adaptation V1/LongHorizon/PreparationRunway test before this commit — committing them didn't change what gets executed, only whether it's protected in history.

## 4. What remains outside this phase's scope

4 stray files (`calendar_july.json`, `confirm_response.json`, `preview_response.json`, `baseline_tmp/`) remain uncommitted, for separate owner review — genuinely disposable-looking debug/scratch artifacts, not committed on the assumption they're safe to discard without explicit confirmation.

## 5. Final classification

```
WORKING_TREE_FULLY_COMMITTED_LOCALLY_ATTRIBUTION_CLEANUP_DEFERRED
```

All real engagement work (Adaptation V1, 10K-Generalization, PreparationRunway, Long Horizon composition, mobile/Flutter) now exists in local git history across 3 commits, none pushed. It is no longer one `git clean`/reset/environment-switch away from total loss. Splitting the large snapshot commit into cleanly-attributed per-phase commits remains a real, valuable, but explicitly deferred follow-up task for someone with full context on each individual work stream — not attempted here, not silently declared done.
