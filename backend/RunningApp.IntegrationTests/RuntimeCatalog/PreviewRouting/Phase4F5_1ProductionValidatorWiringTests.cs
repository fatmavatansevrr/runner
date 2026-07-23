using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RunningApp.Application.DTOs.Plan;
using RunningApp.Application.Exceptions;
using RunningApp.Application.RuntimeCatalog;
using RunningApp.Application.RuntimeCatalog.PreviewRouting;
using RunningApp.Application.RuntimeCatalog.Resolvers;
using RunningApp.Application.RuntimeCatalog.Schedule.Materialization;
using RunningApp.Domain.Enums;
using RunningApp.IntegrationTests.RuntimeCatalog.Schedule.Materialization;
using Xunit;

namespace RunningApp.IntegrationTests.RuntimeCatalog.PreviewRouting;

/// <summary>
/// Backend Integration Phase 4F.5.1 — proves the independent-audit gap fix:
/// <see cref="DatedGeneratedCatalogPlanSkeletonValidator"/> is now actually
/// invoked in the reachable dark preview path, after materialization and
/// before the dark result is discarded, with correct ordering, correct
/// failure wrapping, and zero effect on any public/persistence boundary.
/// </summary>
public sealed class Phase4F5_1ProductionValidatorWiringTests
{
    private sealed class RecordingCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly ICatalogWeekSkeletonCalendarMaterializer _inner;
        private readonly List<string> _sequence;
        public RecordingCalendarMaterializer(ICatalogWeekSkeletonCalendarMaterializer inner, List<string> sequence) { _inner = inner; _sequence = sequence; }
        public int InvocationCount { get; private set; }
        public DatedGeneratedCatalogPlanSkeleton? LastResult { get; private set; }

        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context)
        {
            InvocationCount++;
            _sequence.Add("materializer");
            LastResult = _inner.Materialize(context);
            return LastResult;
        }
    }

    private sealed class RecordingValidator : IDatedGeneratedCatalogPlanSkeletonValidator
    {
        private readonly IDatedGeneratedCatalogPlanSkeletonValidator _inner;
        private readonly List<string> _sequence;
        public RecordingValidator(IDatedGeneratedCatalogPlanSkeletonValidator inner, List<string> sequence) { _inner = inner; _sequence = sequence; }
        public int InvocationCount { get; private set; }
        public DatedGeneratedCatalogPlanSkeleton? ReceivedSkeleton { get; private set; }
        public IReadOnlyList<DayOfWeek>? ReceivedPreferredDays { get; private set; }
        public DayOfWeek? ReceivedLongRunDay { get; private set; }

        public DatedGeneratedCatalogPlanSkeletonValidationResult Validate(DatedGeneratedCatalogPlanSkeleton skeleton, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDayPreference)
        {
            InvocationCount++;
            _sequence.Add("validator");
            ReceivedSkeleton = skeleton;
            ReceivedPreferredDays = preferredDays;
            ReceivedLongRunDay = longRunDayPreference;
            return _inner.Validate(skeleton, preferredDays, longRunDayPreference);
        }
    }

    private sealed class ThrowingValidator : IDatedGeneratedCatalogPlanSkeletonValidator
    {
        private readonly Exception _exception;
        public int InvocationCount { get; private set; }
        public ThrowingValidator(Exception exception) => _exception = exception;
        public DatedGeneratedCatalogPlanSkeletonValidationResult Validate(DatedGeneratedCatalogPlanSkeleton skeleton, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDayPreference)
        {
            InvocationCount++;
            throw _exception;
        }
    }

    private sealed class CountingValidator : IDatedGeneratedCatalogPlanSkeletonValidator
    {
        public int InvocationCount { get; private set; }
        public DatedGeneratedCatalogPlanSkeletonValidationResult Validate(DatedGeneratedCatalogPlanSkeleton skeleton, IReadOnlyList<DayOfWeek> preferredDays, DayOfWeek longRunDayPreference)
        {
            InvocationCount++;
            return DatedGeneratedCatalogPlanSkeletonValidationResult.Valid();
        }
    }

    private sealed class MalformedCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly ICatalogWeekSkeletonCalendarMaterializer _inner = new CatalogWeekSkeletonCalendarMaterializer();

        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context)
        {
            var real = _inner.Materialize(context);
            // Corrupt week 1: force two slots onto the same date (duplicate-date invariant violation).
            var week = real.Weeks[0];
            var slots = week.SessionSlots.ToList();
            var tampered = new List<DatedGeneratedCatalogSessionSlotSkeleton>(slots)
            {
                [0] = slots[0] with { SessionDate = slots[1].SessionDate, SessionDayOfWeek = slots[1].SessionDayOfWeek },
            };
            var tamperedWeek = week with { SessionSlots = tampered };
            var weeks = real.Weeks.ToList();
            weeks[0] = tamperedWeek;
            return real with { Weeks = weeks };
        }
    }

    private sealed class ThrowingCalendarMaterializer : ICatalogWeekSkeletonCalendarMaterializer
    {
        private readonly Exception _exception;
        public ThrowingCalendarMaterializer(Exception exception) => _exception = exception;
        public DatedGeneratedCatalogPlanSkeleton Materialize(CatalogCalendarAssignmentContext context) => throw _exception;
    }

    private sealed class FixedResultEligibilityGate : ICatalogCandidateEligibilityGate
    {
        private readonly PlanCatalogCandidateSummary _result;
        public FixedResultEligibilityGate(PlanCatalogCandidateSummary result) => _result = result;
        public Task<PlanCatalogCandidateSummary> LoadForPublicPreviewAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) => Task.FromResult(_result);
        public Task<PlanCatalogCandidateSummary> LoadForInternalDryRunAsync(string candidateKey, int candidateVersion, System.Threading.CancellationToken ct = default) => Task.FromResult(_result);
    }

    private static ICatalogPlanSkeletonOrchestrator RealSkeletonOrchestrator() => new CatalogPlanSkeletonOrchestrator(
        new CatalogPhaseAllocationResolver(),
        new CatalogRunLayoutResolver(),
        new CatalogStageToWeekContextFactory(),
        new CatalogStageToWeekMaterializer(),
        new GeneratedCatalogPlanSkeletonValidator());

    private static RuntimeConditionResolutionService RealOrchestration() =>
        new(new TimeAdequacyResolver(), new PaceSourceResolver(), new CoreEntryReadinessResolver(), new GoalFeasibilityResolver());

    private static ICatalogCandidateEligibilityGate RealDraftGate()
    {
        var bundleLoader = new PlanCatalogBundleLoader(
            Options.Create(new PlanCatalogOptions { CatalogRootPath = Path.Combine(TestPlanServicesFactory.RepoRoot(), "plan-catalog", "catalog") }),
            NullLogger<PlanCatalogBundleLoader>.Instance);
        return new CatalogCandidateEligibilityGate(bundleLoader);
    }

    private static async Task<PlanCatalogCandidateSummary> LoadControlledPublishedCandidateAsync()
    {
        var real = await CatalogPlanSkeletonOrchestratorFixtures.LoadRealPilotCandidateAsync();
        return new PlanCatalogCandidateSummary
        {
            CandidateKey = real.CandidateKey,
            CandidateVersion = real.CandidateVersion,
            CandidateStatus = "PUBLISHED",
            CanonicalDistanceFamily = real.CanonicalDistanceFamily,
            Level = real.Level,
            DaysPerWeek = real.DaysPerWeek,
            CoreCycle = real.CoreCycle,
            MasterTemplate = real.MasterTemplate,
            Layout = real.Layout,
            LevelModifier = real.LevelModifier,
            WorkoutProgression = real.WorkoutProgression,
            ProgressionModifier = real.ProgressionModifier,
            RulePack = real.RulePack,
            PeakVolumeBandPolicy = real.PeakVolumeBandPolicy,
            RuntimeConditionValueRegistry = real.RuntimeConditionValueRegistry,
            DependencyStatuses = new Dictionary<string, string>
            {
                ["masterTemplate"] = "PUBLISHED", ["layout"] = "PUBLISHED", ["levelModifier"] = "PUBLISHED", ["rulePack"] = "PUBLISHED",
            },
            ReferencedWorkouts = real.ReferencedWorkouts,
            PhaseKeys = real.PhaseKeys,
            PhaseAllocations = real.PhaseAllocations,
            SlotRoles = real.SlotRoles,
        };
    }

    private static GeneratePreviewRequest PilotRequest(
        DateOnly raceDate,
        IReadOnlyList<Weekday>? preferredDays = null,
        Weekday? longRunDay = null) => new()
    {
        GoalType = GoalType.Race,
        GoalDistance = GoalDistance.TenK,
        Level = RunningBackground.Intermediate,
        DaysPerWeek = 4,
        Unit = DistanceUnit.Km,
        StartDate = raceDate.AddDays(-84),
        RaceDate = raceDate,
        PreferredDays = preferredDays ?? new[] { Weekday.Mon, Weekday.Wed, Weekday.Fri, Weekday.Sun },
        LongRunDay = longRunDay ?? Weekday.Sun,
    };

    // ── A. Validator invoked in the dark path, with correct data ──────────────

    [Fact]
    public async Task Validator_InvokedOnce_ReceivesExactMaterializerOutputAndContext_PublicPayloadUnchanged()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var sequence = new List<string>();
        var recordingMaterializer = new RecordingCalendarMaterializer(new CatalogWeekSkeletonCalendarMaterializer(), sequence);
        var recordingValidator = new RecordingValidator(new DatedGeneratedCatalogPlanSkeletonValidator(), sequence);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), recordingMaterializer, recordingValidator);
        var asOfDate = new DateOnly(2026, 1, 5);

        var snapshot = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.Equal(1, recordingMaterializer.InvocationCount);
        Assert.Equal(1, recordingValidator.InvocationCount);
        Assert.Same(recordingMaterializer.LastResult, recordingValidator.ReceivedSkeleton);
        Assert.Equal(new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Sunday }, recordingValidator.ReceivedPreferredDays);
        Assert.Equal(DayOfWeek.Sunday, recordingValidator.ReceivedLongRunDay);
        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
    }

    // ── B. Correct execution order ─────────────────────────────────────────────

    [Fact]
    public async Task ExecutionOrder_MaterializerRunsBeforeValidator()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var sequence = new List<string>();
        var recordingMaterializer = new RecordingCalendarMaterializer(new CatalogWeekSkeletonCalendarMaterializer(), sequence);
        var recordingValidator = new RecordingValidator(new DatedGeneratedCatalogPlanSkeletonValidator(), sequence);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), recordingMaterializer, recordingValidator);
        var asOfDate = new DateOnly(2026, 1, 5);

        await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.Equal(new[] { "materializer", "validator" }, sequence);
    }

    // ── C. Validator rejection is wrapped ───────────────────────────────────────

    [Fact]
    public async Task ValidatorRejection_WrappedInPlanPreviewGenerationFailedException_PreservingExactInstance()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var cause = new CatalogDatedSkeletonInvalidException("synthetic dated-skeleton validation failure");
        var throwingValidator = new ThrowingValidator(cause);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), new CatalogWeekSkeletonCalendarMaterializer(), throwingValidator);
        var asOfDate = new DateOnly(2026, 1, 5);

        var ex = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        Assert.Same(cause, ex.InnerException);
        Assert.Equal(1, throwingValidator.InvocationCount);
        Assert.Contains("CATALOG_INTERNAL_SKELETON_MATERIALIZATION_FAILED", ex.Message);
    }

    // ── D. Malformed materializer output is caught by the REAL validator ──────

    [Fact]
    public async Task MalformedMaterializerOutput_DuplicateSessionDate_RejectedByRealValidator()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(
            gate, RealOrchestration(), RealSkeletonOrchestrator(), new MalformedCalendarMaterializer(), new DatedGeneratedCatalogPlanSkeletonValidator());
        var asOfDate = new DateOnly(2026, 1, 5);

        var ex = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        Assert.IsType<CatalogDatedSkeletonInvalidException>(ex.InnerException);
    }

    // ── E. Validator not invoked when materialization fails ───────────────────

    [Fact]
    public async Task Validator_NotInvoked_WhenMaterializationThrows()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var cause = new CatalogPreferredDayConfigurationUnsafeException("synthetic materializer failure");
        var throwingMaterializer = new ThrowingCalendarMaterializer(cause);
        var countingValidator = new CountingValidator();
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), throwingMaterializer, countingValidator);
        var asOfDate = new DateOnly(2026, 1, 5);

        var ex = await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        Assert.Equal(0, countingValidator.InvocationCount);
        Assert.Same(cause, ex.InnerException);
    }

    // ── F. Validator not invoked before eligibility ────────────────────────────

    [Fact]
    public async Task Validator_NotInvoked_ForDraftV10Candidate()
    {
        var countingValidator = new CountingValidator();
        var generator = new CatalogPreviewGenerator(RealDraftGate(), RealOrchestration(), RealSkeletonOrchestrator(), new CatalogWeekSkeletonCalendarMaterializer(), countingValidator);

        await Assert.ThrowsAsync<CatalogCandidateNotPublishedException>(() =>
            generator.GenerateAsync(PilotRequest(new DateOnly(2026, 4, 1)), new DateOnly(2026, 1, 5)));

        Assert.Equal(0, countingValidator.InvocationCount);
    }

    [Fact]
    public async Task Validator_NotInvoked_WhenGovernanceRejectsRequest()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var countingValidator = new CountingValidator();
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator(), new CatalogWeekSkeletonCalendarMaterializer(), countingValidator);
        var request = PilotRequest(new DateOnly(2026, 4, 1));
        request.RaceDate = null; // TimeAdequacyResolver throws ArgumentException -> governance-failure path, before dark skeleton/calendar steps.

        await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(request, new DateOnly(2026, 1, 5)));

        Assert.Equal(0, countingValidator.InvocationCount);
    }

    [Fact]
    public async Task Validator_NotInvoked_WhenStructuralSkeletonCreationFails()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var countingValidator = new CountingValidator();
        var throwingSkeletonOrchestrator = new ThrowingSkeletonOrchestrator(new CatalogPhaseAllocationInvalidException("synthetic skeleton failure"));
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), throwingSkeletonOrchestrator, new CatalogWeekSkeletonCalendarMaterializer(), countingValidator);
        var asOfDate = new DateOnly(2026, 1, 5);

        await Assert.ThrowsAsync<PlanPreviewGenerationFailedException>(() =>
            generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate));

        Assert.Equal(0, countingValidator.InvocationCount);
    }

    private sealed class ThrowingSkeletonOrchestrator : ICatalogPlanSkeletonOrchestrator
    {
        private readonly Exception _exception;
        public ThrowingSkeletonOrchestrator(Exception exception) => _exception = exception;
        public CatalogPlanSkeletonOrchestrationResult Build(CatalogPlanSkeletonOrchestrationContext context) => throw _exception;
    }

    // ── G. Public boundary regression after successful validation ─────────────

    [Fact]
    public async Task PublicBoundary_UnchangedAfterSuccessfulValidation()
    {
        var candidate = await LoadControlledPublishedCandidateAsync();
        var gate = new FixedResultEligibilityGate(candidate);
        var generator = new CatalogPreviewGenerator(gate, RealOrchestration(), RealSkeletonOrchestrator());
        var asOfDate = new DateOnly(2026, 1, 5);

        var snapshot = await generator.GenerateAsync(PilotRequest(asOfDate.AddDays(84)), asOfDate);

        Assert.NotNull(snapshot.GeneratedPreviewPlanPayload);
        Assert.DoesNotContain(snapshot.GetType().GetProperties(), p => p.Name.Contains("Dated") || p.Name.Contains("Validator") || p.Name.Contains("Validation"));
        Assert.Equal(4, snapshot.ResolverResults.Count);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.ContentHash));
    }

    [Fact]
    public void ConfirmService_HasNoReferenceToDatedSkeletonOrValidatorTypes()
    {
        var fields = typeof(CatalogPlanConfirmationService).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.DoesNotContain(fields, f => (f.FieldType.FullName ?? "").Contains("Materialization"));

        var ctorParamTypeNames = typeof(CatalogPlanConfirmationService).GetConstructors().Single().GetParameters().Select(p => p.ParameterType.FullName ?? "").ToList();
        Assert.DoesNotContain(ctorParamTypeNames, t => t.Contains("Materialization"));
    }
}
