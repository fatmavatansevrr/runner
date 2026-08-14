# Phase 4L.6A — Mobile Release Artifact and Backend Catalog Packaging Closure

## 1. Executive result

Phase 4L.6A is a partial closure. Backend runtime-catalog packaging is locally proven, including a published-process HTTP smoke. Mobile production artifact acceptance remains blocked. The overall release decision remains `LONG_HORIZON_PILOT_RELEASE_DECISION_IS_NO_GO`.

Final classification:

`LONG_HORIZON_MOBILE_RELEASE_ARTIFACT_REMAINS_BLOCKED_BY_EXPLICIT_APPLICATION_ID_FIREBASE_REGISTRATION_SIGNING_OR_ANDROID_PLATFORM_REQUIREMENT`

Narrow backend result:

`LONG_HORIZON_BACKEND_PUBLISH_ARTIFACT_NOW_CONTAINS_THE_COMPLETE_RUNTIME_CATALOG_AND_RESOLVES_IT_WITHOUT_REPOSITORY_OR_DEVELOPER_PATH_DEPENDENCY`

No planner formula, allocation, Runway/Core, NotToday, adaptation, activation, retry, background-planning, or client-side planning behavior changed.

## 2. Inherited blockers

Phase 4L.6 found Android `minSdk` 21 versus Firebase Auth's API 23 requirement, the example application ID `com.example.antigravity_app`, debug signing for release, no APK/AAB, and a backend publish with no runtime catalog. The parent release record remains `OPEN/P1`.

## 3. Scope and exclusions

This pass changed Android release configuration, backend deployment packaging/root authority, focused tests, governance, and documentation only. It did not alter formulas, allocation, Runway/Core prescription, persistence schemas, NotToday, lifecycle automation, client planning, CI/CD, CORS/TLS, database-secret policy, observability, rollout, or staging UAT. Phase 4L.6B was not implemented. No commit or push was made.

## 4. Mobile configuration inspection

Inspected Gradle settings/wrapper/properties, manifests, `google-services.json`, `pubspec.yaml`, Android ignore rules, release build type, SDK sources, version, identity references, and output/signing conventions. The inherited `flutter.minSdkVersion` resolved to 21. The only authoritative checked-in Android/Firebase package is `com.example.antigravity_app`. No approved Appsel reverse-domain Android identity or production-keystore ownership exists in the repository. Existing Android ignore rules cover `key.properties`, `*.keystore`, and `*.jks`.

## 5. minSdk decision

`mobile/android/app/build.gradle.kts` now sets `minSdk = 23` explicitly. API 21–22 devices are intentionally outside the supported set because Firebase Auth 6.5.4 requires API 23; no manifest override or dependency trick is used. A configuration test pins this floor. A fresh dependency/build traversal was blocked by restricted Flutter SDK cache access, so the stronger claim that no resolved plugin requires more than 23 is not made.

## 6. Application ID decision

The previous and current repository default is `com.example.antigravity_app`. Because no approved production identity exists, the pass did not invent a company domain. `APPSEL_ANDROID_APPLICATION_ID` now parameterizes the release identity, but its default intentionally remains the sole Firebase-registered example ID. Consequently, production identity approval remains blocked and no production-ready classification is made.

## 7. Firebase package parity

The checked-in Firebase client package exactly matches the default `com.example.antigravity_app`. It does not establish parity for a future production ID. A production release requires Firebase registration and a matching approved client configuration for the chosen ID. No Firebase admin credential or fabricated client was added.

## 8. Signing design

Release no longer uses debug signing. It requires `APPSEL_RELEASE_STORE_FILE`, `APPSEL_RELEASE_STORE_PASSWORD`, `APPSEL_RELEASE_KEY_ALIAS`, and `APPSEL_RELEASE_KEY_PASSWORD` through Gradle properties or environment variables and fails clearly when a release task lacks them. Debug signing remains available for debug builds. A locally generated ignored validation key is not production signing evidence and is not tracked.

## 9. Android build identity/version

