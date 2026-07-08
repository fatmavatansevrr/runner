# Plan Catalog (Süreç A) — Canonical Antigravity Implementasyon Brief'i v2

> **Amaç:** Appsel'in kullanıcıdan bağımsız, versioned ve publish edilebilir plan kataloglarını üretmek.
>
> **Kapsam dışı:** Kullanıcı onboarding'i, template seçimi, kullanıcı kapasitesi, warning/suitability,
> plan mode, phase haftalarının kullanıcıya göre çözülmesi, stage→hafta ataması, haftalık hacim eğrisi,
> pace üretimi, takvim, preview/confirm ve `PlanDocument`.
>
> Bu modül hiçbir noktada şu değerleri üretmez veya işlemez:
>
> ```text
> userId
> raceDate
> preferredDays
> weeklyVolumeAnchor
> startingLongRun
> goalFeasibility result
> warning result
> planMode result
> concrete phase weeks
> concrete workout weeks
> weekly volume curve
> scheduled dates
> PlanDocument
> ```

---

## 1. Süreç A ve Süreç B'nin kesin sınırı

Appsel plan sistemi iki ayrı kodlama alanıdır.

### Süreç A — Template Catalog Authoring

Bu dokümanın konusudur.

```text
Distance Master
+ Run Layout
+ Level Modifier
+ Distance Workout Progression
+ Level Progression Modifier
+ Workout Catalog
+ Rule Pack / Policies
+ Combination
→ PublishedTemplateBundle
```

Bu aşamada belirli bir kullanıcı yoktur.

### Süreç B — Runtime Assignment and Personalized Generation

`backend/RunningApp.*` altında daha sonra uygulanacaktır.

```text
Onboarding Input
+ PublishedTemplateBundle
+ Runtime resolver sonuçları
→ kişiselleştirilmiş plan
```

Süreç B:

- Uygun combination'ı seçer.
- Gerçek phase uzunluklarını çözer.
- Progression stage'lerini gerçek haftalara dağıtır.
- Runtime eligibility condition'larını değerlendirir.
- Peak-volume bandından kişiye özel reachable peak üretir.
- Workout instance, dosage, schedule ve `PlanDocument` üretir.

Süreç A bunların hiçbirini yapmaz.

---

## 2. Repo ve solution yerleşimi

```text
runner/
├── backend/                              ← MEVCUT; bu görevde dokunulmaz
│   ├── RunningApp.Api/
│   ├── RunningApp.Application/
│   ├── RunningApp.Domain/
│   ├── RunningApp.Infrastructure/
│   ├── RunningApp.Persistence/
│   ├── RunningApp.IntegrationTests/
│   └── RunningApp.sln
│
├── plan-catalog/                         ← YENİ; yalnız Süreç A
│   ├── src/
│   │   ├── PlanCatalog.Contracts/        ← Süreç A→B published boundary
│   │   ├── PlanCatalog.Core/             ← authoring modelleri, validator ve workflow
│   │   ├── PlanCatalog.Infrastructure/   ← JSON, filesystem, schema, hash, repository
│   │   └── PlanCatalog.Cli/              ← admin/CI komutları
│   │
│   ├── tests/
│   │   └── PlanCatalog.Tests/
│   │       ├── Contracts/
│   │       ├── Validation/
│   │       ├── Serialization/
│   │       ├── Hashing/
│   │       ├── Loading/
│   │       ├── Publishing/
│   │       ├── Combinations/
│   │       └── Golden/
│   │
│   ├── catalog/                          ← düzenlenebilir authoring kaynakları
│   │   ├── templates/
│   │   ├── layouts/
│   │   ├── level-modifiers/
│   │   ├── workout-progressions/
│   │   ├── progression-modifiers/
│   │   ├── workouts/
│   │   ├── registries/
│   │   ├── rule-packs/
│   │   ├── policies/
│   │   └── combinations/
│   │
│   ├── schemas/                          ← JSON Schema dosyaları
│   ├── artifacts/                        ← immutable build/publish çıktıları
│   ├── PlanCatalog.sln
│   └── README.md
│
├── design-references/
└── mobile/
```

### 2.1 Bağımlılık yönü

```text
PlanCatalog.Contracts
        ↑
PlanCatalog.Core
        ↑
PlanCatalog.Infrastructure
        ↑
PlanCatalog.Cli
```

Kurallar:

- `Contracts` başka projeye bağımlı olmaz.
- `Core`, `Infrastructure` veya CLI'a referans vermez.
- `Infrastructure`, Core içindeki port/interface'leri uygular.
- `Cli`, Core use-case'lerini çağırır.
- Hiçbir `PlanCatalog.*` projesi `backend/RunningApp.*` projesine referans vermez.
- Backend entegrasyonu bu görevin parçası değildir.

---

## 3. Contracts ve Core ayrımı

### 3.1 `PlanCatalog.Contracts`

Yalnız Süreç A ile Süreç B arasındaki stable published boundary bulunur:

- `PublishedTemplateBundle`
- `VersionedCatalogReference`
- `CatalogArtifactReference`
- `CatalogReleaseReference`
- Stable shared enums/discriminator'lar
- Runtime condition vocabulary
- Published manifest DTO'ları

Burada bulunmaz:

- Draft modeli
- Validator context'i
- Publish command
- Filesystem loader
- Mutable authoring session
- CLI option modelleri
- Repository implementasyonu

### 3.2 `PlanCatalog.Core`

Authoring domain'i burada yaşar:

