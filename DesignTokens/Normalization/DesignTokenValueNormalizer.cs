using System.Globalization;
using System.Text.Json;
using Site.DesignTokens.Models;
using Site.DesignTokens.Sources;
using Site.DesignTokens.Values;

namespace Site.DesignTokens.Normalization;

public sealed class DesignTokenValueNormalizer : IDesignTokenValueNormalizer
{
    public DesignTokenNormalizationResult Normalize(DesignTokenRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var normalizedRegistry = new DesignTokenRegistry();
        var errors = new List<DesignTokenNormalizationError>();

        foreach (var token in registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            if (!TryNormalizeToken(token, out var normalizedValue, out var errorMessage))
            {
                errors.Add(new DesignTokenNormalizationError(token.Path.Value, errorMessage));
                continue;
            }

            normalizedRegistry.Add(new DesignToken(
                token.Path,
                token.Type,
                rawValue: token.RawValue,
                normalizedValue: normalizedValue,
                description: token.Description,
                sourceType: token.SourceType,
                sourceName: token.SourceName,
                sourcePriority: token.SourcePriority));
        }

        return new DesignTokenNormalizationResult(normalizedRegistry, errors);
    }

    private static bool TryNormalizeToken(
        DesignToken token,
        out object normalizedValue,
        out string errorMessage)
    {
        switch (token.Type)
        {
            case DesignTokenType.Color:
                if (TryGetString(token.RawValue, out var colorValue))
                {
                    normalizedValue = new ColorTokenValue { Value = colorValue };
                    errorMessage = string.Empty;
                    return true;
                }

                normalizedValue = null!;
                errorMessage = "Value cannot be converted to color.";
                return false;

            case DesignTokenType.Dimension:
                if (TryNormalizeDimensionToken(token.RawValue, out var dimensionValue, out errorMessage))
                {
                    normalizedValue = dimensionValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            case DesignTokenType.Typography:
                if (TryNormalizeTypographyToken(token.RawValue, out var typographyValue, out errorMessage))
                {
                    normalizedValue = typographyValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            case DesignTokenType.FontFamily:
                if (TryGetString(token.RawValue, out var fontFamilyValue))
                {
                    normalizedValue = new FontFamilyTokenValue { Value = fontFamilyValue };
                    errorMessage = string.Empty;
                    return true;
                }

                normalizedValue = null!;
                errorMessage = "Value cannot be converted to fontFamily.";
                return false;

            case DesignTokenType.FontWeight:
                if (TryNormalizeFontWeightToken(token.RawValue, out var fontWeightValue, out errorMessage))
                {
                    normalizedValue = fontWeightValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            case DesignTokenType.Shadow:
                if (TryNormalizeShadowToken(token.RawValue, out var shadowValue, out errorMessage))
                {
                    normalizedValue = shadowValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            case DesignTokenType.Border:
                if (TryNormalizeBorderToken(token.RawValue, out var borderValue, out errorMessage))
                {
                    normalizedValue = borderValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            case DesignTokenType.Duration:
                if (TryNormalizeDurationToken(token.RawValue, out var durationValue, out errorMessage))
                {
                    normalizedValue = durationValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            case DesignTokenType.Number:
                if (TryNormalizeNumberToken(token.RawValue, out var numberTokenValue, out errorMessage))
                {
                    normalizedValue = numberTokenValue;
                    return true;
                }

                normalizedValue = null!;
                return false;

            default:
                normalizedValue = null!;
                errorMessage = $"Unsupported token type '{token.Type}'.";
                return false;
        }
    }

    private static bool TryNormalizeDimensionToken(
        object? rawValue,
        out object normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new DimensionTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetObjectElement(rawValue, out var valueObject))
        {
            errorMessage = "Value for 'dimension' is not an object.";
            return false;
        }

        var hasStaticShape = valueObject.TryGetProperty("value", out _);
        var hasResponsiveShape =
            valueObject.TryGetProperty("mobile", out _) ||
            valueObject.TryGetProperty("tablet", out _) ||
            valueObject.TryGetProperty("desktop", out _);

        if (hasStaticShape && hasResponsiveShape)
        {
            errorMessage = "Dimension token cannot mix static and responsive shapes.";
            return false;
        }

        if (hasResponsiveShape)
        {
            if (!TryNormalizeResponsiveDimensionToken(valueObject, out var responsiveValue, out errorMessage))
            {
                return false;
            }

            normalizedValue = responsiveValue;
            return true;
        }

        if (!TryGetDimensionValue(rawValue, "dimension", allowUnitlessZero: true, out var dimensionValue, out errorMessage))
        {
            return false;
        }

        normalizedValue = new DimensionTokenValue
        {
            Value = dimensionValue.Value,
            Unit = dimensionValue.Unit
        };

        return true;
    }

    private static bool TryNormalizeResponsiveDimensionToken(
        JsonElement valueObject,
        out ResponsiveDimensionTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        foreach (var property in valueObject.EnumerateObject())
        {
            if (!string.Equals(property.Name, "mobile", StringComparison.Ordinal) &&
                !string.Equals(property.Name, "tablet", StringComparison.Ordinal) &&
                !string.Equals(property.Name, "desktop", StringComparison.Ordinal))
            {
                errorMessage = $"Unsupported responsive breakpoint key '{property.Name}'.";
                return false;
            }
        }

        if (!TryGetResponsiveDimensionBreakpoint(valueObject, "mobile", out var mobile, out var mobileReference, out errorMessage) ||
            !TryGetResponsiveDimensionBreakpoint(valueObject, "tablet", out var tablet, out var tabletReference, out errorMessage) ||
            !TryGetResponsiveDimensionBreakpoint(valueObject, "desktop", out var desktop, out var desktopReference, out errorMessage))
        {
            return false;
        }

        normalizedValue = new ResponsiveDimensionTokenValue
        {
            Mobile = mobile,
            MobileReference = mobileReference,
            Tablet = tablet,
            TabletReference = tabletReference,
            Desktop = desktop,
            DesktopReference = desktopReference
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeDurationToken(
        object? rawValue,
        out DurationTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new DurationTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetDimensionValue(rawValue, "duration", allowUnitlessZero: false, out var durationValue, out errorMessage))
        {
            return false;
        }

        normalizedValue = new DurationTokenValue
        {
            Value = durationValue.Value,
            Unit = durationValue.Unit
        };

        return true;
    }

    private static bool TryNormalizeFontWeightToken(
        object? rawValue,
        out FontWeightTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new FontWeightTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetInt32(rawValue, out var fontWeight))
        {
            errorMessage = "fontWeight is not numeric.";
            return false;
        }

        normalizedValue = new FontWeightTokenValue
        {
            Value = fontWeight
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeNumberToken(
        object? rawValue,
        out NumberTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new NumberTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetDecimal(rawValue, out var numberValue))
        {
            errorMessage = "number is not numeric.";
            return false;
        }

        normalizedValue = new NumberTokenValue
        {
            Value = numberValue
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeTypographyToken(
        object? rawValue,
        out TypographyTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new TypographyTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetObjectElement(rawValue, out var valueObject))
        {
            errorMessage = "Composite value is not an object.";
            return false;
        }

        if (!TryGetOptionalString(valueObject, "fontFamily", out var fontFamily, out errorMessage) ||
            !TryGetOptionalFontWeight(valueObject, "fontWeight", out var fontWeight, out var fontWeightReference, out errorMessage) ||
            !TryGetOptionalNumberOrReference(valueObject, "lineHeight", out var lineHeight, out var lineHeightReference, out errorMessage) ||
            !TryGetOptionalDimensionOrReference(valueObject, "fontSize", out var fontSize, out var fontSizeReference, out errorMessage) ||
            !TryGetOptionalDimensionOrReference(valueObject, "letterSpacing", out var letterSpacing, out var letterSpacingReference, out errorMessage))
        {
            return false;
        }

        normalizedValue = new TypographyTokenValue
        {
            FontFamily = fontFamily,
            FontWeight = fontWeight,
            FontWeightReference = fontWeightReference,
            FontSize = fontSize,
            FontSizeReference = fontSizeReference,
            LineHeight = lineHeight,
            LineHeightReference = lineHeightReference,
            LetterSpacing = letterSpacing,
            LetterSpacingReference = letterSpacingReference
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeShadowToken(
        object? rawValue,
        out ShadowTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new ShadowTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetObjectElement(rawValue, out var valueObject))
        {
            errorMessage = "Composite value is not an object.";
            return false;
        }

        if (!TryGetOptionalString(valueObject, "color", out var color, out errorMessage) ||
            !TryGetOptionalDimensionOrReference(valueObject, "offsetX", out var offsetX, out var offsetXReference, out errorMessage) ||
            !TryGetOptionalDimensionOrReference(valueObject, "offsetY", out var offsetY, out var offsetYReference, out errorMessage) ||
            !TryGetOptionalDimensionOrReference(valueObject, "blur", out var blur, out var blurReference, out errorMessage) ||
            !TryGetOptionalDimensionOrReference(valueObject, "spread", out var spread, out var spreadReference, out errorMessage))
        {
            return false;
        }

        normalizedValue = new ShadowTokenValue
        {
            Color = color,
            OffsetX = offsetX,
            OffsetXReference = offsetXReference,
            OffsetY = offsetY,
            OffsetYReference = offsetYReference,
            Blur = blur,
            BlurReference = blurReference,
            Spread = spread,
            SpreadReference = spreadReference
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryNormalizeBorderToken(
        object? rawValue,
        out BorderTokenValue normalizedValue,
        out string errorMessage)
    {
        normalizedValue = null!;

        if (TryGetString(rawValue, out var referenceValue))
        {
            normalizedValue = new BorderTokenValue
            {
                Reference = referenceValue
            };

            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetObjectElement(rawValue, out var valueObject))
        {
            errorMessage = "Composite value is not an object.";
            return false;
        }

        if (!TryGetOptionalDimensionOrReference(valueObject, "width", out var width, out var widthReference, out errorMessage) ||
            !TryGetOptionalString(valueObject, "style", out var style, out errorMessage) ||
            !TryGetOptionalString(valueObject, "color", out var color, out errorMessage))
        {
            return false;
        }

        normalizedValue = new BorderTokenValue
        {
            Width = width,
            WidthReference = widthReference,
            Style = style,
            Color = color
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetOptionalString(
        JsonElement element,
        string propertyName,
        out string? value,
        out string errorMessage)
    {
        value = null;

        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.Null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind != JsonValueKind.String)
        {
            errorMessage = $"Value cannot be converted to string for '{propertyName}'.";
            return false;
        }

        value = propertyValue.GetString();
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetOptionalNumberOrReference(
        JsonElement element,
        string propertyName,
        out decimal? value,
        out string? reference,
        out string errorMessage)
    {
        value = null;
        reference = null;

        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.Null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.String)
        {
            reference = propertyValue.GetString();
            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetDecimal(propertyValue, out var decimalValue))
        {
            errorMessage = $"Value cannot be converted to number for '{propertyName}'.";
            return false;
        }

        value = decimalValue;
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetOptionalFontWeight(
        JsonElement element,
        string propertyName,
        out int? value,
        out string? reference,
        out string errorMessage)
    {
        value = null;
        reference = null;

        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.Null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.String)
        {
            reference = propertyValue.GetString();
            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetInt32(propertyValue, out var intValue))
        {
            errorMessage = $"Value cannot be converted to fontWeight for '{propertyName}'.";
            return false;
        }

        value = intValue;
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetOptionalDimensionOrReference(
        JsonElement element,
        string propertyName,
        out DimensionValue? value,
        out string? reference,
        out string errorMessage)
    {
        value = null;
        reference = null;

        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.Null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.String)
        {
            reference = propertyValue.GetString();
            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetDimensionValue(propertyValue, propertyName, allowUnitlessZero: true, out var dimensionValue, out errorMessage))
        {
            errorMessage = $"Nested dimension value has invalid shape for '{propertyName}': {errorMessage}";
            return false;
        }

        value = dimensionValue;
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetResponsiveDimensionBreakpoint(
        JsonElement element,
        string propertyName,
        out DimensionValue? value,
        out string? reference,
        out string errorMessage)
    {
        value = null;
        reference = null;

        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.Null)
        {
            errorMessage = string.Empty;
            return true;
        }

        if (propertyValue.ValueKind == JsonValueKind.String)
        {
            reference = propertyValue.GetString();
            errorMessage = string.Empty;
            return true;
        }

        if (!TryGetDimensionValue(propertyValue, propertyName, allowUnitlessZero: true, out var dimensionValue, out errorMessage))
        {
            errorMessage = $"Nested dimension value has invalid shape for '{propertyName}': {errorMessage}";
            return false;
        }

        value = dimensionValue;
        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetDimensionValue(
        object? rawValue,
        string context,
        bool allowUnitlessZero,
        out DimensionValue value,
        out string errorMessage)
    {
        value = null!;

        if (!TryGetObjectElement(rawValue, out var valueObject))
        {
            errorMessage = $"Value for '{context}' is not an object.";
            return false;
        }

        if (!valueObject.TryGetProperty("value", out var numberElement))
        {
            errorMessage = $"'{context}' is missing value.";
            return false;
        }

        if (!TryGetDecimal(numberElement, out var decimalValue))
        {
            errorMessage = $"'{context}' value is not numeric.";
            return false;
        }

        if (!valueObject.TryGetProperty("unit", out var unitElement))
        {
            if (allowUnitlessZero && decimalValue == 0)
            {
                value = new DimensionValue
                {
                    Value = decimalValue,
                    Unit = string.Empty
                };

                errorMessage = string.Empty;
                return true;
            }

            errorMessage = $"'{context}' is missing unit.";
            return false;
        }

        if (unitElement.ValueKind != JsonValueKind.String)
        {
            errorMessage = $"'{context}' unit is not a string.";
            return false;
        }

        var unit = unitElement.GetString();
        if (string.IsNullOrWhiteSpace(unit))
        {
            if (allowUnitlessZero && decimalValue == 0)
            {
                value = new DimensionValue
                {
                    Value = decimalValue,
                    Unit = string.Empty
                };

                errorMessage = string.Empty;
                return true;
            }

            errorMessage = $"'{context}' is missing unit.";
            return false;
        }

        value = new DimensionValue
        {
            Value = decimalValue,
            Unit = unit
        };

        errorMessage = string.Empty;
        return true;
    }

    private static bool TryGetObjectElement(object? rawValue, out JsonElement element)
    {
        if (rawValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
        {
            element = jsonElement;
            return true;
        }

        element = default;
        return false;
    }

    private static bool TryGetString(object? rawValue, out string value)
    {
        switch (rawValue)
        {
            case string stringValue:
                value = stringValue;
                return true;
            case JsonElement { ValueKind: JsonValueKind.String } jsonElement:
                value = jsonElement.GetString() ?? string.Empty;
                return true;
            default:
                value = string.Empty;
                return false;
        }
    }

    private static bool TryGetInt32(object? rawValue, out int value)
    {
        switch (rawValue)
        {
            case int intValue:
                value = intValue;
                return true;
            case long longValue when longValue >= int.MinValue && longValue <= int.MaxValue:
                value = (int)longValue;
                return true;
            case decimal decimalValue when decimalValue >= int.MinValue && decimalValue <= int.MaxValue && decimal.Truncate(decimalValue) == decimalValue:
                value = decimal.ToInt32(decimalValue);
                return true;
            case JsonElement { ValueKind: JsonValueKind.Number } jsonElement when jsonElement.TryGetInt32(out var int32Value):
                value = int32Value;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static bool TryGetDecimal(object? rawValue, out decimal value)
    {
        switch (rawValue)
        {
            case decimal decimalValue:
                value = decimalValue;
                return true;
            case int intValue:
                value = intValue;
                return true;
            case long longValue:
                value = longValue;
                return true;
            case double doubleValue:
                value = Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture);
                return true;
            case float floatValue:
                value = Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture);
                return true;
            case JsonElement { ValueKind: JsonValueKind.Number } jsonElement when jsonElement.TryGetDecimal(out var decimalJsonValue):
                value = decimalJsonValue;
                return true;
            default:
                value = default;
                return false;
        }
    }
}
