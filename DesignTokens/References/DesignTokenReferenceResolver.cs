using System.Text.RegularExpressions;
using Site.DesignTokens.Models;
using Site.DesignTokens.Sources;
using Site.DesignTokens.Values;

namespace Site.DesignTokens.References;

public sealed class DesignTokenReferenceResolver : IDesignTokenReferenceResolver
{
    private static readonly Regex ReferencePattern = new(@"^\{(?<path>[A-Za-z0-9._-]+)\}$", RegexOptions.Compiled);
    private readonly Dictionary<string, ResolutionState> _states = new(StringComparer.Ordinal);
    private readonly List<DesignTokenReferenceResolutionError> _errors = [];
    private DesignTokenRegistry _sourceRegistry = null!;
    private DesignTokenRegistry _resolvedRegistry = null!;

    public DesignTokenReferenceResolutionResult Resolve(DesignTokenRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        _sourceRegistry = registry;
        _resolvedRegistry = new DesignTokenRegistry();
        _states.Clear();
        _errors.Clear();

        foreach (var token in registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            ResolveToken(token.Path.Value, []);
        }

        return new DesignTokenReferenceResolutionResult(_resolvedRegistry, _errors);
    }

    private DesignToken? ResolveToken(string path, List<string> chain)
    {
        if (_states.TryGetValue(path, out var existingState))
        {
            if (existingState.Status == TokenResolutionStatus.Resolved)
            {
                return existingState.Token;
            }

            if (existingState.Status == TokenResolutionStatus.Resolving)
            {
                var cycleChain = string.Join(" -> ", chain.Concat([path]));
                AddError(chain.LastOrDefault() ?? path, $"Circular reference detected: {cycleChain}");
                return existingState.Token;
            }

            return existingState.Token;
        }

        if (!_sourceRegistry.TryGet(path, out var sourceToken) || sourceToken is null)
        {
            return null;
        }

        _states[path] = new ResolutionState(TokenResolutionStatus.Resolving, sourceToken);
        chain.Add(path);

        if (!TryResolveTokenValue(sourceToken, chain, out var resolvedValue))
        {
            var failedToken = new DesignToken(
                sourceToken.Path,
                sourceToken.Type,
                rawValue: sourceToken.RawValue,
                normalizedValue: sourceToken.NormalizedValue,
                resolvedValue: null,
                description: sourceToken.Description,
                sourceType: sourceToken.SourceType,
                sourceName: sourceToken.SourceName,
                sourcePriority: sourceToken.SourcePriority);

            UpsertResolvedToken(failedToken);
            _states[path] = new ResolutionState(TokenResolutionStatus.Failed, failedToken);
            chain.RemoveAt(chain.Count - 1);
            return failedToken;
        }

        var resolvedToken = new DesignToken(
            sourceToken.Path,
            sourceToken.Type,
            rawValue: sourceToken.RawValue,
            normalizedValue: sourceToken.NormalizedValue,
            resolvedValue: resolvedValue,
            description: sourceToken.Description,
            sourceType: sourceToken.SourceType,
            sourceName: sourceToken.SourceName,
            sourcePriority: sourceToken.SourcePriority);

        UpsertResolvedToken(resolvedToken);
        _states[path] = new ResolutionState(TokenResolutionStatus.Resolved, resolvedToken);
        chain.RemoveAt(chain.Count - 1);
        return resolvedToken;
    }

    private bool TryResolveTokenValue(
        DesignToken token,
        List<string> chain,
        out object resolvedValue)
    {
        resolvedValue = null!;

        switch (token.Type)
        {
            case DesignTokenType.Color:
                return TryResolveColorToken(token, chain, out resolvedValue);
            case DesignTokenType.Dimension:
                return TryResolveDimensionToken(token, chain, out resolvedValue);
            case DesignTokenType.FontFamily:
                return TryResolveFontFamilyToken(token, chain, out resolvedValue);
            case DesignTokenType.FontWeight:
                return TryResolveFontWeightToken(token, chain, out resolvedValue);
            case DesignTokenType.Duration:
                return TryResolveDurationToken(token, chain, out resolvedValue);
            case DesignTokenType.Number:
                return TryResolveNumberToken(token, chain, out resolvedValue);
            case DesignTokenType.Typography:
                return TryResolveTypographyToken(token, chain, out resolvedValue);
            case DesignTokenType.Shadow:
                return TryResolveShadowToken(token, chain, out resolvedValue);
            case DesignTokenType.Border:
                return TryResolveBorderToken(token, chain, out resolvedValue);
            default:
                AddError(token.Path.Value, $"Unsupported token type '{token.Type}'.");
                return false;
        }
    }