- `PlanTemplateDefinition`
- `PhaseDefinition`
- `RunLayoutDefinition`
- `LevelModifierDefinition`
- `WorkoutProgressionDefinition`
- `ProgressionModifierDefinition`
- `WorkoutDefinition`
- `RulePackDefinition`
- `PeakVolumeBandPolicy`
- `TemplateCombinationDefinition`
- Validator'lar
- Bundle assembler
- Publish state machine
- Repository port'ları
- Canonical validation sonuçları

---

## 4. Artifact haritası

```text
PlanCatalog
├── Distance Masters
│   ├── FIVE_K_MASTER
│   ├── TEN_K_MASTER
│   ├── HALF_MARATHON_MASTER
│   └── MARATHON_MASTER
│
├── Run Layouts
│   ├── RUN_LAYOUT_2D
│   ├── RUN_LAYOUT_3D
│   ├── RUN_LAYOUT_4D
│   └── RUN_LAYOUT_5D
│
├── Level Modifiers
│   ├── NEW_MODIFIER
│   ├── INTERMEDIATE_MODIFIER
│   ├── ADVANCED_MODIFIER
│   └── EXPERIENCED_MODIFIER
│
├── Workout Progressions
│   └── {DISTANCE}_WORKOUT_PROGRESSION_V1
│
├── Progression Modifiers
│   └── {LEVEL}_PROGRESSION_MODIFIER_V1
│
├── Workout Definitions
│   └── EASY_STANDARD, FARTLEK, THRESHOLD_TEMPO, ...
│
├── Rule Packs
│   └── APPSEL_RACE_PLAN_V1
│
├── Registries
│   └── RUNTIME_CONDITION_VALUES_V1
│
├── Policies
│   └── PEAK_VOLUME_BANDS_V1, ...
│
└── Combinations
    └── TEN_K__4D__INTERMEDIATE, ...
```

---

## 5. Ortak metadata ve versioning modeli

Her authoring artifact aynı top-level metadata'yı taşır.

```csharp
public sealed record CatalogDocumentMetadata
{
    public required string DocumentType { get; init; }
    public required int SchemaVersion { get; init; }

    public required string Key { get; init; }
    public required int Version { get; init; }

    public required CatalogStatus Status { get; init; }

    // Publish/build sırasında hesaplanır; draft kaynakta null olabilir.
    public string? ContentHash { get; init; }
}
```

```csharp
public enum CatalogStatus
{
    Draft,
    Validated,
    Published,
    Retired
}
```

Kurallar:

- `(DocumentType, Key, Version)` unique olmalıdır.
- Published artifact immutable'dır.
- İçerik değişikliği yeni version gerektirir.
- Aynı key/version ile farklı hash publish edilemez.
- `ContentHash`, metadata içindeki kendi `ContentHash` alanı hariç canonical JSON üzerinden hesaplanır.
- `CreatedAt` gibi nondeterministic alanlar canonical content hash'e dahil edilmez.
- Published artifact üzerinde in-place update yoktur.
- Retire, historical bundle'ları bozmaz.

---

## 6. Stable enum ve JSON kuralları

Bütün enum'lar JSON'da `UPPER_SNAKE_CASE` serialize edilir.

Örnekler:

```text
TEN_K
RACE_SPECIFIC
KEY_SESSION
GOAL_FEASIBILITY_IN
PUBLISHED
```

Kurallar:

- CLR type name veya assembly-qualified discriminator saklanmaz.
- `documentType` açık ve stable string'dir.
- Tarih/saat authoring artifact'larında zorunlu değildir.
- Null optional alanlar serialize edilmez.
- Collection sıraları canonical hashing için deterministik olmalıdır.
- Dictionary kullanılırsa key'ler ordinal olarak sıralanır.
- Decimal değerler invariant culture ile serialize edilir.

---

## 7. Core authoring modelleri

Aşağıdaki kayıtlar `PlanCatalog.Core` içinde bulunur.

### 7.1 PlanTemplateDefinition

```csharp
public sealed record PlanTemplateDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required DistanceFamily DistanceFamily { get; init; }
    public required CoreCycleDefinition CoreCycle { get; init; }
    public required IReadOnlyList<int> SupportedRunsPerWeek { get; init; }
    public required IReadOnlyList<PhaseDefinition> Phases { get; init; }

    // Mesafeye özgü phase-relative progression artifact'ı.
    public required VersionedCatalogReference WorkoutProgression { get; init; }

    public required IReadOnlyList<VersionedCatalogReference>
        RequiredRules { get; init; }
}
```

```csharp
public sealed record CoreCycleDefinition
{
    public required int MinimumWeeks { get; init; }
    public required int DefaultWeeks { get; init; }
    public required int MaximumWeeks { get; init; }
}
```

```csharp
public sealed record PhaseDefinition
{
    public required PhaseKey PhaseKey { get; init; }
    public required int MinimumWeeks { get; init; }
    public required int PreferredWeeks { get; init; }
    public required int MaximumWeeks { get; init; }

    public required IReadOnlyList<PhaseIntent> Intents { get; init; }
    public required IReadOnlyList<WorkoutFamily>
        EligibleWorkoutFamilies { get; init; }

    public required int CompressionPriority { get; init; }
    public required int ExtensionPriority { get; init; }
    public required bool IsCompressionProtected { get; init; }
}
```

### 7.2 RunLayoutDefinition

```csharp
public sealed record RunLayoutDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required int RunsPerWeek { get; init; }
    public required IReadOnlyList<LayoutSlotDefinition> Slots { get; init; }
}
```

```csharp
public sealed record LayoutSlotDefinition
{
    public required int SequenceOrder { get; init; }
    public required SlotRole Role { get; init; }
}
```

