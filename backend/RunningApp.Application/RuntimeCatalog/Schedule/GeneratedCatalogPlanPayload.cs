using RunningApp.Domain.Enums;

namespace RunningApp.Application.RuntimeCatalog.Schedule;

/// <summary>
/// Backend Integration Phase 4F.1 — the strongly typed, immutable schedule
/// contract that a future materialization phase will populate and store in
/// <see cref="RuntimeCatalog.PreviewRouting.CatalogPreviewSnapshot.GeneratedPreviewPlanPayload"/>.
///
/// A running-session day type. Deliberately has NO "Rest" member (unlike
/// <see cref="TrainingDayType"/>, which has <c>Rest</c>): per Phase 4F.1
/// Decision 4, a generated schedule day always represents an actual planned
/// running session — days without a generated session are implicit rest
/// days and are never materialized as entries at all. This is enforced at
/// the type level (a caller cannot even construct a "rest" session), not
/// just by a runtime validation rule.
/// </summary>
public enum GeneratedCatalogWorkoutType
{
    Easy,
    Interval,
    Tempo,
    LongRun,
    RecoveryEasy,
}

/// <summary>
/// Which of a session's (or segment's) two possible numeric targets is
/// authoritative. Phase 4F.1 Decision 7: a session has exactly one
/// authoritative target — the other metric, if present at all, is a
/// non-authoritative estimate only.
/// </summary>
public enum GeneratedCatalogPrescriptionBasis
{
    Distance,
    Duration,
}

/// <summary>
/// Phase 4F.1 Decision 8 — a structured, numeric pace prescription. Numeric
/// fields are authoritative; <see cref="GeneratedCatalogPacePrescription.DisplayText"/>
/// is optional presentation data only and must never override them.
/// </summary>
public enum GeneratedCatalogPaceType
{
    /// <summary>No pace guidance at all — every numeric field is null.</summary>
    None,

    /// <summary>A single authoritative target pace.</summary>
    Target,

    /// <summary>A minimum/maximum pace band, no single authoritative point.</summary>
    Range,

    /// <summary>Guidance is qualitative (e.g. "conversational effort") — no numeric pace target of any kind.</summary>
    EffortOnly,
}

/// <summary>Phase 4F.1 Decision 9 — the role a workout segment plays within a session.</summary>
public enum GeneratedCatalogSegmentType
{
    WarmUp,
    WorkInterval,
    Recovery,
    Steady,
    CoolDown,
}

/// <summary>
/// Phase 4F.1 Decision 8 — structured numeric pace guidance. Never a
/// free-text-only field. <see cref="TargetSecondsPerKm"/>/<see cref="MinSecondsPerKm"/>/
/// <see cref="MaxSecondsPerKm"/> are the authoritative values; <see cref="DisplayText"/>
/// is optional, non-authoritative presentation text (e.g. a pre-formatted
/// "4:45-5:00/km" string) that must never be used to derive a numeric value
/// and must never contradict-but-override the numeric fields during
/// validation (validation never inspects <see cref="DisplayText"/> at all).
/// Unit conversion (km/mile) is deliberately NOT implemented in this phase —
/// storing pace as seconds-per-km (not a formatted string) is exactly what
/// keeps that future conversion possible without a schema change.
/// </summary>
public sealed class GeneratedCatalogPacePrescription
{
    public required GeneratedCatalogPaceType PaceType { get; init; }

    /// <summary>Required and positive when <see cref="PaceType"/> is <see cref="GeneratedCatalogPaceType.Target"/>; null otherwise.</summary>
    public int? TargetSecondsPerKm { get; init; }

    /// <summary>Required and positive when <see cref="PaceType"/> is <see cref="GeneratedCatalogPaceType.Range"/>; null otherwise. Represents the FASTER (numerically smaller seconds-per-km) end of the band — see the validator's documented min/max convention.</summary>
    public int? MinSecondsPerKm { get; init; }

    /// <summary>Required and positive when <see cref="PaceType"/> is <see cref="GeneratedCatalogPaceType.Range"/>; null otherwise. Represents the SLOWER (numerically larger seconds-per-km) end of the band.</summary>
    public int? MaxSecondsPerKm { get; init; }

    /// <summary>Optional, non-authoritative presentation text. Never validated against or used to derive the numeric fields.</summary>
    public string? DisplayText { get; init; }

    /// <summary>Optional qualitative effort label (e.g. "conversational", "comfortably hard"). Most relevant when <see cref="PaceType"/> is <see cref="GeneratedCatalogPaceType.EffortOnly"/>, but not restricted to it.</summary>
    public string? EffortLabel { get; init; }
}

