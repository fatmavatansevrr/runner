# Domain Wave 3 Complexity Removal

Wave 3 removes `complexityTier` from reusable `WorkoutDefinition` draft artifacts. No replacement taxonomy, moved field, inferred tier, or runtime derivation was introduced.

Created draft workout versions:

- `EASY_STANDARD v4`
- `FARTLEK v4`
- `LONG_RUN_STANDARD v4`
- `THRESHOLD_TEMPO v4`

The new publish-eligible workout schema line is `schemaVersion: 3`; it rejects the legacy field with semantic validator code `LEGACY_COMPLEXITY_TIER_NOT_ALLOWED_IN_NEW_SCHEMA`. Legacy schema versions 1 and 2 remain readable and still require a valid legacy `complexityTier`.

D5, D7, D9, and D11 are recorded as technical-only removals in `PilotDomainContentAudit` (`AUD-313`, `AUD-314`, `AUD-316`, `AUD-317`). D2, D3, D4, and D13 were not resolved.
