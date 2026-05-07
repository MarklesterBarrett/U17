namespace Site.DesignTokens;

internal static class ColorTokenAlias
{
    public static string ToCanonical(string value)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.StartsWith("color-", StringComparison.OrdinalIgnoreCase)
            ? normalized.ToLowerInvariant()
            : $"color-{normalized.ToLowerInvariant()}";
    }

    public static string ToLegacy(string value)
    {
        var normalized = Normalize(value);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        return normalized.StartsWith("color-", StringComparison.OrdinalIgnoreCase)
            ? normalized["color-".Length..].ToLowerInvariant()
            : normalized.ToLowerInvariant();
    }

    private static string Normalize(string value)
    {
        return string.Join(
            "-",
            (value ?? string.Empty)
                .Trim()
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries));
    }
}
