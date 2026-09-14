# PHASE HM-E1 — HALF-MARATHON EXPERIENCED 3D/4D/5D PRODUCT ELIGIBILITY, REUSE, AND NUMERIC AUTHORITY CLOSURE

**Type:** GOVERNANCE / PRODUCT-DECISION (no production implementation, no catalog artifact, no schema, no frontend change)

---

## 1. Scope framing

This phase's own governing prompt is explicit that `Experienced` is an **existing** product level (`RunningBackground.Experienced`, and separately `RunningExperience.Experienced` inside the plan-catalog engine's own internal taxonomy) — the question is narrowly **"what does the existing Experienced level mean for HALF_MARATHON,"** not whether it should exist. This report answers that question honestly, per this engagement's standing "no forced closure" discipline: if no real cross-distance Experienced authority exists to build HM-specific numbers on, that is reported as the exact blocker, not worked around.

## 2. Experienced is an existing product level — confirmed, with no functional backend representation

- `backend/RunningApp.Domain/Enums/RunningBackground.cs` — full enum: `{ Beginner, Intermediate, Advanced, Experienced }`, doc-commented "the entire active product contract." Confirmed real and current.
- `plan-catalog/src/PlanCatalog.Contracts/Enums/RunningExperience.cs` — the catalog engine's own, separate internal taxonomy: `{ New, Intermediate, Advanced, Experienced }`. Confirmed `Experienced` is a structurally reserved top rung of the catalog engine's own ladder, not a foreign concept to the engine's schema (`LevelModifierDefinition`, `PeakVolumeBandEntry`, `ProgressionModifierDefinition` all reference `RunningExperience` and therefore can, in principle, carry an `Experienced` row).
- Reserved-in-schema is not the same as populated. See §3-§4.

## 3. Experienced modifier audit — AUTHORITY_MISSING

- `find plan-catalog/catalog -iname "*experienced*"` → **zero results**. No `experienced-modifier.v*.json`, no dedicated Experienced progression file, of any kind, for any distance, has ever existed as a live catalog artifact.
- `grep -rl '"EXPERIENCED"' plan-catalog/catalog/level-modifiers/` → **zero results**. No live level-modifier document contains an Experienced row.
- `grep -n "EXPERIENCED" plan-catalog/catalog/policies/peak-volume-bands.v9.json` (the current live peak-volume-band policy) → **zero results**.
- **Positive, deliberate exclusion, not mere absence**: `plan-catalog/tests/PlanCatalog.Tests/Golden/PeakVolumePolicyImmutabilityTests.cs:66-73`, test `PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows`, asserts `Assert.DoesNotContain(policy.Entries, e => e.Experience == Contracts.Enums.RunningExperience.Experienced)` against the current live policy. A second test at line 76, `RestoredPolicyV1_DoesNotInventNewOrAdvancedOrExperiencedRows`, asserts the same discipline for the historical v1 restoration path. This is a maintained regression guard, not a coincidence of timing — the catalog engine's own test suite actively prevents an Experienced row from silently reappearing.

**Conclusion: `AUTHORITY_MISSING`.** No real Experienced progression modifier exists anywhere in the live catalog, for any distance, and its absence is enforced by a standing regression test rather than merely unaddressed.

## 4. 10K Experienced precedent audit — AUTHORITY_MISSING (deliberately, not "not yet built")

Per this phase's own explicit instruction, 10K precedent is useful evidence but never automatic HM authority — audited independently:

- `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs` — the public-eligibility gate for 10K (`IsSupportedIdentity`). No branch admits `RunningBackground.Experienced` at any frequency.
- **Direct, current regression test proof** — `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Gen9AdvancedCombinedDarkVerificationTests.cs:442,476-479`:
  ```
  // ── Isolation: public gate closed for Advanced 2D/7D; Intermediate/Beginner/Experienced unaffected ──
  public void ExperiencedLevel_RemainsUnrecognized_ForEveryFrequency()
      Assert.False(V1CatalogPilotIdentityPolicy.IsSupportedIdentity(GoalType.Race, GoalDistance.TenK, RunningBackground.Experienced, days));
  ```
  This test iterates every frequency and asserts non-support for **10K itself** — the single most mature, most extensively built-out distance in the product. If Experienced had any real backend authority anywhere, 10K would be the place it would exist first; it does not.
