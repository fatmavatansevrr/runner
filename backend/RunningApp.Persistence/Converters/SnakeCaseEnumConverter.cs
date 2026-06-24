using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Text.Json;

namespace RunningApp.Persistence.Converters;

public class SnakeCaseEnumConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : struct, Enum
{
    public SnakeCaseEnumConverter() : base(
        v => ToSnakeCase(v),
        v => FromSnakeCase(v))
    {
    }

    private static string ToSnakeCase(TEnum value)
    {
        return JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
    }

    private static TEnum FromSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        if (Enum.TryParse<TEnum>(value, true, out var result))
        {
            return result;
        }

        // Try mapping snake_case to PascalCase
        foreach (var val in Enum.GetValues<TEnum>())
        {
            if (JsonNamingPolicy.SnakeCaseLower.ConvertName(val.ToString()).Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                return val;
            }
        }

        throw new ArgumentException($"Unknown enum value: {value} for type {typeof(TEnum).Name}");
    }
}
