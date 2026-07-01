namespace RunningApp.Application.Common;

public static class PaceFormatter
{
    /// <summary>Seconds/km → "5:30" min/km display string.</summary>
    public static string Format(double secondsPerKm)
    {
        var minutes = (int)(secondsPerKm / 60);
        var seconds = (int)(secondsPerKm % 60);
        return $"{minutes}:{seconds:D2}";
    }

    /// <summary>"5:30" min/km string → seconds/km double.</summary>
    public static double Parse(string pace)
    {
        var parts = pace.Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], out var minutes)
            || !int.TryParse(parts[1], out var seconds))
        {
            throw new FormatException($"Invalid pace format: '{pace}'. Expected 'M:SS'.");
        }
        return minutes * 60.0 + seconds;
    }
}
