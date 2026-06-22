using System.Text.RegularExpressions;

public static class MissionAuthoringValidation
{
    private static readonly Regex StableIdPattern = new Regex("^[a-z][a-z0-9_]*$", RegexOptions.Compiled);

    public static bool IsNullOrWhiteSpace(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    public static string CleanId(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    public static bool IsStableId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return StableIdPattern.IsMatch(value.Trim());
    }

    public static string BuildStableIdWarning(string fieldName, string value)
    {
        return $"{fieldName} usa '{value}'. Recomendado: snake_case estable, sin espacios ni mayúsculas. Ejemplo: main_find_hook.";
    }
}
