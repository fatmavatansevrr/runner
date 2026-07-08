# Golden Fixture v2 — 10K / Intermediate / 4 Gün / 12 Hafta

**Bu v2 sürümdür.** v1'de yapılan detaylı review'da bulunan 5 P0 + 11 orta-öncelik
sorununun tamamı burada düzeltildi. v1→v2 değişiklik listesi §9'da.

---

## 1. Input snapshot

| Alan | Değer |
|---|---|
| Distance | TEN_K |
| Running experience | INTERMEDIATE |
| Selected runs/week | 4 |
| Preferred days | TUESDAY, THURSDAY, SATURDAY, SUNDAY |
| Average weekly volume range | KM_21_30 → anchor **24 km** |
| Longest run (son 30 gün) | 9 km |
| Recent race | 5K, 24:10 (1450s), 2026-06-15 |
| Race date | 2026-10-25 (Pazar) |
| Goal time | 50:00 (3000s) |
| Confirm date | 2026-07-30 |
| Plan start | **2026-08-03** |

## 2. Resolver çıktıları (düzeltilmiş sıra ile)

| # | Resolver | Sonuç |
|---:|---|---|
| 1 | Current Capacity | anchor 24 km |
| 2 | Long-run Compatibility | 9/24=0.375 → **ACCEPTABLE**, factor 0.90 |
| 3 | Core Entry Readiness | **STANDARD**, gaps=[] |
| 4 | Preparation Gap | requiredRunwayWeeks=**0** |
| 5 | Time Adequacy | **ADEQUATE**, runwayWeeks=0 |
| 6 | Starting Long Run | min(9×0.90=8.1, 24×0.36=8.64, 10) = **8.0 km** |
| 7 | Phase Allocation | Foundation 3 / Build 4 / Race-Specific 4 / Taper 1 |
| 8 | **Provisional** Peak Volume | reachablePeak=**38 km** (band [30,42]) |
| 9 | Pace Conversion (Riegel k=1.06) | 1450×2^1.06 = **3023.15s (50:23)** |
| 10 | Pace Profile | easy 350-375, threshold 315-320, 10K 300-305, goal 300 s/km |
| 11 | Goal Feasibility | goalGapRatio=0.007658 (**%0.77**, rounded from raw ratio) → **REALISTIC** |
| 12 | Training Suitability | SUITABLE |
| 13 | Warning Gate | **Yok** |
| 14 | Plan Mode | **STANDARD** |
| 15 | **Final** Peak Volume | STANDARD mode → provisional aynen onaylanır: **38 km** |
| 16 | Load & Pace Dosage | cutback %15.6 (hafta 7), post-cutback baseline uygulandı |
| 17 | Taper | pre-taper ref 37.5 → training 20.0 km = **%46.7 azaltım** |
| 18 | Final Validation | tüm invariant'lar PASS (1 not: hafta 11 long-run payı kasıtlı olarak bandın hafif altında) |

> **Neden peak volume artık iki aşamalı?** v1'de Goal Feasibility resolver'ı, henüz çalışmamış Peak Volume adımının sonucunu (`resolvedPeakKm`) kullanıyordu — bu deterministik pipeline'da geçersiz bir bağımlılıktı. v2'de peak, plan mode belirlenmeden önce **provisional** olarak hesaplanır (Goal Feasibility ve diğer adımlar bunu okuyabilir), plan mode belirlendikten sonra ise **final** olarak onaylanır/ayarlanır (STANDARD'da değişmez, COMPRESSED'de azaltılırdı). Circular dependency oluşmaz.

## 3. Phase / hafta dağılımı

| Phase | Haftalar |
|---|---|
| FOUNDATION | 1-3 |
| BUILD | 4-7 (7. hafta cutback) |
| RACE_SPECIFIC | 8-11 |
| TAPER | 12 (race week) |

## 4. Hafta hafta özet (düzeltilmiş)

