# Revision 4 → Revision 4.1 Canonical Reconciliation

**Status of this document: AUDIT EVIDENCE, not a canonical authority.** Nothing in this document is DECIDED by virtue of appearing here. Every clause below marked `PROPOSED_FOR_USER_FREEZE` requires explicit user review before Revision 4.1 (or any successor) can be treated as canonical. `appsel-adaptation-v1-canonical-spec — Revision 4.1.md` itself was **not modified** in this phase.

## Summary

Revision 4.1 changes are concentrated entirely in **§7** (two new subsections + one modified paragraph), with small mechanical follow-ons in §10/§11/§12/§13/§14 (UI-language row, non-goal bullets, one backlog row, two implementation notes, and updated "next phase" prose). No change touches §1–§6, §8, §9 — the entire ScheduleRepairPolicy family, Safety Path, Session Provenance, and WindowExecutionSummary sections are byte-for-byte identical between Rev4 and Rev4.1.

## Semantic diff, clause by clause

### 1. Title / revision header

- Rev4: `Revizyon 4 — Maintain/Reduce Numeric Anchor Semantics (ReduceBand çözüldü)`
- Rev4.1: `Revizyon 4.1 — Rounding Tolerance + Target-Week Numeric Infeasibility (kapanış)`

**Classification: DOCUMENTATION_ONLY.**

### 2. New "Rev4.1 notu" (introductory changelog block)

New paragraph summarizing the two findings below, framed explicitly as findings from real HTTP/catalog testing ("4M.4B.2/2A/2B/2C confirmation dizisi"), stating up front that neither finding changed the numeric formula.

**Classification: DOCUMENTATION_ONLY** (it is a changelog note, asserts nothing new on its own — the substance is in the sections it points to).

### 3. §7 "Severity-ordering invariant" — modified

- **Rev4 text (verbatim):** `NextWindowTargetAnchor(Maintain) ≤ NextWindowTargetAnchor(ProgressAsPlanned) tarafı ise CatalogProgressionStep'in monoton olmayan (plato/regresyon) davranışı olabileceğinden varsayılmaz, testle doğrulanır (BACKLOG değil, 4M.4B.2'nin test kapsamı).`
  - Plain reading: Rev4 does **not** assert this inequality holds. It explicitly flags it as *unverified*, dependent on `CatalogProgressionStep`'s own possible non-monotonic behavior, and defers verification to 4M.4B.2's test scope. Rev4 makes **no claim about what happens if verification fails** — there is no acceptance criterion, no tolerance, no fallback framing in Rev4 for this case.
- **Rev4.1 text (verbatim, replacing the above):** `NextWindowTargetAnchor(Maintain) ≤ NextWindowTargetAnchor(ProgressAsPlanned) tarafı ise kesin (strict) olarak doğru değildir — 4M.4B.2B'nin gerçek-catalog sweep'i ... 183 geçerli case'in 94'ünde (%51) Maintain > ProgressAsPlanned bulmuştur. Sapma her zaman küçük: maks. mutlak 0.247km, maks. relatif %1.36, ve CatalogProgressionStep'in kendi session-distance rounding davranışından kaynaklanır ...`
- **Phase this entered:** 4M.4B.2B discovered the 94/183 result; 4M.4B.2C wrote it into the spec as Rev4.1.
- **Triggering finding:** the real-catalog sweep in `MaintainNotExceedingProgressAsPlannedInvariantTests` (originally built in 4M.4B.2, run for the first time with real data in 4M.4B.2B).
- **Rationale used:** the deviation is small and attributed to catalog session-distance rounding, not to Maintain applying any progression step.
- **Did production behavior already exist before this text?** Yes — the numeric formulas (`Maintain = PriorValidatedCheckpointLoad`, `ProgressAsPlanned = CatalogProgressionStep(...)`) were unchanged from 4M.4B.2 onward; only the *canonical description of their relationship* changed, from "unverified, to be tested" to "verified false in the strict sense, quantified, and reclassified as acceptable within a bound."
- **Did the user explicitly approve this specific text before it was written?** **No.** The user's 4M.4B.2B/2C phase prompts asked me to investigate and report; the phrase "94/183... max relative 1.36%" first appears as *my own investigation output*, later folded into canonical spec language by me in 4M.4B.2C. The user's 4M.4B.2C prompt *did* pre-specify the shape of the acceptance rule (see item 4 below) but did not see or approve this exact restated paragraph before it was written into the spec file.
- **Classification: TEST/IMPLEMENTATION_OBSERVATION_ENCODED_AS_CANONICAL.** This is the load-bearing item this audit exists to surface: a real measured result was written directly into canonical language without a prior, separate user sign-off step on that exact language.
- **Implementation/tests depending on it:** `MaintainNotExceedingProgressAsPlannedInvariantTests.Maintain_DoesNotMateriallyExceedProgressAsPlanned_BeyondRoundingTolerance` (asserts the 1.5% bound, not the strict inequality).

