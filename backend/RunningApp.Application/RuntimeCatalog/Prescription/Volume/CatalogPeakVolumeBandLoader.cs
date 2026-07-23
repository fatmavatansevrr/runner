using System.Text.Json;
using Microsoft.Extensions.Options;
using RunningApp.Application.Exceptions;

namespace RunningApp.Application.RuntimeCatalog.Prescription.Volume;

public interface ICatalogPeakVolumeBandLoader
{
    Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default);
}

public sealed class CatalogPeakVolumeBandLoader : ICatalogPeakVolumeBandLoader
{
    private readonly PlanCatalogOptions _options;

    public CatalogPeakVolumeBandLoader(IOptions<PlanCatalogOptions> options)
    {
        _options = options.Value;
    }

    public async Task<CatalogPeakVolumeBand> LoadAsync(PlanCatalogReference reference, string distanceFamily, string experience, int runsPerWeek, CancellationToken ct = default)
    {
        var path = Path.Combine(_options.CatalogRootPath, "policies", $"peak-volume-bands.v{reference.Version}.json");
        if (!File.Exists(path))
        {
            throw new PlanCatalogLoadException($"Peak-volume band policy '{reference.Key}' v{reference.Version} was not found at '{path}'.");
        }

        await using var stream = File.OpenRead(path);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var metadata = document.RootElement.GetProperty("metadata");
        var key = metadata.GetProperty("key").GetString();
        var version = metadata.GetProperty("version").GetInt32();
        if (key != reference.Key || version != reference.Version)
        {
            throw new PlanCatalogLoadException($"Peak-volume band policy identity mismatch. Expected {reference.Key} v{reference.Version}, found {key} v{version}.");
        }

        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            if (entry.GetProperty("distanceFamily").GetString() == distanceFamily &&
                entry.GetProperty("experience").GetString() == experience &&
                entry.GetProperty("runsPerWeek").GetInt32() == runsPerWeek)
            {
                return new CatalogPeakVolumeBand(
                    distanceFamily,
                    experience,
                    runsPerWeek,
                    entry.GetProperty("minimumKm").GetDouble(),
                    entry.GetProperty("maximumKm").GetDouble(),
                    reference.Key,
                    reference.Version);
            }
        }

        throw new PlanCatalogLoadException($"No peak-volume band entry exists for {distanceFamily}/{experience}/{runsPerWeek} in {reference.Key} v{reference.Version}.");
    }
}