Layout gerçek gün isimlerini içermez.

### 7.3 LevelModifierDefinition

```csharp
public sealed record LevelModifierDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required RunningExperience Experience { get; init; }

    public required IReadOnlySet<string> EligibleWorkoutKeys { get; init; }

    // Level'a özgü complexity/dosage artifact'ı.
    public required VersionedCatalogReference
        ProgressionModifier { get; init; }
}
```

Bu modelde bulunmaz:

```text
ProgressionProfileKey
PeakVolumeBandKey
MaximumHardSessionsPerWeek
```

`MaximumHardSessionsPerWeek` tek kaynak olarak
`ProgressionModifierDefinition` içinde yaşar.

### 7.4 WorkoutProgressionDefinition

```csharp
public sealed record WorkoutProgressionDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
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

    public required IReadOnlyList<string>
        WorkoutCandidateKeys { get; init; }

    public required int MinimumExposures { get; init; }
    public required int MaximumExposures { get; init; }

    public required StageCompressionBehavior
        CompressionBehavior { get; init; }

    public required StageExtensionBehavior
        ExtensionBehavior { get; init; }

    public required IReadOnlyList<RuntimeEligibilityCondition>
        Requires { get; init; }

    public string? FallbackStageKey { get; init; }
}
```

Bu artifact kesinlikle içermez:

- Week number
- Concrete phase length
- Calendar date
- User-specific dosage
- Runtime resolver sonucu

### 7.5 RuntimeEligibilityCondition

```csharp
public sealed record RuntimeEligibilityCondition
{
    public required RuntimeConditionType ConditionType { get; init; }

    public required IReadOnlySet<string>
        AllowedValues { get; init; }
}
```

```csharp
public enum RuntimeConditionType
{
    GoalFeasibilityIn,
    PlanModeIn,
    PaceSourceIn,
    TimeAdequacyIn,
    CoreEntryReadinessIn
}
```

V1 kararı:

- `AllowedValues` stable string vocabulary'dir.
- Süreç A backend enum'larına referans vermez.
- Her condition type için izin verilen değerler
  `RuntimeConditionValueRegistry` içinde doğrulanır.
- Serbest metin predicate, expression veya script yoktur.

Örnek registry:

```text
GOAL_FEASIBILITY_IN:
  REALISTIC
  CHALLENGING
  UNSUPPORTED
  NOT_REQUESTED

PLAN_MODE_IN:
  STANDARD
  FOCUSED_CORE
  COMPRESSED
  READINESS_ONLY
  COMPLETION_FOCUSED
```

### 7.6 RuntimeConditionValueRegistryDefinition

`RuntimeConditionValueRegistry` yalnızca kod içi bir yardımcı sınıf değildir; versioned ve publish edilen bir catalog artifact'tır.

```csharp
public sealed record RuntimeConditionValueRegistryDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required IReadOnlyList<RuntimeConditionValueSet>
        ConditionValueSets { get; init; }
}
```

```csharp
public sealed record RuntimeConditionValueSet
{
    public required RuntimeConditionType ConditionType { get; init; }

    public required IReadOnlySet<string>
        AllowedValues { get; init; }
}
```

Canonical artifact:

```text
RUNTIME_CONDITION_VALUES_V1
```

Önerilen source dosya:

```text
catalog/registries/runtime-condition-values.v1.json
```

Bu registry:

- Süreç A'daki `RuntimeEligibilityCondition.AllowedValues` doğrulamasının tek kaynağıdır.
- Published bundle/release içinde exact version ve content hash ile pinlenir.
- Süreç A'nın Süreç B enum'larına compile-time referans vermesini gerektirmez.
- String vocabulary'nin elle dağınık biçimde çoğalmasını engeller.
- Yeni bir runtime değerinin eklenmesi veya kaldırılması durumunda yeni registry version gerektirir.

Örnek:

```json
{
  "metadata": {
    "documentType": "RUNTIME_CONDITION_VALUE_REGISTRY",
    "schemaVersion": 1,
    "key": "RUNTIME_CONDITION_VALUES_V1",
    "version": 1,
    "status": "PUBLISHED"
  },
  "conditionValueSets": [
    {
      "conditionType": "GOAL_FEASIBILITY_IN",
      "allowedValues": [
        "REALISTIC",
        "CHALLENGING",
        "UNSUPPORTED",
        "NOT_REQUESTED"
      ]
    },
    {
      "conditionType": "PLAN_MODE_IN",
      "allowedValues": [
        "STANDARD",
        "FOCUSED_CORE",
        "COMPRESSED",
        "READINESS_ONLY",
        "COMPLETION_FOCUSED"
      ]
    }
  ]
}
```

### Gelecekteki Süreç B yükümlülüğü

Süreç B kodlandığında backend tarafında bir contract test bulunmalıdır:

```text
Backend resolver-output enum/string serialization set
==
Published RUNTIME_CONDITION_VALUES_V1 allowed-value set
```

Bu test:

- Backend'in serialized resolver output değerlerini registry ile karşılaştırır.
- Eksik veya fazladan değer varsa fail eder.
- Registry version değiştiğinde backend'in açıkça uyarlanmasını zorunlu kılar.
- Süreç A ile Süreç B arasında sessiz string drift oluşmasını engeller.

Bu görevde backend testi yazılmaz; yalnızca published contract ve gelecekteki yükümlülük tanımlanır.

### 7.7 ProgressionModifierDefinition