### 4. §7 new subsection: "ROUNDING PRODUCT DEFAULT"

- **Rev4:** no equivalent text exists anywhere in Rev4.
- **Rev4.1 (new):** *"V1 kabul kriteri: Maintain, ProgressAsPlanned'i MATERYAL olarak aşmamalıdır. 'Materyal' = relative deviation > %1.5. Rounding-only sapma (<= %1.5) kabul edilebilir PRODUCT DEFAULT'tur. Bilimsel bir eşik iddiası DEĞİLDİR..."* Explicitly lists what was *not* done (no clamp, no rounding change, no runtime constant).
- **Phase entered:** 4M.4B.2C.
- **Triggering finding:** same sweep as item 3.
- **Rationale:** the user's own 4M.4B.2C phase-kickoff prompt, Section A, verbatim specified: *"Freeze the V1 product default as: Maintain must not MATERIALLY exceed ProgressAsPlanned. A rounding-only deviation of <= 1.5% is acceptable... Classification: PRODUCT DEFAULT."* — **the 1.5% number and the "PRODUCT DEFAULT" classification were both dictated by the user directly**, not derived or chosen by me. My role in 4M.4B.2C was to (a) verify the real data stayed within that pre-specified bound (it does: max 1.36% < 1.5%) and (b) transcribe the user's already-specified rule into canonical spec prose.
- **Did production behavior already exist first?** N/A — this is a governance/acceptance-criterion clause, not a runtime behavior; nothing at runtime reads "1.5%" (confirmed: the constant lives only in the test file, `MaintainNotExceedingProgressAsPlannedInvariantTests.RoundingToleranceRelativeDeviation`, deliberately not promoted to a shared/runtime constant).
- **Did the user explicitly approve this before Rev4.1 was written?** **Yes, the *rule and number* were user-specified in the phase prompt.** What the user has *not* yet done is review the specific canonical-document sentences I wrote to encode that rule, or independently confirm the underlying 183-case measurement this document's own §21 correlation work (Part C of this audit) is meant to support.
- **Classification: NEW_PRODUCT_DEFAULT** (user-directed value, not user-reviewed final text).
- **Implementation/tests depending on it:** same test as item 3.

### 5. §7 new subsection: "TARGET PRESCRIPTION INFEASIBILITY"

