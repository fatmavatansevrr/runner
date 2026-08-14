# Backend DI Baseline Resolution — Catalog Live Pilot

## Executive result

Investigation established that `CatalogLivePilotOptions.Enabled=true` is intentional and environment-specific for the real Development host used by `CustomWebApplicationFactory`. The option's CLR default and base/production configuration remain disabled. The first failing test has a stale host-level expectation.

The second test combines service resolution, an unmeasured no-database-I/O claim, and the same stale feature-flag assertion. Repository search found no connection interceptor, open counter, diagnostic listener, fake provider, or connection-state observation supporting its `WithNoDbConnection` name. Because the requested stop condition `NO_DB_CONNECTION_ASSERTION_MECHANISM_UNRELIABLE` is therefore present, no speculative test edit or configuration change was made.

Outcome: `STOPPED — NO_DB_CONNECTION_ASSERTION_MECHANISM_UNRELIABLE`. The backend remains at 1,385 passed / 2 failed / 1,387 total.

## Initial reproduction

HEAD before the pass:

```text
3549a8a1eeef18ca96794fa1056043142d13bc78
3549a8a docs(catalog): clarify GoalPaceReachabilityVerifier measures theoretical completeness, not runtime safety
```

The immediately preceding fresh full backend run established 1,385 passed, 2 failed, 0 skipped, 1,387 total. This pass reproduced both tests individually and together before making any change:

| Command/filter | Passed | Failed | Skipped | Exact failure |
| --- | ---: | ---: | ---: | --- |
| `...RealHost_CatalogLivePilotOptions_DefaultsToDisabled` | 0 | 1 | 0 | line 107, `Assert.False`, expected false, actual true |
| `...RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection` | 0 | 1 | 0 | line 126, `Assert.False`, expected false, actual true |
| `FullyQualifiedName~DependencyInjectionResolutionTests` | 5 | 2 | 0 | the same two failures only; total 7 |

The failure cause does not differ between individual and group execution.

Pre-check commands also recorded `git status --short`, `git diff --check`, and `git diff --name-status`. `git diff --check` returned exit 0 with only pre-existing LF/CRLF warnings.

Existing dirty files were attributed as follows:

| Origin | Content |
| --- | --- |
| Phase 4G.4A/4G.4B/4G.4B.V | reconciliation audit, runway typed-contract documentation, internal runway contracts/validators/tests, validation document |
| Prior Phase 4G.3B.7/3B.8 | volume policy/governance and verifier changes plus decision audits |
| Previous baseline investigation | `BACKEND_TEST_BASELINE_STABILIZATION_RESET_AND_DI_FIX.md` |
| Governance/audit | activation-readiness and ten-k audit JSON/Markdown pairs |
| Generated | `backend/**/bin/**`, `backend/**/obj/**` Debug/Release output |
| Local/design | `.claude/`, local acceptance/output JSON, `baseline_tmp/`, Docker file, design PNGs |

None was modified by this pass.

## Configuration-source inventory

| Source inspected | Value | Real/base host | Development host | Test host |
| --- | --- | ---: | ---: | ---: |
| `CatalogLivePilotOptions.Enabled` property initializer | `false` | fallback | fallback | fallback |
| `appsettings.json` | section absent | loaded | loaded first | loaded first |
| `appsettings.Development.json` | `true` | no unless Development | loaded | loaded because factory forces Development |
| `appsettings.Testing.json` | file absent | no | no | no |
| `launchSettings.json` | sets `ASPNETCORE_ENVIRONMENT=Development`; no option key | launch-only | selects Development | not relied upon; factory selects Development |
| process environment | no matching option/environment override observed in this shell | possible standard provider | possible standard provider | none observed |
| command line | no option supplied by these tests | none | none | none |
| `CustomWebApplicationFactory.ConfigureAppConfiguration` | connection string and catalog root only | no | no | loaded last; does not override pilot option |
| `Program.cs Configure<T>` | binds `CatalogLivePilot` section | yes | yes | yes |
| `PostConfigure<T>` / alternative Configure | none found | — | — | — |