```csharp
public sealed record ProgressionModifierDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }
    public required RunningExperience Experience { get; init; }

    public required int MaximumComplexityTier { get; init; }
    public required int MaximumHardSessionsPerWeek { get; init; }

    public required decimal MainSetDoseMultiplier { get; init; }

    public required bool AllowGoalPaceRehearsal { get; init; }
    public required bool AllowSecondHardStimulus { get; init; }
}
```

Mesafeye özgü stage sırası içermez.

### 7.8 WorkoutDefinition

```csharp
public sealed record WorkoutDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required WorkoutFamily Family { get; init; }
    public required int ComplexityTier { get; init; }

    public required IReadOnlyList<PhaseKey>
        EligiblePhases { get; init; }

    public required IReadOnlyList<PrescriptionMode>
        AllowedPrescriptionModes { get; init; }

    public required IReadOnlyList<WorkoutComponentDefinition>
        Components { get; init; }
}
```

Workout definition structural schema ve eligibility taşır.

İçermez:

- Kullanıcı pace'i
- Kullanıcı mesafesi
- Gerçek tekrar sayısı
- Gerçek takvim tarihi

### 7.9 PeakVolumeBandPolicy

```csharp
public sealed record PeakVolumeBandPolicy
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required IReadOnlyList<PeakVolumeBandEntry>
        Entries { get; init; }
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
DistanceFamily × RunningExperience × RunsPerWeek
```

Peak band:

- Kullanıcı peak'i değildir.
- Hard cap olmak zorunda değildir.
- Runtime provisional/final peak resolver'larının kullandığı product band'dir.

### 7.10 RulePackDefinition

Önceki brief'teki eksik contract budur.

```csharp
public sealed record RulePackDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required VersionedCatalogReference
        RuntimeConditionValueRegistry { get; init; }

    public required VersionedCatalogReference
        PeakVolumeBandPolicy { get; init; }

    public required IReadOnlyList<VersionedCatalogReference>
        Policies { get; init; }

    public required IReadOnlyList<VersionedCatalogReference>
        Rules { get; init; }
}
```

Rule pack:

- Exact policy version'larını pinler.
- Peak-volume policy'nin sahibidir.
- Combination'a doğrudan sayısal band kopyalamaz.

### 7.11 TemplateCombinationDefinition

```csharp
public sealed record TemplateCombinationDefinition
{
    public required CatalogDocumentMetadata Metadata { get; init; }

    public required VersionedCatalogReference
        MasterTemplate { get; init; }

    public required VersionedCatalogReference
        Layout { get; init; }

    public required VersionedCatalogReference
        LevelModifier { get; init; }

    public required VersionedCatalogReference
        RulePack { get; init; }
}
```

Combination yalnız compatibility root'udur.

Şunları doğrudan taşımaz:

- Workout progression
- Progression modifier
- Peak-volume band
- Workout listesi
- Concrete week mapping

Bunlar dependency closure sırasında çözülür.

---

## 8. Published boundary ve bundle assembly

### 8.1 VersionedCatalogReference

`PlanCatalog.Contracts`:

```csharp
public sealed record VersionedCatalogReference
{
    public required string DocumentType { get; init; }
    public required string Key { get; init; }
    public required int Version { get; init; }
}
```

### 8.2 CatalogArtifactReference

```csharp
public sealed record CatalogArtifactReference
{
    public required string DocumentType { get; init; }
    public required string Key { get; init; }
    public required int Version { get; init; }
    public required string ContentHash { get; init; }
}
```

### 8.3 PublishedTemplateBundle

```csharp
public sealed record PublishedTemplateBundle
{
    public required string BundleKey { get; init; }
    public required int BundleVersion { get; init; }

    public required CatalogArtifactReference
        Combination { get; init; }

    public required CatalogArtifactReference
        MasterTemplate { get; init; }

    public required CatalogArtifactReference
        Layout { get; init; }

    public required CatalogArtifactReference
        LevelModifier { get; init; }

    public required CatalogArtifactReference
        WorkoutProgression { get; init; }

    public required CatalogArtifactReference
        ProgressionModifier { get; init; }

    public required CatalogArtifactReference
        RulePack { get; init; }

    public required CatalogArtifactReference
        RuntimeConditionValueRegistry { get; init; }

    public required CatalogArtifactReference
        PeakVolumeBandPolicy { get; init; }

    public required IReadOnlyList<CatalogArtifactReference>
        Workouts { get; init; }

    public required string BundleContentHash { get; init; }
}
```

### 8.4 Dependency closure

Bundle assembler şu graph'i çözer:

```text
Combination
├── MasterTemplate
│   ├── WorkoutProgression
│   └── RequiredRules
├── Layout
├── LevelModifier
│   ├── ProgressionModifier
│   └── EligibleWorkoutKeys
└── RulePack
    ├── RuntimeConditionValueRegistry
    ├── PeakVolumeBandPolicy
    ├── Policies
    └── Rules
```

Workout listesi:

```text
WorkoutProgression candidate keys
∩ LevelModifier eligible keys
∩ WorkoutCatalog published definitions
```

olarak resolve edilir.

Bundle içinde her dependency exact version ve content hash ile pinlenir.

---

## 9. Stage sırası ile gerçek hafta atamasının sınırı

Süreç A:

```text
RACE_SPECIFIC
1. TEN_K_SPECIFIC_INTRO
2. GOAL_PACE_REHEARSAL
```

tanımlar.

Süreç A şunu tanımlamaz:

```text
Week 8  = TEN_K_SPECIFIC_INTRO
Week 11 = GOAL_PACE_REHEARSAL
```

Gerçek atama Süreç B'nin sorumluluğudur:

```text
IWorkoutStageScheduler
ordered stages
+ resolved phase weeks
+ plan mode
+ runtime conditions
→ concrete week assignments
```

Stage→hafta sonucu catalog artifact değil, runtime decision trace sonucudur.

---

## 10. Runtime condition sözleşmesi

Örnek:

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

Süreç A:

- Koşulu tanımlar.
- Runtime değerini hesaplamaz.

Süreç B:

1. Goal feasibility sonucunu üretir.
2. Condition'ı değerlendirir.
3. Stage'i kullanır, fallback'e düşürür veya atlar.
4. Kararı trace'e yazar.

---

## 11. Validator mimarisi

Tek bir dev validator yazılmayacaktır.

```text
PlanCatalog.Core/Validation/
├── PlanTemplateValidator
├── RunLayoutValidator
├── LevelModifierValidator
├── WorkoutProgressionValidator
├── ProgressionModifierValidator
├── WorkoutDefinitionValidator
├── RuntimeConditionValueRegistryValidator
├── PeakVolumeBandPolicyValidator
├── RulePackValidator
├── TemplateCombinationValidator
├── CatalogGraphValidator
└── PublishReadinessValidator
```

Ortak çıktı:

```csharp
public sealed record ValidationIssue(
    string Code,
    ValidationSeverity Severity,
    string Message,
    string? JsonPath = null);

public sealed record ValidationResult(
    IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.All(x => x.Severity != ValidationSeverity.Error);
}
```

---

## 12. Validation kuralları

### 12.1 Master template

- Metadata geçerli.
- `MinimumWeeks <= DefaultWeeks <= MaximumWeeks`.
- Phase key'leri unique.
- Tam olarak bir `TAPER`.
- Taper minimumu > 0.
- `sum(PreferredWeeks) == DefaultWeeks`.
- `sum(MinimumWeeks) <= MinimumWeeks`.
- `sum(MaximumWeeks) >= MaximumWeeks`.
- `WorkoutProgression` mevcut ve aynı distance family'ye ait.
- Required rule reference'ları mevcut.
- Kullanıcıya özel km/pace/date alanı yok.

### 12.2 Run layout

- `RunsPerWeek` product aralığında.
- Slot sayısı = `RunsPerWeek`.
- Sequence unique ve 1..N contiguous.
- Tam olarak bir `LONG_RUN`.
- `RACE` normal weekly layout'a gömülmez.
- `KEY_SESSION` sayısı 0..2 arasında olabilir.

Exact hard-session uygunluğu layout validator'da değil,
combination validator'da level modifier ile birlikte kontrol edilir.

### 12.3 Level modifier

- Experience unique.
- `ProgressionModifier` mevcut ve aynı experience'a ait.
- Workout key'leri mevcut.
- Eski `ProgressionProfileKey` ve `PeakVolumeBandKey` alanları yok.
- Hard-session cap duplicate edilmez.

### 12.4 Workout progression

- Distance family master ile aynı.
- Phase reference'ları master phase'leriyle uyumlu.
- Relative order unique, pozitif ve contiguous.
- Concrete week alanı yok.
- `0 <= MinimumExposures <= MaximumExposures`.
- Candidate workout key'leri mevcut.
- Candidate workout family ilgili phase'de eligible.
- Condition type registry'de mevcut.
- Allowed value registry ile uyumlu.
- Fallback aynı artifact içinde mevcut.
- Circular fallback chain yok.
- Self fallback yok.
- Unreachable stage graph yok.

### 12.5 Progression modifier

- Experience unique.
- Complexity tier >= 1.
- Hard-session cap >= 0.
- Dose multiplier > 0.
- `AllowSecondHardStimulus == false` ise hard-session cap > 1 olamaz.

### 12.6 Workout definition

- Workout key unique.
- Family canonical dört değerden biri:
  - EASY
  - LONG_RUN
  - QUALITY
  - RACE
- Complexity tier >= 1.
- Eligible phases boş olamaz.
- Prescription mode boş olamaz.
- Component type'ları stable.
- CLR type name/discriminator yok.
- User-specific dosage yok.

### 12.7 Runtime condition value registry

- Metadata geçerli.
- Her `RuntimeConditionType` en fazla bir value set'e sahip.
- `AllowedValues` boş olamaz.
- Value'lar unique ve `UPPER_SNAKE_CASE`.
- Aynı string farklı condition type'larda kullanılabilir; bu tek başına hata değildir.
- Published progression condition'larında kullanılan her allowed value registry'de mevcut.
- Registry değişikliği yeni version gerektirir.
- Published bundle exact registry version/hash'ini pinler.

### 12.8 Peak-volume policy

- `(DistanceFamily, Experience, RunsPerWeek)` unique.
- Minimum >= 0.
- Minimum <= Maximum.
- Supported combination için tuple mevcut.
- Exact band modifier veya combination içine kopyalanmamış.

### 12.9 Rule pack

- Peak policy reference mevcut.
- Rule/policy reference'ları unique.
- Referans edilen artifact'lar en az `VALIDATED`.
- Circular rule-pack dependency yok.

### 12.10 Combination

- Root reference'ların tamamı mevcut.
- Layout run count master tarafından destekleniyor.
- Workout progression distance master ile uyumlu.
- Progression modifier experience level modifier ile uyumlu.
- Validator açıkça şu iki-hop zinciri çözer:
  `Combination → LevelModifier → ProgressionModifier`.