- **Rev4:** no equivalent text. Rev4's only related content is the *zero-completion* Reduce degeneracy (`min(undefined, X) = X`) and the general framing in §13.9/§9 that "LoadDecision is not durably persisted" — nothing about a *valid, non-degenerate* anchor being numerically insufficient for a target week's catalog minimums.
- **Rev4.1 (new):** *"V1 kanonik davranışı: selected Maintain/Reduce numeric anchor hedef phase/week'in catalog minimum prescription'ını karşılayamıyorsa: → anchor YUKARI ARTIRILMAZ → catalog minimum'a clamp edilmez → ... Bunun yerine: mevcut typed continuation block korunur — LONG_HORIZON_CONTINUATION_BLOCKED..."* plus an explicit architectural invariant (Catalog=progression authority / Adaptation=anchor-constraint authority) and a "multi-window acceptance" clarification (not every Reduce→Maintain→Progress sequence must succeed).
- **Phase entered:** 4M.4B.2B discovered the real mechanism (`CoreJitContextUnavailable`/`FourDaySessionDistanceAllocationPolicy` rejecting small anchors, symmetric across Maintain/Reduce); 4M.4B.2C wrote it into the spec.
- **Triggering finding:** the A/B reproduction in 4M.4B.2B (real Maintain-carried anchor and real Reduce-selected anchor of equal magnitude both rejected identically at the same target week).
- **Rationale:** the user's 4M.4B.2C phase prompt, Section C, verbatim specified the entire rule, almost clause-for-clause identical to what ended up in Rev4.1: *"Freeze V1 behavior as: If the selected Maintain/Reduce numeric anchor cannot support the target phase/week's canonical catalog minimum prescription: → DO NOT increase the anchor → DO NOT clamp upward... Instead: preserve the existing typed continuation block: LONG_HORIZON_CONTINUATION_BLOCKED..."*
- **Did production behavior already exist before the canonical text?** **Yes, completely and exactly** — confirmed by direct code audit in 4M.4B.2C §11 (traced the full path from `FourDaySessionDistanceAllocationPolicy`'s exception through `IsBlock` to the HTTP 409) *before* any spec text was written, and confirmed **zero production code changed** in that phase. This is the cleanest possible case of "documented existing behavior," not "invented new behavior."
- **Did the user explicitly approve this before Rev4.1 was written?** **Yes, the rule itself was dictated verbatim by the user in the phase prompt.** As with item 4, what remains unreviewed is the final canonical-document prose, not the underlying rule.
- **Classification: NEW_DECIDED_PRODUCT_BEHAVIOR**, but with the important caveat that "new" here means *newly written into the canonical document*, not newly introduced into the runtime — the runtime behavior is old and unchanged; only its canonical status changed from undocumented to documented.
- **Implementation/tests depending on it:** `LongHorizonThreeWindowAnchorThreadingE2ETests.RealChain_ReduceLandingOnRunwayCoreBoundary_ThenMaintain_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement`, `RealReduceLandingOnRunwayCoreBoundary_BlocksOnGenuineCatalogMinimumVolume_WithoutFalseAdvancement`.

### 6. §10 (Engine vs UI dili) — one new row

Adds: *"(rev4.1) Target prescription infeasibility (mevcut typed block, yeni bir UI metni değil) → Mevcut '...' block dili."* Explicitly states no new UI copy was introduced — it points at the pre-existing block message.

**Classification: DOCUMENTATION_ONLY.**

### 7. §11 (V1 Non-Goals) — two new bullets

Adds: *"(rev4.1) infeasible bir target week için anchor'ın yukarı clamp edilmesi"* and *"(rev4.1) infeasible bir target week için sentetik/hafifletilmiş workout structure üretilmesi"*.

**Classification: DOCUMENTATION_ONLY** (restates prohibitions already implied by item 5; does not itself authorize or forbid anything new beyond item 5).

### 8. §12 (BACKLOG) — one new row + one new note

New row: *"(rev4.1) %1.5 rounding tolerance'ın kalibrasyonu | PRODUCT DEFAULT olarak dondu (4M.4B.2C); ilk kullanıcı/production verisiyle yeniden değerlendirilebilir..."* — explicitly flags the 1.5% number itself as revisitable, not permanent.

New "Rev4.1 notu" confirms `ReduceBand`, percentage reduction, `RecoveryWeek`, and "numeric translation DecisionRequired" were **not** re-opened or re-added.

**Classification: BACKLOG_CHANGE** (the new row) + **DOCUMENTATION_ONLY** (the confirmation note).

### 9. §13 (Implementation Notes) — two new numbered notes (10, 11)

Note 10: restates item 3/4 as an implementation-facing warning ("do not code `Maintain <= ProgressAsPlanned` as a strict runtime invariant anywhere").
Note 11: restates item 5 as an implementation-facing warning ("do not interpret `CoreJitContextUnavailable` as an upward-clamp opportunity").

**Classification: DOCUMENTATION_ONLY** (both are restatements/warnings derived from items already covered above, aimed at future implementers, not new rules themselves).

### 10. §14 — "sonraki fazlar" / "Sıradaki adım" updated

Lists the 4M.4B.2/2A/2B/2C phase chain and states the numeric-anchor/policy scope is closed pending this revision's approval, replacing Rev4's "4M.2 persistence layer next" framing (which was already stale relative to the actual phase history by the time Rev4.1 was written).

**Classification: DOCUMENTATION_ONLY.**

## First checkpoint + zero completion — explicit trace (as required)

- **What Rev4 said:** §7's Reduce formula already covers this exact case in its own text: *"EffectiveCompletedCount == 0 ise: NextWindowTargetAnchor = PriorValidatedCheckpointLoad (min(tanımsız, X) = X ...)"* — but this presupposes `PriorValidatedCheckpointLoad` exists. Rev4 does not separately address the case where it does *not* exist yet (a plan's true first-ever checkpoint).
- **What Rev4 did NOT define:** the doubly-degenerate case (zero completion **and** no prior anchor at all) — what should happen numerically or as an HTTP outcome.
- **Observed runtime behavior:** confirmed real, via `LongHorizonFirstCheckpointNumericAnchorTests.FirstCheckpoint_ZeroCompletion_NoPriorNoEvidence_BlocksWithExistingTypedConflict_NoNumericFallback` (built in the 4M.4B.2 confirmation pass, predating Rev4.1) — the selector returns `null`, which the pre-existing `isJitEvidenceUnavailable` check turns into the same typed 409 that predates all of this work.
- **Did Rev4.1 add any rule for this state?** **No.** Rev4.1's two new subsections (items 4/5 above) do not mention or change this case at all. It remains governed by Rev4's own literal text plus the pre-existing (older than Rev4) typed-Block mechanism.
- **Was `LONG_HORIZON_CONTINUATION_BLOCKED` encoded as DECIDED behavior, or merely documented as existing runtime behavior?** It was never separately "encoded" for this specific case in either Rev4 or Rev4.1 — the Block here falls out of Rev4's own existing zero-completion formula colliding with a pre-existing, older-than-Rev4 typed-conflict mechanism, and was only ever *verified*, never freshly *decided*, in any phase from 4M.4B.2 onward. **This is not user-approved-as-a-new-rule; it is Rev4's own formula's natural consequence, empirically confirmed.**

