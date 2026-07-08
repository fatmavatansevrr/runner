# Appsel — Master Template Üretim Süreci

**Belge amacı:** Bu doküman yalnızca Appsel’deki **master template kataloglarının üretilmesi, doğrulanması, kombinasyonlarının tanımlanması ve publish edilmesi** sürecini açıklar.

Bu belge **kullanıcı onboarding’i sırasında uygun template’in seçilmesi, kullanıcı girdilerinin değerlendirilmesi, suitability/plan mode çözülmesi veya kişiye özel plan üretilmesi** sürecini kapsamaz.

---

## 1. İki farklı kodlama sürecinin kesin ayrımı

Appsel plan sistemi iki bağımsız fakat bağlantılı kodlama alanından oluşur.

### Süreç A — Template Catalog Authoring

Bu dokümanın konusu budur.

Amaç:

- Mesafe bazlı master template’leri oluşturmak
- Gün sayısı layout’larını oluşturmak
- Seviye modifier’larını oluşturmak
- Workout definition kataloğunu oluşturmak
- Rule pack referanslarını bağlamak
- Desteklenen kombinasyonları tanımlamak
- Template kataloglarını validate etmek
- Versionlamak ve publish etmek

Bu süreçte henüz belirli bir kullanıcı yoktur.

Örnek çıktı:

```text
TEN_K_MASTER v1
RUN_LAYOUT_4D v1
INTERMEDIATE_MODIFIER v1
APPSEL_RACE_PLAN_V1
```

### Süreç B — Runtime Plan Assignment and Generation

Bu ayrı bir backend sürecidir.

Amaç:

- Kullanıcının onboarding cevaplarını almak
- Uygun master template’i seçmek
- Uygun layout ve modifier’ları bağlamak
- Kullanıcının kapasitesini değerlendirmek
- Haftalık hacim, long run, pace ve workout dozajını üretmek
- Warning/suitability/plan mode kararlarını vermek
- PlanDocument oluşturmak
- Preview ve confirm yaşam döngüsünü yürütmek

Örnek:

```text
User:
10K
Intermediate
4 gün
24 km recent weekly volume
9 km recent longest run
12 hafta

Runtime composition:
TEN_K_MASTER
+ RUN_LAYOUT_4D
+ INTERMEDIATE_MODIFIER
+ APPSEL_RACE_PLAN_V1
→ kişiselleştirilmiş 12 haftalık plan
```

Bu iki süreç aynı servis içinde birleştirilmemelidir.

---

## 2. Master template nedir?

Master template, bir yarış mesafesinin **structural skeleton** tanımıdır.

Master template şunları tanımlar:

- Yarış mesafesi
- Core cycle minimum/default/maximum haftaları
- Phase listesi ve sırası
- Her phase’in amacı
- Phase bazlı eligible workout family’leri
- Compression ve extension davranışı
- Taper korunumu
- Gerekli rule grupları
- Desteklenen koşu günü sayıları

Master template şunları tanımlamaz:

- Kullanıcının başlangıç haftalık kilometresi
- Peak kilometre
- Kullanıcıya özel long run
- Pace
- Gerçek takvim günleri
- Goal feasibility
- Warning
- Plan mode

Bunların tamamı runtime generation pipeline’ına aittir.

---

## 3. V1 katalog yapısı

### Distance master’lar

```text
FIVE_K_MASTER
TEN_K_MASTER
HALF_MARATHON_MASTER
MARATHON_MASTER
```

### Run layout’lar

```text
RUN_LAYOUT_2D
RUN_LAYOUT_3D
RUN_LAYOUT_4D
RUN_LAYOUT_5D
```

### Experience modifier’lar

```text
NEW_MODIFIER
INTERMEDIATE_MODIFIER
ADVANCED_MODIFIER
EXPERIENCED_MODIFIER
```

Temel kompozisyon:

```text
Distance Master
+ Run Layout
+ Experience Modifier
+ Rule Pack
+ Workout Catalog
= Template Combination
```

Bu kombinasyon hâlâ kullanıcı planı değildir.

---

## 4. Ayrı katalogların sorumlulukları

### 4.1 Distance Master Template

İçerir:

- `DistanceFamily`
- `CoreCycle`
- `PhaseDefinition` listesi
- `SupportedRunsPerWeek`
- `RequiredRuleKeys`
- Compression/extension politikası
- Mesafeye özgü structural intent

İçermez:

- Exact kilometre
- Exact pace
- Kullanıcı takvimi
- Kullanıcıya özel dosage

### 4.2 Run Layout

Haftalık slot iskeletini tanımlar.

Örnek 4 günlük layout:

