using System.Globalization;
using System.Text.RegularExpressions;
using Site.DesignTokens.Models;
using Site.DesignTokens.Values;

namespace Site.DesignTokens.Validation;

public sealed class DesignTokenValidator : IDesignTokenValidator
{
    private static readonly HashSet<string> DimensionUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "px", "rem", "em", "%", "vw", "vh", "vmin", "vmax", "ch", "ex"
    };

    private static readonly HashSet<string> DurationUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "ms", "s"
    };

    private static readonly HashSet<string> BorderStyles = new(StringComparer.OrdinalIgnoreCase)
    {
        "none", "hidden", "dotted", "dashed", "solid", "double", "groove", "ridge", "inset", "outset"
    };

    private static readonly Regex ReferencePattern = new(@"^\{[A-Za-z0-9._-]+\}$", RegexOptions.Compiled);
    private static readonly Regex HexColorPattern = new(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);
    private static readonly Regex RgbColorPattern = new(@"^rgba?\(.+\)$", RegexOptions.Compiled);
    private static readonly Regex HslColorPattern = new(@"^hsla?\(.+\)$", RegexOptions.Compiled);

    public DesignTokenValidationResult Validate(DesignTokenRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var errors = new List<DesignTokenValidationError>();

        foreach (var token in registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            ValidateToken(token, errors);
        }

        return new DesignTokenValidationResult(registry, errors);
    }

    private static void ValidateToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        switch (token.Type)
        {
            case DesignTokenType.Color:
                ValidateColorToken(token, errors);
                break;
            case DesignTokenType.Dimension:
                ValidateDimensionToken(token, errors);
                break;
            case DesignTokenType.Duration:
                ValidateDurationToken(token, errors);
                break;
            case DesignTokenType.FontFamily:
                ValidateFontFamilyToken(token, errors);
                break;
            case DesignTokenType.FontWeight:
                ValidateFontWeightToken(token, errors);
                break;
            case DesignTokenType.Number:
                ValidateNumberToken(token, errors);
                break;
            case DesignTokenType.Typography:
                ValidateTypographyToken(token, errors);
                break;
            case DesignTokenType.Shadow:
                ValidateShadowToken(token, errors);
                break;
            case DesignTokenType.Border:
                ValidateBorderToken(token, errors);
                break;
        }
    }

    private static void ValidateColorToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not ColorTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        ValidateColorString(token, "value", value.Value, errors);
    }

    private static void ValidateDimensionToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        switch (token.ResolvedValue)
        {
            case DimensionTokenValue value:
                ValidateDimensionLike(token, "value", value.Value, value.Unit, allowUnitlessZero: true, allowNegative: true, errors);
                return;

            case ResponsiveDimensionTokenValue value:
                ValidateResponsiveDimensionToken(token, value, errors);
                return;

            default:
                AddMissingResolvedValueError(token, errors);
                return;
        }
    }

    private static void ValidateDurationToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not DurationTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (value.Value is null)
        {
            AddError(token, "value", "Missing duration value.", errors);
        }

        if (string.IsNullOrWhiteSpace(value.Unit))
        {
            AddError(token, "unit", "Missing duration unit.", errors);
        }
        else if (!DurationUnits.Contains(value.Unit))
        {
            AddError(token, "unit", $"Unsupported duration unit '{value.Unit}'.", errors);
        }

        if (value.Value < 0)
        {
            AddError(token, "value", "Duration must not be negative.", errors);
        }
    }

    private static void ValidateFontFamilyToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not FontFamilyTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (string.IsNullOrWhiteSpace(value.Value))
        {
            AddError(token, "value", "Font family must be a non-empty string.", errors);
        }
        else if (ContainsReferenceSyntax(value.Value))
        {
            AddError(token, "value", "Unresolved reference found.", errors);
        }
    }

    private static void ValidateFontWeightToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not FontWeightTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (value.Value is null)
        {
            AddError(token, "value", "Font weight must be numeric.", errors);
            return;
        }

        if (value.Value < 1 || value.Value > 1000)
        {
            AddError(token, "value", "Font weight must be between 1 and 1000.", errors);
        }
    }

    private static void ValidateNumberToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not NumberTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (value.Value is null)
        {
            AddError(token, "value", "Number token must be numeric.", errors);
        }
    }

    private static void ValidateTypographyToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not TypographyTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (HasUnresolvedTypographyReferences(value))
        {
            AddError(token, null, "Unresolved reference found.", errors);
        }

        if (string.IsNullOrWhiteSpace(value.FontFamily))
        {
            AddError(token, "fontFamily", "Typography fontFamily is required.", errors);
        }
        else
        {
            ValidateNonReferenceString(token, "fontFamily", value.FontFamily, errors);
        }

        if (value.FontWeight is null)
        {
            AddError(token, "fontWeight", "Typography fontWeight is required.", errors);
        }
        else if (value.FontWeight < 1 || value.FontWeight > 1000)
        {
            AddError(token, "fontWeight", "Typography fontWeight must be between 1 and 1000.", errors);
        }

        if (value.FontSize is null)
        {
            AddError(token, "fontSize", "Typography fontSize is required.", errors);
        }
        else
        {
            ValidateDimensionLike(token, "fontSize", value.FontSize.Value, value.FontSize.Unit, true, true, errors);
        }

        if (value.LineHeight is null)
        {
            AddError(token, "lineHeight", "Typography lineHeight is required.", errors);
        }

        if (value.LetterSpacing is not null)
        {
            ValidateDimensionLike(token, "letterSpacing", value.LetterSpacing.Value, value.LetterSpacing.Unit, true, true, errors);
        }
    }

    private static void ValidateShadowToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not ShadowTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (HasUnresolvedShadowReferences(value))
        {
            AddError(token, null, "Unresolved reference found.", errors);
        }

        if (string.IsNullOrWhiteSpace(value.Color))
        {
            AddError(token, "color", "Shadow color is required.", errors);
        }
        else
        {
            ValidateColorString(token, "color", value.Color, errors);
        }

        ValidateRequiredDimension(token, "offsetX", value.OffsetX, allowNegative: true, errors);
        ValidateRequiredDimension(token, "offsetY", value.OffsetY, allowNegative: true, errors);
        ValidateRequiredDimension(token, "blur", value.Blur, allowNegative: false, errors);
        ValidateRequiredDimension(token, "spread", value.Spread, allowNegative: true, errors);
    }

    private static void ValidateBorderToken(DesignToken token, List<DesignTokenValidationError> errors)
    {
        if (token.ResolvedValue is not BorderTokenValue value)
        {
            AddMissingResolvedValueError(token, errors);
            return;
        }

        if (!string.IsNullOrWhiteSpace(value.WidthReference) || ContainsReferenceSyntax(value.Color) || ContainsReferenceSyntax(value.Style))
        {
            AddError(token, null, "Unresolved reference found.", errors);
        }

        if (value.Width is null)
        {
            AddError(token, "width", "Border width is required.", errors);
        }
        else
        {
            ValidateDimensionLike(token, "width", value.Width.Value, value.Width.Unit, allowUnitlessZero: true, allowNegative: false, errors);
        }

        if (string.IsNullOrWhiteSpace(value.Style))
        {
            AddError(token, "style", "Border style is required.", errors);
        }
        else if (!BorderStyles.Contains(value.Style))
        {
            AddError(token, "style", $"Unsupported border style '{value.Style}'.", errors);
        }

        if (string.IsNullOrWhiteSpace(value.Color))
        {
            AddError(token, "color", "Border color is required.", errors);
        }
        else
        {
            ValidateColorString(token, "color", value.Color, errors);
        }
    }

    private static void ValidateRequiredDimension(
        DesignToken token,
        string field,
        DimensionValue? value,
        bool allowNegative,
        List<DesignTokenValidationError> errors)
    {
        if (value is null)
        {
            AddError(token, field, $"Shadow {field} is required.", errors);
            return;
        }

        ValidateDimensionLike(token, field, value.Value, value.Unit, allowUnitlessZero: true, allowNegative: allowNegative, errors);
    }

    private static void ValidateDimensionLike(
        DesignToken token,
        string field,
        decimal? value,
        string unit,
        bool allowUnitlessZero,
        bool allowNegative,
        List<DesignTokenValidationError> errors)
    {
        if (value is null)
        {
            AddError(token, $"{field}.value", "Missing value.", errors);
            return;
        }

        if (!allowNegative && value < 0)
        {
            AddError(token, $"{field}.value", "Negative values are not allowed.", errors);
        }

        var hasUnit = !string.IsNullOrWhiteSpace(unit);

        if (value == 0 && allowUnitlessZero && !hasUnit)
        {
            return;
        }

        if (!hasUnit)
        {
            AddError(token, $"{field}.unit", "Missing unit for non-zero dimension value.", errors);
            return;
        }

        if (!DimensionUnits.Contains(unit))
        {
            AddError(token, $"{field}.unit", $"Unsupported dimension unit '{unit}'.", errors);
        }
    }

    private static void ValidateColorString(
        DesignToken token,
        string field,
        string? value,
        List<DesignTokenValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(token, field, "Color value must not be empty.", errors);
            return;
        }

        if (ContainsReferenceSyntax(value))
        {
            AddError(token, field, "Unresolved reference found.", errors);
            return;
        }

        if (IsValidColor(value))
        {
            return;
        }

        AddError(token, field, $"Invalid color value '{value}'.", errors);
    }

    private static void ValidateNonReferenceString(
        DesignToken token,
        string field,
        string value,
        List<DesignTokenValidationError> errors)
    {
        if (ContainsReferenceSyntax(value))
        {
            AddError(token, field, "Unresolved reference found.", errors);
        }
    }

    private static bool IsValidColor(string value)
    {
        return HexColorPattern.IsMatch(value) ||
               RgbColorPattern.IsMatch(value) ||
               HslColorPattern.IsMatch(value) ||
               string.Equals(value, "transparent", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "currentColor", StringComparison.Ordinal);
    }

    private static bool HasUnresolvedTypographyReferences(TypographyTokenValue value) =>
        !string.IsNullOrWhiteSpace(value.Reference) ||
        !string.IsNullOrWhiteSpace(value.FontWeightReference) ||
        !string.IsNullOrWhiteSpace(value.FontSizeReference) ||
        !string.IsNullOrWhiteSpace(value.LineHeightReference) ||
        !string.IsNullOrWhiteSpace(value.LetterSpacingReference);

    private static bool HasUnresolvedShadowReferences(ShadowTokenValue value) =>
        !string.IsNullOrWhiteSpace(value.Reference) ||
        !string.IsNullOrWhiteSpace(value.OffsetXReference) ||
        !string.IsNullOrWhiteSpace(value.OffsetYReference) ||
        !string.IsNullOrWhiteSpace(value.BlurReference) ||
        !string.IsNullOrWhiteSpace(value.SpreadReference);

    private static void ValidateResponsiveDimensionToken(
        DesignToken token,
        ResponsiveDimensionTokenValue value,
        List<DesignTokenValidationError> errors)
    {
        if (!string.IsNullOrWhiteSpace(value.MobileReference) ||
            !string.IsNullOrWhiteSpace(value.TabletReference) ||
            !string.IsNullOrWhiteSpace(value.DesktopReference))
        {
            AddError(token, null, "Unresolved reference found.", errors);
        }

        if (value.Mobile is null && value.Tablet is null && value.Desktop is null)
        {
            AddError(token, null, "Responsive dimension token must define at least one breakpoint value.", errors);
            return;
        }

        ValidateOptionalResponsiveDimension(token, "mobile", value.Mobile, errors);
        ValidateOptionalResponsiveDimension(token, "tablet", value.Tablet, errors);
        ValidateOptionalResponsiveDimension(token, "desktop", value.Desktop, errors);
    }

    private static void ValidateOptionalResponsiveDimension(
        DesignToken token,
        string field,
        DimensionValue? value,
        List<DesignTokenValidationError> errors)
    {
        if (value is null)
        {
            return;
        }

        ValidateDimensionLike(token, field, value.Value, value.Unit, allowUnitlessZero: true, allowNegative: true, errors);
    }

    private static bool ContainsReferenceSyntax(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (ReferencePattern.IsMatch(value) ||
         value.Contains('{', StringComparison.Ordinal) ||
         value.Contains('}', StringComparison.Ordinal));

    private static void AddMissingResolvedValueError(DesignToken token, List<DesignTokenValidationError> errors) =>
        AddError(token, null, "Resolved value is missing.", errors);

    private static void AddError(
        DesignToken token,
        string? field,
        string message,
        List<DesignTokenValidationError> errors)
    {
        errors.Add(new DesignTokenValidationError(token.Path.Value, token.Type, field, message));
    }
}
