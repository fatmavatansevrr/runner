# Appsel Plan Catalog — Süreç A (Template Catalog Authoring)

This solution implements **only** Process A (template catalog authoring, validation, and publishing)
per `plan-catalog-antigravity-brief-v2.md`. It does not implement Process B (runtime plan generation),
and has no project reference to any `backend/RunningApp.*` project.

## Layout

```
src/PlanCatalog.Contracts/      published boundary: metadata, references, bundle, manifest, enums
src/PlanCatalog.Core/           authoring models, validators, ports (no filesystem/JSON dependency)
src/PlanCatalog.Infrastructure/ JSON Schema validation, canonical serialization, hashing, publish workflow
src/PlanCatalog.Cli/            validate / build-bundle / build-release / publish / verify-release / retire
tests/PlanCatalog.Tests/        contract, validator, graph, hashing, schema, publishing, golden tests
catalog/                        editable authoring source (draft/validated) — never read by the backend
schemas/                        JSON Schema per published artifact type
artifacts/                      immutable, generated publish output (git-ignored build output)
```

## Building and testing

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

## CLI

```bash
dotnet run --project src/PlanCatalog.Cli -- validate [--key KEY] [--version N] [--json]
dotnet run --project src/PlanCatalog.Cli -- validate-combination <KEY> [--version N] [--json]
dotnet run --project src/PlanCatalog.Cli -- build-bundle <COMBINATION_KEY> [--version N] [--json]
dotnet run --project src/PlanCatalog.Cli -- build-release --version <RELEASE_VERSION> [--json]
dotnet run --project src/PlanCatalog.Cli -- publish --version <RELEASE_VERSION> [--dry-run] [--json]
dotnet run --project src/PlanCatalog.Cli -- verify-release --version <RELEASE_VERSION> [--json]
dotnet run --project src/PlanCatalog.Cli -- retire --type <DOCUMENT_TYPE> --key <KEY> --version <N> [--json]
```

All commands exit non-zero on failure and support `--json` for machine-readable output.
`PLAN_CATALOG_ARTIFACTS_DIR` overrides the default `artifacts/` output location (used by tests).

## Pilot scope

`TEN_K_MASTER` × `RUN_LAYOUT_4D` × `INTERMEDIATE_MODIFIER` → `TEN_K__4D__INTERMEDIATE`, published as
bundle version 1 with exact dependency-closure version/hash pinning. See `artifacts/appsel-plan-catalog/`
after running `publish` for the generated release.

## Future Process B obligation (documented, not implemented here)

When Process B is built in `backend/RunningApp.*`, it must add a contract test asserting that its
serialized runtime-resolver output values are exactly the allowed-value set published in
`RUNTIME_CONDITION_VALUES_V1`. No such backend test is included in this repository.
