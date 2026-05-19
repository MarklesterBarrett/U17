namespace Site.DesignTokens.Values;

public sealed class TypographyTokenValue
{
    public string? Reference { get; init; }

    public string? FontFamily { get; init; }

    public int? FontWeight { get; init; }

    public string? FontWeightReference { get; init; }

    public DimensionValue? FontSize { get; init; }

    public string? FontSizeReference { get; init; }

    public decimal? LineHeight { get; init; }

    public string? LineHeightReference { get; init; }

    public DimensionValue? LetterSpacing { get; init; }

    public string? LetterSpacingReference { get; init; }
}
