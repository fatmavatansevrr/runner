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
        // HM.2 Step 1c — HM.0 §F.3/§G Family 1: this branch previously checked
        // no distance field at all. Adding the explicit TEN_K check is
        // zero-delta (only TEN_K can reach this branch today, per the
        // identity gate) and closes the same latent defect the other
        // sibling occurrences in this class already had fixed for them.
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 4 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
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
        // HM.2 Step 1c — HM.0 §F.3/§G Family 1: distance check added, zero-delta (see above).
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "INTERMEDIATE" && request.Candidate.DaysPerWeek == 3 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.ThreeDayIntermediate).Build(request);
        }
        // Phase 10K-GEN.23 -- implements GEN.21's frozen Option-1 authority
        // (Phase K decision). Exact typed combination match only, mirroring
        // every other branch here -- never a broad "DaysPerWeek == 3"
        // condition, so this can never silently swallow a future
        // Advanced x3D candidate.
        // HM.2 Step 1c — HM.0 §F.3/§G Family 1: distance check added, zero-delta (see above).
        if (request.Candidate.CanonicalDistanceFamily == "TEN_K" && request.Candidate.Level == "NEW" && request.Candidate.DaysPerWeek == 3 && ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
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

        // HM.2 — exact dark-cell dispatch only; every other HM cell remains fail-closed.
        if (request.Candidate.CanonicalDistanceFamily == "HALF_MARATHON" &&
            request.Candidate.Level == "INTERMEDIATE" && request.Candidate.DaysPerWeek == 4 &&
            ReferenceEquals(_policy, VolumeSafetyPolicy.Default))
        {
            return new CatalogVolumeAndLongRunPlanner(VolumeSafetyPolicy.HalfMarathonIntermediate4D).Build(request);
        }

        // HM.2 Step 1c — closes HM.0 §F.3/§G Family 1's primary occurrence:
        // every branch above now explicitly requires CanonicalDistanceFamily
        // == "TEN_K", so any candidate reaching this point with the
        // unconfigured VolumeSafetyPolicy.Default (i.e. no per-distance
        // policy was ever selected for it) is, by construction, not TEN_K.
        // Before this fix, such a candidate (e.g. a future HALF_MARATHON
        // Intermediate×4D request, once the identity gate is ever widened
        // publicly) would silently fall through to the generic algorithm
        // below using VolumeSafetyPolicy.Default — TEN_K's own numeric
        // authority — producing a plausible-looking but scientifically wrong
        // plan with no error (HM.0 §O risk 1). Zero delta for TEN_K: the
        // only candidate that legitimately reaches here with _policy still
        // equal to Default is TEN_K/Intermediate/4D, which this guard lets
        // through unchanged (CanonicalDistanceFamily == "TEN_K", so the
        // condition below is false and control falls through exactly as
        // before).
        if (ReferenceEquals(_policy, VolumeSafetyPolicy.Default) && request.Candidate.CanonicalDistanceFamily != "TEN_K")
        {
            throw new CatalogVolumeUnsupportedDistanceFamilyException(request.Candidate.CanonicalDistanceFamily, request.Candidate.Level, request.Candidate.DaysPerWeek);
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
        var taperWeekCount = request.BoundPlan.Weeks.Count(w => w.PhaseKey == "TAPER");
        var (taper, taperMultipliers) = ResolveTaperDecision(taperWeekCount);
        var share = ResolveLongRunWeeklyShareDecision();
        var weekly = BuildWeeklyPlan(request, bounds, starting, peak, taper, taperMultipliers);
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

        // HM.1.5 explicitly froze 25km as calibration-only, never as a user
        // default. Until a separate HM missing/zero-readiness authority is
        // approved, fail closed instead of silently borrowing 10K's 24km.
        if (ReferenceEquals(_policy, VolumeSafetyPolicy.HalfMarathonIntermediate4D))
        {
            throw new CatalogVolumeInvalidReadinessInputException(
                "HALF_MARATHON Intermediate×4D requires positive observed RecentWeeklyVolumeKm; no approved missing/zero starting-volume fallback exists.");
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
        // HM.5.3 -- for a policy that opts in via ResolvedPeakReferenceIsSelectedCeiling (only
        // HalfMarathonIntermediate4D today), transitions are capped at the golden-fixture
        // calibration count before driving the interpolation ratio below.
        // GoldenFixtureNonTaperTransitions is a CALIBRATION POINT (how many non-taper
        // transitions the golden fixture itself has), not a floor on horizon length.
        // Interpolating for transitions <= this count is unchanged for every policy (including
        // HM's own 14W golden, which sits exactly at the calibration count). Once a horizon's
        // non-taper transition count EXCEEDS the calibration count (HM 15W/16W specifically),
        // the un-capped formula extrapolates the multiplier linearly past the calibrated ratio,
        // producing a resolved peak above ResolvedPeakReference.Value (46.5km for 16W, strictly
        // greater than the frozen 43.0km selected/reference peak) -- treating "more Core weeks"
        // as authority for "a higher peak", which HM.5.3's governing semantic explicitly
        // forbids for this cell (more weeks give more time to reach the SAME selected peak,
        // never a higher one). Capping the numerator here makes the multiplier saturate at
        // canonicalDefaultMultiplier for any horizon at or beyond the calibration point, so
        // `reachable` asymptotically approaches (and, when startingVolumeKm equals
        // GoldenFixtureStartingVolumeKm, resolves to exactly) ResolvedPeakReference.Value --
        // never above it. Every existing 10K policy leaves the flag false (default) and keeps
        // its own already-shipped extrapolation-past-calibration behavior for longer horizons
        // completely unchanged (confirmed by full regression -- e.g. BeginnerFourDay's 13W/14W
        // horizons intentionally resolve above their own 21km reference, which is real, already
        // shipped, already-tested product behavior this phase must not alter).
        var cappedTransitions = _policy.ResolvedPeakReferenceIsSelectedCeiling
            ? Math.Min(transitions, _policy.GoldenFixtureNonTaperTransitions)
            : transitions;
        // Provenance is audit metadata only. Numeric planning deliberately consumes Value
        // and never branches on ResolvedPeakReference.Provenance.
        var canonicalDefaultMultiplier = _policy.ResolvedPeakReference.Value / _policy.GoldenFixtureStartingVolumeKm;
        var transitionAdjustedMultiplier = 1d + ((canonicalDefaultMultiplier - 1d) * cappedTransitions / _policy.GoldenFixtureNonTaperTransitions);
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

    /// <summary>
    /// HM.1.4B — generalized from a single-scalar validation to a full
    /// ordered taper-multiplier-sequence validation, so a genuine multi-week
    /// Taper phase (e.g. HALF_MARATHON's frozen 2-week taper) can be
    /// represented and validated without silently permitting the
    /// compounding defect <c>HM.1.4A</c> found (43 → 22.8 → 12.1km, ≈72%
    /// cumulative reduction, confirmed by direct trace). Every existing 10K
    /// policy (<see cref="VolumeSafetyPolicy.TaperVolumeMultipliers"/> null,
    /// <see cref="VolumeSafetyPolicy.ResolvedTaperVolumeMultipliers"/>
    /// resolving to the single-element <c>[TaperVolumeMultiplier]</c>) takes
    /// the <c>multipliers.Count == 1</c> branch below, which is byte-identical
    /// to this method's pre-HM.1.4B body.
    /// </summary>
    private (TaperVolumeDecision Decision, IReadOnlyList<double> Multipliers) ResolveTaperDecision(int taperWeekCount)
    {
        var multipliers = _policy.ResolvedTaperVolumeMultipliers;
        if (taperWeekCount > 0 && multipliers.Count != taperWeekCount)
        {
            throw new CatalogVolumeInvalidTaperRuleException(
                $"Policy declares {multipliers.Count} taper multiplier(s) but the bound plan has {taperWeekCount} taper week(s); the two counts must match exactly so every taper week has its own, independently-applied multiplier.");
        }

        var finalReduction = 1d - multipliers[^1];
        if (finalReduction < 0.41d || finalReduction > 0.60d)
        {
            throw new CatalogVolumeInvalidTaperRuleException("Taper multiplier does not map to the accepted 41%-60% reduction range.");
        }

        // Every taper week before the final (deepest) one must itself be a
        // genuine, positive reduction from the fixed pre-taper reference,
        // strictly smaller than the final week's own reduction -- i.e. the
        // authored schedule must deepen monotonically toward race week.
        // Unreachable for every existing 10K policy (multipliers.Count == 1,
        // so this loop never executes).
        for (var i = 0; i < multipliers.Count - 1; i++)
        {
            var reduction = 1d - multipliers[i];
            if (reduction <= 0d || reduction >= finalReduction)
            {
                throw new CatalogVolumeInvalidTaperRuleException(
                    $"Taper week {i + 1} of {multipliers.Count}'s reduction ({reduction:P0}) must be a genuine, positive reduction strictly smaller than the final taper week's reduction ({finalReduction:P0}) -- taper reductions must deepen monotonically toward race week, never chained from another taper week's own output.");
            }
        }

        if (multipliers.Count == 1)
        {
            // Byte-identical to this method's pre-HM.1.4B body.
            return (new TaperVolumeDecision(
                multipliers[0],
                Math.Round(finalReduction, 2, MidpointRounding.AwayFromZero),
                "41%-60% reduction",
                CatalogEvidenceBasis.EvidenceInformed,
                CatalogDecisionStatus.ExplicitProductDefault,
                "Golden Fixture v3 week 12 reduces from 38km to 20km (0.526 remaining); V1 default rounded to 0.53."), multipliers);
        }

        return (new TaperVolumeDecision(
            multipliers[^1],
            Math.Round(finalReduction, 2, MidpointRounding.AwayFromZero),
            "41%-60% reduction (final/deepest taper week only; earlier taper weeks deepen monotonically toward it, each independently against the same fixed pre-taper reference)",
            CatalogEvidenceBasis.EvidenceInformed,
            CatalogDecisionStatus.ExplicitProductDefault,
            $"HM.1.4B non-chained {multipliers.Count}-week taper mechanism: each taper week applies its own multiplier independently against the fixed pre-taper reference, never chained from another taper week's own output. Multipliers (first-to-last): {string.Join(", ", multipliers)}."), multipliers);
    }

    private LongRunWeeklyShareDecision ResolveLongRunWeeklyShareDecision() => new(
        _policy.LongRunPreferredMinimumShare,
        _policy.LongRunPreferredMaximumShare,
        _policy.LongRunSelectionShare,
        _policy.LongRunHardCapShare,
        CatalogEvidenceBasis.ProductPracticeInformed,
        CatalogDecisionStatus.ExplicitProductDefault,
        "Doc13 §8.1 / Golden Fixture v3 four-day long-run practice: preferred 30%-36%, hard cap 40%; selection share 33%.",
        _policy.PreferredAbsolutePeakLongRunKm);

    private CatalogWeeklyVolumePlan BuildWeeklyPlan(
        CatalogVolumePlanningRequest request,
        CatalogVolumeBounds bounds,
        StartingVolumeDecision starting,
        ReachablePeakDecision peak,
        TaperVolumeDecision taper,
        IReadOnlyList<double> taperMultipliers)
    {
        var orderedWeeks = request.BoundPlan.Weeks.OrderBy(w => w.WeekNumber).ToList();
        var nonTaperWeeks = orderedWeeks.Where(w => w.PhaseKey != "TAPER").ToList();
        var records = new List<CatalogWeeklyVolumeWeek>();
        var traces = new List<WeeklyVolumeDecisionTrace>();

        double? previous = null;
        // HM.1.4B -- captured exactly once, the moment the taper phase is
        // entered (never reassigned to a taper week's own output). Every
        // taper week's target is computed independently against THIS SAME
        // fixed value -- no taper week's target ever depends on another
        // taper week's result. For a 1-week taper (every existing 10K
        // policy today), this anchor is used exactly once and equals
        // `previous` at that moment, so the resulting arithmetic is
        // byte-identical to the pre-HM.1.4B formula.
        double? preTaperAnchorKm = null;
        var taperPosition = 0;
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
                preTaperAnchorKm ??= previous ?? starting.SelectedStartingVolumeKm;
                var positionMultiplier = taperPosition < taperMultipliers.Count ? taperMultipliers[taperPosition] : taperMultipliers[^1];
                unclamped = preTaperAnchorKm.Value * positionMultiplier;
                clamp = CatalogVolumeClamp.TaperReduction;
                changeRule = taperMultipliers.Count == 1
                    ? "canonical_taper_multiplier_0.53_from_previous_week"
                    : $"hm_1_4b_non_chained_taper_multiplier_{positionMultiplier:0.####}_from_fixed_pre_taper_anchor_{preTaperAnchorKm.Value:0.####}km_week_{taperPosition + 1}_of_{taperMultipliers.Count}";
                recoveryRule = "taper_only_not_recurring_deload";
                authority = CatalogNumericRuleAuthority.AcceptedProductDefault;
                taperPosition++;
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

        // HM.1.6 -- ordered RACE_SPECIFIC week numbers, computed once. Only
        // ever consulted below when share.PreferredAbsolutePeakLongRunKm is
        // non-null (every existing 10K policy leaves it null), so this list's
        // mere non-emptiness (e.g. a future or existing 10K candidate that
        // also happens to use a "RACE_SPECIFIC" phase key) has zero effect on
        // its own -- the field, not the phase key's presence, is the gate.
        var raceSpecificWeekNumbers = weekly.Weeks.Where(w => w.PhaseKey == "RACE_SPECIFIC").Select(w => w.WeekNumber).ToList();

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

            // HM.1.6 -- RACE_SPECIFIC-phase peak-share eligibility. Gated
            // entirely on share.PreferredAbsolutePeakLongRunKm being
            // configured (null for every existing 10K policy -- see that
            // field's own doc comment for the full zero-delta argument).
            // When active, the target share ramps LINEARLY, by this week's
            // ordered position within the RACE_SPECIFIC phase, from the
            // ordinary SelectionShare (continuous with the immediately
            // preceding BUILD week -- no jump at the phase boundary) toward
            // HardCapShare (never past it -- reuses the two already-approved
            // share numbers, invents no third share figure) at the final
            // RACE_SPECIFIC week. This is an ALLOWANCE, not a mandate: the
            // absolute-kilometer ceiling below and the ordinary weekly-volume
            // progression (unchanged by this method) still jointly decide
            // whether the higher share ever produces a materially larger
            // long run.
            var isPeakEligibleWeek = !useBeginnerThreeDayTaperShare && share.PreferredAbsolutePeakLongRunKm is not null && week.PhaseKey == "RACE_SPECIFIC";
            if (isPeakEligibleWeek)
            {
                var position = raceSpecificWeekNumbers.IndexOf(week.WeekNumber);
                var denominator = Math.Max(1, raceSpecificWeekNumbers.Count - 1);
                selectionShare += (hardCapShare - selectionShare) * position / denominator;
            }

            var lower = Round(weeklyVolume * preferredMinShare);
            var upper = Round(weeklyVolume * (isPeakEligibleWeek ? hardCapShare : preferredMaxShare));
            // HM.5.3 -- a HARD SAFETY CEILING must round DOWN (never nearest/away-from-zero),
            // because rounding-to-nearest can round a compliant raw value UP past the true
            // percentage cap. Example (real 12W/15W reproduction): weeklyVolume=41.0,
            // hardCapShare=0.40 -> raw ceiling 16.4km; RoundToNearest(16.4, 0.5) = 16.5km, i.e.
            // rounding manufactured a ceiling ABOVE the true 40% limit. `selected` below was
            // then clamped up to that inflated 16.5km ceiling, which is genuinely > 40% of
            // weekly volume -- exactly what CatalogFinalPrescribedPlanValidator's own
            // (unrounded, 0.001km-tolerance) hard-cap check correctly rejected as
            // FINAL_WEEK_*_LONG_RUN_SHARE_EXCEEDS_CAP. The planner's internal
            // CatalogVolumePlanValidator never caught this because it validates `selected`
            // against this same (already-inflated) rounded ceiling, not the true percentage --
            // a planner/validator parity gap (both must honor the same true authority: the
            // raw hard-cap fraction, not a rounding of it that can exceed it). Rounding this
            // ceiling DOWN to the rounding grid instead guarantees the clamp ceiling is never
            // greater than the true fractional/absolute limit, so every value the planner
            // selects at or below it is, by construction, also accepted by the final
            // validator's own unrounded check. Zero-delta for every existing 10K week where
            // the nearest-rounded and floor-rounded hard cap already coincide (the common
            // case) -- this only ever lowers a ceiling that rounding-to-nearest had incorrectly
            // raised above the true limit.
            var hardCap = RoundDown(weeklyVolume * hardCapShare);
            // HM.1.6 -- an optional absolute ceiling additionally narrows the
            // effective hard cap in kilometers, never widens it. No-op
            // (hardCap unchanged) for every existing 10K policy, whose
            // PreferredAbsolutePeakLongRunKm is always null.
            if (share.PreferredAbsolutePeakLongRunKm is { } absoluteCeilingKm)
            {
                hardCap = Math.Min(hardCap, RoundDown(absoluteCeilingKm));
            }
            var target = Round(weeklyVolume * selectionShare);
            var unclamped = target;
            var clamp = CatalogVolumeClamp.None;
            var reason = useBeginnerThreeDayTaperShare
                ? "beginner_three_day_taper_specific_long_run_share_override_gen23"
                : isPeakEligibleWeek
                    ? "hm_1_6_race_specific_peak_share_progression_toward_hard_cap"
                    : "weekly_volume_derived_long_run_share";
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

    /// <summary>
    /// HM.5.3 -- safety-ceiling-only rounding: floors to the rounding grid instead of rounding
    /// to nearest, so a value used as a hard upper safety bound (e.g. the long-run
    /// hard-percentage-of-weekly-volume cap) can never be rounded UP past the true limit it
    /// represents. Never used for ordinary target/selection values (those still use the
    /// existing nearest-rounding <see cref="Round(double)"/>).
    /// </summary>
    private double RoundDown(double value) => Math.Floor(value / _policy.RoundingIncrementKm) * _policy.RoundingIncrementKm;

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
