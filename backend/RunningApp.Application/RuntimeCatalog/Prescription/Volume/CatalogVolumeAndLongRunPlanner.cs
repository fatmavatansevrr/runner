using RunningApp.Application.RuntimeCatalog.Schedule.Binding;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

internal interface ICatalogVolumeAndLongRunPlanner
{
    CatalogVolumeAndLongRunPlan Build(CatalogVolumePlanningRequest request);
}

internal sealed class CatalogVolumeAndLongRunPlanner : ICatalogVolumeAndLongRunPlanner
{
    private readonly VolumeSafetyPolicy _policy;

    /// <summary>Uses <see cref="VolumeSafetyPolicy.Default"/> — the same values this class held as private inline constants before Phase 4G.3B.0. Preserves every existing call site (<c>new CatalogVolumeAndLongRunPlanner()</c>) unchanged.</summary>
    public CatalogVolumeAndLongRunPlanner() : this(VolumeSafetyPolicy.Default)
    {
    }

    public CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy policy)
    {
        _policy = policy;
    }

    public CatalogVolumeAndLongRunPlan Build(CatalogVolumePlanningRequest request)
    {
        if (request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 4 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.BeginnerFourDay).Build(request);
        }
        // Phase 10K-GEN.12 -- GEN.11-approved 2D authority, same exact typed
        // combination match as every other branch here. Never a broad
        // "DaysPerWeek == 2" condition without also pinning CanonicalDistanceFamily
        // and Level, so a future non-TenK or non-Beginner/Intermediate 2D
        // candidate can never silently inherit this authority.
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "NEW" &&
            request.Candidate.DaysPerWeek == 2 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.Beginner2D).Build(request);
        }
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "INTERMEDIATE" &&
            request.Candidate.DaysPerWeek == 2 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.Intermediate2D).Build(request);
        }
        // Phase 10K-GEN.9 defect fix: this branch was unconditional on Level
        // (matching any 3D candidate), which would have silently routed a
        // future Advanced x3D candidate to Intermediate's own 3D numeric
        // authority instead of falling through to the Advanced dispatch
        // below. GEN.7 froze Advanced x3D as a distinct, separately-numbered
        // identity (VolumeSafetyPolicy.Advanced3D) -- restricting this branch
        // to Intermediate (its only real caller today) is implementation-only
        // and introduces no new authority.
        if (request.Candidate.Level == "INTERMEDIATE" && request.Candidate.DaysPerWeek == 3 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.ThreeDayIntermediate).Build(request);
        }
        // Phase 10K-GEN.23 -- implements GEN.21's frozen Option-1 authority
        // (Phase K decision). Exact typed combination match only, mirroring
        // every other branch here -- never a broad "DaysPerWeek == 3"
        // condition, so this can never silently swallow a future
        // Advanced x3D candidate.
        if (request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 3 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.ThreeDayBeginner).Build(request);
        }
        // Phase 10K-FREQ.6D.10: exact typed combination match only (CanonicalDistanceFamily +
        // Level + DaysPerWeek) -- never a broad "DaysPerWeek >= 5" or "Level != Beginner" condition,
        // so a future Beginner x5D/Advanced x5D candidate can never silently inherit this
        // Intermediate-specific FREQ.6C authority.
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "INTERMEDIATE" &&
            request.Candidate.DaysPerWeek == 5 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.FiveDayIntermediate).Build(request);
        }
        // Phase 10K-FREQ.6D.26 -- FREQ.6D.23/6D.25-approved Intermediate x6D
        // authority, same exact typed combination match as 5D above.
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "INTERMEDIATE" &&
            request.Candidate.DaysPerWeek == 6 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.SixDayIntermediate).Build(request);
        }
        // Phase 10K-GEN.9 -- GEN.7/GEN.8-approved Advanced 3D/4D/5D/6D
        // authority, same exact typed combination match as Intermediate
        // above. Never a broad "Level != Intermediate/Beginner" condition.
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "ADVANCED" &&
            ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.ForAdvancedDaysPerWeek(request.Candidate.DaysPerWeek)).Build(request);
        }
        var weekCount = request.BoundPlan.Weeks.Count;
        if (weekCount < request.Candidate.CoreCycle.MinimumWeeks || weekCount > request.Candidate.CoreCycle.MaximumWeeks)
        {
            throw new CatalogVolumeUnsupportedCycleLengthException(weekCount);
        }

        if (request.PeakVolumeBand.MinimumKm <= 0 || request.PeakVolumeBand.MaximumKm < request.PeakVolumeBand.MinimumKm)
        {
            throw new CatalogVolumeRuleInconsistentException("Peak-volume band minimum/maximum values are inconsistent.");
        }

        var bounds = new CatalogVolumeBounds(
            request.PeakVolumeBand.MinimumKm,
            request.PeakVolumeBand.MaximumKm,
            request.PeakVolumeBand.SourceArtifactKey,
            request.PeakVolumeBand.SourceArtifactVersion);

        var starting = ResolveStartingVolume(request.PrescriptionContext);
        var peak = ResolvePeak(starting.SelectedStartingVolumeKm, bounds, request.BoundPlan);
        var taper = ResolveTaperDecision();
        var share = ResolveLongRunWeeklyShareDecision();
        var weekly = BuildWeeklyPlan(request, bounds, starting, peak, taper);
        if (request.Candidate.DaysPerWeek == 3)
        {
            var projectedTaper = weekly.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm;
            // Phase 10K-GEN.23 -- GEN.21's frozen Option-1 authority: Beginner
            // gets its own, lower, taper-specific floor (8.5km, the new
            // 3.0+2.5+3.0 minima triple) instead of the Intermediate/
            // normal-week 12.0km floor. Intermediate's own gate is byte-
            // identical to before this phase.
            if (request.Candidate.Level == "NEW")
            {
                if (projectedTaper < V1BeginnerThreeDayVolumeEligibilityPolicy.MinimumFullLayoutTaperWeeklyVolumeKm)
                {
                    throw new BeginnerThreeDayCoreProductIneligibleException(projectedTaper);
                }
            }
            else if (projectedTaper < 12d)
            {
                throw new ThreeDayCoreProductIneligibleException(projectedTaper);
            }
        }
        if (request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 4)
        {
            var projectedTaper = weekly.Weeks.Single(w => w.IsTaperWeek).PlannedWeeklyVolumeKm;
            if (projectedTaper < V1BeginnerFourDayVolumeEligibilityPolicy.MinimumFullLayoutWeeklyVolumeKm)
            {
                throw new BeginnerFourDayCoreProductIneligibleException(projectedTaper);
            }
        }
        var longRun = BuildLongRunPlan(request, weekly, bounds, share);

        var weeklyValidation = CatalogVolumePlanValidator.ValidateWeeklyPlan(request.BoundPlan, weekly);
        var longRunValidation = CatalogVolumePlanValidator.ValidateLongRunPlan(request.BoundPlan, weekly, longRun);
        weekly = weekly with { ValidationResult = weeklyValidation };
        longRun = longRun with { ValidationResult = longRunValidation };

        if (!weeklyValidation.IsValid)
        {
            throw new CatalogVolumePlanInvalidException("Weekly volume plan failed validation: " + string.Join(", ", weeklyValidation.Errors));
        }

        if (!longRunValidation.IsValid)
        {
            throw new CatalogVolumePlanInvalidException("Long-run progression failed validation: " + string.Join(", ", longRunValidation.Errors));
        }

        return new CatalogVolumeAndLongRunPlan(weekly, longRun);
    }

    private StartingVolumeDecision ResolveStartingVolume(CatalogPlanPrescriptionContext context)
    {
        var readiness = context.PlanLevelReadiness;
        var reported = readiness.WeeklyVolume.Kilometers;

        if (readiness.WeeklyVolume.State == PrescriptionInputState.Invalid)
        {
            throw new CatalogVolumeInvalidReadinessInputException("RecentWeeklyVolumeKm is invalid; Phase 4F.7B.1 follows the fail-closed no-silent-fallback precedent.");
        }

        if (readiness.LongestRun.State == PrescriptionInputState.Invalid || readiness.RecentRace.State == PrescriptionInputState.Invalid)
        {
            throw new CatalogVolumeInvalidReadinessInputException("Readiness evidence is invalid; Phase 4F.7B.1 does not emit a numeric volume plan from contract-invalid readiness data.");
        }

        if (readiness.WeeklyVolume.State == PrescriptionInputState.Available && reported is > 0)
        {
            return new StartingVolumeDecision(
                reported,
                readiness.WeeklyVolume.State,
                Round(reported.Value),
                WeeklyVolumeAnchorSource.RecentFourWeekAverage,
                CatalogVolumeClamp.None,
                CatalogEvidenceBasis.EvidenceInformed,
                CatalogDecisionStatus.CanonicalConfirmed,
                "PHASE4F_7B1_CANONICAL_VOLUME_RULE_CORRECTION.md; Doc13 §3 / Golden Fixture v3 weeklyVolumeAnchorKm semantics");
        }

        // Phase 10K-GEN.9 -- GEN.8's frozen Advanced readiness authority:
        // Advanced never resolves a starting-volume default. Missing and
        // explicit-zero both fail closed here, before any per-Level default
        // resolver runs.
        if (ReferenceEquals(_policy, VolumeSafetyPolicy.Advanced3D) || ReferenceEquals(_policy, VolumeSafetyPolicy.Advanced4D) ||
            ReferenceEquals(_policy, VolumeSafetyPolicy.Advanced5D) || ReferenceEquals(_policy, VolumeSafetyPolicy.Advanced6D))
        {
            throw new AdvancedMissingOrZeroReadinessProductIneligibleException();
        }

        // Phase 10K-GEN.12 -- GEN.11's frozen 2D readiness authority: no
        // starting-volume default is ever resolved for 2D at either level.
        if (ReferenceEquals(_policy, VolumeSafetyPolicy.Beginner2D) || ReferenceEquals(_policy, VolumeSafetyPolicy.Intermediate2D))
        {
            throw new TwoDayMissingOrZeroReadinessProductIneligibleException();
        }

        // Phase 10K-GEN.24 -- user decision resolving GEN.23's own disclosed
        // gap: Beginner x3D remains SUPPORTED; only explicit-zero readiness
        // is PRODUCT_INELIGIBLE (mirroring GEN.9's Advanced
        // missing-or-zero pattern's mechanism class, but narrower -- only
        // the explicit-zero request shape is rejected here, not missing).
        // Missing readiness falls through unchanged to the same
        // V1BeginnerFourDayMissingReadinessStartingVolumePolicy reuse
        // GEN.23 already established below (12.0km default, unaffected).
        // No numeric value changed anywhere by this check.
        if (ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner) &&
            readiness.WeeklyVolume.State == PrescriptionInputState.Available && reported == 0)
        {
            throw new BeginnerThreeDayExplicitZeroReadinessProductIneligibleException();
        }

        // Phase 10K-GEN.23 -- Beginner x3D reuses Beginner x4D's own missing/
        // explicit-zero starting-volume defaults (12.0/9.5km) verbatim, the
        // same reuse GEN.5 already applied by hand to derive its own
        // matrices; no new Beginner x3D-specific starting-volume number is
        // introduced. (GEN.24: the explicit-zero branch is now unreachable
        // for ThreeDayBeginner specifically, intercepted above; missing
        // still reaches this dispatch unchanged.)
        return ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)
            ? V1ThreeDayMissingReadinessStartingVolumePolicy.Resolve(readiness)
            : ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner)
                ? V1BeginnerFourDayMissingReadinessStartingVolumePolicy.Resolve(readiness)
                : ReferenceEquals(_policy, VolumeSafetyPolicy.BeginnerFourDay)
                    ? V1BeginnerFourDayMissingReadinessStartingVolumePolicy.Resolve(readiness)
                    : ReferenceEquals(_policy, VolumeSafetyPolicy.FiveDayIntermediate)
                        ? V1FiveDayIntermediateMissingReadinessStartingVolumePolicy.Resolve(readiness)
                        : ReferenceEquals(_policy, VolumeSafetyPolicy.SixDayIntermediate)
                            ? V1SixDayIntermediateMissingReadinessStartingVolumePolicy.Resolve(readiness)
                            : V1MissingReadinessStartingVolumePolicy.Resolve(readiness);
    }

    /// <summary>
    /// Phase 10K-GEN.23 -- generalized from a single <c>ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayIntermediate)</c>
    /// check to also admit <see cref="VolumeSafetyPolicy.ThreeDayBeginner"/>,
    /// which reuses this exact sequential-growth mechanism unmodified (same
    /// 7%/8%/2.0km growth mechanics, GEN.2B.1/GEN.2B.3, no Level effect).
    /// Byte-identical behavior for every existing ThreeDayIntermediate caller.
    /// </summary>
    private static bool IsThreeDaySequentialGrowthPolicy(VolumeSafetyPolicy policy) =>
        ReferenceEquals(policy, VolumeSafetyPolicy.ThreeDayIntermediate) || ReferenceEquals(policy, VolumeSafetyPolicy.ThreeDayBeginner);

    private ReachablePeakDecision ResolvePeak(double startingVolumeKm, CatalogVolumeBounds bounds, BoundCatalogPlan boundPlan)
    {
        var nonTaperWeeks = boundPlan.Weeks.Count(w => w.PhaseKey != "TAPER");
        if (IsThreeDaySequentialGrowthPolicy(_policy))
        {
            var threeDayReachable = startingVolumeKm;
            for (var i = 1; i < nonTaperWeeks; i++)
            {
                var increase = Math.Min(threeDayReachable * _policy.PreferredMaxWeeklyIncreaseRatio, _policy.AbsoluteWeeklyIncrementCapKm);
                var candidate = Round(threeDayReachable + increase);
                while ((candidate - threeDayReachable) / threeDayReachable > _policy.HardMaxWeeklyIncreaseRatio)
                {
                    candidate = Round(candidate - _policy.RoundingIncrementKm);
                }
                threeDayReachable = Math.Min(candidate, bounds.MaximumKm);
            }
            var threeDayClassification = threeDayReachable < bounds.MinimumKm ? PeakBandClassification.BelowTypicalPeakButValid :
                threeDayReachable >= bounds.MaximumKm ? PeakBandClassification.UpperBoundConstrained : PeakBandClassification.WithinTypicalPeakBand;
            var threeDayProvenance = ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner)
                ? "TEN_K/NEW/3D GEN.23 sequential preferred growth (reuses GEN.2B.2's 3D mechanism unmodified); peak band is not mandatory."
                : "TEN_K/INTERMEDIATE/3D GEN.2B.2 sequential preferred growth; peak band is not mandatory.";
            return new ReachablePeakDecision(startingVolumeKm, threeDayReachable, threeDayReachable, threeDayClassification,
                _policy.PreferredMaxWeeklyIncreaseRatio, _policy.HardMaxWeeklyIncreaseRatio, _policy.AbsoluteWeeklyIncrementCapKm,
                CatalogEvidenceBasis.EvidenceInformed, CatalogDecisionStatus.ExplicitProductDefault,
                threeDayProvenance);
        }
        var transitions = Math.Max(0, nonTaperWeeks - 1);
        // Provenance is audit metadata only. Numeric planning deliberately consumes Value
        // and never branches on ResolvedPeakReference.Provenance.
        var canonicalDefaultMultiplier = _policy.ResolvedPeakReference.Value / _policy.GoldenFixtureStartingVolumeKm;
        var transitionAdjustedMultiplier = 1d + ((canonicalDefaultMultiplier - 1d) * transitions / _policy.GoldenFixtureNonTaperTransitions);
        var reachable = startingVolumeKm * transitionAdjustedMultiplier;

        reachable = Round(reachable);
        var selected = reachable < bounds.MinimumKm ? reachable : Round(Clamp(reachable, bounds.MinimumKm, bounds.MaximumKm));
        var classification = reachable < bounds.MinimumKm
            ? PeakBandClassification.BelowTypicalPeakButValid
            : reachable > bounds.MaximumKm
                ? PeakBandClassification.UpperBoundConstrained
                : PeakBandClassification.WithinTypicalPeakBand;

        if (selected < startingVolumeKm)
        {
            throw new CatalogVolumeUnreachablePeakRuleException("Resolved reachable peak is below starting volume.");
        }

        return new ReachablePeakDecision(
            startingVolumeKm,
            reachable,
            selected,
            classification,
            _policy.PreferredMaxWeeklyIncreaseRatio,
            _policy.HardMaxWeeklyIncreaseRatio,
            _policy.AbsoluteWeeklyIncrementCapKm,
            CatalogEvidenceBasis.EvidenceInformed,
            CatalogDecisionStatus.CanonicalConfirmed,
            "docs/canonical/golden-fixture-v3/progression_rules_v2.yaml profilePercentageCaps.INTERMEDIATE and absoluteWeeklyIncrementCapKm[4]; peak-volume band remains typical band only");
    }

    private TaperVolumeDecision ResolveTaperDecision()
    {
        var reduction = 1d - _policy.TaperVolumeMultiplier;
        if (reduction < 0.41d || reduction > 0.60d)
        {
            throw new CatalogVolumeInvalidTaperRuleException("Taper multiplier does not map to the accepted 41%-60% reduction range.");
        }

        return new TaperVolumeDecision(
            _policy.TaperVolumeMultiplier,
            Math.Round(reduction, 2, MidpointRounding.AwayFromZero),
            "41%-60% reduction",
            CatalogEvidenceBasis.EvidenceInformed,
            CatalogDecisionStatus.ExplicitProductDefault,
            "Golden Fixture v3 week 12 reduces from 38km to 20km (0.526 remaining); V1 default rounded to 0.53.");
    }

    private LongRunWeeklyShareDecision ResolveLongRunWeeklyShareDecision() => new(
        _policy.LongRunPreferredMinimumShare,
        _policy.LongRunPreferredMaximumShare,
        _policy.LongRunSelectionShare,
        _policy.LongRunHardCapShare,
        CatalogEvidenceBasis.ProductPracticeInformed,
        CatalogDecisionStatus.ExplicitProductDefault,
        "Doc13 §8.1 / Golden Fixture v3 four-day long-run practice: preferred 30%-36%, hard cap 40%; selection share 33%.");

    private CatalogWeeklyVolumePlan BuildWeeklyPlan(
        CatalogVolumePlanningRequest request,
        CatalogVolumeBounds bounds,
        StartingVolumeDecision starting,
        ReachablePeakDecision peak,
        TaperVolumeDecision taper)
    {
        var orderedWeeks = request.BoundPlan.Weeks.OrderBy(w => w.WeekNumber).ToList();
        var nonTaperWeeks = orderedWeeks.Where(w => w.PhaseKey != "TAPER").ToList();
        var records = new List<CatalogWeeklyVolumeWeek>();
        var traces = new List<WeeklyVolumeDecisionTrace>();

        double? previous = null;
        foreach (var week in orderedWeeks)
        {
            var isTaper = week.PhaseKey == "TAPER";
            var progressionStageKey = week.Sessions.FirstOrDefault(s => s.StructuralRole == "KEY_SESSION")?.ProgressionStageKey;
            double unclamped;
            CatalogVolumeClamp clamp;
            string changeRule;
            string recoveryRule;
            CatalogNumericRuleAuthority authority;

            if (week.WeekNumber == orderedWeeks[0].WeekNumber)
            {
                unclamped = starting.SelectedStartingVolumeKm;
                clamp = starting.AppliedClamp;
                changeRule = "first_week_readiness_anchor";
                recoveryRule = "none";
                authority = CatalogNumericRuleAuthority.RuntimeUserDerived;
            }
            else if (isTaper)
            {
                unclamped = (previous ?? starting.SelectedStartingVolumeKm) * taper.Multiplier;
                clamp = CatalogVolumeClamp.TaperReduction;
                changeRule = "canonical_taper_multiplier_0.53_from_previous_week";
                recoveryRule = "taper_only_not_recurring_deload";
                authority = CatalogNumericRuleAuthority.AcceptedProductDefault;
            }
            else
            {
                if (IsThreeDaySequentialGrowthPolicy(_policy))
                {
                    var reference = previous ?? starting.SelectedStartingVolumeKm;
                    unclamped = Math.Min(reference + Math.Min(reference * _policy.PreferredMaxWeeklyIncreaseRatio, _policy.AbsoluteWeeklyIncrementCapKm), peak.SelectedPeakKm);
                }
                else
                {
                    var index = nonTaperWeeks.FindIndex(w => w.WeekNumber == week.WeekNumber);
                    var denominator = Math.Max(1, nonTaperWeeks.Count - 1);
                    unclamped = starting.SelectedStartingVolumeKm + ((peak.SelectedPeakKm - starting.SelectedStartingVolumeKm) * index / denominator);
                }
                clamp = CatalogVolumeClamp.None;
                changeRule = "technical_linear_interpolation_from_start_to_selected_reachable_peak_across_non_taper_weeks";
                recoveryRule = "no_catalog_recurring_recovery_or_deload_rule_present";
                authority = CatalogNumericRuleAuthority.TechnicalDeterministicRule;
            }

            var selected = isTaper
                ? Math.Min(Round(unclamped), peak.SelectedPeakKm)
                : Round(Clamp(unclamped, Math.Min(starting.SelectedStartingVolumeKm, peak.SelectedPeakKm), peak.SelectedPeakKm));
            if (!isTaper && previous is > 0 && IsThreeDaySequentialGrowthPolicy(_policy))
            {
                while ((selected - previous.Value) / previous.Value > _policy.HardMaxWeeklyIncreaseRatio ||
                       selected - previous.Value > _policy.AbsoluteWeeklyIncrementCapKm)
                {
                    selected = Round(selected - _policy.RoundingIncrementKm);
                }
            }
            if (!isTaper && selected != Round(unclamped))
            {
                clamp = selected > Round(unclamped) ? CatalogVolumeClamp.LowerBound : CatalogVolumeClamp.UpperBound;
            }

            var changeKm = previous is null ? 0 : Round(selected - previous.Value);
            double? changePercent = previous is null or 0 ? null : RoundPercent(changeKm / previous.Value);
            var classification = isTaper ? "TAPER" :
                clamp != CatalogVolumeClamp.None ? "CLAMPED" :
                selected >= peak.SelectedPeakKm ? "PEAK" :
                changeKm > 0 ? "BUILDING" : "STEADY";

            records.Add(new CatalogWeeklyVolumeWeek
            {
                WeekNumber = week.WeekNumber,
                PhaseKey = week.PhaseKey,
                ProgressionStageKey = progressionStageKey,
                PlannedWeeklyVolumeKm = selected,
                PreviousWeekVolumeKm = previous,
                ChangeKm = changeKm,
                ChangePercent = changePercent,
                VolumeClassification = classification,
                IsRecoveryOrDeloadWeek = false,
                IsTaperWeek = isTaper,
                AnchorSource = starting.AnchorSource,
                CatalogBounds = bounds,
                AppliedClamp = clamp,
                DecisionReason = week.WeekNumber == orderedWeeks[0].WeekNumber ? "starting_volume_readiness_anchor_not_peak_band_floor" : changeRule,
                SourceArtifactKey = bounds.SourceArtifactKey,
                SourceArtifactVersion = bounds.SourceArtifactVersion,
                Provenance = $"{bounds.SourceArtifactKey} v{bounds.SourceArtifactVersion}; {request.Candidate.ProgressionModifier.Key} v{request.Candidate.ProgressionModifier.Version}"
            });

            traces.Add(new WeeklyVolumeDecisionTrace(
                week.WeekNumber,
                week.PhaseKey,
                request.PrescriptionContext.PlanLevelReadiness.WeeklyVolume.Kilometers,
                request.PrescriptionContext.PlanLevelReadiness.WeeklyVolume.State,
                starting.AnchorSource,
                Round(unclamped),
                bounds.MinimumKm,
                bounds.MaximumKm,
                clamp,
                peak.SelectedPeakKm,
                previous,
                changeRule,
                recoveryRule,
                _policy.RoundingRule,
                selected,
                authority));

            previous = selected;
        }

        return new CatalogWeeklyVolumePlan
        {
            CandidateKey = request.Candidate.CandidateKey,
            CandidateVersion = request.Candidate.CandidateVersion,
            FirstWeekVolumeKm = starting.SelectedStartingVolumeKm,
            PeakVolumeKm = peak.SelectedPeakKm,
            StartingVolumeDecision = starting,
            ReachablePeakDecision = peak,
            TaperVolumeDecision = taper,
            CatalogBounds = bounds,
            Weeks = records,
            DecisionTrace = traces,
            ValidationResult = new CatalogVolumeValidationResult(true, [])
        };
    }

    private CatalogLongRunProgression BuildLongRunPlan(
        CatalogVolumePlanningRequest request,
        CatalogWeeklyVolumePlan weekly,
        CatalogVolumeBounds weeklyBounds,
        LongRunWeeklyShareDecision share)
    {
        var readiness = request.PrescriptionContext.PlanLevelReadiness;
        var anchor = request.PrescriptionContext.LongRunAnchor;
        var records = new List<CatalogLongRunWeek>();
        var traces = new List<LongRunDecisionTrace>();
        double? previous = null;

        foreach (var week in weekly.Weeks)
        {
            var lowConfidence = readiness.LongestRun.State == PrescriptionInputState.Inconsistent;
            var weeklyVolume = week.PlannedWeeklyVolumeKm;
            // Phase 10K-GEN.23 -- narrow, taper-only long-run-share override
            // for Beginner x3D (see V1BeginnerThreeDayTaperLongRunSharePolicy's
            // own doc comment for the full arithmetic verification of why
            // this differs from the normal-week share used every other
            // week). Every other candidate/week is unaffected -- both
            // conditions must hold.
            var useBeginnerThreeDayTaperShare = week.IsTaperWeek && ReferenceEquals(_policy, VolumeSafetyPolicy.ThreeDayBeginner);
            var preferredMinShare = useBeginnerThreeDayTaperShare ? V1BeginnerThreeDayTaperLongRunSharePolicy.PreferredMinimumShare : share.PreferredMinimumShare;
            var preferredMaxShare = useBeginnerThreeDayTaperShare ? V1BeginnerThreeDayTaperLongRunSharePolicy.PreferredMaximumShare : share.PreferredMaximumShare;
            var selectionShare = useBeginnerThreeDayTaperShare ? V1BeginnerThreeDayTaperLongRunSharePolicy.SelectionShare : share.SelectionShare;
            var hardCapShare = useBeginnerThreeDayTaperShare ? V1BeginnerThreeDayTaperLongRunSharePolicy.HardCapShare : share.HardCapShare;
            var lower = Round(weeklyVolume * preferredMinShare);
            var upper = Round(weeklyVolume * preferredMaxShare);
            var hardCap = Round(weeklyVolume * hardCapShare);
            var target = Round(weeklyVolume * selectionShare);
            var unclamped = target;
            var clamp = CatalogVolumeClamp.None;
            var reason = useBeginnerThreeDayTaperShare ? "beginner_three_day_taper_specific_long_run_share_override_gen23" : "weekly_volume_derived_long_run_share";
            var authority = CatalogNumericRuleAuthority.TechnicalDeterministicRule;

            if (week.WeekNumber == weekly.Weeks[0].WeekNumber &&
                anchor.Source == LongRunAnchorSource.Recent30DayLongestRun &&
                readiness.LongestRun.State == PrescriptionInputState.Available &&
                readiness.LongestRun.Kilometers is > 0)
            {
                unclamped = Math.Min(readiness.LongestRun.Kilometers.Value, target);
                reason = "recent_longest_run_anchor_reconciled_below_or_equal_reported_maximum";
                authority = CatalogNumericRuleAuthority.RuntimeUserDerived;
            }

            if (lowConfidence)
            {
                unclamped = target;
                clamp = CatalogVolumeClamp.ConservativeClamp;
                reason = "LOW_CONFIDENCE_CONSERVATIVE_CLAMP";
                authority = CatalogNumericRuleAuthority.Inconsistent;
            }

            var selected = Round(Clamp(unclamped, lower, Math.Min(upper, hardCap)));
            if (selected != Round(unclamped) && clamp == CatalogVolumeClamp.None)
            {
                clamp = selected > Round(unclamped) ? CatalogVolumeClamp.LowerBound : CatalogVolumeClamp.UpperBound;
            }

            if (selected - hardCap > 0.001)
            {
                throw new CatalogLongRunHardCapViolationException($"Week {week.WeekNumber} long run exceeds the four-day hard cap.");
            }

            var changeKm = previous is null ? 0 : Round(selected - previous.Value);
            double? changePercent = previous is null or 0 ? null : RoundPercent(changeKm / previous.Value);
            records.Add(new CatalogLongRunWeek
            {
                WeekNumber = week.WeekNumber,
                PhaseKey = week.PhaseKey,
                PlannedLongRunDistanceKm = selected,
                PlannedWeeklyVolumeKm = weeklyVolume,
                LongRunShareOfWeeklyVolume = RoundPercent(selected / weeklyVolume),
                LongRunAnchorSource = lowConfidence ? LongRunAnchorSource.WeeklyVolumeDerived : anchor.Source,
                RecentLongestRunState = readiness.LongestRun.State,
                CompatibilityClamp = clamp,
                CatalogBounds = new CatalogVolumeBounds(lower, hardCap, weeklyBounds.SourceArtifactKey, weeklyBounds.SourceArtifactVersion),
                ChangeFromPreviousWeekKm = changeKm,
                ChangeFromPreviousWeekPercent = changePercent,
                DecisionReason = reason,
                SourceArtifactKey = weeklyBounds.SourceArtifactKey,
                SourceArtifactVersion = weeklyBounds.SourceArtifactVersion,
                Provenance = "four_day_long_run_preferred_share_30_to_36_percent_hard_cap_40_percent"
            });

            traces.Add(new LongRunDecisionTrace(
                week.WeekNumber,
                week.PhaseKey,
                readiness.LongestRun.Kilometers,
                readiness.LongestRun.State,
                lowConfidence ? LongRunAnchorSource.WeeklyVolumeDerived : anchor.Source,
                lowConfidence,
                "long_run_prefers_30_to_36_percent_and_must_not_exceed_40_percent_of_weekly_volume",
                lower,
                hardCap,
                clamp,
                previous,
                "select_33_percent_within_four_day_preferred_range",
                week.IsTaperWeek ? "taper_follows_reduced_weekly_volume_envelope" : "not_taper",
                _policy.RoundingRule,
                selected,
                authority));

            previous = selected;
        }

        return new CatalogLongRunProgression
        {
            CandidateKey = request.Candidate.CandidateKey,
            CandidateVersion = request.Candidate.CandidateVersion,
            Weeks = records,
            DecisionTrace = traces,
            ValidationResult = new CatalogVolumeValidationResult(true, []),
            WeeklyShareDecision = share
        };
    }

    private static double Clamp(double value, double min, double max) => Math.Min(max, Math.Max(min, value));

    private double Round(double value) => Round(value, _policy.RoundingIncrementKm);

    private static double Round(double value, double roundingIncrementKm) =>
        Math.Round(value / roundingIncrementKm, MidpointRounding.AwayFromZero) * roundingIncrementKm;

    private static double RoundPercent(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    internal LongRunCompatibilityClass ClassifyLongRunCompatibility(NormalizedRunningReadiness readiness)
    {
        if (readiness.LongestRun.State == PrescriptionInputState.Inconsistent)
        {
            return LongRunCompatibilityClass.Inconsistent;
        }

        if (readiness.WeeklyVolume.Kilometers is not > 0 || readiness.LongestRun.Kilometers is not > 0)
        {
            return LongRunCompatibilityClass.Missing;
        }

        var ratio = readiness.LongestRun.Kilometers.Value / readiness.WeeklyVolume.Kilometers.Value;
        return ratio switch
        {
            _ when ratio <= _policy.LongRunPreferredMaximumShare => LongRunCompatibilityClass.Balanced,
            _ when ratio <= _policy.LongRunHardCapShare => LongRunCompatibilityClass.Acceptable,
            _ => LongRunCompatibilityClass.HighShare
        };
    }
}