| Hafta | Phase | Toplam (km) | Long run (km) | Pay | Key session | Key session sınıfı |
|---:|---|---:|---:|---:|---|---|
| 1 | FOUNDATION | 24.0 | 8.0 | 33.3% | EASY_WITH_STRIDES | LOW |
| 2 | FOUNDATION | 25.0 | 8.5 | 34.0% | EASY_WITH_STRIDES | LOW |
| 3 | FOUNDATION | 26.5 | 9.0 | 34.0% | EASY_WITH_STRIDES | LOW |
| 4 | BUILD | 28.0 | 9.5 | 33.9% | FARTLEK | HARD |
| 5 | BUILD | 30.0 | 10.0 | 33.3% | THRESHOLD_TEMPO | HARD |
| 6 | BUILD | 32.0 | 10.5 | 32.8% | FARTLEK | HARD |
| 7 | BUILD (**cutback %15.6**) | 27.0 | 9.5 | 35.2% | EASY_WITH_STRIDES | LOW |
| 8 | RACE_SPECIFIC | 33.0 | 11.0 (standard) | 33.3% | TEN_K_REPETITIONS | HARD |
| 9 | RACE_SPECIFIC | 35.0 | 11.5 (steady finish, **MODERATE**) | 32.9% | THRESHOLD_TEMPO | HARD |
| 10 | RACE_SPECIFIC | 37.0 | 12.0 (standard) | 32.4% | TEN_K_REPETITIONS | HARD |
| 11 | RACE_SPECIFIC (peak) | 38.0 | 11.0 (**bilinçli azaltıldı**) | 28.9% | RACE_PACE_REPEATS | HARD |
| 12 | TAPER (race week) | 20.0 train + 10.0 race = 30.0 | — (race günü) | — | Kısa aktivasyon | MODERATE |