Namespace remains `com.example.antigravity_app`; application ID is externally parameterized; minimum SDK is 23; compile/target SDK remain Flutter-authoritative; NDK is `27.0.12077973`; application version is `0.1.0+1`. Release does not set `debuggable=true`, and no minification/resource-shrinking redesign was introduced. The example default means production identity remains unacceptable.

## 10. Release APK result

No fresh release APK was produced. `flutter pub get` and subsequent Flutter operations could not acquire the externally owned Flutter SDK cache lock/write access in the restricted execution environment. An escalation attempt was rejected because the environment approval quota was exhausted. No debug/profile artifact is substituted as evidence.

## 11. Release AAB result

No fresh release AAB was produced for the same explicit environment reason. No AAB metadata claim is made.

## 12. Artifact verification

There is no mobile artifact to inspect. Package ID, embedded minSdk/targetSdk, debuggable flag, signing certificate, Firebase resources, assets, secrets, and SHA-256 therefore remain unverified at artifact level. `jarsigner` and `keytool` are available; `apkanalyzer`, `aapt`, `bundletool`, and `adb` were not available on `PATH`.

## 13. Device/emulator smoke, if available

Not run: no release APK was produced and no device/emulator installation authority was available. Firebase launch/bootstrap is not claimed.

## 14. Mobile regression tests

No fresh Flutter regression result is claimed. The inherited Phase 4L.6 baseline is 339 passed, 0 failed, but it predates this configuration change. A new `android_release_configuration_test.dart` was formatted and added, but could not be executed because the Flutter tool could not use its SDK cache. Flutter 3.32.2/Dart 3.8.1, Java 17.0.2, Gradle 8.12, and Android Gradle Plugin 8.7.3 were identified.

## 15. Catalog authority inspection

The backend previously depended on repository-relative configuration and published no catalog. Runtime loaders consume canonical JSON beneath `plan-catalog/catalog`; schemas, audits, phase documents, tests, and source files are not runtime assets. Relative configuration is now rooted at application content root, never the process current directory.

## 16. Runtime catalog inventory

The complete canonical runtime inventory is 71 JSON files in 12 exact-case directories: combinations 10, layouts 2, level-modifiers 6, long-horizon-progressions 1, policies 3, preparation-runway-progressions 4, progression-modifiers 2, registries 2, rule-packs 4, templates 6, workout-progressions 5, and workouts 26. Only these catalog JSON files are packaged.

## 17. Backend packaging design

`RunningApp.Api.csproj` links `plan-catalog/catalog/**/*.json` into `plan-catalog/catalog/%(RecursiveDir)%(Filename)%(Extension)` and copies it to build and publish output. Directory structure is deterministic and schemas/audits/docs/tests are excluded.

## 18. Catalog root resolution

Authority order is: (1) explicit configured path, with a relative value rooted at content root; (2) packaged `plan-catalog/catalog` beneath content root; (3) repository fallback only in Development. Current working directory is never authority. Production cannot silently reach a developer checkout.

## 19. Startup validation

Startup validates root existence, all 12 exact-case directories, non-empty inventory, JSON parseability, case-conflicting paths, and the active v10 pilot in a canonical deploy package. A Process-A immutable release manifest may intentionally omit a retired v10 so frozen plans remain readable; focused retirement tests pin this exception. Missing/invalid deployment catalog fails before misleading public template errors.

## 20. Publish result

Final command: `dotnet publish backend/RunningApp.Api/RunningApp.Api.csproj -c Release --no-restore -o .local-acceptance/isolated/level1/level2/phase4l6a-publish-v4 -p:DebugType=None -p:DebugSymbols=false -p:PathMap=C:\Users\vatan\Desktop\runner=/_/src`.

Result: success; 115 files; 15,506,379 bytes total; 71 catalog JSON files; 0 PDB; 0 developer absolute-path matches. The output contains normal API assemblies, runtime dependencies, web config, `appsettings.json`, `appsettings.Development.json`, and the runtime catalog.

