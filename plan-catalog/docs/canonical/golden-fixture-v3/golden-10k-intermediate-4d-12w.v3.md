# Golden Fixture v3 — 10K / Intermediate / 4 Gün / 12 Hafta

## Durum

Bu sürüm, v2 incelemesinde kalan P0 maddelerini kapatır ve **ilk kodlanabilir canonical golden master** olarak kullanılabilir.

## Kapanan kritik maddeler

1. Warning hesaplama ile UI acknowledgement gate ayrıldı; suitability artık henüz hesaplanmamış warning sonucuna bağlı değil.
2. Peak target resolver ile weekly volume curve generator ayrıldı; peak resolver artık önceden seçilmiş haftalık artış listesini input olarak kullanmıyor.
3. Mixed workout mesafeleri `EXACT_SESSION_TOTAL`, `ESTIMATED_SESSION_TOTAL` veya `EMBEDDED_COMPONENTS` ile açıkça muhasebeleştiriliyor.
4. Hafta 11 race-pace seansı recovery jog dahil 9.0 km estimate edildi; haftalık toplam 38 km korunarak Perşembe 10.5 km yapıldı.
5. Single-session spike guardrail final validation'a eklendi; plan generation sırasında historical + prior planned baseline kullanılıyor.
6. Taper dağılımı 8 + 8 + 4 km oldu; yarıştan önceki gün shakeout 4 km cap ile sınırlandı.
7. Race day `loadClassification: RACE`; training hard-stimulus sayacından açıkça ayrıldı.
8. Cutback, `PHASE_TRANSITION_DELOAD` olarak işaretlendi; periodic cutback ile aynı kavram sayılmıyor.
9. Root document'a timezone, unit, plan dates, week boundaries ve validation summary eklendi.
10. `CURRENT_LACTATE_THRESHOLD_EFFORT` yerine `ESTIMATED_THRESHOLD_EFFORT` kullanıldı.

## Canonical pipeline v3

```text
Input Snapshot
→ Weekly Volume Resolver
→ Long-run Compatibility
→ Core Entry Readiness
→ Preparation Gap
→ Time Adequacy
→ Starting Long Run
→ Phase Allocation
→ Provisional Peak Target
→ Weekly Volume Curve Generator
→ Pace Conversion
→ Pace Profile
→ Goal Feasibility
→ Warning Policy Evaluation
→ Training Suitability
→ Warning Presentation Gate
→ Plan Mode
→ Final Peak Approval
→ Workout/Load Dosage
→ Taper Distribution
→ Final Validation
→ PlanDocument + DecisionTrace
```

## Haftalık hacim

| Hafta | Phase | Training km | Long run | Ana seans |
|---:|---|---:|---:|---|
| 1 | FOUNDATION | 24.0 | 8.0 | Easy + strides |
| 2 | FOUNDATION | 25.0 | 8.5 | Easy + strides |
| 3 | FOUNDATION | 26.5 | 9.0 | Easy + strides |
| 4 | BUILD | 28.0 | 9.5 | Fartlek |
| 5 | BUILD | 30.0 | 10.0 | Estimated threshold tempo |
| 6 | BUILD | 32.0 | 10.5 | Fartlek |
| 7 | BUILD / phase-transition deload | 27.0 | 9.5 | Easy + strides |
| 8 | RACE_SPECIFIC | 33.0 | 11.0 | 10K repetitions |
| 9 | RACE_SPECIFIC | 35.0 | 11.5 | Threshold + moderate steady-finish LR |
| 10 | RACE_SPECIFIC | 37.0 | 12.0 | 10K repetitions |
| 11 | RACE_SPECIFIC / peak | 38.0 | 11.0 | 3×1.5 km goal pace; 9.0 km estimated total |
| 12 | TAPER | 20.0 + 10.0 race | — | 8 km activation + 8 km easy + 4 km shakeout |

## Golden-master sınırı

Bu fixture artık algoritmanın beklenen çıktısını test etmek için kilitlenebilir. Ancak aynı sayıları diğer seviye/mesafe kombinasyonlarına kopyalamak doğru değildir; kod, rule dosyalarından çözüm üretmelidir.

## Canonical week-boundary semantics

- `planStartDate` is the Monday that opens Week 1.
- Every `weekStartDate` is the corresponding calendar-week Monday.
- Every `weekEndDate` is the corresponding calendar-week Sunday.
- A week may begin with a rest day; `weekStartDate` is not the first scheduled run date.
- Week 12 therefore runs from `2026-10-19` through `2026-10-25`, while its first scheduled run remains Tuesday `2026-10-20`.

## Vocabulary boundary clarification

`PrescriptionMode` and `DistanceAccountingMode` are separate vocabularies and must not be merged.

### PrescriptionMode values used by this fixture

- `DISTANCE`
- `MIXED`

### DistanceAccountingMode values used by this fixture

- `EXACT_SESSION_TOTAL`
- `ESTIMATED_SESSION_TOTAL`
- `EMBEDDED_COMPONENTS`

The three distance-accounting values describe how a session total is reconciled with its components. They are not `PrescriptionMode` values.

Workout-specific component labels present in the generated fixture, such as `FARTLEK_MAIN_SET`, `TEMPO_MAIN_SET`, `INTERVAL_MAIN_SET`, `STEADY_FINISH`, `ACTIVATION_REPEATS`, and `EASY_RUNNING_TO_SESSION_TOTAL`, must not automatically be promoted into a global Plan Catalog enum. Their catalog ownership requires a separate authoring decision.