/// <summary>
/// Phase 4F.1 Decision 9 — an optional, strongly typed workout segment
/// (warm-up / work interval / recovery / steady / cooldown). Segments are
/// never generated from catalog rules in this phase — the contract exists so
/// a future materialization phase has somewhere typed to put them for
/// interval/threshold sessions. Easy and simple long-run sessions are
/// expected to have zero segments.
/// </summary>
public sealed class GeneratedCatalogWorkoutSegmentPayload
{
    /// <summary>1-based ordering within the session. Must be unique and consecutive from 1 across a session's segment list.</summary>
    public required int SegmentOrder { get; init; }

    public required GeneratedCatalogSegmentType SegmentType { get; init; }

    /// <summary>e.g. "6" for "6x400m". Null/absent for non-repeating segment types (warm-up, cooldown, single steady block).</summary>
    public int? RepetitionCount { get; init; }

    public required GeneratedCatalogPrescriptionBasis PrescriptionBasis { get; init; }

    /// <summary>Required and positive when <see cref="PrescriptionBasis"/> is Distance; null when Duration.</summary>
    public double? TargetDistanceKm { get; init; }

    /// <summary>Required and positive when <see cref="PrescriptionBasis"/> is Duration; null when Distance.</summary>
    public int? TargetDurationSeconds { get; init; }

    public GeneratedCatalogPacePrescription? PacePrescription { get; init; }

    /// <summary>Free-text intensity descriptor (e.g. "z4", "goal pace"), distinct from the session-level <see cref="GeneratedCatalogTrainingDayPayload.PlannedIntensity"/>.</summary>
    public string? Intensity { get; init; }

    /// <summary>e.g. "jog", "walk", "standing" — only meaningful for <see cref="GeneratedCatalogSegmentType.Recovery"/> segments.</summary>
    public string? RecoveryType { get; init; }

    /// <summary>Optional, non-authoritative presentation text.</summary>
    public string? DisplayText { get; init; }
}

/// <summary>
/// Phase 4F.1 Decision 11 — internal, day-level provenance explaining exactly
/// which catalog artifacts produced a session. Never exposed on any public
/// DTO (mirrors the existing internal-only treatment of
/// <see cref="Resolvers.ResolverDecisionTrace"/> and the day-level catalog
/// fields already present on <see cref="Domain.Entities.TrainingDay"/> —
/// <c>CatalogWorkoutKey</c>, <c>CatalogStageKey</c>, <c>CatalogSlotRole</c>).
/// </summary>
public sealed class GeneratedCatalogDayProvenance
{
    public required string SourceStageKey { get; init; }
    public string? SourceWorkoutKey { get; init; }
    public int? SourceWorkoutVersion { get; init; }
    public string? SourceProgressionStepKey { get; init; }

    /// <summary>Layout slot role (e.g. "KEY_SESSION", "EASY_SUPPORT", "LONG_RUN") — mirrors <see cref="Domain.Entities.TrainingDay.CatalogSlotRole"/>.</summary>
    public string? SourceLayoutSlotRole { get; init; }

    /// <summary>
    /// Phase 10K-FREQ.6D.4D Split D — exact prescription-profile lineage, populated only when
    /// the source <c>CatalogPrescribedSession.PrescriptionSource</c> is
    /// <c>CatalogSessionPrescriptionSource.ProfileBacked</c> (both fields together); null/null
    /// for Legacy sessions. Mirrors <see cref="Domain.Entities.TrainingDay.CatalogPrescriptionProfileKey"/>/
    /// <see cref="Domain.Entities.TrainingDay.CatalogPrescriptionProfileVersion"/> exactly — carried
    /// through, never re-derived or re-selected at this mapping boundary.
    /// </summary>
    public string? SourcePrescriptionProfileKey { get; init; }
    public int? SourcePrescriptionProfileVersion { get; init; }
}

/// <summary>
/// Phase 4F.1 — a single planned running session. Represents ONLY an actual
/// session; a day with no generated session is an implicit rest day and is
/// simply absent from <see cref="GeneratedCatalogWeekPayload.Sessions"/> (see
/// <see cref="GeneratedCatalogWorkoutType"/>'s own doc comment). Deliberately
/// carries no REQUIRED/OPTIONAL classification field (Phase 4F.1 explicitly
/// defers that decision) and no recovery-jog-suggestion field (out of the
/// schedule contract entirely — a future recommendation/DailyTip/Notification
/// concern, never a <see cref="Domain.Entities.TrainingDay"/> row).
/// </summary>
public sealed class GeneratedCatalogTrainingDayPayload
{
    public required DateOnly Date { get; init; }

