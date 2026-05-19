using System.Text.Json;
using System.Text.Json.Nodes;
using Site.DesignTokens.Css;
using Site.DesignTokens.Models;
using Site.DesignTokens.Values;

namespace Site.DesignTokens.Tailwind;

public sealed class DesignTokenTailwindExporter : IDesignTokenTailwindExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public DesignTokenTailwindExportResult Export(DesignTokenRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var errors = new List<DesignTokenTailwindExportError>();
        var extend = new JsonObject();

        foreach (var token in registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            if (!TryMapToken(token, extend, errors))
            {
                continue;
            }
        }

        if (errors.Count > 0)
        {
            return new DesignTokenTailwindExportResult(string.Empty, errors);
        }

        var root = new JsonObject
        {
            ["theme"] = new JsonObject
            {
                ["extend"] = extend
            }
        };

        return new DesignTokenTailwindExportResult(
            JsonSerializer.Serialize(root, SerializerOptions),
            errors);
    }

    private static bool TryMapToken(
        DesignToken token,
        JsonObject extend,
        List<DesignTokenTailwindExportError> errors)
    {
        if (token.ResolvedValue is null)
        {
            errors.Add(new DesignTokenTailwindExportError(token.Path.Value, "Token has no resolved value."));
            return false;
        }

        if (!DesignTokenCssVariableName.TryCreate(token, out var cssVariableName, out var cssNameError))
        {
            errors.Add(new DesignTokenTailwindExportError(token.Path.Value, cssNameError));
            return false;
        }

        var cssVariableReference = $"var({cssVariableName})";
        var segments = token.Path.Segments;

        switch (token.Type)
        {
            case DesignTokenType.Color when segments.Count >= 2 && IsResolved<ColorTokenValue>(token):
                AddNestedString(extend, "colors", segments.Skip(1).ToArray(), cssVariableReference);
                return true;

            case DesignTokenType.Dimension when segments.Count >= 2 && IsSpacingToken(segments) && IsResolvedDimension(token):
                AddNestedString(extend, "spacing", segments.Skip(1).ToArray(), cssVariableReference);
                return true;

            case DesignTokenType.FontFamily when segments.Count >= 3 && IsFontFamilyToken(segments) && IsResolved<FontFamilyTokenValue>(token):
                AddNestedArray(extend, "fontFamily", segments.Skip(2).ToArray(), [cssVariableReference]);
                return true;

            case DesignTokenType.FontWeight when segments.Count >= 3 && IsFontWeightToken(segments) && IsResolved<FontWeightTokenValue>(token):
                AddNestedString(extend, "fontWeight", segments.Skip(2).ToArray(), cssVariableReference);
                return true;

            case DesignTokenType.Duration when segments.Count >= 2 && IsResolved<DurationTokenValue>(token):
                AddNestedString(extend, "transitionDuration", segments.Skip(1).ToArray(), cssVariableReference);
                return true;

            case DesignTokenType.Number when segments.Count >= 2 && IsOpacityToken(segments) && IsResolved<NumberTokenValue>(token):
                AddNestedString(extend, "opacity", segments.Skip(1).ToArray(), cssVariableReference);
                return true;

            case DesignTokenType.Shadow when segments.Count >= 2 && IsResolved<ShadowTokenValue>(token):
                AddNestedString(extend, "boxShadow", segments.Skip(1).ToArray(), cssVariableReference);
                return true;

            case DesignTokenType.Typography when segments.Count >= 2 && token.ResolvedValue is TypographyTokenValue typography:
                AddTypographyEntry(extend, segments.Skip(1).ToArray(), segments, typography);
                return true;

            default:
                return true;
        }
    }

    private static void AddTypographyEntry(
        JsonObject extend,
        IReadOnlyList<string> targetSegments,
        IReadOnlyList<string> tokenSegments,
        TypographyTokenValue value)
    {
        var tokenBase = $"--{string.Join("-", tokenSegments.Select(ToKebabCase))}";
        var typographyArray = new JsonArray
        {
            $"var({tokenBase}-font-size)"
        };

        var options = new JsonObject
        {
            ["lineHeight"] = $"var({tokenBase}-line-height)"
        };

        if (value.LetterSpacing is not null)
        {
            options["letterSpacing"] = $"var({tokenBase}-letter-spacing)";
        }

        if (value.FontWeight is not null)
        {
            options["fontWeight"] = $"var({tokenBase}-font-weight)";
        }

        typographyArray.Add(options);
        AddNestedNode(extend, "fontSize", targetSegments, typographyArray);
    }

    private static bool IsResolved<TValue>(DesignToken token)
        where TValue : class =>
        token.ResolvedValue is TValue;

    private static bool IsResolvedDimension(DesignToken token) =>
        token.ResolvedValue is DimensionTokenValue or ResponsiveDimensionTokenValue;

    private static bool IsSpacingToken(IReadOnlyList<string> segments) =>
        segments.Count > 0 &&
        (string.Equals(segments[0], "space", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(segments[0], "spacing", StringComparison.OrdinalIgnoreCase));

    private static bool IsOpacityToken(IReadOnlyList<string> segments) =>
        segments.Count > 0 &&
        string.Equals(segments[0], "opacity", StringComparison.OrdinalIgnoreCase);

    private static bool IsFontFamilyToken(IReadOnlyList<string> segments) =>
        string.Equals(segments[0], "font", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(segments[1], "family", StringComparison.OrdinalIgnoreCase);

    private static bool IsFontWeightToken(IReadOnlyList<string> segments) =>
        string.Equals(segments[0], "font", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(segments[1], "weight", StringComparison.OrdinalIgnoreCase);

    private static void AddNestedString(JsonObject root, string themeKey, IReadOnlyList<string> pathSegments, string value) =>
        AddNestedNode(root, themeKey, pathSegments, JsonValue.Create(value));

    private static void AddNestedArray(JsonObject root, string themeKey, IReadOnlyList<string> pathSegments, IReadOnlyList<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        AddNestedNode(root, themeKey, pathSegments, array);
    }

    private static void AddNestedNode(JsonObject root, string themeKey, IReadOnlyList<string> pathSegments, JsonNode? node)
    {
        if (root[themeKey] is not JsonObject themeObject)
        {
            themeObject = new JsonObject();
            root[themeKey] = themeObject;
        }

        var current = themeObject;
        for (var index = 0; index < pathSegments.Count - 1; index++)
        {
            var key = pathSegments[index];
            if (current[key] is not JsonObject child)
            {
                child = new JsonObject();
                current[key] = child;
            }

            current = child;
        }

        current[pathSegments[^1]] = node;
    }

    private static string ToKebabCase(string value)
    {
        return string.Concat(value.Select((ch, index) =>
            index > 0 && char.IsUpper(ch)
                ? $"-{char.ToLowerInvariant(ch)}"
                : char.ToLowerInvariant(ch).ToString()));
    }
}