- `LevelModifier.ProgressionModifier` referansı missing/invalid ise combination validation da fail eder.
- `KEY_SESSION count <= ProgressionModifier.MaximumHardSessionsPerWeek`.
- Peak matrix'te `(distance, experience, runsPerWeek)` satırı var.
- Effective workout set boş değil.
- Her progression stage için en az bir effective candidate var
  veya valid fallback chain var.
- Combination key unique.
- Combination published ise dependency closure içindeki her artifact published.

### 12.11 Publish readiness

- Schema validation geçti.
- Domain validation geçti.
- Graph validation geçti.
- Content hash hesaplandı.
- Dependency closure tam.
- Aynı key/version farklı hash ile mevcut değil.
- Bundle hash deterministik.
- Published artifact üzerinde mutable path yok.

---

## 13. JSON Schema dosyaları

```text
schemas/
├── catalog-document-metadata.schema.json
├── plan-template.schema.json
├── run-layout.schema.json
├── level-modifier.schema.json
├── workout-progression.schema.json
├── progression-modifier.schema.json
├── workout-definition.schema.json
├── runtime-condition-value-registry.schema.json
├── peak-volume-band-policy.schema.json
├── rule-pack.schema.json
├── template-combination.schema.json
└── published-template-bundle.schema.json
```

İki aşamalı validation:

```text
JSON Schema
→ shape, required field, primitive type

Domain/Cross-catalog validation
→ semantic ve graph invariant'ları
```

---

## 14. Catalog source ve published artifact ayrımı

### 14.1 Authoring source

```text
catalog/
```

- İnsan veya agent tarafından düzenlenebilir.
- Draft/validated kaynakları içerir.
- Backend tarafından okunmaz.

### 14.2 Build/publish output

```text
artifacts/
└── appsel-plan-catalog/
    └── 1.0.0/
        ├── release-manifest.json
        ├── bundles/
        ├── templates/
        ├── layouts/
        ├── level-modifiers/
        ├── workout-progressions/
        ├── progression-modifiers/
        ├── workouts/
        ├── registries/
        ├── rule-packs/
        ├── policies/
        ├── combinations/
        └── checksums.sha256
```

Published output:

- Immutable.
- Canonical serialize edilmiş.
- Hash'leri doğrulanmış.
- Exact dependency version'larını pinlemiş.
- Draft dosyalara referans vermez.

---

## 15. Release manifest

```csharp
public sealed record CatalogReleaseManifest
{
    public required string ReleaseKey { get; init; }
    public required string ReleaseVersion { get; init; }

    public required IReadOnlyList<CatalogArtifactReference>
        Artifacts { get; init; }

    public required IReadOnlyList<CatalogArtifactReference>
        Bundles { get; init; }

    public required string ManifestContentHash { get; init; }
}
```

Release manifest:

- Publish edilen bütün artifact ve bundle'ları listeler.
- Aynı release'in yeniden build edilmesinde aynı hash'i üretmelidir.
- Runtime'ın draft klasörlerini taramasını engeller.
- Rollback yerine önceki release manifest'ine dönüşü mümkün kılar.

---

## 16. Lifecycle ve publish workflow

```text
DRAFT → VALIDATED → PUBLISHED → RETIRED
```

### DRAFT

- Düzenlenebilir.
- Runtime tarafından seçilemez.

### VALIDATED

- Schema + domain + graph validation geçmiştir.
- İçerik hâlâ publish edilmemiştir.

### PUBLISHED

- Immutable.
- Bundle içinde pinlenebilir.
- Aynı key/version overwrite edilemez.

### RETIRED

- Yeni bundle üretiminde seçilmez.
- Historical bundle/reference bozulmaz.

### 16.1 Publish bağımlılık sırası

```text
WorkoutDefinition
ProgressionModifier
RuntimeConditionValueRegistry
PeakVolumeBandPolicy
Rules/Policies
WorkoutProgression
RunLayout
LevelModifier
MasterTemplate
RulePack
Combination
PublishedTemplateBundle
CatalogReleaseManifest
```

Bu sıralama teknik build sırasıdır; source dosyaların yazılma sırası değildir.

### 16.2 Atomic publish

Publish:

1. Temporary output directory oluşturur.
2. Bütün validation'ları çalıştırır.
3. Canonical JSON ve hash'leri üretir.
4. Bundle ve manifest oluşturur.
5. Son doğrulamayı çalıştırır.
6. Temporary directory'yi atomik olarak release directory'ye taşır.

Hata olursa yarım release bırakılmaz.

---

## 17. Infrastructure port ve adapter'ları

Core port'ları:

```csharp
public interface ICatalogSourceRepository { }
public interface IPublishedArtifactRepository { }
public interface IJsonSchemaValidator { }
public interface ICanonicalJsonSerializer { }
public interface IContentHasher { }
public interface ICatalogBundleAssembler { }
public interface ICatalogPublisher { }
```

Infrastructure adapter'ları:

```text
FileSystemCatalogSourceRepository
FileSystemPublishedArtifactRepository
JsonSchemaNetValidator
SystemTextJsonCanonicalSerializer
Sha256ContentHasher
```

Core doğrudan `File.*`, path veya PostgreSQL API kullanmaz.

---

## 18. CLI komutları

```bash
dotnet run --project src/PlanCatalog.Cli -- validate
dotnet run --project src/PlanCatalog.Cli -- validate --key TEN_K_MASTER --version 1
dotnet run --project src/PlanCatalog.Cli -- validate-combination TEN_K__4D__INTERMEDIATE
dotnet run --project src/PlanCatalog.Cli -- build-bundle TEN_K__4D__INTERMEDIATE
dotnet run --project src/PlanCatalog.Cli -- build-release --version 1.0.0
dotnet run --project src/PlanCatalog.Cli -- publish --version 1.0.0
dotnet run --project src/PlanCatalog.Cli -- verify-release --version 1.0.0
dotnet run --project src/PlanCatalog.Cli -- retire --type PLAN_TEMPLATE --key TEN_K_MASTER --version 1
```

