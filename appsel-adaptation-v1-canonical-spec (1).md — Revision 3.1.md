# Appsel Adaptation V1 — Canonical Decision Specification
### Revizyon 3.1 — Superseded Denominator Clarification

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

**Hâlâ açık bırakılan sınır durum (BACKLOG):** 2/4'ün *hangi*
kombinasyonu olduğu (Key+Long tamam, iki Easy kaçmış vs. başka
kombinasyon) şu an tek bir `Maintain` sonucuna gidiyor — daha ince
ayrım ilk kullanıcı verisiyle netleştirilecek.

### Maintain semantiği (DECIDED)
```
Maintain → freeze progression factor
```
Catalog'ın phase progression step'i uygulanmaz. "Son başarılı
window'u baseline al" gibi daha karmaşık referans mantığı BACKLOG.

### Reduce semantiği (DECIDED — sınır, sayı değil)
```
Catalog generates normal next window
      ↓
Adaptation caps: TargetLoadAdjustment = Reduce
      ↓
(numeric üretim ayrı component'in işi, engine sayı üretmez)
```
Yüzde/oran (`ReduceBand`) PRODUCT DEFAULT olarak sonradan, evidence
ile belirlenir — `Reduce ≠ RecoveryWeek`.

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

---

## 12. Açık Kalan Kararlar (BACKLOG)

| Konu | Neden erteleniyor |
|---|---|
| Next-window 2/4 sınır durumunun ince ayrımı | Product default; erken kullanıcı verisiyle kalibre edilecek |
| Reduce oranı (`ReduceBand`) | Sayısal üretim ayrı component; evidence bekliyor |
| Maintain baseline'ı ("son başarılı window") | V1 basit freeze yeterli, gerekirse V2'de |
| 21K/42K'ya geçişte catalog-specific farklar | `ScheduleRepairPolicy` ve `NextWindowLoadDecision` sabit kalır, yalnız catalog progression/load semantics distance-specific olur |
| `AdaptationSessionChange` tam diff-entity | V1'de `AdaptationDecisionRecord`'daki opsiyonel alanlar yeterli |
| Maintain/Reduce'un numeric translation'ı | Implementation sırasında interface ile çözülecek |
| Concurrency / idempotency key | Implementation sırasında çözülecek |
| Audit record persistence transaction boundary | Implementation sırasında çözülecek |
| `travel` / `personal` için runtime reason token'ı | Şu an canlıda karşılık yok; reason-vocabulary genişlemesi ileride ele alınabilir (§4.1) |

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
   adaptation policy hiçbir yerde km/dakika/yüzde üretmez.
8. **(rev3.1) TS reference implementasyonundaki denominator varsayımı
   REJECTED.** Phase 4M.1'de hazırlanan TypeScript referans kodu
   (`appsel-adaptation-4M1/`), Superseded session'ları
   `ExpectedSessionCount`'tan çıkaran eski (Rev3) okumayı uyguluyordu.
   Bu artık geçersiz — doğru semantics §6'daki Rev3.1 tanımıdır
   (Superseded paydadan silinmez). Gerçek .NET implementasyonu bu
   TS kodunu **davranış otoritesi olarak değil, yalnızca karar-modeli
   referansı olarak** kullanmalı ve bu spesifik noktada ondan
   sapmalıdır. TS kodu henüz bu düzeltmeyle güncellenmedi.

---

## 14. Phase 4M.1 — Adaptation Domain Contracts and Pure Decision Policies

Spec artık **FROZEN**. İlk implementation dilimi bilinçli olarak dar
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
wiring, 4M.4 next-window activation/numeric constraint.

Bu faz için referans implementasyon (TypeScript, 19/19 test geçiyor —
bkz. ayrı `appsel-adaptation-4M1/` klasörü) hazırlandı; aynı yapı
doğrudan Dart'a (Flutter'sız domain paketi olarak) taşınabilir.

**Sıradaki adım:** 4M.2 — persistence katmanı ve `AdaptationDecisionRecord`
yazma/lineage oluşturma; bu noktada concurrency/idempotency invariantı
(§13.4) somut şekilde ele alınmalı.
