namespace Site.DesignTokens.Themes;

public static class DesignTokenThemeVariantCatalog
{
    private static readonly ThemeVariantDefinition[] DefinitionsInternal =
    [
        new("default", "Default", ":root", DesignTokenThemeVariantType.Default, true),
        new("dark", "Dark", "[data-theme=\"dark\"]", DesignTokenThemeVariantType.Dark, false),
        new("highContrast", "High Contrast", "[data-theme=\"high-contrast\"]", DesignTokenThemeVariantType.HighContrast, false),
        new("ocean", "Ocean", "[data-theme=\"ocean\"]", DesignTokenThemeVariantType.Brand, false)
    ];

    public static IReadOnlyList<ThemeVariantDefinition> Definitions => DefinitionsInternal;

    public static bool TryGet(string? alias, out ThemeVariantDefinition definition)
    {
        var canonicalAlias = Canonicalize(alias);
        foreach (var item in DefinitionsInternal)
        {
            if (string.Equals(item.Alias, canonicalAlias, StringComparison.Ordinal))
            {
                definition = item;
                return true;
            }
        }

        definition = default;
        return false;
    }

    public static string? Canonicalize(string? alias)
    {
        var normalized = Normalize(alias);
        return normalized switch
        {
            "default" => "default",
            "dark" => "dark",
            "highcontrast" => "highContrast",
            "ocean" => "ocean",
            _ => null
        };
    }

    public static IReadOnlyList<string> GetEnabledAliases(IEnumerable<string>? aliases)
    {
        var enabled = new HashSet<string>(StringComparer.Ordinal)
        {
            "default"
        };

        if (aliases is null)
        {
            return enabled.ToArray();
        }

        foreach (var alias in aliases)
        {
            var canonicalAlias = Canonicalize(alias);
            if (!string.IsNullOrWhiteSpace(canonicalAlias))
            {
                enabled.Add(canonicalAlias);
            }
        }

        return enabled.ToArray();
    }

    private static string Normalize(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return string.Empty;
        }

        return new string(alias
            .Trim()
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    public readonly record struct ThemeVariantDefinition(
        string Alias,
        string Name,
        string Selector,
        DesignTokenThemeVariantType VariantType,
        bool IsDefault);
}