Consumers use `IOptions<CatalogLivePilotOptions>`. No separate `IOptionsSnapshot`/`IOptionsMonitor` registration or hidden in-memory pilot override was found.

## Configuration precedence

For the tested host, effective precedence is:

```text
CLR initializer false
< appsettings.json (no key)
< appsettings.Development.json true
< environment/command-line (no matching value observed)
< factory in-memory additions (no CatalogLivePilot key)
```

`CustomWebApplicationFactory.UseEnvironment("Development")` guarantees the Development file participates. Therefore:

| Environment | Effective value established from repository/test setup |
| --- | --- |
| base/production configuration | `false` through the property default because base settings contain no key |
| Development | `true` |
| integration-test real host | `true`, because it is explicitly Development |
| Testing settings file | not applicable; no such file exists and factory does not select Testing |

Configuration precedence is provable and no environment leak or binding defect was observed.

## Live-pilot intent evidence

Conclusion: `ENVIRONMENT_SPECIFIC`.

- `CatalogLivePilotOptions.Enabled` explicitly defaults false.
- `appsettings.json` does not enable it, preserving base/production disabled posture.
- `appsettings.Development.json` explicitly enables `CatalogLivePilot` and all three Development-only `LocalCatalogAcceptance` gates.
- `LocalCatalogAcceptanceOptions` independently requires `IHostEnvironment.IsDevelopment()` and all flags, and documents local HTTP preview/confirm acceptance without publishing the DRAFT catalog artifact.
- Git blame and `git show 8f12a712...` prove the explicit Development enablement and the failing tests entered the same accumulated Phase 4F checkpoint. The checkpoint includes Phase 4F.9 local-acceptance work.
- `running-background-v2-1-intermediate-pilot-closure.json` records the effective lifecycle as PUBLISHED through the Development-only local-acceptance override while the on-disk artifact remains DRAFT, and separately records the activation default as disabled.
- The Phase 4F.8.2 routing audit records production activation as disabled by default.

Thus `true` is intentional for the Development/test real host; it is not a claim that production defaults true.

## Test 1 root cause

Original assertion:

```csharp
Assert.False(options.Value.Enabled); // line 107
```

Actual invariant tested: effective `IOptions<CatalogLivePilotOptions>` value from the real Development host, not the CLR property default and not base/production configuration.

Actual behavior: `true`.

Approved behavior: `true` for Development; `false` for the option/base default.

Root-cause classification: `STALE_TEST_EXPECTATION`.

The test name `DefaultsToDisabled` conflates the property default with a configured Development host. A future fix should preserve a separate pure default test (`new CatalogLivePilotOptions().Enabled == false`) and assert the real Development host is enabled.

## Test 2 root cause

Assertions in order:

1. `IGenerationRouteDecider` resolves.
2. `ICatalogPreviewGenerator` resolves.
3. `IGeneratedCatalogPlanPayloadValidator` resolves.
4. `ICatalogPlanConfirmationService` resolves.
5. `ICatalogPeakVolumeBandLoader` resolves.
6. Effective `CatalogLivePilotOptions.Enabled` is false.

Only assertion 6 fails, at line 126. It does not prove any service-resolution or database invariant and is unnecessary for assertions 1–5.

Root-cause classification: `MULTIPLE_CONCERNS_IN_ONE_TEST`.

The stale shared assertion should eventually be removed from this test, but only alongside reliable replacement coverage for its claimed no-database-connection invariant.

## Multiple-concern analysis

Classification: `MULTIPLE_CONCERNS_IN_ONE_TEST` plus an unreliable named invariant.

The test combines:

- coherent service graph/lifetime resolution;
- a claim that resolution opens no database connection;
- unrelated Development feature-flag state.

The first is genuinely exercised. The third is exercised but stale. The second is not measured: the current implementation merely assumes that lack of an exception means no connection opened. Because the configured PostgreSQL database is real and available during integration runs, constructor-time open/close activity could occur without making the test fail.

Repository-wide inspection found no `DbConnectionInterceptor`, `DbCommandInterceptor`, `ConnectionOpening`/`ConnectionOpened` callback, counter, diagnostic listener, fake provider, or connection state assertion in this test path. This makes the current no-I/O mechanism unreliable.