**Değişenler (v1→v2):**
- Hafta 7: 24.0 → **27.0 km** (cutback %25 → **%15.6**, Intermediate cutback bandı [15,20]% ile uyumlu)
- Hafta 9 long run: threshold-finish → **"steady finish"** (kontrollü aerobik, 5:30-5:45/km, ~12dk) — artık HARD değil, MODERATE
- Hafta 11 long run: 12.5 → **11.0 km** (aynı hafta içinde goal-pace rehearsal zaten HARD olduğu için ikinci hard-stimulus riski kaldırıldı; azalan hacim Thu/Sat'a dağıtıldı)
- Hafta 12 training hacmi: 17.0 → **20.0 km** (taper matematiği düzeltildi, bkz. §6)
- Slot adı: `PRIMARY_QUALITY` → **`KEY_SESSION`** (family/loadClassification ayrı alanlara taşındı)

## 5. Pace referansları

| Etiket | Pace |
|---|---|
| Easy | 5:50–6:15 /km (350-375 s/km) |
| Long run (standard) | Easy bandıyla aynı |
| Long run steady finish (hafta 9) | son ~12dk: 5:30–5:45/km (330-345 s/km) — **threshold değil** |
| Threshold/tempo | 5:15–5:20 /km (315-320 s/km) |
| 10K-repetitions | 5:00–5:05 /km (300-305 s/km) |
| Race-pace repeats | 5:00 /km (300 s/km, tam goal pace) |

## 6. Taper hesabı (düzeltildi)

```
preTaperReferenceKm = (hafta10 + hafta11) / 2 = (37.0 + 38.0) / 2 = 37.5
taperTrainingVolumeKm = 20.0
reductionPercent = (37.5 - 20.0) / 37.5 × 100 = 46.67%
```

v1'de bu hesap **yanlıştı**: 17 km training hacmi ile "reduction %45.3" denmişti, ama
(37.5−17)/37.5 = **%54.7**'dir — %45.3 aslında *kalan hacmin oranı* idi, azaltım oranı değil.
v2'de training hacmi 20 km'ye çekilerek doğru hesapla (%46.7) mevcut taper bandına
([40,50]%) tam oturması sağlandı — rule dosyasında değişiklik gerekmedi.

## 7. Cutback büyüklüğü (düzeltildi)

```
week6 = 32.0 km, week7 = 27.0 km
reductionPercent = (32.0 - 27.0) / 32.0 × 100 = 15.63%
```

`progression.rules.yaml` artık profile bazlı cutback bandı taşıyor:
`INTERMEDIATE: [15%, 20%]`. v1'deki 24 km (%25 azaltım) bu bandın dışındaydı ve
genel (profile'sız) %20-30 bandına dayanıyordu — bu genel bant kaldırıldı.

## 8. Gün gün program (özet — tam detaylar PlanDocument JSON'da)

Her **KEY_SESSION** günü artık JSON'da tam `components` (warm-up/main-set/recovery/
cool-down, pace aralığı) taşıyor; burada sadece özet veriliyor.

### Hafta 7 (cutback, düzeltilmiş)
| Gün | Slot | Workout | km |
|---|---|---|---:|
| Salı 08-25→**09-15** | KEY_SESSION | EASY_WITH_STRIDES (LOW) | 6.5 |
| Perşembe | EASY_SUPPORT | EASY_STANDARD | 6.0 |
| Cumartesi | EASY_SUPPORT | EASY_STANDARD | 5.0 |
| Pazar | LONG_RUN | LONG_RUN_STANDARD | 9.5 |

### Hafta 9 (long run artık MODERATE, threshold değil)
| Gün | Slot | Workout | km | Sınıf |
|---|---|---|---:|---|
| Salı | KEY_SESSION | THRESHOLD_TEMPO (25dk tempo) | 9.0 | HARD |
| Perşembe | EASY_SUPPORT | EASY_STANDARD | 8.5 | LOW |
| Cumartesi | EASY_SUPPORT | EASY_STANDARD | 6.0 | LOW |
| Pazar | LONG_RUN | LONG_RUN_PROGRESSION (steady finish 12dk @5:30-5:45) | 11.5 | **MODERATE** |

### Hafta 11 (long run azaltıldı, tek hard stimulus)
| Gün | Slot | Workout | km | Sınıf |
|---|---|---|---:|---|
| Salı | KEY_SESSION | RACE_PACE_REPEATS (3×1.5km @5:00/km) | 8.5 | HARD |
| Perşembe | EASY_SUPPORT | EASY_STANDARD | 11.0 | LOW |
| Cumartesi | EASY_SUPPORT | EASY_STANDARD | 7.5 | LOW |
| Pazar | LONG_RUN | LONG_RUN_STANDARD (**azaltıldı**) | 11.0 | LOW |

### Hafta 12 (taper/race, düzeltilmiş training hacmi)
| Gün | Slot | Workout | km |
|---|---|---|---:|
| Salı | KEY_SESSION | RACE_PACE_REPEATS kısa aktivasyon (3×1dk @goal pace) | 7.0 |
| Perşembe | EASY_SUPPORT | EASY_STANDARD | 7.0 |
| Cumartesi | EASY_SUPPORT | EASY_SHAKEOUT | 6.0 |
| **Pazar** | **RACE** | **RACE_DAY** | **10.0** |

Training toplamı: 20.0 km + yarış 10.0 km = **30.0 km**.

## 9. v1 → v2 değişiklik listesi (review'a birebir karşılık)

| # | Review maddesi | Durum |
|---:|---|---|
| 1 | Taper matematik hatası | ✅ Düzeltildi: training 17→20km, doğru %46.7 azaltım |
| 2 | Cutback bandı ihlali | ✅ Düzeltildi: 24→27km (%15.6), profile-bazlı bant eklendi |
| 3 | Trace dependency sırası | ✅ Düzeltildi: iki aşamalı peak (provisional→final) |
| 4 | Preparation-gap adımları eksik | ✅ Eklendi: CORE_ENTRY_READINESS + PREPARATION_GAP |
| 5 | Workout component/prescription eksik | ✅ Eklendi: tüm KEY_SESSION günlerinde tam components |
| 6 | PRIMARY_QUALITY/EASY_WITH_STRIDES çelişkisi | ✅ Düzeltildi: slotRole=KEY_SESSION, family+loadClassification ayrı |
| 7 | Long-run progression 2. hard stimulus | ✅ Düzeltildi: hafta9 MODERATE steady-finish, hafta11 long run azaltıldı |
| 8 | Starting-long-run resolver eksik | ✅ Eklendi, formül açık |
| 9 | Peak seçim gerekçesi yetersiz | ✅ Düzeltildi: increment listesi trace'te, longest-run→peak yanlış nedensellik iddiası kaldırıldı |
| 10 | Week 12 schema farklı | ✅ Düzeltildi: tüm haftalar plannedTrainingDistanceKm/plannedRaceDistanceKm/totalPlannedDistanceKm |
| 11 | Geçersiz UUID | ✅ Düzeltildi: gerçek UUID formatı + ayrı fixtureKey alanı |
| 12 | contentHash canonicalization belirsiz | ✅ Düzeltildi: hashCanonicalizationRule alanı + script ile hesaplandı (elle yazılmadı) |
| 13 | Goal gap yuvarlama | ✅ Düzeltildi: goalGapRatio (ham) ile goalGapPercentDisplay (yuvarlanmış) ayrıldı |
| 14 | Pace türetme trace'te yok | ✅ Eklendi: PACE_PROFILE_RESOLVER adımı |
| 15 | Süre-bazlı seansta exact km yanıltıcı | ✅ Düzeltildi: prescriptionMode + distanceIsEstimate alanları |
| 16 | Warning check adı yanıltıcı | ✅ Düzeltildi: LOW_DISTANCE_READINESS_CHECK → LONG_RUN_COMPATIBILITY_WARNING_CHECK |

**Not:** Hafta 11'in long-run payı (%28.9) preferred bandın (30-36%) hafif altında kalıyor — bu kasıtlı bir tasarım kararı (aynı hafta içinde ikinci hard-stimulus oluşturmamak için) ve final validation'da `PASS_WITH_NOTE` olarak işaretlendi, gizlenmedi.