    private bool TryResolveColorToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as ColorTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized color value.");
            return false;
        }

        if (!TryResolveReferenceString(token.Path.Value, normalized.Value, DesignTokenType.Color, chain, out var resolvedString))
        {
            return false;
        }

        resolvedValue = new ColorTokenValue { Value = resolvedString ?? normalized.Value };
        return true;
    }

    private bool TryResolveFontFamilyToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as FontFamilyTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized fontFamily value.");
            return false;
        }

        if (!TryResolveReferenceString(token.Path.Value, normalized.Value, DesignTokenType.FontFamily, chain, out var resolvedString))
        {
            return false;
        }

        resolvedValue = new FontFamilyTokenValue { Value = resolvedString ?? normalized.Value };
        return true;
    }

    private bool TryResolveFontWeightToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as FontWeightTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized fontWeight value.");
            return false;
        }

        if (!TryResolveReferenceValue(
                token.Path.Value,
                normalized.Reference,
                DesignTokenType.FontWeight,
                chain,
                ExtractFontWeightValue,
                out var fontWeight))
        {
            return false;
        }

        resolvedValue = new FontWeightTokenValue
        {
            Value = fontWeight ?? normalized.Value
        };

        return true;
    }

    private bool TryResolveNumberToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as NumberTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized number value.");
            return false;
        }

        if (!TryResolveReferenceValue(
                token.Path.Value,
                normalized.Reference,
                DesignTokenType.Number,
                chain,
                ExtractNumberValue,
                out var numberValue))
        {
            return false;
        }

        resolvedValue = new NumberTokenValue
        {
            Value = numberValue ?? normalized.Value
        };

        return true;
    }

    private bool TryResolveDimensionToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        if (token.NormalizedValue is DimensionTokenValue normalizedStatic)
        {
            if (!TryResolveReferenceValue(
                    token.Path.Value,
                    normalizedStatic.Reference,
                    DesignTokenType.Dimension,
                    chain,
                    ExtractStaticDimensionTokenValue,
                    out var dimensionValue))
            {
                return false;
            }

            var resolvedDimension = dimensionValue ?? normalizedStatic;
            resolvedValue = new DimensionTokenValue
            {
                Value = resolvedDimension.Value,
                Unit = resolvedDimension.Unit
            };

            return true;
        }

        if (token.NormalizedValue is ResponsiveDimensionTokenValue normalizedResponsive)
        {
            if (!TryResolveResponsiveDimensionBreakpoint(token.Path.Value, "mobile", normalizedResponsive.Mobile, normalizedResponsive.MobileReference, chain, out var mobile) ||
                !TryResolveResponsiveDimensionBreakpoint(token.Path.Value, "tablet", normalizedResponsive.Tablet, normalizedResponsive.TabletReference, chain, out var tablet) ||
                !TryResolveResponsiveDimensionBreakpoint(token.Path.Value, "desktop", normalizedResponsive.Desktop, normalizedResponsive.DesktopReference, chain, out var desktop))
            {
                return false;
            }

            resolvedValue = new ResponsiveDimensionTokenValue
            {
                Mobile = mobile,
                Tablet = tablet,
                Desktop = desktop
            };

            return true;
        }

        AddError(token.Path.Value, "Token has no normalized dimension value.");
        return false;
    }

    private bool TryResolveDurationToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as DurationTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized duration value.");
            return false;
        }

        if (!TryResolveReferenceValue(
                token.Path.Value,
                normalized.Reference,
                DesignTokenType.Duration,
                chain,
                ExtractDurationTokenValue,
                out var durationValue))
        {
            return false;
        }

        var resolvedDuration = durationValue ?? normalized;
        resolvedValue = new DurationTokenValue
        {
            Value = resolvedDuration.Value,
            Unit = resolvedDuration.Unit
        };

        return true;
    }

    private bool TryResolveTypographyToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as TypographyTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized typography value.");
            return false;
        }

        if (!TryResolveReferenceValue(
                token.Path.Value,
                normalized.Reference,
                DesignTokenType.Typography,
                chain,
                ExtractTypographyValue,
                out var referencedTypography))
        {
            return false;
        }

        if (referencedTypography is not null)
        {
            resolvedValue = referencedTypography;
            return true;
        }

        if (!TryResolveReferenceString(token.Path.Value, normalized.FontFamily, DesignTokenType.FontFamily, chain, out var fontFamily) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.FontWeightReference, DesignTokenType.FontWeight, chain, ExtractFontWeightValue, out var fontWeight) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.FontSizeReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var fontSize) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.LineHeightReference, DesignTokenType.Number, chain, ExtractNumberValue, out var lineHeight) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.LetterSpacingReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var letterSpacing))
        {
            return false;
        }

        resolvedValue = new TypographyTokenValue
        {
            FontFamily = fontFamily ?? normalized.FontFamily,
            FontWeight = fontWeight ?? normalized.FontWeight,
            FontSize = fontSize ?? normalized.FontSize,
            LineHeight = lineHeight ?? normalized.LineHeight,
            LetterSpacing = letterSpacing ?? normalized.LetterSpacing
        };

        return true;
    }

    private bool TryResolveShadowToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as ShadowTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized shadow value.");
            return false;
        }

        if (!TryResolveReferenceValue(
                token.Path.Value,
                normalized.Reference,
                DesignTokenType.Shadow,
                chain,
                ExtractShadowValue,
                out var referencedShadow))
        {
            return false;
        }

        if (referencedShadow is not null)
        {
            resolvedValue = referencedShadow;
            return true;
        }

        if (!TryResolveReferenceString(token.Path.Value, normalized.Color, DesignTokenType.Color, chain, out var color) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.OffsetXReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var offsetX) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.OffsetYReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var offsetY) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.BlurReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var blur) ||
            !TryResolveReferenceValue(token.Path.Value, normalized.SpreadReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var spread))
        {
            return false;
        }

        resolvedValue = new ShadowTokenValue
        {
            Color = color ?? normalized.Color,
            OffsetX = offsetX ?? normalized.OffsetX,
            OffsetY = offsetY ?? normalized.OffsetY,
            Blur = blur ?? normalized.Blur,
            Spread = spread ?? normalized.Spread
        };

        return true;
    }

    private bool TryResolveBorderToken(DesignToken token, List<string> chain, out object resolvedValue)
    {
        resolvedValue = null!;
        var normalized = token.NormalizedValue as BorderTokenValue;
        if (normalized is null)
        {
            AddError(token.Path.Value, "Token has no normalized border value.");
            return false;
        }

        if (!TryResolveReferenceValue(
                token.Path.Value,
                normalized.Reference,
                DesignTokenType.Border,
                chain,
                ExtractBorderValue,
                out var referencedBorder))
        {
            return false;
        }

        if (referencedBorder is not null)
        {
            resolvedValue = referencedBorder;
            return true;
        }

        if (!TryResolveReferenceValue(token.Path.Value, normalized.WidthReference, DesignTokenType.Dimension, chain, ExtractDimensionValue, out var width) ||
            !TryValidateLiteralOnly(token.Path.Value, normalized.Style, "border.style") ||
            !TryResolveReferenceString(token.Path.Value, normalized.Color, DesignTokenType.Color, chain, out var color))
        {
            return false;
        }

        resolvedValue = new BorderTokenValue
        {
            Width = width ?? normalized.Width,
            Style = normalized.Style,
            Color = color ?? normalized.Color
        };

        return true;
    }

    private bool TryResolveReferenceString(
        string sourcePath,
        string? value,
        DesignTokenType expectedType,
        List<string> chain,
        out string? resolvedValue)
    {
        resolvedValue = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (TryParseReference(value, out var reference))
        {
            if (!TryResolveReferenceValue(sourcePath, reference.RawReference, expectedType, chain, ExtractStringValue, out var stringValue))
            {
                return false;
            }

            resolvedValue = stringValue;
            return true;
        }

        if (ContainsReferenceSyntax(value))
        {
            AddError(sourcePath, $"Embedded references are not supported: {value}");
            return false;
        }

        return true;
    }

    private bool TryResolveReferenceValue<T>(
        string sourcePath,
        string? rawReference,
        DesignTokenType expectedType,
        List<string> chain,
        Func<DesignToken, T?> projector,
        out T? resolvedValue)
    {
        resolvedValue = default;

        if (string.IsNullOrWhiteSpace(rawReference))
        {
            return true;
        }

        if (!TryParseReference(rawReference, out var reference))
        {
            AddError(sourcePath, $"Embedded references are not supported: {rawReference}");
            return false;
        }

        var targetToken = ResolveToken(reference.TargetPath, chain);
        if (targetToken is null)
        {
            AddError(sourcePath, $"Token reference target not found: {reference.TargetPath}");
            return false;
        }

        if (targetToken.Type != expectedType)
        {
            AddError(sourcePath, $"Incompatible reference type. Expected {expectedType}, got {targetToken.Type} from {reference.TargetPath}.");
            return false;
        }

        var projectedValue = projector(targetToken);
        if (projectedValue is null)
        {
            AddError(sourcePath, $"Referenced token '{reference.TargetPath}' did not resolve to a usable {expectedType} value.");
            return false;
        }

        resolvedValue = projectedValue;
        return true;
    }

    private bool TryResolveResponsiveDimensionBreakpoint(
        string sourcePath,
        string breakpointName,
        DimensionValue? value,
        string? rawReference,
        List<string> chain,
        out DimensionValue? resolvedValue)
    {
        resolvedValue = value;

        if (string.IsNullOrWhiteSpace(rawReference))
        {
            return true;
        }

        if (!TryParseReference(rawReference, out var reference))
        {
            AddError(sourcePath, $"Embedded references are not supported: {rawReference}");
            return false;
        }

        var targetToken = ResolveToken(reference.TargetPath, chain);
        if (targetToken is null)
        {
            AddError(sourcePath, $"Token reference target not found: {reference.TargetPath}");
            return false;
        }

        if (targetToken.Type != DesignTokenType.Dimension)
        {
            AddError(sourcePath, $"Incompatible reference type. Expected Dimension, got {targetToken.Type} from {reference.TargetPath}.");
            return false;
        }

        if (targetToken.ResolvedValue is ResponsiveDimensionTokenValue)
        {
            AddError(sourcePath, $"Responsive dimension breakpoint '{breakpointName}' cannot reference responsive token '{reference.TargetPath}' in this step.");
            return false;
        }

        var dimensionValue = ExtractDimensionValue(targetToken);
        if (dimensionValue is null)
        {
            AddError(sourcePath, $"Referenced token '{reference.TargetPath}' did not resolve to a usable Dimension value.");
            return false;
        }

        resolvedValue = dimensionValue;
        return true;
    }

    private bool TryValidateLiteralOnly(string sourcePath, string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (ContainsReferenceSyntax(value))
        {
            AddError(sourcePath, $"{fieldName} cannot reference another token in this step.");
            return false;
        }

        return true;
    }

    private static string? ExtractStringValue(DesignToken token)
    {
        return token.ResolvedValue switch
        {
            ColorTokenValue color => color.Value,
            FontFamilyTokenValue fontFamily => fontFamily.Value,
            _ => null
        };
    }

    private static int? ExtractFontWeightValue(DesignToken token) =>
        (token.ResolvedValue as FontWeightTokenValue)?.Value;

    private static decimal? ExtractNumberValue(DesignToken token) =>
        (token.ResolvedValue as NumberTokenValue)?.Value;

    private static DimensionValue? ExtractDimensionValue(DesignToken token)
    {
        var value = token.ResolvedValue as DimensionTokenValue;
        return value?.Value is null
            ? null
            : new DimensionValue { Value = value.Value.Value, Unit = value.Unit };
    }

    private static DimensionTokenValue? ExtractStaticDimensionTokenValue(DesignToken token)
    {
        return token.ResolvedValue switch
        {
            DimensionTokenValue dimensionValue => dimensionValue,
            ResponsiveDimensionTokenValue => null,
            _ => null
        };
    }

    private static DurationTokenValue? ExtractDurationTokenValue(DesignToken token) =>
        token.ResolvedValue as DurationTokenValue;

    private static TypographyTokenValue? ExtractTypographyValue(DesignToken token) =>
        token.ResolvedValue as TypographyTokenValue;

    private static ShadowTokenValue? ExtractShadowValue(DesignToken token) =>
        token.ResolvedValue as ShadowTokenValue;

    private static BorderTokenValue? ExtractBorderValue(DesignToken token) =>
        token.ResolvedValue as BorderTokenValue;

    private static bool TryParseReference(string value, out DesignTokenReference reference)
    {
        var match = ReferencePattern.Match(value);
        if (!match.Success)
        {
            reference = null!;
            return false;
        }

        var targetPath = match.Groups["path"].Value;
        reference = new DesignTokenReference(string.Empty, targetPath, value);
        return true;
    }

    private static bool ContainsReferenceSyntax(string value) =>
        value.Contains('{', StringComparison.Ordinal) || value.Contains('}', StringComparison.Ordinal);

    private void AddError(string sourcePath, string message)
    {
        _errors.Add(new DesignTokenReferenceResolutionError(sourcePath, message));
    }

    private void UpsertResolvedToken(DesignToken token)
    {
        if (_resolvedRegistry.TryGet(token.Path, out _))
        {
            return;
        }

        _resolvedRegistry.Add(token);
    }

    private sealed record ResolutionState(TokenResolutionStatus Status, DesignToken Token);

    private enum TokenResolutionStatus
    {
        Resolving,
        Resolved,
        Failed
    }
}