    /// <summary>1-based ordering of this session among the week's sessions (NOT a day-of-week index — rest days are not represented at all, so this is dense/consecutive across only the actual sessions).</summary>
    public required int SessionOrderInWeek { get; init; }

    public required GeneratedCatalogWorkoutType WorkoutType { get; init; }

    public required GeneratedCatalogPrescriptionBasis PrescriptionBasis { get; init; }

    /// <summary>Authoritative target when <see cref="PrescriptionBasis"/> is Distance; must be positive then, and null when <see cref="PrescriptionBasis"/> is Duration.</summary>
    public double? TargetDistanceKm { get; init; }

    /// <summary>Authoritative target when <see cref="PrescriptionBasis"/> is Duration; must be positive then, and null when <see cref="PrescriptionBasis"/> is Distance.</summary>
    public int? TargetDurationMinutes { get; init; }

    /// <summary>Non-authoritative estimate only, permitted when <see cref="PrescriptionBasis"/> is Duration (must not duplicate the authoritative <see cref="TargetDurationMinutes"/> semantics — it estimates distance for a duration-based session).</summary>
    public double? EstimatedDistanceKm { get; init; }

    /// <summary>Non-authoritative estimate only, permitted when <see cref="PrescriptionBasis"/> is Distance.</summary>
    public int? EstimatedDurationMinutes { get; init; }

    /// <summary>Free-text session-level intensity descriptor (e.g. "z2"), mirrors <see cref="Domain.Entities.TrainingDay.Intensity"/>.</summary>
    public string? PlannedIntensity { get; init; }

    public required GeneratedCatalogPacePrescription PacePrescription { get; init; }

    /// <summary>Optional; empty for sessions with no structured intervals (e.g. most Easy/LongRun sessions).</summary>
    public IReadOnlyList<GeneratedCatalogWorkoutSegmentPayload> Segments { get; init; } = Array.Empty<GeneratedCatalogWorkoutSegmentPayload>();

    public required GeneratedCatalogDayProvenance Provenance { get; init; }
}

/// <summary>Phase 4F.1 Decision 11 — internal, week-level provenance.</summary>
public sealed class GeneratedCatalogWeekProvenance
{
    public required string StageKey { get; init; }
    public string? SourcePhaseKey { get; init; }
    public string? VolumeRuleKey { get; init; }
    public string? ProgressionReferenceKey { get; init; }
}

/// <summary>
/// Phase 4F.1 — one plan-relative, seven-day week block (Decision 6). Week 1
/// begins exactly on <see cref="GeneratedCatalogPlanPayload.StartDate"/>;
/// week N begins <c>StartDate + (N-1)*7</c> days and ends 6 days later.
/// Carries its own explicit <see cref="StartDate"/>/<see cref="EndDate"/> so
/// confirm/read paths never need to recompute them with a possibly-different
/// formula.
/// </summary>
public sealed class GeneratedCatalogWeekPayload
{
    /// <summary>1-based, consecutive from 1, no duplicates across a plan.</summary>
    public required int WeekNumber { get; init; }

    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }

    /// <summary>The workout-progression stage key this week's sessions were drawn from (e.g. "BUILD", "GOAL_PACE_REHEARSAL").</summary>
    public required string StageKey { get; init; }

    /// <summary>Sum of this week's session distances — present for read-path parity with <see cref="Domain.Entities.TrainingWeek.PlannedVolumeKm"/> (Home/Calendar/Active-Plan-Details all read that field). Not independently re-derived by the read paths; the payload is the source of truth.</summary>
    public required double PlannedVolumeKm { get; init; }

    /// <summary>Only actual running sessions — never a rest-day placeholder (see <see cref="GeneratedCatalogWorkoutType"/>).</summary>
    public required IReadOnlyList<GeneratedCatalogTrainingDayPayload> Sessions { get; init; }

    public required GeneratedCatalogWeekProvenance Provenance { get; init; }
}

/// <summary>
/// Phase 4F.1 Decision 11 — internal, plan-level provenance. Distinct from
/// (and narrower than) <see cref="RuntimeCatalog.PreviewRouting.CatalogPreviewSnapshot"/>'s
/// own outer provenance fields (<c>CandidateStatusAtGenerationTime</c>,
/// <c>RouteReason</c>, resolver results, decision trace) — this type carries
/// only what a materialized schedule itself needs to explain its own origin,
/// independent of the outer snapshot envelope.
/// </summary>
public sealed class GeneratedCatalogPlanProvenance
{
    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    /// <summary>Every referenced dependency artifact's exact (key, version) identity, keyed by role — same shape and roles as <see cref="RuntimeCatalog.PreviewRouting.CatalogPreviewSnapshot.ReferencedArtifacts"/>.</summary>
    public required IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions { get; init; }

