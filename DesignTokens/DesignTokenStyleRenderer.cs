using Umbraco.Cms.Core.Models.PublishedContent;

namespace Site.DesignTokens;

public sealed class DesignTokenStyleRenderer : IDesignTokenStyleRenderer
{
    private static readonly IReadOnlyDictionary<string, StylePropertyDefinition> PropertyMap =
        new Dictionary<string, StylePropertyDefinition>(StringComparer.Ordinal)
        {
            ["backgroundColor"] = new("background-color", TokenValueKind.Color),
            ["textColor"] = new("color", TokenValueKind.Color),
            ["borderColor"] = new("border-color", TokenValueKind.Color),
            ["paddingInline"] = new("padding-inline", TokenValueKind.Spacing),
            ["paddingBlock"] = new("padding-block", TokenValueKind.Spacing),
            ["borderRadius"] = new("border-radius", TokenValueKind.Radius),
            ["columnGap"] = new("column-gap", TokenValueKind.Spacing),
            ["rowGap"] = new("row-gap", TokenValueKind.Spacing),
            ["justifyItems"] = new("justify-items", TokenValueKind.Raw),
            ["alignItems"] = new("align-items", TokenValueKind.Raw),
            ["justifyContent"] = new("justify-content", TokenValueKind.Raw),
            ["alignContent"] = new("align-content", TokenValueKind.Raw)
        };

    private readonly IDesignTokenProvider _designTokenProvider;

    public DesignTokenStyleRenderer(IDesignTokenProvider designTokenProvider)
    {
        _designTokenProvider = designTokenProvider;
    }

    public ElementStyleOverrides GetElementStyleOverrides(IPublishedElement? settings)
    {
        if (settings is null)
        {
            return new ElementStyleOverrides(null);
        }

        var tokenSet = _designTokenProvider.GetTokens();
        var colorTokens = tokenSet.Colors.ToDictionary(x => x.Alias, x => x.Value, StringComparer.OrdinalIgnoreCase);
        var spacingTokens = tokenSet.Spacing.Select(x => x.Alias).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var declarations = new List<string>();

        foreach (var property in PropertyMap)
        {
            var settingValue = settings.Value<string>(property.Key);
            var declaration = ResolveDeclaration(property.Value, settingValue, colorTokens, spacingTokens);

            if (!string.IsNullOrWhiteSpace(declaration))
            {
                declarations.Add(declaration);
            }
        }

        return new ElementStyleOverrides(CombineDeclarations(declarations.ToArray()));
    }

    private string? ResolveDeclaration(
        StylePropertyDefinition propertyDefinition,
        string? settingValue,
        IReadOnlyDictionary<string, string> colorTokens,
        IReadOnlySet<string> spacingTokens)
    {
        if (string.IsNullOrWhiteSpace(settingValue))
        {
            return null;
        }

        var resolvedValue = propertyDefinition.ValueKind switch
        {
            TokenValueKind.Color => ResolveTokenValue(colorTokens, settingValue),
            TokenValueKind.Spacing => ResolveSpacingTokenValue(spacingTokens, settingValue),
            TokenValueKind.Radius => ResolveRadiusValue(settingValue),
            TokenValueKind.Raw => settingValue,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(resolvedValue))
        {
            return null;
        }

        return $"{propertyDefinition.CssProperty}: {resolvedValue};";
    }

    private static string? ResolveTokenValue(IReadOnlyDictionary<string, string> tokens, string tokenAlias)
    {
        return tokens.TryGetValue(tokenAlias, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static string? ResolveSpacingTokenValue(IReadOnlySet<string> tokens, string tokenAlias)
    {
        if (string.Equals(tokenAlias, "none", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tokenAlias, "space-none", StringComparison.OrdinalIgnoreCase))
        {
            return "0";
        }

        return tokens.Contains(tokenAlias)
            ? $"var(--{tokenAlias})"
            : null;
    }

    private static string? ResolveRadiusValue(string radiusAlias)
    {
        return radiusAlias switch
        {
            "radius-none" => "0",
            "radius-sm" => "0.375rem",
            "radius-md" => "0.75rem",
            "radius-lg" => "1.25rem",
            "radius-full" => "999px",
            _ => radiusAlias
        };
    }

    private static string? CombineDeclarations(params string?[] declarations)
    {
        var values = declarations
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();

        return values.Count == 0 ? null : string.Join(" ", values);
    }

    private sealed record StylePropertyDefinition(string CssProperty, TokenValueKind ValueKind);

    private enum TokenValueKind
    {
        Raw,
        Color,
        Spacing,
        Radius
    }
}
