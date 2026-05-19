namespace Site.DesignTokens.Values;

public sealed class ShadowTokenValue
{
    public string? Reference { get; init; }

    public string? Color { get; init; }

    public DimensionValue? OffsetX { get; init; }

    public string? OffsetXReference { get; init; }

    public DimensionValue? OffsetY { get; init; }

    public string? OffsetYReference { get; init; }

    public DimensionValue? Blur { get; init; }

    public string? BlurReference { get; init; }

    public DimensionValue? Spread { get; init; }

    public string? SpreadReference { get; init; }
}