```text
KEY_SESSION
EASY_SUPPORT
EASY_SUPPORT
LONG_RUN
```

Gerçek gün isimlerini veya workout içeriğini taşımaz.

### 4.3 Experience Modifier

Koşucu seviyesine göre izinleri ve sınırları tanımlar.

Örnek:

```text
INTERMEDIATE_MODIFIER
```

İçerebilir:

- Maximum hard-session count
- Eligible workout complexity
- `ProgressionModifierKey` — level'a özgü complexity/dosage modifier referansı
- Workout eligibility
- Phase dosage modifier reference’ları

İçermez:

- Mesafeye özgü workout progression stage'leri
- Peak-volume band reference'ı
- Gün sayısına bağlı sayısal peak-volume değerleri

Mesafeye özgü workout progression `PlanTemplateDefinition.WorkoutProgressionKey`
üzerinden bağlanır. Peak-volume bandı ise Rule Pack içindeki ayrı,
versioned `PeakVolumeBandPolicy` artifact'ından
`DistanceFamily × RunningExperience × RunsPerWeek` anahtarıyla çözülür.

Level ayrı master template değildir.

Yanlış:

```text
TEN_K_INTERMEDIATE_4D_MASTER
```

Doğru:

```text
TEN_K_MASTER
+ RUN_LAYOUT_4D
+ INTERMEDIATE_MODIFIER
```

### 4.4 Workout Definition Catalog

Örnek workout key’leri:

```text
EASY_STANDARD
EASY_WITH_STRIDES
FARTLEK
THRESHOLD_TEMPO
TEN_K_REPETITIONS
RACE_PACE_REPEATS
LONG_RUN_STANDARD
LONG_RUN_PROGRESSION
RACE_DAY
```

Workout definition şunları tanımlar:

- Family
- Component schema
- Allowed prescription mode
- Eligible phase
- Eligible level
- Component constraint’leri

Kullanıcıya özel tekrar veya pace runtime’da çözülür.

### 4.5 Rule Pack

Örnek modüller:

```text
WEEKLY_VOLUME_V1
LONG_RUN_V1
PROGRESSION_V1
PHASE_ALLOCATION_V1
PACE_PROFILE_V1
GOAL_FEASIBILITY_V1
TAPER_V1
WARNING_POLICY_V1
```

Master template bu değerleri kopyalamaz; rule key’lerine referans verir.

---

## 5. Kombinasyon kavramı

Kombinasyon, katalog parçalarının birlikte kullanılabilir olduğunu tanımlayan **uyumluluk sözleşmesidir**.


### 5.1 Workout progression stage ile gerçek hafta atamasının sınırı

`WorkoutProgressionDefinition`, gerçek takvim haftalarını veya sabit hafta numaralarını tanımlamaz.

Süreç A yalnızca şunları tanımlar:

- Phase içindeki stage sırası
- Stage'in amacı
- İzin verilen workout candidate'ları
- Stage'in minimum/maximum exposure gereksinimi
- Stage'in compression/extension davranışı
- Stage'in hangi runtime koşullarına bağlı olduğu

Örnek:

```text
BUILD
  1. CONTROLLED_FAST_INTRO
  2. THRESHOLD_DEVELOPMENT

RACE_SPECIFIC
  1. TEN_K_SPECIFIC_INTRO
  2. GOAL_PACE_REHEARSAL
```

Bu tanım şunu söylemez:

```text
Hafta 5 = CONTROLLED_FAST_INTRO
Hafta 6 = THRESHOLD_DEVELOPMENT
Hafta 10 = GOAL_PACE_REHEARSAL
```

Gerçek hafta ataması Süreç B'nin sorumluluğudur; çünkü runtime sırasında şu değerler çözülmüş olur:

- Gerçek phase uzunluğu
- Phase'in compressed veya extended olup olmadığı
- Runway eklenip eklenmediği
- Plan mode
- User readiness
- Goal feasibility
- Kullanılabilir workout sayısı
- Taper proximity
- Scheduling constraints

Dolayısıyla contract sınırı:

```text
Süreç A:
phase-relative ordered stages

Süreç B:
ordered stages → concrete week assignments
```

Runtime tarafında önerilen sorumluluk:

```text
IWorkoutStageScheduler
```

Örnek çıktı:

```json
{
  "phaseKey": "RACE_SPECIFIC",
  "resolvedPhaseWeeks": [8, 9, 10, 11],
  "assignments": [
    {
      "stageKey": "TEN_K_SPECIFIC_INTRO",
      "weekNumbers": [8, 10]
    },
    {
      "stageKey": "THRESHOLD_MAINTENANCE",
      "weekNumbers": [9]
    },
    {
      "stageKey": "GOAL_PACE_REHEARSAL",
      "weekNumbers": [11]
    }
  ]
}
```

