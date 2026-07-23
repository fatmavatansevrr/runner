using Microsoft.EntityFrameworkCore;
using RunningApp.Application.RuntimeCatalog.Schedule;
using RunningApp.Persistence;

namespace RunningApp.Application.RuntimeCatalog.PreviewRouting;

public sealed record CatalogPersistedPlanValidationResult(bool IsValid, IReadOnlyList<string> Errors);

public sealed class CatalogPersistedPlanValidator
{
    public async Task<CatalogPersistedPlanValidationResult> ValidateAsync(
        AppDbContext context,
        Guid previewId,
        CatalogPreviewSnapshot snapshot,
        CancellationToken ct = default)
    {
        var errors = new List<string>();
        var payload = snapshot.GeneratedPreviewPlanPayload;
        if (payload is null)
        {
            errors.Add("PayloadMissing");
            return new CatalogPersistedPlanValidationResult(false, errors);
        }

        var plan = await context.TrainingPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.SourcePreviewId == previewId, ct);

        if (plan is null)
        {
            errors.Add("TrainingPlanMissing");
            return new CatalogPersistedPlanValidationResult(false, errors);
        }

        if (plan.GenerationSource != GenerationSource.Catalog) errors.Add("GenerationSourceMismatch");
        if (plan.CatalogCandidateKey != snapshot.CandidateKey) errors.Add("CandidateKeyMismatch");
        if (plan.CatalogCandidateVersion != snapshot.CandidateVersion) errors.Add("CandidateVersionMismatch");
        if (plan.CatalogPreviewContentHash != snapshot.ContentHash) errors.Add("ContentHashMismatch");

        var weeks = await context.TrainingWeeks.AsNoTracking()
            .Where(w => w.PlanId == plan.Id)
            .OrderBy(w => w.WeekNumber)
            .ToListAsync(ct);

        if (weeks.Count != payload.Weeks.Count) errors.Add("WeekCountMismatch");
        if (!weeks.Select(w => w.WeekNumber).SequenceEqual(Enumerable.Range(1, weeks.Count))) errors.Add("WeekNumbersNotContiguous");

        var days = await context.TrainingDays.AsNoTracking()
            .Where(d => d.PlanId == plan.Id)
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        var expectedSessionCount = payload.Weeks.Sum(w => w.Sessions.Count);
        if (days.Count != expectedSessionCount) errors.Add("SessionCountMismatch");
        if (days.Any(d => d.GenerationSource != GenerationSource.Catalog)) errors.Add("DayGenerationSourceMismatch");
        if (days.Any(d => d.ActualDistanceKm != null || d.ActualDurationMin != null || d.ActualPaceMinKm != null || d.CompletedAt != null)) errors.Add("ActualFieldsPopulated");
        if (days.Any(d => string.IsNullOrWhiteSpace(d.CatalogPhaseKey))) errors.Add("MissingPhaseKey");
        if (days.Any(d => string.IsNullOrWhiteSpace(d.CatalogWorkoutDefinitionKey))) errors.Add("MissingWorkoutDefinitionKey");
        if (days.Any(d => string.IsNullOrWhiteSpace(d.CatalogStructuralRole))) errors.Add("MissingStructuralRole");
        if (days.Any(d => string.IsNullOrWhiteSpace(d.CatalogPrescriptionJson) || d.CatalogPrescriptionSchemaVersion != 1)) errors.Add("MissingPrescription");

        foreach (var weekPayload in payload.Weeks)
        {
            var week = weeks.SingleOrDefault(w => w.WeekNumber == weekPayload.WeekNumber);
            if (week is null) continue;
            if (Math.Abs(week.PlannedVolumeKm - weekPayload.PlannedVolumeKm) > 0.001) errors.Add($"Week{weekPayload.WeekNumber}VolumeMismatch");
            if (week.CatalogPhaseKey != weekPayload.Provenance.SourcePhaseKey) errors.Add($"Week{weekPayload.WeekNumber}PhaseMismatch");

            var weekDays = days.Where(d => d.WeekId == week.Id).ToList();
            if (weekDays.Count != weekPayload.Sessions.Count) errors.Add($"Week{weekPayload.WeekNumber}SessionCountMismatch");
        }

        var taperSharpen = days.SingleOrDefault(d => d.CatalogProgressionStageKey == "TAPER_SHARPEN");
        if (payload.Weeks.SelectMany(w => w.Sessions).Any(s => s.Provenance.SourceStageKey == "TAPER_SHARPEN"))
        {
            if (taperSharpen is null) errors.Add("TaperSharpenMissing");
            else
            {
                if (taperSharpen.CatalogPhaseKey != "TAPER") errors.Add("TaperSharpenPhaseMismatch");
                if (taperSharpen.CatalogStructuralRole != "KEY_SESSION") errors.Add("TaperSharpenRoleMismatch");
                if (taperSharpen.CatalogWorkoutDefinitionKey != "EASY_STANDARD") errors.Add("TaperSharpenWorkoutMismatch");
                if (!taperSharpen.CatalogPrescriptionJson!.Contains("controlled_sharpening", StringComparison.OrdinalIgnoreCase))
                    errors.Add("TaperSharpenControlledSharpeningMissing");
            }
        }

        return new CatalogPersistedPlanValidationResult(errors.Count == 0, errors);
    }
}
