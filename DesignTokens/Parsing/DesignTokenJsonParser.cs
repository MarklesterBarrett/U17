using System.Text.Json;
using Site.DesignTokens.Models;

namespace Site.DesignTokens.Parsing;

public sealed class DesignTokenJsonParser : IDesignTokenJsonParser
{
    private static readonly IReadOnlyDictionary<string, DesignTokenType> TypeMap =
        new Dictionary<string, DesignTokenType>(StringComparer.Ordinal)
        {
            ["color"] = DesignTokenType.Color,
            ["dimension"] = DesignTokenType.Dimension,
            ["typography"] = DesignTokenType.Typography,
            ["fontFamily"] = DesignTokenType.FontFamily,
            ["fontWeight"] = DesignTokenType.FontWeight,
            ["shadow"] = DesignTokenType.Shadow,
            ["border"] = DesignTokenType.Border,
            ["duration"] = DesignTokenType.Duration,
            ["number"] = DesignTokenType.Number
        };

    public DesignTokenParseResult Parse(string json)
    {
        var registry = new DesignTokenRegistry();
        var errors = new List<DesignTokenParseError>();

        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add(new DesignTokenParseError(string.Empty, "JSON is empty."));
            return new DesignTokenParseResult(registry, errors);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(new DesignTokenParseError(string.Empty, "Root JSON value must be an object."));
                return new DesignTokenParseResult(registry, errors);
            }

            WalkObject(document.RootElement, [], registry, errors);
        }
        catch (JsonException exception)
        {
            errors.Add(new DesignTokenParseError(string.Empty, $"Invalid JSON: {exception.Message}"));
        }

        return new DesignTokenParseResult(registry, errors);
    }

    private static void WalkObject(
        JsonElement element,
        IReadOnlyList<string> pathSegments,
        DesignTokenRegistry registry,
        List<DesignTokenParseError> errors)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (pathSegments.Count == 0 &&
                string.Equals(property.Name, "themes", StringComparison.Ordinal) &&
                property.Value.ValueKind == JsonValueKind.Object)
            {
                continue;
            }

            var childPath = pathSegments.Concat([property.Name]).ToArray();
            var childPathText = string.Join(".", childPath);

            if (string.IsNullOrWhiteSpace(property.Name))
            {
                errors.Add(new DesignTokenParseError(childPathText, "Path segment cannot be empty."));
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (IsTokenCandidate(property.Value))
            {
                ParseToken(property.Value, childPath, registry, errors);
                continue;
            }

            WalkObject(property.Value, childPath, registry, errors);
        }
    }

    private static bool IsTokenCandidate(JsonElement element)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.StartsWith('$'))
            {
                return true;
            }
        }

        return false;
    }

    private static void ParseToken(
        JsonElement element,
        IReadOnlyList<string> pathSegments,
        DesignTokenRegistry registry,
        List<DesignTokenParseError> errors)
    {
        var pathText = string.Join(".", pathSegments);

        if (!element.TryGetProperty("$type", out var typeElement))
        {
            errors.Add(new DesignTokenParseError(pathText, "Token is missing $type."));
            return;
        }

        if (!element.TryGetProperty("$value", out var valueElement))
        {
            errors.Add(new DesignTokenParseError(pathText, "Token is missing $value."));
            return;
        }

        if (typeElement.ValueKind != JsonValueKind.String)
        {
            errors.Add(new DesignTokenParseError(pathText, "Token $type must be a string."));
            return;
        }

        var typeName = typeElement.GetString();
        if (string.IsNullOrWhiteSpace(typeName) || !TypeMap.TryGetValue(typeName, out var tokenType))
        {
            errors.Add(new DesignTokenParseError(pathText, $"Unsupported token type '{typeName}'."));
            return;
        }

        DesignTokenPath tokenPath;
        try
        {
            tokenPath = new DesignTokenPath(pathSegments);
        }
        catch (ArgumentException exception)
        {
            errors.Add(new DesignTokenParseError(pathText, exception.Message));
            return;
        }

        var description = element.TryGetProperty("$description", out var descriptionElement) &&
                          descriptionElement.ValueKind == JsonValueKind.String
            ? descriptionElement.GetString()
            : null;

        var token = new DesignToken(
            tokenPath,
            tokenType,
            rawValue: ReadRawValue(valueElement),
            description: description);

        try
        {
            registry.Add(token);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(new DesignTokenParseError(pathText, exception.Message));
        }
    }

    private static object ReadRawValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt64(out var int64Value) => int64Value,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Null => element.Clone(),
            _ => element.Clone()
        };
    }
}