Bu çıktı catalog artifact değildir; generated plan decision trace'inin parçasıdır.

### 5.2 Runtime koşullu stage'ler

Bazı progression stage'leri yalnız runtime'da üretilen bir karar sağlandığında kullanılabilir.

Örnek:

```json
{
  "stageKey": "GOAL_PACE_REHEARSAL",
  "requires": [
    {
      "conditionType": "GOAL_FEASIBILITY_IN",
      "allowedValues": ["REALISTIC", "CHALLENGING"]
    }
  ]
}
```

Bu, Süreç A'nın Goal Feasibility hesapladığı anlamına gelmez.

Süreç A yalnızca koşullu eligibility tanımlar:

```text
Bu stage, Goal Feasibility sonucu izin verilen değerlerden biriyse kullanılabilir.
```

Süreç B ise:

1. Goal Feasibility sonucunu üretir.
2. Stage koşullarını değerlendirir.
3. Stage'i kullanır, downgrade eder veya atlar.
4. Kararı decision trace'e yazar.

Condition karşılanmazsa catalog validation başarısız olmaz; runtime stage selection farklı bir candidate seçer.

Örnek fallback:

```json
{
  "stageKey": "GOAL_PACE_REHEARSAL",
  "requires": [
    {
      "conditionType": "GOAL_FEASIBILITY_IN",
      "allowedValues": ["REALISTIC", "CHALLENGING"]
    }
  ],
  "fallbackStageKey": "CURRENT_FITNESS_SPECIFIC_REHEARSAL"
}
```

Runtime koşullarında CLR expression, executable script veya serbest metin predicate saklanmamalıdır. Yalnızca stable condition type ve versioned enum/value kullanılmalıdır.

Örnek:

```text
TEN_K_MASTER
+ RUN_LAYOUT_4D
+ INTERMEDIATE_MODIFIER
```

Geçerli combination kaydı:

```json
{
  "combinationKey": "TEN_K__4D__INTERMEDIATE",
  "masterTemplateKey": "TEN_K_MASTER",
  "layoutKey": "RUN_LAYOUT_4D",
  "modifierKey": "INTERMEDIATE_MODIFIER",
  "status": "PUBLISHED"
}
```

Bu kayıt kullanıcıya atanmış plan değildir.

---

## 6. Kaç kombinasyon bulunur?

Teorik üst sınır:

```text
4 distance
× 4 run layout
× 4 experience
= 64 kombinasyon
```

Ancak tüm kombinasyonlar desteklenmek zorunda değildir.

Örnek support matrix:

| Distance | 2D | 3D | 4D | 5D |
|---|---:|---:|---:|---:|
| 5K | Opsiyonel | Evet | Evet | Evet |
| 10K | Hayır/sınırlı | Evet | Evet | Evet |
| Half Marathon | Hayır | Evet | Evet | Evet |
| Marathon | Hayır | Evet | Evet | Evet |

Combination sayısı formülle hard-code edilmez; catalog compatibility kayıtlarından okunur.

---

## 7. Ana kod modelleri

### 7.1 PlanTemplateDefinition

```csharp
public sealed record PlanTemplateDefinition
{
    public required string DocumentType { get; init; }
    public required int SchemaVersion { get; init; }
    public required string TemplateKey { get; init; }
    public required DistanceFamily DistanceFamily { get; init; }
    public required CoreCycleDefinition CoreCycle { get; init; }
    public required IReadOnlyList<int> SupportedRunsPerWeek { get; init; }
    public required IReadOnlyList<PhaseDefinition> Phases { get; init; }

    // Mesafeye özgü, phase-relative workout stage kataloğu.
    public required string WorkoutProgressionKey { get; init; }

    public required IReadOnlyList<string> RequiredRuleKeys { get; init; }
}
```

`WorkoutProgressionKey`, gerçek hafta ataması içermez. Yalnızca bu mesafe
ailesi için kullanılacak phase-relative progression artifact'ına referans verir.

### 7.2 CoreCycleDefinition

```csharp
public sealed record CoreCycleDefinition
{
    public required int MinimumWeeks { get; init; }
    public required int DefaultWeeks { get; init; }
    public required int MaximumWeeks { get; init; }
}
```

### 7.3 PhaseDefinition

```csharp
public sealed record PhaseDefinition
{
    public required PhaseKey PhaseKey { get; init; }
    public required int MinimumWeeks { get; init; }
    public required int PreferredWeeks { get; init; }
    public required int MaximumWeeks { get; init; }
    public required IReadOnlyList<PhaseIntent> Intents { get; init; }
    public required IReadOnlyList<WorkoutFamily> EligibleWorkoutFamilies { get; init; }
    public required int CompressionPriority { get; init; }
    public required int ExtensionPriority { get; init; }
    public required bool IsCompressionProtected { get; init; }
}
```