CLI:

- Non-zero exit code ile validation failure döner.
- Machine-readable JSON report seçeneği sunar.
- `--dry-run` destekler.
- Unrelated repo dosyalarını değiştirmez.
- Published artifact overwrite etmez.

---

## 19. V1 support matrix

V1 launch scope açıkça belirlenmelidir.

Önerilen başlangıç:

| Distance | 2D | 3D | 4D | 5D |
|---|---:|---:|---:|---:|
| 5K | DRAFT / karar bekliyor | PUBLISHED | PUBLISHED | PUBLISHED |
| 10K | UNSUPPORTED | PUBLISHED | PUBLISHED | PUBLISHED |
| Half Marathon | UNSUPPORTED | PUBLISHED | PUBLISHED | PUBLISHED |
| Marathon | UNSUPPORTED | PUBLISHED | PUBLISHED | PUBLISHED |

Notlar:

- `2D 5K` açık ürün kararı olarak kalabilir.
- Belirsiz kayıt `PUBLISHED` combination üretmez.
- Support matrix kodda hard-code edilmez; combination catalog'dan türetilir.
- Pilot scope yalnız `TEN_K__4D__INTERMEDIATE` olabilir.

> **Not:** `UNSUPPORTED`, `DRAFT / karar bekliyor` ve benzeri ifadeler bu tabloda ürün destek durumunu anlatan informal notasyondur. Bunlar `CatalogStatus` enum değerleri değildir. `CatalogStatus` yalnızca `DRAFT`, `VALIDATED`, `PUBLISHED`, `RETIRED` değerlerini içerir. Desteklenmeyen bir kombinasyon için catalog kaydı hiç üretilmeyebilir veya ayrı bir support-policy alanı kullanılabilir; `UNSUPPORTED` adında yeni bir `CatalogStatus` enum değeri eklenmemelidir.

---

## 20. Pilot üretim sırası

### Adım 1 — Solution skeleton

- Dört proje
- Test projesi
- Directory.Build.props
- Nullable/WarningsAsErrors
- Formatting/analyzer ayarları
- Backend'e referans olmadığını doğrulayan architecture test

### Adım 2 — Published contracts

- Version/reference
- Bundle
- Release manifest
- Stable enum serialization

### Adım 3 — Core authoring modelleri

- Bütün artifact modelleri
- Metadata
- Rule pack
- Workout definition
- Runtime condition value registry artifact modeli

### Adım 4 — Validator'lar

- Individual validator
- Cross-catalog graph validator
- Publish readiness validator

### Adım 5 — JSON schemas

- Her artifact için schema
- Schema tests

### Adım 6 — Loader, canonical serialization ve hashing

- Deterministic load
- Duplicate detection
- Canonical JSON
- SHA-256
- Round-trip test

### Adım 7 — 10K pilot data

- `TEN_K_MASTER v1`
- `TEN_K_WORKOUT_PROGRESSION_V1`
- `RUN_LAYOUT_4D v1`
- `INTERMEDIATE_MODIFIER v1`
- `INTERMEDIATE_PROGRESSION_MODIFIER_V1`
- Pilot workout definitions
- `PEAK_VOLUME_BANDS_V1`
- `RUNTIME_CONDITION_VALUES_V1`
- `APPSEL_RACE_PLAN_V1`
- `TEN_K__4D__INTERMEDIATE v1`

### Adım 8 — Bundle assembler

- Dependency closure
- Exact version/hash pinning
- Effective workout set
- Pilot bundle

### Adım 9 — Publish workflow ve CLI

- Validate
- Build bundle
- Build release
- Publish
- Verify
- Retire

### Adım 10 — Structural golden test

Kişiye özel plan üretilmez.

Doğrulamalar:

- Default core = 12
- Preferred phases total = 12
- One long-run slot
- One key-session slot
- Workout progression concrete week içermez
- Progression modifier doğru experience'a ait
- Peak matrix tuple mevcut
- Effective workout set boş değil
- Runtime condition vocabulary valid
- Bundle exact versions/hashes içeriyor
- Aynı source iki build'de aynı bundle hash'i üretiyor

### Adım 11 — Diğer mesafeler

Pilot onayından sonra:

- 5K
- Half Marathon
- Marathon
- Remaining level/layout combinations

---

## 21. Test stratejisi

### 21.1 Contract tests

- Stable enum serialization
- No CLR discriminator
- Published bundle round-trip
- Metadata/version rules

### 21.2 Validator tests

Her validation code için en az:

- Bir passing test
- Bir failing test

### 21.3 Graph tests

- Missing dependency
- Wrong distance progression
- Wrong experience modifier
- Missing `LevelModifier → ProgressionModifier` reference
- Referansı bulunan fakat yüklenemeyen progression modifier
- Combination'ın iki-hop traversal ile hard-session cap'i çözememesi
- Layout key-session count'ının resolved progression modifier cap'ini aşması
- Missing peak tuple
- Empty effective workout set
- Circular fallback
- Circular rule dependency

### 21.4 Hash tests

- Property order hash'i değiştirmez.
- Collection order semantic ise korunur.
- Dictionary order hash'i değiştirmez.
- `ContentHash` alanı kendi hash'ine dahil edilmez.
- Aynı source aynı hash.
- İçerik değişikliği farklı hash.

