using System.Text.Json;

namespace RunningApp.Application.Common;

/// <summary>
/// Converts an enum value to the same snake_case string the API's JSON
/// serializer would produce for it (Program.cs registers a global
/// JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)). Use this
/// anywhere an enum needs to become a plain string field on a DTO
/// (e.g. inside an anonymous/derived response) instead of calling
/// `.ToString().ToLower()`, which mangles multi-word enum names
/// (HalfMarathon -> "halfmarathon" instead of "half_marathon").
/// </summary>
public static class EnumSnakeCase
{
    public static string ToSnakeCase(Enum value) => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