## 1.5% Maintain/Progress rounding tolerance — explicit trace (as required)

- **Exact section added:** §7, "ROUNDING PRODUCT DEFAULT" (item 4 above).
- **Exact wording:** see item 4's quoted Rev4.1 text.
- **Exact classification:** `PRODUCT DEFAULT` (Rev4.1's own label, per the user's own phase-prompt instruction to classify it that way).
- **Why 1.5% was chosen:** **it was not derived from the data by me** — the user's 4M.4B.2C phase prompt specified "1.5%" directly, before any distribution analysis beyond the single max-value sweep existed. The number was *validated* against the max observed value (1.36% < 1.5%) but not *computed from* a formal statistical procedure over the full distribution — that distributional analysis is what Part C of this current audit phase now provides.
- **What measured data existed when chosen:** at the time the user specified 1.5% (in the 4M.4B.2C prompt), the only measured data in front of either party was the 4M.4B.2B finding: 94/183 violations, max relative 1.36%. No percentile/median/bucket distribution existed yet.
- **Does any external evidence exist for 1.5%?** No literature, standard, or external benchmark was cited or consulted for this number, by the user or by me. It is explicitly self-declared as non-scientific in the spec text itself.
- **Is it purely a product default derived from observed catalog output?** Yes, by the spec's own explicit language, and by the actual process: chosen as a round number comfortably above the single observed maximum, not derived from any deeper statistical or catalog-design principle.

## Candidate clauses for user freeze

The following exact Rev4.1 clauses are put forward as `PROPOSED_FOR_USER_FREEZE`. None are asserted as `DECIDED` by this document.

1. **PROPOSED_FOR_USER_FREEZE** — §7 ROUNDING PRODUCT DEFAULT, verbatim as written in Rev4.1 (item 4 above), including the exact 1.5% figure.
2. **PROPOSED_FOR_USER_FREEZE** — §7 TARGET PRESCRIPTION INFEASIBILITY, verbatim as written in Rev4.1 (item 5 above), including the architectural invariant and multi-window acceptance clarification.
3. **PROPOSED_FOR_USER_FREEZE** — the modified severity-ordering paragraph (item 3 above), specifically the claim that the 94/183 result is "always... rounding-only."  **This specific claim is the subject of Part C of this audit and should not be frozen until that forensic analysis is reviewed** — see `MAINTAIN_VS_PROGRESS_ORDERING_FORENSIC_AUDIT.md`.

All other Rev4.1 changes (items 1, 2, 6, 7, 8's confirmation note, 9, 10) are DOCUMENTATION_ONLY/BACKLOG_CHANGE and do not require a separate freeze decision beyond acknowledging they accurately restate items 3–5.