### 7.4 RunLayoutDefinition

```csharp
public sealed record RunLayoutDefinition
{
    public required string LayoutKey { get; init; }
    public required int RunsPerWeek { get; init; }
    public required IReadOnlyList<LayoutSlotDefinition> Slots { get; init; }
}
```

### 7.5 LevelModifierDefinition

```csharp
public sealed record LevelModifierDefinition
{
    public required string ModifierKey { get; init; }
    public required RunningExperience Experience { get; init; }
    public required int MaximumHardSessionsPerWeek { get; init; }
    public required IReadOnlySet<string> EligibleWorkoutKeys { get; init; }

    // Level'a özgü complexity ve dosage davranışı.
    public required string ProgressionModifierKey { get; init; }
}
```

Bu modelde özellikle şu eski alanlar **bulunmaz**:

```csharp
ProgressionProfileKey
PeakVolumeBandKey
```

Neden:

- Mesafeye özgü workout sırası `WorkoutProgressionDefinition` içindedir.
- Level'a özgü zorluk/dozaj davranışı `ProgressionModifierDefinition` içindedir.
- Peak-volume bandı ayrı `PeakVolumeBandPolicy` artifact'ında çözülür.

### 7.6 WorkoutProgressionDefinition

```csharp
public sealed record WorkoutProgressionDefinition
{
    public required string DocumentType { get; init; }
    public required int SchemaVersion { get; init; }
    public required string ProgressionKey { get; init; }
    public required DistanceFamily DistanceFamily { get; init; }

    public required IReadOnlyList<PhaseWorkoutProgressionDefinition>
        PhaseProgressions { get; init; }
}
```

```csharp
public sealed record PhaseWorkoutProgressionDefinition
{
    public required PhaseKey PhaseKey { get; init; }

    public required IReadOnlyList<WorkoutProgressionStageDefinition>
        Stages { get; init; }
}
```

```csharp
public sealed record WorkoutProgressionStageDefinition
{
    public required string StageKey { get; init; }
    public required int RelativeOrder { get; init; }
    public required IReadOnlyList<string> WorkoutCandidateKeys { get; init; }

    public required int MinimumExposures { get; init; }
    public required int MaximumExposures { get; init; }

    public required StageCompressionBehavior CompressionBehavior { get; init; }
    public required StageExtensionBehavior ExtensionBehavior { get; init; }

    public required IReadOnlyList<RuntimeEligibilityCondition>
        Requires { get; init; }

    public string? FallbackStageKey { get; init; }
}
```

Bu artifact:

- sabit hafta numarası taşımaz,
- gerçek phase uzunluğu bilmez,
- kullanıcı takvimi bilmez,
- yalnızca phase içindeki stage sırasını ve declarative eligibility'yi tanımlar.

### 7.7 RuntimeEligibilityCondition

```csharp
public sealed record RuntimeEligibilityCondition
{
    public required RuntimeConditionType ConditionType { get; init; }
    public required IReadOnlySet<string> AllowedValues { get; init; }
}
```

Örnek condition type'ları:

```text
GOAL_FEASIBILITY_IN
PLAN_MODE_IN
PACE_SOURCE_IN
TIME_ADEQUACY_IN
CORE_ENTRY_READINESS_IN
```

Süreç A bu değerleri üretmez. Süreç B ilgili resolver çıktılarını üretir ve koşulları değerlendirir.

### 7.8 ProgressionModifierDefinition

```csharp
public sealed record ProgressionModifierDefinition
{
    public required string DocumentType { get; init; }
    public required int SchemaVersion { get; init; }
    public required string ModifierKey { get; init; }
    public required RunningExperience Experience { get; init; }

    public required int MaximumComplexityTier { get; init; }
    public required int MaximumHardSessionsPerWeek { get; init; }
    public required decimal MainSetDoseMultiplier { get; init; }
    public required bool AllowGoalPaceRehearsal { get; init; }
    public required bool AllowSecondHardStimulus { get; init; }
}
```

Bu artifact level'a özgüdür; mesafeye özgü stage sırası içermez.

### 7.9 PeakVolumeBandPolicy

```csharp
public sealed record PeakVolumeBandPolicy
{
    public required string DocumentType { get; init; }
    public required int SchemaVersion { get; init; }
    public required string PolicyKey { get; init; }

    public required IReadOnlyList<PeakVolumeBandEntry> Entries { get; init; }
}
```

