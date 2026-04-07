using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
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

    private readonly Lazy<IReadOnlyDictionary<string, string>> _colorTokens;
    private readonly Lazy<IReadOnlyDictionary<string, string>> _spacingTokens;

    public DesignTokenStyleRenderer(IWebHostEnvironment environment)
    {
        var colorsPath = Path.Combine(environment.WebRootPath, "App_Plugins", "DesignTokens", "tokens", "colors.json");
        var spacingPath = Path.Combine(environment.WebRootPath, "App_Plugins", "DesignTokens", "tokens", "spacing.json");
        _colorTokens = new Lazy<IReadOnlyDictionary<string, string>>(() => LoadColorTokens(colorsPath));
        _spacingTokens = new Lazy<IReadOnlyDictionary<string, string>>(() => LoadSpacingTokens(spacingPath));
    }

    public ElementStyleOverrides GetElementStyleOverrides(IPublishedElement? settings)
    {
        if (settings is null)
        {
            return new ElementStyleOverrides(null);
        }

        var declarations = new List<string>();

        foreach (var property in PropertyMap)
        {
            var settingValue = settings.Value<string>(property.Key);
            var declaration = ResolveDeclaration(property.Value, settingValue);

            if (!string.IsNullOrWhiteSpace(declaration))
            {
                declarations.Add(declaration);
            }
        }

        return new ElementStyleOverrides(CombineDeclarations(declarations.ToArray()));
    }

    private string? ResolveDeclaration(StylePropertyDefinition propertyDefinition, string? settingValue)
    {
        if (string.IsNullOrWhiteSpace(settingValue))
        {
            return null;
        }

        var resolvedValue = propertyDefinition.ValueKind switch
        {
            TokenValueKind.Color => ResolveTokenValue(_colorTokens.Value, settingValue),
            TokenValueKind.Spacing => ResolveTokenValue(_spacingTokens.Value, settingValue),
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

    private static IReadOnlyDictionary<string, string> LoadColorTokens(string colorsPath)
    {
        if (!File.Exists(colorsPath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        using var stream = File.OpenRead(colorsPath);
        using var document = JsonDocument.Parse(stream);

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return tokens;
        }

        foreach (var property in document.RootElement.EnumerateObject())
        {
            var token = property.Value;

            if (!token.TryGetProperty("$type", out var typeElement) ||
                !string.Equals(typeElement.GetString(), "color", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!token.TryGetProperty("$value", out var valueElement))
            {
                continue;
            }

            var alias = TryGetAlias(token) ?? property.Name;
            var value = valueElement.GetString();

            if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            tokens[alias] = value;
        }

        return tokens;
    }

    private static IReadOnlyDictionary<string, string> LoadSpacingTokens(string spacingPath)
    {
        if (!File.Exists(spacingPath))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        using var stream = File.OpenRead(spacingPath);
        using var document = JsonDocument.Parse(stream);

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CollectSpacingTokens(document.RootElement, tokens);
        return tokens;
    }

    private static string? TryGetAlias(JsonElement token)
    {
        if (!token.TryGetProperty("$extensions", out var extensionsElement) ||
            !extensionsElement.TryGetProperty("site", out var siteElement) ||
            !siteElement.TryGetProperty("alias", out var aliasElement))
        {
            return null;
        }

        return aliasElement.GetString();
    }

    private static void CollectSpacingTokens(JsonElement element, IDictionary<string, string> tokens)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (element.TryGetProperty("$type", out var typeElement) &&
            string.Equals(typeElement.GetString(), "dimension", StringComparison.OrdinalIgnoreCase) &&
            element.TryGetProperty("$value", out var valueElement) &&
            valueElement.ValueKind == JsonValueKind.Object)
        {
            var alias = TryGetAlias(element);
            var resolvedValue =
                TryGetBreakpointValue(valueElement, "desktop") ??
                TryGetBreakpointValue(valueElement, "laptop") ??
                TryGetBreakpointValue(valueElement, "tablet") ??
                TryGetBreakpointValue(valueElement, "mobile");

            if (!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(resolvedValue))
            {
                tokens[alias] = resolvedValue;
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            CollectSpacingTokens(property.Value, tokens);
        }
    }

    private static string? TryGetBreakpointValue(JsonElement valueElement, string breakpoint)
    {
        return valueElement.TryGetProperty(breakpoint, out var breakpointElement)
            ? breakpointElement.GetString()
            : null;
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