    /// <summary>Always <see cref="RuntimeCatalog.PreviewRouting.GenerationSource.Catalog"/> for a payload produced by a real materializer — recorded explicitly, never assumed, mirroring the outer snapshot's own convention.</summary>
    public required string GenerationSource { get; init; }

    /// <summary>The single frozen domain evaluation date used for the entire preview (see Phase 4E.1's AsOfDate policy) — duplicated here (not just on the outer snapshot) so the schedule payload is self-describing if ever inspected independently of its envelope.</summary>
    public required DateOnly AsOfDate { get; init; }

    /// <summary>Identifies which stage-to-week materializer implementation/version produced this payload. Null in Phase 4F.1 — no materializer exists yet. Present in the schema now so a future phase does not need another schema-version bump just to add version tracking for itself.</summary>
    public string? MaterializerVersion { get; init; }
}

/// <summary>
/// Backend Integration Phase 4F.1 — the complete, strongly typed, immutable
/// generated-schedule contract. This is the ONLY shape
/// <see cref="RuntimeCatalog.PreviewRouting.CatalogPreviewSnapshot.GeneratedPreviewPlanPayload"/>
/// may hold going forward (never <c>object</c>, <c>dynamic</c>,
/// <c>JsonElement</c>, or an untyped dictionary/blob — Phase 4F.1 Decision 2).
///
/// This phase defines the contract and its structural validator only. No
/// code in this phase produces a real, non-null instance of this type from
/// catalog stages — <see cref="RuntimeCatalog.PreviewRouting.CatalogPreviewGenerator"/>
/// still always leaves <c>GeneratedPreviewPlanPayload</c> null (see Phase
/// 4E.1's own documented "success path unreachable" boundary, unchanged by
/// this phase). Every instance of this type used in this phase's own tests
/// is a hand-built fixture proving the contract and validator work — never a
/// claim that live materialization exists.
/// </summary>
public sealed class GeneratedCatalogPlanPayload
{
    /// <summary>The only schedule schema version this phase's validator accepts. Intentionally a separate version number from <see cref="RuntimeCatalog.PreviewRouting.CatalogPreviewSnapshot"/>'s own (currently unversioned) outer schema — Phase 4F.1 Decision 10: schedule evolution must not be coupled to outer-snapshot evolution.</summary>
    public const int CurrentSchemaVersion = 1;

    public required int SchemaVersion { get; init; }

    /// <summary>The user-selected plan start date, preserved EXACTLY — never normalized to a Monday, a preferred running day, the first generated session, or a race-week boundary (Phase 4F.1 Decision 5).</summary>
    public required DateOnly StartDate { get; init; }

    /// <summary>Derived consistently from the full plan-relative week structure — must equal the final week's <see cref="GeneratedCatalogWeekPayload.EndDate"/>. Never independently chosen.</summary>
    public required DateOnly EndDate { get; init; }

    /// <summary>The number of weeks this plan was generated for. Must equal <c>Weeks.Count</c> exactly (Phase 4F.1 Decision 3) — this field exists (rather than relying on <c>Weeks.Count</c> alone) so validation can detect a truncated/partial week list as a distinct, explicit failure rather than silently trusting whatever list length happens to be present.</summary>
    public required int PlannedWeekCount { get; init; }

    /// <summary>Expected running-session count per full week — drives the generic (non-TEN_K-hardcoded) per-week session-count validation rule. For the Phase 4F.1 TEN_K/INTERMEDIATE/4D pilot fixture this is 4.</summary>
    public required int DaysPerWeek { get; init; }

    /// <summary>The internal catalog distance family (e.g. "TEN_K") — never a hardcoded pilot constant; carried through from <see cref="Resolvers.ResolverInputSnapshot.CanonicalDistanceFamily"/> so the contract remains reusable for FIVE_K/HALF_MARATHON/MARATHON in a future phase (Phase 4F.1 Decision 12).</summary>
    public required string CanonicalDistanceFamily { get; init; }

    public required GoalType GoalType { get; init; }

    public required string CandidateKey { get; init; }
    public required int CandidateVersion { get; init; }

    /// <summary>Duplicated from <see cref="GeneratedCatalogPlanProvenance.DependencyVersions"/> at the top level for read-path convenience (matches the "Recommended contract shape" field list) — the provenance object remains the authoritative internal record.</summary>
    public required IReadOnlyDictionary<string, PlanCatalogReference> DependencyVersions { get; init; }

    public required IReadOnlyList<GeneratedCatalogWeekPayload> Weeks { get; init; }

    public required GeneratedCatalogPlanProvenance Provenance { get; init; }
}
