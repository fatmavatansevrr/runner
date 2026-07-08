using System.Text;
using System.Text.Json;

namespace PlanCatalog.Infrastructure.Serialization;

/// <summary>Converts PascalCase enum member names to UPPER_SNAKE_CASE — see brief §6.</summary>
public sealed class UpperSnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static readonly UpperSnakeCaseNamingPolicy Instance = new();

    public override string ConvertName(string name)
    {
        var builder = new StringBuilder(name.Length * 2);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                var previous = name[i - 1];
                var isNewWordStart = char.IsLower(previous) || char.IsDigit(previous) ||
                    (char.IsUpper(previous) && i + 1 < name.Length && char.IsLower(name[i + 1]));

                if (isNewWordStart)
                {
                    builder.Append('_');
                }
            }

            builder.Append(char.ToUpperInvariant(c));
        }

        return builder.ToString();
    }
}