### 21.5 Golden tests

- 10K/4D/Intermediate source snapshot
- Published bundle snapshot
- Release manifest snapshot
- Checksums snapshot

### 21.6 Architecture tests

- PlanCatalog projeleri backend'e referans vermiyor.
- Core, Infrastructure'a referans vermiyor.
- Contracts bağımsız.
- Runtime generation sınıfları plan-catalog içinde yok.

---

## 22. CI pipeline

Minimum CI:

```text
dotnet restore
dotnet build -c Release
dotnet test -c Release
catalog validate
catalog build-release --version ci
catalog verify-release --version ci
git diff --exit-code artifacts/generated-fixtures
```

CI şu durumlarda fail eder:

- Invalid schema
- Missing reference
- Duplicate key/version
- Hash mismatch
- Unsupported published combination
- Golden drift
- Backend project reference
- Uncommitted generated fixture drift

---

## 23. Antigravity prompt planı

Kodlama için önerilen ana prompt sayısı: **8**

### Prompt 1 — Solution skeleton ve dependency boundaries

Yalnız proje yapısı, references, analyzers ve architecture tests.

### Prompt 2 — Published contracts

Yalnız `PlanCatalog.Contracts`.

### Prompt 3 — Core authoring models

Metadata dahil bütün artifact modelleri; business workflow yok.

### Prompt 4 — Validators ve runtime condition registry

Individual validation, graph validation ve versioned
`RUNTIME_CONDITION_VALUES_V1` artifact'ı.

### Prompt 5 — Schemas, loader, canonical serializer, hashing

Infrastructure katmanı.

### Prompt 6 — 10K pilot catalog data

Yalnız JSON kaynakları ve structural tests.

### Prompt 7 — Bundle assembler, release manifest ve publish workflow

Dependency closure ve immutable output.

### Prompt 8 — CLI ve end-to-end golden tests

Validate/build/publish/verify komutları.

Her prompt sonrası:

1. Değişen dosyalar
2. Mimari kararlar
3. Eklenen testler
4. Çalıştırılan komutlar
5. Build/test sonucu
6. Bilinen açıklar
7. Unrelated changes confirmation

raporlanmalıdır.

---

## 24. Exit criteria

Süreç A tamamlanmış sayılır:

- [ ] `plan-catalog/` bağımsız build/test ediliyor.
- [ ] Backend'e project reference yok.
- [ ] Contracts/Core/Infrastructure/Cli ayrımı korunuyor.
- [ ] Ortak metadata/version/status modeli var.
- [ ] RulePackDefinition ve WorkoutDefinition mevcut.
- [ ] Dört distance master mevcut.
- [ ] Her master kendi workout progression artifact'ına bağlı.
- [ ] Dört layout ayrı artifact.
- [ ] Dört level modifier ve progression modifier mevcut.
- [ ] Peak-volume policy tüm published combination tuple'larını kapsıyor.
- [ ] Combination exact root version'larını pinliyor.
- [ ] Published bundle dependency closure'ı exact hash'lerle pinliyor.
- [ ] Runtime condition value registry versioned artifact olarak publish ediliyor.
- [ ] Published bundle registry'nin exact version/hash'ini pinliyor.
- [ ] Backend için enum/string parity contract-test yükümlülüğü dokümante edilmiş.
- [ ] Combination validator `Combination → LevelModifier → ProgressionModifier` iki-hop zincirini test ediyor.
- [ ] Stage artifact'larında concrete week yok.
- [ ] JSON schemas tamam.
- [ ] Canonical serialization/hash deterministik.
- [ ] Published artifact immutable.
- [ ] Atomic release publish var.
- [ ] Structural golden tests geçiyor.
- [ ] Plan-catalog içinde onboarding/runtime plan generation kodu yok.

---

## 25. Bu brief ile kapanan önceki boşluklar

Bu v2 brief aşağıdaki eksikleri kapatır:

1. `PlanCatalog.Infrastructure` ayrımı eklendi.
2. `src/`, `tests/`, `schemas/`, `artifacts/` yapısı netleşti.
3. `PlanCatalog.Contracts` yalnız published boundary olarak daraltıldı.
4. Ortak metadata, version, status ve content hash modeli eklendi.
5. Eksik `RulePackDefinition` eklendi.
6. Eksik `WorkoutDefinition` eklendi.
7. Duplicate hard-session cap kaldırıldı; tek sahibi progression modifier oldu.
8. Layout key-session validation ile level cap validation ayrıldı.
9. Dependency closure ve exact version/hash pinning tanımlandı.
10. `PublishedTemplateBundle` assembly kuralı netleşti.
11. Release manifest eklendi.
12. Source catalog ile immutable artifact ayrıldı.
13. Atomic publish tanımlandı.
14. Runtime condition string vocabulary registry ile kapatıldı.
15. CLI, CI, architecture testleri ve error davranışı tanımlandı.
16. Stage→hafta sınırı açıkça Süreç B'ye bırakıldı.
17. Peak bandın rule pack üzerinden üç boyutlu policy lookup olduğu kesinleştirildi.
18. Runtime condition vocabulary versioned `RUNTIME_CONDITION_VALUES_V1` artifact'ına dönüştürüldü.
19. Gelecekteki backend enum/string parity contract testi yükümlülüğü eklendi.
20. Combination validator için `Combination → LevelModifier → ProgressionModifier` iki-hop traversal ve failure testleri açıkça tanımlandı.
21. Support matrix'teki informal `UNSUPPORTED` notasyonunun `CatalogStatus` olmadığı netleştirildi.