## Fix applied

No code or test fix was applied due to the explicit stop condition. No option was forced false, no assertion was deleted merely to obtain green, and approved Development behavior was not disabled.

Smallest missing implementation decision: approve a reliable test-only observation seam, preferably an EF Core/Npgsql connection interceptor counter registered by a dedicated factory/test host, then assert the counter remains zero while resolving the five services. With that invariant in place, the feature flag assertion can be separated and corrected without reducing coverage.

## Test coverage preserved

All existing coverage remains byte-for-byte unchanged. In particular:

- no service-resolution assertion was removed;
- the nominal no-DB coverage was not weakened, although investigation found its mechanism insufficient;
- the CLR default and Development host distinctions were not papered over;
- no test was skipped, suppressed, retried, or relaxed.

## Focused test result

Pre-change `DependencyInjectionResolutionTests` result: 5 passed, 2 failed, 0 skipped, total 7. The two individual runs each reproduced one failure. No post-fix focused run exists because no fix was permitted after the stop condition.

## Full backend result

The current fresh full-suite baseline remains:

```text
Passed: 1,385
Failed: 2
Skipped: 0
Total: 1,387
```

Exact remaining failures:

- `DependencyInjectionResolutionTests.RealHost_CatalogLivePilotOptions_DefaultsToDisabled`
- `DependencyInjectionResolutionTests.RealHost_AllSixTargetServices_ResolveFromOneScope_WithNoDbConnection`

A final Release build/full-suite sequence was not run after investigation because no files affecting compilation or behavior changed and the mandated stop condition prevents claiming completion. Plan-catalog tests were not run because no plan-catalog code or mechanically consumed artifact changed.

## Files changed

Created by this pass only:

```text
BACKEND_DI_BASELINE_RESOLUTION_CATALOG_LIVE_PILOT.md
```

No existing file was modified or deleted. No configuration change was made.

## Files not changed

- `DependencyInjectionResolutionTests.cs`
- `CatalogLivePilotOptions` / `LivePlanPreviewRouting.cs`
- `Program.cs`
- all `appsettings*.json` and launch settings
- `CustomWebApplicationFactory.cs`
- reset endpoint/service/persistence files
- public endpoints, DTOs, catalog generation, horizon policy, frontend, schema, catalog artifacts, and TD files
- every pre-existing dirty file

## Reset and Preparation Runway non-impact

Reset behavior was not inspected further or modified; the prior transient HTTP 500 group is outside this pass.

Preparation Runway contracts, validators, tests, and all Phase 4G.4A/4G.4B/4G.4B.V documents remained untouched. Preparation Runway remains dark. `PLAN_HORIZON_COMPOSITION_REQUIRED`, race-preview behavior, the 12-week Development live-pilot posture, endpoints, and DTO contracts are unchanged.

## Remaining risks

- The no-database-connection claim needs a reliable test-only observation mechanism before the mixed test can be safely corrected under this prompt's rules.
- The two deterministic assertions remain red.
- Development enablement is deliberate, but naming it simply “live pilot enabled” without the environment qualifier risks confusing it with the still-disabled base/production default.

Anything not completed: test correction, reliable no-DB instrumentation, focused green run, Release build, and final green full backend suite.

## Commit/push status

No commit, amend, rebase, reset, history rewrite, branch operation, or push was performed.

## Stop conditions

Triggered:

```text
NO_DB_CONNECTION_ASSERTION_MECHANISM_UNRELIABLE
```

Not triggered:

```text
LIVE_PILOT_CONFIGURATION_INTENT_UNDETERMINED
CONFIGURATION_PRECEDENCE_CANNOT_BE_PROVEN
TEST_FAILURE_CAUSE_DIFFERS_BETWEEN_INDIVIDUAL_AND_GROUP_RUN
FIX_REQUIRES_DISABLING_APPROVED_LIVE_BEHAVIOR
```

The pass stops here rather than weakening the no-I/O invariant or describing a non-green result as complete.