## 21. Published API external-directory smoke

The successful process smoke used the preceding clean `publish-v3` beneath `.local-acceptance/isolated/level1/level2`, with that publish directory as current directory and with its would-be Development repository fallback absent. `/health` and `/health/database` were healthy. The sandbox denied a literal `C:\tmp` publish, so this is repository-independent path-resolution proof inside an ignored isolated subtree, not proof from a physically external filesystem.

After the final validator compatibility adjustment, `publish-v4` was rebuilt and fully tested. A direct v4 process-start attempt was rejected by the execution environment's exhausted approval quota; it was not retried indirectly. Therefore v4 process smoke is explicitly not claimed.

## 22. Long-Horizon preview smoke

Against the isolated published v3 API, 21 weeks returned 200 with `total_weeks=21` and `contract_version=1`; 52 weeks returned 200 with `total_weeks=52`. The final API assembly hash is unchanged between v3 and v4; the Application assembly changed only for startup compatibility, and the final 3,137-test suite plus 18 focused publish/runtime tests passed.

## 23. Boundary/static/Habit smoke

Published v3 HTTP results: dedicated Long-Horizon 20 returned 422 `LONG_HORIZON_PILOT_UNSUPPORTED`; 53 returned 422 `PLAN_HORIZON_EXCEEDS_SUPPORTED_WINDOW`; unsupported distance returned 400 `VALIDATION_ERROR`; unsupported level/frequency returned 422 `LONG_HORIZON_PILOT_UNSUPPORTED`; existing static 20-week preview returned 200; Habit preview returned 200. EventLog logging was disabled only in smoke environment because Windows EventLog access can wrap requests under the restricted identity; production observability remains Phase 4L.6B scope.

## 24. Repository-independence proof

The v3 fallback candidate `.local-acceptance/isolated/level1/plan-catalog/catalog` did not exist. The API therefore selected its packaged content-root catalog; no manual post-publish catalog copy occurred. The final v4 package has the same 71-file catalog layout and passes resolver/package tests. A physically outside-repository run remains desirable when sandbox permissions permit it.

## 25. Linux/case-sensitivity analysis

All target links use deterministic directory names and platform-neutral runtime APIs. Validation compares required directory names with `StringComparer.Ordinal`, rejects case-conflicting relative paths, and normalizes inventory paths to `/`. A Linux/container execution was not available, so analysis and tests—not an actual Linux process—are the evidence.

## 26. Catalog artifact tests

Six resolver/inventory/MSBuild tests plus one real HTTP packaged-catalog test were added. A final combined package/published-release focused run passed 18/18 after aligning the pre-existing Process-A test fixture with the two newer runtime directory families and preserving intentional v10 retirement semantics.

## 27. Artifact security review

The backend package contains no PDB, test binary, `.claude`, phase/audit document, baseline/temp content, response JSON, keystore, signing property, Firebase admin credential, or developer absolute path. No new DB credential or auth token was introduced. Existing `appsettings` production-secret/default concerns remain Phase 4L.6B blockers. Local outputs and the validation keystore are ignored and uncommitted.

## 28. Artifact manifests

Backend final artifact: `.local-acceptance/isolated/level1/level2/phase4l6a-publish-v4`; runtime target .NET 9; main assembly `RunningApp.Api.dll`, SHA-256 `41C48CA53E283C4636873D16E91B5E205EA2A84802426FA743923557DC542F2C`; application assembly SHA-256 `0F83DD1CD97C45779E9B52AD4A7D3547E571D5D55D65844B6AF5A85BF9B6BC8D`; catalog root `plan-catalog/catalog`; catalog count 71; configuration files `appsettings.json` and `appsettings.Development.json`; migration assembly `RunningApp.Persistence.dll` present. No mobile manifest exists because no APK/AAB exists.

## 29. Test results

