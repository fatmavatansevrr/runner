# Appsel Adaptation V1 — Canonical Decision Specification

### Revizyon 5 — Multi-Week Window Aggregation (BLOCKER çözüldü)

Durum etiketleri: **DECIDED** (V1'de bu şekilde uygulanır) /
**BACKLOG** (V2+ için bilinçli olarak ertelendi) /
**PRODUCT DEFAULT** (bilimsel eşik değil, kalibre edilecek başlangıç değeri)

> **Rev2 notu:** 4 preferred day + 4 session pilot yapısında (Mon/Wed/Fri/Sun
> = Easy/Key/Easy/Long), missed bir KEY/LONG için gelecekte hiçbir zaman
> gerçekten *boş* bir preferred slot çıkmaz. Çözüm: **SingleSessionSubstitution**
> (KEY/LONG, gelecekteki bir EASY slotunu alabilir).

> **Rev3 notu:** Rev2'nin `WindowExecutionSummary` → `NextWindowAdaptationPolicy`
> zincirinde iki model hatası vardı: (1) karar sıralaması bazen daha kötü
> adherence'a daha iyi sonuç verebiliyordu, (2) `SafetyReviewRequired`
> load-decision enum'una karışmış, iki farklı boyutu tek enum'a
> sıkıştırıyordu. İkisi de bu revizyonda düzeltildi; ayrıca provenance,
> candidate-selection determinizmi ve `illness` davranışı netleştirildi.

> **Rev3.1 notu:** Rev3'ün §6'sı kendi içinde çelişkiliydi — canonical
> tanım `ExpectedSessionCount`'un Superseded session'ları **hariç
> tuttuğunu** söylüyordu, ama aynı bölümdeki pilot örneği ("Key ✓,
> Long ✓, Easy 1/2") paydanın **2 kaldığını** varsayıyordu — yani
> Superseded'in payı silmediğini. Bu revizyon bu iç çelişkiyi temizliyor:
> **doğru semantics, paydanın silinmediği yöndür.** Superseded session
> orijinal planlama beklentisinin tarihsel bir parçası olarak kalır;
> yalnızca "completed" veya "unrecovered NotToday" sayılmaz — kendi
> nötr kategorisinde durur. Yeni ürün davranışı icat edilmedi, yalnızca
> Rev3'ün kendi iç tutarsızlığı giderildi.
>
> Ayrıca 4M.1 implementasyon audit'inde bulunan bir boşluk kapatıldı:
> canlı `NotToday` endpoint'inin reason vocabulary'sinde `pain_or_discomfort`
> karşılığı hiç yoktu, yani `SafetyReviewRequired` pathway'i production'da
> ulaşılamazdı. §4.1'de `soreness → Safety` eşlemesi ve
> `RuntimeNotTodayReasonMapper` sınır katmanı DECIDED olarak eklendi.

> **Rev4 notu:** 4M.4B.1 audit'i `Maintain`/`Reduce`'un o ana kadar
> tamamen sembolik kaldığını (`NextWindowLoadDecisionPolicy`'nin çıktısı
> composition tarafından hiç okunmuyordu) ve `Maintain` için zaten var
> olan, yeniden kullanılabilir bir `PriorAnchor` mimarisi olduğunu ortaya
> çıkardı. `Reduce` için ise literatürdeki klasik "deload" yüzdeleri
> (%10-40) yanlış senaryoyu hedefliyordu — o senaryo tutarlı antrenman
> yapan birinin yorgunluğu içindir, Appsel'in `Reduce`'u düşük adherence'a
> tepkidir. Bunun yerine "Model 2 — Evidence-Anchored" seçildi: `Reduce`,
> yeni bir yüzde icat etmeden, var olan `ValidatedSustainableLoad`
> (gerçekten tamamlanan mesafe) anchor'ını `PriorValidatedCheckpointLoad`
> ile sınırlayarak kullanır. `ReduceBand` artık BACKLOG'da değil —
> §7'de tam formül DECIDED.

> **Rev4.1 notu (bu revizyon):** Rev4'ün formülünü gerçek `activate-next-window`
> HTTP zinciri ve gerçek catalog pipeline'ı üzerinden doğrulayan
> 4M.4B.2/2A/2B/2C confirmation dizisi iki gerçek, ölçülmüş bulgu
> ortaya çıkardı — ikisi de **numeric formülü değiştirmedi**, yalnızca
> gerçek runtime'ın zaten ne yaptığını belgeledi ve kabul kriterini
> netleştirdi:
>
> 1. **Rounding-only severity deviation:** `Maintain <= ProgressAsPlanned`
>    şiddet sıralaması, gerçek catalog session-distance rounding'i
>    yüzünden **kesin (strict)** olarak doğru değil — 200 case'lik
>    gerçek-catalog sweep'inde 183 geçerli case'in 94'ü (%51) bu sıralamayı
>    ihlal ediyor, fakat sapma her zaman küçük (maks. 0.247km mutlak,
>    **maks. %1.36 relatif**) ve tamamen catalog'un kendi session-distance
>    yuvarlama davranışından kaynaklanıyor — `Maintain`'in bir progression
>    adımı uygulamasından veya adaptation tarafında bir "uplift"ten değil.
>    §7'ye **ROUNDING PRODUCT DEFAULT** olarak eklendi (aşağıya bakınız).
> 2. **Target-week numeric infeasibility:** `CoreJitContextUnavailable`,
>    daha önce şüphelenilenin aksine **Maintain'e özgü bir plumbing hatası
>    değil**. Seçilen (Maintain veya Reduce) anchor, hedef Core/Runway
>    haftasının gerçek catalog minimum prescription'ını
>    (`FourDaySessionDistanceAllocationPolicy`) karşılayamayacak kadar
>    küçükse, catalog bunu simetrik olarak reddeder — hem küçük bir
>    Maintain-carried anchor hem de küçük bir Reduce-selected anchor
>    aynı gerçek mekanizmayla aynı şekilde reddedilir (4M.4B.2B'de A/B
>    reprodüksiyonla doğrulandı). §7'ye **TARGET PRESCRIPTION
>    INFEASIBILITY** olarak eklendi (aşağıya bakınız): onaylanan V1
>    davranışı mevcut `LONG_HORIZON_CONTINUATION_BLOCKED` tipli block'tur
>    — yukarı clamp yok, sentetik workout yok, RecoveryWeek yok.
>
> Her iki bulgu da gerçek, mevcut runtime davranışıyla **zaten tam
> uyumlu** çıktı (4M.4B.2C implementasyon audit'i, §7 sonu) — bu
> revizyon hiçbir production kodunu değiştirmedi, yalnızca hangi
> davranışın DECIDED/PRODUCT DEFAULT olduğunu netleştirdi ve buna göre
> test kabul kriterini güncelledi.

> **Rev5 notu (bu revizyon) — BLOCKER çözüldü:** 4M.5A audit'i,
> `WINDOW_DECISION_POLICY_NOT_DEFINED_FOR_MULTI_WEEK_WINDOWS` bulgusunu
> gerçek veriyle doğruladı — 576 gerçek rolling activation window'un
> **%83.3'ü 16-session** (4 gerçek yapısal hafta tek activation'da
> materialize ediliyor), tek seferlik bir test artifact'ı değil.
> `EffectiveCompletedCount >= 4` dalı 16-session'da %25 completion'da
> bile `ProgressAsPlanned` üretiyor ve role-awareness'ı hiç kontrol
> etmiyordu. 4M.5B decision-design audit'i, orijinal `0-1/2/3/4`
> matrisinin gerçek biriminin **tek yapısal hafta** (1K+2E+1L=4)
> olduğunu kanıtladı ve üç modeli (Window-normalized / B-weekly-summary
> / B-weekly-checkpoint) blast-radius ve domain-semantics açısından
> karşılaştırdı.
>
> **Seçilen model: B-weekly-summary + B1 (worst-week-wins) + orijinal-hafta
> lineage attribution.** §7'nin formülü DEĞİŞMEDEN her gerçek yapısal
> haftaya uygulanıyor, sonuçlar `Reduce < Maintain < ProgressAsPlanned`
> sıralamasıyla worst-week-wins ile aggregate ediliyor — bu sıralama
> zaten Rev4.1'de frozen olduğundan hiçbir yeni sabit/yüzde icat
> edilmedi. `B-weekly-checkpoint` (haftalık ayrı activation) reddedildi
> çünkü window identity/idempotency/chronology/Block-semantics'i
> (4M.4–4M.4B.2C'de frozen) yeniden açardı; `B-weekly-summary` bunların
> hiçbirine dokunmadan yalnızca evidence şeklini değiştiriyor. Tam
> formül ve rationale §7a'da.
>
> **Spec durumu:** `TEN_K_INTERMEDIATE_4D_ADAPTATION_V1` artık
> `CONDITIONALLY_VERIFIED` değil — tüm bilinen blocker'lar (rounding
> tolerance, target-prescription-infeasibility, multi-week window
> aggregation) DECIDED. Sıradaki adım implementasyon (Phase 4M.5C) ve
> ardından final re-acceptance.

---

## 1. Vocabulary Tree (DECIDED)

```
PlanAdaptationEngine
├── ScheduleRepairPolicy
│   ├── Skip
│   ├── RescheduleToEmptySlot
│   └── SubstituteFutureEasy      (SingleSessionSubstitution)
│
├── CandidateSelectionPolicy       — YENİ (rev3, deterministic ordering)
│
├── ScheduleSafetyValidator
│   ├── PreferredDayConstraint
│   ├── HardSessionSeparation
│   ├── LongRunSeparation
│   ├── WindowBoundaryConstraint
│   ├── TaperProtection
│   ├── PhaseBoundaryConstraint    (rev3: artık TÜM faz sınırları)
│   └── NoCascadeMove              (rev3: + chronological immutability)
│
├── SessionPlanningStatus          (Active / Superseded)
│
├── WindowExecutionSummary          (rev3: EffectiveCompletedCount eklendi)
│
├── NextWindowLoadDecision          — rev3: SafetyReviewRequired ayrıldı
│   ├── ProgressAsPlanned
│   ├── Maintain
│   └── Reduce
│
├── SafetyReviewRequired : bool     — YENİ (rev3, ayrı boyut)
│
├── AdaptationDecisionRecord
```

Not: `RollingWindowCheckpoint` kullanılır, `mesocycle` kullanılmaz —
Appsel'in penceresi ürün/teknik bir sınır, spor bilimindeki spesifik
training-block kavramıyla bire bir eşleşmiyor.

---

## 2. Çekirdek Terminoloji (DECIDED)

| Appsel terimi | Canonical EN | Türkçe | Kapsam |
|---|---|---|---|
| adaptation | `PlanAdaptation` | plan adaptasyonu | Üst kavram |
| schedule repair | `ScheduleRepair` | takvim onarımı | Aynı window içi müdahale |
| reschedule | `Reschedule` | yeniden planla | Tarih değişir, workout korunur |
| drop | `Skip` (UI) / `Drop` (engine) | atla | UI ve engine dili ayrı tutulur |
| no makeup | `NoMakeupPolicy` | telafi yok | — |
| next-window adaptation | `NextWindowAdaptationPolicy` | sonraki pencere adaptasyonu | rev3: iki ayrı çıktı üretir (bkz. §7) |
| execution outcome | `ExecutionOutcome` | uygulama sonucu | Planned/Completed/NotToday |
| adherence | `Adherence` | plana uyum | Genel tamamlama oranı |
| safety rule | `SafetyConstraint` | güvenlik kısıtı | Validator tarafından enforce edilir |
| hard-day separation | `HardSessionSeparation` | zor seans ayrımı | — |
| explanation | `AdaptationReasonCode` | adaptasyon neden kodu | Provenance |
| safety flag | `SafetyFlag` | güvenlik işareti | pain/discomfort, illness (schedule repair için) |
| reason class | `ReasonClass` (Operational \| Safety) | neden sınıfı | Raw reason code ayrı kalır — overengineering yok (rev3) |
| tek seans kaydırma sınırı | `SingleSessionDisplacementOnly` | tekil seans kısıtı | Cascade yok |
| kümülatif eşik | `CumulativeMissThreshold` | nötr isim | "Plan Realignment" değil |

**Rev3 düzeltmesi:** `OperationalReason` ayrı bir enum/abstraction
olarak kaldırıldı — gereksiz katman. `ReasonClass = Operational | Safety`
yeterli; raw `NotTodayReasonCode` zaten ayrı tutuluyor (§4).

**Reddedilen/ayrılan eşleştirmeler:** `training load management`
(çok geniş bir spor bilimi alanı; sınıf adı olarak
`PlanAdaptationEngine` kullanılır), `mesocycle checkpoint`
(`RollingWindowCheckpoint` kalır), `cumulative training stress
deficit` (`MissedSessionScore` ya da tamamen kaldırılır),
`Plan Realignment threshold` (Runna'ya özgü ürün dili;
`CumulativeMissThreshold` kullanılır).

---

## 3. ScheduleRepairPolicy — Rule Ailesi (DECIDED)

> **Tetikleyici sınırı (rev3, non-goal):** `ScheduleRepair` yalnızca
> kullanıcının açık `NotToday` onayıyla tetiklenir. V1'de batch/otomatik
> missed-session inference (örn. "uygulama 4 gün açılmadı") yok.

### EasySupportRule
```
EASY_SUPPORT + NotToday
→ Skip
→ no reschedule search
→ no downstream move
```

### KeySessionRule
```
KEY_SESSION + NotToday
↓
ReasonClass == Safety  OR  reason == illness ?     ← rev3
├─ YES → Skip (+ SafetyFlag yalnızca pain_or_discomfort için, bkz. §4)
└─ NO
    ↓
    future empty preferred slot? (same window, aynı Phase, boş)
    ├─ YES + HardSessionSeparation & LongRunSeparation valid
    │      → RescheduleToEmptySlot  (candidate: CandidateSelectionPolicy, aşağı bakınız)
    └─ NO
        ↓
        future EASY_SUPPORT slot var mı? (same window, aynı Phase)
        ├─ YES + spacing valid
        │      → SubstituteFutureEasy
        │        (KEY o slotu alır, EASY → Superseded)
        └─ NO → Skip
```
Saat bazlı eşik (eski "72h" kuralı) kullanılmaz; yalnız structure-based validation.

### LongRunRule
```
LONG_RUN + NotToday
↓
ReasonClass == Safety  OR  reason == illness ?
├─ YES → Skip (+ SafetyFlag yalnızca pain_or_discomfort için)
└─ NO
    ↓
    future empty preferred slot?
    ├─ YES + spacing valid → RescheduleToEmptySlot
    └─ NO
        ↓
        future EASY_SUPPORT slot var mı?
        ├─ YES + spacing valid → SubstituteFutureEasy
        └─ NO → Skip

Taper içindeyse: yukarıdakiler hiç çalışmaz → doğrudan Skip
(bkz. TaperProtectionRule)
```
%70/%80 kısaltılmış long run synthesis'i V1 kapsamı dışında (BACKLOG).

### SingleSessionSubstitution (DECIDED)

Cascade'e girmeden, makeup eklemeden, preferred day dışına çıkmadan
çalışan kontrollü ikame mekanizması:

```
priority session (KEY veya LONG)
        may replace
future EASY_SUPPORT slot (aynı window, aynı Phase içinde)

KEY  cannot replace LONG slotu
LONG cannot replace KEY slotu
```

**Rev3 düzeltmesi — terminoloji:** "haftalık toplam session sayısını
değiştirmez" ifadesi yanlış anlaşılabiliyordu; DB'de replacement
session yaratıldığından fiziksel satır sayısı artar. Doğru ifade:

> `NoMakeupPolicy` **effective active planned session count**'u
> artırmaz — fiziksel satır sayısı değil, aktif/geçerli planlanmış
> seans sayısı sabit kalır.

### CandidateSelectionPolicy (YENİ — rev3, DECIDED)

Hem `RescheduleToEmptySlot` hem `SubstituteFutureEasy` için ortak,
deterministic aday seçimi:

```
Candidate ordering:
1. chronological ascending (en yakın tarih önce)
2. first safety-valid candidate wins

Birden fazla uygun future EASY_SUPPORT veya boş preferred slot varsa
→ en erken geçerli olan seçilir. Rastgelelik veya "en iyi" seçim yok.
```

### Chronological Immutability (YENİ — rev3, DECIDED)

`NoCascadeMove`'un bir uzantısı:

```
Adaptation decisions are applied chronologically and are immutable
once committed.

A later repair cannot rearrange or supersede a previously adapted
priority session.
```

Örnek: Wed KEY → Fri EASY'yi substitute etti ve commit edildiyse,
daha sonra Sun LONG da missed olursa artık Fri'yi kullanamaz (zaten
dolu, KEY tarafından alınmış). Bu durumda LONG için CandidateSelectionPolicy
bir sonraki geçerli adaya bakar; yoksa Skip. Solver yok — sıralı,
commit-edildikçe kilitlenen kararlar var.

### TaperProtectionRule (rev3 — netleştirildi)
Tek cümlelik canonical kural:
```
During Taper: adaptation must never increase planned load.
```
Alt davranışlar (rev3: "neutral load" belirsizliği kaldırıldı):
```
easy          → Skip
key rehearsal → may only be moved UNCHANGED
                (no distance/duration/intensity/segment/role
                modification) AND safety constraints geçmeli
long          → Skip
race date     → immutable
```

### HardSessionSeparationRule / WindowBoundaryRule
Validator seviyesinde enforce edilir, reschedule kararının bir
parçası olarak ayrı kural değildir.

### PreferredDayConstraint
```
valid candidate =
  future
  AND same window
  AND same Phase                    ← rev3
  AND on a PreferredDay
  AND slot empty
  AND safety-valid
```
Preferred day dışına V1'de otomatik çıkılmaz. Boş preferred day
adayı yoksa → `SubstituteFutureEasy` denenir; o da yoksa → Skip.

`SubstituteFutureEasy` adayları zaten yalnızca mevcut aktif
`EASY_SUPPORT` seanslarıdır, dolayısıyla tarihleri zaten
`PreferredDayConstraint`'i sağlar — ayrı bir kontrole gerek yok (rev3).

### PhaseBoundaryConstraint (rev3 — genişletildi)

**Rev2'de yalnızca Taper'a girişi engelliyordu. Rev3: tüm faz
sınırlarına genişletildi** — workout semantiği phase'e göre
değişebileceğinden ve engine'in distance-agnostic kalması için:

```
candidate.Phase == triggerSession.Phase
```

Yani hiçbir reschedule/substitution herhangi bir faz sınırını
(Foundation→Build, Build→RaceSpecific, *→Taper) geçemez — Taper
zaten ayrıca `TaperProtectionRule` ile korunuyor, bu genel kural
onun bir üst kümesi.

---

## 4. Safety Path (rev3 — illness netleştirildi)

```
NotTodayReasonCode
  schedule_conflict | travel | weather | tired | illness |
  pain_or_discomfort | personal | other
        ↓
ReasonClassificationService
        ↓
  Operational  |  Safety
```

`pain_or_discomfort → Safety`. Diğerleri (illness dahil) → `Operational`
sınıflandırması **analytics/UI etiketlemesi için** böyle kalır.

**Rev3 — `illness` için ayrı schedule-repair davranışı (DECIDED):**
S�nıflandırma `Operational` kalsa da, `ScheduleRepair` seviyesinde
`illness` de `pain_or_discomfort` gibi davranır — reschedule/substitute
denenmez:

```
ScheduleRepair açısından:
  pain_or_discomfort  → Skip, no reschedule attempt, SafetyFlag = true
  illness              → Skip, no reschedule attempt, SafetyFlag = false
  diğer tüm operational → normal repair rules (reschedule denenir)
```

Fark: yalnızca `pain_or_discomfort` window-seviyesi `SafetyReviewRequired`'ı
tetikler (§7). `illness`, "hasta olan birine 2 gün sonra Long Run
taşımayalım" sağduyusunu sağlar ama tıbbi yorum yapmaz, kullanıcıyı
review akışına sokmaz — bu ayrım, tanı koymadan konservatif davranmayı
mümkün kılıyor.

Minimal safety davranışı (pain_or_discomfort):
```
AdaptationResult:
  ScheduleAction = Skip
  SafetyFlag = PainOrDiscomfort
```
UI: *"Bu antrenmanı yeniden planlamıyoruz. Devam etmeden önce nasıl
hissettiğini değerlendir."* — süre, protokol, hangi egzersiz gibi
bilgi verilmez.

**Retroactivity kuralı (YENİ — rev3, DECIDED):**
```
SafetyFlag affects future adaptation decisions, not retroactive
schedule repairs already committed.
```
Yani daha önce commit edilmiş bir `RescheduleToEmptySlot`/
`SubstituteFutureEasy`, sonradan gelen bir safety event yüzünden
geri alınmaz veya yeniden optimize edilmez.

### §4.1 Runtime Reason Vocabulary Mapping (YENİ — rev3.1, DECIDED)

4M.1 sonrası audit'te ortaya çıktı: canlı `NotToday` endpoint'inin
gerçek reason vocabulary'si (`fatigue / soreness / illness / schedule /
weather / other`) canonical `NotTodayReasonCode` (§2, §4) ile birebir
örtüşmüyor — ve daha önemlisi, canlıda **`pain_or_discomfort`
karşılığı hiç yok.** Bu netleşmeden 4M.3'e gidilseydi, `SafetyReviewRequired`
pathway'i production'da fiilen ulaşılamaz kalırdı.

```
Live runtime reason mapping for Adaptation V1:

schedule  → schedule_conflict → Operational
weather   → weather           → Operational
illness   → illness           → Operational, blocks repair
fatigue   → tired             → Operational
other     → other             → Operational

soreness  → Safety
           → blocks schedule repair
           → sets HasSafetyFlag
           → SafetyReviewRequired = true
```

**Rationale:** `soreness`, `pain_or_discomfort` ile semantik olarak
yeniden adlandırılmıyor / eşitlenmiyor — bu ayrı bir runtime source
reason olarak kalır, yalnızca conservative Safety pathway'ine
yönlendirilir. Gerekçe, önceki risk-asimetrisi tartışmasıyla aynı:
olası bir ağrı/sakatlık sinyalini yanlışlıkla Operational'a düşürmenin
maliyeti (sistemin sakat olabilecek birini reschedule ile koşmaya
itmesi), normal kas hassasiyetini yanlışlıkla Safety'e almanın
maliyetinden (gereksiz bir review nudge'ı) daha yüksektir. Sistem
`soreness`'tan hiçbir recovery duration, diagnosis, veya
return-to-running prescription çıkarmaz — bu V1 non-goal sınırının
(§11) dışına çıkmaz.

`travel` ve `personal` için şu an canlıda karşılık yok — bu **blocker
değil**, gelecekteki bir reason-vocabulary genişlemesi olarak
BACKLOG'da kalır (§12).

**Mimari sınır (DECIDED):** `ReasonClassificationPolicy` yalnızca
canonical `NotTodayReasonCode` üzerinden çalışmaya devam eder —
4M.3'te doğrudan `soreness == pain_or_discomfort` gibi bir token-alias
YAPILMAZ. Bunun yerine ayrı, ince bir mapping boundary eklenir:

```
RuntimeNotTodayReasonMapper
        ↓
AdaptationReasonMeaning     (canonical anlam — henüz mecburen
                              pain_or_discomfort'a eşit değil)
        ↓
ReasonClassificationPolicy
```

Bu, canonical domain sözlüğünü (§2) canlı endpoint'in kendi
vocabulary'sinden ayrı tutar — runtime tarafında yeni bir reason
token'ı eklenir/değişirse yalnızca mapper güncellenir, domain
politikası dokunulmaz kalır.

---

## 5. Session Provenance ve Planning Status (DECIDED)

Bir NotToday session'ın kendisi **asla mutasyona uğramaz**.
Reschedule/substitution yeni bir session yaratır ve eskisiyle
ilişkilendirilir.

```
Örnek — RescheduleToEmptySlot:

Session A: Wed, KEY, ExecutionOutcome = NotToday   (immutable, kalır)
Session B: Fri, KEY, ExecutionOutcome = Planned
           AdaptedFromSessionId = A
           AdaptationReason = MissedKeySession

Örnek — SubstituteFutureEasy:

Session A: Wed, KEY, ExecutionOutcome = NotToday
Session C: Fri, EASY, SessionPlanningStatus = Superseded
Session B: Fri, KEY, ExecutionOutcome = Planned
           AdaptedFromSessionId = A
```

İki ayrı kavram birbirine karıştırılmaz:

```
ExecutionOutcome        (kullanıcı ne yaptı?)
  Planned
  Completed
  NotToday

SessionPlanningStatus   (session hâlâ planda aktif mi?)
  Active
  Superseded
```

`Superseded` bir `NotToday` değildir — kullanıcı o seansı kaçırmadı,
sistem onu adaptation nedeniyle kaldırdı.

**Logical session lineage kuralı (YENİ — rev3, DECIDED):**
```
A replacement session and its AdaptedFrom lineage represent ONE
logical expected session for adherence evaluation.
```
Bu, `WindowExecutionSummary` hesaplayan geliştiricinin
`sessions.Count(x => x.Completed)` gibi naif bir sorgu yazıp A+B'yi
iki ayrı seans olarak saymasını önlemek için açıkça yazılmalı —
`AdaptedFromSessionId` bunu dolaylı sağlasa da, kural olarak
dokümante edilmemiş bir varsayıma bırakılmamalı.

---

## 6. WindowExecutionSummary (rev3.1 — schema + denominator düzeltmesi)

**Rev2'deki hata:** Raw event'lere bakan bir summary, başarıyla
repair edilmiş bir KEY'i hâlâ "missed" sayabiliyordu. Rev3'te iki ek
düzeltme var: (1) genel bir `EffectiveCompletedCount` eksikti, bu da
`Reduce` kararının yanlış tetiklenmesine yol açıyordu (§7); (2)
`ExpectedSessionCount`'un ne saydığı belirsizdi.

**Rev3.1'deki düzeltme:** Rev3'ün `ExpectedSessionCount` tanımı kendi
içinde çelişkiliydi (bkz. yukarıdaki Rev3.1 notu). Doğru semantics
aşağıda — Superseded session **paydadan silinmez**, yalnızca kendi
nötr kategorisinde kalır.

```
Raw Event            Schedule Repair              Final Window Evidence
KEY NotToday    →    KEY successfully recovered →  KEY completed
```

Güncel schema:

```
WindowExecutionSummary
  ExpectedSessionCount        ← orijinal logical planning expectation
                                 (aşağıya bakınız)
  EffectiveCompletedCount

  KeySessionExpected        : bool
  KeySessionCompleted       : bool   ← repair sonrası final durum

  LongRunExpected           : bool
  LongRunCompleted          : bool   ← repair sonrası final durum

  EasyExpectedCount
  EasyCompletedCount

  UnrecoveredNotTodayCount     ← rev3: NotTodayCount'tan yeniden adlandırıldı
  SupersededByAdaptationCount  ← informational only

  HasSafetyFlag
```

**`ExpectedSessionCount` canonical tanımı (rev3.1, DECIDED):**
```
ExpectedSessionCount = original logical planning expectation
(one count per logical session role — bkz. §5 lineage kuralı).

Superseded session:
  - actionable değildir
  - Completed değildir
  - NotToday değildir
  - negative adherence signal değildir
  - FAKAT original expectation denominator'dan SİLİNMEZ

Replacement session:
  - yeni bir expected session yaratmaz
  - AdaptedFrom lineage üzerinden original expected role'ü karşılar

Şu OLMAZ:  COUNT(all session rows in DB)
Şu OLMAZ:  Superseded session'ları ExpectedSessionCount'tan çıkarmak
           (Rev3'teki hatalı okuma — bkz. Rev3.1 notu)
```

**Kilit örnek (Rev3.1, lock edilecek):**
```
Mon EASY completed
Wed KEY  NotToday
Fri EASY Superseded by adaptation
Fri KEY  replacement completed   (AdaptedFrom = Wed KEY)
Sun LONG completed
```
Beklenen sonuç:
```
ExpectedSessionCount        = 4
EffectiveCompletedCount     = 3

KeySessionExpected          = true
KeySessionCompleted         = true

LongRunExpected             = true
LongRunCompleted            = true

EasyExpectedCount            = 2
EasyCompletedCount           = 1

SupersededByAdaptationCount  = 1
UnrecoveredNotTodayCount     = 0
```

**`UnrecoveredNotTodayCount` (rev3, isim düzeltmesi):**
Eski `NotTodayCount` ismi yanıltıcıydı — raw DB'de her zaman
`NotToday` satırı kalır (immutable, §5), repair başarılı olsa bile.
Yeni isim yalnızca **kurtarılamamış** (hiçbir replacement/substitute
üretmemiş) kayıpları sayar. Ham/denetim amaçlı sayım gerekirse ayrı
bir audit sorgusundan (`AdaptationDecisionRecord` üzerinden) alınır.

**`SupersededByAdaptationCount` — informational only (rev3, DECIDED):**
```
SupersededByAdaptationCount is informational only.
It is NOT a negative adherence signal and MUST NOT influence
NextWindowLoadDecision.
```
Bir Easy, KEY'i kurtarmak için superseded olduysa bu kullanıcının
adherence'ı değildir — policy bunu cezalandırmaz.

`KeySessionCompleted` / `LongRunCompleted`, session hangi tarihte
tamamlandıysa (orijinal veya lineage ile bağlı yeni session) `true`
olur.

---

## 7. NextWindowLoadDecision + SafetyReviewRequired (rev3 — model düzeltmesi)

**Rev2'deki model hatası:** `SafetyReviewRequired`, load-decision
enum'una (`ProgressAsPlanned/Maintain/Reduce`) karışmıştı. Bunlar
farklı boyutlar — ilk üçü "yük ne olacak", sonuncusu "kullanıcı
gözden geçirmeli mi". Rev3'te ayrıldı:

```
NextWindowLoadDecision {
  ProgressAsPlanned,
  Maintain,
  Reduce
}

SafetyReviewRequired : bool   // ayrı, load decision'dan bağımsız
```

Örnek çıktı: `LoadDecision = Maintain, SafetyReviewRequired = true`
— yani "pain varsa yük ne olacak" sorusu artık örtük değil, açık.

`CONTINUE/HOLD/REDUCE` yerine bu isimler kullanılıyor — "current
state'i mi yoksa catalog progression'ı mı devam ettiriyoruz"
belirsizliği kod seviyesinde ortadan kalksın diye.

### Karar mantığı (rev3 — severity-first, PRODUCT DEFAULT)

**Rev2'deki bug:** Role-first sıralama (`LongRunCompleted == false?
→ Maintain` önce kontrol ediliyordu) tutarsız sonuç üretiyordu:
0/4 tam katılımsızlık `Maintain` alırken, Key+Long tamamlanmış ama
iki Easy kaçmış 2/4 durumu `Reduce` alabiliyordu — daha kötü
adherence daha iyi sonuç veriyordu.

**Rev3 düzeltmesi — önce genel severity, sonra role importance:**

```
HasSafetyFlag?
  YES → SafetyReviewRequired = true   (LoadDecision'ı EZMEZ, ayrı boyut)

EffectiveCompletedCount:

  0–1 completed
    → LoadDecision = Reduce

  2 completed
    → LoadDecision = Maintain

  3 completed
    if the only missing role is Easy
      → LoadDecision = ProgressAsPlanned
    else (Key veya Long eksik)
      → LoadDecision = Maintain

  4 completed
    → LoadDecision = ProgressAsPlanned
```

"Completed" = repair sonrası final effective execution (§6);
başarıyla kurtarılmış bir KEY/LONG tamamlanmış sayılır.

> **(rev5 ile ÇÖZÜLDÜ — bkz. §7a aşağıda)** Bu bölümün orijinal
> `0–1/2/3/4` eşikleri, tek bir gerçek yapısal haftanın (1 Key + 2
> Easy + 1 Long = 4 seans) kararı için geçerlidir — 4M.5B audit'i bunu
> kanıtladı: "yalnızca Easy eksik → Progress" dalı zaten rol-yapısal,
> yüzde tabanlı değil, ve bu yalnızca 4=1K+2E+1L olduğunda anlamlı.
> `4M.5A`'nın bulduğu 16-session multi-week window sorunu (576 gerçek
> window'un %83.3'ü) rev5'te **B-weekly-summary + B1 aggregation**
> modeliyle çözüldü — bu bölümdeki formül artık **hafta-seviyesinde**
> değişmeden uygulanıyor, aggregation ayrı bir katman (§7a).

**Hâlâ açık bırakılan sınır durum (BACKLOG):** 2/4'ün *hangi*
kombinasyonu olduğu (Key+Long tamam, iki Easy kaçmış vs. başka
kombinasyon) şu an tek bir `Maintain` sonucuna gidiyor — daha ince
ayrım ilk kullanıcı verisiyle netleştirilecek. (Bu, tek bir gerçek
haftanın içindeki 2/4 durumu için hâlâ geçerli — rev5'in weekly
aggregation'ı bu belirsizliği değiştirmedi, yalnızca hangi window'a
uygulanacağını netleştirdi.)

---

### §7a Multi-Week Window Aggregation — B-weekly-summary + B1 (rev5, DECIDED)

**4M.5A'nın bulduğu blocker:** Gerçek rolling activation window'ların
%83.3'ü (576 örneklemin) **16-session** — yani 4 gerçek yapısal
haftayı tek seferde materialize ediyor (GE davranışı). §7'nin
`0–1/2/3/4` formülü tek bir yapısal haftanın (1K+2E+1L=4) kararı için
doğru; doğrudan 16-session'a uygulanınca role-awareness'ı kaybediyor
(bkz. yukarıdaki not).

**4M.5B'nin kanıtladığı zemin (DECIDED):**
```
Yapısal haftalar zaten EXPLICIT_AND_PERSISTED:
  GlobalWeek, StructuralStartDate/EndDate, deterministic 7-day offset.
  Her yapısal hafta (recovery haftaları dahil, 5 stage family'de de)
  tam olarak 1 KEY + 2 EASY + 1 LONG.

WindowExecutionSummaryBuilder'ın bugünkü AND-reduction'ı (KeySessionCompleted/
LongRunCompleted'ın birden fazla occurrence'da true olması için hepsinin
tamamlanmış olmasını istemesi) kasıtlı bir multi-occurrence kuralı değil
— tek-occurrence şemasının kazara genellemesi. Bu, orijinal matrisin
gerçek biriminin tek yapısal hafta olduğunun kanıtı.
```

**Seçilen model (DECIDED): B-weekly-summary + B1 (worst-week-wins)**

`B-weekly-checkpoint` (her haftada ayrı bir activation/checkpoint)
**seçilmedi** — window identity, idempotency, chronology ve
Block-semantics'i (4M.4–4M.4B.2C'de frozen) yeniden açardı.
`B-weekly-summary` bunların hiçbirine dokunmuyor: tek-activation
mimarisi korunuyor, yalnızca **evidence şekli** değişiyor.

```
Bir activation window'un içindeki her gerçek yapısal hafta W için:

WeeklyExecutionSummary(W)
  = WindowExecutionSummary (§6) ile AYNI şema ve AYNI lineage/
    Superseded/denominator mantığı, yalnızca W'nin
    StructuralStartDate/EndDate aralığına scope edilmiş.

WeeklyLoadDecision(W)
  = NextWindowLoadDecisionPolicy (§7, yukarıdaki formül, DEĞİŞMEDEN)
    WeeklyExecutionSummary(W)'ye uygulanır.
    (Her hafta tam olarak 1K+2E+1L olduğundan, KeySessionCompleted/
    LongRunCompleted boolean'ları artık YENİDEN DOĞRU — tek-occurrence
    varsayımı bu granülaritede zaten geçerli.)

Final window-level karar (B1 — worst-week-wins):

  NextWindowLoadDecision =
    min( WeeklyLoadDecision(W) for W in window'daki gerçek haftalar )
    sıralama: Reduce < Maintain < ProgressAsPlanned

  SafetyReviewRequired =
    OR( WeeklySafetyReviewRequired(W) for W in window'daki gerçek haftalar )
    (mevcut window-level HasSafetyFlag OR-agregasyonuyla matematiksel
    olarak özdeş — yeni bir karar değil)
```

**Neden B1 (yeni sabit icat etmiyor):** `Reduce < Maintain <
ProgressAsPlanned` sıralaması zaten §7'nin severity-ordering
invariant'ında frozen (rev4.1). "Worst-week-wins" bu sıralamayı
aggregation'a uygulamaktan başka bir şey değil — B2 varyantlarının
(most-recent/majority/recency-weighted) her biri yeni bir PRODUCT
DEFAULT (ağırlıklandırma formülü, tie-break kuralı) gerektirirken, B1
gerektirmiyor.

**Recency-blindness kasıtlı, kusur değil (DECIDED, rationale):** B1,
front-loaded ve back-loaded kötü haftaları aynı şekilde ele alır
(hangi hafta kötüydü fark etmez). Bu kabul edilebilir çünkü karar,
4 haftanın **tamamı bittikten sonra**, checkpoint anında bir kerede
veriliyor — o anda hiçbir haftanın diğerinden daha "güncel" olması
gelecek pencereyi farklı etkilemez, çünkü hepsi zaten geçmiş.

**Numeric anchor mimarisi ETKİLENMEDİ (DECIDED):**
`NextWindowNumericAnchorSelector`'ın Maintain/Reduce formülü (§7,
yukarıda, frozen) hâlâ **window başına bir kez**, aggregate edilmiş
final `NextWindowLoadDecision` üzerinden çalışır —
`ValidatedSustainableLoad(window)` ve `PriorValidatedCheckpointLoad`
window-level kalır, hafta başına bölünmez. Yalnızca *hangi
LoadDecision'ın* bu formüle girdiği değişti (artık aggregate), formül
kendisi değişmedi.

**Mixed-phase window'lar (4M.5B'nin doğruladığı gerçek senaryo, artık
otomatik çözülüyor):** Bir window'un 3 farklı phase'e yayılması
mümkün (gerçek HTTP veriyle doğrulandı). `WeeklyExecutionSummary`
artık her haftayı kendi tarih aralığına scope ettiğinden, her hafta
zaten kendi doğru phase context'inde değerlendiriliyor — window-level
phase-karışıklığı sorunu B-weekly-summary ile kendiliğinden ortadan
kalkıyor, ayrı bir kural gerekmedi.

#### Weekly Lineage Attribution Rule (rev5, DECIDED)

Schedule repair candidate'ları window-scoped (week-scoped değil) —
yani bir hafta içinde kaçırılan KEY/LONG, `PhaseBoundaryConstraint`
ihlal edilmediği sürece **başka bir yapısal haftaya** reschedule/
substitute edilebilir (§3). Bu, "bir session'ın evidence'ı hangi
haftaya yazılır" sorusunu doğurdu.

```
Bir haftanın "expected role'leri" = o haftanın tarih aralığındaki
  ORİJİNAL (Superseded/replacement olmayan) session'ların rolleri.
  Bu, adaptation'dan BAĞIMSIZ, SABİT kalır.

Bir haftanın rolünün "completed" durumu = o haftanın orijinal
  session'ının lineage zincirinin (§5) final effective state'i
  (§6, DEĞİŞMEDEN) — replacement session'ın FİZİKSEL tarihi hangi
  haftaya düşerse düşsün, evidence her zaman orijinal haftaya yazılır.

Replacement session'ın fiziksel tarihi HİÇBİR ZAMAN başka bir
  haftanın evidence'ına yeni bir şey EKLEMEZ.
```

**Neden orijinal hafta (destination değil):** Somut karşı-örnek:
Week 1'deki kaçırılan bir KEY, `SubstituteFutureEasy` ile Week 2'nin
bir EASY slotunu alırsa — destination-hafta attribution seçilseydi,
Week 2'nin takviminde 5 aktif session görünürdü (kendi KEY'i + Week
1'den gelen KEY), bu da Model B'yi seçmemizin asıl sebebi olan "her
hafta tam olarak 1K+2E+1L" temiz şablonunu bozardı. Orijinal-hafta
attribution ile Week 2'nin kendi Easy'si `Superseded` (nötr, §6'daki
gibi) görünür, Week 1'in KEY'i `Completed` sayılır — mevcut frozen
lineage mantığının birebir uzantısı, yeni kavram gerektirmiyor.

**Şema etkisi:** `WindowExecutionSummary`'nin kendisi (§6) DEĞİŞMEDİ
— yalnızca artık hem window-level (numeric anchor için, DEĞİŞMEDEN)
hem week-level (LoadDecision aggregation için, YENİ) olmak üzere iki
farklı granülaritede instantiate ediliyor. `WeeklyExecutionSummary`
ayrı bir tip değil, aynı builder'ın farklı bir scope'a uygulanmış
hali.

---

### Maintain / Reduce Numeric Anchor Semantics (rev4 — DECIDED, `ReduceBand` çözüldü)

**4M.4B.1 audit'inde bulunan mimari:** Composition katmanı zaten iki
anchor kaynağı taşıyor —

```
ValidatedSustainableLoad(window)   = bu pencerede gerçekten tamamlanan
                                      mesafelerin ortalaması (evidence anchor)
PriorValidatedCheckpointLoad       = önceki checkpoint'ten taşınan,
                                      zaten kabul edilmiş anchor
                                      ("PriorAnchor(state)" helper — şu ana
                                      kadar yalnızca retry-continuation
                                      için kullanılıyordu)
CatalogProgressionStep(anchor)     = normal ileri-ilerleme fonksiyonu
                                      (mevcut/varsayılan davranış)
```

Bu ikisi üzerinden üç `LoadDecision`'ın anchor semantiği:

```
ProgressAsPlanned (mevcut/varsayılan davranış, değişmedi):
  NextWindowTargetAnchor = CatalogProgressionStep(ValidatedSustainableLoad(window))

Maintain (DECIDED, 4M.4B.1):
  NextWindowTargetAnchor = PriorValidatedCheckpointLoad
  (ilerleme adımı uygulanmaz; son kabul edilmiş anchor'da donar —
  PriorAnchor(state) helper'ı yeniden kullanır, yeni kod yok)

Reduce (DECIDED, rev4 — Model 2, "Evidence-Anchored"):
  EffectiveCompletedCount > 0 ise:
    NextWindowTargetAnchor = min(ValidatedSustainableLoad(window), PriorValidatedCheckpointLoad)
  EffectiveCompletedCount == 0 ise (tamamlanan mesafe verisi hiç yok):
    NextWindowTargetAnchor = PriorValidatedCheckpointLoad
    (min(tanımsız, X) = X — formülün kendi doğal çöküşü; bu, yeni bir
    "taban yüzdesi" icat etmez, Reduce bu durumda Maintain'e eşitlenir)
```

**Neden Model 2 (yüzde kesme değil):** Klasik "deload week" literatürü
(%10-40 hacim azaltma, en sık atıfta bulunulan nokta %20-30) **tutarlı
şekilde antrenman yapan** birinin birikmiş yorgunluğu için tasarlanmış
bir senaryoyu varsayıyor. Appsel'in `Reduce`'u ise **düşük adherence'a**
(0-1/4 tamamlanmış) tepki — yani kişi zaten neredeyse hiç koşmadı.
Ona catalog-progression hedefinin keyfi bir yüzdesini vermek yerine,
zaten var olan `ValidatedSustainableLoad` mekanizmasını (gerçekten
gösterilen kapasite) doğrudan anchor olarak kullanmak hem daha az
mühendislik hem daha savunulabilir bir prensip: **"Reduce = ilerleme
uygulama, yalnızca gösterilen kapasiteyi yansıt."**

**Severity-ordering invariant (rev4.1 — güncellendi, ROUNDING PRODUCT DEFAULT ile netleştirildi):**

```
NextWindowTargetAnchor(Reduce) ≤ NextWindowTargetAnchor(Maintain)
```

`min()` operatörü sayesinde bu sayısal olarak garanti — Reduce hiçbir
zaman Maintain'i sayıca geçemez (test: 200 randomize case, 0 ihlal).

```
NextWindowTargetAnchor(Maintain) ≤ NextWindowTargetAnchor(ProgressAsPlanned)
```

tarafı ise **kesin (strict) olarak doğru değildir** — 4M.4B.2B'nin
gerçek-catalog sweep'i (200 case, `LongHorizonGeNumericExecutor` ile
gerçek session-distance allocation/rounding üzerinden, reimplementasyon
değil) 183 geçerli case'in 94'ünde (%51) `Maintain > ProgressAsPlanned`
bulmuştur. Sapma her zaman küçük: **maks. mutlak 0.247km, maks. relatif
%1.36**, ve `CatalogProgressionStep`'in kendi session-distance rounding
davranışından kaynaklanır — `Maintain`'e hiçbir progression adımı
uygulanmadığı (yukarıdaki formül değişmedi) ve adaptation tarafında
hiçbir uplift eklenmediği doğrulanmıştır.

#### ROUNDING PRODUCT DEFAULT (rev4.1, DECIDED — 4M.5A forensic audit ile FREEZE)

```
V1 kabul kriteri:

Maintain, ProgressAsPlanned'i MATERYAL olarak aşmamalıdır.

"Materyal" = relative deviation > %1.5

Rounding-only sapma (<= %1.5) kabul edilebilir PRODUCT DEFAULT'tur.
Bilimsel bir eşik iddiası DEĞİLDİR — 4M.4B.2C'nin gerçek-catalog
sweep sonucuna dayanan, kalibre edilecek bir başlangıç değeridir.
```

**4M.5A forensic audit ile bu artık yalnızca "üst sınırın biraz
üstü" değil, tam istatistiksel karakterizasyonla desteklenmiş
(DECIDED, freeze):**

```
183 case, 94 strict-order violation

pre-materialization violation:  0
post-materialization violation: 94
rounding/allocation kaynaklı:   94/94  (%100)
unexplained:                     0/94  (%0)

median relative deviation: %0.391
p99 relative deviation:    %1.361
max relative deviation:    %1.361   (%1.5 tavanının belirgin altında)
```

Yani invariant **anchor seviyesinde hiç bozulmuyor** — sapma yalnızca
deterministic catalog materialization/rounding sonrasında oluşuyor,
kaynağı tamamen izole edilmiş ve açıklanmamış hiçbir vaka yok.

**Ne YAPILMADI (rev4.1, açıkça listelenmiştir):**

```
- Maintain aşağı clamp edilmedi
- ProgressAsPlanned yukarı inflate edilmedi
- session-distance rounding değiştirilmedi
- catalog progression değiştirilmedi
- runtime'a epsilon/tolerance sabiti eklenmedi (yalnızca test/governance
  kabul katmanında, bkz. 4M.4B.2C testi)
```

`ReduceBand` artık **BACKLOG'dan çıkarıldı** — Model 2 hiçbir yeni
sabit/yüzde gerektirmiyor, formülün kendisi yeterli. `Reduce ≠
RecoveryWeek` ayrımı hâlâ geçerli: bu mekanizma yeni bir workout
structure'ı (kısa/yumuşatılmış hafta template'i) üretmiyor, yalnızca
numeric anchor'ı sınırlıyor.

**First-checkpoint / no-evidence davranışı (4M.5A ile FREEZE — DECIDED):**
`PriorValidatedCheckpointLoad` hiç yoksa (planın ilk checkpoint'i) VE
aynı anda `EffectiveCompletedCount == 0` ise (hiç tamamlanan mesafe
verisi de yok) — yani formülün iki fallback kaynağı da aynı anda boşsa
— sistem **yeni bir sayısal fallback icat etmez.** Mevcut typed
conflict/block mekanizması zaten bu durumu güvenle karşılıyor (test:
`FirstCheckpoint_ZeroCompletion_NoPriorNoEvidence_BlocksWithExistingTypedConflict_NoNumericFallback`,
gerçek HTTP+DB, 4M.4B.2/4M.5A ile doğrulandı). Bu satır ilk sorulduğunda
(4M.4B.2 review'de) açık bir soruydu — artık kapalı.

#### TARGET PRESCRIPTION INFEASIBILITY (YENİ — rev4.1, DECIDED)

4M.4B.2B audit'i, `CoreJitContextUnavailable` block'unun **Maintain'e
özgü bir plumbing hatası olmadığını** kanıtladı (A/B reprodüksiyon:
eşit derecede küçük bir Reduce-selected anchor da hedef Core haftasında
aynı gerçek mekanizmayla aynı şekilde reddediliyor). Gerçek neden:
seçilen (Maintain veya Reduce) anchor, hedef Core/Runway haftasının
gerçek catalog minimum prescription'ını
(`FourDaySessionDistanceAllocationPolicy` — residual volume, long-run
payı çıkarıldıktan sonra KEY/EASY minimumlarını karşılayamıyor)
sağlayamayacak kadar küçük olabilir.

```
V1 kanonik davranışı:

selected Maintain/Reduce numeric anchor
hedef phase/week'in catalog minimum prescription'ını karşılayamıyorsa:

→ anchor YUKARI ARTIRILMAZ
→ catalog minimum'a clamp edilmez
→ catalog minimumları zayıflatılmaz
→ daha hafif sentetik workout structure'ı üretilmez
→ RecoveryWeek yaratılmaz
→ workout içeriği yeniden yazılmaz
→ başka bir phase/week'e atlanmaz
→ sessizce ProgressAsPlanned'e fallback yapılmaz

Bunun yerine: mevcut typed continuation block korunur —

LONG_HORIZON_CONTINUATION_BLOCKED

mevcut sanitized public davranışla (§10, mevcut UI dili).

Selected anchor otoritesini korur. Catalog, o anchor'dan geçerli bir
target week materialize edilip edilemeyeceği konusunda otoritesini
korur. Geçerli bir prescription yoksa: activation gerçekleşmez.
```

**Mimari invariant (rev4.1, DECIDED):**

```
Catalog = progression / workout prescription authority
Adaptation = numeric anchor constraint authority

Doğru akış:
  adaptation izin verilen anchor'ı seçer
          ↓
  catalog phase-uygun materialization dener
          ↓
  feasible ise: activate
  değilse: typed continuation block

YANLIŞ akış (V1'de YASAK):
  adaptation düşük anchor seçer
          ↓
  catalog minimum çok yüksek
          ↓
  sistem sessizce anchor'ı yükseltir
```

Böyle bir yukarı-clamp, adaptation kararını tersine çevirir ve V1'de
yasaktır.

**4M.4B.2C implementasyon audit sonucu (DECIDED):** Mevcut runtime
davranışı bu kuralla **zaten tam uyumludur** — hiçbir production kodu
bu revizyonla değişmedi:

```
FourDaySessionDistanceAllocationPolicy
  → infeasible allocation'ı reddeder (CatalogSessionPrescriptionInfeasibleException)
  → JIT composition üzerinden propagate olur
  → persistence Block döner
  → 4M.4B.2A IsBlock sinyali (LongHorizonRollingPersistenceResult.IsBlock)
  → outer activation typed 409 döner (LONG_HORIZON_CONTINUATION_BLOCKED)
```

Gerçek HTTP entegrasyon testleriyle doğrulandı: feasible Maintain
başarıyla activate olur ve chronology genuine olarak ilerler; feasible
Reduce başarıyla activate olur ve chronology genuine olarak ilerler;
infeasible Maintain ve infeasible Reduce ikisi de typed block döner,
window asla false-advance etmez (bkz. Phase 4M.4B.2C doc).

**Multi-window acceptance (rev4.1, DECIDED):** Her keyfi
`Reduce → Maintain → ProgressAsPlanned` zincirinin başarıyla activate
olması **gerekmez**. Rev4.1 sonrası doğru invariant:

```
Her transition için:
  seçilen anchor catalog-feasible ise → doğru sonraki window activate olur
  seçilen anchor catalog-infeasible ise → typed Block, chronology
    false-advance etmez
```

**Numeric translation implementasyon durumu (netleştirme, DECIDED):**
Yukarıdaki formül `Maintain`/`Reduce` için anchor semantiğini tam
olarak karara bağlar — bu artık açık bir **ürün** kararı değildir.
Geriye kalan yalnızca **runtime implementasyonu**dur (4M.4B.2 kapsamı):
`LoadDecision`, composition çağrısına (`ComposeAndActivateNextWindowAsync`
girdisi) bu üç dalı seçecek şekilde bağlanmalı. Bu implementasyon işi
§12'deki BACKLOG tablosunda artık ayrı bir satır olarak **listelenmez**
— yalnızca bir faz-uygulama görevidir, açık ürün kararı değildir.

**Önemli — durum netleştirmesi:** `LoadDecision`/`SafetyReviewRequired`
bugün itibarıyla persisted edilmiş bir historical checkpoint-decision
kaydı **değildir**. Her checkpoint'te persisted source-window state'ten
(§6, §9) yeniden hesaplanır (`WindowExecutionSummaryBuilder` →
`NextWindowLoadDecisionPolicy`); ayrı bir durable decision-snapshot
tablosu yoktur. Bu, `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT`
backlog kaleminin konusudur (bkz. §12) ve bu revizyonda değişmemiştir
— yukarıdaki formülün DECIDED olması, kararın nasıl hesaplandığını
karara bağlar, kararın nasıl/ne zaman persisted edileceğini değil.

### SafetyReviewRequired → next window'a ne yapıyor? (rev3, DECIDED)
```
SafetyReviewRequired = true
  → LoadDecision hesaplanmaya devam eder (genelde Maintain çıkar,
    çünkü pain/discomfort'a yol açan seans zaten Skip edilmiştir)
  → adaptation engine ekstra bir numeric ayar YAPMAZ
  → mevcut activation akışı SafetyReviewRequired flag'ini UI'a taşır,
    engine kendi bir "acknowledgement" UX'i üretmez
```

---

## 8. Realignment (DECIDED — özellik olarak alınmadı)

Runna'nın "3+ missed → whole-plan realignment" özelliği **referans
alınmadı**. Appsel zaten rolling window kullandığından, eşdeğer
davranış `NextWindowLoadDecision` (Maintain/Reduce) ile zaten
karşılanıyor; ayrı bir "realignment" feature'ı eklenmiyor.

---

## 9. AdaptationDecisionRecord (DECIDED — audit schema)

```
AdaptationDecisionRecord
  PlanId
  SourceWindowId
  TriggerSessionId          (kaçırılan orijinal session)
  DecisionType               (Skip / RescheduleToEmptySlot /
                               SubstituteFutureEasy / ProgressAsPlanned /
                               Maintain / Reduce)
  SafetyReviewRequired        : bool     ← rev3: ayrı alan, DecisionType'tan çıkarıldı
  ReasonCode
  ReplacementSessionId?      (yeni oluşturulan session, varsa)
  SupersededSessionId?       (kaldırılan EASY, varsa)
  CreatedAt
```

Tam diff-entity (`AdaptationSessionChange` child table) V1 için şart
değil — yukarıdaki opsiyonel alanlar yeterli provenance sağlıyor.
Gerekirse V2'de genişletilir (**BACKLOG**).

Not: bu şema, window-seviyesi `NextWindowLoadDecision`/
`SafetyReviewRequired` çıktısının kendisi için ayrı bir historical
snapshot kaydı **değildir** — bkz. §7'nin sonundaki netleştirme ve
§12'deki `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` kalemi.

---

## 10. Engine vs UI Dili (rev3 — wording düzeltmesi)

| Engine | UI |
|---|---|
| Skip | "Bu koşuyu atlayalım." |
| RescheduleToEmptySlot | "Bu koşuyu [gün]'e taşıdık." |
| SubstituteFutureEasy | "Bu antrenmanı [gün]'e taşıdık; o günkü kolay koşuyu kaldırdık." *(rev3: eski "yaptık" ifadesi yanlıştı — henüz gerçekleşmedi, planlandı)* |
| ProgressAsPlanned | "Planına normal şekilde devam ediyoruz." |
| Maintain | "Bir sonraki bölümde yükü artırmıyoruz." |
| Reduce | "Bir sonraki bölümü biraz daha hafif tutuyoruz." |
| SafetyReviewRequired (ayrı, üstteki üçle birlikte gösterilebilir) | "Devam etmeden önce nasıl hissettiğini değerlendir." |
| (rev4.1) Target prescription infeasibility (mevcut typed block, yeni bir UI metni değil) | Mevcut "plan devam edemiyor / yeniden değerlendirme gerekiyor" block dili — bkz. mevcut `LONG_HORIZON_CONTINUATION_BLOCKED` sanitized public davranışı |

`HOLD`, `deload`, `mesocycle`, `LoadDecision` gibi terimler
kullanıcıya gösterilmez.

---

## 11. V1 Non-Goals (FROZEN)

- injury diagnosis
- return-to-running prescription
- HRV/sleep entegrasyonu
- missed_score / exponential decay
- ML tabanlı karar
- makeup workout
- cascade rescheduling
- same-week load optimization
- automatic race-date change
- automatic level change
- automatic taper redesign
- long-run %70/%80 synthetic replacement
- whole-plan regeneration
- Runna-tipi ayrı "Plan Realignment" feature'ı
- KEY ↔ LONG slot substitution (yalnız KEY/LONG → EASY yönü var)
- birden fazla session'ın aynı anda cascade substitution'a girmesi
- batch/otomatik missed-session inference (yalnızca explicit NotToday tetikler) — YENİ (rev3)
- SafetyFlag'in daha önce commit edilmiş schedule repair'leri retroaktif iptal etmesi — YENİ (rev3)
- (rev4.1) infeasible bir target week için anchor'ın yukarı clamp edilmesi
- (rev4.1) infeasible bir target week için sentetik/hafifletilmiş workout structure üretilmesi

---

## 12. Açık Kalan Kararlar (BACKLOG)

| Konu | Neden erteleniyor |
|---|---|
| ~~`NextWindowLoadDecision` eşiklerinin gerçek window boyutuna göre kalibrasyonu~~ **ÇÖZÜLDÜ (rev5)** | 4M.5A/4M.5B ile B-weekly-summary + B1 modeli DECIDED oldu (§7a). Artık BACKLOG değil. |
| Next-window 2/4 sınır durumunun ince ayrımı | Product default; erken kullanıcı verisiyle kalibre edilecek |
| Maintain baseline'ı — daha sofistike "son başarılı window" politikası | V1 authority zaten DECIDED ve implemented: `PriorValidatedCheckpointLoad` / `PriorAnchor(state)` (bkz. §7) — basit "son kabul edilmiş anchor'da dondur" freeze'i. Bu backlog kalemi yalnızca V1'in ötesinde, daha sofistike bir "gerçek son başarılı window" seçim politikası (örn. ardışık Maintain zincirlerinde hangi anchor'ın referans alınacağının daha ince ayrımı, çoklu-checkpoint geçmişi) içindir — V1 baseline'ının kendisi belirsiz değildir. |
| 21K/42K'ya geçişte catalog-specific farklar | `ScheduleRepairPolicy` ve `NextWindowLoadDecision` sabit kalır, yalnız catalog progression/load semantics distance-specific olur |
| `AdaptationSessionChange` tam diff-entity | V1'de `AdaptationDecisionRecord`'daki opsiyonel alanlar yeterli |
| Concurrency / idempotency key | Implementation sırasında çözülecek |
| Audit record persistence transaction boundary | Implementation sırasında çözülecek |
| `travel` / `personal` için runtime reason token'ı | Şu an canlıda karşılık yok; reason-vocabulary genişlemesi ileride ele alınabilir (§4.1) |
| `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT` | `LoadDecision`/`SafetyReviewRequired` şu an her checkpoint'te persisted source-window state'ten yeniden hesaplanır, durable bir historical decision-snapshot kaydı yoktur (bkz. §7, §9). Gelecekte support/debugging/auditability için ayrı bir kayıt (`PlanId`, `WindowId`, `LoadDecision`, `SafetyReviewRequired`, summary snapshot/version, `CreatedAt`, policy/version provenance) gerekebilir. Şema/migration bu revizyonda eklenmez. |
| (rev4.1) %1.5 rounding tolerance'ın kalibrasyonu | PRODUCT DEFAULT olarak dondu (4M.4B.2C); ilk kullanıcı/production verisiyle yeniden değerlendirilebilir. Şu an yalnızca test/governance kabul katmanında yaşıyor, runtime'a taşınmadı. |

**Rev4 düzeltmesi:** Önceki "Maintain/Reduce'un numeric translation'ı
— Implementation sırasında interface ile çözülecek" satırı bu
tablodan **çıkarıldı**. §7'nin "Maintain / Reduce Numeric Anchor
Semantics" bölümü artık tam formülü DECIDED olarak veriyor — numeric
translation açık bir ürün kararı olmaktan çıktı, yalnızca runtime
implementasyonu (4M.4B.2) kaldı.

**Rev4.1 notu:** `ReduceBand`, percentage reduction, `RecoveryWeek`
ve "numeric translation DecisionRequired" kalemleri bu revizyonda
**yeniden eklenmedi** — Rev4'te resolved/non-goal olarak kalan
durumları korunuyor. Bu revizyon yalnızca iki yeni kalemi kapattı
(rounding tolerance, target-week infeasibility); mevcut resolved
kararları geri açmadı. **Ancak** bu revizyon sırasında 4M.5 review'i
yukarıdaki yeni, kapatılmamış kritik kalemi (window-boyutu kalibrasyonu)
ortaya çıkardı — bu BACKLOG'a değil, önceliklendirilmiş
**DecisionRequired**'a eklendi.

---

## 13. Implementation Notes (rev3 — spec hatası değil, dikkat noktaları)

Bunlar artık ürün kararı değil — Phase 4M.1 kod tabanında (aşağı
bakınız) uygulanmış, testlerle doğrulanmış implementasyon kararları:

1. **`ExpectedSessionCount` DB row count değildir**, logical session
   lineage'a dayanır (§5, §6) — örnek sayılarından bağımsız, canonical
   kural olarak ifade edilir.
2. **`WindowExecutionSummaryBuilder` tek canonical authority'dir.**
   Başka hiçbir servis kendi `Count(Completed)` hesabını yapmaz.
3. **`SafetyReviewRequired` activation-blocking DEĞİLDİR.** Engine bu
   flag için acknowledgement UX üretmez, yalnızca taşır — implement
   eden kişi bunu activation'ı bloklayan bir şey sanmamalı.
4. **Chronological Immutability + concurrency invariantı:**
   `one trigger session → at most one committed AdaptationDecisionRecord
   → at most one active replacement lineage`. Concurrency çözümü
   implementasyon detayı (BACKLOG) ama bu invariant korunmalı.
5. **`Superseded` session'lar actionable değildir** —
   `Complete`/`NotToday` gibi execution-state geçişi yapamaz; bu
   detail/calendar endpoint'lerinde de filtrelenmeli veya
   adapted-history olarak gösterilmeli.
6. **`PhaseBoundaryConstraint` canonical phase identity'ye dayanmalı**,
   distance-specific string isimlere değil — engine 21K/42K'da
   dallanmamalı.
7. **`NextWindowAdaptationResult { LoadDecision, SafetyReviewRequired }`**
   arayüzü sabit; numeric generator (`catalog target + adaptation
   constraint → effective target`) ayrı authority olarak kalır —
   adaptation policy hiçbir yerde km/dakika/yüzde üretmez. (rev4:
   §7'deki `NextWindowTargetAnchor` formülü de bu ayrımı korur —
   `Maintain`/`Reduce` yalnızca hangi *anchor*'ın seçileceğini
   belirler, `CatalogProgressionStep`'in kendisini adaptation
   yeniden yazmaz.)
8. **(rev3.1) TS reference implementasyonundaki denominator varsayımı
   REJECTED.** Phase 4M.1'de hazırlanan TypeScript referans kodu
   (`appsel-adaptation-4M1/`), Superseded session'ları
   `ExpectedSessionCount`'tan çıkaran eski (Rev3) okumayı uyguluyordu.
   Bu artık geçersiz — doğru semantics §6'daki Rev3.1 tanımıdır
   (Superseded paydadan silinmez). Gerçek .NET implementasyonu bu
   TS kodunu **davranış otoritesi olarak değil, yalnızca karar-modeli
   referansı olarak** kullanmalı ve bu spesifik noktada ondan
   sapmalıdır. TS kodu henüz bu düzeltmeyle güncellenmedi.
9. **(rev4) `LoadDecision`/`SafetyReviewRequired` durable persistence
   iddiası kurulmamalı.** Bir implementasyon dokümanı/prompt'u
   "`LoadDecision` persisted edilir" gibi bir ifade kullanmamalı —
   repo re-audit'i yeni bir durable snapshot eklendiğini kanıtlamadıkça.
   Doğru ifade: "`LoadDecision`, canlı activation path'i tarafından
   hesaplanır ve dışa açılır; composition bunu henüz tüketmez; durable
   bir historical decision snapshot'ı yoktur." (bkz. §7 sonu, §9, §12
   `DURABLE_NEXT_WINDOW_ADAPTATION_DECISION_AUDIT`.)
10. **(rev4.1) `Maintain <= ProgressAsPlanned` kesin (strict) bir
    runtime invariant DEĞİLDİR ve hiçbir yerde bu şekilde enforce
    edilmemelidir.** Doğru ifade: "Maintain, ProgressAsPlanned'i
    materyal olarak (>%1.5 relatif) aşmaz; ≤%1.5'lik sapma catalog'un
    kendi session-distance rounding'inden kaynaklanan kabul edilen
    PRODUCT DEFAULT'tur." Bir implementasyon bu ilişkiyi runtime'da
    clamp/kıyaslama olarak kodlamaya kalkışmamalı — yalnızca
    test/governance kabul katmanında yaşar (bkz. §7,
    `MaintainNotExceedingProgressAsPlannedInvariantTests`).
11. **(rev4.1) `CoreJitContextUnavailable`/target-week infeasibility
    bir upward-clamp fırsatı olarak YORUMLANMAMALIDIR.** Bir anchor
    hedef haftanın catalog minimumunu karşılayamıyorsa doğru davranış
    mevcut typed block'tur (§7 TARGET PRESCRIPTION INFEASIBILITY),
    anchor'ı büyütmek veya farklı bir hafta/phase'e sessizce atlamak
    değil.
12. **(rev5 ile ÇÖZÜLDÜ) `NextWindowLoadDecisionPolicy`'nin eşik
    matrisi artık yalnızca gerçek yapısal hafta (`WeeklyExecutionSummary`,
    her zaman 1K+2E+1L=4) seviyesinde invoke edilir** — window-level
    (16-session gibi) doğrudan invoke edilmesi **YANLIŞ**tır. Bir
    implementasyon "severity-first matris çalışıyor" derken, testin
    hangi granülaritede (`WeeklyExecutionSummary` mi
    `WindowExecutionSummary` mi) çalıştığını **açıkça belirtmeli** —
    yalnızca ilki `NextWindowLoadDecisionPolicy`'nin girdisi olarak
    geçerlidir; ikincisi hâlâ `NextWindowNumericAnchorSelector`'ın
    (`ValidatedSustainableLoad`, `PriorValidatedCheckpointLoad`)
    girdisi olarak kullanılır ama `LoadDecision` hesaplamasına asla
    doğrudan sokulmaz (bkz. §7a).
13. **(rev5) B1 aggregation (`min` üzerinden worst-week-wins) ve
    weekly lineage attribution (orijinal hafta) ayrı, birbirinden
    bağımsız iki implementasyon detayıdır** — biri diğerini
    varsaymaz. `WeeklyExecutionSummary` inşa edilirken lineage
    attribution kuralı (orijinal hafta, §7a) uygulanmalı; aggregation
    (B1) bu adımdan SONRA, tamamlanmış `WeeklyLoadDecision` listesi
    üzerinde çalışır.

---

## 14. Phase 4M.1 — Adaptation Domain Contracts and Pure Decision Policies

Spec artık **FROZEN** (§7'deki tek açık DecisionRequired hariç —
bkz. yukarı). İlk implementation dilimi bilinçli olarak dar
tutuldu:

```
Phase 4M.1 — Adaptation Domain Contracts and Pure Decision Policies
  1. enums / value objects
  2. ReasonClassification
  3. CandidateSelectionPolicy
  4. ScheduleRepairPolicy (pure decisions)
  5. WindowExecutionSummary(Builder)
  6. NextWindowLoadDecision (pure policy)
  7. exhaustive unit-test decision matrix

  NO DB mutation · NO API wiring · NO Flutter · NO numeric adaptation
```

Sonraki fazlar: 4M.2 persistence/session replacement, 4M.3 `NotToday`
wiring, 4M.4 next-window activation/numeric constraint, 4M.4B.2/2A/2B/2C
Maintain/Reduce numeric anchor real-runtime confirmation ve policy
kapanışı, 4M.5 end-to-end acceptance (window-boyutu kalibrasyonu
DecisionRequired'ı bulan pass).

Bu faz için referans implementasyon (TypeScript, 19/19 test geçiyor —
bkz. ayrı `appsel-adaptation-4M1/` klasörü) hazırlandı; aynı yapı
doğrudan Dart'a (Flutter'sız domain paketi olarak) taşınabilir.

**Sıradaki adım:** §7'deki window-boyutu kalibrasyon sorusu
netleşmeden Phase 4M.5 "IMPLEMENTED_AND_VERIFIED" olarak kapatılamaz.