```csharp
public sealed record PeakVolumeBandEntry
{
    public required DistanceFamily DistanceFamily { get; init; }
    public required RunningExperience Experience { get; init; }
    public required int RunsPerWeek { get; init; }
    public required decimal MinimumKm { get; init; }
    public required decimal MaximumKm { get; init; }
}
```

Lookup anahtarı:

```text
DistanceFamily
+ RunningExperience
+ RunsPerWeek
```

Örnek:

```text
TEN_K + INTERMEDIATE + 3D → 22–32 km
TEN_K + INTERMEDIATE + 4D → 30–42 km
TEN_K + INTERMEDIATE + 5D → 36–50 km
```

`PeakVolumeBandPolicy` Rule Pack'e bağlanır; `LevelModifierDefinition` veya
`TemplateCombinationDefinition` içinde exact band saklanmaz.

### 7.10 TemplateCombinationDefinition

```csharp
public sealed record TemplateCombinationDefinition
{
    public required string CombinationKey { get; init; }

    public required VersionedCatalogReference MasterTemplate { get; init; }
    public required VersionedCatalogReference Layout { get; init; }
    public required VersionedCatalogReference LevelModifier { get; init; }
    public required VersionedCatalogReference RulePack { get; init; }

    public required CatalogStatus Status { get; init; }
}
```

Combination yalnız compatibility ve version bundle taşır. Exact peak-volume
sayısı veya gerçek workout-week mapping içermez.

---

## 8. Master template üretim sırası

### Adım 1 — Catalog contract’larını oluştur

- Template
- Core cycle
- Phase
- Layout
- Level modifier
- Workout progression
- Progression modifier
- Peak-volume band policy
- Combination
- Workout reference
- Rule reference

Henüz production template seed’i oluşturulmaz.

### Adım 2 — Validator’ları oluştur

#### Master template validation

- Template key zorunlu
- `minimum <= default <= maximum`
- Phase key’leri unique
- Exactly one `TAPER`
- Taper minimumu sıfırdan büyük
- Preferred phase toplamı default core’a eşit
- Minimum phase toplamı minimum core’u aşmıyor
- Maximum phase toplamı maximum core’un altında değil
- `WorkoutProgressionKey` mevcut artifact'a referans veriyor
- Rule key’leri unique
- User-specific dosage alanı yok

#### Run layout validation

- Slot sayısı `RunsPerWeek` ile eşit
- Exactly one `LONG_RUN`
- `KEY_SESSION` sayısı policy’ye uygun
- Slot sequence unique
- Normal layout’a `RACE` gömülmüyor

#### Level modifier validation

- Experience için duplicate modifier yok
- Hard-session cap geçerli
- `ProgressionModifierKey` mevcut artifact'a referans veriyor
- Workout key reference’ları mevcut
- `PeakVolumeBandKey` ve eski `ProgressionProfileKey` alanları yok

#### Workout progression validation

- Phase reference'ları master phase'leriyle uyumlu
- Stage `RelativeOrder` değerleri phase içinde unique ve sıralı
- Stage tanımı concrete week number içermiyor
- `MinimumExposures <= MaximumExposures`
- Runtime condition type'ları canonical registry'de mevcut
- Condition allowed value'ları ilgili resolver output enum'larıyla uyumlu
- Fallback stage varsa aynı progression definition içinde bulunuyor
- Circular fallback chain bulunmuyor
- Candidate workout key'leri workout catalog'da mevcut
- Candidate workout family'leri ilgili phase içinde eligible

#### Peak-volume policy validation

- `(DistanceFamily, RunningExperience, RunsPerWeek)` tuple'ı unique
- `MinimumKm <= MaximumKm`
- Supported combination için gerekli tuple mevcut
- Exact band combination veya modifier içine kopyalanmamış

#### Combination validation

- Referans verilen master/layout/modifier/rule pack mevcut
- Layout gün sayısı master tarafından destekleniyor
- Master'ın workout progression artifact'ı mevcut
- Level'ın progression modifier artifact'ı mevcut
- Rule Pack'in peak-volume policy referansı mevcut
- Peak-volume matrix'te distance × level × days satırı mevcut
- Cross-catalog compatibility geçiyor
- Combination key unique

### Adım 3 — 10K pilot master’ı oluştur

```text
TEN_K_MASTER
```

Core cycle:

```text
minimum: 8
default: 12
maximum: 14
```

Default phase dağılımı:

```text
FOUNDATION: 3
BUILD: 4
RACE_SPECIFIC: 4
TAPER: 1
```

Supported runs/week:

```text
3, 4, 5
```

Ayrıca master şu artifact'a referans verir:

