using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Site.DesignTokens;

public sealed class DesignTokenStyleRenderer : IDesignTokenStyleRenderer
{
    private readonly Lazy<IReadOnlyDictionary<string, string>> _colorTokens;

    public DesignTokenStyleRenderer(IWebHostEnvironment environment)
    {
        var colorsPath = Path.Combine(environment.WebRootPath, "App_Plugins", "DesignTokens", "tokens", "colors.json");
        _colorTokens = new Lazy<IReadOnlyDictionary<string, string>>(() => LoadColorTokens(colorsPath));
    }

    public string? ResolveColorDeclaration(string cssProperty, string? tokenAlias)
    {
        if (string.IsNullOrWhiteSpace(cssProperty) || string.IsNullOrWhiteSpace(tokenAlias))
        {
            return null;
        }

        if (!_colorTokens.Value.TryGetValue(tokenAlias, out var colorValue) || string.IsNullOrWhiteSpace(colorValue))
        {
            return null;
        }

        return $"{cssProperty}: {colorValue};";
    }

    public string? CombineDeclarations(params string?[] declarations)
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
}