- A second corroborating comment, `Gen4DBeginnerFourDayCoreTests.cs:102`: "Experienced x4D (never widened by any [phase])" — confirms this exclusion has been a stable, repeatedly-reaffirmed invariant across many generations (GEN.4 era) of 10K work, not an oversight.
- `PlanCatalogDomainMapper.cs:53-72` — current code comment: `RunningBackground {Beginner, Intermediate, Advanced, Experienced}` classified collectively as `BackendRepresentationSupport.NotSupported`, with only `RunningBackground.Intermediate` carrying "an explicit, tested mapping to catalog level 'INTERMEDIATE'." **Currency caveat** (recorded honestly, per this engagement's discipline against silently trusting stale comments): this specific comment predates HM's own extensive Beginner/Advanced buildout (HM.13/HM.14/HM.15/HM.16) and is now stale in its *collective* framing — Beginner and Advanced are both very much supported today, for HALF_MARATHON. However, the comment's **Experienced-specific** claim is independently and decisively corroborated by every other piece of direct evidence in §3-§4 above (zero catalog files, zero code references, a live regression test proving 10K non-support). The comment is stale collectively but its narrow claim about Experienced remains true today by direct, independent verification — not because the comment says so.

**Conclusion: `AUTHORITY_MISSING`.** There is no 10K Experienced precedent to inherit. 10K's own most mature build treats Experienced as deliberately, actively out of scope — proven by a standing test, not merely by omission.

## 5. Historical HM-specific Experienced candidate — recorded, explicitly not promoted

`HM_21K_Claude_Handoff_and_Build_Plan.md:165-190` (a `DELIBERATE_LEGACY_RECORD`, per HM.19's own classification convention) contains a pre-catalog-engine candidate table:

```
                3D       4D       5D
Intermediate   28–40    36–50    44–58
Advanced       36–48    46–60    54–70
Experienced    40–52    50–66    60–76      (km/week peak-volume bands)

Starting long-run cap:
Beginner/New              8 km
Intermediate             12 km
Advanced/Experienced     15 km   (grouped together, NOT independently derived)
```

This table predates the entire GEN.*/HM.* catalog engine build (it predates even HM.0). Per this phase's own explicit instruction, it is evidence of **historical intent**, not automatic authority: (a) it groups Advanced and Experienced's starting-LR-cap together, itself hinting the original authors saw no meaningful difference between the two tiers on that specific field; (b) none of these numbers were ever carried into the live `VolumeSafetyPolicy.cs`/taper-policy source, which today only implements Beginner/Intermediate/Advanced; (c) the two dead `peak-volume-bands.v1.json`/`v2.json` TEN_K/EXPERIENCED rows (`3D=[40,55], 4D=[46,62], 5D=[50,68]`) are a *different* historical candidate, from a *different* (later, catalog-engine-era) source, for a *different* distance, with different numbers than this HM-specific table — meaning even the historical record itself disagrees on what "Experienced" volume should be, with no reconciliation ever attempted or needed at the time, because Experienced was dropped before either candidate became live.

**This confirms rather than resolves the authority gap**: two independent historical sources each proposed real numbers for Experienced, at two different points in the product's history, and both were abandoned before shipping — not converged upon.

## 6. Experienced vs Advanced — mandatory real-differentiation analysis

Searched all root-level `.md` files, `PHASE_*.md` reports, and `plan-catalog/docs/**` for any product-design prose (not bare enum listings) explaining what should distinguish Experienced from Advanced.

**Found exactly one piece of authored product-facing language**, `RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md:23-28`, the approved onboarding self-description copy:
- Advanced: *"I train consistently and regularly run 10 km or more."*
- Experienced: *"I have a strong running base and regularly train for longer distances."*

**Everything else** (`RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md:153-155`, `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md:60-68,240-241`, `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md:514`) treats Advanced and Experienced as one undifferentiated pair — either describing only routing/HTTP behavior (404, no coercion), or explicitly noting the taxonomy drift, never articulating a training-philosophy or workout-type distinction between them.

**Analysis**: The one authored distinction that exists is thin but real, and it points in exactly one direction — **greater sustained volume / longer-distance training history**, not a different *kind* of training stimulus, workout taxonomy, or coaching sophistication. There is no evidence anywhere of an intended "Experienced trains differently" philosophy (e.g., more structured intensity, different periodization, different race specificity). This matters directly for this phase's own governing constraint: it forbids assuming "Experienced = Advanced + more kilometers" **unless evidence genuinely supports it** — and here, the only evidence that exists (thin as it is) supports exactly that framing, not a genuinely different stimulus model. This is the opposite risk from Advanced 5D's KEY2 decision in HM.15 (where real evidence justified a genuinely new stimulus); here, the honest reading of the evidence is that no genuinely new stimulus is indicated, only "more of the same," which is itself informative for what Experienced-specific authority *would* look like *if* it existed — but does not manufacture the missing authority to build it on.

## 7. Per-frequency product-eligibility resolution (3D / 4D / 5D independently)

Per this phase's own instruction not to assume all three frequencies are eligible together: each was evaluated independently against the same blocker.

- **3D**: No real Experienced dose/hard-stimulus/peak-volume authority exists anywhere (§3-§4) to combine with HM's own frozen 3D structural authority (HM.2/HM.7/HM.13/HM.15). `BLOCKED`.
- **4D**: Same blocker — no real Experienced authority exists to combine with HM's own frozen 4D structural authority (HM.3/HM.4/HM.8/HM.13/HM.16). `BLOCKED`.
- **5D**: Same root blocker, compounded — Advanced 5D's own KEY2 model (HM.15, THRESHOLD_TEMPO in Build/RaceSpecific) was itself a genuinely novel, escalated, HM-Advanced-specific decision; extending or altering it for a hypothetical Experienced 5D would require *first* establishing whether Experienced needs its own second-hard-stimulus answer at all — which in turn requires the same missing base-level Experienced authority this entire audit could not find. `BLOCKED`.

All three frequencies share **one identical root blocker** (absence of any real, pre-existing, cross-distance Experienced-level authority), not three independent blockers — this is recorded as a single blocker class, not three.

## 8. Numeric-authority freezing — not reached

Per §7, no cell reached eligibility, so no numeric-authority freezing work (peak bands, selected peak, growth rates, absolute caps, calibration starts, LR-share quadruples, absolute-peak-LR, starting-LR-cap, taper duration/multipliers, dose/hard-stimulus authority, 5D KEY2 authority) was performed. Attempting to freeze numbers in the absence of real base-level authority would mean HM unilaterally inventing the first-ever cross-distance Experienced semantics for the entire product — which this phase's own governing prompt (§37) explicitly prohibits: *"Any HM Experienced semantics must remain compatible with existing 10K Experienced taxonomy/modifier behavior... encode it as HM authority not a silent rewrite of the global level definition."* There is no existing global level definition to remain compatible with (§3-§4) and no HM-only workaround that would not itself constitute originating that definition.

The historical HM-specific and TEN_K-specific candidate numbers (§5) are recorded as candidates only — explicitly not adopted, not interpolated, not averaged, and not used as a fallback default. Per this engagement's own established discipline (mirroring HM.19's historical-candidate register), citing an old number is not the same as promoting it to authority.

## 9. 10–16W feasibility simulation — not reached

Not applicable; no cell is eligible to simulate (§7-§8).

## 10. HM V1 zero-delta preservation — explicit statement

**Zero production, catalog, schema, or frontend files were read for modification and none were modified.** This phase performed pure evidence-gathering and product-judgment reasoning (matching HM.13/HM.15's own governance-phase pattern). The existing public HM V1 matrix is untouched and unaffected:
```
Beginner       3D PUBLIC   4D PUBLIC   5D PRODUCT_INELIGIBLE
Intermediate   3D PUBLIC   4D PUBLIC   5D PUBLIC
Advanced       3D PUBLIC   4D PUBLIC   5D PUBLIC
Experienced    3D BLOCKED  4D BLOCKED  5D BLOCKED   (this phase's own finding — no change to any other row)
```
No do-not-reopen item from HM.19's register (§26 of the HM.19 handoff) was touched, reopened, or reinterpreted by this phase.

## 11. 10K taxonomy-consistency statement

This phase's finding is, by construction, maximally consistent with 10K: it changes nothing about 10K's own Experienced treatment, and in fact its central evidence (§4) is drawn directly from 10K's own live, passing regression test proving Experienced is deliberately unrecognized there too. HM and 10K remain in identical states with respect to Experienced (`BLOCKED`/`NotSupported` in both), which is the strongest possible form of cross-distance consistency — no divergence was introduced in either direction.

## 12. 6D deferral

Not evaluated in this phase, per explicit instruction (6D remains scoped to the future HM-X1 track regardless of level). This phase's finding does not change HM-X1's own scope or priority.

## 13. Files inspected

`backend/RunningApp.Domain/Enums/RunningBackground.cs`; `plan-catalog/src/PlanCatalog.Contracts/Enums/RunningExperience.cs`; `plan-catalog/catalog/policies/peak-volume-bands.v1.json`, `.v2.json`, `.v9.json`; `plan-catalog/catalog/level-modifiers/` (directory listing); `backend/RunningApp.Application/RuntimeCatalog/PreviewRouting/V1CatalogPilotIdentityPolicy.cs`; `backend/RunningApp.Application/RuntimeCatalog/PlanCatalogDomainMapper.cs`; `plan-catalog/tests/PlanCatalog.Tests/Golden/PeakVolumePolicyImmutabilityTests.cs`; `backend/RunningApp.IntegrationTests/RuntimeCatalog/Schedule/LongHorizon/Gen9AdvancedCombinedDarkVerificationTests.cs`; `backend/RunningApp.IntegrationTests/RuntimeCatalog/Prescription/Volume/Gen4DBeginnerFourDayCoreTests.cs`; `HM_21K_Claude_Handoff_and_Build_Plan.md`; `RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md`; `RUNNING_BACKGROUND_V2_1_INTERMEDIATE_PILOT_CLOSURE.md`; `PHASE4G_4A_PREPARATION_RUNWAY_CANONICAL_RECONCILIATION_AUDIT.md`; `HALF_MARATHON_V1_FINAL_CLOSURE_AND_EXPANSION_HANDOFF.md`.

## 14. Files created

- `PHASE_HM_E1_EXPERIENCED_3D_4D_5D_PRODUCT_ELIGIBILITY_REUSE_AND_NUMERIC_AUTHORITY_CLOSURE.md` (this file).

## 15. Files modified

- `PHASE_LEDGER.md` — new row (governance bookkeeping only).
- `MASTER_ROADMAP.md` — new short entry (governance bookkeeping only).

**No `.cs`, catalog JSON, schema, database migration, or Flutter runtime source file was touched** — confirmed via `git status --porcelain` immediately before finalizing this report.

## 16. Do-not-reopen additions

Add to the standing register: **Experienced-level absence is an active, test-enforced invariant** (`PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows`, `ExperiencedLevel_RemainsUnrecognized_ForEveryFrequency`), not a gap awaiting the next convenient phase to fill. Any future phase proposing real Experienced authority must be a dedicated, cross-distance authority phase (not HM-scoped) that first amends or removes those two tests with its own new evidence base — HM must not attempt a narrow, HM-only Experienced implementation that leaves those tests (and 10K) unaddressed, per §37 of this phase's own governing prompt.

---

## FINAL CLASSIFICATION

```
HM_EXPERIENCED_3D_4D_5D_AUTHORITY_BLOCKED
```

**Exact blocker (single, shared across all three frequencies):** No real, pre-existing, cross-distance `Experienced`-level catalog/modifier/dose/hard-stimulus authority exists anywhere in the product today, for any distance including 10K — its absence is a deliberately maintained, test-enforced invariant (`PeakVolumePolicyImmutabilityTests.PolicyV3_HasNoBeginnerAdvancedExperiencedEliteRows`; `Gen9AdvancedCombinedDarkVerificationTests.ExperiencedLevel_RemainsUnrecognized_ForEveryFrequency`), not an unaddressed gap. The only authored product-facing differentiation from Advanced (`RUNNING_BACKGROUND_V2_FRONTEND_BACKEND_ALIGNMENT.md:23-28`'s onboarding copy) is too thin to derive real numeric or structural authority from, and the two historical numeric candidates that do exist (`HM_21K_Claude_Handoff_and_Build_Plan.md`'s HM-specific table; `peak-volume-bands.v1/v2.json`'s TEN_K rows) disagree with each other and were both abandoned before shipping. Per this phase's own §37, HM may not originate the first-ever cross-distance Experienced semantics unilaterally; that authority must come from a dedicated, non-HM-scoped phase. 3D, 4D, and 5D are `BLOCKED` for the identical reason — this is one blocker, not three. No HM V1 public matrix row, do-not-reopen item, or 10K behavior was changed by this phase.