Final automated results: backend restore passed; plan-catalog restore passed; Release backend build passed with 0 warnings/0 errors; plan-catalog 1,250 passed/0 failed/0 skipped; Long-Horizon backend 914/0/0; governance parity 12/0/0; final package/published-release focus 18/0/0; full backend after all fixes 3,137/0/0. Final publish passed. Flutter commands and mobile builds were blocked before execution and are not hidden skips.

Required matrix disposition:

- 1 PASS; 2 BLOCKED (fresh dependency graph unavailable); 3 FAIL/BLOCKER; 4 FAIL/BLOCKER for production ID; 5 FAIL/BLOCKER for production ID; 6 CONFIG-PARITY ONLY; 7 PASS; 8 PASS; 9 PASS by configuration assertion, execution blocked; 10 CONFIG PASS/artifact blocked; 11 PASS (`0.1.0+1`); 12 BLOCKED.
- 13–19 BLOCKED (no fresh Flutter invocation); 20–21 BLOCKED (no APK/AAB); 22–30 BLOCKED because no artifact/device exists.
- 31–45 PASS: 71-file inventory, dependencies/directory families, build/publish copy, explicit override, packaged fallback, Development-only fallback, fail-fast missing package, exact casing, platform-neutral paths, and no absolute developer path are verified.
- 46 PASS; 47 PASS on v3 and environment-blocked on final v4; 48–60 PASS on v3 smoke and final automated package/runtime coverage, with the physical-outside-repository and Linux qualifications in sections 24–25.
- 61–70 PASS for tracked/package inspection; no generated binary is tracked.
- 71–80 PASS: formulas, allocation, Runway/Core, NotToday, adaptation, automatic activation/retry, background planning, client planning are unchanged; Phase 4L.6B is not implemented.

## 30. Governance

Added `TD-LONG-HORIZON-MOBILE-RELEASE-ARTIFACT-BACKEND-CATALOG-PACKAGING-001` as `OPEN`, classification `BACKEND_CATALOG_PACKAGING_PROVEN_MOBILE_PRODUCTION_ARTIFACT_IDENTITY_AND_SIGNING_UNPROVEN`, severity `P1_RELEASE_BLOCKER_PARTIAL_CLOSURE`. Appended Phase 4L.6A evidence to the parent release record and kept it `OPEN`. JSON/Markdown parity is verified. Aggregate: 69 risks, 16 OPEN, 53 CLOSED.

## 31. Remaining blockers

Mobile: approved non-example ID, matching Firebase Android registration, production signing ownership/credentials, fresh Flutter dependency/analyze/test/build, APK/AAB inspection, and device install/launch. Release-wide: production configuration/security/secrets, production-like migration rehearsal, authenticated device UAT, observability, rollout ownership, kill switch, and rollback drill. Backend artifact packaging itself has no known remaining functional blocker; literal external-filesystem and Linux process evidence remain environment qualifications.

## 32. Final classification

`LONG_HORIZON_MOBILE_RELEASE_ARTIFACT_REMAINS_BLOCKED_BY_EXPLICIT_APPLICATION_ID_FIREBASE_REGISTRATION_SIGNING_OR_ANDROID_PLATFORM_REQUIREMENT`

`LONG_HORIZON_BACKEND_PUBLISH_ARTIFACT_NOW_CONTAINS_THE_COMPLETE_RUNTIME_CATALOG_AND_RESOLVES_IT_WITHOUT_REPOSITORY_OR_DEVELOPER_PATH_DEPENDENCY`

`LONG_HORIZON_PLANNER_FORMULAS_NOT_TODAY_ACTIVATION_RETRY_AND_ROLLING_LIFECYCLE_BEHAVIOR_REMAIN_UNCHANGED`

The combined success output is intentionally not emitted because its mobile closure predicates are false.

## 33. Exact next phase

Do not begin Phase 4L.6B until release owners supply an approved Android application ID, matching Firebase client, and production-signing authority, then rerun the entire Flutter artifact/inspection/device matrix. After that evidence closes this record, the recommended next phase remains **Phase 4L.6B — Production Configuration, Security, Migration and Forward-Schema Rollback Closure**.