```text
TEN_K_WORKOUT_PROGRESSION_V1
```

### Adım 4 — 10K workout progression artifact'ını oluştur

```text
TEN_K_WORKOUT_PROGRESSION_V1
```

Bu artifact:

- phase-relative stage sırasını,
- candidate workout key'lerini,
- exposure sınırlarını,
- compression/extension davranışını,
- runtime eligibility condition'larını,
- fallback stage'leri

tanımlar.

Concrete week number tanımlamaz.

### Adım 5 — 4D pilot layout’u oluştur

```text
RUN_LAYOUT_4D
```

Slot’lar:

```text
KEY_SESSION
EASY_SUPPORT
EASY_SUPPORT
LONG_RUN
```

### Adım 6 — Intermediate level ve progression modifier'larını oluştur

```text
INTERMEDIATE_MODIFIER
INTERMEDIATE_PROGRESSION_MODIFIER_V1
```

İlk pilotta:

- Maximum hard sessions: 1
- Maximum complexity tier
- Main-set dose multiplier
- Goal-pace rehearsal eligibility
- 10K pilot workout eligibility

Peak-volume bandı burada tutulmaz.

### Adım 7 — Peak-volume matrix artifact'ını oluştur

```text
PEAK_VOLUME_BANDS_V1
```

En az şu pilot satır bulunur:

```text
TEN_K + INTERMEDIATE + 4D → 30–42 km
```

3D ve 5D satırları da v1.0 canonical tabloyla birlikte eklenmelidir.

### Adım 8 — Pilot combination oluştur

```text
TEN_K__4D__INTERMEDIATE
```

Combination:

- master version,
- layout version,
- level modifier version,
- rule-pack version

referanslarını taşır.

Workout progression ve peak policy, sırasıyla master ve rule pack üzerinden çözülür.

### Adım 9 — Structural golden test

Bu aşamada kişiye özel plan veya gerçek hafta ataması üretilmez.

Doğrulanacaklar:

- Master seçilebilir
- Layout destekleniyor
- Modifier uyumlu
- Default core 12 hafta
- Preferred phase toplamı 12
- Exactly one key-session slot
- Exactly one long-run slot
- `TEN_K_WORKOUT_PROGRESSION_V1` mevcut
- Stage'ler concrete week number içermiyor
- `INTERMEDIATE_PROGRESSION_MODIFIER_V1` mevcut
- Peak matrix'te `TEN_K × INTERMEDIATE × 4D` satırı mevcut
- Gerekli workout key’leri mevcut
- Gerekli rule key’leri mevcut

### Adım 10 — Diğer distance master’ları ekle

- `FIVE_K_MASTER`
- `HALF_MARATHON_MASTER`
- `MARATHON_MASTER`

Her master kendi distance-specific `WorkoutProgressionDefinition` artifact'ına referans verir.

### Adım 11 — Diğer layout/modifier kombinasyonlarını oluştur

Support matrix’e göre combination kayıtları üretilir ve validate edilir.

---

## 9. Seed mi, JSON catalog mu?

Önerilen ayrım:

### Kod tarafında

- Contract’lar
- Validator’lar
- Catalog loader
- Publish workflow
- Canonical serialization
- Content hash
- Testler

### Veri tarafında

- Master template JSON
- Layout JSON
- Modifier JSON
- Workout definition JSON
- Combination JSON
- Rule reference’ları

Önerilen klasör:

```text
catalog/
  templates/
    five-k-master.v1.json
    ten-k-master.v1.json
    half-marathon-master.v1.json
    marathon-master.v1.json

  layouts/
    run-layout-2d.v1.json
    run-layout-3d.v1.json
    run-layout-4d.v1.json
    run-layout-5d.v1.json

  modifiers/
    new-modifier.v1.json
    intermediate-modifier.v1.json
    advanced-modifier.v1.json
    experienced-modifier.v1.json

  workout-progressions/
    five-k-workout-progression.v1.json
    ten-k-workout-progression.v1.json
    half-marathon-workout-progression.v1.json
    marathon-workout-progression.v1.json

  progression-modifiers/
    new-progression-modifier.v1.json
    intermediate-progression-modifier.v1.json
    advanced-progression-modifier.v1.json
    experienced-progression-modifier.v1.json

  policies/
    peak-volume-bands.v1.json

  combinations/
    supported-combinations.v1.json

  workouts/
    workout-definitions.v1.json
```

Uzun vadede büyük static C# sınıfları yerine versioned JSON catalog tercih edilmelidir.

---

## 10. Catalog yaşam döngüsü

```text
DRAFT
VALIDATED
PUBLISHED
RETIRED
```

### DRAFT

