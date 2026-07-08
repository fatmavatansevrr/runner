# Release-Status Chain Audit — RELEASE-STATUS-001

## Expected chain (per task)

`0.2.0-pilot` → SUPERSEDED by `0.3.0-pilot`; `0.3.0-pilot` → SUPERSEDED by `0.4.0-pilot`; `0.4.0-pilot` →
active.

## Actual ledger state (`artifacts/appsel-plan-catalog/release-status.json`)

| releaseVersion | status | supersededByVersion | reason (truncated) |
|---|---|---|---|
| `1.0.0` | SUPERSEDED | `0.1.0-pilot` | Predates domain-content decision audit; published before the PLACEHOLDER_UNCONFIRMED publish guard existed. |
| `0.1.0-pilot` | SUPERSEDED | `0.2.0-pilot` | Superseded after TEN_K/INTERMEDIATE/4D domain-content reconciliation against Golden Fixture v3. |
| `0.2.0-pilot` | SUPERSEDED | `0.3.0-pilot` | Replaced by 0.3.0-pilot's TEN_K_MASTER v2 TAPER phase-family eligibility correction. |
| `0.3.0-pilot` | SUPERSEDED | `0.4.0-pilot` | 0.3.0-pilot's manifest mislabeled combination v1 with mutated content; 0.4.0-pilot corrects this. |
| `0.4.0-pilot` | *(no entry)* | — | Absence of a ledger entry means active/non-superseded, by ledger design (`IReleaseStatusLedger.GetSupersededStatus` returns `null` for anything not recorded). |

## Verdict: `NO_OP_ALREADY_CORRECT`

The ledger already reflects exactly the expected chain, including the exact `0.2.0-pilot`→`0.3.0-pilot`
and `0.3.0-pilot`→`0.4.0-pilot` entries the task asked to verify. **No entry was added, modified, or
duplicated.** `recordedAtUtc` and `reason` text for all four existing entries are untouched.

## Chain integrity checks

- **Acyclic**: `1.0.0 → 0.1.0-pilot → 0.2.0-pilot → 0.3.0-pilot → 0.4.0-pilot` is a single linear chain;
  no release supersedes itself or an earlier release in a way that would create a cycle.
- **Ordered**: each `supersededByVersion` points strictly forward to the next release in publication
  order; no entry points backward.
- **No dangling targets**: every `supersededByVersion` value (`0.1.0-pilot`, `0.2.0-pilot`, `0.3.0-pilot`,
  `0.4.0-pilot`) corresponds to a release directory that actually exists under
  `artifacts/appsel-plan-catalog/` (confirmed via directory listing).
- **Exactly one active release**: `0.4.0-pilot` is the only release with no ledger entry, and is therefore
  the sole active Pilot release — confirmed independently by `verify-release --version 0.4.0-pilot`
  producing **PASSED** with no `VERIFY_RELEASE_SUPERSEDED` warning, while all four superseded releases
  produce **PASSED** *with* that warning (non-blocking, as designed — historical verification must still
  succeed for superseded releases).

## Commands run

- `verify-release --version 1.0.0` → PASSED (with SUPERSEDED warning, superseded by `0.1.0-pilot`)
- `verify-release --version 0.1.0-pilot` → PASSED (with SUPERSEDED warning, superseded by `0.2.0-pilot`)
- `verify-release --version 0.2.0-pilot` → PASSED (with SUPERSEDED warning, superseded by `0.3.0-pilot`)
- `verify-release --version 0.3.0-pilot` → PASSED (with SUPERSEDED warning, superseded by `0.4.0-pilot`)
- `verify-release --version 0.4.0-pilot` → PASSED (no SUPERSEDED warning — active)

## Final status: `RELEASE_STATUS_CHAIN` = **NO_OP_ALREADY_CORRECT**
