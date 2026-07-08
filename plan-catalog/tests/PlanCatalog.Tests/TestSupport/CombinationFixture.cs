using PlanCatalog.Contracts;
using PlanCatalog.Contracts.Enums;
using PlanCatalog.Core.Enums;
using PlanCatalog.Contracts.References;
using PlanCatalog.Core.Catalog;
using PlanCatalog.Core.Models;

namespace PlanCatalog.Tests.TestSupport;

/// <summary>A minimal, valid TEN_K / 4D / INTERMEDIATE-shaped fixture, mutable per-test via `with`.</summary>
public sealed class CombinationFixture
{
    public WorkoutDefinition EasyWorkout { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.WorkoutDefinition, "EASY_STANDARD", status: CatalogStatus.Published),
        Family = WorkoutFamily.Easy,
        ComplexityTier = 1,
        EligiblePhases = [PhaseKey.Foundation, PhaseKey.Build, PhaseKey.RaceSpecific, PhaseKey.Taper],
        AllowedPrescriptionModes = [PrescriptionMode.EffortBased],
        Components = [new WorkoutComponentDefinition { SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, IntensityDescriptor = "EASY" }]
    };

    public WorkoutDefinition LongRunWorkout { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.WorkoutDefinition, "LONG_RUN_STANDARD", status: CatalogStatus.Published),
        Family = WorkoutFamily.LongRun,
        ComplexityTier = 1,
        EligiblePhases = [PhaseKey.Foundation, PhaseKey.Build, PhaseKey.RaceSpecific, PhaseKey.Taper],
        AllowedPrescriptionModes = [PrescriptionMode.EffortBased],
        Components = [new WorkoutComponentDefinition { SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, IntensityDescriptor = "EASY" }]
    };

    public WorkoutDefinition ThresholdWorkout { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.WorkoutDefinition, "THRESHOLD_TEMPO", status: CatalogStatus.Published),
        Family = WorkoutFamily.Quality,
        ComplexityTier = 2,
        EligiblePhases = [PhaseKey.Build, PhaseKey.RaceSpecific],
        AllowedPrescriptionModes = [PrescriptionMode.PaceBased],
        Components = [new WorkoutComponentDefinition { SequenceOrder = 1, ComponentType = WorkoutComponentType.MainSet, IntensityDescriptor = "THRESHOLD" }]
    };

    public RuntimeConditionValueRegistryDefinition Registry { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.RuntimeConditionValueRegistry, "RUNTIME_CONDITION_VALUES_V1", status: CatalogStatus.Published),
        ConditionValueSets =
        [
            new RuntimeConditionValueSet { ConditionType = RuntimeConditionType.GoalFeasibilityIn, AllowedValues = new HashSet<string> { "REALISTIC", "CHALLENGING" } }
        ]
    };

    public PeakVolumeBandPolicy PeakVolumeBandPolicy { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.PeakVolumeBandPolicy, "PEAK_VOLUME_BANDS_V1", status: CatalogStatus.Published),
        Entries =
        [
            new PeakVolumeBandEntry { DistanceFamily = DistanceFamily.TenK, Experience = RunningExperience.Intermediate, RunsPerWeek = 4, MinimumKm = 30m, MaximumKm = 45m }
        ]
    };

    public ProgressionModifierDefinition ProgressionModifier { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.ProgressionModifier, "INTERMEDIATE_PROGRESSION_MODIFIER_V1", status: CatalogStatus.Published),
        Experience = RunningExperience.Intermediate,
        MaximumComplexityTier = 2,
        MaximumHardSessionsPerWeek = 1,
        MainSetDoseMultiplier = 1.0m,
        AllowGoalPaceRehearsal = true,
        AllowSecondHardStimulus = false
    };

    public LevelModifierDefinition LevelModifier { get; }

    public RunLayoutDefinition Layout { get; } = new()
    {
        Metadata = Meta.Of(DocumentTypes.RunLayout, "RUN_LAYOUT_4D", status: CatalogStatus.Published),
        RunsPerWeek = 4,
        Slots =
        [
            new LayoutSlotDefinition { SequenceOrder = 1, Role = SlotRole.KeySession },
            new LayoutSlotDefinition { SequenceOrder = 2, Role = SlotRole.EasySupport },
            new LayoutSlotDefinition { SequenceOrder = 3, Role = SlotRole.EasySupport },
            new LayoutSlotDefinition { SequenceOrder = 4, Role = SlotRole.LongRun }
        ]
    };

    public WorkoutProgressionDefinition WorkoutProgression { get; }

    public PlanTemplateDefinition MasterTemplate { get; }

    public RulePackDefinition RulePack { get; }

    public TemplateCombinationDefinition Combination { get; }

    public CombinationFixture()
    {
        LevelModifier = new LevelModifierDefinition
        {
            Metadata = Meta.Of(DocumentTypes.LevelModifier, "INTERMEDIATE_MODIFIER", status: CatalogStatus.Published),
            Experience = RunningExperience.Intermediate,
            EligibleWorkoutKeys = new HashSet<string> { EasyWorkout.Metadata.Key, LongRunWorkout.Metadata.Key, ThresholdWorkout.Metadata.Key },
            ProgressionModifier = Ref(DocumentTypes.ProgressionModifier, ProgressionModifier.Metadata)
        };

        WorkoutProgression = new WorkoutProgressionDefinition
        {
            Metadata = Meta.Of(DocumentTypes.WorkoutProgression, "TEN_K_WORKOUT_PROGRESSION_V1", status: CatalogStatus.Published),
            DistanceFamily = DistanceFamily.TenK,
            PhaseProgressions =
            [
                new PhaseWorkoutProgressionDefinition
                {
                    PhaseKey = PhaseKey.Build,
                    Stages =
                    [
                        new WorkoutProgressionStageDefinition
                        {
                            StageKey = "TEMPO_INTRO",
                            RelativeOrder = 1,
                            WorkoutCandidateKeys = [ThresholdWorkout.Metadata.Key],
                            MinimumExposures = 1,
                            MaximumExposures = 3,
                            CompressionBehavior = StageCompressionBehavior.Compressible,
                            ExtensionBehavior = StageExtensionBehavior.Extendable,
                            Requires = []
                        }
                    ]
                }
            ]
        };

        MasterTemplate = new PlanTemplateDefinition
        {
            Metadata = Meta.Of(DocumentTypes.PlanTemplate, "TEN_K_MASTER", status: CatalogStatus.Published),
            DistanceFamily = DistanceFamily.TenK,
            CoreCycle = new CoreCycleDefinition { MinimumWeeks = 8, DefaultWeeks = 12, MaximumWeeks = 14 },
            SupportedRunsPerWeek = [3, 4, 5],
            Phases =
            [
                new PhaseDefinition { PhaseKey = PhaseKey.Foundation, MinimumWeeks = 2, PreferredWeeks = 3, MaximumWeeks = 4, Intents = [PhaseIntent.AerobicBase], EligibleWorkoutFamilies = [WorkoutFamily.Easy, WorkoutFamily.LongRun], CompressionPriority = 1, ExtensionPriority = 1, IsCompressionProtected = false },
                new PhaseDefinition { PhaseKey = PhaseKey.Build, MinimumWeeks = 3, PreferredWeeks = 4, MaximumWeeks = 5, Intents = [PhaseIntent.VolumeBuild], EligibleWorkoutFamilies = [WorkoutFamily.Easy, WorkoutFamily.LongRun, WorkoutFamily.Quality], CompressionPriority = 2, ExtensionPriority = 2, IsCompressionProtected = false },
                new PhaseDefinition { PhaseKey = PhaseKey.RaceSpecific, MinimumWeeks = 2, PreferredWeeks = 4, MaximumWeeks = 4, Intents = [PhaseIntent.RaceSpecificSharpening], EligibleWorkoutFamilies = [WorkoutFamily.Easy, WorkoutFamily.LongRun, WorkoutFamily.Quality], CompressionPriority = 3, ExtensionPriority = 3, IsCompressionProtected = false },
                new PhaseDefinition { PhaseKey = PhaseKey.Taper, MinimumWeeks = 1, PreferredWeeks = 1, MaximumWeeks = 1, Intents = [PhaseIntent.Taper], EligibleWorkoutFamilies = [WorkoutFamily.Easy, WorkoutFamily.LongRun], CompressionPriority = 4, ExtensionPriority = 4, IsCompressionProtected = true }
            ],
            WorkoutProgression = Ref(DocumentTypes.WorkoutProgression, WorkoutProgression.Metadata),
            RequiredRules = []
        };

        RulePack = new RulePackDefinition
        {
            Metadata = Meta.Of(DocumentTypes.RulePack, "APPSEL_RACE_PLAN_V1", status: CatalogStatus.Published),
            RuntimeConditionValueRegistry = Ref(DocumentTypes.RuntimeConditionValueRegistry, Registry.Metadata),
            PeakVolumeBandPolicy = Ref(DocumentTypes.PeakVolumeBandPolicy, PeakVolumeBandPolicy.Metadata),
            Policies = [],
            Rules = []
        };

        Combination = new TemplateCombinationDefinition
        {
            Metadata = Meta.Of(DocumentTypes.TemplateCombination, "TEN_K__4D__INTERMEDIATE", status: CatalogStatus.Published),
            MasterTemplate = Ref(DocumentTypes.PlanTemplate, MasterTemplate.Metadata),
            Layout = Ref(DocumentTypes.RunLayout, Layout.Metadata),
            LevelModifier = Ref(DocumentTypes.LevelModifier, LevelModifier.Metadata),
            RulePack = Ref(DocumentTypes.RulePack, RulePack.Metadata)
        };
    }

    private static VersionedCatalogReference Ref(string documentType, PlanCatalog.Core.Metadata.CatalogDocumentMetadata metadata) => new()
    {
        DocumentType = documentType,
        Key = metadata.Key,
        Version = metadata.Version
    };

    public CatalogSourceSnapshot BuildSnapshot() => new CatalogSnapshotBuilder()
        .With(MasterTemplate)
        .With(Layout)
        .With(LevelModifier)
        .With(WorkoutProgression)
        .With(ProgressionModifier)
        .With(EasyWorkout)
        .With(LongRunWorkout)
        .With(ThresholdWorkout)
        .With(Registry)
        .With(PeakVolumeBandPolicy)
        .With(RulePack)
        .With(Combination)
        .Build();
}