- Düzenlenebilir
- Runtime tarafından seçilemez

### VALIDATED

- Schema ve domain validation geçmiştir
- Henüz runtime selection’a açık değildir

### PUBLISHED

- Immutable kabul edilir
- Runtime tarafından seçilebilir
- Değişiklik için yeni version gerekir

### RETIRED

- Yeni kullanıcılar için seçilmez
- Historical plan referansları korunur

---

## 11. Versioning

Her catalog nesnesi ayrı versionlanır.

Örnek:

```text
TEN_K_MASTER v1
RUN_LAYOUT_4D v1
INTERMEDIATE_MODIFIER v2
APPSEL_RACE_PLAN_V1 v3
```

Combination belirli version’lara referans vermelidir.

```json
{
  "combinationKey": "TEN_K__4D__INTERMEDIATE",
  "version": 1,
  "masterTemplate": {
    "key": "TEN_K_MASTER",
    "version": 1
  },
  "layout": {
    "key": "RUN_LAYOUT_4D",
    "version": 1
  },
  "modifier": {
    "key": "INTERMEDIATE_MODIFIER",
    "version": 2
  },
  "rulePack": {
    "key": "APPSEL_RACE_PLAN_V1",
    "version": 3
  }
}
```

Published version update edilmez.

---

## 12. Combination testleri

### Structural tests

- Referans verilen katalog kayıtları mevcut
- Master layout gün sayısını destekliyor
- Modifier’ın workout key’leri mevcut
- Master’ın required rule key’leri rule pack’te mevcut
- Phase bounds core cycle ile uyumlu
- Preferred phase toplamı default core’a eşit
- Exactly one taper phase
- Exactly one long-run slot

### Cross-catalog tests

- Workout family phase içinde eligible
- Modifier workout key’i doğru family’ye ait
- Layout canonical slot role kullanıyor
- `PRIMARY_QUALITY` yok; `KEY_SESSION` var
- Template içinde user-specific km/pace/date yok
- Published combination immutable

### Snapshot tests

Her combination canonical JSON olarak serialize edilir ve content hash doğrulanır.

---

## 13. Süreç A tamamlandığında beklenen çıktı

```text
4 distance master
4 distance-specific workout progression artifact
4 run layout
4 experience modifier
4 level-specific progression modifier
N workout definition
versioned peak-volume band policy
versioned rule pack
support matrix
combination catalog
schema validation
domain validation
content hash
publish workflow
catalog tests
```

Bu aşamada şunlar üretilmez:

```text
userId
raceDate
preferredDays
weeklyVolumeAnchor
startingLongRun
goalFeasibility
warning
planMode
weekly curve
scheduled dates
PlanDocument
```

Bunların tamamı Süreç B’ye aittir.

---

## 14. Süreç A ile Süreç B arasındaki contract

Template catalog tarafı runtime’a versioned ve immutable bir bundle verir.

Bu sözleşme üç farklı şeyi açıkça taşır:

1. Versioned catalog reference'ları
2. Phase-relative progression stage contract'ı
3. Runtime eligibility condition contract'ı

### 14.1 PublishedTemplateBundle

```csharp
public sealed record PublishedTemplateBundle
{
    public required TemplateVersionReference MasterTemplate { get; init; }
    public required LayoutVersionReference Layout { get; init; }
    public required ModifierVersionReference LevelModifier { get; init; }

    public required WorkoutProgressionVersionReference
        WorkoutProgression { get; init; }

    public required ProgressionModifierVersionReference
        ProgressionModifier { get; init; }

    public required RulePackVersionReference RulePack { get; init; }

    public required PeakVolumeBandPolicyVersionReference
        PeakVolumeBandPolicy { get; init; }

    public required IReadOnlyList<WorkoutDefinitionReference>
        Workouts { get; init; }
}
```

Bundle, publish edilen exact version'ları taşır. Runtime bu version'ları değiştirmez.

### 14.2 Phase-relative progression contract

Süreç A:

- Stage sırasını ve intent'ini tanımlar.
- Stage candidate workout key'lerini tanımlar.
- Exposure, compression ve extension davranışını tanımlar.
- Sabit hafta numarası üretmez.
- Phase length bilmez.
- Kullanıcı takvimi bilmez.

Süreç B:

- Gerçek phase haftalarını çözer.
- `IWorkoutStageScheduler` ile stage'leri concrete week'lere dağıtır.
- Compression/extension durumunda stage tekrarını veya atlanmasını belirler.
- Gerçek workout instance ve dosage üretir.
- Atama kararını decision trace'e yazar.

Sınır:

```text
Süreç A:
phase-relative ordered stages

Süreç B:
ordered stages → concrete week assignments
```

