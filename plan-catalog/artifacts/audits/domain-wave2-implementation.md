# Domain Wave 2 Implementation

Wave 2 implements D1, D6, D8, D10, and D12 only. The current active root remains `TEN_K__4D__INTERMEDIATE v4`; the new candidate root is draft `TEN_K__4D__INTERMEDIATE v5`.

Implemented outcomes:

- D1: `RUN_LAYOUT_4D v2` removes `sequenceOrder`; slot order derives from the `slots` array.
- D6: `EASY_STANDARD v3` omits `components`.
- D8: `FARTLEK v3` uses `WARM_UP, MAIN_SET, RECOVERY, COOL_DOWN`.
- D10: `LONG_RUN_STANDARD v3` omits `components`.
- D12: `THRESHOLD_TEMPO v3` uses `WARM_UP, MAIN_SET, COOL_DOWN`.

Classifications: D1, D6, and D10 are `TECHNICAL_ONLY`; D8 and D12 are `CANONICAL_CONFIRMED`. No other blocker was resolved.

Validation evidence: `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release` (266/266), CLI source validation, active v4 validation, candidate v5 validation, release preview/cross-release hash consistency, and all historical release verifications passed. Candidate bundle hash: `cc894e866be081fb3b44171a27f2bee94375240c1fd6c6042255e3c393014732`.
