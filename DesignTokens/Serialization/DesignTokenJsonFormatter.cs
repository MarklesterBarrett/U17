using System.Text.Json;
using System.Text.Json.Nodes;
using Site.DesignTokens.Models;
using Site.DesignTokens.Values;

namespace Site.DesignTokens.Serialization;

public sealed class DesignTokenJsonFormatter : IDesignTokenJsonFormatter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public string Format(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        using var document = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(JsonNode.Parse(document.RootElement.GetRawText()), SerializerOptions);
    }

    public string FormatRegistry(DesignTokenRegistry registry, bool useResolvedValues)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var root = new JsonObject();

        foreach (var token in registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            var current = root;
            for (var index = 0; index < token.Path.Segments.Count - 1; index++)
            {
                var segment = token.Path.Segments[index];
                if (current[segment] is not JsonObject child)
                {
                    child = new JsonObject();
                    current[segment] = child;
                }

                current = child;
            }

            var tokenNode = new JsonObject
            {
                ["$type"] = GetTypeName(token.Type),
                ["$value"] = SerializeTokenValue(token, useResolvedValues)
            };

            if (!string.IsNullOrWhiteSpace(token.Description))
            {
                tokenNode["$description"] = token.Description;
            }

            current[token.Name] = tokenNode;
        }

        return JsonSerializer.Serialize(root, SerializerOptions);
    }

    private static string GetTypeName(DesignTokenType type)
    {
        return type switch
        {
            DesignTokenType.Color => "color",
            DesignTokenType.Dimension => "dimension",
            DesignTokenType.Typography => "typography",
            DesignTokenType.FontFamily => "fontFamily",
            DesignTokenType.FontWeight => "fontWeight",
            DesignTokenType.Shadow => "shadow",
            DesignTokenType.Border => "border",
            DesignTokenType.Duration => "duration",
            DesignTokenType.Number => "number",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static JsonNode? SerializeTokenValue(DesignToken token, bool useResolvedValues)
    {
        var value = useResolvedValues
            ? token.ResolvedValue ?? token.NormalizedValue ?? token.RawValue
            : token.NormalizedValue ?? token.RawValue;

        return value switch
        {
            null => null,
            string stringValue => JsonValue.Create(stringValue),
            bool boolValue => JsonValue.Create(boolValue),
            int intValue => JsonValue.Create(intValue),
            long longValue => JsonValue.Create(longValue),
            decimal decimalValue => JsonValue.Create(decimalValue),
            JsonElement jsonElement => JsonNode.Parse(jsonElement.GetRawText()),
            ColorTokenValue color => JsonValue.Create(color.Value),
            FontFamilyTokenValue fontFamily => JsonValue.Create(fontFamily.Value),
            FontWeightTokenValue fontWeight => fontWeight.Reference is not null ? JsonValue.Create(fontWeight.Reference) : JsonValue.Create(fontWeight.Value),
            NumberTokenValue number => number.Reference is not null ? JsonValue.Create(number.Reference) : JsonValue.Create(number.Value),
            DurationTokenValue duration => duration.Reference is not null ? JsonValue.Create(duration.Reference) : SerializeDimension(duration.Value, duration.Unit),
            DimensionTokenValue dimension => dimension.Reference is not null ? JsonValue.Create(dimension.Reference) : SerializeDimension(dimension.Value, dimension.Unit),
            ResponsiveDimensionTokenValue responsive => SerializeResponsiveDimension(responsive),
            TypographyTokenValue typography => SerializeTypography(typography),
            ShadowTokenValue shadow => SerializeShadow(shadow),
            BorderTokenValue border => SerializeBorder(border),
            _ => throw new InvalidOperationException($"Unsupported token value type '{value.GetType().Name}'.")
        };
    }

    private static JsonNode SerializeResponsiveDimension(ResponsiveDimensionTokenValue value)
    {
        var json = new JsonObject();

        if (!string.IsNullOrWhiteSpace(value.MobileReference))
        {
            json["mobile"] = value.MobileReference;
        }
        else if (value.Mobile is not null)
        {
            json["mobile"] = SerializeDimensionValue(value.Mobile);
        }

        if (!string.IsNullOrWhiteSpace(value.TabletReference))
        {
            json["tablet"] = value.TabletReference;
        }
        else if (value.Tablet is not null)
        {
            json["tablet"] = SerializeDimensionValue(value.Tablet);
        }

        if (!string.IsNullOrWhiteSpace(value.DesktopReference))
        {
            json["desktop"] = value.DesktopReference;
        }
        else if (value.Desktop is not null)
        {
            json["desktop"] = SerializeDimensionValue(value.Desktop);
        }

        return json;
    }

    private static JsonNode SerializeTypography(TypographyTokenValue value)
    {
        if (!string.IsNullOrWhiteSpace(value.Reference))
        {
            return JsonValue.Create(value.Reference);
        }

        var json = new JsonObject();

        if (!string.IsNullOrWhiteSpace(value.FontFamily))
        {
            json["fontFamily"] = value.FontFamily;
        }

        if (!string.IsNullOrWhiteSpace(value.FontWeightReference))
        {
            json["fontWeight"] = value.FontWeightReference;
        }
        else if (value.FontWeight is not null)
        {
            json["fontWeight"] = value.FontWeight.Value;
        }

        if (!string.IsNullOrWhiteSpace(value.FontSizeReference))
        {
            json["fontSize"] = value.FontSizeReference;
        }
        else if (value.FontSize is not null)
        {
            json["fontSize"] = SerializeDimensionValue(value.FontSize);
        }

        if (!string.IsNullOrWhiteSpace(value.LineHeightReference))
        {
            json["lineHeight"] = value.LineHeightReference;
        }
        else if (value.LineHeight is not null)
        {
            json["lineHeight"] = value.LineHeight.Value;
        }

        if (!string.IsNullOrWhiteSpace(value.LetterSpacingReference))
        {
            json["letterSpacing"] = value.LetterSpacingReference;
        }
        else if (value.LetterSpacing is not null)
        {
            json["letterSpacing"] = SerializeDimensionValue(value.LetterSpacing);
        }

        return json;
    }

    private static JsonNode SerializeShadow(ShadowTokenValue value)
    {
        if (!string.IsNullOrWhiteSpace(value.Reference))
        {
            return JsonValue.Create(value.Reference);
        }

        var json = new JsonObject();

        if (!string.IsNullOrWhiteSpace(value.Color))
        {
            json["color"] = value.Color;
        }

        if (!string.IsNullOrWhiteSpace(value.OffsetXReference))
        {
            json["offsetX"] = value.OffsetXReference;
        }
        else if (value.OffsetX is not null)
        {
            json["offsetX"] = SerializeDimensionValue(value.OffsetX);
        }

        if (!string.IsNullOrWhiteSpace(value.OffsetYReference))
        {
            json["offsetY"] = value.OffsetYReference;
        }
        else if (value.OffsetY is not null)
        {
            json["offsetY"] = SerializeDimensionValue(value.OffsetY);
        }

        if (!string.IsNullOrWhiteSpace(value.BlurReference))
        {
            json["blur"] = value.BlurReference;
        }
        else if (value.Blur is not null)
        {
            json["blur"] = SerializeDimensionValue(value.Blur);
        }

        if (!string.IsNullOrWhiteSpace(value.SpreadReference))
        {
            json["spread"] = value.SpreadReference;
        }
        else if (value.Spread is not null)
        {
            json["spread"] = SerializeDimensionValue(value.Spread);
        }

        return json;
    }

    private static JsonNode SerializeBorder(BorderTokenValue value)
    {
        if (!string.IsNullOrWhiteSpace(value.Reference))
        {
            return JsonValue.Create(value.Reference);
        }

        var json = new JsonObject();

        if (!string.IsNullOrWhiteSpace(value.WidthReference))
        {
            json["width"] = value.WidthReference;
        }
        else if (value.Width is not null)
        {
            json["width"] = SerializeDimensionValue(value.Width);
        }

        if (!string.IsNullOrWhiteSpace(value.Style))
        {
            json["style"] = value.Style;
        }

        if (!string.IsNullOrWhiteSpace(value.Color))
        {
            json["color"] = value.Color;
        }

        return json;
    }

    private static JsonNode SerializeDimension(decimal? value, string unit)
    {
        return SerializeDimensionValue(new DimensionValue
        {
            Value = value ?? 0,
            Unit = unit
        });
    }

    private static JsonNode SerializeDimensionValue(DimensionValue value)
    {
        return new JsonObject
        {
            ["value"] = value.Value,
            ["unit"] = value.Unit
        };
    }
}