### 14.3 Runtime eligibility condition contract

Süreç A bir stage için declarative condition tanımlayabilir:

```text
GOAL_FEASIBILITY_IN
PLAN_MODE_IN
PACE_SOURCE_IN
TIME_ADEQUACY_IN
CORE_ENTRY_READINESS_IN
```

Süreç A bu değerleri üretmez.

Süreç B:

1. İlgili resolver sonucunu üretir.
2. Stage condition'ını değerlendirir.
3. Stage'i kullanır, fallback'e düşürür veya atlar.
4. Sonucu decision trace'e yazar.

Örnek:

```json
{
  "stageKey": "GOAL_PACE_REHEARSAL",
  "conditionType": "GOAL_FEASIBILITY_IN",
  "actualValue": "UNSUPPORTED",
  "outcome": "NOT_ELIGIBLE",
  "fallbackStageKey": "CURRENT_FITNESS_SPECIFIC_REHEARSAL"
}
```

Catalog runtime sonucuna bağımlı değildir; yalnız runtime tarafından değerlendirilecek
stable condition contract'ını taşır.

### 14.4 Peak-volume lookup contract

Süreç A:

- `PeakVolumeBandPolicy` artifact'ını versionlar ve publish eder.
- Distance × level × runs-per-week tuple'larını tanımlar.
- Combination publish edilirken gerekli tuple'ın varlığını doğrular.

Süreç B:

- Kullanıcının resolved distance, level ve runs-per-week değerleriyle lookup yapar.
- Bandı provisional/final peak resolver'larında kullanır.
- Kullanıcıya özel reachable peak'i ayrıca hesaplar.

Süreç A exact kullanıcı peak'i üretmez.

Akış:

```text
Template Catalog Authoring
→ PublishedTemplateBundle
→ Runtime Assignment and Personalized Generation
```

Runtime bu bundle’ı kullanır fakat değiştirmez.

---

## 15. Antigravity implementation planı

Master-template tarafı için önerilen ana prompt sayısı: **6**

### Prompt 1 — Catalog domain contracts

- Template
- Phase
- Layout
- Level modifier
- Workout progression
- Progression modifier
- Peak-volume band policy
- Combination
- Version/status

### Prompt 2 — Catalog validators

- Template validator
- Layout validator
- Level modifier validator
- Workout progression validator
- Progression modifier validator
- Peak-volume policy validator
- Combination validator
- Cross-catalog validation

### Prompt 3 — 10K pilot catalog

- `TEN_K_MASTER`
- `TEN_K_WORKOUT_PROGRESSION_V1`
- `RUN_LAYOUT_4D`
- `INTERMEDIATE_MODIFIER`
- `INTERMEDIATE_PROGRESSION_MODIFIER_V1`
- `PEAK_VOLUME_BANDS_V1` pilot satırları
- Pilot workout reference’ları
- Pilot combination

### Prompt 4 — Serialization, hashing ve loader

- JSON loading
- Schema validation
- Canonical serialization
- Content hash
- Duplicate key/version kontrolü

### Prompt 5 — Publish workflow

- `DRAFT → VALIDATED → PUBLISHED`
- Immutable version
- Repository
- Publish command
- Retire/new-version davranışı

### Prompt 6 — Diğer distance master’lar ve support matrix

- 5K
- Half Marathon
- Marathon
- Remaining layouts/modifiers
- Supported combinations

Her ana prompt sonrasında test ve review yapılmalıdır.

---

## 16. Exit criteria

Süreç şu şartlarda tamamlanır:

- Dört distance master mevcut
- Layout’lar ayrı katalogda
- Experience seviyeleri modifier olarak mevcut
- Support matrix açık
- Combination kayıtları deterministik
- Bütün kayıtlar versioned
- Published kayıtlar immutable
- Template içinde kullanıcı dosage’ı yok
- Cross-catalog validator bütün referansları doğruluyor
- 10K/4D/Intermediate structural golden testleri geçiyor
- Runtime onboarding veya user assignment kodu bu modülde bulunmuyor

---

## 17. Net karar

Appsel’de template ile ilgili iki kodlama süreci vardır:

```text
A. Template Catalog Authoring
B. Runtime Template Assignment and Personalized Plan Generation
```

Bu doküman yalnızca **A sürecini** tanımlar.

A sürecinde kombinasyonlar **catalog compatibility kayıtları** olarak oluşturulur.

B sürecinde bu kombinasyonlardan biri kullanıcı girdilerine göre seçilir ve kişiselleştirilmiş plan üretilir.

Önerilen bounded-context ayrımı:

```text
Appsel.PlanCatalog
Appsel.PlanGeneration
```
